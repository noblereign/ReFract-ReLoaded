using BepInEx;
using System;
using System.Reflection;
using System.Collections;
using InterprocessLib;
using ReFract.Shared;
using Renderite.Unity;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using AmplifyOcclusion;

namespace ReFract.Unity;

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private Messenger _msg;
    private static readonly Dictionary<int, UnityEngine.Camera> _cameraCache = new();
    
    public static Dictionary<string, Type> TypeLookups = new Dictionary<string, Type>
    {
        { "AmbientOcclusion", typeof(AmbientOcclusion) },
        { "AutoExposure", typeof(AutoExposure) },
        { "Bloom", typeof(Bloom) },
        { "ChromaticAberration", typeof(ChromaticAberration) },
        { "ColorGrading", typeof(ColorGrading) },
        { "DepthOfField", typeof(DepthOfField) },
        { "Grain", typeof(Grain) },
        { "LensDistortion", typeof(LensDistortion) },
        { "MotionBlur", typeof(MotionBlur) },
        { "ScreenSpaceReflections", typeof(ScreenSpaceReflections) },
        { "Vignette", typeof(Vignette) },
        { "AmplifyOcclusionBase", typeof(AmplifyOcclusionBase) } // Include this specifically since it does post processing, but is not part of the bundle stack
    };
    // TypeLookups will be used to easily get a type from one specified in a dynamic variable name string

	void Awake()
	{
        _msg = new Messenger("dog.glacier.ReFract", [typeof(ReFractCommand)]);
        try
        {
            _msg.ReceiveObject<ReFractCommand>("SetVariable", HandleSetVariable);
            Debug.Log($"[Re:Fract] Receiver registered!");
        }
        catch (TypeLoadException ex)
        {
            Debug.Log($"[Re:Fract] Failed to register receiver. This is likely a DLL version mismatch: {ex}");
        }
    }

    private void HandleSetVariable(ReFractCommand command)
    {
        Debug.Log($"[Re:Fract] Received command for RT ID: {command.RenderTextureId}");

        if (command.RenderTextureId == 0)
        {
            Debug.LogWarning("[Re:Fract] ERROR: Command has a zero RenderTextureId. Ignoring.");
            return;
        }

        if (!_cameraCache.TryGetValue(command.RenderTextureId, out var camera) || camera == null)
        {
            Debug.Log($"[Re:Fract] Camera for {command.RenderTextureId} not in cache or is null. Searching...");
            
            var rtAsset = RenderingManager.Instance.RenderTextures.GetAsset(command.RenderTextureId);
            if (rtAsset?.Texture == null)
            {
                // This can happen if the texture isn't ready yet. Don't log an error, just wait for the next command.
                return;
            }
            Debug.Log($"[Re:Fract] Found render texture asset for {command.RenderTextureId}");

            camera = FindCameraRenderingTo(rtAsset.Texture);
            if (camera == null)
            {
                Debug.LogWarning($"[Re:Fract] ...but no camera was found. Waiting a few frames...");
                StartCoroutine(WaitForCameraAndApply(command));
                return;
            }
            _cameraCache[command.RenderTextureId] = camera;
            Debug.Log($"[Re:Fract] Found and cached camera '{camera.name}' for ID {command.RenderTextureId}");
        }
        else
        {
            Debug.Log($"[Re:Fract] Using cached camera '{camera.name}' for ID {command.RenderTextureId}");
        }

        if (camera == null) // It might have been destroyed since last checked
        {
            Debug.LogWarning($"[Re:Fract] ERROR: Cached camera for {command.RenderTextureId} was destroyed. Removing from cache.");
            _cameraCache.Remove(command.RenderTextureId);
            return;
        }

        ApplyCommand(camera, command);
    }

    private IEnumerator WaitForCameraAndApply(ReFractCommand command)
    {
        for (int i = 0; i < 5; i++)
            yield return null;

        var rtAsset = RenderingManager.Instance.RenderTextures.GetAsset(command.RenderTextureId);
        if (rtAsset?.Texture == null) yield break;

        var camera = FindCameraRenderingTo(rtAsset.Texture);
        if (camera != null)
        {
            _cameraCache[command.RenderTextureId] = camera;
            Debug.Log($"[Re:Fract] Found camera '{camera.name}' after waiting.");
            ApplyCommand(camera, command);
        }
        else
        {
            Debug.LogWarning($"[Re:Fract] ...still no camera found after waiting.");
        }
    }

    private void ApplyCommand(Camera camera, ReFractCommand command)
    {
        Debug.Log($"[Re:Fract] Searching for component '{command.ComponentName}' on camera '{camera.name}'");
        
        object target = null;
        Type type = null;
        if (TypeLookups.TryGetValue(command.ComponentName, out type))
        {
            target = camera.GetComponent(type);
        }
        else
        {
            //target = camera.GetComponent(command.ComponentName);
            Debug.LogWarning($"[Re:Fract] Unsupported Type {command.ComponentName}");
            return;
        }
        
        if (target == null)
        {
            // Try to find the component in the PostProcessLayer bundles
            var postProcessLayer = camera.gameObject.GetComponent<PostProcessLayer>();
            if (postProcessLayer != null)
            {
                var bundle = postProcessLayer.GetBundle(type);
                if (bundle != null)
                {
                    target = bundle.settings;
                }
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"[Re:Fract] ERROR: Camera '{camera.name}' does not have component '{command.ComponentName}'");
            return;
        }
        Debug.Log($"[Re:Fract] Found target '{command.ComponentName}'");

        object value = GetValueFromCommand(command);
        var compType = target.GetType();
        
        Debug.Log($"[Re:Fract] Attempting to set '{command.ParameterName}' on '{command.ComponentName} ({compType.FullName})' to value '{value}'");

        // Check for PostProcessing Parameters (ParameterOverride)
        var field = compType.GetField(command.ParameterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && typeof(ParameterOverride).IsAssignableFrom(field.FieldType))
        {
            var fieldValue = field.GetValue(target); // This is the ParameterOverride instance
            if (fieldValue != null)
            {
                var paramType = fieldValue.GetType();
                bool valueSet = false;

                // Try Field first (Standard for PostProcessing Stack v2)
                var valueField = paramType.GetField("value", BindingFlags.Instance | BindingFlags.Public);
                if (valueField != null)
                {
                    try
                    {
                        valueField.SetValue(fieldValue, value);
                        Debug.Log($"[Re:Fract] Set 'value' field on ParameterOverride '{command.ParameterName}'.");
                        valueSet = true;
                    }
                    catch (Exception ex) { Debug.LogWarning($"[Re:Fract] Failed to set ParameterOverride field value: {ex}"); }
                }

                // Fallback to Property if field fails or doesn't exist
                if (!valueSet)
                {
                    var valueProp = paramType.GetProperty("value");
                    if (valueProp != null && valueProp.CanWrite)
                    {
                        try
                        {
                            valueProp.SetValue(fieldValue, value);
                            Debug.Log($"[Re:Fract] Set 'value' property on ParameterOverride '{command.ParameterName}'.");
                            valueSet = true;
                        }
                        catch (Exception ex) { Debug.LogWarning($"[Re:Fract] Failed to set ParameterOverride property value: {ex}"); }
                    }
                }
        
                if (valueSet)
                {
                    // Enable the override. This is crucial for the change to take effect.
                    var overrideStateField = paramType.GetField("overrideState", BindingFlags.Instance | BindingFlags.Public);
                    if (overrideStateField != null)
                    {
                        overrideStateField.SetValue(fieldValue, true);
                        Debug.Log($"[Re:Fract] SUCCESS: Activated override for '{command.ParameterName}'.");
                    }
                    else
                    {
                        Debug.LogWarning($"[Re:Fract] Could not find 'overrideState' field on '{paramType.Name}'. The setting may not apply visually.");
                    }
                    return;
                }
            }
        }

        var propSetter = Introspection.GetPropSetter(compType, command.ParameterName);
        if (propSetter != null)
        {
            propSetter(target, value);
            Debug.Log($"[Re:Fract] SUCCESS: Set property '{command.ParameterName}'.");
            return;
        }

        var fieldSetter = Introspection.GetFieldSetter(compType, command.ParameterName);
        if (fieldSetter != null)
        {
            fieldSetter(ref target, value);
            Debug.Log($"[Re:Fract] SUCCESS: Set field '{command.ParameterName}'.");
            return;
        }

        Debug.LogWarning($"[Re:Fract] ERROR: Could not find writable property or field '{command.ParameterName}' on component '{command.ComponentName}'");
    }

    private static object GetValueFromCommand(ReFractCommand command)
    {
        switch (command.ValueType)
        {
            case ReFractCommandValueType.Int:
                return command.IntValue;
            case ReFractCommandValueType.Float:
                return command.FloatValue;
            case ReFractCommandValueType.Bool:
                return command.BoolValue;
            case ReFractCommandValueType.Color:
                return new Color(command.ColorValue.r, command.ColorValue.g, command.ColorValue.b, command.ColorValue.a);
            case ReFractCommandValueType.Vector2:
                return new Vector2(command.Vector2Value.x, command.Vector2Value.y);
            case ReFractCommandValueType.Vector4:
                return new Vector4(command.Vector4Value.x, command.Vector4Value.y, command.Vector4Value.z, command.Vector4Value.w);
            case ReFractCommandValueType.String:
                return command.StringValue;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static UnityEngine.Camera? FindCameraRenderingTo(RenderTexture target)
    {
        //return UnityEngine.Object.FindObjectsOfType<UnityEngine.Camera>().FirstOrDefault(c => c.activeTexture == target);
        // Find all cameras currently in the scene
        Debug.Log($"[Re:Fract] Searching for RenderTexture {(target ? target.name : "NULL TARGET")} @ {(target ? target.GetInstanceID() : "N/A")}");
        Camera[] allCameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in allCameras)
        {
            Debug.Log($"[Re:Fract] Camera {cam.name} @ {cam.GetInstanceID()} --> {(cam.targetTexture ? cam.targetTexture.name : "NULL TARGET")} @ {(cam.targetTexture ? cam.targetTexture.GetInstanceID() : "N/A")}...");
            // Check if the camera's targetTexture matches the desired one
            if (cam.targetTexture == target)
            {
                return cam;
            }
        }

        Debug.LogWarning("[Re:Fract] No camera found rendering to the specified RenderTexture.");
        return null;
    }
}

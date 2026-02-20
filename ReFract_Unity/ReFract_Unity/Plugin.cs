using BepInEx;
using InterprocessLib;
using ReFract.Shared;
using Renderite.Unity;
using UnityEngine;

namespace ReFract.Unity;

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private Messenger _msg;
    private static readonly Dictionary<int, UnityEngine.Camera> _cameraCache = new();
    
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
                Debug.LogWarning($"[Re:Fract] ...but no camera was found. :(");
                // This can also happen if the camera isn't ready yet.
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
        
        Debug.Log($"[Re:Fract] Searching for component '{command.ComponentName}' on camera '{camera.name}'");
        // TODO: This is slow. Cache component types or use a faster lookup method.
        var component = camera.GetComponent(command.ComponentName);
        if (component == null)
        {
            Debug.LogWarning($"[Re:Fract] ERROR: Camera '{camera.name}' does not have component '{command.ComponentName}'");
            return;
        }
        Debug.Log($"[Re:Fract] Found component '{command.ComponentName}'");

        object value = GetValueFromCommand(command);
        var compType = component.GetType();
        
        Debug.Log($"[Re:Fract] Attempting to set '{command.ParameterName}' on '{command.ComponentName}' to value '{value}'");

        var propSetter = Introspection.GetPropSetter(compType, command.ParameterName);
        if (propSetter != null)
        {
            propSetter(component, value);
            Debug.Log($"[Re:Fract] SUCCESS: Set property '{command.ParameterName}'.");
            return;
        }

        var fieldSetter = Introspection.GetFieldSetter(compType, command.ParameterName);
        if (fieldSetter != null)
        {
            object compObj = component;
            fieldSetter(ref compObj, value);
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
        Camera[] allCameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in allCameras)
        {
            Debug.Log($"Camera {cam.name} @ {cam.GetInstanceID()}...");
            // Check if the camera's targetTexture matches the desired one
            if (cam.activeTexture == target)
            {
                return cam;
            }
        }

        Debug.LogWarning("No camera found rendering to the specified RenderTexture.");
        return null;
    }
}

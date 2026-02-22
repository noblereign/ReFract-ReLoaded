using BepInEx;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using InterprocessLib;
using ReFract.Shared;
using Renderite.Shared;
using Renderite.Unity;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using AmplifyOcclusion;
using TextureFormat = UnityEngine.TextureFormat;

namespace ReFract.Unity;

/// <summary>
/// This component is added to every camera managed by Re:Fract.
/// Its sole purpose is to ensure that when the camera GameObject is destroyed,
/// the "ReFract_Volume" child object is also explicitly destroyed. This prevents
/// the volume from being orphaned and left in the scene root.
/// </summary>
public class ReFractVolumeTracker : MonoBehaviour
{
    public GameObject VolumeObject;

    void OnDestroy()
    {
        if (VolumeObject != null)
        {
            Destroy(VolumeObject);
        }
        else
        {
            var child = transform.Find("ReFract_Volume");
            if (child != null) Destroy(child.gameObject);
        }
    }
}

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private Messenger _msg;
    private static readonly Dictionary<int, List<UnityEngine.Camera>> _cameraCache = new();
    private static readonly Dictionary<int, bool> _removeAlphaCameras = new();
    
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

            Harmony harmony = new Harmony("dog.glacier.ReFractUnity");
            harmony.PatchAll();
        }
        catch (TypeLoadException ex)
        {
            Debug.Log($"[Re:Fract] Failed to register receiver. This is likely a DLL version mismatch: {ex}");
        }
    }

    [HarmonyPatch(typeof(CameraRenderer), "Render")]
    public static class CameraRenderer_Patch
    {
        private static FieldInfo _cameraField;
        private static FieldInfo _camera360Field;

        // Note: This method of finding the source camera is shared between Prefix and Postfix.
        private static Camera FindSourceCamera(CameraRenderTask task)
        {
            Vector3 taskPos = new Vector3(task.position.x, task.position.y, task.position.z);
            Quaternion taskRot = new Quaternion(task.rotation.x, task.rotation.y, task.rotation.z, task.rotation.w);
            Camera sourceCam = null;
            float minDistance = float.MaxValue;

            foreach (var list in _cameraCache.Values)
            {
                if (list == null) continue;
                foreach (var cam in list)
                {
                    if (cam == null) continue;

                    if (cam.orthographic)
                    {
                        if (task.parameters.projection != CameraProjection.Orthographic) continue;
                        if (Mathf.Abs(cam.orthographicSize - task.parameters.orthographicSize) > 0.01f) continue;
                    }
                    else
                    {
                        if (task.parameters.projection == CameraProjection.Orthographic) continue;
                        if (task.parameters.fov < 180f && Mathf.Abs(cam.fieldOfView - task.parameters.fov) > 5.0f) continue;
                    }

                    float dist = Vector3.Distance(cam.transform.position, taskPos);
                    float angle = Quaternion.Angle(cam.transform.rotation, taskRot);

                    if (dist < 2.0f && angle < 15.0f)
                    {
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            sourceCam = cam;
                        }
                    }
                }
            }
            return sourceCam;
        }
        
        private static Camera GetCaptureCamera(CameraRenderTask task)
        {
            if (task.parameters.fov >= 180f)
            {
                if (_camera360Field == null) _camera360Field = AccessTools.Field(typeof(CameraRenderer), "camera360");
                var cam360 = _camera360Field.GetValue(null);
                if (cam360 == null) return null;
                var prop = AccessTools.Property(cam360.GetType(), "Camera");
                return prop?.GetValue(cam360) as Camera;
            }
            
            if (_cameraField == null) _cameraField = AccessTools.Field(typeof(CameraRenderer), "camera");
            return _cameraField.GetValue(null) as Camera;
        }

        [HarmonyPrefix]
        public static void Prefix(CameraRenderTask task)
        {
            var captureCam = GetCaptureCamera(task);
            if (captureCam == null) return;
            
            var sourceCam = FindSourceCamera(task);

            var rootVolume = captureCam.gameObject.GetComponent<PostProcessVolume>();
            var childTransform = captureCam.transform.Find("ReFract_Volume");
            var childVolume = childTransform != null ? childTransform.GetComponent<PostProcessVolume>() : null;

            if (sourceCam != null)
            {
                Debug.Log($"[Re:Fract] Syncing PostProcess from '{sourceCam.name}' to Capture Camera.");
                var sourceVolume = sourceCam.GetComponentInChildren<PostProcessVolume>();
                if (sourceVolume != null && sourceVolume.profile != null)
                {
                    int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                    if (childVolume == null)
                    {
                        var child = new GameObject("ReFract_Volume");
                        child.transform.SetParent(captureCam.transform, false);
                        child.layer = ignoreRaycastLayer; 
                        childVolume = child.AddComponent<PostProcessVolume>();
                        var col = child.AddComponent<BoxCollider>();
                        col.isTrigger = true;
                        col.size = Vector3.one * 0.01f;
                        childVolume.isGlobal = false;
                    }
                    
                    if (rootVolume != null) rootVolume.enabled = false;

                    childVolume.enabled = true;

                    var profileClone = Instantiate(sourceVolume.profile);
                    var mb = profileClone.GetSetting<MotionBlur>();
                    if (mb != null) mb.enabled.value = false;

                    childVolume.profile = profileClone;
                    childVolume.weight = sourceVolume.weight;

                    var layer = captureCam.GetComponent<PostProcessLayer>();
                    if (layer != null)
                    {
                        layer.volumeLayer |= (1 << ignoreRaycastLayer);
                        layer.volumeTrigger = captureCam.transform;
                    }
                }
            }
            else
            {
                if (rootVolume != null) rootVolume.enabled = false;
                if (childVolume != null) childVolume.enabled = false;
            }
        }
        
        [HarmonyPostfix]
        public static void Postfix(CameraRenderTask task)
        {
            var sourceCam = FindSourceCamera(task);
            if (sourceCam == null || sourceCam.targetTexture == null) return;

            if (_removeAlphaCameras.TryGetValue(sourceCam.targetTexture.GetInstanceID(), out bool removeAlpha) && removeAlpha)
            {
                try
                {
                    // Access the shared memory buffer where the render result was just written
                    Span<byte> data = RenderingManager.Instance.SharedMemory.AccessData(task.resultData);
                    
                    // Determine alpha offset based on format
                    // Default to RGBA (offset 3)
                    int alphaOffset = 3;
                    
                    // Check for ARGB format (offset 0)
                    string fmt = task.parameters.textureFormat.ToString();
                    if (fmt.IndexOf("ARGB", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        alphaOffset = 0;
                    }

                    // Set alpha to 255 (Opaque)
                    for (int i = alphaOffset; i < data.Length; i += 4)
                    {
                        data[i] = 255;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Re:Fract] Failed to remove alpha: {ex}");
                }
            }
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

        // Try to resolve the texture immediately to handle RemoveAlpha using the Unity Instance ID
        var rtAsset = RenderingManager.Instance.RenderTextures.GetAsset(command.RenderTextureId);
        if (rtAsset?.Texture != null && command.IsRemoveAlphaCommand)
        {
            int unityId = rtAsset.Texture.GetInstanceID();
            Debug.Log($"[Re:Fract] Setting RemoveAlpha for Unity Texture {unityId} (Resonite {command.RenderTextureId}) to {command.BoolValue}");
            _removeAlphaCameras[unityId] = command.BoolValue;
        }

        if (!_cameraCache.TryGetValue(command.RenderTextureId, out var cameras) || cameras == null)
        {
            Debug.Log($"[Re:Fract] Camera for {command.RenderTextureId} not in cache or is null. Searching...");
            
            if (rtAsset?.Texture == null)
            {
                // If texture is missing, we can't do anything yet.
                // If it was an alpha command, we also couldn't set the flag, so we wait.
                if (command.IsRemoveAlphaCommand)
                {
                    StartCoroutine(WaitForCameraAndApply(command));
                }
                return;
            }
            Debug.Log($"[Re:Fract] Found render texture asset for {command.RenderTextureId}");

            cameras = FindCamerasRenderingTo(rtAsset.Texture);
            if (cameras.Count == 0)
            {
                Debug.LogWarning($"[Re:Fract] ...but no camera was found. Waiting a few frames...");
                StartCoroutine(WaitForCameraAndApply(command));
                return;
            }
            _cameraCache[command.RenderTextureId] = cameras;
            Debug.Log($"[Re:Fract] Found and cached {cameras.Count} cameras for ID {command.RenderTextureId}");
        }
        else
        {
            Debug.Log($"[Re:Fract] Using cached cameras for ID {command.RenderTextureId}");
            // Ensure alpha flag is set if we used cache (in case rtAsset lookup failed above but cache exists)
            if (command.IsRemoveAlphaCommand && cameras.Count > 0 && cameras[0].targetTexture != null)
            {
                _removeAlphaCameras[cameras[0].targetTexture.GetInstanceID()] = command.BoolValue;
            }
        }

        cameras.RemoveAll(c => c == null);

        if (cameras.Count == 0)
        {
            Debug.LogWarning($"[Re:Fract] ERROR: All cached cameras for {command.RenderTextureId} were destroyed. Removing from cache.");
            if (rtAsset?.Texture != null)
            {
                _removeAlphaCameras.Remove(rtAsset.Texture.GetInstanceID());
            }
            _cameraCache.Remove(command.RenderTextureId);
            return;
        }

        foreach (var camera in cameras)
        {
            EnsurePostProcessVolume(camera);
            if (!command.IsRemoveAlphaCommand)
            {
                ApplyCommand(camera, command);
            }
        }
    }

    private IEnumerator WaitForCameraAndApply(ReFractCommand command)
    {
        for (int i = 0; i < 5; i++)
            yield return null;

        var rtAsset = RenderingManager.Instance.RenderTextures.GetAsset(command.RenderTextureId);
        if (rtAsset?.Texture == null) yield break;

        // Handle Alpha Command late
        if (command.IsRemoveAlphaCommand)
        {
            _removeAlphaCameras[rtAsset.Texture.GetInstanceID()] = command.BoolValue;
        }

        var cameras = FindCamerasRenderingTo(rtAsset.Texture);
        if (cameras.Count > 0)
        {
            _cameraCache[command.RenderTextureId] = cameras;
            Debug.Log($"[Re:Fract] Found {cameras.Count} cameras after waiting.");
            foreach (var camera in cameras)
            {
                EnsurePostProcessVolume(camera);
                if (!command.IsRemoveAlphaCommand)
                {
                    ApplyCommand(camera, command);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Re:Fract] ...still no camera found after waiting.");
        }
    }

    private void EnsurePostProcessVolume(Camera camera)
    {
        var tracker = camera.gameObject.GetComponent<ReFractVolumeTracker>();
        if (tracker == null)
        {
            tracker = camera.gameObject.AddComponent<ReFractVolumeTracker>();
        }
        
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        var layer = camera.gameObject.GetComponent<PostProcessLayer>();
        if (layer != null)
        {
            layer.volumeTrigger = camera.transform;
            int volumeLayerMask = 1 << ignoreRaycastLayer;
            if ((layer.volumeLayer & volumeLayerMask) == 0)
            {
                Debug.Log($"[Re:Fract] PostProcessLayer volumeLayer mask mismatch. Adding layer {ignoreRaycastLayer} (Ignore Raycast).");
                layer.volumeLayer |= volumeLayerMask;
            }
        }

        var legacyVol = camera.GetComponent<PostProcessVolume>();
        if (legacyVol != null) Destroy(legacyVol);
        var legacyCol = camera.GetComponent<BoxCollider>();
        if (legacyCol != null) Destroy(legacyCol);

        Transform child = camera.transform.Find("ReFract_Volume");
        if (child == null)
        {
            GameObject go = new GameObject("ReFract_Volume");
            go.transform.SetParent(camera.transform, false);
            go.layer = ignoreRaycastLayer;
            child = go.transform;
        }

        tracker.VolumeObject = child.gameObject;

        var volume = child.GetComponent<PostProcessVolume>();
        if (volume == null)
        {
            Debug.Log($"[Re:Fract] Creating PostProcessVolume on child of '{camera.name}'.");
            volume = child.gameObject.AddComponent<PostProcessVolume>();
        }

        volume.isGlobal = false;
        var collider = child.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = child.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * 0.01f;
        }

        string uniqueName = $"ReFract_Profile_{camera.GetInstanceID()}";
        if (volume.profile == null)
        {
            Debug.Log($"[Re:Fract] PostProcessVolume on '{camera.name}' has no profile. Creating one and adding all settings.");
            var newProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            newProfile.name = uniqueName;
            volume.profile = newProfile;
            foreach (var type in TypeLookups.Values)
            {
                if (typeof(PostProcessEffectSettings).IsAssignableFrom(type))
                {
                    Debug.Log($"[Re:Fract] Adding setting '{type.Name}' to new profile.");
                    volume.profile.AddSettings(type);
                }
            }
        }
        else if (volume.profile.name != uniqueName)
        {
            Debug.Log($"[Re:Fract] Ensuring unique profile for '{camera.name}'.");
            var newProfile = Instantiate(volume.profile);
            newProfile.name = uniqueName;
            volume.profile = newProfile;
            
            foreach (var type in TypeLookups.Values)
            {
                if (typeof(PostProcessEffectSettings).IsAssignableFrom(type) && !newProfile.HasSettings(type))
                {
                    newProfile.AddSettings(type);
                }
            }
        }

        if (volume.profile.TryGetSettings(out ColorGrading cg))
        {
            cg.gradingMode.value = GradingMode.HighDefinitionRange;
            cg.gradingMode.overrideState = true;
        }
    }

    private void ApplyCommand(Camera camera, ReFractCommand command)
    {
        Debug.Log($"[Re:Fract] Searching for component '{command.ComponentName}' on camera '{camera.name}'");

        object target = null;
        if (!TypeLookups.TryGetValue(command.ComponentName, out Type type))
        {
            Debug.LogWarning($"[Re:Fract] Unsupported Type {command.ComponentName}");
            return;
        }

        if (typeof(PostProcessEffectSettings).IsAssignableFrom(type))
        {
            var volume = camera.GetComponentInChildren<PostProcessVolume>();
            if (volume != null && volume.profile != null)
            {
                foreach (var setting in volume.profile.settings)
                {
                    if (setting.GetType() == type)
                    {
                        target = setting;
                        break;
                    }
                }
                if (target == null)
                    target = volume.profile.AddSettings(type);
            }
        }
        else
        {
            target = camera.GetComponent(type);
        }

        if (target == null)
        {
            Debug.LogWarning($"[Re:Fract] ERROR: Camera '{camera.name}' does not have or could not create component '{command.ComponentName}'");
            return;
        }
        Debug.Log($"[Re:Fract] Found target '{command.ComponentName}'");

        object value = GetValueFromCommand(command);
        var compType = target.GetType();
        
        if (command.ParameterName.EndsWith("!"))
        {
            string propName = command.ParameterName.Substring(0, command.ParameterName.Length - 1);
            Debug.Log($"[Re:Fract] Attempting to set property '{propName}' on '{command.ComponentName} ({compType.FullName})' to value '{value}'");

            var directPropSetter = Introspection.GetPropSetter(compType, propName);
            if (directPropSetter != null)
            {
                directPropSetter(target, value);
                Debug.Log($"[Re:Fract] SUCCESS: Set property '{propName}'.");
            }
            else
            {
                Debug.LogWarning($"[Re:Fract] ERROR: Could not find writable property '{propName}' on component '{command.ComponentName}'");
            }
            return;
        }

        Debug.Log($"[Re:Fract] Attempting to set '{command.ParameterName}' on '{command.ComponentName} ({compType.FullName})' to value '{value}'");

        var field = compType.GetField(command.ParameterName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && typeof(ParameterOverride).IsAssignableFrom(field.FieldType))
        {
            var fieldValue = field.GetValue(target); 
            if (fieldValue != null)
            {
                var paramType = fieldValue.GetType();
                bool valueSet = false;

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

    private static List<UnityEngine.Camera> FindCamerasRenderingTo(RenderTexture target)
    {
        Debug.Log($"[Re:Fract] Searching for RenderTexture {(target ? target.name : "NULL TARGET")} @ {(target ? target.GetInstanceID() : "N/A")}");
        Camera[] allCameras = FindObjectsOfType<Camera>();
        List<UnityEngine.Camera> foundCameras = new List<UnityEngine.Camera>();

        foreach (Camera cam in allCameras)
        {
            Debug.Log($"[Re:Fract] Camera {cam.name} @ {cam.GetInstanceID()} --> {(cam.targetTexture ? cam.targetTexture.name : "NULL TARGET")} @ {(cam.targetTexture ? cam.targetTexture.GetInstanceID() : "N/A")}...");
            if (cam.targetTexture == target)
            {
                foundCameras.Add(cam);
            }
        }

        if (foundCameras.Count == 0) Debug.LogWarning("[Re:Fract] No camera found rendering to the specified RenderTexture.");
        return foundCameras;
    }
}

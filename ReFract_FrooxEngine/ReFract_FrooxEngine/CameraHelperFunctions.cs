using Elements.Core;
using FrooxEngine;
using ReFract.Shared;

namespace ReFract
{
    public static class CameraHelperFunctions
    {
        public static void SetCameraVariable<T>(DynamicVariableSpace space, string camName, string componentName, string paramName, T value)
        {
            if (Plugin._messenger == null) return;
            if (space == null) return;

            // Get the camera's render texture asset ID
            if (!space.TryReadValue(Plugin.DynVarCamKeyString + camName, out Camera cameraRef))
            {
                Plugin.Log.LogWarning($"Re:Fract: Could not find camera variable for {camName}");
                return;
            }

            var camera = cameraRef;
            if (camera == null)
            {
                Plugin.Log.LogWarning($"Re:Fract: Camera for {camName} is null");
                return;
            }

            var renderTexture = camera.RenderTexture.Target;
            if (renderTexture == null)
            {
                Plugin.Log.LogWarning($"Re:Fract: Render texture for camera {camName} is null");
                return;
            }

            var assetId = renderTexture.Asset.AssetId;
            if (assetId == 0)
            {
                Plugin.Log.LogWarning($"Re:Fract: Asset ID for render texture of camera {camName} is 0");
                return;
            }

            var command = new ReFractCommand
            {
                RenderTextureId = assetId,
                CameraName = camName,
                ComponentName = componentName,
                ParameterName = paramName,
            };

            switch (value)
            {
                case int i:
                    command.ValueType = ReFractCommandValueType.Int;
                    command.IntValue = i;
                    break;
                case float f:
                    command.ValueType = ReFractCommandValueType.Float;
                    command.FloatValue = f;
                    break;
                case bool b:
                    command.ValueType = ReFractCommandValueType.Bool;
                    command.BoolValue = b;
                    break;
                case color c:
                    command.ValueType = ReFractCommandValueType.Color;
                    command.ColorValue = new ReFractColor { r = c.r, g = c.g, b = c.b, a = c.a };
                    break;
                case float2 f2:
                    command.ValueType = ReFractCommandValueType.Vector2;
                    command.Vector2Value = new ReFractVector2 { x = f2.x, y = f2.y };
                    break;
                case float4 f4:
                    command.ValueType = ReFractCommandValueType.Vector4;
                    command.Vector4Value = new ReFractVector4 { x = f4.x, y = f4.y, z = f4.z, w = f4.w };
                    break;
                case string s:
                    command.ValueType = ReFractCommandValueType.String;
                    command.StringValue = s;
                    break;
                default:
                    Plugin.Log.LogWarning($"Re:Fract: Unsupported value type for {camName}/{componentName}/{paramName}: {typeof(T)}");
                    return;
            }

            Plugin._messenger.SendObject("SetVariable", command);
        }

        public static void RefreshCameraState(DynamicReferenceVariable<Camera> camVar, Camera camera)
        {
            // TODO: Implement camera state refresh
            Plugin.Log.LogMessage($"Reset {camera.Name} ({camera.ReferenceID})");
        }
    }
}

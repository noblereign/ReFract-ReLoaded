using System.Collections;
using System.Reflection;
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
            Plugin.Log.LogMessage($"Re:Fract: Reset {camera.Name} ({camera.ReferenceID})");
            string name = camVar.VariableName;
            string[] splitName = name.Split('_');

            // We need to use reflection to get the dynamic variable space handler
            var handlerField = camVar.GetType().BaseType.GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            if (handlerField == null)
            {
                Plugin.Log.LogWarning("Re:Fract: Could not find handler field on DynamicReferenceVariable.");
                return;
            }

            var handler = handlerField.GetValue(camVar) as DynamicVariableHandler<Camera>;
            if (handler?.CurrentSpace == null) return;

            // Now we get the dictionary of all dynamic values in the space
            var spaceDictField = typeof(DynamicVariableSpace).GetField("_dynamicValues", BindingFlags.Instance | BindingFlags.NonPublic);
            if (spaceDictField == null)
            {
                Plugin.Log.LogWarning("Re:Fract: Could not find _dynamicValues field on DynamicVariableSpace.");
                return;
            }

            if (spaceDictField.GetValue(handler.CurrentSpace) is not System.Collections.IDictionary spaceDict) return;

            // Iterate over all the dynamic variables and re-apply the ones for our camera
            foreach (var key in spaceDict.Keys)
            {
                var keyName = key.GetType().GetField("name")?.GetValue(key) as string;
                if (keyName == null) continue;

                string[] stringTokens = keyName.Split('_');

                // Check if the variable name matches the Re:Fract camera naming convention
                // Camera variable: ReFract_Cam_<Name>
                // Setting variable: ReFract_<Name>_<Component>_<Setting>
                if (splitName.Length == 3 && splitName[0] == "ReFract" && splitName[1] == "Cam" &&
                    stringTokens.Length == 4 && stringTokens[0] == "ReFract" && stringTokens[1] == splitName[2])
                {
                    var value = spaceDict[key]?.GetType().GetProperty("Value")?.GetValue(spaceDict[key]);
                    if (value == null) continue;

                    Plugin.Log.LogMessage($"Re:Fract: Refreshing '{keyName}' for camera '{stringTokens[1]}'");

                    // We need to use reflection to call the generic SetCameraVariable method with the correct value type
                    var method = typeof(CameraHelperFunctions).GetMethod("SetCameraVariable");
                    if (method == null)
                    {
                        Plugin.Log.LogWarning("Re:Fract: Could not find SetCameraVariable method.");
                        continue;
                    }

                    var genericMethod = method.MakeGenericMethod(value.GetType());
                    genericMethod.Invoke(null, new object[] { handler.CurrentSpace, stringTokens[1], stringTokens[2], stringTokens[3], value });
                }
            }
        }
    }
}

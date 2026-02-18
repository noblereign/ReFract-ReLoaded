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

            var command = new ReFractCommand
            {
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
                    command.ColorValue = c;
                    break;
                case float2 f2:
                    command.ValueType = ReFractCommandValueType.Vector2;
                    command.Vector2Value = f2;
                    break;
                case float4 f4:
                    command.ValueType = ReFractCommandValueType.Vector4;
                    command.Vector4Value = f4;
                    break;
                case string s:
                    command.ValueType = ReFractCommandValueType.String;
                    command.StringValue = s;
                    break;
                default:
                    Plugin.Log.LogWarning($"Re:Fract: Unsupported value type for {camName}/{componentName}/{paramName}: {typeof(T)}");
                    return;
            }
            Plugin.Log.LogMessage($"Re:Fract: Sending {camName}/{componentName}/{paramName}: {typeof(T)}");
            Plugin._messenger.SendObject("SetVariable", command);
        }

        public static void RefreshCameraState(DynamicReferenceVariable<Camera> camVar, Camera camera)
        {
            // TODO: Implement camera state refresh
            Plugin.Log.LogMessage($"Reset {camera.Name} ({camera.ReferenceID})");
        }
    }
}

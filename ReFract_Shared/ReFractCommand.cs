using Elements.Core;
using InterprocessLib;
using Renderite.Shared;

namespace ReFract.Shared
{
    public enum ReFractCommandValueType : byte
    {
        Int,
        Float,
        Bool,
        Color,
        Vector2,
        Vector4,
        String
    }

    public class ReFractCommand : RendererCommand
    {
        public string CameraName = "";
        public string ComponentName = "";
        public string ParameterName = "";
        public ReFractCommandValueType ValueType;
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public color ColorValue;
        public float2 Vector2Value;
        public float4 Vector4Value;
        public string StringValue = "";

        public override void Pack(ref MemoryPacker packer)
        {
            packer.Write(CameraName);
            packer.Write(ComponentName);
            packer.Write(ParameterName);
            packer.Write((byte)ValueType);

            switch (ValueType)
            {
                case ReFractCommandValueType.Int:
                    packer.Write(IntValue);
                    break;
                case ReFractCommandValueType.Float:
                    packer.Write(FloatValue);
                    break;
                case ReFractCommandValueType.Bool:
                    packer.Write(BoolValue);
                    break;
                case ReFractCommandValueType.Color:
                    packer.Write(ColorValue);
                    break;
                case ReFractCommandValueType.Vector2:
                    packer.Write(Vector2Value);
                    break;
                case ReFractCommandValueType.Vector4:
                    packer.Write(Vector4Value);
                    break;
                case ReFractCommandValueType.String:
                    packer.Write(StringValue);
                    break;
            }
        }

        public override void Unpack(ref MemoryUnpacker unpacker)
        {
            unpacker.Read(ref CameraName);
            unpacker.Read(ref ComponentName);
            unpacker.Read(ref ParameterName);
            unpacker.Read(ref ValueType);

            switch (ValueType)
            {
                case ReFractCommandValueType.Int:
                    unpacker.Read(ref IntValue);
                    break;
                case ReFractCommandValueType.Float:
                    unpacker.Read(ref FloatValue);
                    break;
                case ReFractCommandValueType.Bool:
                    unpacker.Read(ref BoolValue);
                    break;
                case ReFractCommandValueType.Color:
                    unpacker.Read(ref ColorValue);
                    break;
                case ReFractCommandValueType.Vector2:
                    unpacker.Read(ref Vector2Value);
                    break;
                case ReFractCommandValueType.Vector4:
                    unpacker.Read(ref Vector4Value);
                    break;
                case ReFractCommandValueType.String:
                    unpacker.Read(ref StringValue);
                    break;
            }
        }
    }
}

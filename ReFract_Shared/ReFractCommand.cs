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

    public struct ReFractColor
    {
        public float r, g, b, a;
        public override string ToString() => $"({r}, {g}, {b}, {a})";
    }

    public struct ReFractVector2
    {
        public float x, y;
        public override string ToString() => $"({x}, {y})";
    }

    public struct ReFractVector4
    {
        public float x, y, z, w;
        public override string ToString() => $"({x}, {y}, {z}, {w})";
    }

    public class ReFractCommand : RendererCommand
    {
        public int RenderTextureId;
        public bool IsRemoveAlphaCommand; // New flag
        public string CameraName = "";
        public string ComponentName = "";
        public string ParameterName = "";
        public ReFractCommandValueType ValueType;
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public ReFractColor ColorValue;
        public ReFractVector2 Vector2Value;
        public ReFractVector4 Vector4Value;
        public string StringValue = "";

        public override void Pack(ref MemoryPacker packer)
        {
            packer.Write(RenderTextureId);
            packer.Write(IsRemoveAlphaCommand); // Packing the new flag
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
                    packer.Write(ColorValue.r);
                    packer.Write(ColorValue.g);
                    packer.Write(ColorValue.b);
                    packer.Write(ColorValue.a);
                    break;
                case ReFractCommandValueType.Vector2:
                    packer.Write(Vector2Value.x);
                    packer.Write(Vector2Value.y);
                    break;
                case ReFractCommandValueType.Vector4:
                    packer.Write(Vector4Value.x);
                    packer.Write(Vector4Value.y);
                    packer.Write(Vector4Value.z);
                    packer.Write(Vector4Value.w);
                    break;
                case ReFractCommandValueType.String:
                    packer.Write(StringValue);
                    break;
            }
        }

        public override void Unpack(ref MemoryUnpacker unpacker)
        {
            unpacker.Read(ref RenderTextureId);
            unpacker.Read(ref IsRemoveAlphaCommand); // Unpacking the new flag
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
                    unpacker.Read(ref ColorValue.r);
                    unpacker.Read(ref ColorValue.g);
                    unpacker.Read(ref ColorValue.b);
                    unpacker.Read(ref ColorValue.a);
                    break;
                case ReFractCommandValueType.Vector2:
                    unpacker.Read(ref Vector2Value.x);
                    unpacker.Read(ref Vector2Value.y);
                    break;
                case ReFractCommandValueType.Vector4:
                    unpacker.Read(ref Vector4Value.x);
                    unpacker.Read(ref Vector4Value.y);
                    unpacker.Read(ref Vector4Value.z);
                    unpacker.Read(ref Vector4Value.w);
                    break;
                case ReFractCommandValueType.String:
                    unpacker.Read(ref StringValue);
                    break;
            }
        }
    }
}

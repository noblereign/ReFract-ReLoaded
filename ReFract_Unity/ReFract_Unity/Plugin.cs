using BepInEx;
using HarmonyLib;
using InterprocessLib;
using ReFract.Shared;
using Renderite.Unity;

namespace ReFract.Unity;

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private Messenger _msg;
    
	void Awake()
	{
		var harmony = new Harmony("dog.glacier.ReFractUnity");
		harmony.PatchAll();
        _msg = new Messenger("dog.glacier.ReFract", [typeof(ReFractCommand)]);
        try
        {
            _msg.ReceiveObject<ReFractCommand>("SetVariable", HandleSetVariable);
        }
        catch (TypeLoadException ex)
        {
            Console.WriteLine($"[Re:Fract] Failed to register receiver. This is likely a DLL version mismatch: {ex}");
        }
    }

    private void HandleSetVariable(ReFractCommand command)
    {
        Console.WriteLine($"Received command for camera {command.CameraName}:");
        Console.WriteLine($"  Component: {command.ComponentName}");
        Console.WriteLine($"  Parameter: {command.ParameterName}");

        switch (command.ValueType)
        {
            case ReFractCommandValueType.Int:
                Console.WriteLine($"  Value: {command.IntValue}");
                break;
            case ReFractCommandValueType.Float:
                Console.WriteLine($"  Value: {command.FloatValue}");
                break;
            case ReFractCommandValueType.Bool:
                Console.WriteLine($"  Value: {command.BoolValue}");
                break;
            case ReFractCommandValueType.Color:
                Console.WriteLine($"  Value: {command.ColorValue}");
                break;
            case ReFractCommandValueType.Vector2:
                Console.WriteLine($"  Value: {command.Vector2Value}");
                break;
            case ReFractCommandValueType.Vector4:
                Console.WriteLine($"  Value: {command.Vector4Value}");
                break;
            case ReFractCommandValueType.String:
                Console.WriteLine($"  Value: {command.StringValue}");
                break;
        }
    }
}
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using InterprocessLib;
using ReFract.Shared;
using Renderite.Unity;

namespace ReFract.Unity;

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource? Log;
    private Messenger _msg;
    
	void Awake()
	{
		Log = base.Logger;

		var harmony = new Harmony("dog.glacier.ReFractUnity");
		harmony.PatchAll();
        _msg = new Messenger("dog.glacier.ReFract", [typeof(ReFractCommand)]);
        _msg.ReceiveObject<ReFractCommand>("SetVariable", HandleSetVariable);
    }

    private void HandleSetVariable(ReFractCommand command)
    {
        Log.LogInfo($"Received command for camera {command.CameraName}:");
        Log.LogInfo($"  Component: {command.ComponentName}");
        Log.LogInfo($"  Parameter: {command.ParameterName}");

        switch (command.ValueType)
        {
            case ReFractCommandValueType.Int:
                Log.LogInfo($"  Value: {command.IntValue}");
                break;
            case ReFractCommandValueType.Float:
                Log.LogInfo($"  Value: {command.FloatValue}");
                break;
            case ReFractCommandValueType.Bool:
                Log.LogInfo($"  Value: {command.BoolValue}");
                break;
            case ReFractCommandValueType.Color:
                Log.LogInfo($"  Value: {command.ColorValue}");
                break;
            case ReFractCommandValueType.Vector2:
                Log.LogInfo($"  Value: {command.Vector2Value}");
                break;
            case ReFractCommandValueType.Vector4:
                Log.LogInfo($"  Value: {command.Vector4Value}");
                break;
            case ReFractCommandValueType.String:
                Log.LogInfo($"  Value: {command.StringValue}");
                break;
        }
    }
}
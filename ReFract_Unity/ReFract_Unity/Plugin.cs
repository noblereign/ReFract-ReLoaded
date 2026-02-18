using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Renderite.Unity;
using Valve.VR;

namespace ReFract_Unity;

[BepInPlugin("dog.glacier.ReFractUnity", "Re:Fract // Reloaded (for Unity)", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource? Log;

	void Awake()
	{
		Log = base.Logger;

		var harmony = new Harmony("dog.glacier.ReFractUnity");
		harmony.PatchAll();
	}
}

[HarmonyPatch(typeof(RenderingManager), "UpdateVR_Active")]
class Patch
{
	static bool Prefix(bool vrActive)
	{
		SteamVR.instance?.compositor.SuspendRendering(!vrActive);
		return true;
	}
}
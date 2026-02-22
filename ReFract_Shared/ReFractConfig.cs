using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;

namespace ReFract.Shared;

class ReFractConfig
{
    // We define our config variables in a public scope
    public readonly ConfigEntry<bool> debugLogging;
    public readonly ConfigEntry<bool> forceRemoveAlpha;

    public ReFractConfig(ConfigFile cfg)
    {
        cfg.SaveOnConfigSet = false;

        debugLogging = cfg.Bind(
            "General",                          // Config section
            "Debug logging",                     // Key of this config
            false,                    // Default value
            "Whether or not to print debug logging. This can make your log files grow incredibly quickly, only enable it if you really need it"    // Description
        );
        forceRemoveAlpha = cfg.Bind(
            "General",                          // Config section
            "Always reset alpha channel",                     // Key of this config
            false,                    // Default value
            "By default, Re:Fract embeds the depth data of photos in the alpha channel. If you're using a camera without the option to disable that behavior, you can force it to happen here."    // Description
        );

        ClearOrphanedEntries(cfg);
        cfg.Save();
        cfg.SaveOnConfigSet = true;
    }
    static void ClearOrphanedEntries(ConfigFile cfg)
    {
        // Find the private property `OrphanedEntries` from the type `ConfigFile` //
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        // And get the value of that property from our ConfigFile instance //
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        // And finally, clear the `OrphanedEntries` dictionary //
        orphanedEntries.Clear();
    }
}
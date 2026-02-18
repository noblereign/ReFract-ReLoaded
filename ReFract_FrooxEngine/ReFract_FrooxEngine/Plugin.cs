using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using BepisResoniteWrapper;
using FrooxEngine;
using HarmonyLib;
using InterprocessLib;
using ReFract.Shared;
using System.Collections;
using System.Reflection;
using System.Xml.Linq;

// a lot of this code will be based on https://github.com/Zozokasu/ResoniteReFract and of course, https://github.com/yoshiyoshyosh/ReFract (as that's the latest updated fork of it)

namespace ReFract;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log = null!;
    public static Messenger? _messenger;

    public static string DynVarKeyString => "Re.Fract_";
    public static string DynVarCamKeyString => "Re.Fract_Camera_";
    public static string ReFractTag => "Re:FractCameraSpace";

    public override void Load()
    {
        Log = base.Log;
        ResoniteHooks.OnEngineReady += OnEngineReady;
        Log.LogInfo($"Plugin {PluginMetadata.GUID} is preparing interprocess");
        _messenger = new Messenger("dog.glacier.ReFract", [typeof(ReFractCommand)]);
        Log.LogInfo($"Plugin {PluginMetadata.GUID} is loaded!");
    }

    private void OnEngineReady()
    {
        try
        {
            Harmony harmony = new Harmony("dog.glacier.ReFract");
            harmony.PatchAll();
            Log.LogInfo("Harmony patches installed.");
            var patches = Harmony.GetAllPatchedMethods();
            Log.LogInfo("=== Patched Methods ===");
            foreach (var method in patches)
            {
                Log.LogInfo($"Patched: {method.DeclaringType?.FullName}.{method.Name}");
                var patchInfo = Harmony.GetPatchInfo(method);
                if (patchInfo != null)
                {
                    Log.LogInfo($"  Prefixes: {patchInfo.Prefixes.Count}");
                    Log.LogInfo($"  Postfixes: {patchInfo.Postfixes.Count}");
                }
            }
            Log.LogInfo("=== End of Patched Methods ===");

            Log.LogInfo("Harmony patches installed.");
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to install patches: {ex}");
        }
    }

    [HarmonyPatch]
    public class DynamicVariableBase_Patch
    {
        // Patching ComponentBase because harmony A) Sucks at patching generic methods, and B) The entry component I had to target doesn't override OnStart
        // Actually patching Component because ComponentBase wasnt working well for some reason???? like it'd catch a couple at the start but then never again 😭 - Noble
        [HarmonyPatch(typeof(Component), "Initialize")]
        [HarmonyPostfix]
        public static void ParentReferencePostfix(Component __instance)
        {
            // Check if the component is a dynamic value variable
            Type t = __instance.GetType();
            if (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(DynamicValueVariable<>))
            {
                // Get the field reference for the "Value" field on the variable
                FieldInfo? field = t.GetField("Value", BindingFlags.Instance | BindingFlags.Public);
                if (field == null)
                    return;

                // Get the IField for it
                IField Value = (IField)field.GetValue(__instance);

                // Get the handler field for the dynamic variable (the meat of dynvars)
                var handlerField = t.BaseType.GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);

                // i'm sorry but i'm doing this with reflection twin
                var genericType = t.GetGenericArguments()[0];
                var dynvarCamSetter = typeof(DynamicVariableBase_Patch).GetMethod("DynvarCamSetter").MakeGenericMethod(genericType);

                // Subscribe to the Changed event on the value field.
                // I sorely wish I could have just patched the function in the variable space that handles all changes variables send ;_;
                Value.Changed += (IChangeable c) => {
                    var handler = handlerField.GetValue(__instance);
                    dynvarCamSetter.Invoke(null, new object[] { __instance, handler });
                };
                Log.LogDebug($"Re:Fract: Found DynamicValueVariable {__instance.Name} ({__instance.WorkerType} @ {__instance.ReferenceID})");
            }
            // Specifically also check if it's a camera variable
            else if (t == typeof(DynamicReferenceVariable<Camera>))
            {
                Log.LogDebug($"Re:Fract: Found Camera Variable {__instance.Name} ({__instance.WorkerType} @ {__instance.ReferenceID})");
                // Get the Reference fieldinfo
                FieldInfo? field = t.GetField("Reference", BindingFlags.Instance | BindingFlags.Public);
                if (field == null) return;
                
                // Get the actual syncref
                ISyncRef Reference = (ISyncRef)field.GetValue(__instance);
                DynamicReferenceVariable<Camera>? camVar = __instance as DynamicReferenceVariable<Camera>;
                // Store the name of the variable for later
                string? CamName = camVar?.VariableName.Value;

                Log.LogDebug($"Re:Fract: {__instance.Name} ({__instance.ReferenceID}) camera name: {CamName} ");

                // I *should* be okay not unsubbing these events since the object they're attached to gets destroyed (and thus the object listening to these events)
                Reference.Changed += (IChangeable c) => {
                    string? Name = camVar?.VariableName.Value;
                    SyncRef<Camera>? refVar = c as SyncRef<Camera>;

                    // Handler delegate that can unsubscribe itself
                    // This'll be used for when a camera is dropped into the dynvar.
                    // Will be used to subscribe to changed events on the camera's "PostProcessing" checkbox and the camera's slot's "Active" checkbox, and refreshes the camera whenever they become active.
                    Action<IChangeable>? handler = null;
                    handler = delegate(IChangeable c2)
                    {
                        if (c2 is Sync<bool> active && refVar != null && refVar.Target != null && camVar != null)
                        {
                            Log.LogDebug($"Re:Fract : Camera \"{Name}\" is {(active ? "active" : "inactive")}!");
                            if (active)
                            {
                                Engine.Current.WorldManager.FocusedWorld.RunInUpdates(2, () => CameraHelperFunctions.RefreshCameraState(camVar, refVar.Target as Camera));
                            }
                        }
                        else if (c2 is Sync<bool> active2)
                        {
                            Log.LogDebug($"Re:Fract : Camera \"{Name}\" is no longer valid, unsubscribing!");
                            active2.Changed -= handler;
                        }
                    };
                    // If the camera is dropped into the dynvar, refresh it, then subscribe to the aforementioned changed events.
                    if (camVar != null && Name != null && Name.StartsWith(DynVarCamKeyString) && refVar != null && refVar.Target != null)
                    {
                        CameraHelperFunctions.RefreshCameraState(camVar, refVar.Target as Camera);
                        Log.LogDebug($"Re:Fract : Camera \"{Name}\" updated!");

                        // Just to be extra safe ;) (Though I am under no illusions that juggling events is uh... not great)
                        Sync<bool> cameraSlotActive = refVar.Target.Slot.ActiveSelf_Field;
                        Sync<bool> cameraPostProcessActive = refVar.Target.Postprocessing;

                        var ev = typeof(SyncElement).GetEvent("Changed", BindingFlags.Public | BindingFlags.Instance);
                        var fi = typeof(SyncElement).GetField(ev.Name, AccessTools.all);

                        // Make sure we aren't already subscribed so we don't add duplicate subscriptions.
                        Delegate? del = fi.GetValue(refVar.Target.Slot.ActiveSelf_Field) as Delegate;
                        if (del == null || !del.HasSubscriber(handler))
                        {
                            refVar.Target.Slot.ActiveSelf_Field.Changed += handler;
                            Log.LogDebug($"Re:Fract : Camera \"{Name}\" subscribed to slot active state!");
                        }

                        del = fi.GetValue(refVar.Target.Postprocessing) as Delegate;
                        if (del == null || !del.HasSubscriber(handler))
                        {
                            refVar.Target.Postprocessing.Changed += handler;
                            Log.LogDebug($"Re:Fract : Camera \"{Name}\" subscribed to postprocessing state!");
                        }
                    }
                };
                // Whenever an existing variable starts, refresh the camera state. The camera state has to be update like all the time, otherwise it'll be reset
                if (camVar != null && CamName != null && CamName.StartsWith(DynVarCamKeyString))
                {
                    string[] splitName = CamName.Split('_');
                    if (splitName.Length == 3)
                    {
                        // Wait for any variable spaces and such to initialize
                        __instance.World.RunInUpdates(3, () => {
                            Log.LogDebug("Re:Fract : Starting " + splitName[2]);
                            CameraHelperFunctions.RefreshCameraState(camVar, camVar.Reference.Target);
                            camVar.Reference.SyncElementChanged(camVar.Reference);
                        });
                    }
                }
            }
        }

        // Function that gets called whenever a dynamic value changes so it can set a post processing option on the camera
        public static void DynvarCamSetter<T>(DynamicVariableBase<T> __instance, DynamicVariableHandler<T> handler)
        {
            string Name = __instance.VariableName;

            //Log.LogDebug("Re:Fract: DynamicVariableBase_Patch: " + Name + ": " + typeof(T).Name);
            if (Name != null && Name.StartsWith(DynVarKeyString))
            {
                T Value = __instance.DynamicValue;
                if (Value == null)
                    return;

                DynamicVariableSpace Space = handler.CurrentSpace;

                string[] camParams = Name.Split('_');
                if (camParams.Length == 4)
                {
                    string camName = camParams[1];
                    string componentName = camParams[2];
                    string paramName = camParams[3];
                    CameraHelperFunctions.SetCameraVariable(Space, camName, componentName, paramName, Value);
                }
            }
        }
    }

}

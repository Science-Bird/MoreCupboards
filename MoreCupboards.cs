using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreCupboards
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class MoreCupboards : BaseUnityPlugin
    {
        public static MoreCupboards Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static PluginInfo pluginInfo;

        public static ConfigFile config;
        public static ConfigEntry<int> cupboardPrice;
        public static ConfigEntry<int> maximumCupboards;
        public static ConfigEntry<bool> noDoors;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            pluginInfo = Info;
            cupboardPrice = base.Config.Bind("General", "Cupboard Price", 100, "How much it costs to buy cupboards from the store.");
            maximumCupboards = base.Config.Bind("General", "Maximum Cupboards", 5, new ConfigDescription("Maximum extra cupboards you're allowed to buy for your ship. This is in addition to the vanilla cupboard, so the actual number of cupboards you can have on the ship will be one higher than this.", new AcceptableValueRange<int>(1, 5)));
            noDoors = base.Config.Bind("General", "No Doors", false, "Remove doors from purchased cupboards (does not affect vanilla cupboard).");

            AddCupboards.RegisterCupboard();

            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}

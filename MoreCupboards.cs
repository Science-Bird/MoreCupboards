using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreCupboards
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]// ScienceBird.MoreCupboards, MoreCupboards
    [BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mattymatty.MattyFixes", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("TerminalFormatter", BepInDependency.DependencyFlags.SoftDependency)]

    public class MoreCupboards : BaseUnityPlugin
    {
        public static MoreCupboards Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }

        public static AssetBundle CupboardAssets;

        public static PluginInfo pluginInfo;

        public static ConfigFile config;
        public static ConfigEntry<int> cupboardPrice, maximumCupboards;
        public static ConfigEntry<bool> noDoors, separateCupboardEntries, useColourNames, autoParent;

        public static bool mattyPresent = false;
        public static bool mrovPresent = false;

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            pluginInfo = Info;
            cupboardPrice = base.Config.Bind("General", "Cupboard Price", 250, "How much it costs to buy cupboards from the store.");
            maximumCupboards = base.Config.Bind("General", "Maximum Cupboards", 5, new ConfigDescription("Maximum extra cupboards you're allowed to buy for your ship. This is in addition to the vanilla cupboard, so the actual number of cupboards you can have on the ship will be one higher than this.", new AcceptableValueRange<int>(1, 5)));
            noDoors = base.Config.Bind("General", "No Doors", false, "Remove doors from purchased cupboards (does not affect vanilla cupboard).");
            separateCupboardEntries = base.Config.Bind("Terminal", "Separate Cupboard Entries", false, "Rather than the shop listing a single cupboard, it and the storage menu will list numbered cupboards (1Cupboard, 2Cupboard, etc.) so you can request specific cupboards (if you're experiencing unusal behaviour in the terminal, try enabling this).");
            useColourNames = base.Config.Bind("Terminal", "Use Colour Names", false, "If using the above separate entries option, instead of names like 1Cupboard and 2Cupboard, they will be named based on their colour, like Orange Cupboard and Yellow Cupboard.");
            autoParent = base.Config.Bind("Technical", "Auto Parent", true, "If Matty Fixes is found, attempt to parent items to cupboards (only try turning this off if you're having any major errors or other issues with the mod).");

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            CupboardAssets = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "morecupboardsassets"));

            AddCupboards.RegisterCupboards();

            Patch();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            bool doLobbyCompat = Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility");
            mattyPresent = Chainloader.PluginInfos.ContainsKey("mattymatty.MattyFixes");
            mrovPresent = Chainloader.PluginInfos.ContainsKey("TerminalFormatter");

            if (doLobbyCompat)
            {
                LobbyCompatibility.RegisterCompatibility();
            }

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

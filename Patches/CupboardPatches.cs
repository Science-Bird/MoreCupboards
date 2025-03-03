using System.Reflection;
using System;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using static UnityEngine.Rendering.DebugUI;

namespace MoreCupboards.Patches
{
    [HarmonyPatch]
    class InteractSurfaceCopy
    {
        internal static bool storeFlag = false;
        public static bool mrovPresent = false;
        public static bool getData = true;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void DestroyCupboardDoors(StartOfRound __instance, string sceneName)
        {
            getData = true;
            if (sceneName == "SampleSceneRelay" && MoreCupboards.noDoors.Value)
            {
                GameObject vanillaCupboard = GameObject.Find("StorageCloset");
                Animator[] animators = vanillaCupboard.GetComponentsInChildren<Animator>();
                foreach (Animator animator in animators)
                {
                    UnityEngine.Object.Destroy(animator.gameObject);
                }
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "TerminalFormatter")
                {
                    mrovPresent = true;
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPrefix]
        [HarmonyBefore("mrov.terminalformatter")]
        static void TerminalTextCheck(Terminal __instance, TerminalNode node)
        {
            if (MoreCupboards.separateCupboardEntries.Value)
            {
                return;
            }
            if (node.displayText.Contains("[buyableItemsList]"))
            {
                storeFlag = true;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void TerminalTextOverride(Terminal __instance)
        {
            if (MoreCupboards.separateCupboardEntries.Value)
            {
                return;
            }
            string extraSpace = "";
            if (mrovPresent && storeFlag)
            {
                extraSpace = " ";
            }
            for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
            {
                string name = i.ToString() + "Cupboard";
                if (__instance.currentText.Contains(name))
                {
                    __instance.currentText = __instance.currentText.Replace(name, "Cupboard" + extraSpace);
                }
            }
            if (storeFlag)
            {
                int count = __instance.currentText.Split("Cupboard").Length - 1;
                if (MoreCupboards.maximumCupboards.Value > 1 && __instance.currentText.IndexOf("Cupboard") > 0 && count > 1)
                {
                    string cupboardSubstring1 = __instance.currentText.Substring(__instance.currentText.IndexOf("Cupboard"));
                    string cupboardSubstring2 = cupboardSubstring1.Substring(cupboardSubstring1.IndexOf("\n") + 2);
                    string cupboardSubstring3 = cupboardSubstring2.Substring(cupboardSubstring2.LastIndexOf("Cupboard"));
                    string cupboardSubstring4 = cupboardSubstring2.Substring(0, cupboardSubstring3.IndexOf("\n") + cupboardSubstring2.LastIndexOf("Cupboard") + 2);
                    if (count == 2)
                    {
                        __instance.currentText = __instance.currentText.Remove(__instance.currentText.LastIndexOf(cupboardSubstring4), cupboardSubstring4.Length);
                    }
                    else
                    {
                        __instance.currentText = __instance.currentText.Replace(cupboardSubstring4, "");
                    }
                }
                storeFlag = false;
            }
            __instance.screenText.text = __instance.currentText;
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnSubmit))]
        [HarmonyPrefix]
        static void CupboardCommandOverride(Terminal __instance)
        {
            if (MoreCupboards.separateCupboardEntries.Value)
            {
                return;
            }
            string command = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            if (command.ToLower() == "cupboard" || command.ToLower().IndexOf("cupb") == 0 || command.ToLower().IndexOf("cupb") == 1)
            {
                command = "cupboard";
                UnlockableItem[] unlockableList = StartOfRound.Instance.unlockablesList.unlockables.ToArray();
                int storedCupboard = 0;
                int newCupboard = 0;
                for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
                {
                    string name = i.ToString() + "Cupboard";
                    foreach (UnlockableItem unlockable in unlockableList)
                    {
                        if (unlockable.unlockableName == "Cupboard" && unlockable.inStorage)
                        {
                            storedCupboard = -1;
                            break;
                        }
                        else if (unlockable.unlockableName == name && unlockable.inStorage)
                        {
                            storedCupboard = i;
                            break;
                        }
                    }
                    if (newCupboard == 0 && storedCupboard == 0)
                    {
                        foreach (UnlockableItem unlockable in unlockableList)
                        {
                            if (unlockable.unlockableName == name && !unlockable.hasBeenUnlockedByPlayer)
                            {
                                newCupboard = i;
                                break;
                            }
                        }
                    }
                    if (storedCupboard != 0)
                    {
                        break;
                    }
                }
                if (storedCupboard != 0)
                {
                    if (storedCupboard != -1)
                    {
                        MoreCupboards.Logger.LogInfo($"Retrieving cupboard{storedCupboard}");
                        command = storedCupboard.ToString() + command;
                    }
                }
                else if (newCupboard != 0)
                {
                    MoreCupboards.Logger.LogInfo($"Buying cupboard{newCupboard}");
                    command = newCupboard.ToString() + command;
                }
                else
                {
                    MoreCupboards.Logger.LogInfo("Unable to find cupboards!");
                    return;
                }
                if (storedCupboard != -1)
                {
                    __instance.screenText.text = __instance.screenText.text.Substring(0, __instance.screenText.text.Length - __instance.textAdded) + command;
                }
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.StoreObjectLocalClient))]
        [HarmonyPrefix]
        static void UnparentItemsOnStore(ShipBuildModeManager __instance)
        {
            if (__instance.timeSincePlacingObject <= 0.25f || !__instance.InBuildMode || __instance.placingObject == null || !StartOfRound.Instance.unlockablesList.unlockables[__instance.placingObject.unlockableID].canBeStored)
            {
                return;
            }
            UnlockableItem unlockable = StartOfRound.Instance.unlockablesList.unlockables[__instance.placingObject.unlockableID];
            if (unlockable.unlockableName.Contains("Cupboard"))
            {
                StartOfRound playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
                GrabbableObject[]? shelvedObjects = __instance.placingObject?.parentObject?.gameObject?.GetComponentsInChildren<GrabbableObject>();
                foreach (GrabbableObject shelfObject in shelvedObjects)
                {
                    shelfObject.parentObject = null;
                    shelfObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
                }
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.StoreObjectServerRpc))]
        [HarmonyPrefix]
        static void UnparentItemsOnStoreServer(ShipBuildModeManager __instance, NetworkObjectReference objectRef)
        {
            NetworkObjectReference tempRef = objectRef;
            if (!tempRef.TryGet(out var networkObject))
            {
                return;
            }
            PlaceableShipObject placeObject = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
            if (StartOfRound.Instance.unlockablesList.unlockables[placeObject.unlockableID].unlockableName.Contains("Cupboard"))
            {
                StartOfRound playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
                GrabbableObject[]? shelvedObjects = placeObject?.parentObject?.gameObject?.GetComponentsInChildren<GrabbableObject>();
                foreach (GrabbableObject shelfObject in shelvedObjects)
                {
                    shelfObject.parentObject = null;
                    shelfObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
                }
            }
        }

        [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.StoreShipObjectClientRpc))]
        [HarmonyPrefix]
        static void UnparentItemsOnStoreClient(ShipBuildModeManager __instance, NetworkObjectReference objectRef, int playerWhoStored)
        {
            if (NetworkManager.Singleton == null || __instance.NetworkManager.ShutdownInProgress || __instance.IsServer || playerWhoStored == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
            {
                return;
            }
            NetworkObjectReference tempRef = objectRef;
            if (tempRef.TryGet(out var networkObject))
            {
                PlaceableShipObject placeObject = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
                if (StartOfRound.Instance.unlockablesList.unlockables[placeObject.unlockableID].unlockableName.Contains("Cupboard"))
                {
                    StartOfRound playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
                    GrabbableObject[]? shelvedObjects = placeObject?.parentObject?.gameObject?.GetComponentsInChildren<GrabbableObject>();
                    foreach (GrabbableObject shelfObject in shelvedObjects)
                    {
                        shelfObject.parentObject = null;
                        shelfObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ParentCupboardsOnLoad(StartOfRound __instance)
        {
            MoreCupboards.Logger.LogDebug("Parenting existing cupboards to ship...");
            GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
            for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
            {
                GameObject customCupboard = GameObject.Find($"StorageCloset{i}(Clone)");
                GameObject customCupboardAlt = GameObject.Find($"StorageCloset{i}Alt(Clone)");
                if (customCupboard != null && hangarShip != null)
                {
                    customCupboard.transform.SetParent(hangarShip.transform, worldPositionStays: true);
                }
                else if (customCupboardAlt != null && hangarShip != null)
                {
                    customCupboardAlt.transform.SetParent(hangarShip.transform, worldPositionStays: true);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        [HarmonyPostfix]
        static void ParentNewCupboard(StartOfRound __instance, int unlockableIndex)
        {
            UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[unlockableIndex];
            if (unlockableItem.unlockableName.Contains("Cupboard") && unlockableItem.unlockableName != "Cupboard")
            {
                MoreCupboards.Logger.LogDebug("Parenting cupboard to ship...");
                int indexNumber = int.Parse(unlockableItem.unlockableName.Replace("Cupboard", ""));

                GameObject hangarShip = GameObject.Find("/Environment/HangarShip");
                GameObject customCupboard = GameObject.Find($"StorageCloset{indexNumber}(Clone)");
                GameObject customCupboardAlt = GameObject.Find($"StorageCloset{indexNumber}Alt(Clone)");
                if (customCupboard != null && hangarShip != null)
                {
                    customCupboard.transform.SetParent(hangarShip.transform, worldPositionStays: true);
                }
                else if (customCupboardAlt != null && hangarShip != null)
                {
                    customCupboardAlt.transform.SetParent(hangarShip.transform, worldPositionStays: true);
                }
                
            }
            
        }
    }
}

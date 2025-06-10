using System.Reflection;
using System;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;

namespace MoreCupboards.Patches
{
    [HarmonyPatch]
    class CupboardPatches
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void DestroyCupboardDoors(StartOfRound __instance, string sceneName)
        {
            if (sceneName == "SampleSceneRelay" && MoreCupboards.noDoors.Value)
            {
                GameObject vanillaCupboard = GameObject.Find("StorageCloset");
                Animator[] animators = vanillaCupboard.GetComponentsInChildren<Animator>();
                foreach (Animator animator in animators)
                {
                    if (animator.gameObject.name == "Cube.000" || animator.gameObject.name == "Cube.002")
                    {
                        UnityEngine.Object.Destroy(animator.gameObject);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPrefix]
        [HarmonyBefore("TerminalFormatter")]
        static void TerminalTextCheck(Terminal __instance, TerminalNode node, out bool __state)
        {
            __state = false;
            if (MoreCupboards.separateCupboardEntries.Value && !MoreCupboards.useColourNames.Value)
            {
                return;
            }
            if (node.displayText.Contains("[buyableItemsList]"))
            {
                __state = true;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("TerminalFormatter")]
        static void TerminalTextOverride(Terminal __instance, bool __state)
        {
            if (MoreCupboards.separateCupboardEntries.Value)
            {
                if (MoreCupboards.useColourNames.Value)
                {
                    __instance.currentText = __instance.currentText.Replace("1Cupboard", "Orange Cupboard").Replace("2Cupboard", "Yellow Cupboard").Replace("3Cupboard", "Green Cupboard").Replace("4Cupboard", "Blue Cupboard").Replace("5Cupboard", "Purple Cupboard");
                    __instance.screenText.text = __instance.currentText;
                }
                return;
            }
            string extraSpace = "";
            if (MoreCupboards.mrovPresent && __state)
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
            if (__state)
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
            }
            __instance.screenText.text = __instance.currentText;
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.OnSubmit))]
        [HarmonyPrefix]
        static void CupboardCommandOverride(Terminal __instance)
        {
            string command = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded).ToLower();
            if (MoreCupboards.separateCupboardEntries.Value)
            {
                if (MoreCupboards.useColourNames.Value && command.Contains("cupb"))
                {
                    if (command.StartsWith("orange cupb"))
                    {
                        command = "1Cupboard";
                    }
                    else if (command.StartsWith("yellow cupb"))
                    {
                        command = "2Cupboard";
                    }
                    else if(command.StartsWith("green cupb"))
                    {
                        command = "3Cupboard";
                    }
                    else if(command.StartsWith("blue cupb"))
                    {
                        command = "4Cupboard";
                    }
                    else if(command.StartsWith("purple cupb"))
                    {
                        command = "5Cupboard";
                    }
                    __instance.screenText.text = __instance.screenText.text.Substring(0, __instance.screenText.text.Length - __instance.textAdded) + command;
                }
                return;
            }

            if (command == "cupboard" || command.IndexOf("cupb") == 0 || command.IndexOf("cupb") == 1)
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
                try
                {
                    MoreCupboards.Logger.LogDebug("Parenting cupboard to ship...");
                    int indexNumber = string.IsNullOrEmpty(unlockableItem.unlockableName.Replace("Cupboard", "")) ? 0 : int.Parse(unlockableItem.unlockableName.Replace("Cupboard", ""));
                    if (indexNumber <= MoreCupboards.maximumCupboards.Value && indexNumber > 0)
                    {
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
                catch (FormatException)
                {
                    MoreCupboards.Logger.LogWarning("Incorrect cupboard name string passed!");
                }
            }
            
        }
    }
}

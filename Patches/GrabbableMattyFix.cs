using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using static MoreCupboards.Patches.CupboardMattyFix;

namespace MoreCupboards.Patches;

[HarmonyPatch]
internal class GrabbableStartPatch
{

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
    private static IEnumerable<CodeInstruction> RedirectSpawnOnGroundCheck(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        var itemPropertiesFld = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.itemProperties));
        var spawnsOnGroundFld = AccessTools.Field(typeof(Item), nameof(Item.itemSpawnsOnGround));

        var replacementMethod = AccessTools.Method(typeof(GrabbableStartPatch), nameof(NewSpawnOnGroundCheck));

        var matcher = new CodeMatcher(codes);


        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, itemPropertiesFld),
            new CodeMatch(OpCodes.Ldfld, spawnsOnGroundFld),
            new CodeMatch(OpCodes.Brfalse)
        );

        if (matcher.IsInvalid)
        {
            return codes;
        }

        matcher.Advance(1);

        matcher.RemoveInstructions(2);

        matcher.Insert(new CodeInstruction(OpCodes.Call, replacementMethod));

        MoreCupboards.Logger.LogDebug("MattyFix: GrabbableObject.Start patched!");

        return matcher.Instructions();
    }

    private static bool NewSpawnOnGroundCheck(GrabbableObject grabbableObject)
    {
        MoreCupboards.Logger.LogDebug($"MattyFix: {grabbableObject.itemProperties.itemName}({grabbableObject.NetworkObjectId}) processing GrabbableObject pos {grabbableObject.transform.position}");

        var ret = ShouldSpawnOnGround(grabbableObject);

        MoreCupboards.Logger.LogDebug($"MattyFix: {grabbableObject.itemProperties.itemName}({grabbableObject.NetworkObjectId}) processing GrabbableObject spawnState " + $"OnGround - was: {grabbableObject.itemProperties.itemSpawnsOnGround} new:{ret}");

        return ret;
    }

    private static bool ShouldSpawnOnGround(GrabbableObject grabbableObject)
    {
        var ret = grabbableObject.itemProperties.itemSpawnsOnGround;

        if (grabbableObject is ClipboardItem || (grabbableObject is PhysicsProp && grabbableObject.itemProperties.itemName == "Sticky note"))
            return ret;

        if (StartOfRound.Instance.localPlayerController && !StartOfRoundPatch.IsInitializingGame)
            return ret;

        if (Closets != null)
        {
            foreach (ClosetHolder closet in Closets)
            {
                if (closet.gameObject.name == "StorageCloset" && AddCupboards.mattyPresent)
                {
                    MoreCupboards.Logger.LogDebug("Found vanilla cupboard! Skipping MattyFix...");
                }
                if (grabbableObject.transform.parent == closet.gameObject.transform)
                {
                    MoreCupboards.Logger.LogDebug("Found parent for spawning!");
                    ret = false;
                }
            }
        }
        return ret;
    }

    [HarmonyPatch]
    internal class StartOfRoundPatch
    {
        internal static bool IsInitializingGame = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        private static void MarkServerStart(StartOfRound __instance)
        {
            IsInitializingGame = true;
            __instance.StartCoroutine(WaitCoupleOfFrames());
        }

        private static IEnumerator WaitCoupleOfFrames()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            IsInitializingGame = false;
        }
    }
}
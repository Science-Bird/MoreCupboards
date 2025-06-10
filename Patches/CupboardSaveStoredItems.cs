using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace MoreCupboards.Patches;

[HarmonyPatch]
internal class CupboardSaveStoredItems
{
    static bool doSync = false;
    static int syncTimer = 30;

    static void ApplyCupboardParents()
    {
        if (!MoreCupboards.mattyPresent || !MoreCupboards.autoParent.Value)
        {
            return;
        }
        MoreCupboards.Logger.LogDebug("Finding objects in cupboards...");
        GrabbableObject[] grabbables = Object.FindObjectsOfType<GrabbableObject>();
        List<BoxCollider> colliders = new List<BoxCollider>();

        for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
        {
            string indexString = i.ToString();
            GameObject cupboard;
            cupboard = GameObject.Find("StorageCloset" + indexString + "(Clone)/PlacementCollider");
            if (cupboard == null)
            {
                cupboard = GameObject.Find("StorageCloset" + indexString + "Alt" + "(Clone)/PlacementCollider");
            }
            if (cupboard == null)
            {
                MoreCupboards.Logger.LogDebug("Unable to find specified cupboard! " + "StorageCloset" + indexString);
                continue;
            }
            BoxCollider collider = cupboard.GetComponent<BoxCollider>();
            if (collider != null)
            {
                colliders.Add(collider);
            }
        }
        foreach (GrabbableObject grabbable in grabbables)
        {
            foreach (BoxCollider collider in colliders)
            {
                Transform parent = collider.gameObject.transform.parent;
                if (collider.bounds.Contains(grabbable.transform.position) && parent != null)
                {
                    grabbable.transform.parent = parent;
                    grabbable.targetFloorPosition = grabbable.transform.localPosition;
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
    private static void OnUpdate(StartOfRound __instance)
    {
        if (!MoreCupboards.mattyPresent || !MoreCupboards.autoParent.Value)
        {
            return;
        }
        if (doSync)
        {
            syncTimer -= 1;
            if (syncTimer <= 0)
            {
                ApplyCupboardParents();
                doSync = false;
                syncTimer = 30;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
    private static void LoadOnServerSpawn(StartOfRound __instance)
    {
        if (MoreCupboards.mattyPresent && MoreCupboards.autoParent.Value)
        {
            doSync = true;
            syncTimer = 30;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
    private static void LoadAfterCupboardSync(StartOfRound __instance)
    {
        if (MoreCupboards.mattyPresent && MoreCupboards.autoParent.Value)
        {
            doSync = true;
            syncTimer = 30;
        }
    }
}
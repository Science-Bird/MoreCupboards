using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;

namespace MoreCupboards.Patches;

[HarmonyPatch]
internal class CupboardMattyFix
{
    private static List<ClosetHolder>? _closet;

    internal static List<ClosetHolder> Closets
    {
        get
        {
            if (_closet != null)
            {
                if (_closet.Count() > 0)
                {
                    return _closet;
                }
            }
            for (int i = 0; i <= MoreCupboards.maximumCupboards.Value; i++)
            {
                MoreCupboards.Logger.LogInfo($"Loading closet {i}...");
                ClosetHolder? closetTemp = new ClosetHolder(i);
                if (closetTemp.Value.gameObject != null)
                {
                    MoreCupboards.Logger.LogInfo($"Loaded closet {i}!");
                    _closet?.Add(closetTemp.Value);
                }
            }
            return _closet;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
    private static void PreSpawnInitialization(GrabbableObject __instance)
    {
        _closet = new List<ClosetHolder>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnDestroy))]
    private static void ResetOnDisconnect()
    {
        _closet = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
    private static void AfterCupboardSync(StartOfRound __instance)
    {
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost))
            return;

        if (__instance.IsServer)
            return;

        List<ClosetHolder> closetList = Closets;
        if (closetList == null)
        {
            MoreCupboards.Logger.LogError("Null closet list!");
            return;
        }
        if (closetList.Count() == 0)
        {
            MoreCupboards.Logger.LogInfo("Empty closet list!");
            return;
        }
        for (int i = 0; i < closetList.Count(); i++)
        {
            var closet = closetList[i];
            if (closet.IsInitialized)
            {
                MoreCupboards.Logger.LogDebug($"MattyFix: SyncShipUnlockablesClientRpc Cupboard Triggered but was already Initialized! index: {i}");
                return;
            }

            closet.IsInitialized = true;

            if (closet.Unlockable.inStorage)
                return;

            closet.gameObject.GetComponent<AutoParentToShip>().MoveToOffset();

            Physics.SyncTransforms();

            var grabbables = Object.FindObjectsOfType<GrabbableObject>();
            foreach (var grabbable in grabbables.Where(g => g.isInShipRoom))
            {
                var offset = 0f;
                if (grabbable.hasHitGround)
                    offset = grabbable.itemProperties.verticalOffset;
                ShelfCheck(grabbable, offset, i);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPriority(0)]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
    private static void OnServerSpawn(GrabbableObject __instance)
    {
        List<ClosetHolder> closetList = Closets;
        if (closetList == null)
        {
            MoreCupboards.Logger.LogError("Null closet list!");
            return;
        }
        if (closetList.Count() == 0)
        {
            MoreCupboards.Logger.LogInfo("Empty closet list!");
            return;
        }
        for (int i = 0; i < closetList.Count(); i++)
        {
            var closet = closetList[i];

            if (closet.IsInitialized)
            {
                MoreCupboards.Logger.LogDebug($"MattyFix: LoadShipGrabbableItems Cupboard Triggered but was already Initialized! index: {i}");
                return;
            }

            closet.IsInitialized = true;

            if (closet.Unlockable.inStorage)
                return;

            closet.gameObject.GetComponent<AutoParentToShip>().MoveToOffset();

            Physics.SyncTransforms();

            var grabbables = Object.FindObjectsOfType<GrabbableObject>();
            foreach (var grabbable in grabbables.Where(g => g.isInShipRoom))
            {
                ShelfCheck(grabbable, 0f, i);
            }
        }
    }

    private static void ShelfCheck(GrabbableObject grabbable, float offset = 0f, int index = 0)
    {
        MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Cupboard Triggered! index: {index}");

        if (grabbable is ClipboardItem || (grabbable is PhysicsProp && grabbable.itemProperties.itemName == "Sticky note"))
            return;

        var tolerance = 0.1;
        var sqrTolerance = tolerance * tolerance;
        try
        {
            var pos = grabbable.transform.position + Vector3.down * offset;

            MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Item pos {pos}! index: {index}");

            List<ClosetHolder> closetList = Closets;
            var closet = closetList[index];

            var distance = 500f;
            PlaceableObjectsSurface found = null;
            Vector3? closest = null;

            MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Cupboard pos {closet.Collider.bounds.min}! index: {index}");

            var closetCollider = closet.Collider;
            if (pos.y < closetCollider.bounds.max.y && closetCollider.bounds.SqrDistance(pos) <= sqrTolerance)
            {
                foreach (var shelfHolder in closet.Shelves)
                {
                    var hitPoint = shelfHolder.Collider.ClosestPointOnBounds(pos);
                    var tmp = pos.y - hitPoint.y;

                    MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Shelve is {tmp} away! index: {index}");

                    if (tmp > distance)
                    {
                        continue;
                    }
                    if (tmp >= 0 && tmp < distance)
                    {
                        found = shelfHolder.Shelf;
                        distance = tmp;
                        closest = hitPoint;
                    }
                }
                MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Chosen Shelve is {distance} away! index: {index}");
                MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - With hitpoint at {closest}! index: {index}");
            }

            var transform = grabbable.transform;
            if (found != null)
            {
                Vector3 newPos = closest.Value + Vector3.up * grabbable.itemProperties.verticalOffset;
                transform.parent = closet.gameObject.transform;
                MoreCupboards.Logger.LogDebug($"Parenting {grabbable.itemProperties.itemName} to {closet.gameObject.name}, index: {index} index: {index}");
                transform.position = newPos;
                grabbable.targetFloorPosition = transform.localPosition;
                MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Pos on shelf {newPos}! index: {index}");
            }
        }
        catch (Exception ex)
        {
            MoreCupboards.Logger.LogError($"MattyFix: Exception while checking for Cupboard {ex}, index: {index}");
        }
    }

    internal struct ClosetHolder
    {
        public readonly UnlockableItem Unlockable;
        public readonly GameObject? gameObject;
        public readonly List<ShelfHolder> Shelves;
        public readonly Collider Collider;
        public bool IsInitialized;

        public ClosetHolder(int index)
        {
            string indexString = index.ToString();
            if (index == 0)
            {
                indexString = string.Empty;
                gameObject = GameObject.Find("/Environment/HangarShip/StorageCloset");
            }
            else
            {

                gameObject = GameObject.Find("StorageCloset" + indexString + "(Clone)");
                if (gameObject == null)
                {
                    gameObject = GameObject.Find("StorageCloset" + indexString + "Alt" + "(Clone)");
                }
                if (gameObject == null)
                {
                    MoreCupboards.Logger.LogDebug("Unable to find specified cupboard! " + "StorageCloset" + indexString);
                    return;
                }
            }
            Unlockable = StartOfRound.Instance.unlockablesList.unlockables
                .Find(u => u.unlockableName == indexString + "Cupboard");
            if (Unlockable.inStorage)
            {
                MoreCupboards.Logger.LogDebug("Cupboard found in storage, skipping! " + "StorageCloset" + indexString);
                gameObject = null;
                return;
            }
            Collider = gameObject.GetComponent<Collider>();
            Shelves = gameObject.GetComponentsInChildren<PlaceableObjectsSurface>().Select(s =>
                new ShelfHolder
                {
                    Shelf = s,
                    Collider = s.GetComponent<Collider>()
                }).ToList();
        }
    }

    internal struct ShelfHolder
    {
        public PlaceableObjectsSurface Shelf;
        public Collider Collider;
    }
}
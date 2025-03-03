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
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
            (!networkManager.IsClient && !networkManager.IsHost))
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

        if (grabbable is ClipboardItem ||
            (grabbable is PhysicsProp && grabbable.itemProperties.itemName == "Sticky note"))
            return;

        var tolerance = 0.1;
        var sqrTolerance = tolerance * tolerance;
        try
        {
            var pos = grabbable.transform.position + Vector3.down * offset;

            MoreCupboards.Logger.LogDebug($"MattyFix: {grabbable.itemProperties.itemName}({grabbable.NetworkObjectId}) - Item pos {pos}! index: {index}");

            List<ClosetHolder> closetList = Closets;
            var closet = closetList[index];

            var distance = float.MaxValue;
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
                Vector3 newPos = closest.Value + Vector3.up * grabbable.itemProperties.verticalOffset; //ItemPatches.FixPlacement(closest.Value, found.transform, grabbable);
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

    private static void UpdateItemRotation(Item item, string itemPath = null)
    {
        var ogRotation = item.restingRotation;
        ogRotation.y = item.floorYOffset;

        item.floorYOffset = (int)Math.Round(ogRotation.y);
    }


    [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
    internal static class NetworkSpawnPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(-900)]
        private static void Postfix(NetworkBehaviour __instance)
        {
            if (__instance is not GrabbableObject grabbable)
                return;

            if (grabbable is ClipboardItem ||
                (grabbable is PhysicsProp && grabbable.itemProperties.itemName == "Sticky note"))
                return;

            if (StartOfRound.Instance.localPlayerController && !StartOfRoundPatch.IsInitializingGame)
                return;

            try
            {
                grabbable.isInElevator = true;
                grabbable.isInShipRoom = true;
                if (grabbable is LungProp lungProp)
                {
                    lungProp.isLungDocked = false;
                    lungProp.isLungPowered = false;
                    lungProp.isLungDockedInElevator = false;
                    lungProp.GetComponent<AudioSource>()?.Stop();
                }

                grabbable.floorYRot = (int)Math.Floor(grabbable.transform.eulerAngles.y - 90f - grabbable.itemProperties.floorYOffset);
                grabbable.transform.rotation = Quaternion.Euler(grabbable.itemProperties.restingRotation.x, grabbable.transform.eulerAngles.y, grabbable.itemProperties.restingRotation.z);
            }
            catch (Exception ex)
            {
                MoreCupboards.Logger.LogError($"MattyFix: Exception while setting rotation, {ex}");
            }
        }
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

            //if it's one of the pre-existing items
            if (grabbableObject is ClipboardItem ||
                (grabbableObject is PhysicsProp && grabbableObject.itemProperties.itemName == "Sticky note"))
                return ret;

            ret = StartOfRound.Instance.IsServer;//Tied to OutOfBounds

            if (_closet != null)
            {
                foreach (ClosetHolder closet in _closet)
                {
                    if (grabbableObject.transform.parent == closet.gameObject.transform)
                    {
                        MoreCupboards.Logger.LogDebug("Found parent for spawning!");
                        ret = false;
                    }
                }
            }
            return ret;
        }

    }
}
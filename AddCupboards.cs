using System.IO;
using System.Reflection;
using LethalLib.Modules;
using UnityEngine;
using System.Collections.Generic;
using System;
using LethalLib.Extras;

namespace MoreCupboards
{
    public class AddCupboards
    {
        public static List<UnlockableItemDef> cupboardUnlockables = new List<UnlockableItemDef>();
        private static readonly string[] colourNames = ["Cupboard Orange", "Cupboard Yellow", "Cupboard Green", "Cupboard Blue", "Cupboard Purple"];
        private static int price = 300;

        public static void RegisterCupboards()
        {
            if (MoreCupboards.cupboardPrice.Value >= 0)
            {
                price = MoreCupboards.cupboardPrice.Value;
            }

            TerminalNode cupboardInfoNode = (TerminalNode)MoreCupboards.CupboardAssets.LoadAsset("CupboardInfo");
            string altString = "";
            if (MoreCupboards.noDoors.Value)
            {
                altString = "Alt";
            }
            for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
            {
                cupboardUnlockables.Add((UnlockableItemDef)MoreCupboards.CupboardAssets.LoadAsset("Cupboard" + i.ToString() + altString + "UnlockableItemDef"));
            }

            foreach (UnlockableItemDef cupboard in cupboardUnlockables)
            {
                NetworkPrefabs.RegisterNetworkPrefab(cupboard.unlockable.prefabObject);
                Unlockables.RegisterUnlockable(cupboard, StoreType.ShipUpgrade, null, null, cupboardInfoNode, price);
            }
        }
    }
}

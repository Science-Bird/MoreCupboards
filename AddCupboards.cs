using System.IO;
using System.Reflection;
using LethalLib.Modules;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MoreCupboards
{
    public class AddCupboards
    {
        public static AssetBundle CupboardAssets;
        public static ContentLoader ContentLoader;
        public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static int price = 100;
        public static void RegisterCupboard()
        {

            if (MoreCupboards.cupboardPrice.Value >= 0)
            {
                price = MoreCupboards.cupboardPrice.Value;
            }
            CupboardAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cupboardunlockable"));

            ContentLoader = new ContentLoader(MoreCupboards.pluginInfo, CupboardAssets, (content, prefab) => {
                Prefabs.Add(content.ID, prefab);
            });

            string altString = "";
            if (MoreCupboards.noDoors.Value)
            {
                altString = "Alt";
            }

            List<ContentLoader.CustomContent> contentList = new List<ContentLoader.CustomContent>();
            for (int i = 1; i <= MoreCupboards.maximumCupboards.Value; i++)
            {
                contentList.Add(new LethalLib.Modules.ContentLoader.Unlockable(i.ToString() + "Cupboard", "Assets/LethalCompany/Mods/MoreCupboards/Cupboard" + i.ToString() + altString + "UnlockableItemDef.asset", price, null, null, "Assets/LethalCompany/Mods/MoreCupboards/CupboardInfo.asset", StoreType.ShipUpgrade));
            }
            ContentLoader.CustomContent[] content = contentList.ToArray();
            ContentLoader.RegisterAll(content);
        }
    }
}

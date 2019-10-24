using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using Harmony;
using UnityEngine;

namespace AnylandMods.PersonalizedUI
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static Config config;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            config = new Config();
            config.Load();

            var btnReload = new MenuButton("personalizedUiReloadConfig", "Reload UI Config");
            btnReload.Action += BtnReload_Action;
            Foundation.ModMenu.Add(btnReload);

            return true;
        }

        private static void BtnReload_Action(string id, Dialog dialog)
        {
            config.Load();
        }
    }

    [HarmonyPatch(typeof(Dialog), "AddFundament")]
    public static class FundamentalReplacement {
        public static bool Prefix()
        {
            return !Main.config.HideFundament;
        }

        public static void Postfix(Dialog __instance)
        {
            if (Main.config.FundamentTID.Length > 0) {
                Managers.thingManager.InstantiateThingOnDialogViaCache(
                    ThingRequestContext.LocalTest,
                    thingId: Main.config.FundamentTID,
                    fundament: __instance.transform,
                    position: Vector3.zero,
                    scale: 1.0f,
                    useDefaultRotation: true
                );
            }
        }
    }
}

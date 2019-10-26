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
        internal static ConfigFile config;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            config = new ConfigFile(mod);
            config.Load();

            ModMenu.AddButton(harmony, "Reload UI Config", BtnReload_Action);

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
                    useDefaultRotation: true,
                    isGift: Main.config.Dynamic
                );
            }
        }
    }

    [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.ReportMissingThing))]
    public static class SuppressMissingThingRptIfLocalTest {
        public static bool Prefix(ThingRequestContext thingRequestContext)
        {
            // Philipp said he'd rather not have the server get unusual requests.
            // Not sure if this would count, but just in case...
            return thingRequestContext != ThingRequestContext.LocalTest;
        }
    }

    [HarmonyPatch(typeof(ThingDialog), "AddBacksideButtons")]
    public static class SetAsFundamentButton {
        public static void Postfix(ThingDialog __instance)
        {
            __instance.AddButton("setAsFundament", null, "Use for Dialogs...", "ButtonCompactNoIcon", 0, -420, textColor: TextColor.Blue, isOnBackside: true);
        }
    }

    [HarmonyPatch(typeof(ThingDialog), nameof(ThingDialog.OnClick))]
    public static class HandleSetAsFundamentClick {
        public static void Postfix(ThingDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("setAsFundament")) {
                Main.config.FundamentTID = __instance.thing.thingId;
                Main.config.Save();
            }
        }
    }
}

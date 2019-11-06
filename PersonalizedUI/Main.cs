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
        internal static bool enableCustomColors = true;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            config = new ConfigFile(mod);
            config.Load();

            ModMenu.AddButton(harmony, "Change UI Bkgd...", BtnCustomize_Action);

            return true;
        }

        private static void BtnCustomize_Action(string id, Dialog dialog)
        {
            config.Load();
            MenuDialog.SwitchTo(UIMenu.Menu, dialog.hand(), dialog.tabName);
        }
    }

    [HarmonyPatch(typeof(Dialog), "AddFundament")]
    public static class FundamentalReplacement {
        public static bool Prefix(Dialog __instance)
        {
            // GiftsDialog breaks when AddFundament is patched out.
            return !Main.config.HideFundament || __instance.GetType() == typeof(GiftsDialog);
        }

        public static void Postfix(Dialog __instance)
        {
            if (Main.config.FundamentTID.Length > 0 && __instance.GetType() != typeof(GiftsDialog)) {
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

    [HarmonyPatch(typeof(Dialog), nameof(Dialog.AddButton))]
    public static class CustomButtonColor {
        public static void Prefix(ref string buttonColor)
        {
            if (Main.enableCustomColors && buttonColor.Length == 0) {
                buttonColor = Main.config.ButtonColor;
            }
        }
    }

    [HarmonyPatch(typeof(Dialog), "AddCheckbox")]
    public static class CustomCheckboxColor {
        public static void Postfix(Dialog __instance, GameObject __result)
        {
            string colorstr = Main.config.CheckboxColor;
            if (Main.enableCustomColors && colorstr.Length > 0) {
                __instance.SetButtonColor(__result, Misc.ColorStringToColor(colorstr));
            }
        }
    }

    [HarmonyPatch(typeof(Dialog), "ApplyTextSizeFactor")]
    public static class CustomTextColor {
        public static void Postfix(Transform textPart)
        {
            string colorstr = Main.config.TextColor;
            if (Main.enableCustomColors && colorstr.Length > 0) {
                textPart.GetComponent<TextMesh>().GetComponent<Renderer>().material.color = Misc.ColorStringToColor(colorstr);
            }
        }
    }

    [HarmonyPatch(typeof(KeyboardDialog), nameof(KeyboardDialog.Start))]
    public static class DisableCustomColorsForKeyboard {
        public static void Prefix()
        {
            Main.enableCustomColors = false;
        }
    }

    [HarmonyPatch(typeof(KeyboardDialog), nameof(KeyboardDialog.HandleAndCloseDialog))]
    public static class RestoreCustomColorsAfterKeyboard {
        public static void Postfix()
        {
            Main.enableCustomColors = true;
        }
    }
}

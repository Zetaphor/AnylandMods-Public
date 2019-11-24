using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;

namespace AnylandMods.Improvements.ColorPicker {
    [HarmonyPatch(typeof(MaterialDialog), nameof(MaterialDialog.UpdatePropertyInterface))]
    public static class AddRGBSliders {
        public static bool Prefix(MaterialDialog __instance)
        {
            bool colorExpanderShows = (bool)typeof(MaterialDialog).GetField("colorExpanderShows", Publication.StaticNonPub).GetValue(null);
            if (CreationHelper.currentMaterialTab == MaterialTab.material && colorExpanderShows) {
                int xOnFundament = -590 * ((__instance.side() == Side.Right) ? -1 : 1);
                if (!__instance.currentAddedPropertyButtonsListSignature().Equals("RGBSliders")) {
                    __instance.currentAddedPropertyButtonsListSignature("RGBSliders");
                    __instance.DeleteTexturePropertyButtons();
                    __instance.DeleteParticleSystemPropertyButtons();

                    GameObject btn;
                    btn = __instance.AddButton("materialProperty", "R", "R", "ButtonSmall", xOnFundament, -240, buttonColor: "255,0,0", textSizeFactor: 2.5f, textColor: TextColor.Red);
                    __instance.StyleAsPropertyButton(btn);
                    btn = __instance.AddButton("materialProperty", "G", "G", "ButtonSmall", xOnFundament, 0, buttonColor: "0,255,0", textSizeFactor: 2.5f, textColor: TextColor.Green);
                    __instance.StyleAsPropertyButton(btn);
                    btn = __instance.AddButton("materialProperty", "B", "B", "ButtonSmall", xOnFundament, 240, buttonColor: "0,0,255", textSizeFactor: 2.5f, textColor: TextColor.Blue);
                    __instance.StyleAsPropertyButton(btn);
                }
                return false;
            } else {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(MaterialDialog), "UpdateColorExpander")]
    public static class UpdatePropertyInterfaceWithColorExpander {
        public static void Postfix(MaterialDialog __instance)
        {
            if (CreationHelper.currentMaterialTab == MaterialTab.material)
                __instance.UpdatePropertyInterface();
        }
    }
}

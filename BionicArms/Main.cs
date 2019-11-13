using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using Harmony;
using UnityEngine;

namespace AnylandMods.BionicArms
{
    public static class Main {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static ConfigFile config;
        internal static Menu menu;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            config = new ConfigFile(modEntry);

            menu = new Menu();
            menu.SetBackButton(ModMenu.Menu);

            var miMoveHand = new MenuCheckbox("moveHand", "Move HandDot");
            miMoveHand.Footnote = "With Leg";
            miMoveHand.ExtraIcon = ExtraIcon.TouchUncollidable;
            miMoveHand.Action += MiMoveHand_Action;
            var miExpEnabled = new MenuCheckbox("expEnabled", "Move Exponentially");
            miExpEnabled.Action += MiExpEnabled_Action;
            var miExpBase = new MenuSlider("Exp. Base: ", 2.0f, Main.config.ExpBase, 10000.0f, "^distance");
            miExpBase.RoundValues = true;
            miExpBase.Action += MiExpBase_Action;

            menu.Add(miExpEnabled);
            menu.Add(miMoveHand);
            menu.Add(miExpBase);

            ModMenu.AddButton(harmony, "Leg Options...", LegOptions_Action);

            return true;
        }

        private static void MiExpBase_Action(string id, Dialog dialog, float value)
        {
            config.ExpBase = value;
            config.Save();
        }

        public static void MiExpEnabled_Action(string id, Dialog dialog, bool value)
        {
            config.ExpEnabled = value;
            config.Save();
        }

        public static void MiMoveHand_Action(string id, Dialog dialog, bool value)
        {
            config.MoveHand = value;
            config.Save();
        }

        public static void LegOptions_Action(string id, Dialog dialog)
        {
            MenuDialog.SwitchTo(menu, dialog.hand(), dialog.tabName);
        }
    }

    [HarmonyPatch(typeof(Hand), "HandleLegPuppeteerMovement")]
    public static class ExponentialLegPuppeteering {
        public static void Prefix(Hand __instance)
        {
            if (Main.config.ExpEnabled) {
                Vector3 delta = __instance.transform.position - __instance.previousPosition();
                Vector3 leg_linear = __instance.leg.position.normalized * Mathf.Log(__instance.leg.position.magnitude, Main.config.ExpBase);
                leg_linear += delta;
                Vector3 leg_exp = leg_linear.normalized * Mathf.Pow(Main.config.ExpBase, leg_linear.magnitude);
                // Change the position it thinks the hand was in last frame, so the delta calculation returns the value we want
                __instance.previousPosition(__instance.leg.position + __instance.transform.position - leg_exp);
            }
        }
    }

    [HarmonyPatch(typeof(Hand), "StartLegPuppeteering")]
    public static class HandDotLegPuppeteeringStart {
        private static Vector3 FindHandDotPos(Transform attachmentPoint)
        {
            foreach (ThingPart tp in attachmentPoint.GetComponentsInChildren<ThingPart>()) {
                if (tp.givenName.ToLower().Equals("handdot")) {
                    return tp.transform.position;
                }
            }
            return attachmentPoint.position;
        }

        public static void Postfix(Hand __instance)
        {
            if (Main.config.MoveHand) {
                __instance.handDot.transform.parent = __instance.leg.transform;
                __instance.handDot.transform.position = FindHandDotPos(__instance.leg.transform);
            }
        }
    }

    [HarmonyPatch(typeof(Hand), "EndLegPuppeteering")]
    public static class HandDotLegPuppeteeringEnd {
        public static void Postfix(Hand __instance)
        {
            __instance.handDot.transform.localPosition = __instance.handDotNormalPosition();
        }
    }
}

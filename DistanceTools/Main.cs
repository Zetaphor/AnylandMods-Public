using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using Harmony;
using UnityEngine;

namespace AnylandMods.DistanceTools
{
    public static class Main {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static ConfigFile config;
        internal static Menu menu;
        internal static Perspective.PerspectiveOptions perspectiveOpts;
        internal static bool expDragInProgressLeft = false;
        internal static bool expDragInProgressRight = false;
        internal static Vector3 expDragStartPosLeft;
        internal static Vector3 expDragStartPosRight;
        internal static Vector3 expDragHandStartPosLeft;
        internal static Vector3 expDragHandStartPosRight;

        static Main()
        {
            perspectiveOpts = new Perspective.PerspectiveOptions();
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            config = new ConfigFile(modEntry);

            menu = new Menu();
            menu.SetBackButton(ModMenu.Menu);

            var miExpLegs = new MenuCheckbox("expLegs", "Move Legs Exponentially");
            miExpLegs.Action += MiExpEnabled_Action;
            var miExpDrag = new MenuCheckbox("expDrag", "Move Things Exponentially");
            miExpDrag.Action += MiExpDrag_Action;
            var miExpBase = new MenuSlider("Exp. Base: ", 2.0f, Main.config.ExpBase, 10000.0f, "^distance");
            miExpBase.RoundValues = true;
            miExpBase.Action += MiExpBase_Action;

            menu.Add(miExpDrag);
            menu.Add(miExpLegs);
            menu.Add(miExpBase);

            ModMenu.AddButton(harmony, "Motion Amplification...", LegOptions_Action);
            ModMenu.AddButton(harmony, "Perspective Edit Mode", MiPerspectiveGrab_Action);

            return true;
        }

        private static void MiExpDrag_Action(string id, Dialog dialog, bool value)
        {
            config.ExpDrag = value;
            config.Save();
        }

        private static void MiPerspectiveGrab_Action(string id, Dialog dialog)
        {
            if (Our.mode == EditModes.Area) {
                CustomDialog.SwitchTo<Perspective.PerspectiveEditDialog>(dialog.hand(), dialog.tabName);
            } else {
                Managers.errorManager.BeepError();
            }
        }

        private static void MiExpBase_Action(string id, Dialog dialog, float value)
        {
            config.ExpBase = value;
            config.Save();
        }

        public static void MiExpEnabled_Action(string id, Dialog dialog, bool value)
        {
            config.ExpLegs = value;
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
            if (Main.config.ExpLegs) {
                Vector3 delta = __instance.transform.position - __instance.previousPosition();
                Vector3 leg_linear = __instance.leg.position.normalized * Mathf.Log(__instance.leg.position.magnitude, Main.config.ExpBase);
                leg_linear += delta;
                Vector3 leg_exp = leg_linear.normalized * Mathf.Pow(Main.config.ExpBase, leg_linear.magnitude);
                // Change the position it thinks the hand was in last frame, so the delta calculation returns the value we want
                __instance.previousPosition(__instance.leg.position + __instance.transform.position - leg_exp);
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class ExponentialThingMovement {
        public static void Postfix(HandDot __instance)
        {
            if (Main.config.ExpDrag && (Our.mode == EditModes.Area || Our.mode == EditModes.Thing) && __instance.currentlyHeldObject != null
                && __instance.controller != null && CrossDevice.GetPress(__instance.controller, CrossDevice.button_grab, __instance.side)) {

                Vector3 startPos = (__instance.side == Side.Left) ? Main.expDragStartPosLeft : Main.expDragStartPosRight;
                Vector3 handStartPos = (__instance.side == Side.Left) ? Main.expDragHandStartPosLeft : Main.expDragHandStartPosRight;
                if ((__instance.side == Side.Left) ? !Main.expDragInProgressLeft : !Main.expDragInProgressRight) {
                    // That is, if this is the start of us dragging this particular thing...
                    startPos = __instance.currentlyHeldObject.transform.position;
                    if (__instance.side == Side.Left) {
                        Main.expDragInProgressLeft = true;
                        Main.expDragStartPosLeft = startPos;
                    } else {
                        Main.expDragInProgressRight = true;
                        Main.expDragStartPosRight = startPos;
                    }
                }
                Vector3 offset = __instance.transform.position - handStartPos;
                __instance.currentlyHeldObject.transform.position = startPos + offset.normalized * (Mathf.Pow(Main.config.ExpBase, offset.magnitude) - 1);
            } else {
                if (__instance.side == Side.Left) {
                    Main.expDragInProgressLeft = false;
                    Main.expDragHandStartPosLeft = __instance.transform.position;
                } else {
                    Main.expDragInProgressRight = false;
                    Main.expDragHandStartPosRight = __instance.transform.position;
                }
            }
        }
    }
}

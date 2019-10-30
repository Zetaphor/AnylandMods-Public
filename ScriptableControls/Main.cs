using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using Valve.VR;

namespace AnylandMods.ScriptableControls {
    public static class Main {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            ModMenu.AddButton(harmony, "Body Motions...", BtnBodyMotions_Action);

            BodyTellManager.OnUpdate += HandDotUpdateHook.UpdateTests;

            return true;
        }

        private static void BtnBodyMotions_Action(string id, Dialog dialog)
        {
            BodyTellManager.Update();
            Managers.dialogManager.SwitchToNewDialog(DialogType.BodyMotions, dialog.hand(), dialog.tabName);
        }

        internal static void TellBody(string data)
        {
            Managers.behaviorScriptManager.TriggerTellBodyEventToAttachments(Managers.personManager.ourPerson, data, true);
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class HandDotUpdateHook {
        private static List<Tuple<IFlagTest, string>> tests;
        private static UInt64 flags = 0;

        static HandDotUpdateHook()
        {
            tests = new List<Tuple<IFlagTest, string>>();
        }

        public static void UpdateTests()
        {
            Harmony.FileLog.Log("UpdateTests called");
            tests.Clear();
            foreach (string tell in BodyTellManager.BodyTellList) {
                IFlagTest test;
                Harmony.FileLog.Log("Parsing " + tell);
                if (ControlState.TryParseTellString(tell, out test)) {
                    Harmony.FileLog.Log("It works! " + test.ToString());
                    tests.Add(new Tuple<IFlagTest, string>(test, tell));
                } else {
                    Harmony.FileLog.Log("No luck there.");
                }
            }
        }

        private static void SendPressMsgs(HandDot handDot, EVRButtonId button, string name)
        {
            string sideLetter = (handDot.side == Side.Left) ? "l" : "r";
            if (handDot.controller.GetPressDown(button)) {
                Main.TellBody(sideLetter + name + "1");
            } else if (handDot.controller.GetPressUp(button)) {
                Main.TellBody(sideLetter + name + "0");
            }
        }

        public static void Postfix(HandDot __instance)
        {
            if (__instance.controller is null)
                return;

            bool context = CrossDevice.GetPress(__instance.controller, CrossDevice.button_context, __instance.side);
            bool delete = CrossDevice.GetPress(__instance.controller, CrossDevice.button_delete, __instance.side);
            bool fingers = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x > 0.9f;
            bool grab = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grab, __instance.side);
            bool legs = CrossDevice.GetPress(__instance.controller, CrossDevice.button_legPuppeteering, __instance.side);
            bool teleport = CrossDevice.GetPress(__instance.controller, CrossDevice.button_teleport, __instance.side);
            bool trigger = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grabTip, __instance.side);

            UInt64 myFlags = 0;
            if (context) myFlags += ControlState.Flags.ContextLaser;
            if (delete) myFlags += ControlState.Flags.Delete;
            if (fingers) myFlags += ControlState.Flags.FingersClosed;
            if (grab) myFlags += ControlState.Flags.Grab;
            if (legs) myFlags += ControlState.Flags.LegControl;
            if (teleport) myFlags += ControlState.Flags.TeleportLaser;
            if (trigger) myFlags += ControlState.Flags.Trigger;

            if (__instance.side == Side.Left) {
                flags = (flags & ControlState.Flags.RightMask) | (myFlags << ControlState.Flags.BitsToShiftForLeft);
            } else {
                flags = (flags & ControlState.Flags.LeftMask) | myFlags;
            }

            foreach (Tuple<IFlagTest, string> test in tests) {
                if (test.Item1.Evaluate(flags)) {
                    BodyTellManager.Trigger(test.Item2);
                }
            }
        }
    }
}
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

            MenuButton btnBodyMotions = new MenuButton("bodyMotions", "Body Motions...");
            btnBodyMotions.Action += BtnBodyMotions_Action;
            Foundation.ModMenu.Add(btnBodyMotions);

            return true;
        }

        private static void BtnBodyMotions_Action(string id, Dialog dialog)
        {
            Managers.dialogManager.SwitchToNewDialog(DialogType.BodyMotions, dialog.hand(), dialog.tabName);
        }

        internal static void TellBody(string data)
        {
            Managers.behaviorScriptManager.TriggerTellBodyEventToAttachments(Managers.personManager.ourPerson, data, true);
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class HandDotUpdateHook {
        private const int TouchPadSectorCount = 4;
        private const float TouchPadSectorSize = 2.0f * (float)Math.PI / TouchPadSectorCount;

        private enum StatusFlags {
            None = 0,
            StickDown = 1,
            FingersClosed = 2
        }

        internal static SteamVR_Controller.Device leftController = null;
        private static StatusFlags statusL = StatusFlags.None;
        private static StatusFlags statusR = StatusFlags.None;

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
            if (__instance.side == Side.Left) {
                leftController = __instance.controller;
            }
            Vector2 stick = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis0);
            float fingers = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x;
            string sideLetter = (__instance.side == Side.Left) ? "l" : "r";

            if (CrossDevice.GetPressDown(__instance.controller, CrossDevice.button_delete, __instance.side)) {
                Main.TellBody(sideLetter + "del1");
            } else if (CrossDevice.GetPressUp(__instance.controller, CrossDevice.button_delete, __instance.side)) {
                Main.TellBody(sideLetter + "del0");
            }

            StatusFlags oldFlags = (__instance.side == Side.Left) ? statusL : statusR;
            StatusFlags newFlags = StatusFlags.None;

            if (Math.Abs(stick.x) < 0.5f && stick.y < -0.8f)
                newFlags |= StatusFlags.StickDown;

            if (fingers > 0.9f)
                newFlags |= StatusFlags.FingersClosed;

            StatusFlags edge = oldFlags ^ newFlags;

            if ((edge & StatusFlags.StickDown) != StatusFlags.None)
                Main.TellBody(sideLetter + "stdn" + ((newFlags & StatusFlags.StickDown) != StatusFlags.None ? "1" : "0"));

            if ((edge & StatusFlags.FingersClosed) != StatusFlags.None)
                Main.TellBody(sideLetter + "close" + ((newFlags & StatusFlags.FingersClosed) != StatusFlags.None ? "1" : "0"));

            if (__instance.side == Side.Left)
                statusL = newFlags;
            else
                statusR = newFlags;
        }
    }
}
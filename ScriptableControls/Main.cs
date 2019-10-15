using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using Valve.VR;

namespace AnylandMods.ScriptableControls
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            return true;
        }

        internal static void TellBody(string data)
        {
            Managers.behaviorScriptManager.TriggerTellBodyEventToAttachments(Managers.personManager.ourPerson, data, true);
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class HandDotUpdateHook
    {
        private const int TouchPadSectorCount = 4;
        private const float TouchPadSectorSize = 2.0f * (float)Math.PI / TouchPadSectorCount;

        private enum StatusFlags
        {
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
            if (handDot.controller.GetPressDown(button))
            {
                Main.TellBody(sideLetter + name + "1");
            } else if (handDot.controller.GetPressUp(button))
            {
                Main.TellBody(sideLetter + name + "0");
            }
        }

        public static void Postfix(HandDot __instance)
        {
            if (__instance.controller is null)
                return;
            if (__instance.side == Side.Left)
            {
                leftController = __instance.controller;
            }
            Vector2 stick = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis0);
            float fingers = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x;
            string sideLetter = (__instance.side == Side.Left) ? "l" : "r";

            if (CrossDevice.GetPressDown(__instance.controller, CrossDevice.button_delete, __instance.side))
            {
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

            /*SendPressMsgs(__instance, EVRButtonId.k_EButton_DPad_Up, "dpn");
            SendPressMsgs(__instance, EVRButtonId.k_EButton_DPad_Down, "dps");
            SendPressMsgs(__instance, EVRButtonId.k_EButton_DPad_Left, "dpl");
            SendPressMsgs(__instance, EVRButtonId.k_EButton_DPad_Right, "dpr");*/
        }
    }

    /*[HarmonyPatch(typeof(ThingPart), "ReplaceTextPlaceholders")]
    public static class AxisDebug
    {
        public static void Postfix(string s, ref string __result)
        {
            if (HandDotUpdateHook.leftController is null) return;
            s = s.ReplaceCaseInsensitive("[axis0]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_Axis0).ToString());
            s = s.ReplaceCaseInsensitive("[axis1]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_Axis1).ToString());
            s = s.ReplaceCaseInsensitive("[axis2]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_Axis2).ToString());
            s = s.ReplaceCaseInsensitive("[axis3]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_Axis3).ToString());
            s = s.ReplaceCaseInsensitive("[axis4]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_Axis4).ToString());
            s = s.ReplaceCaseInsensitive("[tpad]", HandDotUpdateHook.leftController.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad).ToString());
            __result = s;
        }
    }*/
}


/* Saved code:
            float angle = (float)Math.Atan2(pad.y, pad.x);
            int sector = (int)Math.Floor(angle + TouchPadSectorSize / 2);
            if (sector != hdd.padSector)
            {
                string sectorLetter;
                switch (sector)
                {
                    case -1:
                        sectorLetter = "x";
                        break;
                    case 0:
                        sectorLetter = "e";
                        break;
                    case 1:
                        sectorLetter = "n";
                        break;
                    case 2:
                        sectorLetter = "w";
                        break;
                    case 3:
                        sectorLetter = "s";
                        break;
                    default:
                        // This shouldn't happen.
                        sectorLetter = sector.ToString();
                        FileLog.Log("Unexpected touchpad sector " + sectorLetter);
                        break;
                }
                Main.TellBody(sideLetter + "pad" + sectorLetter);
            }

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using Valve.VR;
using System.Reflection;
using System.Reflection.Emit;

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

    [HarmonyPatch(typeof(Hand), "HandleTeleportLaser")]
    public static class FreeUpTeleportButtonWhenStickCanBeUsedInstead {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {
                yield return inst;
                if (inst.opcode == OpCodes.Call && ((MethodInfo)inst.operand).Name.Equals("GetPress")) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CrossDevice), "hasStick"));
                    yield return new CodeInstruction(OpCodes.Not);
                    yield return new CodeInstruction(OpCodes.And);
                }
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class HandDotUpdateHook {
        private static List<ControlState> tests;
        private static UInt64 flags = 0;

        static HandDotUpdateHook()
        {
            tests = new List<ControlState>();
        }

        public static void UpdateTests()
        {
            DebugLog.Log("UpdateTests called");
            tests.Clear();
            foreach (string tell in BodyTellManager.BodyTellList) {
                IFlagTest test;
                DebugLog.Log("Parsing " + tell);
                if (ControlState.TryParseTellString(tell, out test)) {
                    DebugLog.Log("It works! " + test.ToString());
                    tests.Add(new ControlState(tell, test));
                } else {
                    DebugLog.Log("No luck there.");
                }
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

            if (CrossDevice.hasStick && (Managers.browserManager is null || !Managers.browserManager.CursorIsInBrowser())) {
                Vector2 stick = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis0);
                legs = (stick.y <= -0.7f && Mathf.Abs(stick.x) <= 0.5f);
            }

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

            foreach (ControlState test in tests) {
                test.Update(flags);
                if (test.Edge) {
                    DebugLog.Log("{0} Edge {1} {2}", test.State, test.Label, test.Test);
                }
                if (test.Edge && test.State) {
                    BodyTellManager.Trigger(test.Label);
                }
            }
        }
    }
}
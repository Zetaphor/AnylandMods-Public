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
        public static UniversalScript universal;
        public static HarmonyInstance harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            ModMenu.AddButton(harmony, "Body Motions...", BtnBodyMotions_Action);

            BodyTellManager.OnUpdate += HandDotUpdateHook.UpdateTests;

            universal = new UniversalScript(mod);

            modEntry.OnUpdate = delegate (UnityModManager.ModEntry entry, float dt) {
                Person me = Managers.personManager.ourPerson;
                GameObject head = me.GetAttachmentPointById(AttachmentPointId.Head);
                var thing = head.GetComponentInChildren<Thing>();
                if (thing != null && thing.gameObject.GetComponent<UniversalScript.Tag_ScriptLinesAdded>() == null) {
                    DebugLog.Log("Adding universal script to {0}", thing.givenName);
                    universal.Load();
                    universal.AddScriptToHead();
                    thing.gameObject.AddComponent<UniversalScript.Tag_ScriptLinesAdded>();
                }
            };

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
        private const float XThreshold = 0.3f;
        private const float YThreshold = 0.3f;
        private const float ZThreshold = 0.2f;
        private const float VelocityThreshold1 = 0.7f;
        private const float VelocityThreshold2 = 2.5f;
        private const float FingersClosedThreshold = 0.25f;

        private static List<ControlState> tests;
        private static List<string> tells;
        private static FlagSet flags = FlagSet.Zeros;
        private static Vector3 lastposLeft, lastposRight;
        private static float lasttimeLeft, lasttimeRight;

        static HandDotUpdateHook()
        {
            tests = new List<ControlState>();
            tells = new List<string>();
        }

        public static void UpdateTests()
        {
            foreach (string tell in BodyTellManager.BodyTellList) {
                if (!tells.Contains(tell)) {
                    if (ControlState.TryParseTellString(tell, out ControlState state))
                        tests.Add(state);
                    tells.Add(tell);
                }
            }
        }

        private static UInt64 DirectionToFlag(Vector3 direction, UInt64[] flagTbl, Side hand)  // {X-, X+, Y-, Y+, Z-, Z+, in, out}
        {
            UInt64 left = (hand == Side.Left) ? flagTbl[7] : flagTbl[6];
            UInt64 right = (hand == Side.Right) ? flagTbl[7] : flagTbl[6];
            float absx = Mathf.Abs(direction.x), absy = Mathf.Abs(direction.y), absz = Mathf.Abs(direction.z);
            if (absx > absy && absx > absz) {
                return (direction.x > 0) ? flagTbl[1] | right : flagTbl[0] | left;
            } else if (absy > absx && absy > absz) {
                return (direction.y > 0) ? flagTbl[3] : flagTbl[2];
            } else if (absz > absx && absz > absy) {
                return (direction.z > 0) ? flagTbl[5] : flagTbl[4];
            } else {
                return 0;
            }
        }

        public static void Postfix(HandDot __instance)
        {
            if (__instance.controller is null)
                return;

            float distanceBetweenHands = (__instance.transform.position - __instance.otherDot.transform.position).magnitude;
            bool apart = distanceBetweenHands >= 1.0f;
            bool both_together = distanceBetweenHands < 0.1f;
            bool context = CrossDevice.GetPress(__instance.controller, CrossDevice.button_context, __instance.side);
            bool delete = CrossDevice.GetPress(__instance.controller, CrossDevice.button_delete, __instance.side);
            bool fingers = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x >= FingersClosedThreshold;
            bool grab = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grab, __instance.side);
            bool holding = __instance.currentlyHeldObject != null;
            bool legs = CrossDevice.GetPress(__instance.controller, CrossDevice.button_legPuppeteering, __instance.side);
            bool teleport = CrossDevice.GetPress(__instance.controller, CrossDevice.button_teleport, __instance.side);
            bool trigger = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grabTip, __instance.side);

            if (CrossDevice.hasStick && (Managers.browserManager is null || !Managers.browserManager.CursorIsInBrowser())) {
                Vector2 stick = __instance.controller.GetAxis(EVRButtonId.k_EButton_Axis0);
                legs = (stick.y <= -0.7f && Mathf.Abs(stick.x) <= 0.5f);
            }

            UInt64 myFlags = 0;
            if (apart) myFlags |= ControlState.Flags.HandsApart;
            if (both_together) myFlags |= ControlState.Flags.BothTogether;
            if (context) myFlags |= ControlState.Flags.ContextLaser;
            if (delete) myFlags |= ControlState.Flags.Delete;
            if (fingers) myFlags |= ControlState.Flags.FingersClosed;
            if (grab) myFlags |= ControlState.Flags.Grab;
            if (holding) myFlags |= ControlState.Flags.Holding;
            if (legs) myFlags |= ControlState.Flags.LegControl;
            if (teleport) myFlags |= ControlState.Flags.TeleportLaser;
            if (trigger) myFlags |= ControlState.Flags.Trigger;

            Vector3 headpos = Managers.personManager.ourPerson.Head.transform.position;
            Quaternion headrot = Managers.personManager.ourPerson.Head.transform.rotation;
            Vector3 handpos = __instance.transform.position;
            Vector3 handpos_local = Quaternion.Inverse(headrot) * (handpos - headpos);
            handpos_local /= Managers.personManager.GetOurScale();
            Quaternion handrot = Managers.personManager.ourPerson.GetHandBySide(__instance.side).transform.rotation;
            Vector3 pointdir = handrot * Vector3.forward;
            Vector3 pointdir_local = Quaternion.Inverse(headrot) * pointdir;

            myFlags |= DirectionToFlag(pointdir_local, new UInt64[] {
                ControlState.Flags.PointLeft,
                ControlState.Flags.PointRight,
                ControlState.Flags.PointDown,
                ControlState.Flags.PointUp,
                ControlState.Flags.PointFwd,
                ControlState.Flags.PointBack,
                ControlState.Flags.PointIn,
                ControlState.Flags.PointOut
            }, __instance.side);

            Vector3 palmdir = handrot * Vector3.left;
            Vector3 palmdir_local = Quaternion.Inverse(headrot) * palmdir;

            myFlags |= DirectionToFlag(palmdir_local, new UInt64[] {
                ControlState.Flags.PalmLeft,
                ControlState.Flags.PalmRight,
                ControlState.Flags.PalmDown,
                ControlState.Flags.PalmUp,
                ControlState.Flags.PalmFwd,
                ControlState.Flags.PalmBack,
                ControlState.Flags.PalmIn,
                ControlState.Flags.PalmOut
            }, __instance.side);

            if (handpos_local.x >= XThreshold) {
                myFlags |= ControlState.Flags.PosX2;
            } else if (handpos_local.x <= -XThreshold) {
                myFlags |= ControlState.Flags.PosX0;
            } else {
                myFlags |= ControlState.Flags.PosX1;
            }

            if (handpos_local.y >= YThreshold) {
                myFlags |= ControlState.Flags.PosY2;
            } else if (handpos_local.y <= -YThreshold) {
                myFlags |= ControlState.Flags.PosY0;
            } else {
                myFlags |= ControlState.Flags.PosY1;
            }

            if (handpos_local.z >= 2.0f * ZThreshold) {
                myFlags |= ControlState.Flags.PosZ2;
            } else if (handpos_local.z >= ZThreshold) {
                myFlags |= ControlState.Flags.PosZ1;
            } else {
                myFlags |= ControlState.Flags.PosZ0;
            }

            var lasttime = __instance.side == Side.Left ? lasttimeLeft : lasttimeRight;
            var lastpos = __instance.side == Side.Left ? lastposLeft : lastposRight;

            if (Time.time != lasttime) {
                Vector3 velocity = (handpos_local - lastpos) / (Time.time - lasttime);

                if (velocity.magnitude >= VelocityThreshold1) {
                    myFlags |= ControlState.Flags.Moving;
                    if (velocity.magnitude >= VelocityThreshold2) {
                        myFlags |= ControlState.Flags.MovingFast;
                    }
                }

                myFlags |= DirectionToFlag(velocity, new UInt64[] {
                    ControlState.Flags.DirLeft,
                    ControlState.Flags.DirRight,
                    ControlState.Flags.DirDown,
                    ControlState.Flags.DirUp,
                    ControlState.Flags.DirFwd,
                    ControlState.Flags.DirBack,
                    ControlState.Flags.DirIn,
                    ControlState.Flags.DirOut
                }, __instance.side);
            }

            if (__instance.side == Side.Left) {
                flags = new FlagSet(myFlags, flags.right);
                lastposLeft = handpos_local;
                lasttimeLeft = Time.time;
            } else {
                flags = new FlagSet(flags.left, myFlags);
                lastposRight = handpos_local;
                lasttimeRight = Time.time;
            }

            foreach (ControlState test in tests) {
                test.Update(flags);
                if (test.Edge) {
                    //DebugLog.LogTemp("{0} Edge {1} {2}", test.State, test.Label, test.Test);
                    //DebugLog.LogTemp("@edge={0:X} req={3:X} lastflags={1:X} pass={2:X}", test.FlagsAtEdge, test.LastFlags, test.AtRequiredEdge, test.RequireEdge);
                }
                if (test.ShouldTrigger) {
                    float t = Time.time;
                    if (!test.ConstantTrigger || t - test.LastTrigTime >= 0.1f) {
                        test.LastTrigTime = t;
                        BodyTellManager.Trigger(test.Label);
                    }
                }
            }
        }
    }
}
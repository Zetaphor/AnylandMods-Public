using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using UnityEngine;
using Harmony;

namespace AnylandMods.MultiLevelUndo
{
    public static class Main
    {
        internal static HistoryBook<Tuple<ThingPart, int>, ThingPartStateHistoryEntry> thingPartStateHistory;

        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            thingPartStateHistory = new HistoryBook<Tuple<ThingPart, int>, ThingPartStateHistoryEntry>(IdentifyThingPartState);

            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            return true;
        }
        private static string IdentifyThingPartState(Tuple<ThingPart, int> tuple)
        {
            string id = String.Format("{0}:{1}", tuple.Item1.gameObject.GetInstanceID(), tuple.Item2);
            DebugLog.Log("IdentifyThingPartState returning " + id);
            return id;
        }

        internal static Tuple<ThingPart, int> GetThingPartStateTuple(ThingPart thingPart) => new Tuple<ThingPart, int>(thingPart, thingPart.currentState);
    }

    [HarmonyPatch(typeof(HandDot), "HandleScalingOfLastTransformHandled")]
    public static class OnlyMemorizeForUndoOnceWhenScaling {
        internal static bool isScaling;
        public static void Postfix()
        {
            isScaling = true;
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class ResetFlagAfterScale {
        public static void Postfix(HandDot __instance)
        {
            bool isGrab = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grab, __instance.side);
            bool isGrabTip = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grabTip, __instance.side);
            if (!isGrab && !isGrabTip) {
                OnlyMemorizeForUndoOnceWhenScaling.isScaling = false;
            }
        }
    }

    [HarmonyPatch(typeof(ThingPart), "MemorizeForUndo")]
    public static class RewriteThingPartMemorizeForUndo {
        internal static bool disableMemorize = false;
        public static bool Prefix(ThingPart __instance)
        {
            if (__instance.GetIsOfThingBeingEdited() && !OnlyMemorizeForUndoOnceWhenScaling.isScaling && !disableMemorize) {
                if (__instance.currentState > __instance.states.Count - 1) {
                    __instance.currentState = 0;
                }
                var tuple = Main.GetThingPartStateTuple(__instance);
                Main.thingPartStateHistory.PushState(tuple, new ThingPartStateHistoryEntry(__instance.states[__instance.currentState]));
                DebugLog.Log(String.Format("M4U UndoCount={0} RedoCount={1}", Main.thingPartStateHistory.UndoCount(tuple), Main.thingPartStateHistory.RedoCount(tuple)));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ThingPart), nameof(ThingPart.Undo))]
    public static class RewriteThingPartUndo {
        public static bool Prefix(ThingPart __instance)
        {
            var tuple = Main.GetThingPartStateTuple(__instance);
            if (Main.thingPartStateHistory.UndoCount(tuple) > 0) {
                ThingPartStateHistoryEntry saved = Main.thingPartStateHistory.Undo(tuple, new ThingPartStateHistoryEntry(__instance.states[__instance.currentState]));
                DebugLog.Log("Undoing to {0}", saved);
                __instance.transform.localPosition = saved.position;
                __instance.transform.localEulerAngles = saved.rotation;
                __instance.transform.localScale = saved.scale;
                __instance.material.color = saved.color;

                RewriteThingPartMemorizeForUndo.disableMemorize = true;
                __instance.SetStatePropertiesByTransform(false);
                RewriteThingPartMemorizeForUndo.disableMemorize = false;

                if (__instance.materialType == MaterialTypes.Transparent)
                    __instance.UpdateMaterial();
            } else {
                Managers.soundManager.Play("no", __instance.transform, 0.5f, false, false);
            }
            DebugLog.Log(String.Format("[U] UndoCount={0} RedoCount={1}", Main.thingPartStateHistory.UndoCount(tuple), Main.thingPartStateHistory.RedoCount(tuple)));

            return false;
        }
    }

    [HarmonyPatch(typeof(ThingPart), nameof(ThingPart.HasUndoForThisState))]
    public static class RewriteThingPartHasUndoForThisState {
        public static bool Prefix(ThingPart __instance, ref bool __result)
        {
            __result = Main.thingPartStateHistory.UndoCount(Main.GetThingPartStateTuple(__instance)) > 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(ThingPartDialog), nameof(ThingPartDialog.UpdateUndoButton))]
    public static class AddThingPartRedoButton {
        private static GameObject redoButton = null;

        public static void Postfix(ThingPartDialog __instance)
        {
            ThingPart tp = __instance.thingPart();
            var tuple = Main.GetThingPartStateTuple(tp);
            if (redoButton != null) {
                UnityEngine.Object.Destroy(redoButton);
                redoButton = null;
            }
            if (Main.thingPartStateHistory.RedoCount(tuple) > 0) {
                int xOnFundament = (!tp.isText && !__instance.showSubThings()) ? 500 : 290;
                redoButton = __instance.AddButton("redo", null, null, "ButtonVerySmall", xOnFundament, 420, "undo");

                // Flip the icon horizontally
                Transform iconQuad = redoButton.transform.Find("IconQuad");
                Vector3 scale = iconQuad.localScale;
                iconQuad.localScale = new Vector3(-scale.x, scale.y, scale.z);
            }
            DebugLog.Log(String.Format("UUB UndoCount={0} RedoCount={1}", Main.thingPartStateHistory.UndoCount(tuple), Main.thingPartStateHistory.RedoCount(tuple)));
        }
    }

    [HarmonyPatch(typeof(ThingPartDialog), nameof(ThingPartDialog.SwitchToState))]
    public static class DoNotMemorizeWhenChangingStates {
        public static void Prefix()
        {
            RewriteThingPartMemorizeForUndo.disableMemorize = true;
        }

        public static void Postfix()
        {
            RewriteThingPartMemorizeForUndo.disableMemorize = false;
        }
    }

    [HarmonyPatch(typeof(ThingPartDialog), "OnClick")]
    public static class HandleThingPartDialogClicks {
        public static void Postfix(ThingPartDialog __instance, string contextName)
        {
            if (contextName.Equals("redo")) {
                ThingPart tp = __instance.thingPart();
                var tuple = Main.GetThingPartStateTuple(tp);
                if (Main.thingPartStateHistory.RedoCount(tuple) > 0) {
                    ThingPartStateHistoryEntry saved = Main.thingPartStateHistory.Redo(tuple, new ThingPartStateHistoryEntry(tp.states[tp.currentState]));
                    DebugLog.Log("Redoing to {0}", saved);
                    tp.transform.localPosition = saved.position;
                    tp.transform.localEulerAngles = saved.rotation;
                    tp.transform.localScale = saved.scale;
                    tp.material.color = saved.color;

                    RewriteThingPartMemorizeForUndo.disableMemorize = true;
                    tp.SetStatePropertiesByTransform(false);
                    RewriteThingPartMemorizeForUndo.disableMemorize = false;

                    if (tp.materialType == MaterialTypes.Transparent)
                        tp.UpdateMaterial();

                    DebugLog.Log(String.Format("[R] UndoCount={0} RedoCount={1}", Main.thingPartStateHistory.UndoCount(tuple), Main.thingPartStateHistory.RedoCount(tuple)));
                } else {
                    Managers.soundManager.Play("no", __instance.transform, 0.5f, false, false);
                }
                __instance.UpdateUndoButton();
            }
        }
    }
}

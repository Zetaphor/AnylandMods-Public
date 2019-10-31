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

        private static string IdentifyThingPartState(Tuple<ThingPart, int> thingPartState)
        {
            return String.Format("{0}:{1}", thingPartState.Item1.guid, thingPartState.Item2);
        }

        internal static Tuple<ThingPart, int> GetThingPartStateTuple(ThingPart thingPart) => new Tuple<ThingPart, int>(thingPart, thingPart.currentState);
    }

    [HarmonyPatch(typeof(ThingPart), "MemorizeForUndo")]
    public static class RewriteThingPartMemorizeForUndo {
        public static bool Prefix(ThingPart __instance)
        {
            if (__instance.GetIsOfThingBeingEdited()) {
                if (__instance.currentState > __instance.states.Count - 1) {
                    __instance.currentState = 0;
                }
                var tuple = Main.GetThingPartStateTuple(__instance);
                Main.thingPartStateHistory.PushState(tuple, new ThingPartStateHistoryEntry(__instance.states[__instance.currentState]));
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
                ThingPartStateHistoryEntry saved = Main.thingPartStateHistory.Undo(tuple);
                __instance.transform.localPosition = saved.position;
                __instance.transform.localEulerAngles = saved.rotation;
                __instance.transform.localScale = saved.scale;
                __instance.material.color = saved.color;
                __instance.SetStatePropertiesByTransform(false);
                if (__instance.materialType == MaterialTypes.Transparent)
                    __instance.UpdateMaterial();
            } else {
                Managers.soundManager.Play("no", __instance.transform, 0.5f, false, false);
            }

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
}

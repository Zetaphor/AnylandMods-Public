using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using UnityEngine;
using Harmony;

namespace AnylandMods.Improvements
{
    public static class Main {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            return true;
        }
    }

    [HarmonyPatch(typeof(HandDot), "HandleScalingOfWholeThing")]
    public static class ScaleOnlyActiveState {
        public static bool Enabled { get; set; } = true;

        public static void Prefix(Transform thingTransform, ref Vector3 __state)
        {
            __state = thingTransform.localScale;
        }

        public static void Postfix(Transform thingTransform, Vector3 __state)
        {
            float ratio = thingTransform.localScale.magnitude / __state.magnitude;
            thingTransform.localScale = __state;

            foreach (ThingPart tp in thingTransform.GetComponentsInChildren<ThingPart>()) {
                if (tp.transform.parent == thingTransform) {
                    tp.transform.localPosition *= ratio;
                    tp.transform.localScale *= ratio;
                    tp.SetStatePropertiesByTransform(false);
                }
            }
        }
    }
}

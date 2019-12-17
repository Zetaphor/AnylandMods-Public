using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;

namespace AnylandMods {
    public static class Util {
        public static Thing LastContextLaseredThing { get; internal set; } = null;
    }

    namespace FoundationPatches {
        [HarmonyPatch(typeof(ThingDialog), nameof(ThingDialog.Start))]
        public static class CaptureLastContextLaseredThing {
            public static void Postfix(ThingDialog __instance)
            {
                Util.LastContextLaseredThing = __instance.thing;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using System.Reflection;

namespace AnylandMods
{
    public static partial class Foundation
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static Menu ModMenu { get; private set; }

        static Foundation()
        {
            ModMenu = new Menu();
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            return true;
        }
    }

    /*public class MethodReplaceTranspiler {
        private MethodInfo oldMethod, newMethod;

        public MethodReplaceTranspiler(MethodInfo oldMethod, MethodInfo newMethod)
        {
            this.oldMethod = oldMethod;
            this.newMethod = newMethod;
        }

        public MethodReplaceTranspiler(Type oldType, string oldMethodName, Type newType, string newMethodName)
        {
            this.oldMethod = AccessTools.Method(oldType, oldMethodName);
            this.newMethod = AccessTools.Method(newType, newMethodName);
        }

        public IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {

            }
        }
    }*/

    [HarmonyPatch(typeof(MainDialog), nameof(MainDialog.Start))]
    public static class AddModMenuButton {
        public static void Postfix(MainDialog __instance)
        {
            __instance.AddButton("modMenu", null, "Mods", "ButtonSmallCentered", -200, 420, textColor: TextColor.Blue, ignoreTextPositioning: true);
        }
    }

    [HarmonyPatch(typeof(MainDialog), nameof(MainDialog.OnClick))]
    public static class HandleModMenuButtonClick {
        public static void Postfix(MainDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("modMenu")) {
                MenuDialog.SwitchTo(Foundation.ModMenu, __instance.hand());
            }
        }
    }
}

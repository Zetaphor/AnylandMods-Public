using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using UnityModManagerNet;

namespace AnylandMods.GodMode
{
    public static class Main
    {
        public static bool enabled;
		public static bool gmEnabled = false;
        public static UnityModManager.ModEntry mod;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            return true;
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.Start))]
    public static class AddButton
    {
        public static void Postfix(OwnProfileDialog __instance)
        {
            __instance.AddCheckbox("godMode", null, "Enable God Mode", 0, 300, Main.gmEnabled, textColor: TextColor.Blue, extraIcon: ExtraIcon.Unlocked);
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.OnClick), new Type[] { typeof(string), typeof(string), typeof(bool), typeof(GameObject) })]
    public static class HandleButtonClick
    {
        public static void Postfix(OwnProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName == "godMode")
            {
                Main.gmEnabled = state;
            }
        }
    }

    [HarmonyPatch(typeof(AreaManager), nameof(AreaManager.weAreOwnerOfCurrentArea), MethodType.Getter)]
    public static class ForceOwner
    {
        public static bool Prefix(ref bool __result)
        {
            __result = Main.gmEnabled;
            return !__result;
        }
    }

    [HarmonyPatch(typeof(AreaManager), nameof(AreaManager.weAreEditorOfCurrentArea), MethodType.Getter)]
    public static class ForceEditor
    {
        public static bool Prefix(ref bool __result)
        {
            __result = Main.gmEnabled;
            return !__result;
        }
    }
}
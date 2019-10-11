using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using UnityModManagerNet;

namespace AnylandMods.VehicleUpdate
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
    }

    [HarmonyPatch(typeof(ThingPartAttributesDialog), nameof(ThingPartAttributesDialog.Start))]
    public static class AddControllableButton
    {
        public static void Postfix(ThingPartAttributesDialog __instance)
        {
            __instance.AddButton("controllable", null, "Control Settings", "ButtonCompactNoIcon", 0, -500, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(ThingPartAttributesDialog), nameof(ThingPartAttributesDialog.OnClick))]
    public static class ControllableButtonOnClick
    {
        public static void Postfix(ThingPartAttributesDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName == "controllable")
            {
                __instance.SwitchTo(DialogType.Controllable);
            }
        }
    }

    [HarmonyPatch(typeof(DialogManager), nameof(DialogManager.GetDialogObject), new Type[] { typeof(DialogType) })]
    public static class EnableControllableDialog
    {
        public static void Postfix(DialogManager __instance, ref GameObject __result, DialogType dialogType)
        {
            if (dialogType == DialogType.Controllable)
            {
                __result.AddComponent<ControllableDialog>();
            }
        }
    }
}
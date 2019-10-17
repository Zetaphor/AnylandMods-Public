using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;

namespace AnylandMods
{
    public static class Foundation
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static Menu ModMenu { get; private set; }

        static Foundation()
        {
            ModMenu = new Menu();
            for (int i=0; i<12; ++i) {
                var b = new MenuButton("test" + i.ToString(), "Test Item #" + i.ToString());
                b.Action += delegate {
                    FileLog.Log("Test Item #" + i.ToString() + " selected");
                };
                ModMenu.Add(b);
            }
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            mod = modEntry;
            return true;
        }
    }

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
                MenuDialog.SwitchTo(Managers.dialogManager, Foundation.ModMenu, __instance.hand());
            }
        }
    }
}

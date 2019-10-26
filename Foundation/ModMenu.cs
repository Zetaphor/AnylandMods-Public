using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using System.Reflection;

namespace AnylandMods {
    public static class ModMenu {
        public static Menu Menu { get; private set; }
        private static int idCount = 0;

        static ModMenu()
        {
            Menu = new Menu();
        }

        private static string MakeUniqueId(HarmonyInstance harmony) => String.Format("{0}.#{1}", harmony.Id, idCount++);

        public static MenuButton AddButton(HarmonyInstance harmony, string text, MenuButton.ItemAction onClick = null)
        {
            MenuButton button = new MenuButton(MakeUniqueId(harmony), text);
            if (onClick != null)
                button.Action += onClick;
            Menu.Add(button);
            return button;
        }

        public static MenuCheckbox AddCheckbox(HarmonyInstance harmony, string text, MenuCheckbox.DataItemAction onClick = null)
        {
            MenuCheckbox checkbox = new MenuCheckbox(MakeUniqueId(harmony), text);
            if (onClick != null)
                checkbox.Action += onClick;
            Menu.Add(checkbox);
            return checkbox;
        }
    }

    namespace FoundationPatches {
        [HarmonyPatch(typeof(MainDialog), nameof(MainDialog.Start))]
        static class AddModMenuButton {
            public static void Postfix(MainDialog __instance)
            {
                __instance.AddButton("modMenu", null, "Mods", "ButtonSmallCentered", -200, 420, textColor: TextColor.Blue, ignoreTextPositioning: true);
            }
        }

        [HarmonyPatch(typeof(MainDialog), nameof(MainDialog.OnClick))]
        static class HandleModMenuButtonClick {
            public static void Postfix(MainDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
            {
                if (contextName.Equals("modMenu")) {
                    ModMenu.Menu.SetBackButton(DialogType.Main);
                    MenuDialog.SwitchTo(ModMenu.Menu, __instance.hand());
                }
            }
        }
    }
}

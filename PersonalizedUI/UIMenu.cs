using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AnylandMods.PersonalizedUI {
    static class UIMenu {
        public static Menu Menu { get; private set; }
        private static Regex thingIDRegex;

        static UIMenu()
        {
            thingIDRegex = new Regex("^[0-9A-F]{24}$", RegexOptions.IgnoreCase);

            MenuCheckbox chkHideFundament = new MenuCheckbox("hideFundament", "Hide Default Bkgd");
            chkHideFundament.Value = Main.config.HideFundament;
            chkHideFundament.ExtraIcon = ExtraIcon.Invisible;
            chkHideFundament.Action += chkHideFundament_Action;

            MenuCheckbox chkDynamic = new MenuCheckbox("dynamic", "Script + Collide");
            chkDynamic.Value = Main.config.Dynamic;
            chkDynamic.ExtraIcon = ExtraIcon.Holdable;
            chkDynamic.Action += chkDynamic_Action;

            MenuButton btnSetFundamentTID = new MenuButton("setFundamentTID", "Select Thing");
            btnSetFundamentTID.Action += btnSetFundamentTID_Action;

            MenuButton btnResetBackground = new MenuButton("resetFundamentTID", "Reset Background");
            btnResetBackground.Action += btnResetBackground_Action;

            Menu = new Menu("GUI Appearance");
            Menu.Add(chkHideFundament);
            Menu.Add(chkDynamic);
            Menu.Add(btnSetFundamentTID);
            Menu.Add(btnResetBackground);
            Menu.SetBackButton(ModMenu.Menu);
        }

        private static void btnResetBackground_Action(string id, Dialog dialog)
        {
            Main.config.HideFundament = false;
            Main.config.FundamentTID = "";
            Main.config.Dynamic = false;
            Main.config.Save();
            Managers.soundManager.Play("success", dialog.transform, 0.2f);
            MenuDialog.SwitchTo(ModMenu.Menu, dialog.hand(), dialog.tabName);
        }

        private static void btnSetFundamentTID_Action(string id, Dialog dialog)
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            var mode = thingIDRegex.IsMatch(clipboard) ? SetFundamentTIDDialog.Mode.PreviewNewFundament : SetFundamentTIDDialog.Mode.ThingIDNotInClipboard;
            var par = new SetFundamentTIDDialog.Params(mode, clipboard);
            CustomDialog.SwitchTo<SetFundamentTIDDialog>(par, dialog.hand(), dialog.tabName);
        }

        private static void chkHideFundament_Action(string id, Dialog dialog, bool value)
        {
            Main.config.HideFundament = value;
            Main.config.Save();
        }

        private static void chkDynamic_Action(string id, Dialog dialog, bool value)
        {
            if (value) {
                var par = new SetFundamentTIDDialog.Params(SetFundamentTIDDialog.Mode.ConfirmCollision, Main.config.FundamentTID);
                CustomDialog.SwitchTo<SetFundamentTIDDialog>(par, dialog.hand(), dialog.tabName);
            } else {
                Main.config.Dynamic = false;
                Main.config.Save();
            }
        }
    }
}

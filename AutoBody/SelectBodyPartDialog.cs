using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class SelectBodyPartDialog : MenuDialog {
        private AttachmentPointId apid;
        private bool isDelete;

        private class MenuItemHandler {
            private SelectBodyPartDialog dialog;
            private string name;

            public void Handle(string id, Dialog dlg)
            {
                if (dialog.isDelete) {
                    dialog.GetSavedAttachmentList().Remove(name);
                    dialog.Menu = dialog.BuildMenu();
                } else {
                    dialog.SelectItem(name);
                }
            }

            public static MenuItem.ItemAction Handler(SelectBodyPartDialog dialog, string thingName)
            {
                var mih = new MenuItemHandler();
                mih.dialog = dialog;
                mih.name = thingName;
                return mih.Handle;
            }
        }

        public struct Argument {
            public AttachmentPointId point;
            public bool isDelete;

            public Argument(AttachmentPointId point, bool isDelete = false)
            {
                this.point = point;
                this.isDelete = isDelete;
            }
        }

        protected override void InitCustomDialog(object arg = null)
        {
            var arg_ = (Argument)arg;
            apid = arg_.point;
            isDelete = arg_.isDelete;
            base.InitCustomDialog(BuildMenu());
        }

        protected virtual void SelectItem(string thingName)
        {
            Main.SetAttachment(apid, thingName);
        }

        private Menu BuildMenu()
        {
            string title = "";
            switch (apid) {
                case AttachmentPointId.HeadTop: title = "Hat"; break;
                case AttachmentPointId.Head: title = "Head"; break;
                case AttachmentPointId.ArmLeft: title = "Left Arm"; break;
                case AttachmentPointId.ArmRight: title = "Right Arm"; break;
                case AttachmentPointId.TorsoUpper: title = "Upper Torso"; break;
                case AttachmentPointId.TorsoLower: title = "Lower Torso"; break;
                case AttachmentPointId.LegLeft: title = "Left Leg"; break;
                case AttachmentPointId.LegRight: title = "Right Leg"; break;
                case AttachmentPointId.HandLeft: title = "Left Hand*"; break;
                case AttachmentPointId.HandRight: title = "Right Hand*"; break;
            }

            var menu = new Menu(title);
            menu.SetBackButton(Main.pointMenu);
            menu.TwoColumns = true;
            menu.DialogClose += Menu_DialogClose;

            if (isDelete) {
                var btnConfirmDelete = new MenuButton("confirmDelete", "Confirm");
                btnConfirmDelete.Action += BtnConfirmDelete_Action;
                menu.Add(btnConfirmDelete);

                var btnCancelDelete = new MenuButton("cancelDelete", "Undo Deletion");
                btnCancelDelete.TextColor = TextColor.Gold;
                btnCancelDelete.Action += BtnCancelDelete_Action;
                menu.Add(btnCancelDelete);
            } else {
                var btnSave = new MenuButton("save", "+ Save");
                btnSave.TextColor = TextColor.Green;
                btnSave.Action += BtnSave_Action;
                menu.Add(btnSave);

                var btnDelete = new MenuButton("delete", "- Delete");
                btnDelete.TextColor = TextColor.Red;
                btnDelete.Action += BtnDelete_Action;
                menu.Add(btnDelete);
            }

            if (ShouldShowDetachButton()) {
                var btnDetach = new MenuButton("detach", "(None)");
                btnDetach.TextColor = TextColor.Blue;
                btnDetach.Action += BtnDetach_Action;
                menu.Add(btnDetach);
            }

            foreach (string k in GetSelectionNames()) {
                var btn = new MenuButton("attach_" + k, k);
                if (isDelete) {
                    btn.TextColor = TextColor.Red;
                    btn.Text = "- " + btn.Text;
                }
                btn.Action += MenuItemHandler.Handler(this, k);
                menu.Add(btn);
            }

            FinalizeMenu(menu);
            return menu;
        }

        protected virtual void FinalizeMenu(Menu menu)
        {
        }

        protected virtual bool ShouldShowDetachButton()
        {
            return true;
        }

        protected virtual IEnumerable<string> GetSelectionNames()
        {
            return GetSavedAttachmentList().ThingNames;
        }

        private void BtnDetach_Action(string id, Dialog dialog)
        {
            DoDetach();
        }

        protected virtual void DoDetach()
        {
            Main.SetAttachment(apid, "");
        }

        private void BtnCancelDelete_Action(string id, Dialog dialog)
        {
            Main.config.Load();
            isDelete = false;
            Menu = BuildMenu();
        }

        private void BtnConfirmDelete_Action(string id, Dialog dialog)
        {
            Main.config.Save();
            isDelete = false;
            Menu = BuildMenu();
        }

        private void BtnDelete_Action(string id, Dialog dialog)
        {
            isDelete = true;
            Menu = BuildMenu();
        }

        private void Menu_DialogClose(MenuDialog obj)
        {
            Our.SetPreviousMode();
        }

        public new void Start()
        {
            base.Start();
            AddMirror();
            Our.SetMode(EditModes.Body);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveMirror();
            Managers.personManager.ShowOurSecondaryDots(false);
        }

        protected virtual SavedAttachmentList GetSavedAttachmentList()
        {
            return Main.config.GetListForAttachmentPoint(apid);
        }

        protected virtual bool DoSave()
        {
            bool isHand = (apid == AttachmentPointId.HandLeft || apid == AttachmentPointId.HandRight);
            if (GetSavedAttachmentList().AddCurrent(isHand) is string name) {
                Person ourPerson = Managers.personManager.ourPerson;
                if (apid == AttachmentPointId.LegLeft) {
                    Main.config.LegPosLeft[name] = ourPerson.AttachmentPointLegLeft.transform.localPosition;
                    Main.config.LegRotLeft[name] = ourPerson.AttachmentPointLegLeft.transform.localEulerAngles;
                } else if (apid == AttachmentPointId.LegRight) {
                    Main.config.LegPosRight[name] = ourPerson.AttachmentPointLegRight.transform.localPosition;
                    Main.config.LegRotRight[name] = ourPerson.AttachmentPointLegRight.transform.localEulerAngles;
                }
                Main.config.Save();
                return true;
            } else {
                return false;
            }
        }

        private void BtnSave_Action(string id, Dialog dialog)
        {
            if (DoSave()) {
                Managers.soundManager.Play("success", transform, 0.2f);
                Menu = BuildMenu();
            }
        }
    }
}

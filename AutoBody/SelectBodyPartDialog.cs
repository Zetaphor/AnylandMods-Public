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
            private AttachmentPointId apid;
            private bool isDelete;
            private string name;

            public void Handle(string id, Dialog dialog)
            {
                if (isDelete) {
                    Main.config.GetListForAttachmentPoint(apid).Remove(name);
                    SwitchTo<SelectBodyPartDialog>(new Argument(apid, true), dialog.hand(), dialog.tabName);
                } else {
                    Main.SetAttachment(apid, name);
                }
            }

            public static MenuItem.ItemAction Handler(AttachmentPointId point, string thingName, bool isDelete)
            {
                var mih = new MenuItemHandler();
                mih.apid = point;
                mih.isDelete = isDelete;
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

            var btnDetach = new MenuButton("detach", "(None)");
            btnDetach.TextColor = TextColor.Blue;
            btnDetach.Action += BtnDetach_Action;
            menu.Add(btnDetach);

            foreach (string k in Main.config.GetListForAttachmentPoint(apid).ThingNames) {
                var btn = new MenuButton("attach_" + k, k);
                if (isDelete) {
                    btn.TextColor = TextColor.Red;
                    btn.Text = "- " + btn.Text;
                }
                btn.Action += MenuItemHandler.Handler(apid, k, isDelete);
                menu.Add(btn);
            }
            
            base.InitCustomDialog(menu);
        }

        private void BtnDetach_Action(string id, Dialog dialog)
        {
            Main.SetAttachment(apid, "");
        }

        private void BtnCancelDelete_Action(string id, Dialog dialog)
        {
            Main.config.Load();
            SwitchTo<SelectBodyPartDialog>(new Argument(apid, false), dialog.hand(), dialog.tabName);
        }

        private void BtnConfirmDelete_Action(string id, Dialog dialog)
        {
            Main.config.Save();
            SwitchTo<SelectBodyPartDialog>(new Argument(apid, false), dialog.hand(), dialog.tabName);
        }

        private void BtnDelete_Action(string id, Dialog dialog)
        {
            SwitchTo<SelectBodyPartDialog>(new Argument(apid, true), dialog.hand(), dialog.tabName);
        }

        private void Menu_DialogClose(MenuDialog obj)
        {
            Our.SetPreviousMode();
        }

        public new void Start()
        {
            base.Start();
            AddMirror();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveMirror();
            Managers.personManager.ShowOurSecondaryDots(false);
        }

        private void BtnSave_Action(string id, Dialog dialog)
        {
            if (Main.config.GetListForAttachmentPoint(apid).AddCurrent() is string name) {
                Person ourPerson = Managers.personManager.ourPerson;
                if (apid == AttachmentPointId.LegLeft) {
                    Main.config.LegPosLeft[name] = ourPerson.AttachmentPointLegLeft.transform.localPosition;
                    Main.config.LegRotLeft[name] = ourPerson.AttachmentPointLegLeft.transform.localEulerAngles;
                } else if (apid == AttachmentPointId.LegRight) {
                    Main.config.LegPosRight[name] = ourPerson.AttachmentPointLegRight.transform.localPosition;
                    Main.config.LegRotRight[name] = ourPerson.AttachmentPointLegRight.transform.localEulerAngles;
                }
                Main.config.Save();
                Managers.soundManager.Play("success", transform, 0.2f);
                SwitchTo<SelectBodyPartDialog>(new Argument(apid), dialog.hand(), dialog.tabName);
            }
        }
    }
}

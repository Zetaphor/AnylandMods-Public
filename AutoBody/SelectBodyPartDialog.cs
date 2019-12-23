using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class SelectBodyPartDialog : MenuDialog {
        private AttachmentPointId apid;
        private bool isDelete;
        private bool isLockedToPlayspace;

        protected static Vector3? SavedLegPosLL { get; set; } = null;
        protected static Vector3? SavedLegPosLW { get; set; } = null;
        protected static Vector3? SavedLegPosRL { get; set; } = null;
        protected static Vector3? SavedLegPosRW { get; set; } = null;

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

            if (apid == AttachmentPointId.LegLeft || apid == AttachmentPointId.LegRight) {
                Person ourPerson = Managers.personManager.ourPerson;
                isLockedToPlayspace = (ourPerson.GetAttachmentPointById(apid).transform.parent != ourPerson.Torso.transform);
                var btnToggleLock = new MenuButton("toggleLock", isLockedToPlayspace ? "Unlock from Playspace" : "Lock to Playspace");
                btnToggleLock.TextColor = TextColor.Blue;
                btnToggleLock.Action += BtnToggleLock_Action;
                menu.Add(btnToggleLock);

                var btnCopyLegPos = new MenuButton("copyLegPos", "Copy Position");
                btnCopyLegPos.TextColor = TextColor.Blue;
                btnCopyLegPos.Action += BtnCopyLegPos_Action;
                menu.Add(btnCopyLegPos);

                var btnPasteLegPos = new MenuButton("pasteLegPos", "Paste Local Pos.");
                btnPasteLegPos.TextColor = TextColor.Blue;
                btnPasteLegPos.Action += BtnPasteLegPos_Action;
                menu.Add(btnPasteLegPos);

                var btnPasteLegPosWorld = new MenuButton("pasteLegPosWorld", "Paste World Pos.");
                btnPasteLegPosWorld.TextColor = TextColor.Blue;
                btnPasteLegPosWorld.Action += BtnPasteLegPosWorld_Action;
                menu.Add(btnPasteLegPosWorld);
            }

            if (ShouldShowDetachButton()) {
                var btnDetach = new MenuButton("detach", "(None)");
                btnDetach.TextColor = TextColor.White;
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

        private void BtnToggleLock_Action(string id, Dialog dialog)
        {
            Main.SetLegPlayspaceLock(apid, !isLockedToPlayspace);
            Menu = BuildMenu();
        }

        private void BtnPasteLegPosWorld_Action(string id, Dialog dialog)
        {
            if (apid == AttachmentPointId.LegLeft) {
                if (SavedLegPosLW.HasValue)
                    Managers.personManager.ourPerson.AttachmentPointLegLeft.transform.position = SavedLegPosLW.Value;
                else
                    Managers.errorManager.BeepError();
            } else if (apid == AttachmentPointId.LegRight) {
                if (SavedLegPosRW.HasValue)
                    Managers.personManager.ourPerson.AttachmentPointLegRight.transform.position = SavedLegPosRW.Value;
                else
                    Managers.errorManager.BeepError();
            }
        }

        private void BtnPasteLegPos_Action(string id, Dialog dialog)
        {
            if (apid == AttachmentPointId.LegLeft) {
                if (SavedLegPosLL.HasValue)
                    Managers.personManager.ourPerson.AttachmentPointLegLeft.transform.localPosition = SavedLegPosLL.Value;
                else
                    Managers.errorManager.BeepError();
            } else if (apid == AttachmentPointId.LegRight) {
                if (SavedLegPosRL.HasValue)
                    Managers.personManager.ourPerson.AttachmentPointLegRight.transform.localPosition = SavedLegPosRL.Value;
                else
                    Managers.errorManager.BeepError();
            }
        }

        private void BtnCopyLegPos_Action(string id, Dialog dialog)
        {
            Vector3 toCopy = Vector3.zero;
            if (apid == AttachmentPointId.LegLeft) {
                SavedLegPosLL = toCopy = Managers.personManager.ourPerson.AttachmentPointLegLeft.transform.localPosition;
                SavedLegPosLW = Managers.personManager.ourPerson.AttachmentPointLegLeft.transform.position;
            } else if (apid == AttachmentPointId.LegRight) {
                SavedLegPosRL = toCopy = Managers.personManager.ourPerson.AttachmentPointLegRight.transform.localPosition;
                SavedLegPosRW = Managers.personManager.ourPerson.AttachmentPointLegRight.transform.position;
            }
            GUIUtility.systemCopyBuffer = String.Format("xa{0} goto {1:.3} {2:.3} {3:.3} ", (apid == AttachmentPointId.LegLeft) ? 6 : 7, toCopy.x, toCopy.y, toCopy.z);
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
            } else {
                Managers.errorManager.BeepError();
            }
        }
    }
}

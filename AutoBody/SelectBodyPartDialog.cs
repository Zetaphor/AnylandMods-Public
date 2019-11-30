using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.AutoBody {
    class SelectBodyPartDialog : MenuDialog {
        private AttachmentPointId apid;

        private class MenuItemHandler {
            private AttachmentPointId apid;
            private string name;

            public void Handle(string id, Dialog dialog)
            {
                Main.SetAttachment(apid, name);
            }

            public static MenuItem.ItemAction Handler(AttachmentPointId point, string thingName)
            {
                var mih = new MenuItemHandler();
                mih.apid = point;
                mih.name = thingName;
                return mih.Handle;
            }
        }

        protected override void InitCustomDialog(object arg = null)
        {
            apid = (AttachmentPointId)arg;
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
            var btnSave = new MenuButton("save", "+ Save");
            btnSave.TextColor = TextColor.Green;
            btnSave.Action += BtnSave_Action;
            menu.Add(btnSave);
            foreach (string k in Main.config.GetListForAttachmentPoint(apid).ThingNames) {
                var btn = new MenuButton("attach_" + k, k);
                btn.Action += MenuItemHandler.Handler(apid, k);
                menu.Add(btn);
            }
            
            base.InitCustomDialog(menu);
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
                SwitchTo<SelectBodyPartDialog>(apid, dialog.hand(), dialog.tabName);
            }
        }
    }
}

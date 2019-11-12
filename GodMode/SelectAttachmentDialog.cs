using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Harmony;

namespace AnylandMods.GodMode {
    class SelectAttachmentDialog : MenuDialog {
        private Dictionary<AttachmentPointId, string> attachedThings;

        protected override void InitCustomDialog(object arg = null)
        {
            attachedThings = new Dictionary<AttachmentPointId, string>();
            var person = (Person)arg;
            var menu = new Menu();
            menu.SetBackButton(DialogType.Thing);

            foreach (string idstr in Enum.GetNames(typeof(AttachmentPointId))) {
                var id = (AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), idstr);
                if (id != AttachmentPointId.None) {
                    GameObject gobj = person.GetThingOnAttachmentPointById(id);
                    if (gobj != null) {
                        attachedThings[id] = gobj.GetComponent<Thing>().thingId;
                        menu.Add(new MenuButton(idstr, idstr));
                    }
                }
            }

            base.InitCustomDialog(menu);
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            base.OnClick(contextName, contextId, state, thisButton);
            try {
                var id = (AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), contextName);
                Our.thingIdOfInterest = attachedThings[id];
                Managers.dialogManager.SwitchToNewDialog(DialogType.Thing, hand, tabName);
            } catch (ArgumentException) { }
        }
    }

    [HarmonyPatch(typeof(ProfileDialog), nameof(ProfileDialog.Start))]
    public static class AddSelectAttachmentButton {
        public static void Postfix(ProfileDialog __instance)
        {
            __instance.AddButton("selectAttachment", null, "Atchmt", "ButtonSmallCentered", 100, -425, textSizeFactor: 0.75f, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(ProfileDialog), nameof(ProfileDialog.OnClick))]
    public static class HandleSelectAttachmentClick {
        public static void Postfix(ProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("selectAttachment")) {
                Person person = Managers.personManager.GetPersonById(Our.personIdOfInterest);
                CustomDialog.SwitchTo<SelectAttachmentDialog>(person, __instance.hand(), __instance.tabName);
            }
        }
    }
}

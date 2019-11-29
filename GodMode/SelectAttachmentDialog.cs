using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Harmony;

namespace AnylandMods.GodMode {
    class MenuButtonWithThing : MenuButton {
        public string ThingId { get; set; }

        public MenuButtonWithThing(string id, string text, string thingId)
            : base(id, text)
        {
            ThingId = thingId;
        }

        public override GameObject Create(Dialog dialog, int xOnFundament, int yOnFundament)
        {
            GameObject button = base.Create(dialog, xOnFundament, yOnFundament);
            Managers.thingManager.InstantiateThingOnDialogViaCache(ThingRequestContext.LocalTest, ThingId, button.transform, new Vector3(-600.0f, 0.0f, 0.0f), allowGrabbing: true, useDefaultRotation: true);
            return button;
        }
    }

    class SelectAttachmentDialog : MenuDialog {
        private Dictionary<string, Thing> things;

        protected override void InitCustomDialog(object arg = null)
        {
            things = new Dictionary<string, Thing>();
            var person = (Person)arg;
            var menu = new Menu("Attachments");
            menu.SetBackButton(DialogType.Thing);
            menu.TwoColumns = true;

            foreach (Thing thing in person.Rig.GetComponentsInChildren<Thing>()) {
                string contextName = "button" + things.Count.ToString();
                things[contextName] = thing;
                menu.Add(new MenuButtonWithThing(contextName, thing.givenName, thing.thingId));
            }

            base.InitCustomDialog(menu);
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            base.OnClick(contextName, contextId, state, thisButton);
            if (things.ContainsKey(contextName)) {
                Thing thing = things[contextName];
                Our.thingIdOfInterest = thing.thingId;
                GameObject gobj = Managers.dialogManager.SwitchToNewDialog(DialogType.Thing, hand, tabName);
                gobj.GetComponent<ThingDialog>().thing = thing;
            }
        }
    }

    [HarmonyPatch(typeof(ProfileDialog), nameof(ProfileDialog.Start))]
    public static class AddSelectAttachmentButton {
        public static void Postfix(ProfileDialog __instance)
        {
            __instance.AddButton("selectAttachment", null, "Atchmt", "ButtonSmallCentered", 100, -425, textSizeFactor: 0.75f, textColor: TextColor.Blue);
            __instance.AddButton("triggerEvent", null, "Event", "ButtonSmallCentered", 0, -425, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(ProfileDialog), nameof(ProfileDialog.OnClick))]
    public static class HandleSelectAttachmentClick {
        public static void Postfix(ProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            Person person = __instance.personThisIsOf();
            if (contextName.Equals("selectAttachment")) {
                if (person != null) {
                    CustomDialog.SwitchTo<SelectAttachmentDialog>(person, __instance.hand(), __instance.tabName);
                } else {
                    Managers.soundManager.Play("no", __instance.transform, 0.5f);
                }
            } else if (contextName.Equals("triggerEvent")) {
                if (person != null) {
                    CustomDialog.SwitchTo<TriggerEventDialog>(person, __instance.hand(), __instance.tabName);
                } else {
                    Managers.soundManager.Play("no", __instance.transform, 0.5f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.Start))]
    public static class AddSelectAttachmentButton2 {
        public static void Postfix(OwnProfileDialog __instance)
        {
            __instance.AddButton("selectAttachment", null, "Atchmt", "ButtonSmallCentered", 100, -425, textSizeFactor: 0.75f, textColor: TextColor.Blue);
            __instance.AddButton("triggerEvent", null, "Event", "ButtonSmallCentered", 0, -425, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.OnClick))]
    public static class HandleSelectAttachmentClick2 {
        public static void Postfix(OwnProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("selectAttachment")) {
                CustomDialog.SwitchTo<SelectAttachmentDialog>(Managers.personManager.ourPerson, __instance.hand(), __instance.tabName);
            } else if (contextName.Equals("triggerEvent")) {
                CustomDialog.SwitchTo<TriggerEventDialog>(Managers.personManager.ourPerson, __instance.hand(), __instance.tabName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Harmony;

namespace AnylandMods.GodMode {
    class TriggerEventDialog : MenuDialog {
        private static Menu eventMenu;
        
        static TriggerEventDialog()
        {
            eventMenu = new Menu("Trigger Event");
            eventMenu.SetBackButton(DialogType.Thing);
            foreach (string item in Enum.GetNames(typeof(StateListener.EventType))) {
                eventMenu.Add(new MenuButton(item, item));
            }
        }

        private Thing thing;
        private Person person;
        private bool isPerson;
        private StateListener.EventType eventType;

        protected override void InitCustomDialog(object arg = null)
        {
            base.InitCustomDialog(eventMenu);
            if (arg is Thing) {
                thing = (Thing)arg;
                isPerson = false;
            } else {
                person = (Person)arg;
                isPerson = true;
            }
        }

        private void StringCallback(string data)
        {
            var things = new List<Thing>();
            if (isPerson) {
                foreach (string name in Enum.GetNames(typeof(AttachmentPointId))) {
                    if (!name.Equals("None")) {
                        var pointId = (AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), name);
                        try {
                            GameObject gobj = person.GetThingOnAttachmentPointById(pointId);
                            if (gobj != null) {
                                Thing thing = gobj.GetComponent<Thing>();
                                if (thing != null) {
                                    things.Add(thing);
                                }
                            }
                        } catch (NullReferenceException) { }
                    }
                }
            } else {
                things.Add(thing);
            }

            foreach (Thing thing in things) {
                thing.TriggerEventAsStateAuthority(eventType, data);
            }
            CustomDialog.SwitchTo<TriggerEventDialog>(thing, hand, tabName);
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            base.OnClick(contextName, contextId, state, thisButton);
            try {
                eventType = (StateListener.EventType)Enum.Parse(typeof(StateListener.EventType), contextName);
                Managers.dialogManager.GetInput(StringCallback, placeholderHint: "Data (optional)");
            } catch (ArgumentException) { }
        }
    }

    [HarmonyPatch(typeof(ThingDialog), nameof(ThingDialog.Start))]
    public static class AddTriggerEventButton {
        public static void Postfix(ThingDialog __instance)
        {
            __instance.AddButton("triggerEvent", null, "Event", "ButtonSmallCentered", 275, -425, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(ThingDialog), nameof(ThingDialog.OnClick))]
    public static class HandleTriggerEventClick {
        public static void Postfix(ThingDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("triggerEvent")) {
                CustomDialog.SwitchTo<TriggerEventDialog>(__instance.thing, __instance.hand(), __instance.tabName);
            }
        }
    }
}

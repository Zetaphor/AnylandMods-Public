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
            eventMenu = new Menu();
            eventMenu.SetBackButton(DialogType.Thing);
            foreach (string item in Enum.GetNames(typeof(StateListener.EventType))) {
                eventMenu.Add(new MenuButton(item, item));
            }
        }

        private Thing thing;
        private StateListener.EventType eventType;

        protected override void InitCustomDialog(object arg = null)
        {
            base.InitCustomDialog(eventMenu);
            thing = (Thing)arg;
        }

        private void StringCallback(string data)
        {
            thing.TriggerEventAsStateAuthority(eventType, data);
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

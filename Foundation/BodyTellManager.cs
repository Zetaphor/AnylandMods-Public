using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;

namespace AnylandMods {
    public static class BodyTellManager {
        private static BodyMotionsDialog dummyDialog;
        public static IEnumerable<string> BodyTellList { get; private set; }
        public delegate void UpdateAction();  // since System.Action (without <T>) appears to be missing...
        public delegate void BodyTellHook(string data, bool byScript);
        public static event UpdateAction OnUpdate;
        public static event BodyTellHook ToldByBody;
        
        static BodyTellManager()
        {
            dummyDialog = new BodyMotionsDialog();
        }

        private static string ToLowerCase(string str)
        {
            return str.ToLower();
        }

        public static void Update()
        {
            BodyTellList = dummyDialog.GetTellBodyDataBodyIsListeningFor(dummyDialog.GetMyThingParts()).Select(ToLowerCase);
            if (OnUpdate != null) {
                OnUpdate();
            }
        }

        internal static void TriggerToldByBody(string data, bool byScript)
        {
            if (ToldByBody != null) {
                ToldByBody(data, byScript);
            }
        }

        public static void Trigger(string message)
        {
            Managers.behaviorScriptManager.TriggerTellBodyEventToAttachments(Managers.personManager.ourPerson, message.ToLower(), true);
        }
    }

    namespace FoundationPatches {
        [HarmonyPatch(typeof(PersonManager), nameof(PersonManager.DoAttachThing))]
        public static class UpdateBodyTellListWhenNeeded1 {
            public static void Postfix()
            {
                BodyTellManager.Update();
            }
        }

        [HarmonyPatch(typeof(PersonManager), nameof(PersonManager.InitializeOurPerson))]
        public static class UpdateBodyTellListWhenNeeded2 {
            public static void Postfix()
            {
                BodyTellManager.Update();
            }
        }

        [HarmonyPatch(typeof(HeldThingsRegistrar), nameof(HeldThingsRegistrar.RegisterHold))]
        public static class UpdateBodyTellListWhenNeeded3 {
            public static void Postfix()
            {
                BodyTellManager.Update();
            }
        }

        [HarmonyPatch(typeof(BehaviorScriptManager), nameof(BehaviorScriptManager.TriggerTellBodyEventToAttachments))]
        public static class BodyTellTriggerHook {
            public static void Postfix(Person person, string data, bool weAreStateAuthority)
            {
                if (person.isOurPerson) {
                    BodyTellManager.TriggerToldByBody(data.ToLower(), !weAreStateAuthority);
                }
            }
        }
    }
}

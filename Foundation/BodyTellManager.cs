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
        public static event UpdateAction OnUpdate;
        
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
    }
}

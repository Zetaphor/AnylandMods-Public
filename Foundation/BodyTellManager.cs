using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace AnylandMods {
    public static class BodyTellManager {
        private static BodyMotionsDialog dummyDialog;
        public static IEnumerable<string> BodyTellList { get; private set; }
        public delegate void UpdateAction();  // since System.Action (without <T>) appears to be missing...
        public delegate void BodyTellHook(string tell, TellEventInfo info);
        public static event UpdateAction OnUpdate;
        public static event BodyTellHook ToldByBody;

        public struct TellEventInfo {
            public bool SentByScript { get; set; }
            public ThingPart ThingPart { get; set; }
            public Thing Thing {
                get {
                    if (ThingPart == null)
                        return null;
                    else
                        return ThingPart.GetParentThing();
                }
            }
            public bool IsTrusted {
                get {
                    if (ThingPart == null)
                        return true;
                    else
                        return Managers.personManager.GetIsThisObjectOfOurPerson(ThingPart.gameObject, false);
                }
            }

            public TellEventInfo(ThingPart thingPart, bool sentByScript)
            {
                ThingPart = thingPart;
                SentByScript = sentByScript;
            }
        }
        
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
            OnUpdate?.Invoke();
        }

        internal static void TriggerToldByBody(string tell, bool byScript)
        {
            ToldByBody?.Invoke(tell.ToLower(), new TellEventInfo(FoundationPatches.CaptureThingPartForBodyTellHooks.capturedThingPart, byScript));
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

        [HarmonyPatch(typeof(ThingPart), "HandleTell")]
        public static class CaptureThingPartForBodyTellHooks {
            internal static ThingPart capturedThingPart = null;

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
            {
                var storeToField = new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(CaptureThingPartForBodyTellHooks), "capturedThingPart"));
                foreach (CodeInstruction inst in code) {
                    bool isTTBE2A = false;
                    if (inst.opcode == OpCodes.Callvirt && ((MethodInfo)inst.operand).Name.Equals("TriggerTellBodyEventToAttachments")) {
                        isTTBE2A = true;
                        DebugLog.Log("Adding ThingPart capture code for body tell hooks");
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return storeToField;
                    }

                    yield return inst;

                    if (isTTBE2A) {
                        yield return new CodeInstruction(OpCodes.Ldnull);
                        yield return storeToField;
                    }
                }
            }
        }
    }
}

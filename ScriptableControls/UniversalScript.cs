using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityModManagerNet;
using UnityEngine;
using Harmony;

namespace AnylandMods.ScriptableControls {
    public class UniversalScript {
        public ICollection<string> ScriptLines { get; private set; }

        private string filename;
        private UnityModManager.ModEntry mod;

        public UniversalScript(UnityModManager.ModEntry mod, string filename = "universal.txt")
        {
            ScriptLines = new List<string>();
            this.filename = Path.Combine(mod.Path, filename);
            this.mod = mod;
        }

        public sealed class Tag_ScriptLinesAdded : MonoBehaviour {
            public int[] stateLineCounts;
            public bool ignore = false;
            public void Start()
            {
                enabled = false;
            }
            public void Update() { }
        }

        public void AddScriptToHead()
        {
            Person ourPerson = Managers.personManager.ourPerson;
            var headPart = ourPerson.GetAttachmentPointById(AttachmentPointId.Head).GetComponentInChildren<ThingPart>();
            if (headPart == null) {
                DebugLog.Log("Did not find ThingPart in Head");
                return;
            } else {
                DebugLog.Log("Found ThingPart in Head");
                var tag = headPart.gameObject.GetComponent<Tag_ScriptLinesAdded>();
                if (tag == null || tag.ignore) {
                    int[] stateLineCounts = new int[headPart.states.Count];
                    for (int i=0; i<headPart.states.Count; ++i) {
                        stateLineCounts[i] = headPart.states[i].listeners.Count;
                    }
                    if (tag == null) {
                        headPart.gameObject.AddComponent<Tag_ScriptLinesAdded>().stateLineCounts = stateLineCounts;
                    } else {
                        tag.ignore = false;
                    }
                    foreach (string line in ScriptLines) {
                        StateListener listener = BehaviorScriptParser.GetStateListenerFromScriptLine(line, headPart.GetMyRootThing(), headPart);
                        foreach (ThingPartState state in headPart.states) {
                            state.listeners.Add(listener);
                        }
                    }
                } else {
                    for (int i=0; i<tag.stateLineCounts.Length; ++i) {
                        var listeners = headPart.states[i].listeners;
                        listeners.RemoveRange(tag.stateLineCounts[i], tag.stateLineCounts.Length - tag.stateLineCounts[i]);
                        tag.ignore = true;
                        AddScriptToHead();
                    }
                }
                BodyTellManager.Update();
            }
        }

        public void Load()
        {
            ScriptLines.Clear();
            StreamReader file = null;
            try {
                file = File.OpenText(filename);
                while (!file.EndOfStream) {
                    string line = file.ReadLine().Trim();
                    ScriptLines.Add(line);
                }
            } catch (FileNotFoundException) {
                DebugLog.Log("{0} not found!", filename);
            } finally {
                if (file != null)
                    file.Close();
            }
        }

        public void Save()
        {
            StreamWriter file = File.CreateText(filename);
            foreach (string line in ScriptLines) {
                file.WriteLine(line);
            }
            file.Close();
        }

        public static void LoadAndAddToHead(UnityModManager.ModEntry mod, string filename = "universal.txt")
        {
            var script = new UniversalScript(mod, filename);
            script.Load();
            script.AddScriptToHead();
        }
    }

    /*[HarmonyPatch(typeof(Person), "ConstructAttachments")]
    public static class ConstructAttachmentsHook {
        public static void Postfix()
        {
            Main.universal.Load();
            Main.universal.AddScriptToHead();
        }
    }

    [HarmonyPatch(typeof(Person), "AttachNewThing")]
    public static class AttachNewThingHook {
        private static System.Collections.IEnumerable GetEnumerable(System.Collections.IEnumerator beginning)
        {
            while (beginning.MoveNext()) {
                yield return beginning.Current;
            }
            DebugLog.Log("Finished original coroutine");
            Main.universal.Load();
            Main.universal.AddScriptToHead();
            DebugLog.Log("Finished coroutine extension");
        }

        public static void Postfix(AttachmentPointId attachmentPointId, Person __instance, ref System.Collections.IEnumerator __result)
        {
            DebugLog.LogTemp("AttachNewThingHook.Postfix was called");
            __result = GetEnumerable(__result).GetEnumerator();
        }
    }*/

    [HarmonyPatch(typeof(AttachmentPoint), "Update")]
    public static class KeepScriptInHead {
        public static void Postfix(AttachmentPoint __instance)
        {
            if (__instance.name == "HeadAttachmentPoint") {
                var person = __instance.GetComponentInParent<Person>();
                if (person != null && person.isOurPerson) {
                    var tag = __instance.GetComponent<UniversalScript.Tag_ScriptLinesAdded>();
                    if (tag == null)
                        Main.universal.AddScriptToHead();
                }
            }
        }
    }

    [HarmonyPatch(typeof(CreateDialog), "DoSave")]
    public static class SaveThingHook {
        private static Thing thingScript;

        private static void Hook(SaveThing_Response response)
        {
            if (response.error == null && thingScript.givenName.ToLower() == "--universal script") {
                Main.universal.ScriptLines.Clear();
                foreach (ThingPart part in thingScript.gameObject.GetComponentsInChildren<ThingPart>()) {
                    foreach (ThingPartState state in part.states) {
                        foreach (string line in state.scriptLines) {
                            DebugLog.LogTemp("Found universal script line: {0}", line);
                            Main.universal.ScriptLines.Add(line);
                        }
                    }
                }
                Main.universal.Save();
                Main.universal.AddScriptToHead();
            }
        }

        public static void Prefix(Thing ___thingScript)
        {
            thingScript = ___thingScript;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {
                if (inst.opcode == OpCodes.Ldftn) {
                    var method = (MethodBase)inst.operand;
                    DebugLog.Log("Patching {0}", method.Name);
                    Main.harmony.Patch(method, postfix: new HarmonyMethod(typeof(SaveThingHook), "Hook"));
                    break;
                }
            }
            return code;
        }
    }
}

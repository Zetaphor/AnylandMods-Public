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
                Thing headThing = headPart.GetMyRootThing();
                DebugLog.Log("Found ThingPart in Head");
                if (headThing.GetComponent<Tag_ScriptLinesAdded>() == null) {
                    DebugLog.Log("Adding universal script...");
                    foreach (string line in ScriptLines) {
                        StateListener listener = BehaviorScriptParser.GetStateListenerFromScriptLine(line, headPart.GetMyRootThing(), headPart);
                        foreach (ThingPartState state in headPart.states) {
                            DebugLog.Log("Adding {0}", line);
                            state.listeners.Add(listener);
                        }
                    }
                    headThing.gameObject.AddComponent<Tag_ScriptLinesAdded>();
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
                    DebugLog.Log("Loaded universal script line: {0}", line);
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

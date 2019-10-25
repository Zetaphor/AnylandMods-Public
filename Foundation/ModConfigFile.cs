using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityModManagerNet;

namespace AnylandMods {
    public class ModConfigFile {
        private List<string> lines = null;
        private Dictionary<string, string> keyValuePairs = null;
        private Dictionary<string, int> valueLineNumbers;
        private StringBuilder defaultText;
        private UnityModManager.ModEntry mod;

        public ModConfigFile(UnityModManager.ModEntry mod, string filename = "config.txt")
        {
            this.mod = mod;
            Filename = filename;
            defaultText = new StringBuilder();
            valueLineNumbers = new Dictionary<string, int>();
        }

        public string Filename { get; set; }

        public string Path {
            get {
                return System.IO.Path.Combine(mod.Path, Filename);
            }
        }

        public string ContentText {
            get {
                return String.Join("\n", lines);
            }
        }

        public static bool ParseBool(string str) => (new string[] { "1", "yes", "true", "on" }).Contains(str.ToLower());

        public void AddDefaultLine(string line = "")
        {
            defaultText.AppendLine(line);
        }

        public string this[string key] {
            get {
                return keyValuePairs[key];
            }
            set {
                keyValuePairs[key] = value;
                string line = String.Format("{0}={1}", key, value);
                if (valueLineNumbers.ContainsKey(key)) {
                    lines[valueLineNumbers[key]] = line;
                } else {
                    lines.Add(line);
                    valueLineNumbers[key] = lines.Count - 1;
                }
                ValueChanged(key, value);
            }
        }

        public void Load()
        {
            StreamReader file = null;
            lines = new List<string>();
            keyValuePairs = new Dictionary<string, string>();
            valueLineNumbers.Clear();

            try {
                file = File.OpenText(Path);
                while (!file.EndOfStream) {
                    string line = file.ReadLine().Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int equals = line.IndexOf('=');
                    if (equals != -1) {
                        string key = line.Substring(0, equals).ToLower();
                        string value = line.Substring(equals + 1);
                        keyValuePairs[key] = value;
                        valueLineNumbers[key] = lines.Count;
                        ValueChanged(key, value);
                    } else {
                        Harmony.FileLog.Log(String.Format("[{0}] Warning: Improperly formatted configuration line \"{1}\"", mod.Info.DisplayName, line));
                    }
                    lines.Add(line);
                }
            } catch (FileNotFoundException) {
                File.WriteAllText(Path, defaultText.ToString());
                Load();
            } finally {
                if (file != null) {
                    file.Close();
                }
            }
        }

        protected void SetKeyValueInternally(string key, string value)
        {
            keyValuePairs[key] = value;
        }

        protected virtual void ValueChanged(string key, string newValue)
        {
        }

        public void Save()
        {
            // TODO: Fix this
            File.WriteAllText(Path, ContentText);
        }
    }
}

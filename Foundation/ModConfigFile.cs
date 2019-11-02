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
        private Dictionary<string, string> originalCaps = null;
        private Dictionary<string, string> defaults = null;
        private StringBuilder defaultText;
        private UnityModManager.ModEntry mod;

        public ModConfigFile(UnityModManager.ModEntry mod, string filename = "config.txt")
        {
            this.mod = mod;
            Filename = filename;
            defaultText = new StringBuilder();
            valueLineNumbers = new Dictionary<string, int>();
            originalCaps = new Dictionary<string, string>();
            defaults = new Dictionary<string, string>();
        }

        public string Filename { get; set; }

        public string Path {
            get {
                return System.IO.Path.Combine(mod.Path, Filename);
            }
        }

        public string ContentText {
            get {
                StringBuilder builder = new StringBuilder();
                foreach (string line in lines) {
                    builder.AppendLine(line);
                }
                return builder.ToString();
            }
        }

        public static bool ParseBool(string str) => (new string[] { "1", "yes", "true", "on" }).Contains(str.ToLower());

        public void AddDefaultLine(string line = "")
        {
            defaultText.AppendLine(line);
        }

        public string this[string key] {
            get {
                string v;
                if (keyValuePairs.TryGetValue(key, out v)) {
                    return v;
                } else if (defaults.TryGetValue(key, out v)) {
                    return v;
                } else {
                    return String.Empty;
                }
            }
            set {
                SetKeyValueInternally(key, value);
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
                    lines.Add(line);
                    if (line.Length == 0 || line[0] == '#') continue;
                    int equals = line.IndexOf('=');
                    if (equals != -1) {
                        string key_case = line.Substring(0, equals);
                        string key = key_case.ToLower();
                        originalCaps[key] = key_case;
                        string value = line.Substring(equals + 1);
                        keyValuePairs[key] = value;
                        valueLineNumbers[key] = lines.Count - 1;
                        ValueChanged(key, value);
                    } else {
                        DebugLog.Log(String.Format("[{0}] Warning: Improperly formatted configuration line \"{1}\"", mod.Info.DisplayName, line));
                    }
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

        public void AddDefaultValue(string key, string value = "", bool addDefaultLine = true)
        {
            defaults[key.ToLower()] = value;
            if (addDefaultLine) {
                AddDefaultLine(String.Format("{0}={1}", key, value));
            }
        }

        protected void SetKeyValueInternally(string key, string value)
        {
            key = key.ToLower();
            keyValuePairs[key] = value;
            if (!originalCaps.ContainsKey(key)) {
                originalCaps[key] = key;
            }
            string line = String.Format("{0}={1}", originalCaps[key], value);
            if (valueLineNumbers.ContainsKey(key)) {
                lines[valueLineNumbers[key]] = line;
            } else {
                lines.Add(line);
                valueLineNumbers[key] = lines.Count - 1;
            }
        }

        protected virtual void ValueChanged(string key, string newValue)
        {
        }

        public void Save()
        {
            File.WriteAllText(Path, ContentText);
        }
    }
}

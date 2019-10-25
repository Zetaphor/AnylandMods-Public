using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace AnylandMods.PersonalizedUI {
    class Config {
        public bool HideFundament { get; private set; }
        public string FundamentTID { get; set; }

        private static bool ParseBool(string str) => (new string[] { "1", "yes", "true", "on" }).Contains(str.ToLower());

        public void Load()
        {
            string filename = Path.Combine(Main.mod.Path, "config.txt");
            StreamReader cfgfile = null;

            try {
                cfgfile = File.OpenText(filename);
                while (!cfgfile.EndOfStream) {
                    string line = cfgfile.ReadLine().Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int equals = line.IndexOf('=');
                    if (equals != -1) {
                        string key = line.Substring(0, equals).ToLower();
                        string value = line.Substring(equals + 1);

                        if (key.Equals("hidefundament")) {
                            HideFundament = ParseBool(value);
                        } else if (key.Equals("fundamenttid")) {
                            FundamentTID = value;
                        } else {
                            Harmony.FileLog.Log("[PersonalizedUI] Warning: Unknown configuration key '" + key + "'");
                        }
                    } else {
                        Harmony.FileLog.Log("[PersonalizedUI] Warning: Improperly formatted configuration line '" + line + "'");
                    }
                }
            } catch (FileNotFoundException) {
                string[] lines = new string[] {
                    "# Set HideFundament to 1 to disable dialog backgrounds.",
                    "HideFundament=0",
                    "",
                    "# Fill in a thing ID to display this thing on every dialog.",
                    "FundamentTID="
                };
                File.WriteAllText(filename, String.Join("\n", lines));
            } finally {
                if (cfgfile != null) {
                    cfgfile.Close();
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityModManagerNet;

namespace AnylandMods.PersonalizedUI {
    class ConfigFile : ModConfigFile {
        private bool hideFundament = false;
        private bool dynamic = false;

        public ConfigFile(UnityModManager.ModEntry mod) : base(mod)
        {
            AddDefaultLine("# Set this to True to disable the original dialog background.");
            AddDefaultValue("HideFundament", "False");
            AddDefaultLine();
            AddDefaultLine("# Fill in a thing ID to use it as the default background for dialogs.");
            AddDefaultValue("FundamentTID");
            AddDefaultLine();
            AddDefaultLine("# Set this to True to enable collision and scripting. Useful for adding interactive functionality.");
            AddDefaultValue("Dynamic", "False");
            AddDefaultLine("");
            AddDefaultLine("# Syntax is R,G,B (e.g. \"ButtonColor=0,192,255\")");
            AddDefaultLine("# Note: These options sometimes break.");
            AddDefaultValue("ButtonColor");
            AddDefaultValue("CheckboxColor");
            AddDefaultValue("TextColor");
        }

        protected override void ValueChanged(string key, string newValue)
        {
            if (key.Equals("hidefundament")) {
                hideFundament = ModConfigFile.ParseBool(newValue);
            } else if (key.Equals("dynamic")) {
                dynamic = ModConfigFile.ParseBool(newValue);
            }
        }

        public bool HideFundament {
            get {
                return hideFundament;
            }
            set {
                hideFundament = value;
                SetKeyValueInternally("hidefundament", value.ToString());
            }
        }

        public string FundamentTID {
            get {
                return this["fundamenttid"];
            }
            set {
                this["fundamenttid"] = value;
            }
        }

        public bool Dynamic {
            get {
                return dynamic;
            }
            set {
                dynamic = value;
                SetKeyValueInternally("dynamic", value.ToString());
            }
        }

        public string ButtonColor {
            get {
                return this["buttoncolor"];
            }
            set {
                this["buttoncolor"] = value;
            }
        }

        public string CheckboxColor {
            get {
                return this["checkboxcolor"];
            }
            set {
                this["checkboxcolor"] = value;
            }
        }

        public string TextColor {
            get {
                return this["textcolor"];
            }
            set {
                this["textcolor"] = value;
            }
        }
    }
}

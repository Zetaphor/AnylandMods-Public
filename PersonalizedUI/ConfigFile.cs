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
            AddDefaultLine("# Set this to 1 to disable the original dialog background.");
            AddDefaultLine("HideFundament=0");
            AddDefaultLine();
            AddDefaultLine("# Fill in a thing ID to use it as the default background for dialogs.");
            AddDefaultLine("FundamentTID=");
            AddDefaultLine();
            AddDefaultLine("# Set this to 1 to enable collision and scripting. Useful for adding interactive functionality.");
            AddDefaultLine("Dynamic=0");
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
    }
}

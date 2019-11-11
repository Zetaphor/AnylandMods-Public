using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;

namespace AnylandMods.BionicArms {
    class ConfigFile : ModConfigFile {
        private bool expEnabled;
        private bool moveHandDot;
        private float expbase;

        public ConfigFile(UnityModManager.ModEntry mod) : base(mod)
        {
            AddDefaultValue("MoveHand", "False");
            AddDefaultValue("ExpEnabled", "False");
            AddDefaultValue("ExpBase", "2");
            Load();
        }

        public bool ExpEnabled {
            get => expEnabled;
            set {
                expEnabled = value;
                SetKeyValueInternally("ExpEnabled", value.ToString());
            }
        }

        public bool MoveHand {
            get => moveHandDot;
            set {
                moveHandDot = value;
                SetKeyValueInternally("MoveHand", value.ToString());
            }
        }

        public float ExpBase {
            get => expbase;
            set {
                expbase = value;
                SetKeyValueInternally("ExpBase", value.ToString());
            }
        }

        protected override void ValueChanged(string key, string newValue)
        {
            switch (key) {
                case "enabled":
                    expEnabled = ParseBool(newValue);
                    break;
                case "movehand":
                    moveHandDot = ParseBool(newValue);
                    break;
                case "expbase":
                    if (!float.TryParse(newValue, out expbase)) {
                        DebugLog.Log("ExpBase={0} is not a valid floating point value. Resetting to default.");
                        ExpBase = 20.0f;
                        Save();
                    }
                    break;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;

namespace AnylandMods.DistanceTools {
    class ConfigFile : ModConfigFile {
        private bool expLegs;
        private bool expDrag;
        private float expbase;
        private bool enablePerspective;
        private float ipd;

        public ConfigFile(UnityModManager.ModEntry mod) : base(mod)
        {
            AddDefaultValue("ExpLegs", "False");
            AddDefaultValue("ExpDrag", "False");
            AddDefaultValue("ExpBase", "2");
            AddDefaultLine();
            AddDefaultLine("# The perspective editing option is buggy and incomplete. Set to True to enable it anyway.");
            AddDefaultValue("EnablePerspectiveEdit", "False");
            AddDefaultValue("IPD", "60.4");
            Load();
        }

        public bool ExpLegs {
            get => expLegs;
            set {
                expLegs = value;
                SetKeyValueInternally("ExpLegs", value.ToString());
            }
        }

        public bool ExpDrag {
            get => expDrag;
            set {
                expDrag = value;
                SetKeyValueInternally("ExpDrag", value.ToString());
            }
        }

        public float ExpBase {
            get => expbase;
            set {
                expbase = value;
                SetKeyValueInternally("ExpBase", value.ToString());
            }
        }

        public bool EnablePerspectiveEdit {
            get => enablePerspective;
            set {
                enablePerspective = value;
                SetKeyValueInternally("EnablePerspectiveEdit", value.ToString());
            }
        }

        public float IPD {
            get => ipd;
            set {
                ipd = value;
                SetKeyValueInternally("IPD", value.ToString());
            }
        }

        protected override void ValueChanged(string key, string newValue)
        {
            switch (key) {
                case "explegs":
                    expLegs = ParseBool(newValue);
                    break;
                case "expdrag":
                    expDrag = ParseBool(newValue);
                    break;
                case "expbase":
                    if (!float.TryParse(newValue, out expbase)) {
                        DebugLog.Log("ExpBase={0} is not a valid floating point value. Resetting to default.", newValue);
                        ExpBase = 20.0f;
                        Save();
                    }
                    break;
                case "enableperspectiveedit":
                    enablePerspective = ParseBool(newValue);
                    break;
                case "ipd":
                    if (!float.TryParse(newValue, out ipd)) {
                        DebugLog.Log("IPD={0} is not a valid floating point value. Resetting to default.", newValue);
                        IPD = 60.4f;
                        Save();
                    }
                    break;
            }
        }
    }
}

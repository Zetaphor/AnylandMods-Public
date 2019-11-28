using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityModManagerNet;
using UnityEngine;
using Newtonsoft.Json;

namespace AnylandMods.AutoBody {
    class ConfigFile : ModConfigFile {
        private SavedAttachmentList listHead, listHeadTop, listTorsoLower, listTorsoUpper, listHandLeft, listHandRight;
        private SavedAttachmentList listArmLeft, listArmRight, listLegLeft, listLegRight;

        private bool ignoreAddBody = false;
        private bool enableTellControl = true;

        public ConfigFile(UnityModManager.ModEntry mod)
            : base(mod, "config.txt")
        {
            listHead = new SavedAttachmentList(AttachmentPointId.Head);
            listHeadTop = new SavedAttachmentList(AttachmentPointId.HeadTop);
            listTorsoLower = new SavedAttachmentList(AttachmentPointId.TorsoLower);
            listTorsoUpper = new SavedAttachmentList(AttachmentPointId.TorsoUpper);
            listHandLeft = new SavedAttachmentList(AttachmentPointId.HandLeft);
            listHandRight = new SavedAttachmentList(AttachmentPointId.HandRight);
            listArmLeft = new SavedAttachmentList(AttachmentPointId.ArmLeft);
            listArmRight = new SavedAttachmentList(AttachmentPointId.ArmRight);
            listLegLeft = new SavedAttachmentList(AttachmentPointId.LegLeft);
            listLegRight = new SavedAttachmentList(AttachmentPointId.LegRight);

            AddDefaultValue("IgnoreAddBody", "False");
            AddDefaultValue("EnableTellControl", "True");
        }

        public bool IgnoreAddBody {
            get => ignoreAddBody;
            set {
                SetKeyValueInternally("IgnoreAddBody", value.ToString());
                ignoreAddBody = value;
            }
        }

        public bool EnableTellControl {
            get => enableTellControl;
            set {
                SetKeyValueInternally("EnableTellControl", value.ToString());
            }
        }

        private ref SavedAttachmentList GetListRefForAttachmentPoint(AttachmentPointId point)
        {
            switch (point) {
                case AttachmentPointId.Head: return ref listHead;
                case AttachmentPointId.HeadTop: return ref listHeadTop;
                case AttachmentPointId.TorsoLower: return ref listTorsoLower;
                case AttachmentPointId.TorsoUpper: return ref listTorsoUpper;
                case AttachmentPointId.HandLeft: return ref listHandLeft;
                case AttachmentPointId.HandRight: return ref listHandRight;
                case AttachmentPointId.ArmLeft: return ref listArmLeft;
                case AttachmentPointId.ArmRight: return ref listArmRight;
                case AttachmentPointId.LegLeft: return ref listLegLeft;
                case AttachmentPointId.LegRight: return ref listLegRight;
                default: throw new ArgumentOutOfRangeException("point", String.Format("{0} is not a valid attachment point.", point));
            }
        }

        public SavedAttachmentList GetListForAttachmentPoint(AttachmentPointId point)
        {
            return GetListRefForAttachmentPoint(point);
        }

        public void UpdateThingId(string oldId, string newId, string newName)
        {
            DebugLog.Log("oldId={0}, newId={1}, newName={2}", oldId, newId, newName);
            SavedAttachmentList[] lists = new SavedAttachmentList[] {
                listHead, listHeadTop, listTorsoLower, listTorsoUpper, listHandLeft, listHandRight, listArmLeft, listArmRight, listLegLeft, listLegRight
            };
            foreach (SavedAttachmentList list in lists) {
                if (list.TryGetThingName(oldId, out string oldName) && oldName.Equals(newName)) {
                    AttachmentData oldData = list[oldName];
                    list[newName /* == oldName */] = new AttachmentData(newId, oldData.position, oldData.rotation);
                    DebugLog.Log("Replacing {0} -> {1} ({2}) in list{3}", oldId, newId, newName, list.AttachmentPoint);
                }
            }
        }

        protected override void ValueChanged(string key, string newValue)
        {
            base.ValueChanged(key, newValue);
            if (key.Equals("ignoreaddbody")) {
                ignoreAddBody = ParseBool(newValue);
            } else if (key.Equals("enabletellcontrol")) {
                enableTellControl = ParseBool(newValue);
            } else {
                AttachmentPointId point = AttachmentPointId.None;
                switch (key) {
                    case "headtop": point = AttachmentPointId.HeadTop; break;
                    case "head": point = AttachmentPointId.Head; break;
                    case "handleft": point = AttachmentPointId.HandLeft; break;
                    case "handright": point = AttachmentPointId.HandRight; break;
                    case "armleft": point = AttachmentPointId.ArmLeft; break;
                    case "armright": point = AttachmentPointId.ArmRight; break;
                    case "torsoupper": point = AttachmentPointId.TorsoUpper; break;
                    case "torsolower": point = AttachmentPointId.TorsoLower; break;
                    case "legleft": point = AttachmentPointId.LegLeft; break;
                    case "legright": point = AttachmentPointId.LegRight; break;
                }
                if (point != AttachmentPointId.None) {
                    GetListRefForAttachmentPoint(point) = new SavedAttachmentList(point, newValue);
                }
            }
        }

        protected override void PreSave()
        {
            base.PreSave();
            AttachmentPointId[] allPoints = new AttachmentPointId[] {
                AttachmentPointId.Head,
                AttachmentPointId.HeadTop,
                AttachmentPointId.TorsoLower,
                AttachmentPointId.TorsoUpper,
                AttachmentPointId.HandLeft,
                AttachmentPointId.HandRight,
                AttachmentPointId.ArmLeft,
                AttachmentPointId.ArmRight,
                AttachmentPointId.LegLeft,
                AttachmentPointId.LegRight
            };
            foreach (AttachmentPointId point in allPoints) {
                SetKeyValueInternally(point.ToString(), GetListForAttachmentPoint(point).ToJson());
            }
        }
    }
}

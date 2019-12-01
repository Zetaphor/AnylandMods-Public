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

        public Dictionary<string, Vector3> LegPosLeft { get; private set; }
        public Dictionary<string, Vector3> LegPosRight { get; private set; }
        public Dictionary<string, Vector3> LegRotLeft { get; private set; }
        public Dictionary<string, Vector3> LegRotRight { get; private set; }

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

            LegPosLeft = new Dictionary<string, Vector3>();
            LegPosRight = new Dictionary<string, Vector3>();
            LegRotLeft = new Dictionary<string, Vector3>();
            LegRotRight = new Dictionary<string, Vector3>();

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
            bool replacedSomething = false;
            DebugLog.Log("oldId={0}, newId={1}, newName={2}", oldId, newId, newName);
            SavedAttachmentList[] lists = new SavedAttachmentList[] {
                listHead, listHeadTop, listTorsoLower, listTorsoUpper, listHandLeft, listHandRight, listArmLeft, listArmRight, listLegLeft, listLegRight
            };
            foreach (SavedAttachmentList list in lists) {
                if (list.TryGetThingName(oldId, out string oldName) && oldName.Equals(newName)) {
                    AttachmentData oldData = list[oldName];
                    list[newName /* == oldName */] = new AttachmentData(newId, oldData.position, oldData.rotation);
                    DebugLog.Log("Replacing {0} -> {1} ({2}) in list{3}", oldId, newId, newName, list.AttachmentPoint);
                    replacedSomething = true;
                }
            }
            if (replacedSomething)
                Save();
        }

        private void ParseLegPos(string value, Dictionary<string, Vector3> posDict, Dictionary<string, Vector3> rotDict)
        {
            posDict.Clear();
            rotDict.Clear();
            var jsondict = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
            foreach (string k in jsondict.Keys) {
                string[] parts = jsondict[k].Split(',');
                posDict.Add(k, new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])));
                rotDict.Add(k, new Vector3(float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5])));
            }
        }

        private string UnparseLegPos(Dictionary<string, Vector3> posDict, Dictionary<string, Vector3> rotDict)
        {
            var jsondict = new Dictionary<string, string>();
            foreach (string k in posDict.Keys) {
                var values = new float[] { posDict[k].x, posDict[k].y, posDict[k].z, rotDict[k].x, rotDict[k].y, rotDict[k].z };
                jsondict[k] = String.Join(",", values.Select(v => v.ToString()).ToArray());
            }
            return JsonConvert.SerializeObject(jsondict);
        }

        protected override void ValueChanged(string key, string newValue)
        {
            base.ValueChanged(key, newValue);
            if (key.Equals("ignoreaddbody")) {
                ignoreAddBody = ParseBool(newValue);
            } else if (key.Equals("enabletellcontrol")) {
                enableTellControl = ParseBool(newValue);
            } else if (key.Equals("legposleft")) {
                ParseLegPos(newValue, LegPosLeft, LegRotLeft);
            } else if (key.Equals("legposright")) {
                ParseLegPos(newValue, LegPosRight, LegRotRight);
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
            SetKeyValueInternally("LegPosLeft", UnparseLegPos(LegPosLeft, LegRotLeft));
            SetKeyValueInternally("LegPosRight", UnparseLegPos(LegPosRight, LegRotRight));
        }
    }
}

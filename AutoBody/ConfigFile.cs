using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityModManagerNet;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class ConfigFile : ModConfigFile {
        private SavedAttachmentList listHead, listHeadTop, listTorsoLower, listTorsoUpper, listHandLeft, listHandRight;
        private SavedAttachmentList listArmLeft, listArmRight, listLegLeft, listLegRight;

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

        protected override void ValueChanged(string key, string newValue)
        {
            base.ValueChanged(key, newValue);
            try {
                var dict = JsonUtility.FromJson<Dictionary<string, AttachmentData>>(newValue);
                var point = (AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), key);
                SavedAttachmentList list = GetListForAttachmentPoint(point);
                list.Clear();
                foreach (string name in dict.Keys) {
                    list[name] = dict[name];
                }
            } catch (ArgumentException) { }
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
                SetKeyValueInternally(point.ToString(), JsonUtility.ToJson(GetListForAttachmentPoint(point)));
            }
        }
    }
}

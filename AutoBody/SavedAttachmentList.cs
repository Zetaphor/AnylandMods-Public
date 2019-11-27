using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    [Serializable]
    class SavedAttachmentList : Dictionary<string, AttachmentData> {
        public AttachmentPointId AttachmentPoint { get; private set; }
        private Dictionary<string, string> namesById;

        public SavedAttachmentList(AttachmentPointId point)
        {
            AttachmentPoint = point;
            namesById = new Dictionary<string, string>();
        }

        public bool AddCurrent()
        {
            GameObject gobj = Managers.personManager.ourPerson.GetAttachmentPointById(AttachmentPoint);
            AttachmentPoint ap = gobj.GetComponent<AttachmentPoint>();
            if (ap.attachedThing != null) {
                this[ap.attachedThing.name.ToLower()] = ap.GetAttachmentData();
                return true;
            } else {
                return false;
            }
        }

        public bool TryGetThingName(string thingId, out string thingName)
        {
            return namesById.TryGetValue(thingId, out thingName);
        }

        public new AttachmentData this[string name] {
            get {
                return base[name.ToLower()];
            }
            set {
                if (TryGetThingName(value.thingId, out string oldName)) {
                    Remove(oldName);
                }
                namesById[value.thingId] = name;
                base[name] = value;
            }
        }

        public override void OnDeserialization(object sender)
        {
            base.OnDeserialization(sender);
            namesById.Clear();
            foreach (string name in Keys) {
                namesById[this[name].thingId] = name;
            }
        }
    }
}

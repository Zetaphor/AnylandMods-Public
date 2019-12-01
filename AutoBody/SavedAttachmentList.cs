using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace AnylandMods.AutoBody {
    class SavedAttachmentList {
        public AttachmentPointId AttachmentPoint { get; private set; }
        private Dictionary<string, AttachmentData> entries;
        private Dictionary<string, string> namesById;

        public SavedAttachmentList(AttachmentPointId point, string json = "{}")
        {
            AttachmentPoint = point;
            
            entries = new Dictionary<string, AttachmentData>();
            var jsondict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (string k in jsondict.Keys) {
                entries[k] = JsonUtility.FromJson<AttachmentData>(jsondict[k]);
            }

            namesById = new Dictionary<string, string>();
            foreach (string name in entries.Keys) {
                namesById[this[name].thingId] = name;
            }
        }

        public string AddCurrent()
        {
            GameObject gobj = Managers.personManager.ourPerson.GetAttachmentPointById(AttachmentPoint);
            if (gobj == null) {
                DebugLog.Log("Attachment point {0} does not exist!", AttachmentPoint);
                return null;
            }
            AttachmentPoint ap = gobj.GetComponent<AttachmentPoint>();
            if (ap == null) {
                DebugLog.Log("Attachment point {0} missing AttachmentPoint component; adding it now.");
                ap = gobj.AddComponent<AttachmentPoint>();
                ap.id = AttachmentPoint;
            }
            if (ap.attachedThing != null) {
                string name = ap.attachedThing.name.ToLower();
                this[name] = ap.GetAttachmentData();
                return name;
            } else {
                return null;
            }
        }

        public bool TryGetThingName(string thingId, out string thingName)
        {
            return namesById.TryGetValue(thingId, out thingName);
        }

        public bool ContainsName(string thingName)
        {
            return entries.ContainsKey(thingName);
        }

        public void Remove(string thingName)
        {
            entries.Remove(thingName);
        }
        
        public IEnumerable<string> ThingNames {
            get => entries.Keys;
        }

        public AttachmentData this[string name] {
            get {
                return entries[name.ToLower()];
            }
            set {
                if (TryGetThingName(value.thingId, out string oldName)) {
                    entries.Remove(oldName);
                }
                namesById[value.thingId] = name;
                entries[name] = value;
                DebugLog.LogTemp("----------------");
                foreach (string k in entries.Keys) {
                    DebugLog.LogTemp("{0}: {1}, {2}, {3}", k, this[k].Tid, this[k].P, this[k].R);
                }
                foreach (string k in namesById.Keys) {
                    DebugLog.LogTemp("id {0} : {1}", k, namesById[k]);
                }
                DebugLog.LogTemp("----------------");
            }
        }

        public void Clear()
        {
            entries.Clear();
            namesById.Clear();
        }

        public string ToJson()
        {
            var jsondict = new Dictionary<string, string>();
            foreach (string k in entries.Keys) {
                jsondict[k] = JsonUtility.ToJson(this[k]);
            }
            return JsonConvert.SerializeObject(jsondict);
        }
    }
}

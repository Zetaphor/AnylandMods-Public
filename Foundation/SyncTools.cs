using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods {
    public static class SyncTools {
        private class ThingSpawner : MonoBehaviour {
            public string ThingId { get; set; }
            public Action<Thing> Callback { get; set; }
            public bool KeepSyncing { get; set; }

            private void MyCallback(GameObject gameObject)
            {
                var thing = gameObject.GetComponent<Thing>();
                Callback(thing);
                var sync = gameObject.AddComponent<SyncAuthority>();
                sync.OnlySyncOnce = !KeepSyncing;
                sync.SpawnOutOfEarshot = true;
                Destroy(this);
            }

            public void Start()
            {
                StartCoroutine(Managers.thingManager.InstantiateThingViaCache(ThingRequestContext.LocalTest, ThingId, MyCallback));
            }
        }

        public static void SpawnThing(string thingId, Action<Thing> callback, bool keepSyncing = false)
        {
            GameObject spawner = new GameObject("ThingSpawner");
            var comp = spawner.AddComponent<ThingSpawner>();
            comp.ThingId = thingId;
            comp.Callback = callback;
            comp.KeepSyncing = keepSyncing;
        }

        public static void SpawnThing(string thingId, Vector3 position, Quaternion rotation = default, bool keepSyncing = false)
        {
            SpawnThing(thingId, delegate (Thing thing) {
                thing.transform.position = position;
                thing.transform.rotation = rotation;
            }, keepSyncing);
        }
    }
}

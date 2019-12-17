using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods {
    public static class SyncTools {
        public static Vector3 FarAway { get; private set; }

        static SyncTools()
        {
            FarAway = new Vector3(-999999f, -999999f, -999999f);
        }

        private class ThingSpawner : MonoBehaviour {
            public string ThingId { get; set; }
            public Action<Thing> Callback { get; set; }
            public bool KeepSyncing { get; set; }

            private void MyCallback(GameObject gameObject)
            {
                var thing = gameObject.GetComponent<Thing>();
                thing.transform.position = FarAway;
                thing.ThrowMe(Vector3.zero, Vector3.zero);
                Callback(thing);

                Managers.personManager.ourPerson.photonView.RPC("DoAddJustCreatedTemporaryThing_Remote", PhotonTargets.Others, new object[] {
                    ThingId, FarAway, thing.transform.rotation, thing.thrownId
                });

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

        public static void SpawnThing(string thingId, Vector3 position, Quaternion rotation = default, Vector3 velocity = default, Vector3 angularVelocity = default)
        {
            SpawnThing(thingId, delegate (Thing thing) {
                thing.transform.position = position;
                thing.transform.rotation = rotation;
                thing.rigidbody.velocity = velocity;
                thing.rigidbody.angularVelocity = angularVelocity;
            }, false);
        }
    }
}

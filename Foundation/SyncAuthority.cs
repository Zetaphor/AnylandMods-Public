using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace AnylandMods {
    public class SyncAuthority : MonoBehaviour {
        private static float lastSyncTime = float.NegativeInfinity;
        private static List<SyncAuthority> allInstances;
        public static float SyncInterval { get; set; } = 0.1f;
        private static bool syncPending = false;

        public bool OnlySyncOnce { get; set; } = true;
        public bool SpawnOutOfEarshot { get; set; } = false;

        static SyncAuthority()
        {
            allInstances = new List<SyncAuthority>();
        }

        public static void SyncNow()
        {
            DebugLog.LogTemp("SyncNow called");
            syncPending = false;
            lastSyncTime = Time.time;

            var thingPartStatesString = new StringBuilder();
            var thingPhysicsString = new StringBuilder();
            //var thingAttributesString = new StringBuilder();

            foreach (SyncAuthority sync in allInstances) {
                if (sync.isActiveAndEnabled) {
                    Thing thing = sync.GetComponent<Thing>();
                    if (thing.name != Universe.objectNameIfAlreadyDestroyed) {
                        // Thing part states

                        ThingSpecifierType spectype = ThingSpecifierType.None;
                        string specid = thing.GetSpecifierId(ref spectype);
                        string compressed = PersonManager.GetSyncingCompressedSpecifierType(spectype);
                        System.Collections.IEnumerator enumerator = thing.transform.GetEnumerator();
                        try {
                            while (enumerator.MoveNext()) {
                                var transform = (Transform)enumerator.Current;
                                ThingPart tp = transform.GetComponent<ThingPart>();
                                if (tp != null) {
                                    if (thingPartStatesString.Length > 0)
                                        thingPartStatesString.Append("\n");
                                    thingPartStatesString.Append(compressed + "|");
                                    thingPartStatesString.Append(specid + "|");
                                    thingPartStatesString.Append(tp.GetSyncingToAreaNewcomersDataString(true));
                                }
                            }
                        } finally {
                            var disposable = enumerator as IDisposable;
                            if (disposable != null)
                                disposable.Dispose();
                        }

                        // Physics

                        if (thing.rigidbody != null && thing.isThrownOrEmitted && !string.IsNullOrEmpty(thing.thrownId)) {
                            if (thingPhysicsString.Length > 0)
                                thingPhysicsString.Append("\n");
                            thingPhysicsString.Append(compressed + "|");
                            thingPhysicsString.Append(specid + "|");
                            thingPhysicsString.Append(thing.thingId + "|");
                            if (sync.SpawnOutOfEarshot) {
                                thingPhysicsString.Append(PersonManager.GetSyncingCompressedVector3(SyncTools.FarAway));
                            } else {
                                thingPhysicsString.Append(PersonManager.GetSyncingCompressedVector3(thing.transform.position) + "|");
                            }
                            thingPhysicsString.Append(PersonManager.GetSyncingCompressedQuaternion(thing.transform.rotation) + "|");
                            thingPhysicsString.Append(PersonManager.GetSyncingCompressedVector3(thing.rigidbody.velocity) + "|");
                            thingPhysicsString.Append(PersonManager.GetSyncingCompressedVector3(thing.rigidbody.angularVelocity) + "|");
                            thingPhysicsString.Append(PersonManager.GetSyncingCompressedFloat(thing.destroyMeInTime));
                        }
                    }

                    if (sync.OnlySyncOnce) {
                        sync.enabled = false;
                    }
                }
            }

            if (thingPartStatesString.Length > 0 || thingPhysicsString.Length > 0) {
                Managers.personManager.ourPerson.photonView.RPC("DoInformOfBehaviorScriptVariablesAndThingStates_Remote", PhotonTargets.Others, new object[] {
                    Time.time,
                    thingPartStatesString.ToString(),
                    thingPhysicsString.ToString(),
                    "", "", "", "", "", "", ""
                });

                foreach (SyncAuthority sync in allInstances) {
                    if (sync.SpawnOutOfEarshot) {
                        Managers.personManager.DoInformOfThingPhysics(sync.GetComponent<Thing>());
                        sync.SpawnOutOfEarshot = false;
                    }
                }
            }
        }

        public void Start()
        {
            allInstances.Add(this);
        }

        public void Update()
        {
            if (Time.time - lastSyncTime >= SyncInterval && !syncPending) {
                DebugLog.Log("Doing sync");
                syncPending = true;
                Invoke("CallSyncNow", 0f);
            }
        }

        void CallSyncNow()
        {
            SyncNow();
        }

        public void OnDestroy()
        {
            allInstances.Remove(this);
        }
    }
}

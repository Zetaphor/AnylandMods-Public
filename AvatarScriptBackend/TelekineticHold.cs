using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AvatarScriptBackend {
    class TelekineticHold : MonoBehaviour {
        private static Vector3 GetAccel(Vector3 handVelocity)
        {
            float speed = handVelocity.magnitude;
            float accel = 32.0f * speed * speed;
            return handVelocity.normalized * accel;
        }

        private Vector3 lastHandPos;
        private Thing thing;
        private bool wasPhysicalBeforePickup;
        private bool hadGravityBeforePickup;
        private float dragBeforePickup;
        private float angularDragBeforePickup;
        private bool moveWithHand = false;
        public GameObject Hand { get; set; } = null;
        private float timeSinceSync = 0.0f;
        private Vector3 savedPosition;
        private Vector3 savedRotation;
        private bool positionWasReset = false;

        private static List<TelekineticHold> allMovedObjects;

        static TelekineticHold()
        {
            allMovedObjects = new List<TelekineticHold>();
        }

        public void Start()
        {
        }

        public void Update()
        {
            if (moveWithHand) {
                Vector3 handVelocity = (Hand.transform.position - lastHandPos) / Time.deltaTime;
                Vector3 acceleration = GetAccel(handVelocity) * Time.deltaTime;
                DebugLog.LogTemp("{0}, {1}", handVelocity, acceleration);
                thing.rigidbody.AddForce(acceleration, ForceMode.VelocityChange);
                lastHandPos = Hand.transform.position;

                timeSinceSync += Time.deltaTime;
                if (timeSinceSync > 1.0f/30) {
                    timeSinceSync = 0.0f;
                    if (thing.movableByEveryone) {
                        Managers.personManager.DoPlaceAsMovableByEveryone(thing, Side.Right);
                    } else if (thing.IsPlacement() && Managers.areaManager.weAreEditorOfCurrentArea) {
                        Managers.personManager.DoMovePlacement(thing.gameObject, thing.gameObject.transform.localPosition, thing.gameObject.transform.localEulerAngles);
                    } else {
                        Managers.personManager.DoInformOfThingPhysics(thing);
                    }
                    /*Managers.personManager.ourPerson.photonView.RPC("DoPlaceAsMovableByEveryone_Remote", PhotonTargets.Others, new object[]
                    {
                        thing.placementId,
                        Side.Right,
                        thing.transform.position,
                        thing.transform.rotation
                    });*/
                }
            } else if (!wasPhysicalBeforePickup && thing.rigidbody.velocity.magnitude < 0.1f) {
                thing.rigidbody.isKinematic = true;
                thing.rigidbody.drag = dragBeforePickup;
                thing.rigidbody.angularDrag = angularDragBeforePickup;
            }
        }

        public void FlyToward(Vector3 position)
        {
            thing.rigidbody.velocity = 5.0f * (position - thing.rigidbody.position);
        }
        
        public static TelekineticHold PickUp(Thing thing, GameObject hand)
        {
            if (thing.isLocked)
                return null;
            var comp = thing.gameObject.GetComponent<TelekineticHold>();
            if (comp == null) {
                comp = thing.gameObject.AddComponent<TelekineticHold>();
                allMovedObjects.Add(comp);
                comp.savedPosition = thing.transform.position;
                comp.savedRotation = thing.transform.localEulerAngles;
            } else if (comp.moveWithHand) {
                return comp;
            }
            if (comp.positionWasReset) {
                comp.savedPosition = thing.transform.position;
                comp.savedRotation = thing.transform.localEulerAngles;
                comp.positionWasReset = false;
                allMovedObjects.Add(comp);
            }
            comp.Hand = hand;
            comp.lastHandPos = hand.transform.position;
            comp.thing = thing;
            if (thing.rigidbody == null) {
                thing.rigidbody = thing.gameObject.AddComponent<Rigidbody>();
                comp.wasPhysicalBeforePickup = false;
                comp.hadGravityBeforePickup = false;
            } else {
                comp.wasPhysicalBeforePickup = !comp.thing.rigidbody.isKinematic;
                comp.hadGravityBeforePickup = comp.thing.rigidbody.useGravity;
            }
            comp.dragBeforePickup = comp.thing.rigidbody.drag;
            comp.angularDragBeforePickup = comp.thing.rigidbody.angularDrag;
            comp.thing.rigidbody.isKinematic = false;
            comp.thing.rigidbody.useGravity = false;
            comp.thing.rigidbody.drag = 2.0f;
            comp.thing.rigidbody.angularDrag = 4.0f;
            comp.moveWithHand = true;

            return comp;
        }

        public void ResetPosition()
        {
            if (!positionWasReset) {
                PutDown();
                thing.transform.position = savedPosition;
                thing.transform.localEulerAngles = savedRotation;
                positionWasReset = true;
                Managers.personManager.ourPerson.photonView.RPC("DoPlaceAsMovableByEveryone_Remote", PhotonTargets.Others, new object[]
                    {
                    thing.placementId,
                    Side.Right,
                    thing.transform.position,
                    thing.transform.rotation
                    });
            }
        }

        public static void ResetAll()
        {
            foreach (TelekineticHold tkh in allMovedObjects) {
                try {
                    tkh.ResetPosition();
                } catch (Exception ex) {
                    DebugLog.Log("Error resetting position:\n{0}", ex);
                }
            }
            allMovedObjects.Clear();
        }

        public void PutDown()
        {
            moveWithHand = false;
            thing.rigidbody.isKinematic = !wasPhysicalBeforePickup;
            thing.rigidbody.useGravity = hadGravityBeforePickup;
        }
    }
}

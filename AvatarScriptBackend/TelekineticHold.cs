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
            float accel = 16f * speed * speed;
            return handVelocity.normalized * accel;
        }

        private Vector3 lastHandPos;
        private Vector3 lastHandVec;
        private Quaternion lastHandRot;
        public Thing Thing { get; private set; }
        private bool wasPhysicalBeforePickup;
        private bool hadGravityBeforePickup;
        private bool hadCollisionBeforePickup;
        private float dragBeforePickup;
        private float angularDragBeforePickup;
        private bool moveWithHand = false;
        public GameObject Hand { get; set; } = null;
        private float timeSinceSync = 0.0f;
        private Vector3 savedPosition;
        private Vector3 savedRotation;
        private bool positionWasReset = false;

        public bool AutoAim { get; set; }
        public bool AllowCollision { get; set; }

        public static List<TelekineticHold> AllMovedObjects { get; private set; }
        public static List<TelekineticHold> AllActiveHolds { get; private set; }

        static TelekineticHold()
        {
            AllMovedObjects = new List<TelekineticHold>();
            AllActiveHolds = new List<TelekineticHold>();
        }

        public void Start()
        {
        }

        public void Update()
        {
            if (moveWithHand) {
                if (Thing == null || Thing.rigidbody == null) {
                    enabled = false;
                    return;
                }
                Vector3 handMovement = Hand.transform.position - lastHandPos;
                Vector3 handVelocity = handMovement / Time.deltaTime;
                Vector3 acceleration = GetAccel(handVelocity) * Time.deltaTime;
                Thing.rigidbody.useGravity = false;
                Thing.rigidbody.detectCollisions = AllowCollision && hadCollisionBeforePickup;
                Thing.rigidbody.AddForce(acceleration, ForceMode.VelocityChange);
                Vector3 axis = Vector3.Cross(handMovement.normalized, lastHandVec.normalized);
                float angle = Vector3.SignedAngle(handMovement, lastHandVec, axis);
                Thing.rigidbody.AddTorque(-3f * axis * angle * (lastHandVec + handMovement).magnitude, ForceMode.VelocityChange);
                //Thing.transform.rotation = Quaternion.Inverse(lastHandRot) * Hand.transform.rotation * Thing.transform.rotation;
                lastHandVec = handMovement;
                lastHandPos = Hand.transform.position;
                lastHandRot = Hand.transform.rotation;

                if (AutoAim) {
                    Person target = Managers.personManager.GetCurrentAreaPersons()
                        .Where(p => !p.isOurPerson)
                        .OrderBy(p => (p.Head.transform.position - Thing.transform.position).magnitude)
                        .FirstOrDefault();
                    if (target != null)
                        Thing.transform.rotation = Quaternion.LookRotation(target.Head.transform.position - Thing.transform.position);
                }

                var handDot = Hand.GetComponent<HandDot>();
                if (handDot != null) {
                    if (CrossDevice.GetPressDown(handDot.controller, CrossDevice.button_grabTip, handDot.side)) {
                        Thing.TriggerEventAsStateAuthority(StateListener.EventType.OnTriggered);
                    } else if (CrossDevice.GetPressUp(handDot.controller, CrossDevice.button_grabTip, handDot.side)) {
                        Thing.TriggerEventAsStateAuthority(StateListener.EventType.OnUntriggered);
                    }
                }

                timeSinceSync += Time.deltaTime;
                if (timeSinceSync > 1.0f/30) {
                    timeSinceSync = 0.0f;
                    if (Thing.movableByEveryone) {
                        Managers.personManager.DoPlaceAsMovableByEveryone(Thing, Side.Right);
                    } else if (Thing.IsPlacement() && Managers.areaManager.weAreEditorOfCurrentArea) {
                        //Managers.personManager.DoMovePlacement(thing.gameObject, thing.gameObject.transform.localPosition, thing.gameObject.transform.localEulerAngles);
                    } else {
                        Managers.personManager.DoInformOfThingPhysics(Thing);
                    }
                    /*Managers.personManager.ourPerson.photonView.RPC("DoPlaceAsMovableByEveryone_Remote", PhotonTargets.Others, new object[]
                    {
                        thing.placementId,
                        Side.Right,
                        thing.transform.position,
                        thing.transform.rotation
                    });*/
                }
            } else if (!wasPhysicalBeforePickup && Thing.rigidbody.velocity.magnitude < 0.1f) {
                Thing.rigidbody.isKinematic = true;
                Thing.rigidbody.detectCollisions = hadCollisionBeforePickup;
                Thing.rigidbody.drag = dragBeforePickup;
                Thing.rigidbody.angularDrag = angularDragBeforePickup;
            }
        }

        public void FlyToward(Vector3 position)
        {
            Thing.rigidbody.velocity = 3.0f * (position - Thing.rigidbody.position);
        }
        
        public static TelekineticHold PickUp(Thing thing, GameObject hand)
        {
            var comp = thing.gameObject.GetComponent<TelekineticHold>();
            if (comp == null) {
                comp = thing.gameObject.AddComponent<TelekineticHold>();
                AllMovedObjects.Add(comp);
                comp.savedPosition = thing.transform.position;
                comp.savedRotation = thing.transform.localEulerAngles;
            }
            if (!AllActiveHolds.Contains(comp))
                AllActiveHolds.Add(comp);
            if (comp.moveWithHand)
                return comp;
            if (comp.positionWasReset) {
                comp.savedPosition = thing.transform.position;
                comp.savedRotation = thing.transform.localEulerAngles;
                comp.positionWasReset = false;
                AllMovedObjects.Add(comp);
            }
            comp.Hand = hand;
            comp.lastHandPos = hand.transform.position;
            comp.Thing = thing;
            comp.AllowCollision = false;
            if (thing.rigidbody == null) {
                thing.rigidbody = thing.gameObject.AddComponent<Rigidbody>();
                comp.wasPhysicalBeforePickup = false;
                comp.hadGravityBeforePickup = false;
                comp.hadCollisionBeforePickup = false;
            } else {
                comp.wasPhysicalBeforePickup = !comp.Thing.rigidbody.isKinematic;
                comp.hadGravityBeforePickup = comp.Thing.rigidbody.useGravity;
                comp.hadCollisionBeforePickup = comp.Thing.rigidbody.detectCollisions;
            }
            comp.dragBeforePickup = comp.Thing.rigidbody.drag;
            comp.angularDragBeforePickup = comp.Thing.rigidbody.angularDrag;
            comp.Thing.rigidbody.isKinematic = false;
            comp.Thing.rigidbody.useGravity = false;
            comp.Thing.rigidbody.drag = 2.0f;
            comp.Thing.rigidbody.angularDrag = 15.0f;
            comp.AutoAim = false;
            comp.moveWithHand = true;

            return comp;
        }

        public void ResetPosition()
        {
            if (!positionWasReset) {
                PutDown();
                Thing.transform.position = savedPosition;
                Thing.transform.localEulerAngles = savedRotation;
                positionWasReset = true;
                /*Managers.personManager.ourPerson.photonView.RPC("DoPlaceAsMovableByEveryone_Remote", PhotonTargets.Others, new object[]
                    {
                    Thing.placementId,
                    Side.Right,
                    Thing.transform.position,
                    Thing.transform.rotation
                    });*/
            }
        }

        public static void ResetAll()
        {
            var toReset = new List<TelekineticHold>();
            foreach (TelekineticHold tkh in AllMovedObjects) {
                toReset.Add(tkh);
            }
            foreach (TelekineticHold tkh in toReset) {
                try {
                    tkh.PutDown();
                    tkh.ResetPosition();
                } catch (Exception ex) {
                    DebugLog.Log("Error resetting position:\n{0}", ex);
                }
            }
            AllMovedObjects.Clear();
        }

        public void PutDown()
        {
            moveWithHand = false;
            Thing.rigidbody.isKinematic = !wasPhysicalBeforePickup;
            Thing.rigidbody.useGravity = hadGravityBeforePickup;
            Thing.rigidbody.detectCollisions = hadCollisionBeforePickup;
            AllActiveHolds.Remove(this);
        }

        public static void PutDownAll()
        {
            var toPutDown = new List<TelekineticHold>();
            foreach (TelekineticHold tkh in AllActiveHolds) {
                toPutDown.Add(tkh);
            }
            foreach (TelekineticHold tkh in toPutDown) {
                try {
                    tkh.PutDown();
                } catch (NullReferenceException) {
                } catch (Exception ex) {
                    DebugLog.Log("Error putting down:\n{0}", ex);
                }
            }
        }
    }
}

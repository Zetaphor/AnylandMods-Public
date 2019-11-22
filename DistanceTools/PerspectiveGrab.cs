using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;

namespace AnylandMods.DistanceTools.Perspective {
    enum DistanceMode {
        Fixed,     // Constant distance set by user.
        Preserve,  // Preserve original distance from before grab.
        MaxScale   // Distance for maximum (250x) scale.
    }

    class GrabHand {
        public static GrabHand Left { get; set; }
        public static GrabHand Right { get; set; }
        
        public static GrabHand FromSide(Side side)
        {
            return (side == Side.Left) ? Left : Right;
        }

        private float preservedDistance;
        private Vector3 grabOffset;

        public Transform Head { get; private set; }
        public Hand Hand { get; private set; }
        public Thing HeldThing { get; private set; } = null;

        public GrabHand(Person person, Side handSide)
        {
            Head = person.Head.transform;
            Hand = person.GetHandBySide(handSide).GetComponent<Hand>();
        }

        public Thing StartGrab()
        {
            Vector3 headToEye = Head.right * (Main.config.IPD / 2000.0f);
            if (Hand.side == Side.Left) headToEye = -headToEye;
            Vector3 eyePos = Head.position + headToEye;
            Vector3 eyeToHand = Hand.transform.position - eyePos;
            RaycastHit[] hits = Physics.RaycastAll(Hand.transform.position, eyeToHand);
            if (hits.Length > 0) {
                try {
                    RaycastHit hit = hits.OrderBy(h => h.distance).First(h => h.collider.gameObject.tag.Equals("ThingPart"));
                    HeldThing = hit.collider.gameObject.GetComponent<ThingPart>().transform.parent.gameObject.GetComponent<Thing>().GetMyRootThing();
                    var handDot = Hand.handDot.GetComponent<HandDot>();
                    handDot.currentlyHeldObject = HeldThing.gameObject;
                    float scale = HeldThing.transform.localScale.x;
                    float proportion = eyeToHand.magnitude / (eyeToHand.magnitude + hit.distance);
                    preservedDistance = (hit.point - eyePos).magnitude;
                    DebugLog.Log("{0} * {1}", scale, proportion);
                    scale *= proportion;
                    DebugLog.Log("= {0}", scale);
                    // TODO: Sync new scale
                    grabOffset = (hit.point - HeldThing.transform.position) * proportion;
                    DebugLog.LogTemp("({0} - {1}) * p = {2}", hit.point, HeldThing.transform.position, grabOffset);
                    HeldThing.transform.position = Hand.transform.position - grabOffset;
                    handDot.StorePickUpPosition(HeldThing.gameObject);
                    Managers.personManager.DoEditHold(Hand.gameObject, HeldThing.gameObject);
                    HeldThing.transform.localScale = new Vector3(scale, scale, scale);
                    return HeldThing;
                } catch (InvalidOperationException) {
                    return null;
                }
            } else {
                return null;
            }
        }

        private static RaycastHit? FindCorrectRayHit(IEnumerable<RaycastHit> hits)
        {
            try {
                return hits.OrderBy(h => h.distance)
                    .First(h => h.collider.gameObject.tag.Equals("ThingPart")
                    && h.collider.gameObject.GetComponent<ThingPart>().IsPartOfPlacement());
            } catch (InvalidOperationException) {
                return null;
            }
        }
        
        public void LetGo(bool withoutMoving = false)
        {
            Vector3 headToEye = Head.right * (Main.config.IPD / 2000.0f);
            if (Hand.side == Side.Left) headToEye = -headToEye;
            Vector3 eyePos = Head.position + headToEye;
            if (HeldThing != null) {
                try {
                    if (withoutMoving) return;

                    const float maxScale = 250.0f;
                    Vector3 eyeToHand = Hand.transform.position - eyePos;
                    float scaleInHand = HeldThing.transform.localScale.x;

                    float maxDist;
                    switch (Main.perspectiveOpts.DistanceMode) {
                        case DistanceMode.Fixed:
                            maxDist = Main.perspectiveOpts.FixedDistance;
                            break;
                        case DistanceMode.Preserve:
                            maxDist = preservedDistance;
                            break;
                        case DistanceMode.MaxScale:
                            maxDist = maxScale * eyeToHand.magnitude / scaleInHand;  // whatever distance will make it the max scale (250x)
                            break;
                        default:
                            DebugLog.Log("WARNING: Unknown distance mode for perspective editing!");
                            maxDist = 1.0f;
                            break;
                    }

                    Vector3 newPos;
                    float newScale = 1.0f;
                    Vector3 dropOffset = grabOffset;
                    Collider heldCollider = null;
                    
                    Bounds? boundingBox = null;
                    foreach (var r in HeldThing.GetComponentsInChildren<Renderer>()) {
                        if (boundingBox.HasValue)
                            boundingBox.Value.Encapsulate(r.bounds);
                        else
                            boundingBox = r.bounds;
                    }

                    if (Main.perspectiveOpts.PreferCloserRaycast && boundingBox.HasValue) {
                        float radius = boundingBox.Value.extents.magnitude / eyeToHand.magnitude * maxDist;
                        RaycastHit[] hits = ConeCast.ConeCastAll(Hand.transform.position + eyeToHand, radius, eyeToHand, maxDist, Mathf.Atan2(radius, maxDist));
                        RaycastHit? hit_ = FindCorrectRayHit(hits);
                        if (hit_.HasValue) {
                            RaycastHit hit = hit_.Value;
                            newPos = hit.point;
                            heldCollider = HeldThing.GetComponent<Collider>();
                            if (heldCollider != null) {
                                dropOffset -= heldCollider.ClosestPoint(newPos) - HeldThing.transform.position;
                            }
                        } else {
                            newPos = eyePos + eyeToHand.normalized * maxDist;
                        }
                    } else {
                        newPos = eyePos + eyeToHand.normalized * maxDist;
                    }
                    Vector3 newPosWithoutOffset = newPos;
                    for (var i = 0; i < 16; ++i) {
                        newScale = scaleInHand * (newPos - eyePos).magnitude / (HeldThing.transform.position - eyePos).magnitude;
                        newPos = newPosWithoutOffset - dropOffset * newScale / scaleInHand;
                        DebugLog.LogTemp("[{0}] {1} @ {2} - {3} = {4}", i, newScale, newPosWithoutOffset, dropOffset * newScale / scaleInHand, newPos);
                    }

                    DebugLog.Log("{0} -> {1} @ {2}", scaleInHand, newScale, newPos);

                    Managers.personManager.DoUpdatePlacementScale(HeldThing, HeldThing.transform.localScale = new Vector3(newScale, newScale, newScale));
                    Managers.personManager.DoMovePlacement(HeldThing.gameObject, newPos, HeldThing.transform.localEulerAngles);
                } finally {
                    HeldThing = null;
                }
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public class HandDotHook {
        public static void Postfix(HandDot __instance)
        {
            if (Main.perspectiveOpts.Enabled && Our.mode == EditModes.Area) {
                if (CrossDevice.GetPressDown(__instance.controller, CrossDevice.button_grab, __instance.side)) {
                    GrabHand.FromSide(__instance.side).StartGrab();
                } else if (CrossDevice.GetPressUp(__instance.controller, CrossDevice.button_grab, __instance.side)) {
                    bool trigger = CrossDevice.GetPress(__instance.controller, CrossDevice.button_grabTip, __instance.side);
                    GrabHand.FromSide(__instance.side).LetGo(trigger);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PersonManager), nameof(PersonManager.InitializeOurPerson))]
    public static class InitializeGrabHands {
        public static void Postfix(PersonManager __instance)
        {
            GrabHand.Left = new GrabHand(__instance.ourPerson, Side.Left);
            GrabHand.Right = new GrabHand(__instance.ourPerson, Side.Right);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;

namespace AnylandMods.DistanceTools.PerspectiveGrab {
    class GrabHand {
        public static GrabHand Left { get; set; }
        public static GrabHand Right { get; set; }
        
        public static GrabHand FromSide(Side side)
        {
            return (side == Side.Left) ? Left : Right;
        }

        public Transform Eye { get; private set; }
        public Hand Hand { get; private set; }
        public Thing HeldThing { get; private set; } = null;

        public GrabHand(Person person, Side handSide)
        {
            Eye = person.Head.transform;
            Hand = person.GetHandBySide(handSide).GetComponent<Hand>();
        }

        public Thing StartGrab()
        {
            Vector3 eyeToHand = Hand.transform.position - Eye.position;
            RaycastHit[] hits = Physics.RaycastAll(Hand.transform.position, eyeToHand);
            if (hits.Length > 0) {
                try {

                    RaycastHit hit = hits.OrderBy(h => h.distance).First(h => h.collider.gameObject.tag.Equals("ThingPart"));
                    HeldThing = hit.collider.gameObject.GetComponent<ThingPart>().transform.parent.gameObject.GetComponent<Thing>().GetMyRootThing();
                    var ttf = HeldThing.GetComponent<TransformTargetFollower>();
                    if (ttf is null)
                        ttf = HeldThing.gameObject.AddComponent<TransformTargetFollower>();
                    var handDot = Hand.handDot.GetComponent<HandDot>();
                    handDot.currentlyHeldObject = HeldThing.gameObject;
                    float scale = HeldThing.transform.localScale.x;
                    float proportion = eyeToHand.magnitude / (eyeToHand.magnitude + hit.distance);
                    DebugLog.Log("{0} * {1}", scale, proportion);
                    scale *= proportion;
                    DebugLog.Log("= {0}", scale);
                    // TODO: Sync new scale
                    Vector3 hitOffset = hit.point - HeldThing.transform.position;
                    HeldThing.transform.position = Hand.transform.position - hitOffset * proportion;
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

        public void LetGo()
        {
            if (HeldThing != null) {
                try {
                    const float maxScale = 250.0f;
                    Vector3 eyeToHand = Hand.transform.position - Eye.position;
                    float originalScale = HeldThing.transform.localScale.x;
                    float maxDist = maxScale * eyeToHand.magnitude / originalScale;  // whatever distance will make it the max scale (250x)
                    RaycastHit[] hits = Physics.RaycastAll(Hand.transform.position, eyeToHand, maxDist);

                    Vector3 newPos;
                    float newScale;
                    try {
                        RaycastHit hit = hits.OrderBy(h => h.distance).First(h => h.collider != HeldThing.transform);
                        newPos = hit.point;
                        newScale = originalScale * (newPos - Eye.position).magnitude / eyeToHand.magnitude;
                    } catch (InvalidOperationException) {
                        newPos = eyeToHand.normalized * maxScale;
                        newScale = maxScale;
                    }

                    DebugLog.Log("{0} -> {1} @ {2}", originalScale, newScale, newPos);

                    var ttf = HeldThing.GetComponent<TransformTargetFollower>();
                    if (ttf is null)
                        ttf = HeldThing.gameObject.AddComponent<TransformTargetFollower>();
                    ttf.targetPosition = newPos;
                    ttf.targetScale = new Vector3(newScale, newScale, newScale);
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
            if (Main.perspectiveGrab) {
                if (CrossDevice.GetPressDown(__instance.controller, CrossDevice.button_grab, __instance.side)) {
                    GrabHand.FromSide(__instance.side).StartGrab();
                } else if (CrossDevice.GetPressUp(__instance.controller, CrossDevice.button_grab, __instance.side)) {
                    GrabHand.FromSide(__instance.side).LetGo();
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

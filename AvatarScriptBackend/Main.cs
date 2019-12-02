using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityModManagerNet;
using UnityEngine;
using Harmony;

namespace AnylandMods.AvatarScriptBackend {
    public class Main {
        private delegate bool Effect(RaycastHit hit, Thing thing, ThingPart part);

        public static bool enabled;
        public static UnityModManager.ModEntry mod;

        private static Effect onPoint = Effect_Activate;

        private static List<GameObject> disabledObjects;
        private static TelekineticHold tkh = null;

        private static GameObject cameraHolder = null;
        private static Transform previousLeftHandParent = null;
        private static Transform previousRightHandParent = null;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            disabledObjects = new List<GameObject>();
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;
            return true;
        }

        private static bool Effect_Activate(RaycastHit hit, Thing thing, ThingPart part)
        {
            bool didSomething = false;
            foreach (ThingPart tp in thing.GetComponentsInChildren<ThingPart>()) {
                for (int state = 0; state < tp.states.Count; ++state) {
                    for (int ln = 0; ln < tp.states[state].listeners.Count; ++ln) {
                        StateListener listener = tp.states[state].listeners[ln];
                        if (listener.isForAnyState || state == tp.currentState) {
                            var eventTypes = new StateListener.EventType[] {
                                StateListener.EventType.OnTouches,
                                StateListener.EventType.OnTriggered,
                                StateListener.EventType.OnUntriggered,
                                StateListener.EventType.OnToldByAny,
                                StateListener.EventType.OnToldByNearby,
                                StateListener.EventType.OnTold
                            };
                            if (eventTypes.Contains(listener.eventType)) {
                                DebugLog.Log("{0}[{1}]: {2} {3}", thing.name, state, listener.eventType, listener.whenData);
                                tp.ExecuteCommands(listener);
                                Managers.personManager.DoBehaviorScriptLine(thing.gameObject, tp.indexWithinThing, ln, state, tp.currentState);
                                didSomething = true;
                            }
                        }
                    }
                }
            }
            return didSomething;
        }

        private static bool Effect_Disable(RaycastHit hit, Thing thing, ThingPart part)
        {
            hit.collider.gameObject.SetActive(false);
            disabledObjects.Add(hit.collider.gameObject);
            return true;
        }

        private static bool Effect_Pickup(RaycastHit hit, Thing thing, ThingPart part)
        {
            if (tkh == null) {
                if (thing != null) {
                    tkh = TelekineticHold.PickUp(thing, Managers.personManager.ourPerson.GetHandBySide(Side.Right).GetComponent<Hand>().handDot);
                    return tkh != null;
                } else {
                    return false;
                }
            } else {
                tkh.FlyToward(hit.point);
                return true;
            }
        }

        private static void BodyTellManager_ToldByBody(string data, BodyTellManager.TellEventInfo info)
        {
            Person me = Managers.personManager.ourPerson;
            GameObject head = me.Head;
            Hand leftHand = me.GetHandBySide(Side.Left).GetComponent<Hand>();
            Hand rightHand = me.GetHandBySide(Side.Right).GetComponent<Hand>();
            HandDot leftHD = leftHand.handDot.GetComponent<HandDot>();
            HandDot rightHD = rightHand.handDot.GetComponent<HandDot>();

            if (!data.StartsWith("xc"))
                DebugLog.LogTemp("body tell {0}", data);

            if (!info.IsTrusted)
                return;

            switch (data) {
                case "xxrtouchcone":
                    RaycastHit[] allHits = ConeCast.ConeCastAll(rightHD.transform.position, 4.0f, rightHD.transform.position - head.transform.position, 8.0f, 20.0f);
                    
                    var activator = new GameObject("ActivateThingParts");
                    var component = activator.AddComponent<ActivateThingPartsForTouchCone>();
                    component.MyPosition = rightHD.transform.position;
                    component.ThingParts = allHits.OrderBy(hit => hit.distance)
                        .Select(hit => hit.collider.gameObject.GetComponent<ThingPart>())
                        .Where(tp => tp != null);
                    
                    break;

                case "x point fire":
                    //allHits = Physics.RaycastAll(rightHD.transform.position, rightHD.transform.position - head.transform.position);
                    //const float maxDist = 50.0f;
                    //const float degrees = 3.0f;
                    //float maxRadius = Mathf.Tan(Mathf.Deg2Rad * degrees) * maxDist;
                    //allHits = ConeCast.ConeCastAll(rightHD.transform.position, maxRadius, rightHD.transform.position - head.transform.position, maxDist, degrees);
                    Vector3 headToHand = rightHD.transform.position - head.transform.position;
                    Vector3 direction = TrackHandVelocity.Right.normalized;
                    if (Vector3.Angle(direction, headToHand) < 20.0f)
                        direction = headToHand;
                    allHits = Physics.SphereCastAll(rightHD.transform.position + direction.normalized * 0.3f, 0.25f, direction);
                    Func<RaycastHit, float> sortfunc = delegate (RaycastHit h) {
                        float angle = Vector3.Angle(direction, h.point - head.transform.position);
                        return h.distance * angle;
                    };
                    foreach (RaycastHit hit in allHits.OrderBy(sortfunc)) {
                        DebugLog.LogTemp("{0} hit", hit);
                        ThingPart part;
                        Thing thing = null;
                        try {
                            part = hit.collider.gameObject.GetComponent<ThingPart>();
                            if (part != null) {
                                Transform parent = part.gameObject.transform.parent;
                                if (parent != null) {
                                    thing = parent.gameObject.GetComponent<Thing>();
                                    if (thing != null)
                                        thing = thing.GetMyRootThing();
                                }
                            } else {
                                thing = hit.collider.gameObject.GetComponentInParent<Thing>();
                                DebugLog.LogTemp("got {0}", thing);
                            }
                            DebugLog.LogTemp("calling onPoint");
                            if (onPoint(hit, thing, part))
                                break;
                        } catch (NullReferenceException ex) {
                            DebugLog.LogTemp("{0}", ex);
                        }
                    }
                    break;

                case "x point to activate":
                    onPoint = Effect_Activate;
                    break;

                case "x point to disable":
                    onPoint = Effect_Disable;
                    break;

                case "x point to pickup":
                    onPoint = Effect_Pickup;
                    break;

                case "rsnap":
                    foreach (GameObject obj in disabledObjects) {
                        try {
                            obj.SetActive(true);
                        } catch (NullReferenceException) {
                        }
                    }
                    disabledObjects.Clear();
                    TelekineticHold.ResetAll();
                    break;

                case "x point stop":
                    if (tkh != null) {
                        tkh.PutDown();
                        tkh = null;
                    }
                    break;

                case "x setcam":
                case "x setcamhands":
                    var leftHand_ = leftHand.transform;
                    var rightHand_ = rightHand.transform;
                    if (info.Thing != null) {
                        if (cameraHolder != null) {
                            GameObject.Destroy(cameraHolder);
                        }
                        cameraHolder = new GameObject();
                        cameraHolder.transform.parent = info.Thing.gameObject.transform;
                        var cam = cameraHolder.AddComponent<Camera>();

                        if (data.Length > 12) {
                            // x setcamhands
                            /*if (previousLeftHandParent == null)
                                previousLeftHandParent = leftHand_.transform.parent;
                            leftHand_.SetParent(info.Thing.gameObject.transform, false);

                            if (previousRightHandParent == null)
                                previousRightHandParent = rightHand_.transform.parent;
                            rightHand_.SetParent(info.Thing.gameObject.transform, false);*/
                        }
                    }
                    break;

                case "x resetcam":
                    leftHand_ = leftHand.transform.parent;
                    rightHand_ = rightHand.transform.parent;
                    if (cameraHolder != null) {
                        GameObject.Destroy(cameraHolder);
                        cameraHolder = null;
                        if (previousLeftHandParent != null) {
                            leftHand_.SetParent(previousLeftHandParent, false);
                            previousLeftHandParent = null;
                        }
                        if (previousRightHandParent != null) {
                            rightHand_.SetParent(previousRightHandParent, false);
                            previousRightHandParent = null;
                        }
                    }
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class TrackHandVelocity {
        private static Vector3 lastPosLeft, lastPosRight;
        public static Vector3 Left { get; private set; }
        public static Vector3 Right { get; private set; }
        
        public static Vector3 ForSide(Side side)
        {
            return (side == Side.Left) ? Left : Right;
        }

        public static void Postfix(HandDot __instance)
        {
            if (__instance.side == Side.Left) {
                Left = (__instance.transform.position - lastPosLeft) / Time.deltaTime;
                lastPosLeft = __instance.transform.position;
            } else {
                Right = (__instance.transform.position - lastPosRight) / Time.deltaTime;
                lastPosRight = __instance.transform.position;
            }
        }
    }

    class ActivateThingPartsForTouchCone : MonoBehaviour {
        public Vector3 MyPosition { get; set; }
        public IEnumerable<ThingPart> ThingParts { get; set; }

        void Start()
        {
            StartCoroutine(Routine());
        }

        private void Activate(ThingPart tp)
        {
            DebugLog.LogTemp("{0} touched", tp.gameObject);
            tp.TriggerEventAsStateAuthority(StateListener.EventType.OnTouches, "hand");
            tp.TriggerEventAsStateAuthority(StateListener.EventType.OnTouches, "");
        }

        IEnumerator<YieldInstruction> Routine()
        {
            float time = 0.0f;
            foreach (ThingPart tp in ThingParts) {
                float distance = (tp.transform.position - MyPosition).magnitude;
                float timeToReach = distance / 16;
                yield return new WaitForSeconds(timeToReach - time);
                time = timeToReach;
                DebugLog.Log("{0} m", distance);
                Activate(tp);
            }
        }
    }

    /*[HarmonyPatch(typeof(HandDot), "Update")]
    public static class HandDotDebugGraph {
        public static void Postfix(HandDot __instance)
        {
            if (__instance.side == Side.Right && __instance.gameObject.GetComponent<ContinuousFFT>() is null && __instance.otherDot != null) {
                var fft = __instance.gameObject.AddComponent<ContinuousFFT>();
                
                var graphs = new GameObject();
                var xr = graphs.AddComponent<DebugGraph>();
                var xi = graphs.AddComponent<DebugGraph>();
                var yr = graphs.AddComponent<DebugGraph>();
                var yi = graphs.AddComponent<DebugGraph>();
                xi.Matrix = Matrix4x4.Translate(new Vector3(0, 0, 0.01f));
                yr.Matrix = Matrix4x4.Translate(new Vector3(0, 0, 0.10f));
                yi.Matrix = Matrix4x4.Translate(new Vector3(0, 0, 0.11f));
                xr.Material.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
                xi.Material.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
                yr.Material.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
                yi.Material.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
                xr.DataArray = fft.FFTXR;
                xi.DataArray = fft.FFTXI;
                yr.DataArray = fft.FFTYR;
                yi.DataArray = fft.FFTYI;

                DebugLog.Log("Graphs created");

                graphs.transform.localScale = new Vector3(0.75f, 0.5f, 1.0f);
                graphs.transform.position = new Vector3(0, 0, 2);
            }
        }
    }*/
}
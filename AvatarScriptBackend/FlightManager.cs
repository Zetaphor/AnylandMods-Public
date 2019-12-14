using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using Valve.VR;

namespace AnylandMods.AvatarScriptBackend {
    public class FlightManager : MonoBehaviour {
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public Vector3 AngularAcceleration { get; set; }
        public float DragFactor { get; set; }

        public float MaxLeanAngle { get; set; } = 70f;
        public Quaternion CurrentLean { get; private set; }
        public Vector3 AccelWithLean { get; set; }

        private const float minSignificantVelocity = 4.0f;

        private Vector3 lastSignificantDir;
        private Vector3 velWithLean;

        public void Start()
        {
            DragFactor = 0.5f;
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
            velWithLean = Vector3.zero;
            AccelWithLean = Vector3.zero;
            AngularVelocity = Vector3.zero;
            AngularAcceleration = Vector3.zero;
            CurrentLean = Quaternion.identity;
        }

        private static float Sigmoid(float a, float b, float c, float x)
        {
            return a / (1 + Mathf.Exp(b - x / c));
        }

        private static float DSigmoidDxAt0(float a, float b, float c)
        {
            float expB = Mathf.Exp(b);
            return a * expB / (c * (expB + 1) * (expB + 1));
        }

        private float LeanAngleForVelocity(Vector3 velocity)
        {
            const float B = 5f;
            const float C = 4f;
            return Sigmoid(MaxLeanAngle, B, C, velocity.magnitude) - Sigmoid(MaxLeanAngle, B, C, 0) - DSigmoidDxAt0(MaxLeanAngle, B, C) * Mathf.Log(velocity.magnitude + 1f);
        }

        private Vector3 FlatVelocity {
            get => new Vector3(Velocity.x, 0, Velocity.z);
        }

        public void HintFacingAngle()
        {
            float angle = Vector3.SignedAngle(FlatVelocity.normalized, Vector3.forward, Vector3.up);
            lastSignificantDir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        public void Update()
        {
            Velocity += Acceleration * Time.deltaTime;
            velWithLean += AccelWithLean * Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;

            AngularVelocity += AngularAcceleration * Time.deltaTime;
            var angularDelta = AngularVelocity * Time.deltaTime;
            Quaternion angVelRotation = Quaternion.Inverse(CurrentLean);
            angVelRotation *= Quaternion.Euler(angularDelta);
            angVelRotation *= CurrentLean;
            transform.rotation *= angVelRotation;

            float dragCoefficient = Mathf.Pow(DragFactor, Time.deltaTime);
            Velocity *= dragCoefficient;
            velWithLean *= dragCoefficient;
            AngularVelocity *= dragCoefficient;

            if (FlatVelocity.magnitude >= minSignificantVelocity)
                lastSignificantDir = FlatVelocity.normalized;

            if (velWithLean.magnitude >= minSignificantVelocity) {
                //Quaternion baseRotation = Quaternion.FromToRotation(Vector3.forward, lastSignificantDir);
                Quaternion baseRotation = Quaternion.Inverse(CurrentLean) * transform.rotation;
                Quaternion newLeanRot = Quaternion.AngleAxis(LeanAngleForVelocity(velWithLean), Vector3.Cross(Vector3.up, velWithLean.normalized));
                transform.rotation = newLeanRot * baseRotation;
                CurrentLean = newLeanRot;
            }
        }

        public void ResetRotation()
        {
            HintFacingAngle();
            CurrentLean = Quaternion.identity;
            transform.rotation = Quaternion.LookRotation(lastSignificantDir.normalized, Vector3.up);
            //transform.rotation = Quaternion.identity;
        }
    }

    [HarmonyPatch(typeof(PersonManager), nameof(PersonManager.InitializeOurPerson))]
    public static class AddFlightManager {
        public static FlightManager FM { get; private set; }
        public static void Postfix(PersonManager __instance)
        {
            FM = __instance.ourPerson.Rig.AddComponent<FlightManager>();
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;
        }

        private static Matrix4x4 GetTransformForChar(char ch)
        {
            Person ourPerson = Managers.personManager.ourPerson;
            switch (ch) {
                case '1': return ourPerson.Head.transform.localToWorldMatrix;
                case '2': return ourPerson.Torso.transform.localToWorldMatrix;
                case '3': return ourPerson.GetHandBySide(Side.Left).GetComponent<Hand>().handDot.transform.localToWorldMatrix;
                case '4': return ourPerson.GetHandBySide(Side.Right).GetComponent<Hand>().handDot.transform.localToWorldMatrix;
                case '5':
                    Matrix4x4 head = ourPerson.Head.transform.localToWorldMatrix;
                    Matrix4x4 torso = ourPerson.Torso.transform.localToWorldMatrix;
                    return new Matrix4x4(torso.GetColumn(0), new Vector4(0, 1, 0, 0), head.GetColumn(2), new Vector4(0, 0, 0, 1));
                default:
                    return Matrix4x4.identity;
            }
        }

        private static bool HandleTellWithVectorArg(string tell, string command, out Vector3 vec)
        {
            string[] args = tell.Substring(command.Length + 2).Split(' ');
            try {
                vec = GetTransformForChar(tell[command.Length]) * new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
                return true;
            } catch (IndexOutOfRangeException) {
                DebugLog.Log("Not enough arguments given to '{0}'.", command);
                vec = Vector3.zero;
                return false;
            } catch (FormatException) {
                DebugLog.Log("Invalid argument given to '{0}'.", command);
                vec = Vector3.zero;
                return false;
            }
        }

        private static void BodyTellManager_ToldByBody(string tell, BodyTellManager.TellEventInfo info)
        {
            float val;
            if (!info.IsTrusted)
                return;

            if (tell.StartsWith("xx fly ") && float.TryParse(tell.Substring(7), out val)) {
                FM.Acceleration = Managers.personManager.ourPerson.Head.transform.forward * val;
            } else if (tell.StartsWith("xx drag ") && float.TryParse(tell.Substring(8), out val)) {
                FM.DragFactor = val;
            } else if (tell.StartsWith("xx setvel")) {
                if (HandleTellWithVectorArg(tell, "xx setvel", out Vector3 vec)) {
                    FM.Velocity = vec;
                }
            } else if (tell.StartsWith("xx addvel")) {
                if (HandleTellWithVectorArg(tell, "xx addvel", out Vector3 vec)) {
                    FM.Velocity += vec;
                }
            } else if (tell.StartsWith("xx setacc")) {
                if (HandleTellWithVectorArg(tell, "xx setacc", out Vector3 vec)) {
                    FM.Acceleration = vec;
                }
            } else if (tell.Equals("xx resetrot")) {
                FM.ResetRotation();
            } else if (tell.Equals("xx grab")) {
                SpecialFlightModes.DefaultMode = SpecialFlightModes.FlightMode.Grab;
            } else if (tell.Equals("xx default")) {
                SpecialFlightModes.DefaultMode = SpecialFlightModes.FlightMode.Default;
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class SpecialFlightModes {
        public enum FlightMode {
            Default,
            Wings,
            Grab
        };

        private static float lastUpdateTime = -1.0f;
        private static FlightMode mode = FlightMode.Default;
        private static Vector3 dotLPosLast, dotRPosLast, dotLVelLast, dotRVelLast;
        private static float ourScaleLast = 1f;
        private static bool lastPosIsInvalid = true;

        public static FlightMode DefaultMode { get; set; } = FlightMode.Default;

        private static FlightManager FM {
            get => AddFlightManager.FM;  //shortcut
        }

        private static void StartMode(FlightMode mode)
        {
            FM.HintFacingAngle();
            if (mode == FlightMode.Default) {
                FM.DragFactor = 0.3f;
            } else if (mode == FlightMode.Grab) {
                FM.DragFactor = 0.1f;
            }
        }

        private static void UpdateMode(FlightMode mode)
        {
            Person me = Managers.personManager.ourPerson;
            Transform head = me.Head.transform;

            Hand handL = me.GetHandBySide(Side.Left).GetComponent<Hand>();
            Hand handR = me.GetHandBySide(Side.Right).GetComponent<Hand>();
            HandDot dotL = handL.handDot.GetComponent<HandDot>();
            HandDot dotR = handR.handDot.GetComponent<HandDot>();
            Vector3 dotLPos = Quaternion.Inverse(me.Torso.transform.rotation) * (dotL.transform.position - me.Torso.transform.position);
            Vector3 dotRPos = Quaternion.Inverse(me.Torso.transform.rotation) * (dotR.transform.position - me.Torso.transform.position);
            if (lastPosIsInvalid) {
                dotLPosLast = dotLPos;
                dotRPosLast = dotRPos;
                lastPosIsInvalid = false;
            }
            Vector3 dotLVel = (dotLPos - dotLPosLast) / Time.deltaTime;
            Vector3 dotRVel = (dotRPos - dotRPosLast) / Time.deltaTime;
            Vector3 dotAvgVel = (dotLVel + dotRVel) / 2;

            float ourScale = Managers.personManager.GetOurScale();
            Transform torso = me.Torso.transform;
            float handDist = (dotRPos - dotLPos).magnitude;

            if (mode == FlightMode.Default) {
                FM.HintFacingAngle();
            } else if (mode == FlightMode.Wings) {
                bool fingersClosedLeft = handL.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x > 0.25f;
                bool fingersClosedRight = handR.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x > 0.25f;
                if (fingersClosedLeft) {
                    dotLPos = dotLPosLast;
                    dotLVel = dotLVelLast;
                    lastPosIsInvalid = true;
                }
                if (fingersClosedRight) {
                    dotRPos = dotRPosLast;
                    dotRVel = dotRVelLast;
                    lastPosIsInvalid = true;
                }

                float gravityControl = (dotLPos - head.position).magnitude + (dotRPos - head.position).magnitude;
                float yAccel = Mathf.Lerp(-100.0f, 0.0f, gravityControl);
                float angle = 0.5f * Vector3.SignedAngle((handR.transform.position - handL.transform.position) / ourScale, torso.right, torso.forward);
                angle = (!fingersClosedLeft && !fingersClosedRight) ? angle : 0.0f;

                if (!fingersClosedLeft || !fingersClosedRight) {
                    FM.AccelWithLean = (Quaternion.Inverse(FM.CurrentLean) * torso.rotation) * new Vector3(0.0f, 0.0f, 20.0f * handDist * handDist / (ourScale * ourScale));
                    FM.Acceleration = FM.AccelWithLean;
                    FM.Acceleration += new Vector3(0, yAccel);
                    FM.Acceleration -= 15f * (torso.rotation * dotAvgVel) * dotAvgVel.magnitude / ourScale;
                    FM.AngularAcceleration = handDist / ourScale * angle * Vector3.up;
                    FM.DragFactor = Mathf.Clamp((-Vector3.SignedAngle(dotR.transform.position - handR.transform.position, torso.transform.forward, torso.transform.right) + 90.0f) / 180.0f, 0.0f, 1.0f);
                }
            } else if (mode == FlightMode.Grab) {
                FM.HintFacingAngle();
                FM.DragFactor = Mathf.Max(0.25f, 1.0f - handL.controller.GetAxis(EVRButtonId.k_EButton_Axis2).x);
                FM.Acceleration = -25f * (torso.rotation * dotAvgVel) * dotAvgVel.magnitude;
                Vector3 midpoint = (dotLPos + dotRPos) / 2;
                float angle = Vector3.SignedAngle(dotLPos - midpoint, dotLPosLast - midpoint, torso.up) + Vector3.SignedAngle(dotRPos - midpoint, dotRPosLast - midpoint, torso.up);
                FM.AngularVelocity += ((dotRPos - midpoint).magnitude + (dotLPos - midpoint).magnitude) * angle * angle * angle * Vector3.up / 20f;

                Vector3? tppos = Our.lastTeleportHitPoint;
                Our.lastTeleportHitPoint = null;
                if (CrossDevice.GetPress(handL.controller, CrossDevice.button_grabTip, Side.Left)) {
                    float ratio = (dotRPos - dotLPos).magnitude / ((dotRPosLast - dotLPosLast).magnitude * ourScale / ourScaleLast);
                    Managers.personManager.ApplyAndCachePhotonRigScale(ourScale * ratio, true);
                } else if (CrossDevice.GetPressDown(handL.controller, CrossDevice.button_grab, Side.Left)) {
                    Managers.personManager.ApplyAndCachePhotonRigScale(1.0f);
                }
                Our.lastTeleportHitPoint = tppos;
            }

            dotLPosLast = dotLPos;
            dotRPosLast = dotRPos;
            dotLVelLast = dotLVel;
            dotRVelLast = dotRVel;
            ourScaleLast = ourScale;
        }

        private static void EndMode(FlightMode mode)
        {
            FM.Acceleration = Vector3.zero;
            FM.AccelWithLean = Vector3.zero;
            FM.AngularAcceleration = Vector3.zero;

            if (mode == FlightMode.Wings) {
                FM.ResetRotation();
            }
        }

        public static void Postfix()
        {
            if (Time.time <= lastUpdateTime)
                return;

            lastUpdateTime = Time.time;

            FlightMode newMode = DefaultMode;
            Person me = Managers.personManager.ourPerson;
            Hand handL = me.GetHandBySide(Side.Left).GetComponent<Hand>();
            Hand handR = me.GetHandBySide(Side.Right).GetComponent<Hand>();
            HandDot dotL = handL.handDot.GetComponent<HandDot>();
            HandDot dotR = handR.handDot.GetComponent<HandDot>();
            var llobj = me.GetThingOnAttachmentPointById(AttachmentPointId.LegLeft);
            Thing leftLeg = null;
            if (llobj != null)
                leftLeg = llobj.GetComponent<Thing>();

            if (leftLeg != null && leftLeg.givenName.Contains("wings")) {
                newMode = FlightMode.Wings;
            } else if (CrossDevice.GetPress(dotL.controller, CrossDevice.button_teleport, Side.Left) && CrossDevice.GetPress(dotR.controller, CrossDevice.button_teleport, Side.Right)) {
                FM.HintFacingAngle();
                newMode = FlightMode.Grab;
            }

            if (newMode != mode) {
                EndMode(mode);
                mode = newMode;
                StartMode(mode);
            }

            UpdateMode(mode);
        }
    }
}

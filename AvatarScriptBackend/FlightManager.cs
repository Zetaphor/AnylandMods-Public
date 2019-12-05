﻿using System;
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
        public Quaternion AngularVelocity { get; set; }
        public Quaternion AngularAcceleration { get; set; }
        public float DragFactor { get; set; }

        public void Start()
        {
            DragFactor = 0.5f;
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
            AngularVelocity = Quaternion.identity;
            AngularAcceleration = Quaternion.identity;
        }

        public void Update()
        {
            Velocity += Acceleration * Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;

            AngularVelocity *= Quaternion.SlerpUnclamped(Quaternion.identity, AngularAcceleration, Time.deltaTime);
            transform.rotation *= Quaternion.SlerpUnclamped(Quaternion.identity, AngularVelocity, Time.deltaTime);

            float dragCoefficient = Mathf.Pow(DragFactor, Time.deltaTime);
            Velocity *= dragCoefficient;
            AngularVelocity = Quaternion.SlerpUnclamped(Quaternion.identity, AngularVelocity, dragCoefficient);
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
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "Update")]
    public static class SpecialFlightModes {
        private enum FlightMode {
            Default,
            Wings
        };

        private static float lastUpdateTime = -1.0f;
        private static FlightMode mode = FlightMode.Default;
        private static Vector3 dotLPosLast, dotRPosLast, dotLVelLast, dotRVelLast;
        private static bool lastPosIsInvalid = true;

        private static FlightManager FM {
            get => AddFlightManager.FM;  //shortcut
        }

        private static void StartMode(FlightMode mode)
        {
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
            Vector3 dotMidpoint = (dotLPos + dotRPos) / 2;
            Vector3 dotLVel = (dotLPos - dotLPosLast) / Time.deltaTime;
            Vector3 dotRVel = (dotRPos - dotRPosLast) / Time.deltaTime;

            Transform torso = me.Torso.transform;
            float handDist = (dotRPos - dotLPos).magnitude;

            if (mode == FlightMode.Wings) {
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

                Vector3 dotAvgVel = (dotLVel + dotRVel) / 2;
                float gravityControl = dotMidpoint.y - head.position.y; // expected range ~= -1 (min gravity) to 0.1 (max gravity)
                gravityControl = Mathf.Min((gravityControl + 1) * 4, 0.0f);
                float yAccel = Mathf.Min(-gravityControl - FM.Velocity.y, 0.0f);
                FM.Acceleration = torso.rotation * new Vector3(0.0f, 0.0f, 20.0f * handDist * handDist);
                FM.Acceleration += new Vector3(0, yAccel);
                FM.Acceleration -= 20f * (torso.rotation * dotAvgVel) * dotAvgVel.magnitude;
                float angle1 = 0.5f * Vector3.SignedAngle(handR.transform.position - handL.transform.position, torso.right, torso.forward);
                float angle2 = Vector3.SignedAngle(dotLVel, dotRVel, torso.up);
                angle2 *= 0.1f * dotLVel.magnitude * dotRVel.magnitude;
                angle2 *= angle2 / 360f;
                float angle = (!fingersClosedLeft && !fingersClosedRight) ? (angle1 + angle2) : 0.0f;
                FM.AngularAcceleration = Quaternion.AngleAxis(0.5f * handDist * angle, torso.up);
                FM.DragFactor = Mathf.Clamp((Vector3.SignedAngle(dotR.transform.position - handR.transform.position, torso.transform.forward, torso.transform.right) + 90.0f) / 180.0f, 0.0f, 1.0f);
            }

            dotLPosLast = dotLPos;
            dotRPosLast = dotRPos;
            dotLVelLast = dotLVel;
            dotRVelLast = dotRVel;
        }

        private static void EndMode(FlightMode mode)
        {
            if (mode == FlightMode.Wings) {
                FM.Acceleration = Vector3.zero;
                FM.AngularAcceleration = Quaternion.identity;
                FM.DragFactor = 0.3f;
            }
        }

        public static void Postfix()
        {
            if (Time.time <= lastUpdateTime)
                return;

            lastUpdateTime = Time.time;

            FlightMode newMode = FlightMode.Default;
            Person me = Managers.personManager.ourPerson;
            var llobj = me.GetThingOnAttachmentPointById(AttachmentPointId.LegLeft);
            Thing leftLeg = null;
            if (llobj != null)
                leftLeg = llobj.GetComponent<Thing>();

            if (leftLeg != null && leftLeg.givenName.Contains("wings")) {
                newMode = FlightMode.Wings;
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
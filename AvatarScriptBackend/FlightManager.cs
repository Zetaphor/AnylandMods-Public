using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;

namespace AnylandMods.AvatarScriptBackend {
    public class FlightManager : MonoBehaviour {
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        public float DragFactor { get; set; }

        public void Start()
        {
            DragFactor = 0.5f;
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
        }

        public void Update()
        {
            Velocity += Acceleration * Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;
            Velocity *= Mathf.Pow(DragFactor, Time.deltaTime);
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
}

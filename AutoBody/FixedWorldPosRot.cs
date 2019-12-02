using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class FixedWorldPosRot : MonoBehaviour {
        private Vector3 Position;
        private Vector3 Rotation;

        private void Start() { }

        public void Update()
        {
            transform.position = Position;
            transform.eulerAngles = Rotation;
        }

        public static void LockPosRot(GameObject obj)
        {
            var comp = obj.GetComponent<FixedWorldPosRot>();
            if (comp == null) {
                comp = obj.AddComponent<FixedWorldPosRot>();
            }
            comp.Position = obj.transform.position;
            comp.Rotation = obj.transform.eulerAngles;
            comp.enabled = true;
        }

        public static void UnlockPosRot(GameObject obj)
        {
            var comp = obj.GetComponent<FixedWorldPosRot>();
            if (comp != null)
                comp.enabled = false;
        }
    }
}

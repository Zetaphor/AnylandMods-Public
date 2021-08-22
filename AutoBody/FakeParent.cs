using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class FakeParent : MonoBehaviour {
        private Transform parent;
        private Vector3 lastPosReal;
        private Quaternion lastRotReal;
        private Vector3 lastPosFake;
        private Quaternion lastRotFake;

        public void GetFakeLocalPosRot(out Vector3 pos, out Quaternion rot)
        {
            if (transform.parent == Parent) {
                pos = transform.localPosition;
                rot = transform.localRotation;
            } else {
                Transform originalParent = transform.parent;
                transform.parent = Parent;
                pos = transform.localPosition;
                rot = transform.localRotation;
                transform.parent = originalParent;
            }
        }

        public void SetFakeLocalPosRot(Vector3 pos, Quaternion rot)
        {
            if (transform.parent == Parent) {
                transform.localPosition = pos;
                transform.localRotation = rot;
            } else {
                Transform originalParent = transform.parent;
                transform.parent = Parent;
                transform.localPosition = pos;
                transform.localRotation = rot;
                transform.parent = originalParent;
            }
        }

        public Transform Parent {
            get => parent;
            set {
                if (value == null)
                    parent = transform.parent;
                else
                    parent = value;
                Reset();
            }
        }

        public void Start()
        {
            if (Parent == null)
                Parent = transform.parent;
            Reset();
        }

        public void Reset()
        {
            lastPosReal = transform.localPosition;
            lastRotReal = transform.localRotation;
            GetFakeLocalPosRot(out lastPosFake, out lastRotFake);
        }

        public void Update()
        {
            if (Parent != transform.parent) {
                Vector3 deltaPos = transform.localPosition - lastPosReal;
                Quaternion deltaRot = transform.localRotation * Quaternion.Inverse(lastRotReal);
                SetFakeLocalPosRot(lastPosFake, lastRotFake);
                transform.localPosition += deltaPos;
                transform.localRotation *= deltaRot;
                lastPosReal = transform.localPosition;
                lastRotReal = transform.localRotation;
                GetFakeLocalPosRot(out lastPosFake, out lastRotFake);
            }
        }
    }
}

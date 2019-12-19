using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AvatarScriptBackend {
    class CopyPosition : MonoBehaviour {
        public Transform Target { get; set; } = null;

        public void Update()
        {
            if (Target != null) {
                transform.position = Target.position;
            }
        }
    }
}

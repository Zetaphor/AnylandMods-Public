using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class DelayedSwitch : MonoBehaviour {
        private float timeLeft;
        private float delay;
        private Vector3 start;
        private Vector3 end;
        private AttachmentPointId apid;
        private string thingName;
        private bool shouldMove;

        private void Start() { }

        public void Begin(AttachmentPointId point, string thingName, float delay, Vector3? newPos = null)
        {
            DebugLog.LogTemp("apid={0}, tn={1}, d={2}, np={3}", point, thingName, delay, newPos);
            apid = point;
            this.thingName = thingName;
            this.delay = timeLeft = delay;
            shouldMove = (newPos.HasValue && point == AttachmentPointId.LegLeft || point == AttachmentPointId.LegRight);
            if (shouldMove) {
                start = transform.localPosition;
                end = newPos.Value;
            }
            enabled = true;
        }

        public void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0.0f) {
                Main.SetAttachment(apid, thingName, shouldMove);
                enabled = false;
            } else if (shouldMove) {
                transform.localPosition = Vector3.Lerp(end, start, timeLeft / delay);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace AnylandMods {
    public abstract class CustomDialog : Dialog {
        protected abstract void InitCustomDialog(object arg = null);

        public static GameObject SwitchTo<TDialog>(object arg = null, Hand hand = null, string tabName = "") where TDialog : CustomDialog
        {
            if (hand == null) {
                hand = Managers.dialogManager.GetDialogHand();
            }
            if (hand.currentDialog != null) {
                UnityEngine.Object.Destroy(hand.currentDialog);
            }
            hand.currentDialog = new GameObject("Dialog");
            hand.currentDialog.AddComponent<TDialog>().InitCustomDialog(arg);
            hand.currentDialog.gameObject.name = "MenuDialog";
            hand.currentDialog.transform.parent = hand.transform;
            hand.currentDialog.SetActive(true);
            Dialog component = hand.currentDialog.GetComponent<Dialog>();
            component.tabName = tabName;
            hand.TriggerHapticPulse(Universe.miniBurstPulse);
            return hand.currentDialog;
        }

        public static GameObject SwitchTo<TDialog>(Hand hand = null, string tabName = "") where TDialog : CustomDialog
        {
            return SwitchTo<TDialog>(null, hand, tabName);
        }
    }
}

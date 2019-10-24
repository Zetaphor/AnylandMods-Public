using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace AnylandMods {
    public static partial class Foundation {
        private static int CompareByFirstKey<T1, T2>(Tuple<T1, T2> a, Tuple<T1, T2> b) where T1 : IComparable
        {
            return a.Item1.CompareTo(b.Item1);
        }

        public static GameObject SwitchToDialog<TDialog>(object arg = null, Hand hand = null, string tabName = "") where TDialog : CustomDialog
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

        public static GameObject SwitchToDialog<TDialog>(Hand hand = null, string tabName = "") where TDialog : CustomDialog
        {
            return SwitchToDialog<TDialog>(null, hand, tabName);
        }
    }

    public abstract class CustomDialog : Dialog {
        protected internal abstract void InitCustomDialog(object arg = null);
    }
}

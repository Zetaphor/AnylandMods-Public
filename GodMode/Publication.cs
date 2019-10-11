using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace AnylandMods
{
    static class Publication
    {
        public static GameObject AddCheckbox(this Dialog dialog, string contextName = null, string contextId = null, string text = null, int xOnFundament = 0, int yOnFundament = 0, bool state = false, float textSizeFactor = 1f, string prefabName = "Checkbox", TextColor textColor = TextColor.Default, string footnote = null, ExtraIcon extraIcon = ExtraIcon.None)
        {
            object[] args = new object[] { contextName, contextId, text, xOnFundament, yOnFundament, state, textSizeFactor, prefabName, textColor, footnote, extraIcon };
            return (GameObject)typeof(Dialog).GetMethod("AddCheckbox", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, args);
        }

        public static void SetFundamentColor(this Dialog dialog, Color color)
        {
            typeof(Dialog).GetMethod("SetFundamentColor", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, new object[] { color });
        }

        public static GameObject SwitchTo(this Dialog dialog, DialogType dialogType, string tabName = "")
        {
            return (GameObject)typeof(Dialog).GetMethod("SwitchTo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, new object[] { dialogType, tabName });
        }
    }
}

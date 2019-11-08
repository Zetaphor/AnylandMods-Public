using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;

namespace AnylandMods {
    public static class DebugLog {
        public static string LogText { get; private set; }
        private static List<string> lines;
        private static int maxLines = 30;

        public static int MaxLines {
            get {
                return maxLines;
            }
            set {
                maxLines = value;
                UpdateLogText();
            }
        }

        static DebugLog()
        {
            LogText = "";
            lines = new List<string>();
        }

        private static void UpdateLogText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in lines) {
                sb.AppendLine(line);
            }
            LogText = sb.ToString();
        }

        public static string LogTemp(string format, params object[] args)
        {
            string text = String.Format(format, args);
            lines.Add(text);
            if (lines.Count > maxLines)
                lines.RemoveAt(0);
            UpdateLogText();
            return text;
        }

        public static string Log(string format, params object[] args)
        {
            string text = LogTemp(format, args);
            FileLog.Log(text);
            return text;
        }
    }

    namespace FoundationPatches {
        [HarmonyPatch(typeof(ThingPart), "ReplaceTextPlaceholders")]
        public static class AddDebugLogPlaceholder {
            public static void Postfix(string s, ref string __result)
            {
                if (__result != null) {
                    __result = __result.ReplaceCaseInsensitive("[mod debug log]", DebugLog.LogText);
                }
            }
        }
    }
}

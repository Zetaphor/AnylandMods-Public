using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnylandMods.ScriptableControls {
    class ControlState {
        public static class Flags {
            public const UInt64 ContextLaser = 1;
            public const UInt64 Delete = 2;
            public const UInt64 FingersClosed = 4;
            public const UInt64 LegControl = 8;
            public const UInt64 TeleportLaser = 16;
            public const UInt64 Trigger = 32;
            public const UInt64 Grab = 64;
            public const int BitsToShiftForLeft = 7;
            public const UInt64 RightMask = 0b1111111;
            public const UInt64 LeftMask = RightMask << BitsToShiftForLeft;

            public static UInt64 BitValueForLetter(char letter)
            {
                switch (letter) {
                    case 'c': return ContextLaser;
                    case 'd': return Delete;
                    case 'f': return FingersClosed;
                    case 'g': return Grab;
                    case 'l': return LegControl;
                    case 'p': return TeleportLaser;
                    case 't': return Trigger;
                    default: return 0;
                }
            }
        }

        private static Regex tellRegex;

        #region Static stuff

        static ControlState()
        {
            tellRegex = new Regex("^xc([blr]?)([01]) ?([cdfglpt]*)-?([cdfglpt]*)$");
        }

        public static bool TryParseTellString(string tell, out IFlagTest test)
        {
            test = null;
            Match match = tellRegex.Match(tell);
            if (!match.Success) return false;

            string p_side = match.Groups[1].Value;
            char p_state = match.Groups[2].Value[0];
            string p_true = match.Groups[3].Value;
            string p_false = match.Groups[4].Value;

            UInt64 flags = 0;
            UInt64 mask = 0;

            foreach (char c in p_true) {
                flags |= Flags.BitValueForLetter(c);
            }
            mask = flags;
            foreach (char c in p_false) {
                UInt64 bit = Flags.BitValueForLetter(c);
                if ((flags & bit) != 0) {
                    // None of them can ever be both true and false.
                    return false;
                }
            }

            var left = new MaskTest(flags << Flags.BitsToShiftForLeft, mask << Flags.BitsToShiftForLeft);
            var right = new MaskTest(flags, mask);

            if (p_state == '1') {
                switch (p_side) {
                    case "b": test = left & right; return true;
                    case "l": test = left; return true;
                    case "r": test = right; return true;
                    case "": test = left | right; return true;
                    default: return false;
                }
            } else {
                switch (p_side) {
                    case "b": test = !(left & right); return true;
                    case "l": test = !left; return true;
                    case "r": test = !right; return true;
                    case "": test = !(left | right); return true;
                    default: return false;
                }
            }
        }

        #endregion

        public IFlagTest Test { get; set; }
        public bool State { get; set; }
        public string Label { get; set; }
        public bool Edge { get; private set; }

        public ControlState(string label, IFlagTest test)
        {
            Label = label;
            Test = test;
            State = false;
            Edge = false;
        }

        public ControlState(string label)
        {
            Label = label;
            State = false;
            Edge = false;

            IFlagTest test;
            if (TryParseTellString(label, out test)) {
                Test = test;
            } else {
                Test = null;
            }
        }

        public void Update(UInt64 flags)
        {
            bool oldState = State;
            if (Test is null) {
                State = false;
            } else {
                State = Test.Evaluate(flags);
            }
            Edge = (State != oldState);
        }
    }
}

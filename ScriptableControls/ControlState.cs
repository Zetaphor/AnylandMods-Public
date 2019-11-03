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
            public const UInt64 Holding = 128;
            public const UInt64 PosX0 = 256;
            public const UInt64 PosX1 = 512;
            public const UInt64 PosX2 = 1024;
            public const UInt64 PosY0 = 2048;
            public const UInt64 PosY1 = 4096;
            public const UInt64 PosY2 = 8192;
            public const UInt64 PosZ0 = 16384;
            public const UInt64 PosZ1 = 32768;
            public const UInt64 PosZ2 = 65536;
            public const int BitsToShiftForLeft = 17;
            public const UInt64 RightMask = 0b11111111111111111;
            public const UInt64 LeftMask = RightMask << BitsToShiftForLeft;

            public static UInt64 BitValueForLetter(char letter)
            {
                switch (letter) {
                    case 'c': return ContextLaser;
                    case 'd': return Delete;
                    case 'f': return FingersClosed;
                    case 'g': return Grab;
                    case 'h': return Holding;
                    case 'l': return LegControl;
                    case 'r': return TeleportLaser;
                    case 't': return Trigger;
                    default: return 0;
                }
            }
        }

        private static Regex tellRegex;

        #region Static stuff

        static ControlState()
        {
            tellRegex = new Regex("^xc([blr]?)([01]) ?([cdfglrtp012]*)-?([cdfglrtp012]*)$");
        }

        private static UInt64 StringToFlags(string str)
        {
            UInt64 flags = 0;
            char mode = 'p';
            char axis = 'x';
            foreach (char c in str) {
                if (c == 'p' || c == 'v' || c == 'q') {
                    mode = c;
                } else if (c == 'x' || c == 'y' || c == 'z') {
                    axis = c;
                } else if (c == '0' || c == '1' || c == '2') {
                    var seq = new string(new char[] { mode, axis, c });
                    switch (seq) {
                        case "px0": flags |= Flags.PosX0; break;
                        case "px1": flags |= Flags.PosX1; break;
                        case "px2": flags |= Flags.PosX2; break;
                        case "py0": flags |= Flags.PosY0; break;
                        case "py1": flags |= Flags.PosY1; break;
                        case "py2": flags |= Flags.PosY2; break;
                        case "pz0": flags |= Flags.PosZ0; break;
                        case "pz1": flags |= Flags.PosZ1; break;
                        case "pz2": flags |= Flags.PosZ2; break;
                    }
                } else {
                    flags |= Flags.BitValueForLetter(c);
                }
            }
            return flags;
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

            DebugLog.Log(String.Format("p_side={0} p_state={1} p_true={2} p_false={3}", p_side, p_state, p_true, p_false));

            UInt64 f_true = StringToFlags(p_true);
            UInt64 f_false = StringToFlags(p_false);
            UInt64 flags = f_true;
            UInt64 mask = f_true | f_false;
            
            DebugLog.Log("flags={0} mask={1}", flags, mask);

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

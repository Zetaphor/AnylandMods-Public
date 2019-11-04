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
            public const UInt64 Moving = 256;
            public const UInt64 MovingFast = 16777216;
            public const UInt64 PosX0 = 512;
            public const UInt64 PosX1 = 1024;
            public const UInt64 PosX2 = 2048;
            public const UInt64 PosY0 = 4096;
            public const UInt64 PosY1 = 8192;
            public const UInt64 PosY2 = 16384;
            public const UInt64 PosZ0 = 32768;
            public const UInt64 PosZ1 = 65536;
            public const UInt64 PosZ2 = 131072;
            public const UInt64 DirLeft = 262144;
            public const UInt64 DirRight = 524288;
            public const UInt64 DirUp = 1048576;
            public const UInt64 DirDown = 2097152;
            public const UInt64 DirFwd = 4194304;
            public const UInt64 DirBack = 8388608;
            
            public const int BitsToShiftForLeft = 25;
            public const UInt64 RightMask = 0b1111111111111111111111111;
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
                    case 'm': return Moving;
                    case 'n': return MovingFast;
                    case 'r': return TeleportLaser;
                    case 't': return Trigger;
                    default: return 0;
                }
            }
        }

        #region Static stuff

        private static Regex tellRegex;
        private static Dictionary<string, IFlagTest> testcache;

        static ControlState()
        {
            tellRegex = new Regex("^xc([blr]?)([01]) ?([cdfglmnrtpqxyz0-5]*)-?([cdfglmnrtpqxyz0-5]*)$");
            testcache = new Dictionary<string, IFlagTest>();
        }

        private static UInt64 StringToFlags(string str)
        {
            UInt64 flags = 0;
            char mode = 'p';
            char axis = 'x';
            foreach (char c in str) {
                if (c == 'p' || c == 'q') {
                    mode = c;
                } else if (c == 'x' || c == 'y' || c == 'z') {
                    axis = c;
                } else if ('0' <= c && c <= '5') {
                    if (mode == 'p') {
                        var seq = new string(new char[] { axis, c });
                        switch (seq) {
                            case "x0": flags |= Flags.PosX0; break;
                            case "x1": flags |= Flags.PosX1; break;
                            case "x2": flags |= Flags.PosX2; break;
                            case "y0": flags |= Flags.PosY0; break;
                            case "y1": flags |= Flags.PosY1; break;
                            case "y2": flags |= Flags.PosY2; break;
                            case "z0": flags |= Flags.PosZ0; break;
                            case "z1": flags |= Flags.PosZ1; break;
                            case "z2": flags |= Flags.PosZ2; break;
                        }
                    } else if (mode == 'q') {
                        switch (c) {
                            case '0': flags |= Flags.DirLeft; break;
                            case '1': flags |= Flags.DirRight; break;
                            case '2': flags |= Flags.DirDown; break;
                            case '3': flags |= Flags.DirUp; break;
                            case '4': flags |= Flags.DirBack; break;
                            case '5': flags |= Flags.DirFwd; break;
                        }
                    }
                } else {
                    flags |= Flags.BitValueForLetter(c);
                }
            }
            return flags;
        }

        public static bool TryParseTellString(string tell, out IFlagTest test)
        {
            if (testcache.TryGetValue(tell, out test)) {
                return true;
            }

            test = null;
            Match match = tellRegex.Match(tell);
            if (!match.Success) {
                testcache[tell] = null;
                return false;
            }

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

            switch (p_side) {
                case "b": test = left & right; break;
                case "l": test = left; break;
                case "r": test = right; break;
                case "": test = left | right; break;
                default: return false;
            }

            if (p_state == '0')
                test = test.Complement;

            testcache[tell] = test;

            return true;
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

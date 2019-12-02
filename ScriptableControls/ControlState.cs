using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnylandMods.ScriptableControls {
    class ControlState {
        public static class Flags {
            public const UInt64 ContextLaser = 0x1;
            public const UInt64 Delete = 0x2;
            public const UInt64 FingersClosed = 0x4;
            public const UInt64 LegControl = 0x8;
            public const UInt64 TeleportLaser = 0x10;
            public const UInt64 Trigger = 0x20;
            public const UInt64 Grab = 0x40;
            public const UInt64 Holding = 0x80;
            public const UInt64 Moving = 0x100;
            public const UInt64 MovingFast = 0x200;
            public const UInt64 PosX0 = 0x400;
            public const UInt64 PosX1 = 0x800;
            public const UInt64 PosX2 = 0x1000;
            public const UInt64 PosY0 = 0x2000;
            public const UInt64 PosY1 = 0x4000;
            public const UInt64 PosY2 = 0x8000;
            public const UInt64 PosZ0 = 0x10000;
            public const UInt64 PosZ1 = 0x20000;
            public const UInt64 PosZ2 = 0x40000;
            public const UInt64 DirLeft = 0x80000;
            public const UInt64 DirRight = 0x100000;
            public const UInt64 DirUp = 0x200000;
            public const UInt64 DirDown = 0x400000;
            public const UInt64 DirFwd = 0x800000;
            public const UInt64 DirBack = 0x1000000;
            public const UInt64 HandsApart = 0x2000000;
            public const UInt64 BothTogether = 0x4000000;

            public const int BitsToShiftForLeft = 32;
            public const UInt64 RightMask = 0b11111111111111111111111111111111;
            public const UInt64 LeftMask = RightMask << BitsToShiftForLeft;

            public static UInt64 BitValueForLetter(char letter)
            {
                switch (letter) {
                    case 'a': return HandsApart;
                    case 'b': return BothTogether;
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
        private static Dictionary<string, ControlState> testcache;

        static ControlState()
        {
            tellRegex = new Regex("^xc([blr]?)([0-3]) ?([abcdfghlmnrtqxyz0-5]*)-?([abcdfghlmnrtqxyz0-5]*)-?([abcdfghlmnrtqxyz0-5]*)$");
            testcache = new Dictionary<string, ControlState>();
        }

        private static UInt64 StringToFlags(string str)
        {
            UInt64 flags = 0;
            char mode = '-';
            foreach (char c in str) {
                if (c == 'q' || c == 'x' || c == 'y' || c == 'z') {
                    mode = c;
                } else if ('0' <= c && c <= '5') {
                    if (mode == 'x' || mode == 'y' || mode == 'z') {
                        var seq = new string(new char[] { mode, c });
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

        public static bool TryParseTellString(string tell, out ControlState state)
        {
            if (testcache.TryGetValue(tell, out state)) {
                return state != null;
            }

            state = null;
            Match match = tellRegex.Match(tell);
            if (!match.Success) {
                testcache[tell] = null;
                return false;
            }

            string p_side = match.Groups[1].Value;
            char p_state = match.Groups[2].Value[0];
            string p_true = match.Groups[3].Value;
            string p_false = match.Groups[4].Value;
            string p_edge = match.Groups[5].Value;

            DebugLog.Log(String.Format("p_side={0} p_state={1} p_true={2} p_false={3} p_edge={4}", p_side, p_state, p_true, p_false, p_edge));

            UInt64 f_true = StringToFlags(p_true);
            UInt64 f_false = StringToFlags(p_false);
            UInt64 flags = f_true;
            UInt64 mask = f_true | f_false;

            DebugLog.Log("flags={0} mask={1}", flags, mask);

            var left = new MaskTest(flags << Flags.BitsToShiftForLeft, mask << Flags.BitsToShiftForLeft);
            var right = new MaskTest(flags, mask);

            IFlagTest test;
            switch (p_side) {
                case "b": test = left & right; break;
                case "l": test = left; break;
                case "r": test = right; break;
                case "": test = left | right; break;
                default: return false;
            }

            if (p_state == '0' || p_state == '2')
                test = test.Complement;

            bool constantTrigger = p_state == '2' || p_state == '3';
            testcache[tell] = state = new ControlState(tell, test, StringToFlags(p_edge), constantTrigger);

            return true;
        }

        #endregion

        public IFlagTest Test { get; set; }
        public bool State { get; set; }
        public string Label { get; set; }
        public bool Edge { get; private set; }
        public bool ConstantTrigger { get; set; }
        public UInt64 RequireEdge { get; set; }
        public UInt64 LastFlags { get; private set; }
        public UInt64 FlagsAtEdge { get; private set; }
        public float LastTrigTime { get; set; }
        
        public bool AtRequiredEdge {
            get {
                return RequireEdge == 0 || (FlagsAtEdge & RequireEdge) != 0;
            }
        }

        public bool ShouldTrigger {
            get {
                return (ConstantTrigger || Edge) && State && AtRequiredEdge;
            }
        }

        public ControlState(string label, IFlagTest test, UInt64 requireEdge, bool constantTrigger)
        {
            Label = label;
            Test = test;
            State = false;
            Edge = false;
            RequireEdge = requireEdge;
            ConstantTrigger = constantTrigger;
            LastFlags = 0;
            LastTrigTime = -1;
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
            FlagsAtEdge = flags ^ LastFlags;
            LastFlags = flags;
        }
    }
}

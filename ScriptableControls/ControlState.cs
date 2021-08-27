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
            public const UInt64 DirIn = 0x400000000;
            public const UInt64 DirOut = 0x800000000;
            public const UInt64 HandsApart = 0x2000000;
            public const UInt64 BothTogether = 0x4000000;
            public const UInt64 PointLeft = 0x8000000;
            public const UInt64 PointRight = 0x10000000;
            public const UInt64 PointUp = 0x20000000;
            public const UInt64 PointDown = 0x40000000;
            public const UInt64 PointFwd = 0x80000000;
            public const UInt64 PointBack = 0x100000000;
            public const UInt64 PointIn = 0x200000000;
            public const UInt64 PointOut = 0x400000000;
            public const UInt64 PalmLeft = 0x800000000;
            public const UInt64 PalmRight = 0x1000000000;
            public const UInt64 PalmUp = 0x2000000000;
            public const UInt64 PalmDown = 0x4000000000;
            public const UInt64 PalmFwd = 0x8000000000;
            public const UInt64 PalmBack = 0x10000000000;
            public const UInt64 PalmIn = 0x20000000000;
            public const UInt64 PalmOut = 0x40000000000;

            public const int BitsToShiftForLeft = 64;
            public static readonly FlagSet RightMask = new FlagSet(0, ~0UL);
            public static readonly FlagSet LeftMask = new FlagSet(~0UL, 0);

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
            tellRegex = new Regex("^xc([blr]?)([0-3]) ?([abcdfghlmnprtqxyz0-7]*)-?([abcdfghlmnprtqxyz0-7]*)-?([abcdfghlmnprtqxyz0-7]*)$");
            testcache = new Dictionary<string, ControlState>();
        }

        private static UInt64 StringToFlags(string str)
        {
            UInt64 flags = 0;
            char mode = '-';
            foreach (char c in str) {
                if ("opqxyz".Contains(c)) {
                    mode = c;
                } else if ('0' <= c && c <= '7') {
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
                            case '6': flags |= Flags.DirIn; break;
                            case '7': flags |= Flags.DirOut; break;
                        }
                    } else if (mode == 'p') {
                        switch (c) {
                            case '0': flags |= Flags.PointLeft; break;
                            case '1': flags |= Flags.PointRight; break;
                            case '2': flags |= Flags.PointDown; break;
                            case '3': flags |= Flags.PointUp; break;
                            case '4': flags |= Flags.PointFwd; break;
                            case '5': flags |= Flags.PointBack; break;
                            case '6': flags |= Flags.PointIn; break;
                            case '7': flags |= Flags.PointOut; break;
                        }
                    } else if (mode == 'o') {
                        switch (c) {
                            case '0': flags |= Flags.PalmLeft; break;
                            case '1': flags |= Flags.PalmRight; break;
                            case '2': flags |= Flags.PalmDown; break;
                            case '3': flags |= Flags.PalmUp; break;
                            case '4': flags |= Flags.PalmFwd; break;
                            case '5': flags |= Flags.PalmBack; break;
                            case '6': flags |= Flags.PalmIn; break;
                            case '7': flags |= Flags.PalmOut; break;
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

            UInt64 f_true = StringToFlags(p_true);
            UInt64 f_false = StringToFlags(p_false);
            UInt64 flags = f_true;
            UInt64 mask = f_true | f_false;

            DebugLog.Log("flags={0} mask={1}", flags, mask);

            var left = new MaskTest(new FlagSet(flags, 0), new FlagSet(mask, 0));
            var right = new MaskTest(new FlagSet(0, flags), new FlagSet(0, mask));

            IFlagTest test;
            FlagSet edge;
            UInt64 edgeflags = StringToFlags(p_edge);
            switch (p_side) {
                case "b":
                    test = left & right;
                    edge = new FlagSet(edgeflags, edgeflags);
                    break;
                case "l":
                    test = left;
                    edge = new FlagSet(edgeflags, 0);
                    break;
                case "r":
                    test = right;
                    edge = new FlagSet(0, edgeflags);
                    break;
                case "":
                    test = left | right;
                    edge = FlagSet.Zeros;
                    DebugLog.LogTemp("WARNING: p_side = \"\"");
                    break;
                default: return false;
            }

            if (p_state == '0' || p_state == '2')
                test = test.Complement;

            bool constantTrigger = p_state == '2' || p_state == '3';
            testcache[tell] = state = new ControlState(tell, test, edge, constantTrigger);

            return true;
        }

        #endregion

        public IFlagTest Test { get; set; }
        public bool State { get; set; }
        public string Label { get; set; }
        public bool Edge { get; private set; }
        public bool ConstantTrigger { get; set; }
        public FlagSet RequireEdge { get; set; }
        public FlagSet LastFlags { get; private set; }
        public FlagSet FlagsAtEdge { get; private set; }
        public float LastTrigTime { get; set; }
        
        public bool AtRequiredEdge {
            get {
                return RequireEdge == FlagSet.Zeros || (FlagsAtEdge & RequireEdge) != FlagSet.Zeros;
            }
        }

        public bool ShouldTrigger {
            get {
                return (ConstantTrigger || Edge) && State && AtRequiredEdge;
            }
        }

        public ControlState(string label, IFlagTest test, FlagSet requireEdge, bool constantTrigger)
        {
            Label = label;
            Test = test;
            State = false;
            Edge = false;
            RequireEdge = requireEdge;
            ConstantTrigger = constantTrigger;
            LastFlags = FlagSet.Zeros;
            LastTrigTime = -1;
        }

        public void Update(FlagSet flags)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnylandMods.ScriptableControls {
    static class ControlState {
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
        
        static ControlState()
        {
            tellRegex = new Regex("^xc([blr]?)([01]) ?([cdfglpt]*)-?([cdfglpt]*)$");
        }

        public static IFlagTest ParseTellString(string tell)
        {
            Match match = tellRegex.Match(tell);
            if (!match.Success) return MaskTest.Falsehood;

            string p_side = match.Captures[0].Value;
            char p_state = match.Captures[1].Value[0];
            string p_true = match.Captures[2].Value;
            string p_false = match.Captures[3].Value;

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
                    return MaskTest.Falsehood;
                }
            }

            var left = new MaskTest(flags << Flags.BitsToShiftForLeft, mask << Flags.BitsToShiftForLeft);
            var right = new MaskTest(flags, mask);

            if (p_state == '1') {
                switch (p_side) {
                    case "b": return left & right;
                    case "l": return left;
                    case "r": return right;
                    case "": return left | right;
                    default: return MaskTest.Falsehood;
                }
            } else {
                switch (p_side) {
                    case "b": return !(left & right);
                    case "l": return !left;
                    case "r": return !right;
                    case "": return !(left | right);
                    default: return MaskTest.Falsehood;
                }
            }
        }
    }
}

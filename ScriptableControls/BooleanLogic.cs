using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.ScriptableControls {
    struct MaskTest {
        public enum Mode {
            And = 0,
            Or = 1
        };

        public UInt64 flags;
        public UInt64 mask;
        public Mode mode;

        public MaskTest(UInt64 flags, UInt64 mask, Mode mode = Mode.And)
        {
            this.flags = flags;
            this.mask = mask;
            this.mode = mode;
        }

        public MaskTest(UInt64 flags, Mode mode = Mode.And)
            : this(flags, flags, mode)
        { }

        public bool Evaluate(UInt64 currentFlags)
        {
            if (mode == Mode.And)
                return (currentFlags & mask) == (flags & mask);
            else // Mode.Or
                return (~(currentFlags ^ flags) & mask) == 0;
        }

        private static Mode OppositeMode(Mode mode)
        {
            return mode == Mode.And ? Mode.Or : Mode.And;
        }

        public MaskTest Complement {
            get {
                return new MaskTest(~flags, mask, OppositeMode(mode));
            }
        }

        public static MaskTest operator!(MaskTest target)
        {
            return target.Complement;
        }
    }
}

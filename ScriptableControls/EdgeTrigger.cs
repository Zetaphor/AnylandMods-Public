using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// I think I'm going to redo this in a way that works better for what I'm trying to do
// Just committing it so I'll still have it.

namespace AnylandMods.ScriptableControls {
    class EdgeTrigger {
        private UInt64 flags;

        public delegate void FlagChangeEvent(int flagnum, bool state);
        public event FlagChangeEvent FlagChange;

        protected virtual void OnFlagChange(int flagnum, bool state)
        {
            if (FlagChange != null)
                FlagChange(flagnum, state);
        }

        public UInt64 Flags {
            get {
                return flags;
            }
            set {
                UInt64 edges = flags ^ value;
                flags = value;

                UInt64 flags_copy = flags & edges;
                UInt64 edges_copy = edges;
                int flagnum = 0;
                while (flags_copy > 0) {
                    if ((edges & 1) == 1)
                        OnFlagChange(flagnum, (flags_copy & 1) == 1);
                    flags_copy >>= 1;
                    edges_copy >>= 1;
                }
            }
        }

        public bool this[int flagnum] {
            get {
                return (flags & (1UL << flagnum)) > 0;
            }
            set {
                UInt64 mask = 1UL << flagnum;
                if (value)
                    flags |= mask;
                else
                    flags &= ~mask;
                OnFlagChange(flagnum, value);
            }
        }
    }
}

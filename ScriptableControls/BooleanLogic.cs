using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.ScriptableControls {
    enum BooleanOp {
        And = 0,
        Or = 1
    };

    interface IFlagTest {
        bool Evaluate(UInt64 currentFlags);
        IFlagTest Complement { get; }
    }

    class MaskTest : IFlagTest {
        public UInt64 Flags { get; private set; }
        public UInt64 Mask { get; private set; }
        public BooleanOp Mode { get; private set; }

        public static MaskTest Tautology { get; private set; }
        public static MaskTest Falsehood { get; private set; }
        
        static MaskTest()
        {
            Tautology = new MaskTest(0, 0, BooleanOp.And);
            Falsehood = new MaskTest(0, 0, BooleanOp.Or);
        }

        public MaskTest(UInt64 flags, UInt64 mask, BooleanOp mode = BooleanOp.And)
        {
            this.Flags = flags;
            this.Mask = mask;
            this.Mode = mode;
        }

        public MaskTest(UInt64 flags, BooleanOp mode = BooleanOp.And)
            : this(flags, flags, mode)
        { }

        public bool Evaluate(UInt64 currentFlags)
        {
            if (Mode == BooleanOp.And)
                return (currentFlags & Mask) == (Flags & Mask);
            else // Mode.Or
                return Mask != 0 && (~(currentFlags ^ Flags) & Mask) == 0;
        }

        internal static BooleanOp OppositeMode(BooleanOp mode)
        {
            return mode == BooleanOp.And ? BooleanOp.Or : BooleanOp.And;
        }

        public MaskTest Complement {
            get {
                return new MaskTest(~Flags, Mask, OppositeMode(Mode));
            }
        }

        IFlagTest IFlagTest.Complement {
            get {
                return Complement;
            }
        }

        public static MaskTest operator!(MaskTest target)
        {
            return target.Complement;
        }

        public static CompoundTest operator|(MaskTest a, IFlagTest b)
        {
            var tests = new IFlagTest[] { a, b };
            return new CompoundTest(tests, BooleanOp.Or);
        }

        public static CompoundTest operator&(MaskTest a, IFlagTest b)
        {
            var tests = new IFlagTest[] { a, b };
            return new CompoundTest(tests, BooleanOp.And);
        }
    }

    class CompoundTest : IFlagTest {
        private IFlagTest[] tests;
        private BooleanOp mode;

        private CompoundTest() { }

        public CompoundTest(IEnumerable<IFlagTest> tests, BooleanOp mode = BooleanOp.And)
        {
            this.tests = tests.ToArray();
            this.mode = mode;
        }

        public bool Evaluate(UInt64 currentFlags)
        {
            if (mode == BooleanOp.And) {
                foreach (IFlagTest test in tests) {
                    if (!test.Evaluate(currentFlags)) return false;
                }
                return true;
            } else {
                foreach (IFlagTest test in tests) {
                    if (test.Evaluate(currentFlags)) return true;
                }
                return false;
            }
        }

        private static IFlagTest FlagTestToComplement(IFlagTest test)
        {
            return test.Complement;
        }

        public CompoundTest Complement {
            get {
                var ct = new CompoundTest();
                ct.tests = tests.Select(FlagTestToComplement).ToArray();
                ct.mode = MaskTest.OppositeMode(mode);
                return ct;
            }
        }

        IFlagTest IFlagTest.Complement {
            get {
                return Complement;
            }
        }

        public static CompoundTest operator!(CompoundTest target)
        {
            return target.Complement;
        }

        public static CompoundTest operator|(CompoundTest a, IFlagTest b)
        {
            var tests = new IFlagTest[] { a, b };
            return new CompoundTest(tests, BooleanOp.Or);
        }

        public static CompoundTest operator&(CompoundTest a, IFlagTest b)
        {
            var tests = new IFlagTest[] { a, b };
            return new CompoundTest(tests, BooleanOp.And);
        }
    }
}

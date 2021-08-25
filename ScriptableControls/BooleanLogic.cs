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
        bool Evaluate(FlagSet currentFlags);
        IFlagTest Complement { get; }
    }

    class MaskTest : IFlagTest {
        public FlagSet Flags { get; private set; }
        public FlagSet Mask { get; private set; }
        public BooleanOp Mode { get; private set; }

        public static MaskTest Tautology { get; private set; }
        public static MaskTest Falsehood { get; private set; }
        
        static MaskTest()
        {
            Tautology = new MaskTest(FlagSet.Zeros, FlagSet.Zeros, BooleanOp.And);
            Falsehood = new MaskTest(FlagSet.Zeros, FlagSet.Zeros, BooleanOp.Or);
        }

        public MaskTest(FlagSet flags, FlagSet mask, BooleanOp mode = BooleanOp.And)
        {
            this.Flags = flags;
            this.Mask = mask;
            this.Mode = mode;
        }

        public MaskTest(FlagSet flags, BooleanOp mode = BooleanOp.And)
            : this(flags, flags, mode)
        { }

        public bool Evaluate(FlagSet currentFlags)
        {
            if (Mode == BooleanOp.And)
                return (currentFlags & Mask) == (Flags & Mask);
            else // Mode.Or
                return Mask != FlagSet.Zeros && (~(currentFlags ^ Flags) & Mask) != FlagSet.Zeros;
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

        /*public override string ToString()
        {
            var chars = new Stack<char>();
            const int bits = ControlState.Flags.BitsToShiftForLeft * 2;
            FlagSet ftemp = Flags;
            FlagSet mtemp = Mask;
            for (int i=0; i<bits; ++i) {
                if ((mtemp & 1) == 1) {
                    chars.Push((ftemp & 1) == 1 ? '1' : '0');
                } else {
                    chars.Push('X');
                }
                ftemp >>= 1;
                mtemp >>= 1;
            }

            StringBuilder sb = new StringBuilder((Mode == BooleanOp.Or) ? "<v" : "<&");
            while (chars.Count > 0) {
                sb.Append(chars.Pop());
            }
            sb.Append(">");
            return sb.ToString();
        }*/
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

        public bool Evaluate(FlagSet currentFlags)
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

        public override string ToString()
        {
            string oper = (mode == BooleanOp.And) ? " & " : " | ";
            StringBuilder sb = new StringBuilder("(");
            bool first = true;
            foreach (var test in tests) {
                if (!first) {
                    sb.Append(oper);
                    first = false;
                }
                sb.Append(test.ToString());
            }
            return sb.ToString();
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

        private static CompoundTest BinaryBoolOperator(CompoundTest a, IFlagTest b, BooleanOp op)
        {
            IFlagTest[] tests;
            if (a.mode == op && b is CompoundTest bc) {
                tests = new IFlagTest[bc.tests.Length + 1];
                a.tests.CopyTo(tests, 0);
                tests[tests.Length - 1] = b;
            } else {
                tests = new IFlagTest[] { a, b };
            }
            return new CompoundTest(tests, op);
        }

        public static CompoundTest operator|(CompoundTest a, IFlagTest b)
        {
            return BinaryBoolOperator(a, b, BooleanOp.Or);
        }

        public static CompoundTest operator&(CompoundTest a, IFlagTest b)
        {
            return BinaryBoolOperator(a, b, BooleanOp.And);
        }
    }

    struct FlagSet {
        public UInt64 left;
        public UInt64 right;

        public static readonly FlagSet Zeros = new FlagSet(0, 0);
        public static readonly FlagSet Ones = ~Zeros;

        public FlagSet(UInt64 left, UInt64 right)
        {
            this.left = left;
            this.right = right;
        }

        public static FlagSet operator |(FlagSet a, FlagSet b)
        {
            return new FlagSet(a.left | b.left, a.right | b.right);
        }

        public static FlagSet operator &(FlagSet a, FlagSet b)
        {
            return new FlagSet(a.left & b.left, a.right & b.right);
        }

        public static FlagSet operator ^(FlagSet a, FlagSet b)
        {
            return new FlagSet(a.left ^ b.left, a.right ^ b.right);
        }

        public static FlagSet operator ~(FlagSet a)
        {
            return new FlagSet(~a.left, ~a.right);
        }

        public static FlagSet operator <<(FlagSet a, int b)
        {
            UInt64 left = (a.left << b) | (a.right >> (64 - b));
            UInt64 right = a.right << b;
            return new FlagSet(left, right);
        }

        public static FlagSet operator >>(FlagSet a, int b)
        {
            UInt64 left = a.left >> b;
            UInt64 right = (a.right >> b) | (a.left << (64 - b));
            return new FlagSet(left, right);
        }

        public static bool operator ==(FlagSet a, FlagSet b)
        {
            return a.left == b.left && a.right == b.right;
        }

        public static bool operator !=(FlagSet a, FlagSet b)
        {
            return a.left != b.left || a.right != b.right;
        }

        public override bool Equals(object obj)
        {
            try {
                return this == (FlagSet)obj;
            } catch (InvalidCastException) {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return left.GetHashCode() ^ right.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("[{0:lX} : {1:lX}]", left, right);
        }
    }
}

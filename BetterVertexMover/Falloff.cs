using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.BetterVertexMover
{
    abstract class FalloffFunction
    {
        public FalloffFunction(float radius)
        {
            Radius = radius;
        }

        public float Radius { get; set; }
        protected abstract float R1ValueAt(float distance);

        public float ValueAt(float distance)
        {
            if (Radius == 0.0f)
            {
                return distance == 0.0f ? 1.0f : 0.0f;
            } else
            {
                return R1ValueAt(distance / Radius);
            }
        }
    }

    namespace Functions
    {
        class Smooth : FalloffFunction {
            public Smooth(float radius) : base(radius) { }
            protected override float R1ValueAt(float distance) => (float)Math.Exp(-5.0 * distance * distance);
        }

        class Dome : FalloffFunction {
            public Dome(float radius) : base(radius) { }
            protected override float R1ValueAt(float distance) => (float)Math.Sqrt(1.0 - distance * distance);
        }

        class Sharp : FalloffFunction {
            public Sharp(float radius) : base(radius) { }
            protected override float R1ValueAt(float distance) => (float)Math.Exp(-5.0 * distance);
        }

        class Linear : FalloffFunction {
            public Linear(float radius) : base(radius) { }
            protected override float R1ValueAt(float distance) => 1.0f - distance;
        }

        class Constant : FalloffFunction {
            public Constant(float radius) : base(radius) { }
            protected override float R1ValueAt(float distance) => 1.0f;
        }
    }
}

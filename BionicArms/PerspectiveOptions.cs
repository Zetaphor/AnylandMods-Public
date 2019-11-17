using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.DistanceTools.Perspective {
    class PerspectiveOptions {
        public bool Enabled { get; set; }
        public DistanceMode DistanceMode { get; set; }
        public float FixedDistance { get; set; }
        public bool PreferCloserRaycast { get; set; }
    }
}

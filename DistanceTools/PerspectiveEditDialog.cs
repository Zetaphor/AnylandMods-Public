using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.DistanceTools.Perspective {
    class PerspectiveEditDialog : MenuDialog {
        protected override void InitCustomDialog(object arg = null)
        {
            Main.perspectiveOpts.Enabled = true;
            BodyTellManager.Trigger("perspective start");

            Menu menu = new Menu("Perspective Edit");

            var sldDistance = new MenuSlider("Max Distance: ", 1, 100, "m");
            sldDistance.RoundValues = true;
            sldDistance.Action += SldDistance_Action;
            sldDistance.Value = Main.perspectiveOpts.FixedDistance;

            var chkPreserve = new MenuCheckbox("preserve", "Keep Same Distance");
            chkPreserve.Action += ChkPreserve_Action;
            chkPreserve.Value = (Main.perspectiveOpts.DistanceMode == DistanceMode.Preserve);
            chkPreserve.ExtraIcon = ExtraIcon.Uncollidable;

            var chkPreferRaycast = new MenuCheckbox("preferRaycast", "Up To Object");
            chkPreferRaycast.Footnote = "If Closer";
            chkPreferRaycast.Action += ChkPreferRaycast_Action;
            chkPreferRaycast.ExtraIcon = ExtraIcon.ScalesUniformly;

            menu.Add(sldDistance);
            menu.Add(chkPreserve);
            menu.Add(chkPreferRaycast);

            base.InitCustomDialog(menu);
        }

        private void SldDistance_Action(string id, Dialog dialog, float value)
        {
            Main.perspectiveOpts.FixedDistance = value;
        }

        private void ChkPreferRaycast_Action(string id, Dialog dialog, bool value)
        {
            Main.perspectiveOpts.PreferCloserRaycast = value;
        }

        private void ChkPreserve_Action(string id, Dialog dialog, bool value)
        {
            Main.perspectiveOpts.DistanceMode = value ? DistanceMode.Preserve : DistanceMode.Fixed;
        }

        private void OnDestroy()
        {
            Main.perspectiveOpts.Enabled = false;
            BodyTellManager.Trigger("perspective end");
        }
    }
}

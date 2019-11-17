using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnylandMods.DistanceTools.Perspective {
    class PerspectiveEditDialog : MenuDialog {
        private MenuCheckbox chkFixed, chkPreserve, chkMaxScale;
        protected override void InitCustomDialog(object arg = null)
        {
            Main.perspectiveOpts.Enabled = true;
            BodyTellManager.Trigger("perspective start");

            Menu menu = new Menu("Perspective Edit");

            var sldDistance = new MenuSlider("Max Distance: ", 1, 100, "m");
            sldDistance.RoundValues = true;
            sldDistance.Action += SldDistance_Action;
            sldDistance.Value = Main.perspectiveOpts.FixedDistance;

            chkFixed = new MenuCheckbox("fixed", "Set Max Distance");
            chkFixed.Footnote = "^";
            chkFixed.Action += ChkFixed_Action;
            chkFixed.Value = (Main.perspectiveOpts.DistanceMode == DistanceMode.Fixed);

            chkPreserve = new MenuCheckbox("preserve", "Keep Same Distance");
            chkPreserve.Action += ChkPreserve_Action;
            chkPreserve.Value = (Main.perspectiveOpts.DistanceMode == DistanceMode.Preserve);

            chkMaxScale = new MenuCheckbox("maxScale", "Max 250x Scale");
            chkMaxScale.Action += ChkMaxScale_Action;
            chkMaxScale.Value = (Main.perspectiveOpts.DistanceMode == DistanceMode.MaxScale);

            chkFixed.TextColor = chkPreserve.TextColor = chkMaxScale.TextColor = TextColor.Green;

            var chkPreferRaycast = new MenuCheckbox("preferRaycast", "Up To Object");
            chkPreferRaycast.Footnote = "If Closer";
            chkPreferRaycast.Action += ChkPreferRaycast_Action;

            menu.Add(sldDistance);
            menu.Add(chkFixed);
            menu.Add(chkPreserve);
            menu.Add(chkMaxScale);
            menu.Add(chkPreferRaycast);

            base.InitCustomDialog(menu);
        }

        private void SldDistance_Action(string id, Dialog dialog, float value)
        {
            Main.perspectiveOpts.FixedDistance = value;
        }

        private MenuCheckbox GetDistanceModeCheckbox(DistanceMode mode)
        {
            switch (mode) {
                case DistanceMode.Fixed: return chkFixed;
                case DistanceMode.Preserve: return chkPreserve;
                case DistanceMode.MaxScale: return chkMaxScale;
                default:
                    throw new ArgumentOutOfRangeException("mode", "Unknown distance mode \"" + mode.ToString() + "\"");
            }
        }

        private void HandleDistanceModeCheckbox(DistanceMode mode, bool value)
        {
            MenuCheckbox oldCheckbox = GetDistanceModeCheckbox(Main.perspectiveOpts.DistanceMode);
            MenuCheckbox thisCheckbox = GetDistanceModeCheckbox(mode);

            if (mode == Main.perspectiveOpts.DistanceMode) {
                thisCheckbox.GameObject.GetComponent<DialogPart>().Press(thisCheckbox.GameObject.GetComponent<UnityEngine.Collider>());
            } else if (value) {
                oldCheckbox.GameObject.GetComponent<DialogPart>().Press(oldCheckbox.GameObject.GetComponent<UnityEngine.Collider>());
                Main.perspectiveOpts.DistanceMode = mode;
            }
        }

        private void ChkPreferRaycast_Action(string id, Dialog dialog, bool value)
        {
            Main.perspectiveOpts.PreferCloserRaycast = value;
        }

        private void ChkMaxScale_Action(string id, Dialog dialog, bool value)
        {
            HandleDistanceModeCheckbox(DistanceMode.MaxScale, value);
        }

        private void ChkPreserve_Action(string id, Dialog dialog, bool value)
        {
            HandleDistanceModeCheckbox(DistanceMode.Preserve, value);
        }

        private void ChkFixed_Action(string id, Dialog dialog, bool value)
        {
            HandleDistanceModeCheckbox(DistanceMode.Fixed, value);
        }

        private void OnDestroy()
        {
            Main.perspectiveOpts.Enabled = false;
            BodyTellManager.Trigger("perspective end");
        }
    }
}

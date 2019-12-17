using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    class SelectEmittableThingDialog : SelectBodyPartDialog {
        protected override bool ShouldShowDetachButton() => false;

        protected override SavedAttachmentList GetSavedAttachmentList() => Main.config.Emittables;

        protected override void FinalizeMenu(Menu menu)
        {
            menu.Title = "Emittables";
        }

        protected override void InitCustomDialog(object arg = null)
        {
            base.InitCustomDialog(new Argument(AttachmentPointId.None));
        }

        protected override void SelectItem(string thingName)
        {
            SyncTools.SpawnThing(Main.config.Emittables[thingName].thingId, transform.position, transform.rotation);
        }

        protected override bool DoSave()
        {
            if (Util.LastContextLaseredThing != null) {
                Main.config.Emittables[Util.LastContextLaseredThing.givenName.ToLower()] = new AttachmentData(Util.LastContextLaseredThing.thingId, Vector3.zero, Vector3.zero);
                Main.config.Save();
                return true;
            } else {
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.PersonalizedUI {
    class SetFundamentTIDDialog : CustomDialog {
        public struct Params {
            public bool isValid;
            public string thingID;
            public Params(bool isValid, string thingID)
            {
                this.isValid = isValid;
                this.thingID = thingID;
            }
        }

        private Params par;

        protected override void InitCustomDialog(object arg)
        {
            par = (Params)arg;
        }

        public void Start()
        {
            Init(gameObject);
            if (par.isValid) {
                if (!Main.config.HideFundament) {
                    AddFundament();
                }
                Managers.thingManager.InstantiateThingOnDialogViaCache(
                    ThingRequestContext.LocalTest,
                    thingId: par.thingID,
                    fundament: transform,
                    position: Vector3.zero,
                    scale: 1.0f,
                    useDefaultRotation: true,
                    isGift: Main.config.Dynamic
                );
                AddLabel("Is this good?", textSizeFactor: 2.5f, align: TextAlignment.Center, anchor: TextAnchor.MiddleCenter);
                AddButton("back", null, "No", "ButtonMainDialog", -235, 300, textSizeFactor: 1.5f);
                AddButton("confirm", null, "Yes", "ButtonMainDialog", 235, 300, textSizeFactor: 1.5f);
            } else {
                AddFundament();
                AddLabel("Copy a thing ID, and then click that button again.", maxLineLength: 24, textSizeFactor: 1.5f, align: TextAlignment.Center, anchor: TextAnchor.MiddleCenter);
                AddButton("back", null, "OK", "ButtonMainDialog", 0, 300, textSizeFactor: 1.5f);
            }
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            base.OnClick(contextName, contextId, state, thisButton);
            switch (contextName) {
                case "confirm":
                    Main.config.FundamentTID = par.thingID;
                    Main.config.Save();
                    Managers.soundManager.Play("success", transform, 0.2f);
                    MenuDialog.SwitchTo(UIMenu.Menu, hand, tabName);
                    break;
                case "back":
                    MenuDialog.SwitchTo(ModMenu.Menu, hand, tabName);
                    break;
            }
        }
    }
}

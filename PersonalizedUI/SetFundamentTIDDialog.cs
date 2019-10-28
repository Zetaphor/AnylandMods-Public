using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.PersonalizedUI {
    class SetFundamentTIDDialog : CustomDialog {
        public enum Mode {
            PreviewNewFundament,
            ThingIDNotInClipboard,
            ConfirmCollision,
        }
        public struct Params {
            public Mode mode;
            public string thingID;
            public Params(Mode mode, string thingID)
            {
                this.mode = mode;
                this.thingID = thingID;
            }
        }

        private Params par;

        protected override void InitCustomDialog(object arg)
        {
            par = (Params)arg;
        }

        private GameObject AddCollisionTestButton(int xOnFundament = 0, int yOnFundament = 0, float scaleX = 1.0f, float scaleY = 1.0f)
        {
            GameObject button = AddModelButton("MaterialTabs/material", "", null, xOnFundament, yOnFundament);
            button.transform.localScale = new Vector3(scaleX, 1.0f, scaleY);
            return button;
        }

        public void Start()
        {
            Init(gameObject);
            switch (par.mode) {
                case Mode.PreviewNewFundament:
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
                    break;
                case Mode.ThingIDNotInClipboard:
                    AddFundament();
                    AddLabel("Copy a thing ID, and then click that button again.", maxLineLength: 24, textSizeFactor: 1.5f, align: TextAlignment.Center, anchor: TextAnchor.MiddleCenter);
                    AddButton("back", null, "OK", "ButtonMainDialog", 0, 300, textSizeFactor: 1.5f);
                    break;
                case Mode.ConfirmCollision:
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
                        isGift: true
                    );
                    AddCollisionTestButton(0, -300, 17.5f, 6.0f);
                    AddCollisionTestButton(0, 300, 17.5f, 6.0f);
                    AddButton("back", null, "Cancel", "ButtonBigCreateThing", 0, -600, textSizeFactor: 1.5f);
                    AddButton("confirmCollision", null, "Confirm", "ButtonBigCreateThing", 0, 0, textSizeFactor: 1.5f, textColor: TextColor.Red);
                    break;
            }
        }

        public override void OnClick(string contextName, string contextId, bool state, GameObject thisButton)
        {
            base.OnClick(contextName, contextId, state, thisButton);
            bool exitDialog = false;
            switch (contextName) {
                case "confirm":
                    Main.config.FundamentTID = par.thingID;
                    Main.config.Save();
                    Managers.soundManager.Play("success", transform, 0.2f);
                    exitDialog = true;
                    break;
                case "confirmCollision":
                    Main.config.Dynamic = true;
                    Main.config.Save();
                    Managers.soundManager.Play("success", transform, 0.2f);
                    exitDialog = true;
                    break;
                case "back":
                    exitDialog = true;
                    break;
            }
            if (exitDialog) {
                MenuDialog.SwitchTo(UIMenu.Menu, hand, tabName);
            }
        }
    }
}

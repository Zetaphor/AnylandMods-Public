using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace AnylandMods.Improvements.ColorPicker {
    [HarmonyPatch(typeof(MaterialDialog), nameof(MaterialDialog.UpdatePropertyInterface))]
    public static class AddRGBSliders {
        private static GameObject btnRed, btnGreen, btnBlue;
        internal static int selRGBIndex = 0;

        public static bool Prefix(MaterialDialog __instance)
        {
            bool colorExpanderShows = (bool)typeof(MaterialDialog).GetField("colorExpanderShows", Publication.StaticNonPub).GetValue(null);
            if (CreationHelper.currentMaterialTab == MaterialTab.material && colorExpanderShows) {
                int xOnFundament = -590 * ((__instance.side() == Side.Right) ? -1 : 1);
                __instance.verticalSide().SetActive(true);
                if (!__instance.currentAddedPropertyButtonsListSignature().Equals("RGBSliders")) {
                    __instance.currentAddedPropertyButtonsListSignature("RGBSliders");
                    __instance.DeleteTexturePropertyButtons();
                    __instance.DeleteParticleSystemPropertyButtons();
                    __instance.UpdateSlider("MaterialDialogProperty_texture/Param");

                    btnRed = __instance.AddButton("materialProperty", "R", "R", "ButtonSmall", xOnFundament, -240, buttonColor: "240,0,0", textColor: TextColor.Red);
                    __instance.StyleAsPropertyButton(btnRed);
                    btnGreen = __instance.AddButton("materialProperty", "G", "G", "ButtonSmall", xOnFundament, 0, buttonColor: "0,224,0", textColor: TextColor.Green);
                    __instance.StyleAsPropertyButton(btnGreen);
                    btnBlue = __instance.AddButton("materialProperty", "B", "B", "ButtonSmall", xOnFundament, 240, buttonColor: "0,0,255", textColor: TextColor.Blue);
                    __instance.StyleAsPropertyButton(btnBlue);
                }
                return false;
            } else {
                return true;
            }
        }

        public static void Postfix(MaterialDialog __instance)
        {
            if (__instance.currentAddedPropertyButtonsListSignature().Length == 0) {
                foreach (GameObject gobj in new GameObject[] { btnRed, btnGreen, btnBlue }) {
                    try {
                        UnityEngine.Object.Destroy(gobj);
                    } catch (NullReferenceException) { }
                }
                btnRed = btnGreen = btnBlue = null;
            }
        }
    }

    [HarmonyPatch(typeof(MaterialDialog), "UpdateColorExpander")]
    public static class UpdatePropertyInterfaceWithColorExpander {
        public static void Postfix(MaterialDialog __instance)
        {
            if (CreationHelper.currentMaterialTab == MaterialTab.material)
                __instance.UpdatePropertyInterface();
        }
    }

    [HarmonyPatch(typeof(MaterialDialog), nameof(MaterialDialog.OnClick))]
    public static class HandleRGBPropertyClick {
        public static void Postfix(MaterialDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("materialProperty")) {
                int oldRGBIndex = AddRGBSliders.selRGBIndex;
                switch (contextId) {
                    case "R":
                        AddRGBSliders.selRGBIndex = 0;
                        break;
                    case "G":
                        AddRGBSliders.selRGBIndex = 1;
                        break;
                    case "B":
                        AddRGBSliders.selRGBIndex = 2;
                        break;
                }
                if (AddRGBSliders.selRGBIndex != oldRGBIndex) {
                    __instance.UpdatePropertyInterface();
                } else {
                    __instance.SetCheckboxState(thisButton, true);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MaterialDialog), "UpdatePropertyDotPosition")]
    public static class UpdateRGBPropertyDotPosition {
        public static bool Prefix(MaterialDialog __instance)
        {
            if (CreationHelper.currentMaterialTab == MaterialTab.material) {
                Color color = CreationHelper.currentColor[MaterialTab.material];
                float num = 0.0f;
                switch (AddRGBSliders.selRGBIndex) {
                    case 0:
                        num = color.r;
                        break;
                    case 1:
                        num = color.g;
                        break;
                    case 2:
                        num = color.b;
                        break;
                }
                GameObject propertyDot = __instance.propertyDot();
                Vector3 localPosition = propertyDot.transform.localPosition;
                localPosition.z = -0.07f + 0.07f * num * 2f;
                propertyDot.transform.localPosition = localPosition;
                return false;
            } else {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(MaterialDialog), "HandlePropertySliding")]
    public static class HandleRGBPropertySliding {
        public static void Prefix(MaterialDialog __instance)
        {
            if (__instance.controller.GetPressDown(CrossDevice.button_grab) || __instance.controller.GetPressDown(CrossDevice.button_grabTip)) {
                Color color = CreationHelper.currentColor[MaterialTab.material];
                float num = 0.0f;
                switch (AddRGBSliders.selRGBIndex) {
                    case 0:
                        num = color.r;
                        break;
                    case 1:
                        num = color.g;
                        break;
                    case 2:
                        num = color.b;
                        break;
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {
                yield return inst;
                if (inst.opcode == OpCodes.Callvirt && inst.operand is MethodInfo && ((MethodInfo)inst.operand).Name.Equals("TriggerHapticPulse")) {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HandleRGBPropertySliding), "RGBChangeHook"));
                }
            }
        }

        private static void RGBChangeHook(float value)
        {
            if (CreationHelper.currentMaterialTab == MaterialTab.material) {
                Color color = CreationHelper.currentColor[MaterialTab.material];
                switch (AddRGBSliders.selRGBIndex) {
                    case 0:
                        color.r = value;
                        break;
                    case 1:
                        color.g = value;
                        break;
                    case 2:
                        color.b = value;
                        break;
                }
                DebugLog.LogTemp("setting color to {0} (value = {1})", color, value);
                CreationHelper.currentColor[MaterialTab.material] = color;
            }
        }
    }
}

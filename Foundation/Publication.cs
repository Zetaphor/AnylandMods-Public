using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace AnylandMods
{
    public static class Publication
    {
        public const BindingFlags InstanceNonPub = BindingFlags.Instance | BindingFlags.NonPublic;
        public const BindingFlags StaticNonPub = BindingFlags.Static | BindingFlags.NonPublic;

        static Publication()
        {
            VertexMoverInit();
            HandInit();
        }

        // Dialog

        public static GameObject AddCheckbox(this Dialog dialog, string contextName = null, string contextId = null, string text = null, int xOnFundament = 0, int yOnFundament = 0, bool state = false, float textSizeFactor = 1f, string prefabName = "Checkbox", TextColor textColor = TextColor.Default, string footnote = null, ExtraIcon extraIcon = ExtraIcon.None)
        {
            object[] args = new object[] { contextName, contextId, text, xOnFundament, yOnFundament, state, textSizeFactor, prefabName, textColor, footnote, extraIcon };
            return (GameObject)typeof(Dialog).GetMethod("AddCheckbox", InstanceNonPub).Invoke(dialog, args);
        }

        public static void SetFundamentColor(this Dialog dialog, Color color)
        {
            typeof(Dialog).GetMethod("SetFundamentColor", InstanceNonPub).Invoke(dialog, new object[] { color });
        }

        public static GameObject SwitchTo(this Dialog dialog, DialogType dialogType, string tabName = "")
        {
            return (GameObject)typeof(Dialog).GetMethod("SwitchTo", InstanceNonPub).Invoke(dialog, new object[] { dialogType, tabName });
        }

        public static DialogSlider AddSlider(this Dialog dialog, string valuePrefix = "", string valueSuffix = "", int x = 0, int y = 0, float minValue = 0f, float maxValue = 100f, bool roundValues = false, float value = 0f, Action<float> onValueChange = null, bool showValue = true, float textSizeFactor = 1f)
        {
            object[] args = new object[] { valuePrefix, valueSuffix, x, y, minValue, maxValue, roundValues, value, onValueChange, showValue, textSizeFactor };
            return (DialogSlider)typeof(Dialog).GetMethod("AddSlider", InstanceNonPub).Invoke(dialog, args);
        }

        public static Hand hand(this Dialog dialog)
        {
            return (Hand)typeof(Dialog).GetField("hand", InstanceNonPub).GetValue(dialog);
        }

        public static void SetButtonColor(this Dialog dialog, GameObject button, Color color)
        {
            typeof(Dialog).GetMethod("SetButtonColor", InstanceNonPub).Invoke(dialog, new object[] { button, color });
        }

        public static GameObject GetUiWrapper(this Dialog dialog)
        {
            return (GameObject)typeof(Dialog).GetMethod("GetUiWrapper", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static void SetUiWrapper(this Dialog dialog, GameObject wrapper)
        {
            typeof(Dialog).GetMethod("SetUiWrapper", InstanceNonPub).Invoke(dialog, new object[] { wrapper });
        }

        public static GameObject wrapper(this Dialog dialog)
        {
            return (GameObject)typeof(Dialog).GetField("wrapper", InstanceNonPub).GetValue(dialog);
        }

        public static void wrapper(this Dialog dialog, GameObject wrapper)
        {
            typeof(Dialog).GetField("wrapper", InstanceNonPub).SetValue(dialog, wrapper);
        }

        public static void SetCheckboxState(this Dialog dialog, GameObject part, bool state, bool ignoreIcon = true)
        {
            typeof(Dialog).GetMethod("SetCheckboxState", InstanceNonPub).Invoke(dialog, new object[] { part, state, ignoreIcon });
        }

        // DialogManager
        
        public static Hand GetDialogHand(this DialogManager manager)
        {
            return (Hand)typeof(DialogManager).GetMethod("GetDialogHand", InstanceNonPub).Invoke(manager, new object[] { });
        }

        // VertexMover(Dialog)

        private static FieldInfo VertexMover_grabbedVertexIndex;
        private static FieldInfo VertexMover_thingPart;
        private static MethodInfo VertexMover_UpdateSelectedVerticesIndicatorPositions;
        private static MethodInfo VertexMover_UpdateSelectedVertexIndicatorPositions;

        private static void VertexMoverInit()
        {
            VertexMover_grabbedVertexIndex = typeof(VertexMover).GetField("grabbedVertexIndex", InstanceNonPub);
            VertexMover_thingPart = typeof(VertexMover).GetField("thingPart", InstanceNonPub);
            VertexMover_UpdateSelectedVerticesIndicatorPositions = typeof(VertexMover).GetMethod("UpdateSelectedVerticesIndicatorPositions", InstanceNonPub);
            VertexMover_UpdateSelectedVertexIndicatorPositions = typeof(VertexMover).GetMethod("UpdateSelectedVertexIndicatorPositions", InstanceNonPub);
        }

        public static int grabbedVertexIndex(this VertexMover vertexMover)
        {
            return (int)VertexMover_grabbedVertexIndex.GetValue(vertexMover);   
        }

        public static ThingPart thingPart(this VertexMover vertexMover)
        {
            object result = VertexMover_thingPart.GetValue(vertexMover);
            return result is null ? null : (ThingPart)result;
        }

        public static void UpdateSelectedVerticesIndicatorPositions(this VertexMover vertexMover, Vector3[] vertices, Vector3 offset)
        {
            VertexMover_UpdateSelectedVerticesIndicatorPositions.Invoke(vertexMover, new object[] { vertices, offset });
        }

        public static void UpdateSelectedVertexIndicatorPositions(this VertexMover vertexMover)
        {
            VertexMover_UpdateSelectedVertexIndicatorPositions.Invoke(vertexMover, new object[] { });
        }

        public static VertexMover vertexMover(this VertexMoverDialog dialog)
        {
            return (VertexMover)typeof(VertexMoverDialog).GetField("vertexMover", InstanceNonPub).GetValue(dialog);
        }

        // BodyMotionsDialog

        public static List<ThingPart> GetMyThingParts(this BodyMotionsDialog dialog)
        {
            return (List<ThingPart>)typeof(BodyMotionsDialog).GetMethod("GetMyThingParts", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static List<string> GetTellBodyDataBodyIsListeningFor(this BodyMotionsDialog dialog, List<ThingPart> thingParts)
        {
            return (List<string>)typeof(BodyMotionsDialog).GetMethod("GetTellBodyDataBodyIsListeningFor", InstanceNonPub).Invoke(dialog, new object[] { thingParts });
        }

        // ThingPartDialog

        public static ThingPart thingPart(this ThingPartDialog dialog)
        {
            return (ThingPart)typeof(ThingPartDialog).GetField("thingPart", InstanceNonPub).GetValue(dialog);
        }

        public static bool showSubThings(this ThingPartDialog dialog)
        {
            return (bool)typeof(ThingPartDialog).GetField("showSubThings", InstanceNonPub).GetValue(dialog);
        }

        // MaterialDialog

        public static void AddPropertyButtonsIfNeeded(this MaterialDialog dialog)
        {
            typeof(MaterialDialog).GetMethod("AddPropertyButtonsIfNeeded", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static GameObject verticalSide(this MaterialDialog dialog)
        {
            return (GameObject)typeof(MaterialDialog).GetField("verticalSide", InstanceNonPub).GetValue(dialog);
        }

        public static Side side(this MaterialDialog dialog)
        {
            return (Side)typeof(MaterialDialog).GetField("side", InstanceNonPub).GetValue(dialog);
        }

        public static string currentAddedPropertyButtonsListSignature(this MaterialDialog dialog)
        {
            return (string)typeof(MaterialDialog).GetField("currentAddedPropertyButtonsListSignature", InstanceNonPub).GetValue(dialog);
        }

        public static void currentAddedPropertyButtonsListSignature(this MaterialDialog dialog, string signature)
        {
            typeof(MaterialDialog).GetField("currentAddedPropertyButtonsListSignature", InstanceNonPub).SetValue(dialog, signature);
        }

        public static void DeleteTexturePropertyButtons(this MaterialDialog dialog)
        {
            typeof(MaterialDialog).GetMethod("DeleteTexturePropertyButtons", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static void DeleteParticleSystemPropertyButtons(this MaterialDialog dialog)
        {
            typeof(MaterialDialog).GetMethod("DeleteParticleSystemPropertyButtons", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static void StyleAsPropertyButton(this MaterialDialog dialog, GameObject thisButton)
        {
            typeof(MaterialDialog).GetMethod("StyleAsPropertyButton", InstanceNonPub).Invoke(dialog, new object[] { thisButton });
        }

        public static void AddPropertySideInterface(this MaterialDialog dialog)
        {
            typeof(MaterialDialog).GetMethod("AddPropertySideInterface", InstanceNonPub).Invoke(dialog, new object[] { });
        }

        public static void UpdateSlider(this MaterialDialog dialog, string path)
        {
            typeof(MaterialDialog).GetMethod("UpdateSlider", InstanceNonPub).Invoke(dialog, new object[] { path });
        }

        public static GameObject propertyDot(this MaterialDialog dialog)
        {
            return (GameObject)typeof(MaterialDialog).GetField("propertyDot", InstanceNonPub).GetValue(dialog);
        }

        public static void propertyValueWhenEmptyClickStarted(this MaterialDialog dialog, float value)
        {
            typeof(MaterialDialog).GetField("propertyValueWhenEmptyClickStarted", InstanceNonPub).SetValue(dialog, value);
        }

        // ProfileDialog

        public static Person personThisIsOf(this ProfileDialog dialog)
        {
            return (Person)typeof(ProfileDialog).GetField("personThisIsOf", InstanceNonPub).GetValue(dialog);
        }

        // Hand

        private static FieldInfo previousPosField;
        private static FieldInfo handDotNormalPositionField;
        private static FieldInfo handObjectsWhilePuppeteeringField;

        private static void HandInit()
        {
            previousPosField = typeof(Hand).GetField("previousPosition", InstanceNonPub);
            handDotNormalPositionField = typeof(Hand).GetField("handDotNormalPosition", InstanceNonPub);
            handObjectsWhilePuppeteeringField = typeof(Hand).GetField("handObjectsWhilePuppeteering", InstanceNonPub);
        }

        public static Vector3 previousPosition(this Hand hand)
        {
            return (Vector3)previousPosField.GetValue(hand);
        }

        public static void previousPosition(this Hand hand, Vector3 newPos)
        {
            previousPosField.SetValue(hand, newPos);
        }

        public static Vector3 handDotNormalPosition(this Hand hand)
        {
            return (Vector3)handDotNormalPositionField.GetValue(hand);
        }

        public static GameObject handObjectsWhilePuppeteering(this Hand hand)
        {
            return (GameObject)handObjectsWhilePuppeteeringField.GetValue(hand);
        }

        // HandDot

        public static void StorePickUpPosition(this HandDot handDot, GameObject gameObject)
        {
            DebugLog.Log("{0}.{1}({2})", handDot, typeof(HandDot).GetMethod("StorePickUpPosition", InstanceNonPub).Name, gameObject);
            typeof(HandDot).GetMethod("StorePickUpPosition", InstanceNonPub).Invoke(handDot, new object[] { gameObject });
        }
    }
}

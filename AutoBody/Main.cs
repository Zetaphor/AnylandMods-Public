using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityModManagerNet;
using Harmony;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace AnylandMods.AutoBody
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static ConfigFile config;
        private static Regex regex, regexForIn, regexForSave;
        internal static Menu pointMenu;
        internal static HarmonyInstance harmony;
        internal static bool holdAttachments;
        internal static Dictionary<AttachmentPointId, string[]> savedSlots;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            config = new ConfigFile(mod);
            config.Load();

            savedSlots = new Dictionary<AttachmentPointId, string[]>();

            ModMenu.AddButton(harmony, "Saved Body Parts...", SavedBodyParts_Action);

            pointMenu = new Menu("Saved Body Parts");
            pointMenu.SetBackButton(DialogType.OwnProfile);
            pointMenu.TwoColumns = true;
            pointMenu.DialogClose += PointMenu_DialogClose;

            MenuButton mbtn;
            mbtn = new MenuButton("HeadTop", "(XA0) Hat");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("Head", "(XA1) Head");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("ArmLeft", "(XA2) Left Arm");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("TorsoUpper", "(XA3) Upper Torso");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("ArmRight", "(XA4) Right Arm");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("TorsoLower", "(XA5) Lower Torso");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("LegLeft", "(XA6,8) Left Leg");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("LegRight", "(XA7,9) Right Leg");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("HandLeft", "(XAA) Left Hand*");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("HandRight", "(XAB) Right Hand*");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("Emits", "Emittables");
            mbtn.Action += Emits_Action;
            pointMenu.Add(mbtn);

            regex = new Regex("^xa([0-9ab]) ?(.*)$");
            regexForIn = new Regex(" in ([0-9]*\\.?[0-9]*)s? ?(?:via (.*))?$");
            regexForSave = new Regex("^save ?([0-9]) ?");
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;
            return true;
        }

        private static void Emits_Action(string id, Dialog dialog)
        {
            CustomDialog.SwitchTo<SelectEmittableThingDialog>(dialog.hand(), dialog.tabName);
        }

        public static void OpenSavedBodyParts()
        {
            Main.config.Load();
            MenuDialog.SwitchTo(Main.pointMenu);
        }

        public static void SavedBodyParts_Action(string id, Dialog dialog)
        {
            OpenSavedBodyParts();
        }

        private static void PointMenu_DialogClose(MenuDialog obj)
        {
            Our.SetPreviousMode();
        }

        private static void Mbtn_Action(string id, Dialog dialog)
        {
            var apid = (AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), id);
            CustomDialog.SwitchTo<SelectBodyPartDialog>(new SelectBodyPartDialog.Argument(apid), dialog.hand(), dialog.tabName);
        }

        private class StartCoroutineAttach : MonoBehaviour {
            public void Start() { }
            public void Update() { }

            public System.Collections.IEnumerator MyCoroutine(AttachmentPoint point, AttachmentData data)
            {
                GameObject gobj = null;
                yield return StartCoroutine(Managers.thingManager.InstantiateThingViaCache(ThingRequestContext.PersonClassAttachNewThing, data.thingId, delegate (GameObject go) {
                    gobj = go;
                }));
                if (gobj != null) {
                    gobj.transform.parent = point.transform;
                    gobj.transform.localPosition = data.position;
                    gobj.transform.localEulerAngles = data.rotation;
                    Managers.personManager.DoAttachThing(point.gameObject, gobj);
                }
            }

            public void Attach(AttachmentPoint point, AttachmentData data)
            {
                StartCoroutine(MyCoroutine(point, data));
            }
        }

        internal static void SetAttachment(AttachmentPointId point, string thingName, bool moveLeg = true)
        {
            Person ourPerson = Managers.personManager.ourPerson;
            GameObject attpoint = ourPerson.GetAttachmentPointById(point);
            SavedAttachmentList list = config.GetListForAttachmentPoint(point);

            if (point == AttachmentPointId.HandLeft) {
                HandPseudoAttachmentControl.thingNameLeft = thingName;
            } else if (point == AttachmentPointId.HandRight) {
                HandPseudoAttachmentControl.thingNameRight = thingName;
            }

            if (thingName.ToLower().StartsWith("load") && (thingName.Length == 5 || (thingName.Length == 6 && thingName[4] == ' '))) {
                if (int.TryParse(thingName[thingName.Length - 1].ToString(), out int slot)) {
                    if (savedSlots.TryGetValue(point, out string[] array)) {
                        thingName = array[slot];
                    }
                }
            }

            if (thingName.Length == 0 || thingName.Equals("-")) {
                Managers.personManager.DoRemoveAttachedThing(attpoint);
            } else if (list.ContainsName(thingName)) {
                var comp = attpoint.GetComponent<StartCoroutineAttach>();
                if (comp == null) {
                    comp = attpoint.AddComponent<StartCoroutineAttach>();
                }
                comp.Attach(attpoint.GetComponent<AttachmentPoint>(), list[thingName]);

                if (moveLeg) {
                    try {
                        if (point == AttachmentPointId.LegLeft) {
                            ourPerson.AttachmentPointLegLeft.transform.localPosition = config.LegPosLeft[thingName];
                            ourPerson.AttachmentPointLegLeft.transform.localEulerAngles = config.LegRotLeft[thingName];
                            Managers.personManager.SaveOurLegAttachmentPointPositions();
                        } else if (point == AttachmentPointId.LegRight) {
                            ourPerson.AttachmentPointLegRight.transform.localPosition = config.LegPosRight[thingName];
                            ourPerson.AttachmentPointLegRight.transform.localEulerAngles = config.LegRotRight[thingName];
                            Managers.personManager.SaveOurLegAttachmentPointPositions();
                        }
                    } catch (KeyNotFoundException) {
                        DebugLog.Log("warning: no leg pos/rot saved for {0}:{1}", point, thingName);
                    }
                }
            } else {
                DebugLog.Log("\"{0}\" is not a known attachment for {1}.", thingName, point);
                if (point == AttachmentPointId.HandLeft) {
                    HandPseudoAttachmentControl.thingNameLeft = "";
                } else if (point == AttachmentPointId.HandRight) {
                    HandPseudoAttachmentControl.thingNameRight = "";
                }
            }
        }

        private static void BodyTellManager_ToldByBody(string data, BodyTellManager.TellEventInfo info)
        {
            // TODO: Add a toggle to disable IsTrusted check
            if (!config.EnableTellControl || !info.IsTrusted)
                return;

            Match match = regex.Match(data);
            if (match.Success) {
                var points = new AttachmentPointId[] {
                    AttachmentPointId.HeadTop,
                    AttachmentPointId.Head,
                    AttachmentPointId.ArmLeft,
                    AttachmentPointId.TorsoUpper,
                    AttachmentPointId.ArmRight,
                    AttachmentPointId.TorsoLower,
                    AttachmentPointId.LegLeft,
                    AttachmentPointId.LegRight,
                    AttachmentPointId.LegLeft,
                    AttachmentPointId.LegRight,
                    AttachmentPointId.HandLeft,
                    AttachmentPointId.HandRight
                };
                char pointChar = match.Groups[1].Value[0];
                int pointNum = Char.IsDigit(pointChar) ? (pointChar - '0') : (10 + pointChar - 'a');
                AttachmentPointId point = points[pointNum];
                string thingName = match.Groups[2].Value;
                float delay = 0.0f;

                Match matchForIn = regexForIn.Match(thingName);
                if (matchForIn.Success) {
                    thingName = thingName.Substring(0, matchForIn.Index);
                    float.TryParse(matchForIn.Groups[1].Value, out delay);
                    if (matchForIn.Groups[2].Success && delay > 0) {
                        SetAttachment(point, matchForIn.Groups[2].Value);
                    }
                }

                Match matchForSave = regexForSave.Match(thingName);
                if (matchForSave.Success) {
                    DebugLog.LogTemp("matchForSave {0}", matchForSave);
                    string[] array;
                    if (!savedSlots.TryGetValue(point, out array)) {
                        array = new string[10];
                        savedSlots[point] = array;
                    }
                    thingName = thingName.Substring(matchForSave.Length);
                    array[int.Parse(matchForSave.Groups[1].Value)] = thingName;
                    return;
                }

                bool shouldMove = (pointNum == 6 || pointNum == 7);
                GameObject ap = Managers.personManager.ourPerson.GetAttachmentPointById(point);
                DebugLog.LogTemp("parent of {0} is {1}", ap, ap.transform.parent.gameObject);
                if (shouldMove && thingName.Equals("lock")) {
                    ap.transform.parent = Managers.personManager.ourPerson.transform;
                } else if (shouldMove && thingName.Equals("unlock")) {
                    ap.transform.parent = Managers.personManager.ourPerson.Torso.transform;
                } else if (delay > 0.0f) {
                    var ds = ap.GetComponent<DelayedSwitch>();
                    if (ds == null)
                        ds = ap.AddComponent<DelayedSwitch>();
                    var legPosDict = (pointNum == 6) ? config.LegPosLeft : config.LegPosRight;
                    Vector3? targetPos = null;
                    if (shouldMove && legPosDict.TryGetValue(thingName, out Vector3 tpos))
                        targetPos = tpos;
                    ds.Begin(point, thingName, delay, targetPos);
                } else {
                    SetAttachment(point, thingName, shouldMove);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Person), nameof(Person.ResetLegsPositionRotationToUniversalDefault))]
    public static class ReparentLegsOnReset1 {
        public static void Prefix(Person __instance, Side? onlyThisSide)
        {
            if (!onlyThisSide.HasValue || onlyThisSide.Value == Side.Left)
                __instance.AttachmentPointLegLeft.transform.parent = __instance.Torso.transform;
            if (!onlyThisSide.HasValue || onlyThisSide.Value == Side.Right)
                __instance.AttachmentPointLegRight.transform.parent = __instance.Torso.transform;
        }
    }

    [HarmonyPatch(typeof(Person), nameof(Person.ResetLegsPositionRotationToBodyOrUniversalDefault))]
    public static class ReparentLegsOnReset2 {
        public static void Prefix(Person __instance)
        {
            ReparentLegsOnReset1.Prefix(__instance, null);
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.Start))]
    public static class AddButtons {
        public static void Postfix(OwnProfileDialog __instance)
        {
            __instance.AddButton("savedBodyParts", null, "Saved Body Parts...", "ButtonCompact", 100, 300, textColor: TextColor.Blue, align: TextAlignment.Center);
            __instance.AddButton("holdAttachments", null, "Hold", "ButtonSmallCentered", -220, 300, null, false, textColor: TextColor.Blue);
            Main.holdAttachments = false;
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), "AddBacksideButtons")]
    public static class AddCheckboxes {
        private static void DoAdd(OwnProfileDialog instance)
        {
            instance.AddCheckbox("ignoreAddBody", null, "Attach Head Only", 0, 300, Main.config.IgnoreAddBody, textColor: TextColor.Blue);
            instance.AddCheckbox("enableTellControl", null, "Enable Tells", 0, 415, Main.config.EnableTellControl, textColor: TextColor.Blue, footnote: "\"XA# THING\"");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            bool added = false;
            foreach (CodeInstruction inst in code) {
                yield return inst;
                if (!added && inst.opcode == OpCodes.Call && inst.operand is MethodInfo && ((MethodInfo)inst.operand).Name.Equals("AddCheckbox")) {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AddCheckboxes), "DoAdd"));
                    added = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.OnClick))]
    public static class HandleButtonClicks {
        public static void Postfix(OwnProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("savedBodyParts")) {
                Main.OpenSavedBodyParts();
            } else if (contextName.Equals("holdAttachments")) {
                Main.holdAttachments = !Main.holdAttachments;
            } else if (contextName.Equals("ignoreAddBody")) {
                Main.config.IgnoreAddBody = state;
                Main.config.Save();
            } else if (contextName.Equals("enableTellControl")) {
                Main.config.EnableTellControl = state;
                Main.config.Save();
            }
        }
    }

    [HarmonyPatch(typeof(CreateDialog), "DoSave")]
    public static class UpdateIdOnThingSave {
        private static string oldId;
        private static Thing thingScript;

        private static void UpdateId(SaveThing_Response response)
        {
            if (response.error == null) {
                Main.config.UpdateThingId(oldId, thingScript.thingId, thingScript.givenName);
            } else {
                DebugLog.Log("Not calling UpdateThingId for {0} ({1}) because an error occurred while saving: {2}", oldId, thingScript.name, response.error);
            }
        }

        public static void Prefix(Thing ___thingScript)
        {
            thingScript = ___thingScript;
            oldId = thingScript.thingId;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {
                if (inst.opcode == OpCodes.Ldftn) {
                    var method = (MethodBase)inst.operand;
                    DebugLog.Log("Patching {0}", method.Name);
                    Main.harmony.Patch(method, postfix: new HarmonyMethod(typeof(UpdateIdOnThingSave), "UpdateId"));
                    break;
                }
            }
            return code;
        }
    }

    [HarmonyPatch(typeof(ThingManager), nameof(ThingManager.SetOurCurrentBodyAttachmentsByThing))]
    public static class IgnoreAddBodyIfSet {
        private static bool CheckIgnoreAddBody()
        {
            return Main.config.IgnoreAddBody;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code, ILGenerator ilgen)
        {
            foreach (CodeInstruction inst in code) {
                if (inst.opcode == OpCodes.Ldstr && inst.operand.Equals("HeadCore/HeadTopAttachmentPoint")) {
                    Label lbl = ilgen.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IgnoreAddBodyIfSet), "CheckIgnoreAddBody"));
                    yield return new CodeInstruction(OpCodes.Brfalse, lbl);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ret);
                    ilgen.MarkLabel(lbl);
                }
                yield return inst;
            }
        }
    }

    [HarmonyPatch(typeof(PhotonView), nameof(PhotonView.RPC), typeof(string), typeof(PhotonTargets), typeof(object[]))]
    public static class SyncHandPointsAsHeldItems {
        public static void Prefix(ref string methodName, ref object[] parameters)
        {
            if (!methodName.Equals("vc"))
                DebugLog.LogTemp("{0}({1})", methodName, string.Join(", ", parameters.Select(o => o.ToString()).ToArray()));
            if (methodName.Equals("DoAttachThing_Remote")) {
                var apid = (AttachmentPointId)parameters[0];
                if (apid == AttachmentPointId.HandLeft || apid == AttachmentPointId.HandRight) {
                    methodName = "DoAddToHand_Remote";
                    object[] oldParams = parameters;
                    parameters = new object[] {
                        (apid == AttachmentPointId.HandLeft) ? TopographyId.Left : TopographyId.Right,
                        oldParams[1],
                        oldParams[2],
                        oldParams[3],
                        EditModes.None,
                        EditModes.None
                    };
                }
            } else if (methodName.Equals("DoRemoveAttachedThing_LocalOrRemote")) {
                var apid = (AttachmentPointId)parameters[0];
                if (apid == AttachmentPointId.HandLeft || apid == AttachmentPointId.HandRight) {
                    methodName = "DoClearFromHand_Remote";
                    parameters = new object[] { (apid == AttachmentPointId.HandLeft) ? TopographyId.Left : TopographyId.Right };
                }
            }
        }
    }

    [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.UpdateAttachment))]
    public static class DoNotSaveHandAttachmentsToServer {
        private static System.Collections.IEnumerator EmptyCoroutine()
        {
            yield break;
        }

        public static bool Prefix(AttachmentPointId attachmentPointId, ref System.Collections.IEnumerator __result)
        {
            if (attachmentPointId == AttachmentPointId.HandLeft || attachmentPointId == AttachmentPointId.HandRight) {
                __result = EmptyCoroutine();
                return false;
            } else {
                return true;
            }
        }
    }

    public class HandPseudoAttachmentControl : MonoBehaviour {
        private static bool holdingLeft = false;
        private static bool holdingRight = false;
        internal static string thingNameLeft = "";
        internal static string thingNameRight = "";

        private void Start() { }

        public void Update()
        {
            var ap = GetComponent<AttachmentPoint>();
            var person = GetComponentInParent<Person>();
            if (ap == null || person == null)
                return;
            if (person.isOurPerson) {
                if (ap.id == AttachmentPointId.HandLeft) {
                    bool holding = person.GetHandBySide(Side.Left).GetComponentInChildren<HandDot>().currentlyHeldObject != null;
                    if (holding != holdingLeft) {
                        holdingLeft = holding;
                        if (holding && ap.attachedThing != null) {
                            GameObject.Destroy(ap.attachedThing);
                        }
                    }
                } else if (ap.id == AttachmentPointId.HandRight) {
                    bool holding = person.GetHandBySide(Side.Right).GetComponentInChildren<HandDot>().currentlyHeldObject != null;
                    if (holding != holdingRight) {
                        holdingRight = holding;
                        if (holding && ap.attachedThing != null) {
                            GameObject.Destroy(ap.attachedThing);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Person), "SetAttachmentPointReferencesById")]
    public static class AddHandPointReferences {
        public static void Postfix(Person __instance, GameObject ___AttachmentPointHandLeft, GameObject ___AttachmentPointHandRight)
        {
            __instance.AttachmentPointsById.Add(AttachmentPointId.HandLeft, ___AttachmentPointHandLeft);
            __instance.AttachmentPointsById.Add(AttachmentPointId.HandRight, ___AttachmentPointHandRight);
            ___AttachmentPointHandLeft.gameObject.AddComponent<HandPseudoAttachmentControl>();
            ___AttachmentPointHandRight.gameObject.AddComponent<HandPseudoAttachmentControl>();
        }
    }

    [HarmonyPatch(typeof(HandDot), nameof(HandDot.HandleOnTriggerStay))]
    public static class HoldAttachmentsOnPoints1 {
        internal static AttachmentPointId activePoint = AttachmentPointId.None;

        private static void CapturePointId(GameObject point, HandDot instance)
        {
            var ap = point.GetComponent<AttachmentPoint>();
            if (ap == null || CrossDevice.GetPress(instance.controller, CrossDevice.button_delete, instance.side)) {
                activePoint = AttachmentPointId.None;
            } else {
                activePoint = ap.id;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code) {
                if (inst.opcode == OpCodes.Callvirt) {
                    if (((MethodInfo)inst.operand).Name.Equals("DoRemoveAttachedThing")) {
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HoldAttachmentsOnPoints1), "CapturePointId"));
                    }
                }
                yield return inst;
            }
        }

        public static void Postfix(HandDot __instance)
        {
            if (CrossDevice.GetPress(__instance.controller, CrossDevice.button_delete, __instance.side)) {
                activePoint = AttachmentPointId.None;
            }
        }
    }

    [HarmonyPatch(typeof(HandDot), "AttachIfCollidesWithAttachmentPointSphere")]
    public static class HoldAttachmentsOnPoints2 {
        public static bool Prefix(GameObject thing)
        {
            if (Main.holdAttachments && HoldAttachmentsOnPoints1.activePoint != AttachmentPointId.None) {
                GameObject point = Managers.personManager.ourPerson.GetAttachmentPointById(HoldAttachmentsOnPoints1.activePoint);
                Managers.personManager.DoAttachThing(point, thing, false);
                return false;
            } else {
                return true;
            }
        }
    }
}

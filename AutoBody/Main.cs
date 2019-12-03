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
        private static Regex regex, regexForIn;
        internal static Menu pointMenu;
        internal static HarmonyInstance harmony;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            config = new ConfigFile(mod);
            config.Load();

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

            regex = new Regex("^xa([0-9]) ?(.*)$");
            regexForIn = new Regex(" in ([0-9]*.?[0-9]*)s?$");
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;
            return true;
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
                    } catch (KeyNotFoundException) { }
                }
            } else {
                DebugLog.Log("\"{0}\" is not a known attachment for {1}.", thingName, point);
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
                    AttachmentPointId.LegRight
                };
                int pointNum = Int32.Parse(match.Groups[1].Value);
                AttachmentPointId point = points[pointNum];
                string thingName = match.Groups[2].Value;
                float delay = 0.0f;

                Match matchForIn = regexForIn.Match(thingName);
                if (matchForIn.Success) {
                    thingName = thingName.Substring(0, matchForIn.Index);
                    float.TryParse(matchForIn.Groups[1].Value, out delay);
                }

                bool shouldMove = (pointNum == 6 || pointNum == 7);
                GameObject ap = Managers.personManager.ourPerson.GetAttachmentPointById(point);
                if (shouldMove && thingName.Equals("lock")) {
                    FixedWorldPosRot.LockPosRot(ap);
                } else if (shouldMove && thingName.Equals("unlock")) {
                    FixedWorldPosRot.UnlockPosRot(ap);
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

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.Start))]
    public static class AddButton {
        public static void Postfix(OwnProfileDialog __instance)
        {
            __instance.AddButton("savedBodyParts", null, "Saved Body Parts...", "ButtonCompact", 0, 300, textColor: TextColor.Blue, align: TextAlignment.Center);
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
}

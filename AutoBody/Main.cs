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
        private static Regex regex;
        internal static Menu pointMenu;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            config = new ConfigFile(mod);
            config.Load();

            pointMenu = new Menu("Save Body Part");
            MenuButton mbtn;
            mbtn = new MenuButton("HeadTop", "(XA0) Hat");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("Head", "(XA1) Head");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("HandLeft", "(XA2) Left Hand");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("ArmLeft", "(XA3) Left Arm");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("TorsoUpper", "(XA4) Upper Torso");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("ArmRight", "(XA5) Right Arm");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("HandRight", "(XA6) Right Hand");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("TorsoLower", "(XA7) Lower Torso");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("LegLeft", "(XA8) Left Leg");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);
            mbtn = new MenuButton("LegRight", "(XA9) Right Leg");
            mbtn.Action += Mbtn_Action;
            pointMenu.Add(mbtn);

            regex = new Regex("xa([0-9]) ?(.*)");
            BodyTellManager.ToldByBody += BodyTellManager_ToldByBody;
            return true;
        }

        private static void Mbtn_Action(string id, Dialog dialog)
        {
            Main.config.GetListForAttachmentPoint((AttachmentPointId)Enum.Parse(typeof(AttachmentPointId), id)).AddCurrent();
            Main.config.Save();
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

        private static void BodyTellManager_ToldByBody(string data, bool byScript)
        {
            // TODO: Add some kind of check to make sure this was triggered by an attachment (with a toggle to disable it)
            if (!config.EnableTellControl)
                return;

            Match match = regex.Match(data);
            if (match.Success) {
                var points = new AttachmentPointId[] {
                    AttachmentPointId.HeadTop,
                    AttachmentPointId.Head,
                    AttachmentPointId.HandLeft,
                    AttachmentPointId.ArmLeft,
                    AttachmentPointId.TorsoUpper,
                    AttachmentPointId.ArmRight,
                    AttachmentPointId.HandRight,
                    AttachmentPointId.TorsoLower,
                    AttachmentPointId.LegLeft,
                    AttachmentPointId.LegRight
                };
                AttachmentPointId point = points[Int32.Parse(match.Groups[1].Value)];
                GameObject attpoint = Managers.personManager.ourPerson.GetAttachmentPointById(point);
                SavedAttachmentList list = config.GetListForAttachmentPoint(point);
                string thingName = match.Groups[2].Value;
                if (thingName.Length == 0) {
                    Managers.personManager.DoRemoveAttachedThing(attpoint);
                } else if (list.ContainsKey(thingName)) {
                    var comp = attpoint.GetComponent<StartCoroutineAttach>();
                    if (comp == null) {
                        comp = attpoint.AddComponent<StartCoroutineAttach>();
                    }
                    comp.Attach(attpoint.GetComponent<AttachmentPoint>(), list[thingName]);
                } else {
                    DebugLog.Log("\"{0}\" is not a known attachment for {1}.", thingName, point);
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
            instance.AddCheckbox("ignoreAddBody", null, "Attach Head Only", 0, -70, Main.config.IgnoreAddBody, textColor: TextColor.Blue);
            instance.AddCheckbox("enableTellControl", null, "Enable Tells", 0, 45, Main.config.EnableTellControl, textColor: TextColor.Blue, footnote: "\"XA# THING\"");
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
                MenuDialog.SwitchTo(Main.pointMenu);
            } else if (contextName.Equals("ignoreAddBody")) {
                Main.config.IgnoreAddBody = state;
                Main.config.Save();
            } else if (contextName.Equals("enableTellControl")) {
                Main.config.EnableTellControl = state;
                Main.config.Save();
            }
        }
    }
}

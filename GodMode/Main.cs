using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using UnityModManagerNet;

namespace AnylandMods.GodMode
{
    public static class Main
    {
        public static bool enabled;
        public static bool gmEnabled = false;
        public static UnityModManager.ModEntry mod;
        internal static HarmonyInstance harmony = null;
        internal static bool hearEveryone = false;

        public static IEnumerable<CodeInstruction> ForceClonableTranspiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code)
            {
                yield return inst;
                if (inst.opcode == OpCodes.Ldfld)
                {
                    FieldInfo field = inst.operand as FieldInfo;
                    if (!(field is null))
                    {
                        if (field.Name.Equals("isClonable"))
                        {
                            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Main), "gmEnabled"));
                            yield return new CodeInstruction(OpCodes.Or);
                        }
                        else if (field.Name.Equals("isNeverClonable"))
                        {
                            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Main), "gmEnabled"));
                            yield return new CodeInstruction(OpCodes.Not);
                            yield return new CodeInstruction(OpCodes.And);
                        }
                    } else
                    {
                        DebugLog.Log("Warning: operand " + inst.operand.ToString() + " is not FieldInfo!");
                    }
                }
            }
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

            ModMenu.AddCheckbox(harmony, "Enable God Mode", GmEnableCheckbox_Action).ExtraIcon = ExtraIcon.Unlocked;
            ModMenu.AddCheckbox(harmony, "Hear Everyone", HearEveryone_Action);

            MethodInfo[] methods = new MethodInfo[]
            {
                typeof(IncludeThingDialog).GetMethod("AddMergePartsButtonIfAppropriate", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(ThingDialog).GetMethod("AddBacksideButtons", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(ThingDialog).GetMethod("AddInfo", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(ThingManager).GetMethod("ExportAllThings")
            };
            foreach (MethodInfo method in methods)
            {
                try
                {
                    harmony.Patch(method, transpiler: new HarmonyMethod(typeof(Main), "ForceClonableTranspiler"));
                } catch (TargetInvocationException ex)
                {
                    DebugLog.Log("Warning: Unable to patch " + method.Name);
                    DebugLog.Log(ex.ToString());
                }
            }

            mod = modEntry;
            return true;
        }

        private static void GmEnableCheckbox_Action(string id, Dialog dialog, bool value)
        {
            Main.gmEnabled = value;
        }

        private static void HearEveryone_Action(string id, Dialog dialog, bool value)
        {
            Main.hearEveryone = value;
            if (!value) {
                foreach (Person p in Managers.personManager.GetCurrentAreaPersons()) {
                    if (!p.isOurPerson) {
                        p.SetAmplifySpeech(p.amplifySpeech);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(AreaManager), nameof(AreaManager.weAreOwnerOfCurrentArea), MethodType.Getter)]
    public static class ForceOwner
    {
        public static bool Prefix(ref bool __result)
        {
            __result = Main.gmEnabled;
            return !__result;
        }
    }

    [HarmonyPatch(typeof(AreaManager), nameof(AreaManager.weAreEditorOfCurrentArea), MethodType.Getter)]
    public static class ForceEditor
    {
        public static bool Prefix(ref bool __result)
        {
            __result = Main.gmEnabled;
            return !__result;
        }
    }

    [HarmonyPatch(typeof(MySizeDialog), "HandleSetMyScale")]
    public static class NoSizeLimit
    {
        private static float MathfClampIfNotGodMode(float value, float min, float max)
        {
            if (Main.gmEnabled)
                return value;
            else
                return Mathf.Clamp(value, min, max);
        }

        private static IEnumerable<CodeInstruction> InnerTranspiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code)
            {
                if (inst.opcode == OpCodes.Call && ((MethodInfo)inst.operand).Name.Equals("Clamp"))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NoSizeLimit), nameof(MathfClampIfNotGodMode)));
                else
                    yield return inst;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            foreach (CodeInstruction inst in code)
            {
                yield return inst;
                if (inst.opcode == OpCodes.Ldftn)
                {
                    MethodBase method = (MethodBase)inst.operand;
                    Main.harmony.Patch(method, transpiler: new HarmonyMethod(typeof(NoSizeLimit), nameof(InnerTranspiler)));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Hand), "GetLaserDistanceMultiplier")]
    public static class DoNotClampAirLaserDistance {
        public static void Prefix(ref bool isForAirLaser)
        {
            if (Main.gmEnabled)
                isForAirLaser = false;
        }
    }

    [HarmonyPatch(typeof(Person), "Update")]
    public static class ForceMaxSpeechDistance {
        public static void Postfix(Person __instance)
        {
            if (Main.hearEveryone) {
                if (__instance.Head != null) {
                    __instance.Head.GetComponent<AudioSource>().maxDistance = 999999f;
                }
            }
        }
    }
}
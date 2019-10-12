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
                        FileLog.Log("Warning: operand " + inst.operand.ToString() + " is not FieldInfo!");
                    }
                }
            }
        }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();

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
                    FileLog.Log("Warning: Unable to patch " + method.Name);
                    FileLog.Log(ex.ToString());
                }
            }

            mod = modEntry;
            return true;
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.Start))]
    public static class AddButton
    {
        public static void Postfix(OwnProfileDialog __instance)
        {
            __instance.AddCheckbox("godMode", null, "Enable God Mode", 0, 300, Main.gmEnabled, textColor: TextColor.Blue, extraIcon: ExtraIcon.Unlocked);
        }
    }

    [HarmonyPatch(typeof(OwnProfileDialog), nameof(OwnProfileDialog.OnClick), new Type[] { typeof(string), typeof(string), typeof(bool), typeof(GameObject) })]
    public static class HandleButtonClick
    {
        public static void Postfix(OwnProfileDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName == "godMode")
            {
                Main.gmEnabled = state;
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
}
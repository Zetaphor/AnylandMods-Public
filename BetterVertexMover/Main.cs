using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using System.Reflection;

namespace AnylandMods.BetterVertexMover
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static FalloffFunction falloff;
        internal static Vector3[] savedVertices = null;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            falloff = new Functions.Dome(0.0f);

            return true;
        }
    }

    [HarmonyPatch(typeof(VertexMoverDialog), nameof(VertexMoverDialog.Start))]
    public static class ExtendDialog
    {
        public static void RadiusSliderChange(float value)
        {
            Main.falloff.Radius = value;
        }

        public static void Postfix(VertexMoverDialog __instance)
        {
            __instance.AddSlider("Area Radius: ", "", 0, 30, 0, 2, false, Main.falloff.Radius, new Action<float>(RadiusSliderChange));
            int x = -300;
            foreach (string func in new string[] { "Dome", "Linear", "Sharp", "Constant" })
            {
                bool state = (Main.falloff.GetType().Name.Equals(func));
                __instance.AddButton("falloff_" + func, null, func, "ButtonSmallCentered", x, -420, state: state, textColor: TextColor.Blue);
                x += 100;
            }
        }
    }

    [HarmonyPatch(typeof(VertexMoverDialog), nameof(VertexMoverDialog.OnClick))]
    public static class HandleButtonClicks
    {
        public static void Postfix(VertexMoverDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.StartsWith("falloff_"))
            {
                string func = contextName.Substring(8);
                Type funcType = Assembly.GetExecutingAssembly().GetType(func);
                Main.falloff = (FalloffFunction)funcType.GetConstructor(new Type[] { typeof(float) }).Invoke(new object[] { Main.falloff.Radius });
            }
        }
    }

    [HarmonyPatch(typeof(VertexMover), "HandlePress")]
    public static class ResetSavedVerticesOnPress
    {
        public static void Postfix(VertexMover __instance)
        {
            if (!__instance.handDot.GetPress(CrossDevice.button_grabTip))
            {
                Main.savedVertices = null;
            }
        }
    }

    [HarmonyPatch(typeof(VertexMover), "AdjustGrabbedVertexPosition")]
    public static class AdjustPositionHook
    {
        public static void Prefix(VertexMover __instance, ref Vector3[] __state, bool roundIfNeeded)
        {
            __state = (Vector3[])__instance.mesh.vertices.Clone();
        }

        public static void Postfix(VertexMover __instance, Vector3[] __state, bool roundIfNeeded)
        {
            if (Main.savedVertices == null)
            {
                Main.savedVertices = __state;
            }
            int grabbed = __instance.grabbedVertexIndex();
            Vector3 origpos = Main.savedVertices[grabbed];
            Vector3 thispos = __state[grabbed];
            Vector3[] vertices = __instance.mesh.vertices;
            Vector3 delta = vertices[grabbed] - thispos;
            ThingPart tp = __instance.thingPart();
            for (int i=0; i<__state.Length; ++i)
            {
                Vector3 vtxpos = __state[i];
                Vector3 vtxorig = Main.savedVertices[i];
                if (vtxpos != thispos)
                {
                    float distance = (vtxorig - origpos).magnitude;
                    if (distance < Main.falloff.Radius)
                    {
                        vertices[i] += delta * Main.falloff.ValueAt(distance);
                        tp.changedVertices[i] = vertices[i];
                    }
                }
            }

            __instance.mesh.vertices = vertices;
            __instance.RecalculateNormals();
        }
    }
}
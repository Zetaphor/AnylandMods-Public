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
            __instance.AddSlider("Area Radius: ", "", 0, 30, 0, 4, false, Main.falloff.Radius, new Action<float>(RadiusSliderChange));
            __instance.AddButton("falloffDome", null, "Dome", "ButtonSmallCentered", -300, -420, textColor: TextColor.Blue);
            __instance.AddButton("falloffLinear", null, "Linear", "ButtonSmallCentered", -200, -420, textColor: TextColor.Blue);
            __instance.AddButton("falloffSharp", null, "Sharp", "ButtonSmallCentered", -100, -420, textColor: TextColor.Blue);
            __instance.AddButton("falloffConstant", null, "Constant", "ButtonSmallCentered", 0, -420, textColor: TextColor.Blue);
        }
    }

    [HarmonyPatch(typeof(VertexMoverDialog), nameof(VertexMoverDialog.OnClick))]
    public static class HandleButtonClicks
    {
        public static void Postfix(VertexMoverDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("falloffDome"))
                Main.falloff = new Functions.Dome(Main.falloff.Radius);
            else if (contextName.Equals("falloffLinear"))
                Main.falloff = new Functions.Linear(Main.falloff.Radius);
            else if (contextName.Equals("falloffSharp"))
                Main.falloff = new Functions.Sharp(Main.falloff.Radius);
            else if (contextName.Equals("falloffConstant"))
                Main.falloff = new Functions.Constant(Main.falloff.Radius);
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
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
        internal struct UndoVertex {
            public int index;
            public Vector3 pos;

            public UndoVertex(int index, Vector3 pos)
            {
                this.index = index;
                this.pos = pos;
            }
        }

        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        internal static FalloffFunction falloff;
        internal static Vector3[] savedVertices = null;
        internal static Stack<UndoVertex[]> undoStack;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;

            falloff = new Functions.Smooth(0.0f);

            return true;
        }

        private static void Mb_Action(string id, Dialog dialog)
        {
            Managers.dialogManager.SwitchToNewDialog(DialogType.VertexMover, dialog.hand(), dialog.tabName);
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
            if (!(Main.undoStack is null))
                Main.undoStack.Clear();
            __instance.AddButton("undo", null, null, "ButtonVerySmall", 450, -25, "undo");
            __instance.AddButton("falloffSmooth", null, "Smooth", "ButtonSmallCentered", -275, -420, textSizeFactor: 0.75f, textColor: TextColor.Blue);
            __instance.AddButton("falloffDome", null, "Dome", "ButtonSmallCentered", -175, -420, textColor: TextColor.Blue);
            __instance.AddButton("falloffLinear", null, "Linear", "ButtonSmallCentered", -75, -420, textSizeFactor: 0.75f, textColor: TextColor.Blue);
            __instance.AddButton("falloffSharp", null, "Sharp", "ButtonSmallCentered", 25, -420, textColor: TextColor.Blue);
            __instance.AddButton("falloffConstant", null, "Constant", "ButtonSmallCentered", 125, -420, textSizeFactor: 0.6f, textColor: TextColor.Blue);
            __instance.AddButton("invert", null, "Invert", "ButtonSmallCentered", 450, -125, textSizeFactor: 0.75f, textColor: TextColor.Blue);
            __instance.AddSlider("Effect Area: ", "", 0, 30, 0, 4, false, Main.falloff.Radius, new Action<float>(RadiusSliderChange));
        }
    }

    [HarmonyPatch(typeof(VertexMoverDialog), nameof(VertexMoverDialog.OnClick))]
    public static class HandleButtonClicks
    {
        public static void Postfix(VertexMoverDialog __instance, string contextName, string contextId, bool state, GameObject thisButton)
        {
            if (contextName.Equals("falloffSmooth"))
                Main.falloff = new Functions.Smooth(Main.falloff.Radius);
            else if (contextName.Equals("falloffDome"))
                Main.falloff = new Functions.Dome(Main.falloff.Radius);
            else if (contextName.Equals("falloffLinear"))
                Main.falloff = new Functions.Linear(Main.falloff.Radius);
            else if (contextName.Equals("falloffSharp"))
                Main.falloff = new Functions.Sharp(Main.falloff.Radius);
            else if (contextName.Equals("falloffConstant"))
                Main.falloff = new Functions.Constant(Main.falloff.Radius);
            else if (contextName.Equals("undo")) {
                if (Main.undoStack.Count > 0) {
                    Main.UndoVertex[] undoVertices = Main.undoStack.Pop();
                    VertexMover mover = __instance.vertexMover();
                    ThingPart tp = mover.thingPart();
                    Vector3[] vertices = mover.mesh.vertices;
                    foreach (Main.UndoVertex vertex in undoVertices) {
                        vertices[vertex.index] = vertex.pos;
                        tp.changedVertices[vertex.index] = vertex.pos;
                    }
                    mover.mesh.vertices = vertices;
                } else {
                    Managers.soundManager.Play("no", __instance.transform, 0.5f, false, false);
                }
            } else if (contextName.Equals("invert")) {
                VertexMover mover = __instance.vertexMover();
                ThingPart tp = mover.thingPart();
                Vector3[] vertices = mover.mesh.vertices;
                for (int i=0; i<vertices.Length; ++i) {
                    vertices[i] = new Vector3(-vertices[i].x, vertices[i].y, vertices[i].z);
                    tp.changedVertices[i] = vertices[i];
                }
                mover.mesh.vertices = vertices;
            }
        }
    }

    [HarmonyPatch(typeof(VertexMover), "HandlePress")]
    public static class ResetSavedVerticesOnPress
    {
        public static void Postfix(VertexMover __instance)
        {
            if (Main.savedVertices != null && !__instance.handDot.GetPress(CrossDevice.button_grabTip))
            {
                var undoVertices = new List<Main.UndoVertex>();
                Vector3[] meshVertices = __instance.mesh.vertices;
                for (int i=0; i<Main.savedVertices.Length; ++i) {
                    if (!meshVertices[i].Equals(Main.savedVertices[i])) {
                        undoVertices.Add(new Main.UndoVertex(i, Main.savedVertices[i]));
                    }
                }
                if (Main.undoStack is null)
                    Main.undoStack = new Stack<Main.UndoVertex[]>();
                Main.undoStack.Push(undoVertices.ToArray());
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
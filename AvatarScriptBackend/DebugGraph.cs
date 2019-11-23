using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnylandMods.AvatarScriptBackend {
    public class DebugGraph : MonoBehaviour {
        private Mesh mesh = null;
        private double[] data = null;
        private Vector3[] vertices;

        public double[] DataArray {
            get => data;
            set {
                data = value;
                CreateMesh();
            }
        }

        public Material Material { get; set; }
        public Matrix4x4 Matrix { get; set; }

        private void CreateMesh()
        {
            if (data is null) {
                mesh = null;
            } else {
                mesh = new Mesh();
                vertices = new Vector3[2 * data.Length];
                mesh.vertices = vertices;
                var triangles = new int[6 * (data.Length - 1)];
                for (int i = 0; i < data.Length - 1; ++i) {
                    int ibase = 6 * i;
                    /*ab
                     *cd*/
                    int a = data.Length + i;
                    int b = a + 1;
                    int c = i;
                    int d = c + 1;
                    triangles[ibase] = a;
                    triangles[ibase + 1] = b;
                    triangles[ibase + 2] = c;
                    triangles[ibase + 3] = b;
                    triangles[ibase + 4] = d;
                    triangles[ibase + 5] = c;
                }
                mesh.triangles = triangles;
            }
        }

        private void UpdateMesh()
        {
            if (mesh != null) {
                float pitch = 1.0f / data.Length;
                for (int i = 0; i < data.Length; ++i) {
                    float x = pitch * i;
                    vertices[i] = new Vector3(x, 0);
                    vertices[data.Length + i] = new Vector3(x, (float)data[i]);
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
        }

        public DebugGraph()
        {
            Material = new Material(Shader.Find("Diffuse"));
            Material.color = Color.green;
        }

        void Start()
        {
            Matrix = Matrix4x4.identity;
        }

        void Update()
        {
            UpdateMesh();
            if (mesh != null) {
                Graphics.DrawMesh(mesh, gameObject.transform.localToWorldMatrix * Matrix, Material, gameObject.layer);
            }
        }
    }
}
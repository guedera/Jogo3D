using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRacers
{
    /// <summary>Small flat-shaded mesh builder; forests are batched per terrain chunk.</summary>
    public sealed class AlpineMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<Color> colors = new List<Color>();

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2);
            colors.Add(color); colors.Add(color); colors.Add(color);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        { Triangle(a, b, c, color); Triangle(a, c, d, color); }

        public void Box(Vector3 center, Vector3 size, Color color)
        {
            Vector3 a = center - size * .5f, b = center + size * .5f;
            Quad(new Vector3(a.x,a.y,a.z), new Vector3(a.x,b.y,a.z), new Vector3(b.x,b.y,a.z), new Vector3(b.x,a.y,a.z), color);
            Quad(new Vector3(a.x,a.y,b.z), new Vector3(b.x,a.y,b.z), new Vector3(b.x,b.y,b.z), new Vector3(a.x,b.y,b.z), color);
            Quad(new Vector3(a.x,b.y,a.z), new Vector3(a.x,b.y,b.z), new Vector3(b.x,b.y,b.z), new Vector3(b.x,b.y,a.z), color);
            Quad(new Vector3(a.x,a.y,a.z), new Vector3(b.x,a.y,a.z), new Vector3(b.x,a.y,b.z), new Vector3(a.x,a.y,b.z), color);
            Quad(new Vector3(a.x,a.y,a.z), new Vector3(a.x,a.y,b.z), new Vector3(a.x,b.y,b.z), new Vector3(a.x,b.y,a.z), color);
            Quad(new Vector3(b.x,a.y,a.z), new Vector3(b.x,b.y,a.z), new Vector3(b.x,b.y,b.z), new Vector3(b.x,a.y,b.z), color);
        }
        void Cone(Vector3 baseCenter, float radius, float height, Color color)
        {
            const int sides = 7;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 first = baseCenter + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                Vector3 next = baseCenter + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
                Triangle(first, baseCenter + Vector3.up * height, next, color);
                Triangle(baseCenter, first, next, color * .8f);
            }
        }
        public void Pine(Vector3 basePosition, float height, bool snow)
        {
            Box(basePosition + Vector3.up * height * .23f, new Vector3(.9f, height * .46f, .9f), new Color(.23f, .23f, .20f));
            for (int layer = 0; layer < 3; layer++)
            {
                float y = height * (.18f + layer * .21f), h = height * (.49f - layer * .04f);
                float radius = height * (.22f - layer * .045f);
                Cone(basePosition + Vector3.up * y, radius, h, new Color(.10f + layer * .015f, .24f + layer * .025f, .19f + layer * .015f));
                if (snow) Cone(basePosition + Vector3.up * (y + h * .52f), radius * .50f, h * .49f, new Color(.83f, .88f, .89f));
            }
        }
        public void Arch(float center, float radius, float spring, float rise, float top, float depth)
        {
            const int segments = 24;
            float z = depth * .5f;
            for (int i = 0; i < segments; i++)
            {
                float u = -1 + 2f * i / segments, v = -1 + 2f * (i + 1) / segments;
                float x0 = center + u * radius, x1 = center + v * radius;
                float y0 = spring + rise * Mathf.Sqrt(Mathf.Max(0, 1 - u * u));
                float y1 = spring + rise * Mathf.Sqrt(Mathf.Max(0, 1 - v * v));
                Color color = new Color(.48f, .51f, .50f) * (i % 2 == 0 ? 1 : .93f);
                Quad(new Vector3(x0,y0,-z), new Vector3(x0,top,-z), new Vector3(x1,top,-z), new Vector3(x1,y1,-z), color);
                Quad(new Vector3(x0,y0,z), new Vector3(x1,y1,z), new Vector3(x1,top,z), new Vector3(x0,top,z), color);
                Quad(new Vector3(x0,top,-z), new Vector3(x0,top,z), new Vector3(x1,top,z), new Vector3(x1,top,-z), color);
                Quad(new Vector3(x0,y0,-z), new Vector3(x1,y1,-z), new Vector3(x1,y1,z), new Vector3(x0,y0,z), color * .8f);
                if (i == 0) Quad(new Vector3(x0,y0,-z), new Vector3(x0,y0,z), new Vector3(x0,top,z), new Vector3(x0,top,-z), color);
                if (i == segments - 1) Quad(new Vector3(x1,y1,-z), new Vector3(x1,top,-z), new Vector3(x1,top,z), new Vector3(x1,y1,z), color);
            }
        }
        public GameObject Create(Transform parent, Material material, string name, bool collision)
        {
            var mesh = new Mesh { name = name.Replace(' ', '_'), indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true; return go;
        }
    }
}

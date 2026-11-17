using System.Collections.Generic;
using UnityEngine;

namespace SkyRacers
{
    public sealed class CanyonCourse : MonoBehaviour
    {
        public float ceiling = 150;
        public float length = 14800;
        public Material rockMaterial, markerMaterial, darkMaterial;
        public Vector3[] route;
        public float[] widths;
        public float[] cumulative;
        public float TotalDistance => cumulative[cumulative.Length - 1];
        public Vector3 Spawn => route[0] + Vector3.up * 65;
        public Quaternion SpawnRotation => Quaternion.LookRotation(route[1] - route[0]);

        public void Generate()
        {
            const int count = 186;
            route = new Vector3[count]; widths = new float[count]; cumulative = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float ramp = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .2f, t));
                float x = ramp * (420 * Mathf.Sin(t * Mathf.PI * 8) + 160 * Mathf.Sin(t * Mathf.PI * 17));
                route[i] = new Vector3(x, 0, t * length);
                widths[i] = Mathf.Lerp(140, 92, t) + 12 * Mathf.Sin(t * Mathf.PI * 12);
                if (i > 0) cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            }
            var random = new System.Random(1806);
            var rings = new List<Vector3[]>();
            // Extend behind the spawn so the camera never sees an open back seam.
            for (int i = -4; i <= count + 3; i++)
            {
                int n = Mathf.Clamp(i, 0, count - 1);
                Vector3 center = route[n];
                if (i < 0) center += Vector3.back * (-i * 80);
                if (i >= count) center += (route[count - 1] - route[count - 2]).normalized * ((i - count + 1) * 80);
                Vector3 forward = route[Mathf.Min(n + 1, count - 1)] - route[Mathf.Max(0, n - 1)];
                Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
                float w = widths[n];
                float jitterL = (float)random.NextDouble() * 16;
                float jitterR = (float)random.NextDouble() * 16;
                float[] offsets = { -w - 1500, -w * 1.35f - jitterL, -w * 1.12f - jitterL, -w, 0, w, w * 1.12f + jitterR, w * 1.35f + jitterR, w + 1500 };
                float[] heights = { ceiling, ceiling, 72 + jitterL, 0, 0, 0, 72 + jitterR, ceiling, ceiling };
                var ring = new Vector3[9];
                for (int j = 0; j < 9; j++) ring[j] = center + right * offsets[j] + Vector3.up * heights[j];
                rings.Add(ring);
            }
            for (int start = 0; start < rings.Count - 1; start += 8)
            {
                var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
                int end = Mathf.Min(start + 8, rings.Count - 1);
                for (int i = start; i < end; i++)
                for (int j = 0; j < 8; j++)
                {
                    bool floor = j == 3 || j == 4;
                    Color color = floor ? new Color(.49f, .31f, .20f) : new Color(.64f, .36f, .22f);
                    if (j == 1 || j == 6) color = new Color(.78f, .48f, .29f);
                    color *= .88f + (float)random.NextDouble() * .22f;
                    AddTriangle(vertices, indices, colors, rings[i][j], rings[i + 1][j], rings[i][j + 1], color);
                    AddTriangle(vertices, indices, colors, rings[i][j + 1], rings[i + 1][j], rings[i + 1][j + 1], color * .97f);
                }
                var mesh = new Mesh { name = "Canyon_" + start };
                mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetColors(colors);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var chunk = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                chunk.transform.SetParent(transform, false); chunk.isStatic = true;
                chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
                chunk.GetComponent<MeshRenderer>().sharedMaterial = rockMaterial;
                chunk.GetComponent<MeshCollider>().sharedMesh = mesh;
            }
            // Small paired course markers communicate direction without blocking the path.
            for (int i = 5; i < count - 2; i += 7)
            {
                var tangent = (route[i + 1] - route[i - 1]).normalized;
                var right = Vector3.Cross(Vector3.up, tangent);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = route[i] + right * widths[i] * .88f * side;
                    Primitive("Route beacon", p + Vector3.up * 21, new Vector3(2, 42, 2), darkMaterial);
                    Primitive("Beacon light", p + Vector3.up * 43, new Vector3(3, 5, 3), markerMaterial);
                }
            }
            var finish = route[count - 1];
            var fwd = (finish - route[count - 2]).normalized;
            var across = Vector3.Cross(Vector3.up, fwd);
            for (int s = -1; s <= 1; s += 2)
                Primitive("Finish tower", finish + across * 87 * s + Vector3.up * 70, new Vector3(5, 140, 5), markerMaterial);
            var top = Primitive("Finish beam", finish + Vector3.up * 140, new Vector3(179, 4, 4), markerMaterial);
            top.transform.rotation = Quaternion.LookRotation(fwd);
        }
        GameObject Primitive(string label, Vector3 p, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(transform, false); go.transform.position = p; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true;
            return go;
        }
        static void AddTriangle(List<Vector3> v, List<int> t, List<Color> c, Vector3 a, Vector3 b, Vector3 d, Color color)
        { int n = v.Count; v.Add(a); v.Add(b); v.Add(d); t.Add(n); t.Add(n + 1); t.Add(n + 2); c.Add(color); c.Add(color); c.Add(color); }

        public bool CrossedSector(int sector, Vector3 previous, Vector3 current)
        {
            Vector3 tangent = (route[sector] - route[sector - 1]).normalized;
            float before = Vector3.Dot(previous - route[sector], tangent);
            float after = Vector3.Dot(current - route[sector], tangent);
            if (before >= 0 || after < 0) return false;
            float t = -before / (after - before);
            Vector3 crossing = Vector3.Lerp(previous, current, t);
            Vector3 right = Vector3.Cross(Vector3.up, tangent);
            return crossing.y > 0 && crossing.y < ceiling && Mathf.Abs(Vector3.Dot(crossing - route[sector], right)) < widths[sector] * 1.35f;
        }
    }
}

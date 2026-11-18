using System.Collections.Generic;
using UnityEngine;

namespace SkyRacers
{
    public sealed class CanyonCourse : MonoBehaviour
    {
        [Tooltip("Allowed height above the local canyon floor, not world altitude.")]
        public float ceiling = 450;
        public float length = 40000;
        public Material rockMaterial, markerMaterial, darkMaterial;
        public Vector3[] route;
        public float[] widths;
        public float[] cumulative;
        public float TotalDistance => cumulative[cumulative.Length - 1];
        public Vector3 Spawn => route[0] + Vector3.up * 65;
        public Quaternion SpawnRotation => Quaternion.LookRotation(route[1] - route[0]);
        public int BridgeSector => route.Length / 2;
        public float BridgeHalfSpan => 11f;
        public float BridgeSpring => ceiling - 42;
        public float BridgeRise => 30;

        public float DistanceRemaining(Vector3 position, int nextSector)
        {
            if (nextSector >= route.Length) return 0;
            int end = Mathf.Clamp(nextSector, 1, route.Length - 1);
            Vector3 segment = route[end] - route[end - 1];
            float t = Mathf.Clamp01(Vector3.Dot(position - route[end - 1], segment) / segment.sqrMagnitude);
            float travelled = Mathf.Lerp(cumulative[end - 1], cumulative[end], t);
            return Mathf.Max(0, TotalDistance - travelled);
        }

        public float FloorAt(float z)
        {
            if (route == null || route.Length < 2) return 0;
            float index = Mathf.Clamp(z / length, 0, 1) * (route.Length - 1);
            int a = Mathf.Min(Mathf.FloorToInt(index), route.Length - 2);
            return Mathf.Lerp(route[a].y, route[a + 1].y, index - a);
        }
        public float CeilingAt(Vector3 position) => FloorAt(position.z) + ceiling;
        public Vector3 RightAt(int i)
        {
            Vector3 forward = route[Mathf.Min(i + 1, route.Length - 1)] - route[Mathf.Max(0, i - 1)];
            return Vector3.Cross(Vector3.up, forward).normalized;
        }

        public void Generate()
        {
            const int count = 1001;
            route = new Vector3[count]; widths = new float[count]; cumulative = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float ramp = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.04f, .2f, t));
                float x = 2.7f * ramp * (420 * Mathf.Sin(t * Mathf.PI * 8) + 160 * Mathf.Sin(t * Mathf.PI * 17));
                float y = ramp * (110 + 85 * Mathf.Sin(t * Mathf.PI * 4 - .4f) + 35 * Mathf.Sin(t * Mathf.PI * 11));
                route[i] = new Vector3(x, y, t * length);
                widths[i] = .36f * (Mathf.Lerp(140, 92, t) + 12 * Mathf.Sin(t * Mathf.PI * 12));
                if (i > 0) cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            }
            var random = new System.Random(1806);
            var rings = new List<Vector3[]>();
            for (int i = -4; i <= count + 3; i++)
            {
                int n = Mathf.Clamp(i, 0, count - 1);
                Vector3 center = route[n];
                if (i < 0) center += Vector3.back * (-i * 40);
                if (i >= count) center += Vector3.forward * ((i - count + 1) * 40);
                Vector3 right = RightAt(n);
                float w = widths[n];
                float jl = (float)random.NextDouble() * 16, jr = (float)random.NextDouble() * 16;
                float ridge = 300 + 130 * Mathf.Sin(n * .13f) + 100 * Mathf.Sin(n * .051f);
                float[] offsets = { -w - 1800, -w - 600, -w * 1.35f - jl, -w * 1.12f - jl, -w, 0, w, w * 1.12f + jr, w * 1.35f + jr, w + 600, w + 1800 };
                float[] heights = { ceiling + ridge, ceiling + 60, ceiling, ceiling * .48f + jl, 0, 0, 0, ceiling * .48f + jr, ceiling, ceiling + 60, ceiling + ridge * 1.2f };
                var ring = new Vector3[11];
                for (int j = 0; j < ring.Length; j++) ring[j] = center + right * offsets[j] + Vector3.up * heights[j];
                rings.Add(ring);
            }
            for (int start = 0; start < rings.Count - 1; start += 8)
            {
                var ground = new AlpineMesh(); var forest = new AlpineMesh();
                int end = Mathf.Min(start + 8, rings.Count - 1);
                for (int i = start; i < end; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Color color;
                        if (j == 4 || j == 5) color = new Color(.29f, .36f, .31f);
                        else if (j == 0 || j == 9) color = new Color(.78f, .83f, .85f);
                        else if (j == 1 || j == 8) color = Mathf.PerlinNoise(i * .08f, j * .7f) > .58f ? new Color(.79f, .84f, .84f) : new Color(.24f, .34f, .28f);
                        else color = new Color(.43f, .48f, .49f);
                        color *= .97f + (float)random.NextDouble() * .06f;
                        ground.Quad(rings[i][j], rings[i + 1][j], rings[i + 1][j + 1], rings[i][j + 1], color);
                    }
                    // Cold stream at the very bottom, following the same elevation profile.
                    var c0 = rings[i][5] + Vector3.up * .2f; var c1 = rings[i + 1][5] + Vector3.up * .2f;
                    Vector3 right = (rings[i][6] - rings[i][5]).normalized;
                    ground.Quad(c0 - right * 7, c1 - right * 7, c1 + right * 7, c0 + right * 7, new Color(.12f, .29f, .34f));
                    for (int side = 0; side < 2; side++)
                    for (int tree = 0; tree < 5; tree++)
                    {
                        int inner = side == 0 ? 2 : 8, outer = side == 0 ? 1 : 9;
                        float t = (float)random.NextDouble(), across = .04f + (float)random.NextDouble() * .92f;
                        Vector3 a = Vector3.Lerp(rings[i][inner], rings[i + 1][inner], t);
                        Vector3 b = Vector3.Lerp(rings[i][outer], rings[i + 1][outer], t);
                        Vector3 p = Vector3.Lerp(a, b, across);
                        float height = 14 + (float)random.NextDouble() * 19;
                        forest.Pine(p, height, tree % 4 == 0);
                    }
                    // Occasional lower-bank trees, kept away from the central flight corridor.
                    if (i % 6 == 0)
                    for (int side = 0; side < 2; side++)
                    {
                        Vector3 p = Vector3.Lerp(rings[i][5], rings[i][side == 0 ? 4 : 6], .94f);
                        float height = 16 + (float)random.NextDouble() * 9;
                        forest.Pine(p, height, i % 9 == 0);
                        var obstacle = new GameObject("Pine collision").AddComponent<CapsuleCollider>();
                        obstacle.transform.SetParent(transform, false); obstacle.transform.position = p;
                        obstacle.center = Vector3.up * height * .5f; obstacle.height = height; obstacle.radius = height * .13f;
                    }
                }
                ground.Create(transform, rockMaterial, "Alpine canyon " + start, true);
                forest.Create(transform, rockMaterial, "Pine forest " + start, false);
            }
            BuildBridge();
            BuildWaterfall(count / 3, -1, rings);
            BuildWaterfall(count * 3 / 4, 1, rings);
            var finish = route[count - 1]; var acrossFinish = RightAt(count - 1);
            float halfWidth = widths[count - 1] * .9f;
            // Finish is a painted strip across the floor, without artificial poles.
            var stripe = new AlpineMesh();
            Vector3 along = Vector3.Cross(acrossFinish, Vector3.up);
            stripe.Quad(finish - acrossFinish * halfWidth - along * 5 + Vector3.up * .4f,
                finish - acrossFinish * halfWidth + along * 5 + Vector3.up * .4f,
                finish + acrossFinish * halfWidth + along * 5 + Vector3.up * .4f,
                finish + acrossFinish * halfWidth - along * 5 + Vector3.up * .4f, new Color(.8f,.85f,.8f));
            stripe.Create(transform, rockMaterial, "Finish marking", false);
        }

        void BuildWaterfall(int index, int side, List<Vector3[]> rings)
        {
            Vector3 right = RightAt(index) * side, origin = route[index];
            var water = new AlpineMesh();
            var ring=rings[index+4];
            Vector3 top=ring[side<0?2:8], middle=ring[side<0?3:7], bottom=ring[side<0?4:6];
            Vector3 At(float t) => (t<.52f?Vector3.Lerp(top,middle,t/.52f):Vector3.Lerp(middle,bottom,(t-.52f)/.48f))-right*.8f;
            Vector3 across = Vector3.Cross(right, Vector3.up);
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f, b = (i + 1) / 48f;
                Vector3 p = At(a);
                Vector3 q = At(b);
                float w = 4 + a * 3;
                water.Quad(p-across*w, q-across*w, q+across*w, p+across*w, new Color(.63f,.81f,.87f));
                water.Quad(p+across*w, q+across*w, q-across*w, p-across*w, new Color(.63f,.81f,.87f));
            }
            var fall = water.Create(transform, rockMaterial, "Waterfall " + index, false);
            fall.AddComponent<WaterfallMist>().origin = origin + right * widths[index] * .98f + Vector3.up * 5;
        }

        void BuildBridge()
        {
            var bridge = new GameObject("Midpoint arch bridge").transform;
            bridge.SetParent(transform, false); bridge.position = route[BridgeSector];
            bridge.rotation = Quaternion.LookRotation(Vector3.Cross(RightAt(BridgeSector), Vector3.up));
            var masonry = new AlpineMesh();
            float main = BridgeHalfSpan, outer = widths[BridgeSector] * 1.35f + 30;
            float sideRadius = (outer - main - 16) * .5f;
            masonry.Arch(0, main, BridgeSpring, BridgeRise, ceiling + 6, 12);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                masonry.Box(new Vector3(sign * (main + 4), (ceiling + 6) * .5f, 0), new Vector3(8, ceiling + 6, 20), new Color(.47f, .49f, .47f));
                masonry.Arch(sign * (main + 8 + sideRadius), sideRadius, BridgeSpring, BridgeRise, ceiling + 6, 16);
                masonry.Box(new Vector3(sign * (outer - 4), (ceiling + 6) * .5f, 0), new Vector3(8, ceiling + 6, 20), new Color(.47f, .49f, .47f));
            }
            masonry.Box(new Vector3(0, ceiling + 8, 0), new Vector3(outer * 2, 4, 24), new Color(.27f, .30f, .29f));
            for (int side = -1; side <= 1; side += 2)
                masonry.Box(new Vector3(0, ceiling + 12, side * 11), new Vector3(outer * 2, 4, 1), new Color(.65f, .69f, .68f));
            masonry.Create(bridge, rockMaterial, "Stone arches and deck", true);
            // One bridge only; its deck reaches above the local flight ceiling.
        }
        public bool CrossedBridgePlane(Vector3 previous, Vector3 current, out Vector3 crossing)
        {
            Vector3 normal = Vector3.Cross(RightAt(BridgeSector), Vector3.up);
            float a = Vector3.Dot(previous - route[BridgeSector], normal), b = Vector3.Dot(current - route[BridgeSector], normal);
            crossing = current;
            if (a >= 0 || b < 0) return false;
            crossing = Vector3.Lerp(previous, current, -a / (b - a)); return true;
        }
        public bool UnderBridge(Vector3 point)
        {
            Vector3 relative = point - route[BridgeSector];
            float x = Mathf.Abs(Vector3.Dot(relative, RightAt(BridgeSector)));
            if (x + 8 >= BridgeHalfSpan) return false;
            // Reserve clearance for wings and tail, not just the aircraft centre.
            float arch = BridgeSpring + BridgeRise * Mathf.Sqrt(1 - Mathf.Pow((x + 8) / BridgeHalfSpan, 2));
            return relative.y > 3 && relative.y + 3 < arch;
        }

        GameObject Primitive(string label, Vector3 p, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(transform, false); go.transform.position = p; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true; return go;
        }
        public bool CrossedSector(int sector, Vector3 previous, Vector3 current)
        {
            Vector3 tangent = Vector3.Cross(RightAt(sector), Vector3.up);
            float before = Vector3.Dot(previous - route[sector], tangent), after = Vector3.Dot(current - route[sector], tangent);
            if (before >= 0 || after < 0) return false;
            Vector3 crossing = Vector3.Lerp(previous, current, -before / (after - before));
            float floor = FloorAt(crossing.z);
            return crossing.y > floor && crossing.y < floor + ceiling && Mathf.Abs(Vector3.Dot(crossing - route[sector], RightAt(sector))) < widths[sector] * 1.35f;
        }
    }
}

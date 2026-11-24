using System.Collections.Generic;
using UnityEngine;

namespace SkyRacers
{
    public sealed class CanyonCourse : MonoBehaviour
    {
        [Tooltip("Radar exposure height above the local canyon floor, not world altitude.")]
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
                widths[i] = Mathf.Lerp(.36f * (Mathf.Lerp(140, 92, t) + 12 * Mathf.Sin(t * Mathf.PI * 12)), 3200, Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,t)));
                if (i > 0) cumulative[i] = cumulative[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            }
            var random = new System.Random(1806);
            var rings = new List<Vector3[]>();
            for (int i = -4; i <= count + 125; i++)
            {
                int n = Mathf.Clamp(i, 0, count - 1);
                Vector3 center = route[n];
                if (i < 0) center += Vector3.back * (-i * 40);
                if (i >= count) center += Vector3.forward * ((i - count + 1) * 40);
                Vector3 right = RightAt(n);
                float w = widths[n];
                float jl = (float)random.NextDouble() * 16, jr = (float)random.NextDouble() * 16;
                float ridge = 300 + 130 * Mathf.Sin(n * .13f) + 100 * Mathf.Sin(n * .051f);
                // Keep every ring ordered even in the wide turnaround basin.
                float shoulder = Mathf.Max(w + 600, w * 1.35f + 180);
                float outerRidge = Mathf.Max(w + 1800, shoulder + 1200);
                float[] offsets = { -outerRidge, -shoulder, -w * 1.35f - jl, -w * 1.12f - jl, -w, 0, w, w * 1.12f + jr, w * 1.35f + jr, shoulder, outerRidge };
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
            BuildSurroundingMountains(rings);
            BuildBridge();
            BuildWaterfall(count / 3, -1, rings);
            BuildWaterfall(count * 3 / 4, 1, rings);
            BuildMissionArch(0, "BASE / CHEGADA", 32, 110, 40);
            BuildMissionArch(count-1, "RETORNO", 90, 180, 60);
        }

        // Wide terrain apron shares the canyon's edge vertices exactly. The flight
        // corridor stays narrow; only the landscape outside it is extended.
        void BuildMissionArch(int sector,string label,float radius,float spring,float rise)
        {
            var root=new GameObject(label+" - mission gate").transform;
            root.SetParent(transform,false);root.position=route[sector];
            root.rotation=Quaternion.LookRotation(Vector3.Cross(RightAt(sector),Vector3.up));
            var arch=new AlpineMesh();
            // Curved band instead of a solid wall above the opening.
            const int segments=48;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.PI*i/segments,b=Mathf.PI*(i+1)/segments;
                Vector3 innerA=new Vector3(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*rise,0);
                Vector3 innerB=new Vector3(Mathf.Cos(b)*radius,spring+Mathf.Sin(b)*rise,0);
                Vector3 outerA=new Vector3(Mathf.Cos(a)*(radius+4),spring+Mathf.Sin(a)*(rise+4),0);
                Vector3 outerB=new Vector3(Mathf.Cos(b)*(radius+4),spring+Mathf.Sin(b)*(rise+4),0);
                var z=Vector3.forward*3;
                arch.Quad(innerA-z,outerA-z,outerB-z,innerB-z,Color.white);
                arch.Quad(innerB+z,outerB+z,outerA+z,innerA+z,Color.white);
                arch.Quad(innerA-z,innerB-z,innerB+z,innerA+z,Color.white);
                arch.Quad(outerA+z,outerB+z,outerB-z,outerA-z,Color.white);
            }
            foreach(int side in new[]{-1,1})arch.Box(new Vector3(side*(radius+2),spring/2,0),new Vector3(4,spring,6),Color.white);
            arch.Create(root,markerMaterial,label+" arch",true);
        }

        void BuildSurroundingMountains(List<Vector3[]> canyon)
        {
            const int bands=12, caps=16;
            const float halfWidth=24000, endExtension=16000;
            var expanded=new List<Vector3[]>();
            foreach(var ring in canyon)
            {
                var row=new Vector3[ring.Length+bands*2];
                System.Array.Copy(ring,0,row,bands,ring.Length);
                for(int side=0;side<2;side++)
                {
                    Vector3 edge=ring[side==0?0:ring.Length-1];
                    for(int b=1;b<=bands;b++)
                    {
                        float t=b/(float)bands;
                        Vector3 point=new Vector3(Mathf.Lerp(edge.x,side==0?-halfWidth:halfWidth,t),0,edge.z);
                        point.y=Mathf.Lerp(edge.y,MountainHeight(point.x,point.z),Mathf.SmoothStep(0,1,Mathf.Min(1,t*4)));
                        row[side==0?bands-b:bands+ring.Length-1+b]=point;
                    }
                }
                expanded.Add(row);
            }
            var rows=new List<Vector3[]>();
            Vector3[] Cap(Vector3[] edge,int step,int sign)
            {
                var row=new Vector3[edge.Length];float t=step/(float)caps;
                for(int j=0;j<row.Length;j++)
                {
                    row[j]=edge[j]+Vector3.forward*(sign*endExtension*t);
                    row[j].y=Mathf.Lerp(edge[j].y,MountainHeight(row[j].x,row[j].z),Mathf.SmoothStep(0,1,Mathf.Min(1,t*3)));
                }
                return row;
            }
            for(int i=caps;i>0;i--)rows.Add(Cap(expanded[0],i,-1));
            rows.AddRange(expanded);
            for(int i=1;i<=caps;i++)rows.Add(Cap(expanded[expanded.Count-1],i,1));
            for(int start=0;start<rows.Count-1;start+=16)
            {
                var mesh=new AlpineMesh();
                for(int i=start;i<Mathf.Min(start+16,rows.Count-1);i++)
                for(int j=0;j<rows[i].Length-1;j++)
                {
                    bool insideCanyon=i>=caps&&i<caps+canyon.Count-1;
                    if(insideCanyon&&j>=bands&&j<bands+10)continue;
                    float height=(rows[i][j].y+rows[i+1][j+1].y)*.5f;
                    Color color=Color.Lerp(new Color(.31f,.39f,.34f),new Color(.80f,.84f,.85f),Mathf.InverseLerp(650,1200,height));
                    mesh.Quad(rows[i][j],rows[i+1][j],rows[i+1][j+1],rows[i][j+1],color);
                }
                mesh.Create(transform,rockMaterial,"Alpine canyon outer mountains "+start,true);
            }
        }
        static float MountainHeight(float x,float z)
            => 520+Mathf.PerlinNoise(x*.00028f+73,z*.00028f+41)*750
                +Mathf.PerlinNoise(x*.0009f+11,z*.0009f+29)*230;

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
        public bool CrossedBridgePlane(Vector3 previous, Vector3 current, out Vector3 crossing, bool returning = false)
        {
            Vector3 normal = Vector3.Cross(RightAt(BridgeSector), Vector3.up);
            float a = Vector3.Dot(previous - route[BridgeSector], normal), b = Vector3.Dot(current - route[BridgeSector], normal);
            crossing = current;
            if (!MissionRules.CrossesPlane(a,b,returning)) return false;
            crossing = Vector3.Lerp(previous, current, -a / (b - a)); return true;
        }
        public bool UnderBridge(Vector3 point)
        {
            Vector3 relative = point - route[BridgeSector];
            float x = Mathf.Abs(Vector3.Dot(relative, RightAt(BridgeSector)));
            // Route validation checks the actual opening. Aircraft colliders already
            // handle wings/tail and roll; a fixed 8 m inset caused false defeats.
            return MissionRules.WithinBridgeOpening(x, relative.y, BridgeHalfSpan, BridgeSpring, BridgeRise);
        }

        GameObject Primitive(string label, Vector3 p, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
            go.transform.SetParent(transform, false); go.transform.position = p; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true; return go;
        }
        public bool CrossedSector(int sector, Vector3 previous, Vector3 current, bool returning = false)
        {
            Vector3 tangent = Vector3.Cross(RightAt(sector), Vector3.up);
            float before = Vector3.Dot(previous - route[sector], tangent), after = Vector3.Dot(current - route[sector], tangent);
            if (!MissionRules.CrossesPlane(before,after,returning)) return false;
            Vector3 crossing = Vector3.Lerp(previous, current, -before / (after - before));
            if(sector==0 || sector==route.Length-1)
            {
                Vector3 relative=crossing-route[sector];
                bool home=sector==0;
                if(!MissionRules.WithinBridgeOpening(Vector3.Dot(relative,RightAt(sector)),relative.y,
                    home?32:90,home?110:180,home?40:60))return false;
            }
            float floor = FloorAt(crossing.z);
            return crossing.y > floor && Mathf.Abs(Vector3.Dot(crossing - route[sector], RightAt(sector))) < widths[sector] * 1.35f;
        }
    }
}

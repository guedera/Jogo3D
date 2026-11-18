using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRacers
{
    public static class AircraftFactory
    {
        [Serializable] class Model { public Node[] nodes; public int materialCount; }
        [Serializable] class Node
        {
            public string name; public int parent; public Vector3 position; public Quaternion rotation;
            public Vector3 scale; public Part[] parts;
        }
        [Serializable] class Part
        {
            public Vector3[] vertices, normals; public Vector2[] uv; public int[] triangles; public int material;
        }

        public static GameObject Create(string json, Material[] materials, Material exhaustMat)
        {
            var model = JsonUtility.FromJson<Model>(json);
            var root = new GameObject("F18 Player"); root.layer = 2; // Ignore Raycast, only for chase camera casts.
            var visual = new GameObject("F18 Visual").transform; visual.SetParent(root.transform, false);
            var nodes = new Transform[model.nodes.Length];
            for (int i = 0; i < nodes.Length; i++) nodes[i] = new GameObject(model.nodes[i].name).transform;
            for (int i = 0; i < nodes.Length; i++)
            {
                var data = model.nodes[i]; var node = nodes[i];
                node.SetParent(data.parent < 0 ? visual : nodes[data.parent], false);
                node.localPosition = data.position; node.localRotation = data.rotation; node.localScale = data.scale;
                for (int j = 0; j < data.parts.Length; j++)
                {
                    var part = data.parts[j];
                    var mesh = new Mesh { name = $"F18_{i}_{j}", vertices = part.vertices, uv = part.uv, triangles = part.triangles };
                    if (part.normals.Length == part.vertices.Length) mesh.normals = part.normals; else mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    var child = new GameObject("Geometry", typeof(MeshFilter), typeof(MeshRenderer));
                    child.transform.SetParent(node, false); child.GetComponent<MeshFilter>().sharedMesh = mesh;
                    child.GetComponent<MeshRenderer>().sharedMaterial = materials[part.material];
                }
            }
            // glTF was mirrored on Z, so the actual model nose points along Unity +Z.
            var bounds = BoundsOf(visual);
            float scale = 18f / bounds.size.z;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = -bounds.center * scale;
            bounds = BoundsOf(visual);
            var rb = root.AddComponent<Rigidbody>(); rb.mass = 12000; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var fuselage = root.AddComponent<BoxCollider>(); fuselage.size = new Vector3(2.4f, 2.1f, bounds.size.z); fuselage.center = new Vector3(0, -.4f, 0);
            foreach (int part in new[] { 20, 32, 34, 36, 42, 46 })
            {
                var b = BoundsOf(nodes[part]);
                var collider = root.AddComponent<BoxCollider>(); collider.center = b.center;
                collider.size = new Vector3(Mathf.Max(.3f, b.size.x), Mathf.Max(.5f, b.size.y), Mathf.Max(.3f, b.size.z));
            }
            root.AddComponent<FlightInput>(); root.AddComponent<FlightController>(); root.AddComponent<FlightEffects>();
            var surfaces = root.AddComponent<AircraftVisuals>();
            var mapped = new List<AircraftVisuals.Surface>();
            // Explicit node IDs from inspection of the supplied hierarchy.
            AddSurface(mapped, nodes[34], Vector3.right, FlightSurfaceRole.Stabilator, 1, 120);
            AddSurface(mapped, nodes[36], Vector3.right, FlightSurfaceRole.Stabilator, -1, 120);
            // The flap meshes have modelling origins, not hinge pivots. Endpoints
            // below are on their actual forward edges in mesh-local coordinates.
            AddHingedSurface(mapped, nodes[12], new Vector3(-.2625f,.15625f,-.04375f), new Vector3(.1625f,.15625f,0), FlightSurfaceRole.Aileron, -1);
            AddHingedSurface(mapped, nodes[28], new Vector3(-.1625f,.15625f,0), new Vector3(.2625f,.15625f,-.04375f), FlightSurfaceRole.Aileron, 1);
            AddHingedSurface(mapped, nodes[10], new Vector3(-.5f,.15625f,-.36836f), new Vector3(.21875f,.15625f,-.24795f), FlightSurfaceRole.TrailingFlap, -1);
            AddHingedSurface(mapped, nodes[22], new Vector3(-.21875f,.15625f,-.24795f), new Vector3(.5f,.15625f,-.36836f), FlightSurfaceRole.TrailingFlap, 1);
            // Use the swept, canted rudder hinge rather than a world-vertical axis.
            AddHingedSurface(mapped, nodes[40], new Vector3(-.0121f,.00037f,.06343f), new Vector3(-.02713f,.3918f,-.07003f), FlightSurfaceRole.Rudder, 1);
            AddHingedSurface(mapped, nodes[44], new Vector3(.0121f,.00037f,.06343f), new Vector3(.02713f,.3918f,-.07003f), FlightSurfaceRole.Rudder, -1);
            // Braking mixes the available surfaces; no isolated dorsal speedbrake
            // or separate leading-edge flap exists in this asset.
            // Fold hinges (nodes 15 and 31) remain at the extended rest pose.
            surfaces.surfaces = mapped.ToArray();
            surfaces.exhausts = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                // Nodes 5/6 are the actual nozzle cylinders. Derive anchors after scaling.
                var nozzle = BoundsOf(nodes[5 + i]);
                var anchor = new GameObject("Nozzle anchor " + i).transform;
                anchor.SetParent(root.transform, false);
                anchor.localPosition = new Vector3(nozzle.center.x, nozzle.center.y, nozzle.min.z + .03f);
                float diameter = Mathf.Min(nozzle.size.x, nozzle.size.y) * .88f;
                anchor.localScale = new Vector3(diameter, diameter, 1);
                var glow = new GameObject("Exhaust plume " + i, typeof(MeshFilter), typeof(MeshRenderer));
                glow.transform.SetParent(anchor, false);
                glow.GetComponent<MeshFilter>().sharedMesh = ExhaustMesh(i);
                glow.GetComponent<Renderer>().sharedMaterial = exhaustMat; surfaces.exhausts[i] = glow.transform;
                glow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                glow.GetComponent<Renderer>().receiveShadows = false;
            }
            foreach (Transform t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
            Debug.Log($"F18 bounds: {bounds.size}; import scale: {scale:F3}");
            return root;
        }
        static void AddSurface(List<AircraftVisuals.Surface> list, Transform part, Vector3 axis, FlightSurfaceRole role, int side, float rate)
        { list.Add(new AircraftVisuals.Surface { part = part, axis = axis.normalized, neutral = part.localRotation, role = role, side = side, degreesPerSecond = rate }); }
        static void AddHingedSurface(List<AircraftVisuals.Surface> list, Transform mesh, Vector3 first, Vector3 second, FlightSurfaceRole role, int side)
        {
            var hinge = new GameObject(role + (side < 0 ? " left hinge" : " right hinge")).transform;
            hinge.SetParent(mesh.parent, false);
            hinge.SetPositionAndRotation(mesh.TransformPoint((first + second) * .5f), mesh.rotation);
            // Preserve every vertex in the neutral pose, then rotate just this piece.
            mesh.SetParent(hinge, true);
            AddSurface(list, hinge, (second - first).normalized, role, side, role == FlightSurfaceRole.TrailingFlap ? 65 : 120);
        }
        static Mesh ExhaustMesh(int engine)
        {
            const int sides = 16, rings = 8;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int r = 0; r < rings; r++)
            for (int s = 0; s <= sides; s++)
            {
                float t = (float)r / (rings - 1), angle = s * Mathf.PI * 2 / sides;
                float radius = .5f * Mathf.Pow(1 - t, .6f);
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -t));
                uv.Add(new Vector2((float)s / sides, t));
                if (r == rings - 1 || s == sides) continue;
                int a = r * (sides + 1) + s, b = a + sides + 1;
                indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            var mesh = new Mesh { name = "F18_Exhaust_" + engine };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
        static Bounds BoundsOf(Transform transform)
        {
            var all = transform.GetComponentsInChildren<Renderer>(); var b = all[0].bounds;
            foreach (var r in all) b.Encapsulate(r.bounds); return b;
        }
    }
}

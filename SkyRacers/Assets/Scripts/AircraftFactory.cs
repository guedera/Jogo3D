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
            root.AddComponent<FlightInput>(); root.AddComponent<FlightController>();
            var surfaces = root.AddComponent<AircraftVisuals>();
            var mapped = new List<AircraftVisuals.Surface>();
            // Explicit node IDs from inspection of the supplied hierarchy.
            AddSurface(mapped, nodes[34], Vector3.right, -20, 17, 0, 12);
            AddSurface(mapped, nodes[36], Vector3.right, -20, -17, 0, 12);
            AddSurface(mapped, nodes[41], Vector3.up, 0, 0, 24, -23);
            AddSurface(mapped, nodes[45], Vector3.up, 0, 0, 24, 23);
            // The model has no isolated dorsal speedbrake. Differential rudder and
            // stabilator deflection represents the F18's surface braking visually.
            // Fold hinges (nodes 15 and 31) remain at the extended rest pose.
            surfaces.surfaces = mapped.ToArray();
            surfaces.exhausts = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere); glow.name = "Engine glow " + i;
                var collision = glow.GetComponent<Collider>();
                collision.enabled = false;
                if (Application.isPlaying) UnityEngine.Object.Destroy(collision); else UnityEngine.Object.DestroyImmediate(collision);
                glow.transform.SetParent(root.transform, false); glow.transform.localPosition = new Vector3(i == 0 ? -.85f : .85f, -.1f, -8.2f);
                glow.GetComponent<Renderer>().sharedMaterial = exhaustMat; surfaces.exhausts[i] = glow.transform;
                glow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (Transform t in root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
            Debug.Log($"F18 bounds: {bounds.size}; import scale: {scale:F3}");
            return root;
        }
        static void AddSurface(List<AircraftVisuals.Surface> list, Transform part, Vector3 axis, float pitch, float roll, float yaw, float brake)
        { list.Add(new AircraftVisuals.Surface { part = part, axis = axis, neutral = part.localRotation, pitch = pitch, roll = roll, yaw = yaw, brake = brake }); }
        static Bounds BoundsOf(Transform transform)
        {
            var all = transform.GetComponentsInChildren<Renderer>(); var b = all[0].bounds;
            foreach (var r in all) b.Encapsulate(r.bounds); return b;
        }
    }
}

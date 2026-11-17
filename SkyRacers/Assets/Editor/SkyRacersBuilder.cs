using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SkyRacers.Editor
{
    public static class SkyRacersBuilder
    {
        public const string ScenePath = "Assets/Scenes/CanyonRun.unity";
        const string Generated = "Assets/Game";
        [MenuItem("SkyRacers/1 - Build Game Scene")]
        public static void BuildGame()
        {
            Directory.CreateDirectory(Generated + "/Meshes");
            Directory.CreateDirectory(Generated + "/Materials");
            Directory.CreateDirectory(Generated + "/Prefabs");
            AssetDatabase.Refresh();
            var prefab = BuildAircraft();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Late afternoon sun", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(42, -28, 0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(1, .88f, .72f); light.intensity = 1.65f;
            light.shadows = LightShadows.Soft;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.44f, .57f, .64f);
            RenderSettings.ambientEquatorColor = new Color(.49f, .41f, .33f);
            RenderSettings.ambientGroundColor = new Color(.25f, .21f, .18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.68f, .70f, .66f); RenderSettings.fogDensity = .00065f;
            var course = new GameObject("Canyon 01 - fixed course").AddComponent<CanyonCourse>();
            course.rockMaterial = MaterialAsset("Canyon Rock", Shader.Find("SkyRacers/Canyon Rock"), Color.white);
            course.markerMaterial = MaterialAsset("Course markers", Shader.Find("Universal Render Pipeline/Lit"), new Color(.85f, .96f, .35f));
            course.markerMaterial.EnableKeyword("_EMISSION"); course.markerMaterial.SetColor("_EmissionColor", new Color(.35f, .48f, .07f));
            course.darkMaterial = MaterialAsset("Beacon steel", Shader.Find("Universal Render Pipeline/Lit"), new Color(.10f, .14f, .14f));
            course.Generate();
            foreach (var filter in course.GetComponentsInChildren<MeshFilter>())
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh)))
                {
                    filter.sharedMesh = SaveMesh(filter.sharedMesh);
                    var collider = filter.GetComponent<MeshCollider>();
                    if (collider != null) collider.sharedMesh = filter.sharedMesh;
                }
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ChaseCamera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.nearClipPlane = .25f; camera.farClipPlane = 3500;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.38f, .57f, .66f);
            camera.fieldOfView = 62;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cameraObject.transform.SetPositionAndRotation(course.Spawn + new Vector3(0, 11, -34), Quaternion.Euler(7, 0, 0));
            var ui = new GameObject("Game Interface", typeof(RectTransform)).AddComponent<GameUI>();
            var run = new GameObject("Game State").AddComponent<RunManager>();
            run.aircraftPrefab = prefab.GetComponent<FlightController>(); run.Course = course;
            run.chaseCamera = cameraObject.GetComponent<ChaseCamera>(); run.ui = ui;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "SkyRacers"; PlayerSettings.productName = "SkyRacers";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            AssetDatabase.SaveAssets();
            Debug.Log("SKYRACERS_BUILD_OK: " + ScenePath + "; course=" + course.TotalDistance.ToString("F0") + "m");
        }

        static GameObject BuildAircraft()
        {
            var materials = new Material[9];
            for (int i = 0; i < materials.Length; i++)
            {
                string texturePath = $"Assets/ThirdParty/F18/Texture_{i}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.SaveAndReimport();
                var material = MaterialAsset("F18_" + i, Shader.Find("Universal Render Pipeline/Lit"), Color.white);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetFloat("_Smoothness", .22f); material.SetFloat("_Cull", 0);
                material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .05f); material.EnableKeyword("_ALPHATEST_ON");
                materials[i] = material;
            }
            var exhaustMat = MaterialAsset("Engine glow", Shader.Find("Universal Render Pipeline/Unlit"), new Color(1, .42f, .12f));
            var root = AircraftFactory.Create(File.ReadAllText("Assets/ThirdParty/F18/aircraft.json"), materials, exhaustMat);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(filter.sharedMesh))) filter.sharedMesh = SaveMesh(filter.sharedMesh);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Generated + "/Prefabs/F18Player.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
        static Material MaterialAsset(string name, Shader shader, Color color)
        {
            if (shader == null) throw new InvalidOperationException("Required shader missing: " + name);
            string path = Generated + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); EditorUtility.SetDirty(material); return material;
        }
        static Mesh SaveMesh(Mesh mesh)
        {
            string path = Generated + "/Meshes/" + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); return existing; }
            return mesh;
        }

        [MenuItem("SkyRacers/2 - Open Game Scene")]
        public static void OpenGame() { EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("SkyRacers/3 - Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = UnityEditor.BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Windows/SkyRacers.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("SKYRACERS_WINDOWS_BUILD_OK");
        }

        public static void BatchBuild()
        {
            BuildGame(); Validate(); BuildWindows();
        }
        [MenuItem("SkyRacers/4 - Validate Course")]
        public static void Validate()
        {
            var course = UnityEngine.Object.FindAnyObjectByType<CanyonCourse>();
            if (course == null) throw new Exception("Course missing");
            Physics.SyncTransforms();
            for (int i = 0; i < course.route.Length; i++)
            {
                if (!Physics.Raycast(course.route[i] + Vector3.up * 65, Vector3.down, out _, 80, 1)) throw new Exception("Floor gap at sector " + i);
                if (i > 0 && !course.CrossedSector(i, course.route[i] - (course.route[i] - course.route[i - 1]).normalized * 4 + Vector3.up * 65,
                    course.route[i] + (course.route[i] - course.route[i - 1]).normalized * 4 + Vector3.up * 65)) throw new Exception("Invalid sector " + i);
            }
            if (course.CrossedSector(1, course.route[1] + Vector3.up * 65 + Vector3.forward * 5, course.route[1] + Vector3.up * 65 - Vector3.forward * 5)) throw new Exception("Reverse sector accepted");
            Debug.Log("SKYRACERS_VALIDATION_OK: floor coverage, sequential gates, reverse rejection; " + course.route.Length + " sectors");
        }
    }
}

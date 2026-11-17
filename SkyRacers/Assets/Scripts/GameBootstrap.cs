using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SkyRacers
{
    /// <summary>Self-contained playable scene. The editor builder can bake this setup for scene editing.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public TextAsset aircraftData;
        public Material[] aircraftMaterials;
        public Material terrainMaterial, markerMaterial, darkMaterial, exhaustMaterial;
        void Awake()
        {
            var course = new GameObject("Canyon 01").AddComponent<CanyonCourse>();
            course.rockMaterial = terrainMaterial; course.markerMaterial = markerMaterial; course.darkMaterial = darkMaterial;
            course.Generate();
            var sun = new GameObject("Afternoon sun", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(42, -28, 0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(1, .88f, .72f); light.intensity = 1.65f; light.shadows = LightShadows.Soft;
            RenderSettings.sun = light; RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.44f, .57f, .64f);
            RenderSettings.ambientEquatorColor = new Color(.49f, .41f, .33f);
            RenderSettings.ambientGroundColor = new Color(.25f, .21f, .18f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.68f, .70f, .66f); RenderSettings.fogDensity = .00065f;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ChaseCamera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.nearClipPlane = .25f; camera.farClipPlane = 3500;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.38f, .57f, .66f); camera.fieldOfView = 62;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var plane = AircraftFactory.Create(aircraftData.text, aircraftMaterials, exhaustMaterial).GetComponent<FlightController>();
            var ui = new GameObject("Game Interface", typeof(RectTransform)).AddComponent<GameUI>();
            var run = new GameObject("Game State").AddComponent<RunManager>();
            run.Course = course; run.aircraftPrefab = plane; run.useSceneAircraft = true;
            run.chaseCamera = cameraObject.GetComponent<ChaseCamera>(); run.ui = ui;
        }
    }
}

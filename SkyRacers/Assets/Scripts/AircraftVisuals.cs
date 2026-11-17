using System;
using UnityEngine;

namespace SkyRacers
{
    public sealed class AircraftVisuals : MonoBehaviour
    {
        [Serializable]
        public class Surface
        {
            public Transform part;
            public Vector3 axis = Vector3.right;
            public float pitch, roll, yaw, brake;
            public Quaternion neutral = Quaternion.identity;
            [NonSerialized] public float angle;
        }
        public Surface[] surfaces = Array.Empty<Surface>();
        public Transform[] exhausts = Array.Empty<Transform>();
        FlightController flight;
        AudioSource engine;
        AudioClip engineClip;

        void Start()
        {
            flight = GetComponent<FlightController>();
            engine = gameObject.AddComponent<AudioSource>();
            // Self-generated engine loop; no external audio assets.
            const int rate = 22050;
            var samples = new float[rate * 2];
            var random = new System.Random(18);
            float noise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                noise = Mathf.Lerp(noise, (float)random.NextDouble() * 2 - 1, .12f);
                float envelope = Mathf.Min(1, Mathf.Min(i, samples.Length - 1 - i) / 220f);
                samples[i] = (.22f * Mathf.Sin(t * 2 * Mathf.PI * 70) + .08f * Mathf.Sin(t * 2 * Mathf.PI * 140) + noise * .55f) * envelope;
            }
            engineClip = AudioClip.Create("Generated turbine", samples.Length, 1, rate, false);
            engineClip.SetData(samples, 0); engine.clip = engineClip;
            engine.loop = true; engine.spatialBlend = 0; engine.volume = 0; engine.Play();
        }

        void LateUpdate()
        {
            if (flight == null) return;
            bool running = flight.Run != null && flight.Run.State == RunState.Running;
            foreach (var s in surfaces)
            {
                if (s.part == null) continue;
                var a = flight.Input.Axes;
                float target = a.x * s.pitch + a.z * s.roll + a.y * s.yaw + flight.Airbrake * s.brake;
                s.angle = Mathf.Lerp(s.angle, target, 1 - Mathf.Exp(-10 * Time.deltaTime));
                s.part.localRotation = s.neutral * Quaternion.AngleAxis(s.angle, s.axis);
            }
            foreach (var exhaust in exhausts)
            {
                exhaust.gameObject.SetActive(running);
                exhaust.localScale = new Vector3(.55f, .55f, 1.2f + flight.Throttle * 3.2f);
            }
            engine.volume = running ? .08f + .12f * flight.Throttle : 0;
            engine.pitch = .65f + flight.Throttle * .8f + flight.Speed * .002f;
        }
        public void ResetSurfaces()
        {
            foreach (var s in surfaces) if (s.part != null) { s.angle = 0; s.part.localRotation = s.neutral; }
        }
        void OnDestroy() { if (engineClip != null) Destroy(engineClip); }
    }
}

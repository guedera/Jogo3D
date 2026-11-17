using UnityEngine;

namespace SkyRacers
{
    [RequireComponent(typeof(Rigidbody), typeof(FlightInput))]
    public sealed class FlightController : MonoBehaviour
    {
        [Header("Flight tuning (metres / seconds)")]
        public float engineAcceleration = 24;
        public float afterburnerAcceleration = 16;
        public float drag = .00030f;
        public float brakeDrag = .00085f;
        public float throttleRate = .35f;
        public Vector3 turnRates = new Vector3(38, 17, 95);
        public float velocityAlignment = 1.8f;
        public float initialSpeed = 170;
        public float initialThrottle = .62f;
        public Rigidbody Body { get; private set; }
        public FlightInput Input { get; private set; }
        public RunManager Run { get; set; }
        public float Throttle { get; private set; }
        public float Speed => Body == null ? 0 : Body.linearVelocity.magnitude;
        public float Airbrake { get; private set; }
        public bool Afterburner => Throttle >= .995f;
        // Fixed reference for this arcade atmosphere, not a full atmospheric model.
        public float Mach => Speed / 340f;
        public float HighestPoint
        {
            get
            {
                float top = transform.position.y;
                if (hull != null) foreach (var c in hull) if (c.enabled) top = Mathf.Max(top, c.bounds.max.y);
                return top;
            }
        }
        Collider[] hull;

        void Awake()
        {
            Body = GetComponent<Rigidbody>(); Input = GetComponent<FlightInput>();
            Body.mass = 12000;
            Body.useGravity = true;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = 3;
            hull = GetComponentsInChildren<Collider>();
        }

        public void ResetFlight(Vector3 position, Quaternion rotation)
        {
            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.position = position; Body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            Body.isKinematic = true;
            Input.Clear(); Throttle = initialThrottle; Airbrake = 0;
            GetComponent<AircraftVisuals>()?.ResetSurfaces();
        }
        public void Launch() { Body.isKinematic = false; Body.linearVelocity = transform.forward * initialSpeed; }
        public void Freeze()
        {
            Input.Clear();
            if (!Body.isKinematic) { Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; }
            Body.isKinematic = true;
        }

        void FixedUpdate()
        {
            if (Run == null || Run.State != RunState.Running) return;
            var dt = Time.fixedDeltaTime;
            Throttle = Mathf.Clamp01(Throttle + Input.ThrottleChange * throttleRate * dt);
            Airbrake = Mathf.MoveTowards(Airbrake, Input.Braking ? 1 : 0, dt * 4);
            float speed = Speed;
            float authority = Mathf.Lerp(.2f, 1f, Mathf.InverseLerp(24, 65, speed));
            var targetAngular = transform.TransformDirection(Vector3.Scale(Input.Axes, turnRates)) * Mathf.Deg2Rad * authority;
            // A bank creates a coordinated turn, while manual yaw remains available.
            float bank = Vector3.Dot(transform.right, Vector3.down);
            targetAngular += Vector3.up * bank * 27 * Mathf.Deg2Rad * authority;
            Body.angularVelocity = Vector3.Lerp(Body.angularVelocity, targetAngular, 1 - Mathf.Exp(-5 * dt));
            Vector3 velocity = Body.linearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            var lateral = velocity - transform.forward * forwardSpeed;
            float lift = 9.81f * Mathf.Pow(Mathf.InverseLerp(22, 60, speed), 2);
            float thrust = engineAcceleration * Throttle + (Afterburner ? afterburnerAcceleration : 0);
            Body.AddForce(transform.forward * thrust
                - velocity * speed * (drag + Airbrake * brakeDrag)
                - lateral * velocityAlignment * authority
                + transform.up * lift, ForceMode.Acceleration);
            if (HighestPoint >= Run.Course.CeilingAt(transform.position)) Run.Fail("Você ultrapassou o topo do cânion.");
            else if (transform.position.y < Run.Course.FloorAt(transform.position.z) - 20) Run.Fail("O avião atingiu o solo.");
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Run != null && Run.State == RunState.Running) Run.Fail("Colisão com o cânion.");
        }
    }
}

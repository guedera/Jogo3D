using UnityEngine;

namespace SkyRacers
{
    [RequireComponent(typeof(Rigidbody), typeof(FlightInput))]
    public sealed class FlightController : MonoBehaviour
    {
        [Header("Flight tuning (metres / seconds)")]
        public float engineAcceleration = 18;
        public float afterburnerAcceleration = 10;
        public float wingArea = 37.2f;
        public float throttleRate = .35f;
        public Vector3 turnRates = new Vector3(75, 24, 150);
        public Vector3 angularAcceleration = new Vector3(480, 240, 720);
        public float initialSpeed = 170;
        public float initialThrottle = .62f;
        public Rigidbody Body { get; private set; }
        public FlightInput Input { get; private set; }
        public RunManager Run { get; set; }
        public float Throttle { get; private set; }
        public float Speed => Body == null ? 0 : Body.linearVelocity.magnitude;
        public float Airbrake { get; private set; }
        public float LoadFactor { get; private set; }
        public float AngleOfAttack { get; private set; }
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
            Body.linearDamping = 0; Body.angularDamping = 0;
            Body.maxAngularVelocity = 6;
            hull = GetComponentsInChildren<Collider>();
        }

        public void ResetFlight(Vector3 position, Quaternion rotation)
        {
            GetComponent<FlightEffects>()?.ResetEffects();
            Body.isKinematic = false;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.position = position; Body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            Body.isKinematic = true;
            Input.Clear(); Throttle = initialThrottle; Airbrake = 0; LoadFactor = 1; AngleOfAttack = 0;
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
            Vector3 velocity = Body.linearVelocity;
            // Use the physics pose: transform can still contain the interpolated
            // render pose, introducing a frame of feedback delay in fast manoeuvres.
            Quaternion rotation = Body.rotation;
            Vector3 forward = rotation * Vector3.forward, right = rotation * Vector3.right;
            Vector3 localVelocity = Quaternion.Inverse(rotation) * velocity;
            Vector3 airflow = speed > .1f ? velocity / speed : forward;
            float alpha = speed > 1 ? Mathf.Atan2(-localVelocity.y, localVelocity.z) : 0;
            float beta = speed > 1 ? Mathf.Atan2(localVelocity.x, localVelocity.z) : 0;
            AngleOfAttack = alpha * Mathf.Rad2Deg;
            float density = FlightDynamics.Density(Body.position.y);
            float pressure = FlightDynamics.PressurePerMass(speed, density, wingArea, Body.mass);
            float lift = FlightDynamics.LiftAcceleration(alpha, pressure);
            LoadFactor = lift / FlightDynamics.Gravity;
            // Lift is perpendicular to airflow. Banking rotates this force and produces
            // the turn naturally; there is no forced velocity alignment or bank/yaw shortcut.
            Vector3 liftDirection = Vector3.Cross(airflow, right).normalized;
            float sideAcceleration = -localVelocity.x * Mathf.Clamp(pressure * .035f, 0, 3);
            float coefficient = lift / Mathf.Max(1, pressure);
            float dragAcceleration = pressure * FlightDynamics.DragCoefficient(coefficient, Mach, Airbrake, beta);
            float thrust = (engineAcceleration * Throttle + (Afterburner ? afterburnerAcceleration : 0))
                * Mathf.Lerp(.45f, 1, density / 1.225f);
            Body.AddForce(forward * thrust - airflow * dragAcceleration
                + liftDirection * lift + right * sideAcceleration, ForceMode.Acceleration);

            // Rate-command fly-by-wire with AoA/G protection and automatic trim
            // only on release. Rate acceleration is independent of the frame rate.
            float authority = Mathf.InverseLerp(25, 110, speed);
            float pitch = FlightDynamics.PitchRate(Input.Axes.x, alpha, lift,
                Vector3.Dot(Physics.gravity, liftDirection), speed, pressure, turnRates.x * Mathf.Deg2Rad);
            float yaw = sideAcceleration / Mathf.Max(35, speed) + beta * 4 + Input.Axes.y * .32f;
            Vector3 localAngular = new Vector3(
                pitch,
                Mathf.Clamp(yaw, -turnRates.y * Mathf.Deg2Rad, turnRates.y * Mathf.Deg2Rad),
                Input.Axes.z * turnRates.z * Mathf.Deg2Rad) * authority;
            Vector3 currentAngular = Quaternion.Inverse(rotation) * Body.angularVelocity;
            Vector3 nextAngular = new Vector3(
                FlightDynamics.StepAngularRate(currentAngular.x, localAngular.x, angularAcceleration.x * Mathf.Deg2Rad, dt),
                FlightDynamics.StepAngularRate(currentAngular.y, localAngular.y, angularAcceleration.y * Mathf.Deg2Rad, dt),
                FlightDynamics.StepAngularRate(currentAngular.z, localAngular.z, angularAcceleration.z * Mathf.Deg2Rad, dt));
            Body.AddTorque(rotation * (nextAngular - currentAngular), ForceMode.VelocityChange);
            if (!Run.IsFreeFlight && HighestPoint >= Run.Course.CeilingAt(transform.position))
                Run.Fail("Você ultrapassou o topo do cânion.");
            else if (!Run.IsFreeFlight && transform.position.y < Run.Course.FloorAt(transform.position.z) - 20)
                Run.Crash();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Run != null && Run.State == RunState.Running) Run.Crash();
        }
    }
}

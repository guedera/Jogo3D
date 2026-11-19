using System;

namespace SkyRacers
{
    // A tunable flight model for this game, not certified F/A-18 performance data.
    // Scalar equations are kept independent of Unity so the flight envelope can be tested.
    public static class FlightDynamics
    {
        public const float Gravity = 9.81f;
        // Game envelope: deliberately more permissive than a literal Hornet FCS.
        public const float MaxPositiveG = 10.5f;
        public const float MaxNegativeG = -5;
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Density(float altitude) => 1.225f * (float)Math.Exp(-Math.Max(0, altitude) / 8500f);
        public static float PressurePerMass(float speed, float density, float area, float mass)
            => .5f * density * speed * speed * area / Math.Max(1, mass);
        public static float RequestedG(float pitchInput)
            => 1 - Clamp(pitchInput, -1, 1) * (pitchInput < 0 ? MaxPositiveG - 1 : 1 - MaxNegativeG);
        public static float TargetAlpha(float load, float pressure)
            => Clamp((load * Gravity / Math.Max(1, pressure) - .18f) / 4.5f, -.40f, .45f);
        public static float PitchRate(float input, float alpha, float lift, float gravityAlongLift, float speed, float pressure, float maxRate)
        {
            float trajectoryRate = -(lift + gravityAlongLift) / Math.Max(35, speed);
            float targetAlpha = TargetAlpha(RequestedG(input), pressure);
            if (Math.Abs(input) < .001f)
                return Clamp(trajectoryRate + (alpha - targetAlpha) * 8, -maxRate, maxRate);
            // Rate command with a predictive AoA/G envelope. Permit a quick initial
            // nose response, then follow the achievable flight-path turn rate.
            float requestedRate = Clamp(input, -1, 1) * maxRate;
            float envelopeRate = trajectoryRate + (alpha - targetAlpha) / .10f;
            float rate = input < 0 ? Math.Max(requestedRate, envelopeRate) : Math.Min(requestedRate, envelopeRate);
            return Clamp(rate, -maxRate, maxRate);
        }
        public const float AngularResponseTime = .035f;
        public static float StepAngularRate(float current, float target, float acceleration, float dt, float responseTime = AngularResponseTime)
        {
            // Just 35 ms of angular lag: soften engagement/release without changing
            // the sustained rate or the available G. Exponential response is timestep-aware.
            float blend = responseTime > 0 ? 1 - (float)Math.Exp(-dt / responseTime) : 1;
            return current + Clamp((target - current) * blend, -acceleration * dt, acceleration * dt);
        }
        public static float LiftCoefficient(float alpha)
        {
            // Linear lift slope, followed by a progressive stall above 27 degrees.
            float attached = .18f + 4.5f * alpha;
            float stalled = .9f * (float)Math.Sin(2 * alpha);
            float blend = Clamp(((float)Math.Abs(alpha) - .471f) / .4f, 0, 1);
            blend = blend * blend * (3 - 2 * blend);
            return attached + (stalled - attached) * blend;
        }
        public static float LiftAcceleration(float alpha, float pressure)
            => Clamp(pressure * LiftCoefficient(alpha), MaxNegativeG * Gravity, MaxPositiveG * Gravity);
        public static float DragCoefficient(float liftCoefficient, float mach, float airbrake, float sideslip)
        {
            float transonic = (mach - 1) / .18f;
            float wave = .015f * (float)Math.Exp(-transonic * transonic) + .008f * Clamp((mach - .9f) / .3f, 0, 1);
            float slip = (float)Math.Sin(sideslip);
            return .052f + .065f * liftCoefficient * liftCoefficient + wave + .20f * airbrake + .8f * slip * slip;
        }
    }
}

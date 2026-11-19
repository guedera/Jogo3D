using System;
using SkyRacers;

// Runs the actual scalar flight model outside Unity; integration below is a
// longitudinal test rig, not a substitute for Rigidbody/collision Play Mode tests.
static class FlightChecks
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    sealed class Rig
    {
        public float vx = 170, vy, altitude = 600, theta, angular, maxLoad, load;
        public float responseTime = FlightDynamics.AngularResponseTime;
        public float Speed => (float)Math.Sqrt(vx * vx + vy * vy);
        public void Step(float pitch, float throttle, float brake, float dt = .02f)
        {
            float speed = Speed, gamma = (float)Math.Atan2(vy, vx);
            float alpha = theta - gamma;
            alpha = (float)Math.Atan2(Math.Sin(alpha), Math.Cos(alpha));
            float density = FlightDynamics.Density(altitude);
            float q = FlightDynamics.PressurePerMass(speed, density, 37.2f, 12000);
            float lift = FlightDynamics.LiftAcceleration(alpha, q);
            load = lift / FlightDynamics.Gravity; maxLoad = Math.Max(load, maxLoad);
            float drag = q * FlightDynamics.DragCoefficient(lift / Math.Max(1, q), speed / 340, brake, 0);
            float thrust = (18 * throttle + (throttle >= .995f ? 10 : 0)) * (.45f + .55f * density / 1.225f);
            float rate = FlightDynamics.PitchRate(pitch, alpha, lift, -9.81f * (float)Math.Cos(gamma), speed, q, 75 * (float)Math.PI / 180);
            rate *= FlightDynamics.Clamp((speed - 25) / 85, 0, 1);
            angular = FlightDynamics.StepAngularRate(angular, rate, 480 * (float)Math.PI / 180, dt, responseTime);
            theta -= angular * dt;
            vx += (thrust * (float)Math.Cos(theta) - drag * (float)Math.Cos(gamma) - lift * (float)Math.Sin(gamma)) * dt;
            vy += (thrust * (float)Math.Sin(theta) - drag * (float)Math.Sin(gamma) + lift * (float)Math.Cos(gamma) - 9.81f) * dt;
            altitude += vy * dt;
            Check(float.IsFinite(Speed) && float.IsFinite(theta), "Non-finite flight state");
        }
        public void Fly(float seconds, float pitch = 0, float throttle = .62f, float brake = 0)
        { for (int i = 0; i < seconds / .02f; i++) Step(pitch, throttle, brake); }
    }
    static void Main()
    {
        Check(FlightDynamics.RequestedG(-1) == 10.5f && FlightDynamics.RequestedG(1) == -5, "Pitch sign / G envelope");
        Check(FlightDynamics.LiftCoefficient(.35f) > FlightDynamics.LiftCoefficient(.9f), "Stall must reduce lift");
        Check(FlightDynamics.DragCoefficient(1.5f, .6f, 0, 0) > FlightDynamics.DragCoefficient(.2f, .6f, 0, 0), "Turns must cost energy");
        for (int speed = 30; speed <= 600; speed += 10)
        for (int angle = -90; angle <= 90; angle++)
        {
            float a = FlightDynamics.LiftAcceleration(angle * (float)Math.PI / 180,
                FlightDynamics.PressurePerMass(speed, 1.225f, 37.2f, 12000));
            Check(a <= 10.5f * 9.81f + .001f && a >= -5 * 9.81f - .001f, "G limit exceeded");
        }
        var level = new Rig(); level.Fly(20);
        Check(Math.Abs(level.altitude - 600) < 35 && Math.Abs(level.vy) < 4, "Neutral trim drifts excessively");
        var pull = new Rig(); pull.Fly(2, -1, 1);
        Check(pull.theta > .45f && pull.vy > 30 && pull.maxLoad > 5, "Pitch response too sluggish / reversed");
        var push = new Rig(); push.Fly(2, 1, .62f);
        Check(push.theta < -.15f && push.vy < -15 && push.load < 0, "Push response reversed");
        var idle = new Rig(); idle.Fly(5, 0, 0);
        var brakes = new Rig(); brakes.Fly(5, 0, 0, 1);
        Check(brakes.Speed < idle.Speed - 15, "Airbrakes ineffective");
        var burner = new Rig(); burner.Fly(20, 0, 1);
        Check(burner.Speed > 340, "Afterburner cannot reach Mach 1");
        var recovery = new Rig(); recovery.Fly(1, -1, 1); recovery.Fly(8, 0, .62f);
        Check(Math.Abs(recovery.load - 1) < .15f, "Trim fails to recover after releasing pitch");
        Console.WriteLine($"PASS: flight envelope, trim, pitch signs, stall, drag and brakes; level drift {level.altitude - 600:F1} m / 20 s; 2 s pull {pull.theta * 180 / Math.PI:F1} deg; AB {burner.Speed / 340:F2} Mach / 20 s");

        foreach (float initial in new[] { 170f, 340f, 450f })
        {
            var up = new Rig { vx = initial }; up.Fly(2, -1, 1);
            var down = new Rig { vx = initial }; down.Fly(2, 1, 1);
            var immediate = new Rig { vx = initial, responseTime = 0 }; immediate.Fly(2, -1, 1);
            Check(Math.Abs(up.theta-immediate.theta) < Math.Abs(immediate.theta)*.05f, "Inertia must preserve the approved pitch authority");
            Check(up.theta > (initial < 200 ? 1.1f : .4f), "Weak S response at " + initial);
            Check(down.theta < (initial < 200 ? -.5f : -.2f), "Weak W response at " + initial);
            var pulse = new Rig { vx = initial }; pulse.Fly(.2f, -1, .62f);
            Check(pulse.theta > .02f, "Input latency at " + initial);
            var coarse = new Rig { vx = initial };
            for (int i = 0; i < 40; i++) coarse.Step(-1, 1, 0, .05f);
            Check(Math.Abs(coarse.theta - up.theta) < .12f, "Pitch response depends excessively on timestep");
            Console.WriteLine($"PASS: W/S at {initial * 3.6f:F0} km/h: 2 s pull {up.theta * 180 / Math.PI:F1} deg, push {down.theta * 180 / Math.PI:F1} deg");
        }
        var loop = new Rig(); loop.Fly(15, -1, 1);
        Check(float.IsFinite(loop.Speed) && loop.theta > Math.PI, "Sustained pitch cannot pass vertical");
        float residual = FlightDynamics.StepAngularRate(1, 0, 20, .02f);
        Check(residual > .4f && residual < .7f, "Release must leave only a short angular tail");
        for(int i=0;i<10;i++)residual=FlightDynamics.StepAngularRate(residual,0,20,.02f);
        Check(residual < .003f, "Angular inertia must settle promptly after release");
        float onset=FlightDynamics.StepAngularRate(0,1,20,.02f);
        Check(onset >= .39f, "Small inertia must not make input sluggish");
        float reversed=1;
        for(int i=0;i<10;i++)reversed=FlightDynamics.StepAngularRate(reversed,-1,20,.02f);
        Check(reversed < -.98f, "Countersteering must retain authority");
        Console.WriteLine("PASS: subtle angular inertia, prompt release/countersteer and preserved pitch authority (within 5%)");
        foreach (var role in new[] { FlightSurfaceRole.Aileron, FlightSurfaceRole.Stabilator, FlightSurfaceRole.TrailingFlap })
        {
            float left = FlightSurfaceMixer.Deflection(role, -1, 0, 1, 0, 0, 0);
            float right = FlightSurfaceMixer.Deflection(role, 1, 0, 1, 0, 0, 0);
            Check(left > 0 && right < 0 && Math.Abs(left + right) < .001f, "Roll deflection must be differential and correctly signed: " + role);
        }
        Check(FlightSurfaceMixer.Deflection(FlightSurfaceRole.Stabilator, 1, -1, 0, 0, 0, 0) > 0, "S must raise stabilator trailing edge");
        Check(FlightSurfaceMixer.Deflection(FlightSurfaceRole.Stabilator, -1, 1, 0, 0, 0, 0) < 0, "W must lower stabilator trailing edge");
        foreach (int side in new[] { -1, 1 })
            Check(FlightSurfaceMixer.Deflection(FlightSurfaceRole.Rudder, side, 0, 0, 1, 0, 0) < 0, "E must deflect both rudders right");
        Check(FlightSurfaceMixer.AutoFlap(170, 24) > FlightSurfaceMixer.AutoFlap(170, 0), "AoA must schedule flaps");
        foreach (FlightSurfaceRole role in Enum.GetValues(typeof(FlightSurfaceRole)))
        foreach (int side in new[] { -1, 1 })
        {
            Check(Math.Abs(FlightSurfaceMixer.Deflection(role, side, 1, 1, 1, 1, 1)) <= 35, "Surface overtravel");
            Check(FlightSurfaceMixer.Deflection(role, side, 0, 0, 0, 0, 0) == 0, "Surface fails to return to neutral");
        }
        Console.WriteLine("PASS: aileron/flap/stabilator mixing, rudder direction, automatic flaps and travel limits");

        foreach (float step in new[] { 25f, 50f })
        {
            float value = 10 * step;
            Check(HudTape.TickY(value, value, step) == 180, "Readout index misaligned");
            Check(HudTape.TickY(value + step / 2, value, step) == 200, "Tape must slide continuously down as value increases");
            float before = HudTape.TickY(value - .001f, value, step);
            float after = HudTape.TickY(value + .001f, value, step);
            Check(Math.Abs(after - before) < .01f, "Tape jumps at graduation boundary");
            Check(HudTape.TickValue(value, step, HudTape.Rows / 2) == value, "Centre graduation incorrect");
        }
        Console.WriteLine("PASS: HUD tape direction, midpoint alignment and graduation continuity");
    }
}

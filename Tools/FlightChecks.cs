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

        var mission=new MissionProgress(5);
        Check(!mission.Accept(3), "Cannot skip outbound sectors");
        for(int i=1;i<5;i++)Check(mission.Accept(i), "Outbound sector rejected");
        Check(mission.Returning&&!mission.Complete&&mission.NextSector==3, "Far end must start return, never victory");
        Check(!mission.Accept(0), "Cannot skip return sectors");
        for(int i=3;i>=0;i--)Check(mission.Accept(i), "Return sector rejected");
        Check(mission.Complete&&!mission.Accept(0), "Victory only once after reaching home");
        Check(MissionRules.CrossesPlane(-1,1,false)&&!MissionRules.CrossesPlane(-1,1,true), "Outbound direction");
        Check(MissionRules.CrossesPlane(1,-1,true)&&!MissionRules.CrossesPlane(1,-1,false), "Return direction");
        Check(MissionRules.CanAcquire(true,false,true,true,true), "Exposed return aircraft must be targetable");
        Check(MissionRules.CanAcquire(true,false,false,true,true), "Outbound SAM must acquire above the ceiling too");
        Check(!MissionRules.CanAcquire(true,false,true,false,true), "No launches below ceiling");
        Check(!MissionRules.CanAcquire(true,false,true,true,false), "Terrain must mask radar");
        Check(!MissionRules.CanAcquire(true,true,true,true,true), "No SAM in free flight");
        Check(!MissionRules.CanAcquire(false,false,true,true,true), "No SAM while paused/ended");
        var scheduler=new SamLaunchClock();
        Check(!scheduler.Tick(true,1,1,0), "Lock delay must warn before firing");
        Check(scheduler.Tick(true,.6f,1.6f,0), "Sustained exposure must launch");
        Check(!scheduler.Tick(true,.5f,2.1f,1), "Launch cooldown");
        Check(!scheduler.Tick(false,5,7.1f,1), "Descending prevents next launch");
        Check(!scheduler.Tick(true,1,8.1f,1), "New exposure requires reacquisition");
        Check(scheduler.Tick(true,.6f,8.7f,1), "Second exposed interval launches");
        Check(!scheduler.Tick(true,5,13.7f,4), "Bound live missile count");
        var stock=new CountermeasureStock();
        Check(stock.TryUse(0)&&!stock.TryUse(.1f)&&stock.Count==9, "Flare cooldown and one charge per press");
        for(int i=1;i<10;i++)Check(stock.TryUse(i), "Ten flares available");
        Check(stock.Count==0&&!stock.TryUse(20), "No eleventh flare");stock.Reset();
        Check(stock.Count==10&&stock.TryUse(0), "Restart restores stock/cooldown");
        Check(MissionRules.MissileTurnRate(450)<MissionRules.MissileTurnRate(170), "Missile turn radius still increases with speed");
        Check(Math.Abs(MissionRules.MissileTurnRate(340)*340-MissionRules.MissileMaxG*9.81f)<.01f, "Missile lateral acceleration must match new seeker envelope");
        Check(MissionRules.MissileTurnRate(950)>3*FlightDynamics.MaxPositiveG*9.81f/950,
            "Supersonic missile must turn at least three times faster than its old tuning");
        Console.WriteLine("PASS: outbound/return mission, home-only victory, radar gating, periodic launches, terrain masking rules and exactly ten flares");

        // A missile 2 km behind a straight-flying supersonic target must close
        // promptly; integrates the same acceleration used by AirDefense.
        float missileSpeed=MissionRules.MissileLaunchSpeed,gap=2000,catchTime=0;
        while(gap>0&&catchTime<15)
        {
            missileSpeed=MissionRules.StepMissileSpeed(missileSpeed,.02f);
            gap-=(missileSpeed-450)*.02f;catchTime+=.02f;
        }
        Check(gap<=0&&catchTime<7, "SAM must catch a 450 m/s target from 2 km behind within 7 s in straight flight");
        Check(MissionRules.StepMissileSpeed(949,1)==MissionRules.MissileMaxSpeed, "Missile speed capped without overshoot");
        Check(MissionRules.FlarePairs*2==24 && (MissionRules.FlarePairs-1)*MissionRules.FlarePairInterval<MissionRules.FlareCooldown,
            "A charge emits 24 flares in a short salvo before the next charge is available");
        Console.WriteLine($"PASS: 24-flare salvo; SAM straight pursuit closes 2 km at 450 m/s in {catchTime:0.00} s");

        Check(MissionRules.WithinBridgeOpening(3.5f,65,11,408,30), "Valid off-centre bridge passage must not trigger defeat");
        Check(MissionRules.WithinBridgeOpening(-3.5f,300,11,408,30), "Bridge passage accepted on either side");
        Check(MissionRules.WithinBridgeOpening(0,430,11,408,30), "Actual upper arch opening accepted");
        Check(!MissionRules.WithinBridgeOpening(11,65,11,408,30), "Bridge pillar/bypass rejected");
        Check(!MissionRules.WithinBridgeOpening(0,439,11,408,30), "Overflight rejected");
        Check(!MissionRules.WithinBridgeOpening(0,-1,11,408,30), "Below-floor passage rejected");
        Console.WriteLine("PASS: bridge actual opening accepts offset passage without fixed wingspan inset");

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

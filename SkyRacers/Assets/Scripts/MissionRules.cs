using System;

namespace SkyRacers
{
    public sealed class MissionProgress
    {
        public int NextSector { get; private set; } = 1;
        public bool Returning { get; private set; }
        public bool Complete { get; private set; }
        readonly int sectors;
        public MissionProgress(int sectors) { this.sectors=Math.Max(2,sectors); }
        public bool Accept(int sector)
        {
            if(Complete || sector!=NextSector)return false;
            if(Returning) {NextSector--;Complete=NextSector<0;}
            else if(++NextSector>=sectors) {Returning=true;NextSector=sectors-2;}
            return true;
        }
    }
    public static class MissionRules
    {
        public const int FlareCapacity=10, FlarePairs=12;
        public const float FlarePairInterval=.05f;
        public const float MissileLaunchSpeed=300, MissileMaxSpeed=950, MissileAcceleration=240;
        public static float StepMissileSpeed(float speed,float dt)
            => Math.Min(MissileMaxSpeed,speed+MissileAcceleration*Math.Max(0,dt));
        public const float LockSeconds=1.5f, LaunchInterval=4f, FlareCooldown=.6f;
        public static bool WithinBridgeOpening(float lateral,float height,float halfSpan,float spring,float rise)
        {
            float x=Math.Abs(lateral);
            if(x>=halfSpan || height<=0)return false;
            return height<spring+rise*(float)Math.Sqrt(1-x*x/(halfSpan*halfSpan));
        }
        public static bool CanAcquire(bool running,bool freeFlight,bool returning,bool aboveCeiling,bool visible)
            => running && !freeFlight && aboveCeiling && visible;
        public static bool CrossesPlane(float before,float after,bool returning)
            => returning ? before>0&&after<=0 : before<0&&after>=0;
        // More agile game seeker; finite turn radius still permits evasive manoeuvres.
        public const float MissileMaxG=32;
        public static float MissileTurnRate(float speed)
            => Math.Min(100*(float)Math.PI/180, MissileMaxG*FlightDynamics.Gravity/Math.Max(35,speed));
    }
    public sealed class CountermeasureStock
    {
        public int Count {get;private set;}=MissionRules.FlareCapacity;
        float readyAt;
        public void Reset() {Count=MissionRules.FlareCapacity;readyAt=0;}
        public bool TryUse(float now)
        {
            if(Count<=0||now<readyAt)return false;
            Count--;readyAt=now+MissionRules.FlareCooldown;return true;
        }
    }
    public sealed class SamLaunchClock
    {
        float exposure,nextLaunch;
        public void Reset() {exposure=nextLaunch=0;}
        public bool Tick(bool acquire,float dt,float now,int liveMissiles)
        {
            exposure=acquire?exposure+dt:0;
            if(exposure<MissionRules.LockSeconds||now<nextLaunch||liveMissiles>=4)return false;
            nextLaunch=now+MissionRules.LaunchInterval;return true;
        }
    }
}

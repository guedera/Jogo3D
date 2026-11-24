using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRacers
{
    // Cinematic game rules, not a model of a real missile/seeker. Flares stand
    // for the combined countermeasure burst seen in the film (flare + chaff).
    public sealed class AirDefense : MonoBehaviour
    {
        sealed class Decoy { public Transform visual; public Vector3 velocity; public float age; public Light glow; }
        sealed class Salvo { public int pair; public float nextPair; }
        sealed class Missile
        {
            public Transform visual; public Vector3 direction, previousPlane;
            public float speed=MissionRules.MissileLaunchSpeed,age,lost; public Decoy decoy; public bool distracted,blind;
        }
        RunManager run;
        readonly List<Vector3> sites=new List<Vector3>();
        readonly List<Missile> missiles=new List<Missile>();
        readonly List<Decoy> decoys=new List<Decoy>();
        readonly List<Salvo> salvos=new List<Salvo>();
        Transform transient;
        Material ribbon;
        readonly CountermeasureStock stock=new CountermeasureStock();
        readonly SamLaunchClock launchClock=new SamLaunchClock();
        public int Flares => stock.Count;
        public int Incoming => missiles.Count;
        public bool TryGetMissilePosition(int index,out Vector3 position)
        {
            position=Vector3.zero;
            if(index<0||index>=missiles.Count||missiles[index].visual==null)return false;
            position=missiles[index].visual.position;return true;
        }
        public float NearestMissileDistance
        {
            get
            {
                float nearest=float.PositiveInfinity;
                foreach(var missile in missiles)
                    if(missile.visual!=null)nearest=Mathf.Min(nearest,Vector3.Distance(missile.visual.position,run.Aircraft.Body.position));
                return nearest;
            }
        }
        public bool Acquiring { get; private set; }
        public bool Exposed => run!=null&&!run.IsFreeFlight&&run.Aircraft.HighestPoint>run.Course.CeilingAt(run.Aircraft.Body.position);
        public void Initialize(RunManager manager)
        {
            run=manager;
            transient=new GameObject("Missiles and countermeasures").transform;
            transient.SetParent(transform,false);
            ribbon=new Material(FlightEffects.VfxMaterial) {name="Countermeasure trails"};ribbon.SetFloat("_Ribbon",1);
            Physics.SyncTransforms();
            for(int index=60;index<run.Course.route.Length;index+=70)
            {
                Vector3 right=run.Course.RightAt(index)*(index%140==60?1:-1);
                Vector3 point=run.Course.route[index]+right*(run.Course.widths[index]*1.35f+90)+Vector3.up*(run.Course.ceiling+1800);
                if(!Physics.Raycast(point,Vector3.down,out var hit,3000,1,QueryTriggerInteraction.Ignore))continue;
                point=hit.point;sites.Add(point+Vector3.up*13);
                var mesh=new AlpineMesh();mesh.Box(Vector3.up*2,new Vector3(12,4,9),new Color(.21f,.26f,.21f));
                mesh.Box(new Vector3(0,6,0),new Vector3(4,5,3),new Color(.3f,.34f,.29f));
                foreach(int side in new[]{-1,1})mesh.Box(new Vector3(side*3,8,0),new Vector3(2,2,9),new Color(.35f,.39f,.32f));
                var site=mesh.Create(run.Course.transform,run.Course.rockMaterial,"SAM battery "+index,true);site.transform.position=point;
            }
        }
        public void ResetFlight()
        {
            foreach(Transform child in transient) {child.gameObject.SetActive(false);Destroy(child.gameObject);}
            missiles.Clear();decoys.Clear();salvos.Clear();stock.Reset();launchClock.Reset();Acquiring=false;
        }
        public void StopThreats()
        {
            foreach(var missile in missiles)if(missile.visual!=null)Destroy(missile.visual.gameObject);
            missiles.Clear();launchClock.Reset();Acquiring=false;
        }
        public void DeployFlare()
        {
            if(run.State!=RunState.Running||!stock.TryUse(Time.time))return;
            salvos.Add(new Salvo {nextPair=Time.time});
        }
        void EmitFlare(int side,int pair)
        {
            var plane=run.Aircraft;
            Vector3 p=plane.transform.position-plane.transform.forward*6-plane.transform.up+plane.transform.right*(side*2);
            var go=new GameObject("Flare salvo / pair "+pair).transform;go.SetParent(transient,false);go.position=p;
            var ps=FlightEffects.Particles("White hot flare",p,go,new Color(1,.86f,.55f,1),.12f,2.5f,1);
            var main=ps.main;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=20;
            var emission=ps.emission;emission.rateOverTime=45;
            var smoke=FlightEffects.Particles("Flare smoke",p,go,new Color(.74f,.75f,.73f,.32f),1.6f,1.4f,1.5f);
            var smokeEmission=smoke.emission;smokeEmission.rateOverTime=8;smokeEmission.rateOverDistance=.15f;
            var smokeMain=smoke.main;smokeMain.maxParticles=80;
            var trail=go.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=ribbon;trail.time=.65f;trail.startWidth=.8f;trail.endWidth=.05f;
            trail.startColor=new Color(1,.91f,.65f,.95f);trail.endColor=new Color(1,.38f,.06f,0);trail.minVertexDistance=.4f;
            trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
            Light glow=null;
            if(pair==0) {glow=go.gameObject.AddComponent<Light>();glow.color=new Color(1,.56f,.18f);glow.range=24;glow.intensity=4;}
            float fan=pair/(float)(MissionRules.FlarePairs-1);
            decoys.Add(new Decoy {visual=go,glow=glow,velocity=plane.Body.linearVelocity*.72f
                +plane.transform.right*(side*Mathf.Lerp(35,85,fan))
                +plane.transform.up*Mathf.Lerp(-32,22,fan)-plane.transform.forward*(12+pair*2)});
        }
        void Update()
        {
            if(run!=null&&(run.State==RunState.Failed||run.State==RunState.Won))StopThreats();
        }
        void FixedUpdate()
        {
            if(run==null||run.State!=RunState.Running)return;
            float dt=Time.fixedDeltaTime;
            for(int i=salvos.Count-1;i>=0;i--)
            {
                var salvo=salvos[i];
                while(salvo.pair<MissionRules.FlarePairs && Time.time>=salvo.nextPair)
                {
                    EmitFlare(-1,salvo.pair);EmitFlare(1,salvo.pair);
                    salvo.pair++;salvo.nextPair+=MissionRules.FlarePairInterval;
                }
                if(salvo.pair>=MissionRules.FlarePairs)salvos.RemoveAt(i);
            }
            for(int i=decoys.Count-1;i>=0;i--)
            {
                var d=decoys[i];d.age+=dt;
                if(d.age>4) {Destroy(d.visual.gameObject);decoys.RemoveAt(i);continue;}
                d.velocity=(d.velocity+Vector3.down*9.81f*dt)*Mathf.Exp(-.45f*dt);
                d.visual.position+=d.velocity*dt;
                if(d.glow!=null)d.glow.intensity=4*Mathf.Clamp01(1-d.age/3);
                if(d.age>=2.5f && d.age-dt<2.5f)
                {
                    foreach(var ps in d.visual.GetComponentsInChildren<ParticleSystem>())ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
                    d.visual.GetComponent<TrailRenderer>().emitting=false;
                }
            }
            for(int i=missiles.Count-1;i>=0;i--)
            {
                if(!StepMissile(missiles[i],dt))missiles.RemoveAt(i);
                if(run.State!=RunState.Running)break;
            }
            if(run.State!=RunState.Running)return;
            Vector3 site=Vector3.zero;float nearest=float.MaxValue;bool visible=false;
            if(Exposed)
            foreach(var candidate in sites)
            {
                float distance=Vector3.Distance(candidate,run.Aircraft.Body.position);
                if(distance<nearest && distance<6500 && !Physics.Linecast(candidate,run.Aircraft.Body.position,1,QueryTriggerInteraction.Ignore))
                {nearest=distance;site=candidate;visible=true;}
            }
            Acquiring=MissionRules.CanAcquire(true,run.IsFreeFlight,run.Returning,Exposed,visible);
            if(launchClock.Tick(Acquiring,dt,Time.time,missiles.Count))Launch(site);
        }
        void Launch(Vector3 position)
        {
            var mesh=new AlpineMesh();
            mesh.Box(Vector3.zero,new Vector3(.45f,.45f,4.5f),new Color(.84f,.84f,.76f));
            mesh.Box(new Vector3(0,0,-1.4f),new Vector3(2,.08f,1),new Color(.3f,.34f,.31f));
            mesh.Box(new Vector3(0,0,-1.4f),new Vector3(.08f,2,1),new Color(.3f,.34f,.31f));
            var go=mesh.Create(transient,run.Course.rockMaterial,"SAM in flight",false);go.isStatic=false;go.layer=2;
            Vector3 direction=(run.Aircraft.Body.position-position).normalized;
            go.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction));
            var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=ribbon;trail.time=3.2f;trail.minVertexDistance=1;
            trail.startWidth=2.3f;trail.endWidth=5;trail.startColor=new Color(.98f,.95f,.87f,.9f);trail.endColor=new Color(.8f,.82f,.8f,0);
            trail.shadowCastingMode=ShadowCastingMode.Off;
            var plume=FlightEffects.Particles("SAM exhaust",position,go.transform,new Color(1,.55f,.15f,1),.2f,4,2);
            plume.transform.localPosition=Vector3.back*2.3f;var e=plume.emission;e.rateOverTime=50;
            missiles.Add(new Missile {visual=go.transform,direction=direction,previousPlane=run.Aircraft.Body.position});
        }
        bool StepMissile(Missile m,float dt)
        {
            m.age+=dt;Vector3 p=m.visual.position,plane=run.Aircraft.Body.position;
            if(m.age>24) {Destroy(m.visual.gameObject);return false;}
            if(!m.distracted&&!m.blind)
            foreach(var d in decoys)
            {
                float distance=Vector3.Distance(p,d.visual.position);
                if(d.age<2.5f&&distance>90&&distance<850&&Vector3.Dot(m.direction,(d.visual.position-p).normalized)>.65f
                    &&!Physics.Linecast(p,d.visual.position,1,QueryTriggerInteraction.Ignore))
                {m.decoy=d;m.distracted=true;break;}
            }
            Vector3 aim=plane;
            if(m.distracted)
            {
                if(m.decoy.visual!=null)aim=m.decoy.visual.position;else m.blind=true;
            }
            else
            {
                bool blocked=Physics.Linecast(p,plane,1,QueryTriggerInteraction.Ignore);
                m.lost=blocked?m.lost+dt:0;
                if(m.lost>.8f)m.blind=true;
            }
            m.speed=MissionRules.StepMissileSpeed(m.speed,dt);
            if(!m.blind)m.direction=Vector3.RotateTowards(m.direction,(aim-p).normalized,MissionRules.MissileTurnRate(m.speed)*dt,0);
            Vector3 next=p+m.direction*m.speed*dt;
            // Relative swept segment avoids tunnelling past a rapidly moving aircraft.
            Vector3 relativeStart=p-m.previousPlane,relativeEnd=next-plane,delta=relativeEnd-relativeStart;
            float t=delta.sqrMagnitude>0?Mathf.Clamp01(-Vector3.Dot(relativeStart,delta)/delta.sqrMagnitude):0;
            bool hitPlane=m.age>.3f&&(relativeStart+delta*t).sqrMagnitude<49;
            bool hitGround=Physics.SphereCast(p,.7f,m.direction,out var hit,m.speed*dt,1,QueryTriggerInteraction.Ignore);
            bool hitDecoy=m.distracted&&m.decoy.visual!=null&&DistanceToSegment(m.decoy.visual.position,p,next)<8;
            if(hitGround||hitPlane||hitDecoy)
            {
                Detonate(hitGround?hit.point:next);Destroy(m.visual.gameObject);
                if(hitPlane&&(!hitGround||t*m.speed*dt<hit.distance))run.Crash("Impacto de míssil terra-ar.");
                return false;
            }
            m.visual.SetPositionAndRotation(next,Quaternion.LookRotation(m.direction));m.previousPlane=plane;return true;
        }
        static float DistanceToSegment(Vector3 point,Vector3 a,Vector3 b)
        {Vector3 d=b-a;return Vector3.Distance(point,a+d*Mathf.Clamp01(Vector3.Dot(point-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
        void Detonate(Vector3 p)
        {
            var ps=FlightEffects.Particles("SAM detonation",p,transient,new Color(1,.51f,.18f,.9f),.65f,8,18);
            var e=ps.emission;e.enabled=false;ps.Emit(35);Destroy(ps.gameObject,2);
        }
        void OnDestroy() {if(ribbon!=null)Destroy(ribbon);if(transient!=null)Destroy(transient.gameObject);}
    }
}

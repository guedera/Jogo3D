using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRacers
{
    public sealed class FlightEffects : MonoBehaviour
    {
        FlightController plane;
        readonly List<ParticleSystem> smoke = new List<ParticleSystem>();
        readonly List<TrailRenderer> trails = new List<TrailRenderer>();
        readonly List<GameObject> debris = new List<GameObject>();
        MeshRenderer[] pieces;
        Collider[] hull;
        GameObject visual;
        AudioSource alarm, sounds;
        AudioClip alarmClip, crashClip, boomClip;
        Material wingTrailMaterial;
        float previousMach;
        bool broken;
        public static Material VfxMaterial => Resources.Load<Material>("FlightVFX");
        void Start()
        {
            plane=GetComponent<FlightController>();
            visual=transform.Find("F18 Visual").gameObject;
            pieces=visual.GetComponentsInChildren<MeshRenderer>(); hull=GetComponents<Collider>();
            sounds=gameObject.AddComponent<AudioSource>(); sounds.spatialBlend=0;
            alarm=gameObject.AddComponent<AudioSource>(); alarm.loop=true; alarm.volume=.22f;
            alarmClip=Sound("Altitude caution",1,0); crashClip=Sound("Impact",1.8f,1); boomClip=Sound("Sonic boom",.8f,2);
            alarm.clip=alarmClip;
            foreach(var nozzle in GetComponent<AircraftVisuals>().exhausts)
            {
                var ps=Particles("Engine haze",nozzle.parent.position-nozzle.parent.forward*.2f,transform,new Color(.53f,.58f,.62f,.16f),.65f,1.3f,5);
                ps.transform.localRotation=Quaternion.Euler(0,180,0);
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=7;shape.radius=.16f;
                var main=ps.main;main.maxParticles=512;
                var emission=ps.emission;emission.rateOverTime=18;emission.rateOverDistance=.85f;emission.enabled=false;
                smoke.Add(ps);
            }
            wingTrailMaterial=new Material(VfxMaterial) {name="Wing vapour ribbon"};
            wingTrailMaterial.SetFloat("_Ribbon",1);
            Vector3 left=Vector3.zero,right=Vector3.zero;
            var vertices=new List<Vector3>();
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                Vector3 local=transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                vertices.Add(local);
                if(local.x<left.x)left=local;if(local.x>right.x)right=local;
            }
            // Pick the aft end of each wingtip, not whichever extreme vertex is first.
            float leftEdge=left.x,rightEdge=right.x;
            foreach(var vertex in vertices)
            {
                if(vertex.x<=leftEdge+.03f && vertex.z<left.z)left=vertex;
                if(vertex.x>=rightEdge-.03f && vertex.z<right.z)right=vertex;
            }
            foreach(var tip in new[]{left,right})
            {
                var go=new GameObject("Subtle wing vapour");go.transform.SetParent(transform,false);go.transform.localPosition=tip;
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=wingTrailMaterial;trail.time=.5f;trail.minVertexDistance=.5f;
                trail.alignment=LineAlignment.View;trail.textureMode=LineTextureMode.Stretch;
                trail.numCornerVertices=2;trail.numCapVertices=2;
                trail.startWidth=.14f;trail.endWidth=0;trail.startColor=new Color(.86f,.93f,1,.24f);trail.endColor=new Color(.86f,.93f,1,0);
                trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;trail.emitting=false;trails.Add(trail);
            }
        }
        void Update()
        {
            if(plane==null||plane.Run==null)return;
            bool active=plane.Run.State==RunState.Running;
            if(plane.Run.State==RunState.Paused)sounds.Pause();else sounds.UnPause();
            bool paused=plane.Run.State==RunState.Paused;
            foreach(var ps in smoke)
            {
                var e=ps.emission;e.enabled=active&&!broken&&plane.Throttle>.04f;
                if(paused) {if(!ps.isPaused)ps.Pause();}
                else if(active&&!broken) {if(!ps.isPlaying)ps.Play();}
                else ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach(var trail in trails) {trail.emitting=active&&!broken;if(!active&&!paused)trail.Clear();}
            float margin=plane.Run.Course.CeilingAt(transform.position)-plane.HighestPoint;
            bool danger=active&&!plane.Run.IsFreeFlight&&margin<60;
            if(danger) {alarm.pitch=margin<25?1.5f:1;if(!alarm.isPlaying)alarm.Play();}
            else alarm.Stop();
            if(active && previousMach<1 && plane.Mach>=1) SonicBoom();
            previousMach=plane.Mach;
        }
        void SonicBoom()
        {
            sounds.PlayOneShot(boomClip,.48f);
            var ps=Particles("Sonic condensation",transform.position,null,new Color(.84f,.93f,1,.10f),.38f,2,0);
            var e=ps.emission;e.enabled=false;
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64;Vector3 radial=transform.right*Mathf.Cos(a)+transform.up*Mathf.Sin(a);
                var p=new ParticleSystem.EmitParams {position=transform.position+radial*3,velocity=plane.Body.linearVelocity+radial*26,startLifetime=.32f,startSize=1.3f,startColor=new Color(.85f,.94f,1,.10f)};
                ps.Emit(p,1);
            }
            debris.Add(ps.gameObject); Destroy(ps.gameObject,1);
        }
        public void BreakApart(Vector3 velocity)
        {
            if(broken||plane==null)return;broken=true;alarm.Stop();
            var colliders=new List<Collider>();
            foreach(var part in pieces)
            {
                var go=new GameObject("Debris "+part.transform.parent.name,typeof(MeshFilter),typeof(MeshRenderer));
                go.layer=2;go.transform.SetPositionAndRotation(part.transform.position,part.transform.rotation);go.transform.localScale=part.transform.lossyScale;
                var mesh=part.GetComponent<MeshFilter>().sharedMesh;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=part.sharedMaterials;
                var collider=go.AddComponent<BoxCollider>();collider.center=mesh.bounds.center;
                Vector3 size=mesh.bounds.size;collider.size=new Vector3(Mathf.Max(.015f,size.x),Mathf.Max(.015f,size.y),Mathf.Max(.015f,size.z));
                var body=go.AddComponent<Rigidbody>();body.mass=8;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=velocity*.1f+Random.onUnitSphere*18+Vector3.up*8;body.angularVelocity=Random.onUnitSphere*7;
                foreach(var other in colliders)Physics.IgnoreCollision(collider,other);colliders.Add(collider);debris.Add(go);
                Destroy(go,10);
            }
            foreach(var c in hull)c.enabled=false;
            visual.SetActive(false);
            Vector3 point=transform.position;
            Burst("Fireball",point,new Color(1,.38f,.07f,.85f),.65f,9,18,48);
            Burst("Smoke cloud",point,new Color(.16f,.18f,.20f,.6f),3.2f,7,9,38);
            Burst("Hot fragments",point,new Color(1,.75f,.23f,1),1.1f,.3f,45,65);
            var flash=new GameObject("Explosion flash").AddComponent<Light>();flash.transform.position=point;flash.type=LightType.Point;flash.color=new Color(1,.5f,.2f);flash.range=55;flash.intensity=8;Destroy(flash.gameObject,.15f);
            sounds.PlayOneShot(crashClip,.8f);
        }
        void Burst(string name,Vector3 position,Color color,float life,float size,float speed,int count)
        {
            var ps=Particles(name,position,null,color,life,size,speed);var e=ps.emission;e.enabled=false;
            ps.Emit(count);debris.Add(ps.gameObject);Destroy(ps.gameObject,life+1);
        }
        public void ResetEffects()
        {
            broken=false;previousMach=0;
            foreach(var go in debris)if(go!=null)Destroy(go);debris.Clear();
            if(visual!=null)visual.SetActive(true);
            if(hull!=null)foreach(var c in hull)c.enabled=true;
            foreach(var ps in smoke)
            {
                var emission=ps.emission;emission.enabled=false;
                ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach(var t in trails) {t.emitting=false;t.Clear();}
            if(alarm!=null)alarm.Stop();if(sounds!=null)sounds.Stop();
        }
        public static ParticleSystem Particles(string name,Vector3 position,Transform parent,Color color,float life,float size,float speed)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,true);go.transform.position=position;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=5;main.loop=true;main.startLifetime=life;main.startSize=new ParticleSystem.MinMaxCurve(size*.6f,size);main.startSpeed=speed;main.startColor=color;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=300;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.3f;
            var e=ps.emission;e.rateOverTime=0;
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var grow=ps.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,1.7f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=VfxMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            ps.Play();return ps;
        }
        static AudioClip Sound(string name,float seconds,int kind)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(rate*seconds)];var random=new System.Random(kind+81);float noise=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;noise=Mathf.Lerp(noise,(float)random.NextDouble()*2-1,.15f);
                if(kind==0) {float phase=Mathf.Repeat(t,.5f);data[i]=phase<.16f?Mathf.Sin(2*Mathf.PI*880*t)*Mathf.Sin(Mathf.PI*phase/.16f)*.5f:0;}
                else {float pulse=kind==2?Mathf.Exp(-t*16)+(t>.14f?Mathf.Exp(-(t-.14f)*18)*.65f:0):Mathf.Exp(-t*4);data[i]=(noise*.6f+Mathf.Sin(2*Mathf.PI*(kind==2?48:65)*t)*.35f)*pulse;}
            }
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy(){if(wingTrailMaterial!=null)Destroy(wingTrailMaterial);if(alarmClip!=null)Destroy(alarmClip);if(crashClip!=null)Destroy(crashClip);if(boomClip!=null)Destroy(boomClip);foreach(var go in debris)if(go!=null)Destroy(go);}
    }
}

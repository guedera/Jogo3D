using UnityEngine;
using UnityEngine.Rendering;

namespace SkyRacers
{
    // Static soft billboards: no colliders, lights or per-frame cloud scripts.
    public static class SimpleClouds
    {
        public static void Create(Transform world, bool freeFlight)
        {
            var go=new GameObject("High scattered clouds");
            go.transform.SetParent(world,false);
            var clouds=go.AddComponent<ParticleSystem>();
            clouds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=clouds.main;
            main.loop=false;main.playOnAwake=false;main.startSpeed=0;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startSize3D=true;main.maxParticles=800;
            main.simulationSpeed=1;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=clouds.emission;emission.enabled=false;
            var shape=clouds.shape;shape.enabled=false;
            var renderer=clouds.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=FlightEffects.VfxMaterial;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.sortMode=ParticleSystemSortMode.Distance;
            // Start before emitting, then advance once so Unity builds live particle bounds.
            clouds.Play();
            var random=new System.Random(freeFlight?239:117);
            float Range(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
            int count=freeFlight?65:90;
            for(int i=0;i<count;i++)
            {
                var center=freeFlight
                    ?new Vector3(Range(-9000,9000),Range(1350,1750),Range(-9000,9000))
                    :new Vector3(Range(-900,900),Range(950,1300),-1000+i*500);
                // Guaranteed bank in the initial forward view of each map.
                if(i==0)center=freeFlight?new Vector3(300,1400,-5000):new Vector3(250,1000,2300);
                for(int puff=0;puff<7;puff++)
                {
                    float size=Range(500,800),shade=Range(.88f,1);
                    var particle=new ParticleSystem.EmitParams {
                        position=center+new Vector3(Range(-240,240),Range(-45,65),Range(-150,150)),
                        velocity=Vector3.zero,startLifetime=1000000,
                        startSize3D=new Vector3(size,size*Range(.45f,.65f),size),
                        startColor=new Color(shade*.96f,shade*.98f,shade,Range(.55f,.72f))
                    };
                    clouds.Emit(particle,1);
                }
            }
            clouds.Simulate(.02f,true,false,false);
            clouds.Pause();
            // Explicit bounds also keep the static layer visible after switching modes.
            renderer.localBounds=freeFlight
                ?new Bounds(new Vector3(0,1600,0),new Vector3(22000,4000,22000))
                :new Bounds(new Vector3(0,1500,21000),new Vector3(7000,4000,48000));
        }
    }
}

using UnityEngine;
namespace SkyRacers
{
    public sealed class WaterfallMist : MonoBehaviour
    {
        public Vector3 origin;
        void Start()
        {
            var ps=FlightEffects.Particles("Waterfall spray",origin,transform,new Color(.75f,.88f,.95f,.12f),2.2f,8,4);
            var e=ps.emission;e.rateOverTime=12;
            var shape=ps.shape;shape.radius=6;
        }
    }
}

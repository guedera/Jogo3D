using UnityEngine;

namespace SkyRacers
{
    // Independent 20 x 20 km training range, activated only in free flight.
    public sealed class FreeFlightWorld : MonoBehaviour
    {
        public Vector3 Spawn => new Vector3(0,HeightAt(0,-7500)+420,-7500);
        public static float HeightAt(float x,float z)
        {
            float Hill(float cx,float cz,float size,float height)
                => Mathf.Exp(-((x-cx)*(x-cx)+(z-cz)*(z-cz))/(size*size))*height;
            return 45+Mathf.PerlinNoise(x*.0004f+50,z*.0004f+50)*100
                +Hill(-3200,-1800,1700,780)+Hill(3500,3500,2100,1100)+Hill(-4300,5300,2200,900);
        }
        public void Generate(Material material)
        {
            const int cells=160,chunk=16;const float step=125;
            for(int cz=0;cz<cells;cz+=chunk)
            for(int cx=0;cx<cells;cx+=chunk)
            {
                var ground=new AlpineMesh();var forest=new AlpineMesh();
                Vector3 Point(int x,int z) {float px=-10000+x*step,pz=-10000+z*step;return new Vector3(px,HeightAt(px,pz),pz);}
                for(int z=cz;z<cz+chunk;z++)for(int x=cx;x<cx+chunk;x++)
                {
                    Vector3 a=Point(x,z),b=Point(x,z+1),c=Point(x+1,z+1),d=Point(x+1,z);
                    Color color=a.y>650?new Color(.76f,.81f,.80f):Color.Lerp(new Color(.25f,.34f,.26f),new Color(.43f,.47f,.40f),Mathf.InverseLerp(150,650,a.y));
                    ground.Quad(a,b,c,d,color);
                    if((x*17+z*31)%9==0 && Mathf.Abs(a.x)>700 && a.y<600)forest.Pine(a,22+(x+z)%17,false);
                }
                ground.Create(transform,material,"Alpine canyon training terrain "+cx+" "+cz,true);
                forest.Create(transform,material,"Training forest "+cx+" "+cz,false);
            }
            // Alternating tall rock formations leave several wide routes between them.
            for(int i=0;i<24;i++)
            {
                float x=Mathf.Sin(i*2.4f)*2400,z=-6200+i*510;
                var rock=new AlpineMesh();float h=140+(i%5)*65;
                for(int j=0;j<5;j++)rock.Box(Vector3.up*(h*(j+.5f)/5),new Vector3(100-j*14,h/5,95-j*12),new Color(.4f,.44f,.43f));
                var go=rock.Create(transform,material,"Rock needle "+i,true);
                go.transform.position=new Vector3(x,HeightAt(x,z),z);go.transform.rotation=Quaternion.Euler(0,i*37,0);
            }
            for(int i=0;i<7;i++)
            {
                float x=Mathf.Sin(i*1.2f)*650,z=-5000+i*1700;
                var arch=new AlpineMesh();float radius=65+i%3*15,spring=80+i%2*30;
                arch.Arch(0,radius,spring,55,spring+72,30);
                foreach(int side in new[]{-1,1})arch.Box(new Vector3(side*(radius+15),(spring+72)/2,0),new Vector3(30,spring+72,36),new Color(.48f,.5f,.46f));
                var go=arch.Create(transform,material,"Training stone arch "+i,true);go.transform.position=new Vector3(x,HeightAt(x,z),z);
            }
        }
    }
}

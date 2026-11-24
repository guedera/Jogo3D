using UnityEngine;
using UnityEngine.UI;

namespace SkyRacers
{
    // Vector briefing map: actual canyon route or contours of the training range.
    public sealed class MissionMapGraphic : MaskableGraphic
    {
        CanyonCourse course;
        bool free;
        public void Configure(CanyonCourse value,bool freeFlight) {course=value;free=freeFlight;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float w=rectTransform.rect.width,h=rectTransform.rect.height;
            Color grid=new Color(.19f,.31f,.32f,.45f),ink=new Color(.48f,.68f,.64f,.4f),routeColor=new Color(.97f,.72f,.35f);
            for(float x=0;x<=w;x+=40)Line(vh,new Vector2(x,0),new Vector2(x,h),.7f,grid);
            for(float y=0;y<=h;y+=40)Line(vh,new Vector2(0,y),new Vector2(w,y),.7f,grid);
            if(free)
            {
                const int nx=48,ny=32;
                for(int z=0;z<ny;z++)for(int x=0;x<nx;x++)
                {
                    Vector2[] p={new Vector2(x*w/nx,z*h/ny),new Vector2((x+1)*w/nx,z*h/ny),new Vector2((x+1)*w/nx,(z+1)*h/ny),new Vector2(x*w/nx,(z+1)*h/ny)};
                    float[] heights=new float[4];
                    for(int j=0;j<4;j++)heights[j]=FreeFlightWorld.HeightAt(p[j].x/w*20000-10000,p[j].y/h*20000-10000);
                    for(int level=200;level<=1000;level+=200)
                    {
                        Vector2 first=Vector2.zero;bool pending=false;
                        for(int j=0;j<4;j++)
                        {
                            int k=(j+1)%4;
                            if((heights[j]<level)==(heights[k]<level))continue;
                            Vector2 crossing=Vector2.Lerp(p[j],p[k],(level-heights[j])/(heights[k]-heights[j]));
                            if(pending){Line(vh,first,crossing,1.3f,ink);pending=false;}else{first=crossing;pending=true;}
                        }
                    }
                }
                for(int i=0;i<7;i++)Marker(vh,new Vector2((Mathf.Sin(i*1.2f)*650+10000)/20000*w,(-5000+i*1700+10000)/20000*h),routeColor);
            }
            else if(course!=null)
            {
                Vector2 Project(Vector3 p)=>new Vector2(w*.5f+p.x/5000*w,h*.90f-p.z/course.length*h*.8f);
                for(int i=10;i<course.route.Length;i+=10)
                {
                    Vector2 a=Project(course.route[i-10]),b=Project(course.route[i]);
                    Line(vh,a,b,2.2f,routeColor);Line(vh,a+Vector2.right*7,b+Vector2.right*7,1,new Color(.48f,.8f,.72f,.8f));
                    if(i%140==0)Marker(vh,b+Vector2.left*65,new Color(.9f,.36f,.25f,.8f));
                }
                Marker(vh,Project(course.route[0]),Color.white);Marker(vh,Project(course.route[course.route.Length-1]),routeColor);
                Vector2 bridge=Project(course.route[course.BridgeSector]);Line(vh,bridge+Vector2.left*18,bridge+Vector2.right*18,4,Color.white);
            }
            Line(vh,new Vector2(w-35,18),new Vector2(w-35,62),1.5f,Color.white);
            Line(vh,new Vector2(w-51,40),new Vector2(w-19,40),1.5f,Color.white);
        }
        static void Marker(VertexHelper vh,Vector2 p,Color c)
        {
            Vector2 a=p+Vector2.up*5,b=p+Vector2.right*5,d=p+Vector2.down*5,e=p+Vector2.left*5;
            Line(vh,a,b,1.5f,c);Line(vh,b,d,1.5f,c);Line(vh,d,e,1.5f,c);Line(vh,e,a,1.5f,c);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            Vector2 d=(b-a).normalized,n=new Vector2(-d.y,d.x)*width*.5f;
            int start=vh.currentVertCount;
            foreach(var p in new[]{a-n,a+n,b+n,b-n})vh.AddVert(new Vector3(p.x,-p.y,0),c,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE (10.11) and owner 17:38-17:40 ("A drogi też zrobiłeś wszędzie zaokrąglone
// zakręty?", "łatwiej się wtedy jeździ i autobus będzie miał lepiej"): a drive that leaves a city road crosses the
// sidewalk as a real street mouth. The city kerb is open over it (layout.DRIVE_MOUTHS, build_infrastructure.in_mouth),
// the two R x R sidewalk corners are cut from the foundation (layout.*_MOUTH_CORNERS) and built here:
//   - the drive rises from the road (0) to the sidewalk (.16) over R, then to the plot's own drive level at the far
//     edge of the sidewalk, between flush granite edging stones whose tops follow it,
//   - each corner: an asphalt fan rising with the drive, a granite kerb on the arc flush with the sidewalk, the sidewalk
//     inside the arc.
// u runs along the road, v from the road edge into the plot. Shared by the arena A1 car parks and Hotel Sobczak T03
// (the casino T11 has the same construction in BraxChampionsCasinoT11Street).
public static class BraxChampionsDriveMouth
{
    public const float SidewalkY=.16f,Edging=.25f,KerbWidth=.18f;
    public static int Mouths{get;private set;}
    public static int Corners{get;private set;}
    static int serial;

    public struct Spec
    {
        public string id;
        public bool alongX;        // the road runs along local x
        public float edge;         // the road edge's local coordinate across the road
        public float dir;          // +1: the plot lies towards +v in local coordinates, -1 towards -v
        public float u0,u1;        // the drive between these along the road
        public float R;            // corner radius
        public float depth;        // sidewalk depth (where the plot's own drive takes over)
        public float topEnd;       // the drive's height at the far edge of the sidewalk
        public float unit;         // K when the parent is not scaled, 1 when it is
        public string assetDir;
        public Vector3 P(float u,float y,float v)=>(alongX?new Vector3(u,y,edge+dir*v):new Vector3(edge+dir*v,y,u))*unit;
        public float H(float v)=>v<=R?SidewalkY*Mathf.Max(0f,v)/R:SidewalkY+(topEnd-SidewalkY)*Mathf.Clamp01((v-R)/(depth-R));
    }

    public static void Build(Transform parent,Spec s,Material asphalt,Material walk,Material kerb)
    {
        if(s.u1<=s.u0||s.R<=0f||s.depth<=s.R)throw new ArgumentException("Invalid drive mouth "+s.id);
        var drive=new List<Vector3>();
        // ramp over R, full width (the corner fans meet its sides)
        Up(drive,s.P(s.u0,0f,0f),s.P(s.u1,0f,0f),s.P(s.u1,s.H(s.R),s.R));
        Up(drive,s.P(s.u0,0f,0f),s.P(s.u1,s.H(s.R),s.R),s.P(s.u0,s.H(s.R),s.R));
        // across the rest of the sidewalk between the edging stones
        float a=s.u0+Edging,b=s.u1-Edging;
        Up(drive,s.P(a,s.H(s.R),s.R),s.P(b,s.H(s.R),s.R),s.P(b,s.topEnd,s.depth));
        Up(drive,s.P(a,s.H(s.R),s.R),s.P(b,s.topEnd,s.depth),s.P(a,s.topEnd,s.depth));
        Make(parent,s,s.id+" | drive across the sidewalk",asphalt,drive,true);
        var stones=new List<Vector3>();
        foreach(var (e0,e1) in new[]{(s.u0,a),(b,s.u1)})
        {
            int n=Mathf.Max(1,Mathf.CeilToInt((s.depth-s.R)/1f));
            for(int k=0;k<n;k++)
            {
                float v0=s.R+(s.depth-s.R)*k/n+(k>0?.004f:0f),v1=s.R+(s.depth-s.R)*(k+1)/n-(k<n-1?.004f:0f);
                Prism(stones,s,e0,e1,v0,v1,-.10f);
            }
        }
        Make(parent,s,s.id+" | granite edging",kerb,stones,false);
        foreach(float side in new[]{-1f,1f})Corner(parent,s,side,asphalt,walk,kerb);
        Mouths++;
    }

    static void Corner(Transform parent,Spec s,float side,Material asphalt,Material walk,Material kerb)
    {
        const int seg=8;
        float ue=side<0?s.u0:s.u1,R=s.R;
        var C=new Vector2(ue+side*R,R);var P0=new Vector2(ue,0f);
        Vector2 Arc(float r,float t){float th=(-90f+t*(side>0?-90f:90f))*Mathf.Deg2Rad;return C+new Vector2(Mathf.Cos(th),Mathf.Sin(th))*r;}
        Vector3 V(Vector2 q,float y)=>s.P(q.x,y,q.y);
        float Y(Vector2 q)=>SidewalkY*Mathf.Clamp01(q.y/R);
        var fan=new List<Vector3>();var disc=new List<Vector3>();var ring=new List<Vector3>();
        for(int i=0;i<seg;i++)
        {
            float t0=(float)i/seg,t1=(float)(i+1)/seg;
            Vector2 p=Arc(R,t0),q=Arc(R,t1);
            Up(fan,V(P0,0f),V(p,Y(p)),V(q,Y(q)));
            Up(disc,V(C,SidewalkY),V(Arc(R-KerbWidth,t0),SidewalkY),V(Arc(R-KerbWidth,t1),SidewalkY));
            float j=.004f/R;
            Vector2 o0=Arc(R,t0+(i>0?j:0f)),o1=Arc(R,t1-(i<seg-1?j:0f)),i0=Arc(R-KerbWidth,t0+(i>0?j:0f)),i1=Arc(R-KerbWidth,t1-(i<seg-1?j:0f));
            var c=(V(o0,.03f)+V(o1,.03f)+V(i0,.03f)+V(i1,.03f))*.25f;
            Quad(ring,c,V(i0,SidewalkY),V(o0,SidewalkY),V(o1,SidewalkY),V(i1,SidewalkY));
            Quad(ring,c,V(o0,-.10f),V(o1,-.10f),V(o1,SidewalkY),V(o0,SidewalkY));
            Quad(ring,c,V(i1,-.10f),V(i0,-.10f),V(i0,SidewalkY),V(i1,SidewalkY));
            Quad(ring,c,V(i0,-.10f),V(o0,-.10f),V(o0,SidewalkY),V(i0,SidewalkY));
            Quad(ring,c,V(o1,-.10f),V(i1,-.10f),V(i1,SidewalkY),V(o1,SidewalkY));
            Quad(ring,c,V(i0,-.10f),V(i1,-.10f),V(o1,-.10f),V(o0,-.10f));
        }
        Make(parent,s,s.id+" | corner fan "+side,asphalt,fan,true);
        Make(parent,s,s.id+" | corner sidewalk "+side,walk,disc,true);
        Make(parent,s,s.id+" | corner kerb arc "+side,kerb,ring,true);
        Corners++;
    }

    static void Prism(List<Vector3> t,Spec s,float u0,float u1,float v0,float v1,float bottom)
    {
        Vector3 a0=s.P(u0,bottom,v0),a1=s.P(u1,bottom,v0),a2=s.P(u1,bottom,v1),a3=s.P(u0,bottom,v1);
        Vector3 b0=s.P(u0,s.H(v0),v0),b1=s.P(u1,s.H(v0),v0),b2=s.P(u1,s.H(v1),v1),b3=s.P(u0,s.H(v1),v1);
        var c=(a0+a1+a2+a3+b0+b1+b2+b3)/8f;
        Quad(t,c,b0,b3,b2,b1);Quad(t,c,a0,a1,a2,a3);
        Quad(t,c,a0,b0,b1,a1);Quad(t,c,a1,b1,b2,a2);Quad(t,c,a2,b2,b3,a3);Quad(t,c,a3,b3,b0,a0);
    }

    static void Quad(List<Vector3> l,Vector3 centre,Vector3 a,Vector3 b,Vector3 c,Vector3 d){Out(l,centre,a,b,c);Out(l,centre,a,c,d);}
    static void Out(List<Vector3> l,Vector3 centre,Vector3 a,Vector3 b,Vector3 c)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3f-centre)<0f)l.AddRange(new[]{a,c,b});else l.AddRange(new[]{a,b,c});
    }
    static void Up(List<Vector3> l,Vector3 a,Vector3 b,Vector3 c)
    {
        if(Vector3.Cross(b-a,c-a).y<0f)l.AddRange(new[]{a,c,b});else l.AddRange(new[]{a,b,c});
    }

    static void Make(Transform parent,Spec s,string name,Material mat,List<Vector3> t,bool collider)
    {
        var idx=new int[t.Count];for(int i=0;i<idx.Length;i++)idx[i]=i;
        var uv=new Vector2[t.Count];for(int i=0;i<t.Count;i++)uv[i]=new Vector2(t[i].x,t[i].z)/s.unit;
        var mesh=new Mesh{name=name};
        mesh.vertices=t.ToArray();mesh.uv=uv;mesh.triangles=idx;
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,s.assetDir+"/DriveMouth_"+(serial++)+".asset");
        var o=new GameObject(name);o.transform.SetParent(parent,false);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(collider)o.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
    }
}

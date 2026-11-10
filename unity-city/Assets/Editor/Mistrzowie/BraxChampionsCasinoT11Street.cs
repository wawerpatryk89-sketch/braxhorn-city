using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 17:15: "Kasyno nie ma dojazdu z ulicy, już wspominałem". Both drives existed as one graded plane each, but
// at the city the kerb ran straight across the mouth, the corners were square and the asphalt lay up to 16 cm below
// the sidewalk and the lawn beside it with open edges. Each drive is now a real street mouth (layout.T11_DRIVE_MOUTHS,
// T11_MOUTH_CORNERS; the city kerb is open over it, build_infrastructure.in_mouth):
//   - at the road edge the drive rises from road level (0) to the sidewalk (.16) over the corner radius R,
//   - it crosses the sidewalk and the lawn at their level between flush granite edging stones,
//   - 5 m before the plot line it rises to the casino's apron road (.2485) and runs on to the drop-off loop,
//   - both corners turn in R 3.5 m arcs (ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE): an asphalt fan rising with the drive,
//     a granite kerb on the arc flush with the sidewalk, the sidewalk inside it.
public static partial class BraxChampionsCasinoT11
{
    public const float MouthR=3.5f,MouthEdging=.25f,SidewalkY=.16f;
    const float MouthRoadTop=.2485f;
    static int mouthCorners,mouthEdgingStones;

    // u: along the road, v: from the road edge into the plot. alongX: the road runs along local x (road Z330 south of
    // the plot); otherwise along local z (road X400 west of it). edge: the road edge's local coordinate across the road.
    struct Mouth
    {
        public string id;public bool alongX;public float edge,u0,u1,vPlot,vEnd,plotU1;
        public Vector3 P(float u,float y,float v)=>alongX?new Vector3(u,y,edge+v):new Vector3(edge+v,y,u);
        public float H(float v)
        {
            if(v<=MouthR)return SidewalkY*Mathf.Max(0f,v)/MouthR;
            if(v<=vPlot-5f)return SidewalkY;
            if(v<=vPlot)return SidewalkY+(MouthRoadTop-SidewalkY)*(v-(vPlot-5f))/5f;
            return MouthRoadTop;
        }
    }

    static void StreetMouths()
    {
        var cityPaving=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/paving.mat");
        var kerbStone=BraxChampionsFoundation.SourceMaterial("infrastructure_kerb","granite_tile",.9f,new Color(.78f,.79f,.80f));
        var walk=cityPaving?cityPaving:paving;
        // road Z330: edge world z 340 = local -125, drive local x -23.5..-16.5, plot line local z -50, loop -47.5
        StreetMouth(new Mouth{id="south_street_road_connection",alongX=true,edge=-125f,u0=-23.5f,u1=-16.5f,vPlot=75f,vEnd=77.5f,plotU1=-16.5f},walk,kerbStone);
        // road X400: edge world x 410 = local -85, drive local z -48..-37, plot line local x -60, front road -49.5; inside
        // the plot the drive stops at z -37.5, where the west fire lane begins (no asphalt on asphalt)
        StreetMouth(new Mouth{id="left_street_road_connection",alongX=false,edge=-85f,u0=-48f,u1=-37f,vPlot=25f,vEnd=35.5f,plotU1=-37.5f},walk,kerbStone);
        Debug.Log("T11_STREET_MOUTHS 2 cornerArcs="+mouthCorners+" R="+MouthR+" edgingStones="+mouthEdgingStones+
            " cityKerbOpen=layout.T11_DRIVE_MOUTHS rampToSidewalk="+MouthR+"m");
    }

    static void StreetMouth(Mouth m,Material walk,Material kerbStone)
    {
        // the drive: full width over the ramp (the corner fans meet its sides), then between the edging stones
        float[] vs={0f,MouthR,m.vPlot-5f,m.vPlot,m.vEnd};
        for(int i=0;i<vs.Length-1;i++)
        {
            bool edged=i==1||i==2;                                    // across the sidewalk and the lawn
            float a=vs[i],b=vs[i+1],w0=edged?m.u0+MouthEdging:m.u0,w1=edged?m.u1-MouthEdging:i==3?m.plotU1:m.u1;
            if(m.alongX)GradedDrive(shell,m.id+"_"+i,asphalt,w0,w1,m.edge+a,m.edge+b,m.H(a),m.H(b),false);
            else GradedDrive(shell,m.id+"_"+i,asphalt,m.edge+a,m.edge+b,w0,w1,m.H(a),m.H(b),true);
        }
        // flush granite edging along both sides from the ramp to the plot line, its top following the drive
        foreach(var (e0,e1) in new[]{(m.u0,m.u0+MouthEdging),(m.u1-MouthEdging,m.u1)})
            for(int i=1;i<=2;i++)
            {
                float a=vs[i],b=vs[i+1];
                // stones of at most 1 m with 8 mm joints
                int n=Mathf.Max(1,Mathf.CeilToInt((b-a)/1f));
                for(int k=0;k<n;k++)
                {
                    float s0=a+(b-a)*k/n+(k>0?.004f:0f),s1=a+(b-a)*(k+1)/n-(k<n-1?.004f:0f);
                    Prism(m,"T11 drive granite edging "+m.id,kerbStone,e0,e1,s0,s1,m.H(s0),m.H(s1),-.10f);
                    mouthEdgingStones++;
                }
            }
        foreach(float s in new[]{-1f,1f})Corner(m,s,walk,kerbStone);
    }

    // one corner: s=-1 beside u0, s=+1 beside u1
    static void Corner(Mouth m,float s,Material walk,Material kerbStone)
    {
        const int seg=8;const float kerbW=.18f;
        float ue=s<0?m.u0:m.u1;
        var C=new Vector2(ue+s*MouthR,MouthR);var P0=new Vector2(ue,0f);
        Vector2 Arc(float r,float t){float th=(-90f+t*(s>0?-90f:90f))*Mathf.Deg2Rad;return C+new Vector2(Mathf.Cos(th),Mathf.Sin(th))*r;}
        Vector3 V(Vector2 q,float y)=>m.P(q.x,y,q.y)*K;
        float Y(Vector2 q)=>SidewalkY*Mathf.Clamp01(q.y/MouthR);           // the fan rises with the drive
        var fan=new List<Vector3>();var walkTris=new List<Vector3>();var kerb=new List<Vector3>();
        for(int i=0;i<seg;i++)
        {
            float t0=(float)i/seg,t1=(float)(i+1)/seg;
            Vector2 a=Arc(MouthR,t0),b=Arc(MouthR,t1);
            Up(fan,V(P0,0f),V(a,Y(a)),V(b,Y(b)));
            Vector2 c0=Arc(MouthR-kerbW,t0),c1=Arc(MouthR-kerbW,t1);
            Up(walkTris,V(C,SidewalkY),V(c0,SidewalkY),V(c1,SidewalkY));
            // granite stone on the arc, 8 mm joints, top flush with the sidewalk
            float j=.004f/MouthR;
            Vector2 o0=Arc(MouthR,t0+(i>0?j:0f)),o1=Arc(MouthR,t1-(i<seg-1?j:0f)),i0=Arc(MouthR-kerbW,t0+(i>0?j:0f)),i1=Arc(MouthR-kerbW,t1-(i<seg-1?j:0f));
            var sc=(V(o0,.03f)+V(o1,.03f)+V(i0,.03f)+V(i1,.03f))*.25f;          // the stone's own centre
            Quad(kerb,sc,V(i0,SidewalkY),V(o0,SidewalkY),V(o1,SidewalkY),V(i1,SidewalkY));
            Quad(kerb,sc,V(o0,-.10f),V(o1,-.10f),V(o1,SidewalkY),V(o0,SidewalkY));
            Quad(kerb,sc,V(i1,-.10f),V(i0,-.10f),V(i0,SidewalkY),V(i1,SidewalkY));
            Quad(kerb,sc,V(i0,-.10f),V(o0,-.10f),V(o0,SidewalkY),V(i0,SidewalkY));
            Quad(kerb,sc,V(o1,-.10f),V(i1,-.10f),V(i1,SidewalkY),V(o1,SidewalkY));
            Quad(kerb,sc,V(i0,-.10f),V(i1,-.10f),V(o1,-.10f),V(o0,-.10f));
        }
        TriMesh(m.id+" corner fan "+s,asphalt,fan,true);
        TriMesh(m.id+" corner sidewalk "+s,walk,walkTris,true);
        TriMesh(m.id+" corner kerb arc "+s,kerbStone,kerb,true);
        mouthCorners++;
    }

    // a quad of a convex stone: both triangles wound to face away from the stone's centre
    static void Quad(List<Vector3> l,Vector3 centre,Vector3 a,Vector3 b,Vector3 c,Vector3 d){Out(l,centre,a,b,c);Out(l,centre,a,c,d);}
    static void Out(List<Vector3> l,Vector3 centre,Vector3 a,Vector3 b,Vector3 c)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3f-centre)<0f)l.AddRange(new[]{a,c,b});else l.AddRange(new[]{a,b,c});
    }
    // a walkable / drivable triangle facing up
    static void Up(List<Vector3> l,Vector3 a,Vector3 b,Vector3 c)
    {
        if(Vector3.Cross(b-a,c-a).y<0f)l.AddRange(new[]{a,c,b});else l.AddRange(new[]{a,b,c});
    }

    // closed stone with a top sloping along v (8 corners)
    static void Prism(Mouth m,string name,Material mat,float u0,float u1,float v0,float v1,float top0,float top1,float bottom)
    {
        Vector3 V(float u,float y,float v)=>m.P(u,y,v)*K;
        var t=new List<Vector3>();
        Vector3 a0=V(u0,bottom,v0),a1=V(u1,bottom,v0),a2=V(u1,bottom,v1),a3=V(u0,bottom,v1);
        Vector3 b0=V(u0,top0,v0),b1=V(u1,top0,v0),b2=V(u1,top1,v1),b3=V(u0,top1,v1);
        var c=(a0+a1+a2+a3+b0+b1+b2+b3)/8f;
        Quad(t,c,b0,b3,b2,b1);Quad(t,c,a0,a1,a2,a3);
        Quad(t,c,a0,b0,b1,a1);Quad(t,c,a1,b1,b2,a2);Quad(t,c,a2,b2,b3,a3);Quad(t,c,a3,b3,b0,a0);
        TriMesh(name,mat,t,false);
    }

    // triangles in casino-local K space, already wound (Up / Out)
    static void TriMesh(string name,Material mat,List<Vector3> t,bool collider)
    {
        var idx=new int[t.Count];for(int i=0;i<idx.Length;i++)idx[i]=i;
        var mesh=new Mesh{name="T11 "+name};
        mesh.vertices=t.ToArray();
        var uv=new Vector2[t.Count];
        for(int i=0;i<t.Count;i++)uv[i]=new Vector2(t[i].x,t[i].z)/K;
        mesh.uv=uv;mesh.triangles=idx;
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,output+"/Meshes/T11_street_"+(serial++)+".asset");
        var o=new GameObject(name);o.transform.SetParent(shell,false);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(collider)o.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
    }
}

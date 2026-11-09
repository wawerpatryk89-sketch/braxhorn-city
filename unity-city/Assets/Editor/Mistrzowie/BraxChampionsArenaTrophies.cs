using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 9.11: "Zrobisz ładne puchary?" - render f2faacc9 shows lit glass trophy
// cases either side of the entrance doors behind the glass. No CC0 trophy model
// exists on Poly Haven, so the cup is a true surface of revolution of a classic
// championship "loving cup" profile (real proportions: 0.62 m metal on a
// 0.12 m plinth = 0.74 m), polished arena gold, two swept tube handles.
public static partial class BraxChampionsArenaArrival
{
    // (radius, height) in metres, outer wall upward then inner wall back down.
    static readonly Vector2[] CupProfile={
        new Vector2(0f,0f),new Vector2(.090f,0f),new Vector2(.092f,.012f),new Vector2(.084f,.022f),
        new Vector2(.066f,.034f),new Vector2(.040f,.060f),new Vector2(.026f,.090f),new Vector2(.020f,.150f),
        new Vector2(.019f,.200f),new Vector2(.032f,.214f),new Vector2(.034f,.226f),new Vector2(.021f,.240f),
        new Vector2(.024f,.262f),new Vector2(.050f,.288f),new Vector2(.090f,.318f),new Vector2(.124f,.356f),
        new Vector2(.146f,.402f),new Vector2(.157f,.452f),new Vector2(.160f,.505f),new Vector2(.164f,.560f),
        new Vector2(.170f,.588f),new Vector2(.166f,.598f),new Vector2(.156f,.592f),new Vector2(.152f,.560f),
        new Vector2(.148f,.505f),new Vector2(.138f,.452f),new Vector2(.112f,.400f),new Vector2(0f,.372f)};
    const float CupMetalHeight=.598f, CupPlinth=.12f;
    static Mesh cupMesh, handleMesh;

    static void LatheQuad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        void Tri(Vector3 p,Vector3 q,Vector3 r)
        {
            if(Vector3.Dot(Vector3.Cross(q-p,r-p),facing)<0){var s=q;q=r;r=s;}
            int n=v.Count;v.Add(p);v.Add(q);v.Add(r);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        Tri(a,b,c);Tri(a,c,d);
    }

    static Mesh CupMesh()
    {
        if(cupMesh)return cupMesh;
        const int seg=48;
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i+1<CupProfile.Length;i++)
        {
            var p0=CupProfile[i];var p1=CupProfile[i+1];
            float dr=p1.x-p0.x,dy=p1.y-p0.y;
            for(int s=0;s<seg;s++)
            {
                float a0=2f*Mathf.PI*s/seg,a1=2f*Mathf.PI*(s+1)/seg,am=(a0+a1)*.5f;
                Vector3 P(Vector2 p,float a)=>new Vector3(p.x*Mathf.Cos(a),p.y,p.x*Mathf.Sin(a));
                // Profile normal (dy,-dr) rotated about the axis = visible side.
                var facing=new Vector3(dy*Mathf.Cos(am),-dr,dy*Mathf.Sin(am));
                LatheQuad(v,t,P(p0,a0),P(p0,a1),P(p1,a1),P(p1,a0),facing);
            }
        }
        cupMesh=new Mesh{name="A1 championship cup | lathe 48 | real 0.598 m"};
        cupMesh.SetVertices(v);cupMesh.SetTriangles(t,0);
        // Smooth shading: weld by position before normals.
        cupMesh=Weld(cupMesh);
        AssetDatabase.CreateAsset(cupMesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_trophy_cup.asset");
        return cupMesh;
    }

    static Mesh HandleMesh()
    {
        if(handleMesh)return handleMesh;
        // Elongated "ear" handles: ellipse 6 x 7.5 cm, both ends ON the bowl
        // wall (lower end at y .396 / r .146, upper end at y .544 / r .161).
        const int arc=24,ring=10;const float Rx=.060f,Ry=.075f,r=.011f,cx=.156f,cy=.470f;
        var v=new List<Vector3>();var t=new List<int>();
        foreach(float side in new[]{-1f,1f})
            for(int i=0;i<arc;i++)
            {
                float b0=Mathf.Lerp(-100f,100f,i/(float)arc)*Mathf.Deg2Rad,b1=Mathf.Lerp(-100f,100f,(i+1)/(float)arc)*Mathf.Deg2Rad;
                Vector3 C(float b)=>new Vector3(side*(cx+Rx*Mathf.Cos(b)),cy+Ry*Mathf.Sin(b),0);
                Vector3 N(float b)=>new Vector3(side*Mathf.Cos(b)/Rx,Mathf.Sin(b)/Ry,0).normalized;
                for(int k=0;k<ring;k++)
                {
                    float c0=2f*Mathf.PI*k/ring,c1=2f*Mathf.PI*(k+1)/ring;
                    Vector3 Q(float b,float c)=>C(b)+(N(b)*Mathf.Cos(c)+Vector3.forward*Mathf.Sin(c))*r;
                    var mid=C((b0+b1)*.5f);var q=Q((b0+b1)*.5f,(c0+c1)*.5f);
                    LatheQuad(v,t,Q(b0,c0),Q(b1,c0),Q(b1,c1),Q(b0,c1),q-mid);
                }
            }
        handleMesh=new Mesh{name="A1 championship cup | two swept handles"};
        handleMesh.SetVertices(v);handleMesh.SetTriangles(t,0);
        handleMesh=Weld(handleMesh);
        AssetDatabase.CreateAsset(handleMesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_trophy_handles.asset");
        return handleMesh;
    }

    // Merge coincident vertices so RecalculateNormals gives polished, smooth gold.
    static Mesh Weld(Mesh m)
    {
        var src=m.vertices;var tri=m.triangles;
        var map=new Dictionary<Vector3Int,int>();var outV=new List<Vector3>();var outT=new int[tri.Length];
        for(int i=0;i<tri.Length;i++)
        {
            var p=src[tri[i]];var key=new Vector3Int(Mathf.RoundToInt(p.x*20000f),Mathf.RoundToInt(p.y*20000f),Mathf.RoundToInt(p.z*20000f));
            if(!map.TryGetValue(key,out int idx)){idx=outV.Count;outV.Add(p);map[key]=idx;}
            outT[i]=idx;
        }
        var w=new Mesh{name=m.name};w.SetVertices(outV);w.SetTriangles(outT,0);
        w.RecalculateNormals();w.RecalculateBounds();
        var uv=new Vector2[outV.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(outV[i].x+outV[i].z,outV[i].y);
        w.uv=uv;w.RecalculateTangents();
        return w;
    }

    static void Trophy(Transform parent,Vector3 nominalBase,float scale,float yaw,Material gold,Material plinth,string label)
    {
        var g=new GameObject("A1 TROPHY | "+label).transform;g.SetParent(parent,false);
        g.localPosition=nominalBase*K;g.localRotation=Quaternion.Euler(0,yaw,0);g.localScale=Vector3.one*(K*scale);
        // Black plinth 0.20 x 0.12 x 0.20 (scaled with the cup) + thin gold band.
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name="trophy black plinth";
        p.transform.SetParent(g,false);p.transform.localPosition=new Vector3(0,CupPlinth*.5f,0);
        p.transform.localScale=new Vector3(.20f,CupPlinth,.20f);p.GetComponent<Renderer>().sharedMaterial=plinth;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        var band=GameObject.CreatePrimitive(PrimitiveType.Cube);band.name="trophy gold name band";
        band.transform.SetParent(g,false);band.transform.localPosition=new Vector3(0,CupPlinth*.5f,0);
        band.transform.localScale=new Vector3(.205f,.022f,.205f);band.GetComponent<Renderer>().sharedMaterial=gold;
        Object.DestroyImmediate(band.GetComponent<Collider>());
        foreach(var (mesh,name) in new[]{(CupMesh(),"trophy cup"),(HandleMesh(),"trophy handles")})
        {
            var o=new GameObject(name);o.transform.SetParent(g,false);o.transform.localPosition=new Vector3(0,CupPlinth,0);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=gold;
        }
    }

    // Two all-glass lit trophy cases flanking the entrance doors, inside the
    // front glazing: 2.40 x 2.30 x 0.60 m, black plinth .80, glass .80-2.22,
    // black LED cap. Lower level 3 large cups, glass shelf 1.62, 4 small cups.
    static void EntranceTrophyCases(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | lit trophy cases either side of the entrance (owner render)",Vector3.zero);
        var gold=exterior["arena_gold"];var black=exterior["arena_black"];
        var glass=exterior["clear_glass"];var light=exterior["light"];
        const float W=2.40f,D=.60f,baseTop=.80f,glassTop=2.22f,shelf=1.62f;
        int cups=0;
        foreach(float s in new[]{-1f,1f})
        {
            float x=s*8.6f,z=FrontGlassZ(8.6f)+.36f+D*.5f;
            float f=LobbyFloorY(arenaRoot,x,z);
            Box(root,"A1 TROPHY CASE | black lacquer plinth",black,new Vector3(x,f+baseTop*.5f,z),new Vector3(W,baseTop,D),true);
            Box(root,"A1 TROPHY CASE | gold plinth line",gold,new Vector3(x,f+baseTop-.02f,z),new Vector3(W+.01f,.025f,D+.01f),false);
            Box(root,"A1 TROPHY CASE | black LED cap",black,new Vector3(x,f+glassTop+.04f,z),new Vector3(W,.08f,D),false);
            Box(root,"A1 TROPHY CASE | warm LED under cap",light,new Vector3(x,f+glassTop-.006f,z),new Vector3(W-.10f,.012f,D-.10f),false);
            float gh=glassTop-baseTop,gy=f+baseTop+gh*.5f;
            Box(root,"A1 TROPHY CASE | glass front",glass,new Vector3(x,gy,z-D*.5f),new Vector3(W,gh,.012f),true);
            Box(root,"A1 TROPHY CASE | glass back",glass,new Vector3(x,gy,z+D*.5f),new Vector3(W,gh,.012f),true);
            foreach(float e in new[]{-1f,1f})
                Box(root,"A1 TROPHY CASE | glass side",glass,new Vector3(x+e*W*.5f,gy,z),new Vector3(.012f,gh,D),true);
            Box(root,"A1 TROPHY CASE | glass shelf",glass,new Vector3(x,f+shelf,z),new Vector3(W-.03f,.012f,D-.03f),false);
            Box(root,"A1 TROPHY CASE | shelf LED line",light,new Vector3(x,f+shelf-.012f,z-D*.5f+.03f),new Vector3(W-.06f,.010f,.012f),false);
            foreach(float dx in new[]{-.78f,0f,.78f})
            {Trophy(root,new Vector3(x+dx,f+baseTop,z),1f,dx*40f,gold,black,"large championship cup");cups++;}
            foreach(float dx in new[]{-.87f,-.29f,.29f,.87f})
            {Trophy(root,new Vector3(x+dx,f+shelf+.006f,z),.54f,dx*60f,gold,black,"small cup");cups++;}
        }
        Debug.Log("A1_TROPHY_CASES cases=2 cups="+cups+" large=0.74m small=0.40m lathe=48 handles=swept material=arena_gold");
    }
}

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
    // Owner 9.11 "różne wzory nie tylko jeden": four more real trophy shapes.
    static readonly Vector2[] UrnProfile={ // lidded urn with finial
        new Vector2(0f,0f),new Vector2(.085f,0f),new Vector2(.086f,.014f),new Vector2(.060f,.030f),new Vector2(.034f,.060f),
        new Vector2(.030f,.100f),new Vector2(.046f,.118f),new Vector2(.030f,.134f),new Vector2(.060f,.170f),new Vector2(.105f,.220f),
        new Vector2(.130f,.290f),new Vector2(.128f,.350f),new Vector2(.100f,.410f),new Vector2(.070f,.448f),new Vector2(.082f,.470f),
        new Vector2(.090f,.480f),new Vector2(.080f,.492f),new Vector2(.074f,.520f),new Vector2(.052f,.552f),new Vector2(.018f,.572f),
        new Vector2(.016f,.584f),new Vector2(.024f,.596f),new Vector2(.026f,.610f),new Vector2(.018f,.626f),new Vector2(0f,.632f)};
    static readonly Vector2[] GlobeProfile={ // column carrying a globe
        new Vector2(0f,0f),new Vector2(.090f,0f),new Vector2(.090f,.020f),new Vector2(.070f,.032f),new Vector2(.040f,.044f),
        new Vector2(.030f,.060f),new Vector2(.030f,.360f),new Vector2(.040f,.372f),new Vector2(.026f,.390f),new Vector2(.060f,.420f),
        new Vector2(.066f,.432f),new Vector2(.058f,.440f),new Vector2(.075f,.452f),new Vector2(.087f,.476f),new Vector2(.094f,.505f),
        new Vector2(.094f,.535f),new Vector2(.087f,.564f),new Vector2(.075f,.588f),new Vector2(.058f,.606f),new Vector2(.035f,.618f),
        new Vector2(0f,.624f)};
    static readonly Vector2[] BowlProfile={ // wide champions bowl on a slender stem
        new Vector2(0f,0f),new Vector2(.100f,0f),new Vector2(.101f,.012f),new Vector2(.080f,.026f),new Vector2(.040f,.050f),
        new Vector2(.022f,.080f),new Vector2(.017f,.200f),new Vector2(.026f,.212f),new Vector2(.018f,.226f),new Vector2(.016f,.290f),
        new Vector2(.040f,.310f),new Vector2(.110f,.340f),new Vector2(.180f,.372f),new Vector2(.214f,.398f),new Vector2(.222f,.410f),
        new Vector2(.214f,.414f),new Vector2(.200f,.404f),new Vector2(.140f,.372f),new Vector2(0f,.352f)};
    static readonly Vector2[] GobletProfile={ // tall goblet, no handles
        new Vector2(0f,0f),new Vector2(.080f,0f),new Vector2(.082f,.012f),new Vector2(.060f,.030f),new Vector2(.030f,.060f),
        new Vector2(.018f,.110f),new Vector2(.016f,.250f),new Vector2(.030f,.262f),new Vector2(.017f,.276f),new Vector2(.030f,.300f),
        new Vector2(.080f,.350f),new Vector2(.112f,.420f),new Vector2(.122f,.500f),new Vector2(.130f,.580f),new Vector2(.136f,.600f),
        new Vector2(.128f,.604f),new Vector2(.118f,.580f),new Vector2(.108f,.500f),new Vector2(0f,.430f)};
    static readonly Dictionary<string,Mesh> latheCache=new Dictionary<string,Mesh>();
    static Mesh handleMesh;
    static Material trophyGold;

    // Owner 9.11 "złoty połysk puchar": the facade gold (rough .16, metal .96)
    // reads dull on a small curved cup. Dedicated mirror-polished gold: full
    // metal, minimum shader roughness, full broad reflection of a bright warm
    // showroom (sky/ground of THIS material only) + a faint gold self-glow.
    static Material TrophyGold()
    {
        if(trophyGold)return trophyGold;
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        trophyGold=new Material(shader){name="A1 trophy | mirror-polished gold",enableInstancing=true};
        trophyGold.SetColor("_Color",new Color(1.00f,.79f,.34f));
        trophyGold.SetFloat("_Metallic",1f);
        trophyGold.SetFloat("_Roughness",.09f);
        trophyGold.SetFloat("_ReflectionStrength",1f);
        trophyGold.SetColor("_SkyColor",new Color(1.00f,.96f,.88f));
        trophyGold.SetColor("_GroundColor",new Color(.30f,.24f,.16f));
        trophyGold.SetColor("_EmissionColor",new Color(.11f,.075f,.025f));
        trophyGold.SetFloat("_VertexTint",0f);
        trophyGold.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(trophyGold,"Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/A1_trophy_polished_gold.mat");
        return trophyGold;
    }

    static void LatheQuad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        void Tri(Vector3 p,Vector3 q,Vector3 r)
        {
            if(Vector3.Dot(Vector3.Cross(q-p,r-p),facing)<0){var s=q;q=r;r=s;}
            int n=v.Count;v.Add(p);v.Add(q);v.Add(r);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        Tri(a,b,c);Tri(a,c,d);
    }

    static Mesh CupMesh()=>LatheMesh("cup",CupProfile);
    static Mesh LatheMesh(string id,Vector2[] profile)
    {
        if(latheCache.TryGetValue(id,out var cached)&&cached)return cached;
        const int seg=48;
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i+1<profile.Length;i++)
        {
            var p0=profile[i];var p1=profile[i+1];
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
        var raw=new Mesh{name="A1 trophy "+id+" | lathe 48"};
        raw.SetVertices(v);raw.SetTriangles(t,0);
        var mesh=Weld(raw);   // smooth shading: weld by position before normals
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_trophy_"+id+".asset");
        latheCache[id]=mesh;
        return mesh;
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

    static void Trophy(Transform parent,Vector3 nominalBase,float scale,float yaw,Material gold,Material plinth,string label,int design=0)
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
        var parts=new List<(Mesh,string)>();
        switch(design%5)
        {
            case 0:parts.Add((CupMesh(),"trophy loving cup"));parts.Add((HandleMesh(),"trophy handles"));break;
            case 1:parts.Add((LatheMesh("urn",UrnProfile),"trophy lidded urn"));break;
            case 2:parts.Add((LatheMesh("globe",GlobeProfile),"trophy globe on column"));break;
            case 3:parts.Add((LatheMesh("bowl",BowlProfile),"trophy champions bowl"));break;
            default:parts.Add((LatheMesh("goblet",GobletProfile),"trophy goblet"));break;
        }
        foreach(var (mesh,name) in parts)
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
        var gold=TrophyGold();var black=exterior["arena_black"];
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
            // Warm show light inside each case: gives the polished gold its glints.
            var lamp=new GameObject("A1 TROPHY CASE | warm show light").AddComponent<Light>();
            lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(x,f+glassTop-.15f,z-.10f)*K;
            lamp.type=LightType.Point;lamp.color=new Color(1f,.88f,.66f);lamp.intensity=1.4f;lamp.range=3.2f*K;
            lamp.shadows=LightShadows.None;lamp.renderMode=LightRenderMode.ForceVertex;
            // Every case shows five different designs; the two cases mirror.
            int[] large=s<0?new[]{1,0,2}:new[]{2,0,1};
            int[] small=s<0?new[]{3,4,4,3}:new[]{4,3,3,4};
            float[] lx={-.78f,0f,.78f},sx={-.87f,-.29f,.29f,.87f};
            for(int i=0;i<3;i++)
            {Trophy(root,new Vector3(x+lx[i],f+baseTop,z),1f,lx[i]*40f,gold,black,"large trophy design "+large[i],large[i]);cups++;}
            for(int i=0;i<4;i++)
            {Trophy(root,new Vector3(x+sx[i],f+shelf+.006f,z),.54f,sx[i]*60f,gold,black,"small trophy design "+small[i],small[i]);cups++;}
        }
        Debug.Log("A1_TROPHY_CASES cases=2 cups="+cups+" designs=5(loving_cup,lidded_urn,globe,champions_bowl,goblet) large=0.74m small=0.40m lathe=48 handles=swept material=mirror_polished_gold rough=.09 metal=1 reflection=1");
    }
}

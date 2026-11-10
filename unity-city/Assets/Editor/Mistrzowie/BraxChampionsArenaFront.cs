using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// OWNER RENDER f2faacc9 (9.11): "Ma być jak na zdjęciu łącznie z szyldem",
// "Zadaszenie tak samo", "Na wejściu za szybą mogą być jakieś sofy kwiaty".
// Front public entrance of A1, building space, nominal metres x K once.
// Render proportions measured at the facade plane (~23.5 px per metre):
// gold canopy underside ~6.2 m, fascia ~1.2 m, sign box on top of it
// ~38-39 m wide and ~6.5 m high, black belt ~7.6-11.4 m, lounge right above.
public static partial class BraxChampionsArenaArrival
{
    public const float CanopyUnderside=6.20f, CanopyTop=7.40f, CanopyHalfWidth=20f, CanopyFrontZ=-63.6f;
    public const float BeltBottom=7.60f, BeltTop=11.40f;
    // Upper VIP lounge floor (old slab top 18.24) now sits on the belt top,
    // exactly like the render: sofas visible right above the black band.
    public const float LoungeDrop=(BeltTop+.12f)-18.24f;
    const string LobbyModels="Assets/Source/Mistrzowie/ArenaLobby/";

    static float FrontGlassZ(float x)=>-59f*Mathf.Sqrt(Mathf.Max(0f,1f-x*x/(96f*96f)));

    // Owner render 9.11 / owner 10.11 07:14 "z zewnątrz napraw arenę ... masz moje zdjęcia": the vertical lines on the
    // black columns and the strips in the canopy soffit GLOW warm, they are not painted gold. The shared "light"
    // diffuser (.45 emission) reads beige in daylight, so the arena gets its own bright 2700 K LED line.
    static Material arenaLed;
    static Material ArenaLed(Dictionary<string,Material> exterior)
    {
        if(arenaLed)return arenaLed;
        arenaLed=new Material(exterior["light"]){name="A1 arena | warm 2700K LED line"};
        arenaLed.SetColor("_Color",new Color(1f,.86f,.58f));
        arenaLed.SetColor("_EmissionColor",new Color(1.05f,.74f,.36f));
        AssetDatabase.CreateAsset(arenaLed,"Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/A1_arena_warm_led.mat");
        return arenaLed;
    }

    // Owner 10.11 07:19: "podłoga tam jest chyba chujowa na wejściu ... jakąś zajebistą podłogę trzeba dać".
    // The public lobby (glass ellipse in front of the hall's front wall) gets its own finished floor 4.9 cm over the
    // structural dark slab (.18). The red runner (.230-.248) lies ON it; a real collider so the lobby furniture
    // (LobbyFloorY) and the player stand on the finish. The runner strip itself is left open (no coplanar faces).
    public const float LobbyFloorTop=.229f, LobbyBackZ=-35.43f, LobbyRunnerHalf=2.0f;
    // Chosen for the owner (10.11 07:25 "na podłogi coś dobierz, na wejściowej, za szkłem"): ambientCG Tiles012,
    // polished black and white marble checkerboard, 60 x 60 cm tiles (4 x 4 per 2.4 m texture).
    static Material LobbyFloorMaterial()=>BraxChampionsFoundation.LibraryAcg(
        "arena_lobby_floor_marble_checkerboard","Tiles012",Color.white,2.4f);
    static void LobbyFloor(Transform arenaRoot,Dictionary<string,Material> exterior,Material floor)
    {
        var root=Group(arenaRoot,"A1 LOBBY | premium entrance floor (owner 10.11)",Vector3.zero);
        const float rx=96f-.06f,rz=59f-.06f,step=.5f;
        float HalfWidth(float z)=>rx*Mathf.Sqrt(Mathf.Max(0f,1f-z*z/(rz*rz)));
        float start=-rz*Mathf.Sqrt(1f-Mathf.Pow((LobbyRunnerHalf+.25f)/rx,2f));
        var v=new List<Vector3>();var t=new List<int>();int rows=0;
        for(float z0=start;z0<LobbyBackZ-.001f;z0+=step)
        {
            float z1=Mathf.Min(z0+step,LobbyBackZ),w0=HalfWidth(z0),w1=HalfWidth(z1);
            foreach(float s in new[]{-1f,1f})
                FrontQuad(v,t,new Vector3(s*LobbyRunnerHalf,LobbyFloorTop,z0),new Vector3(s*w0,LobbyFloorTop,z0),
                    new Vector3(s*w1,LobbyFloorTop,z1),new Vector3(s*LobbyRunnerHalf,LobbyFloorTop,z1),Vector3.up);
            rows++;
        }
        FrontMesh(root,"A1 lobby premium floor",v,t,floor,true);
        // Where the lobby continues beside the hall (|x| > 60.4) the 4.9 cm edge of the finish gets a gold nosing.
        float edgeW=HalfWidth(LobbyBackZ);
        foreach(float s in new[]{-1f,1f})
            Box(root,"A1 LOBBY | gold nosing of the floor finish",exterior["arena_gold"],
                new Vector3(s*(60.40f+edgeW)*.5f,(.18f+LobbyFloorTop)*.5f+.003f,LobbyBackZ+.015f),
                new Vector3(edgeW-60.40f,LobbyFloorTop-.18f+.006f,.03f),false);
        Debug.Log("A1_LOBBY_FLOOR material="+floor.name+" top="+LobbyFloorTop+" rows="+rows+" runnerOpen=+-"+LobbyRunnerHalf+" collider=true");
    }

    // Owner 10.11 07:30: "tak nie zostanie, jaka czarna bryła ... ja ci zdjęcie dałem". In the owner render the whole
    // glass front shows a WARM, LIT interior; ours showed the black underside of the roof and the black lobby wall,
    // so every pane read black. The lobby (crescent between the glass and the hall's front wall, 27 m high) gets a
    // finished light ceiling under the roof: warm white plaster with warm LED lines every 2.5 m and a lit oval halo
    // over the entrance. Seen through the upper glass from the street and from the lobby. Emissive only, no lamps.
    public const float LobbyCeilingY=27.03f;
    static void LobbyCeiling(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | lit ceiling under the roof (owner render)",Vector3.zero);
        var plaster=BraxChampionsFoundation.SourceMaterial("A1_lobby_ceiling_warm_white_plaster","white_stucco_02",2.0f,new Color(.93f,.92f,.90f));
        var led=ArenaLed(exterior);
        const float rx=96f-.25f,rz=59f-.25f,step=1f;
        float HalfWidth(float z)=>rx*Mathf.Sqrt(Mathf.Max(0f,1f-z*z/(rz*rz)));
        float start=-rz*Mathf.Sqrt(1f-Mathf.Pow(4f/rx,2f));
        var v=new List<Vector3>();var t=new List<int>();
        for(float z0=start;z0<LobbyBackZ-.001f;z0+=step)
        {
            float z1=Mathf.Min(z0+step,LobbyBackZ),w0=HalfWidth(z0),w1=HalfWidth(z1);
            FrontQuad(v,t,new Vector3(-w0,LobbyCeilingY,z0),new Vector3(w0,LobbyCeilingY,z0),
                new Vector3(w1,LobbyCeilingY,z1),new Vector3(-w1,LobbyCeilingY,z1),Vector3.down);
        }
        FrontMesh(root,"A1 lobby lit ceiling plaster",v,t,plaster,false);
        int lines=0;
        for(float z=LobbyBackZ-1.25f;z>start+.6f;z-=2.5f)
        {
            float w=HalfWidth(z)-.6f;if(w<4f)continue;
            Box(root,"A1 LOBBY ceiling | warm LED line",led,new Vector3(0,LobbyCeilingY-.012f,z),new Vector3(2f*w,.02f,.07f),false);
            lines++;
        }
        // Oval halo over the entrance (owner render: lit ring in the lobby ceiling behind the sign): gold rim + LED.
        var rim=new FacadeBatch();var glow=new FacadeBatch();
        const int parts=48;const float hx=14f,hz=6.5f,hcz=-47.5f;
        for(int i=0;i<parts;i++)
        {
            float a=2f*Mathf.PI*i/parts,b=2f*Mathf.PI*(i+1)/parts;
            var p=new Vector3(hx*Mathf.Cos(a),0,hcz+hz*Mathf.Sin(a));var q=new Vector3(hx*Mathf.Cos(b),0,hcz+hz*Mathf.Sin(b));
            var rot=Quaternion.LookRotation((q-p).normalized,Vector3.up);var mid=(p+q)*.5f;float len=Vector3.Distance(p,q)+.03f;
            rim.Beam(mid+Vector3.up*(LobbyCeilingY-.16f),new Vector3(.36f,.32f,len),rot);
            glow.Beam(mid+Vector3.up*(LobbyCeilingY-.33f),new Vector3(.24f,.02f,len),rot);
        }
        rim.Emit(root,"A1_lobby_ceiling_gold_halo_28x13",exterior["arena_gold"]);
        glow.Emit(root,"A1_lobby_ceiling_halo_LED",led);
        Debug.Log("A1_LOBBY_CEILING y="+LobbyCeilingY+" plaster=white_stucco_02 ledLines="+lines+" halo=28x13");
    }

    // Orient a quad/triangle so Unity's cross(b-a,c-a) faces the wanted side.
    static void FrontTri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0){var s=b;b=c;c=s;}
        int n=v.Count;v.Add(a*K);v.Add(b*K);v.Add(c*K);t.Add(n);t.Add(n+1);t.Add(n+2);
    }
    static void FrontQuad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        FrontTri(v,t,a,b,c,facing);FrontTri(v,t,a,c,d,facing);
    }
    static GameObject FrontMesh(Transform parent,string name,List<Vector3> v,List<int> t,Material m,bool solid)
    {
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);
        // Metric planar UV (1 unit = 1 nominal metre) on the dominant axis.
        mesh.RecalculateNormals();
        var n=mesh.normals;var uv=new Vector2[v.Count];
        for(int i=0;i<v.Count;i++)
        {
            var p=v[i]/K;var a=n[i];
            if(Mathf.Abs(a.y)>.7f)uv[i]=new Vector2(p.x,p.z);
            else if(Mathf.Abs(a.x)>.7f)uv[i]=new Vector2(p.z,p.y);
            else uv[i]=new Vector2(p.x,p.y);
        }
        mesh.uv=uv;mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/"+name.Replace(' ','_').Replace('|','_')+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=m;
        if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        return go;
    }

    // Render: thick champagne-gold canopy straight across the entrance, the
    // black soffit full of warm downlights, no posts (cantilever from the belt).
    static void FrontGoldCanopy(Transform group,Dictionary<string,Material> exterior)
    {
        var root=Group(group,"A1 PUBLIC SOUTH | owner render gold canopy 40 x 4.4-5.9 x 1.2",Vector3.zero);
        const int parts=48;
        float y0=CanopyUnderside,y1=CanopyTop,fz=CanopyFrontZ,hw=CanopyHalfWidth;
        float BackZ(float x)=>FrontGlassZ(x)+.20f; // tucked 20 cm into the glazing line
        var gv=new List<Vector3>();var gt=new List<int>();
        var bv=new List<Vector3>();var bt=new List<int>();
        for(int i=0;i<parts;i++)
        {
            float x0=Mathf.Lerp(-hw,hw,i/(float)parts),x1=Mathf.Lerp(-hw,hw,(i+1)/(float)parts);
            // Black top (seen only from the upper lounge) and black soffit.
            FrontQuad(bv,bt,new Vector3(x0,y1,BackZ(x0)),new Vector3(x1,y1,BackZ(x1)),new Vector3(x1,y1,fz),new Vector3(x0,y1,fz),Vector3.up);
            FrontQuad(bv,bt,new Vector3(x0,y0,BackZ(x0)),new Vector3(x1,y0,BackZ(x1)),new Vector3(x1,y0,fz),new Vector3(x0,y0,fz),Vector3.down);
            // Gold front fascia, full 1.2 m.
            FrontQuad(gv,gt,new Vector3(x0,y0,fz),new Vector3(x0,y1,fz),new Vector3(x1,y1,fz),new Vector3(x1,y0,fz),Vector3.back);
        }
        foreach(float s in new[]{-1f,1f})
        {
            float x=s*hw;
            FrontQuad(gv,gt,new Vector3(x,y0,BackZ(x)),new Vector3(x,y1,BackZ(x)),new Vector3(x,y1,fz),new Vector3(x,y0,fz),new Vector3(s,0,0));
        }
        FrontMesh(root,"A1 owner render canopy gold fascia",gv,gt,exterior["arena_gold"],true);
        FrontMesh(root,"A1 owner render canopy black soffit and top",bv,bt,exterior["arena_black"],true);

        // Owner render: a warm line under the front edge and LED STRIPS running from the facade to the fascia
        // across the whole black soffit (was three rows of small beige squares).
        var light=ArenaLed(exterior);
        Box(root,"A1 canopy | warm LED line under gold fascia",light,
            new Vector3(0,y0-.012f,fz+.12f),new Vector3(hw*2f-.30f,.024f,.06f),false);
        int lamps=0;
        for(int i=0;i<21;i++)
        {
            float x=-18.5f+i*1.85f,z0=fz+.45f,z1=BackZ(x)-.45f;
            if(z1-z0<.6f)continue;
            Box(root,"A1 canopy | warm LED soffit strip",light,
                new Vector3(x,y0-.008f,(z0+z1)*.5f),new Vector3(.08f,.016f,z1-z0),false);
            lamps++;
        }
        foreach(float x in new[]{-12f,0f,12f})
        {
            var l=new GameObject("A1 canopy | warm downlight wash").AddComponent<Light>();
            l.transform.SetParent(root,false);l.transform.localPosition=new Vector3(x,y0-.6f,fz+2.2f)*K;
            l.type=LightType.Point;l.color=new Color(1f,.82f,.55f);l.intensity=1.1f;l.range=9f*K;
            l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForceVertex;
        }
        Debug.Log("A1_OWNER_RENDER_CANOPY width="+(hw*2f)+" front="+fz+" underside="+y0+" top="+y1+
            " downlights="+lamps+" ledStrips="+lamps+" posts=0 gold_fascia=1.2m");
    }

    // ---- Owner 10.11 07:50-07:51 on the render next to his photo: "w połowie ... złoty, okrągły detal wokół areny,
    // a u ciebie nie widać nic", "detale mają być tak samo jak na moim zdjęciu: szerokie, zaokrąglone, te złote,
    // w koło, te pionowe tak samo", "wejście ma być zaznaczone, że to jest wejście ... nic nie widać, wielka szyba".
    // One smooth swept mesh per trim around the whole ellipse: rounded (bull-nose) gold profile, no end caps.
    static void GoldSweep(Transform parent,string name,Material m,float y0,float height,float projection,float radius,
        float baseOffset,int steps=360,int arc=5)
    {
        const float rx=96f,rz=59f;
        float r=Mathf.Min(radius,Mathf.Min(projection,height*.5f)-.001f);
        var prof=new List<Vector2>();var pn=new List<Vector2>();
        prof.Add(new Vector2(0,0));pn.Add(new Vector2(0,-1));
        for(int k=0;k<=arc;k++)
        {
            float a=Mathf.Lerp(-90f,0f,k/(float)arc)*Mathf.Deg2Rad;
            prof.Add(new Vector2(projection-r+r*Mathf.Cos(a),r+r*Mathf.Sin(a)));pn.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a)));
        }
        for(int k=0;k<=arc;k++)
        {
            float a=Mathf.Lerp(0f,90f,k/(float)arc)*Mathf.Deg2Rad;
            prof.Add(new Vector2(projection-r+r*Mathf.Cos(a),height-r+r*Mathf.Sin(a)));pn.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a)));
        }
        prof.Add(new Vector2(0,height));pn.Add(new Vector2(0,1));
        int P=prof.Count;var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int i=0;i<=steps;i++)
        {
            float a=2f*Mathf.PI*i/steps;
            var p=new Vector3(rx*Mathf.Cos(a),0,rz*Mathf.Sin(a));
            var o=new Vector3(p.x/(rx*rx),0,p.z/(rz*rz)).normalized;
            for(int j=0;j<P;j++)
            {
                v.Add((p+o*(baseOffset+prof[j].x)+Vector3.up*(y0+prof[j].y))*K);
                n.Add((o*pn[j].x+Vector3.up*pn[j].y).normalized);
                uv.Add(new Vector2(i*.5f,j*.2f));
            }
        }
        for(int i=0;i<steps;i++)for(int j=0;j<P-1;j++)
        {
            int a0=i*P+j,a1=(i+1)*P+j,b0=a0+1,b1=a1+1;
            if(Vector3.Dot(Vector3.Cross(v[a1]-v[a0],v[b0]-v[a0]),n[a0]+n[b0])>=0){t.Add(a0);t.Add(a1);t.Add(b0);t.Add(b0);t.Add(a1);t.Add(b1);}
            else{t.Add(a0);t.Add(b0);t.Add(a1);t.Add(b0);t.Add(b1);t.Add(a1);}
        }
        var mesh=new Mesh{name=name,indexFormat=v.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
        mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/"+name.Replace(' ','_').Replace('|','_')+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r2=go.AddComponent<MeshRenderer>();r2.sharedMaterial=m;r2.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Owner render: a wide rounded gold nosing on both edges of the black belt and a gold crown under the roof
    // coping, all the way round; a warm LED line glows under the belt's lower nosing.
    static void FacadeGoldTrims(Transform group,Dictionary<string,Material> exterior)
    {
        var gold=exterior["arena_gold"];
        const float belt=.23f;                                   // outer face of the black belt (radial from the glass)
        // Owner 10.11 08:03-08:04: "u góry okrąg cały złoty ma być grubszy, po środku te dwa jeszcze grubsze, pionowe
        // zostają" + "zrób sobie obliczenia". Measured on the owner's wide render (building 28 m = 640 px, 23 px/m):
        // black belt ~5 m, projecting ~1.5 m; crown ~0.65 m; column line ~0.25 m. Hierarchy as ordered: the two
        // mid rings 2.5x the vertical strip (0.44 m) = 1.10 m high, the crown 0.85 m, both 0.60 m proud, bull-nose.
        GoldSweep(group,"A1 owner render | rounded gold nosing under the belt",gold,BeltBottom-.20f,1.10f,.60f,.35f,belt,360,6);
        GoldSweep(group,"A1 owner render | rounded gold nosing over the belt",gold,BeltTop-1.00f,1.10f,.60f,.35f,belt,360,6);
        GoldSweep(group,"A1 owner render | warm LED line under the belt nosing",ArenaLed(exterior),BeltBottom-.25f,.05f,.54f,.02f,belt,360,1);
        GoldSweep(group,"A1 owner render | rounded gold crown under the roof",gold,26.55f,.85f,.60f,.30f,0f,360,6);
        Debug.Log("A1_OWNER_RENDER_GOLD_TRIMS belt=2x(1.10 high,0.60 proud,R0.35) crown=0.85x0.60 R0.30 vertical=0.44 steps=360 continuous=true");
    }

    // The entrance must read as the entrance: two black pylons from the paving to the canopy either side of the
    // doors, each with a wide rounded gold strip and a warm LED line, a black head band with a gold nosing over the
    // doors, and a lit gold ring hanging just inside the doors (owner render).
    static void EntrancePortal(Transform group,Dictionary<string,Material> exterior)
    {
        var root=Group(group,"A1 PUBLIC ENTRANCE | owner render portal: black pylons, gold, lit ring",Vector3.zero);
        var black=exterior["arena_black"];var gold=exterior["arena_gold"];var led=ArenaLed(exterior);
        // back 24 cm inside the curved glazing line (glass z=-58.92 at x=5), front 1.10 m proud of the door line
        const float glass=-59.04f,back=-58.80f,depth=1.10f,pw=1.15f,px=4.97f+pw*.5f,top=CanopyUnderside;
        foreach(float s in new[]{-1f,1f})
        {
            float x=s*px,zf=glass-depth;
            Box(root,"A1 entrance | black pylon",black,new Vector3(x,(.245f+top)*.5f,(back+zf)*.5f),new Vector3(pw,top-.245f,back-zf),true);
            Box(root,"A1 entrance | wide gold strip on the pylon",gold,new Vector3(x,(.60f+top-.25f)*.5f,zf-.03f),new Vector3(.34f,top-.85f,.06f),false);
            Box(root,"A1 entrance | warm LED line in the gold strip",led,new Vector3(x,(.60f+top-.25f)*.5f,zf-.065f),new Vector3(.10f,top-.95f,.02f),false);
            Box(root,"A1 entrance | gold LED line on the door side of the pylon",led,
                new Vector3(s*(px-pw*.5f)-s*.01f,(.60f+top-.25f)*.5f,glass-depth*.5f),new Vector3(.02f,top-.95f,.08f),false);
        }
        // head band over the doors (door opening is 4.5 m high, canopy soffit 6.2 m)
        Box(root,"A1 entrance | black head band over the doors",black,new Vector3(0,(4.50f+top)*.5f,glass-.30f),new Vector3(2f*(px-pw*.5f),top-4.50f,.50f),true);
        Box(root,"A1 entrance | gold nosing under the head band",gold,new Vector3(0,4.50f+.07f,glass-.58f),new Vector3(2f*(px-pw*.5f),.14f,.10f),false);
        Box(root,"A1 entrance | warm LED line under the head band",led,new Vector3(0,4.50f-.012f,glass-.40f),new Vector3(2f*(px-pw*.5f)-.2f,.02f,.06f),false);
        // Lit gold ring just inside the doors, hung from the lobby roof.
        var ring=new FacadeBatch();var glow=new FacadeBatch();
        const int parts=40;const float rr=2.6f,ry=5.6f,rz=-55.2f;
        for(int i=0;i<parts;i++)
        {
            float a=2f*Mathf.PI*i/parts,b=2f*Mathf.PI*(i+1)/parts;
            var p=new Vector3(rr*Mathf.Cos(a),0,rz+rr*Mathf.Sin(a));var q=new Vector3(rr*Mathf.Cos(b),0,rz+rr*Mathf.Sin(b));
            var rot=Quaternion.LookRotation((q-p).normalized,Vector3.up);float len=Vector3.Distance(p,q)+.02f;
            ring.Beam((p+q)*.5f+Vector3.up*ry,new Vector3(.18f,.22f,len),rot);
            glow.Beam((p+q)*.5f+Vector3.up*(ry-.12f),new Vector3(.12f,.02f,len),rot);
        }
        ring.Emit(root,"A1_entrance_gold_ring_inside_doors",gold);
        glow.Emit(root,"A1_entrance_gold_ring_LED",led);
        foreach(float a in new[]{90f,210f,330f})
        {
            var p=new Vector3(rr*Mathf.Cos(a*Mathf.Deg2Rad),ry+.11f,rz+rr*Mathf.Sin(a*Mathf.Deg2Rad));
            Box(root,"A1 entrance | ring suspension rod",gold,new Vector3(p.x,(p.y+LobbyCeilingY)*.5f,p.z),new Vector3(.02f,LobbyCeilingY-p.y,.02f),false);
        }
        Debug.Log("A1_ENTRANCE_PORTAL pylons=2 ("+pw+"x"+depth+"x"+top+") goldStrips=2 ledLines=5 headBand=true ring=r"+rr+"@"+ry);
    }

    // Owner render: through the upper glass the interior glows - rows of suspended warm linear lights in the lobby
    // (y 19.5, between the lounge and the roof), following the glazing.
    static void LobbyPendants(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | suspended warm linear lights (seen through the upper glass)",Vector3.zero);
        var led=ArenaLed(exterior);var black=exterior["arena_black"];
        int n=0;
        foreach(float z in new[]{-53.5f,-47.5f,-41.5f})
        {
            float w=96f*Mathf.Sqrt(Mathf.Max(0f,1f-z*z/(59f*59f)))-3f;
            for(float x=-w;x<w-1f;x+=6f)
            {
                float x1=Mathf.Min(x+5.2f,w);
                Box(root,"A1 LOBBY | linear pendant body",black,new Vector3((x+x1)*.5f,19.56f,z),new Vector3(x1-x,.10f,.22f),false);
                Box(root,"A1 LOBBY | linear pendant warm diffuser",led,new Vector3((x+x1)*.5f,19.50f,z),new Vector3(x1-x-.04f,.02f,.18f),false);
                foreach(float hx in new[]{x+.4f,x1-.4f})
                    Box(root,"A1 LOBBY | pendant suspension cable",black,new Vector3(hx,(19.61f+LobbyCeilingY)*.5f,z),new Vector3(.012f,LobbyCeilingY-19.61f,.012f),false);
                n++;
            }
        }
        Debug.Log("A1_LOBBY_PENDANTS rows=3 y=19.5 pieces="+n);
    }

    // ---- entrance lobby furniture behind the front glass (both sides) ----
    static float LobbyFloorY(Transform arenaRoot,float x,float z)
    {
        Physics.SyncTransforms();
        var start=arenaRoot.TransformPoint(new Vector3(x,4f,z)*K);
        float best=.245f,bestErr=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(start,Vector3.down,8f*K,~0,QueryTriggerInteraction.Ignore))
        {
            if(hit.normal.y<.7f)continue;
            float y=arenaRoot.InverseTransformPoint(hit.point).y/K;
            if(Mathf.Abs(y-.245f)<bestErr&&Mathf.Abs(y-.245f)<1f){bestErr=Mathf.Abs(y-.245f);best=y;}
        }
        return best;
    }

    static bool HasTextures(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            foreach(var m in r.sharedMaterials)
                if(m)foreach(var n in m.GetTexturePropertyNames())if(m.GetTexture(n))return true;
        return false;
    }

    static bool PlaceLobbyModel(Transform arenaRoot,Transform parent,string file,string label,float x,float z,float yaw,bool solid,float fixedFloor=float.NaN)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
        if(!source){BraxBuildQuality.Warn("A1_LOBBY_MODEL_MISSING: "+LobbyModels+file+" (prepare_arena_lobby_models.py)");return false;}
        // Import-order guard (same as R1): if glTFast imported the model before
        // its texture assets, import it once more so materials get their maps.
        if(!HasTextures(source))
        {
            AssetDatabase.ImportAsset(LobbyModels+file,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
            if(!source)return false;
        }
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!go){BraxBuildQuality.Warn("A1_LOBBY_MODEL_INSTANTIATE_FAILED: "+file);return false;}
        go.name="A1 LOBBY | "+label+" | Poly Haven CC0 real size";
        go.transform.SetParent(parent,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        go.transform.localScale=Vector3.one*K; // model is in real metres
        float floor=float.IsNaN(fixedFloor)?LobbyFloorY(arenaRoot,x,z):fixedFloor;
        go.transform.localPosition=new Vector3(x,floor,z)*K;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        Physics.SyncTransforms();
        var b=RendererBounds(go);
        if(b.size.y<=.001f){BraxBuildQuality.Warn("A1_LOBBY_MODEL_EMPTY: "+file);return false;}
        // Visible bottom exactly on the lobby floor (no floating, no sinking).
        go.transform.position+=Vector3.up*(arenaRoot.TransformPoint(new Vector3(x,floor,z)*K).y-b.min.y);
        if(!HasTextures(go))BraxBuildQuality.Warn("A1_LOBBY_MODEL_UNTEXTURED: "+file);
        if(solid)
        {
            Physics.SyncTransforms();
            b=RendererBounds(go);
            var box=go.AddComponent<BoxCollider>();
            box.center=go.transform.InverseTransformPoint(b.center);
            var size=go.transform.InverseTransformVector(b.size);
            box.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
        }
        return true;
    }

    static void EntranceLobbyFurniture(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | entrance lounges behind the front glass (owner render)",Vector3.zero);
        var gold=exterior["arena_gold"];var stone=exterior["dark_stone"];var light=exterior["light"];
        int placed=0,wanted=0;
        foreach(float s in new[]{-1f,1f})
        foreach(float ax in new[]{14f,26f})
        {
            float x=s*ax,z=FrontGlassZ(ax)+4.2f;
            // Sofa faces the glass (-Z): its front is what the street sees.
            wanted++;if(PlaceLobbyModel(arenaRoot,root,"sofa_02/sofa_02_2k.gltf","black tufted leather sofa",x,z,180f,true))placed++;
            float tz=z-1.30f,floor=LobbyFloorY(arenaRoot,x,tz);
            Box(root,"A1 LOBBY | coffee table dark granite top",stone,new Vector3(x,floor+.40f,tz),new Vector3(1.10f,.05f,.62f),false);
            foreach(float lx in new[]{-.45f,.45f})foreach(float lz in new[]{-.22f,.22f})
                Box(root,"A1 LOBBY | coffee table gold leg",gold,new Vector3(x+lx,floor+.19f,tz+lz),new Vector3(.035f,.38f,.035f),false);
            foreach(float side in new[]{-1f,1f})
            {
                wanted++;
                if(PlaceLobbyModel(arenaRoot,root,"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","black leather armchair",
                    x+side*1.70f,tz,side<0?90f:-90f,true))placed++;
            }
            // Floor lamp beside the sofa: gold stem, warm shade.
            float lxp=x+s*1.35f,lzp=z+.10f,lf=LobbyFloorY(arenaRoot,lxp,lzp);
            Box(root,"A1 LOBBY | floor lamp gold base",gold,new Vector3(lxp,lf+.015f,lzp),new Vector3(.34f,.03f,.34f),false);
            Box(root,"A1 LOBBY | floor lamp gold stem",gold,new Vector3(lxp,lf+.80f,lzp),new Vector3(.035f,1.55f,.035f),false);
            Box(root,"A1 LOBBY | floor lamp warm shade",light,new Vector3(lxp,lf+1.62f,lzp),new Vector3(.42f,.34f,.42f),false);
            // Tall plants at both ends of the group, a smaller one by the table.
            foreach(float px in new[]{-3.30f,3.30f})
            {
                wanted++;
                if(PlaceLobbyModel(arenaRoot,root,"potted_plant_01/potted_plant_01_1k.gltf","potted tree",x+px,z+.30f,(ax*37f+px*53f+s*90f)%360f,true))placed++;
            }
        }
        // Plants flanking the entrance doors inside the glass.
        foreach(float s in new[]{-1f,1f})
        {
            wanted++;
            if(PlaceLobbyModel(arenaRoot,root,"potted_plant_02/potted_plant_02_1k.gltf","leafy plant by the door",s*6.3f,FrontGlassZ(6.3f)+1.15f,s*35f,true))placed++;
        }
        Debug.Log("A1_LOBBY_FURNITURE placed="+placed+"/"+wanted+" lounges=4 sofas=4 armchairs=8 plants=10 lamps=4");
        if(placed<wanted)BraxBuildQuality.Warn("A1_LOBBY_FURNITURE incomplete "+placed+"/"+wanted);
    }

    // Owner 9.11: "przed areną prawa lewa palmy mają być". Same owned city palm
    // as the approved T11 casino (BraxMiastoAssety miasto_palmy/palma), real
    // size, LOD from its own nodes. Avenue outside the bollards (|x|=8.6) and
    // the 6.4 m carpet aisle, inside the central walking axis (car parks start
    // at |x|=30), from the planters down to the street sidewalk (z=-133).
    static void EntrancePalmAvenue(Transform arenaRoot,Transform parent)
    {
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm){BraxBuildQuality.Warn("A1_ENTRANCE_PALMS_MISSING: miasto_palmy/palma");return;}
        var root=Group(parent,"A1 ENTRANCE | palm avenue left and right (owner 9.11)",Vector3.zero);
        var spots=new List<Vector2>();
        foreach(float s in new[]{-1f,1f})
        {
            foreach(float z in new[]{-80f,-92f,-104f,-116f,-128f})spots.Add(new Vector2(s*13.5f,z));
            spots.Add(new Vector2(s*26.5f,-66f));
        }
        int placed=0;
        foreach(var at in spots)
        {
            float y=.245f;
            try{y=ActualFloorYNominal(arenaRoot,at.x,at.y);}catch(Exception){}
            var world=arenaRoot.TransformPoint(new Vector3(at.x,y,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(root,palm,"A1 entrance palm",world,(at.x*7f+at.y*3f)%360f,1f,false,true);
            if(!instance)continue;
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
            placed++;
        }
        Debug.Log("A1_ENTRANCE_PALMS placed="+placed+"/"+spots.Count+" x=+-13.5 (z -80..-128) and +-26.5 (z -66)");
        if(placed<spots.Count)BraxBuildQuality.Warn("A1_ENTRANCE_PALMS incomplete "+placed+"/"+spots.Count);
    }

    // Owner render: the upper VIP lounge has green plants between the sofas
    // and a frameless glass balustrade at the slab edge. Authored in the
    // lounge's own space (old 18.24 m floor); the lounge root carries LoungeDrop.
    static void UpperLoungeFinish(Transform lounge,Dictionary<string,Material> exterior)
    {
        const float floor=18.24f,edge=-51.40f;
        Box(lounge,"UPPER lounge | frameless glass balustrade",exterior["clear_glass"],
            new Vector3(0,floor+.55f,edge),new Vector3(83.6f,1.10f,.024f),true);
        Box(lounge,"UPPER lounge | champagne gold handrail",exterior["arena_gold"],
            new Vector3(0,floor+1.12f,edge),new Vector3(83.6f,.05f,.07f),false);
        int placed=0;
        foreach(float x in new[]{-41.5f,-28.5f,-18.5f,0f,18.5f,28.5f,41.5f}) // clear of sofas (+-9/22/35) and lamps (+-15/40)
            if(PlaceLobbyModel(lounge,lounge,"potted_plant_01/potted_plant_01_1k.gltf","upper lounge potted tree",
                x,-46.4f,(x*13f)%360f,false,floor))placed++;
        Debug.Log("A1_UPPER_LOUNGE_FINISH plants="+placed+"/7 glassBalustrade=83.6x1.10 goldHandrail=true");
    }

    // Owner 9.11: "szyby u góry były przyciemnione, a na dole jasne - tylko
    // wejście może być jasne". Both levels already used ONE facade_glass, but
    // the sun-lit lobby behind the 52%-transmitting lower glass read light.
    // The arena gets its own darker smoked glass on EVERY facade pane (lower
    // and upper, all LODs); only the door leaves/entrance fill keep clear_glass.
    static void ArenaSmokedGlass(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var shared=exterior["facade_glass"];
        var arenaGlass=new Material(shared){name="A1 arena | uniform dark smoked facade glass"};
        // Owner 9.11 23:43: "Szyby miały być przyciemnione z zewnątrz"; 10.11 00:48: "Szyby LEKKO przyciemnione, nie
        // czarne" (.70 in APK 35 and .76/.90 on the renders all read as a black box). Owner render: lightly tinted,
        // reflective glass with the warm lit lobby clearly visible behind it. Panes are single-sided.
        arenaGlass.SetColor("_Color",new Color(.12f,.16f,.22f,.55f));
        arenaGlass.SetFloat("_Roughness",.04f);
        arenaGlass.SetFloat("_ReflectionStrength",1f);
        AssetDatabase.CreateAsset(arenaGlass,"Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/A1_arena_smoked_glass.mat");
        int swapped=0;
        foreach(var r in arenaRoot.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials;bool hit=false;
            for(int i=0;i<mats.Length;i++)if(mats[i]==shared){mats[i]=arenaGlass;hit=true;swapped++;}
            if(hit)r.sharedMaterials=mats;
        }
        Debug.Log("A1_SMOKED_GLASS uniform lower+upper panes="+swapped+" alpha=.55 smoked_blue reflection=1 doors=clear_glass_only");
        if(swapped==0)BraxBuildQuality.Warn("A1_SMOKED_GLASS_NOT_APPLIED: no facade_glass renderer under the arena");
    }
}

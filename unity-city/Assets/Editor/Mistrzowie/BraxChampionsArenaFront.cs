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

    // ---- Owner 10.11 08:11-08:14: "w lobby trzeba więcej rzeczy ... sofy przy ścianie wzdłuż ... obić skórą ...
    // kwiatki też w środku" and "z tych zdjęć porobić portrety w ładnej złotej ramce i powiesić w lobby".
    // GALLERY OF CHAMPIONS on the black marble lobby wall: the owner's 8 portraits (painted frames trimmed, one
    // uniform gold 3D frame each, a gold picture light), long built-in leather banquettes under them (black leather
    // seat, owner's quilted A28 backrest, black marble plinth, gold rail), planters, brass vases, round ottomans.
    const string PortraitAtlas="Assets/Source/Mistrzowie/ArenaPortraits/portraits_";
    static Material PortraitMaterial(string atlas)
    {
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitAtlas+atlas+".jpg");
        if(!tex){BraxBuildQuality.Warn("A1_PORTRAITS_MISSING: "+PortraitAtlas+atlas+".jpg");return null;}
        var m=new Material(Shader.Find(BraxCityMaterials.ShaderName)){name="A1 lobby | owner portraits "+atlas,enableInstancing=true};
        m.SetTexture("_MainTex",tex);m.SetTexture("_EmissionMap",tex);
        m.SetColor("_Color",new Color(.42f,.42f,.42f));m.SetColor("_EmissionColor",new Color(.70f,.70f,.70f));   // lit like the murals
        m.SetFloat("_Metallic",0f);m.SetFloat("_Roughness",.85f);m.SetFloat("_VertexTint",0f);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,"Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/A1_lobby_portraits_"+atlas+".mat");
        return m;
    }
    static void PortraitQuad(Transform parent,string name,Material m,int cell,float xc,float y0,float size,float z)
    {
        float u0=(cell%2)*.5f,v0=1f-(cell/2+1)*.5f;
        Vector3 bl=new Vector3(xc-size*.5f,y0,z),br=new Vector3(xc+size*.5f,y0,z),tl=bl+Vector3.up*size,tr=br+Vector3.up*size;
        var mesh=new Mesh{name=name};
        mesh.vertices=new[]{bl*K,tl*K,tr*K,br*K};
        mesh.uv=new[]{new Vector2(u0,v0),new Vector2(u0,v0+.5f),new Vector2(u0+.5f,v0+.5f),new Vector2(u0+.5f,v0)};
        // faces -Z (toward the lobby): (b-a)x(c-a) must point to -Z
        bool flip=Vector3.Dot(Vector3.Cross(tl-bl,tr-bl),Vector3.back)<0;
        mesh.triangles=flip?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/"+name.Replace(' ','_').Replace('|','_')+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Banquette(Transform root,float x0,float x1,float wall,float f,Material black,Material quilt,Material marble,Material gold,Material led)
    {
        float L=x1-x0,xc=(x0+x1)*.5f;
        Box(root,"A1 LOBBY banquette | black marble plinth",marble,new Vector3(xc,f+.05f,wall-.40f),new Vector3(L-.08f,.10f,.60f),true);
        Box(root,"A1 LOBBY banquette | warm LED toe line",led,new Vector3(xc,f+.012f,wall-.705f),new Vector3(L-.30f,.016f,.02f),false);
        Box(root,"A1 LOBBY banquette | black leather seat base",black,new Vector3(xc,f+.20f,wall-.45f),new Vector3(L,.20f,.62f),true);
        int n=Mathf.Max(1,Mathf.RoundToInt((L-.36f)/1.9f));float w=(L-.36f)/n;
        for(int i=0;i<n;i++)
            Box(root,"A1 LOBBY banquette | black leather seat cushion",black,new Vector3(x0+.18f+w*(i+.5f),f+.375f,wall-.47f),new Vector3(w-.03f,.15f,.60f),false);
        Box(root,"A1 LOBBY banquette | quilted A28 leather backrest",quilt,new Vector3(xc,f+.76f,wall-.09f),new Vector3(L,.92f,.14f),true);
        Box(root,"A1 LOBBY banquette | gold rail on the backrest",gold,new Vector3(xc,f+1.237f,wall-.09f),new Vector3(L+.02f,.035f,.16f),false);
        foreach(float e in new[]{x0+.09f,x1-.09f})
            Box(root,"A1 LOBBY banquette | black leather armrest",black,new Vector3(e,f+.40f,wall-.40f),new Vector3(.18f,.60f,.78f),true);
    }

    static void LobbyGallery(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        var root=Group(arenaRoot,"A1 LOBBY | gallery of champions: portraits, leather banquettes, plants (owner 10.11)",Vector3.zero);
        float f=LobbyFloorTop,wall=LobbyBackZ;
        var black=BraxChampionsFoundation.LibraryLeather("A1_lobby_banquette_black_leather",new Color(.10f,.095f,.095f));
        var quilt=BraxChampionsFoundation.LibraryAcg("A1_lobby_banquette_quilted_A28","Leather013",Color.white,1.2f);
        var marble=BraxChampionsFoundation.LibraryMarble("A1_lobby_banquette_black_marble",new Color(.95f,.93f,.91f),3.0f);
        var gold=exterior["arena_gold"];var led=ArenaLed(exterior);
        // banquettes: clear of the belt vitrine (x 3.5..4.5) and the door light slots (x +-3.1)
        int seats=0;
        foreach(float s in new[]{-1f,1f})
            foreach(var (a,b) in new[]{(6.2f,18.6f),(21.4f,34.8f),(37.2f,50.6f)})
            { Banquette(root,s<0?-b:a,s<0?-a:b,wall,f,black,quilt,marble,gold,led);seats++; }
        // portraits: centred between the wall's 6 m gold inlays (x = 24, 30, 36, 42, 48), 3 x 3 m, over the banquettes
        var mats=new[]{PortraitMaterial("A"),PortraitMaterial("B")};
        const float size=3.0f,bar=.20f,y0=2.20f,zf=-.035f;
        var order=new[]{(-27f,6),(-33f,0),(-39f,2),(-45f,5),(27f,4),(33f,7),(39f,1),(45f,3)};
        int hung=0;
        foreach(var (xc,i) in order)
        {
            var m=mats[i/4];if(!m)continue;
            float z=wall+zf;
            PortraitQuad(root,"A1 LOBBY portrait "+(i+1),m,i%4,xc,f+y0,size,z);
            float yc=f+y0+size*.5f;
            Box(root,"A1 LOBBY portrait | black liner",exterior["arena_black"],new Vector3(xc,yc,wall-.012f),new Vector3(size+.10f,size+.10f,.02f),false);
            foreach(float e in new[]{-1f,1f})
            {
                Box(root,"A1 LOBBY portrait | gold frame side",gold,new Vector3(xc+e*(size*.5f+.05f+bar*.5f),yc,wall-.06f),new Vector3(bar,size+.10f+2f*bar,.12f),false);
                Box(root,"A1 LOBBY portrait | gold frame top/bottom",gold,new Vector3(xc,yc+e*(size*.5f+.05f+bar*.5f),wall-.06f),new Vector3(size+.10f,bar,.12f),false);
            }
            // gold picture light over the frame
            Box(root,"A1 LOBBY portrait | gold picture light arm",gold,new Vector3(xc,f+y0+size+.05f+bar+.18f,wall-.20f),new Vector3(.05f,.05f,.40f),false);
            Box(root,"A1 LOBBY portrait | gold picture light",gold,new Vector3(xc,f+y0+size+.05f+bar+.15f,wall-.42f),new Vector3(1.30f,.08f,.10f),false);
            Box(root,"A1 LOBBY portrait | picture light warm LED",led,new Vector3(xc,f+y0+size+.05f+bar+.105f,wall-.42f),new Vector3(1.24f,.012f,.06f),false);
            hung++;
        }
        // planters with tall plants between the banquettes, brass vases on marble pedestals
        int plants=0,vases=0;
        foreach(float s in new[]{-1f,1f})
        {
            foreach(float ax in new[]{20.0f,52.2f})
            {
                float x=s*ax,z=wall-.75f;
                Box(root,"A1 LOBBY | black marble planter",marble,new Vector3(x,f+.30f,z),new Vector3(1.0f,.60f,1.0f),true);
                Box(root,"A1 LOBBY | gold planter rim",gold,new Vector3(x,f+.615f,z),new Vector3(1.05f,.03f,1.05f),false);
                if(PlaceLobbyModel(arenaRoot,root,"potted_plant_01/potted_plant_01_1k.gltf","gallery planter tree",x,z,ax*31f%360f,false,f+.33f))plants++;
            }
            float px=s*36.0f,pz=wall-.55f;
            Box(root,"A1 LOBBY | black marble vase pedestal",marble,new Vector3(px,f+.45f,pz),new Vector3(.62f,.90f,.62f),true);
            Box(root,"A1 LOBBY | gold pedestal line",gold,new Vector3(px,f+.885f,pz),new Vector3(.64f,.03f,.64f),false);
            if(PlaceLobbyModel(arenaRoot,root,"brass_vase_01/brass_vase_01_1k.gltf","brass vase on pedestal",px,pz,s*30f,false,f+.90f,1.25f))vases++;
        }
        // round island ottomans with a planter and a plant in the middle
        int ottomans=0;
        foreach(var at in new[]{new Vector2(-40f,-44.5f),new Vector2(40f,-44.5f),new Vector2(-56f,-42.5f),new Vector2(56f,-42.5f)})
        {
            RoundedCylinder(root,"A1 LOBBY | round black leather ottoman",black,new Vector3(at.x,f+.225f,at.y),1.90f,.45f,.10f,true);
            RoundedCylinder(root,"A1 LOBBY | ottoman gold band",gold,new Vector3(at.x,f+.06f,at.y),1.93f,.05f,.01f,false);
            RoundedCylinder(root,"A1 LOBBY | ottoman centre planter black marble",marble,new Vector3(at.x,f+.75f,at.y),.70f,.60f,.04f,true);
            RoundedCylinder(root,"A1 LOBBY | ottoman planter gold rim",gold,new Vector3(at.x,f+1.06f,at.y),.73f,.04f,.01f,false);
            if(PlaceLobbyModel(arenaRoot,root,"potted_plant_02/potted_plant_02_1k.gltf","ottoman planter plant",at.x,at.y,at.x*17f%360f,false,f+.86f,1.3f))plants++;
            ottomans++;
        }
        Debug.Log("A1_LOBBY_GALLERY portraits="+hung+"/8 size=3x3 goldFrames="+hung+" banquettes="+seats+" ottomans="+ottomans+" plants="+plants+" brassVases="+vases);
        if(hung<8)BraxBuildQuality.Warn("A1_LOBBY_GALLERY portraits "+hung+"/8 (ArenaPortraits atlases)");
    }

    // Owner 10.11 08:11-08:25: "więcej drzewek przed arenę, tych palm", "żadnych palm w doniczkach, normalnie
    // posadzone, tam jest miejsca, trawnika jest dużo", "albo wzdłuż ulicy na trawie, ładnie rozsadzić palmy".
    // The region plan (legacy_foundation.zone) has GRASS between the A1 plot (local z -75) and the Z650 sidewalk
    // (local z -133), and on both sides of the plot (local |x| 110..183). The owner's own city palms (same pack as
    // the palm avenue) are planted straight into that lawn, with a small warm uplight in the grass:
    // a row along the street, a second avenue row beside the promenade, a row on each side lawn.
    static void LawnPalms(Transform arenaRoot,Transform parent,Dictionary<string,Material> exterior)
    {
        // Owner 10.11 08:43-08:47: "te grube palmy na grubym pniu są zajebiste wzdłuż drogi ... wzdłuż chodnika szerokiego",
        // "nie powiedziałem tylko grube": the thick-trunk palm (palm_hp_4 -> miasto_palmy/palma) along the street and the
        // entrance walk, both models mixed on the side lawns.
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");var palm2=BraxMiastoAssety.Model("miasto_palmy","palma2");
        if(!palm){BraxBuildQuality.Warn("A1_LAWN_PALMS_MISSING: miasto_palmy/palma");return;}
        var root=Group(parent,"A1 LAWNS | palms planted in the grass (owner 10.11)",Vector3.zero);
        var led=ArenaLed(exterior);
        var spots=new List<Vector2>();var thickOnly=new HashSet<Vector2>();
        foreach(float s in new[]{-1f,1f})
        {
            foreach(float z in new[]{-88f,-100f,-112f,-124f}){spots.Add(new Vector2(s*21f,z));thickOnly.Add(new Vector2(s*21f,z));}   // walk avenue, 2nd row
            foreach(float ax in new[]{33f,45f,57f,77f,89f,101f,113f,125f,137f,149f,161f,173f})              // along the street,
                {spots.Add(new Vector2(s*ax,-128.5f));thickOnly.Add(new Vector2(s*ax,-128.5f));}            // clear of the lanes x+-61.8..68.2
            for(float z=-112f;z<=104.1f;z+=16f)
                if(!(s>0&&z>-13f&&z<5f))spots.Add(new Vector2(s*128f,z));                                     // side lawns, clear of the east walk (z -10..2)
        }
        int placed=0;
        foreach(var at in spots)
        {
            float y=.16f;
            try{y=ActualFloorYNominal(arenaRoot,at.x,at.y);}catch(Exception){}
            Box(root,"A1 LAWNS | warm uplight in the grass at the palm",led,new Vector3(at.x,y+.015f,at.y-1.30f),new Vector3(.26f,.02f,.26f),false);
            var src=(!thickOnly.Contains(at)&&palm2&&placed%2==1)?palm2:palm;
            var world=arenaRoot.TransformPoint(new Vector3(at.x,y,at.y)*K);
            var instance=BraxMiastoAssety.Postaw(root,src,"A1 lawn palm",world,(at.x*11f+at.y*7f)%360f,1f,false,true);
            if(!instance)continue;
            BraxMiastoAssety.LodyZWezlow(instance,new[]{.25f,.08f,.012f});
            placed++;
        }
        Debug.Log("A1_LAWN_PALMS placed="+placed+"/"+spots.Count+" street=24 (z -128.5) avenue=8 (x+-21) sideLawns=27 (x+-128, east walk kept clear) pots=0 thick=street+walk mixed=sideLawns");
        if(placed<spots.Count)BraxBuildQuality.Warn("A1_LAWN_PALMS incomplete "+placed+"/"+spots.Count);
    }

    // ---- Owner 10.11 08:26: "flagi, żeby powiewały, przed areną, jakieś takie premium". Ten 14 m black poles with
    // gold collar and gold ball, each with a 1.5 x 6 m swallowtail banner (bordeaux / black, gold border, the gold
    // bull of the owner's entrance mural, CHAMPION ARENA, B R A X H O R N - make_arena_flags.py) that waves in the
    // wind in the vertex shader (BraxChampionsFlag.shader). Both sides of the walkway to the doors + the forecourt.
    const string FlagAtlas="Assets/Source/Mistrzowie/ArenaFlags/arena_flags.png";
    static Material FlagMaterial()
    {
        const string template="Assets/Editor/Mistrzowie/BraxChampionsFlag.shader",path="Assets/PlayableGenerated/Mistrzowie/Buildings/MistrzowieFlag.shader";
        const string sourceName="Shader \"Braxhorn/MistrzowieFlagSource\"";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(FlagAtlas);
        if(!tex){BraxBuildQuality.Warn("A1_FLAGS_ATLAS_MISSING: "+FlagAtlas);return null;}
        if(!System.IO.File.Exists(template))throw new System.IO.FileNotFoundException("Flag source shader missing",template);
        string src=System.IO.File.ReadAllText(template);
        if(!src.Contains(sourceName))throw new InvalidOperationException("Flag shader identity changed");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllText(path,src.Replace(sourceName,"Shader \"Braxhorn/MistrzowieFlagMobile\""));
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);
        if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Generated flag shader failed import: "+path);
        var m=new Material(shader){name="A1 arena | waving banner flags",enableInstancing=true};
        m.SetTexture("_MainTex",tex);m.SetFloat("_Cutoff",.5f);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,"Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/A1_arena_flags.mat");
        return m;
    }
    // Banner mesh in its own frame: x across (0 at the pole, +-1.5 m), y down from the top bar (0..-6), normal -Z.
    // uv = atlas cell, uv2 = (u from the pole, t from the top) for the wave. Left-side banners mirror x but keep the
    // print readable from the street.
    static Mesh BannerMesh(int cell,float dir)
    {
        const int nx=6,ny=24;const float bw=1.5f,bh=6.0f;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var uv2=new List<Vector2>();var t=new List<int>();
        for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++)
        {
            float u=i/(float)nx,tt=j/(float)ny;
            v.Add(new Vector3(dir*u*bw,-tt*bh,0)*K);n.Add(Vector3.back);
            float tu=dir>0?u:1f-u;
            uv.Add(new Vector2(cell*.5f+tu*.5f,1f-tt));uv2.Add(new Vector2(u,tt));
        }
        for(int j=0;j<ny;j++)for(int i=0;i<nx;i++)
        {
            int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
            var face=Vector3.Cross(v[b]-v[a],v[c]-v[a]);
            if(Vector3.Dot(face,Vector3.back)>=0){t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);}
            else{t.Add(a);t.Add(c);t.Add(b);t.Add(b);t.Add(c);t.Add(d);}
        }
        var mesh=new Mesh{name="A1 banner flag "+cell+(dir>0?" R":" L")};
        mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetUVs(1,uv2);mesh.SetTriangles(t,0);
        var b2=new Bounds(new Vector3(dir*.75f,-3f,0)*K,new Vector3(2.2f,6.6f,1.2f)*K);mesh.bounds=b2;   // room for the wave
        AssetDatabase.CreateAsset(mesh,"Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_banner_flag_"+cell+(dir>0?"_R":"_L")+".asset");
        return mesh;
    }
    static void ArenaFlags(Transform arenaRoot,Transform parent,Dictionary<string,Material> exterior)
    {
        var mat=FlagMaterial();if(!mat)return;
        var root=Group(parent,"A1 FRONT | waving banner flags (owner 10.11)",Vector3.zero);
        var black=exterior["arena_black"];var gold=exterior["arena_gold"];var stone=exterior["dark_stone"];var led=ArenaLed(exterior);
        var meshes=new Dictionary<string,Mesh>();
        var spots=new List<Vector2>();
        foreach(float s in new[]{-1f,1f})
        {
            spots.Add(new Vector2(s*11.5f,-72.5f));                                   // forecourt, either side of the carpet
            foreach(float z in new[]{-86f,-98f,-110f,-122f})spots.Add(new Vector2(s*10.2f,z));   // walkway, between the palms
        }
        const float poleH=14f,plinth=.50f;
        int k=0;
        foreach(var at in spots)
        {
            float y=.16f;
            try{y=ActualFloorYNominal(arenaRoot,at.x,at.y);}catch(Exception){}
            float dir=at.x<0?-1f:1f;                                                  // banner hangs away from the walkway
            Box(root,"A1 FLAG | black granite plinth",stone,new Vector3(at.x,y+plinth*.5f,at.y),new Vector3(.90f,plinth,.90f),true);
            Box(root,"A1 FLAG | gold plinth line",gold,new Vector3(at.x,y+plinth-.01f,at.y),new Vector3(.92f,.02f,.92f),false);
            RoundedCylinder(root,"A1 FLAG | black pole",black,new Vector3(at.x,y+plinth+poleH*.5f,at.y),.085f,poleH,.02f,true);
            RoundedCylinder(root,"A1 FLAG | gold collar",gold,new Vector3(at.x,y+plinth+.20f,at.y),.13f,.30f,.03f,false);
            RoundedCylinder(root,"A1 FLAG | gold ball finial",gold,new Vector3(at.x,y+plinth+poleH+.16f,at.y),.17f,.34f,.16f,false);
            float top=y+plinth+poleH-.45f;
            Box(root,"A1 FLAG | gold banner arm",gold,new Vector3(at.x+dir*.83f,top+.03f,at.y),new Vector3(1.66f,.05f,.05f),false);
            Box(root,"A1 FLAG | warm uplight",led,new Vector3(at.x,y+plinth+.012f,at.y-.30f),new Vector3(.22f,.012f,.22f),false);
            int cell=k%2;string key=cell+(dir>0?"R":"L");
            if(!meshes.TryGetValue(key,out var mesh)){mesh=BannerMesh(cell,dir);meshes[key]=mesh;}
            var go=new GameObject("A1 FLAG | waving banner "+(cell==0?"bordeaux":"black"));
            go.transform.SetParent(root,false);
            go.transform.localPosition=new Vector3(at.x+dir*.12f,top,at.y)*K;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            k++;
        }
        Debug.Log("A1_ARENA_FLAGS poles="+spots.Count+" height="+poleH+" banners=1.5x6 swallowtail wave=vertexShader atlas="+FlagAtlas);
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

    static bool PlaceLobbyModel(Transform arenaRoot,Transform parent,string file,string label,float x,float z,float yaw,bool solid,float fixedFloor=float.NaN,float scale=1f)
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
        go.transform.localScale=Vector3.one*K*scale; // model is in real metres
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

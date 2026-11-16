using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - DOWNTOWN: premium towers close together (owner 16.11.2026 14:50: "Zacznij budować wieżowce i
// bukmachera, minimum 4 wieżowce premium blisko siebie"; plan 12.11 claude/braxhorn-city-ZALOZENIA-GRY-I-PLAN-MISTRZOW:
// "Downtown (T08-T13): BRAXHORN TOWER 250-300 m z lądowiskiem", "punkt BRAXHORN BET"). The five free tower plots of
// plan_inventory.json round the casino T11, 115 m apart (55 x 55 m each):
//   T08 (495,570) BANK            - twisting: 42 office floors, every plate turned 3 degrees (126 in all), the glass a ruled
//                                   surface between the plates (as the Evolution Tower)
//   T09 (610,570) BRAXHORN TOWER  - 60 floors + a 14 m lit crown = 262 m: a chamfered square with two setbacks,
//                                   stainless blades, BRAXHORN on the crown, a heliport on top; BRAXHORN BET downstairs
//   T10 (725,570) MEDIA           - elliptical, 34 floors, white bands, a crown of blades
//   T12 (610,465) KLUB / BIURA    - three stacked blocks turned 90 degrees to each other: cantilevers, roof terraces,
//                                   white slab bands
//   T13 (725,465) USŁUGI / KLUB   - tapering 40 -> 26 m under a steel diagrid
// Real sizes (nominal metres, K once on the root): lobby 8.0 m (double height), office storey 4.0 m, door 2.0 x 2.7 m.
// Owner 16.11 17:37 ("Real Real wszystko ... drewno to drewno, metal to metal, szkło to szkło"): the office floors are
// clad in a real curtain wall (CurtainStorey: 1.5 m units of coated glass reflecting the region's real sky, spandrels,
// black anodised mullion and transom caps with depth, every unit a fraction of a degree off true) after real towers
// (T08 Evolution Tower, T10 Absolute World, T13 Hearst Tower). The ground floors are finished and open: smoked glass,
// real doors (BraxCityDoor), marble floor, reception, a lift bank, seats, plants, pendants, light.
// No surface lies on another (owner 13:57): every prism has its own caps; caps of stacked prisms meet face to face
// (opposite faces, never seen); details stand 2 cm or more off the faces behind them.
public static class BraxChampionsWiezowce
{
    static float K => BraxClaudeOsiedle.K;
    public const float SiteY=.16f, PlotBaseY=-.25f, Fl=.18f, Lobby=8.0f, Storey=4.0f, Band=.90f, Proud=.10f, PlotHalf=27.5f;
    public const float DoorHalf=1.0f, DoorH=2.70f, Ceiling=.30f, CanopyTop=4.90f, CanopyOut=3.02f, Turn=3f;
    public static float Shaft=>Fl+Lobby;                   // the first office floor (the lobby ceiling hangs .30 under it)
    enum Shape{Twist,Setback,Ellipse,Stacked,Taper}
    struct Tower{public string id,name;public float x,z,yaw;public Shape shape;public int floors;}
    // front = local -Z, turned to the road it faces: T08-T10 road Z650 (north), T12/T13 road Z330 (south)
    static readonly Tower[] Towers={
        new Tower{id="T08",name="BANK",x=495,z=570,yaw=180f,shape=Shape.Twist,floors=42},
        new Tower{id="T09",name="BRAXHORN TOWER",x=610,z=570,yaw=180f,shape=Shape.Setback,floors=60},
        new Tower{id="T10",name="MEDIA",x=725,z=570,yaw=180f,shape=Shape.Ellipse,floors=34},
        new Tower{id="T12",name="KLUB / BIURA",x=610,z=465,yaw=0f,shape=Shape.Stacked,floors=33},
        new Tower{id="T13",name="USŁUGI / KLUB",x=725,z=465,yaw=0f,shape=Shape.Taper,floors=36},
    };
    public static readonly string[] Ids={"T08","T09","T10","T12","T13"};
    // the lift core in plan: x from x0, z from z0, w wide, d deep (local metres)
    struct Core{public float xMin,yMin,width,height;public float xMax=>xMin+width;public float yMax=>yMin+height;
        public Core(float x0,float z0,float w,float d){xMin=x0;yMin=z0;width=w;height=d;}}
    public static float Height(string id)
    {
        foreach(var t in Towers)if(t.id==id)return Shaft+t.floors*Storey+(t.shape==Shape.Setback?14f:0f);
        return 0f;
    }

    // ---------------- materials ----------------
    static Material gLobby,gClear,bandWhite,bandGraphite,steel,black,granite,plaza,marble,walnut,ceilingWhite,
        ceilingBlack,ledWhite,ledGreen,teak,grass,deck,paintWhite,lantern;
    static string outFolder;
    static Material Surface(string id,Color c,float rough,float metal,Color? emission=null)
    {
        string path=outFolder+"/Materials/tower_"+id+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m)return m;
        var shader=Shader.Find(BraxCityMaterials.ShaderName);if(!shader)throw new InvalidOperationException("City shader unavailable");
        m=new Material(shader){name="Mistrzowie tower "+id,enableInstancing=true};
        m.SetColor("_Color",c);m.SetFloat("_Roughness",rough);m.SetFloat("_Metallic",metal);m.SetFloat("_VertexTint",0f);
        if(emission.HasValue)m.SetColor("_EmissionColor",emission.Value);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material Src(string id,string slug,float metres,Color tint,float rough)
    {
        var m=BraxChampionsFoundation.SourceMaterial("tower_"+id,slug,metres,tint);
        m.SetFloat("_Roughness",rough);EditorUtility.SetDirty(m);return m;
    }
    // the skybox's own colour factor: Skybox/Panoramic multiplies by its _Tint (.5 gamma = .2140 linear) x
    // unity_ColorSpaceDouble (4.5948 in a linear project) = .9834, then by its exposure
    const float SkyboxTint=.9834f;
    public const string GlassShader="Braxhorn/ChampionsPBR";
    // coated curtain-wall glass: f0 = the coating's reflectance (real reflective units 15-30 %), body = the room behind
    static Material SkyGlass(string id,Color f0,Color body,float rough)
    {
        string path=outFolder+"/Materials/tower_"+id+".mat";
        var shader=Shader.Find(GlassShader);
        var sky=AssetDatabase.LoadAssetAtPath<Texture2D>(BraxChampionsSky.Source);
        if(!shader||!sky)
        {
            BraxBuildQuality.Warn("DO POPRAWKI TOWER_SKY_GLASS "+id+" "+(!shader?"shader "+GlassShader+" missing":"sky panorama missing "+BraxChampionsSky.Source)+
                ": this glass reflects the broad sky only");
            var f=Surface(id,body,rough,0f);f.SetFloat("_ReflectionStrength",1f);f.SetTexture("_MainTex",unitFrame);return f;
        }
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
        m.name="Mistrzowie tower sky glass "+id;m.enableInstancing=true;
        m.SetColor("_Color",body);m.SetFloat("_Roughness",rough);m.SetFloat("_Metallic",0f);m.SetFloat("_VertexTint",0f);
        m.SetTexture("_MainTex",unitFrame);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);m.SetFloat("_ReflectionStrength",1f);
        m.SetTexture("_SkyPanorama",sky);m.SetFloat("_SkyRotation",BraxChampionsSky.Rotation);
        m.SetFloat("_SkyExposure",BraxChampionsSky.Exposure*SkyboxTint);m.SetVector("_GlassF0",new Vector4(f0.r,f0.g,f0.b,1f));
        m.SetFloat("_SkyGlass",1f);m.EnableKeyword("_SKY_GLASS");
        EditorUtility.SetDirty(m);return m;
    }
    // the unit's frame in the glass texture (alpha 1 = glass, 0 = the black anodised frame: 2 px of 128 at every edge): from
    // afar the mip chain averages the frame into the glass smoothly, near the 3D caps stand in front of it
    static Texture2D unitFrame;
    static Texture2D UnitFrame()
    {
        string dir=outFolder+"/Textures",path=dir+"/tower_curtain_unit_frame.png";
        System.IO.Directory.CreateDirectory(dir);
        if(!System.IO.File.Exists(path))
        {
            const int N=128,B=2;var t=new Texture2D(N,N,TextureFormat.RGBA32,false);var px=new Color32[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                bool frame=x<B||y<B||x>=N-B||y>=N-B;
                px[y*N+x]=frame?new Color32(18,18,20,0):new Color32(255,255,255,255);
            }
            t.SetPixels32(px);t.Apply(false);
            System.IO.File.WriteAllBytes(path,t.EncodeToPNG());
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        if(AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.alphaSource=TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency=false;imp.mipmapEnabled=true;imp.streamingMipmaps=true;imp.wrapMode=TextureWrapMode.Clamp;
            imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=8;imp.maxTextureSize=128;
            var a=imp.GetPlatformTextureSettings("Android");a.overridden=true;a.maxTextureSize=128;a.format=TextureImporterFormat.ASTC_4x4;
            imp.SetPlatformTextureSettings(a);imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material mullion;
    static void Materials(Dictionary<string,Material> ext)
    {
        System.IO.Directory.CreateDirectory(outFolder+"/Materials");
        unitFrame=UnitFrame();
        // the frame: black anodised aluminium (owner 11.11 14:57: everything that holds the glass is black)
        mullion=BraxChampionsFoundation.LibraryMetal("tower_black_anodised_frame",new Color(.075f,.075f,.085f));
        gLobby=ext["facade_glass"];gClear=ext["clear_glass"];steel=ext["steel"];black=ext["arena_black"];
        bandWhite=BraxChampionsFoundation.LibraryMetal("tower_white_aluminium",new Color(.86f,.87f,.88f));
        bandGraphite=BraxChampionsFoundation.LibraryMetal("tower_graphite_aluminium",new Color(.13f,.135f,.14f));
        try{granite=BraxChampionsFoundation.LibraryMarble("tower_black_granite",new Color(.86f,.84f,.84f),2.0f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI TOWER_BLACK_GRANITE "+e.Message);granite=Src("black_granite_fallback","granite_tile",1.2f,new Color(.30f,.31f,.33f),.5f);}
        // the plaza: the neutral grey granite (granite_tile, mean 78/78/79) lifted to a light grey - no warm stone
        plaza=Src("plaza_granite","granite_tile",2.4f,new Color(1.60f,1.60f,1.60f),.75f);
        try{marble=BraxChampionsFoundation.LibraryMarbleWhite("tower_lobby_marble",Color.white,2.4f);marble.SetFloat("_Roughness",.25f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI TOWER_WHITE_MARBLE "+e.Message);marble=plaza;}
        walnut=Src("walnut","smoked_walnut_veneer",1.2f,new Color(.55f,.46f,.40f),.45f);
        ceilingWhite=Src("ceiling_white",BraxChampionsFoundation.InteriorPaintSlug,2.0f,BraxChampionsFoundation.PaintTint(new Color(1.24f,1.24f,1.23f)),.85f);
        ceilingBlack=Src("ceiling_black","brushed_concrete",2.5f,new Color(.10f,.10f,.11f),.9f);
        teak=Src("terrace_teak","teak_veneer",1.0f,new Color(.91f,1.20f,1.72f),.6f);
        deck=Src("helideck","concrete_floor_02",2.0f,new Color(.38f,.39f,.41f),.85f);
        grass=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/grass.mat");
        if(!grass)grass=Src("lawn","sparse_grass",2f,new Color(1.16f,1.64f,1.79f),.9f);
        // cool white light (owner: no golden light); the lantern of the BRAXHORN TOWER crown glows white
        ledWhite=Surface("led_white",new Color(.95f,.96f,.98f),.4f,0f,new Color(1.10f,1.14f,1.20f));
        lantern=Surface("crown_lantern",new Color(.90f,.92f,.95f),.35f,0f,new Color(.85f,.88f,.94f));
        ledGreen=Surface("led_braxhorn_green",new Color(.20f,.85f,.35f),.3f,0f,new Color(.25f,1.05f,.40f));
        paintWhite=Surface("helideck_white_paint",new Color(.93f,.93f,.92f),.6f,0f);
    }

    // ---------------- geometry ----------------
    static void Q(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)W.Quad(m,d,c,b,a);else W.Quad(m,a,b,c,d);
    }
    static Vector2 Centre(List<Vector2> p){var c=Vector2.zero;foreach(var v in p)c+=v;return c/p.Count;}
    static Vector3 V3(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
    // the outward normal of edge i of a convex polygon
    static Vector2 EdgeOut(List<Vector2> p,int i,Vector2 c)
    {
        var a=p[i];var b=p[(i+1)%p.Count];var e=b-a;var n=new Vector2(e.y,-e.x).normalized;
        if(Vector2.Dot(n,(a+b)*.5f-c)<0)n=new Vector2(-n.x,-n.y);return n;
    }
    static List<Vector2> Offset(List<Vector2> p,float d)
    {
        var c=Centre(p);var r=new List<Vector2>(p.Count);
        for(int i=0;i<p.Count;i++)
        {
            var n1=EdgeOut(p,(i-1+p.Count)%p.Count,c);var n2=EdgeOut(p,i,c);var bis=(n1+n2).normalized;
            float k=Vector2.Dot(bis,n1);r.Add(p[i]+bis*(d/Mathf.Max(.2f,k)));
        }
        return r;
    }
    static List<Vector2> Rot(List<Vector2> p,float deg)
    {
        float a=deg*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);var r=new List<Vector2>(p.Count);
        foreach(var v in p)r.Add(new Vector2(v.x*c-v.y*s,v.x*s+v.y*c));return r;
    }
    static List<Vector2> Box4(float hx,float hz,float ox=0f,float oz=0f)=>new List<Vector2>{new Vector2(ox-hx,oz-hz),new Vector2(ox+hx,oz-hz),new Vector2(ox+hx,oz+hz),new Vector2(ox-hx,oz+hz)};
    static List<Vector2> Octagon(float h,float c)=>new List<Vector2>{new Vector2(-h+c,-h),new Vector2(h-c,-h),new Vector2(h,-h+c),new Vector2(h,h-c),
        new Vector2(h-c,h),new Vector2(-h+c,h),new Vector2(-h,h-c),new Vector2(-h,-h+c)};
    // an ellipse with a FLAT front chord of half-width f (the entrance); an even number of corners
    static List<Vector2> Ellipse(float a,float b,float f,int n=48)
    {
        float t1=Mathf.PI*2f-(float)Math.Acos(-f/a),t2=Mathf.PI*2f-(float)Math.Acos(f/a);       // x=-f and x=+f on the front (z<0)
        var ts=new List<float>();
        for(int m=n;;m+=2)
        {
            ts.Clear();
            for(int i=0;i<m;i++){float t=Mathf.PI*2f*i/m;if(t>t1-.01f&&t<t2+.01f)continue;ts.Add(t);}
            ts.Add(t1);ts.Add(t2);ts.Sort();
            if(ts.Count%2==0)break;
        }
        var r=new List<Vector2>(ts.Count);foreach(var t in ts)r.Add(new Vector2(a*Mathf.Cos(t),b*Mathf.Sin(t)));
        return r;
    }
    static void Sides(Material m,List<Vector2> p,float y0,float y1)
    {
        var c=Centre(p);
        for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];var n=EdgeOut(p,i,c);
            Q(m,V3(a,y0),V3(b,y0),V3(b,y1),V3(a,y1),new Vector3(n.x,0,n.y));}
    }
    // a convex polygon's cap (an even number of corners): quads fanned from corner 0
    static void Cap(Material m,List<Vector2> p,float y,bool up)
    {
        if(p.Count%2==1)throw new InvalidOperationException("tower cap needs an even corner count");
        var n=up?Vector3.up:Vector3.down;
        for(int k=1;k+2<p.Count;k+=2)Q(m,V3(p[0],y),V3(p[k],y),V3(p[k+1],y),V3(p[k+2],y),n);
    }
    static void Prism(Material side,Material bottom,Material top,List<Vector2> p,float y0,float y1)
    {
        Sides(side,p,y0,y1);if(bottom)Cap(bottom,p,y0,false);if(top)Cap(top,p,y1,true);
    }
    // a frustum: the bottom polygon at y0, the top polygon (same corner count, same order) at y1
    static void Frustum(Material side,Material bottom,Material top,List<Vector2> p0,List<Vector2> p1,float y0,float y1)
    {
        var c=Centre(p0);
        for(int i=0;i<p0.Count;i++)
        {
            int j=(i+1)%p0.Count;var n=EdgeOut(p0,i,c);
            Q(side,V3(p0[i],y0),V3(p0[j],y0),V3(p1[j],y1),V3(p1[i],y1),new Vector3(n.x,0,n.y));
        }
        if(bottom)Cap(bottom,p0,y0,false);if(top)Cap(top,p1,y1,true);
    }
    // ---------------- the curtain wall (owner 16.11 17:37: "Real ... drewno to drewno, metal to metal, szkło to szkło") ------
    // Built the way real office towers are clad (unitised curtain wall: one unit per module 1.2-1.5 m wide and per storey,
    // BRANZ "Curtain wall cladding"; Evolution Tower: reflective cold-mirror glass, Hearst Tower: diagrid over the glass,
    // Absolute World: white slab edges round recessed glass): vision glass over a spandrel (opaque glass, or a projecting
    // aluminium band), black anodised mullion caps WITH DEPTH at every unit joint, transoms at the stack joint and over the
    // spandrel. Every insulating unit sits a fraction of a degree off true, as real units do, so the sky in the glass
    // breaks unit by unit. The glass reflects the region's real sky panorama (Braxhorn/ChampionsPBR _SKY_GLASS); from
    // afar the frame is in the glass's own texture (mip-mapped, no shimmer), near it is real 3D in front of the glass.
    struct Skin
    {
        public Material vision, spandrel, band;      // band set = an aluminium band prism instead of a spandrel of glass
        public float module, spandrelH, bandProud;   // unit width, spandrel height, how far the band stands out
        public float mullW, mullD, transH, transD;   // the caps: face width and depth in front of the glass
    }
    const float Back=.03f;                            // frame bars reach 3 cm behind the glass line (units tilt <= 1.8 cm)
    static BraxChampionsMeshBatch glassSkin;          // the units (one mesh per material)
    static List<(float y0,float y1,BraxChampionsMeshBatch b)> frames;   // the 3D frame in 8-storey chunks (near LOD each)
    static BraxChampionsMeshBatch FrameAt(float y)
    {
        int k=Mathf.FloorToInt((y-Shaft)/(8f*Storey));
        while(frames.Count<=k)frames.Add((Shaft+frames.Count*8f*Storey,Shaft+(frames.Count+1)*8f*Storey,new BraxChampionsMeshBatch()));
        return frames[Mathf.Max(0,k)].b;
    }
    static uint Hash(int a,int b,int c,int d)
    {
        unchecked{uint h=2166136261u;foreach(int v in new[]{a,b,c,d,towerId.GetHashCode()}){h^=(uint)v;h*=16777619u;h^=h>>15;}return h;}
    }
    static float Rand(uint h){unchecked{h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;h^=h>>16;}return (h&0xFFFFFF)/16777216f;}
    // quad a-b-c-d with its front along n, its UV 0..1 over the unit (the frame lines of the glass texture)
    static void UnitQuad(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)glassSkin.QuadUV(m,b,a,d,c,new Vector2(1,0),new Vector2(0,0),new Vector2(0,1),new Vector2(1,1));
        else glassSkin.QuadUV(m,a,b,c,d,new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1));
    }
    // one insulating unit: up to .6 degrees off true about its vertical axis and .4 about its horizontal one
    static void Unit(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n,uint seed)
    {
        float yawT=Mathf.Tan((Rand(seed)-.5f)*1.2f*Mathf.Deg2Rad),pitT=Mathf.Tan((Rand(seed*7919u+13u)-.5f)*.8f*Mathf.Deg2Rad);
        var ctr=(a+b+c+d)*.25f;var e=(b-a).normalized;var up=(d-a).normalized;
        Vector3 T(Vector3 p){var r=p-ctr;return p+n*(Vector3.Dot(r,e)*yawT+Vector3.Dot(r,up)*pitT);}
        UnitQuad(m,T(a),T(b),T(c),T(d),n);glassUnits++;
    }
    // a frame bar (mullion or transom cap) from a to b on the glass line: w wide, d in front of the glass (+3 cm behind
    // it); its face and two sides only - the back is in the wall, the ends meet the next bar or a band
    static void Bar(BraxChampionsMeshBatch bt,Material m,Vector3 a,Vector3 b,Vector3 n,float w,float d,bool back=false)
    {
        var dir=b-a;float len=dir.magnitude;if(len<1e-3f)return;var f=dir/len;
        var nn=(n-Vector3.Dot(n,f)*f).normalized;var x=Vector3.Cross(f,nn).normalized;
        var c=(a+b)*.5f+nn*((d-Back)*.5f);
        Vector3 hx=x*(w*.5f),hy=f*(len*.5f),hz=nn*((d+Back)*.5f);
        Vector3 P(int sx,int sy,int sz)=>c+hx*sx+hy*sy+hz*sz;
        void F(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 nf){if(Vector3.Dot(Vector3.Cross(p1-p0,p2-p0),nf)<0)bt.Quad(m,p3,p2,p1,p0);else bt.Quad(m,p0,p1,p2,p3);}
        F(P(-1,-1,1),P(1,-1,1),P(1,1,1),P(-1,1,1),nn);      // face
        F(P(1,-1,-1),P(1,-1,1),P(1,1,1),P(1,1,-1),x);       // sides
        F(P(-1,-1,1),P(-1,-1,-1),P(-1,1,-1),P(-1,1,1),-x);
        if(back)F(P(1,-1,-1),P(-1,-1,-1),P(-1,1,-1),P(1,1,-1),-nn);   // seen from inside (the sky casino)
    }
    static bool Same(List<Vector2> a,List<Vector2> b){if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if((a[i]-b[i]).sqrMagnitude>1e-8f)return false;return true;}
    static List<Vector2> Lerp(List<Vector2> a,List<Vector2> b,float t){var r=new List<Vector2>(a.Count);for(int i=0;i<a.Count;i++)r.Add(Vector2.Lerp(a[i],b[i],t));return r;}
    // one storey of curtain wall on the glass line: outline p0 at y, p1 at y+Storey (= p0 on a straight tower)
    static void CurtainStorey(List<Vector2> p0,List<Vector2> p1,float y,Skin s,int floor,Material roof=null)
    {
        var c=Centre(p0);int nE=p0.Count;float sp=s.spandrelH;var bt=FrameAt(y);
        bool straight=Same(p0,p1);
        if(s.band)
        {
            if(straight)Prism(s.band,s.band,s.band,Offset(p0,s.bandProud),y,y+sp);
            else Frustum(s.band,s.band,s.band,Offset(p0,s.bandProud),Offset(Lerp(p0,p1,sp/Storey),s.bandProud),y,y+sp);
        }
        float g0=s.band?sp:0f;                                  // where the glass and the mullions start
        for(int i=0;i<nE;i++)
        {
            int j=(i+1)%nE;
            Vector3 P(float t,float h){var a=Vector2.Lerp(p0[i],p1[i],h/Storey);var b=Vector2.Lerp(p0[j],p1[j],h/Storey);return V3(Vector2.Lerp(a,b,t),y+h);}
            var n2=EdgeOut(p0,i,c);var n=new Vector3(n2.x,0,n2.y);
            float L=(p0[j]-p0[i]).magnitude;int m=Mathf.Max(1,Mathf.RoundToInt(L/s.module));
            for(int k=0;k<m;k++)
            {
                float t0=(float)k/m,t1=(float)(k+1)/m;
                // (the two units of a module stop 4 mm apart behind the spandrel's transom: tilted, they never cross)
                float top=s.band?Storey:Storey-.002f;
                if(!s.band)Unit(s.spandrel,P(t0,.002f),P(t1,.002f),P(t1,sp-.002f),P(t0,sp-.002f),n,Hash(floor,i,k,7));
                Unit(s.vision,P(t0,s.band?sp:sp+.002f),P(t1,s.band?sp:sp+.002f),P(t1,top),P(t0,top),n,Hash(floor,i,k,3));
            }
            // mullion caps at the unit joints; the corner one (k = 0) wider, it covers the turn to the previous edge
            for(int k=0;k<m;k++){float t=(float)k/m;Bar(bt,mullion,P(t,g0),P(t,Storey),n,k==0?s.mullW*1.6f:s.mullW,s.mullD);}
            // transoms (shallower than the mullions, never in one plane with them), stopping at the corner caps
            // (on a band tower the band's own top is the sill: no transom there). Each starts past its own corner cap and runs
            // into the next edge's corner cap, so two transoms never share a corner.
            float cut=Mathf.Min(.45f,s.mullW*.8f/L);
            if(!s.band){Bar(bt,mullion,P(cut,0),P(1f,0),n,s.transH,s.transD);Bar(bt,mullion,P(cut,sp),P(1f,sp),n,s.transH,s.transD);}
        }
        if(roof)Cap(roof,p1,y+Storey,true);
    }
    // a glass skin: vision units over spandrels of opaque (shadow-box) glass of the same coating, 1.10 m at the slab
    static Skin GlassSkin(Color f0,float mullD=.14f)=>new Skin{
        vision=SkyGlass(towerId+"_vision",f0,new Color(.030f,.034f,.040f),.04f),
        spandrel=SkyGlass(towerId+"_spandrel",f0,new Color(.012f,.013f,.015f),.06f),
        module=1.5f,spandrelH=1.10f,mullW=.065f,mullD=mullD,transH=.065f,transD=.08f};
    // a band skin: vision units between projecting aluminium slab bands (the band is the spandrel)
    static Skin BandSkin(Color f0,Material band,float proud,float bandH=.90f)=>new Skin{
        vision=SkyGlass(towerId+"_vision",f0,new Color(.030f,.034f,.040f),.04f),band=band,bandProud=proud,
        module=1.5f,spandrelH=bandH,mullW=.065f,mullD=.12f,transH=.065f,transD=.08f};
    // a blade (stainless fin) standing on the face of edge i at parameter t, from y0 to y1, w wide, d deep
    static void Blade(Material m,List<Vector2> p,int i,float t,float y0,float y1,float w=.30f,float d=.45f)
    {
        var c=Centre(p);var a=p[i];var b=p[(i+1)%p.Count];var n=EdgeOut(p,i,c);var at=Vector2.Lerp(a,b,t);
        var o=new Vector3(n.x,0,n.y);
        W.OBox(m,V3(at,(y0+y1)*.5f)+o*(d*.5f),new Vector3(w,y1-y0,d),Quaternion.LookRotation(o,Vector3.up));
    }
    // stainless blades standing on the curtain wall's mullions: every other joint of the 1.5 m units (3 m), never on a corner
    static void Blades(Material m,List<Vector2> p,float y0,float y1,float step,Func<int,bool> edgeOk=null)
    {
        for(int i=0;i<p.Count;i++)
        {
            if(edgeOk!=null&&!edgeOk(i))continue;
            float len=(p[(i+1)%p.Count]-p[i]).magnitude;int mod=Mathf.Max(1,Mathf.RoundToInt(len/1.5f));int every=Mathf.Max(1,Mathf.RoundToInt(step/1.5f));
            for(int k=every;k<mod;k+=every)Blade(m,p,i,(float)k/mod,y0,y1);
        }
    }

    // ---------------- per tower state ----------------
    static Transform outside,details,inside;
    static int doors,seats,models,betScreens,glassUnits;
    static string towerId;
    static BraxChampionsMeshBatch letters,kat;
    static List<Light> lights;
    static readonly List<Vector2> hung=new List<Vector2>();   // the pendants of this tower (plan x, z)

    public static void Generate(string id,Dictionary<string,Material> ext,string folder)
    {
        Tower t=default;bool found=false;
        foreach(var x in Towers)if(x.id==id){t=x;found=true;}
        if(!found)throw new ArgumentException("Unknown tower plot "+id);
        outFolder=folder;towerId=id;doors=0;seats=0;models=0;betScreens=0;glassUnits=0;cab=null;cabL=cabR=null;landL=landR=null;slots=0;tables=0;roulettes=0;obsluga=0;ownerMissing=0;heliY=float.NaN;helis=0;betEkrany.Clear();lototo=null;hung.Clear();Materials(ext);
        Physics.SyncTransforms();
        float datum=0f;
        if(Physics.Raycast(new Vector3(t.x,80f,t.z)*K,Vector3.down,out var hit,200f*K,~0,QueryTriggerInteraction.Ignore))
        {
            datum=hit.point.y-PlotBaseY*K;
            if(Mathf.Abs(datum)>.05f*K)BraxBuildQuality.Warn("DO POPRAWKI TOWER_"+id+"_GROUND hit y="+hit.point.y.ToString("F3"));
        }
        else BraxBuildQuality.Warn("DO POPRAWKI TOWER_"+id+"_GROUND_RAYCAST_MISS: datum y=0");
        var root=new GameObject(id+" | "+t.name+" | premium tower, finished lobby").transform;
        outside=new GameObject("exterior").transform;outside.SetParent(root,false);
        details=new GameObject("exterior details (blades, diagrid, crown; distance LOD)").transform;details.SetParent(root,false);
        inside=new GameObject("lobby (finished)").transform;inside.SetParent(root,false);
        W.Wyczysc();W.Start(outside);W.Wnetrze=false;W.Komorka=40f;
        letters=new BraxChampionsMeshBatch();kat=new BraxChampionsMeshBatch();lights=new List<Light>();
        glassSkin=new BraxChampionsMeshBatch();frames=new List<(float,float,BraxChampionsMeshBatch)>();
        Site();
        float roof;
        switch(t.shape)
        {
            case Shape.Twist:roof=Twist(t);break;
            case Shape.Setback:roof=Setback(t);break;
            case Shape.Ellipse:roof=EllipseTower(t);break;
            case Shape.Stacked:roof=Stacked(t);break;
            default:roof=Taper(t);break;
        }
        int saved=W.Zapisz(id+"_tower");
        // the curtain wall: the glass units in one mesh per material; the 3D frame in 8-storey chunks, each shown while it is
        // near (>= 22 % of the screen high, ~120 m) - beyond that the frame in the glass texture carries it, without shimmer
        glassSkin.Build(outside,id+" | curtain wall glass units",folder,true,1f);
        var frameRoot=new GameObject("curtain wall frame (black anodised caps, near LOD per 8 storeys)").transform;frameRoot.SetParent(root,false);
        int chunks=0;
        foreach(var (y0,y1,b) in frames)
        {
            if(b.Empty)continue;
            var g=b.Build(frameRoot,id+" | curtain wall frame "+y0.ToString("F0")+"-"+y1.ToString("F0")+" m",folder,false,1f);
            var rs=g.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)continue;
            var lg=g.gameObject.AddComponent<LODGroup>();lg.SetLODs(new[]{new LOD(.22f,rs)});lg.RecalculateBounds();chunks++;
        }
        kat.Build(inside,id+" | lobby screens and pieces (catalogue)",folder,false,1f);
        BraxChampionsLetters3D.Build(letters,outside,id+" "+t.name+" lettering",1f);
        var detailRenderers=details.GetComponentsInChildren<Renderer>(true);
        if(detailRenderers.Length>0)
        {
            // the blades, the diagrid and the crown stay until the tower is a speck (no pop-in of the shape)
            var lod=details.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[]{new LOD(.008f,detailRenderers),new LOD(.0001f,Array.Empty<Renderer>())});lod.RecalculateBounds();
        }
        root.localScale=Vector3.one*K;
        root.rotation=Quaternion.Euler(0,t.yaw,0);
        root.position=new Vector3(t.x*K,datum,t.z*K);
        foreach(var l in root.GetComponentsInChildren<Light>(true))l.range*=K;
        WireLift(root);
        int palms=Palms(root);
        if(doors<1)BraxBuildQuality.Warn("DO POPRAWKI TOWER_"+id+"_ENTRANCE_DOOR_MISSING");
        Debug.Log("CHAMPIONS_TOWER "+id+" "+t.name+" shape="+t.shape+" floors="+t.floors+" height="+(roof).ToString("F1")+"m nominal ("+
            (roof*K).ToString("F1")+" world) meshes="+saved+" curtainWall=units:"+glassUnits+",frameChunks:"+chunks+" doors="+doors+" seats="+seats+" models="+models+" palms="+palms+" lights="+lights.Count+
            (betScreens>0?" BRAXHORN_BET screens="+betScreens+" films=0 (street premises) lototo="+(lototo?"studio+kolektura":"none"):""));
    }

    // ---------------- the plot: granite plaza over the whole 55 x 55 m, planters, the tower stands on it ----------------
    static void Site()
    {
        W.Grupa=outside;W.Wnetrze=false;
        W.MM(plaza,-PlotHalf,PlotHalf,PlotBaseY,SiteY,-PlotHalf,PlotHalf,true);
        // granite planters with lawn at the four corners of the plaza (each its own body: granite sides, lawn top)
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
        {
            float cx=sx*(PlotHalf-4f),cz=sz*(PlotHalf-4f);
            var bed=Box4(3f,3f,cx,cz);
            Prism(granite,null,grass,bed,SiteY,SiteY+.45f);
            W.KolizjaMM(cx-3f,cx+3f,SiteY,SiteY+.45f,cz-3f,cz+3f);
        }
    }

    // ---------------- the lobby (the ground floor of every tower) ----------------
    // F: the ground-floor outline (convex, even corner count); front: its flat front edge at z=front (local -Z);
    // doorsX: the doors on that edge; core: the lift core (x0,x1,z0,z1); split: a partition at x=split (the BET wing
    // west of it) or NaN
    static void GroundFloor(List<Vector2> F,float front,float[] doorsX,Core core,float split=float.NaN)
    {
        float y0=Fl,y1=Shaft,ceil=Shaft-Ceiling;
        // the floor slab (its own body on the plaza) and the ceiling
        W.Grupa=inside;W.Wnetrze=true;
        var parts=new List<(List<Vector2> poly,Material floor,Material ceil)>();
        if(float.IsNaN(split))parts.Add((F,marble,ceilingWhite));
        else{parts.Add((ClipX(F,split,true),granite,ceilingBlack));parts.Add((ClipX(F,split,false),marble,ceilingWhite));}
        foreach(var (poly,fm,cm) in parts){Prism(fm,null,fm,poly,SiteY,y0);Cap(cm,poly,ceil,false);}
        {
            float x0=float.MaxValue,x1=float.MinValue,z0=float.MaxValue,z1=float.MinValue;
            foreach(var v in F){x0=Mathf.Min(x0,v.x);x1=Mathf.Max(x1,v.x);z0=Mathf.Min(z0,v.y);z1=Mathf.Max(z1,v.y);}
            W.KolizjaMM(x0,x1,SiteY,y0,z0,z1);
        }
        W.Grupa=outside;W.Wnetrze=false;
        var c=Centre(F);
        for(int i=0;i<F.Count;i++)
        {
            var a=F[i];var b=F[(i+1)%F.Count];var n=EdgeOut(F,i,c);var o=new Vector3(n.x,0,n.y);
            float len=(b-a).magnitude;var e=(b-a)/len;
            bool isFront=Mathf.Abs(a.y-front)<.01f&&Mathf.Abs(b.y-front)<.01f;
            // openings on this edge (u along the edge from a)
            var cuts=new List<(float u0,float u1)>();
            if(isFront&&doorsX!=null)foreach(float dx in doorsX){float u=Mathf.Abs(e.x)>.5f?(dx-a.x)/e.x:0f;cuts.Add((u-DoorHalf-.10f,u+DoorHalf+.10f));}
            cuts.Sort((p,q)=>p.u0.CompareTo(q.u0));
            float u0=0f;
            void Glass(float s0,float s1,float ya,float yb)
            {
                if(s1-s0<.05f)return;
                var pa=a+e*s0;var pb=a+e*s1;
                // the pane (24 mm) on the edge line; black mullions at both ends, a transom at 4.0 m
                W.OBox(gLobby,V3((pa+pb)*.5f,(ya+yb)*.5f),new Vector3(s1-s0,yb-ya,.024f),Quaternion.LookRotation(o,Vector3.up));
            }
            void Panes(float s0,float s1)
            {
                if(s1-s0<.05f)return;
                int n2=Mathf.Max(1,Mathf.CeilToInt((s1-s0)/3.0f-.01f));float w=(s1-s0)/n2;
                for(int k=0;k<n2;k++){Glass(s0+k*w+.06f,s0+(k+1)*w-.06f,y0,y0+4.0f-.06f);Glass(s0+k*w+.06f,s0+(k+1)*w-.06f,y0+4.0f+.06f,y1-.02f);}
                W.Grupa=details;
                // mullions stand on the plaza (their feet 2 cm under the panes' - never in the panes' plane)
                for(int k=0;k<=n2;k++){var pm=a+e*(s0+k*w);W.OBox(black,V3(pm,(SiteY+y1)*.5f)+o*.03f,new Vector3(.12f,y1-SiteY,.18f),Quaternion.LookRotation(o,Vector3.up));}
                {var pa=a+e*s0;var pb=a+e*s1;W.OBox(black,V3((pa+pb)*.5f,y0+4.0f)+o*.03f,new Vector3(s1-s0,.12f,.18f),Quaternion.LookRotation(o,Vector3.up));}
                W.Grupa=outside;
                var ca=a+e*s0;var cb=a+e*s1;W.Kolizja(V3((ca+cb)*.5f,(y0+y1)*.5f),new Vector3(s1-s0,y1-y0,.3f),Quaternion.LookRotation(o,Vector3.up));
            }
            foreach(var (c0,c1) in cuts)
            {
                Panes(u0,c0);
                // the door: black portal, two framed clear leaves (BraxCityDoor), the smoked transom over it
                var mid=a+e*((c0+c1)*.5f);
                Entrance(mid,o,e,y0);
                Glass(c0+.10f,c1-.10f,y0+DoorH+.16f,y1-.02f);   // (its top 2 cm under the curtain wall's first transom)
                u0=c1;
            }
            Panes(u0,len);
        }
    }
    static List<Vector2> ClipX(List<Vector2> p,float x,bool keepLess)
    {
        var r=new List<Vector2>();
        for(int i=0;i<p.Count;i++)
        {
            var a=p[i];var b=p[(i+1)%p.Count];
            bool ia=keepLess?a.x<=x:a.x>=x,ib=keepLess?b.x<=x:b.x>=x;
            if(ia)r.Add(a);
            if(ia!=ib){float t=(x-a.x)/(b.x-a.x);r.Add(Vector2.Lerp(a,b,t));}
        }
        if(r.Count%2==1){int g=0;float best=0f;for(int i=0;i<r.Count;i++){float d=(r[(i+1)%r.Count]-r[i]).magnitude;if(d>best){best=d;g=i;}}
            r.Insert(g+1,(r[g]+r[(g+1)%r.Count])*.5f);}
        return r;
    }
    // a double door in a black portal on a façade edge: at = the door centre on the façade line, o = outward normal,
    // e = along the edge
    static void Entrance(Vector2 at,Vector3 o,Vector2 e,float y0)
    {
        var q=Quaternion.LookRotation(o,Vector3.up);var E=V3(e,0f).normalized;var c=V3(at,y0);
        float top=y0+DoorH,jw=.10f,jd=.24f,head=.16f;
        W.Grupa=outside;
        float sink=c.y-SiteY;                                                                                                   // jambs on the plaza
        foreach(float s in new[]{-1f,1f})W.OBox(black,c+E*s*(DoorHalf+jw*.5f)+Vector3.up*((DoorH+head-sink)*.5f),new Vector3(jw,DoorH+head+sink,jd),q,true);   // jambs
        W.OBox(black,c+Vector3.up*(DoorH+head*.5f),new Vector3(2f*DoorHalf+2f*jw,head,jd),q);                                                        // head
        W.OBox(steel,c+Vector3.up*.005f,new Vector3(2f*DoorHalf,.01f,.30f),q);                                                                        // threshold
        var root=new GameObject("TOWER entrance | operable double door (black portal, stainless pulls)").transform;
        root.SetParent(outside,false);root.localPosition=c;root.localRotation=q;
        var leaves=new Transform[2];
        const float st=.055f,railTop=.09f,railBottom=.16f,t=.05f;
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1f:1f;
            var leaf=new GameObject("entrance leaf "+(i+1)).transform;leaf.SetParent(root,false);
            leaf.localPosition=new Vector3(side*DoorHalf,0,0);leaves[i]=leaf;
            // the leaf in the root's frame: x across (hinge at +-DoorHalf), y up, z out
            W.Grupa=leaf;
            float l0=Mathf.Min(0f,-side*DoorHalf+side*.004f),l1=Mathf.Max(0f,-side*DoorHalf+side*.004f);
            Vector3 L(float x,float y,float z)=>root.TransformPoint(leaf.localPosition+new Vector3(x,y,z))-root.TransformPoint(Vector3.zero)+c;
            void B(Material m,float x0,float x1,float yy0,float yy1,float z0,float z1,bool col=false)
                =>W.OBox(m,L((x0+x1)*.5f,(yy0+yy1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,yy1-yy0,z1-z0),q,col);
            B(gClear,l0+st,l1-st,railBottom,DoorH-railTop,-.006f,.006f,true);
            B(black,l0,l0+st,.01f,DoorH-.005f,-t*.5f,t*.5f);B(black,l1-st,l1,.01f,DoorH-.005f,-t*.5f,t*.5f);
            B(black,l0+st,l1-st,DoorH-railTop,DoorH-.005f,-t*.5f,t*.5f);B(black,l0+st,l1-st,.01f,railBottom,-t*.5f,t*.5f);
            float px=side<0?l1-.13f:l0+.13f;
            foreach(float f in new[]{-1f,1f})W.Rura(steel,L(px,.45f,f*.085f),L(px,1.85f,f*.085f),.016f,12);
        }
        W.Grupa=outside;
        var door=root.gameObject.AddComponent<BraxCityDoor>();
        door.id="TOWER_"+towerId+"_ENTRANCE_"+doors;door.label=towerId+" entrance";door.leaves=leaves;door.sides=new[]{1f,-1f};
        doors++;
        // a graphite canopy 6 x 3 m over the door, its back 2 cm off the glass line
        W.OBox(bandGraphite,c+Vector3.up*(CanopyTop-.15f)+o*(CanopyOut-1.5f),new Vector3(6f,.30f,3.0f),q);
    }

    // ---------------- the finished lobby inside ----------------
    static float LiftCentre(Core core,int lifts,int i){float lw=1.2f,gap=(core.width-lifts*lw)/(lifts+1);return core.xMin+gap*(i+1)+lw*(i+.5f);}
    static void LobbyFitOut(Core core,Vector2 desk,Vector2 lounge,bool loungeFacesRight,int lifts=4,float xmin=float.NegativeInfinity,int working=-1)
    {
        float y0=Fl,ceil=Shaft-Ceiling;
        W.Grupa=inside;W.Wnetrze=true;
        // the lift core clad in walnut, its front at z=core.yMin, the lift doors in brushed stainless; a working lift (the
        // SKY KASYNO express) has its shaft open in the core
        if(working<0)W.MM(walnut,core.xMin,core.xMax,y0,ceil,core.yMin,core.yMax,true);
        else LiftCore(core,LiftCentre(core,lifts,working),y0,ceil);
        float lw=1.2f;
        for(int i=0;i<lifts;i++)
        {
            if(i==working)continue;
            float x=LiftCentre(core,lifts,i);
            W.MM(steel,x-lw*.5f,x+lw*.5f,y0,y0+2.6f,core.yMin-.04f,core.yMin);                                     // the doors, 4 cm proud
            W.MM(black,x-lw*.5f-.08f,x+lw*.5f+.08f,y0+2.6f,y0+2.75f,core.yMin-.05f,core.yMin);                       // their head
            W.MM(steel,x+lw*.5f+.20f,x+lw*.5f+.32f,y0+1.0f,y0+1.25f,core.yMin-.03f,core.yMin);                     // call panel
        }
        // the reception: walnut front, white marble top, a black granite plinth in shadow
        W.MM(walnut,desk.x-2.0f,desk.x+2.0f,y0+.08f,y0+1.06f,desk.y-.4f,desk.y+.4f,true);
        W.MM(granite,desk.x-1.94f,desk.x+1.94f,y0,y0+.08f,desk.y-.34f,desk.y+.34f);
        W.MM(marble,desk.x-2.05f,desk.x+2.05f,y0+1.06f,y0+1.10f,desk.y-.45f,desk.y+.45f);
        BraxKatalog.Monitor(Ctx(),new Vector3(desk.x-.6f,y0+1.10f,desk.y+.05f),Vector3.forward,1,24f);
        BraxKatalog.Monitor(Ctx(),new Vector3(desk.x+.6f,y0+1.10f,desk.y+.05f),Vector3.forward,2,24f);
        // the lounge: a leather sofa, two lounge chairs, a round table, plants (Poly Haven, real size)
        Lounge(lounge,loungeFacesRight,"lobby","SOFA W HOLU");
        foreach(var pp in new[]{new Vector2(core.xMin-1.2f,core.yMin+.6f),new Vector2(core.xMax+1.2f,core.yMin+.6f)})
            if(pp.x>xmin)Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf","lobby plant",new Vector3(pp.x,y0,pp.y),pp.x*17f);
        // pendants over the reception and the lounge, the light of the hall
        Model("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf","lobby pendant",new Vector3(desk.x,ceil,desk.y),0f,hang:true);
        Model("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf","lobby pendant",new Vector3(lounge.x,ceil,lounge.y),0f,hang:true);
        lights.Add(W.Lampa("lobby light",new Vector3(desk.x,ceil-1.2f,desk.y-2f),1.1f,16f,new Color(.97f,.98f,1f)));
        lights.Add(W.Lampa("lobby light",new Vector3(lounge.x,ceil-1.2f,lounge.y),1.0f,16f,new Color(.97f,.98f,1f)));
        lights.Add(W.Lampa("lobby light",new Vector3((core.xMin+core.xMax)*.5f,ceil-1.2f,core.yMin-3f),1.0f,14f,new Color(.97f,.98f,1f)));
    }
    // a lounge group (Poly Haven, real size): the sofa with its back to the side, two lounge chairs facing it over a round
    // table; three seats on the sofa
    static void Lounge(Vector2 at,bool facesRight,string what,string seat)
    {
        float y0=Fl;W.Grupa=inside;W.Wnetrze=true;
        var f=facesRight?Vector3.right:Vector3.left;var r=new Vector3(-f.z,0,f.x);var c=new Vector3(at.x,y0,at.y);
        Model("ArenaLobby/sofa_02/sofa_02_2k.gltf",what+" sofa",c-f*1.2f,Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg);
        Model("HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf",what+" table",c,0f);
        foreach(float s in new[]{-1f,1f})
        {
            var p=c+f*1.1f+r*s*1.0f;var face=c-p;face.y=0;
            Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf",what+" lounge chair",p,Mathf.Atan2(face.x,face.z)*Mathf.Rad2Deg);
        }
        for(int i=-1;i<=1;i++){W.Siedzisko(c+Vector3.up*.45f-f*1.15f+r*i*.6f,f,seat);seats++;}
    }
    // light lines in the ceiling: troughs 30 cm wide (no thin line), 6 cm deep, 2 cm under the ceiling, running along z at
    // a spacing between x = xa and xb, clipped 1.2 m inside the outline F (and under zMax), parted round the lift core
    static void LightLines(List<Vector2> F,float xa,float xb,float spacing,Core? core,float zMax=float.PositiveInfinity)
    {
        int n=Mathf.Max(1,Mathf.FloorToInt((xb-xa)/spacing+.01f));float step=(xb-xa)/n;
        var xs=new float[n];for(int i=0;i<n;i++)xs[i]=xa+step*(i+.5f);
        LightLinesAt(F,xs,core,zMax);
    }
    static void LightLinesAt(List<Vector2> F,float[] xs,Core? core,float zMax=float.PositiveInfinity)
    {
        float ceil=Shaft-Ceiling;W.Grupa=inside;W.Wnetrze=true;
        foreach(float x in xs)
        {
            if(!ZRange(F,x,out float z0,out float z1))continue;
            z0+=1.2f;z1=Mathf.Min(z1-1.2f,zMax);
            var segs=new List<(float a,float b)>{(z0,z1)};
            if(core.HasValue&&x>core.Value.xMin-.6f&&x<core.Value.xMax+.6f)segs=Cut(segs,core.Value.yMin-.6f,core.Value.yMax+.6f);
            // never through a pendant hanging from the ceiling (they are placed first)
            foreach(var h in hung)if(Mathf.Abs(h.x-x)<.75f)segs=Cut(segs,h.y-.75f,h.y+.75f);
            foreach(var (a,b) in segs)if(b-a>1.5f)W.MM(ledWhite,x-.15f,x+.15f,ceil-.08f,ceil-.02f,a,b);
        }
    }
    static List<(float a,float b)> Cut(List<(float a,float b)> segs,float c0,float c1)
    {
        var r=new List<(float a,float b)>();
        foreach(var (a,b) in segs){if(c1<=a||c0>=b){r.Add((a,b));continue;}if(c0>a)r.Add((a,c0));if(c1<b)r.Add((c1,b));}
        return r;
    }
    // where the line x = const crosses a convex outline: its lowest and highest z
    static bool ZRange(List<Vector2> p,float x,out float z0,out float z1)
    {
        z0=float.MaxValue;z1=float.MinValue;
        for(int i=0;i<p.Count;i++)
        {
            var a=p[i];var b=p[(i+1)%p.Count];
            if((a.x-x)*(b.x-x)>0f||Mathf.Abs(b.x-a.x)<1e-5f)continue;
            float z=Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x));z0=Mathf.Min(z0,z);z1=Mathf.Max(z1,z);
        }
        return z1>z0;
    }
    static BraxKatalog.Ctx Ctx()=>new BraxKatalog.Ctx{m=kat,root=inside,cols=inside,unit=1f,water=null,inst=inside,seat=(p,f,l)=>{W.Grupa=inside;W.Siedzisko(p,f,l);seats++;}};

    // ---------------- T08: twisting ----------------
    static float Twist(Tower t)
    {
        const float H=16f;var F=Box4(H,H);
        var core=new Core(-5f,-1f,10f,9f);
        GroundFloor(F,-H,new[]{0f},core);
        LobbyFitOut(core,new Vector2(8.5f,-8.5f),new Vector2(-9.5f,-8.5f),true);
        LightLines(F,-H,H,4f,core);
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;
        // as the Evolution Tower (Moscow): every plate turned 3 degrees on the one below, the glass between two plates a ruled
        // surface (the units twist with it), reflective blue-silver coated glass with glass spandrels
        var skin=GlassSkin(new Color(.22f,.28f,.33f),.10f);
        for(int f=0;f<t.floors;f++,y+=Storey)CurtainStorey(Rot(F,Turn*f),Rot(F,Turn*(f+1)),y,skin,f,f==t.floors-1?deck:null);
        // the roof: a parapet of the same glass units' black frame (anodised), 1.4 m, round the last plate; a white light
        // band 25 cm high 2 cm off its face
        var last=Rot(F,Turn*t.floors);
        Prism(mullion,mullion,deck,Offset(last,.16f),y,y+1.4f);
        W.Grupa=details;Sides(ledWhite,Offset(last,.18f),y+.95f,y+1.20f);W.Grupa=outside;
        W.KolizjaMM(-H,H,Shaft,y,-H,H);
        return y+1.4f;
    }

    // ---------------- T09: BRAXHORN TOWER (setbacks, blades, crown, heliport) + BRAXHORN BET ----------------
    static float Setback(Tower t)
    {
        var F0=Octagon(20f,6f);var F1=Octagon(17f,5f);var F2=Octagon(14f,4f);
        const float split=-7f;
        // the lobby east of the BET: the lift core in the middle of it (x 1..13), the reception right of the door's line,
        // a lounge at each side
        var core=new Core(1f,-2f,12f,12f);
        GroundFloor(F0,-20f,new[]{-10.5f,4.5f},core,split);
        // the express lift's three stops: the lobby, the sky casino, the heliport's pavilion on the crown (its floor)
        heliY=Shaft+t.floors*Storey+14f+PavFloor;
        CasinoMaterials();ExpressLift(LiftCentre(core,4,0));
        // the express lift's name over its portal in the lobby (white light on the walnut)
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"SKY KASYNO",new Vector3(LiftCentre(core,4,0),Fl+2.50f,-2f),180f,.24f,.05f,.08f,2.2f);
        LobbyFitOut(core,new Vector2(11.5f,-8.5f),new Vector2(15.5f,-13f),false,4,split,0);
        Lounge(new Vector2(-2.5f,-12f),true,"lobby west","SOFA W HOLU");
        Bet(F0,split);
        LightLines(F0,split+.6f,20f,4f,core);
        LightLinesAt(F0,new[]{-15.3f,-11.0f,-8.6f},null,15f-.6f);           // between the pendants' rows, over the tills
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;int s1=30,s2=48;
        // neutral silver coated glass with glass spandrels; the setback terraces are the roofs of the sections below
        var skin=GlassSkin(new Color(.19f,.21f,.23f));
        for(int f=0;f<t.floors;f++,y+=Storey)
        {
            var g=f<s1?F0:f<s2?F1:F2;
            bool lastOfSection=f==s1-1||f==s2-1||f==t.floors-1;
            if(f>=CasinoFloor)CasinoStorey(g,y,f==t.floors-1);
            else CurtainStorey(g,g,y,skin,f,lastOfSection?deck:null);
        }
        float yS1=Shaft+s1*Storey,yS2=Shaft+s2*Storey,roof=y;
        // terraces of the setbacks: a parapet ring is the band of the next section's first floor (its own caps)
        // stainless blades every 3 m on every face of each section (30 cm face, 45 cm deep)
        W.Grupa=details;
        Blades(steel,F0,Shaft,yS1,3.0f);Blades(steel,F1,yS1,yS2,3.0f);
        // the top section's blades run up into the crown on the sides and chamfers; front and back faces stay open for
        // the name
        Blades(steel,F2,yS2,roof,3.0f);
        Blades(steel,F2,roof,roof+14f,3.0f,i=>i!=0&&i!=4);
        W.Grupa=outside;
        // the crown: a lantern glowing white 12.5 m high, 1.5 m in from the blade line; BRAXHORN in black steel on its
        // front and back; a stainless top slab; the heliport above it
        var lan=Offset(F2,-1.5f);
        Prism(lantern,null,null,lan,roof,roof+12.5f);
        Prism(steel,steel,steel,Offset(F2,.5f),roof+12.5f,roof+14f);
        foreach(var (z,yaw) in new[]{(-(14f-1.5f),180f),((14f-1.5f),0f)})
        {
            var at=new Vector3(0,roof+4.2f,z);
            BraxChampionsLetters3D.Text(letters,black,black,"BRAXHORN",at,yaw,3.6f,.25f,.10f,17f);
        }
        Heliport(roof+14f,F2);
        SkyCasino(F2);
        // the tower's body is solid but for the sky casino (its glass and floor collide on their own) and the lift's way
        W.KolizjaMM(-20f,20f,Shaft,yS1,-20f,20f);W.KolizjaMM(-17f,17f,yS1,yS2,-17f,17f);W.KolizjaMM(-14f,14f,yS2,CasinoY-.30f,-14f,14f);
        W.KolizjaMM(-14f,14f,roof,roof+14f,-14f,14f);
        // the tower's name over the lobby door (white light) and BRAXHORN BET over its door (green light): letters standing
        // on the canopy at its front edge, their faces flush with its fascia, so the canopy never hides their foot
        float edge=-20f-CanopyOut;
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"BRAXHORN TOWER",new Vector3(4.5f,Fl+CanopyTop,edge+.10f),180f,.55f,.10f,.12f,5.6f);
        BraxChampionsLetters3D.Text(letters,ledGreen,black,"BRAXHORN BET",new Vector3(-10.5f,Fl+CanopyTop,edge+.10f),180f,.60f,.10f,.12f,5.6f);
        return roof+14f+HeliGap+HeliSlab;                     // the heliport deck's top
    }
    // ---------------- the heliport (owner 16.11 21:52 "lotnisko dla heli", 21:56 "jest zejście? ... jakaś winda musiała
    // być", 22:00 "postaw go na dachu i zrób wejście") ----------------
    // An elevated rooftop heliport as they are built (ICAO Annex 14 vol. II, FAA AC 150/5390-2C): a concrete deck 21.6 m
    // square (the FATO >= 1.5 D of the EC135, D 12.2 m) on a steel grid (posts into girders every 9.3 m) 3 m over the
    // crown's slab. Its markings are cut into its top - never paint laid over it (ZASADA migotanie 16.11): the TLOF
    // perimeter white 30 cm, the touchdown / positioning ring yellow 50 cm wide of inner diameter 0.5 D (6.1 m), the
    // identification H white 3 x 1.8 m in 40 cm strokes, its bars along the approach (east - west); green perimeter lights;
    // a safety net 1.5 m wide round it (open where the stairs come up); a windsock (five bands orange / white, 2.4 m) on
    // its mast; a glass balustrade on the slab's edge. Under the deck a glass pavilion round the express lift (its third
    // stop LĄDOWISKO · DACH) and two steel stairs up to the deck - an elevated heliport needs two ways off. The BRAXHORN
    // helicopter stands on the ring.
    const float HeliHalf=10.8f,HeliGap=3.0f,HeliSlab=.40f,PavFloor=.03f,NetWidth=1.5f;
    static float heliY=float.NaN;                         // the lift's roof stop (the pavilion's floor), set before ExpressLift
    static int helis;
    static Material heliYellow,heliNet,sockOrange;
    // the stairs (south and its escape twin north): their tops at x = +-3.0, the landings +-(3.0..4.4) on the deck's edge
    const float StairTop=3.0f,StairLand=1.40f,StairIn=11.0f,StairOut=12.4f;
    static bool PrzyLadowaniuSchodow(float x,float z)=>z<0f?x>StairTop-.2f&&x<StairTop+StairLand+.2f:x<-(StairTop-.2f)&&x>-(StairTop+StairLand+.2f);
    static void Heliport(float y,List<Vector2> slab)
    {
        W.Grupa=outside;W.Wnetrze=false;
        const float h=HeliHalf;float y0=y+HeliGap,y1=y0+HeliSlab;
        heliYellow=Surface("helideck_yellow_paint",new Color(.95f,.72f,.07f),.6f,0f);
        sockOrange=Surface("windsock_orange",new Color(.97f,.36f,.06f),.8f,0f);
        HeliDeckTop(y1,h);
        var sq=Box4(h,h);Sides(bandGraphite,sq,y0,y1);Cap(bandGraphite,sq,y0,false);
        W.KolizjaMM(-h,h,y0,y1,-h,h);
        // the steel grid under it: girders along z every 9.3 m, posts into them (no bottom face on the slab)
        foreach(float gx in new[]{-9.3f,0f,9.3f})
        {
            W.MM(bandGraphite,gx-.15f,gx+.15f,y0-.45f,y0,-h,h);
            foreach(float gz in new[]{-9.3f,0f,9.3f})
                W.OBox(steel,new Vector3(gx,(y+y0-.20f)*.5f,gz),new Vector3(.40f,y0-.20f-y,.40f),Quaternion.identity,true,true);
        }
        // green perimeter lights (ICAO: TLOF / FATO edge), none on the stairs' landings
        W.Grupa=details;
        const int N=18;const float c=1.2f;
        for(int k=0;k<N;k+=2)foreach(var (px,pz) in new[]{(-h+k*c+c*.5f,-h-.12f),(-h+k*c+c*.5f,h+.12f),(-h-.12f,-h+k*c+c*.5f),(h+.12f,-h+k*c+c*.5f)})
        {
            if(Mathf.Abs(pz)>h&&PrzyLadowaniuSchodow(px,pz))continue;
            W.MM(ledGreen,px-.12f,px+.12f,y1-.10f,y1+.08f,pz-.12f,pz+.12f);
        }
        W.Grupa=outside;
        HeliNet(y0,y1,h);
        Windsock(new Vector3(13.2f,y,4.0f),y1+3.2f,Vector3.forward);
        SlabBalustrade(Offset(slab,.35f),y);
        HeliPavilion(y,y0);
        HeliStair(StairTop,1f,-StairIn,-StairOut,-h,y,y1);
        HeliStair(-StairTop,-1f,StairIn,StairOut,h,y,y1);
        Helikopter(y1);
        Debug.Log("TOWER_T09_HELIPORT deck="+(2f*h).ToString("F1")+"m top="+y1.ToString("F2")+"m nominal markings=cut(TLOF white .30, TDPM yellow 3.05-3.55, H 3x1.8) "+
            "net="+NetWidth.ToString("F1")+"m stairs=2 lift_stop="+heliY.ToString("F2")+" helicopters="+helis);
        if(helis!=1)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_HELICOPTER helicopters="+helis+" (expected 1: the BRAXHORN EC135)");
    }
    // the deck's top: one surface, each piece in its own colour (cut, never laid over). Rays from the centre (every 5.625
    // deg plus the H's corners) cut the disc round the H, the yellow ring, the deck and the white perimeter band into quads
    // between the same rays; inside the H's rectangle a grid on the rays' points - no edge has a vertex its neighbour lacks
    static void HeliDeckTop(float yt,float h)
    {
        const float r0=3.05f,r1=3.55f,band=.30f,hx=1.5f,hz=.9f,bar=.40f,cross=.20f;
        var ang=new List<float>();
        for(int i=0;i<64;i++)ang.Add(i*360f/64f);
        foreach(var p in new[]{new Vector2(hx,hz),new Vector2(cross,hz),new Vector2(hx,hz-bar)})
            foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
                ang.Add(Mathf.Repeat(Mathf.Atan2(sz*p.y,sx*p.x)*Mathf.Rad2Deg,360f));
        ang.Sort();
        var a=new List<float>();foreach(var v in ang)if(a.Count==0||v-a[a.Count-1]>1e-3f)a.Add(v);
        if(360f-a[a.Count-1]+a[0]<1e-3f)a.RemoveAt(a.Count-1);
        int n=a.Count;var dir=new Vector2[n];var rect=new Vector2[n];
        var xs=new List<float>{-hx,-cross,cross,hx};var zs=new List<float>{-hz,-(hz-bar),hz-bar,hz};
        Vector2 OnBox(Vector2 d,float bx,float bz)
        {
            float t=Mathf.Min(Mathf.Abs(d.x)>1e-6f?bx/Mathf.Abs(d.x):float.MaxValue,Mathf.Abs(d.y)>1e-6f?bz/Mathf.Abs(d.y):float.MaxValue);
            var q=d*t;
            if(Mathf.Abs(Mathf.Abs(q.x)-bx)<1e-4f)q.x=Mathf.Sign(q.x)*bx;
            if(Mathf.Abs(Mathf.Abs(q.y)-bz)<1e-4f)q.y=Mathf.Sign(q.y)*bz;
            return q;
        }
        for(int i=0;i<n;i++)
        {
            float r=a[i]*Mathf.Deg2Rad;dir[i]=new Vector2(Mathf.Cos(r),Mathf.Sin(r));rect[i]=OnBox(dir[i],hx,hz);
            if(Mathf.Abs(rect[i].y)==hz)xs.Add(rect[i].x);
            if(Mathf.Abs(rect[i].x)==hx)zs.Add(rect[i].y);
        }
        List<float> Uniq(List<float> l){l.Sort();var u=new List<float>();foreach(var v in l)if(u.Count==0||v-u[u.Count-1]>1e-4f)u.Add(v);return u;}
        xs=Uniq(xs);zs=Uniq(zs);
        Vector3 P(Vector2 p)=>new Vector3(p.x,yt,p.y);
        for(int i=0;i<n;i++)
        {
            int j=(i+1)%n;
            Q(deck,P(rect[i]),P(rect[j]),P(dir[j]*r0),P(dir[i]*r0),Vector3.up);
            Q(heliYellow,P(dir[i]*r0),P(dir[j]*r0),P(dir[j]*r1),P(dir[i]*r1),Vector3.up);
            Q(deck,P(dir[i]*r1),P(dir[j]*r1),P(OnBox(dir[j],h-band,h-band)),P(OnBox(dir[i],h-band,h-band)),Vector3.up);
            Q(paintWhite,P(OnBox(dir[i],h-band,h-band)),P(OnBox(dir[j],h-band,h-band)),P(OnBox(dir[j],h,h)),P(OnBox(dir[i],h,h)),Vector3.up);
        }
        // the H (bars along x, the crossbar across them) and the deck in its two notches
        for(int i=0;i+1<xs.Count;i++)for(int k=0;k+1<zs.Count;k++)
        {
            float cx=(xs[i]+xs[i+1])*.5f,cz=(zs[k]+zs[k+1])*.5f;
            bool white=Mathf.Abs(cz)>hz-bar||Mathf.Abs(cx)<cross;
            Q(white?paintWhite:deck,new Vector3(xs[i],yt,zs[k]),new Vector3(xs[i+1],yt,zs[k]),new Vector3(xs[i+1],yt,zs[k+1]),new Vector3(xs[i],yt,zs[k+1]),Vector3.up);
        }
    }
    // the safety net (ICAO: 1.5 m wide round an elevated deck, its outer edge not over the deck): a knotted net in a
    // tube frame on brackets from the deck's fascia, rising a little outward; its picture has mip levels (from afar a
    // dark band, never a shimmer of cords). Open over the stairs.
    static void HeliNet(float y0,float y1,float h)
    {
        heliNet=HeliNetMaterial();
        float yi=y1-.25f,yo=y1-.08f,o=h+NetWidth;
        // the four sides cut round the stairs, the four corners
        var spans=new List<(float,float,int)>();      // (from, to, side): side 0 south (-z), 1 north, 2 west (-x), 3 east
        foreach(int side in new[]{0,1})
        {
            float s0=side==0?StairTop-19*.28f-.3f:-(StairTop+StairLand+.2f),s1=side==0?StairTop+StairLand+.2f:-(StairTop-19*.28f-.3f);
            spans.Add((-h,s0,side));spans.Add((s1,h,side));
        }
        spans.Add((-h,h,2));spans.Add((-h,h,3));
        foreach(var (u0,u1,side) in spans)
        {
            Vector3 In(float u)=>side==0?new Vector3(u,yi,-h):side==1?new Vector3(u,yi,h):side==2?new Vector3(-h,yi,u):new Vector3(h,yi,u);
            Vector3 Out(float u)=>side==0?new Vector3(u,yo,-o):side==1?new Vector3(u,yo,o):side==2?new Vector3(-o,yo,u):new Vector3(o,yo,u);
            var nrm=Vector3.up;
            Q(heliNet,In(u0),In(u1),Out(u1),Out(u0),nrm);
            W.Rura(steel,Out(u0),Out(u1),.025f,8);
            int nb=Mathf.Max(1,Mathf.CeilToInt((u1-u0)/2.7f));
            for(int b=0;b<=nb;b++){float u=Mathf.Lerp(u0,u1,(float)b/nb);var i=In(u);W.Rura(steel,new Vector3(i.x,y0+.12f,i.z),Out(u),.03f,8);}
        }
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
        {
            var ci=new Vector3(sx*h,yi,sz*h);var cx=new Vector3(sx*o,yo,sz*h);var cc=new Vector3(sx*o,yo,sz*o);var cz=new Vector3(sx*h,yo,sz*o);
            Q(heliNet,ci,cx,cc,cz,Vector3.up);
            W.Rura(steel,cx,cc,.025f,8);W.Rura(steel,cz,cc,.025f,8);W.Rura(steel,new Vector3(ci.x,y0+.12f,ci.z),cc,.03f,8);
        }
    }
    static Material HeliNetMaterial()
    {
        string dir=outFolder+"/Textures",path=dir+"/tower_helideck_safety_net.png";
        System.IO.Directory.CreateDirectory(dir);
        if(!System.IO.File.Exists(path))
        {
            // 1 m of the net: a 32 mm square mesh of 6 mm dark cords (a 256 px tile, 8 px a mesh)
            const int S=256,M=8;var t=new Texture2D(S,S,TextureFormat.RGBA32,false);var px=new Color32[S*S];
            for(int y=0;y<S;y++)for(int x=0;x<S;x++)
            {
                int cx=x%M,cy=y%M;byte al=(byte)(cx<2||cy<2?255:0);
                if((cx<2)&&(cy<2))al=255;
                px[y*S+x]=new Color32(46,48,46,al);
            }
            t.SetPixels32(px);t.Apply(false);
            System.IO.File.WriteAllBytes(path,t.EncodeToPNG());
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        if(AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.alphaSource=TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency=false;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Repeat;
            imp.filterMode=FilterMode.Trilinear;imp.anisoLevel=8;imp.maxTextureSize=256;
            var an=imp.GetPlatformTextureSettings("Android");an.overridden=true;an.maxTextureSize=256;an.format=TextureImporterFormat.ASTC_4x4;
            imp.SetPlatformTextureSettings(an);imp.SaveAndReimport();
        }
        var m=Surface("helideck_safety_net",new Color(.20f,.21f,.20f,1f),.8f,0f);
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_Transparent",1f);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0f);m.SetFloat("_Cull",0f);
        m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");EditorUtility.SetDirty(m);
        return m;
    }
    // the windsock (ICAO: a truncated cone of fabric >= 2.4 m long, 0.6 m at its mouth, five bands orange / white, the
    // first and last orange) on a steel mast; it blows along blow, sagging 12 deg
    static void Windsock(Vector3 foot,float topY,Vector3 blow)
    {
        var up=Vector3.up;var top=new Vector3(foot.x,topY,foot.z);
        W.MM(steel,foot.x-.2f,foot.x+.2f,foot.y,foot.y+.02f,foot.z-.2f,foot.z+.2f);
        W.Rura(steel,new Vector3(foot.x,foot.y+.02f,foot.z),top,.05f,10);
        var mouth=top+blow*.35f-up*.05f;
        W.Rura(steel,top,mouth+up*.30f,.02f,8);
        float sag=12f*Mathf.Deg2Rad;var axis=(blow*Mathf.Cos(sag)-up*Mathf.Sin(sag)).normalized;
        var u=Vector3.Cross(axis,up).normalized;var w=Vector3.Cross(u,axis);
        const int sides=16;
        for(int b=0;b<5;b++)
        {
            float t0=b/5f,t1=(b+1)/5f,ra=Mathf.Lerp(.30f,.15f,t0),rb=Mathf.Lerp(.30f,.15f,t1);
            Vector3 ca=mouth+axis*2.4f*t0,cb=mouth+axis*2.4f*t1;var m=b%2==0?sockOrange:paintWhite;
            for(int s=0;s<sides;s++)
            {
                float p0=s*Mathf.PI*2f/sides,p1=(s+1)*Mathf.PI*2f/sides;
                Vector3 d0=u*Mathf.Cos(p0)+w*Mathf.Sin(p0),d1=u*Mathf.Cos(p1)+w*Mathf.Sin(p1);
                Vector3 A=ca+d0*ra,B=ca+d1*ra,C=cb+d1*rb,D=cb+d0*rb;var nOut=(d0+d1).normalized;
                Q(m,A,B,C,D,nOut);Q(m,A,D,C,B,-nOut);                        // fabric: seen from inside too
            }
        }
        for(int s=0;s<sides;s++)
        {
            float p0=s*Mathf.PI*2f/sides,p1=(s+1)*Mathf.PI*2f/sides;
            W.Rura(steel,mouth+(u*Mathf.Cos(p0)+w*Mathf.Sin(p0))*.30f,mouth+(u*Mathf.Cos(p1)+w*Mathf.Sin(p1))*.30f,.012f,6,false);
        }
    }
    // a glass balustrade (1.1 m: a steel shoe, clear glass, a stainless rail) round the crown's slab, colliding
    static void SlabBalustrade(List<Vector2> P,float y)
    {
        var c=Centre(P);
        for(int i=0;i<P.Count;i++)
        {
            var a=P[i];var b=P[(i+1)%P.Count];var n=EdgeOut(P,i,c);var e=b-a;float L=e.magnitude;var mid=(a+b)*.5f;
            var q=Quaternion.LookRotation(new Vector3(e.x,0f,e.y));
            W.OBox(steel,new Vector3(mid.x,y+.05f,mid.y),new Vector3(.08f,.10f,L),q,false,true);
            Q(casinoGlass,V3(a,y+.10f),V3(b,y+.10f),V3(b,y+1.04f),V3(a,y+1.04f),new Vector3(n.x,0f,n.y));
            W.Rura(steel,V3(a,y+1.07f),V3(b,y+1.07f),.03f,10);
            W.Kolizja(new Vector3(mid.x,y+.55f,mid.y),new Vector3(.12f,1.1f,L),q);
        }
    }
    // the pavilion under the deck: clear glass in a black frame round the express lift's landing (its core in walnut as
    // in the lobby and the casino), a granite floor, the deck's underside its ceiling with a light; the door south
    static void HeliPavilion(float y,float top)
    {
        const float x0=1.2f,x1=4.9f,z0=-5.0f,z1=.80f,door=.75f,t=.05f;
        float yf=y+PavFloor;
        W.Grupa=inside;W.Wnetrze=true;
        W.OBox(granite,new Vector3((x0+x1)*.5f,y+PavFloor*.5f,(z0+z1)*.5f),new Vector3(x1-x0,PavFloor,z1-z0),Quaternion.identity,false,true);
        W.KolizjaMM(x0,x1,y,yf,z0,z1);
        LiftCore(new Core(liftX-1.35f,LiftZ0,2.7f,2.6f),liftX,yf,top);
        W.Grupa=inside;W.Wnetrze=true;
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"LĄDOWISKO",new Vector3(liftX,yf+2.42f,LiftZ0),180f,.20f,.04f,.08f,2.4f);
        var walls=new List<(Vector2,Vector2)>{(new Vector2(x0,z0),new Vector2(liftX-door,z0)),(new Vector2(liftX+door,z0),new Vector2(x1,z0)),
            (new Vector2(x1,z0),new Vector2(x1,z1)),(new Vector2(x1,z1),new Vector2(x0,z1)),(new Vector2(x0,z1),new Vector2(x0,z0))};
        var centre=new Vector2((x0+x1)*.5f,(z0+z1)*.5f);var posts=new HashSet<(int,int)>();
        foreach(var (a,b) in walls)
        {
            var e=b-a;float L=e.magnitude;var mid=(a+b)*.5f;var o=mid-centre;var nrm=new Vector3(o.x,0f,o.y).normalized;
            var q=Quaternion.LookRotation(new Vector3(e.x,0f,e.y));
            Q(casinoGlass,V3(a,yf+.06f),V3(b,yf+.06f),V3(b,top-.06f),V3(a,top-.06f),nrm);
            int n=Mathf.Max(1,Mathf.CeilToInt(L/1.25f));
            for(int k=0;k<=n;k++)
            {
                var p=Vector2.Lerp(a,b,(float)k/n);
                if(posts.Add((Mathf.RoundToInt(p.x*100f),Mathf.RoundToInt(p.y*100f))))W.MM(mullion,p.x-t,p.x+t,yf,top,p.y-t,p.y+t);
            }
            W.OBox(mullion,new Vector3(mid.x,yf+.03f,mid.y),new Vector3(2f*t,.06f,L),q);
            W.OBox(mullion,new Vector3(mid.x,top-.03f,mid.y),new Vector3(2f*t,.06f,L),q);
            W.Kolizja(new Vector3(mid.x,(yf+top)*.5f,mid.y),new Vector3(.10f,top-yf,L),q);
        }
        // over the door: a transom at 2.4 m and glass above it
        W.MM(mullion,liftX-door,liftX+door,yf+2.40f,yf+2.46f,z0-t,z0+t);
        Q(casinoGlass,new Vector3(liftX-door,yf+2.46f,z0),new Vector3(liftX+door,yf+2.46f,z0),new Vector3(liftX+door,top-.06f,z0),new Vector3(liftX-door,top-.06f,z0),Vector3.back);
        W.MM(ledWhite,x0+.6f,x1-.6f,top-.02f,top,z0+.5f,LiftZ0-.7f);
        lights.Add(W.Lampa("BRAXHORN TOWER | heliport pavilion light",new Vector3((x0+x1)*.5f,top-.5f,(z0+LiftZ0)*.5f),.8f,7f,new Color(.97f,.98f,1f)));
        W.Grupa=outside;W.Wnetrze=false;
    }
    // a steel stair to the deck (20 risers of 17 cm, treads 28 cm: open steel treads on two stringers, a handrail each
    // side, colliding as a ramp) climbing along x (dir) to its top xt at the deck's top yt, between z zi (the deck's side)
    // and zo; its landing 1.4 m beyond xt out to the deck's edge ze
    static void HeliStair(float xt,float dir,float zi,float zo,float ze,float y,float yt)
    {
        const int N=20;const float G=.28f;
        float run=(N-1)*G,xb=xt-dir*run,rise=(yt-y)/N,za=Mathf.Min(zi,zo),zb=Mathf.Max(zi,zo),zc=(za+zb)*.5f,width=zb-za;
        W.Grupa=outside;W.Wnetrze=false;
        for(int k=0;k<N-1;k++)
        {
            float a=xb+dir*k*G,b=xb+dir*(k+1)*G,ty=y+(k+1)*rise;
            W.MM(steel,Mathf.Min(a,b),Mathf.Max(a,b),ty-.04f,ty,za+.07f,zb-.07f);
        }
        float len=Mathf.Sqrt(run*run+(yt-y)*(yt-y));var q=Quaternion.Euler(0f,0f,Mathf.Atan2(yt-y,dir*run)*Mathf.Rad2Deg);
        foreach(float sz in new[]{za+.03f,zb-.03f})
        {
            W.OBox(bandGraphite,new Vector3((xb+xt)*.5f,(y+yt)*.5f-.06f,sz),new Vector3(len,.26f,.06f),q,false,true);
            W.Rura(steel,new Vector3(xb,y+1.0f,sz),new Vector3(xt,yt+1.0f,sz),.025f,8);
            for(int k=0;k<=4;k++){float f=k/4f;var p=new Vector3(Mathf.Lerp(xb,xt,f),Mathf.Lerp(y,yt,f),sz);W.Rura(steel,p+Vector3.up*.08f,p+Vector3.up*1.0f,.02f,8);}
            W.Kolizja(new Vector3((xb+xt)*.5f,(y+yt)*.5f+.5f,sz),new Vector3(len,1.0f,.06f),q);
        }
        W.Rampa(new Vector3(xt,yt,zc),new Vector3(xb,y,zc),width);
        // the landing on the deck's edge, its guard on the open sides, two legs to the slab
        float l0=Mathf.Min(xt,xt+dir*StairLand),l1=Mathf.Max(xt,xt+dir*StairLand),lz0=Mathf.Min(za,ze),lz1=Mathf.Max(zb,ze),xe=xt+dir*StairLand;
        W.MM(steel,l0,l1,yt-.06f,yt,lz0,lz1);
        W.KolizjaMM(l0,l1,yt-.06f,yt,lz0,lz1);
        float zOut=zo>0f?zb-.03f:za+.03f;
        W.Rura(steel,new Vector3(xt,yt+1.0f,zOut),new Vector3(xe,yt+1.0f,zOut),.025f,8);
        W.Rura(steel,new Vector3(xe,yt+1.0f,zOut),new Vector3(xe,yt+1.0f,ze),.025f,8);
        foreach(var p in new[]{new Vector3(xe,yt,zOut),new Vector3(xe,yt,ze-Mathf.Sign(ze)*.05f)})W.Rura(steel,p,p+Vector3.up*1.0f,.02f,8);
        W.KolizjaMM(Mathf.Min(xt,xe),Mathf.Max(xt,xe),yt,yt+1.0f,zOut-.03f,zOut+.03f);
        W.KolizjaMM(xe-.03f,xe+.03f,yt,yt+1.0f,lz0,lz1);
        foreach(float lz in new[]{za+.10f,zb-.10f})W.Rura(bandGraphite,new Vector3(xe-dir*.10f,y,lz),new Vector3(xe-dir*.10f,yt-.06f,lz),.05f,8);
    }
    // the BRAXHORN helicopter (owner 16.11 22:00): the owner's EC135 in the BRAXHORN livery (black, the gold cheatline,
    // BRAXHORN and the coat of arms; model GRIP420, CC-BY 4.0, repainted by unity-tools/mistrzowie/helikopter) on the
    // ring, nose east, its right door - the pilot's - toward the south stair. A working helicopter: AKCJA at that door
    // (BraxChampionsHelikopter); the player's vehicle hook - a BraxChampionsCar in proxy mode - carries the joystick and the
    // follow camera, the hull's colliders under it (the camera never hits its own helicopter).
    public const string HeliModel="Assets/Source/Mistrzowie/Helikopter/braxhorn_ec135.glb";
    const string HeliTextures="Assets/Source/Mistrzowie/Helikopter/_tekstury/braxhorn_ec135";
    static void Helikopter(float yt)
    {
        if(!System.IO.File.Exists(HeliModel))
        {
            BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_HELICOPTER_MISSING "+HeliModel+" (pack braxhorn-mistrzowie-helikopter-20261116 not opened: KLUCZ) - the heliport stays empty");
            return;
        }
        // the textures first: glTFast takes the project's compressed textures instead of its own copies
        if(System.IO.Directory.Exists(HeliTextures))AssetDatabase.ImportAsset(HeliTextures,ImportAssetOptions.ImportRecursive|ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(HeliModel,ImportAssetOptions.ForceSynchronousImport);
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(HeliModel);
        if(!src){BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_HELICOPTER_IMPORT "+HeliModel);return;}
        var heli=new GameObject("HELIKOPTER BRAXHORN | Airbus EC135 (GRIP420, CC-BY 4.0) | latający").transform;
        heli.SetParent(outside.parent,false);heli.localPosition=new Vector3(0f,yt,0f);heli.localRotation=Quaternion.Euler(0f,90f,0f);
        var model=(GameObject)UnityEngine.Object.Instantiate(src,heli,false);model.name="BRAXHORN EC135 | model";
        model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
        BraxMiastoAssety.Przygotuj(model,false,true);
        var rs=model.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0){var lod=model.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.01f,rs)});lod.RecalculateBounds();}
        Transform Punkt(string name,Transform parent,Vector3 local){var p=new GameObject(name).transform;p.SetParent(parent,false);p.localPosition=local;return p;}
        // in the model's metres: the pilot's (right) seat - his hips over its cushion -, the right door on the deck, the
        // camera point over the cabin's back
        var fotel=Punkt("fotel pilota (prawy)",heli,new Vector3(.31f,1.28f,1.70f));
        var wejscie=Punkt("wejście (prawe drzwi)",heli,new Vector3(1.90f,0f,1.60f));
        var kam=Punkt("kamera (BraxChampionsCar w trybie helikoptera)",heli,new Vector3(0f,1.20f,-1.0f));
        var kol=Punkt("kolizje kadłuba",kam,-kam.localPosition);
        void Box(string name,Vector3 c,Vector3 s){var b=Punkt(name,kol,c);b.gameObject.AddComponent<BoxCollider>().size=s;}
        Box("kabina",new Vector3(0f,1.75f,.40f),new Vector3(1.70f,2.40f,5.60f));
        Box("belka ogonowa",new Vector3(0f,2.05f,-4.80f),new Vector3(.80f,1.10f,4.80f));
        Box("statecznik",new Vector3(0f,3.0f,-6.75f),new Vector3(.30f,1.80f,.90f));
        Box("płozy",new Vector3(0f,.25f,.20f),new Vector3(2.90f,.50f,3.40f));
        var car=kam.gameObject.AddComponent<BraxChampionsCar>();
        var h=heli.gameObject.AddComponent<BraxChampionsHelikopter>();
        car.helikopter=h;
        h.model=model.transform;h.fotel=fotel;h.wejscie=wejscie;h.kamera=car;
        h.wirnikGlowny=BraxMiastoAssety.Znajdz(model.transform,"wirnik_glowny");
        h.wirnikOgonowy=BraxMiastoAssety.Znajdz(model.transform,"wirnik_ogonowy");
        if(!h.wirnikGlowny||!h.wirnikOgonowy)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_HELICOPTER_ROTORS main="+(h.wirnikGlowny!=null)+" tail="+(h.wirnikOgonowy!=null)+" (nodes wirnik_glowny / wirnik_ogonowy)");
        foreach(var t in heli.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        helis++;
    }
    // ---------------- T09 SKY KASYNO ----------------
    // Owner 16.11 17:42: "na jednym wieżowcu kasyno na samej górze z widokiem na całe miasto" (plan 12.11: "Dwa kasyna:
    // KASYNO WAWER T11 i sky kasyno na szczycie wieżowca"). The top two storeys of the BRAXHORN TOWER (240-248 m over the
    // plaza, the highest room of the region) are one hall 8 m high in clear glass all round: the whole city below. The
    // express lift from the lobby (lift 1 of the bank, a working BraxWinda: three stops, the last the heliport) opens into it. Inside: dark oak
    // planks, a dark ceiling with lamps, blackjack tables made as real ones (red felt, a padded black leather armrest,
    // walnut body, steel chip tray, pedestals), two banks of working slot machines (mechanical and screen, BraxChampionsKasyno-
    // Sprzet), a bar with the owner's back-bar and lamps, lounges at the glass.
    public const int CasinoFloor=58;
    public static float CasinoY=>Shaft+CasinoFloor*Storey;
    const float LiftZ0=-2f,LiftZ1=.30f,LiftHalf=1.05f,LiftLeaf=.60f;
    static Material casinoGlass,oakDark,felt,leatherBlack,darkMetal;
    static Transform cab,cabL,cabR;static Transform[] landL,landR;static float liftX;static int slots,tables,roulettes,ownerMissing,obsluga;
    static void CasinoMaterials()
    {
        // clear glass both ways (seen from the hall too), the city's clear glass otherwise
        string path=outFolder+"/Materials/tower_sky_casino_clear_glass.mat";
        casinoGlass=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!casinoGlass){casinoGlass=new Material(gClear){name="Mistrzowie tower sky casino clear glass (both sides)"};casinoGlass.SetFloat("_Cull",0f);AssetDatabase.CreateAsset(casinoGlass,path);}
        // the owner's hall board (10.11): oak planks smoked a shade darker; Poly Haven velour for the felt (red, the
        // region's casino colour); black hide for the armrests; blackened steel for the pedestals
        oakDark=Src("sky_casino_oak","oak_wood_planks",2.2f,new Color(.50f,.44f,.42f),.6f);
        felt=Src("sky_casino_felt","velour_velvet",.6f,new Color(.82f,.80f,.82f),.95f);
        leatherBlack=BraxChampionsFoundation.LibraryLeather("tower_casino_leather_black",new Color(.30f,.52f,.70f));
        darkMetal=BraxChampionsFoundation.LibraryMetal("tower_casino_blackened_steel",new Color(.10f,.10f,.11f));
    }
    // one storey of the hall's glass: clear units, mullions with a back (seen from the hall), the sill / the 4 m transom,
    // the glass's collision; the roof deck over the top one
    static void CasinoStorey(List<Vector2> p,float y,bool roof)
    {
        var c=Centre(p);var bt=FrameAt(y);
        for(int i=0;i<p.Count;i++)
        {
            int j=(i+1)%p.Count;var n2=EdgeOut(p,i,c);var n=new Vector3(n2.x,0,n2.y);
            float L=(p[j]-p[i]).magnitude;int m=Mathf.Max(1,Mathf.RoundToInt(L/1.5f));
            Vector3 P(float t,float h)=>V3(Vector2.Lerp(p[i],p[j],t),y+h);
            for(int k=0;k<m;k++)UnitQuad(casinoGlass,P((float)k/m,0),P((float)(k+1)/m,0),P((float)(k+1)/m,Storey),P((float)k/m,Storey),n);
            for(int k=0;k<m;k++)Bar(bt,mullion,P((float)k/m,0),P((float)k/m,Storey),n,k==0?.10f:.065f,.14f,true);
            float cut=Mathf.Min(.45f,.08f/L);
            Bar(bt,mullion,P(cut,0),P(1f,0),n,.10f,.08f,true);
            W.Grupa=inside;W.Wnetrze=true;
            W.Kolizja(V3(Vector2.Lerp(p[i],p[j],.5f),y+Storey*.5f)-n*.15f,new Vector3(L,Storey,.30f),Quaternion.LookRotation(n,Vector3.up));
            W.Grupa=outside;W.Wnetrze=false;
        }
        if(roof)Cap(deck,p,y+Storey,true);
    }
    static void SkyCasino(List<Vector2> F)
    {
        float y=CasinoY,ceil=CasinoY+2f*Storey-Ceiling;
        W.Grupa=inside;W.Wnetrze=true;
        Cap(oakDark,F,y,true);Cap(ceilingBlack,F,ceil,false);
        W.KolizjaMM(-14f,14f,y-.30f,y,-14f,14f);
        // the core: walnut round the express lift's shaft, the lift opening to the hall (south, -z)
        LiftCore(new Core(-4f,-2f,10f,7f),liftX,y,ceil);
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"SKY KASYNO",new Vector3(-1.2f,y+3.2f,-2f),180f,.42f,.06f,.10f,5.0f);
        // ---- the bar on the core's back (z 5): back counter, the owner's back-bar, the bartender's walk, the counter
        const float bz=5f;
        kat.RoundedBox(walnut,new Vector3(1f,y+.48f,bz+.30f),new Vector3(9.0f,.96f,.60f),.02f);
        kat.RoundedBox(marble,new Vector3(1f,y+.98f,bz+.31f),new Vector3(9.1f,.04f,.62f),.012f);
        int units=OwnerBackBar(-3.2f,5.2f,y+1.0f,bz+.04f,y+3.1f);
        kat.RoundedBox(walnut,new Vector3(1f,y+.55f,7.35f),new Vector3(9.0f,1.10f,.70f),.03f);
        kat.RoundedBox(marble,new Vector3(1f,y+1.12f,7.30f),new Vector3(9.2f,.04f,.84f),.015f);
        kat.Tube(steel,new Vector3(-3.4f,y+.22f,7.84f),new Vector3(5.4f,y+.22f,7.84f),.025f,10);
        W.KolizjaMM(-3.5f,5.5f,y,y+1.14f,7.0f,7.72f);W.KolizjaMM(-3.5f,5.5f,y,y+1.0f,bz,bz+.6f);
        for(int i=0;i<7;i++)
        {
            float x=-2.6f+i*1.2f;
            Model("HotelSobczak/bar_chair_round_01/bar_chair_round_01_1k.gltf","sky casino bar stool",new Vector3(x,y,8.40f),0f);
            W.Grupa=inside;W.Siedzisko(new Vector3(x,y+.76f,8.40f),Vector3.back,"SKY KASYNO BAR");seats++;
        }
        for(int i=0;i<4;i++)OwnerModel("bar_lampa_markpeters","sky casino bar lamp",new Vector3(-2.0f+i*2.0f,y+2.55f,7.30f),0f,.50f,ceil);
        // ---- the slot machines, working (BraxChampionsKasynoSprzet / BraxChampionsAutomaty; owner 16.11 18:19 "mechaniczne i
        // komputerowe ... mogą normalnie działać", 19:13 "różnorodne ... co druga różnego typu"): a bank west and a bank east of
        // the lift, each two rows of six back to back with their stools - mechanical and screen machines in turn, each in
        // another of the six themes of the owner's strips - the linked jackpot board hanging over each; as KASYNO WAWER
        W.Grupa=inside;W.Wnetrze=true;
        BraxChampionsFoundation.Optional("SKY KASYNO slot machines",()=>
        {
            // 16.11 20:28 "różne gry w automatach ... lepiej rozstawić": the banks play the five games in turn, and north of each a
            // CAROUSEL of eight round a drum with the owner's toppers on its crown - KOŁO MISTRZÓW west, VIDEO POKER / KENO east
            var kas=BraxChampionsKasynoSprzet.Zacznij(inside,kat,"SKY KASYNO");
            const int Mech=BraxChampionsAutomatyGry.Mech,Video=BraxChampionsAutomatyGry.Video,Poker=BraxChampionsAutomatyGry.Poker,
                Keno=BraxChampionsAutomatyGry.Keno,Kolo=BraxChampionsAutomatyGry.Kolo;
            BraxChampionsKasynoSprzet.Bank(kas,new Vector3(-9.6f,y,-4.5f),0f,6,0,ceil,new[]{Mech,Video,Kolo,Poker});
            BraxChampionsKasynoSprzet.Bank(kas,new Vector3(9.8f,y,-4.5f),0f,6,1,ceil,new[]{Video,Keno,Mech,Kolo});
            BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(-9.5f,y,2.0f),8,new[]{Kolo,Video},0,2.95f);
            BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(9.8f,y,2.0f),8,new[]{Poker,Keno},0,2.95f);
            // the service (owner 20:29 "porządna recepcja ... szatnia"): the hostess' podium by the express lift, the cloakroom and
            // the cashier's cage in the south-west corner by the glass; the owner's signs hang over the roulette and the blackjack
            BraxChampionsKasynoSprzet.Recepcja(kas,new Vector3(-3.0f,y,-3.6f),90f,1.4f,false);
            BraxChampionsKasynoSprzet.Szatnia(kas,new Vector3(-9.8f,y,-12.0f),0f,2.0f,1,true);
            BraxChampionsKasynoSprzet.Kasa(kas,new Vector3(-12.15f,y,-9.3f),90f,2);
            BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(0f,y+3.4f,-9.4f),BraxChampionsKasynoSprzet.ArkRuletka,1.6f,ceil);
            foreach(float x in new[]{-6.6f,6.6f})
                BraxChampionsKasynoSprzet.SzyldWiszacy(kas,new Vector3(x,y+3.4f,-9.6f),BraxChampionsKasynoSprzet.ArkBlackjack,1.4f,ceil);
            obsluga=kas.punkty.Count;
            BraxChampionsKasynoSprzet.Ruletka(kas,new Vector3(0f,y,-9.4f),0f);
            BraxChampionsKasynoSprzet.Zakoncz(kas);slots=kas.maszyny.Count;roulettes=kas.ruletki.Count;
        });
        W.Grupa=inside;W.Wnetrze=true;
        // ---- blackjack tables to the south (the street side, the glass behind the players)
        foreach(float x in new[]{-6.6f,6.6f})BlackjackTable(new Vector2(x,-9.6f));
        // (the roulette between them - owner 16.11 19:13 "ruletki prawdziwe" - is set with the slot machines above: a European
        // table at x 0, the players to the hall)
        // ---- lounges at the west and east glass, the sofas facing the city; plants
        Lounge(new Vector2(-10.5f,8.5f),false,"sky casino lounge W","SOFA SKY KASYNO");
        Lounge(new Vector2(10.5f,8.5f),true,"sky casino lounge E","SOFA SKY KASYNO");
        W.Grupa=inside;W.Wnetrze=true;
        foreach(var pp in new[]{new Vector2(-13.3f,-6.8f),new Vector2(11f,-11f),new Vector2(-11f,11f),new Vector2(11f,11f)})
            Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf","sky casino plant",new Vector3(pp.x,y,pp.y),pp.x*13f);
        // ---- light: lamps on the dark ceiling over the tables and the lounges, the light of the hall
        foreach(var lp in new[]{new Vector2(-6.6f,-9.6f),new Vector2(0f,-9.6f),new Vector2(6.6f,-9.6f),new Vector2(-10.5f,8.5f),new Vector2(10.5f,8.5f)})
            Model("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf","sky casino ceiling lamp",new Vector3(lp.x,ceil,lp.y),0f,hang:true);
        foreach(var lp in new[]{new Vector3(-6.6f,4.6f,-9.6f),new Vector3(0f,4.6f,-9.6f),new Vector3(6.6f,4.6f,-9.6f),new Vector3(-9.6f,4.6f,-4.5f),
                                new Vector3(9.8f,4.6f,-4.5f),new Vector3(1f,3.6f,7.3f),new Vector3(-10.5f,4.6f,8.5f),new Vector3(10.5f,4.6f,8.5f)})
            lights.Add(W.Lampa("SKY KASYNO light",new Vector3(lp.x,y+lp.y,lp.z),1.0f,11f,new Color(.97f,.98f,1f)));
        Debug.Log("TOWER_T09_SKY_KASYNO floor="+CasinoY.ToString("F2")+"m nominal glass=clear-both-sides tables="+tables+" roulettes="+roulettes+" slots="+slots+
            " backBarUnits="+units+" ownerModelsMissing="+ownerMissing+" lift=BraxWinda("+Stops().Length+" stops)");
        if(ownerMissing>0)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_SKY_KASYNO owner models missing x"+ownerMissing+" (osiedle_wnetrza pack): back-bar / bar lamps stay empty");
        if(slots!=40)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_SKY_KASYNO slot machines "+slots+"/40");
        if(obsluga!=4)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_SKY_KASYNO service points "+obsluga+"/4 (reception, cloakroom, two cashier windows)");
    }
    // a blackjack table as real ones are (2.2 x 1.05 m, felt at .78 m): a half-ellipse of red felt, a padded black leather
    // armrest round the players' arc, a walnut apron under it, the dealer's straight walnut edge with a steel chip tray, two
    // blackened steel pedestals; five dining chairs (Poly Haven) round the arc, the dealer standing on the straight side (+z)
    static void BlackjackTable(Vector2 at)
    {
        float y=CasinoY,top=.78f;const float A=1.10f,B=.95f,Rail=.11f,RailH=.075f;
        Vector3 L(float x,float h,float z)=>new Vector3(at.x+x,y+h,at.y+z);
        Vector2 Arc(float t,float k)=>new Vector2(A*k*Mathf.Cos(t),-B*k*Mathf.Sin(t));
        Vector2 Out(float t){var v=new Vector2(Mathf.Cos(t)/A,-Mathf.Sin(t)/B);return v.normalized;}
        void Up(Material m,Vector3 a,Vector3 b,Vector3 c,bool up){var n=Vector3.Cross(b-a,c-a);if((n.y<0)==up)kat.Triangle(m,a,c,b);else kat.Triangle(m,a,b,c);}
        const int N=28;float kIn=1f-Rail/A*.9f;
        for(int i=0;i<N;i++)
        {
            float t0=Mathf.PI*i/N,t1=Mathf.PI*(i+1)/N;var a0=Arc(t0,kIn);var a1=Arc(t1,kIn);
            Up(felt,L(0,top,.02f),L(a0.x,top,a0.y),L(a1.x,top,a1.y),true);                     // the felt
            var b0=Arc(t0,1f);var b1=Arc(t1,1f);
            Up(walnut,L(0,top-.16f,.02f),L(b0.x,top-.16f,b0.y),L(b1.x,top-.16f,b1.y),false);  // the underside
        }
        // the padded armrest: a half-round roll of leather on the arc's rim (out of the felt's plane by its own height)
        kat.Surface(leatherBlack,(u,v)=>{float t=Mathf.PI*u,ph=Mathf.PI*v;var c=Arc(t,1f-Rail*.5f/A);var o=Out(t);
            float r=-Mathf.Cos(ph)*Rail*.5f,h=Mathf.Sin(ph)*RailH;return L(c.x+o.x*r,top-.005f+h,c.y+o.y*r);},N,6,true);
        // the walnut apron under it, straight down to the underside
        kat.Surface(walnut,(u,v)=>{float t=Mathf.PI*u;var c=Arc(t,1f);return L(c.x,top-.16f+v*.155f,c.y);},N,1);
        // the dealer's side: a straight walnut edge, the steel chip tray let into it, the felt's strip to it
        kat.RoundedBox(walnut,L(0,top-.08f,.10f),new Vector3(2*A+.02f,.16f,.16f),.02f);
        kat.RoundedBox(steel,L(0,top+.012f,.10f),new Vector3(.62f,.024f,.12f),.006f);
        foreach(float sx in new[]{-1f,1f})kat.RoundedBox(darkMetal,L(sx*.55f,(top-.16f)*.5f,-.30f),new Vector3(.14f,top-.16f,.46f),.02f);
        W.Grupa=inside;W.Wnetrze=true;W.KolizjaMM(at.x-A,at.x+A,y,y+top,at.y-B,at.y+.18f);
        for(int k=0;k<5;k++)
        {
            float t=Mathf.PI*(k+.5f)/5f;var p=Arc(t,1f)+Out(t)*.42f;var face=Out(t)*-1f;
            Model("HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf","sky casino blackjack chair",new Vector3(at.x+p.x,y,at.y+p.y),Mathf.Atan2(face.x,face.y)*Mathf.Rad2Deg);
            W.Grupa=inside;W.Siedzisko(new Vector3(at.x+p.x,y+.47f,at.y+p.y),new Vector3(face.x,0,face.y),"SKY KASYNO BLACKJACK");seats++;
        }
        tables++;
    }
    // one of the owner's models (osiedle_wnetrza: CC BY, decrypted in CI): base centred on at (or hung so its top is at
    // hangTo), fitted to fitHeight metres, turned by yaw; null and counted when the pack is not here
    static GameObject OwnerModel(string file,string label,Vector3 at,float yaw,float fitHeight,float hangTo=float.NaN)
    {
        var src=BraxMiastoAssety.Model("osiedle_wnetrza",file);
        if(!src){ownerMissing++;return null;}
        try
        {
            var o=(GameObject)UnityEngine.Object.Instantiate(src,inside,false);o.name="TOWER | "+label+" | "+file+" (biblioteka właściciela, CC BY)";
            o.transform.localPosition=Vector3.zero;o.transform.localRotation=Quaternion.Euler(0,yaw,0)*src.transform.localRotation;o.transform.localScale=src.transform.localScale;
            var b=BraxMiastoAssety.Granice(o);
            if(fitHeight>0f&&b.size.y>1e-4f){o.transform.localScale*=fitHeight/b.size.y;b=BraxMiastoAssety.Granice(o);}
            var t=inside.TransformPoint(at);
            o.transform.position+=new Vector3(t.x-b.center.x,t.y-b.min.y,t.z-b.center.z);
            if(!float.IsNaN(hangTo))
            {
                // a pendant: a steel cable from its top to the ceiling
                var bb=BraxMiastoAssety.Granice(o);var c=inside.InverseTransformPoint(bb.center);float topY=inside.InverseTransformPoint(bb.max).y;
                if(hangTo>topY+.05f)kat.Tube(steel,new Vector3(c.x,topY,c.z),new Vector3(c.x,hangTo,c.z),.004f,6);
            }
            BraxMiastoAssety.Przygotuj(o,true,true);models++;return o;
        }
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI TOWER_OWNER_MODEL "+file+" "+e.Message);ownerMissing++;return null;}
    }
    // the owner's back-bar units (windupbird) standing on the back counter between xa and xb, fronts to the bar (+z), the
    // bottles at their real height (.33 m, as E1's bar)
    static int OwnerBackBar(float xa,float xb,float y0,float zBack,float yTop)
    {
        var src=BraxMiastoAssety.Model("osiedle_wnetrza","bar_polki_butelki_windupbird");if(!src){ownerMissing++;return 0;}
        try
        {
            var tmp=(GameObject)UnityEngine.Object.Instantiate(src);tmp.transform.position=Vector3.zero;tmp.transform.rotation=src.transform.localRotation;
            float maxB=0f;
            foreach(var t in tmp.GetComponentsInChildren<Transform>(true))
                if(t!=tmp.transform&&(t.name.StartsWith("Object")||t.name.StartsWith("Box"))){float h=BraxMiastoAssety.Granice(t.gameObject).size.y;if(h>maxB&&h<BraxMiastoAssety.Granice(tmp).size.y*.6f)maxB=h;}
            var b0=BraxMiastoAssety.Granice(tmp);UnityEngine.Object.DestroyImmediate(tmp);if(maxB<=0f)maxB=b0.size.y*.2f;
            float s=Mathf.Min(.33f/maxB,(yTop-y0)/Mathf.Max(1e-4f,b0.size.y));
            bool longX=b0.size.x>=b0.size.z;float yaw=longX?0f:90f;float wr=Mathf.Max(b0.size.x,b0.size.z)*s,Lr=xb-xa;
            int n=Mathf.Clamp(Mathf.FloorToInt((Lr+.1f)/(wr+.1f)),1,12);float step=Lr/n;
            for(int i=0;i<n;i++)
            {
                var o=(GameObject)UnityEngine.Object.Instantiate(src,inside,false);o.name="TOWER | sky casino back-bar "+(i+1)+" | bar_polki_butelki_windupbird (biblioteka właściciela, CC BY)";
                o.transform.localPosition=Vector3.zero;o.transform.localRotation=Quaternion.Euler(0,yaw,0)*src.transform.localRotation;o.transform.localScale=src.transform.localScale*s;
                var b=BraxMiastoAssety.Granice(o);var t=inside.TransformPoint(new Vector3(xa+step*(i+.5f),y0,zBack));
                o.transform.position+=new Vector3(t.x-b.center.x,t.y-b.min.y,t.z-b.min.z);
                BraxMiastoAssety.Przygotuj(o,true,true);models++;
            }
            return n;
        }
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI TOWER_OWNER_BACKBAR "+e.Message);ownerMissing++;return 0;}
    }
    // a lift core in walnut round one working lift's shaft (x = the shaft's centre): the walls beside and over the doors,
    // a brushed stainless portal 3 cm proud, the call panel
    static void LiftCore(Core core,float x,float y0,float y1)
    {
        float s0=x-LiftHalf,s1=x+LiftHalf,d0=x-LiftLeaf,d1=x+LiftLeaf,front=core.yMin,wall=front+.20f;
        W.Grupa=inside;W.Wnetrze=true;
        W.MM(walnut,core.xMin,s0,y0,y1,core.yMin,core.yMax,true);
        W.MM(walnut,s1,core.xMax,y0,y1,core.yMin,core.yMax,true);
        W.MM(walnut,s0,s1,y0,y1,front+(LiftZ1-LiftZ0),core.yMax,true);
        W.MM(walnut,s0,d0,y0,y1,front,wall,true);W.MM(walnut,d1,s1,y0,y1,front,wall,true);
        W.MM(walnut,d0,d1,y0+2.25f,y1,front,wall,true);
        W.MM(steel,d0-.08f,d0,y0,y0+2.25f,front-.03f,front);W.MM(steel,d1,d1+.08f,y0,y0+2.25f,front-.03f,front);
        W.MM(steel,d0-.08f,d1+.08f,y0+2.25f,y0+2.33f,front-.03f,front);
        W.MM(steel,d1+.20f,d1+.32f,y0+1.0f,y0+1.25f,front-.03f,front);
    }
    // the express lift: a cabin built at its floor y = 0 (BraxWinda carries it to a stop), its doors, the landing doors of the
    // lobby and of the sky casino (each leaf its own object: they slide)
    static void ExpressLift(float x)
    {
        liftX=x;float zf=LiftZ0+.34f,zb=LiftZ1-.06f;
        cab=new GameObject("BRAXHORN TOWER | SKY KASYNO express lift | cabin (BraxWinda)").transform;cab.SetParent(inside,false);
        W.Grupa=cab;W.Wnetrze=true;
        W.MM(marble,x-.98f,x+.98f,-.12f,.01f,zf-.08f,zb+.05f,true);
        W.MM(walnut,x-.98f,x+.98f,.01f,2.5f,zb,zb+.05f,true);
        W.MM(walnut,x-.98f,x-.93f,.01f,2.5f,zf,zb,true);W.MM(walnut,x+.93f,x+.98f,.01f,2.5f,zf,zb,true);
        W.MM(steel,x-.98f,x+.98f,2.5f,2.56f,zf-.08f,zb+.05f);W.MM(ledWhite,x-.55f,x+.55f,2.47f,2.5f,zf+.40f,zb-.40f);
        W.MM(steel,x-.93f,x-LiftLeaf,.01f,2.5f,zf-.04f,zf,true);W.MM(steel,x+LiftLeaf,x+.93f,.01f,2.5f,zf-.04f,zf,true);
        W.MM(steel,x-LiftLeaf,x+LiftLeaf,2.25f,2.5f,zf-.04f,zf,true);
        W.MM(black,x+.70f,x+.86f,1.0f,1.45f,zf,zf+.02f);
        W.Rura(steel,new Vector3(x-.80f,.95f,zb-.06f),new Vector3(x+.80f,.95f,zb-.06f),.02f);
        W.Lampa("SKY KASYNO lift cabin light",new Vector3(x,2.2f,(zf+zb)*.5f),.7f,3f,new Color(.97f,.98f,1f));
        cabL=new GameObject("cabin door L").transform;cabL.SetParent(cab,false);W.Grupa=cabL;W.MM(steel,x-LiftLeaf,x,.01f,2.25f,zf-.08f,zf-.05f,true);
        cabR=new GameObject("cabin door R").transform;cabR.SetParent(cab,false);W.Grupa=cabR;W.MM(steel,x,x+LiftLeaf,.01f,2.25f,zf-.08f,zf-.05f,true);
        var ys=Stops();landL=new Transform[ys.Length];landR=new Transform[ys.Length];
        for(int s=0;s<ys.Length;s++)
        {
            float yy=ys[s],z0=LiftZ0+.22f;
            landL[s]=new GameObject("SKY KASYNO lift landing door L | stop "+s).transform;landL[s].SetParent(inside,false);W.Grupa=landL[s];
            W.MM(steel,x-LiftLeaf,x,yy,yy+2.25f,z0,z0+.03f,true);
            landR[s]=new GameObject("SKY KASYNO lift landing door R | stop "+s).transform;landR[s].SetParent(inside,false);W.Grupa=landR[s];
            W.MM(steel,x,x+LiftLeaf,yy,yy+2.25f,z0,z0+.03f,true);
        }
        W.Grupa=inside;
    }
    // the express lift's stops (nominal y of their floors): the lobby, the sky casino and - on the BRAXHORN TOWER - the
    // heliport's pavilion
    static float[] Stops()=>float.IsNaN(heliY)?new[]{Fl,CasinoY}:new[]{Fl,CasinoY,heliY};
    static readonly string[] StopNames={"HOL · BRAXHORN TOWER","SKY KASYNO · 60 PIĘTRO","LĄDOWISKO · DACH"};
    // after the root is placed: the lift's stops in world y, its call points, the BraxWinda itself
    static void WireLift(Transform root)
    {
        if(!cab)return;
        foreach(var t in cab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        foreach(var arr in new[]{landL,landR})foreach(var l in arr)foreach(var t in l.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        var w=cab.gameObject.AddComponent<BraxWinda>();
        w.nazwa="BRAXHORN TOWER · SKY KASYNO";
        w.kabina=cab;w.kabinaL=cabL;w.kabinaP=cabR;
        var ys=Stops();int n=ys.Length;
        w.pietraY=new float[n];w.pietraNazwy=new string[n];w.uzytkowe=new bool[n];w.wezwanie=new Vector3[n];
        for(int i=0;i<n;i++)
        {
            w.pietraY[i]=root.TransformPoint(new Vector3(0,ys[i],0)).y;w.pietraNazwy[i]=StopNames[i];w.uzytkowe[i]=true;
            w.wezwanie[i]=root.TransformPoint(new Vector3(liftX,ys[i]+1.2f,LiftZ0-.9f));
        }
        w.przystanekL=landL;w.przystanekP=landR;
        var sh=root.TransformPoint(new Vector3(liftX,0,(LiftZ0+LiftZ1)*.5f));w.szyb=new Vector2(sh.x,sh.z);
        w.wnetrze=new Vector2(.93f*K,.95f*K);w.przesuw=LiftLeaf*.95f;w.start=0;
    }
    // BRAXHORN BET (plan 12.11 "punkt BRAXHORN BET"): the west wing of the BRAXHORN TOWER ground floor, its own door on
    // the street. A dark sportsbook: black granite floor, a black ceiling with white light lines, a video wall of fifteen
    // 65" screens and nine 55" along the tills (real TVs, switched off: no films in street premises), the name in green
    // light over it, the betting counter with three tills along the partition, a lounge and a row of armchairs facing the
    // screens, six high tables with stools, pendants.
    static void Bet(List<Vector2> F,float split)
    {
        float y0=Fl,ceil=Shaft-Ceiling;
        W.Grupa=inside;W.Wnetrze=true;
        // the partition from the lobby (30 cm): black granite on the BET side, white marble on the lobby side (two halves
        // meeting back to back); the back wall of the screens at z=15
        W.MM(granite,split-.30f,split-.15f,y0,ceil,-19.9f,19.9f);
        W.MM(marble,split-.15f,split,y0,ceil,-19.9f,19.9f);
        W.KolizjaMM(split-.30f,split,y0,ceil,-19.9f,19.9f);
        W.MM(granite,-18.8f,split-.30f,y0,ceil,15f,15.3f,true);
        var ctx=Ctx();
        // the video wall: fifteen 65" screens (5 x 3) on the back wall, nine 55" over the counter and along the partition.
        // Owner 11.11 21:48: no films in street premises - the screens are real TVs, switched off (black glass); they are
        // where the fights of the BRAXHORN BET league will be shown
        float cx=(-18.8f+split-.30f)*.5f;
        foreach(float yy in new[]{2.35f,3.21f,4.07f})for(int i=-2;i<=2;i++)
            BetScreen(ctx,new Vector3(cx+i*1.48f,y0+yy,15f),Vector3.back,65f,"BRAXHORN BET ekran sciana");
        for(int i=0;i<9;i++)BetScreen(ctx,new Vector3(split-.30f,y0+2.95f,-17f+i*3.5f),Vector3.left,55f,"BRAXHORN BET ekran przy kasach");
        BraxChampionsLetters3D.Text(letters,ledGreen,black,"BRAXHORN BET",new Vector3(cx,y0+5.0f,15f),180f,.70f,.08f,.12f,8f);
        // the counter: walnut with a marble top along the partition, z 1..9, the staff behind it; three tills
        W.MM(walnut,split-1.30f-.80f,split-1.30f,y0+.08f,y0+1.06f,1f,9f,true);
        W.MM(granite,split-1.30f-.74f,split-1.30f-.06f,y0,y0+.08f,1.06f,8.94f);
        W.MM(marble,split-1.30f-.85f,split-1.30f+.05f,y0+1.06f,y0+1.10f,.95f,9.05f);
        for(int i=0;i<3;i++)BraxKatalog.Monitor(ctx,new Vector3(split-1.80f,y0+1.10f,2.5f+i*2.6f),Vector3.right,i,24f);
        // the lounge facing the screens: a sofa and two lounge chairs round a table, z 7..11
        Model("ArenaLobby/sofa_02/sofa_02_2k.gltf","BET sofa",new Vector3(-13.2f,y0,9.6f),0f);
        for(int i=-1;i<=1;i++){W.Siedzisko(new Vector3(-13.2f+i*.6f,y0+.45f,9.55f),Vector3.forward,"BRAXHORN BET");seats++;}
        Model("HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf","BET table",new Vector3(-13.2f,y0,11.2f),0f);
        foreach(float s in new[]{-1f,1f})Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf","BET lounge chair",new Vector3(-13.2f+s*2.2f,y0,10.6f),-s*30f);
        // a row of armchairs facing the video wall, side tables between the pairs
        foreach(float x in new[]{-17.6f,-16.0f,-11.8f,-10.2f})
        {
            Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf","BET armchair",new Vector3(x,y0,5.8f),0f);
            W.Siedzisko(new Vector3(x,y0+.45f,5.75f),Vector3.forward,"BRAXHORN BET");seats++;
        }
        foreach(float x in new[]{-16.8f,-11.0f})Model("HotelSobczak/side_table_01/side_table_01_1k.gltf","BET side table",new Vector3(x,y0,5.9f),0f);
        // high tables: a black steel post on a round foot, a walnut top 70 cm, two bar stools each, a pendant over each
        // (16.11: the west middle one gave way to the LOTOTO studio)
        foreach(var (hx,z) in new[]{(-13.2f,-1.5f),(-13.2f,-6.5f),(-13.2f,-11.5f),(-17.4f,-4.0f),(-17.4f,-14.0f)})
        {
            var p=new Vector3(hx,y0,z);
            W.Walec(black,p,.25f,.02f,24);W.Walec(black,p+Vector3.up*.02f,.04f,1.01f,12);W.Walec(walnut,p+Vector3.up*1.03f,.35f,.04f,32);
            W.KolizjaMM(p.x-.36f,p.x+.36f,y0,y0+1.07f,z-.36f,z+.36f);
            foreach(float s in new[]{-1f,1f})
            {
                var sp=p+new Vector3(s*.75f,0,0);
                Model("HotelSobczak/bar_chair_round_01/bar_chair_round_01_1k.gltf","BET bar stool",sp,s>0?-90f:90f);
                W.Siedzisko(sp+Vector3.up*.78f,Vector3.forward,"BRAXHORN BET");seats++;
            }
            Model("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf","BET pendant",new Vector3(hx,ceil,z),0f,hang:true);
        }
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf","BET plant",new Vector3(-17.5f,y0,13.6f),0f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf","BET plant",new Vector3(split-1.0f,y0,-17.6f),40f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf","BET plant",new Vector3(-8.6f,y0,13.6f),0f);
        foreach(float z in new[]{-14f,-6f,2f,10f})foreach(float x in new[]{-16.4f,-10.2f})
            lights.Add(W.Lampa("BRAXHORN BET light",new Vector3(x,ceil-1.0f,z),.9f,11f,new Color(.96f,.98f,1f)));
        // LOTOTO BRAX CASH (owner 16.11 22:52-23:08: "Kolektura ... i w telewizorach losowanie", the studio "w tym BRAXHORN
        // BET", his poster): the draw studio against the west glass (where a high table stood), the kolektura's counter by the
        // partition south of the tills, the coupon desk and his poster on a stand in the hall; every screen of the premises
        // shows the studio's camera - a live picture of the game, not a film (BraxChampionsLototo)
        W.Grupa=inside;W.Wnetrze=true;
        lototo=BraxChampionsKasynoSprzet.Lototo(inside,kat,new Vector3(-17.4f,y0,-9.2f),90f,new Vector3(split-2.37f,y0,-4.2f),90f,
            new Vector3(-11.4f,y0,-3.6f),270f,new Vector3(-11.2f,y0,-8.4f),180f,betEkrany);
    }
    // a BRAXHORN BET screen: the catalogue TV on the wall, its picture the black glass of a TV that is off and NO film player
    // (owner 11.11 21:48: films only in flats, hotels, apartments - never in street premises)
    // (16.11 23:03: the screens show the LOTOTO studio's camera - the draws live and the results; still never a film)
    static readonly List<Renderer> betEkrany=new List<Renderer>();static BraxChampionsLototo lototo;
    static void BetScreen(BraxKatalog.Ctx ctx,Vector3 p,Vector3 f,float inches,string label)
    {
        BraxKatalog.Materials();
        var before=new HashSet<BraxTelewizor>(inside.GetComponentsInChildren<BraxTelewizor>(true));
        BraxKatalog.Tv(ctx,p,f,inches,0,false,label,BraxKatalog.GlassDark);
        foreach(var tv in inside.GetComponentsInChildren<BraxTelewizor>(true))
        {
            if(before.Contains(tv))continue;
            tv.gameObject.name=label+" | ekran bez filmów: studio LOTOTO BRAX CASH na żywo (transmisje walk ligi BRAXHORN BET)";
            var r=tv.GetComponent<Renderer>();if(r)betEkrany.Add(r);
            UnityEngine.Object.DestroyImmediate(tv);
        }
        betScreens++;
    }

    // ---------------- T10: elliptical ----------------
    static float EllipseTower(Tower t)
    {
        var F=Ellipse(21f,15f,5.5f);float front=F[0].y;
        foreach(var p in F)if(p.y<front)front=p.y;
        var core=new Core(-5f,-2f,10f,8f);
        GroundFloor(F,front,new[]{0f},core);
        LobbyFitOut(core,new Vector2(9.5f,-7.5f),new Vector2(-11.5f,-4.5f),true);
        LightLines(F,-21f,21f,4f,core);
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;
        // as the Absolute World towers: white slab edges standing .60 m out round the whole ellipse, the glass set back
        var skin=BandSkin(new Color(.17f,.22f,.26f),bandWhite,.60f);
        for(int f=0;f<t.floors;f++,y+=Storey)CurtainStorey(F,F,y,skin,f,f==t.floors-1?deck:null);
        // the crown: a ring of white blades 8 m high every 2 m round the ellipse, a graphite lid
        W.Grupa=details;Blades(bandWhite,F,y,y+8f,2.0f);W.Grupa=outside;
        Prism(bandGraphite,bandGraphite,bandGraphite,Offset(F,.60f),y+7.4f,y+8.2f);
        W.KolizjaMM(-15f,15f,Shaft,y,-10f,10f);
        return y+8.2f;
    }

    // ---------------- T12: three stacked blocks ----------------
    static float Stacked(Tower t)
    {
        var A=Box4(18f,13f);var B=Box4(13f,18f);
        var core=new Core(-5f,-2f,10f,9f);
        // the west wing is WAWER NIERUCHOMOŚCI (16.11 23:12-23:13), its own door on the street at x -12; the lobby's lounge east
        GroundFloor(A,-13f,new[]{0f,-12f},core);
        LobbyFitOut(core,new Vector2(10f,-8f),new Vector2(12.5f,4.5f),false,4,OfficeX);
        Nieruchomosci(A);
        LightLines(A,-18f,18f,4f,core);
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;int per=t.floors/3;
        // white aluminium slab edges .25 m out, neutral grey coated glass
        var skin=BandSkin(new Color(.17f,.18f,.20f),bandWhite,.25f);
        for(int b=0;b<3;b++)
        {
            var g=b==1?B:A;
            for(int f=0;f<per;f++,y+=Storey)CurtainStorey(g,g,y,skin,b*per+f,f==per-1?(b==2?deck:teak):null);
            if(b<2)Terraces(g,b==1?A:B,y);
        }
        // the top: a graphite parapet
        Prism(bandGraphite,bandGraphite,bandGraphite,Offset(A,Proud),y,y+1.2f);
        W.KolizjaMM(-18f,18f,Shaft,y,-18f,18f);
        return y+1.2f;
    }
    // WAWER NIERUCHOMOŚCI, the real estate office of Patryk and Monika (owner 16.11 23:12 with her poster "Poznaj Monikę ...
    // Prowadzę biuro nieruchomości w Mieście Mistrzów"): the T12's ground floor west of the partition at x -6 (walnut on its
    // side, white marble on the lobby's - two halves back to back), its name in white light on the canopy over its door,
    // the fit-out by BraxChampionsKasynoSprzet.Biuro; the chairs and the lounge are the tower's real-size models
    const float OfficeX=-6f;
    static void Nieruchomosci(List<Vector2> F)
    {
        float y0=Fl,ceil=Shaft-Ceiling,x0=-18f,z0=-13f,z1=13f;
        W.Grupa=inside;W.Wnetrze=true;
        W.MM(walnut,OfficeX-.30f,OfficeX-.15f,y0,ceil,z0+.1f,z1-.1f);
        W.MM(marble,OfficeX-.15f,OfficeX,y0,ceil,z0+.1f,z1-.1f);
        W.KolizjaMM(OfficeX-.30f,OfficeX,y0,ceil,z0+.1f,z1-.1f);
        BraxChampionsKasynoSprzet.Biuro(inside,kat,x0+.2f,OfficeX-.30f,z0+.15f,z1-.15f,y0,-12f);
        // her desk's chairs: two for the clients before it, hers behind it; a lounge at the back; plants
        foreach(float sx in new[]{-.55f,.55f})
        {
            Model("HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf","WAWER NIERUCHOMOŚCI client chair",new Vector3(-12f+sx,y0,z0+.15f+6.2f-1.05f),0f);
            W.Grupa=inside;W.Siedzisko(new Vector3(-12f+sx,y0+.46f,z0+.15f+6.2f-1.0f),Vector3.forward,"WAWER NIERUCHOMOŚCI");seats++;
        }
        Model("HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf","WAWER NIERUCHOMOŚCI Monika's chair",new Vector3(-12f,y0,z0+.15f+6.2f+.75f),180f);
        Lounge(new Vector2(-11.5f,7.5f),true,"WAWER NIERUCHOMOŚCI","SOFA W BIURZE");
        foreach(var (px,pz) in new[]{(-17.2f,-11.8f),(-7.0f,-11.8f),(-7.0f,11.8f)})Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf","WAWER NIERUCHOMOŚCI plant",new Vector3(px,y0,pz),px*13f);
        float edge=-13f-CanopyOut;
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"WAWER NIERUCHOMOŚCI",new Vector3(-12f,Fl+CanopyTop,edge+.10f),180f,.42f,.10f,.10f,5.6f);
    }
    // the roof of a block where the next block does not cover it: a teak terrace (the block's roof cap) with a glass
    // balustrade on its open edges and granite planters
    static void Terraces(List<Vector2> lower,List<Vector2> upper,float y)
    {
        W.Grupa=details;
        float lx=lower[1].x,lz=lower[2].y,ux=upper[1].x,uz=upper[2].y;
        if(lx>ux)foreach(float s in new[]{-1f,1f})
        {
            float x0=s<0?-lx+.15f:ux,x1=s<0?-ux:lx-.15f,xb=s<0?-lx+.15f:lx-.15f;
            W.MM(gClear,xb-.012f,xb+.012f,y,y+1.10f,-lz+.15f,lz-.15f);
            W.MM(granite,(x0+x1)*.5f-.6f,(x0+x1)*.5f+.6f,y,y+.5f,-lz+3f,-lz+6f);
        }
        if(lz>uz)foreach(float s in new[]{-1f,1f})
        {
            float zb=s<0?-lz+.15f:lz-.15f;
            W.MM(gClear,-lx+.15f,lx-.15f,y,y+1.10f,zb-.012f,zb+.012f);
        }
        W.Grupa=outside;
    }

    // ---------------- T13: tapering under a diagrid ----------------
    static float Taper(Tower t)
    {
        const float h0=20f,h1=13f;
        var F=Box4(h0,h0);
        var core=new Core(-6f,-2f,12f,12f);
        GroundFloor(F,-h0,new[]{0f},core);
        LobbyFitOut(core,new Vector2(11f,-10f),new Vector2(-12.5f,-9.5f),true);
        LightLines(F,-h0,h0,4f,core);
        W.Grupa=outside;W.Wnetrze=false;
        float top=Shaft+t.floors*Storey;
        float Half(float yy)=>Mathf.Lerp(h0,h1,(yy-Shaft)/(top-Shaft));
        float y=Shaft;
        // as the Hearst Tower: clear blue coated glass behind a stainless diagrid
        var skin=GlassSkin(new Color(.17f,.22f,.28f));
        for(int f=0;f<t.floors;f++,y+=Storey)
            CurtainStorey(Box4(Half(y),Half(y)),Box4(Half(y+Storey),Half(y+Storey)),y,skin,f,f==t.floors-1?deck:null);
        // the diagrid: diamonds four storeys high, eight half-steps across each face, ring beams at the nodes, corner
        // columns - steel members 60 x 50 cm, 35 cm off the glass
        W.Grupa=details;
        int levels=t.floors/4;
        Vector3 Pt(int face,float s,float yy,float off)
        {
            float h=Half(yy)+off;
            switch(face){case 0:return new Vector3(s*h,yy,-h);case 1:return new Vector3(h,yy,s*h);case 2:return new Vector3(-s*h,yy,h);default:return new Vector3(-h,yy,-s*h);}
        }
        void Member(Vector3 a,Vector3 b,Vector3 n,float w,float d){var dir=b-a;W.OBox(steel,(a+b)*.5f,new Vector3(w,d,dir.magnitude),Quaternion.LookRotation(dir.normalized,n));}
        for(int face=0;face<4;face++)
        {
            var nOut=(Pt(face,0f,Shaft,1f)-Pt(face,0f,Shaft,0f)).normalized;
            for(int i=0;i<levels;i++)
            {
                float ya=Shaft+i*16f,yb=ya+16f;
                for(int j=0;j<=8;j++)
                {
                    if((i+j)%2!=0)continue;
                    float s=-1f+j*.25f;
                    if(j<8)Member(Pt(face,s,ya,.45f),Pt(face,s+.25f,yb,.45f),nOut,.60f,.50f);
                    if(j>0)Member(Pt(face,s,ya,.45f),Pt(face,s-.25f,yb,.45f),nOut,.60f,.50f);
                }
                Member(Pt(face,-1f,yb,.45f),Pt(face,1f,yb,.45f),nOut,.50f,.50f);                                // ring beam
            }
        }
        foreach(var (sx,sz) in new[]{(-1f,-1f),(1f,-1f),(1f,1f),(-1f,1f)})
        {
            var a=new Vector3(sx*(h0+.45f),Shaft,sz*(h0+.45f));var b=new Vector3(sx*(h1+.45f),top,sz*(h1+.45f));
            var n=new Vector3(sx,0,sz).normalized;W.OBox(steel,(a+b)*.5f,new Vector3(.8f,.8f,(b-a).magnitude),Quaternion.LookRotation((b-a).normalized,n));
        }
        W.Grupa=outside;
        // the parapet 10 cm proud of the diagrid's top ring beam (its outer face at h1+.70)
        Prism(bandGraphite,bandGraphite,bandGraphite,Box4(h1+.80f,h1+.80f),top,top+1.4f);
        W.KolizjaMM(-h0,h0,Shaft,top,-h0,h0);
        return top+1.4f;
    }

    // ---------------- models (Poly Haven, real size) ----------------
    static GameObject Model(string path,string label,Vector3 at,float yaw,bool hang=false)
    {
        string full="Assets/Source/Mistrzowie/"+path;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(full);
        if(!source){BraxBuildQuality.Warn("DO POPRAWKI TOWER_MODEL_MISSING "+full);return null;}
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;if(!go)return null;
        go.name="TOWER | "+label+" | Poly Haven CC0, real size";
        go.transform.SetParent(inside,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;
        go.transform.localScale=Vector3.one;go.transform.localPosition=at;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0)
        {
            var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var target=inside.TransformPoint(at);
            go.transform.position+=Vector3.up*(hang?target.y-b.max.y:target.y-b.min.y);
        }
        if(hang)hung.Add(new Vector2(at.x,at.z));
        models++;return go;
    }
    // the owner's city palms on the plaza, beside the planters
    static int Palms(Transform root)
    {
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm){BraxBuildQuality.Warn("DO POPRAWKI TOWER_PALMS_MISSING: miasto_palmy/palma");return 0;}
        int placed=0;
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
        {
            var world=root.TransformPoint(new Vector3(sx*(PlotHalf-4f),SiteY+.45f,sz*(PlotHalf-4f)));
            var o=BraxChampionsModel.Postaw(outside,palm,"tower plaza palm",world,(sx*31f+sz*7f+360f)%360f,1f,false,true);
            if(!o)continue;
            o.transform.localScale=Vector3.one;
            BraxMiastoAssety.LodyZWezlow(o,new[]{.25f,.08f,.012f});
            BraxChampionsPalmTrunk.OnSpot(o,world);
            placed++;
        }
        return placed;
    }
}

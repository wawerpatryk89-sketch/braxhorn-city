using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - DOWNTOWN: premium towers close together (owner 16.11.2026 14:50: "Zacznij budować wieżowce i
// bukmachera, minimum 4 wieżowce premium blisko siebie"; plan 12.11 claude/braxhorn-city-ZALOZENIA-GRY-I-PLAN-MISTRZOW:
// "Downtown (T08-T13): BRAXHORN TOWER 250-300 m z lądowiskiem", "punkt BRAXHORN BET"). The five free tower plots of
// plan_inventory.json round the casino T11, 115 m apart (55 x 55 m each):
//   T08 (495,570) BANK            - twisting: 42 office floors, every plate turned 2 degrees (84 in all), white slab bands
//   T09 (610,570) BRAXHORN TOWER  - 60 floors + a 14 m lit crown = 262 m: a chamfered square with two setbacks,
//                                   stainless blades, BRAXHORN on the crown, a heliport on top; BRAXHORN BET downstairs
//   T10 (725,570) MEDIA           - elliptical, 34 floors, white bands, a crown of blades
//   T12 (610,465) KLUB / BIURA    - three stacked blocks turned 90 degrees to each other: cantilevers, roof terraces,
//                                   white slab bands
//   T13 (725,465) USŁUGI / KLUB   - tapering 40 -> 26 m under a steel diagrid
// Real sizes (nominal metres, K once on the root): lobby 8.0 m (double height), office storey 4.0 m, slab band .90 m,
// door 2.0 x 2.7 m. The office floors stand behind OPAQUE black glass (owner 10.11: façade glass black, only the door
// leaves light) - no empty storey can be seen through them. The ground floors are finished and open: smoked glass, real
// doors (BraxCityDoor), marble floor, reception, a lift bank, seats, plants, pendants, light.
// No surface lies on another (owner 13:57): every prism has its own caps; caps of stacked prisms meet face to face
// (opposite faces, never seen); details stand 2 cm or more off the faces behind them.
public static class BraxChampionsWiezowce
{
    static float K => BraxClaudeOsiedle.K;
    public const float SiteY=.16f, PlotBaseY=-.25f, Fl=.18f, Lobby=8.0f, Storey=4.0f, Band=.90f, Proud=.10f, PlotHalf=27.5f;
    public const float DoorHalf=1.0f, DoorH=2.70f, Ceiling=.30f, CanopyTop=4.90f, CanopyOut=3.02f;
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
    static Material gBlack,gLobby,gClear,band,bandWhite,bandGraphite,steel,black,granite,plaza,marble,walnut,ceilingWhite,
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
    static void Materials(Dictionary<string,Material> ext)
    {
        System.IO.Directory.CreateDirectory(outFolder+"/Materials");
        // opaque black glass with the sky in it (a dielectric: dark body, fresnel reflection at a glance)
        gBlack=Surface("black_glass",new Color(.018f,.022f,.028f),.05f,0f);
        if(gBlack.HasProperty("_ReflectionStrength"))gBlack.SetFloat("_ReflectionStrength",1f);
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
    // one office storey: the slab band (Proud wider than the glass, its own caps) and the black glass over it
    static void OfficeStorey(List<Vector2> glass,float y,Material bandM,Material roof=null)
    {
        Prism(bandM,bandM,bandM,Offset(glass,Proud),y,y+Band);
        Prism(gBlack,gBlack,roof?roof:gBlack,glass,y+Band,y+Storey);
    }
    // a blade (stainless fin) standing on the face of edge i at parameter t, from y0 to y1, w wide, d deep
    static void Blade(Material m,List<Vector2> p,int i,float t,float y0,float y1,float w=.30f,float d=.45f)
    {
        var c=Centre(p);var a=p[i];var b=p[(i+1)%p.Count];var n=EdgeOut(p,i,c);var at=Vector2.Lerp(a,b,t);
        var o=new Vector3(n.x,0,n.y);
        W.OBox(m,V3(at,(y0+y1)*.5f)+o*(d*.5f),new Vector3(w,y1-y0,d),Quaternion.LookRotation(o,Vector3.up));
    }
    static void Blades(Material m,List<Vector2> p,float y0,float y1,float step,Func<int,bool> edgeOk=null)
    {
        for(int i=0;i<p.Count;i++)
        {
            if(edgeOk!=null&&!edgeOk(i))continue;
            float len=(p[(i+1)%p.Count]-p[i]).magnitude;int n=Mathf.Max(1,Mathf.RoundToInt(len/step));
            for(int k=0;k<n;k++)Blade(m,p,i,(k+.5f)/n,y0,y1);
        }
    }

    // ---------------- per tower state ----------------
    static Transform outside,details,inside;
    static int doors,seats,models,betScreens;
    static string towerId;
    static BraxChampionsMeshBatch letters,kat;
    static List<Light> lights;
    static readonly List<Vector2> hung=new List<Vector2>();   // the pendants of this tower (plan x, z)

    public static void Generate(string id,Dictionary<string,Material> ext,string folder)
    {
        Tower t=default;bool found=false;
        foreach(var x in Towers)if(x.id==id){t=x;found=true;}
        if(!found)throw new ArgumentException("Unknown tower plot "+id);
        outFolder=folder;towerId=id;doors=0;seats=0;models=0;betScreens=0;hung.Clear();Materials(ext);
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
        int palms=Palms(root);
        if(doors<1)BraxBuildQuality.Warn("DO POPRAWKI TOWER_"+id+"_ENTRANCE_DOOR_MISSING");
        Debug.Log("CHAMPIONS_TOWER "+id+" "+t.name+" shape="+t.shape+" floors="+t.floors+" height="+(roof).ToString("F1")+"m nominal ("+
            (roof*K).ToString("F1")+" world) meshes="+saved+" doors="+doors+" seats="+seats+" models="+models+" palms="+palms+" lights="+lights.Count+
            (betScreens>0?" BRAXHORN_BET screens="+betScreens+" films=0 (street premises)":""));
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
                for(int k=0;k<n2;k++){Glass(s0+k*w+.06f,s0+(k+1)*w-.06f,y0,y0+4.0f-.06f);Glass(s0+k*w+.06f,s0+(k+1)*w-.06f,y0+4.0f+.06f,y1);}
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
                Glass(c0+.10f,c1-.10f,y0+DoorH+.16f,y1);
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
    static void LobbyFitOut(Core core,Vector2 desk,Vector2 lounge,bool loungeFacesRight,int lifts=4,float xmin=float.NegativeInfinity)
    {
        float y0=Fl,ceil=Shaft-Ceiling;
        W.Grupa=inside;W.Wnetrze=true;
        // the lift core clad in walnut, its front at z=core.yMin, the lift doors in brushed stainless
        W.MM(walnut,core.xMin,core.xMax,y0,ceil,core.yMin,core.yMax,true);
        float lw=1.2f,gap=(core.width-lifts*lw)/(lifts+1);
        for(int i=0;i<lifts;i++)
        {
            float x=core.xMin+gap*(i+1)+lw*(i+.5f);
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
        for(int f=0;f<t.floors;f++,y+=Storey)OfficeStorey(Rot(F,2f*f),y,bandWhite,f==t.floors-1?bandGraphite:null);
        // the roof: a white parapet 1.4 m over the last plate, its top a grey concrete roof deck (no white lid seen from the
        // towers round it); a white light band 25 cm high 2 cm off the parapet's face
        var last=Rot(F,2f*(t.floors-1));
        Prism(bandWhite,bandWhite,deck,Offset(last,Proud),y,y+1.4f);
        W.Grupa=details;Sides(ledWhite,Offset(last,Proud+.02f),y+.95f,y+1.20f);W.Grupa=outside;
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
        LobbyFitOut(core,new Vector2(11.5f,-8.5f),new Vector2(15.5f,-13f),false,4,split);
        Lounge(new Vector2(-2.5f,-12f),true,"lobby west","SOFA W HOLU");
        Bet(F0,split);
        LightLines(F0,split+.6f,20f,4f,core);
        LightLinesAt(F0,new[]{-15.3f,-11.0f,-8.6f},null,15f-.6f);           // between the pendants' rows, over the tills
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;int s1=30,s2=48;
        for(int f=0;f<t.floors;f++,y+=Storey)
        {
            var g=f<s1?F0:f<s2?F1:F2;
            bool lastOfSection=f==s1-1||f==s2-1||f==t.floors-1;
            OfficeStorey(g,y,bandGraphite,lastOfSection?bandGraphite:null);
        }
        float yS1=Shaft+s1*Storey,yS2=Shaft+s2*Storey,roof=y;
        // terraces of the setbacks: a parapet ring is the band of the next section's first floor (its own caps)
        // stainless blades every 3 m on every face of each section (30 cm face, 45 cm deep)
        W.Grupa=details;
        // (the lowest ones rise from the top of the first slab band: their feet never lie in the plane of its underside)
        Blades(steel,F0,Shaft+Band,yS1,3.0f);Blades(steel,F1,yS1,yS2,3.0f);
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
        Heliport(roof+14f);
        W.KolizjaMM(-20f,20f,Shaft,yS1,-20f,20f);W.KolizjaMM(-17f,17f,yS1,yS2,-17f,17f);W.KolizjaMM(-14f,14f,yS2,roof+14f,-14f,14f);
        // the tower's name over the lobby door (white light) and BRAXHORN BET over its door (green light): letters standing
        // on the canopy at its front edge, their faces flush with its fascia, so the canopy never hides their foot
        float edge=-20f-CanopyOut;
        BraxChampionsLetters3D.Text(letters,ledWhite,black,"BRAXHORN TOWER",new Vector3(4.5f,Fl+CanopyTop,edge+.10f),180f,.55f,.10f,.12f,5.6f);
        BraxChampionsLetters3D.Text(letters,ledGreen,black,"BRAXHORN BET",new Vector3(-10.5f,Fl+CanopyTop,edge+.10f),180f,.60f,.10f,.12f,5.6f);
        return roof+15.6f;
    }
    // the heliport: an 21.6 m square deck 1.2 m over the crown on steel posts, its top made of 1.2 m cells - the white
    // edge and the white H are cells of the deck itself (no paint on top), green edge lights
    static void Heliport(float y)
    {
        W.Grupa=outside;W.Wnetrze=false;
        const int N=18;const float c=1.2f,h=N*c*.5f;float y0=y+1.2f,y1=y0+.40f;
        for(int i=0;i<N;i++)for(int j=0;j<N;j++)
        {
            bool edge=i==0||j==0||i==N-1||j==N-1;
            bool H=(j>=5&&j<=12)&&((i==6||i==7)||(i==10||i==11)||((j==8||j==9)&&(i==8||i==9)));
            float x0=-h+i*c,z0=-h+j*c;
            Q(edge||H?paintWhite:deck,new Vector3(x0,y1,z0),new Vector3(x0+c,y1,z0),new Vector3(x0+c,y1,z0+c),new Vector3(x0,y1,z0+c),Vector3.up);
        }
        var sq=Box4(h,h);Sides(bandGraphite,sq,y0,y1);Cap(bandGraphite,sq,y0,false);
        foreach(float sx in new[]{-1f,0f,1f})foreach(float sz in new[]{-1f,0f,1f})
            if(sx!=0f||sz!=0f)W.MM(steel,sx*(h-1.5f)-.2f,sx*(h-1.5f)+.2f,y,y0,sz*(h-1.5f)-.2f,sz*(h-1.5f)+.2f);
        W.Grupa=details;
        for(int k=0;k<N;k+=2)foreach(var (px,pz) in new[]{(-h+k*c+c*.5f,-h-.12f),(-h+k*c+c*.5f,h+.12f),(-h-.12f,-h+k*c+c*.5f),(h+.12f,-h+k*c+c*.5f)})
            W.MM(ledGreen,px-.12f,px+.12f,y1-.10f,y1+.08f,pz-.12f,pz+.12f);
        W.Grupa=outside;
        W.KolizjaMM(-h,h,y0,y1,-h,h);
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
        foreach(var (hx,z) in new[]{(-13.2f,-1.5f),(-13.2f,-6.5f),(-13.2f,-11.5f),(-17.4f,-4.0f),(-17.4f,-9.0f),(-17.4f,-14.0f)})
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
    }
    // a BRAXHORN BET screen: the catalogue TV on the wall, its picture the black glass of a TV that is off and NO film player
    // (owner 11.11 21:48: films only in flats, hotels, apartments - never in street premises)
    static void BetScreen(BraxKatalog.Ctx ctx,Vector3 p,Vector3 f,float inches,string label)
    {
        BraxKatalog.Materials();
        var before=new HashSet<BraxTelewizor>(inside.GetComponentsInChildren<BraxTelewizor>(true));
        BraxKatalog.Tv(ctx,p,f,inches,0,false,label,BraxKatalog.GlassDark);
        foreach(var tv in inside.GetComponentsInChildren<BraxTelewizor>(true))
        {
            if(before.Contains(tv))continue;
            tv.gameObject.name=label+" | ekran wyłączony (bez filmów; transmisje walk ligi BRAXHORN BET)";
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
        for(int f=0;f<t.floors;f++,y+=Storey)OfficeStorey(F,y,bandWhite,f==t.floors-1?bandGraphite:null);
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
        GroundFloor(A,-13f,new[]{0f},core);
        LobbyFitOut(core,new Vector2(10f,-8f),new Vector2(-12.5f,-6.5f),true);
        LightLines(A,-18f,18f,4f,core);
        W.Grupa=outside;W.Wnetrze=false;
        float y=Shaft;int per=t.floors/3;
        for(int b=0;b<3;b++)
        {
            var g=b==1?B:A;
            for(int f=0;f<per;f++,y+=Storey)OfficeStorey(g,y,bandWhite,f==per-1?(b==2?bandGraphite:teak):null);
            if(b<2)Terraces(g,b==1?A:B,y);
        }
        // the top: a graphite parapet
        Prism(bandGraphite,bandGraphite,bandGraphite,Offset(A,Proud),y,y+1.2f);
        W.KolizjaMM(-18f,18f,Shaft,y,-18f,18f);
        return y+1.2f;
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
        for(int f=0;f<t.floors;f++,y+=Storey)
        {
            float hb=Half(y),hm=Half(y+Band),ht=Half(y+Storey);
            Frustum(bandWhite,bandWhite,bandWhite,Box4(hb+Proud,hb+Proud),Box4(hm+Proud,hm+Proud),y,y+Band);
            Frustum(gBlack,gBlack,f==t.floors-1?bandGraphite:gBlack,Box4(hm,hm),Box4(ht,ht),y+Band,y+Storey);
        }
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

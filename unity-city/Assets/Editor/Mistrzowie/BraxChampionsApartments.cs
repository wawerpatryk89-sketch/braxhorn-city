using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW – APARTAMENTOWCE T04-T07 (owner 9.11.2026):
// "Masz wybudować apartamentowce ... tu masz przykładowe zdjęcia" (5 photos),
// "Najpierw stawiasz jeden, całkowicie wykończony. W środku ma być tak samo,
// wykończenie. Pokoje, wszystko ma być porobione ... korytarze ... Bez wyposażenia.
// Każdy budynek od A do Z po kolei. Robisz jeden, dopiero następny."
// Plan: project doc braxhorn-city-MISTRZOWIE-APARTAMENTOWCE-PLAN-9-11.
//
// T04 = owner photo 1: white body, wraparound curved glass balconies with LED
// under the edge, timber fins, rounded dark roof overhang with a timber soffit.
// Real dimensions (nominal metres, K applied ONCE on the root):
// ground storey 4.20, storey 3.15 (2.83 clear), slab .30, glass balustrade 1.10
// (WT 2002 §298), balcony 2.2 deep, glazing module <=1.35 m, timber fins 50x150
// at 150 mm, stair riser .175 / tread .28 (WT §68), door .90 x 2.10.
// T05 = owner photo 2: rectilinear white frame grid, grey panels, recessed
// loggias (2.0 m) with LED downlights in the soffits, two vertical timber
// slat strips, flat roof with a .90 m parapet.
// T06 = owner photo 3: polished black marble, gold curved balconies with LED,
// central glass core rising above the roof, ground + 4. Faces road X800
// (the Z330 side is the back of plot H3 MMA).
// T07 = owner photo 5 with details of photo 4: white body, black marble corner
// volume, timber slat screen, sharp white balconies with timber soffits.
// Faces road X1130 (east).
// Rooms are FINISHED (oak floors, painted walls, white ceilings with downlights,
// marble bathrooms, doors with walnut casings, terrazzo corridors, stair, lift)
// and EMPTY: no furniture, no kitchen units, no sanitary fittings (owner).
public static class BraxChampionsApartments
{
    static float K => BraxClaudeOsiedle.K;
    public const float GroundStorey=4.20f, Storey=3.15f, Slab=.30f, Ffl0=.30f, SiteY=.16f, PlotBaseY=-.25f;
    public const float PlotHalf=30f, Balustrade=1.10f;
    // Per building, set from its plot design before generating.
    static float HalfW, HalfD, Loggia, Walk;
    static int Floors;
    static Style style;
    static float CoreBack=>HalfD-.30f;
    public const float Riser=.175f, Tread=.28f, DoorW=.90f, DoorH=2.10f, Partition=.12f, CorridorWall=.20f;
    const float Finish=.012f;                                    // floor finish over the slab
    // Core: dog-leg stair (lanes 1.30 m) west of the axis, lift east of it.
    const float LaneA0=-3.0f, LaneA1=-1.7f, LaneB0=-1.4f, LaneB1=-.1f, StairZ0=2.4f;
    const float CorridorZ0=-.7f, CorridorZ1=1.1f, LiftZ=5.4f;
    public static float Level(int f)=>f<=0?Ffl0:Ffl0+GroundStorey+(f-1)*Storey;   // finished floor
    public static float CeilingOf(int f)=>Level(f+1)-Slab;
    public static float RoofTop=>Level(Floors);
    public static int Risers(int f){int n=Mathf.RoundToInt((Level(f+1)-Level(f))/Riser);return n%2==1?n+1:n;}
    public static float FlightLength(int f)=>Risers(f)/2*Tread;

    // Plan inventory (plan_inventory.json, 60x60 "rental" plots). Front = local -Z,
    // turned toward the nearest road: T04/T05 face road Z650, T06/T07 road Z330.
    enum Style{CurvedWrap,Loggia,MarbleGold,MarbleCorner}
    // walk = local z (front = -z) where the path meets the road sidewalk.
    struct Plot { public string id, name; public float x, z, yaw, halfW, halfD, loggia, walk; public int floors; public Style style; }
    static readonly Plot[] Plots={
        // owner photo 1: ground + 3, curved wraparound balconies
        new Plot{id="T04",name="APARTAMENTY 1",x=870,z=570,yaw=180f,halfW=16f,halfD=8.5f,floors=4,style=Style.CurvedWrap,loggia=0f,walk=-63.2f},
        // owner photo 2: ground + 3, recessed loggias 2.0 m, white frames, grey panels, timber strips
        new Plot{id="T05",name="APARTAMENTY 2",x=1040,z=570,yaw=180f,halfW=17f,halfD=9f,floors=4,style=Style.Loggia,loggia=2.0f,walk=-63.2f},
        // owner photo 3: ground + 4, black marble, gold curved balconies, glass core; front west to road X800 (sidewalk x 817)
        new Plot{id="T06",name="APARTAMENTY 3",x=870,z=450,yaw=90f,halfW=17f,halfD=9f,floors=5,style=Style.MarbleGold,loggia=0f,walk=-53.2f},
        // owner photo 5 + details of 4: ground + 3, white, black marble corner, timber slats; front east to road X1130 (sidewalk x 1113)
        new Plot{id="T07",name="APARTAMENTY 4",x=1040,z=450,yaw=270f,halfW=17f,halfD=9f,floors=4,style=Style.MarbleCorner,loggia=0f,walk=-73.2f},
    };
    public static readonly string[] Ids={"T04","T05","T06","T07"};

    // ---------------- materials (region textures reused, ASTC 6x6 ids) ----------------
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static Material M(string id,string slug,float metres,Color tint,float roughFactor)
    {
        if(mats.TryGetValue(id,out var m)&&m)return m;
        m=BraxChampionsFoundation.SourceMaterial(id,slug,metres,tint);
        m.SetFloat("_Roughness",roughFactor);EditorUtility.SetDirty(m);
        return mats[id]=m;
    }
    static Material Region(string file,Func<Material> fallback)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/"+file);
        if(m)return m;
        BraxBuildQuality.Warn("DO POPRAWKI APT_REGION_MATERIAL_MISSING "+file+" (fallback used)");
        return fallback();
    }
    // skin = solid façade parts and slab edges; pier = ground-floor piers; edge = balcony fascia
    static Material skin,pier,edge,gold;
    static Material render,panel,granite,timber,walnut,black,facadeGlass,clearGlass,led,steel,
        oak,paint,ceiling,bathFloor,bathWall,terrazzo,marbleBlack,deck,grass,paving;

    static void Materials(Dictionary<string,Material> ext,string outFolder)
    {
        render=M("apt_white_render","white_stucco_02",2.0f,new Color(.97f,.96f,.94f),.90f);
        panel=M("apt_grey_panel","rectangular_facade_tiles",2.0f,new Color(.56f,.57f,.59f),.85f);
        granite=M("apt_dark_granite","granite_tile",1.2f,new Color(.30f,.31f,.33f),.55f);
        timber=M("apt_timber","teak_veneer",1.0f,new Color(.96f,.70f,.47f),.60f);
        walnut=M("apt_door_walnut","teak_veneer",1.0f,new Color(.44f,.30f,.21f),.50f);
        oak=M("apt_floor_oak","oak_wood_planks",1.6f,new Color(.98f,.93f,.86f),.60f);
        paint=M("apt_wall_paint","beige_wall_001",2.2f,new Color(1f,.99f,.97f),.95f);
        bathFloor=M("apt_bath_floor","tiled_floor_001",.8f,new Color(.92f,.92f,.92f),.45f);
        bathWall=M("apt_bath_marble","marble_01",1.4f,Color.white,.30f);
        terrazzo=M("apt_terrazzo","terrazzo_tiles",1.6f,new Color(.96f,.95f,.93f),.40f);
        deck=M("apt_balcony_stone","granite_tile_04",1.0f,new Color(.90f,.90f,.88f),.60f);
        ceiling=W.Mat.Plain("apartamenty: sufit biały mat",new Color(.93f,.93f,.92f),.92f);
        try{marbleBlack=BraxChampionsFoundation.LibraryMarble("apt_black_marble",new Color(.86f,.80f,.78f),2.5f);}
        catch(Exception e){BraxBuildQuality.Warn("DO POPRAWKI APT_BLACK_MARBLE_MISSING "+e.Message);marbleBlack=granite;}
        grass=Region("grass.mat",()=>M("apt_lawn_fallback","brown_mud_leaves_01",1.4f,new Color(.55f,.72f,.40f),1f));
        paving=Region("paving.mat",()=>M("apt_paving","concrete_pavement_02",1.8f,Color.white,1f));
        gold=ext["arena_gold"];
        black=ext["arena_black"];led=ext["light"];steel=ext["steel"];clearGlass=ext["clear_glass"];
        string glassPath=outFolder+"/Materials/apt_facade_glass.mat";
        facadeGlass=AssetDatabase.LoadAssetAtPath<Material>(glassPath);
        if(!facadeGlass)
        {
            // Reflective but readable: the finished rooms are seen from the street.
            facadeGlass=new Material(ext["facade_glass"]){name="apt facade glass | reflective, readable interior"};
            facadeGlass.SetColor("_Color",new Color(.40f,.47f,.53f,.42f));
            if(facadeGlass.HasProperty("_ReflectionStrength"))facadeGlass.SetFloat("_ReflectionStrength",.78f);
            facadeGlass.SetFloat("_Roughness",.05f);
            AssetDatabase.CreateAsset(facadeGlass,glassPath);
        }
    }

    // ---------------- primitives (nominal metres, building local, front = -Z) ----------------
    static void Box(Material m,float x0,float x1,float y0,float y1,float z0,float z1,bool col=false)
    {
        if(x1-x0<.001f||y1-y0<.001f||z1-z0<.001f)return;
        W.MM(m,x0,x1,y0,y1,z0,z1,col);
    }
    static void Face(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0)W.Quad(m,d,c,b,a);else W.Quad(m,a,b,c,d);
    }
    static void Ceiling(float x0,float x1,float z0,float z1,float y)
    {
        Face(ceiling,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.down);
    }
    static void Downlight(float x,float z,float ceilingY)=>Box(led,x-.09f,x+.09f,ceilingY-.012f,ceilingY+.004f,z-.09f,z+.09f);

    // Rounded rectangle, counter-clockwise in (x,z); round[i] for corners
    // 0:(-x,-z) 1:(+x,-z) 2:(+x,+z) 3:(-x,+z).
    static List<Vector2> RoundRect(float x0,float x1,float z0,float z1,float r,bool[] round,int seg=8)
    {
        var p=new List<Vector2>();
        var cs=new[]{new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};
        float[] start={180f,270f,0f,90f};
        for(int i=0;i<4;i++)
        {
            if(!round[i]){p.Add(cs[i]);continue;}
            var c=cs[i]+new Vector2(i==0||i==3?r:-r,i<2?r:-r);
            for(int k=0;k<=seg;k++){float a=(start[i]+90f*k/seg)*Mathf.Deg2Rad;p.Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}
        }
        return p;
    }
    // Convex prism: top/bottom fans from the centroid, side quads facing outward.
    static void Prism(List<Vector2> poly,float y0,float y1,Material top,Material side,Material bottom,bool col)
    {
        var c=Vector2.zero;foreach(var q in poly)c+=q;c/=poly.Count;
        for(int i=0;i<poly.Count;i++)
        {
            var a=poly[i];var b=poly[(i+1)%poly.Count];
            if((b-a).sqrMagnitude<1e-6f)continue;
            var mid=(a+b)*.5f;
            if(top)Face(top,new Vector3(c.x,y1,c.y),new Vector3(a.x,y1,a.y),new Vector3(mid.x,y1,mid.y),new Vector3(b.x,y1,b.y),Vector3.up);
            if(bottom)Face(bottom,new Vector3(c.x,y0,c.y),new Vector3(a.x,y0,a.y),new Vector3(mid.x,y0,mid.y),new Vector3(b.x,y0,b.y),Vector3.down);
            var n=new Vector3(b.y-a.y,0,-(b.x-a.x));                       // outward for CCW
            if(side)Face(side,new Vector3(a.x,y0,a.y),new Vector3(b.x,y0,b.y),new Vector3(b.x,y1,b.y),new Vector3(a.x,y1,a.y),n);
        }
        if(col)
        {
            float x0=float.MaxValue,x1=float.MinValue,z0=float.MaxValue,z1=float.MinValue;
            foreach(var q in poly){x0=Mathf.Min(x0,q.x);x1=Mathf.Max(x1,q.x);z0=Mathf.Min(z0,q.y);z1=Mathf.Max(z1,q.y);}
            W.KolizjaMM(x0,x1,y0,y1,z0,z1);
        }
    }
    // Thin strip following polygon edges (glass balustrade panes, LED lines), inset from the edge.
    static int Ribbon(List<Vector2> poly,float inset,float y0,float y1,float thick,Material m,bool col,Func<Vector2,Vector2,bool> keep)
    {
        int made=0;
        for(int i=0;i<poly.Count;i++)
        {
            var a=poly[i];var b=poly[(i+1)%poly.Count];
            if((b-a).sqrMagnitude<1e-6f||(keep!=null&&!keep(a,b)))continue;
            var d=(b-a).normalized;var o=new Vector2(d.y,-d.x);                // outward (CCW)
            a-=o*inset;b-=o*inset;
            var mid=(a+b)*.5f;float len=(b-a).magnitude+.006f;
            W.OBox(m,new Vector3(mid.x,(y0+y1)*.5f,mid.y),new Vector3(thick,y1-y0,len),Quaternion.LookRotation(new Vector3(d.x,0,d.y),Vector3.up),col);
            made++;
        }
        return made;
    }
    // Sloped walkway from (z0, yLow) up to (z1, yHigh) on a base at yBase, with a ramp collider.
    static void Wedge(Material m,float x0,float x1,float z0,float yLow,float z1,float yHigh,float yBase)
    {
        Face(m,new Vector3(x0,yLow,z0),new Vector3(x1,yLow,z0),new Vector3(x1,yHigh,z1),new Vector3(x0,yHigh,z1),Vector3.up);
        Face(m,new Vector3(x0,yBase,z0),new Vector3(x1,yBase,z0),new Vector3(x1,yLow,z0),new Vector3(x0,yLow,z0),Vector3.back);
        Face(m,new Vector3(x0,yBase,z0),new Vector3(x0,yBase,z1),new Vector3(x0,yHigh,z1),new Vector3(x0,yLow,z0),Vector3.left);
        Face(m,new Vector3(x1,yBase,z0),new Vector3(x1,yBase,z1),new Vector3(x1,yHigh,z1),new Vector3(x1,yLow,z0),Vector3.right);
        W.Rampa(new Vector3((x0+x1)*.5f,yHigh,z1),new Vector3((x0+x1)*.5f,yLow,z0),x1-x0);
    }

    // ---------------- walls with door openings ----------------
    struct Gap{public float a,b,top;public Gap(float a,float b,float top){this.a=a;this.b=b;this.top=top;}}
    static int doors;
    // Wall along X centred on z, from x0 to x1, openings (a,b,top above y0).
    static void WallX(Material m,float z,float x0,float x1,float y0,float y1,float t,params Gap[] gaps)
    {
        Array.Sort(gaps,(p,q)=>p.a.CompareTo(q.a));
        float x=x0;
        foreach(var g in gaps)
        {
            Box(m,x,g.a,y0,y1,z-t*.5f,z+t*.5f,true);
            Box(m,g.a,g.b,y0+g.top,y1,z-t*.5f,z+t*.5f,true);
            Casing(true,z,g.a,g.b,y0,y0+g.top,t);
            x=g.b;
        }
        Box(m,x,x1,y0,y1,z-t*.5f,z+t*.5f,true);
    }
    static void WallZ(Material m,float x,float z0,float z1,float y0,float y1,float t,params Gap[] gaps)
    {
        Array.Sort(gaps,(p,q)=>p.a.CompareTo(q.a));
        float z=z0;
        foreach(var g in gaps)
        {
            Box(m,x-t*.5f,x+t*.5f,y0,y1,z,g.a,true);
            Box(m,x-t*.5f,x+t*.5f,y0+g.top,y1,g.a,g.b,true);
            Casing(false,x,g.a,g.b,y0,y0+g.top,t);
            z=g.b;
        }
        Box(m,x-t*.5f,x+t*.5f,y0,y1,z,z1,true);
    }
    // Walnut casing (jambs + head), 5 cm wide, 12 mm proud of both wall faces.
    static void Casing(bool alongX,float plane,float a,float b,float y0,float y1,float t)
    {
        float w=.05f,d=t*.5f+.012f;
        if(alongX)
        {
            Box(walnut,a,a+w,y0,y1,plane-d,plane+d);Box(walnut,b-w,b,y0,y1,plane-d,plane+d);
            Box(walnut,a,b,y1-w,y1,plane-d,plane+d);
        }
        else
        {
            Box(walnut,plane-d,plane+d,y0,y1,a,a+w);Box(walnut,plane-d,plane+d,y0,y1,b-w,b);
            Box(walnut,plane-d,plane+d,y1-w,y1,a,b);
        }
    }
    // Door leaf standing open at 90 degrees, hinged at the opening's 'a' side, swinging toward 'side'; steel lever.
    static void Leaf(bool alongX,float plane,float a,float b,float y0,float side)
    {
        float w=(b-a)-.10f,th=.04f,p0=Mathf.Min(plane,plane+side*w),p1=Mathf.Max(plane,plane+side*w);
        float face=a+.05f+th,free=plane+side*(w-.10f);
        if(alongX)
        {
            Box(walnut,a+.05f,face,y0+.01f,y0+DoorH-.06f,p0,p1);
            Box(steel,face,face+.025f,y0+1.0f,y0+1.03f,free-.07f,free+.07f);
        }
        else
        {
            Box(walnut,p0,p1,y0+.01f,y0+DoorH-.06f,a+.05f,face);
            Box(steel,free-.07f,free+.07f,y0+1.0f,y0+1.03f,face,face+.025f);
        }
        doors++;
    }

    // ---------------- façade ----------------
    enum Seg{Glass,Door,Render,Timber,Panel,Marble}
    struct Part{public float a,b;public Seg s;public Part(float a,float b,Seg s){this.a=a;this.b=b;this.s=s;}}
    static Transform outside,details,inside;
    // One storey band of a façade. alongX: plane z=c, else plane x=c; nrm = outward sign.
    static void FacadeBand(bool alongX,float c,float nrm,int f,Part[] parts)
    {
        float y0=Level(f),y1=CeilingOf(f);
        foreach(var p in parts)
        {
            W.Grupa=outside;W.Wnetrze=false;
            if(p.s==Seg.Glass||p.s==Seg.Door)
            {
                float len=p.b-p.a;int n=Mathf.Max(1,Mathf.CeilToInt(len/1.35f));
                if(p.s==Seg.Glass)Pane(alongX,c,p.a,p.b,y0+.06f,y1-.06f,0f);
                else Pane(alongX,c,p.a,p.a+len*.5f,y0+.06f,y1-.06f,-nrm*.08f);     // sliding leaf open by half
                W.Grupa=details;                                                    // frames drop out far away
                Rail(alongX,c,p.a,p.b,y1-.06f,y1);
                if(p.s==Seg.Glass)Rail(alongX,c,p.a,p.b,y0,y0+.06f);
                else Rail(alongX,c,p.a,p.b,y0-.01f,y0+.012f);                       // flush threshold
                for(int i=0;i<=n;i++)Mullion(alongX,c,p.a+len*i/n,y0,y1);
            }
            else
            {
                var mat=p.s==Seg.Timber?timber:p.s==Seg.Panel?panel:p.s==Seg.Marble?marbleBlack:skin;float t=.30f;
                if(alongX)Box(mat,p.a,p.b,y0,y1,c-t*.5f,c+t*.5f,true);else Box(mat,c-t*.5f,c+t*.5f,y0,y1,p.a,p.b,true);
                W.Grupa=inside;W.Wnetrze=true;                                     // painted inner face
                float i0=nrm<0?c+t*.5f:c-t*.5f-.01f,i1=i0+.01f;
                if(alongX)Box(paint,p.a,p.b,y0,y1,i0,i1);else Box(paint,i0,i1,y0,y1,p.a,p.b);
            }
        }
        W.Grupa=outside;W.Wnetrze=false;
    }
    // Left half (x<0) mirrored to the right, optional centre part.
    static Part[] Sym(Part[] left,Part? centre)
    {
        var all=new List<Part>(left);
        if(centre.HasValue)all.Add(centre.Value);
        for(int i=left.Length-1;i>=0;i--)all.Add(new Part(-left[i].b,-left[i].a,left[i].s));
        return all.ToArray();
    }
    static void Pane(bool alongX,float c,float a,float b,float y0,float y1,float offset)
    {
        float k=c+offset;
        if(alongX)Box(facadeGlass,a,b,y0,y1,k-.01f,k+.01f,true);else Box(facadeGlass,k-.01f,k+.01f,y0,y1,a,b,true);
    }
    static void Rail(bool alongX,float c,float a,float b,float y0,float y1)
    {
        if(alongX)Box(black,a,b,y0,y1,c-.06f,c+.06f);else Box(black,c-.06f,c+.06f,y0,y1,a,b);
    }
    static void Mullion(bool alongX,float c,float u,float y0,float y1)
    {
        if(alongX)Box(black,u-.03f,u+.03f,y0,y1,c-.06f,c+.06f);else Box(black,c-.03f,c+.03f,y0,y1,u-.03f,u+.03f);
    }

    // ---------------- apartment (finished, empty) ----------------
    // Plan, measured from the OUTER end (u=0, side façade) to the INNER end (core / party wall):
    //   living + kitchen: full depth, 45 % of the width, side window + balcony door;
    //   façade zone: bedroom 1, bedroom 2 (floor-to-ceiling glass);
    //   corridor strip (2.40 m): hall with the entry door, bathroom at the inner end
    //   (bathrooms stack above each other next to the core).
    static int apartments;
    static float Apartment(int f,float x0,float x1,float zf,float zc,bool innerAtX1,string number,List<Light> lights,float livingRecess=0f)
    {
        W.Grupa=inside;W.Wnetrze=true;
        float y0=Level(f),y1=CeilingOf(f),top=Level(f+1),s=Mathf.Sign(zc-zf),zs=zc-s*2.4f,w=x1-x0,cy=y1-.01f;
        float X(float u)=>innerAtX1?x0+u:x1-u;
        float dirU=innerAtX1?1f:-1f;
        float lu=Mathf.Round(.45f*w*10f)/10f,bu=(w-lu)*.5f,bathU=2.2f,uB2=lu+bu,uHall=w-bathU;
        if(uHall-uB2<1.1f)BraxBuildQuality.Warn("DO POPRAWKI APT_BEDROOM2_DOOR_TIGHT apartment "+number+" overlap="+(uHall-uB2));
        float Lo(float p,float q)=>Mathf.Min(p,q);
        float Hi(float p,float q)=>Mathf.Max(p,q);
        float fz0=Lo(zf,zs),fz1=Hi(zf,zs),sz0=Lo(zs,zc),sz1=Hi(zs,zc),az0=Lo(zf,zc),az1=Hi(zf,zc);
        float zGlass=zf-s*.14f;
        // living façade: behind a loggia when recessed (T05), in the bedroom line otherwise
        float zfL=zf+s*livingRecess,lz0=Lo(zfL,zc),lz1=Hi(zfL,zc);
        // floors: oak, bathroom porcelain tiles (finish 12 mm over the slab)
        Box(oak,Lo(X(0),X(lu)),Hi(X(0),X(lu)),y0,y0+Finish,lz0,lz1);
        Box(oak,Lo(X(lu),X(w)),Hi(X(lu),X(w)),y0,y0+Finish,fz0,fz1);
        Box(oak,Lo(X(lu),X(uHall)),Hi(X(lu),X(uHall)),y0,y0+Finish,sz0,sz1);
        float bx0=Lo(X(uHall),X(w)),bx1=Hi(X(uHall),X(w));
        Box(bathFloor,bx0,bx1,y0,y0+Finish,sz0,sz1);
        // living | rest: full depth, 1.40 m opening into the hall
        float zm=(zs+zc)*.5f;
        WallZ(paint,X(lu),Lo(zGlass,zc),Hi(zGlass,zc),y0,top,Partition,new Gap(zm-.70f,zm+.70f,2.30f));
        if(livingRecess>0f)
        {
            // the partition is the loggia's side wall in front of the living: white render on that face
            float face=X(lu)-dirU*Partition*.5f,zo=zfL-s*.30f;
            W.Grupa=outside;W.Wnetrze=false;
            Box(render,Lo(face,face-dirU*.012f),Hi(face,face-dirU*.012f),y0,y1,Lo(zf,zo),Hi(zf,zo));
            W.Grupa=inside;W.Wnetrze=true;
        }
        // bedroom 1 | bedroom 2
        WallZ(paint,X(uB2),Lo(zGlass,zs),Hi(zGlass,zs),y0,top,Partition);
        // façade zone | corridor strip, bedroom doors from the hall
        float u1=Mathf.Clamp(lu+bu*.5f,lu+.65f,uHall-.65f),u2=(uB2+uHall)*.5f;
        float d1=X(u1),d2=X(u2);
        WallX(paint,zs,Lo(X(lu),X(w)),Hi(X(lu),X(w)),y0,top,Partition,
            new Gap(d1-DoorW*.5f,d1+DoorW*.5f,DoorH),new Gap(d2-DoorW*.5f,d2+DoorW*.5f,DoorH));
        Leaf(true,zs,d1-DoorW*.5f,d1+DoorW*.5f,y0,-s);Leaf(true,zs,d2-DoorW*.5f,d2+DoorW*.5f,y0,-s);
        // hall | bathroom, .80 door swinging into the bathroom
        WallZ(paint,X(uHall),sz0,sz1,y0,top,Partition,new Gap(zm-.40f,zm+.40f,DoorH));
        Leaf(false,X(uHall),zm-.40f,zm+.40f,y0,dirU);
        // bathroom: marble to the ceiling on all four walls (door opening kept)
        float hw=Partition*.5f,rx0=innerAtX1?bx0+hw:bx0,rx1=innerAtX1?bx1:bx1-hw;
        float rz0=s>0?sz0+hw:sz0,rz1=s>0?sz1:sz1-hw;
        float doorSide=innerAtX1?rx0:rx1,o=innerAtX1?.01f:-.01f,far=innerAtX1?rx1:rx0;
        Box(bathWall,Lo(doorSide,doorSide+o),Hi(doorSide,doorSide+o),y0,cy,rz0,zm-.40f);
        Box(bathWall,Lo(doorSide,doorSide+o),Hi(doorSide,doorSide+o),y0,cy,zm+.40f,rz1);
        Box(bathWall,Lo(doorSide,doorSide+o),Hi(doorSide,doorSide+o),y0+DoorH,cy,zm-.40f,zm+.40f);
        Box(bathWall,Lo(far,far-o),Hi(far,far-o),y0,cy,rz0,rz1);
        Box(bathWall,rx0,rx1,y0,cy,rz0,rz0+.01f);Box(bathWall,rx0,rx1,y0,cy,rz1-.01f,rz1);
        // mullions where partitions meet the glass (no see-through gap between rooms)
        float cGlass=zf-s*.15f;
        foreach(float u in new[]{lu,uB2})Box(black,X(u)-.035f,X(u)+.035f,y0,y1,cGlass-.06f,cGlass+.06f);
        // white ceiling 1 cm below the slab, recessed downlights in every room
        Ceiling(Lo(X(0),X(lu)),Hi(X(0),X(lu)),lz0,lz1,cy);
        Ceiling(Lo(X(lu),X(w)),Hi(X(lu),X(w)),az0,az1,cy);
        float zRooms=(zf+zs)*.5f,zStrip=(zs+zc)*.5f;
        Downlight(X(lu*.5f),(zfL+zc)*.5f,cy);Downlight(X(lu*.5f),(zfL+zs)*.5f,cy);
        Downlight(X(lu+bu*.5f),zRooms,cy);Downlight(X(uB2+bu*.5f),zRooms,cy);
        Downlight(X((lu+uHall)*.5f),zStrip,cy);Downlight(X(uHall+bathU*.5f),zStrip,cy);
        lights.Add(W.Lampa("apartment "+number+" warm light",new Vector3(X(lu*.5f),y1-.45f,(zfL+zc)*.5f),.9f,9f));
        // entry from the building corridor into the hall; number on a 12 mm black plate beside it
        float entry=X((lu+uHall)*.5f),wall=zc+s*CorridorWall;
        Box(black,entry+.66f,entry+.94f,y0+1.47f,y0+1.63f,Lo(wall,wall+s*.012f),Hi(wall,wall+s*.012f));
        W.Napis(number,new Vector3(entry+.80f,y0+1.55f,wall+s*.014f),new Vector3(0,0,s),.10f,new Color(.84f,.66f,.30f));
        apartments++;
        return entry;
    }

    // Dog-leg stair from level f to f+1: flight 1 in lane A (+Z), half landing, flight 2 in lane B (-Z).
    // Floating stepped flights: each storey repeats above the previous one with a full storey of headroom.
    static void Stair(int f)
    {
        W.Grupa=inside;W.Wnetrze=true;
        float a=Level(f),b=Level(f+1),h=b-a;int n=Risers(f);
        float r=h/n;int half=n/2;float zMid=StairZ0+FlightLength(f),mid=a+h*.5f;
        for(int k=0;k<half;k++)
        {
            float t1=a+(k+1)*r,t2=mid+(k+1)*r;
            Box(terrazzo,LaneA0,LaneA1,t1-r-.16f,t1,StairZ0+k*Tread,StairZ0+(k+1)*Tread);
            Box(terrazzo,LaneB0,LaneB1,t2-r-.16f,t2,zMid-(k+1)*Tread,zMid-k*Tread);
        }
        Box(terrazzo,LaneA0,LaneB1,mid-.20f,mid,zMid,CoreBack,true);                     // half landing
        W.Rampa(new Vector3((LaneA0+LaneA1)*.5f,mid,zMid),new Vector3((LaneA0+LaneA1)*.5f,a,StairZ0),LaneA1-LaneA0);
        W.Rampa(new Vector3((LaneB0+LaneB1)*.5f,b,StairZ0),new Vector3((LaneB0+LaneB1)*.5f,mid,zMid),LaneB1-LaneB0);
        Box(clearGlass,-1.565f,-1.535f,a,b,StairZ0,zMid,true);                            // glass between flights
        W.Rura(steel,new Vector3(LaneA1+.05f,a+.90f,StairZ0),new Vector3(LaneA1+.05f,mid+.90f,zMid),.022f,8);
        W.Rura(steel,new Vector3(LaneB0-.05f,mid+.90f,zMid),new Vector3(LaneB0-.05f,b+.90f,StairZ0),.022f,8);
        // light under the next half landing (or the top-floor ceiling)
        float above=f+1<Floors-1?Level(f+1)+(Level(f+2)-Level(f+1))*.5f-.20f:CeilingOf(Floors-1)-.01f;
        Downlight((LaneA0+LaneB1)*.5f,(zMid+CoreBack)*.5f,above);
    }

    public static void Generate(string id,Dictionary<string,Material> ext,string outFolder)
    {
        Plot plot=default;bool found=false;
        foreach(var p in Plots)if(p.id==id){plot=p;found=true;}
        if(!found)throw new ArgumentException("Unknown apartment plot "+id);
        HalfW=plot.halfW;HalfD=plot.halfD;Floors=plot.floors;Loggia=plot.loggia;Walk=plot.walk;style=plot.style;
        System.IO.Directory.CreateDirectory(outFolder+"/Materials");System.IO.Directory.CreateDirectory(outFolder+"/Prefabs");
        Materials(ext,outFolder);apartments=0;doors=0;
        bool marble=style==Style.MarbleGold;
        skin=marble?marbleBlack:render;pier=marble?marbleBlack:granite;edge=marble?gold:render;
        // Ground datum from the real ground: the empty plot base sits at -.25 (legacy_foundation.py).
        Physics.SyncTransforms();
        float datum=0f;
        if(Physics.Raycast(new Vector3(plot.x,80f,plot.z)*K,Vector3.down,out var hit,200f*K,~0,QueryTriggerInteraction.Ignore))
        {
            datum=hit.point.y-PlotBaseY*K;
            if(Mathf.Abs(datum)>.05f*K)BraxBuildQuality.Warn("DO POPRAWKI APT_"+id+"_GROUND hit y="+hit.point.y.ToString("F3")+" expected plot base "+(PlotBaseY*K).ToString("F3"));
        }
        else BraxBuildQuality.Warn("DO POPRAWKI APT_"+id+"_GROUND_RAYCAST_MISS: datum y=0");

        var root=new GameObject(id+" | "+plot.name+" | apartment building, finished empty interiors").transform;
        outside=new GameObject("exterior").transform;outside.SetParent(root,false);
        details=new GameObject("exterior details (frames, fins, LED; distance LOD)").transform;details.SetParent(root,false);
        inside=new GameObject("interior (finished, empty)").transform;inside.SetParent(root,false);
        W.Wyczysc();W.Start(outside);W.Wnetrze=false;W.Komorka=40f;
        var lights=new List<Light>();
        Site();
        Structure();
        CommonFacades();
        int panes=style==Style.CurvedWrap?FrontAndRoofT04():style==Style.Loggia?FrontAndRoofT05():
                  style==Style.MarbleGold?FrontAndRoofT06():FrontAndRoofT07();
        panes+=RearBalconies();
        Interior(lights);

        int saved=W.Zapisz(id+"_apartments");
        // Distance LOD: frames, fins and LED lines drop out when the building is small on screen;
        // the white body, glass, balconies and roof always stay (no pop-in of the building itself).
        var detailRenderers=details.GetComponentsInChildren<Renderer>(true);
        var lod=details.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[]{new LOD(.06f,detailRenderers),new LOD(.0001f,Array.Empty<Renderer>())});
        lod.RecalculateBounds();
        // ---- place: K once, yaw toward the road, ground datum from the raycast ----
        root.localScale=Vector3.one*K;
        root.rotation=Quaternion.Euler(0,plot.yaw,0);
        root.position=new Vector3(plot.x*K,datum,plot.z*K);
        foreach(var l in root.GetComponentsInChildren<Light>(true))l.range*=K;
        int palms=Palms(root,outside);
        var vis=inside.gameObject.AddComponent<BraxWidocznoscWnetrza>();vis.nazwa=id+" apartments";
        // Interiors are seen through the glass from the street: drawn within ~60 m.
        vis.strefy=new[]{new Bounds(root.position+Vector3.up*8f*K,new Vector3(120f,44f,120f)*K)};
        var at=root.position;var rot=root.rotation;root.position=Vector3.zero;root.rotation=Quaternion.identity;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,outFolder+"/Prefabs/"+id+"_Apartments.prefab",InteractionMode.AutomatedAction);
        root.position=at;root.rotation=rot;
        if(!prefab)throw new System.IO.IOException(id+" apartment prefab save failed");
        Debug.Log("APT_BUILDING "+id+" '"+plot.name+"' style="+plot.style+" body="+(2*HalfW)+"x"+(2*HalfD)+" floors=ground+"+(Floors-1)+
            " groundStorey="+GroundStorey+" storey="+Storey+" roofTop="+RoofTop+" loggia="+Loggia+" apartments="+apartments+" doors="+doors+
            " balconyGlassPanes="+panes+" balustrade="+Balustrade+" stair=dog-leg riser"+Riser+"/tread"+Tread+
            " lift=1 lights="+lights.Count+" palms="+palms+" meshes="+saved+" datumY="+datum.ToString("F3")+
            " yaw="+plot.yaw+" interiors=finished_empty_no_furniture");
    }

    // ---- site: lawn over the whole plot, apron, terrace, ramp, path to the sidewalk, bollards ----
    static void Site()
    {
        W.Grupa=outside;W.Wnetrze=false;
        Box(grass,-PlotHalf,PlotHalf,PlotBaseY,SiteY,-PlotHalf,PlotHalf,true);
        Box(paving,-HalfW-1.6f,HalfW+1.6f,SiteY,SiteY+.03f,-HalfD-3.4f,HalfD+2.1f,true);
        Box(granite,-HalfW,HalfW,PlotBaseY,Ffl0,-HalfD,HalfD,true);                       // plinth
        Box(granite,-HalfW,HalfW,SiteY,Ffl0,-HalfD-2.6f,-HalfD,true);                     // front terrace
        Wedge(granite,-2.0f,2.0f,-HalfD-5.5f,SiteY+.03f,-HalfD-2.6f,Ffl0,SiteY);          // 3.8 % ramp, no step
        Box(paving,-2.5f,2.5f,SiteY,SiteY+.03f,Walk,-HalfD-3.4f,true);                     // to the road sidewalk
        W.Grupa=details;
        for(float z=-16f;z>Walk+2f;z-=6f)
            foreach(float sx in new[]{-1f,1f})
            {
                float x=sx*3.1f;
                Box(black,x-.08f,x+.08f,SiteY,SiteY+.85f,z-.08f,z+.08f,true);
                Box(led,x-.085f,x+.085f,SiteY+.85f,SiteY+.95f,z-.085f,z+.085f);
            }
        W.Grupa=outside;
    }

    // ---- structure: floor slabs with the stairwell opening, roof slab ----
    static void Structure()
    {
        W.Grupa=outside;W.Wnetrze=false;
        for(int f=1;f<=Floors;f++)
        {
            float y=Level(f);
            if(f<Floors)
            {
                Box(skin,-HalfW,HalfW,y-Slab,y,-HalfD,StairZ0,true);
                Box(skin,-HalfW,LaneA0,y-Slab,y,StairZ0,HalfD,true);
                Box(skin,LaneB1,HalfW,y-Slab,y,StairZ0,HalfD,true);
                Box(skin,LaneA0,LaneB1,y-Slab,y,CoreBack,HalfD,true);
            }
            else Box(skin,-HalfW,HalfW,y-Slab,y,-HalfD,HalfD,true);
        }
    }

    // ---- shared façades: recessed ground-floor front with the entrance, rear, both sides ----
    static void CommonFacades()
    {
        float sideWindow=-HalfD+1.9f+Loggia;                                              // front living rooms
        for(int f=0;f<Floors;f++)
        {
            if(f==0)
                FacadeBand(true,-HalfD+1.0f,-1f,0,Sym(new[]{new Part(-HalfW,-HalfW+.8f,Seg.Render),new Part(-HalfW+.8f,-3.2f,Seg.Glass),
                    new Part(-3.2f,-1.6f,Seg.Render),new Part(-1.6f,0f,Seg.Door)},null));
            FacadeBand(true,HalfD-.15f,1f,f,Sym(new[]{new Part(-HalfW,-HalfW+1f,Seg.Render),new Part(-HalfW+1f,-HalfW+2.6f,Seg.Glass),
                new Part(-HalfW+2.6f,-HalfW+4.4f,f==0?Seg.Glass:Seg.Door),new Part(-HalfW+4.4f,-4.5f,Seg.Glass)},new Part(-4.5f,4.5f,Seg.Render)));
            foreach(float sx in new[]{-1f,1f})
                FacadeBand(false,sx*(HalfW-.15f),sx,f,new[]{new Part(-HalfD,sideWindow,Seg.Render),new Part(sideWindow,sideWindow+2.2f,Seg.Glass),
                    new Part(sideWindow+2.2f,-.5f,Seg.Render),new Part(-.5f,.9f,Seg.Glass),new Part(.9f,3.6f,Seg.Render),
                    new Part(3.6f,5.8f,Seg.Glass),new Part(5.8f,HalfD,Seg.Render)});
        }
        W.Grupa=outside;W.Wnetrze=false;
        // ground floor: dark granite piers, LED downlights in the 1 m deep entrance soffit
        foreach(float x in new[]{-HalfW+.4f,-9f,9f,HalfW-.4f})Box(pier,x-.4f,x+.4f,Ffl0,CeilingOf(0),-HalfD-.05f,-HalfD+.95f,true);
        W.Grupa=details;
        for(float x=-(HalfW-2f);x<=HalfW-1.99f;x+=2f)Downlight(x,-HalfD+.48f,CeilingOf(0));
        W.Grupa=outside;
    }

    // ---- rear balconies 3.6 x 1.6 for the rear living rooms (floors 1..top) ----
    static int RearBalconies()
    {
        W.Grupa=outside;W.Wnetrze=false;
        int panes=0;
        for(int f=1;f<Floors;f++)
            foreach(float cx in new[]{-(HalfW-3.5f),HalfW-3.5f})
            {
                float deckTop=Level(f)-.03f;
                var poly=RoundRect(cx-1.8f,cx+1.8f,HalfD,HalfD+1.6f,0f,new[]{false,false,false,false});
                Prism(poly,deckTop-.25f,deckTop,deck,edge,render,true);
                bool Outer(Vector2 a,Vector2 b)=>!(Mathf.Abs(a.y-HalfD)<.01f&&Mathf.Abs(b.y-HalfD)<.01f);
                panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer);
            }
        return panes;
    }

    // ---- T04 (owner photo 1): glass front with timber piers, curved wraparound balconies, rounded overhang ----
    static int FrontAndRoofT04()
    {
        for(int f=1;f<Floors;f++)
            FacadeBand(true,-HalfD+.15f,-1f,f,Sym(new[]{new Part(-HalfW,-13.2f,Seg.Render),new Part(-13.2f,-11.8f,Seg.Timber),
                new Part(-11.8f,-10.8f,Seg.Glass),new Part(-10.8f,-9.0f,Seg.Door),new Part(-9.0f,-.1f,Seg.Glass)},new Part(-.1f,.1f,Seg.Render)));
        // timber fins (50 x 150 @ 150 mm) on a black backing, floors 1-3, both front piers
        W.Grupa=details;W.Wnetrze=false;
        foreach(float xa in new[]{-13.2f,11.8f})
        {
            Box(black,xa,xa+1.4f,CeilingOf(0),RoofTop,-HalfD-.03f,-HalfD);
            for(int i=0;i<9;i++){float x=xa+.075f+i*.15f;Box(timber,x,x+.05f,CeilingOf(0),RoofTop,-HalfD-.18f,-HalfD-.03f);}
        }
        // curved front balconies (floors 1-3): 22 x 2.2 m, rounded ends, frameless glass, LED under the edge
        int panes=0;
        for(int f=1;f<Floors;f++)
        {
            float deckTop=Level(f)-.03f;
            var poly=RoundRect(-11f,11f,-HalfD-2.2f,-HalfD,1.1f,new[]{true,true,false,false});
            W.Grupa=outside;
            Prism(poly,deckTop-.30f,deckTop,deck,render,render,true);
            bool Outer(Vector2 a,Vector2 b)=>!(Mathf.Abs(a.y+HalfD)<.01f&&Mathf.Abs(b.y+HalfD)<.01f);
            panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer);
            W.Grupa=details;
            Ribbon(poly,.12f,deckTop-.30f-.012f,deckTop-.30f,.05f,led,false,Outer);
        }
        // rounded black overhang, timber soffit with downlights, LED line under the edge
        var roof=RoundRect(-HalfW-1.4f,HalfW+1.4f,-HalfD-2.6f,HalfD+.9f,2.0f,new[]{true,true,true,true});
        W.Grupa=outside;
        Prism(roof,RoofTop,RoofTop+.45f,black,black,timber,true);
        W.Grupa=details;
        Ribbon(roof,.08f,RoofTop-.012f,RoofTop,.05f,led,false,null);
        for(float x=-15f;x<=15.01f;x+=1.5f)
            foreach(float z in new[]{-HalfD-1.7f,-HalfD-.8f})Downlight(x,z,RoofTop);
        W.Grupa=outside;
        // No flat building-name text on the façade (rule ZASADA-SZYLDY-3D 9.11):
        // a sign for this building waits for the owner's artwork / brand decision.
        return panes;
    }

    // ---- T05 (owner photo 2): white frame grid, grey panels, recessed loggias, two timber strips, flat roof ----
    static int FrontAndRoofT05()
    {
        float c=-HalfD+.15f,cr=-HalfD+Loggia+.15f,xo=HalfW-.6f;                             // front plane, recessed plane
        const float xi=9.26f;                                                               // loggia inner edge = living partition face
        int panes=0;
        for(int f=1;f<Floors;f++)
        {
            float y0=Level(f),y1=CeilingOf(f);
            // bedrooms in the front line: grey panels and floor-to-ceiling windows
            FacadeBand(true,c,-1f,f,Sym(new[]{new Part(-xi,-8.4f,Seg.Panel),new Part(-8.4f,-5.4f,Seg.Glass),new Part(-5.4f,-3.9f,Seg.Panel),
                new Part(-3.9f,-.9f,Seg.Glass),new Part(-.9f,-.1f,Seg.Panel)},new Part(-.1f,.1f,Seg.Render)));
            // living rooms 2.0 m behind, opening onto the loggia with a sliding door
            FacadeBand(true,cr,-1f,f,Sym(new[]{new Part(-xo,-14.2f,Seg.Glass),new Part(-14.2f,-12.4f,Seg.Door),new Part(-12.4f,-xi,Seg.Glass)},null));
            W.Grupa=outside;W.Wnetrze=false;
            foreach(float sx in new[]{-1f,1f})
            {
                Box(render,sx<0?-HalfW:xo,sx<0?-xo:HalfW,y0,y1,-HalfD,cr+.15f,true);           // corner pier = loggia side
                float la=sx<0?-xo:xi,lb=sx<0?-xi:xo;
                Box(deck,la,lb,y0,y0+.008f,-HalfD,cr-.06f);                                       // loggia floor finish
                Box(clearGlass,la,lb,y0,y0+Balustrade,-HalfD+.05f,-HalfD+.062f,true);panes++;    // 1.10 m frameless glass
                W.Grupa=details;
                foreach(float t in new[]{.27f,.73f})Downlight(la+(lb-la)*t,(-HalfD+cr)*.5f,y1);  // soffit downlights
                W.Grupa=outside;
            }
        }
        // white frame grid: slab bands at every level, corner frames and the central frame
        for(int f=1;f<=Floors;f++)Box(render,-HalfW-.12f,HalfW+.12f,Level(f)-Slab-.06f,Level(f)+.06f,-HalfD-.12f,-HalfD,true);
        foreach(float sx in new[]{-1f,1f})Box(render,sx<0?-HalfW-.12f:xo,sx<0?-xo:HalfW+.12f,CeilingOf(0),RoofTop+.9f,-HalfD-.12f,-HalfD,true);
        Box(render,-.30f,.30f,CeilingOf(0),RoofTop+.9f,-HalfD-.12f,-HalfD,true);
        // two vertical timber strips (50 x 150 @ 150 mm) shading the inner bedroom windows, on black rails
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx<0?-3.9f:.9f,b=sx<0?-.9f:3.9f;
            Box(black,a,b,CeilingOf(0)-.06f,CeilingOf(0),-HalfD-.30f,-HalfD-.12f);
            Box(black,a,b,RoofTop+.84f,RoofTop+.9f,-HalfD-.30f,-HalfD-.12f);
            for(int i=0;i<20;i++){float x=a+.075f+i*.15f;Box(timber,x-.025f,x+.025f,CeilingOf(0),RoofTop+.84f,-HalfD-.30f,-HalfD-.15f);}
        }
        // flat roof: .90 m white parapet with black coping, stone roof paving
        Box(render,-HalfW-.12f,HalfW+.12f,RoofTop,RoofTop+.9f,-HalfD-.12f,-HalfD+.25f,true);
        Box(render,-HalfW,HalfW,RoofTop,RoofTop+.9f,HalfD-.25f,HalfD,true);
        foreach(float sx in new[]{-1f,1f})
            Box(render,sx<0?-HalfW:HalfW-.25f,sx<0?-HalfW+.25f:HalfW,RoofTop,RoofTop+.9f,-HalfD+.25f,HalfD-.25f,true);
        Box(black,-HalfW-.13f,HalfW+.13f,RoofTop+.9f,RoofTop+.93f,-HalfD-.13f,-HalfD+.26f);
        Box(black,-HalfW-.01f,HalfW+.01f,RoofTop+.9f,RoofTop+.93f,HalfD-.26f,HalfD+.01f);
        foreach(float sx in new[]{-1f,1f})
            Box(black,sx<0?-HalfW-.01f:HalfW-.26f,sx<0?-HalfW+.26f:HalfW+.01f,RoofTop+.9f,RoofTop+.93f,-HalfD+.26f,HalfD-.26f);
        Box(deck,-HalfW+.25f,HalfW-.25f,RoofTop,RoofTop+.02f,-HalfD+.25f,HalfD-.25f);
        return panes;
    }

    // ---- T06 (owner photo 3): black marble, gold curved balconies with LED, central glass core, ground + 4 ----
    static int FrontAndRoofT06()
    {
        float c=-HalfD+.15f;
        for(int f=1;f<Floors;f++)
            FacadeBand(true,c,-1f,f,Sym(new[]{new Part(-HalfW,-15.9f,Seg.Render),new Part(-15.9f,-13.4f,Seg.Glass),new Part(-13.4f,-11.6f,Seg.Door),
                new Part(-11.6f,-3.6f,Seg.Glass),new Part(-3.6f,-3.2f,Seg.Render),new Part(-3.2f,0f,Seg.Glass)},null));
        W.Grupa=outside;W.Wnetrze=false;
        // glass core: black marble fins with gold edges frame the central glazing from the ground to the crown
        float crown=RoofTop+1.2f;
        foreach(float sx in new[]{-1f,1f})
        {
            Box(marbleBlack,sx*3.2f-.12f,sx*3.2f+.12f,Ffl0,crown,-HalfD-.60f,-HalfD,true);
            Box(gold,sx*3.2f-.12f,sx*3.2f+.12f,Ffl0,crown,-HalfD-.62f,-HalfD-.60f);
        }
        // glass crown of the core above the roof, gold cap
        Box(facadeGlass,-3.08f,3.08f,RoofTop,crown,-HalfD-.01f,-HalfD+.01f,true);
        foreach(float sx in new[]{-1f,1f})Box(facadeGlass,sx*3.08f-.01f,sx*3.08f+.01f,RoofTop,crown,-HalfD,-HalfD+3.0f,true);
        Box(facadeGlass,-3.08f,3.08f,RoofTop,crown,-HalfD+2.99f,-HalfD+3.01f,true);
        Box(gold,-3.32f,3.32f,crown,crown+.12f,-HalfD-.62f,-HalfD+3.1f,true);
        // gold curved balconies (floors 1..top): rounded ends, frameless glass, LED under the edge
        int panes=0;
        for(int f=1;f<Floors;f++)
            foreach(float sx in new[]{-1f,1f})
            {
                float deckTop=Level(f)-.03f,a=sx<0?-HalfW+.6f:3.8f,b=sx<0?-3.8f:HalfW-.6f;
                var poly=RoundRect(a,b,-HalfD-2.0f,-HalfD,1.0f,new[]{true,true,false,false});
                W.Grupa=outside;
                Prism(poly,deckTop-.30f,deckTop,deck,gold,render,true);
                bool Outer(Vector2 p,Vector2 q)=>!(Mathf.Abs(p.y+HalfD)<.01f&&Mathf.Abs(q.y+HalfD)<.01f);
                panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer);
                W.Grupa=details;
                Ribbon(poly,.12f,deckTop-.30f-.012f,deckTop-.30f,.05f,led,false,Outer);
            }
        // flat roof: black marble parapet .90 m with a gold coping, stone roof paving
        W.Grupa=outside;
        foreach(float sx in new[]{-1f,1f})                                                  // front parapet stops at the core
            Box(marbleBlack,sx<0?-HalfW:3.32f,sx<0?-3.32f:HalfW,RoofTop,RoofTop+.9f,-HalfD,-HalfD+.25f,true);
        Box(marbleBlack,-HalfW,HalfW,RoofTop,RoofTop+.9f,HalfD-.25f,HalfD,true);
        foreach(float sx in new[]{-1f,1f})
            Box(marbleBlack,sx<0?-HalfW:HalfW-.25f,sx<0?-HalfW+.25f:HalfW,RoofTop,RoofTop+.9f,-HalfD+.25f,HalfD-.25f,true);
        foreach(float sx in new[]{-1f,1f})
            Box(gold,sx<0?-HalfW-.01f:3.32f,sx<0?-3.32f:HalfW+.01f,RoofTop+.9f,RoofTop+.94f,-HalfD-.01f,-HalfD+.26f);
        Box(gold,-HalfW-.01f,HalfW+.01f,RoofTop+.9f,RoofTop+.94f,HalfD-.26f,HalfD+.01f);
        foreach(float sx in new[]{-1f,1f})
            Box(gold,sx<0?-HalfW-.01f:HalfW-.26f,sx<0?-HalfW+.26f:HalfW+.01f,RoofTop+.9f,RoofTop+.94f,-HalfD+.26f,HalfD-.26f);
        Box(deck,-HalfW+.25f,HalfW-.25f,RoofTop,RoofTop+.02f,-HalfD+.25f,HalfD-.25f);
        return panes;
    }

    // ---- T07 (owner photo 5 + details of 4): white body, black marble corner volume, timber slats ----
    static int FrontAndRoofT07()
    {
        float c=-HalfD+.15f,top=RoofTop+.9f;
        const float m0=9.2f;                                                                // marble corner from the right living partition
        for(int f=1;f<Floors;f++)
            FacadeBand(true,c,-1f,f,new[]{new Part(-HalfW,-16.4f,Seg.Render),new Part(-16.4f,-14.4f,Seg.Glass),new Part(-14.4f,-12.6f,Seg.Door),
                new Part(-12.6f,-.6f,Seg.Glass),new Part(-.6f,.6f,Seg.Render),new Part(.6f,8.6f,Seg.Glass),new Part(8.6f,m0,Seg.Render),
                new Part(m0,10.2f,Seg.Marble),new Part(10.2f,15.8f,Seg.Glass),new Part(15.8f,HalfW,Seg.Marble)});
        W.Grupa=outside;W.Wnetrze=false;
        // black marble corner volume: 30 cm proud frame on the front and around the corner, ground to parapet
        Box(marbleBlack,m0-.2f,m0+.2f,CeilingOf(0),top,-HalfD-.30f,-HalfD,true);                   // inner fin
        Box(marbleBlack,HalfW-.6f,HalfW+.30f,Ffl0,top,-HalfD-.30f,-HalfD+.6f,true);                // corner post
        for(int f=1;f<=Floors;f++)Box(marbleBlack,m0-.2f,HalfW+.30f,Level(f)-Slab-.06f,Level(f)+.06f,-HalfD-.30f,-HalfD,true);
        float sw=-HalfD+1.9f;                                                                       // side window of the front living
        foreach(var z in new[]{(-HalfD+.6f,sw),(sw+2.2f,-3f)})
            Box(marbleBlack,HalfW,HalfW+.03f,Ffl0,top,z.Item1,z.Item2,true);
        for(int f=1;f<=Floors;f++)Box(marbleBlack,HalfW,HalfW+.03f,Level(f)-Slab,Level(f),sw,sw+2.2f);
        Box(marbleBlack,HalfW,HalfW+.30f,Ffl0,top,-3.3f,-3f,true);                                   // side end fin
        // sharp white balconies on the left, timber soffit and LED (photo 4), frameless glass 1.10 m
        int panes=0;
        for(int f=1;f<Floors;f++)
        {
            float deckTop=Level(f)-.03f;
            var poly=RoundRect(-HalfW+.6f,-.9f,-HalfD-2.0f,-HalfD,0f,new[]{false,false,false,false});
            W.Grupa=outside;
            Prism(poly,deckTop-.25f,deckTop,deck,render,timber,true);
            bool Outer(Vector2 p,Vector2 q)=>!(Mathf.Abs(p.y+HalfD)<.01f&&Mathf.Abs(q.y+HalfD)<.01f);
            panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer);
            W.Grupa=details;
            Ribbon(poly,.10f,deckTop-.25f-.012f,deckTop-.25f,.05f,led,false,Outer);
            foreach(float t in new[]{.2f,.4f,.6f,.8f})Downlight(-HalfW+.6f+(HalfW-1.5f)*t,-HalfD-1.0f,deckTop-.25f);
        }
        // timber slat screen (50 x 150 @ 150 mm) in front of the right bedrooms, on black rails
        W.Grupa=outside;
        Box(black,.9f,8.4f,CeilingOf(0)-.06f,CeilingOf(0),-HalfD-.30f,-HalfD-.12f);
        Box(black,.9f,8.4f,RoofTop-.06f,RoofTop,-HalfD-.30f,-HalfD-.12f);
        for(int i=0;i<50;i++){float x=.975f+i*.15f;Box(timber,x-.025f,x+.025f,CeilingOf(0),RoofTop,-HalfD-.30f,-HalfD-.15f);}
        // flat roof: white parapet .90 m with black coping; marble parapet over the corner volume
        Box(render,-HalfW,m0-.2f,RoofTop,top,-HalfD,-HalfD+.25f,true);
        Box(render,-HalfW,HalfW,RoofTop,top,HalfD-.25f,HalfD,true);
        Box(render,-HalfW,-HalfW+.25f,RoofTop,top,-HalfD+.25f,HalfD-.25f,true);
        Box(render,HalfW-.25f,HalfW,RoofTop,top,-3f,HalfD-.25f,true);
        Box(marbleBlack,m0-.2f,HalfW,RoofTop,top,-HalfD,-HalfD+.25f,true);
        Box(marbleBlack,HalfW-.25f,HalfW,RoofTop,top,-HalfD+.25f,-3f,true);
        Box(black,-HalfW-.01f,m0-.2f,top,top+.03f,-HalfD-.01f,-HalfD+.26f);
        Box(black,-HalfW-.01f,HalfW+.01f,top,top+.03f,HalfD-.26f,HalfD+.01f);
        Box(black,-HalfW-.01f,-HalfW+.26f,top,top+.03f,-HalfD+.26f,HalfD-.26f);
        Box(black,HalfW-.26f,HalfW+.01f,top,top+.03f,-3f,HalfD-.26f);
        Box(deck,-HalfW+.25f,HalfW-.25f,RoofTop,RoofTop+.02f,-HalfD+.25f,HalfD-.25f);
        return panes;
    }

    // ---- interior per floor: corridor, core (stair + lift), four apartments ----
    static void Interior(List<Light> lights)
    {
        W.Grupa=inside;W.Wnetrze=true;W.Komorka=14f;
        for(int f=0;f<Floors;f++)
        {
            float y0=Level(f),y1=CeilingOf(f),top=Level(f+1),cy=y1-.01f;
            float zFrontIn=f==0?-HalfD+1.15f:-HalfD+.30f,zc=CorridorZ0-CorridorWall*.5f,zr=CorridorZ1+CorridorWall*.5f;
            int no=0;
            string N()=>f+"."+(++no);
            // corridor: terrazzo, linear light, windows at both ends
            W.Grupa=inside;W.Wnetrze=true;
            Box(terrazzo,-HalfW+.3f,HalfW-.3f,y0,y0+Finish,CorridorZ0,CorridorZ1);
            Ceiling(-HalfW+.3f,HalfW-.3f,CorridorZ0,CorridorZ1,cy);
            Box(led,-HalfW+1.2f,HalfW-1.2f,cy-.012f,cy+.004f,.13f,.27f);
            lights.Add(W.Lampa("corridor light floor "+f,new Vector3(0,y1-.3f,.2f),.8f,14f));
            if(f==0||style==Style.MarbleGold)
            {
                // lobby from the entrance to the corridor (T06: the glass core's lounge on every floor):
                // terrazzo, black marble walls, LED lines
                float bay=zFrontIn-.15f;
                Box(terrazzo,-3f,3f,y0,y0+Finish,bay,CorridorZ0);
                foreach(float sx in new[]{-1f,1f})
                {
                    WallZ(paint,sx*3.1f,zFrontIn,CorridorZ0,y0,top,CorridorWall);
                    Box(marbleBlack,sx>0?3.0f-.012f:-3.0f,sx>0?3.0f:-3.0f+.012f,y0,cy,zFrontIn,zc);
                }
                Ceiling(-3f,3f,bay,CorridorZ0,cy);
                for(float z=bay+1.2f;z<=-1.5f;z+=1.6f)Box(led,-2.2f,2.2f,cy-.012f,cy+.004f,z-.04f,z+.04f);
                lights.Add(W.Lampa((f==0?"lobby":"glass core lounge floor "+f)+" light",new Vector3(0,y1-.5f,(bay+CorridorZ0)*.5f),1.0f,10f));
                float eA=Apartment(f,-HalfW+.3f,-3.2f,zFrontIn,zc,true,N(),lights);
                float eB=Apartment(f,3.2f,HalfW-.3f,zFrontIn,zc,false,N(),lights);
                WallX(paint,CorridorZ0,-HalfW+.3f,-3.0f,y0,top,CorridorWall,new Gap(eA-DoorW*.5f,eA+DoorW*.5f,DoorH));
                WallX(paint,CorridorZ0,3.0f,HalfW-.3f,y0,top,CorridorWall,new Gap(eB-DoorW*.5f,eB+DoorW*.5f,DoorH));
                Leaf(true,CorridorZ0,eA-DoorW*.5f,eA+DoorW*.5f,y0,-1f);Leaf(true,CorridorZ0,eB-DoorW*.5f,eB+DoorW*.5f,y0,-1f);
            }
            else
            {
                float eA=Apartment(f,-HalfW+.3f,-.1f,zFrontIn,zc,true,N(),lights,Loggia);
                float eB=Apartment(f,.1f,HalfW-.3f,zFrontIn,zc,false,N(),lights,Loggia);
                WallZ(paint,0f,zFrontIn-.14f,zc,y0,top,CorridorWall);                        // party wall
                WallX(paint,CorridorZ0,-HalfW+.3f,HalfW-.3f,y0,top,CorridorWall,
                    new Gap(eA-DoorW*.5f,eA+DoorW*.5f,DoorH),new Gap(eB-DoorW*.5f,eB+DoorW*.5f,DoorH));
                Leaf(true,CorridorZ0,eA-DoorW*.5f,eA+DoorW*.5f,y0,-1f);Leaf(true,CorridorZ0,eB-DoorW*.5f,eB+DoorW*.5f,y0,-1f);
            }
            // rear apartments and the rear corridor wall (open to the core between x -3.1..3.1)
            float eC=Apartment(f,-HalfW+.3f,-3.1f,HalfD-.30f,zr,true,N(),lights);
            float eD=Apartment(f,3.1f,HalfW-.3f,HalfD-.30f,zr,false,N(),lights);
            WallX(paint,CorridorZ1,-HalfW+.3f,-3.1f,y0,top,CorridorWall,new Gap(eC-DoorW*.5f,eC+DoorW*.5f,DoorH));
            WallX(paint,CorridorZ1,3.1f,HalfW-.3f,y0,top,CorridorWall,new Gap(eD-DoorW*.5f,eD+DoorW*.5f,DoorH));
            Leaf(true,CorridorZ1,eC-DoorW*.5f,eC+DoorW*.5f,y0,1f);Leaf(true,CorridorZ1,eD-DoorW*.5f,eD+DoorW*.5f,y0,1f);
            // core: walls, landing, lift lobby with closed steel lift doors, stairwell
            WallZ(paint,-3.05f,CorridorZ1,CoreBack,y0,top,.10f);WallZ(paint,3.05f,CorridorZ1,CoreBack,y0,top,.10f);
            WallZ(paint,0f,StairZ0,CoreBack,y0,top,CorridorWall);
            WallX(paint,LiftZ,.1f,3.0f,y0,top,CorridorWall);
            Box(steel,.85f,2.25f,y0+Finish,y0+DoorH,LiftZ-.115f,LiftZ-.10f);
            Box(black,1.545f,1.555f,y0+Finish,y0+DoorH,LiftZ-.12f,LiftZ-.115f);
            Box(steel,.75f,2.35f,y0+DoorH,y0+DoorH+.12f,LiftZ-.12f,LiftZ-.10f);
            Box(steel,2.45f,2.55f,y0+1.05f,y0+1.25f,LiftZ-.12f,LiftZ-.10f);              // call button panel
            Box(terrazzo,-3f,3f,y0,y0+Finish,CorridorZ1,StairZ0);
            Box(terrazzo,.1f,3f,y0,y0+Finish,StairZ0,LiftZ-.1f);
            if(f==0)Box(terrazzo,LaneA0,LaneB1,y0,y0+Finish,StairZ0,CoreBack);
            Ceiling(-3f,3f,CorridorZ1,StairZ0,cy);Ceiling(.1f,3f,StairZ0,LiftZ-.1f,cy);
            Downlight(-1.5f,(CorridorZ1+StairZ0)*.5f,cy);Downlight(1.55f,(StairZ0+LiftZ)*.5f,cy);
            if(f<Floors-1)Stair(f);
            else
            {
                // top floor: no flight up -> guard the stairwell (1.10 m glass) and close its ceiling
                float zMid=StairZ0+FlightLength(f-1);
                Box(clearGlass,-1.565f,-1.535f,y0,y0+Balustrade,StairZ0,zMid,true);
                Box(clearGlass,LaneA0,-1.535f,y0,y0+Balustrade,StairZ0-.015f,StairZ0+.015f,true);
                W.Rura(steel,new Vector3(LaneA0,y0+Balustrade,StairZ0),new Vector3(-1.55f,y0+Balustrade,StairZ0),.022f,8);
                W.Rura(steel,new Vector3(-1.55f,y0+Balustrade,StairZ0),new Vector3(-1.55f,y0+Balustrade,zMid),.022f,8);
                Ceiling(LaneA0,LaneB1,StairZ0,CoreBack,cy);
            }
        }

    }

    // Region palms (miasto_palmy) on the front lawn and behind the building.
    static int Palms(Transform root,Transform parent)
    {
        var palm=BraxMiastoAssety.Model("miasto_palmy","palma");
        if(!palm){BraxBuildQuality.Warn("DO POPRAWKI APT_PALMS_MISSING: miasto_palmy/palma");return 0;}
        var spots=new List<Vector2>();
        foreach(float sx in new[]{-1f,1f})
        {
            foreach(float z in new[]{-17f,-23f,-29f})spots.Add(new Vector2(sx*7.5f,z));
            foreach(float z in new[]{18f,25f})spots.Add(new Vector2(sx*12f,z));
        }
        int placed=0;
        foreach(var p in spots)
        {
            var world=root.TransformPoint(new Vector3(p.x,SiteY,p.y));
            var o=BraxMiastoAssety.Postaw(parent,palm,"apartment garden palm",world,(p.x*11f+p.y*5f)%360f,1f,false,true);
            if(!o)continue;
            o.transform.localScale=Vector3.one;                     // the parent already carries K
            BraxMiastoAssety.LodyZWezlow(o,new[]{.25f,.08f,.012f});
            placed++;
        }
        return placed;
    }
}

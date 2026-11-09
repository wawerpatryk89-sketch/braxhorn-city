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
// Rooms are FINISHED (oak floors, painted walls, white ceilings with downlights,
// marble bathrooms, doors with walnut casings, terrazzo corridors, stair, lift)
// and EMPTY: no furniture, no kitchen units, no sanitary fittings (owner).
public static class BraxChampionsApartments
{
    static float K => BraxClaudeOsiedle.K;
    public const float GroundStorey=4.20f, Storey=3.15f, Slab=.30f, Ffl0=.30f, SiteY=.16f, PlotBaseY=-.25f;
    public const float HalfW=16f, HalfD=8.5f, PlotHalf=30f, Balustrade=1.10f;
    public const int Floors=4;                                   // ground + 3
    public const float Riser=.175f, Tread=.28f, DoorW=.90f, DoorH=2.10f, Partition=.12f, CorridorWall=.20f;
    const float Finish=.012f;                                    // floor finish over the slab
    // Core: dog-leg stair (lanes 1.30 m) west of the axis, lift east of it.
    const float LaneA0=-3.0f, LaneA1=-1.7f, LaneB0=-1.4f, LaneB1=-.1f, StairZ0=2.4f, CoreBack=HalfD-.30f;
    const float CorridorZ0=-.7f, CorridorZ1=1.1f, LiftZ=5.4f;
    public static float Level(int f)=>f<=0?Ffl0:Ffl0+GroundStorey+(f-1)*Storey;   // finished floor
    public static float CeilingOf(int f)=>Level(f+1)-Slab;
    public static float RoofTop=>Level(Floors);
    public static int Risers(int f){int n=Mathf.RoundToInt((Level(f+1)-Level(f))/Riser);return n%2==1?n+1:n;}
    public static float FlightLength(int f)=>Risers(f)/2*Tread;

    // Plan inventory (plan_inventory.json, 60x60 "rental" plots). Front = local -Z,
    // turned toward the nearest road: T04/T05 face road Z650, T06/T07 road Z330.
    struct Plot { public string id, name; public float x, z, yaw; }
    static readonly Plot[] Plots={
        new Plot{id="T04",name="APARTAMENTY 1",x=870,z=570,yaw=180f},
    };
    public static readonly string[] Ids={"T04"};

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
    static Material render,granite,timber,walnut,black,facadeGlass,clearGlass,led,steel,
        oak,paint,ceiling,bathFloor,bathWall,terrazzo,marbleBlack,deck,grass,paving;

    static void Materials(Dictionary<string,Material> ext,string outFolder)
    {
        render=M("apt_white_render","white_stucco_02",2.0f,new Color(.97f,.96f,.94f),.90f);
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
    enum Seg{Glass,Door,Render,Timber}
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
                var mat=p.s==Seg.Timber?timber:render;float t=.30f;
                if(alongX)Box(mat,p.a,p.b,y0,y1,c-t*.5f,c+t*.5f,true);else Box(mat,c-t*.5f,c+t*.5f,y0,y1,p.a,p.b,true);
                W.Grupa=inside;W.Wnetrze=true;                                     // painted inner face
                float i0=nrm<0?c+t*.5f:c-t*.5f-.01f,i1=i0+.01f;
                if(alongX)Box(paint,p.a,p.b,y0,y1,i0,i1);else Box(paint,i0,i1,y0,y1,p.a,p.b);
            }
        }
        W.Grupa=outside;W.Wnetrze=false;
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
    static float Apartment(int f,float x0,float x1,float zf,float zc,bool innerAtX1,string number,List<Light> lights)
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
        // floors: oak, bathroom porcelain tiles (finish 12 mm over the slab)
        Box(oak,Lo(X(0),X(lu)),Hi(X(0),X(lu)),y0,y0+Finish,az0,az1);
        Box(oak,Lo(X(lu),X(w)),Hi(X(lu),X(w)),y0,y0+Finish,fz0,fz1);
        Box(oak,Lo(X(lu),X(uHall)),Hi(X(lu),X(uHall)),y0,y0+Finish,sz0,sz1);
        float bx0=Lo(X(uHall),X(w)),bx1=Hi(X(uHall),X(w));
        Box(bathFloor,bx0,bx1,y0,y0+Finish,sz0,sz1);
        // living | rest: full depth, 1.40 m opening into the hall
        float zm=(zs+zc)*.5f;
        WallZ(paint,X(lu),Lo(zGlass,zc),Hi(zGlass,zc),y0,top,Partition,new Gap(zm-.70f,zm+.70f,2.30f));
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
        Ceiling(x0,x1,az0,az1,cy);
        float zRooms=(zf+zs)*.5f,zStrip=(zs+zc)*.5f;
        Downlight(X(lu*.5f),(zf+zc)*.5f,cy);Downlight(X(lu*.5f),zRooms,cy);
        Downlight(X(lu+bu*.5f),zRooms,cy);Downlight(X(uB2+bu*.5f),zRooms,cy);
        Downlight(X((lu+uHall)*.5f),zStrip,cy);Downlight(X(uHall+bathU*.5f),zStrip,cy);
        lights.Add(W.Lampa("apartment "+number+" warm light",new Vector3(X(lu*.5f),y1-.45f,(zf+zc)*.5f),.9f,9f));
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
        System.IO.Directory.CreateDirectory(outFolder+"/Materials");System.IO.Directory.CreateDirectory(outFolder+"/Prefabs");
        Materials(ext,outFolder);apartments=0;doors=0;
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

        // ---- site: lawn over the whole plot, apron, terrace, ramp, path to the sidewalk ----
        Box(grass,-PlotHalf,PlotHalf,PlotBaseY,SiteY,-PlotHalf,PlotHalf,true);
        Box(paving,-HalfW-1.6f,HalfW+1.6f,SiteY,SiteY+.03f,-HalfD-3.4f,HalfD+2.1f,true);
        Box(granite,-HalfW,HalfW,PlotBaseY,Ffl0,-HalfD,HalfD,true);                       // plinth
        Box(granite,-HalfW,HalfW,SiteY,Ffl0,-HalfD-2.6f,-HalfD,true);                     // front terrace
        Wedge(granite,-2.0f,2.0f,-HalfD-5.5f,SiteY+.03f,-HalfD-2.6f,Ffl0,SiteY);          // 3.8 % ramp, no step
        Box(paving,-2.5f,2.5f,SiteY,SiteY+.03f,-63.2f,-HalfD-3.4f,true);                   // to the sidewalk (road Z650)
        W.Grupa=details;
        for(int i=0;i<8;i++)
            foreach(float sx in new[]{-1f,1f})
            {
                float z=-16f-i*6f,x=sx*3.1f;
                Box(black,x-.08f,x+.08f,SiteY,SiteY+.85f,z-.08f,z+.08f,true);
                Box(led,x-.085f,x+.085f,SiteY+.85f,SiteY+.95f,z-.085f,z+.085f);
            }

        // ---- structure: floor slabs with the stairwell opening, roof slab ----
        W.Grupa=outside;
        for(int f=1;f<=Floors;f++)
        {
            float y=Level(f);
            if(f<Floors)
            {
                Box(render,-HalfW,HalfW,y-Slab,y,-HalfD,StairZ0,true);
                Box(render,-HalfW,LaneA0,y-Slab,y,StairZ0,HalfD,true);
                Box(render,LaneB1,HalfW,y-Slab,y,StairZ0,HalfD,true);
                Box(render,LaneA0,LaneB1,y-Slab,y,CoreBack,HalfD,true);
            }
            else Box(render,-HalfW,HalfW,y-Slab,y,-HalfD,HalfD,true);
        }

        // ---- façades: front glass with timber + render piers, rear and side windows ----
        for(int f=0;f<Floors;f++)
        {
            var front=f==0
                ? new[]{new Part(-HalfW,-15.2f,Seg.Render),new Part(-15.2f,-3.2f,Seg.Glass),new Part(-3.2f,-1.6f,Seg.Render),
                        new Part(-1.6f,1.6f,Seg.Door),new Part(1.6f,3.2f,Seg.Render),new Part(3.2f,15.2f,Seg.Glass),new Part(15.2f,HalfW,Seg.Render)}
                : new[]{new Part(-HalfW,-13.2f,Seg.Render),new Part(-13.2f,-11.8f,Seg.Timber),new Part(-11.8f,-10.8f,Seg.Glass),
                        new Part(-10.8f,-9.0f,Seg.Door),new Part(-9.0f,-.1f,Seg.Glass),new Part(-.1f,.1f,Seg.Render),new Part(.1f,9.0f,Seg.Glass),
                        new Part(9.0f,10.8f,Seg.Door),new Part(10.8f,11.8f,Seg.Glass),new Part(11.8f,13.2f,Seg.Timber),new Part(13.2f,HalfW,Seg.Render)};
            FacadeBand(true,f==0?-HalfD+1.0f:-HalfD+.15f,-1f,f,front);
            FacadeBand(true,HalfD-.15f,1f,f,new[]{new Part(-HalfW,-15f,Seg.Render),new Part(-15f,-13.5f,Seg.Glass),
                new Part(-13.5f,-11.7f,f==0?Seg.Glass:Seg.Door),new Part(-11.7f,-4.5f,Seg.Glass),new Part(-4.5f,4.5f,Seg.Render),
                new Part(4.5f,11.7f,Seg.Glass),new Part(11.7f,13.5f,f==0?Seg.Glass:Seg.Door),new Part(13.5f,15f,Seg.Glass),new Part(15f,HalfW,Seg.Render)});
            foreach(float sx in new[]{-1f,1f})
                FacadeBand(false,sx*(HalfW-.15f),sx,f,new[]{new Part(-HalfD,-6.6f,Seg.Render),new Part(-6.6f,-4.4f,Seg.Glass),
                    new Part(-4.4f,-.5f,Seg.Render),new Part(-.5f,.9f,Seg.Glass),new Part(.9f,3.6f,Seg.Render),
                    new Part(3.6f,5.8f,Seg.Glass),new Part(5.8f,HalfD,Seg.Render)});
        }
        W.Grupa=outside;W.Wnetrze=false;
        // ground floor: dark granite piers, LED downlights in the 1 m deep entrance soffit
        foreach(float x in new[]{-15.6f,-9f,9f,15.6f})Box(granite,x-.4f,x+.4f,Ffl0,CeilingOf(0),-HalfD-.05f,-HalfD+.95f,true);
        W.Grupa=details;
        for(float x=-14f;x<=14.01f;x+=2f)Downlight(x,-HalfD+.48f,CeilingOf(0));
        // timber fins (50 x 150 @ 150 mm) on a black backing, floors 1-3, both front piers
        foreach(float xa in new[]{-13.2f,11.8f})
        {
            Box(black,xa,xa+1.4f,CeilingOf(0),RoofTop,-HalfD-.03f,-HalfD);
            for(int i=0;i<9;i++){float x=xa+.075f+i*.15f;Box(timber,x,x+.05f,CeilingOf(0),RoofTop,-HalfD-.18f,-HalfD-.03f);}
        }

        // ---- curved front balconies (floors 1-3): 22 x 2.2 m, rounded ends, frameless glass, LED ----
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
        // ---- rear balconies 3.6 x 1.6 for the rear living rooms (floors 1-3) ----
        W.Grupa=outside;
        for(int f=1;f<Floors;f++)
            foreach(float cx in new[]{-12.6f,12.6f})
            {
                float deckTop=Level(f)-.03f;
                var poly=RoundRect(cx-1.8f,cx+1.8f,HalfD,HalfD+1.6f,0f,new[]{false,false,false,false});
                Prism(poly,deckTop-.25f,deckTop,deck,render,render,true);
                bool Outer(Vector2 a,Vector2 b)=>!(Mathf.Abs(a.y-HalfD)<.01f&&Mathf.Abs(b.y-HalfD)<.01f);
                panes+=Ribbon(poly,.06f,deckTop,deckTop+Balustrade,.012f,clearGlass,true,Outer);
            }

        // ---- roof: rounded black overhang, timber soffit with downlights, LED line under the edge ----
        var roof=RoundRect(-HalfW-1.4f,HalfW+1.4f,-HalfD-2.6f,HalfD+.9f,2.0f,new[]{true,true,true,true});
        W.Grupa=outside;
        Prism(roof,RoofTop,RoofTop+.45f,black,black,timber,true);
        W.Grupa=details;
        Ribbon(roof,.08f,RoofTop-.012f,RoofTop,.05f,led,false,null);
        for(float x=-15f;x<=15.01f;x+=1.5f)
            foreach(float z in new[]{-HalfD-1.7f,-HalfD-.8f})Downlight(x,z,RoofTop);
        // No flat building-name text on the façade (rule ZASADA-SZYLDY-3D 9.11):
        // a sign for this building waits for the owner's artwork / brand decision.

        // ---- interior per floor: corridor, core (stair + lift), four apartments ----
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
            if(f==0)
            {
                // lobby from the entrance to the corridor: terrazzo, black marble walls, LED lines
                Box(terrazzo,-3f,3f,y0,y0+Finish,-HalfD+1.0f,CorridorZ0);
                foreach(float sx in new[]{-1f,1f})
                {
                    WallZ(paint,sx*3.1f,zFrontIn,CorridorZ0,y0,top,CorridorWall);
                    Box(marbleBlack,sx>0?3.0f-.012f:-3.0f,sx>0?3.0f:-3.0f+.012f,y0,cy,zFrontIn,zc);
                }
                Ceiling(-3f,3f,-HalfD+1.0f,CorridorZ0,cy);
                for(float z=-6.3f;z<=-1.5f;z+=1.6f)Box(led,-2.2f,2.2f,cy-.012f,cy+.004f,z-.04f,z+.04f);
                lights.Add(W.Lampa("lobby light",new Vector3(0,y1-.5f,-4f),1.0f,10f));
                float eA=Apartment(0,-HalfW+.3f,-3.2f,zFrontIn,zc,true,N(),lights);
                float eB=Apartment(0,3.2f,HalfW-.3f,zFrontIn,zc,false,N(),lights);
                WallX(paint,CorridorZ0,-HalfW+.3f,-3.0f,y0,top,CorridorWall,new Gap(eA-DoorW*.5f,eA+DoorW*.5f,DoorH));
                WallX(paint,CorridorZ0,3.0f,HalfW-.3f,y0,top,CorridorWall,new Gap(eB-DoorW*.5f,eB+DoorW*.5f,DoorH));
                Leaf(true,CorridorZ0,eA-DoorW*.5f,eA+DoorW*.5f,y0,-1f);Leaf(true,CorridorZ0,eB-DoorW*.5f,eB+DoorW*.5f,y0,-1f);
            }
            else
            {
                float eA=Apartment(f,-HalfW+.3f,-.1f,zFrontIn,zc,true,N(),lights);
                float eB=Apartment(f,.1f,HalfW-.3f,zFrontIn,zc,false,N(),lights);
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
        Debug.Log("APT_BUILDING "+id+" '"+plot.name+"' body="+(2*HalfW)+"x"+(2*HalfD)+" floors=ground+"+(Floors-1)+
            " groundStorey="+GroundStorey+" storey="+Storey+" roofTop="+RoofTop+" apartments="+apartments+" doors="+doors+
            " balconyGlassPanes="+panes+" balustrade="+Balustrade+" stair=dog-leg riser"+Riser+"/tread"+Tread+
            " lift=1 lights="+lights.Count+" palms="+palms+" meshes="+saved+" datumY="+datum.ToString("F3")+
            " yaw="+plot.yaw+" interiors=finished_empty_no_furniture");
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

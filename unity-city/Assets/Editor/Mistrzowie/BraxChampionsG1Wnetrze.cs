using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 interior (board "RZUT PARTERU - UKŁAD FUNKCJONALNY", "LEGENDA STREF"): the malls on both levels (central N-S under
// the vault, north and south E-W), the triple-height entrance hall, the atrium void with glass-railed galleries and two
// bridges, 33 shops in units with glass fronts (automatic sliding doors), fascias with 3D names and fitted interiors by
// trade (fashion, shoes, electronics, cosmetics, books, toys, jewellery - silver, pharmacy, opticians, phones, home...),
// the anchors (BRAXHORN MARKET on P0, SPORT ARENA on P1 in the east wing), the double-height food court (8 counters with
// menus, 30 tables), toilets (M / K / accessible / baby), the service corridor to the goods-in, finished ceilings
// (white with linear LED lines in the malls, downlights in the shops), the mall furniture (benches, planters, directory
// boards, bins). Everything at real size x K, functional (doors open, seats are seats), nothing grey at any glass.
public static partial class BraxChampionsD2
{
    // a shop unit: rectangle, the side its front faces ('N' +z, 'S' -z, 'E' +x, 'W' -x), the level, the trade
    struct G1Unit{public string name,trade;public float x0,x1,z0,z1;public char face,face2;public int level;}
    const float G1ServiceZ1=-51.85f;                       // the P0 service corridor's north wall line (z -54.7..-51.85, x 30..85)
    static readonly List<G1Unit> g1Units=new List<G1Unit>();
    static float LevelY(int l)=>l==0?G1F0:G1F1;
    // a piece of furniture / equipment: a rounded box, radius from its thinnest side (owner 11.11 17:53: no blocks)
    static void F(Material mat,float x0,float x1,float y0,float y1,float z0,float z1,bool col=false)
    {
        float th=Mathf.Min(x1-x0,Mathf.Min(y1-y0,z1-z0));if(th<=0f)return;
        RB(mat,x0,x1,y0,y1,z0,z1,Mathf.Clamp(th*.3f,.004f,.035f),col,th>.1f?2:1);
    }
    static float MallCeil(int l)=>l==0?G1Ceil0:G1Ceil1;
    static float ShopCeil(int l)=>MallCeil(l);            // = the façade glass head (5.12 / 11.12): no ceiling void seen from the street
    static bool OnFacade(G1Unit u)=>u.z1>=G1HD-.31f;      // the front units: their back is the façade's shop glazing

    static void G1UnitPlan()
    {
        g1Units.Clear();
        string[] p0={"MODA NOVA","URBAN STYLE","OBUWIE KROK","PERFUMERIA AURA","TECHNO SPOT","KSIĘGARNIA SŁOWO","JUBILER SREBRO","APTEKA ZDROWIE",
                     "OPTYK WIZJA","GSM CENTRUM","ZABAWKI HOP","DOM I WNĘTRZA","BIELIZNA LUNA","ZEGARKI CZAS","TORBY I WALIZKI","KWIACIARNIA","DROGERIA LILA"};
        string[] t0={"fashion","fashion","shoes","cosmetics","electronics","books","jewellery","pharmacy","optician","phones","toys","home","fashion","jewellery","bags","flowers","cosmetics"};
        string[] p1={"SPORT STYLE","DENIM 77","KIDS WORLD","GAMES ZONE","FOTO STUDIO","ELEGANCJA","BUTY MIEJSKIE","KOSMETYKI NATURA",
                     "AUDIO HIFI","PAPIERNIA","PRALNIA EXPRESS","BIURO PODRÓŻY","CAFÉ PANORAMA","TELEFONY PLUS","LODY GELATO","SALON FRYZJERSKI","BIURO CENTRUM"};
        string[] t1={"sport","fashion","toys","electronics","electronics","fashion","shoes","cosmetics","electronics","books","service","service","cafe","phones","cafe","salon","service"};
        for(int l=0;l<2;l++)
        {
            var names=l==0?p0:p1;var trades=l==0?t0:t1;int k=0;
            void U(float x0,float x1,float z0,float z1,char f,char f2='\0'){g1Units.Add(new G1Unit{name=names[k],trade=trades[k],x0=x0,x1=x1,z0=z0,z1=z1,face=f,face2=f2,level=l});k++;}
            // front units (z 40..55) facing the north mall
            U(-47f,-33.5f,G1NorthMallZ1,G1HD-.3f,'S');U(-33.5f,-20f,G1NorthMallZ1,G1HD-.3f,'S','E');U(20f,33.5f,G1NorthMallZ1,G1HD-.3f,'S','W');U(33.5f,47f,G1NorthMallZ1,G1HD-.3f,'S');
            // central blocks: four units on each side of the central mall, a north-facing and a south-facing unit at the wings
            foreach(float sx in new[]{-1f,1f})
            {
                float a=sx*G1MallX,b=sx*30f;char f=sx<0?'E':'W';
                for(int i=0;i<4;i++){float z0=G1SouthMallZ1+i*(G1NorthMallZ0-G1SouthMallZ1)/4f,z1=G1SouthMallZ1+(i+1)*(G1NorthMallZ0-G1SouthMallZ1)/4f;U(Mathf.Min(a,b),Mathf.Max(a,b),z0,z1,f,i==0?'S':i==3?'N':'\0');}   // the corner units glazed to the N / S mall too
                float c=sx*30f,d=sx*G1EastX0;
                U(Mathf.Min(c,d),Mathf.Max(c,d),9f,G1NorthMallZ0,'N');U(Mathf.Min(c,d),Mathf.Max(c,d),G1SouthMallZ1,9f,'S');
            }
            // the SE block (x 30..47) on the south mall: P0 behind it the service corridor (z -55..-51.85), P1 to the façade
            U(G1FoodX,G1EastX0,l==0?G1ServiceZ1:-G1HD+.3f,G1SouthMallZ0,'N');
        }
    }

    static void G1Interior()
    {
        ShopMaterials();
        G1UnitPlan();
        G1Floors();
        foreach(var u in g1Units)ShopUnit(u);
        Flush("G1 interior | shop units");
        G1Anchor();Flush("G1 interior | BRAXHORN MARKET (anchor P0)");
        G1SportStore();Flush("G1 interior | SPORT ARENA (P1 east wing)");
        G1FoodCourt();Flush("G1 interior | food court");
        G1Services();Flush("G1 interior | toilets, service corridor, goods-in");
        G1Ceilings();Flush("G1 interior | ceilings, lights");
        G1Galleries();Flush("G1 interior | galleries, bridges, balustrades");
        G1MallFurniture();
    }

    // ---- floors: the malls' large porcelain field with dark bands along the shop fronts (strips, never stacked), the
    // shops their own; P0 on the ground slab (.30 -> .32), P1 as a screed on the slab (6.20 -> 6.32)
    static void G1Floors()
    {
        foreach(int l in new[]{0,1})
        {
            float y=LevelY(l),y0=l==0?WalkY:G1F1-.12f;
            void F(Material mat,float x0,float x1,float z0,float z1){if(x1-x0>.01f&&z1-z0>.01f)B(mat,x0,x1,y0,y,z0,z1,true);}
            // north mall (x -47..47, z 28..40) - bands .6 along both fronts; P1: also the bridge over the void
            F(gMallBand,G1CinemaX1,G1EastX0,G1NorthMallZ1-.6f,G1NorthMallZ1);F(gMallBand,G1CinemaX1,G1EastX0,G1NorthMallZ0,G1NorthMallZ0+.6f);
            if(l==0)F(gMallFloor,G1CinemaX1,G1EastX0,G1NorthMallZ0+.6f,G1NorthMallZ1-.6f);
            else{F(gMallFloor,G1CinemaX1,-20f,G1NorthMallZ0+.6f,G1NorthMallZ1-.6f);F(gMallFloor,20f,G1EastX0,G1NorthMallZ0+.6f,G1NorthMallZ1-.6f);F(gMallFloor2,-20f,20f,G1NorthMallZ0+.6f,G1NorthMallZ1-.6f);}
            // south mall (z -25..-13)
            F(gMallBand,G1CinemaX1,G1EastX0,G1SouthMallZ1-.6f,G1SouthMallZ1);
            if(l==0){F(gMallFloor,G1CinemaX1,G1EastX0,G1SouthMallZ0,G1SouthMallZ1-.6f);}
            else{F(gMallFloor,G1CinemaX1,-G1FoodX,G1SouthMallZ0,G1SouthMallZ1-.6f);F(gMallFloor,G1FoodX,G1EastX0,G1SouthMallZ0,G1SouthMallZ1-.6f);F(gMallFloor2,-G1FoodX,G1FoodX,G1SouthMallZ0+.3f,G1SouthMallZ1-.6f);}
            // central mall (x -12..12, z -13..28): P0 the whole floor (a terrazzo field under the void), P1 the side galleries
            if(l==0)
            {
                F(gMallBand,-G1MallX,-G1MallX+.6f,G1SouthMallZ1,G1NorthMallZ0);F(gMallBand,G1MallX-.6f,G1MallX,G1SouthMallZ1,G1NorthMallZ0);
                F(gMallFloor,-G1MallX+.6f,-G1VoidX,G1SouthMallZ1,G1NorthMallZ0);F(gMallFloor,G1VoidX,G1MallX-.6f,G1SouthMallZ1,G1NorthMallZ0);
                F(gMallFloor2,-G1VoidX,G1VoidX,G1SouthMallZ1,G1NorthMallZ0);
                // the entrance hall (x -20..20, z 40..55) and the projection's floor to the curtain wall
                F(gMallFloor2,-20f,20f,G1NorthMallZ1,G1HD+G1HallFront-.12f);
            }
            else
            {
                F(gMallBand,-G1MallX,-G1MallX+.6f,G1SouthMallZ1,G1NorthMallZ0);F(gMallBand,G1MallX-.6f,G1MallX,G1SouthMallZ1,G1NorthMallZ0);
                F(gMallFloor,-G1MallX+.6f,-G1VoidX,G1SouthMallZ1,G1NorthMallZ0);F(gMallFloor,G1VoidX,G1MallX-.6f,G1SouthMallZ1,G1NorthMallZ0);
            }
        }
    }

    // ---- a shop unit: walls, the glass front with its automatic door, the fascia with the 3D name, floor, ceiling and
    // downlights, the fit-out by trade
    static Material ShopFloor(string t)=>t=="fashion"||t=="shoes"||t=="bags"?oakSlat:t=="jewellery"||t=="cosmetics"||t=="optician"?StoneTop():t=="electronics"||t=="phones"||t=="sport"||t=="games"?gMallBand:t=="books"||t=="cafe"||t=="home"?walnut:gMallFloor2;
    static Material whiteMarbleMat;
    static Material StoneTop()=>gStoneTop;                                   // light limestone (Poly Haven marble_01)
    static Material ShopWall(string t)
    {
        switch(t)
        {
            case "fashion":case "bags":return gWallPanels;
            case "shoes":case "books":case "cafe":return gWood;
            case "electronics":case "phones":case "games":return Inside(Tex("g1_wall_graphite","white_stucco_02",2.0f,new Color(.30f,.31f,.34f)),.9f);
            case "toys":return Inside(Tex("g1_wall_toys_yellow","white_stucco_02",2.0f,new Color(1.41f,1.18f,.44f)),.9f);
            case "pharmacy":return Inside(Tex("g1_wall_pharmacy_green","white_stucco_02",2.0f,new Color(1.18f,1.35f,1.24f)),.9f);
            case "sport":return Inside(Tex("g1_wall_sport_navy","white_stucco_02",2.0f,new Color(.15f,.24f,.47f)),.9f);
            case "home":return Inside(Tex("g1_wall_home_white","white_stucco_02",2.0f,new Color(1.42f,1.40f,1.35f)),.9f);   // DOM I WNĘTRZA: light plaster, not bare brown
            default:return gWallWarm;
        }
    }
    static bool OpenTo(G1Unit u,char f)=>u.face==f||u.face2==f;
    static void ShopUnit(G1Unit u)
    {
        float y=LevelY(u.level),yc=ShopCeil(u.level),top=u.level==0?G1Slab:G1Roof-.9f;
        var wall=ShopWall(u.trade);
        // the unit's own walls (8 cm linings inside its line) on the closed sides, up to the slab above
        void WallX(float z,float x0,float x1,float dz){x0+=OpenTo(u,'W')?.02f:0f;x1-=OpenTo(u,'E')?.02f:0f;B(wall,x0,x1,y,yc,Mathf.Min(z,z+dz),Mathf.Max(z,z+dz),true);B(gWhitePanel,x0,x1,yc,top,Mathf.Min(z,z+dz),Mathf.Max(z,z+dz));}
        void WallZ(float x,float z0,float z1,float dx){B(wall,Mathf.Min(x,x+dx),Mathf.Max(x,x+dx),y,yc,z0,z1,true);B(gWhitePanel,Mathf.Min(x,x+dx),Mathf.Max(x,x+dx),yc,top,z0,z1);}
        bool fac=OnFacade(u);                                                     // open to the façade's shop glazing at the back
        float zb=fac?G1HD-.26f:u.z1;                                              // just inside the mullions' inner face (54.75)
        bool oN=OpenTo(u,'N')||fac,oS=OpenTo(u,'S'),oE=OpenTo(u,'E'),oW=OpenTo(u,'W');
        if(!oN)WallX(u.z1,u.x0,u.x1,-.08f);
        if(!oS)WallX(u.z0,u.x0,u.x1,.08f);
        // open fronts: the linings stop 2 cm behind the front line (the fascia and the end posts cover them, no shared face)
        float wz0=u.z0+(oS?.02f:.08f),wz1=zb-(oN?(fac?0f:.02f):.08f);
        if(!oE)WallZ(u.x1,wz0,wz1,-.08f);
        if(!oW)WallZ(u.x0,wz0,wz1,.08f);
        // floor (on the slab / screed), ceiling with downlights
        float ix0=u.x0+(oW?0f:.08f),ix1=u.x1-(oE?0f:.08f),iz0=u.z0+(oS?0f:.08f),iz1=fac?zb:u.z1-(oN?0f:.08f);
        B(ShopFloor(u.trade),ix0,ix1,u.level==0?WalkY:G1F1-.12f,y,iz0,iz1,true);
        // the ceiling by trade (owner 18:28 no white, 19:31 not one colour everywhere): electronics and sport black with LED
        // lines, the "clean" trades (jewellery, cosmetics, optician, pharmacy, café, salon, services, flowers) warm plaster with
        // downlights, fashion / shoes / books / toys / home timber slats on black felt
        string tr=u.trade;
        if(tr=="electronics"||tr=="phones"||tr=="games"||tr=="sport")
        {
            Q(gCinemaDark,new Vector3(ix0,yc,iz0),new Vector3(ix1,yc,iz0),new Vector3(ix1,yc,iz1),new Vector3(ix0,yc,iz1),Vector3.down);
            if(ix1-ix0>iz1-iz0)for(float z=iz0+1.2f;z<iz1-.8f;z+=2.4f)B(led,ix0+.6f,ix1-.6f,yc-.03f,yc,z-.03f,z+.03f);
            else for(float x=ix0+1.2f;x<ix1-.8f;x+=2.4f)B(led,x-.03f,x+.03f,yc-.03f,yc,iz0+.6f,iz1-.6f);
        }
        else if(tr=="jewellery"||tr=="cosmetics"||tr=="optician"||tr=="pharmacy"||tr=="cafe"||tr=="salon"||tr=="service"||tr=="flowers")
        {
            Q(gWallWarm,new Vector3(ix0,yc,iz0),new Vector3(ix1,yc,iz0),new Vector3(ix1,yc,iz1),new Vector3(ix0,yc,iz1),Vector3.down);
            for(float x=ix0+1.3f;x<ix1-.9f;x+=1.8f)for(float z=iz0+1.3f;z<iz1-.9f;z+=1.8f){m.Cylinder(frame,new Vector3(x,yc-.014f,z),.075f,.014f,16,Quaternion.identity,false,true);m.Cylinder(ledWarm,new Vector3(x,yc-.026f,z),.055f,.012f,16,Quaternion.identity,false,true);}
        }
        else SlatCeiling(ix0+.01f,ix1-.01f,iz0+.02f,iz1-.02f,yc,null);                                    // timber slats on black felt, LED lines (no white ceilings)
        // the main front with its automatic door; a corner unit's second frontage glazed (no door), both with the name
        bool along=u.face=='S'||u.face=='N';
        ShopFront(u,u.face,true,along?u.x0:u.z0,along?u.x1:u.z1);
        if(u.face2!='\0')
        {
            float a=u.face2=='S'||u.face2=='N'?u.x0:u.z0,b=u.face2=='S'||u.face2=='N'?u.x1:zb;
            if(u.face2=='S'||u.face2=='N'){if(u.face=='E')a+=.08f;else b-=.08f;}       // the closed end has the side wall
            ShopFront(u,u.face2,false,a,b);
        }
        ShopFitOut(u,ix0,ix1,iz0,iz1,y,fac);
        G1Shops++;
    }
    // a shop front on side f of the unit over t0..t1: toughened glass panes (max 2.4) between slim black mullions to 4.0,
    // a black head and sill, the automatic sliding door in the middle (the train's runtime: opens on approach), above it
    // the dark fascia to the mall ceiling with the name in 3D letters (white, lit; the pharmacy green) and an LED line
    static void ShopFront(G1Unit u,char f,bool door,float f0,float f1)
    {
        float y=LevelY(u.level),ym=MallCeil(u.level);
        Vector3 Fp(float t,float yy,float o)=>f=='S'?new Vector3(t,yy,u.z0+o):f=='N'?new Vector3(t,yy,u.z1-o):f=='E'?new Vector3(u.x1-o,yy,t):new Vector3(u.x0+o,yy,t);
        float fc=(f0+f1)*.5f,dw=1.0f,e=door?.06f:.16f;
        Vector3 outN=f=='S'?Vector3.back:f=='N'?Vector3.forward:f=='E'?Vector3.right:Vector3.left;
        void FrontBox(Material mat,float t0,float t1,float y0,float y1,float o0,float o1)
        {var a=Fp(t0,y0,o0);var b=Fp(t1,y1,o1);B(mat,Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),y0,y1,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z));}
        void FrontGlass(float t0,float t1,float y0,float y1){var a=Fp(t0,y0,.10f);var b=Fp(t1,y0,.10f);var c=Fp(t1,y1,.10f);var d=Fp(t0,y1,.10f);Q(gShopGlass,a,b,c,d,outN);Q(gShopGlass,a,b,c,d,-outN);}
        var segs=door?new List<(float,float)>{(f0+e,fc-dw),(fc+dw,f1-e)}:new List<(float,float)>{(f0+e,f1-e)};
        foreach(var (a,b) in segs)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt((b-a)/2.4f-.01f));float w=(b-a)/n;
            for(int i=0;i<n;i++)FrontGlass(a+i*w+.012f,a+(i+1)*w-.012f,y+.10f,y+4.0f);
            for(int i=0;i<=n;i++){float t=a+i*w;FrontBox(frame,t-.03f,t+.03f,y+.10f,y+4.0f,.04f,.16f);}
            FrontBox(frame,a-.03f,b+.03f,y+.002f,y+.10f,.04f,.16f);
            var ca=Fp(a,y,.0f);var cb=Fp(b,y+4f,.2f);Col(Mathf.Min(ca.x,cb.x),Mathf.Max(ca.x,cb.x),y,y+4f,Mathf.Min(ca.z,cb.z),Mathf.Max(ca.z,cb.z),"witryna");
        }
        FrontBox(frame,f0+.012f,f1-.012f,y+4.0f,y+4.08f,.04f,.16f);
        FrontBox(frame,f0+e-.05f,f0+e-.02f,y+.002f,y+4.0f,.0f,.16f);FrontBox(frame,f1-e+.02f,f1-e+.05f,y+.002f,y+4.0f,.0f,.16f);   // 1 cm inside the unit's line (no face shared with the linings)
        float yaw=f=='S'?180f:f=='N'?0f:f=='E'?90f:-90f;
        if(door)SlidingGlassDoor("G1 "+u.name+" drzwi",Fp(fc,y,.18f),yaw,dw,2.6f);
        FrontBox(gDark,f0+.012f,f1-.012f,y+4.08f,ym,.0f,.16f);
        var lm=new BraxChampionsMeshBatch();
        float cap=Mathf.Min(.46f,(ym-y-4.08f)*.62f);                                  // large, lit (owner 19:29: każdy szyld czytelny)
        var bp=Fp(fc,y+4.08f+((ym-y-4.08f)-cap)*.5f,-.002f);
        BraxChampionsLetters3D.Text(lm,u.trade=="pharmacy"?Inside(S("g1_pharmacy_green_lit",new Color(.2f,.85f,.4f),.3f,0f,new Color(.25f,1.0f,.45f)),.9f):gLetterFace,gLetterSide,u.name,bp,yaw,cap,.06f,.12f,f1-f0-1.2f);
        BraxChampionsLetters3D.Build(lm,root,"G1 szyld "+u.name+(door?"":" (narożnik)"));Signs++;
        FrontBox(led,f0+.3f,f1-.3f,y+4.10f,y+4.13f,-.02f,.0f);
    }
    // a light café chair for the malls (the station's chair at a quarter of the triangles: rounded pads with one bevel
    // step, 6-sided tubes): bent black steel, cognac leather seat and back; a BraxSeat
    static void G1Chair(Vector3 p,Vector3 face)
    {
        face.y=0;face.Normalize();var rot=Quaternion.LookRotation(-face,Vector3.up);var u=Vector3.Cross(Vector3.up,face).normalized;
        Vector3 P(float a,float h,float d)=>p+u*a+Vector3.up*h-face*d;
        foreach(float a in new[]{-.2f,.2f})
        {
            m.Tube(frame,P(a,0f,-.19f),P(a,.44f,-.17f),.012f,6);
            m.Tube(frame,P(a,0f,.20f),P(a,.44f,.17f),.012f,6);
            m.Tube(frame,P(a,.44f,.17f),P(a,.92f,.22f),.012f,6);
        }
        m.RoundedBox(gChairLeather,P(0,.475f,0f),new Vector3(.46f,.07f,.44f),.02f,rot,1);
        // the back as wide as the seat and 36 cm tall (owner 19:51: the seat was longer than the back)
        m.RoundedBox(gChairLeather,P(0,.735f,.205f),new Vector3(.46f,.36f,.05f),.015f,rot*Quaternion.Euler(8f,0,0),1);
        Seat(P(0,.51f,0).x,P(0,.51f,0).y,P(0,.51f,0).z,face,"krzesło");
        var cc=P(0,.45f,0);Col(cc.x-.25f,cc.x+.25f,p.y,p.y+.9f,cc.z-.25f,cc.z+.25f,"krzesło");
    }

    // a premium portal sign (owner 10.11 / 11.11: no cheap lettering): a graphite carrier with rounded edges projecting
    // 12 cm from the portal head, brushed-steel 3D letters (8 cm deep) of the name and a smaller line of the function, a
    // warm LED line between them (15 mm proud), a downlight profile along the top. c = the head face's centre at the
    // carrier's bottom, n = outward (horizontal), w = width.
    static void PortalSign(string id,Vector3 c,Vector3 n,float w,string main,string sub,float h=.80f)
    {
        n.y=0;n.Normalize();var u=Vector3.Cross(Vector3.up,n).normalized;float yaw=Mathf.Atan2(n.x,n.z)*Mathf.Rad2Deg;
        Vector3 P(float a,float y,float o)=>c+u*a+Vector3.up*y+n*o;
        var a0=P(-w*.5f,0f,0f);var a1=P(w*.5f,h,.12f);
        RB(gDark,Mathf.Min(a0.x,a1.x),Mathf.Max(a0.x,a1.x),a0.y,a1.y,Mathf.Min(a0.z,a1.z),Mathf.Max(a0.z,a1.z),.03f,false,2);
        var l0=P(-w*.5f+.25f,h*.36f,.12f);var l1=P(w*.5f-.25f,h*.36f+.012f,.135f);
        B(ledWarm,Mathf.Min(l0.x,l1.x),Mathf.Max(l0.x,l1.x),l0.y,l1.y,Mathf.Min(l0.z,l1.z),Mathf.Max(l0.z,l1.z));
        var d0=P(-w*.5f+.1f,h,.04f);var d1=P(w*.5f-.1f,h+.05f,.16f);
        RB(frame,Mathf.Min(d0.x,d1.x),Mathf.Max(d0.x,d1.x),d0.y,d1.y,Mathf.Min(d0.z,d1.z),Mathf.Max(d0.z,d1.z),.01f,false,1);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,main,P(0f,h*.44f,.122f),yaw,h*.44f,.08f,.14f,w-.8f);
        if(!string.IsNullOrEmpty(sub))BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,sub,P(0f,h*.10f,.122f),yaw,h*.16f,.03f,.22f,w-1.2f);
        BraxChampionsLetters3D.Build(lm,root,"G1 szyld "+id);Signs++;
    }

    // ---- BRAXHORN MARKET (P0, east wing north: x 47..85, z 10..55), entered from the north mall (x 47, z 28..40: an open
    // front under a black portal with the 3D name) and from the street (the north entrance at x 66). Everything from the
    // owner's library (pack miasto_sklepy, the same as the city's SUPERMARKET BRAXHORN): five lines of island shelving
    // (regal_sklep) with the goods of the nine aisles on every shelf, the fridges (lodowka) on the south wall, an open
    // chiller for MEAT · COLD CUTS · CHEESE (built, white; the meat, sausages, cheese and salmon from the pack), the
    // checkouts (kasa) at both entries, the trolleys (wozek), fruit and vegetables on oak tables by the east windows.
    // Without the pack: the built shelving, checkouts and trolleys stay - empty, never boxes for goods.
    static void G1Anchor()
    {
        float x0=G1EastX0,x1=G1HW-.3f,z0=10f,z1=G1HD-.26f,y=G1F0,yc=G1Ceil0;
        B(gMarketFloor,x0+.15f,x1,WalkY,y,z0+.15f,z1,true);
        B(gWallWarm,x0,x1,y,G1Slab,z0,z0+.15f,true);                                    // the south wall (to the furniture store)
        B(gWallWarm,x0,x0+.15f,y,G1Slab,z0+.15f,G1NorthMallZ0,true);B(gWallWarm,x0,x0+.15f,y,G1Slab,G1NorthMallZ1,z1,true);
        B(gDark,x0-.02f,x0+.15f,4.2f,G1Slab,G1NorthMallZ0,G1NorthMallZ1);               // the portal's head over the mall front
        PortalSign("BRAXHORN MARKET",new Vector3(x0-.02f,3.92f,(G1NorthMallZ0+G1NorthMallZ1)*.5f),Vector3.left,11.4f,"BRAXHORN MARKET","SUPERMARKET • ŚWIEŻE • CODZIENNIE",1.0f);
        SlatCeiling(x0+.15f,x1,z0+.15f,z1,yc,null);                             // timber slats on black felt (owner 18:28: no white)
        bool pack=BraxMiastoAssety.Jest("miasto_sklepy");
        var wyp=Group(root,"G1 BRAXHORN MARKET | wyposażenie (paczka miasto_sklepy)");
        // ---- island shelving: lines x 60 .. 73.3 (every 3.33; the column lines x 60 / 70 between the islands), five 4.6 m
        // islands per line between the column rows (z 15 / 25 / 35); the pack's segments and shelf levels (BraxSupermarket)
        var regal=pack?BraxMiastoAssety.Model("miasto_sklepy","regal_sklep"):null;
        float[] islands={17.65f,22.35f,27.65f,32.35f,37.65f};
        (float z0,float z1,float[] ys)[] seg={(-2.30f,-1.20f,new[]{.21f,.47f,.75f,1.03f,1.31f}),(-.55f,.55f,new[]{.21f,.57f,.91f,1.25f}),(1.20f,2.30f,new[]{.21f,.67f,1.15f})};
        int line=0;
        for(int k=0;k<5;k++)
        {
            float x=60f+k*10f/3f;
            for(int i=0;i<islands.Length;i++,line++)
            {
                float zc=islands[i];
                if(regal)
                {
                    var o=BraxMiastoAssety.Postaw(wyp,regal,"Regał-wyspa "+(k+1)+"."+(i+1),Wld(new Vector3(x,y,zc)),0f,1f,true,true);
                    if(!o)continue;
                    BraxMiastoAssety.LodyZWezlow(o,new[]{.22f,.05f});BraxMiastoAssety.Kolizja(cols,o,"kolizja: regał-wyspa",.03f);
                    var tw=BraxSklepyTowar.Korzen(wyp,"BRAXHORN MARKET regał "+(k+1)+"."+(i+1));
                    string typ="sm_g"+(line%9);
                    foreach(float sg in new[]{-1f,1f})foreach(var sgm in seg)for(int l=0;l<sgm.ys.Length;l++)
                    {
                        float free=l+1<sgm.ys.Length?sgm.ys[l+1]-sgm.ys[l]-.03f:.45f;
                        LibShelf(tw,BraxSklepyTowar.Ma(typ)?typ:"market",new Vector3(x+sg*.22f,y+sgm.ys[l]+.004f,zc+sgm.z0+.03f),Vector3.forward,Vector3.right*-sg,sgm.z1-sgm.z0-.06f,free);
                    }
                    BraxSklepyTowar.Zakoncz(tw,"BRAXHORN MARKET regał "+(k+1)+"."+(i+1));
                }
                else
                {
                    // built island (owner 19:49: no grey blocks): black steel plinth, back panel and ends, oak shelves with
                    // chrome price rails, an LED line under each shelf - empty until the pack's goods
                    if(!wcTile)WcMaterials();
                    F(blackMatt,x-.45f,x+.45f,y,y+.12f,zc-2.3f,zc+2.3f,true);F(blackMatt,x-.03f,x+.03f,y+.12f,y+1.75f,zc-2.3f,zc+2.3f);
                    for(int l=0;l<5;l++){float yy=y+.40f+l*.30f;F(gOak,x-.42f,x+.42f,yy-.025f,yy,zc-2.28f,zc+2.28f);foreach(float sg in new[]{-1f,1f})F(chrome,Mathf.Min(x+sg*.42f,x+sg*.44f),Mathf.Max(x+sg*.42f,x+sg*.44f),yy-.05f,yy-.005f,zc-2.28f,zc+2.28f);}
                    foreach(float e in new[]{-1f,1f})F(blackMatt,x-.45f,x+.45f,y,y+1.75f,Mathf.Min(zc+e*2.3f,zc+e*2.27f),Mathf.Max(zc+e*2.3f,zc+e*2.27f));
                }
                if(i==4)F(signBlue,x-.25f,x+.25f,y+2.25f,y+2.55f,zc+2.25f,zc+2.30f);   // the aisle number plate
            }
        }
        // ---- the open chiller on the south wall (x 54..66): MIĘSO · WĘDLINY · SERY; the meat from the pack
        {
            float a=54f,b=66f,zw=z0+.15f,dC=.95f,y1=y+.45f,y2=y+1.0f,y3=y+1.45f,yK=y+2.05f;
            var ch=Inside(S("g1_market_chiller_white",new Color(.93f,.94f,.95f),.35f,.3f),.9f);
            F(ch,a,b,y,y1,zw,zw+dC,true);F(ch,a,b,y1,yK,zw,zw+.08f);
            F(ch,a+.02f,b-.02f,y2-.03f,y2,zw+.08f,zw+.63f);F(ch,a+.02f,b-.02f,y3-.03f,y3,zw+.08f,zw+.50f);
            F(ch,a,b,yK,yK+.15f,zw,zw+.6f);F(ch,a-.04f,a,y,yK+.15f,zw,zw+dC,true);F(ch,b,b+.04f,y,yK+.15f,zw,zw+dC,true);
            B(led,a+.05f,b-.05f,yK-.03f,yK,zw+.50f,zw+.58f);
            var lmc=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lmc,Inside(S("g1_market_meat_red",new Color(.55f,.12f,.10f),.5f,0f),.9f),frame,"MIĘSO • WĘDLINY • SERY",new Vector3((a+b)*.5f,yK+.35f,zw+.6f+.002f),0f,.30f,.03f,.10f,b-a-1f);
            BraxChampionsLetters3D.Build(lmc,root,"G1 market napis chłodni");Signs++;
            B(gCinemaDark,a,b,yK+.15f,yK+.75f,zw+.55f,zw+.6f);
            var tw=BraxSklepyTowar.Korzen(wyp,"BRAXHORN MARKET chłodnia");
            LibShelf(tw,"ch_dol",new Vector3(a+.06f,y1+.004f,zw+dC*.5f),Vector3.right,Vector3.back,b-a-.12f,.5f);
            LibShelf(tw,"ch_srodek",new Vector3(a+.06f,y2+.004f,zw+.355f),Vector3.right,Vector3.back,b-a-.12f,.4f);
            LibShelf(tw,"ch_gora",new Vector3(a+.06f,y3+.004f,zw+.29f),Vector3.right,Vector3.back,b-a-.12f,.5f);
            BraxSklepyTowar.Zakoncz(tw,"BRAXHORN MARKET chłodnia");
        }
        // ---- the fridges (pack lodowka, glass doors, lit) on the rest of the south wall (x 67..84), fronts to the store
        var lod=pack?BraxMiastoAssety.Model("miasto_sklepy","lodowka"):null;
        if(lod)
        {
            int n=0;for(float x=67.0f+.44f;x<84.6f-.44f;x+=.874f)BraxMiastoAssety.Postaw(wyp,lod,"Lodówka "+(++n),Wld(new Vector3(x,y,z0+.15f+.50f)),180f,1f,true,true);
            Col(67f,84.6f,y,y+2.2f,z0+.15f,z0+1.15f,"lodówki");
        }
        // ---- the checkouts (pack kasa; built ones without the pack) at the mall entry and the street entry
        var kasa=pack?BraxMiastoAssety.Model("miasto_sklepy","kasa"):null;
        foreach(var (cx,cz,yaw) in new[]{(53.8f,13.5f,90f),(53.8f,17.9f,90f),(53.8f,22.3f,90f),(53.5f,45.8f,180f),(57.9f,45.8f,180f),(73.0f,45.8f,180f),(77.4f,45.8f,180f)})
        {
            if(kasa){var o=BraxMiastoAssety.Postaw(wyp,kasa,"Kasa",Wld(new Vector3(cx,y,cz)),yaw,1f,true,true);if(o){BraxMiastoAssety.LodyZWezlow(o,new[]{.2f,.05f});BraxMiastoAssety.Kolizja(cols,o,"kolizja: kasa",.1f);}}
            else foreach(float s in new[]{-1f,1f})Checkout(new Vector3(cx,y,cz)+(yaw==90f?Vector3.forward:Vector3.right)*s*1.1f,yaw==90f?90f:0f);
        }
        // ---- fruit and vegetables on oak tables by the east windows (pack owoce_skrzynka, Poly Haven bananas)
        foreach(float za in new[]{16f,23.5f,31f,38.5f})
        {
            float xa=80.6f,xb=83.6f,h=.80f,zb=za+5f;
            F(oakSlat,xa,xb,y+h-.05f,y+h,za,zb);
            foreach(var (dx,dz) in new[]{(0,0),(1,0),(0,1),(1,1)})F(oakSlat,dx==0?xa+.1f:xb-.16f,dx==0?xa+.16f:xb-.1f,y,y+h-.05f,dz==0?za+.1f:zb-.16f,dz==0?za+.16f:zb-.1f);
            Col(xa,xb,y,y+h,za,zb,"stół warzyw");
            var tw=BraxSklepyTowar.Korzen(wyp,"BRAXHORN MARKET warzywa "+za);
            LibShelf(tw,"warzywniak",new Vector3(xa+.05f,y+h+.004f,(za+zb)*.5f),Vector3.forward,Vector3.right,zb-za-.1f,.6f);
            BraxSklepyTowar.Zakoncz(tw,"BRAXHORN MARKET warzywa "+za);
        }
        // ---- trolleys (pack wozek; built ones without the pack) at both entries
        if(pack){BraxSklepyTowar.Wozki(wyp,cols,Wld(new Vector3(48.6f,y,41.5f)),Vector3.forward,8);BraxSklepyTowar.Wozki(wyp,cols,Wld(new Vector3(56.2f,y,50.2f)),Vector3.right,8);}
        else{TrolleyRow(new Vector3(48.6f,y,41.5f),Vector3.forward,9);TrolleyRow(new Vector3(56.2f,y,50.2f),Vector3.right,9);}
        G1Shops++;
    }
    // a checkout: the desk with the conveyor belt (black rubber), the scanner, the cashier's seat, a POS terminal with a lit
    // screen, a card reader, the lane number light on a black pole; c = desk centre, yaw 0: lane along z, customers +z -> -z
    static void Checkout(Vector3 c,float yaw)
    {
        var r=Quaternion.Euler(0,yaw,0);
        void Bx(Material mat,float a0,float a1,float y0,float y1,float b0,float b1,bool col=false)
        {
            var p=c+r*new Vector3((a0+a1)*.5f,0,(b0+b1)*.5f);float th=Mathf.Min(a1-a0,Mathf.Min(y1-y0,b1-b0));
            m.RoundedBox(mat,new Vector3(p.x,c.y+(y0+y1)*.5f,p.z),new Vector3(a1-a0,y1-y0,b1-b0),Mathf.Clamp(th*.3f,.003f,.03f),r,th>.1f?2:1);
            if(col){BraxChampionsMeshBatch.Collider(cols,"kasa",new Vector3(p.x,c.y+(y0+y1)*.5f,p.z),new Vector3(a1-a0,y1-y0,b1-b0),yaw);Colliders++;}
        }
        Bx(gOak,-.30f,.30f,0f,.85f,-1.8f,1.8f,true);
        Bx(gCinemaDark,-.31f,.31f,.85f,.88f,-1.81f,1.81f);
        Bx(rubber,-.24f,.24f,.88f,.90f,.1f,1.7f);                                        // the belt
        Bx(gCinemaDark,-.20f,.20f,.88f,.93f,-.45f,-.05f);Bx(glassDark,-.16f,.16f,.93f,.935f,-.40f,-.10f);   // scanner
        Bx(gCinemaDark,.30f,.36f,.85f,1.25f,-.25f,.15f);Bx(screenLit(),.36f,.365f,.95f,1.20f,-.22f,.12f);   // POS screen (to the cashier)
        Bx(gCinemaDark,-.36f,-.30f,.88f,1.08f,-.80f,-.62f);                              // card reader (to the customer)
        Bx(leatherNavy,.55f,1.0f,.50f,.62f,-.35f,.10f);Bx(frame,.74f,.80f,0f,.50f,-.15f,-.09f);   // cashier's seat
        m.Cylinder(frame,c+r*new Vector3(-.26f,0,-1.7f),.025f,2.0f,8,Quaternion.identity,true);
        Bx(led,-.36f,-.16f,2.0f,2.2f,-1.75f,-1.65f);                                     // the lane light
    }
    // nested trolleys (stainless wire baskets, black handles, four castors), n in a row from p along dir
    static void TrolleyRow(Vector3 p,Vector3 dir,int n)
    {
        var side=new Vector3(dir.z,0,-dir.x);
        for(int i=0;i<n;i++)
        {
            var c=p+dir*(i*.32f);
            for(int k=0;k<4;k++){var w=c+dir*((k<2?0f:.75f))+side*(k%2==0?-.24f:.24f);m.Cylinder(rubber,w+Vector3.up*.06f,.06f,.03f,10,Quaternion.Euler(0,0,90)*Quaternion.LookRotation(dir),true);}
            Beam(stainless,c+Vector3.up*.14f-side*.24f,c+dir*.75f+Vector3.up*.14f-side*.24f,.02f,.02f);
            Beam(stainless,c+Vector3.up*.14f+side*.24f,c+dir*.75f+Vector3.up*.14f+side*.24f,.02f,.02f);
            foreach(float h in new[]{.45f,.95f})
            {
                var a=c+Vector3.up*h-side*.26f;var b=c+Vector3.up*h+side*.26f;var e=dir*.80f;
                Beam(stainless,a,b,.012f,.012f);Beam(stainless,a+e,b+e,.012f,.012f);Beam(stainless,a,a+e,.012f,.012f);Beam(stainless,b,b+e,.012f,.012f);
            }
            for(float t=.1f;t<.8f;t+=.1f){var a=c+dir*t-side*.26f;var b=c+dir*t+side*.26f;Beam(stainless,a+Vector3.up*.45f,a+Vector3.up*.95f,.008f,.008f);Beam(stainless,b+Vector3.up*.45f,b+Vector3.up*.95f,.008f,.008f);}
            Beam(stainless,c-side*.24f+Vector3.up*.14f,c-side*.26f+Vector3.up*.95f,.02f,.02f);Beam(stainless,c+side*.24f+Vector3.up*.14f,c+side*.26f+Vector3.up*.95f,.02f,.02f);
            Beam(gRed,c-dir*.06f-side*.26f+Vector3.up*1.02f,c-dir*.06f+side*.26f+Vector3.up*1.02f,.035f,.035f);   // the handle (red)
        }
        var e0=p-side*.3f;var e1=p+dir*(n*.32f+.6f)+side*.3f;
        Col(Mathf.Min(e0.x,e1.x),Mathf.Max(e0.x,e1.x),p.y,p.y+1.05f,Mathf.Min(e0.z,e1.z),Mathf.Max(e0.z,e1.z),"wózki sklepowe");
    }

    // ---- SPORT ARENA (P1, the whole east wing): clothes rails in rows (empty until the clothes models), the shoe wall on
    // the west wall (empty), BRAXHORN NUTRITION - a corner of the owner's supplements (pack: whey, gainers, creatine) on
    // walnut shelving, treadmills and benches from the owner's gym pack (miasto_silownia_metro) by the north windows,
    // cash desks south of the mall entrance (x 47, z 28..40)
    static void G1SportStore()
    {
        float x0=G1EastX0,x1=G1HW-.3f,z0=-G1HD+.3f,z1=G1HD-.26f,y=G1F1;
        B(gMallBand,x0+.15f,x1,G1F1-.12f,y,z0,z1,true);
        B(ShopWall("sport"),x0,x0+.15f,y,G1Roof-.9f,z0,G1NorthMallZ0,true);B(ShopWall("sport"),x0,x0+.15f,y,G1Roof-.9f,G1NorthMallZ1,z1,true);
        B(gDark,x0-.02f,x0+.15f,y+3.9f,G1Roof-.9f,G1NorthMallZ0,G1NorthMallZ1);
        B(ShopWall("sport"),x0+.15f,x1,y,G1Roof-.9f,z0,z0+.08f,true);
        PortalSign("SPORT ARENA",new Vector3(x0-.02f,y+3.62f,(G1NorthMallZ0+G1NorthMallZ1)*.5f),Vector3.left,11.4f,"SPORT ARENA","ODZIEŻ • OBUWIE • NUTRITION",1.0f);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,Inside(S("g1_nutrition_green",new Color(.2f,.85f,.4f),.3f,0f,new Color(.25f,1.0f,.45f)),.9f),frame,"BRAXHORN NUTRITION",new Vector3(66f,y+3.1f,z0+.082f),0f,.32f,.03f,.10f,10f);
        BraxChampionsLetters3D.Build(lm,root,"G1 szyld BRAXHORN NUTRITION");Signs++;
        SlatCeiling(x0+.15f,x1,z0+.08f,z1,G1Ceil1,null);
        // clothes rails every 8 m, all of them full (owner 21:21: "co to za czarne słupy" - the empty rails); between them
        // oak display tables with folded stacks, balls and sneakers; a dumbbell rack and training mats by the gym corner
        BraxKatalog.ClothesMaterials();
        for(float z=-40f;z<42f;z+=8f)for(float x=55f;x<80f;x+=8f)
        {
            var a=new Vector3(x,y,z);var b=new Vector3(x+5f,y,z);
            m.Tube(frame,a,a+Vector3.up*1.55f,.018f,8);m.Tube(frame,b,b+Vector3.up*1.55f,.018f,8);m.Tube(frame,a+Vector3.up*1.55f,b+Vector3.up*1.55f,.016f,8);
            m.Tube(frame,a+Vector3.back*.28f,a+Vector3.forward*.28f,.015f,6);m.Tube(frame,b+Vector3.back*.28f,b+Vector3.forward*.28f,.015f,6);
            Col(x-.05f,x+5.05f,y,y+1.6f,z-.28f,z+.28f,"wieszak");
        }
        for(float z=-36f;z<40f;z+=8f)for(float x=56f;x<80f;x+=8f)
        {
            F(gOak,x,x+3f,y+.80f,y+.85f,z-.5f,z+.5f);
            foreach(float xl in new[]{x+.15f,x+2.79f})F(frame,xl,xl+.06f,y,y+.80f,z-.4f,z+.4f);
            Col(x,x+3f,y,y+.85f,z-.5f,z+.5f,"stół ekspozycyjny");
        }
        {
            int row=0;
            for(float z=-40f;z<42f;z+=8f,row++)
            {
                var zz=z;var rr=row;
                Goods("SPORT ARENA | ubrania "+rr,()=>
                {
                    int[] kinds={5,6,0,3,5,2};
                    for(float x=55f;x<80f;x+=8f)
                    {
                        int n=Mathf.FloorToInt(5f/.22f);
                        for(int i=0;i<n;i++){int kind=kinds[((i/6)+rr+(int)x)%kinds.Length];var mat=kind==3?BraxKatalog.Trousers[(i+rr)%2==0?0:3]:kind==2?BraxKatalog.Coats[(i+rr)%4]:BraxKatalog.SportCols[(i/6+rr)%4];
                            var hk=new Vector3(x+(i+.5f)*.22f,y+1.55f,zz);if(!NearColumn(hk,.75f)&&!ModelGarment(hk,Vector3.right,kind,i+rr))BraxKatalog.Garment(Kat(),hk,Vector3.right,kind,mat);}
                    }
                    if(zz+4f<40f)for(float x=56f;x<80f;x+=8f)
                    {
                        for(int i=0;i<4;i++)BraxKatalog.FoldedStack(Kat(),new Vector3(x+.4f+i*.45f,y+.85f,zz+4f-.2f),Vector3.back,BraxKatalog.SportCols[(i+rr)%4],5+i%3);
                        for(int i=0;i<3;i++)BraxKatalog.Ball(Kat(),new Vector3(x+.6f+i*.7f,y+.85f,zz+4f+.25f),(i+rr)%3);
                        if(!ShopModel(ButyPaczka,Buty[(rr+(int)x)%5],new Vector3(x+2.7f,y+.85f,zz+4f+.1f),Vector3.back))foreach(float xs in new[]{x+2.62f,x+2.80f})BraxKatalog.Sneaker(Kat(),new Vector3(xs,y+.85f,zz+4f+.1f),Vector3.back,BraxKatalog.SportCols[rr%4]);
                    }
                });
            }
            Goods("SPORT ARENA | hantle i maty",()=>
            {
                float rx=70f,rz=44.2f;
                foreach(float h in new[]{.35f,.75f})B(frame,rx,rx+4f,y+h-.03f,y+h,rz-.25f,rz+.25f);
                foreach(float x in new[]{rx,rx+2f,rx+4f-.05f})foreach(float zq in new[]{rz-.25f,rz+.2f})B(frame,x,x+.05f,y,y+.80f,zq,zq+.05f);
                for(int i=0;i<10;i++)BraxKatalog.Dumbbell(Kat(),new Vector3(rx+.2f+i*.38f,y+(i<5?.35f:.75f)+.049f+(4f+i*2f)*.0028f,rz),Vector3.forward,4f+i*2f);   // resting on the head
                Material[] mats={BraxKatalog.SportCols[0],BraxKatalog.SportCols[1],BraxKatalog.SportCols[3]};
                for(int i=0;i<9;i++)BraxKatalog.MatRoll(Kat(),new Vector3(65f+(i%3)*.16f,y,44.0f+(i/3)*.16f),Vector3.up,mats[i%3]);
            });
            Col(70f,74f,y,y+.8f,43.9f,44.5f,"stojak na hantle");
        }
        // the shoe wall on the west wall (x 47.15..48.2, z -50..26): marble shelves, walnut back, try-on benches
        for(int r=0;r<6;r++){float yy=y+.4f+r*.45f;F(StoneTop(),47.2f,48.2f,yy,yy+.03f,-50f,26f);}
        F(gWood,47.15f,47.22f,y,y+3.2f,-50f,26f);
        for(int i=0;i<3;i++)StationBench(new Vector3(50.5f,y,-40f+i*20f),Vector3.left,3,"ławka przymierzania obuwia",0,true);
        Goods("SPORT ARENA | ściana butów",()=>{for(int r=1;r<5;r++){float yy=y+.4f+r*.45f+.03f;int i=r;for(float z=-49.5f;z<25.6f;z+=1.0f,i++){if(ShopModel(ButyPaczka,Buty[i%5],new Vector3(47.75f,yy,z),Vector3.right))continue;BraxKatalog.Sneaker(Kat(),new Vector3(47.75f,yy,z-.07f),Vector3.right,BraxKatalog.SportCols[i%4]);BraxKatalog.Sneaker(Kat(),new Vector3(47.75f,yy,z+.07f),Vector3.right,BraxKatalog.SportCols[i%4]);}}});
        // BRAXHORN NUTRITION on the south wall (x 56..76): walnut shelving, the owner's supplements on every shelf
        {
            float a=56f,b=76f,zw=z0+.08f,dep=.45f;
            F(walnut,a,b,y,y+.12f,zw,zw+dep,true);F(walnut,a,b,y+.12f,y+2.4f,zw,zw+.04f);F(walnut,a-.04f,a,y,y+2.4f,zw,zw+dep,true);F(walnut,b,b+.04f,y,y+2.4f,zw,zw+dep,true);
            var tw=BraxSklepyTowar.Korzen(root,"G1 SPORT ARENA BRAXHORN NUTRITION");
            string[] typy={"odz_dol","odz_polka","odz_polka","odz_gora","odz_gora"};
            for(int l=0;l<5;l++){float yy=y+.12f+l*.45f;if(l>0)F(walnut,a,b,yy-.025f,yy,zw+.04f,zw+dep);LibShelf(tw,typy[l],new Vector3(a+.05f,yy+.004f,zw+.25f),Vector3.right,Vector3.back,b-a-.1f,l==0?.45f:.40f);}
            B(ledWarm,a+.05f,b-.05f,y+2.35f,y+2.38f,zw+.30f,zw+.40f);
            BraxSklepyTowar.Zakoncz(tw,"G1 SPORT ARENA BRAXHORN NUTRITION");
        }
        // cash desks south of the entrance
        for(int i=0;i<3;i++){float z=18f+i*3.2f;F(walnut,49.5f,52f,y,y+1.0f,z,z+1.6f,true);F(StoneTop(),49.45f,52.05f,y+1.0f,y+1.04f,z-.05f,z+1.65f);var p=new Vector3(50.75f,y+1.04f,z+.8f);Goods("SPORT ARENA kasa "+i,()=>Monitor(p,Vector3.left));}
        // treadmills and benches from the owner's gym pack by the north windows
        var biez=BraxMiastoAssety.Jest("miasto_silownia_metro")?BraxMiastoAssety.Model("miasto_silownia_metro","silownia_bieznia"):null;
        var law=BraxMiastoAssety.Jest("miasto_silownia_metro")?BraxMiastoAssety.Model("miasto_silownia_metro","silownia_lawka"):null;
        var gym=Group(root,"G1 SPORT ARENA | bieżnie i ławki (paczka miasto_silownia_metro)");
        for(int i=0;i<5;i++)
        {
            var at=Wld(new Vector3(58f+i*4.5f,y,48.5f));
            if(biez){var o=BraxMiastoAssety.Postaw(gym,biez,"Bieżnia "+(i+1),at,180f,1f,true,true);if(o)BraxMiastoAssety.Kolizja(cols,o,"kolizja: bieżnia",.05f);}
        }
        for(int i=0;i<2;i++){var at=Wld(new Vector3(81f,y,44f+i*4f));if(law){var o=BraxMiastoAssety.Postaw(gym,law,"Ławka treningowa "+(i+1),at,90f,1f,true,true);if(o)BraxMiastoAssety.Kolizja(cols,o,"kolizja: ławka",.05f);}}
        G1Shops++;
    }

    // ---- the food court (x -30..30, z -55..-25, double height to the roof under larch slats): 8 counters on the side walls
    // (x +-30) with lit menu screens, a stainless kitchen line, the name on a navy fascia; 44 tables with 4 chairs each in
    // the column bays (a clear aisle from the south entrance), planters, bins
    static void G1FoodCourt()
    {
        float y=G1F0;
        B(gMallFloor2,-G1FoodX,G1FoodX,WalkY,y,-G1HD+.3f,G1SouthMallZ0,true);
        string[] names={"PIZZA NAPOLI","SUSHI ZEN","BURGER BAR","KEBAB GRILL","PIEROGI U BABCI","THAI WOK","SAŁATKI FRESH","CAFÉ DOLCE"};
        for(int i=0;i<8;i++)
        {
            float sx=i<4?-1f:1f;int k=i%4;float z0=-52f+k*6.5f,z1=z0+6.0f,xb=sx*(G1FoodX-.3f),xf=sx*(G1FoodX-4.0f);
            F(gWood,Mathf.Min(xb,xb-sx*.10f),Mathf.Max(xb,xb-sx*.10f),y,y+5.2f,z0,z1,true);
            for(int j=0;j<3;j++)
            {
                float zc=z0+1.0f+j*2.0f;F(gCinemaDark,Mathf.Min(xb-sx*.10f,xb-sx*.14f),Mathf.Max(xb-sx*.10f,xb-sx*.14f),y+2.4f,y+3.3f,zc-.85f,zc+.85f);
                var px=new Color32[BraxChampionsKinoEkrany.ML*BraxChampionsKinoEkrany.MH];BraxChampionsKinoEkrany.MenuLokalu(px,i,j);   // owner 19:49: no empty blue screens
                ScreenPanel(ScreenMaterial("g1_menu_"+i+"_"+j,BraxChampionsKinoEkrany.ML,BraxChampionsKinoEkrany.MH,px),new Vector3(xb-sx*.156f,y+2.85f,zc),new Vector3(-sx,0,0),1.60f,.80f,.015f);
            }
            F(walnut,Mathf.Min(xf,xf+sx*.7f),Mathf.Max(xf,xf+sx*.7f),y,y+1.05f,z0+.3f,z1-.3f,true);
            F(StoneTop(),Mathf.Min(xf-sx*.05f,xf+sx*.75f),Mathf.Max(xf-sx*.05f,xf+sx*.75f),y+1.05f,y+1.09f,z0+.25f,z1-.25f);
            B(led,Mathf.Min(xf-sx*.012f,xf),Mathf.Max(xf-sx*.012f,xf),y+.12f,y+.15f,z0+.4f,z1-.4f);
            F(stainless,Mathf.Min(xb-sx*.10f,xb-sx*.75f),Mathf.Max(xb-sx*.10f,xb-sx*.75f),y,y+.92f,z0+.3f,z1-.3f,true);
            F(signBlue,Mathf.Min(xf,xf+sx*.10f),Mathf.Max(xf,xf+sx*.10f),y+3.6f,y+4.4f,z0+.2f,z1-.2f);
            var lm=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,names[i],new Vector3(xf-sx*.002f,y+3.78f,(z0+z1)*.5f),sx<0?90f:-90f,.32f,.03f,.10f,z1-z0-.8f);
            BraxChampionsLetters3D.Build(lm,root,"G1 food court "+names[i]);Signs++;
            F(gWood,Mathf.Min(xb,xf),Mathf.Max(xb,xf),y,y+3.4f,z1,z1+.12f,true);
            // the food on the counter from the library: pizza / kebab, shawarma / burgers, fries (pack miasto_sklepy), fruit
            // in wooden bowls and cakes (the region's Poly Haven originals); the others' counters stay clear until their models
            if(i==3)BraxChampionsPromenadeEquipment.KebabSpit(root,m,new Vector3(xb-sx*.42f,y+.92f,(z0+z1)*.5f-1.6f));   // the turning doner spit on the kitchen line (owner 20:03)
            string lada=i==0?"pizzeria":i==2?"bistro":i==3?"bar":null;
            if(lada!=null)
            {
                var tw=BraxSklepyTowar.Korzen(g1Goods,"G1 food court "+names[i]);
                LibCounter(tw,lada,new Vector3(xf+sx*.375f,y+1.094f,z0+.45f),Vector3.forward,Vector3.right*sx,z1-z0-.9f);
                BraxSklepyTowar.Zakoncz(tw,"G1 food court "+names[i]);
            }
            else if(i==6){string[] f={"wooden_bowl_02","lemon","food_lime_01","food_apple_01","food_kiwi_01","food_pomegranate_01"};for(int j=0;j<f.Length;j++)BraxChampionsPromenadeFurniture.Flower(g1Goods,f[j],new Vector3(xf+sx*.375f,y+1.09f,z0+.7f+j*.85f),f[j]=="wooden_bowl_02"?.30f:.09f,j*40f);}
            else if(i==7){string[] f={"carrot_cake","croissant","strawberry_chocolate_cake","croissant","tea_set_01"};for(int j=0;j<f.Length;j++)BraxChampionsPromenadeFurniture.Flower(g1Goods,f[j],new Vector3(xf+sx*.375f,y+1.09f,z0+.8f+j*1.0f),f[j]=="croissant"?.14f:f[j]=="tea_set_01"?.40f:.26f,j*30f);}
            F(gWood,Mathf.Min(xb,xf),Mathf.Max(xb,xf),y+3.4f,y+3.52f,z0,z1+.12f);         // the counter's canopy
            Q(gWood,new Vector3(Mathf.Min(xb,xf),y+3.398f,z0),new Vector3(Mathf.Max(xb,xf),y+3.398f,z0),new Vector3(Mathf.Max(xb,xf),y+3.398f,z1),new Vector3(Mathf.Min(xb,xf),y+3.398f,z1),Vector3.down);
            for(int j=0;j<3;j++)m.Cylinder(ledWarm,new Vector3((xb+xf)*.5f,y+3.39f,z0+1f+j*2f),.08f,.008f,16,Quaternion.Euler(180,0,0),true,false);
        }
        // seating in the column bays (centres x +-5, +-15; z -50, -40, -30), four tables per bay
        int t=0;
        foreach(float bx in new[]{-15f,-5f,5f,15f})foreach(float bz in new[]{-50f,-40f,-30f})foreach(float dx in new[]{-2.5f,2.5f})foreach(float dz in new[]{-2.5f,2.5f})
        {
            float x=bx+dx,z=bz+dz;
            if(Mathf.Abs(x)<3f&&z<-46f)continue;                                          // the south entrance's windbreak
            CafeTable(x,y,z);t++;
            for(int k=0;k<4;k++){float a=k*90f+45f;var d=new Vector3(Mathf.Sin(a*Mathf.Deg2Rad),0,Mathf.Cos(a*Mathf.Deg2Rad));G1Chair(new Vector3(x,y,z)+d*.78f,-d);}
        }
        foreach(float x in new[]{-24f,24f})foreach(float z in new[]{-27f,-53.5f})Prop(root,"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(x,y,z),x*7f,.3f);
        foreach(float x in new[]{-10f,10f})foreach(float z in new[]{-26.2f,-44.5f})Bin(x+.9f,y,z);
        // the double-height ceiling under the roof: larch slats on black felt, open over the vault's south end
        SlatCeiling(-G1FoodX+.02f,G1FoodX-.02f,-G1HD+.3f,G1SouthMallZ0,G1Roof-.92f,new List<(float,float,float,float)>{(-G1VaultR,G1VaultR,G1VaultZ0,G1SouthMallZ0)});
    }

    // ---- services: the toilets (SW block x -47..-30 on both levels: lobby from the mall with the accessible WC and the baby
    // care room, the men's and women's rooms with cubicles, urinals, basins on marble counters, mirrors), the P0 service
    // corridor behind the SE unit to the goods-in in the east wing (staff door to the food court's kitchens)
    static void G1Services()
    {
        G1Toilets(0);G1Toilets(1);
        float y=G1F0;
        B(gMallBand,G1FoodX+.15f,G1EastX0,WalkY,y,-G1HD+.3f,G1ServiceZ1,true);
        B(gWallWarm,G1FoodX,G1FoodX+.15f,y,G1Slab,-G1HD+.3f,-53.8f,true);B(gWallWarm,G1FoodX,G1FoodX+.15f,y,G1Slab,-52.8f,G1ServiceZ1,true);
        B(gWallWarm,G1FoodX,G1FoodX+.15f,y+2.2f,G1Slab,-53.8f,-52.8f);
        B(gWallWarm,G1FoodX+.15f,G1EastX0,y,G1Slab,-G1HD+.3f,-G1HD+.38f,true);       // the lining on the south façade
        Door("G1_service_fc","G1 zaplecze food court (personel)",G1FoodX+.075f,y,-53.3f,90f,1.0f,2.2f,false);
        Q(ceilWhite,new Vector3(G1FoodX+.15f,y+3.0f,-G1HD+.38f),new Vector3(G1EastX0,y+3.0f,-G1HD+.38f),new Vector3(G1EastX0,y+3.0f,G1ServiceZ1),new Vector3(G1FoodX+.15f,y+3.0f,G1ServiceZ1),Vector3.down);
        for(float x=G1FoodX+2f;x<G1EastX0;x+=3f)B(led,x-.3f,x+.3f,y+2.97f,y+3.0f,-53.35f,-53.25f);
    }
    // sanitary ware (real shapes, glazed porcelain): a wall-hung WC (rounded pan, seat and lid, the stainless flush plate on
    // the wall), a urinal, a counter basin (a turned bowl sitting on the top)
    static void WcPan(Vector3 wall,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);var y=wall.y;
        m.RoundedBox(porcelainMat(),wall+f*.30f+Vector3.up*(y*0f+.30f),new Vector3(.37f,.30f,.55f),.14f,rot,3);
        m.RoundedBox(porcelainMat(),wall+f*.31f+Vector3.up*.465f,new Vector3(.38f,.03f,.47f),.012f,rot,2);
        m.RoundedBox(porcelainMat(),wall+f*.06f+Vector3.up*.30f,new Vector3(.40f,.42f,.12f),.04f,rot,2);
        m.RoundedBox(stainless,wall+f*.012f+Vector3.up*1.05f,new Vector3(.25f,.16f,.024f),.01f,rot,1);
        var c0=wall+f*.04f;var c1=wall+f*.58f;Col(Mathf.Min(c0.x,c1.x)-.2f,Mathf.Max(c0.x,c1.x)+.2f,y,y+.48f,Mathf.Min(c0.z,c1.z)-.2f,Mathf.Max(c0.z,c1.z)+.2f,"WC");
    }
    static void Urinal(Vector3 wall,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(porcelainMat(),wall+f*.17f+Vector3.up*.82f,new Vector3(.38f,.70f,.34f),.13f,rot,3);
        m.Cylinder(stainless,wall+f*.05f+Vector3.up*1.22f,.018f,.12f,10,Quaternion.identity,true);
    }
    static void Basin(Vector3 top)
    {
        m.Lathe(porcelainMat(),top,new[]{new Vector2(.0f,.0f),new Vector2(.12f,.0f),new Vector2(.18f,.04f),new Vector2(.21f,.12f),new Vector2(.215f,.13f),new Vector2(.19f,.13f),new Vector2(.165f,.06f),new Vector2(.0f,.03f)},28,Quaternion.identity,false,false);
    }

    // ---- ceilings: the malls white with linear LED lines along the axes (the fascias are the bulkheads at the fronts); the
    // entrance hall at 20.5 under larch slats on black felt with LED lines
    static void G1Ceilings()
    {
        foreach(int l in new[]{0,1})
        {
            float yc=MallCeil(l);
            // the malls' ceilings: timber slats on black felt with LED lines (owner 11.11 18:28: no all-white interiors)
            void C(float x0,float x1,float z0,float z1){if(x1-x0>.3f&&z1-z0>.3f)SlatCeiling(x0,x1,z0,z1,yc,null);}
            if(l==0){C(G1CinemaX1,G1EastX0,G1NorthMallZ0,G1NorthMallZ1);C(G1CinemaX1,G1EastX0,G1SouthMallZ0,G1SouthMallZ1);}
            else
            {
                C(G1CinemaX1,-G1VaultR,G1NorthMallZ0,G1NorthMallZ1);C(G1VaultR,G1EastX0,G1NorthMallZ0,G1NorthMallZ1);
                C(G1CinemaX1,-G1VaultR,G1SouthMallZ0,G1SouthMallZ1);C(G1VaultR,G1EastX0,G1SouthMallZ0,G1SouthMallZ1);
            }
            // the central mall's sides (P0 under the galleries x 8..12; P1 the galleries' outer strip x 10..12)
            float vx=l==0?G1VoidX:G1VaultR;
            C(-G1MallX,-vx,G1SouthMallZ1,G1NorthMallZ0);C(vx,G1MallX,G1SouthMallZ1,G1NorthMallZ0);
        }
        SlatCeiling(-20f+.1f,20f-.1f,G1HallZ0+.1f,G1HD+G1HallFront-.2f,G1HallTop-.02f,null);
        // over the hall's south strip (z 40..45, |x| 10..20) the main roof: a white ceiling at the P1 ceiling level, a white
        // fascia over the roof slab's edge (z 45) up to the hall's back wall, and the strips over the units at x +-20
        foreach(float sx in new[]{-1f,1f})
        {
            float a=Mathf.Min(sx*G1VaultR,sx*19.95f),b=Mathf.Max(sx*G1VaultR,sx*19.95f);   // to the strips over the units (x 19.95..20)
            SlatCeiling(a,b,G1NorthMallZ1,G1HallZ0,G1Ceil1,null);
            B(gBlackFascia,a,b,G1Ceil1,G1Roof,G1HallZ0,G1HallZ0+.03f);
            B(gBlackFascia,Mathf.Min(sx*20f,sx*19.95f),Mathf.Max(sx*20f,sx*19.95f),G1Ceil1,G1Roof-.88f,G1NorthMallZ1,G1HD-.26f);
        }
    }

    // ---- galleries: glass balustrades (black posts, cap and shoe) round the atrium void (gaps for the escalators' tops and
    // the panoramic lifts' doors), along the gallery over the entrance hall (gaps for the grand stairs) and over the food
    // court; the slab edges clad in white with an LED line
    const float G1EscHalf=2.2f,G1LiftZ=20f,G1StairX=15.5f,G1StairW=2.2f;
    static void G1Galleries()
    {
        float y=G1F1,h=1.08f,e=G1VoidX+.04f;
        void Rail(Vector3 a,Vector3 b,bool f=true,bool l=true)
        {
            GlassRail(a,b,h,1.4f,f,l);
            Col(Mathf.Min(a.x,b.x)-.06f,Mathf.Max(a.x,b.x)+.06f,y,y+h,Mathf.Min(a.z,b.z)-.06f,Mathf.Max(a.z,b.z)+.06f,"balustrada");
        }
        float zs=G1SouthMallZ1+.04f,zn=G1NorthMallZ0-.04f;
        foreach(float sx in new[]{-1f,1f})
        {
            Rail(new Vector3(sx*e,y,zs),new Vector3(sx*e,y,G1LiftZ-.6f));
            Rail(new Vector3(sx*e,y,G1LiftZ+.6f),new Vector3(sx*e,y,zn));
            foreach(float z in new[]{zs,zn})Rail(new Vector3(sx*G1EscHalf,y,z),new Vector3(sx*e,y,z),true,false);
            // slab edge cladding (from the P0 ceiling to the P1 floor) with an LED line, the void's long sides
            B(gBlackFascia,Mathf.Min(sx*G1VoidX,sx*(G1VoidX-.05f)),Mathf.Max(sx*G1VoidX,sx*(G1VoidX-.05f)),G1Ceil0,G1F1-.012f,G1SouthMallZ1,G1NorthMallZ0);
            B(led,Mathf.Min(sx*(G1VoidX-.05f),sx*(G1VoidX-.062f)),Mathf.Max(sx*(G1VoidX-.05f),sx*(G1VoidX-.062f)),G1Slab-.10f,G1Slab-.06f,G1SouthMallZ1+.06f,G1NorthMallZ0-.06f);
            // the lift's sill at P1 (the gallery edge to the landing door)
            B(granite,Mathf.Min(sx*G1VoidX,sx*(G1VoidX-.11f)),Mathf.Max(sx*G1VoidX,sx*(G1VoidX-.11f)),G1F1-.10f,G1F1,G1LiftZ-.55f,G1LiftZ+.55f,true);
        }
        // the bridges' edges at the void (z -13, z 28)
        foreach(var (a,b) in new[]{(-G1VoidX,-G1EscHalf),(G1EscHalf,G1VoidX)}){B(gBlackFascia,a,b,G1Ceil0,G1F1-.012f,G1NorthMallZ0-.05f,G1NorthMallZ0);B(gBlackFascia,a,b,G1Ceil0,G1F1-.012f,G1SouthMallZ1,G1SouthMallZ1+.05f);}   // not behind the escalators' tops
        // over the entrance hall (z 40, gaps for the grand stairs) and the food court (z -25)
        float zh=G1NorthMallZ1-.04f,s0=G1StairX-G1StairW*.5f-.05f,s1=G1StairX+G1StairW*.5f+.05f;
        Rail(new Vector3(-20f+.04f,y,zh),new Vector3(-s1,y,zh));Rail(new Vector3(-s0,y,zh),new Vector3(s0,y,zh));Rail(new Vector3(s1,y,zh),new Vector3(20f-.04f,y,zh));
        Rail(new Vector3(-G1FoodX+.04f,y,G1SouthMallZ0+.04f),new Vector3(G1FoodX-.04f,y,G1SouthMallZ0+.04f));
        B(gBlackFascia,-20f,20f,G1Ceil0-.02f,G1F1-.012f,G1NorthMallZ1,G1NorthMallZ1+.05f);B(led,-19.9f,19.9f,G1Slab-.10f,G1Slab-.06f,G1NorthMallZ1+.05f,G1NorthMallZ1+.062f);   // in the void, in front of the slab's edge
        B(gBlackFascia,-G1FoodX,G1FoodX,G1Ceil0-.02f,G1F1-.012f,G1SouthMallZ0-.05f,G1SouthMallZ0);B(led,-G1FoodX+.1f,G1FoodX-.1f,G1Slab-.10f,G1Slab-.06f,G1SouthMallZ0-.062f,G1SouthMallZ0-.05f);
        // the hall's side slab edges (x +-20, z 40..54.74) between the units' storeys
        foreach(float sx in new[]{-1f,1f})B(gBlackFascia,Mathf.Min(sx*20f,sx*19.95f),Mathf.Max(sx*20f,sx*19.95f),G1Ceil0,G1F1+.08f,G1NorthMallZ1,G1HD-.26f);
    }

    // ---- mall furniture: upholstered benches back to back in the north / south malls, planters with greenery, lit
    // directory totems by the escalators, bins
    static void G1MallFurniture()
    {
        foreach(int l in new[]{0,1})
        {
            float y=LevelY(l);
            foreach(float x in new[]{-35f,-25f,25f,35f})
            {
                StationBench(new Vector3(x,y,(G1NorthMallZ0+G1NorthMallZ1)*.5f-.45f),Vector3.back,4,"ławka pasaż G1",2,true);
                StationBench(new Vector3(x,y,(G1SouthMallZ0+G1SouthMallZ1)*.5f+.45f),Vector3.forward,4,"ławka pasaż G1",2,true);
            }
            foreach(float x in new[]{-41f,-31f,31f,41f})foreach(float z in new[]{(G1NorthMallZ0+G1NorthMallZ1)*.5f+1.4f,(G1SouthMallZ0+G1SouthMallZ1)*.5f-1.4f})
                Prop(root,"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf",new Vector3(x,y,z),x*13f+z,.3f);
            foreach(float x in new[]{-28f,28f}){Bin(x,y,(G1NorthMallZ0+G1NorthMallZ1)*.5f);Bin(x,y,(G1SouthMallZ0+G1SouthMallZ1)*.5f);}
        }
        G1Directory(-5.2f,G1F0,6f,"ATRIUM · PARTER");G1Directory(5.2f,G1F0,6f,"ATRIUM · PARTER");
        G1Directory(-10.5f,G1F1,G1NorthMallZ0+2.5f,"PIĘTRO · PÓŁNOC");G1Directory(10.5f,G1F1,G1SouthMallZ1-2.5f,"PIĘTRO · POŁUDNIE");
        G1Directory(0f,G1F0,G1NorthMallZ1+2.0f,"HOL GŁÓWNY");
        G1Hall();
        // portrait advertising totems (owner 20:53: his 9:16 films "na telefonach ... albo gdzieś" - a portrait screen each
        // side, 0.9 x 1.6 m, on a black lacquer body with a red LED edge); BraxTelewizor plays the portrait films (on / off with AKCJA)
        foreach(var (x,y,z) in new[]{(-30f,G1F0,34f),(30f,G1F0,34f),(-30f,G1F0,-19f),(30f,G1F0,-19f),(-12.5f,G1F0,47.5f),(-30f,G1F1,34f),(30f,G1F1,-19f)})G1AdTotem(x,y,z);
    }
    static readonly Dictionary<string,Material> g1Plans=new Dictionary<string,Material>();
    static Material PlanMat(string tu)
    {
        if(g1Plans.TryGetValue(tu,out var mt)&&mt)return mt;
        var n0=new List<string>();var t0=new List<string>();var n1=new List<string>();var t1=new List<string>();
        foreach(var u in g1Units){(u.level==0?n0:n1).Add(u.name);(u.level==0?t0:t1).Add(u.trade);}
        n0.AddRange(new[]{"BRAXHORN MARKET","BRAXHORN HOME","KINO MULTIPLEX","FOOD COURT","WC"});t0.AddRange(new[]{"food","home","games","food","x"});
        n1.AddRange(new[]{"SPORT ARENA","KINO MULTIPLEX","WC"});t1.AddRange(new[]{"sport","games","x"});
        var px=new Color32[BraxChampionsKinoEkrany.PGW*BraxChampionsKinoEkrany.PGH];
        BraxChampionsKinoEkrany.PlanGalerii(px,n0.ToArray(),t0.ToArray(),n1.ToArray(),t1.ToArray(),tu);
        mt=ScreenMaterial("g1_plan_"+g1Plans.Count,BraxChampionsKinoEkrany.PGW,BraxChampionsKinoEkrany.PGH,px);g1Plans[tu]=mt;return mt;
    }
    // a screen with its picture (UV 0..1, read from the front): c = the glass's centre, n = toward the viewer; a black
    // bezel round it 4 mm proud (never over the picture); the caller keeps 15 mm free behind the glass
    static void ScreenPanel(Material mat,Vector3 c,Vector3 n,float w,float h,float bz=.03f,bool picture=true)
    {
        n.y=0;n.Normalize();var r=Vector3.Cross(n,Vector3.up).normalized;var up=Vector3.up;
        var a=c-r*(w*.5f)-up*(h*.5f);var b=c+r*(w*.5f)-up*(h*.5f);var cc=c+r*(w*.5f)+up*(h*.5f);var d=c-r*(w*.5f)+up*(h*.5f);
        if(picture)m.QuadUV(mat,a,d,cc,b,new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0));
        var rot=Quaternion.LookRotation(n,Vector3.up);
        foreach(float s in new[]{-1f,1f})
        {
            m.Box(frame,c+up*(s*(h*.5f+bz*.5f))-n*.0055f,new Vector3(w+2f*bz,bz,.019f),rot);
            m.Box(frame,c+r*(s*(w*.5f+bz*.5f))-n*.0055f,new Vector3(bz,h,.019f),rot);
        }
        Screens++;
    }
    static void G1Directory(float x,float y,float z,string tu)
    {
        RB(gDark,x-.6f,x+.6f,y,y+2.2f,z-.12f,z+.12f,.04f,true);
        var lm=new BraxChampionsMeshBatch();
        foreach(float f in new[]{-1f,1f})
        {
            float zf=z+f*.12f;
            ScreenPanel(PlanMat(tu),new Vector3(x,y+1.25f,zf+f*.016f),new Vector3(0,0,f),1.0f,1.5f,.02f);
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"PLAN GALERII",new Vector3(x,y+2.05f,zf+f*.002f),f>0?0f:180f,.09f,.01f,.10f,1.0f);
        }
        BraxChampionsLetters3D.Build(lm,root,"G1 plan galerii");
        Signs++;
    }

    // ---- the entrance hall (owner 11.11 19:49: "na tym holu jest pusto ... jakiś lombard z telefonami ... mały"): an
    // island kiosk LOMBARD GSM west of the axis (black lacquer counter with an oak top, glass vitrines with phones, tablets
    // and watches on stands, a central oak display tower with lit shelves and the price screen, a black fascia ring on slim
    // posts with the lit name, a red LED line, two bar stools for the customers), east of the axis a round seating island
    // (cognac leather round a planter with low indoor plants), planters by the stairs
    static Material phoneScreenA,phoneScreenB,lombardScreen;
    static void Phone(Vector3 p,Vector3 face,int v,float scale=1f)
    {
        face.y=0;face.Normalize();var rot=Quaternion.LookRotation(face,Vector3.up)*Quaternion.Euler(-14f,0,0);
        var up=rot*Vector3.up;var fw=rot*Vector3.forward;var r=rot*Vector3.right;
        float w=.074f*scale,h=.156f*scale;
        m.Box(glass,p+Vector3.up*.035f-face*.03f,new Vector3(.06f*scale,.07f,.004f),Quaternion.LookRotation(face,Vector3.up)*Quaternion.Euler(-14f,0,0));   // the acrylic stand
        var c=p+Vector3.up*(h*.5f+.01f);
        m.RoundedBox(blackMatt,c,new Vector3(w,h,.0085f),.006f*scale,rot,2);
        var s=c+fw*.0045f;var a=s-r*(w*.44f)-up*(h*.45f);var b=s+r*(w*.44f)-up*(h*.45f);var cc=s+r*(w*.44f)+up*(h*.45f);var d=s-r*(w*.44f)+up*(h*.45f);
        m.QuadUV(v%2==0?phoneScreenA:phoneScreenB,a,b,cc,d,new Vector2(1,0),new Vector2(0,0),new Vector2(0,1),new Vector2(1,1));
    }
    static void G1Hall()
    {
        float y=G1F0;
        if(!phoneScreenA)
        {
            var pa=new Color32[BraxChampionsKinoEkrany.FW*BraxChampionsKinoEkrany.FH];BraxChampionsKinoEkrany.EkranTelefonu(pa,0);phoneScreenA=ScreenMaterial("g1_telefon_a",BraxChampionsKinoEkrany.FW,BraxChampionsKinoEkrany.FH,pa);
            var pb=new Color32[BraxChampionsKinoEkrany.FW*BraxChampionsKinoEkrany.FH];BraxChampionsKinoEkrany.EkranTelefonu(pb,1);phoneScreenB=ScreenMaterial("g1_telefon_b",BraxChampionsKinoEkrany.FW,BraxChampionsKinoEkrany.FH,pb);
            var pl=new Color32[BraxChampionsKinoEkrany.LW*BraxChampionsKinoEkrany.LH];BraxChampionsKinoEkrany.EkranLombardu(pl);lombardScreen=ScreenMaterial("g1_lombard_cennik",BraxChampionsKinoEkrany.LW,BraxChampionsKinoEkrany.LH,pl);
        }
        if(!wcTile)WcMaterials();
        // ---- LOMBARD GSM: x -9.0..-5.0, z 46.8..49.2
        const float kx0=-9.0f,kx1=-5.0f,kz0=46.8f,kz1=49.2f;float kc=(kx0+kx1)*.5f,kzc=(kz0+kz1)*.5f;
        RB(gCinemaDark,kx0,kx1,y,y+.95f,kz0,kz1,.04f,true,2);                                       // the counter body (closed ring, the staff gap east)
        RB(gOak,kx0-.03f,kx1+.03f,y+.95f,y+.99f,kz0-.03f,kz1+.03f,.012f,false,2);                   // the oak top
        B(gRedLit,kx0+.05f,kx1-.05f,y+.10f,y+.115f,kz0-.012f,kz0);B(gRedLit,kx0+.05f,kx1-.05f,y+.10f,y+.115f,kz1,kz1+.012f);   // the red LED line (owner: black, red, white)
        // glass vitrines on the long sides: black frames, glass, the goods on black velvet
        foreach(float sz in new[]{-1f,1f})
        {
            float zo=sz<0?kz0+.02f:kz1-.02f,zi=sz<0?kz0+.42f:kz1-.42f;
            RB(gVelvetBlack,kx0+.12f,kx1-.12f,y+.99f,y+1.01f,Mathf.Min(zo,zi),Mathf.Max(zo,zi),.004f,false,1);
            foreach(var (a,b) in new[]{(kx0+.10f,kx0+.13f),(kx1-.13f,kx1-.10f)})B(frame,a,b,y+.99f,y+1.33f,Mathf.Min(zo,zi),Mathf.Max(zo,zi));
            B(frame,kx0+.10f,kx1-.10f,y+1.30f,y+1.33f,Mathf.Min(zo,zi),Mathf.Max(zo,zi));
            var g0=new Vector3(kx0+.13f,y+1.01f,zo);var g1=new Vector3(kx1-.13f,y+1.01f,zo);
            Q(glass,g0,g1,g1+Vector3.up*.29f,g0+Vector3.up*.29f,new Vector3(0,0,sz));Q(glass,g0,g1,g1+Vector3.up*.29f,g0+Vector3.up*.29f,new Vector3(0,0,-sz));
            var t0=new Vector3(kx0+.13f,y+1.30f,zo);var t1=new Vector3(kx1-.13f,y+1.30f,zi);
            Q(glass,t0,new Vector3(t1.x,t0.y,t0.z),t1,new Vector3(t0.x,t0.y,t1.z),Vector3.up);
            for(int i=0;i<9;i++){var p=new Vector3(kx0+.38f+i*.40f,y+1.012f,(zo+zi)*.5f);if(i%3==2){Watch(p,new Vector3(0,0,sz));}else Phone(p,new Vector3(0,0,sz));}
        }
        // the central tower: oak, lit shelves of phones both ways, the price screen over the till on the west end
        RB(gOak,kc-.9f,kc+.9f,y+.99f,y+2.35f,kzc-.18f,kzc+.18f,.02f,true,2);
        foreach(float sz in new[]{-1f,1f})
            for(int j=0;j<2;j++)
            {
                float ys=y+1.45f+j*.42f,zf=kzc+sz*.18f;
                B(gStoneTop,kc-.8f,kc+.8f,ys,ys+.025f,Mathf.Min(zf,zf+sz*.16f),Mathf.Max(zf,zf+sz*.16f));
                B(ledWarm,kc-.78f,kc+.78f,ys+.38f,ys+.392f,Mathf.Min(zf+sz*.012f,zf+sz*.06f),Mathf.Max(zf+sz*.012f,zf+sz*.06f));
                for(int i=0;i<5;i++)Phone(new Vector3(kc-.64f+i*.32f,ys+.025f,zf+sz*.08f),new Vector3(0,0,sz));
            }
        ScreenPanel(lombardScreen,new Vector3(kc-.905f-.016f,y+1.85f,kzc),Vector3.left,.62f,.35f,.02f);
        ScreenPanel(lombardScreen,new Vector3(kc+.905f+.016f,y+1.85f,kzc),Vector3.right,.62f,.35f,.02f);
        // the till: a card terminal and a small screen on the east end, the staff stool
        RB(blackMatt,kx1-.45f,kx1-.30f,y+.99f,y+1.02f,kzc-.10f,kzc+.10f,.008f,false,1);
        RB(blackMatt,kx1-.42f,kx1-.34f,y+1.02f,y+1.10f,kzc-.04f,kzc+.04f,.01f,false,1);
        // the fascia ring on four slim black posts (2.45..2.95) with LOMBARD GSM lit white on both long sides
        foreach(float px in new[]{kx0+.05f,kx1-.05f})foreach(float pz in new[]{kz0+.05f,kz1-.05f})m.Cylinder(frame,new Vector3(px,y+.99f,pz),.025f,1.46f,12,Quaternion.identity,true);
        RB(gCinemaDark,kx0-.05f,kx1+.05f,y+2.45f,y+2.95f,kz0-.05f,kz1+.05f,.03f,false,2);
        B(gRedLit,kx0-.04f,kx1+.04f,y+2.44f,y+2.452f,kz0-.04f,kz1+.04f);
        var lm=new BraxChampionsMeshBatch();
        foreach(float sz in new[]{-1f,1f})
        {
            float zf=sz<0?kz0-.05f-.002f:kz1+.05f+.002f;
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"LOMBARD GSM",new Vector3(kc,y+2.62f,zf),sz<0?180f:0f,.22f,.04f,.12f,3.6f);
            BraxChampionsLetters3D.Text(lm,gRedLit,gLetterSide,"SKUP • SPRZEDAŻ • NAPRAWA",new Vector3(kc,y+2.51f,zf),sz<0?180f:0f,.065f,.015f,.14f,3.6f);
        }
        BraxChampionsLetters3D.Build(lm,root,"G1 szyld LOMBARD GSM");Signs++;
        foreach(float sz in new[]{-1f,1f})BarStool(new Vector3(kc-.6f,y,sz<0?kz0-.45f:kz1+.45f),new Vector3(0,0,-sz));
        G1Shops++;
        // ---- the seating island east: a round leather bench ring (R 1.15..1.75, seat .45) round a granite planter with a tree
        var sc=new Vector3(7f,y,48f);
        Ring(gStoneTop,sc,.95f,1.15f,y,y+.60f,48);Disc(lawn,sc+Vector3.up*.56f,.95f,48);
        Ring(gCinemaDark,sc,1.15f,1.75f,y,y+.36f,48);
        Ring(gChairLeather,sc,1.17f,1.73f,y+.36f,y+.46f,48);
        // owner 11.11 20:18 ("z tym drzewem w galerii to przesadziłeś"): no tree indoors - low indoor plants in the planter
        foreach(var (dx,dz,k) in new[]{(0f,0f,1),(-.45f,.35f,0),(.45f,-.35f,0),(.3f,.5f,1),(-.35f,-.5f,1)})Prop(root,k==0?"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf":"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(sc.x+dx,y+.56f,sc.z+dz),dx*90f+dz*40f,0f);
        for(int i=0;i<8;i++){float a=i*45f*Mathf.Deg2Rad;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var p=sc+d*1.45f;Seat(p.x,y+.46f,p.z,d,"siedzisko wyspy w holu");}
        Col(sc.x-1.75f,sc.x+1.75f,y,y+.46f,sc.z-1.75f,sc.z+1.75f,"wyspa z siedziskami");
        // planters at the stairs' feet
        foreach(float sx in new[]{-1f,1f})Prop(root,"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf",new Vector3(sx*12.6f,y,51.6f),sx*30f,.3f);
    }
    static Material[] totemMats;
    static void G1AdTotem(float x,float y,float z)
    {
        if(totemMats==null)
        {
            totemMats=new Material[3];
            for(int k=0;k<3;k++){var px=new Color32[BraxChampionsKinoEkrany.VW*BraxChampionsKinoEkrany.VH];BraxChampionsKinoEkrany.ReklamaPion(px,k);totemMats[k]=ScreenMaterial("g1_reklama_pion_"+k,BraxChampionsKinoEkrany.VW,BraxChampionsKinoEkrany.VH,px);}
        }
        RB(gCinemaDark,x-.55f,x+.55f,y,y+2.25f,z-.15f,z+.15f,.05f,true,3);
        B(gRedLit,x-.575f,x-.55f,y+.05f,y+2.20f,z-.06f,z+.06f);B(gRedLit,x+.55f,x+.575f,y+.05f,y+2.20f,z-.06f,z+.06f);   // the LED fins, 25 mm proud of the sides
        int i=0;
        foreach(float f in new[]{-1f,1f})
        {
            var c=new Vector3(x,y+1.25f,z+f*(.15f+.016f));var mat=totemMats[(i+(int)Mathf.Abs(x+z))%3];
            ScreenPanel(mat,c,new Vector3(0,0,f),.90f,1.60f,.02f,false);                            // the bezel
            BraxKatalog.WorkingScreen(Kat(),mat,c,new Vector3(0,0,f),.90f,1.60f,true,"G1 totem reklamowy "+x+" "+z,i++);   // the picture: the films (9:16), on / off
        }
    }
    // a bar stool: a black steel base and column, a round cognac leather seat, a foot ring; a BraxSeat
    static void BarStool(Vector3 p,Vector3 face)
    {
        m.Cylinder(frame,p,.22f,.02f,24,Quaternion.identity,true);
        m.Cylinder(chrome,p+Vector3.up*.02f,.03f,.62f,14,Quaternion.identity,false);
        var ring=new List<Vector3>();for(int k=0;k<=20;k++){float a=k*Mathf.PI*2f/20f;ring.Add(p+Vector3.up*.30f+new Vector3(Mathf.Cos(a)*.17f,0,Mathf.Sin(a)*.17f));}
        for(int k=0;k+1<ring.Count;k++)m.Tube(chrome,ring[k],ring[k+1],.011f,8);
        for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f;m.Tube(chrome,p+Vector3.up*.30f+new Vector3(Mathf.Cos(a)*.17f,0,Mathf.Sin(a)*.17f),p+Vector3.up*.30f+new Vector3(Mathf.Cos(a)*.03f,0,Mathf.Sin(a)*.03f),.008f,6);}
        m.Lathe(gChairLeather,p+Vector3.up*.64f,new[]{new Vector2(0,0),new Vector2(.17f,0),new Vector2(.19f,.02f),new Vector2(.195f,.05f),new Vector2(.18f,.075f),new Vector2(0,.08f)},28,Quaternion.identity,true,true);
        Seat(p.x,p.y+.72f,p.z,face,"hoker");
        Col(p.x-.2f,p.x+.2f,p.y,p.y+.72f,p.z-.2f,p.z+.2f,"hoker");
    }

    // ================================================================ circulation (escalators, lifts, stairs)
    static void G1Circulation()
    {
        // two escalator pairs in the atrium: north (tops at the north bridge, z 28) and south (tops at the south bridge, z -13)
        G1Escalator("G1 schody ruchome N1 (w górę)",-1.4f,G1NorthMallZ0,-1f,true);
        G1Escalator("G1 schody ruchome N2 (w dół)",1.4f,G1NorthMallZ0,-1f,false);
        G1Escalator("G1 schody ruchome S1 (w górę)",1.4f,G1SouthMallZ1,1f,true);
        G1Escalator("G1 schody ruchome S2 (w dół)",-1.4f,G1SouthMallZ1,1f,false);
        // two panoramic lifts on the void's long sides, their doors to the galleries
        foreach(float sx in new[]{-1f,1f})G1GlassLift("G1 winda panoramiczna "+(sx<0?"zachód":"wschód"),sx,G1LiftZ);
        // the grand stairs in the entrance hall (both sides) up to the gallery over the hall (z 40)
        foreach(float sx in new[]{-1f,1f})G1Stair(sx*G1StairX,G1NorthMallZ1,1f);
    }
    // an escalator in the atrium along z: xc, zTop the upper comb line (the bridge's edge), dir the run's direction from the
    // top down (+1 toward +z); up / down. A real build: the moving step band (stainless treads, yellow edge lines, ribbed
    // risers; BraxSchodyRuchome), the ride (BraxSchodyRuchomeJazda: the player is carried, motor sound, the handrails' UV
    // scroll), comb plates, stainless landing plates, black skirts and truss cladding, glass balustrades (separate panes
    // clamped in the black deck, no posts - as real escalators) under black rubber handrails with newels at both ends
    static void G1Escalator(string name,float xc,float zTop,float dir,bool up)
    {
        float yTop=G1F1,yBot=G1F0,h=yTop-yBot,run=h/Mathf.Tan(30f*Mathf.Deg2Rad),sw=1.0f,plat=1.3f,ext=1.0f;
        float zBot=zTop+dir*run,zBotEnd=zBot+dir*plat;
        int n=Mathf.CeilToInt(run/.40f);float dz=run/n,dy=h/n;
        var band=Group(root,name+" | taśma stopni");
        var bm=new BraxChampionsMeshBatch();
        float hx=sw*.5f;
        for(int i=up?0:-1;i<=(up?n:n-1);i++)
        {
            float zs=zTop+dir*i*dz,zf=zs+dir*dz,y=yTop-(i+1)*dy-.012f,zp=zs+dir*.05f;   // 12 mm under the floors at the ends (into the combs)
            // the step at the comb line: its riser 25 mm back (inside the bridge's edge) and the step above it 25 mm shorter, so
            // no face of the band lies in the slab's edge face at rest (zero flicker); the band moves one step and repeats
            float zr=i==0?zs-dir*.025f:zs,ze=i==-1?zf-dir*.025f:zf;
            bm.BoxMinMax(stainless,xc-hx+.04f,xc+hx-.04f,y-.02f,y,Mathf.Min(zp,ze),Mathf.Max(zp,ze));
            bm.BoxMinMax(paintYellow,xc-hx+.04f,xc+hx-.04f,y-.02f,y,Mathf.Min(zs,zp),Mathf.Max(zs,zp));
            bm.BoxMinMax(paintYellow,xc-hx,xc-hx+.04f,y-.02f,y,Mathf.Min(zs,ze),Mathf.Max(zs,ze));bm.BoxMinMax(paintYellow,xc+hx-.04f,xc+hx,y-.02f,y,Mathf.Min(zs,ze),Mathf.Max(zs,ze));
            bm.BoxMinMax(stainless,xc-hx,xc+hx,y,y+dy-.02f,Mathf.Min(zr-.005f,zr+.005f),Mathf.Max(zr-.005f,zr+.005f));
            for(float xr=xc-hx+.1f;xr<xc+hx-.1f;xr+=.09f)bm.BoxMinMax(stainless,xr,xr+.02f,y+.02f,y+dy-.04f,Mathf.Min(zr,zr+dir*.012f),Mathf.Max(zr,zr+dir*.012f));
        }
        bm.Build(band,"stopnie",Folder,false);
        var ruch=band.gameObject.AddComponent<BraxSchodyRuchome>();
        ruch.krok=(up?new Vector3(0,dy,-dir*dz):new Vector3(0,-dy,dir*dz))*K;ruch.predkosc=.5f*K;
        foreach(var t in band.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        // comb plates (yellow) at the step line, stainless landing plates behind them, a stainless threshold strip
        foreach(var (zc,dk,yy) in new[]{(zTop,-dir,yTop),(zBot,dir,yBot)})
        {
            float zg=zc+dk*.18f,ze=zc+dk*plat;
            B(paintYellow,xc-hx-.004f,xc+hx+.004f,yy,yy+.015f,Mathf.Min(zc,zg),Mathf.Max(zc,zg));
            B(stainless,xc-hx-.004f,xc+hx+.004f,yy,yy+.012f,Mathf.Min(zg,ze),Mathf.Max(zg,ze));   // on the floors
            for(float x=xc-hx;x<xc+hx;x+=.06f)B(stainless,x+.01f,x+.022f,yy+.012f,yy+.016f,Mathf.Min(zg+dk*.08f,ze-dk*.08f),Mathf.Max(zg+dk*.08f,ze-dk*.08f));
        }
        var pT=new Vector3(xc,yTop,zTop);var pB=new Vector3(xc,yBot,zBot);
        GameObjectRamp(name+" bieg",pT,pB,sw);
        // the balustrade line (step nosings): flat 1 m beyond the top comb, the incline, flat 1 m beyond the bottom comb
        var line=new[]{new Vector3(0,yTop,zTop-dir*ext),new Vector3(0,yTop,zTop),new Vector3(0,yBot,zBot),new Vector3(0,yBot,zBot+dir*ext)};
        var porecze=new List<Renderer>();
        foreach(float s in new[]{-1f,1f})
        {
            float xs=xc+s*(hx+.095f),xg=xc+s*(hx+.10f);
            Vector3 L(int i,float up_)=>new Vector3(xs,line[i].y+up_,line[i].z);
            // skirts / deck (black): the flat ends as boxes, the incline as a beam into the floors
            B(stainless,xs-.09f,xs+.09f,yTop,yTop+.12f,Mathf.Min(zTop,zTop-dir*ext),Mathf.Max(zTop,zTop-dir*ext));
            B(stainless,xs-.09f,xs+.09f,yBot,yBot+.12f,Mathf.Min(zBot,zBot+dir*ext),Mathf.Max(zBot,zBot+dir*ext));
            Beam(stainless,L(1,-.44f),L(2,-.44f),.18f,1.12f);                    // owner 19:49: not black blocks - brushed steel
            // glass panes (12 mm joints) clamped in the deck, 0.88 high
            for(int k=0;k<3;k++)
            {
                var a=line[k];var b=line[k+1];float len=Vector3.Distance(a,b);int np=Mathf.Max(1,Mathf.CeilToInt(len/1.5f));
                for(int j=0;j<np;j++)
                {
                    var p0=Vector3.Lerp(a,b,j/(float)np);var p1=Vector3.Lerp(a,b,(j+1)/(float)np);var u=(p1-p0).normalized*.006f;
                    var q0=new Vector3(xg,p0.y+.10f,p0.z)+u;var q1=new Vector3(xg,p1.y+.10f,p1.z)-u;
                    Q(glass,q0,q1,q1+Vector3.up*.88f,q0+Vector3.up*.88f,Vector3.right*s);Q(glass,q0,q1,q1+Vector3.up*.88f,q0+Vector3.up*.88f,-Vector3.right*s);
                }
            }
            // the handrail (black rubber) on the glass edge with semicircular newels at both ends
            var pts=new List<Vector3>();float r=.16f,yh=1.02f;
            Vector3 H(int i)=>new Vector3(xg,line[i].y+yh,line[i].z);
            {var c0=H(0)+Vector3.down*r;pts.Add(new Vector3(xg,line[0].y+.14f,line[0].z+dir*.06f));for(int k=0;k<=8;k++){float th=Mathf.Lerp(-90f,90f,k/8f)*Mathf.Deg2Rad;pts.Add(c0+new Vector3(0,r*Mathf.Sin(th),-dir*r*Mathf.Cos(th)));}}
            pts.Add(H(1));pts.Add(H(2));
            {var c3=H(3)+Vector3.down*r;for(int k=0;k<=8;k++){float th=Mathf.Lerp(90f,-90f,k/8f)*Mathf.Deg2Rad;pts.Add(c3+new Vector3(0,r*Mathf.Sin(th),dir*r*Mathf.Cos(th)));}pts.Add(new Vector3(xg,line[3].y+.14f,line[3].z-dir*.06f));}
            var hm=new BraxChampionsMeshBatch();
            for(int k=0;k+1<pts.Count;k++)hm.Tube(rubber,pts[k],pts[k+1],.035f,10);
            var hr=hm.Build(root,name+" poręcz "+(s<0?"L":"P"),Folder,false);
            foreach(var rr in hr.GetComponentsInChildren<Renderer>(true))porecze.Add(rr);
            foreach(var t in hr.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            Col(xs-.1f,xs+.1f,yBot,yTop+1.05f,Mathf.Min(zTop-dir*ext,zBot+dir*ext),Mathf.Max(zTop-dir*ext,zBot+dir*ext),"schody ruchome balustrada");
        }
        // the truss's underside cladding (black) under the incline
        Beam(gOak,new Vector3(xc,yTop-1.0f,zTop),new Vector3(xc,yBot-1.0f,zBot),sw+.33f,.06f);   // the underside in oak (owner 19:49: not black blocks)
        var drive=new GameObject(name+" | napęd i jazda").AddComponent<BraxSchodyRuchomeJazda>();
        drive.transform.SetParent(root,false);
        drive.gora=root.TransformPoint(pT*K);drive.dol=root.TransformPoint(pB*K);drive.szer=sw*K;drive.wGore=up;drive.predkosc=.5f*K;drive.okresPoreczy=.5f*K;drive.porecze=porecze.ToArray();
        G1Escalators++;
    }
    // a panoramic lift on the void's long side (sx -1 west, +1 east), its doors toward the gallery: a glass shaft in black
    // frames (separate panes between transoms) from P0 to above P1, the building lift (BraxWinda) inside
    static void G1GlassLift(string id,float sx,float zc)
    {
        float hw=.85f,y0=G1F0,y1=G1F1+2.9f,xc=sx*(G1VoidX-.95f),xd=xc+sx*hw,xv=xc-sx*hw;
        foreach(float px in new[]{-1f,1f})foreach(float pz in new[]{-1f,1f})B(frame,xc+px*hw-.05f,xc+px*hw+.05f,y0,y1,zc+pz*hw-.05f,zc+pz*hw+.05f,true);
        B(frame,xc-hw-.05f,xc+hw+.05f,y1,y1+.30f,zc-hw-.05f,zc+hw+.05f);
        float[] ty={y0,y0+2.4f,G1Ceil0,G1F1+.05f,G1F1+2.4f,y1};
        // transoms on the three glass sides, the panes between them
        foreach(float yy in new[]{y0+2.4f,G1Ceil0,G1F1+.05f,G1F1+2.4f})
        {
            B(frame,Mathf.Min(xv,xv)-.035f,xv+.035f,yy-.035f,yy+.035f,zc-hw+.05f,zc+hw-.05f);
            foreach(float pz in new[]{-1f,1f})B(frame,xc-hw+.05f,xc+hw-.05f,yy-.035f,yy+.035f,zc+pz*hw-.035f,zc+pz*hw+.035f);
        }
        for(int k=0;k+1<ty.Length;k++)
        {
            float ya=ty[k]+(k==0?.0f:.035f),yb=ty[k+1]-(k+1==ty.Length-1?0f:.035f);
            var a=new Vector3(xv,ya,zc-hw+.05f);var b=new Vector3(xv,ya,zc+hw-.05f);
            Q(glass,a,b,b+Vector3.up*(yb-ya),a+Vector3.up*(yb-ya),Vector3.right);Q(glass,a,b,b+Vector3.up*(yb-ya),a+Vector3.up*(yb-ya),Vector3.left);
            foreach(float pz in new[]{-1f,1f})
            {
                var c=new Vector3(xc-hw+.05f,ya,zc+pz*hw);var d=new Vector3(xc+hw-.05f,ya,zc+pz*hw);
                Q(glass,c,d,d+Vector3.up*(yb-ya),c+Vector3.up*(yb-ya),Vector3.forward);Q(glass,c,d,d+Vector3.up*(yb-ya),c+Vector3.up*(yb-ya),Vector3.back);
            }
        }
        // the door side: door frames (black) at both stops, glass between the openings
        foreach(float ys in new[]{y0,G1F1})
        {
            B(frame,Mathf.Min(xd,xd)-.05f,xd+.05f,ys,ys+2.25f,zc-.52f,zc-.45f);B(frame,xd-.05f,xd+.05f,ys,ys+2.25f,zc+.45f,zc+.52f);
            B(frame,xd-.05f,xd+.05f,ys+2.25f,ys+2.35f,zc-.52f,zc+.52f);
            foreach(float pz in new[]{-1f,1f}){float za=zc+pz*.52f,zb=zc+pz*(hw-.05f);var a=new Vector3(xd,ys,Mathf.Min(za,zb));var b=new Vector3(xd,ys,Mathf.Max(za,zb));Q(glass,a,b,b+Vector3.up*2.35f,a+Vector3.up*2.35f,Vector3.right);Q(glass,a,b,b+Vector3.up*2.35f,a+Vector3.up*2.35f,Vector3.left);}
        }
        foreach(var (ya,yb) in new[]{(y0+2.35f,G1F1),(G1F1+2.35f,y1)})
        {
            var a=new Vector3(xd,ya,zc-hw+.05f);var b=new Vector3(xd,ya,zc+hw-.05f);
            Q(glass,a,b,b+Vector3.up*(yb-ya),a+Vector3.up*(yb-ya),Vector3.right);Q(glass,a,b,b+Vector3.up*(yb-ya),a+Vector3.up*(yb-ya),Vector3.left);
        }
        B(frame,xd-.035f,xd+.035f,G1F1-.035f,G1F1+.035f,zc-hw+.05f,zc+hw-.05f);
        Col(Mathf.Min(xv,xv)-.06f,xv+.06f,y0,y1,zc-hw,zc+hw,"szyb windy");
        foreach(float pz in new[]{-1f,1f})Col(xc-hw,xc+hw,y0,y1,zc+pz*hw-.06f,zc+pz*hw+.06f,"szyb windy");
        BuildingLift(id,xc,zc,hw-.12f,hw-.12f,sx<0?90f:-90f,new[]{y0+.012f,G1F1+.012f},new[]{"PARTER","PIĘTRO"},.42f);
    }
    // a grand stair in the entrance hall along z: xc, zTop the P1 gallery edge, dir +1 = the run descends toward +z.
    // A solid white body to the floor (no underside to see), granite treads with a 2 cm nosing past the sides, glass
    // balustrades on the treads (black posts, cap, shoe)
    static void G1Stair(float xc,float zTop,float dir)
    {
        int n=34;float rise=(G1F1-G1F0)/n,run=.29f,w=G1StairW,hw=w*.5f;
        for(int i=0;i<n;i++)
        {
            float zf=zTop+dir*(n-1-i)*run,zb=zf+dir*run,y=G1F0+(i+1)*rise;   // step i (from the bottom), front edge zb, back zf
            B(gBlackFascia,xc-hw,xc+hw,G1F0,y-.04f,Mathf.Min(zf,zb),Mathf.Max(zf,zb),true);
            B(granite,xc-hw-.02f,xc+hw+.02f,y-.04f,y,Mathf.Min(zf,zb+dir*.02f),Mathf.Max(zf,zb+dir*.02f));
        }
        float zBot=zTop+dir*n*run;
        GameObjectRamp("G1 schody główne",new Vector3(xc,G1F1,zTop),new Vector3(xc,G1F0,zBot),w);
        foreach(float s in new[]{-1f,1f})
        {
            float x=xc+s*(hw-.07f);
            GlassRail(new Vector3(x,G1F1,zTop+dir*.137f),new Vector3(x,G1F0+rise,zBot-dir*(run-.137f)),1.0f,1.3f);   // posts off the nosing planes
            Col(x-.06f,x+.06f,G1F0,G1F1+1.0f,Mathf.Min(zTop,zBot),Mathf.Max(zTop,zBot),"balustrada schodów");
        }
        B(rubber,xc-hw,xc+hw,G1F0,G1F0+.012f,Mathf.Min(zBot,zBot+dir*1.2f),Mathf.Max(zBot,zBot+dir*1.2f));   // the foot mat
    }
}

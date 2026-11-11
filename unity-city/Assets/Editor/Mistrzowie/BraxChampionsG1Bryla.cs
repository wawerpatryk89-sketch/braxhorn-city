using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 shell (board "ORTOGRAFIE - ELEWACJE", "MODUŁ ELEWACJI - SIATKA 5,00 m", "WYMIARY I PRZEKRÓJ"): the structure on the
// 10 x 10 m grid (columns .6 x .6, round-clad in the malls), the P0 slab, the P1 slab with its openings, the roof slab;
// the facades on the 5 m module - anthracite composite cassettes (M-01) with 16 mm open joints on a black backing, the
// tinted glazing (M-03) recessed 10 cm behind black anodised mullions and transoms (glass 4.8 + spandrel 1.2 per storey),
// light architectural-concrete pilasters and cornices (M-02), red matt fins (M-04), a dark granite plinth (M-06);
// the main entrance block projecting 2.5 m: a 20 m glass curtain wall over a windbreak lobby with automatic sliding doors,
// the canopy, the sign band; the cinema block (north-west) dark with KINO MULTIPLEX, the BRAXHORN CITY block (north-east)
// with the owner's bull mark; the atrium's elliptical glass barrel vault (span 20, rise 8) from the food court to the hall.
public static partial class BraxChampionsD2
{
    const float G1VaultRise=8f,G1HallZ0=45f,G1HallFront=2.5f,G1HallTop=20.5f;
    const float GlassTop0=5.12f,GlassTop1=11.12f;                            // P0 glass FFL..5.12, spandrel ..6.32; P1 ..11.12
    // facade frame: side 0 north (z +HD, outward +z), 1 south (z -HD), 2 west (x -HW), 3 east (x +HW); u along it, o outward
    static Vector3 FP(int side,float u,float y,float o)
    {
        switch(side){case 0:return new Vector3(u,y,G1HD+o);case 1:return new Vector3(u,y,-G1HD-o);case 2:return new Vector3(-G1HW-o,y,u);default:return new Vector3(G1HW+o,y,u);}
    }
    static void FB(Material mat,int side,float u0,float u1,float y0,float y1,float o0,float o1,bool col=false)
    {
        var a=FP(side,u0,y0,o0);var b=FP(side,u1,y1,o1);
        B(mat,Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),y0,y1,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z),col);
    }
    static Vector3 FOut(int side)=>side==0?Vector3.forward:side==1?Vector3.back:side==2?Vector3.left:Vector3.right;
    static void FGlass(Material mat,int side,float u0,float u1,float y0,float y1,float o)
    {
        var a=FP(side,u0,y0,o);var b=FP(side,u1,y0,o);var c=FP(side,u1,y1,o);var d=FP(side,u0,y1,o);
        Q(mat,a,b,c,d,FOut(side));Q(mat,a,b,c,d,-FOut(side));
    }
    // cassettes (M-01) over u0..u1 x y0..y1 on a black backing: 2.5 x ~1.5 m panels, 16 mm open joints
    static void Cassettes(int side,float u0,float u1,float y0,float y1,Material mat=null)
    {
        mat=mat??gCladding;
        if(u1-u0<.05f||y1-y0<.05f)return;
        FB(gCinemaDark,side,u0,u1,y0,y1,-.27f,0f,true);                         // 2 cm deeper than the mullions
        int nu=Mathf.Max(1,Mathf.RoundToInt((u1-u0)/2.5f)),nv=Mathf.Max(1,Mathf.RoundToInt((y1-y0)/1.5f));
        float du=(u1-u0)/nu,dv=(y1-y0)/nv;
        for(int i=0;i<nu;i++)for(int j=0;j<nv;j++)
            FB(mat,side,u0+i*du+.008f,u0+(i+1)*du-.008f,y0+j*dv+.008f,y0+(j+1)*dv-.008f,0f,.05f);
    }
    // a glazed panel: tinted glass 10 cm behind the face, black mullions at u0 / u1 and transoms at y0 / y1
    static void Glazed(int side,float u0,float u1,float y0,float y1,bool m0=true,bool m1=true)
    {
        FGlass(glassDark,side,u0,u1,y0,y1,-.10f);
        if(m0)FB(frame,side,u0-.035f,u0+.035f,y0,y1,-.25f,.03f);
        if(m1)FB(frame,side,u1-.035f,u1+.035f,y0,y1,-.25f,.03f);
        FB(frame,side,u0+.035f,u1-.035f,y0-.035f,y0+.035f,-.25f,.03f);FB(frame,side,u0+.035f,u1-.035f,y1-.035f,y1+.035f,-.25f,.03f);
    }
    // a two-storey shop bay of 5 m: glass P0 and P1, the spandrels as cassettes, the colliders
    static void ShopBay(int side,float u0,float u1,bool p1=true)
    {
        Glazed(side,u0,u1,G1F0+.05f,GlassTop0);
        Cassettes(side,u0+.035f,u1-.035f,GlassTop0+.035f,G1F1-.035f);                 // to the P1 glazing's bottom transom
        if(p1){Glazed(side,u0,u1,G1F1,GlassTop1);Cassettes(side,u0+.035f,u1-.035f,GlassTop1+.035f,G1Roof);}
        else Cassettes(side,u0+.035f,u1-.035f,G1F1,G1Roof);
        FCol(side,u0,u1,G1F0,G1Roof);
    }
    // double-height glazing (cinema lobby, food court): mullions every bay, transoms every ~2.7 m
    static void TallBay(int side,float u0,float u1)
    {
        float[] ys={G1F0+.05f,3.0f,5.75f,8.45f,GlassTop1};
        for(int k=0;k+1<ys.Length;k++)Glazed(side,u0,u1,ys[k],ys[k+1],k==0,k==0);
        FB(frame,side,u0-.035f,u0+.035f,ys[0],ys[ys.Length-1],-.25f,.03f);FB(frame,side,u1-.035f,u1+.035f,ys[0],ys[ys.Length-1],-.25f,.03f);
        Cassettes(side,u0+.035f,u1-.035f,GlassTop1+.035f,G1Roof);
        FCol(side,u0,u1,G1F0,G1Roof);
    }
    static void FCol(int side,float u0,float u1,float y0,float y1)
    {
        var a=FP(side,u0,y0,-.30f);var b=FP(side,u1,y1,.05f);
        Col(Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),y0,y1,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z),"elewacja");
    }
    static void Pilaster(int side,float u,float w=.9f){FB(gLight,side,u-w*.5f,u+w*.5f,-.05f,G1Top,.05f,.45f,true);}
    static void Fin(int side,float u){FB(gRed,side,u-.15f,u+.15f,G1F0,G1Top-.8f,.065f,.85f,true);}   // under the cornice, 15 mm off the bands

    // a column: black satin cylinder R .40, a 12 cm brushed stainless base band 12 mm proud
    static Material gColumn;
    static void G1Column(float x,float z,float y0,float y1)
    {
        if(!gColumn)gColumn=S("g1_column_black_satin",new Color(.045f,.045f,.05f),.62f,.35f);
        m.Cylinder(gColumn,new Vector3(x,y0,z),.40f,y1-y0,32,Quaternion.identity,false);
        m.Cylinder(stainless,new Vector3(x,y0,z),.412f,.12f,32,Quaternion.identity,true);
    }

    // ================================================================ shell
    static void G1Shell()
    {
        // ---- slabs: ground (to the walk level; the finishes per zone in the interior), P1 with its openings, the roof
        B(concrete,-G1HW+.27f,G1HW-.27f,-.40f,WalkY,-G1HD+.27f,G1HD-.27f,true);                 // to the façade backing (no shared faces)
        Slab(concrete,-G1HW+.27f,G1HW-.27f,-G1HD+.27f,G1HD-.27f,G1Slab,G1F1-.12f,G1P1Holes(),true);
        var rh=new List<(float x0,float x1,float z0,float z1)>{(-G1VaultR,G1VaultR,G1VaultZ0,G1HallZ0),(-20f,20f,G1HallZ0,G1HD)};
        Slab(gRoof,-G1HW+.27f,G1HW-.27f,-G1HD+.27f,G1HD-.27f,G1Roof-.9f,G1Roof,rh,true);   // to the cassettes' backing (no shared faces)
        // ---- columns on the 10 m grid (not in the cinema halls, not in the atrium's middle line)
        for(float x=-75f;x<=75f;x+=10f)for(float z=-45f;z<=45f;z+=10f)
        {
            if(x<G1CinemaX1-.1f||Mathf.Abs(x)<.1f)continue;
            if(z>G1HallZ0-.1f&&Mathf.Abs(x)<20.1f)continue;
            if(Mathf.Abs(x)<G1VoidX&&z>G1SouthMallZ1&&z<G1NorthMallZ0)continue;     // nothing standing in the atrium's void (owner 19:29: the columns were not joined)
            if((Mathf.Abs(z-35f)<.1f||Mathf.Abs(z+15f)<.1f)&&Mathf.Abs(x)<47.1f)continue;   // nothing on the malls' axes (owner 11.11 17:52: the column hid the KINO sign)
            if(x>-47.1f&&x<-29.9f&&z<-24.9f)continue;                                // the toilet block: in its walls (owner 19:49: a column in the WC)
            if(x>29.9f&&x<47.1f&&Mathf.Abs(z+25f)<.1f)continue;                      // the SE unit's shop front line
            // owner 11.11 19:49 ("Słupy ... niektóre rzeczy są niewykończone"): one column everywhere - round, black satin
            // (the gallery's black / red / white), a brushed stainless base band; no more white / walnut / concrete mix
            bool food=Mathf.Abs(x)<=G1FoodX+.1f&&z<G1FoodZ1;
            bool home=x>G1EastX0&&z>-38f&&z<10f;
            float top=food?G1Roof-.9f:G1Slab;
            G1Column(x,z,G1F0,top);
            if(!food)G1Column(x,z,G1F1,G1Roof-.9f);
            Col(x-.4f,x+.4f,G1F0,G1Roof-.9f,z-.4f,z+.4f,"słup");
        }
        // ---- the plinth (dark granite) round the whole building, below the floor level, but not across the doors
        foreach(int side in new[]{0,1,2,3})
        {
            float L=side<2?G1HW:G1HD;
            var doors=G1DoorsOn(side);
            float u=-L;
            foreach(var (a,b) in doors){if(a-.02f>u)FB(gStone,side,u,a-.02f,-.10f,G1F0-.012f,-.05f,.10f,true);u=Mathf.Max(u,b+.02f);}
            if(u<L)FB(gStone,side,u,L,-.10f,G1F0-.012f,-.05f,.10f,true);   // outside the floors (they end at the slab, o -.06), 10 cm into the ground
        }
        G1North();G1South();G1West();G1East();
        G1EntranceBlock();
        // corner trims (black anodised angles) at the four corners, full height
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
            B(frame,Mathf.Min(sx*G1HW,sx*(G1HW+.07f)),Mathf.Max(sx*G1HW,sx*(G1HW+.07f)),0f,G1Top,Mathf.Min(sz*G1HD,sz*(G1HD+.07f)),Mathf.Max(sz*G1HD,sz*(G1HD+.07f)));
        // parapet cappings (black anodised) on the screen walls
        foreach(int side in new[]{0,1,2,3}){float L=side<2?G1HW:G1HD;FB(frame,side,-L,L,G1Top,G1Top+.08f,-.34f,.10f);}
    }
    // P1 openings: the atrium void, the entrance hall, the food court, the cinema wing (halls and lobby double height)
    static List<(float x0,float x1,float z0,float z1)> G1P1Holes()
    {
        return new List<(float,float,float,float)>{
            (-G1VoidX,G1VoidX,G1SouthMallZ1,G1NorthMallZ0),        // atrium void between the two bridges
            (-20f,20f,G1NorthMallZ1,G1HD),                           // the entrance hall (to the roof)
            (-G1FoodX,G1FoodX,-G1HD+.27f,G1FoodZ1),                // food court (double height; the façade backing's strip kept)
            (-G1HW,G1CinemaX1,-G1HD,G1HD),                          // the cinema wing
        };
    }
    // the door openings along each side (u ranges), for the plinth and the façades
    static List<(float a,float b)> G1DoorsOn(int side)
    {
        switch(side)
        {
            case 0:return new List<(float,float)>{(-74.2f,-65.8f),(-12f,12f),(61.8f,70.2f)};
            case 1:return new List<(float,float)>{(-4.2f,4.2f)};
            case 3:return new List<(float,float)>{(-52f,-38f),(-12.2f,-7.8f)};
            default:return new List<(float,float)>{};
        }
    }

    // ---- NORTH (the front): cinema block | shops | main entrance block | shops | BRAXHORN CITY block
    static void G1North()
    {
        const int s=0;
        // cinema block u -85..-55: lobby glazing P0 u -82..-58 with the entrance (u -74.2..-65.8), dark above
        Glazed(s,-82f,-78f,G1F0+.05f,GlassTop0);Glazed(s,-78f,-74.2f,G1F0+.05f,GlassTop0,true,false);
        Glazed(s,-65.8f,-62f,G1F0+.05f,GlassTop0,false,true);Glazed(s,-62f,-58f,G1F0+.05f,GlassTop0);
        FB(frame,s,-74.2f,-65.8f,GlassTop0-.035f,GlassTop0+.035f,-.25f,.03f);
        Glazed(s,-74.2f,-65.8f,3.2f,GlassTop0,false,false);
        G1EntranceDoors(s,-70f,"kino");
        Cassettes(s,-84.94f,-82f,0f,G1Top);Cassettes(s,-58f,-55.45f,0f,G1Top);
        Cassettes(s,-82f,-58f,GlassTop0+.035f,G1Top);
        FCol(s,-85f,-55f,G1F0,G1Top);
        // shops west u -55..-20 (7 bays), east u 20..55
        foreach(float g in new[]{-1f,1f})
        {
            float a=g<0?-55f:20f;
            for(int i=0;i<7;i++){float u0=a+i*5f;ShopBay(s,u0,u0+5f);}
            Cassettes(s,a+.45f,a+35f-.45f,G1Roof,G1Top-.8f);
            FB(gLight,s,a,a+35f,G1Top-.8f,G1Top,.05f,.45f);                  // cornice
        }
        foreach(float u in new[]{-55f,-45f,-35f,-25f,25f,35f,45f,55f})Pilaster(s,u);
        foreach(float u in new[]{-20.45f,20.45f})Pilaster(s,u);
        foreach(float g in new[]{-1f,1f}){float a=g<0?-55f:20f;FB(gLight,s,a,a+35f,G1Roof-.18f,G1Roof+.5f,.05f,.32f);}
        foreach(float u in new[]{-22f,22f})Fin(s,u);
        // BRAXHORN CITY block u 55..85: the anchor's shop windows P0 (u 57.5..82.5) with its entrance, dark above (the mark)
        for(float u=57.5f;u<82.5f-.1f;u+=5f)
        {
            if(u+5f>61.8f&&u<70.2f){continue;}
            Glazed(s,u,u+5f,G1F0+.05f,GlassTop0);
        }
        Glazed(s,57.5f,61.8f,G1F0+.05f,GlassTop0,true,false);Glazed(s,70.2f,72.5f,G1F0+.05f,GlassTop0,false,true);
        FB(frame,s,61.8f,70.2f,GlassTop0-.035f,GlassTop0+.035f,-.25f,.03f);
        Glazed(s,61.8f,70.2f,3.2f,GlassTop0,false,false);
        G1EntranceDoors(s,66f,"anchor");
        Cassettes(s,55.45f,57.5f,0f,G1Top);Cassettes(s,82.5f,84.94f,0f,G1Top);
        Cassettes(s,57.5f,82.5f,GlassTop0+.035f,G1Top);
        FCol(s,55f,85f,G1F0,G1Top);
        Fin(s,84.4f);Fin(s,-84.4f);
        // canopies over the cinema and the anchor entrances
        G1Canopy(s,-70f,9.0f,3.0f,4.6f);G1Canopy(s,66f,9.0f,3.0f,4.6f);
    }
    // ---- SOUTH (service + the food court's windows + the south entrance)
    static void G1South()
    {
        const int s=1;
        Cassettes(s,-84.94f,-30f,0f,G1Top);Cassettes(s,30f,84.94f,0f,G1Top);
        for(float u=-30f;u<30f-.1f;u+=5f)
        {
            if(u+5f>-4.2f&&u<4.2f)continue;
            TallBay(s,u,u+5f);
        }
        // the south entrance (u -4.2..4.2) double height glazing over the doors
        Glazed(s,-5f,-4.2f,G1F0+.05f,GlassTop1);Glazed(s,4.2f,5f,G1F0+.05f,GlassTop1);
        Glazed(s,-4.2f,4.2f,3.2f,GlassTop1);
        Cassettes(s,-5f,5f,GlassTop1+.035f,G1Roof);
        G1EntranceDoors(s,0f,"południe");
        Cassettes(s,-30f,30f,G1Roof,G1Top-.8f);FB(gLight,s,-30f,30f,G1Top-.8f,G1Top,.05f,.45f);
        foreach(float u in new[]{-75f,-60f,-45f,-30f,-15f,15f,30f,45f,60f,75f})Pilaster(s,u);
        FB(gLight,s,-84.9f,84.9f,G1Roof-.18f,G1Roof+.5f,.05f,.32f);
        foreach(float u in new[]{-52.5f,52.5f})Fin(s,u);
        G1Canopy(s,0f,10f,3.0f,4.6f);
        // the fire escape stairs from the P1 exits at u -62 / 62 (black steel, functional)
        foreach(float u in new[]{-66f,58f})G1FireStair(u);
    }
    // ---- WEST (the cinema): dark, the lobby's double-height glazing at the north end, red fins, KINO MULTIPLEX
    static void G1West()
    {
        const int s=2;
        Cassettes(s,-54.94f,26f,0f,G1Top);
        for(float u=26f;u<51f-.1f;u+=5f)TallBay(s,u,u+5f);
        Cassettes(s,51f,54.94f,0f,G1Top);
        Cassettes(s,26f,51f,G1Roof,G1Top);
        // the KINO MULTIPLEX field (u -50..10) is kept clear of pilasters and fins: the letters stand on the cassettes
        foreach(float u in new[]{-51.5f,26f,51f})Pilaster(s,u);
        FB(gLight,s,-54.9f,54.9f,G1Roof-.18f,G1Roof+.5f,.05f,.32f);
        foreach(float u in new[]{15f})Fin(s,u);
    }
    // ---- EAST (the anchor: glazed; the entertainment: some glazing, its side entrance; the docks)
    static void G1East()
    {
        const int s=3;
        for(float u=12f;u<52f-.1f;u+=5f)ShopBay(s,u,u+5f);
        Cassettes(s,52f,54.94f,0f,G1Top);Cassettes(s,12f,52f,G1Roof,G1Top);
        // BRAXHORN HOME (the furniture store): its windows P0 u -35..-15, the side entrance u -12.2..-7.8
        for(float u=-35f;u<-15f-.1f;u+=5f){Glazed(s,u,u+5f,G1F0+.05f,GlassTop0);Cassettes(s,u+.035f,u+5f-.035f,GlassTop0+.035f,G1Top);FCol(s,u,u+5f,G1F0,GlassTop0);}
        Cassettes(s,-15f,-12.2f,0f,G1Top);Cassettes(s,-7.8f,12f,0f,G1Top);
        Glazed(s,-12.2f,-7.8f,3.2f,GlassTop0);Cassettes(s,-12.2f,-7.8f,GlassTop0+.035f,G1Top);
        G1EntranceDoors(s,-10f,"salon meblowy",2.2f);
        G1Canopy(s,-10f,7f,2.6f,4.6f);
        // the loading docks u -52..-38: three dock doors with seals, levellers, bumpers, lights and a canopy
        Cassettes(s,-54.94f,-51.5f,0f,G1Top);Cassettes(s,-38f,-35f,0f,G1Top);
        for(int i=0;i<3;i++)G1Dock(-51.5f+i*4.5f);
        Cassettes(s,-51.5f,-38f,4.7f,G1Top);
        FB(frame,s,-52.5f,-37.5f,4.55f,4.75f,.05f,3.2f);                       // dock canopy
        foreach(float u in new[]{-35f,-25f,-15f,12f,22f,32f,42f,52f})Pilaster(s,u);
        FB(gLight,s,-54.9f,54.9f,G1Roof-.18f,G1Roof+.5f,.05f,.32f);
        foreach(float u in new[]{-36.7f,0f,27f,47f})Fin(s,u);   // owner 19:47: no fin across the docks (was u -45)
    }
    static void G1Dock(float u0)
    {
        const int s=3;float u1=u0+3.5f,h=4.2f,yd=1.2f;                           // dock floor at truck bed height 1.2 m over the yard
        Cassettes(s,u0+3.5f,u0+4.5f,0f,4.7f);
        FB(gStone,s,u0,u1,0f,yd,-.30f,.06f,true);                                 // dock wall
        FB(gCinemaDark,s,u0+.05f,u1-.05f,yd,h,-.30f,-.25f,true);                 // the sectional door (closed, black)
        for(float y=yd+.5f;y<h-.1f;y+=.5f)FB(frame,s,u0+.05f,u1-.05f,y-.01f,y+.01f,-.25f,-.23f);
        // the dock seal (black rubber frame), bumpers, the leveller lip, a dock light
        FB(rubber,s,u0-.25f,u0,yd,h+.25f,0f,.35f);FB(rubber,s,u1,u1+.25f,yd,h+.25f,0f,.35f);FB(rubber,s,u0-.25f,u1+.25f,h,h+.25f,0f,.35f);
        foreach(float u in new[]{u0+.35f,u1-.35f})FB(rubber,s,u-.15f,u+.15f,yd-.45f,yd-.05f,.06f,.18f);
        FB(paintYellow,s,u0+.1f,u1-.1f,yd-.04f,yd,.06f,.30f);
        m.Cylinder(led,FP(s,u1+.6f,yd+2.2f,.15f),.07f,.02f,12,Quaternion.Euler(0,0,-90),true);
    }
    // the windbreak lobby doors (automatic sliding, glass leaves in black frames, sliding behind fixed glass): an outer pair
    // in the façade, an inner pair 3.5 m in; the windbreak's glass side screens, its ceiling, a mat. half: the façade opening
    static void G1EntranceDoors(int side,float u,string label,float half=4.2f)
    {
        float y=G1F0,ws=Mathf.Min(2.6f,half-.05f),w=Mathf.Min(1.2f,(ws-.15f)*.5f);
        float yaw=side==0?0f:side==1?180f:side==2?-90f:90f;
        // the façade: fixed glass beside the doors (the leaves slide behind it), the transom
        FB(frame,side,u-half,u+half,3.1f,3.2f,-.25f,.03f);
        Glazed(side,u-half,u-w,y+.05f,3.1f,true,false);Glazed(side,u+w,u+half,y+.05f,3.1f,false,true);
        SlidingGlassDoor("G1 "+label+" drzwi",FP(side,u,y,-.18f),yaw,w,2.6f);
        // the inner line 3.5 m in: fixed glass from the doors to the side screens, the doors
        foreach(float g in new[]{-1f,1f})
        {
            var a=FP(side,u+g*w,y+.05f,-3.6f);var b=FP(side,u+g*ws,y+.05f,-3.6f);
            Q(glass,a,b,b+Vector3.up*2.95f,a+Vector3.up*2.95f,FOut(side));Q(glass,a,b,b+Vector3.up*2.95f,a+Vector3.up*2.95f,-FOut(side));
        }
        FB(frame,side,u-ws,u+ws,3.0f,3.1f,-3.65f,-3.55f);
        SlidingGlassDoor("G1 "+label+" drzwi wewnętrzne",FP(side,u,y,-3.68f),yaw,w,2.6f);
        // the windbreak's side screens (glass, black frames), its ceiling, the mat
        foreach(float g in new[]{-1f,1f})
        {
            var a=FP(side,u+g*ws,y,-.10f);var b=FP(side,u+g*ws,y,-3.6f);
            var p0=new Vector3(a.x,y,a.z);var p1=new Vector3(b.x,y,b.z);var nrm=Vector3.Cross(p1-p0,Vector3.up);
            Q(glass,p0,p1,p1+Vector3.up*3.0f,p0+Vector3.up*3.0f,nrm);Q(glass,p0,p1,p1+Vector3.up*3.0f,p0+Vector3.up*3.0f,-nrm);
            B(frame,Mathf.Min(a.x,b.x)-.03f,Mathf.Max(a.x,b.x)+.03f,3.0f,3.1f,Mathf.Min(a.z,b.z)-.03f,Mathf.Max(a.z,b.z)+.03f);
            Col(Mathf.Min(a.x,b.x)-.05f,Mathf.Max(a.x,b.x)+.05f,y,3.0f,Mathf.Min(a.z,b.z)-.05f,Mathf.Max(a.z,b.z)+.05f,"wiatrołap");
            var f0=FP(side,u+g*w,y,-3.6f);var f1=FP(side,u+g*ws,y,-3.6f);
            Col(Mathf.Min(f0.x,f1.x)-.05f,Mathf.Max(f0.x,f1.x)+.05f,y,3.0f,Mathf.Min(f0.z,f1.z)-.05f,Mathf.Max(f0.z,f1.z)+.05f,"szyba wiatrołapu");
            var e0=FP(side,u+g*w,y,-.10f);var e1=FP(side,u+g*half,y,-.10f);
            Col(Mathf.Min(e0.x,e1.x)-.05f,Mathf.Max(e0.x,e1.x)+.05f,y,3.1f,Mathf.Min(e0.z,e1.z)-.05f,Mathf.Max(e0.z,e1.z)+.05f,"szyba");
        }
        var c0=FP(side,u-ws,3.1f,-.25f);var c1=FP(side,u+ws,3.2f,-3.6f);
        B(gWood,Mathf.Min(c0.x,c1.x),Mathf.Max(c0.x,c1.x),3.1f,3.2f,Mathf.Min(c0.z,c1.z),Mathf.Max(c0.z,c1.z));
        var m0=FP(side,u-ws+.1f,y,-.25f);var m1=FP(side,u+ws-.1f,y,-3.45f);
        Flat(rubber,Mathf.Min(m0.x,m1.x),Mathf.Max(m0.x,m1.x),Mathf.Min(m0.z,m1.z),Mathf.Max(m0.z,m1.z),y+.012f);
    }
    // automatic sliding glass door at c (floor level), facing yaw: two leaves w each, apart by w (the train's runtime)
    static void SlidingGlassDoor(string label,Vector3 c,float yaw,float w,float h)
    {
        var d=Group(root,label+" (automatyczne, rozsuwane)");
        d.localPosition=c*K;d.localRotation=Quaternion.Euler(0,yaw,0);
        var leaves=new Transform[2];var outs=new Vector3[2];var slides=new Vector3[2];
        for(int i=0;i<2;i++)
        {
            float side=i==0?-1f:1f;
            var t=Group(d,"skrzydło "+(i+1));t.localPosition=new Vector3(side*w*.5f,0,0)*K;
            var lm=new BraxChampionsMeshBatch();
            float hw=w*.5f;
            lm.BoxMinMax(frame,-hw,hw,0f,.10f,-.025f,.025f);lm.BoxMinMax(frame,-hw,hw,h-.08f,h,-.025f,.025f);
            lm.BoxMinMax(frame,-hw,-hw+.05f,.10f,h-.08f,-.025f,.025f);lm.BoxMinMax(frame,hw-.05f,hw,.10f,h-.08f,-.025f,.025f);
            var g0=new Vector3(-hw+.04f,.09f,0f);var g1=new Vector3(hw-.04f,.09f,0f);var g2=new Vector3(hw-.04f,h-.07f,0f);var g3=new Vector3(-hw+.04f,h-.07f,0f);
            Quad2(lm,glass,g0,g1,g2,g3);
            lm.BoxMinMax(frame,-hw+.06f,hw-.06f,.98f,1.02f,-.03f,-.025f);       // the push bar line (black)
            lm.Build(t,"skrzydło szklane",Folder,false);
            BraxChampionsMeshBatch.Collider(t,"skrzydło",new Vector3(0,h*.5f,0),new Vector3(w,h,.06f));
            leaves[i]=t;outs[i]=Vector3.zero;slides[i]=new Vector3(side*w*.98f,0,0)*K;
        }
        // the door's own head box (black) with the sensor
        var hm=new BraxChampionsMeshBatch();hm.BoxMinMax(frame,-w-.05f,w+.05f,h,h+.10f,-.08f,.08f);hm.BoxMinMax(led,-.10f,.10f,h-.012f,h,-.06f,-.04f);hm.Build(d,"napęd drzwi",Folder,false);
        var c2=d.gameObject.AddComponent<BraxChampionsD2DrzwiPociagu>();
        c2.skrzydla=leaves;c2.wysuw=outs;c2.przesuw=slides;c2.promien=2.2f*K;c2.czas=1.1f;
        Doors++;
    }
    // a cantilevered canopy over an entrance: black steel frame, glass panes (black frames), LED downlights; centre u,
    // width w, depth d, at height y
    static void G1Canopy(int side,float u,float w,float d,float y,float ob=0f)
    {
        if(ob>0f){G1CanopyAt(side,u,w,d,y,ob);return;}
        G1CanopyAt(side,u,w,d,y,0f);
    }
    static void G1CanopyAt(int side,float u,float w,float d,float y,float ob)
    {
        d+=ob;
        float b0=ob+.05f,b1=ob+.25f;
        FB(frame,side,u-w*.5f,u+w*.5f,y,y+.30f,b0,b1);                          // the back beam on the façade
        FB(frame,side,u-w*.5f,u+w*.5f,y+.06f,y+.18f,d-.15f,d);                  // the front edge
        for(int i=0;i<=4;i++){float uu=u-w*.5f+w*i/4f;FB(frame,side,uu-.06f,uu+.06f,y+.04f,y+.20f,b1,d-.15f);}
        for(int i=0;i<4;i++){float a=u-w*.5f+w*i/4f+.06f,b=u-w*.5f+w*(i+1)/4f-.06f;var p0=FP(side,a,y+.21f,b1);var p1=FP(side,b,y+.21f,b1);var p2=FP(side,b,y+.21f,d-.15f);var p3=FP(side,a,y+.21f,d-.15f);Q(glass,p0,p1,p2,p3,Vector3.up);Q(glass,p0,p1,p2,p3,Vector3.down);}
        for(int i=0;i<4;i++){var p=FP(side,u-w*.375f+w*.25f*i,y+.03f,(b1+d)*.5f);m.Cylinder(ledWarm,p,.08f,.012f,16,Quaternion.Euler(180,0,0),true,false);}
        // tension rods to the façade
        foreach(float g in new[]{-1f,1f})m.Tube(frame,FP(side,u+g*w*.45f,y+2.6f,b0),FP(side,u+g*w*.45f,y+.2f,d-.2f),.02f,8);
    }
    // ---- the main entrance block (north, u -20..20, projecting 2.5 m): the glass curtain wall to 20.5 over the windbreak,
    // glass side returns, the sign band (cassettes) to 24, its roof; behind it the triple-height hall to the vault
    static void G1EntranceBlock()
    {
        const int s=0;float o=G1HallFront;
        float[] ys={G1F0+.05f,3.2f,6.0f,9.2f,12.5f,16.5f,G1HallTop};
        for(float u=-20f;u<20f-.1f;u+=2.5f)
            for(int k=0;k+1<ys.Length;k++)
            {
                bool door=k==0&&u+2.5f>-12f&&u<12f;if(door)continue;
                FGlass(glassDark,s,u,u+2.5f,ys[k],ys[k+1],o-.10f);
                FB(frame,s,u+.04f,u+2.46f,ys[k+1]-.04f,ys[k+1]+.04f,o-.25f,o+.04f);
            }
        for(float u=-20f;u<=20.01f;u+=2.5f)FB(frame,s,u-.045f,u+.045f,u>-12.1f&&u<12.1f?3.2f:ys[0],G1HallTop,o-.30f,o+.06f);
        // doors: three pairs of automatic sliding doors in the curtain wall (u -8, 0, 8), glass between them to 3.2
        foreach(float uc in new[]{-8f,0f,8f})SlidingGlassDoor("G1 wejście główne "+uc.ToString("0"),FP(s,uc,G1F0,o-.18f),0f,1.2f,2.8f);
        foreach(var (a,b) in new[]{(-12f,-9.2f),(-6.8f,-1.2f),(1.2f,6.8f),(9.2f,12f)})
        {FGlass(glassDark,s,a,b,ys[0],3.0f,o-.10f);FB(frame,s,a-.03f,a+.03f,ys[0],3.2f,o-.25f,o+.04f);FB(frame,s,b-.03f,b+.03f,ys[0],3.2f,o-.25f,o+.04f);Col(a,b,G1F0,3.0f,G1HD+o-.3f,G1HD+o,"szyba");}
        FB(frame,s,-12f,12f,3.0f,3.2f,o-.25f,o+.04f);
        Col(-20f,-12f,G1F0,G1HallTop,G1HD+o-.3f,G1HD+o,"ściana szklana");Col(12f,20f,G1F0,G1HallTop,G1HD+o-.3f,G1HD+o,"ściana szklana");Col(-12f,12f,3.0f,G1HallTop,G1HD+o-.3f,G1HD+o,"ściana szklana");
        // glass side returns (x = +-20, z 55..57.5)
        foreach(float sx in new[]{-1f,1f})
        {
            float x=sx*20f;
            for(int k=0;k+1<ys.Length;k++)
            {
                var a=new Vector3(x,ys[k],G1HD);var b=new Vector3(x,ys[k],G1HD+o-.10f);var c=new Vector3(x,ys[k+1],G1HD+o-.10f);var d=new Vector3(x,ys[k+1],G1HD);
                Q(glassDark,a,b,c,d,Vector3.right);Q(glassDark,a,b,c,d,Vector3.left);
                B(frame,x-.04f,x+.04f,ys[k+1]-.04f,ys[k+1]+.04f,G1HD,G1HD+o-.10f);
            }
            B(frame,x-.05f,x+.05f,0f,G1HallTop,G1HD,G1HD+.08f);
            Col(x-.1f,x+.1f,G1F0,G1HallTop,G1HD,G1HD+o,"ściana szklana");
        }
        // the sign band 20.5..24 (front and returns), the block's roof (flat, membrane), its granite step
        Cassettes(s,-20f,20f,G1HallTop+.5f,G1Top);                                   // the block's back, above the hall roof slab
        for(int i=0;i<16;i++){float u0=-20f+i*2.5f+.008f,u1=-20f+(i+1)*2.5f-.008f;for(int j=0;j<2;j++)FB(gDark,s,u0,u1,G1HallTop+j*1.75f+.008f,G1HallTop+(j+1)*1.75f-.008f,o,o+.05f);}
        B(gCinemaDark,-20f,20f,G1HallTop,G1Top,G1HD+o-.30f,G1HD+o);
        foreach(float sx in new[]{-1f,1f}){float x=sx*20f;B(gCinemaDark,Mathf.Min(x+sx*.02f,x+sx*.30f),Mathf.Max(x+sx*.02f,x+sx*.30f),G1HallTop,G1Top-.015f,G1HD+.08f,G1HD+o);}
        B(gRoof,-20.02f,20.02f,G1Top-.40f,G1Top-.015f,G1HD,G1HD+o-.30f);   // between the side pieces and the front band
        B(frame,-20.32f,20.32f,G1Top,G1Top+.08f,G1HD+o-.34f,G1HD+o+.10f);
        B(gStone,-20.02f,20.02f,-.10f,G1F0-.01f,G1HD+.01f,G1HD+o-.10f,true);       // under the hall floor (10 mm), over the square (10 mm)
        // the hall above the main roof: side walls x +-20 (z 45..55, 12.5..20.5), the back wall round the vault, the roof
        foreach(float sx in new[]{-1f,1f}){float x=sx*20f;B(gDark,Mathf.Min(x-sx*.02f,x+sx*.30f),Mathf.Max(x-sx*.02f,x+sx*.30f),G1Roof-.88f,G1HallTop,G1HallZ0,G1HD);}   // 2 cm proud of the roof slab's edge
        {
            const int n=24;float zb=G1HallZ0;
            for(int i=0;i<n;i++)
            {
                float x0=-G1VaultR+2*G1VaultR*i/n,x1=-G1VaultR+2*G1VaultR*(i+1)/n;
                float y0=VaultH(x0),y1=VaultH(x1);
                Q(gWhitePanel,new Vector3(x0,y0,zb),new Vector3(x1,y1,zb),new Vector3(x1,G1HallTop,zb),new Vector3(x0,G1HallTop,zb),Vector3.forward);
                Q(gDark,new Vector3(x0,y0,zb-.05f),new Vector3(x1,y1,zb-.05f),new Vector3(x1,G1HallTop,zb-.05f),new Vector3(x0,G1HallTop,zb-.05f),Vector3.back);
            }
            foreach(float sx in new[]{-1f,1f})B(gWhitePanel,Mathf.Min(sx*G1VaultR,sx*20f),Mathf.Max(sx*G1VaultR,sx*20f),G1Roof,G1HallTop,zb-.30f,zb);
        }
        Slab(gRoof,-20.3f,20.3f,G1HallZ0-.3f,G1HD,G1HallTop,G1HallTop+.5f,new List<(float,float,float,float)>(),false);
        B(gDark,-20.3f,20.3f,G1HallTop+.5f,G1Top,G1HallZ0-.3f,G1HallZ0);           // the band's back above the hall roof
        // the canopy over the main doors (deep, 14 m wide) at 4.4 m
        G1Canopy(s,0f,26f,3.5f,4.4f,o);
    }
    static float VaultH(float x)=>G1Roof+G1VaultRise*Mathf.Sqrt(Mathf.Max(0f,1f-(x/G1VaultR)*(x/G1VaultR)));
    // the fire escape stair on the south façade: from the P1 exit door (u..u+8) down to the walk, black steel, glass rail
    static void G1FireStair(float u)
    {
        const int s=1;float top=G1F1,w=1.4f,o0=.6f,o1=o0+w;
        // landing at the door, a straight flight along the façade down to the walk, landing at the bottom
        FB(frame,s,u,u+2f,top-.25f,top,o0,o1,true);
        int n=Mathf.CeilToInt((top-WalkY)/.18f);float rise=(top-WalkY)/n,run=.28f;
        for(int i=0;i<n-1;i++){float uu=u+2f+i*run,y=top-(i+1)*rise;FB(frame,s,uu,uu+run+.02f,y-.04f,y,o0+.05f,o1-.05f);}   // the last riser lands on the walk
        float ue=u+2f+n*run;
        foreach(float o in new[]{o0+.03f,o1-.03f})
        {
            var a=FP(s,u+2f,top-.3f,o);var b=FP(s,ue,WalkY-.3f+rise,o);Beam(frame,a,b,.06f,.25f);
            var ga=FP(s,u+2f,top,o);var gb=FP(s,ue,WalkY+rise,o);GlassRail(ga,gb,1.0f,1.3f);
        }
        var c0=FP(s,u+2f,top,o0);var c1=FP(s,ue,WalkY,o1);
        GameObjectRamp("schody ewakuacyjne",c0,c1,w);
        // the P1 exit door (fire door, black) in the façade
        FB(frame,s,u+.3f,u+1.7f,top,top+2.2f,.05f,.08f);
        FB(gRed,s,u+.35f,u+1.65f,top+2.25f,top+2.4f,.05f,.10f);                  // the exit sign box (red)
    }
    static GameObject GameObjectRamp(string name,Vector3 a,Vector3 b,float w)
    {
        var o=new GameObject(name+" | kolizja");o.transform.SetParent(cols,false);
        var d=b-a;var mid=(a+b)*.5f;float len=new Vector2(d.x,d.z).magnitude;
        o.transform.localPosition=new Vector3(mid.x,mid.y-.15f,mid.z)*K;
        o.transform.localRotation=Quaternion.LookRotation(new Vector3(d.x,0,d.z).normalized,Vector3.up)*Quaternion.Euler(Mathf.Atan2(-d.y,len)*Mathf.Rad2Deg,0,0);
        o.AddComponent<BoxCollider>().size=new Vector3(w,.3f,Mathf.Sqrt(len*len+d.y*d.y))*K;Colliders++;
        return o;
    }

    // ================================================================ the vault
    // elliptical barrel vault (half span 10, rise 8) over x -10..10 from the food court (z -40) to the hall (z 45): black
    // arched ribs every 2.5 m, purlins on 12 divisions, glass panes between, a black kerb on the roof, the south gable glazed
    static void G1Vault()
    {
        const int n=16;float z0=G1VaultZ0,z1=G1HallZ0;
        Vector3 P(int i,float z){float t=Mathf.PI*i/n;float x=-G1VaultR*Mathf.Cos(t);return new Vector3(x,G1Roof+G1VaultRise*Mathf.Sin(t),z);}
        for(float z=z0;z<z1-.01f;z+=2.5f)
            for(int i=0;i<n;i++)
            {
                var a=P(i,z+.04f);var b=P(i+1,z+.04f);var c=P(i+1,z+2.46f);var d=P(i,z+2.46f);
                var nn=Vector3.Cross(b-a,d-a);if(nn.y<0)nn=-nn;
                Q(gVaultGlass,a,b,c,d,nn);Q(gVaultGlass,a,b,c,d,-nn);
            }
        for(float z=z0;z<=z1+.01f;z+=2.5f){float zz=Mathf.Min(z,z1-.11f);for(int i=0;i<n;i++)Beam(frame,P(i,zz),P(i+1,zz),.10f,.22f);}   // the last rib in front of the hall wall
        for(int i=1;i<n;i++)Beam(frame,P(i,z0),P(i,z1-.06f),.07f,.10f);
        foreach(float sx in new[]{-1f,1f})B(frame,Mathf.Min(sx*G1VaultR,sx*(G1VaultR+.4f)),Mathf.Max(sx*G1VaultR,sx*(G1VaultR+.4f)),G1Roof,G1Roof+.5f,z0,z1-.30f);   // into the hall wall
        // the south gable: glass in a fan of black mullions
        for(int i=0;i<n;i++)
        {
            var a=P(i,z0);var b=P(i+1,z0);var c=new Vector3(0f,G1Roof,z0);
            T(gVaultGlass,c,a,b,Vector3.back);T(gVaultGlass,c,a,b,Vector3.forward);
            Beam(frame,c,a,.06f,.10f);
        }
    }

    // ================================================================ signs
    static void G1Signs()
    {
        var lm=new BraxChampionsMeshBatch();
        // GALERIA GŁÓWNA over the main entrance (white, lit), the red LED line under it
        float zf=G1HD+G1HallFront+.05f+.002f;
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"GALERIA GŁÓWNA",new Vector3(0,G1HallTop+1.0f,zf),0f,2.0f,.25f,.12f,36f);
        B(gRed,-17f,17f,G1HallTop+.45f,G1HallTop+.57f,zf-.002f,zf+.05f);
        B(led,-17f,17f,G1HallTop+.47f,G1HallTop+.55f,zf+.05f,zf+.062f);
        // KINO MULTIPLEX on the cinema block (front) and on the west façade
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"KINO",new Vector3(-70f,14.6f,G1HD+.052f),0f,4.2f,.30f,.12f,26f);
        BraxChampionsLetters3D.Text(lm,gRedLit,gLetterSide,"MULTIPLEX",new Vector3(-70f,12.3f,G1HD+.052f),0f,1.5f,.18f,.14f,26f);
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"KINO MULTIPLEX",new Vector3(-G1HW-.052f,16.0f,-20f),-90f,3.2f,.25f,.12f,60f);
        B(led,-G1HW-.062f,-G1HW-.05f,15.4f,15.48f,-48f,8f);
        // BRAXHORN CITY (north-east block) with the owner's bull mark over it
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"BRAXHORN",new Vector3(70f,10.0f,G1HD+.052f),0f,2.2f,.22f,.12f,26f);
        BraxChampionsLetters3D.Text(lm,gRedLit,gLetterSide,"CITY",new Vector3(70f,7.4f,G1HD+.052f),0f,1.6f,.18f,.20f,26f);
        BraxChampionsLetters3D.Build(lm,root,"G1 napisy 3D");Signs+=4;
        G1Bull(new Vector3(70f,18.0f,G1HD+.05f));
    }
    // the owner's BRAXHORN bull mark (ArenaRoofBull.bytes, made by build_arena_roof_bull.py from the owner's mark) as a
    // 9 m relief on the north-east block: polished steel plates, graphite edges, the green LED glow; skipped if not made
    static void G1Bull(Vector3 c)
    {
        string src="Assets/Source/Mistrzowie/ArenaRoofBull.bytes";
        if(!System.IO.File.Exists(src)){Debug.LogWarning("G1: "+src+" missing - the bull mark on the façade skipped (CI makes it)");return;}
        var g=Group(root,"G1 znak BRAXHORN (byk właściciela, 9 m, płyty stalowe + LED)");
        g.localPosition=c*K;g.localRotation=Quaternion.Euler(0f,180f,0f);
        // the source is the 80 m roof mark lying with +Z up: stand it up (rotate -90 about x is in the roof's frame; here the
        // mark's plane is x / y facing +z after this group's turn)
        var inner=Group(g,"byk");inner.localRotation=Quaternion.Euler(-90f,0f,0f);inner.localScale=Vector3.one*(9f/80f);
        try
        {
            string json;
            using(var f=System.IO.File.OpenRead(src))using(var zip=new System.IO.Compression.GZipStream(f,System.IO.Compression.CompressionMode.Decompress))using(var r=new System.IO.StreamReader(zip))json=r.ReadToEnd();
            var data=JsonUtility.FromJson<BullFile>(json);
            if(data?.signs==null||data.signs.Length==0)return;
            var steel=S("g1_bull_polished_steel",new Color(.86f,.88f,.90f),.24f,.85f);var edge=S("g1_bull_graphite",new Color(.09f,.095f,.10f),.42f,.55f);
            var glow=S("g1_bull_green_led",new Color(.32f,.95f,.22f),.35f,0f,new Color(.40f,1.45f,.28f));
            foreach(var md in data.signs[0].meshes)
            {
                int count=md.positions==null?0:md.positions.Length/3;if(count==0||md.triangles==null)continue;
                var v=new Vector3[count];for(int i=0;i<count;i++)v[i]=new Vector3(md.positions[3*i],md.positions[3*i+1],md.positions[3*i+2])*K;
                var mesh=new Mesh{name="G1 bull "+md.role,indexFormat=count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
                mesh.vertices=v;mesh.triangles=md.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,Folder+"/g1_bull_"+md.role+".asset");
                var o=new GameObject("byk | "+md.role);o.transform.SetParent(inner,false);
                o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=md.role=="led"?glow:md.role=="steel_edge"?edge:steel;
            }
        }
        catch(Exception e){Debug.LogWarning("G1 bull mark: "+e.Message);}
    }
    [Serializable] sealed class BullFile{public float scaleK;public BullSign[] signs;}
    [Serializable] sealed class BullSign{public string id;public BraxChampionsFoundation.MeshData[] meshes;}

    // ================================================================ the roofscape (seen from the towers and from above)
    static void G1Roofscape()
    {
        // inner faces of the screen walls (12.5..24) are the black backing; walkways of light pavers, HVAC units with fans,
        // smaller skylights over the malls, the lift overruns
        foreach(var (x0,x1,z0,z1) in new[]{(-80f,-14f,-1.2f,1.2f),(14f,80f,-1.2f,1.2f),(-1.2f+50f,1.2f+50f,-50f,40f)})B(gRoofLight,x0,x1,G1Roof,G1Roof+.05f,z0,z1);
        foreach(var (x,z,w,d) in new[]{(-65f,-30f,6f,3.5f),(-65f,-10f,6f,3.5f),(-65f,10f,6f,3.5f),(-30f,10f,5f,3f),(30f,10f,5f,3f),(65f,30f,7f,4f),(65f,-20f,7f,4f),(-30f,-35f,5f,3f),(35f,-40f,5f,3f)})
        {
            B(gHvac,x-w*.5f,x+w*.5f,G1Roof,G1Roof+1.9f,z-d*.5f,z+d*.5f);
            for(int i=0;i<2;i++){var p=new Vector3(x-w*.25f+i*w*.5f,G1Roof+1.9f,z);m.Cylinder(frame,p,Mathf.Min(w,d)*.32f,.12f,24,Quaternion.identity,true,false);m.Cylinder(gCinemaDark,p+Vector3.up*.12f,Mathf.Min(w,d)*.27f,.02f,24,Quaternion.identity,true,false);}
            B(gHvac,x-w*.5f-.3f,x-w*.5f,G1Roof,G1Roof+.3f,z-d*.5f,z+d*.5f);
        }
        // skylights (glass pyramids on black frames) over the north and south malls
        foreach(var (x,z) in new[]{(-30f,34f),(30f,34f),(-30f,-19f),(30f,-19f)})
        {
            B(frame,x-3.1f,x+3.1f,G1Roof,G1Roof+.35f,z-2.1f,z+2.1f);
            var a=new Vector3(x-3f,G1Roof+.35f,z-2f);var b=new Vector3(x+3f,G1Roof+.35f,z-2f);var c=new Vector3(x+3f,G1Roof+.35f,z+2f);var d=new Vector3(x-3f,G1Roof+.35f,z+2f);var t=new Vector3(x,G1Roof+1.6f,z);
            foreach(var (p,q) in new[]{(a,b),(b,c),(c,d),(d,a)}){T(gVaultGlass,p,q,t,(p+q)*.5f-new Vector3(x,G1Roof,z));T(gVaultGlass,p,q,t,new Vector3(x,G1Roof,z)-(p+q)*.5f);Beam(frame,p,t,.05f,.05f);}
        }
        // lift overruns and the stair cores' roof exits
        foreach(var (x,z) in new[]{(-14f,0f),(14f,0f),(-44f,-50f),(44f,-50f)})B(gDark,x-1.6f,x+1.6f,G1Roof,G1Roof+3.0f,z-1.6f,z+1.6f);
    }
}

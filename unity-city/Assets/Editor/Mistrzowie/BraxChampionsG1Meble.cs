using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 BRAXHORN HOME (owner 11.11 18:26 "W galerii ma być tylko sklepy i kino", 18:27 "możesz zrobić jakiś salon meblowy ... ale
// zrób zajebiste meble"): the east wing P0 (x 47..85, z -38..10), entered from the south mall (x 47) and from the street (the
// east side entrance, z -12.2..-7.8). Herringbone oak parquet, timber slats on black felt overhead, walnut-clad round
// columns, room settings between the columns behind low partitions:
//   south (z -35..-25): S1 living (a cream bouclé corner sofa, round marble table, armchairs, arc lamp), S2 dining (a solid
//   oak table for 8, upholstered chairs, globe pendants, a fluted walnut sideboard), S3 bedroom (king bed with a channel-
//   tufted velvet headboard, bedside tables and lamps, a bench);
//   north (z -5..5): N1 living (a cognac leather sofa, armchairs, walnut table, bookcases), N2 home office (desk, office
//   chair, a library wall, a reading corner), N3 bedroom (linen bed);
//   the middle (z -12.6..-11.4): the armchair collection on a walnut podium (eight fabrics); the west: the cash desk under
//   the 3D BRAXHORN HOME letters and the lighting display (a pendant cluster over a round table).
// Every piece built here on a real structure (owner 11.11 rules: no blocks, rounded, upholstered, wood is wood, metal is
// metal, Poly Haven materials only): legs, frames, separate seat and back cushions, rolled arms, throw pillows; the
// sofas, chairs and beds are BraxSeat places. The goods-in (x 47..85, z -55..-38) is behind the store.
public static partial class BraxChampionsD2
{
    static Material hMarbleFloor,hWallWhite,hBoucle,hWoolGrey,hWoolCharcoal,hVelvetGreen,hVelvetRust,hLinen,hCotton,hTaupe,hCognac,hOakSolid,hParquet,hRugGrey,hRugBeige,hOpal,hShade,hConcrete;
    static void HomeMaterials()
    {
        if(hBoucle)return;
        hBoucle=Inside(Tex("g1_home_boucle_cream","curly_teddy_natural",.35f,new Color(1.02f,1.0f,.98f)),.85f);
        hWoolGrey=Inside(Tex("g1_home_wool_grey","poly_wool_herringbone",.45f,new Color(1.30f,1.30f,1.34f)),.85f);
        hWoolCharcoal=Inside(Tex("g1_home_wool_charcoal","poly_wool_herringbone",.45f,new Color(.55f,.55f,.58f)),.85f);
        hVelvetGreen=Inside(Tex("g1_home_velvet_emerald","velour_velvet",.5f,new Color(.22f,2.5f,1.25f)),.85f);
        hVelvetRust=Inside(Tex("g1_home_velvet_rust","velour_velvet",.5f,new Color(.85f,.90f,.45f)),.85f);
        hLinen=Inside(Tex("g1_home_linen","hessian_230",.4f,new Color(1.45f,1.45f,1.45f)),.85f);
        hCotton=Inside(Tex("g1_home_cotton_white","cotton_jersey",.5f,new Color(1.22f,1.30f,1.36f)),.85f);
        hTaupe=Inside(Tex("g1_home_satin_taupe","crepe_satin",.6f,new Color(.62f,.66f,.74f)),.85f);
        hCognac=Inside(Tex("g1_home_leather_cognac","brown_leather",.6f,new Color(1.55f,1.45f,1.35f)),.85f);
        hOakSolid=Inside(Tex("g1_home_oak_solid","oak_wood_planks",2.4f,new Color(1.05f,.98f,.92f)),.85f);
        hParquet=Inside(Tex("g1_home_parquet","herringbone_parquet",1.8f,new Color(1.0f,.95f,.90f)),.85f);
        // owner 11.11 23:46 ("nie wygląda premium ... podłoga zjebana, ściany zjebane"): the store floor in wide light oak planks
        // (calm, no small tiles, no stains), the walls bright warm-white plaster to the black ceiling
        hMarbleFloor=Inside(Tex("g1_home_oak_wide_planks","laminate_floor_02",2.2f,new Color(1.40f,1.34f,1.26f)),.92f);
        hWallWhite=Inside(Tex("g1_home_wall_white","white_stucco_02",2.0f,new Color(1.40f,1.38f,1.33f)),.92f);
        // owner 11.11 19:31-19:32 ("wszystko w jednym kolorze ... się zlewa", "zmień teksturę na słupy"): the columns in grey
        // polished microcement (not wood), the ceiling black
        hConcrete=Inside(Tex("g1_home_polished_concrete","concrete_floor_02",3.0f,new Color(.92f,.92f,.94f)),.85f);
        hRugGrey=Inside(Tex("g1_home_rug_grey","poly_wool_herringbone",.9f,new Color(1.05f,1.05f,1.10f)),.85f);
        hRugBeige=Inside(Tex("g1_home_rug_beige","terlenka",1.2f,new Color(1.05f,1.0f,.95f)),.85f);
        hOpal=S("g1_home_opal_glass",new Color(.98f,.95f,.88f),.15f,0f,new Color(1.25f,1.05f,.78f));
        hShade=Inside(Tex("g1_home_lamp_linen","hessian_230",.3f,new Color(1.70f,1.66f,1.60f)),.9f);
    }

    // ---- the frame of a piece: c on the floor at the middle of its FRONT edge, face = where it faces; P(a, h, d): a to its
    // right as seen from the front (u), h up, d back from the front edge; rot: local x = u, local z = face (front)
    struct HF
    {
        public Vector3 c,f,u;public Quaternion rot;
        public HF(Vector3 c,Vector3 face){face.y=0;face.Normalize();this.c=c;f=face;u=Vector3.Cross(Vector3.up,face).normalized;rot=Quaternion.LookRotation(face,Vector3.up);}
        public Vector3 P(float a,float h,float d)=>c+u*a+Vector3.up*h-f*d;
        public Quaternion Lean(float back,float roll=0f)=>rot*Quaternion.Euler(-back,0f,roll);
    }
    static void HCol(HF k,float a0,float a1,float d0,float d1,float h,string name)
    {
        var p0=k.P(a0,0f,d0);var p1=k.P(a1,0f,d1);
        Col(Mathf.Min(p0.x,p1.x),Mathf.Max(p0.x,p1.x),k.c.y,k.c.y+h,Mathf.Min(p0.z,p1.z),Mathf.Max(p0.z,p1.z),name);
    }
    // a turned, tapered leg (walnut / oak) or a slim black steel one
    static void HLeg(Material mat,Vector3 b,float h,float r0,float r1){m.Cylinder(mat,b,r0,h,14,Quaternion.identity,true,true,r1);}

    // ---- a sofa (and an armchair: W ~0.9): tapered legs, an upholstered base, rolled arms, a back frame, separate seat
    // cushions with a soft crown, plump back cushions leaning back, two throw pillows; chaise > 0 extends the end cushion on
    // side chaiseSide (+1 right, -1 left) forward into a chaise longue; each cushion a BraxSeat
    static void HSofa(Vector3 c,Vector3 face,float W,Material up,Material accent,Material leg,string label,float chaise=0f,int chaiseSide=1,float arm=.22f)
    {
        var k=new HF(c,face);float D=.98f;
        int n=Mathf.Max(1,Mathf.RoundToInt((W-2f*arm)/.78f));float cw=(W-2f*arm)/n;
        // legs (6 under a long sofa), the base
        var la=new List<float>{-W*.5f+.09f,W*.5f-.09f};if(W>2.0f)la.Add(0f);
        foreach(float a in la)foreach(float d in new[]{.09f,D-.09f})HLeg(leg,k.P(a,0f,d),.12f,.018f,.026f);
        if(chaise>0f){float ac=chaiseSide*(W*.5f-.09f);HLeg(leg,k.P(ac,0f,-chaise+.09f),.12f,.018f,.026f);}
        m.RoundedBox(up,k.P(0f,.21f,D*.5f),new Vector3(W-.02f,.18f,D-.02f),.05f,k.rot,2);
        // the back frame and the arms (rolled: a large radius)
        m.RoundedBox(up,k.P(0f,.58f,D-.11f),new Vector3(W,.58f,.22f),.09f,k.rot,3);
        foreach(float s in new[]{-1f,1f})
        {
            float ext=chaise>0f&&s==chaiseSide?chaise:0f;
            m.RoundedBox(up,k.P(s*(W*.5f-arm*.5f),.39f,(D-ext)*.5f),new Vector3(arm,.54f,D+ext),.10f,k.rot,3);
        }
        if(chaise>0f)m.RoundedBox(up,k.P(chaiseSide*(W*.5f-arm-cw*.5f),.21f,-chaise*.5f),new Vector3(cw,.18f,chaise),.05f,k.rot,2);
        // seat cushions (y .30 .. .46), back cushions (leaning 12 degrees), seats
        for(int i=0;i<n;i++)
        {
            float a=-W*.5f+arm+(i+.5f)*cw;bool ch=chaise>0f&&((chaiseSide>0&&i==n-1)||(chaiseSide<0&&i==0));
            float d0=ch?-chaise:0f,dl=D-.22f-d0;
            m.RoundedBox(up,k.P(a,.38f,d0+dl*.5f),new Vector3(cw-.012f,.16f,dl),.075f,k.rot,3);
            m.RoundedBox(up,k.P(a,.68f,D-.30f),new Vector3(cw-.02f,.46f,.21f),.095f,k.Lean(12f),3);
            var st=k.P(a,.46f,.40f);Seat(st.x,st.y,st.z,face,label);
        }
        // throw pillows at the ends (the accent fabric), turned a little
        if(W>1.3f)foreach(float s in new[]{-1f,1f})m.RoundedBox(accent,k.P(s*(W*.5f-arm-.27f),.64f,D-.46f),new Vector3(.46f,.44f,.13f),.065f,k.Lean(18f,s*7f),3);
        else m.RoundedBox(accent,k.P(0f,.60f,D-.45f),new Vector3(.44f,.32f,.12f),.06f,k.Lean(15f),3);
        float a0=-W*.5f,a1=W*.5f;
        HCol(k,a0,a1,0f,D,.88f,"sofa");
        if(chaise>0f)HCol(k,chaiseSide>0?W*.5f-arm-cw:a0,chaiseSide>0?a1:a0+arm+cw,-chaise,0f,.50f,"szezlong");
        Benches++;
    }
    // ---- a round coffee table: a bevelled stone top on a turned pedestal (walnut / dark granite)
    static void HCoffeeRound(Vector3 c,float r,float h,Material top,Material foot)
    {
        m.Cylinder(top,c+Vector3.up*(h-.04f),r-.012f,.012f,48,Quaternion.identity,false,true,r);
        m.Cylinder(top,c+Vector3.up*(h-.028f),r,.028f,48,Quaternion.identity,true,false,r-.006f);
        m.Lathe(foot,c,new[]{new Vector2(.24f*r/.55f,0f),new Vector2(.26f*r/.55f,.02f),new Vector2(.15f*r/.55f,.07f),new Vector2(.10f,h*.55f),new Vector2(.12f,h-.08f),new Vector2(.17f,h-.04f)},32,Quaternion.identity,true,false);
        Col(c.x-r,c.x+r,c.y,c.y+h,c.z-r,c.z+r,"stolik");
    }
    // ---- a rectangular coffee table: a walnut top with rounded edges, a black steel frame, a lower oak shelf
    static void HCoffeeRect(Vector3 c,Vector3 face,float L,float Wd,float h,Material top,Material shelf)
    {
        var k=new HF(c,face);
        m.RoundedBox(top,k.P(0f,h-.022f,Wd*.5f),new Vector3(L,.044f,Wd),.018f,k.rot,2);
        foreach(float a in new[]{-L*.5f+.07f,L*.5f-.07f})foreach(float d in new[]{.07f,Wd-.07f})m.RoundedBox(frame,k.P(a,(h-.044f)*.5f,d),new Vector3(.03f,h-.044f,.03f),.008f,k.rot,1);
        m.RoundedBox(shelf,k.P(0f,.12f,Wd*.5f),new Vector3(L-.10f,.025f,Wd-.10f),.01f,k.rot,1);
        HCol(k,-L*.5f,L*.5f,0f,Wd,h,"stolik");
    }
    // ---- a dining table: a solid oak top 5.5 cm with rounded edges, an apron, four tapered square legs
    static void HDiningTable(Vector3 c,Vector3 face,float L,float Wd,Material top,Material leg)
    {
        var k=new HF(c,face);float h=.76f;
        m.RoundedBox(top,k.P(0f,h-.0275f,Wd*.5f),new Vector3(L,.055f,Wd),.02f,k.rot,2);
        foreach(float d in new[]{.14f,Wd-.14f})m.RoundedBox(leg,k.P(0f,h-.10f,d),new Vector3(L-.30f,.09f,.035f),.01f,k.rot,1);
        foreach(float a in new[]{-L*.5f+.14f,L*.5f-.14f})m.RoundedBox(leg,k.P(a,h-.10f,Wd*.5f),new Vector3(.035f,.09f,Wd-.30f),.01f,k.rot,1);
        foreach(float a in new[]{-L*.5f+.13f,L*.5f-.13f})foreach(float d in new[]{.13f,Wd-.13f})
        {
            m.RoundedBox(leg,k.P(a,(h-.055f)*.5f+.02f,d),new Vector3(.075f,h-.095f,.075f),.012f,k.rot,1);
            m.RoundedBox(leg,k.P(a,.01f,d),new Vector3(.055f,.02f,.055f),.008f,k.rot,1);
        }
        HCol(k,-L*.5f,L*.5f,0f,Wd,h,"stół");
    }
    // ---- a dining chair: upholstered seat on an oak frame, tapered front legs, rear legs sweeping up into the back posts,
    // an upholstered curved back (three panels), stretchers; a BraxSeat
    static void HDiningChair(Vector3 c,Vector3 face,Material seat,Material wood,string label)
    {
        var k=new HF(c,face);
        m.RoundedBox(wood,k.P(0f,.425f,.23f),new Vector3(.46f,.045f,.44f),.012f,k.rot,1);
        m.RoundedBox(seat,k.P(0f,.475f,.225f),new Vector3(.47f,.07f,.45f),.03f,k.rot,3);
        foreach(float s in new[]{-1f,1f})
        {
            HLeg(wood,k.P(s*.20f,0f,.05f),.405f,.014f,.02f);
            m.Tube(wood,k.P(s*.20f,0f,.42f),k.P(s*.215f,.93f,.49f),.017f,10);
            m.Tube(wood,k.P(s*.20f,.13f,.05f),k.P(s*.20f,.13f,.42f),.010f,8);
        }
        m.Tube(wood,k.P(-.20f,.15f,.25f),k.P(.20f,.15f,.25f),.010f,8);
        // the back (owner 11.11 19:51: "tam gdzie się siedzi jest dłuższe niż oparcie"): as wide as the seat (.48) and 34 cm
        // tall - a centre panel and two angled wings
        m.RoundedBox(seat,k.P(0f,.76f,.47f),new Vector3(.25f,.34f,.055f),.025f,k.Lean(8f),3);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(seat,k.P(s*.178f,.76f,.452f),new Vector3(.13f,.34f,.055f),.025f,k.rot*Quaternion.Euler(-8f,-s*14f,0f),2);
        var st=k.P(0f,.51f,.22f);Seat(st.x,st.y,st.z,face,label);
        HCol(k,-.24f,.24f,0f,.50f,.90f,"krzesło");
    }
    // ---- a bed (d from the foot to the head): legs, an upholstered base, a mattress, the duvet with its turned-down fold
    // draping over the sides, a throw across the foot, sleeping and decorative pillows, a channel-tufted headboard
    static void HBed(Vector3 c,Vector3 face,float Wb,Material head,Material bed,Material accent,Material leg,string label)
    {
        var k=new HF(c,face);float Lb=2.0f;
        foreach(float a in new[]{-Wb*.5f,0f,Wb*.5f})foreach(float d in new[]{.05f,Lb})HLeg(leg,k.P(a*.96f,0f,d),.07f,.02f,.026f);
        m.RoundedBox(bed,k.P(0f,.20f,(Lb+.10f)*.5f-.05f),new Vector3(Wb+.12f,.26f,Lb+.10f),.05f,k.rot,2);
        m.RoundedBox(hCotton,k.P(0f,.45f,Lb*.5f),new Vector3(Wb,.24f,Lb-.04f),.07f,k.rot,3);
        // the duvet over the lower two thirds, its sides draping, the fold
        float dl=Lb-.55f;
        m.RoundedBox(hCotton,k.P(0f,.605f,dl*.5f-.03f),new Vector3(Wb+.10f,.07f,dl+.06f),.035f,k.rot,3);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(hCotton,k.P(s*(Wb*.5f+.035f),.49f,dl*.5f-.03f),new Vector3(.03f,.25f,dl+.04f),.014f,k.rot,2);
        m.RoundedBox(hCotton,k.P(0f,.645f,dl-.08f),new Vector3(Wb+.08f,.08f,.22f),.04f,k.rot,3);
        // the throw at the foot (the accent fabric)
        m.RoundedBox(accent,k.P(0f,.655f,.34f),new Vector3(Wb+.16f,.028f,.56f),.013f,k.rot,2);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(accent,k.P(s*(Wb*.5f+.072f),.50f,.34f),new Vector3(.026f,.30f,.56f),.012f,k.rot,2);
        // pillows
        foreach(float s in new[]{-1f,1f})m.RoundedBox(hCotton,k.P(s*Wb*.25f,.66f,Lb-.30f),new Vector3(Wb*.5f-.06f,.16f,.46f),.08f,k.Lean(22f),3);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(accent,k.P(s*Wb*.2f,.70f,Lb-.52f),new Vector3(.50f,.40f,.14f),.07f,k.Lean(16f,s*6f),3);
        m.RoundedBox(head,k.P(0f,.68f,Lb-.62f),new Vector3(.52f,.28f,.13f),.065f,k.Lean(12f),3);
        // the headboard: vertical channels on a backing panel
        float hw=Wb+.30f;int nc=Mathf.RoundToInt(hw/.15f);float cwid=hw/nc;
        m.RoundedBox(head,k.P(0f,.90f,Lb+.14f),new Vector3(hw+.02f,1.16f,.06f),.025f,k.rot,2);
        for(int i=0;i<nc;i++)m.RoundedBox(head,k.P(-hw*.5f+(i+.5f)*cwid,.90f,Lb+.07f),new Vector3(cwid-.008f,1.12f,.12f),.055f,k.rot,3);
        foreach(float s in new[]{-1f,1f}){var st=k.P(s*Wb*.32f,.57f,Lb*.42f);Seat(st.x,st.y,st.z,face,label);}
        HCol(k,-Wb*.5f-.08f,Wb*.5f+.08f,-.03f,Lb+.18f,.60f,"łóżko");
        HCol(k,-hw*.5f,hw*.5f,Lb+.02f,Lb+.18f,1.48f,"zagłówek");
    }
    // ---- a bedside table: walnut, two drawers (fronts 12 mm proud, black pulls), black steel legs, a lamp on it
    static void HNightstand(Vector3 c,Vector3 face,Material body)
    {
        var k=new HF(c,face);
        foreach(float a in new[]{-.21f,.21f})foreach(float d in new[]{.05f,.37f})m.RoundedBox(frame,k.P(a,.07f,d),new Vector3(.025f,.14f,.025f),.007f,k.rot,1);
        m.RoundedBox(body,k.P(0f,.35f,.22f),new Vector3(.50f,.42f,.40f),.02f,k.rot,2);
        foreach(float yy in new[]{.255f,.455f})
        {
            m.RoundedBox(body,k.P(0f,yy,.01f),new Vector3(.46f,.18f,.024f),.008f,k.rot,1);
            m.RoundedBox(frame,k.P(0f,yy+.04f,-.007f),new Vector3(.14f,.014f,.018f),.006f,k.rot,1);
        }
        HTableLamp(k.P(.06f,.56f,.24f),.42f,gStoneTop);
        HCol(k,-.25f,.25f,0f,.42f,.56f,"szafka nocna");
    }
    // ---- a table lamp: a turned stone (or walnut) base, a brass-free black stem, a linen drum shade lit inside
    static void HTableLamp(Vector3 b,float h,Material baseMat)
    {
        float s=h/.42f;
        m.Lathe(baseMat,b,new[]{new Vector2(.07f*s,0f),new Vector2(.085f*s,.03f*s),new Vector2(.10f*s,.12f*s),new Vector2(.075f*s,.20f*s),new Vector2(.02f*s,.24f*s)},28,Quaternion.identity,true,true);
        m.Cylinder(frame,b+Vector3.up*.24f*s,.007f*s,.06f*s,8,Quaternion.identity,true,false);
        m.Cylinder(ledWarm,b+Vector3.up*.27f*s,.022f*s,.05f*s,12,Quaternion.identity,true,true);
        m.Cylinder(hShade,b+Vector3.up*.25f*s,.17f*s,.17f*s,32,Quaternion.identity,false,false,.13f*s);
    }
    // ---- a sideboard: walnut body on black legs, a fluted front (half-round flutes), a stone top, decor on it
    static void HSideboard(Vector3 c,Vector3 face,float L,Material body,Material top)
    {
        var k=new HF(c,face);float h=.78f,Dp=.46f;
        foreach(float a in new[]{-L*.5f+.08f,L*.5f-.08f})foreach(float d in new[]{.08f,Dp-.08f})m.RoundedBox(frame,k.P(a,.08f,d),new Vector3(.03f,.16f,.03f),.008f,k.rot,1);
        m.RoundedBox(body,k.P(0f,(.16f+h-.03f)*.5f,Dp*.5f),new Vector3(L,h-.03f-.16f,Dp),.02f,k.rot,2);
        for(float a=-L*.5f+.04f;a<L*.5f-.03f;a+=.045f)m.Cylinder(body,k.P(a,.19f,.004f),.019f,h-.25f,8,Quaternion.identity,true,false);
        m.RoundedBox(top,k.P(0f,h-.015f,Dp*.5f),new Vector3(L+.02f,.03f,Dp+.02f),.01f,k.rot,2);
        HCol(k,-L*.5f,L*.5f,0f,Dp,h,"komoda");
    }
    // ---- a stone vase (turned), a walnut bowl
    static void HVase(Vector3 b,float h,Material mat)
    {
        m.Lathe(mat,b,new[]{new Vector2(.05f,0f),new Vector2(.09f,h*.25f),new Vector2(.10f,h*.5f),new Vector2(.06f,h*.85f),new Vector2(.035f,h*.92f),new Vector2(.045f,h)},24,Quaternion.identity,true,false);
    }
    // ---- an arc floor lamp: a stone base, a brushed steel arc over to the lit dome
    static void HArcLamp(Vector3 b,Vector3 toward,float reach)
    {
        toward.y=0;toward.Normalize();
        m.RoundedBox(gStoneTop,b+Vector3.up*.04f,new Vector3(.34f,.08f,.34f),.02f,Quaternion.LookRotation(toward),2);
        Vector3 Arc(float t){float a=t*Mathf.PI*.5f;return b+Vector3.up*(.08f+1.95f*Mathf.Sin(a)+ .10f*t)+toward*(reach*(1f-Mathf.Cos(a)));}
        for(int i=0;i<14;i++)m.Tube(stainless,Arc(i/14f),Arc((i+1)/14f),.013f,10);
        var e=Arc(1f);var shadeTop=e+toward*.02f-Vector3.up*.05f;
        m.Lathe(frame,shadeTop-Vector3.up*.20f,new[]{new Vector2(.25f,0f),new Vector2(.23f,.06f),new Vector2(.17f,.14f),new Vector2(.06f,.20f),new Vector2(.015f,.21f)},32,Quaternion.identity,false,true);
        m.Cylinder(ledWarm,shadeTop-Vector3.up*.21f,.16f,.012f,24,Quaternion.identity,false,true);
        Col(b.x-.17f,b.x+.17f,b.y,b.y+2.1f,b.z-.17f,b.z+.17f,"lampa");
    }
    // ---- a reading floor lamp: a disc base, a steel stem, a linen drum
    static void HFloorLamp(Vector3 b)
    {
        m.Cylinder(frame,b,.16f,.025f,24,Quaternion.identity,true,false,.15f);
        m.Cylinder(stainless,b+Vector3.up*.025f,.012f,1.40f,10,Quaternion.identity,true,false);
        m.Cylinder(ledWarm,b+Vector3.up*1.38f,.03f,.06f,12,Quaternion.identity,true,true);
        m.Cylinder(hShade,b+Vector3.up*1.34f,.22f,.30f,32,Quaternion.identity,false,false,.19f);
        Col(b.x-.16f,b.x+.16f,b.y,b.y+1.65f,b.z-.16f,b.z+.16f,"lampa");
    }
    // ---- a globe pendant: the cable from the slats, a black cap, an opal glass globe (lit)
    static void HPendant(Vector3 globeCentre,float r,float ceiling)
    {
        var top=globeCentre+Vector3.up*(r*.92f);
        m.Tube(frame,new Vector3(top.x,ceiling,top.z),top+Vector3.up*.05f,.004f,6);
        m.Cylinder(frame,top-Vector3.up*.005f,.045f,.06f,16,Quaternion.identity,true,false,.03f);
        var prof=new List<Vector2>();for(int i=0;i<=10;i++){float a=Mathf.PI*i/10f;prof.Add(new Vector2(Mathf.Max(.002f,Mathf.Sin(a)*r),r-Mathf.Cos(a)*r));}
        m.Lathe(hOpal,globeCentre-Vector3.up*r,prof.ToArray(),24,Quaternion.identity,false,false);
    }
    // ---- a rug (12 mm on the floor), its edges rounded
    static void HRug(Vector3 c,float sx,float sz,Material mat){m.RoundedBox(mat,c+Vector3.up*.006f,new Vector3(sx,.012f,sz),.005f,Quaternion.identity,1);}
    // ---- a bookcase: walnut uprights and shelves on a black frame back, the owner's books on the shelves (library)
    static void HBookcase(Vector3 c,Vector3 face,float L,float H,Material wood,Transform books)
    {
        var k=new HF(c,face);float Dp=.36f;int nb=Mathf.Max(1,Mathf.RoundToInt(L/.9f));float bw=L/nb;
        m.RoundedBox(wood,k.P(0f,H*.5f,Dp-.01f),new Vector3(L,H,.02f),.006f,k.rot,1);
        for(int i=0;i<=nb;i++)m.RoundedBox(wood,k.P(-L*.5f+i*bw,H*.5f,Dp*.5f),new Vector3(.03f,H,Dp-.02f),.008f,k.rot,1);
        for(int j=0;j<=5;j++)
        {
            float yy=.06f+j*(H-.10f)/5f;
            m.RoundedBox(wood,k.P(0f,yy,Dp*.5f),new Vector3(L,.03f,Dp-.02f),.008f,k.rot,1);
            if(j<5&&books)for(int i=0;i<nb;i++)
                if((i+j)%3!=1)LibShelf(books,"ksiegarnia",k.P(-L*.5f+i*bw+.04f,yy+.016f,Dp*.45f),k.u,-k.f,bw-.08f,(H-.10f)/5f-.05f);
                else HVase(k.P(-L*.5f+(i+.5f)*bw,yy+.015f,Dp*.45f),.24f,gStoneTop);
        }
        HCol(k,-L*.5f,L*.5f,0f,Dp,H,"regał");
    }
    // ---- a desk: a walnut top on two black steel sled frames, a modesty panel
    static void HDesk(Vector3 c,Vector3 face,float L,float Wd)
    {
        var k=new HF(c,face);float h=.75f;
        m.RoundedBox(walnut,k.P(0f,h-.02f,Wd*.5f),new Vector3(L,.04f,Wd),.015f,k.rot,2);
        foreach(float s in new[]{-1f,1f})
        {
            float a=s*(L*.5f-.10f);
            foreach(float d in new[]{.08f,Wd-.08f})m.RoundedBox(frame,k.P(a,(h-.04f)*.5f,d),new Vector3(.04f,h-.04f,.04f),.01f,k.rot,1);
            m.RoundedBox(frame,k.P(a,h-.06f,Wd*.5f),new Vector3(.04f,.04f,Wd-.10f),.01f,k.rot,1);
            m.RoundedBox(frame,k.P(a,.02f,Wd*.5f),new Vector3(.04f,.04f,Wd-.10f),.01f,k.rot,1);
        }
        m.RoundedBox(walnut,k.P(0f,.48f,Wd-.10f),new Vector3(L-.30f,.40f,.02f),.006f,k.rot,1);
        HCol(k,-L*.5f,L*.5f,0f,Wd,h,"biurko");
    }
    // ---- an office chair: a five-star aluminium base on castors, the gas lift, a leather seat and a curved back, arms
    static void HOfficeChair(Vector3 c,Vector3 face,string label)
    {
        var k=new HF(c,face);var b=k.P(0f,0f,.28f);
        for(int i=0;i<5;i++)
        {
            float a=i*72f*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Sin(a),0f,Mathf.Cos(a));
            m.Tube(stainless,b+Vector3.up*.10f,b+Vector3.up*.08f+dir*.32f,.018f,8);
            m.Cylinder(frame,b+dir*.32f+Vector3.up*.0f,.028f,.05f,10,Quaternion.Euler(0f,0f,90f)*Quaternion.identity,true,true);
        }
        m.Cylinder(frame,b+Vector3.up*.10f,.035f,.30f,12,Quaternion.identity,true,false,.025f);
        m.RoundedBox(frame,k.P(0f,.41f,.27f),new Vector3(.40f,.03f,.38f),.01f,k.rot,1);
        m.RoundedBox(gLeatherBlack,k.P(0f,.465f,.26f),new Vector3(.50f,.08f,.48f),.035f,k.rot,3);
        m.RoundedBox(gLeatherBlack,k.P(0f,.82f,.52f),new Vector3(.48f,.56f,.08f),.04f,k.Lean(10f),3);
        m.Tube(frame,k.P(0f,.45f,.48f),k.P(0f,.60f,.55f),.02f,8);
        foreach(float s in new[]{-1f,1f})
        {
            m.Tube(frame,k.P(s*.25f,.43f,.36f),k.P(s*.25f,.66f,.30f),.012f,8);
            m.RoundedBox(gLeatherBlack,k.P(s*.25f,.675f,.27f),new Vector3(.06f,.025f,.24f),.01f,k.rot,1);
        }
        var st=k.P(0f,.50f,.26f);Seat(st.x,st.y,st.z,face,label);
        HCol(k,-.32f,.32f,-.04f,.60f,1.1f,"krzesło biurowe");
    }
    // ---- an upholstered bench (the foot of a bed, the hall): a cushion on a walnut frame and legs
    static void HBench(Vector3 c,Vector3 face,float L,Material up,string label)
    {
        var k=new HF(c,face);
        foreach(float a in new[]{-L*.5f+.06f,L*.5f-.06f})foreach(float d in new[]{.06f,.38f})HLeg(walnut,k.P(a,0f,d),.30f,.016f,.022f);
        m.RoundedBox(walnut,k.P(0f,.32f,.22f),new Vector3(L,.04f,.44f),.012f,k.rot,1);
        m.RoundedBox(up,k.P(0f,.40f,.22f),new Vector3(L-.02f,.12f,.44f),.05f,k.rot,3);
        var st=k.P(0f,.46f,.22f);Seat(st.x,st.y,st.z,face,label);
        HCol(k,-L*.5f,L*.5f,0f,.44f,.46f,"ławka");
    }
    static void HPlant(Vector3 at,float yaw,int kind){Prop(root,kind==0?"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf":"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",at,yaw,.3f);}

    // ================================================================ BRAXHORN HOME (the store)
    static int G1HomePieces;
    static void G1Home()
    {
        HomeMaterials();
        float x0=G1EastX0+.15f,x1=G1HW-.3f,z0=-38.0f,z1=10f,y=G1F0;
        float wz0=-12.6f,wz1=-7.4f,wx=81.0f;                                   // the side entrance's windbreak (street door x 85)
        // ---- floor: herringbone oak parquet; the windbreak in the malls' stone; the walls lined; the ceiling slats
        void Fl(Material mat,float a,float b,float c0,float c1){if(b-a>.01f&&c1-c0>.01f)B(mat,a,b,WalkY,y,c0,c1,true);}
        Fl(hMarbleFloor,x0,wx,z0,9.94f);Fl(hMarbleFloor,wx,x1,z0,wz0);Fl(hMarbleFloor,wx,x1,wz1,9.94f);   // owner 19:32: "podłoga może być"Fl(gMallBand,wx,x1,wz0,wz1);
        B(gWhitePanel,x0-.15f,x0,y,G1Slab,z0,G1SouthMallZ0,true);B(gWhitePanel,x0-.15f,x0,y,G1Slab,G1SouthMallZ1,z1-.06f,true);
        B(gDark,x0-.17f,x0,4.2f,G1Slab,G1SouthMallZ0,G1SouthMallZ1);
        B(hWallWhite,x0,x0+.06f,y,G1Ceil0,-35.06f,G1SouthMallZ0);B(hWallWhite,x0,x0+.06f,y,G1Ceil0,G1SouthMallZ1,9.94f);
        B(hWallWhite,x0+.06f,x1,y,G1Ceil0,9.94f,10f);
        // a black ceiling with black tracks and spotlights over the room settings (a showroom, not a wood box)
        Q(gCinemaDark,new Vector3(x0+.06f,G1Ceil0,z0),new Vector3(x1,G1Ceil0,z0),new Vector3(x1,G1Ceil0,9.94f),new Vector3(x0+.06f,G1Ceil0,9.94f),Vector3.down);
        foreach(float zt in new[]{-31.5f,-28.0f,-19.0f,-12.5f,-1.5f,2.0f})
        {
            B(frame,x0+1.0f,x1-1.0f,G1Ceil0-.05f,G1Ceil0,zt-.025f,zt+.025f);
            for(float x=x0+2.0f;x<x1-1.5f;x+=2.2f)
            {
                var sp=new Vector3(x,G1Ceil0-.05f,zt);m.Cylinder(frame,sp-Vector3.up*.06f,.012f,.06f,8,Quaternion.identity,false,false);
                m.Cylinder(frame,sp-Vector3.up*.24f,.055f,.18f,16,Quaternion.identity,true,false,.05f);m.Cylinder(ledWarm,sp-Vector3.up*.252f,.042f,.012f,16,Quaternion.identity,false,true);
            }
        }
        // owner 11.11 23:37 "ten sklep się ma nazywać Sobczak ... Sobczak meble ... piękny napis w 3D": SOBCZAK MEBLE
        PortalSign("SOBCZAK MEBLE",new Vector3(x0-.17f,3.92f,(G1SouthMallZ0+G1SouthMallZ1)*.5f),Vector3.left,11.4f,"SOBCZAK MEBLE","DOM • OŚWIETLENIE • DODATKI",1.0f);
        // ---- the partitions (3.0 m, warm plaster, rounded) along the column lines z -35 (with the staff door) and z 5
        float py=G1Ceil0-.003f;   // full height to the black ceiling: no grey shell band over the partitions (owner 23:46)
        RB(hWallWhite,x0+.06f,48.6f,y,py,-35.07f,-34.93f,.02f,true,2);RB(hWallWhite,50.4f,x1,y,py,-35.07f,-34.93f,.02f,true,2);
        RB(hWallWhite,48.6f,50.4f,y+2.3f,py,-35.07f,-34.93f,.02f,false,2);
        Door("G1_home_staff","G1 BRAXHORN HOME zaplecze",49.5f,y,-35f,0f,1.8f,2.3f,false);
        RB(hWallWhite,55f,x1,y,py,4.93f,5.07f,.02f,true,2);
        Flush("G1 BRAXHORN HOME | podłoga, ściany, sufit, przegrody");
        // ---- the rooms (each its own group, drawn up close)
        void Room(string name,Action build)
        {
            build();var g=Group(root,"G1 BRAXHORN HOME | "+name);m.Build(g,name,Folder,false);m=new BraxChampionsMeshBatch();LodCull(g,50f);G1HomePieces++;
        }
        var N=Vector3.forward;var Sth=Vector3.back;var E=Vector3.right;var Wst=Vector3.left;
        // S1 living: walnut battens on the partition, a bouclé corner sofa with a chaise, two velvet armchairs, a round
        // marble table on a walnut pedestal, an arc lamp over it, a side table, a rug, a plant, a sideboard with vases
        Room("S1 salon (sofa narożna boucle)",()=>
        {
            B(gCinemaDark,55.45f,64.55f,y+.10f,y+2.90f,-34.93f,-34.918f);
            for(float x=55.5f;x<64.45f;x+=.14f)B(walnut,x,x+.06f,y+.12f,y+2.88f,-34.918f,-34.888f);
            HRug(new Vector3(60f,y,-31.4f),4.2f,3.0f,hRugGrey);
            HSofa(new Vector3(59.6f,y+.012f,-33.90f),N,3.2f,hBoucle,hVelvetGreen,walnut,"sofa BRAXHORN HOME",1.0f,1);
            HCoffeeRound(new Vector3(59.3f,y+.012f,-31.75f),.55f,.40f,gStoneTop,walnut);
            foreach(float x in new[]{58.1f,60.5f})HSofa(new Vector3(x,y+.012f,-30.2f),Sth,.92f,hVelvetGreen,hBoucle,walnut,"fotel BRAXHORN HOME",0f,1,.17f);
            HArcLamp(new Vector3(57.0f,y+.012f,-34.4f),new Vector3(.66f,0f,.75f),2.0f);
            HCoffeeRound(new Vector3(57.55f,y+.012f,-33.4f),.24f,.52f,gStoneTop,frame);
            HSideboard(new Vector3(63.3f,y,-34.93f+.47f),N,2.0f,walnut,gStoneTop);
            HVase(new Vector3(62.7f,y+.78f,-34.70f),.42f,gStoneTop);HVase(new Vector3(63.0f,y+.78f,-34.72f),.28f,gStoneTop);HTableLamp(new Vector3(64.0f,y+.78f,-34.70f),.46f,walnut);
            HPlant(new Vector3(56.0f,y,-26.2f),20f,0);HPlant(new Vector3(64.2f,y,-26.4f),70f,1);
        });
        // S2 dining: a solid oak table for eight on a beige rug, upholstered chairs (linen, oak), three globe pendants, a fluted
        // walnut sideboard with lamps and vases
        Room("S2 jadalnia (stół dębowy, 8 krzeseł)",()=>
        {
            HRug(new Vector3(70f,y,-30.9f),4.0f,3.0f,hRugBeige);
            var tc=new Vector3(70f,y+.012f,-30.4f);
            HDiningTable(tc,N,2.4f,1.0f,hOakSolid,hOakSolid);
            foreach(float x in new[]{69.2f,70f,70.8f}){HDiningChair(new Vector3(x,y+.012f,-30.25f),Sth,hLinen,hOakSolid,"krzesło jadalnia");HDiningChair(new Vector3(x,y+.012f,-31.55f),N,hLinen,hOakSolid,"krzesło jadalnia");}
            HDiningChair(new Vector3(68.65f,y+.012f,-30.9f),E,hCognac,hOakSolid,"krzesło jadalnia");HDiningChair(new Vector3(71.35f,y+.012f,-30.9f),Wst,hCognac,hOakSolid,"krzesło jadalnia");
            foreach(float x in new[]{69.2f,70f,70.8f})HPendant(new Vector3(x,y+1.98f,-30.9f),.16f,G1Ceil0-.065f);
            HSideboard(new Vector3(70f,y,-34.93f+.47f),N,2.4f,walnut,gStoneTop);
            HTableLamp(new Vector3(69.1f,y+.78f,-34.70f),.46f,gStoneTop);HVase(new Vector3(70.6f,y+.78f,-34.72f),.36f,gStoneTop);HVase(new Vector3(70.9f,y+.78f,-34.70f),.24f,gStoneTop);
            HPlant(new Vector3(66.0f,y,-34.2f),40f,1);HPlant(new Vector3(74.0f,y,-26.4f),10f,0);
        });
        // S3 bedroom: linen acoustic panels behind a king bed with an emerald velvet headboard, bedside tables with lamps, a
        // velvet bench at the foot, a rug, a plant by the window
        Room("S3 sypialnia (łóżko 180, zagłówek welurowy)",()=>
        {
            for(int i=0;i<8;i++){float x=76.0f+i*1.05f;RB(hLinen,x,x+1.0f,y+.6f,y+2.7f,-34.93f,-34.89f,.012f,false,2);}
            HRug(new Vector3(79.5f,y,-31.6f),3.4f,2.6f,hRugGrey);
            HBed(new Vector3(79.5f,y+.012f,-32.75f),N,1.8f,hVelvetGreen,hWoolGrey,hTaupe,walnut,"łóżko BRAXHORN HOME");
            foreach(float s in new[]{-1f,1f})HNightstand(new Vector3(79.5f+s*1.33f,y+.012f,-34.42f),N,walnut);
            HBench(new Vector3(79.5f,y+.012f,-32.0f),N,1.5f,hVelvetGreen,"ławka sypialnia");
            HPlant(new Vector3(83.9f,y,-26.6f),0f,0);
        });
        // N1 living (facing south): oak bookcases with books either side of a cognac leather sofa, two grey wool armchairs, a
        // walnut coffee table, a reading lamp, a rug
        var books=BraxSklepyTowar.Korzen(g1Goods,"G1 BRAXHORN HOME");
        Room("N1 salon (sofa skórzana koniak)",()=>
        {
            HRug(new Vector3(60f,y,1.6f),4.0f,3.0f,hRugBeige);
            HSofa(new Vector3(60f,y+.012f,3.92f),Sth,2.5f,hCognac,hWoolGrey,walnut,"sofa skórzana");
            foreach(float x in new[]{58.9f,61.1f})HSofa(new Vector3(x,y+.012f,1.6f),N,.92f,hWoolGrey,hVelvetRust,walnut,"fotel",0f,1,.17f);
            HCoffeeRect(new Vector3(60f,y+.012f,2.55f),Sth,1.30f,.70f,.40f,walnut,gOak);
            foreach(float x in new[]{56.6f,63.4f})HBookcase(new Vector3(x,y,4.93f-.36f),Sth,2.2f,2.4f,gOak,books);
            HFloorLamp(new Vector3(58.3f,y+.012f,4.3f));
            HPlant(new Vector3(55.9f,y,-4.3f),30f,1);
        });
        // N2 home office: oak panelling, a walnut desk with a laptop and a lamp, an office chair, a library wall, a reading
        // corner (rust velvet armchair, lamp, side table)
        Room("N2 gabinet (biurko, fotel biurowy, biblioteka)",()=>
        {
            B(gWallPanels,65.4f,74.6f,y+.02f,y+2.95f,4.89f,4.93f);
            HRug(new Vector3(69.4f,y,1.4f),3.2f,2.4f,hRugGrey);
            HBookcase(new Vector3(69.4f,y,4.89f-.36f),Sth,4.4f,2.6f,walnut,books);
            HDesk(new Vector3(69.4f,y+.012f,1.0f),N,1.8f,.80f);
            HOfficeChair(new Vector3(69.4f,y+.012f,1.1f),Sth,"krzesło biurowe");
            HTableLamp(new Vector3(68.6f,y+.772f,.45f),.40f,gStoneTop);
            Goods("BRAXHORN HOME | laptop",()=>{BraxKatalog.Laptop(Kat(),new Vector3(70.0f,y+.772f,.62f),N,3);BraxKatalog.Monitor(Kat(),new Vector3(69.1f,y+.772f,.36f),N,2);BraxKatalog.KeyboardMouse(Kat(),new Vector3(69.1f,y+.772f,.74f),N);});
            HSofa(new Vector3(73.2f,y+.012f,.6f),new Vector3(-.5f,0f,-.8f),.92f,hVelvetRust,hLinen,walnut,"fotel do czytania",0f,1,.17f);
            HFloorLamp(new Vector3(74.2f,y+.012f,1.6f));
            HCoffeeRound(new Vector3(72.1f,y+.012f,-.3f),.24f,.52f,walnut,frame);
            HPlant(new Vector3(65.8f,y,-4.2f),60f,0);
        });
        // N3 bedroom: a queen bed with a linen headboard, bedside tables and lamps, a wool bench, a rug, plants
        Room("N3 sypialnia (łóżko 160, len)",()=>
        {
            for(float x=75.6f;x<84.2f;x+=.14f)B(gOak,x,x+.06f,y+.12f,y+2.88f,4.90f,4.93f);
            HRug(new Vector3(79.8f,y,2.4f),3.2f,2.6f,hRugBeige);
            HBed(new Vector3(79.8f,y+.012f,2.75f),Sth,1.6f,hLinen,hLinen,hWoolCharcoal,gOak,"łóżko");
            foreach(float s in new[]{-1f,1f})HNightstand(new Vector3(79.8f+s*1.23f,y+.012f,4.42f),Sth,gOak);
            HBench(new Vector3(79.8f,y+.012f,2.0f),Sth,1.3f,hWoolCharcoal,"ławka sypialnia");
            HPlant(new Vector3(84.0f,y,-4.0f),45f,1);
        });
        // the armchair collection: a walnut podium (12 cm) between the column lines, eight armchairs facing the aisle
        Room("kolekcja foteli",()=>
        {
            RB(granite,56.2f,73.8f,y,y+.12f,-12.7f,-11.1f,.02f,true,2);B(ledWarm,56.4f,73.6f,y+.02f,y+.035f,-12.712f,-12.70f);   // dark granite, an LED line along its front
            Material[] fab={hBoucle,hWoolGrey,hVelvetGreen,hCognac,hLinen,hWoolCharcoal,gLeatherBlack,hVelvetRust};
            Material[] acc={hVelvetRust,hBoucle,hLinen,hWoolGrey,hVelvetGreen,hBoucle,hCognac,hLinen};
            for(int i=0;i<8;i++)HSofa(new Vector3(57.3f+i*2.2f,y+.12f,-12.45f),Sth,.92f,fab[i],acc[i],i%2==0?walnut:frame,"fotel kolekcja",0f,1,.17f);
            for(int i=0;i<3;i++)HCoffeeRound(new Vector3(59.5f+i*6.6f,y,-14.3f),.30f,.45f,gStoneTop,walnut);
        });
        // the west: the cash desk under the 3D letters on a walnut-battened wall; the lighting display (a pendant cluster over
        // a round walnut table with lamps, floor lamps round it)
        Room("kasa i oświetlenie",()=>
        {
            for(float x=50.6f;x<54.9f;x+=.14f)B(walnut,x,x+.06f,y+.12f,y+2.88f,-34.93f,-34.90f);
            RB(gCinemaDark,50.9f,54.3f,y,y+.08f,-31.25f,-30.65f,.02f,false,2);
            RB(walnut,50.8f,54.4f,y+.08f,y+1.02f,-31.3f,-30.6f,.03f,true,2);RB(gStoneTop,50.75f,54.45f,y+1.02f,y+1.06f,-31.35f,-30.55f,.012f,false,2);
            Goods("BRAXHORN HOME | kasa",()=>BraxKatalog.Monitor(Kat(),new Vector3(53.6f,y+1.06f,-30.95f),N,0,24f));
            HTableLamp(new Vector3(51.3f,y+1.06f,-30.95f),.40f,gStoneTop);
            HCoffeeRound(new Vector3(51.0f,y,-4.0f),.80f,.75f,walnut,walnut);
            HTableLamp(new Vector3(50.7f,y+.75f,-4.2f),.50f,gStoneTop);HTableLamp(new Vector3(51.4f,y+.75f,-3.7f),.36f,walnut);HVase(new Vector3(51.3f,y+.75f,-4.45f),.30f,gStoneTop);
            for(int i=0;i<7;i++){float a=i*51.4f*Mathf.Deg2Rad,r=i==0?0f:.55f;HPendant(new Vector3(51.0f+Mathf.Sin(a)*r,y+2.2f+(i%3)*.28f,-4.0f+Mathf.Cos(a)*r),.13f+(i%2)*.04f,G1Ceil0-.065f);}
            HArcLamp(new Vector3(53.4f,y,-6.2f),new Vector3(-.6f,0f,.6f),1.3f);HFloorLamp(new Vector3(53.6f,y,-1.4f));HFloorLamp(new Vector3(48.6f,y,-1.2f));
            HPlant(new Vector3(48.0f,y,-26.4f),15f,0);HPlant(new Vector3(54.2f,y,-26.4f),80f,1);
        });
        // ---- owner 20:07 ("szafki ładne z klameczkami, komody ... sklep full bogaty, żeby tam było wszystko do mieszkań"): a
        // display wall (z -21.6..-21.4, x 56.4..74.6) with two full kitchens, wardrobes, dressers and bathroom vanities, the
        // TV wall on the west side - every piece from BraxKatalog, the same functions the flats and the hotel will use
        Room("ściana ekspozycyjna (kuchnie, szafy, łazienki)",()=>
        {
            RB(hWallWhite,56.4f,74.6f,y,y+2.75f,-21.6f,-21.4f,.02f,true,2);
            B(ledWarm,56.5f,74.5f,y+2.75f,y+2.762f,-21.58f,-21.42f);
        });
        Room("K1 kuchnia orzech z wyspą",()=>
        {
            BraxKatalog.Materials();var k=Kat();
            BraxKatalog.Kitchen(k,new Vector3(57.0f,y,-21.4f),Vector3.right,Vector3.forward,new[]{"slupek","lodowka","szuflady","zlew","zmywarka","szuflady","piekarnik","szuflady"},BraxKatalog.Walnut,BraxKatalog.BlackMatt,BraxKatalog.Stone,"BRAXHORN HOME kuchnia orzech");
            // the island (drawers to the kitchen, a stone top with an overhang for three bar stools on the other side)
            BraxKatalog.Kitchen(k,new Vector3(60.0f,y,-19.0f),Vector3.left,Vector3.back,new[]{"szuflady","drzwi","drzwi","szuflady"},BraxKatalog.Walnut,BraxKatalog.BlackMatt,BraxKatalog.Stone,"BRAXHORN HOME wyspa",false);
            RB(BraxKatalog.Walnut,57.6f,60.0f,y+.10f,y+.87f,-19.0f,-18.98f,.004f,false,1);
            RB(BraxKatalog.Stone,57.6f,60.0f,y+.87f,y+.91f,-19.0f,-18.68f,.006f,false,2);
            foreach(float x in new[]{58.0f,58.8f,59.6f})BarStool(new Vector3(x,y,-18.35f),Vector3.back);
            foreach(float x in new[]{58.2f,59.4f})HPendant(new Vector3(x,y+2.0f,-19.3f),.15f,G1Ceil0-.065f);
            HRug(new Vector3(59.4f,y,-17.0f),2.4f,1.6f,hRugGrey);
        });
        Room("K2 kuchnia grafit",()=>
        {
            BraxKatalog.Materials();var k=Kat();
            BraxKatalog.Kitchen(k,new Vector3(61.8f,y,-21.6f),Vector3.left,Vector3.back,new[]{"lodowka","szuflady","piekarnik","szuflady","zlew","zmywarka","szuflady","slupek"},BraxKatalog.Graphite,BraxKatalog.Steel,BraxKatalog.StoneDark,"BRAXHORN HOME kuchnia grafit");
            HDiningTable(new Vector3(59.4f,y+.012f,-24.2f),Vector3.forward,1.6f,.9f,hOakSolid,hOakSolid);
            foreach(float x in new[]{58.9f,59.9f}){HDiningChair(new Vector3(x,y+.012f,-23.75f),Vector3.back,hLinen,hOakSolid,"krzesło kuchnia");HDiningChair(new Vector3(x,y+.012f,-24.65f),Vector3.forward,hLinen,hOakSolid,"krzesło kuchnia");}
        });
        Room("szafy i komody",()=>
        {
            BraxKatalog.Materials();var k=Kat();
            BraxKatalog.Wardrobe(k,new Vector3(64.2f,y,-21.4f+.60f+.022f),Vector3.forward,2.0f,BraxKatalog.Oak,BraxKatalog.Oak,2.30f,"BRAXHORN HOME szafa dąb 200");
            BraxKatalog.Wardrobe(k,new Vector3(66.8f,y,-21.4f+.60f+.022f),Vector3.forward,1.5f,BraxKatalog.Walnut,BraxKatalog.Walnut,2.30f,"BRAXHORN HOME szafa orzech 150");
            BraxKatalog.Dresser(k,new Vector3(69.0f,y,-21.4f+.45f),Vector3.forward,1.6f,3,2,BraxKatalog.Walnut,BraxKatalog.BlackMatt,true,.82f,false,"BRAXHORN HOME komoda orzech");
            G1Mirror("BRAXHORN HOME lustro nad komodą",new Vector3(69.0f,y+1.62f,-21.4f),Vector3.forward,1.0f,.80f);
            HTableLamp(new Vector3(68.5f,y+.82f,-21.2f),.40f,gStoneTop);HVase(new Vector3(69.6f,y+.82f,-21.2f),.30f,gStoneTop);
            BraxKatalog.Dresser(k,new Vector3(71.0f,y,-21.4f+.45f),Vector3.forward,1.2f,4,1,BraxKatalog.Oak,BraxKatalog.Chrome,false,1.15f,true,"BRAXHORN HOME komoda dąb");
            BraxKatalog.Dresser(k,new Vector3(73.0f,y,-21.4f+.45f),Vector3.forward,1.4f,2,3,BraxKatalog.Graphite,BraxKatalog.Steel,true,.75f,true,"BRAXHORN HOME komoda grafit");
            HRug(new Vector3(68.5f,y,-18.6f),3.0f,2.0f,hRugBeige);
            HBench(new Vector3(68.5f,y+.012f,-18.2f),Vector3.forward,1.4f,hVelvetGreen,"pufa przy szafach");
        });
        Room("łazienki",()=>
        {
            BraxKatalog.Materials();var k=Kat();if(!wcTile)WcMaterials();
            B(wcTile,62.8f,74.4f,y,y+2.40f,-21.614f,-21.6f);                      // a 14 mm tiled panel on the wall's south face
            BraxKatalog.Vanity(k,new Vector3(64.2f,y,-21.614f),Vector3.back,1.0f,BraxKatalog.Oak,"BRAXHORN HOME umywalka dąb");
            BraxKatalog.Vanity(k,new Vector3(66.4f,y,-21.614f),Vector3.back,1.2f,BraxKatalog.Walnut,"BRAXHORN HOME umywalka orzech");
            foreach(var (x,w) in new[]{(64.2f,.8f),(66.4f,1.0f)})G1Mirror("BRAXHORN HOME łazienka "+x.ToString("0.0"),new Vector3(x,y+1.56f,-21.614f),Vector3.back,w,.78f);   // live mirrors
            WcPanReal(new Vector3(68.6f,y,-21.614f),Vector3.back);
            m.Tube(chrome,new Vector3(70.0f,y+1.1f,-21.68f),new Vector3(70.7f,y+1.1f,-21.68f),.012f,10);   // a towel rail with a towel
            foreach(float x in new[]{70.0f,70.7f})m.Tube(chrome,new Vector3(x,y+1.1f,-21.614f),new Vector3(x,y+1.1f,-21.68f),.008f,8);
            RB(hLinen,70.08f,70.62f,y+.62f,y+1.088f,-21.695f,-21.665f,.010f,false,2);                    // the towel hangs over the rail:
            m.Cylinder(hLinen,new Vector3(70.08f,y+1.1f,-21.68f),.021f,.54f,14,Quaternion.Euler(0,0,-90),true,true);   // its fold round the bar
            BraxKatalog.Dresser(k,new Vector3(72.4f,y,-21.614f-.45f),Vector3.back,1.6f,2,2,BraxKatalog.Oak,BraxKatalog.BlackMatt,true,.62f,true,"BRAXHORN HOME szafka łazienkowa");
            HPlant(new Vector3(73.8f,y,-22.4f),25f,1);
        });
        Room("RTV (telewizory BRAXHORN)",()=>
        {
            BraxKatalog.Materials();var k=Kat();
            BraxKatalog.MediaUnit(k,new Vector3(x0+.06f+.44f,y,-11.4f),Vector3.right,2.0f,walnut,65f,0,"BRAXHORN HOME RTV orzech");
            BraxKatalog.MediaUnit(k,new Vector3(x0+.06f+.44f,y,-8.4f),Vector3.right,2.0f,BraxKatalog.Graphite,55f,1,"BRAXHORN HOME RTV grafit");
            HSofa(new Vector3(51.8f,y+.012f,-9.9f),Vector3.left,2.4f,hWoolGrey,hVelvetGreen,walnut,"sofa RTV");
            HCoffeeRect(new Vector3(50.3f,y+.012f,-9.9f),Vector3.left,1.1f,.6f,.38f,walnut,gOak);
            HRug(new Vector3(50.4f,y,-9.9f),2.4f,3.2f,hRugGrey);
        });
        BraxSklepyTowar.Zakoncz(books,"G1 BRAXHORN HOME");
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"SOBCZAK MEBLE",new Vector3(53.05f,y+2.22f,-34.90f+.004f),0f,.27f,.06f,.12f,3.35f);   // clear of the column at x 50.9
        BraxChampionsLetters3D.Build(lm,root,"G1 szyld SOBCZAK MEBLE (wnętrze)");Signs++;
        G1Shops++;
    }

    // ---- the goods-in (x 47..85, z -55..-38): sealed concrete floor, the three docks' inner doors, steel racking (empty),
    // the wall to the store with its door, the service corridor's end
    static void G1GoodsIn()
    {
        float y=G1F0,x0=G1EastX0,x1=G1HW-.3f,z0=-G1HD+.3f,z1=-38.0f;
        B(Inside(Tex("g1_goods_floor","anti_slip_concrete",2.0f,new Color(.95f,.95f,.96f)),.8f),x0,x1,WalkY,y,z0,z1-.15f,true);
        B(gWhitePanel,x0,48.6f,y,G1Slab,z1-.15f,z1,true);B(gWhitePanel,50.4f,x1,y,G1Slab,z1-.15f,z1,true);B(gWhitePanel,48.6f,50.4f,y+2.4f,G1Slab,z1-.15f,z1);
        Q(ceilWhite,new Vector3(x0,G1Ceil0,z0),new Vector3(x1,G1Ceil0,z0),new Vector3(x1,G1Ceil0,z1-.15f),new Vector3(x0,G1Ceil0,z1-.15f),Vector3.down);
        for(float x=x0+2f;x<x1-1f;x+=4f)B(led,x-.04f,x+.04f,G1Ceil0-.03f,G1Ceil0,z0+1f,z1-1.2f);
        Door("G1_goods_home","G1 dostawy -> salon meblowy",49.5f,y,z1-.075f,0f,1.8f,2.4f,false);
        var orange=Inside(S("g1_racking_orange",new Color(.90f,.42f,.08f),.5f,.3f),.9f);var blue=Inside(S("g1_racking_blue",new Color(.10f,.25f,.62f),.5f,.3f),.9f);
        for(int r=0;r<2;r++)
        {
            float zr=-53.9f+r*3.4f;
            for(float x=51f;x<70f;x+=2.7f)foreach(float dz in new[]{0f,1.1f})B(blue,x-.04f,x+.04f,y,y+4.2f,zr+dz-.04f,zr+dz+.04f,true);
            for(int l=0;l<3;l++){float yy=y+.15f+l*1.4f;foreach(float dz in new[]{0f,1.1f})B(orange,51f,70.0f,yy,yy+.10f,zr+dz-.05f,zr+dz+.05f);}
            Col(51f,70f,y,y+4.2f,zr-.05f,zr+1.15f,"regał magazynowy");
        }
    }
}

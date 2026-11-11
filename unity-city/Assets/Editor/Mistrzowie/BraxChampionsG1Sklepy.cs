using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 shops' fit-outs (owner 11.11 17:32: "te kwadraty ... tego nawet nie rób ... lepiej zostawić puste ... musi być
// uzupełniony każdy sklep, jak jest możliwość, z naszej biblioteki ... mięso ... w lodówkach"; 17:33: "laptopa na przykład
// możesz stworzyć ... do sklepu elektronicznego"). No goods are ever boxes:
//   - the shop furniture is built (rails, shelving, gondolas, vitrines, counters, tables, benches, mirrors, fitting rooms);
//   - the goods come from the owner's library: the shop pack miasto_sklepy through the city's BraxSklepyTowar (medicines,
//     books, groceries, meat and cheese, milk, bottles, cans), Poly Haven originals of the region (cakes, croissants, tea
//     sets, flowers) - a missing model leaves the place empty and is logged, never a stand-in;
//   - electronics are built here as real products: laptops (keys, trackpad, hinge, lit screen), phones on stands, TVs and
//     monitors (thin bezels, the picture on the screen), headphones, speakers, consoles, cameras - every screen a picture
//     from one painted atlas (desktop, phone home screen, film still, football game), nothing stacked on another face;
//   - clothes, shoes, cosmetics, toys, jewellery: empty fixtures until real models are chosen (owner: "pomyśleć").
// Small goods sit in LOD groups that disappear beyond ~25 m (phone cost; no far-distance depth fighting).
public static partial class BraxChampionsD2
{
    static Material eAlu,eKey,eBezel,eBack,eWhite,eDisplay,jSilver,jDial,jDiamond,jSapphire,jEmerald,jRuby;
    static readonly System.Random g1Rng=new System.Random(1111);
    static Transform g1Goods;
    // the screen atlas (1024 x 256): desktop | film still | football game (256 x 144 each) | phone home screen (118 x 256)
    // tiles as (u0, v0, u1, v1)
    static Vector4 TDesk=>new Vector4(0f,0f,256f/1024f,144f/256f);
    static Vector4 TFilm=>new Vector4(256f/1024f,0f,512f/1024f,144f/256f);
    static Vector4 TGame=>new Vector4(512f/1024f,0f,768f/1024f,144f/256f);
    static Vector4 TPhone=>new Vector4(768f/1024f,0f,886f/1024f,1f);

    static void ShopMaterials()
    {
        eAlu=Inside(S("g1_el_aluminium",new Color(.76f,.77f,.79f),.32f,.9f),.9f);
        // jewellery (owner 19:29: the jeweller must read as a jeweller): polished silver / platinum, a white dial, cut stones
        jSilver=Inside(S("g1_jewel_silver_polished",new Color(.92f,.92f,.94f),.06f,1f),.95f);
        jDial=Inside(S("g1_jewel_dial_white",new Color(.95f,.95f,.93f),.18f,0f),.95f);
        jDiamond=Inside(S("g1_jewel_diamond",new Color(.95f,.98f,1.0f),.02f,0f,new Color(.30f,.32f,.36f)),.95f);
        jSapphire=Inside(S("g1_jewel_sapphire",new Color(.10f,.22f,.85f),.03f,0f,new Color(.04f,.08f,.30f)),.95f);
        jEmerald=Inside(S("g1_jewel_emerald",new Color(.04f,.60f,.30f),.03f,0f,new Color(.02f,.20f,.10f)),.95f);
        jRuby=Inside(S("g1_jewel_ruby",new Color(.75f,.04f,.10f),.03f,0f,new Color(.25f,.01f,.03f)),.95f);
        eKey=Inside(S("g1_el_keys_black",new Color(.05f,.05f,.06f),.55f,0f),.9f);
        eBezel=Inside(S("g1_el_bezel_black",new Color(.02f,.02f,.025f),.12f,0f),.9f);
        eBack=Inside(S("g1_el_back_graphite",new Color(.14f,.15f,.17f),.25f,.6f),.9f);
        eWhite=Inside(S("g1_el_white_gloss",new Color(.93f,.93f,.94f),.2f,0f),.9f);
        eDisplay=ScreenMaterial("g1_screens_atlas",1024,256,PaintScreens());
        g1Goods=Group(root,"G1 | towar sklepów (biblioteka: miasto_sklepy, Poly Haven; elektronika z kodu)");
    }

    // ---- the picture atlas, painted pixel by pixel
    static Color32[] PaintScreens()
    {
        const int W=1024,H=256;var px=new Color32[W*H];
        void P(int x,int y,Color32 c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}
        void R(int x0,int y0,int x1,int y1,Color32 c){for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)P(x,y,c);}
        Color32 L(Color32 a,Color32 b,float t){t=Mathf.Clamp01(t);return new Color32((byte)(a.r+(b.r-a.r)*t),(byte)(a.g+(b.g-a.g)*t),(byte)(a.b+(b.b-a.b)*t),255);}
        void Txt(string s,int x,int y,int sk,Color32 c)=>BraxChampionsPiksele.Tekst(px,W,H,s,x,y,sk,c);
        // desktop (0..255 x 0..143): a deep blue wave wallpaper, two windows, the taskbar
        for(int y=0;y<144;y++)for(int x=0;x<256;x++)
        {
            float w=Mathf.Sin(x*.035f+y*.02f)*10f+70f;
            var c=L(new Color32(12,28,70,255),new Color32(40,120,190,255),y/143f);
            if(Mathf.Abs(y-w)<6f)c=L(c,new Color32(120,200,240,255),1f-Mathf.Abs(y-w)/6f);
            P(x,y,c);
        }
        R(0,0,256,10,new Color32(18,20,28,255));for(int i=0;i<6;i++)R(6+i*12,2,13+i*12,8,i==0?new Color32(60,140,255,255):new Color32((byte)(150+i*15),120,(byte)(90+i*20),255));
        R(30,40,150,120,new Color32(236,238,242,255));R(30,112,150,120,new Color32(40,90,200,255));
        for(int i=0;i<6;i++)R(36,100-i*9,36+(i%3==0?100:70),104-i*9,new Color32(170,176,186,255));
        R(140,22,236,96,new Color32(250,250,252,255));R(140,88,236,96,new Color32(30,30,36,255));R(146,30,230,82,new Color32(60,150,90,255));
        Txt("BRAXHORN",150,124,1,new Color32(240,244,255,255));
        // film still (256..511): a sunset over the sea with mountains
        for(int y=0;y<144;y++)for(int x=256;x<512;x++)
        {
            int lx=x-256;float hz=60f;
            Color32 c;
            if(y>hz){c=L(new Color32(250,130,60,255),new Color32(40,30,90,255),(y-hz)/(143f-hz));}
            else{c=L(new Color32(20,15,40,255),new Color32(80,45,80,255),y/hz);if(Mathf.Abs(lx-150)<14f-(hz-y)*.12f&&(y%3)!=0)c=L(c,new Color32(255,190,110,255),.7f);}
            float sd=new Vector2(lx-150,y-74).magnitude;if(sd<16f&&y>hz)c=L(new Color32(255,214,130,255),c,sd/16f*.3f);
            float mh=hz+8f+12f*Mathf.Abs(Mathf.Sin(lx*.021f))+7f*Mathf.Sin(lx*.07f+1f);
            if(y>hz&&y<mh&&(lx<110||lx>196))c=new Color32(34,20,44,255);
            P(x,y,c);
        }
        // football (512..767): striped pitch, lines, players, the score bar
        for(int y=0;y<144;y++)for(int x=512;x<768;x++){int lx=x-512;P(x,y,(lx/16)%2==0?new Color32(46,128,52,255):new Color32(56,142,60,255));}
        R(512+127,8,512+129,124,new Color32(235,240,235,255));R(512+8,8,768-8,10,new Color32(235,240,235,255));R(512+8,122,768-8,124,new Color32(235,240,235,255));
        for(int a=0;a<64;a++){float t=a/64f*Mathf.PI*2f;P(512+128+(int)(Mathf.Cos(t)*20),66+(int)(Mathf.Sin(t)*20),new Color32(235,240,235,255));}
        var pl=new[]{(60,40),(90,80),(110,60),(150,90),(170,50),(200,70),(40,70),(220,60)};
        for(int i=0;i<pl.Length;i++)R(512+pl[i].Item1,pl[i].Item2,512+pl[i].Item1+4,pl[i].Item2+7,i%2==0?new Color32(220,40,40,255):new Color32(40,60,220,255));
        R(512,126,768,144,new Color32(10,12,20,255));Txt("BRX 2:1 CTY  67:12",512+40,131,1,new Color32(250,250,250,255));
        // phone home screen (768..885 x 0..255): violet to orange, the clock, 4 x 5 app icons, the dock
        for(int y=0;y<256;y++)for(int x=768;x<886;x++)P(x,y,L(new Color32(250,140,70,255),new Color32(110,50,190,255),y/255f));
        Txt("12:45",768+29,200,2,new Color32(255,255,255,255));
        Color32[] ic={new Color32(60,180,90,255),new Color32(40,120,240,255),new Color32(240,70,70,255),new Color32(250,200,40,255),new Color32(150,90,240,255),new Color32(30,200,210,255),new Color32(240,120,30,255),new Color32(230,230,235,255)};
        for(int r=0;r<5;r++)for(int c=0;c<4;c++){int x0=768+10+c*26,y0=170-r*30;R(x0+1,y0,x0+19,y0+20,ic[(r*4+c)%ic.Length]);R(x0,y0+1,x0+20,y0+19,ic[(r*4+c)%ic.Length]);}
        R(768+4,6,886-4,30,new Color32(255,255,255,70));for(int c=0;c<4;c++){int x0=768+10+c*26;R(x0,9,x0+20,27,ic[(c+3)%ic.Length]);}
        return px;
    }

    // ---- a thin display: the back and edges in one material, the front a bezel ring round the picture - every face on
    // its own plane (the picture is not laid over anything). n = the front's normal, v = the picture's up.
    static void QUV(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)m.QuadUV(mat,d,c,b,a,ud,uc,ub,ua);else m.QuadUV(mat,a,b,c,d,ua,ub,uc,ud);
    }
    static void Panel(Material back,Vector3 c,Vector3 v,Vector3 n,float w,float h,float t,float bz,Vector4 tile)
    {
        v.Normalize();n.Normalize();var u=Vector3.Cross(n,v).normalized;                // the picture's right seen from the front
        Vector3 F(float a,float b,float o)=>c+u*a+v*b+n*o;
        float hw=w*.5f,hh=h*.5f,ht=t*.5f,iw=hw-bz,ih=hh-bz;
        Q(back,F(-hw,-hh,-ht),F(hw,-hh,-ht),F(hw,hh,-ht),F(-hw,hh,-ht),-n);
        Q(back,F(-hw,-hh,-ht),F(hw,-hh,-ht),F(hw,-hh,ht),F(-hw,-hh,ht),-v);Q(back,F(-hw,hh,-ht),F(hw,hh,-ht),F(hw,hh,ht),F(-hw,hh,ht),v);
        Q(back,F(-hw,-hh,-ht),F(-hw,hh,-ht),F(-hw,hh,ht),F(-hw,-hh,ht),-u);Q(back,F(hw,-hh,-ht),F(hw,hh,-ht),F(hw,hh,ht),F(hw,-hh,ht),u);
        Q(eBezel,F(-hw,-hh,ht),F(hw,-hh,ht),F(hw,-ih,ht),F(-hw,-ih,ht),n);Q(eBezel,F(-hw,ih,ht),F(hw,ih,ht),F(hw,hh,ht),F(-hw,hh,ht),n);
        Q(eBezel,F(-hw,-ih,ht),F(-iw,-ih,ht),F(-iw,ih,ht),F(-hw,ih,ht),n);Q(eBezel,F(iw,-ih,ht),F(hw,-ih,ht),F(hw,ih,ht),F(iw,ih,ht),n);
        QUV(eDisplay,F(-iw,-ih,ht),F(iw,-ih,ht),F(iw,ih,ht),F(-iw,ih,ht),n,new Vector2(tile.x,tile.y),new Vector2(tile.z,tile.y),new Vector2(tile.z,tile.w),new Vector2(tile.x,tile.w));
    }
    // a laptop on a table at p, open toward f: aluminium body, 5 rows of keys and the space bar, the trackpad, the hinge,
    // the lid at 112 degrees with its picture
    static void Laptop(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f).normalized;var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(eAlu,p+Vector3.up*.009f,new Vector3(.31f,.017f,.215f),.005f,rot,2);
        for(int row=0;row<5;row++)for(int k=0;k<12;k++)
        {
            if(row==0&&(k>=3&&k<=8)){if(k==3)m.Box(eKey,p+Vector3.up*.0178f-f*(.005f)+r*(-.0004f),new Vector3(.128f,.003f,.016f),rot,false);continue;}
            m.Box(eKey,p+Vector3.up*.0178f+r*((k-5.5f)*.0228f)-f*(.005f+row*.0195f),new Vector3(.0195f,.003f,.0165f),rot,false);
        }
        m.Box(eBack,p+Vector3.up*.0172f+f*.055f,new Vector3(.11f,.0016f,.064f),rot,false);
        var hinge=p+Vector3.up*.012f-f*.1075f;
        m.Cylinder(eKey,hinge-r*.12f,.0055f,.24f,10,Quaternion.FromToRotation(Vector3.up,r),true,true);
        float a=112f*Mathf.Deg2Rad;var dir=f*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);var n=f*Mathf.Sin(a)-Vector3.up*Mathf.Cos(a);
        Panel(eAlu,hinge+dir*.1075f+Vector3.up*.006f,dir,n,.31f,.212f,.006f,.012f,TDesk);
    }
    // a phone leaning on a black stand at p toward f (portrait, 18 degrees back)
    static void Phone(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(eKey,p+Vector3.up*.012f,new Vector3(.085f,.024f,.07f),.006f,rot,2);
        float t=18f*Mathf.Deg2Rad;var v=Vector3.up*Mathf.Cos(t)-f*Mathf.Sin(t);var n=f*Mathf.Cos(t)+Vector3.up*Mathf.Sin(t);
        Panel(eBack,p+Vector3.up*.024f+f*.012f+v*.0755f,v,n,.072f,.151f,.008f,.0035f,TPhone);
    }
    // a TV / monitor: the panel (16:9) centred at c facing n, on two feet (table) or a wall bracket
    static void Screen169(Vector3 c,Vector3 n,float w,bool feet,float yFeet,bool film=true)
    {
        n.y=0;n.Normalize();float h=w*9f/16f+.02f;var r=Vector3.Cross(Vector3.up,n).normalized;
        Panel(eBezel,c,Vector3.up,n,w,h,w>1f?.04f:.02f,w>1f?.012f:.01f,film?TFilm:TDesk);
        if(feet)foreach(float s in new[]{-1f,1f})
        {
            var top=c+r*s*w*.36f-Vector3.up*(h*.5f-.01f);
            Beam(eBezel,top-n*.01f,new Vector3(top.x,yFeet,top.z)+n*.09f,.025f,.012f);
            Beam(eBezel,top-n*.01f,new Vector3(top.x,yFeet,top.z)-n*.07f,.025f,.012f);
        }
        else Beam(eBezel,c-n*.03f,c-n*.09f,.25f,.18f);                                   // the wall bracket
    }
    static void Monitor(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(eAlu,p+Vector3.up*.006f,new Vector3(.22f,.012f,.16f),.004f,rot,2);
        Beam(eAlu,p+Vector3.up*.012f-f*.03f,p+Vector3.up*.33f-f*.05f,.05f,.02f);
        Panel(eBezel,p+Vector3.up*.40f-f*.02f,Vector3.up,f,.61f,.36f,.022f,.008f,TDesk);
    }
    // over-ear headphones on a stand at p toward f
    static void Headphones(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f).normalized;
        m.Cylinder(eBack,p,.07f,.012f,20,Quaternion.identity,true);m.Cylinder(eAlu,p+Vector3.up*.012f,.010f,.25f,10,Quaternion.identity,true);
        var cc=p+Vector3.up*.255f;Vector3 prev=Vector3.zero;
        for(int i=0;i<=12;i++){float t=Mathf.Lerp(15f,165f,i/12f)*Mathf.Deg2Rad;var q=cc+r*Mathf.Cos(t)*.085f+Vector3.up*Mathf.Sin(t)*.085f;if(i>0)m.Tube(eKey,prev,q,.011f,8);prev=q;}
        foreach(float s in new[]{-1f,1f})
        {
            var cup=cc+r*s*.088f-Vector3.up*.035f;var ax=Quaternion.FromToRotation(Vector3.up,-r*s);
            m.Cylinder(eBack,cup+r*s*.016f,.046f,.032f,18,ax,true,true);
            m.Cylinder(eKey,cup-r*s*.016f,.043f,.018f,18,ax,true,false);
            m.Tube(eAlu,cup+Vector3.up*.03f+r*s*.0f,cc+r*s*.083f+Vector3.up*.02f,.004f,6);
        }
    }
    // a bookshelf speaker (walnut, black baffle drivers proud of the front by 15+ mm)
    static void Speaker(Vector3 p,Vector3 f,float h)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);float w=h*.6f,d=h*.75f;
        m.RoundedBox(walnut,p+Vector3.up*h*.5f,new Vector3(w,h,d),.008f,rot,2);
        var ax=Quaternion.FromToRotation(Vector3.up,f);var fr=p+f*d*.5f;
        m.Cylinder(eKey,fr+Vector3.up*h*.36f,w*.36f,.016f,24,ax,true,false);m.Cylinder(eBack,fr+Vector3.up*h*.36f,w*.12f,.026f,16,ax,true,false);
        m.Cylinder(eKey,fr+Vector3.up*h*.78f,w*.12f,.018f,16,ax,true,false);
    }
    static void Console(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);var r=Vector3.Cross(Vector3.up,f).normalized;
        m.RoundedBox(eWhite,p+Vector3.up*.04f,new Vector3(.30f,.08f,.26f),.02f,rot,2);
        m.Box(led,p+Vector3.up*.04f+f*.131f,new Vector3(.20f,.004f,.004f),rot,false);
        var pad=p+f*.24f+r*.12f;
        m.RoundedBox(eKey,pad+Vector3.up*.02f,new Vector3(.15f,.035f,.06f),.015f,rot,2);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(eKey,pad+Vector3.up*.018f+r*s*.06f-f*.03f,new Vector3(.045f,.03f,.08f),.014f,rot,2);
    }
    // ---- jewellery built in code (owner 17:33: "jeżeli potrafisz zrobić sam ... realistycznie"): p on the display surface,
    // f toward the customer
    static Vector3 JU(Vector3 f)=>Vector3.Cross(Vector3.up,f).normalized;
    static void JGem(Vector3 c,float r,Material mat,Quaternion rot)
    {
        m.Lathe(mat,c,new[]{new Vector2(.0005f,-r*.9f),new Vector2(r,-r*.05f),new Vector2(r*.62f,r*.35f),new Vector2(.0005f,r*.42f)},8,rot,false,false);
    }
    static void JRingTorus(Vector3 c,float R,float r,Material mat,Quaternion rot)
    {
        var prof=new List<Vector2>();for(int i=0;i<=10;i++){float a=Mathf.PI*2f*i/10f;prof.Add(new Vector2(R+Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
        m.Lathe(mat,c,prof.ToArray(),20,rot,false,false);
    }
    // a wristwatch on a black velvet cushion: the polished case and bezel, a white dial with black indices and hands, the
    // crown, a black leather strap over the cushion
    static void Watch(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var u=JU(f);var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(gVelvetBlack,p+Vector3.up*.032f,new Vector3(.075f,.064f,.05f),.022f,rot,2);
        m.RoundedBox(gLeatherBlack,p+Vector3.up*.064f+f*.004f,new Vector3(.020f,.006f,.058f),.003f,rot,1);
        var c=p+Vector3.up*.040f+f*.027f;var face=Quaternion.FromToRotation(Vector3.up,f);
        m.RoundedBox(gLeatherBlack,c,new Vector3(.020f,.062f,.006f),.003f,rot,1);
        m.Cylinder(jSilver,c,.020f,.010f,24,face,true,true,.019f);
        m.Cylinder(jDial,c+f*.0101f,.0165f,.0008f,24,face,true,false);
        for(int i=0;i<12;i++){float a=i*30f*Mathf.Deg2Rad;var q=c+f*.0112f+(u*Mathf.Sin(a)+Vector3.up*Mathf.Cos(a))*.0135f;m.Box(eKey,q,new Vector3(.0012f,.0012f,.0006f)+((i%3==0)?new Vector3(.0006f,.0025f,0f):Vector3.zero),rot*Quaternion.Euler(0,0,-i*30f));}   // 1 mm indices: a radius would not show
        m.Box(eKey,c+f*.0114f+Vector3.up*.004f,new Vector3(.0012f,.009f,.0005f),rot);
        m.Box(eKey,c+f*.0114f+u*.004f,new Vector3(.011f,.0012f,.0005f),rot*Quaternion.Euler(0,0,12f));
        m.Cylinder(jSilver,c+u*.021f,.0022f,.004f,8,Quaternion.FromToRotation(Vector3.up,u),true,true);
    }
    // a ring in an open velvet box: the base, the lid tilted back, the band (polished) with a cut stone
    static void RingBox(Vector3 p,Vector3 f,Material stone)
    {
        f.y=0;f.Normalize();var u=JU(f);var rot=Quaternion.LookRotation(f,Vector3.up);
        m.RoundedBox(gVelvetBlack,p+Vector3.up*.019f,new Vector3(.052f,.038f,.052f),.008f,rot,2);
        m.RoundedBox(gVelvetBlack,p+Vector3.up*.058f-f*.029f,new Vector3(.052f,.046f,.010f),.004f,rot*Quaternion.Euler(-14f,0,0),2);
        var c=p+Vector3.up*.050f;
        JRingTorus(c,.0095f,.0016f,jSilver,Quaternion.FromToRotation(Vector3.up,u));
        JGem(c+Vector3.up*.0122f,.0042f,stone,Quaternion.identity);
    }
    // a necklace on a black velvet neck form: the chain round the neck (lower in front), the pendant stone
    static void NecklaceBust(Vector3 p,Vector3 f,Material stone)
    {
        f.y=0;f.Normalize();var u=JU(f);
        m.Lathe(gVelvetBlack,p,new[]{new Vector2(.055f,0f),new Vector2(.05f,.012f),new Vector2(.03f,.03f),new Vector2(.022f,.10f),new Vector2(.024f,.17f),new Vector2(.05f,.20f),new Vector2(.056f,.215f),new Vector2(.002f,.225f)},20,Quaternion.identity,true,false);
        Vector3 Q(float a){float s=Mathf.Sin(a),c=Mathf.Cos(a);return p+u*(s*.027f)+f*(c*.027f)+Vector3.up*(.155f-.035f*Mathf.Max(0f,c));}
        for(int i=0;i<20;i++){float a0=Mathf.PI*2f*i/20f,a1=Mathf.PI*2f*(i+1)/20f;m.Tube(jSilver,Q(a0),Q(a1),.0012f,5);}
        JGem(Q(0f)+f*.003f-Vector3.up*.007f,.006f,stone,Quaternion.FromToRotation(Vector3.up,f));
    }
    static void Camera(Vector3 p,Vector3 f)
    {
        f.y=0;f.Normalize();var rot=Quaternion.LookRotation(f,Vector3.up);var r=Vector3.Cross(Vector3.up,f).normalized;
        m.RoundedBox(eKey,p+Vector3.up*.048f,new Vector3(.135f,.092f,.07f),.01f,rot,2);
        m.RoundedBox(eKey,p+Vector3.up*.045f+r*.055f+f*.02f,new Vector3(.035f,.086f,.05f),.012f,rot,2);
        m.RoundedBox(eKey,p+Vector3.up*.10f-r*.01f,new Vector3(.05f,.03f,.05f),.008f,rot,2);
        var ax=Quaternion.FromToRotation(Vector3.up,f);var lc=p+Vector3.up*.046f-r*.01f+f*.035f;
        m.Cylinder(eKey,lc,.034f,.09f,24,ax,true,false);m.Cylinder(eAlu,lc+f*.03f,.0355f,.008f,24,ax,false,false);
        m.Cylinder(glassDark,lc+f*.09f,.026f,.004f,20,ax,true,false);
    }
    // LOD: the group is drawn closer than `metres` only
    static void LodCull(Transform t,float metres)
    {
        var rs=t.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return;
        var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        var lod=t.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[]{new LOD(Mathf.Clamp(Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))/(2f*metres*K*.52f),.01f,.6f),rs)});
        lod.RecalculateBounds();
    }
    // build a shop's small goods into their own culled group (the static batch m is swapped meanwhile)
    static Transform goodsInst;   // the goods group being built: the catalogue's shared pieces are placed in it (culled with it)
    static void Goods(string label,Action build)
    {
        var keep=m;var keepI=goodsInst;m=new BraxChampionsMeshBatch();var g=Group(g1Goods,label);goodsInst=g;
        try{build();m.Build(g,label,Folder,false);LodCull(g,25f);}
        finally{m=keep;goodsInst=keepI;}
    }
    static Vector3 Wld(Vector3 local)=>root.TransformPoint(local*K);
    // the owner's library on a shelf / counter: start = the left end on the shelf top at mid depth (local), along it,
    // inward (away from the customer), the length and the free height (local metres)
    static void LibShelf(Transform grp,string typ,Vector3 start,Vector3 along,Vector3 inward,float len,float maxH)
        =>BraxSklepyTowar.Polka(typ,grp,Wld(start),along.normalized,inward.normalized,len*K,g1Rng,maxH*K);
    static void LibCounter(Transform grp,string typ,Vector3 start,Vector3 along,Vector3 inward,float len)
        =>BraxSklepyTowar.Lada(typ,grp,Wld(start),along.normalized,inward.normalized,len*K,g1Rng);

    // an upholstered sofa (owner 11.11 15:55: "krzesło jest krzesłem, obite ... gąbka ... zaokrąglone podłokietniki"):
    // c = the middle of its front edge on the floor, face = where the sitters look; walnut legs, an upholstered base,
    // seat cushions and back cushions with foam (rounded 6-7 cm), rounded padded arms; every seat a BraxSeat
    static void Sofa(Vector3 c,Vector3 face,int seats,Material up,string label)
    {
        face.y=0;face.Normalize();var u=Vector3.Cross(Vector3.up,face).normalized;var rot=Quaternion.LookRotation(-face,Vector3.up);
        float sw=.62f,arm=.20f,W=seats*sw+2f*arm,D=.92f;
        Vector3 P(float a,float h,float d)=>c+u*a+Vector3.up*h-face*d;                    // d > 0 toward the back
        foreach(var (a,d) in new[]{(-W*.5f+.08f,.08f),(W*.5f-.08f,.08f),(-W*.5f+.08f,D-.08f),(W*.5f-.08f,D-.08f)})
            m.Cylinder(walnut,P(a,0f,d),.025f,.11f,10,Quaternion.identity,false,false,.018f);
        m.RoundedBox(up,P(0f,.21f,D*.5f),new Vector3(W,.20f,D),.035f,rot,1);
        for(int i=0;i<seats;i++)
        {
            float a=-W*.5f+arm+(i+.5f)*sw;
            m.RoundedBox(up,P(a,.385f,D*.42f),new Vector3(sw-.012f,.15f,D*.74f),.06f,rot,2);
            m.RoundedBox(up,P(a,.66f,D-.20f),new Vector3(sw-.02f,.44f,.20f),.07f,rot*Quaternion.Euler(-10f,0,0),2);
            var st=P(a,.46f,D*.42f);Seat(st.x,st.y,st.z,face,label);
        }
        m.RoundedBox(up,P(0f,.55f,D-.07f),new Vector3(W,.58f,.14f),.05f,rot,1);
        foreach(float sd in new[]{-1f,1f})m.RoundedBox(up,P(sd*(W*.5f-arm*.5f),.42f,D*.5f),new Vector3(arm,.42f,D),.08f,rot,2);
        var a0=P(-W*.5f,0f,0f);var a1=P(W*.5f,0f,D);
        Col(Mathf.Min(a0.x,a1.x),Mathf.Max(a0.x,a1.x),c.y,c.y+.84f,Mathf.Min(a0.z,a1.z),Mathf.Max(a0.z,a1.z),"sofa");
        Benches++;
    }

    // ================================================================ the fit-out of a shop unit
    // the catalogue's context on the current batch (BraxKatalog: one function per piece, used everywhere)
    static BraxKatalog.Ctx Kat()=>new BraxKatalog.Ctx{m=m,root=root,cols=cols,unit=K,water=waterJet,seat=(p,f,l)=>Seat(p.x,p.y,p.z,f,l),inst=goodsInst};
    static void ShopFitOut(G1Unit u,float x0,float x1,float z0,float z1,float y,bool fac=false)
    {
        bool alongX=u.face=='S'||u.face=='N';float D=alongX?z1-z0:x1-x0,Wd=alongX?x1-x0:z1-z0;
        Vector3 P(float t,float yy,float d){if(u.face=='S')return new Vector3(x0+t,yy,z0+d);if(u.face=='N')return new Vector3(x1-t,yy,z1-d);if(u.face=='E')return new Vector3(x1-d,yy,z0+t);return new Vector3(x0+d,yy,z1-t);}
        Vector3 Dir(float dt,float dd)=>(P(dt,0,dd)-P(0,0,0)).normalized;                       // a local direction
        // every fixture rounded (owner 11.11 17:53: "co może być zaokrąglone, to zaokrąglone"): radius from its thinnest side
        void Box(Material mat,float t0,float t1,float y0,float y1,float d0,float d1,bool col=false)
        {
            var a=P(t0,y0,d0);var b=P(t1,y1,d1);float x0_=Mathf.Min(a.x,b.x),x1_=Mathf.Max(a.x,b.x),z0_=Mathf.Min(a.z,b.z),z1_=Mathf.Max(a.z,b.z);
            float th=Mathf.Min(x1_-x0_,Mathf.Min(y1-y0,z1_-z0_));
            RB(mat,x0_,x1_,y0,y1,z0_,z1_,Mathf.Clamp(th*.3f,.004f,.03f),col,th>.1f?2:1);
        }
        Vector3 toFront=Dir(0,-1),toBack=Dir(0,1),alongT=Dir(1,0);
        // a display table: a solid 5 cm top (rounded edges) on two black steel sled legs (posts, foot and head rails) and a
        // stretcher; h = the top's height
        void Table(Material top,float t0,float t1,float d0,float d1,float h)
        {
            Box(top,t0,t1,y+h-.05f,y+h,d0,d1);
            foreach(float tl in new[]{t0+.15f,t1-.21f})
            {
                Box(frame,tl,tl+.06f,y+.03f,y+h-.11f,d0+.12f,d0+.18f);Box(frame,tl,tl+.06f,y+.03f,y+h-.11f,d1-.18f,d1-.12f);
                Box(frame,tl,tl+.06f,y,y+.03f,d0+.10f,d1-.10f);Box(frame,tl,tl+.06f,y+h-.11f,y+h-.05f,d0+.12f,d1-.12f);
            }
            Box(frame,t0+.21f,t1-.21f,y+.30f,y+.34f,(d0+d1)*.5f-.02f,(d0+d1)*.5f+.02f);
            var a=P(t0,y,d0);var b=P(t1,y,d1);Col(Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),y,y+h,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z),"stół ekspozycyjny");
        }
        // a corner unit's second frontage (glazed) is not a wall: nothing stands against it on that side (s 0 = t 0, 1 = t Wd)
        bool SideOpen(float sd)
        {
            if(u.face2=='\0')return false;var q=P(sd==0f?0f:Wd,y,D*.5f);
            return u.face2=='N'?Mathf.Abs(q.z-u.z1)<.5f:u.face2=='S'?Mathf.Abs(q.z-u.z0)<.5f:u.face2=='E'?Mathf.Abs(q.x-u.x1)<.5f:Mathf.Abs(q.x-u.x0)<.5f;
        }
        // a straight-edged batten (2-6 cm: a radius would not show, the triangles would)
        void SB(Material mat,float t0,float t1,float y0,float y1,float d0,float d1){var a=P(t0,y0,d0);var b=P(t1,y1,d1);B(mat,Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),y0,y1,Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z));}
        // a glass case (12 mm panes, black frame on top) on a surface at yb over t0..t1 x d0..d1, h high
        void GlassCase(float t0,float t1,float d0,float d1,float yb,float h)
        {
            var c0=P(t0,yb,d0);var c1=P(t1,yb,d0);var c2=P(t1,yb,d1);var c3=P(t0,yb,d1);var up=Vector3.up*h;
            foreach(var (a,b) in new[]{(c0,c1),(c1,c2),(c2,c3),(c3,c0)}){var n=Vector3.Cross(b-a,Vector3.up);Q(glass,a,b,b+up,a+up,n);Q(glass,a,b,b+up,a+up,-n);}
            SB(frame,t0,t1,yb+h,yb+h+.025f,d0,d1);
        }
        // a finished side wall (owner 11.11 18:32 "ściany ... gołe, tam nic nie ma"): walnut battens on black felt to 3.2 m, a
        // light-oak cabinet on a shadow plinth with a stone top and black handles, two floating oak shelves lit from below;
        // WallItems puts the goods on the top (level 0) and on the shelves (1, 2)
        void SideWall(float s,float d0,float d1,bool shelves)
        {
            if(SideOpen(s)||d1-d0<1.2f)return;
            float w=s==0f?0f:Wd,dr=s==0f?1f:-1f;float T(float o)=>w+dr*o;
            SB(gCinemaDark,T(0f),T(.02f),y+.10f,y+3.2f,d0,d1);
            for(float d=d0+.05f;d+.06f<d1-.04f;d+=.14f)SB(walnut,T(.02f),T(.05f),y+.12f,y+3.18f,d,d+.06f);
            Box(gCinemaDark,T(.05f),T(.44f),y,y+.08f,d0+.05f,d1-.05f);
            Box(gOak,T(.05f),T(.50f),y+.08f,y+.86f,d0,d1,true);
            Box(StoneTop(),T(.05f),T(.53f),y+.86f,y+.90f,d0-.02f,d1+.02f);
            int nh=Mathf.Max(1,Mathf.RoundToInt((d1-d0)/1.0f));
            for(int i=0;i<nh;i++){float dc=d0+(i+.5f)*(d1-d0)/nh;Box(frame,T(.50f),T(.518f),y+.73f,y+.75f,dc-.22f,dc+.22f);}
            if(shelves)foreach(float yy in new[]{1.55f,2.10f})
            {
                Box(gOak,T(.05f),T(.33f),y+yy-.04f,y+yy,d0+.1f,d1-.1f);
                SB(ledWarm,T(.24f),T(.29f),y+yy-.052f,y+yy-.04f,d0+.2f,d1-.2f);
            }
        }
        // the goods on a finished side wall (inside a Goods group: drawn up close only)
        void WallItems(float s,float d0,float d1,bool shelves,Action<Vector3,Vector3,int> item)
        {
            if(SideOpen(s)||d1-d0<1.2f)return;
            float w=s==0f?0f:Wd,dr=s==0f?1f:-1f;float T(float o)=>w+dr*o;var face=s==0f?alongT:-alongT;
            for(float d=d0+.45f;d<d1-.3f;d+=.8f)item(P(T(.27f),y+.90f,d),face,0);
            if(shelves)for(int l=1;l<=2;l++)for(float d=d0+.4f;d<d1-.3f;d+=.6f)item(P(T(.18f),y+(l==1?1.55f:2.10f),d),face,l);
        }
        string nm=u.name;
        // a façade unit: the 2.4 m street window zone stays clear (white plinths for the displays), the rest in front of it;
        // the inner units get the full-width feature wall at the back (walnut / graphite) with an LED cove
        if(fac)
        {
            float Df=D;D-=2.4f;
            for(int i=0;i<3;i++){float t=Wd*(.2f+.3f*i);Box(gCinemaDark,t-.50f,t+.50f,y,y+.06f,Df-1.45f,Df-.75f);Box(walnut,t-.55f,t+.55f,y+.06f,y+.42f,Df-1.5f,Df-.7f,true);}   // walnut window podiums on a black shadow plinth
            Box(frame,.4f,Wd-.4f,y+3.0f,y+3.04f,Df-1.12f,Df-1.08f);
        }
        else
        {
            Box(u.trade=="electronics"||u.trade=="phones"?gCinemaDark:gWood,.3f,Wd-.3f,y+.10f,y+3.6f,D-.16f,D-.12f);
            Box(ledWarm,.3f,Wd-.3f,y+3.62f,y+3.65f,D-.20f,D-.12f);
        }
        // the cash desk by one side near the front: walnut, white marble top, a POS terminal with its screen
        float ct=Wd-2.4f,cd=2.2f;
        Box(walnut,ct,ct+1.8f,y,y+1.0f,cd,cd+.7f,true);Box(StoneTop(),ct-.03f,ct+1.83f,y+1.0f,y+1.04f,cd-.03f,cd+.73f);
        var pos=P(ct+.85f,y+1.04f,cd+.35f);Goods(nm+" | kasa",()=>Monitor(pos,toFront));
        Transform lib=null;Transform Lib(){if(!lib)lib=BraxSklepyTowar.Korzen(g1Goods,"G1 "+nm);return lib;}
        // wall shelving on both side walls: a white (or walnut) carcass, n shelves; returns the shelf tops
        List<(float t,float yy,float side)> WallShelves(Material mat,int n,float depth,float d0,float d1,float h=2.2f)
        {
            var tops=new List<(float,float,float)>();
            foreach(float s in new[]{0f,1f})
            {
                if(SideOpen(s))continue;
                float ta=s==0?.08f:Wd-.08f-depth,tb=ta+depth;
                Box(mat,ta,tb,y,y+.10f,d0,d1,true);Box(mat,s==0?ta:tb-.03f,s==0?ta+.03f:tb,y,y+h,d0,d1);
                Box(mat,ta,tb,y+h-.03f,y+h,d0,d1);Box(mat,ta,tb,y,y+h,d0-.03f,d0);Box(mat,ta,tb,y,y+h,d1,d1+.03f);
                for(int k=0;k<n;k++){float yy=y+.30f+k*(h-.45f)/n;Box(mat,ta+(s==0?.03f:0f),tb-(s==0?0f:.03f),yy-.025f,yy,d0,d1);tops.Add((s==0?ta+.03f+(depth-.03f)*.5f:ta+(depth-.03f)*.5f,yy,s));}
            }
            return tops;
        }
        switch(u.trade)
        {
            case "fashion":case "sport":case "bags":
            {
                // clothes rails along both walls (black tubes on wall brackets) under a walnut top shelf, two free-standing
                // rails, a walnut table, a mirror, two fitting rooms at the back (walnut walls, curtains)
                foreach(float s in new[]{0f,1f})
                {
                    if(SideOpen(s))continue;
                    float t=s==0?.35f:Wd-.35f;
                    var a=P(t,y+1.65f,3.4f);var b=P(t,y+1.65f,D-3.4f);Beam(frame,a,b,.03f,.03f);
                    for(float d=3.4f;d<D-3.3f;d+=1.8f){var q=P(t,y+1.65f,d);Beam(frame,q,q+(s==0?-alongT:alongT)*.27f,.02f,.02f);}
                    Box(walnut,s==0?.08f:Wd-.48f,s==0?.48f:Wd-.08f,y+2.15f,y+2.19f,3.4f,D-3.4f);
                }
                foreach(float t in new[]{Wd*.35f,Wd*.65f})
                {
                    var a=P(t,y,4f);var b=P(t,y,D-4.5f);
                    m.Tube(frame,a,a+Vector3.up*1.55f,.018f,8);m.Tube(frame,b,b+Vector3.up*1.55f,.018f,8);m.Tube(frame,a+Vector3.up*1.55f,b+Vector3.up*1.55f,.016f,8);
                    m.Tube(frame,a+toFront*.25f,a+toBack*.25f,.015f,6);m.Tube(frame,b+toFront*.25f,b+toBack*.25f,.015f,6);
                    Col(Mathf.Min(a.x,b.x)-.25f,Mathf.Max(a.x,b.x)+.25f,y,y+1.6f,Mathf.Min(a.z,b.z)-.25f,Mathf.Max(a.z,b.z)+.25f,"wieszak");
                }
                Box(walnut,Wd*.5f-.9f,Wd*.5f+.9f,y,y+.80f,2.0f,2.9f,true);
                Box(glassDark,.4f,1.4f,y+.1f,y+2.2f,D-.32f,D-.30f);
                float fr=Wd-.2f;
                for(int i=0;i<2;i++)
                {
                    float t0=fr-(i+1)*1.4f,t1=fr-i*1.4f;
                    Box(gWood,t0,t0+.05f,y,y+2.4f,D-1.8f,D-.12f,true);
                    var cm=Inside(Tex("g1_fitting_curtain","poly_wool_herringbone",.5f,new Color(.45f,.45f,.52f)),.9f);
                    Box(cm,t0+.05f,t1,y+.15f,y+2.3f,D-1.82f,D-1.78f);Beam(frame,P(t0,y+2.36f,D-1.8f),P(t1,y+2.36f,D-1.8f),.02f,.02f);
                    Box(glassDark,t0+.3f,t1-.3f,y+.3f,y+2.0f,D-.17f,D-.15f);
                }
                Box(gWood,fr-2.8f-.05f,fr-2.8f,y,y+2.4f,D-1.8f,D-.12f,true);
                // the goods (owner 19:52 "sklepy są niewyposażone", 21:21 the empty rails): clothes on wire hangers on every rail,
                // folded stacks / balls on the table, dress forms at the front; bags: handbags and suitcases instead of clothes
                BraxKatalog.ClothesMaterials();
                bool sport=u.trade=="sport",bags=u.trade=="bags";
                int[] kinds=sport?new[]{5,6,0,5,3}:nm.Contains("DENIM")?new[]{3,0,3,1,6}:nm.Contains("BIELIZNA")?new[]{4,0,4,4}:nm.Contains("ELEGANCJA")?new[]{2,1,4,2,1}:nm.Contains("URBAN")?new[]{6,0,3,2,0}:new[]{0,1,2,4,3,6};
                Material Cloth(int kind,int i)=>sport&&kind!=3?BraxKatalog.SportCols[i%4]:kind==1?BraxKatalog.Shirts[i%4]:kind==2?BraxKatalog.Coats[i%4]:kind==3?BraxKatalog.Trousers[i%4]:kind==4?BraxKatalog.Dresses[i%4]:BraxKatalog.Tees[i%6];
                void Hang(Vector3 a,Vector3 b,float step,int seed)
                {
                    var al=(b-a).normalized;int n=Mathf.FloorToInt((b-a).magnitude/step);
                    for(int i=0;i<n;i++){int kind=kinds[((i/6)+seed)%kinds.Length];BraxKatalog.Garment(Kat(),a+al*((i+.5f)*step),al,kind,Cloth(kind,i+seed));}
                }
                if(!bags)Goods(nm+" | ubrania",()=>
                {
                    foreach(float s in new[]{0f,1f})if(!SideOpen(s)){float t=s==0?.35f:Wd-.35f;Hang(P(t,y+1.65f,3.5f),P(t,y+1.65f,D-3.5f),.18f,(int)s*3);}
                    int sd=1;foreach(float t in new[]{Wd*.35f,Wd*.65f})Hang(P(t,y+1.55f,4.1f),P(t,y+1.55f,D-4.6f),.20f,sd++*5);
                    for(int i=0;i<3;i++)for(int j=0;j<2;j++)
                    {
                        var q=P(Wd*.5f-.55f+i*.55f,y+.80f,2.2f+j*.5f);
                        if(sport&&j==1)BraxKatalog.Ball(Kat(),q,i);else BraxKatalog.FoldedStack(Kat(),q,toFront,Cloth(i==1?0:kinds[0],i+j*3),5+(i+j)%3);
                    }
                    if(sport)foreach(float s in new[]{0f,1f})if(!SideOpen(s))for(float d=3.7f;d<D-3.6f;d+=.75f)
                    {var q=P(s==0?.28f:Wd-.28f,y+2.19f,d);var f=s==0?alongT:-alongT;BraxKatalog.Sneaker(Kat(),q-toBack*.07f,f,Cloth(5,(int)(d*3)));BraxKatalog.Sneaker(Kat(),q+toBack*.07f,f,Cloth(5,(int)(d*3)));}
                });
                else Goods(nm+" | torby i walizki",()=>
                {
                    Material[] lth={BraxKatalog.Leather,BraxKatalog.Coats[1],BraxKatalog.Dresses[1],BraxKatalog.Coats[0]};
                    foreach(float s in new[]{0f,1f})if(!SideOpen(s))
                    {
                        var f=s==0?alongT:-alongT;int i=0;
                        for(float d=3.7f;d<D-3.7f;d+=.45f)BraxKatalog.Handbag(Kat(),P(s==0?.28f:Wd-.28f,y+2.19f,d),f,lth[i++%4],.28f+(i%3)*.04f);
                        for(float d=3.6f;d<D-3.6f;d+=.70f)BraxKatalog.Suitcase(Kat(),P(s==0?.40f:Wd-.40f,y,d),f,i++%2==0?BraxKatalog.Graphite:BraxKatalog.Alu,(i%3)!=0);
                    }
                    for(int i=0;i<3;i++)BraxKatalog.Handbag(Kat(),P(Wd*.5f-.55f+i*.55f,y+.80f,2.45f),toFront,lth[i],.34f);
                });
                // dress forms: on the street window's podiums (façade units) or by the entrance
                if(!bags)
                {
                    if(fac){float Df=D+2.4f;for(int i=0;i<3;i++)BraxKatalog.Mannequin(Kat(),P(Wd*(.2f+.3f*i),y+.42f,Df-1.1f),toBack,Cloth(kinds[i%kinds.Length]==3?4:kinds[i%kinds.Length],i));}
                    else foreach(float t in new[]{Wd*.16f,Wd*.36f})BraxKatalog.Mannequin(Kat(),P(t,y,1.1f),toFront,Cloth(t<Wd*.2f?4:2,(int)t));
                }
                break;
            }
            case "shoes":
            {
                // the shoe wall: white marble shelves on both walls (empty until the shoe models), leather benches, low mirrors
                WallShelves(StoneTop(),6,.45f,3.2f,D-1.0f,2.6f);
                for(int i=0;i<2;i++)Box(leatherNavy,Wd*.3f+i*Wd*.4f-.6f,Wd*.3f+i*Wd*.4f+.6f,y,y+.45f,D*.5f-.3f,D*.5f+.3f,true);
                foreach(float t in new[]{Wd*.3f,Wd*.7f})Box(glassDark,t-.25f,t+.25f,y+.05f,y+.65f,D*.5f+.29f,D*.5f+.31f);
                break;
            }
            case "electronics":case "phones":case "games":
            {
                // solid oak display tables on black steel sled legs; the goods built here: laptops and monitors / phones on stands / consoles and TVs /
                // headphones and speakers (AUDIO) / cameras (FOTO); TVs on the back wall or the side walls
                bool audio=nm.Contains("AUDIO"),foto=nm.Contains("FOTO"),phones=u.trade=="phones",games=u.trade=="games"||nm.Contains("GAMES");
                var tables=new List<float>();for(float d=3.5f;d<D-2.6f;d+=3.0f)tables.Add(d);
                foreach(float d in tables)Table(gOak,Wd*.5f-1.5f,Wd*.5f+1.5f,d,d+1.0f,.90f);
                Goods(nm+" | elektronika",()=>
                {
                    foreach(float d in tables)
                    {
                        for(int i=0;i<4;i++)
                        {
                            float t=Wd*.5f-1.12f+i*.75f;
                            foreach(var (dd,face) in new[]{(d+.27f,toFront),(d+.73f,toBack)})
                            {
                                var p=P(t,y+.90f,dd);
                                if(phones){Phone(p-alongT*.13f,face);Phone(p+alongT*.13f,face);}
                                else if(audio){if(i%2==0)Headphones(p,face);else Speaker(p,face,.30f);}
                                else if(foto)Camera(p,face);
                                else if(games){if(i%2==0)Console(p,face);else Headphones(p,face);}
                                else if(i%2==0)BraxKatalog.Laptop(Kat(),p,face,i+(int)d);else BraxKatalog.Monitor(Kat(),p,face,i+1);   // owner 20:07-20:12: the catalogue's BRAXHORN laptops / monitors
                            }
                        }
                    }
                    // TVs: on the back wall (inner units) or both side walls (façade units), 55" / 65"
                    // the BRAXHORN TVs (owner 20:12: "telewizory ... jak najlepsze ... swoją markę"): 65" on the back wall, 55" on the side walls
                    if(!fac){int k=0;for(float t=1.4f;t<Wd-1.2f;t+=2.0f)BraxKatalog.Tv(Kat(),P(t,y+2.1f,D-.16f),toFront,65f,k++,false,nm+" TV");}
                    else foreach(float s in new[]{0f,1f})if(!SideOpen(s)){int k=(int)s;for(float d=3.5f;d<D-1.5f;d+=2.6f)BraxKatalog.Tv(Kat(),P(s==0?.05f:Wd-.05f,y+2.1f,d),s==0?alongT:-alongT,55f,k++,false,nm+" TV");}
                    if(audio)foreach(float t in new[]{.8f,Wd-.8f})Speaker(P(t,y,D-1.0f),toFront,1.05f);
                });
                // both side walls finished: battens, the oak cabinet with the goods on it, lit shelves (façade units: the TVs over the cabinet)
                foreach(float s in new[]{0f,1f})SideWall(s,2.6f,D-1.4f,!fac);
                Goods(nm+" | ściany",()=>{foreach(float s in new[]{0f,1f})WallItems(s,2.6f,D-1.4f,!fac,(p,f,l)=>
                {
                    if(phones){if(l==0){Phone(p-Vector3.Cross(Vector3.up,f)*.13f,f);Phone(p+Vector3.Cross(Vector3.up,f)*.13f,f);}else Phone(p,f);}
                    else if(audio){if(l==0)Speaker(p,f,.30f);else Headphones(p,f);}
                    else if(foto)Camera(p,f);
                    else if(games){if(l==0)Console(p,f);else Headphones(p,f);}
                    else{if(l==0)BraxKatalog.Laptop(Kat(),p,f,(int)(p.x+p.z));else if(l==1)Headphones(p,f);else Speaker(p,f,.24f);}
                });});
                break;
            }
            case "cosmetics":case "optician":
            {
                // white gondolas with glass shelves on both walls, a lit mirror counter (the goods: empty for now)
                WallShelves(gOak,4,.40f,3.0f,D-1.2f);
                Box(gCinemaDark,Wd*.5f-1.14f,Wd*.5f+1.14f,y,y+.08f,D*.5f-.34f,D*.5f+.34f);Box(walnut,Wd*.5f-1.2f,Wd*.5f+1.2f,y+.08f,y+.96f,D*.5f-.4f,D*.5f+.4f,true);Box(StoneTop(),Wd*.5f-1.23f,Wd*.5f+1.23f,y+.96f,y+1.0f,D*.5f-.43f,D*.5f+.43f);
                Box(glassDark,Wd*.5f-1.0f,Wd*.5f+1.0f,y+1.2f,y+2.0f,D-.32f,D-.30f);Box(ledWarm,Wd*.5f-1.0f,Wd*.5f+1.0f,y+2.02f,y+2.05f,D-.33f,D-.29f);
                break;
            }
            case "pharmacy":
            {
                // the pharmacy: shelving on both walls and behind the counter, the owner's BRAXHORN PHARMA medicines (pack
                // miasto_sklepy, lek_01..24) on every shelf and on the counter; the dispensing counter with two stations
                var tops=WallShelves(gOak,5,.40f,3.0f,D-3.2f);
                var g=Lib();
                foreach(var (t,yy,s) in tops)LibShelf(g,"apteka",P(t,yy,3.05f),toBack,s==0?-alongT:alongT,D-6.3f,.30f);
                Box(walnut,.6f,Wd-.6f,y,y+1.0f,D-2.8f,D-2.2f,true);Box(StoneTop(),.55f,Wd-.55f,y+1.0f,y+1.04f,D-2.85f,D-2.15f);
                LibCounter(g,"apteka",P(.9f,y+1.04f,D-2.5f),alongT,toBack,Wd-1.8f);
                if(!fac)
                {
                    Box(gOak,.4f,Wd-.4f,y,y+2.4f,D-.55f,D-.16f,true);
                    for(int k=0;k<5;k++){float yy=y+.35f+k*.40f;Box(gOak,.4f,Wd-.4f,yy-.025f,yy,D-.95f,D-.55f);LibShelf(g,"apteka",P(.5f,yy,D-.75f),alongT,toBack,Wd-1.0f,.35f);}
                }
                Box(Inside(S("g1_pharmacy_cross",new Color(.2f,.85f,.4f),.3f,0f,new Color(.25f,1.0f,.45f)),.9f),Wd*.5f-.4f,Wd*.5f+.4f,y+2.9f,y+3.0f,D-.6f,D-.56f);
                break;
            }
            case "jewellery":
            {
                // vitrines: white marble bases, glass cases in black frames (empty until the jewellery models)
                for(float d=3.0f;d<D-2.0f;d+=2.8f)foreach(float t in new[]{Wd*.3f,Wd*.7f})
                {
                    {var tt=t;var dd=d;Goods(nm+" | witryna",()=>{Material[] st={jDiamond,jSapphire,jEmerald,jRuby};for(int i=0;i<4;i++){var p=P(tt-.54f+i*.36f,y+.93f,dd+.36f);if(i%3==0)Watch(p,toFront);else if(i%3==1)RingBox(p,toFront,st[(i+(int)dd)%4]);else NecklaceBust(p,toFront,st[(i+1)%4]);}});}
                    Box(gCinemaDark,t-.74f,t+.74f,y,y+.08f,d+.06f,d+.64f);Box(walnut,t-.8f,t+.8f,y+.08f,y+.86f,d,d+.7f,true);Box(StoneTop(),t-.82f,t+.82f,y+.86f,y+.9f,d-.02f,d+.72f);Box(gCinemaDark,t-.75f,t+.75f,y+.9f,y+.93f,d+.05f,d+.65f);   // walnut cabinet on a shadow plinth, stone top
                    var g0=P(t-.78f,y+.93f,d+.02f);var g1=P(t+.78f,y+1.3f,d+.68f);
                    B(frame,Mathf.Min(g0.x,g1.x),Mathf.Max(g0.x,g1.x),y+1.30f,y+1.33f,Mathf.Min(g0.z,g1.z),Mathf.Max(g0.z,g1.z));
                    foreach(var (q0,q1) in new[]{(P(t-.78f,y+.93f,d+.02f),P(t+.78f,y+.93f,d+.02f)),(P(t-.78f,y+.93f,d+.68f),P(t+.78f,y+.93f,d+.68f))})
                    {Q(glass,q0,q1,q1+Vector3.up*.37f,q0+Vector3.up*.37f,Vector3.Cross(q1-q0,Vector3.up));Q(glass,q0,q1,q1+Vector3.up*.37f,q0+Vector3.up*.37f,-Vector3.Cross(q1-q0,Vector3.up));}
                }
                // the walls: battens, oak cabinets with lit glass cases along them (the jewellery models come with the owner's pack)
                foreach(float s in new[]{0f,1f})
                {
                    SideWall(s,2.6f,D-1.2f,false);if(SideOpen(s))continue;
                    float w=s==0f?0f:Wd,dr=s==0f?1f:-1f;
                    var face=s==0f?alongT:-alongT;
                    for(float d=2.75f;d+1.3f<D-1.3f;d+=1.5f){var dd=d;var ww=w+dr*.29f;Goods(nm+" | gablota ścienna",()=>{Material[] st={jSapphire,jDiamond,jRuby,jEmerald};for(int i=0;i<3;i++){var p=P(ww,y+.90f,dd+.25f+i*.40f);if(i==1)NecklaceBust(p,face,st[i]);else if(i==0)Watch(p,face);else RingBox(p,face,st[(int)dd%4]);}});
                        GlassCase(Mathf.Min(w+dr*.10f,w+dr*.48f),Mathf.Max(w+dr*.10f,w+dr*.48f),d,d+1.3f,y+.90f,.42f);SB(ledWarm,Mathf.Min(w+dr*.12f,w+dr*.46f),Mathf.Max(w+dr*.12f,w+dr*.46f),y+1.31f,y+1.32f,d+.05f,d+1.25f);}
                }
                break;
            }
            case "books":
            {
                // walnut bookcases on both walls and two tables: the owner's books (ksiazki_01..08, covers) from the library
                var tops=WallShelves(gCinemaDark,5,.36f,2.8f,D-.8f,2.4f);var g=Lib();   // black bookcases: the books stand out of the walnut room
                foreach(var (t,yy,s) in tops)LibShelf(g,"ksiegarnia",P(t,yy,2.85f),toBack,s==0?-alongT:alongT,D-3.7f,.36f);
                for(float d=3.5f;d<D-2.5f;d+=3.2f){Box(walnut,Wd*.5f-1f,Wd*.5f+1f,y,y+.8f,d,d+1.2f,true);LibCounter(g,"ksiegarnia",P(Wd*.5f-.9f,y+.8f,d+.6f),alongT,toBack,1.8f);}
                break;
            }
            case "toys":
            {
                // oak shelving on both walls full of toys (catalogue: teddy bears, die-cast cars, stacking rings, balls), a play rug
                // with two big bears and a car race in the middle (owner 21:21 the empty shop)
                var tops=WallShelves(gOak,4,.50f,2.8f,D-.8f,2.0f);
                Box(Inside(Tex("g1_toys_play_rug","poly_wool_herringbone",.6f,new Color(.62f,1.30f,1.80f)),.9f),Wd*.5f-1.5f,Wd*.5f+1.5f,y,y+.012f,D*.5f-1.5f,D*.5f+1.5f);
                BraxKatalog.ClothesMaterials();
                Goods(nm+" | zabawki",()=>
                {
                    Material[] car={BraxKatalog.ToyRed,BraxKatalog.ToyBlue,BraxKatalog.ToyYellow,BraxKatalog.ToyGreen};
                    foreach(var (t,yy,s) in tops)
                    {
                        var f=s==0?alongT:-alongT;int i=(int)(yy*7f);
                        for(float d=3.1f;d<D-1.1f;d+=.46f,i++)
                        {
                            var q=P(t,yy,d);
                            switch(i%5){case 0:BraxKatalog.TeddyBear(Kat(),q,f,1f,i%2==0);break;case 1:BraxKatalog.ToyCar(Kat(),q,f,car[i%4]);break;case 2:BraxKatalog.StackingRings(Kat(),q);break;case 3:BraxKatalog.Ball(Kat(),q,i%3);break;default:BraxKatalog.ToyCar(Kat(),q,-f,car[(i+1)%4]);break;}
                        }
                    }
                    BraxKatalog.TeddyBear(Kat(),P(Wd*.5f-.7f,y+.012f,D*.5f),toFront,2.4f);BraxKatalog.TeddyBear(Kat(),P(Wd*.5f+.7f,y+.012f,D*.5f),toFront,2.0f,true);
                    for(int i=0;i<4;i++)BraxKatalog.ToyCar(Kat(),P(Wd*.5f-.6f+i*.4f,y+.012f,D*.5f-.8f),toFront,car[i]);
                });
                break;
            }
            case "home":
            {
                // DOM I WNĘTRZA: a small showroom with the BRAXHORN HOME pieces (built on a real structure): a linen sofa with
                // velvet pillows, a round marble table, two bouclé armchairs, a reading lamp, a fluted sideboard on the back
                // wall, a solid oak table with four upholstered chairs, rugs, plants; walls finished
                HomeMaterials();
                foreach(float s in new[]{0f,1f})SideWall(s,2.6f,D*.32f,true);
                float sb=D*.66f,sf=sb-.98f;
                HRug(P(Wd*.5f,y,sf-.6f),alongX?Wd*.55f:3.0f,alongX?3.0f:Wd*.55f,hRugGrey);
                HSofa(P(Wd*.5f,y+.012f,sf),toFront,2.4f,hLinen,hVelvetGreen,walnut,"sofa DOM I WNĘTRZA");
                HCoffeeRound(P(Wd*.5f,y+.012f,sf-.95f),.45f,.40f,gStoneTop,walnut);
                foreach(float a in new[]{-.62f,.62f})HSofa(P(Wd*.5f+a,y+.012f,sf-1.95f),toBack,.92f,hBoucle,hVelvetRust,walnut,"fotel",0f,1,.17f);
                HFloorLamp(P(Wd*.5f-1.75f,y+.012f,sf+.5f));
                HSideboard(P(Wd*.5f,y,D-.47f),toFront,2.2f,walnut,gStoneTop);
                HVase(P(Wd*.5f-.6f,y+.78f,D-.25f),.40f,gStoneTop);HTableLamp(P(Wd*.5f+.7f,y+.78f,D-.25f),.44f,walnut);
                float dt=D*.26f;
                if(D>=13f){
                HRug(P(Wd*.5f,y,dt+.5f),alongX?3.2f:2.6f,alongX?2.6f:3.2f,hRugBeige);
                HDiningTable(P(Wd*.5f,y+.012f,dt),toFront,1.8f,.95f,hOakSolid,hOakSolid);
                foreach(float a in new[]{-.42f,.42f}){HDiningChair(P(Wd*.5f+a,y+.012f,dt-.15f),toBack,hLinen,hOakSolid,"krzesło");HDiningChair(P(Wd*.5f+a,y+.012f,dt+1.10f),toFront,hLinen,hOakSolid,"krzesło");}
                HPendant(P(Wd*.5f,y+1.98f,dt+.475f),.17f,MallCeil(u.level)-.065f);}
                HPlant(P(.8f,y,D-.9f),30f,0);HPlant(P(Wd-.8f,y,D-.9f),75f,1);
                break;
            }
            case "flowers":
            {
                // zinc buckets and pots with the region's Poly Haven flowers (flower_empodium, flower_gazania), plants
                for(float t=1.1f;t<Wd-1.0f;t+=1.0f)for(float d=2.9f;d<D-1f;d+=1.4f)
                {
                    var b=P(t,y,d);m.Cylinder(stainless,b,.20f,.38f,18,Quaternion.identity,false,true);m.Cylinder(gCinemaDark,b+Vector3.up*.33f,.19f,.02f,18,Quaternion.identity,true,false);
                    BraxChampionsPromenadeFurniture.Flower(g1Goods,((int)(t*3+d))%2==0?"flower_empodium":"flower_gazania",b+Vector3.up*.35f,.55f,t*40f+d*25f);
                }
                Prop(root,"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",P(.7f,y,1.4f),10f,.3f);
                foreach(float sd in new[]{0f,1f})SideWall(sd,2.4f,D-1.0f,true);
                Goods(nm+" | półki",()=>{foreach(float sd in new[]{0f,1f})WallItems(sd,2.4f,D-1.0f,true,(p,f,l)=>BraxChampionsPromenadeFurniture.Flower(g1Goods,l==1?"flower_gazania":"flower_empodium",p,l==0?.55f:.40f,Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg));});
                break;
            }
            case "cafe":
            {
                // the counter (walnut, marble) with the region's Poly Haven cakes, croissants, a tea set; tables and chairs
                Box(walnut,.3f,Wd-.3f,y,y+1.05f,D-2.2f,D-1.5f,true);Box(StoneTop(),.25f,Wd-.25f,y+1.05f,y+1.09f,D-2.25f,D-1.45f);
                if(!nm.Contains("LODY"))
                {
                    string[] ids={"carrot_cake","croissant","strawberry_chocolate_cake","croissant","tea_set_01"};
                    for(int i=0;i<ids.Length;i++)BraxChampionsPromenadeFurniture.Flower(g1Goods,ids[i],P(.8f+i*(Wd-1.6f)/ids.Length,y+1.09f,D-1.85f),ids[i]=="croissant"?.14f:ids[i]=="tea_set_01"?.40f:.26f,i*30f);
                }
                foreach(float s in new[]{0f,1f})SideWall(s,2.4f,D-2.6f,true);
                Goods(nm+" | półki",()=>{foreach(float s in new[]{0f,1f})WallItems(s,2.4f,D-2.6f,true,(p,f,l)=>{if(l>0)BraxChampionsPromenadeFurniture.Flower(g1Goods,l==1?"tea_set_01":"wooden_bowl_02",p,l==1?.36f:.28f,Mathf.Atan2(f.x,f.z)*Mathf.Rad2Deg);});});
                for(float t=1.6f;t<Wd-1.4f;t+=2.4f)for(float d=2.5f;d<D-3.5f;d+=2.4f){var c=P(t,y,d);CafeTable(c.x,y,c.z);foreach(float a in new[]{0f,180f}){var dd=new Vector3(Mathf.Sin(a*Mathf.Deg2Rad),0,Mathf.Cos(a*Mathf.Deg2Rad));G1Chair(c+dd*.75f,-dd);}}
                break;
            }
            case "salon":
            {
                foreach(float s in new[]{0f,1f})SideWall(s,2.4f,D-1.6f,true);
                for(int i=0;i<3;i++){float t=1.2f+i*1.8f;if(t>Wd-1f)break;Box(glassDark,t-.5f,t+.5f,y+.9f,y+2.0f,D-.32f,D-.30f);Box(walnut,t-.55f,t+.55f,y+.75f,y+.80f,D-.65f,D-.32f);G1Chair(P(t,y,D-1.2f),toFront);}
                break;
            }
            case "service":
            {
                Box(walnut,.5f,Wd-.5f,y,y+1.05f,3.0f,3.7f,true);Box(StoneTop(),.45f,Wd-.45f,y+1.05f,y+1.09f,2.95f,3.75f);
                for(int i=0;i<3;i++){float t=1.5f+i*2f;if(t>Wd-1f)break;G1Chair(P(t,y,2.2f),toBack);}
                var mon=P(Wd*.5f,y+1.09f,3.35f);Goods(nm+" | monitor",()=>Monitor(mon,toFront));
                foreach(float s in new[]{0f,1f})SideWall(s,4.4f,D-1.0f,true);
                break;
            }
        }
        if(lib)BraxSklepyTowar.Zakoncz(lib,"G1 "+nm);
    }
}

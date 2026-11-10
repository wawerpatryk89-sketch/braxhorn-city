using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 22:30-22:47, after the renders of the finished E1 ("ja się nie będę powtarzał", "lustro to lustro i tak
// wszystko", "asortyment alkoholu oryginał z mojej biblioteki - na osiedlu w dyskotece jest", "obicia wszystko ma być top
// top", WC: "tu 100 % do zmiany", bar: "stół jedynie ładny", "uzupełniasz wszystko co jest możliwe z naszych zasobów",
// "ma być 100 % premium, każdy jeden materiał", "wszystko ma być funkcjonalne"):
// * mirrors are LIVE mirrors (BraxChampionsMirror: the city's planar reflection), never a grey plate: the bar's mirror wall,
//   the WC vanities, the cloakrooms;
// * the bar's alcohol, stools, pendants and glasses are the owner's own models from his library (the estate disco's pack
//   osiedle_wnetrza, restored in CI by restore_private.py): windupbird's back-bar shelves with bottles, Helyeouka's whisky
//   and wine bottles, NosTeam's leather bar stools, Mark Peters' bar pendants, Saandy's glasses (all CC BY 4.0,
//   Assets/Source/Mistrzowie/E1/CREDITS.md). Without the pack (the preview, a build without the key) the same places get
//   turned bottles with labels and liquid, leather stools and the Poly Haven pendant, and the log says so;
// * upholstery: quilted oxblood leather (ambientCG Leather013) on the settee backs and stool backs, smooth black leather
//   (ambientCG Leather037) on the seats; the lounge gets Poly Haven's black tufted Chesterfield sofas (sofa_02, real size),
//   leather ottomans and white marble coffee tables, wool rugs with a red border;
// * the toilets are rebuilt: black marble floor, white marble slabs on the walls, a black marble feature wall with a
//   floating white marble vanity, white stone vessel basins, wall spouts, a backlit live mirror, full-height black lacquer
//   cubicles, a dropped white ceiling with a perimeter light cove, recessed stainless towel / bin units;
// * the hall ceilings carry linear light slots instead of rows of downlight dots;
// * every sofa, settee, stool and bench is a seat (BraxSeat): you can sit down.
public static partial class BraxChampionsE1
{
    static Material woodFloor,cloakFloor,quiltRed,leatherBlack,marbleWallWhite,marbleWallBlack,labelPaper,labelBlack,labelRed,capBlack,stoneBasin;
    static Material[] glassTint,liquid;
    static Transform mirrorGroup;
    public static int LiveMirrors,OwnerModels,OwnerMissing,Seats,TurnedBottles;
    const string OwnerPack="osiedle_wnetrza";

    static void PremiumMaterials()
    {
        // Leather013 is a deep oxblood quilt (35,7,4): a little lifted, still red (no brown); Leather037 tinted to black hide
        // owner 10.11 23:20 ("podłoga ... całkowicie do zmiany" - billiards, bar, lounge, the passages): the board's own
        // material "Drewno (strefy wewn.)" - real oak planks (Poly Haven oak_wood_planks), smoked a shade darker as the board's
        // bar / billiards pictures; the cloakrooms: grey stone tiles in large format (granite_tile_04, neutral grey)
        woodFloor=Inside(Tex("interior_oak_planks","oak_wood_planks",2.2f,new Color(.50f,.44f,.42f)),.85f);
        cloakFloor=Inside(Tex("cloakroom_stone_tiles","granite_tile_04",1.4f,new Color(.98f,1.03f,1.12f)),.85f);
        quiltRed=Inside(BraxChampionsFoundation.LibraryAcg("e1_quilted_leather_oxblood","Leather013",new Color(1.6f,1.0f,1.25f),.55f),.85f);
        leatherBlack=Inside(BraxChampionsFoundation.LibraryLeather("e1_leather_black_smooth",new Color(.30f,.52f,.70f)),.85f);
        marbleWallWhite=Inside(BraxChampionsFoundation.LibraryMarbleWhite("e1_white_marble_slabs",new Color(.97f,.97f,.98f),2.6f),BraxChampionsInteriorLight.Walls);
        marbleWallBlack=Inside(BraxChampionsFoundation.LibraryMarble("e1_black_marble_slabs",new Color(.92f,.92f,.95f),2.2f),BraxChampionsInteriorLight.Walls);
        stoneBasin=Inside(BraxChampionsFoundation.LibraryMarbleWhite("e1_white_stone_basin",new Color(.98f,.98f,.99f),.6f),.85f);
        labelPaper=Inside(S("bottle_label_paper",new Color(.93f,.93f,.92f),.7f,0f),.85f);
        labelBlack=Inside(S("bottle_label_black",new Color(.05f,.05f,.06f),.45f,0f),.85f);
        labelRed=Inside(S("bottle_label_red",new Color(.62f,.04f,.06f),.45f,0f),.85f);
        capBlack=Inside(S("bottle_cap_black",new Color(.04f,.04f,.05f),.30f,.25f),.85f);
        // bottle glass: flint, green, cobalt, smoke (transparent); liquids: clear spirit (transparent), red, orange, green, black
        var gt=new[]{new Color(.86f,.90f,.92f,.26f),new Color(.10f,.40f,.20f,.55f),new Color(.12f,.26f,.62f,.50f),new Color(.22f,.24f,.27f,.58f)};
        glassTint=new Material[gt.Length];for(int i=0;i<gt.Length;i++)glassTint[i]=S("bottle_glass_"+i,gt[i],.04f,0f,null,true);
        var lq=new[]{new Color(.80f,.86f,.90f,.30f),new Color(.55f,.03f,.07f),new Color(.92f,.36f,.04f),new Color(.16f,.55f,.18f),new Color(.05f,.04f,.05f)};
        liquid=new Material[lq.Length];for(int i=0;i<lq.Length;i++)liquid[i]=Inside(S("bottle_liquid_"+i,lq[i],.10f,0f,null,lq[i].a<.99f),.9f);
    }

    // ================================================================ live mirror (the region's planar reflection)
    // centre c on the glass (nominal), n = unit normal into the room (axis aligned), w x h; the caller builds the backing
    // 30 mm behind the glass (never closer: no flicker) and any halo / frame round it
    static void LiveMirror(string label,Vector3 c,Vector3 n,float w,float h,int texW=1024,int texH=512,float range=12f,float reach=18f)
    {
        if(!mirrorGroup)mirrorGroup=Group(root,"E1 lustra | żywe odbicie (BraxChampionsMirror)");
        var g0=BraxWnetrzaKit.Grupa;bool i0=BraxWnetrzaKit.Wnetrze;
        BraxWnetrzaKit.Grupa=mirrorGroup;BraxWnetrzaKit.Wnetrze=true;
        try{BraxChampionsMirror.Wall("E1 "+label,root.TransformPoint(c*K),root.rotation*n,w*K,h*K,null,range*K,reach*K,texW,texH);LiveMirrors++;}
        catch(System.Exception e){Debug.LogError("E1 live mirror "+label+" DO POPRAWKI: "+e.Message);}
        finally{BraxWnetrzaKit.Grupa=g0;BraxWnetrzaKit.Wnetrze=i0;}
    }

    // ================================================================ seats (BraxSeat: sit down on approach)
    static void Seat(float x,float surface,float z,Vector3 face,string label)
    {
        var g=new GameObject("Siedzisko "+label);g.transform.SetParent(cols,false);
        g.transform.localPosition=new Vector3(x,surface,z)*K;g.transform.localRotation=Quaternion.LookRotation(face);
        var s=g.AddComponent<BraxSeat>();s.label=label;s.surfaceY=root.TransformPoint(new Vector3(x,surface,z)*K).y;
        Seats++;
    }

    // ================================================================ the owner's library (osiedle_wnetrza)
    static bool OwnerPackHere()=>BraxMiastoAssety.Jest(OwnerPack);
    // a copy of one of his models: real size x K x s (or fitted to a height), base centred on (x, y, z) nominal, turned by yaw
    static GameObject Owner(Transform parent,string file,string label,float x,float y,float z,float yaw,float s=1f,float fitHeight=0f)
    {
        if(!OwnerPackHere()){OwnerMissing++;return null;}
        var src=BraxMiastoAssety.Model(OwnerPack,file);
        if(!src){OwnerMissing++;return null;}
        try
        {
            var o=(GameObject)Object.Instantiate(src,parent,false);o.name="E1 | "+label+" | "+file+" (biblioteka właściciela, CC BY)";
            o.transform.position=Vector3.zero;o.transform.rotation=root.rotation*Quaternion.Euler(0,yaw,0)*src.transform.localRotation;
            o.transform.localScale=src.transform.localScale*(K*s);
            var b=BraxMiastoAssety.Granice(o);
            if(fitHeight>0f&&b.size.y>1e-4f){o.transform.localScale*=fitHeight*K/b.size.y;b=BraxMiastoAssety.Granice(o);}
            var t=root.TransformPoint(new Vector3(x,y,z)*K);
            o.transform.position+=new Vector3(t.x-b.center.x,t.y-b.min.y,t.z-b.center.z);
            BraxMiastoAssety.Przygotuj(o,true,true);
            OwnerModels++;return o;
        }
        catch(System.Exception e){Debug.LogWarning("E1 owner model "+file+" DO POPRAWKI: "+e.Message);OwnerMissing++;return null;}
    }
    static float TopOf(GameObject o)=>root.InverseTransformPoint(BraxMiastoAssety.Granice(o).max).y/K;

    // ================================================================ turned bottles (when the owner's pack is not here)
    // wine, vodka, whisky, liqueur, gin: glass below and above the label, the label band in the glass line (nothing laid
    // over the glass), the liquid 6 mm inside, the cap / foil
    static readonly Vector2[][] BottleBody={
        new[]{new Vector2(0,0),new Vector2(.034f,0),new Vector2(.037f,.006f),new Vector2(.037f,.205f),new Vector2(.033f,.228f),new Vector2(.017f,.256f),new Vector2(.014f,.272f),new Vector2(.014f,.300f)},
        new[]{new Vector2(0,0),new Vector2(.036f,0),new Vector2(.038f,.006f),new Vector2(.038f,.215f),new Vector2(.032f,.236f),new Vector2(.016f,.250f),new Vector2(.015f,.282f)},
        new[]{new Vector2(0,0),new Vector2(.043f,0),new Vector2(.046f,.006f),new Vector2(.046f,.168f),new Vector2(.041f,.186f),new Vector2(.019f,.200f),new Vector2(.017f,.226f)},
        new[]{new Vector2(0,0),new Vector2(.040f,0),new Vector2(.044f,.010f),new Vector2(.045f,.120f),new Vector2(.040f,.165f),new Vector2(.024f,.195f),new Vector2(.015f,.212f),new Vector2(.014f,.246f)},
        new[]{new Vector2(0,0),new Vector2(.040f,0),new Vector2(.042f,.006f),new Vector2(.042f,.190f),new Vector2(.036f,.205f),new Vector2(.017f,.214f),new Vector2(.016f,.240f)}};
    static void Bottle(Vector3 p,int kind,int v,int sides=12)
    {
        var prof=BottleBody[kind%BottleBody.Length];
        float r=prof[3].x,lab0=kind==2?.04f:.06f,lab1=kind==2?.13f:kind==3?.11f:.16f;
        var glass=glassTint[kind==0?(v%2==0?1:3):kind==4?(v%2==0?2:0):kind==2?0:(v%3==0?3:0)];
        var lq=liquid[kind==0?1+(v%2)*3:kind==1?0:kind==2?(v%2==0?2:0):kind==3?1+(v%3):(v%2==0?0:3)];
        // glass outer: below the label, above the label (profile split at lab0 / lab1)
        var lower=new List<Vector2>();var upper=new List<Vector2>();
        foreach(var q in prof){if(q.y<=lab0)lower.Add(q);if(q.y>=lab1)upper.Add(q);}
        lower.Add(new Vector2(r,lab0));upper.Insert(0,new Vector2(r,lab1));
        m.Lathe(glass,p,lower.ToArray(),sides,Quaternion.identity,true,false);
        m.Lathe(glass,p,upper.ToArray(),sides,Quaternion.identity,false,false);
        var lab=v%3==0?labelBlack:v%3==1?labelPaper:labelRed;
        m.Lathe(lab,p,new[]{new Vector2(r,lab0),new Vector2(r,lab1)},sides,Quaternion.identity,false,false);
        // liquid inside, 6 mm in, to 70-85 %
        float fill=prof[prof.Length-3].y*(.72f+.04f*(v%4));
        m.Lathe(lq,p+Vector3.up*.008f,new[]{new Vector2(0,0),new Vector2(r-.006f,0),new Vector2(r-.006f,fill-.008f),new Vector2(0,fill-.008f)},sides,Quaternion.identity,false,false);
        // cap / foil
        var top=prof[prof.Length-1];
        m.Lathe(kind==0?(v%2==0?labelRed:capBlack):kind==1?steelPolished:capBlack,p+Vector3.up*(top.y-.026f),
                new[]{new Vector2(0,0),new Vector2(top.x+.0015f,0),new Vector2(top.x+.0015f,.030f),new Vector2(0,.030f)},sides,Quaternion.identity,true,true);
        TurnedBottles++;
    }
    // a glass on a counter: tumbler (rocks), wine glass (stem)
    static void GlassItem(Vector3 p,bool wine)
    {
        if(wine)m.Lathe(clearGlass,p,new[]{new Vector2(0,0),new Vector2(.035f,0),new Vector2(.035f,.004f),new Vector2(.004f,.008f),new Vector2(.004f,.09f),new Vector2(.03f,.11f),new Vector2(.042f,.15f),new Vector2(.038f,.20f),new Vector2(.036f,.20f),new Vector2(.039f,.15f),new Vector2(.028f,.115f),new Vector2(0,.105f)},14,Quaternion.identity,false,false);
        else m.Lathe(clearGlass,p,new[]{new Vector2(0,0),new Vector2(.040f,0),new Vector2(.042f,.09f),new Vector2(.039f,.09f),new Vector2(.036f,.014f),new Vector2(0,.014f)},14,Quaternion.identity,false,false);
    }

    // ================================================================ bar stool (code, when the owner's stool is not here):
    // domed polished base, column with the gas-lift sleeve, foot ring on four spokes, black leather cushion with a red
    // welt - no backrest (owner 10.11 23:20: "oparcia przy hokerach ... coś takiego nie istnieje"); face = where you sit facing
    static void Stool(float x,float z,float h,Vector3 face)
    {
        var c=new Vector3(x,FFL,z);
        m.Lathe(steelPolished,c,new[]{new Vector2(0,0),new Vector2(.215f,0),new Vector2(.225f,.006f),new Vector2(.222f,.016f),new Vector2(.15f,.030f),new Vector2(.06f,.040f),new Vector2(.030f,.056f)},40,Quaternion.identity,true,false);
        m.Cylinder(steelPolished,c+Vector3.up*.05f,.026f,h-.26f,20);
        m.Lathe(black,c+Vector3.up*(h-.30f),new[]{new Vector2(.041f,0),new Vector2(.041f,.12f),new Vector2(.03f,.14f)},24,Quaternion.identity,true,false);
        float yr=FFL+.30f;const int seg=24;
        for(int i=0;i<seg;i++)
        {
            float a0=i*Mathf.PI*2f/seg,a1=(i+1)*Mathf.PI*2f/seg;
            m.Tube(steelPolished,new Vector3(x+Mathf.Cos(a0)*.19f,yr,z+Mathf.Sin(a0)*.19f),new Vector3(x+Mathf.Cos(a1)*.19f,yr,z+Mathf.Sin(a1)*.19f),.011f,8);
        }
        for(int i=0;i<4;i++){float a=(45f+i*90f)*Mathf.Deg2Rad;m.Tube(steelPolished,new Vector3(x,yr,z),new Vector3(x+Mathf.Cos(a)*.19f,yr,z+Mathf.Sin(a)*.19f),.008f,8);}
        // seat pan and cushion
        var s0=c+Vector3.up*(h-.16f);
        m.Lathe(steelPolished,s0,new[]{new Vector2(.03f,0),new Vector2(.16f,.03f),new Vector2(.175f,.05f),new Vector2(0,.05f)},32,Quaternion.identity,false,false);
        m.Lathe(leatherBlack,s0+Vector3.up*.05f,new[]{new Vector2(0,0),new Vector2(.178f,0),new Vector2(.196f,.010f),new Vector2(.203f,.035f),new Vector2(.199f,.070f),new Vector2(.186f,.090f),new Vector2(.15f,.102f),new Vector2(.08f,.108f),new Vector2(0,.110f)},40,Quaternion.identity,true,false);
        for(int i=0;i<40;i++)
        {
            float a0=i*Mathf.PI*2f/40,a1=(i+1)*Mathf.PI*2f/40;var yw=s0.y+.05f+.072f;
            m.Tube(quiltRed,new Vector3(x+Mathf.Cos(a0)*.198f,yw,z+Mathf.Sin(a0)*.198f),new Vector3(x+Mathf.Cos(a1)*.198f,yw,z+Mathf.Sin(a1)*.198f),.0065f,6);
        }
        Col(x-.21f,x+.21f,FFL,FFL+h,z-.21f,z+.21f,"hoker");
        Seat(x,FFL+h,z,face.sqrMagnitude>.01f?face:Vector3.forward,"HOKER");
    }

    // ================================================================ BAR (x 21..49, back wall z -15.2, counter z -18.3..-17.6)
    static void Bar()
    {
        var g=Group(root,"E1 bar | wyposażenie (biblioteka właściciela / Poly Haven)");
        const float x0=21f,x1=49f,wz=-15.2f,lin=-15.212f,gz=-15.242f;
        float mb=FFL+1.0f,mt=FFL+2.95f;
        // back wall: black marble slabs; the mirror wall (live) with a warm halo round it; red light lines over and under
        B(marbleWallBlack,20f,50f,FFL+.92f,mb-.02f,lin,wz);B(marbleWallBlack,20f,50f,mt+.02f,Ceiling,lin,wz);
        B(marbleWallBlack,20f,x0-.02f,mb-.02f,mt+.02f,lin,wz);B(marbleWallBlack,x1+.02f,50f,mb-.02f,mt+.02f,lin,wz);
        B(lampWarm,x0-.02f,x1+.02f,mb-.02f,mt+.02f,lin-.012f,lin);
        RB(black,x0-.04f,x1+.04f,mb-.04f,mb-.02f,gz-.012f,lin,.004f,false,1);RB(black,x0-.04f,x1+.04f,mt+.02f,mt+.04f,gz-.012f,lin,.004f,false,1);   // top / bottom trims
        LiveMirror("bar mirror wall",new Vector3((x0+x1)*.5f,(mb+mt)*.5f,gz),Vector3.back,x1-x0,mt-mb,4096,288,16f,22f);
        B(redLed,x0,x1,mt+.10f,mt+.13f,lin-.012f,lin);
        // "BAR" in 3D over the mirror: polished letters, red faces
        BraxChampionsLetters3D.Text(m,red,steelPolished,"BAR",new Vector3(35f,mt+.55f,lin-.002f),180f,.80f,.10f,.16f,4f);Signs++;
        // back counter: black lacquer with a white marble top (as the bar top), 20 glass-door fridges under it
        RB(lacquer,x0,x1,FFL,FFL+.88f,-15.45f,lin,.02f,true);
        RB(marbleWhite,x0-.03f,x1+.03f,FFL+.88f,FFL+.92f,-15.78f,lin,.010f,false,2);B(lacquer,x0,x1,FFL,FFL+.10f,-15.75f,-15.45f);
        Col(x0,x1,FFL,FFL+.92f,-15.78f,-15.45f,"lada zaplecza");
        for(int i=0;i<20;i++)
        {
            float x=x0+i*1.4f;
            RB(steelPolished,x-.02f,x+.02f,FFL+.10f,FFL+.86f,-15.76f,-15.45f,.006f,false,2);
            B(fixture,x+.02f,x+1.38f,FFL+.10f,FFL+.86f,-15.47f,-15.45f);
            B(lampWarm,x+.06f,x+1.34f,FFL+.84f,FFL+.86f,-15.72f,-15.47f);
            B(steelPolished,x+.04f,x+1.36f,FFL+.46f,FFL+.47f,-15.72f,-15.47f);
            B(clearGlass,x+.03f,x+1.37f,FFL+.12f,FFL+.84f,-15.758f,-15.75f);
            m.Cylinder(steelPolished,new Vector3(x+1.25f,FFL+.30f,-15.79f),.008f,.40f,8);
            for(int k=0;k<6;k++){Bottle(new Vector3(x+.17f+k*.21f,FFL+.10f,-15.6f),1,i+k,10);if(k%2==0)Bottle(new Vector3(x+.17f+k*.21f,FFL+.47f,-15.6f),4,i+k,10);}
            FinishItems++;
        }
        RB(steelPolished,x1-.02f,x1,FFL+.10f,FFL+.86f,-15.76f,-15.45f,.006f,false,2);
        // the shelves: the owner's back-bar units with his bottles (windupbird) standing on the back counter - or glass shelves
        int units=OwnerShelves(g,FFL+.92f,x0+.2f,x1-.2f,lin-.03f,mt);
        if(units==0)
        {
            Debug.Log("E1 bar: owner pack "+OwnerPack+" not here - glass shelves with turned bottles (in the game: his windupbird units)");
            for(float x=x0+.10f;x<=x1-.09f;x+=3.475f)m.Cylinder(steelPolished,new Vector3(x,FFL+.92f,-15.40f),.012f,mt-FFL-.92f,12);
            for(int s=0;s<3;s++)
            {
                float y=FFL+1.30f+s*.52f;
                B(clearGlass,x0+.2f,x1-.2f,y,y+.012f,-15.56f,-15.27f);
                B(lampWarm,x0+.2f,x1-.2f,y-.010f,y,-15.56f,-15.545f);
                int v=s*31;
                for(float x=x0+.32f;x<x1-.3f;x+=.135f,v++)
                {
                    float du=(x-x0-.10f)%3.475f;if(du<.07f||du>3.405f)continue;                      // the uprights
                    int kind=(v*7+s*3)%5;Bottle(new Vector3(x,y+.012f,-15.42f),kind,v);
                }
            }
        }
        // glasses on the back counter by the coffee station: the owner's (Saandy) or turned ones
        foreach(float x in new[]{x0+.9f,x1-.9f})
            if(!Owner(g,"bar_szklo_saandy","bar: szkło",x,FFL+.92f,-15.55f,0f,.9f))
                for(int i=0;i<4;i++)for(int j=0;j<2;j++)GlassItem(new Vector3(x-.30f+i*.20f,FFL+.92f,-15.62f+j*.16f),j==1);
        // coffee station: espresso machine, grinder
        m.RoundedBox(steelPolished,new Vector3(34f,FFL+1.12f,-15.5f),new Vector3(.80f,.40f,.50f),.04f,3);
        m.RoundedBox(black,new Vector3(34.6f,FFL+1.05f,-15.5f),new Vector3(.22f,.26f,.30f),.03f,3);
        m.Lathe(clearGlass,new Vector3(34.6f,FFL+1.18f,-15.5f),new[]{new Vector2(.05f,0),new Vector2(.09f,.14f),new Vector2(.085f,.16f),new Vector2(0,.16f)},20,Quaternion.identity,false,true);
        // the bar counter (owner: "stół jedynie ładny" - kept): walnut body with half-round ends, fluted front, stainless kick
        // plate, white marble top with an overhang on the guests' side; a red light line under the overhang
        float bz0=-18.3f,bz1=-17.6f,bzm=(bz0+bz1)*.5f,br=(bz1-bz0)*.5f,bt=FFL+1.01f;
        B(lacquer,22f,48f,FFL,bt,bz0,bz1,true);
        HalfRound(lacquer,new Vector3(22f,0,bzm),br,Vector3.left,FFL,bt,20);HalfRound(lacquer,new Vector3(48f,0,bzm),br,Vector3.right,FFL,bt,20);
        for(float x=22.1f;x<47.95f;x+=.16f)RB(lacquer,x,x+.08f,FFL+.14f,bt-.06f,bz0-.03f,bz0+.01f,.012f,false,2);
        RB(chrome,22f,48f,FFL,FFL+.12f,bz0-.025f,bz0+.01f,.01f,false,2);
        B(marbleWhite,22f,48f,bt,bt+.05f,bz0-.25f,bz1+.05f);
        HalfRound(marbleWhite,new Vector3(22f,0,bzm-.10f),br+.15f,Vector3.left,bt,bt+.05f,24);HalfRound(marbleWhite,new Vector3(48f,0,bzm-.10f),br+.15f,Vector3.right,bt,bt+.05f,24);
        B(redLed,22f,48f,bt-.014f,bt,bz0-.215f,bz0-.20f);
        // beer towers: polished T with three taps and black handles, drip tray
        foreach(float x in new[]{26f,37f,44f})
        {
            m.Cylinder(steelPolished,new Vector3(x,bt+.05f,-17.95f),.045f,.42f,20);
            m.Tube(steelPolished,new Vector3(x-.24f,bt+.47f,-17.95f),new Vector3(x+.24f,bt+.47f,-17.95f),.04f,16);
            for(int i=-1;i<=1;i++){var tp=new Vector3(x+i*.16f,bt+.47f,-17.95f);m.Tube(steelPolished,tp,tp+new Vector3(0,-.02f,.09f),.012f,10);   // taps to the bartender
                m.Lathe(black,tp+new Vector3(0,.02f,.02f),new[]{new Vector2(.012f,0),new Vector2(.016f,.12f),new Vector2(.012f,.14f),new Vector2(0,.145f)},12,Quaternion.identity,true,true);}
            RB(steelPolished,x-.30f,x+.30f,bt+.05f,bt+.07f,-17.92f,-17.72f,.008f,false,2);
            FinishItems++;
        }
        // the owner's whisky and wine on the bar by the towers (Helyeouka) - or a turned line-up
        foreach(float x in new[]{24.6f,35.6f,42.6f})
            if(!Owner(g,"bar_butelki_helyeouka","bar: whisky i wino",x,bt+.05f,-17.8f,180f,1f,.33f))
                for(int i=0;i<4;i++)Bottle(new Vector3(x-.24f+i*.16f,bt+.05f,-17.78f),i%2==0?2:0,i+(int)x);
        // till: a slim terminal with the live screen
        m.Cylinder(steelPolished,new Vector3(30f,bt+.05f,-17.80f),.07f,.015f,24);m.Cylinder(steelPolished,new Vector3(30f,bt+.065f,-17.80f),.015f,.16f,12);
        RB(black,29.83f,30.17f,bt+.18f,bt+.42f,-17.86f,-17.82f,.008f,false,2);
        kioskR.Add(Screen(scrGroup,"terminal bar",scrKiosk,new Vector3(30f,bt+.30f,-17.874f),.30f,.20f,0f));
        // stools: the owner's leather bar stools (NosTeam) - or the code stool; every one a seat
        for(float x=23f;x<=47.01f;x+=1.0f)
        {
            var o=Owner(g,"bar_hoker_skora_nosteam","bar: hoker skórzany",x,FFL,bz0-.65f,0f,.99f);
            if(o){Col(x-.21f,x+.21f,FFL,FFL+.78f,bz0-.86f,bz0-.44f,"hoker");Seat(x,FFL+.78f,bz0-.65f,Vector3.forward,"HOKER");}
            else Stool(x,bz0-.65f,.78f,Vector3.forward);
        }
        // pendants over the bar: the owner's bar lamps (Mark Peters) - or the Poly Haven pendant; a cord to the ceiling
        for(float x=23f;x<=47.01f;x+=2f)
        {
            float y=FFL+2.05f,top=y+.40f;
            var o=Owner(g,"bar_lampa_markpeters","bar: lampa wisząca",x,y,-17.95f,0f,.35f);
            if(o)top=TopOf(o);
            else if(Prop(g,"HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf",new Vector3(x,y,-17.95f),0f,true))top=y+.95f;
            else{m.Lathe(black,new Vector3(x,y,-17.95f),new[]{new Vector2(.20f,0),new Vector2(.19f,.08f),new Vector2(.12f,.22f),new Vector2(.03f,.26f),new Vector2(0,.27f)},32,Quaternion.identity,false,true);
                 m.Cylinder(lampWarm,new Vector3(x,y-.004f,-17.95f),.185f,.006f,32,Quaternion.identity,false,true);top=y+.27f;}
            if(top<Ceiling-.05f)m.Cylinder(black,new Vector3(x,top,-17.95f),.004f,Ceiling-top,6);
            FinishItems++;
        }
        Debug.Log("E1 bar: owner models="+OwnerModels+" (pack "+(OwnerPackHere()?"restored":"MISSING - code / Poly Haven stand-ins")+"), turned bottles="+TurnedBottles);
    }
    // windupbird's back-bar units along the back counter (as R1's NOVA club): bottle height 0.33 m, the units' long side along
    // the wall, fronts to the room (-z), backs 30 mm before the mirror; returns the number of units placed
    static int OwnerShelves(Transform g,float y0,float xa,float xb,float zBack,float yTop)
    {
        if(!OwnerPackHere())return 0;
        var src=BraxMiastoAssety.Model(OwnerPack,"bar_polki_butelki_windupbird");if(!src){OwnerMissing++;return 0;}
        try
        {
            var tmp=(GameObject)Object.Instantiate(src);tmp.transform.position=Vector3.zero;tmp.transform.rotation=src.transform.localRotation;
            var hs=new List<float>();
            foreach(var t in tmp.GetComponentsInChildren<Transform>(true))
                if(t!=tmp.transform&&(t.name.StartsWith("Object")||t.name.StartsWith("Box"))){float h=BraxMiastoAssety.Granice(t.gameObject).size.y;if(h>1e-4f)hs.Add(h);}
            var b0=BraxMiastoAssety.Granice(tmp);Object.DestroyImmediate(tmp);
            float maxB=0f;foreach(var h in hs)maxB=Mathf.Max(maxB,h);if(hs.Count<3)maxB=b0.size.y*.2f;
            float s=Mathf.Min(.33f*K/maxB,(yTop-y0)*K/Mathf.Max(1e-4f,b0.size.y));
            bool longX=b0.size.x>=b0.size.z;float yaw=longX?180f:270f;
            float wr=Mathf.Max(b0.size.x,b0.size.z)*s/K,L=xb-xa;
            int n=Mathf.Clamp(Mathf.FloorToInt((L+.1f)/(wr+.1f)),1,16);float step=L/n;
            for(int i=0;i<n;i++)
            {
                var o=(GameObject)Object.Instantiate(src,g,false);o.name="E1 bar | regał z butelkami "+(i+1)+" | bar_polki_butelki_windupbird (biblioteka właściciela, CC BY)";
                o.transform.position=Vector3.zero;o.transform.rotation=root.rotation*Quaternion.Euler(0,yaw,0)*src.transform.localRotation;
                o.transform.localScale=src.transform.localScale*s;
                var b=BraxMiastoAssety.Granice(o);var t=root.TransformPoint(new Vector3(xa+step*(i+.5f),y0,zBack)*K);
                o.transform.position+=new Vector3(t.x-b.center.x,t.y-b.min.y,t.z-b.max.z);
                BraxMiastoAssety.Przygotuj(o,true,true);OwnerModels++;
            }
            Debug.Log("E1 bar: owner's back-bar units x"+n+" (width "+wr.ToString("F2")+" m, scale "+s.ToString("F3")+")");
            return n;
        }
        catch(System.Exception e){Debug.LogWarning("E1 bar shelves from the owner's model DO POPRAWKI: "+e.Message);return 0;}
    }

    // ================================================================ LOUNGE by the glass to the sea (x 10..60, z -24.7..-19)
    static void Lounge()
    {
        var g=Group(root,"E1 lounge | Chesterfield, pufy skórzane, stoliki marmur (Poly Haven CC0)");
        for(int k=0;k<6;k++)
        {
            float cx=14.5f+k*7.6f,tz=-21.7f,rf=FFL;
            foreach(float s in new[]{-1f,1f})
            {
                float sz=tz+s*1.55f;var face=s<0?Vector3.forward:Vector3.back;
                if(!Prop(g,"ArenaLobby/sofa_02/sofa_02_2k.gltf",new Vector3(cx,rf,sz),s<0?0f:180f,true))
                    Sofa(leatherBlack,new Vector3(cx,0,tz+s*2.05f),Vector3.right,face,2.2f,2,true,quiltRed);
                Col(cx-1.1f,cx+1.1f,FFL,FFL+.82f,Mathf.Min(tz+s*1.05f,tz+s*2.05f),Mathf.Max(tz+s*1.05f,tz+s*2.05f),"sofa chesterfield");
                foreach(float u in new[]{-.5f,.5f})Seat(cx+u,FFL+.47f,sz,face,"SOFA");
            }
            if(!Prop(g,"HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf",new Vector3(cx,rf,tz),0f,true,.6f))
            {m.Cylinder(chrome,new Vector3(cx,FFL,tz),.30f,.02f,32);m.Cylinder(chrome,new Vector3(cx,FFL+.02f,tz),.06f,.38f,20);Oval(marbleWhite,new Vector3(cx,FFL+.40f,tz),.62f,.62f,.04f);}
            foreach(float s in new[]{-1f,1f})
            {
                // round pouf in the quilted oxblood leather on a polished plinth (Poly Haven's ottoman is brown: not here)
                var pc=new Vector3(cx+s*1.75f,rf,tz);
                m.Lathe(steelPolished,pc,new[]{new Vector2(0,0),new Vector2(.30f,0),new Vector2(.30f,.025f),new Vector2(0,.025f)},40,Quaternion.identity,false,true);
                m.Lathe(quiltRed,pc+Vector3.up*.025f,new[]{new Vector2(0,0),new Vector2(.32f,0),new Vector2(.345f,.03f),new Vector2(.355f,.20f),new Vector2(.345f,.37f),new Vector2(.31f,.405f),new Vector2(.20f,.42f),new Vector2(0,.425f)},40,Quaternion.identity,false,true);
                Col(pc.x-.35f,pc.x+.35f,FFL,FFL+.45f,tz-.35f,tz+.35f,"pufa");Seat(pc.x,FFL+.45f,tz,new Vector3(-s,0,0),"PUFA");
            }
            FinishItems++;
        }
    }

    // ================================================================ TOILETS M / K (x a..b, z 15.1..24.7)
    static void Toilets()
    {
        const float z0=15.1f,z1=24.7f,t=.012f,yc=FFL+2.85f,front=23.0f;
        foreach(var (a,b,men,tag) in new[]{(-22.4f,-15.1f,true,"M"),(-14.9f,-7.6f,false,"K")})
        {
            float wa=a+t,wb=b-t;
            // floor: black marble slabs; walls: white marble slabs, the vanity wall in black marble (12 mm, to above the ceiling)
            B(hallFloor,a,b,SlabTop,FFL,z0,z1);
            FaceRun(marbleWallBlack,true,a,1f,z0,z1,FFL,yc+.30f,t);
            FaceRun(marbleWallWhite,true,b,-1f,z0,z1,FFL,yc+.30f,t);
            FaceRun(marbleWallWhite,false,z0,1f,wa,wb,FFL,yc+.30f,t);
            FaceRun(marbleWallWhite,false,z1,-1f,wa,wb,FFL+1.12f,yc+.30f,t);
            // dropped ceiling: white, a 15 cm shadow gap round it with the light cove (warm LED line above the gap)
            B(ceilWhite,wa+.15f,wb-.15f,yc,yc+.05f,z0+t+.15f,z1-t-.15f);
            B(lampWarm,wa,wb,yc+.12f,yc+.135f,z0+t,z0+t+.15f);B(lampWarm,wa,wb,yc+.12f,yc+.135f,z1-t-.15f,z1-t);
            B(lampWarm,wa,wa+.15f,yc+.12f,yc+.135f,z0+t+.15f,z1-t-.15f);B(lampWarm,wb-.15f,wb,yc+.12f,yc+.135f,z0+t+.15f,z1-t-.15f);
            B(ceilWhite,wa,wb,yc+.135f,yc+.16f,z0+t,z1-t);                                                     // the cove's top
            // light slots: over the vanity, over the cubicle fronts, over the urinals / the room
            void Slot(float xa,float xb,float za,float zb){B(black,xa,xb,yc-.03f,yc,za,zb);B(lampWarm,xa+.015f,xb-.015f,yc-.042f,yc-.03f,za+.015f,zb-.015f);FinishItems++;}
            Slot(wa+.38f,wa+.48f,16.1f,20.3f);Slot(wa+.6f,wb-.6f,front-.75f,front-.65f);Slot(wb-.75f,wb-.65f,16.3f,20.4f);
            Sprinkler((a+b)*.5f,yc,19.5f);Smoke((a+b)*.5f+.8f,yc,17.5f);
            // vanity: floating white marble slab 12 cm on hidden brackets, warm light under it, three stone vessel basins,
            // wall spouts, pump dispensers
            float vz0=16.0f,vz1=20.4f,vt=FFL+.92f;
            RB(marbleWhite,wa,wa+.56f,vt-.12f,vt,vz0,vz1,.012f,true,2);
            B(lampWarm,wa+.08f,wa+.50f,vt-.134f,vt-.12f,vz0+.10f,vz1-.10f);
            for(int k=0;k<3;k++)
            {
                float bz=vz0+.75f+k*1.45f,bx=wa+.30f;
                m.Lathe(stoneBasin,new Vector3(bx,vt,bz),new[]{new Vector2(.09f,0),new Vector2(.16f,.012f),new Vector2(.198f,.05f),new Vector2(.21f,.10f),new Vector2(.21f,.12f),
                    new Vector2(.197f,.12f),new Vector2(.19f,.075f),new Vector2(.14f,.035f),new Vector2(.035f,.024f),new Vector2(0,.024f)},40,Quaternion.identity,false,false);
                m.Cylinder(steelPolished,new Vector3(bx,vt+.024f,bz),.032f,.003f,20,Quaternion.identity,true,false);
                // wall spout under the mirror: rosette, spout over the bowl, the lever above
                var w0=new Vector3(wa,vt+.22f,bz);
                m.Cylinder(steelPolished,w0,.032f,.012f,24,Quaternion.Euler(0,0,-90),true,false);
                m.Tube(steelPolished,w0+Vector3.right*.01f,w0+Vector3.right*.25f,.011f,14);
                m.Cylinder(steelPolished,w0+new Vector3(.25f,-.02f,0),.012f,.02f,12,Quaternion.identity,false,true);
                m.Cylinder(steelPolished,w0+new Vector3(0,.10f,0),.022f,.012f,20,Quaternion.Euler(0,0,-90),true,false);
                m.Tube(steelPolished,w0+new Vector3(.012f,.10f,0),w0+new Vector3(.07f,.13f,0),.006f,8);
                // pump dispenser on the slab
                var dp=new Vector3(wa+.12f,vt,bz+.36f);
                m.Lathe(steelPolished,dp,new[]{new Vector2(0,0),new Vector2(.032f,0),new Vector2(.034f,.01f),new Vector2(.034f,.15f),new Vector2(.03f,.165f),new Vector2(.012f,.17f),new Vector2(.01f,.20f),new Vector2(0,.20f)},20,Quaternion.identity,true,true);
                m.Tube(steelPolished,dp+Vector3.up*.195f,dp+new Vector3(.055f,.19f,0),.006f,8);
            }
            // backlit live mirror 4.4 x 1.1 m, glass 30 mm off the marble, a warm halo round it
            float my0=vt+.35f,my1=my0+1.10f;
            B(lampWarm,wa,wa+.012f,my0-.02f,my1+.02f,vz0-.02f,vz1+.02f);
            LiveMirror("WC "+tag+" vanity",new Vector3(wa+.042f,(my0+my1)*.5f,(vz0+vz1)*.5f),Vector3.right,vz1-vz0,my1-my0,1024,256,9f,12f);
            // recessed stainless towel / waste unit and a hand dryer by the vanity
            RB(steelPolished,wa,wa+.02f,FFL+.25f,FFL+1.65f,21.0f,21.5f,.006f,true,2);
            B(black,wa+.02f,wa+.032f,FFL+1.42f,FFL+1.45f,21.08f,21.42f);B(black,wa+.02f,wa+.032f,FFL+.55f,FFL+.75f,21.08f,21.42f);
            m.RoundedBox(steelPolished,new Vector3(wa+.10f,FFL+1.05f,22.05f),new Vector3(.20f,.62f,.30f),.05f,3);
            // cubicles: full-height black lacquer partitions on polished feet, full-height doors that open, wall-hung pans
            // on the black marble duct with the flush plate, a paper holder, brush, bin, hook in every cubicle
            const int n=5;float w=(b-a-.2f)/n;
            B(marbleWallBlack,wa,wb,FFL,FFL+1.12f,24.45f,z1,true);
            RB(marbleWhite,wa,wb,FFL+1.12f,FFL+1.15f,24.42f,z1,.006f,false,2);
            for(int i=0;i<=n;i++)
            {
                float px=a+.1f+i*w;
                RB(lacquer,px-.02f,px+.02f,FFL+.12f,FFL+1.16f,front,24.43f,.008f,true,2);
                RB(lacquer,px-.02f,px+.02f,FFL+1.16f,FFL+2.45f,front,z1-t,.008f,true,2);
                m.Cylinder(steelPolished,new Vector3(px,FFL,front+.10f),.022f,.12f,14);m.Cylinder(steelPolished,new Vector3(px,FFL,24.3f),.022f,.12f,14);
            }
            for(int i=0;i<n;i++)
            {
                float cx=a+.1f+(i+.5f)*w;
                RB(lacquer,cx-w*.5f+.02f,cx-.36f,FFL+.12f,FFL+2.45f,front-.02f,front+.02f,.008f,true,2);
                RB(lacquer,cx+.36f,cx+w*.5f-.02f,FFL+.12f,FFL+2.45f,front-.02f,front+.02f,.008f,true,2);
                RB(lacquer,cx-.36f,cx+.36f,FFL+2.33f,FFL+2.45f,front-.02f,front+.02f,.008f,false,2);
                Door("E1_WC_"+tag+"_"+(i+1),"E1 WC "+tag+" kabina "+(i+1),cx,FFL+.12f,front,0f,.70f,2.20f,false,lacquer);
                Toilet(cx,24.45f);
                m.Cylinder(steelPolished,new Vector3(cx+w*.5f-.02f,FFL+.75f,24.05f),.055f,.12f,20,Quaternion.Euler(0,0,90),true,true);
                m.Cylinder(steelPolished,new Vector3(cx-w*.5f+.14f,FFL,24.2f),.05f,.38f,16,Quaternion.identity,true,false);
                m.Cylinder(steelPolished,new Vector3(cx+w*.5f-.17f,FFL,24.25f),.09f,.30f,20,Quaternion.identity,true,false);
                m.Cylinder(steelPolished,new Vector3(cx,FFL+1.75f,front+.02f),.012f,.06f,10,Quaternion.Euler(90,0,0),true,false);
                FinishItems++;
            }
            if(men)for(int k=0;k<3;k++)
            {
                float uz=16.6f+k*.9f;Urinal(wb,uz,-1f);
                RB(lacquer,wb-.55f,wb,FFL+.55f,FFL+1.65f,uz+.43f,uz+.47f,.008f,true,2);
            }
            else m.RoundedBox(fixture,new Vector3(wb-.06f,FFL+.95f,17.0f),new Vector3(.12f,.55f,.85f),.04f,3);    // baby changing unit (folded)
            Plant(Group(root,"E1 WC "+tag+" roślina"),wa+.42f,z0+.48f,false);
        }
    }

    // ================================================================ linear light slots on the hall ceilings
    static void Slots(float x0,float x1,float z0,float z1,float y,float step,bool alongX,System.Func<float,float,bool> free=null)
    {
        void Slot(float a,float b,float c)
        {
            if(alongX){B(black,a,b,y-.03f,y,c-.05f,c+.05f);B(lampWarm,a+.02f,b-.02f,y-.042f,y-.03f,c-.03f,c+.03f);}
            else{B(black,c-.05f,c+.05f,y-.03f,y,a,b);B(lampWarm,c-.03f,c+.03f,y-.042f,y-.03f,a+.02f,b-.02f);}
            FinishItems++;
        }
        float u0=alongX?x0:z0,u1=alongX?x1:z1,v0=alongX?z0:x0,v1=alongX?z1:x1;
        for(float v=v0+step*.5f;v<v1-.3f;v+=step)
        {
            float start=float.NaN;
            for(float u=u0+.4f;;u+=.3f)
            {
                bool inside=u<=u1-.4f,f=inside&&(free==null||(alongX?free(u,v):free(v,u)));
                if(f){if(float.IsNaN(start))start=u;}
                else{if(!float.IsNaN(start)){float e=u-.3f;if(e-start>=.6f)Slot(start,e,v);start=float.NaN;}if(!inside)break;}
            }
        }
        for(float x=x0+1.6f;x<x1;x+=3.2f)for(float z=z0+1.6f;z<z1;z+=3.2f)if(free==null||free(x+.45f,z-.45f))Sprinkler(x+.45f,y,z-.45f);
        for(float x=x0+3.5f;x<x1;x+=7f)for(float z=z0+3.5f;z<z1;z+=7f)if(free==null||free(x-.6f,z+.6f))Smoke(x-.6f,y,z+.6f);
    }
}

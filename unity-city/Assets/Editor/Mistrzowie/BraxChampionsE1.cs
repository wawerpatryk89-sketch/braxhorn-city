using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 19:18 (board "E1 CENTRUM ROZRYWKI - KRĘGLE•BILARD•STREFA ROZRYWKI", docs/zlecenia/plansze), 19:40 ("rób od
// podstaw E1 dokładnie, parking podziemny najlepiej"), 19:40 ("na taksówki przed E1"), 20:06 ("rób e1 dokładnie"). Built 1:1
// from the board in nominal metres (= real metres, x K once), on the plot by the promenade (x 530, z -13, 120 x 50 x 15 m):
//
// * FRONT to the promenade (south, road Z-70): ground band 0-5.4 m of graphite glass between black metal pilasters on the
//   10 m module, black metal panels above, vertical red LED lines on every pilaster, red light canopy line and parapet line (no gold);
//   the portal over the hall (15 m) with red 3D "E1" and white "CENTRUM ROZRYWKI", a 6 m canopy with downlights and a red
//   carpet; the owner's mural (his picture, upscaled 4K) flat on the wall at the west corner; KRĘGLE / BILARD / GRY / BAR /
//   LOUNGE 3D signs with their pictograms, one per module over the east wing; main entrance (two glass double doors) + two
//   side / emergency doors.
// * The games are PLAYED (owner 10.11 20:45-21:31): bowling (BraxChampionsKregle: real pins, gutters, pit, pinsetter, ten-pin
//   scoring on the lane screens and the scorer consoles), billiards (BraxChampionsBilard: 18 tables, real pockets, cue);
//   every screen shows something live (BraxChampionsE1Ekrany); furniture rounded, in real materials (velvet, leather,
//   walnut, marble, porcelain, stainless).
// * GROUND FLOOR to the board's plan (10 | 50 | 10 | 40 | 10 m across, 10 + 30 + 10 m deep): hall + reception (double height,
//   12 m) in the middle, BOWLING west (24 lanes in 12 pairs, real lane sizes, pins, pinsetters behind the masking units,
//   ball returns, settees, scoring screens, shoe counter, ball racks), BILLIARDS east (18 tables 9 ft, pendant lamps, cue
//   racks), BAR / LOUNGE along the glass to the sea, GAMES / VR at the back (arcades, air hockey, VR pods, claw machines),
//   offices, cloakrooms M / K, toilets M / K, store, plant rooms. Every door closes and opens on approach (BraxCityDoor).
// * B1 GARAGE under the whole building (~200 bays 3.1 x 4.95 m on 6.5 m aisles, columns on the 10 m grid, beams, LED
//   lines, accessible bays), ramp from road Z70 at the back (12 % average, 6 % ends, street mouth with R 3 m arcs), a
//   stair and a working lift (BraxWinda B1 <-> HOL) up into the hall.
// * SITE: forecourt in concrete pavers flush with the Z-70 sidewalk, granite planters with flowers, lamps, benches; a TAXI
//   LAY-BY cut into the Z-70 north sidewalk (3 m bay, 36 m straight, 7.94 m S-tapers of R 6 m, kerb rounded, open city kerb
//   along it: layout.E1_TAXI_*); the service yard at the back with the loading door and the ramp trench.
// No brown, no beige (lanes are pale silvered maple), nothing coplanar of two materials, nothing 1-5 mm apart (paint 8 mm,
// finishes 2 cm), colliders on every floor, wall, stair, ramp and piece of furniture you would walk into.
public static class BraxChampionsE1
{
    // ---- plan (local nominal metres: origin at the plot centre, +x east, +z north, front = -z) - layout.E1_* must match
    public const float CentreX=530f,CentreZ=-13f,HW=60f,HD=25f,Y0=.16f,FFL=.18f,Band=5.4f,Ceiling=6.0f,RoofIn=11.4f,Roof=12f,Parapet=12.6f,PortalTop=15f;
    public const float SiteX0=-64f,SiteX1=64f,SiteZ0=-40f,SiteZ1=66f;                  // world x 466..594, z -53..53
    public const float RoadEdgeS=-47f,BayDepth=3.0f,BayX0=-13f,BayX1=23f,BayTaper=7.94f,BayR=6f,KerbW=.18f;
    public const float RoadEdgeN=73f,RampX0=41f,RampX1=48f,RampTop=58f,RampBottom=27f,MouthR=3f;
    public const float B1=-3.60f,SlabBottom=-.30f,SlabTop=.06f;           // finishes SlabTop..FFL (gutters / pit recessed into them)
    public const float StairX0=.2f,StairRun=.28f,StairZ0=23.1f,StairZ1=24.7f;public const int Risers=22;
    public const float LiftX0=7.3f,LiftX1=9.9f,LiftZ0=21.9f,LiftZ1=24.7f,LiftDoorHalf=.5f;
    public const int Lanes=24,Tables=18;
    public const float LaneX0=-57.7f,PairPitch=3.48f,FoulZ=-13.0f,LaneLen=18.29f;
    const string Out="Assets/PlayableGenerated/Mistrzowie",Folder=Out+"/Meshes/E1";
    static float K=>BraxClaudeOsiedle.K;
    public static int Doors,Bays,Pins,BilliardTables,Colliders,Signs;

    static Transform root,cols;
    static BraxChampionsMeshBatch m;
    static Material black,blackMatte,chrome,redLed,lampWarm,purpleLed,magenta,red,whiteLit,facadeGlass,clearGlass,concrete,pavers,carpetRed,
        hallFloor,carpet,carpetGraphite,maple,gutter,felt,lacquer,velvet,charcoal,screen,whiteTile,techFloor,garageFloor,garageWhite,
        lineWhite,lineYellow,lineBlue,ledCool,marbleWhite,roofMembrane,hvac,asphalt,walk,antiSlip,rampWall,pinWhite,pinRed,
        cue,mirror,steelPolished,rubber,porcelain,muralBall;
    static Material[] bottles,houseBalls,poolBalls;
    static Material muralArt,scrBoard,scrKiosk,scrHockey;static Material[] scrArcade,scrLane;
    static List<Renderer> boardR=new List<Renderer>(),kioskR=new List<Renderer>(),arcadeR=new List<Renderer>(),hockeyR=new List<Renderer>();
    // the owner's mural (10.11 21:17, "zdjęcie to mural na ścianę, narożnik jak w projekcie"): his 2048 x 768 picture upscaled x2
    // by Real-ESRGAN (RealESRGAN_x2plus) to 4096 x 1536; the original is in docs/zlecenia/plansze
    public const string MuralPath="Assets/Mistrzowie/Textures/E1/e1_mural_owner_4096.png";

    // ================================================================ materials
    static Material S(string id,Color c,float r,float met,Color? e=null,bool t=false)=>BraxChampionsHarbour.Surface("e1_"+id,c,r,met,e,t);
    static Material Tex(string id,string slug,float metres,Color tint)=>BraxChampionsFoundation.SourceMaterial("e1_"+id,slug,metres,tint);
    static Material Inside(Material x,float a){BraxChampionsInteriorLight.Apply(x,a);return x;}
    // Owner 10.11 20:50: "obijać wszystko materiałami prawdziwymi, nic plastikowego, sztucznego" and "przede wszystkim to
    // ma wyglądać". Every surface you touch or sit on is a real scanned PBR map (Poly Haven / ambientCG, prepare_assets.py):
    // brushed black metal on the facade, dark painted plaster inside, black lacquered walnut on the tables and the bar,
    // plum velvet on the bowling settees, black leather in the lounge, woven wool carpet, billiard cloth, maple lanes,
    // marble, concrete. The warm maps are tinted to neutral (the region rule: no brown, no beige); only LEDs, screens and
    // paint are plain colours.
    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!BraxChampionsPromenadeFurniture.Teak)BraxChampionsPromenadeFurniture.Materials();
        black=BraxChampionsFoundation.LibraryMetal("e1_black_brushed_metal",new Color(.075f,.078f,.088f));          // board: "Panel metalowy (czarny mat)"
        blackMatte=Inside(Tex("interior_dark_plaster","white_stucco_02",2.2f,new Color(.15f,.15f,.18f)),BraxChampionsInteriorLight.Walls);
        // owner 10.11 21:51-21:52: "całkowity brak złotego koloru ... w tym projekcie nie ma złotego, to jest światło" and "może
        // być w tym miejscu czerwony świecący": no gold anywhere - the board's lines are LIGHT, here glowing red (the E1 red);
        // hardware that was stainless is polished stainless, lamps give warm white light
        // the library metal map is warm (reads cream / beige): hardware is a neutral polished stainless surface, as the marina's
        chrome=S("polished_stainless_hardware",new Color(.80f,.82f,.86f),.18f,.90f);
        redLed=S("red_led_line",new Color(.92f,.06f,.08f),.4f,0f,new Color(1.70f,.10f,.12f));
        lampWarm=S("lamp_warm_white_diffuser",new Color(1f,.97f,.92f),.4f,0f,new Color(1.25f,1.12f,.95f));
        purpleLed=S("purple_led",new Color(.62f,.22f,.95f),.4f,0f,new Color(.80f,.22f,1.20f));
        magenta=S("mural_magenta_panel",new Color(.46f,.06f,.42f),.35f,.15f,new Color(.55f,.05f,.50f));
        red=S("red_composite_letters",new Color(.85f,.05f,.07f),.30f,.20f,new Color(.70f,.02f,.03f)); // "Panel kompozytowy (czerwony)"
        whiteLit=S("white_backlit_letters",new Color(.96f,.96f,.97f),.30f,0f,new Color(.75f,.75f,.78f));
        facadeGlass=S("facade_glass_graphite",new Color(.20f,.24f,.28f,.40f),.05f,0f,null,true); // "Szkło fasadowe (przyciemniane)"
        clearGlass=S("clear_glass",new Color(.80f,.88f,.92f,.20f),.05f,0f,null,true);
        concrete=Tex("architectural_concrete","concrete_wall_003",2.4f,new Color(1.0f,1.12f,1.31f));      // the map is warm (206,184,157): neutral light grey
        pavers=Tex("forecourt_pavers","concrete_pavement_02",1.8f,new Color(1.0f,1.09f,1.21f));            // neutral grey, no beige
        carpetRed=Tex("red_carpet_wool","poly_wool_herringbone",.9f,new Color(.48f,.034f,.053f));
        hallFloor=Inside(BraxChampionsFoundation.LibraryMarble("e1_hall_black_marble",new Color(.92f,.92f,.95f),1.6f),.8f);
        carpet=Inside(Tex("bowling_carpet_wool","poly_wool_herringbone",.9f,new Color(.29f,.19f,.49f)),.85f);     // woven wool, deep violet
        carpetGraphite=Inside(Tex("billiard_carpet_wool","poly_wool_herringbone",.9f,new Color(.33f,.36f,.41f)),.85f);
        // maple lanes: laminate_floor_02 (155,128,99) lightened to pale natural maple, boards along the lane
        maple=Inside(Tex("lane_maple","laminate_floor_02",1.0f,new Color(1.32f,1.45f,1.62f)),.9f);
        gutter=Inside(BraxChampionsFoundation.LibraryMetal("e1_gutter_black_steel",new Color(.06f,.06f,.07f)),.75f);
        felt=Inside(Tex("billiard_cloth_green","poly_wool_herringbone",.35f,new Color(.25f,.90f,.49f)),.9f);       // worsted cloth, tournament green
        lacquer=Inside(Tex("black_lacquered_walnut","smoked_walnut_veneer",1.2f,new Color(.15f,.17f,.21f)),.75f);  // grain under black lacquer
        velvet=Inside(Tex("settee_plum_velvet","velour_velvet",.6f,new Color(.52f,.67f,1.55f)),.8f);               // (174,45,71) -> plum
        charcoal=Inside(Tex("lounge_black_leather","leather_white",.8f,new Color(.30f,.30f,.35f)),.8f);            // black hide
        screen=S("score_screen",new Color(.05f,.12f,.25f),.2f,0f,new Color(.18f,.48f,1.00f));
        whiteTile=Inside(Tex("wc_white_tiles","long_white_tiles",1.2f,new Color(1.0f,1.035f,1.13f)),BraxChampionsInteriorLight.Walls);
        techFloor=Inside(Tex("tech_concrete_floor","concrete_floor_02",3f,new Color(1.0f,1.09f,1.32f)),.8f);       // (120,110,91) -> neutral grey
        garageFloor=Inside(Tex("garage_floor","garage_floor",3f,new Color(1.0f,1.14f,1.43f)),.85f);              // (124,109,87) -> neutral grey
        // clean cool white (white_stucco_02 neutralised, as the hotel garage): no beige under the LED lines
        garageWhite=Inside(Tex("garage_white","white_stucco_02",2f,new Color(1.10f,1.14f,1.20f)),BraxChampionsInteriorLight.Ceilings);
        lineWhite=Inside(S("paint_white",new Color(.93f,.93f,.92f),.6f,0f),.85f);
        lineYellow=Inside(S("paint_yellow",new Color(.95f,.76f,.08f),.6f,0f),.85f);
        lineBlue=Inside(S("paint_accessible_blue",new Color(.10f,.32f,.72f),.6f,0f),.85f);
        ledCool=S("led_cool",new Color(1f,1f,1f),.3f,0f,new Color(1.15f,1.15f,1.20f));
        marbleWhite=Inside(BraxChampionsFoundation.LibraryMarbleWhite("e1_white_marble",new Color(.96f,.96f,.97f),2.0f),.8f);
        roofMembrane=S("roof_membrane_grey",new Color(.43f,.44f,.46f),.85f,0f);
        hvac=S("galvanised_steel_neutral",new Color(.58f,.60f,.63f),.45f,.80f);                           // the library map reads khaki
        antiSlip=Tex("ramp_anti_slip_concrete","anti_slip_concrete",2f,new Color(1.0f,1.11f,1.31f));
        rampWall=Tex("ramp_wall_concrete","concrete_wall_008",2.5f,new Color(1.0f,1.05f,1.26f));
        pinWhite=Inside(S("pin_white_coating",new Color(.95f,.95f,.96f),.16f,0f),.85f);                    // lacquered maple pin
        pinRed=Inside(S("pin_red_neck",new Color(.80f,.05f,.06f),.18f,0f),.85f);
        cue=maple;
        mirror=Inside(S("mirror",new Color(.86f,.88f,.90f),.04f,1f),.8f);
        steelPolished=S("polished_steel_neutral",new Color(.78f,.80f,.84f),.22f,.88f);
        rubber=Inside(Tex("black_rubber_leather","leather_white",.4f,new Color(.10f,.10f,.11f)),.7f);
        porcelain=Inside(S("porcelain_white",new Color(.95f,.95f,.94f),.12f,0f),.85f);
        var bc=new[]{new Color(.15f,.45f,.25f,.6f),new Color(.55f,.62f,.70f,.5f),new Color(.60f,.10f,.12f,.6f),new Color(.85f,.86f,.88f,.45f)};
        bottles=new Material[4];
        for(int i=0;i<4;i++)bottles[i]=S("bottle_"+i,bc[i],.05f,0f,new Color(bc[i].r*.25f,bc[i].g*.25f,bc[i].b*.25f),true);
        asphalt=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/asphalt.mat");
        if(!asphalt)asphalt=BraxChampionsFoundation.SourceMaterial("e1_asphalt","clean_asphalt",2.1f,Color.white);
        walk=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/paving.mat");
        if(!walk)walk=BraxChampionsFoundation.SourceMaterial("e1_city_paving","concrete_pavement_02",1.8f,Color.white);
        muralBall=S("ball_red_resin",new Color(.62f,.03f,.06f),.10f,.20f);
        BallMaterials();
        ScreenMaterials();
        // plush prizes in the claw machines: velour in four colours
        var pt=new[]{new Color(1.40f,1.60f,1.80f),new Color(.45f,2.0f,3.0f),new Color(1.45f,4.6f,.90f),new Color(1.40f,5.2f,3.4f)};
        plush=new Material[pt.Length];for(int i=0;i<pt.Length;i++)plush[i]=Inside(Tex("plush_"+i,"velour_velvet",.25f,pt[i]),.85f);
        // air hockey field: white laminate with the air holes, red centre line and circles, blue lines, the goal creases
        var field=Paint("air_hockey_field",256,512,(u,v)=>
        {
            float x=(u-.5f)*1.10f,z=(v-.5f)*2.30f;var c=new Color(.93f,.94f,.96f);
            float hu=u*20f-Mathf.Floor(u*20f)-.5f,hv=v*42f-Mathf.Floor(v*42f)-.5f;if(hu*hu+hv*hv<.035f)c=new Color(.55f,.57f,.62f);
            float rc=Mathf.Sqrt(x*x+z*z);
            if(Mathf.Abs(z)<.006f||Mathf.Abs(rc-.20f)<.006f)c=new Color(.82f,.08f,.10f);
            if(Mathf.Abs(Mathf.Abs(z)-.45f)<.005f)c=new Color(.10f,.25f,.75f);
            foreach(float e in new[]{-1.15f,1.15f}){float d=Mathf.Sqrt(x*x+(z-e)*(z-e));if(Mathf.Abs(d-.24f)<.006f)c=new Color(.82f,.08f,.10f);}
            c.a=1f;return c;
        });
        airField=Inside(S("air_hockey_field",Color.white,.22f,0f),.9f);airField.SetTexture("_MainTex",field);EditorUtility.SetDirty(airField);
        // the mural: the owner's print, lit a little (a printed aluminium panel on the north facade, always in shade)
        var art=AssetDatabase.LoadAssetAtPath<Texture2D>(MuralPath);
        muralArt=S("mural_owner_print",Color.white,.55f,0f,new Color(.30f,.30f,.30f));
        if(art){muralArt.SetTexture("_MainTex",art);muralArt.SetTexture("_EmissionMap",art);EditorUtility.SetDirty(muralArt);}
        else Debug.LogWarning("E1 mural: "+MuralPath+" missing");
    }
    // the screens' starting pictures (BraxChampionsPiksele, the same painters the runtime repaints them with)
    static Texture2D Pixels(string name,int w,int h,Color32[] px)
    {
        var tex=new Texture2D(w,h,TextureFormat.RGBA32,true){name="E1 "+name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
        tex.SetPixels32(px);tex.Apply(true);
        System.IO.Directory.CreateDirectory(Out+"/Textures");
        string path=Out+"/Textures/e1_"+name+".asset";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tex,path);
        return tex;
    }
    static Material ScreenMat(string id,Texture2D t)
    {
        var mt=S("screen_"+id,Color.white,.18f,0f,new Color(1.05f,1.05f,1.10f));
        mt.SetTexture("_MainTex",t);mt.SetTexture("_EmissionMap",t);EditorUtility.SetDirty(mt);return mt;
    }
    static void ScreenMaterials()
    {
        var tory=new int[Lanes];var stoly=new int[Tables];for(int i=0;i<Lanes;i++)tory[i]=-1;for(int i=0;i<Tables;i++)stoly[i]=-1;
        scrLane=new Material[Lanes];
        for(int l=0;l<Lanes;l++){var px=new Color32[BraxChampionsPiksele.TorW*BraxChampionsPiksele.TorH];BraxChampionsPiksele.TabelaToru(px,l,new int[21],0);
            scrLane[l]=ScreenMat("lane_"+(l+1),Pixels("screen_lane_"+(l+1),BraxChampionsPiksele.TorW,BraxChampionsPiksele.TorH,px));}
        var b=new Color32[BraxChampionsPiksele.TablicaW*BraxChampionsPiksele.TablicaH];BraxChampionsPiksele.Tablica(b,tory,null,stoly,"");
        scrBoard=ScreenMat("status_board",Pixels("screen_status_board",BraxChampionsPiksele.TablicaW,BraxChampionsPiksele.TablicaH,b));
        var k=new Color32[BraxChampionsPiksele.KioskW*BraxChampionsPiksele.KioskH];BraxChampionsPiksele.Kiosk(k,tory,stoly);
        scrKiosk=ScreenMat("kiosk",Pixels("screen_kiosk",BraxChampionsPiksele.KioskW,BraxChampionsPiksele.KioskH,k));
        scrArcade=new Material[5];
        for(int v=0;v<5;v++){var a=new Color32[BraxChampionsPiksele.AutomatW*BraxChampionsPiksele.AutomatH];BraxChampionsPiksele.Automat(a,v,v*7);
            scrArcade[v]=ScreenMat("arcade_"+(v+1),Pixels("screen_arcade_"+(v+1),BraxChampionsPiksele.AutomatW,BraxChampionsPiksele.AutomatH,a));}
        var h=new Color32[BraxChampionsPiksele.CymbW*BraxChampionsPiksele.CymbH];BraxChampionsPiksele.Cymbergaj(h,0,0);
        scrHockey=ScreenMat("air_hockey",Pixels("screen_air_hockey",BraxChampionsPiksele.CymbW,BraxChampionsPiksele.CymbH,h));
    }
    // a screen face: unit quad (uv 0..1) facing -z turned by yaw, scaled to w x h; its own renderer (the runtime repaints it)
    static Mesh unitQuad;static Transform scrGroup;
    static MeshRenderer Screen(Transform parent,string name,Material mat,Vector3 c,float w,float h,float yaw)
    {
        string path=Folder+"/e1_screen_unit_quad.asset";
        if(!unitQuad)unitQuad=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!unitQuad)
        {
            unitQuad=new Mesh{name="E1 screen unit quad"};
            unitQuad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            unitQuad.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            unitQuad.triangles=new[]{0,2,1,0,3,2};unitQuad.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
            unitQuad.RecalculateTangents();unitQuad.RecalculateBounds();AssetDatabase.CreateAsset(unitQuad,path);
        }
        var g=new GameObject(name);g.transform.SetParent(parent,false);
        g.transform.localPosition=c*K;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=new Vector3(w*K,h*K,1f);
        g.AddComponent<MeshFilter>().sharedMesh=unitQuad;var mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;
        mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return mr;
    }

    // ================================================================ helpers (nominal, E1-local)
    static void B(Material mat,float x0,float x1,float y0,float y1,float z0,float z1,bool col=false)
    {
        if(x1-x0<1e-4f||y1-y0<1e-4f||z1-z0<1e-4f)return;
        m.BoxMinMax(mat,x0,x1,y0,y1,z0,z1);
        if(col)Col(x0,x1,y0,y1,z0,z1);
    }
    static void Col(float x0,float x1,float y0,float y1,float z0,float z1,string name="kolizja")
    {
        BraxChampionsMeshBatch.Collider(cols,name,new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0));
        Colliders++;
    }
    // a box from a (low end) to b (high end) along z, width w, thickness t under the line: ramps
    static void SlopeZ(Material mat,float x0,float x1,float za,float ya,float zb,float yb,float t,bool col=true)
    {
        float len=Mathf.Sqrt((zb-za)*(zb-za)+(yb-ya)*(yb-ya)),pitch=-Mathf.Atan2(yb-ya,zb-za)*Mathf.Rad2Deg;
        var rot=Quaternion.Euler(pitch,0,0);var up=rot*Vector3.up;
        var c=new Vector3((x0+x1)*.5f,(ya+yb)*.5f,(za+zb)*.5f)-up*(t*.5f);
        m.Box(mat,c,new Vector3(x1-x0,t,len),rot);
        if(!col)return;
        var o=new GameObject("rampa | kolizja");o.transform.SetParent(cols,false);
        o.transform.localPosition=c*K;o.transform.localRotation=rot;
        o.AddComponent<BoxCollider>().size=new Vector3(x1-x0,t,len)*K;Colliders++;
    }
    // quad with its front face along n
    static void Q(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)m.Quad(mat,d,c,b,a);else m.Quad(mat,a,b,c,d);
    }
    static void T(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)m.Triangle(mat,a,c,b);else m.Triangle(mat,a,b,c);
    }
    static void Sphere(Material mat,Vector3 c,float r,int lon=12,int lat=8)
    {
        for(int i=0;i<lat;i++)
        {
            float a0=-90f+180f*i/lat,a1=-90f+180f*(i+1)/lat;
            for(int j=0;j<lon;j++)
            {
                float b0=360f*j/lon,b1=360f*(j+1)/lon;
                Vector3 P(float a,float b)=>c+new Vector3(Mathf.Cos(a*Mathf.Deg2Rad)*Mathf.Cos(b*Mathf.Deg2Rad),Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad)*Mathf.Sin(b*Mathf.Deg2Rad))*r;
                var p00=P(a0,b0);var p01=P(a0,b1);var p10=P(a1,b0);var p11=P(a1,b1);var n=((p00+p11)*.5f-c);
                if(i==0)T(mat,p00,p11,p10,n);else if(i==lat-1)T(mat,p00,p01,p10,n);else Q(mat,p00,p01,p11,p10,n);
            }
        }
    }
    // bowling pin (38.1 cm, 12.1 cm belly) scaled by s, standing on p; two red neck stripes
    static readonly float[] PinH={0f,.02f,.06f,.10f,.14f,.19f,.235f,.25f,.265f,.285f,.31f,.335f,.36f,.381f};
    static readonly float[] PinR={.026f,.040f,.052f,.0585f,.0605f,.056f,.042f,.035f,.030f,.024f,.026f,.031f,.028f,0f};
    static void Pin(Vector3 p,float s,int sides=8)
    {
        for(int i=0;i+1<PinH.Length;i++)
        {
            var mat=(i==7||i==9)?pinRed:pinWhite;
            m.Cylinder(mat,p+Vector3.up*PinH[i]*s,PinR[i]*s,(PinH[i+1]-PinH[i])*s,sides,Quaternion.identity,i==PinH.Length-2,i==0,Mathf.Max(PinR[i+1]*s,.0001f));
        }
        Pins++;
    }
    static Transform Group(Transform parent,string name){var g=new GameObject(name).transform;g.SetParent(parent,false);return g;}
    static void Flush(string name,Transform parent=null){m.Build(parent??root,name,Folder,false);m=new BraxChampionsMeshBatch();}

    // wall along x at z (thickness t centred), gaps (a,b,top)
    static void WallX(Material mat,float z,float x0,float x1,float y0,float y1,float t,params (float a,float b,float h)[] gaps)
    {
        float x=x0;
        foreach(var g in gaps){B(mat,x,g.a,y0,y1,z-t*.5f,z+t*.5f,true);B(mat,g.a,g.b,g.h,y1,z-t*.5f,z+t*.5f,true);x=g.b;}
        if(y0==FFL)B(mat,x0,x1,SlabTop,FFL,z-t*.5f,z+t*.5f);
        B(mat,x,x1,y0,y1,z-t*.5f,z+t*.5f,true);
    }
    static void WallZ(Material mat,float x,float z0,float z1,float y0,float y1,float t,params (float a,float b,float h)[] gaps)
    {
        float z=z0;
        foreach(var g in gaps){B(mat,x-t*.5f,x+t*.5f,y0,y1,z,g.a,true);B(mat,x-t*.5f,x+t*.5f,g.h,y1,g.a,g.b,true);z=g.b;}
        if(y0==FFL)B(mat,x-t*.5f,x+t*.5f,SlabTop,FFL,z0,z1);
        B(mat,x-t*.5f,x+t*.5f,y0,y1,z,z1,true);
    }

    // ---- rounded furniture (owner 10.11 20:45: "nie rób wszystkich kwadratowych, tylko zaokrąglone - podłokietniki")
    static void RB(Material mat,float x0,float x1,float y0,float y1,float z0,float z1,float r,bool col=false,int seg=4)
    {
        if(x1-x0<1e-4f||y1-y0<1e-4f||z1-z0<1e-4f)return;
        m.RoundedBoxMinMax(mat,x0,x1,y0,y1,z0,z1,r,seg);
        if(col)Col(x0,x1,y0,y1,z0,z1);
    }
    // oval / round slab (table tops, ottomans): side wall, top, underside
    static void Oval(Material mat,Vector3 c,float rx,float rz,float h,int sides=40)
    {
        var top=c+Vector3.up*h;
        for(int i=0;i<sides;i++)
        {
            float a0=i*Mathf.PI*2f/sides,a1=(i+1)*Mathf.PI*2f/sides;
            var d0=new Vector3(Mathf.Cos(a0)*rx,0,Mathf.Sin(a0)*rz);var d1=new Vector3(Mathf.Cos(a1)*rx,0,Mathf.Sin(a1)*rz);
            Q(mat,c+d0,c+d1,top+d1,top+d0,d0+d1);
            T(mat,top,top+d0,top+d1,Vector3.up);
            T(mat,c,c+d0,c+d1,Vector3.down);
        }
    }
    // a sofa: rounded frame, seat and back cushions, rolled arms, stainless legs. back = centre of the back edge on the floor,
    // along / face = unit axes (x or z), len along, depth .92
    static void Sofa(Material up,Vector3 back,Vector3 along,Vector3 face,float len,int seats,bool arms=true)
    {
        const float d=.92f,armW=.20f;
        bool ax=Mathf.Abs(along.x)>.5f;
        void Part(Material mat,float u0,float u1,float v0,float v1,float y0,float y1,float r)
        {
            var c=back+along*((u0+u1)*.5f)+face*((v0+v1)*.5f)+Vector3.up*((y0+y1)*.5f);
            var size=ax?new Vector3(u1-u0,y1-y0,v1-v0):new Vector3(v1-v0,y1-y0,u1-u0);
            m.RoundedBox(mat,c,size,r,4);
        }
        float L=len*.5f,inner=arms?L-armW:L;
        Part(up,-L+.02f,L-.02f,.02f,d-.02f,FFL+.10f,FFL+.30f,.045f);                                 // frame
        Part(up,-L,L,0f,.20f,FFL+.12f,FFL+.82f,.07f);                                              // back frame
        float w=2f*inner/seats;
        for(int i=0;i<seats;i++)
        {
            float u0=-inner+i*w+.006f,u1=-inner+(i+1)*w-.006f;
            Part(up,u0,u1,.20f,d-.01f,FFL+.29f,FFL+.45f,.06f);                                      // seat cushion
            Part(up,u0,u1,.16f,.36f,FFL+.45f,FFL+.86f,.09f);                                       // back cushion
        }
        if(arms)foreach(float sgn in new[]{-1f,1f})Part(up,sgn<0?-L:L-armW,sgn<0?-L+armW:L,0f,d,FFL+.10f,FFL+.62f,.095f);   // rolled arms
        foreach(float su in new[]{-1f,1f})foreach(float sv in new[]{.08f,d-.08f})
            m.Cylinder(chrome,back+along*(su*(L-.08f))+face*sv+Vector3.up*FFL,.024f,.10f,12,Quaternion.identity,false,false,.017f);
        var cc=back+along*0f+face*(d*.5f);var sz=ax?new Vector3(len,.86f,d):new Vector3(d,.86f,len);
        Col(cc.x-sz.x*.5f,cc.x+sz.x*.5f,FFL,FFL+.86f,cc.z-sz.z*.5f,cc.z+sz.z*.5f,"sofa");
    }
    // bar stool: stainless foot rest, chrome column, upholstered round seat with a low back
    static void Stool(float x,float z,float h,Vector3 back)
    {
        m.Cylinder(steelPolished,new Vector3(x,FFL,z),.23f,.025f,24,Quaternion.identity,true,false,.21f);
        m.Cylinder(steelPolished,new Vector3(x,FFL+.025f,z),.032f,h-.12f,14);
        m.Tube(chrome,new Vector3(x-.2f,FFL+.30f,z),new Vector3(x+.2f,FFL+.30f,z),.012f,8);           // foot rest
        m.Cylinder(charcoal,new Vector3(x,FFL+h-.11f,z),.19f,.07f,24,Quaternion.identity,true,true,.20f);
        m.Cylinder(charcoal,new Vector3(x,FFL+h-.04f,z),.20f,.04f,24,Quaternion.identity,true,false,.17f);
        if(back.sqrMagnitude>.01f)
        {
            var bc=new Vector3(x,FFL+h+.12f,z)+back*.17f;var rot=Quaternion.LookRotation(back);
            m.RoundedBox(charcoal,bc,new Vector3(.34f,.22f,.06f),.03f,rot,3);
        }
        Col(x-.21f,x+.21f,FFL,FFL+h,z-.21f,z+.21f,"hoker");
    }
    // office chair: 5-star base on castors, gas lift, rounded seat, back and armrests
    static void Chair(float x,float z,Vector3 face)
    {
        var c=new Vector3(x,FFL,z);
        for(int i=0;i<5;i++){float a=i*72f*Mathf.Deg2Rad;var e=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.30f;
            m.Tube(steelPolished,c+Vector3.up*.08f,c+e+Vector3.up*.06f,.018f,8);m.Cylinder(rubber,c+e,.025f,.05f,10);}
        m.Cylinder(steelPolished,c+Vector3.up*.06f,.025f,.38f,12);
        m.RoundedBox(charcoal,c+Vector3.up*.48f,new Vector3(.50f,.08f,.48f),.035f,Quaternion.LookRotation(face),3);
        m.RoundedBox(charcoal,c+Vector3.up*.80f-face*.24f,new Vector3(.46f,.52f,.07f),.03f,Quaternion.LookRotation(face),3);
        var right=Vector3.Cross(Vector3.up,face);
        foreach(float s in new[]{-1f,1f})m.RoundedBox(rubber,c+right*s*.26f+Vector3.up*.66f,new Vector3(.05f,.04f,.26f),.018f,Quaternion.LookRotation(face),3);
        Col(x-.3f,x+.3f,FFL,1.05f,z-.3f,z+.3f,"krzesło");
    }

    // upholstered bench: stainless feet, black walnut plinth, rounded leather cushion (along = x or z), seat at 45 cm
    static void Bench(Vector3 c,Vector3 along,float len,float depth=.48f,Material up=null)
    {
        up=up??charcoal;bool ax=Mathf.Abs(along.x)>.5f;
        Vector3 S3(float l,float h,float d)=>ax?new Vector3(l,h,d):new Vector3(d,h,l);
        m.RoundedBox(lacquer,new Vector3(c.x,FFL+.20f,c.z),S3(len-.08f,.20f,depth-.08f),.03f,3);
        m.RoundedBox(up,new Vector3(c.x,FFL+.375f,c.z),S3(len,.15f,depth),.06f,4);
        var side=ax?Vector3.forward:Vector3.right;
        foreach(float s in new[]{-1f,1f})foreach(float t in new[]{-1f,1f})
            m.Cylinder(chrome,new Vector3(c.x,FFL,c.z)+along*(s*(len*.5f-.12f))+side*(t*(depth*.5f-.10f)),.022f,.10f,12,Quaternion.identity,false,false,.016f);
        var sz=S3(len,.45f,depth);
        Col(c.x-sz.x*.5f,c.x+sz.x*.5f,FFL,FFL+.45f,c.z-sz.z*.5f,c.z+sz.z*.5f,"ławka");
    }
    // a curved slab (reception desk, lamp rims): annular sector rIn..rOut about c, angles a0..a1 (rad, 0 = +x, toward +z)
    static void Arc(Material mat,Vector3 c,float rIn,float rOut,float a0,float a1,float y0,float y1,int seg,bool ends=true)
    {
        Vector3 P(float r,float a,float y)=>new Vector3(c.x+Mathf.Cos(a)*r,y,c.z+Mathf.Sin(a)*r);
        for(int i=0;i<seg;i++)
        {
            float b0=a0+(a1-a0)*i/seg,b1=a0+(a1-a0)*(i+1)/seg;var mid=new Vector3(Mathf.Cos((b0+b1)*.5f),0,Mathf.Sin((b0+b1)*.5f));
            Q(mat,P(rIn,b0,y1),P(rIn,b1,y1),P(rOut,b1,y1),P(rOut,b0,y1),Vector3.up);
            Q(mat,P(rIn,b0,y0),P(rIn,b1,y0),P(rOut,b1,y0),P(rOut,b0,y0),Vector3.down);
            Q(mat,P(rOut,b0,y0),P(rOut,b1,y0),P(rOut,b1,y1),P(rOut,b0,y1),mid);
            if(rIn>1e-4f)Q(mat,P(rIn,b0,y0),P(rIn,b1,y0),P(rIn,b1,y1),P(rIn,b0,y1),-mid);
        }
        if(ends)foreach(float a in new[]{a0,a1})
        {
            var t=new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a))*(a==a0?-1f:1f);
            Q(mat,P(rIn,a,y0),P(rOut,a,y0),P(rOut,a,y1),P(rIn,a,y1),t);
        }
    }
    // half a round slab on the flat end of a counter: c on the end face, radius r, bulging toward outDir
    static void HalfRound(Material mat,Vector3 c,float r,Vector3 outDir,float y0,float y1,int seg=16)
    {
        var side=Vector3.Cross(Vector3.up,outDir).normalized;
        Vector3 P(float t,float y){float a=-Mathf.PI*.5f+Mathf.PI*t;return new Vector3(c.x,y,c.z)+(outDir*Mathf.Cos(a)+side*Mathf.Sin(a))*r;}
        var top=new Vector3(c.x,y1,c.z);var bot=new Vector3(c.x,y0,c.z);
        for(int i=0;i<seg;i++)
        {
            float t0=(float)i/seg,t1=(float)(i+1)/seg;
            T(mat,top,P(t0,y1),P(t1,y1),Vector3.up);T(mat,bot,P(t0,y0),P(t1,y0),Vector3.down);
            var mid=(P(t0,0)+P(t1,0))*.5f-new Vector3(c.x,0,c.z);
            Q(mat,P(t0,y0),P(t1,y0),P(t1,y1),P(t0,y1),mid);
        }
    }
    // a vessel basin standing on a counter (c = centre on the counter top): porcelain outer wall, rim, bowl, chrome drain
    static void Basin(Vector3 c,float rTop,float rBase,float h,int seg=36)
    {
        float rim=.012f,deep=h-.025f;
        Vector3 P(float r,float a,float y)=>new Vector3(c.x+Mathf.Cos(a)*r,c.y+y,c.z+Mathf.Sin(a)*r);
        for(int i=0;i<seg;i++)
        {
            float a0=i*Mathf.PI*2f/seg,a1=(i+1)*Mathf.PI*2f/seg;var d=new Vector3(Mathf.Cos((a0+a1)*.5f),0,Mathf.Sin((a0+a1)*.5f));
            Q(porcelain,P(rBase,a0,0),P(rBase,a1,0),P(rTop,a1,h),P(rTop,a0,h),d+Vector3.down*((rTop-rBase)/h));
            Q(porcelain,P(rTop-rim,a0,h),P(rTop-rim,a1,h),P(rTop,a1,h),P(rTop,a0,h),Vector3.up);
            Q(porcelain,P(.035f,a0,h-deep),P(.035f,a1,h-deep),P(rTop-rim,a1,h),P(rTop-rim,a0,h),-d+Vector3.up*.6f);
            T(steelPolished,new Vector3(c.x,c.y+h-deep,c.z),P(.035f,a0,h-deep),P(.035f,a1,h-deep),Vector3.up);
        }
    }
    // a chrome mixer behind a basin: round foot, column, spout over the bowl, side lever
    static void Tap(Vector3 basin,Vector3 toBack,float reach)
    {
        var b=basin+toBack*reach;
        m.Cylinder(steelPolished,b,.028f,.016f,20);
        m.Cylinder(steelPolished,b,.016f,.30f,16);
        var top=b+Vector3.up*.30f;var spout=top-toBack*(reach-.03f);
        m.Cylinder(steelPolished,top+Vector3.down*.012f,.017f,.024f,16);
        m.Tube(steelPolished,top,spout,.012f,12);
        m.Cylinder(steelPolished,spout+Vector3.down*.03f,.013f,.03f,12,Quaternion.identity,true,true);
        m.Tube(steelPolished,b+Vector3.up*.22f,b+Vector3.up*.25f+Vector3.Cross(Vector3.up,toBack)*.08f,.007f,8);
    }
    // a horizontal picture (uv 0..1) of its own mesh: air hockey field; facing up (or -z when vertical)
    static void TexQuad(Transform parent,string name,Material mat,Vector3 c,float w,float h,bool vertical)
    {
        var mesh=new Mesh{name="E1 "+name};
        float hw=w*.5f*K,hh=h*.5f*K;
        if(vertical)
        {
            mesh.vertices=new[]{new Vector3(-hw,-hh,0),new Vector3(-hw,hh,0),new Vector3(hw,hh,0),new Vector3(hw,-hh,0)};
            mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
        }
        else
        {
            mesh.vertices=new[]{new Vector3(-hw,0,-hh),new Vector3(-hw,0,hh),new Vector3(hw,0,hh),new Vector3(hw,0,-hh)};
            mesh.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};
        }
        mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=Folder+"/e1_"+name.Replace(' ','_')+".asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh,path);
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=c*K;
        g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
    }

    // ---- pool table pockets: the rail corner (or the middle of a long rail) as a convex outline with the round pocket
    // cut out of its top - stainless rim round the hole, black leather beyond, a black leather hole wall down to the pocket
    // floor; open toward the field where the hole crosses the outline (the mouth between the cushion jaws)
    static void PocketCasting(float cx,float cz,Vector2 P,float R,Vector2[] poly,float yBase,float yTop,float yHole)
    {
        float Hit(float a)
        {
            var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));float best=1e9f;
            for(int i=0;i<poly.Length;i++)
            {
                var p0=poly[i];var e=poly[(i+1)%poly.Length]-p0;float den=d.x*e.y-d.y*e.x;if(Mathf.Abs(den)<1e-9f)continue;
                var w=p0-P;float t=(w.x*e.y-w.y*e.x)/den,s=(w.x*d.y-w.y*d.x)/den;
                if(t>0f&&s>=-1e-5f&&s<=1f+1e-5f&&t<best)best=t;
            }
            return best;
        }
        const float Two=Mathf.PI*2f;
        var angs=new List<float>();for(int i=0;i<72;i++)angs.Add(i*Two/72);
        foreach(var v in poly){float a=Mathf.Atan2(v.y-P.y,v.x-P.x);if(a<0)a+=Two;angs.Add(a);}
        angs.Sort();
        var extra=new List<float>();
        for(int i=0;i<angs.Count;i++)
        {
            float a0=angs[i],a1=i+1<angs.Count?angs[i+1]:angs[0]+Two;
            if((Hit(a0)-R)*(Hit(a1)-R)<0f)
            {
                float lo=a0,hi=a1;
                for(int k=0;k<32;k++){float mid=(lo+hi)*.5f;if((Hit(lo)-R)*(Hit(mid)-R)<=0f)hi=mid;else lo=mid;}
                extra.Add(((lo+hi)*.5f)%Two);
            }
        }
        angs.AddRange(extra);angs.Sort();
        float rim=R+.016f;
        Vector3 V(Vector2 p,float y)=>new Vector3(cx+p.x,y,cz+p.y);
        var pc=new Vector3(cx+P.x,0,cz+P.y);
        for(int i=0;i<angs.Count;i++)
        {
            float a0=angs[i],a1=i+1<angs.Count?angs[i+1]:angs[0]+Two;
            if(a1-a0<1e-5f||Hit((a0+a1)*.5f)<=R+1e-4f)continue;                                  // the mouth: no top
            var d0=new Vector2(Mathf.Cos(a0),Mathf.Sin(a0));var d1=new Vector2(Mathf.Cos(a1),Mathf.Sin(a1));
            float h0=Mathf.Max(Hit(a0),R),h1=Mathf.Max(Hit(a1),R);
            Vector2 i0=P+d0*R,i1=P+d1*R,r0=P+d0*Mathf.Min(rim,h0),r1=P+d1*Mathf.Min(rim,h1),o0=P+d0*h0,o1=P+d1*h1;
            Q(chrome,V(i0,yTop),V(i1,yTop),V(r1,yTop),V(r0,yTop),Vector3.up);
            if(h0>rim+1e-4f||h1>rim+1e-4f)Q(rubber,V(r0,yTop),V(r1,yTop),V(o1,yTop),V(o0,yTop),Vector3.up);
            Q(rubber,V(i0,yBase),V(i1,yBase),V(o1,yBase),V(o0,yBase),Vector3.down);
            var inward=pc-(V(i0,0)+V(i1,0))*.5f;
            Q(rubber,V(i0,yHole),V(i1,yHole),V(i1,yTop),V(i0,yTop),inward);
            if((o1-o0).sqrMagnitude>1e-10f)Q(rubber,V(o0,yBase),V(o1,yBase),V(o1,yTop),V(o0,yTop),-((V(o0,0)+V(o1,0))*.5f-pc)*-1f);
        }
    }

    // ---- WC: wall-hung pan with a closed seat lid, on the tiled service duct with a chrome flush plate (back at wallZ, to -z)
    static void Toilet(float x,float wallZ)
    {
        m.RoundedBox(porcelain,new Vector3(x,FFL+.34f,wallZ-.26f),new Vector3(.36f,.32f,.54f),.14f,5);
        m.RoundedBox(porcelain,new Vector3(x,FFL+.525f,wallZ-.28f),new Vector3(.37f,.035f,.47f),.016f,3);
        RB(steelPolished,x-.12f,x+.12f,FFL+.90f,FFL+1.05f,wallZ-.02f,wallZ+.01f,.012f,false,2);
        Col(x-.2f,x+.2f,FFL,FFL+.55f,wallZ-.55f,wallZ);
    }
    // wall-hung urinal on a wall at wallX, standing out toward dir (+1 = +x, -1 = -x)
    static void Urinal(float wallX,float z,float dir)
    {
        m.RoundedBox(porcelain,new Vector3(wallX+dir*.16f,FFL+.78f,z),new Vector3(.34f,.64f,.40f),.13f,5);
        m.Cylinder(steelPolished,new Vector3(wallX+dir*.045f,FFL+1.10f,z),.022f,.32f,14);
        RB(steelPolished,Mathf.Min(wallX,wallX+dir*.02f),Mathf.Max(wallX,wallX+dir*.02f),FFL+1.45f,FFL+1.55f,z-.06f,z+.06f,.008f,false,2);
        Col(Mathf.Min(wallX,wallX+dir*.34f),Mathf.Max(wallX,wallX+dir*.34f),FFL+.4f,FFL+1.1f,z-.2f,z+.2f,"pisuar");
    }

    // ---- games (owner 10.11 20:50: everything functional later - built to real sizes now): an upright arcade cabinet,
    // front at zf facing -z, rounded body, sloped control panel with a joystick and six buttons, screen, lit marquee
    static void Arcade(float x,float zf,int k)
    {
        float x0=x-.36f,x1=x+.36f,zb=zf+.80f;var led=k%2==0?purpleLed:redLed;
        RB(black,x0,x1,FFL,FFL+.98f,zf,zb,.03f);
        RB(black,x0,x1,FFL+.98f,FFL+1.86f,zf+.22f,zb,.03f);
        var rot=Quaternion.Euler(-14f,0,0);var pc=new Vector3(x,FFL+1.03f,zf+.02f);
        m.RoundedBox(black,pc,new Vector3(.72f,.07f,.42f),.025f,rot,3);
        arcadeR.Add(Screen(scrGroup,"automat "+(k+1),scrArcade[k%scrArcade.Length],new Vector3(x,FFL+1.41f,zf+.208f),.60f,.45f,0f));
        B(led,x-.33f,x+.33f,FFL+1.72f,FFL+1.82f,zf+.208f,zf+.22f);
        RB(chrome,x-.10f,x+.10f,FFL+.42f,FFL+.66f,zf-.016f,zf+.01f,.008f,false,2);
        foreach(float sx in new[]{x0-.012f,x1})B(led,sx,sx+.012f,FFL+.08f,FFL+1.80f,zf+.30f,zf+.33f);
        var up=rot*Vector3.up;var js=pc+rot*new Vector3(-.17f,.035f,0f);
        m.Cylinder(steelPolished,js,.008f,.075f,10,rot,false,false);
        Sphere(red,js+up*.095f,.022f,14,10);
        for(int i=0;i<6;i++){var bp=pc+rot*new Vector3(.02f+(i%3)*.075f,.035f,(i/3)*.07f-.035f);m.Cylinder(i%3==0?red:i%3==1?chrome:purpleLed,bp,.016f,.012f,16,rot,true,false);}
        Col(x0,x1,FFL,FFL+1.86f,zf-.19f,zb,"automat");
    }
    // air hockey 2.4 x 1.2 m: two rounded pedestals, rounded body, rails with goal slots, the painted field, mallets, puck
    static Material airField;
    static void AirHockey(Transform parent,float x,float z,int k)
    {
        float hx=.62f,hz=1.22f,top=FFL+.80f;
        foreach(float s in new[]{-1f,1f})RB(black,x-.45f,x+.45f,FFL,FFL+.56f,z+s*.75f-.2f,z+s*.75f+.2f,.05f);
        RB(black,x-hx,x+hx,FFL+.55f,top-.02f,z-hz,z+hz,.05f);
        TexQuad(parent,"air hockey field "+k,airField,new Vector3(x,top,z),2*(hx-.07f),2*(hz-.07f),false);
        RB(black,x-hx,x-hx+.07f,top-.04f,top+.06f,z-hz,z+hz,.03f,false,3);RB(black,x+hx-.07f,x+hx,top-.04f,top+.06f,z-hz,z+hz,.03f,false,3);
        foreach(float s in new[]{-1f,1f})
        {
            float za=s<0?z-hz:z+hz-.07f,zb=s<0?z-hz+.07f:z+hz;
            RB(black,x-hx+.07f,x-.16f,top-.04f,top+.06f,za,zb,.03f,false,3);RB(black,x+.16f,x+hx-.07f,top-.04f,top+.06f,za,zb,.03f,false,3);
            // mallet (handle and knob), red
            var mc=new Vector3(x,top,z+s*(hz-.30f));
            m.Cylinder(red,mc,.048f,.022f,28,Quaternion.identity,true,false);
            m.Cylinder(red,mc+Vector3.up*.022f,.022f,.05f,20,Quaternion.identity,false,false);
            m.Cylinder(red,mc+Vector3.up*.072f,.026f,.02f,20,Quaternion.identity,true,false,.02f);
        }
        m.Cylinder(rubber,new Vector3(x+.18f,top,z+.25f),.041f,.008f,24,Quaternion.identity,true,false);
        foreach(float s in new[]{-1f,1f})B(purpleLed,s<0?x-hx-.012f:x+hx,s<0?x-hx:x+hx+.012f,FFL+.57f,FFL+.60f,z-hz+.08f,z+hz-.08f);
        hockeyR.Add(Screen(scrGroup,"cymbergaj wynik "+(k+1),scrHockey,new Vector3(x-hx-.012f,FFL+.69f,z),.30f,.11f,90f));
        Col(x-hx,x+hx,FFL,top+.06f,z-hz,z+hz,"cymbergaj");
    }
    // VR pod: round plinth with an anti-slip mat and a LED band, padded safety ring on steel posts (open to -z),
    // the headset on a ceiling reel
    static void VRPod(float x,float z)
    {
        m.Cylinder(black,new Vector3(x,FFL,z),1.0f,.12f,64,Quaternion.identity,true,false);
        m.Cylinder(rubber,new Vector3(x,FFL+.12f,z),.90f,.012f,64,Quaternion.identity,true,false);
        m.Cylinder(purpleLed,new Vector3(x,FFL+.04f,z),1.012f,.04f,64,Quaternion.identity,false,false);
        float rr=.88f,y=FFL+1.02f;const int n=28;
        for(int i=0;i<n;i++)
        {
            float a0=i*Mathf.PI*2f/n,a1=(i+1)*Mathf.PI*2f/n,am=(a0+a1)*.5f;
            if(Mathf.Abs(Mathf.DeltaAngle(am*Mathf.Rad2Deg,-90f))<28f)continue;
            var p0=new Vector3(x+Mathf.Cos(a0)*rr,y,z+Mathf.Sin(a0)*rr);var p1=new Vector3(x+Mathf.Cos(a1)*rr,y,z+Mathf.Sin(a1)*rr);
            m.Tube(rubber,p0,p1,.035f,12);m.Cylinder(rubber,p1-Vector3.up*.035f,.035f,.07f,12,Quaternion.identity,false,false);
            if(i%2==0){var cc=(p0+p1)*.5f;BraxChampionsMeshBatch.Collider(cols,"VR barierka",new Vector3(cc.x,(FFL+.12f+y)*.5f,cc.z),
                new Vector3(Vector3.Distance(p0,p1)*2.05f,y-FFL-.12f,.08f),-Mathf.Atan2(p1.z-p0.z,p1.x-p0.x)*Mathf.Rad2Deg);Colliders++;}
        }
        foreach(float a in new[]{-55f,55f,125f,235f}){float r=a*Mathf.Deg2Rad;m.Cylinder(steelPolished,new Vector3(x+Mathf.Cos(r)*rr,FFL+.132f,z+Mathf.Sin(r)*rr),.022f,y-FFL-.132f,14);}
        m.Cylinder(steelPolished,new Vector3(x,2.55f,z),.004f,Ceiling-.14f-2.55f,6);
        m.Cylinder(black,new Vector3(x,Ceiling-.14f,z),.10f,.14f,24);
        m.RoundedBox(black,new Vector3(x,2.48f,z),new Vector3(.20f,.11f,.12f),.045f,3);
        m.RoundedBox(rubber,new Vector3(x,2.48f,z-.06f),new Vector3(.17f,.08f,.03f),.013f,3);
        Col(x-1f,x+1f,FFL,FFL+.12f,z-1f,z+1f,"VR podest");
    }
    // claw machine (.9 x .9 x 2.15 m): rounded base with the card reader, control ledge with a joystick and a button,
    // glass case on black posts, plush prizes, the chute, gantry and claw, lit header
    static Material[] plush;
    static void ClawMachine(float x,float z,int k)
    {
        const float h=.45f;float yb=FFL+.86f,yt=FFL+1.90f;
        RB(black,x-h,x+h,FFL,yb,z-h,z+h,.03f);
        RB(chrome,x-.09f,x+.09f,FFL+.32f,FFL+.50f,z-h-.016f,z-h+.01f,.006f,false,2);
        RB(black,x-h,x+h,yb-.10f,yb+.04f,z-h-.22f,z-h+.05f,.025f);
        var js=new Vector3(x-.13f,yb+.04f,z-h-.10f);m.Cylinder(steelPolished,js,.008f,.08f,10);Sphere(red,js+Vector3.up*.10f,.024f,14,10);
        m.Cylinder(chrome,new Vector3(x+.13f,yb+.04f,z-h-.10f),.032f,.014f,24,Quaternion.identity,true,false);
        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})
            RB(black,sx<0?x-h:x+h-.045f,sx<0?x-h+.045f:x+h,yb,yt,sz<0?z-h:z+h-.045f,sz<0?z-h+.045f:z+h,.012f,false,2);
        B(clearGlass,x-h+.045f,x+h-.045f,yb,yt,z-h+.015f,z-h+.025f);B(clearGlass,x-h+.045f,x+h-.045f,yb,yt,z+h-.025f,z+h-.015f);
        B(clearGlass,x-h+.015f,x-h+.025f,yb,yt,z-h+.045f,z+h-.045f);B(clearGlass,x+h-.025f,x+h-.015f,yb,yt,z-h+.045f,z+h-.045f);
        // the chute in the front-left corner (black box, stainless edge), the prize pile
        RB(black,x-h+.05f,x-h+.27f,yb,yb+.20f,z-h+.05f,z-h+.27f,.012f,false,2);
        for(int i=0;i<34;i++)
        {
            float u=Hash(i,k*31+7),v=Hash(i*3+1,k*17+3),w=Hash(i*7+2,k*5+11);
            float px=x-h+.10f+u*(2*h-.20f),pz=z-h+.10f+v*(2*h-.20f);
            if(px<x-h+.33f&&pz<z-h+.33f)continue;
            float r=.065f+w*.03f;Sphere(plush[(i+k)%plush.Length],new Vector3(px,yb+r*.9f+(i%3)*.06f,pz),r,14,10);
        }
        foreach(float sz in new[]{-1f,1f})m.Tube(steelPolished,new Vector3(x-h+.05f,yt-.06f,z+sz*(h-.08f)),new Vector3(x+h-.05f,yt-.06f,z+sz*(h-.08f)),.012f,10);
        m.Tube(steelPolished,new Vector3(x+.05f,yt-.06f,z-h+.08f),new Vector3(x+.05f,yt-.06f,z+h-.08f),.012f,10);
        m.RoundedBox(black,new Vector3(x+.05f,yt-.09f,z+.05f),new Vector3(.12f,.06f,.12f),.015f,3);
        m.Cylinder(steelPolished,new Vector3(x+.05f,yt-.42f,z+.05f),.003f,.30f,6);
        m.Cylinder(chrome,new Vector3(x+.05f,yt-.50f,z+.05f),.035f,.08f,20,Quaternion.identity,true,true,.025f);
        for(int i=0;i<3;i++){float a=i*120f*Mathf.Deg2Rad;var o=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            var p0=new Vector3(x+.05f,yt-.50f,z+.05f)+o*.03f;m.Tube(chrome,p0,p0+o*.06f+Vector3.down*.07f,.006f,8);m.Tube(chrome,p0+o*.06f+Vector3.down*.07f,p0+o*.03f+Vector3.down*.14f,.006f,8);}
        RB(black,x-h,x+h,yt,yt+.26f,z-h,z+h,.03f);
        B(k%2==0?redLed:purpleLed,x-h+.04f,x+h-.04f,yt+.06f,yt+.20f,z-h-.012f,z-h);
        Col(x-h,x+h,FFL,yt+.26f,z-h-.22f,z+h,"automat z pluszakami");
    }
    // office desk with steel legs, a modesty panel, a monitor on a stand, keyboard; the person sits on the -z side
    static void Desk(float x0,float x1,float zc)
    {
        float z0=zc-.40f,z1=zc+.40f,y=FFL+.72f;
        RB(lacquer,x0,x1,y,y+.03f,z0,z1,.012f,true,3);
        foreach(float x in new[]{x0+.06f,x1-.06f})foreach(float z in new[]{z0+.06f,z1-.06f})m.Cylinder(steelPolished,new Vector3(x,FFL,z),.025f,.72f,14,Quaternion.identity,false,false);
        RB(lacquer,x0+.12f,x1-.12f,FFL+.30f,y-.02f,z1-.10f,z1-.08f,.008f,false,2);
        float mx=(x0+x1)*.5f;
        m.Cylinder(steelPolished,new Vector3(mx,y+.03f,zc+.22f),.10f,.012f,24);m.Cylinder(steelPolished,new Vector3(mx,y+.042f,zc+.22f),.018f,.20f,12);
        RB(black,mx-.31f,mx+.31f,y+.20f,y+.56f,zc+.20f,zc+.235f,.008f,false,2);
        boardR.Add(Screen(scrGroup,"monitor biuro "+zc,scrBoard,new Vector3(mx,y+.38f,zc+.188f),.58f,.32f,0f));
        RB(black,mx-.22f,mx+.22f,y+.03f,y+.05f,zc-.12f,zc+.03f,.006f,false,2);
        RB(black,mx+.30f,mx+.36f,y+.03f,y+.06f,zc-.08f,zc+.02f,.025f,false,3);
    }

    // ---- balls: a UV sphere (radius 1) shared by every ball; painted resin / phenolic finishes
    static Mesh sphereMesh;
    static Mesh SphereMesh()
    {
        if(sphereMesh)return sphereMesh;
        const int lon=32,lat=20;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        for(int j=0;j<=lat;j++)
        {
            float a=(-90f+180f*j/lat)*Mathf.Deg2Rad;
            for(int i=0;i<=lon;i++){float b=360f*i/lon*Mathf.Deg2Rad;v.Add(new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a),Mathf.Cos(a)*Mathf.Sin(b)));uv.Add(new Vector2((float)i/lon,(float)j/lat));}
        }
        for(int j=0;j<lat;j++)for(int i=0;i<lon;i++){int a=j*(lon+1)+i,b=a+1,c=a+lon+1,d=c+1;t.Add(a);t.Add(c);t.Add(b);t.Add(b);t.Add(c);t.Add(d);}
        sphereMesh=new Mesh{name="E1 ball sphere (uv)"};
        sphereMesh.vertices=v.ToArray();sphereMesh.normals=v.ToArray();sphereMesh.uv=uv.ToArray();sphereMesh.triangles=t.ToArray();
        sphereMesh.RecalculateTangents();sphereMesh.RecalculateBounds();
        string path=Folder+"/e1_ball_sphere.asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old)AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(sphereMesh,path);
        return sphereMesh;
    }
    static Texture2D Paint(string name,int w,int h,Func<float,float,Color> f)
    {
        var tex=new Texture2D(w,h,TextureFormat.RGBA32,true){name="E1 "+name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
        var px=new Color32[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)px[y*w+x]=f((x+.5f)/w,(y+.5f)/h);
        tex.SetPixels32(px);tex.Apply(true);
        System.IO.Directory.CreateDirectory(Out+"/Textures");
        string path=Out+"/Textures/e1_"+name+".asset";
        if(AssetDatabase.LoadAssetAtPath<Texture2D>(path))AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(tex,path);
        return tex;
    }
    static float Hash(int x,int y){unchecked{uint h=(uint)(x*374761393+y*668265263);h=(h^(h>>13))*1274126177u;return (h^(h>>16))/4294967295f;}}
    static float Noise(float x,float y)
    {
        int xi=Mathf.FloorToInt(x),yi=Mathf.FloorToInt(y);float fx=x-xi,fy=y-yi;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
        float a=Hash(xi,yi),b=Hash(xi+1,yi),c=Hash(xi,yi+1),d=Hash(xi+1,yi+1);
        return a+(b-a)*fx+(c-a)*fy+(a-b-c+d)*fx*fy;
    }
    static Vector3 Dir(float u,float v){float lon=u*Mathf.PI*2f,lat=(v-.5f)*Mathf.PI;return new Vector3(Mathf.Cos(lat)*Mathf.Cos(lon),Mathf.Sin(lat),Mathf.Cos(lat)*Mathf.Sin(lon));}
    static void BallMaterials()
    {
        // house bowling balls: reactive resin with a marbled swirl (two colours), glossy
        var sw=new[]{(new Color(.55f,.03f,.06f),new Color(.04f,.04f,.05f)),(new Color(.06f,.16f,.58f),new Color(.78f,.80f,.84f)),
                     (new Color(.32f,.06f,.52f),new Color(.95f,.72f,.20f)),(new Color(.03f,.42f,.24f),new Color(.04f,.05f,.05f))};
        houseBalls=new Material[sw.Length];
        for(int k=0;k<sw.Length;k++)
        {
            var (ca,cb)=sw[k];int seed=k*17;
            var tex=Paint("bowling_ball_"+k,256,128,(u,v)=>
            {
                var d=Dir(u,v);float n=0f,amp=1f,f=2.2f;
                for(int o=0;o<4;o++){n+=amp*Noise(d.x*f+d.z*f*.7f+seed,d.y*f+d.z*f*.4f-seed);amp*=.5f;f*=2.1f;}
                float swirl=Mathf.Sin((d.x*3.1f+d.y*2.3f+n*5.5f)*2.4f)*.5f+.5f;
                var c=Color.Lerp(ca,cb,Mathf.SmoothStep(.35f,.65f,swirl));
                c*=.88f+.24f*Noise(d.x*40f+seed,d.y*40f+d.z*40f);
                // the three finger holes are drilled where u ~ .25 near the equator
                float du=Mathf.Abs(u-.25f)*2f*Mathf.PI,dv=(v-.5f)*Mathf.PI;
                foreach(var (hu,hv) in new[]{(0f,.12f),(-.10f,-.06f),(.10f,-.06f)})
                    if(Mathf.Sqrt((du-hu)*(du-hu)+(dv-hv)*(dv-hv))<.045f)c=new Color(.02f,.02f,.02f);
                c.a=1f;return c;
            });
            var mt=S("bowling_ball_resin_"+k,Color.white,.08f,.15f);
            mt.SetTexture("_MainTex",tex);EditorUtility.SetDirty(mt);houseBalls[k]=Inside(mt,.85f);
        }
        // pool balls: phenolic resin, numbers in white circles on both sides; 9-15 striped
        var col=new[]{new Color(.96f,.95f,.90f),new Color(.98f,.78f,.06f),new Color(.06f,.20f,.66f),new Color(.82f,.07f,.07f),new Color(.32f,.10f,.52f),
                      new Color(.97f,.44f,.06f),new Color(.04f,.44f,.22f),new Color(.56f,.03f,.10f),new Color(.03f,.03f,.035f)};
        poolBalls=new Material[16];
        for(int n=0;n<16;n++)
        {
            int num=n;var basec=n<=8?col[n]:col[n-8];bool stripe=n>=9;
            const int W=256,H=128;
            var px=new Color32[W*H];
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                float u=(x+.5f)/W,v=(y+.5f)/H;Color c;
                if(n==0)c=col[0];
                else if(stripe)c=Mathf.Abs(v-.5f)<.125f?basec:col[0];
                else c=basec;
                // number circles at u .25 and .75 on the equator
                foreach(float cu in new[]{.25f,.75f})
                {
                    float dx=(u-cu)*2f*Mathf.PI,dy=(v-.5f)*Mathf.PI;
                    if(n>0&&Mathf.Sqrt(dx*dx+dy*dy)<.36f)c=col[0];
                }
                px[y*W+x]=c;
            }
            if(n>0)foreach(int cx0 in new[]{W/4,3*W/4})
            {
                string s=num.ToString();int w=s.Length*6*2-2;
                BraxChampionsPiksele.Tekst(px,W,H,s,cx0-w/2,H/2-7,2,new Color32(10,10,12,255));
            }
            var tex=new Texture2D(W,H,TextureFormat.RGBA32,true){name="E1 pool ball "+n,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            tex.SetPixels32(px);tex.Apply(true);
            System.IO.Directory.CreateDirectory(Out+"/Textures");
            string path=Out+"/Textures/e1_pool_ball_"+n+".asset";
            if(AssetDatabase.LoadAssetAtPath<Texture2D>(path))AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(tex,path);
            var mt=S("pool_ball_"+n,Color.white,.06f,0f);mt.SetTexture("_MainTex",tex);EditorUtility.SetDirty(mt);poolBalls[n]=Inside(mt,.9f);
        }
    }
    // a ball object (shared sphere mesh, scaled to the radius); with physics when mass > 0
    static Rigidbody Ball(Transform parent,string name,Material mat,Vector3 local,float radius,float mass)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.layer=8;
        g.transform.localPosition=local*K;g.transform.localScale=Vector3.one*radius*K;
        g.AddComponent<MeshFilter>().sharedMesh=SphereMesh();
        var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        if(mass<=0f){GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);return null;}
        g.AddComponent<SphereCollider>().radius=1f;
        var rb=g.AddComponent<Rigidbody>();rb.mass=mass;rb.isKinematic=true;
        rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        GameObjectUtility.SetStaticEditorFlags(g,0);
        return rb;
    }

    // ---- doors: closed leaves that open on approach (BraxCityDoor), hinge groups at the jambs; frame in the main batch
    static void Door(string id,string label,float x,float y,float z,float yaw,float width,float height,bool glassLeaf)
    {
        var group=Group(root,label+" | door (opens on approach)");
        group.localPosition=new Vector3(x,y,z)*K;group.localRotation=Quaternion.Euler(0,yaw,0);
        int n=width>1.5f?2:1;var leaves=new Transform[n];var sides=new float[n];
        for(int i=0;i<n;i++)
        {
            float side=n==1?-1f:(i==0?-1f:1f),lw=width/n;
            var hinge=Group(group,"door leaf "+(i+1));hinge.localPosition=new Vector3(side*width*.5f,0,0)*K;
            float a0=Mathf.Min(0f,-side*lw)+.01f,a1=Mathf.Max(0f,-side*lw)-.01f;
            var lm=new BraxChampionsMeshBatch();
            if(glassLeaf)
            {
                lm.BoxMinMax(black,a0,a1,height-.14f,height-.02f,-.05f,.05f);lm.BoxMinMax(black,a0,a1,.02f,.24f,-.05f,.05f);
                lm.BoxMinMax(black,a0,a0+.10f,.24f,height-.14f,-.05f,.05f);lm.BoxMinMax(black,a1-.10f,a1,.24f,height-.14f,-.05f,.05f);
                lm.BoxMinMax(clearGlass,a0+.10f,a1-.10f,.24f,height-.14f,-.012f,.012f);
                float px=side<0?a1-.30f:a0+.30f;
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(chrome,px-.03f,px+.03f,.75f,1.95f,f>0?.05f:-.11f,f>0?.11f:-.05f);
            }
            else
            {
                lm.BoxMinMax(blackMatte,a0,a1,.02f,height-.02f,-.025f,.025f);
                float px=side<0?a1-.12f:a0+.12f;
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(chrome,px-.08f,px+.08f,1.02f,1.06f,f>0?.025f:-.075f,f>0?.075f:-.025f);
            }
            lm.Build(hinge,label+" leaf "+(i+1),Folder,false);
            BraxChampionsMeshBatch.Collider(hinge,"door leaf collider",new Vector3((a0+a1)*.5f,height*.5f,0),new Vector3(a1-a0,height,.08f));
            foreach(var t in hinge.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            leaves[i]=hinge;sides[i]=n==1?1f:(i==0?1f:-1f);
        }
        var d=group.gameObject.AddComponent<BraxCityDoor>();d.id=id;d.label=label;d.leaves=leaves;d.sides=sides;
        Doors++;
    }

    // ================================================================ GENERATE
    public static int Generate(string outFolder)
    {
        System.IO.Directory.CreateDirectory(Folder);
        Doors=Bays=Pins=BilliardTables=Colliders=Signs=0;
        Materials();
        var go=new GameObject("E1 | CENTRUM ROZRYWKI | kręgle 24 tory, bilard 18 stołów, gry/VR, bar/lounge, parking B1 (owner 10.11 19:18 board)");
        root=go.transform;root.position=new Vector3(CentreX,0,CentreZ)*K;
        cols=Group(root,"kolizje");
        scrGroup=Group(root,"E1 ekrany (BraxChampionsE1Ekrany)");boardR.Clear();kioskR.Clear();arcadeR.Clear();hockeyR.Clear();
        m=new BraxChampionsMeshBatch();
        Site();Flush("E1 site | forecourt, taxi lay-by, yard, ramp");
        Shell();Flush("E1 shell | facades, portal, roof");
        FrontSigns();Flush("E1 signs | E1, CENTRUM ROZRYWKI, KRĘGLE, BILARD, GRY, BAR, LOUNGE");
        Hall();Flush("E1 interior | hall + reception");
        Bowling();Flush("E1 interior | bowling 24 lanes");
        Billiards();Flush("E1 interior | billiards 18 tables, bar, lounge");
        BackRooms();Flush("E1 interior | games/VR, offices, cloakrooms, toilets, store, plant");
        Garage();Flush("E1 B1 | garage");
        Lift();
        var ek=scrGroup.gameObject.AddComponent<BraxChampionsE1Ekrany>();
        var kk=root.GetComponentsInChildren<BraxChampionsKregle>(true);var bb=root.GetComponentsInChildren<BraxChampionsBilard>(true);
        ek.kregle=kk.Length>0?kk[0]:null;ek.bilard=bb.Length>0?bb[0]:null;
        ek.tablice=boardR.ToArray();ek.kioski=kioskR.ToArray();ek.automaty=arcadeR.ToArray();ek.cymbergaj=hockeyR.ToArray();
        Debug.Log("E1_CENTRUM_ROZRYWKI lanes="+Lanes+" pins="+Pins+" billiardTables="+BilliardTables+" garageBays="+Bays+" doors="+Doors+
                  " colliders="+Colliders+" signs="+Signs+" lift=B1-HOL taxiBay="+(BayX1-BayX0)+"m");
        return 1;
    }

    // ================================================================ SITE
    static void Site()
    {
        // forecourt to the Z-70 sidewalk (local z -40) and the strips beside the building, concrete pavers flush at .16
        B(pavers,SiteX0,SiteX1,-.40f,Y0,SiteZ0,-HD,true);
        foreach(float s in new[]{-1f,1f})B(pavers,s<0?SiteX0:HW,s<0?-HW:SiteX1,-.40f,Y0,-HD,HD,true);
        // red carpet from the taxi bay to the doors (2 cm)
        B(carpetRed,2.5f,7.5f,Y0,Y0+.02f,SiteZ0+.4f,-31.0f);
        // planters with the board's flowers, lamps, benches facing the promenade
        var models=Group(root,"kwiaty (Poly Haven)");
        foreach(float x in new[]{-50f,-36f,-22f,-8f,18f,32f,46f,58f})BraxChampionsPromenadeFurniture.Planter(m,cols,models,x,-34.5f);
        foreach(float x in new[]{-50f,-30f,-10f,20f,40f,59f})BraxChampionsPromenadeFurniture.Lamp(m,cols,x,-38.6f);   // in line with the pilasters: never in front of a sign
        foreach(float x in new[]{-43f,-15f,25f,52f})BraxChampionsPromenadeFurniture.BenchFramed(m,cols,x,-37.2f,new Vector2(0,-1),"E1 "+x.ToString("0"));
        TaxiBay();
        TaxiSign(10.5f,-41.2f);
        // ---- service yard behind the building (concrete), the ramp trench at x 40..47 from z 58 down to the B1 portal
        float yz0=HD,yz1=SiteZ1;
        B(techFloor,SiteX0,RampX0-.3f,-.40f,Y0,yz0,yz1,true);
        B(techFloor,RampX1+.3f,SiteX1,-.40f,Y0,yz0,yz1,true);
        B(techFloor,RampX0-.3f,RampX1+.3f,-.40f,Y0,RampTop,yz1,true);                               // apron at grade
        Ramp();
        // the garage drive across the Z70 sidewalk: a street mouth with R 3 m arcs (layout.E1_GARAGE_*)
        BraxChampionsDriveMouth.Build(root,new BraxChampionsDriveMouth.Spec{id="E1 garage street mouth",alongX=true,edge=RoadEdgeN,dir=-1f,
            u0=RampX0,u1=RampX1,R=MouthR,depth=RoadEdgeN-SiteZ1,topEnd=Y0,unit=K,assetDir=Folder},asphalt,walk,BraxChampionsPromenadeFurniture.Kerb);
        // asphalt lane on the apron from the mouth to the ramp, between flush granite edging
        B(asphalt,RampX0,RampX1,Y0,Y0+.02f,RampTop,SiteZ1);
    }

    // taxi lay-by: kerb face from the road edge (z -47) along an S of two R 6 m arcs to z -44 (bay 3 m), straight, back again
    static void TaxiBay()
    {
        float zE=RoadEdgeS,zB=RoadEdgeS+BayDepth,zCut=zB+KerbW,x0=BayX0-BayTaper,x1=BayX1+BayTaper;
        float th=(float)Math.Acos(1f-BayDepth/(2f*BayR));                          // 41.4 deg
        // kerb face line z(x)
        var pts=new List<Vector2>();
        int n=10;
        for(int i=0;i<=n;i++){float a=th*i/n;pts.Add(new Vector2(x0+BayR*Mathf.Sin(a),zE+BayR*(1-Mathf.Cos(a))));}
        for(int i=n;i>=0;i--){float a=th*i/n;pts.Add(new Vector2(BayX0-BayR*Mathf.Sin(a),zB-BayR*(1-Mathf.Cos(a))));}
        pts.RemoveAt(pts.Count-1);
        var right=new List<Vector2>();
        foreach(var p in pts)right.Add(new Vector2(x0+x1-p.x,p.y));
        right.Reverse();
        var line=new List<Vector2>(pts);line.Add(new Vector2(BayX0,zB));line.Add(new Vector2(BayX1,zB));line.AddRange(right);
        // asphalt under the line (at road level 0), kerb stone .18 behind the line (top .16), city paving to the cut edge
        for(int i=0;i+1<line.Count;i++)
        {
            var a=line[i];var b=line[i+1];
            Q(asphalt,new Vector3(a.x,0,zE),new Vector3(b.x,0,zE),new Vector3(b.x,0,b.y),new Vector3(a.x,0,a.y),Vector3.up);
            var d=(b-a).normalized;var nn=new Vector2(-d.y,d.x);if(nn.y<0)nn=new Vector2(-nn.x,-nn.y);
            Vector2 ak=a+nn*KerbW,bk=b+nn*KerbW;
            if(i==0)ak=new Vector2(a.x,a.y+KerbW);
            if(i==line.Count-2)bk=new Vector2(b.x,b.y+KerbW);
            ak.y=Mathf.Min(ak.y,zCut);bk.y=Mathf.Min(bk.y,zCut);
            var kerb=BraxChampionsPromenadeFurniture.Kerb;
            Q(kerb,new Vector3(a.x,Y0,a.y),new Vector3(b.x,Y0,b.y),new Vector3(bk.x,Y0,bk.y),new Vector3(ak.x,Y0,ak.y),Vector3.up);
            Q(kerb,new Vector3(a.x,0,a.y),new Vector3(b.x,0,b.y),new Vector3(b.x,Y0,b.y),new Vector3(a.x,Y0,a.y),new Vector3(-nn.x,0,-nn.y));
            if(ak.y<zCut-1e-4f||bk.y<zCut-1e-4f)
                Q(walk,new Vector3(ak.x,Y0,ak.y),new Vector3(bk.x,Y0,bk.y),new Vector3(bk.x,Y0,zCut),new Vector3(ak.x,Y0,zCut),Vector3.up);
        }
        Col(x0,x1,-.40f,0f,zE,zB);
        Col(x0,x1,-.40f,Y0,zB,zCut);
        // the bay's yellow edge line (8 mm) and the "TAXI" letters standing on the kerb face side
        B(lineYellow,BayX0+.5f,BayX1-.5f,0f,.008f,zE+.10f,zE+.22f);
        foreach(float x in new[]{BayX0+.5f,BayX1-.62f})B(lineYellow,x,x+.12f,0f,.008f,zE+.22f,zB-.15f);
    }
    static void TaxiSign(float x,float z)
    {
        m.Cylinder(BraxChampionsHarbour.Graphite,new Vector3(x,Y0,z),.06f,3.2f,14,Quaternion.identity,true);
        B(black,x-.75f,x+.75f,2.55f,3.25f,z-.08f,z+.08f);
        B(redLed,x-.71f,x+.71f,2.59f,2.62f,z-.092f,z-.08f);
        BraxChampionsLetters3D.Text(m,lineYellow,black,"TAXI",new Vector3(x,2.72f,z-.075f),180f,.36f,.03f,.14f,1.3f);
        BraxChampionsLetters3D.Text(m,lineYellow,black,"TAXI",new Vector3(x,2.72f,z+.075f),0f,.36f,.03f,.14f,1.3f);
        Col(x-.1f,x+.1f,Y0,3.2f,z-.1f,z+.1f,"taxi sign");Signs++;
    }

    // ramp: grade (.16) at z 58 -> B1 (-3.60) at z 27; 6 % / 14.3 % / 6 %; walls, parapets, LED, the B1 portal under the building
    static void Ramp()
    {
        float x0=RampX0,x1=RampX1;
        float[] zs={RampTop,54f,31f,RampBottom,HD};float[] ys={Y0,Y0-.24f,B1+.24f,B1,B1};
        for(int i=0;i+1<zs.Length;i++)
        {
            if(zs[i]-zs[i+1]<1e-3f)continue;
            SlopeZ(antiSlip,x0,x1,zs[i+1],ys[i+1],zs[i],ys[i],.35f);
        }
        // trench walls (concrete), from below the ramp to grade, parapet 1.1 m above grade with a black coping
        foreach(float s in new[]{-1f,1f})
        {
            float a=s<0?x0-.3f:x1,b=s<0?x0:x1+.3f;
            B(rampWall,a,b,B1-.40f,Y0+1.1f,HD,RampTop,true);
            B(black,a-.02f,b+.02f,Y0+1.1f,Y0+1.16f,HD,RampTop);
            for(float z=HD+3f;z<RampTop-1f;z+=5f)B(redLed,s<0?x0:x1-.012f,s<0?x0+.012f:x1,-.2f,-.12f,z,z+1.2f);
        }
    }

    // ================================================================ SHELL
    static void Shell()
    {
        // ground floor slab (B1 ceiling under it), holes for the stair and the lift shaft
        void Slab(float x0,float x1,float z0,float z1,bool col=true){B(garageWhite,x0,x1,SlabBottom,SlabTop,z0,z1);if(col)Col(x0,x1,SlabBottom,FFL,z0,z1,"posadzka parter");}
        Slab(-HW,HW,-HD,LiftZ0,false);
        // its colliders: the floor at FFL, and round the bowling lanes the real surfaces - lane beds at FFL, gutters 6 cm
        // down, the divider between the lanes of a pair, the pit 10 cm down behind the pin decks (pins and balls fall in)
        float lx0=LaneX0,lx1=LaneX0+12*PairPitch,lz0=FoulZ+.02f,lz1=LaneEnd-.3f,pit1=LaneEnd+.3f;
        Col(-HW,HW,SlabBottom,FFL,-HD,lz0,"posadzka parter");
        Col(-HW,lx0,SlabBottom,FFL,lz0,LiftZ0,"posadzka parter");Col(lx1,HW,SlabBottom,FFL,lz0,LiftZ0,"posadzka parter");
        Col(lx0,lx1,SlabBottom,FFL,pit1,LiftZ0,"posadzka parter");
        Col(lx0,lx1,SlabBottom,SlabTop+.02f,lz1,pit1,"dół za torami");
        Col(lx0,PairX(0)-1.59f,SlabBottom,FFL,lz0,lz1,"posadzka parter");Col(PairX(11)+1.59f,lx1,SlabBottom,FFL,lz0,lz1,"posadzka parter");
        for(int l=0;l<Lanes;l++)
        {
            float xc=LaneX(l);
            Col(xc-.5335f,xc+.5335f,SlabBottom,FFL,lz0,lz1,"tor");
            Col(xc-.775f,xc-.5335f,SlabBottom,FFL-.06f,lz0,lz1,"rynna");Col(xc+.5335f,xc+.775f,SlabBottom,FFL-.06f,lz0,lz1,"rynna");
        }
        for(int p=0;p<12;p++)Col(PairX(p)-.04f,PairX(p)+.04f,SlabBottom,FFL+.25f,lz0,lz1,"przegroda");
        Slab(-HW,StairX0,LiftZ0,HD);
        Slab(StairX0,StairX0+(Risers-1)*StairRun,LiftZ0,StairZ0);
        Slab(StairX0+(Risers-1)*StairRun,LiftX0,LiftZ0,HD);
        Slab(LiftX1,HW,LiftZ0,HD);
        // structure: perimeter wall boxes (interior face black), glass ground band at the front
        float zf=-HD,zb=HD;
        // front wall above the band
        B(blackMatte,-HW,PortalX0+1.4f,Band,Parapet,zf,zf+.3f,true);B(blackMatte,PortalX1-1.4f,HW,Band,Parapet,zf,zf+.3f,true);
        B(blackMatte,PortalX0+1.4f,PortalX1-1.4f,7.6f,Parapet,zf,zf+.3f,true);
        B(black,-HW+.3f,HW-.3f,SlabTop,FFL,zf,zf+.3f);                                            // finish strip behind the glass
        // sides and back: full height, door / portal openings
        WallZ(blackMatte,-HW+.15f,-HD,HD,FFL,Parapet,.3f,(-5.4f,-4.2f,2.6f));                         // west: staff door
        WallZ(blackMatte,HW-.15f,-HD,HD,FFL,Parapet,.3f,(-5.4f,-4.2f,2.6f),(16f,20f,3.6f));          // east: staff door + plant room roller door
        WallX(blackMatte,HD-.15f,-HW,HW,FFL,Parapet,.3f,(-38f,-32f,4.0f),(-55.6f,-54.4f,2.6f));       // back: loading door, staff door
        // B1 retaining walls, the ramp portal in the back wall
        WallZ(garageWhite,-HW+.15f,-HD,HD,B1-.30f,SlabBottom,.3f);
        WallZ(garageWhite,HW-.15f,-HD,HD,B1-.30f,SlabBottom,.3f);
        WallX(garageWhite,-HD+.15f,-HW+.3f,HW-.3f,B1-.30f,SlabBottom,.3f);
        WallX(garageWhite,HD-.15f,-HW+.3f,HW-.3f,B1-.30f,SlabBottom,.3f,(RampX0,RampX1,SlabBottom-.01f));
        B(black,RampX0,RampX1,-1.2f,Y0+.2f,HD,HD+.06f);                                      // portal lintel facing the ramp
        BraxChampionsLetters3D.Text(m,whiteLit,black,"PARKING E1",new Vector3((RampX0+RampX1)*.5f,-.95f,HD+.055f),0f,.42f,.04f,.14f,6.4f);
        B1Floor();
        // roof slab, parapet coping, membrane
        B(roofMembrane,-HW+.3f,HW-.3f,RoofIn,Roof,-HD+.3f,HD-.3f);
        Col(-HW,HW,RoofIn,Roof,-HD,HD,"dach");
        B(black,-HW-.06f,HW+.06f,Parapet,Parapet+.06f,-HD-.06f,-HD+.36f);
        B(black,-HW-.06f,HW+.06f,Parapet,Parapet+.06f,HD-.36f,HD+.06f);
        foreach(float s in new[]{-1f,1f})B(black,s<0?-HW-.06f:HW-.36f,s<0?-HW+.36f:HW+.06f,Parapet,Parapet+.06f,-HD+.36f,HD-.36f);
        RoofEquipment();
        Facades();
        Portal();
        Mural();
    }

    static void B1Floor()
    {
        B(garageFloor,-HW+.3f,HW-.3f,B1-.30f,B1,-HD+.3f,HD-.3f);
        Col(-HW,HW,B1-.30f,B1,-HD,HD,"posadzka B1");
    }

    static void RoofEquipment()
    {
        // board roof plan: AC units at both ends, two skylight boxes over the middle
        foreach(var (x,z) in new[]{(-52f,-12f),(-52f,6f),(-44f,-12f),(-44f,6f),(46f,-12f),(46f,6f),(54f,-12f),(54f,6f),(-30f,16f),(30f,16f)})
        {
            B(hvac,x-1.6f,x+1.6f,Roof,Roof+1.5f,z-1.0f,z+1.0f,true);
            foreach(float fx in new[]{-.8f,.8f})
            {
                m.Cylinder(black,new Vector3(x+fx,Roof+1.5f,z),.55f,.08f,20);
                m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(x+fx,Roof+1.58f,z),.10f,.06f,12);
            }
        }
        foreach(float x in new[]{-12f,22f})
        {
            B(black,x-3.2f,x+3.2f,Roof,Roof+.45f,-6.2f,6.2f,true);
            B(clearGlass,x-3.0f,x+3.0f,Roof+.45f,Roof+.47f,-6.0f,6.0f);
            for(float z=-4f;z<=4.01f;z+=2f)B(black,x-3.0f,x+3.0f,Roof+.47f,Roof+.53f,z-.03f,z+.03f);
        }
        // ducts between the units
        B(hvac,-52f,-44f,Roof+.2f,Roof+.75f,-2.5f,-1.5f);B(hvac,46f,54f,Roof+.2f,Roof+.75f,-2.5f,-1.5f);
    }

    // facades: pilasters on the 10 m module with red LED lines, glass band 0..5.4 at the front, black panels above with joints
    public const float PortalX0=-2.5f,PortalX1=12.5f,SideDoorW=-37.5f,SideDoorE=52f;
    // glass between black mullions (a, b and evenly at most `pane` apart), z = outer glass plane, colliders on the panes
    static void GlassRun(float a,float b,float y0,float y1,float z,float pane,bool transom)
    {
        if(b-a<.2f)return;
        int n=Mathf.Max(1,Mathf.CeilToInt((b-a)/pane));float st=(b-a)/n;
        for(int i=0;i<=n;i++){float x=a+i*st;B(black,x-.05f,x+.05f,y0,y1,z-.10f,z+.06f);}
        for(int i=0;i<n;i++)
        {
            float p0=a+i*st+.05f,p1=a+(i+1)*st-.05f;
            B(facadeGlass,p0,p1,y0,y1,z-.02f,z,true);
            if(transom)B(black,p0,p1,2.86f,2.94f,z-.10f,z+.06f);
        }
    }
    // the 10 m module k clipped by the portal (piers x -2.5..-1.1 and 11.1..12.5)
    static bool Module(int k,out float a,out float b)
    {
        a=k*10f+.30f;b=(k+1)*10f-.30f;
        if(b<=PortalX0||a>=PortalX1)return true;
        if(a<PortalX0){b=PortalX0;return true;}
        if(b>PortalX1){a=PortalX1;return true;}
        return false;
    }
    static void Facades()
    {
        // ---- FRONT (z -25)
        float zf=-HD;
        for(int k=-6;k<=6;k++)
        {
            float x=k*10f;
            if(x>PortalX0-.3f&&x<PortalX1+.3f)continue;                                      // the portal piers stand there
            B(black,x-.30f,x+.30f,Y0,Parapet,zf-.40f,zf,true);
            B(redLed,x-.04f,x+.04f,.6f,Parapet-.6f,zf-.412f,zf-.40f);
        }
        for(int k=-6;k<6;k++)
        {
            if(!Module(k,out float a,out float b))continue;
            float door=k==-4?SideDoorW:k==5?SideDoorE:float.NaN;
            if(float.IsNaN(door))GlassRun(a,b,FFL,Band-.12f,zf,2.5f,true);
            else
            {
                GlassRun(a,door-1.25f,FFL,Band-.12f,zf,2.5f,true);
                GlassRun(door+1.25f,b,FFL,Band-.12f,zf,2.5f,true);
                B(black,door-1.25f,door+1.25f,2.6f,2.72f,zf-.10f,zf+.06f);
                GlassRun(door-1.25f,door+1.25f,2.72f,Band-.12f,zf,2.6f,false);
                Door(door<0?"E1_SIDE_W":"E1_SIDE_E",door<0?"E1 side entrance west (bowling) / emergency exit":"E1 side entrance east (lounge) / emergency exit",
                     door,FFL,zf-.01f,180f,2.4f,2.6f-FFL,true);
            }
            B(black,a,b,Band-.12f,Band,zf-.12f,zf+.06f);                                    // head
            B(black,a,b,Y0,FFL,zf-.12f,zf);                                                 // sill
            // upper band: black panels with 2 cm joints, red light canopy line and the LED line under it
            for(int r=0;r<4;r++)
            {
                float y0=Band+.15f+r*1.79f,y1=Mathf.Min(y0+1.77f,Parapet-.05f);
                int nc=Mathf.Max(1,Mathf.RoundToInt((b-a)/3.1f));
                for(int c=0;c<nc;c++){float p0=a+c*(b-a)/nc+.01f,p1=a+(c+1)*(b-a)/nc-.01f;B(black,p0,p1,y0,y1,zf-.06f,zf);}
            }
            B(redLed,a,b,Band,Band+.15f,zf-.10f,zf);
            B(redLed,a,b,Band-.02f,Band,zf-.30f,zf-.12f);
        }
        B(redLed,-HW-.06f,HW+.06f,Parapet-.10f,Parapet-.02f,zf-.42f,zf-.40f);
        // ---- SIDES: pilasters every 10 m with LED lines, panels with joints, staff doors, the east roller door
        foreach(float s in new[]{-1f,1f})
        {
            float xo=s*HW;
            foreach(float z in new[]{-24.7f,-10f,0f,10f,24.7f})
            {
                B(black,s<0?xo-.40f:xo,s<0?xo:xo+.40f,Y0,Parapet,z-.30f,z+.30f,true);
                B(redLed,s<0?xo-.412f:xo+.40f,s<0?xo-.40f:xo+.412f,.6f,Parapet-.6f,z-.04f,z+.04f);
            }
            foreach(var (a,b) in new[]{(-24.4f,-10.3f),(-9.7f,-.3f),(.3f,9.7f),(10.3f,24.4f)})
                for(int r=0;r<7;r++)
                {
                    float y0=Y0+.3f+r*1.75f,y1=Mathf.Min(y0+1.73f,Parapet-.05f);
                    int nc=Mathf.Max(1,Mathf.RoundToInt((b-a)/3.1f));
                    for(int c=0;c<nc;c++)
                    {
                        float p0=a+c*(b-a)/nc+.01f,p1=a+(c+1)*(b-a)/nc-.01f;
                        if(y0<2.8f&&p1>-5.5f&&p0<-4.1f)continue;                                // staff door
                        if(s>0&&y0<3.8f&&p1>15.9f&&p0<20.1f)continue;                         // roller door
                        B(black,s<0?xo-.06f:xo,s<0?xo:xo+.06f,y0,y1,p0,p1);
                    }
                }
            Door(s<0?"E1_STAFF_W":"E1_STAFF_E","E1 staff door "+(s<0?"west":"east"),s*(HW-.15f),FFL,-4.8f,s<0?90f:-90f,1.2f,2.6f-FFL,false);
        }
        for(float y=FFL+.1f;y<3.55f;y+=.12f)B(hvac,HW+.02f,HW+.06f,y,y+.09f,16.05f,19.95f);
        B(hvac,HW,HW+.02f,FFL,3.6f,16.0f,20.0f,true);
        // ---- BACK: pilasters, panels, the loading door shutter + dock, staff door, the ramp portal
        float zb=HD;
        for(int k=-6;k<=6;k++)
        {
            float x=k*10f+(k==-6?.3f:k==6?-.3f:0f);
            B(black,x-.30f,x+.30f,Y0,Parapet,zb,zb+.40f,true);
            B(redLed,x-.04f,x+.04f,.6f,Parapet-.6f,zb+.40f,zb+.412f);
        }
        for(int k=-6;k<6;k++)
        {
            float a=k*10f+.30f,b=(k+1)*10f-.30f;
            for(int r=0;r<7;r++)
            {
                float y0=Y0+.3f+r*1.75f,y1=Mathf.Min(y0+1.73f,Parapet-.05f);
                for(int c=0;c<3;c++)
                {
                    float p0=a+c*(b-a)/3f+.01f,p1=a+(c+1)*(b-a)/3f-.01f;
                    if(y0<4.2f&&p1>-38.1f&&p0<-31.9f)continue;
                    if(y0<2.8f&&p1>-55.7f&&p0<-54.3f)continue;
                    if(y0<1.0f&&p1>RampX0-.4f&&p0<RampX1+.4f)continue;
                    B(black,p0,p1,y0,y1,zb,zb+.06f);
                }
            }
        }
        for(float y=FFL+.1f;y<3.95f;y+=.12f)B(hvac,-37.95f,-32.05f,y,y+.09f,zb+.02f,zb+.06f);
        B(hvac,-38f,-32f,FFL,4.0f,zb,zb+.02f,true);
        B(concrete,-39.4f,-30.6f,Y0,Y0+1.1f,zb+.40f,zb+3.0f,true);                                       // loading dock
        Door("E1_STAFF_N","E1 staff door north (plant room)",-55f,FFL,HD-.15f,0f,1.2f,2.6f-FFL,false);
    }

    // portal over the hall: black frame 15 x 15 m projecting 3 m, recessed glass wall, canopy, double doors
    static void Portal()
    {
        float x0=PortalX0,x1=PortalX1,zf=-HD,zp=-HD-3f,g0=x0+1.4f,g1=x1-1.4f;
        B(black,x0,g0,Y0,PortalTop,zp,zf,true);B(black,g1,x1,Y0,PortalTop,zp,zf,true);
        B(black,g0,g1,7.6f,PortalTop,zp,zf,true);
        B(redLed,g0,g0+.08f,FFL,7.6f,zp,zp+.6f);B(redLed,g1-.08f,g1,FFL,7.6f,zp,zp+.6f);
        B(redLed,g0+.08f,g1-.08f,7.52f,7.6f,zp,zp+.6f);
        B(redLed,x0,x1,PortalTop-.14f,PortalTop-.04f,zp-.012f,zp+.04f);
        // the hall's glass wall in the portal (z -25): two double doors 2.6 m, glass to 7.6 m
        float zg=zf;
        var doorX=new[]{3.2f,6.8f};
        GlassRun(g0+.08f,doorX[0]-1.35f,FFL,2.9f,zg,1.2f,false);
        GlassRun(doorX[0]+1.35f,doorX[1]-1.35f,FFL,2.9f,zg,1.2f,false);
        GlassRun(doorX[1]+1.35f,g1-.08f,FFL,2.9f,zg,1.2f,false);
        B(black,g0+.08f,g1-.08f,2.9f,3.0f,zg-.10f,zg+.06f);                                          // transom
        GlassRun(g0+.08f,g1-.08f,3.0f,7.52f,zg,1.25f,false);
        foreach(float dx in doorX)
            Door("E1_MAIN_"+(dx<5f?"W":"E"),"E1 main entrance "+(dx<5f?"west":"east")+" leaf pair",dx,FFL,zg-.01f,180f,2.6f,2.9f-FFL,true);
        // canopy 6 m out at 5.0 m: black slab, red LED edge, downlights
        float cz=zp-3f;
        B(black,x0-1.0f,x1+1.0f,5.0f,5.45f,cz,zp,true);
        B(redLed,x0-1.0f,x1+1.0f,5.0f,5.45f,cz-.03f,cz);
        for(float x=x0;x<=x1+.01f;x+=2.5f)for(float z=cz+1f;z<zp-.5f;z+=2f)m.Cylinder(ledCool,new Vector3(x,4.99f,z),.12f,.012f,12,Quaternion.identity,false,true);
        // granite planters with the board's flowers (Poly Haven) either side of the portal
        var flowers=Group(root,"kwiaty portalu (Poly Haven)");
        foreach(float x in new[]{x0-1.8f,x1+1.8f})BraxChampionsPromenadeFurniture.Planter(m,cols,flowers,x,zp-.75f);
    }

    // the owner's mural (10.11 21:17-21:28: "masz tylko upscale zrób, dobrze to zamontuj na ścianie", "zdjęcie to mural na
    // ścianę, narożnik jak w projekcie"): his picture, upscaled to 4096 x 1536, printed on an aluminium composite panel
    // 17.0 x 6.375 m (the picture's own proportion) mounted flat on the front facade at the west corner, over the glass of
    // the bowling hall: from the building corner (x -60.4, flush with the side pilasters) east, in the upper band
    // (y 5.85..12.23, under the parapet). The panel stands on a steel subframe on the pilaster faces (z -25.40) and on
    // standoffs to the panels between them; slim black aluminium edge trim round the print.
    static void Mural()
    {
        float x0=-60.40f,x1=x0+17.0f,y0=5.85f,y1=y0+17.0f*768f/2048f,zw=-HD-.40f,zb=zw-.01f,zp=zw-.05f;
        // panel body: top, bottom, the two ends (its back against the subframe, the print is its face)
        Q(black,new Vector3(x0,y1,zb),new Vector3(x1,y1,zb),new Vector3(x1,y1,zp),new Vector3(x0,y1,zp),Vector3.up);
        Q(black,new Vector3(x0,y0,zb),new Vector3(x1,y0,zb),new Vector3(x1,y0,zp),new Vector3(x0,y0,zp),Vector3.down);
        foreach(float x in new[]{x0,x1})Q(black,new Vector3(x,y0,zb),new Vector3(x,y1,zb),new Vector3(x,y1,zp),new Vector3(x,y0,zp),new Vector3(x==x0?-1f:1f,0,0));
        Q(black,new Vector3(x0,y0,zb),new Vector3(x1,y0,zb),new Vector3(x1,y1,zb),new Vector3(x0,y1,zb),Vector3.forward);
        TexQuad(root,"E1 mural owner print",muralArt,new Vector3((x0+x1)*.5f,(y0+y1)*.5f,zp),x1-x0,y1-y0,true);
        // edge trim: 25 mm black aluminium angle round the print, 15 mm proud of it
        RB(black,x0-.025f,x1+.025f,y1,y1+.025f,zp-.015f,zb,.006f,false,2);RB(black,x0-.025f,x1+.025f,y0-.025f,y0,zp-.015f,zb,.006f,false,2);
        RB(black,x0-.025f,x0,y0,y1,zp-.015f,zb,.006f,false,2);RB(black,x1,x1+.025f,y0,y1,zp-.015f,zb,.006f,false,2);
        // standoffs to the recessed panels (the pilaster faces carry the panel directly)
        foreach(float x in new[]{-57.2f,-54.0f,-46.6f,-44.6f})foreach(float y in new[]{y0+.5f,y1-.5f})
            m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(x,y,zb),.03f,zb-(-HD-.06f)>0?0f:(-HD-.06f)-zb,12,Quaternion.Euler(90,0,0),false,false);
        Col(x0,x1,y0,y1,zp,zw,"mural");
    }

    // ================================================================ SIGNS
    static void FrontSigns()
    {
        float zp=-HD-3f;
        // "E1" red, 3.6 m, and "CENTRUM ROZRYWKI" white on the portal head
        // the name sign (owner 21:21 "ten szyld też w 3D"): E1 as solid letters 0.9 m deep, CENTRUM ROZRYWKI 0.3 m deep,
        // a red LED line under the name on the portal head
        BraxChampionsLetters3D.Text(m,red,black,"E1",new Vector3(5f,10.4f,zp+.01f),180f,3.6f,.90f,.18f,11f);
        BraxChampionsLetters3D.Text(m,whiteLit,black,"CENTRUM ROZRYWKI",new Vector3(5f,8.55f,zp+.01f),180f,.90f,.30f,.14f,12.2f);
        B(redLed,-.8f,10.8f,8.20f,8.24f,zp-.012f,zp);
        Signs+=2;
        // east wing: five 3D signs with pictograms (board: KRĘGLE•BILARD•GRY•BAR•LOUNGE), centred on the panels
        // one sign per facade module, between the pilasters (they stand 40 cm proud: a sign across one would be cut)
        var items=new[]{("KRĘGLE",16.1f),("BILARD",25.0f),("GRY",35.0f),("BAR",45.0f),("LOUNGE",55.0f)};
        float y=7.35f,zf=-HD-.06f;
        foreach(var (t,x) in items)
        {
            B(black,x-3.5f,x+3.5f,y-.75f,y+1.15f,zf-.12f,zf);
            B(redLed,x-3.5f,x+3.5f,y-.75f,y-.71f,zf-.132f,zf-.12f);
            BraxChampionsLetters3D.Text(m,whiteLit,black,t,new Vector3(x+.8f,y-.30f,zf-.115f),180f,.85f,.12f,.12f,4.3f);
            Pictogram(t,new Vector3(x-2.5f,y+.20f,zf-.12f));
            Signs++;
        }
        // hall side lettering over the openings
        BraxChampionsLetters3D.Text(m,chrome,black,"KRĘGLE",new Vector3(.14f,7.6f,-9f),90f,.95f,.10f,.12f,8f);
        BraxChampionsLetters3D.Text(m,chrome,black,"BILARD • BAR",new Vector3(9.86f,7.6f,-6f),-90f,.95f,.10f,.12f,9f);
        Signs+=2;
    }
    // pictograms in front of a sign panel (face toward -z), centred on c
    static void Pictogram(string kind,Vector3 c)
    {
        var f=new Vector3(0,0,-1);
        switch(kind)
        {
            case "KRĘGLE":
                Pin(c+new Vector3(-.25f,-.85f,-.25f),4.2f,14);Pin(c+new Vector3(.25f,-.85f,-.25f),4.2f,14);
                Sphere(muralBall,c+new Vector3(0f,-.62f,-.55f),.24f,14,9);break;
            case "BILARD":
                for(int r=0;r<3;r++)for(int i=0;i<=r;i++)
                    Sphere(i==0&&r==0?whiteLit:(r+i)%2==0?red:chrome,c+new Vector3((i-r*.5f)*.36f,.35f-r*.32f,-.2f),.17f,12,8);break;
            case "GRY":
                B(whiteLit,c.x-.55f,c.x+.55f,c.y-.30f,c.y+.20f,c.z-.18f,c.z);
                foreach(float s in new[]{-1f,1f})m.Cylinder(whiteLit,c+new Vector3(s*.55f,-.05f,-.18f),.25f,.18f,16,Quaternion.Euler(90,0,0),true);
                Sphere(red,c+new Vector3(.32f,-.02f,-.2f),.07f,10,6);Sphere(chrome,c+new Vector3(.46f,-.14f,-.2f),.07f,10,6);
                B(black,c.x-.42f,c.x-.18f,c.y-.08f,c.y,c.z-.2f,c.z-.17f);B(black,c.x-.34f,c.x-.26f,c.y-.16f,c.y+.08f,c.z-.2f,c.z-.17f);break;
            case "BAR":
                m.Cylinder(whiteLit,c+new Vector3(0,-.75f,-.15f),.24f,.04f,16,Quaternion.identity,true);
                m.Cylinder(whiteLit,c+new Vector3(0,-.71f,-.15f),.03f,.52f,10,Quaternion.identity,false);
                m.Cylinder(whiteLit,c+new Vector3(0,-.19f,-.15f),.02f,.55f,16,Quaternion.identity,true,false,.42f);
                Sphere(red,c+new Vector3(.18f,.30f,-.15f),.08f,10,6);break;
            default:   // LOUNGE: an armchair
                B(whiteLit,c.x-.55f,c.x+.55f,c.y-.55f,c.y-.25f,c.z-.45f,c.z-.05f);
                B(whiteLit,c.x-.55f,c.x+.55f,c.y-.25f,c.y+.35f,c.z-.12f,c.z-.02f);
                foreach(float s in new[]{-1f,1f})B(whiteLit,c.x+s*.55f-.08f,c.x+s*.55f+.08f,c.y-.55f,c.y+.05f,c.z-.45f,c.z-.02f);break;
        }
    }

    // ================================================================ HALL (x 0..10, full depth, 12 m)
    static void Hall()
    {
        float x0=.15f,x1=9.85f,z0=-HD+.3f,z1=HD-.3f;
        // floor: black marble (stair hole and lift shaft left open)
        B(hallFloor,x0,x1,SlabTop,FFL,z0,LiftZ0);
        B(hallFloor,x0,StairX0,SlabTop,FFL,LiftZ0,z1);
        B(hallFloor,StairX0,StairX0+(Risers-1)*StairRun,SlabTop,FFL,LiftZ0,StairZ0);
        B(hallFloor,StairX0+(Risers-1)*StairRun,LiftX0,SlabTop,FFL,LiftZ0,z1);
        // walls to the zones (x 0 and x 10), openings: bowling (z -14..-4, 4.5 m), corridor (13.1..14.9), lounge, billiards
        WallZ(blackMatte,0f,z0,z1,FFL,RoofIn,.3f,(-14f,-4f,4.5f),(13.1f,14.9f,3.0f));
        WallZ(blackMatte,10f,z0,z1,FFL,RoofIn,.3f,(-23.2f,-17.2f,4.0f),(-8f,2f,4.5f));
        foreach(float x in new[]{.31f,9.69f})
            for(float z=-22f;z<=22.01f;z+=4f)
            {
                if((z>-15f&&z<-3f)||(x>5&&((z>-24f&&z<-16f)||(z>-9f&&z<3f)))||(x<5&&z>12f))continue;
                B(redLed,x<5?.15f:9.838f,x<5?.162f:9.85f,.6f,10.8f,z-.03f,z+.03f);
            }
        // ceiling 11.0: black coffers with a red LED frame and cool downlights
        B(blackMatte,x0,x1,11.0f,11.1f,z0,z1);
        for(float z=z0+2f;z<z1-1f;z+=4f){B(redLed,x0+.6f,x1-.6f,10.97f,11.0f,z-.03f,z+.03f);
            foreach(float x in new[]{2.5f,7.5f})m.Cylinder(ledCool,new Vector3(x,10.985f,z+2f),.14f,.015f,12,Quaternion.identity,false,true);}
        // reception: curved desk (black lacquered walnut, rounded ends), white marble top with an overhang, red LED kick
        // line, 140 deg arc facing the doors
        float rz=-2.0f,ccx=5f,ccz=rz+3.3f,R=3.0f,a0=-160f*Mathf.Deg2Rad,a1=-20f*Mathf.Deg2Rad,top=FFL+1.0f;
        var cc=new Vector3(ccx,0,ccz);
        Arc(lacquer,cc,R-.3f,R+.3f,a0,a1,FFL,top,40,false);
        Arc(marbleWhite,cc,R-.42f,R+.36f,a0,a1,top,top+.05f,40,false);
        Arc(redLed,cc,R+.30f,R+.312f,a0,a1,FFL+.08f,FFL+.12f,40);
        foreach(float a in new[]{a0,a1})
        {
            var outDir=a==a0?new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a)):new Vector3(-Mathf.Sin(a),0,Mathf.Cos(a));
            HalfRound(lacquer,cc+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R,.30f,outDir,FFL,top,20);
            HalfRound(marbleWhite,cc+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(R-.03f),.39f,outDir,top,top+.05f,20);
        }
        Col(2.0f,8.0f,FFL,top+.05f,rz,rz+2.6f,"recepcja");
        BraxChampionsLetters3D.Text(m,chrome,black,"E1",new Vector3(5f,.55f,rz+.01f),180f,.40f,.03f,.14f,1.2f);
        // back wall of the reception: black, the live status board of all lanes and tables, stainless lettering under it
        B(black,2.2f,7.8f,FFL,4.2f,6.0f,6.3f,true);
        RB(black,2.55f,7.45f,1.80f,4.10f,5.95f,6.0f,.02f,false,2);
        boardR.Add(Screen(scrGroup,"tablica stanu recepcja",scrBoard,new Vector3(5f,2.95f,5.938f),4.6f,2.1f,0f));
        BraxChampionsLetters3D.Text(m,chrome,black,"CENTRUM ROZRYWKI",new Vector3(5f,1.30f,6.01f),180f,.32f,.04f,.14f,4.6f);
        // ticket kiosks (rounded, black, a portrait screen toward the hall) and leather benches
        foreach(float z in new[]{-18f,-14f})foreach(float x in new[]{1.0f,9.0f})
        {
            RB(black,x-.3f,x+.3f,FFL,1.6f,z-.25f,z+.25f,.06f,true);
            float fx=x<5?x+.312f:x-.312f;
            kioskR.Add(Screen(scrGroup,"kiosk "+x+" "+z,scrKiosk,new Vector3(fx,1.18f,z),.36f,.45f,x<5?-90f:90f));
        }
        foreach(float z in new[]{-9f,10f})Bench(new Vector3(5f,0,z),Vector3.right,2.6f,.52f);
        // glass guard round the stair hole, the lift head: black box with a stainless frame and its call panel
        float sx1=StairX0+(Risers-1)*StairRun;
        B(clearGlass,StairX0,sx1,FFL,FFL+1.1f,StairZ0-.02f,StairZ0,true);
        B(clearGlass,StairX0,StairX0+.02f,FFL,FFL+1.1f,StairZ0,HD-.3f,true);
        B(BraxChampionsHarbour.Steel,StairX0,sx1,FFL+1.1f,FFL+1.15f,StairZ0-.04f,StairZ0+.02f);
        BraxChampionsLetters3D.Text(m,chrome,black,"PARKING B1",new Vector3(3.2f,2.2f,HD-.29f),180f,.30f,.03f,.14f,4f);
    }

    // ================================================================ BOWLING (x -59.7..0) - played (BraxChampionsKregle)
    public const float LaneEnd=FoulZ+LaneLen+1.4f,PinRow=.264f,PinSpacing=.305f,BallR=.109f,ApproachZ0=-17.6f,ReturnZ=-18.4f,ReturnTop=FFL+.78f;
    static float PairX(int p)=>LaneX0+PairPitch*.5f+p*PairPitch;
    static float LaneX(int l)=>PairX(l/2)+(l%2==0?-.815f:.815f);
    static Vector3 W(float x,float y,float z)=>root.TransformPoint(new Vector3(x,y,z)*K);
    static void Bowling()
    {
        float x0=-HW+.3f,x1=-.15f,z0=-HD+.3f;
        float laneEnd=LaneEnd,mask0=laneEnd,machine=13.0f;
        // floor: woven wool carpet round the lanes, machine room concrete
        B(carpet,x0,x1,SlabTop,FFL,z0,ApproachZ0);
        B(carpet,x0,LaneX0,SlabTop,FFL,ApproachZ0,machine);
        B(carpet,LaneX0+12*PairPitch,x1,SlabTop,FFL,ApproachZ0,machine);
        B(techFloor,LaneX0,LaneX0+12*PairPitch,SlabTop,FFL,mask0+.3f,machine-.1f);
        B(carpet,x0,LaneX0,SlabTop,FFL,machine,14.9f);B(carpet,LaneX0+12*PairPitch,x1,SlabTop,FFL,machine,14.9f);
        B(carpet,LaneX0,LaneX0+12*PairPitch,SlabTop,FFL,machine+.1f,14.9f);                          // back corridor
        B(blackMatte,x0,x1,Ceiling,Ceiling+.1f,z0,14.9f);
        float hz=FoulZ+LaneLen;
        for(int p=0;p<12;p++)
        {
            float xp=PairX(p);
            foreach(float s in new[]{-1f,1f})
            {
                float xc=xp+s*.815f;
                // maple approach (lane + gutters wide), black foul line, maple lane bed, black steel gutters 6 cm deep, the
                // pin deck to the pit; the approach dots and the 7 target arrows as dark maple inlays FLUSH (cut out of the lane)
                float a0=ApproachZ0,a1=FoulZ-.02f;
                B(maple,xc-.775f,xc+.775f,SlabTop,FFL,a0,a1);
                B(black,xc-.775f,xc+.775f,SlabTop,FFL,FoulZ-.02f,FoulZ+.02f);
                float ar=FoulZ+4.6f;                                                                  // the arrows 4.6 m past the foul line
                B(maple,xc-.5335f,xc+.5335f,SlabTop,FFL,FoulZ+.02f,ar-.10f);
                for(int k=0;k<7;k++)                                                                  // arrows: 7 dark wedges across the lane
                {
                    float ax=xc-.4572f+k*.1524f,az=ar+(k<4?k:6-k)*.15f;
                    B(maple,k==0?xc-.5335f:ax-.1524f+.02f,ax-.02f,SlabTop,FFL,ar-.10f,ar+.70f);
                    B(lacquer,ax-.02f,ax+.02f,SlabTop,FFL,ar-.10f,az);                                  // dark walnut inlay in the boards (flush)
                    B(maple,ax-.02f,ax+.02f,SlabTop,FFL,az,ar+.70f);
                }
                B(maple,xc+.4572f+.02f,xc+.5335f,SlabTop,FFL,ar-.10f,ar+.70f);
                B(maple,xc-.5335f,xc+.5335f,SlabTop,FFL,ar+.70f,laneEnd-.3f);
                foreach(float g in new[]{-1f,1f})B(gutter,g<0?xc-.775f:xc+.5335f,g<0?xc-.5335f:xc+.775f,SlabTop,FFL-.06f,FoulZ+.02f,laneEnd-.3f);
                // overhead score screen on a ceiling rod (the live screen face is its own object, BraxChampionsKregle paints it)
                RB(black,xc-.72f,xc+.72f,3.05f,3.95f,-16.95f,-16.82f,.03f,false,3);
                m.Cylinder(steelPolished,new Vector3(xc,3.95f,-16.885f),.025f,Ceiling-3.95f,10);
            }
            // capping between the pairs (ball-return track cover, black), divider in the pair, kickbacks at the pin deck
            float cap=xp+PairPitch*.5f;
            if(p<11){RB(black,cap-.15f,cap+.15f,SlabTop,FFL+.12f,FoulZ+.02f,laneEnd-.3f,.03f,true,2);B(carpet,cap-.15f,cap+.15f,SlabTop,FFL,ApproachZ0,FoulZ+.02f);}
            RB(black,xp-.04f,xp+.04f,SlabTop,FFL+.25f,FoulZ+.02f,hz0(),.02f,false,2);
            RB(lacquer,xp-.04f,xp+.04f,FFL,1.3f,hz0(),laneEnd+.3f,.02f,true,2);                     // division kickback between the pin decks
            B(black,xp-.04f,xp+.04f,SlabTop,FFL,ApproachZ0,FoulZ+.02f);
            foreach(float s in new[]{-1f,1f})RB(lacquer,xp+s*1.59f-.06f,xp+s*1.59f+.06f,FFL,1.3f,hz0(),laneEnd+.3f,.03f,true,2);
            // ball return: rounded black hood (78 cm) with polished steel rails on top, purple light line
            RB(black,xp-.36f,xp+.36f,FFL,ReturnTop,-19.25f,-17.6f,.10f,true);
            m.Tube(steelPolished,new Vector3(xp-.26f,ReturnTop+.02f,-19.1f),new Vector3(xp-.26f,ReturnTop+.02f,-17.7f),.02f,10);
            m.Tube(steelPolished,new Vector3(xp+.26f,ReturnTop+.02f,-19.1f),new Vector3(xp+.26f,ReturnTop+.02f,-17.7f),.02f,10);
            B(purpleLed,xp-.355f,xp+.355f,FFL+.42f,FFL+.45f,-19.262f,-19.25f);
            // scorer console behind the return: rounded black, two live screens (left lane, right lane) toward the settee
            RB(black,xp-.48f,xp+.48f,FFL,FFL+.95f,-19.75f,-19.45f,.05f,true,3);
            // U settee in plum velvet facing the lanes (back sofa + two wings), an oval marble table on a steel pedestal
            float sz=-22.5f;
            Sofa(velvet,new Vector3(xp,0,sz),Vector3.right,Vector3.forward,3.2f,3,false);
            foreach(float sg in new[]{-1f,1f})Sofa(velvet,new Vector3(xp+sg*1.6f,0,sz+1.71f),Vector3.forward,new Vector3(-sg,0,0),1.56f,2,false);
            m.Cylinder(steelPolished,new Vector3(xp,FFL,-20.6f),.22f,.02f,28);m.Cylinder(steelPolished,new Vector3(xp,FFL+.02f,-20.6f),.04f,.40f,16);
            Oval(marbleWhite,new Vector3(xp,FFL+.42f,-20.6f),.32f,.42f,.04f);
            Col(xp-.32f,xp+.32f,FFL,FFL+.46f,-21.02f,-20.18f,"stolik");
        }
        B(black,LaneX0,LaneX0+12*PairPitch,SlabTop,SlabTop+.02f,laneEnd-.3f,laneEnd+.3f);              // pit behind the pin decks
        // masking units over the pin decks: black panel, purple LED lines, the lane numbers
        float mz=mask0-.8f;
        B(black,LaneX0,LaneX0+12*PairPitch,2.6f,Ceiling,mz-.25f,mz);
        for(int i=0;i<3;i++)B(purpleLed,LaneX0,LaneX0+12*PairPitch,2.75f+i*.35f,2.79f+i*.35f,mz-.262f,mz-.25f);
        for(int p=0;p<12;p++)BraxChampionsLetters3D.Text(m,whiteLit,black,(2*p+1).ToString()+"   "+(2*p+2).ToString(),new Vector3(PairX(p),4.3f,mz-.24f),180f,.42f,.03f,.14f,3f);
        for(int l=0;l<Lanes;l++)
        {
            float xc=LaneX(l);
            B(hvac,xc-.7f,xc+.7f,1.4f,2.55f,mz,mz+2.2f);
            B(black,xc-.72f,xc+.72f,1.3f,1.4f,mz,mz+2.2f);
        }
        WallX(blackMatte,machine,LaneX0,LaneX0+12*PairPitch,FFL,Ceiling,.2f,(-36.6f,-35.4f,2.4f));
        Door("E1_MACHINE","E1 bowling machine room",-36f,FFL,machine,0f,1.2f,2.4f-FFL,false);
        B(blackMatte,LaneX0+12*PairPitch,LaneX0+12*PairPitch+.2f,FFL,Ceiling,mz,machine,true);
        B(blackMatte,LaneX0-.2f,LaneX0,FFL,Ceiling,mz,machine,true);
        for(float z=-22f;z<12f;z+=3f)B(purpleLed,x0,x0+.012f,.8f,Ceiling-.4f,z-.03f,z+.03f);
        // side area: shoe counter (rounded, walnut + marble), shoe shelves, ball racks with real house balls, café tables
        float sx=LaneX0+12*PairPitch+.4f;
        RB(lacquer,sx+1f,sx+7f,FFL,FFL+1.05f,4.0f,4.7f,.05f,true);
        RB(marbleWhite,sx+.95f,sx+7.05f,FFL+1.05f,FFL+1.10f,3.95f,4.75f,.02f,false,3);
        RB(lacquer,sx+1f,sx+7f,FFL,2.4f,7.6f,8.0f,.03f,true);
        for(int r=0;r<4;r++)for(int i=0;i<8;i++)RB(i%2==0?rubber:charcoal,sx+1.2f+i*.7f,sx+1.75f+i*.7f,.5f+r*.45f,.62f+r*.45f,7.3f,7.6f,.04f,false,3);   // shoes
        var props=Group(root,"E1 kręgle | kule na stojakach i zwrotnicach");
        for(int k=0;k<3;k++)
        {
            float rz=-10f+k*4f;
            RB(steelPolished,sx+.2f,sx+.3f,FFL,1.2f,rz,rz+2.4f,.03f,true,2);
            for(int lv=0;lv<3;lv++)
            {
                m.Tube(steelPolished,new Vector3(sx+.36f,.47f+lv*.35f,rz),new Vector3(sx+.36f,.47f+lv*.35f,rz+2.4f),.015f,8);
                m.Tube(steelPolished,new Vector3(sx+.64f,.47f+lv*.35f,rz),new Vector3(sx+.64f,.47f+lv*.35f,rz+2.4f),.015f,8);
                for(int i=0;i<9;i++)Ball(props,"kula na stojaku",houseBalls[(i+lv+k)%houseBalls.Length],new Vector3(sx+.5f,.47f+lv*.35f+BallR*.8f,rz+.14f+i*.26f),BallR,0f);
            }
        }
        for(int k=0;k<4;k++)
        {
            float tx=sx+4.5f+(k%2)*5f,tz=-8f+(k/2)*6f;
            m.Cylinder(steelPolished,new Vector3(tx,FFL,tz),.26f,.02f,28);m.Cylinder(steelPolished,new Vector3(tx,FFL+.02f,tz),.04f,1.0f,16);
            Oval(marbleWhite,new Vector3(tx,FFL+1.02f,tz),.45f,.45f,.04f);
            Col(tx-.45f,tx+.45f,FFL,FFL+1.06f,tz-.45f,tz+.45f,"stolik");
            for(int c=0;c<3;c++){float a=c*120f*Mathf.Deg2Rad;var off=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Stool(tx+off.x*.85f,tz+off.z*.85f,.78f,off);}
        }
        BowlingGame(props);
    }
    static float hz0()=>FoulZ+LaneLen-2.6f;

    // the played lanes: 240 pins with real mass on their spots, the game ball, house balls on the returns, the 24 live
    // score screens, and BraxChampionsKregle with the lane geometry (world metres)
    static void BowlingGame(Transform props)
    {
        var game=Group(root,"E1 | KRĘGLE gra (BraxChampionsKregle)");
        // one pin (two meshes: white lacquer, red neck stripes) shared by all 240
        var keep=m;m=new BraxChampionsMeshBatch();
        Pin(Vector3.zero,1f,16);
        var tpl=m.Build(root,"E1 pin template",Folder,false);m=keep;
        var pinMeshes=new List<(Mesh mesh,Material mat)>();
        foreach(var f in tpl.GetComponentsInChildren<MeshFilter>(true))pinMeshes.Add((f.sharedMesh,f.GetComponent<MeshRenderer>().sharedMaterial));
        UnityEngine.Object.DestroyImmediate(tpl.gameObject);
        var pins=new Transform[Lanes*10];float hz=FoulZ+LaneLen;
        for(int l=0;l<Lanes;l++)
        {
            float xc=LaneX(l);int k=0;
            for(int r=0;r<4;r++)for(int i=0;i<=r;i++,k++)
            {
                var pg=new GameObject("kręgiel tor "+(l+1)+" #"+(k+1));pg.transform.SetParent(game,false);pg.layer=8;
                pg.transform.localPosition=new Vector3(xc+(i-r*.5f)*PinSpacing,FFL,hz+r*PinRow)*K;
                foreach(var (mesh,mat) in pinMeshes)
                {
                    var c=new GameObject("pin part");c.transform.SetParent(pg.transform,false);c.layer=8;
                    c.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=c.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;
                    mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                var cap=pg.AddComponent<CapsuleCollider>();cap.center=new Vector3(0,.1905f*K,0);cap.height=.381f*K;cap.radius=.0605f*K;cap.direction=1;
                var rb=pg.AddComponent<Rigidbody>();rb.mass=1.55f;rb.isKinematic=true;
                rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.Continuous;
                foreach(var t in pg.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
                pins[l*10+k]=pg.transform;
            }
        }
        // house balls on every return (4), the game ball (physics) on the first one
        for(int p=0;p<12;p++)for(int i=0;i<4;i++)
            Ball(props,"kula na zwrotnicy",houseBalls[(p+i)%houseBalls.Length],new Vector3(PairX(p)+(i%2==0?-.13f:.13f),ReturnTop+BallR,-19.0f+(i/2)*.26f),BallR,0f);
        var ball=Ball(game,"kula do gry",houseBalls[0],new Vector3(PairX(0),ReturnTop+BallR,-17.85f),BallR,7.3f);
        // live score screens: over every approach (faced to the settees) and on the scorer console of its pair
        var screens=new Renderer[Lanes];var consoles=new Renderer[Lanes];
        for(int l=0;l<Lanes;l++)
        {
            screens[l]=Screen(game,"ekran wyniku tor "+(l+1),scrLane[l],new Vector3(LaneX(l),3.5f,-16.962f),1.32f,.78f,0f);
            consoles[l]=Screen(game,"konsola wyniku tor "+(l+1),scrLane[l],new Vector3(PairX(l/2)+(l%2==0?-.235f:.235f),FFL+.74f,-19.762f),.42f,.134f,0f);
        }
        var kg=game.gameObject.AddComponent<BraxChampionsKregle>();
        kg.faul=new Vector3[Lanes];kg.zwrotnica=new Vector3[Lanes];kg.kulaNaZwrotnicy=new Vector3[Lanes];
        for(int l=0;l<Lanes;l++)
        {
            kg.faul[l]=W(LaneX(l),FFL,FoulZ);
            kg.zwrotnica[l]=W(PairX(l/2),ReturnTop,ReturnZ);
            kg.kulaNaZwrotnicy[l]=W(PairX(l/2),ReturnTop+BallR,-17.85f);
        }
        kg.kregle=pins;kg.kula=ball;kg.ekrany=screens;kg.konsole=consoles;Pins=Lanes*10;
        kg.rozbieg=(FoulZ-ApproachZ0)*K;kg.glowa=LaneLen*K;kg.koniecPola=(LaneEnd-.3f-FoulZ)*K;
        kg.polowaToru=.5335f*K;kg.rynna=.2415f*K;kg.promien=BallR*K;kg.glebRynny=.06f*K;kg.srodekCiezkosci=.135f*K;
        kg.predkosc=8.0f*K;
    }

    // ================================================================ BILLIARDS + BAR + LOUNGE (x 10..59.7) - played (BraxChampionsBilard)
    // 9 ft tables to the WPA sizes: playing field 2.54 x 1.27 m, cushions 5 cm, rails 13 cm, cloth 79 cm over the floor,
    // balls 57.2 mm; corner pocket mouths 11.3 cm, side 13 cm
    public const float PoolHX=.635f,PoolHZ=1.27f,PoolCushion=.05f,PoolRail=.13f,PoolCloth=.79f,PoolBall=.0286f;
    static readonly Vector2[] PoolPockets={new Vector2(-PoolHX-.03f,-PoolHZ-.03f),new Vector2(PoolHX+.03f,-PoolHZ-.03f),new Vector2(-PoolHX-.055f,0f),
                                           new Vector2(PoolHX+.055f,0f),new Vector2(-PoolHX-.03f,PoolHZ+.03f),new Vector2(PoolHX+.03f,PoolHZ+.03f)};
    static readonly float[] PoolPocketR={.070f,.070f,.068f,.068f,.070f,.070f};
    static void Billiards()
    {
        float x0=10.15f,x1=HW-.3f,z0=-HD+.3f;
        B(carpetGraphite,x0,x1,SlabTop,FFL,-15.1f,14.9f);
        B(hallFloor,x0,x1,SlabTop,FFL,z0,-15.1f);                                                         // bar / lounge: black marble
        B(blackMatte,x0,x1,Ceiling,Ceiling+.1f,z0,14.9f);
        var game=Group(root,"E1 | BILARD gra (BraxChampionsBilard)");
        var centres=new List<Vector3>();var balls=new List<Rigidbody>();var cushions=new List<Collider>();
        float cloth=FFL+PoolCloth,shade=cloth+.98f;
        int t=0;
        for(int i=0;i<6;i++)for(int j=0;j<3;j++,t++)
        {
            float cx=15.8f+i*7.6f,cz=-8f+j*8f;
            Table(game,t,cx,cz,cushions,balls);
            centres.Add(W(cx,cloth,cz));
            // pendant over the table: black bar on two rods, three black shades with stainless rims and warm diffusers ~1 m
            // over the cloth
            RB(black,cx-.07f,cx+.07f,shade+.27f,shade+.39f,cz-1.30f,cz+1.30f,.035f,false,3);
            for(int k=-1;k<=1;k++)
            {
                var lc=new Vector3(cx,shade,cz+k*.85f);
                m.Cylinder(black,lc,.24f,.30f,40,Quaternion.identity,true,false,.10f);
                Arc(chrome,lc,.232f,.254f,0f,Mathf.PI*2f,shade-.015f,shade+.015f,40,false);
                m.Cylinder(lampWarm,lc+Vector3.down*.010f,.233f,.012f,40,Quaternion.identity,false,true);
            }
            foreach(float s in new[]{-1f,1f})m.Cylinder(steelPolished,new Vector3(cx,shade+.39f,cz+s*1.0f),.012f,Ceiling-shade-.39f,10);
        }
        // the cue that the player plays with (shown behind the cue ball while aiming): maple shaft, white ferrule,
        // blue chalked tip, stainless joint, black lacquered butt with a rubber cap - tip at the origin, the butt along -z
        var keep=m;m=new BraxChampionsMeshBatch();
        var back=Quaternion.Euler(-90f,0,0);
        m.Cylinder(S("cue_tip_chalk",new Color(.12f,.36f,.70f),.9f,0f),Vector3.zero,.0065f,.012f,14,back,true,true);
        m.Cylinder(porcelain,new Vector3(0,0,-.012f),.0065f,.025f,14,back,false,false);
        m.Cylinder(cue,new Vector3(0,0,-.037f),.0065f,.72f,14,back,false,false,.0098f);
        m.Cylinder(chrome,new Vector3(0,0,-.757f),.0105f,.022f,16,back,false,false);
        m.Cylinder(lacquer,new Vector3(0,0,-.779f),.0105f,.62f,16,back,false,false,.0145f);
        m.Cylinder(rubber,new Vector3(0,0,-1.399f),.0145f,.07f,16,back,false,true);
        var kij=Group(game,"kij bilardowy");kij.gameObject.layer=8;
        m.Build(kij,"cue",Folder,false);m=keep;
        foreach(var tr in kij.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(tr.gameObject,0);
        var bil=game.gameObject.AddComponent<BraxChampionsBilard>();
        bil.stoly=centres.ToArray();bil.bile=balls.ToArray();bil.bandy=cushions.ToArray();bil.kij=kij;
        bil.polX=PoolHX*K;bil.polZ=PoolHZ*K;bil.r=PoolBall*K;bil.promienLuzy=.069f*K;
        bil.luzy=new Vector3[PoolPockets.Length];for(int i=0;i<PoolPockets.Length;i++)bil.luzy[i]=new Vector3(PoolPockets[i].x,0,PoolPockets[i].y)*K;
        bil.sila=2.8f*K;bil.silaMocna=5.2f*K;bil.zasieg=2.1f*K;
        // cue racks on the east wall: walnut board with clips, a bottom rail, eight maple cues each standing in it
        for(int k=0;k<6;k++)
        {
            float rz=-12f+k*4.5f;
            RB(lacquer,x1-.10f,x1,FFL+.95f,FFL+1.60f,rz,rz+1.4f,.02f,true,3);
            RB(lacquer,x1-.25f,x1,FFL,FFL+.07f,rz,rz+1.4f,.02f,false,3);
            for(int c=0;c<8;c++)
            {
                float cz=rz+.12f+c*.165f,cx=x1-.16f;
                m.Cylinder(lacquer,new Vector3(cx,FFL+.02f,cz),.0145f,.62f,12,Quaternion.identity,false,false,.0105f);
                m.Cylinder(chrome,new Vector3(cx,FFL+.64f,cz),.0105f,.022f,12,Quaternion.identity,false,false);
                m.Cylinder(cue,new Vector3(cx,FFL+.662f,cz),.0098f,.72f,12,Quaternion.identity,true,false,.0065f);
                RB(chrome,cx-.03f,x1-.10f,FFL+1.40f,FFL+1.43f,cz-.022f,cz+.022f,.008f,false,2);           // clip
            }
        }
        for(float z=-14f;z<14f;z+=3.5f)if(z<-12.1f||Mathf.Abs(z-10.5f)>1.6f)B(redLed,x1-.012f,x1,.8f,Ceiling-.4f,z-.03f,z+.03f);
        // back bar: plaster wall, three stainless shelves with lit bottles, walnut back counter
        B(blackMatte,20f,50f,FFL,Ceiling,-15.2f,-15.0f,true);
        for(int s=0;s<3;s++)
        {
            float y=FFL+1.12f+s*.55f;RB(chrome,21f,49f,y,y+.04f,-15.55f,-15.2f,.012f,false,2);B(redLed,21f,49f,y+.04f,y+.052f,-15.212f,-15.2f);
            for(float x=21.3f;x<48.8f;x+=.32f)
            {
                int c=((int)(x*7f))%4;
                m.Cylinder(bottles[c],new Vector3(x,y+.04f,-15.38f),.04f,.24f,12,Quaternion.identity,false,true);
                m.Cylinder(bottles[c],new Vector3(x,y+.28f,-15.38f),.04f,.04f,12,Quaternion.identity,false,false,.014f);
                m.Cylinder(bottles[c],new Vector3(x,y+.32f,-15.38f),.014f,.06f,10,Quaternion.identity,true,false);
            }
        }
        RB(lacquer,21f,49f,FFL,FFL+.92f,-15.75f,-15.2f,.03f,true);
        // bar counter 1.06 m: walnut body with half-round ends, fluted front, stainless kick plate, white marble top with an
        // overhang on the guests' side
        float bz0=-18.3f,bz1=-17.6f,bzm=(bz0+bz1)*.5f,br=(bz1-bz0)*.5f,bt=FFL+1.01f;
        B(lacquer,22f,48f,FFL,bt,bz0,bz1,true);
        HalfRound(lacquer,new Vector3(22f,0,bzm),br,Vector3.left,FFL,bt,20);HalfRound(lacquer,new Vector3(48f,0,bzm),br,Vector3.right,FFL,bt,20);
        for(float x=22.1f;x<47.95f;x+=.16f)RB(lacquer,x,x+.08f,FFL+.14f,bt-.06f,bz0-.03f,bz0+.01f,.012f,false,2);
        RB(chrome,22f,48f,FFL,FFL+.12f,bz0-.025f,bz0+.01f,.01f,false,2);
        B(marbleWhite,22f,48f,bt,bt+.05f,bz0-.25f,bz1+.05f);
        HalfRound(marbleWhite,new Vector3(22f,0,bzm-.10f),br+.15f,Vector3.left,bt,bt+.05f,24);HalfRound(marbleWhite,new Vector3(48f,0,bzm-.10f),br+.15f,Vector3.right,bt,bt+.05f,24);
        for(float x=23f;x<=47.01f;x+=1.0f)Stool(x,bz0-.65f,.78f,Vector3.back);
        // lounge by the glass to the sea: six groups of two black leather sofas, oval marble tables on stainless pedestals
        for(int k=0;k<6;k++)
        {
            float cx=14.5f+k*7.6f,cz=-22.2f;
            Sofa(charcoal,new Vector3(cx,0,cz-1.36f),Vector3.right,Vector3.forward,3.0f,3);
            Sofa(charcoal,new Vector3(cx,0,cz+2.36f),Vector3.right,Vector3.back,3.0f,3);
            m.Cylinder(chrome,new Vector3(cx,FFL,cz+.5f),.30f,.02f,32);m.Cylinder(chrome,new Vector3(cx,FFL+.02f,cz+.5f),.06f,.38f,20);
            Oval(marbleWhite,new Vector3(cx,FFL+.40f,cz+.5f),.80f,.42f,.04f);
            Col(cx-.8f,cx+.8f,FFL,FFL+.44f,cz+.08f,cz+.92f,"stolik");
        }
    }

    // a 9 ft table: turned walnut legs on stainless feet, rounded walnut body, the cloth bed, cloth-covered cushions between
    // the six pockets, walnut rails, pocket castings (black leather, stainless rim round the hole) and mother-of-pearl
    // sights; colliders: the cloth bed, six bouncy cushions, the rail frame; 16 numbered balls (cue ball on the head spot)
    static void Table(Transform game,int index,float cx,float cz,List<Collider> cushions,List<Rigidbody> balls)
    {
        float fx=PoolHX,fz=PoolHZ,c=PoolCushion,hx=fx+c+PoolRail,hz=fz+c+PoolRail,top=FFL+PoolCloth,railTop=top+.05f,cushTop=top+.044f,castTop=railTop+.008f;
        const float jawC=.08f,jawS=.065f,railC=.03f,railS=.09f;      // cushion ends (corner / side), rail ends
        var g=Group(game,"stół bilardowy "+(index+1));
        // six turned legs: stainless foot cup, bulb, taper, collar, column into the body
        foreach(float s in new[]{-1f,1f})foreach(float t in new[]{-1f,0f,1f})
        {
            var b=new Vector3(cx+s*(hx-.24f),FFL,cz+t*(hz-.34f));
            m.Cylinder(chrome,b,.085f,.035f,24,Quaternion.identity,true,false,.075f);
            m.Cylinder(lacquer,b+Vector3.up*.035f,.075f,.08f,24,Quaternion.identity,false,false,.105f);
            m.Cylinder(lacquer,b+Vector3.up*.115f,.105f,.06f,24,Quaternion.identity,false,false,.105f);
            m.Cylinder(lacquer,b+Vector3.up*.175f,.105f,.14f,24,Quaternion.identity,false,false,.07f);
            m.Cylinder(lacquer,b+Vector3.up*.315f,.07f,.03f,24,Quaternion.identity,false,false,.085f);
            m.Cylinder(lacquer,b+Vector3.up*.345f,.085f,top-.20f-(FFL+.345f)+.02f,24,Quaternion.identity,false,false,.085f);
        }
        // body (apron) under the bed, the cloth bed over the field and the cushions
        RB(lacquer,cx-hx+.02f,cx+hx-.02f,top-.20f,top-.03f,cz-hz+.02f,cz+hz-.02f,.03f);
        B(felt,cx-fx-c,cx+fx+c,top-.03f,top,cz-fz-c,cz+fz+c);
        foreach(float s in new[]{-1f,1f})
        {
            // cushions (cloth-covered rubber, rounded nose): two on each long side, one on each short side
            foreach(var (z0,z1) in new[]{(-fz+jawC,-jawS),(jawS,fz-jawC)})RB(felt,s<0?cx-fx-c:cx+fx,s<0?cx-fx:cx+fx+c,top-.005f,cushTop,cz+z0,cz+z1,.016f,false,3);
            RB(felt,cx-fx+jawC,cx+fx-jawC,top-.005f,cushTop,s<0?cz-fz-c:cz+fz,s<0?cz-fz:cz+fz+c,.016f,false,3);
            // walnut rails between the castings
            foreach(var (z0,z1) in new[]{(-fz+railC,-railS),(railS,fz-railC)})RB(lacquer,s<0?cx-hx:cx+fx+c,s<0?cx-fx-c:cx+hx,top-.03f,railTop,cz+z0,cz+z1,.025f,false,3);
            RB(lacquer,cx-fx+railC,cx+fx-railC,top-.03f,railTop,s<0?cz-hz:cz+fz+c,s<0?cz-fz-c:cz+hz,.025f,false,3);
            // sights: mother-of-pearl dots on the rail tops (6 on a long rail, 3 on a short rail)
            for(int k=1;k<=7;k++)if(k!=4)m.Cylinder(porcelain,new Vector3(cx+s*(fx+c+PoolRail*.5f),railTop-.002f,cz-fz+k*(2*fz)/8f),.008f,.008f,12);
            for(int k=1;k<=3;k++)m.Cylinder(porcelain,new Vector3(cx-fx+k*(2*fx)/4f,railTop-.002f,cz+s*(fz+c+PoolRail*.5f)),.008f,.008f,12);
        }
        // pocket castings: the corner of the rail frame (cut toward the field along the mouth) and the middle of each long
        // rail, the hole cut out; the pocket floor a black disc 6 mm over the cloth
        for(int i=0;i<PoolPockets.Length;i++)
        {
            var P=PoolPockets[i];float R=PoolPocketR[i],sx=Mathf.Sign(P.x),sz=Mathf.Abs(P.y)<.01f?0f:Mathf.Sign(P.y);
            Vector2[] poly;
            if(sz==0f)poly=new[]{new Vector2(sx*(fx+c),-railS),new Vector2(sx*hx,-railS),new Vector2(sx*hx,railS),new Vector2(sx*(fx+c),railS)};
            else
            {
                var pts=new List<Vector2>{new Vector2(sx*(fx+c),sz*(fz-railC)),new Vector2(sx*hx,sz*(fz-railC))};
                for(int k=0;k<=6;k++){float a=k*Mathf.PI*.5f/6f;pts.Add(new Vector2(sx*(hx-.06f+Mathf.Cos(a)*.06f),sz*(hz-.06f+Mathf.Sin(a)*.06f)));}
                pts.Add(new Vector2(sx*(fx-railC),sz*hz));pts.Add(new Vector2(sx*(fx-railC),sz*(fz+c)));
                poly=pts.ToArray();
            }
            PocketCasting(cx,cz,P,R,poly,top-.03f,castTop,top+.006f);
            m.Cylinder(rubber,new Vector3(cx+P.x,top-.14f,cz+P.y),R+.002f,.146f,40,Quaternion.identity,true,true);
        }
        // colliders: the cloth bed, the cushions (bouncy rubber), the rail frame (also keeps the player out)
        var kol=Group(g,"kolizje stołu");
        Collider Box(float x0,float x1,float y0,float y1,float z0,float z1,string name)
        {
            var o=new GameObject(name);o.transform.SetParent(kol,false);
            o.transform.position=W(cx+(x0+x1)*.5f,(y0+y1)*.5f,cz+(z0+z1)*.5f);
            var bc=o.AddComponent<BoxCollider>();bc.size=new Vector3(x1-x0,y1-y0,z1-z0)*K;Colliders++;return bc;
        }
        Box(-fx-c,fx+c,top-.10f,top,-fz-c,fz+c,"sukno");
        foreach(float s in new[]{-1f,1f})
        {
            foreach(var (z0,z1) in new[]{(-fz+jawC,-jawS),(jawS,fz-jawC)})cushions.Add(Box(s<0?-fx-c:fx,s<0?-fx:fx+c,top,railTop,z0,z1,"banda"));
            cushions.Add(Box(-fx+jawC,fx-jawC,top,railTop,s<0?-fz-c:fz,s<0?-fz:fz+c,"banda"));
            cushions.Add(Box(s<0?-hx:fx+c,s<0?-fx-c:hx,FFL,railTop,-hz,hz,"rama"));
            cushions.Add(Box(-hx,hx,FFL,railTop,s<0?-hz:fz+c,s<0?-fz-c:hz,"rama"));
        }
        // balls: the cue ball on the head spot, the 15 racked on the foot spot (the 8 in the middle, a stripe and a solid
        // in the back corners), apex toward the cue ball
        var bg=Group(g,"bile");
        balls.Add(Ball(bg,"bila biała",poolBalls[0],new Vector3(cx,top+PoolBall,cz-fz*.5f),PoolBall,.17f));
        int[] order={1,9,2,10,8,3,11,7,14,4,5,13,15,6,12};
        var byNum=new Rigidbody[16];int q=0;
        for(int r=0;r<5;r++)for(int i=0;i<=r;i++,q++)
        {
            int n=order[q];float d=PoolBall*2f+.0005f;
            byNum[n]=Ball(bg,"bila "+n,poolBalls[n],new Vector3(cx+(i-r*.5f)*d,top+PoolBall,cz+fz*.5f+r*d*.866f),PoolBall,.17f);
        }
        for(int n=1;n<=15;n++)balls.Add(byNum[n]);
        BilliardTables++;
    }

    // ================================================================ BACK BAND (z 15..24.7): rooms, doors, toilets, games/VR
    static void BackRooms()
    {
        float z0=15.1f,z1=HD-.3f,top=4.5f;
        // west: corridor wall with doors, partitions, ceilings
        WallX(blackMatte,15f,-HW+.3f,-.15f,FFL,Ceiling,.2f,(-55.6f,-54.4f,2.4f),(-40.6f,-39.4f,2.4f),(-26.6f,-25.4f,2.4f),(-19.6f,-18.4f,2.4f),(-11.6f,-10.4f,2.4f),(-4.6f,-3.4f,2.4f));
        foreach(var (x,lab,id) in new[]{(-55f,"E1 plant room west","E1_TECH_W"),(-40f,"E1 store / magazyn","E1_STORE"),(-26f,"E1 cloakroom M","E1_SZATNIA_M"),
                                       (-19f,"E1 toilets M","E1_WC_M"),(-11f,"E1 toilets K","E1_WC_K"),(-4f,"E1 cloakroom K","E1_SZATNIA_K")})
            Door(id,lab,x,FFL,15f,0f,1.2f,2.4f-FFL,false);
        foreach(float x in new[]{-50f,-30f,-22.5f,-15f,-7.5f})B(blackMatte,x-.1f,x+.1f,FFL,top,z0,z1,true);
        B(garageWhite,-HW+.3f,-.15f,top,top+.1f,z0,z1);
        // floors
        B(techFloor,-HW+.3f,-30.1f,SlabTop,FFL,z0,z1);
        B(carpetGraphite,-30.1f,-22.6f,SlabTop,FFL,z0,z1);B(carpetGraphite,-7.4f,-.15f,SlabTop,FFL,z0,z1);
        B(whiteTile,-22.4f,-7.6f,SlabTop,FFL,z0,z1);
        // room signs over the doors (3D)
        foreach(var (x,t) in new[]{(-55f,"TECH."),(-40f,"MAGAZYN"),(-26f,"SZATNIA M"),(-19f,"WC M"),(-11f,"WC K"),(-4f,"SZATNIA K")})
            BraxChampionsLetters3D.Text(m,chrome,black,t,new Vector3(x,2.75f,14.91f),180f,.20f,.02f,.14f,2.6f);
        // plant / store: units, shelves
        foreach(var (a,b) in new[]{(-59.4f,-57.6f),(-52.4f,-50.6f)})B(hvac,a,b,FFL,2.2f,16.5f,23.5f,true);   // path x -57..-53 between the two doors
        for(int i=0;i<2;i++){float x=-48.5f+i*4.6f;for(int lv=0;lv<4;lv++)B(BraxChampionsHarbour.Steel,x,x+3.6f,.4f+lv*.7f,.44f+lv*.7f,17.5f,24.0f);
            foreach(float px in new[]{x,x+3.56f})foreach(float pz in new[]{17.5f,23.96f})B(BraxChampionsHarbour.Steel,px,px+.04f,FFL,2.6f,pz,pz+.04f);Col(x,x+3.6f,FFL,2.6f,17.5f,24f,"regał");}
        // cloakrooms: steel lockers (rounded doors, stainless number plates), leather benches
        var locker=BraxChampionsFoundation.LibraryMetal("e1_locker_graphite",new Color(.16f,.17f,.19f));
        foreach(var (a,b) in new[]{(-29.9f,-22.7f),(-7.3f,-.25f)})
        {
            B(locker,a,b,FFL,2.0f,23.9f,24.4f);
            for(float x=a+.02f;x+.46f<b;x+=.5f)for(int r=0;r<2;r++)
            {
                float y0=FFL+.12f+r*.92f;
                RB(locker,x,x+.46f,y0,y0+.86f,23.86f,23.9f,.012f,false,2);
                RB(chrome,x+.36f,x+.42f,y0+.40f,y0+.46f,23.845f,23.862f,.006f,false,2);
            }
            Col(a,b,FFL,2f,23.86f,24.4f,"szafki");
            Bench(new Vector3((a+b)*.5f,0,19.5f),Vector3.right,b-a-2f,.46f);
        }
        // toilets: cubicles with doors that open (BraxCityDoor), wall-hung pans on the tiled duct, basins on a marble
        // counter under a mirror, urinals with screens in M
        foreach(var (a,b,men,tag) in new[]{(-22.4f,-15.1f,true,"M"),(-14.9f,-7.6f,false,"K")})
        {
            const int n=5;float w=(b-a-.2f)/n,front=23.0f;
            B(whiteTile,a,b,FFL,FFL+1.12f,24.45f,z1);                                                       // service duct
            for(int i=0;i<=n;i++){float px=a+.1f+i*w;RB(black,px-.02f,px+.02f,FFL+.15f,FFL+2.0f,front,z1,.01f,true,2);}
            for(int i=0;i<n;i++)
            {
                float cx=a+.1f+(i+.5f)*w;
                RB(black,cx-w*.5f+.02f,cx-.36f,FFL+.15f,FFL+2.0f,front-.02f,front+.02f,.008f,true,2);
                RB(black,cx+.36f,cx+w*.5f-.02f,FFL+.15f,FFL+2.0f,front-.02f,front+.02f,.008f,true,2);
                Door("E1_WC_"+tag+"_"+(i+1),"E1 WC "+tag+" kabina "+(i+1),cx,FFL+.15f,front,0f,.70f,1.85f,false);
                Toilet(cx,24.45f);
                m.Cylinder(steelPolished,new Vector3(cx+w*.5f-.02f,FFL+.75f,24.05f),.055f,.12f,20,Quaternion.Euler(0,0,90),true,true);   // paper roll holder
            }
            // basin counter on the west wall, three vessel basins with mixers, mirror with a steel frame
            float wx=a,cz0=16.0f,cz1=20.4f,ct=FFL+.84f;
            RB(lacquer,wx,wx+.50f,FFL+.42f,ct,cz0,cz1,.02f,true,3);
            RB(marbleWhite,wx,wx+.56f,ct,ct+.04f,cz0-.03f,cz1+.03f,.012f,false,3);
            for(int k=0;k<3;k++){float bz=cz0+.75f+k*1.45f;Basin(new Vector3(wx+.30f,ct+.04f,bz),.21f,.15f,.13f);Tap(new Vector3(wx+.30f,ct+.04f,bz),Vector3.left,.24f);}
            RB(steelPolished,wx,wx+.03f,FFL+1.08f,FFL+2.12f,cz0,cz1,.008f,false,2);
            B(mirror,wx+.03f,wx+.04f,FFL+1.12f,FFL+2.08f,cz0+.04f,cz1-.04f);
            if(men)for(int k=0;k<3;k++)
            {
                float uz=16.6f+k*.9f;Urinal(b,uz,-1f);
                RB(black,b-.55f,b,FFL+.55f,FFL+1.65f,uz+.43f,uz+.47f,.008f,true,2);
            }
        }
        // east band: games / VR open to the billiards (3 openings), offices, plant
        WallX(blackMatte,15f,10.15f,HW-.3f,FFL,Ceiling,.2f,(13f,19f,3.0f),(22f,28f,3.0f),(31f,37f,3.0f),(43.4f,44.6f,2.4f),(53.9f,55.1f,2.4f));
        Door("E1_OFFICES","E1 offices / biura",44f,FFL,15f,0f,1.2f,2.4f-FFL,false);
        Door("E1_TECH_E","E1 plant room east",54.5f,FFL,15f,0f,1.2f,2.4f-FFL,false);
        BraxChampionsLetters3D.Text(m,chrome,black,"STREFA GIER • VR",new Vector3(25f,3.4f,14.91f),180f,.38f,.03f,.14f,9f);
        foreach(var (x,t) in new[]{(44f,"BIURA"),(54.5f,"TECH.")})BraxChampionsLetters3D.Text(m,chrome,black,t,new Vector3(x,2.75f,14.91f),180f,.20f,.02f,.14f,2.6f);
        foreach(float x in new[]{40f,50f})B(blackMatte,x-.1f,x+.1f,FFL,top,z0,z1,true);
        B(garageWhite,40f,HW-.3f,top,top+.1f,z0,z1);
        B(blackMatte,10.15f,40f,Ceiling,Ceiling+.1f,z0,z1);
        B(carpet,10.15f,40f,SlabTop,FFL,z0,z1);B(carpetGraphite,40.1f,49.9f,SlabTop,FFL,z0,z1);B(techFloor,50.1f,HW-.3f,SlabTop,FFL,z0,z1);
        // games: ten upright arcade cabinets along the back wall, two air hockey tables, four VR pods, two claw machines
        var gry=Group(root,"E1 strefa gier");
        for(int i=0;i<10;i++)Arcade(11.6f+i*1.1f,23.88f,i);
        AirHockey(gry,27f,18.2f,0);AirHockey(gry,32.5f,18.2f,1);
        for(int i=0;i<4;i++)VRPod(26.5f+i*3.2f,22.4f);
        ClawMachine(36.4f,17.55f,0);ClawMachine(37.8f,17.55f,1);
        // offices: two desks with monitors (the live status board) and office chairs, a walnut cabinet
        foreach(float z in new[]{18f,22f}){Desk(41f,45f,z);Chair(43f,z-.75f,Vector3.forward);}
        RB(lacquer,47.6f,49.7f,FFL,FFL+1.2f,15.6f,16.1f,.02f,true,3);
        for(int i=0;i<2;i++)B(hvac,51f+i*3.6f,53.6f+i*3.6f,FFL,2.4f,19.5f,23.8f,true);
    }

    // ================================================================ B1 GARAGE
    static readonly float[] RowZ0={-HD+.3f,-13.2f,-8.25f,3.3f,8.25f,19.7f},RowZ1={-19.7f,-8.25f,-3.3f,8.25f,13.2f,HD-.3f};
    static void Garage()
    {
        float y=B1,cy=SlabBottom;
        // columns (white, black base 2 cm proud), beams on the column lines
        for(int k=-5;k<=5;k++)foreach(float z in new[]{-8.25f,8.25f})
        {
            float x=k*10f;
            B(garageWhite,x-.25f,x+.25f,y,cy-.5f,z-.25f,z+.25f,true);
            B(black,x-.27f,x+.27f,y,y+.6f,z-.27f,z+.27f);
            B(lineYellow,x-.271f,x+.271f,y+.6f,y+.75f,z-.271f,z+.271f);
        }
        foreach(float z in new[]{-8.25f,8.25f})B(garageWhite,-HW+.3f,HW-.3f,cy-.5f,cy,z-.25f,z+.25f);
        // bays 3.1 m on the 10 m rhythm, 8 mm white lines, accessible bays by the lobby, numbers
        for(int r=0;r<6;r++)
        {
            float z0=RowZ0[r],z1=RowZ1[r];
            for(int k=-6;k<6;k++)
                for(int i=0;i<3;i++)
                {
                    float a=k*10f+.35f+i*3.1f,b=a+3.1f;
                    if(r==5&&(b>-3.2f&&a<10.2f))continue;                                              // lobby, stair, lift
                    if(r==5&&b>RampX0-.5f&&a<RampX1+.5f)continue;                                      // ramp landing
                    bool access=r==4&&k==-1;
                    float lz0=r==2||r==4?z0+.05f:z0,lz1=r==1||r==3?z1-.05f:z1;
                    B(access?lineBlue:lineWhite,a-.05f,a+.05f,y,y+.008f,lz0,lz1);
                    if(i==2||(r==5&&b+3.1f>RampX0-.5f&&b<RampX1+.5f))B(access?lineBlue:lineWhite,b-.05f,b+.05f,y,y+.008f,lz0,lz1);
                    if(access){B(lineBlue,a+.3f,b-.3f,y,y+.008f,(z0+z1)*.5f-.9f,(z0+z1)*.5f+.9f);}
                    Bays++;
                }
        }
        // aisle arrows and the walking line to the lobby
        foreach(var (x,z,d) in new[]{(-30f,-16.45f,1f),(20f,-16.45f,1f),(-30f,0f,-1f),(20f,0f,-1f),(-20f,16.45f,1f),(25f,16.45f,-1f)})
        {
            B(lineWhite,Mathf.Min(x-1.2f*d,x+.2f*d),Mathf.Max(x-1.2f*d,x+.2f*d),y,y+.008f,z-.12f,z+.12f);
            for(int i=0;i<6;i++){float t=i*.12f;B(lineWhite,x+(.2f+t)*d-.06f,x+(.2f+t)*d+.06f,y,y+.008f,z-.6f+t,z+.6f-t);}
        }
        // LED lines under the slab over the aisles
        foreach(float z in new[]{-16.45f,0f,16.45f})for(float x=-55f;x<56f;x+=5f){B(ledCool,x-.75f,x+.75f,cy-.10f,cy-.06f,z-.04f,z+.04f);B(BraxChampionsHarbour.Steel,x-.04f,x+.04f,cy-.06f,cy,z-.02f,z+.02f);}
        // "B1" on the walls, exit signs
        foreach(float x in new[]{-45f,-15f,25f})BraxChampionsLetters3D.Text(m,lineYellow,black,"B1",new Vector3(x,y+1.2f,-HD+.29f),0f,.9f,.04f,.14f,2f);
        // lobby: glass wall with a double door, white walls round the stair and the lift
        float lx0=-3f,lx1=LiftX1;
        B(garageWhite,lx0-.2f,lx0,y,cy,19.9f,HD-.3f,true);
        foreach(var (a,b) in new[]{(lx0,-2.65f),(-.35f,lx1)})
        {
            B(clearGlass,a,b,y,cy,19.88f,19.9f,true);
            for(float x=a;x<=b+.01f;x+=1.5f)B(black,x-.03f,x+.03f,y,cy,19.86f,19.92f);
        }
        B(black,-2.65f,-.35f,y+2.3f,cy,19.86f,19.92f);
        B(clearGlass,-2.65f,-.35f,y+2.36f,cy,19.88f,19.9f);
        Door("E1_B1_LOBBY","E1 B1 lift lobby",-1.5f,y,19.89f,180f,2.3f,2.3f,true);
        B(garageFloor,lx0,LiftX0,y,y+.02f,19.92f,HD-.3f);B(garageFloor,LiftX0,lx1,y,y+.02f,19.92f,LiftZ0);   // lobby floor 2 cm (finish), not in the shaft
        BraxChampionsLetters3D.Text(m,lineWhite,black,"WINDA • SCHODY • HOL",new Vector3(-1.5f,y+2.65f,19.865f),180f,.18f,.02f,.14f,2.6f);
        // stair: 22 risers B1 -> HOL along the back wall, solid steps, steel handrail, glass to the lobby
        float rise=(FFL-B1)/Risers;
        for(int i=0;i<Risers-1;i++)
        {
            float x0=StairX0+i*StairRun,top=B1+(i+1)*rise;
            B(garageWhite,x0+.04f,x0+StairRun,y+.02f,top,StairZ0,HD-.3f);
            B(black,x0,x0+.04f,y+.02f,top,StairZ0,HD-.3f);                                       // black nosing (front of the step)
            Col(x0,x0+StairRun,y,top,StairZ0,HD-.3f,"stopień");
        }
        float sx1=StairX0+(Risers-1)*StairRun;
        m.Tube(BraxChampionsHarbour.Steel,new Vector3(StairX0,y+1.0f,StairZ0+.05f),new Vector3(sx1,FFL+1.0f,StairZ0+.05f),.025f);
        m.Tube(BraxChampionsHarbour.Steel,new Vector3(StairX0,y+1.0f,HD-.35f),new Vector3(sx1,FFL+1.0f,HD-.35f),.025f);
        // lift shaft (white in B1, black + stainless over the hall floor), front wall with the door opening at both stops
        float sx0=LiftX0,sxx=LiftX1,sz0=LiftZ0,shaftTop=FFL+3.4f;
        B(garageWhite,sx0,sx0+.2f,y,cy,sz0,HD-.3f,true);B(garageWhite,sxx-.2f,sxx,y,cy,sz0,HD-.3f,true);
        B(black,sx0,sx0+.2f,FFL,shaftTop,sz0,HD-.3f,true);B(black,sxx-.2f,sxx,FFL,shaftTop,sz0,HD-.3f,true);
        B(black,sx0,sxx,shaftTop,shaftTop+.2f,sz0,HD-.3f,true);
        float dx0=8.6f-LiftDoorHalf,dx1=8.6f+LiftDoorHalf;
        foreach(var (f0,f1,mat) in new[]{(y,cy,garageWhite),(Y0,shaftTop,black)})
        {
            B(mat,sx0+.2f,dx0,f0,f1,sz0,sz0+.2f,true);B(mat,dx1,sxx-.2f,f0,f1,sz0,sz0+.2f,true);
        }
        B(garageWhite,dx0,dx1,y+2.2f,cy,sz0,sz0+.2f,true);B(black,dx0,dx1,FFL+2.2f,shaftTop,sz0,sz0+.2f,true);
        B(garageWhite,dx0,dx1,cy,Y0,sz0,sz0+.2f);                                                    // between the stops (slab edge)
        foreach(float fy in new[]{y,FFL})
        {
            B(chrome,dx0-.08f,dx0,fy,fy+2.28f,sz0-.04f,sz0);B(chrome,dx1,dx1+.08f,fy,fy+2.28f,sz0-.04f,sz0);B(chrome,dx0-.08f,dx1+.08f,fy+2.2f,fy+2.28f,sz0-.04f,sz0);
            B(BraxChampionsHarbour.Steel,dx1+.2f,dx1+.32f,fy+1.0f,fy+1.3f,sz0-.03f,sz0);
            B(redLed,dx1+.235f,dx1+.285f,fy+1.12f,fy+1.17f,sz0-.04f,sz0-.03f);
        }
        B(redLed,sx0,sxx,shaftTop+.2f,shaftTop+.24f,sz0-.005f,sz0+.04f);
        BraxChampionsLetters3D.Text(m,chrome,black,"WINDA • PARKING B1",new Vector3(8.6f,shaftTop-.55f,sz0+.005f),180f,.20f,.02f,.14f,2.4f);
    }

    // ================================================================ LIFT (BraxWinda B1 <-> HOL)
    static void Lift()
    {
        float xc=8.6f,hw=1.05f,z0=LiftZ0+.3f,z1=HD-.35f;
        var landL=new Transform[2];var landR=new Transform[2];
        float[] stops={B1,FFL};
        for(int s=0;s<2;s++)
        {
            foreach(float side in new[]{-1f,1f})
            {
                var t=Group(root,"E1 lift landing door "+(side<0?"L":"R")+" | "+(s==0?"B1":"HOL"));
                t.localPosition=new Vector3(xc+side*LiftDoorHalf*.5f,stops[s],LiftZ0+.1f)*K;
                m.BoxMinMax(BraxChampionsHarbour.Steel,-LiftDoorHalf*.5f,LiftDoorHalf*.5f,0f,2.2f,-.015f,.015f);
                m.Build(t,"landing door",Folder,false);m=new BraxChampionsMeshBatch();
                BraxChampionsMeshBatch.Collider(t,"landing door",new Vector3(0,1.1f,0),new Vector3(LiftDoorHalf,2.2f,.05f));
                foreach(var c in t.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(c.gameObject,0);
                if(side<0)landL[s]=t;else landR[s]=t;
            }
        }
        var cab=Group(root,"E1 LIFT | cabin");
        m.BoxMinMax(hallFloor,xc-hw,xc+hw,-.12f,0f,z0,z1);
        m.BoxMinMax(BraxChampionsHarbour.Steel,xc-hw,xc+hw,0f,2.5f,z1,z1+.05f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(BraxChampionsHarbour.Steel,s>0?xc+hw:xc-hw-.05f,s>0?xc+hw+.05f:xc-hw,0f,2.5f,z0,z1);
        m.BoxMinMax(BraxChampionsHarbour.Steel,xc-hw-.05f,xc+hw+.05f,2.5f,2.58f,z0,z1+.05f);
        m.BoxMinMax(ledCool,xc-.5f,xc+.5f,2.47f,2.5f,z0+.4f,z1-.4f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(BraxChampionsHarbour.Steel,s>0?xc+LiftDoorHalf:xc-hw,s>0?xc+hw:xc-LiftDoorHalf,0f,2.5f,z0-.03f,z0);
        m.BoxMinMax(BraxChampionsHarbour.Steel,xc-hw,xc+hw,2.2f,2.5f,z0-.03f,z0);
        m.BoxMinMax(chrome,xc-hw+.05f,xc+hw-.05f,.9f,.94f,z1-.03f,z1);
        m.Build(cab,"cabin",Folder,false);m=new BraxChampionsMeshBatch();
        BraxChampionsMeshBatch.Collider(cab,"cabin floor",new Vector3(xc,-.06f,(z0+z1)*.5f),new Vector3(2*hw,.12f,z1-z0));
        foreach(float s in new[]{-1f,1f})BraxChampionsMeshBatch.Collider(cab,"cabin wall",new Vector3(s*hw+xc,1.25f,(z0+z1)*.5f),new Vector3(.05f,2.5f,z1-z0));
        BraxChampionsMeshBatch.Collider(cab,"cabin back",new Vector3(xc,1.25f,z1+.025f),new Vector3(2*hw,2.5f,.05f));
        var cabL=Group(cab,"cabin door L");var cabR=Group(cab,"cabin door R");
        m.BoxMinMax(BraxChampionsHarbour.Steel,xc-LiftDoorHalf,xc,0f,2.2f,z0-.06f,z0-.03f);m.Build(cabL,"cabin door L",Folder,false);m=new BraxChampionsMeshBatch();
        m.BoxMinMax(BraxChampionsHarbour.Steel,xc,xc+LiftDoorHalf,0f,2.2f,z0-.06f,z0-.03f);m.Build(cabR,"cabin door R",Folder,false);m=new BraxChampionsMeshBatch();
        cab.localPosition=new Vector3(0,FFL,0)*K;
        foreach(var t in cab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        var w=cab.gameObject.AddComponent<BraxWinda>();
        w.nazwa="E1 • WINDA PARKING";w.kabina=cab;w.kabinaL=cabL;w.kabinaP=cabR;
        w.pietraY=new[]{root.TransformPoint(new Vector3(0,B1,0)*K).y,root.TransformPoint(new Vector3(0,FFL,0)*K).y};
        w.pietraNazwy=new[]{"B1","HOL"};w.uzytkowe=new[]{true,true};
        w.wezwanie=new[]{root.TransformPoint(new Vector3(xc,B1+1.2f,LiftZ0-.9f)*K),root.TransformPoint(new Vector3(xc,FFL+1.2f,LiftZ0-.9f)*K)};
        w.przystanekL=landL;w.przystanekP=landR;
        var shaft=root.TransformPoint(new Vector3(xc,0,(z0+z1)*.5f)*K);
        w.szyb=new Vector2(shaft.x,shaft.z);w.wnetrze=new Vector2(hw*K,(z1-z0)*.5f*K);
        w.przesuw=LiftDoorHalf*.95f*K;w.start=1;
    }
}

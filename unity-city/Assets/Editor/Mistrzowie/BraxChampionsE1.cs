using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 19:18 (board "E1 CENTRUM ROZRYWKI - KRĘGLE•BILARD•STREFA ROZRYWKI", docs/zlecenia/plansze), 19:40 ("rób od
// podstaw E1 dokładnie, parking podziemny najlepiej"), 19:40 ("na taksówki przed E1"), 20:06 ("rób e1 dokładnie"). Built 1:1
// from the board in nominal metres (= real metres, x K once), on the plot by the promenade (x 530, z -13, 120 x 50 x 15 m):
//
// * FRONT to the promenade (south, road Z-70): ground band 0-5.4 m of graphite glass between black metal pilasters on the
//   10 m module, black metal panels above, vertical gold LED lines on every pilaster, gold canopy line and parapet line;
//   the portal over the hall (15 m) with red 3D "E1" and white "CENTRUM ROZRYWKI", a 6 m canopy with downlights and a red
//   carpet; the magenta bowling mural at the west end with giant 3D pins and a ball; KRĘGLE / BILARD / GRY / BAR / LOUNGE
//   3D signs with their pictograms over the east wing; main entrance (two glass double doors) + two side / emergency doors.
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
    static Material black,blackMatte,gold,goldLed,purpleLed,magenta,red,whiteLit,facadeGlass,clearGlass,concrete,pavers,carpetRed,
        hallFloor,carpet,carpetGraphite,maple,gutter,felt,lacquer,velvet,charcoal,screen,whiteTile,techFloor,garageFloor,garageWhite,
        lineWhite,lineYellow,lineBlue,ledCool,marbleWhite,roofMembrane,hvac,asphalt,walk,antiSlip,rampWall,pinWhite,pinRed,ballRed,
        ballBlue,ballGreen,ballBlueSolid,topiary,cue,mirror;
    static Material[] bottles;

    // ================================================================ materials
    static Material S(string id,Color c,float r,float met,Color? e=null,bool t=false)=>BraxChampionsHarbour.Surface("e1_"+id,c,r,met,e,t);
    static Material Inside(Material x,float a){BraxChampionsInteriorLight.Apply(x,a);return x;}
    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!BraxChampionsPromenadeFurniture.Teak)BraxChampionsPromenadeFurniture.Materials();
        black=S("black_metal_panel",new Color(.045f,.048f,.055f),.30f,.82f);                    // board: "Panel metalowy (czarny mat)"
        blackMatte=Inside(S("interior_black_matte",new Color(.055f,.055f,.062f),.72f,.05f),BraxChampionsInteriorLight.Walls);
        gold=S("gold_alucobond",new Color(.95f,.72f,.30f),.20f,.95f);                            // "Alucobond / metal (złoty)"
        goldLed=S("gold_led_line",new Color(1f,.82f,.46f),.4f,0f,new Color(1.10f,.78f,.36f));
        purpleLed=S("purple_led",new Color(.62f,.22f,.95f),.4f,0f,new Color(.80f,.22f,1.20f));
        magenta=S("mural_magenta_panel",new Color(.46f,.06f,.42f),.35f,.15f,new Color(.55f,.05f,.50f));
        red=S("red_composite_letters",new Color(.85f,.05f,.07f),.30f,.20f,new Color(.70f,.02f,.03f)); // "Panel kompozytowy (czerwony)"
        whiteLit=S("white_backlit_letters",new Color(.96f,.96f,.97f),.30f,0f,new Color(.75f,.75f,.78f));
        facadeGlass=S("facade_glass_graphite",new Color(.20f,.24f,.28f,.40f),.05f,0f,null,true); // "Szkło fasadowe (przyciemniane)"
        clearGlass=S("clear_glass",new Color(.80f,.88f,.92f,.20f),.05f,0f,null,true);
        concrete=BraxChampionsFoundation.SourceMaterial("e1_architectural_concrete","concrete_wall_003",2.4f,new Color(1.0f,1.12f,1.31f));      // the map is warm (206,184,157): tinted to neutral light grey
        pavers=BraxChampionsFoundation.SourceMaterial("e1_forecourt_pavers","concrete_pavement_02",1.8f,new Color(1.0f,1.09f,1.21f));    // neutral grey, no beige
        carpetRed=S("red_carpet",new Color(.58f,.03f,.05f),.95f,0f);
        hallFloor=Inside(BraxChampionsFoundation.LibraryMarble("e1_hall_black_marble",new Color(.92f,.92f,.95f),1.6f),.8f);
        carpet=Inside(S("bowling_carpet_cosmic",new Color(.08f,.06f,.14f),.95f,0f),.8f);
        carpetGraphite=Inside(S("billiard_carpet_graphite",new Color(.12f,.125f,.135f),.95f,0f),.8f);
        // pale silvered maple (oak_wood_planks alone is orange): the lanes read light, never brown
        maple=Inside(BraxChampionsFoundation.SourceMaterial("e1_lane_silvered_maple","oak_wood_planks",.9f,new Color(1.12f,1.52f,2.05f)),.85f);   // (162,117,84) -> light grey maple
        gutter=Inside(S("lane_gutter_black",new Color(.04f,.04f,.046f),.30f,.55f),.7f);
        felt=Inside(S("billiard_felt_green",new Color(.05f,.40f,.21f),.95f,0f),.85f);
        lacquer=Inside(S("black_lacquer",new Color(.03f,.03f,.035f),.18f,.10f),.7f);
        velvet=Inside(S("settee_plum_velvet",new Color(.30f,.08f,.36f),.85f,0f),.75f);
        charcoal=Inside(S("lounge_charcoal_fabric",new Color(.14f,.145f,.16f),.88f,0f),.75f);
        screen=S("score_screen",new Color(.05f,.12f,.25f),.2f,0f,new Color(.18f,.48f,1.00f));
        whiteTile=Inside(BraxChampionsFoundation.SourceMaterial("e1_wc_white_tiles","long_white_tiles",1.2f,new Color(1.0f,1.035f,1.13f)),BraxChampionsInteriorLight.Walls);
        techFloor=Inside(BraxChampionsFoundation.SourceMaterial("e1_tech_concrete_floor","concrete_floor_02",3f,new Color(1.0f,1.09f,1.32f)),.8f);       // (120,110,91) -> neutral grey
        garageFloor=Inside(BraxChampionsFoundation.SourceMaterial("e1_garage_floor","garage_floor",3f,new Color(1.0f,1.14f,1.43f)),.85f);              // (124,109,87) -> neutral grey
        // clean cool white (white_stucco_02 neutralised, as the hotel garage): no beige under the LED lines
        garageWhite=Inside(BraxChampionsFoundation.SourceMaterial("e1_garage_white","white_stucco_02",2f,new Color(1.10f,1.14f,1.20f)),BraxChampionsInteriorLight.Ceilings);
        lineWhite=Inside(S("paint_white",new Color(.93f,.93f,.92f),.6f,0f),.85f);
        lineYellow=Inside(S("paint_yellow",new Color(.95f,.76f,.08f),.6f,0f),.85f);
        lineBlue=Inside(S("paint_accessible_blue",new Color(.10f,.32f,.72f),.6f,0f),.85f);
        ledCool=S("led_cool",new Color(1f,1f,1f),.3f,0f,new Color(1.15f,1.15f,1.20f));
        marbleWhite=Inside(BraxChampionsFoundation.LibraryMarbleWhite("e1_white_marble",new Color(.96f,.96f,.97f),2.0f),.8f);
        roofMembrane=S("roof_membrane_grey",new Color(.43f,.44f,.46f),.85f,0f);
        hvac=S("hvac_grey_metal",new Color(.64f,.66f,.68f),.45f,.6f);
        antiSlip=BraxChampionsFoundation.SourceMaterial("e1_ramp_anti_slip_concrete","anti_slip_concrete",2f,new Color(1.0f,1.11f,1.31f));
        rampWall=BraxChampionsFoundation.SourceMaterial("e1_ramp_wall_concrete","concrete_wall_008",2.5f,new Color(1.0f,1.05f,1.26f));
        pinWhite=Inside(S("pin_white",new Color(.95f,.95f,.96f),.25f,0f),.85f);
        pinRed=Inside(S("pin_red_neck",new Color(.80f,.05f,.06f),.3f,0f),.85f);
        ballRed=S("ball_red_pearl",new Color(.62f,.03f,.06f),.12f,.25f);
        ballBlue=S("ball_blue_pearl",new Color(.08f,.18f,.62f),.12f,.25f);
        ballGreen=S("ball_green_pearl",new Color(.06f,.45f,.22f),.12f,.25f);
        ballBlueSolid=S("ball_blue_solid",new Color(.07f,.20f,.65f),.12f,.1f);
        topiary=S("box_topiary",new Color(.16f,.33f,.12f),.9f,0f);
        cue=Inside(S("cue_silvered_maple",new Color(.80f,.82f,.84f),.3f,.2f),.8f);
        mirror=Inside(S("mirror",new Color(.86f,.88f,.90f),.04f,1f),.8f);
        var bc=new[]{new Color(.15f,.45f,.25f,.6f),new Color(.55f,.62f,.70f,.5f),new Color(.60f,.10f,.12f,.6f),new Color(.85f,.86f,.88f,.45f)};
        bottles=new Material[4];
        for(int i=0;i<4;i++)bottles[i]=S("bottle_"+i,bc[i],.05f,0f,new Color(bc[i].r*.25f,bc[i].g*.25f,bc[i].b*.25f),true);
        asphalt=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/asphalt.mat");
        if(!asphalt)asphalt=BraxChampionsFoundation.SourceMaterial("e1_asphalt","clean_asphalt",2.1f,Color.white);
        walk=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/paving.mat");
        if(!walk)walk=BraxChampionsFoundation.SourceMaterial("e1_city_paving","concrete_pavement_02",1.8f,Color.white);
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
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(gold,px-.03f,px+.03f,.75f,1.95f,f>0?.05f:-.11f,f>0?.11f:-.05f);
            }
            else
            {
                lm.BoxMinMax(blackMatte,a0,a1,.02f,height-.02f,-.025f,.025f);
                float px=side<0?a1-.12f:a0+.12f;
                foreach(float f in new[]{-1f,1f})lm.BoxMinMax(gold,px-.08f,px+.08f,1.02f,1.06f,f>0?.025f:-.075f,f>0?.075f:-.025f);
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
        foreach(float x in new[]{-56f,-29f,-1f,13f,39f,61f})BraxChampionsPromenadeFurniture.Lamp(m,cols,x,-38.6f);
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
        B(goldLed,x-.71f,x+.71f,2.59f,2.62f,z-.092f,z-.08f);
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
            for(float z=HD+3f;z<RampTop-1f;z+=5f)B(goldLed,s<0?x0:x1-.012f,s<0?x0+.012f:x1,-.2f,-.12f,z,z+1.2f);
        }
    }

    // ================================================================ SHELL
    static void Shell()
    {
        // ground floor slab (B1 ceiling under it), holes for the stair and the lift shaft
        void Slab(float x0,float x1,float z0,float z1){B(garageWhite,x0,x1,SlabBottom,SlabTop,z0,z1);Col(x0,x1,SlabBottom,FFL,z0,z1,"posadzka parter");}
        Slab(-HW,HW,-HD,LiftZ0);
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

    // facades: pilasters on the 10 m module with gold LED lines, glass band 0..5.4 at the front, black panels above with joints
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
            B(goldLed,x-.04f,x+.04f,.6f,Parapet-.6f,zf-.412f,zf-.40f);
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
            // upper band: black panels with 2 cm joints, gold canopy line and the LED line under it
            for(int r=0;r<4;r++)
            {
                float y0=Band+.15f+r*1.79f,y1=Mathf.Min(y0+1.77f,Parapet-.05f);
                int nc=Mathf.Max(1,Mathf.RoundToInt((b-a)/3.1f));
                for(int c=0;c<nc;c++){float p0=a+c*(b-a)/nc+.01f,p1=a+(c+1)*(b-a)/nc-.01f;B(black,p0,p1,y0,y1,zf-.06f,zf);}
            }
            B(gold,a,b,Band,Band+.15f,zf-.10f,zf);
            B(goldLed,a,b,Band-.02f,Band,zf-.30f,zf-.12f);
        }
        B(goldLed,-HW-.06f,HW+.06f,Parapet-.10f,Parapet-.02f,zf-.42f,zf-.40f);
        // ---- SIDES: pilasters every 10 m with LED lines, panels with joints, staff doors, the east roller door
        foreach(float s in new[]{-1f,1f})
        {
            float xo=s*HW;
            foreach(float z in new[]{-24.7f,-10f,0f,10f,24.7f})
            {
                B(black,s<0?xo-.40f:xo,s<0?xo:xo+.40f,Y0,Parapet,z-.30f,z+.30f,true);
                B(goldLed,s<0?xo-.412f:xo+.40f,s<0?xo-.40f:xo+.412f,.6f,Parapet-.6f,z-.04f,z+.04f);
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
            B(goldLed,x-.04f,x+.04f,.6f,Parapet-.6f,zb+.40f,zb+.412f);
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
        B(gold,g0,g0+.08f,FFL,7.6f,zp,zp+.6f);B(gold,g1-.08f,g1,FFL,7.6f,zp,zp+.6f);
        B(goldLed,g0+.08f,g1-.08f,7.52f,7.6f,zp,zp+.6f);
        B(goldLed,x0,x1,PortalTop-.14f,PortalTop-.04f,zp-.012f,zp+.04f);
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
        // canopy 6 m out at 5.0 m: black slab, gold LED edge, downlights
        float cz=zp-3f;
        B(black,x0-1.0f,x1+1.0f,5.0f,5.45f,cz,zp,true);
        B(goldLed,x0-1.0f,x1+1.0f,5.0f,5.45f,cz-.03f,cz);
        for(float x=x0;x<=x1+.01f;x+=2.5f)for(float z=cz+1f;z<zp-.5f;z+=2f)m.Cylinder(ledCool,new Vector3(x,4.99f,z),.12f,.012f,12,Quaternion.identity,false,true);
        // planters with box balls either side of the portal
        foreach(float x in new[]{x0-1.8f,x1+1.8f})
        {
            B(concrete,x-.7f,x+.7f,Y0,Y0+.9f,zp-1.2f,zp-.2f,true);
            B(BraxChampionsPromenadeFurniture.Lawn(),x-.62f,x+.62f,Y0+.9f,Y0+.92f,zp-1.12f,zp-.28f);
            Sphere(topiary,new Vector3(x,Y0+1.55f,zp-.7f),.62f,14,9);
        }
    }

    // bowling mural (board hero render): magenta lit panel x -58.8..-43.6 with giant 3D pins and a red ball
    static void Mural()
    {
        float x0=-58.8f,x1=-43.6f,zf=-HD-.42f;
        B(black,x0-.2f,x1+.2f,Y0,11.4f,zf-.10f,zf+.40f,true);                                    // back 2 cm off the glass
        B(magenta,x0,x1,1.2f,11.2f,zf-.12f,zf-.10f);
        for(float x=x0+.6f;x<x1;x+=.6f)B(black,x-.03f,x+.03f,1.2f,11.2f,zf-.20f,zf-.12f);       // perforated look
        for(float y=1.6f;y<11.2f;y+=.6f)B(black,x0,x1,y-.03f,y+.03f,zf-.20f,zf-.12f);
        B(purpleLed,x0,x1,.9f,1.0f,zf-.12f,zf-.10f);
        B(black,x0+.3f,x1-.3f,2.4f,2.6f,zf-1.3f,zf-.20f,true);                                       // ledge
        Pin(new Vector3(-53.6f,2.6f,zf-.75f),12.5f,20);
        Pin(new Vector3(-50.9f,2.6f,zf-.75f),11.0f,20);
        Sphere(ballRed,new Vector3(-47.0f,3.62f,zf-.75f),1.02f,24,14);
    }

    // ================================================================ SIGNS
    static void FrontSigns()
    {
        float zp=-HD-3f;
        // "E1" red, 3.6 m, and "CENTRUM ROZRYWKI" white on the portal head
        BraxChampionsLetters3D.Text(m,red,black,"E1",new Vector3(5f,10.4f,zp+.01f),180f,3.6f,.45f,.18f,11f);
        BraxChampionsLetters3D.Text(m,whiteLit,black,"CENTRUM ROZRYWKI",new Vector3(5f,8.55f,zp+.01f),180f,.90f,.20f,.14f,12.2f);
        Signs+=2;
        // east wing: five 3D signs with pictograms (board: KRĘGLE•BILARD•GRY•BAR•LOUNGE), centred on the panels
        var items=new[]{("KRĘGLE",17.5f),("BILARD",26.8f),("GRY",36.1f),("BAR",45.4f),("LOUNGE",54.6f)};
        float y=7.35f,zf=-HD-.06f;
        foreach(var (t,x) in items)
        {
            B(black,x-4.1f,x+4.1f,y-.75f,y+1.15f,zf-.12f,zf);
            B(goldLed,x-4.1f,x+4.1f,y-.75f,y-.71f,zf-.132f,zf-.12f);
            BraxChampionsLetters3D.Text(m,whiteLit,black,t,new Vector3(x+.9f,y-.30f,zf-.115f),180f,.85f,.12f,.12f,5.2f);
            Pictogram(t,new Vector3(x-2.9f,y+.20f,zf-.12f));
            Signs++;
        }
        // hall side lettering over the openings
        BraxChampionsLetters3D.Text(m,gold,black,"KRĘGLE",new Vector3(.14f,7.6f,-9f),90f,.95f,.10f,.12f,8f);
        BraxChampionsLetters3D.Text(m,gold,black,"BILARD • BAR",new Vector3(9.86f,7.6f,-6f),-90f,.95f,.10f,.12f,9f);
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
                Sphere(ballRed,c+new Vector3(0f,-.62f,-.55f),.24f,14,9);break;
            case "BILARD":
                for(int r=0;r<3;r++)for(int i=0;i<=r;i++)
                    Sphere(i==0&&r==0?whiteLit:(r+i)%2==0?red:gold,c+new Vector3((i-r*.5f)*.36f,.35f-r*.32f,-.2f),.17f,12,8);break;
            case "GRY":
                B(whiteLit,c.x-.55f,c.x+.55f,c.y-.30f,c.y+.20f,c.z-.18f,c.z);
                foreach(float s in new[]{-1f,1f})m.Cylinder(whiteLit,c+new Vector3(s*.55f,-.05f,-.18f),.25f,.18f,16,Quaternion.Euler(90,0,0),true);
                Sphere(red,c+new Vector3(.32f,-.02f,-.2f),.07f,10,6);Sphere(gold,c+new Vector3(.46f,-.14f,-.2f),.07f,10,6);
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
                B(goldLed,x<5?.15f:9.838f,x<5?.162f:9.85f,.6f,10.8f,z-.03f,z+.03f);
            }
        // ceiling 11.0: black coffers with a gold LED frame and cool downlights
        B(blackMatte,x0,x1,11.0f,11.1f,z0,z1);
        for(float z=z0+2f;z<z1-1f;z+=4f){B(goldLed,x0+.6f,x1-.6f,10.97f,11.0f,z-.03f,z+.03f);
            foreach(float x in new[]{2.5f,7.5f})m.Cylinder(ledCool,new Vector3(x,10.985f,z+2f),.14f,.015f,12,Quaternion.identity,false,true);}
        // reception: curved desk (black lacquer front, white marble top, gold kick line), 140 deg arc facing the doors
        float rz=-2.0f,ccx=5f,ccz=rz+3.3f,R=3.0f;int seg=12;
        for(int i=0;i<seg;i++)
        {
            float a=(-70f+140f*(i+.5f)/seg)*Mathf.Deg2Rad,w=2f*(R+.3f)*Mathf.Sin(70f*Mathf.Deg2Rad/seg)+.02f;
            var rot=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);var outward=new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a));
            var c=new Vector3(ccx,0,ccz)+outward*R;
            m.Box(lacquer,c+Vector3.up*((FFL+1.08f)*.5f),new Vector3(w,1.08f-FFL,.6f),rot);
            m.Box(marbleWhite,c+outward*.05f+Vector3.up*1.105f,new Vector3(w+.04f,.05f,.75f),rot);
            m.Box(goldLed,c+outward*.306f+Vector3.up*(FFL+.10f),new Vector3(w,.04f,.012f),rot);
        }
        Col(2.0f,8.0f,FFL,1.13f,rz,rz+2.6f,"recepcja");
        BraxChampionsLetters3D.Text(m,gold,black,"E1",new Vector3(5f,.55f,rz+.01f),180f,.40f,.03f,.14f,1.2f);
        // back wall of the reception: black with a gold E1 and a screen wall of prices
        B(black,2.2f,7.8f,FFL,4.2f,6.0f,6.3f,true);
        B(screen,2.6f,7.4f,2.0f,3.9f,5.98f,6.0f);
        BraxChampionsLetters3D.Text(m,gold,black,"CENTRUM ROZRYWKI",new Vector3(5f,1.30f,6.01f),180f,.32f,.04f,.14f,4.6f);
        // ticket kiosks and benches
        foreach(float z in new[]{-18f,-14f})foreach(float x in new[]{1.0f,9.0f})
        {
            B(black,x-.3f,x+.3f,FFL,1.5f,z-.25f,z+.25f,true);
            B(screen,x<5?x+.301f:x-.302f,x<5?x+.302f:x-.301f,1.0f,1.45f,z-.18f,z+.18f);
        }
        foreach(float z in new[]{-9f,10f})BraxChampionsPromenadeFurniture.BenchFramed(m,cols,5f,z,new Vector2(0,-1),"hol "+z);
        // glass guard round the stair hole, the lift head: black box with a gold frame and its call panel
        float sx1=StairX0+(Risers-1)*StairRun;
        B(clearGlass,StairX0,sx1,FFL,FFL+1.1f,StairZ0-.02f,StairZ0,true);
        B(clearGlass,StairX0,StairX0+.02f,FFL,FFL+1.1f,StairZ0,HD-.3f,true);
        B(BraxChampionsHarbour.Steel,StairX0,sx1,FFL+1.1f,FFL+1.15f,StairZ0-.04f,StairZ0+.02f);
        BraxChampionsLetters3D.Text(m,gold,black,"PARKING B1",new Vector3(3.2f,2.2f,HD-.29f),180f,.30f,.03f,.14f,4f);
    }

    // ================================================================ BOWLING (x -59.7..0)
    static void Bowling()
    {
        float x0=-HW+.3f,x1=-.15f,z0=-HD+.3f;
        float laneEnd=FoulZ+LaneLen+1.4f,mask0=laneEnd,machine=13.0f;
        // floor: cosmic carpet round the lanes, machine room concrete
        B(carpet,x0,x1,SlabTop,FFL,z0,-17.6f);
        B(carpet,x0,LaneX0,SlabTop,FFL,-17.6f,machine);
        B(carpet,LaneX0+12*PairPitch,x1,SlabTop,FFL,-17.6f,machine);
        B(techFloor,LaneX0,LaneX0+12*PairPitch,SlabTop,FFL,mask0+.3f,machine-.1f);
        B(carpet,x0,LaneX0,SlabTop,FFL,machine,14.9f);B(carpet,LaneX0+12*PairPitch,x1,SlabTop,FFL,machine,14.9f);
        B(carpet,LaneX0,LaneX0+12*PairPitch,SlabTop,FFL,machine+.1f,14.9f);                          // back corridor
        // suspended black ceiling at 6 m with purple LED lines over the lanes
        B(blackMatte,x0,x1,Ceiling,Ceiling+.1f,z0,14.9f);
        // lanes
        for(int p=0;p<12;p++)
        {
            float xp=LaneX0+PairPitch*.5f+p*PairPitch;
            foreach(float s in new[]{-1f,1f})
            {
                float xc=xp+s*.815f;
                // approach (lane + gutter width), foul line, lane bed, gutters, pin deck, pit
                B(maple,xc-.775f,xc+.775f,SlabTop,FFL,-17.6f,FoulZ-.02f);
                B(black,xc-.775f,xc+.775f,SlabTop,FFL,FoulZ-.02f,FoulZ+.02f);
                B(maple,xc-.5335f,xc+.5335f,SlabTop,FFL,FoulZ+.02f,laneEnd-.3f);
                foreach(float g in new[]{-1f,1f})B(gutter,g<0?xc-.775f:xc+.5335f,g<0?xc-.5335f:xc+.775f,SlabTop,FFL-.06f,FoulZ+.02f,laneEnd-.3f);
                // pins: head pin at foul + 18.29, the 4 rows 30.5 cm apart (triangle 12 in)
                float hz=FoulZ+LaneLen;
                for(int r=0;r<4;r++)for(int i=0;i<=r;i++)Pin(new Vector3(xc+(i-r*.5f)*.305f,FFL,hz+r*.264f),1f,8);
                // aiming arrows (7 dark diamonds 8 mm in the lane? no: flush strips of dark maple would be coplanar) - range finder dots on the approach are painted as 2 cm black rings
                // overhead score screen on a ceiling rod
                B(black,xc-.72f,xc+.72f,3.05f,3.95f,-16.95f,-16.85f);
                B(screen,xc-.66f,xc+.66f,3.11f,3.89f,-16.96f,-16.95f);
                m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(xc,3.95f,-16.9f),.025f,Ceiling-3.95f,8);
            }
            // capping between the pairs, divider in the pair, kickbacks at the pin deck
            float cap=xp+PairPitch*.5f;
            if(p<11){B(black,cap-.15f,cap+.15f,SlabTop,FFL+.12f,FoulZ+.02f,laneEnd-.3f,true);B(carpet,cap-.15f,cap+.15f,SlabTop,FFL,-17.6f,FoulZ+.02f);}
            B(black,xp-.04f,xp+.04f,SlabTop,FFL+.25f,FoulZ+.02f,laneEnd-.3f);
            B(black,xp-.04f,xp+.04f,SlabTop,FFL,-17.6f,FoulZ+.02f);
            foreach(float s in new[]{-1f,1f})B(blackMatte,xp+s*1.59f-.06f,xp+s*1.59f+.06f,FFL,1.3f,hz0(),laneEnd+.3f,true);
            // ball return between the two approaches, balls on it
            B(black,xp-.35f,xp+.35f,FFL,.80f,-19.2f,-17.6f,true);
            B(purpleLed,xp-.36f,xp+.36f,.62f,.66f,-19.21f,-17.59f);
            for(int i=0;i<4;i++)Sphere(i%2==0?ballRed:ballBlue,new Vector3(xp-.18f+i*.12f,.91f,-18.4f+i%2*.2f),.109f,10,7);
            // settee: U sofa in plum velvet facing the lanes, a small table, the scorer console
            float sz=-21.8f;
            B(velvet,xp-1.45f,xp+1.45f,FFL,.62f,sz-.5f,sz+.2f,true);
            B(velvet,xp-1.45f,xp+1.45f,.62f,1.05f,sz-.5f,sz-.3f);
            foreach(float s in new[]{-1f,1f})B(velvet,xp+s*1.45f-(s>0?.6f:0f),xp+s*1.45f+(s<0?.6f:0f),FFL,.62f,sz+.2f,sz+1.4f,true);
            B(lacquer,xp-.45f,xp+.45f,FFL,.46f,sz+.6f,sz+1.2f,true);
            B(marbleWhite,xp-.47f,xp+.47f,.46f,.50f,sz+.58f,sz+1.22f);
            B(goldLed,xp-.45f,xp+.45f,.40f,.43f,sz+.588f,sz+.60f);
            B(black,xp-.2f,xp+.2f,FFL,1.0f,-20.2f,-19.9f,true);
            B(screen,xp-.18f,xp+.18f,.72f,.98f,-20.21f,-20.2f);
        }
        B(black,LaneX0,LaneX0+12*PairPitch,SlabTop,SlabTop+.02f,laneEnd-.3f,laneEnd+.3f);              // pit behind the pin decks
        // masking units over the pin decks: black panel, purple LED lines, BRAXHORN E1 text every pair
        float mz=mask0-.8f;
        B(black,LaneX0,LaneX0+12*PairPitch,2.6f,Ceiling,mz-.25f,mz);
        for(int i=0;i<3;i++)B(purpleLed,LaneX0,LaneX0+12*PairPitch,2.75f+i*.35f,2.79f+i*.35f,mz-.262f,mz-.25f);
        for(int p=0;p<12;p++)
        {
            float xp=LaneX0+PairPitch*.5f+p*PairPitch;
            BraxChampionsLetters3D.Text(m,whiteLit,black,(2*p+1).ToString()+"   "+(2*p+2).ToString(),new Vector3(xp,4.3f,mz-.24f),180f,.42f,.03f,.14f,3f);
        }
        // pinsetters behind (dark machines over each pin deck) and the machine room wall + door
        for(int l=0;l<Lanes;l++)
        {
            float xp=LaneX0+PairPitch*.5f+(l/2)*PairPitch,xc=xp+(l%2==0?-.815f:.815f);
            B(hvac,xc-.7f,xc+.7f,1.4f,2.55f,mz,mz+2.2f);
            B(black,xc-.72f,xc+.72f,1.3f,1.4f,mz,mz+2.2f);
        }
        WallX(blackMatte,machine,LaneX0,LaneX0+12*PairPitch,FFL,Ceiling,.2f,(-36.6f,-35.4f,2.4f));
        Door("E1_MACHINE","E1 bowling machine room",-36f,FFL,machine,0f,1.2f,2.4f-FFL,false);
        B(blackMatte,LaneX0+12*PairPitch,LaneX0+12*PairPitch+.2f,FFL,Ceiling,mz,machine,true);
        B(blackMatte,LaneX0-.2f,LaneX0,FFL,Ceiling,mz,machine,true);
        // purple LED wall lines on the west wall and the hall wall
        for(float z=-22f;z<12f;z+=3f)B(purpleLed,x0,x0+.012f,.8f,Ceiling-.4f,z-.03f,z+.03f);
        // shoe counter + ball racks + café tables in the side area (x -15.9..0)
        float sx=LaneX0+12*PairPitch+.4f;
        B(lacquer,sx+1f,sx+7f,FFL,1.08f,4.0f,4.7f,true);
        B(marbleWhite,sx+.95f,sx+7.05f,1.08f,1.13f,3.95f,4.75f);
        B(black,sx+1f,sx+7f,FFL,2.4f,7.6f,8.0f,true);
        for(int r=0;r<4;r++)for(int i=0;i<8;i++)B(i%2==0?velvet:charcoal,sx+1.2f+i*.7f,sx+1.75f+i*.7f,.5f+r*.45f,.62f+r*.45f,7.3f,7.6f);  // shoe shelves
        for(int k=0;k<3;k++)
        {
            float rz=-10f+k*4f;
            B(BraxChampionsHarbour.Steel,sx+.2f,sx+.3f,FFL,1.2f,rz,rz+2.4f,true);
            for(int lv=0;lv<3;lv++){B(black,sx+.3f,sx+.7f,.45f+lv*.35f,.48f+lv*.35f,rz,rz+2.4f);
                for(int i=0;i<9;i++)Sphere((i+lv)%3==0?ballRed:(i+lv)%3==1?ballBlue:ballGreen,
                    new Vector3(sx+.5f,.59f+lv*.35f,rz+.14f+i*.26f),.109f,10,7);}
        }
        for(int k=0;k<4;k++)
        {
            float tx=sx+4.5f+(k%2)*5f,tz=-8f+(k/2)*6f;
            m.Cylinder(black,new Vector3(tx,FFL,tz),.25f,.02f,16);m.Cylinder(black,new Vector3(tx,FFL,tz),.04f,1.05f,10);
            m.Cylinder(marbleWhite,new Vector3(tx,1.05f,tz),.45f,.04f,20);
            Col(tx-.45f,tx+.45f,FFL,1.09f,tz-.45f,tz+.45f,"stolik");
            for(int c=0;c<3;c++){float a=c*120f*Mathf.Deg2Rad;Stool(tx+Mathf.Cos(a)*.85f,tz+Mathf.Sin(a)*.85f,.78f);}
        }
    }
    static float hz0()=>FoulZ+LaneLen-2.6f;
    static void Stool(float x,float z,float h)
    {
        m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(x,FFL,z),.22f,.03f,16);
        m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(x,FFL+.03f,z),.035f,h-.1f,10);
        m.Cylinder(black,new Vector3(x,FFL+h-.08f,z),.20f,.08f,16);
        Col(x-.2f,x+.2f,FFL,FFL+h,z-.2f,z+.2f,"hoker");
    }

    // ================================================================ BILLIARDS + BAR + LOUNGE (x 10..59.7)
    static void Billiards()
    {
        float x0=10.15f,x1=HW-.3f,z0=-HD+.3f;
        B(carpetGraphite,x0,x1,SlabTop,FFL,-15.1f,14.9f);
        B(hallFloor,x0,x1,SlabTop,FFL,z0,-15.1f);                                                         // bar / lounge: black marble
        B(blackMatte,x0,x1,Ceiling,Ceiling+.1f,z0,14.9f);
        // tables 9 ft (2.84 x 1.57 incl. rails), long side along z, 6 x 3
        for(int i=0;i<6;i++)for(int j=0;j<3;j++)
        {
            float cx=15.8f+i*7.6f,cz=-8f+j*8f;
            Table(cx,cz);
            // pendant: black bar with three shades and warm light under them
            B(black,cx-.08f,cx+.08f,2.35f,2.45f,cz-1.25f,cz+1.25f);
            for(int k=-1;k<=1;k++){m.Cylinder(black,new Vector3(cx,2.05f,cz+k*.85f),.22f,.30f,16,Quaternion.identity,true,false,.10f);
                m.Cylinder(goldLed,new Vector3(cx,2.04f,cz+k*.85f),.20f,.012f,16,Quaternion.identity,false,true);}
            foreach(float s in new[]{-1f,1f})m.Cylinder(BraxChampionsHarbour.Steel,new Vector3(cx,2.45f,cz+s*1.0f),.012f,Ceiling-2.45f,6);
        }
        // cue racks on the east wall
        for(int k=0;k<6;k++)
        {
            float rz=-12f+k*4.5f;
            B(lacquer,x1-.12f,x1,1.0f,2.4f,rz,rz+1.4f,true);
            for(int c=0;c<8;c++)m.Cylinder(cue,new Vector3(x1-.2f,1.05f,rz+.12f+c*.16f),.014f,1.45f,6);
        }
        for(float z=-14f;z<14f;z+=3.5f)B(goldLed,x1-.012f,x1,.8f,Ceiling-.4f,z-.03f,z+.03f);
        // partition to the bar: back bar wall x 20..50 at z -15, the bar counter in front of it
        B(blackMatte,20f,50f,FFL,Ceiling,-15.2f,-15.0f,true);
        for(int s=0;s<3;s++)
        {
            float y=1.3f+s*.55f;B(gold,21f,49f,y,y+.04f,-15.55f,-15.2f);B(goldLed,21f,49f,y+.04f,y+.05f,-15.25f,-15.2f);
            for(float x=21.3f;x<48.8f;x+=.32f)
            {
                int c=((int)(x*7f))%4;
                m.Cylinder(bottles[c],new Vector3(x,y+.04f,-15.38f),.04f,.30f,8,Quaternion.identity,true,false,.025f);
            }
        }
        B(lacquer,21f,49f,FFL,.95f,-15.7f,-15.2f,true);                                              // back counter
        // bar counter: fluted black front, gold kick line, white marble top
        float bz0=-18.3f,bz1=-17.6f;
        B(lacquer,22f,48f,FFL,1.08f,bz0,bz1,true);
        for(float x=22.1f;x<47.95f;x+=.16f)B(black,x,x+.08f,FFL+.12f,1.0f,bz0-.03f,bz0);
        B(goldLed,22f,48f,FFL+.06f,FFL+.10f,bz0-.032f,bz0);
        B(marbleWhite,21.9f,48.1f,1.08f,1.13f,bz0-.25f,bz1+.05f);
        for(float x=23f;x<=47.01f;x+=1.0f)Stool(x,bz0-.65f,.78f);
        // lounge to the glass: six sofa groups (charcoal, gold legs), low marble tables
        for(int k=0;k<6;k++)
        {
            float cx=14.5f+k*7.6f,cz=-22.2f;
            Sofa(cx,cz-.9f,3.0f,1f);Sofa(cx,cz+1.9f,3.0f,-1f);
            B(marbleWhite,cx-.7f,cx+.7f,.38f,.42f,cz-.05f,cz+.95f,true);
            B(gold,cx-.6f,cx+.6f,FFL,.38f,cz+.05f,cz+.85f);
        }
    }
    static void Table(float cx,float cz)
    {
        float hx=.785f,hz=1.42f,top=.80f;
        foreach(float s in new[]{-1f,1f})foreach(float t in new[]{-1f,1f})B(lacquer,cx+s*(hx-.18f)-.09f,cx+s*(hx-.18f)+.09f,FFL,top-.18f,cz+t*(hz-.3f)-.09f,cz+t*(hz-.3f)+.09f);
        B(lacquer,cx-hx+.08f,cx+hx-.08f,top-.18f,top-.02f,cz-hz+.08f,cz+hz-.08f);                  // apron / slate bed
        B(felt,cx-hx+.16f,cx+hx-.16f,top-.02f,top,cz-hz+.16f,cz+hz-.16f);
        // rails: black with felt cushions, gold sights
        foreach(float s in new[]{-1f,1f})
        {
            B(lacquer,s<0?cx-hx:cx+hx-.16f,s<0?cx-hx+.16f:cx+hx,top-.10f,top+.04f,cz-hz,cz+hz);
            B(lacquer,cx-hx+.16f,cx+hx-.16f,top-.10f,top+.04f,s<0?cz-hz:cz+hz-.16f,s<0?cz-hz+.16f:cz+hz);
            B(felt,s<0?cx-hx+.16f:cx+hx-.20f,s<0?cx-hx+.20f:cx+hx-.16f,top,top+.035f,cz-hz+.24f,cz+hz-.24f);
            for(int k=-3;k<=3;k++)if(k!=0)B(gold,s<0?cx-hx+.05f:cx+hx-.11f,s<0?cx-hx+.11f:cx+hx-.05f,top+.04f,top+.046f,cz+k*.355f-.02f,cz+k*.355f+.02f);
        }
        // six pockets
        foreach(float s in new[]{-1f,1f})foreach(float t in new[]{-1f,0f,1f})
            m.Cylinder(black,new Vector3(cx+s*(hx-.14f),top-.12f,cz+t*(hz-.14f)),.075f,.175f,12);   // 1.5 cm over the rail
        // racked balls (5 rows) at the foot spot, the cue ball at the head
        float fz=cz+hz*.5f;
        for(int r=0;r<5;r++)for(int i=0;i<=r;i++)
        {
            int n=r*(r+1)/2+i;var c=n%3==0?red:n%3==1?gold:ballBlueSolid;
            Sphere(n==4?lacquer:c,new Vector3(cx+(i-r*.5f)*.058f,top+.029f,fz+r*.05f),.0286f,8,5);
        }
        Sphere(whiteLit,new Vector3(cx,top+.029f,cz-hz*.5f),.0286f,8,5);
        Col(cx-hx,cx+hx,FFL,top+.04f,cz-hz,cz+hz,"stół bilardowy");
        BilliardTables++;
    }
    static void Sofa(float cx,float cz,float len,float face)
    {
        // face +1: seat toward +z (back on -z side)
        float back=face>0?cz-.45f:cz+.45f,front=face>0?cz+.45f:cz-.45f;
        float z0=Mathf.Min(back,front),z1=Mathf.Max(back,front);
        B(charcoal,cx-len*.5f,cx+len*.5f,FFL+.12f,.62f,z0,z1,true);
        B(charcoal,cx-len*.5f,cx+len*.5f,.62f,1.02f,face>0?z0:z1-.22f,face>0?z0+.22f:z1);
        foreach(float s in new[]{-1f,1f})B(charcoal,cx+s*len*.5f-(s>0?.2f:0f),cx+s*len*.5f+(s<0?.2f:0f),.62f,.80f,z0,z1);
        foreach(float s in new[]{-1f,1f})B(gold,cx+s*(len*.5f-.15f)-.03f,cx+s*(len*.5f-.15f)+.03f,FFL,FFL+.12f,z0+.1f,z1-.1f);
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
            BraxChampionsLetters3D.Text(m,gold,black,t,new Vector3(x,2.75f,14.91f),180f,.20f,.02f,.14f,2.6f);
        // plant / store: units, shelves
        foreach(var (a,b) in new[]{(-59.4f,-57.6f),(-52.4f,-50.6f)})B(hvac,a,b,FFL,2.2f,16.5f,23.5f,true);   // path x -57..-53 between the two doors
        for(int i=0;i<2;i++){float x=-48.5f+i*4.6f;for(int lv=0;lv<4;lv++)B(BraxChampionsHarbour.Steel,x,x+3.6f,.4f+lv*.7f,.44f+lv*.7f,17.5f,24.0f);
            foreach(float px in new[]{x,x+3.56f})foreach(float pz in new[]{17.5f,23.96f})B(BraxChampionsHarbour.Steel,px,px+.04f,FFL,2.6f,pz,pz+.04f);Col(x,x+3.6f,FFL,2.6f,17.5f,24f,"regał");}
        // cloakrooms: lockers and benches
        foreach(var (a,b) in new[]{(-29.9f,-22.7f),(-7.3f,-.25f)})
        {
            for(float x=a+.1f;x<b-.5f;x+=.5f){B(BraxChampionsHarbour.Graphite,x,x+.48f,FFL,2.0f,23.9f,24.4f);B(gold,x+.38f,x+.42f,1.1f,1.3f,23.88f,23.9f);}
            Col(a,b,FFL,2f,23.9f,24.4f,"szafki");
            B(lacquer,a+1f,b-1f,.42f,.47f,19.3f,19.7f,true);
        }
        // toilets: cubicles, a basin counter with mirror, urinals in M
        foreach(var (a,b,men) in new[]{(-22.4f,-15.1f,true),(-14.9f,-7.6f,false)})
        {
            int n=men?3:4;float w=(b-a-.2f)/n;
            for(int i=0;i<=n;i++)B(black,a+.1f+i*w-.02f,a+.1f+i*w+.02f,FFL+.15f,2.2f,21.6f,z1,true);
            for(int i=0;i<n;i++)
            {
                float cx=a+.1f+(i+.5f)*w;
                B(black,cx-w*.5f+.03f,cx+w*.5f-.03f,FFL+.15f,2.2f,21.56f,21.6f);                     // door panel (closed)
                B(whiteLit,cx-.19f,cx+.19f,FFL,.42f,23.9f,z1-.05f);
            }
            B(marbleWhite,a+.3f,b-.3f,.82f,.86f,15.25f,15.8f,true);
            B(mirror,a+.3f,b-.3f,1.05f,2.05f,15.21f,15.25f);
            for(float x=a+1.2f;x<b-.6f;x+=1.4f)B(whiteLit,x-.22f,x+.22f,.80f,.83f,15.3f,15.7f);
            if(men)for(int i=0;i<3;i++)B(whiteLit,b-.6f-i*.8f-.2f,b-.6f-i*.8f+.2f,.55f,1.15f,18.6f,18.95f);
        }
        // east band: games / VR open to the billiards (3 openings), offices, plant
        WallX(blackMatte,15f,10.15f,HW-.3f,FFL,Ceiling,.2f,(13f,19f,3.0f),(22f,28f,3.0f),(31f,37f,3.0f),(43.4f,44.6f,2.4f),(53.9f,55.1f,2.4f));
        Door("E1_OFFICES","E1 offices / biura",44f,FFL,15f,0f,1.2f,2.4f-FFL,false);
        Door("E1_TECH_E","E1 plant room east",54.5f,FFL,15f,0f,1.2f,2.4f-FFL,false);
        BraxChampionsLetters3D.Text(m,gold,black,"STREFA GIER • VR",new Vector3(25f,3.4f,14.91f),180f,.38f,.03f,.14f,9f);
        foreach(var (x,t) in new[]{(44f,"BIURA"),(54.5f,"TECH.")})BraxChampionsLetters3D.Text(m,gold,black,t,new Vector3(x,2.75f,14.91f),180f,.20f,.02f,.14f,2.6f);
        foreach(float x in new[]{40f,50f})B(blackMatte,x-.1f,x+.1f,FFL,top,z0,z1,true);
        B(garageWhite,40f,HW-.3f,top,top+.1f,z0,z1);
        B(blackMatte,10.15f,40f,Ceiling,Ceiling+.1f,z0,z1);
        B(carpet,10.15f,40f,SlabTop,FFL,z0,z1);B(carpetGraphite,40.1f,49.9f,SlabTop,FFL,z0,z1);B(techFloor,50.1f,HW-.3f,SlabTop,FFL,z0,z1);
        // arcades along the back wall, air hockey, VR pods, claw machines
        for(int i=0;i<10;i++)
        {
            float x=11.0f+i*1.25f;
            B(black,x,x+1.0f,FFL,1.85f,23.3f,24.3f,true);
            B(screen,x+.12f,x+.88f,1.1f,1.65f,23.28f,23.3f);
            B(i%2==0?purpleLed:goldLed,x,x+1.0f,1.85f,1.95f,23.3f,24.3f);
            B(lacquer,x+.05f,x+.95f,.85f,.95f,22.9f,23.3f);
        }
        foreach(float x in new[]{27f,32.5f})
        {
            B(black,x-.65f,x+.65f,FFL,.78f,17.0f,19.4f,true);
            B(whiteLit,x-.58f,x+.58f,.78f,.80f,17.07f,19.33f);
            B(purpleLed,x-.66f,x+.66f,.78f,.84f,16.99f,17.05f);B(purpleLed,x-.66f,x+.66f,.78f,.84f,19.35f,19.41f);
        }
        for(int i=0;i<4;i++)
        {
            float x=26.5f+i*3.2f,z=22.4f;
            m.Cylinder(black,new Vector3(x,FFL,z),1.0f,.12f,24);
            m.Cylinder(purpleLed,new Vector3(x,FFL+.12f,z),1.0f,.02f,24,Quaternion.identity,false,false);
            m.Tube(BraxChampionsHarbour.Steel,new Vector3(x-.9f,FFL+.12f,z),new Vector3(x-.9f,1.1f,z),.025f);
            m.Tube(BraxChampionsHarbour.Steel,new Vector3(x+.9f,FFL+.12f,z),new Vector3(x+.9f,1.1f,z),.025f);
            m.Tube(BraxChampionsHarbour.Steel,new Vector3(x-.9f,1.1f,z),new Vector3(x+.9f,1.1f,z),.025f);
            Col(x-1f,x+1f,FFL,1.1f,z-1f,z+1f,"VR");
        }
        foreach(float x in new[]{38.0f,36.7f})
        {
            B(black,x-.55f,x+.55f,FFL,.9f,17.0f,18.1f,true);B(clearGlass,x-.5f,x+.5f,.9f,1.9f,17.05f,18.05f);
            B(black,x-.55f,x+.55f,1.9f,2.1f,17.0f,18.1f);B(goldLed,x-.56f,x+.56f,2.02f,2.06f,16.99f,18.11f);
        }
        // offices: two desks with chairs
        foreach(float z in new[]{18f,22f})
        {
            B(lacquer,41f,45f,.74f,.78f,z-.4f,z+.4f,true);B(lacquer,41.1f,41.2f,FFL,.74f,z-.35f,z+.35f);B(lacquer,44.8f,44.9f,FFL,.74f,z-.35f,z+.35f);
            B(screen,42.6f,43.4f,.82f,1.3f,z+.18f,z+.2f);Stool(43f,z-.85f,.5f);
        }
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
        // lift shaft (white in B1, black + gold over the hall floor), front wall with the door opening at both stops
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
            B(gold,dx0-.08f,dx0,fy,fy+2.28f,sz0-.04f,sz0);B(gold,dx1,dx1+.08f,fy,fy+2.28f,sz0-.04f,sz0);B(gold,dx0-.08f,dx1+.08f,fy+2.2f,fy+2.28f,sz0-.04f,sz0);
            B(BraxChampionsHarbour.Steel,dx1+.2f,dx1+.32f,fy+1.0f,fy+1.3f,sz0-.03f,sz0);
            B(goldLed,dx1+.235f,dx1+.285f,fy+1.12f,fy+1.17f,sz0-.04f,sz0-.03f);
        }
        B(goldLed,sx0,sxx,shaftTop+.2f,shaftTop+.24f,sz0-.005f,sz0+.04f);
        BraxChampionsLetters3D.Text(m,gold,black,"WINDA • PARKING B1",new Vector3(8.6f,shaftTop-.55f,sz0+.005f),180f,.20f,.02f,.14f,2.4f);
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
        m.BoxMinMax(gold,xc-hw+.05f,xc+hw-.05f,.9f,.94f,z1-.03f,z1);
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

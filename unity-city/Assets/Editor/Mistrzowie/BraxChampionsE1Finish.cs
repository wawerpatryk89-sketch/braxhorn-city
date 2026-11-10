using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 10.11 21:57: "Całkowity brak wykończenia" - "masz wykończyć wszystko tak, jak się wykańcza mieszkanie normalnie",
// "co to za ściany, co to za elewacje". E1 finished like a building handed over for use, every room:
// * walls: smooth painted plaster (BraxChampionsE1.Materials), walnut slat acoustic panels on felt in the bowling and
//   billiards halls, ceramic tiles floor to ceiling in the toilets; black metal skirting along every wall, black steel
//   frames round every door;
// * ceilings: painted (graphite in the halls, white in the back rooms) with real fittings on a grid - recessed downlights
//   (LED panels in the back rooms), square air diffusers, sprinkler heads, smoke detectors;
// * services: exit signs over every door, fire extinguishers with their signs by the exits, hose reel cabinets in the
//   halls, light switches by the doors, sockets along the walls of the rooms;
// * toilets: hand dryers, soap dispensers, towel dispensers, the mirror light, hooks, bins;
// * bar: mirror wall behind the bottles, beer towers, coffee machine, glass-door fridges, pendants over the counter;
// * plants in pots in the hall, lounge, bowling and offices (Poly Haven CC0 model, real size);
// * outside (the board): fibre-cement cladding panels, dark granite paving, lit planter beds along the glass, palms in
//   stone planters before the front.
public static partial class BraxChampionsE1
{
    static Material wallWhite,cladding,ceilWhite,acousticFelt,fixture,exitGreen,exitWhite,fireRed,grilleDark,wcFloor,terrazzo,planterStone,plantSoil;
    static readonly List<(float x,float y,float z,float yaw,float w,float h,bool glass,string label)> doorList=new List<(float,float,float,float,float,float,bool,string)>();
    public static int FinishItems;

    static void FinishMaterials()
    {
        cladding=BraxChampionsFoundation.LibraryMetal("e1_facade_black_metal_panels",new Color(.17f,.18f,.215f));   // board "Panel metalowy (czarny mat)": brushed grain reads
        ceilWhite=Inside(S("ceiling_paint_white_matte",new Color(.92f,.92f,.91f),.88f,0f),BraxChampionsInteriorLight.Ceilings);
        wallWhite=Inside(S("wall_paint_white_matte",new Color(.88f,.88f,.87f),.85f,0f),BraxChampionsInteriorLight.Walls);
        // the interior ambient's ground colour is warm (.80,.77,.72): a downward face (a ceiling) took it and read beige /
        // like raw concrete (owner 10.11 22:43 on the hotel). White paint keeps a neutral ground colour: white stays white.
        foreach(var w in new[]{ceilWhite,wallWhite})w.SetColor("_GroundColor",new Color(.96f,.96f,.97f));
        acousticFelt=Inside(Tex("acoustic_felt_black","hessian_230",.6f,new Color(.21f,.25f,.38f)),.8f);
        fixture=Inside(S("fixture_white_powder_coat",new Color(.93f,.94f,.95f),.35f,.05f),.9f);
        exitGreen=S("exit_sign_green",new Color(.05f,.62f,.25f),.3f,0f,new Color(.05f,.95f,.35f));
        exitWhite=S("exit_sign_white",new Color(.97f,.98f,.98f),.3f,0f,new Color(1.0f,1.0f,1.0f));
        fireRed=Inside(S("fire_red_enamel",new Color(.72f,.04f,.05f),.25f,.10f),.9f);
        grilleDark=Inside(S("diffuser_grille_dark",new Color(.14f,.14f,.15f),.5f,.30f),.9f);
        wcFloor=Inside(Tex("wc_floor_tiles","anti_skid_tiles",1.2f,new Color(.90f,1.14f,1.50f)),.85f);           // (134,107,84) -> grey
        terrazzo=Inside(Tex("cloakroom_terrazzo","terrazzo_tiles",1.5f,new Color(1.10f,1.38f,1.75f)),.85f);
        planterStone=Tex("planter_granite","granite_tile_03",1.6f,new Color(.70f,.78f,.92f));
        plantSoil=BraxChampionsPromenadeFurniture.Lawn();
    }

    static void Finish()
    {
        FinishItems=0;
        Linings();
        Skirting();
        DoorFrames();
        Ceilings();
        Services();
        Plants();
        Rooms();
        Outside();
    }

    // ================================================================ walls
    // walnut slats (35 x 30 mm, every 70 mm) on black acoustic felt, along a wall face at c (x when alongZ), facing dir,
    // from a to b, y0..y1
    static void Slats(bool alongZ,float c,float dir,float a,float b,float y0,float y1)
    {
        if(b-a<.4f)return;
        float f0=c+dir*.02f,s0=f0,s1=f0+dir*.03f;
        if(alongZ){B(acousticFelt,Mathf.Min(c,f0),Mathf.Max(c,f0),y0,y1,a,b);}
        else B(acousticFelt,a,b,y0,y1,Mathf.Min(c,f0),Mathf.Max(c,f0));
        for(float u=a+.02f;u+.035f<=b-.02f;u+=.07f)
        {
            if(alongZ)RB(lacquer,Mathf.Min(s0,s1),Mathf.Max(s0,s1),y0+.01f,y1-.01f,u,u+.035f,.006f,false,1);
            else RB(lacquer,u,u+.035f,y0+.01f,y1-.01f,Mathf.Min(s0,s1),Mathf.Max(s0,s1),.006f,false,1);
        }
        // black metal trims top and bottom
        if(alongZ){B(black,Mathf.Min(c,s1+dir*.012f),Mathf.Max(c,s1+dir*.012f),y0-.04f,y0,a,b);B(black,Mathf.Min(c,s1+dir*.012f),Mathf.Max(c,s1+dir*.012f),y1,y1+.04f,a,b);}
        else{B(black,a,b,y0-.04f,y0,Mathf.Min(c,s1+dir*.012f),Mathf.Max(c,s1+dir*.012f));B(black,a,b,y1,y1+.04f,Mathf.Min(c,s1+dir*.012f),Mathf.Max(c,s1+dir*.012f));}
        FinishItems++;
    }
    // ceramic tiles 10 mm on a wall face
    static void Tiles(bool alongZ,float c,float dir,float a,float b,float y0,float y1)
    {
        if(b-a<.05f||y1-y0<.05f)return;
        float f=c+dir*.01f;
        if(alongZ)B(whiteTile,Mathf.Min(c,f),Mathf.Max(c,f),y0,y1,a,b);else B(whiteTile,a,b,y0,y1,Mathf.Min(c,f),Mathf.Max(c,f));
    }
    static void Linings()
    {
        float y0=FFL+.14f,y1=Ceiling-.35f;
        // bowling: the west wall (staff door z -5.4..-4.2), the hall wall (openings z -14..-4, 13.1..14.9)
        Slats(true,-HW+.3f,1f,-24.4f,-5.7f,y0,y1);Slats(true,-HW+.3f,1f,-3.9f,12.6f,y0,y1);
        Slats(true,-.15f,-1f,-24.4f,-14.3f,y0,y1);Slats(true,-.15f,-1f,-3.7f,12.8f,y0,y1);
        // billiards: the hall wall (openings -23.2..-17.2, -8..2), the back wall between the openings and the doors
        Slats(true,10.15f,1f,-16.9f,-8.3f,y0,y1);Slats(true,10.15f,1f,2.3f,14.6f,y0,y1);
        foreach(var (a,b) in new[]{(10.4f,12.7f),(19.3f,21.7f),(28.3f,30.7f),(37.3f,43.1f),(44.9f,53.6f),(55.4f,59.4f)})Slats(false,14.9f,-1f,a,b,y0,y1);
        // offices, cloakrooms, store: white matte paint over the graphite, from the skirting to the ceiling
        foreach(var (a,b) in new[]{(-49.9f,-30.1f),(-29.9f,-22.6f),(-7.4f,-.15f),(40.1f,49.9f)})
        {
            FaceRun(wallWhite,false,15.1f,1f,a,b,FFL+.08f,4.5f,.008f);FaceRun(wallWhite,false,HD-.3f,-1f,a,b,FFL+.08f,4.5f,.008f);
            FaceRun(wallWhite,true,a,1f,15.1f,HD-.3f,FFL+.08f,4.5f,.008f);FaceRun(wallWhite,true,b,-1f,15.1f,HD-.3f,FFL+.08f,4.5f,.008f);
        }
        // toilets: marble linings in BraxChampionsE1Premium.Toilets
    }

    // ================================================================ skirting: 80 mm black metal, 12 mm proud
    // a finish layer on a wall face (paint, tiles, skirting) with the doors of that face cut out (the layer continues over them)
    static void FaceRun(Material mat,bool alongZ,float c,float dir,float a,float b,float y0,float y1,float thick)
    {
        var cuts=new List<(float a,float b,float top)>();
        foreach(var d in doorList)
        {
            bool dz=Mathf.Abs(Mathf.DeltaAngle(d.yaw,90f))<1f||Mathf.Abs(Mathf.DeltaAngle(d.yaw,-90f))<1f;
            if(alongZ&&dz&&Mathf.Abs(d.x-c)<.35f)cuts.Add((d.z-d.w*.5f-.06f,d.z+d.w*.5f+.06f,d.y+d.h+.06f));
            if(!alongZ&&!dz&&Mathf.Abs(d.z-c)<.35f)cuts.Add((d.x-d.w*.5f-.06f,d.x+d.w*.5f+.06f,d.y+d.h+.06f));
        }
        cuts.Sort((p,q)=>p.a.CompareTo(q.a));
        float f=c+dir*thick,c0=Mathf.Min(c,f),c1=Mathf.Max(c,f);
        void Box(float p,float q,float ya,float yb){if(q-p<.02f||yb-ya<.02f)return;if(alongZ)B(mat,c0,c1,ya,yb,p,q);else B(mat,p,q,ya,yb,c0,c1);}
        float u=a;
        foreach(var (ca,cb,top) in cuts){if(cb<=a||ca>=b)continue;Box(u,Mathf.Max(u,ca),y0,y1);Box(Mathf.Max(a,ca),Mathf.Min(b,cb),Mathf.Max(y0,top),y1);u=Mathf.Max(u,cb);}
        Box(u,b,y0,y1);
    }
    static void SkirtRun(bool alongZ,float c,float dir,float a,float b)
    {
        // gaps: the doors on this face
        var cuts=new List<(float,float)>();
        foreach(var d in doorList)
        {
            bool dz=Mathf.Abs(Mathf.DeltaAngle(d.yaw,90f))<1f||Mathf.Abs(Mathf.DeltaAngle(d.yaw,-90f))<1f;   // door across x (in an alongZ wall)
            if(alongZ&&dz&&Mathf.Abs(d.x-c)<.35f)cuts.Add((d.z-d.w*.5f-.07f,d.z+d.w*.5f+.07f));
            if(!alongZ&&!dz&&Mathf.Abs(d.z-c)<.35f)cuts.Add((d.x-d.w*.5f-.07f,d.x+d.w*.5f+.07f));
        }
        cuts.Sort((p,q)=>p.Item1.CompareTo(q.Item1));
        float u=a;
        foreach(var (ca,cb) in cuts){if(cb<a||ca>b)continue;Run(u,Mathf.Max(u,ca));u=Mathf.Max(u,cb);}
        Run(u,b);
        void Run(float p,float q)
        {
            if(q-p<.05f)return;float f=c+dir*.012f;
            if(alongZ)B(black,Mathf.Min(c,f),Mathf.Max(c,f),FFL,FFL+.08f,p,q);else B(black,p,q,FFL,FFL+.08f,Mathf.Min(c,f),Mathf.Max(c,f));
        }
    }
    static void Skirting()
    {
        // bowling hall
        SkirtRun(true,-.15f,-1f,-24.7f,-14f);SkirtRun(true,-.15f,-1f,-4f,13.1f);
        SkirtRun(false,14.9f,-1f,-HW+.3f,LaneX0-.2f);SkirtRun(false,14.9f,-1f,LaneX0+12*PairPitch+.2f,-.15f);
        SkirtRun(false,14.9f,-1f,LaneX0,LaneX0+12*PairPitch);SkirtRun(false,13.1f,1f,LaneX0,LaneX0+12*PairPitch);   // back corridor
        // hall
        SkirtRun(true,.15f,1f,-24.7f,-14f);SkirtRun(true,.15f,1f,-4f,13.1f);SkirtRun(true,.15f,1f,14.9f,StairZ0-.05f);
        SkirtRun(true,9.85f,-1f,-24.7f,-23.2f);SkirtRun(true,9.85f,-1f,-17.2f,-8f);SkirtRun(true,9.85f,-1f,2f,LiftZ0);
        // billiards / bar / lounge
        SkirtRun(true,10.15f,1f,-24.7f,-23.2f);SkirtRun(true,10.15f,1f,-17.2f,-8f);SkirtRun(true,10.15f,1f,2f,14.9f);
        SkirtRun(true,HW-.3f,-1f,-24.7f,14.9f);
        foreach(var (a,b) in new[]{(10.15f,13f),(19f,22f),(28f,31f),(37f,HW-.3f)})SkirtRun(false,14.9f,-1f,a,b);
        // back rooms (z 15.1..24.7): front and back walls and both sides of every room (toilets are tiled)
        foreach(var (a,b) in new[]{(-HW+.3f,-50.1f),(-49.9f,-30.1f),(-29.9f,-22.6f),(-7.4f,-.15f),(10.15f,39.9f),(40.1f,49.9f),(50.1f,HW-.3f)})
        {
            SkirtRun(false,15.1f,1f,a,b);SkirtRun(false,HD-.3f,-1f,a,b);
            SkirtRun(true,a,1f,15.1f,HD-.3f);SkirtRun(true,b,-1f,15.1f,HD-.3f);
        }
    }

    // ================================================================ door frames: black steel, 50 mm faces, 20 mm proud
    static void DoorFrames()
    {
        foreach(var d in doorList)
        {
            if(d.glass||d.label.Contains("kabina"))continue;          // glass doors: in the glazing frames; cubicles: the lacquer panels frame them
            float depth=d.label.Contains("kabina")?.08f:.34f,hw=d.w*.5f,top=d.h;
            var rot=Quaternion.Euler(0,d.yaw,0);var o=new Vector3(d.x,d.y,d.z);
            void Bx(float x0,float x1,float y0,float y1,float z0,float z1)
            {
                var c=o+rot*new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f);
                m.Box(black,c,new Vector3(x1-x0,y1-y0,z1-z0),rot);
            }
            Bx(-hw-.05f,-hw+.008f,0f,top+.05f,-depth*.5f,depth*.5f);
            Bx(hw-.008f,hw+.05f,0f,top+.05f,-depth*.5f,depth*.5f);
            Bx(-hw-.05f,hw+.05f,top-.008f,top+.05f,-depth*.5f,depth*.5f);
            FinishItems++;
        }
    }

    // ================================================================ ceilings: fittings on a grid
    static void Downlight(float x,float y,float z)
    {
        m.Cylinder(fixture,new Vector3(x,y-.015f,z),.095f,.015f,24,Quaternion.identity,false,true);
        m.Cylinder(lampWarm,new Vector3(x,y-.021f,z),.07f,.006f,24,Quaternion.identity,false,true);
    }
    static void Panel(float x,float y,float z)
    {
        B(fixture,x-.31f,x+.31f,y-.012f,y,z-.31f,z+.31f);B(lampWarm,x-.28f,x+.28f,y-.018f,y-.012f,z-.28f,z+.28f);
    }
    static void Diffuser(float x,float y,float z)
    {
        B(fixture,x-.30f,x+.30f,y-.015f,y,z-.30f,z+.30f);
        B(grilleDark,x-.24f,x+.24f,y-.021f,y-.015f,z-.24f,z+.24f);
        for(int i=-2;i<=2;i++){B(fixture,x-.24f,x+.24f,y-.027f,y-.021f,z+i*.09f-.008f,z+i*.09f+.008f);}
    }
    static void Sprinkler(float x,float y,float z)
    {
        m.Cylinder(steelPolished,new Vector3(x,y-.05f,z),.011f,.05f,10,Quaternion.identity,false,false);
        m.Cylinder(steelPolished,new Vector3(x,y-.056f,z),.026f,.006f,14,Quaternion.identity,true,true);
        m.Cylinder(fixture,new Vector3(x,y-.008f,z),.035f,.008f,14,Quaternion.identity,false,true);
    }
    static void Smoke(float x,float y,float z)=>m.Cylinder(fixture,new Vector3(x,y-.05f,z),.055f,.05f,24,Quaternion.identity,false,true,.05f);
    static void Grid(float x0,float x1,float z0,float z1,float y,float light,bool panels,System.Func<float,float,bool> free=null)
    {
        bool Free(float x,float z)=>free==null||free(x,z);
        for(float x=x0+light*.5f;x<x1;x+=light)for(float z=z0+light*.5f;z<z1;z+=light)if(Free(x,z)){if(panels)Panel(x,y,z);else Downlight(x,y,z);FinishItems++;}
        float step=light*Mathf.Max(2f,Mathf.Round(6f/light));                       // diffusers half way between the lights
        for(float x=x0+light*2f;x<x1-.5f;x+=step)for(float z=z0+light*2f;z<z1-.5f;z+=step)if(Free(x,z))Diffuser(x,y,z);
        for(float x=x0+1.6f;x<x1;x+=3.2f)for(float z=z0+1.6f;z<z1;z+=3.2f)if(Free(x+.45f,z-.45f))Sprinkler(x+.45f,y,z-.45f);
        for(float x=x0+3.5f;x<x1;x+=7f)for(float z=z0+3.5f;z<z1;z+=7f)if(Free(x-.6f,z+.6f))Smoke(x-.6f,y,z+.6f);
    }
    static void Ceilings()
    {
        float lanes0=LaneX0,lanes1=LaneX0+12*PairPitch;
        // bowling: over the settees and approaches, over the lanes (lane lights along every lane), the side areas
        // owner 10.11 22:30: no rows of light dots on the black - linear light slots (warm LED in a black channel)
        Slots(-HW+.3f,-.15f,-HD+.3f,-14f,Ceiling,2.4f,true);
        for(int l=0;l<Lanes;l++){float x=LaneX(l);B(black,x-.05f,x+.05f,Ceiling-.03f,Ceiling,-11.5f,4.5f);B(lampWarm,x-.03f,x+.03f,Ceiling-.042f,Ceiling-.03f,-11.48f,4.48f);FinishItems++;}
        Slots(-HW+.3f,lanes0,-14f,14.9f,Ceiling,2.6f,false);Slots(lanes1,-.15f,-14f,14.9f,Ceiling,2.6f,false);
        for(float x=lanes0+1.6f;x<lanes1;x+=6f){Sprinkler(x,Ceiling,-3f);Sprinkler(x,Ceiling,3.2f);Smoke(x+3f,Ceiling,0f);}
        // billiards / bar / lounge (pendants over the tables: the grid keeps clear of them)
        System.Func<float,float,bool> clearOfTables=(x,z)=>
        {
            for(int i=0;i<6;i++)for(int j=0;j<3;j++){float cx=15.8f+i*7.6f,cz=-8f+j*8f;if(Mathf.Abs(x-cx)<1.2f&&Mathf.Abs(z-cz)<2.0f)return false;}
            return true;
        };
        Slots(10.15f,HW-.3f,-HD+.3f,14.9f,Ceiling,2.4f,true,clearOfTables);
        // games, the hall (high ceiling: services only), back rooms (white, LED panels)
        Slots(10.15f,39.9f,15.1f,HD-.3f,Ceiling,2.4f,true);
        for(float z=-20f;z<20f;z+=6f){Sprinkler(2.5f,11f,z);Sprinkler(7.5f,11f,z);Smoke(5f,11f,z+3f);}
        foreach(var (a,b) in new[]{(-HW+.3f,-50.1f),(-49.9f,-30.1f),(-29.9f,-22.6f),(-7.4f,-.15f),(40.1f,49.9f),(50.1f,HW-.3f)})
            Grid(a,b,15.1f,HD-.3f,4.5f,2.4f,true);
    }

    // ================================================================ services on the walls
    // exit sign: green lightbox, white running-man / arrow pictogram 12 mm proud, over a door on both faces
    static void ExitSign(Vector3 c,Quaternion rot)
    {
        m.RoundedBox(exitGreen,c,new Vector3(.36f,.15f,.05f),.008f,rot,2);
        var f=rot*Vector3.back;
        void W(float x,float y,float w,float h)=>m.Box(exitWhite,c+rot*new Vector3(x,y,-.031f),new Vector3(w,h,.012f),rot);
        W(-.09f,0f,.05f,.10f);W(-.09f,0f,.03f,.08f);                  // the door
        W(.04f,0f,.14f,.018f);W(.10f,.022f,.03f,.018f);W(.10f,-.022f,.03f,.018f);   // the arrow
        _=f;
    }
    static void Extinguisher(Vector3 wallPoint,Vector3 outN)
    {
        var c=wallPoint+outN*.10f;c.y=FFL+.12f;
        m.Cylinder(fireRed,c,.075f,.40f,20,Quaternion.identity,true,true,.075f);
        m.Cylinder(fireRed,c+Vector3.up*.40f,.075f,.04f,20,Quaternion.identity,true,false,.035f);
        m.Cylinder(black,c+Vector3.up*.44f,.022f,.06f,12);
        m.Tube(black,c+Vector3.up*.47f+outN*.02f,c+Vector3.up*.20f+outN*.09f,.009f,8);
        m.Cylinder(steelPolished,c+Vector3.up*.18f-outN*.06f,.085f,.025f,20,Quaternion.identity,false,false);
        // the sign above: red, white flame pictogram
        var s=wallPoint+outN*.006f+Vector3.up*(FFL+1.55f);var rot=Quaternion.LookRotation(-outN);
        m.Box(fireRed,s,new Vector3(.20f,.20f,.012f),rot);
        m.Box(exitWhite,s+outN*.012f+Vector3.down*.02f,new Vector3(.06f,.09f,.012f),rot);
        m.Box(exitWhite,s+outN*.012f+Vector3.up*.045f,new Vector3(.03f,.04f,.012f),rot);
        FinishItems++;
    }
    static void HoseCabinet(Vector3 wallPoint,Vector3 outN)
    {
        var rot=Quaternion.LookRotation(-outN);var c=wallPoint+outN*.125f;c.y=FFL+1.25f;
        m.RoundedBox(fireRed,c,new Vector3(.72f,.72f,.25f),.012f,rot,2);
        m.Box(clearGlass,c+outN*.129f,new Vector3(.60f,.60f,.008f),rot);
        m.Cylinder(fireRed,c-outN*.06f,.24f,.12f,28,rot*Quaternion.Euler(90,0,0),true,true);
        m.Cylinder(black,c-outN*.02f,.20f,.06f,28,rot*Quaternion.Euler(90,0,0),false,false);
        FinishItems++;
    }
    static void Switch(Vector3 wallPoint,Vector3 outN,float y,bool socket)
    {
        var rot=Quaternion.LookRotation(-outN);var c=wallPoint+outN*.006f;c.y=y;
        m.RoundedBox(fixture,c,new Vector3(.086f,.086f,.012f),.006f,rot,2);
        if(socket){m.Cylinder(grilleDark,c+outN*.006f,.022f,.004f,16,rot*Quaternion.Euler(-90,0,0),true,false);}
        else m.Box(fixture,c+outN*.009f,new Vector3(.05f,.05f,.006f),rot);
    }
    static void Services()
    {
        foreach(var d in doorList)
        {
            var rot=Quaternion.Euler(0,d.yaw,0);var n=rot*Vector3.forward;var along=rot*Vector3.right;
            var o=new Vector3(d.x,d.y,d.z);bool cubicle=d.label.Contains("kabina");
            float off=d.glass?.06f:cubicle?.05f:(Mathf.Abs(d.z-15f)<.05f||Mathf.Abs(d.z-13f)<.05f?.10f:.15f);
            foreach(float s in new[]{-1f,1f})
            {
                if(cubicle&&s>0)continue;
                var face=rot*Quaternion.Euler(0,s>0?0f:180f,0);
                if(d.glass&&s>0)continue;                                                   // glass doors: the sign inside, on the transom
                float lin=s>0&&d.label.StartsWith("E1 toilets")?.012f:0f;                         // the toilets' marble lining (12 mm)
                if(!cubicle)ExitSign(o+Vector3.up*(d.glass?d.h+.10f:d.h+.28f)+n*(s*(off+lin+.025f)),face);
                if(!d.glass&&!cubicle)Switch(o+n*(s*(off+lin))+along*(d.w*.5f+.20f)*(s>0?1f:-1f),n*s,FFL+1.05f,false);
            }
            FinishItems++;
        }
        // the main and side entrances from inside
        // extinguishers by the exits, hose reel cabinets in the halls
        Extinguisher(new Vector3(-HW+.3f,0,-6.4f),Vector3.right);Extinguisher(new Vector3(-.15f,0,-15.0f),Vector3.left);
        Extinguisher(new Vector3(HW-.3f,0,-6.4f),Vector3.left);Extinguisher(new Vector3(10.15f,0,-9.0f),Vector3.right);
        Extinguisher(new Vector3(.15f,0,-20.5f),Vector3.right);Extinguisher(new Vector3(9.85f,0,-24.0f),Vector3.left);
        Extinguisher(new Vector3(10.15f,0,16.0f),Vector3.right);Extinguisher(new Vector3(-37.0f,0,14.9f),Vector3.back);
        Extinguisher(new Vector3(-HW+.3f,0,13.6f),Vector3.right);Extinguisher(new Vector3(41.0f,0,15.1f),Vector3.forward);
        HoseCabinet(new Vector3(-HW+.3f,0,13.9f),Vector3.right);HoseCabinet(new Vector3(HW-.3f,0,8.0f),Vector3.left);
        HoseCabinet(new Vector3(.15f,0,-16.0f),Vector3.right);HoseCabinet(new Vector3(25.0f,0,HD-.3f),Vector3.back);
        // sockets along the walls of the rooms (every 3 m, 30 cm over the floor), double at the desks
        foreach(var (a,b) in new[]{(-49.9f,-30.1f),(-29.9f,-22.6f),(-7.4f,-.15f),(10.15f,39.9f),(40.1f,49.9f)})
            for(float x=a+1.5f;x<b-1f;x+=3f){Switch(new Vector3(x,0,15.1f),Vector3.forward,FFL+.30f,true);Switch(new Vector3(x,0,HD-.3f),Vector3.back,FFL+.30f,true);}
        for(float z=-22f;z<14f;z+=4f)Switch(new Vector3(HW-.3f,0,z),Vector3.left,FFL+.30f,true);
    }

    // ================================================================ plants (Poly Haven potted_plant_01 / 02, real size x K)
    static int plantsPlaced;
    // a Poly Haven CC0 model at real size (x K): on the floor at 'local' or centred on it (wall pieces)
    static GameObject Prop(Transform parent,string rel,Vector3 local,float yaw,bool floor,float colR=0f)
    {
        string path="Assets/Source/Mistrzowie/"+rel;
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!src){Debug.LogWarning("E1 model missing: "+path+" (prepare_arena_lobby_models.py / prepare_hotel_sobczak_models.py)");return null;}
        var go=PrefabUtility.InstantiatePrefab(src) as GameObject;if(!go)return null;
        go.name="E1 | "+System.IO.Path.GetFileNameWithoutExtension(rel)+" | Poly Haven CC0, real size";go.transform.SetParent(parent,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;
        go.transform.localScale=Vector3.one*K*BraxChampionsRealSize.Scale(rel);
        go.transform.localPosition=local*K;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0)
        {
            var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);var target=parent.TransformPoint(local*K);
            go.transform.position+=floor?Vector3.up*(target.y-bb.min.y):target-bb.center;
        }
        if(colR>0f)Col(local.x-colR,local.x+colR,FFL,FFL+.6f,local.z-colR,local.z+colR,"model");
        FinishItems++;return go;
    }
    static void Plant(Transform parent,float x,float z,bool big=true)
    {
        if(Prop(parent,big?"ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf":"ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf",new Vector3(x,FFL,z),(x*37f+z*11f)%360f,true,.3f))plantsPlaced++;
    }
    static void Plants()
    {
        var g=Group(root,"E1 rośliny (Poly Haven CC0)");
        foreach(var (x,z) in new[]{(.9f,-23.9f),(9.1f,-23.9f),(.9f,12.2f),(9.1f,4.0f),(.9f,-2.5f),(9.1f,10.0f)})Plant(g,x,z);       // hall
        for(int k=0;k<7;k++)Plant(g,10.9f+k*7.6f,-24.0f);                                                                     // lounge, by the glass
        foreach(var (x,z) in new[]{(10.9f,-14.6f),(59.0f,-14.6f),(59.0f,14.2f),(10.9f,14.2f)})Plant(g,x,z);                   // billiards corners
        foreach(var (x,z) in new[]{(-59.0f,-24.0f),(-.9f,-24.0f),(-1.0f,-15.5f),(-.9f,10f)})Plant(g,x,z,false);               // bowling
        foreach(var (x,z) in new[]{(49.2f,24.0f),(40.8f,24.0f),(40.8f,15.7f)})Plant(g,x,z,false);                            // offices
    }

    // ================================================================ rooms: offices, cloakrooms, lounge rugs, store, plant rooms
    static void Rooms()
    {
        var g=Group(root,"E1 wyposażenie pomieszczeń (Poly Haven CC0)");
        // offices: meeting table with six chairs, a shelf wall with binders, the status board on a wall screen, clock, pictures
        m.Cylinder(steelPolished,new Vector3(47.5f,FFL,20f),.32f,.02f,32);m.Cylinder(steelPolished,new Vector3(47.5f,FFL+.02f,20f),.06f,.68f,20);
        Oval(lacquer,new Vector3(47.5f,FFL+.70f,20f),.95f,.55f,.04f);Col(46.55f,48.45f,FFL,FFL+.74f,19.45f,20.55f,"stół");
        Chair(46.15f,20f,Vector3.right);Chair(48.85f,20f,Vector3.left);
        foreach(float x in new[]{47.0f,48.0f}){Chair(x,19.05f,Vector3.forward);Chair(x,20.95f,Vector3.back);}
        RB(lacquer,41.3f,44.8f,FFL,FFL+2.0f,24.32f,24.68f,.015f,true,2);
        for(int lv=0;lv<5;lv++)
        {
            float y=FFL+.05f+lv*.39f;RB(lacquer,41.32f,44.78f,y,y+.025f,24.0f,24.32f,.006f,false,2);
            for(int i=0;i<26;i++){float x=41.45f+i*.128f;var mat=(i*7+lv*3)%5==0?red:(i+lv)%3==0?charcoal:black;
                RB(mat,x,x+.08f,y+.025f,y+.33f,24.06f,24.30f,.008f,false,2);}
        }
        RB(black,49.84f,49.9f,FFL+1.15f,FFL+1.95f,18.5f,19.9f,.01f,false,2);
        boardR.Add(Screen(scrGroup,"monitor biuro ściana",scrBoard,new Vector3(49.828f,FFL+1.55f,19.2f),1.3f,.72f,90f));
        Prop(g,"HotelSobczak/wall_clock/wall_clock_1k.gltf",new Vector3(40.13f,FFL+2.3f,20f),90f,false);
        Prop(g,"HotelSobczak/hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",new Vector3(40.13f,FFL+1.6f,22.6f),90f,false);
        Prop(g,"HotelSobczak/hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",new Vector3(40.13f,FFL+1.6f,17.4f),90f,false);
        // cloakrooms: a full-height mirror and a coat rail with hooks on the side walls
        foreach(var (a,b) in new[]{(-29.9f,-22.6f),(-7.4f,-.15f)})
        {
            RB(lacquer,a,a+.02f,FFL+.22f,FFL+2.08f,16.56f,18.04f,.006f,false,2);
            RB(steelPolished,a+.02f,a+.06f,FFL+.24f,FFL+.28f,16.58f,18.02f,.006f,false,2);RB(steelPolished,a+.02f,a+.06f,FFL+2.02f,FFL+2.06f,16.58f,18.02f,.006f,false,2);
            RB(steelPolished,a+.02f,a+.06f,FFL+.28f,FFL+2.02f,16.58f,16.62f,.006f,false,2);RB(steelPolished,a+.02f,a+.06f,FFL+.28f,FFL+2.02f,17.98f,18.02f,.006f,false,2);
            // the wall facing the lockers: walnut slats on black felt either side of the door (as the halls)
            float dx=a<-20f?-26f:-4f;Slats(false,15.108f,1f,a+.25f,dx-.95f,FFL+.14f,3.2f);Slats(false,15.108f,1f,dx+.95f,b-.25f,FFL+.14f,3.2f);
            Slats(true,a+.008f,1f,15.35f,16.45f,FFL+.14f,3.2f);Slats(true,a+.008f,1f,18.15f,23.8f,FFL+.14f,3.2f);   // the mirror wall
            LiveMirror("cloakroom "+(a<-20f?"M":"K"),new Vector3(a+.05f,FFL+1.15f,17.3f),Vector3.right,1.38f,1.76f,512,640,8f,10f);
            m.Tube(steelPolished,new Vector3(b-.06f,FFL+1.75f,16.2f),new Vector3(b-.06f,FFL+1.75f,21.6f),.016f,12);
            for(float z=16.4f;z<21.5f;z+=.45f){m.Tube(steelPolished,new Vector3(b-.008f,FFL+1.75f,z),new Vector3(b-.14f,FFL+1.82f,z),.008f,8);}
            RB(black,b-.03f,b,FFL+1.70f,FFL+1.80f,16.1f,21.7f,.01f,false,2);
            FinishItems++;
        }
        // store: crates on the shelves
        for(int i=0;i<2;i++){float x=-48.5f+i*4.6f;for(int lv=0;lv<3;lv++)for(int c=0;c<4;c++)
            Prop(g,(c+lv)%2==0?"PolyHaven/plastic_crate_02/plastic_crate_02_2k.gltf":"PolyHaven/wooden_crate_02/wooden_crate_02_2k.gltf",new Vector3(x+.45f+c*.9f,.44f+lv*.7f,19.0f+(c%2)*2.5f),c*90f,true);}
        // plant rooms: electrical cabinets with doors, pipes along the walls with valves
        foreach(var (x0,x1,zw,dir) in new[]{(-57.4f,-52.6f,HD-.3f,-1f),(55.5f,59.4f,15.1f,1f)})
        {
            if(x0>0f)for(float x=x0;x+.8f<=x1+.01f;x+=.82f)
            {
                float zb=zw+dir*.40f;RB(hvac,x,x+.80f,FFL,FFL+2.0f,Mathf.Min(zw,zb),Mathf.Max(zw,zb),.01f,true,2);
                float zf=zb+dir*.006f;B(black,x+.39f,x+.41f,FFL+.05f,FFL+1.95f,Mathf.Min(zb,zf),Mathf.Max(zb,zf));
                B(black,x+.30f,x+.33f,FFL+1.0f,FFL+1.2f,Mathf.Min(zb,zf),Mathf.Max(zb,zf));B(black,x+.47f,x+.50f,FFL+1.0f,FFL+1.2f,Mathf.Min(zb,zf),Mathf.Max(zb,zf));
            }
            foreach(float y in new[]{3.2f,3.5f,3.8f}){m.Tube(y<3.4f?fireRed:steelPolished,new Vector3(x0-1.5f,y,zw+dir*.15f),new Vector3(x1+.3f,y,zw+dir*.15f),.05f,14);}
            FinishItems++;
        }
    }

    // ================================================================ outside (the board): lit planter beds along the glass,
    // palms in stone planters before the front
    static void Outside()
    {
        var models=Group(root,"E1 kwiaty przy elewacji (Poly Haven)");
        for(int k=-6;k<6;k++)
        {
            if(!Module(k,out float a,out float b))continue;
            for(float x=a+1.3f;x+1.1f<=b;x+=2.3f)
            {
                if(Mathf.Abs(x-SideDoorW)<2.4f||Mathf.Abs(x-SideDoorE)<2.4f||(x>PortalX0-1.5f&&x<PortalX1+1.5f))continue;
                if(x-1.0f<-HW+.6f)continue;
                BraxChampionsPromenadeFurniture.Planter(m,cols,models,x,-HD-1.25f);
                B(lampWarm,x-.9f,x+.9f,Y0+.47f,Y0+.50f,-HD-1.712f,-HD-1.70f);                                  // light line on the front of the bed
                FinishItems++;
            }
        }
        // palms (the owner's miasto_palmy, as on the promenade) in 1.8 m granite planters
        var src=new[]{BraxMiastoAssety.Model("miasto_palmy","palma"),BraxMiastoAssety.Model("miasto_palmy","palma2")};
        var palms=Group(root,"E1 palmy przed frontem");int n=0;
        foreach(float x in new[]{-46f,-24f,-9f,19f,36f,57f})
        {
            float z=-30.5f;
            B(planterStone,x-.9f,x+.9f,Y0,Y0+.6f,z-.9f,z-.8f);B(planterStone,x-.9f,x+.9f,Y0,Y0+.6f,z+.8f,z+.9f);
            B(planterStone,x-.9f,x-.8f,Y0,Y0+.6f,z-.8f,z+.8f);B(planterStone,x+.8f,x+.9f,Y0,Y0+.6f,z-.8f,z+.8f);
            B(plantSoil,x-.8f,x+.8f,Y0,Y0+.52f,z-.8f,z+.8f);
            Col(x-.9f,x+.9f,Y0,Y0+.6f,z-.9f,z+.9f,"donica palmy");
            var s=src[n%2];n++;
            if(s){var at=root.TransformPoint(new Vector3(x,Y0+.52f,z)*K);var o=BraxMiastoAssety.Postaw(palms,s,"E1 palma "+n,at,n*67f,1f,false,true);
                if(o)BraxChampionsPalmTrunk.OnSpot(o,at);}
            FinishItems++;
        }
    }
}

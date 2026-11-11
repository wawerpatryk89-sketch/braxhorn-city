using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Owner 11.11 12:56 (board "D2 DWORZEC KOLEJOWO–AUTOBUSOWY", docs/zlecenia/plansze): "możesz zacząć budować dworzec
// autobusowy", "zadbaj o szczegóły, o materiały, wszystko ma być wykończone". 13:08-13:09: "to jest dworzec
// kolejowo-autobusowy, więc musisz zaplanować tory, żeby gdzieś za miasto uciekały", "pociąg, który tam startuje i
// wyjeżdża za miasto ... jedzie do innego miasta i znika z naszej gry. Taka sztuczka", "nie sugeruj się tym planem ...
// wybierz najlepszą działkę ... ulic nie trzeba było zmieniać, albo jakiś wiadukt zrób ... może górą ten pociąg potem
// jechać ... zaplanuj". Plan sent 11.11 (D2_plan_tory.png): the block X70-X400 / Z70-Z330 (empty reservations; the metro
// M1 "DWORZEC", tram and buses at its corner), the railway ON A VIADUCT so no street changes: rail +6.5 m, two covered
// platforms 2 x 260 m (+7.26 m) behind the station building, buffer stops at the east end (a terminus: the train starts
// here), west over road X70 on a steel tied-arch bridge (clearance 5.25 m), out of the region onto an embankment, into a
// cutting and a TUNNEL in a green hill ~170 m beyond the region edge, where the train disappears (BraxChampionsD2Pociag).
//
// Board 1:1 (nominal metres = real metres, x K once; local origin = world (235, 135), +x east, +z north, front = -z):
// * BUILDING 80 x 36 m: the MAIN HALL 36 x 36 (1 296 m2) under a barrel vault (crown 12.5 m) with the glazed arch and the
//   clock to the forecourt, travertine pilaster towers, wings 8 m (ground floor 4.5 + first floor) with dark hip roofs,
//   larch slat panels with 3D "KOLEJ" / "AUTOBUSY" and their pictograms, the entrance canopy with 3D "DWORZEC /
//   KOLEJOWO–AUTOBUSOWY"; inside to the board's floor plan (hall + info, rail / bus ticket offices, waiting rooms, WC M/K/♿
//   in both front corners, tech room, store; +1 food court, shops, offices).
// * FORECOURT: one-way loop (in west, out east) round the fountain plaza, raised zebra to the entrance, Kiss&Ride 4, TAXI 6,
//   car park 140 bays (6 accessible) with its own two-way drive, flowers, trees, lamps, benches, the wayfinding pylon.
// * BUS TERMINAL: 8 numbered nose-in stands at a covered island, its own terminal building, separate drives in / out.
// Levels: lanes .16 (= the city sidewalk the street mouths rise to), walks .30 (kerbs 14 cm), lawns .26, floor .32.
// Nothing coplanar of two materials, nothing 1-5 mm apart (paint 12 mm), colliders on everything you walk on or into.
public static partial class BraxChampionsD2
{
    // ---- plan - layout.D2_* and test_d2_20261111.py must match
    public const float CentreX=235f,CentreZ=135f;
    public const float LaneY=.16f,WalkY=.30f,LawnY=.26f,FFL=.32f,RoadEdge=-55f;
    public const float SiteX0=-140f,SiteX1=140f,SiteZ0=-48f,SiteZ1=69f;                    // world x 95..375, z 87..204
    // building
    public const float BW=40f,BD=36f,HallW=18f,Spring=8.0f,Crown=12.5f,Eave=8.0f,Floor1=4.5f,Tower=22.6f;
    // forecourt loop (lane centreline: west arm x -52.5, arcs R 9.5 round (+-43,-22), front lane z -12.5), lane 7 m
    public const float ArmX=58.5f,ArcCX=49f,ArcCZ=-22f,ArcR=9.5f,LaneHalf=3.5f,FrontZ=-12.5f,LayBy=2.5f;
    public const float KissX0=-40f,KissX1=-16f,TaxiX0=12f,TaxiX1=45f,TableX=6f,TableRamp=3f;
    // car park
    public const float ParkX0=-138f,ParkX1=-84f,ParkZ0=-42f,ParkZ1=30f,ParkDriveX0=-126.5f,ParkDriveX1=-119.5f,ParkWalkX=-88.5f;
    // bus terminal
    public const float BusX0=66f,BusX1=138f,BusZ0=-46f,BusZ1=8f,IslandZ1=14f,IslandX1=118f,BusInX0=109f,BusInX1=116f,BusOutX0=125f,BusOutX1=132f;
    public const float TermX0=118f,TermX1=138f,TermZ0=8f,TermZ1=34f;
    public const int Stands=8;public const float StandX0=70f,StandPitch=5.5f;
    // street mouths across the Z70 north sidewalk (layout.D2_MOUTHS_X, world x = local + 235)
    public static readonly (string id,float x0,float x1)[] MouthList={("D2_in",-62f,-55f),("D2_out",55f,62f),("D2_park",ParkDriveX0,ParkDriveX1),
        ("D2_bus_in",BusInX0,BusInX1),("D2_bus_out",BusOutX0,BusOutX1)};
    // railway (station deck x -130..136 local, platforms -130..130; the line runs west along z 51.5 local = 186.5 world)
    public const float RailTop=6.5f,PlatformTop=7.26f,DeckTop=5.85f,DeckSoffit=5.25f,Track1Z=49.25f,Track2Z=53.75f,LineZ=51.5f;
    public const float PlatformEdge=1.725f,P1Z0=39.0f,P2Z1=64.0f,PlatX0=-130f,PlatX1=130f,BufferX=133.5f,DeckEndX=136f;
    public const float DeckZ0=38.6f,DeckZ1=64.4f,LineDeckHalf=6.0f;
    public static float P1Z1=>Track1Z-PlatformEdge;      // 47.525
    public static float P2Z0=>Track2Z+PlatformEdge;      // 55.475
    // the bridge over X70 (world x 49..93) and the line beyond the region (world x < 0 = local < -235)
    public const float BridgeX0=-186f,BridgeX1=-142f,RegionEdgeX=-235f,ViaductEndX=-295f;
    // the hill with the tunnel (WORLD nominal, BraxChampionsHorizon.RailHill builds its land): centre, radius, height
    public const float HillWX=-330f,HillWZ=CentreZ+LineZ,HillRX=250f,HillRZ=400f,HillH=44f,PortalWX=-185f,Formation=RailTop-.6f,CutHalf=6.5f;
    public const float TunnelEndWX=-310f;

    const string Out="Assets/PlayableGenerated/Mistrzowie",Folder=Out+"/Meshes/D2";
    static float K=>BraxClaudeOsiedle.K;
    public static int Doors,Colliders,Signs,Bays,AccessibleBays,Benches,Lamps,Trees,Seats,Sleepers,Piers,Hangers,Lifts,Screens;

    static Transform root,cols;
    static BraxChampionsMeshBatch m;

    // ================================================================ materials
    static Material asphalt,walk,lawn,kerb,granite,graniteDark,travertine,navy,larch,larchSoffit,roofGraphite,glass,glassDark,
        frame,steel,steelNavy,concrete,concreteDark,ballast,sleeper,railHead,railWeb,paintWhite,paintYellow,paintBlue,tactile,
        led,ledWarm,whiteLetters,water,waterJet,hallFloor,plaster,ceilWhite,tunnelDark,black,rubber,signBlue,signFace,clockFace,
        stainless,bufferRed,bufferYellow;

    static Material S(string id,Color c,float r,float met,Color? e=null,bool t=false)=>BraxChampionsHarbour.Surface("d2_"+id,c,r,met,e,t);
    static Material Tex(string id,string slug,float metres,Color tint)=>BraxChampionsFoundation.SourceMaterial("d2_"+id,slug,metres,tint);
    static Material Inside(Material x,float a){BraxChampionsInteriorLight.Apply(x,a);return x;}
    // Board materials (Poly Haven / ambientCG, PBR): facade "stone_tiles_001" -> vein-cut travertine slabs (Travertine009, the
    // hotel's podium stone: Poly Haven has no stone_tiles_001), cladding "metal_panels_02" -> blue_metal_plate (navy panels),
    // slats and soffits "wood_planks_03" -> teak_veneer (warm larch-like boards), interiors "granite_tile_03", roads
    // "clean_asphalt", walks "concrete_pavement_02"; the roofs dark graphite standing-seam metal (board front picture).
    static void Materials()
    {
        if(!BraxChampionsHarbour.Steel)BraxChampionsHarbour.Materials();
        if(!BraxChampionsPromenadeFurniture.Teak)BraxChampionsPromenadeFurniture.Materials();
        asphalt=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/asphalt.mat");
        if(!asphalt)asphalt=Tex("asphalt","clean_asphalt",2.1f,Color.white);
        walk=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/paving.mat");
        if(!walk)walk=Tex("city_paving","concrete_pavement_02",1.8f,Color.white);
        lawn=AssetDatabase.LoadAssetAtPath<Material>(Out+"/Materials/grass.mat");
        if(!lawn)lawn=BraxChampionsFoundation.LibraryAcg("d2_lawn","Grass004",Color.white,1.4f);
        kerb=BraxChampionsPromenadeFurniture.Kerb;
        granite=Tex("plaza_granite","granite_tile_03",2.4f,new Color(1.10f,1.16f,1.26f));                  // light grey granite slabs
        graniteDark=Tex("granite_dark","granite_tile_03",1.6f,new Color(.55f,.58f,.64f));
        travertine=BraxChampionsFoundation.LibraryTravertine("d2_facade_travertine",new Color(1.02f,1.0f,.97f),2.4f);
        navy=Tex("navy_metal_panels","blue_metal_plate",2.4f,new Color(.62f,.70f,.86f));
        larch=Tex("larch_slats","teak_veneer",1.4f,new Color(1.10f,.92f,.78f));
        larchSoffit=Tex("larch_soffit","teak_veneer",1.8f,new Color(1.05f,.88f,.74f));
        roofGraphite=BraxChampionsFoundation.LibraryMetal("d2_roof_graphite_metal",new Color(.20f,.215f,.24f));
        glass=S("glass_clear",new Color(.66f,.76f,.82f,.26f),.04f,0f,null,true);
        glassDark=S("glass_facade",new Color(.18f,.24f,.29f,.42f),.04f,.1f,null,true);
        // everything that holds glass is BLACK (owner 11.11 14:57: "zakaz szarych listew na szkle" - grey blends with the
        // glass): satin black anodised aluminium (ambientCG Metal048A brushed, tinted black) for frames, mullions, posts,
        // handrail caps, shoes, door frames and handles, lift frames
        frame=BraxChampionsFoundation.LibraryMetal("d2_frame_black_anodised",new Color(.075f,.075f,.085f));
        steel=BraxChampionsHarbour.Steel;
        steelNavy=S("steel_navy_paint",new Color(.10f,.16f,.30f),.38f,.55f);
        concrete=Tex("fair_faced_concrete","concrete_wall_009",3.0f,new Color(.97f,1.0f,1.06f));   // clean formwork concrete (viaduct, piers, slabs)
        concreteDark=Tex("concrete_weathered","concrete_wall_008",3.0f,new Color(.82f,.86f,.92f));
        ballast=Tex("track_ballast","gravel_embedded_concrete_02",.9f,new Color(.80f,.80f,.82f));
        sleeper=S("sleeper_concrete",new Color(.62f,.62f,.60f),.85f,0f);
        railHead=S("rail_head_bright",new Color(.70f,.72f,.75f),.25f,.95f);
        railWeb=S("rail_web_rust",new Color(.30f,.22f,.18f),.80f,.35f);
        paintWhite=S("paint_white",new Color(.93f,.93f,.91f),.55f,0f);
        paintYellow=S("paint_yellow",new Color(.96f,.78f,.10f),.55f,0f);
        paintBlue=S("paint_accessible_blue",new Color(.07f,.29f,.66f),.6f,0f);
        tactile=S("tactile_yellow",new Color(.95f,.75f,.08f),.7f,0f);
        led=S("led_cool",Color.white,.3f,0f,new Color(1.15f,1.15f,1.20f));
        ledWarm=S("led_warm",new Color(1f,.95f,.86f),.3f,0f,new Color(1.30f,1.12f,.88f));
        whiteLetters=S("letters_white_backlit",new Color(.96f,.96f,.97f),.3f,0f,new Color(.85f,.85f,.88f));
        water=S("fountain_water",new Color(.22f,.40f,.46f,.62f),.03f,.1f,null,true);
        waterJet=S("fountain_jet",new Color(.88f,.94f,.97f,.24f),.05f,0f,new Color(.22f,.24f,.26f),true);
        hallFloor=Inside(Tex("hall_granite","granite_tile_03",1.2f,new Color(1.08f,1.12f,1.20f)),.85f);
        // interior partitions: smooth warm-white emulsion on skimmed plaster (the stucco texture read as grey concrete)
        plaster=Inside(S("wall_paint_warm_white",new Color(.91f,.90f,.87f),.82f,0f),BraxChampionsInteriorLight.Walls);
        // smooth painted plasterboard (the stucco texture read as raw concrete on a ceiling - owner 11.11 15:29)
        ceilWhite=Inside(S("ceiling_board_white",new Color(.93f,.93f,.92f),.88f,0f),BraxChampionsInteriorLight.Ceilings);
        tunnelDark=S("tunnel_soot",new Color(.045f,.045f,.05f),.95f,0f);
        black=S("black_matte",new Color(.03f,.03f,.035f),.6f,0f);
        rubber=S("rubber_black",new Color(.05f,.05f,.055f),.85f,0f);
        signBlue=S("sign_navy",new Color(.06f,.14f,.34f),.35f,.2f,new Color(.04f,.08f,.20f));
        signFace=S("sign_white_face",new Color(.97f,.97f,.98f),.3f,0f,new Color(.70f,.70f,.72f));
        clockFace=S("clock_face_white",new Color(.97f,.97f,.96f),.25f,0f,new Color(.80f,.80f,.78f));
        stainless=S("stainless_brushed",new Color(.78f,.80f,.83f),.28f,.88f);
        bufferRed=S("buffer_red",new Color(.78f,.08f,.08f),.45f,.3f);
        bufferYellow=S("buffer_yellow",new Color(.95f,.76f,.08f),.45f,.3f);
        BuildingMaterials();
    }

    // ================================================================ helpers (nominal, D2-local)
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
    static void Q(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)m.Quad(mat,d,c,b,a);else m.Quad(mat,a,b,c,d);
    }
    static void T(Material mat,Vector3 a,Vector3 b,Vector3 c,Vector3 n)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0)m.Triangle(mat,a,c,b);else m.Triangle(mat,a,b,c);
    }
    // flat horizontal rectangle facing up
    static void Flat(Material mat,float x0,float x1,float z0,float z1,float y)
    {
        if(x1-x0<1e-4f||z1-z0<1e-4f)return;
        Q(mat,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.up);
    }
    // walkable ground: the surface plus a thin collider slab under it
    static void Ground(Material mat,float x0,float x1,float z0,float z1,float y,string name="teren")
    {
        Flat(mat,x0,x1,z0,z1,y);
        if(x1-x0>.05f&&z1-z0>.05f)Col(x0,x1,y-.4f,y,z0,z1,name);
    }
    static void RB(Material mat,float x0,float x1,float y0,float y1,float z0,float z1,float r,bool col=false,int seg=4)
    {
        if(x1-x0<1e-4f||y1-y0<1e-4f||z1-z0<1e-4f)return;
        m.RoundedBoxMinMax(mat,x0,x1,y0,y1,z0,z1,r,seg);
        if(col)Col(x0,x1,y0,y1,z0,z1);
    }
    // box between two points in plan (a -> b), width w, y0..y1 (kerbs, rails, beams along any direction)
    static void Bar(Material mat,Vector2 a,Vector2 b,float w,float y0,float y1,bool col=false,string name="kolizja")
    {
        var d=b-a;float len=d.magnitude;if(len<1e-4f)return;
        float yaw=Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg;
        var c=new Vector3((a.x+b.x)*.5f,(y0+y1)*.5f,(a.y+b.y)*.5f);
        m.Box(mat,c,new Vector3(w,y1-y0,len),Quaternion.Euler(0,yaw,0));
        if(col){BraxChampionsMeshBatch.Collider(cols,name,c,new Vector3(w,y1-y0,len),yaw);Colliders++;}
    }
    // a 3D bar between two arbitrary points (square section s): arch ribs, hangers, bracing
    static void Beam(Material mat,Vector3 a,Vector3 b,float w,float h)
    {
        var d=b-a;float len=d.magnitude;if(len<1e-4f)return;
        var rot=Quaternion.LookRotation(d/len,Mathf.Abs(d.normalized.y)>.99f?Vector3.forward:Vector3.up);
        m.Box(mat,(a+b)*.5f,new Vector3(w,h,len),rot);
    }
    // Glass balustrade (owner 11.11 14:57/14:58): nothing grey at any glass and the glass divided - separate toughened panes
    // (max ~1.4 m) between slim black posts (40 x 50, the glass 8 mm deep in their slots), a black handrail cap over the top
    // edge, a black base shoe. a..b is the line the glass stands on (sloping on stairs), h its height over that line;
    // first/last: posts at the ends (off where another run already has a post at that corner).
    static void GlassRail(Vector3 a,Vector3 b,float h,float pane=1.4f,bool first=true,bool last=true)
    {
        var d=b-a;var flat=new Vector3(d.x,0f,d.z);float L=flat.magnitude;if(L<.05f)return;
        var dir=flat/L;var n=new Vector3(-dir.z,0f,dir.x);var rot=Quaternion.LookRotation(n,Vector3.up);
        int N=Mathf.Max(1,Mathf.CeilToInt(L/pane-.02f));
        Vector3 At(float t)=>a+d*t;
        float inset=.012f/L;
        for(int i=0;i<N;i++)
        {
            float t0=(float)i/N+inset,t1=(float)(i+1)/N-inset;
            var p0=At(t0)+Vector3.up*.03f;var p1=At(t1)+Vector3.up*.03f;var p2=At(t1)+Vector3.up*(h-.02f);var p3=At(t0)+Vector3.up*(h-.02f);
            Q(glass,p0,p1,p2,p3,n);Q(glass,p0,p1,p2,p3,-n);
        }
        for(int i=0;i<=N;i++)
        {
            if((i==0&&!first)||(i==N&&!last))continue;
            var c=At((float)i/N);
            m.Box(frame,c+Vector3.up*((h-.005f)*.5f),new Vector3(.04f,h-.005f,.05f),rot);
        }
        var u=d.normalized*.025f;var ue=d.normalized*.01f;
        Beam(frame,a-u+Vector3.up*h,b+u+Vector3.up*h,.055f,.07f);            // handrail cap
        Beam(frame,a-ue+Vector3.up*.03f,b+ue+Vector3.up*.03f,.06f,.10f);     // base shoe (1 cm past the ends: never flush with a beam end)
    }
    static Transform Group(Transform parent,string name){var g=new GameObject(name).transform;g.SetParent(parent,false);return g;}
    static void Flush(string name,Transform parent=null){m.Build(parent??root,name,Folder,false);m=new BraxChampionsMeshBatch();}
    static void Seat(float x,float surface,float z,Vector3 face,string label)
    {
        var g=new GameObject("Siedzisko "+label);g.transform.SetParent(cols,false);
        g.transform.localPosition=new Vector3(x,surface,z)*K;g.transform.localRotation=Quaternion.LookRotation(face);
        var s=g.AddComponent<BraxSeat>();s.label=label;s.surfaceY=root.TransformPoint(new Vector3(x,surface,z)*K).y;
        Seats++;
    }
    // a model from Assets/Source/Mistrzowie (Poly Haven, real size x K), base on the floor at local p
    static GameObject Prop(Transform parent,string rel,Vector3 local,float yaw,float colR=0f)
    {
        string path="Assets/Source/Mistrzowie/"+rel;
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!src){Debug.LogWarning("D2 model missing: "+path);return null;}
        var go=PrefabUtility.InstantiatePrefab(src) as GameObject;if(!go)return null;
        go.name="D2 | "+System.IO.Path.GetFileNameWithoutExtension(rel)+" | Poly Haven CC0, real size";go.transform.SetParent(parent,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;
        go.transform.localScale=Vector3.one*K*BraxChampionsRealSize.Scale(rel);
        go.transform.localPosition=local*K;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0)
        {
            var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);var target=parent.TransformPoint(local*K);
            go.transform.position+=Vector3.up*(target.y-bb.min.y);
        }
        if(colR>0f)Col(local.x-colR,local.x+colR,local.y,local.y+1.2f,local.z-colR,local.z+colR,"model");
        return go;
    }
    // city street tree (the same drzewo_verdant as every street of the region), trunk collider
    static GameObject treeModel;static Transform treeGroup;
    static void Tree(float x,float z,float y=LawnY)
    {
        if(!treeGroup)treeGroup=Group(root,"D2 drzewa (drzewo_verdant, jak ulice rejonu)");
        if(!treeModel)treeModel=BraxRegion.Model("drzewo_verdant");
        if(treeModel)
        {
            var at=root.TransformPoint(new Vector3(x,y,z)*K);
            BraxRegion.Put(treeGroup,treeModel,"D2 drzewo "+x.ToString("0")+","+z.ToString("0"),at,(x*31f+z*17f)%360f,1f,false,6.5f*K);
        }
        Col(x-.3f,x+.3f,y,y+3.5f,z-.3f,z+.3f,"pień");Trees++;
    }

    // ================================================================ GENERATE
    public static int Generate(string outFolder)
    {
        System.IO.Directory.CreateDirectory(Folder);
        Doors=Colliders=Signs=Bays=AccessibleBays=Benches=Lamps=Trees=Seats=Sleepers=Piers=Hangers=Lifts=Screens=0;
        treeGroup=null;treeModel=null;
        Materials();
        var go=new GameObject("D2 | DWORZEC KOLEJOWO-AUTOBUSOWY | hala, perony 2x260 m na wiadukcie, terminal 8 stanowisk, parking 140 (owner 11.11 board + plan torów)");
        root=go.transform;root.position=new Vector3(CentreX,0,CentreZ)*K;
        cols=Group(root,"kolizje");
        m=new BraxChampionsMeshBatch();
        Site();Flush("D2 site | loop, plaza, car park, walks, lawns, kerbs, markings");
        SiteFurniture();Flush("D2 site | fountain, beds, lamps, benches, bollards, pylon, signs");
        Building();Flush("D2 building | shell, arch hall, wings, roofs, canopy");
        BuildingSigns();Flush("D2 building | 3D letters DWORZEC KOLEJOWO-AUTOBUSOWY, KOLEJ, AUTOBUSY");
        Interior();Flush("D2 building | interior: hall, ticket offices, waiting rooms, WC, +1");
        StationDeck();Flush("D2 railway | station deck, piers, platforms");
        Canopies();Flush("D2 railway | platform canopies, catenary");
        Track();Flush("D2 railway | track: ballast, sleepers, rails, buffer stops");
        Access();Flush("D2 railway | stairs, escalators, lift shafts, passage under the tracks");
        Bridge();Flush("D2 railway | steel tied-arch bridge over X70");
        LineWest();Flush("D2 railway | viaduct to the region edge, embankment, cutting, tunnel portal");
        BusTerminal();Flush("D2 bus terminal | island, canopy, stands, terminal building");
        Clock();Train();LiveScreens();
        Debug.Log("D2_DWORZEC platforms=2x"+(PlatX1-PlatX0)+"m railTop="+RailTop+" bridgeSpan="+(BridgeX1-BridgeX0)+" carBays="+Bays+" accessible="+AccessibleBays+
                  " busStands="+Stands+" doors="+Doors+" colliders="+Colliders+" signs="+Signs+" trees="+Trees+" lamps="+Lamps+" benches="+Benches+
                  " seats="+Seats+" sleepers="+Sleepers+" piers="+Piers+" hangers="+Hangers+" lifts="+Lifts+" screens="+Screens+" portalWorldX="+PortalWX);
        return 1;
    }

    // ================================================================ SITE
    // lane geometry: centreline polyline of the loop (in-arm north, west arc, front lane east, east arc, out-arm south)
    static List<Vector2> LoopLine(int arcSeg=12)
    {
        var p=new List<Vector2>{new Vector2(-ArmX,SiteZ0)};
        for(int i=0;i<=arcSeg;i++){float a=Mathf.PI-(Mathf.PI*.5f)*i/arcSeg;p.Add(new Vector2(-ArcCX+ArcR*Mathf.Cos(a),ArcCZ+ArcR*Mathf.Sin(a)));}
        for(int i=0;i<=arcSeg;i++){float a=Mathf.PI*.5f-(Mathf.PI*.5f)*i/arcSeg;p.Add(new Vector2(ArcCX+ArcR*Mathf.Cos(a),ArcCZ+ArcR*Mathf.Sin(a)));}
        p.Add(new Vector2(ArmX,SiteZ0));
        return p;
    }
    // offset of a polyline (miter, left = +n of the direction (d.y,-d.x) rotated: here n = (-d.y, d.x))
    static List<Vector2> Offset(List<Vector2> p,float o)
    {
        var r=new List<Vector2>();
        for(int i=0;i<p.Count;i++)
        {
            Vector2 d0=i>0?(p[i]-p[i-1]).normalized:(p[1]-p[0]).normalized,d1=i<p.Count-1?(p[i+1]-p[i]).normalized:d0;
            Vector2 n0=new Vector2(-d0.y,d0.x),n1=new Vector2(-d1.y,d1.x),n=(n0+n1).normalized;
            float c=n.x*n0.x+n.y*n0.y;r.Add(p[i]+n*(o/Mathf.Max(.3f,c)));
        }
        return r;
    }
    static float LaneH(float x)  // raised zebra table on the front lane (x -6..6 at walk level, ramps 3 m)
    {
        float a=Mathf.Abs(x);
        if(a<=TableX)return WalkY;if(a>=TableX+TableRamp)return LaneY;
        return Mathf.Lerp(WalkY,LaneY,(a-TableX)/TableRamp);
    }
    static void Site()
    {
        // ---- the one-way loop: asphalt between its two edges, kerbs (.18) on both edges inside the asphalt
        var c=LoopLine();var L=Offset(c,LaneHalf);var R=Offset(c,-LaneHalf);      // L: outer (west/north/east), R: inner (island)
        // heading north on the west arm, +n = (-d.y,d.x) = (-1,0): west -> L is the outer edge
        for(int i=0;i+1<c.Count;i++)
        {
            float h0=LaneH(c[i].x),h1=LaneH(c[i+1].x);
            if(Mathf.Abs(c[i].y-FrontZ)<.01f&&Mathf.Abs(c[i+1].y-FrontZ)<.01f)continue;   // straight front lane: below, cut for the table
            Q(asphalt,new Vector3(L[i].x,h0,L[i].y),new Vector3(L[i+1].x,h1,L[i+1].y),new Vector3(R[i+1].x,h1,R[i+1].y),new Vector3(R[i].x,h0,R[i].y),Vector3.up);
        }
        // the straight front lane x -43..43 in pieces: flat, ramp, table (walk level, zebra), ramp, flat
        float zA=FrontZ-LaneHalf,zB=FrontZ+LaneHalf;
        float[] xs={-ArcCX,-TableX-TableRamp,-TableX,TableX,TableX+TableRamp,ArcCX};
        for(int i=0;i+1<xs.Length;i++)
        {
            float x0=xs[i],x1=xs[i+1],y0=LaneH(x0),y1=LaneH(x1);
            Q(asphalt,new Vector3(x0,y0,zA),new Vector3(x1,y1,zA),new Vector3(x1,y1,zB),new Vector3(x0,y0,zB),Vector3.up);
        }
        Col(-ArcCX,ArcCX,LaneY-.5f,LaneY,zA,zB,"pętla jezdnia");
        foreach(float s in new[]{-1f,1f})
        {
            Col(s<0?-ArmX-LaneHalf:ArmX-LaneHalf,s<0?-ArmX+LaneHalf:ArmX+LaneHalf,LaneY-.5f,LaneY,SiteZ0,ArcCZ,"pętla ramię");
            Col(s<0?-ArcCX-ArcR-LaneHalf:ArcCX,s<0?-ArcCX:ArcCX+ArcR+LaneHalf,LaneY-.5f,LaneY,ArcCZ,zB,"pętla łuk");
        }
        Col(-TableX,TableX,LaneY-.5f,WalkY,zA,zB,"stół przejścia");
        // kerbs along both edges (not across the table and its ramps: there the lane rises to the walks)
        Kerbs(L,true);Kerbs(R,false);
        // ---- lay-bys on the building side of the front lane: Kiss&Ride 4 (west), TAXI 6 (east)
        foreach(var (x0,x1) in new[]{(KissX0,KissX1),(TaxiX0,TaxiX1)})
        {
            Ground(asphalt,x0,x1,zB,zB+LayBy,LaneY,"zatoka");
            B(kerb,x0,x1,LaneY-.12f,WalkY,zB+LayBy-.18f,zB+LayBy,true);              // kerb at the back of the bay (on the asphalt)
            // 45 degree ends: triangles of asphalt and the kerb on the diagonal
            foreach(float s in new[]{-1f,1f})
            {
                float xe=s<0?x0:x1;
                T(asphalt,new Vector3(xe,LaneY,zB),new Vector3(xe+s*LayBy,LaneY,zB),new Vector3(xe,LaneY,zB+LayBy),Vector3.up);
                Bar(kerb,new Vector2(xe+s*(LayBy-.30f),zB+.17f),new Vector2(xe,zB+LayBy-.13f),.18f,LaneY-.12f,WalkY,true,"krawężnik zatoki");
            }
        }
        // ---- walks: front sidewalk from the lane / lay-bys to the building (z .. 0), x -56..56; corners outside the arcs
        float[] fx={-ArmX-LaneHalf,KissX0-LayBy,KissX0,KissX1,KissX1+LayBy,TaxiX0-LayBy,TaxiX0,TaxiX1,TaxiX1+LayBy,ArmX+LaneHalf};
        for(int i=0;i+1<fx.Length;i++)
        {
            float x0=fx[i],x1=fx[i+1];bool bay=(x0>=KissX0&&x1<=KissX1)||(x0>=TaxiX0&&x1<=TaxiX1);
            bool taper=!bay&&((x0>=KissX0-LayBy&&x1<=KissX0)||(x0>=KissX1&&x1<=KissX1+LayBy)||(x0>=TaxiX0-LayBy&&x1<=TaxiX0)||(x0>=TaxiX1&&x1<=TaxiX1+LayBy));
            if(bay){Ground(walk,x0,x1,zB+LayBy,0f,WalkY,"chodnik przed dworcem");continue;}
            if(taper)
            {
                // the walk above the diagonal: a quad from the lane edge (or the bay end) to the building
                bool west=x1<=KissX0+.01f||Mathf.Abs(x1-TaxiX0)<.01f;           // taper before a bay: diagonal rises toward x1
                Vector3 a=new Vector3(x0,WalkY,west?zB:zB+LayBy),b=new Vector3(x1,WalkY,west?zB+LayBy:zB);
                Q(walk,a,b,new Vector3(x1,WalkY,0f),new Vector3(x0,WalkY,0f),Vector3.up);
                Col(x0,x1,WalkY-.4f,WalkY,zB,0f,"chodnik");
                continue;
            }
            if(x0<-ArcCX){Ground(walk,-ArcCX,x1,zB,0f,WalkY,"chodnik");continue;}       // x < -43 / > 43: the corner squares
            if(x1>ArcCX){Ground(walk,x0,ArcCX,zB,0f,WalkY,"chodnik");continue;}
            Ground(walk,x0,x1,zB,0f,WalkY,"chodnik przed dworcem");
        }
        // outer corners beyond the arcs: fans from the corner (+-56, -9) to the outer arc (R 13 round (+-43, -22))
        // (the arc points are the outer edge polyline's own vertices, so the kerb stones and the walk meet exactly)
        foreach(float s in new[]{-1f,1f})
        {
            var apex=new Vector3(s*(ArmX+LaneHalf),WalkY,zB);int i0=s<0?1:14;
            for(int i=i0;i<i0+12;i++)T(walk,apex,new Vector3(L[i].x,WalkY,L[i].y),new Vector3(L[i+1].x,WalkY,L[i+1].y),Vector3.up);
            Col(s<0?-ArmX-LaneHalf:ArcCX,s<0?-ArcCX:ArmX+LaneHalf,WalkY-.4f,WalkY,ArcCZ,zB,"chodnik narożnik");
            Ground(walk,s<0?-ArmX-LaneHalf:ArcCX,s<0?-ArcCX:ArmX+LaneHalf,zB,0f,WalkY,"chodnik narożnik");
        }
        // ---- the fountain plaza: the island inside the loop (rounded north corners = inner arcs, R 6)
        Plaza(R);
        // ---- walks round the building and to the bus island, lawns, car park, bus apron
        Ground(walk,-ArmX-LaneHalf,-BW,0f,DeckZ0-2.1f,WalkY,"chodnik zachód");            // west of the building, up to the arcade
        Ground(walk,BW,BusX0,0f,DeckZ0-2.1f,WalkY,"chodnik wschód");
        Ground(walk,-BW,BW,BD,DeckZ0-2.1f,WalkY,"chodnik za budynkiem");
        Arcade();
        CarPark();
        WestGarden();
        BusApron();
        Lawns();
        Mouths();
        Markings(c);
    }
    // kerb stones along an edge polyline, laid INSIDE the asphalt (toward the lane centre); skipped over the zebra table
    static void Kerbs(List<Vector2> e,bool outer)
    {
        for(int i=0;i+1<e.Count;i++)
        {
            var a=e[i];var b=e[i+1];
            if(Mathf.Abs(a.y-(FrontZ+(outer?LaneHalf:-LaneHalf)))<.05f&&Mathf.Abs(b.y-a.y)<.05f)
            {
                // straight front lane edge: kerb in two pieces outside the table; on the outer edge also not along the lay-bys
                float x0=Mathf.Min(a.x,b.x),x1=Mathf.Max(a.x,b.x);
                var cuts=new List<(float,float)>{(-TableX-TableRamp,TableX+TableRamp)};
                if(outer){cuts.Add((KissX0-LayBy,KissX1+LayBy));cuts.Add((TaxiX0-LayBy,TaxiX1+LayBy));}
                cuts.Sort((p,q)=>p.Item1.CompareTo(q.Item1));
                float x=x0;float zk=a.y+(outer?-.09f:.09f);
                foreach(var (c0,c1) in cuts){if(c0>x)Bar(kerb,new Vector2(x,zk),new Vector2(Mathf.Min(c0,x1),zk),.18f,LaneY-.12f,WalkY,true,"krawężnik");x=Mathf.Max(x,c1);}
                if(x<x1)Bar(kerb,new Vector2(x,zk),new Vector2(x1,zk),.18f,LaneY-.12f,WalkY,true,"krawężnik");
                continue;
            }
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x);   // left normal; L is on the left, so inward = -n for outer
            var inward=outer?n*-1f:n;
            Bar(kerb,a+inward*.09f,b+inward*.09f,.18f,LaneY-.12f,WalkY,true,"krawężnik");
        }
    }
    // ---- fountain plaza: rounded island (south edge on the city sidewalk with a 3 m gentle ramp .16 -> .30)
    static void Plaza(List<Vector2> inner)
    {
        // outline: (-49,-45), the inner edge polyline north of z -45 (west arm, R 6 arc, front lane, arc, east arm), (49,-45)
        var outline=new List<Vector2>{new Vector2(-ArmX+LaneHalf,SiteZ0+3f)};
        foreach(var p in inner)if(p.y>SiteZ0+3f+1e-3f)outline.Add(p);
        outline.Add(new Vector2(ArmX-LaneHalf,SiteZ0+3f));
        var cen=new Vector2(0,-30f);
        // fan from the centre at walk level (convex outline), the granite ring and the fountain basin sit on it
        for(int i=0;i<outline.Count;i++)
        {
            var a=outline[i];var b=outline[(i+1)%outline.Count];
            T(walk,new Vector3(cen.x,WalkY,cen.y),new Vector3(a.x,WalkY,a.y),new Vector3(b.x,WalkY,b.y),Vector3.up);
        }
        Col(-ArmX+LaneHalf,ArmX-LaneHalf,WalkY-.4f,WalkY,SiteZ0+3f,FrontZ-LaneHalf,"plac");
        // ramp band to the city sidewalk (.16 at z -48 -> .30 at z -45), 4.7 %
        float x0=-ArmX+LaneHalf,x1=ArmX-LaneHalf;
        Q(walk,new Vector3(x0,LaneY,SiteZ0),new Vector3(x1,LaneY,SiteZ0),new Vector3(x1,WalkY,SiteZ0+3f),new Vector3(x0,WalkY,SiteZ0+3f),Vector3.up);
        var o=new GameObject("plac rampa | kolizja");o.transform.SetParent(cols,false);
        float ang=-Mathf.Atan2(WalkY-LaneY,3f)*Mathf.Rad2Deg;
        o.transform.localPosition=new Vector3(0,(LaneY+WalkY)*.5f-.2f,SiteZ0+1.5f)*K;o.transform.localRotation=Quaternion.Euler(ang,0,0);
        o.AddComponent<BoxCollider>().size=new Vector3(x1-x0,.4f,3.02f)*K;Colliders++;
    }
    // under the station deck: paved arcade between the piers (x -130..136), lawn at the ends
    static void Arcade()
    {
        Ground(walk,-ArmX-LaneHalf,BusX0,DeckZ0-2.1f,DeckZ1+.6f,WalkY,"pasaż pod torami");
        Ground(walk,PlatX0,-ArmX-LaneHalf,ParkZ1+6f,DeckZ1+.6f,WalkY,"pasaż pod torami zachód");
        Ground(walk,BusX0,TermX1,TermZ1,DeckZ1+.6f,WalkY,"pasaż pod torami wschód");
    }
    static void Lawns()
    {
        // west edge strip, north of the car park, north of the deck, east edge strip, the strips along the city sidewalk
        Flat(lawn,SiteX0,ParkX0,SiteZ0,SiteZ1,LawnY);
        Flat(lawn,ParkX0,PlatX0,ParkZ1,SiteZ1,LawnY);
        Flat(lawn,PlatX0,ParkX1,ParkZ1,ParkZ1+6f,LawnY);
        Flat(lawn,PlatX0,TermX1,DeckZ1+.6f,SiteZ1,LawnY);
        Flat(lawn,TermX1,SiteX1,SiteZ0,SiteZ1,LawnY);
        // along the city sidewalk south of the car park (except its drive) and of the bus apron (except its drives)
        Flat(lawn,ParkX0,ParkDriveX0,SiteZ0,ParkZ0,LawnY);Flat(lawn,ParkDriveX1,ParkX1,SiteZ0,ParkZ0,LawnY);
        Flat(lawn,BusX0,BusInX0,SiteZ0,BusZ0,LawnY);Flat(lawn,BusInX1,BusOutX0,SiteZ0,BusZ0,LawnY);Flat(lawn,BusOutX1,BusX1,SiteZ0,BusZ0,LawnY);
        Flat(lawn,ArmX+LaneHalf,BusX0,SiteZ0,0f,LawnY);                                             // between the out-arm and the apron
        Col(SiteX0,SiteX1,LawnY-.6f,LawnY,SiteZ0,SiteZ1,"trawnik (pod wszystkim)");
        // edging stones along the city sidewalk in front of the lawns (.16 -> .30, 10 cm)
        foreach(var (a,b) in new[]{(SiteX0,ParkDriveX0),(ParkDriveX1,-ArmX-LaneHalf),(ArmX+LaneHalf,BusInX0),(BusInX1,BusOutX0),(BusOutX1,SiteX1)})
            B(kerb,a,b,LaneY-.10f,WalkY,SiteZ0,SiteZ0+.10f,true);
    }
    // west garden between the car park and the loop: lawn with trees, a path from the car park to the front walk
    static void WestGarden()
    {
        float x0=ParkX1,x1=-ArmX-LaneHalf;
        Flat(lawn,x0,x1,SiteZ0,ArcCZ,LawnY);                         // south part (the walk and the arc corner are north of z -22)
        Flat(lawn,x0,x1-4f,ArcCZ,0f,LawnY);
        Ground(walk,x1-4f,x1,ArcCZ,0f,WalkY,"ścieżka parking-dworzec");
        Ground(walk,x0,x1,0f,ParkZ1+6f,WalkY,"plac zachodni");
        foreach(var (x,z) in new[]{(-79f,-40f),(-68f,-36f),(-79f,-28f),(-68f,-24f),(-79f,-14f),(-68f,-10f)})Tree(x,z);
    }
    // ---- the car park: 3 modules of bays 5 | aisle 6.5 | bays 5 across x, rows 60 m along z (24 bays of 2.5 m), cross
    // aisles 6 m at both ends, the two-way drive into the south one; 6 accessible bays 3.6 m at the east row's north end
    // (nearest the station) - 144 - 9 + 6 = 141 bays; a 4.5 m walk along the east side to the station
    static void CarPark()
    {
        Ground(asphalt,ParkX0,ParkWalkX,ParkZ0,ParkZ1,LaneY,"parking");
        Ground(asphalt,ParkDriveX0,ParkDriveX1,SiteZ0,ParkZ0,LaneY,"wjazd na parking");
        Ground(walk,ParkWalkX,ParkX1,ParkZ0,ParkZ1,WalkY,"chodnik parkingu");
        // kerbs round the asphalt (inside it) with the drive's opening; along the drive to the city sidewalk
        B(kerb,ParkX0,ParkDriveX0,LaneY-.12f,WalkY,ParkZ0,ParkZ0+.18f,true);B(kerb,ParkDriveX1,ParkWalkX,LaneY-.12f,WalkY,ParkZ0,ParkZ0+.18f,true);
        B(kerb,ParkX0,ParkWalkX,LaneY-.12f,WalkY,ParkZ1-.18f,ParkZ1,true);
        B(kerb,ParkX0,ParkX0+.18f,LaneY-.12f,WalkY,ParkZ0+.18f,ParkZ1-.18f,true);B(kerb,ParkWalkX-.18f,ParkWalkX,LaneY-.12f,WalkY,ParkZ0+.18f,ParkZ1-.18f,true);
        B(kerb,ParkDriveX0,ParkDriveX0+.18f,LaneY-.12f,WalkY,SiteZ0+.10f,ParkZ0,true);B(kerb,ParkDriveX1-.18f,ParkDriveX1,LaneY-.12f,WalkY,SiteZ0+.10f,ParkZ0,true);
        float rz0=-36f,rz1=24f,y=LaneY+.012f;
        for(int mod=0;mod<3;mod++)
        {
            float mx=ParkX0+mod*16.5f;
            for(int side=0;side<2;side++)
            {
                float x0=side==0?mx:mx+11.5f,x1=x0+5f;bool aisleEast=side==0;     // west row: aisle on its east side
                bool accRow=mod==2&&side==1;
                float z=rz1;int n=0;
                if(accRow)
                {
                    for(int i=0;i<6;i++)
                    {
                        float za=z-3.6f;Flat(paintBlue,x0+.06f,x1-.06f,za+.06f,z-.06f,y);Wheelchair((x0+x1)*.5f,(za+z)*.5f);
                        Flat(paintWhite,x0,x1,za-.06f,za+.06f,y+.002f);z=za;AccessibleBays++;Bays++;
                    }
                }
                while(z-2.5f>=rz0-1e-3f&&n<64)
                {
                    float za=z-2.5f;Flat(paintWhite,x0,x1,za-.06f,za+.06f,y);z=za;Bays++;n++;
                }
                Flat(paintWhite,x0,x1,rz1-.06f,rz1+.06f,y);
                // the bays' end line on the aisle side
                float xe=aisleEast?x1:x0;Flat(paintWhite,xe-.06f,xe+.06f,z,rz1,y);
                // wheel stops at the back of each bay row (rubber, on the asphalt)
            }
            // aisle arrows (two-way: one each way)
            Arrow(new Vector2(mx+8.25f-1.6f,-10f),new Vector2(0,1));Arrow(new Vector2(mx+8.25f+1.6f,8f),new Vector2(0,-1));
        }
        // trees along the walk and at the row ends (beds in the walk)
        for(int i=0;i<5;i++){float z=ParkZ0+6f+i*14f;Tree((ParkWalkX+ParkX1)*.5f,z,WalkY);}
        PSign(ParkDriveX1+1.2f,ParkZ0-1.0f);
    }
    // white wheelchair pictogram in the accessible bay (14 mm over the blue): wheel ring, frame, back
    static void Wheelchair(float x,float z)
    {
        var cc=new Vector3(x+.15f,LaneY+.024f,z-.25f);int n=24;
        for(int i=0;i<n;i++)
        {
            float a0=2*Mathf.PI*i/n,a1=2*Mathf.PI*(i+1)/n;
            Vector3 P(float a,float r)=>cc+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
            Q(paintWhite,P(a0,.40f),P(a1,.40f),P(a1,.50f),P(a0,.50f),Vector3.up);
        }
        Flat(paintWhite,x-.45f,x-.33f,z-.25f,z+.55f,LaneY+.024f);          // back
        Flat(paintWhite,x-.45f,x+.25f,z+.05f,z+.17f,LaneY+.024f);          // seat
        Flat(paintWhite,x+.20f,x+.55f,z+.47f,z+.59f,LaneY+.024f);          // footrest
        var h=new Vector3(x-.39f,LaneY+.024f,z+.85f);
        for(int i=0;i<12;i++){float a0=2*Mathf.PI*i/12,a1=2*Mathf.PI*(i+1)/12;T(paintWhite,h,h+new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0))*.14f,h+new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1))*.14f,Vector3.up);}
    }
    static void PSign(float x,float z)
    {
        m.Cylinder(steel,new Vector3(x,WalkY,z),.05f,2.6f,12,Quaternion.identity,true);
        RB(signBlue,x-.45f,x+.45f,WalkY+2.0f,WalkY+2.9f,z-.04f,z+.04f,.06f);
        BraxChampionsLetters3D.Text(m,signFace,signBlue,"P",new Vector3(x,WalkY+2.18f,z-.045f),180f,.55f,.02f);
        Col(x-.1f,x+.1f,WalkY,WalkY+2.6f,z-.1f,z+.1f,"znak P");Signs++;
    }
    // ---- bus apron (asphalt, its own drives), the island strip and the walk from the island to the arcade
    static void BusApron()
    {
        Ground(asphalt,BusX0,BusX1,BusZ0,BusZ1,LaneY,"plac autobusowy");
        foreach(var (a,b) in new[]{(BusInX0,BusInX1),(BusOutX0,BusOutX1)})Ground(asphalt,a,b,SiteZ0,BusZ0,LaneY,"wjazd autobusów");
        // kerbs (inside the asphalt): west edge (to the planted strip), south (with the drives), east, north = the island face
        B(kerb,BusX0,BusX0+.18f,LaneY-.12f,WalkY,BusZ0,BusZ1,true);B(kerb,BusX1-.18f,BusX1,LaneY-.12f,WalkY,BusZ0,BusZ1,true);
        float x=BusX0+.18f;
        foreach(var (a,b) in new[]{(BusInX0,BusInX1),(BusOutX0,BusOutX1)}){B(kerb,x,a,LaneY-.12f,WalkY,BusZ0,BusZ0+.18f,true);x=b;}
        B(kerb,x,BusX1-.18f,LaneY-.12f,WalkY,BusZ0,BusZ0+.18f,true);
        foreach(var (a,b) in new[]{(BusInX0,BusInX1),(BusOutX0,BusOutX1)})
            foreach(float s in new[]{-1f,1f}){float e=s<0?a:b;B(kerb,s<0?e:e-.18f,s<0?e+.18f:e,LaneY-.12f,WalkY,SiteZ0+.10f,BusZ0,true);}
        // the island (walk level) along the stands, Kassel kerb face (bus kerb 18 cm above the apron -> .34 at the edge)
        Ground(walk,BusX0,IslandX1,BusZ1,IslandZ1,WalkY,"peron autobusowy");
        B(kerb,BusX0,IslandX1-.4f,LaneY-.12f,WalkY,BusZ1-.30f,BusZ1,true);
        Ground(walk,BusX0,TermX0,IslandZ1,TermZ1,WalkY,"dojście do terminalu");
    }
    // street mouths across the Z70 north sidewalk (layout.D2_MOUTHS_X)
    static void Mouths()
    {
        foreach(var (id,x0,x1) in MouthList)
            BraxChampionsDriveMouth.Build(root,new BraxChampionsDriveMouth.Spec{id="D2 "+id+" street mouth",alongX=true,edge=RoadEdge,dir=1f,
                u0=x0,u1=x1,R=3f,depth=SiteZ0-RoadEdge,topEnd=LaneY,unit=K,assetDir=Folder},asphalt,walk,kerb);
    }
    // ---- markings: lane arrows, zebra on the table, lay-by bays, "K+R" and "TAXI" painted, bus stand numbers + bay lines
    static void Markings(List<Vector2> c)
    {
        // zebra (P-10) on the table: the stripes PARALLEL TO THE LANE (x), 0.5 m wide / 0.5 m gaps, laid one after another
        // across the lane (z) - the pedestrian walking north to the entrance steps over them (owner 11.11 15:28: they were
        // along the walking direction); crossing 10 m wide in front of the three doors
        for(int k=0;k<7;k++){float z=FrontZ-LaneHalf+.25f+k*1.0f;Flat(paintWhite,-5f,5f,z,z+.5f,WalkY+.012f);}
        // arrows: in-arm north, front lane east, out-arm south
        Arrow(new Vector2(-ArmX,-38f),new Vector2(0,1));Arrow(new Vector2(-26f,FrontZ),new Vector2(1,0));Arrow(new Vector2(26f,FrontZ),new Vector2(1,0));
        Arrow(new Vector2(ArmX,-32f),new Vector2(0,-1));
        // lay-by bay lines
        for(int i=0;i<=4;i++){float x=KissX0+i*6f;Flat(paintWhite,x-.06f,x+.06f,FrontZ+LaneHalf+.15f,FrontZ+LaneHalf+LayBy-.25f,LaneY+.012f);}
        for(int i=0;i<=6;i++){float x=TaxiX0+i*5.5f;Flat(paintWhite,x-.06f,x+.06f,FrontZ+LaneHalf+.15f,FrontZ+LaneHalf+LayBy-.25f,LaneY+.012f);}
        Painted("K+R",paintWhite,(KissX0+KissX1)*.5f,LaneY+.024f,FrontZ+LaneHalf+.45f,1.4f);
        Painted("TAXI",paintYellow,(TaxiX0+TaxiX1)*.5f,LaneY+.024f,FrontZ+LaneHalf+.45f,1.4f);
        // bus stands: angled bay lines and numbers on the apron
        for(int i=0;i<=Stands;i++){float x=StandX0+i*StandPitch;Flat(paintWhite,x-.07f,x+.07f,BusZ1-13.5f,BusZ1-.4f,LaneY+.012f);}
        Flat(paintYellow,StandX0,StandX0+Stands*StandPitch,BusZ1-13.62f,BusZ1-13.5f,LaneY+.012f);
        for(int i=0;i<Stands;i++)Painted((i+1).ToString(),paintWhite,StandX0+(i+.5f)*StandPitch,LaneY+.024f,BusZ1-12.6f,1.6f);
        Arrow(new Vector2((BusInX0+BusInX1)*.5f,-40f),new Vector2(0,1));Arrow(new Vector2((BusOutX0+BusOutX1)*.5f,-40f),new Vector2(0,-1));
        Arrow(new Vector2(118f,-20f),new Vector2(-1,0));Arrow(new Vector2(90f,-34f),new Vector2(1,0));
        Arrow(new Vector2((ParkDriveX0+ParkDriveX1)*.5f-1.6f,-46f),new Vector2(0,1));
    }
    // text painted flat on the asphalt (3D kit letters laid down, 4 mm), reading from the south (drivers heading north)
    static void Painted(string text,Material mat,float x,float y,float z,float cap)
    {
        var g=Group(root,"D2 napis na jezdni "+text);g.localPosition=new Vector3(x,y,z)*K;g.localRotation=Quaternion.Euler(90f,0f,0f);
        var lm=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(lm,mat,mat,text,Vector3.zero,180f,cap,.004f);
        lm.Build(g,"D2 napis "+text,Folder,false);
    }
    // painted lane arrow (shaft 3 m + head), flat 12 mm over the asphalt; lying flat its plan is drawn with Flat strips
    static void Arrow(Vector2 p,Vector2 d)
    {
        d=d.normalized;var n=new Vector2(-d.y,d.x);float y=LaneY+.012f;
        Vector3 V(Vector2 q)=>new Vector3(q.x,y,q.y);
        var a=p-d*2.0f;var b=p+d*1.0f;
        Q(paintWhite,V(a-n*.08f),V(b-n*.08f),V(b+n*.08f),V(a+n*.08f),Vector3.up);
        T(paintWhite,V(b-n*.45f),V(b+d*1.4f),V(b+n*.45f),Vector3.up);
    }
}

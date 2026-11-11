using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// G1 GALERIA GŁÓWNA + KINO (owner 11.11 16:33, board "G1 GALERIA GŁÓWNA + KINO - karta referencyjna budowy 3D": 170 x 110 x
// 24 m, 2 storeys (parter 6.0, piętro 5.5, technika to 12.5, the facade screen to 24), structural grid 10 x 10 m (17 x 11),
// facade module 5 m (glass 4.8 + spandrel 1.2 per storey), materials M-01..M-10, functions: galeria handlowa, kino
// multiplex (8 halls), food court, rozrywka (kręgle, bilard: owner 11.11 18:26 "W galerii ma być tylko sklepy i kino" - the
// east wing is the BRAXHORN HOME furniture store instead, the bowling is E1's), sklepy (ankry); outside: plac główny >= 8000 m2, 600-800
// parking spaces, taxi / drop-off, deliveries at the back / side; 16:33-16:36 "kino ... funkcjonalne, żeby leciały nasze
// filmy ... recepcja ... wybór repertuaru", "galerię na gotowo ... zadbaj o detale wszystkie"; 15:47 everything functional
// as in life; 14:57 nothing grey at any glass; no gold (the board's brass trims are black anodised here).
//
// Built with the station's builders (this is the same partial class: materials, doors, glass rails, slat ceilings, lifts,
// benches); its own root at the register's plot G1 (world 940, 175), local x east, z NORTH = the front (main entrances,
// the square with the M5 metro entrance and the tram stop TR09 on road Z330). The site is the block X800..X1130 /
// Z70..Z330 without the tower plots T14 / T15 / T16 (layout.G1_SITE_RECTS).
public static partial class BraxChampionsD2
{
    public const float G1CentreX=940f,G1CentreZ=175f;
    // the block interior (local): the city walks end at world x 817 / 1113, z 87 / 313
    public const float G1SiteX0=-123f,G1SiteX1=173f,G1SiteZ0=-88f,G1SiteZ1=138f;
    // the building: 170 x 110, front (north) at z +55; floors; the roof; the facade screen
    public const float G1HW=85f,G1HD=55f,G1F0=.32f,G1F1=6.32f,G1Roof=12.5f,G1Top=24f,G1Slab=5.60f,G1Ceil0=5.0f,G1Ceil1=11.0f;
    // the atrium: the glazed barrel vault (span 20, axis N-S) and the P1 void
    public const float G1VaultR=10f,G1VaultZ0=-40f,G1VaultZ1=50f,G1VoidX=8f;
    // the malls (P0 and P1): central N-S, north E-W, south E-W
    public const float G1MallX=12f,G1NorthMallZ0=28f,G1NorthMallZ1=40f,G1SouthMallZ0=-25f,G1SouthMallZ1=-13f;
    // wings: the cinema (west), the anchor + entertainment (east); the food court (south centre)
    public const float G1CinemaX1=-47f,G1EastX0=47f,G1FoodX=30f,G1FoodZ1=-25f;
    // the towers' plots kept free (local): T15, T14, T16
    public static readonly (float x0,float x1,float z0,float z1)[] G1Towers={(-80f,-30f,80f,130f),(75f,125f,80f,130f),(115f,165f,-35f,35f)};
    // the M5 metro entrance in the square (layout.ENTRANCE_HOLES / ENTRANCE_PATHS, local): no paving in the hole
    public const float G1MetroX0=58.99f,G1MetroX1=66.87f,G1MetroZ0=73.17f,G1MetroZ1=75.80f;
    // street mouths: Z70 north sidewalk (road edge local z -95) and X800 east sidewalk (road edge local x -130)
    public const float G1RoadZ=-95f,G1RoadX=-130f;
    public static readonly (string id,float a,float b)[] G1MouthsZ70={("G1_park_w",-78f,-71f),("G1_park_e",20f,27f),("G1_dostawy",122f,130f)};
    public static readonly (string id,float a,float b)[] G1MouthsX800={("G1_west",0f,7f),("G1_taxi",95f,102f)};
    // car parks: west lot (and its deck), south lot; the delivery yard; the drop-off loop (north-west)
    public const float G1WestX0=-121f,G1WestX1=-89f,G1WestZ0=-86f,G1WestZ1=76f,G1SouthZ0=-86f,G1SouthZ1=-59f,G1SouthX1=80f;
    public const float G1YardX0=85f,G1YardX1=171f,G1YardZ0=-86f,G1YardZ1=-38f;
    public const float G1TaxiX0=-121f,G1TaxiX1=-84f,G1TaxiZ0=84f,G1TaxiZ1=136f;
    public static int G1Bays,G1AccessibleBays,G1Shops,G1Halls,G1CinemaSeats,G1Escalators;

    static Material gDark,gLight,gRed,gStone,gRoof,gRoofLight,gHvac,gVaultGlass,gShopGlass,gCinemaDark,gMallFloor,gMallFloor2,gMallBand,gWhitePanel,gWood,gMarketFloor,gChairLeather;
    static Material gLetterFace,gLetterSide,gStoneTop,gVelvetBlack,gLeatherBlack,gOak,gWallPanels,gWallWarm;
    static void G1Materials()
    {
        // Owner 11.11 15:53 ("Poly Haven, tylko i wyłącznie Poly Haven ... materiałów, obić, wszystkiego") and 17:53 ("drewno to
        // drewno, metal to metal"): every surface a Poly Haven photoscan (colour, normal, roughness) - stone, wood, plaster,
        // fabric, leather; smooth metal (anodised, painted aluminium, stainless), glass, screens and light are clean PBR.
        // M-01 anthracite composite (painted aluminium), M-02 light architectural concrete, M-03 the facade glass, M-04 red
        // accent (painted metal), M-05 trims black anodised, M-06 granite plinth, M-07 paving, M-08 asphalt, M-10 roof
        gDark=S("g1_m01_anthracite_composite",new Color(.17f,.18f,.19f),.38f,.72f);
        gLight=Tex("g1_m02_concrete_light","concrete_wall_009",3.0f,new Color(1.16f,1.15f,1.12f));
        gRed=S("g1_m04_red_accent_painted_metal",new Color(.62f,.05f,.06f),.42f,.55f);
        gStone=graniteDark;
        gRoof=Tex("g1_m10_roof_membrane","concrete_floor_02",3.0f,new Color(.46f,.47f,.49f));
        gRoofLight=Tex("g1_m10_roof_walkway","anti_slip_concrete",1.5f,new Color(.95f,.95f,.96f));
        gHvac=S("g1_m10_hvac_galvanised",new Color(.68f,.70f,.72f),.40f,.85f);
        gVaultGlass=S("g1_vault_glass",new Color(.62f,.74f,.80f,.22f),.03f,.1f,null,true);
        gShopGlass=S("g1_shop_glass",new Color(.70f,.78f,.82f,.14f),.02f,.05f,null,true);
        gCinemaDark=S("g1_black_lacquer",new Color(.035f,.035f,.04f),.35f,0f);
        // the floors: cream limestone slabs in a running bond (Poly Haven marble_01) in the malls, the same stone lighter in
        // the atrium / hall / food court fields, dark granite bands (granite_tile_03), light granite tiles in the market
        gMallFloor=Inside(Tex("g1_mall_floor_limestone","marble_01",3.0f,new Color(1.10f,1.09f,1.10f)),.85f);
        gMallFloor2=Inside(Tex("g1_mall_floor_limestone_light","marble_01",2.4f,new Color(1.24f,1.23f,1.22f)),.85f);
        gMarketFloor=Inside(Tex("g1_market_floor_granite","granite_tile_04",2.0f,new Color(1.55f,1.53f,1.52f)),.85f);
        gMallBand=Inside(Tex("g1_mall_floor_band","granite_tile_03",1.2f,new Color(.40f,.42f,.46f)),.85f);
        // walls and ceilings: white plaster (white_stucco_02); wood: walnut veneer; tops: the light limestone
        gWhitePanel=Inside(Tex("g1_white_plaster","white_stucco_02",2.0f,new Color(1.06f,1.06f,1.05f)),.9f);
        ceilWhite=Inside(Tex("g1_ceiling_plaster","white_stucco_02",2.4f,new Color(1.08f,1.08f,1.07f)),BraxChampionsInteriorLight.Ceilings);
        plaster=gWhitePanel;
        gWood=Inside(Tex("g1_walnut_lining","smoked_walnut_veneer",1.4f,new Color(1.05f,.98f,.92f)),.85f);
        // owner 11.11 18:28 ("takie białe ... słabo wygląda. Całe białe"): the shops' fixtures in light oak, the walls in oak
        // panelling or a warm textured plaster, the ceilings timber slats on black felt - no all-white interiors
        gOak=Inside(Tex("g1_light_oak_veneer","teak_veneer",1.2f,new Color(1.22f,1.10f,.96f)),.85f);
        gWallPanels=Inside(Tex("g1_wall_oak_panels","wooden_panels",2.4f,new Color(1.0f,.96f,.92f)),.85f);
        gWallWarm=Inside(Tex("g1_wall_warm_plaster","beige_wall_001",2.4f,new Color(.92f,.86f,.80f)),.85f);
        gStoneTop=Inside(Tex("g1_counter_stone","marble_01",1.2f,new Color(1.26f,1.25f,1.24f)),.85f);
        // upholstery: cognac leather (brown_leather), black leather (fabric_leather_02 dark), navy leather, black velvet
        gChairLeather=Inside(Tex("g1_chair_cognac_leather","brown_leather",.6f,new Color(1.0f,.92f,.86f)),.85f);
        gLeatherBlack=Inside(Tex("g1_leather_black","fabric_leather_02",.5f,new Color(.16f,.16f,.17f)),.85f);
        leatherNavy=Inside(Tex("g1_leather_navy","fabric_leather_02",.5f,new Color(.30f,.38f,.62f)),.85f);
        gVelvetBlack=Inside(Tex("g1_velvet_black","velour_velvet",.6f,new Color(.10f,.10f,.11f)),.7f);
        // trims: black anodised aluminium (smooth metal, clean PBR); letters: brushed stainless faces, graphite returns
        frame=S("g1_black_anodised_aluminium",new Color(.07f,.07f,.08f),.30f,.85f);
        gLetterFace=S("g1_letters_brushed_steel",new Color(.86f,.87f,.89f),.22f,.92f,new Color(.34f,.34f,.36f));
        gLetterSide=S("g1_letters_graphite_returns",new Color(.10f,.10f,.11f),.40f,.6f);
    }

    // ================================================================ GENERATE
    public static int GenerateG1(string outFolder)
    {
        string keep=Folder;Folder=Out+"/Meshes/G1";
        try
        {
            System.IO.Directory.CreateDirectory(Folder);
            Doors=Colliders=Signs=Bays=AccessibleBays=Benches=Lamps=Trees=Seats=Lifts=Screens=0;
            G1Bays=G1AccessibleBays=G1Shops=G1Halls=G1CinemaSeats=G1Escalators=0;
            treeGroup=null;treeModel=null;
            Materials();FurnitureMaterials();BuildingMaterials();G1Materials();
            counterTop=StoneTop();benchWood=Inside(Tex("bench_oak","oak_wood_planks",1.0f,new Color(.92f,.86f,.82f)),.85f);wallPanel=gWood;
            var go=new GameObject("G1 | GALERIA GŁÓWNA + KINO MULTIPLEX | 170 x 110 x 24 m, plac, parkingi, dostawy (owner 11.11 board G1)");
            root=go.transform;root.position=new Vector3(G1CentreX,0,G1CentreZ)*K;
            cols=Group(root,"kolizje");
            m=new BraxChampionsMeshBatch();
            G1Site();Flush("G1 site | square, car parks, delivery yard, drop-off, walks, lawns, kerbs, markings");
            G1SiteFurniture();Flush("G1 site | fountain, trees, lamps, benches, bike racks, bollards, flags, pylon, signs");
            G1Shell();Flush("G1 building | structure, slabs, facades, roof");
            G1Vault();Flush("G1 building | the atrium's glazed barrel vault");
            G1Signs();Flush("G1 building | 3D signs: GALERIA GŁÓWNA, KINO MULTIPLEX, BRAXHORN CITY");
            G1Roofscape();Flush("G1 building | roof: membrane, walkways, HVAC, skylights, the screen walls' inner faces");
            G1Interior();Flush("G1 interior | malls, shops, food court, services");
            G1Circulation();Flush("G1 interior | escalators, lifts, stairs");
            G1Cinema();Flush("G1 kino | lobby, reception, halls");
            G1Home();G1GoodsIn();Flush("G1 dostawy | rampa rozładunkowa, korytarz, magazyn");
            Debug.Log("G1_GALERIA bays="+G1Bays+" accessible="+G1AccessibleBays+" shops="+G1Shops+" cinemaHalls="+G1Halls+" cinemaSeats="+G1CinemaSeats+" escalators="+G1Escalators+
                      " doors="+Doors+" colliders="+Colliders+" signs="+Signs+" trees="+Trees+" lamps="+Lamps+" benches="+Benches+" seats="+Seats+" lifts="+Lifts+" screens="+Screens);
            return 1;
        }
        finally{Folder=keep;}
    }

    // ================================================================ SITE
    static bool InTower(float x,float z,float margin=0f){foreach(var t in G1Towers)if(x>t.x0-margin&&x<t.x1+margin&&z>t.z0-margin&&z<t.z1+margin)return true;return false;}
    static void G1Site()
    {
        // ---- the square: granite field in front of the building and between the tower plots up to the city walk; dark
        // granite bands on the 10 m grid (12 mm inlays); the metro entrance hole left open
        var holes=new List<(float x0,float x1,float z0,float z1)>{(G1MetroX0,G1MetroX1,G1MetroZ0,G1MetroZ1)};
        SquareGround(-G1HW,G1HW,G1HD,80f,holes);
        SquareGround(-30f,75f,80f,G1SiteZ1,holes);
        SquareGround(G1HW,110f,G1HD,80f,holes);                                   // to the metro walk east
        for(float x=-80f;x<=80f;x+=10f){float z1=x>-30f&&x<75f?G1SiteZ1-.5f:79.5f;Flat(graniteDark,x-.20f,x+.20f,G1HD+3.5f,z1,WalkY+.012f);}
        foreach(float z in new[]{65f,75f})Flat(graniteDark,-G1HW+.5f,G1HW-.5f,z-.20f,z+.20f,WalkY+.012f);
        for(float z=90f;z<G1SiteZ1-1f;z+=10f)
        {
            // the bands stop 1.5 m short of the metro entrance's surround
            Flat(graniteDark,-29.5f,Mathf.Min(74.5f,z>70f&&z<80f?55f:74.5f),z-.20f,z+.20f,WalkY+.012f);
        }
        // the metro walk east to road X1130 (layout ENTRANCE_PATHS) and the walk round the building's east side
        Ground(walk,110f,G1SiteX1,72.67f,76.3f,WalkY,"chodnik do metra");
        Ground(walk,G1HW,113f,-36f,G1HD,WalkY,"chodnik wschodni");
        // ---- the west car park (two double rows along z), its walk along the building, the south car park, the yard
        G1Lot(G1WestX0,G1WestX1,G1WestZ0,G1WestZ1,false);
        Ground(walk,G1WestX1,-G1HW,G1WestZ0+3f,G1HD+.07f,WalkY,"chodnik zachodni");
        G1Lot(G1WestX1,G1SouthX1,G1SouthZ0,G1SouthZ1,true);
        Ground(walk,G1WestX1,G1HW,G1SouthZ1,-G1HD,WalkY,"chodnik południowy");
        // lawn strip with kerbs between the south car park and the delivery yard
        Ground(lawn,G1SouthX1,G1YardX0,G1SouthZ0,G1SouthZ1,LawnY,"pas zieleni");
        G1Yard();
        G1Taxi();
        // the corners of the site not used otherwise: lawns (the walk to the metro, round the tower plots)
        Ground(lawn,110f,G1SiteX1,37f,72.67f,LawnY,"trawnik");Ground(lawn,110f,G1SiteX1,76.3f,80f,LawnY,"trawnik");
        Ground(lawn,113f,G1SiteX1,G1YardZ1,37f,LawnY,"trawnik");
        Ground(lawn,G1HW,113f,G1YardZ1,-36f,LawnY,"trawnik");
        Ground(lawn,-G1HW,-80f,80f,G1SiteZ1,LawnY,"trawnik");                    // between the drop-off and T15
        G1Mouths();
        G1Edges();
        G1Markings();
    }
    // granite square ground over a rectangle minus holes (the metro entrance)
    static void SquareGround(float x0,float x1,float z0,float z1,List<(float x0,float x1,float z0,float z1)> holes)
    {
        var xs=new List<float>{x0,x1};foreach(var h in holes){if(h.x0>x0&&h.x0<x1)xs.Add(h.x0);if(h.x1>x0&&h.x1<x1)xs.Add(h.x1);}
        xs.Sort();
        for(int i=0;i+1<xs.Count;i++)
        {
            float a=xs[i],b=xs[i+1],xm=(a+b)*.5f,z=z0;
            foreach(var h in holes)if(xm>h.x0&&xm<h.x1&&h.z1>z0&&h.z0<z1){if(h.z0>z)Ground(granite,a,b,z,h.z0,WalkY,"plac");z=Mathf.Max(z,h.z1);}
            if(z<z1)Ground(granite,a,b,z,z1,WalkY,"plac");
        }
    }
    // a car park: asphalt, kerbs inside it, bays (2.5 x 5) in double rows on 6 m aisles; along x (south lot) or z (west lot);
    // accessible bays (3.6 m, blue, pictogram) nearest the building
    static void G1Lot(float x0,float x1,float z0,float z1,bool alongX)
    {
        Ground(asphalt,x0,x1,z0,z1,LaneY,alongX?"parking południowy":"parking zachodni");
        float y=LaneY+.012f;
        if(!alongX)
        {
            // two modules across x: row 5 | aisle 6 | row 5 | row 5 | aisle 6 | row 5 = 32 m; bays along z from z0+6 to z1-6
            float[] rows={x0,x0+11f,x0+16f,x0+27f};bool[] aisleEast={true,false,true,false};
            for(int r=0;r<4;r++)
            {
                float rx0=rows[r],rx1=rx0+5f,za=z0+7f,zb=z1-7f;bool acc=r==3;
                float z=za;int n=0;
                Flat(paintWhite,rx0,rx1,z-.06f,z+.06f,y);
                if(acc)for(int i=0;i<8;i++)
                {
                    float zc=zb-3.6f;Flat(paintBlue,rx0+.06f,rx1-.06f,zc+.06f,zb-.06f,y);Wheelchair((rx0+rx1)*.5f,(zc+zb)*.5f);
                    Flat(paintWhite,rx0,rx1,zc-.06f,zc+.06f,y+.002f);zb=zc;G1AccessibleBays++;G1Bays++;
                }
                while(z+2.5f<=zb+1e-3f&&n<80){z+=2.5f;Flat(paintWhite,rx0,rx1,z-.06f,z+.06f,y);G1Bays++;n++;}
                float xe=aisleEast[r]?rx1:rx0;Flat(paintWhite,xe-.06f,xe+.06f,za,z,y);
            }
            foreach(float ax in new[]{x0+8f,x0+24f}){Arrow(new Vector2(ax-1.4f,z0+20f),new Vector2(0,1));Arrow(new Vector2(ax+1.4f,z1-20f),new Vector2(0,-1));}
        }
        else
        {
            // along x: row 5 (north, at the walk) | aisle 6 | row 5 | row 5 (south) ... z0..z1 = 27 m: 5 + 6 + 5 + 5 + 6 = 27
            float[] rows={z1-5f,z1-16f,z1-21f};bool[] aisleSouth={true,false,true};
            for(int r=0;r<3;r++)
            {
                float rz0=rows[r],rz1=rz0+5f,xa=x0+7f,xb=x1-3f;
                float x=xa;int n=0;bool acc=r==0;
                Flat(paintWhite,x-.06f,x+.06f,rz0,rz1,y);
                if(acc)for(int i=0;i<6;i++)
                {
                    // accessible bays opposite the building's south door (x -6..6 is the walk to it)
                    float xc=x+3.6f;Flat(paintBlue,x+.06f,xc-.06f,rz0+.06f,rz1-.06f,y);Wheelchair((x+xc)*.5f,(rz0+rz1)*.5f);
                    Flat(paintWhite,xc-.06f,xc+.06f,rz0,rz1,y+.002f);x=xc;G1AccessibleBays++;G1Bays++;
                }
                while(x+2.5f<=xb+1e-3f&&n<90){x+=2.5f;Flat(paintWhite,x-.06f,x+.06f,rz0,rz1,y);G1Bays++;n++;}
                float ze=aisleSouth[r]?rz0:rz1;Flat(paintWhite,xa,x,ze-.06f,ze+.06f,y);
            }
            foreach(float ax in new[]{-40f,10f,55f}){Arrow(new Vector2(ax,z1-8.0f),new Vector2(1,0));Arrow(new Vector2(ax+8f,z1-10.4f),new Vector2(-1,0));}
        }
        // kerbs inside the asphalt on the sides that meet walks or lawns
        if(!alongX){B(kerb,x1-.18f,x1,LaneY-.12f,WalkY,z0+27f,z1,true);B(kerb,x0,x1,LaneY-.12f,WalkY,z1-.18f,z1,true);}
        else{B(kerb,x0+27f,x1,LaneY-.12f,WalkY,z1-.18f,z1,true);B(kerb,x1-.18f,x1,LaneY-.12f,WalkY,z0,z1-.18f,true);}
    }
    // the delivery yard (south-east): asphalt, three docks on the building's east facade (south part), truck markings
    static void G1Yard()
    {
        Ground(asphalt,G1YardX0,G1YardX1,G1YardZ0,G1YardZ1,LaneY,"plac dostaw");
        B(kerb,G1YardX0,G1YardX1,LaneY-.12f,WalkY,G1YardZ1-.18f,G1YardZ1,true);
        B(kerb,G1YardX0,G1YardX0+.18f,LaneY-.12f,WalkY,G1YardZ0,G1SouthZ1,true);
        float y=LaneY+.012f;
        for(int i=0;i<4;i++){float z=-54f+i*5.0f;Flat(paintYellow,G1YardX0+.3f,G1YardX0+20f,z-.06f,z+.06f,y);}
        Painted("DOSTAWY",paintWhite,G1YardX0+26f,LaneY+.024f,-62f,1.6f);
    }
    // the taxi / drop-off loop (north-west): a one-way ring round a planted island, entered and left from road X800;
    // TAXI bays on its north side, the drop-off (K+R) on its east side by the square
    static void G1Taxi()
    {
        float x0=G1TaxiX0,x1=G1TaxiX1,z0=G1TaxiZ0,z1=G1TaxiZ1;
        Ground(asphalt,x0,x1,z0,z1,LaneY,"pętla taxi");
        Ground(lawn,x0+7.18f,x1-10.18f,z0+8.18f,z1-10.18f,LawnY+.08f,"wyspa");   // inside the kerbs, 2 cm under their top
        foreach(var (a,b,c,d) in new[]{(x0+7f,x1-10f,z0+8f,z0+8.18f),(x0+7f,x1-10f,z1-10.18f,z1-10f),(x0+7f,x0+7.18f,z0+8.18f,z1-10.18f),(x1-10.18f,x1-10f,z0+8.18f,z1-10.18f)})
            B(kerb,a,b,LaneY-.12f,LawnY+.10f,c,d,true);
        B(kerb,x0,x1,LaneY-.12f,WalkY,z1-.18f,z1,true);B(kerb,x1-.18f,x1,LaneY-.12f,WalkY,z0,z1-.18f,true);B(kerb,x0+7f,x1-.18f,LaneY-.12f,WalkY,z0,z0+.18f,true);
        float y=LaneY+.012f;
        for(int i=0;i<=5;i++){float x=x0+9f+i*5.5f;Flat(paintWhite,x-.06f,x+.06f,z1-4.6f,z1-.4f,y);}
        Painted("TAXI",paintYellow,x0+22f,LaneY+.024f,z1-3.4f,1.4f);
        for(int i=0;i<=3;i++){float z=z0+10f+i*6f;Flat(paintWhite,x1-4.6f,x1-.4f,z-.06f,z+.06f,y);}
        Painted("K+R",paintWhite,x1-2.5f,LaneY+.024f,z0+19f,1.2f);
        Arrow(new Vector2(x0+3.5f,z0+20f),new Vector2(0,1));Arrow(new Vector2(x1-6.5f,z1-16f),new Vector2(0,-1));
        Arrow(new Vector2(x0+20f,z1-6.5f),new Vector2(1,0));Arrow(new Vector2(x0+18f,z0+4f),new Vector2(-1,0));
    }
    static void G1Mouths()
    {
        foreach(var (id,a,b) in G1MouthsZ70)
        {
            Ground(asphalt,a,b,G1SiteZ0,id=="G1_dostawy"?G1YardZ0:G1SouthZ0,LaneY,"wjazd");
            BraxChampionsDriveMouth.Build(root,new BraxChampionsDriveMouth.Spec{id="G1 "+id+" street mouth",alongX=true,edge=G1RoadZ,dir=1f,
                u0=a,u1=b,R=3f,depth=G1SiteZ0-G1RoadZ,topEnd=LaneY,unit=K,assetDir=Folder},asphalt,walk,kerb);
        }
        foreach(var (id,a,b) in G1MouthsX800)
        {
            BraxChampionsDriveMouth.Build(root,new BraxChampionsDriveMouth.Spec{id="G1 "+id+" street mouth",alongX=false,edge=G1RoadX,dir=1f,
                u0=a,u1=b,R=3f,depth=G1SiteX0-G1RoadX,topEnd=LaneY,unit=K,assetDir=Folder},asphalt,walk,kerb);
            // from the sidewalk's edge to the lot / the loop
            Ground(asphalt,G1SiteX0,id=="G1_west"?G1WestX0:G1TaxiX0,a,b,LaneY,"wjazd");
        }
    }
    // the site's edges (a coverage scan of the dump found them open): 2 m lawn strips along the block's sidewalks west, south
    // and east (not across the drives), the strip between the west car park and the taxi loop, the walk from the west car
    // park to the square; kerbs (18 cm, to the walk height) where a strip meets asphalt; all solid (no open steps)
    static void G1Edges()
    {
        void Lawn(float x0,float x1,float z0,float z1){if(x1-x0>.05f&&z1-z0>.05f)B(lawn,x0,x1,LaneY-.12f,LawnY,z0,z1,true);}
        void Kerb(float x0,float x1,float z0,float z1){if(x1-x0>.05f&&z1-z0>.05f)B(kerb,x0,x1,LaneY-.12f,WalkY,z0,z1,true);}
        // west (x -123..-121): the drives at z 0..7 and 95..102 stay asphalt
        foreach(var (a,b) in new[]{(G1SiteZ0,0f),(7f,95f),(102f,G1SiteZ1)}){Lawn(G1SiteX0,G1WestX0-.18f,a,b);Kerb(G1WestX0-.18f,G1WestX0,a,b);}
        // south (z -88..-86): the three drives stay asphalt; kerbs along the lots and the yard (not along the lawn x 80..85)
        foreach(var (a,b) in new[]{(G1WestX0,-78f),(-71f,20f),(27f,122f),(130f,G1SiteX1)})
        {
            Lawn(a,b,G1SiteZ0,G1SouthZ0-.18f);
            foreach(var (c,d) in new[]{(a,Mathf.Min(b,G1SouthX1)),(Mathf.Max(a,G1YardX0),Mathf.Min(b,G1YardX1))})Kerb(c,d,G1SouthZ0-.18f,G1SouthZ0);
            if(b>G1SouthX1&&a<G1YardX0)Lawn(Mathf.Max(a,G1SouthX1),Mathf.Min(b,G1YardX0),G1SouthZ0-.18f,G1SouthZ0);
        }
        // east of the yard (x 171..173, z -86..-38)
        Lawn(G1YardX1+.18f,G1SiteX1,G1SouthZ0,G1YardZ1);Kerb(G1YardX1,G1YardX1+.18f,G1SouthZ0,G1YardZ1);
        // north of the taxi loop (z 136..138)
        Lawn(G1WestX0,-G1HW,G1TaxiZ1,G1SiteZ1);
        // between the west car park (its kerb at z 76) and the taxi loop (z 84): lawn, the loop's corner kerbed
        Lawn(G1WestX0,G1WestX1,G1WestZ1,G1TaxiZ0-.18f);Kerb(G1WestX0,G1TaxiX0+7f,G1TaxiZ0-.18f,G1TaxiZ0);Lawn(G1TaxiX0+7f,G1WestX1,G1TaxiZ0-.18f,G1TaxiZ0);
        // the walk from the west car park along the cinema to the square and the taxi loop (z 55..84)
        B(walk,G1WestX1,-G1HW,LaneY-.12f,WalkY,G1HD+.07f,G1TaxiZ0,true);
    }
    static void G1Markings()
    {
        // zebras across the south car park's aisle to the building's south door and to the walk to the square (P-10, along
        // the aisle), STOP lines at the mouths
        float y=LaneY+.012f;
        foreach(float xc in new[]{0f,-60f})
            for(int k=0;k<6;k++){float z=G1SouthZ1-16f+.25f+k*1.0f;Flat(paintWhite,xc-3f,xc+3f,z,z+.5f,y);}
        foreach(var (id,a,b) in G1MouthsZ70)Flat(paintWhite,a+.3f,b-.3f,G1SiteZ0+1.0f,G1SiteZ0+1.4f,y);
    }

    // ================================================================ SITE FURNITURE
    static void G1SiteFurniture()
    {
        // the fountain on the square's axis, its granite ring and benches; beds with trees round the square; lamps
        var fc=new Vector3(-2f,WalkY,104f);
        Fountain(fc);
        Ring(granite,new Vector3(fc.x,WalkY+.024f,fc.z),7.6f,10.5f,WalkY+.024f,WalkY+.024f,64);   // 12 mm over the dark bands
        foreach(float a in new[]{20f,160f,250f,290f})
        {
            var d=new Vector3(Mathf.Cos(a*Mathf.Deg2Rad),0,Mathf.Sin(a*Mathf.Deg2Rad));
            StationBench(fc+d*9.4f+Vector3.up*.024f,-d,4,"ławka przy fontannie G1 "+a.ToString("0"));
        }
        // tree beds in the square (granite kerbs, lawn, the city tree), benches facing the entrance
        foreach(var (x,z) in new[]{(-24f,92f),(30f,92f),(-24f,122f),(30f,124f),(52f,104f),(52f,128f),(8f,128f),(-60f,68f),(-35f,68f),(35f,68f),(60f,68f)})
        {
            BedG1(x-3f,x+3f,z-3f,z+3f);Tree(x,z,WalkY+.10f);
            StationBench(new Vector3(x,WalkY,z-4.2f),new Vector3(0,0,-1),4,"ławka placu G1 "+x.ToString("0"));
        }
        // front walk: planters with flowers between the entrances, bike racks by the side entrances, bollards
        var fl=Group(root,"G1 kwiaty (Poly Haven)");
        foreach(float x in new[]{-50f,-40f,-30f,30f,40f,50f})BraxChampionsPromenadeFurniture.Planter(m,cols,fl,x,G1HD+5.5f,x<0?"flower_empodium":"flower_gazania");
        foreach(float x0 in new[]{-80f,72f})for(int i=0;i<6;i++)BikeRack(x0+i*1.0f,G1HD+6.5f);
        for(float x=-29f;x<=74f;x+=3f){if(x>55f&&x<70f)continue;Bollard(x,G1SiteZ1-1.2f);}
        // lamps: the square, the walks, the car parks, the yard, the drop-off
        foreach(var (x,z) in new[]{(-80f,62f),(-55f,62f),(55f,62f),(80f,62f),(-28f,84f),(73f,84f),(-28f,110f),(73f,110f),(-28f,134f),(73f,134f),(15f,84f),(15f,134f),
                                   (100f,62f),(140f,70f)})
        {BraxChampionsPromenadeFurniture.Lamp(m,cols,x,z);}
        for(int i=0;i<8;i++){BraxChampionsPromenadeFurniture.Lamp(m,cols,(G1WestX0+G1WestX1)*.5f,G1WestZ0+12f+i*20f);}
        for(int i=0;i<7;i++){BraxChampionsPromenadeFurniture.Lamp(m,cols,-80f+i*25f,G1SouthZ1-10.5f);}
        foreach(float x in new[]{100f,140f})BraxChampionsPromenadeFurniture.Lamp(m,cols,x,G1YardZ1-1f);
        // the gallery's pylon at the corner of the square by the road (GALERIA GŁÓWNA, KINO, P), parking and taxi signs
        G1Pylon(-27f,G1SiteZ1-4f);
        PSign(G1WestX1+1.5f,G1WestZ0+8f);PSign(-74.5f,G1SiteZ0+5f);PSign(23.5f,G1SiteZ0+5f);
        TaxiSign(G1TaxiX0+9f,G1TaxiZ1-1.2f,"TAXI");
        // flags in front of the main entrance (navy, red, white cloth on black masts)
        for(int i=0;i<4;i++){Flag(-44f+i*5f,G1HD+3.2f,i);Flag(29f+i*5f,G1HD+3.2f,i+1);}
    }
    static void BedG1(float x0,float x1,float z0,float z1)
    {
        foreach(var (a,b,c,d) in new[]{(x0,x1,z0,z0+.20f),(x0,x1,z1-.20f,z1),(x0,x0+.20f,z0+.20f,z1-.20f),(x1-.20f,x1,z0+.20f,z1-.20f)})
            B(granite,a,b,WalkY,WalkY+.45f,c,d,true);
        B(lawn,x0+.20f,x1-.20f,WalkY,WalkY+.40f,z0+.20f,z1-.20f);
    }
    static void BikeRack(float x,float z)
    {
        // a black steel hoop (Sheffield stand) on the paving
        var a=new Vector3(x,WalkY,z-.35f);var b=new Vector3(x,WalkY,z+.35f);
        m.Tube(frame,a,a+Vector3.up*.70f,.024f,10);m.Tube(frame,b,b+Vector3.up*.70f,.024f,10);
        for(int i=0;i<8;i++)
        {
            float t0=Mathf.PI*i/8f,t1=Mathf.PI*(i+1)/8f;
            m.Tube(frame,new Vector3(x,WalkY+.70f+.35f*Mathf.Sin(t0),z-.35f*Mathf.Cos(t0)),new Vector3(x,WalkY+.70f+.35f*Mathf.Sin(t1),z-.35f*Mathf.Cos(t1)),.024f,10);
        }
        Col(x-.05f,x+.05f,WalkY,WalkY+1.05f,z-.4f,z+.4f,"stojak rowerowy");
    }
    static void Bollard(float x,float z)
    {
        m.Cylinder(frame,new Vector3(x,WalkY,z),.10f,.90f,16,Quaternion.identity,true);
        m.Cylinder(led,new Vector3(x,WalkY+.74f,z),.115f,.04f,16,Quaternion.identity,true,true);   // a lit ring 15 mm proud
        Col(x-.11f,x+.11f,WalkY,WalkY+.9f,z-.11f,z+.11f,"słupek");
    }
    // a flag on a black mast: the cloth a gently waved strip (navy / red / white by i)
    static Material flagNavy,flagRed,flagWhite;
    static void Flag(float x,float z,int i)
    {
        if(!flagNavy){flagNavy=Tex("g1_flag_navy","poly_wool_herringbone",.4f,new Color(.13f,.26f,.68f));flagRed=Tex("g1_flag_red","poly_wool_herringbone",.4f,new Color(1.53f,.13f,.15f));flagWhite=Tex("g1_flag_white","poly_wool_herringbone",.4f,new Color(1.95f,1.95f,1.93f));}
        m.Cylinder(frame,new Vector3(x,WalkY,z),.06f,10.5f,12,Quaternion.identity,true,false,.04f);
        m.Cylinder(frame,new Vector3(x,WalkY+10.5f,z),.07f,.08f,12,Quaternion.identity,true,true);
        var mat=i%3==0?flagNavy:i%3==1?flagRed:flagWhite;
        const int n=8;float L=2.2f,H=1.4f,top=WalkY+10.2f;
        for(int k=0;k<n;k++)
        {
            float u0=L*k/n,u1=L*(k+1)/n,w0=.10f*Mathf.Sin(u0*2.6f+i),w1=.10f*Mathf.Sin(u1*2.6f+i);
            var a=new Vector3(x+.07f+u0,top-H,z+w0);var b=new Vector3(x+.07f+u1,top-H,z+w1);var c=new Vector3(x+.07f+u1,top,z+w1);var d=new Vector3(x+.07f+u0,top,z+w0);
            Q(mat,a,b,c,d,Vector3.forward);Q(mat,a,b,c,d,Vector3.back);
        }
        Col(x-.08f,x+.08f,WalkY,WalkY+3f,z-.08f,z+.08f,"maszt");
    }
    // the pylon: a 9 m anthracite blade on a granite foot, GALERIA GŁÓWNA / KINO MULTIPLEX / P in 3D (lit), a red edge
    static void G1Pylon(float x,float z)
    {
        RB(gStone,x-1.4f,x+1.4f,WalkY,WalkY+.5f,z-.8f,z+.8f,.04f,true);
        RB(gDark,x-1.1f,x+1.1f,WalkY+.5f,WalkY+9.0f,z-.35f,z+.35f,.05f,true);
        B(gRed,x+1.1f,x+1.16f,WalkY+.5f,WalkY+9.0f,z-.35f,z+.35f);
        var lm=new BraxChampionsMeshBatch();
        foreach(float f in new[]{1f,-1f})
        {
            float zf=z+f*.352f,yaw=f>0?0f:180f;
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"GALERIA",new Vector3(x,WalkY+7.6f,zf),yaw,.42f,.03f,.10f,2.0f);
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"GŁÓWNA",new Vector3(x,WalkY+7.0f,zf),yaw,.42f,.03f,.10f,2.0f);
            BraxChampionsLetters3D.Text(lm,gRed,frame,"KINO",new Vector3(x,WalkY+5.6f,zf),yaw,.34f,.03f,.10f,2.0f);
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"MULTIPLEX",new Vector3(x,WalkY+5.1f,zf),yaw,.24f,.03f,.10f,2.0f);
            BraxChampionsLetters3D.Text(lm,gLetterFace,gLetterSide,"P 420",new Vector3(x,WalkY+3.4f,zf),yaw,.40f,.03f,.10f,2.0f);
        }
        BraxChampionsLetters3D.Build(lm,root,"G1 pylon");Signs++;
    }
}

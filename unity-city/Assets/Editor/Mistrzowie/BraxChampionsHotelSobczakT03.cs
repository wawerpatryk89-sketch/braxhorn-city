using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// HOTEL SOBCZAK / T03 — complete procedural Unity 6 construction.
// Nominal dimensions are metres. BraxClaudeOsiedle.K is applied exactly once.
public static class BraxChampionsHotelSobczakT03
{
    public const float CentreX=1030f,CentreZ=725f,Width=64f,Depth=42f,Height=130f;
    public const float B2=-7.2f,B1=-3.6f,Ground=0f,First=6f,GuestBase=9.6f,GuestTop=117.6f,RoofTop=130f;
    public const int GuestFloors=30,RoomsPerFloor=14,TotalRooms=420,ParkingPerBasement=40,Elevators=4,Stairs=2;
    const string Out="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    const string RootName="T03 | HOTEL SOBCZAK | 5* | COMPLETE";
    static float K=>BraxClaudeOsiedle.K;
    static BraxChampionsHotelSobczakMaterials.Set M;
    static Transform root,site,basements,publicFloors,guestExterior,guestInteriors,cores,roof;
    static readonly List<GameObject> guestFloorRoots=new List<GameObject>();
    static int roomsBuilt,parkingBuilt,elevatorsBuilt,stairsBuilt,serial;

    [Serializable] sealed class Report
    {
        public string status="GENERATED_VISUAL_REVIEW_PENDING";
        public string id="T03",name="HOTEL SOBCZAK / 5*";
        public float width=Width,depth=Depth,height=Height,centreX=CentreX,centreZ=CentreZ;
        public int physicalLevels=34,guestFloors=GuestFloors,rooms,parking,elevators,stairs;
        public string roomLayout="7 SOUTH + 7 NORTH; bay 7.00 x 11.65 m; sleeping zone 3.50 x 7.00 m";
        public string materials="official Poly Haven 4K sources -> Android max 2K ASTC 4x4";
        public bool apkBuilt=false,visualAccepted=false;
    }

    public static void Generate()
    {
        if(Mathf.Abs(K-(float)(2.33/1.80))>.01f)Debug.LogWarning("HOTEL SOBCZAK uses project K="+K);
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Out+"/Materials");
        M=BraxChampionsHotelSobczakMaterials.Create();
        var existing=GameObject.Find(RootName);if(existing)UnityEngine.Object.DestroyImmediate(existing);
        serial=roomsBuilt=parkingBuilt=elevatorsBuilt=stairsBuilt=0;guestFloorRoots.Clear();

        root=new GameObject(RootName).transform;
        root.position=new Vector3(CentreX,0,CentreZ)*K;
        site=Group(root,"T03 SITE / entrance, driveway, fountains");
        basements=Group(root,"T03 B2+B1 / 80 parking spaces + technical");
        publicFloors=Group(root,"T03 PUBLIC FLOORS / lobby + restaurant + SPA + fitness");
        guestExterior=Group(root,"T03 EXTERIOR / guest floors 2-31");
        guestInteriors=Group(root,"T03 STREAMED INTERIORS / 30 x 14 rooms");
        cores=Group(root,"T03 VERTICAL CORES / 4 lifts + 2 stairs");
        roof=Group(root,"T03 MANSARD / roof + technical");

        BuildSite();
        BuildBasements();
        BuildGroundFloor();
        BuildFirstFloor();
        BuildGuestExteriorAndStructure();
        BuildGuestInteriors30();
        BuildStairs();
        BuildElevators();
        BuildRoof();
        BuildPorticoAndSign();
        BuildFacadePilasters();

        if(roomsBuilt!=TotalRooms)throw new InvalidOperationException("HOTEL SOBCZAK room count "+roomsBuilt+" != 420");
        if(parkingBuilt!=80)throw new InvalidOperationException("HOTEL SOBCZAK parking count "+parkingBuilt+" != 80");
        if(elevatorsBuilt!=4||stairsBuilt!=2)throw new InvalidOperationException("HOTEL SOBCZAK vertical core count invalid");

        var streamer=root.gameObject.AddComponent<BraxHotelSobczakFloorStreamer>();
        streamer.guestInteriors=guestFloorRoots.ToArray();streamer.metreScale=K;
        streamer.guestBaseY=GuestBase;streamer.floorHeight=3.6f;streamer.halfWidth=Width*.5f;streamer.halfDepth=Depth*.5f;

        var report=new Report{rooms=roomsBuilt,parking=parkingBuilt,elevators=elevatorsBuilt,stairs=stairsBuilt};
        File.WriteAllText(Out+"/hotel-sobczak-report.json",JsonUtility.ToJson(report,true));
        var at=root.position;root.position=Vector3.zero;
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Out+"/HotelSobczakT03.prefab",InteractionMode.AutomatedAction);
        root.position=at;
        AssetDatabase.Refresh();AssetDatabase.SaveAssets();
        Debug.Log("HOTEL_SOBCZAK_T03_GENERATED rooms=420 parking=80 lifts=4 stairs=2 footprint=64x42 height=130 visualReview=PENDING APK=false");
    }

    static Transform Group(Transform p,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(p,false);return t;
    }
    static void Static(GameObject o)
    {
        GameObjectUtility.SetStaticEditorFlags(o,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
    }
    static GameObject B(Transform p,string name,Material mat,float x,float y,float z,float w,float h,float d,bool solid=true,float tile=0)
    {
        if(w<=0||h<=0||d<=0)throw new InvalidOperationException("HOTEL SOBCZAK invalid box "+name);
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(p,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;o.transform.localScale=new Vector3(w,h,d)*K;
        var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=mat;
        if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        if(mat==M.glass||mat==M.water){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
        else Static(o);
        if(tile>0)SetTile(r,w,h,d,tile);
        return o;
    }
    static GameObject Cyl(Transform p,string name,Material mat,float x,float y,float z,float diameter,float h,bool solid=true)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(p,false);
        o.transform.localPosition=new Vector3(x,y,z)*K;o.transform.localScale=new Vector3(diameter,h*.5f,diameter)*K;
        o.GetComponent<MeshRenderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());else Static(o);return o;
    }
    static void SetTile(Renderer r,float w,float h,float d,float metres)
    {
        var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);
        float u,v;if(h<Mathf.Min(w,d)*.25f){u=w/metres;v=d/metres;}else{u=Mathf.Max(w,d)/metres;v=h/metres;}
        var st=new Vector4(Mathf.Max(.25f,u),Mathf.Max(.25f,v),0,0);
        b.SetVector("_MainTex_ST",st);b.SetVector("_BumpMap_ST",st);b.SetVector("_ARM_ST",st);r.SetPropertyBlock(b);
    }
    static void Label(Transform p,string text,float x,float y,float z,float size,float yaw=0)
    {
        var o=new GameObject("LABEL | "+text);o.transform.SetParent(p,false);o.transform.localPosition=new Vector3(x,y,z)*K;
        o.transform.localRotation=Quaternion.Euler(0,yaw,0);var t=o.AddComponent<TextMesh>();t.text=text;
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=64;t.characterSize=size*K*10f/64f;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.93f,.73f,.36f);
        o.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;
    }

    // ---------------------------------------------------------------- SITE
    static void BuildSite()
    {
        B(site,"T03 granite entrance plaza",M.graniteOutdoor,0,.035f,-27f,28f,.07f,12f,true,2f);
        B(site,"T03 main asphalt hotel driveway",M.asphalt,0,.025f,-43f,9f,.05f,30f,true,2.5f);
        B(site,"T03 red carpet arrival",M.carpetRed,0,.075f,-27f,4.2f,.05f,12f,false);
        // External switchback parking access stays entirely in the 37m front setback.
        BuildRamp(site,"T03 external ramp G -> B1",25f,-43f,-1.80f,6.5f,24.27f,-8.5308f,M.antiSlip);
        BuildRamp(site,"T03 external ramp B1 -> B2",16.5f,-43f,-5.40f,6.5f,24.27f,8.5308f,M.antiSlip);
        foreach(float x in new[]{-16f,16f})
        {
            Cyl(site,"T03 fountain stone basin",M.graniteOutdoor,x,.20f,-31f,5.6f,.38f,true);
            Cyl(site,"T03 fountain water",M.water,x,.42f,-31f,4.9f,.035f,false);
            Cyl(site,"T03 fountain central jet pedestal",M.marble,x,.72f,-31f,.45f,.65f,false);
        }
        foreach(float x in new[]{-24f,-12f,12f,24f})
            B(site,"T03 warm entrance bollard",M.brass,x,.50f,-34f,.16f,1f,.16f,true);
    }
    static void BuildRamp(Transform p,string name,float x,float z,float y,float width,float length,float angle,Material mat)
    {
        var o=B(p,name,mat,x,y,z,width,.25f,length,true,2.5f);o.transform.localRotation=Quaternion.Euler(angle,0,0);
        B(p,name+" left retaining wall",M.concrete,x-width*.5f-.12f,y+.75f,z,.20f,1.5f,length,true,3f).transform.localRotation=Quaternion.Euler(angle,0,0);
        B(p,name+" right retaining wall",M.concrete,x+width*.5f+.12f,y+.75f,z,.20f,1.5f,length,true,3f).transform.localRotation=Quaternion.Euler(angle,0,0);
    }

    // ---------------------------------------------------------------- BASEMENTS
    static void BuildBasements()
    {
        BuildParkingLevel("B2",B2,basements);
        BuildParkingLevel("B1",B1,basements);
        // Horizontal access tunnels from external ramps.
        B(basements,"B1 access tunnel floor",M.antiSlip,25f,-3.48f,-26f,6.5f,.20f,10f,true,2.5f);
        B(basements,"B2 access tunnel floor",M.antiSlip,16.5f,-7.08f,-38f,6.5f,.20f,34f,true,2.5f);
    }
    static void BuildParkingLevel(string id,float y,Transform p)
    {
        var g=Group(p,"T03 "+id+" | 40 PARKING SPACES");
        B(g,id+" floor",M.garage,0,y+.10f,0,63.4f,.20f,41.4f,true,3f);
        B(g,id+" north wall",M.concrete,0,y+1.8f,20.8f,64f,3.6f,.35f,true,3f);
        B(g,id+" east wall",M.concrete,31.8f,y+1.8f,0,.35f,3.6f,42f,true,3f);
        B(g,id+" west wall",M.concrete,-31.8f,y+1.8f,0,.35f,3.6f,42f,true,3f);
        // Leave two vehicle openings in south wall for the external ramp/tunnel connections.
        B(g,id+" south wall west",M.concrete,-7.75f,y+1.8f,-20.8f,48.5f,3.6f,.35f,true,3f);
        B(g,id+" south wall east",M.concrete,29.25f,y+1.8f,-20.8f,5.5f,3.6f,.35f,true,3f);
        B(g,id+" ceiling",M.concrete,0,y+3.54f,0,64f,.12f,42f,true,3f);
        B(g,id+" central drive aisle",M.garage,1.3f,y+.215f,0,52f,.025f,6f,false,3f);
        for(int row=0;row<2;row++)
        {
            float z=row==0?-5.6f:5.6f;
            for(int i=0;i<20;i++)
            {
                float x=-23.4f+i*2.6f;
                // white U marking: back + two side lines
                B(g,id+" PARK "+(row*20+i+1).ToString("D2")+" back",M.parkingPaint,x,y+.232f,z+(row==0?-2.56f:2.56f),2.48f,.018f,.07f,false);
                B(g,id+" PARK line L",M.parkingPaint,x-1.27f,y+.232f,z,.06f,.018f,5.12f,false);
                B(g,id+" PARK line R",M.parkingPaint,x+1.27f,y+.232f,z,.06f,.018f,5.12f,false);
                parkingBuilt++;
            }
        }
        // Technical/storage rooms in unused north/south bands.
        B(g,id+" technical room floor",M.antiSlip,-14f,y+.225f,15f,18f,.03f,7f,false,2.5f);
        B(g,id+" storage floor",M.antiSlip,12f,y+.225f,-15f,16f,.03f,7f,false,2.5f);
        Label(g,id+" 40 MIEJSC",0,y+1.0f,19.8f,.28f,180);
    }

    // ---------------------------------------------------------------- PUBLIC GROUND
    static void BuildGroundFloor()
    {
        var g=Group(publicFloors,"PARTER 0.00-6.00 | lobby reception cafe");
        B(g,"Ground marble slab",M.marble,0,.12f,0,63.4f,.24f,41.4f,true,1.5f);
        B(g,"Ground clean ceiling",M.stucco,0,5.90f,0,63.4f,.20f,41.4f,true,1.8f);
        BuildPublicFacade(g,0f,6f,2.4f,4.2f);
        // Exact functional areas from owner's board.
        Area(g,"RECEPCJA 60m2",M.marble,0,.15f,-14.5f,12f,5f);
        Area(g,"LOBBY 350m2",M.marble,0,.16f,-2f,25f,14f);
        Area(g,"BAR CAFE LOUNGE 180m2",M.parquet,0,.17f,14f,30f,6f);
        Area(g,"TOALETA M 40m2",M.marble,-26f,.17f,14f,8f,5f);
        Area(g,"TOALETA K 40m2",M.marble,26f,.17f,14f,8f,5f);
        Area(g,"BAGAZ 50m2",M.oak,-26f,.17f,-14f,10f,5f);
        Area(g,"CONCIERGE 40m2",M.marble,26f,.17f,-14f,8f,5f);

        // Reception: 12 m desk line, marble top + oak/brass body.
        B(g,"Reception oak body",M.oak,0,.65f,-12.2f,11.5f,1.05f,.75f,true,1.2f);
        B(g,"Reception marble top",M.marble,0,1.22f,-12.2f,11.9f,.12f,.92f,true,1.5f);
        for(float x=-5.2f;x<=5.2f;x+=2.6f)B(g,"Reception brass division",M.brass,x,.70f,-12.63f,.06f,1.0f,.05f,false);

        // Lobby furniture clusters.
        foreach(float x in new[]{-7.5f,0f,7.5f})
        {
            B(g,"Lobby leather sofa",M.leather,x,.55f,-2.5f,4.6f,1.0f,1.45f,true,.3f);
            B(g,"Lobby marble coffee table",M.marble,x,.46f,.2f,2.2f,.18f,1.2f,true,1.5f);
        }
        // Bar.
        B(g,"Cafe oak bar body",M.oak,0,.70f,15.2f,13f,1.15f,.90f,true,1.2f);
        B(g,"Cafe marble bar top",M.marble,0,1.32f,15.2f,13.4f,.12f,1.08f,true,1.5f);
        for(float x=-5.5f;x<=5.5f;x+=2.2f)Cyl(g,"Cafe brass stool",M.brass,x,.50f,12.7f,.42f,.90f,true);

        BuildServiceRoom(g,"TOALETA M",-26f,14f,8f,5f,6f,M.marble);
        BuildServiceRoom(g,"TOALETA K",26f,14f,8f,5f,6f,M.marble);
        BuildServiceRoom(g,"BAGAZ",-26f,-14f,10f,5f,6f,M.oak);
        BuildServiceRoom(g,"CONCIERGE",26f,-14f,8f,5f,6f,M.marble);
        Label(g,"HOTEL SOBCZAK",0,4.4f,-20.4f,.34f,0);
    }

    static void BuildFirstFloor()
    {
        var g=Group(publicFloors,"PIETRO 1 6.00-9.60 | restaurant kitchen spa fitness");
        B(g,"Level1 structural slab",M.marble,0,6.08f,0,63.4f,.16f,41.4f,true,1.5f);
        B(g,"Level1 ceiling",M.stucco,0,9.50f,0,63.4f,.20f,41.4f,true,1.8f);
        BuildPublicFacade(g,6f,3.6f,1.8f,2.2f);
        Area(g,"RESTAURACJA 250m2",M.parquet,-17f,6.17f,-10f,25f,10f);
        Area(g,"KUCHNIA 180m2",M.tile,-18f,6.17f,10f,20f,9f);
        Area(g,"SPA 350m2",M.terrazzo,17f,6.17f,8f,25f,14f);
        Area(g,"FITNESS 250m2",M.rubber,17f,6.17f,-10f,25f,10f);

        // Restaurant tables.
        for(float x=-27f;x<=-7f;x+=5f)for(float z=-12.5f;z<=-7.5f;z+=5f)
        {
            Cyl(g,"Restaurant marble table",M.marble,x,6.75f,z,1.25f,.12f,true);
            for(float dx=-1;dx<=1;dx+=2)B(g,"Restaurant leather chair",M.leather,x+dx,6.55f,z,.65f,.90f,.65f,true,.3f);
        }
        // Kitchen stainless workline.
        for(float x=-26f;x<=-10f;x+=4f)B(g,"Kitchen stainless bench",M.steel,x,6.65f,11f,3.0f,1.0f,.85f,true);
        // SPA treatment beds and glass screen.
        for(float x=8f;x<=26f;x+=6f)B(g,"SPA white leather treatment bed",M.leatherWhite,x,6.55f,9f,2.2f,.55f,.85f,true,.3f);
        B(g,"SPA clear glass relaxation divider",M.glass,17f,7.55f,4f,20f,2.7f,.06f,false);
        // Fitness equipment proxies, realistic dimensions.
        for(float x=8f;x<=26f;x+=4.5f)
        {
            B(g,"Fitness treadmill deck",M.darkBronze,x,6.35f,-11f,.85f,.22f,2.0f,true);
            B(g,"Fitness treadmill console",M.darkBronze,x,7.20f,-10.2f,.75f,.85f,.14f,true);
        }
    }

    static void Area(Transform g,string name,Material mat,float x,float y,float z,float w,float d)
        =>B(g,name+" floor",mat,x,y,z,w,.035f,d,false,mat==M.marble?1.5f:mat==M.parquet?3.4f:2f);

    static void BuildServiceRoom(Transform g,string name,float x,float z,float w,float d,float h,Material floor)
    {
        float y=h*.5f;
        B(g,name+" west wall",M.stucco,x-w*.5f,y,z,.14f,h,d,true,1.8f);
        B(g,name+" east wall",M.stucco,x+w*.5f,y,z,.14f,h,d,true,1.8f);
        B(g,name+" north wall",M.stucco,x,y,z+d*.5f,w,h,.14f,true,1.8f);
        // south wall with 1m open door
        B(g,name+" south left",M.stucco,x-w*.25f-.25f,y,z-d*.5f,w*.5f-1f,h,.14f,true,1.8f);
        B(g,name+" south right",M.stucco,x+w*.25f+.25f,y,z-d*.5f,w*.5f-1f,h,.14f,true,1.8f);
        B(g,name+" door header",M.stucco,x,h-.25f,z-d*.5f,1f,.50f,.14f,true,1.8f);
    }

    static void BuildPublicFacade(Transform p,float baseY,float floorH,float windowW,float windowH)
    {
        float sill=Mathf.Max(.35f,(floorH-windowH)*.45f),header=floorH-sill-windowH;
        foreach(float zsign in new[]{-1f,1f})
        {
            float z=zsign*20.82f;
            for(int i=0;i<7;i++)
            {
                float x=-21f+i*7f;
                WindowBay(p,"Public facade",x,baseY,z,7f,floorH,windowW,windowH,sill,header,zsign);
            }
            // stair wings
            B(p,"Public facade west wing",M.facade,-28.25f,baseY+floorH*.5f,z,7.5f,floorH,.36f,true,3f);
            B(p,"Public facade east wing",M.facade,28.25f,baseY+floorH*.5f,z,7.5f,floorH,.36f,true,3f);
        }
        foreach(float xsign in new[]{-1f,1f})
            B(p,"Public side facade",M.facade,xsign*31.82f,baseY+floorH*.5f,0,.36f,floorH,42f,true,3f);
    }

    // ---------------------------------------------------------------- GUEST EXTERIOR
    static void BuildGuestExteriorAndStructure()
    {
        for(int f=0;f<GuestFloors;f++)
        {
            float y0=GuestBase+f*3.6f;
            B(guestExterior,"Guest slab "+(f+2),M.marble,0,y0+.08f,0,63.3f,.16f,41.3f,true,1.5f);
            B(guestExterior,"Guest ceiling "+(f+2),M.stucco,0,y0+3.51f,0,63.3f,.18f,41.3f,true,1.8f);
            foreach(float zsign in new[]{-1f,1f})
            {
                float z=zsign*20.82f;
                for(int i=0;i<7;i++)
                    WindowBay(guestExterior,"Guest facade F"+(f+2),-21f+i*7f,y0,z,7f,3.6f,2.2f,2.25f,.65f,.70f,zsign);
                B(guestExterior,"Guest stair wing west F"+(f+2),M.facade,-28.25f,y0+1.8f,z,7.5f,3.6f,.36f,true,3f);
                B(guestExterior,"Guest stair wing east F"+(f+2),M.facade,28.25f,y0+1.8f,z,7.5f,3.6f,.36f,true,3f);
            }
            foreach(float xsign in new[]{-1f,1f})
                B(guestExterior,"Guest side wall F"+(f+2),M.facade,xsign*31.82f,y0+1.8f,0,.36f,3.6f,42f,true,3f);
        }
        // main horizontal stone belts
        foreach(float y in new[]{6f,9.6f,117.6f})
        {
            B(guestExterior,"Front classical cornice "+y,M.stucco,0,y,-21.18f,64f,.34f,.46f,false,1.8f);
            B(guestExterior,"Rear classical cornice "+y,M.stucco,0,y,21.18f,64f,.34f,.46f,false,1.8f);
        }
    }
    static void WindowBay(Transform p,string prefix,float x,float y0,float z,float bayW,float floorH,float winW,float winH,float sill,float header,float outward)
    {
        float side=(bayW-winW)*.5f;
        float zwall=z;
        B(p,prefix+" jamb L",M.facade,x-(winW*.5f+side*.5f),y0+floorH*.5f,zwall,side,floorH,.36f,true,3f);
        B(p,prefix+" jamb R",M.facade,x+(winW*.5f+side*.5f),y0+floorH*.5f,zwall,side,floorH,.36f,true,3f);
        B(p,prefix+" sill",M.facade,x,y0+sill*.5f,zwall,winW,sill,.36f,true,3f);
        B(p,prefix+" header",M.facade,x,y0+floorH-header*.5f,zwall,winW,header,.36f,true,3f);
        float gy=y0+sill+winH*.5f, gz=zwall+outward*.20f;
        B(p,prefix+" glass",M.glass,x,gy,gz,winW-.14f,winH-.12f,.055f,false);
        B(p,prefix+" frame top",M.darkBronze,x,gy+winH*.5f,gz+outward*.04f,winW+.12f,.10f,.08f,false);
        B(p,prefix+" frame bottom",M.darkBronze,x,gy-winH*.5f,gz+outward*.04f,winW+.12f,.10f,.08f,false);
        B(p,prefix+" frame left",M.darkBronze,x-winW*.5f,gy,gz+outward*.04f,.10f,winH,.08f,false);
        B(p,prefix+" frame right",M.darkBronze,x+winW*.5f,gy,gz+outward*.04f,.10f,winH,.08f,false);
    }

    // ---------------------------------------------------------------- 420 ROOMS
    static void BuildGuestInteriors30()
    {
        for(int f=0;f<GuestFloors;f++)
        {
            float floorY=GuestBase+f*3.6f;
            var fg=Group(guestInteriors,"GUEST INTERIOR FLOOR "+(f+2)+" | 14 ROOMS");
            fg.localPosition=Vector3.up*floorY*K;guestFloorRoots.Add(fg.gameObject);
            // Double 2.4m corridors directly outside room doors.
            B(fg,"South corridor carpet",M.carpet,0,.13f,-7.8f,49f,.035f,2.4f,false,.3f);
            B(fg,"North corridor carpet",M.carpet,0,.13f,7.8f,49f,.035f,2.4f,false,.3f);
            B(fg,"Central service core marble",M.marble,0,.13f,0,49f,.035f,13.2f,false,1.5f);
            for(int i=0;i<7;i++)
            {
                float x=-21f+i*7f;
                BuildGuestRoom(fg,f+2,i+1,x,-14.825f,-1);
                BuildGuestRoom(fg,f+2,14-i,x,14.825f,1);
            }
            // Housekeeping / linen spaces between lift core and room corridors.
            B(fg,"Housekeeping west",M.oak,-14f,.55f,0,5f,1.0f,3f,true,1.2f);
            B(fg,"Linen east",M.oak,14f,.55f,0,5f,1.0f,3f,true,1.2f);
            fg.gameObject.SetActive(false);
        }
    }
    static void BuildGuestRoom(Transform p,int floor,int localNumber,float x,float z,float side)
    {
        string number=(floor*100+localNumber).ToString();
        var g=Group(p,"ROOM "+number+" | bay 7.00x11.65 | sleep module 3.50x7.00");
        float innerZ=side*9.0f,outerZ=side*20.65f,centreZ=(innerZ+outerZ)*.5f;
        B(g,"Room oak floor",M.oak,x,.10f,centreZ,6.86f,.05f,11.5f,false,1.2f);
        // side partitions
        B(g,"Room west partition",M.stucco,x-3.5f,1.70f,centreZ,.14f,3.4f,11.4f,true,1.8f);
        B(g,"Room east partition",M.stucco,x+3.5f,1.70f,centreZ,.14f,3.4f,11.4f,true,1.8f);
        // corridor wall with 1m door opening centred.
        B(g,"Room corridor wall left",M.stucco,x-2f,1.70f,innerZ,3f,3.4f,.14f,true,1.8f);
        B(g,"Room corridor wall right",M.stucco,x+2f,1.70f,innerZ,3f,3.4f,.14f,true,1.8f);
        B(g,"Room door header",M.stucco,x,3.05f,innerZ,1f,.70f,.14f,true,1.8f);
        B(g,"Room brass threshold",M.brass,x,.035f,innerZ-side*.12f,1.0f,.04f,.18f,false);
        Label(g,number,x,2.55f,innerZ-side*.10f,.11f,side>0?180:0);

        // 3.50 x 7.00 sleeping zone from owner's drawing, centred in the larger premium bay.
        float sleepZ=outerZ-side*3.85f;
        B(g,"Sleep zone rug 3.50x7.00",M.carpet,x,.135f,sleepZ,3.5f,.025f,7f,false,.3f);
        B(g,"King bed oak base",M.oak,x,.32f,outerZ-side*3.0f,2.15f,.42f,2.25f,true,1.2f);
        B(g,"King mattress",M.whitePaint,x,.62f,outerZ-side*3.0f,2.05f,.30f,2.10f,true);
        B(g,"Leather headboard",M.leather,x,1.15f,outerZ-side*1.95f,2.4f,1.25f,.14f,true,.3f);
        foreach(float dx in new[]{-1.45f,1.45f})
        {
            B(g,"Oak nightstand",M.oak,x+dx,.42f,outerZ-side*2.5f,.65f,.72f,.55f,true,1.2f);
            B(g,"Brass bedside lamp",M.brass,x+dx,1.08f,outerZ-side*2.5f,.10f,.65f,.10f,false);
        }
        B(g,"Oak writing desk",M.oak,x+2.35f,.60f,outerZ-side*6.2f,1.7f,.12f,.70f,true,1.2f);
        B(g,"Leather desk chair",M.leather,x+2.35f,.48f,outerZ-side*5.15f,.68f,.85f,.68f,true,.3f);
        B(g,"Oak wardrobe",M.oak,x-2.65f,1.25f,innerZ+side*2.2f,1.2f,2.5f,.65f,true,1.2f);

        // Bathroom 2.50 x 3.20 in inner corner.
        float bathX=x-2.10f,bathZ=innerZ+side*1.70f;
        B(g,"Bathroom marble floor",M.marble,bathX,.145f,bathZ,2.5f,.035f,3.2f,false,1.5f);
        B(g,"Bathroom privacy wall",M.stucco,x-.85f,1.65f,bathZ,.12f,3.3f,3.2f,true,1.8f);
        B(g,"Bathroom glass shower",M.glass,x-2.55f,1.10f,bathZ+side*.85f,1.15f,2.0f,.06f,false);
        B(g,"Bathroom vanity",M.marble,x-2.2f,.58f,bathZ-side*.85f,1.6f,.18f,.55f,true,1.5f);
        Cyl(g,"Bathroom basin",M.whitePaint,x-2.2f,.73f,bathZ-side*.85f,.52f,.15f,true);
        Cyl(g,"Bathroom toilet",M.whitePaint,x-1.35f,.38f,bathZ+side*.55f,.55f,.70f,true);
        roomsBuilt++;
    }

    // ---------------------------------------------------------------- STAIRS
    static void BuildStairs()
    {
        foreach(float x in new[]{-28f,28f})
        {
            var s=Group(cores,"STAIR "+(x<0?"WEST":"EAST")+" | B2 TO 31");
            BuildStairInterval(s,x,B2,B1,0);BuildStairInterval(s,x,B1,Ground,1);
            BuildStairInterval(s,x,Ground,First,0);BuildStairInterval(s,x,First,GuestBase,1);
            for(int f=0;f<GuestFloors-1;f++)BuildStairInterval(s,x,GuestBase+f*3.6f,GuestBase+(f+1)*3.6f,f%2);
            B(s,"Stair fire wall west",M.stucco,x-2.6f,55f,0,.16f,124f,9f,true,1.8f);
            B(s,"Stair fire wall east",M.stucco,x+2.6f,55f,0,.16f,124f,9f,true,1.8f);
            stairsBuilt++;
        }
    }
    static void BuildStairInterval(Transform p,float centreX,float y0,float y1,int reverse)
    {
        float rise=y1-y0;int perFlight=Mathf.Max(10,Mathf.CeilToInt(rise/.36f));float stepRise=rise/(perFlight*2f);
        float tread=rise>4f?.26f:.28f,run=perFlight*tread;
        float startZ=reverse==0?-run*.5f:run*.5f,dir=reverse==0?1f:-1f;
        for(int i=0;i<perFlight;i++)
        {
            float z=startZ+dir*(i+.5f)*tread;
            B(p,"Stair flight A step",M.granite,centreX-.9f,y0+(i+.5f)*stepRise,z,1.5f,stepRise,tread,true,2.3f);
            float z2=-startZ-dir*(i+.5f)*tread;
            B(p,"Stair flight B step",M.granite,centreX+.9f,y0+rise*.5f+(i+.5f)*stepRise,z2,1.5f,stepRise,tread,true,2.3f);
        }
        B(p,"Stair mid landing",M.granite,centreX,y0+rise*.5f,0,3.8f,.16f,1.4f,true,2.3f);
    }

    // ---------------------------------------------------------------- ELEVATORS
    static void BuildElevators()
    {
        float[] xs={-4.8f,-1.6f,1.6f,4.8f};
        for(int i=0;i<4;i++)BuildElevator(i,xs[i],0);
    }
    static void BuildElevator(int index,float x,float z)
    {
        var e=Group(cores,"ELEVATOR "+(index+1)+" | "+(index==3?"SERVICE":"PASSENGER"));
        // full-height shaft: rear + side walls, landing side closed by sliding leaves.
        B(e,"Lift shaft west",M.stucco,x-1.45f,55f,z,.16f,124f,2.9f,true,1.8f);
        B(e,"Lift shaft east",M.stucco,x+1.45f,55f,z,.16f,124f,2.9f,true,1.8f);
        B(e,"Lift shaft rear",M.stucco,x,55f,z+1.45f,2.9f,124f,.16f,true,1.8f);

        var levels=new List<float>{B2,B1,Ground,First};
        for(int f=0;f<GuestFloors;f++)levels.Add(GuestBase+f*3.6f);
        levels.Add(GuestTop); // TECH landing; 35th stop, not a guest floor.
        var names=new List<string>{"B2","B1","PARTER","1"};
        for(int n=2;n<=31;n++)names.Add(n.ToString());names.Add("TECH");
        var usable=Enumerable.Repeat(true,levels.Count).ToArray();
        if(index<3)usable[usable.Length-1]=false;

        var landL=new Transform[levels.Count];var landR=new Transform[levels.Count];var calls=new Vector3[levels.Count];
        for(int n=0;n<levels.Count;n++)
        {
            float yy=levels[n];
            landL[n]=DoorLeaf(e,"Landing "+names[n]+" left",x-.68f,yy,z-1.52f,M.darkBronze,M.glass);
            landR[n]=DoorLeaf(e,"Landing "+names[n]+" right",x+.68f,yy,z-1.52f,M.darkBronze,M.glass);
            B(e,"Landing "+names[n]+" brass lintel",M.brass,x,yy+2.35f,z-1.55f,2.8f,.12f,.12f,false);
            calls[n]=root.TransformPoint(new Vector3(x,yy+1.2f,z-2.35f)*K);
        }
        var cabin=Group(e,"Moving premium cabin");
        cabin.localPosition=new Vector3(x,B2,z)*K;
        B(cabin,"Cabin marble floor",M.marble,0,.08f,0,2.45f,.16f,2.45f,true,1.5f);
        B(cabin,"Cabin ceiling",M.steel,0,2.55f,0,2.45f,.16f,2.45f,true);
        B(cabin,"Cabin rear oak panel",M.oak,0,1.3f,1.19f,2.35f,2.45f,.10f,true,1.2f);
        B(cabin,"Cabin west glass",M.glass,-1.19f,1.3f,0,.10f,2.45f,2.35f,true);
        B(cabin,"Cabin east glass",M.glass,1.19f,1.3f,0,.10f,2.45f,2.35f,true);
        var cabL=DoorLeaf(cabin,"Cabin left",-0.68f,0,-1.23f,M.darkBronze,M.glass);
        var cabR=DoorLeaf(cabin,"Cabin right",0.68f,0,-1.23f,M.darkBronze,M.glass);

        var controller=e.gameObject.AddComponent<BraxWinda>();
        controller.nazwa="HOTEL SOBCZAK · WINDA "+(index+1);
        controller.kabina=cabin;controller.kabinaL=cabL;controller.kabinaP=cabR;
        controller.pietraY=levels.Select(v=>v*K).ToArray();controller.pietraNazwy=names.ToArray();controller.uzytkowe=usable;
        controller.przystanekL=landL;controller.przystanekP=landR;controller.wezwanie=calls;
        Vector3 world=root.TransformPoint(new Vector3(x,0,z)*K);controller.szyb=new Vector2(world.x,world.z);
        controller.wnetrze=new Vector2(1.05f*K,1.05f*K);controller.przesuw=.72f*K;controller.start=2;
        elevatorsBuilt++;
    }
    static Transform DoorLeaf(Transform p,string name,float x,float y,float z,Material frame,Material glass)
    {
        var t=Group(p,name);t.localPosition=new Vector3(x,y,z)*K;
        B(t,name+" glass",glass,0,1.18f,0,1.25f,2.30f,.07f,true);
        B(t,name+" frame top",frame,0,2.32f,0,1.30f,.08f,.09f,false);
        B(t,name+" frame edge",frame,x<0?-.60f:.60f,1.18f,0,.08f,2.30f,.09f,false);
        return t;
    }

    // ---------------------------------------------------------------- ROOF
    static void BuildRoof()
    {
        B(roof,"Mansard front",M.roof,0,121.4f,-19f,64f,.35f,8.59f,true,3f).transform.localRotation=Quaternion.Euler(-62.24f,0,0);
        B(roof,"Mansard rear",M.roof,0,121.4f,19f,64f,.35f,8.59f,true,3f).transform.localRotation=Quaternion.Euler(62.24f,0,0);
        B(roof,"Mansard west",M.roof,-30f,121.4f,0,8.59f,.35f,34f,true,3f).transform.localRotation=Quaternion.Euler(0,0,62.24f);
        B(roof,"Mansard east",M.roof,30f,121.4f,0,8.59f,.35f,34f,true,3f).transform.localRotation=Quaternion.Euler(0,0,-62.24f);
        float upperAngle=Mathf.Atan2(4.8f,8f)*Mathf.Rad2Deg,upperLen=Mathf.Sqrt(8f*8f+4.8f*4.8f);
        B(roof,"Upper roof front",M.roof,0,127.6f,-13f,56f,.32f,upperLen,true,3f).transform.localRotation=Quaternion.Euler(-upperAngle,0,0);
        B(roof,"Upper roof rear",M.roof,0,127.6f,13f,56f,.32f,upperLen,true,3f).transform.localRotation=Quaternion.Euler(upperAngle,0,0);
        B(roof,"Upper roof west",M.roof,-24f,127.6f,0,upperLen,.32f,18f,true,3f).transform.localRotation=Quaternion.Euler(0,0,upperAngle);
        B(roof,"Upper roof east",M.roof,24f,127.6f,0,upperLen,.32f,18f,true,3f).transform.localRotation=Quaternion.Euler(0,0,-upperAngle);
        B(roof,"Hidden roof technical deck",M.antiSlip,0,125.0f,0,40f,.16f,18f,true,2.5f);
        // Dormers aligned with 7 room bays on front and rear.
        for(int i=0;i<7;i++)foreach(float s in new[]{-1f,1f})
        {
            float x=-21f+i*7f,z=s*18.1f;
            B(roof,"Mansard dormer body",M.stucco,x,123.25f,z,3.0f,3.2f,1.35f,true,1.8f);
            B(roof,"Mansard dormer glass",M.glass,x,123.25f,z+s*.72f,1.55f,1.85f,.06f,false);
            B(roof,"Mansard dormer cap",M.roof,x,125.0f,z,3.3f,.22f,1.75f,true,3f);
        }
        // Technical equipment is hidden behind the mansard.
        for(float x=-12f;x<=12f;x+=8f)
            B(roof,"Hidden HVAC module",M.steel,x,126.0f,0,4.5f,1.6f,2.6f,true);
    }

    // ---------------------------------------------------------------- PORTICO / SIGN / CLASSICAL DETAIL
    static void BuildPorticoAndSign()
    {
        // Basement/ground exterior stone plinth.
        B(root,"Classical sandstone plinth front",M.sandstone,0,1.2f,-21.15f,64f,2.4f,.55f,true,3f);
        B(root,"Grand entrance canopy",M.darkBronze,0,5.15f,-24.0f,18f,.42f,5.8f,true);
        B(root,"Grand entrance canopy brass rim",M.brass,0,5.38f,-24.0f,18.3f,.12f,6.0f,false);
        foreach(float x in new[]{-6f,-2f,2f,6f})
        {
            Cyl(root,"Grand portico ivory column",M.stucco,x,2.65f,-25.8f,.90f,5.3f,true);
            Cyl(root,"Grand portico brass base",M.brass,x,.18f,-25.8f,1.15f,.24f,true);
            Cyl(root,"Grand portico brass capital",M.brass,x,5.32f,-25.8f,1.10f,.22f,true);
        }
        // Clear 3.6m entrance, no collider on glass leaves.
        B(root,"Main entrance glass left",M.glass,-.95f,2.0f,-21.38f,1.7f,3.6f,.08f,false);
        B(root,"Main entrance glass right",M.glass,.95f,2.0f,-21.38f,1.7f,3.6f,.08f,false);
        foreach(float x in new[]{-1.85f,0f,1.85f})B(root,"Entrance brass mullion",M.brass,x,2.0f,-21.45f,.08f,3.8f,.10f,false);

        // Central classical risalit and roof pavilion.
        B(root,"Central facade risalit",M.stucco,0,63f,-21.35f,16f,108f,.70f,false,1.8f);
        // Do not cover windows: risalit is represented by edge pilasters rather than a solid wall over guest windows.
        var ris=GameObject.Find("Central facade risalit");if(ris)UnityEngine.Object.DestroyImmediate(ris);
        foreach(float x in new[]{-8f,8f})B(root,"Central tall pilaster",M.stucco,x,63f,-21.32f,.72f,108f,.72f,false,1.8f);
        B(root,"Central pavilion body",M.stucco,0,122.2f,-17.3f,16f,7.0f,6.0f,true,1.8f);
        B(root,"Central pavilion glass",M.glass,0,123.0f,-20.35f,6.0f,3.5f,.08f,false);
        // triangular suggestion from two roof beams
        B(root,"Central pediment left slope",M.stucco,-4f,126.5f,-20.55f,9.0f,.48f,.7f,false,1.8f).transform.localRotation=Quaternion.Euler(0,0,19f);
        B(root,"Central pediment right slope",M.stucco,4f,126.5f,-20.55f,9.0f,.48f,.7f,false,1.8f).transform.localRotation=Quaternion.Euler(0,0,-19f);
        AddBlockSign("HOTEL SOBCZAK",0,8.25f,-21.58f);
    }

    static readonly Dictionary<char,string[]> Font=new Dictionary<char,string[]>{
        {'A',new[]{"01110","10001","10001","11111","10001","10001","10001"}},
        {'B',new[]{"11110","10001","10001","11110","10001","10001","11110"}},
        {'C',new[]{"01111","10000","10000","10000","10000","10000","01111"}},
        {'E',new[]{"11111","10000","10000","11110","10000","10000","11111"}},
        {'H',new[]{"10001","10001","10001","11111","10001","10001","10001"}},
        {'K',new[]{"10001","10010","10100","11000","10100","10010","10001"}},
        {'L',new[]{"10000","10000","10000","10000","10000","10000","11111"}},
        {'O',new[]{"01110","10001","10001","10001","10001","10001","01110"}},
        {'S',new[]{"01111","10000","10000","01110","00001","00001","11110"}},
        {'T',new[]{"11111","00100","00100","00100","00100","00100","00100"}},
        {'Z',new[]{"11111","00001","00010","00100","01000","10000","11111"}}
    };
    static void AddBlockSign(string text,float cx,float cy,float z)
    {
        float px=.12f,gap=.035f,charW=5*px+4*gap,space=.38f;
        float total=text.Sum(c=>c==' '?space:charW+.18f)-.18f,x0=cx-total*.5f;
        float cursor=x0;
        foreach(char c in text)
        {
            if(c==' '){cursor+=space;continue;}
            var rows=Font[c];
            for(int r=0;r<7;r++)for(int col=0;col<5;col++)if(rows[r][col]=='1')
                B(root,"HOTEL SOBCZAK 3D letter "+c,M.brass,cursor+col*(px+gap),cy+(3-r)*(px+gap),z,px,px,.12f,false);
            cursor+=charW+.18f;
        }
    }

    static void BuildFacadePilasters()
    {
        // 16 x 4m rhythm from reference; actual room zones keep seven 7m bays above.
        for(float x=-30f;x<=30f;x+=4f)
        {
            B(root,"Front classical facade pilaster",M.stucco,x,58.8f,-21.30f,.26f,117.6f,.34f,false,1.8f);
            B(root,"Rear classical facade pilaster",M.stucco,x,58.8f,21.30f,.26f,117.6f,.34f,false,1.8f);
        }
        B(root,"Main top cornice front",M.stucco,0,117.45f,-21.42f,65.0f,.55f,.75f,false,1.8f);
        B(root,"Main top cornice rear",M.stucco,0,117.45f,21.42f,65.0f,.55f,.75f,false,1.8f);
    }
}

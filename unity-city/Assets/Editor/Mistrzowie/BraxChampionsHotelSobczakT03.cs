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
    public const float CentreX=1030f,CentreZ=710f,Width=64f,Depth=42f,Height=130f;
    public const float B2=-7.2f,B1=-3.6f,Ground=0f,First=6f,GuestBase=9.6f,GuestTop=117.6f,RoofTop=130f;
    public const int GuestFloors=30,RoomsPerFloor=14,TotalRooms=420,ParkingPerBasement=40,Elevators=4,Stairs=2;
    const string Out="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    const string RootName="T03 | HOTEL SOBCZAK | 5* | COMPLETE";
    static float K=>BraxClaudeOsiedle.K;
    static BraxChampionsHotelSobczakMaterials.Set M;
    static Transform root,site,basements,publicFloors,guestExterior,guestInteriors,cores,roof;
    static readonly List<GameObject> guestFloorRoots=new List<GameObject>();
    static readonly List<GameObject> basementFloorRoots=new List<GameObject>();
    static int roomsBuilt,parkingBuilt,elevatorsBuilt,stairsBuilt,serial;

    [Serializable] sealed class Report
    {
        public string status="GENERATED_VISUAL_REVIEW_PENDING";
        public string id="T03",name="HOTEL SOBCZAK / 5*";
        public float width=Width,depth=Depth,height=Height,centreX=CentreX,centreZ=CentreZ;
        public int physicalLevels=34,guestFloors=GuestFloors,rooms,parking,elevators,stairs;
        public string roomLayout="7 SOUTH + 7 NORTH; bay 6.00 x 19.40 m; sleeping zone 3.50 x 7.00 m; central corridor 2.40 m; WEST/EAST cores 10.60 m";
        public string materials="official Poly Haven 4K sources -> Android max 2K ASTC 4x4";
        public bool apkBuilt=false,visualAccepted=false;
    }

    public static void Generate()
    {
        if(Mathf.Abs(K-(float)(2.33/1.80))>.01f)Debug.LogWarning("HOTEL SOBCZAK uses project K="+K);
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Out+"/Materials");
        M=BraxChampionsHotelSobczakMaterials.Create();
        var existing=GameObject.Find(RootName);if(existing)UnityEngine.Object.DestroyImmediate(existing);
        serial=roomsBuilt=parkingBuilt=elevatorsBuilt=stairsBuilt=0;guestFloorRoots.Clear();basementFloorRoots.Clear();

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
        streamer.guestInteriors=guestFloorRoots.ToArray();streamer.basementInteriors=basementFloorRoots.ToArray();streamer.metreScale=K;
        streamer.guestBaseY=GuestBase;streamer.floorHeight=3.6f;streamer.halfWidth=Width*.5f;streamer.halfDepth=Depth*.5f;
        streamer.basementHalfWidth=43f;streamer.basementHalfDepth=38f;

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
    static void SlabWithCoreOpenings(Transform p,string name,Material mat,float y,float h,float tile)
    {
        // 63.20 x 41.20 m internal slab, segmented around two stair wells and the four-lift bank.
        // WEST/EAST stair wells: 5.40 x 4.50 m. Central lift-bank opening: 11.80 x 3.10 m.
        const float halfW=31.60f,halfD=20.60f,stairOuter=29.00f,stairInner=23.60f;
        const float stairZ=2.25f,liftX=5.90f,liftZ=1.55f;
        float northD=halfD-stairZ,southD=northD;
        B(p,name+" north strip",mat,0,y,(halfD+stairZ)*.5f,63.20f,h,northD,true,tile);
        B(p,name+" south strip",mat,0,y,-(halfD+stairZ)*.5f,63.20f,h,southD,true,tile);

        B(p,name+" far west",mat,-(halfW+stairOuter)*.5f,y,0,halfW-stairOuter,h,stairZ*2f,true,tile);
        B(p,name+" west-to-lift",mat,( -stairInner-liftX)*.5f,y,0,stairInner-liftX,h,stairZ*2f,true,tile);
        B(p,name+" lift-to-east",mat,( liftX+stairInner)*.5f,y,0,stairInner-liftX,h,stairZ*2f,true,tile);
        B(p,name+" far east",mat,(halfW+stairOuter)*.5f,y,0,halfW-stairOuter,h,stairZ*2f,true,tile);

        float liftBand=(stairZ-liftZ);
        B(p,name+" lift south bridge",mat,0,y,-(stairZ+liftZ)*.5f,liftX*2f,h,liftBand,true,tile);
        B(p,name+" lift north bridge",mat,0,y,(stairZ+liftZ)*.5f,liftX*2f,h,liftBand,true,tile);
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
        // Owner reserve: local X -43..+43, Z -38..+38. Building front is Z=-21.
        B(site,"T03 granite entrance plaza",M.graniteOutdoor,0,.035f,-28.5f,28f,.07f,11f,true,2f);
        B(site,"T03 drop-off asphalt",M.asphalt,0,.025f,-35f,22f,.05f,5.5f,true,2.5f);
        B(site,"T03 red carpet arrival",M.carpetRed,0,.075f,-27.0f,4.2f,.05f,10f,false);

        // Parking access runs ONLY along EAST side of T03, never across the representative entrance.
        // Each slope is 24.00 m at 15%, with 1.50 m transition zones at both ends.
        BuildRamp(site,"T03 EAST ramp G -> B1",36.25f,-23.0f,-1.80f,6.5f,24.0f,8.5308f,M.antiSlip);
        B(site,"T03 EAST ramp G-B1 top transition",M.antiSlip,36.25f,.02f,-35.75f,6.5f,.08f,1.5f,true,2.5f);
        B(site,"T03 EAST ramp G-B1 bottom transition",M.antiSlip,36.25f,-3.58f,-10.25f,6.5f,.08f,1.5f,true,2.5f);

        BuildRamp(site,"T03 EAST ramp B1 -> B2",36.25f,0.0f,-5.40f,6.5f,24.0f,-8.5308f,M.antiSlip);
        B(site,"T03 EAST ramp B1-B2 top transition",M.antiSlip,36.25f,-3.58f,12.75f,6.5f,.08f,1.5f,true,2.5f);
        B(site,"T03 EAST ramp B1-B2 bottom transition",M.antiSlip,36.25f,-7.18f,-12.75f,6.5f,.08f,1.5f,true,2.5f);

        foreach(float x in new[]{-16f,16f})
        {
            Cyl(site,"T03 fountain stone basin",M.graniteOutdoor,x,.20f,-31f,5.6f,.38f,true);
            Cyl(site,"T03 fountain water",M.water,x,.42f,-31f,4.9f,.035f,false);
            Cyl(site,"T03 fountain central jet pedestal",M.marble,x,.72f,-31f,.45f,.65f,false);
        }
        foreach(float x in new[]{-25f,-19f,19f,25f})
        {
            Cyl(site,"T03 premium planter",M.sandstone,x,.45f,-27.5f,1.7f,.9f,true);
            Cyl(site,"T03 premium shrub",M.stucco,x,1.25f,-27.5f,1.15f,1.0f,false);
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
        basementFloorRoots.Add(BuildParkingLevel("B2",B2,basements));
        basementFloorRoots.Add(BuildParkingLevel("B1",B1,basements));
        // Short horizontal connectors from EAST-side ramps into real garage openings.
        B(basements,"B1 EAST ramp connector",M.antiSlip,34.0f,-3.48f,-10.25f,4.0f,.20f,6.5f,true,2.5f);
        B(basements,"B1 EAST internal turning apron",M.antiSlip,25.0f,-3.47f,1.0f,12.0f,.20f,10.0f,true,2.5f);
        B(basements,"B1 EAST second-ramp connector",M.antiSlip,34.0f,-3.48f,12.75f,4.0f,.20f,6.5f,true,2.5f);
        B(basements,"B2 EAST ramp connector",M.antiSlip,34.0f,-7.08f,-12.75f,4.0f,.20f,6.5f,true,2.5f);
    }
    static GameObject BuildParkingLevel(string id,float y,Transform p)
    {
        var g=Group(p,"T03 "+id+" | 40 PARKING SPACES");
        SlabWithCoreOpenings(g,id+" floor",M.garage,y+.15f,.30f,3f);
        B(g,id+" north wall",M.concrete,0,y+1.8f,20.8f,64f,3.6f,.40f,true,3f);
        B(g,id+" west wall",M.concrete,-31.8f,y+1.8f,0,.40f,3.6f,42f,true,3f);
        B(g,id+" south wall",M.concrete,0,y+1.8f,-20.8f,64f,3.6f,.40f,true,3f);

        // EAST wall has real 6.50m vehicle openings aligned with the ramp connectors.
        if(id=="B1")
        {
            B(g,id+" east wall S1",M.concrete,31.8f,y+1.8f,-17.1f,.40f,3.6f,7.4f,true,3f);
            B(g,id+" east wall MID",M.concrete,31.8f,y+1.8f,1.25f,.40f,3.6f,16.5f,true,3f);
            B(g,id+" east wall N1",M.concrete,31.8f,y+1.8f,18.1f,.40f,3.6f,5.4f,true,3f);
        }
        else
        {
            B(g,id+" east wall S1",M.concrete,31.8f,y+1.8f,-18.0f,.40f,3.6f,5.6f,true,3f);
            B(g,id+" east wall N1",M.concrete,31.8f,y+1.8f,4.2f,.40f,3.6f,33.2f,true,3f);
        }
        SlabWithCoreOpenings(g,id+" ceiling",M.concrete,y+3.54f,.12f,3f);
        B(g,id+" central drive aisle",M.garage,0,y+.315f,0,56f,.025f,6f,false,3f);

        for(int row=0;row<2;row++)
        {
            float z=row==0?-5.6f:5.6f;
            var widths=new List<float>();
            for(int i=0;i<20;i++)
                widths.Add(id=="B1" && row==0 && i<4 ? 3.6f : 2.6f);
            float total=widths.Sum();
            float cursor=-total*.5f;
            for(int i=0;i<20;i++)
            {
                float w=widths[i],x=cursor+w*.5f;cursor+=w;
                string kind=(id=="B1"&&row==0&&i<4)?" ACCESSIBLE":"";
                B(g,id+" PARK "+(row*20+i+1).ToString("D2")+kind+" back",M.parkingPaint,x,y+.332f,z+(row==0?-2.56f:2.56f),w-.12f,.018f,.07f,false);
                B(g,id+" PARK line L",M.parkingPaint,x-w*.5f+.03f,y+.332f,z,.06f,.018f,5.12f,false);
                B(g,id+" PARK line R",M.parkingPaint,x+w*.5f-.03f,y+.332f,z,.06f,.018f,5.12f,false);
                parkingBuilt++;
            }
        }
        B(g,id+" technical room floor",M.antiSlip,-14f,y+.335f,15f,18f,.03f,7f,false,2.5f);
        B(g,id+" storage floor",M.antiSlip,12f,y+.335f,-15f,16f,.03f,7f,false,2.5f);
        Label(g,id+" 40 MIEJSC",0,y+1.0f,19.8f,.28f,180);
        g.gameObject.SetActive(false);
        return g.gameObject;
    }

    // ---------------------------------------------------------------- PUBLIC GROUND
    static void BuildGroundFloor()
    {
        var g=Group(publicFloors,"PARTER 0.00-6.00 | lobby reception cafe");
        SlabWithCoreOpenings(g,"Ground structural slab",M.marble,.15f,.30f,1.5f);
        SlabWithCoreOpenings(g,"Ground clean ceiling",M.stucco,5.94f,.12f,1.8f);
        BuildPublicFacade(g,0f,6f,2.4f,4.2f);

        // Owner-approved exact local zones.
        Area(g,"WIATROLAP 40m2",M.marble,0,.17f,-18.10f,8f,5f);
        Area(g,"RECEPCJA 60m2",M.marble,0,.17f,-12.30f,12f,5f);
        Area(g,"LOBBY 350m2",M.marble,0,.18f,-1.50f,25f,14f);
        Area(g,"BAR CAFE LOUNGE 180m2",M.parquet,0,.19f,10.0f,30f,6f);
        Area(g,"TOALETA M 40m2",M.marble,-26f,.19f,17.6f,8f,5f);
        Area(g,"TOALETA K 40m2",M.marble,26f,.19f,17.6f,8f,5f);
        Area(g,"BAGAZ 50m2",M.oak,-26f,.19f,-12.5f,10f,5f);
        Area(g,"CONCIERGE 40m2",M.marble,26f,.19f,-12.5f,8f,5f);

        // Vestibule side/rear partitions with a real 3.60m clear passage.
        B(g,"Vestibule west glass",M.glass,-4.0f,2.0f,-18.1f,.08f,3.8f,5f,false);
        B(g,"Vestibule east glass",M.glass,4.0f,2.0f,-18.1f,.08f,3.8f,5f,false);
        B(g,"Vestibule rear glass L",M.glass,-2.9f,2.0f,-15.6f,2.2f,3.8f,.08f,false);
        B(g,"Vestibule rear glass R",M.glass,2.9f,2.0f,-15.6f,2.2f,3.8f,.08f,false);

        B(g,"Reception oak body",M.oak,0,.68f,-12.3f,11.6f,1.10f,.82f,true,1.2f);
        B(g,"Reception marble top",M.marble,0,1.27f,-12.3f,12.0f,.12f,.90f,true,1.5f);
        for(float x=-5.2f;x<=5.2f;x+=2.6f)B(g,"Reception brass division",M.brass,x,.72f,-12.73f,.06f,1.0f,.05f,false);

        foreach(float x in new[]{-7.5f,0f,7.5f})
        {
            B(g,"Lobby leather sofa",M.leather,x,.55f,-2.0f,4.6f,1.0f,1.45f,true,.3f);
            B(g,"Lobby marble coffee table",M.marble,x,.46f,.5f,2.2f,.18f,1.2f,true,1.5f);
        }

        B(g,"Cafe oak bar body",M.oak,0,.70f,10.8f,13f,1.15f,.90f,true,1.2f);
        B(g,"Cafe marble bar top",M.marble,0,1.32f,10.8f,13.4f,.12f,1.08f,true,1.5f);
        for(float x=-5.5f;x<=5.5f;x+=1.8f)Cyl(g,"Cafe brass stool",M.brass,x,.50f,9.3f,.42f,.90f,true);

        BuildServiceRoom(g,"TOALETA M",-26f,17.6f,8f,5f,6f,M.marble);
        BuildServiceRoom(g,"TOALETA K",26f,17.6f,8f,5f,6f,M.marble);
        BuildServiceRoom(g,"BAGAZ",-26f,-12.5f,10f,5f,6f,M.oak);
        BuildServiceRoom(g,"CONCIERGE",26f,-12.5f,8f,5f,6f,M.marble);

        // BOH functions required by the order.
        BuildServiceRoom(g,"HOUSEKEEPING",-25f,6f,7f,5f,6f,M.oak);
        BuildServiceRoom(g,"MAGAZYN",25f,6f,7f,5f,6f,M.oak);
        BuildServiceRoom(g,"ZAPLECZE RECEPCJI",0,-7.2f,10f,3.2f,6f,M.oak);

        // Public WC fixtures, luggage storage and concierge equipment.
        foreach(float x in new[]{-28f,-26f,-24f})
        {
            Cyl(g,"WC M toilet",M.whitePaint,x,.38f,18.2f,.55f,.70f,true);
            B(g,"WC M privacy panel",M.stucco,x+.75f,1.10f,18.2f,.08f,2.20f,1.20f,true,1.8f);
        }
        foreach(float x in new[]{24f,26f,28f})
        {
            Cyl(g,"WC K toilet",M.whitePaint,x,.38f,18.2f,.55f,.70f,true);
            B(g,"WC K privacy panel",M.stucco,x-.75f,1.10f,18.2f,.08f,2.20f,1.20f,true,1.8f);
        }
        B(g,"WC M marble vanity",M.marble,-26f,.78f,16.2f,4.5f,.16f,.62f,true,1.5f);
        B(g,"WC K marble vanity",M.marble,26f,.78f,16.2f,4.5f,.16f,.62f,true,1.5f);
        for(float z=-13.8f;z<=-11.2f;z+=1.3f)B(g,"Baggage oak rack",M.oak,-26f,1.1f,z,7.5f,2.0f,.45f,true,1.2f);
        B(g,"Concierge oak desk",M.oak,26f,.65f,-12.5f,5.5f,1.05f,.80f,true,1.2f);
        B(g,"Concierge marble top",M.marble,26f,1.22f,-12.5f,5.7f,.10f,.90f,true,1.5f);
        Label(g,"HOTEL SOBCZAK",0,4.4f,-20.4f,.34f,0);
    }

    static void BuildFirstFloor()
    {
        var g=Group(publicFloors,"PIETRO 1 6.00-9.60 | restaurant kitchen spa fitness");
        SlabWithCoreOpenings(g,"Level1 structural slab",M.marble,6.15f,.30f,1.5f);
        SlabWithCoreOpenings(g,"Level1 ceiling",M.stucco,9.54f,.12f,1.8f);
        BuildPublicFacade(g,6f,3.6f,1.8f,2.2f);
        Area(g,"RESTAURACJA 250m2",M.parquet,-17f,6.17f,-10f,25f,10f);
        Area(g,"KUCHNIA 180m2",M.tile,-18f,6.17f,10f,20f,9f);
        Area(g,"SPA 350m2",M.terrazzo,17f,6.17f,8f,25f,14f);
        Area(g,"FITNESS 250m2",M.rubber,17f,6.17f,-10f,25f,10f);

        BuildLevelZone(g,"RESTAURACJA",-17f,-10f,25f,10f,6f,3.6f,true,1.8f);
        BuildLevelZone(g,"KUCHNIA",-18f,10f,20f,9f,6f,3.6f,false,1.4f);
        BuildLevelZone(g,"SPA",17f,8f,25f,14f,6f,3.6f,false,1.4f);
        BuildLevelZone(g,"FITNESS",17f,-10f,25f,10f,6f,3.6f,true,1.8f);

        // Restaurant tables.
        for(float x=-27f;x<=-7f;x+=5f)for(float z=-12.5f;z<=-7.5f;z+=5f)
        {
            Cyl(g,"Restaurant marble table",M.marble,x,6.75f,z,1.25f,.12f,true);
            for(float dx=-1;dx<=1;dx+=2)B(g,"Restaurant leather chair",M.leather,x+dx,6.55f,z,.65f,.90f,.65f,true,.3f);
        }
        // Buffet and kitchen: hot line, prep, dishwash, cold store and storage.
        B(g,"Restaurant buffet oak body",M.oak,-17f,6.70f,-6.3f,8.0f,1.0f,.90f,true,1.2f);
        B(g,"Restaurant buffet marble top",M.marble,-17f,7.25f,-6.3f,8.2f,.10f,1.0f,true,1.5f);
        for(float x=-26f;x<=-10f;x+=4f)B(g,"Kitchen hot stainless bench",M.steel,x,6.65f,8.3f,3.0f,1.0f,.85f,true);
        B(g,"Kitchen prep island",M.steel,-18f,6.65f,11.0f,5.5f,1.0f,1.2f,true);
        B(g,"Kitchen dishwash line",M.steel,-24.5f,6.65f,13.0f,4.5f,1.0f,.85f,true);
        B(g,"Kitchen cold room",M.steel,-12.0f,7.45f,12.4f,4.0f,2.7f,3.0f,true);
        B(g,"Kitchen dry storage rack",M.oak,-26.0f,7.30f,6.5f,3.0f,2.2f,.55f,true,1.2f);
        // SPA treatment beds and glass screen.
        for(float x=8f;x<=26f;x+=6f)B(g,"SPA white leather treatment bed",M.leatherWhite,x,6.55f,9f,2.2f,.55f,.85f,true,.3f);
        B(g,"SPA clear glass relaxation divider",M.glass,17f,7.55f,4f,20f,2.7f,.06f,false);
        B(g,"SPA reception oak desk",M.oak,8.0f,6.65f,2.7f,4.0f,1.0f,.80f,true,1.2f);
        B(g,"SPA wet-zone marble floor",M.marble,24f,6.19f,12f,8f,.035f,5f,false,1.5f);
        for(float x=21f;x<=27f;x+=3f)B(g,"SPA shower glass",M.glass,x,7.45f,13.5f,2.0f,2.4f,.06f,false);
        for(float x=10f;x<=22f;x+=4f)B(g,"SPA relaxation lounger",M.leatherWhite,x,6.50f,5.2f,2.0f,.45f,.80f,true,.3f);
        B(g,"SPA changing lockers",M.oak,26.5f,7.25f,3.0f,1.2f,2.4f,5.0f,true,1.2f);
        // Fitness equipment proxies, realistic dimensions.
        for(float x=8f;x<=26f;x+=4.5f)
        {
            B(g,"Fitness treadmill deck",M.darkBronze,x,6.35f,-11f,.85f,.22f,2.0f,true);
            B(g,"Fitness treadmill console",M.darkBronze,x,7.20f,-10.2f,.75f,.85f,.14f,true);
        }
        for(float x=9f;x<=18f;x+=4.5f)
        {
            Cyl(g,"Fitness exercise bike flywheel",M.darkBronze,x,6.70f,-7.5f,.65f,.22f,true);
            B(g,"Fitness exercise bike frame",M.darkBronze,x,7.05f,-7.5f,.12f,1.1f,1.0f,true);
        }
        B(g,"Fitness free-weight rack",M.darkBronze,25f,7.05f,-7.2f,5.0f,1.6f,.55f,true);
        for(float x=23f;x<=27f;x+=1f)Cyl(g,"Fitness dumbbell",M.steel,x,6.45f,-7.3f,.28f,.70f,true);
        B(g,"Fitness flat bench",M.leather,20f,6.55f,-12.2f,1.8f,.45f,.65f,true,.3f);
    }

    static void BuildLevelZone(Transform g,string name,float x,float z,float w,float d,float baseY,float h,bool doorNorth,float doorW)
    {
        float wallY=baseY+h*.5f,t=.14f,doorH=2.20f,seg=(w-doorW)*.5f;
        B(g,name+" west wall",M.stucco,x-w*.5f,wallY,z,t,h,d,true,1.8f);
        B(g,name+" east wall",M.stucco,x+w*.5f,wallY,z,t,h,d,true,1.8f);
        if(doorNorth)
        {
            B(g,name+" south wall",M.stucco,x,wallY,z-d*.5f,w,h,t,true,1.8f);
            B(g,name+" north left",M.stucco,x-(doorW*.5f+seg*.5f),wallY,z+d*.5f,seg,h,t,true,1.8f);
            B(g,name+" north right",M.stucco,x+(doorW*.5f+seg*.5f),wallY,z+d*.5f,seg,h,t,true,1.8f);
            B(g,name+" north door header",M.stucco,x,baseY+(doorH+h)*.5f,z+d*.5f,doorW,h-doorH,t,true,1.8f);
        }
        else
        {
            B(g,name+" north wall",M.stucco,x,wallY,z+d*.5f,w,h,t,true,1.8f);
            B(g,name+" south left",M.stucco,x-(doorW*.5f+seg*.5f),wallY,z-d*.5f,seg,h,t,true,1.8f);
            B(g,name+" south right",M.stucco,x+(doorW*.5f+seg*.5f),wallY,z-d*.5f,seg,h,t,true,1.8f);
            B(g,name+" south door header",M.stucco,x,baseY+(doorH+h)*.5f,z-d*.5f,doorW,h-doorH,t,true,1.8f);
        }
    }

    static void Area(Transform g,string name,Material mat,float x,float y,float z,float w,float d)
        =>B(g,name+" floor",mat,x,y,z,w,.035f,d,false,mat==M.marble?1.5f:mat==M.parquet?3.4f:2f);

    static void BuildServiceRoom(Transform g,string name,float x,float z,float w,float d,float h,Material floor)
    {
        float y=h*.5f,doorW=1.0f,doorH=2.20f,seg=(w-doorW)*.5f;
        B(g,name+" west wall",M.stucco,x-w*.5f,y,z,.14f,h,d,true,1.8f);
        B(g,name+" east wall",M.stucco,x+w*.5f,y,z,.14f,h,d,true,1.8f);
        B(g,name+" north wall",M.stucco,x,y,z+d*.5f,w,h,.14f,true,1.8f);
        B(g,name+" south left",M.stucco,x-(doorW*.5f+seg*.5f),y,z-d*.5f,seg,h,.14f,true,1.8f);
        B(g,name+" south right",M.stucco,x+(doorW*.5f+seg*.5f),y,z-d*.5f,seg,h,.14f,true,1.8f);
        B(g,name+" door header",M.stucco,x,(doorH+h)*.5f,z-d*.5f,doorW,h-doorH,.14f,true,1.8f);
        B(g,name+" open door leaf",M.oak,x-doorW*.46f,doorH*.5f,z-d*.5f+.48f,.06f,doorH,.94f,false,1.2f);
    }

    static void BuildPublicFacade(Transform p,float baseY,float floorH,float windowW,float windowH)
    {
        float sill=Mathf.Max(.35f,(floorH-windowH)*.45f),header=floorH-sill-windowH;
        foreach(float zsign in new[]{-1f,1f})
        {
            float z=zsign*20.80f;
            for(int i=0;i<7;i++)
            {
                float x=-18f+i*6f;
                if(baseY==0f && zsign<0f && i==3)
                {
                    float side=(6f-3.6f)*.5f;
                    B(p,"Main entrance facade jamb L",M.facade,x-(1.8f+side*.5f),baseY+floorH*.5f,z,side,floorH,.40f,true,3f);
                    B(p,"Main entrance facade jamb R",M.facade,x+(1.8f+side*.5f),baseY+floorH*.5f,z,side,floorH,.40f,true,3f);
                    B(p,"Main entrance facade header",M.facade,x,baseY+5.10f,z,3.6f,1.8f,.40f,true,3f);
                }
                else
                {
                    WindowBay(p,"Public facade",x,baseY,z,6f,floorH,windowW,windowH,sill,header,zsign);
                    if(baseY==0f)BuildArchTrim(p,x,baseY,z,windowW,windowH,sill,zsign);
                }
            }
            B(p,"Public facade west service wing",M.facade,-26.30f,baseY+floorH*.5f,z,10.6f,floorH,.40f,true,3f);
            B(p,"Public facade east service wing",M.facade,26.30f,baseY+floorH*.5f,z,10.6f,floorH,.40f,true,3f);
        }
        foreach(float xsign in new[]{-1f,1f})
            B(p,"Public side facade",M.facade,xsign*31.80f,baseY+floorH*.5f,0,.40f,floorH,42f,true,3f);
    }
    static void BuildArchTrim(Transform p,float x,float y0,float z,float winW,float winH,float sill,float outward)
    {
        // Real 3D arch surround: 9 stone segments following a semicircle, not a flat texture.
        float r=winW*.5f,cy=y0+sill+winH-r;
        for(int i=0;i<9;i++)
        {
            float a=Mathf.Lerp(0f,180f,i/8f)*Mathf.Deg2Rad;
            float px=x+Mathf.Cos(a)*r,py=cy+Mathf.Sin(a)*r;
            var seg=B(p,"Ground arch 3D segment",M.stucco,px,py,z+outward*.24f,.34f,.20f,.24f,false,1.8f);
            seg.transform.localRotation=Quaternion.Euler(0,0,-Mathf.Cos(a)*58f);
        }
    }

    // ---------------------------------------------------------------- GUEST EXTERIOR
    static void BuildGuestExteriorAndStructure()
    {
        for(int f=0;f<GuestFloors;f++)
        {
            float y0=GuestBase+f*3.6f;
            SlabWithCoreOpenings(guestExterior,"Guest slab "+(f+2),M.marble,y0+.14f,.28f,1.5f);
            SlabWithCoreOpenings(guestExterior,"Guest ceiling finish "+(f+2),M.stucco,y0+3.54f,.12f,1.8f);
            foreach(float zsign in new[]{-1f,1f})
            {
                float z=zsign*20.80f;
                for(int i=0;i<7;i++)
                    DoubleWindowBay(guestExterior,"Guest facade F"+(f+2),-18f+i*6f,y0,z,6f,3.6f,1.45f,2.20f,.85f,zsign);
                B(guestExterior,"Guest WEST service core facade F"+(f+2),M.facade,-26.30f,y0+1.8f,z,10.6f,3.6f,.40f,true,3f);
                B(guestExterior,"Guest EAST service core facade F"+(f+2),M.facade,26.30f,y0+1.8f,z,10.6f,3.6f,.40f,true,3f);
            }
            foreach(float xsign in new[]{-1f,1f})
                B(guestExterior,"Guest side wall F"+(f+2),M.facade,xsign*31.80f,y0+1.8f,0,.40f,3.6f,42f,true,3f);
        }
        foreach(float y in new[]{6f,9.6f})
        {
            B(guestExterior,"Front classical cornice "+y,M.stucco,0,y,-21.18f,64f,.35f,.45f,false,1.8f);
            B(guestExterior,"Rear classical cornice "+y,M.stucco,0,y,21.18f,64f,.35f,.45f,false,1.8f);
        }
    }
    static void DoubleWindowBay(Transform p,string prefix,float x,float y0,float z,float bayW,float floorH,float winW,float winH,float sill,float outward)
    {
        float gap=.75f,side=(bayW-(2f*winW+gap))*.5f,header=floorH-sill-winH;
        B(p,prefix+" bay left masonry",M.facade,x-bayW*.5f+side*.5f,y0+floorH*.5f,z,side,floorH,.40f,true,3f);
        B(p,prefix+" bay middle masonry",M.facade,x,y0+floorH*.5f,z,gap,floorH,.40f,true,3f);
        B(p,prefix+" bay right masonry",M.facade,x+bayW*.5f-side*.5f,y0+floorH*.5f,z,side,floorH,.40f,true,3f);
        B(p,prefix+" sill masonry",M.facade,x,y0+sill*.5f,z,bayW-side*2f,sill,.40f,true,3f);
        B(p,prefix+" header masonry",M.facade,x,y0+floorH-header*.5f,z,bayW-side*2f,header,.40f,true,3f);
        float offset=(gap+winW)*.5f;
        foreach(float wx in new[]{x-offset,x+offset})
        {
            float gy=y0+sill+winH*.5f,gz=z+outward*.22f;
            B(p,prefix+" glass",M.glass,wx,gy,gz,winW-.12f,winH-.10f,.055f,false);
            B(p,prefix+" frame top",M.darkBronze,wx,gy+winH*.5f,gz+outward*.04f,winW+.10f,.10f,.08f,false);
            B(p,prefix+" frame bottom",M.darkBronze,wx,gy-winH*.5f,gz+outward*.04f,winW+.10f,.10f,.08f,false);
            B(p,prefix+" frame left",M.darkBronze,wx-winW*.5f,gy,gz+outward*.04f,.10f,winH,.08f,false);
            B(p,prefix+" frame right",M.darkBronze,wx+winW*.5f,gy,gz+outward*.04f,.10f,winH,.08f,false);
        }
    }
    static void WindowBay(Transform p,string prefix,float x,float y0,float z,float bayW,float floorH,float winW,float winH,float sill,float header,float outward)
    {
        float side=(bayW-winW)*.5f;
        float zwall=z;
        B(p,prefix+" jamb L",M.facade,x-(winW*.5f+side*.5f),y0+floorH*.5f,zwall,side,floorH,.40f,true,3f);
        B(p,prefix+" jamb R",M.facade,x+(winW*.5f+side*.5f),y0+floorH*.5f,zwall,side,floorH,.40f,true,3f);
        B(p,prefix+" sill",M.facade,x,y0+sill*.5f,zwall,winW,sill,.40f,true,3f);
        B(p,prefix+" header",M.facade,x,y0+floorH-header*.5f,zwall,winW,header,.40f,true,3f);
        float gy=y0+sill+winH*.5f,gz=zwall+outward*.20f;
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

            // One central corridor exactly 2.40 m clear, running along X.
            B(fg,"Central corridor wool runner",M.carpet,0,.16f,0,63.0f,.035f,2.40f,false,.3f);
            B(fg,"WEST service core floor",M.marble,-26.30f,.16f,0,10.6f,.035f,8.0f,false,1.5f);
            B(fg,"EAST service core floor",M.marble,26.30f,.16f,0,10.6f,.035f,8.0f,false,1.5f);

            for(int i=0;i<7;i++)
            {
                float x=-18f+i*6f;
                BuildGuestRoom(fg,f+2,i+1,x,-10.90f,-1);
                BuildGuestRoom(fg,f+2,14-i,x,10.90f,1);
            }
            // One 0.20m party wall per actual boundary: no duplicated overlapping walls.
            foreach(float wallX in new[]{-21f,-15f,-9f,-3f,3f,9f,15f,21f})
            {
                B(fg,"SOUTH room party wall",M.stucco,wallX,1.70f,-10.90f,.20f,3.4f,19.40f,true,1.8f);
                B(fg,"NORTH room party wall",M.stucco,wallX,1.70f,10.90f,.20f,3.4f,19.40f,true,1.8f);
            }
            B(fg,"Housekeeping WEST",M.oak,-26.3f,.55f,5.2f,5.0f,1.0f,3.0f,true,1.2f);
            B(fg,"Linen EAST",M.oak,26.3f,.55f,5.2f,5.0f,1.0f,3.0f,true,1.2f);
            fg.gameObject.SetActive(false);
        }
    }
    static void BuildGuestRoom(Transform p,int floor,int localNumber,float x,float z,float side)
    {
        string number=(floor*100+localNumber).ToString();
        var g=Group(p,"ROOM "+number+" | bay 6.00x19.40 | sleep module 3.50x7.00");
        float innerZ=side*1.20f,outerZ=side*20.60f,centreZ=side*10.90f;
        B(g,"Room oak floor",M.oak,x,.15f,centreZ,5.80f,.05f,19.20f,false,1.2f);

        // 0.18 m corridor wall with a true 1.00 x 2.20 m opening.
        B(g,"Room corridor wall left",M.stucco,x-1.75f,1.70f,innerZ,2.50f,3.4f,.18f,true,1.8f);
        B(g,"Room corridor wall right",M.stucco,x+1.75f,1.70f,innerZ,2.50f,3.4f,.18f,true,1.8f);
        B(g,"Room door header",M.stucco,x,2.90f,innerZ,1.0f,1.40f,.18f,true,1.8f);
        B(g,"Room brass threshold",M.brass,x,.04f,innerZ-side*.11f,1.0f,.04f,.18f,false);
        B(g,"Room oak door leaf OPEN",M.oak,x-.47f,1.10f,innerZ+side*.47f,.06f,2.20f,.94f,false,1.2f);
        Label(g,number,x,2.55f,innerZ-side*.11f,.11f,side>0?180:0);

        // Owner's exact 3.50 x 7.00 sleeping zone inside the larger premium bay.
        float sleepZ=outerZ-side*6.0f;
        B(g,"Sleep zone rug 3.50x7.00",M.carpet,x,.18f,sleepZ,3.5f,.025f,7.0f,false,.3f);
        float bedZ=outerZ-side*4.0f;
        B(g,"King bed oak base",M.oak,x,.32f,bedZ,2.05f,.42f,2.15f,true,1.2f);
        B(g,"King mattress",M.whitePaint,x,.62f,bedZ,2.00f,.30f,2.10f,true);
        B(g,"Leather headboard",M.leather,x,1.20f,bedZ-side*1.10f,2.45f,1.30f,.14f,true,.3f);
        foreach(float dx in new[]{-1.45f,1.45f})
        {
            B(g,"Oak nightstand",M.oak,x+dx,.42f,bedZ,.65f,.72f,.55f,true,1.2f);
            B(g,"Brass bedside lamp",M.brass,x+dx,1.08f,bedZ,.10f,.65f,.10f,false);
        }

        B(g,"Oak writing desk 1.70m",M.oak,x+1.95f,.60f,innerZ+side*8.0f,1.70f,.12f,.70f,true,1.2f);
        B(g,"Leather desk chair",M.leather,x+1.95f,.48f,innerZ+side*7.0f,.68f,.85f,.68f,true,.3f);
        B(g,"Oak wardrobe 1.60m",M.oak,x-1.90f,1.25f,innerZ+side*5.0f,1.60f,2.50f,.65f,true,1.2f);
        B(g,"TV panel",M.darkBronze,x+2.75f,1.55f,innerZ+side*10.0f,.08f,1.10f,1.70f,false);
        B(g,"Luggage rack",M.oak,x-1.9f,.42f,innerZ+side*8.2f,1.40f,.55f,.60f,true,1.2f);
        Cyl(g,"Room side table",M.marble,x+1.8f,.45f,innerZ+side*11.2f,.75f,.10f,true);
        B(g,"Room lounge chair",M.leather,x+2.3f,.50f,innerZ+side*12.0f,.80f,.90f,.80f,true,.3f);

        // Bathroom exactly 2.40 x 3.00 m, 0.12 m partitions, 0.80 x 2.10 m door.
        float bathX=x-1.80f,bathZ=innerZ+side*1.50f;
        B(g,"Bathroom marble floor",M.marble,bathX,.19f,bathZ,2.40f,.035f,3.00f,false,1.5f);
        B(g,"Bathroom privacy wall",M.stucco,x-.60f,1.65f,bathZ,.12f,3.3f,3.00f,true,1.8f);
        float bathDoorZ=innerZ+side*3.00f;
        B(g,"Bathroom bedroom wall left",M.stucco,x-2.60f,1.65f,bathDoorZ,.80f,3.3f,.12f,true,1.8f);
        B(g,"Bathroom bedroom wall right",M.stucco,x-1.00f,1.65f,bathDoorZ,.80f,3.3f,.12f,true,1.8f);
        B(g,"Bathroom 0.80m door header",M.stucco,x-1.80f,2.70f,bathDoorZ,.80f,1.20f,.12f,true,1.8f);
        B(g,"Bathroom brass threshold",M.brass,x-1.80f,.04f,bathDoorZ-side*.08f,.80f,.04f,.14f,false);
        B(g,"Bathroom oak door leaf OPEN",M.oak,x-2.17f,1.05f,bathDoorZ-side*.38f,.06f,2.10f,.74f,false,1.2f);
        B(g,"Bathroom glass shower 1.20x0.90",M.glass,x-2.20f,1.10f,bathZ+side*.80f,1.20f,2.0f,.06f,false);
        B(g,"Bathroom vanity",M.marble,x-1.85f,.58f,bathZ-side*.80f,1.55f,.18f,.55f,true,1.5f);
        Cyl(g,"Bathroom basin",M.whitePaint,x-1.85f,.73f,bathZ-side*.80f,.52f,.15f,true);
        Cyl(g,"Bathroom toilet",M.whitePaint,x-1.05f,.38f,bathZ+side*.48f,.55f,.70f,true);
        roomsBuilt++;
    }

    // ---------------------------------------------------------------- STAIRS
    static void BuildStairs()
    {
        foreach(float x in new[]{-26.3f,26.3f})
        {
            var s=Group(cores,"STAIR "+(x<0?"WEST":"EAST")+" | B2 TO TECH");
            BuildStairInterval(s,x,B2,B1,0);BuildStairInterval(s,x,B1,Ground,1);
            BuildStairInterval(s,x,Ground,First,0);BuildStairInterval(s,x,First,GuestBase,1);
            for(int f=0;f<GuestFloors-1;f++)BuildStairInterval(s,x,GuestBase+f*3.6f,GuestBase+(f+1)*3.6f,f%2);
            BuildStairInterval(s,x,GuestBase+(GuestFloors-1)*3.6f,GuestTop,GuestFloors%2);
            B(s,"Stair fire wall west",M.stucco,x-2.6f,55.2f,0,.22f,124.8f,9f,true,1.8f);
            B(s,"Stair fire wall east",M.stucco,x+2.6f,55.2f,0,.22f,124.8f,9f,true,1.8f);
            stairsBuilt++;
        }
    }
    static void BuildStairInterval(Transform p,float centreX,float y0,float y1,int reverse)
    {
        float rise=y1-y0;
        int perFlight=Mathf.Abs(rise-6.0f)<.05f?18:11; // 36 or 22 risers exactly.
        float stepRise=rise/(perFlight*2f);
        float tread=.29f,run=perFlight*tread;
        float startZ=reverse==0?-run*.5f:run*.5f,dir=reverse==0?1f:-1f;
        for(int i=0;i<perFlight;i++)
        {
            float z=startZ+dir*(i+.5f)*tread;
            B(p,"Stair flight A step",M.granite,centreX-.9f,y0+(i+.5f)*stepRise,z,1.5f,stepRise,tread,true,2.0f);
            float z2=-startZ-dir*(i+.5f)*tread;
            B(p,"Stair flight B step",M.granite,centreX+.9f,y0+rise*.5f+(i+.5f)*stepRise,z2,1.5f,stepRise,tread,true,2.0f);
        }
        B(p,"Stair mid landing 1.50m",M.granite,centreX,y0+rise*.5f,0,3.8f,.16f,1.5f,true,2.0f);
    }

    // ---------------------------------------------------------------- ELEVATORS
    static void BuildElevators()
    {
        float[] xs={-4.35f,-1.45f,1.45f,4.35f};
        for(int i=0;i<4;i++)BuildElevator(i,xs[i],0);
    }
    static void BuildElevator(int index,float x,float z)
    {
        bool service=index==3;
        float shaftW=service?2.90f:2.50f,shaftD=service?2.70f:2.50f;
        float cabW=service?2.20f:2.00f,cabD=service?1.80f:1.70f,doorW=service?1.20f:1.10f;
        var e=Group(cores,"ELEVATOR "+(index+1)+" | "+(service?"SERVICE":"PASSENGER"));

        B(e,"Lift shaft west",M.stucco,x-shaftW*.5f,55.2f,z,.22f,124.8f,shaftD,true,1.8f);
        B(e,"Lift shaft east",M.stucco,x+shaftW*.5f,55.2f,z,.22f,124.8f,shaftD,true,1.8f);
        B(e,"Lift shaft rear",M.stucco,x,55.2f,z+shaftD*.5f,shaftW,124.8f,.22f,true,1.8f);

        var levels=new List<float>{B2,B1,Ground,First};
        for(int f=0;f<GuestFloors;f++)levels.Add(GuestBase+f*3.6f);
        levels.Add(GuestTop);
        var names=new List<string>{"B2","B1","PARTER","1"};
        for(int n=2;n<=31;n++)names.Add(n.ToString());names.Add("TECH");
        var usable=Enumerable.Repeat(true,levels.Count).ToArray();
        if(!service)usable[usable.Length-1]=false;

        var landL=new Transform[levels.Count];var landR=new Transform[levels.Count];var calls=new Vector3[levels.Count];
        for(int n=0;n<levels.Count;n++)
        {
            float yy=levels[n],leafW=doorW*.5f,doorZ=z-shaftD*.5f-.07f;
            landL[n]=DoorLeaf(e,"Landing "+names[n]+" left",x-doorW*.25f,yy,doorZ,M.darkBronze,M.glass,leafW);
            landR[n]=DoorLeaf(e,"Landing "+names[n]+" right",x+doorW*.25f,yy,doorZ,M.darkBronze,M.glass,leafW);
            B(e,"Landing "+names[n]+" brass lintel",M.brass,x,yy+2.30f,doorZ-.03f,shaftW,.12f,.12f,false);
            calls[n]=usable[n]?root.TransformPoint(new Vector3(x,yy+1.2f,z-2.10f)*K):Vector3.zero;
        }

        var cabin=Group(e,"Moving premium cabin");
        cabin.localPosition=new Vector3(x,B2,z)*K;
        B(cabin,"Cabin marble floor",M.marble,0,.08f,0,cabW,.16f,cabD,true,1.5f);
        B(cabin,"Cabin ceiling",M.steel,0,2.55f,0,cabW,.16f,cabD,true);
        B(cabin,"Cabin rear oak panel",M.oak,0,1.3f,cabD*.5f-.05f,cabW-.10f,2.45f,.10f,true,1.2f);
        B(cabin,"Cabin west panel",M.steel,-cabW*.5f+.05f,1.3f,0,.10f,2.45f,cabD-.10f,true);
        B(cabin,"Cabin east panel",M.steel,cabW*.5f-.05f,1.3f,0,.10f,2.45f,cabD-.10f,true);
        float leaf=doorW*.5f,frontZ=-cabD*.5f-.04f;
        var cabL=DoorLeaf(cabin,"Cabin left",-doorW*.25f,0,frontZ,M.darkBronze,M.glass,leaf);
        var cabR=DoorLeaf(cabin,"Cabin right",doorW*.25f,0,frontZ,M.darkBronze,M.glass,leaf);

        var controller=e.gameObject.AddComponent<BraxWinda>();
        controller.nazwa="HOTEL SOBCZAK · WINDA "+(index+1);
        controller.kabina=cabin;controller.kabinaL=cabL;controller.kabinaP=cabR;
        controller.pietraY=levels.Select(v=>v*K).ToArray();controller.pietraNazwy=names.ToArray();controller.uzytkowe=usable;
        controller.przystanekL=landL;controller.przystanekP=landR;controller.wezwanie=calls;
        Vector3 world=root.TransformPoint(new Vector3(x,0,z)*K);controller.szyb=new Vector2(world.x,world.z);
        controller.wnetrze=new Vector2((cabW*.5f-.08f)*K,(cabD*.5f-.08f)*K);
        controller.przesuw=(doorW*.28f)*K;controller.start=2;
        elevatorsBuilt++;
    }
    static Transform DoorLeaf(Transform p,string name,float x,float y,float z,Material frame,Material glass,float leafWidth)
    {
        var t=Group(p,name);t.localPosition=new Vector3(x,y,z)*K;
        B(t,name+" glass",glass,0,1.10f,0,leafWidth,2.20f,.07f,true);
        B(t,name+" frame top",frame,0,2.20f,0,leafWidth+.06f,.08f,.09f,false);
        B(t,name+" frame edge",frame,x<0?-leafWidth*.45f:leafWidth*.45f,1.10f,0,.08f,2.20f,.09f,false);
        return t;
    }

    // ---------------------------------------------------------------- ROOF
    static void BuildRoof()
    {
        // Owner geometry: lower mansard ~67° from 117.60 to 125.20; upper roof ~15° to max 130.00.
        float lowerRise=7.60f,lowerAngle=67f,lowerLen=lowerRise/Mathf.Sin(lowerAngle*Mathf.Deg2Rad);
        float lowerRun=lowerLen*Mathf.Cos(lowerAngle*Mathf.Deg2Rad);
        float lowerCenterZ=21.0f-lowerRun*.5f;
        B(roof,"Mansard front",M.roof,0,121.40f,-lowerCenterZ,64f,.18f,lowerLen,false,3f).transform.localRotation=Quaternion.Euler(-lowerAngle,0,0);
        B(roof,"Mansard rear",M.roof,0,121.40f,lowerCenterZ,64f,.18f,lowerLen,false,3f).transform.localRotation=Quaternion.Euler(lowerAngle,0,0);
        B(roof,"Mansard west",M.roof,-(32f-lowerRun*.5f),121.40f,0,lowerLen,.18f,42f,false,3f).transform.localRotation=Quaternion.Euler(0,0,lowerAngle);
        B(roof,"Mansard east",M.roof,(32f-lowerRun*.5f),121.40f,0,lowerLen,.18f,42f,false,3f).transform.localRotation=Quaternion.Euler(0,0,-lowerAngle);

        float upperStartZ=21.0f-lowerRun,upperRise=4.70f;
        float upperAngle=Mathf.Atan2(upperRise,upperStartZ)*Mathf.Rad2Deg;
        float upperLen=Mathf.Sqrt(upperStartZ*upperStartZ+upperRise*upperRise);
        B(roof,"Upper roof front",M.roof,0,127.55f,-upperStartZ*.5f,56f,.16f,upperLen,false,3f).transform.localRotation=Quaternion.Euler(-upperAngle,0,0);
        B(roof,"Upper roof rear",M.roof,0,127.55f,upperStartZ*.5f,56f,.16f,upperLen,false,3f).transform.localRotation=Quaternion.Euler(upperAngle,0,0);
        B(roof,"Upper roof west",M.roof,-(32f-lowerRun-upperStartZ*.5f),127.55f,0,upperLen,.16f,34f,false,3f).transform.localRotation=Quaternion.Euler(0,0,upperAngle);
        B(roof,"Upper roof east",M.roof,(32f-lowerRun-upperStartZ*.5f),127.55f,0,upperLen,.16f,34f,false,3f).transform.localRotation=Quaternion.Euler(0,0,-upperAngle);

        B(roof,"Hidden roof technical deck",M.antiSlip,0,125.0f,0,40f,.16f,18f,true,2.5f);

        // Seven 3m dormers on front and rear, aligned to the seven 6m room bays.
        for(int i=0;i<7;i++)foreach(float s in new[]{-1f,1f})
        {
            float x=-18f+i*6f,z=s*18.2f;
            B(roof,"Mansard dormer body",M.stucco,x,123.35f,z,3.0f,2.8f,1.35f,false,1.8f);
            B(roof,"Mansard dormer glass",M.glass,x,123.35f,z+s*.72f,1.45f,1.75f,.06f,false);
            B(roof,"Mansard dormer cap",M.roof,x,124.90f,z,3.3f,.20f,1.75f,false,3f);
        }
        // Smaller side dormers, as required by the owner.
        foreach(float xs in new[]{-1f,1f})foreach(float z in new[]{-10f,0f,10f})
        {
            float x=xs*29.3f;
            B(roof,"Side mansard dormer body",M.stucco,x,123.10f,z,1.35f,2.5f,2.6f,false,1.8f);
            B(roof,"Side mansard dormer glass",M.glass,x+xs*.72f,123.10f,z,.06f,1.55f,1.30f,false);
            B(roof,"Side mansard dormer cap",M.roof,x,124.50f,z,1.75f,.20f,2.9f,false,3f);
        }

        for(float x=-12f;x<=12f;x+=8f)
            B(roof,"Hidden HVAC module",M.steel,x,126.0f,0,4.5f,1.6f,2.6f,true);
    }

    // ---------------------------------------------------------------- PORTICO / SIGN / CLASSICAL DETAIL
    static void BuildPorticoAndSign()
    {
        // Basement/ground exterior stone plinth.
        B(root,"Classical sandstone plinth front",M.sandstone,0,.60f,-21.15f,64f,1.20f,.55f,true,3f);
        B(root,"Grand entrance canopy",M.darkBronze,0,5.15f,-24.0f,18f,.42f,5.8f,true);
        B(root,"Grand entrance canopy brass rim",M.brass,0,5.38f,-24.0f,18.3f,.12f,6.0f,false);
        foreach(float x in new[]{-6f,-2f,2f,6f})
        {
            Cyl(root,"Grand portico ivory column",M.stucco,x,2.65f,-25.8f,.90f,5.3f,true);
            Cyl(root,"Grand portico brass base",M.brass,x,.18f,-25.8f,1.15f,.24f,true);
            Cyl(root,"Grand portico brass capital",M.brass,x,5.32f,-25.8f,1.10f,.22f,true);
        }
        // Three 0.15m x 0.38m entrance steps and an accessible 1.80m ramp <=5%.
        for(int i=0;i<3;i++)B(root,"Grand entrance step "+(i+1),M.graniteOutdoor,0,.075f+i*.15f,-27.0f+i*.38f,12f,.15f,.38f,true,2f);
        var accessRamp=B(root,"Grand entrance accessible ramp",M.antiSlip,8.8f,.225f,-28.0f,1.8f,.12f,9.0f,true,2.5f);
        accessRamp.transform.localRotation=Quaternion.Euler(-2.87f,0,0);

        // Clear 3.6m entrance, no collider on glass leaves.
        B(root,"Main entrance glass left",M.glass,-.95f,2.0f,-21.38f,1.7f,3.6f,.08f,false);
        B(root,"Main entrance glass right",M.glass,.95f,2.0f,-21.38f,1.7f,3.6f,.08f,false);
        foreach(float x in new[]{-1.85f,0f,1.85f})B(root,"Entrance brass mullion",M.brass,x,2.0f,-21.45f,.08f,3.8f,.10f,false);

        // Central classical risalit and roof pavilion.
        // Central risalit is expressed by edge pilasters only; never create a hidden wall across guest glazing.
        foreach(float x in new[]{-9f,9f})B(root,"Central tall pilaster",M.stucco,x,63f,-21.32f,.72f,108f,.72f,false,1.8f);
        B(root,"Central pavilion body",M.stucco,0,122.2f,-17.3f,18f,7.0f,6.0f,false,1.8f);
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
        float px=.10f,gap=.030f,charW=5*px+4*gap,space=.34f;
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
        // Pilasters follow the seven 6m room-bay boundaries plus service-wing edges.
        foreach(float x in new[]{-31.6f,-21f,-15f,-9f,-3f,3f,9f,15f,21f,31.6f})
        {
            B(root,"Front classical facade pilaster",M.stucco,x,58.8f,-21.30f,.26f,117.6f,.34f,false,1.8f);
            B(root,"Rear classical facade pilaster",M.stucco,x,58.8f,21.30f,.26f,117.6f,.34f,false,1.8f);
        }
        B(root,"Main top cornice front",M.stucco,0,117.45f,-21.30f,65.0f,.55f,.65f,false,1.8f);
        B(root,"Main top cornice rear",M.stucco,0,117.45f,21.30f,65.0f,.55f,.65f,false,1.8f);
    }
}

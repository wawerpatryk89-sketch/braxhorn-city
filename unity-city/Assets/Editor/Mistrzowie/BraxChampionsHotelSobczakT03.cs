using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// HOTEL SOBCZAK / T03 — rebuild 9.11.2026 on the owner's board
// "HOTEL T03 — SOBCZAK 5★ MUROWANY MODERN BRICK":
//   plot 60 x 60 at X=1030 Z=710; podium 42 x 38 (granite); tower 34 x 32 (brick);
//   ground 6 m (reception, lobby, waiting, luggage, toilets, lifts); floor 1 5 m
//   (restaurant, SPA, kitchen, back of house); floors 2-27 = 26 x 3.5 m, 10 rooms
//   each = 260 rooms; 2 stairs / 3 lifts; corridor 2.4 m; roof 8 m (technical +
//   terrace); total 6 + 5 + 91 + 8 = 110 m.
// Owner 9.11 19:57: the entrance must be real (it was see-through glass without
// collisions), no trees in front, real sidewalks, a real reception, every floor
// and room finished, corridors, lifts; lobby sofas in leather. 19:58: a proper sign.
// Built with the W kit (merged meshes, K applied ONCE on the root); the guest floor
// is ONE template prefab instanced 26 x and streamed (BraxHotelSobczakFloorStreamer).
public static class BraxChampionsHotelSobczakT03
{
    public const float CentreX=1030f,CentreZ=710f,PlotHalf=30f;
    public const float PodiumHalfW=21f,PodiumHalfD=19f,TowerHalfW=17f,TowerHalfD=16f;
    public const float Ffl0=.30f,GroundStorey=6.0f,FirstStorey=5.0f,GuestStorey=3.5f,Slab=.30f,RoofZone=8.0f;
    public const int GuestFloors=26,RoomsPerFloor=10,TotalRooms=260,Elevators=3,Stairs=2,Stops=GuestFloors+3;
    public const float SiteY=.16f,DriveY=.20f,PlazaY=.30f,PlotBaseY=-.25f,Balustrade=1.10f,Riser=.175f,Tread=.28f;
    const float TechZ0=-5.6f,TechZ1=4.0f,TechX=10f;              // roof technical box (lift overrun + plant + lift lobby)
    public const float RoomW=6.8f,RoomDepth=10.5f,CorridorW=2.4f;
    const float Fin=.012f;
    // core, symmetric about the tower centre: rooms | corridor | core band | corridor | rooms
    const float RoomWallZ=5.1f,CoreWallZ=2.5f;                   // wall centrelines, t .20
    static readonly float[] LiftX={-2.7f,0f,2.7f};
    const float LiftDoorHalf=.55f,ShaftFront=-2.4f,ShaftBack=.1f,LiftBank=4.15f;
    const float StairX0=11.3f,StairLand=12.6f,StairX1=16.7f,LaneIn=.2f,LaneOut=2.3f;
    const string Out="Assets/PlayableGenerated/Mistrzowie/HotelSobczakT03";
    const string LobbyModels="Assets/Source/Mistrzowie/ArenaLobby/";
    public const string RootName="T03 | HOTEL SOBCZAK | 5* | COMPLETE";
    static float K=>BraxClaudeOsiedle.K;
    public static float Level(int f)=>f<=0?Ffl0:f==1?Ffl0+GroundStorey:Ffl0+GroundStorey+FirstStorey+(f-2)*GuestStorey;
    public static float RoofDeck=>Level(GuestFloors+2);           // 102.30
    public static float Crown=>RoofDeck+RoofZone;                  // 110.30
    public static float CeilingOf(int f)=>Level(f+1)-Slab;
    public static int Risers(float h){int n=Mathf.RoundToInt(h/Riser);return n%2==1?n+1:n;}

    static BraxChampionsHotelSobczakMaterials.Set M;
    static Transform root,site,podium,tower,details,podiumInside,cores,roof,guestFloors,signs;
    static readonly List<GameObject> floorInstances=new List<GameObject>();
    static int roomsBuilt,doorsBuilt,modelsPlaced,serial;
    static Mesh leafMesh;

    [Serializable] sealed class Report
    {
        public string status="GENERATED_VISUAL_REVIEW_PENDING",id="T03",name="HOTEL SOBCZAK / 5*",style="MUROWANY MODERN BRICK (owner board 9.11)";
        public float centreX=CentreX,centreZ=CentreZ,podiumW=2*PodiumHalfW,podiumD=2*PodiumHalfD,towerW=2*TowerHalfW,towerD=2*TowerHalfD,height;
        public int guestFloors=GuestFloors,rooms,elevators=Elevators,stairs=Stairs,stops=Stops,doors,lobbyModels;
        public string roomLayout="5 FRONT + 5 BACK per floor; room 6.80 x 10.50 m (bath 3.10 x 3.30); corridors 2.40 m; core: 3 lifts + 2 dog-leg stairs";
        public bool apkBuilt=false,visualAccepted=false;
    }

    public static void Generate()
    {
        Directory.CreateDirectory(Out);Directory.CreateDirectory(Out+"/Meshes");Directory.CreateDirectory(Out+"/Materials");
        M=BraxChampionsHotelSobczakMaterials.Create();
        var existing=GameObject.Find(RootName);if(existing)UnityEngine.Object.DestroyImmediate(existing);
        roomsBuilt=doorsBuilt=modelsPlaced=serial=0;floorInstances.Clear();leafMesh=null;

        // Ground datum from the real plot base (legacy_foundation.py: plot_base at -.25).
        Physics.SyncTransforms();
        float datum=0f;
        if(Physics.Raycast(new Vector3(CentreX,80f,CentreZ)*K,Vector3.down,out var hit,200f*K,~0,QueryTriggerInteraction.Ignore))
        {
            datum=hit.point.y-PlotBaseY*K;
            if(Mathf.Abs(datum)>.05f*K)BraxBuildQuality.Warn("DO POPRAWKI T03_GROUND hit y="+hit.point.y.ToString("F3"));
        }
        else BraxBuildQuality.Warn("DO POPRAWKI T03_GROUND_RAYCAST_MISS: datum y=0");

        root=new GameObject(RootName).transform;
        site=G(root,"T03 SITE / plaza, sidewalks, kerbs, drive, fountains (no trees)");
        podium=G(root,"T03 PODIUM / granite 42x38, ground 6 m + floor 1 5 m");
        tower=G(root,"T03 TOWER / brick 34x32, floors 2-27");
        details=G(root,"T03 DETAILS / frames, LED, curtains, bollards (distance LOD)");
        podiumInside=G(root,"T03 PUBLIC FLOORS / reception, lobby, bar, restaurant, SPA, kitchen");
        cores=G(root,"T03 VERTICAL CORES / 3 lifts + 2 stairs");
        roof=G(root,"T03 ROOF / technical + terrace");
        guestFloors=G(root,"T03 GUEST FLOORS / 26 x 10 rooms (streamed)");
        signs=G(root,"T03 SIGN / SOBCZAK HOTEL 3D");
        W.Wyczysc();W.Start(site);W.Wnetrze=false;W.Komorka=40f;

        Site();
        PodiumShell();
        Entrance();
        TowerShell();
        Roof();
        LiftShafts();
        W.Komorka=14f;
        GroundFloor();
        FirstFloor();
        var template=GuestFloorTemplate();
        var cabins=LiftCabins();
        int saved=W.Zapisz("hotel_sobczak_t03");
        foreach(var cab in cabins)cab.localPosition=new Vector3(0,Ffl0,0);         // cabins wait at PARTER
        foreach(var d in root.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true))   // moving parts are never static
            foreach(var t in d.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);

        // one template prefab, 26 instances (shared meshes), per-floor numbers and landing doors
        var templatePrefab=PrefabUtility.SaveAsPrefabAsset(template.gameObject,Out+"/GuestFloorTemplate.prefab");
        UnityEngine.Object.DestroyImmediate(template.gameObject);
        if(!templatePrefab)throw new InvalidOperationException("HOTEL SOBCZAK failed to save GuestFloorTemplate.prefab");
        for(int f=2;f<GuestFloors+2;f++)
        {
            var instance=PrefabUtility.InstantiatePrefab(templatePrefab,guestFloors) as GameObject;
            if(!instance)throw new InvalidOperationException("HOTEL SOBCZAK failed to instantiate floor "+f);
            instance.name="GUEST INTERIOR FLOOR "+f+" | 10 ROOMS | PREFAB INSTANCE";
            instance.transform.localPosition=Vector3.up*Level(f);
            Renumber(instance.transform,f);
            floorInstances.Add(instance);
        }
        // landing doors of every stop (moved by BraxWinda); guest floors keep theirs inside the streamed floor
        var landingL=new Transform[Elevators,Stops];var landingR=new Transform[Elevators,Stops];
        for(int s=0;s<Stops;s++)
        {
            Transform parent=s==0||s==1?podiumInside:s==Stops-1?roof:floorInstances[s-2].transform;
            for(int l=0;l<Elevators;l++)
            {
                float y=Level(s)-(s>=2&&s<Stops-1?Level(s):0f);   // inside a floor instance y is local
                // pocket doors: in the plane of the lift wall, they slide into the wall (never in front of it)
                landingL[l,s]=LeafObject(parent,"Landing door L | lift "+(l+1)+" | stop "+StopName(s),new Vector3(LiftX[l]-LiftDoorHalf*.5f,y+1.1f,-CoreWallZ));
                landingR[l,s]=LeafObject(parent,"Landing door R | lift "+(l+1)+" | stop "+StopName(s),new Vector3(LiftX[l]+LiftDoorHalf*.5f,y+1.1f,-CoreWallZ));
            }
        }
        int signParts=Sign();
        // named markers for the scene audit (the flights themselves are merged W meshes)
        foreach(float sx in new[]{-1f,1f})
        {
            var stair=G(cores,"STAIR "+(sx<0?"W":"E")+" | dog-leg 2.1 m flights | PARTER TO TARAS | 29 levels");
            stair.localPosition=new Vector3(sx*(StairLand+StairX1)*.5f,Ffl0,0);
        }

        // ---- place: K once, ground datum from the raycast ----
        root.localScale=Vector3.one*K;
        root.position=new Vector3(CentreX*K,datum,CentreZ*K);
        foreach(var l in root.GetComponentsInChildren<Light>(true))l.range*=K;
        var detailRenderers=details.GetComponentsInChildren<Renderer>(true);
        var lod=details.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[]{new LOD(.05f,detailRenderers),new LOD(.0001f,Array.Empty<Renderer>())});lod.RecalculateBounds();
        var vis=podiumInside.gameObject.AddComponent<BraxWidocznoscWnetrza>();vis.nazwa="T03 public floors";
        // lobby and restaurant are drawn within ~60 m of the podium (seen through the glass), hidden from far away
        vis.strefy=new[]{new Bounds(root.TransformPoint(new Vector3(0,12f,0)),new Vector3(2*PodiumHalfW+120f,40f,2*PodiumHalfD+120f)*K)};
        for(int l=0;l<Elevators;l++)Elevator(l,cabins[l],landingL,landingR);
        foreach(var d in root.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true))d.zasieg=2.6f*K;
        var streamer=root.gameObject.AddComponent<BraxHotelSobczakFloorStreamer>();
        var instances=new GameObject[floorInstances.Count];for(int i=0;i<instances.Length;i++)instances[i]=floorInstances[i];
        streamer.guestInteriors=instances;streamer.basementInteriors=new GameObject[0];streamer.metreScale=1f;   // root carries K
        streamer.guestBaseY=Level(2);streamer.floorHeight=GuestStorey;streamer.halfWidth=TowerHalfW;streamer.halfDepth=TowerHalfD;
        streamer.neighbourFloors=1;
        foreach(var go in instances)go.SetActive(false);

        var report=new Report{rooms=roomsBuilt,doors=doorsBuilt,lobbyModels=modelsPlaced,height=Crown-Ffl0};
        File.WriteAllText(Out+"/hotel-sobczak-report.json",JsonUtility.ToJson(report,true));
        var at=root.position;root.position=Vector3.zero;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Out+"/HotelSobczakT03.prefab",InteractionMode.AutomatedAction);
        root.position=at;
        if(!prefab)throw new IOException("HOTEL SOBCZAK prefab save failed");
        AssetDatabase.SaveAssets();
        if(roomsBuilt!=TotalRooms)throw new InvalidOperationException("HOTEL SOBCZAK room count "+roomsBuilt+" != "+TotalRooms);
        Debug.Log("HOTEL_SOBCZAK_T03_REBUILT style=MODERN_BRICK podium=42x38 tower=34x32 height="+(Crown-Ffl0).ToString("F2")+
            " guestFloors="+GuestFloors+" rooms="+roomsBuilt+" lifts="+Elevators+" stops="+Stops+" stairs="+Stairs+
            " doors="+doorsBuilt+" lobbyLeatherModels="+modelsPlaced+" signParts="+signParts+" meshes="+saved+
            " datumY="+datum.ToString("F3")+" entrance=automatic_sliding_doors_with_collision trees=0 visualReview=PENDING");
    }

    // ================================================================ helpers
    static Transform G(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
    static void In(Transform g,bool interior){W.Grupa=g;W.Wnetrze=interior;}
    static float Lo(float a,float b)=>Mathf.Min(a,b);
    static float Hi(float a,float b)=>Mathf.Max(a,b);
    static void Box(Material m,float x0,float x1,float y0,float y1,float z0,float z1,bool col=false)
    {
        if(x1<x0){var t=x0;x0=x1;x1=t;}if(z1<z0){var t=z0;z0=z1;z1=t;}
        if(x1-x0<.001f||y1-y0<.001f||z1-z0<.001f)return;
        W.MM(m,x0,x1,y0,y1,z0,z1,col);
    }
    static void Face(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0)W.Quad(m,d,c,b,a);else W.Quad(m,a,b,c,d);
    }
    static void Ceil(float x0,float x1,float z0,float z1,float y)
        =>Face(M.ceiling,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.down);
    static void Downlight(float x,float z,float y)=>Box(M.warmLight,x-.08f,x+.08f,y-.012f,y+.004f,z-.08f,z+.08f);
    static void Rod(Material m,Vector3 a,Vector3 b,float r)=>W.Rura(m,a,b,r,8);
    // Circle as a closed polygon (fountains, planters, chandelier).
    static List<Vector2> Circle(float cx,float cz,float r,int seg=24)
    {
        var p=new List<Vector2>();
        for(int i=0;i<seg;i++){float a=i*Mathf.PI*2f/seg;p.Add(new Vector2(cx+Mathf.Cos(a)*r,cz+Mathf.Sin(a)*r));}
        return p;
    }
    static void Prism(List<Vector2> poly,float y0,float y1,Material top,Material side,bool col)
    {
        var c=Vector2.zero;foreach(var q in poly)c+=q;c/=poly.Count;
        for(int i=0;i<poly.Count;i++)
        {
            var a=poly[i];var b=poly[(i+1)%poly.Count];var mid=(a+b)*.5f;
            if(top)Face(top,new Vector3(c.x,y1,c.y),new Vector3(a.x,y1,a.y),new Vector3(mid.x,y1,mid.y),new Vector3(b.x,y1,b.y),Vector3.up);
            if(side)Face(side,new Vector3(a.x,y0,a.y),new Vector3(b.x,y0,b.y),new Vector3(b.x,y1,b.y),new Vector3(a.x,y1,a.y),new Vector3(b.y-a.y,0,-(b.x-a.x)));
        }
        if(col){float r=0;foreach(var q in poly)r=Mathf.Max(r,(q-c).magnitude);W.KolizjaMM(c.x-r,c.x+r,y0,y1,c.y-r,c.y+r);}
    }
    static void Ring(List<Vector2> poly,float y0,float y1,float thick,Material m,bool col)
    {
        for(int i=0;i<poly.Count;i++)
        {
            var a=poly[i];var b=poly[(i+1)%poly.Count];var d=(b-a).normalized;var mid=(a+b)*.5f;
            W.OBox(m,new Vector3(mid.x,(y0+y1)*.5f,mid.y),new Vector3(thick,y1-y0,(b-a).magnitude+.01f),Quaternion.LookRotation(new Vector3(d.x,0,d.y),Vector3.up),col);
        }
    }
    static void Wedge(Material m,float x0,float x1,float z0,float yLow,float z1,float yHigh,float yBase)
    {
        Face(m,new Vector3(x0,yLow,z0),new Vector3(x1,yLow,z0),new Vector3(x1,yHigh,z1),new Vector3(x0,yHigh,z1),Vector3.up);
        Face(m,new Vector3(x0,yBase,z0),new Vector3(x0,yBase,z1),new Vector3(x0,yHigh,z1),new Vector3(x0,yLow,z0),Vector3.left);
        Face(m,new Vector3(x1,yBase,z0),new Vector3(x1,yBase,z1),new Vector3(x1,yHigh,z1),new Vector3(x1,yLow,z0),Vector3.right);
        W.Rampa(new Vector3((x0+x1)*.5f,yHigh,z1),new Vector3((x0+x1)*.5f,yLow,z0),Mathf.Abs(x1-x0));
    }

    struct Gap{public float a,b,top;public Gap(float a,float b,float top){this.a=a;this.b=b;this.top=top;}}
    static void WallX(Material m,float z,float x0,float x1,float y0,float y1,float t,params Gap[] gaps)
    {
        Array.Sort(gaps,(p,q)=>p.a.CompareTo(q.a));float x=x0;
        foreach(var g in gaps)
        {
            Box(m,x,g.a,y0,y1,z-t*.5f,z+t*.5f,true);Box(m,g.a,g.b,y0+g.top,y1,z-t*.5f,z+t*.5f,true);
            if(g.top<2.6f)Casing(true,z,g.a,g.b,y0,y0+g.top,t);x=g.b;
        }
        Box(m,x,x1,y0,y1,z-t*.5f,z+t*.5f,true);
    }
    static void WallZ(Material m,float x,float z0,float z1,float y0,float y1,float t,params Gap[] gaps)
    {
        Array.Sort(gaps,(p,q)=>p.a.CompareTo(q.a));float z=z0;
        foreach(var g in gaps)
        {
            Box(m,x-t*.5f,x+t*.5f,y0,y1,z,g.a,true);Box(m,x-t*.5f,x+t*.5f,y0+g.top,y1,g.a,g.b,true);
            if(g.top<2.6f)Casing(false,x,g.a,g.b,y0,y0+g.top,t);z=g.b;
        }
        Box(m,x-t*.5f,x+t*.5f,y0,y1,z,z1,true);
    }
    static void Casing(bool alongX,float plane,float a,float b,float y0,float y1,float t)
    {
        float w=.05f,d=t*.5f+.012f;
        if(alongX){Box(M.teakDark,a,a+w,y0,y1,plane-d,plane+d);Box(M.teakDark,b-w,b,y0,y1,plane-d,plane+d);Box(M.teakDark,a,b,y1-w,y1,plane-d,plane+d);}
        else{Box(M.teakDark,plane-d,plane+d,y0,y1,a,a+w);Box(M.teakDark,plane-d,plane+d,y0,y1,b-w,b);Box(M.teakDark,plane-d,plane+d,y1-w,y1,a,b);}
    }
    // Door leaf standing open at 90 degrees, hinged at 'a', swinging toward 'side'; brass lever.
    static void Leaf(bool alongX,float plane,float a,float b,float y0,float side,float h=2.10f)
    {
        float w=(b-a)-.10f,th=.045f,p0=Lo(plane,plane+side*w),p1=Hi(plane,plane+side*w),face=a+.05f+th,free=plane+side*(w-.10f);
        if(alongX){Box(M.teakDark,a+.05f,face,y0+.01f,y0+h-.06f,p0,p1);Box(M.brass,face,face+.025f,y0+1.0f,y0+1.03f,free-.07f,free+.07f);}
        else{Box(M.teakDark,p0,p1,y0+.01f,y0+h-.06f,a+.05f,face);Box(M.brass,free-.07f,free+.07f,y0+1.0f,y0+1.03f,face,face+.025f);}
        doorsBuilt++;
    }
    static string StopName(int s)=>s==0?"PARTER":s==Stops-1?"TARAS":s.ToString();

    // ================================================================ SITE
    static void Site()
    {
        In(site,false);
        // plaza deck around the podium, flush with the lobby floor (no step at the door), cut for the dropped kerb
        float pw=PodiumHalfW,pd=PodiumHalfD;
        Box(M.paving,-PlotHalf,-1.8f,PlotBaseY,PlazaY,-PlotHalf,-pd,true);
        Box(M.paving,1.8f,PlotHalf,PlotBaseY,PlazaY,-PlotHalf,-pd,true);
        Box(M.paving,-1.8f,1.8f,PlotBaseY,PlazaY,-PlotHalf+1.2f,-pd,true);
        Box(M.paving,-PlotHalf,PlotHalf,PlotBaseY,PlazaY,pd,PlotHalf,true);
        foreach(float sx in new[]{-1f,1f})Box(M.paving,sx*pw,sx*PlotHalf,PlotBaseY,PlazaY,-pd,pd,true);
        Wedge(M.paving,-1.8f,1.8f,-PlotHalf,DriveY,-PlotHalf+1.2f,PlazaY,PlotBaseY);      // dropped kerb at the crossing
        // granite kerb along the plaza front edge (11 cm above the drive)
        Box(M.kerb,-PlotHalf,-1.8f,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);
        Box(M.kerb,1.8f,PlotHalf,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);
        // frontage to road Z650 (13 m strip, sidewalk edge at local z -43.2): drop-off loop, island, path
        const float edge=-43.2f;
        Box(M.asphalt,-16f,16f,SiteY,DriveY,-34f,-PlotHalf,true);                       // drop-off lane
        foreach(float sx in new[]{-1f,1f})Box(M.asphalt,sx*10f,sx*16f,SiteY,DriveY,edge,-34f,true);   // in / out
        Box(M.paving,-1.8f,1.8f,SiteY,DriveY+.005f,edge,-34f,true);                     // footpath to the sidewalk
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*1.8f,b=sx*10f;
            Box(M.kerb,a,b,SiteY,DriveY+.12f,-34.18f,-34f,true);                          // island kerbs
            Box(M.kerb,b-sx*.18f,b,SiteY,DriveY+.12f,edge,-34f,true);
            Box(M.kerb,a,a+sx*.18f,SiteY,DriveY+.12f,edge,-34f,true);
            Box(M.lawn,Lo(a+sx*.18f,b-sx*.18f),Hi(a+sx*.18f,b-sx*.18f),SiteY,DriveY+.10f,edge,-34.18f,true);
            // fountain: granite basin, water, central jet
            var basin=Circle(sx*5.9f,-38.6f,2.4f);
            Ring(basin,DriveY,DriveY+.55f,.30f,M.podiumStone,true);
            Prism(Circle(sx*5.9f,-38.6f,2.25f),DriveY,DriveY+.42f,M.water,null,false);
            Prism(Circle(sx*5.9f,-38.6f,.35f,12),DriveY,DriveY+.75f,M.podiumStone,M.podiumStone,true);
            Rod(M.water,new Vector3(sx*5.9f,DriveY+.75f,-38.6f),new Vector3(sx*5.9f,DriveY+2.1f,-38.6f),.07f);
        }
        // zebra crossing (stripes run with the traffic)
        for(float z=-33.55f;z<=-30.4f;z+=.9f)Box(M.whitePaint,-1.7f,1.7f,DriveY,DriveY+.004f,z,z+.45f);
        // raised lawn beds with granite edges on the sides and the back (no trees: owner 9.11)
        foreach(float sx in new[]{-1f,1f})Planter(sx*23.2f,sx*29.6f,-26f,27f);
        Planter(-23.2f,23.2f,21.6f,29.6f);
        foreach(float sx in new[]{-1f,1f})foreach(float x in new[]{10.5f,15f})Planter(sx*x-1.6f,sx*x+1.6f,-23.6f,-22.2f);
        // LED bollards along the plaza edge and drop-off lamps
        In(details,false);
        for(float x=-27f;x<=27.01f;x+=4.5f)
        {
            if(Mathf.Abs(x)<2.5f)continue;
            Box(M.darkBronze,x-.08f,x+.08f,PlazaY,PlazaY+.9f,-PlotHalf+.6f,-PlotHalf+.76f,true);
            Box(M.warmLight,x-.085f,x+.085f,PlazaY+.9f,PlazaY+1.0f,-PlotHalf+.595f,-PlotHalf+.765f);
        }
        foreach(float sx in new[]{-1f,1f})foreach(float x in new[]{4.2f,13f})
        {
            Rod(M.darkBronze,new Vector3(sx*x,PlazaY,-PlotHalf+.45f),new Vector3(sx*x,PlazaY+5.0f,-PlotHalf+.45f),.07f);
            Box(M.darkBronze,sx*x-.25f,sx*x+.25f,PlazaY+4.9f,PlazaY+5.05f,-PlotHalf+.2f,-PlotHalf+.7f);
            Box(M.warmLight,sx*x-.22f,sx*x+.22f,PlazaY+4.86f,PlazaY+4.9f,-PlotHalf+.23f,-PlotHalf+.67f);
        }
        In(site,false);
    }
    static void Planter(float x0,float x1,float z0,float z1)
    {
        if(x1<x0){var t=x0;x0=x1;x1=t;}
        Box(M.podiumStone,x0,x1,PlazaY,PlazaY+.45f,z0,z0+.18f,true);Box(M.podiumStone,x0,x1,PlazaY,PlazaY+.45f,z1-.18f,z1,true);
        Box(M.podiumStone,x0,x0+.18f,PlazaY,PlazaY+.45f,z0+.18f,z1-.18f,true);Box(M.podiumStone,x1-.18f,x1,PlazaY,PlazaY+.45f,z0+.18f,z1-.18f,true);
        Box(M.lawn,x0+.18f,x1-.18f,PlazaY,PlazaY+.38f,z0+.18f,z1-.18f,true);
    }

    // ================================================================ SLABS with core holes
    // holes: stair flights x +-[12.6,16.7] z [-2.4,2.4]; lift shafts x [-3.95,3.95] z [-2.4,.1]
    static void SlabWithHoles(Material m,float y0,float y1,float hw,float hd)
    {
        Box(m,-hw,hw,y0,y1,-hd,-2.4f,true);Box(m,-hw,hw,y0,y1,2.4f,hd,true);
        Box(m,-hw,-StairX1,y0,y1,-2.4f,2.4f,true);Box(m,StairX1,hw,y0,y1,-2.4f,2.4f,true);
        Box(m,-StairLand,-3.95f,y0,y1,-2.4f,2.4f,true);Box(m,3.95f,StairLand,y0,y1,-2.4f,2.4f,true);
        Box(m,-3.95f,3.95f,y0,y1,ShaftBack,2.4f,true);
    }
    // Ceiling of a public floor with the same openings (stairwells stay open, no quad across a flight).
    static void CeilAround(float hw,float hd,float y)
    {
        Ceil(-hw,hw,-hd,-2.4f,y);Ceil(-hw,hw,2.4f,hd,y);
        Ceil(-hw,-StairX1,-2.4f,2.4f,y);Ceil(StairX1,hw,-2.4f,2.4f,y);
        Ceil(-StairLand,-3.95f,-2.4f,2.4f,y);Ceil(3.95f,StairLand,-2.4f,2.4f,y);
        Ceil(-3.95f,3.95f,ShaftBack,2.4f,y);
    }
    // Wall behind the lift bank and the two short returns: closes the void behind the shafts
    // so the rooms on both sides of the core (toilets, service rooms) never connect through it.
    static void CoreBack(float y0,float y1)
    {
        WallX(M.paint,CoreWallZ,-LiftBank,LiftBank,y0,y1,.2f);
        foreach(float sx in new[]{-1f,1f})WallZ(M.paint,sx*(LiftBank+.1f),ShaftBack+.2f,CoreWallZ-.1f,y0,y1,.2f);
    }

    // ================================================================ PODIUM SHELL (granite)
    static readonly float[] PodiumPiersX={-20.5f,-13f,-5f,5f,13f,20.5f};
    static readonly float[] PodiumPiersZ={-18.5f,-9.5f,0f,9.5f,18.5f};
    static void PodiumShell()
    {
        In(podium,false);
        Box(M.podiumStone,-PodiumHalfW,PodiumHalfW,PlotBaseY,Ffl0,-PodiumHalfD,PodiumHalfD,true);       // plinth
        // slabs stop at the inner face of the envelope: the stone bands cover their edges (no coplanar faces)
        SlabWithHoles(M.ceiling,Level(1)-Slab,Level(1),PodiumHalfW-.3f,PodiumHalfD-.3f);
        SlabWithHoles(M.ceiling,Level(2)-Slab,Level(2),PodiumHalfW-.3f,PodiumHalfD-.3f);                  // podium roof
        float top=Level(2),parapet=top+.30f;
        // piers continuous ground -> parapet, 25 cm proud
        foreach(float x in PodiumPiersX)foreach(float sz in new[]{-1f,1f})
            Box(M.podiumStone,x-.5f,x+.5f,Ffl0,parapet,sz*PodiumHalfD-(sz<0?.25f:-.25f),sz*(PodiumHalfD-.3f),true);
        foreach(float z in PodiumPiersZ)foreach(float sx in new[]{-1f,1f})
            Box(M.podiumStone,sx*PodiumHalfW+sx*.25f,sx*(PodiumHalfW-.3f),Ffl0,parapet,z-.5f,z+.5f,true);
        foreach(float sz in new[]{-1f,1f})PodiumBands(true,sz*(PodiumHalfD-.15f),sz,PodiumHalfW);
        foreach(float sx in new[]{-1f,1f})PodiumBands(false,sx*(PodiumHalfW-.15f),sx,PodiumHalfD);
        // podium roof: stone coping + frameless glass balustrade, terrace paving and planters around the tower
        In(podium,false);
        foreach(float sz in new[]{-1f,1f})
        {
            Box(M.podiumStone,-PodiumHalfW-.3f,PodiumHalfW+.3f,top,parapet,sz*(PodiumHalfD-.3f),sz*(PodiumHalfD+.3f),true);
            Box(M.clearGlass,-PodiumHalfW,PodiumHalfW,parapet,top+Balustrade,sz*(PodiumHalfD-.02f),sz*(PodiumHalfD+.006f),true);
        }
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.podiumStone,sx*(PodiumHalfW-.3f),sx*(PodiumHalfW+.3f),top,parapet,-PodiumHalfD,PodiumHalfD,true);
            Box(M.clearGlass,sx*(PodiumHalfW-.02f),sx*(PodiumHalfW+.006f),parapet,top+Balustrade,-PodiumHalfD,PodiumHalfD,true);
        }
        Box(M.paving,-PodiumHalfW+.3f,PodiumHalfW-.3f,top,top+.02f,-PodiumHalfD+.3f,-TowerHalfD,false);
        Box(M.paving,-PodiumHalfW+.3f,PodiumHalfW-.3f,top,top+.02f,TowerHalfD,PodiumHalfD-.3f,false);
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.paving,sx*TowerHalfW,sx*(PodiumHalfW-.3f),top,top+.02f,-TowerHalfD,TowerHalfD,false);
            for(float z=-13f;z<=13.01f;z+=6.5f)RoofPlanter(sx*19f,z,1.2f,2.6f,top+.02f);
        }
        foreach(float x in new[]{-12f,-6f,6f,12f})foreach(float sz in new[]{-1f,1f})RoofPlanter(x,sz*17.6f,2.4f,1.0f,top+.02f);
        // LED line under the stone band at the podium top (board render)
        In(details,false);
        foreach(float sz in new[]{-1f,1f})Box(M.ledCool,-PodiumHalfW,PodiumHalfW,top-.06f,top-.02f,sz*(PodiumHalfD+.26f),sz*(PodiumHalfD+.30f));
        foreach(float sx in new[]{-1f,1f})Box(M.ledCool,sx*(PodiumHalfW+.26f),sx*(PodiumHalfW+.30f),top-.06f,top-.02f,-PodiumHalfD,PodiumHalfD);
    }
    static void RoofPlanter(float x,float z,float hw,float hd,float y)
    {
        Box(M.podiumStone,x-hw,x+hw,y,y+.55f,z-hd,z+hd,true);
        Box(M.lawn,x-hw+.12f,x+hw-.12f,y+.55f,y+.58f,z-hd+.12f,z+hd-.12f);
    }
    // Ground floor: full-height glass between piers; floor 1: stone spandrel + glass + stone head.
    // The front centre bay (x +-4.5) is the entrance (ground) and the solid sign panel (floor 1).
    static void PodiumBands(bool alongX,float c,float nrm,float half)
    {
        var piers=alongX?PodiumPiersX:PodiumPiersZ;
        var gaps=new List<(float a,float b)>();float prev=-half;
        foreach(float p in piers){if(p-.5f>prev)gaps.Add((prev,p-.5f));prev=p+.5f;}
        if(prev<half)gaps.Add((prev,half));
        float y0=Ffl0,y1=Level(1)-Slab,f0=Level(1)-Slab,f1=Level(2)-Slab;
        bool front=alongX&&nrm<0;
        foreach(var g in gaps)
        {
            if(g.b-g.a<.05f)continue;
            bool centre=front&&g.a>-5f&&g.b<5f;
            // ground floor
            In(podium,false);
            if(!centre)
            {
                Pane(alongX,c,g.a,g.b,y0,y1,M.glass);
                In(details,false);Frame(alongX,c,g.a,g.b,y0,y1,2.0f);In(podium,false);
            }
            // slab band + floor 1
            Solid(alongX,c,g.a,g.b,y1,f0+1.0f,M.podiumStone);
            if(centre)Solid(alongX,c,g.a,g.b,f0+1.0f,f1,M.podiumStone);                         // sign panel
            else
            {
                Pane(alongX,c,g.a,g.b,f0+1.0f,f1-.6f,M.glass);
                Solid(alongX,c,g.a,g.b,f1-.6f,f1,M.podiumStone);
                In(details,false);Frame(alongX,c,g.a,g.b,f0+1.0f,f1-.6f,2.0f);In(podium,false);
            }
            Solid(alongX,c,g.a,g.b,f1,Level(2),M.podiumStone);
        }
    }
    static void Solid(bool alongX,float c,float a,float b,float y0,float y1,Material m)
    {
        if(alongX)Box(m,a,b,y0,y1,c-.15f,c+.15f,true);else Box(m,c-.15f,c+.15f,y0,y1,a,b,true);
    }
    static void Pane(bool alongX,float c,float a,float b,float y0,float y1,Material m)
    {
        if(alongX)Box(m,a,b,y0,y1,c-.01f,c+.01f,true);else Box(m,c-.01f,c+.01f,y0,y1,a,b,true);
    }
    // Slim dark bronze frame: sill, head and mullions every <= module metres.
    static void Frame(bool alongX,float c,float a,float b,float y0,float y1,float module)
    {
        int n=Mathf.Max(1,Mathf.CeilToInt((b-a)/module));
        void R(float u0,float u1,float v0,float v1){if(alongX)Box(M.darkBronze,u0,u1,v0,v1,c-.05f,c+.05f);else Box(M.darkBronze,c-.05f,c+.05f,v0,v1,u0,u1);}
        R(a,b,y0,y0+.06f);R(a,b,y1-.06f,y1);
        for(int i=1;i<n;i++){float u=a+(b-a)*i/n;R(u-.03f,u+.03f,y0,y1);}
    }

    // ================================================================ ENTRANCE (real doors)
    static void Entrance()
    {
        In(podium,false);
        float z0=-PodiumHalfD+.15f,z1=-14.5f,top=3.2f;
        // glass vestibule (wiatrolap) 8 x 4.5 m: side walls with collisions, teak ceiling with LED
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.clearGlass,sx*4f-.01f,sx*4f+.01f,Ffl0,top,z0,z1,true);
            Box(M.steel,sx*4f-.04f,sx*4f+.04f,top-.06f,top,z0,z1);
        }
        // facade bay above and beside the outer doors (glass transom, steel frame)
        Box(M.clearGlass,-4.5f,4.5f,top,Level(1)-Slab,z0-.01f,z0+.01f,true);
        foreach(float sx in new[]{-1f,1f})Box(M.clearGlass,sx*1.45f,sx*4.5f,Ffl0,top,z0-.01f,z0+.01f,true);
        foreach(float sx in new[]{-1f,1f})Box(M.clearGlass,sx*1.45f,sx*4f,Ffl0,top,z1-.01f,z1+.01f,true);
        Box(M.paint,-4f,4f,top,Level(1)-Slab,z1-.1f,z1+.1f,true);                                   // bulkhead over inner doors
        In(details,false);
        foreach(float z in new[]{z0,z1})
        {
            Box(M.steel,-4.5f,4.5f,top-.08f,top,z-.06f,z+.06f);
            foreach(float x in new[]{-4f,-1.45f,1.45f,4f})Box(M.steel,x-.04f,x+.04f,Ffl0,top,z-.06f,z+.06f);
        }
        In(podiumInside,true);
        Box(M.blackMarble,-3.95f,3.95f,Ffl0,Ffl0+Fin,z0+.1f,z1);                                    // entrance mat zone
        Face(M.teak,new Vector3(-3.95f,top-.01f,z0),new Vector3(3.95f,top-.01f,z0),new Vector3(3.95f,top-.01f,z1),new Vector3(-3.95f,top-.01f,z1),Vector3.down);
        Box(M.ledCool,-3.2f,3.2f,top-.025f,top-.01f,(z0+z1)*.5f-.05f,(z0+z1)*.5f+.05f);
        // two pairs of automatic sliding doors (BraxDrzwiAutomatyczne): closed they block, they open as you walk up
        // leaves run 10 cm in front of the fixed glass (never through it)
        AutoDoors("T03 MAIN ENTRANCE | outer automatic doors",z0-.10f);
        AutoDoors("T03 MAIN ENTRANCE | inner automatic doors",z1-.10f);
        // canopy 15 x 6.6 m with LED soffit lines, black fascia, tie rods to the podium
        In(podium,false);
        float c0=-PodiumHalfD-6.6f,cy=4.6f;
        Box(M.darkBronze,-7.5f,7.5f,cy,cy+.5f,c0,-PodiumHalfD-.25f,true);
        Face(M.teak,new Vector3(-7.4f,cy-.005f,c0+.1f),new Vector3(7.4f,cy-.005f,c0+.1f),new Vector3(7.4f,cy-.005f,-PodiumHalfD-.25f),new Vector3(-7.4f,cy-.005f,-PodiumHalfD-.25f),Vector3.down);
        In(details,false);
        foreach(float z in new[]{-PodiumHalfD-1.6f,-PodiumHalfD-3.4f,-PodiumHalfD-5.2f})Box(M.ledCool,-6.8f,6.8f,cy-.02f,cy-.005f,z-.05f,z+.05f);
        foreach(float sx in new[]{-1f,1f})Rod(M.steel,new Vector3(sx*6.8f,cy+.5f,c0+.4f),new Vector3(sx*5f,Level(1)+2.6f,-PodiumHalfD-.25f),.04f);   // to the stone piers
        // granite inlay under the canopy (5 mm proud of the plaza)
        In(site,false);
        Box(M.stoneDark,-7.5f,7.5f,PlazaY,PlazaY+.005f,c0,-PodiumHalfD-.25f);
    }
    static void AutoDoors(string name,float z)
    {
        var g=G(podium,name);g.localPosition=new Vector3(0,Ffl0,z);
        var door=g.gameObject.AddComponent<BraxDrzwiAutomatyczne>();door.nazwa="HOTEL SOBCZAK";door.przesuw=1.40f;
        door.skrzydloL=DoorLeafGlass(g,"left",-.725f);door.skrzydloP=DoorLeafGlass(g,"right",.725f);
        doorsBuilt+=2;
    }
    static Transform DoorLeafGlass(Transform door,string side,float x)
    {
        var t=G(door,"automatic door leaf "+side);t.localPosition=new Vector3(x,0,0);
        // built in world coordinates at build time (root still at origin, scale 1)
        var at=door.localPosition+t.localPosition;
        In(t,false);
        Box(M.clearGlass,at.x-.72f,at.x+.72f,at.y+.02f,at.y+3.08f,at.z-.012f,at.z+.012f,true);
        Box(M.steel,at.x-.74f,at.x+.74f,at.y,at.y+.08f,at.z-.03f,at.z+.03f);
        Box(M.steel,at.x-.74f,at.x+.74f,at.y+3.04f,at.y+3.12f,at.z-.03f,at.z+.03f);
        Box(M.steel,at.x+(x<0?-.74f:.70f),at.x+(x<0?-.70f:.74f),at.y,at.y+3.12f,at.z-.03f,at.z+.03f);
        Box(M.brass,at.x+(x<0?.55f:-.60f),at.x+(x<0?.60f:-.55f),at.y+.9f,at.y+2.1f,at.z-.06f,at.z+.06f);
        return t;
    }

    // ================================================================ TOWER SHELL (brick)
    static void TowerShell()
    {
        In(tower,false);
        float y0=Level(2),y1=RoofDeck;
        for(int f=3;f<=GuestFloors+2;f++)SlabWithHoles(M.ceiling,Level(f)-Slab,Level(f),TowerHalfW-.3f,TowerHalfD-.3f);   // edges hidden by spandrels
        // front/back: continuous brick piers (room partitions 1.0 m, window mid-piers .5 m), 15 cm proud
        foreach(float sz in new[]{-1f,1f})
        {
            float zo=sz*TowerHalfD,zi=sz*(TowerHalfD-.3f),zp=sz*(TowerHalfD+.15f);
            for(int i=0;i<=5;i++)
            {
                float x=-TowerHalfW+i*RoomW,a=i==0?x:x-.5f,b=i==5?x:x+.5f;
                Box(M.brick,a,b,y0,y1,Lo(zp,zi),Hi(zp,zi),true);
                if(i<5)Box(M.brick,x+RoomW*.5f-.25f,x+RoomW*.5f+.25f,y0,y1,Lo(zp,zi),Hi(zp,zi),true);
            }
            for(int f=2;f<GuestFloors+2;f++)
            {
                float b0=Level(f),b1=Level(f+1);
                Box(M.brick,-TowerHalfW,TowerHalfW,b0-Slab,b0+.6f,Lo(zo,zi),Hi(zo,zi),true);         // spandrel / sill
                Box(M.brick,-TowerHalfW,TowerHalfW,b1-Slab-.35f,b1-Slab,Lo(zo,zi),Hi(zo,zi),true);   // head
                Box(M.glass,-TowerHalfW,TowerHalfW,b0+.6f,b1-Slab-.35f,sz*(TowerHalfD-.16f),sz*(TowerHalfD-.14f),true);
                In(details,false);
                Box(M.darkBronze,-TowerHalfW,TowerHalfW,b0+.6f,b0+.66f,sz*(TowerHalfD-.2f),sz*(TowerHalfD-.1f));
                Box(M.darkBronze,-TowerHalfW,TowerHalfW,b1-Slab-.41f,b1-Slab-.35f,sz*(TowerHalfD-.2f),sz*(TowerHalfD-.1f));
                for(int i=0;i<5;i++)foreach(float u in new[]{1.825f,4.975f})
                {
                    float x=-TowerHalfW+i*RoomW+u;
                    Box(M.darkBronze,x-.03f,x+.03f,b0+.6f,b1-Slab-.35f,sz*(TowerHalfD-.2f),sz*(TowerHalfD-.1f));
                }
                Curtains(true,sz,b0);
                In(tower,false);
            }
        }
        // sides: corner-room windows (by the armchair / desk, clear of the bed wall), corridor-end windows,
        // solid brick over the stair cores
        float[] wins={-14.7f,-3.8f,3.8f,14.7f};float[] half={.7f,.7f,.7f,.7f};
        foreach(float sx in new[]{-1f,1f})
        {
            float xo=sx*TowerHalfW,xi=sx*(TowerHalfW-.3f);
            float prev=-TowerHalfD;
            for(int w=0;w<4;w++)
            {
                Box(M.brick,Lo(xo,xi),Hi(xo,xi),y0,y1,prev,wins[w]-half[w],true);prev=wins[w]+half[w];
                Box(M.brick,Lo(xo+sx*.15f,xo),Hi(xo+sx*.15f,xo),y0,y1,wins[w]-half[w]-.25f,wins[w]-half[w],true);
                Box(M.brick,Lo(xo+sx*.15f,xo),Hi(xo+sx*.15f,xo),y0,y1,wins[w]+half[w],wins[w]+half[w]+.25f,true);
            }
            Box(M.brick,Lo(xo,xi),Hi(xo,xi),y0,y1,prev,TowerHalfD,true);
            for(int f=2;f<GuestFloors+2;f++)
            {
                float b0=Level(f),b1=Level(f+1);
                for(int w=0;w<4;w++)
                {
                    float a=wins[w]-half[w],b=wins[w]+half[w];
                    In(tower,false);
                    Box(M.brick,Lo(xo,xi),Hi(xo,xi),b0-Slab,b0+.6f,a,b,true);
                    Box(M.brick,Lo(xo,xi),Hi(xo,xi),b1-Slab-.35f,b1-Slab,a,b,true);
                    Box(M.glass,sx*(TowerHalfW-.16f),sx*(TowerHalfW-.14f),b0+.6f,b1-Slab-.35f,a,b,true);
                    In(details,false);
                    Box(M.darkBronze,sx*(TowerHalfW-.2f),sx*(TowerHalfW-.1f),b0+.6f,b0+.66f,a,b);
                    Box(M.darkBronze,sx*(TowerHalfW-.2f),sx*(TowerHalfW-.1f),b1-Slab-.41f,b1-Slab-.35f,a,b);
                }
            }
        }
        // inactive (streamed-out) floors still read as rooms from outside: back walls inside the corridor walls
        In(tower,true);
        for(int f=2;f<GuestFloors+2;f++)
        {
            float b0=Level(f),b1=CeilingOf(f);
            foreach(float sz in new[]{-1f,1f})
            {
                float z=sz*(RoomWallZ+.05f);
                Face(M.paint,new Vector3(-TowerHalfW+.3f,b0,z),new Vector3(TowerHalfW-.3f,b0,z),new Vector3(TowerHalfW-.3f,b1,z),new Vector3(-TowerHalfW+.3f,b1,z),new Vector3(0,0,sz));
            }
        }
        // stone bands with LED: podium top, mid tower, crown base
        In(tower,false);
        foreach(int f in new[]{2,15})StoneBand(Level(f)-Slab-.15f,Level(f)+.05f);
        StoneBand(RoofDeck-Slab-.2f,RoofDeck+.25f);
    }
    static void StoneBand(float y0,float y1)
    {
        In(tower,false);
        foreach(float sz in new[]{-1f,1f})Box(M.podiumStone,-TowerHalfW-.25f,TowerHalfW+.25f,y0,y1,Lo(sz*(TowerHalfD+.25f),sz*(TowerHalfD-.3f)),Hi(sz*(TowerHalfD+.25f),sz*(TowerHalfD-.3f)),true);
        foreach(float sx in new[]{-1f,1f})Box(M.podiumStone,Lo(sx*(TowerHalfW+.25f),sx*(TowerHalfW-.3f)),Hi(sx*(TowerHalfW+.25f),sx*(TowerHalfW-.3f)),y0,y1,-TowerHalfD,TowerHalfD,true);
        In(details,false);
        foreach(float sz in new[]{-1f,1f})Box(M.ledCool,-TowerHalfW-.25f,TowerHalfW+.25f,y0-.04f,y0,sz*(TowerHalfD+.2f),sz*(TowerHalfD+.24f));
        foreach(float sx in new[]{-1f,1f})Box(M.ledCool,sx*(TowerHalfW+.2f),sx*(TowerHalfW+.24f),y0-.04f,y0,-TowerHalfD,TowerHalfD);
        In(tower,false);
    }
    // Sheer curtains parked at the outer sides of both room windows (always on: rooms read lived-in from outside).
    static void Curtains(bool alongX,float sz,float b0)
    {
        float y0=b0+.62f,y1=b0+GuestStorey-Slab-.38f,z=sz*(TowerHalfD-.42f);
        for(int i=0;i<5;i++)
        {
            float x=-TowerHalfW+i*RoomW;
            Box(M.curtain,x+.5f,x+1.1f,y0,y1,z-.02f,z+.02f);
            Box(M.curtain,x+RoomW-1.1f,x+RoomW-.5f,y0,y1,z-.02f,z+.02f);
        }
    }

    // ================================================================ ROOF (8 m: technical + terrace)
    static void Roof()
    {
        In(roof,false);
        float y=RoofDeck,e=TowerHalfW-.3f,f=TowerHalfD-.3f,pz=2.8f;
        // terrace paving everywhere outside the technical box and the stair penthouses
        Box(M.paving,-e,e,y,y+.02f,-f,TechZ0);
        Box(M.paving,-e,e,y,y+.02f,TechZ1,f);
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.paving,sx*TechX,sx*(StairX0-.2f),y,y+.02f,TechZ0,TechZ1);
            Box(M.paving,sx*(StairX0-.2f),sx*e,y,y+.02f,TechZ0,-pz);
            Box(M.paving,sx*(StairX0-.2f),sx*e,y,y+.02f,pz,TechZ1);
        }
        // frameless glass balustrade on the crown stone band
        foreach(float sz in new[]{-1f,1f})Box(M.clearGlass,-TowerHalfW,TowerHalfW,y+.25f,y+Balustrade,sz*(TowerHalfD-.04f),sz*(TowerHalfD-.02f),true);
        foreach(float sx in new[]{-1f,1f})Box(M.clearGlass,sx*(TowerHalfW-.04f),sx*(TowerHalfW-.02f),y+.25f,y+Balustrade,-TowerHalfD,TowerHalfD,true);
        // technical box over the lift bank: lift overrun + plant + a 2.7 m lift lobby opening onto the terrace
        float t0=y,t1=y+5f;
        Box(M.brick,-TechX,TechX,t0,t1,TechZ1-.3f,TechZ1,true);
        foreach(float sx in new[]{-1f,1f})Box(M.brick,sx*(TechX-.3f),sx*TechX,t0,t1,TechZ0,TechZ1,true);
        Box(M.brick,-TechX,-1.6f,t0,t1,TechZ0,TechZ0+.3f,true);Box(M.brick,1.6f,TechX,t0,t1,TechZ0,TechZ0+.3f,true);
        Box(M.brick,-1.6f,1.6f,t0+3.0f,t1,TechZ0,TechZ0+.3f,true);
        Box(M.clearGlass,-1.6f,1.6f,t0+2.4f,t0+3.0f,TechZ0+.14f,TechZ0+.16f,true);
        In(details,false);
        Box(M.steel,-1.7f,1.7f,t0+2.36f,t0+2.44f,TechZ0-.04f,TechZ0+.34f);
        foreach(float sx in new[]{-1f,1f})Box(M.steel,sx*1.6f-.05f,sx*1.6f+.05f,t0,t0+2.44f,TechZ0-.04f,TechZ0+.34f);
        In(roof,false);
        Box(M.concrete,-TechX,TechX,t1,t1+.3f,TechZ0,TechZ1,true);
        In(roof,true);
        LiftFront(t0,t1);
        foreach(float sx in new[]{-1f,1f})WallX(M.concrete,-CoreWallZ,Lo(sx*LiftBank,sx*(TechX-.3f)),Hi(sx*LiftBank,sx*(TechX-.3f)),t0,t1,.2f);
        Box(M.terrazzo,-TechX+.3f,TechX-.3f,y,y+Fin,TechZ0+.3f,-CoreWallZ-.1f);
        Ceil(-TechX+.3f,TechX-.3f,TechZ0+.3f,-CoreWallZ-.1f,t1-.01f);
        Box(M.ledCool,-6f,6f,t1-.025f,t1-.01f,-4.1f,-4.0f);
        W.Lampa("roof lift lobby light",new Vector3(0,t1-.6f,-4f),.9f,8f);
        In(roof,false);
        // crown: dark bronze louvre screen to 110.30 m with an LED line on top (board render)
        float c0=t1+.3f,c1=Crown;
        for(float x=-TechX+.2f;x<=TechX-.19f;x+=.4f)foreach(float z in new[]{TechZ0-.05f,TechZ1+.05f})Box(M.darkBronze,x-.05f,x+.05f,c0,c1,z-.05f,z+.05f);
        for(float z=TechZ0+.2f;z<=TechZ1-.19f;z+=.4f)foreach(float sx in new[]{-1f,1f})Box(M.darkBronze,sx*(TechX+.05f)-.05f,sx*(TechX+.05f)+.05f,c0,c1,z-.05f,z+.05f);
        In(details,false);
        foreach(float z in new[]{TechZ0-.05f,TechZ1+.05f})Box(M.ledCool,-TechX-.1f,TechX+.1f,c1,c1+.05f,z-.05f,z+.05f);
        foreach(float sx in new[]{-1f,1f})Box(M.ledCool,sx*(TechX+.05f)-.05f,sx*(TechX+.05f)+.05f,c1,c1+.05f,TechZ0,TechZ1);
        // stair penthouses (closed on all four sides) with a door to the terrace
        In(roof,false);
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*(StairX0-.2f),b=sx*TowerHalfW,ph=y+3.5f;
            Box(M.brick,Lo(a,b),Hi(a,b),y,ph,2.6f,pz,true);
            WallX(M.brick,-2.7f,Lo(a,b),Hi(a,b),y,ph,.2f,new Gap(sx<0?-12.5f:11.4f,sx<0?-11.4f:12.5f,2.3f));
            Box(M.brick,Lo(a,a+sx*.2f),Hi(a,a+sx*.2f),y,ph,-2.6f,2.6f,true);
            Box(M.brick,Lo(b,b-sx*.3f),Hi(b,b-sx*.3f),y,ph,-2.6f,2.6f,true);
            Box(M.concrete,Lo(a,b),Hi(a,b),ph,ph+.25f,-pz,pz,true);
            Box(M.terrazzo,Lo(a+sx*.2f,sx*StairLand),Hi(a+sx*.2f,sx*StairLand),y,y+Fin,-2.4f,2.4f);
            // the last flight only arrives: guard the open well of the missing up-flight
            Box(M.clearGlass,Lo(sx*StairLand,sx*(StairLand+.02f)),Hi(sx*StairLand,sx*(StairLand+.02f)),y,y+Balustrade,-2.4f,0f,true);
            Box(M.clearGlass,Lo(sx*StairLand,sx*StairX1),Hi(sx*StairLand,sx*StairX1),y,y+Balustrade,-.015f,.015f,true);
            Rod(M.steel,new Vector3(sx*StairLand,y+Balustrade,-2.4f),new Vector3(sx*StairLand,y+Balustrade,0f),.022f);
            In(roof,true);
            Ceil(Lo(a+sx*.2f,b-sx*.3f),Hi(a+sx*.2f,b-sx*.3f),-2.6f,2.6f,ph-.01f);
            W.Lampa("stair penthouse light "+(sx<0?"W":"E"),new Vector3(sx*14f,ph-.4f,0),.7f,7f);
            In(roof,false);
        }
        // terrace: white leather loungers and planters
        foreach(float x in new[]{-13f,-8f,8f,13f})Lounger(roof,x,y,-11f);
        foreach(float x in new[]{-12f,12f})RoofPlanter(x,10f,2.5f,1.0f,y+.02f);
    }
    static void Lounger(Transform g,float x,float y,float z)
    {
        Box(M.leatherWhite,x-.35f,x+.35f,y+.25f,y+.40f,z-1.0f,z+.6f,true);
        W.OBox(M.leatherWhite,new Vector3(x,y+.62f,z+.9f),new Vector3(.7f,.08f,.8f),Quaternion.Euler(-35f,0,0));
        Box(M.teakDark,x-.30f,x+.30f,y,y+.25f,z-.9f,z+.8f,true);
    }

    // ================================================================ LIFT SHAFTS (continuous) + per-stop fronts
    static void LiftShafts()
    {
        In(cores,true);
        float y0=Ffl0-.2f,y1=RoofDeck+4.6f;
        foreach(float x in new[]{-4.05f,-1.35f,1.35f,4.05f})Box(M.concrete,x-.1f,x+.1f,y0,y1,ShaftFront,ShaftBack+.2f,true);
        Box(M.concrete,-4.15f,4.15f,y0,y1,ShaftBack,ShaftBack+.2f,true);
    }
    // Lift front wall of one stop (corridor face at z=-2.6): three 1.10 m openings, steel portals, call panels.
    static void LiftFront(float y0,float y1)
    {
        float z=-CoreWallZ,t=.2f;
        var gaps=new List<Gap>();foreach(float x in LiftX)gaps.Add(new Gap(x-LiftDoorHalf,x+LiftDoorHalf,2.2f));
        float x0=-LiftBank,cursor=x0;
        foreach(var g in gaps)
        {
            Box(M.paint,cursor,g.a,y0,y1,z-t*.5f,z+t*.5f,true);Box(M.paint,g.a,g.b,y0+g.top,y1,z-t*.5f,z+t*.5f,true);cursor=g.b;
        }
        Box(M.paint,cursor,LiftBank,y0,y1,z-t*.5f,z+t*.5f,true);
        // corridor face: teak cladding, steel portals, call buttons
        Box(M.teak,-LiftBank,LiftBank,y0+2.2f+.15f,y0+2.9f,z-t*.5f-.02f,z-t*.5f);
        foreach(float x in LiftX)
        {
            Box(M.steel,x-LiftDoorHalf-.1f,x-LiftDoorHalf,y0,y0+2.35f,z-t*.5f-.04f,z+t*.5f);
            Box(M.steel,x+LiftDoorHalf,x+LiftDoorHalf+.1f,y0,y0+2.35f,z-t*.5f-.04f,z+t*.5f);
            Box(M.steel,x-LiftDoorHalf-.1f,x+LiftDoorHalf+.1f,y0+2.2f,y0+2.35f,z-t*.5f-.04f,z+t*.5f);
            Box(M.steel,x+LiftDoorHalf+.22f,x+LiftDoorHalf+.34f,y0+1.0f,y0+1.3f,z-t*.5f-.03f,z-t*.5f);
            Box(M.warmLight,x+LiftDoorHalf+.255f,x+LiftDoorHalf+.305f,y0+1.12f,y0+1.17f,z-t*.5f-.04f,z-t*.5f-.03f);
        }
    }
    static Mesh LeafMesh()
    {
        if(leafMesh)return leafMesh;
        float hx=LiftDoorHalf*.5f,hy=1.1f,hz=.025f;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        void F(Vector3 c,Vector3 u,Vector3 w,Vector3 nn)
        {
            int s=v.Count;v.Add(c-u-w);v.Add(c+u-w);v.Add(c+u+w);v.Add(c-u+w);
            for(int i=0;i<4;i++)n.Add(nn);
            uv.Add(new Vector2(0,0));uv.Add(new Vector2(u.magnitude*2,0));uv.Add(new Vector2(u.magnitude*2,w.magnitude*2));uv.Add(new Vector2(0,w.magnitude*2));
            // Unity front face = Cross(b-a,c-a) toward the viewer (same rule as W.Quad)
            if(Vector3.Dot(Vector3.Cross(u,w),nn)>0){t.Add(s);t.Add(s+1);t.Add(s+2);t.Add(s);t.Add(s+2);t.Add(s+3);}
            else{t.Add(s);t.Add(s+2);t.Add(s+1);t.Add(s);t.Add(s+3);t.Add(s+2);}
        }
        var X=new Vector3(hx,0,0);var Y=new Vector3(0,hy,0);var Z=new Vector3(0,0,hz);
        F(Z,X,Y,Vector3.forward);F(-Z,X,Y,Vector3.back);F(X,Z,Y,Vector3.right);F(-X,Z,Y,Vector3.left);F(Y,X,Z,Vector3.up);F(-Y,X,Z,Vector3.down);
        leafMesh=new Mesh{name="T03 lift landing door leaf 0.55 x 2.20"};
        leafMesh.SetVertices(v);leafMesh.SetNormals(n);leafMesh.SetUVs(0,uv);leafMesh.SetTriangles(t,0);
        leafMesh.RecalculateBounds();leafMesh.RecalculateTangents();
        AssetDatabase.CreateAsset(leafMesh,Out+"/Meshes/lift_landing_leaf.asset");
        return leafMesh;
    }
    static Transform LeafObject(Transform parent,string name,Vector3 local)
    {
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=local;
        o.AddComponent<MeshFilter>().sharedMesh=LeafMesh();
        var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=M.steel;
        var c=o.AddComponent<BoxCollider>();c.size=new Vector3(LiftDoorHalf,2.2f,.05f);
        return o.transform;
    }
    // Moving cabins: built at local floor y=0; BraxWinda lifts them to the start stop.
    static Transform[] LiftCabins()
    {
        var result=new Transform[Elevators];
        for(int l=0;l<Elevators;l++)
        {
            float xc=LiftX[l],z0=-2.3f,z1=-.05f,hw=1.1f;
            var cab=G(cores,"ELEVATOR "+(l+1)+" | PASSENGER | cabin");
            In(cab,true);
            Box(M.marble,xc-hw,xc+hw,-.12f,0f,z0,z1,true);
            Box(M.mirror,xc-hw+.05f,xc+hw-.05f,.9f,2.4f,z1-.05f,z1);
            Box(M.teak,xc-hw,xc+hw,0f,2.5f,z1,z1+.05f,true);
            foreach(float sx in new[]{-1f,1f})Box(M.teak,sx>0?xc+hw:xc-hw-.05f,sx>0?xc+hw+.05f:xc-hw,0f,2.5f,z0,z1,true);
            Box(M.steel,xc-hw,xc+hw,2.5f,2.58f,z0,z1);
            Box(M.ledCool,xc-.6f,xc+.6f,2.47f,2.5f,z0+.4f,z1-.4f);
            Rod(M.brass,new Vector3(xc-hw+.15f,.95f,z1-.09f),new Vector3(xc+hw-.15f,.95f,z1-.09f),.022f);
            foreach(float sx in new[]{-1f,1f})Box(M.steel,sx>0?xc+LiftDoorHalf:xc-hw,sx>0?xc+hw:xc-LiftDoorHalf,0f,2.5f,z0-.03f,z0,true);
            Box(M.steel,xc-hw,xc+hw,2.2f,2.5f,z0-.03f,z0,true);
            Box(M.steel,xc+.75f,xc+.95f,1.0f,1.5f,z0+.01f,z0+.04f);
            var cabL=G(cab,"cabin door L");var cabR=G(cab,"cabin door R");
            In(cabL,true);Box(M.steel,xc-LiftDoorHalf,xc,0f,2.2f,z0-.06f,z0-.03f,true);
            In(cabR,true);Box(M.steel,xc,xc+LiftDoorHalf,0f,2.2f,z0-.06f,z0-.03f,true);
            In(cab,true);
            W.Lampa("lift "+(l+1)+" cabin light",new Vector3(xc,2.2f,(z0+z1)*.5f),.7f,3f);
            result[l]=cab;
        }
        return result;
    }
    static void Elevator(int l,Transform cab,Transform[,] landingL,Transform[,] landingR)
    {
        var c=cab.gameObject.AddComponent<BraxWinda>();
        c.nazwa="HOTEL SOBCZAK · WINDA "+(l+1);
        c.kabina=cab;c.kabinaL=cab.Find("cabin door L");c.kabinaP=cab.Find("cabin door R");
        var ys=new float[Stops];var names=new string[Stops];var usable=new bool[Stops];var calls=new Vector3[Stops];
        var pl=new Transform[Stops];var pr=new Transform[Stops];
        for(int s=0;s<Stops;s++)
        {
            ys[s]=root.TransformPoint(new Vector3(0,Level(s),0)).y;names[s]=StopName(s);usable[s]=true;
            calls[s]=root.TransformPoint(new Vector3(LiftX[l],Level(s)+1.2f,-CoreWallZ-.9f));
            pl[s]=landingL[l,s];pr[s]=landingR[l,s];
        }
        c.pietraY=ys;c.pietraNazwy=names;c.uzytkowe=usable;c.wezwanie=calls;c.przystanekL=pl;c.przystanekP=pr;
        var shaft=root.TransformPoint(new Vector3(LiftX[l],0,(ShaftFront+ShaftBack)*.5f));
        c.szyb=new Vector2(shaft.x,shaft.z);c.wnetrze=new Vector2(1.05f*K,1.1f*K);
        c.przesuw=LiftDoorHalf*.95f;c.start=0;
        foreach(var t in cab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        foreach(var t in pl)if(t)GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        foreach(var t in pr)if(t)GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
    }

    // ================================================================ STAIRS (dog-leg, flights along x)
    // side +1 = east core, -1 = west core; cycles split tall storeys (6 m, 5 m) into <=3.5 m dog-legs.
    static void Stair(float side,float y0,float y1,int cycles)
    {
        float h=(y1-y0)/cycles;int n=Risers(h);float r=h/n;int half=n/2;float run=half*Tread;
        float X(float u)=>side*u;
        for(int c=0;c<cycles;c++)
        {
            float a=y0+c*h,mid=a+h*.5f,b=a+h;
            for(int k=0;k<half;k++)
            {
                float t1=a+(k+1)*r,t2=mid+(k+1)*r;
                Box(M.terrazzo,X(StairLand+k*Tread),X(StairLand+(k+1)*Tread),t1-r-.16f,t1,-LaneOut,-LaneIn);
                Box(M.terrazzo,X(StairLand+run-(k+1)*Tread),X(StairLand+run-k*Tread),t2-r-.16f,t2,LaneIn,LaneOut);
            }
            Box(M.terrazzo,X(StairLand+run),X(StairX1),mid-.2f,mid,-LaneOut,LaneOut,true);              // half landing
            W.Rampa(new Vector3(X(StairLand+run),mid,-(LaneIn+LaneOut)*.5f),new Vector3(X(StairLand),a,-(LaneIn+LaneOut)*.5f),LaneOut-LaneIn);
            W.Rampa(new Vector3(X(StairLand),b,(LaneIn+LaneOut)*.5f),new Vector3(X(StairLand+run),mid,(LaneIn+LaneOut)*.5f),LaneOut-LaneIn);
            Box(M.clearGlass,X(StairLand),X(StairLand+run),a,b,-.015f,.015f,true);                      // glass between flights
            Rod(M.steel,new Vector3(X(StairLand),a+.9f,-.12f),new Vector3(X(StairLand+run),mid+.9f,-.12f),.022f);
            Rod(M.steel,new Vector3(X(StairLand+run),mid+.9f,.12f),new Vector3(X(StairLand),b+.9f,.12f),.022f);
            if(c<cycles-1)Box(M.terrazzo,X(StairX0),X(StairLand),b-.2f,b,-2.4f,2.4f,true);                // intermediate landing
            Downlight(X((StairLand+run+StairX1)*.5f),0,b-.22f);
        }
    }

    // ================================================================ GROUND FLOOR (6 m)
    static void GroundFloor()
    {
        In(podiumInside,true);
        float y0=Ffl0,yc=CeilingOf(0)-.01f,top=Level(1);
        float zf=-PodiumHalfD+.3f,zb=PodiumHalfD-.3f,xw=PodiumHalfW-.3f;
        // floors: cream marble lobby with black marble border, wings marble, back zones
        Box(M.marble,-11.2f,11.2f,y0,y0+Fin,-14.5f,-CoreWallZ-.1f);
        Box(M.blackMarble,-11.2f,11.2f,y0+Fin,y0+Fin+.002f,-14.5f,-14.1f);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*11.4f,sx*xw,y0,y0+Fin,zf,-CoreWallZ-.1f);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*4f,sx*11.2f,y0,y0+Fin,zf,-14.5f);
        Box(M.carpet,-xw,xw,y0,y0+Fin,CoreWallZ+.1f,RoomWallZ-.1f);                                   // back corridor
        Box(M.oak,-11.2f,11.2f,y0,y0+Fin,RoomWallZ+.1f,zb);                                            // waiting lounge
        foreach(float sx in new[]{-1f,1f})Box(M.tile,sx*11.4f,sx*xw,y0,y0+Fin,RoomWallZ+.1f,zb);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*(StairX1+.2f),sx*xw,y0,y0+Fin,-2.4f,2.4f);     // side passage wing -> back
        // ceiling (5.70 m clear) with LED coves, round brass chandelier ring over the lounge; stairwells stay open
        CeilAround(xw,zb,yc);
        for(float z=-13f;z<=-4f;z+=3f)Box(M.ledCool,-10.5f,10.5f,yc-.015f,yc,z-.04f,z+.04f);
        var ring=Circle(-6.5f,-9f,2.4f,32);
        In(podiumInside,true);
        Ring(ring,yc-1.5f,yc-1.38f,.08f,M.brass,false);
        Ring(Circle(-6.5f,-9f,2.25f,32),yc-1.52f,yc-1.49f,.06f,M.warmLight,false);
        foreach(float a in new[]{0f,120f,240f})
        {
            float rad=a*Mathf.Deg2Rad;Rod(M.brass,new Vector3(-6.5f+Mathf.Cos(rad)*2.4f,yc-1.38f,-9f+Mathf.Sin(rad)*2.4f),new Vector3(-6.5f,yc,-9f),.012f);
        }
        // walls: wings (wide openings), core band toilets, back corridor walls
        WallZ(M.paint,-11.3f,zf,-CoreWallZ,y0,top,.2f,new Gap(-13.5f,-6.5f,4.2f));                    // bar opening 7 m
        WallZ(M.paint,11.3f,zf,-CoreWallZ,y0,top,.2f,new Gap(-17.8f,-15.2f,2.8f),new Gap(-4.9f,-4.0f,2.1f));   // staff door behind reception
        Leaf(false,11.3f,-4.9f,-4.0f,y0,1f);
        // reception: teak counter with a marble top at 1.10 m, LED line, backlit teak wall with the small 3D sign
        Box(M.teak,6.2f,7.0f,y0,y0+1.04f,-12.6f,-6.2f,true);
        Box(M.marble,6.1f,7.1f,y0+1.04f,y0+1.10f,-12.7f,-6.1f,true);
        Box(M.ledCool,6.18f,6.2f,y0+.08f,y0+.11f,-12.5f,-6.3f);
        Box(M.teakDark,7.0f,7.6f,y0,y0+.75f,-12.4f,-6.4f,true);
        Box(M.teak,11.15f,11.2f,y0,yc,-13.8f,-5f);
        Box(M.ledCool,11.12f,11.15f,y0+2.4f,y0+2.43f,-13.6f,-5.2f);
        Box(M.ledCool,11.12f,11.15f,y0+4.6f,y0+4.63f,-13.6f,-5.2f);
        W.Lampa("reception light",new Vector3(8.5f,yc-1f,-9.4f),1.0f,9f);
        // lobby lounge: real leather sofas and armchairs (Poly Haven CC0), marble tables, plants, rug
        Box(M.carpetRoom,-10f,-3f,y0+Fin,y0+Fin+.008f,-12.3f,-5.7f);
        LobbyModel("sofa_02/sofa_02_2k.gltf","lobby leather sofa",-6.5f,y0,-11.6f,0f,true);
        LobbyModel("sofa_02/sofa_02_2k.gltf","lobby leather sofa",-6.5f,y0,-6.4f,180f,true);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","lobby leather armchair",-9.4f,y0,-9f,90f,true);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","lobby leather armchair",-3.6f,y0,-9f,-90f,true);
        CoffeeTable(-6.5f,y0,-9f,1.6f,.9f);
        foreach(var p in new[]{new Vector2(-10.6f,-13.6f),new Vector2(-2.4f,-13.6f),new Vector2(-10.6f,-4.4f),new Vector2(2.6f,-13.6f)})
            LobbyModel("potted_plant_01/potted_plant_01_1k.gltf","lobby plant",p.x,y0,p.y,(p.x*13f)%360f,false);
        W.Lampa("lobby light",new Vector3(0,yc-1.2f,-9f),1.1f,13f);
        // lobby bar (west wing): teak counter, marble top, backlit shelves, leather stools, armchairs
        Box(M.teak,-15.6f,-14.8f,y0,y0+1.05f,-16.5f,-6f,true);
        Box(M.marble,-15.75f,-14.65f,y0+1.05f,y0+1.11f,-16.6f,-5.9f,true);
        Box(M.teakDark,-xw,-xw+.45f,y0,y0+2.6f,-16.5f,-6f,true);
        for(float yy=y0+1.1f;yy<y0+2.6f;yy+=.5f)Box(M.ledCool,-xw+.42f,-xw+.45f,yy,yy+.02f,-16.4f,-6.1f);
        for(float z=-15.6f;z<=-6.8f;z+=1.25f)Stool(-14.0f,y0,z);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","bar leather armchair",-13.0f,y0,-17.9f,0f,true);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","bar leather armchair",-18.0f,y0,-17.9f,0f,true);
        W.Lampa("bar light",new Vector3(-16f,yc-1f,-11f),.9f,9f);
        // east wing: concierge desk, luggage room
        Box(M.teak,15.5f,16.3f,y0,y0+1.04f,-17.6f,-15.4f,true);Box(M.marble,15.4f,16.4f,y0+1.04f,y0+1.10f,-17.7f,-15.3f,true);
        WallX(M.paint,-10f,14.0f,xw,y0,top,.2f,new Gap(16.2f,17.2f,2.1f));       // luggage room (bagazownia)
        WallZ(M.paint,13.9f,-10f,-CoreWallZ,y0,top,.2f);
        Leaf(true,-10f,16.2f,17.2f,y0,1f);
        for(float z=-8.8f;z<=-4.2f;z+=1.5f)Box(M.teakDark,18.6f,xw,y0,y0+2.2f,z-.25f,z+.25f,true);
        W.Lampa("concierge light",new Vector3(16f,yc-1f,-14f),.8f,8f);
        // core band: guest toilets (men west, women east) opening to the lobby
        foreach(float sx in new[]{-1f,1f})Toilets(sx,y0,top,yc);
        LiftFront(y0,top);
        Box(M.marble,-LiftBank,LiftBank,y0+2.9f,yc,-CoreWallZ-.12f,-CoreWallZ-.1f);          // marble-clad lift wall up to the ceiling
        CoreBack(y0,top);
        // back: corridor walls, waiting lounge (poczekalnia), luggage store, staff room with service exit
        WallX(M.paint,RoomWallZ,-xw,xw,y0,top,.2f,new Gap(-9f,9f,3.6f),new Gap(-16.6f,-15.6f,2.1f),new Gap(15.6f,16.6f,2.1f));
        Leaf(true,RoomWallZ,-16.6f,-15.6f,y0,1f);Leaf(true,RoomWallZ,15.6f,16.6f,y0,1f);
        foreach(float sx in new[]{-1f,1f})WallZ(M.paint,sx*11.3f,RoomWallZ+.1f,zb,y0,top,.2f);
        Box(M.carpetRoom,-8f,8f,y0+Fin,y0+Fin+.008f,8.5f,15.5f);
        LobbyModel("sofa_02/sofa_02_2k.gltf","waiting leather sofa",-4f,y0,15.6f,180f,true);
        LobbyModel("sofa_02/sofa_02_2k.gltf","waiting leather sofa",4f,y0,15.6f,180f,true);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","waiting leather armchair",-7.5f,y0,11f,90f,true);
        LobbyModel("modern_arm_chair_01/modern_arm_chair_01_2k.gltf","waiting leather armchair",7.5f,y0,11f,-90f,true);
        CoffeeTable(-4f,y0,12.4f,1.4f,.8f);CoffeeTable(4f,y0,12.4f,1.4f,.8f);
        foreach(float x in new[]{-10.4f,10.4f})LobbyModel("potted_plant_02/potted_plant_02_1k.gltf","waiting plant",x,y0,17.6f,0f,false);
        W.Lampa("waiting lounge light",new Vector3(0,yc-1f,12f),.9f,11f);
        foreach(float z in new[]{8f,11.5f,15f})                                                                  // back office desks
        {
            Box(M.teak,-19.6f,-16.4f,y0+.72f,y0+.76f,z-.6f,z+.6f,true);
            Box(M.teakDark,-19.55f,-16.45f,y0,y0+.72f,z+.5f,z+.56f);
            Box(M.leather,-18.3f,-17.7f,y0+.42f,y0+.5f,z-1.3f,z-.7f,true);
        }
        for(float z=6.5f;z<=17.5f;z+=1.6f)Box(M.teakDark,-xw,-xw+.6f,y0,y0+2.2f,z-.35f,z+.35f,true);              // archive shelves
        for(float z=6.5f;z<=12f;z+=1.1f)Box(M.teak,xw-.6f,xw,y0,y0+2.0f,z-.5f,z+.5f,true);                       // staff lockers
        Box(M.teak,14f,17f,y0+.72f,y0+.76f,14f,16f,true);
        W.Lampa("back of house light",new Vector3(16f,yc-1f,12f),.7f,9f);
        Stair(-1f,Level(0),Level(1),2);Stair(1f,Level(0),Level(1),2);
        StairWalls(y0,top,true);
    }
    static void Toilets(float sx,float y0,float top,float yc)
    {
        float a=sx*4.15f,b=sx*11.2f,z0=-CoreWallZ+.1f,z1=CoreWallZ-.1f,x0=Lo(a,b),x1=Hi(a,b);
        WallX(M.paint,-CoreWallZ,x0,x1,y0,top,.2f,new Gap(sx<0?-9.6f:8.7f,sx<0?-8.7f:9.6f,2.1f));
        Leaf(true,-CoreWallZ,sx<0?-9.6f:8.7f,sx<0?-8.7f:9.6f,y0,1f);
        WallX(M.paint,CoreWallZ,x0,x1,y0,top,.2f);
        Box(M.marbleWet,x0,x1,y0,y0+Fin,z0,z1);
        Box(M.marble,x0,x1,y0,y0+3.0f,z1-.01f,z1);
        for(int i=0;i<3;i++)
        {
            float x=sx*(5.2f+i*1.3f);
            Box(M.whitePaint,x-.2f,x+.2f,y0,y0+.42f,z1-.7f,z1-.05f,true);
            Box(M.teakDark,x+.62f,x+.67f,y0+.1f,y0+2.1f,z1-1.6f,z1,true);
        }
        float v0=Lo(sx*4.35f,sx*8.4f),v1=Hi(sx*4.35f,sx*8.4f);                 // vanity clear of the door (8.7-9.6)
        Box(M.marble,v0,v1,y0+.82f,y0+.88f,z0,z0+.55f,true);
        Box(M.teakDark,v0+.05f,v1-.05f,y0+.15f,y0+.82f,z0,z0+.5f);
        for(float x=v0+.6f;x<v1-.3f;x+=1.2f)Box(M.whitePaint,x-.25f,x+.25f,y0+.84f,y0+.92f,z0+.1f,z0+.42f);
        Box(M.mirror,v0+.1f,v1-.1f,y0+1.1f,y0+2.1f,z0,z0+.02f);
        Ceil(x0,x1,z0,z1,y0+3.0f);
        W.Lampa((sx<0?"men":"women")+" toilet light",new Vector3((x0+x1)*.5f,y0+2.6f,0),.7f,6f);
    }
    // Stair enclosures: core walls with the landing openings, side walls to the service rooms / toilets.
    static void StairWalls(float y0,float top,bool podiumLevel)
    {
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*StairX0,b=sx*StairX1;
            foreach(float sz in new[]{-1f,1f})
                WallX(M.paint,sz*CoreWallZ,Lo(sx*11.2f,b),Hi(sx*11.2f,b),y0,top,.2f,new Gap(sx<0?-12.5f:11.4f,sx<0?-11.4f:12.5f,2.4f));
            WallZ(M.paint,sx*11.25f,-CoreWallZ+.1f,CoreWallZ-.1f,y0,top,.1f);
            if(podiumLevel)WallZ(M.paint,sx*(StairX1+.1f),-CoreWallZ-.1f,CoreWallZ+.1f,y0,top,.2f);
            Box(M.terrazzo,Lo(a,sx*StairLand),Hi(a,sx*StairLand),y0,y0+Fin,-2.4f,2.4f);
            if(podiumLevel)Box(M.terrazzo,Lo(sx*StairLand,b),Hi(sx*StairLand,b),y0,y0+Fin,-2.4f,2.4f);
        }
    }
    static void CoffeeTable(float x,float y,float z,float w,float d)
    {
        Box(M.blackMarble,x-w*.5f,x+w*.5f,y+.40f,y+.45f,z-d*.5f,z+d*.5f,true);
        foreach(float dx in new[]{-1f,1f})Box(M.brass,x+dx*(w*.5f-.12f)-.02f,x+dx*(w*.5f-.12f)+.02f,y,y+.40f,z-d*.5f+.08f,z+d*.5f-.08f);
    }
    static void Stool(float x,float y,float z)
    {
        Rod(M.brass,new Vector3(x,y,z),new Vector3(x,y+.72f,z),.03f);
        Prism(Circle(x,z,.2f,12),y,y+.02f,M.brass,M.brass,false);
        Prism(Circle(x,z,.21f,12),y+.72f,y+.80f,M.leather,M.leather,true);
    }
    // Poly Haven CC0 leather sofa / armchair / plant, real size, standing exactly on the floor.
    static bool LobbyModel(string file,string label,float x,float y,float z,float yaw,bool solid)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
        if(!source)
        {
            BraxBuildQuality.Warn("DO POPRAWKI T03_LOBBY_MODEL_MISSING "+file+" (prepare_arena_lobby_models.py)"+(file.StartsWith("potted")?"":" -> procedural leather piece"));
            if(file.StartsWith("sofa"))ProceduralSofa(x,y,z,yaw,2.2f);
            else if(file.StartsWith("modern"))ProceduralSofa(x,y,z,yaw,.85f);
            return false;
        }
        if(!HasTextures(source))   // import-order guard (as in the arena): re-import once so the leather gets its maps
        {
            AssetDatabase.ImportAsset(LobbyModels+file,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            source=AssetDatabase.LoadAssetAtPath<GameObject>(LobbyModels+file);
            if(!source)return false;
        }
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!go)return false;
        go.name="T03 LOBBY | "+label+" | Poly Haven CC0 leather, real size";
        go.transform.SetParent(podiumInside,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;
        go.transform.localScale=Vector3.one;                              // root still at scale 1: real metres
        go.transform.localPosition=new Vector3(x,y,z);
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0)
        {
            var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            go.transform.position+=Vector3.up*(y+Fin-b.min.y);
            if(solid)
            {
                b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                var box=go.AddComponent<BoxCollider>();box.center=go.transform.InverseTransformPoint(b.center);
                var size=go.transform.InverseTransformVector(b.size);box.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
            }
        }
        if(!HasTextures(go))BraxBuildQuality.Warn("DO POPRAWKI T03_LOBBY_MODEL_UNTEXTURED "+file);
        modelsPlaced++;
        return true;
    }
    static bool HasTextures(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            foreach(var m in r.sharedMaterials)
                if(m)foreach(var n in m.GetTexturePropertyNames())if(m.GetTexture(n))return true;
        return false;
    }
    static void ProceduralSofa(float x,float y,float z,float yaw,float width)
    {
        var q=Quaternion.Euler(0,yaw,0);
        Vector3 P(float lx,float ly,float lz)=>new Vector3(x,y,z)+q*new Vector3(lx,ly,lz);
        W.OBox(M.leather,P(0,.25f,0),new Vector3(width,.30f,.9f),q,true);
        W.OBox(M.leather,P(0,.47f,-.05f),new Vector3(width-.3f,.14f,.75f),q);
        W.OBox(M.leather,P(0,.70f,.37f),new Vector3(width,.55f,.18f),q);
        foreach(float s in new[]{-1f,1f})W.OBox(M.leather,P(s*(width*.5f-.08f),.55f,0),new Vector3(.16f,.30f,.9f),q);
    }

    // ================================================================ FLOOR 1 (5 m)
    static void FirstFloor()
    {
        In(podiumInside,true);
        float y0=Level(1),yc=CeilingOf(1)-.01f,top=Level(2);
        float zf=-PodiumHalfD+.3f,zb=PodiumHalfD-.3f,xw=PodiumHalfW-.3f;
        // restaurant across the front: oak floor, pendant LED lines, 15 marble tables with leather chairs
        Box(M.oak,-xw,xw,y0,y0+Fin,zf,-CoreWallZ-.1f);
        CeilAround(xw,zb,yc);
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.oak,sx*(StairX1+.2f),sx*xw,y0,y0+Fin,-2.4f,2.4f);                       // nook off the restaurant
            WallX(M.paint,CoreWallZ,Lo(sx*(StairX1+.2f),sx*xw),Hi(sx*(StairX1+.2f),sx*xw),y0,top,.2f);   // kitchen / SPA stay closed
        }
        for(float x=-18f;x<=18.01f;x+=6f)Box(M.ledCool,x-2f,x+2f,yc-.6f,yc-.57f,-10.5f,-10.45f);
        for(float x=-16f;x<=8.01f;x+=6f)for(float z=-16f;z<=-6.9f;z+=4.5f)DiningTable(x,y0,z);
        Box(M.teak,13.5f,14.3f,y0,y0+1.05f,-16f,-6f,true);Box(M.marble,13.4f,14.4f,y0+1.05f,y0+1.11f,-16.1f,-5.9f,true);
        Box(M.teakDark,xw-.45f,xw,y0,y0+2.8f,-16f,-6f,true);
        for(float yy=y0+.6f;yy<y0+2.8f;yy+=.55f)Box(M.ledCool,xw-.48f,xw-.45f,yy,yy+.02f,-15.9f,-6.1f);
        W.Lampa("restaurant light W",new Vector3(-10f,yc-1f,-11f),1.0f,13f);
        W.Lampa("restaurant light E",new Vector3(8f,yc-1f,-11f),1.0f,13f);
        LiftFront(y0,top);
        // core band service rooms (servery west, SPA reception east)
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*4.15f,b=sx*11.2f;
            WallX(M.paint,-CoreWallZ,Lo(a,b),Hi(a,b),y0,top,.2f,new Gap(sx<0?-8.2f:7.0f,sx<0?-7.0f:8.2f,2.2f));
            WallX(M.paint,CoreWallZ,Lo(a,b),Hi(a,b),y0,top,.2f,new Gap(sx<0?-8.2f:7.0f,sx<0?-7.0f:8.2f,2.2f));
            Box(sx<0?M.tile:M.terrazzo,Lo(a,b),Hi(a,b),y0,y0+Fin,-2.4f,2.4f);
        }
        CoreBack(y0,top);
        Box(M.steel,-10.6f,-5f,y0,y0+.9f,-2f,-1.3f,true);                                               // pass counter
        Box(M.teak,5.5f,8.5f,y0,y0+1.05f,1.2f,1.9f,true);                                                 // SPA desk
        // kitchen (west back): stainless lines, island, hoods, cold room
        Box(M.tile,-xw,-4.3f,y0,y0+Fin,CoreWallZ+.1f,zb);
        WallZ(M.paint,-4.25f,CoreWallZ+.1f,zb,y0,top,.1f);
        for(float x=-19f;x<=-6.5f;x+=3.1f)
        {
            Box(M.steel,x-1.4f,x+1.4f,y0,y0+.9f,zb-.75f,zb,true);
            Box(M.steel,x-1.4f,x+1.4f,y0+2.2f,y0+2.7f,zb-1.0f,zb);
        }
        Box(M.steel,-15f,-8f,y0,y0+.9f,9f,10.4f,true);
        Box(M.steel,-xw,-16.5f,y0,y0+2.8f,3.2f,7.5f,true);
        W.Lampa("kitchen light",new Vector3(-12f,yc-.8f,11f),1.0f,12f);
        // SPA (east back): raised pool, loungers, treatment room
        Box(M.terrazzo,4.3f,xw,y0,y0+Fin,CoreWallZ+.1f,zb);
        WallZ(M.paint,4.25f,CoreWallZ+.1f,zb,y0,top,.1f);
        Ring(new List<Vector2>{new Vector2(8f,7.5f),new Vector2(17f,7.5f),new Vector2(17f,12f),new Vector2(8f,12f)},y0,y0+.55f,.3f,M.marble,true);
        Box(M.water,8.15f,16.85f,y0+.05f,y0+.45f,7.65f,11.85f);
        for(float x=8.5f;x<=16.6f;x+=2.7f)Lounger(podiumInside,x,y0,15.6f);
        WallX(M.paint,4.4f,15.5f,xw,y0,top,.12f,new Gap(16.2f,17.1f,2.1f));
        W.Lampa("SPA light",new Vector3(12.5f,yc-1f,10f),.9f,12f);
        // back of house behind the lift bank
        Box(M.tile,-4.15f,4.15f,y0,y0+Fin,CoreWallZ+.1f,zb);
        for(float z=4f;z<=16f;z+=3f)Box(M.teakDark,-3.8f,-3.2f,y0,y0+2.2f,z-1f,z+1f,true);
        Stair(-1f,Level(1),Level(2),2);Stair(1f,Level(1),Level(2),2);
        StairWalls(y0,top,true);
    }
    static void DiningTable(float x,float y,float z)
    {
        Rod(M.brass,new Vector3(x,y,z),new Vector3(x,y+.72f,z),.05f);
        Prism(Circle(x,z,.55f,16),y+.72f,y+.76f,M.marble,M.marble,true);
        foreach(var d in new[]{new Vector2(-.85f,0),new Vector2(.85f,0),new Vector2(0,-.85f),new Vector2(0,.85f)})
        {
            float cx=x+d.x,cz=z+d.y;
            Box(M.leather,cx-.23f,cx+.23f,y+.42f,y+.50f,cz-.23f,cz+.23f,true);
            var back=new Vector2(cx,cz)+d.normalized*.21f;
            Box(M.leather,back.x-(d.x==0?.23f:.03f),back.x+(d.x==0?.23f:.03f),y+.50f,y+.95f,back.y-(d.x==0?.03f:.23f),back.y+(d.x==0?.03f:.23f));
            Box(M.brass,cx-.02f,cx+.02f,y,y+.42f,cz-.02f,cz+.02f);
        }
    }

    // ================================================================ GUEST FLOOR TEMPLATE (y = 0 at the floor)
    static Transform GuestFloorTemplate()
    {
        var t=new GameObject("HOTEL SOBCZAK | GUEST FLOOR TEMPLATE | 10 rooms").transform;
        In(t,true);
        float h=GuestStorey,yc=h-Slab-.01f,yk=2.75f,xw=TowerHalfW-.3f;
        // corridors: carpet, dropped ceiling with LED coves, wall sconces
        foreach(float sz in new[]{-1f,1f})
        {
            float a=sz*(CoreWallZ+.1f),b=sz*(RoomWallZ-.1f);
            Box(M.carpet,-xw,xw,0,Fin,Lo(a,b),Hi(a,b));
            Ceil(-xw,xw,Lo(a,b),Hi(a,b),yk);
            Box(M.ledCool,-xw+.4f,xw-.4f,yk-.012f,yk,sz*(CoreWallZ+.18f)-.02f,sz*(CoreWallZ+.18f)+.02f);
            W.Lampa("corridor light "+(sz<0?"front":"back"),new Vector3(0,yk-.3f,sz*3.8f),.8f,18f);
            // finish: teak skirting broken at every opening, bronze sconces between the room doors, teak relief panels
            var roomGaps=new List<Vector2>();var coreGaps=new List<Vector2>();
            for(int i=0;i<5;i++)
            {
                float ra=i==0?-xw:-TowerHalfW+i*RoomW+.1f;roomGaps.Add(new Vector2(ra+.2f,ra+1.2f));
                float fz=sz*(RoomWallZ-.1f),sc=ra+3.9f;
                Box(M.darkBronze,sc-.09f,sc+.09f,1.72f,2.12f,Lo(fz,fz-sz*.02f),Hi(fz,fz-sz*.02f));
                Box(M.warmLight,sc-.06f,sc+.06f,1.78f,2.06f,Lo(fz-sz*.02f,fz-sz*.06f),Hi(fz-sz*.02f,fz-sz*.06f));
            }
            foreach(float sx in new[]{-1f,1f})coreGaps.Add(new Vector2(sx<0?-12.55f:11.35f,sx<0?-11.35f:12.55f));
            if(sz<0)foreach(float x in LiftX)coreGaps.Add(new Vector2(x-LiftDoorHalf-.12f,x+LiftDoorHalf+.12f));
            else{coreGaps.Add(new Vector2(-7.05f,-6.05f));coreGaps.Add(new Vector2(6.05f,7.05f));}
            Skirting(sz*(RoomWallZ-.1f),-sz,-xw,xw,roomGaps);
            Skirting(sz*(CoreWallZ+.1f),sz,-xw,xw,coreGaps);
            float cf=sz*(CoreWallZ+.1f);
            foreach(float cx in sz<0?new[]{-7.7f,7.7f}:new[]{-9.2f,0f,9.2f})
            {
                Box(M.darkBronze,cx-1.2f,cx+1.2f,1.05f,2.35f,Lo(cf,cf+sz*.03f),Hi(cf,cf+sz*.03f));
                Box(M.teak,cx-1.12f,cx+1.12f,1.13f,2.27f,Lo(cf+sz*.03f,cf+sz*.04f),Hi(cf+sz*.03f,cf+sz*.04f));
                for(float v=1.3f;v<2.2f;v+=.25f)Box(M.teakDark,cx-1.0f,cx+1.0f,v,v+.04f,Lo(cf+sz*.04f,cf+sz*.055f),Hi(cf+sz*.04f,cf+sz*.055f));
            }
        }
        // rooms: 5 front (201-205, west to east) and 5 back (206-210, east to west)
        for(int i=0;i<5;i++)
        {
            float x0=-TowerHalfW+i*RoomW,x1=x0+RoomW;
            float a=i==0?-xw:x0+.1f,b=i==4?xw:x1-.1f;
            Room(t,201+i,a,b,-1f);
            Room(t,210-i,a,b,1f);
        }
        // party walls between rooms (both rows)
        foreach(float sz in new[]{-1f,1f})
            for(int i=1;i<5;i++)WallZ(M.paint,-TowerHalfW+i*RoomW,Lo(sz*RoomWallZ,sz*(TowerHalfD-.3f)),Hi(sz*RoomWallZ,sz*(TowerHalfD-.3f)),0,h,.2f);
        // core band: lift fronts, service rooms (linen west, technical east), stair walls, back core wall
        LiftFront(0,h);
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*4.15f,b=sx*11.2f;
            WallX(M.paint,-CoreWallZ,Lo(a,b),Hi(a,b),0,h,.2f);
            WallX(M.paint,CoreWallZ,Lo(a,b),Hi(a,b),0,h,.2f,new Gap(sx<0?-7.0f:6.1f,sx<0?-6.1f:7.0f,2.1f));
            Leaf(true,CoreWallZ,sx<0?-7.0f:6.1f,sx<0?-6.1f:7.0f,0,1f);
            Box(M.tile,Lo(a,b),Hi(a,b),0,Fin,-2.4f,2.4f);
            Ceil(Lo(a,b),Hi(a,b),-2.4f,2.4f,yc);
            for(float x=Lo(a,b)+.8f;x<Hi(a,b)-.5f;x+=1.4f)Box(M.teakDark,x-.6f,x+.6f,0,2.2f,-2.4f,-1.85f,true);
        }
        CoreBack(0,h);
        StairWalls(0,h,false);
        foreach(float sx in new[]{-1f,1f})
        {
            Ceil(Lo(sx*StairX0,sx*StairLand),Hi(sx*StairX0,sx*StairLand),-2.4f,2.4f,yk);
            Stair(sx,0,h,1);
        }
        return t;
    }

    static void Skirting(float face,float into,float x0,float x1,List<Vector2> gaps)
    {
        gaps.Sort((p,q)=>p.x.CompareTo(q.x));float x=x0;float z0=Lo(face,face+into*.015f),z1=Hi(face,face+into*.015f);
        foreach(var g in gaps){if(g.x>x)Box(M.teakDark,x,g.x,Fin,.11f,z0,z1);x=Mathf.Max(x,g.y);}
        if(x1>x)Box(M.teakDark,x,x1,Fin,.11f,z0,z1);
    }

    // One finished, furnished 5* room: hall + marble bathroom along the corridor, bedroom to the window.
    static void Room(Transform t,int number,float a,float b,float row)
    {
        In(t,true);
        float zc=row*(RoomWallZ+.1f),zf=row*(TowerHalfD-.3f),s=row;
        float Z(float d)=>zc+s*d;
        float yc=GuestStorey-Slab-.01f,h=GuestStorey,zh=Z(3.3f);
        var marker=new GameObject("ROOM "+number+" | 6.80x10.50 | finished, furnished").transform;marker.SetParent(t,false);
        marker.localPosition=new Vector3((a+b)*.5f,0,(zc+zf)*.5f);
        // corridor wall with the entry door (0.90 x 2.10), open leaf into the hall, number plate
        float d0=a+.25f,d1=a+1.15f,wall=row*RoomWallZ;
        WallX(M.paint,wall,a-.1f,b+.1f,0,h,.2f,new Gap(d0,d1,2.1f));
        Leaf(true,wall,d0,d1,0,s);
        float plateZ=wall-s*.1f;
        Box(M.brass,d1+.15f,d1+.45f,1.45f,1.65f,Lo(plateZ,plateZ-s*.012f),Hi(plateZ,plateZ-s*.012f));
        W.Napis(number.ToString(),new Vector3(d1+.3f,1.55f,plateZ-s*.014f),new Vector3(0,0,-s),.10f,new Color(.12f,.10f,.08f));
        Box(M.warmLight,d1+.2f,d1+.4f,2.05f,2.25f,Lo(plateZ,plateZ-s*.03f),Hi(plateZ,plateZ-s*.03f));
        // floors: oak hall and bedroom, marble bathroom
        float bx0=a+1.45f,bx1=a+4.55f;
        Box(M.oak,a,b,0,Fin,Lo(zh,zf),Hi(zh,zf));
        Box(M.oak,a,bx0,0,Fin,Lo(zc,zh),Hi(zc,zh));
        Box(M.oak,bx1,b,0,Fin,Lo(zc,zh),Hi(zc,zh));
        Box(M.marbleWet,bx0,bx1,0,Fin,Lo(zc,zh),Hi(zc,zh));
        // bathroom walls (0.80 door from the hall), marble lining, fittings
        float zm=(zc+zh)*.5f;
        WallZ(M.paint,bx0+.06f,Lo(zc,zh),Hi(zc,zh),0,h,.12f,new Gap(zm-.4f,zm+.4f,2.1f));
        Leaf(false,bx0+.06f,zm-.4f,zm+.4f,0,1f);
        WallZ(M.paint,bx1-.06f,Lo(zc,zh),Hi(zc,zh),0,h,.12f);
        WallX(M.paint,zh,bx0,bx1,0,h,.12f);
        float ix0=bx0+.12f,ix1=bx1-.12f,iz0=Lo(zc,zh-s*.06f),iz1=Hi(zc,zh-s*.06f);
        Box(M.marble,ix0,ix0+.01f,0,yc,iz0,zm-.4f);Box(M.marble,ix0,ix0+.01f,0,yc,zm+.4f,iz1);Box(M.marble,ix0,ix0+.01f,2.1f,yc,zm-.4f,zm+.4f);
        Box(M.marble,ix1-.01f,ix1,0,yc,iz0,iz1);
        Box(M.marble,ix0,ix1,0,yc,Lo(zc,zc+s*.01f),Hi(zc,zc+s*.01f));Box(M.marble,ix0,ix1,0,yc,Lo(zh-s*.06f,zh-s*.07f),Hi(zh-s*.06f,zh-s*.07f));
        Box(M.teakDark,a+1.8f,a+3.2f,.2f,.82f,Lo(Z(.02f),Z(.58f)),Hi(Z(.02f),Z(.58f)),true);                 // vanity
        Box(M.marble,a+1.75f,a+3.25f,.82f,.88f,Lo(Z(.02f),Z(.62f)),Hi(Z(.02f),Z(.62f)),true);
        Box(M.whitePaint,a+2.25f,a+2.75f,.84f,.92f,Lo(Z(.12f),Z(.48f)),Hi(Z(.12f),Z(.48f)));
        Box(M.mirror,a+1.85f,a+3.15f,1.05f,1.95f,Lo(Z(.012f),Z(.03f)),Hi(Z(.012f),Z(.03f)));
        Box(M.whitePaint,a+3.65f,a+4.15f,0,.42f,Lo(Z(.02f),Z(.72f)),Hi(Z(.02f),Z(.72f)),true);              // WC
        Box(M.marbleWet,a+3.1f,ix1,Fin,.05f,Lo(zh-s*.07f,Z(2.15f)),Hi(zh-s*.07f,Z(2.15f)));                 // shower tray
        Box(M.clearGlass,a+3.1f,ix1,.05f,2.05f,Lo(Z(2.13f),Z(2.15f)),Hi(Z(2.13f),Z(2.15f)),true);
        Box(M.steel,ix1-.45f,ix1-.15f,2.15f,2.18f,Lo(Z(2.6f),Z(2.9f)),Hi(Z(2.6f),Z(2.9f)));
        // wardrobe niche (teak wardrobe against the corridor wall) and luggage bench
        Box(M.teak,bx1+.08f,b-.04f,0,2.6f,Lo(Z(.02f),Z(.64f)),Hi(Z(.02f),Z(.64f)),true);
        Box(M.leather,bx1+.25f,b-.25f,.32f,.45f,Lo(Z(2.0f),Z(2.45f)),Hi(Z(2.0f),Z(2.45f)),true);
        // bedroom: teak headboard wall, king bed, nightstands with lamps, bench, rug
        float zb=(zh+zf)*.5f-s*.2f;
        Box(M.teak,a,a+.03f,0,2.8f,zb-2.0f,zb+2.0f);
        Box(M.leather,a+.03f,a+.12f,.35f,1.45f,zb-1.25f,zb+1.25f,true);
        Box(M.teakDark,a+.12f,a+2.25f,0,.32f,zb-1.0f,zb+1.0f,true);
        Box(M.fabric,a+.14f,a+2.23f,.32f,.58f,zb-.98f,zb+.98f,true);
        Box(M.fabric,a+.9f,a+2.26f,.58f,.62f,zb-1.0f,zb+1.0f);
        foreach(float dz in new[]{-.5f,.5f})Box(M.whitePaint,a+.18f,a+.62f,.58f,.72f,zb+dz-.36f,zb+dz+.36f);
        foreach(float dz in new[]{-1.45f,1.45f})
        {
            Box(M.teak,a+.06f,a+.56f,0,.55f,zb+dz-.25f,zb+dz+.25f,true);
            Rod(M.brass,new Vector3(a+.3f,.55f,zb+dz),new Vector3(a+.3f,.95f,zb+dz),.012f);
            Box(M.warmLight,a+.2f,a+.4f,.95f,1.15f,zb+dz-.1f,zb+dz+.1f);
        }
        Box(M.leather,a+2.35f,a+2.8f,0,.45f,zb-.8f,zb+.8f,true);
        Box(M.carpetRoom,a+.6f,a+4.4f,Fin,Fin+.008f,zb-1.9f,zb+1.9f);
        // TV wall: teak media console and panel
        Box(M.teak,b-.45f,b,.15f,.55f,zb-1.0f,zb+1.0f,true);
        Box(M.darkBronze,b-.05f,b,.95f,1.75f,zb-.8f,zb+.8f);
        // window corner: desk with a leather chair (TV side), armchair with a side table (bed side)
        Box(M.teak,b-.62f,b,.72f,.76f,Lo(Z(10.5f-.5f),Z(10.5f-1.9f)),Hi(Z(10.5f-.5f),Z(10.5f-1.9f)),true);
        Box(M.teak,b-.6f,b-.55f,0,.72f,Lo(Z(10.5f-.55f),Z(10.5f-1.85f)),Hi(Z(10.5f-.55f),Z(10.5f-1.85f)));
        Box(M.leather,b-1.25f,b-.8f,.42f,.5f,Lo(Z(10.5f-.95f),Z(10.5f-1.4f)),Hi(Z(10.5f-.95f),Z(10.5f-1.4f)),true);
        Box(M.leather,b-1.29f,b-1.23f,.5f,.95f,Lo(Z(10.5f-.95f),Z(10.5f-1.4f)),Hi(Z(10.5f-.95f),Z(10.5f-1.4f)));
        Box(M.leather,a+.6f,a+1.4f,.2f,.45f,Lo(Z(10.5f-.6f),Z(10.5f-1.4f)),Hi(Z(10.5f-.6f),Z(10.5f-1.4f)),true);
        Box(M.leather,a+.6f,a+1.4f,.45f,.95f,Lo(Z(10.5f-1.3f),Z(10.5f-1.42f)),Hi(Z(10.5f-1.3f),Z(10.5f-1.42f)));
        Prism(Circle(a+1.9f,Z(10.5f-.9f),.25f,12),0,.55f,M.blackMarble,M.brass,false);
        // ceiling and light
        Ceil(a,b,Lo(zc,zf),Hi(zc,zf),yc);
        foreach(float dz in new[]{-1.6f,1.6f})Downlight(a+2.6f,zb+dz,yc);
        Downlight((a+bx0)*.5f,zm,yc);Downlight((bx0+bx1)*.5f,zm,yc);Downlight((bx1+b)*.5f,zm,yc);
        W.Lampa("room "+number+" light",new Vector3((a+b)*.5f,yc-.4f,zb),.85f,8f);
        roomsBuilt+=GuestFloors;           // the template is instanced on every guest floor
    }
    static void Renumber(Transform floor,int f)
    {
        foreach(var tr in floor.GetComponentsInChildren<Transform>(true))
        {
            if(tr.name.StartsWith("ROOM 2",StringComparison.Ordinal)&&int.TryParse(tr.name.Substring(5,3),out int n))
                tr.name="ROOM "+(f*100+n%100)+tr.name.Substring(8);
            var tm=tr.GetComponent<TextMesh>();
            if(tm&&int.TryParse(tm.text,out int t))tm.text=(f*100+t%100).ToString();
        }
    }

    // ================================================================ SIGN (region 3D sign generator)
    static int Sign()
    {
        string path="Assets/Source/Mistrzowie/HotelSobczakSign.bytes";
        if(!File.Exists(path)){BraxBuildQuality.Warn("DO POPRAWKI T03_SIGN_MISSING "+path+" (build_sobczak_sign.py)");return 0;}
        SignFile data;
        using(var f=File.OpenRead(path))using(var zip=new GZipStream(f,CompressionMode.Decompress))using(var r=new StreamReader(zip))
            data=JsonUtility.FromJson<SignFile>(r.ReadToEnd());
        if(data==null||data.signs==null||data.signs.Length==0||data.scaleK<=0f){BraxBuildQuality.Warn("DO POPRAWKI T03_SIGN_EMPTY "+path);return 0;}
        var sign=data.signs[0];int parts=0;
        // one set of mesh assets (vertices already x K, front +Z), used twice
        var meshes=new List<(Mesh mesh,string role)>();
        foreach(var m in sign.meshes)
        {
            int count=m.positions==null?0:m.positions.Length/3;if(count==0||m.triangles==null)continue;
            var v=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                v[i]=new Vector3(m.positions[3*i],m.positions[3*i+1],m.positions[3*i+2]);
                uv[i]=m.uv!=null&&m.uv.Length>=2*count?new Vector2(m.uv[2*i],m.uv[2*i+1]):Vector2.zero;
            }
            var mesh=new Mesh{name="T03 sign "+m.id,indexFormat=count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=m.triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Out+"/Meshes/sign_"+m.id+".asset");
            meshes.Add((mesh,m.role));
        }
        // facade: the solid granite panel of floor 1 above the canopy (back of the sign on the stone, 31 cm deep);
        // reception: a 0.32 copy on the backlit teak wall behind the desk
        foreach(var place in new[]{(name:"facade",pos:new Vector3(0,Level(1)+.9f,-PodiumHalfD),yaw:180f,scale:1f),
                                   (name:"reception wall",pos:new Vector3(11.15f,Ffl0+2.85f,-9.4f),yaw:-90f,scale:.32f)})
        {
            var g=G(signs,"SOBCZAK HOTEL 3D sign | "+place.name+" | solid bevelled glyphs, 5 stars, S monogram");
            g.localPosition=place.pos;g.localRotation=Quaternion.Euler(0,place.yaw,0);
            g.localScale=Vector3.one*(place.scale/data.scaleK);
            foreach(var (mesh,role) in meshes)
            {
                var o=new GameObject("sign "+role);o.transform.SetParent(g,false);
                o.AddComponent<MeshFilter>().sharedMesh=mesh;
                o.AddComponent<MeshRenderer>().sharedMaterial=role=="light"?M.warmLight:role=="dark"?M.darkBronze:M.brass;
                parts++;
            }
        }
        return parts;
    }
    [Serializable] sealed class SignFile{public float scaleK;public SignData[] signs;}
    [Serializable] sealed class SignData{public string id;public BraxChampionsFoundation.MeshData[] meshes;}
}

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
    static int roomsBuilt,doorsBuilt,modelsPlaced,modelsMissing,serial;
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
        roomsBuilt=doorsBuilt=modelsPlaced=modelsMissing=serial=0;floorInstances.Clear();leafMesh=null;roomRects.Clear();diningChairs.Clear();chairProto=null;

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
        int garageBays=Garage();
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
        FinishChairs();
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
        lod.SetLODs(new[]{new LOD(.02f,detailRenderers),new LOD(.0001f,Array.Empty<Renderer>())});lod.RecalculateBounds();
        var vis=podiumInside.gameObject.AddComponent<BraxWidocznoscWnetrza>();vis.nazwa="T03 public floors";
        // lobby and restaurant are seen through the glass: drawn within ~200 m (was 60 m: they popped in on
        // approach, owner videos 9.11 "pokazuje, znika"); at 200 m the podium is a few % of the screen
        vis.strefy=new[]{new Bounds(root.TransformPoint(new Vector3(0,12f,0)),new Vector3(2*PodiumHalfW+400f,120f,2*PodiumHalfD+400f)*K)};
        for(int l=0;l<Elevators;l++)Elevator(l,cabins[l],landingL,landingR);
        GarageLiftConfigure();
        foreach(var d in root.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true))d.zasieg=2.6f*K;
        var streamer=root.gameObject.AddComponent<BraxHotelSobczakFloorStreamer>();
        var instances=new GameObject[floorInstances.Count];for(int i=0;i<instances.Length;i++)instances[i]=floorInstances[i];
        streamer.guestInteriors=instances;streamer.basementInteriors=new GameObject[0];streamer.metreScale=1f;   // root carries K
        streamer.guestBaseY=Level(2);streamer.floorHeight=GuestStorey;streamer.halfWidth=TowerHalfW;streamer.halfDepth=TowerHalfD;
        streamer.neighbourFloors=1;
        foreach(var go in instances)go.SetActive(false);
        // room furniture (models, fixtures, lights) is drawn only while the player or camera is in the room or at its door
        int roomZones=0;
        for(int i=0;i<instances.Length;i++)
        {
            int f=i+2;
            foreach(var r in roomRects)
            {
                var g=instances[i].transform.Find("ROOM "+(f*100+r.n%100)+" | furniture (drawn when inside)");
                if(!g)continue;
                float zMid=(r.zc+r.zf)*.5f-r.s*1.3f,depth=Mathf.Abs(r.zf-r.zc)+2.6f;
                var zone=g.gameObject.AddComponent<BraxWidocznoscWnetrza>();zone.nazwa="T03 room "+(f*100+r.n%100);
                zone.strefy=new[]{new Bounds(root.TransformPoint(new Vector3((r.a+r.b)*.5f,Level(f)+1.7f,zMid)),new Vector3(r.b-r.a+.6f,3.6f,depth)*K)};
                roomZones++;
            }
        }

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
            " doors="+doorsBuilt+" polyHavenModels="+modelsPlaced+" modelsMissing="+modelsMissing+" roomZones="+roomZones+" diningChairs="+diningChairs.Count+" signParts="+signParts+" meshes="+saved+
            " garageBays="+garageBays+" garageLift=B1-PLAC datumY="+datum.ToString("F3")+" entrance=automatic_sliding_doors_with_collision trees=0 visualReview=PENDING");
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
    // a rug, not a painted floor: jacquard field 14 mm thick with a bound edge (leather or jersey binding)
    static void Rug(Material field,Material edge,float x0,float x1,float y,float z0,float z1,float band=.11f)
    {
        Box(field,x0+band,x1-band,y,y+.013f,z0+band,z1-band);
        Box(edge,x0,x1,y,y+.014f,z0,z0+band);Box(edge,x0,x1,y,y+.014f,z1-band,z1);
        Box(edge,x0,x0+band,y,y+.014f,z0+band,z1-band);Box(edge,x1-band,x1,y,y+.014f,z0+band,z1-band);
    }
    static void Face(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0)W.Quad(m,d,c,b,a);else W.Quad(m,a,b,c,d);
    }
    static void Ceil(float x0,float x1,float z0,float z1,float y)
        =>Face(M.ceiling,new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1),Vector3.down);
    static void Downlight(float x,float z,float y)=>Box(M.warmLight,x-.08f,x+.08f,y-.012f,y+.004f,z-.08f,z+.08f);
    static void Rod(Material m,Vector3 a,Vector3 b,float r)=>W.Rura(m,a,b,r,8);
    // Soft filled shape (superquadric) for pillows, mattresses, duvets and cushions: owner 9.11 23:02
    // "poduszki mogą być zaokrąglone, nie muszą być kwadratowe". size = (x, thickness y, z) before the rotation;
    // edge rounds the profile through the thickness (1 = fully round), corner the outline (small = square
    // outline with rounded corners). Drawn as W quads, so the fabric maps keep their real scale.
    static void Soft(Material m,Vector3 c,Vector3 size,Quaternion q,float edge,float corner,int nu=16,int nv=6)
    {
        float C(float w,float e)=>Mathf.Sign(Mathf.Cos(w))*Mathf.Pow(Mathf.Abs(Mathf.Cos(w)),e);
        float S(float w,float e)=>Mathf.Sign(Mathf.Sin(w))*Mathf.Pow(Mathf.Abs(Mathf.Sin(w)),e);
        Vector3 P(int i,int j)
        {
            float u=-Mathf.PI+2f*Mathf.PI*i/nu,v=-Mathf.PI*.5f+Mathf.PI*j/nv,r=Mathf.Max(0f,C(v,edge));
            return c+q*new Vector3(size.x*.5f*r*C(u,corner),size.y*.5f*S(v,edge),size.z*.5f*r*S(u,corner));
        }
        for(int i=0;i<nu;i++)for(int j=0;j<nv;j++)
        {
            Vector3 a=P(i,j),b=P(i+1,j),d=P(i,j+1),e=P(i+1,j+1);
            if((a-e).sqrMagnitude<1e-10f&&(b-d).sqrMagnitude<1e-10f)continue;
            Face(m,a,b,e,d,(a+b+d+e)*.25f-c);
        }
    }
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
    // Owner 10.11 12:54-12:55 (APK 37: open slabs, openings without a leaf): "Wszędzie drzwi globalnie pozamykane".
    // A closed dark smoked-walnut leaf inside the 5 cm casing, polished brass levers on both faces; it opens by itself
    // when the player walks up and closes behind him (BraxChampionsInteriorDoor -> the city's BraxCityDoor).
    static void Leaf(bool alongX,float plane,float a,float b,float y0,float side,float h=2.10f)
    {
        BraxChampionsInteriorDoor.Single("T03_DOOR_"+doorsBuilt,"hotel door",alongX,plane,a+.05f,b-.05f,y0+.01f,h-.06f,M.walnutDark,M.brass);
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
        // the garage ramp trench (east, x RampWall0..RampWall1, z -30..GaragePortalZ) and the garage stair + lift core
        // (west, CoreOuter*) are cut out of the deck: pieces edge to edge round them
        Box(M.paving,1.8f,RampWall0,PlotBaseY,PlazaY,-PlotHalf,-pd,true);
        Box(M.paving,RampWall1,PlotHalf,PlotBaseY,PlazaY,-PlotHalf,-pd,true);
        Box(M.paving,-1.8f,1.8f,PlotBaseY,PlazaY,-PlotHalf+1.2f,-pd,true);
        Box(M.paving,-PlotHalf,PlotHalf,PlotBaseY,PlazaY,pd,PlotHalf,true);
        Box(M.paving,pw,RampWall0,PlotBaseY,PlazaY,-pd,GaragePortalZ,true);
        Box(M.paving,RampWall1,PlotHalf,PlotBaseY,PlazaY,-pd,GaragePortalZ,true);
        Box(M.paving,pw,PlotHalf,PlotBaseY,PlazaY,GaragePortalZ,pd,true);
        Box(M.paving,-PlotHalf,CoreOuterX0,PlotBaseY,PlazaY,-pd,pd,true);
        Box(M.paving,CoreOuterX1,-pw,PlotBaseY,PlazaY,-pd,pd,true);
        Box(M.paving,CoreOuterX0,CoreOuterX1,PlotBaseY,PlazaY,-pd,CoreOuterZ0,true);
        Box(M.paving,CoreOuterX0,CoreOuterX1,PlotBaseY,PlazaY,CoreOuterZ1,pd,true);
        Wedge(M.paving,-1.8f,1.8f,-PlotHalf,DriveY,-PlotHalf+1.2f,PlazaY,PlotBaseY);      // dropped kerb at the crossing
        // granite kerb along the plaza front edge (11 cm above the drive)
        Box(M.kerb,-PlotHalf,-1.8f,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);
        Box(M.kerb,1.8f,RampWall0,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);
        Box(M.kerb,RampWall1,PlotHalf,DriveY-.05f,PlazaY+.005f,-PlotHalf,-PlotHalf+.18f,true);
        // the drop-off lane runs on east to the garage ramp (layout.HOTEL_GARAGE_CUTS cuts the lawn under it)
        Box(M.asphalt,16f,RampWall1,SiteY,DriveY,-34f,-PlotHalf,true);
        Box(M.kerb,16f,RampWall1+.18f,SiteY,DriveY+.12f,-34.18f,-34f,true);
        Box(M.kerb,RampWall1,RampWall1+.18f,SiteY,DriveY+.12f,-34f,-PlotHalf,true);
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
            W.Walec(M.water,new Vector3(sx*5.9f,DriveY+.75f,-38.6f),.16f,.10f,18);               // low bubbler over the column
            W.Walec(M.chrome,new Vector3(sx*5.9f,DriveY+.74f,-38.6f),.05f,.02f,10);
        }
        // owner 10.11 17:38-17:40 (rounded kerbs everywhere): the in / out lanes cross the Z650 sidewalk (local z -50 =
        // world 660, to the lanes at -43.2) as road mouths - city kerb open, R 3 m kerb arcs (layout.HOTEL_DRIVE_MOUTHS)
        {
            var cityWalk=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/paving.mat");
            var cityKerb=BraxChampionsFoundation.SourceMaterial("infrastructure_kerb","granite_tile",.9f,new Color(.78f,.79f,.80f));
            foreach(var (u0,u1,id) in new[]{(-16f,-10f,"in"),(10f,16f,"out")})
                BraxChampionsDriveMouth.Build(site,new BraxChampionsDriveMouth.Spec{id="T03 drop-off "+id+" road mouth",alongX=true,edge=-50f,dir=1f,
                    u0=u0,u1=u1,R=3f,depth=edge+50f,topEnd=DriveY,unit=1f,assetDir=Out+"/Meshes"},M.asphalt,cityWalk?cityWalk:M.paving,cityKerb);
        }
        // zebra crossing (stripes run with the traffic)
        for(float z=-33.55f;z<=-30.4f;z+=.9f)Box(M.whitePaint,-1.7f,1.7f,DriveY,DriveY+.004f,z,z+.45f);
        // raised lawn beds with granite edges on the sides and the back (no trees: owner 9.11)
        Planter(23.2f,29.6f,GaragePortalZ+.6f,27f);                                           // east: beside the ramp trench
        Planter(-29.6f,-23.2f,-26f,CoreOuterZ0-.6f);Planter(-29.6f,-23.2f,CoreOuterZ1+.6f,27f); // west: round the garage pavilion
        Planter(-23.2f,23.2f,21.6f,29.6f);
        foreach(float sx in new[]{-1f,1f})foreach(float x in new[]{10.5f,15f})Planter(sx*x-1.6f,sx*x+1.6f,-23.6f,-22.2f);
        // LED bollards along the plaza edge and drop-off lamps
        In(details,false);
        for(float x=-27f;x<=27.01f;x+=4.5f)
        {
            if(Mathf.Abs(x)<2.5f||(x>RampWall0-.5f&&x<RampWall1+.5f))continue;               // the ramp mouth stays open
            W.Walec(M.darkBronze,new Vector3(x,PlazaY,-PlotHalf+.68f),.09f,.82f,14,true);           // round bronze bollard
            W.Walec(M.frosted,new Vector3(x,PlazaY+.82f,-PlotHalf+.68f),.085f,.12f,14);
            W.Walec(M.darkBronze,new Vector3(x,PlazaY+.94f,-PlotHalf+.68f),.095f,.03f,14);
        }
        foreach(float sx in new[]{-1f,1f})foreach(float x in new[]{4.2f,13f})
        {
            Rod(M.darkBronze,new Vector3(sx*x,PlazaY,-PlotHalf+.45f),new Vector3(sx*x,PlazaY+5.0f,-PlotHalf+.45f),.07f);
            W.Walec(M.darkBronze,new Vector3(sx*x,PlazaY,-PlotHalf+.45f),.12f,.35f,14,true);        // base
            W.Walec(M.frosted,new Vector3(sx*x,PlazaY+4.55f,-PlotHalf+.45f),.16f,.42f,16);          // lantern
            W.Walec(M.darkBronze,new Vector3(sx*x,PlazaY+4.97f,-PlotHalf+.45f),.21f,.05f,16);
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

    // ================================================================ GARAGE B1
    // Owner 10.11 15:25-15:26 ("Hotel Sobczak ... coś tam mówiłeś o parkingu, że zostawisz na później ... do teraz parkingu
    // nie ma", "Nie ma nic na później"): the B1 garage of the 9.11 plan, 60 bays, built now. Under the north part of the plot
    // (z .8..29.2, the tower's core stays on rock): 3 rows of 20 bays 2.5 x 5.0 m (lines inlaid in the asphalt, flush),
    // two 6 m aisles, white walls and columns on a 7.5 m grid with black bases, downstand beams, LED lines and lights. Cars
    // come off the drop-off lane (extended east) into a ramp beside the podium: an open granite-walled trench from the plot
    // edge (8 % / 13 % / 8 %, parapets on the plaza) under the plaza deck into B1 (clear 2.45 m under the beams).
    // People: a glass pavilion on the west side of the plaza (black glass, clear door leaf, "PARKING" in 3D letters) over a
    // straight stair and a two-stop lift (BraxWinda: B1, PLAC). layout.HOTEL_GARAGE_CUTS opens the foundation for the trench,
    // the lane and the core. Hotel-local nominal metres (front = -z).
    public const float GarageFloorY=-3.60f,GarageCeilY=-.70f,GarageX0=-29.2f,GarageX1=29.2f,GarageZ0=.8f,GarageZ1=29.2f,GarageWallT=.30f;
    public const float GaragePortalZ=GarageZ0-GarageWallT,RampWall0=21.9f,RampWall1=29.5f,RampRoad0=22.5f,RampRoad1=28.9f,RampBottomZ=3.0f;
    public const float CoreOuterX0=-29.5f,CoreOuterX1=-23.4f,CoreOuterZ0=-8.3f,CoreOuterZ1=1.1f;
    public const float BayW=2.5f,BayD=5.0f,AisleW=6.0f,BeamDrop=.45f,ColumnStep=7.5f;
    public const int BaysPerRow=20,GarageBays=60;
    const float CoreX0=-29.2f,CoreX1=-23.7f,CoreZ0=-8.0f,GStairX1=-27.6f,StairBottomZ=-.5f,PassX0=-27.4f;
    const float GLiftX=-24.85f,GLiftFrontZ=-3.0f,GLiftZ0=-2.80f,GLiftZ1=-.70f,GLiftHW=.95f;
    static Transform garage,garageCab;static readonly Transform[] gLandL=new Transform[2],gLandR=new Transform[2];
    // ramp profile: flat at the drive, 8 % over 3 m, 13 % to z 0, 8 % to the floor at RampBottomZ
    public static float RampY(float z)
    {
        if(z<=-28.5f)return DriveY;
        if(z<=-25.5f)return DriveY-.08f*(z+28.5f);
        float top=DriveY-.24f,low=GarageFloorY+.24f;
        if(z<=0f)return top-(top-low)*(z+25.5f)/25.5f;
        if(z<=RampBottomZ)return low-.08f*z;
        return GarageFloorY;
    }
    static Material garageWhite;
    static int Garage()
    {
        garage=G(root,"T03 GARAGE B1 / 60 bays, ramp, stair + lift pavilion");
        // clean cool white for walls, columns, beams and ceiling (the hotel's warm stucco read beige under the B1 lights;
        // region palette: no beige): white_stucco_02 neutralised, a cool neutral ambient of its own
        garageWhite=BraxChampionsFoundation.SourceMaterial("t03_garage_white_paint","white_stucco_02",2.0f,new Color(1.10f,1.14f,1.20f));
        garageWhite.SetColor("_SkyColor",new Color(.90f,.92f,.95f));garageWhite.SetColor("_GroundColor",new Color(.82f,.84f,.87f));
        garageWhite.SetFloat("_AmbientIntensity",.72f);
        In(garage,true);
        float f0=GarageFloorY-.30f,f1=GarageFloorY,top=GarageCeilY,beamY=GarageCeilY-BeamDrop;
        // ---- floor: aisles, bay strips with inlaid lines (one surface, edge to edge)
        float aisle1=7.0f,rowB=13.0f,rowC=18.0f,aisle2=23.0f,rowA=2.0f,bayX0=GarageX0,bayX1=bayX0+BaysPerRow*BayW;   // bays x -29.2..20.8
        Box(M.asphalt,GarageX0,RampWall0,f0,f1,GarageZ0,rowA,true);                                // walkway along the south wall
        Box(M.asphalt,GarageX0,GarageX1,f0,f1,aisle1,rowB,true);                                   // aisle 1
        Box(M.asphalt,GarageX0,GarageX1,f0,f1,aisle2,GarageZ1,true);                               // aisle 2
        Box(M.asphalt,RampWall0,GarageX1,f0,f1,RampBottomZ,aisle1,true);                            // ramp landing
        Box(M.asphalt,bayX1,RampWall0,f0,f1,rowA,aisle1,true);                                      // end of row A
        Box(M.asphalt,bayX1,GarageX1,f0,f1,rowB,aisle2,true);                                       // east crossover between the aisles
        int bays=0;
        foreach(var (z0,z1,stopNorth) in new[]{(rowA,aisle1,false),(rowB,rowC,true),(rowC,aisle2,false)})
        {
            for(int i=0;i<BaysPerRow;i++)
            {
                float x0=bayX0+i*BayW,x1=x0+BayW,a=i==0?x0:x0+.05f,b=i==BaysPerRow-1?x1:x1-.05f;
                float zb0=z0,zb1=z1;
                if(z0==rowC){zb0=z0+.05f;}                                                          // the back-to-back line rowB|rowC
                if(z1==rowC){zb1=z1-.05f;}
                Box(M.asphalt,a,b,f0,f1,zb0,zb1,true);
                if(i>0)Box(M.whitePaint,x0-.05f,x0+.05f,f0,f1,zb0,zb1,true);
                // rubber wheel stop .6 m before the back of the bay (cars come in from the aisle)
                float ws=stopNorth?z1-.75f:z0+.60f;
                Box(M.rubber,x0+.45f,x1-.45f,f1,f1+.11f,ws,ws+.15f,true);
                bays++;
            }
        }
        Box(M.whitePaint,bayX0,bayX1,f0,f1,rowC-.05f,rowC+.05f,true);                               // line between rows B and C
        // ---- walls (white), slab, downstand beams, columns
        Box(garageWhite,GarageX0-GarageWallT,GarageX0,f0,top,GaragePortalZ,GarageZ1+GarageWallT,true);              // west
        Box(garageWhite,GarageX1,GarageX1+GarageWallT,f0,top,GaragePortalZ,GarageZ1+GarageWallT,true);            // east
        Box(garageWhite,GarageX0,GarageX1,f0,top,GarageZ1,GarageZ1+GarageWallT,true);                              // north
        // south wall with the stair door (x -29.0..-27.8, 2.10 high)
        Box(garageWhite,GarageX0,-29.0f,f0,top,GaragePortalZ,GarageZ0,true);
        Box(garageWhite,-27.8f,RampWall0,f0,top,GaragePortalZ,GarageZ0,true);
        Box(garageWhite,-29.0f,-27.8f,f1+2.10f,top,GaragePortalZ,GarageZ0,true);
        // steel frame inside the reveal, threshold, the closed door (opens on approach)
        Box(M.darkBronze,-29.0f,-28.92f,f1,f1+2.10f,GaragePortalZ-.05f,GarageZ0+.05f,true);
        Box(M.darkBronze,-27.88f,-27.8f,f1,f1+2.10f,GaragePortalZ-.05f,GarageZ0+.05f,true);
        Box(M.darkBronze,-28.92f,-27.88f,f1+2.02f,f1+2.10f,GaragePortalZ-.05f,GarageZ0+.05f,true);
        Box(M.stoneDark,-29.0f,-27.8f,f0,f1,GaragePortalZ,GarageZ0,true);
        BraxChampionsInteriorDoor.Single("T03_GARAGE_STAIR_DOOR","garage stair door",true,(GaragePortalZ+GarageZ0)*.5f,-28.92f,-27.88f,f1,2.02f,M.steel,M.steel);
        doorsBuilt++;
        // the ramp's west wall runs on into B1 to the bottom of the ramp (beside the last bay of row A)
        Box(garageWhite,RampWall0,RampX0In,f0,top,GaragePortalZ,RampBottomZ,true);
        Box(garageWhite,GarageX0-GarageWallT,GarageX1+GarageWallT,top,-.26f,GaragePortalZ,GarageZ1+GarageWallT,true);   // slab (1 cm under the plot base)
        // (the slab's own underside is the ceiling: no second face 2 mm under it)
        var colX=new List<float>();for(int k=3;k<BaysPerRow;k+=3)colX.Add(bayX0+k*BayW);   // -21.7 -14.2 -6.7 .8 8.3 15.8
        foreach(float x in colX)
        {
            Box(garageWhite,x-.225f,x+.225f,beamY,top-.004f,GarageZ0,GarageZ1,true);                  // downstand beam
            foreach(float z in new[]{aisle1-.4f,rowB+.4f,aisle2-.4f})
            {
                Box(M.stoneDark,x-.235f,x+.235f,f1,f1+.30f,z-.235f,z+.235f,true);                  // black base 1 cm proud
                Box(garageWhite,x-.225f,x+.225f,f1+.30f,beamY,z-.225f,z+.225f,true);
            }
        }
        // ---- LED lines over the aisles and the warm lights
        In(garage,true);
        foreach(float z in new[]{(aisle1+rowB)*.5f,(aisle2+GarageZ1)*.5f})
            for(float x=GarageX0+3.75f;x<GarageX1-1f;x+=ColumnStep)
            {
                if(colX.Exists(c=>Mathf.Abs(c-x)<.6f))continue;
                Box(M.ledCool,x-.75f,x+.75f,top-.05f,top-.006f,z-.06f,z+.06f);
            }
        foreach(float z in new[]{(aisle1+rowB)*.5f,(aisle2+GarageZ1)*.5f})
            foreach(float x in new[]{-18f,0f,18f})W.Lampa("T03 garage light",new Vector3(x,top-.3f,z),.85f,15f,new Color(.96f,.98f,1f));
        W.Lampa("T03 garage ramp light",new Vector3(25.7f,top-.3f,(GarageZ0+aisle1)*.5f),.8f,12f,new Color(.96f,.98f,1f));
        GarageRamp();
        GarageCore();
        return bays;
    }
    const float RampX0In=22.2f,RampX1In=29.2f;
    static void GarageRamp()
    {
        In(site,false);
        const float seg=1f;
        for(float z=-PlotHalf;z<RampBottomZ-.001f;z+=seg)
        {
            float z1=Mathf.Min(z+seg,RampBottomZ),y0=RampY(z),y1=RampY(z1);
            Face(M.asphalt,new Vector3(RampRoad0,y0,z),new Vector3(RampRoad1,y0,z),new Vector3(RampRoad1,y1,z1),new Vector3(RampRoad0,y1,z1),Vector3.up);
            W.Rampa(new Vector3((RampRoad0+RampRoad1)*.5f,y0,z),new Vector3((RampRoad0+RampRoad1)*.5f,y1,z1),RampRoad1-RampRoad0);
            // raised 15 cm kerb walks on both sides (granite), their faces to the road
            foreach(var (a,b,face) in new[]{(RampX0In,RampRoad0,1f),(RampRoad1,RampX1In,-1f)})
            {
                Face(M.kerb,new Vector3(a,y0+.15f,z),new Vector3(b,y0+.15f,z),new Vector3(b,y1+.15f,z1),new Vector3(a,y1+.15f,z1),Vector3.up);
                float xf=face>0?b:a;
                Face(M.kerb,new Vector3(xf,y0,z),new Vector3(xf,y1,z1),new Vector3(xf,y1+.15f,z1),new Vector3(xf,y0+.15f,z),new Vector3(face,0,0));
                W.Rampa(new Vector3((a+b)*.5f,y0+.15f,z),new Vector3((a+b)*.5f,y1+.15f,z1),b-a);
            }
            // retaining walls (granite) from the ramp up to the plaza, inner faces only (the deck is built round them)
            // (up to the plot base: the wall-top box carries the face from there to the plaza - never two faces in one plane)
            if(z<GaragePortalZ&&y1+.15f<PlotBaseY)
                foreach(var (xf,face) in new[]{(RampX0In,1f),(RampX1In,-1f)})
                    Face(M.podiumStone,new Vector3(xf,Mathf.Min(y0+.15f,PlotBaseY),z),new Vector3(xf,y1+.15f,Mathf.Min(z1,GaragePortalZ)),new Vector3(xf,PlotBaseY,Mathf.Min(z1,GaragePortalZ)),new Vector3(xf,PlotBaseY,z),new Vector3(face,0,0));
        }
        // wall tops and granite parapets 1.05 m on the plaza (sides of the trench and over the portal)
        foreach(var (a,b) in new[]{(RampWall0,RampX0In),(RampX1In,RampWall1)})
        {
            Box(M.podiumStone,a,b,PlotBaseY,PlazaY,-PlotHalf,GaragePortalZ,true);
            Box(M.podiumStone,a,b,PlazaY,PlazaY+1.05f,-PlotHalf,GaragePortalZ+.30f,true);
            Box(M.stoneDark,a-.02f,b+.02f,PlazaY+1.05f,PlazaY+1.11f,-PlotHalf,GaragePortalZ+.32f,true);
        }
        Box(M.podiumStone,RampX0In,RampX1In,PlazaY,PlazaY+1.05f,GaragePortalZ,GaragePortalZ+.30f,true);
        Box(M.stoneDark,RampX0In+.02f,RampX1In-.02f,PlazaY+1.05f,PlazaY+1.11f,GaragePortalZ-.02f,GaragePortalZ+.32f,true);
        // portal: the slab edge over the ramp with a cool LED line and "PARKING B1" in 3D letters for the drivers
        Box(M.ledCool,RampX0In+.2f,RampX1In-.2f,GarageCeilY-.012f,GarageCeilY,GaragePortalZ+.05f,GaragePortalZ+.15f);
        var letters=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(letters,M.steel,M.stoneDark,"PARKING  B1",new Vector3((RampX0In+RampX1In)*.5f,-.60f,GaragePortalZ-.008f),180f,.24f,.012f,.14f,6.4f);
        BraxChampionsLetters3D.Build(letters,site,"T03 GARAGE portal letters",1f);
    }
    static void GarageCore()
    {
        In(garage,true);
        float f0=GarageFloorY-.30f,f1=GarageFloorY,deck0=0f,deck1=PlazaY,roofY=3.6f;
        // B1 floor of the core, walls round it (white inside), the wall beside the stair
        Box(M.antiSkid,CoreX0,CoreX1,f0,f1,CoreZ0,GaragePortalZ,true);
        Box(garageWhite,CoreOuterX0,CoreX0,f0,deck1,CoreOuterZ0,GaragePortalZ,true);
        Box(garageWhite,CoreX1,CoreOuterX1,f0,deck1,CoreOuterZ0,GaragePortalZ,true);
        Box(garageWhite,CoreX0,CoreX1,f0,deck1,CoreOuterZ0,CoreZ0,true);
        Box(garageWhite,GStairX1,PassX0,f1,deck1,-6.38f,StairBottomZ,true);                                 // top carries the railing
        Box(garageWhite,GStairX1,PassX0,-.40f,deck1,StairBottomZ,GaragePortalZ,true);                        // beam over the landing-passage link
        // straight stair 22 risers from B1 to the plaza (south up), 1.6 m wide, solid granite steps, steel handrail
        const int n=22;float r=(PlazaY-GarageFloorY)/n;
        for(int i=1;i<n;i++)
        {
            float zA=StairBottomZ-i*Tread,zB=zA+Tread;
            Box(M.stoneDark,CoreX0,GStairX1,f1,f1+i*r,zA,zB,false);
        }
        W.Rampa(new Vector3((CoreX0+GStairX1)*.5f,PlazaY,StairBottomZ-(n-1)*Tread),new Vector3((CoreX0+GStairX1)*.5f,f1,StairBottomZ),GStairX1-CoreX0);
        Rod(M.steel,new Vector3(CoreX0+.08f,f1+.9f,StairBottomZ),new Vector3(CoreX0+.08f,PlazaY+.9f,StairBottomZ-(n-1)*Tread),.022f);
        // ---- pavilion deck (.30) round the stair opening and the lift shaft
        float shaftX0=-26.15f,shaftZ0=-3.1f,shaftZ1=-.45f,landZ=StairBottomZ-(n-1)*Tread;   // -6.38
        // each deck piece: white soffit layer (the B1 ceiling) under the black marble, stacked - never two faces in a plane
        void Deck(float x0,float x1,float z0,float z1)
        {
            Box(garageWhite,x0,x1,deck0,deck0+.06f,z0,z1,true);Box(M.blackMarble,x0,x1,deck0+.06f,deck1,z0,z1,true);
        }
        Deck(CoreX0,GStairX1,CoreZ0,landZ);Deck(PassX0,CoreX1,CoreZ0,shaftZ0);Deck(PassX0,shaftX0,shaftZ0,GaragePortalZ);
        Deck(shaftX0,CoreX1,shaftZ1,GaragePortalZ);
        Box(M.blackMarble,CoreOuterX0,CoreOuterX1,-.26f,deck1,GaragePortalZ,CoreOuterZ1,true);           // over the garage wall
        Deck(GStairX1,PassX0,CoreZ0,landZ);                                                               // over the stair-side wall
        // steel railing round the stair opening (posts + rails, no glass)
        for(float z=landZ+.1f;z<=GaragePortalZ-.05f;z+=1.2f)Rod(M.steel,new Vector3(PassX0-.06f,deck1,z),new Vector3(PassX0-.06f,deck1+1.1f,z),.022f);
        Rod(M.steel,new Vector3(PassX0-.06f,deck1+1.1f,landZ+.1f),new Vector3(PassX0-.06f,deck1+1.1f,GaragePortalZ-.05f),.025f);
        Rod(M.steel,new Vector3(PassX0-.06f,deck1+.55f,landZ+.1f),new Vector3(PassX0-.06f,deck1+.55f,GaragePortalZ-.05f),.016f);
        // and along the north end of the opening (on the strip over the garage wall)
        for(float x=CoreX0+.1f;x<PassX0-.05f;x+=.9f)Rod(M.steel,new Vector3(x,deck1,GaragePortalZ+.06f),new Vector3(x,deck1+1.1f,GaragePortalZ+.06f),.022f);
        Rod(M.steel,new Vector3(CoreX0+.1f,deck1+1.1f,GaragePortalZ+.06f),new Vector3(PassX0-.06f,deck1+1.1f,GaragePortalZ+.06f),.025f);
        // ---- lift shaft (B1 to the pavilion roof) with the front wall and openings at both stops
        // west wall between the front and the back wall (never overlapping them: no two faces in one plane)
        Box(garageWhite,shaftX0,shaftX0+.15f,f0,deck1,GLiftFrontZ+.1f,shaftZ1-.15f,true);Box(M.blackMarble,shaftX0,shaftX0+.15f,deck1,roofY,GLiftFrontZ+.1f,shaftZ1-.15f,true);
        Box(garageWhite,shaftX0,CoreX1,f0,deck1,shaftZ1-.15f,shaftZ1,true);Box(M.blackMarble,shaftX0,CoreX1,deck1,roofY,shaftZ1-.15f,shaftZ1,true);
        Box(M.blackMarble,CoreX1,CoreX1+.15f,deck1,roofY,shaftZ0,shaftZ1,true);                    // shaft east wall in the pavilion
        foreach(float y in new[]{f1,deck1})
        {
            float yTop=y==f1?deck1:roofY;var wallM=y==f1?garageWhite:M.blackMarble;
            Box(wallM,shaftX0,GLiftX-LiftDoorHalf,y,yTop,shaftZ0,GLiftFrontZ+.1f,true);
            Box(wallM,GLiftX+LiftDoorHalf,CoreX1,y,yTop,shaftZ0,GLiftFrontZ+.1f,true);
            Box(wallM,GLiftX-LiftDoorHalf,GLiftX+LiftDoorHalf,y+2.2f,yTop,shaftZ0,GLiftFrontZ+.1f,true);
            Box(M.steel,GLiftX-LiftDoorHalf-.1f,GLiftX-LiftDoorHalf+.01f,y,y+2.35f,shaftZ0-.04f,GLiftFrontZ+.09f);
            Box(M.steel,GLiftX+LiftDoorHalf-.01f,GLiftX+LiftDoorHalf+.1f,y,y+2.35f,shaftZ0-.04f,GLiftFrontZ+.09f);
            Box(M.steel,GLiftX-LiftDoorHalf-.1f,GLiftX+LiftDoorHalf+.1f,y+2.19f,y+2.35f,shaftZ0-.04f,GLiftFrontZ+.09f);
            Box(M.steel,GLiftX-LiftDoorHalf-.34f,GLiftX-LiftDoorHalf-.22f,y+1.0f,y+1.3f,shaftZ0-.03f,shaftZ0);
            Box(M.warmLight,GLiftX-LiftDoorHalf-.305f,GLiftX-LiftDoorHalf-.255f,y+1.12f,y+1.17f,shaftZ0-.04f,shaftZ0-.03f);
        }
        // landing doors (the lift's moving parts) at B1 and on the plaza
        for(int s=0;s<2;s++)
        {
            float y=s==0?f1:deck1;
            gLandL[s]=LeafObject(garage,"Landing door L | garage lift | "+(s==0?"B1":"PLAC"),new Vector3(GLiftX-LiftDoorHalf*.5f,y+1.1f,GLiftFrontZ));
            gLandR[s]=LeafObject(garage,"Landing door R | garage lift | "+(s==0?"B1":"PLAC"),new Vector3(GLiftX+LiftDoorHalf*.5f,y+1.1f,GLiftFrontZ));
        }
        // the moving cabin (built at local floor 0, set to the plaza stop)
        {
            float xc=GLiftX,z0=GLiftZ0,z1=GLiftZ1,hw=GLiftHW;
            garageCab=G(garage,"GARAGE LIFT | cabin");
            In(garageCab,true);
            Box(M.blackMarble,xc-hw,xc+hw,-.12f,0f,z0,z1,true);
            BraxChampionsMirror.Wall("T03 garage lift cabin",new Vector3(xc,1.65f,z1-.05f),Vector3.back,2f*hw-.14f,1.46f,M.steel,5f,6f,384,256);
            Box(M.steel,xc-hw,xc+hw,0f,2.5f,z1,z1+.05f,true);
            foreach(float sx in new[]{-1f,1f})Box(M.steel,sx>0?xc+hw:xc-hw-.05f,sx>0?xc+hw+.05f:xc-hw,0f,2.5f,z0,z1,true);
            Box(M.steel,xc-hw,xc+hw,2.5f,2.58f,z0,z1);
            Box(M.ledCool,xc-.5f,xc+.5f,2.47f,2.5f,z0+.4f,z1-.4f);
            foreach(float sx in new[]{-1f,1f})Box(M.steel,sx>0?xc+LiftDoorHalf:xc-hw,sx>0?xc+hw:xc-LiftDoorHalf,0f,2.5f,z0-.03f,z0,true);
            Box(M.steel,xc-hw,xc+hw,2.2f,2.5f,z0-.03f,z0,true);
            var cabL=G(garageCab,"cabin door L");var cabR=G(garageCab,"cabin door R");
            In(cabL,true);Box(M.steel,xc-LiftDoorHalf,xc,0f,2.2f,z0-.06f,z0-.03f,true);
            In(cabR,true);Box(M.steel,xc,xc+LiftDoorHalf,0f,2.2f,z0-.06f,z0-.03f,true);
            In(garageCab,true);
            W.Lampa("garage lift cabin light",new Vector3(xc,2.2f,(z0+z1)*.5f),.7f,3f);
        }
        // ---- the glass pavilion on the plaza: black frame, black glass, clear door leaf (east), roof with LED, "PARKING"
        In(site,false);
        float px0=CoreOuterX0,px1=CoreOuterX1,pz0=CoreOuterZ0,pz1=CoreOuterZ1,doorA=-6.0f,doorB=-4.2f;
        Box(M.stoneDark,px0,px1,roofY,roofY+.30f,pz0,pz1,true);                                    // roof slab
        // (the roof slab's dark granite underside is the pavilion ceiling)
        for(float x=px0+1.2f;x<px1-.6f;x+=1.6f)Downlight(x,(pz0+pz1)*.5f,roofY-.002f);
        // corner posts and mullions (black), black glass between them, 8 cm frames
        void Mull(float x,float z){Box(M.darkBronze,x-.05f,x+.05f,deck1,roofY,z-.05f,z+.05f,true);}
        foreach(float x in new[]{px0+.05f,px1-.05f})foreach(float z in new[]{pz0+.05f,pz1-.05f})Mull(x,z);
        for(float z=pz0+1.6f;z<pz1-.5f;z+=1.6f){Mull(px0+.05f,z);if(z<doorA-.1f||z>doorB+.1f)Mull(px1-.05f,z);}
        for(float x=px0+1.5f;x<px1-.5f;x+=1.5f){Mull(x,pz0+.05f);Mull(x,pz1-.05f);}
        Box(M.blackGlass,px0+.04f,px0+.06f,deck1,roofY,pz0+.1f,pz1-.1f,true);                       // west
        Box(M.blackGlass,px0+.1f,px1-.1f,deck1,roofY,pz0+.04f,pz0+.06f,true);                       // south
        Box(M.blackGlass,px0+.1f,px1-.1f,deck1,roofY,pz1-.06f,pz1-.04f,true);                       // north
        Box(M.blackGlass,px1-.06f,px1-.04f,deck1,roofY,pz0+.1f,doorA-.15f,true);                    // east, both sides of the door
        Box(M.blackGlass,px1-.06f,px1-.04f,deck1,roofY,doorB+.15f,pz1-.1f,true);
        Box(M.blackGlass,px1-.06f,px1-.04f,deck1+2.30f,roofY,doorA-.15f,doorB+.15f,true);           // over the door
        // black portal round the door, the door itself (clear leaf, closed, opens as you walk up)
        Box(M.darkBronze,px1-.12f,px1+.02f,deck1,deck1+2.30f,doorA-.15f,doorA-.03f,true);
        Box(M.darkBronze,px1-.12f,px1+.02f,deck1,deck1+2.30f,doorB+.03f,doorB+.15f,true);
        Box(M.darkBronze,px1-.12f,px1+.02f,deck1+2.18f,deck1+2.30f,doorA-.03f,doorB+.03f,true);
        BraxChampionsInteriorDoor.Single("T03_GARAGE_PAVILION_DOOR","garage pavilion door",false,px1-.05f,doorA,doorB,deck1,2.18f,M.clearGlass,M.steel);
        doorsBuilt++;
        // "PARKING" over the door on the east fascia (8 mm proud of the roof slab edge), read from the plaza
        var letters=new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(letters,M.steel,M.stoneDark,"PARKING",new Vector3(px1+.008f,roofY+.06f,(pz0+pz1)*.5f),90f,.18f,.012f,.16f,6f);
        BraxChampionsLetters3D.Build(letters,site,"T03 GARAGE pavilion letters",1f);
        // the pavilion floor is the deck above; the plaza around it is built in Site()
    }
    static void GarageLiftConfigure()
    {
        if(!garageCab)return;
        garageCab.localPosition=new Vector3(0,PlazaY,0);                                              // waits on the plaza
        var c=garageCab.gameObject.AddComponent<BraxWinda>();
        c.nazwa="HOTEL SOBCZAK · WINDA GARAŻ";
        c.kabina=garageCab;c.kabinaL=garageCab.Find("cabin door L");c.kabinaP=garageCab.Find("cabin door R");
        float[] stopY={GarageFloorY,PlazaY};
        c.pietraY=new[]{root.TransformPoint(new Vector3(0,stopY[0],0)).y,root.TransformPoint(new Vector3(0,stopY[1],0)).y};
        c.pietraNazwy=new[]{"B1","PLAC"};c.uzytkowe=new[]{true,true};
        c.wezwanie=new[]{root.TransformPoint(new Vector3(GLiftX,stopY[0]+1.2f,GLiftFrontZ-.9f)),root.TransformPoint(new Vector3(GLiftX,stopY[1]+1.2f,GLiftFrontZ-.9f))};
        c.przystanekL=new[]{gLandL[0],gLandL[1]};c.przystanekP=new[]{gLandR[0],gLandR[1]};
        var shaft=root.TransformPoint(new Vector3(GLiftX,0,(GLiftZ0+GLiftZ1)*.5f));
        c.szyb=new Vector2(shaft.x,shaft.z);c.wnetrze=new Vector2(GLiftHW*K,(GLiftZ1-GLiftZ0)*.5f*K);
        c.przesuw=LiftDoorHalf*.95f;c.start=1;
        foreach(var t in garageCab.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        foreach(var t in gLandL)if(t)GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        foreach(var t in gLandR)if(t)GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
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
    static void CoreBack(float y0,float y1,Material face=null)
    {
        WallX(face??M.paint,CoreWallZ,-LiftBank,LiftBank,y0,y1,.2f);
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
        // Owner 10.11 12:48-12:50 (APK 37): "Ma być widać, że są drzwi, a nie jakieś jasne drzwi z takim szarym gównem"
        // and "zakaz szarego koloru przy szybach - albo czarne". Glass vestibule (wiatrolap) 8 x 4.5 m: every fixed pane
        // (side walls, sidelights, transom) is BLACK glass, every frame black; only the two sliding door pairs stay clear
        // glass, set in a black portal with a gold edge - the door reads from the street. Black ceiling, warm LED.
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.blackGlass,sx*4f-.01f,sx*4f+.01f,Ffl0,top,z0,z1,true);
            Box(M.darkBronze,sx*4f-.04f,sx*4f+.04f,top-.06f,top,z0,z1);
        }
        // facade bay above and beside the outer doors (black glass transom, black frame)
        Box(M.blackGlass,-4.5f,4.5f,top,Level(1)-Slab,z0-.01f,z0+.01f,true);
        foreach(float sx in new[]{-1f,1f})Box(M.blackGlass,sx*1.60f,sx*4.5f,Ffl0,top,z0-.01f,z0+.01f,true);
        foreach(float sx in new[]{-1f,1f})Box(M.blackGlass,sx*1.60f,sx*4f,Ffl0,top,z1-.01f,z1+.01f,true);
        Box(M.darkBronze,-4f,4f,top,Level(1)-Slab,z1-.1f,z1+.1f,true);                              // black bulkhead over inner doors
        In(details,false);
        foreach(float z in new[]{z0,z1})
        {
            Box(M.darkBronze,-4.5f,4.5f,top-.08f,top,z-.06f,z+.06f);
            foreach(float x in new[]{-4f,4f})Box(M.darkBronze,x-.04f,x+.04f,Ffl0,top,z-.06f,z+.06f);
            // black door portal: jambs .15 x .24 and a head round the 2.9 m door opening, brass inner edge
            foreach(float sx in new[]{-1f,1f})
            {
                Box(M.darkBronze,sx*1.45f+(sx<0?-.15f:0f),sx*1.45f+(sx<0?0f:.15f),Ffl0,top+.10f,z-.12f,z+.12f);
                Box(M.brass,sx*1.45f+(sx<0?-.006f:0f),sx*1.45f+(sx<0?0f:.006f),Ffl0+.02f,top-.02f,z-.125f,z+.125f);
            }
            Box(M.darkBronze,-1.60f,1.60f,top-.06f,top+.10f,z-.12f,z+.12f);
            Box(M.brass,-1.45f,1.45f,top-.066f,top-.06f,z-.125f,z+.125f);
        }
        In(podiumInside,true);
        Box(M.blackMarble,-3.95f,3.95f,Ffl0,Ffl0+Fin,z0+.1f,z1);                                    // entrance mat zone
        Face(M.darkBronze,new Vector3(-3.95f,top-.01f,z0),new Vector3(3.95f,top-.01f,z0),new Vector3(3.95f,top-.01f,z1),new Vector3(-3.95f,top-.01f,z1),Vector3.down);
        Box(M.ledCool,-3.2f,3.2f,top-.025f,top-.01f,(z0+z1)*.5f-.05f,(z0+z1)*.5f+.05f);
        // two pairs of automatic sliding doors (BraxDrzwiAutomatyczne): closed they block, they open as you walk up
        // leaves run 10 cm in front of the fixed glass (never through it)
        AutoDoors("T03 MAIN ENTRANCE | outer automatic doors",z0-.10f);
        AutoDoors("T03 MAIN ENTRANCE | inner automatic doors",z1-.10f);
        // canopy 15 x 6.6 m with LED soffit lines, black fascia, tie rods to the podium
        In(podium,false);
        float c0=-PodiumHalfD-6.6f,cy=4.6f;
        Box(M.darkBronze,-7.5f,7.5f,cy,cy+.5f,c0,-PodiumHalfD-.25f,true);
        Face(M.darkBronze,new Vector3(-7.4f,cy-.005f,c0+.1f),new Vector3(7.4f,cy-.005f,c0+.1f),new Vector3(7.4f,cy-.005f,-PodiumHalfD-.25f),new Vector3(-7.4f,cy-.005f,-PodiumHalfD-.25f),Vector3.down);
        In(details,false);
        foreach(float z in new[]{-PodiumHalfD-1.6f,-PodiumHalfD-3.4f,-PodiumHalfD-5.2f})Box(M.ledCool,-6.8f,6.8f,cy-.02f,cy-.005f,z-.05f,z+.05f);
        for(float x=-6f;x<=6.01f;x+=2f)foreach(float z in new[]{-PodiumHalfD-2.5f,-PodiumHalfD-4.3f})Downlight(x,z,cy-.005f);
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
        Box(M.darkBronze,at.x-.74f,at.x+.74f,at.y,at.y+.12f,at.z-.03f,at.z+.03f);                // black bottom rail
        Box(M.darkBronze,at.x-.74f,at.x+.74f,at.y+3.04f,at.y+3.12f,at.z-.03f,at.z+.03f);
        Box(M.darkBronze,at.x+(x<0?-.74f:.68f),at.x+(x<0?-.68f:.74f),at.y,at.y+3.12f,at.z-.03f,at.z+.03f);
        Box(M.darkBronze,at.x+(x<0?.68f:-.74f),at.x+(x<0?.74f:-.68f),at.y,at.y+3.12f,at.z-.03f,at.z+.03f);   // meeting stile
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
                // glass and frames end inside the 50 cm corner piers: their ends lay in the plane of the side facades
                // (x = +-17) and flickered on every tower corner from afar (owner 13:05 "zero migotania")
                Box(M.glass,-TowerHalfW+.45f,TowerHalfW-.45f,b0+.6f,b1-Slab-.35f,sz*(TowerHalfD-.16f),sz*(TowerHalfD-.14f),true);
                In(details,false);
                Box(M.darkBronze,-TowerHalfW+.45f,TowerHalfW-.45f,b0+.6f,b0+.66f,sz*(TowerHalfD-.2f),sz*(TowerHalfD-.1f));
                Box(M.darkBronze,-TowerHalfW+.45f,TowerHalfW-.45f,b1-Slab-.41f,b1-Slab-.35f,sz*(TowerHalfD-.2f),sz*(TowerHalfD-.1f));
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
    // Pleated linen drapes parked at both sides of every room's window pair (always on: rooms read lived-in from outside).
    static void Curtains(bool alongX,float sz,float b0)
    {
        float y0=b0+.04f,y1=b0+GuestStorey-Slab-.08f,z=sz*(TowerHalfD-.48f);
        for(int i=0;i<5;i++)
        {
            float x=-TowerHalfW+i*RoomW;
            Drape(x+.45f,x+1.15f,y0,y1,z,sz);
            Drape(x+RoomW-1.15f,x+RoomW-.45f,y0,y1,z,sz);
        }
    }
    // Soft folds (owner 10.11 12:56 "firanki z prawdziwej tekstury"): white linen weave, 5 rounded folds drawn as
    // 30 narrow double-sided panels on a sine (was 6 flat zig-zag planks).
    static void Drape(float x0,float x1,float y0,float y1,float z,float sz)
    {
        const int seg=30;const float folds=5f,d=.055f;
        for(int k=0;k<seg;k++)
        {
            float t0=k/(float)seg,t1=(k+1)/(float)seg,u0=Mathf.Lerp(x0,x1,t0),u1=Mathf.Lerp(x0,x1,t1);
            float za=z+d*Mathf.Sin(t0*folds*2f*Mathf.PI),zb=z+d*Mathf.Sin(t1*folds*2f*Mathf.PI);
            var p0=new Vector3(u0,y0,za);var p1=new Vector3(u1,y0,zb);var p2=new Vector3(u1,y1,zb);var p3=new Vector3(u0,y1,za);
            W.Quad(M.linen,p0,p1,p2,p3);W.Quad(M.linen,p3,p2,p1,p0);
        }
        Box(M.darkBronze,x0-.02f,x1+.02f,y1,y1+.03f,z-.03f,z+.03f);
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
            Leaf(true,-2.7f,sx<0?-12.5f:11.4f,sx<0?-11.4f:12.5f,y,1f,2.3f);                          // stair door to the roof
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
        // terrace (owner 9.11 23:01: an empty slab): teak decks, six cafe sets under parasols, a marble bar by the
        // lift lobby, loungers with soft cushions, planters and potted plants along the parapet
        Box(M.teakDark,-15.2f,15.2f,y+.02f,y+.045f,-15.0f,-9.4f);
        Box(M.teakDark,-15.2f,15.2f,y+.02f,y+.045f,9.0f,15.0f);
        foreach(float x in new[]{-13.5f,-11.2f,11.2f,13.5f})Lounger(roof,x,y+.045f,-7.6f);
        foreach(float z in new[]{-12.6f,12.0f})
            foreach(float x in z<0?new[]{-9.6f,-3.2f,3.2f,9.6f}:new[]{-6.4f,6.4f})
            {
                Model(H+"outdoor_table_chair_set_01/outdoor_table_chair_set_01_1k.gltf",roof,"roof terrace cafe set",new Vector3(x,y+.045f,z),90f,true);
                Rod(M.darkBronze,new Vector3(x,y+.045f,z),new Vector3(x,y+2.55f,z),.025f);
                W.Walec(M.linen,new Vector3(x,y+2.45f,z),1.45f,.05f,8);                            // parasol canopy
                W.Walec(M.darkBronze,new Vector3(x,y+.045f,z),.22f,.06f,12,true);
            }
        // bar against the technical box, east of the lift lobby
        float bz=TechZ0-.75f;
        Fluted(true,bz,-1f,3.2f,8.8f,y+.10f,y+1.05f,M.walnut);
        Box(M.walnutDark,3.2f,8.8f,y+.02f,y+1.05f,bz,bz+.55f,true);
        Box(M.marble,3.1f,8.9f,y+1.05f,y+1.11f,bz-.18f,bz+.60f,true);
        for(float x=4.0f;x<=8.01f;x+=1.0f)
            if(!Model(H+"bar_chair_round_01/bar_chair_round_01_1k.gltf",roof,"roof bar stool, leather",new Vector3(x,y+.02f,bz-.62f),0f,true))Stool(x,y,bz-.62f);
        foreach(float x in new[]{-12f,12f})RoofPlanter(x,10f,2.5f,1.0f,y+.02f);
        foreach(float x in new[]{-11f,11f})RoofPlanter(x,-15.3f,2.2f,.45f,y+.02f);
        foreach(var pt in new[]{new Vector2(-15.2f,-15.2f),new Vector2(15.2f,-15.2f),new Vector2(-15.2f,-9.2f),new Vector2(15.2f,-9.2f),
                                 new Vector2(-15.2f,15.2f),new Vector2(15.2f,15.2f),new Vector2(0f,15.2f),new Vector2(-10.8f,-6.4f)})
            Model(A+"potted_plant_01/potted_plant_01_1k.gltf",roof,"roof terrace plant",new Vector3(pt.x,y+.02f,pt.y),pt.x*17f+pt.y*5f,false);
    }
    static void Lounger(Transform g,float x,float y,float z)
    {
        Box(M.teakDark,x-.33f,x+.33f,y+.18f,y+.24f,z-1.0f,z+.95f,true);                               // slatted frame
        foreach(float dz in new[]{-.85f,.8f})foreach(float dx in new[]{-.28f,.28f})Box(M.teakDark,x+dx-.03f,x+dx+.03f,y,y+.18f,z+dz-.03f,z+dz+.03f);
        Soft(M.leatherWhite,new Vector3(x,y+.31f,z-.25f),new Vector3(.64f,.14f,1.45f),Quaternion.identity,.6f,.2f);
        Soft(M.leatherWhite,new Vector3(x,y+.55f,z+.70f),new Vector3(.64f,.13f,.80f),Quaternion.Euler(-48f,0,0),.6f,.2f);
        Soft(M.linen,new Vector3(x,y+.42f,z+.42f),new Vector3(.40f,.11f,.26f),Quaternion.Euler(-30f,0,0),.9f,.4f);   // head cushion
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
    static void LiftFront(float y0,float y1,Material face=null)
    {
        float z=-CoreWallZ,t=.2f;var wallM=face??M.paint;
        var gaps=new List<Gap>();foreach(float x in LiftX)gaps.Add(new Gap(x-LiftDoorHalf,x+LiftDoorHalf,2.2f));
        float x0=-LiftBank,cursor=x0;
        foreach(var g in gaps)
        {
            Box(wallM,cursor,g.a,y0,y1,z-t*.5f,z+t*.5f,true);Box(wallM,g.a,g.b,y0+g.top,y1,z-t*.5f,z+t*.5f,true);cursor=g.b;
        }
        Box(wallM,cursor,LiftBank,y0,y1,z-t*.5f,z+t*.5f,true);
        // corridor face: teak cladding, steel portals, call buttons
        Box(M.teak,-LiftBank,LiftBank,y0+2.2f+.15f,y0+2.9f,z-t*.5f-.02f,z-t*.5f);
        foreach(float x in LiftX)
        {
            // steel portal 1 cm proud into the opening and 1 cm short of the shaft face: its faces never lie in the wall's
            // reveal or back planes (they flickered in every lift reveal - owner 13:05 "zero migotania")
            Box(M.steel,x-LiftDoorHalf-.1f,x-LiftDoorHalf+.01f,y0,y0+2.35f,z-t*.5f-.04f,z+t*.5f-.01f);
            Box(M.steel,x+LiftDoorHalf-.01f,x+LiftDoorHalf+.1f,y0,y0+2.35f,z-t*.5f-.04f,z+t*.5f-.01f);
            Box(M.steel,x-LiftDoorHalf-.1f,x+LiftDoorHalf+.1f,y0+2.19f,y0+2.35f,z-t*.5f-.04f,z+t*.5f-.01f);
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
            BraxChampionsMirror.Wall("T03 lift "+(l+1)+" cabin",new Vector3(xc,1.65f,z1-.05f),Vector3.back,2f*hw-.14f,1.46f,M.steel,5f,6f,384,256);   // owner 12:33: real mirror
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
        float y0=Ffl0,yc=CeilingOf(0)-.01f,top=Level(1),ys=yc-.55f;
        float zf=-PodiumHalfD+.3f,zb=PodiumHalfD-.3f,xw=PodiumHalfW-.3f;
        // floors: cream marble lobby with a black marble border and brass inlay, wings marble, back zones
        Box(M.marble,-11.2f,11.2f,y0,y0+Fin,-14.5f,-CoreWallZ-.1f);
        Box(M.blackMarble,-11.2f,11.2f,y0+Fin,y0+Fin+.002f,-14.5f,-14.1f);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*11.4f,sx*xw,y0,y0+Fin,zf,-CoreWallZ-.1f);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*4f,sx*11.2f,y0,y0+Fin,zf,-14.5f);
        Box(M.carpet,-xw,xw,y0,y0+Fin,CoreWallZ+.1f,RoomWallZ-.1f);                                   // back corridor
        Box(M.oak,-11.2f,11.2f,y0,y0+Fin,RoomWallZ+.1f,zb);                                            // waiting lounge
        foreach(float sx in new[]{-1f,1f})Box(M.tile,sx*11.4f,sx*xw,y0,y0+Fin,RoomWallZ+.1f,zb);
        foreach(float sx in new[]{-1f,1f})Box(M.marble,sx*(StairX1+.2f),sx*xw,y0,y0+Fin,-2.4f,2.4f);     // side passage wing -> back
        // ceiling 5.70 m with a 1.0 m perimeter soffit, LED cove and downlights; brass ring chandelier over the lounge
        CeilAround(xw,zb,yc);
        Soffit(-11.2f,11.2f,-14.5f,-CoreWallZ-.1f,ys,yc,1.0f);
        for(float x=-8.5f;x<=8.51f;x+=3.4f)for(float z=-12.2f;z<=-4.5f;z+=2.6f)Downlight(x,z,yc);
        Ring(Circle(-6.5f,-9f,2.4f,32),ys-1.1f,ys-.98f,.08f,M.brass,false);
        Ring(Circle(-6.5f,-9f,2.25f,32),ys-1.12f,ys-1.09f,.06f,M.warmLight,false);
        foreach(float ang in new[]{0f,120f,240f})
        {
            float rad=ang*Mathf.Deg2Rad;Rod(M.brass,new Vector3(-6.5f+Mathf.Cos(rad)*2.4f,ys-.98f,-9f+Mathf.Sin(rad)*2.4f),new Vector3(-6.5f,yc,-9f),.012f);
        }
        // walls: wings (wide openings), staff door behind reception; lobby faces clad in large marble panels
        WallZ(M.paint,-11.3f,zf,-CoreWallZ,y0,top,.2f,new Gap(-13.5f,-6.5f,4.2f));                    // bar opening 7 m
        WallZ(M.paint,11.3f,zf,-CoreWallZ,y0,top,.2f,new Gap(-17.8f,-15.2f,2.8f),new Gap(-4.9f,-4.0f,2.1f));   // staff door behind reception
        Leaf(false,11.3f,-4.9f,-4.0f,y0,1f);
        Clad(false,-11.2f,1f,zf,-2.6f,y0,yc,1.2f,1.5f,M.marble,new Gap(-13.5f,-6.5f,4.2f));
        Clad(false,11.2f,-1f,zf,-13.8f,y0,yc,1.2f,1.5f,M.marble,new Gap(-17.8f,-15.2f,2.8f));
        Clad(false,11.2f,-1f,-5.0f,-2.6f,y0,yc,1.2f,1.5f,M.marble,new Gap(-4.9f,-4.0f,2.1f));
        // reception: fluted walnut counter, marble top, brass kick, LED; walnut back desk with monitors; backlit teak wall + 3D sign
        Fluted(false,6.2f,-1f,-12.6f,-6.2f,y0+.08f,y0+1.04f,M.walnut);
        Box(M.walnutDark,6.2f,7.0f,y0,y0+1.04f,-12.6f,-6.2f,true);
        Box(M.brass,6.17f,6.25f,y0,y0+.08f,-12.62f,-6.18f);
        Box(M.marble,6.05f,7.1f,y0+1.04f,y0+1.10f,-12.7f,-6.1f,true);
        Box(M.ledCool,6.18f,6.2f,y0+.96f,y0+.99f,-12.5f,-6.3f);
        Box(M.walnut,7.0f,7.7f,y0,y0+.74f,-12.4f,-6.4f,true);Box(M.marble,6.98f,7.72f,y0+.74f,y0+.77f,-12.42f,-6.38f);
        foreach(float z in new[]{-10.6f,-8.0f})
        {
            Box(M.screen,7.25f,7.28f,y0+.95f,y0+1.27f,z-.27f,z+.27f);Box(M.darkBronze,7.28f,7.32f,y0+.77f,y0+1.0f,z-.04f,z+.04f);
        }
        Box(M.teak,11.15f,11.2f,y0,yc,-13.8f,-5f);
        Box(M.ledCool,11.12f,11.15f,y0+2.4f,y0+2.43f,-13.6f,-5.2f);
        Box(M.ledCool,11.12f,11.15f,y0+4.6f,y0+4.63f,-13.6f,-5.2f);
        Model(H+"ceramic_vase_02/ceramic_vase_02_1k.gltf",podiumInside,"reception vase",new Vector3(6.6f,y0+1.10f,-12.2f),0f,false);
        Model(H+"potted_plant_04/potted_plant_04_1k.gltf",podiumInside,"reception succulent",new Vector3(6.6f,y0+1.10f,-6.6f),0f,false);
        W.Lampa("reception light",new Vector3(8.5f,ys-1f,-9.4f),1.0f,9f);
        // lobby lounge: leather sofas and armchairs, marble/metal coffee table, side tables with vases, jacquard rug, plants, art
        Rug(M.rugLobby,M.cognac,-10f,-3f,y0+Fin,-12.3f,-5.7f);
        LobbySeat(A+"sofa_02/sofa_02_2k.gltf","lobby leather sofa",-6.5f,y0,-11.6f,0f,2.2f);
        LobbySeat(A+"sofa_02/sofa_02_2k.gltf","lobby leather sofa",-6.5f,y0,-6.4f,180f,2.2f);
        LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","lobby leather armchair",-9.4f,y0,-9f,90f,.85f);
        LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","lobby leather armchair",-3.6f,y0,-9f,-90f,.85f);
        Model(H+"coffee_table_round_01/coffee_table_round_01_2k.gltf",podiumInside,"lobby coffee table, marble",new Vector3(-6.5f,y0+Fin,-9f),0f,true);
        foreach(float x in new[]{-8.4f,-4.6f})
        {
            Model(H+"side_table_01/side_table_01_1k.gltf",podiumInside,"lobby side table",new Vector3(x,y0+Fin,-12.0f),0f,true);
            Model(H+"ceramic_vase_01/ceramic_vase_01_1k.gltf",podiumInside,"lobby vase",new Vector3(x,y0+.56f,-12.0f),0f,false);
        }
        foreach(var pt in new[]{new Vector2(-10.4f,-13.9f),new Vector2(2.9f,-13.9f)})
            Model(A+"potted_plant_02/potted_plant_02_1k.gltf",podiumInside,"lobby plant",new Vector3(pt.x,y0+Fin,pt.y),pt.x*13f,false);
        Model(H+"hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",podiumInside,"lobby art west",new Vector3(-11.16f,y0+2.4f,-16.2f),90f,false,Snap.Centre);
        Model(H+"hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",podiumInside,"lobby art west 2",new Vector3(-11.16f,y0+2.4f,-4.6f),90f,false,Snap.Centre);
        W.Lampa("lobby light",new Vector3(0,ys-1.2f,-9f),1.1f,13f);
        // centre of the lobby (was an empty floor): round black marble table on a brass drum with a tall plant,
        // black marble floor medallion ring; two more leather lounges by the front windows
        W.Walec(M.brass,new Vector3(0,y0+Fin,-9.2f),.42f,.70f,24,true);
        W.Walec(M.blackMarble,new Vector3(0,y0+Fin+.70f,-9.2f),.85f,.05f,32);
        Model(A+"potted_plant_01/potted_plant_01_1k.gltf",podiumInside,"lobby centre plant",new Vector3(0,y0+Fin+.75f,-9.2f),20f,false);
        Ring(Circle(0,-9.2f,1.9f,40),y0+Fin,y0+Fin+.003f,.14f,M.blackMarble,false);
        foreach(float sx in new[]{-1f,1f})
        {
            float gx=sx*7.6f,gz=-16.9f;
            Rug(M.rugRoom,M.cognac,gx-2.0f,gx+2.0f,y0+Fin,gz-1.5f,gz+1.5f);
            LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","window lounge armchair",gx-1.15f,y0,gz,90f,.85f);
            LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","window lounge armchair",gx+1.15f,y0,gz,-90f,.85f);
            Model(H+"coffee_table_round_01/coffee_table_round_01_2k.gltf",podiumInside,"window lounge table",new Vector3(gx,y0+Fin+.014f,gz),0f,true);
            Model(A+"potted_plant_02/potted_plant_02_1k.gltf",podiumInside,"window lounge plant",new Vector3(sx*10.4f,y0+Fin,-18.0f),sx*40f,false);
        }
        // lobby bar (west wing): fluted walnut front, marble top, brass foot rail, mirrored backlit back bar, leather stools
        Fluted(false,-14.8f,1f,-16.5f,-6f,y0+.08f,y0+1.05f,M.walnut);
        Box(M.walnutDark,-15.6f,-14.8f,y0,y0+1.05f,-16.5f,-6f,true);
        Box(M.brass,-14.85f,-14.77f,y0,y0+.08f,-16.52f,-5.98f);
        Box(M.marble,-15.75f,-14.6f,y0+1.05f,y0+1.11f,-16.6f,-5.9f,true);
        Rod(M.brass,new Vector3(-14.55f,y0+.22f,-16.4f),new Vector3(-14.55f,y0+.22f,-6.1f),.025f);
        Box(M.walnut,-xw,-xw+.45f,y0,y0+.95f,-16.5f,-6f,true);
        BraxChampionsMirror.Wall("T03 lobby bar back",new Vector3(-xw+.02f,y0+1.925f,-11.25f),Vector3.right,10.26f,1.92f,M.brass,20f,30f,1024,192);
        for(float yy=y0+1.25f;yy<y0+2.9f;yy+=.55f){Box(M.walnut,-xw,-xw+.32f,yy,yy+.04f,-16.4f,-6.1f);Box(M.ledCool,-xw+.29f,-xw+.32f,yy-.02f,yy,-16.3f,-6.2f);}
        Box(M.walnut,-xw,-xw+.45f,y0+2.9f,y0+3.1f,-16.5f,-6f);
        for(float z=-15.4f;z<=-6.9f;z+=1.4f)
            if(!Model(H+"bar_chair_round_01/bar_chair_round_01_1k.gltf",podiumInside,"lobby bar stool, leather",new Vector3(-14.0f,y0+Fin,z),-90f,true))Stool(-14.0f,y0,z);
        LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","bar leather armchair",-13.0f,y0,-17.9f,0f,.85f);
        LobbySeat(A+"modern_arm_chair_01/modern_arm_chair_01_2k.gltf","bar leather armchair",-18.0f,y0,-17.9f,0f,.85f);
        Model(H+"side_table_01/side_table_01_1k.gltf",podiumInside,"bar side table",new Vector3(-15.5f,y0+Fin,-17.9f),0f,true);
        W.Lampa("bar light",new Vector3(-16f,ys-1f,-11f),.9f,9f);
        // east wing: fluted concierge desk, brass luggage trolley with suitcases, luggage room, wall clock
        Fluted(false,15.5f,-1f,-17.6f,-15.4f,y0+.08f,y0+1.04f,M.walnut);
        Box(M.walnutDark,15.5f,16.3f,y0,y0+1.04f,-17.66f,-15.4f,true);Box(M.marble,15.35f,16.45f,y0+1.04f,y0+1.10f,-17.75f,-15.3f,true);
        LuggageTrolley(13.2f,y0,-16.4f);
        WallX(M.paint,-10f,14.0f,xw,y0,top,.2f,new Gap(16.2f,17.2f,2.1f));       // luggage room (bagazownia)
        WallZ(M.paint,13.9f,-10f,-CoreWallZ,y0,top,.2f);
        Leaf(true,-10f,16.2f,17.2f,y0,1f);
        // Owner 10.11 12:54 (photo: orange solid wood blocks with suitcases on top): open luggage racks - black steel
        // posts and rails, three dark-walnut shelves each, the suitcases ON the shelves.
        for(float z=-8.8f;z<=-4.2f;z+=1.5f)
        {
            float rx0=18.6f,rx1=xw-.06f;
            foreach(float px in new[]{rx0,rx1-.04f})foreach(float pz in new[]{z-.30f,z+.26f})Box(M.darkBronze,px,px+.04f,y0,y0+2.1f,pz,pz+.04f,true);
            foreach(float sy in new[]{.16f,.86f,1.56f})
            {
                Box(M.walnutDark,rx0,rx1,y0+sy,y0+sy+.03f,z-.30f,z+.30f,true);
                Box(M.darkBronze,rx0,rx1,y0+sy-.04f,y0+sy,z-.31f,z-.29f);Box(M.darkBronze,rx0,rx1,y0+sy-.04f,y0+sy,z+.29f,z+.31f);
            }
            foreach(float sy in new[]{.19f,.89f})
                Model(H+"vintage_suitcase/vintage_suitcase_1k.gltf",podiumInside,"stored suitcases (pair)",new Vector3((rx0+rx1)*.5f,y0+sy,z),0f,false);
        }
        Model(H+"wall_clock/wall_clock_1k.gltf",podiumInside,"concierge wall clock",new Vector3(13.8f,y0+2.6f,-10.12f),180f,false,Snap.Centre);
        W.Lampa("concierge light",new Vector3(16f,ys-1f,-14f),.8f,8f);
        // core band: guest toilets (men west, women east) opening to the lobby
        foreach(float sx in new[]{-1f,1f})Toilets(sx,y0,top,yc);
        LiftFront(y0,top,M.marble);
        Box(M.marble,-LiftBank,LiftBank,y0+2.9f,yc,-CoreWallZ-.12f,-CoreWallZ-.1f);          // marble-clad lift wall up to the ceiling
        foreach(float sx in new[]{-1f,1f})                                                   // toilet fronts clad like the lift wall
            Clad(true,-CoreWallZ-.1f,-1f,Lo(sx*4.35f,sx*11.2f),Hi(sx*4.35f,sx*11.2f),y0,yc,1.2f,1.5f,M.marble,new Gap(Lo(sx*8.7f,sx*9.6f),Hi(sx*8.7f,sx*9.6f),2.1f));
        CoreBack(y0,top);
        // back: corridor (grasscloth), waiting lounge (poczekalnia), back office, staff room with service exit
        WallX(M.grasscloth,RoomWallZ,-xw,xw,y0,top,.2f,new Gap(-9f,9f,3.6f),new Gap(-16.6f,-15.6f,2.1f),new Gap(15.6f,16.6f,2.1f));
        Leaf(true,RoomWallZ,-16.6f,-15.6f,y0,1f);Leaf(true,RoomWallZ,15.6f,16.6f,y0,1f);
        foreach(float sx in new[]{-1f,1f})WallZ(M.grasscloth,sx*11.3f,RoomWallZ+.1f,zb,y0,top,.2f);
        Rug(M.rugRoom,M.cognac,-8f,8f,y0+Fin,8.5f,15.5f);
        LobbySeat(A+"sofa_02/sofa_02_2k.gltf","waiting leather sofa",-4f,y0,15.6f,180f,2.2f);
        LobbySeat(A+"sofa_02/sofa_02_2k.gltf","waiting leather sofa",4f,y0,15.6f,180f,2.2f);
        foreach(float sx in new[]{-1f,1f})
        {
            if(!Model(H+"mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf",podiumInside,"waiting lounge chair, leather",new Vector3(sx*7.6f,y0+Fin,11f),-90f*sx,true))
                ProceduralSofa(sx*7.6f,y0,11f,-90f*sx,.85f);
            Model(H+"modern_coffee_table_01/modern_coffee_table_01_2k.gltf",podiumInside,"waiting coffee table",new Vector3(sx*4f,y0+Fin,14.0f),0f,true);
            Model(H+"ceramic_vase_02/ceramic_vase_02_1k.gltf",podiumInside,"waiting vase",new Vector3(sx*4.3f,y0+.40f,14.0f),0f,false);
            Model(A+"potted_plant_02/potted_plant_02_1k.gltf",podiumInside,"waiting plant",new Vector3(sx*10.4f,y0+Fin,17.6f),0f,false);
            Model(H+"fancy_picture_frame_01/fancy_picture_frame_01_1k.gltf",podiumInside,"waiting lounge art",new Vector3(sx*11.18f,y0+2.2f,12f),-90f*sx,false,Snap.Centre);
        }
        for(float x=-8f;x<=8.01f;x+=4f)Downlight(x,12f,yc);
        W.Lampa("waiting lounge light",new Vector3(0,yc-1f,12f),.9f,11f);
        foreach(float z in new[]{8f,11.5f,15f})                                                                  // back office desks
        {
            Box(M.walnut,-19.6f,-16.4f,y0+.72f,y0+.76f,z-.6f,z+.6f,true);
            Box(M.walnutDark,-19.55f,-16.45f,y0,y0+.72f,z+.5f,z+.56f);
            Box(M.screen,-19.3f,-19.27f,y0+.9f,y0+1.22f,z-.28f,z+.28f);
            if(!Model(H+"dining_chair_02/dining_chair_02_2k.gltf",podiumInside,"back office chair",new Vector3(-18f,y0+Fin,z-.9f),0f,true))
                Box(M.leather,-18.3f,-17.7f,y0+.42f,y0+.5f,z-1.3f,z-.7f,true);
        }
        for(float z=6.5f;z<=17.5f;z+=1.6f)Box(M.walnut,-xw,-xw+.6f,y0,y0+2.2f,z-.35f,z+.35f,true);                // archive cabinets
        for(float z=6.5f;z<=12f;z+=1.1f)Box(M.walnut,xw-.6f,xw,y0,y0+2.0f,z-.5f,z+.5f,true);                     // staff lockers
        Box(M.walnut,14f,17f,y0+.72f,y0+.76f,14f,16f,true);
        W.Lampa("back of house light",new Vector3(16f,yc-1f,12f),.7f,9f);
        Stair(-1f,Level(0),Level(1),2);Stair(1f,Level(0),Level(1),2);
        StairWalls(y0,top,true);
    }
    // Guest toilets: marble walls, walnut cubicles with doors, wall-hung WCs, vessel basins with chrome taps, LED mirror.
    static void Toilets(float sx,float y0,float top,float yc)
    {
        float a=sx*4.15f,b=sx*11.2f,z0=-CoreWallZ+.1f,z1=CoreWallZ-.1f,x0=Lo(a,b),x1=Hi(a,b),ty=y0+3.0f;
        WallX(M.paint,-CoreWallZ,x0,x1,y0,top,.2f,new Gap(sx<0?-9.6f:8.7f,sx<0?-8.7f:9.6f,2.1f));
        Leaf(true,-CoreWallZ,sx<0?-9.6f:8.7f,sx<0?-8.7f:9.6f,y0,1f);
        WallX(M.paint,CoreWallZ,x0,x1,y0,top,.2f);
        Box(M.marbleWet,x0,x1,y0,y0+Fin,z0,z1);
        Box(M.marble,x0,x1,y0,ty,z1-.01f,z1);
        Box(M.marble,x0,Lo(sx*8.65f,sx*9.65f),y0,ty,z0,z0+.01f);Box(M.marble,Hi(sx*8.65f,sx*9.65f),x1,y0,ty,z0,z0+.01f);
        Box(M.marble,Lo(sx*8.65f,sx*9.65f),Hi(sx*8.65f,sx*9.65f),y0+2.1f,ty,z0,z0+.01f);
        // the lining on the lift-core side sits on the toilet face of the 20 cm core wall (x +-4.35), not inside it: in
        // the wall's lobby face plane (x +-4.15) it fought with the paint (owner 13:05 "zero migotania")
        float c0=sx>0?x0+.2f:x0,c1=sx>0?x1:x1-.2f;
        Box(M.marble,c0,c0+.01f,y0,ty,z0,z1);Box(M.marble,c1-.01f,c1,y0,ty,z0,z1);
        // three cubicles against the back wall: walnut partitions and doors (open 90 degrees), wall-hung WCs
        for(int i=0;i<3;i++)
        {
            float c=sx*(5.2f+i*1.3f),p=c+sx*.65f;
            Box(M.walnut,p-.025f,p+.025f,y0+.12f,y0+2.2f,z1-1.6f,z1,true);
            Box(M.chrome,p-.03f,p+.03f,y0,y0+.12f,z1-1.6f,z1-1.5f);
            Box(M.marble,c-.25f,c+.25f,y0,y0+1.1f,z1-.18f,z1,true);
            W.Walec(M.whitePaint,new Vector3(c,y0+.22f,z1-.45f),.19f,.18f,20,true);
            W.Walec(M.whitePaint,new Vector3(c,y0+.40f,z1-.45f),.20f,.03f,20);
            Box(M.chrome,c-.1f,c+.1f,y0+.92f,y0+1.04f,z1-.19f,z1-.18f);
            Box(M.walnut,c-sx*.6f,c-sx*.58f,y0+.12f,y0+2.2f,z1-1.6f-.8f,z1-1.6f);           // door leaf open into the room
            Box(M.chrome,c-sx*.62f,c-sx*.56f,y0+1.0f,y0+1.03f,z1-2.3f,z1-2.2f);
        }
        Box(M.walnut,Lo(sx*4.55f,sx*8.55f),Hi(sx*4.55f,sx*8.55f),y0+2.2f,y0+2.26f,z1-1.62f,z1-1.58f);
        // vanity: marble slab on walnut, two vessel basins, chrome taps, LED-backlit mirror
        float v0=Lo(sx*4.35f,sx*8.4f),v1=Hi(sx*4.35f,sx*8.4f);
        Box(M.marble,v0,v1,y0+.82f,y0+.88f,z0,z0+.55f,true);
        Box(M.walnut,v0+.05f,v1-.05f,y0+.3f,y0+.82f,z0,z0+.5f);
        for(float x=v0+1.0f;x<v1-.5f;x+=2.0f)
        {
            W.Walec(M.whitePaint,new Vector3(x,y0+.88f,z0+.3f),.2f,.13f,20);
            Rod(M.chrome,new Vector3(x,y0+.88f,z0+.06f),new Vector3(x,y0+1.16f,z0+.06f),.014f);
            Rod(M.chrome,new Vector3(x,y0+1.16f,z0+.06f),new Vector3(x,y0+1.16f,z0+.22f),.012f);
        }
        BraxChampionsMirror.Wall("T03 guest toilets vanity",new Vector3((v0+v1)*.5f,y0+1.7f,z0+.01f),Vector3.forward,v1-v0-.24f,.96f,M.chrome,8f,12f,768,192);
        Box(M.ledCool,v0+.12f,v1-.12f,y0+1.18f,y0+1.2f,z0+.01f,z0+.025f);
        Ceil(x0,x1,z0,z1,ty);
        Downlight((x0+x1)*.5f,0,ty);Downlight(sx*5.5f,0,ty);Downlight(sx*9.8f,0,ty);
        W.Lampa((sx<0?"men":"women")+" toilet light",new Vector3((x0+x1)*.5f,y0+2.6f,0),.7f,6f);
    }
    // Stair enclosures: core walls with the landing openings, side walls to the service rooms / toilets.
    static void StairWalls(float y0,float top,bool podiumLevel,Material face=null)
    {
        var wm=face??M.paint;
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*StairX0,b=sx*StairX1;
            foreach(float sz in new[]{-1f,1f})
            {
                WallX(wm,sz*CoreWallZ,Lo(sx*11.2f,b),Hi(sx*11.2f,b),y0,top,.2f,new Gap(sx<0?-12.5f:11.4f,sx<0?-11.4f:12.5f,2.4f));
                Leaf(true,sz*CoreWallZ,sx<0?-12.5f:11.4f,sx<0?-11.4f:12.5f,y0,1f,2.4f);                 // stairwell door
            }
            WallZ(M.paint,sx*11.25f,-CoreWallZ+.1f,CoreWallZ-.1f,y0,top,.1f);
            if(podiumLevel)WallZ(M.paint,sx*(StairX1+.1f),-CoreWallZ-.1f,CoreWallZ+.1f,y0,top,.2f);
            Box(M.terrazzo,Lo(a,sx*StairLand),Hi(a,sx*StairLand),y0,y0+Fin,-2.4f,2.4f);
            if(podiumLevel)Box(M.terrazzo,Lo(sx*StairLand,b),Hi(sx*StairLand,b),y0,y0+Fin,-2.4f,2.4f);
        }
    }
    static void Soffit(float x0,float x1,float z0,float z1,float ys,float yc,float w)
    {
        Box(M.ceiling,x0,x1,ys,yc,z0,z0+w);Box(M.ceiling,x0,x1,ys,yc,z1-w,z1);
        Box(M.ceiling,x0,x0+w,ys,yc,z0+w,z1-w);Box(M.ceiling,x1-w,x1,ys,yc,z0+w,z1-w);
        Box(M.ledCool,x0+w,x1-w,ys,ys+.025f,z0+w-.012f,z0+w);Box(M.ledCool,x0+w,x1-w,ys,ys+.025f,z1-w,z1-w+.012f);
        Box(M.ledCool,x0+w-.012f,x0+w,ys,ys+.025f,z0+w,z1-w);Box(M.ledCool,x1-w,x1-w+.012f,ys,ys+.025f,z0+w,z1-w);
        for(float x=x0+.5f;x<x1;x+=1.6f){Downlight(x,z0+w*.5f,ys);Downlight(x,z1-w*.5f,ys);}
    }
    // Large-format stone panels with 4 mm joints on a wall face (openings left free).
    static void Clad(bool alongX,float plane,float into,float a,float b,float y0,float y1,float pw,float ph,Material m,params Gap[] gaps)
    {
        float t=.02f,p0=Lo(plane,plane+into*t),p1=Hi(plane,plane+into*t);
        for(float u=a;u<b-.01f;u+=pw)
            for(float v=y0;v<y1-.01f;v+=ph)
            {
                float u1=Mathf.Min(u+pw,b)-.002f,v1=Mathf.Min(v+ph,y1)-.002f,ua=u+.002f,va=v+.002f;
                bool hole=false;
                foreach(var g in gaps)if(ua<g.b&&u1>g.a&&va<y0+g.top){hole=true;break;}
                if(hole)
                {
                    // split the panel around the opening: parts left/right/above
                    foreach(var g in gaps)
                    {
                        if(!(ua<g.b&&u1>g.a&&va<y0+g.top))continue;
                        if(ua<g.a)Panel(alongX,ua,g.a,va,v1,p0,p1,m);
                        if(u1>g.b)Panel(alongX,g.b,u1,va,v1,p0,p1,m);
                        if(v1>y0+g.top)Panel(alongX,Mathf.Max(ua,g.a),Mathf.Min(u1,g.b),y0+g.top,v1,p0,p1,m);
                    }
                    continue;
                }
                Panel(alongX,ua,u1,va,v1,p0,p1,m);
            }
    }
    static void Panel(bool alongX,float u0,float u1,float v0,float v1,float p0,float p1,Material m)
    {
        if(alongX)Box(m,u0,u1,v0,v1,p0,p1);else Box(m,p0,p1,v0,v1,u0,u1);
    }
    // Fluted front: vertical half-round flutes (4.5 cm, 6.5 cm pitch) standing on a face plane.
    static void Fluted(bool alongX,float plane,float into,float a,float b,float y0,float y1,Material m)
    {
        float p0=Lo(plane,plane+into*.025f),p1=Hi(plane,plane+into*.025f);
        for(float u=a+.01f;u+.045f<=b;u+=.065f)Panel(alongX,u,u+.045f,y0,y1,p0,p1,m);
    }
    static void LuggageTrolley(float x,float y,float z)
    {
        Box(M.rugLobby,x-.55f,x+.55f,y+.16f,y+.20f,z-.32f,z+.32f,true);
        Box(M.brass,x-.57f,x+.57f,y+.12f,y+.16f,z-.34f,z+.34f);
        foreach(float dx in new[]{-.48f,.48f})foreach(float dz in new[]{-.26f,.26f})W.Walec(M.darkBronze,new Vector3(x+dx,y,z+dz),.05f,.12f,10);
        foreach(float dx in new[]{-.52f,.52f})Rod(M.brass,new Vector3(x+dx,y+.2f,z),new Vector3(x+dx,y+1.75f,z),.02f);
        Rod(M.brass,new Vector3(x-.52f,y+1.75f,z),new Vector3(x+.52f,y+1.75f,z),.02f);
        Rod(M.brass,new Vector3(x-.52f,y+1.55f,z),new Vector3(x+.52f,y+1.55f,z),.014f);
        Model(H+"vintage_suitcase/vintage_suitcase_1k.gltf",podiumInside,"suitcase on trolley",new Vector3(x,y+.2f,z),0f,false);
    }
    static void LobbySeat(string path,string label,float x,float y,float z,float yaw,float width)
    {
        if(!Model(path,podiumInside,label,new Vector3(x,y+Fin,z),yaw,true))ProceduralSofa(x,y,z,yaw,width);
    }
    static void Stool(float x,float y,float z)
    {
        Rod(M.brass,new Vector3(x,y,z),new Vector3(x,y+.72f,z),.03f);
        Prism(Circle(x,z,.2f,12),y,y+.02f,M.brass,M.brass,false);
        Prism(Circle(x,z,.21f,12),y+.72f,y+.80f,M.cognac,M.cognac,true);
    }
    const string H="HotelSobczak/",A="ArenaLobby/";
    enum Snap{Floor,Top,Centre}
    // Poly Haven CC0 model at real size under 'parent' (the root is still at the origin, scale 1).
    // at: floor point (Floor), ceiling/cable point (Top) or centre (Centre); yaw turns the model front (+Z).
    static GameObject Model(string path,Transform parent,string label,Vector3 at,float yaw,bool solid,Snap snap=Snap.Floor)
    {
        string full="Assets/Source/Mistrzowie/"+path;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(full);
        if(!source){BraxBuildQuality.Warn("DO POPRAWKI T03_MODEL_MISSING "+full+" (prepare_hotel_sobczak_models.py / prepare_arena_lobby_models.py)");modelsMissing++;return null;}
        if(!HasTextures(source))   // import-order guard (as in the arena): re-import once so the model gets its maps
        {
            AssetDatabase.ImportAsset(full,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            source=AssetDatabase.LoadAssetAtPath<GameObject>(full);
            if(!source)return null;
        }
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!go)return null;
        go.name="T03 | "+label+" | Poly Haven CC0, real size";
        go.transform.SetParent(parent,false);
        go.transform.localRotation=Quaternion.Euler(0,yaw,0)*go.transform.localRotation;
        go.transform.localScale=Vector3.one*BraxChampionsRealSize.Scale(path);       // owner 12:52: real size everywhere
        go.transform.localPosition=at;
        var target=parent.TransformPoint(at);
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length>0)
        {
            var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            go.transform.position+=snap==Snap.Floor?Vector3.up*(target.y-b.min.y):snap==Snap.Top?Vector3.up*(target.y-b.max.y):target-b.center;
            if(solid)
            {
                b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                var box=go.AddComponent<BoxCollider>();box.center=go.transform.InverseTransformPoint(b.center);
                var size=go.transform.InverseTransformVector(b.size);box.size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
            }
        }
        if(!HasTextures(go))BraxBuildQuality.Warn("DO POPRAWKI T03_MODEL_UNTEXTURED "+full);
        modelsPlaced++;
        return go;
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
    static readonly List<(Transform chair,GameObject model)> diningChairs=new List<(Transform,GameObject)>();
    static Transform chairProto;
    static void FirstFloor()
    {
        In(podiumInside,true);
        float y0=Level(1),yc=CeilingOf(1)-.01f,top=Level(2),ys=yc-.45f;
        float zf=-PodiumHalfD+.3f,zb=PodiumHalfD-.3f,xw=PodiumHalfW-.3f;
        // restaurant across the front: herringbone parquet, soffit with LED cove, walnut slat wall, 12 pedestal tables
        Box(M.parquet,-xw,xw,y0,y0+Fin,zf,-CoreWallZ-.1f);
        CeilAround(xw,zb,yc);
        Soffit(-xw,xw,zf,-CoreWallZ-.1f,ys,yc,.9f);
        foreach(float sx in new[]{-1f,1f})
        {
            Box(M.parquet,sx*(StairX1+.2f),sx*xw,y0,y0+Fin,-2.4f,2.4f);                    // nook off the restaurant
            WallX(M.paint,CoreWallZ,Lo(sx*(StairX1+.2f),sx*xw),Hi(sx*(StairX1+.2f),sx*xw),y0,top,.2f);   // kitchen / SPA stay closed
            // walnut slats on the core wall between the lift wall and the stair openings
            for(float x=sx*4.4f;Mathf.Abs(x)<11.2f;x+=sx*.12f)
            {
                if(Mathf.Abs(x)>6.9f&&Mathf.Abs(x)<8.3f)continue;                                 // servery / SPA doors
                Box(M.walnut,x-.035f,x+.035f,y0,ys,-CoreWallZ-.16f,-CoreWallZ-.1f);
            }
        }
        LiftFront(y0,top,M.marble);
        Box(M.marble,-LiftBank,LiftBank,y0+2.9f,ys,-CoreWallZ-.12f,-CoreWallZ-.1f);
        // chair prototype for the far LOD (four walnut legs, padded leather seat and back), cloned after saving
        chairProto=G(podiumInside,"T03 PROTO | dining chair LOD1 (cloned per chair)");
        In(chairProto,true);
        foreach(float dx in new[]{-.18f,.18f})foreach(float dz in new[]{-.18f,.18f})Box(M.walnutDark,dx-.018f,dx+.018f,0,.44f,dz-.018f,dz+.018f);
        Box(M.cognac,-.22f,.22f,.42f,.50f,-.22f,.22f);
        W.OBox(M.cognac,new Vector3(0,.72f,-.21f),new Vector3(.42f,.44f,.06f),Quaternion.Euler(-8f,0,0));
        In(podiumInside,true);
        diningChairs.Clear();
        int t=0;
        foreach(float x in new[]{-15.5f,-9.5f,-3.5f,2.5f})foreach(float z in new[]{-15.6f,-11.2f,-6.8f})DiningTable(x,y0,z,yc,t++);
        // bar: fluted walnut front, marble top, brass foot rail, mirrored back bar, leather stools
        Fluted(false,13.5f,-1f,-16f,-6f,y0+.08f,y0+1.05f,M.walnut);
        Box(M.walnutDark,13.5f,14.3f,y0,y0+1.05f,-16f,-6f,true);Box(M.brass,13.43f,13.51f,y0,y0+.08f,-16.02f,-5.98f);
        Box(M.marble,13.35f,14.4f,y0+1.05f,y0+1.11f,-16.1f,-5.9f,true);
        Rod(M.brass,new Vector3(13.3f,y0+.22f,-15.9f),new Vector3(13.3f,y0+.22f,-6.1f),.025f);
        Box(M.walnut,xw-.45f,xw,y0,y0+.95f,-16f,-6f,true);
        BraxChampionsMirror.Wall("T03 restaurant bar back",new Vector3(xw-.02f,y0+1.875f,-11.0f),Vector3.left,9.76f,1.82f,M.brass,20f,30f,1024,192);
        for(float yy=y0+1.25f;yy<y0+2.8f;yy+=.5f){Box(M.walnut,xw-.32f,xw,yy,yy+.04f,-15.9f,-6.1f);Box(M.ledCool,xw-.32f,xw-.29f,yy-.02f,yy,-15.8f,-6.2f);}
        for(float z=-15.0f;z<=-6.9f;z+=1.6f)
            if(!Model(H+"bar_chair_round_01/bar_chair_round_01_1k.gltf",podiumInside,"restaurant bar stool, leather",new Vector3(12.6f,y0+Fin,z),90f,true))Stool(12.6f,y0,z);
        W.Lampa("restaurant light W",new Vector3(-10f,ys-1f,-11f),1.0f,13f);
        W.Lampa("restaurant light E",new Vector3(8f,ys-1f,-11f),1.0f,13f);
        // core band service rooms (servery west, SPA reception east)
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*4.15f,b=sx*11.2f;
            WallX(M.paint,-CoreWallZ,Lo(a,b),Hi(a,b),y0,top,.2f,new Gap(sx<0?-8.2f:7.0f,sx<0?-7.0f:8.2f,2.2f));
            WallX(M.paint,CoreWallZ,Lo(a,b),Hi(a,b),y0,top,.2f,new Gap(sx<0?-8.2f:7.0f,sx<0?-7.0f:8.2f,2.2f));
            foreach(float wz in new[]{-CoreWallZ,CoreWallZ})Leaf(true,wz,sx<0?-8.2f:7.0f,sx<0?-7.0f:8.2f,y0,1f,2.2f);   // kitchen / SPA doors
            Box(sx<0?M.antiSkid:M.terrazzo,Lo(a,b),Hi(a,b),y0,y0+Fin,-2.4f,2.4f);
        }
        CoreBack(y0,top);
        Box(M.steel,-10.6f,-5f,y0,y0+.9f,-2f,-1.3f,true);                                               // pass counter
        Box(M.whiteTiles,-10.6f,-5f,y0+.9f,y0+2.4f,-2.4f,-2.39f);
        Fluted(true,1.2f,-1f,5.5f,8.5f,y0+.08f,y0+1.05f,M.walnut);                                      // SPA desk
        Box(M.walnutDark,5.5f,8.5f,y0,y0+1.05f,1.2f,1.9f,true);Box(M.marble,5.45f,8.55f,y0+1.05f,y0+1.1f,1.15f,1.95f,true);
        // kitchen (west back): white tiled walls, anti-slip floor, stainless lines, island, hoods, cold room
        Box(M.antiSkid,-xw,-4.3f,y0,y0+Fin,CoreWallZ+.1f,zb);
        WallZ(M.whiteTiles,-4.25f,CoreWallZ+.1f,zb,y0,top,.1f);
        Box(M.whiteTiles,-xw,-4.3f,y0,y0+3.0f,zb-.01f,zb);
        for(float x=-19f;x<=-6.5f;x+=3.1f)
        {
            Box(M.steel,x-1.4f,x+1.4f,y0,y0+.9f,zb-.75f,zb,true);
            Box(M.steel,x-1.4f,x+1.4f,y0+2.2f,y0+2.7f,zb-1.0f,zb);
            Box(M.screen,x-1.2f,x-.6f,y0+.9f,y0+.92f,zb-.6f,zb-.15f);                                // induction hobs
        }
        Box(M.steel,-15f,-8f,y0,y0+.9f,9f,10.4f,true);
        Box(M.steel,-xw,-16.5f,y0,y0+2.8f,3.2f,7.5f,true);
        Ceil(-xw,-4.3f,CoreWallZ+.1f,zb,yc-1.6f);
        W.Lampa("kitchen light",new Vector3(-12f,yc-1.9f,11f),1.0f,12f);
        // SPA (east back): raised pool with marble surround, white leather loungers with rolled towels, plants
        Box(M.terrazzo,4.3f,xw,y0,y0+Fin,CoreWallZ+.1f,zb);
        WallZ(M.paint,4.25f,CoreWallZ+.1f,zb,y0,top,.1f);
        Clad(false,4.3f,1f,CoreWallZ+.1f,zb,y0,ys,1.2f,1.5f,M.marble);
        Ring(new List<Vector2>{new Vector2(8f,7.5f),new Vector2(17f,7.5f),new Vector2(17f,12f),new Vector2(8f,12f)},y0,y0+.55f,.3f,M.marble,true);
        Box(M.water,8.15f,16.85f,y0+.05f,y0+.45f,7.65f,11.85f);
        for(float x=8.5f;x<=16.6f;x+=2.7f)
        {
            Lounger(podiumInside,x,y0,15.6f);
            W.OBox(M.linenWhite,new Vector3(x,y0+.47f,15.0f),new Vector3(.16f,.16f,.5f),Quaternion.Euler(0,90f,0));
        }
        foreach(float x in new[]{6.0f,19.6f})Model(A+"potted_plant_02/potted_plant_02_1k.gltf",podiumInside,"SPA plant",new Vector3(x,y0+Fin,17.6f),0f,false);
        WallX(M.paint,4.4f,15.5f,xw,y0,top,.12f,new Gap(16.2f,17.1f,2.1f));
        Leaf(true,4.4f,16.2f,17.1f,y0,1f);                                                               // treatment room door
        Box(M.walnut,17.0f,19.6f,y0,y0+.62f,2.9f,3.7f,true);Box(M.cognac,16.95f,19.65f,y0+.62f,y0+.74f,2.85f,3.75f,true);   // treatment bed
        Soffit(4.3f,xw,CoreWallZ+.1f,zb,ys,yc,.8f);
        W.Lampa("SPA light",new Vector3(12.5f,ys-1f,10f),.9f,12f);
        // back of house behind the lift bank
        Box(M.antiSkid,-4.15f,4.15f,y0,y0+Fin,CoreWallZ+.1f,zb);
        for(float z=4f;z<=16f;z+=3f)Box(M.walnutDark,-3.8f,-3.2f,y0,y0+2.2f,z-1f,z+1f,true);
        Stair(-1f,Level(1),Level(2),2);Stair(1f,Level(1),Level(2),2);
        StairWalls(y0,top,true);
    }
    // Pedestal table (marble top with a brass edge, brass column and round base), pendant on a cable over it,
    // four leather dining chairs (Poly Haven dining_chair_02 close up, LOD1 prototype further away).
    static void DiningTable(float x,float y,float z,float yc,int n)
    {
        Prism(Circle(x,z,.55f,24),y+.73f,y+.765f,M.marble,M.marble,true);
        Ring(Circle(x,z,.555f,24),y+.725f,y+.765f,.012f,M.brass,false);
        Rod(M.brass,new Vector3(x,y,z),new Vector3(x,y+.73f,z),.045f);
        Prism(Circle(x,z,.30f,20),y,y+.03f,M.brass,M.brass,false);
        Rod(M.chrome,new Vector3(x,yc,z),new Vector3(x,y+2.45f,z),.004f);
        Model(H+"modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf",podiumInside,"restaurant pendant "+n,new Vector3(x,y+2.45f,z),0f,false,Snap.Top);
        Model(H+"ceramic_vase_01/ceramic_vase_01_1k.gltf",podiumInside,"table vase "+n,new Vector3(x,y+.765f,z),n*37f,false);
        foreach(var (dx,dz,yaw) in new[]{(-.78f,0f,90f),(.78f,0f,-90f),(0f,-.78f,0f),(0f,.78f,180f)})
        {
            var holder=G(podiumInside,"restaurant chair "+n+" | LOD0 leather model / LOD1 prototype");
            holder.localPosition=new Vector3(x+dx,y+Fin,z+dz);holder.localRotation=Quaternion.Euler(0,yaw,0);
            var model=Model(H+"dining_chair_02/dining_chair_02_2k.gltf",holder,"restaurant chair "+n,Vector3.zero,0f,true);
            diningChairs.Add((holder,model));
        }
    }
    // After W.Zapisz: clone the LOD1 prototype under every chair and give each chair a LODGroup.
    static void FinishChairs()
    {
        if(!chairProto)return;
        foreach(var (holder,model) in diningChairs)
        {
            var far=UnityEngine.Object.Instantiate(chairProto.gameObject,holder,false);far.name="LOD1 prototype";
            far.transform.localPosition=Vector3.zero;far.transform.localRotation=Quaternion.identity;far.SetActive(true);
            var farRenderers=far.GetComponentsInChildren<Renderer>(true);
            if(model)
            {
                var lod=holder.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[]{new LOD(.16f,model.GetComponentsInChildren<Renderer>(true)),new LOD(.012f,farRenderers)});
                lod.RecalculateBounds();
            }
        }
        UnityEngine.Object.DestroyImmediate(chairProto.gameObject);chairProto=null;
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
            // woven border of the damask carpet along both walls (velvet band, 2 mm proud)
            Box(M.velvet,-xw,xw,Fin,Fin+.002f,Lo(a,b)+.10f,Lo(a,b)+.22f);Box(M.velvet,-xw,xw,Fin,Fin+.002f,Hi(a,b)-.22f,Hi(a,b)-.10f);
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
            for(int i=1;i<5;i++)WallZ(M.grasscloth,-TowerHalfW+i*RoomW,Lo(sz*RoomWallZ,sz*(TowerHalfD-.3f)),Hi(sz*RoomWallZ,sz*(TowerHalfD-.3f)),0,h,.2f);
        // core band: lift fronts, service rooms (linen west, technical east), stair walls, back core wall
        LiftFront(0,h,M.grasscloth);
        foreach(float sx in new[]{-1f,1f})
        {
            float a=sx*4.15f,b=sx*11.2f;
            WallX(M.grasscloth,-CoreWallZ,Lo(a,b),Hi(a,b),0,h,.2f);
            WallX(M.grasscloth,CoreWallZ,Lo(a,b),Hi(a,b),0,h,.2f,new Gap(sx<0?-7.0f:6.1f,sx<0?-6.1f:7.0f,2.1f));
            Leaf(true,CoreWallZ,sx<0?-7.0f:6.1f,sx<0?-6.1f:7.0f,0,1f);
            Box(M.tile,Lo(a,b),Hi(a,b),0,Fin,-2.4f,2.4f);
            Ceil(Lo(a,b),Hi(a,b),-2.4f,2.4f,yc);
            for(float x=Lo(a,b)+.8f;x<Hi(a,b)-.5f;x+=1.4f)Box(M.teakDark,x-.6f,x+.6f,0,2.2f,-2.4f,-1.85f,true);
        }
        CoreBack(0,h,M.grasscloth);
        StairWalls(0,h,false,M.grasscloth);
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

    // One finished, furnished 5* room. The shell (walls, floors, ceiling, door, bathroom walls) belongs to the
    // floor; furniture, fixtures and the room light live in "ROOM n | furniture" and are drawn only while the
    // player is in the room or at its door (BraxWidocznoscWnetrza added per instance in Generate).
    static readonly List<(int n,float a,float b,float zc,float zf,float s)> roomRects=new List<(int,float,float,float,float,float)>();
    static void Room(Transform t,int number,float a,float b,float row)
    {
        In(t,true);
        float zc=row*(RoomWallZ+.1f),zf=row*(TowerHalfD-.3f),s=row;
        float Z(float d)=>zc+s*d;
        float yc=GuestStorey-Slab-.01f,h=GuestStorey,zh=Z(3.3f);
        roomRects.Add((number,a,b,zc,zf,s));
        var marker=new GameObject("ROOM "+number+" | 6.80x10.50 | finished, furnished").transform;marker.SetParent(t,false);
        marker.localPosition=new Vector3((a+b)*.5f,0,(zc+zf)*.5f);
        // corridor wall (grasscloth both faces) with the entry door, open leaf into the hall, brass number plate
        float d0=a+.25f,d1=a+1.15f,wall=row*RoomWallZ;
        WallX(M.grasscloth,wall,a-.1f,b+.1f,0,h,.2f,new Gap(d0,d1,2.1f));
        Leaf(true,wall,d0,d1,0,s);
        float plateZ=wall-s*.1f;
        Box(M.brass,d1+.15f,d1+.45f,1.45f,1.65f,Lo(plateZ,plateZ-s*.012f),Hi(plateZ,plateZ-s*.012f));
        // the number itself: dark bronze 3D digits 2 mm proud of the brass plate, set per floor by Renumber
        // (owner 10.11 "globalnie zakaz tanich napisów": no TextMesh)
        var numberAnchor=new GameObject("ROOM NUMBER "+number).transform;numberAnchor.SetParent(t,false);
        numberAnchor.localPosition=new Vector3(d1+.3f,1.55f-.034f,plateZ-s*.014f);
        numberAnchor.localRotation=Quaternion.Euler(0,BraxChampionsLetters3D.YawFacing(new Vector3(0,0,-s)),0);
        Box(M.warmLight,d1+.2f,d1+.4f,2.05f,2.25f,Lo(plateZ,plateZ-s*.03f),Hi(plateZ,plateZ-s*.03f));
        // floors: oak hall and bedroom, marble bathroom
        float bx0=a+1.45f,bx1=a+4.55f;
        Box(M.oak,a,b,0,Fin,zh,zf);
        Box(M.oak,a,bx0,0,Fin,zc,zh);
        Box(M.oak,bx1,b,0,Fin,zc,zh);
        Box(M.marbleWet,bx0,bx1,0,Fin,zc,zh);
        // bathroom walls (grasscloth outside, marble lining inside), 0.80 door from the hall
        float zm=(zc+zh)*.5f;
        WallZ(M.grasscloth,bx0+.06f,Lo(zc,zh),Hi(zc,zh),0,h,.12f,new Gap(zm-.4f,zm+.4f,2.1f));
        Leaf(false,bx0+.06f,zm-.4f,zm+.4f,0,1f);
        WallZ(M.grasscloth,bx1-.06f,Lo(zc,zh),Hi(zc,zh),0,h,.12f);
        WallX(M.grasscloth,zh,bx0,bx1,0,h,.12f);
        float ix0=bx0+.12f,ix1=bx1-.12f,iz0=Lo(zc,zh-s*.06f),iz1=Hi(zc,zh-s*.06f);
        Box(M.marble,ix0,ix0+.01f,0,yc,iz0,zm-.4f);Box(M.marble,ix0,ix0+.01f,0,yc,zm+.4f,iz1);Box(M.marble,ix0,ix0+.01f,2.1f,yc,zm-.4f,zm+.4f);
        Box(M.marble,ix1-.01f,ix1,0,yc,iz0,iz1);
        Box(M.marble,ix0,ix1,0,yc,zc,zc+s*.01f);Box(M.marble,ix0,ix1,0,yc,zh-s*.06f,zh-s*.07f);
        // window wall finished inside: grasscloth on piers, sill and head, marble window boards
        float x0=Mathf.Round((a+TowerHalfW)/RoomW)*RoomW-TowerHalfW,x1=x0+RoomW,mid=(x0+x1)*.5f,wz=zf-s*.02f,wb=.6f,wt=GuestStorey-Slab-.35f;
        Box(M.grasscloth,a,x0+.5f,0,yc,zf,wz);Box(M.grasscloth,x1-.5f,b,0,yc,zf,wz);
        Box(M.grasscloth,mid-.25f,mid+.25f,0,yc,zf,wz);
        Box(M.grasscloth,a,b,0,wb,zf,wz);Box(M.grasscloth,a,b,wt,yc,zf,wz);
        foreach(var (w0,w1) in new[]{(x0+.5f,mid-.25f),(mid+.25f,x1-.5f)})
            Box(M.marble,w0-.03f,w1+.03f,wb-.03f,wb+.02f,zf+s*.16f,zf-s*.06f);
        Box(M.darkBronze,a,b,yc-.06f,yc,zf-s*.08f,zf-s*.18f);                          // curtain track pelmet
        // ceiling and downlights
        Ceil(a,b,Lo(zc,zf),Hi(zc,zf),yc);
        float zb=(zh+zf)*.5f-s*.2f;
        foreach(float dz in new[]{-1.6f,1.6f})Downlight(a+2.6f,zb+dz,yc);
        Downlight((a+bx0)*.5f,zm,yc);Downlight((bx0+bx1)*.5f,zm,yc);Downlight((bx1+b)*.5f,zm,yc);

        // ---------------- furniture, fixtures and light (drawn only near the room)
        var fg=G(t,"ROOM "+number+" | furniture (drawn when inside)");
        In(fg,true);
        // bed wall: smoked walnut panel, channel-tufted boucle headboard, brass cap
        Box(M.walnutDark,a,a+.03f,0,2.8f,zb-2.0f,zb+2.0f);
        for(int i=0;i<5;i++)Box(M.boucle,a+.03f,a+.14f,.45f,1.55f,zb-1.2f+i*.48f+.012f,zb-1.2f+(i+1)*.48f-.012f,i==2);
        Box(M.brass,a+.03f,a+.15f,1.55f,1.575f,zb-1.21f,zb+1.21f);
        // king bed (owner 9.11 23:02: no boxes, round pillows): upholstered divan on a recessed walnut plinth,
        // mattress with rounded edges, duvet draped over the sides with a rolled turn-down, satin runner,
        // two euro pillows, two sleeping pillows, two velvet cushions and a satin bolster - all soft shapes
        Box(M.walnutDark,a+.22f,a+2.15f,0,.10f,zb-.9f,zb+.9f);
        var flat=Quaternion.identity;
        Soft(M.jersey,new Vector3(a+1.195f,.22f,zb),new Vector3(2.11f,.25f,2.0f),flat,.30f,.10f,24);
        W.KolizjaMM(a+.14f,a+2.25f,.10f,.58f,zb-1.0f,zb+1.0f);
        Soft(M.linenWhite,new Vector3(a+1.19f,.45f,zb),new Vector3(2.08f,.24f,1.94f),flat,.35f,.10f,24);
        Soft(M.linenWhite,new Vector3(a+1.61f,.47f,zb),new Vector3(1.40f,.30f,2.08f),flat,.28f,.08f,24);   // duvet with drops
        Rod(M.linenWhite,new Vector3(a+.93f,.585f,zb-1.02f),new Vector3(a+.93f,.585f,zb+1.02f),.045f);       // rolled turn-down
        Soft(M.satin,new Vector3(a+1.93f,.475f,zb),new Vector3(.42f,.31f,2.12f),flat,.28f,.08f,24);          // runner over the foot
        var lean=Quaternion.Euler(0,0,-72f);
        foreach(float dz in new[]{-.50f,.50f})
        {
            Soft(M.linenWhite,new Vector3(a+.31f,.86f,zb+dz),new Vector3(.62f,.18f,.64f),lean,.9f,.35f);                          // euro pillow
            Soft(M.linenWhite,new Vector3(a+.50f,.77f,zb+dz),new Vector3(.46f,.16f,.72f),Quaternion.Euler(0,0,-64f),.9f,.35f);    // sleeping pillow
        }
        foreach(float dz in new[]{-.36f,.36f})
            Soft(M.velvet,new Vector3(a+.67f,.73f,zb+dz),new Vector3(.40f,.14f,.40f),Quaternion.Euler(0,dz*30f,-58f),.9f,.4f);    // velvet cushion
        Rod(M.satin,new Vector3(a+.86f,.665f,zb-.42f),new Vector3(a+.86f,.665f,zb+.42f),.09f);                // bolster
        // nightstands (Poly Haven side table), pendants on cables, vase and plant
        foreach(float sd in new[]{-1f,1f})
        {
            Model(H+"side_table_01/side_table_01_1k.gltf",fg,"room "+number+" nightstand",new Vector3(a+.34f,Fin,zb+sd*1.5f),90f,true);
            Rod(M.chrome,new Vector3(a+.34f,yc,zb+sd*1.5f),new Vector3(a+.34f,1.95f,zb+sd*1.5f),.004f);
            Model(H+"modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf",fg,"room "+number+" bedside pendant",new Vector3(a+.34f,1.95f,zb+sd*1.5f),0f,false,Snap.Top);
        }
        Model(H+"ceramic_vase_02/ceramic_vase_02_1k.gltf",fg,"room "+number+" vase",new Vector3(a+.30f,.56f,zb-1.62f),30f,false);
        Model(H+"potted_plant_04/potted_plant_04_1k.gltf",fg,"room "+number+" succulent",new Vector3(a+.30f,.56f,zb+1.62f),0f,false);
        // ottoman at the foot of the bed, jacquard rug
        Model(H+"Ottoman_01/Ottoman_01_2k.gltf",fg,"room "+number+" ottoman",new Vector3(a+2.78f,Fin+.014f,zb),90f,true);
        Rug(M.rugRoom,M.jersey,a+.9f,a+4.3f,Fin,zb-1.9f,zb+1.9f);
        // TV wall: walnut media console with shadow gaps and brass pulls, wall-mounted flat TV
        Box(M.walnutDark,b-.42f,b-.06f,0,.16f,zb-1.0f,zb+1.0f);
        Box(M.walnut,b-.46f,b-.02f,.16f,.58f,zb-1.1f,zb+1.1f,true);
        foreach(float dz in new[]{-.37f,.37f})Box(M.walnutDark,b-.465f,b-.455f,.18f,.56f,zb+dz-.004f,zb+dz+.004f);
        foreach(float dz in new[]{-.73f,0f,.73f})Box(M.brass,b-.48f,b-.46f,.48f,.50f,zb+dz-.12f,zb+dz+.12f);
        Box(M.darkBronze,b-.035f,b-.002f,1.10f,1.70f,zb-.55f,zb+.55f);
        Box(M.screen,b-.06f,b-.035f,1.04f,1.76f,zb-.63f,zb+.63f);
        Model(H+"ceramic_vase_01/ceramic_vase_01_1k.gltf",fg,"room "+number+" console vase",new Vector3(b-.25f,.58f,zb+.86f),0f,false);
        // writing desk by the window: walnut slab desk, leather dining chair, arm lamp, framed art
        float dA=9.95f,dB=8.55f;
        Box(M.walnut,b-.66f,b-.02f,.72f,.76f,Z(dB),Z(dA),true);
        Box(M.walnut,b-.64f,b-.04f,0,.72f,Z(dA-.05f),Z(dA),true);Box(M.walnut,b-.64f,b-.04f,0,.72f,Z(dB),Z(dB+.05f),true);
        Box(M.walnut,b-.06f,b-.04f,.30f,.72f,Z(dB+.05f),Z(dA-.05f));
        Box(M.brass,b-.665f,b-.655f,.725f,.755f,Z(dB),Z(dA));
        Model(H+"desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf",fg,"room "+number+" desk lamp",new Vector3(b-.18f,.76f,Z(dA-.25f)),-90f,false);
        Model(H+"dining_chair_02/dining_chair_02_2k.gltf",fg,"room "+number+" desk chair, leather",new Vector3(b-1.0f,Fin,Z((dA+dB)*.5f)),90f,true);
        Model(H+"fancy_picture_frame_01/fancy_picture_frame_01_1k.gltf",fg,"room "+number+" art above desk",new Vector3(b-.015f,1.72f,Z((dA+dB)*.5f)),-90f,false,Snap.Centre);
        // reading corner: leather mid-century lounge chair angled to the room, marble side table, plant, art
        Model(H+"mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf",fg,"room "+number+" lounge chair, leather",new Vector3(a+.80f,Fin,Z(9.55f)),90f+30f*s,true);
        Prism(Circle(a+1.95f,Z(10.05f),.22f,18),.53f,.56f,M.marble,M.marble,true);
        Rod(M.brass,new Vector3(a+1.95f,Fin,Z(10.05f)),new Vector3(a+1.95f,.53f,Z(10.05f)),.025f);
        Prism(Circle(a+1.95f,Z(10.05f),.16f,18),Fin,.02f,M.brass,M.brass,false);
        Model(H+"potted_plant_04/potted_plant_04_1k.gltf",fg,"room "+number+" plant on side table",new Vector3(a+1.95f,.56f,Z(10.05f)),45f,false);
        Model(H+"hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",fg,"room "+number+" art in reading corner",new Vector3(a+.02f,1.75f,Z(9.3f)),90f,false,Snap.Centre);
        // wardrobe (walnut doors, brass bars) and a leather luggage bench in the hall
        Box(M.walnut,bx1+.08f,b-.04f,0,2.6f,Z(.02f),Z(.64f),true);
        float wm=(bx1+.08f+b-.04f)*.5f;
        Box(M.walnutDark,wm-.004f,wm+.004f,.02f,2.58f,Z(.64f),Z(.645f));
        foreach(float dx in new[]{-.08f,.08f})Box(M.brass,wm+dx-.012f,wm+dx+.012f,.8f,1.6f,Z(.645f),Z(.67f));
        Box(M.walnutDark,bx1+.3f,b-.3f,0,.36f,Z(2.0f),Z(2.45f),true);
        Box(M.cognac,bx1+.28f,b-.28f,.36f,.46f,Z(1.98f),Z(2.47f),true);
        // bathroom: walnut vanity, marble top, vessel basin, chrome tap, LED mirror, wall-hung WC, rain shower, towels
        Box(M.walnut,a+1.8f,a+3.2f,.22f,.82f,Z(.02f),Z(.56f),true);
        Box(M.walnutDark,a+1.8f,a+3.2f,.515f,.525f,Z(.56f),Z(.565f));
        Box(M.marble,a+1.75f,a+3.25f,.82f,.87f,Z(.02f),Z(.60f),true);
        W.Walec(M.whitePaint,new Vector3(a+2.5f,.87f,Z(.33f)),.2f,.13f,20);
        W.Walec(M.chrome,new Vector3(a+2.5f,.87f,Z(.33f)),.022f,.006f,8);
        Rod(M.chrome,new Vector3(a+2.5f,.87f,Z(.07f)),new Vector3(a+2.5f,1.16f,Z(.07f)),.014f);
        Rod(M.chrome,new Vector3(a+2.5f,1.16f,Z(.07f)),new Vector3(a+2.5f,1.16f,Z(.24f)),.012f);
        BraxChampionsMirror.Wall("T03 room "+number+" bathroom",new Vector3(a+2.5f,1.50f,Z(.012f)),new Vector3(0,0,s),1.26f,.86f,M.chrome,5f,6f,384,256);
        Box(M.ledCool,a+1.87f,a+3.13f,1.03f,1.05f,Z(.012f),Z(.025f));
        Box(M.marble,a+3.62f,a+4.18f,0,1.1f,Z(.02f),Z(.20f),true);
        Box(M.chrome,a+3.80f,a+4.00f,.94f,1.06f,Z(.20f),Z(.21f));
        W.Walec(M.whitePaint,new Vector3(a+3.9f,.22f,Z(.46f)),.19f,.18f,20,true);
        W.Walec(M.whitePaint,new Vector3(a+3.9f,.40f,Z(.46f)),.20f,.03f,20);
        Box(M.whitePaint,a+3.72f,a+4.08f,.22f,.40f,Z(.20f),Z(.30f));
        Box(M.marbleWet,a+3.1f,ix1,Fin,.05f,zh-s*.07f,Z(2.15f));
        Box(M.clearGlass,a+3.1f,ix1,.05f,2.05f,Z(2.13f),Z(2.15f),true);
        Box(M.chrome,a+3.08f,a+3.12f,.05f,2.05f,Z(2.12f),Z(2.16f));
        Rod(M.chrome,new Vector3(ix1,2.2f,Z(2.75f)),new Vector3(ix1-.32f,2.2f,Z(2.75f)),.012f);
        W.Walec(M.chrome,new Vector3(ix1-.32f,2.17f,Z(2.75f)),.15f,.02f,20);
        Box(M.chrome,ix1-.02f,ix1,1.0f,1.2f,Z(2.6f),Z(2.9f));
        Box(M.chrome,a+3.2f,ix1-.05f,.05f,.055f,Z(3.05f),Z(3.12f));
        Rod(M.chrome,new Vector3(ix1-.04f,1.1f,Z(1.1f)),new Vector3(ix1-.04f,1.1f,Z(1.7f)),.012f);          // heated towel rail
        Rod(M.chrome,new Vector3(ix1-.04f,.8f,Z(1.1f)),new Vector3(ix1-.04f,.8f,Z(1.7f)),.012f);
        Box(M.linenWhite,ix1-.09f,ix1-.02f,.55f,1.13f,Z(1.13f),Z(1.67f));
        Box(M.linenWhite,a+2.25f,a+2.85f,Fin,Fin+.012f,Z(.95f),Z(1.45f));
        W.Lampa("room "+number+" light",new Vector3((a+b)*.5f,yc-.4f,zb),.85f,8f);
        W.Lampa("room "+number+" bath light",new Vector3((bx0+bx1)*.5f,yc-.3f,zm),.55f,4f);
        In(t,true);                        // back to the floor shell: walls built after the rooms must not land in this room's furniture
        roomsBuilt+=GuestFloors;           // the template is instanced on every guest floor
    }
    static void Renumber(Transform floor,int f)
    {
        foreach(var tr in floor.GetComponentsInChildren<Transform>(true))
        {
            if(tr.name.StartsWith("ROOM 2",StringComparison.Ordinal)&&int.TryParse(tr.name.Substring(5,3),out int n))
                tr.name="ROOM "+(f*100+n%100)+tr.name.Substring(8);
        }
        // 3D room numbers of this floor on the brass plates (anchors from Room)
        foreach(var anchor in floor.GetComponentsInChildren<Transform>(true))
        {
            if(!anchor.name.StartsWith("ROOM NUMBER ",StringComparison.Ordinal)||!int.TryParse(anchor.name.Substring(12),out int n))continue;
            var digits=new BraxChampionsMeshBatch();
            BraxChampionsLetters3D.Text(digits,M.darkBronze,M.darkBronze,(f*100+n%100).ToString(),Vector3.zero,0f,.068f,.006f,.10f,.26f);
            BraxChampionsLetters3D.Build(digits,anchor,"ROOM "+(f*100+n%100),1f);   // the hotel root carries K
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
        // roof: the same sign x1.6 (12.8 x 6.4 m) on bronze posts at the front and back edge of the roof terrace,
        // read against the sky from the whole district (owner 9.11 23:29: "nie widać dobrze ten szyld w 3D");
        // top 110.0 m, inside the 110.3 m crown
        foreach(var place in new[]{(name:"facade",pos:new Vector3(0,Level(1)+.9f,-PodiumHalfD),yaw:180f,scale:1f),
                                   (name:"reception wall",pos:new Vector3(11.15f,Ffl0+2.85f,-9.4f),yaw:-90f,scale:.32f),
                                   (name:"roof front",pos:new Vector3(0,RoofDeck+1.95f,-TowerHalfD+.6f),yaw:180f,scale:1.6f),
                                   (name:"roof back",pos:new Vector3(0,RoofDeck+1.95f,TowerHalfD-.6f),yaw:0f,scale:1.6f)})
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
        foreach(float sz in new[]{-1f,1f})foreach(float x in new[]{-4.4f,0f,4.4f})
            Box(M.darkBronze,x-.08f,x+.08f,RoofDeck+.02f,RoofDeck+6.0f,sz*(TowerHalfD-.6f)-(sz<0?-.06f:.22f),sz*(TowerHalfD-.6f)-(sz<0?-.22f:.06f),true);
        return parts;
    }
    [Serializable] sealed class SignFile{public float scaleK;public SignData[] signs;}
    [Serializable] sealed class SignData{public string id;public BraxChampionsFoundation.MeshData[] meshes;}
}

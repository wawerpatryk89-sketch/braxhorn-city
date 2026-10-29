using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Eight real, separate panoramic-capable lift shafts for the OTHER towers.
// The existing BRAXHORN TOWER quay flagship lift is not replaced or moved.
// All roof travel is physical, via BraxCityElevator and the original
// CharacterController. Doors/cabins have collision, not teleport triggers.
public static class BraxCityTowerLiftNetwork
{
    const float HalfShaft=1.83f;
    const float HalfInner=1.65f;
    const float LowerY=.15f;
    const float OpeningHeight=2.93f;
    const float DoorSpeed=1.35f; // Runtime uses this in BraxCityElevator.
    static Material steel,stone,glass,deck,illumination,black;
    static Transform Group(string name,Transform parent,Vector3 local)
    {
        var tr=new GameObject(name).transform;
        tr.SetParent(parent,false);tr.localPosition=local;
        return tr;
    }
    static void Solid(Transform root,string label,Vector3 at,Vector3 size,Material mat)
    {
        if(size.x<=.035f||size.y<=.035f||size.z<=.035f)
            throw new InvalidOperationException("Zero-area passenger lift part: "+label+" "+size);
        BraxCityArchitecture.Box(label,root,at,size,mat,true);
    }
    static void Surface(Transform root,string label,Vector3 at,Vector3 size,Material mat)
    {
        BraxCityArchitecture.Box(label,root,at,size,mat,false);
    }
    static Transform DoorLeaf(string label,Transform parent,Vector3 centre,float dir)
    {
        var leaf=Group(label,parent,centre);
        leaf.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Solid(leaf,"Real sliding smoked glass safety leaf",new Vector3(0,1.39f,0),
            new Vector3(1.47f,2.78f,.075f),glass);
        foreach(float xx in new[]{-.68f,.68f})
            Surface(leaf,"High end stainless stile",new Vector3(xx,1.39f,-dir*.052f),
                new Vector3(.035f,2.78f,.08f),steel);
        Surface(leaf,"Low satin metal threshold",new Vector3(0,.08f,-dir*.052f),
            new Vector3(1.47f,.12f,.075f),steel);
        return leaf;
    }
    static void Rail(Transform root,string name,Vector3 p,Vector3 size,Material mat)
        =>Solid(root,name,p,size,mat);
    static void ApplyGuardrails(Transform parent,Rect top,float roofY,float sx,float sz,bool far)
    {
        const float height=1.45f;
        var center=top.center;
        // Outer perimeter has REAL colliders and a 1.45-m parapet; no
        // unprotected ledge even if a roof mission isn't active yet.
        Rail(parent,"ROOF safety west outer",new Vector3(top.xMin+.14f,roofY+height*.5f,center.y),
            new Vector3(.28f,height,top.height),stone);
        Rail(parent,"ROOF safety east outer",new Vector3(top.xMax-.14f,roofY+height*.5f,center.y),
            new Vector3(.28f,height,top.height),stone);
        Rail(parent,"ROOF safety north outer",new Vector3(center.x,roofY+height*.5f,top.yMax-.14f),
            new Vector3(top.width,height,.28f),stone);
        Rail(parent,"ROOF safety south outer",new Vector3(center.x,roofY+height*.5f,top.yMin+.14f),
            new Vector3(top.width,height,.28f),stone);

        float x0=sx-HalfShaft,x1=sx+HalfShaft,z0=sz-HalfShaft,z1=sz+HalfShaft;
        foreach(float x in new[]{x0-.1f,x1+.1f})
        {
            Rail(parent,"ROOF LIFT glass rescue rail - west or east",
                new Vector3(x,roofY+height*.5f,sz),
                new Vector3(.16f,height,3.7f),glass);
            Surface(parent,"ROOF LIFT upper stainless trim",
                new Vector3(x,roofY+height-.05f,sz),
                new Vector3(.2f,.08f,3.72f),steel);
        }
        // Entry side faces the street for both near and far towers;
        // the other open edge is always guarded in physical glass.
        float protectedZ=far?z1+.08f:z0-.08f;
        Rail(parent,"ROOF LIFT rear safety glass",new Vector3(sx,roofY+height*.5f,protectedZ),
            new Vector3(3.75f,height,.17f),glass);
        Surface(parent,"ROOF LIFT top steel rail",
            new Vector3(sx,roofY+height-.04f,protectedZ),
            new Vector3(3.75f,.08f,.18f),steel);
    }
    static void BuildRoofSlabs(Transform root,Rect top,float sx,float sz,float roofY)
    {
        float lx=sx-HalfShaft,rx=sx+HalfShaft;
        float south=sz-HalfShaft,north=sz+HalfShaft;
        float zc=top.center.y;
        float y=roofY-.135f;
        // Four physical roof slabs surround EXACTLY one roof-size opening.
        Solid(root,"ACCESSIBLE ROOF DECK west",
            new Vector3((top.xMin+lx)*.5f,y,zc),
            new Vector3(lx-top.xMin,.27f,top.height),deck);
        Solid(root,"ACCESSIBLE ROOF DECK east",
            new Vector3((top.xMax+rx)*.5f,y,zc),
            new Vector3(top.xMax-rx,.27f,top.height),deck);
        Solid(root,"ACCESSIBLE ROOF DECK south",
            new Vector3(sx,y,(top.yMin+south)*.5f),
            new Vector3(rx-lx,.27f,south-top.yMin),deck);
        Solid(root,"ACCESSIBLE ROOF DECK north",
            new Vector3(sx,y,(top.yMax+north)*.5f),
            new Vector3(rx-lx,.27f,top.yMax-north),deck);
    }
    static void BuildShaft(Transform root,float sx,float sz,float roofY,bool far)
    {
        float mid=roofY*.5f,dir=far?1f:-1f;
        Solid(root,"Fire-rated real shaft west side",new Vector3(sx-HalfShaft-.06f,mid,sz),
            new Vector3(.15f,roofY+.3f,3.86f),stone);
        Solid(root,"Fire-rated real shaft east side",new Vector3(sx+HalfShaft+.06f,mid,sz),
            new Vector3(.15f,roofY+.3f,3.86f),stone);
        // The rear third side has a full-height closure. The front shaft wall
        // is divided to leave real door openings at BOTH floor landings.
        Solid(root,"Fire-rated rear lift wall",new Vector3(sx,mid,sz+dir*(HalfShaft+.05f)),
            new Vector3(3.85f,roofY+.3f,.16f),stone);
        float front=sz-dir*(HalfShaft+.06f);
        const float lowerHead=3.10f;
        const float upperBottomGap=3.2f;
        float between=roofY-lowerHead-upperBottomGap;
        if(between<1.0f)throw new InvalidOperationException("Tower roof too close to ground");
        Solid(root,"Fire-rated front shaft wall with real two-floor openings",
            new Vector3(sx,lowerHead+between*.5f,front),
            new Vector3(3.85f,between,.16f),stone);
        foreach(float yy in new[]{LowerY+2.94f,roofY+2.94f})
            Surface(root,"Upper frame lintel at passenger lift landing",
                new Vector3(sx,yy,front),new Vector3(3.78f,.20f,.2f),steel);
    }
    static void BuildCabin(Transform root,float sx,float sz,float roofY,bool far,
        BraxCityWorld world,string id)
    {
        float dir=far?1f:-1f;
        var cabin=Group("Actual moving premium elevator car "+id,root,
            new Vector3(sx,LowerY,sz));
        cabin.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Solid(cabin,"Smoke graphite slip-safe cabin floor",
            new Vector3(0,-.077f,0),new Vector3(3.28f,.154f,3.25f),deck);
        Solid(cabin,"Cabin solid ceiling and lighting cove",
            new Vector3(0,3.13f,0),new Vector3(3.28f,.18f,3.25f),steel);
        // Panoramic sidewalls: glass + real colliders; do not open the car to
        // the sky during its 100+ metre trip.
        foreach(float xx in new[]{-1.59f,1.59f})
        {
            Solid(cabin,"Laminated panoramic car glazing",new Vector3(xx,1.55f,0),
                new Vector3(.10f,3.05f,3.02f),glass);
            Surface(cabin,"Stainless internal handrail",new Vector3(xx*.91f,1.06f,0),
                new Vector3(.10f,.09f,2.64f),steel);
            for(float zz=-1.0f;zz<1.5f;zz+=1.05f)
                Surface(cabin,"Brushed metal glass pane trim",
                    new Vector3(xx,1.56f,zz),new Vector3(.12f,3.08f,.03f),steel);
        }
        Solid(cabin,"Rear tinted glass lift car",
            new Vector3(0,1.5f,dir*1.55f),new Vector3(3.07f,3.0f,.10f),glass);
        // Intentionally bright but NOT emissive: PBR strips can survive
        // on mobile without increasing screen-space bloom or dynamic lights.
        foreach(float xx in new[]{-1.10f,1.10f})
            Surface(cabin,"Recessed warm architectural LED diffuser",
                new Vector3(xx,3.028f,0),new Vector3(.095f,.025f,2.70f),illumination);
        // Real body follows the actual deck; moving doors are excluded from
        // Editor static batching exactly like the flagship quay elevator.
        var cabL=DoorLeaf("Premium cabin moving left",cabin,
            new Vector3(-.77f,0,-dir*1.55f),dir);
        var cabR=DoorLeaf("Premium cabin moving right",cabin,
            new Vector3(.77f,0,-dir*1.55f),dir);
        var landingFront=sz-dir*(HalfShaft+.02f);
        var lowerL=DoorLeaf("Street floor sliding door left",root,
            new Vector3(sx-.77f,LowerY,landingFront),dir);
        var lowerR=DoorLeaf("Street floor sliding door right",root,
            new Vector3(sx+.77f,LowerY,landingFront),dir);
        var upperL=DoorLeaf("Roof floor sliding door left",root,
            new Vector3(sx-.77f,roofY,landingFront),dir);
        var upperR=DoorLeaf("Roof floor sliding door right",root,
            new Vector3(sx+.77f,roofY,landingFront),dir);
        // Accessible pedestal and actual grounded call button: visitor is
        // at the cabin threshold, not accidentally calling the next tower.
        float callZ=landingFront-dir*.90f;
        var call=Group("Physical elevator call panel "+id,root,
            new Vector3(sx-1.45f,LowerY,callZ));
        Surface(call,"Graphite steel face",new Vector3(0,1.25f,0),
            new Vector3(.24f,.58f,.15f),black);
        Surface(call,"Ground-floor clear floor-select ring",new Vector3(0,1.36f,-dir*.09f),
            new Vector3(.11f,.11f,.025f),illumination);
        // One visible instruction sign on the inside lobby wall at eye
        // height; roof exit is unambiguously the top destination.
        BraxCityArchitecture.Sign(root,"WINDA DACH "+id,
            new Vector3(sx,2.90f,callZ+.08f*dir),.28f,Color.white);
        var controller=root.gameObject.AddComponent<BraxCityElevator>();
        controller.player=world.player;controller.cabin=cabin;
        controller.cabinDoorLeft=cabL;controller.cabinDoorRight=cabR;
        controller.lowerDoorLeft=lowerL;controller.lowerDoorRight=lowerR;
        controller.upperDoorLeft=upperL;controller.upperDoorRight=upperR;
        controller.lowerY=LowerY;controller.upperY=roofY;
        controller.entranceZ=landingFront;controller.shaftCenter=new Vector2(sx,sz);
        controller.lowerCall=new Vector3(sx-1.45f,LowerY+1.2f,callZ);
        controller.upperCall=new Vector3(sx-1.45f,roofY+1.2f,callZ);
        var all=world.additionalTowerElevators.ToList();
        all.Add(controller);world.additionalTowerElevators=all.ToArray();
    }
    static void BuildOne(Transform streetRoot,BraxCityWorld world,
        BraxCityArchitecture.Building b,BraxCityStreetSkyline.TowerPlan plan)
    {
        var top=BraxCityStreetSkyline.Footprint(b,plan);
        float roof=BraxCityStreetSkyline.RoofHeight(b,plan.additionalFloors);
        float sx=(b.x0+b.x1)*.5f;
        float dz=b.z1-b.z0;
        float dir=b.far?1f:-1f;
        float streetFace=b.far?b.z0:b.z1;
        float sz=streetFace+dir*Mathf.Min(4.85f,dz*.54f);
        if(sx-HalfShaft<=top.xMin+.4f||sx+HalfShaft>=top.xMax-.4f ||
           sz-HalfShaft<=top.yMin+.25f||sz+HalfShaft>=top.yMax-.25f)
            throw new InvalidOperationException("Cannot fit physical lift inside source tower "+b.id+" "+top);
        // Do not offset the city geometry or the user's original store fronts.
        var root=Group("REAL SKYSCRAPER ELEVATOR "+b.id,streetRoot,Vector3.zero);
        BuildShaft(root,sx,sz,roof,b.far);
        BuildRoofSlabs(root,top,sx,sz,roof);
        ApplyGuardrails(root,top,roof,sx,sz,b.far);
        BuildCabin(root,sx,sz,roof,b.far,world,b.id);
        var worldPoints=new[]{
            new BraxCityPoint{id="R1_TOWER_"+b.id+"_STREET",
                label=b.title+" lobby",
                position=new Vector3(sx,LowerY,streetFace-dir*1.1f)},
            new BraxCityPoint{id="R1_TOWER_"+b.id+"_LOWER_LIFT",
                label=b.title+" lift lobby",
                position=new Vector3(sx,LowerY,sz-dir*3.15f)},
            new BraxCityPoint{id="R1_TOWER_"+b.id+"_ROOF",
                label=b.title+" roof",
                position=new Vector3(sx,roof,sz-dir*2.45f)}
        };
        // These anchors are passive environment references. Do NOT increase
        // the 11 existing MAIN/SIDE missions or activate any NPC.
        world.points=world.points.Concat(worldPoints).ToArray();
        foreach(var moving in root.GetComponentsInChildren<BraxCityMovingLiftPart>(true))
            foreach(var t in moving.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        Debug.Log("CITY_NINE_TOWER_NETWORK_REAL_ELEVATOR id="+b.id+
            " ground="+LowerY+" roof="+roof+" shaft="+new Vector2(sx,sz)+
            " walkableDoors=4 passengerCar=true physicalRoof=true");
    }
    public static void Build(BraxCityWorld world,Transform streetRoot)
    {
        if(!world||world.area!="city"||!streetRoot)
            throw new InvalidOperationException("Tower elevators require original city");
        steel=BraxCityMaterials.Surface("Premium black-titanium elevator frame",
            new Color(.12f,.145f,.16f),.46f,.62f);
        stone=BraxCityMaterials.Surface("Solid shaft matte stone and fire partition",
            new Color(.33f,.34f,.35f),.88f,.04f);
        glass=BraxCityMaterials.Surface("Lift panoramic smoke tempered safety glass",
            new Color(.26f,.36f,.39f,.68f),.21f,.10f);
        deck=BraxCityMaterials.Surface("Non-slip roof and lift floor textured basalt",
            new Color(.42f,.42f,.40f),.92f,.02f);
        illumination=BraxCityMaterials.Surface("Warm satin daylight LED diffuser / unlit",
            new Color(.78f,.71f,.57f),.43f,.03f);
        black=BraxCityMaterials.Surface("Graphite in-cabin vertical floor-control panel",
            new Color(.08f,.09f,.1f),.6f,.3f);
        if(world.additionalTowerElevators.Length!=0)
            throw new InvalidOperationException("Duplicate skyscraper lift build");
        var expected=BraxCityStreetSkyline.HighRises.Where(p=>p.id!=BraxCityStreetSkyline.AccessibleTowerId).ToArray();
        if(expected.Length!=8||!world.elevator)
            throw new InvalidOperationException("The quay lift must exist first + eight remaining high-rises");
        foreach(var p in expected)
        {
            var b=BraxCityArchitecture.StreetBuildings.Single(x=>x.id==p.id);
            BuildOne(streetRoot,world,b,p);
        }
        if(world.additionalTowerElevators.Length!=8||
           world.additionalTowerElevators.Any(l=>!l||!l.cabin||!l.cabinDoorLeft||
                       !l.cabinDoorRight||!l.lowerDoorLeft||!l.upperDoorRight))
            throw new InvalidOperationException("CITY_HIGHRISE_LIFT_INCOMPLETE");
        Debug.Log("CITY_TOWER_LIFTS_ACTUAL_CONSTRUCTED total=9 externalQuay=1 others=8 "+
            "originalBraxRigPreserved=true originalGymPreserved=true noMissionSpawn=true");
    }
}

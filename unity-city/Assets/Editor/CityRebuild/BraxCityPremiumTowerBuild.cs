using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor-only, self-contained tower fit-out on an existing authored frontage.
// Does not alter shared lighting, Brax, camera, controls, NOVA, nutrition or gym.
public static class BraxCityPremiumTowerBuild
{
    static Material stone,steel,glass,ceiling,deck,plant;
    static void Solid(Transform root,string name,Vector3 at,Vector3 size,Material mat)
        =>BraxCityArchitecture.Box(name,root,at,size,mat,true);
    static void Deco(Transform root,string name,Vector3 at,Vector3 size,Material mat)
        =>BraxCityArchitecture.Box(name,root,at,size,mat,false);
    static Transform Group(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    static Transform SlidingLeaf(Transform root,string label,Vector3 at,float width)
    {
        var leaf=Group(label,root,at);
        leaf.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Solid(leaf,"Brushed aluminium glass elevator door",new Vector3(0,1.42f,0),
            new Vector3(width,2.84f,.085f),steel);
        Deco(leaf,"Vertical inset glass door insert",new Vector3(0,1.42f,-.05f),
            new Vector3(width-.2f,2.46f,.015f),glass);
        return leaf;
    }
    static BraxCityDoor BuildStreetDoor(Transform root,float x,float streetZ)
    {
        var doorRoot=Group("BRAXHORN TOWER physical entrance",root,new Vector3(x,.15f,streetZ));
        var left=Group("Street entrance left hinge",doorRoot,new Vector3(-1.05f,0,0));
        var right=Group("Street entrance right hinge",doorRoot,new Vector3(1.05f,0,0));
        Solid(left,"Left tempered-glass door",new Vector3(.50f,1.43f,0),
            new Vector3(1.0f,2.86f,.07f),glass);
        Solid(right,"Right tempered-glass door",new Vector3(-.50f,1.43f,0),
            new Vector3(1.0f,2.86f,.07f),glass);
        foreach(float edge in new[]{-.43f,.43f})
        {
            Deco(left,"Door steel stile",new Vector3(.50f+edge,1.44f,-.055f),new Vector3(.035f,2.85f,.045f),steel);
            Deco(right,"Door steel stile",new Vector3(-.50f+edge,1.44f,-.055f),new Vector3(.035f,2.85f,.045f),steel);
        }
        Deco(left,"Brushed steel door handle",new Vector3(.91f,1.35f,-.1f),new Vector3(.035f,.5f,.11f),steel);
        Deco(right,"Brushed steel door handle",new Vector3(-.91f,1.35f,-.1f),new Vector3(.035f,.5f,.11f),steel);
        var door=doorRoot.gameObject.AddComponent<BraxCityDoor>();
        door.id="R1_TOWER_DOOR";door.label="BRAXHORN TOWER";
        door.leaves=new[]{left,right};door.sides=new[]{1f,-1f};
        return door;
    }
    static void RoofBarrier(Transform parent,float x0,float x1,float z0,float z1,float y)
    {
        const float h=1.48f;
        Solid(parent,"West roof parapet",new Vector3(x0+.22f,y+h*.5f,(z0+z1)*.5f),
            new Vector3(.44f,h,z1-z0),stone);
        Solid(parent,"East roof parapet",new Vector3(x1-.22f,y+h*.5f,(z0+z1)*.5f),
            new Vector3(.44f,h,z1-z0),stone);
        Solid(parent,"South roof parapet",new Vector3((x0+x1)*.5f,y+h*.5f,z0+.22f),
            new Vector3(x1-x0,h,.44f),stone);
        Solid(parent,"North roof parapet",new Vector3((x0+x1)*.5f,y+h*.5f,z1-.22f),
            new Vector3(x1-x0,h,.44f),stone);
        for(float z=z0+1.5f;z<z1-1.0f;z+=2.5f)
        {
            Deco(parent,"Roof handrail west",new Vector3(x0+.52f,y+1.4f,z),new Vector3(.08f,.09f,2.55f),steel);
            Deco(parent,"Roof handrail east",new Vector3(x1-.52f,y+1.4f,z),new Vector3(.08f,.09f,2.55f),steel);
        }
    }
    static void Mechanical(Transform root,float x,float y,float z)
    {
        Solid(root,"Rooftop HVAC platform",new Vector3(x,y+.15f,z),new Vector3(2.6f,.3f,2.1f),stone);
        Solid(root,"HVAC weatherproof housing",new Vector3(x,y+.92f,z),new Vector3(2.2f,1.22f,1.65f),steel);
        for(int i=0;i<8;i++)
            Deco(root,"HVAC ventilation grille",new Vector3(x-1.115f+i*.32f,y+.90f,z-.84f),
                new Vector3(.025f,.68f,.038f),plant);
        Deco(root,"HVAC roof exhaust",new Vector3(x+.62f,y+1.73f,z+.25f),
            new Vector3(.5f,.43f,.55f),plant);
    }
    public static void Build(BraxCityWorld world,Transform streetRoot)
    {
        if(!world||world.area!="city")throw new InvalidOperationException("Premium tower must belong to the original city");
        var b=BraxCityArchitecture.StreetBuildings.Single(x=>x.id==BraxCityStreetSkyline.AccessibleTowerId);
        var plan=BraxCityStreetSkyline.HighRises.Single(x=>x.id==b.id);
        if(!b.far||Mathf.Abs(b.z0-36.1f)>.01f)
            throw new InvalidOperationException("Tower street entrance no longer matches the audited frontage");
        float x=(b.x0+b.x1)*.5f; // exact modular storefront centre = x59
        float shaftZ=53f;
        float roofY=BraxCityStreetSkyline.RoofHeight(b,plan.additionalFloors);
        float groundY=.15f;
        float s0=shaftZ-1.72f,s1=shaftZ+1.72f;
        float l=x-1.74f,r=x+1.74f;
        stone=BraxCityMaterials.Surface("Tower lobby warm natural stone",new Color(.57f,.56f,.53f),.82f);
        steel=BraxCityMaterials.Surface("Tower lift charcoal steel",new Color(.19f,.21f,.22f),.49f,.38f);
        glass=BraxCityMaterials.Surface("Tower lobby clear smoked safety glazing",new Color(.37f,.47f,.48f,.46f),.19f,.13f);
        ceiling=BraxCityMaterials.Surface("Lobby matte ceiling",new Color(.77f,.76f,.72f),.95f);
        deck=BraxCityMaterials.Surface("Roof nonslip terrazzo",new Color(.47f,.48f,.46f),.91f);
        plant=BraxCityMaterials.Surface("Technical galvanized metal",new Color(.33f,.35f,.35f),.75f,.55f);
        var root=Group("R1_TOWER accessible premium lobby lift and roof",streetRoot,Vector3.zero);

        // Physical ground-floor shell. The exterior facade and its original
        // 2-metric door aperture are rendered separately by the source kit.
        Solid(root,"Walkable natural stone lobby floor",
            new Vector3(x,.06f,(b.z0+b.z1)*.5f),
            new Vector3(b.x1-b.x0-.3f,.18f,b.z1-b.z0-.25f),stone);
        float groundCenter=(groundY+3.70f)*.5f,gh=3.70f-groundY;
        Solid(root,"Ground-floor west exterior wall",new Vector3(b.x0+.15f,groundCenter,(b.z0+b.z1)*.5f),
            new Vector3(.3f,gh,b.z1-b.z0),stone);
        Solid(root,"Ground-floor east exterior wall",new Vector3(b.x1-.15f,groundCenter,(b.z0+b.z1)*.5f),
            new Vector3(.3f,gh,b.z1-b.z0),stone);
        Solid(root,"Ground-floor rear wall",new Vector3(x,groundCenter,b.z1-.15f),
            new Vector3(b.x1-b.x0,gh,.3f),stone);
        float entryHalf=1.06f;
        Solid(root,"Entrance wall west of real door",new Vector3((b.x0+x-entryHalf)*.5f,groundCenter,b.z0+.12f),
            new Vector3(x-entryHalf-b.x0,gh,.24f),stone);
        Solid(root,"Entrance wall east of real door",new Vector3((x+entryHalf+b.x1)*.5f,groundCenter,b.z0+.12f),
            new Vector3(b.x1-x-entryHalf,gh,.24f),stone);
        // No full-box collider! Four upper partitions surround a true 3.48m
        // clear shaft, unlike the predecessor's blocked future premises.
        float upperBottom=3.72f, upperHeight=b.Height-upperBottom;
        if(upperHeight<6)throw new InvalidOperationException("Accessible tower base has insufficient height");
        float cy=(upperBottom+b.Height)*.5f;
        Solid(root,"Occupied floors west of shaft",new Vector3((b.x0+l)*.5f,cy,(b.z0+b.z1)*.5f),
            new Vector3(l-b.x0,upperHeight,b.z1-b.z0),ceiling);
        Solid(root,"Occupied floors east of shaft",new Vector3((r+b.x1)*.5f,cy,(b.z0+b.z1)*.5f),
            new Vector3(b.x1-r,upperHeight,b.z1-b.z0),ceiling);
        Solid(root,"Occupied floors south of shaft",new Vector3(x,cy,(b.z0+s0)*.5f),
            new Vector3(r-l,upperHeight,s0-b.z0),ceiling);
        Solid(root,"Occupied floors north of shaft",new Vector3(x,cy,(s1+b.z1)*.5f),
            new Vector3(r-l,upperHeight,b.z1-s1),ceiling);
        var streetDoor=BuildStreetDoor(root,x,b.z0-.06f);
        world.doors=world.doors.Concat(new[]{streetDoor}).ToArray();
        BraxCityArchitecture.Sign(root,"BRAXHORN TOWER",new Vector3(x,3.40f,b.z0-.17f),.32f,Color.white);
        // Deliberately keep the corridor from street door (Z36.1) to the lift
        // at Z51.28 free. Reception and seating stay outside that corridor.
        Solid(root,"Lobby security reception",new Vector3(b.x0+2.1f,.78f,b.z0+5.5f),
            new Vector3(3.0f,1.25f,1.05f),stone);
        Deco(root,"Lobby desk steel kickplate",new Vector3(b.x0+2.1f,.35f,b.z0+4.96f),
            new Vector3(2.8f,.22f,.06f),steel);
        Deco(root,"Reception overhead canopy",new Vector3(b.x0+2.2f,3.22f,b.z0+5.5f),
            new Vector3(3.7f,.18f,1.7f),steel);
        foreach(float dz in new[]{4.9f,7.6f,10.3f})
        {
            Deco(root,"Polished lobby decorative pilaster",new Vector3(b.x1-.34f,1.85f,b.z0+dz),
                new Vector3(.12f,3.15f,.3f),steel);
        }

        // A physical shaft with openings only at the two authorized landings.
        float shaftBottom=groundY+3.1f,shaftTop=roofY-3.10f;
        Solid(root,"Lift shaft west fire-rated wall",new Vector3(l-.1f,roofY*.5f,shaftZ),
            new Vector3(.2f,roofY,3.88f),stone);
        Solid(root,"Lift shaft east fire-rated wall",new Vector3(r+.1f,roofY*.5f,shaftZ),
            new Vector3(.2f,roofY,3.88f),stone);
        Solid(root,"Lift shaft rear fire-rated wall",new Vector3(x,roofY*.5f,s1+.08f),
            new Vector3(3.84f,roofY,.18f),stone);
        if(shaftTop>shaftBottom)
            Solid(root,"Lift shaft front between landings",new Vector3(x,(shaftTop+shaftBottom)*.5f,s0-.1f),
                new Vector3(3.84f,shaftTop-shaftBottom,.19f),stone);
        // Structural lintels above actual door openings.
        foreach(float baseY in new[]{groundY,roofY})
            Solid(root,"Lift landing doorway head",new Vector3(x,baseY+3.02f,s0-.12f),
                new Vector3(3.8f,.28f,.2f),steel);

        // Four roof slabs leave a literal 3.48 × 3.44 m lift opening.
        float floorCenter=roofY-.14f;
        Solid(root,"Terrace roof deck west",new Vector3((b.x0+l)*.5f,floorCenter,(b.z0+b.z1)*.5f),
            new Vector3(l-b.x0,.28f,b.z1-b.z0),deck);
        Solid(root,"Terrace roof deck east",new Vector3((r+b.x1)*.5f,floorCenter,(b.z0+b.z1)*.5f),
            new Vector3(b.x1-r,.28f,b.z1-b.z0),deck);
        Solid(root,"Terrace roof deck south",new Vector3(x,floorCenter,(b.z0+s0)*.5f),
            new Vector3(r-l,.28f,s0-b.z0),deck);
        Solid(root,"Terrace roof deck north",new Vector3(x,floorCenter,(s1+b.z1)*.5f),
            new Vector3(r-l,.28f,b.z1-s1),deck);
        RoofBarrier(root,b.x0,b.x1,b.z0,b.z1,roofY);
        Mechanical(root,b.x0+2.0f,roofY,b.z1-3.25f);
        Mechanical(root,b.x1-2.0f,roofY,b.z1-3.25f);
        // Roof duel and two contacts have separate non-overlapping positions.
        // These are generated architectural anchors, not active quest triggers.
        var points=new[]{
            new BraxCityPoint{id="R1_TOWER_STREET_ENTRY",label="Tower street",position=new Vector3(x,.15f,b.z0-1.7f)},
            new BraxCityPoint{id="R1_TOWER_LOBBY",label="Tower lobby",position=new Vector3(x,.15f,b.z0+4f)},
            new BraxCityPoint{id="R1_TOWER_LIFT_LOWER",label="Lift lower",position=new Vector3(x,.15f,shaftZ)},
            new BraxCityPoint{id="R1_TOWER_LIFT_UPPER",label="Lift upper",position=new Vector3(x,roofY,shaftZ)},
            new BraxCityPoint{id="R1_TOWER_ROOF_ENTRY",label="Roof entrance",position=new Vector3(x,roofY,s0-1.8f)},
            new BraxCityPoint{id="R1_TOWER_MEETING",label="Roof contacts",position=new Vector3(b.x0+3.4f,roofY,b.z0+7f)},
            new BraxCityPoint{id="R1_ROOF_DUEL",label="Future one vs one",position=new Vector3(x,roofY,b.z0+7.3f)},
            new BraxCityPoint{id="R1_TOWER_RETURN",label="Return to lift",position=new Vector3(x,roofY,s0-1.1f)}
        };
        world.points=world.points.Concat(points).ToArray();

        var cabin=Group("Moving physical elevator cabin",root,new Vector3(x,groundY,shaftZ));
        cabin.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Solid(cabin,"Lift floor with collision",new Vector3(0,-.08f,0),new Vector3(3.20f,.16f,3.18f),steel);
        Solid(cabin,"Lift ceiling",new Vector3(0,3.15f,0),new Vector3(3.2f,.18f,3.18f),steel);
        foreach(float xx in new[]{-1.49f,1.49f})
        {
            Solid(cabin,"Lift lateral glass panel",new Vector3(xx,1.5f,0),
                new Vector3(.12f,3f,3.0f),glass);
            Deco(cabin,"Lift stainless handrail",new Vector3(xx*.92f,1.07f,0),
                new Vector3(.12f,.1f,2.75f),steel);
        }
        Solid(cabin,"Lift rear panel",new Vector3(0,1.5f,1.54f),new Vector3(2.95f,3f,.1f),glass);
        var cabLeft=SlidingLeaf(cabin,"Cabin door left",new Vector3(-.77f,0,-1.54f),1.48f);
        var cabRight=SlidingLeaf(cabin,"Cabin door right",new Vector3(.77f,0,-1.54f),1.48f);
        var lowerLeft=SlidingLeaf(root,"Lower landing door left",new Vector3(x-.77f,groundY,s0-.10f),1.48f);
        var lowerRight=SlidingLeaf(root,"Lower landing door right",new Vector3(x+.77f,groundY,s0-.10f),1.48f);
        var upperLeft=SlidingLeaf(root,"Roof landing door left",new Vector3(x-.77f,roofY,s0-.10f),1.48f);
        var upperRight=SlidingLeaf(root,"Roof landing door right",new Vector3(x+.77f,roofY,s0-.10f),1.48f);
        var elevator=root.gameObject.AddComponent<BraxCityElevator>();
        elevator.player=world.player;
        elevator.cabin=cabin;
        elevator.cabinDoorLeft=cabLeft; elevator.cabinDoorRight=cabRight;
        elevator.lowerDoorLeft=lowerLeft;elevator.lowerDoorRight=lowerRight;
        elevator.upperDoorLeft=upperLeft;elevator.upperDoorRight=upperRight;
        elevator.lowerCall=new Vector3(x-2.2f,groundY+1.2f,s0-1.25f);
        elevator.upperCall=new Vector3(x-2.2f,roofY+1.2f,s0-1.25f);
        elevator.lowerY=groundY;elevator.upperY=roofY;
        elevator.entranceZ=s0; elevator.shaftCenter=new Vector2(x,shaftZ);
        world.elevator=elevator;
        // Asset meshes are static by default in the source authoring helper.
        // Explicitly clear all static flags for real moving carriage parts:
        // otherwise Unity can freeze doors/cabin into static batches.
        foreach(var marker in root.GetComponentsInChildren<BraxCityMovingLiftPart>(true))
            foreach(var t in marker.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
        Debug.Log("CITY_TOWER_ARCHITECTURE_CREATED floor="+groundY+" roof="+roofY+
            " authored-roof-points=8 source-footprint="+b.id+
            " / Unity physical traversal and safety acceptance pending");
    }
}

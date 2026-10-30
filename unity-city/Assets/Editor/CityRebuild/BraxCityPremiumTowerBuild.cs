using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editor-only, self-contained tower fit-out on an existing authored frontage.
// Does not alter shared lighting, Brax, camera, controls, NOVA, nutrition or gym.
public static class BraxCityPremiumTowerBuild
{
    static Material stone,steel,glass,ceiling,deck,plant,lobbyFloor,led;
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
        Solid(leaf,"Full-height moving tempered safety glazing",new Vector3(0,1.42f,0),
            new Vector3(width,2.84f,.085f),glass);
        foreach(float x in new[]{-width*.47f,width*.47f})
            Deco(leaf,"Slim charcoal stainless vertical door stile",
                new Vector3(x,1.42f,-.05f),new Vector3(.045f,2.84f,.08f),steel);
        Deco(leaf,"Premium brushed lower door rail",new Vector3(0,.095f,-.05f),
            new Vector3(width,.115f,.08f),steel);
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
    static void RoofShaftSafety(Transform root,float left,float right,float south,float north,float roofY)
    {
        // At roof level the elevator shaft is a *real opening*, not a fake
        // surface. Guard all three open perimeter sides even while the cabin
        // is at street level. The fourth (south) side is controlled by the
        // physical sliding upper landing doors and their collision volumes.
        const float h=1.44f;
        float cy=roofY+h*.5f,cz=(south+north)*.5f;
        Solid(root,"ROOF LIFT SAFETY west laminated glass barrier",
            new Vector3(left-.07f,cy,cz),new Vector3(.14f,h,north-south+.3f),glass);
        Solid(root,"ROOF LIFT SAFETY east laminated glass barrier",
            new Vector3(right+.07f,cy,cz),new Vector3(.14f,h,north-south+.3f),glass);
        Solid(root,"ROOF LIFT SAFETY north laminated glass barrier",
            new Vector3((left+right)*.5f,cy,north+.07f),
            new Vector3(right-left+.32f,h,.14f),glass);
        foreach(float xx in new[]{left-.08f,right+.08f})
        {
            Deco(root,"Lift guard stainless upper rail",
                new Vector3(xx,roofY+h,cz),new Vector3(.075f,.09f,north-south+.35f),steel);
        }
        Deco(root,"Lift guard north rail",
            new Vector3((left+right)*.5f,roofY+h,north+.08f),
            new Vector3(right-left+.38f,.09f,.08f),steel);
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
        plant=BraxCityMaterials.Surface("Technical galvanized metal",new Color(.33f,.35f,.35f),.75f,.55f);
        led=BraxCityMaterials.Surface("Premium elevator and lobby satin LED diffuser / no bloom",
            new Color(.80f,.77f,.67f),.40f,.04f);
        // Reuse the same audited 2K PBR stone library as the rebuilt pavements
        // (sRGB albedo, linear normal & AO/roughness/metallic ARM). The original
        // untextured blue-looking floor is not acceptable for a flagship lobby.
        const string tile="Assets/Source/CityRebuild/textures/concrete_pavement_02/";
        var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(tile+"diffuse.jpg");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(tile+"normal.png")??
                   AssetDatabase.LoadAssetAtPath<Texture2D>(tile+"normal.jpg");
        var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(tile+"arm.jpg");
        if(!albedo||!normal||!arm)
            throw new InvalidOperationException("Premium tower stone PBR library is incomplete");
        lobbyFloor=BraxCityMaterials.Textured("Tower lobby actual PBR slate stone",
            albedo,normal,arm,1f/1.65f);
        lobbyFloor.SetColor("_Color",new Color(.69f,.69f,.66f));
        lobbyFloor.SetFloat("_BumpScale",1.10f);
        EditorUtility.SetDirty(lobbyFloor);
        deck=BraxCityMaterials.Textured("Observation terrace library stone paving",
            albedo,normal,arm,1f/1.65f);
        deck.SetColor("_Color",new Color(.82f,.81f,.78f));
        deck.SetFloat("_BumpScale",.65f);
        deck.SetFloat("_Metallic",0f);
        EditorUtility.SetDirty(deck);
        var root=Group("R1_TOWER accessible premium lobby lift and roof",streetRoot,Vector3.zero);

        // Physical ground-floor shell. The exterior facade and its original
        // 2-metric door aperture are rendered separately by the source kit.
        Solid(root,"Walkable imported PBR stone lobby floor",
            new Vector3(x,.06f,(b.z0+b.z1)*.5f),
            new Vector3(b.x1-b.x0-.3f,.18f,b.z1-b.z0-.25f),lobbyFloor);
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
        // The same title is already rendered by the sourced street façade.
        // Do not overlay two almost-coplanar text meshes (flickering/z-fight).
        // The one BRAXHORN TOWER entry label stays clearly visible.
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
        // The terrace and its side faces share the same audited source PBR
        // stone texture. Keep tiles above the existing structural slab without
        // adding another overlapping walkable collider.
        var tileGrout=BraxCityMaterials.Surface("Tower warm limestone grout",
            new Color(.33f,.34f,.32f),.94f);
        for(float z=b.z0+.65f;z<b.z1-.65f;z+=2.0f)
        {
            Deco(root,"Roof terrace west tiled skirting",
                new Vector3(b.x0+.48f,roofY+.20f,z),new Vector3(.025f,.38f,1.94f),deck);
            Deco(root,"Roof terrace east tiled skirting",
                new Vector3(b.x1-.48f,roofY+.20f,z),new Vector3(.025f,.38f,1.94f),deck);
            Deco(root,"Roof parapet west tile joint",
                new Vector3(b.x0+.496f,roofY+.20f,z+.98f),new Vector3(.029f,.38f,.025f),tileGrout);
            Deco(root,"Roof parapet east tile joint",
                new Vector3(b.x1-.496f,roofY+.20f,z+.98f),new Vector3(.029f,.38f,.025f),tileGrout);
        }
        for(float xx=b.x0+.75f;xx<b.x1-.75f;xx+=2.0f)
        {
            Deco(root,"Roof terrace south tiled skirting",
                new Vector3(xx,roofY+.20f,b.z0+.48f),new Vector3(1.94f,.38f,.025f),deck);
            Deco(root,"Roof terrace north tiled skirting",
                new Vector3(xx,roofY+.20f,b.z1-.48f),new Vector3(1.94f,.38f,.025f),deck);
        }
        // Entrance lobby: warm painted walls, true stone flooring, stainless
        // lower skirting and architectural lighting (not emissive neon).
        var lobbyPaint=BraxCityMaterials.Surface("Tower lobby warm painted wall finish",
            new Color(.78f,.77f,.73f),.96f);
        foreach(float xx in new[]{b.x0+.31f,b.x1-.31f})
        {
            Deco(root,"Tower painted lobby interior wall",
                new Vector3(xx,1.92f,(b.z0+b.z1)*.5f),
                new Vector3(.025f,3.10f,b.z1-b.z0-.65f),lobbyPaint);
            Deco(root,"Tower lobby stainless skirting",
                new Vector3(xx,.24f,(b.z0+b.z1)*.5f),
                new Vector3(.035f,.17f,b.z1-b.z0-.7f),steel);
        }
        Deco(root,"Tower lobby painted rear wall",
            new Vector3(x,1.92f,b.z1-.31f),
            new Vector3(b.x1-b.x0-.7f,3.10f,.025f),lobbyPaint);
        for(float zz=b.z0+3f;zz<b.z1-2f;zz+=5f)
            Deco(root,"Tower lobby satin ceiling lighting recess",
                new Vector3(x,3.58f,zz),new Vector3(4.5f,.035f,.19f),led);
        RoofBarrier(root,b.x0,b.x1,b.z0,b.z1,roofY);
        RoofShaftSafety(root,l,r,s0,s1,roofY);
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
            // Contact group is north of the shaft, separate from the clear
            // 10 x 9 metre 1v1 area south of it. Nothing spawns until mission
            // implementation is explicitly approved by the owner.
            new BraxCityPoint{id="R1_TOWER_MEETING",label="Roof contacts",position=new Vector3(x-1.2f,roofY,b.z1-2.4f)},
            new BraxCityPoint{id="R1_ROOF_DUEL",label="Future one vs one",position=new Vector3(x,roofY,b.z0+7.4f)},
            new BraxCityPoint{id="R1_TOWER_RETURN",label="Return to lift",position=new Vector3(x,roofY,s0-1.1f)},
            // MASTER V3 includes original reference IDs and newer R1_TOWER
            // aliases. Both remain hidden technical points, not extra quests.
            new BraxCityPoint{id="R1_TOWER_LIFT_CABIN",label="Cabin checkpoint",position=new Vector3(x,groundY,shaftZ)},
            new BraxCityPoint{id="R1_ELEVATOR_0",label="Elevator ground floor",position=new Vector3(x,groundY,shaftZ)},
            new BraxCityPoint{id="R1_ELEVATOR_TOP",label="Elevator roof",position=new Vector3(x,roofY,shaftZ)},
            new BraxCityPoint{id="R1_ROOF_ENTRY",label="Roof entry",position=new Vector3(x,roofY,s0-1.8f)},
            new BraxCityPoint{id="R1_ROOF_MEETING",label="Roof contacts",position=new Vector3(x-1.2f,roofY,b.z1-2.4f)},
            new BraxCityPoint{id="R1_ROOF_CAMERA",label="Rooftop camera zone",position=new Vector3(x,roofY+2.8f,b.z0+11.2f)},
            new BraxCityPoint{id="R1_ROOF_RETURN",label="Roof return",position=new Vector3(x,roofY,s0-1.1f)}
        };
        if(world.points.Select(p=>p.id).Intersect(points.Select(p=>p.id)).Any() ||
            points.GroupBy(p=>p.id).Any(g=>g.Count()!=1))
            throw new InvalidOperationException("MASTER V3 tower infrastructure point ID collides with source reservation");
        world.points=world.points.Concat(points).ToArray();

        var cabin=Group("Moving physical elevator cabin",root,new Vector3(x,groundY,shaftZ));
        cabin.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Solid(cabin,"Genuine PBR basalt passenger floor",new Vector3(0,-.08f,0),
            new Vector3(3.20f,.16f,3.18f),lobbyFloor);
        Solid(cabin,"Lift satin graphite ceiling",new Vector3(0,3.15f,0),
            new Vector3(3.2f,.18f,3.18f),steel);
        foreach(float line in new[]{-.98f,.98f})
            Deco(cabin,"Warm recessed linear ceiling LED diffuser",
                new Vector3(line,3.04f,0),new Vector3(.09f,.03f,2.67f),led);
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
        Deco(root,"REAL passenger elevator ground-floor wall panel",
            new Vector3(x-2.2f,1.35f,s0-1.25f),
            new Vector3(.27f,.62f,.12f),steel);
        Deco(root,"Ground floor lift button / no fake teleport",
            new Vector3(x-2.2f,1.37f,s0-1.34f),
            new Vector3(.11f,.11f,.045f),led);
        Deco(root,"Real rooftop passenger elevator exit floor panel",
            new Vector3(x-2.2f,roofY+1.35f,s0-1.25f),
            new Vector3(.27f,.62f,.12f),steel);
        Deco(root,"Roof lift return button",
            new Vector3(x-2.2f,roofY+1.37f,s0-1.34f),
            new Vector3(.11f,.11f,.045f),led);
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
            " authored-roof-points="+points.Length+" source-footprint="+b.id+
            " / Unity physical traversal and safety acceptance pending");
    }
}


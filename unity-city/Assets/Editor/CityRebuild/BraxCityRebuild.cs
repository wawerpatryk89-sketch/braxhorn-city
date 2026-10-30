using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxCityRebuild
{
    // User-authorized rebuild of all 18 source footprints, both frontages.
    // Build succeeds only after real sources import; visual/phone acceptance
    // is never inferred from this switch or from a green workflow.
    public const bool WholeDistrict=true;
    const string Output="Assets/PlayableGenerated/CityRebuild";
    static readonly List<string> replaced=new List<string>();
    [Serializable] sealed class ReviewReport
    {
        public string stage,sourceCommit="ba678928b2dcb2dff9e38bdb5306e090e76360cf",cityCommit,gymIntegration="Source gym preserved; other contractor final commit not yet supplied";
        public int renderedMeshes,triangles,materials,missionReservations=11;
        public int plannedStreetBuildings=18,nearStreetBuildings=9,farStreetBuildings=9,streetHighRises=9;
        public int actuallyBuiltStreetBuildings,actuallyBuiltHighRises,streetMainRoadLengthMetres;
        public string[] actualBuildingIds;
        public bool sourceAssetsRetained=true,phoneAccepted=false,visualAccepted=false;
        public int vehicleSourceRenderersHidden;
        public bool physicalElevatorInstalled,physicalElevatorTested=false;
        public string[] replacedSourceRenderers;
        public string[] remaining={"Review full player route and mobile device performance","Integrate the specified finished gym commit","Review and replace remaining original vegetation where needed"};
    }
    static ReviewReport report;

    static Bounds Box(float x0,float x1,float y0,float y1,float z0,float z1)=>new Bounds(new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0));
    static void Disable(Transform original,IEnumerable<int> ids)
    {
        var set=new HashSet<string>(ids.Select(i=>"CityMesh_"+i));
        foreach(var t in original.GetComponentsInChildren<Transform>(true))
            if(set.Contains(t.name))foreach(var renderer in t.GetComponentsInChildren<MeshRenderer>(true))
            {if(renderer.enabled){renderer.enabled=false;replaced.Add(t.name);}}
    }
    // The complete original city export was inspected, including all 724
    // mesh accessor world bounds and manifest collision flags. Exactly 397
    // vehicle submeshes (cars, accessories and motorcycles) occupy indices
    // 271..667. Their combined world bounds in original source coordinates are
    // x [-33.2,87.46], y [0,2.09], z [23.82,33.26].
    // Their source collision flags are all false. This is a SOURCE-PINNED
    // exclusion; never hide arbitrary objects by a broad car-name heuristic.
    const int VehicleFirst=271,VehicleCount=397;
    static int HideAuditedVehicles(Transform original)
    {
        var indexed=original.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith("CityMesh_",StringComparison.Ordinal))
            .ToDictionary(t=>t.name,t=>t);
        for(int i=VehicleFirst;i<VehicleFirst+VehicleCount;i++)
        {
            Transform t;
            if(!indexed.TryGetValue("CityMesh_"+i,out t))
                throw new InvalidOperationException("Pinned source vehicle renderer missing: CityMesh_"+i);
            foreach(var renderer in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                var b=renderer.bounds;
                if(b.min.y<-.25f||b.max.y>2.35f||b.min.z<23.65f||b.max.z>33.45f)
                    throw new InvalidOperationException("Vehicle-only geometry contract changed: "+t.name+" "+b);
            }
            if(t.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))
                throw new InvalidOperationException("Audited vehicle collider contract changed: "+t.name);
        }
        Disable(original,Enumerable.Range(VehicleFirst,VehicleCount));
        int stillVisible=0;
        for(int i=VehicleFirst;i<VehicleFirst+VehicleCount;i++)
            if(indexed["CityMesh_"+i].GetComponentsInChildren<MeshRenderer>(true).Any(x=>x.enabled))
                stillVisible++;
        if(stillVisible!=0)throw new InvalidOperationException("Some original cars are still visible: "+stillVisible);
        Debug.Log("CITY_EMPTY_STREET_SOURCE_VERIFIED vehicles_hidden="+VehicleCount+
            " no static car meshes remain in original city source");
        return VehicleCount;
    }

    static void Cut(Transform original,Transform physics,Bounds volume,string id,bool removeCollision=true)
    {
        BraxCityMeshCut.Subtract(original,volume,Output+"/Cuts",id);
        if(removeCollision)BraxCityMeshCut.SubtractBoxColliders(physics,volume);
    }
    public static void Apply(BraxCityWorld world,Transform original,Transform physics)
    {
        if(!world||world.area!="city")throw new InvalidOperationException("Renovation must run on the actual city source");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();replaced.Clear();
        BraxCityArtwork.Prepare();
        int hiddenVehicleMeshes=HideAuditedVehicles(original);
        var gymDoor=GameObject.Find("Original opening door");
        var gymDoorPosition=gymDoor?gymDoor.transform.position:Vector3.zero;
        var gymDoorRotation=gymDoor?gymDoor.transform.rotation:Quaternion.identity;
        var gymDoorScale=gymDoor?gymDoor.transform.lossyScale:Vector3.one;
        var root=new GameObject("BRAXHORN CITY reference renovation").transform;
        BraxCityArchitecture.Prepare();
        if(WholeDistrict)BraxCityStreetSkyline.ValidateStreetFootprints();
        // Near/far sidewalks, asphalt and kerbs are authored as spatial pieces;
        // the original source collision stays until the exact crossing cuts.
        Disable(original,Enumerable.Range(0,6));
        Cut(original,physics,Box(-104,66,-.04f,.045f,24,33),"old_road_surface",false);
        BraxCityGround.Street(root);
        var selected=WholeDistrict?BraxCityArchitecture.StreetBuildings:BraxCityArchitecture.StreetBuildings.Take(2).ToArray();
        foreach(var b in selected)
        {
            Cut(original,physics,Box(b.x0+.005f,b.x1-.005f,.18f,55,b.z0-.01f,b.z1+.015f),b.id+"_building");
            // Shared decoration batches are clipped only in the front volume.
            if(b.far)Cut(original,physics,Box(b.x0+.005f,b.x1-.005f,.3f,55,b.z0-.9f,b.z0+.02f),b.id+"_front",false);
            else Cut(original,physics,Box(b.x0+.005f,b.x1-.005f,.3f,55,b.z1-.02f,b.z1+1.4f),b.id+"_front",false);
            var authoredBuilding=BraxCityArchitecture.Build(root,b);
            // The 17 source-premises door leaves were previously decorative
            // and their underlying full-building colliders trapped Brax.
            // Each independently-hinged glass entrance is now a real usable
            // BraxCityDoor. The quay building uses the existing signature
            // premium double door authored later in the tower builder.
            var entrance=BraxCityArchitecture.BuildStreetAccessDoor(authoredBuilding,b);
            if(entrance!=null)
                world.doors=world.doors.Concat(new[]{entrance}).ToArray();
        }
        // A set-back tower above the representative block provides the high
        // skyline requested by the user. The lower detailed street floors stay
        // at their original physical scale and retain distinct premises.
        if(!WholeDistrict)
            BraxCityArchitecture.Tower(root,"central setback",new Rect(28,-3.2f,22,22.8f),23.7f,22);
        if(WholeDistrict)
        {
            Cut(original,physics,Box(14.5f,26,5.1f,30,11.6f,20.63f),"shop_upper",false);
            BraxCityArchitecture.UpperExtension(root,"BRAXHORN NUTRITION",14.5f,26,11.6f,20.6f,5.1f,6);
            // NOVA interior/cellar (including their doors and stairs) remain
            // untouched. Only geometry above its existing ceiling is replaced.
            Cut(original,physics,Box(32,52,5.1f,30,36.08f,62),"nova_upper",false);
            BraxCityArchitecture.UpperExtension(root,"NOVA",32,52,36.1f,62,5.1f,5);
            // The 4.03 m clear gateway below remains the original open passage.
            Cut(original,physics,Box(2,18,4.6f,40,36.08f,48),"courtyard_gateway_upper",false);
            BraxCityArchitecture.UpperExtension(root,"Courtyard gateway",2,18,36.1f,48,4.6f,6);
            Disable(original,BraxCityIndustrial.WholeSourceRendererIds);
            foreach(var sourceCut in BraxCityIndustrial.SourceCuts)
            {
                var batch=original.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==sourceCut.renderer);
                if(!batch)throw new InvalidOperationException("Warehouse source batch changed: "+sourceCut.renderer);
                BraxCityMeshCut.Subtract(batch,sourceCut.volume,Output+"/Cuts",sourceCut.label);
            }
            BraxCityIndustrial.Apply(root);
            // High-rises are placed on the 18 *real street footprints*. The
            // original source first floors are never vertically stretched.
            // Their street-facing facades and returns remain fully modular.
            BraxCityStreetSkyline.Build(root);
            BraxCityPremiumTowerBuild.Build(world,root);
            // The original task required actual access, not skyscraper
            // facades with no doors. The quay lift remains the signature
            // installation; the other eight towers get independent cabs,
            // shaft walls, roof landings and safety railings.
            BraxCityTowerLiftNetwork.Build(world,root);
            Disable(original,Enumerable.Range(179,15));
            BraxCityArchitecture.Tower(root,"north brick district",new Rect(27,100,35,13),.15f,22);
            BraxCityArchitecture.Tower(root,"north glass landmark",new Rect(-13,100,35,13),.15f,31);
            BraxCityArchitecture.Tower(root,"west skyline",new Rect(-33,51,13,33),.15f,25);
            BraxCityArchitecture.Tower(root,"south skyline",new Rect(22,-49,35,14),.15f,24);
            BraxCityArchitecture.Tower(root,"south west skyline",new Rect(-54,-51,36,15),.15f,28);
            Disable(original,Enumerable.Range(105,11));
            BraxCityMeshCut.SubtractBoxColliders(physics,Box(-71.3f,-61.1f,0,31,-15,121));
            BraxCityGround.Viaduct(root);
        }

        foreach(var cut in BraxCitySystemsBuild.CrossingCurbCuts)
            Cut(original,physics,Box(cut.xMin,cut.xMax,-.3f,.25f,cut.yMin,cut.yMax),"crossing_lowered_kerb");
        // The original NOVA and Nutrition interior anchors were present but
        // their street-facing source wall/collision was not a usable portal.
        // Cut only the 2.2m source doorway volumes; retain every other source
        // wall, floor, bar, shop shelf and interior object.
        Cut(original,physics,Box(40.9f,43.1f,.12f,2.90f,35.98f,36.45f),
            "nova_real_source_entrance");
        Cut(original,physics,Box(19.60f,21.80f,.12f,2.90f,20.30f,21.45f),
            "nutrition_real_source_entrance");
        BraxCityVenueDoors.Apply(world,root);
        var metro=BraxCitySystemsBuild.MetroExcavation;
        Cut(original,physics,Box(metro.xMin,metro.xMax,-4,.5f,metro.yMin,metro.yMax),"metro_excavation");
        // The station is an actual underground volume, not an overlay below
        // the source's solid virtual y=0 ground. Remove only the exact
        // corridor and platform air volumes, leaving all other city geometry.
        // The station builder replaces the ground cover with a load-bearing
        // roof and the lower floor with source-library stone.
        Cut(original,physics,Box(74.9f,80.8f,-3.52f,.12f,49.7f,54.05f),
            "G5_access_corridor_void");
        Cut(original,physics,Box(69.7f,85.6f,-3.55f,.12f,54.0f,72.2f),
            "G5_station_platform_and_tracks_void");
        var stone=BraxCityMaterials.Surface("Metro limestone",new Color(.55f,.55f,.51f),.93f);
        var concrete=BraxCityMaterials.Surface("Metro fair-faced concrete",new Color(.5f,.51f,.49f),.95f);
        var metal=BraxCityMaterials.Surface("City brushed steel",new Color(.2f,.22f,.23f),.48f,.55f);
        var glass=BraxCityMaterials.Surface("City controlled glass",new Color(.16f,.23f,.25f),.25f,.18f);
        BraxCitySystemsBuild.Apply(world,root,stone,concrete,metal,glass);

        // Replace only the inspected furnishing groups. Original collision
        // proxies are retained; the environment builder adds collision solely
        // to new objects which did not have a corresponding source proxy.
        Disable(original,BraxCityEnvironment.ReplacedOriginalMeshes);
        var containerBatch=original.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="CityMesh_231");
        var timberBatch=original.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="CityMesh_226");
        if(!containerBatch||!timberBatch)throw new InvalidOperationException("Original harbour batches changed; refuse a broad replacement");
        BraxCityMeshCut.Subtract(containerBatch,BraxCityEnvironment.ContainerReplacementRegion,Output+"/Cuts","container_panels");
        for(int i=0;i<BraxCityEnvironment.PalletReplacementRegions.Length;i++)
            BraxCityMeshCut.Subtract(timberBatch,BraxCityEnvironment.PalletReplacementRegions[i],Output+"/Cuts","pallet_"+i);
        BraxCityEnvironment.Apply(root);
        if(WholeDistrict)BraxCityStreetLighting.Build(root);

        // Only supplied brand images, with their native aspect ratio and real
        // steel supports. The first review uses two large signs, not a wall of ads.
        const string Brand="Assets/CityRebuild/Brand/";
        BraxCityArchitecture.Billboard(root,"nutrition-wide",new Vector3(45,9,21.2f),9,Brand+"brand-wide.png",0);
        BraxCityArchitecture.Billboard(root,"training",new Vector3(39,39,20.05f),13,Brand+"training.png",0);
        if(WholeDistrict)
        {
            BraxCityArchitecture.Billboard(root,"brax",new Vector3(-18,16,21.2f),6,Brand+"brax-fighter.jpg",0);
            BraxCityArchitecture.Billboard(root,"nyxa",new Vector3(-39,16,35.7f),5,Brand+"nyxa-pink.jpg",180);
            BraxCityArchitecture.Billboard(root,"pw-ai-video",new Vector3(57,12,35.6f),4,Brand+"pw-ai-video.jpg",180);
        }
        BraxCityArchitecture.Finish(root);
        // Editor-only baked sun visibility/AO, in generated mesh copies. No
        // global gym light, shared material, animation or door is modified.
        BraxCityLightingOcclusionProxy.Bake(root,original);
        // The prior release retouched NEW facades only and left the OLD
        // neighbouring buildings in oversaturated vertex/baked shader mode.
        // Retune surviving public street legacy geometry with real matte PBR.
        // Interior NOVA/Nutrition and all imported source files stay intact.
        BraxCityLegacyFacadeFinish.Apply(original);
        Disable(original,new[]{BraxCityWaterBuild.ReplacedOriginalMesh});
        BraxCityWaterBuild.Apply(root);
        if(gymDoor&&(Vector3.Distance(gymDoorPosition,gymDoor.transform.position)>.0001f||Quaternion.Angle(gymDoorRotation,gymDoor.transform.rotation)>.0001f||Vector3.Distance(gymDoorScale,gymDoor.transform.lossyScale)>.0001f))
            throw new InvalidOperationException("Gym doorway integration boundary changed unexpectedly");
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        // Count installed mesh-bearing building objects, not declared plan
        // entries. Fail closed if any required storefront is missing.
        var builtBuildings=BraxCityArchitecture.StreetBuildings.Where(b=>
        {
            var node=root.Find("Building "+b.id);
            return node&&node.GetComponentsInChildren<MeshRenderer>(true).Any(x=>x.enabled);
        }).ToArray();
        var highRises=root.GetComponentsInChildren<Transform>(true)
            .Count(t=>t.name.StartsWith("High-rise ",StringComparison.Ordinal));
        if(WholeDistrict && (builtBuildings.Length!=18||highRises<9||world.elevator==null))
            throw new InvalidOperationException("CITY_PREMIUM_INCOMPLETE measured street buildings="+
                builtBuildings.Length+" highRises="+highRises+" elevator="+(world.elevator!=null));
        // Source world may contain doors of the gym / NOVA. Require new doors
        // from the 18 audited storefront footprints in addition to preserved
        // interiors, no named building with a fake decorative entry.
        if(WholeDistrict)
        {
            var required=new HashSet<string>(BraxCityArchitecture.StreetBuildings
                .Select(b=>b.id=="quay_corner"?"R1_TOWER_DOOR":"R1_PUBLIC_DOOR_"+b.id));
            var present=new HashSet<string>(world.doors.Where(d=>d).Select(d=>d.id));
            var missing=required.Where(id=>!present.Contains(id)).ToArray();
            if(missing.Length>0)
                throw new InvalidOperationException("CITY_REAL_DOORS_MISSING "+
                    string.Join(",",missing));
            Debug.Log("CITY_ALL_REAL_ENTRANCES_AUDITED count="+required.Count+
                " originalInteriorsPreserved=true");
            if(world.additionalTowerElevators.Length!=8)
                throw new InvalidOperationException("CITY_HIGHRISE_ELEVATORS_MISSING "+
                    world.additionalTowerElevators.Length+"/8");
            var distinctShafts=world.additionalTowerElevators
                .Select(l=>l.shaftCenter).Distinct().Count();
            if(distinctShafts!=8)
                throw new InvalidOperationException("CITY_ELEVATOR_SHAFT_DUPLICATE "+
                    distinctShafts+"/8");
            Debug.Log("CITY_NINE_PHYSICAL_ELEVATORS_INCLUDED actual="+
                (world.additionalTowerElevators.Length+1)+
                " uniqueAdditionalShafts="+distinctShafts+
                " sourceBuildings=18");
        }
        if(UnityEngine.Object.FindObjectsByType<BraxGymPlayer>(FindObjectsSortMode.None).Length!=1)
            throw new InvalidOperationException("Architecture-only city must contain precisely one controllable Brax");
        report=new ReviewReport{actuallyBuiltStreetBuildings=builtBuildings.Length,actuallyBuiltHighRises=highRises,
            actualBuildingIds=builtBuildings.Select(b=>b.id).ToArray(),streetMainRoadLengthMetres=170,
            vehicleSourceRenderersHidden=hiddenVehicleMeshes,
            stage=WholeDistrict?"whole-district-awaiting-visual-and-mobile-acceptance":"representative-segment-awaiting-player-review",cityCommit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            renderedMeshes=renderers.Length,triangles=renderers.Sum(r=>{var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?f.sharedMesh.triangles.Length/3:0;}),
            materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),
            physicalElevatorInstalled=world.elevator!=null,
            replacedSourceRenderers=replaced.Distinct().ToArray()};
        File.WriteAllText(Output+"/review-scope.json",JsonUtility.ToJson(report,true));
        Debug.Log("CITY_REFERENCE_REBUILD "+JsonUtility.ToJson(report));AssetDatabase.SaveAssets();
    }
    public static void WriteReport(string output)
    {if(report!=null)File.WriteAllText(Path.Combine(output,"city-rebuild.json"),JsonUtility.ToJson(report,true));}
}

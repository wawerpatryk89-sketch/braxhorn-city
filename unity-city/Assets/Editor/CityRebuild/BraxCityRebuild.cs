using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxCityRebuild
{
    // The first Unity review deliberately limits architectural replacement to
    // one complete street segment. Flip only after inspecting the real player
    // build; an unreviewed first pass is never reported as the finished city.
    public const bool WholeDistrict=false;
    const string Output="Assets/PlayableGenerated/CityRebuild";
    static readonly List<string> replaced=new List<string>();
    [Serializable] sealed class ReviewReport
    {
        public string stage,sourceCommit="ba678928b2dcb2dff9e38bdb5306e090e76360cf",cityCommit,gymIntegration="Source gym preserved; other contractor final commit not yet supplied";
        public int renderedMeshes,triangles,materials,missionReservations=11;
        public bool sourceAssetsRetained=true,phoneAccepted=false,visualAccepted=false;
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
    static void Cut(Transform original,Transform physics,Bounds volume,string id,bool removeCollision=true)
    {
        BraxCityMeshCut.Subtract(original,volume,Output+"/Cuts",id);
        if(removeCollision)BraxCityMeshCut.SubtractBoxColliders(physics,volume);
    }
    public static void Apply(BraxCityWorld world,Transform original,Transform physics)
    {
        if(!world||world.area!="city")throw new InvalidOperationException("Renovation must run on the actual city source");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();replaced.Clear();
        var gymDoor=GameObject.Find("Original opening door");
        var gymDoorPosition=gymDoor?gymDoor.transform.position:Vector3.zero;
        var gymDoorRotation=gymDoor?gymDoor.transform.rotation:Quaternion.identity;
        var gymDoorScale=gymDoor?gymDoor.transform.lossyScale:Vector3.one;
        var root=new GameObject("BRAXHORN CITY reference renovation").transform;
        BraxCityArchitecture.Prepare();
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
            BraxCityArchitecture.Build(root,b);
        }
        // A set-back tower above the representative block provides the high
        // skyline requested by the user. The lower detailed street floors stay
        // at their original physical scale and retain distinct premises.
        BraxCityArchitecture.Tower(root,"central setback",new Rect(28,12.4f,22,7.2f),23.7f,22);
        if(WholeDistrict)
        {
            Cut(original,physics,Box(14.5f,26,5.1f,30,11.6f,20.63f),"shop_upper",false);
            BraxCityArchitecture.UpperExtension(root,"BRAXHORN NUTRITION",14.5f,26,11.6f,20.6f,5.1f,6);
            // NOVA interior/cellar (including their doors and stairs) remain
            // untouched. Only geometry above its existing ceiling is replaced.
            Cut(original,physics,Box(32,52,5.1f,30,36.08f,62),"nova_upper",false);
            BraxCityArchitecture.UpperExtension(root,"NOVA",32,52,36.1f,62,5.1f,5);
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
        var metro=BraxCitySystemsBuild.MetroExcavation;
        Cut(original,physics,Box(metro.xMin,metro.xMax,-4,.5f,metro.yMin,metro.yMax),"metro_excavation");
        var stone=BraxCityMaterials.Surface("Metro limestone",new Color(.55f,.55f,.51f),.93f);
        var concrete=BraxCityMaterials.Surface("Metro fair-faced concrete",new Color(.5f,.51f,.49f),.95f);
        var metal=BraxCityMaterials.Surface("City brushed steel",new Color(.2f,.22f,.23f),.48f,.55f);
        var glass=BraxCityMaterials.Surface("City controlled glass",new Color(.16f,.23f,.25f),.25f,.18f);
        BraxCitySystemsBuild.Apply(world,root,stone,concrete,metal,glass);

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
        BraxCityArchitecture.Finish();
        // Editor-only baked sun visibility/AO, in generated mesh copies. No
        // global gym light, shared material, animation or door is modified.
        BraxCityMaterials.Bake(root,new[]{original});
        if(gymDoor&&(Vector3.Distance(gymDoorPosition,gymDoor.transform.position)>.0001f||Quaternion.Angle(gymDoorRotation,gymDoor.transform.rotation)>.0001f||Vector3.Distance(gymDoorScale,gymDoor.transform.lossyScale)>.0001f))
            throw new InvalidOperationException("Gym doorway integration boundary changed unexpectedly");
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        report=new ReviewReport{stage=WholeDistrict?"whole-district-awaiting-visual-and-mobile-acceptance":"representative-segment-awaiting-player-review",cityCommit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            renderedMeshes=renderers.Length,triangles=renderers.Sum(r=>{var f=r.GetComponent<MeshFilter>();return f&&f.sharedMesh?f.sharedMesh.triangles.Length/3:0;}),
            materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),replacedSourceRenderers=replaced.Distinct().ToArray()};
        File.WriteAllText(Output+"/review-scope.json",JsonUtility.ToJson(report,true));
        Debug.Log("CITY_REFERENCE_REBUILD "+JsonUtility.ToJson(report));AssetDatabase.SaveAssets();
    }
    public static void WriteReport(string output)
    {if(report!=null)File.WriteAllText(Path.Combine(output,"city-rebuild.json"),JsonUtility.ToJson(report,true));}
}

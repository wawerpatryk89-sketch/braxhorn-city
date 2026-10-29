using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Standalone stage-1 iPhone visual-review slice, NOT a city source rewrite.
// Complete geometry remains in the full-city branch and every original glTF.
// This temporary scene retains only the gym→nutrition→NOVA→crossing frontage.
public static class BraxCitySegmentReview
{
    public const float MinX=-17f,MaxX=69f,MinZ=-12f,MaxZ=70f;
    public static readonly Vector3 StreetSpawn=new Vector3(6.75f,.23f,27.85f);
    static string reportJSON;
    [Serializable] sealed class SegmentReport
    {
        public string mode="CITY_ONLY_STAGE_1_GYM_NUTRITION_NOVA";
        public string sourceCommit;
        public float xMin,xMax,zMin,zMax;
        public bool originalBrax=true,originalGymSourceUnmodified=true;
        public bool fullCitySourceRetained=true,estateExcludedFromPreviewOnly=true;
        public bool actualUnityPlaytested=false,physicalIphoneAccepted=false;
        public int originalCityMeshesPruned,distantCityRenderersPruned,distantPhysicsObjectsPruned;
        public int selectedBuildingFrontages,selectedHighRises;
        public int renderedSceneMeshes,renderedSceneTriangles;
        public Vector3 playerSpawn;
        public string[] selectedBuildingIds;
        public string[] explicitExclusions={
            "Rest of western district beyond this segment",
            "Full forest and harbour outskirts",
            "Estate scene — source preserved separately",
            "No cars, NPC, enemies or activated missions"
        };
    }
    static bool Intersects(Bounds bounds)
    {
        const float padding=2f;
        return bounds.max.x >= MinX-padding && bounds.min.x <= MaxX+padding &&
               bounds.max.z >= MinZ-padding && bounds.min.z <= MaxZ+padding;
    }
    static bool VisibleInSegment(Renderer r)
    {
        return r && r.enabled && r.gameObject.activeInHierarchy && Intersects(r.bounds);
    }
    static void RemoveFarSourceGeometry(Transform original,ref int count)
    {
        // The city source is a reviewed, unpacked temporary prefab instance
        // in the iPhone-only build path; deleting disabled/distant instances
        // never modifies the source .gltf or other workers' scene.
        var sources=original.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith("CityMesh_",StringComparison.Ordinal)).ToArray();
        foreach(var t in sources)
        {
            if(!t)continue;
            var renderer=t.GetComponent<MeshRenderer>();
            if(!renderer)continue;
            if(VisibleInSegment(renderer))continue;
            if(t.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))
            {
                if(renderer.enabled)
                    throw new InvalidOperationException("Out-of-segment original source has physics: "+t.name);
                continue;
            }
            UnityEngine.Object.DestroyImmediate(t.gameObject);
            count++;
        }
    }
    static void RemoveFarGeneratedMeshes(Transform city,ref int count)
    {
        // Destroy whole distant designed buildings first, retaining intact
        // per-storey facade kits inside every selected local building.
        var groups=city.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith("Building ",StringComparison.Ordinal)
                ||t.name.StartsWith("High-rise ",StringComparison.Ordinal)).ToArray();
        foreach(var group in groups)
        {
            if(!group)continue;
            var renderers=group.GetComponentsInChildren<MeshRenderer>(true);
            if(renderers.Length==0 || renderers.Any(VisibleInSegment))continue;
            count+=renderers.Count(r=>r.enabled);
            UnityEngine.Object.DestroyImmediate(group.gameObject);
        }
        // Remaining generated local 10m roadway, lamppost, industrial, metro
        // and harbour components carry individual meshes. No invalid cached
        // prefab meshes are kept as invisible geometry in the test player.
        var parts=city.GetComponentsInChildren<MeshRenderer>(true).ToArray();
        foreach(var r in parts)
        {
            if(!r || VisibleInSegment(r))continue;
            var go=r.gameObject;
            if(go.GetComponent<BraxCityElevator>()||go.GetComponent<BraxCityWorld>()
                ||go.GetComponent<BraxCityTrafficController>()||go.GetComponent<BraxCityDoor>())
                continue;
            // Do not delete parent objects with multiple authored children,
            // because one child could still be visible in this slice.
            count++;
            if(go.transform.childCount==0)
                UnityEngine.Object.DestroyImmediate(go);
            else
            {
                // Important: renderer's mesh/collider must not enter the build.
                var mf=go.GetComponent<MeshFilter>();
                if(mf)UnityEngine.Object.DestroyImmediate(mf);
                UnityEngine.Object.DestroyImmediate(r);
            }
        }
    }
    static void RemoveFarPhysics(Transform source,ref int count)
    {
        var objects=source.GetComponentsInChildren<Collider>(true).ToArray();
        foreach(var c in objects)
        {
            if(!c ||Intersects(c.bounds))continue;
            var go=c.gameObject;
            if(go.GetComponents<MonoBehaviour>().Any())
            {
                c.enabled=false;count++;continue;
            }
            if(go.transform.childCount==0)
                UnityEngine.Object.DestroyImmediate(go);
            else UnityEngine.Object.DestroyImmediate(c);
            count++;
        }
    }
    static Material Material(string name,Color color,float roughness,float metallic)
        =>BraxCityMaterials.Surface(name,color,roughness,metallic);
    static void Rail(Transform parent,string name,Vector3 pos,Vector3 size,Material mat,bool collider)
    {
        // Temporary architectural boundary belongs ONLY to stage-1 review;
        // the real district has no such wall at its eventual connection.
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;go.transform.SetParent(parent,false);
        go.transform.position=pos;go.transform.localScale=size;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    static void BuildEndBarrier(Transform parent,float x,string label)
    {
        var g=new GameObject("CITY REVIEW boundary / "+label).transform;g.SetParent(parent,false);
        var steel=Material("Review-only satin steel barrier",new Color(.14f,.17f,.18f),.65f,.53f);
        var glass=Material("Review-only tempered glass safety",new Color(.34f,.45f,.46f,.35f),.22f,.10f);
        var stone=Material("Review-only kerb mounted post",new Color(.40f,.42f,.40f),.9f,.01f);
        // The barrier is functional collision, not an invisible wall.
        Rail(g,"Full-width approved review barrier",new Vector3(x,1.60f,28.4f),
            new Vector3(.09f,3.2f,16.5f),glass,true);
        foreach(float z in new[]{20.9f,24f,28f,32f,36.15f})
        {
            Rail(g,"Stainless-grounded protective bollard",new Vector3(x,.85f,z),
                new Vector3(.24f,1.7f,.24f),steel,false);
            Rail(g,"Architectural stone footing",new Vector3(x,.13f,z),
                new Vector3(.48f,.26f,.48f),stone,false);
        }
        for(float y=1.08f;y<3.3f;y+=1.08f)
            Rail(g,"Continuous brushed rail",new Vector3(x,y,28.4f),
                new Vector3(.12f,.075f,16.5f),steel,false);
    }
    public static void Apply(BraxGymPlayer player)
    {
        if(Environment.GetEnvironmentVariable("BRAX_CITY_SEGMENT_REVIEW")!="1"||
            Environment.GetEnvironmentVariable("BRAX_CITY_MOBILE_REVIEW")!="1")
            throw new InvalidOperationException("Segment review must be a separate iOS city-only build");
        if(!player||!player.cityWorld||player.cityWorld.area!="city")
            throw new InvalidOperationException("Segment needs original Brax in compiled city scene");
        var original=GameObject.Find("braxhorn-city");
        var city=GameObject.Find("BRAXHORN CITY reference renovation");
        var physics=GameObject.Find("Original city collisions");
        if(!original||!city||!physics)
            throw new InvalidOperationException("Segment boundary could not locate compiled source city / physics");
        int sourcePruned=0,detailPruned=0,physicsPruned=0;
        RemoveFarSourceGeometry(original.transform,ref sourcePruned);
        RemoveFarGeneratedMeshes(city.transform,ref detailPruned);
        RemoveFarPhysics(physics.transform,ref physicsPruned);
        // Hide future navigation points outside the slice, not source mission
        // reservations (11 technical IDs continue to exist in the full city).
        player.cityWorld.points=player.cityWorld.points
            .Where(p=>p.position.x>=MinX && p.position.x<=MaxX
                &&p.position.z>=MinZ && p.position.z<=MaxZ).ToArray();
        // The linked roof tower and existing NOVA and nutrition doors remain.
        var selected=BraxCityArchitecture.StreetBuildings.Where(b=>
            b.x1>=MinX && b.x0<=MaxX).ToArray();
        var surviving=city.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith("Building ",StringComparison.Ordinal)).ToArray();
        foreach(var b in selected)
            if(!surviving.Any(t=>t.name=="Building "+b.id))
                throw new InvalidOperationException("Segment lost an expected original frontage "+b.id);
        var highRises=city.GetComponentsInChildren<Transform>(true)
            .Count(t=>t.name.StartsWith("High-rise ",StringComparison.Ordinal));
        if(highRises<2)throw new InvalidOperationException("Premium skyline was lost in the local segment");
        var limits=new GameObject("REVIEW ONLY - next area protected behind architectural barriers").transform;
        BuildEndBarrier(limits,MinX,"WEST");
        BuildEndBarrier(limits,MaxX,"EAST");
        // Set spawn in existing street coordinates; user receives manual
        // camera and ORIGINAL Brax, not a 2D billboard or new controller.
        player.transform.position=StreetSpawn;
        player.worldSpawn=StreetSpawn;
        var current=UnityEngine.Object.FindObjectsByType<BraxGymPlayer>(FindObjectsSortMode.None);
        if(current.Length!=1||current[0]!=player)
            throw new InvalidOperationException("Exactly one original Brax must remain playable");
        var active=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
        var report=new SegmentReport
        {
            sourceCommit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",
            xMin=MinX,xMax=MaxX,zMin=MinZ,zMax=MaxZ,
            playerSpawn=StreetSpawn,
            originalCityMeshesPruned=sourcePruned,
            distantCityRenderersPruned=detailPruned,
            distantPhysicsObjectsPruned=physicsPruned,
            selectedBuildingFrontages=selected.Length,
            selectedHighRises=highRises,
            selectedBuildingIds=selected.Select(b=>b.id).ToArray(),
            renderedSceneMeshes=active.Length,
            renderedSceneTriangles=active.Sum(r=>{var f=r.GetComponent<MeshFilter>();
                return f&&f.sharedMesh?f.sharedMesh.triangles.Length/3:0;})
        };
        reportJSON=JsonUtility.ToJson(report,true);
        Debug.Log("CITY_REAL_PLAYABLE_SEGMENT "+reportJSON);
    }
    public static void WriteReport(string output)
    {
        if(string.IsNullOrEmpty(reportJSON))
            throw new InvalidOperationException("No actual segment generated");
        File.WriteAllText(Path.Combine(output,"city-segment-review.json"),reportJSON);
    }
}

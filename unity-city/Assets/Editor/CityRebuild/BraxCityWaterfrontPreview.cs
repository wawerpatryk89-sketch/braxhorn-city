using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Isolated review build, never invoked by BuildCity or production.
// Keeps the real Brax rig, original gym and all source city assets immutable.
public static class BraxCityWaterfrontPreview
{
    [Serializable] sealed class ScopeReport
    {
        public string scope="gym-right-to-waterfront-review";
        public bool includesOriginalGym=true,includesOriginalBrax=true,includesEstate=false;
        public bool visualAccepted=false,physicalIPhoneAccepted=false;
        public int strippedRenderers,strippedColliders,retainedRenderers;
        public float minX=-6,maxX=195,minZ=-64,maxZ=155;
        public string note="Bounds-based renderer and collider stripping; source models and original project unchanged. Actual size and mobile playback require CI proof.";
    }

    public static void Apply(BraxGymPlayer player)
    {
        if(!player||!player.cityWorld||player.cityWorld.area!="city")
            throw new InvalidOperationException("Waterfront review requires the real imported city and Brax");
        var world=player.cityWorld;
        world.waterfrontPreview=true;
        // This footprint retains gym entry, the right-hand city corridor,
        // Nutrition, NOVA, quay, pier and visible shoreline.
        // Geometry crossing the bounds is deliberately retained, not clipped.
        var region=new Bounds(new Vector3(94.5f,55,45.5f),new Vector3(201,300,219));
        var report=new ScopeReport();
        foreach(var rootName in new[]{"braxhorn-city","BRAXHORN CITY reference renovation"})
        {
            var root=GameObject.Find(rootName);
            if(!root)throw new InvalidOperationException("Waterfront scene root missing: "+rootName);
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer||renderer.bounds.Intersects(region))continue;
                // Do not destroy whole objects: world doors, moving lifts and
                // authoring roots may own non-visual components.
                UnityEngine.Object.DestroyImmediate(renderer);
                report.strippedRenderers++;
            }
        }
        // Only remove original physics outside the region; retain the real
        // source staircases, walkable ground, street doors and waterfront.
        var physics=GameObject.Find("Original city collisions");
        if(!physics)throw new InvalidOperationException("Original city physics root missing");
        foreach(var collider in physics.GetComponentsInChildren<Collider>(true))
        {
            if(!collider||collider.bounds.Intersects(region))continue;
            UnityEngine.Object.DestroyImmediate(collider);
            report.strippedColliders++;
        }
        report.retainedRenderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length;
        if(report.strippedRenderers<1||report.retainedRenderers<100)
            throw new InvalidOperationException("Waterfront preview pruning failed: "+JsonUtility.ToJson(report));
        var output="Assets/PlayableGenerated/CityRebuild";
        Directory.CreateDirectory(output);
        File.WriteAllText(output+"/waterfront-preview-scope.json",JsonUtility.ToJson(report,true));
        AssetDatabase.Refresh();
        Debug.Log("CITY_WATERFRONT_PREVIEW_SCOPE "+JsonUtility.ToJson(report));
    }
}

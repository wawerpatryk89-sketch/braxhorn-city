using System;
using System.Linq;
using UnityEngine;

// Editor-only scratch geometry for realistic street-scale cast shadows.
// These proxy solids are NOT rendered in the finished game, never enter the
// saved scene, and cannot be mistaken for street architecture.
public static class BraxCityLightingOcclusionProxy
{
    static void Box(BraxCitySystemMesh mesh,float x0,float x1,float y0,float y1,float z0,float z1)
    {
        if(x1<=x0||y1<=y0||z1<=z0)throw new InvalidOperationException("Invalid building lighting silhouette");
        mesh.Box(new Vector3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),
            new Vector3(x1-x0,y1-y0,z1-z0));
    }

    public static BraxCityMaterials.BakeReport Bake(Transform city,Transform original)
    {
        if(!city||!original)throw new ArgumentNullException("City lighting requires actual built source and renovation");
        GameObject proxy=null;
        Mesh temporaryMesh=null;
        Material temporaryMaterial=null;
        try
        {
            var silhouettes=new BraxCitySystemMesh();
            var buildings=BraxCityArchitecture.StreetBuildings;
            if(buildings.Length!=18)throw new InvalidOperationException("Must prepare both authored street frontages");
            foreach(var b in buildings)
            {
                // A 0.28m interior offset keeps window-frame normals on the
                // genuine facade outside its lighting silhouette; otherwise
                // a solid proxy would incorrectly darken every window.
                Box(silhouettes,b.x0+.28f,b.x1-.28f,.20f,b.Height-.25f,
                    b.z0+.28f,b.z1-.28f);
            }
            foreach(var t in BraxCityStreetSkyline.HighRises)
            {
                var b=buildings.Single(x=>x.id==t.id);
                var f=BraxCityStreetSkyline.Footprint(b,t);
                Box(silhouettes,f.xMin+.23f,f.xMax-.23f,b.Height+.15f,
                    BraxCityStreetSkyline.RoofHeight(b,t.additionalFloors)-.18f,
                    f.yMin+.23f,f.yMax-.23f);
            }
            // Skyline blocks outside the 18 main-road plots are included in
            // distant directional occlusion, but never used for street collision.
            Box(silhouettes,27.25f,61.75f,.3f,73.5f,100.25f,112.75f);
            Box(silhouettes,-12.75f,21.75f,.3f,104.0f,100.25f,112.75f);
            Box(silhouettes,-32.75f,-20.25f,.3f,83.75f,51.25f,83.75f);
            Box(silhouettes,22.25f,56.75f,.3f,80.55f,-48.75f,-35.25f);
            Box(silhouettes,-53.75f,-18.25f,.3f,94.0f,-50.75f,-36.25f);
            // Real steel deck remains the actual render and collider source.
            // This slender invisible box only retains its road-level shadow.
            Box(silhouettes,-70.9f,-61.5f,8.18f,9.45f,-14f,120f);
            proxy=new GameObject("EDITOR_ONLY_OFFLINE_LIGHTING_SILHOUETTES_DELETE_AFTER_BAKE");
            temporaryMesh=silhouettes.Build("Temporary architectural silhouette / never exported");
            proxy.AddComponent<MeshFilter>().sharedMesh=temporaryMesh;
            var shader=Shader.Find("Standard");
            if(!shader)throw new InvalidOperationException("Offline silhouette requires standard Unity shader");
            temporaryMaterial=new Material(shader){name="Do not export editor-only lighting proxy"};
            proxy.AddComponent<MeshRenderer>().sharedMaterial=temporaryMaterial;
            Debug.Log("CITY_PROXIES_BEGIN buildings="+buildings.Length+" highRises="+
                BraxCityStreetSkyline.HighRises.Length+" triangles="+
                temporaryMesh.triangles.Length/3);
            var report=BraxCityMaterials.Bake(city,new[]{original,proxy.transform},false);
            Debug.Log("CITY_PROXY_LIGHTING_COMPLETE triangles="+report.triangles+
                " receivers="+report.receivers+" shadowSamples="+report.samples+
                " originalSourcesRetained=true");
            return report;
        }
        finally
        {
            if(proxy)UnityEngine.Object.DestroyImmediate(proxy);
            if(temporaryMaterial)UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            if(temporaryMesh)UnityEngine.Object.DestroyImmediate(temporaryMesh);
        }
    }
}

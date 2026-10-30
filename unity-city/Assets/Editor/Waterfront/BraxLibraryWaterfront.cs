using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in isolated Unity 6 authoring. Real owner-supplied GLB and Poly Haven PBR only.
// Do NOT call from the production BuildCity/BuildGym entry point.
public static class BraxLibraryWaterfront
{
    const string Source="Assets/Source/Waterfront/";
    const string Models=Source+"Models/", Sand=Source+"Sand/";
    const string Generated="Assets/PlayableGenerated/WaterfrontLibrary";
    const float WaterY=-1.7f;

    [Serializable] public class Placed { public string id,model; public Vector3 position; public float metres; }
    [Serializable] public class Review {
        public string engine="Unity 6";
        public string scope="Gym-right-to-sea ONLY";
        public string status="SOURCE_ASSEMBLED_AWAITING_UNITY_BUILD_AND_VISUAL_PASS";
        public bool originalGymUnchanged=true, nutritionUnchanged=true, originalCitySourcesUnchanged=true;
        public bool productionPublished=false, ownerVisualAccepted=false, physicalIPhoneAccepted=false;
        public Placed[] placed;
    }

    static readonly string[] Required={
        Models+"yacht.glb", Models+"bateau_riva_boat.glb", Models+"jetski_collection.glb",
        Models+"harbor_crane.glb", Models+"rustic_wooden_dock.glb", Models+"lifeguard_tower.glb",
        Sand+"coast_sand_02_diff_2k.jpg", Sand+"coast_sand_02_normal_2k.png",
        Sand+"coast_sand_02_arm_2k.png", Sand+"coast_sand_02_disp_2k.png",
        Sand+"damp_sand_diff_2k.jpg", Sand+"damp_sand_normal_2k.png",
        Sand+"damp_sand_arm_2k.png"
    };

    public static void Apply(BraxGymPlayer player)
    {
        if(!player || !player.cityWorld || player.cityWorld.area!="city")
            throw new InvalidOperationException("Only existing original Brax city is permitted");
        if(Mathf.Abs(BraxCityWaterBuild.WaterY-WaterY)>.001f || Mathf.Abs(BraxCityWaterBuild.ShoreX-84f)>.001f)
            throw new InvalidOperationException("City shoreline drift: inspect before placing any model");
        if(GameObject.Find("BRAXHORN LIBRARY WATERFRONT (TEST)"))
            throw new InvalidOperationException("Duplicate library port prevented");
        foreach(var path in Required)if(!File.Exists(path))
            throw new FileNotFoundException("Missing true Library asset; never fabricate a substitute",path);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in Required)if(!AssetDatabase.LoadMainAssetAtPath(path))
            throw new InvalidOperationException("Library asset not imported in Unity: "+path);
        Directory.CreateDirectory(Generated);
        var root=new GameObject("BRAXHORN LIBRARY WATERFRONT (TEST)").transform;
        Beach(root);
        var placed=new List<Placed>();
        Model(root,"harbor_crane.glb","Harbour gantry crane",new Vector3(71,.15f,-42),29,91,true,null,false,placed);
        Model(root,"rustic_wooden_dock.glb","Marina real timber pier",new Vector3(119,.34f,76.7f),25,90,true,null,false,placed);
        Model(root,"yacht.glb","Yacht at quay",new Vector3(150,WaterY-1.1f,54),17,90,false,null,false,placed);
        Model(root,"bateau_riva_boat.glb","Motorboat at marina",new Vector3(154,WaterY-.65f,91),11,45,false,null,false,placed);
        Model(root,"jetski_collection.glb","Jet ski blue",new Vector3(126,WaterY-.2f,110),3.2f,0,false,"material_gameasset.001",false,placed);
        Model(root,"jetski_collection.glb","Jet ski yellow",new Vector3(135,WaterY-.2f,110),3.2f,-25,false,"material_gameasset.003",false,placed);
        Model(root,"lifeguard_tower.glb","Real beach lifeguard tower",new Vector3(99,-.21f,140),9,180,true,null,true,placed);
        var review=new Review{placed=placed.ToArray()};
        File.WriteAllText(Generated+"/waterfront-qa.json",JsonUtility.ToJson(review,true));
        AssetDatabase.ImportAsset(Generated+"/waterfront-qa.json");
        AssetDatabase.SaveAssets();
        Debug.Log("BRAXHORN_LIBRARY_PORT_SOURCE_ONLY_NOT_VISUALLY_APPROVED "+JsonUtility.ToJson(review));
    }

    static Texture2D Tex(string name)
    {
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Sand+name);
        if(!t)throw new InvalidOperationException("Missing genuine Poly Haven map: "+name);
        return t;
    }
    static void Beach(Transform root)
    {
        var dry=BraxCityMaterials.Textured("Original 4K coast sand / Unity 2K preview",
            Tex("coast_sand_02_diff_2k.jpg"),Tex("coast_sand_02_normal_2k.png"),Tex("coast_sand_02_arm_2k.png"));
        var wet=BraxCityMaterials.Textured("Original 4K damp sand / Unity 2K preview",
            Tex("damp_sand_diff_2k.jpg"),Tex("damp_sand_normal_2k.png"),Tex("damp_sand_arm_2k.png"));
        var height=Tex("coast_sand_02_disp_2k.png");
        var imp=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(height));
        if(!imp.isReadable){imp.isReadable=true;imp.sRGBTexture=false;imp.SaveAndReimport();height=Tex("coast_sand_02_disp_2k.png");}
        const int nx=72,nz=60;
        var points=new Vector3[(nx+1)*(nz+1)];
        var uv=new Vector2[points.Length];
        var dryIndices=new List<int>();var wetIndices=new List<int>();
        for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
        {
            float u=(float)i/nx,v=(float)j/nz;
            float z=115f+v*50f,x=82.4f+u*(59f+3*Mathf.Sin(v*Mathf.PI*2f+.3f));
            float h=Mathf.Lerp(.19f,WaterY+.1f,u*u*(3f-2f*u));
            h+=(height.GetPixelBilinear((u*2.3f)%1f,(v*1.7f)%1f).grayscale-.5f)*.085f*(1f-u);
            int idx=j*(nx+1)+i;points[idx]=new Vector3(x,h,z);uv[idx]=new Vector2(x/5f,z/5f);
        }
        for(int j=0;j<nz;j++)for(int i=0;i<nx;i++)
        {
            int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
            var into=i<nx*.73f?dryIndices:wetIndices;
            into.Add(a);into.Add(c);into.Add(b);
            into.Add(b);into.Add(c);into.Add(d);
        }
        string meshPath=Generated+"/RealCoastalDisplacement.asset";
        if(AssetDatabase.LoadMainAssetAtPath(meshPath))AssetDatabase.DeleteAsset(meshPath);
        var mesh=new Mesh{name="Real sand displacement coast",indexFormat=IndexFormat.UInt32};
        mesh.vertices=points;mesh.uv=uv;mesh.subMeshCount=2;
        mesh.SetTriangles(dryIndices,0);mesh.SetTriangles(wetIndices,1);
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,meshPath);
        var beach=new GameObject("COAST + DAMP REAL LIBRARY BEACH");
        beach.transform.SetParent(root,false);
        beach.AddComponent<MeshFilter>().sharedMesh=mesh;
        beach.AddComponent<MeshRenderer>().sharedMaterials=new[]{dry,wet};
        beach.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Model(Transform root,string file,string name,Vector3 pos,float size,float yaw,
        bool collision,string variant,bool sizeByHeight,List<Placed> placed)
    {
        var path=Models+file;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ??AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!prefab)throw new InvalidOperationException("glTFast model import failed: "+path);
        var anchor=new GameObject("LIBRARY SOURCE - "+name).transform;
        anchor.SetParent(root,false);
        var instance=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!instance)instance=UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetParent(anchor,false);
        instance.transform.localPosition=Vector3.zero;
        instance.transform.localRotation=Quaternion.identity;
        if(variant!=null)
        {
            var variants=instance.GetComponentsInChildren<Transform>(true)
                .Where(t=>t.name.StartsWith("material_gameasset.",StringComparison.Ordinal)
                    && t.name.Length=="material_gameasset.001".Length).ToArray();
            if(variants.Length!=5 || variants.All(v=>v.name!=variant))
                throw new InvalidOperationException("Jet ski variant names changed; no fallback");
            foreach(var v in variants)v.gameObject.SetActive(v.name==variant);
        }
        var bounds=ModelBounds(instance);
        float length=sizeByHeight?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.z);
        if(length<.001f)throw new InvalidOperationException("Zero-size GLB bounds: "+file);
        anchor.localScale=Vector3.one*(size/length);
        anchor.rotation=Quaternion.Euler(0,yaw,0);
        bounds=ModelBounds(instance);
        anchor.position+=new Vector3(pos.x-bounds.center.x,pos.y-bounds.min.y,pos.z-bounds.center.z);
        if(collision)
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>(false))
            {
                if(!filter.sharedMesh || filter.GetComponent<MeshCollider>())continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            }
        if(name.StartsWith("Jet ski") || name.StartsWith("Yacht") || name.StartsWith("Motorboat"))
            anchor.gameObject.AddComponent<BraxWatercraftMooring>();
        placed.Add(new Placed{id=name,model=file,position=pos,metres=size});
    }
    static Bounds ModelBounds(GameObject instance)
    {
        var rs=instance.GetComponentsInChildren<Renderer>(false);
        if(rs.Length==0)throw new InvalidOperationException("Source model has no rendered meshes");
        var b=rs[0].bounds;
        for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
        return b;
    }
}

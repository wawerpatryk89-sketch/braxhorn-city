using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Select actual named FBX objects, including their authored supported loads.
// Never ground a whole collection: its yoga mats are authored at -1 metre.
public static class BraxObozGymEquipment
{
    const float K=1.294f;
    public const string SourcePath="Assets/Source/Prywatne/Oboz/OwnerAssets/OwnerExtras/Gym/gym_environment.fbx";
    public const string AtlasPath="Assets/Source/Prywatne/Oboz/OwnerAssets/OwnerExtras/Gym/gym_environment.png";
    public const string SourceSha256="e28306bd6ffbb1f59b72fe9b1a659f912dfd2ec2ba9c34d36057f1a6d5b046ec";
    const string Folder="Assets/PlayableGenerated/Oboz/Environment/TrainingAssets";
    static Material sharedMaterial;
    static string atlasHash,verifiedSourceDependencies;

    public static string[] Parts(string item)
    {
        if(item=="bench_press")return new[]{"weight_bench","barbell_bar"}
            .Concat(Enumerable.Range(1,10).Select(i=>"weight_plate_"+i)).ToArray();
        if(item=="loaded_weight_rack")return new[]{"weight_rack"}
            .Concat(Enumerable.Range(4,5).Select(i=>"dumbbell_"+i))
            .Concat(Enumerable.Range(11,5).Select(i=>"weight_plate_"+i)).ToArray();
        // Exact names are validated against the real prefab before placement.
        return new[]{item};
    }

    static Material Material()
    {
        var importer=AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Brak atlasu oryginalnej siłowni: "+AtlasPath);
        // BraxObozImport's exact filename rule keeps these values on reimport.
        if(importer.textureType!=TextureImporterType.Default ||
            importer.textureShape!=TextureImporterShape.Texture2D || !importer.sRGBTexture)
        {
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=true;importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
        importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        if(width!=2048 || height!=2048)BraxBuildQuality.Warn(
            "OBÓZ ORYGINALNY ATLAS SIŁOWNI: oczekiwano źródła 2048x2048, odczytano "+width+"x"+height);
        string hash=AssetDatabase.GetAssetDependencyHash(AtlasPath).ToString();
        if(sharedMaterial && atlasHash==hash)return sharedMaterial;
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var shader=Shader.Find("Standard");
        if(!atlas || !shader)throw new InvalidOperationException("Brak tekstury lub Standard shader dla oryginalnego FBX siłowni");
        if(!Directory.Exists(Folder)){Directory.CreateDirectory(Folder);AssetDatabase.Refresh();}
        string path=Folder+"/OwnerGymAtlas-"+hash+".mat";
        sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!sharedMaterial)
        {
            sharedMaterial=new Material(shader){name="Siłownia właściciela / oryginalny atlas 2K",mainTexture=atlas,enableInstancing=true};
            sharedMaterial.SetFloat("_Glossiness",.28f);
            AssetDatabase.CreateAsset(sharedMaterial,path);
        }
        atlasHash=hash;return sharedMaterial;
    }

    public static Transform Place(Transform parent,string item,Vector3 floorLocalMetres,float yaw=0f)
    {
        string dependencies=AssetDatabase.GetAssetDependencyHash(SourcePath).ToString();
        if(verifiedSourceDependencies!=dependencies)
        {
            using(var bytes=File.OpenRead(SourcePath))
            using(var sha=SHA256.Create())
                if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=SourceSha256)
                    throw new InvalidOperationException("FBX siłowni nie odpowiada zmierzonemu oryginałowi SHA-256");
            verifiedSourceDependencies=dependencies;
        }
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if(!source)throw new InvalidOperationException("Brak oryginalnego gym_environment.fbx; bez zastępstw: "+item);
        GameObject model=null;
        try
        {
            // A disconnected instance keeps shared imported meshes and their
            // UVs, with all authored ancestor transforms preserved.
            model=Object.Instantiate(source);
            if(PrefabUtility.IsPartOfPrefabInstance(model))
                PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            model.name="Właściciel / gym_environment / "+item;
            model.transform.SetParent(parent,false);
            model.transform.localRotation=Quaternion.Euler(0,yaw,0)*model.transform.localRotation;
            var all=model.GetComponentsInChildren<MeshFilter>(true);
            var wanted=Parts(item);var selected=new List<MeshFilter>();
            foreach(string part in wanted)
            {
                var matches=all.Where(f=>f.name==part && f.sharedMesh && f.GetComponent<MeshRenderer>()).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("W źródłowym FBX brak jednoznacznej części: "+part);
                selected.Add(matches[0]);
            }
            var keep=new HashSet<Transform>{model.transform};
            foreach(var filter in selected)
                for(var node=filter.transform;node && node!=model.transform;node=node.parent)keep.Add(node);
            var discard=model.GetComponentsInChildren<Transform>(true)
                .Where(t=>t!=model.transform && !keep.Contains(t) && keep.Contains(t.parent)).ToArray();
            foreach(var node in discard)Object.DestroyImmediate(node.gameObject);
            var material=Material();
            foreach(var filter in selected)
            {
                var renderer=filter.GetComponent<MeshRenderer>();renderer.enabled=true;
                renderer.sharedMaterials=Enumerable.Repeat(material,filter.sharedMesh.subMeshCount).ToArray();
                // Static equipment uses its actual low-poly triangles. A solid
                // box around a pull-up frame would block the space underneath.
                foreach(var old in filter.GetComponents<Collider>())Object.DestroyImmediate(old);
                var collider=filter.gameObject.AddComponent<MeshCollider>();
                collider.convex=false;collider.sharedMesh=filter.sharedMesh;
            }
            var renderers=selected.Select(f=>(Renderer)f.GetComponent<MeshRenderer>()).ToArray();
            BraxObozPlacement.NativeMetres(model.transform,renderers,"gym_environment / "+item);
            if(!BraxObozPlacement.OnGround(model.transform,renderers,
                parent.TransformPoint(floorLocalMetres*K),"gym_environment / "+item))
                throw new InvalidOperationException("Brak rzeczywistej podłogi dla oryginalnego urządzenia: "+item);
            var record=BraxObozPlacementRecord.For(model.transform);
            var geometry=model.AddComponent<BraxObozEquipmentGeometry>();
            geometry.sourcePath=SourcePath;geometry.sourceSha256=SourceSha256;
            geometry.sourceParts=wanted;geometry.geometry=selected.ToArray();geometry.floorSupport=record.verticalSupport;
            if(!geometry.TryMeasureBounds(out var actual))
                throw new InvalidOperationException("Nie odczytano rzeczywistych wierzchołków urządzenia: "+item);
            Debug.Log("BRAXHORN_OBOZ_GYM_PART item="+item+" originalParts="+string.Join(",",wanted)+
                " boundsMetres="+(actual.size/K)+" sourceUnchanged=true");
            return model.transform;
        }
        catch{if(model)Object.DestroyImmediate(model);throw;}
    }
}

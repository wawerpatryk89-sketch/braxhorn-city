using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Do not generate hundreds of city meshes only to discover an original
// premium GLB, Brax rig or Mixamo clip was missing after imports.
// This is a source-integrity smoke test, NOT a physical/visual approval gate.
public static class BraxChampionsSourceReadiness
{
    const string Base="Assets/Source/Mistrzowie/";
    // Match the original Brax clip lookup, which tolerates punctuation in FBX labels.
    static string ClipKey(string name) => new string((name ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    static GameObject RealModel(string path,string label)
    {
        if(!File.Exists(path))
            throw new FileNotFoundException("Champions original 3D source file absent: "+label,path);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!prefab)
            throw new InvalidOperationException("Champions original 3D source has no imported prefab: "+label+" path="+path);
        var meshes=prefab.GetComponentsInChildren<MeshFilter>(true)
            .Where(f=>f.sharedMesh && f.sharedMesh.vertexCount>0).Count();
        var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(f=>f.sharedMesh && f.sharedMesh.vertexCount>0).Count();
        if(meshes+skins==0)
            throw new InvalidOperationException("Champions original 3D source imported without visible meshes: "+label+" path="+path);
        return prefab;
    }

    public static void VerifyBeforeScene()
    {
        // Original estate vehicle: meshopt/KTX decoder packages must actually
        // have imported its geometry, not merely be listed in manifest.json.
        RealModel(BraxChampionsCarBuilder.OwnerCar,"estate car_rev.glb");
        RealModel(Base+"CasinoT11/Vegetation2K/shrub_03/shrub_03_2k_embedded.glb",
            "Poly Haven Shrub 03 lossless source");
        RealModel(Base+"CasinoT11/Vegetation2K/quiver_tree_02/quiver_tree_02_2k_embedded.glb",
            "Poly Haven Quiver Tree 02 lossless source");

        var brax=Base+"Brax/BraxOriginal.glb";
        var hero=RealModel(brax,"unchanged Brax");
        BraxChampionsCarClips.VerifyRig(hero);
        var clips=AssetDatabase.LoadAllAssetsAtPath(brax).OfType<AnimationClip>().ToArray();
        foreach(string clip in new[]{"Idle","Walking","Running","Jab Cross (1)","Mma Kick (1)","Jump"})
            if(!clips.Any(c=>c && ClipKey(c.name)==ClipKey(clip)))
                throw new InvalidOperationException("Champions Brax original missing animation: "+clip);

        foreach(string file in new[]{"Entering Car.fbx","Driving.fbx","Exiting Car.fbx"})
        {
            var path=Base+"Brax/CarAnimations/"+file;
            if(!File.Exists(path))
                throw new FileNotFoundException("Approved Mixamo source not restored: "+path,path);
            var original=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c=>c && c.length>.01f && AnimationUtility.GetCurveBindings(c).Length>0);
            if(!original)
                throw new InvalidOperationException("Approved Mixamo FBX has no importable animation curves: "+path);
        }

        if(!BraxChampionsVIPSeats.HasSource)
            BraxBuildQuality.Warn("Premium VIP original not imported; retain old seating for test review");
        Debug.Log("CHAMPIONS_ORIGINALS_PREFLIGHT_PASS car=1 plants=2 brax=1 basic_clips=6 mixamo=3");
    }
}

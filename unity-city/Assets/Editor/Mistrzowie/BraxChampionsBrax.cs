using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Owner decision: use the city's original Brax, with clips already on his own rig.
public static class BraxChampionsBrax
{
    const string Source = "Assets/Source/Mistrzowie/Brax/";
    const string Generated = "Assets/PlayableGenerated/Mistrzowie/Brax";
    static string Clean(string name) => new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    static Texture2D Map(string file, bool linear, bool normal)
    {
        string path = Source + file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new IOException("Missing city Brax texture: " + path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !linear && !normal;
        importer.mipmapEnabled = true; importer.anisoLevel = 8;
        importer.maxTextureSize = 4096;
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true; android.maxTextureSize = 4096;
        android.format = TextureImporterFormat.ASTC_4x4; android.compressionQuality = 100;
        importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!texture) throw new IOException("City Brax texture import failed: " + path);
        return texture;
    }
    public static GameObject Create(Transform parent, CharacterController body)
    {
        string path = Source + "BraxOriginal.glb";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if (!prefab) throw new IOException("Original city Brax model not imported: " + path);
        Directory.CreateDirectory(Generated); AssetDatabase.Refresh();
        var hero = Object.Instantiate(prefab); hero.name = "Brax";
        hero.transform.SetParent(parent, false); hero.transform.localScale = Vector3.one * .024f;
        foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
        var shader = Shader.Find("Braxhorn/HeroPremium");
        if (!shader) throw new IOException("City Brax HeroPremium shader missing");
        var material = new Material(shader) { name = "Brax original city PBR", color = Color.white };
        material.SetTexture("_MainTex", Map("BraxAlbedo.png", false, false));
        material.SetTexture("_BumpMap", Map("BraxNormal.png", true, true));
        material.SetTexture("_Orm", Map("BraxOrm.png", true, false));
        material.SetFloat("_BumpScale", .85f); material.SetFloat("_RoughnessFloor", .42f);
        material.SetFloat("_MetallicScale", .30f); material.SetFloat("_OcclusionStrength", .8f);
        BraxZapisAssetu.Utworz(material, Generated + "/BraxSourceORM.mat");
        foreach (var skin in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.sharedMaterials = Enumerable.Repeat(material, skin.sharedMaterials.Length).ToArray();
            skin.updateWhenOffscreen = true; skin.shadowCastingMode = ShadowCastingMode.Off;
        }
        var animation = hero.GetComponentInChildren<Animation>(true);
        if (!animation) animation = hero.AddComponent<Animation>();
        animation.playAutomatically = false;
        var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
        var mapping = new Dictionary<string, string> { {"Idle","Idle"}, {"Walk","Walking"},
            {"Run","Running"}, {"Punch","Jab Cross (1)"}, {"Kick","Mma Kick (1)"}, {"Jump","Jump"} };
        foreach (var pair in mapping)
        {
            var source = imported.FirstOrDefault(c => c.name == pair.Value)
                ?? imported.FirstOrDefault(c => Clean(c.name) == Clean(pair.Value));
            if (!source) throw new IOException("City Brax source animation not imported: " + pair.Value);
            var clip = Object.Instantiate(source); clip.name = pair.Key; clip.legacy = true;
            clip.wrapMode = pair.Key == "Idle" || pair.Key == "Walk" || pair.Key == "Run" ? WrapMode.Loop : WrapMode.Once;
            BraxZapisAssetu.Utworz(clip, Generated + "/" + pair.Key + ".anim");
            animation.AddClip(clip, pair.Key);
        }
        // Three ORIGINAL owner Mixamo clips re-bound to Brax's existing skeleton.
        // No changes to Idle/Walk/Run/Punch/Kick/Jump or model proportions.
        BraxChampionsCarClips.Attach(hero, animation);
        animation.clip = animation.GetClip("Idle"); animation.Play("Idle"); animation.Sample();
        body.height = 2.25f; body.radius = .35f; body.center = new Vector3(0, 1.125f, 0);
        Debug.Log("BRAX_MISTRZOWIE_PLAYER Brax; city model/rig/clips; no Patryk retarget; device review pending");
        return hero;
    }
}

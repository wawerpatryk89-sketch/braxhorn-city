using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxObozMaterials
{
    const string Root = "Assets/Source/Prywatne/Oboz/environment/";
    static Texture2D Map(string path, bool normal, bool color)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Brak mapy materiału: " + path);
        bool changed = importer.sRGBTexture != color || importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default);
        importer.sRGBTexture = color; importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static Material Pbr(string id)
    {
        string folder = Root + "materials/" + id;
        if (!Directory.Exists(folder)) throw new InvalidOperationException("Brak oryginalnych map: " + id);
        string Find(string kind) => Directory.GetFiles(folder, "*_" + kind + ".jpg").SingleOrDefault();
        var material = new Material(Shader.Find("Braxhorn/ObozSurface")) { name = "Obóz / " + id, enableInstancing = true };
        material.SetTexture("_MainTex", Map(Find("Color"), false, true));
        material.SetTexture("_BumpMap", Map(Find("NormalGL"), true, false));
        material.SetTexture("_RoughnessTex", Map(Find("Roughness"), false, false));
        var metal = Find("Metalness"); if (metal != null) material.SetTexture("_MetallicTex", Map(metal, false, false));
        material.SetFloat("_MetallicFactor", metal == null ? 0 : 1);
        var ao = Find("AmbientOcclusion"); if (ao != null) material.SetTexture("_OcclusionTex", Map(ao, false, false));
        return Save(material, id);
    }
    public static Material ForestGround()
    {
        var material = new Material(Shader.Find("Braxhorn/ObozSurface")) { name = "Obóz / oryginalne igliwie", enableInstancing = true };
        material.SetTexture("_MainTex", Map(Root + "forest-ground/image_0.jpg", false, true));
        material.SetTexture("_BumpMap", Map(Root + "forest-ground/image_2.jpg", true, false));
        material.SetTexture("_RoughnessTex", Map(Root + "forest-ground/image_1.png", false, false));
        material.SetTexture("_OcclusionTex", Map(Root + "forest-ground/image_1.png", false, false));
        material.SetFloat("_PackedORM", 1); material.SetFloat("_RoughnessFactor", .7f); material.SetFloat("_MetallicFactor", 0);
        return Save(material, "forest-ground");
    }
    public static Material PublicPbr(string id,float widthMetres)
    {
        string folder="Assets/Source/Public/Oboz/"+id+"/";
        var material=new Material(Shader.Find("Braxhorn/ObozSurface")){name="Poly Haven / "+id,enableInstancing=true};
        foreach(var pair in new[]{("Color","_MainTex"),("NormalGL","_BumpMap"),("Roughness","_RoughnessTex"),("AmbientOcclusion","_OcclusionTex")})
        {
            string path=folder+pair.Item1+".jpg";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Pobierz zweryfikowane mapy Poly Haven: "+path);
            importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.anisoLevel=8;
            var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_4x4;
            importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
            material.SetTexture(pair.Item2,Map(path,pair.Item1=="NormalGL",pair.Item1=="Color"));
        }
        material.SetFloat("_MetallicFactor",0);
        material.mainTextureScale=Vector2.one/widthMetres;
        return Save(material,"polyhaven-"+id);
    }
    public static Material SignText(Font font)
    {
        var shader=Shader.Find("Braxhorn/CitySignText");
        if(!shader)throw new InvalidOperationException("Brak shadera tablic obozu");
        return Save(new Material(shader){name="Obóz / napisy",mainTexture=font.material.mainTexture},"campus-napisy");
    }
    public static Material Plain(string id, Color color, float smooth = .25f, float metallic = 0)
    {
        var material = new Material(Shader.Find("Standard")) { name = "Obóz / " + id, color = color, enableInstancing = true };
        material.SetFloat("_Glossiness", smooth); material.SetFloat("_Metallic", metallic);
        material.DisableKeyword("_EMISSION"); return Save(material, id);
    }
    static Material Save(Material material, string id)
    {
        string folder = BraxObozCharacters.Generated + "/Materials";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        string path = folder + "/" + id + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); return existing; }
        AssetDatabase.CreateAsset(material, path); return material;
    }
}

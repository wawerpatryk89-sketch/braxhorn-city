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
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Brak wymaganej mapy PBR obozu");
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Brak mapy materiału: " + path);
        bool changed = importer.textureShape != TextureImporterShape.Texture2D || importer.sRGBTexture != color || importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = color; importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (changed) importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!texture) throw new InvalidOperationException("Nie zaimportowano mapy PBR jako Texture2D: " + path +
            "; shape=" + importer.textureShape + "; mainAsset=" + AssetDatabase.LoadMainAssetAtPath(path)?.GetType().FullName);
        return texture;
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
    // Shared by the postprocessor and material authoring: no conflicting reimports.
    public static bool ConfigurePublicMap(TextureImporter importer, string kind)
    {
        bool color=kind=="Color",normal=kind=="NormalGL";
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        var android=importer.GetPlatformTextureSettings("Android");
        bool changed=importer.textureShape!=TextureImporterShape.Texture2D || importer.maxTextureSize!=4096 || !importer.mipmapEnabled || importer.anisoLevel!=8 ||
            importer.wrapMode!=TextureWrapMode.Repeat || importer.filterMode!=FilterMode.Trilinear ||
            importer.textureCompression!=TextureImporterCompression.CompressedHQ || importer.compressionQuality!=90 ||
            importer.sRGBTexture!=color || importer.textureType!=type || !android.overridden ||
            android.maxTextureSize!=4096 || android.format!=TextureImporterFormat.ASTC_4x4 || android.compressionQuality!=90;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.anisoLevel=8;
        importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.compressionQuality=90;importer.sRGBTexture=color;importer.textureType=type;
        android.name="Android";android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=90;
        importer.SetPlatformTextureSettings(android);
        return changed;
    }
    public static Material PublicPbr(string id,float widthMetres)
    {
        if (widthMetres <= 0 || float.IsNaN(widthMetres) || float.IsInfinity(widthMetres))
            throw new ArgumentOutOfRangeException(nameof(widthMetres));
        string folder="Assets/Source/Public/Oboz/"+id+"/";
        var material=new Material(Shader.Find("Braxhorn/ObozSurface")){name="Obóz / publiczne PBR / "+id,enableInstancing=true};
        foreach(var pair in new[]{("Color","_MainTex"),("NormalGL","_BumpMap"),("Roughness","_RoughnessTex"),("AmbientOcclusion","_OcclusionTex")})
        {
            string path=folder+pair.Item1+".jpg";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Brak zweryfikowanej mapy PBR obozu: "+path);
            if(ConfigurePublicMap(importer,pair.Item1))importer.SaveAndReimport();
            bool color=pair.Item1=="Color",normal=pair.Item1=="NormalGL";
            material.SetTexture(pair.Item2,Map(path,normal,color));
        }
        material.SetFloat("_MetallicFactor",0);
        material.mainTextureScale=Vector2.one/widthMetres;
        return Save(material,"polyhaven-"+id);
    }
    public static Material CampGround(Material forest, Material aggregate)
    {
        if (!forest || !aggregate) throw new InvalidOperationException("Brak map podłoża obozu");
        var lawn = PublicPbr("Grass001",1.4f);
        var material = new Material(forest) { name = "Obóz / trawnik, ściółka i żwirowe obrzeża" };
        foreach (var pair in new[]{("_MainTex","_GroundColor"),("_BumpMap","_GroundNormal"),
            ("_RoughnessTex","_GroundRoughness"),("_OcclusionTex","_GroundAO")})
        {
            var map = aggregate.GetTexture(pair.Item1);
            // Optional AO uses the shader's white default; colour and normal cannot be absent.
            if (!map && pair.Item1 != "_OcclusionTex")
                throw new InvalidOperationException("Brak mapy żwiru: " + pair.Item1);
            if (map) material.SetTexture(pair.Item2, map);
        }
        foreach (var pair in new[]{("_MainTex","_LawnColor"),("_BumpMap","_LawnNormal"),
            ("_RoughnessTex","_LawnRoughness"),("_OcclusionTex","_LawnAO")})
            material.SetTexture(pair.Item2,lawn.GetTexture(pair.Item1));
        material.mainTextureScale = Vector2.one;
        material.SetFloat("_GroundUVScale", 2);
        material.SetFloat("_LawnUVScale", 3f/1.4f);
        material.EnableKeyword("OBOZ_GROUND_BLEND");
        return Save(material,"camp-ground-blended");
    }
    public static Material SignText(Font font)
    {
        var shader=Shader.Find("Braxhorn/CitySignText");
        if(!shader)throw new InvalidOperationException("Brak shadera tablic obozu");
        return Save(new Material(shader){name="Obóz / napisy",mainTexture=font.material.mainTexture},"campus-napisy");
    }
    public static Material Stream()
    {
        var shader=Shader.Find("Braxhorn/ObozStream");
        if(!shader)throw new InvalidOperationException("Brak shadera strumienia obozu");
        var material=new Material(shader){name="Obóz / płynący strumień"};
        material.SetColor("_Color",new Color(.176f,.353f,.353f,.72f));
        material.SetFloat("_Smoothness",.88f);material.SetFloat("_RippleStrength",.055f);
        return Save(material,"stream-water");
    }
    public static Material Plain(string id, Color color, float smooth = .25f, float metallic = 0)
    {
        var material = new Material(Shader.Find("Standard")) { name = "Obóz / " + id, color = color, enableInstancing = true };
        material.SetFloat("_Glossiness", smooth); material.SetFloat("_Metallic", metallic);
        material.DisableKeyword("_EMISSION"); return Save(material, id);
    }
    public static void ValidateHierarchy(Transform root)
    {
        var all=root.GetComponentsInChildren<Renderer>(true);
        if(all.Any(r=>r.sharedMaterials.Length==0 || r.sharedMaterials.Any(m=>!m)))
            throw new InvalidOperationException("Obóz zawiera renderer bez materiału");
        var materials=all.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        foreach(var material in materials)
        {
            if(!material.shader || ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("Błąd shadera materiału obozu: "+material.name);
            if(material.shader.name!="Braxhorn/ObozSurface")continue;
            foreach(string map in new[]{"_MainTex","_BumpMap","_RoughnessTex"})
                if(!material.GetTexture(map))throw new InvalidOperationException(material.name+": brak "+map);
            if(!material.IsKeywordEnabled("OBOZ_GROUND_BLEND"))continue;
            foreach(string map in new[]{"_GroundColor","_GroundNormal","_GroundRoughness",
                "_LawnColor","_LawnNormal","_LawnRoughness","_LawnAO"})
                if(!material.GetTexture(map))throw new InvalidOperationException(material.name+": brak "+map);
        }
        Debug.Log("BRAXHORN_OBOZ materiały: renderery="+all.Length+" materiały="+materials.Length+
            "; sprawdzono referencje i błędy shaderów, odbiór wizualny nadal wymagany");
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


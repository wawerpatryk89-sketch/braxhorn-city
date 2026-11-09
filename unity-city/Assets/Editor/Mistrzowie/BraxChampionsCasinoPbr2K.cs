using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Native Poly Haven 2K PBR for T11 only. A1/metro/other original assets
// retain their imported materials and GUIDs untouched.
public static class BraxChampionsCasinoPbr2K
{
    const string Source="Assets/Source/Mistrzowie/CasinoT11/Textures2K/";
    // Physical 2048px image and Unity's imported Android Texture2D are distinct
    // concepts: platform overrides can reduce the latter without losing source.
    static Texture2D OriginalMap(string slug,string kind,string extension,bool colour)
    {
        string path=Source+slug+"_"+kind+"_2k."+extension;
        if(!File.Exists(path))
            throw new FileNotFoundException("Missing ORIGINAL Poly Haven 2K file: "+path,path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)
            throw new InvalidOperationException("Original T11 image exists but TextureImporter is unavailable: "+path);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth!=2048 || sourceHeight!=2048)
            throw new InvalidOperationException("T11 original is NOT native 2048x2048: "+path+
                " actual="+sourceWidth+"x"+sourceHeight);
        // Previously a stale Library/import artifact returned null from
        // LoadAssetAtPath even though the real Poly Haven JPEG, GUID and
        // 2048px source dimensions existed. Recover the importer in-place:
        // no source bytes, GUIDs, material artwork or dimensions are replaced.
        var android=importer.GetPlatformTextureSettings("Android");
        bool invalidSettings=importer.textureType!=TextureImporterType.Default
            || importer.textureShape!=TextureImporterShape.Texture2D
            || importer.sRGBTexture!=colour || !importer.mipmapEnabled
            || !importer.streamingMipmaps || importer.maxTextureSize!=2048
            || !android.overridden || android.maxTextureSize!=2048
            || android.format!=TextureImporterFormat.ASTC_4x4;
        if(invalidSettings)
        {
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=colour;
            importer.mipmapEnabled=true;
            importer.streamingMipmaps=true;
            importer.maxTextureSize=2048;
            android.name="Android";
            android.overridden=true;
            android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_4x4;
            android.compressionQuality=100;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
            importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)
                throw new InvalidOperationException("T11 importer vanished after reimport: "+path);
            Debug.LogWarning("BRAX_T11_IMPORT_RECOVERY restored native 2K ASTC settings: "+path);
        }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)
        {
            // ForceUpdate ignores stale import-cache freshness. A synchronous
            // refresh then allows Unity to register the resulting main asset.
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                texture=AssetDatabase.LoadMainAssetAtPath(path) as Texture2D;
            }
            if(texture)
                Debug.LogWarning("BRAX_T11_IMPORT_RECOVERY restored Texture2D: "+path);
        }
        if(!texture)
        {
            var main=AssetDatabase.LoadMainAssetAtPath(path);
            throw new InvalidOperationException(
                "Unity could not load original native 2K Texture2D after forced import: "+path+
                " guid="+AssetDatabase.AssetPathToGUID(path)+
                " mainAssetType="+(main?main.GetType().FullName:"NULL")+
                " importedFileBytes="+new FileInfo(path).Length+
                ". Preserve original source; inspect Unity texture-import failure.");
        }
        // Source pixels and requested Android format must stay unchanged.
        android=importer.GetPlatformTextureSettings("Android");
        if(importer.textureShape!=TextureImporterShape.Texture2D ||
           importer.sRGBTexture!=colour || !importer.mipmapEnabled
           || !importer.streamingMipmaps || importer.maxTextureSize!=2048
           || !android.overridden || android.maxTextureSize!=2048
           || android.format!=TextureImporterFormat.ASTC_4x4)
            throw new InvalidOperationException("T11 native 2K Android importer remains invalid: "+path);
        if(texture.width!=2048 || texture.height!=2048)
            BraxBuildQuality.Warn("Original T11 source is native 2K, but Unity imported "+
                texture.width+"x"+texture.height+" at "+path+
                ". Preserve original maps and inspect device rendering.");
        return texture;
    }
    public static void VerifyOriginalSourceMaps()
    {
        // Check ALL 27 native 2K source maps in one Unity pass; never change source image bytes.
        var failures=new List<string>();
        foreach(var slug in new[]{
            "concrete_brick_wall_001","sandstone_blocks_05","floor_tiles_04",
            "rectangular_facade_tiles","granite_tile",
            "clean_asphalt","concrete_pavement_02","concrete_floor_02","fabric_pattern_05"})
        {
            foreach(var kind in new[]{"diff","nor_gl","arm"})
            {
                string extension=kind=="diff"?"jpg":"png";
                try { OriginalMap(slug,kind,extension,kind=="diff"); }
                catch(Exception ex)
                {
                    string issue=slug+"_"+kind+": "+ex.Message;
                    failures.Add(issue);
                    Debug.LogWarning("CHAMPIONS_T11_TEXTURE_IMPORT_ISSUE "+issue);
                }
            }
        }
        if(failures.Count>0)
            throw new InvalidOperationException("T11 source textures invalid ("
                +failures.Count+"/27): "+string.Join(" | ",failures));
        Debug.Log("CHAMPIONS_T11_NATIVE_2K_SOURCE_PREFLIGHT_PASS maps=27 shape=Texture2D");
    }

    public static Material Create(string output,string id,string slug,float metres,Color tint)
    {
        if(metres<=0f)throw new ArgumentOutOfRangeException(nameof(metres));
        Texture2D Load(string kind,string extension,bool colour)
            => OriginalMap(slug,kind,extension,colour);
        var diffuse=Load("diff","jpg",true);
        var normal=Load("nor_gl","png",false);
        var arm=Load("arm","png",false);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("CityStaticPBR shader missing");
        var material=new Material(shader){name="T11 Native Poly Haven 2K - "+slug,enableInstancing=true};
        material.SetTexture("_MainTex",diffuse);
        material.SetTexture("_BumpMap",normal);
        material.SetTexture("_ARM",arm);
        material.SetFloat("_NormalEncoding",0);
        material.SetFloat("_UseARMForAO",1);
        material.SetFloat("_Roughness",1);
        material.SetFloat("_Metallic",1);
        material.SetFloat("_VertexTint",0);
        material.SetColor("_Color",tint);
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        foreach(var channel in new[]{"_MainTex","_BumpMap","_ARM"})
            material.SetTextureScale(channel,Vector2.one/metres);
        AssetDatabase.CreateAsset(material,output+"/Materials/"+id+".mat");
        return material;
    }
}

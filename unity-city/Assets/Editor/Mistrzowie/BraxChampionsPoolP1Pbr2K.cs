using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Native Poly Haven 2K PBR for P1 only. A1/metro/other original assets
// retain their imported materials and GUIDs untouched.
public static class BraxChampionsPoolP1Pbr2K
{
    const string Source="Assets/Source/Mistrzowie/PoolP1/Textures2K/";
    public static Material Create(string output,string id,string slug,float metres,Color tint)
    {
        if(metres<=0f)throw new ArgumentOutOfRangeException(nameof(metres));
        Texture2D Load(string kind,string extension,bool colour)
        {
            string path=Source+slug+"_"+kind+"_2k."+extension;
            if(!File.Exists(path))
                throw new FileNotFoundException("Missing ORIGINAL P1 Poly Haven 2K source: "+path,path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)
                throw new InvalidOperationException("Original P1 source importer unavailable: "+path);
            // Check actual provider pixels, not an Editor/Android mipmap preview.
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
            if(sourceWidth!=2048 || sourceHeight!=2048)
                throw new InvalidOperationException("P1 original is not native 2048x2048: "+path+
                    " source="+sourceWidth+"x"+sourceHeight);
            var android=importer.GetPlatformTextureSettings("Android");
            bool invalidSettings=importer.textureType!=TextureImporterType.Default
                || importer.sRGBTexture!=colour || !importer.mipmapEnabled
                || !importer.streamingMipmaps || importer.maxTextureSize!=2048
                || !android.overridden || android.maxTextureSize!=2048
                || android.format!=TextureImporterFormat.ASTC_4x4;
            if(invalidSettings)
            {
                // Repair metadata only. Original file bytes and asset GUID stay intact.
                importer.textureType=TextureImporterType.Default;
                importer.sRGBTexture=colour;importer.mipmapEnabled=true;
                importer.streamingMipmaps=true;importer.maxTextureSize=2048;
                android.name="Android";android.overridden=true;
                android.maxTextureSize=2048;
                android.format=TextureImporterFormat.ASTC_4x4;
                android.compressionQuality=100;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
                importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)
                    throw new InvalidOperationException("P1 importer vanished after recovery: "+path);
                Debug.LogWarning("BRAX_P1_IMPORT_RECOVERY restored source 2K ASTC settings: "+path);
            }
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture)
            {
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(!texture)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    texture=AssetDatabase.LoadMainAssetAtPath(path) as Texture2D;
                }
                if(texture)
                    Debug.LogWarning("BRAX_P1_IMPORT_RECOVERY restored source Texture2D: "+path);
            }
            if(!texture)
            {
                var main=AssetDatabase.LoadMainAssetAtPath(path);
                throw new InvalidOperationException("Unity could not import real original P1 2K Texture2D: "+path+
                    " guid="+AssetDatabase.AssetPathToGUID(path)+
                    " mainAssetType="+(main?main.GetType().FullName:"NULL")+
                    " bytes="+new FileInfo(path).Length);
            }
            android=importer.GetPlatformTextureSettings("Android");
            if(importer.sRGBTexture!=colour || !importer.mipmapEnabled
               || !importer.streamingMipmaps || importer.maxTextureSize!=2048
               || !android.overridden || android.maxTextureSize!=2048
               || android.format!=TextureImporterFormat.ASTC_4x4)
                throw new InvalidOperationException("P1 Android native 2K import remains invalid: "+path);
            if(texture.width!=2048 || texture.height!=2048)
                BraxBuildQuality.Warn("P1 Poly Haven source is native 2K but Unity preview is "+
                    texture.width+"x"+texture.height+" at "+path+
                    "; preserve originals and inspect actual Android ASTC.");
            return texture;
        }
        var diffuse=Load("diff","jpg",true);
        var normal=Load("nor_gl","png",false);
        var arm=Load("arm","png",false);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("CityStaticPBR shader missing");
        var material=new Material(shader){name="P1 Native Poly Haven 2K - "+slug,enableInstancing=true};
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

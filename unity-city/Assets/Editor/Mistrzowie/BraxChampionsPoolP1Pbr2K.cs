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
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!texture || texture.width!=2048 || texture.height!=2048)
                throw new FileNotFoundException("P1 requires native Poly Haven 2048x2048 PBR: "+path,path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer || importer.sRGBTexture!=colour || !importer.mipmapEnabled
               || !importer.streamingMipmaps || importer.maxTextureSize!=2048)
                throw new InvalidOperationException("Invalid P1 2K source import: "+path);
            var android=importer.GetPlatformTextureSettings("Android");
            if(!android.overridden || android.maxTextureSize!=2048
               || android.format!=TextureImporterFormat.ASTC_4x4)
                throw new InvalidOperationException("P1 Android ASTC 2K import missing: "+path);
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

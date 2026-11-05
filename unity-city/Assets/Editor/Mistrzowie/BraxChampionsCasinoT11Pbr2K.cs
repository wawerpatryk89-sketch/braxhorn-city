using System;
using UnityEditor;
using UnityEngine;

// Only T11: 2048px original Poly Haven materials and no new 1K/4K downloads.
// The other Champions buildings keep their existing PBR materials unchanged.
public static class BraxChampionsCasinoT11Pbr2K
{
    const string SourceRoot="Assets/Source/Mistrzowie/CasinoT11/Textures2K/";

    static Texture2D Texture(string slug,string role,string extension)
    {
        string path=SourceRoot+slug+"_"+role+"_2k."+extension;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)throw new InvalidOperationException(
            "Casino T11 missing original Poly Haven 2K "+path+
            ". Run prepare_casino_t11_2k.py in the source preparation step.");
        if(texture.width!=2048||texture.height!=2048)
            throw new InvalidOperationException("Casino T11 must use exactly 2048px originals: "+path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer||!importer.mipmapEnabled)
            throw new InvalidOperationException("Casino T11 requires mipmapped original 2K: "+path);
        return texture;
    }

    public static Material Material(string id,string slug,float metres,Color tint,string output)
    {
        if(metres<=0)throw new ArgumentOutOfRangeException(nameof(metres));
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Existing CityStaticPBR shader missing");
        var m=new Material(shader){name="T11 original Poly Haven 2K "+slug,enableInstancing=true};
        m.SetColor("_Color",tint);
        m.SetFloat("_VertexTint",0);
        m.SetTexture("_MainTex",Texture(slug,"diff","jpg"));
        m.SetTexture("_BumpMap",Texture(slug,"nor_gl","png"));
        m.SetTexture("_ARM",Texture(slug,"arm","png"));
        m.SetFloat("_NormalEncoding",0);
        m.SetFloat("_UseARMForAO",1);
        m.SetFloat("_Roughness",1);
        m.SetFloat("_Metallic",1);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        foreach(var property in new[]{"_MainTex","_BumpMap","_ARM"})
            m.SetTextureScale(property,Vector2.one/metres);
        AssetDatabase.CreateAsset(m,output+"/Materials/T11_"+id+".mat");
        return m;
    }
}

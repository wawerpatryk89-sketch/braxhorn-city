using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>City-only panoramic sky from the user's Poly Haven library asset.</summary>
public static class BraxCitySkyAuthoring
{
    const int Width=2048,Height=1024;
    const string DirectoryPath="Assets/PlayableGenerated/CityRebuild/Textures";
    const string MaterialPath="Assets/PlayableGenerated/CityRebuild/Materials";
    const string TexturePath="Assets/CityRebuild/Waterfront/daylight-sky.jpg";
    const string SkyPath=MaterialPath+"/CityDaylightCloudsSky.mat";

    public static void ApplyToCity(Camera camera)
    {
        if(!camera)throw new ArgumentNullException(nameof(camera));
        var shader=Shader.Find("Skybox/Panoramic");
        if(!shader)throw new InvalidOperationException("Unity 6 Built-in panoramic skybox shader is unavailable");
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(MaterialPath);
        if(!File.Exists(TexturePath))
            throw new FileNotFoundException("User library sky derivative missing",TexturePath);
        AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if(!importer)throw new InvalidOperationException("City sky texture importer missing");
        importer.textureType=TextureImporterType.Default;
        importer.sRGBTexture=true;
        importer.mipmapEnabled=true;
        importer.wrapModeU=TextureWrapMode.Repeat;
        importer.wrapModeV=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Trilinear;
        importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Compressed;
        importer.maxTextureSize=Width;
        var web=importer.GetPlatformTextureSettings("WebGL");
        web.name="WebGL";web.overridden=true;web.maxTextureSize=Width;
        web.format=TextureImporterFormat.ETC2_RGB4;web.compressionQuality=100;
        importer.SetPlatformTextureSettings(web);
        importer.SaveAndReimport();
        var sky=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if(!sky)throw new InvalidOperationException("City cloud sky did not import");
        var previous=AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if(previous)AssetDatabase.DeleteAsset(SkyPath);
        var mat=new Material(shader){name="BRAXHORN CITY library Kloofendal partly cloudy sky"};
        mat.SetTexture("_MainTex",sky);
        if(mat.HasProperty("_Tint"))mat.SetColor("_Tint",new Color(.5f,.5f,.5f,.5f));
        if(mat.HasProperty("_Exposure"))mat.SetFloat("_Exposure",1.0f);
        if(mat.HasProperty("_Rotation"))mat.SetFloat("_Rotation",0f);
        if(mat.HasProperty("_ImageType"))mat.SetFloat("_ImageType",0f);
        if(mat.HasProperty("_Mapping"))mat.SetFloat("_Mapping",0f);
        AssetDatabase.CreateAsset(mat,SkyPath);
        RenderSettings.skybox=mat;
        camera.clearFlags=CameraClearFlags.Skybox;
        camera.backgroundColor=new Color(.40f,.59f,.73f);
        RenderSettings.fog=false;
        AssetDatabase.SaveAssets();
        Debug.Log("CITY_LIBRARY_SKY source=kloofendal_48d_partly_cloudy_puresky_8k.exr runtime=2048x1024 SDR no-bloom visualAcceptance=pending");
    }
}

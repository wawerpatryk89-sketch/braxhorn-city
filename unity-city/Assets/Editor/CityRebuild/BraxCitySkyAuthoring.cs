using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One pre-baked 1024x512 mobile-friendly panoramic sky texture. It has
/// actual cloud silhouettes and a restrained sun disc, but no full-screen
/// procedural fragment noise, sky HDR bloom or another real-time light.
/// This changes only the generated city scene, never the gym or estate.
/// </summary>
public static class BraxCitySkyAuthoring
{
    const int Width=1024,Height=512;
    const string DirectoryPath="Assets/PlayableGenerated/CityRebuild/Textures";
    const string MaterialPath="Assets/PlayableGenerated/CityRebuild/Materials";
    const string TexturePath=DirectoryPath+"/CityDaylightClouds_1k.png";
    const string SkyPath=MaterialPath+"/CityDaylightCloudsSky.mat";

    static float Smooth(float x)
    {
        x=Mathf.Clamp01(x);return x*x*(3-2*x);
    }

    static Color Sample(int ix,int iy,Vector3 towardsSun)
    {
        // Longitude is periodic in the world-space direction, hence there
        // is no visible 360° seam after the sky panorama is mapped by Unity.
        float u=(ix+.5f)/Width, v=(iy+.5f)/Height;
        float phi=(u-.5f)*Mathf.PI*2f;
        float elevation=(v-.5f)*Mathf.PI;
        float y=Mathf.Sin(elevation),horizontal=Mathf.Cos(elevation);
        var dir=new Vector3(Mathf.Sin(phi)*horizontal,y,Mathf.Cos(phi)*horizontal);
        float upper=Smooth((y+.12f)*.87f);
        Color low=new Color(.70f,.77f,.81f);
        Color high=new Color(.19f,.40f,.61f);
        Color sky=Color.Lerp(low,high,Mathf.Pow(upper,.61f));
        // 3D-direction-domain fractal noise is baked once in the editor.
        // An extra vertical term causes broad, irregular cumulus and finer
        // filament clouds without runtime noise shaders.
        float a=Mathf.PerlinNoise(18.7f+dir.x*3.45f+v*.7f,37.4f+dir.z*3.45f+v*.58f);
        float b=Mathf.PerlinNoise(2.1f+dir.x*8.2f+v*3.1f,60.8f+dir.z*8.2f-v*1.2f);
        float c=Mathf.PerlinNoise(91.7f+dir.x*18.1f-v*2.1f,8.3f+dir.z*18.1f+v*3.2f);
        float n=a*.62f+b*.28f+c*.10f;
        float cloud=Smooth((n-.465f)/.15f);
        float altitude=Smooth((y+.16f)*2.4f);
        float cloudAlpha=cloud*altitude*.82f;
        float shade=Smooth((a-.34f)/.38f);
        Color cloudBase=Color.Lerp(new Color(.70f,.77f,.81f),new Color(.94f,.95f,.93f),shade);
        sky=Color.Lerp(sky,cloudBase,cloudAlpha);
        // Genuine visible sun circle + soft atmospheric aureole, limited to
        // SDR 0..1. No post-process bloom or glow on dark street materials.
        float d=Vector3.Dot(dir,towardsSun);
        float halo=Smooth((d-.943f)/.057f);
        halo=halo*halo*.40f;
        sky=Color.Lerp(sky,new Color(.96f,.87f,.71f),halo);
        float disc=Smooth((d-.99918f)/.00060f);
        sky=Color.Lerp(sky,new Color(1f,.986f,.924f),disc);
        return new Color(Mathf.Clamp01(sky.r),Mathf.Clamp01(sky.g),Mathf.Clamp01(sky.b),1);
    }

    public static void ApplyToCity(Camera camera)
    {
        if(!camera)throw new ArgumentNullException(nameof(camera));
        var shader=Shader.Find("Skybox/Panoramic");
        if(!shader)throw new InvalidOperationException("Unity 6 Built-in panoramic skybox shader is unavailable");
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(MaterialPath);
        var image=new Texture2D(Width,Height,TextureFormat.RGB24,false);
        try
        {
            var pixels=new Color32[Width*Height];
            Vector3 towardsSun=BraxCityMaterials.SunDirection.normalized;
            for(int y=0;y<Height;y++)
                for(int x=0;x<Width;x++)
                    pixels[y*Width+x]=(Color32)Sample(x,y,towardsSun);
            image.SetPixels32(pixels);
            image.Apply(false,false);
            var png=image.EncodeToPNG();
            if(png==null||png.Length<1000)
                throw new InvalidOperationException("Baked city sky texture is empty");
            File.WriteAllBytes(TexturePath,png);
        }
        finally {UnityEngine.Object.DestroyImmediate(image);}
        AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if(!importer)throw new InvalidOperationException("City sky texture importer missing");
        importer.textureType=TextureImporterType.Default;
        importer.sRGBTexture=true;
        importer.mipmapEnabled=false;
        importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Compressed;
        importer.maxTextureSize=Width;
        var web=importer.GetPlatformTextureSettings("WebGL");
        web.name="WebGL";web.overridden=true;web.maxTextureSize=Width;
        web.format=TextureImporterFormat.ETC2_RGB4;web.compressionQuality=70;
        importer.SetPlatformTextureSettings(web);
        importer.SaveAndReimport();
        var sky=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if(!sky)throw new InvalidOperationException("City cloud sky did not import");
        var previous=AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if(previous)AssetDatabase.DeleteAsset(SkyPath);
        var mat=new Material(shader){name="BRAXHORN CITY clear daylight with cumulus clouds"};
        mat.SetTexture("_MainTex",sky);
        if(mat.HasProperty("_Tint"))mat.SetColor("_Tint",Color.white);
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
        Debug.Log("CITY_DAYLIGHT_REAL_SKYBOX clouds=procedurally-baked 1024x512 "+
                  "sun=SDR-disc no-bloom sceneOnly=city nativeWebGL_ETC2=true");
    }
}

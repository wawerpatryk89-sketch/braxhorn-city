using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Unity 6 Built-in Render Pipeline; isolated TEST scene only.
// Real physically-based source maps, one scene-local daylight source and native Unity soft shadows.
// Does not author new geometry, fake texture, or affect the accepted gym/Nutrition materials.
public static class BraxWaterfrontPbrLighting
{
    const string Source="Assets/Source/Waterfront/Sand/";
    const string Output="Assets/PlayableGenerated/WaterfrontLibrary";
    const int CoastLayer=27;

    static void Folder()
    {
        if(!Directory.Exists(Output)) Directory.CreateDirectory(Output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
    static Texture2D SourceTexture(string path)
    {
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+path);
        if(!t)throw new InvalidOperationException("Original real sand map missing: "+path);
        return t;
    }
    static void ImportNormal(string path)
    {
        var importer=AssetImporter.GetAtPath(Source+path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Sand normal-map importer missing: "+path);
        if(importer.textureType!=TextureImporterType.NormalMap ||
           importer.sRGBTexture || importer.convertToNormalmap)
        {
            importer.textureType=TextureImporterType.NormalMap;
            importer.convertToNormalmap=false;
            importer.sRGBTexture=false;
            importer.mipmapEnabled=true;
            importer.isReadable=false;
            importer.anisoLevel=4;
            importer.maxTextureSize=2048;
            importer.SaveAndReimport();
        }
    }
    static Color32[] ReadArm(string name)
    {
        string path=Source+name;
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Source ARM importer missing: "+path);
        if(!importer.isReadable || importer.sRGBTexture)
        {
            importer.isReadable=true;
            importer.sRGBTexture=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return SourceTexture(name).GetPixels32();
    }
    // Each pixel is derived exclusively from the real supplied ARM PBR texture:
    // glTF (AO=R, roughness=G, metallic=B) -> Unity Standard (metallic=R, smoothness=A).
    static Texture2D Packed(string key,string sourceArm,bool occlusion)
    {
        Folder();
        var src=SourceTexture(sourceArm);
        var pixels=ReadArm(sourceArm);
        var packed=new Texture2D(src.width,src.height,TextureFormat.RGBA32,true,true);
        for(int i=0;i<pixels.Length;i++)
        {
            var p=pixels[i];
            pixels[i]=occlusion?new Color32(p.r,p.r,p.r,255):
                new Color32(p.b,0,0,(byte)(255-p.g));
        }
        packed.SetPixels32(pixels);
        packed.Apply(true,false);
        string file=Output+"/"+key+(occlusion?"_AO.png":"_MetallicSmoothness.png");
        File.WriteAllBytes(file,packed.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
        var importer=AssetImporter.GetAtPath(file) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Generated packed real PBR texture failed");
        importer.sRGBTexture=false;
        importer.mipmapEnabled=true;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.isReadable=false;
        importer.anisoLevel=4;
        importer.maxTextureSize=2048;
        importer.wrapMode=TextureWrapMode.Repeat;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(file);
    }

    public static Material SandMaterial(string name,string diffuse,string normal,string arm)
    {
        Folder();
        var shader=Shader.Find("Standard");
        if(!shader)throw new InvalidOperationException("Unity 6 Built-in Standard PBR shader not available");
        ImportNormal(normal);
        string file=Output+"/"+name+"_PBR.mat";
        if(AssetDatabase.LoadMainAssetAtPath(file))AssetDatabase.DeleteAsset(file);
        var material=new Material(shader){name=name+" real photographic PBR sand"};
        material.SetTexture("_MainTex",SourceTexture(diffuse));
        material.SetTexture("_BumpMap",SourceTexture(normal));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_BumpScale",.6f);
        material.SetTexture("_MetallicGlossMap",Packed(name,arm,false));
        material.SetTexture("_OcclusionMap",Packed(name,arm,true));
        material.SetFloat("_Metallic",1f);
        material.SetFloat("_GlossMapScale",1f);
        material.SetFloat("_OcclusionStrength",1f);
        material.EnableKeyword("_METALLICGLOSSMAP");
        material.SetFloat("_Glossiness",.5f);
        material.renderQueue=(int)RenderQueue.Geometry;
        AssetDatabase.CreateAsset(material,file);
        return material;
    }

    public static void Apply(Transform waterfrontRoot)
    {
        if(!waterfrontRoot || waterfrontRoot.name!="BRAXHORN LIBRARY WATERFRONT (TEST)")
            throw new InvalidOperationException("Refusing to relight any other scene or original asset");
        var used=LayerMask.LayerToName(CoastLayer);
        if(!string.IsNullOrEmpty(used) && used!="BraxWaterfront")
            throw new InvalidOperationException("Reserved Unity layer 27 is occupied: "+used);
        foreach(var renderer in waterfrontRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.gameObject.layer=CoastLayer;
            var materials=renderer.sharedMaterials.Where(m=>m!=null).ToArray();
            bool opaque=materials.Length!=0 && materials.All(m=>m.renderQueue<3000);
            renderer.receiveShadows=opaque;
            renderer.shadowCastingMode=opaque?ShadowCastingMode.On:ShadowCastingMode.Off;
        }
        // Exclude just the NEW shoreline layer from previous directional lights.
        // All original city, gym and Nutrition objects retain exactly the same illumination.
        foreach(var old in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(old.type==LightType.Directional)
                old.cullingMask &=~ (1<<CoastLayer);
        var sun=new GameObject("BRAXHORN test-only coastal daylight (soft realistic shadows)");
        sun.transform.SetParent(waterfrontRoot,false);
        sun.layer=CoastLayer;
        var light=sun.AddComponent<Light>();
        light.type=LightType.Directional;
        light.cullingMask=1<<CoastLayer;
        light.color=new Color(1f,.97f,.92f);
        light.intensity=.95f;
        light.shadows=LightShadows.Soft;
        light.shadowStrength=.72f;
        light.shadowBias=.025f;
        light.shadowNormalBias=.25f;
        light.transform.rotation=Quaternion.LookRotation(-BraxCityMaterials.SunDirection);
        // These quality settings affect ONLY the opt-in single-scene test build.
        QualitySettings.shadows=ShadowQuality.All;
        QualitySettings.shadowDistance=58f;
        QualitySettings.shadowCascades=2;
        QualitySettings.shadowResolution=ShadowResolution.Medium;
        Debug.Log("BRAXHORN_WATERFRONT_PBR_SHADOWS sourcePBR=true scopedLayer=27 sunlight=1 soft=true visualPass=false");
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The source images are byte-identical to the owner's artwork. Declare their
// importer explicitly before any generated scene work, and fail early with the
// actual importer/object types instead of calling a present image "missing".
public static class BraxCityArtwork
{
    const string Folder="Assets/CityRebuild/Brand/";
    static readonly string[] Files={"brand-wide.png","brand-badge.png","brand-poster.png","brax-fighter.jpg","nyxa-pink.jpg","training.png","pw-ai-video.jpg"};
    public static void Prepare()
    {
        foreach(var name in Files)
        {
            string path=Folder+name;
            if(!File.Exists(path))throw new FileNotFoundException("Original owner artwork is absent from the checkout",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Expected TextureImporter for "+path+"; got "+AssetImporter.GetAtPath(path)?.GetType().FullName);
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.isReadable=false;
            importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
            // Platform compression is applied only to the generated city copy
            // by BraxCityMaterials; this source retains all original pixels.
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture=Load(path);
            Debug.Log("CITY_BRAND_IMPORTED "+path+" type="+texture.GetType().Name+" size="+texture.width+"x"+texture.height+" guid="+AssetDatabase.AssetPathToGUID(path));
        }
    }
    public static Texture2D Load(string path)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path)
                    ??AssetDatabase.LoadAllAssetsAtPath(path).OfType<Texture2D>().FirstOrDefault();
        if(texture)return texture;
        var importer=AssetImporter.GetAtPath(path);
        var main=AssetDatabase.LoadMainAssetAtPath(path);
        throw new InvalidOperationException("Owner artwork did not import as Texture2D: "+path+
            "; fileExists="+File.Exists(path)+"; importer="+(importer?importer.GetType().FullName:"null")+
            "; mainAsset="+(main?main.GetType().FullName:"null")+
            "; objects="+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(path).Where(x=>x).Select(x=>x.GetType().Name)));
    }
}

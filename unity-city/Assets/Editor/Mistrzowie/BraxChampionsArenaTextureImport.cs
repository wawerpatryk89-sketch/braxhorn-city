using System;
using UnityEditor;
using UnityEngine;

// Scoped to two new owner-supplied assets. Existing city/arena import settings stay untouched.
public sealed class BraxChampionsArenaTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path=assetPath.Replace('\\','/');
        bool signs=path.StartsWith("Assets/Source/Mistrzowie/ArenaSigns/",StringComparison.Ordinal);
        bool belt=path.StartsWith("Assets/Source/Mistrzowie/Trophies/",StringComparison.Ordinal);
        if(!signs&&!belt)return;

        var importer=(TextureImporter)assetImporter;
        bool colour=signs||path.EndsWith("Color.png",StringComparison.OrdinalIgnoreCase);
        importer.textureType=TextureImporterType.Default;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.sRGBTexture=colour;
        importer.mipmapEnabled=true;
        // A1 four approved sign textures must remain full-resolution on Android.
        // Streaming could temporarily choose a lower mip, blurring lettering.
        // Keep every SOURCE pixel and the exact original 6:1 artwork.
        importer.streamingMipmaps=!signs;
        importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.anisoLevel=signs?16:4;
        if(signs)
        {
            importer.filterMode=FilterMode.Trilinear;
            importer.wrapMode=TextureWrapMode.Clamp;
        }
        importer.alphaSource=signs?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        importer.alphaIsTransparency=signs;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize=belt?4096:2048;
        var android=importer.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        importer.SetPlatformTextureSettings(android);
    }
}

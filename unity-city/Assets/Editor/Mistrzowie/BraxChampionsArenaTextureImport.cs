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
        importer.sRGBTexture=colour;
        importer.mipmapEnabled=true;
        importer.streamingMipmaps=true;
        importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.anisoLevel=4;
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

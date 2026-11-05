using System;
using UnityEditor;
using UnityEngine;

// Casino P1 only. No changes to original A1/City texture importers.
// Native Poly Haven maps are 2048x2048, never downscaled from 4K.
public sealed class BraxChampionsPoolP1TextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool pool=assetPath.StartsWith("Assets/Source/Mistrzowie/PoolP1/Textures2K/",StringComparison.Ordinal);
        if(!pool)return;
        var importer=(TextureImporter)assetImporter;
        bool diffuse=assetPath.EndsWith("_diff_2k.jpg",StringComparison.Ordinal);
        bool linear=assetPath.EndsWith("_nor_gl_2k.png",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_arm_2k.png",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_nor_gl_2k.jpg",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_arm_2k.jpg",StringComparison.Ordinal);
        if(!diffuse && !linear)
            throw new InvalidOperationException("Unexpected P1 source map; only provider-native 2K diffuse/normal/ARM");
        importer.textureType=TextureImporterType.Default;
        importer.sRGBTexture=diffuse;
        importer.mipmapEnabled=true;
        importer.streamingMipmaps=true;
        importer.maxTextureSize=2048;
        importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.anisoLevel=4;
        importer.wrapMode=TextureWrapMode.Repeat;
        importer.filterMode=FilterMode.Trilinear;
        importer.alphaSource=TextureImporterAlphaSource.None;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=importer.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        importer.SetPlatformTextureSettings(android);
    }
}

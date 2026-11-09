using System;
using UnityEditor;
using UnityEngine;

// Casino T11 only. No changes to original A1/City texture importers.
// Native Poly Haven maps are 2048x2048, never downscaled from 4K.
public sealed class BraxChampionsCasinoT11TextureImport : AssetPostprocessor
{
    // 9.11 audit of APK 32: every non-power-of-two texture (27/27, e.g. murals
    // 6144x2048, velour 2048x1971) shipped UNCOMPRESSED RGBA32. ToLarger gives
    // the GPU a power-of-two ASTC texture; UVs are normalised, so the picture,
    // its proportions on the wall and every source pixel's content stay the same.
    // A new version forces Unity to re-import cached Library copies.
    public override uint GetVersion() => 3;
    void OnPreprocessTexture()
    {
        bool casino=assetPath.StartsWith("Assets/Source/Mistrzowie/CasinoT11/Textures2K/",StringComparison.Ordinal);
        bool vegetation=assetPath.StartsWith("Assets/Source/Mistrzowie/CasinoT11/Vegetation2K/",StringComparison.Ordinal);
        if(!casino&&!vegetation)return;
        var importer=(TextureImporter)assetImporter;
        bool diffuse=assetPath.EndsWith("_diff_2k.jpg",StringComparison.Ordinal);
        bool linear=assetPath.EndsWith("_nor_gl_2k.png",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_arm_2k.png",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_nor_gl_2k.jpg",StringComparison.Ordinal)
                 ||assetPath.EndsWith("_arm_2k.jpg",StringComparison.Ordinal);
        if(!diffuse && !linear)
            throw new InvalidOperationException("Unexpected T11 source map; only provider-native 2K diffuse/normal/ARM");
        importer.textureType=TextureImporterType.Default;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.sRGBTexture=diffuse;
        importer.mipmapEnabled=true;
        importer.streamingMipmaps=true;
        importer.maxTextureSize=2048;
        importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.ToLarger;
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

using System;
using UnityEditor;
using UnityEngine;

// A1 Arena mural import contract: ONLY the three approved owner HD PNGs.
// Canonical sources are staged from GitHub Release 435656828:
// IMG_0264.png (LEFT), IMG_0265.png (RIGHT), IMG_0266.png (ENTRANCE).
// No old JPEG fallback receives special import handling.
public sealed class BraxChampionsArenaMuralTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/Mistrzowie/ArenaMuralsHD/",StringComparison.Ordinal)
           ||!assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
            return;

        var hd=(TextureImporter)assetImporter;
        hd.textureType=TextureImporterType.Default;
        hd.textureShape=TextureImporterShape.Texture2D;
        hd.sRGBTexture=true;
        // Source remains 6144x2048. 8192 is only Unity's cap; it does not upscale.
        hd.maxTextureSize=8192;
        hd.isReadable=false;
        hd.mipmapEnabled=true;
        hd.streamingMipmaps=true;
        hd.wrapMode=TextureWrapMode.Clamp;
        hd.filterMode=FilterMode.Trilinear;
        hd.anisoLevel=4;
        hd.npotScale=TextureImporterNPOTScale.None;
        hd.alphaSource=TextureImporterAlphaSource.None;
        hd.textureCompression=TextureImporterCompression.CompressedHQ;

        var android=hd.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=8192;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        hd.SetPlatformTextureSettings(android);
    }
}

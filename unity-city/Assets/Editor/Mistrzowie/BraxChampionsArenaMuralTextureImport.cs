using System;
using UnityEditor;
using UnityEngine;

// ONLY the owner's three exact A1 HD PNG murals staged from release
// IMG_0264.png / IMG_0265.png / IMG_0266.png.
// Source remains 6144x2048. Android keeps that source under an 8K cap
// (no upscale) with ASTC 4x4 and streaming mipmaps.
public sealed class BraxChampionsArenaMuralTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/Mistrzowie/ArenaMuralsHD/",StringComparison.Ordinal)
           ||!assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return;

        var hd=(TextureImporter)assetImporter;
        hd.textureType=TextureImporterType.Default;
        hd.textureShape=TextureImporterShape.Texture2D;
        hd.sRGBTexture=true;
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

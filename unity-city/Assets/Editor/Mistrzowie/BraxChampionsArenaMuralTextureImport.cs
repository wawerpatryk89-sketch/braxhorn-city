using System;
using UnityEditor;
using UnityEngine;

// A1 Arena mural import contract: ONLY the three approved owner HD PNGs.
// Canonical sources are staged from GitHub Release 435656828:
// IMG_0264.png (LEFT), IMG_0265.png (RIGHT), IMG_0266.png (ENTRANCE).
// No old JPEG fallback receives special import handling.
public sealed class BraxChampionsArenaMuralTextureImport : AssetPostprocessor
{
    // 9.11 audit of APK 32: every non-power-of-two texture (27/27, e.g. murals
    // 6144x2048, velour 2048x1971) shipped UNCOMPRESSED RGBA32. ToLarger gives
    // the GPU a power-of-two ASTC texture; UVs are normalised, so the picture,
    // its proportions on the wall and every source pixel's content stay the same.
    // A new version forces Unity to re-import cached Library copies.
    public override uint GetVersion() => 3;
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
        hd.npotScale=TextureImporterNPOTScale.ToLarger;
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

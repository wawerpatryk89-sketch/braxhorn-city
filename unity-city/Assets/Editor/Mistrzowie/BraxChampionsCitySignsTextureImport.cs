using System;
using UnityEditor;

// Owner-supplied city sign PNG bytes remain unchanged in the source archive.
// Only Unity's Android texture importer is configured for mobile performance.
public sealed class BraxChampionsCitySignsTextureImport : AssetPostprocessor
{
    // 9.11 audit of APK 32: every non-power-of-two texture (27/27, e.g. murals
    // 6144x2048, velour 2048x1971) shipped UNCOMPRESSED RGBA32. ToLarger gives
    // the GPU a power-of-two ASTC texture; UVs are normalised, so the picture,
    // its proportions on the wall and every source pixel's content stay the same.
    // A new version forces Unity to re-import cached Library copies.
    public override uint GetVersion() => 3;
    void OnPreprocessTexture()
    {
        if(!assetPath.Replace('\\','/').StartsWith(
            "Assets/Source/Mistrzowie/CitySigns/",StringComparison.Ordinal))return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Default;
        t.textureShape=TextureImporterShape.Texture2D;
        t.sRGBTexture=true;
        t.mipmapEnabled=true;
        t.streamingMipmaps=true;
        t.isReadable=false;
        t.npotScale=TextureImporterNPOTScale.ToLarger;
        t.maxTextureSize=2048; // source files are 2048x512, never upscale
        t.anisoLevel=4;
        t.alphaSource=TextureImporterAlphaSource.FromInput;
        t.alphaIsTransparency=true;
        t.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=t.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        t.SetPlatformTextureSettings(android);
    }
}

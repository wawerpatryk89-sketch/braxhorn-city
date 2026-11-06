using System;
using UnityEditor;

// Owner-supplied city sign PNG bytes remain unchanged in the source archive.
// Only Unity's Android texture importer is configured for mobile performance.
public sealed class BraxChampionsCitySignsTextureImport : AssetPostprocessor
{
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
        t.npotScale=TextureImporterNPOTScale.None;
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

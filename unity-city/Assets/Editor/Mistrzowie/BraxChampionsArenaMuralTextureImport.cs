using System;
using UnityEditor;
using UnityEngine;

// ONLY the owner's three original 1536x512 murals in the A1 arena.
// This does not alter arena fighter textures, premium signage or other regions.
public sealed class BraxChampionsArenaMuralTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Three separate owner-original JPEGs. Preserve exact pixels/colour
        // and do not change source import settings for any other image.
        const string root="Assets/Source/Mistrzowie/ArenaMurals/";
        // Source HD 6144x2048 stays untouched. Android uses the owner-approved close-view 2K budget.
        if(assetPath.StartsWith("Assets/Source/Mistrzowie/ArenaMuralsHD/",StringComparison.Ordinal)
           &&assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))
        {
            var hd=(TextureImporter)assetImporter;
            hd.textureType=TextureImporterType.Default;
            hd.textureShape=TextureImporterShape.Texture2D;
            hd.sRGBTexture=true;hd.maxTextureSize=8192;hd.isReadable=false;
            hd.mipmapEnabled=true;hd.streamingMipmaps=true;
            hd.wrapMode=TextureWrapMode.Clamp;hd.filterMode=FilterMode.Trilinear;hd.anisoLevel=4;
            hd.npotScale=TextureImporterNPOTScale.None;
            hd.alphaSource=TextureImporterAlphaSource.None;
            hd.textureCompression=TextureImporterCompression.CompressedHQ;
            var a=hd.GetPlatformTextureSettings("Android");
            a.name="Android";a.overridden=true;a.maxTextureSize=2048;
            a.format=TextureImporterFormat.ASTC_4x4;a.compressionQuality=100;
            hd.SetPlatformTextureSettings(a);
            return;
        }
        if(assetPath!=root+"BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg"
           &&assetPath!=root+"BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg"
           &&assetPath!=root+"BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg")return;
        var settings=(TextureImporter)assetImporter;
        settings.textureType=TextureImporterType.Default;
        settings.textureShape=TextureImporterShape.Texture2D;
        settings.sRGBTexture=true;
        settings.maxTextureSize=2048; // original 1536x512; never enlarge or downscale source
        settings.isReadable=false;
        settings.mipmapEnabled=true;
        settings.streamingMipmaps=true;
        settings.wrapMode=TextureWrapMode.Clamp; // stable edge-colour panels
        settings.filterMode=FilterMode.Trilinear;
        settings.anisoLevel=4;
        settings.npotScale=TextureImporterNPOTScale.None;
        settings.alphaSource=TextureImporterAlphaSource.None;
        settings.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=settings.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        settings.SetPlatformTextureSettings(android);
    }
}

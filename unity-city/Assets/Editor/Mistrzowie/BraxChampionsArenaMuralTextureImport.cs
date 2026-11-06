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

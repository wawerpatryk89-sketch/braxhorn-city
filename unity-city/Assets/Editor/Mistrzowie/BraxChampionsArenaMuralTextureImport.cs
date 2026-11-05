using System;
using UnityEditor;
using UnityEngine;

// ONLY the owner's original 1536x512 mural at the A1 entrance wall.
// This does not alter arena fighter textures, premium signage or other regions.
public sealed class BraxChampionsArenaMuralTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(assetPath!="Assets/Source/Mistrzowie/ArenaMurals/BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg")return;
        var settings=(TextureImporter)assetImporter;
        settings.textureType=TextureImporterType.Default;
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

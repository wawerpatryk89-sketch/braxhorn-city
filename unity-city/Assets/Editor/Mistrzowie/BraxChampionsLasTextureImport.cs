using System;
using UnityEditor;

// The quad track's forest (BraxChampionsLasKladow): its card atlas, 16 trees rendered from Poly Haven's scans with the needles'
// own cut-out. Mipmapped with the cut-out's coverage kept at every level (a plain average thins the needles away in the
// distance: the forest would turn into bare trunks), clamped (each cell its own picture), ASTC 6x6 on Android at its full
// 4096 x 2048 (~5 MB: one texture for the whole forest; ZASADY 5.9 - 512 per tree card).
public sealed class BraxChampionsLasTextureImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith("Assets/Source/Mistrzowie/Las/", StringComparison.Ordinal)) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.textureShape = TextureImporterShape.Texture2D;
        t.sRGBTexture = true;
        t.alphaSource = TextureImporterAlphaSource.FromInput;
        t.alphaIsTransparency = false;                 // the colour under the transparent texels is already pushed out (atlas_lasu.py)
        t.mipmapEnabled = true;
        t.mipMapsPreserveCoverage = true;
        t.alphaTestReferenceValue = .5f;               // the material's _Cutoff
        t.streamingMipmaps = true;
        t.isReadable = false;
        t.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        t.npotScale = TextureImporterNPOTScale.None;
        t.maxTextureSize = 4096;
        t.anisoLevel = 2;
        t.textureCompression = TextureImporterCompression.CompressedHQ;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = 4096;
        android.format = TextureImporterFormat.ASTC_6x6;
        android.compressionQuality = 100;
        t.SetPlatformTextureSettings(android);
    }
}

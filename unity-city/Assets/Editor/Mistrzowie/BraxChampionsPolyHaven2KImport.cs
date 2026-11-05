using System.IO;
using UnityEditor;
using UnityEngine;

// Keep original Poly Haven 2K glTF textures at full 2048px on Android.
// glTFast 6.20 imports the original model geometry and material assignments;
// this hook only sets platform texture quality, never recolors source artwork.
// Only this isolated next-release subdirectory is affected.
public sealed class BraxChampionsPolyHaven2KImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Source/Mistrzowie/PolyHaven/"))
            return;
        var filename=Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        if(!filename.EndsWith("_2k"))
            throw new System.InvalidOperationException(
                "Only source 2K PBR textures are allowed in promenade props: "+assetPath);
        var imp=(TextureImporter)assetImporter;
        imp.textureType=TextureImporterType.Default; // OpenGL normal must remain RGB
        imp.sRGBTexture=filename.Contains("_diff_")||filename.Contains("_col_");
        imp.maxTextureSize=2048;
        imp.npotScale=TextureImporterNPOTScale.None;
        imp.mipmapEnabled=true;
        imp.streamingMipmaps=true;
        imp.anisoLevel=8;
        imp.isReadable=false;
        imp.filterMode=FilterMode.Trilinear;
        imp.wrapMode=TextureWrapMode.Repeat;
        imp.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=imp.GetPlatformTextureSettings("Android");
        android.name="Android";
        android.overridden=true;
        android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;
        android.compressionQuality=100;
        imp.SetPlatformTextureSettings(android);
    }
}

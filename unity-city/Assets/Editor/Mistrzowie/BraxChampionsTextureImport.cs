using System.IO;
using UnityEditor;
using UnityEngine;

// Configure generated copies on their FIRST import. Previously CopyAsset
// imported each map with inherited defaults, then SaveAndReimport compressed
// it again. Scope and final texture quality are identical to CopyMap's contract.
public sealed class BraxChampionsTextureImport : AssetPostprocessor
{
    public override uint GetVersion() => 4; // 9.11 evening: ASTC 6x6 for the shared library (APK weight)
    // Owner 9.11: "twoje APK nic się nie zmniejszyło". The ~250 shared 2K PBR
    // maps were ASTC 4x4 (8 bpp, 5.6 MB each). ASTC 6x6 keeps the same 2048 px
    // at 3.56 bpp (2.5 MB). Hotel Sobczak T03 keeps its own 4x4 contract
    // (separate executor's spec). Murals, signs and plates use other importers.
    public static TextureImporterFormat AndroidFormat(string assetPath)
        => Path.GetFileNameWithoutExtension(assetPath).StartsWith("sobczak")
            ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/PlayableGenerated/Mistrzowie/Textures/"))return;
        string name=Path.GetFileNameWithoutExtension(assetPath);
        bool color=name.EndsWith("_color"),normal=name.EndsWith("_normal"),arm=name.EndsWith("_arm");
        if(!color&&!normal&&!arm)return;
        var importer=(TextureImporter)assetImporter;
        // CityStaticPBR reads the original RGB GL normal, not Unity's packed normal.
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
        importer.sRGBTexture=color;
        importer.mipmapEnabled=true;importer.npotScale=TextureImporterNPOTScale.ToLarger;
        // Oryginalne mapy z biblioteki zachowują 4K na dysku. Android otrzymuje
        // do 2K na najbliższych nawierzchniach; tiling materiałów pozostaje.
        importer.maxTextureSize=2048;importer.isReadable=false;importer.anisoLevel=8;
        importer.streamingMipmaps=true;importer.streamingMipmapsPriority=0;
        importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=importer.GetPlatformTextureSettings("Android");
        android.overridden=true;android.maxTextureSize=2048;
        android.format=AndroidFormat(assetPath);android.compressionQuality=100;
        importer.SetPlatformTextureSettings(android);
    }
}

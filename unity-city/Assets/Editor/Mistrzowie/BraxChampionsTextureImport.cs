using System.IO;
using UnityEditor;
using UnityEngine;

// Configure generated copies on their FIRST import. Previously CopyAsset
// imported each map with inherited defaults, then SaveAndReimport compressed
// it again. Scope and final texture quality are identical to CopyMap's contract.
public sealed class BraxChampionsTextureImport : AssetPostprocessor
{
    public override uint GetVersion() => 2;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/PlayableGenerated/Mistrzowie/Textures/"))return;
        string name=Path.GetFileNameWithoutExtension(assetPath);
        bool color=name.EndsWith("_color"),normal=name.EndsWith("_normal"),arm=name.EndsWith("_arm");
        if(!color&&!normal&&!arm)return;
        var importer=(TextureImporter)assetImporter;
        // CityStaticPBR reads the original RGB GL normal, not Unity's packed normal.
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=color;
        importer.mipmapEnabled=true;importer.npotScale=TextureImporterNPOTScale.None;
        // Oryginalne mapy z biblioteki zachowują 4K na dysku. Android otrzymuje
        // do 2K na najbliższych nawierzchniach; tiling materiałów pozostaje.
        importer.maxTextureSize=2048;importer.isReadable=false;importer.anisoLevel=8;
        importer.streamingMipmaps=true;importer.streamingMipmapsPriority=0;
        importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        var android=importer.GetPlatformTextureSettings("Android");
        android.overridden=true;android.maxTextureSize=2048;
        android.format=TextureImporterFormat.ASTC_4x4;android.compressionQuality=100;
        importer.SetPlatformTextureSettings(android);
    }
}

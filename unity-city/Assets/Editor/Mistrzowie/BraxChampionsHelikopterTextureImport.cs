using UnityEditor;

// MIASTO MISTRZÓW - the BRAXHORN helicopter's textures (Assets/Source/Mistrzowie/Helikopter/_tekstury/braxhorn_ec135, opened
// from the owner's pack by install_owner_media.py; made by unity-tools/mistrzowie/helikopter): the glTF references them, so
// glTFast takes these project textures - compressed for the phone - instead of its own uncompressed copies. Sized offline
// (the fuselage 2048, the rest 1024 / 512); "__lin" in the name = a data map (normal, metal-roughness) -> linear, as
// glTFast reads them; colour maps sRGB. Mip maps, no readable copy, ASTC on Android.
public sealed class BraxChampionsHelikopterTextureImport : AssetPostprocessor
{
    public const string Folder = "Assets/Source/Mistrzowie/Helikopter/_tekstury/";
    public override int GetPostprocessOrder() { return 100; }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal)) return;
        var t = (TextureImporter)assetImporter;
        bool data = System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("__lin", System.StringComparison.Ordinal);
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !data;
        t.mipmapEnabled = true; t.isReadable = false; t.anisoLevel = 4;
        t.alphaSource = TextureImporterAlphaSource.None; t.alphaIsTransparency = false;
        t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.CompressedHQ;
        var a = t.GetPlatformTextureSettings("Android");
        a.overridden = true; a.maxTextureSize = 2048; a.format = TextureImporterFormat.ASTC_6x6; a.compressionQuality = 50;
        t.SetPlatformTextureSettings(a);
    }
}

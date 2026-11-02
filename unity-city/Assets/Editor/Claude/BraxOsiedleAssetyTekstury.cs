using UnityEditor;

// Tekstury modeli ze Sketchfaba na osiedlu (Assets/Source/Prywatne/Osiedle/<paczka>/_tekstury/…, odszyfrowane w CI przez
// unity-tools/claude/prywatne.py — paczki osiedle_*). Obrazy zmniejszone offline (≤ 1024) — tu kompresja platformy
// (ETC2 na Androidzie, DXT w przeglądarce), mipmapy, bez kopii do odczytu. „__lin” w nazwie = obraz danych (normalne,
// chropowatość/metal, okluzja) → bez sRGB (tak czyta glTFast). Alfa z pliku (PNG tylko tam, gdzie jest maska).
// Warianty grafik automatu (_tekstury/kasyno_automat_warianty/) czytane w kodzie — też tylko jako skompresowane assety.
public sealed class BraxOsiedleAssetyTekstury : AssetPostprocessor
{
    public override int GetPostprocessOrder() { return 100; }   // po BraxPrywatneImport (pomija Osiedle/)

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Source/Prywatne/Osiedle/") || !assetPath.Contains("/_tekstury/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("__lin");
        t.mipmapEnabled = true; t.isReadable = false; t.anisoLevel = 4;
        t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = false;
        t.maxTextureSize = 1024; t.textureCompression = TextureImporterCompression.Compressed;
        foreach (var platform in new[] { "Android", "WebGL" })
        {
            var ps = t.GetPlatformTextureSettings(platform);
            ps.name = platform; ps.overridden = true; ps.maxTextureSize = 1024;
            ps.format = TextureImporterFormat.Automatic; ps.textureCompression = TextureImporterCompression.Compressed;
            ps.compressionQuality = 50;
            t.SetPlatformTextureSettings(ps);
        }
    }
}

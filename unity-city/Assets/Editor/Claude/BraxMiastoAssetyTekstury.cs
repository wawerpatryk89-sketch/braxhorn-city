using UnityEditor;

// Tekstury modeli ze Sketchfaba w mieście (Assets/Source/Prywatne/Miasto/<paczka>/_tekstury/…, odszyfrowane w CI).
// Obrazy są już zmniejszone offline (budynki ≤ 2048, pojazdy/wyposażenie/siłownia ≤ 1024) — tu kompresja platformy
// (ETC2 na Androidzie, DXT w przeglądarce), mipmapy, bez kopii do odczytu, limit 2048. „__lin” w nazwie = obraz danych
// (normalne, chropowatość/metal, okluzja) → bez sRGB (tak czyta glTFast). Alfa z pliku (PNG tylko tam, gdzie jest maska).
public sealed class BraxMiastoAssetyTekstury : AssetPostprocessor
{
    public override int GetPostprocessOrder() { return 100; }   // po ogólnych postprocesorach Prywatne/ (BraxPrywatneImport pomija Miasto/)

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Source/Prywatne/Miasto/") || !assetPath.Contains("/_tekstury/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("__lin");
        t.mipmapEnabled = true; t.isReadable = false; t.anisoLevel = 4;
        t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = false;
        // 3.11 (właściciel: „kontenerowiec… jest 4K”): statki HD (paczki miasto_statki*) — kolory 4096, dane 2048, wyższa jakość kompresji
        bool hd = assetPath.Contains("/Miasto/miasto_statki"), dane = System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("__lin");
        int maks = hd && !dane ? 4096 : 2048;
        t.maxTextureSize = maks; t.textureCompression = hd ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
        foreach (var platform in new[] { "Android", "WebGL" })
        {
            var ps = t.GetPlatformTextureSettings(platform);
            ps.name = platform; ps.overridden = true; ps.maxTextureSize = maks;
            ps.format = TextureImporterFormat.Automatic; ps.textureCompression = TextureImporterCompression.Compressed;
            ps.compressionQuality = hd ? 100 : 50;
            t.SetPlatformTextureSettings(ps);
        }
    }
}

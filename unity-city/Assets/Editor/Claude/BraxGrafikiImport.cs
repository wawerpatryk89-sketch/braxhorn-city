using UnityEditor;
using UnityEngine;

// GRAFIKI WŁAŚCICIELA (ekran startowy, billboardy — zaszyfrowana paczka „grafiki”, prywatne.py → Assets/Source/Prywatne/
// Grafiki/). Obrazy oglądane na cały ekran / z bliska: bez mipmap (ekran startowy) i bez skalowania do potęgi dwójki, sRGB,
// najwyżej 2048, Android ASTC 4×4 (ostro, bez bloków na napisach). Po ogólnym BraxPrywatneImport (2048 + mipmapy) —
// kolejność 200 nadpisuje te ustawienia tylko dla Grafiki/.
public sealed class BraxGrafikiImport : AssetPostprocessor
{
    const string Katalog = "Assets/Source/Prywatne/Grafiki/";
    public override int GetPostprocessOrder() { return 200; }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Katalog) && !assetPath.EndsWith("/miasto_sklepy/billboardy/19.jpg")) return;
        var ti = assetImporter as TextureImporter;
        if (ti == null) return;
        bool ekran = assetPath.Contains("/Resources/");   // ekran startowy / ładowania — tylko na cały ekran
        ti.textureType = TextureImporterType.Default;
        ti.sRGBTexture = true;
        ti.alphaSource = TextureImporterAlphaSource.None;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.mipmapEnabled = !ekran;                         // billboardy w świecie — z mipmapami (bez migotania z daleka)
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.anisoLevel = ekran ? 0 : 4;
        // budżet APK (miasto ~750 MB, 9.11): billboardy w świecie 1024 i ASTC 6×6; murale oglądane z bliska (02, 19) i ekran startowy 2048.
        // Mural 19 na całą szarą ścianę siłowni (właściciel: „mural na szarej ścianie siłowni musi być”): 4096 — jedyny wyjątek 4K.
        string plik = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool zBliska = ekran || plik == "02";
        ti.maxTextureSize = plik == "19" ? 4096 : (zBliska ? 2048 : 1024);
        ti.streamingMipmaps = !ekran && plik != "19";
        ti.isReadable = false;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        var a = ti.GetPlatformTextureSettings("Android");
        a.overridden = true; a.maxTextureSize = ti.maxTextureSize; a.format = ekran ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6; a.compressionQuality = 100;
        ti.SetPlatformTextureSettings(a);
        var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");   // jakość „niska” nie zmniejsza grafik
        if (ign != null && ign.CanWrite) ign.SetValue(ti, true);
    }
}

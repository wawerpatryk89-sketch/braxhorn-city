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
        if (!assetPath.StartsWith(Katalog)) return;
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
        ti.maxTextureSize = 2048;
        ti.isReadable = false;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        var a = ti.GetPlatformTextureSettings("Android");
        a.overridden = true; a.maxTextureSize = 2048; a.format = TextureImporterFormat.ASTC_4x4; a.compressionQuality = 100;
        ti.SetPlatformTextureSettings(a);
        var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");   // jakość „niska” nie zmniejsza grafik
        if (ign != null && ign.CanWrite) ign.SetValue(ti, true);
    }
}

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Tekstury wyjęte z modeli .glb przez unity-tools/claude/odchudzanie_modeli.py (folder _tekstury_glb obok modelu).
// glTFast w edytorze bierze ten asset zamiast tworzyć nieskompresowaną kopię RGBA32 — tu ustawiamy kompresję platformy
// (ETC2 na Androidzie, DXT w przeglądarce), limit 1K na telefonie i w przeglądarce (9.11: powrót z 2K — waga APK), mipmapy + streaming, brak kopii do odczytu.
// „__lin” w nazwie = obraz danych (normalne, chropowatość/metal, okluzja) → bez sRGB, tak jak oczekuje glTFast.
// Normalne zostają typu Default (glTFast i CityStaticPBR czytają XYZ, nie DXT5nm).
public sealed class BraxClaudeGlbTekstury : AssetPostprocessor
{
    [System.Serializable] sealed class Model { public string model; public int obrazy; }
    [System.Serializable] sealed class Raport { public Model[] glb; }

    // Bezpiecznik kolejności importu: gdyby model .glb zaimportował się przed swoimi teksturami (glTFast nie znalazłby
    // assetu i materiały zostałyby bez map), importujemy go jeszcze raz — tylko wtedy, gdy żaden materiał nie ma tekstury.
    public static void Sprawdz()
    {
        const string path = "Assets/PlayableGenerated/odchudzanie.json";
        if (!File.Exists(path)) { Debug.Log("BRAXHORN_GLB_TEKSTURY: brak raportu odchudzania (krok CI pominięty)"); return; }
        var rep = JsonUtility.FromJson<Raport>(File.ReadAllText(path));
        int ok = 0, again = 0, still = 0;
        foreach (var m in rep.glb ?? new Model[0])
        {
            string glb = "Assets/" + m.model;
            if (m.obrazy <= 0 || !File.Exists(glb)) continue;
            if (Textured(glb)) { ok++; continue; }
            AssetDatabase.ImportAsset(glb, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            if (Textured(glb)) again++;
            else { still++; Debug.LogWarning("DO POPRAWKI: " + glb + " bez tekstur po ponownym imporcie"); }
        }
        Debug.Log("BRAXHORN_GLB_TEKSTURY: modele z teksturami " + ok + ", zaimportowane ponownie " + again + ", nadal bez tekstur " + still);
    }

    static bool Textured(string glb)
    {
        foreach (var mat in AssetDatabase.LoadAllAssetsAtPath(glb).OfType<Material>())
            foreach (var n in mat.GetTexturePropertyNames())
                if (mat.GetTexture(n)) return true;
        return false;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/_tekstury_glb/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !System.IO.Path.GetFileNameWithoutExtension(assetPath).EndsWith("__lin");
        t.mipmapEnabled = true; t.streamingMipmaps = true;
        t.isReadable = false; t.anisoLevel = 4;
        t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = false;
        t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.Compressed;
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

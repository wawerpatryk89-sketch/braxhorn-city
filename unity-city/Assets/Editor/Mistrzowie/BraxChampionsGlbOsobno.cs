using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// OBRAZY Z .glb JAKO OSOBNE TEKSTURY, JAK W MIEŚCIE R1 (właściciel 25.11.2026 17:19: „Wszystko jest w kompresji w grze?”).
// unity-tools/mistrzowie/tekstury_glb_osobno.py w CI, przed Unity, zapisuje obraz z .glb (bajt w bajt) obok modelu
// (_tekstury_glb_m/<model>/…), a w .glb zostawia odnośnik; glTFast bierze wtedy asset tekstury z projektu zamiast robić
// nieskompresowaną kopię RGBA32 (#52: 250 obrazów, ~1,7 GB). Tu: ustawienia importu tych obrazów i kontrola po imporcie,
// że materiały modeli naprawdę mają teksturę z projektu (gdyby model zaimportował się przed swoimi obrazami - drugi import).
public static class BraxChampionsGlbOsobno
{
    public const string Folder = "/_tekstury_glb_m/";
    public const string Raport = "mistrzowie-tekstury-glb.json";

    [Serializable] sealed class Model { public string model; public int obrazy; }
    [Serializable] sealed class Rap { public Model[] glb; }

    // (textures from the project, textures still inside the .glb) over all materials of the model
    static (int projekt, int wGlb) Policz(string glb)
    {
        int p = 0, g = 0;
        foreach (var mat in AssetDatabase.LoadAllAssetsAtPath(glb).OfType<Material>())
            foreach (int id in mat.GetTexturePropertyNameIDs())
            {
                var t = mat.GetTexture(id);
                if (!t) continue;
                if (AssetDatabase.GetAssetPath(t) == glb) g++; else p++;
            }
        return (p, g);
    }

    /// <summary>Never throws: a model that still has its textures inside is imported once more; what stays is logged.</summary>
    public static void Sprawdz()
    {
        try
        {
            if (!File.Exists(Raport)) { Debug.Log("BRAX_GLB_OSOBNO brak raportu " + Raport + " (krok CI pominięty)"); return; }
            var rap = JsonUtility.FromJson<Rap>(File.ReadAllText(Raport));
            int ok = 0, ponownie = 0, nadal = 0, obrazy = 0;
            foreach (var m in rap?.glb ?? new Model[0])
            {
                string glb = "Assets/" + m.model;
                if (m.obrazy <= 0 || !File.Exists(glb)) continue;
                obrazy += m.obrazy;
                var (p, g) = Policz(glb);
                if (g == 0 && p > 0) { ok++; continue; }
                AssetDatabase.ImportAsset(glb, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                (p, g) = Policz(glb);
                if (g == 0 && p > 0) ponownie++;
                else { nadal++; Debug.LogWarning("DO POPRAWKI BRAX_GLB_OSOBNO " + glb + ": tekstury z projektu " + p + ", w .glb " + g); }
            }
            Debug.Log("BRAX_GLB_OSOBNO obrazy=" + obrazy + " modele_ok=" + ok + " zaimportowane_ponownie=" + ponownie + " nadal_w_glb=" + nadal);
        }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI BRAX_GLB_OSOBNO kontrola: " + e.Message); }
        finally { EditorUtility.UnloadUnusedAssetsImmediate(); }
    }
}

// ASTC 6x6 on the phone, mipmaps (streamed), no CPU copy, at most 2048 there (ZASADA-1: a library model's image 1-2K);
// "__lin" = data image read linear by glTFast (normals, metal/roughness, occlusion), "__clamp"/"__mirror" = the glTF wrap.
// Runs after every other texture hook (order 1000), so a folder rule of Prywatne never changes these.
public sealed class BraxChampionsGlbOsobnoImport : AssetPostprocessor
{
    public override int GetPostprocessOrder() { return 1000; }
    public override uint GetVersion() { return 1; }

    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains(BraxChampionsGlbOsobno.Folder)) return;
        var t = (TextureImporter)assetImporter;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        t.textureType = TextureImporterType.Default; t.textureShape = TextureImporterShape.Texture2D;
        t.sRGBTexture = !n.Contains("__lin");
        t.mipmapEnabled = true; t.streamingMipmaps = true; t.isReadable = false; t.anisoLevel = 4;
        t.npotScale = TextureImporterNPOTScale.None;
        t.alphaSource = TextureImporterAlphaSource.FromInput; t.alphaIsTransparency = false;
        t.wrapMode = n.Contains("__clamp") ? TextureWrapMode.Clamp : n.Contains("__mirror") ? TextureWrapMode.Mirror : TextureWrapMode.Repeat;
        t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.Compressed;
        var ps = t.GetPlatformTextureSettings("Android");
        ps.name = "Android"; ps.overridden = true; ps.maxTextureSize = 2048;
        ps.format = TextureImporterFormat.ASTC_6x6; ps.textureCompression = TextureImporterCompression.Compressed;
        ps.compressionQuality = 50;                                                    // normal (R1 3.11: 100 = minutes per image)
        t.SetPlatformTextureSettings(ps);
    }
}

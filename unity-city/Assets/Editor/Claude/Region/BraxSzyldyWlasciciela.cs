using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// SZYLDY Z GRAFIK WŁAŚCICIELA (MIASTO R1, 18.11.2026). Właściciel 12:13: „Tych szyldów nie akceptuję. Jeżeli potrzebujesz jakieś
// obrazy na szyldy, to mów, to ci wydrukuję.”; 12:41: „…piszesz konkretnie… Od A do Z.”; 12:57 — plansza: A (17 lokali ulicy
// głównej, grafiki 4:1) i B (10 lokali Alei Centralnej, 6:1); 13:02: „Te wystarczą… jak brakuje, to zrób porządek sam.”
// Zaszyfrowana paczka globalna braxhorn-sklep-szyldy-r1-01 (repo publiczne; CI: install_sklep_modele.py rozpakowuje do
// Assets/Source/Prywatne/Miasto/szyldy_r1_01/). Grafiki A i B wycięte z planszy co do piksela (bez skalowania, bez zmian treści).
// C (kasetony na wysięgnikach, kwadrat) — z ikon tych samych grafik A (krzyż apteki, kufle, klucz, moneta, hantla, koperta,
// okulary), bez dopisanego tekstu. Bez paczki — dotychczasowe szyldy (litery Bebas / grafiki firm).
// KASETON ŚWIETLNY: lico = grafika (druk na akrylu, podświetlenie od środka), rama z aluminium 2.5 cm dookoła lica, 2.2 cm przed
// licem (lico cofnięte w ramie), od ściany na dystansach 2.5 cm. Lico nigdy w płaszczyźnie innej bryły (bez migotania): rama leży POZA polem grafiki,
// nic nie leży na licu, lico nie leży na ścianie.
internal static class BraxSzyldyWlasciciela
{
    public const string Katalog = "Assets/Source/Prywatne/Miasto/szyldy_r1_01/";
    public const float Rama = .025f, Wystep = .022f, Dystans = .025f;   // rama 2.5 cm, lico cofnięte 2.2 cm w ramie, dystanse 2.5 cm (każda para równoległych ≥ 2 cm)
    static readonly Dictionary<string, string> UlicaGlowna = new Dictionary<string, string>
    {
        { "APTEKA", "apteka" }, { "DELIKATESY", "delikatesy" }, { "BAR 24", "bar_24" }, { "BISTRO", "bistro" }, { "KWIACIARNIA", "kwiaciarnia" },
        { "HOTEL", "hotel" }, { "KANTOR", "kantor" }, { "WARSZTAT ROWEROWY", "warsztat_rowerowy" }, { "KAWIARNIA", "kawiarnia" },
        { "PIEKARNIA", "piekarnia" }, { "BRAXHORN FITNESS", "fitness" }, { "POCZTA", "poczta" }, { "KSIĘGARNIA", "ksiegarnia" },
        { "CUKIERNIA", "cukiernia" }, { "OPTYK", "optyk" }, { "WARZYWA OWOCE", "warzywa_owoce" }, { "PIZZERIA", "pizzeria" },
    };
    static readonly Dictionary<string, string> AlejaCentralna = new Dictionary<string, string>
    {
        { "KAWIARNIA", "kawiarnia" }, { "PIZZERIA", "pizzeria" }, { "CUKIERNIA", "cukiernia" }, { "BISTRO", "bistro" }, { "FRYZJER", "fryzjer" },
        { "APTEKA", "apteka" }, { "DELIKATESY", "delikatesy" }, { "PIEKARNIA", "piekarnia" }, { "OPTYK", "optyk" }, { "KWIACIARNIA", "kwiaciarnia" },
    };
    // C: lokale z szyldem na wysięgniku (BraxUlicePierzeje: wysiegnik = true)
    static readonly Dictionary<string, string> Wysiegniki = new Dictionary<string, string>
    {
        { "APTEKA", "apteka" }, { "BAR 24", "bar_24" }, { "HOTEL", "hotel" }, { "KANTOR", "kantor" }, { "BRAXHORN FITNESS", "fitness" },
        { "POCZTA", "poczta" }, { "OPTYK", "optyk" },
    };
    public static string PlikA(string szyld) => szyld != null && UlicaGlowna.TryGetValue(szyld, out var n) ? "szyld_A_" + n + ".png" : null;
    public static string PlikB(string szyld) => szyld != null && AlejaCentralna.TryGetValue(szyld, out var n) ? "szyld_B_" + n + ".png" : null;
    public static string PlikC(string szyld) => szyld != null && Wysiegniki.TryGetValue(szyld, out var n) ? "szyld_C_" + n + ".png" : null;

    public static int kasetonow;
    public static readonly List<string> zalozone = new List<string>();
    static readonly Dictionary<string, (Material m, float asp)> cache = new Dictionary<string, (Material, float)>();

    /// <summary>Materiał lica kasetonu z grafiką właściciela i proporcje pliku (szerokość / wysokość). null bez paczki.</summary>
    public static Material Grafika(string plik, out float aspekt)
    {
        aspekt = 1f;
        if (string.IsNullOrEmpty(plik)) return null;
        if (cache.TryGetValue(plik, out var c) && c.m) { aspekt = c.asp; return c.m; }
        string p = Katalog + plik;
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp && (imp.wrapMode != TextureWrapMode.Clamp || !imp.mipmapEnabled || imp.maxTextureSize != 1024 || !imp.sRGBTexture || imp.anisoLevel < 8))
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = true; imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 8; imp.maxTextureSize = 1024;
            imp.alphaSource = TextureImporterAlphaSource.None; imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (!t) return null;
        RozmiarPng(p, out int w, out int h);                       // proporcje pliku źródłowego (import może skalować do potęgi 2)
        aspekt = h > 0 ? (float)w / h : (float)t.width / Mathf.Max(1, t.height);
        var m = new Material(Shader.Find("Standard")) { name = "Szyld właściciela: " + Path.GetFileNameWithoutExtension(plik) + " (kaseton, druk na akrylu, podświetlany)", mainTexture = t };
        m.SetFloat("_Glossiness", .55f); m.SetFloat("_Metallic", 0f);
        m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", t); m.SetColor("_EmissionColor", new Color(.32f, .32f, .32f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        string dir = BraxUliceKit.Out + "/SzyldyWlasciciela"; Directory.CreateDirectory(dir);
        BraxZapisAssetu.Utworz(m, dir + "/" + Path.GetFileNameWithoutExtension(plik) + ".mat");
        cache[plik] = (m, aspekt);
        return m;
    }

    /// <summary>Szerokość i wysokość z nagłówka PNG (IHDR); 0 gdy to nie PNG.</summary>
    static void RozmiarPng(string p, out int w, out int h)
    {
        w = h = 0;
        try
        {
            var b = new byte[24];
            using (var fs = File.OpenRead(p)) if (fs.Read(b, 0, 24) < 24) return;
            if (b[0] != 0x89 || b[1] != (byte)'P' || b[12] != (byte)'I' || b[13] != (byte)'H') return;
            w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
            h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        }
        catch (IOException) { w = h = 0; }
    }

    static Material Alu => BraxUliceKit.Mat("szyld: rama kasetonu (aluminium anodowane grafit)", new Color(.13f, .135f, .14f), .4f, .7f);

    /// <summary>Kaseton świetlny z grafiką: c = środek lica (lico w tej płaszczyźnie), n = przód (na ulicę), h = wysokość lica,
    /// doSciany = odległość lica od ściany / nośnika za nim (≥ 4 cm). Szerokość z proporcji grafiki (≤ maxW — wtedy niższy).
    /// Geometria do bieżącej grupy BraxUliceKit. Zwraca szerokość lica (0 bez paczki).</summary>
    public static float Kaseton(string plik, Vector3 c, Vector3 n, float h, float doSciany, float maxW = 99f, string gdzie = "")
    {
        var m = Grafika(plik, out float asp);
        if (!m || h <= .05f) return 0f;
        float w = h * asp;
        if (w > maxW) { w = maxW; h = w / asp; }
        n.y = 0f; n.Normalize();
        var R = Vector3.Cross(n, Vector3.up).normalized; var U = Vector3.up;   // R = prawo patrzącego na lico
        // lico: UV 0…1, u = 0 po lewej patrzącego, v = 0 na dole (obraz nieodbity)
        var s = BraxUliceKit.S(m); int i0 = s.v.Count;
        Vector3 p0 = c - R * (w * .5f) - U * (h * .5f), p1 = p0 + R * w;
        s.Quad(p0, p1, p1 + U * h, p0 + U * h, n);
        s.uv[i0] = new Vector2(0f, 0f); s.uv[i0 + 1] = new Vector2(1f, 0f); s.uv[i0 + 2] = new Vector2(1f, 1f); s.uv[i0 + 3] = new Vector2(0f, 1f);
        // rama dookoła lica (poza polem grafiki): od dystansów (ściana + 2.5 cm) do 2.2 cm przed licem; górna i dolna listwa na całą
        // szerokość z narożnikami, boczne tylko na wysokość lica (bez nakładania brył)
        var a = BraxUliceKit.S(Alu);
        float tyl = doSciany - Dystans, gl = tyl + Wystep; var sr = c + n * ((Wystep - tyl) * .5f);
        a.OBox(sr + U * (h * .5f + Rama * .5f), R, U, n, w + 2f * Rama, Rama, gl);
        a.OBox(sr - U * (h * .5f + Rama * .5f), R, U, n, w + 2f * Rama, Rama, gl);
        a.OBox(sr + R * (w * .5f + Rama * .5f), R, U, n, Rama, h, gl);
        a.OBox(sr - R * (w * .5f + Rama * .5f), R, U, n, Rama, h, gl);
        // dystanse (4 tuleje 3 × 2.5 cm) między ramą a ścianą, bez ściany od strony muru
        foreach (float sx in new[] { -1f, 1f }) foreach (float sy in new[] { -1f, 1f })
            a.OBox(c + R * (sx * (w * .5f + Rama * .5f)) + U * (sy * (h * .5f + Rama * .5f)) - n * (doSciany - Dystans * .5f), R, U, n, .03f, Mathf.Min(.03f, Rama), Dystans, true);
        kasetonow++; zalozone.Add(plik + " " + w.ToString("F2") + "×" + h.ToString("F2") + " m" + (gdzie.Length > 0 ? " (" + gdzie + ")" : ""));
        Debug.Log("BRAXHORN_SZYLD_WLASCICIELA " + zalozone[zalozone.Count - 1]);
        return w;
    }

    /// <summary>Lico dwustronnego kasetonu na wysięgniku (kwadrat C): sama grafika w otworze ramy, środek c, przód n.</summary>
    public static bool LicoWysiegnika(string plik, Vector3 c, Vector3 n, float bok)
    {
        var m = Grafika(plik, out _);
        if (!m) return false;
        n.y = 0f; n.Normalize();
        var R = Vector3.Cross(n, Vector3.up).normalized; var U = Vector3.up;
        var s = BraxUliceKit.S(m); int i0 = s.v.Count;
        Vector3 p0 = c - R * (bok * .5f) - U * (bok * .5f), p1 = p0 + R * bok;
        s.Quad(p0, p1, p1 + U * bok, p0 + U * bok, n);
        s.uv[i0] = new Vector2(0f, 0f); s.uv[i0 + 1] = new Vector2(1f, 0f); s.uv[i0 + 2] = new Vector2(1f, 1f); s.uv[i0 + 3] = new Vector2(0f, 1f);
        Debug.Log("BRAXHORN_SZYLD_WLASCICIELA wysięgnik " + plik + " " + bok.ToString("F2") + " m");
        return true;
    }

    public static string Podsumowanie() => "BRAXHORN_SZYLDY_WLASCICIELA kasetonów " + kasetonow + (zalozone.Count > 0 ? ": " + string.Join("; ", zalozone) : " (brak paczki szyldy_r1_01 — stare szyldy)");
}

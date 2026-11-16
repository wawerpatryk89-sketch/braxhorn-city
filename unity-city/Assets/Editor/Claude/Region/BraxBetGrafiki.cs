using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN BET — GRAFIKI WŁAŚCICIELA (16.11.2026 21:51: „Masz, to ci się przyda, twój kolega to wykorzystał już.”): logo i 6 plakatów
// walk bez zmian treści, w zaszyfrowanej paczce globalnej braxhorn-sklep-bet-01 (repo publiczne; CI: install_sklep_modele.py
// rozpakowuje do Assets/Source/Prywatne/Miasto/bet_01/). Bez paczki — nic: zostają litery z kodu, ściany bez plakatów, bez klocków.
// Migotanie: obraz nigdy w płaszczyźnie innej bryły. KASETON = płyta tylna 2.5 cm + pierścień ramy, lico z logo 3 cm za krawędzią ramy
// (6.5 cm przed płytą). PLAKAT NA ŚCIANIE = obraz 2.5 cm przed licem w ramie 3.5 cm (jak BraxPlakaty). STOJAK W WITRYNIE = plakat
// dwustronny (przód do ulicy, tył z czarnej tektury) w ramie zatrzaskowej 2 cm na słupku z talerzem 3 cm — bez linek (cienkie linie
// migoczą — zasada 13:57).
internal static class BraxBetGrafiki
{
    public const string Katalog = "Assets/Source/Prywatne/Miasto/bet_01/";
    public const float AspektPlakatu = 1024f / 1536f, AspektLogo = 2f;
    public static bool Jest => System.IO.File.Exists(Katalog + "logo_kaseton.jpg");
    static readonly Dictionary<string, Material> mat = new Dictionary<string, Material>();
    public static int kasetonow, plakatow;

    static Material Obraz(string plik, string nazwa, float swiecenie)
    {
        string p = Katalog + plik, key = p + "|" + swiecenie;
        if (mat.TryGetValue(key, out var mm) && mm) return mm;
        if (!System.IO.File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(p) is TextureImporter imp && (imp.wrapMode != TextureWrapMode.Clamp || imp.maxTextureSize != 1024 || !imp.mipmapEnabled))
        { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (!t) return null;
        var m = swiecenie > 0f ? BraxMiastoA.Swieci("BRAXHORN BET: " + nazwa, Color.white, Color.white * swiecenie)
                               : BraxCityMaterials.Surface("BRAXHORN BET: " + nazwa, Color.white, .55f);
        m.SetTexture("_MainTex", t);
        if (swiecenie > 0f && m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", t); m.SetColor("_EmissionColor", Color.white * swiecenie); }
        EditorUtility.SetDirty(m);
        return mat[key] = m;
    }

    static Material Czern => BraxUliceKit.Mat("BRAXHORN BET: rama (aluminium czarne)", new Color(.035f, .035f, .04f), .35f, .6f);

    // obraz na prostokącie: c = środek, n = normalna (do patrzącego), u = 0 po lewej patrzącego
    static void Lico(Material m, Vector3 c, Vector3 n, float w, float h)
    {
        var R = Vector3.Cross(n, Vector3.up).normalized;   // prawo patrzącego
        var s = BraxUliceKit.S(m); int i0 = s.v.Count;
        Vector3 p0 = c - R * (w * .5f) - Vector3.up * (h * .5f), p1 = p0 + R * w;
        s.Quad(p0, p1, p1 + Vector3.up * h, p0 + Vector3.up * h, n);
        s.uv[i0] = new Vector2(0f, 0f); s.uv[i0 + 1] = new Vector2(1f, 0f); s.uv[i0 + 2] = new Vector2(1f, 1f); s.uv[i0 + 3] = new Vector2(0f, 1f);
    }

    // pierścień ramy w płaszczyźnie (R, up) o grubości b, od d0 do d1 wzdłuż n (bez nakładania: góra i dół na całą szerokość, boki między)
    static void Rama(UlSiatka r, Vector3 c, Vector3 n, float w, float h, float b, float d0, float d1)
    {
        var R = Vector3.Cross(n, Vector3.up).normalized; var cc = c + n * ((d0 + d1) * .5f); float g = d1 - d0;
        r.OBox(cc + Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, g);
        r.OBox(cc - Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, g);
        r.OBox(cc + R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, g);
        r.OBox(cc - R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, g);
    }

    /// <summary>Kaseton podświetlany z logo (2:1): srodek = punkt na licu ściany / listwy szyldu, n = na zewnątrz (do patrzącego),
    /// h = wysokość lica. Głębokość 12 cm. Zwraca false bez paczki.</summary>
    public static bool Kaseton(Vector3 srodek, Vector3 n, float h, float swiecenie = .9f)
    {
        var obraz = Obraz("logo_kaseton.jpg", "logo (kaseton podświetlany)", swiecenie); if (!obraz) return false;
        n.y = 0f; n.Normalize(); float w = h * AspektLogo, b = .04f, gl = .12f; var R = Vector3.Cross(n, Vector3.up).normalized;
        var cz = BraxUliceKit.S(Czern);
        cz.OBox(srodek + n * .0125f, R, Vector3.up, n, w + 2f * b, h + 2f * b, .025f);  // płyta tylna 2.5 cm (lico ≥ 2 cm od ściany / listwy szyldu)
        Rama(cz, srodek, n, w, h, b, .025f, gl);
        Lico(obraz, srodek + n * (gl - .03f), n, w, h);                                  // lico 3 cm za krawędzią ramy, 6.5 cm przed płytą
        kasetonow++;
        return true;
    }

    /// <summary>Plakat nr 1…6 w ramie z czarnego aluminium na ścianie: srodek = punkt na licu ściany, n = do pomieszczenia,
    /// h = wysokość grafiki. Zwraca false bez paczki.</summary>
    public static bool Plakat(int nr, Vector3 srodek, Vector3 n, float h)
    {
        var obraz = Obraz("plakat_0" + Mathf.Clamp(nr, 1, 6) + ".jpg", "plakat " + Mathf.Clamp(nr, 1, 6), 0f); if (!obraz) return false;
        n.y = 0f; n.Normalize(); float w = h * AspektPlakatu;
        Lico(obraz, srodek + n * .025f, n, w, h);                                         // 2.5 cm przed licem ściany
        Rama(BraxUliceKit.S(Czern), srodek, n, w, h, .03f, 0f, .035f);
        plakatow++;
        return true;
    }

    /// <summary>Plakat dwustronny na stojaku w witrynie (przód do ulicy): podstawa = punkt na posadzce pod plakatem, n = do ulicy,
    /// h = wysokość grafiki, yDol = wysokość dolnej krawędzi nad posadzką. Zwraca false bez paczki.</summary>
    public static bool Stojak(int nr, Vector3 podstawa, Vector3 n, float h, float yDol)
    {
        var obraz = Obraz("plakat_0" + Mathf.Clamp(nr, 1, 6) + ".jpg", "plakat " + Mathf.Clamp(nr, 1, 6), 0f); if (!obraz) return false;
        n.y = 0f; n.Normalize(); float w = h * AspektPlakatu; var c = podstawa + Vector3.up * (yDol + h * .5f);
        Lico(obraz, c, n, w, h);
        var tyl = BraxUliceKit.S(BraxUliceKit.Mat("BRAXHORN BET: plakat — tył (czarna tektura)", new Color(.05f, .05f, .055f), .9f));
        var R = Vector3.Cross(n, Vector3.up).normalized; int i0 = tyl.v.Count;
        Vector3 q0 = c + R * (w * .5f) - Vector3.up * (h * .5f), q1 = q0 - R * w;
        tyl.Quad(q0, q1, q1 + Vector3.up * h, q0 + Vector3.up * h, -n);                    // tył w tej samej płaszczyźnie, odwrotnie (niewidoczny z przodu)
        var cz = BraxUliceKit.S(Czern);
        Rama(cz, c, n, w, h, .025f, -.01f, .01f);                                         // rama zatrzaskowa 2 cm
        var p = podstawa - n * .045f;                                                     // słupek 4.5 cm za plakatem
        cz.Walec(p, .17f, .03f, 20);                                                      // talerz Ø 34 cm, 3 cm
        cz.Rura(p + Vector3.up * .03f, p + Vector3.up * (yDol + h + .02f), .015f, 10);
        // dwa uchwyty słupek–rama za górną i dolną listwą ramy (poza polem grafiki — bez płaszczyzny tuż za plakatem)
        foreach (float yy in new[] { yDol + h + .0125f, yDol - .0125f })
            cz.OBox(podstawa + Vector3.up * yy - n * .035f, R, Vector3.up, n, .06f, .025f, .05f);
        plakatow++;
        return true;
    }
}

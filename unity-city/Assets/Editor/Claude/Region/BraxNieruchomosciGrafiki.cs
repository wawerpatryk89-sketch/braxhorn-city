using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// WAWER NIERUCHOMOŚCI — GRAFIKI WŁAŚCICIELA (16.11.2026 22:57: „Ty to zamówiłeś na recepcję”; 23:01: „To zrób w ramkach”): logo
// (W z dachami), 3 zdjęcia (kawalerka, ulica willowa, apartament z widokiem na marinę) i 3 ogłoszenia (Zamość, Tur, Bydgoszcz) bez zmian treści, w zaszyfrowanej paczce globalnej braxhorn-sklep-nieruchomosci-01 (repo publiczne; CI: install_sklep_modele.py
// rozpakowuje do Assets/Source/Prywatne/Miasto/nieruchomosci_01/). Bez paczki — nic: biuro jak dotąd (tablica oferty za ladą).
// LOGO ZA RECEPCJĄ = kaseton podświetlany 1.6:1 (płyta tylna 2.5 cm 2 cm od ściany, pierścień ramy, lico 3 cm za krawędzią ramy)
// + nazwa firmy z liter 3D (lico stal szczotkowana, boki grafit) na grafitowym nośniku z linią LED pod kasetonem.
// ZDJĘCIA I OGŁOSZENIA = obraz 1.2 cm przed tyłem ramy (2.3 cm za licem ramy) w ramie z czarnego aluminium 3.5 cm (jak plakaty BET i rzuty mieszkań);
// OGŁOSZENIE W WITRYNIE = dwustronna rama zatrzaskowa na słupku z talerzem (przód do ulicy, tył z czarnej tektury), bez linek.
internal static class BraxNieruchomosciGrafiki
{
    public const string Katalog = "Assets/Source/Prywatne/Miasto/nieruchomosci_01/";
    public const float AspektKasetonu = 1.6f;
    /// <summary>szerokość / wysokość grafik 1…6: plakat_01 poziomy 1536 × 1024, pozostałe pionowe 1024 × 1536
    /// (1–3 zdjęcia, 4 Zamość ul. Poznańska 2, 5 Tur ul. Wiśniowa 4, 6 Bydgoszcz ul. Marii Curie-Skłodowskiej)</summary>
    public static readonly float[] AspektPlakatu = { 1536f / 1024f, 1024f / 1536f, 1024f / 1536f, 1024f / 1536f, 1024f / 1536f, 1024f / 1536f };
    public const int Grafik = 6;
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
        var m = swiecenie > 0f ? BraxMiastoA.Swieci("WAWER NIERUCHOMOŚCI: " + nazwa, Color.white, Color.white * swiecenie)
                               : BraxCityMaterials.Surface("WAWER NIERUCHOMOŚCI: " + nazwa, Color.white, .55f);
        m.SetTexture("_MainTex", t);
        if (swiecenie > 0f && m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", t); m.SetColor("_EmissionColor", Color.white * swiecenie); }
        EditorUtility.SetDirty(m);
        return mat[key] = m;
    }

    static Material Czern => BraxUliceKit.Mat("WAWER NIERUCHOMOŚCI: rama (aluminium czarne)", new Color(.035f, .035f, .04f), .35f, .6f);
    // litery we wnętrzu: lico ze stali nierdzewnej szczotkowanej, boki grafit (zasada 10.11 — bez złota w kodzie; złoto tylko w grafice
    // właściciela), na nośniku z grafitowego aluminium z linią LED pod spodem (zasada 11.11 — napis na nośniku z marginesami i światłem)
    public static Material LicoStal => BraxUliceKit.Mat("WAWER NIERUCHOMOŚCI: litery — lico (stal nierdzewna szczotkowana)", new Color(.78f, .79f, .8f), .3f, .9f);
    public static Material BokiGrafit => BraxUliceKit.Mat("litery przestrzenne: boki grafit", new Color(.06f, .06f, .065f), .5f, .4f);

    /// <summary>Nazwa firmy pod kasetonem: nośnik z grafitowego aluminium 2.5 cm na dystansach 2 cm od ściany (marginesy 12 cm),
    /// litery 3D 5 cm na licu nośnika, profil LED 2.5 cm pod nośnikiem. srodekDol = środek dolnej krawędzi nośnika na licu ściany.</summary>
    public static float NazwaFirmy(string tekst, Vector3 srodekDol, Vector3 n, float cap)
    {
        n.y = 0f; n.Normalize(); var R = Vector3.Cross(n, Vector3.up).normalized;
        float wN = BraxUliceKit.SzerokoscNapisu(tekst, cap), w = wN + .24f, h = cap + .2f;
        var c0 = srodekDol + n * .02f + Vector3.up * (h * .5f);
        BraxUliceKit.S(BraxUliceKit.Mat("WAWER NIERUCHOMOŚCI: nośnik napisu (aluminium grafit)", new Color(.11f, .115f, .12f), .4f, .55f))
            .OBox(c0 + n * .0125f, R, Vector3.up, n, w, h, .025f);
        BraxUliceKit.S(BraxMiastoA.Swieci("WAWER NIERUCHOMOŚCI: linia LED pod napisem", new Color(1f, .97f, .9f), new Color(1.2f, 1.15f, 1.05f)))
            .OBox(c0 - Vector3.up * (h * .5f + .0125f) + n * .0125f, R, Vector3.up, n, w - .04f, .025f, .015f);   // profil 2.5 cm (dno ≥ 2 cm od dna nośnika)
        BraxUliceKit.Napis3D(tekst, c0 - Vector3.up * (cap * .5f) + n * .025f, n, cap, .05f, LicoStal, BokiGrafit);
        return w;
    }

    // obraz na prostokącie: c = środek, n = normalna (do patrzącego), u = 0 po lewej patrzącego
    static void Lico(Material m, Vector3 c, Vector3 n, float w, float h)
    {
        var R = Vector3.Cross(n, Vector3.up).normalized;   // prawo patrzącego
        var s = BraxUliceKit.S(m); int i0 = s.v.Count;
        Vector3 p0 = c - R * (w * .5f) - Vector3.up * (h * .5f), p1 = p0 + R * w;
        s.Quad(p0, p1, p1 + Vector3.up * h, p0 + Vector3.up * h, n);
        s.uv[i0] = new Vector2(0f, 0f); s.uv[i0 + 1] = new Vector2(1f, 0f); s.uv[i0 + 2] = new Vector2(1f, 1f); s.uv[i0 + 3] = new Vector2(0f, 1f);
    }

    // pierścień ramy w płaszczyźnie (R, up) o grubości b, od d0 do d1 wzdłuż n (góra i dół na całą szerokość, boki między)
    static void Rama(UlSiatka r, Vector3 c, Vector3 n, float w, float h, float b, float d0, float d1)
    {
        var R = Vector3.Cross(n, Vector3.up).normalized; var cc = c + n * ((d0 + d1) * .5f); float g = d1 - d0;
        r.OBox(cc + Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, g);
        r.OBox(cc - Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, g);
        r.OBox(cc + R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, g);
        r.OBox(cc - R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, g);
    }

    /// <summary>Kaseton podświetlany z logo (1.6:1) na ścianie za recepcją: srodek = punkt na licu ściany, n = do pomieszczenia,
    /// h = wysokość lica. Płyta tylna 2 cm od ściany (bez wspólnej płaszczyzny z gładzią), głębokość 12 cm. Zwraca false bez paczki.</summary>
    public static bool Kaseton(Vector3 srodek, Vector3 n, float h, float swiecenie = .85f)
    {
        var obraz = Obraz("logo_kaseton.jpg", "logo (kaseton podświetlany)", swiecenie); if (!obraz) return false;
        n.y = 0f; n.Normalize(); float w = h * AspektKasetonu, b = .04f, gl = .12f; var R = Vector3.Cross(n, Vector3.up).normalized;
        var c0 = srodek + n * .02f;                                                       // dystanse 2 cm
        var cz = BraxUliceKit.S(Czern);
        cz.OBox(c0 + n * .0125f, R, Vector3.up, n, w + 2f * b, h + 2f * b, .025f);       // płyta tylna 2.5 cm
        Rama(cz, c0, n, w, h, b, .025f, gl);
        Lico(obraz, c0 + n * (gl - .03f), n, w, h);                                       // lico 3 cm za krawędzią ramy, 6.5 cm przed płytą
        kasetonow++;
        return true;
    }

    /// <summary>Zdjęcie (1…3) albo ogłoszenie (4…6) w ramie z czarnego aluminium: srodek = punkt na licu podkładu (≥ 3 cm od ściany), n = do
    /// pomieszczenia, h = wysokość zdjęcia (szerokość z proporcji pliku). Zwraca szerokość albo 0 bez paczki.</summary>
    public static float Plakat(int nr, Vector3 srodek, Vector3 n, float h)
    {
        int i = Mathf.Clamp(nr, 1, Grafik);
        var obraz = Obraz("plakat_0" + i + ".jpg", (i <= 3 ? "zdjęcie " : "ogłoszenie ") + i, 0f); if (!obraz) return 0f;
        n.y = 0f; n.Normalize(); float w = h * AspektPlakatu[i - 1];
        Lico(obraz, srodek + n * .012f, n, w, h);                                         // 1.2 cm przed tyłem ramy — 2.3 cm za licem ramy (≥ 2 cm)
        Rama(BraxUliceKit.S(Czern), srodek, n, w, h, .03f, 0f, .035f);
        plakatow++;
        return w;
    }

    /// <summary>Szerokość grafiki nr 1…6 o wysokości h (do rozmieszczenia na ścianie).</summary>
    public static float Szerokosc(int nr, float h) => h * AspektPlakatu[Mathf.Clamp(nr, 1, Grafik) - 1];

    /// <summary>Ogłoszenie dwustronne na stojaku w witrynie (przód do ulicy): podstawa = punkt na posadzce pod ramą, n = do ulicy,
    /// h = wysokość grafiki, yDol = dolna krawędź nad posadzką. Zwraca false bez paczki.</summary>
    public static bool Stojak(int nr, Vector3 podstawa, Vector3 n, float h, float yDol)
    {
        int i = Mathf.Clamp(nr, 1, Grafik);
        var obraz = Obraz("plakat_0" + i + ".jpg", (i <= 3 ? "zdjęcie " : "ogłoszenie ") + i, 0f); if (!obraz) return false;
        n.y = 0f; n.Normalize(); float w = h * AspektPlakatu[i - 1]; var c = podstawa + Vector3.up * (yDol + h * .5f);
        Lico(obraz, c, n, w, h);
        var tyl = BraxUliceKit.S(BraxUliceKit.Mat("WAWER NIERUCHOMOŚCI: ogłoszenie — tył (czarna tektura)", new Color(.05f, .05f, .055f), .9f));
        var R = Vector3.Cross(n, Vector3.up).normalized; int i0 = tyl.v.Count;
        Vector3 q0 = c + R * (w * .5f) - Vector3.up * (h * .5f), q1 = q0 - R * w;
        tyl.Quad(q0, q1, q1 + Vector3.up * h, q0 + Vector3.up * h, -n);                    // tył w tej samej płaszczyźnie, odwrotnie (niewidoczny z przodu)
        var cz = BraxUliceKit.S(Czern);
        Rama(cz, c, n, w, h, .025f, -.01f, .01f);                                         // rama zatrzaskowa 2 cm
        var p = podstawa - n * .045f;                                                     // słupek 4.5 cm za ramą
        cz.Walec(p, .17f, .03f, 20);                                                      // talerz Ø 34 cm, 3 cm
        cz.Rura(p + Vector3.up * .03f, p + Vector3.up * (yDol + h + .02f), .015f, 10);
        foreach (float yy in new[] { yDol + h + .0125f, yDol - .0125f })                 // uchwyty za górną i dolną listwą ramy
            cz.OBox(podstawa + Vector3.up * yy - n * .035f, R, Vector3.up, n, .06f, .025f, .05f);
        plakatow++;
        return true;
    }
}

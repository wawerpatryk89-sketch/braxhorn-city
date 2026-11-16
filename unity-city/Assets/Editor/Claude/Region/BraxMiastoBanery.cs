using System.IO;
using UnityEditor;
using UnityEngine;

// BANERY WŁAŚCICIELA W R1 (16.11.2026): zaszyfrowana paczka globalna braxhorn-sklep-banery-r1-01 (repo publiczne; CI:
// install_sklep_modele.py rozpakowuje do Assets/Source/Prywatne/Miasto/banery_r1_01/). Grafiki bez zmian treści. Bez paczki — nic.
//  • 23:14 „Tu masz, mistrzu, do tego, do komisu?” — KOMIS SAMOCHODOWY BRAXHORN CITY: baner frontlit 5.2 × 2.9 m na stalowej ramie
//    na dachu kontenera-biura komisu przy T1 TRAMWAJOWA, przodem do ulicy (widać go zza aut), dwa naświetlacze LED na wysięgnikach.
//  • 23:16 „billboard… motywacyjny… powiesić nad główną ulicą… między tymi dwiema kamienicami… żeby wisiał, z dwóch stron” — TRENUJ.
//    ZAWALCZ O PAS MISTRZA!: baner dwustronny (blockout, nadruk z obu stron) 8 × 3.2 m w poprzek Portowej, w tunelach górnym
//    i dolnym rury stalowe Ø 48 mm, liny stalowe Ø 12 mm z napinaczami śrubowymi do kotew w filarach elewacji po obu stronach
//    ulicy; dół banera 5.8 m nad jezdnią (autobusy i ciężarówki przejeżdżają pod spodem — skrajnia 4.6 m).
// Migotanie: obraz nigdy w płaszczyźnie innej bryły — obie strony banera to ta sama płaszczyzna z przeciwnymi normalnymi (niewidoczne
// od tyłu), rama i rury poza polem obrazu, stopy i płyty kotew ≥ 2.5 cm (bez cienkich płyt na powierzchni).
internal static class BraxMiastoBanery
{
    public const string Katalog = "Assets/Source/Prywatne/Miasto/banery_r1_01/";
    public const string Komis = "komis_baner.jpg", Trenuj = "trenuj_baner.jpg";
    public const float AspektKomis = 1672f / 941f, AspektTrenuj = 1983f / 793f;
    public static bool Jest(string plik) => File.Exists(Katalog + plik);
    public static int banerow;

    static Material Obraz(string plik, string nazwa)
    {
        string p = Katalog + plik; if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(p) is TextureImporter imp && (imp.wrapMode != TextureWrapMode.Clamp || imp.maxTextureSize != 2048 || !imp.mipmapEnabled))
        { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.mipmapEnabled = true; imp.anisoLevel = 4; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (!t) return null;
        var m = BraxCityMaterials.Surface("Baner właściciela: " + nazwa + " (PVC frontlit, nadruk)", Color.white, .72f);
        m.SetTexture("_MainTex", t); EditorUtility.SetDirty(m);
        return m;
    }

    static Material Stal => BraxCityMaterials.Surface("Banery: stal ocynkowana (rama, rury, kotwy)", new Color(.62f, .64f, .66f), .42f, .75f);
    static Material Lina => BraxCityMaterials.Surface("Banery: lina stalowa ocynkowana", new Color(.55f, .57f, .59f), .4f, .8f);

    // siatka z UlSiatka jako osobny obiekt (z zapisanym assetem — jak BraxUliceKit.Zapisz)
    static void Obiekt(UlSiatka s, string nazwa, Transform rodzic, Material m, bool cienie = true)
    {
        if (s.v.Count == 0 || !m) return;
        Directory.CreateDirectory(BraxUliceKit.Out);
        var mesh = s.Zbuduj(nazwa);
        BraxZapisAssetu.Utworz(mesh, BraxUliceKit.Out + "/baner_" + BraxUliceKit.Bezpieczna(nazwa) + "_" + (banerow * 10 + rodzic.childCount) + ".asset");
        var go = new GameObject(nazwa); go.transform.SetParent(rodzic, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = cienie ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
    }

    // obraz na prostokącie (środek c, normalna n do patrzącego, prawo patrzącego R = n × up): u = 0 po lewej patrzącego
    static void Lico(UlSiatka s, Vector3 c, Vector3 n, float w, float h, bool lustro = false)
    {
        var R = Vector3.Cross(n, Vector3.up).normalized; int i0 = s.v.Count;
        Vector3 p0 = c - R * (w * .5f) - Vector3.up * (h * .5f), p1 = p0 + R * w;
        s.Quad(p0, p1, p1 + Vector3.up * h, p0 + Vector3.up * h, n);
        float u0 = lustro ? 1f : 0f, u1 = lustro ? 0f : 1f;
        s.uv[i0] = new Vector2(u0, 0f); s.uv[i0 + 1] = new Vector2(u1, 0f); s.uv[i0 + 2] = new Vector2(u1, 1f); s.uv[i0 + 3] = new Vector2(u0, 1f);
    }

    /// <summary>KOMIS: baner na ramie na dachu kontenera. srodekDach = środek krawędzi przedniej banera na wysokości dachu (y = lico dachu),
    /// n = przód (do ulicy), w = szerokość banera. Zwraca opis albo null bez paczki.</summary>
    public static string NaDachuKontenera(Transform g, Vector3 srodekDach, Vector3 n, float w)
    {
        var obraz = Obraz(Komis, "KOMIS SAMOCHODOWY BRAXHORN CITY"); if (!obraz) return null;
        n.y = 0f; n.Normalize(); var R = Vector3.Cross(n, Vector3.up).normalized;
        float h = w / AspektKomis, yDol = .35f, b = .045f;
        var c = srodekDach + Vector3.up * (yDol + h * .5f);
        var root = new GameObject("Komis: baner KOMIS SAMOCHODOWY BRAXHORN CITY (grafika właściciela)").transform; root.SetParent(g, false);
        var sO = new UlSiatka(); Lico(sO, c, n, w, h);
        var sT = new UlSiatka(); Lico(sT, c, -n, w, h);                                      // tył banera (szary PVC) — ta sama płaszczyzna, odwrotnie
        var st = new UlSiatka();
        // rama obwodowa z profilu 45 × 45 mm wokół banera (± 2.25 cm od płaszczyzny), bez nakładania na pole obrazu
        st.OBox(c + Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, b);
        st.OBox(c - Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, b);
        st.OBox(c + R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, b);
        st.OBox(c - R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, b);
        // dwie poprzeczki 45 × 45 mm za banerem (na 1/4 i 3/4 wysokości, między bokami ramy), dwa słupy 80 × 80 mm za poprzeczkami,
        // stopy 300 × 300 × 25 mm na dachu, zastrzały z rury Ø 48 do tyłu na stopach 200 × 200 × 25 mm
        float yG = yDol + h + b;
        foreach (float fy in new[] { .25f, .75f })
            st.OBox(srodekDach + Vector3.up * (yDol + h * fy) - n * (b * .5f + b * .5f), R, Vector3.up, n, w, b, b);
        foreach (float sx in new[] { -w * .5f + .55f, w * .5f - .55f })
        {
            var p = srodekDach + R * sx - n * (b * .5f + b + .04f);                               // oś słupa (lico słupa = tył poprzeczki)
            st.OBox(p + Vector3.up * .0125f, R, Vector3.up, n, .3f, .025f, .3f);                // stopa
            st.OBox(p + Vector3.up * (.025f + (yG - .025f) * .5f), R, Vector3.up, n, .08f, yG - .025f, .08f);
            var a0 = p + Vector3.up * (yDol + h * .55f) - n * .04f; var a1 = p - n * 1.6f + Vector3.up * .025f;
            st.Rura(a0, a1, .024f, 8);                                                           // zastrzał
            st.OBox(a1 - Vector3.up * .0125f, R, Vector3.up, n, .2f, .025f, .2f);                // stopa zastrzału
        }
        // dwa naświetlacze LED na wysięgnikach nad banerem, skierowane w dół na nadruk (baner frontlit)
        var led = new UlSiatka(); var obud = new UlSiatka();
        foreach (float sx in new[] { -w * .25f, w * .25f })
        {
            var u = c + Vector3.up * (h * .5f + b) + R * sx;
            st.Rura(u, u + n * .75f + Vector3.up * .12f, .02f, 8);
            var q = u + n * .75f + Vector3.up * .12f;
            obud.OBox(q + Vector3.up * .05f, R, Vector3.up, n, .42f, .1f, .26f);
            led.OBox(q - Vector3.up * .0175f, R, Vector3.up, n, .36f, .035f, .2f);                // szyba LED 3.5 cm pod obudową (bez wspólnej płaszczyzny)
        }
        Obiekt(sO, "Komis: baner — nadruk (KOMIS SAMOCHODOWY BRAXHORN CITY)", root, obraz);
        Obiekt(sT, "Komis: baner — tył (szary PVC)", root, BraxCityMaterials.Surface("Banery: tył banera (szary PVC)", new Color(.42f, .43f, .44f), .8f));
        Obiekt(st, "Komis: baner — rama, słupy, zastrzały (stal ocynkowana)", root, Stal);
        Obiekt(obud, "Komis: baner — naświetlacze (obudowy)", root, BraxCityMaterials.Surface("Banery: obudowa naświetlacza (aluminium grafit)", new Color(.12f, .125f, .13f), .4f, .6f));
        Obiekt(led, "Komis: baner — naświetlacze (LED)", root, BraxRegionMeble.Swiecacy("Banery: naświetlacz LED (biały ciepły)", new Color(1f, .95f, .86f), 1.4f), false);
        banerow++;
        return "baner " + w.ToString("F1") + " × " + h.ToString("F2") + " m na dachu kontenera, góra " + (srodekDach.y + yG).ToString("F1") + " m";
    }

    /// <summary>PORTOWA: baner dwustronny w poprzek jezdni. x = oś banera, zPld/zPln = lica elewacji (płd. / płn.), xKotPld/xKotPln = osie
    /// filarów z kotwami, yJezdni = lico jezdni pod banerem, szer = szerokość banera w poprzek ulicy. Zwraca opis albo null bez paczki.</summary>
    public static string NadUlica(Transform root, float x, float zPld, float zPln, float xKotPld, float xKotPln, float yJezdni, float szer)
    {
        var obraz = Obraz(Trenuj, "TRENUJ. ZAWALCZ O PAS MISTRZA!"); if (!obraz) return null;
        float h = szer / AspektTrenuj, yD = yJezdni + 5.8f, yG = yD + h, zc = (zPld + zPln) * .5f;
        var g = new GameObject("Ulica: baner nad jezdnią TRENUJ. ZAWALCZ O PAS MISTRZA! (grafika właściciela, dwustronny)").transform; g.SetParent(root, false);
        var c = new Vector3(x, (yD + yG) * .5f, zc);
        // nadruk z obu stron: od zachodu (patrzący na +x widzi lico −x) i od wschodu — każdy czytelny, nie lustrzany
        var sA = new UlSiatka(); Lico(sA, c, Vector3.left, szer, h);
        var sB = new UlSiatka(); Lico(sB, c, Vector3.right, szer, h);
        var st = new UlSiatka(); var ln = new UlSiatka();
        // tunele z rurami Ø 48 mm wzdłuż górnej i dolnej krawędzi (rura poza polem obrazu: oś 2.4 cm nad / pod krawędzią)
        float rr = .024f, z0 = zc - szer * .5f, z1 = zc + szer * .5f;
        foreach (float yy in new[] { yG + rr, yD - rr }) st.Rura(new Vector3(x, yy, z0 - .12f), new Vector3(x, yy, z1 + .12f), rr, 10);
        // kotwy w filarach: płyta 250 × 250 × 25 mm, ucho; liny Ø 12 mm z napinaczem śrubowym 0.3 m przy kotwie (cztery liny)
        var kot = new UlSiatka(); int lin = 0;
        foreach (var (zf, xk, nz) in new[] { (zPld, xKotPld, 1f), (zPln, xKotPln, -1f) })
        {
            var nn = new Vector3(0f, 0f, nz);
            foreach (var (yr, yk) in new[] { (yG + rr, yG + .45f), (yD - rr, yD - .25f) })
            {
                var pk = new Vector3(xk, yk, zf);
                kot.OBox(pk + nn * .0125f, Vector3.right, Vector3.up, nn, .25f, .25f, .025f);       // płyta kotwy na kołkach
                kot.Rura(pk + nn * .025f, pk + nn * .1f, .012f, 8);                                // ucho kotwy
                var a = pk + nn * .1f; var bE = new Vector3(x, yr, nz > 0 ? z0 - .12f : z1 + .12f); // koniec rury
                var dir = (bE - a).normalized;
                kot.Rura(a + dir * .05f, a + dir * .35f, .022f, 8);                                // napinacz śrubowy
                ln.Rura(a, a + dir * .05f, .006f, 6); ln.Rura(a + dir * .35f, bE, .006f, 6);        // lina
                lin++;
            }
        }
        Obiekt(sA, "Ulica: baner TRENUJ — nadruk od zachodu", g, obraz, true);
        Obiekt(sB, "Ulica: baner TRENUJ — nadruk od wschodu", g, obraz, true);
        Obiekt(st, "Ulica: baner TRENUJ — rury w tunelach (stal ocynkowana)", g, Stal);
        Obiekt(kot, "Ulica: baner TRENUJ — kotwy i napinacze (stal ocynkowana)", g, Stal);
        Obiekt(ln, "Ulica: baner TRENUJ — liny stalowe Ø 12 mm", g, Lina, false);
        banerow++;
        return "baner " + szer.ToString("F1") + " × " + h.ToString("F2") + " m nad jezdnią (x " + x.ToString("F1") + ", dół " + (yD - yJezdni).ToString("F1") + " m nad jezdnią), lin " + lin;
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// KOMIS PRZY T1 TRAMWAJOWA — PŁOT I BIURO Z WEJŚCIEM (właściciel 16.11.2026 23:37–23:38: „Na płocie go powieś”, „komis, zrób płot
// normalny i powieś go na płocie i tę budkę zrób trochę lepszą, że tam można było wejść”).
//  • PŁOT PANELOWY 3D jak na prawdziwych placach: podmurówka betonowa 25 × 6 cm, słupki 60 × 60 mm co 2.5 m z kapturkami, panele
//    z drutu (oczka 200 × 50 mm, druty 5 mm) — siatka W TEKSTURZE z kanałem alfa (zasada 16.11: linie < 25 cm w teksturę, nie
//    w geometrię; mipmapy z zachowaniem pokrycia — siatka nie znika z daleka), stal ocynkowana malowana RAL 7016. Od ulicy, po bokach
//    i z tyłu; wjazd 8 m przy pylonie otwarty. Baner właściciela wisi na płocie od ulicy (BraxMiastoBanery.NaPlocie).
//  • BIURO W KONTENERZE 6 × 2.4 m × K z WEJŚCIEM: drzwi stalowe na zawiasach (BraxCityDoor — same się otwierają i zamykają), stopień,
//    okno z szybą przezroczystą, w środku panele podłogowe, ściany z płyt laminowanych (listwy H w teksturze), sufit z oprawą LED,
//    grafika komisu właściciela w ramce za biurkiem, biurko komisanta z laptopem,
//    krzesło i dwa krzesła klienta, szafa na dokumenty, szafka na kluczyki, roślina; AKCJA „ZAPYTAJ O AUTO” przy biurku.
// Migotanie: każda warstwa ściany jako bryła, lica ≥ 2 cm od siebie; płyty podłogi i stopnie ≥ 2.5 cm (bez cienkich płyt na powierzchni).
public static partial class BraxRegionMiastoB
{
    static Material plotPanel;
    internal const float OdstepBanera = .055f;                                               // baner przed płaszczyzną paneli

    // tekstura panelu 3D: kafel 0.4 × 0.4 m (8 drutów pionowych co 50 mm, 2 pary poziomych co 200 mm), 512 px, alfa = drut
    static Material PlotPanel()
    {
        if (plotPanel) return plotPanel;
        const int N = 512; var px = new Color32[N * N];
        float pxm = N / .4f;                                                                  // pikseli na metr
        int dr = Mathf.Max(2, Mathf.RoundToInt(.005f * pxm * 1.25f));                         // drut 5 mm (z zapasem na mipmapy)
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool pion = (x % 64) < dr;
                int yy = y % 256; bool poz = yy < dr || (yy >= 10 && yy < 10 + dr);           // poziome druty podwójne (co 200 mm)
                byte a = (byte)(pion || poz ? 255 : 0);
                byte c = (byte)(pion ? 255 : 235);                                            // lekki cień na drutach poziomych
                px[y * N + x] = new Color32(c, c, c, a);
            }
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, true); tx.SetPixels32(px); tx.Apply();
        string dir = BraxUliceKit.Out + "/Ekrany"; Directory.CreateDirectory(dir);
        string path = dir + "/komis_plot_panel_3d.png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .5f;
            imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.maxTextureSize = 512; imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = BraxCityMaterials.Surface("Komis: płot panelowy 3D (stal ocynkowana, RAL 7016)", new Color(.16f, .17f, .18f), .5f, .35f);
        if (t) { m.SetTexture("_MainTex", t); m.mainTextureScale = new Vector2(1f / .4f, 1f / .4f); }
        m.SetFloat("_Cutoff", .5f); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Mode", 1f); m.EnableKeyword("_ALPHATEST_ON");
        // bez _Cull 0: panel to dwa quady o przeciwnych normalnych (każda strona oświetlona po swojemu, bez z-fightingu między nimi)
        m.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(m);
        return plotPanel = m;
    }

    static Material plytaWn;

    // płyta ścienna biura kontenerowego: laminat biały, łączenia co 1.25 m listwą H 3 cm — listwy W TEKSTURZE (zasada 16.11: linie < 25 cm
    // w teksturę), kafel 1.25 m w poziomie (UV Bud = metry świata, pion bez zmian)
    static Material PlytaWnetrza()
    {
        if (plytaWn) return plytaWn;
        const int N = 512; var px = new Color32[N * N];
        int lw = Mathf.Max(6, Mathf.RoundToInt(.03f * N / 1.25f));
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                byte sz = (byte)(240 + ((x * 7 + y * 13) % 5));                                      // lekka faktura laminatu (bez wzoru z daleka)
                var c = new Color32(sz, sz, (byte)(sz - 3), 255);
                if (x < lw) c = (x == 0 || x == lw - 1) ? new Color32(150, 152, 154, 255) : new Color32(212, 214, 215, 255);
                px[y * N + x] = c;
            }
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, true); tx.SetPixels32(px); tx.Apply();
        string dir = BraxUliceKit.Out + "/Ekrany"; Directory.CreateDirectory(dir);
        string path = dir + "/komis_plyta_scienna.png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        { imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.maxTextureSize = 512; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = BraxCityMaterials.Surface("Komis: biuro — płyta ścienna (laminat biały, listwy H co 1.25 m)", Color.white, .45f, 0f);
        if (t) { m.SetTexture("_MainTex", t); m.mainTextureScale = new Vector2(1f / 1.25f, 1f); }
        EditorUtility.SetDirty(m);
        return plytaWn = m;
    }

    /// <summary>Odcinek płotu od a do b (na gruncie y): podmurówka, słupki co 2.5 m, panele dwustronne. slA / slB = słupek na końcu a / b
    /// (false, gdy narożnik ma już słupek z sąsiedniego odcinka — bez zdublowanych brył). Zwraca liczbę przęseł.</summary>
    static int OdcinekPlotu(Transform g, string nazwa, Vector3 a, Vector3 b, float hP, BraxRegionMeble.Bud sl, BraxRegionMeble.Bud pod, BraxRegionMeble.Bud pan, bool slA = true, bool slB = true)
    {
        var d = b - a; d.y = 0f; float L = d.magnitude; if (L < .3f) return 0; d /= L;
        var nn = Vector3.Cross(Vector3.up, d); var rot = Quaternion.LookRotation(nn, Vector3.up);
        int n = Mathf.Max(1, Mathf.CeilToInt(L / 2.5f)); float krok = L / n;
        const float hPod = .25f, gPod = .05f, sx = .06f, sz = .06f;                                // słupek 60 × 60: lica 3 cm od panelu i od panelu odcinka w narożniku
        for (int i = 0; i <= n; i++)
        {
            if ((i == 0 && !slA) || (i == n && !slB)) continue;
            var p = a + d * (krok * i);
            sl.Box(p + Vector3.up * ((hP + .05f) * .5f), new Vector3(sx, hP + .05f, sz), rot);
            sl.Box(p + Vector3.up * (hP + .05f + .0125f), new Vector3(sx + .01f, .025f, sz + .01f), rot);   // kapturek
        }
        for (int i = 0; i < n; i++)
        {
            var p0 = a + d * (krok * i + sx * .5f); var p1 = a + d * (krok * (i + 1) - sx * .5f);
            pod.Box((p0 + p1) * .5f + Vector3.up * (hPod * .5f - .05f), new Vector3((p1 - p0).magnitude - .004f, hPod + .05f, gPod), rot);   // lokalne x = wzdłuż płotu; 5 cm w gruncie, 2 mm luzu przy słupkach (łącznik)
            float y0 = hPod + .02f, y1 = hP;
            Vector3 A0 = p0 + Vector3.up * y0, B0 = p1 + Vector3.up * y0, B1 = p1 + Vector3.up * y1, A1 = p0 + Vector3.up * y1;
            pan.Quad(A0, B0, B1, A1); pan.Quad(B0, A0, A1, B1);                                   // obie strony (siatka widoczna z placu i z ulicy)
        }
        var k = new GameObject("kolizja: " + nazwa); k.transform.SetParent(g, false); k.transform.position = (a + b) * .5f + Vector3.up * (hP * .5f);
        k.transform.rotation = rot; k.AddComponent<BoxCollider>().size = new Vector3(L, hP, .08f);
        return n;
    }

    /// <summary>Płot komisu: od ulicy (bez wjazdu xWj0…xWj1), boki i tył. srodekBanera = środek dołu banera na licu płotu od ulicy.</summary>
    static string KomisPlot(Transform g, Rect R, float y, float xWj0, float xWj1, out Vector3 srodekBanera, out float hBanera)
    {
        float hP = 1.73f * K, zU = R.yMax - .2f, zT = R.yMin + .1f, xZ = R.xMin + .1f, xW = R.xMax - .1f;
        var sl = new BraxRegionMeble.Bud(); var pod = new BraxRegionMeble.Bud(); var pan = new BraxRegionMeble.Bud();
        int prz = 0;
        prz += OdcinekPlotu(g, "płot komisu od ulicy (zachód)", new Vector3(xZ, y, zU), new Vector3(xWj0, y, zU), hP, sl, pod, pan, slA: false);   // narożniki: słupki boków
        prz += OdcinekPlotu(g, "płot komisu od ulicy (wschód)", new Vector3(xWj1, y, zU), new Vector3(xW, y, zU), hP, sl, pod, pan, slB: false);
        prz += OdcinekPlotu(g, "płot komisu (bok zachodni)", new Vector3(xZ, y, zT), new Vector3(xZ, y, zU), hP, sl, pod, pan);
        prz += OdcinekPlotu(g, "płot komisu (bok wschodni)", new Vector3(xW, y, zT), new Vector3(xW, y, zU), hP, sl, pod, pan);
        prz += OdcinekPlotu(g, "płot komisu (tył)", new Vector3(xZ, y, zT), new Vector3(xW, y, zT), hP, sl, pod, pan, false, false);
        sl.Obiekt("Komis: płot — słupki 60 × 60 z kapturkami (RAL 7016)", g, BraxCityMaterials.Surface("Komis: płot — słupki (stal malowana RAL 7016)", new Color(.16f, .17f, .18f), .5f, .35f), false, true);
        pod.Obiekt("Komis: płot — podmurówka betonowa", g, BraxRegionMeble.Tex("Komis: płot — podmurówka (beton prefabrykowany)", 1.2f, new Color(.78f, .78f, .76f), .9f, "concrete_floor_02", "concrete_wall_008"), false, true);
        pan.Obiekt("Komis: płot — panele 3D (siatka)", g, PlotPanel(), false, true);
        // baner na zachodnim odcinku od ulicy: środek dołu 5.5 cm przed panelem (2.5 cm przed licem słupków), od strony ulicy
        hBanera = hP - .25f - .1f;
        srodekBanera = new Vector3(R.xMin + 9.8f, y + .25f + .05f, zU + OdstepBanera);   // między biurem a autami
        return "płot panelowy 3D " + hP.ToString("F2") + " m, przęseł " + prz + ", wjazd " + (xWj1 - xWj0).ToString("F1") + " m";
    }

    /// <summary>Biuro komisu w kontenerze z wejściem: (x0, z0) narożnik, wymiary × K, drzwi i okno od placu (+z).</summary>
    static string KomisBiuro(Transform g, Rect R, float y)
    {
        float L = 6.06f * K, B = 2.44f * K, Hc = 2.60f * K, x0 = R.xMin + .6f, z0 = R.yMin + .5f, x1 = x0 + L, z1 = z0 + B, r = .12f * K;
        var root = new GameObject("Komis: biuro w kontenerze (z wejściem)").transform; root.SetParent(g, false);
        var rama = new BraxRegionMeble.Bud(); var bl = new BraxRegionMeble.Bud(); var wn = new BraxRegionMeble.Bud(); var pod = new BraxRegionMeble.Bud();
        var szk = new BraxRegionMeble.Bud(); var dach = new BraxRegionMeble.Bud(); var suf = new BraxRegionMeble.Bud(); var ozd = new BraxRegionMeble.Bud();
        // rama stalowa: słupy narożne i belki górne / dolne
        foreach (float xx in new[] { x0, x1 - r }) foreach (float zz in new[] { z0, z1 - r }) rama.MinMax(new Vector3(xx, y, zz), new Vector3(xx + r, y + Hc, zz + r));
        foreach (float yy in new[] { y, y + Hc - r })
        {
            rama.MinMax(new Vector3(x0 + r, yy, z0), new Vector3(x1 - r, yy + r, z0 + r)); rama.MinMax(new Vector3(x0 + r, yy, z1 - r), new Vector3(x1 - r, yy + r, z1));
            rama.MinMax(new Vector3(x0, yy, z0 + r), new Vector3(x0 + r, yy + r, z1 - r)); rama.MinMax(new Vector3(x1 - r, yy, z0 + r), new Vector3(x1, yy + r, z1 - r));
        }
        // ściany: blacha trapezowa (zewnątrz, 4 cm za licem ramy, 5 cm grubości) + płyta laminowana 2 cm (wewnątrz); otwory drzwi i okna
        float yP = y + r, yS = y + Hc - r, e0 = .04f, e1 = .065f, e2 = .09f, w1 = .11f;     // lico blachy, tył blachy, tył płyty (2.5 cm pustki), lico płyty
        float dW = .9f * K, dH = 2.0f * K, dx = x1 - 1.3f * K;                                // drzwi
        float ox0 = x0 + 1.4f * K, ox1 = x0 + 2.6f * K, oy0 = y + .95f * K, oy1 = y + 1.95f * K; // okno
        void SciX(float zA, float zB, float zB2, float zC, float xa, float xb, float ya, float yb)    // ściana wzdłuż x: blacha zA…zB, pustka, płyta zB2…zC
        { if (xb - xa < .01f || yb - ya < .01f) return; bl.MinMax(new Vector3(xa, ya, zA), new Vector3(xb, yb, zB)); wn.MinMax(new Vector3(xa, ya, zB2), new Vector3(xb, yb, zC)); }
        void SciZ(float xA, float xB, float xB2, float xC, float za, float zb, float ya, float yb)
        { if (zb - za < .01f || yb - ya < .01f) return; bl.MinMax(new Vector3(xA, ya, za), new Vector3(xB, yb, zb)); wn.MinMax(new Vector3(xB2, ya, za), new Vector3(xC, yb, zb)); }
        float ix0 = x0 + r, ix1 = x1 - r, iz0 = z0 + r, iz1 = z1 - r;
        SciX(z0 + e0, z0 + e1, z0 + e2, z0 + w1, ix0, ix1, yP, yS);                                       // tył (z0)
        SciZ(x0 + e0, x0 + e1, x0 + e2, x0 + w1, iz0, iz1, yP, yS);                                       // bok zachodni
        SciZ(x1 - e0, x1 - e1, x1 - e2, x1 - w1, iz0, iz1, yP, yS);                                       // bok wschodni
        {   // front (z1) z otworami: od zachodu do okna, pod i nad oknem, okno → drzwi, nad drzwiami, drzwi → wschód
            float a = z1 - e0, b = z1 - e1, b2 = z1 - e2, c = z1 - w1;
            SciX(a, b, b2, c, ix0, ox0, yP, yS); SciX(a, b, b2, c, ox0, ox1, yP, oy0); SciX(a, b, b2, c, ox0, ox1, oy1, yS);
            SciX(a, b, b2, c, ox1, dx - dW * .5f, yP, yS); SciX(a, b, b2, c, dx - dW * .5f, dx + dW * .5f, yP + dH, yS); SciX(a, b, b2, c, dx + dW * .5f, ix1, yP, yS);
            // ościeżnice (stal 5 cm) w otworach — od lica blachy do lica płyty
            foreach (var (xa, xb, ya, yb) in new[] { (ox0 - .05f, ox0, oy0 - .05f, oy1 + .05f), (ox1, ox1 + .05f, oy0 - .05f, oy1 + .05f), (ox0, ox1, oy0 - .05f, oy0), (ox0, ox1, oy1, oy1 + .05f),
                                                      (dx - dW * .5f - .05f, dx - dW * .5f, yP, yP + dH + .05f), (dx + dW * .5f, dx + dW * .5f + .05f, yP, yP + dH + .05f), (dx - dW * .5f, dx + dW * .5f, yP + dH, yP + dH + .05f) })
                rama.MinMax(new Vector3(xa, ya, c - .025f), new Vector3(xb, yb, a + .025f));   // 2.5 cm przed licem blachy i płyty
            // szyba przezroczysta w osi ściany (dwustronna) i parapet wewnętrzny 2.5 cm
            float zs = (a + c) * .5f;
            foreach (var nn in new[] { 1f, -1f })
            { var q0 = new Vector3(ox0, oy0, zs); var q1 = new Vector3(ox1, oy0, zs); var q2 = new Vector3(ox1, oy1, zs); var q3 = new Vector3(ox0, oy1, zs); if (nn > 0) szk.Quad(q0, q1, q2, q3); else szk.Quad(q1, q0, q3, q2); }
            wn.MinMax(new Vector3(ox0 - .08f, oy0 - .05f - .025f, c - .2f), new Vector3(ox1 + .08f, oy0 - .05f, c));
        }
        // podłoga: płyta 4 cm na belkach dolnych (lico = góra belek), wykładzina; dach z blachy 5 cm, sufit podwieszony 2 cm pod nim
        pod.MinMax(new Vector3(ix0, yP - .04f, iz0), new Vector3(ix1, yP, iz1));
        dach.MinMax(new Vector3(ix0, yS - .05f, iz0), new Vector3(ix1, yS, iz1));
        suf.MinMax(new Vector3(x0 + w1, yS - .05f - .025f, z0 + w1), new Vector3(x1 - w1, yS - .05f, z1 - w1));
        // stopień przed drzwiami (krata pomostowa na kątownikach), 2 × 7 cm
        ozd.MinMax(new Vector3(dx - .55f, y, z1 + .02f), new Vector3(dx + .55f, y + (yP - y) * .5f, z1 + .42f));
        rama.Obiekt("Komis: biuro — rama stalowa, ościeżnice", root, BraxRegionMeble.Tex("Komis: kontener — rama (stal malowana grafit)", 1.0f, new Color(.30f, .31f, .32f), .55f, "painted_metal_shutter", "metal_plate"), false, true);
        bl.Obiekt("Komis: biuro — blacha trapezowa (biała)", root, BraxRegionMeble.Tex("Komis: kontener — blacha trapezowa (biała)", 1.2f, new Color(.90f, .91f, .90f), .55f, "corrugated_iron", "painted_metal_shutter"), false, true);
        wn.Obiekt("Komis: biuro — ściany wewnątrz (płyta laminowana biała)", root, PlytaWnetrza(), false, true);
        pod.Obiekt("Komis: biuro — podłoga (panele dąb szary)", root, BraxRegionMeble.Tex("Komis: biuro — panele podłogowe (dąb szary)", 1.2f, new Color(.80f, .77f, .72f), .55f, "wood_floor", "oak_wood_planks"), false, true);
        dach.Obiekt("Komis: biuro — dach (blacha)", root, BraxRegionMeble.Tex("Komis: kontener — rama (stal malowana grafit)", 1.0f, new Color(.30f, .31f, .32f), .55f, "painted_metal_shutter", "metal_plate"), false, true);
        suf.Obiekt("Komis: biuro — sufit (płyta biała)", root, BraxCityMaterials.Surface("Komis: biuro — sufit (płyta laminowana biała)", new Color(.93f, .93f, .91f), .5f, 0f), false, false);
        szk.Obiekt("Komis: biuro — szyba okna (przezroczysta)", root, BraxUliceKit.SzkloPrzezroczyste, false, false);
        ozd.Obiekt("Komis: biuro — stopień (krata pomostowa)", root, BraxRegionMeble.Tex("Komis: stopień (blacha ryflowana)", .6f, new Color(.62f, .63f, .64f), .5f, "metal_plate", "metal_plate_02"), false, true);
        // DRZWI stalowe na zawiasach (BraxCityDoor): skrzydło 4.5 cm w osi ściany, klamka po obu stronach; otwierają się same przy podejściu
        {
            var dr = new GameObject("Drzwi biura komisu").transform; dr.SetParent(root, false); dr.position = new Vector3(dx, yP, z1 - (e0 + w1) * .5f); dr.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var zaw = new GameObject("skrzydło (zawias)").transform; zaw.SetParent(dr, false); zaw.localPosition = new Vector3(-dW * .5f + .01f, 0f, 0f);
            var sk = new BraxRegionMeble.Bud(); var kl = new BraxRegionMeble.Bud();
            float lw = dW - .02f, lh = dH - .015f;
            sk.MinMax(new Vector3(0f, .01f, -.0225f), new Vector3(lw, lh, .0225f));
            foreach (float s in new[] { 1f, -1f })
            {
                kl.MinMax(new Vector3(lw - .1f, .98f * K, s * .0225f), new Vector3(lw - .06f, 1.18f * K, s * .045f));                 // szyld 2.25 cm
                kl.Cyl(new Vector3(lw - .08f, 1.05f * K, s * .045f), new Vector3(lw - .08f, 1.05f * K, s * .085f), .01f, 10);
                kl.Cyl(new Vector3(lw - .08f, 1.05f * K, s * .085f), new Vector3(lw - .22f, 1.05f * K, s * .085f), .011f, 10);   // klamka
            }
            var goS = sk.Obiekt("Drzwi biura komisu: skrzydło (stal malowana)", zaw, BraxRegionMeble.Tex("Komis: drzwi kontenera (stal malowana)", 1.0f, new Color(.55f, .57f, .58f), .5f, "painted_metal_shutter", "metal_plate"), false, true);
            if (goS) goS.transform.localPosition = Vector3.zero;
            var goK = kl.Obiekt("Drzwi biura komisu: klamki (stal nierdzewna)", zaw, BraxCityMaterials.Surface("Komis: klamka (stal nierdzewna)", new Color(.78f, .79f, .8f), .25f, .9f), false, false);
            // Bud.Obiekt stawia siatkę w układzie świata — przeliczenie: skrzydło budowane w lokalnym układzie zawiasu
            foreach (var go in new[] { goS, goK }) if (go) { go.transform.SetParent(zaw, false); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; }
            var kc = new GameObject("kolizja: skrzydło").AddComponent<BoxCollider>(); kc.transform.SetParent(zaw, false); kc.center = new Vector3(lw * .5f, lh * .5f, 0f); kc.size = new Vector3(lw, lh, .045f);
            var d = dr.gameObject.AddComponent<BraxCityDoor>(); d.id = "R1_KOMIS_BIURO"; d.label = "BIURO KOMISU"; d.leaves = new[] { zaw }; d.sides = new[] { 1f };
        }
        // KOLIZJE: ściany (front bez otworu drzwi), podłoga, dach
        void Kol(string n, Vector3 a, Vector3 b) => BraxRegionMeble.Kolizja(root, "Komis: kolizja biura — " + n, (a + b) * .5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z)));
        Kol("tył", new Vector3(x0, y, z0), new Vector3(x1, y + Hc, z0 + w1));
        Kol("bok zachodni", new Vector3(x0, y, z0), new Vector3(x0 + w1, y + Hc, z1));
        Kol("bok wschodni", new Vector3(x1 - w1, y, z0), new Vector3(x1, y + Hc, z1));
        Kol("front (zachód)", new Vector3(x0, y, z1 - w1), new Vector3(dx - dW * .5f, y + Hc, z1));
        Kol("front (wschód)", new Vector3(dx + dW * .5f, y, z1 - w1), new Vector3(x1, y + Hc, z1));
        Kol("nad drzwiami", new Vector3(dx - dW * .5f, yP + dH, z1 - w1), new Vector3(dx + dW * .5f, y + Hc, z1));
        Kol("podłoga", new Vector3(x0, y, z0), new Vector3(x1, yP, z1));
        Kol("dach", new Vector3(x0, yS - .05f, z0), new Vector3(x1, y + Hc, z1));
        // WYPOSAŻENIE: biurko komisanta (blat dąb 1.4 × 0.7 m × K na stelażu), krzesło za biurkiem, dwa krzesła klienta, laptop,
        // szafa na dokumenty przy ścianie zachodniej, szafka na kluczyki na ścianie tylnej, oprawa LED w suficie, roślina
        float k = K, bw = .7f * k, bd = .35f * k, bh = .75f * k, bx = x0 + 2.55f * k, bz = z0 + w1 + .8f * k + bd;
        var blat = new BraxRegionMeble.Bud(); var stel = new BraxRegionMeble.Bud();
        blat.MinMax(new Vector3(bx - bw, yP + bh - .035f, bz - bd), new Vector3(bx + bw, yP + bh, bz + bd));
        foreach (float sx in new[] { -bw + .03f, bw - .07f }) foreach (float sz in new[] { -bd + .03f, bd - .07f }) stel.MinMax(new Vector3(bx + sx, yP, bz + sz), new Vector3(bx + sx + .04f, yP + bh - .035f, bz + sz + .04f));
        stel.MinMax(new Vector3(bx - bw + .03f, yP + .3f * k, bz + bd - .07f), new Vector3(bx + bw - .03f, yP + bh - .035f, bz + bd - .05f));   // blenda od klienta
        blat.Obiekt("Komis: biurko — blat (okleina dąb)", root, BraxRegionMeble.Tex("Komis: biurko — okleina dąb", 1.6f, new Color(.86f, .74f, .6f), .45f, "oak_veneer_01", "teak_veneer"), false, true);
        stel.Obiekt("Komis: biurko — stelaż (stal malowana grafit)", root, BraxCityMaterials.Surface("Komis: biurko — stelaż (stal grafit)", new Color(.16f, .17f, .18f), .45f, .4f), false, true);
        BraxRegionMeble.Kolizja(root, "Komis: kolizja biurka", new Vector3(bx, yP + bh * .5f, bz), new Vector3(2f * bw, bh, 2f * bd));
        BraxRegionMeble.Model(root, "Komis: laptop komisanta", new Vector3(bx + .15f, yP + bh, bz - .05f), 180f, .24f, false, "classic_laptop");   // otwarty, ekran ~24 cm
        BraxUlicePierzeje.KrzesloTapicerowane("Komis: krzesło komisanta", root, new Vector3(bx, yP, bz - bd - .45f), 0f);
        foreach (float o in new[] { -.42f * k, .42f * k }) BraxUlicePierzeje.KrzesloTapicerowane("Komis: krzesło klienta", root, new Vector3(bx + o, yP, bz + bd + .45f), 180f);
        var sz2 = new BraxRegionMeble.Bud();
        sz2.MinMax(new Vector3(x0 + w1 + .02f, yP, z0 + w1 + .25f), new Vector3(x0 + w1 + .02f + .45f * k, yP + 1.9f * k, z0 + w1 + .25f + .9f * k));                  // szafa aktowa
        sz2.MinMax(new Vector3(x1 - 2.3f * k, yP + 1.35f * k, z0 + w1 + .02f), new Vector3(x1 - 2.3f * k + .55f, yP + 1.35f * k + .45f, z0 + w1 + .02f + .09f)); // szafka na kluczyki
        sz2.Obiekt("Komis: szafa na dokumenty i szafka na kluczyki (stal lakierowana szara)", root, BraxCityMaterials.Surface("Komis: szafy (stal lakierowana RAL 7035)", new Color(.56f, .58f, .58f), .45f, .3f), false, true);
        {   // uchwyty prętowe ze stali nierdzewnej (słupki od lica w głąb, pręt 3 cm przed licem): szafa aktowa — dwoje drzwi, szafka na kluczyki
            var uch = new BraxRegionMeble.Bud();
            void Uchwyt(Vector3 lico, Vector3 n, float hU)
            {
                foreach (float s in new[] { -1f, 1f }) uch.Cyl(lico - n * .005f + Vector3.up * (s * hU * .5f), lico + n * .028f + Vector3.up * (s * hU * .5f), .005f, 8);
                uch.Cyl(lico + n * .03f - Vector3.up * (hU * .5f + .01f), lico + n * .03f + Vector3.up * (hU * .5f + .01f), .006f, 10);
            }
            float fx = x0 + w1 + .02f + .45f * k, fz = z0 + w1 + .25f + .45f * k;
            Uchwyt(new Vector3(fx, yP + 1.0f * k, fz - .06f), Vector3.right, .12f); Uchwyt(new Vector3(fx, yP + 1.0f * k, fz + .06f), Vector3.right, .12f);
            Uchwyt(new Vector3(x1 - 2.3f * k + .55f - .06f, yP + 1.35f * k + .225f, z0 + w1 + .02f + .09f), Vector3.forward, .08f);
            uch.Obiekt("Komis: uchwyty szaf (stal nierdzewna)", root, BraxCityMaterials.Surface("Komis: klamka (stal nierdzewna)", new Color(.78f, .79f, .8f), .25f, .9f), false, false);
        }
        BraxRegionMeble.Kolizja(root, "Komis: kolizja szafy", new Vector3(x0 + w1 + .02f + .225f * k, yP + .95f * k, z0 + w1 + .25f + .45f * k), new Vector3(.45f * k, 1.9f * k, .9f * k));
        var led = new BraxRegionMeble.Bud(); led.MinMax(new Vector3(bx - .6f, yS - .075f - .04f, (z0 + z1) * .5f - .3f), new Vector3(bx + .6f, yS - .075f, (z0 + z1) * .5f + .3f));
        led.Obiekt("Komis: biuro — oprawa LED (panel 120 × 60)", root, BraxRegionMeble.Swiecacy("Komis: oprawa LED biura (biały neutralny)", new Color(1f, .97f, .92f), 1.3f), false, false);
        BraxRegion.Warm(root, "Komis: światło biura", new Vector3(bx, yS - .4f, (z0 + z1) * .5f), 1.1f, 6f);
        BraxRegionMeble.Model(root, "Komis: roślina w biurze", new Vector3(x1 - w1 - .45f, yP, z0 + w1 + .45f), 40f, 1.3f, true, "pachira_aquatica_01", "potted_plant_04");
        // grafika komisu właściciela w ramce na ścianie za biurkiem komisanta (bez paczki — nic)
        string ramka = BraxMiastoBanery.WRamce(root, new Vector3(bx, yP + 1.55f * k, z0 + w1), Vector3.forward, .75f);
        // AKCJA przy biurku (od strony klienta) i punkt świata w środku biura
        BraxMiastoA.Akcja(root, "komis_biuro", "ZAPYTAJ O AUTO", "Komisant: Skup, sprzedaż, zamiana. Wszystkie auta z dokumentami — wybierz, pogadamy o cenie.", new Vector3(bx, yP, bz + bd + .65f), 1.4f);
        world_point("R1_KOMIS_AUT", "komis samochodów używanych — biuro w kontenerze (w środku, przy biurku komisanta)", new Vector3(bx, yP, bz + bd + .65f));
        world_point("R1_KOMIS_BIURO_WEJSCIE", "komis — przed drzwiami biura", new Vector3(dx, y, z1 + 1.0f));
        return "biuro z wejściem " + L.ToString("F1") + " × " + B.ToString("F1") + " m (drzwi BraxCityDoor, okno, biurko, 3 krzesła, szafy, oprawa LED" + (ramka != null ? ", " + ramka : "") + ")";
    }
}

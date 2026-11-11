using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// MUZEUM — WYPOSAŻENIE I EKSPONATY Z KODU (11.11.2026, właściciel 23:09: „Muzeum Miasta drzwi zjebałeś, zobacz… Je wyposażenie.
// Zrób muzeum.”; 21:52: „Wszystko ma być dopieszczone”; 21:20: „żeby nie robić podwójnej roboty” — klasa do użycia w każdym
// rejonie). Wymiary rzeczywiste × K, siatki raz na wariant (BraxChlodnie.Wez) i stawiane jako obiekty z obrotem (lokalne +z = przód).
//  • KINKIET — latarnia ścienna z czarnej stali: płytka, ramię z zastrzałem, podstawa, 4 słupki narożne, szyby mleczne świecące
//    ciepłą bielą, daszek-ostrosłup z kulką (portal muzeum zamiast latarni na chodniku — zasada 19:40 „na chodnikach nic”).
//  • OBRAZY — 16 obrazów malowanych z kodu (atlas 2048², pędzel, splot płótna, werniks): morze ze statkiem (zachód, świt), port
//    nocą i o zmierzchu, góry nad jeziorem (lato, zima), kompozycje geometryczne, koła, martwe natury, sztorm z latarnią morską,
//    pole pod gwiaździstym niebem, łąka z makami. Ramy: klasyczne profilowane (orzech) albo cienkie czarne; lampka nad obrazem.
//  • PLANSZE działów (PORT BRAXHORN, NAWIGACJA, SZTUKA, TECHNIKA, DŹWIĘK I RUCH, PLAN WYSTAWY) — ilustracje z kodu w druku,
//    wszystkie napisy literami 3D Bebas na planszy (12.11: „napis zawsze w 3D”), rama z czarnej stali, na ścianę albo na nogach.
//  • ŻAGLOWIEC — model w dużej gablocie: szafka z orzecha, klosz szklany w ramie ze stali, kadłub z wręg (malowany: miedziane
//    dno, biała linia wodna, burty z klepek, czerwony pas z furtami), pokład dębowy, bukszpryt, 3 maszty ze stengami i marsami,
//    reje brasowane 25°, żagle rejowe wydęte, foki i bezan, olinowanie (wanty, sztagi), proporzec, podpórki na desce.
//  • ARMATA OKRĘTOWA na lawecie: lufa toczona (winograd, pierścienie, wylot z przewodem), czopy z okuciami, laweta schodkowa,
//    4 koła z obręczami na osiach, łoże i klin pod zamkiem, piramida 14 kul, podest z desek.
//  • KOŁO STEROWE na ścianę: wieniec i wieniec ozdobny, piasta z kołpakiem ze stali, 8 szprych z toczonymi uchwytami, oś z płytą.
//  • HEŁM NURKA (miedź): napierśnik z obręczą i 12 śrubami, pierścień szyjny (stal), hełm z 3 iluminatorami (szkło, kraty), zawór.
//  • GRAMOFON z tubą: postument, skrzynka, talerz z filcem i płytą (etykieta), ramię z membraną i igłą, korba, tuba lakierowana
//    bordo z wywiniętym brzegiem i przetłoczeniami, wspornik tuby.
//  • BICYKL (wysokie koło 52"): koła ze szprychami (48 / 20), obręcze, opony, rama łukowa, widelec, kierownica z rączkami,
//    siodełko skórzane na sprężynach, korby z pedałami, stopień, podest z klinami pod koła.
//  • GLOBUS na stojaku: kula z mapą świata (z kodu: kontynenty, siatka, równik), południk ze stali (oś 23.5°), obręcz horyzontu,
//    3 nogi toczone z rozpórkami, słupek pod południkiem.
// Bez złota i mosiądzu (zasada 10.11): okucia i drobny metal ze stali polerowanej lub czarnej, hełm z miedzi, tuba lakierowana.
internal static class BraxMuzeumKod
{
    static float K => BraxClaudeOsiedle.K;
    const string Katalog = BraxRegion.Out + "/Muzeum";
    static Material czern, stal, swiatlo, drewno, dab, zeliwo, miedz, plotno, lina, tuba, guma, skora, szklo, marmur, talerz;
    static Material _obrazy, _plansze, _globus; static readonly Material[] _kadluby = new Material[3];
    static readonly Color32[] Bl = { new Color32(255, 255, 255, 255) };
    public static int Postawione;

    static void Materialy()
    {
        if (czern) return;
        czern = BraxCityMaterials.Surface("muzeum: stal czarna malowana (latarnie, ramy)", new Color(.045f, .045f, .05f), .38f, .55f);
        stal = BraxCityMaterials.Surface("muzeum: stal nierdzewna polerowana", new Color(.86f, .87f, .88f), .12f, 1f);
        swiatlo = BraxMiastoA.Swieci("muzeum: szkło mleczne latarni (ciepła biel)", new Color(1f, .96f, .88f), new Color(1.5f, 1.38f, 1.18f));
        drewno = BraxUliceKit.Tex("muzeum: drewno ciemne (orzech)", 1.0f, new Color(.7f, .58f, .5f), new Color(.3f, .2f, .13f), .5f, "dark_wood", "oak_veneer_01");
        dab = BraxUliceKit.Tex("muzeum: boazeria dąb", 1.2f, null, new Color(.55f, .4f, .26f), .5f, "oak_veneer_01", "dark_wood");
        zeliwo = BraxCityMaterials.Surface("muzeum: żeliwo czarne (armata, kule)", new Color(.075f, .075f, .08f), .42f, .75f);
        miedz = BraxCityMaterials.Surface("muzeum: miedź (hełm nurka)", new Color(.70f, .42f, .30f), .32f, 1f);
        plotno = BraxUliceKit.Tex("muzeum: płótno żagli (len)", .5f, new Color(.97f, .94f, .86f), new Color(.92f, .89f, .8f), .85f, "rough_linen");
        lina = BraxUliceKit.Tex("muzeum: olinowanie (konopie)", .4f, new Color(.32f, .25f, .17f), new Color(.3f, .23f, .15f), .9f, "rough_linen");
        tuba = BraxCityMaterials.Surface("muzeum: lakier bordo (tuba gramofonu, proporzec)", new Color(.45f, .07f, .09f), .22f, .3f);
        guma = BraxUliceKit.Mat("muzeum: guma (opony bicykla, pedały)", new Color(.05f, .05f, .055f), .9f, 0f);
        skora = BraxUliceKit.Tex("muzeum: ławki skóra", .8f, new Color(.35f, .22f, .15f), new Color(.3f, .18f, .12f), .5f, "brown_leather", "leather_red_02");
        szklo = BraxUliceKit.SzkloPrzezroczyste;
        marmur = BraxUliceKit.Tex("muzeum: posadzka marmur", 1.6f, null, new Color(.9f, .88f, .85f), .2f, "marble_01");
        talerz = BraxCityMaterials.Surface("muzeum: gramofon — talerz, filc i płyta", Color.white, .3f, .1f);
    }
    static Material ObrazyM => _obrazy ? _obrazy : (_obrazy = AtlasObrazow());
    static Material PlanszeM => _plansze ? _plansze : (_plansze = AtlasPlansz());
    static Material GlobusM => _globus ? _globus : (_globus = AtlasGlobusa());
    static Material KadlubM(int w) => _kadluby[w] ? _kadluby[w] : (_kadluby[w] = AtlasKadluba(w));
    static Color32 C(float r, float g, float b) => new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);
    static Color K3(float r, float g, float b) => new Color(r, g, b);
    static Color Lp(Color a, Color b, float t) => Color.Lerp(a, b, t);

    static Material Atlas(string plik, string nazwa, int W, int H, Color[] px, float szorstkosc)
    {
        var tx = new Texture2D(W, H, TextureFormat.RGB24, true); tx.SetPixels(px); tx.Apply();
        Directory.CreateDirectory(Katalog); string path = Katalog + "/" + plik;
        File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = Mathf.Max(W, H); imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = BraxCityMaterials.Surface(nazwa, Color.white, szorstkosc, 0f);
        if (t) { m.SetTexture("_MainTex", t); EditorUtility.SetDirty(m); }
        return m;
    }

    // szum wartości (deterministyczny) i fbm — faktury obrazów z kodu
    static float Hs(int a, int b, int z) { unchecked { uint h = (uint)a * 374761393u + (uint)b * 668265263u + (uint)z * 2246822519u; h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffffu) / 65535f; } }
    static float Szum(float x, float y, int z)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(Hs(xi, yi, z), Hs(xi + 1, yi, z), fx), Mathf.Lerp(Hs(xi, yi + 1, z), Hs(xi + 1, yi + 1, z), fx), fy);
    }
    static float Fbm(float x, float y, int z) { float s = 0f, a = .5f; for (int o = 0; o < 4; o++) { s += a * Szum(x, y, z + o * 31); x = x * 2.03f + 1.7f; y = y * 2.03f + 3.1f; a *= .5f; } return s / .9375f; }
    static float SS(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
    static bool WTrojkacie(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by), d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy), d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f; return !(neg && pos);
    }

    // ---------------------------------------------------------------- bryły toczone wokół dowolnej osi
    /// <summary>Bryła toczona wokół osi os przez punkt p: profil (r, t wzdłuż osi, indeks koloru) jak BraxChlodnie.Toczony
    /// (normalne po prawej stronie kierunku profilu = na zewnątrz).</summary>
    static void TocOs(UlSiatka s, Vector3 p, Vector3 os, List<Vector3> pr, int seg, Color32[] kol = null)
    {
        var tmp = new UlSiatka(); BraxChlodnie.Toczony(tmp, Vector3.zero, pr, kol ?? Bl, seg);
        var q = Quaternion.FromToRotation(Vector3.up, os.normalized); int k0 = s.v.Count;
        for (int i = 0; i < tmp.v.Count; i++)
        {
            s.v.Add(p + q * tmp.v[i]); s.n.Add(q * tmp.n[i]);
            var t3 = q * new Vector3(tmp.t[i].x, tmp.t[i].y, tmp.t[i].z); s.t.Add(new Vector4(t3.x, t3.y, t3.z, tmp.t[i].w));
            s.uv.Add(tmp.uv[i]); s.c.Add(tmp.c[i]);
        }
        foreach (int ix in tmp.i) s.i.Add(k0 + ix);
    }
    /// <summary>Profil torusa (pierścień o przekroju elipsy ra × rt wokół promienia R, na wysokości t0) — obieg przeciwny do
    /// wskazówek: zewnątrz w górę, góra do środka, wewnątrz w dół, spód na zewnątrz.</summary>
    static List<Vector3> Torus(float R, float ra, float rt, float t0 = 0f, int n = 10)
    {
        var l = new List<Vector3>();
        for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; l.Add(new Vector3(R + ra * Mathf.Cos(a), t0 + rt * Mathf.Sin(a), 0f)); }
        return l;
    }
    /// <summary>Profil kuli (od bieguna dolnego do górnego), środek na wysokości t0 osi.</summary>
    static List<Vector3> KulaProfil(float r, float t0 = 0f, int n = 8)
    {
        var l = new List<Vector3>();
        for (int i = 0; i <= n; i++) { float f = i * Mathf.PI / n; l.Add(new Vector3(r * Mathf.Sin(f), t0 - r * Mathf.Cos(f), 0f)); }
        return l;
    }

    static Transform Postaw(Transform rodzic, string nazwa, string klucz, System.Action<BraxChlodnie.Partie> buduj, Vector3 p, Vector3 przod, bool cien = true)
    {
        Materialy();
        var w = BraxChlodnie.Wez(klucz, buduj);
        var g = BraxChlodnie.Korzen(rodzic, nazwa, p, przod);
        BraxChlodnie.Wstaw(g, nazwa + " (siatka)", w, Vector3.zero, cien);
        Postawione++;
        return g;
    }

    // ---------------------------------------------------------------- kinkiet (latarnia ścienna)
    /// <summary>Kinkiet: punkt = środek płytki na licu ściany, przod = od ściany. Wysokość latarni ~0.5 m × K (płytka w osi ramienia).</summary>
    public static Transform Kinkiet(Transform rodzic, string nazwa, Vector3 punktSciany, Vector3 przod)
        => Postaw(rodzic, "Kinkiet · " + nazwa, "muzeum_kinkiet", BudujKinkiet, punktSciany, przod);

    static void BudujKinkiet(BraxChlodnie.Partie p)
    {
        var c = p[czern]; var sw = p[swiatlo];
        c.Poducha(new Vector3(-.065f, -.13f, 0f), new Vector3(.065f, .13f, .022f), .008f, 0f, 3);          // płytka ścienna
        c.Rura(new Vector3(0f, 0f, .015f), new Vector3(0f, 0f, .31f), .014f, 10);                          // ramię
        c.Rura(new Vector3(0f, -.105f, .015f), new Vector3(0f, -.008f, .21f), .008f, 8);                   // zastrzał
        const float zc = .31f, a = .085f, y0 = .014f, y1 = .32f, sl = .016f;
        c.Poducha(new Vector3(-a - .012f, y0 - .028f, zc - a - .012f), new Vector3(a + .012f, y0, zc + a + .012f), .006f, 0f, 3);   // podstawa
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {   // słupki narożne
                float x0 = sx > 0 ? a - sl : -a, z0 = zc + (sz > 0 ? a - sl : -a);
                c.Box(new Vector3(x0, y0, z0), new Vector3(x0 + sl, y1, z0 + sl));
            }
        // szyby mleczne (świecą) w osi słupków — krawędzie schowane w słupkach, bez wspólnych płaszczyzn
        float xs = a - sl * .5f, zi0 = zc - a + sl, zi1 = zc + a - sl;
        sw.Quad(new Vector3(xs, y0, zi0), new Vector3(xs, y0, zi1), new Vector3(xs, y1, zi1), new Vector3(xs, y1, zi0), Vector3.right);
        sw.Quad(new Vector3(-xs, y0, zi0), new Vector3(-xs, y0, zi1), new Vector3(-xs, y1, zi1), new Vector3(-xs, y1, zi0), Vector3.left);
        sw.Quad(new Vector3(-a + sl, y0, zc + xs), new Vector3(a - sl, y0, zc + xs), new Vector3(a - sl, y1, zc + xs), new Vector3(-a + sl, y1, zc + xs), Vector3.forward);
        sw.Quad(new Vector3(-a + sl, y0, zc - xs), new Vector3(a - sl, y0, zc - xs), new Vector3(a - sl, y1, zc - xs), new Vector3(-a + sl, y1, zc - xs), Vector3.back);
        // daszek: płyta, ostrosłup, kulka
        float r0 = a + .016f, yd = y1 + .02f, ya = y1 + .115f;
        c.Poducha(new Vector3(-r0, y1, zc - r0), new Vector3(r0, yd, zc + r0), .005f, 0f, 3);
        var A = new Vector3(0f, ya, zc);
        var P = new[] { new Vector3(-r0, yd, zc - r0), new Vector3(r0, yd, zc - r0), new Vector3(r0, yd, zc + r0), new Vector3(-r0, yd, zc + r0) };
        for (int i = 0; i < 4; i++)
        {
            var b0 = P[i]; var b1 = P[(i + 1) % 4]; var srodekKr = (b0 + b1) * .5f - new Vector3(0f, yd, zc); srodekKr.y = 0f;
            var n = (srodekKr.normalized * (ya - yd) + Vector3.up * r0).normalized;
            c.Tri(b0, b1, A, n);
        }
        c.Rura(A - Vector3.up * .01f, A + Vector3.up * .035f, .007f, 8);
        BraxChlodnie.Toczony(c, A + Vector3.up * .05f, KulaProfil(.016f), Bl, 10);
    }

    // ================================================================ OBRAZY Z KODU (atlas 2048 × 2048, 16 pól po 512 px)
    struct ObrazDef { public int gen, wariant; public float aspekt; public bool stary; public ObrazDef(int g, int w, float a, bool s) { gen = g; wariant = w; aspekt = a; stary = s; } }
    static readonly ObrazDef[] Obrazy = {
        new ObrazDef(0, 0, 4f / 3f, true),   //  0 morze o zachodzie, żaglowiec
        new ObrazDef(1, 0, 4f / 3f, true),   //  1 port nocą
        new ObrazDef(2, 0, 4f / 3f, true),   //  2 góry nad jeziorem (lato)
        new ObrazDef(3, 0, 1f, false),       //  3 kompozycja geometryczna
        new ObrazDef(4, 0, .75f, false),     //  4 koła na jasnym tle
        new ObrazDef(5, 0, .75f, true),      //  5 martwa natura (wazon, kwiaty, owoce)
        new ObrazDef(6, 0, 4f / 3f, true),   //  6 sztorm i latarnia morska
        new ObrazDef(7, 0, 4f / 3f, true),   //  7 pole pod gwiaździstym niebem
        new ObrazDef(0, 1, 4f / 3f, true),   //  8 morze o świcie
        new ObrazDef(2, 1, 4f / 3f, true),   //  9 góry zimą
        new ObrazDef(3, 1, .75f, false),     // 10 kompozycja pionowa
        new ObrazDef(4, 1, 1f, false),       // 11 koła na ciemnym tle
        new ObrazDef(7, 1, 4f / 3f, true),   // 12 łąka z makami
        new ObrazDef(5, 1, .75f, true),      // 13 martwa natura II
        new ObrazDef(1, 1, 4f / 3f, true),   // 14 port o zmierzchu
        new ObrazDef(6, 1, 4f / 3f, true),   // 15 latarnia morska o zmierzchu
    };
    public static int LiczbaObrazow => Obrazy.Length;
    public static float Aspekt(int nr) => Obrazy[((nr % Obrazy.Length) + Obrazy.Length) % Obrazy.Length].aspekt;
    static Vector4[] uvObrazow;
    static float RamaObrazu(int nr) => Obrazy[nr % Obrazy.Length].stary ? .085f : .03f;
    /// <summary>Wysokość obrazu z ramą w metrach świata (szer = szerokość płótna, m rzeczywiste).</summary>
    public static float WysokoscObrazu(int nr, float szer) => (szer / Obrazy[nr % Obrazy.Length].aspekt + 2f * RamaObrazu(nr) - .024f) * K;

    // sylwetka żaglowca (x, y w jednostkach wysokości obrazu): 0 = poza, 1 = kadłub / maszt, 2 = żagiel
    static int Statek(float x, float y, float bx, float wy, float L)
    {
        float hb = wy - .04f * L, ht = wy + .13f * L;
        if (y > hb && y < ht) { float f = (y - hb) / (ht - hb), hl = L * .5f * (.8f + .2f * f); if (x > bx - hl && x < bx + hl * (1f + .15f * f)) return 1; }
        float xs0 = bx + L * .5f, xs1 = bx + L * .78f;
        if (x > xs0 && x < xs1 && Mathf.Abs(y - (ht + (x - xs0) / (xs1 - xs0) * L * .12f)) < .0025f) return 1;   // bukszpryt
        float[] mx = { -.27f, 0f, .25f }, mh = { .62f, .9f, .78f };
        for (int i = 0; i < 3; i++)
        {
            float xm = bx + mx[i] * L, top = ht + mh[i] * L;
            if (Mathf.Abs(x - xm) < .0028f && y > ht && y < top) return 1;
            if (i == 0)
            {   // bezan (żagiel skośny za masztem)
                float y0s = ht + .08f * L, y1s = ht + .5f * L;
                if (y > y0s && y < y1s && x < xm && x > xm - L * .22f * (1f - (y - y0s) / (y1s - y0s) * .45f)) return 2;
                continue;
            }
            for (int s = 0; s < 3; s++)
            {
                float ya = ht + (.12f + s * .27f) * mh[i] * L, yb = ya + .24f * mh[i] * L, hw = L * (.13f - .028f * s) * (mh[i] / .9f);
                if (y > ya && y < yb && Mathf.Abs(x - xm) < hw * (1f - .12f * (y - ya) / (yb - ya))) return 2;
            }
        }
        float xf = bx + .25f * L + .01f;
        return WTrojkacie(x, y, xf, ht + .7f * L, xs1, ht + L * .12f, xf, ht + .12f * L) ? 2 : 0;   // foki
    }

    static Color Morze(float x, float v, float asp, int w, int z)
    {
        const float hor = .42f;
        Color top = w == 0 ? K3(.2f, .17f, .38f) : K3(.42f, .58f, .82f), hz = w == 0 ? K3(1f, .58f, .3f) : K3(1f, .86f, .74f), sun = w == 0 ? K3(1f, .9f, .66f) : K3(1f, 1f, .93f);
        Color gl = w == 0 ? K3(.07f, .1f, .18f) : K3(.08f, .22f, .32f), ph = w == 0 ? K3(.5f, .32f, .3f) : K3(.55f, .62f, .68f);
        float sx = (w == 0 ? .7f : .28f) * asp, sy = hor + (w == 0 ? .09f : .16f); Color c;
        if (v > hor)
        {
            float t = (v - hor) / (1f - hor); c = Lp(hz, top, Mathf.Pow(t, .65f));
            c = Lp(c, Lp(hz, Color.white, .35f), SS(.55f, .78f, Fbm(x * 2.5f, v * 9f, z)) * (.35f + .4f * t));
            float ds = Vector2.Distance(new Vector2(x, v), new Vector2(sx, sy)); c = c + sun * (Mathf.Exp(-ds * ds * 45f) * .55f); if (ds < .042f) c = sun;
        }
        else
        {
            float t = (hor - v) / hor; c = Lp(ph, gl, Mathf.Pow(t, .55f)) * (.82f + .32f * Fbm(x * 28f, v * 140f, z + 3));
            float dx = Mathf.Abs(x - sx); c = Lp(c, sun, Mathf.Exp(-dx * dx * 70f / (1f + t * 3f)) * SS(.45f, .72f, Fbm(x * 18f, v * 190f, z + 5)) * .75f * (1f - t * .5f));
        }
        int st = Statek(x, v, (w == 0 ? .3f : .62f) * asp, hor + .004f, .32f);
        if (st == 1) c = K3(.06f, .05f, .06f); else if (st == 2) c = w == 0 ? K3(.16f, .12f, .14f) : K3(.92f, .88f, .8f);
        return c;
    }

    static Color Port(float x, float v, float asp, int w, int z)
    {
        const float hor = .36f; Color c;
        Color top = w == 0 ? K3(.02f, .03f, .09f) : K3(.16f, .1f, .24f), hz = w == 0 ? K3(.1f, .12f, .24f) : K3(.9f, .45f, .32f);
        Color dom = w == 0 ? K3(.04f, .04f, .07f) : K3(.13f, .08f, .13f), okno = w == 0 ? K3(1f, .76f, .42f) : K3(1f, .82f, .55f);
        int kol = Mathf.FloorToInt(x * 18f); float hD = .05f + .16f * Hs(kol, 7, z) + (Hs(kol, 9, z) > .88f ? .12f : 0f);
        if (v > hor)
        {
            float t = (v - hor) / (1f - hor); c = Lp(hz, top, Mathf.Pow(t, .5f));
            if (w == 0)
            {
                if (v > hor + .25f && Hs(Mathf.FloorToInt(x * 160f), Mathf.FloorToInt(v * 160f), z + 2) > .993f) c = K3(.95f, .95f, 1f);
                float dm = Vector2.Distance(new Vector2(x, v), new Vector2(.8f * asp, .8f)); c = c + K3(.5f, .5f, .45f) * Mathf.Exp(-dm * dm * 60f); if (dm < .04f) c = K3(.96f, .94f, .86f);
            }
            else c = Lp(c, Lp(hz, Color.white, .3f), SS(.6f, .8f, Fbm(x * 3f, v * 10f, z)) * .4f);
            if (v < hor + hD)
            {
                c = dom; int ox = Mathf.FloorToInt(x * 110f), oy = Mathf.FloorToInt((v - hor) * 70f); float fx = x * 110f - ox, fy = (v - hor) * 70f - oy;
                if (fx > .25f && fx < .75f && fy > .2f && fy < .75f && v < hor + hD - .012f && Hs(ox, oy, z + 4) > (w == 0 ? .55f : .7f)) c = okno;
            }
        }
        else
        {
            float t = (hor - v) / hor; c = Lp(Lp(hz, dom, .5f), top, Mathf.Pow(t, .5f)) * (.8f + .35f * Fbm(x * 30f, v * 160f, z + 3));
            c = Lp(c, okno, Mathf.Clamp01(Mathf.Pow(Szum(x * 110f, v * 7f, z + 6), 5f) * (1f - t) * 1.6f));   // odbicia świateł
            if (w == 0) { float dm = Mathf.Abs(x - .8f * asp); c = Lp(c, K3(.8f, .8f, .75f), Mathf.Exp(-dm * dm * 300f) * SS(.5f, .8f, Szum(x * 40f, v * 90f, z + 8)) * .6f); }
        }
        return c;
    }

    static Color GoryNiebo(float x, float v, int w, int z, float hor)
    {
        Color top = w == 0 ? K3(.36f, .56f, .86f) : K3(.55f, .64f, .78f), hz = w == 0 ? K3(.86f, .9f, .95f) : K3(.9f, .91f, .94f);
        Color c = Lp(hz, top, Mathf.Pow(Mathf.Clamp01((v - hor) / (1f - hor)), .8f));
        c = Lp(c, Color.white, SS(.58f, .8f, Fbm(x * 2f, v * 6f, z + 1)) * .7f);
        float gr = .5f + .2f * Fbm(x * 1.6f, .3f, z + 2) + .07f * Fbm(x * 7f, 1.3f, z + 3);
        if (v < gr)
        {
            float g2 = .5f + .2f * Fbm((x + .004f) * 1.6f, .3f, z + 2) + .07f * Fbm((x + .004f) * 7f, 1.3f, z + 3); bool lit = g2 < gr;
            Color skala = w == 0 ? K3(.36f, .38f, .46f) : K3(.5f, .53f, .6f); if (lit) skala = Lp(skala, Color.white, .25f);
            float snieg = w == 0 ? gr - .08f - .04f * Fbm(x * 12f, v * 12f, z + 4) : .4f;
            c = (v > snieg ? (lit ? K3(.97f, .97f, 1f) : K3(.78f, .82f, .9f)) : skala) * (.9f + .2f * Fbm(x * 30f, v * 30f, z + 5));
        }
        float las = hor + .07f + .05f * Fbm(x * 5f, 2.2f, z + 6) + .02f * Szum(x * 90f, 3f, z + 7);
        if (v < las)
        {
            c = (w == 0 ? K3(.07f, .18f, .1f) : K3(.16f, .2f, .22f)) * (.8f + .4f * Szum(x * 140f, v * 60f, z + 8));
            if (w == 1 && Szum(x * 200f, v * 200f, z + 9) > .7f) c = K3(.9f, .92f, .95f);
        }
        return c;
    }
    static Color Gory(float x, float v, float asp, int w, int z)
    {
        const float hor = .3f;
        if (v >= hor) return GoryNiebo(x, v, w, z, hor);
        float t = (hor - v) / hor, vv = hor + (hor - v) * 1.1f + (Szum(x * 60f, v * 300f, z + 11) - .5f) * .006f;   // odbicie w jeziorze
        return Lp(GoryNiebo(x, vv, w, z, hor), w == 0 ? K3(.12f, .22f, .32f) : K3(.3f, .36f, .42f), .3f + .3f * t) * (.9f + .15f * Fbm(x * 40f, v * 220f, z + 12));
    }

    static Color Kompozycja(float x, float v, float asp, int w, int z)
    {
        float u = x / asp; const float g = .009f;
        float[] xs = w == 0 ? new[] { 0f, .27f, .71f, 1f } : new[] { 0f, .38f, .62f, 1f };
        float[] ys = w == 0 ? new[] { 0f, .3f, .79f, 1f } : new[] { 0f, .18f, .55f, .86f, 1f };
        foreach (var a in xs) if (a > 0f && a < 1f && Mathf.Abs(x - a * asp) < g) return K3(.06f, .06f, .07f);
        foreach (var a in ys) if (a > 0f && a < 1f && Mathf.Abs(v - a) < g) return K3(.06f, .06f, .07f);
        int i = 0, j = 0; while (i < xs.Length - 2 && u > xs[i + 1]) i++; while (j < ys.Length - 2 && v > ys[j + 1]) j++;
        if (w == 0) { if (i == 0 && j == 2) return K3(.78f, .1f, .09f); if (i == 2 && j == 0) return K3(.1f, .2f, .55f); if (i == 0 && j == 0) return K3(.95f, .82f, .2f); }
        else { if (i == 1 && j == 3) return K3(.78f, .1f, .09f); if (i == 2 && j == 1) return K3(.1f, .2f, .55f); if (i == 0 && j == 0) return K3(.95f, .82f, .2f); }
        return Hs(i, j, z + w * 13) > .8f ? K3(.85f, .85f, .82f) : K3(.95f, .94f, .9f);
    }

    static Color Kola(float x, float v, float asp, int w, int z)
    {
        float u = x / asp; var P = new Vector2(x, v);
        Color c = w == 0 ? Lp(K3(.93f, .87f, .74f), K3(.72f, .64f, .5f), Vector2.Distance(new Vector2(u, v), new Vector2(.4f, .6f)) * 1.3f)
                         : Lp(K3(.12f, .12f, .17f), K3(.03f, .03f, .05f), Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 1.4f);
        Color[] pal = { K3(.82f, .15f, .12f), K3(.12f, .3f, .7f), K3(.95f, .78f, .2f), K3(.1f, .5f, .35f), K3(.5f, .2f, .55f), K3(.95f, .5f, .15f), K3(.05f, .05f, .06f), K3(.92f, .9f, .85f) };
        for (int i = 0; i < 9; i++)
        {
            float cx = (.12f + .76f * Hs(i, 1, z)) * asp, cy = .12f + .76f * Hs(i, 2, z), r = .04f + .16f * Hs(i, 3, z) * Hs(i, 4, z);
            float d = Vector2.Distance(P, new Vector2(cx, cy)); var k = pal[(i + w * 3) % pal.Length];
            if (d < r) c = Lp(k, Lp(k, Color.white, .35f), SS(r, 0f, d) * .4f);
            else if (d < r * 1.18f) c = Lp(c, Lp(k, Color.black, .5f), (1f - (d - r) / (r * .18f)) * .6f);
        }
        for (int i = 0; i < 5; i++)
        {
            var a = new Vector2(Hs(i, 5, z) * asp, Hs(i, 6, z)); var ab = new Vector2(Hs(i, 7, z) * asp, Hs(i, 8, z)) - a;
            float t = Mathf.Clamp01(Vector2.Dot(P - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            if (Vector2.Distance(P, a + ab * t) < .0035f) c = w == 0 ? K3(.06f, .06f, .07f) : K3(.9f, .88f, .82f);
        }
        return c;
    }

    static Color MartwaNatura(float x, float v, float asp, int w, int z)
    {
        float u = x / asp; const float st = .3f; var P = new Vector2(x, v);
        Color c = Lp(K3(.26f, .19f, .11f), K3(.06f, .045f, .03f), Vector2.Distance(new Vector2(u, v), new Vector2(.25f, .85f)) * 1.1f);
        if (v < st) { c = Lp(K3(.42f, .25f, .12f), K3(.2f, .11f, .05f), (st - v) / st * 1.2f) * (.85f + .25f * Fbm(x * 60f, v * 8f, z + 1)); if (v > st - .012f) c = K3(.55f, .36f, .2f); }
        float vx = .5f * asp, vy = .47f, rx = .14f, ry = .16f; Color kw = w == 0 ? K3(.16f, .3f, .62f) : K3(.6f, .3f, .2f);
        float ex = (x - vx) / rx, ey = (v - vy) / ry;
        if (ex * ex + ey * ey < 1f || (Mathf.Abs(x - vx) < .045f + .02f * SS(.62f, .7f, v) && v > vy && v < .71f))
        {   // wazon (światło z lewej, odblask)
            c = Lp(Lp(kw, Color.black, .45f), Lp(kw, Color.white, .35f), Mathf.Clamp01(.6f - (x - vx) / rx * .6f + (v - vy) * 1.2f));
            if (Vector2.Distance(P, new Vector2(vx - .05f, vy + .06f)) < .02f) c = Lp(c, Color.white, .6f);
        }
        for (int i = 0; i < 9; i++)
        {   // kwiaty i liście nad wazonem
            float fx = vx + (Hs(i, 1, z) - .5f) * .34f, fy = .78f + (Hs(i, 2, z) - .3f) * .2f, r = .035f + .03f * Hs(i, 3, z), d = Vector2.Distance(P, new Vector2(fx, fy));
            if (i % 3 == 0) { if (d < r * 1.5f && Szum(x * 80f, v * 80f, z + i) > .45f) c = K3(.12f, .32f, .12f); }
            else if (d < r) { Color kf = (i + w) % 3 == 0 ? K3(.85f, .12f, .15f) : (i + w) % 3 == 1 ? K3(.95f, .93f, .88f) : K3(.95f, .55f, .65f); c = Lp(kf, Lp(kf, Color.black, .35f), d / r * .8f); }
        }
        void Owoc(float ox, float oy, float r, Color k)
        {
            if (Vector2.Distance(P, new Vector2(ox, oy)) < r) c = Lp(Lp(k, Color.white, .3f), Lp(k, Color.black, .5f), Mathf.Clamp01((x - ox + r * .4f) / r * .5f - (v - oy - r * .4f) / r * .5f + .2f));
        }
        Owoc(.78f * asp, st + .05f, .055f, K3(.75f, .1f, .08f)); Owoc(.88f * asp, st + .04f, .045f, w == 0 ? K3(.7f, .12f, .08f) : K3(.45f, .6f, .15f));
        Owoc(.2f * asp, st + .035f, .04f, K3(.95f, .8f, .2f));
        for (int i = 0; i < 7; i++) Owoc((.28f + .025f * (i % 4)) * asp, st + .025f + .03f * (i / 4), .018f, K3(.35f, .12f, .4f));
        return c;
    }

    static Color Sztorm(float x, float v, float asp, int w, int z)
    {
        float u = x / asp; const float hor = .38f; Color c;
        Color n0 = w == 0 ? K3(.18f, .21f, .23f) : K3(.3f, .22f, .28f), n1 = w == 0 ? K3(.48f, .52f, .52f) : K3(.85f, .55f, .4f);
        if (v > hor)
        {
            c = Lp(n1, n0, (v - hor) / (1f - hor));
            c = Lp(c, Lp(n0, Color.black, .4f), SS(.45f, .75f, Fbm(x * 3f + v, v * 5f, z)) * .8f);
            c = Lp(c, Lp(n1, Color.white, .3f), SS(.65f, .85f, Fbm(x * 4f, v * 7f, z + 1)) * .4f);
        }
        else
        {
            c = Lp(K3(.2f, .3f, .3f), K3(.05f, .1f, .11f), (hor - v) / hor);
            float fala = Mathf.Sin(x * 22f + Fbm(x * 3f, v * 6f, z + 2) * 7f + v * 90f); c *= .75f + .35f * (fala * .5f + .5f);
            c = Lp(c, K3(.88f, .92f, .92f), SS(.82f, .97f, fala) * SS(.35f, .65f, Fbm(x * 12f, v * 40f, z + 3)) * .85f);   // piana
        }
        float sk = .22f + .2f * SS(.62f, .78f, u) + .05f * Fbm(x * 9f, 1f, z + 4);
        if (u > .6f && v < sk) c = Lp(K3(.13f, .12f, .11f), K3(.25f, .22f, .2f), Fbm(x * 25f, v * 25f, z + 5));   // skała
        float lx = .8f * asp, l0 = sk - .01f, l1 = .74f, wl = .034f - .01f * (v - l0) / (l1 - l0);
        if (v > l0 - .02f && v < l1 && Mathf.Abs(x - lx) < wl) c = Mathf.FloorToInt((v - l0) / .065f) % 2 == 0 ? K3(.9f, .88f, .84f) : K3(.68f, .1f, .08f);   // wieża w pasy
        if (v >= l1 && v < l1 + .05f && Mathf.Abs(x - lx) < .028f) c = K3(1f, .93f, .75f);                                                                  // laterna
        if (v >= l1 + .05f && v < l1 + .075f && Mathf.Abs(x - lx) < .034f - (v - l1 - .05f) * 1.3f) c = K3(.1f, .1f, .1f);                                   // hełm
        if (v > hor) { var dl = new Vector2(x - lx, v - (l1 + .025f)); if (dl.x < 0f) c = Lp(c, K3(1f, .95f, .8f), Mathf.Clamp01(.4f - Mathf.Abs(dl.y / dl.x) * 2.5f) * Mathf.Exp(dl.x * 1.5f)); }   // snop światła
        return c;
    }

    static Color Pole(float x, float v, float asp, int w, int z)
    {
        float u = x / asp, hor = .38f + .03f * Mathf.Sin(u * 5f); Color c;
        if (w == 0)
        {
            if (v > hor)
            {   // niebo w wirach i gwiazdy z poświatą
                float a = Fbm(x * 2.2f, v * 2.2f, z) * 9f, pas = Mathf.Sin((x * Mathf.Cos(a) + v * Mathf.Sin(a)) * 140f);
                c = Lp(K3(.12f, .2f, .5f), K3(.35f, .52f, .78f), Fbm(x * 3f, v * 3f, z + 1)) * (.85f + .2f * pas);
                for (int i = 0; i < 6; i++)
                {
                    var g = new Vector2((.1f + .8f * Hs(i, 1, z)) * asp, hor + .15f + .45f * Hs(i, 2, z)); float d = Vector2.Distance(new Vector2(x, v), g), r = .025f + .02f * Hs(i, 3, z);
                    c = Lp(c, K3(1f, .95f, .7f), Mathf.Clamp01(1.2f - d / r) + Mathf.Exp(-d * d / (r * r * 6f)) * .35f * (1f + .3f * Mathf.Sin(d * 300f)));
                }
            }
            else { float a = .6f + Fbm(x * 4f, v * 4f, z + 2), pas = Mathf.Sin((x * Mathf.Cos(a) + v * Mathf.Sin(a)) * 160f); c = Lp(K3(.78f, .62f, .22f), K3(.5f, .36f, .1f), (hor - v) / hor * .8f + .3f * Fbm(x * 8f, v * 8f, z + 3)) * (.85f + .2f * pas); }
            float h = (v - .08f) / .78f;   // cyprys
            if (h > 0f && h < 1f && Mathf.Abs(u - .14f) * asp < .055f * Mathf.Pow(1f - h, .7f) * (1f + .25f * Mathf.Sin(v * 60f + Fbm(x * 20f, v * 20f, z + 4) * 4f))) c = K3(.05f, .14f, .08f) * (.7f + .6f * Fbm(x * 50f, v * 50f, z + 5));
        }
        else
        {
            if (v > hor)
            {
                c = Lp(K3(.82f, .88f, .95f), K3(.4f, .6f, .9f), (v - hor) / (1f - hor)); c = Lp(c, Color.white, SS(.55f, .75f, Fbm(x * 2.5f, v * 6f, z + 1)) * .8f);
                if (v < hor + .04f + .03f * Szum(x * 30f, 1f, z + 2)) c = K3(.2f, .32f, .2f);
            }
            else
            {
                float t = (hor - v) / hor, s = 60f + 200f * t; c = Lp(K3(.45f, .6f, .25f), K3(.2f, .4f, .12f), t) * (.85f + .25f * Fbm(x * 30f, v * 80f, z + 3));
                if (Szum(x * s, v * s * 1.3f, z + 4) > .8f - .1f * t) c = K3(.85f, .1f, .08f);   // maki
            }
        }
        return c;
    }

    static Material AtlasObrazow()
    {
        const int S = 2048, CW = 512; var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(.2f, .2f, .2f);
        uvObrazow = new Vector4[Obrazy.Length];
        for (int n = 0; n < Obrazy.Length; n++)
        {
            var d = Obrazy[n]; int w = d.aspekt >= 1f ? CW : Mathf.RoundToInt(CW * d.aspekt), h = d.aspekt >= 1f ? Mathf.RoundToInt(CW / d.aspekt) : CW;
            int ox = (n % 4) * CW + (CW - w) / 2, oy = (n / 4) * CW + (CW - h) / 2, z = 101 + n * 7;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = (y + .5f) / h, xx = (x + .5f) / h, a = d.aspekt;   // x w jednostkach wysokości obrazu
                    Color c = d.gen switch
                    {
                        0 => Morze(xx, v, a, d.wariant, z), 1 => Port(xx, v, a, d.wariant, z), 2 => Gory(xx, v, a, d.wariant, z), 3 => Kompozycja(xx, v, a, d.wariant, z),
                        4 => Kola(xx, v, a, d.wariant, z), 5 => MartwaNatura(xx, v, a, d.wariant, z), 6 => Sztorm(xx, v, a, d.wariant, z), _ => Pole(xx, v, a, d.wariant, z),
                    };
                    // faktura: pociągnięcia pędzla, splot płótna; stare obrazy — werniks (ciepły) i winieta
                    c *= .9f + .2f * Fbm(xx * 70f + Fbm(xx * 5f, v * 5f, z + 50) * 4f, v * 22f, z + 60);
                    c *= ((x + y) & 1) == 0 ? 1.012f : .988f;
                    if (d.stary) { float r = Vector2.Distance(new Vector2(xx / a, v), new Vector2(.5f, .5f)); c = Lp(c, c * K3(1f, .9f, .72f), .28f) * (1f - .3f * r * r); }
                    px[(oy + y) * S + ox + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b));
                }
            uvObrazow[n] = new Vector4((float)ox / S, (float)oy / S, (float)w / S, (float)h / S);
        }
        return Atlas("obrazy_muzeum.png", "muzeum: obrazy (malowane z kodu)", S, S, px, .75f);
    }

    /// <summary>Obraz w ramie: srodek = środek płótna na licu ściany, przod = od ściany, nr = 0–15, szer = szerokość płótna (m
    /// rzeczywiste), lampka = oprawka nad ramą (świeci w dół).</summary>
    public static Transform Obraz(Transform rodzic, string nazwa, Vector3 srodek, Vector3 przod, int nr, float szer, bool lampka = true)
    {
        Materialy(); nr = ((nr % Obrazy.Length) + Obrazy.Length) % Obrazy.Length; var m = ObrazyM;
        return Postaw(rodzic, "Obraz · " + nazwa, "muzeum_obraz_" + nr + "_" + Mathf.RoundToInt(szer * 100f) + (lampka ? "_l" : ""), p => BudujObraz(p, nr, szer, lampka, m), srodek, przod);
    }
    static void BudujObraz(BraxChlodnie.Partie p, int nr, float a, bool lampka, Material atlas)
    {
        var d = Obrazy[nr]; float b = a / d.aspekt, f = RamaObrazu(nr), xa = a * .5f - .012f, ya = b * .5f - .012f;   // rama zakrywa 1.2 cm płótna
        float g0 = .006f, gF = d.stary ? .055f : .042f, zP = d.stary ? .03f : .018f;   // płótno ≥ 2.4 cm za licem ramy
        var rama = d.stary ? p[drewno] : p[czern];
        void Listwy(float w0, float w1, float z0, float z1, float r)
        {   // górna i dolna na całą szerokość, boczne między nimi (wewnętrzna krawędź w odległości w0 od płótna, zewnętrzna w1)
            rama.Poducha(new Vector3(-xa - w1, ya + w0, z0), new Vector3(xa + w1, ya + w1, z1), r, 0f, 3);
            rama.Poducha(new Vector3(-xa - w1, -ya - w1, z0), new Vector3(xa + w1, -ya - w0, z1), r, 0f, 3);
            rama.Poducha(new Vector3(-xa - w1, -ya - w0, z0), new Vector3(-xa - w0, ya + w0, z1), r, 0f, 3);
            rama.Poducha(new Vector3(xa + w0, -ya - w0, z0), new Vector3(xa + w1, ya + w0, z1), r, 0f, 3);
        }
        Listwy(0f, f, g0, gF, .008f);
        if (d.stary) { Listwy(f - .028f, f, .03f, gF + .023f, .01f); Listwy(0f, .016f, .03f, gF + .013f, .005f); }   // profil: wałek zewnętrzny, listewka przy płótnie
        var pl = p[atlas]; int k0 = pl.v.Count; var r4 = uvObrazow[nr];
        pl.Quad(new Vector3(a * .5f, -b * .5f, zP), new Vector3(-a * .5f, -b * .5f, zP), new Vector3(-a * .5f, b * .5f, zP), new Vector3(a * .5f, b * .5f, zP), Vector3.forward);
        pl.uv[k0] = new Vector2(r4.x, r4.y); pl.uv[k0 + 1] = new Vector2(r4.x + r4.z, r4.y); pl.uv[k0 + 2] = new Vector2(r4.x + r4.z, r4.y + r4.w); pl.uv[k0 + 3] = new Vector2(r4.x, r4.y + r4.w);   // lewo patrzącego = +x
        if (!lampka) return;
        // lampka obrazu: płytka, ramię, daszek otwarty od dołu ze świecącą listwą (2.2 cm pod daszkiem — bez wspólnej płaszczyzny)
        float yA = ya + f + .09f, yS = yA - .035f, zS = .21f, Ls = Mathf.Min(.3f, a * .3f); var c = p[czern];
        c.Poducha(new Vector3(-.03f, yA - .025f, 0f), new Vector3(.03f, yA + .025f, .022f), .005f, 0f, 3);   // płytka 2.2 cm (lico ≥ 2 cm od ściany)
        c.Rura(new Vector3(0f, yA, .008f), new Vector3(0f, yA - .02f, zS - .03f), .007f, 8);
        c.Box(new Vector3(-Ls, yS + .02f, zS - .035f), new Vector3(Ls, yS + .03f, zS + .035f));
        c.Box(new Vector3(-Ls, yS - .02f, zS + .028f), new Vector3(Ls, yS + .02f, zS + .035f));
        c.Box(new Vector3(-Ls, yS - .02f, zS - .035f), new Vector3(Ls, yS + .02f, zS - .028f));
        c.Box(new Vector3(-Ls, yS - .02f, zS - .028f), new Vector3(-Ls + .007f, yS + .02f, zS + .028f));
        c.Box(new Vector3(Ls - .007f, yS - .02f, zS - .028f), new Vector3(Ls, yS + .02f, zS + .028f));
        p[swiatlo].Quad(new Vector3(-Ls + .007f, yS - .002f, zS - .028f), new Vector3(Ls - .007f, yS - .002f, zS - .028f), new Vector3(Ls - .007f, yS - .002f, zS + .028f), new Vector3(-Ls + .007f, yS - .002f, zS + .028f), Vector3.down);
    }

    // ================================================================ PLANSZE DZIAŁÓW (atlas 1024 × 1024, 6 plansz 512 × 336)
    public static readonly string[] Plansze = { "PORT BRAXHORN", "NAWIGACJA", "SZTUKA", "TECHNIKA", "DŹWIĘK I RUCH", "PLAN WYSTAWY" };
    static readonly string[][] LiniePlansz = { new[] { "ŻEGLUGA", "RYBOŁÓWSTWO", "HANDEL MORSKI" }, new[] { "KOTWICA", "KOŁO STEROWE", "HEŁM NURKA" }, new[] { "MALARSTWO", "RZEŹBA", "CERAMIKA" },
                                               new[] { "MECHANIKA", "POMIAR CZASU", "OPTYKA" }, new[] { "GRAMOFON", "BICYKL", "GLOBUS" } };
    static Vector4[] uvPlansz;
    static Material AtlasPlansz()
    {
        const int S = 1024, CW = 512, CH = 336; var px = new Color[S * S];
        Color[] tla = { K3(.06f, .11f, .2f), K3(.05f, .09f, .17f), K3(.3f, .06f, .07f), K3(.05f, .2f, .15f), K3(.06f, .17f, .14f), K3(.11f, .11f, .12f) };
        uvPlansz = new Vector4[Plansze.Length];
        for (int n = 0; n < Plansze.Length; n++)
        {
            int ox = (n % 2) * CW, oy = (n / 2) * 341 + 2; Color tlo = tla[n], jasny = K3(.93f, .91f, .87f), akcent = Lp(tlo, Color.white, .45f);
            for (int y = 0; y < CH; y++) for (int x = 0; x < CW; x++) px[(oy + y) * S + ox + x] = Lp(Lp(tlo, Color.black, .25f), tlo, (float)y / CH) * (.97f + .03f * Szum(x * .3f, y * .3f, n));
            void Pas(int y0, int y1) { for (int y = y0; y < y1; y++) for (int x = 16; x < CW - 16; x++) px[(oy + y) * S + ox + x] = akcent; }
            Pas(CH - 74, CH - 72); Pas(30, 32);   // linie pod tytułem i u dołu (tytuł i wiersze to litery 3D na planszy, nie druk)
            if (n < 5) Ilustracja(px, S, ox + 16, oy + 44, 224, 200, n, jasny, akcent, tlo);
            else Plan(px, S, ox + 16, oy + 40, CW - 32, 206, jasny, akcent);
            uvPlansz[n] = new Vector4((float)ox / S, (float)oy / S, (float)CW / S, (float)CH / S);
        }
        return Atlas("plansze_muzeum.png", "muzeum: plansze działów (z kodu)", S, S, px, .6f);
    }
    static void Ilustracja(Color[] px, int S, int bx, int by, int w, int h, int n, Color jasny, Color akcent, Color tlo)
    {
        float asp = (float)w / h;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / h, v = (float)y / h; Color? k = null;
                var d = new Vector2(u - .5f * asp, v - .5f); float r = d.magnitude, a = Mathf.Atan2(d.y, d.x);
                if (n == 0)
                {   // żaglowiec na falach
                    int s = Statek(u, v, .5f * asp, .34f, .55f); if (s == 1) k = jasny; else if (s == 2) k = Lp(jasny, tlo, .3f);
                    if (k == null && v < .3f && Mathf.Abs(Mathf.Repeat(v + .012f * Mathf.Sin(u * 25f), .07f) - .035f) < .006f) k = akcent;
                }
                else if (n == 1)
                {   // róża wiatrów: 4 długie i 4 krótkie ramiona, dwa okręgi
                    float ramie = .42f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 18f) + .26f * Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 2f)), 18f);
                    if (r < Mathf.Max(ramie, .05f)) k = Mathf.Repeat(a + Mathf.PI * 2f, Mathf.PI * .5f) < Mathf.PI * .25f ? jasny : akcent;
                    if (Mathf.Abs(r - .3f) < .006f || Mathf.Abs(r - .44f) < .004f) k = jasny;
                }
                else if (n == 2)
                {   // waza w ramie
                    float prof = .1f + .12f * Mathf.Sin(Mathf.Clamp01((v - .12f) / .62f) * Mathf.PI) - .05f * SS(.6f, .78f, v), fx = Mathf.Abs(d.x), fy = Mathf.Abs(v - .47f);
                    if ((v > .1f && v < .8f && fx < prof) || (v > .8f && v < .84f && fx < .1f)) k = jasny;
                    if ((Mathf.Abs(fx - .44f) < .012f && fy < .45f) || (Mathf.Abs(fy - .45f) < .012f && fx < .44f)) k = akcent;
                }
                else if (n == 3)
                {   // koło zębate z okienkami
                    float zab = Mathf.Repeat(a * 12f / (Mathf.PI * 2f), 1f) < .5f ? .4f : .33f;
                    if (r < zab && r > .13f) k = (r > .16f && r < .27f && Mathf.Repeat(a * 5f / (Mathf.PI * 2f), 1f) < .55f) ? Lp(tlo, Color.black, .2f) : jasny;
                    if (r < .08f) k = akcent;
                }
                else
                {   // tuba i fale dźwięku
                    var t = new Vector2(u - .18f * asp, v - .5f); float rt = t.magnitude, at = Mathf.Atan2(t.y, t.x);
                    if (Mathf.Abs(at) < .55f && Mathf.Repeat(rt, .1f) < .018f && rt > .2f && rt < .95f) k = jasny;
                    if (t.x > -.14f && t.x < .12f && Mathf.Abs(t.y) < .02f + Mathf.Max(0f, t.x + .14f) * .55f) k = akcent;
                }
                if (k.HasValue) px[(by + y) * S + bx + x] = k.Value;
            }
    }
    static void Plan(Color[] px, int S, int bx, int by, int w, int h, Color jasny, Color akcent)
    {
        // obrys sali i trzy działy w widoku od wejścia (wejście u dołu): TECHNIKA po lewej, SZTUKA, PORT po prawej
        int x0 = 30, x1 = w - 30, y0 = 34, y1 = h - 6, wD = (x1 - x0) / 3, cx = (x0 + x1) / 2;
        Color[] dz = { K3(.06f, .2f, .15f), K3(.3f, .06f, .07f), K3(.06f, .11f, .2f) };   // nazwy działów — litery 3D (Plansza)
        void P(int x, int y, Color k) { if (x >= 0 && x < w && y >= 0 && y < h) px[(by + y) * S + bx + x] = k; }
        for (int i = 0; i < 3; i++)
        {
            for (int y = y0; y < y1; y++) for (int x = x0 + i * wD; x < x0 + (i + 1) * wD; x++) P(x, y, Lp(dz[i], Color.white, .12f));
        }
        for (int y = y0 - 3; y < y1 + 3; y++) for (int x = x0 - 3; x < x1 + 3; x++) if (x < x0 || x >= x1 || y < y0 || y >= y1) P(x, y, jasny);
        for (int y = y0 - 3; y < y0; y++) for (int x = cx - 14; x < cx + 14; x++) P(x, y, Lp(dz[1], Color.white, .12f));   // przerwa — drzwi
        for (int y = -6; y <= 6; y++) for (int x = -6; x <= 6; x++) if (x * x + y * y <= 36) P(cx + 34 + x, y0 + 14 + y, K3(.85f, .15f, .12f));   // TU JESTEŚ
    }

    /// <summary>Plansza działu: punkt = środek na licu ściany (albo na posadzce, gdy wolnostojąca), przod = do oglądających,
    /// nr = 0–5 (Plansze). Tablica 1.5 × 0.98 m × K w ramie z czarnej stali; wolnostojąca na dwóch nogach (środek 1.35 m × K).</summary>
    public static Transform Plansza(Transform rodzic, string nazwa, Vector3 punkt, Vector3 przod, int nr, bool wolnostojaca = false)
    {
        Materialy(); nr = Mathf.Clamp(nr, 0, Plansze.Length - 1); var m = PlanszeM;
        var g = Postaw(rodzic, "Plansza " + Plansze[nr] + " · " + nazwa, "muzeum_plansza_" + nr + (wolnostojaca ? "_noga" : "_sciana"), p => BudujPlansze(p, nr, wolnostojaca, m), punkt, przod);
        if (wolnostojaca) BraxChlodnie.Kolizja(g, "plansza", new Vector3(-.78f, 0f, -.22f), new Vector3(.78f, 1.86f, .12f));
        // napisy planszy LITERAMI 3D (12.11 00:05: „napis zawsze w 3D… nie napis zwykły”; zasada 10.11 — bez tekstu w teksturze):
        // tytuł, trzy wiersze po prawej, na planie nazwy działów i WEJŚCIE; położenie w pikselach planszy 512 × 336 (u od lewej patrzącego)
        const float A = .75f, B = .492f; float yc = wolnostojaca ? 1.35f : 0f;
        Vector3 Pkt(float u, float v) => g.position + g.rotation * (new Vector3(A - u * 2f * A, yc - B + v * 2f * B, .018f) * K);
        var h = new GameObject("napisy 3D planszy").transform; h.SetParent(g, false); h.position = g.position; h.rotation = Quaternion.identity;   // Napis buduje w osiach świata
        void N(string t, float uPx, float vPx, float capPx, float maxPx, float gl)
        {
            float cap = capPx / 336f * 2f * B * K, maxW = maxPx / 512f * 2f * A * K, w = BraxLiteryBebas.Szerokosc(t, cap); if (w > maxW) cap *= maxW / w;
            BraxRegionMeble.Napis(h, t, Pkt(uPx / 512f, vPx / 336f), g.forward, cap / .72f, new Color(.9f, .9f, .88f), gl * K);
        }
        // grubość liter ≥ 1.6 cm (× K ≥ 2 cm lica od druku — zero migotania), lico wewnątrz głębokości ramy (4 cm)
        N(Plansze[nr], 256f, 298f, 40f, 470f, .018f);
        if (nr < 5) for (int i = 0; i < 3; i++) N(LiniePlansz[nr][i], 384f, 212f - i * 52f, 24f, 220f, .016f);
        else { float[] ux = { 116f, 256f, 396f }; string[] nz = { "TECHNIKA", "SZTUKA", "PORT" }; for (int i = 0; i < 3; i++) N(nz[i], ux[i], 159f, 22f, 120f, .016f); N("WEJŚCIE", 256f, 56f, 16f, 160f, .016f); }
        return g;
    }
    static void BudujPlansze(BraxChlodnie.Partie p, int nr, bool noga, Material atlas)
    {
        const float A = .75f, B = .492f, f = .03f; float yc = noga ? 1.35f : 0f, z0 = noga ? -.035f : .006f;
        var c = p[czern];
        c.Poducha(new Vector3(-A - f, yc + B - .008f, z0), new Vector3(A + f, yc + B + f, .04f), .006f, 0f, 3);
        c.Poducha(new Vector3(-A - f, yc - B - f, z0), new Vector3(A + f, yc - B + .008f, .04f), .006f, 0f, 3);
        c.Poducha(new Vector3(-A - f, yc - B + .008f, z0), new Vector3(-A + .008f, yc + B - .008f, .04f), .006f, 0f, 3);
        c.Poducha(new Vector3(A - .008f, yc - B + .008f, z0), new Vector3(A + f, yc + B - .008f, .04f), .006f, 0f, 3);
        var pl = p[atlas]; int k0 = pl.v.Count; var r4 = uvPlansz[nr];
        pl.Quad(new Vector3(A, yc - B, .018f), new Vector3(-A, yc - B, .018f), new Vector3(-A, yc + B, .018f), new Vector3(A, yc + B, .018f), Vector3.forward);   // druk ≥ 2 cm przed ścianą i za licem ramy
        pl.uv[k0] = new Vector2(r4.x, r4.y); pl.uv[k0 + 1] = new Vector2(r4.x + r4.z, r4.y); pl.uv[k0 + 2] = new Vector2(r4.x + r4.z, r4.y + r4.w); pl.uv[k0 + 3] = new Vector2(r4.x, r4.y + r4.w);
        if (!noga) return;
        c.Box(new Vector3(-A, yc - B, -.028f), new Vector3(A, yc + B, -.012f));                                    // plecy tablicy
        foreach (float sx in new[] { -.55f, .55f })
        {
            p[stal].Rura(new Vector3(sx, .02f, -.055f), new Vector3(sx, yc - B + .05f, -.055f), .02f, 12);       // noga
            c.Poducha(new Vector3(sx - .07f, 0f, -.2f), new Vector3(sx + .07f, .025f, .1f), .008f, 0f, 3);           // stopa
        }
    }

    // ================================================================ MAPA ŚWIATA (globus): kontynenty z wielokątów (dł., szer.)
    static readonly float[][] Lady = {
        new float[] { -168,66, -162,70, -156,71, -140,70, -128,70, -115,68, -100,68, -95,72, -85,70, -80,64, -95,60, -93,56, -82,55, -78,60, -70,61, -64,58, -60,55, -56,52, -60,47, -66,45, -70,42, -74,40, -76,35, -80,32, -81,26, -82,25, -83,29, -88,30, -94,29, -97,27, -97,22, -95,19, -91,19, -87,21, -87,16, -84,15, -83,10, -78,8, -80,7, -84,9, -87,13, -92,15, -96,16, -105,20, -106,23, -110,23, -112,29, -115,31, -117,33, -121,35, -124,40, -124,46, -125,50, -130,54, -135,58, -140,60, -148,61, -152,59, -158,57, -163,55, -166,60, -165,63 },
        new float[] { -120,72, -95,74, -80,74, -70,78, -80,82, -100,80, -120,77 },
        new float[] { -80,63, -65,62, -62,67, -72,71, -80,73, -90,71, -85,68 },
        new float[] { -73,78, -60,82, -35,83, -20,81, -18,76, -22,70, -30,68, -40,65, -44,60, -50,62, -54,67, -58,75 },
        new float[] { -24,64, -22,66.5f, -14,66.3f, -13,65, -18,63.5f },
        new float[] { -78,8, -72,12, -62,11, -55,6, -50,2, -50,-1, -44,-2, -35,-6, -37,-12, -39,-18, -41,-22, -48,-26, -49,-29, -53,-34, -58,-36, -57,-39, -63,-41, -65,-45, -67,-47, -69,-51, -68,-55, -73,-53, -75,-47, -74,-42, -73,-37, -71,-30, -70,-20, -75,-15, -78,-10, -81,-5, -80,-1, -78,3, -77,7 },
        new float[] { -9,37, -9,43, -2,43.5f, -1,46, -4.5f,48.3f, 0,49.5f, 4,51.5f, 8,54, 8.5f,57, 10.5f,57.5f, 10.5f,59, 5,58.5f, 5,62, 10,64, 15,68, 20,70, 28,71, 33,69.5f, 40,67, 44,68, 55,68.5f, 60,70, 68,73, 80,73, 90,76, 105,78, 113,74, 130,71, 142,72, 158,70, 170,70, 180,68, 180,65, 178,62, 170,60, 163,58, 162,55, 156,51, 156,57, 150,59, 142,59, 137,55, 141,52, 140,48, 135,43, 130,42, 129,35, 126,37, 125,40, 121,40, 122,37, 119,35, 122,30, 120,26, 117,23, 111,21, 108,22, 106,18, 109,12, 106,9, 104,10, 100,13, 101,7, 104,1.5f, 102,2, 98,8, 98,16, 94,17, 92,22, 90,22, 86,20, 80,15, 80,10, 77,8, 74,15, 72,21, 67,24, 62,25, 57,25.5f, 56,24, 59,22, 57,19, 52,16, 45,13, 43,13, 40,20, 37,25, 34,28, 33,30, 35,32, 36,36, 30,36.5f, 27,37, 26,40, 23,40.5f, 23,38, 21,38, 20,40, 19,42, 16,43, 13,45.5f, 12.5f,44, 14,42, 16,41.5f, 18.5f,40, 16,38, 15.5f,40, 11,42.5f, 9,44.3f, 6,43, 3,43.3f, 3,42, 0,40, -1,37.5f, -5.5f,36 },
        new float[] { -17,21, -17,15, -15,11, -12,7.5f, -8,4.5f, -3,5, 5,6, 9,4, 10,-1, 12,-5, 13,-12, 12,-17, 15,-27, 18,-34.5f, 20,-35, 26,-34, 30,-31, 33,-26, 35,-24, 35,-19, 40,-15, 40,-10, 39,-6, 40,-2, 42,1, 46,2, 51,10.5f, 51,12, 44,11, 43,12.5f, 38,18, 35,24, 33,29, 32,31, 29,31, 25,32, 20,31, 20,33, 15,32, 11,33.5f, 10,37, 8,37, 3,36.8f, -1,35.5f, -6,35.8f, -10,31, -13,27, -16,24 },
        new float[] { 43.5f,-25, 47,-25, 50,-15.5f, 49,-12, 46,-16, 44,-20 },
        new float[] { -5.5f,50, 1.5f,51, 1.5f,53, -.5f,54.5f, -2,56, -2,57.7f, -3.5f,58.6f, -5.2f,58.5f, -6.2f,56.5f, -4.8f,55, -3.2f,54.3f, -4.8f,53.3f, -4.8f,52, -3.2f,51.5f },
        new float[] { -10,51.5f, -6,52, -6,54.5f, -8,55.3f, -10,54 },
        new float[] { 130,31, 132,34, 135,34, 140,35.5f, 141,38, 142,41, 141.5f,45.5f, 145,44, 143,42, 140,41.5f, 139,38, 137,37, 133,35.5f, 130,33.5f },
        new float[] { 120,18, 122,18, 126,7, 122,7, 120,12 },
        new float[] { 109,1.5f, 111,-3, 116,-4, 119,1, 117,7, 113,3 },
        new float[] { 95,5.5f, 98,4, 106,-6, 104,-5.8f, 100,-1, 96.5f,2 },
        new float[] { 105,-6, 114.5f,-7.8f, 114.5f,-8.7f, 106,-7.5f },
        new float[] { 131,-1, 138,-1.5f, 146,-6, 150.5f,-10.5f, 146,-9, 141,-9, 136,-4.5f, 132,-3 },
        new float[] { 113.5f,-22, 114,-26, 115,-34, 118,-35, 123,-34, 129,-32, 134,-32.5f, 138,-35, 140,-38, 146,-39, 150,-37.5f, 153,-31, 153,-25, 149,-21, 145.5f,-15, 142.5f,-10.7f, 141.5f,-13, 141,-17, 136,-15, 136.7f,-12, 132,-11.3f, 129.5f,-15, 126,-14, 122,-18, 119,-20 },
        new float[] { 172.7f,-34.5f, 178.5f,-37.7f, 176.8f,-40, 174.8f,-41.3f, 173,-42, 170,-45, 166.5f,-46, 169,-46.7f, 174,-41.5f, 172.7f,-40.5f },
    };
    static readonly float[][] Morza = {   // wody śródlądowe wycinane z lądu: Bałtyk, Morze Czarne, Kaspijskie, Zatoka Perska
        new float[] { 10,54, 13,54.5f, 20,54.5f, 21,57, 24,57.5f, 24,59.5f, 30,60, 23,60.3f, 21,61, 22,65.5f, 25,65.8f, 21,63, 17,61, 19,60, 18,58.5f, 16,56, 13,55.5f, 11,56 },
        new float[] { 28,41.5f, 28,43.5f, 30,45, 33,46, 36,45.3f, 39,47, 41.5f,42, 36,41.5f, 32,41.5f },
        new float[] { 47,45.5f, 51,47, 54,45, 53,41, 54,37, 50,37, 49,40, 47,43 },
        new float[] { 48,30, 50,30, 56,27, 56.3f,25.5f, 54,24, 51,24.5f, 50.5f,26, 49,27 },
    };
    static bool WWielokacie(float[] p, Vector4 bb, float x, float y)
    {
        if (x < bb.x || x > bb.z || y < bb.y || y > bb.w) return false;
        bool w = false; int n = p.Length / 2;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = p[2 * i], yi = p[2 * i + 1], xj = p[2 * j], yj = p[2 * j + 1];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) w = !w;
        }
        return w;
    }
    static Vector4 Obrys(float[] p) { var b = new Vector4(999f, 999f, -999f, -999f); for (int i = 0; i < p.Length; i += 2) { b.x = Mathf.Min(b.x, p[i]); b.y = Mathf.Min(b.y, p[i + 1]); b.z = Mathf.Max(b.z, p[i]); b.w = Mathf.Max(b.w, p[i + 1]); } return b; }
    static Material AtlasGlobusa()
    {
        const int W = 1024, H = 512; var lad = new bool[W * H]; var px = new Color[W * H];
        var bl = Lady.Select(Obrys).ToArray(); var bm = Morza.Select(Obrys).ToArray();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float lon = -180f + 360f * (x + .5f) / W, lat = -90f + 180f * (y + .5f) / H;
                bool l = lat < -71f + 3f * Mathf.Sin(lon * .05f);
                for (int i = 0; i < Lady.Length && !l; i++) l = WWielokacie(Lady[i], bl[i], lon, lat);
                for (int i = 0; i < Morza.Length && l; i++) if (WWielokacie(Morza[i], bm[i], lon, lat)) l = false;
                lad[y * W + x] = l;
            }
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float lon = -180f + 360f * (x + .5f) / W, lat = -90f + 180f * (y + .5f) / H; bool l = lad[y * W + x];
                bool brzeg = l && ((x > 0 && !lad[y * W + x - 1]) || (x < W - 1 && !lad[y * W + x + 1]) || (y > 0 && !lad[(y - 1) * W + x]) || (y < H - 1 && !lad[(y + 1) * W + x]));
                Color c = l ? Lp(K3(.86f, .8f, .62f), K3(.68f, .62f, .42f), Fbm(lon * .05f, lat * .05f, 3)) : Lp(K3(.34f, .52f, .62f), K3(.22f, .36f, .5f), Mathf.Abs(lat) / 90f);
                if (brzeg) c = K3(.32f, .26f, .18f);
                if (Mathf.Repeat(lon + 180f, 30f) < .45f || Mathf.Repeat(lat + 90f, 30f) < .45f) c = Lp(c, K3(.12f, .14f, .16f), .4f);   // siatka co 30°
                if (Mathf.Abs(lat) < .45f) c = Lp(c, K3(.6f, .1f, .08f), .65f);                                                            // równik
                px[y * W + x] = c * (.94f + .08f * Szum(x * .4f, y * .4f, 5));
            }
        return Atlas("globus_mapa.png", "muzeum: globus (mapa świata z kodu)", W, H, px, .45f);
    }

    // ================================================================ KADŁUB ŻAGLOWCA — malowanie (u wzdłuż kadłuba, v = |t| przekroju)
    static Material AtlasKadluba(int w)
    {   // 0: burty z dębu, czerwony pas z furtami | 1: czarne burty, biały pas z furtami (bryg) | 2: granatowe burty, biały pas (kuter)
        Color burta = w == 0 ? K3(.24f, .15f, .09f) : w == 1 ? K3(.05f, .05f, .06f) : K3(.07f, .12f, .26f), pasK = w == 0 ? K3(.62f, .12f, .09f) : K3(.9f, .88f, .82f);
        const int W = 512, H = 256; var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float s = (x + .5f) / W, t = (y + .5f) / H; Color c;
                if (t < .62f) { c = K3(.56f, .33f, .23f) * (.9f + .15f * Szum(s * 60f, t * 20f, 1)); if (Mathf.Repeat(s * 40f, 1f) < .05f || Mathf.Repeat(t * 14f, 1f) < .06f) c *= .8f; }   // miedziane dno (blachy)
                else if (t < .68f) c = K3(.92f, .9f, .85f);                                                                                                        // linia wodna
                else if (t < .83f || t > .935f) { c = burta * (.85f + .25f * Szum(s * 3f, t * 60f, 2)); if (Mathf.Repeat(t * 42f, 1f) < .1f) c *= .7f; }   // burty z klepek
                else { c = pasK; float fx = Mathf.Repeat(s * (w == 1 ? 10f : 13f), 1f); if (w < 2 && s > .1f && s < .9f && fx > .38f && fx < .62f && t > .86f && t < .91f) c = K3(.04f, .03f, .03f); }   // pas (z furtami)
                if (t > .975f) c = K3(.05f, .04f, .04f);                                                                                                           // listwa nadburcia
                px[y * W + x] = c;
            }
        return Atlas("kadlub_zaglowca_" + w + ".png", "muzeum: model żaglowca — kadłub " + w + " (malowany)", W, H, px, .45f);
    }

    // ================================================================ EKSPONATY 3D
    /// <summary>Koło sterowe na ścianie: punkt = środek na licu ściany, przod = od ściany. Średnica z uchwytami ~1.5 m × K.</summary>
    public static Transform KoloSterowe(Transform rodzic, string nazwa, Vector3 punktSciany, Vector3 przod)
    {
        var g = Postaw(rodzic, "Koło sterowe · " + nazwa, "muzeum_kolo_sterowe", BudujKoloSterowe, punktSciany, przod);
        BraxChlodnie.Kolizja(g, "koło sterowe", new Vector3(-.76f, -.76f, 0f), new Vector3(.76f, .76f, .3f));
        return g;
    }
    static void BudujKoloSterowe(BraxChlodnie.Partie p)
    {
        var dw = p[dab]; var st = p[stal]; const float zc = .2f; var c0 = new Vector3(0f, 0f, zc);
        st.Poducha(new Vector3(-.12f, -.12f, 0f), new Vector3(.12f, .12f, .02f), .01f, 0f, 3);                        // płyta ścienna
        st.Rura(new Vector3(0f, 0f, .015f), new Vector3(0f, 0f, zc - .05f), .03f, 14);                                // oś
        TocOs(dw, c0, Vector3.forward, Torus(.5f, .03f, .024f, 0f, 12), 48);                                           // wieniec
        TocOs(dw, c0, Vector3.forward, Torus(.42f, .012f, .014f, 0f, 8), 48);                                          // wieniec ozdobny
        TocOs(dw, c0 - Vector3.forward * .06f, Vector3.forward, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.1f, 0f, 0), new Vector3(.115f, .015f, 0), new Vector3(.115f, .105f, 0), new Vector3(.1f, .12f, 0), new Vector3(.065f, .12f, 0) }, 24);   // piasta
        TocOs(st, c0 - Vector3.forward * .06f, Vector3.forward, new List<Vector3> { new Vector3(.066f, .118f, 0), new Vector3(.07f, .13f, 0), new Vector3(.05f, .145f, 0), new Vector3(0f, .15f, 0) }, 20);   // kołpak
        for (int i = 0; i < 8; i++)
        {
            float k = i * Mathf.PI / 4f + Mathf.PI / 8f; var d = new Vector3(Mathf.Cos(k), Mathf.Sin(k), 0f);
            dw.Rura(c0 + d * .1f, c0 + d * .6f, .018f, 10);
            TocOs(dw, c0 + d * .6f, d, new List<Vector3> { new Vector3(.019f, 0f, 0), new Vector3(.026f, .03f, 0), new Vector3(.017f, .07f, 0), new Vector3(.022f, .1f, 0), new Vector3(.03f, .125f, 0), new Vector3(.02f, .15f, 0), new Vector3(0f, .156f, 0) }, 12);   // uchwyt toczony
        }
    }

    /// <summary>Hełm nurka (miedź) — punkt = środek spodu napierśnika (do gabloty), przod = iluminator przedni. Wys. 0.53 m × K.</summary>
    public static Transform HelmNurka(Transform rodzic, string nazwa, Vector3 p, Vector3 przod) => Postaw(rodzic, "Hełm nurka · " + nazwa, "muzeum_helm_nurka", BudujHelm, p, przod);
    static void BudujHelm(BraxChlodnie.Partie p)
    {
        var cu = p[miedz]; var st = p[stal]; var gl = p[szklo];
        BraxChlodnie.Toczony(cu, Vector3.zero, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.27f, 0f, 0), new Vector3(.3f, .012f, 0), new Vector3(.3f, .04f, 0), new Vector3(.285f, .055f, 0), new Vector3(.26f, .095f, 0), new Vector3(.225f, .135f, 0), new Vector3(.19f, .16f, 0) }, Bl, 32);   // napierśnik
        BraxChlodnie.Toczony(st, Vector3.zero, new List<Vector3> { new Vector3(.186f, .157f, 0), new Vector3(.206f, .163f, 0), new Vector3(.208f, .19f, 0), new Vector3(.188f, .197f, 0) }, Bl, 32);   // pierścień szyjny
        BraxChlodnie.Toczony(cu, Vector3.zero, new List<Vector3> { new Vector3(.19f, .194f, 0), new Vector3(.2f, .24f, 0), new Vector3(.202f, .31f, 0), new Vector3(.19f, .39f, 0), new Vector3(.16f, .45f, 0), new Vector3(.11f, .495f, 0), new Vector3(.05f, .52f, 0), new Vector3(0f, .526f, 0) }, Bl, 32);   // hełm
        for (int i = 0; i < 12; i++) { float k = i * Mathf.PI / 6f + Mathf.PI / 12f; var d = new Vector3(Mathf.Cos(k), 0f, Mathf.Sin(k)); st.Rura(d * .29f + Vector3.up * .028f, d * .318f + Vector3.up * .028f, .009f, 6); }   // śruby obręczy (między iluminatorami, łby 1.8 cm nad obręczą)
        var ciemny = new[] { C(1f, 1f, 1f), C(.1f, .1f, .11f) };
        void Iluminator(Vector3 srodek, Vector3 os, float r, bool krata)
        {   // pierścień (zatopiony 3 cm w hełmie), ciemne wnętrze 2.2 cm za szybą, szyba, krata
            var b0 = srodek - os * .03f;
            TocOs(st, b0, os, new List<Vector3> { new Vector3(r + .025f, 0f, 0), new Vector3(r + .025f, .06f, 0), new Vector3(r + .01f, .078f, 0), new Vector3(r + .002f, .076f, 0), new Vector3(r, .044f, 0) }, 24);
            TocOs(p[czern], b0, os, new List<Vector3> { new Vector3(r + .001f, .044f, 0), new Vector3(0f, .044f, 0) }, 24, ciemny);   // ciemne wnętrze hełmu za szybą (mat, nie stal)
            TocOs(gl, b0, os, new List<Vector3> { new Vector3(r + .001f, .066f, 0), new Vector3(0f, .066f, 0) }, 24);
            if (!krata) return;
            var pr = Vector3.Cross(os, Vector3.up).normalized; if (pr.sqrMagnitude < .5f) pr = Vector3.right;
            foreach (float o in new[] { -.35f, .35f }) st.Rura(b0 + os * .085f + Vector3.up * (r * o) - pr * (r + .01f), b0 + os * .085f + Vector3.up * (r * o) + pr * (r + .01f), .006f, 6);
        }
        Iluminator(new Vector3(0f, .33f, .212f), Vector3.forward, .085f, false);
        Iluminator(new Vector3(.212f, .33f, 0f), Vector3.right, .06f, true);
        Iluminator(new Vector3(-.212f, .33f, 0f), Vector3.left, .06f, true);
        var dg = new Vector3(0f, .55f, .83f).normalized; Iluminator(new Vector3(0f, .33f, 0f) + dg * .2f, dg, .042f, false);
        st.Rura(new Vector3(0f, .37f, -.18f), new Vector3(0f, .41f, -.26f), .02f, 10);                                  // zawór powietrza
        TocOs(st, new Vector3(0f, .41f, -.26f), new Vector3(0f, .45f, -.89f).normalized, new List<Vector3> { new Vector3(.032f, 0f, 0), new Vector3(.032f, .03f, 0), new Vector3(.02f, .04f, 0), new Vector3(0f, .042f, 0) }, 12);
    }

    /// <summary>Armata okrętowa na lawecie z podestem i piramidą kul: punkt = środek podestu na posadzce, przod = kierunek lufy.</summary>
    public static Transform Armata(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
    {
        var g = Postaw(rodzic, "Armata okrętowa · " + nazwa, "muzeum_armata", BudujArmate, p, przod);
        BraxChlodnie.Kolizja(g, "armata", new Vector3(-.75f, 0f, -1.05f), new Vector3(1.05f, .95f, 1.3f));
        BraxChlodnie.Lod(g, 3f);
        return g;
    }
    static void BudujArmate(BraxChlodnie.Partie p)
    {
        var fe = p[zeliwo]; var dr = p[drewno]; var dk = p[dab]; const float y0 = .12f, yb = y0 + .64f, zz = -.62f, tz = zz + .82f;
        for (int i = 0; i < 6; i++) dk.Poducha(new Vector3(-.75f + i * .3f + .003f, 0f, -1.05f), new Vector3(-.75f + (i + 1) * .3f - .003f, y0, 1.25f), .012f, 0f, 3);   // podest z desek
        TocOs(fe, new Vector3(0f, yb, zz), Vector3.forward, new List<Vector3> {
            new Vector3(0f, -.17f, 0), new Vector3(.045f, -.158f, 0), new Vector3(.05f, -.13f, 0), new Vector3(.028f, -.1f, 0), new Vector3(.028f, -.07f, 0), new Vector3(.09f, -.045f, 0), new Vector3(.17f, -.012f, 0), new Vector3(.2f, 0f, 0),
            new Vector3(.205f, .03f, 0), new Vector3(.198f, .065f, 0), new Vector3(.19f, .075f, 0), new Vector3(.188f, .55f, 0), new Vector3(.2f, .56f, 0), new Vector3(.2f, .59f, 0), new Vector3(.176f, .6f, 0), new Vector3(.166f, 1.25f, 0),
            new Vector3(.176f, 1.26f, 0), new Vector3(.176f, 1.28f, 0), new Vector3(.152f, 1.29f, 0), new Vector3(.136f, 1.76f, 0), new Vector3(.142f, 1.8f, 0), new Vector3(.162f, 1.85f, 0), new Vector3(.166f, 1.9f, 0), new Vector3(.166f, 1.94f, 0),
            new Vector3(.072f, 1.946f, 0), new Vector3(.066f, 1.93f, 1), new Vector3(.066f, 1.6f, 1), new Vector3(0f, 1.6f, 1) }, 28, new[] { C(1f, 1f, 1f), C(.2f, .2f, .2f) });   // lufa: winograd, zamek, pierścienie, wylot, przewód
        fe.Rura(new Vector3(-.26f, yb, tz), new Vector3(.26f, yb, tz), .055f, 14);                                       // czopy
        foreach (float sx in new[] { -1f, 1f })
        {
            float x0 = sx > 0 ? .23f : -.33f, x1 = x0 + .1f;
            dr.Box(new Vector3(x0, y0 + .14f, -.78f), new Vector3(x1, y0 + .4f, -.46f));                               // policzki lawety (schodkowe)
            dr.Box(new Vector3(x0, y0 + .14f, -.46f), new Vector3(x1, y0 + .5f, -.14f));
            dr.Box(new Vector3(x0, y0 + .14f, -.14f), new Vector3(x1, yb - .03f, .42f));
            fe.Box(new Vector3(x0 - .004f, yb - .03f, tz - .075f), new Vector3(x1 + .004f, yb + .07f, tz - .06f));        // okucie czopa
            fe.Box(new Vector3(x0 - .004f, yb - .03f, tz + .06f), new Vector3(x1 + .004f, yb + .07f, tz + .075f));
            fe.Box(new Vector3(x0 - .004f, yb + .055f, tz - .06f), new Vector3(x1 + .004f, yb + .07f, tz + .06f));
            foreach (var (zk, rk) in new[] { (.28f, .2f), (-.67f, .18f) })
            {   // koła (drewno) z żelazną obręczą i piastą
                var c0 = new Vector3(sx * .42f, y0 + rk, zk); var os = Vector3.right * sx;
                TocOs(dr, c0, os, new List<Vector3> { new Vector3(0f, -.045f, 0), new Vector3(rk - .02f, -.045f, 0), new Vector3(rk - .02f, .045f, 0), new Vector3(0f, .045f, 0) }, 24);
                TocOs(fe, c0, os, new List<Vector3> { new Vector3(rk - .02f, -.047f, 0), new Vector3(rk, -.047f, 0), new Vector3(rk, .047f, 0), new Vector3(rk - .02f, .047f, 0) }, 24);
                TocOs(fe, c0, os, new List<Vector3> { new Vector3(.05f, .044f, 0), new Vector3(.05f, .07f, 0), new Vector3(.03f, .08f, 0), new Vector3(0f, .08f, 0) }, 14);
            }
        }
        dr.Box(new Vector3(-.39f, y0 + .1f, .2f), new Vector3(.39f, y0 + .22f, .36f));                                  // osie (przednia, tylna)
        dr.Box(new Vector3(-.39f, y0 + .09f, -.74f), new Vector3(.39f, y0 + .2f, -.6f));
        dr.Box(new Vector3(-.23f, y0 + .32f, -.75f), new Vector3(.23f, y0 + .4f, -.2f));                                 // łoże
        dr.Box(new Vector3(-.12f, y0 + .4f, -.62f), new Vector3(.12f, y0 + .45f, -.3f));                                 // klin pod zamkiem
        for (int l = 0; l < 3; l++)                                                                                       // piramida kul 9 + 4 + 1
            for (int i = 0; i < 3 - l; i++)
                for (int j = 0; j < 3 - l; j++)
                    BraxChlodnie.Toczony(fe, new Vector3(.6f + l * .065f + i * .13f, y0 + .065f + l * .092f, -.85f + l * .065f + j * .13f), KulaProfil(.065f, 0f, 8), Bl, 14);
    }

    // ---------------------------------------------------------------- gablota (klosz): stal w narożach, szyby dwustronne w osi ramy
    static void Klosz(BraxChlodnie.Partie p, Vector3 lo, Vector3 hi)
    {
        var s = p[szklo]; var r = p[stal]; const float e = .02f; float h = e * .5f;
        foreach (float x in new[] { lo.x, hi.x - e }) foreach (float z in new[] { lo.z, hi.z - e }) r.Box(new Vector3(x, lo.y, z), new Vector3(x + e, hi.y, z + e));
        foreach (float y in new[] { lo.y, hi.y - e })
        {
            foreach (float z in new[] { lo.z, hi.z - e }) r.Box(new Vector3(lo.x + e, y, z), new Vector3(hi.x - e, y + e, z + e));
            foreach (float x in new[] { lo.x, hi.x - e }) r.Box(new Vector3(x, y, lo.z + e), new Vector3(x + e, y + e, hi.z - e));
        }
        void Sz(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n) { s.Quad(a, b, c, d, n); s.Quad(a, b, c, d, -n); }
        float y0 = lo.y + e, y1 = hi.y - e;
        Sz(new Vector3(lo.x + e, y0, hi.z - h), new Vector3(hi.x - e, y0, hi.z - h), new Vector3(hi.x - e, y1, hi.z - h), new Vector3(lo.x + e, y1, hi.z - h), Vector3.forward);
        Sz(new Vector3(lo.x + e, y0, lo.z + h), new Vector3(hi.x - e, y0, lo.z + h), new Vector3(hi.x - e, y1, lo.z + h), new Vector3(lo.x + e, y1, lo.z + h), Vector3.back);
        Sz(new Vector3(hi.x - h, y0, lo.z + e), new Vector3(hi.x - h, y0, hi.z - e), new Vector3(hi.x - h, y1, hi.z - e), new Vector3(hi.x - h, y1, lo.z + e), Vector3.right);
        Sz(new Vector3(lo.x + h, y0, lo.z + e), new Vector3(lo.x + h, y0, hi.z - e), new Vector3(lo.x + h, y1, hi.z - e), new Vector3(lo.x + h, y1, lo.z + e), Vector3.left);
        Sz(new Vector3(lo.x + e, hi.y - h, lo.z + e), new Vector3(hi.x - e, hi.y - h, lo.z + e), new Vector3(hi.x - e, hi.y - h, hi.z - e), new Vector3(lo.x + e, hi.y - h, hi.z - e), Vector3.up);
    }

    /// <summary>Model statku w dużej gablocie (szafka + klosz): punkt = środek szafki na posadzce, przod = burta do oglądających;
    /// wariant 0 = fregata (3 maszty), 1 = bryg (2 maszty rejowe), 2 = kuter (1 maszt z gaflem i foki).</summary>
    public static Transform Zaglowiec(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int wariant = 0)
    {
        Materialy(); wariant = Mathf.Clamp(wariant, 0, 2); var km = KadlubM(wariant); int w = wariant;
        var g = Postaw(rodzic, "Model statku · " + nazwa, "muzeum_zaglowiec_" + w, q => BudujZaglowiec(q, km, w), p, przod);
        BraxChlodnie.Kolizja(g, "gablota żaglowca", new Vector3(-1.03f, 0f, -.48f), new Vector3(1.03f, 2.07f, .48f));
        return g;
    }
    static void BudujZaglowiec(BraxChlodnie.Partie q, Material kadlub, int w)
    {
        const float A = 1.0f, Bz = .45f, Hs = .85f, Hk = 1.22f;
        q[drewno].Poducha(new Vector3(-A, 0f, -Bz), new Vector3(A, Hs - .03f, Bz), .02f, 0f, 3);                        // szafka
        q[drewno].Poducha(new Vector3(-A - .03f, Hs - .03f, -Bz - .03f), new Vector3(A + .03f, Hs, Bz + .03f), .012f, 0f, 3);   // blat z gzymsem
        Klosz(q, new Vector3(-A + .02f, Hs, -Bz + .02f), new Vector3(A - .02f, Hs + Hk, Bz - .02f));
        var dr = q[drewno];
        dr.Poducha(new Vector3(-.6f, Hs, -.16f), new Vector3(.6f, Hs + .025f, .16f), .006f, 0f, 3);                     // deska ekspozycyjna
        foreach (float x in new[] { -.3f, .25f }) dr.Box(new Vector3(x - .035f, Hs + .025f, -.06f), new Vector3(x + .035f, Hs + .1f, .06f));   // podpórki
        // ---- kadłub z wręg (u: 0 rufa → 1 dziób; t: −1 lewa burta … 0 stępka … 1 prawa burta)
        float L = w == 0 ? 1.1f : w == 1 ? .95f : .82f, B = w == 2 ? .22f : .24f, H = w == 2 ? .18f : .2f; var o = new Vector3(0f, Hs + .095f, 0f);
        float Szer(float u) => B * .5f * (1f - Mathf.Pow(Mathf.Max(0f, (u - .55f) / .45f), 2f)) * (.78f + .22f * SS(0f, .3f, u)) + .003f;
        float Gl(float u) => H * (1f - .4f * Mathf.Pow(Mathf.Max(0f, (u - .72f) / .28f), 2f)) * (1f - .1f * Mathf.Max(0f, (.12f - u) / .12f));
        float Sh(float u) => .045f * (1f - u) * (1f - u) + .03f * u * u * u;
        Vector3 Pt(float u, float t)
        {
            float b = Szer(u), d = Gl(u), ys = H + Sh(u), a = t * Mathf.PI * .5f;
            return o + new Vector3(-L * .5f + u * L, ys - d * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(a)), 1.3f), b * Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), .7f));
        }
        float[] ts = { -1f, -.93f, -.86f, -.78f, -.68f, -.6f, -.48f, -.34f, -.18f, 0f, .18f, .34f, .48f, .6f, .68f, .78f, .86f, .93f, 1f };
        const int N = 24; int M = ts.Length; var s = q[kadlub]; int k0 = s.v.Count;
        for (int i = 0; i <= N; i++) for (int j = 0; j < M; j++) { float u = (float)i / N; s.v.Add(Pt(u, ts[j])); s.uv.Add(new Vector2(u, Mathf.Abs(ts[j]))); s.c.Add(s.kolor); s.n.Add(Vector3.up); s.t.Add(new Vector4(1f, 0f, 0f, 1f)); }
        for (int i = 0; i <= N; i++)
            for (int j = 0; j < M; j++)
            {
                int id = k0 + i * M + j;
                var dS = s.v[k0 + Mathf.Min(N, i + 1) * M + j] - s.v[k0 + Mathf.Max(0, i - 1) * M + j];
                var dT = s.v[k0 + i * M + Mathf.Min(M - 1, j + 1)] - s.v[k0 + i * M + Mathf.Max(0, j - 1)];
                var sr = new Vector3(s.v[id].x, o.y + H * .6f, o.z); var n = Vector3.Cross(dS, dT);
                if (n.sqrMagnitude < 1e-10f) n = s.v[id] - sr; n.Normalize(); if (Vector3.Dot(n, s.v[id] - sr) < 0f) n = -n;
                s.n[id] = n; var tg = dS.normalized; s.t[id] = new Vector4(tg.x, tg.y, tg.z, 1f);
            }
        for (int i = 0; i < N; i++)
            for (int j = 0; j < M - 1; j++)
            {
                int a = k0 + i * M + j, b = a + 1, c = a + M, d = c + 1; var ns = s.n[a] + s.n[b] + s.n[c] + s.n[d];
                if (Vector3.Dot(Vector3.Cross(s.v[b] - s.v[a], s.v[d] - s.v[a]), ns) + Vector3.Dot(Vector3.Cross(s.v[d] - s.v[a], s.v[c] - s.v[a]), ns) >= 0f) s.i.AddRange(new[] { a, b, d, a, d, c });
                else s.i.AddRange(new[] { a, d, b, a, c, d });
            }
        var srR = Vector3.zero; for (int j = 0; j < M; j++) srR += Pt(0f, ts[j]); srR /= M;
        for (int j = 0; j < M - 1; j++) dr.Tri(srR, Pt(0f, ts[j]), Pt(0f, ts[j + 1]), Vector3.left);                    // pawęż (rufa)
        dr.Rura(Pt(1f, 0f), Pt(1f, 1f) + Vector3.up * .025f, .008f, 8);                                                  // dziobnica
        // pokład (1.2 cm pod nadburciem) i nadburcie od środka
        for (int i = 0; i < N; i++)
        {
            float u0 = (float)i / N, u1 = (float)(i + 1) / N; var dn = Vector3.down * .012f;
            Vector3 l0 = Pt(u0, -1f) + dn, l1 = Pt(u1, -1f) + dn, r0 = Pt(u0, 1f) + dn, r1 = Pt(u1, 1f) + dn;
            q[dab].Quad(l0, r0, r1, l1, Vector3.up);
            dr.Quad(Pt(u0, -1f), Pt(u1, -1f), l1, l0, Vector3.forward); dr.Quad(Pt(u0, 1f), Pt(u1, 1f), r1, r0, Vector3.back);
        }
        float Pok(float x) { float u = (x + L * .5f) / L; return o.y + H + Sh(u) - .012f; }
        dr.Box(new Vector3(.05f, Pok(.05f), -.04f), new Vector3(.13f, Pok(.05f) + .018f, .04f));                          // luki
        dr.Box(new Vector3(-.2f, Pok(-.2f), -.035f), new Vector3(-.13f, Pok(-.2f) + .018f, .035f));
        dr.Rura(new Vector3(-.08f, Pok(-.08f), 0f), new Vector3(-.08f, Pok(-.08f) + .035f, 0f), .018f, 12);               // kabestan
        var bow = new Vector3(L * .5f - .04f, Pok(L * .5f - .04f) + .012f, 0f); var bEnd = new Vector3(L * .5f + .27f, bow.y + .12f, 0f);
        dr.Rura(bow, bEnd, .007f, 8);                                                                                    // bukszpryt
        // ---- maszty, reje, żagle, olinowanie
        float[] mx = w == 0 ? new[] { .27f, -.02f, -.3f } : w == 1 ? new[] { .2f, -.15f } : new[] { .06f }, mh = w == 0 ? new[] { .72f, .8f, .58f } : w == 1 ? new[] { .68f, .74f } : new[] { .78f };
        int gafel = w == 0 ? 2 : w == 2 ? 0 : -1; var topy = new Vector3[mx.Length]; var wanty = new List<(Vector3, Vector3)>();
        var br = Quaternion.Euler(0f, 25f, 0f); Vector3 rk = br * Vector3.forward, nS = Vector3.Cross(rk, Vector3.up).normalized;   // reje brasowane 25°
        if (nS.x < 0f) nS = -nS;   // wydęcie żagli ku dziobowi (+x)
        for (int m = 0; m < mx.Length; m++)
        {
            float yd = Pok(mx[m]); var b0 = new Vector3(mx[m], yd - .03f, 0f); var top = new Vector3(mx[m], yd + mh[m], 0f); topy[m] = top;
            dr.Rura(b0, new Vector3(mx[m], yd + mh[m] * .56f, 0f), .009f, 8); dr.Rura(new Vector3(mx[m], yd + mh[m] * .52f, 0f), top, .0055f, 8);   // maszt i stenga
            dr.Box(new Vector3(mx[m] - .03f, yd + mh[m] * .5f, -.04f), new Vector3(mx[m] + .03f, yd + mh[m] * .5f + .006f, .04f));               // mars
            float b = Szer((mx[m] + L * .5f) / L);
            foreach (float sz in new[] { -1f, 1f }) foreach (float dx in new[] { -.02f, -.06f, -.1f }) wanty.Add((new Vector3(mx[m], yd + mh[m] * .5f, 0f), new Vector3(mx[m] + dx, yd + .012f, sz * (b - .004f))));
            if (m == gafel)
            {   // bezan / grot kutra: bom, gafel i żagiel skośny w płaszczyźnie symetrii (dwustronny)
                float lb = w == 2 ? .42f : .3f, hg = w == 2 ? .58f : .42f;
                var bom0 = new Vector3(mx[m], yd + .08f, 0f); var bom1 = new Vector3(mx[m] - lb, yd + .1f, 0f); var gaf0 = new Vector3(mx[m], yd + hg, 0f); var gaf1 = new Vector3(mx[m] - lb * .85f, yd + hg + .1f, 0f);
                dr.Rura(bom0, bom1, .004f, 6); dr.Rura(gaf0, gaf1, .004f, 6);
                var pk = new[] { bom0 + new Vector3(-.01f, .006f, 0f), bom1 + new Vector3(.005f, .006f, 0f), gaf1 + new Vector3(.005f, -.006f, 0f), gaf0 + new Vector3(-.01f, -.006f, 0f) };
                q[plotno].Quad(pk[0], pk[1], pk[2], pk[3], Vector3.forward); q[plotno].Quad(pk[0], pk[1], pk[2], pk[3], Vector3.back);
                continue;
            }
            float[] hy = { .28f, .52f, .76f }, hl = { .2f, .16f, .11f }; float sk = mh[m] / .8f;
            for (int r = 0; r < 3; r++)
            {
                var cy = new Vector3(mx[m] + .012f, yd + hy[r] * mh[m], 0f); float l = hl[r] * sk;
                dr.Rura(cy - rk * l, cy + rk * l, .004f, 6);                                                             // reja
                float yTop = cy.y - .006f, yBot = r == 0 ? yd + .1f : yd + hy[r - 1] * mh[m] + .006f, lB = r == 0 ? l * 1.05f : hl[r - 1] * sk * .97f;
                var pts = new Vector3[5, 4];
                for (int a = 0; a <= 4; a++)
                    for (int v = 0; v <= 3; v++)
                    {
                        float ua = a / 4f * 2f - 1f, vv = v / 3f, ll = Mathf.Lerp(lB, l * .98f, vv);
                        pts[a, v] = new Vector3(cy.x, Mathf.Lerp(yBot, yTop, vv), 0f) + rk * (ua * ll) + nS * (.035f * (1f - ua * ua) * Mathf.Sin(Mathf.PI * Mathf.Lerp(.15f, 1f, vv)));
                    }
                for (int a = 0; a < 4; a++)
                    for (int v = 0; v < 3; v++)
                    { q[plotno].Quad(pts[a, v], pts[a + 1, v], pts[a + 1, v + 1], pts[a, v + 1], nS); q[plotno].Quad(pts[a, v], pts[a + 1, v], pts[a + 1, v + 1], pts[a, v + 1], -nS); }
            }
        }
        // foki od bukszprytu do fokmasztu (dwustronne)
        var fx = new Vector3(mx[0] + .01f, 0f, 0f);
        foreach (var (hTop, t) in new[] { (.62f, 1f), (.4f, .55f) })
        {
            var A1 = new Vector3(fx.x, Pok(mx[0]) + mh[0] * hTop, 0f); var B1 = Vector3.Lerp(bow, bEnd, t); var C1 = new Vector3(fx.x + .02f, Pok(mx[0]) + .12f, 0f);
            q[plotno].Tri(A1, B1, C1, Vector3.forward); q[plotno].Tri(A1, B1, C1, Vector3.back);
        }
        // olinowanie: wanty, sztagi, achtersztag
        var li = q[lina];
        foreach (var (a, b) in wanty) li.Rura(a, b, .0016f, 4);
        li.Rura(topy[0], bEnd, .0016f, 4);
        if (mx.Length > 1) li.Rura(topy[1], new Vector3(mx[0], Pok(mx[0]) + mh[0] * .5f, 0f), .0016f, 4);
        if (mx.Length > 2) li.Rura(topy[2], new Vector3(mx[1], Pok(mx[1]) + mh[1] * .45f, 0f), .0016f, 4);
        int gl = mx.Length > 1 ? 1 : 0; float xa = mx[gl] - .2f;                                                         // achtersztagi z najwyższego masztu
        foreach (float sz in new[] { -1f, 1f }) li.Rura(topy[gl], new Vector3(xa, Pok(xa) + .012f, sz * (Szer((xa + L * .5f) / L) - .004f)), .0016f, 4);
        var pp = topy[gl] + Vector3.down * .01f;                                                                         // proporzec
        q[tuba].Tri(pp, pp + new Vector3(0f, -.03f, 0f), pp + new Vector3(-.11f, -.012f, 0f), Vector3.forward); q[tuba].Tri(pp, pp + new Vector3(0f, -.03f, 0f), pp + new Vector3(-.11f, -.012f, 0f), Vector3.back);
    }

    /// <summary>Gramofon z tubą na postumencie: punkt = środek postumentu na posadzce, przod = kierunek tuby.</summary>
    public static Transform Gramofon(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
    {
        var g = Postaw(rodzic, "Gramofon z tubą · " + nazwa, "muzeum_gramofon", BudujGramofon, p, przod);
        BraxChlodnie.Kolizja(g, "gramofon", new Vector3(-.33f, 0f, -.33f), new Vector3(.33f, 1.1f, .33f));
        return g;
    }
    static void BudujGramofon(BraxChlodnie.Partie p)
    {
        const float Hp = .85f, y1 = Hp + .17f; var dr = p[drewno]; var st = p[stal];
        p[marmur].Poducha(new Vector3(-.32f, 0f, -.32f), new Vector3(.32f, Hp, .32f), .015f, 0f, 3);                   // postument
        dr.Poducha(new Vector3(-.19f, Hp, -.19f), new Vector3(.19f, y1, .19f), .012f, 0f, 3);                            // skrzynka
        dr.Poducha(new Vector3(-.205f, Hp, -.205f), new Vector3(.205f, Hp + .025f, .205f), .008f, 0f, 3);                // cokolik
        BraxChlodnie.Toczony(p[talerz], new Vector3(0f, y1, 0f), new List<Vector3> { new Vector3(.15f, 0f, 0), new Vector3(.15f, .022f, 0), new Vector3(.127f, .022f, 0), new Vector3(.127f, .022f, 1), new Vector3(.125f, .026f, 1),
            new Vector3(.042f, .026f, 1), new Vector3(.042f, .026f, 2), new Vector3(.006f, .026f, 2), new Vector3(.006f, .026f, 3), new Vector3(.004f, .04f, 3), new Vector3(0f, .04f, 3) },
            new[] { C(.12f, .24f, .15f), C(.025f, .025f, .03f), C(.62f, .1f, .1f), C(.85f, .86f, .88f) }, 40);            // filc, płyta, etykieta, trzpień
        // tuba: lakier bordo na zewnątrz, wywinięty brzeg, jaśniejsze wnętrze; przetłoczenia płatów (8 linii)
        var dir = new Vector3(0f, .52f, .855f).normalized; var gardlo = new Vector3(0f, y1 + .1f, -.13f); const float Lh = .62f, Rb = .27f; const int nH = 14;
        float Rt(float t) => .024f + (Rb - .024f) * Mathf.Pow(t / Lh, 2.6f);
        var pr = new List<Vector3>();
        for (int i = 0; i <= nH; i++) { float t = Lh * i / nH; pr.Add(new Vector3(Rt(t), t, 0)); }
        pr.Add(new Vector3(Rb + .008f, Lh + .004f, 1)); pr.Add(new Vector3(Rb + .004f, Lh + .012f, 1));
        for (int i = nH; i >= 0; i--) { float t = Lh * i / nH; pr.Add(new Vector3(Rt(t) - .005f, t, 2)); }
        TocOs(p[tuba], gardlo, dir, pr, 32, new[] { C(1f, 1f, 1f), C(.8f, .8f, .8f), C(1f, .85f, .78f) });
        var e1 = Vector3.Cross(dir, Vector3.right).normalized; var e2 = Vector3.Cross(dir, e1);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI / 4f; var rd = e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a);
            for (int i = 1; i < 7; i++) { float t0 = Lh * (i - 1) / 6f, t1 = Lh * i / 6f; p[tuba].Rura(gardlo + dir * t0 + rd * (Rt(t0) + .002f), gardlo + dir * t1 + rd * (Rt(t1) + .002f), .003f, 4); }
        }
        st.Rura(new Vector3(0f, y1 - .005f, -.13f), gardlo + dir * .01f, .026f, 12);                                     // kolano tuby
        var dol = e1.y < 0f ? e1 : -e1; var uchwyt = gardlo + dir * .3f + dol * (Rt(.3f) + .006f);
        st.Rura(new Vector3(0f, y1, -.175f), new Vector3(0f, y1 + .22f, -.175f), .009f, 8); st.Rura(new Vector3(0f, y1 + .22f, -.175f), uchwyt, .009f, 8);   // wspornik tuby
        st.Rura(new Vector3(0f, y1 + .08f, -.13f), new Vector3(.03f, y1 + .075f, -.035f), .011f, 10);                     // ramię
        TocOs(st, new Vector3(.02f, y1 + .065f, -.02f), Vector3.right, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.035f, 0f, 0), new Vector3(.035f, .02f, 0), new Vector3(0f, .02f, 0) }, 18);   // membrana
        st.Rura(new Vector3(.03f, y1 + .035f, -.02f), new Vector3(.03f, y1 + .027f, -.012f), .0015f, 4);                   // igła
        st.Rura(new Vector3(.19f, y1 - .08f, 0f), new Vector3(.25f, y1 - .08f, 0f), .008f, 8); st.Rura(new Vector3(.25f, y1 - .08f, 0f), new Vector3(.25f, y1 - .14f, .03f), .007f, 8);   // korba
        dr.Rura(new Vector3(.25f, y1 - .14f, .03f), new Vector3(.29f, y1 - .14f, .03f), .012f, 10);
    }

    // ---------------------------------------------------------------- bicykl
    static void KoloBicykla(BraxChlodnie.Partie p, Vector3 c, float R, int nSzpr)
    {
        TocOs(p[stal], c, Vector3.forward, Torus(R - .012f, .012f, .01f, 0f, 8), 56);                                    // obręcz
        TocOs(p[guma], c, Vector3.forward, Torus(R + .008f, .017f, .017f, 0f, 8), 56);                                   // opona
        TocOs(p[stal], c, Vector3.forward, new List<Vector3> { new Vector3(0f, -.05f, 0), new Vector3(.045f, -.05f, 0), new Vector3(.045f, -.04f, 0), new Vector3(.02f, -.035f, 0), new Vector3(.02f, .035f, 0), new Vector3(.045f, .04f, 0), new Vector3(.045f, .05f, 0), new Vector3(0f, .05f, 0) }, 16);   // piasta z kołnierzami
        for (int i = 0; i < nSzpr; i++)
        {
            float a = i * Mathf.PI * 2f / nSzpr, sg = i % 2 == 0 ? 1f : -1f; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            p[stal].Rura(c + d * .04f + Vector3.forward * (.045f * sg), c + d * (R - .02f) + Vector3.forward * (.003f * sg), .0022f, 4);
        }
    }
    /// <summary>Bicykl (wysokie koło) na podeście: punkt = środek podestu na posadzce, przod = bok roweru do oglądających
    /// (rower wzdłuż lokalnego x, przednie koło w +x).</summary>
    public static Transform Bicykl(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
    {
        var g = Postaw(rodzic, "Bicykl · " + nazwa, "muzeum_bicykl", BudujBicykl, p, przod);
        BraxChlodnie.Kolizja(g, "bicykl", new Vector3(-1.05f, 0f, -.4f), new Vector3(1.2f, 1.65f, .4f));
        BraxChlodnie.Lod(g, 2.5f);
        return g;
    }
    static void BudujBicykl(BraxChlodnie.Partie p)
    {
        const float yP = .1f, R1 = .66f, R2 = .2f; var ra = p[czern]; var dr = p[drewno];
        dr.Poducha(new Vector3(-1.05f, 0f, -.32f), new Vector3(1.2f, yP, .32f), .015f, 0f, 3);                           // podest
        var c1 = new Vector3(.42f, yP + R1 + .025f, 0f); var c2 = new Vector3(-.78f, yP + R2 + .025f, 0f);
        KoloBicykla(p, c1, R1, 48); KoloBicykla(p, c2, R2, 20);
        foreach (var (c, R) in new[] { (c1, R1), (c2, R2) }) foreach (float sx in new[] { -1f, 1f })                     // kliny pod koła
            dr.Box(new Vector3(c.x + sx * .055f - .035f, yP, -.04f), new Vector3(c.x + sx * .055f + .035f, yP + .045f, .04f));
        var glowka = new Vector3(.36f, c1.y + R1 + .09f, 0f);
        foreach (float sz in new[] { -1f, 1f }) ra.Rura(c1 + new Vector3(0f, 0f, .058f * sz), glowka + new Vector3(.01f, -.06f, .045f * sz), .012f, 8);   // widelec
        ra.Rura(glowka + new Vector3(.012f, -.065f, -.05f), glowka + new Vector3(.012f, -.065f, .05f), .012f, 8);           // korona widelca
        ra.Rura(glowka + new Vector3(.01f, -.07f, 0f), glowka + new Vector3(-.005f, .05f, 0f), .02f, 10);                   // główka
        var kc = glowka + new Vector3(-.01f, .075f, 0f);                                                                  // kierownica
        ra.Rura(glowka + new Vector3(-.005f, .045f, 0f), kc, .012f, 8);
        var kl = kc + new Vector3(-.03f, -.05f, -.3f); var kp = kc + new Vector3(-.03f, -.05f, .3f);
        ra.Rura(kl, kc + new Vector3(0f, 0f, -.08f), .01f, 8); ra.Rura(kc + new Vector3(0f, 0f, -.08f), kc + new Vector3(0f, 0f, .08f), .01f, 8); ra.Rura(kc + new Vector3(0f, 0f, .08f), kp, .01f, 8);
        dr.Rura(kl, kl + new Vector3(0f, 0f, -.09f), .016f, 10); dr.Rura(kp, kp + new Vector3(0f, 0f, .09f), .016f, 10);   // rączki
        float rho = R1 + .1f; var pts = new List<Vector3>();                                                              // rama łukowa
        for (int i = 0; i <= 8; i++) { float a = Mathf.Lerp(97f, 158f, i / 8f) * Mathf.Deg2Rad; pts.Add(c1 + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rho); }
        var tyl = c2 + new Vector3(.1f, .16f, 0f); pts.Add(tyl);
        ra.Rura(glowka + new Vector3(-.012f, 0f, 0f), pts[0], .02f, 10);
        for (int i = 0; i + 1 < pts.Count; i++) ra.Rura(pts[i], pts[i + 1], Mathf.Lerp(.02f, .013f, i / (float)(pts.Count - 1)), 10);
        foreach (float sz in new[] { -1f, 1f }) ra.Rura(tyl, c2 + new Vector3(0f, 0f, .055f * sz), .01f, 8);              // widelec tylny
        float aS = 106f * Mathf.Deg2Rad; var sp = c1 + new Vector3(Mathf.Cos(aS), Mathf.Sin(aS), 0f) * rho;                 // siodełko na sprężynach
        p[skora].Poducha(sp + new Vector3(-.13f, .045f, -.075f), sp + new Vector3(.1f, .085f, .075f), .025f, .006f, 6);
        foreach (float dx in new[] { -.08f, .05f }) p[stal].Rura(sp + new Vector3(dx, -.01f, 0f), sp + new Vector3(dx, .047f, 0f), .011f, 8);
        foreach (float sz in new[] { -1f, 1f })
        {   // korby z pedałami
            float a = (sz > 0 ? 300f : 120f) * Mathf.Deg2Rad; var e = c1 + new Vector3(Mathf.Cos(a) * .12f, Mathf.Sin(a) * .12f, .065f * sz);
            ra.Rura(c1 + new Vector3(0f, 0f, .065f * sz), e, .009f, 8);
            p[guma].Box(e + new Vector3(-.045f, -.013f, sz > 0 ? .008f : -.075f), e + new Vector3(.045f, .013f, sz > 0 ? .075f : -.008f));
        }
        ra.Rura(pts[7], pts[7] + new Vector3(0f, 0f, .055f), .008f, 8); p[guma].Box(pts[7] + new Vector3(-.03f, -.01f, .035f), pts[7] + new Vector3(.03f, .01f, .075f));   // stopień
    }

    // ---------------------------------------------------------------- globus
    static void KulaUV(UlSiatka s, Vector3 c, float r, int nLon, int nLat, Quaternion q)
    {
        int k0 = s.v.Count;
        for (int j = 0; j <= nLat; j++)
        {
            float lat = -Mathf.PI * .5f + Mathf.PI * j / nLat;
            for (int i = 0; i <= nLon; i++)
            {
                float lon = -Mathf.PI + Mathf.PI * 2f * i / nLon;
                var d = q * new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon)); var tg = q * new Vector3(-Mathf.Sin(lon), 0f, Mathf.Cos(lon));
                s.v.Add(c + d * r); s.n.Add(d); s.t.Add(new Vector4(tg.x, tg.y, tg.z, 1f)); s.uv.Add(new Vector2((float)i / nLon, (float)j / nLat)); s.c.Add(s.kolor);
            }
        }
        for (int j = 0; j < nLat; j++)
            for (int i = 0; i < nLon; i++)
            {
                int a = k0 + j * (nLon + 1) + i, b = a + 1, cc = a + nLon + 1, dd = cc + 1; var ns = s.n[a] + s.n[b] + s.n[cc] + s.n[dd];
                if (Vector3.Dot(Vector3.Cross(s.v[b] - s.v[a], s.v[dd] - s.v[a]), ns) + Vector3.Dot(Vector3.Cross(s.v[dd] - s.v[a], s.v[cc] - s.v[a]), ns) >= 0f) s.i.AddRange(new[] { a, b, dd, a, dd, cc });
                else s.i.AddRange(new[] { a, dd, b, a, cc, dd });
            }
    }
    /// <summary>Globus na stojaku: punkt = środek na posadzce, przod = strona oglądana (Europa i Afryka do oglądających).</summary>
    public static Transform Globus(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
    {
        Materialy(); var gm = GlobusM;
        var g = Postaw(rodzic, "Globus · " + nazwa, "muzeum_globus", q => BudujGlobus(q, gm), p, przod);
        BraxChlodnie.Kolizja(g, "globus", new Vector3(-.44f, 0f, -.44f), new Vector3(.44f, 1.34f, .44f));
        return g;
    }
    static void BudujGlobus(BraxChlodnie.Partie p, Material mapa)
    {
        const float yc = 1.0f, Rg = .3f, Rm = .328f; var dr = p[drewno]; var st = p[stal]; var c = new Vector3(0f, yc, 0f);
        TocOs(dr, c, Vector3.up, new List<Vector3> { new Vector3(.36f, -.012f, 0), new Vector3(.43f, -.012f, 0), new Vector3(.43f, .012f, 0), new Vector3(.36f, .012f, 0), new Vector3(.36f, -.012f, 0) }, 48);   // obręcz horyzontu
        for (int i = 0; i < 3; i++)
        {   // nogi toczone i rozpórki
            float a = (90f + i * 120f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var F = d * .36f; var T = d * .395f + Vector3.up * (yc - .012f); var os = (T - F).normalized; float Ln = (T - F).magnitude;
            TocOs(dr, F, os, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.03f, .005f, 0), new Vector3(.035f, .03f, 0), new Vector3(.02f, .06f, 0), new Vector3(.022f, Ln * .3f, 0), new Vector3(.03f, Ln * .33f, 0),
                new Vector3(.018f, Ln * .36f, 0), new Vector3(.016f, Ln * .8f, 0), new Vector3(.024f, Ln * .85f, 0), new Vector3(.02f, Ln, 0), new Vector3(0f, Ln + .001f, 0) }, 12);
            dr.Rura(d * .372f + Vector3.up * .32f, Vector3.up * .32f, .012f, 8);
        }
        dr.Rura(Vector3.up * .3f, Vector3.up * (yc - Rm - .004f), .022f, 10);                                           // słupek pod południkiem
        TocOs(st, c, Vector3.forward, Torus(Rm, .008f, .006f, 0f, 8), 56);                                                // południk
        var tilt = Quaternion.AngleAxis(-23.5f, Vector3.forward); var os2 = tilt * Vector3.up;
        KulaUV(p[mapa], c, Rg, 48, 24, tilt * Quaternion.Euler(0f, -75f, 0f));
        foreach (float sg in new[] { -1f, 1f }) st.Rura(c + os2 * (sg * (Rg - .01f)), c + os2 * (sg * (Rm + .002f)), .007f, 8);   // osie w biegunach
    }

    // ================================================================ WIĘCEJ WYPOSAŻENIA (12.11 00:31: „wyposażenie musi zrobić większe, bo tam nic nie ma”)
    static Material terakota, zielen, ksiazki, plaszcze;
    static void Materialy2()
    {
        Materialy(); if (terakota) return;
        terakota = BraxUliceKit.Tex("muzeum: terakota (amfory)", .5f, new Color(.86f, .5f, .34f), new Color(.62f, .32f, .2f), .85f, "white_rough_plaster", "white_stucco");
        zielen = BraxCityMaterials.Surface("muzeum: lakier zielony (kocioł maszyny parowej)", new Color(.08f, .22f, .15f), .3f, .35f);
        ksiazki = BraxUliceKit.Tex("muzeum: oprawy książek (skóra, kolory)", .3f, null, new Color(.6f, .55f, .5f), .6f, "leather_white", "white_stucco");
        plaszcze = BraxUliceKit.Tex("muzeum: płaszcze w szatni (wełna, kolory)", .4f, null, new Color(.4f, .4f, .42f), .9f, "rough_linen", "white_stucco");
    }
    static Transform Postaw2(Transform rodzic, string nazwa, string klucz, System.Action<BraxChlodnie.Partie> buduj, Vector3 p, Vector3 przod, Vector3 kolLo, Vector3 kolHi)
    {
        Materialy2(); var g = Postaw(rodzic, nazwa, klucz, buduj, p, przod);
        if (kolHi.y > kolLo.y) BraxChlodnie.Kolizja(g, nazwa, kolLo, kolHi);
        BraxChlodnie.Lod(g, Mathf.Max(1.5f, (kolHi - kolLo).magnitude * .5f));
        return g;
    }
    static void Cokol(BraxChlodnie.Partie p, float a, float c, float h) => p[marmur].Poducha(new Vector3(-a, 0f, -c), new Vector3(a, h, c), .015f, 0f, 3);

    /// <summary>Łódź wiosłowa (4.2 m) na kozłach na podeście: punkt = środek podestu na posadzce, przod = burta do oglądających.</summary>
    public static Transform Lodz(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Łódź wiosłowa · " + nazwa, "muzeum_lodz", BudujLodz, p, przod, new Vector3(-2.4f, 0f, -.95f), new Vector3(2.4f, 1.25f, .95f));
    static void BudujLodz(BraxChlodnie.Partie q)
    {
        var dr = q[drewno]; var dk = q[dab]; const float L = 4.2f, B = 1.3f, H = .55f, g = .03f;
        q[czern].Poducha(new Vector3(-2.35f, 0f, -.9f), new Vector3(2.35f, .12f, .9f), .02f, 0f, 3);                   // podest
        var o = new Vector3(0f, .52f, 0f);
        float Szer(float u) => B * .5f * (1f - Mathf.Pow(Mathf.Max(0f, (u - .5f) / .5f), 1.8f)) * (.7f + .3f * SS(0f, .35f, u)) + .004f;
        float Gl(float u) => H * (1f - .45f * Mathf.Pow(Mathf.Max(0f, (u - .7f) / .3f), 2f));
        float Sh(float u) => .06f * (1f - u) * (1f - u) + .1f * u * u * u;
        Vector3 Pt(float u, float t, float wn)
        {
            float b = Szer(u) - wn, d = Gl(u) - wn, ys = H + Sh(u), a = t * Mathf.PI * .5f;
            return o + new Vector3(-L * .5f + u * L, ys - Mathf.Max(0f, d) * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(a)), 1.2f), Mathf.Max(0f, b) * Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), .65f));
        }
        float[] ts = { -1f, -.85f, -.7f, -.55f, -.4f, -.25f, -.12f, 0f, .12f, .25f, .4f, .55f, .7f, .85f, 1f };
        const int N = 22; int M = ts.Length;
        foreach (var (mat, wn, zewn) in new[] { (dk, 0f, true), (dk, g, false) })
        {   // poszycie z zewnątrz i od środka (2.5–3 cm grubości), UV w metrach (wzdłuż × po obwodzie)
            var s = mat; int k0 = s.v.Count;
            for (int i = 0; i <= N; i++)
            {
                float u = (float)i / N, dl = 0f; var prev = Pt(u, ts[0], wn);
                for (int j = 0; j < M; j++) { var P = Pt(u, ts[j], wn); dl += Vector3.Distance(prev, P); prev = P; s.v.Add(P); s.uv.Add(new Vector2(P.x, dl)); s.c.Add(s.kolor); s.n.Add(Vector3.up); s.t.Add(new Vector4(1f, 0f, 0f, 1f)); }
            }
            for (int i = 0; i <= N; i++)
                for (int j = 0; j < M; j++)
                {
                    int id = k0 + i * M + j; var dS = s.v[k0 + Mathf.Min(N, i + 1) * M + j] - s.v[k0 + Mathf.Max(0, i - 1) * M + j]; var dT = s.v[k0 + i * M + Mathf.Min(M - 1, j + 1)] - s.v[k0 + i * M + Mathf.Max(0, j - 1)];
                    var sr = new Vector3(s.v[id].x, o.y + H * .7f, 0f); var n = Vector3.Cross(dS, dT); if (n.sqrMagnitude < 1e-10f) n = s.v[id] - sr; n.Normalize();
                    if ((Vector3.Dot(n, s.v[id] - sr) < 0f) == zewn) n = -n; s.n[id] = n;
                    var tg = dS.sqrMagnitude > 1e-10f ? dS.normalized : Vector3.right; tg = (tg - n * Vector3.Dot(tg, n)).normalized; if (tg.sqrMagnitude < .5f) tg = Vector3.Cross(n, Vector3.up).normalized;
                    s.t[id] = new Vector4(tg.x, tg.y, tg.z, 1f);   // styczne wzdłuż kadłuba (mapa normalnych tekstury)
                }
            for (int i = 0; i < N; i++)
                for (int j = 0; j < M - 1; j++)
                {
                    int a = k0 + i * M + j, b = a + 1, c = a + M, d = c + 1; var ns = s.n[a] + s.n[b] + s.n[c] + s.n[d];
                    if (Vector3.Dot(Vector3.Cross(s.v[b] - s.v[a], s.v[d] - s.v[a]), ns) + Vector3.Dot(Vector3.Cross(s.v[d] - s.v[a], s.v[c] - s.v[a]), ns) >= 0f) s.i.AddRange(new[] { a, b, d, a, d, c }); else s.i.AddRange(new[] { a, d, b, a, c, d });
                }
        }
        for (int i = 0; i < N; i++)
            foreach (float t in new[] { -1f, 1f })
            {   // burta (relingi) z ciemnego drewna — listwa na krawędzi poszycia
                float u0 = (float)i / N, u1 = (float)(i + 1) / N;
                dr.Quad(Pt(u0, t, 0f) + Vector3.up * .004f, Pt(u1, t, 0f) + Vector3.up * .004f, Pt(u1, t, g) + Vector3.up * .004f, Pt(u0, t, g) + Vector3.up * .004f, Vector3.up);
            }
        var sR = Vector3.zero; for (int j = 0; j < M; j++) sR += Pt(0f, ts[j], 0f); sR /= M;
        for (int j = 0; j < M - 1; j++) { dk.Tri(sR, Pt(0f, ts[j], 0f), Pt(0f, ts[j + 1], 0f), Vector3.left); dk.Tri(sR + Vector3.right * g, Pt(0f, ts[j], g) + Vector3.right * g, Pt(0f, ts[j + 1], g) + Vector3.right * g, Vector3.right); }   // pawęż
        dr.Rura(Pt(1f, 0f, 0f) + new Vector3(-.02f, -.01f, 0f), Pt(1f, 1f, 0f) + new Vector3(0f, .03f, 0f), .025f, 8);         // dziobnica
        foreach (float x in new[] { -1.1f, .1f, 1.15f })
        {   // ławki (pawęże) na wspornikach
            float u = (x + L * .5f) / L, y = o.y + H + Sh(u) - .2f, b = 0f;
            for (float t = 0f; t <= 1.001f; t += .02f) { var P = Pt(u, t, g); if (P.y >= y - .03f) { b = P.z - .004f; break; } }   // szerokość wnętrza na wysokości ławki
            if (b > .05f) dk.Box(new Vector3(x - .13f, y - .03f, -b), new Vector3(x + .13f, y, b));
        }
        foreach (float sz in new[] { -1f, 1f })
        {   // dwa wiosła leżące na ławkach, dulki na burtach
            var a0 = new Vector3(1.25f, o.y + H - .165f, sz * .17f); var a1 = new Vector3(-1.35f, o.y + H - .165f, sz * .27f);   // pióra wioseł w szerokiej części rufowej (w obrysie łodzi)
            dr.Rura(a0, a1, .022f, 8); dk.OBox(a1 + (a1 - a0).normalized * .25f, (a1 - a0).normalized, Vector3.forward * sz, Vector3.up, .55f, .15f, .02f);
            float u = (.2f + L * .5f) / L; var dl = Pt(u, sz, 0f); q[stal].Rura(dl, dl + Vector3.up * .07f, .01f, 6);
        }
        foreach (float x in new[] { -1.3f, 1.2f })
        {   // kozły pod kilem
            float u = (x + L * .5f) / L, yk = Pt(u, 0f, 0f).y;
            dr.Box(new Vector3(x - .08f, .12f, -.35f), new Vector3(x + .08f, yk + .01f, .35f));
        }
    }

    /// <summary>Ładunek portowy: paleta, beczki z obręczami, skrzynie, zwoje liny — punkt = środek na posadzce.</summary>
    public static Transform Ladunek(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Ładunek portowy · " + nazwa, "muzeum_ladunek", BudujLadunek, p, przod, new Vector3(-1.05f, 0f, -.75f), new Vector3(1.05f, 1.2f, .75f));
    static void Beczka(BraxChlodnie.Partie q, Vector3 c, float h, float r)
    {
        var pr = new List<Vector3>(); for (int i = 0; i <= 8; i++) { float y = h * i / 8f; pr.Add(new Vector3(r * (.86f + .14f * Mathf.Sin(Mathf.PI * i / 8f)), y, 0)); }
        pr.Insert(0, new Vector3(0f, .015f, 0)); pr.Insert(1, new Vector3(r * .86f - .012f, .015f, 0)); pr.Insert(2, new Vector3(r * .86f - .012f, 0f, 0));
        pr.Add(new Vector3(r * .86f - .012f, h, 0)); pr.Add(new Vector3(r * .86f - .012f, h - .018f, 0)); pr.Add(new Vector3(0f, h - .018f, 0));
        BraxChlodnie.Toczony(q[drewno], c, pr, Bl, 18);
        foreach (float f in new[] { .1f, .3f, .7f, .9f })
        {   // obręcze
            float rr = r * (.86f + .14f * Mathf.Sin(Mathf.PI * f)); TocOs(q[zeliwo], c + Vector3.up * (h * f), Vector3.up, Torus(rr + .002f, .006f, .016f, 0f, 6), 18);
        }
    }
    static void BudujLadunek(BraxChlodnie.Partie q)
    {
        var dk = q[dab];
        for (int i = 0; i < 5; i++) dk.Box(new Vector3(-1f, .1f, -.7f + i * .34f), new Vector3(1f, .125f, -.7f + i * .34f + .09f));       // paleta: deski
        foreach (float x in new[] { -.95f, 0f, .95f }) dk.Box(new Vector3(x - .05f, 0f, -.7f), new Vector3(x + .05f, .1f, .7f));          // klocki
        Beczka(q, new Vector3(-.6f, .125f, -.32f), .82f, .3f); Beczka(q, new Vector3(-.6f, .125f, .32f), .82f, .3f);
        foreach (var (lo, hi) in new[] { (new Vector3(.05f, .125f, -.62f), new Vector3(.95f, .62f, -.02f)), (new Vector3(.1f, .125f, .05f), new Vector3(.9f, .5f, .62f)), (new Vector3(.2f, .62f, -.55f), new Vector3(.85f, 1.0f, -.08f)) })
        {   // skrzynie z listwami
            dk.Poducha(lo, hi, .012f, 0f, 3);
            foreach (float yy in new[] { lo.y + .06f, hi.y - .1f }) q[drewno].Box(new Vector3(lo.x - .025f, yy, lo.z - .025f), new Vector3(hi.x + .025f, yy + .04f, hi.z + .025f));
        }
        for (int k = 0; k < 4; k++) TocOs(q[lina], new Vector3(.5f, .5f + .022f, .33f), Vector3.up, Torus(.07f + k * .05f, .022f, .022f, 0f, 6), 20);   // zwój liny na skrzyni
    }

    /// <summary>Dzwon okrętowy na drewnianej ramie z linką: punkt = środek ramy na posadzce, przod = do oglądających.</summary>
    public static Transform Dzwon(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Dzwon okrętowy · " + nazwa, "muzeum_dzwon", BudujDzwon, p, przod, new Vector3(-.6f, 0f, -.4f), new Vector3(.6f, 1.95f, .4f));
    static void BudujDzwon(BraxChlodnie.Partie q)
    {
        var dr = q[drewno]; var dz = q[stal]; const float yd = 1.0f;
        dr.Poducha(new Vector3(-.6f, 0f, -.4f), new Vector3(.6f, .08f, .4f), .015f, 0f, 3);                             // podstawa
        foreach (float sx in new[] { -1f, 1f }) dr.Poducha(new Vector3(sx * .5f - .06f, .08f, -.06f), new Vector3(sx * .5f + .06f, 1.85f, .06f), .012f, 0f, 3);   // słupy
        dr.Poducha(new Vector3(-.6f, 1.85f, -.08f), new Vector3(.6f, 1.95f, .08f), .012f, 0f, 3);                        // belka
        dz.Rura(new Vector3(0f, 1.85f, 0f), new Vector3(0f, yd + .5f, 0f), .015f, 8);                                     // zawiesie
        BraxChlodnie.Toczony(dz, new Vector3(0f, yd, 0f), new List<Vector3> { new Vector3(0f, .4f, 0), new Vector3(.11f, .37f, 0), new Vector3(.15f, .2f, 0), new Vector3(.19f, .03f, 0), new Vector3(.215f, 0f, 0),
            new Vector3(.228f, .006f, 0), new Vector3(.205f, .045f, 0), new Vector3(.175f, .14f, 0), new Vector3(.15f, .26f, 0), new Vector3(.14f, .36f, 0), new Vector3(.12f, .43f, 0), new Vector3(.07f, .47f, 0), new Vector3(0f, .48f, 0) }, Bl, 32);   // dzwon (wnętrze i zewnątrz)
        TocOs(dz, new Vector3(0f, yd + .52f, 0f), Vector3.right, Torus(.04f, .012f, .012f, 0f, 6), 14);                   // ucho
        dz.Rura(new Vector3(0f, yd + .4f, 0f), new Vector3(0f, yd + .06f, 0f), .01f, 6); BraxChlodnie.Toczony(dz, new Vector3(0f, yd + .05f, 0f), KulaProfil(.035f), Bl, 12);   // serce
        q[plotno].Rura(new Vector3(0f, yd + .02f, 0f), new Vector3(0f, yd - .42f, 0f), .011f, 8);                         // linka dzwonu z frędzlem
        BraxChlodnie.Toczony(q[plotno], new Vector3(0f, yd - .52f, 0f), new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.035f, .01f, 0), new Vector3(.02f, .1f, 0), new Vector3(.012f, .11f, 0) }, Bl, 10);
    }

    /// <summary>Luneta na drewnianym trójnogu: punkt = środek na posadzce, przod = kierunek obiektywu.</summary>
    public static Transform Luneta(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Luneta · " + nazwa, "muzeum_luneta", BudujLunete, p, przod, new Vector3(-.5f, 0f, -.6f), new Vector3(.5f, 1.55f, .75f));
    static void Trojnog(BraxChlodnie.Partie q, float h, float rozstaw)
    {
        for (int i = 0; i < 3; i++)
        {
            float a = (90f + i * 120f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            q[drewno].Rura(d * rozstaw, Vector3.up * h + d * .05f, .022f, 8); q[stal].Rura(d * rozstaw - Vector3.up * .001f, d * rozstaw + Vector3.up * .04f, .025f, 8);
        }
        BraxChlodnie.Toczony(q[stal], Vector3.up * (h - .02f), new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.08f, 0f, 0), new Vector3(.08f, .05f, 0), new Vector3(0f, .05f, 0) }, Bl, 16);
    }
    static void BudujLunete(BraxChlodnie.Partie q)
    {
        Trojnog(q, 1.2f, .45f);
        q[stal].Rura(new Vector3(0f, 1.23f, 0f), new Vector3(0f, 1.32f, 0f), .025f, 10);
        var os = new Vector3(0f, .26f, 1f).normalized; var c0 = new Vector3(0f, 1.36f, 0f) - os * .45f;
        TocOs(q[czern], c0, os, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.022f, 0f, 0), new Vector3(.022f, .12f, 0), new Vector3(.03f, .14f, 0), new Vector3(.034f, .4f, 0), new Vector3(.042f, .42f, 0),
            new Vector3(.045f, .75f, 0), new Vector3(.055f, .77f, 0), new Vector3(.058f, 1.05f, 0), new Vector3(.064f, 1.07f, 0), new Vector3(.064f, 1.15f, 0), new Vector3(.05f, 1.15f, 0), new Vector3(.05f, 1.08f, 0), new Vector3(0f, 1.08f, 0) }, 20);
        foreach (float t in new[] { .14f, .42f, .77f, 1.07f }) TocOs(q[stal], c0 + os * t, os, Torus(t < .3f ? .031f : t < .6f ? .043f : t < .9f ? .056f : .065f, .004f, .006f, 0f, 6), 20);   // pierścienie ze stali
        q[stal].Rura(new Vector3(0f, 1.32f, 0f), c0 + os * .5f - Vector3.up * .04f, .02f, 8);                              // obejma
    }

    /// <summary>Kamera filmowa na statywie (dwie szpule, obiektyw, korba): punkt = środek na posadzce, przod = obiektyw.</summary>
    public static Transform Kamera(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Kamera filmowa · " + nazwa, "muzeum_kamera", BudujKamere, p, przod, new Vector3(-.5f, 0f, -.5f), new Vector3(.5f, 1.85f, .6f));
    static void BudujKamere(BraxChlodnie.Partie q)
    {
        Trojnog(q, 1.15f, .45f);
        var dr = q[drewno]; var cz = q[czern]; var st = q[stal];
        dr.Poducha(new Vector3(-.13f, 1.2f, -.22f), new Vector3(.13f, 1.52f, .2f), .012f, 0f, 3);                         // korpus (drewno)
        cz.Box(new Vector3(-.14f, 1.17f, -.15f), new Vector3(.14f, 1.2f, .15f));                                           // głowica
        TocOs(st, new Vector3(0f, 1.38f, .2f), Vector3.forward, new List<Vector3> { new Vector3(.05f, 0f, 0), new Vector3(.05f, .04f, 0), new Vector3(.04f, .05f, 0), new Vector3(.04f, .14f, 0), new Vector3(.045f, .15f, 0), new Vector3(.03f, .155f, 0), new Vector3(0f, .155f, 0) }, 18);   // obiektyw
        foreach (float zz in new[] { -.12f, .1f })
        {   // szpule na górze
            TocOs(cz, new Vector3(-.03f, 1.66f, zz), Vector3.right, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.15f, 0f, 0), new Vector3(.15f, .06f, 0), new Vector3(0f, .06f, 0) }, 24);
            st.Rura(new Vector3(-.06f, 1.66f, zz), new Vector3(.06f, 1.66f, zz), .012f, 8);   // oś szpuli (≥ 3 cm poza tarczą)
        }
        st.Rura(new Vector3(.13f, 1.36f, -.05f), new Vector3(.2f, 1.36f, -.05f), .008f, 6); st.Rura(new Vector3(.2f, 1.36f, -.05f), new Vector3(.2f, 1.28f, -.05f), .007f, 6); cz.Rura(new Vector3(.2f, 1.28f, -.05f), new Vector3(.25f, 1.28f, -.05f), .012f, 8);   // korba
    }

    /// <summary>Maszyna parowa (kocioł z kominem, cylinder, korbowód, koło zamachowe, regulator) na żeliwnej ramie i ceglanym
    /// fundamencie: punkt = środek na posadzce, przod = bok maszyny do oglądających.</summary>
    public static Transform MaszynaParowa(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Maszyna parowa · " + nazwa, "muzeum_maszyna_parowa", BudujMaszyne, p, przod, new Vector3(-1.75f, 0f, -.95f), new Vector3(2.15f, 2.95f, .95f));
    static void BudujMaszyne(BraxChlodnie.Partie q)
    {
        var fe = q[zeliwo]; var st = q[stal]; var zi = q[zielen]; var cz = q[tuba];
        q[marmur].Poducha(new Vector3(-1.7f, 0f, -.9f), new Vector3(2.1f, .25f, .9f), .02f, 0f, 3);                      // fundament
        fe.Box(new Vector3(-1.6f, .25f, -.5f), new Vector3(2.0f, .4f, .5f));                                               // rama
        // kocioł poziomy (oś x) z pasami i paleniskiem, komin
        TocOs(zi, new Vector3(-1.45f, .95f, -.1f), Vector3.right, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.38f, 0f, 0), new Vector3(.42f, .04f, 0), new Vector3(.42f, 1.9f, 0), new Vector3(.38f, 1.94f, 0), new Vector3(.2f, 1.96f, 0), new Vector3(0f, 1.97f, 0) }, 28);
        foreach (float t in new[] { .25f, .7f, 1.15f, 1.6f }) TocOs(st, new Vector3(-1.45f + t, .95f, -.1f), Vector3.right, Torus(.424f, .012f, .02f, 0f, 6), 28);
        foreach (float x in new[] { -1.2f, -.05f }) fe.Box(new Vector3(x - .08f, .4f, -.42f), new Vector3(x + .08f, .6f, .22f));   // siodła kotła (między pasami)
        fe.Poducha(new Vector3(-1.62f, .4f, -.42f), new Vector3(-1.45f, 1.25f, .22f), .015f, 0f, 3);                       // drzwiczki paleniska (czoło)
        fe.Rura(new Vector3(-1.2f, 1.3f, -.1f), new Vector3(-1.2f, 2.75f, -.1f), .11f, 16);                                 // komin
        TocOs(fe, new Vector3(-1.2f, 2.75f, -.1f), Vector3.up, new List<Vector3> { new Vector3(.11f, 0f, 0), new Vector3(.17f, .12f, 0), new Vector3(.17f, .16f, 0), new Vector3(.09f, .16f, 0), new Vector3(.09f, .02f, 0) }, 16);   // korona komina
        TocOs(st, new Vector3(-.3f, 1.35f, -.1f), Vector3.up, new List<Vector3> { new Vector3(.07f, 0f, 0), new Vector3(.07f, .1f, 0), new Vector3(.1f, .12f, 0), new Vector3(.1f, .2f, 0), new Vector3(.06f, .26f, 0), new Vector3(0f, .27f, 0) }, 16);   // dzwon parowy (kopuła)
        // cylinder (oś x) na kotle, tłoczysko, wodzik, korbowód, wał z kołem zamachowym (oś z) i regulator odśrodkowy
        TocOs(fe, new Vector3(.25f, 1.5f, -.1f), Vector3.right, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.13f, 0f, 0), new Vector3(.15f, .02f, 0), new Vector3(.15f, .48f, 0), new Vector3(.13f, .5f, 0), new Vector3(0f, .5f, 0) }, 20);
        st.Rura(new Vector3(.75f, 1.5f, -.1f), new Vector3(1.15f, 1.5f, -.1f), .02f, 10);                                 // tłoczysko
        st.Box(new Vector3(1.1f, 1.45f, -.16f), new Vector3(1.22f, 1.55f, -.04f));                                         // wodzik
        st.Box(new Vector3(.75f, 1.41f, -.15f), new Vector3(1.3f, 1.43f, -.05f));                                          // prowadnica wodzika
        fe.Box(new Vector3(1.22f, .4f, -.14f), new Vector3(1.3f, 1.41f, -.06f));                                          // wspornik prowadnicy
        var kc = new Vector3(1.38f, 1.05f, -.1f); var czop = kc + new Vector3(.18f, .12f, 0f);
        st.Rura(new Vector3(1.18f, 1.5f, -.1f), czop, .022f, 10);                                                          // korbowód
        TocOs(st, kc + new Vector3(0f, 0f, -.12f), Vector3.forward, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.23f, 0f, 0), new Vector3(.23f, .04f, 0), new Vector3(0f, .04f, 0) }, 24);   // tarcza korby
        fe.Box(new Vector3(1.3f, .4f, -.18f), new Vector3(1.46f, kc.y - .05f, -.02f));                                    // łożysko wału
        st.Rura(kc + new Vector3(0f, 0f, -.15f), kc + new Vector3(0f, 0f, .55f), .045f, 12);                               // wał
        var kz = kc + new Vector3(0f, 0f, .4f);                                                                            // koło zamachowe
        TocOs(cz, kz, Vector3.forward, new List<Vector3> { new Vector3(.52f, -.05f, 0), new Vector3(.62f, -.05f, 0), new Vector3(.64f, -.03f, 0), new Vector3(.64f, .03f, 0), new Vector3(.62f, .05f, 0), new Vector3(.52f, .05f, 0), new Vector3(.52f, -.05f, 0) }, 40);
        TocOs(cz, kz, Vector3.forward, new List<Vector3> { new Vector3(0f, -.06f, 0), new Vector3(.1f, -.06f, 0), new Vector3(.1f, .06f, 0), new Vector3(0f, .06f, 0) }, 20);
        for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f); cz.Rura(kz + d * .09f, kz + d * .53f, .025f, 8); }
        st.Rura(kz + new Vector3(0f, 0f, .06f), kz + new Vector3(0f, 0f, .09f), .05f, 12);
        var rg = new Vector3(.9f, 1.7f, -.1f); st.Rura(new Vector3(.9f, 1.6f, -.1f), rg + Vector3.up * .3f, .015f, 8);      // regulator Watta
        foreach (float sx in new[] { -1f, 1f }) { var kul = rg + new Vector3(sx * .14f, .1f, 0f); st.Rura(rg + Vector3.up * .28f, kul, .006f, 6); BraxChlodnie.Toczony(st, kul, KulaProfil(.035f), Bl, 12); }
    }

    /// <summary>Rzeźba współczesna na postumencie: 0 = pierścień ze stali, 1 = spirala z płyt marmuru, 2 = trzy kule, 3 = łuk
    /// (grafit). Punkt = środek postumentu na posadzce.</summary>
    public static Transform Rzezba(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int wariant)
    {
        wariant = ((wariant % 4) + 4) % 4; int w = wariant;
        return Postaw2(rodzic, "Rzeźba · " + nazwa, "muzeum_rzezba_" + w, q => BudujRzezbe(q, w), p, przod, new Vector3(-.45f, 0f, -.45f), new Vector3(.45f, 2.2f, .45f));
    }
    static void BudujRzezbe(BraxChlodnie.Partie q, int w)
    {
        const float hp = .55f; q[czern].Poducha(new Vector3(-.42f, 0f, -.42f), new Vector3(.42f, hp, .42f), .012f, 0f, 3);   // postument (grafit)
        if (w == 0)
        {   // pierścień pionowy ze stali polerowanej na krótkim trzpieniu
            q[stal].Rura(new Vector3(0f, hp, 0f), new Vector3(0f, hp + .1f, 0f), .04f, 12);
            TocOs(q[stal], new Vector3(0f, hp + .1f + .62f, 0f), Vector3.forward, Torus(.5f, .12f, .12f, 0f, 14), 48);
        }
        else if (w == 1)
        {   // spirala: 16 płyt marmuru, każda obrócona o 11°
            for (int i = 0; i < 16; i++)
            {
                float y = hp + i * .085f + .0425f, a = i * 11f * Mathf.Deg2Rad; var r = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var f = Vector3.Cross(r, Vector3.up);
                q[marmur].OBox(new Vector3(0f, y, 0f), r, Vector3.up, f, .72f, .085f, .26f);   // płyty stykają się (bez szczelin)
            }
        }
        else if (w == 2)
        {   // trzy kule w równowadze: grafit, stal, marmur
            BraxChlodnie.Toczony(q[czern], new Vector3(0f, hp + .3f, 0f), KulaProfil(.3f, 0f, 14), Bl, 28);
            BraxChlodnie.Toczony(q[stal], new Vector3(.04f, hp + .6f + .2f, 0f), KulaProfil(.2f, 0f, 12), Bl, 24);
            BraxChlodnie.Toczony(q[marmur], new Vector3(-.02f, hp + 1.0f + .12f, 0f), KulaProfil(.12f, 0f, 10), Bl, 20);
        }
        else
        {   // łuk: pół torusa z grafitu, stojący
            for (int i = 0; i < 16; i++)
            {   // segmenty łuku (rury) po półokręgu w płaszczyźnie xy
                float a0 = i * Mathf.PI / 16f, a1 = (i + 1) * Mathf.PI / 16f;
                q[czern].Rura(new Vector3(Mathf.Cos(a0) * .55f, hp + Mathf.Sin(a0) * .55f * 1.6f, 0f), new Vector3(Mathf.Cos(a1) * .55f, hp + Mathf.Sin(a1) * .55f * 1.6f, 0f), .07f, 14);
            }
        }
    }

    /// <summary>Trzy amfory (do gabloty): punkt = środek dna gabloty.</summary>
    public static Transform Amfory(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Amfory · " + nazwa, "muzeum_amfory", BudujAmfory, p, przod, Vector3.zero, Vector3.zero);
    static void BudujAmfory(BraxChlodnie.Partie q)
    {
        foreach (var (x, s) in new[] { (-.22f, .8f), (0f, 1f), (.22f, .75f) })
        {
            float h = .5f * s, r = .08f * s; var c = new Vector3(x, 0f, 0f);
            BraxChlodnie.Toczony(q[czern], c, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.045f * s, 0f, 0), new Vector3(.045f * s, .025f * s, 0), new Vector3(0f, .025f * s, 0) }, Bl, 16);   // stojak
            BraxChlodnie.Toczony(q[terakota], c + Vector3.up * .025f * s, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.02f * s, 0f, 0), new Vector3(.025f * s, .04f * s, 0), new Vector3(r * .7f, h * .25f, 0), new Vector3(r, h * .5f, 0),
                new Vector3(r * .85f, h * .72f, 0), new Vector3(r * .35f, h * .85f, 0), new Vector3(r * .3f, h * .97f, 0), new Vector3(r * .42f, h, 0), new Vector3(r * .3f, h, 0), new Vector3(r * .26f, h * .95f, 0) }, Bl, 20);
            foreach (float sz in new[] { -1f, 1f })
                for (int i = 0; i < 5; i++)
                {   // ucha
                    float a0 = i * Mathf.PI / 5f, a1 = (i + 1) * Mathf.PI / 5f; var b0 = c + Vector3.up * (.025f * s + h * .8f);
                    Vector3 P(float a) => b0 + new Vector3(0f, Mathf.Sin(a) * h * .1f, sz * (r * .35f + (1f - Mathf.Cos(a)) * r * .28f));
                    q[terakota].Rura(P(a0), P(a1), .008f * s, 6);
                }
        }
    }

    /// <summary>Lampy naftowe (trzy, do gabloty): punkt = środek dna gabloty.</summary>
    public static Transform LampyNaftowe(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
        => Postaw2(rodzic, "Lampy naftowe · " + nazwa, "muzeum_lampy", BudujLampy, p, przod, Vector3.zero, Vector3.zero);
    static void BudujLampy(BraxChlodnie.Partie q)
    {
        foreach (var (x, s) in new[] { (-.22f, .85f), (0f, 1f), (.22f, .9f) })
        {
            var c = new Vector3(x, 0f, 0f);
            BraxChlodnie.Toczony(q[talerz], c, new List<Vector3> { new Vector3(0f, 0f, 0), new Vector3(.07f * s, 0f, 0), new Vector3(.07f * s, .02f * s, 0), new Vector3(.03f * s, .05f * s, 0), new Vector3(.03f * s, .1f * s, 0), new Vector3(.075f * s, .16f * s, 0), new Vector3(.06f * s, .21f * s, 0), new Vector3(.025f * s, .23f * s, 0) },
                new[] { C(.25f, .32f, .45f) }, 20);   // podstawa i zbiornik (szkło barwione)
            BraxChlodnie.Toczony(q[stal], c + Vector3.up * .23f * s, new List<Vector3> { new Vector3(.025f * s, 0f, 0), new Vector3(.04f * s, .02f * s, 0), new Vector3(.04f * s, .045f * s, 0), new Vector3(.03f * s, .05f * s, 0) }, Bl, 16);   // palnik
            BraxChlodnie.Toczony(q[szklo], c + Vector3.up * .28f * s, new List<Vector3> { new Vector3(.03f * s, 0f, 0), new Vector3(.045f * s, .05f * s, 0), new Vector3(.025f * s, .12f * s, 0), new Vector3(.02f * s, .2f * s, 0) }, Bl, 16);   // klosz szklany
        }
    }

    /// <summary>Szatnia: lada z blatem, dwa wieszaki z płaszczami, napis 3D SZATNIA na ścianie — punkt = środek przy ścianie (lico),
    /// przod = do sali, dl = długość lady (m rzeczywiste).</summary>
    public static Transform Szatnia(Transform rodzic, string nazwa, Vector3 punktSciany, Vector3 przod)
    {
        var g = Postaw2(rodzic, "Szatnia · " + nazwa, "muzeum_szatnia", BudujSzatnie, punktSciany, przod, new Vector3(-1.8f, 0f, 1.45f), new Vector3(1.8f, 1.1f, 1.95f));
        BraxRegionMeble.Napis(rodzic, "SZATNIA", g.position + g.rotation * (new Vector3(0f, 2.55f, 0f) * K), g.forward, .26f * K / .72f, new Color(.13f, .13f, .14f), .05f);
        return g;
    }
    static void BudujSzatnie(BraxChlodnie.Partie q)
    {
        q[dab].Poducha(new Vector3(-1.8f, 0f, 1.45f), new Vector3(1.8f, 1.02f, 1.9f), .012f, 0f, 3);                       // lada
        q[marmur].Poducha(new Vector3(-1.84f, 1.02f, 1.42f), new Vector3(1.84f, 1.06f, 1.95f), .01f, 0f, 3);                // blat
        Color32[] kol = { C(.12f, .16f, .3f), C(.35f, .35f, .37f), C(.6f, .5f, .38f), C(.08f, .08f, .09f), C(.4f, .12f, .12f), C(.2f, .3f, .22f) };
        foreach (float zr in new[] { .45f })
        {
            foreach (float sx in new[] { -1.6f, 1.6f }) q[stal].Rura(new Vector3(sx, 0f, zr), new Vector3(sx, 1.75f, zr), .02f, 10);
            q[stal].Rura(new Vector3(-1.6f, 1.72f, zr), new Vector3(1.6f, 1.72f, zr), .016f, 10);                          // drążek
            for (int i = 0; i < 9; i++)
            {   // wieszaki z płaszczami
                float x = -1.4f + i * .35f; var pl = q[plaszcze]; pl.kolor = kol[i % kol.Length];
                q[stal].Rura(new Vector3(x, 1.72f, zr), new Vector3(x, 1.64f, zr), .004f, 4);
                pl.Poducha(new Vector3(x - .2f, .72f + (i % 3) * .06f, zr - .1f), new Vector3(x + .2f, 1.62f, zr + .1f), .06f, .01f, 4); pl.kolor = Bl[0];
            }
        }
    }

    /// <summary>Sklep muzealny: dwa regały z książkami pod ścianą i stół z wydawnictwami, napis 3D SKLEP MUZEALNY — punkt = środek
    /// przy ścianie (lico), przod = do sali.</summary>
    public static Transform Sklepik(Transform rodzic, string nazwa, Vector3 punktSciany, Vector3 przod)
    {
        var g = Postaw2(rodzic, "Sklep muzealny · " + nazwa, "muzeum_sklepik", BudujSklepik, punktSciany, przod, new Vector3(-2.3f, 0f, 0f), new Vector3(2.3f, 2.2f, .42f));
        BraxChlodnie.Kolizja(g, "stół sklepu", new Vector3(-.8f, 0f, 1.6f), new Vector3(.8f, .78f, 2.4f));
        BraxRegionMeble.Napis(rodzic, "SKLEP MUZEALNY", g.position + g.rotation * (new Vector3(0f, 2.55f, 0f) * K), g.forward, .22f * K / .72f, new Color(.13f, .13f, .14f), .05f);
        return g;
    }
    static void BudujSklepik(BraxChlodnie.Partie q)
    {
        var dk = q[dab]; var ks = q[ksiazki]; var rnd = new System.Random(11);
        Color32[] kol = { C(.45f, .1f, .1f), C(.1f, .2f, .4f), C(.15f, .3f, .2f), C(.6f, .55f, .45f), C(.1f, .1f, .12f), C(.5f, .3f, .15f), C(.75f, .72f, .65f) };
        foreach (float xs in new[] { -1.15f, 1.15f })
        {
            dk.Box(new Vector3(xs - 1.1f, 0f, .02f), new Vector3(xs - 1.07f, 2.1f, .4f)); dk.Box(new Vector3(xs + 1.07f, 0f, .02f), new Vector3(xs + 1.1f, 2.1f, .4f));   // boki
            dk.Box(new Vector3(xs - 1.07f, 0f, .02f), new Vector3(xs + 1.07f, 2.1f, .04f));                                // plecy
            for (int l = 0; l < 6; l++)
            {
                float y = .05f + l * .4f; dk.Box(new Vector3(xs - 1.07f, y - .025f, .04f), new Vector3(xs + 1.07f, y, .4f));       // półka
                if (l == 5) continue;
                float x = xs - 1.04f;
                while (x < xs + .95f)
                {   // książki: grzbiety od sali, różne wysokości i kolory
                    float w = .025f + (float)rnd.NextDouble() * .035f, h = .2f + (float)rnd.NextDouble() * .1f; ks.kolor = kol[rnd.Next(kol.Length)];
                    ks.Box(new Vector3(x, y, .07f), new Vector3(x + w - .002f, y + h, .07f + .14f + (float)rnd.NextDouble() * .06f)); x += w;
                }
            }
        }
        dk.Poducha(new Vector3(-.8f, 0f, 1.6f), new Vector3(.8f, .74f, 2.4f), .012f, 0f, 3);                                // stół
        for (int i = 0; i < 8; i++)
        {   // stosy książek na stole
            float x = -.6f + (i % 4) * .4f, z = 1.8f + (i / 4) * .4f; int n = 2 + rnd.Next(3); float y = .74f;
            for (int j = 0; j < n; j++) { ks.kolor = kol[rnd.Next(kol.Length)]; float th = .03f + (float)rnd.NextDouble() * .02f; ks.Box(new Vector3(x - .11f, y, z - .14f), new Vector3(x + .11f, y + th, z + .14f)); y += th + .002f; }
        }
        ks.kolor = Bl[0];
    }
}

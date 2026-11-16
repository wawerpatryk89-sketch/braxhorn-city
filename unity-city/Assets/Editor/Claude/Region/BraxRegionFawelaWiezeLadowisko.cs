using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// LĄDOWISKO DLA HELIKOPTERA NA DACHU WIEŻOWCA C5_U04 (wieża szklana przy promenadzie, 148 m) — właściciel 16.11.2026 22:00: „Na dachu
// wieżowca dla helikoptera miejsce. I będziemy latać z miasta do miasta helikopterem. Zrób też miejsce zajebiste i wejście na dach.”
// (w Mieście Mistrzów lądowisko stoi na BRAXHORN TOWER T09). Całość z kodu, z prawdziwych materiałów PBR:
//  • HOL: portal od promenady to prawdziwe drzwi automatyczne; hol 10 × 16 m (marmur, okleina tekowa, granit przy windzie, linie LED
//    w suficie), recepcja, strefa oczekiwania, kaseton „LĄDOWISKO · DACH”, winda HOL ↔ DACH (BraxWinda — ta sama co w biurowcach Tyłów B).
//  • DACH: taras z płyt gresowych na wspornikach, szklany pawilon odlotów wokół szybu windy, nad nim płyta lądowiska na stalowej
//    konstrukcji (D = 16 m, płyta 17 × 17 m, półka bezpieczeństwa 1.5 m, wysokość 4.4 m nad tarasem — nad attyką i pawilonem),
//    oznakowanie W TEKSTURZE płyty (obwód TLOF, żółty okrąg przyziemienia, białe H, masa 6.4 t, D 16 — wzór CAP 437 / ICAO Annex 14
//    t. II), zielone lampy obwodu, naświetlacze, rękaw wiatrowy w pasy, dwoje schodów stalowych z kratą i poręczami.
//  • GRA: AKCJA „LOT HELIKOPTEREM” na płycie (pod loty między miastami), punkty świata R1_LADOWISKO_*.
// Migotanie: każda bryła ≥ 2 cm od równoległych płaszczyzn innych brył, obraz płyty jako jej lico (nie warstwa nad blachą).
public static partial class BraxRegionFawela
{
    internal const string LadowiskoWieza = "C5_U04";
    internal const int LadowiskoStrona = 0;                                    // portal od promenady (+z)
    const float HolGl = 16f, PlytaB = 17f, Polka = 1.5f, PlytaNad = 4.4f, PawW = 9f;
    internal static bool ladowiskoZbudowane;
    internal static string ladowiskoOpis = "";

    // środek szybu windy lądowiska za ścianą tylną holu (drzwi windy w ścianie tylnej, przodem do portalu)
    static Vector2 SzybLadowiska(LokalPlan hol)
    {
        var h = BraxRegionTylyB.SzybPolowy; var c = hol.F.P(hol.sc, 0f, hol.tyl + h.y);
        return new Vector2(c.x, c.z);
    }
    static Rect SzybRect(Vector2 s) { var h = BraxRegionTylyB.SzybPolowy; return Rect.MinMaxRect(s.x - h.x, s.y - h.y, s.x + h.x, s.y + h.y); }

    // czworokąt o zadanej normalnej (kolejność wierzchołków dobierana — bez zgadywania nawinięcia)
    static void QuadN(Bud b, Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 n)
    {
        if (Vector3.Dot(Vector3.Cross(bb - a, c - a), n) >= 0f) b.Quad(a, bb, c, d); else b.Quad(d, c, bb, a);
    }
    static void QuadUVN(Bud b, Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 n)
    {   // a = (u0,v0), bb = (u1,v0), c = (u1,v1), d = (u0,v1)
        if (Vector3.Dot(Vector3.Cross(bb - a, c - a), n) >= 0f) b.QuadUV(a, bb, c, d, new Rect(0f, 0f, 1f, 1f));
        else b.QuadUV(bb, a, d, c, new Rect(0f, 0f, 1f, 1f), true);
    }

    static void BoxMM(Bud b, Vector3 a, Vector3 c, bool dno = false) => b.Box(Vector3.Min(a, c), Vector3.Max(a, c), dno);

    static GameObject Wyrob(Bud b, string nazwa, Transform rodzic, Material m, bool kolizja = false, bool cienie = true) => b.Obiekt(nazwa, rodzic, m, cienie, kolizja);

    // tekstura z napisami (kaseton, ekran odlotów): tło, wiersze (tekst, wysokość linii bazowej 0–1, wysokość wersalików 0–1, kolor)
    static readonly Dictionary<string, Material> ladTex = new Dictionary<string, Material>();
    static Material NapisyTekstura(string plik, int W, int H, Color tlo, (string t, float y, float cap, Color c)[] wiersze, float swiecenie)
    {
        if (ladTex.TryGetValue(plik, out var mm) && mm) return mm;
        var px = new Color[W * H]; for (int i = 0; i < px.Length; i++) px[i] = tlo;
        foreach (var (t, y, cap, c) in wiersze)
        {
            float hc = cap * H, szer = BraxUliceKit.SzerokoscNapisu(t, 1f); if (szer > .01f) hc = Mathf.Min(hc, W * .92f / szer);
            BraxUliceKit.NapisWTeksturze(px, W, H, t, W * .5f, y * H, hc, c);
        }
        return ladTex[plik] = ZapiszTeksture(plik, W, H, px, swiecenie, 1024);
    }
    static Material ZapiszTeksture(string plik, int W, int H, Color[] px, float swiecenie, int maks)
    {
        var tx = new Texture2D(W, H, TextureFormat.RGB24, false); tx.SetPixels(px); tx.Apply();
        string dir = BraxUliceKit.Out + "/Ekrany"; System.IO.Directory.CreateDirectory(dir);
        string path = dir + "/" + plik + ".png";
        System.IO.File.WriteAllBytes(path, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = maks; imp.mipmapEnabled = true; imp.anisoLevel = 8; imp.SaveAndReimport(); }
        var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = swiecenie > 0f ? BraxMiastoA.Swieci("Lądowisko: " + plik, Color.white, Color.white * swiecenie) : BraxCityMaterials.Surface("Lądowisko: " + plik, Color.white, .8f);
        if (t2) { m.SetTexture("_MainTex", t2); if (swiecenie > 0f && m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", t2); m.SetColor("_EmissionColor", Color.white * swiecenie); } }
        EditorUtility.SetDirty(m);
        return m;
    }

    // płyta lądowiska: farba antypoślizgowa (ciemna zieleń szara), styki profili aluminiowych co 0.3 m, obwód TLOF biały 0.3 m,
    // okrąg przyziemienia żółty (średnica wewn. 0.5 D, szer. 1 m), białe H 4 × 3 m (kreska 0.75 m), masa „6.4 t” i „D 16”
    static Material TeksturaPlyty()
    {
        const string plik = "ladowisko_plyta_D16";
        if (ladTex.TryGetValue(plik, out var mm) && mm) return mm;
        int N = 2048; float ppm = N / PlytaB; var px = new Color[N * N];
        var tlo = new Color(.20f, .25f, .23f); var bial = new Color(.93f, .93f, .9f); var zolt = new Color(.98f, .78f, .12f);
        var rnd = new System.Random(16);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float sz = (float)rnd.NextDouble() * .035f - .0175f;                                 // ziarno farby antypoślizgowej
                var c = tlo + new Color(sz, sz, sz);
                if ((int)(y / ppm / .3f) != (int)((y + 1) / ppm / .3f)) c *= .72f;                   // styk profili (2 px)
                px[y * N + x] = c;
            }
        void Prost(float x0, float y0, float x1, float y1, Color c)
        {
            int a0 = Mathf.Clamp((int)(x0 * ppm), 0, N - 1), a1 = Mathf.Clamp((int)(x1 * ppm), 0, N), b0 = Mathf.Clamp((int)(y0 * ppm), 0, N - 1), b1 = Mathf.Clamp((int)(y1 * ppm), 0, N);
            for (int y = b0; y < b1; y++) for (int x = a0; x < a1; x++) px[y * N + x] = c;
        }
        float B = PlytaB, m = .3f, cx = B * .5f;
        Prost(0, 0, B, m, bial); Prost(0, B - m, B, B, bial); Prost(0, 0, m, B, bial); Prost(B - m, 0, B, B, bial);           // obwód TLOF
        float rIn = 4f, rOut = 5f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + .5f) / ppm - cx, dy = (y + .5f) / ppm - cx, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r >= rIn && r <= rOut) px[y * N + x] = Color.Lerp(px[y * N + x], zolt, Mathf.Clamp01(Mathf.Min(r - rIn, rOut - r) * ppm));
            }
        float hW = 3f, hH = 4f, k = .75f;
        Prost(cx - hW * .5f, cx - hH * .5f, cx - hW * .5f + k, cx + hH * .5f, bial); Prost(cx + hW * .5f - k, cx - hH * .5f, cx + hW * .5f, cx + hH * .5f, bial);
        Prost(cx - hW * .5f + k, cx - k * .5f, cx + hW * .5f - k, cx + k * .5f, bial);
        BraxUliceKit.NapisWTeksturze(px, N, N, "6.4 t", N * .5f, (B - m - 1.6f) * ppm, .9f * ppm, bial);
        BraxUliceKit.NapisWTeksturze(px, N, N, "D 16", N * .5f, (m + .7f) * ppm, .7f * ppm, bial);
        return ladTex[plik] = ZapiszTeksture(plik, N, N, px, 0f, 2048);
    }

    // ============================================================================ HOL
    static void ZbudujHol(Transform korzen, string id, LokalPlan p, float yBase, float yDach, float gl)
    {
        var F = p.F; float k = K;
        var hr = new GameObject("Hol lądowiska (" + id + ")").transform; hr.SetParent(korzen, false); hr.position = Vector3.zero;
        float sA = p.s0 + LokOds, sB = p.s1 - LokOds, dT = p.tyl, y0 = yBase, yS = yBase + LokSufit;
        var szyb = SzybLadowiska(p); var hs = BraxRegionTylyB.SzybPolowy;
        float sS0 = p.sc - hs.x, sS1 = p.sc + hs.x;                                           // szerokość szybu w ścianie tylnej
        Vector3 P(float s, float y, float d) => F.P(s, y, d);
        var nIn = -F.N; var R = F.R;
        // drzwi automatyczne przesuwne w portalu (ta sama rama co w lokalach)
        BraxDrzwiLokalu.Podwojne(hr, "R1_WIEZA_" + id + "_HOL", "HOL — LĄDOWISKO", F.P(p.sc, yBase, 0f), F.N, LokDrzwiW, LokDrzwiH, gl, .025f, BraxDrzwiLokalu.Przesuwne);
        // NAPIS NAD WEJŚCIEM: litery przestrzenne podświetlane na tablicy montażowej (aluminium grafit) stojącej na daszku portalu
        // (daszek: PortalWiezy, 14 cm, wysięg 1.9 m; tablica w obrębie cięgien daszka; ogonek Ą wisi przed tablicą, nie w daszku)
        if (p.qd - p.q0 >= 0 && p.qd - p.q0 < p.otw.Count)
        {
            var od = p.otw[p.qd - p.q0]; float yk = od.yMax + .72f + .14f, szer = od.width + .9f;
            const string napis = "LĄDOWISKO · BRAXHORN HELI";
            float cap = Mathf.Min(.3f, szer / Mathf.Max(.01f, BraxUliceKit.SzerokoscNapisu(napis, 1f))), wN = BraxUliceKit.SzerokoscNapisu(napis, cap), yb = yk + .1f;
            BraxUliceKit.UstawGrupe("Szyld HOL LĄDOWISKA (" + id + ")", 160f);
            var lico = BraxMiastoA.Swieci("lądowisko: litery nad wejściem (podświetlenie)", new Color(.97f, .97f, .95f), new Color(1.35f, 1.35f, 1.3f));
            var boki = BraxUliceKit.Mat("litery przestrzenne: boki grafit", new Color(.06f, .06f, .065f), .5f, .4f);
            BraxUliceKit.S(boki).Box(F.P(od.center.x - wN * .5f - .1f, yk, -1.56f), F.P(od.center.x + wN * .5f + .1f, yb + cap + .06f, -1.60f));   // tablica 4 cm
            BraxUliceKit.Napis3D(napis, F.P(od.center.x, yb, -1.60f), F.N, cap, .06f, lico, boki);
        }
        // materiały
        var marmur = T("hol lądowiska: posadzka — marmur polerowany", 1.6f, new Color(.95f, .94f, .92f), .18f, "marble_01", "floor_tiles_06");
        var tek = T("hol lądowiska: okleina tekowa (panele ścienne)", 1.4f, new Color(.86f, .74f, .62f), .45f, "teak_veneer", "oak_veneer_01");
        var granit = T("hol lądowiska: granit grafitowy polerowany (ściana windy, blat)", 1.6f, new Color(.55f, .55f, .56f), .2f, "granite_tile", "granite_tile_04");
        var gladz = T("hol lądowiska: sufit — gładź biała", 3f, new Color(1f, 1f, .99f), .95f, "white_stucco", "white_plaster_02");
        var tynk = T("hol lądowiska: ściany — tynk gładki", 3f, new Color(.97f, .96f, .94f), .9f, "white_stucco", "white_plaster_02");
        var stal = BraxCityMaterials.Surface("hol lądowiska: stal nierdzewna szczotkowana", new Color(.72f, .73f, .74f), .3f, .85f);
        var led = BraxMiastoA.Swieci("hol lądowiska: linie LED w suficie", new Color(1f, .97f, .9f), new Color(1.3f, 1.25f, 1.15f));
        // posadzka (lico = poziom chodnika, wycięcie płyt pod holem w PosadzkiLokali), sufit, ściany
        var bp = new Bud(); QuadN(bp, P(sA, y0, .04f), P(sB, y0, .04f), P(sB, y0, dT), P(sA, y0, dT), Vector3.up); Wyrob(bp, "Hol: posadzka marmurowa", hr, marmur, false, false);
        var bs = new Bud(); QuadN(bs, P(sA, yS, .04f), P(sB, yS, .04f), P(sB, yS, dT), P(sA, yS, dT), Vector3.down); Wyrob(bs, "Hol: sufit", hr, gladz, false, false);
        var bw = new Bud();
        QuadN(bw, P(sA, y0, .04f), P(sA, y0, dT), P(sA, yS, dT), P(sA, yS, .04f), R);
        QuadN(bw, P(sB, y0, .04f), P(sB, y0, dT), P(sB, yS, dT), P(sB, yS, .04f), -R);
        Wyrob(bw, "Hol: ściany boczne (tynk)", hr, tynk, false, false);
        // ściana tylna: granit po obu stronach szybu (szyb — lico frontu windy z Tyłów B, bez wspólnej płaszczyzny z granitem)
        var bg = new Bud();
        foreach (var (a, b) in new[] { (sA, sS0), (sS1, sB) }) if (b - a > .05f) bg.Box(Vector3.Min(P(a, y0, dT - .03f), P(b, yS, dT)), Vector3.Max(P(a, y0, dT - .03f), P(b, yS, dT)));
        // front szybu między skrzydłami: ten sam granit 3 cm, otwory na opaskę drzwi i kasetę przywołania (cała ściana windy w granicie)
        {
            float fW = F.N.z >= 0f ? 1f : -1f, xa = Mathf.Min(P(sS0, y0, dT).x, P(sS1, y0, dT).x), xb = Mathf.Max(P(sS0, y0, dT).x, P(sS1, y0, dT).x);
            OkladzinaFrontuSzybu(bg, szyb, fW, y0, yS, xa, xb);
        }
        Wyrob(bg, "Hol: ściana tylna — granit czarny", hr, granit, false, false);
        // panele tekowe 2.2 cm na ścianach bocznych (szczelina cienia 3 cm pod sufitem), cokół ze stali 3 cm
        var bt = new Bud(); var bc = new Bud();
        foreach (var (sw, sg) in new[] { (sA, 1f), (sB, -1f) })
        {
            float s0 = sw, s1 = sw + sg * .022f;
            bt.Box(Vector3.Min(P(s0, y0 + .12f, 1.2f), P(s1, yS - .03f, dT - .4f)), Vector3.Max(P(s0, y0 + .12f, 1.2f), P(s1, yS - .03f, dT - .4f)));
            float c1 = sw + sg * .03f;
            bc.Box(Vector3.Min(P(s0, y0, 1.2f), P(c1, y0 + .1f, dT - .03f)), Vector3.Max(P(s0, y0, 1.2f), P(c1, y0 + .1f, dT - .03f)));
        }
        Wyrob(bt, "Hol: panele z okleiny tekowej", hr, tek, false, true); Wyrob(bc, "Hol: cokół ze stali", hr, stal, false, false);
        // linie LED w suficie (3 pasy 18 cm wzdłuż holu, 3 cm pod sufitem)
        var bl = new Bud();
        foreach (float s in new[] { sA + 1.6f, p.sc, sB - 1.6f }) bl.Box(Vector3.Min(P(s - .09f, yS - .03f, 1.4f), P(s + .09f, yS, dT - 1.4f)), Vector3.Max(P(s - .09f, yS - .03f, 1.4f), P(s + .09f, yS, dT - 1.4f)), true);
        Wyrob(bl, "Hol: linie LED w suficie", hr, led, false, false);
        // RECEPCJA (po prawej od wejścia): front z okleiny tekowej na cokole, blat z granitu z nawisem, laptop
        {
            float rs0 = sB - 2.1f, rs1 = sB - 1.35f, rd0 = 3.8f, rd1 = 7.6f, hR = 1.08f * k;
            var rf = new Bud(); rf.Box(Vector3.Min(P(rs0 + .05f, y0, rd0 + .05f), P(rs1 - .05f, y0 + .1f, rd1 - .05f)), Vector3.Max(P(rs0 + .05f, y0, rd0 + .05f), P(rs1 - .05f, y0 + .1f, rd1 - .05f)));
            Wyrob(rf, "Recepcja: cokół (stal)", hr, stal, false, false);
            var rt = new Bud(); rt.Box(Vector3.Min(P(rs0, y0 + .1f, rd0), P(rs1, y0 + hR - .04f, rd1)), Vector3.Max(P(rs0, y0 + .1f, rd0), P(rs1, y0 + hR - .04f, rd1)));
            Wyrob(rt, "Recepcja: front (okleina tekowa)", hr, tek, false, true);
            var rb = new Bud(); rb.Box(Vector3.Min(P(rs0 - .04f, y0 + hR - .04f, rd0 - .04f), P(rs1 + .02f, y0 + hR, rd1 + .04f)), Vector3.Max(P(rs0 - .04f, y0 + hR - .04f, rd0 - .04f), P(rs1 + .02f, y0 + hR, rd1 + .04f)), true);
            Wyrob(rb, "Recepcja: blat (granit polerowany)", hr, granit, false, true);
            var a0 = P(rs0, y0, rd0); var a1 = P(rs1, y0 + hR, rd1);
            BraxRegionMeble.Kolizja(hr, "Recepcja lądowiska: kolizja", (a0 + a1) * .5f, new Vector3(Mathf.Abs(a1.x - a0.x), hR, Mathf.Abs(a1.z - a0.z)));
            BraxRegionMeble.Model(hr, "Recepcja lądowiska: laptop", P(rs0 + .3f, y0 + hR, (rd0 + rd1) * .5f), Quaternion.LookRotation(-R).eulerAngles.y, .02f, false, "classic_laptop");
            BraxUlicePierzeje.KrzesloTapicerowane("Recepcja lądowiska: krzesło", hr, P(sB - .75f, y0, (rd0 + rd1) * .5f), Quaternion.LookRotation(-R).eulerAngles.y);
            BraxMiastoA.Akcja(hr, "ladowisko_recepcja", "RECEPCJA LĄDOWISKA", "Recepcja: Lot na dach? Winda na końcu holu — lądowisko 148 m nad promenadą.", P(rs0 - 1.0f, y0, (rd0 + rd1) * .5f), 1.6f);
        }
        // STREFA OCZEKIWANIA (po lewej): dwie sofy naprzeciw siebie, stolik, rośliny (Poly Haven)
        {
            float sm = sA + 1.9f, dm = 6.2f;
            BraxRegionMeble.Model(hr, "Hol lądowiska: sofa", P(sm, y0, dm - 1.55f), Quaternion.LookRotation(nIn).eulerAngles.y, .82f, true, "sofa_02");
            BraxRegionMeble.Model(hr, "Hol lądowiska: sofa", P(sm, y0, dm + 1.55f), Quaternion.LookRotation(-nIn).eulerAngles.y, .82f, true, "sofa_02");
            BraxRegionMeble.Model(hr, "Hol lądowiska: stolik", P(sm, y0, dm), 0f, .42f, true, "modern_coffee_table_01");
            BraxRegionMeble.Model(hr, "Hol lądowiska: roślina", P(sA + .55f, y0, 2.2f), 30f, 1.7f, true, "pachira_aquatica_01", "potted_plant_04");
            BraxRegionMeble.Model(hr, "Hol lądowiska: roślina", P(sA + .55f, y0, dT - .9f), 75f, 1.7f, true, "pachira_aquatica_01", "potted_plant_04");
            BraxRegionMeble.Model(hr, "Hol lądowiska: roślina", P(sB - .55f, y0, dT - .9f), 140f, 1.7f, true, "pachira_aquatica_01", "potted_plant_04");
        }
        // KASETON nad windą: „LĄDOWISKO · DACH 148 m” (płyta tylna 2.5 cm, rama 6 cm, lico 3 cm za ramą)
        {
            var obraz = NapisyTekstura("ladowisko_kaseton_hol", 1024, 256, new Color(.03f, .03f, .035f),
                new[] { ("LĄDOWISKO", .52f, .34f, new Color(.98f, .8f, .3f)), ("WINDA NA DACH  ·  " + Mathf.RoundToInt(yDach - yBase) + " m", .16f, .2f, new Color(.92f, .92f, .9f)) }, 1.0f);
            float w = Mathf.Min(2.5f, 2f * hs.x - .2f), h = .7f, yc = yS - .55f; var c = P(p.sc, yc, dT - .03f);   // w obrębie lica szybu, na okładzinie z granitu
            var czern = BraxCityMaterials.Surface("hol lądowiska: kaseton — aluminium czarne", new Color(.03f, .03f, .035f), .35f, .6f);
            var bk = new Bud(); var n = F.N;
            Vector3 Q(float x, float y, float dd) => c + R * x + Vector3.up * y + n * dd;
            bk.Box(Vector3.Min(Q(-w * .5f - .04f, -h * .5f - .04f, 0f), Q(w * .5f + .04f, h * .5f + .04f, .025f)), Vector3.Max(Q(-w * .5f - .04f, -h * .5f - .04f, 0f), Q(w * .5f + .04f, h * .5f + .04f, .025f)), true);
            foreach (var (x0, x1, yy0, yy1) in new[] { (-w * .5f - .04f, w * .5f + .04f, h * .5f, h * .5f + .04f), (-w * .5f - .04f, w * .5f + .04f, -h * .5f - .04f, -h * .5f), (-w * .5f - .04f, -w * .5f, -h * .5f, h * .5f), (w * .5f, w * .5f + .04f, -h * .5f, h * .5f) })
                bk.Box(Vector3.Min(Q(x0, yy0, .025f), Q(x1, yy1, .1f)), Vector3.Max(Q(x0, yy0, .025f), Q(x1, yy1, .1f)), true);
            Wyrob(bk, "Hol: kaseton lądowiska — obudowa", hr, czern, false, false);
            var bo = new Bud();
            QuadUVN(bo, Q(w * .5f, -h * .5f, .07f), Q(-w * .5f, -h * .5f, .07f), Q(-w * .5f, h * .5f, .07f), Q(w * .5f, h * .5f, .07f), n);
            Wyrob(bo, "Hol: kaseton lądowiska — lico", hr, obraz, false, false);
        }
        // winda HOL ↔ DACH
        float yTaras = yDach + .08f;
        BraxRegionTylyB.WindaZewn(hr, "LĄDOWISKO " + id, szyb, new[] { yBase, yTaras }, new[] { true, true }, new[] { "0 HOL", "D DACH — LĄDOWISKO" }, yTaras + 3.3f, F.N.z >= 0f ? 1f : -1f);
        lokaleWiezZbudowane.Add(id + "|HOL LĄDOWISKA"); lokaleWiezZbudowane.Add(id + "|SZYB LĄDOWISKA");
        BraxUliceKit.Punkt("R1_LADOWISKO_HOL", "hol lądowiska (wieżowiec " + id + ", promenada) — wejście z promenady", F.P(p.sc, yBase, 2.5f));
        BraxUliceKit.Punkt("R1_LADOWISKO_WINDA_DOL", "winda na dach — przystanek w holu", F.P(p.sc, yBase, dT - 1.4f));
        BraxUliceKit.Zapisz(hr, "hol_ladowiska_" + id);   // partie zestawu (litery nad wejściem) — jak lokale (WnetrzeKamienicy zapisuje swoje)
        Debug.Log("BRAXHORN_LADOWISKO " + id + ": hol " + (sB - sA).ToString("F1") + " × " + dT.ToString("F1") + " m, drzwi automatyczne w portalu, recepcja, strefa oczekiwania, winda HOL ↔ DACH (" + (yTaras - yBase).ToString("F0") + " m)");
    }

    /// <summary>Okładzina 3 cm na froncie szybu windy (Tyły B) przy przystanku y0: od xL do xR (świat), od y0 do y1, z otworami na
    /// opaskę drzwi (stal, wystaje 3 cm przed okładzinę) i kasetę przywołania (lico w licu okładziny). f = strona drzwi (+1 = +z).</summary>
    static void OkladzinaFrontuSzybu(Bud b, Vector2 szyb, float f, float y0, float y1, float xL, float xR)
    {
        var hs = BraxRegionTylyB.SzybPolowy; var dw = BraxRegionTylyB.DrzwiWindy; float k = K;
        float zA = szyb.y + f * hs.y, zB = szyb.y + f * (hs.y + .03f), z0 = Mathf.Min(zA, zB), z1 = Mathf.Max(zA, zB);
        float oL = szyb.x - dw.x * .5f - .16f, oR = szyb.x + dw.x * .5f + .16f, oT = y0 + dw.y + .25f;          // opaska
        float kL = szyb.x + dw.x * .5f + .35f, kR = szyb.x + dw.x * .5f + .55f, kD = y0 + 1.0f * k, kG = y0 + 1.35f * k;   // kaseta
        void Bx(float xa, float xb, float ya, float yb) { if (xb - xa > .005f && yb - ya > .005f) b.Box(new Vector3(xa, ya, z0), new Vector3(xb, yb, z1)); }
        Bx(xL, oL, y0, y1);                     // lewo od opaski, cała wysokość
        Bx(oL, oR, oT, y1);                     // nad opaską
        Bx(oR, kL, y0, y1);                     // między opaską a kasetą
        Bx(kL, kR, y0, kD); Bx(kL, kR, kG, y1); // pod i nad kasetą
        Bx(kR, xR, y0, y1);                     // prawo od kasety
    }

    // ============================================================================ DACH
    static void ZbudujLadowisko(Transform korzen, string id, Rect rd, float yd, Vector2 szyb, float yAttyka)
    {
        var g = new GameObject("Lądowisko na dachu (" + id + ")").transform; g.SetParent(korzen, false); g.position = Vector3.zero;
        float k = K, yT = yd + .08f, yP = yT + PlytaNad;                                        // taras, wierzch płyty
        var hs = BraxRegionTylyB.SzybPolowy; var dz = SzybRect(szyb);
        // ---- materiały (Poly Haven, jak w całym R1)
        var plyty = T("lądowisko: taras — płyty gresowe na wspornikach", 1.2f, new Color(.78f, .78f, .77f), .75f, "large_floor_tiles_02", "granite_tile_03");
        // konstrukcja: stal malowana proszkowo RAL 7016 (antracyt) — gładka powłoka, jak na prawdziwych lądowiskach dachowych
        var stal = BraxCityMaterials.Surface("lądowisko: stal konstrukcji (farba proszkowa RAL 7016)", new Color(.21f, .23f, .25f), .5f, 0f);   // farba = dielektryk
        var krata = T("lądowisko: stopnie i pomosty (blacha ryflowana ocynkowana)", .6f, new Color(.66f, .67f, .68f), .5f, "metal_plate", "metal_plate_02");
        var posPaw = T("lądowisko: pawilon — posadzka (marmur polerowany)", 1.6f, new Color(.95f, .94f, .92f), .18f, "marble_01", "floor_tiles_06");
        var alu = BraxCityMaterials.Surface("lądowisko: aluminium (obrzeża płyty)", new Color(.70f, .71f, .73f), .35f, .8f);
        var poreczM = BraxCityMaterials.Surface("lądowisko: poręcze (stal nierdzewna)", new Color(.75f, .76f, .77f), .28f, .9f);
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        var dachP = BraxCityMaterials.Surface("lądowisko: pawilon — dach (aluminium grafit)", new Color(.16f, .17f, .18f), .45f, .6f);
        var sufit = T("lądowisko: pawilon — sufit (gładź)", 3f, new Color(1f, 1f, .99f), .95f, "white_stucco", "white_plaster_02");
        var ledM = BraxMiastoA.Swieci("lądowisko: oprawy LED pawilonu", new Color(1f, .97f, .9f), new Color(1.2f, 1.15f, 1.05f));
        var ziel = BraxMiastoA.Swieci("lądowisko: lampy obwodu (zielone)", new Color(.2f, 1f, .35f), new Color(.25f, 1.6f, .45f));
        var biel = BraxMiastoA.Swieci("lądowisko: naświetlacze (światło białe)", new Color(1f, .98f, .92f), new Color(1.6f, 1.55f, 1.4f));
        // ---- TARAS: płyty 6 cm na wspornikach (lico 8 cm nad membraną), otwór na szyb; kolizja dachu w KolizjeZLokalami
        var rIn = Rect.MinMaxRect(rd.xMin + .45f, rd.yMin + .45f, rd.xMax - .45f, rd.yMax - .45f);
        float pz0 = dz.yMin - .4f, pz1 = dz.yMax + 4.2f, px0 = szyb.x - PawW * .5f, px1 = szyb.x + PawW * .5f, yPs = yT + 3.2f;
        var rPaw = Rect.MinMaxRect(px0, pz0, px1, pz1);
        var bt = new Bud();
        foreach (var r in MinusRect(rIn, rPaw)) bt.Box(new Vector3(r.xMin, yT - .06f, r.yMin), new Vector3(r.xMax, yT, r.yMax));
        Wyrob(bt, "Lądowisko: taras — płyty gresowe", g, plyty, false, false);
        var bpp = new Bud();
        foreach (var r in MinusRect(rPaw, dz)) bpp.Box(new Vector3(r.xMin, yT - .06f, r.yMin), new Vector3(r.xMax, yT, r.yMax));
        Wyrob(bpp, "Pawilon odlotów: posadzka marmurowa", g, posPaw, false, false);
        // attyka (od środka): kolizja do wysokości attyki
        foreach (var (a, b) in new[] { (new Vector3(rd.xMin, yd, rd.yMin), new Vector3(rd.xMax, yAttyka, rd.yMin + .45f)), (new Vector3(rd.xMin, yd, rd.yMax - .45f), new Vector3(rd.xMax, yAttyka, rd.yMax)),
                                       (new Vector3(rd.xMin, yd, rd.yMin), new Vector3(rd.xMin + .45f, yAttyka, rd.yMax)), (new Vector3(rd.xMax - .45f, yd, rd.yMin), new Vector3(rd.xMax, yAttyka, rd.yMax)) })
            KolBox(g, "Lądowisko: kolizja attyki", a, b);
        // ---- PAWILON ODLOTÓW wokół szybu (szkło od posadzki do sufitu, słupki stalowe, drzwi przesuwne od strony +z)
        {
            var sl = new Bud(); var bsz = new Bud(); float drzW = 2.0f, drzH = 2.4f;
            void ScianaPaw(Vector3 a, Vector3 b, bool drzwi)
            {
                float L = Vector3.Distance(a, b); var u = (b - a) / L; int n = Mathf.Max(1, Mathf.CeilToInt(L / 1.8f));
                var nOut = Vector3.Cross(u, Vector3.up);
                float sDrz0 = L * .5f - drzW * .5f - .06f, sDrz1 = L * .5f + drzW * .5f + .06f;
                for (int i = 0; i <= n; i++)
                {
                    float q = L * i / n; if (drzwi && q > sDrz0 - .1f && q < sDrz1 + .1f) continue;     // bez słupka w świetle drzwi
                    var c = a + u * q;
                    BoxMM(sl, c - u * .06f - nOut * .06f + Vector3.up * (yT - c.y), c + u * .06f + nOut * .06f + Vector3.up * (yPs - c.y));
                }
                for (int i = 0; i < n; i++)
                {
                    float t0 = L * i / n + .06f, t1 = L * (i + 1) / n - .06f;
                    if (drzwi && t1 > sDrz0 && t0 < sDrz1)
                    {   // przęsło z drzwiami: szkło po bokach i nad drzwiami, słupki ościeżnicy
                        foreach (var (q0, q1) in new[] { (t0, sDrz0 - .06f), (sDrz1 + .06f, t1) }) if (q1 - q0 > .05f) SzybaPaw(a + u * q0, a + u * q1, yT, yPs);
                        SzybaPaw(a + u * Mathf.Max(t0, sDrz0), a + u * Mathf.Min(t1, sDrz1), yT + drzH + .14f, yPs);
                        foreach (float q in new[] { sDrz0, sDrz1 }) { var c = a + u * q; BoxMM(sl, c - u * .06f - nOut * .06f + Vector3.up * (yT - c.y), c + u * .06f + nOut * .06f + Vector3.up * (yPs - c.y)); }
                        var cn = a + u * (L * .5f); BoxMM(sl, cn - u * (drzW * .5f) - nOut * .06f + Vector3.up * (yT + drzH + .02f - cn.y), cn + u * (drzW * .5f) + nOut * .06f + Vector3.up * (yT + drzH + .14f - cn.y), true);
                    }
                    else SzybaPaw(a + u * t0, a + u * t1, yT, yPs);
                }
                // kolizja ściany (bez otworu drzwi)
                if (drzwi)
                {
                    foreach (var (q0, q1) in new[] { (0f, sDrz0), (sDrz1, L) }) { var c0 = a + u * q0; var c1 = a + u * q1; KolBox(g, "Pawilon: kolizja szyby", Vector3.Min(c0, c1) - new Vector3(.06f, 0f, .06f) + Vector3.up * (yT - c0.y), Vector3.Max(c0, c1) + new Vector3(.06f, 0f, .06f) + Vector3.up * (yPs - c0.y)); }
                }
                else KolBox(g, "Pawilon: kolizja szyby", Vector3.Min(a, b) - new Vector3(.06f, 0f, .06f) + Vector3.up * (yT - a.y), Vector3.Max(a, b) + new Vector3(.06f, 0f, .06f) + Vector3.up * (yPs - a.y));
            }
            void SzybaPaw(Vector3 a, Vector3 b, float y0, float y1)
            {
                var a0 = new Vector3(a.x, y0, a.z); var b0 = new Vector3(b.x, y0, b.z); var b1 = new Vector3(b.x, y1, b.z); var a1 = new Vector3(a.x, y1, a.z);
                bsz.Quad(a0, b0, b1, a1); bsz.Quad(a1, b1, b0, a0);
            }
            ScianaPaw(new Vector3(px0, yT, pz0), new Vector3(px1, yT, pz0), false);
            ScianaPaw(new Vector3(px1, yT, pz0), new Vector3(px1, yT, pz1), false);
            ScianaPaw(new Vector3(px1, yT, pz1), new Vector3(px0, yT, pz1), true);
            ScianaPaw(new Vector3(px0, yT, pz1), new Vector3(px0, yT, pz0), false);
            Wyrob(sl, "Pawilon odlotów: słupki stalowe", g, stal, false, true); Wyrob(bsz, "Pawilon odlotów: szkło", g, szklo, false, false);
            BraxDrzwiLokalu.Podwojne(g, "R1_LADOWISKO_PAWILON", "PAWILON ODLOTÓW", new Vector3((px0 + px1) * .5f, yT, pz1), Vector3.forward, drzW, drzH, .14f, .025f, BraxDrzwiLokalu.Przesuwne);
            // dach pawilonu z okapem 30 cm, sufit, oprawy LED
            var bd = new Bud(); bd.Box(new Vector3(px0 - .3f, yPs, pz0 - .3f), new Vector3(px1 + .3f, yPs + .25f, pz1 + .3f)); Wyrob(bd, "Pawilon odlotów: dach", g, dachP, false, true);
            var bsu = new Bud(); QuadN(bsu, new Vector3(px0 - .3f, yPs, pz0 - .3f), new Vector3(px1 + .3f, yPs, pz0 - .3f), new Vector3(px1 + .3f, yPs, pz1 + .3f), new Vector3(px0 - .3f, yPs, pz1 + .3f), Vector3.down);
            Wyrob(bsu, "Pawilon odlotów: sufit", g, sufit, false, false);
            var bl = new Bud(); foreach (float x in new[] { px0 + 1.6f, px1 - 1.6f }) foreach (float z in new[] { dz.yMax + 1.2f, pz1 - 1.0f }) bl.Box(new Vector3(x - .3f, yPs - .03f, z - .3f), new Vector3(x + .3f, yPs, z + .3f), true);
            Wyrob(bl, "Pawilon odlotów: oprawy LED", g, ledM, false, false);
            // trzon windy w pawilonie: okładzina z granitu grafitowego 3 cm dookoła (front z otworami na stalową opaskę drzwi i kasetę)
            {
                var gr = T("lądowisko: trzon windy — granit grafitowy", 1.6f, new Color(.55f, .55f, .56f), .25f, "granite_tile", "granite_tile_04");
                var bTrz = new Bud(); float oTrz = .03f;
                bTrz.Box(new Vector3(dz.xMin - oTrz, yT, dz.yMin - oTrz), new Vector3(dz.xMax + oTrz, yPs, dz.yMin));                  // tył
                bTrz.Box(new Vector3(dz.xMin - oTrz, yT, dz.yMin), new Vector3(dz.xMin, yPs, dz.yMax));                          // bok L
                bTrz.Box(new Vector3(dz.xMax, yT, dz.yMin), new Vector3(dz.xMax + oTrz, yPs, dz.yMax));                          // bok P
                OkladzinaFrontuSzybu(bTrz, szyb, 1f, yT, yPs, dz.xMin - oTrz, dz.xMax + oTrz);                                  // front (drzwi do +z)
                Wyrob(bTrz, "Pawilon odlotów: okładzina trzonu windy (granit)", g, gr, false, true);
            }
            // poczekalnia: dwa fotele, stolik, roślina; tablica odlotów na stelażu
            BraxRegionMeble.Model(g, "Pawilon: fotel", new Vector3(px0 + 1.1f, yT, dz.yMax + 2.3f), 90f, .8f, true, "modern_arm_chair_01", "mid_century_lounge_chair");
            BraxRegionMeble.Model(g, "Pawilon: fotel", new Vector3(px0 + 1.1f, yT, dz.yMax + 3.5f), 90f, .8f, true, "modern_arm_chair_01", "mid_century_lounge_chair");
            BraxRegionMeble.Model(g, "Pawilon: stolik", new Vector3(px0 + 2.1f, yT, dz.yMax + 2.9f), 0f, .42f, true, "modern_coffee_table_01");
            BraxRegionMeble.Model(g, "Pawilon: roślina", new Vector3(px1 - .6f, yT, pz1 - .7f), 20f, 1.6f, true, "pachira_aquatica_01", "potted_plant_04");
            var tab = NapisyTekstura("ladowisko_odloty", 1024, 576, new Color(.02f, .04f, .08f),
                new[] { ("ODLOTY  ·  LĄDOWISKO R1", .86f, .09f, new Color(.98f, .8f, .3f)), ("MIASTO MISTRZÓW   BRAXHORN TOWER", .62f, .075f, Color.white),
                        ("OSIEDLE   NA ŻYCZENIE", .44f, .075f, Color.white), ("PORT   NA ŻYCZENIE", .26f, .075f, Color.white), ("BRAXHORN HELI", .08f, .06f, new Color(.6f, .65f, .7f)) }, .9f);
            float tx = px1 - 1.2f, tzz = dz.yMax + 1.6f, tw = 1.2f, th = .68f;
            var bstel = new Bud(); bstel.Box(new Vector3(tx - .04f, yT, tzz - .25f), new Vector3(tx + .04f, yT + 1.9f, tzz - .15f));
            bstel.Box(new Vector3(tx - .3f, yT, tzz - .45f), new Vector3(tx + .3f, yT + .03f, tzz + .05f));
            bstel.Box(new Vector3(tx - tw * .5f - .03f, yT + 1.2f, tzz - .15f), new Vector3(tx + tw * .5f + .03f, yT + 1.26f + th, tzz - .125f), true);   // płyta tylna ekranu
            foreach (var (xa, xb, ya, yb2) in new[] { (tx - tw * .5f - .03f, tx + tw * .5f + .03f, yT + 1.2f, yT + 1.23f), (tx - tw * .5f - .03f, tx + tw * .5f + .03f, yT + 1.23f + th, yT + 1.26f + th),
                                                       (tx - tw * .5f - .03f, tx - tw * .5f, yT + 1.23f, yT + 1.23f + th), (tx + tw * .5f, tx + tw * .5f + .03f, yT + 1.23f, yT + 1.23f + th) })
                bstel.Box(new Vector3(xa, ya, tzz - .125f), new Vector3(xb, yb2, tzz - .02f), true);                                                          // rama ekranu
            Wyrob(bstel, "Pawilon: tablica odlotów — stelaż", g, stal, false, true);
            var bo = new Bud(); QuadUVN(bo, new Vector3(tx + tw * .5f, yT + 1.23f, tzz - .05f), new Vector3(tx - tw * .5f, yT + 1.23f, tzz - .05f), new Vector3(tx - tw * .5f, yT + 1.23f + th, tzz - .05f), new Vector3(tx + tw * .5f, yT + 1.23f + th, tzz - .05f), Vector3.forward);
            Wyrob(bo, "Pawilon: tablica odlotów — ekran", g, tab, false, false);
        }
        // ---- PŁYTA LĄDOWISKA nad pawilonem: środek nad pawilonem, w granicach tarasu (z półką 1.5 m)
        float hb = PlytaB * .5f, hz = hb + Polka;
        // środek płyty: środek tarasu (miejsce na schody po obu stronach), o ile pawilon zostaje pod płytą (≤ 3.6 m przesunięcia)
        float pcx = (px0 + px1) * .5f, pcz = (pz0 + pz1) * .5f;
        float cx = Mathf.Clamp(Mathf.Clamp(rd.center.x, pcx - 3.6f, pcx + 3.6f), rd.xMin + hz + .6f, rd.xMax - hz - .6f);
        float cz = Mathf.Clamp(Mathf.Clamp(rd.center.y, pcz - 3.6f, pcz + 3.6f), rd.yMin + hz + .6f, rd.yMax - hz - .6f);
        bool miesci = rd.width >= 2f * hz + 1.2f && rd.height >= 2f * hz + 1.2f;
        if (!miesci) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_LADOWISKO " + id + ": dach " + rd.width.ToString("F1") + " × " + rd.height.ToString("F1") + " m za mały na płytę " + (2f * hz).ToString("F0") + " m"); return; }
        {
            // płyta 25 cm (aluminium), lico z oznakowaniem w teksturze; spód widoczny z tarasu
            float x0 = cx - hb, x1 = cx + hb, z0 = cz - hb, z1 = cz + hb, yb = yP - .25f;
            var bpl = new Bud();
            QuadN(bpl, new Vector3(x0, yb, z0), new Vector3(x1, yb, z0), new Vector3(x1, yP, z0), new Vector3(x0, yP, z0), Vector3.back);
            QuadN(bpl, new Vector3(x0, yb, z1), new Vector3(x1, yb, z1), new Vector3(x1, yP, z1), new Vector3(x0, yP, z1), Vector3.forward);
            QuadN(bpl, new Vector3(x0, yb, z0), new Vector3(x0, yb, z1), new Vector3(x0, yP, z1), new Vector3(x0, yP, z0), Vector3.left);
            QuadN(bpl, new Vector3(x1, yb, z0), new Vector3(x1, yb, z1), new Vector3(x1, yP, z1), new Vector3(x1, yP, z0), Vector3.right);
            QuadN(bpl, new Vector3(x0, yb, z0), new Vector3(x1, yb, z0), new Vector3(x1, yb, z1), new Vector3(x0, yb, z1), Vector3.down);
            Wyrob(bpl, "Lądowisko: płyta — obrzeża i spód (aluminium)", g, alu, false, true);
            var bo = new Bud(); QuadUVN(bo, new Vector3(x0, yP, z0), new Vector3(x1, yP, z0), new Vector3(x1, yP, z1), new Vector3(x0, yP, z1), Vector3.up);
            Wyrob(bo, "Lądowisko: płyta — lico z oznakowaniem (H, okrąg, TLOF)", g, TeksturaPlyty(), false, false);
            KolBox(g, "Lądowisko: kolizja płyty", new Vector3(x0, yb, z0), new Vector3(x1, yP, z1));
            // dźwigary pod płytą: wzdłuż x głębokość 45 cm, wzdłuż z 40 cm (bez wspólnych spodów), słupy HEB 30 na tarasie poza pawilonem
            var bk = new Bud();
            foreach (float z in new[] { z0 + .3f, cz, z1 - .3f }) bk.Box(new Vector3(x0 + .2f, yb - .45f, z - .12f), new Vector3(x1 - .2f, yb, z + .12f), true);
            foreach (float x in new[] { x0 + .3f, cx, x1 - .3f })
                foreach (var (za, zb) in new[] { (z0 + .42f, cz - .12f), (cz + .12f, z1 - .42f) }) bk.Box(new Vector3(x - .11f, yb - .40f, za), new Vector3(x + .11f, yb, zb), true);
            var pav = Rect.MinMaxRect(px0 - .7f, pz0 - .7f, px1 + .7f, pz1 + .7f); int slupow = 0;
            foreach (float x in new[] { x0 + .3f, cx, x1 - .3f })
                foreach (float z in new[] { z0 + .3f, cz, z1 - .3f })
                {
                    if (pav.Contains(new Vector2(x, z))) continue;
                    bk.Box(new Vector3(x - .15f, yT, z - .15f), new Vector3(x + .15f, yb - .45f, z + .15f));
                    bk.Box(new Vector3(x - .25f, yT, z - .25f), new Vector3(x + .25f, yT + .03f, z + .25f));                    // blacha podstawy 3 cm
                    KolBox(g, "Lądowisko: kolizja słupa", new Vector3(x - .15f, yT, z - .15f), new Vector3(x + .15f, yb, z + .15f)); slupow++;
                }
            Wyrob(bk, "Lądowisko: konstrukcja stalowa (dźwigary, słupy HEB)", g, stal, false, true);
            // półka bezpieczeństwa 1.5 m (krata), lico 25 cm pod płytą, krawędź zewnętrzna podniesiona 25 cm; przerwy przy schodach
            float yh = yb, xs = Polka, zW = cz + 5.2f;                                             // schody na osi z = zW (zachód i wschód)
            var bh = new Bud();
            bh.Box(new Vector3(x0 - xs, yh - .08f, z1), new Vector3(x1 + xs, yh, z1 + xs), true); bh.Box(new Vector3(x0 - xs, yh - .08f, z0 - xs), new Vector3(x1 + xs, yh, z0), true);
            bh.Box(new Vector3(x0 - xs, yh - .08f, z0), new Vector3(x0, yh, z1), true); bh.Box(new Vector3(x1, yh - .08f, z0), new Vector3(x1 + xs, yh, z1), true);
            Wyrob(bh, "Lądowisko: półka bezpieczeństwa (krata)", g, krata, false, true);
            KolBox(g, "Lądowisko: kolizja półki N", new Vector3(x0 - xs, yh - .08f, z1), new Vector3(x1 + xs, yh, z1 + xs)); KolBox(g, "Lądowisko: kolizja półki S", new Vector3(x0 - xs, yh - .08f, z0 - xs), new Vector3(x1 + xs, yh, z0));
            KolBox(g, "Lądowisko: kolizja półki W", new Vector3(x0 - xs, yh - .08f, z0), new Vector3(x0, yh, z1)); KolBox(g, "Lądowisko: kolizja półki E", new Vector3(x1, yh - .08f, z0), new Vector3(x1 + xs, yh, z1));
            var bkr = new Bud(); float sw = .75f;
            void Krawedz(Vector3 a, Vector3 b) { bkr.Box(Vector3.Min(a, b), Vector3.Max(a, b) + Vector3.up * .25f); }
            Krawedz(new Vector3(x0 - xs, yh, z1 + xs - .05f), new Vector3(x1 + xs, yh, z1 + xs)); Krawedz(new Vector3(x0 - xs, yh, z0 - xs), new Vector3(x1 + xs, yh, z0 - xs + .05f));
            foreach (float xe in new[] { x0 - xs, x1 + xs - .05f })
            {
                Krawedz(new Vector3(xe, yh, z0 - xs + .05f), new Vector3(xe + .05f, yh, zW - sw)); Krawedz(new Vector3(xe, yh, zW + sw), new Vector3(xe + .05f, yh, z1 + xs - .05f));
            }
            // wsporniki półki (ukośne) co ~4 m
            for (float t = -hb + 1.5f; t <= hb - 1.5f; t += 4f)
                foreach (var (o, rr) in new[] { (new Vector3(cx + t, 0f, z1), Vector3.forward), (new Vector3(cx + t, 0f, z0), Vector3.back), (new Vector3(x0, 0f, cz + t), Vector3.left), (new Vector3(x1, 0f, cz + t), Vector3.right) })
                {
                    var a = new Vector3(o.x, yb - .4f, o.z); var b = new Vector3(o.x, yh - .09f, o.z) + rr * (xs - .15f);
                    var c = (a + b) * .5f; bkr.BoxRot(c, new Vector3(.08f, .08f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
                }
            Wyrob(bkr, "Lądowisko: krawędź półki i wsporniki", g, stal, false, true);
            // lampy obwodu TLOF (zielone, co ~2.8 m, 6 cm nad licem), naświetlacze w narożach półki
            var bz = new Bud(); int lamp = 0;
            for (int i = 0; i <= 6; i++)
            {
                float t = -hb + .25f + i * (PlytaB - .5f) / 6f;
                foreach (var q in new[] { new Vector3(cx + t, yP, z0 + .25f), new Vector3(cx + t, yP, z1 - .25f), new Vector3(x0 + .25f, yP, cz + t), new Vector3(x1 - .25f, yP, cz + t) })
                { if ((i == 0 || i == 6) && (q.x == x0 + .25f || q.x == x1 - .25f)) continue; bz.Cyl(q, q + Vector3.up * .06f, .09f, 10); lamp++; }
            }
            Wyrob(bz, "Lądowisko: lampy obwodu (zielone)", g, ziel, false, false);
            var bn = new Bud(); var bnL = new Bud();
            foreach (var (x, z) in new[] { (x0 - .8f, z0 - .8f), (x1 + .8f, z0 - .8f), (x0 - .8f, z1 + .8f), (x1 + .8f, z1 + .8f) })
            {
                var dir = (new Vector3(cx - x, 0f, cz - z)).normalized; var c = new Vector3(x, yh + .09f, z);
                bn.BoxRot(c, new Vector3(.36f, .18f, .24f), Quaternion.LookRotation(dir));
                bnL.BoxRot(c + dir * .135f, new Vector3(.3f, .12f, .03f), Quaternion.LookRotation(dir));
            }
            Wyrob(bn, "Lądowisko: naświetlacze — obudowy", g, stal, false, true); Wyrob(bnL, "Lądowisko: naświetlacze — szyby", g, biel, false, false);
            // ---- SCHODY (2, zachód i wschód): 23 stopnie po 17.7 cm, krata 30 cm, policzki, poręcze 1.0 m
            int stopni = 0; var bsS = new Bud(); var bsP = new Bud(); var bsR = new Bud();
            foreach (float sg in new[] { -1f, 1f })
            {
                float xTop = sg < 0 ? x0 - xs : x1 + xs; int nR = 23; float rise = (yh - yT) / nR, run = .28f, wS = 1.3f;
                float xBot = xTop + sg * (nR - 1) * run;
                if ((sg < 0 && xBot < rd.xMin + .6f) || (sg > 0 && xBot > rd.xMax - .6f)) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_LADOWISKO schody " + (sg < 0 ? "zachodnie" : "wschodnie") + " nie mieszczą się na tarasie"); continue; }
                for (int i = 0; i < nR - 1; i++)
                {
                    float xa = xTop + sg * (nR - 1 - i) * run, xb = xa - sg * (run + .02f), yy = yT + (i + 1) * rise;
                    bsS.Box(new Vector3(Mathf.Min(xa, xb), yy - .04f, zW - wS * .5f), new Vector3(Mathf.Max(xa, xb), yy, zW + wS * .5f), true); stopni++;
                }
                var pA = new Vector3(xBot + sg * run, yT, zW); var pB = new Vector3(xTop, yh, zW);
                foreach (float sz in new[] { -1f, 1f })
                {
                    var o = Vector3.forward * sz * (wS * .5f + .03f);
                    var a = pA + o + Vector3.up * -.05f; var b = pB + o + Vector3.up * -.05f;
                    bsP.BoxRot((a + b) * .5f, new Vector3(.05f, .25f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));     // policzek
                    var ra = pA + o + Vector3.up * 1.0f; var rb = pB + o + Vector3.up * 1.0f;
                    bsR.Cyl(ra, rb, .025f, 10);                                                                                      // poręcz
                    for (int j = 0; j <= 4; j++) { var q = Vector3.Lerp(pA, pB, j / 4f) + o; bsR.Cyl(q, q + Vector3.up * 1.0f, .02f, 8); }
                }
                // kolizja: rampa wzdłuż krawędzi stopni
                var kc = new GameObject("Lądowisko: schody " + (sg < 0 ? "zachodnie" : "wschodnie") + " — kolizja (rampa)"); kc.transform.SetParent(g, false);
                var dirS = (pB - pA).normalized; var nS = Vector3.Cross(dirS, Vector3.forward * -sg).normalized; if (nS.y < 0f) nS = -nS;
                kc.transform.position = (pA + pB) * .5f - nS * .1f; kc.transform.rotation = Quaternion.LookRotation(dirS, nS);
                var bcS = kc.AddComponent<BoxCollider>(); bcS.size = new Vector3(wS, .2f, Vector3.Distance(pA, pB) + .3f);
            }
            Wyrob(bsS, "Lądowisko: schody — stopnie (krata)", g, krata, false, true); Wyrob(bsP, "Lądowisko: schody — policzki (stal grafit)", g, stal, false, true); Wyrob(bsR, "Lądowisko: schody — poręcze (stal nierdzewna)", g, poreczM, false, true);
            // ---- RĘKAW WIATROWY (maszt 6 m na tarasie, rękaw 2.4 m w pasy pomarańczowo-białe)
            {
                var m0 = new Vector3(rd.xMax - 1.4f, yT, rd.yMax - 1.4f); float hM = 6.0f;
                var bm = new Bud(); bm.Cyl(m0, m0 + Vector3.up * hM, .06f, 10); bm.Box(m0 - new Vector3(.2f, 0f, .2f), m0 + new Vector3(.2f, .03f, .2f));
                var top = m0 + Vector3.up * (hM - .2f); var kier = new Vector3(-.8f, -.25f, -.55f).normalized;
                bm.Cyl(top, top + kier * .12f, .33f, 14, .33f, false);
                Wyrob(bm, "Lądowisko: maszt rękawa wiatrowego", g, stal, false, true);
                // pasy jako dwa osobne materiały (bez polegania na kolorach wierzchołków): 3 pomarańczowe + 2 białe, tkanina poliestrowa
                var brO = new Bud(); var brB = new Bud(); var a = top + kier * .12f;
                for (int i = 0; i < 5; i++)
                {
                    var q0 = a + kier * (i * .48f); var q1 = a + kier * ((i + 1) * .48f); (i % 2 == 0 ? brO : brB).Cyl(q0, q1, .32f - i * .04f, 14, .32f - (i + 1) * .04f, false);
                }
                Wyrob(brO, "Lądowisko: rękaw wiatrowy — pasy pomarańczowe", g, BraxCityMaterials.Surface("lądowisko: rękaw — tkanina pomarańczowa", new Color(1f, .42f, .06f), .9f), false, false);
                Wyrob(brB, "Lądowisko: rękaw wiatrowy — pasy białe", g, BraxCityMaterials.Surface("lądowisko: rękaw — tkanina biała", new Color(.96f, .96f, .94f), .9f), false, false);
                KolBox(g, "Lądowisko: kolizja masztu", m0 - new Vector3(.08f, 0f, .08f), m0 + new Vector3(.08f, hM, .08f));
            }
            // ---- GRA
            var srodek = new Vector3(cx, yP, cz);
            BraxMiastoA.Akcja(g, "ladowisko_lot", "LOT HELIKOPTEREM", "Lądowisko R1 na dachu wieżowca przy promenadzie: lot do Miasta Mistrzów (BRAXHORN TOWER) — loty między miastami.", srodek, 4f);
            BraxUliceKit.Punkt("R1_LADOWISKO_PLYTA", "płyta lądowiska — środek H (" + (yP - .15f).ToString("F0") + " m n.p.t.)", srodek);
            // ścieżka podejścia dla helikoptera (lot między miastami): nad promenadą (+z) — wolna przestrzeń, sąsiednie wieże niższe (108–126 m);
            // podejście ok. 10° z 90 m przed płytą na +35 m, potem zawis 3 m nad H i przyziemienie w R1_LADOWISKO_PLYTA
            BraxUliceKit.Punkt("R1_LADOWISKO_PODEJSCIE", "helikopter: początek podejścia (90 m nad promenadą, +35 m nad płytą)", srodek + new Vector3(0f, 35f, 90f));
            BraxUliceKit.Punkt("R1_LADOWISKO_ZAWIS", "helikopter: zawis 3 m nad H przed przyziemieniem", srodek + Vector3.up * 3f);
            BraxUliceKit.Punkt("R1_LADOWISKO_TARAS", "taras na dachu przy pawilonie odlotów", new Vector3((px0 + px1) * .5f, yT, pz1 + 1.5f));
            BraxUliceKit.Punkt("R1_LADOWISKO_WINDA_GORA", "winda — przystanek na dachu (pawilon odlotów)", new Vector3(szyb.x, yT, dz.yMax + 1.2f));
            ladowiskoZbudowane = true;
            ladowiskoOpis = "płyta " + PlytaB.ToString("F0") + " × " + PlytaB.ToString("F0") + " m (D 16) na " + (yP - .15f).ToString("F0") + " m, półka " + Polka + " m, lamp obwodu " + lamp + ", schodów 2 × " + (stopni / 2) + " stopni, słupów " + slupow + ", pawilon " + PawW.ToString("F0") + " × " + (pz1 - pz0).ToString("F1") + " m";
            Debug.Log("BRAXHORN_LADOWISKO " + id + ": " + ladowiskoOpis);
        }
    }
}

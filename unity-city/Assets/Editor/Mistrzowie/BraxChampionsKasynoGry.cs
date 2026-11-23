using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using Z = BraxChampionsAutomatyZasady;
using A = BraxChampionsAutomaty;
using G = BraxChampionsAutomatyGry;
using P = BraxChampionsAutomatyGry.PokerGra;
using K = BraxChampionsAutomatyGry.KenoGra;
using KG = BraxChampionsAutomatyGry.KoloGra;

// MIASTO MISTRZÓW - the casinos' other machine games and the carousels (owner 16.11 20:28: "No tych automatów możesz zrobić
// więcej ... albo lepiej rozstawić. Tylko musiałbyś zrobić różne gry w automatach ... Pokaż, jak się robi luksus"), the
// generator's half of BraxChampionsAutomatyGry:
//  - VIDEO POKER and KENO on the screen upright: the game's whole picture on the main screen (the cards / the board are its
//    live layer, quads cut from one atlas), the pay table on the upper screen, the owner's topper of the game (16.11 20:46,
//    1:1) in the jackpot screen's place. The cards: his back with the bull and his King, Queen and Jack (1:1, on the card's
//    face inside its margin), the pips and the corner indices drawn as real cards (the index on the margin, never on his art);
//  - KOŁO MISTRZÓW on the stepper: the top box carries his topper, a 62 cm wheel stands on it in a lacquered housing with a
//    gold rim and 24 bulbs, 22 segments, chrome pegs, the red pointer flapping on them; the third drum carries KOŁO;
//  - the CAROUSEL: eight machines round a lacquered drum, their backs to it, a crown with his toppers on four sides;
//  - his BRAXHORN CASINO chips (16.11 20:46) as real chips: his face printed top and bottom, the edge in the chip's colour
//    with gold inserts - two 5s are the roulette's stake of 10.
public static partial class BraxChampionsKasynoSprzet
{
    static Material tloPoker, tloKeno, tabelaPoker, tabelaKeno, kartyMat, kenoMat, koloTarcza, zarowka, zlotoKola, zetonyMat;
    static Material[] toppery, tabelaKolo;
    static Mesh[] probkaPoker, probkaKeno;
    static List<(Mesh, Material)> czKoloTarcza, czWskaznik;
    public static bool TopperyWlasciciela, KartyWlasciciela, ZetonyWlasciciela;
    static readonly Color32 PokerTlo = new Color32(10, 22, 78, 255), KenoTlo = new Color32(10, 18, 56, 255), Krem2 = new Color32(252, 250, 243, 255),
        KartaCzerwien = new Color32(196, 18, 32, 255), KartaCzern = new Color32(22, 22, 28, 255), Zolty = new Color32(255, 214, 64, 255);
    const float Skala = 1024f / EkranW;                                    // the main screen's pixels a metre

    static void GryMaterialy()
    {
        int brakT = 0;
        toppery = new Material[G.Ile]; string[] plik = { null, null, "topper_video_poker", "topper_keno", "topper_kolo_mistrzow" };
        for (int g = G.Poker; g < G.Ile; g++)
            toppery[g] = Ekran("topper_" + g, Png("kasyno_topper_" + plik[g], 1536, 512, Topper(g, plik[g], ref brakT), true), 1.0f);
        TopperyWlasciciela = brakT == 0;
        if (brakT > 0) BraxBuildQuality.Warn("DO POPRAWKI KASYNO_TOPPERY missing " + brakT + "/3 in " + Wlasciciel + "/toppery: those games carry a drawn sign");
        tloPoker = Ekran("tlo_poker", Png("kasyno_tlo_poker", 1024, 576, TloPoker(), true), 1.0f);
        tloKeno = Ekran("tlo_keno", Png("kasyno_tlo_keno", 1024, 576, TloKeno(), true), 1.0f);
        tabelaPoker = Ekran("tabela_poker", Png("kasyno_tabela_poker", 1024, 576, TabelaPoker(), true), 1.0f);
        tabelaKeno = Ekran("tabela_keno", Png("kasyno_tabela_keno", 1024, 576, TabelaKeno(), true), 1.0f);
        kartyMat = Ekran("karty_atlas", Png("kasyno_karty_atlas", P.AtlasW, P.AtlasH, KartyAtlas(), true), 1.0f);
        kenoMat = Ekran("keno_atlas", Png("kasyno_keno_atlas", K.AtlasW, K.AtlasH, KenoAtlas(), true), 1.0f);
        tabelaKolo = new Material[Motywy.Length];
        for (int t = 0; t < Motywy.Length; t++) tabelaKolo[t] = Ekran("tabela_kolo_" + t, Png("kasyno_tabela_kolo_" + t, 1024, 512, TabelaKolo(t), true), .85f);
        koloTarcza = BraxChampionsHarbour.Surface("kasyno_kolo_tarcza", Color.white, .28f, 0f, new Color(.30f, .30f, .30f));
        var kt = Png("kasyno_kolo_tarcza", 1024, 1024, KoloTarczaPx(), true); koloTarcza.SetTexture("_MainTex", kt); koloTarcza.SetTexture("_EmissionMap", kt); Wnetrze(koloTarcza);
        zarowka = Swiatlo("zarowka", new Color(1.8f, 1.45f, .80f));
        zlotoKola = Metal("zloto_kola", new Color(1.0f, .78f, .36f), .22f);
        zetonyMat = BraxChampionsHarbour.Surface("kasyno_zetony", Color.white, .42f, 0f);
        zetonyMat.SetTexture("_MainTex", Png("kasyno_zetony_atlas", 2048, 512, ZetonyAtlas(), true)); Wnetrze(zetonyMat);
        WejscieMaterialy();
        // the layers' pictures before the game starts (saved meshes: four of each)
        probkaPoker = new Mesh[4]; probkaKeno = new Mesh[4];
        var wp = new G.Warstwa(P.AtlasW, P.AtlasH); var wk = new G.Warstwa(K.AtlasW, K.AtlasH);
        for (int v = 0; v < 4; v++)
        {
            var los = new System.Random(500 + v); var talia = new int[52]; P.Tasuj(los, talia);
            var karty = new int[5]; Array.Copy(talia, karty, 5); var m = new Mesh { name = "video poker próbka " + (v + 1) };
            P.Zbuduj(wp, m, karty, new[] { true, true, true, true, true }, 0, 0); probkaPoker[v] = ZapiszSiatke(m, "kasyno_poker_probka_" + (v + 1));
            var typy = new int[K.Typy]; var kule = new int[K.Kule]; K.Losuj(los, typy); K.Losuj(los, kule);
            var mk = new Mesh { name = "keno próbka " + (v + 1) }; K.Zbuduj(wk, mk, typy, kule, K.Kule); probkaKeno[v] = ZapiszSiatke(mk, "kasyno_keno_probka_" + (v + 1));
        }
    }

    // ---------------- poker / keno on the screen upright
    static void AutomatGry(Kasyno k, A.Maszyna m, Vector3 at, Quaternion q, int idx)
    {
        var kor = m.korpus; var qs = Quaternion.Euler(EkranTilt, 0f, 0f);
        var scr = new GameObject("ekran gry").transform; scr.SetParent(kor, false); scr.localPosition = EkranS0; scr.localRotation = qs;
        var o = new GameObject(m.gra == G.Poker ? "video poker (karty)" : "keno (plansza 80)"); o.transform.SetParent(scr, false);
        o.transform.localPosition = new Vector3(0f, 0f, 2f * Przed);                  // the picture is behind it (>= 6 mm)
        var mf = o.AddComponent<MeshFilter>(); mf.sharedMesh = m.gra == G.Poker ? probkaPoker[idx % 4] : probkaKeno[idx % 4];
        Cienie(o.AddComponent<MeshRenderer>()).sharedMaterial = m.gra == G.Poker ? kartyMat : kenoMat; m.warstwa = mf;
        m.licznik = Ekranik(scr, "licznik", licznikEkran, new Vector3(0f, LicznikY, Przed), .32f, .04f, Quaternion.identity);
        m.przycisk = Spin(kor, DeckEkran, new Vector3(-.22f, 0f, .02f), czSpinEkran);
        k.ekran++;
        W.Kolizja(at + q * new Vector3(0f, 1.03f, .10f), new Vector3(.72f, 2.06f, .84f), q);
        W.Kolizja(at + q * new Vector3(0f, .37f, Taboret), new Vector3(.40f, .74f, .40f), q);
        Siedzisko(m, at, q);
    }

    // ---------------- KOŁO MISTRZÓW: the wheel on the stepper
    public static readonly Vector3 KoloC = new Vector3(0f, 2.03f, .075f);       // the wheel's centre, its face's plane
    public const float KoloR = .31f, KoloObudowaR = .36f;
    static void KoloNadstawka(BraxChampionsMeshBatch b, int t)
    {
        var lak = lakier[t];
        // the short top box with the owner's KOŁO MISTRZÓW topper in a chrome frame
        b.RoundedBoxMinMax(lak, -.33f, .33f, 1.42f, 1.64f, -.26f, .22f, .02f, 2);
        Obraz(b, toppery[G.Kolo], new Vector3(0f, 1.53f, .227f), .57f, .19f, Quaternion.identity);
        b.BoxMinMax(chrom, -.30f, .30f, 1.628f, 1.636f, .22f, .232f); b.BoxMinMax(chrom, -.30f, .30f, 1.424f, 1.432f, .22f, .232f);
        b.BoxMinMax(chrom, .29f, .30f, 1.432f, 1.628f, .22f, .232f); b.BoxMinMax(chrom, -.30f, -.29f, 1.432f, 1.628f, .22f, .232f);
        // the saddle, the lacquered housing (its face at z .055), the gold rim, 24 bulbs, the pointer's bracket
        b.RoundedBoxMinMax(lak, -.16f, .16f, 1.636f, 1.71f, -.08f, .04f, .01f, 1);
        var c = new Vector3(0f, KoloC.y, 0f); var qz = Quaternion.Euler(90f, 0f, 0f);
        b.Cylinder(lak, c + new Vector3(0f, 0f, -.07f), KoloObudowaR, .125f, 48, qz, true, true);
        const int N = 48;
        for (int i = 0; i < N; i++)
        {
            float a0 = 2f * Mathf.PI * i / N, a1 = 2f * Mathf.PI * (i + 1) / N;
            b.Tube(zlotoKola, c + new Vector3(KoloObudowaR * Mathf.Sin(a0), KoloObudowaR * Mathf.Cos(a0), .055f), c + new Vector3(KoloObudowaR * Mathf.Sin(a1), KoloObudowaR * Mathf.Cos(a1), .055f), .012f, 6);
        }
        for (int i = 0; i < 24; i++)
        {
            float a = 2f * Mathf.PI * (i + .5f) / 24f; const float rb = .336f, br = .011f;
            b.Lathe(zarowka, c + new Vector3(rb * Mathf.Sin(a), rb * Mathf.Cos(a) - br, .062f), Profil(br, 5), 8);
        }
        // the pointer's bracket on the housing's top and its pin (the pointer swings on it, 1 mm in front)
        b.Box(chrom, new Vector3(0f, KoloC.y + KoloObudowaR + .008f, .045f), new Vector3(.05f, .03f, .07f), Quaternion.identity);
        float py = KoloC.y + KoloR + .055f;
        b.Tube(chrom, new Vector3(0f, py, .078f), new Vector3(0f, py, KoloC.z + .028f - .007f), .004f, 8);
    }
    // the moving parts: the wheel (face, back, rim, pegs, hub; its centre at the origin, the face at z 0) and the pointer
    // (its pivot at the origin, hanging down)
    static void CzesciKola()
    {
        var b = new BraxChampionsMeshBatch(); var qz = Quaternion.Euler(90f, 0f, 0f);
        b.Cylinder(czern, new Vector3(0f, 0f, -.012f), KoloR, .012f, 44, qz, false, true);
        const int S = 88; const float r0 = .05f, sk = 500f / 1024f;
        Vector3 Pk(float r, float f) => new Vector3(-r * Mathf.Sin(f), r * Mathf.Cos(f), 0f);       // viewer's clockwise angle f from the top
        Vector2 Uk(float r, float f) => new Vector2(.5f + r * Mathf.Sin(f) / KoloR * sk, .5f + r * Mathf.Cos(f) / KoloR * sk);
        for (int i = 0; i < S; i++)
        {
            float f0 = 2f * Mathf.PI * i / S, f1 = 2f * Mathf.PI * (i + 1) / S;
            QuadPrzod(b, koloTarcza, Vector3.forward, Pk(r0, f0), Pk(r0, f1), Pk(KoloR, f1), Pk(KoloR, f0), Uk(r0, f0), Uk(r0, f1), Uk(KoloR, f1), Uk(KoloR, f0));
        }
        for (int i = 0; i < 44; i++)
        {
            float f0 = 2f * Mathf.PI * i / 44, f1 = 2f * Mathf.PI * (i + 1) / 44;
            b.Tube(chrom, Pk(KoloR + .004f, f0) + new Vector3(0f, 0f, -.006f), Pk(KoloR + .004f, f1) + new Vector3(0f, 0f, -.006f), .007f, 6);
        }
        int n = KG.Segmenty.Length;
        for (int s = 0; s < n; s++)
            b.Cylinder(chrom, Pk(KoloR - .014f, (s + .5f) * 2f * Mathf.PI / n), .0045f, .02f, 6, qz, true);
        b.Cylinder(zlotoKola, Vector3.zero, .055f, .02f, 24, qz, true);
        b.Cylinder(chrom, new Vector3(0f, 0f, .02f), .03f, .012f, 16, qz, true);
        czKoloTarcza = Czesc(b, "koło mistrzów tarcza");
        // the pointer: a red wedge, 7.5 cm, its tip down into the pegs' circle
        var tip = new Vector2(0f, -.075f); var l = new Vector2(.022f, -.004f); var r = new Vector2(-.022f, -.004f); var t0 = new Vector2(0f, .012f);
        Pryzmat(b, kula, new[] { tip, l, t0, r }, -.006f, .006f);                    // it hangs from the chrome bracket (the cabinet's)
        czWskaznik = Czesc(b, "koło mistrzów wskaźnik");
    }
    static void KoloNaAutomacie(A.Maszyna m, Transform kor, int idx)
    {
        var kolo = new GameObject("koło mistrzów (22 pola)").transform; kolo.SetParent(kor, false); kolo.localPosition = KoloC;
        kolo.localRotation = Quaternion.AngleAxis(KG.KatSegmentu(idx * 5 % KG.Segmenty.Length), Vector3.forward);
        Wstaw(czKoloTarcza, kolo, "tarcza"); m.kolo = kolo;
        var w = new GameObject("wskaźnik koła").transform; w.SetParent(kor, false);
        w.localPosition = new Vector3(0f, KoloC.y + KoloR + .055f, KoloC.z + .028f); Wstaw(czWskaznik, w, "wskaźnik"); m.wskaznik = w;
    }
    // a quad / a triangle facing dir whatever the order given; a convex prism between z0 and z1 (its outline in x / y)
    static void QuadPrzod(BraxChampionsMeshBatch b, Material m, Vector3 dir, Vector3 a, Vector3 c1, Vector3 c2, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
    {
        if (Vector3.Dot(Vector3.Cross(c1 - a, c2 - a), dir) >= 0f) b.QuadUV(m, a, c1, c2, d, ua, ub, uc, ud);
        else b.QuadUV(m, a, d, c2, c1, ua, ud, uc, ub);
    }
    static void TrojkatPrzod(BraxChampionsMeshBatch b, Material m, Vector3 dir, Vector3 a, Vector3 c1, Vector3 c2)
    {
        if (Vector3.Dot(Vector3.Cross(c1 - a, c2 - a), dir) >= 0f) b.Triangle(m, a, c1, c2); else b.Triangle(m, a, c2, c1);
    }
    static void Pryzmat(BraxChampionsMeshBatch b, Material m, Vector2[] o, float z0, float z1)
    {
        Vector3 V(Vector2 p, float z) => new Vector3(p.x, p.y, z);
        var mid = Vector2.zero; foreach (var p in o) mid += p; mid /= o.Length;
        for (int i = 1; i + 1 < o.Length; i++)
        {
            TrojkatPrzod(b, m, Vector3.forward, V(o[0], z1), V(o[i], z1), V(o[i + 1], z1));
            TrojkatPrzod(b, m, Vector3.back, V(o[0], z0), V(o[i], z0), V(o[i + 1], z0));
        }
        for (int i = 0; i < o.Length; i++)
        {
            var p0 = o[i]; var p1 = o[(i + 1) % o.Length]; var e = p1 - p0; var nrm = new Vector3(e.y, -e.x, 0f);
            if (Vector2.Dot(new Vector2(nrm.x, nrm.y), (p0 + p1) * .5f - mid) < 0f) nrm = -nrm;
            QuadPrzod(b, m, nrm, V(p0, z0), V(p1, z0), V(p1, z1), V(p0, z1), Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        }
    }

    // ---------------- the CAROUSEL: n machines round a drum, their backs to it
    /// <summary>A carousel at c (floor): n machines in a ring facing out, the games of gry in turn (start shifts them), a
    /// lacquered drum with gold bands in the middle and a crown with the owner's toppers on four sides at crownY.</summary>
    public static void Karuzela(Kasyno k, Vector3 c, int n, int[] gry, int start, float crownY)
    {
        float rb = .36f / Mathf.Tan(Mathf.PI / n) + .03f;              // the cabinets' back corners clear each other
        for (int i = 0; i < n; i++)
        {
            float a = 360f * i / n + 180f / n; int gra = gry[(i + start) % gry.Length];
            float r0 = rb + (G.Ekranowa(gra) ? EkranD : MechD) * .5f;
            var q = Quaternion.Euler(0f, a, 0f);
            Automat(k, c + q * new Vector3(0f, 0f, r0), a, gra, (start + i) % Motywy.Length);
        }
        var body = k.body; float rd = rb - .04f;
        body.Cylinder(czern, c, rd, crownY - .27f, 40, Quaternion.identity, true);
        foreach (float y in new[] { .02f, 1.0f, 1.9f, crownY - .34f })
            body.Cylinder(zlotoKola, c + new Vector3(0f, y, 0f), rd + .012f, .04f, 40, Quaternion.identity, true, true);
        // the crown: a black box with a gold cornice, the toppers of the carousel's games on its four faces
        float hw = .70f, h = .46f; var cc = c + new Vector3(0f, crownY, 0f);
        body.RoundedBox(czern, cc, new Vector3(2f * hw, h, 2f * hw), .02f, Quaternion.identity, 1);
        body.BoxMinMax(zlotoKola, cc.x - hw - .03f, cc.x + hw + .03f, cc.y + h * .5f, cc.y + h * .5f + .05f, cc.z - hw - .03f, cc.z + hw + .03f);
        body.BoxMinMax(zlotoKola, cc.x - hw - .02f, cc.x + hw + .02f, cc.y - h * .5f - .04f, cc.y - h * .5f, cc.z - hw - .02f, cc.z + hw + .02f);
        for (int f = 0; f < 4; f++)
        {
            int gra = gry[(f + start) % gry.Length]; var mat = gra >= G.Poker ? toppery[gra] : toppery[G.Kolo];
            var q = Quaternion.Euler(0f, 90f * f, 0f);
            Obraz(body, mat, cc + q * new Vector3(0f, 0f, hw + Przed), 1.32f, .44f, q);
        }
        k.karuzele++;
        W.Kolizja(c + new Vector3(0f, crownY * .5f, 0f), new Vector3(2f * rd, crownY, 2f * rd), Quaternion.identity);
    }

    // ---------------- the chips: his faces (16.11 20:46: 5 / 25 / 100 / 500 / 1000) on real 39 mm chips
    public const float ZetonR = .0195f, ZetonH = .0034f;
    // his sheet: each chip's rectangle (x, y from the top-left, w, h) and its edge colour
    static readonly int[,] ZetonyArkusz = { { 29, 55, 441, 447 }, { 500, 56, 442, 446 }, { 972, 56, 441, 445 }, { 200, 548, 485, 489 }, { 751, 547, 490, 490 } };
    static readonly Color32[] ZetonyKolor = { new Color32(168, 170, 176, 255), new Color32(22, 112, 44, 255), new Color32(150, 22, 28, 255),
        new Color32(96, 34, 146, 255), new Color32(26, 26, 30, 255) };
    public static readonly int[] Nominaly = { 5, 25, 100, 500, 1000 };
    static Color32[] ZetonyAtlas()
    {
        const int w = 2048, h = 512; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, new Color32(20, 20, 22, 255));
        var src = Wczytaj(Wlasciciel + "/zetony/zetony_5_25_100_500_1000.png", out int sw, out int sh);
        ZetonyWlasciciela = src != null;
        for (int i = 0; i < 5; i++)
        {
            int x0 = 8 + i * 408, y0 = 104; var kol = ZetonyKolor[i];
            if (src != null)
            {
                int ax = ZetonyArkusz[i, 0], ay = ZetonyArkusz[i, 1], aw = ZetonyArkusz[i, 2], ah = ZetonyArkusz[i, 3];
                BraxChampionsPiksele.Prost(px, w, h, x0, y0, x0 + 400, y0 + 400, kol);
                Wklej(px, w, h, src, sw, sh, ax, sh - ay - ah, aw, ah, x0, y0, 400, 400);
            }
            else
            {
                Kolko(px, w, h, x0 + 200, y0 + 200, 198f, kol); Kolko(px, w, h, x0 + 200, y0 + 200, 140f, new Color32(18, 16, 14, 255));
                Napis(px, w, h, Nominaly[i].ToString(), x0 + 200, y0 + 200, 90f, 230f, 0f, Zloto, 0f, Zloto);
            }
            // the edge: the chip's colour with eight gold inserts (his design), a row of 16 px
            int ey = i * 18;
            for (int x = 0; x < w; x++)
            {
                bool wklad = (x * 16 / w) % 2 == 0;                       // eight inserts round the edge
                for (int y = ey; y < ey + 16; y++) px[y * w + x] = wklad ? new Color32(222, 172, 62, 255) : kol;
            }
        }
        if (src == null) BraxBuildQuality.Warn("DO POPRAWKI KASYNO_ZETONY missing " + Wlasciciel + "/zetony: the chips carry drawn faces");
        return px;
    }
    /// <summary>A chip of nominal index i (0..4) as a part: his face on top and bottom, its edge, base at y 0.</summary>
    static void Zeton(BraxChampionsMeshBatch b, int i, Vector3 at)
    {
        const int N = 24; float cx = (8 + i * 408 + 200) / 2048f, cy = (104 + 200) / 512f, ru = 198f / 2048f, rv = 198f / 512f;
        for (int j = 0; j < N; j++)
        {
            float a0 = 2f * Mathf.PI * j / N, a1 = 2f * Mathf.PI * (j + 1) / N;
            Vector3 E(float a, float y) => at + new Vector3(ZetonR * Mathf.Cos(a), y, ZetonR * Mathf.Sin(a));
            Vector2 U(float a) => new Vector2(cx + ru * Mathf.Cos(a), cy + rv * Mathf.Sin(a));
            // top (seen from above the picture reads from the player's side: u along +x, v along +z) and bottom
            var top = at + new Vector3(0f, ZetonH, 0f);
            if (Vector3.Dot(Vector3.Cross(E(a1, ZetonH) - top, E(a0, ZetonH) - top), Vector3.up) > 0f) b.TriangleUV(zetonyMat, top, E(a1, ZetonH), E(a0, ZetonH), new Vector2(cx, cy), U(a1), U(a0));
            else b.TriangleUV(zetonyMat, top, E(a0, ZetonH), E(a1, ZetonH), new Vector2(cx, cy), U(a0), U(a1));
            if (Vector3.Dot(Vector3.Cross(E(a0, 0f) - at, E(a1, 0f) - at), Vector3.down) > 0f) b.TriangleUV(zetonyMat, at, E(a0, 0f), E(a1, 0f), new Vector2(cx, cy), U(-a0), U(-a1));
            else b.TriangleUV(zetonyMat, at, E(a1, 0f), E(a0, 0f), new Vector2(cx, cy), U(-a1), U(-a0));
            float u0 = (float)j / N, u1 = (float)(j + 1) / N, v0 = (i * 18 + 1f) / 512f, v1 = (i * 18 + 15f) / 512f;
            var nrm = new Vector3(Mathf.Cos((a0 + a1) * .5f), 0f, Mathf.Sin((a0 + a1) * .5f));
            QuadPrzod(b, zetonyMat, nrm, E(a0, 0f), E(a1, 0f), E(a1, ZetonH), E(a0, ZetonH), new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1));
        }
    }

    // ================================================================ the pictures
    // the owner's picture into a rectangle of ours (an area average; his alpha over what is there); src rows from the bottom
    static void Wklej(Color32[] dst, int w, int h, Color32[] src, int sw, int sh, int sx0, int sy0, int sww, int shh, int x0, int y0, int ww, int hh)
    {
        for (int j = 0; j < hh; j++)
        {
            int ya = sy0 + j * shh / hh, yb = Mathf.Max(ya + 1, sy0 + (j + 1) * shh / hh);
            for (int i = 0; i < ww; i++)
            {
                int xa = sx0 + i * sww / ww, xb = Mathf.Max(xa + 1, sx0 + (i + 1) * sww / ww);
                float r = 0, g = 0, bl = 0, a = 0; int n = 0;
                for (int y = ya; y < yb; y++)
                    for (int x = xa; x < xb; x++)
                    {
                        if (x < 0 || y < 0 || x >= sw || y >= sh) continue;
                        var c = src[y * sw + x]; float al = c.a / 255f; r += c.r * al; g += c.g * al; bl += c.b * al; a += al; n++;
                    }
                int X = x0 + i, Y = y0 + j; if (n == 0 || X < 0 || Y < 0 || X >= w || Y >= h) continue;
                float A = a / n; var d = dst[Y * w + X];
                dst[Y * w + X] = new Color32((byte)Mathf.Clamp(r / n + d.r * (1f - A), 0, 255), (byte)Mathf.Clamp(g / n + d.g * (1f - A), 0, 255), (byte)Mathf.Clamp(bl / n + d.b * (1f - A), 0, 255), 255);
            }
        }
    }
    static Color32 Mieszaj(Color32 a, Color32 b, float t) => new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t), (byte)(a.b + (b.b - a.b) * t), 255);
    static void Kolko(Color32[] px, int w, int h, float cx, float cy, float r, Color32 col)
    {
        for (int y = Mathf.Max(0, (int)(cy - r - 1)); y <= Mathf.Min(h - 1, (int)(cy + r + 1)); y++)
            for (int x = Mathf.Max(0, (int)(cx - r - 1)); x <= Mathf.Min(w - 1, (int)(cx + r + 1)); x++)
            {
                float d = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy)), t = Mathf.Clamp01(r + .5f - d);
                if (t > 0f) px[y * w + x] = Mieszaj(px[y * w + x], col, t);
            }
    }
    // a rounded rectangle (anti-aliased edge)
    static void Zaokraglony(Color32[] px, int w, int h, float x0, float y0, float x1, float y1, float r, Color32 col)
    {
        for (int y = Mathf.Max(0, (int)y0); y < Mathf.Min(h, (int)Mathf.Ceil(y1)); y++)
            for (int x = Mathf.Max(0, (int)x0); x < Mathf.Min(w, (int)Mathf.Ceil(x1)); x++)
            {
                float qx = Mathf.Max(Mathf.Abs(x + .5f - (x0 + x1) * .5f) - ((x1 - x0) * .5f - r), 0f), qy = Mathf.Max(Mathf.Abs(y + .5f - (y0 + y1) * .5f) - ((y1 - y0) * .5f - r), 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - r, t = Mathf.Clamp01(.5f - d);
                if (t > 0f) px[y * w + x] = Mieszaj(px[y * w + x], col, t);
            }
    }
    // a card suit (0 spades, 1 hearts, 2 diamonds, 3 clubs), size its height in px, turned over for the lower half of a card
    static bool WZnaku(int s, float x, float y)
    {
        bool Ko(float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        bool Tr(float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by), d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy), d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }
        switch (s)
        {
            case 1: return Ko(-.235f, .20f, .265f) || Ko(.235f, .20f, .265f) || Tr(-.49f, .13f, .49f, .13f, 0f, -.5f);
            case 2: return Mathf.Abs(x) / .36f + Mathf.Abs(y) / .5f <= 1f;
            case 0: return Ko(-.235f, -.07f, .25f) || Ko(.235f, -.07f, .25f) || Tr(-.47f, -.02f, .47f, -.02f, 0f, .5f) || Tr(0f, -.12f, -.17f, -.5f, .17f, -.5f);
            default: return Ko(0f, .25f, .22f) || Ko(-.245f, -.05f, .22f) || Ko(.245f, -.05f, .22f) || Tr(0f, .06f, -.16f, -.5f, .16f, -.5f);
        }
    }
    static void Znak(Color32[] px, int w, int h, int s, float cx, float cy, float size, bool odwroc, Color32 col)
    {
        int x0 = Mathf.Max(0, (int)(cx - size * .6f)), x1 = Mathf.Min(w - 1, (int)(cx + size * .6f)), y0 = Mathf.Max(0, (int)(cy - size * .6f)), y1 = Mathf.Min(h - 1, (int)(cy + size * .6f));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int n = 0;
                for (int sy = 0; sy < 3; sy++) for (int sx = 0; sx < 3; sx++)
                    {
                        float lx = (x + (sx + .5f) / 3f - cx) / size, ly = (y + (sy + .5f) / 3f - cy) / size;
                        if (odwroc) { lx = -lx; ly = -ly; }
                        if (WZnaku(s, lx, ly)) n++;
                    }
                if (n > 0) px[y * w + x] = Mieszaj(px[y * w + x], col, n / 9f);
            }
    }
    static void Tekst(Color32[] px, int w, int h, string t, float cx, float cy, float cap, float maxW, float deg, Color32 col, float rim = 0f, Color32 rimCol = default)
    {
        if (!Napis(px, w, h, t, cx, cy, cap, maxW, deg, col, rim, rimCol))
            BraxChampionsPiksele.Srodek(px, w, h, t, (int)cx, (int)(cy - 3.5f * Mathf.Max(1, (int)(cap / 7f))), Mathf.Max(1, (int)(cap / 7f)), col);
    }

    // ---- the cards (2048 x 1024): the 52 faces, the back, TRZYMAJ, the hands' names
    static readonly string[] Rangi = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    // the pips (2..10) in the real layout: column -1 / 0 / 1, row 1 (top) .. -1 (bottom)
    static readonly float[][] Oczka =
    {
        new[]{0f,1f, 0f,-1f}, new[]{0f,1f, 0f,0f, 0f,-1f}, new[]{-1f,1f, 1f,1f, -1f,-1f, 1f,-1f}, new[]{-1f,1f, 1f,1f, 0f,0f, -1f,-1f, 1f,-1f},
        new[]{-1f,1f, 1f,1f, -1f,0f, 1f,0f, -1f,-1f, 1f,-1f}, new[]{-1f,1f, 1f,1f, 0f,.5f, -1f,0f, 1f,0f, -1f,-1f, 1f,-1f},
        new[]{-1f,1f, 1f,1f, 0f,.5f, -1f,0f, 1f,0f, 0f,-.5f, -1f,-1f, 1f,-1f},
        new[]{-1f,1f, 1f,1f, -1f,1f/3, 1f,1f/3, 0f,0f, -1f,-1f/3, 1f,-1f/3, -1f,-1f, 1f,-1f},
        new[]{-1f,1f, 1f,1f, 0f,2f/3, -1f,1f/3, 1f,1f/3, -1f,-1f/3, 1f,-1f/3, 0f,-2f/3, -1f,-1f, 1f,-1f},
    };
    static Color32[] KartyAtlas()
    {
        int w = P.AtlasW, h = P.AtlasH; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, PokerTlo);
        string dir = Wlasciciel + "/karty/";
        var rewers = Wczytaj(dir + "rewers.png", out int rw, out int rh);
        var figury = new Color32[3][]; var fw = new int[3]; var fh = new int[3]; string[] fn = { "walet", "dama", "krol" }; int brak = rewers == null ? 1 : 0;
        for (int f = 0; f < 3; f++) { figury[f] = Wczytaj(dir + fn[f] + ".png", out fw[f], out fh[f]); if (figury[f] == null) brak++; }
        KartyWlasciciela = brak == 0;
        if (brak > 0) BraxBuildQuality.Warn("DO POPRAWKI KASYNO_KARTY missing " + brak + "/4 in " + dir + ": those cards are drawn");
        for (int c = 0; c < 52; c++) { P.KartaPx(c, out int x0, out int y0); Karta(px, w, h, c, x0, y0, figury, fw, fh); }
        // the back: his picture on the whole card
        P.RewersPx(out int bx, out int by);
        Zaokraglony(px, w, h, bx + 2, by + 2, bx + P.KartaW - 2, by + P.KartaH - 2, 10f, new Color32(120, 14, 22, 255));
        if (rewers != null) { Wklej(px, w, h, rewers, rw, rh, 0, 0, rw, rh, bx + 2, by + 2, P.KartaW - 4, P.KartaH - 4); RogiKarty(px, w, h, bx, by); }
        else
        {
            Zaokraglony(px, w, h, bx + 10, by + 10, bx + P.KartaW - 10, by + P.KartaH - 10, 6f, new Color32(150, 20, 30, 255));
            Tekst(px, w, h, "B", bx + P.KartaW * .5f, by + P.KartaH * .5f, 60f, 100f, 0f, Zloto);
        }
        P.TrzymajPx(out int tx, out int ty);
        Tekst(px, w, h, "TRZYMAJ", tx + P.TrzymajW * .5f, ty + P.TrzymajH * .5f, 26f, P.TrzymajW - 10, 0f, Zolty, 2f, new Color32(60, 30, 0, 255));
        for (int u = 1; u < P.Uklady.Length; u++)
        {
            P.NapisPx(u, out int nx, out int ny);
            Tekst(px, w, h, P.Uklady[u], nx + P.NapisW * .5f, ny + P.NapisH * .5f, 32f, P.NapisW - 24, 0f, new Color32(255, 206, 72, 255), 3f, new Color32(90, 10, 12, 255));
        }
        return px;
    }
    // the screen's colour round a card's rounded corners
    static void RogiKarty(Color32[] px, int w, int h, int x0, int y0)
    {
        const float r = 10f; float ax = x0 + 2, ay = y0 + 2, bx = x0 + P.KartaW - 2, by = y0 + P.KartaH - 2;
        for (int y = y0; y < y0 + P.KartaH; y++)
            for (int x = x0; x < x0 + P.KartaW; x++)
            {
                float qx = Mathf.Max(Mathf.Abs(x + .5f - (ax + bx) * .5f) - ((bx - ax) * .5f - r), 0f), qy = Mathf.Max(Mathf.Abs(y + .5f - (ay + by) * .5f) - ((by - ay) * .5f - r), 0f);
                float d = Mathf.Sqrt(qx * qx + qy * qy) - r; if (d > -.5f) px[y * w + x] = Mieszaj(px[y * w + x], PokerTlo, Mathf.Clamp01(d + .5f));
            }
    }
    static void Karta(Color32[] px, int w, int h, int c, int x0, int y0, Color32[][] fig, int[] fw, int[] fh)
    {
        int r = P.Ranga(c), s = P.Kolor(c); var col = s == 1 || s == 2 ? KartaCzerwien : KartaCzern;
        int W = P.KartaW, H = P.KartaH;
        Zaokraglony(px, w, h, x0 + 2, y0 + 2, x0 + W - 2, y0 + H - 2, 10f, new Color32(176, 172, 160, 255));
        Zaokraglony(px, w, h, x0 + 3, y0 + 3, x0 + W - 3, y0 + H - 3, 9f, Krem2);
        if (r >= P.Walet && r < P.As)
        {
            // his court picture inside the card's margins (the indices outside it)
            int f = r - P.Walet; const int aw = 106; int ah = Mathf.RoundToInt(aw * 1484f / 1060f), ax = x0 + (W - aw) / 2, ay = y0 + (H - ah) / 2;
            if (fig[f] != null) Wklej(px, w, h, fig[f], fw[f], fh[f], 0, 0, fw[f], fh[f], ax, ay, aw, ah);
            else
            {
                Ramka(px, w, h, ax, ay, ax + aw, ay + ah, 3, Zloto);
                Tekst(px, w, h, Rangi[r], x0 + W * .5f, y0 + H * .5f + 14f, 64f, 80f, 0f, col);
                Znak(px, w, h, s, x0 + W * .5f, y0 + H * .5f - 44f, 30f, false, col);
            }
        }
        else if (r == P.As) Znak(px, w, h, s, x0 + W * .5f, y0 + H * .5f, s == 0 ? 74f : 62f, false, col);
        else
        {
            var o = Oczka[r];
            for (int i = 0; i + 1 < o.Length; i += 2)
                Znak(px, w, h, s, x0 + W * .5f + o[i] * 31f, y0 + H * .5f + o[i + 1] * 60f, 25f, o[i + 1] < -.01f, col);
        }
        // the corner indices on the margin: the rank and the suit, top left and (turned) bottom right
        Tekst(px, w, h, Rangi[r], x0 + 11f, y0 + H - 20f, 18f, r == 8 ? 19f : 16f, 0f, col);
        Znak(px, w, h, s, x0 + 11f, y0 + H - 39f, 12f, false, col);
        Tekst(px, w, h, Rangi[r], x0 + W - 11f, y0 + 20f, 18f, r == 8 ? 19f : 16f, 180f, col);
        Znak(px, w, h, s, x0 + W - 11f, y0 + 39f, 12f, true, col);
        RogiKarty(px, w, h, x0, y0);
    }
    static Color32[] TloPoker()
    {
        const int w = 1024, h = 576; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, PokerTlo);
        float X(float x) => w * .5f + x * Skala; float Y(float y) => h * .5f + y * Skala;
        Ramka(px, w, h, 6, 6, w - 6, h - 6, 3, Zloto);
        Tekst(px, w, h, "JACKS OR BETTER", X(0f), Y(.140f), 30f, 600f, 0f, new Color32(255, 206, 72, 255), 3f, new Color32(70, 8, 10, 255));
        for (int i = 0; i < 5; i++)
        {
            float cx = X((i - 2) * P.Rozstaw), cy = Y(P.KartyY), hw = (P.KartaSz * .5f + .004f) * Skala, hh = (P.KartaWys * .5f + .004f) * Skala;
            Ramka(px, w, h, (int)(cx - hw), (int)(cy - hh), (int)(cx + hw), (int)(cy + hh), 2, new Color32(120, 100, 40, 255));
        }
        int by = (int)Y(LicznikY);
        BraxChampionsPiksele.Prost(px, w, h, 40, by - 24, w - 40, by + 24, new Color32(10, 10, 14, 255)); Ramka(px, w, h, 40, by - 24, w - 40, by + 24, 2, Zloto);
        return px;
    }
    static Color32[] TloKeno()
    {
        const int w = 1024, h = 576; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, KenoTlo);
        float X(float x) => w * .5f + x * Skala; float Y(float y) => h * .5f + y * Skala;
        Ramka(px, w, h, 6, 6, w - 6, h - 6, 3, Zloto);
        Tekst(px, w, h, "KENO · 6 Z 80", X(K.SiatkaX), Y(.149f), 24f, 500f, 0f, new Color32(255, 206, 72, 255), 2f, new Color32(70, 8, 10, 255));
        for (int n = 1; n <= K.Pola; n++)
        {
            float cx = X(K.PoleX(n)), cy = Y(K.PoleY(n)), hw = K.PoleSz * .5f * Skala, hh = K.PoleWys * .5f * Skala;
            Zaokraglony(px, w, h, cx - hw + 2, cy - hh + 2, cx + hw - 2, cy + hh - 2, 7f, new Color32(30, 52, 140, 255));
            Tekst(px, w, h, n.ToString(), cx, cy, 22f, hw * 1.6f, 0f, new Color32(190, 205, 255, 255));
        }
        foreach (var (label, y) in new[] { ("TRAFIONE", K.TrafY), ("KULE", K.KuleY) })
        {
            Tekst(px, w, h, label, X(K.PanelX), Y(y + .036f), 15f, 140f, 0f, new Color32(255, 206, 72, 255));
            int x0 = (int)X(K.PanelX - .042f), x1 = (int)X(K.PanelX + .042f), y0 = (int)Y(y - .024f), y1 = (int)Y(y + .024f);
            Ramka(px, w, h, x0, y0, x1, y1, 2, Zloto);
        }
        Tekst(px, w, h, "Z 20", X(K.PanelX), Y(K.KuleY - .040f), 13f, 120f, 0f, new Color32(150, 160, 200, 255));
        int by = (int)Y(LicznikY);
        BraxChampionsPiksele.Prost(px, w, h, 40, by - 24, w - 40, by + 24, new Color32(10, 10, 14, 255)); Ramka(px, w, h, 40, by - 24, w - 40, by + 24, 2, Zloto);
        return px;
    }
    static Color32[] KenoAtlas()
    {
        int w = K.AtlasW, h = K.AtlasH; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, KenoTlo);
        Color32[] tlo = { new Color32(236, 186, 60, 255), new Color32(238, 240, 246, 255), new Color32(214, 28, 40, 255) };
        Color32[] cyf = { new Color32(36, 22, 6, 255), new Color32(20, 30, 90, 255), new Color32(255, 255, 255, 255) };
        for (int st = 0; st < 3; st++)
            for (int n = 1; n <= K.Pola; n++)
            {
                K.PolePx(n, st, out int x0, out int y0);
                if (st == K.Trafiony) Zaokraglony(px, w, h, x0 + 1, y0 + 1, x0 + K.PoleW - 1, y0 + K.PoleH - 1, 9f, Zolty);
                Zaokraglony(px, w, h, x0 + (st == K.Trafiony ? 4 : 2), y0 + (st == K.Trafiony ? 4 : 2), x0 + K.PoleW - (st == K.Trafiony ? 4 : 2), y0 + K.PoleH - (st == K.Trafiony ? 4 : 2), 7f, tlo[st]);
                Tekst(px, w, h, n.ToString(), x0 + K.PoleW * .5f, y0 + K.PoleH * .5f, 20f, K.PoleW - 18, 0f, cyf[st]);
            }
        for (int d = 0; d < 10; d++) { K.CyfraPx(d, out int x0, out int y0); Tekst(px, w, h, d.ToString(), x0 + K.CyfraW * .5f, y0 + K.CyfraH * .5f, 44f, K.CyfraW - 4, 0f, Biel); }
        return px;
    }
    static Color32[] TabelaPoker()
    {
        const int w = 1024, h = 576; var px = TloGradient(w, h); Ramka(px, w, h, 8, 8, w - 8, h - 8, 3, Zloto);
        Tekst(px, w, h, "VIDEO POKER · JACKS OR BETTER", w * .5f, 526f, 30f, 900f, 0f, Zloto, 2f, new Color32(60, 8, 10, 255));
        Tekst(px, w, h, "STAWKA " + Stawka + " · WYGRANA W KREDYTACH", w * .5f, 490f, 15f, 700f, 0f, Szary);
        for (int u = P.Krolewski, row = 0; u >= 1; u--, row++)
        {
            float y = 448f - row * 44f; var col = u == P.Krolewski ? Zloto : Biel;
            Tekst(px, w, h, P.Uklady[u], 300f, y, 24f, 480f, 0f, col);
            Tekst(px, w, h, A.Kwota((long)P.Wyplata[u] * Stawka), 790f, y, 26f, 300f, 0f, col);
            if (row < 8) BraxChampionsPiksele.Prost(px, w, h, 60, (int)(y - 22f), w - 60, (int)(y - 21f), new Color32(60, 66, 90, 255));
        }
        Tekst(px, w, h, "DOTKNIJ KARTY: TRZYMAJ · WYMIEŃ: NOWE KARTY", w * .5f, 32f, 14f, 900f, 0f, Szary);
        return px;
    }
    static Color32[] TabelaKeno()
    {
        const int w = 1024, h = 576; var px = TloGradient(w, h); Ramka(px, w, h, 8, 8, w - 8, h - 8, 3, Zloto);
        Tekst(px, w, h, "KENO · 6 Z 80", w * .5f, 520f, 40f, 800f, 0f, Zloto, 2f, new Color32(60, 8, 10, 255));
        Tekst(px, w, h, "STAWKA " + Stawka + " · WYGRANA W KREDYTACH", w * .5f, 474f, 15f, 700f, 0f, Szary);
        Tekst(px, w, h, "TRAFIONE", 300f, 420f, 18f, 300f, 0f, Szary); Tekst(px, w, h, "WYGRANA", 760f, 420f, 18f, 300f, 0f, Szary);
        for (int t = K.Typy, row = 0; t >= 3; t--, row++)
        {
            float y = 360f - row * 70f; var col = t == K.Typy ? Zloto : Biel;
            Tekst(px, w, h, t + " Z 6", 300f, y, 40f, 300f, 0f, col);
            Tekst(px, w, h, A.Kwota((long)K.Wyplata[t] * Stawka), 760f, y, 40f, 400f, 0f, col);
        }
        Tekst(px, w, h, "SKREŚL 6 LICZB NA EKRANIE · LOSUJE 20 KUL Z 80", w * .5f, 32f, 15f, 900f, 0f, Szary);
        return px;
    }
    // the wheel machine's pay glass: the stepper's table with its own SEVENS and the wheel's row
    static Color32[] TabelaKolo(int t)
    {
        const int w = 1024, h = 512; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, new Color32(12, 8, 10, 255));
        Ramka(px, w, h, 8, 8, w - 8, h - 8, 3, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "KOŁO MISTRZÓW · TABELA WYGRANYCH", w / 2, 462, 4, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "STAWKA " + Stawka + " · WYGRANE W KREDYTACH", w / 2, 434, 2, Szary);
        var wiersze = new List<(int[] s, string v)>
        {
            (new[]{0,0,0}, (KG.Siodemki*Stawka).ToString()), (new[]{1,1,1}, (Z.Trzy[1]*Stawka).ToString()), (new[]{3,3,3}, (Z.Trzy[3]*Stawka).ToString()),
            (new[]{4,4,4}, (Z.Trzy[4]*Stawka).ToString()), (new[]{5,5,5}, (Z.Trzy[5]*Stawka).ToString()), (new[]{6,6,6}, (Z.Trzy[6]*Stawka).ToString()),
            (new[]{6,6,-1}, (Z.Wisnie2*Stawka).ToString()), (new[]{-1,-1,KG.Symbol}, "KOŁO"),
        };
        for (int i = 0; i < wiersze.Count; i++)
        {
            int col = i / 4, row = i % 4, x = 40 + col * 500, y = 338 - row * 80;
            for (int k = 0; k < 3; k++)
            {
                int s = wiersze[i].s[k], sx = x + k * 72;
                if (s >= 0) { Symbolik(px, w, h, t, s, sx, y, 66); Ramka(px, w, h, sx - 2, y - 2, sx + 68, y + 68, 2, Zloto); }
                else Ramka(px, w, h, sx - 2, y - 2, sx + 68, y + 68, 2, Szary);
            }
            BraxChampionsPiksele.Tekst(px, w, h, "= " + wiersze[i].v, x + 228, y + 19, 4, i == 0 || i == 7 ? Zloto : Biel);
        }
        int min = int.MaxValue, max = 0; foreach (int v in KG.Segmenty) { min = Mathf.Min(min, v); max = Mathf.Max(max, v); }
        BraxChampionsPiksele.Srodek(px, w, h, "KOŁO NA 3. BĘBNIE KRĘCI KOŁEM: " + min * Stawka / 10 + " - " + max * Stawka / 10 + " · 1 WIŚNIA = " + Z.Wisnia1 * Stawka, w / 2, 18, 2, Szary);
        return px;
    }
    // the KOŁO symbol on the drum (the atlas' eighth cell, 512 x 256): a little wheel of the theme on black, a gold frame
    static void KomorkaKolo(Color32[] px, int t)
    {
        Z.KomorkaPx(KG.Symbol, out int x0, out int y0); int w = Z.AtlasW, h = Z.AtlasH;
        BraxChampionsPiksele.Prost(px, w, h, x0, y0, x0 + Z.KomorkaW, y0 + Z.KomorkaH, new Color32(8, 6, 12, 255));
        Ramka(px, w, h, x0 + 30, y0 + 4, x0 + Z.KomorkaW - 30, y0 + Z.KomorkaH - 4, 4, new Color32(150, 116, 40, 255));
        var c = Swiatla[t]; float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b)); var kol = new Color32((byte)(255 * c.r / mx), (byte)(255 * c.g / mx), (byte)(255 * c.b / mx), 255);
        float cx = x0 + Z.KomorkaW * .5f, cy = y0 + Z.KomorkaH * .5f, R = 96f;
        for (int y = (int)(cy - R - 8); y <= cy + R + 8; y++)
            for (int x = (int)(cx - R - 8); x <= cx + R + 8; x++)
            {
                float dx = x + .5f - cx, dy = y + .5f - cy, d = Mathf.Sqrt(dx * dx + dy * dy); if (d > R + 7f) continue;
                float f = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg, 360f); int seg = (int)(f / 30f);
                Color32 o = d > R ? Zloto : d < 18f ? new Color32(200, 24, 32, 255) : seg % 2 == 0 ? kol : new Color32(236, 186, 74, 255);
                if (d <= R && d >= 18f && Mathf.Abs(Mathf.Repeat(f + 15f, 30f) - 15f) * Mathf.Deg2Rad * d < 1.6f) o = new Color32(30, 20, 10, 255);
                px[y * w + x] = Mieszaj(px[y * w + x], o, Mathf.Clamp01(R + 7.5f - d));
            }
        Trojkat(px, w, h, new Vector2(cx, cy + R - 14f), new Vector2(cx - 13f, cy + R + 14f), new Vector2(cx + 13f, cy + R + 14f), 0f, new Color32(220, 20, 30, 255));
    }
    // the wheel's face (1024 x 1024 for the 62 cm wheel): 22 segments clockwise from the top in four colours (the 500 black
    // and gold), gold borders, the credits along the radius
    static Color32[] KoloTarczaPx()
    {
        const int w = 1024, h = 1024; const float R = 500f, c = 512f; var px = new Color32[w * h];
        int n = KG.Segmenty.Length; float D = 360f / n;
        Color32[] paleta = { new Color32(196, 24, 36, 255), new Color32(24, 62, 176, 255), new Color32(18, 128, 58, 255), new Color32(118, 34, 156, 255) };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x + .5f - c, dy = y + .5f - c, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > R) { px[y * w + x] = new Color32(10, 8, 12, 255); continue; }
                float f = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg, 360f); int s = (int)(Mathf.Repeat(f + D * .5f, 360f) / D) % n;
                Color32 o = s == 0 ? new Color32(16, 16, 20, 255) : paleta[(s - 1) % 4];
                float brzeg = Mathf.Abs(Mathf.Repeat(f + D * .5f, D) - D * .5f);       // the angle to the segment's middle
                if ((D * .5f - brzeg) * Mathf.Deg2Rad * d < 3.0f) o = Zloto;
                if (d > .93f * R) o = d > .965f * R ? Zloto : new Color32(150, 110, 30, 255);
                if (d < .13f * R) o = Zloto;
                px[y * w + x] = o;
            }
        for (int s = 0; s < n; s++)
        {
            float f = s * D * Mathf.Deg2Rad, deg = Mathf.Atan2(-Mathf.Cos(f), -Mathf.Sin(f)) * Mathf.Rad2Deg;
            Tekst(px, w, h, KG.Segmenty[s].ToString(), c + .62f * R * Mathf.Sin(f), c + .62f * R * Mathf.Cos(f), 64f, .54f * R, deg, s == 0 ? Zloto : Biel, 3f, new Color32(20, 10, 6, 255));
        }
        return px;
    }
    // a game's topper: the owner's (1:1, his transparency over the topper box's black); without it his game's name drawn
    static Color32[] Topper(int g, string plik, ref int brak)
    {
        const int w = 1536, h = 512; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, new Color32(8, 6, 8, 255));
        var src = Wczytaj(Wlasciciel + "/toppery/" + plik + ".png", out int sw, out int sh);
        if (src != null) { Wklej(px, w, h, src, sw, sh, 0, 0, sw, sh, 0, 0, w, h); return px; }
        brak++;
        Ramka(px, w, h, 10, 10, w - 10, h - 10, 8, Zloto);
        Tekst(px, w, h, G.Nazwy[g], w * .5f, h * .5f, 150f, w - 120, 0f, Zloto, 6f, new Color32(90, 10, 12, 255));
        return px;
    }
}

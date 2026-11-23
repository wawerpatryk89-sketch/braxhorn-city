using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using L = BraxChampionsLototo;

// MIASTO MISTRZÓW - LOTOTO BRAX CASH (owner 16.11 22:52-23:08): the draw studio and the kolektura, built in the BRAXHORN BET
// (BraxChampionsWiezowce.Bet) with the casino kit's materials and painters. The studio as a TV lottery studio is: a set
// wall in dark blue with the owner's logo back-lit (his poster's logo, cut 1:1), the draw machine - a steel pedestal, a
// clear glass drum of 72 cm with 60 balls of 6 cm (gold, a white disc with the number on two sides), the outlet at its top,
// the chute (two chrome rails) down to the rail of 20 cups (two rows of ten) in front of it -, the results screen, the host,
// by the machine, a TV camera on its tripod, two softboxes, glass walls in a black frame. The kolektura: a walnut counter with a marble top,
// the owner's logo on its front, the terminal, the coupon printer, the coupon desk, his poster on a stand. The game:
// BraxChampionsLototo (the camera's picture goes to every screen of the BET).
public static partial class BraxChampionsKasynoSprzet
{
    public const string LototoWlasciciel = "Assets/Source/Mistrzowie/LototoWlasciciel";
    // his poster (16.11 23:06, 1024 x 1536): the logo LOTOTO / BRAX CASH (x, y from the top-left, w, h)
    static readonly int[] LototoLogo = { 118, 40, 790, 330 };
    public static bool LototoWlasciciela;

    static Material lotKule, lotTablica, lotTablicaTlo, lotLogo, lotPlakat, lotEkran, lotGranat, lotSoftbox, lotKupon;
    static Mesh[] lotSiatki; static float[] lotSzer;
    // the balls of both games (LOTOTO BRAX CASH 60, BRAX SZÓSTKA 40): one atlas, one mesh per number
    static int MaxKul => Mathf.Max(L.Zasady.BraxCash.pula, L.Zasady.BraxSzostka.pula);

    static void LototoMaterialy()
    {
        Przygotuj();
        // the balls: 60 cells of 256 x 128 (the ball's equirectangular skin): gold, a white disc with the number in navy at
        // the front (+z) and the back (-z)
        const int CW = 256, CH = 128, AW = 2048, AH = 1024;
        var px = new Color32[AW * AH];
        var zl = new Color32(214, 168, 52, 255); BraxChampionsPiksele.Wypelnij(px, zl);
        for (int n = 1; n <= MaxKul; n++)
        {
            int x0 = (n - 1) % 8 * CW, y0 = (n - 1) / 8 * CH;
            for (int y = 0; y < CH; y++) for (int x = 0; x < CW; x++)
            {
                float v = (y + .5f) / CH;                                              // a highlight band round the equator
                byte g = (byte)Mathf.Clamp(150 + 105 * Mathf.Sin(v * Mathf.PI), 0, 255);
                px[(y0 + y) * AW + x0 + x] = new Color32((byte)Mathf.Min(255, g + 40), g, (byte)(g * .35f), 255);
            }
            foreach (float u in new[] { .25f, .75f })
            {
                float cx = x0 + u * CW, cy = y0 + CH * .5f;
                Kolko(px, AW, AH, cx, cy, 25f, new Color32(250, 250, 252, 255));
                Tekst(px, AW, AH, n.ToString(), cx, cy, 20f, 40f, 0f, new Color32(16, 22, 64, 255));
            }
        }
        var kt = Png("lototo_kule_atlas", AW, AH, px, true);
        lotKule = BraxChampionsHarbour.Surface("kasyno_lototo_kule", Color.white, .14f, 0f); lotKule.SetTexture("_MainTex", kt); Wnetrze(lotKule);
        lotSiatki = new Mesh[MaxKul];
        for (int n = 1; n <= MaxKul; n++) lotSiatki[n - 1] = ZapiszSiatke(KulaSiatka((n - 1) % 8, (n - 1) / 8, .03f), "lototo_kula_" + n.ToString("00"));
        // the board's atlas (BraxChampionsLototo.Tablica)
        int tw = L.Tablica.W, th = L.Tablica.H; var tp = new Color32[tw * th];
        var granat = new Color32(12, 20, 64, 255); BraxChampionsPiksele.Wypelnij(tp, granat);
        for (int d = 0; d < 12; d++)
        {
            var r = L.Tablica.Cyfra(d);
            Tekst(tp, tw, th, d < 10 ? d.ToString() : d == 10 ? ":" : ".", r.x + r.width * .5f, r.y + r.height * .5f, 78f, r.width - 6f, 0f, Biel);
        }
        for (int n = 1; n <= MaxKul + 1; n++)
        {
            var r = n <= MaxKul ? L.Tablica.Kula(n) : L.Tablica.Pusta; float cx = r.x + 64f, cy = r.y + 64f;
            Kolko(tp, tw, th, cx, cy, 60f, n <= MaxKul ? Zloto : new Color32(40, 52, 110, 255));
            Kolko(tp, tw, th, cx, cy, 50f, n <= MaxKul ? new Color32(250, 250, 252, 255) : new Color32(24, 34, 88, 255));
            if (n <= MaxKul) Tekst(tp, tw, th, n.ToString(), cx, cy, 46f, 84f, 0f, granat);
        }
        for (int i = 0; i < L.Tablica.Slowa.Length; i++)
        {
            var r = L.Tablica.Slowo(i);
            Tekst(tp, tw, th, L.Tablica.Slowa[i], r.x + r.width * .5f, r.y + r.height * .5f, 64f, r.width - 20f, 0f, i == L.Tablica.NaZywo ? new Color32(255, 70, 60, 255) : Zloto);
        }
        // each word's ink width (the runtime sizes its quad to it: the cell's navy round a word would cover its neighbours)
        lotSzer = new float[L.Tablica.Slowa.Length];
        for (int i = 0; i < lotSzer.Length; i++)
        {
            var r = L.Tablica.Slowo(i); int x0 = r.width, x1 = -1;
            for (int y = r.y; y < r.y + r.height; y += 2) for (int x = 0; x < r.width; x++)
            { var c = tp[y * tw + r.x + x]; if (Mathf.Abs(c.r - granat.r) + Mathf.Abs(c.g - granat.g) + Mathf.Abs(c.b - granat.b) > 30) { if (x < x0) x0 = x; if (x > x1) x1 = x; } }
            lotSzer[i] = x1 >= x0 ? 2f * Mathf.Max(r.width * .5f - x0, x1 + 1 - r.width * .5f) / r.width : 1f;
        }
        var atlas = Png("lototo_tablica_atlas", tw, th, tp, true);
        lotTablica = Ekran("lototo_tablica", atlas, 1.0f);
        // 23.11 (owner's photo: balls cut in half, some missing): the board's background is its own submesh with a material
        // that writes no depth, drawn before the numbers (queue 2001 < 2002); the numbers lie in the same plane over it
        lotTablicaTlo = Ekran("lototo_tablica_tlo", atlas, 1.0f);
        lotTablicaTlo.SetFloat("_ZWrite", 0f); lotTablicaTlo.renderQueue = 2001; lotTablica.renderQueue = 2002;
        EditorUtility.SetDirty(lotTablicaTlo); EditorUtility.SetDirty(lotTablica);
        // his poster and its logo (his art 1:1); without the pack the name in the region's letters
        var src = Wczytaj(LototoWlasciciel + "/plakaty/lototo_brax_cash.png", out int sw, out int sh);
        LototoWlasciciela = src != null;
        if (src == null) BraxBuildQuality.Warn("DO POPRAWKI LOTOTO_PLAKAT missing " + LototoWlasciciel + "/plakaty (pack braxhorn-mistrzowie-lototo-20261116): the name in drawn letters");
        int lw = 1024, lh = Mathf.RoundToInt(1024f * LototoLogo[3] / LototoLogo[2]); var lp = new Color32[lw * lh]; BraxChampionsPiksele.Wypelnij(lp, granat);
        if (src != null) Wklej(lp, lw, lh, src, sw, sh, LototoLogo[0], sh - LototoLogo[1] - LototoLogo[3], LototoLogo[2], LototoLogo[3], 0, 0, lw, lh);
        else Tekst(lp, lw, lh, "LOTOTO BRAX CASH", lw * .5f, lh * .5f, lh * .3f, lw - 60, 0f, Zloto);
        lotLogo = Ekran("lototo_logo", Png("lototo_logo", lw, lh, lp, true), 1.05f);
        if (src != null)
        {
            var pp = new Color32[1024 * 1536]; Wklej(pp, 1024, 1536, src, sw, sh, 0, 0, sw, sh, 0, 0, 1024, 1536);
            lotPlakat = Ekran("lototo_plakat", Png("lototo_plakat", 1024, 1536, pp, true), .95f);
        }
        // the screens' picture (the studio camera, given at runtime), the set's lacquer, the softboxes, the coupons
        lotEkran = Ekran("lototo_transmisja", Png("lototo_transmisja_start", 64, 36, Jednolity(64, 36, granat), false), 1.0f);
        lotGranat = Metal("lototo_lakier_granat", new Color(.05f, .08f, .24f), .22f, 0f);
        lotSoftbox = Swiatlo("lototo_softbox", new Color(1.5f, 1.48f, 1.42f));
        var kp = new Color32[256 * 512]; BraxChampionsPiksele.Wypelnij(kp, new Color32(250, 248, 240, 255));
        // the BET's coupon: the 60 numbers ten a row, as on a real coupon of a game of this kind
        for (int i = 0; i < L.Zasady.BraxCash.pula; i++)
        {
            float cx = 19 + (i % 10) * 24.2f, cy = 470 - (i / 10) * 34f;
            Ramka(kp, 256, 512, (int)cx - 10, (int)cy - 13, (int)cx + 10, (int)cy + 13, 1, new Color32(200, 40, 40, 255));
            Tekst(kp, 256, 512, (i + 1).ToString(), cx, cy, 10f, 18f, 0f, new Color32(200, 40, 40, 255));
        }
        Tekst(kp, 256, 512, "LOTOTO BRAX CASH", 128, 70, 18f, 230f, 0f, new Color32(16, 22, 64, 255));
        lotKupon = BraxChampionsHarbour.Surface("kasyno_lototo_kupon", Color.white, .9f, 0f); lotKupon.SetTexture("_MainTex", Png("lototo_kupon", 256, 512, kp, true)); Wnetrze(lotKupon);
    }
    static Color32[] Jednolity(int w, int h, Color32 c) { var p = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(p, c); return p; }

    // a ball: a UV sphere of radius r whose skin is the atlas cell (cx, cy) of 256 x 128 in 2048 x 1024; u round from +x
    // through +z (the number at u .25 faces +z, at .75 -z), v from the bottom pole
    static Mesh KulaSiatka(int cx, int cy, float r)
    {
        const int S = 16, R = 10; var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= R; j++)
        {
            float th = Mathf.PI * j / R;                                                     // from the top pole
            for (int i = 0; i <= S; i++)
            {
                float ph = 2f * Mathf.PI * i / S; var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                v.Add(d * r); n.Add(d);
                // the viewer's right from +z is -x: u grows from +x (0) through +z (.25) to -x (.5)
                uv.Add(new Vector2((cx * 256f + 2f + (float)i / S * 252f) / 2048f, (cy * 128f + 2f + (1f - (float)j / R) * 124f) / 1024f));
            }
        }
        for (int j = 0; j < R; j++) for (int i = 0; i < S; i++)
        {
            int a = j * (S + 1) + i, b = a + S + 1;
            t.Add(a); t.Add(a + 1); t.Add(b); t.Add(a + 1); t.Add(b + 1); t.Add(b);
        }
        var m = new Mesh { name = "lototo kula" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    /// <summary>The LOTOTO BRAX CASH studio (its floor centre s, its front - the glass to the hall - to yaw; 6.8 x 4.4 m, 3.4 m
    /// high) and the kolektura (the counter's customer side at lada facing ladaYaw; the coupon desk at pulpit; the poster
    /// stand at plakat), under root (nominal metres, root at the origin while building); b = the batch of the static parts.
    /// The screens get the studio's picture. gra 0 = LOTOTO BRAX CASH (60 balls, 20 drawn - the BET), 1 = BRAX SZÓSTKA (40, 6 -
    /// the kolektura of the city R1, for its own builder: without the owner's poster of that game, no LOTOTO poster).</summary>
    public static L Lototo(Transform root, BraxChampionsMeshBatch b, Vector3 s, float yaw, Vector3 lada, float ladaYaw, Vector3 pulpit, float pulpitYaw,
        Vector3 plakat, float plakatYaw, IList<Renderer> ekrany, int gra = 0)
    {
        LototoMaterialy();
        var Z = L.Zasady.Gra(gra);
        if (gra != 0) { lotPlakat = null; BraxBuildQuality.Warn("DO POPRAWKI " + Z.nazwa + "_PLAKAT: the owner's poster of this game is not in the region (the LOTOTO poster is not used)"); }
        var q = Quaternion.Euler(0f, yaw, 0f); Vector3 P(float x, float y, float z) => s + q * new Vector3(x, y, z);
        const float HX = 3.4f, HZ = 2.2f, H = 3.4f;
        var g = new GameObject("LOTOTO BRAX CASH | studio losowań i kolektura (BraxChampionsLototo)").transform; g.SetParent(root, false);
        var lot = g.gameObject.AddComponent<L>(); lot.gra = gra; lot.nazwa = Z.nazwa;
        // ---- the studio: the floor, the set wall, the ceiling, the glass in its black frame
        b.Box(czern, P(0f, .015f, .08f), new Vector3(2f * HX, .03f, 2f * HZ - .16f), q, false);                  // up to the set wall
        W.Kolizja(P(0f, .015f, 0f), new Vector3(2f * HX, .03f, 2f * HZ), q);
        b.Box(lotGranat, P(0f, H * .5f, -HZ + .08f), new Vector3(2f * HX, H, .16f), q);
        b.Box(czern, P(0f, H + .03f, 0f), new Vector3(2f * HX + .1f, .06f, 2f * HZ + .1f), q);
        if (gra == 0) Obraz(b, lotLogo, P(-.7f, 2.62f, -HZ + .16f + Przed), 2.2f, 2.2f * LototoLogo[3] / LototoLogo[2], q);
        foreach (var (a, c, open) in new[] { (new Vector2(-HX, HZ), new Vector2(HX, HZ), false), (new Vector2(HX, -HZ + .16f), new Vector2(HX, HZ), false), (new Vector2(-HX, -HZ + .16f), new Vector2(-HX, HZ), false) })
        {
            var A = P(a.x, 0f, a.y); var C = P(c.x, 0f, c.y); var e = C - A; float len = e.magnitude; var qe = Quaternion.LookRotation(e.normalized);
            b.QuadUV(szklo, A + Vector3.up * .06f, C + Vector3.up * .06f, C + Vector3.up * (H - .06f), A + Vector3.up * (H - .06f), Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            int nm = Mathf.CeilToInt(len / 1.2f);
            for (int k = 0; k <= nm; k++) { var m = Vector3.Lerp(A, C, (float)k / nm); b.Box(czern, m + Vector3.up * H * .5f, new Vector3(.06f, H, .06f), qe); }
            b.Box(czern, (A + C) * .5f + Vector3.up * .03f, new Vector3(.06f, .06f, len), qe);
            W.Kolizja((A + C) * .5f + Vector3.up * H * .5f, new Vector3(.10f, H, len), qe);
        }
        // ---- the machine: the pedestal, the glass drum, the outlet collar, the chute, the rail of cups (one per ball drawn)
        var m0 = new Vector3(-.9f, 0f, -.9f); float bR = .36f, cY = 1.30f;
        b.Cylinder(stal, P(m0.x, 0f, m0.z), .34f, .05f, 28, q, true);
        b.Cylinder(stal, P(m0.x, .05f, m0.z), .22f, .80f, 24, q, true, false, .18f);
        b.Cylinder(chrom, P(m0.x, .85f, m0.z), .26f, .04f, 28, q, true);
        for (int k = 0; k < 16; k++)
        {
            float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; float rr = .20f, yy = cY - bR * .92f;
            b.Tube(chrom, P(m0.x + Mathf.Cos(a0) * rr, yy, m0.z + Mathf.Sin(a0) * rr), P(m0.x + Mathf.Cos(a1) * rr, yy, m0.z + Mathf.Sin(a1) * rr), .018f, 8);
        }
        b.Cylinder(chrom, P(m0.x, .89f, m0.z), .045f, cY - bR - .89f + .01f, 12, q, false);              // the air pipe into the drum
        var drum = new GameObject("bęben (szkło, " + Z.pula + " kul)").transform; drum.SetParent(g, false); drum.localPosition = P(m0.x, cY, m0.z); drum.localRotation = q;
        Kula(b, szklo, P(m0.x, cY, m0.z), bR, 28, 18);
        b.Cylinder(chrom, P(m0.x, cY + bR - .02f, m0.z), .055f, .07f, 18, q, true);                     // the outlet collar
        lot.kule = new Transform[Z.pula];
        // the balls at rest in the drum's bottom before the draw - layers of rings against the glass (the owner's look at the
        // first photo, 16.11 23:42: every ball stood in the drum's centre, it read as one ball); the game re-spreads them
        var stos = new List<Vector3>(); float rb = .03f, lim = bR - rb - .002f; int warstwa = 0;
        for (float y = -lim; stos.Count < Z.pula && y < 0f; y += rb * 1.75f, warstwa++)
        {
            float rr = Mathf.Sqrt(Mathf.Max(0f, lim * lim - y * y));
            for (float ring = rr; ring > -.001f && stos.Count < Z.pula; ring -= 2f * rb)
            {
                int ile = ring < rb ? 1 : Mathf.FloorToInt(2f * Mathf.PI * ring / (2.05f * rb));
                for (int i = 0; i < ile && stos.Count < Z.pula; i++) { float a = 2f * Mathf.PI * (i + .5f * warstwa) / ile; stos.Add(new Vector3(Mathf.Cos(a) * ring, y, Mathf.Sin(a) * ring)); }
            }
        }
        var losKul = new System.Random(Z.pula * 13 + Z.losowane);
        for (int n = 1; n <= Z.pula; n++)
        {
            var o = new GameObject("kula " + n); o.transform.SetParent(drum, false);
            o.transform.localPosition = stos[(n * 37) % Z.pula];                 // the numbers mixed through the pile
            o.transform.localRotation = Quaternion.Euler(losKul.Next(360), losKul.Next(360), losKul.Next(360));
            o.AddComponent<MeshFilter>().sharedMesh = lotSiatki[n - 1]; Cienie(o.AddComponent<MeshRenderer>()).sharedMaterial = lotKule;
            lot.kule[n - 1] = o.transform;
        }
        // the way out: up out of the collar, over and down the chute to the rail's start (studio frame)
        var tor = new[] { new Vector3(m0.x, cY + bR + .06f, m0.z), new Vector3(m0.x + .14f, cY + bR + .12f, m0.z + .06f), new Vector3(m0.x + .42f, cY + .10f, m0.z + .30f),
                          new Vector3(m0.x + .62f, 1.12f, m0.z + .52f) };
        lot.tor = new Transform[tor.Length];
        for (int i = 0; i < tor.Length; i++) { var t = new GameObject("tor " + i).transform; t.SetParent(g, false); t.localPosition = P(tor[i].x, tor[i].y, tor[i].z); lot.tor[i] = t; }
        for (int i = 0; i + 1 < tor.Length; i++)
        {
            var d = (tor[i + 1] - tor[i]).normalized; var side = Vector3.Cross(Vector3.up, d).normalized * .022f; var down = Vector3.down * .022f;
            foreach (float sg in new[] { -1f, 1f }) b.Tube(chrom, P(tor[i].x, tor[i].y, tor[i].z) + q * (side * sg + down), P(tor[i + 1].x, tor[i + 1].y, tor[i + 1].z) + q * (side * sg + down), .005f, 6);
        }
        // the rail: the cups along the studio's x in front of the drum, numbered by place - ten a row (20 drawn: the second
        // row behind and 4 cm higher, seen by the camera over the first), each row on its steel plate with its two legs
        lot.miejsca = new Transform[Z.losowane]; float rx0 = m0.x + .62f, ry = 1.12f, rz = m0.z + .52f;
        int wRz = Mathf.Min(10, Z.losowane), rzedy = (Z.losowane + wRz - 1) / wRz; float krok = Mathf.Min(.12f, .75f / wRz);
        for (int r = 0; r < rzedy; r++)
        {
            float y = ry + r * .04f, z = rz - r * .085f;
            b.Box(stal, P(rx0 + .36f, y - .05f, z), new Vector3(.86f, .03f, .07f), q);
            foreach (float sx in new[] { rx0 - .02f, rx0 + .74f }) b.Cylinder(stal, P(sx, 0f, z), .02f, y - .065f, 10, q, false);
        }
        for (int k = 0; k < Z.losowane; k++)
        {
            int r = k / wRz, kk = k % wRz;
            var c = new Vector3(rx0 + .36f + (kk - (wRz - 1) * .5f) * krok, ry + r * .04f, rz - r * .085f);
            b.Cylinder(chrom, P(c.x, c.y - .035f, c.z), .028f, .012f, 16, q, false);
            var t = new GameObject("miejsce " + (k + 1)).transform; t.SetParent(g, false); t.localPosition = P(c.x, c.y, c.z); lot.miejsca[k] = t;
        }
        // ---- the results screen on the set wall (its quads at runtime), its black bezel
        float bw = 2.4f, bh = 1.35f; var bc = new Vector3(1.75f, 1.55f, -HZ + .20f);
        // the bezel's face 2.7 cm behind the screen (was 1.2): no depth fight between them; its face 2 cm off the wall, its back sunk into it
        b.Box(czern, P(bc.x, bc.y, bc.z - .045f), new Vector3(bw + .08f, bh + .08f, .05f), q);
        var tab = new GameObject("tablica wyników (LOTOTO)"); tab.transform.SetParent(g, false); tab.transform.localPosition = P(bc.x, bc.y, bc.z + Przed);
        tab.transform.localRotation = q;
        var tabR = tab.AddComponent<MeshRenderer>(); tabR.sharedMaterials = new[] { lotTablicaTlo, lotTablica };   // submesh 0 = background
        tabR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tabR.receiveShadows = false;           // a lit screen
        lot.tablica = tab.AddComponent<MeshFilter>(); lot.tablicaW = bw; lot.tablicaH = bh; lot.slowaSzer = lotSzer;
        // ---- the host, the TV camera on its tripod, the softboxes, the light
        lot.prowadzacy = W.Obsada("prowadzący losowania LOTOTO BRAX CASH", P(.05f, 0f, -.55f), yaw - 20f);   // to the camera, a little to the machine
        var kamPos = new Vector3(.25f, 0f, 1.55f);
        foreach (float a in new[] { 0f, 120f, 240f }) { var f = Quaternion.Euler(0f, a, 0f) * Vector3.forward * .32f; b.Tube(czern, P(kamPos.x + f.x, 0f, kamPos.z + f.z), P(kamPos.x, 1.15f, kamPos.z), .014f, 6); }
        b.RoundedBox(czern, P(kamPos.x, 1.30f, kamPos.z + .05f), new Vector3(.20f, .24f, .42f), .02f, q, 2);
        b.Cylinder(czern, P(kamPos.x, 1.32f, kamPos.z - .16f), .07f, .16f, 16, q * Quaternion.Euler(-90f, 0f, 0f), true);
        foreach (float sx in new[] { -2.6f, 2.7f })
        {
            b.Tube(czern, P(sx, 0f, .3f), P(sx, 2.1f, .3f), .02f, 8);
            b.Box(czern, P(sx, 2.25f, .3f), new Vector3(.62f, .62f, .30f), q * Quaternion.Euler(20f, sx < 0f ? 35f : -35f, 0f));
            b.Box(lotSoftbox, P(sx, 2.25f, .3f) + q * (Quaternion.Euler(20f, sx < 0f ? 35f : -35f, 0f) * new Vector3(0f, 0f, -.16f)), new Vector3(.56f, .56f, .02f), q * Quaternion.Euler(20f, sx < 0f ? 35f : -35f, 0f));
        }
        W.Lampa("LOTOTO studio światło", P(0f, H - .4f, .2f), 1.1f, 6.5f, new Color(.98f, .98f, 1f));
        W.Kolizja(P(m0.x, .6f, m0.z), new Vector3(.8f, 1.2f, .8f), q);
        // the TV camera (renders only during a show near the player, and once when the board changes) and its two shots
        var kam = new GameObject("kamera studia LOTOTO (obraz na ekrany BET)"); kam.transform.SetParent(g, false);
        var cam = kam.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 40f; cam.nearClipPlane = .05f; cam.farClipPlane = 14f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.02f, .03f, .08f); lot.kamera = cam;
        Transform Ujecie(string n, Vector3 at, Vector3 look) { var t = new GameObject(n).transform; t.SetParent(g, false); t.localPosition = P(at.x, at.y, at.z); t.localRotation = Quaternion.LookRotation(q * (look - at), Vector3.up); return t; }
        lot.ujecieBeben = Ujecie("ujęcie: maszyna", new Vector3(kamPos.x, 1.52f, kamPos.z - .25f), new Vector3(m0.x + .25f, 1.18f, m0.z));
        lot.ujecieTablica = Ujecie("ujęcie: tablica", new Vector3(bc.x, bc.y, bc.z + 2.05f), new Vector3(bc.x, bc.y, bc.z));
        kam.transform.SetPositionAndRotation(lot.ujecieTablica.position, lot.ujecieTablica.rotation);
        lot.beben = drum; lot.kulaR = .03f; lot.bebenR = bR; lot.ekran = lotEkran;
        // the replay point at the glass (outside, in the hall)
        var sz = new GameObject("szyba studia (AKCJA: powtórka)").transform; sz.SetParent(g, false); sz.localPosition = P(0f, 0f, HZ + .55f); lot.szyba = sz;
        // ---- the kolektura: the counter (its customer side at lada, toward -z of its frame), the terminal, the printer
        var lq = Quaternion.Euler(0f, ladaYaw, 0f); Vector3 K(float x, float y, float z) => lada + lq * new Vector3(x, y, z);
        const float LL = 2.6f;
        b.RoundedBox(ciemnyOrzech, K(0f, .53f, .62f), new Vector3(LL, 1.06f, .70f), .02f, lq, 2);
        b.RoundedBox(marmur, K(0f, 1.08f, .60f), new Vector3(LL + .1f, .04f, .82f), .01f, lq, 2);
        b.Box(mosiadz, K(0f, .06f, .26f), new Vector3(LL, .06f, .02f), lq);
        Obraz(b, lotLogo, K(0f, .62f, .27f - Przed), 1.5f, 1.5f * LototoLogo[3] / LototoLogo[2], lq * Quaternion.Euler(0f, 180f, 0f));
        W.Kolizja(K(0f, .53f, .62f), new Vector3(LL, 1.06f, .72f), lq);
        b.RoundedBox(czern, K(.55f, 1.15f, .70f), new Vector3(.28f, .10f, .24f), .015f, lq, 2);                 // the coupon printer
        b.Box(lotKupon, K(.55f, 1.205f, .60f), new Vector3(.08f, .002f, .14f), lq);
        var ctx = new BraxKatalog.Ctx { m = b, root = root, cols = root, unit = 1f, water = null, inst = root, seat = (pp, ff, ll) => { } };
        BraxKatalog.Monitor(ctx, K(-.35f, 1.10f, .78f), lq * Vector3.forward, 0, 22f);                    // to the clerk
        W.Obsada("kolektura LOTOTO", K(0f, 0f, 1.35f), ladaYaw + 180f);
        var lp = new GameObject("lada kolektury (AKCJA: kupon / sprawdź)").transform; lp.SetParent(g, false); lp.localPosition = K(0f, 0f, -.15f); lot.lada = lp;
        // the coupon desk: a slanted walnut top on a steel foot, the coupons and two pens on chains
        var pq = Quaternion.Euler(0f, pulpitYaw, 0f); Vector3 D(float x, float y, float z) => pulpit + pq * new Vector3(x, y, z);
        b.Cylinder(stal, D(0f, 0f, 0f), .26f, .02f, 20, pq, true);
        b.Box(stal, D(0f, .55f, 0f), new Vector3(.12f, 1.06f, .12f), pq);
        var tq = pq * Quaternion.Euler(15f, 0f, 0f);
        b.RoundedBox(ciemnyOrzech, D(0f, 1.10f, 0f), new Vector3(1.0f, .04f, .55f), .01f, tq, 2);
        for (int i = 0; i < 3; i++) b.Box(lotKupon, D(-.32f + i * .32f, 1.10f, 0f) + tq * new Vector3(0f, .022f, .02f), new Vector3(.12f, .002f, .24f), tq);
        foreach (float sx in new[] { -.42f, .42f }) b.Tube(czern, D(sx, 1.12f, .14f), D(sx + .05f, 1.13f, .02f), .004f, 6);
        W.Kolizja(D(0f, .58f, 0f), new Vector3(1.0f, 1.16f, .55f), pq);
        // his poster on a black stand, both sides
        if (lotPlakat)
        {
            var sq = Quaternion.Euler(0f, plakatYaw, 0f);
            b.Box(czern, plakat + sq * new Vector3(0f, .015f, 0f), new Vector3(.70f, .03f, .40f), sq);
            b.Box(czern, plakat + sq * new Vector3(0f, 1.20f, 0f), new Vector3(1.10f, 1.65f, .04f), sq);
            foreach (float f in new[] { 0f, 180f })
                Obraz(b, lotPlakat, plakat + sq * (Quaternion.Euler(0f, f, 0f) * new Vector3(0f, 1.20f, .02f + Przed)), 1.0f, 1.5f, sq * Quaternion.Euler(0f, f, 0f));
            W.Kolizja(plakat + sq * new Vector3(0f, .85f, 0f), new Vector3(1.1f, 1.7f, .2f), sq);
        }
        // the screens of the premises show the studio
        int ne = 0; if (ekrany != null) foreach (var r in ekrany) if (r) { r.sharedMaterial = lotEkran; ne++; }
        Debug.Log("LOTOTO_BRAX_CASH studio=" + (2f * HX).ToString("F1") + "x" + (2f * HZ).ToString("F1") + "m game=" + Z.nazwa + " balls=" + Z.pula + " drawn=" + Z.losowane +
            " coupon=" + Z.typy + " drum=" + (2f * bR).ToString("F2") + "m screens=" + ne + " draws=" + string.Join("/", Array.ConvertAll(Z.dni, d => d.ToString())) + " " + Z.godzina +
            ":00 kumulacja_start=" + Z.kumulacjaStart + " poster=" + (LototoWlasciciela && gra == 0 ? "owner" : "missing") + " component=BraxChampionsLototo");
        return lot;
    }
    // a sphere (the drum's glass), seen from both sides
    static void Kula(BraxChampionsMeshBatch b, Material m, Vector3 c, float r, int S, int R)
    {
        Vector3 V(int i, int j) { float th = Mathf.PI * j / R, ph = 2f * Mathf.PI * i / S; return c + new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * r; }
        for (int j = 0; j < R; j++) for (int i = 0; i < S; i++)
        {
            Vector3 a = V(i, j), bb = V(i + 1, j), cc = V(i + 1, j + 1), d = V(i, j + 1);
            if (j > 0) b.Triangle(m, a, bb, cc);
            if (j < R - 1) b.Triangle(m, a, cc, d);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — PLAŻA MIEJSKA OD NOWA (przebudowa 2.11; zgłoszenia właściciela 2d5fd1cf, 58c0d21c, 90010071: „na plaży brak
// dostępu” — siatka i szlaban „LAS · BUNKRY”, piasek za ogrodzeniem, płaska tafla piasku, ostra krawędź wody).
//
// USUNIĘTE (zanim cokolwiek położone): cała geometria i kolizje źródłowego miasta w prostokącie x 64…182, z 112.05…178
// (ogrodzenie z bramą i szlabanem, tablica „LAS · BUNKRY / DROGA ZAMKNIĘTA”, asfalt drogi do lasu, łąka-płyta, sosny
// i skarpy) oraz stara plaża Fable/Claude (CapeBeach — płaski piasek na dwóch siatkach, parasole w złym miejscu, budka
// ratownika przodem do mariny) — tamten kod skasowany z BraxHarborBeachBuild.
//
// NOWE (wymiary rzeczywiste × k):
//  • bulwar przedłużony wzdłuż plaży (x 64…84, z 112…166, granit), donice z zielenią i drzewami, latarnie, ławki przodem
//    do morza, kosze, tablica kąpieliska;
//  • mur oporowy z kamiennym zwieńczeniem i balustradą 1,10 m × k, w niej dwa zejścia schodami (4 stopnie 0,15 × 0,30 m × k,
//    poręcze obustronne) i rampa 1:12 z podestem (dostęp bez schodów);
//  • piasek z rzeźbą: łagodny profil plaży z wałem burzowym, stromszym pasem przy wodzie (fala wbiega na piasek), płycizna,
//    rewa, wydmy z trawą od północy, ściółka lasu; mokry piasek płynnie przechodzi w suchy (jedna siatka, warstwy kolorem
//    wierzchołka — shader Braxhorn/NabrzezeTeren); kamienny falochron oddziela kąpielisko od mariny;
//  • piana na linii wody (siatka + przesuw w shaderze, faluje razem z wodą), boje kąpieliska z liną i niewidzialną granicą,
//    budka ratownika przodem do kąpieliska z masztem flagi, koło ratunkowe, deska ratownicza, prysznice, kosze,
//    parasole z leżakami (AKCJA = połóż się), ręczniki.
// Punkty pod misje: BraxNabrzezePlaza.Miejsca (zleceniodawcy i cele).
public static class BraxNabrzezePlaza
{
    public const float MurX = 84f, BulwarX0 = 64f, Z0 = 112f, Z1 = 166f;
    public static float Gora => BraxNabrzeze.Bulwar - 4f * .15f * BraxNabrzeze.K;   // piasek u stóp muru (4 stopnie niżej)
    const float GX0 = 64f, GZ0 = 100f; const int NX = 118, NZ = 78;              // siatka terenu 1 m: x 64…182, z 100…178
    public static readonly float[] Schody = { 120f, 158f };                         // osie zejść schodami
    public const float RampaGora = 143.5f;                                           // podest rampy z 141.5…143.5, rampa w dół na południe
    static float W => BraxNabrzeze.Woda;

    public static readonly Dictionary<string, Vector3> Miejsca = new Dictionary<string, Vector3>();

    // ---------------- kształt brzegu i rzeźba piasku ----------------
    /// <summary>Linia wody (x) na wysokości z: łuk plaży, przy lesie brzeg skręca do x 150.</summary>
    public static float LiniaWody(float z)
    {
        float x = 134.5f + 4.5f * Mathf.Sin((z - 112f) * .085f) + 2f * Mathf.Sin(z * .21f + 1.3f);
        if (z > 152f) x = Mathf.Lerp(x, 150f, Mathf.SmoothStep(0, 1, (z - 152f) / 18f));
        return x;
    }

    static float Profil(float x, float z)
    {
        float xw = LiniaWody(z), d = xw - x;   // d > 0: ląd
        if (d < 0f)
        {
            float y = W + .09f * d;                                         // płycizna 1:11
            if (d < -14f) y = W - 1.26f - (-(d + 14f)) * .05f;              // dalej łagodniej
            y += .32f * Mathf.Exp(-Sq((d + 24f) / 5f));                     // rewa (podwodna łacha)
            return Mathf.Max(y, -6.5f);
        }
        if (d < 4.5f) return W + .1f * d + .02f * BraxNabrzeze.Szum(x, z, .5f, 1);   // pas przyboju 1:10
        float u = Mathf.Clamp01((d - 4.5f) / Mathf.Max(1f, xw - MurX - 4.5f));        // 0 przy wodzie, 1 pod murem
        float top = Gora;
        float yy = W + .45f + (top - W - .45f) * (1f - Mathf.Pow(1f - u, 1.6f));
        yy += .11f * Mathf.Exp(-Sq((u - .2f) / .07f));                                 // wał burzowy
        yy += .055f * BraxNabrzeze.Szum(x, z, .16f, 3) * Mathf.SmoothStep(0, 1, u * 4f);  // nierówności, ślady
        yy += .03f * Mathf.Sin(x * 1.9f + Mathf.Sin(z * .7f) * 2f) * Mathf.SmoothStep(.1f, .5f, u);   // zmarszczki wiatrowe
        if (x < MurX + 3f) yy = Mathf.Lerp(top, yy, (x - MurX) / 3f);                  // przy murze równo (schody, rampa)
        return yy;
    }
    static float Sq(float a) => a * a;

    /// <summary>Wysokość terenu (y) — używana też przez wyposażenie.</summary>
    public static float Wysokosc(float x, float z) { return Teren(x, z, out _, out _); }

    static float Teren(float x, float z, out float skala, out float trawa)
    {
        skala = 0; trawa = 0;
        if (x < MurX)   // za bulwarem: tylko ściółka lasu (północ)
        {
            trawa = 1;
            return .02f + .08f * BraxNabrzeze.Szum(x, z, .12f, 2);
        }
        float plaza = Profil(x, z);
        float marina = -5f;
        float y = z >= 112f ? plaza : Mathf.Lerp(marina, plaza, Mathf.SmoothStep(0, 1, (z - 102f) / 10f));
        // falochron z narzutu kamiennego: oś z ≈ 108.8, od muru do x ≈ 146 (zaokrąglony koniec)
        float xEnd = 146f, zc = 108.8f + .6f * Mathf.Sin(x * .13f);
        if (x < xEnd + 6f)
        {
            float dx = Mathf.Max(0, x - xEnd), dz = Mathf.Abs(z - zc), dd = Mathf.Sqrt(dx * dx + dz * dz);
            float crest = W + .6f + .28f * BraxNabrzeze.Szum(x, z, .35f, 2) + .25f * Mathf.Clamp01(1f - (x - MurX) / 20f);
            float yg = crest - .6f * Mathf.Max(0, dd - 1.6f);
            if (yg > y) { skala = Mathf.Clamp01((yg - y) / .35f); y = yg; }
        }
        // wydmy od północy (z > 156) i ściółka lasu (z > 166) — tylko na lądzie
        float lad = Mathf.Clamp01((LiniaWody(z) - x - 5f) / 6f);
        float wydma = Mathf.SmoothStep(0, 1, (z - 155f) / 10f) * lad;
        if (wydma > 0)
        {
            float hD = Gora + .25f + .55f * (BraxNabrzeze.Szum(x, z, .075f, 3) * .5f + .5f) + .5f * Mathf.SmoothStep(0, 1, (z - 162f) / 10f);
            y = Mathf.Lerp(y, Mathf.Max(y, hD), wydma);
            trawa = Mathf.Clamp01(wydma * 1.4f - .25f) * Mathf.Clamp01((y - Gora - .1f) / .4f);
        }
        return y;
    }

    // ---------------- budowa ----------------
    public static void Buduj(Transform portRoot)
    {
        Miejsca.Clear();
        var root = new GameObject("CLAUDE plaża miejska (od nowa)").transform;   // „CLAUDE plaża*” = obszar doczytywany Nabrzeże
        root.SetParent(portRoot, false); root.position = Vector3.zero;

        // 1) ZDJĘCIE STAREGO: źródło miasta w całym prostokącie plaży i lasu za nią (ogrodzenie, szlaban, tablica, droga, łąka, sosny)
        BraxNabrzeze.Wytnij(MinMax(64f, -2f, 112.05f, 182f, 60f, 178f), "plaza_od_nowa");

        var m = BraxNabrzezeMebel.Materialy();
        var kol = new GameObject("Plaża: kolizje").transform; kol.SetParent(root, false);

        Terenu(root);
        var z = new BraxNabrzeze.Zbior();
        BulwarPlazowy(z, root, kol, m);
        Pianka(root);
        Wyposazenie(z, root, kol, m);
        Kapielisko(z, root, kol, m);
        int n = z.Emit(root, "Plaża");
        BraxNabrzeze.Log("plaża: obiektów scalonych " + n + ", linia wody x " + LiniaWody(115).ToString("F0") + "…" + LiniaWody(160).ToString("F0") +
                         ", piasek pod murem y " + Gora.ToString("F2") + ", woda y " + W);
    }

    static Bounds MinMax(float x0, float y0, float z0, float x1, float y1, float z1)
    { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    // ---------------- teren: jedna siatka, kolizja siatki, warstwy kolorem wierzchołka ----------------
    static void Terenu(Transform root)
    {
        int w = NX + 1, h = NZ + 1;
        var H = new float[w, h]; var S = new float[w, h]; var T = new float[w, h];
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) { H[i, j] = Teren(GX0 + i, GZ0 + j, out S[i, j], out T[i, j]); }
        var v = new List<Vector3>(); var col = new List<Color>(); var uv2 = new List<Vector2>(); var tri = new List<int>();
        var idx = new int[w, h];
        bool Uzyj(int i, int j) { float x = GX0 + i, zz = GZ0 + j; if (x < MurX - .01f) return zz >= Z1 - .01f; return true; }
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++)
        {
            idx[i, j] = -1; if (!Uzyj(i, j)) continue;
            float y = H[i, j];
            idx[i, j] = v.Count; v.Add(new Vector3(GX0 + i, y, GZ0 + j));
            float mokry = y < W + .08f ? 1f : Mathf.Clamp01(1f - (y - (W + .08f)) / .42f);
            float skala = S[i, j], trawa = T[i, j] * (1 - skala);
            mokry *= (1 - skala) * (1 - trawa);
            float suchy = Mathf.Max(0, 1f - mokry - skala - trawa);
            col.Add(new Color(suchy, mokry, trawa, skala));
            float sx = Mathf.Abs(H[Mathf.Min(i + 1, w - 1), j] - H[Mathf.Max(i - 1, 0), j]) * .5f, sz = Mathf.Abs(H[i, Mathf.Min(j + 1, h - 1)] - H[i, Mathf.Max(j - 1, 0)]) * .5f;
            uv2.Add(new Vector2(Mathf.Clamp01((Mathf.Max(sx, sz) - .35f) / .6f), 0));
        }
        int quads = 0;
        for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
        {
            int a = idx[i, j], b = idx[i + 1, j], c = idx[i, j + 1], d = idx[i + 1, j + 1];
            if (a < 0 || b < 0 || c < 0 || d < 0) continue;
            if ((H[i, j] + H[i + 1, j] + H[i, j + 1] + H[i + 1, j + 1]) * .25f < -6.2f) continue;   // głęboko — bez siatki (dno: kolizja w BraxRegionSea)
            // przekątna wzdłuż mniejszej różnicy wysokości (gładszy brzeg)
            if (Mathf.Abs(H[i, j] - H[i + 1, j + 1]) < Mathf.Abs(H[i + 1, j] - H[i, j + 1])) { tri.Add(a); tri.Add(c); tri.Add(d); tri.Add(a); tri.Add(d); tri.Add(b); }
            else { tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
            quads++;
        }
        var mesh = new Mesh { name = "Plaża: teren (piasek, mokry piasek, falochron, wydmy)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetColors(col); mesh.SetUVs(0, v.ConvertAll(p => new Vector2(p.x, p.z))); mesh.SetUVs(1, uv2); mesh.SetTriangles(tri, 0);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        var mat = BraxNabrzeze.Teren("Nabrzeże: teren plaży", new Color(.78f, .84f, .62f));
        BraxNabrzeze.Obiekt(root, "Plaża: teren piasku", mesh, mat, true);
        BraxNabrzeze.Log("teren plaży: wierzchołków " + v.Count + ", pól " + quads);
    }

    // ---------------- bulwar plażowy, mur oporowy, balustrada, zejścia ----------------
    static void BulwarPlazowy(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m)
    {
        float K = BraxNabrzeze.K, y = BraxNabrzeze.Bulwar;
        var bulwar = BraxNabrzeze.Mat("Bulwar: płyty granitowe", 1.6f, new Color(.95f, .94f, .92f), "granite_tile_02", "granite_tile", "concrete_floor_02");
        var ziemia = BraxNabrzeze.Mat("Bulwar: ziemia donic (kora)", 1.4f, new Color(.55f, .45f, .38f), "brown_mud_leaves_01");
        // nawierzchnia (płyta od −0.3 do 0.15) i kolizja
        z[bulwar].BoxMM(new Vector3(BulwarX0, y - .45f, Z0), new Vector3(MurX, y, Z1));
        BraxNabrzeze.KolMM(kol, "Bulwar plażowy", new Vector3(BulwarX0, y - .45f, Z0), new Vector3(MurX, y, Z1));
        // krawężnik od strony łąki i na końcu bulwaru
        z[m.granit].BoxMM(new Vector3(BulwarX0 - .02f, -.2f, Z0), new Vector3(BulwarX0 + .2f, y + .03f, Z1 + .2f));
        z[m.granit].BoxMM(new Vector3(BulwarX0, -.2f, Z1 - .02f), new Vector3(MurX, y + .03f, Z1 + .2f));
        // mur oporowy od strony plaży: lico z betonu (od piasku −1.4 do 0.15) i zwieńczenie z granitu
        z[m.beton].BoxMM(new Vector3(MurX - .3f, Gora - .9f, Z0), new Vector3(MurX, y, Z1));
        z[m.granit].BoxMM(new Vector3(MurX - .45f, y, Z0), new Vector3(MurX + .1f, y + .12f, Z1));
        // donice wzdłuż zachodniej krawędzi: murek z granitu (0,45 m × k), ziemia, krzewy i drzewa
        float hd = .45f * K;
        var donice = new List<Vector2> { new Vector2(114f, 128f), new Vector2(131f, 147f), new Vector2(150f, 164f) };
        foreach (var d in donice)
        {
            z[m.granit].BoxMM(new Vector3(BulwarX0 + .2f, y, d.x), new Vector3(BulwarX0 + 3.6f, y + hd, d.x + .25f));
            z[m.granit].BoxMM(new Vector3(BulwarX0 + .2f, y, d.y - .25f), new Vector3(BulwarX0 + 3.6f, y + hd, d.y));
            z[m.granit].BoxMM(new Vector3(BulwarX0 + 3.35f, y, d.x), new Vector3(BulwarX0 + 3.6f, y + hd, d.y));
            z[ziemia].BoxMM(new Vector3(BulwarX0 + .2f, y, d.x + .25f), new Vector3(BulwarX0 + 3.35f, y + hd - .08f, d.y - .25f));
            BraxNabrzeze.KolMM(kol, "Donica", new Vector3(BulwarX0 + .2f, y, d.x), new Vector3(BulwarX0 + 3.6f, y + hd, d.y));
        }
        Zielen(root, donice, y + hd - .08f);
        // balustrada na murze z przerwami na schody i podest rampy
        var przerwy = new List<Vector2>();
        foreach (float s in Schody) przerwy.Add(new Vector2(s - 1.62f * K / 1.294f - .3f, s + 1.62f * K / 1.294f + .3f));
        przerwy.Add(new Vector2(RampaGora - 2f, RampaGora + .1f));
        przerwy.Sort((a, b) => a.x.CompareTo(b.x));
        float from = Z0 + .2f;
        foreach (var p in przerwy) { Balustrada(z, kol, m, from, p.x); from = p.y; }
        Balustrada(z, kol, m, from, Z1 - .2f);
        // schody: 4 stopnie w stronę morza (+x), szerokość 2,5 m × k
        foreach (float s in Schody)
            BraxNabrzezeMebel.Schody(z, kol, m, new Vector3(MurX + .1f, y, s), Vector3.right, 2.5f * K, Gora, true, Gora - .8f);
        // rampa: podest przy murze (z 141.5…143.5) na poziomie bulwaru, bieg 1:12 na południe do piasku
        float rw = 1.5f * K;
        z[m.granit].BoxMM(new Vector3(MurX, Gora - .8f, RampaGora - 2f), new Vector3(MurX + rw + .1f, y, RampaGora));
        BraxNabrzeze.KolMM(kol, "Rampa podest", new Vector3(MurX, Gora - .8f, RampaGora - 2f), new Vector3(MurX + rw + .1f, y, RampaGora));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MurX + .05f, y, RampaGora), new Vector3(MurX + rw + .15f, y, RampaGora));
        BraxNabrzezeMebel.Balustrada(z, null, m, new Vector3(MurX + rw + .15f, y, RampaGora), new Vector3(MurX + rw + .15f, y, RampaGora - 2f));
        BraxNabrzeze.Kol(kol, "Rampa podest bariera", new Vector3(MurX + rw + .2f, y + 1.1f, RampaGora - 1f), new Vector3(.2f, 2.2f, 2f));
        BraxNabrzezeMebel.Rampa(z, kol, m, new Vector3(MurX + .05f + rw * .5f, y, RampaGora - 2f), Vector3.back, rw, Gora);
        Miejsca["plaza_schody_poludnie"] = new Vector3(MurX - 1.5f, y, Schody[0]);
        Miejsca["plaza_schody_polnoc"] = new Vector3(MurX - 1.5f, y, Schody[1]);
        Miejsca["plaza_rampa"] = new Vector3(MurX - 1.5f, y, RampaGora - 1f);

        // wyposażenie bulwaru: latarnie przy balustradzie, ławki przodem do morza, kosze, tablica kąpieliska
        foreach (float zz in new[] { 115.5f, 127f, 137.5f, 150f, 163.5f }) BraxNabrzezeMebel.Latarnia(z, root, m, new Vector3(MurX - .9f, y, zz), -90f);
        foreach (float zz in new[] { 117.5f, 125f, 133.5f, 153f, 161f })
            BraxNabrzezeMebel.Lawka(z, root, m, new Vector3(MurX - 1.9f, y, zz), 90f, "Ławka bulwaru plażowego");
        foreach (float zz in new[] { 129.5f, 147.5f, 164.5f }) BraxNabrzezeMebel.Kosz(z, root, m, new Vector3(MurX - 1.2f, y, zz));
        BraxNabrzezeMebel.Tablica(z, root, m, new Vector3(MurX - 2.2f, y, 113.6f), 180f, "KĄPIELISKO MIEJSKIE BRAXHORN",
            "Kąpiel tylko w strefie wyznaczonej bojami · ratownik 10:00–18:00\nNumer alarmowy 112 · zakaz wjazdu łodzi i skuterów", new Color(.08f, .26f, .38f), 2.2f);
        Miejsca["bulwar_tablica_kapieliska"] = new Vector3(MurX - 2.2f, y, 115.5f);
        Miejsca["bulwar_plazowy_lawki"] = new Vector3(MurX - 3f, y, 133.5f);
    }

    static void Balustrada(BraxNabrzeze.Zbior z, Transform kol, BraxNabrzezeMebel.Mat m, float z0, float z1)
    {
        if (z1 - z0 < .4f) return;
        float y = BraxNabrzeze.Bulwar + .12f;
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MurX - .15f, y, z0), new Vector3(MurX - .15f, y, z1));
    }

    static void Zielen(Transform root, List<Vector2> donice, float yZiemi)
    {
        var drzewo = BraxRegion.Model("drzewo_verdant");
        var krzewy = new List<GameObject>();
        foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03", "park_shrub" }) { var g = BraxRegion.Model(id); if (g) krzewy.Add(g); }
        var p = new GameObject("Plaża: zieleń donic").transform; p.SetParent(root, false);
        int nd = 0, nk = 0;
        foreach (var d in donice)
        {
            float mid = (d.x + d.y) * .5f;
            if (drzewo && BraxRegion.Put(p, drzewo, "Bulwar: drzewo w donicy", new Vector3(BulwarX0 + 1.9f, yZiemi, mid), (mid * 47f) % 360f, 1f, false, 11f)) nd++;
            var c = new GameObject("Bulwar: pień kolizja").AddComponent<CapsuleCollider>(); c.transform.SetParent(p, false);
            c.transform.position = new Vector3(BulwarX0 + 1.9f, yZiemi + 1.5f, mid); c.radius = .3f; c.height = 3f;
            if (krzewy.Count == 0) continue;
            for (float zz = d.x + 1.2f; zz < d.y - 1f; zz += 1.7f)
            {
                if (Mathf.Abs(zz - mid) < 1.6f) continue;
                if (BraxRegion.Put(p, krzewy[nk % krzewy.Count], "Bulwar: krzew w donicy", new Vector3(BulwarX0 + 1.9f + ((nk % 2) - .5f) * .8f, yZiemi, zz), (zz * 91f) % 360f, .9f, false, 1.3f * BraxNabrzeze.K)) nk++;
            }
        }
        BraxRegion.Rep.trees += nd; BraxRegion.Rep.shrubs += nk;
    }

    // ---------------- piana na linii wody (siatka; faluje razem z wodą po stronie morza) ----------------
    static void Pianka(Transform root)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var c = new List<Color>(); var t = new List<int>();
        const float krok = .8f; const int across = 7; float ladM = 2.4f, morzeM = 3.2f;
        int rows = 0; float dlug = 0;
        for (float zz = 113.5f; zz <= 168.5f; zz += krok)
        {
            // dokładne przecięcie terenu z lustrem wody (szukanie na profilu)
            float xw = LiniaWody(zz), x0 = xw;
            for (float x = xw - 6f; x < xw + 6f; x += .05f) if (Wysokosc(x, zz) < W) { x0 = x; break; }
            float fade = Mathf.Clamp01((zz - 113.5f) / 3f) * Mathf.Clamp01((168.5f - zz) / 4f);
            for (int k = 0; k < across; k++)
            {
                float f = (float)k / (across - 1);                       // 0 = morze, 1 = ląd
                float x = Mathf.Lerp(x0 + morzeM, x0 - ladM, f);
                float yy = Mathf.Max(Wysokosc(x, zz) + .03f, W + .02f);
                bool naWodzie = Wysokosc(x, zz) < W - .02f;
                v.Add(new Vector3(x, yy, zz)); uv.Add(new Vector2(dlug, f));
                float a = fade * Mathf.Clamp01(Mathf.Min(f, 1f - f) * 6f + .15f);
                c.Add(new Color(1, naWodzie ? 1 : 0, 1, a));
            }
            if (rows > 0)
            {
                int s0 = (rows - 1) * across, s1 = rows * across;
                for (int k = 0; k < across - 1; k++) { t.Add(s0 + k); t.Add(s1 + k); t.Add(s0 + k + 1); t.Add(s0 + k + 1); t.Add(s1 + k); t.Add(s1 + k + 1); }
            }
            rows++; dlug += krok;
        }
        var mesh = new Mesh { name = "Plaża: piana przyboju" };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetColors(c); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); var b = mesh.bounds; b.Expand(new Vector3(0, .5f, 0)); mesh.bounds = b;
        var mat = BraxNabrzeze.Piana("Nabrzeże: piana przyboju", false, 7.5f);
        BraxNabrzeze.Obiekt(root, "Plaża: piana przyboju", mesh, mat, false);
    }

    // ---------------- wyposażenie plaży ----------------
    static void Wyposazenie(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m)
    {
        float K = BraxNabrzeze.K;
        var plotna = new[] { m.plotno0, m.plotno1, m.plotno2, m.plotno3 };
        var reczniki = new[] { m.recznik0, m.recznik1, null };
        var stref = new GameObject("Plaża: parasole i leżaki").transform; stref.SetParent(root, false);
        int nPar = 0, nLez = 0;
        // trzy rzędy równolegle do brzegu (12, 19 i 26 m od wody), co ok. 8 m; wolne: zejścia, budka ratownika, rampa
        float[] rzedy = { 12f, 19.5f, 27f };
        for (int r = 0; r < rzedy.Length; r++)
            for (float zz = 117f + r * 2.5f; zz <= 159f; zz += 8.5f)
            {
                if (Mathf.Abs(zz - 140f) < 5f) continue;                       // budka ratownika i przejście do wody
                float x = LiniaWody(zz) - rzedy[r];
                if (x < MurX + 6f) continue;
                if (zz > 154f && r == 2) continue;                              // wydmy
                float y = Wysokosc(x, zz);
                BraxNabrzezeMebel.Parasol(z, stref, m, plotna[(nPar + r) % plotna.Length], new Vector3(x, y, zz), 3f + (nPar % 3) * 2f);
                nPar++;
                foreach (float dz in new[] { -1.05f * K, 1.05f * K })
                {
                    float lx = x + 1.3f * K, lz = zz + dz;
                    BraxNabrzezeMebel.Lezak(z, stref, m, reczniki[(nLez + r) % reczniki.Length], new Vector3(lx, Wysokosc(lx, lz) + .01f, lz), -90f + dz * 2f);
                    nLez++;
                }
                // (ręczniki leżące na piasku USUNIĘTE — zasada właściciela 15:40: nic nie leży płasko na istniejącej powierzchni)
            }
        // WIEŻA RATOWNIKA (właściciel 3.11 10:52 „pomaluj ładnie… nie da się nawet wejść, zła skala”): model lifeguard_tower.glb
        // (6.2 m bez × k, jedna kolizja-pudło) ZDJĘTY — wieża z kodu × k z wejściem po schodach (BraxNabrzezeRatownik), maszt z flagą
        float bx = LiniaWody(140f) - 9f, bz = 140f; bool budka = false;
        try { BraxNabrzezeRatownik.Buduj(root, kol, bx, bz, Wysokosc, Miejsca); budka = true; }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_NABRZEZE wieża ratownika: " + e); }
        var maszt = new Vector3(bx - 1.5f, Wysokosc(bx - 1.5f, bz + 4f), bz + 4f);
        z[m.bialy].Rura(maszt, maszt + Vector3.up * 6f * K, .04f * K, 10, .025f * K);
        var flaga = maszt + Vector3.up * (6f * K - .45f * K);
        z[m.bialy].Quad(flaga, flaga + new Vector3(1.0f * K, -.05f, .1f), flaga + new Vector3(1.0f * K, -.7f * K, .1f), flaga + new Vector3(0, -.7f * K, 0));
        z[m.bialy].Quad(flaga + new Vector3(0, -.7f * K, 0), flaga + new Vector3(1.0f * K, -.7f * K, .1f), flaga + new Vector3(1.0f * K, -.05f, .1f), flaga);
        BraxNabrzeze.Kol(kol, "Maszt flagi", maszt + Vector3.up * 2f, new Vector3(.2f, 4f, .2f));
        // koło ratunkowe przy wodzie (deska ratownicza — kształtowana, przy wieży: BraxNabrzezeRatownik)
        BraxNabrzezeMebel.KoloRatunkowe(z, root, m, new Vector3(bx + 5f, Wysokosc(bx + 5f, bz + 2f), bz + 2f), -90f);
        // prysznice i kosze u stóp zejść, przy rampie
        foreach (float s in Schody)
        {
            float px = MurX + 3.6f * K, pz = s + 3.4f;
            BraxNabrzezeMebel.Prysznic(z, root, m, new Vector3(px, Wysokosc(px, pz), pz), 90f);
            BraxNabrzezeMebel.Kosz(z, root, m, new Vector3(MurX + 2.4f, Wysokosc(MurX + 2.4f, s - 2.8f), s - 2.8f));
        }
        BraxNabrzezeMebel.Kosz(z, root, m, new Vector3(MurX + 3.6f, Wysokosc(MurX + 3.6f, RampaGora - 13.5f), RampaGora - 13.5f));
        Miejsca["plaza_prysznice"] = new Vector3(MurX + 5f, Gora, Schody[0] + 3.4f);
        Miejsca["plaza_srodek"] = new Vector3(LiniaWody(136f) - 18f, Wysokosc(LiniaWody(136f) - 18f, 136f), 136f);
        Miejsca["plaza_brzeg_wody"] = new Vector3(LiniaWody(130f) - 3f, Wysokosc(LiniaWody(130f) - 3f, 130f), 130f);
        Miejsca["plaza_falochron"] = new Vector3(128f, Wysokosc(128f, 109f), 109f);
        // kępy trawy na wydmach (model CC0, jeśli pobrany)
        var trawy = new List<GameObject>();
        foreach (var id in new[] { "grass_medium_01", "grass_medium_02" }) { var g = BraxRegion.Model(id); if (g) trawy.Add(g); }
        int nt = 0;
        if (trawy.Count > 0)
        {
            var tr = new GameObject("Plaża: trawa wydmowa").transform; tr.SetParent(root, false);
            for (int i = 0; i < 70; i++)
            {
                float x = MurX + 6f + (i * 37 % 61), zz = 157f + (i * 13 % 11) + (i % 3) * .3f;
                float y = Teren(x, zz, out _, out float trawa);
                if (trawa < .3f || x > LiniaWody(zz) - 8f) continue;
                if (BraxRegion.Put(tr, trawy[i % trawy.Count], "Plaża: kępa trawy wydmowej", new Vector3(x, y - .03f, zz), i * 53f % 360f, .9f + (i % 4) * .15f, false, .9f * K)) nt++;
            }
        }
        BraxRegion.Rep.beachProps += nPar + nLez;
        BraxNabrzeze.Log("plaża: parasoli " + nPar + ", leżaków " + nLez + ", kęp trawy " + nt + ", wieża ratownika " + (budka ? "z kodu (× k, schody, kabina)" : "BRAK"));
    }

    // ---------------- kąpielisko: boje na linie i niewidzialna granica (także dla łodzi) ----------------
    static void Kapielisko(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m)
    {
        float xb = 160f, zs = 114.5f, zn = 164f;
        var pts = new List<Vector3> { new Vector3(LiniaWody(zs) + 2f, W, zs), new Vector3(xb, W, zs), new Vector3(xb, W, zn), new Vector3(LiniaWody(zn) + 2f, W, zn) };
        int nb = 0;
        for (int s = 0; s < pts.Count - 1; s++)
        {
            var a = pts[s]; var b = pts[s + 1]; float len = Vector3.Distance(a, b); int n = Mathf.Max(1, Mathf.RoundToInt(len / 5f));
            for (int i = 0; i <= n; i++)
            {
                if (s > 0 && i == 0) continue;
                var p = Vector3.Lerp(a, b, (float)i / n);
                BraxNabrzezeMebel.Boja(z, (nb % 4 == 0) ? m.czerwony : m.zolty, p + Vector3.up * .05f, .22f * BraxNabrzeze.K);
                nb++;
            }
            z[m.bialy].Rura(a + Vector3.up * .04f, b + Vector3.up * .04f, .012f, 4, -1, false);
            var c = (a + b) * .5f; float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
            BraxNabrzeze.Kol(kol, "Granica kąpieliska", new Vector3(c.x, -2.5f, c.z), new Vector3(.4f, 11f, len), yaw);
        }
        Miejsca["plaza_kapielisko_boje"] = new Vector3(xb - 4f, W, (zs + zn) * .5f);
        BraxNabrzeze.Log("kąpielisko: boi " + nb + ", granica x " + xb + ", z " + zs + "…" + zn);
    }
}

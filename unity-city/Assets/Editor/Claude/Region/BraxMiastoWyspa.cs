using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// WYSPA WYPOCZYNKOWA Z ARENĄ BRAXHORN (wykonawca A, 2.11). Właściciel z41: „stożek z łatami → prawdziwa, realna wyspa
// z palmami, ze wszystkim, na którą popłynę, na niej ARENA BRAXHORN”; 15:34: „główna hala walk o mistrzostwa NA WYSPIE,
// wyspa dopieszczona, dostęp normalny… WYPOCZYNKOWA plus ARENA, ludzie przyjeżdżają wypoczywać i oglądać walki”.
// WZÓR: „Vacation Island” (MIT, saturadev — github.com/saturadev/vacation-island-unity): kształt wyspy (promień brzegu
// zaburzony szumem fBm, wzgórze potęgowe, grzbiety ridged multifractal, zatoka-laguna, cypel), warstwy piasek suchy / mokry /
// roślinność / skała z wysokości i nachylenia — PRZENIESIONE z Terrain (moduł wyłączony) na SIATKĘ (jedna siatka, warstwy
// kolorem wierzchołka, shader Braxhorn/NabrzezeTeren, fotoskany CC0 coast_sand_02 / damp_sand / trawa / skała). Palmy
// i krzewy z tamtego projektu (Asset Store) NIE użyte: palmy budowane kodem (pień z pierścieniami, liście pierzaste), krzewy
// i skały Poly Haven CC0.
// ZDJĘTE: stara wyspa (krok BraxRegionIsland — stożek z łatami, „zęby piły”, polana z obrzeżem) — krok wyłączony.
// NOWE (× k): teren ~210 × 200 m nad wodą, plaże, laguna, wzgórze 31 m z punktem widokowym; przystań (pomost na palach,
// głowica w kształcie T, knagi, polery, drabinki, latarnie, tablica), deptak z desek na palach od przystani do placu areny;
// PLAC i ARENA BRAXHORN (ośmiobok 48 m, szklane ściany z lizenami, pas LED, daszek, szyld „ARENA BRAXHORN”, kasy biletowe,
// hol, trybuny w 6 sektorach dookoła klatki, klatka ośmiokątna na podeście z siatką, rampa świateł i telebim nad klatką,
// tunel i szatnie zawodników — czerwony i niebieski narożnik); plaża z leżakami i parasolami w rzędach, ratownik, bar
// plażowy, dwa ringi plenerowe na mniejsze walki (nie nasze), palmy, krzewy, skały. Punkty akcji pod misje.
// Osobny obszar doczytywany (korzeń „CLAUDE wyspa…” → BraxClaudeObszary); w scenie głównej sylwetka z daleka.
internal static class BraxMiastoWyspa
{
    public const float CX = 400f, CZ = 60f, W = -1.7f;          // środek wyspy, lustro wody
    const float Shore = 92f, Peak = 30f, ShoreExp = 1.7f, MaxDepth = 12f, Shelf = 36f;
    static readonly Vector2 Plac = new Vector2(-40f, 2f);           // środek placu areny (lokalnie względem CX, CZ)
    const float PlacR = 36f, PlacH = 3.6f;                          // promień płaskiego placu, wysokość nad wodą
    const float ArenaR = 24f;                                       // promień opisany ośmioboku areny
    static float K => BraxMiastoA.K;
    static Transform root, kol;
    public static readonly Dictionary<string, Vector3> Miejsca = new Dictionary<string, Vector3>();

    // ---------------- szum i kształt (Vacation Island, MIT) ----------------
    static float Fbm(float x, float z, int oct, float gain)
    {
        float s = 0, a = 1, f = 1, n = 0;
        for (int i = 0; i < oct; i++) { s += a * (Mathf.PerlinNoise(x * f + 137.13f * i + 1000f, z * f + 71.7f * i + 1000f) * 2f - 1f); n += a; a *= gain; f *= 2.03f; }
        return s / Mathf.Max(n, 1e-5f);
    }
    static float Ridged(float x, float z, int oct)
    {
        float s = 0, a = 1, f = 1, n = 0;
        for (int i = 0; i < oct; i++) { float q = Mathf.PerlinNoise(x * f + 311f * i + 500f, z * f + 97f * i + 500f) * 2f - 1f; q = 1f - Mathf.Abs(q); q *= q; s += q * a; n += a; a *= .5f; f *= 2.07f; }
        return s / Mathf.Max(n, 1e-5f);
    }
    static float Gauss(float x, float z, float cx, float cz, float sg) { float dx = x - cx, dz = z - cz; return Mathf.Exp(-(dx * dx + dz * dz) / (2f * sg * sg)); }

    /// <summary>Wysokość nad lustrem wody (m) w punkcie lokalnym (x, z) względem środka wyspy.</summary>
    public static float H(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z);
        float warp = Fbm(x * .0075f, z * .0075f, 3, .5f) * 16f + Fbm(x * .023f, z * .023f, 2, .5f) * 5f;
        float rr = r + warp, y;
        if (rr < Shore) { float u = 1f - rr / Shore; y = Peak * Mathf.Pow(u, ShoreExp); }
        else { float v = rr - Shore; y = -MaxDepth * Mathf.Pow(1f - Mathf.Exp(-v / Shelf), 1.2f); }
        y -= Gauss(x, z, 52f, 50f, 30f) * 5.5f;      // laguna wcięta w plażę NE
        y += Gauss(x, z, 70f, -40f, 20f) * 12f;      // skalisty cypel (wschód)
        y += Gauss(x, z, 30f, -30f, 26f) * 9f;       // drugi szczyt
        float inland = Mathf.Clamp01((Shore - rr) / 50f);
        y += (Ridged(x * .0105f, z * .0105f, 4) - .48f) * 20f * Mathf.Pow(inland, 1.25f);
        y += (Ridged(x * .030f + 40f, z * .030f - 25f, 3) - .5f) * 6f * Mathf.Pow(inland, 1.6f);
        float land = Mathf.Clamp01((y - 1f) / 6f);
        y += Fbm(x * .021f, z * .021f, 4, .5f) * 2.4f * land + Fbm(x * .07f, z * .07f, 3, .5f) * .6f * land;
        y += Fbm(x * .018f, z * .018f, 3, .5f) * .6f * Mathf.Clamp01(-y / 4f);
        y += Gauss(x, z, Plac.x, Plac.y, 45f) * 6f;   // ląd pod placem i przystanią (zachód, od strony miasta)
        // płaski plac areny (z łagodną skarpą 12 m)
        float dp = Vector2.Distance(new Vector2(x, z), Plac);
        if (dp < PlacR) y = PlacH;
        else if (dp < PlacR + 12f) y = Mathf.Lerp(PlacH, y, Mathf.SmoothStep(0, 1, (dp - PlacR) / 12f));
        return y;
    }
    public static float Y(float wx, float wz) => W + H(wx - CX, wz - CZ);

    public static void Apply(Transform cityRoot)
    {
        Miejsca.Clear(); osoby.Clear(); deptak.Clear(); zajete.Clear(); zapowiedz = null; BraxUliceKit.Start(); BraxMiastoA.WyczyscPamiec();
        // stara wyspa (gdyby krok był jeszcze gdzieś wołany) — zdjęta
        var stara = cityRoot.Find("CLAUDE wyspa (oktagon Ligi, otwarta)"); if (stara) BraxUliceKit.Usun(stara.gameObject, "stara wyspa (stożek z łatami)");
        root = BraxRegion.Root(cityRoot, "CLAUDE wyspa — ARENA BRAXHORN (wypoczynkowa)");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        Directory.CreateDirectory(BraxMiastoA.Out + "/Wyspa");
        int tris = Teren();
        var z = new BraxNabrzeze.Zbior(); var m = BraxNabrzezeMebel.Materialy();
        float xPomostu = Przystan(z, m);
        BraxUliceKit.UstawGrupe("Wyspa: arena BRAXHORN");
        Arena();
        BraxUliceKit.UstawGrupe("Wyspa: bar, ringi, punkt widokowy", 80f);
        Plaze(z, m); BarPlazowy(); Ringi(); PunktWidokowy();
        BraxUliceKit.UstawGrupe("Wyspa: plac wejściowy areny (pylon, telebimy, brama, flagi, food trucki)", 240f);   // widać z morza
        PlacWejsciowy();
        int nz = z.Emit(root, "Wyspa: przystań i plaża");
        int siatek = BraxUliceKit.Zapisz(root, "wyspa");
        int palm = Palmy(), krz = Krzewy();
        Sylwetka(cityRoot);
        Punkty();
        Spacerowicze(); Ludzie();     // na końcu (kolejność budowy): ludzie po wszystkim
        BraxMiastoA.Log("WYSPA: teren " + tris + " trójkątów, przystań x " + xPomostu.ToString("F0") + ", palm " + palm + ", krzewów/skał " + krz + ", siatek " + siatek + "+" + nz + " (ARENA BRAXHORN z dachem membranowym, trybunami z przejściami i lożami VIP, szatnią Braxa i tunelem; plaże dookoła, 3 ratowników, bar, 2 ringi, punkt widokowy)");
    }

    // ---------------- teren: jedna siatka, plac w osobnej podsiatce (bez nakładania) ----------------
    static int Teren()
    {
        const float step = 2f, ext = 150f; int n = Mathf.RoundToInt(ext * 2 / step) + 1;
        var Hh = new float[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Hh[i, j] = H(-ext + i * step, -ext + j * step);
        var v = new List<Vector3>(); var col = new List<Color>(); var uv2 = new List<Vector2>(); var idx = new int[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            {
                idx[i, j] = -1; float h = Hh[i, j]; if (h < -11f) continue;
                float x = -ext + i * step, zz = -ext + j * step;
                float hx = Hh[Mathf.Min(i + 1, n - 1), j] - Hh[Mathf.Max(i - 1, 0), j], hz = Hh[i, Mathf.Min(j + 1, n - 1)] - Hh[i, Mathf.Max(j - 1, 0)];
                float steep = Mathf.Atan(Mathf.Sqrt(hx * hx + hz * hz) / (2f * step)) * Mathf.Rad2Deg;
                float b1 = Fbm(x * .03f, zz * .03f, 3, .5f), b2 = Fbm(x * .008f, zz * .008f, 2, .5f);
                float sand = Mathf.Max(.02f, Mathf.Clamp01((7f - h) / 5f) * Mathf.Clamp01((30f - steep) / 12f));
                float d = (h + .2f) / 1.9f, damp = Mathf.Exp(-d * d) * 2f;
                float grass = Mathf.Clamp01((h - 2f) / 3f) * Mathf.Clamp01((41f - steep) / 13f) * Mathf.Clamp01(b1 * 1.2f + b2 * .7f + 1.25f) * 6f;
                float rock = Mathf.Max(Mathf.Clamp01((steep - 24f) / 12f), Mathf.Clamp01((h - 26f) / 10f));
                rock = Mathf.Max(rock, Gauss(x, zz, 70f, -40f, 16f) * 1.4f) * Mathf.Clamp01(b1 * 1.2f + 1.05f) * 3f;
                float tot = sand + damp + grass + rock;
                idx[i, j] = v.Count; v.Add(new Vector3(CX + x, W + h, CZ + zz));
                col.Add(new Color(sand / tot, damp / tot, grass / tot, rock / tot));
                uv2.Add(new Vector2(Mathf.Clamp01((steep - 20f) / 25f), 0));
            }
        var tT = new List<int>(); var tP = new List<int>(); int quads = 0;
        for (int i = 0; i < n - 1; i++) for (int j = 0; j < n - 1; j++)
            {
                int a = idx[i, j], b = idx[i + 1, j], c = idx[i, j + 1], dd = idx[i + 1, j + 1];
                if (a < 0 || b < 0 || c < 0 || dd < 0) continue;
                float cx = -ext + (i + .5f) * step, cz = -ext + (j + .5f) * step;
                bool plac = Vector2.Distance(new Vector2(cx, cz), Plac) < PlacR - 3f;
                var t = plac ? tP : tT;
                if (Mathf.Abs(Hh[i, j] - Hh[i + 1, j + 1]) < Mathf.Abs(Hh[i + 1, j] - Hh[i, j + 1])) { t.Add(a); t.Add(c); t.Add(dd); t.Add(a); t.Add(dd); t.Add(b); }
                else { t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(dd); }
                quads++;
            }
        var mesh = new Mesh { name = "Wyspa: teren (Vacation Island → siatka)", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetColors(col); mesh.SetUVs(0, v.ConvertAll(p => new Vector2(p.x, p.z))); mesh.SetUVs(1, uv2);
        mesh.subMeshCount = 2; mesh.SetTriangles(tT, 0); mesh.SetTriangles(tP, 1);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        BraxZapisAssetu.Utworz(mesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/teren.asset"));
        var mT = BraxNabrzeze.Teren("Wyspa: teren (piasek, mokry piasek, roślinność, skała)", new Color(.62f, .78f, .45f), 2.2f);
        var mP = BraxMiastoA.Tex("Wyspa: plac areny (płyty granitowe)", 2.0f, new Color(.88f, .86f, .82f), new Color(.66f, .64f, .6f), .7f, "granite_tile_02", "granite_tile", "concrete_floor_02");
        var g = new GameObject("Wyspa: teren"); g.transform.SetParent(root, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = new[] { mT, mP }; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        g.AddComponent<MeshCollider>().sharedMesh = mesh;
        return (tT.Count + tP.Count) / 3;
    }

    // ---------------- przystań: pomost na palach z głowicą T, deptak z desek do placu ----------------
    static float Przystan(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m)
    {
        float pz = CZ + Plac.y;                          // oś pomostu i deptaka (ta sama co plac areny)
        // brzeg w osi: pierwszy punkt od zachodu, gdzie teren wychodzi nad wodę
        float xs = CX - 150f; for (float x = CX - 150f; x < CX + Plac.x; x += .5f) if (Y(x, pz) > W + .1f) { xs = x; break; }
        float deck = W + 1.25f;                           // pokład pomostu (1.25 m nad wodą)
        float x0 = xs - 34f, x1 = xs + 1f, hw = 1.6f * K; // pomost: 35 m w morze, szer. 3.2 m × k
        var deski = BraxNabrzeze.Mat("Wyspa: deski pomostu (egzotyk)", 1.5f, new Color(.85f, .74f, .6f), "wood_planks_grey", "wooden_planks", "wood_planks");
        var pal = BraxNabrzeze.Mat("Wyspa: pale drewniane", 1.0f, new Color(.55f, .45f, .35f), "wood_planks_grey", "wooden_planks");
        // pokład z desek (deski 14 cm z 1 cm przerwą — bryły, nie płaszczyzna na płaszczyźnie)
        void Poklad(float a0, float a1, float b0, float b1, float y, bool wzdluzX)
        {
            float L = wzdluzX ? b1 - b0 : a1 - a0;
            for (float t = 0; t < L - .01f; t += .15f)
            {
                if (wzdluzX) z[deski].Box(new Vector3((a0 + a1) * .5f, y - .03f, b0 + t + .07f), new Vector3(a1 - a0, .06f, .14f));
                else z[deski].Box(new Vector3(a0 + t + .07f, y - .03f, (b0 + b1) * .5f), new Vector3(.14f, .06f, b1 - b0));
            }
            z[pal].Box(new Vector3((a0 + a1) * .5f, y - .16f, (b0 + b1) * .5f), new Vector3(a1 - a0 - .1f, .2f, b1 - b0 - .1f));   // legary
        }
        Poklad(x0, x1, pz - hw, pz + hw, deck, false);
        // głowica T (16 m wzdłuż z) — tu cumują jacht i motorówka
        float tx0 = x0 - 3.2f * K, tz0 = pz - 8f, tz1 = pz + 8f;
        Poklad(tx0, x0, tz0, tz1, deck, true);
        BraxMiastoA.Kol(kol, "pomost wyspy", new Vector3(x0, deck - .3f, pz - hw), new Vector3(x1, deck, pz + hw));
        BraxMiastoA.Kol(kol, "głowica pomostu", new Vector3(tx0, deck - .3f, tz0), new Vector3(x0, deck, tz1));
        // pale co 3 m, knagi, polery, drabinki, balustrada od strony mielizny, latarnie
        for (float x = x0; x <= x1; x += 3f) foreach (float sz in new[] { -1f, 1f }) z[pal].Rura(new Vector3(x, W - 4f, pz + sz * (hw - .1f)), new Vector3(x, deck + .05f, pz + sz * (hw - .1f)), .16f, 10);
        for (float zz = tz0; zz <= tz1; zz += 3.2f) foreach (float xx in new[] { tx0 + .1f, x0 - .1f }) z[pal].Rura(new Vector3(xx, W - 4f, zz), new Vector3(xx, deck + .05f, zz), .16f, 10);
        for (float zz = tz0 + 1f; zz < tz1; zz += 4f) { z[m.stalCiemna].Box(new Vector3(tx0 + .25f, deck + .07f, zz), new Vector3(.12f, .14f, .4f)); }
        foreach (float zz in new[] { tz0 + .4f, tz1 - .4f }) { z[m.stalCiemna].Rura(new Vector3(tx0 + .35f, deck, zz), new Vector3(tx0 + .35f, deck + .45f * K, zz), .12f, 12); }
        foreach (float zz in new[] { tz0 + 5f, tz1 - 5f })
        {   // drabinka z wody na pomost
            foreach (float d in new[] { -.25f, .25f }) z[m.stal].Rura(new Vector3(tx0 - .05f, W - 1.2f, zz + d), new Vector3(tx0 - .05f, deck + .9f, zz + d), .025f, 8);
            for (float y = W - 1f; y < deck; y += .3f) z[m.stal].Rura(new Vector3(tx0 - .05f, y, zz - .25f), new Vector3(tx0 - .05f, y, zz + .25f), .018f, 6);
        }
        for (float x = x0 + 4f; x < x1; x += 10f) BraxNabrzezeMebel.Latarnia(z, root, m, new Vector3(x, deck, pz + hw - .25f), 0f);
        BraxNabrzezeMebel.KoloRatunkowe(z, root, m, new Vector3(x0 + 1.5f, deck, pz - hw + .1f), 0f);
        // tablica przystani (litery na desce, na dwóch słupkach)
        var tab = new Vector3(x1 + 2.5f, 0, pz - hw - 1.2f); tab.y = Y(tab.x, tab.z);
        BraxUliceKit.S(BraxMiastoA.Mat("Wyspa: tablica (drewno ciemne)", new Color(.25f, .18f, .12f), .7f)).Box(tab + new Vector3(-1.4f, 1.3f, -.04f), tab + new Vector3(1.4f, 2.1f, .04f));
        foreach (float d in new[] { -1.2f, 1.2f }) BraxUliceKit.S(BraxMiastoA.Mat("Wyspa: tablica (drewno ciemne)", new Color(.25f, .18f, .12f), .7f)).Box(tab + new Vector3(d - .05f, -.3f, -.05f), tab + new Vector3(d + .05f, 2.1f, .05f));
        BraxUliceKit.Napis3D("PRZYSTAŃ WYSPY", tab + new Vector3(0, 1.75f, -.04f), Vector3.back, .2f, .02f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
        BraxUliceKit.Napis3D("ARENA BRAXHORN · PLAŻA", tab + new Vector3(0, 1.42f, -.04f), Vector3.back, .14f, .02f, BraxMiastoA.Mat("Wyspa: litery białe", new Color(.95f, .95f, .93f), .5f), BraxMiastoA.Mat("Wyspa: litery boki", new Color(.2f, .2f, .2f), .6f));
        // DEPTAK z desek na palach: od pomostu do placu areny (pokład 25 cm nad terenem, profil bez skoków > 0.3 m)
        float xa = CX + Plac.x - PlacR + 1f; var prof = new List<Vector2>();
        float yPrev = deck;
        for (float x = x1; x <= xa + .01f; x += 1f)
        {
            float mx = W; for (float dz = -hw; dz <= hw; dz += .8f) mx = Mathf.Max(mx, Y(x, pz + dz));
            float y = Mathf.Max(mx + .25f, yPrev - .15f);
            prof.Add(new Vector2(x, y)); yPrev = y;
        }
        for (int i = prof.Count - 2; i >= 0; i--) prof[i] = new Vector2(prof[i].x, Mathf.Max(prof[i].y, prof[i + 1].y - .15f));
        foreach (var q in prof) deptak.Add(new Vector3(q.x, q.y, pz));
        for (int i = 0; i < prof.Count - 1; i++)
        {
            var a = prof[i]; var b = prof[i + 1];
            for (float t = 0; t < 1f; t += .15f)
            {
                float x = Mathf.Lerp(a.x, b.x, t + .07f), y = Mathf.Lerp(a.y, b.y, t + .07f);
                z[deski].Box(new Vector3(x, y - .03f, pz), new Vector3(.14f, .06f, hw * 2f));
            }
            if (i % 3 == 0) foreach (float sz in new[] { -1f, 1f }) z[pal].Rura(new Vector3(a.x, Y(a.x, pz + sz * (hw - .15f)) - .4f, pz + sz * (hw - .15f)), new Vector3(a.x, a.y - .06f, pz + sz * (hw - .15f)), .1f, 8);
            var kc = new GameObject("kolizja: deptak"); kc.transform.SetParent(kol, false);
            var mid = new Vector3((a.x + b.x) * .5f, (a.y + b.y) * .5f - .1f, pz); kc.transform.position = mid;
            kc.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            kc.AddComponent<BoxCollider>().size = new Vector3(Vector2.Distance(a, b) + .02f, .2f, hw * 2f);
        }
        // poręcze deptaka
        for (int i = 0; i < prof.Count - 1; i++) foreach (float sz in new[] { -1f, 1f })
            {
                var a = new Vector3(prof[i].x, prof[i].y + 1.0f * K, pz + sz * hw); var b = new Vector3(prof[i + 1].x, prof[i + 1].y + 1.0f * K, pz + sz * hw);
                z[m.drewno].Rura(a, b, .035f, 6);
                if (i % 2 == 0) z[m.drewno].Rura(new Vector3(prof[i].x, prof[i].y, pz + sz * hw), a, .04f, 6);
            }
        // zejście z deptaka na płyty placu: pochylnia z desek 1:10 (bez stopnia; ostatnia deska 1.5 cm nad płytą — nie w jej płaszczyźnie)
        {
            var e = prof[prof.Count - 1]; float yPl = W + PlacH, dh = e.y - yPl;
            if (dh > .03f)
            {
                float lr = dh * 10f;
                for (float t = 0; t < lr; t += .15f)
                {
                    float x = e.x + t + .07f, y = e.y - dh * (t + .07f) / lr; if (y < yPl + .015f) break;
                    z[deski].Box(new Vector3(x, y - .03f, pz), new Vector3(.14f, .06f, hw * 2f));
                }
                var kr = new GameObject("kolizja: pochylnia deptaka"); kr.transform.SetParent(kol, false);
                kr.transform.position = new Vector3(e.x + lr * .5f, (e.y + yPl) * .5f - .1f, pz); kr.transform.rotation = Quaternion.Euler(0, 0, -Mathf.Atan2(dh, lr) * Mathf.Rad2Deg);
                kr.AddComponent<BoxCollider>().size = new Vector3(Mathf.Sqrt(lr * lr + dh * dh) + .02f, .2f, hw * 2f);
            }
        }
        // przejście z deptaka na plac (deptak kończy się 25 cm nad płytami → stopień ≤ 0.3 m)
        Miejsca["wyspa_przystan"] = new Vector3(x0 + 1f, deck, pz);
        Miejsca["wyspa_deptak"] = new Vector3((x1 + xa) * .5f, prof[prof.Count / 2].y, pz);
        BraxMiastoA.Akcja(root, "wyspa_przystan", "PRZYSTAŃ", "Przystań wyspy: zacumuj łódź przy głowicy pomostu — do areny prowadzi deptak.", new Vector3(x0 + 2f, deck, pz), 2.2f);
        return x0;
    }

    // ---------------- ARENA BRAXHORN ----------------
    static Vector3 AC => new Vector3(CX + Plac.x + 4f, W + PlacH, CZ + Plac.y);
    static void Arena()
    {
        var c = AC; float y0 = c.y, Hs = 13.5f, R = ArenaR; c.y = 0f;   // c: środek w planie (y = 0), poziomy jawnie od y0
        var szklo = BraxMiastoA.Mat("Arena: szkło fasady (refleks)", new Color(.16f, .22f, .26f), .08f, .6f);
        var beton = BraxMiastoA.Tex("Arena: beton architektoniczny", 2.4f, new Color(.86f, .86f, .84f), new Color(.6f, .6f, .58f), .8f, "concrete_wall_008", "concrete_floor_02");
        var alu = BraxMiastoA.Mat("Arena: lizeny aluminium", new Color(.55f, .57f, .6f), .35f, .85f);
        var led = BraxMiastoA.Swieci("Arena: pas LED zielony (marka)", new Color(.45f, 1f, .2f), new Color(.8f, 2.2f, .3f));
        var bialeLit = BraxMiastoA.Swieci("Arena: litery szyldu (podświetlane)", Color.white, new Color(2f, 2f, 2f));
        var boki = BraxMiastoA.Mat("Arena: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        var posadzka = BraxMiastoA.Mat("Arena: posadzka hali (żywica grafit)", new Color(.14f, .14f, .15f), .6f);
        var trybM = BraxMiastoA.Mat("Arena: trybuny (beton malowany)", new Color(.3f, .31f, .33f), .85f);
        var fotel = BraxMiastoA.Mat("Arena: fotele trybun (kolory z wierzchołków)", Color.white, .55f);
        var sufit = BraxMiastoA.Mat("Arena: strop (czarny)", new Color(.04f, .04f, .045f), .9f);
        var mata = BraxMiastoA.Mat("Arena: mata klatki (winyl)", new Color(.88f, .88f, .86f), .7f);
        var czarny = BraxMiastoA.Mat("Arena: konstrukcja klatki (czarny lakier)", new Color(.04f, .04f, .045f), .4f, .4f);
        var siatkaM = BraxMiastoA.Mat("Arena: siatka klatki (powlekana, półprzezroczysta)", new Color(.05f, .05f, .05f, .38f), .5f, .2f);
        var gabka = BraxMiastoA.Mat("Arena: osłony klatki (pianka)", new Color(.06f, .06f, .06f), .8f);
        int nb = 8; float ap = R * Mathf.Cos(Mathf.PI / 8f);   // apotema ośmioboku
        Vector3 Wsp(int i, float r) { float a = (i + .5f) * Mathf.PI * 2f / nb; return c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
        // ŚCIANY: 8 boków; bok zachodni (do przystani) z wejściem: 3 pary drzwi automatycznych + kasy
        int wej = 3;   // bok o normalnej ≈ −x (i = 3 → kąt (3.5)·45° = 157.5°... wybierany niżej po normalnej)
        float best = 1f; for (int i = 0; i < nb; i++) { var mid = (Wsp(i, R) + Wsp(i + 1, R)) * .5f - c; if (mid.normalized.x < best) { best = mid.normalized.x; wej = i; } }
        for (int i = 0; i < nb; i++)
        {
            var a = Wsp(i, R); var b = Wsp(i + 1, R); var dl = Vector3.Distance(a, b); var Rr = (b - a) / dl; var N = Vector3.Cross(Rr, Vector3.up).normalized; if (Vector3.Dot(N, (a + b) * .5f - c) < 0) N = -N;
            var otw = new List<Rect>();
            if (i == wej) foreach (float s in new[] { dl * .5f - 4.2f, dl * .5f, dl * .5f + 4.2f }) otw.Add(Rect.MinMaxRect(s - 1.3f, 0f, s + 1.3f, 2.9f));
            // cokół betonowy 0–1 m, szkło 1–11 m, pas LED, attyka betonowa do 13.5 m
            var lico = BraxUliceKit.S(szklo);
            BraxMiastoA.Sciana(lico, a + Vector3.up * y0, Rr, N, dl, 1.0f, 11.0f, otw.Where(o => o.yMax > 1f).Select(o => Rect.MinMaxRect(o.xMin, 1.0f, o.xMax, o.yMax)).ToList(), .44f, BraxUliceKit.S(beton));
            BraxMiastoA.Sciana(BraxUliceKit.S(beton), a + Vector3.up * y0, Rr, N, dl, 0f, 1.0f, otw, .44f, BraxUliceKit.S(beton));
            BraxMiastoA.Sciana(BraxUliceKit.S(beton), a + Vector3.up * y0, Rr, N, dl, 11.6f, Hs, null);
            BraxUliceKit.S(led).OBox(a + Rr * dl * .5f + Vector3.up * (y0 + 11.3f) + N * .05f, Rr, Vector3.up, N, dl, .6f, .1f, true);
            for (float s = 1.0f; s < dl - .5f; s += 2.4f) BraxUliceKit.S(alu).OBox(a + Rr * s + Vector3.up * (y0 + 6f) + N * .1f, Rr, Vector3.up, N, .14f, 10f, .2f, true);
            // wnętrze: ściana od środka (czarna) — lico 0.4 m za szkłem
            var Ni = -N; var ai = a - N * .44f; var bi = b - N * .44f;
            BraxMiastoA.Sciana(BraxUliceKit.S(sufit), bi + Vector3.up * y0, -Rr, Ni, Vector3.Distance(ai, bi), 0f, Hs - .4f, i == wej ? otw.Select(o => Rect.MinMaxRect(dl - o.xMax, o.yMin, dl - o.xMin, o.yMax)).ToList() : null);
            var kc = new GameObject("kolizja: ściana areny " + i); kc.transform.SetParent(kol, false);
            kc.transform.position = (a + b) * .5f - N * .2f + Vector3.up * (y0 + Hs * .5f); kc.transform.rotation = Quaternion.LookRotation(N, Vector3.up);
            if (i != wej) kc.AddComponent<BoxCollider>().size = new Vector3(dl + .4f, Hs, .5f);
            else
            {
                Object.DestroyImmediate(kc);
                float s0 = 0f;
                foreach (var o in otw.OrderBy(o => o.xMin).Concat(new[] { Rect.MinMaxRect(dl, 0, dl, 0) }))
                {
                    if (o.xMin - s0 > .05f) { var k2 = new GameObject("kolizja: ściana wejścia"); k2.transform.SetParent(kol, false); k2.transform.position = a + Rr * ((s0 + o.xMin) * .5f) - N * .2f + Vector3.up * (y0 + Hs * .5f); k2.transform.rotation = Quaternion.LookRotation(N, Vector3.up); k2.AddComponent<BoxCollider>().size = new Vector3(o.xMin - s0, Hs, .5f); }
                    if (o.width > 0) { var k3 = new GameObject("kolizja: nadproże"); k3.transform.SetParent(kol, false); k3.transform.position = a + Rr * o.center.x - N * .2f + Vector3.up * (y0 + (2.9f + Hs) * .5f); k3.transform.rotation = Quaternion.LookRotation(N, Vector3.up); k3.AddComponent<BoxCollider>().size = new Vector3(o.width, Hs - 2.9f, .5f); }
                    s0 = o.xMax;
                }
                foreach (var o in otw)
                    BraxMiastoA.DrzwiPrzesuwne(root, "ARENA BRAXHORN — wejście", a + Rr * o.center.x + Vector3.up * y0 - N * .2f, Rr, o.width, 2.9f, BraxUliceKit.SzkloPrzezroczyste, alu);
                // daszek nad wejściem i szyld „ARENA BRAXHORN” z liter przestrzennych nad daszkiem
                var dc = a + Rr * dl * .5f + Vector3.up * (y0 + 3.6f) + N * 1.6f;
                BraxUliceKit.S(alu).OBox(dc, Rr, Vector3.up, N, 15f, .25f, 3.2f);
                BraxUliceKit.S(led).OBox(dc + N * 1.6f - Vector3.up * .14f, Rr, Vector3.up, N, 15f, .04f, .06f);
                float cap = 1.25f, w = BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN", cap); if (w > dl - 2f) cap *= (dl - 2f) / w;
                BraxUliceKit.Napis3D("ARENA BRAXHORN", a + Rr * dl * .5f + Vector3.up * (y0 + 4.6f) + N * .06f, N, cap, .25f, bialeLit, boki);
                // kasy biletowe (2 kioski przy wejściu)
                foreach (float s in new[] { 3.0f, dl - 3.0f })
                {
                    var kp = a + Rr * s + N * 2.4f + Vector3.up * y0;
                    BraxUliceKit.S(beton).OBox(kp + Vector3.up * 1.4f, Rr, Vector3.up, N, 2.4f, 2.8f, 1.8f);
                    BraxUliceKit.S(szklo).OBox(kp + Vector3.up * 1.7f + N * .91f, Rr, Vector3.up, N, 2.0f, 1.0f, .02f);
                    BraxUliceKit.S(led).OBox(kp + Vector3.up * 2.86f, Rr, Vector3.up, N, 2.5f, .1f, 1.9f);
                    var kk = new GameObject("kolizja: kasa biletowa"); kk.transform.SetParent(kol, false); kk.transform.position = kp + Vector3.up * 1.4f; kk.transform.rotation = Quaternion.LookRotation(N, Vector3.up); kk.AddComponent<BoxCollider>().size = new Vector3(2.4f, 2.8f, 1.8f);
                    BraxMiastoA.Akcja(root, s < 5f ? "arena_kasa" : "arena_kasa_2", "KUP BILET", "Kasa ARENY BRAXHORN: bilety na galę mistrzowską — sektor A–F.", kp + N * 1.6f, 1.6f);
                    Czlowiek("Kasa ARENY", "kibic w kolejce", "stoi", kp + N * 1.45f + Rr * (s < 5f ? .3f : -.3f), -N, KlStoi);
                }
                Miejsca["arena_wejscie"] = a + Rr * dl * .5f + N * 3f + Vector3.up * y0;
                foreach (float s in new[] { dl * .5f - 2.1f, dl * .5f + 2.1f }) Czlowiek("Wejście ARENY", "ochrona", "stoi", a + Rr * s + N * 2.0f + Vector3.up * y0, N, KlStoi);
            }
        }
        // DACH (A 2.11 16:04 „dach stalowy / membranowy”): wieniec betonowy na koronie attyki, belka obwodowa, 8 żeber stalowych
        // do pierścienia ściskanego, między żebrami membrana PTFE w siodło (ugięcie 0.9 m), w środku szklany świetlik.
        // Od środka bez zmian: czarny strop 0.4 m pod koroną — membrana jest NAD nim (nic nie leży na niczym).
        var membrana = BraxMiastoA.Mat("Arena: membrana dachu (PTFE)", new Color(.93f, .93f, .91f), .5f);
        var stalD = BraxMiastoA.Mat("Arena: stal dachu (grafit)", new Color(.16f, .17f, .18f), .45f, .7f);
        float yE = y0 + Hs, rE = R - .5f, rP = 6.5f, hP = 3.4f, ugiecie = .9f; const int ns = 8, nt = 8;
        var strop = BraxUliceKit.S(sufit); var sm = BraxUliceKit.S(membrana); var ze = BraxUliceKit.S(stalD);
        for (int i = 0; i < nb; i++)
        {
            int ii = i;
            Vector3 Mb(float s, float t)
            {
                var q = Vector3.Lerp(Vector3.Lerp(Wsp(ii, rE), Wsp(ii + 1, rE), s), Vector3.Lerp(Wsp(ii, rP), Wsp(ii + 1, rP), s), t);
                q.y = yE + .25f + t * hP - ugiecie * Mathf.Sin(Mathf.PI * s) * Mathf.Sin(Mathf.PI * t); return q;
            }
            BraxUliceKit.S(beton).Quad(Wsp(i, R) + Vector3.up * yE, Wsp(i + 1, R) + Vector3.up * yE, Wsp(i + 1, R - .6f) + Vector3.up * yE, Wsp(i, R - .6f) + Vector3.up * yE, Vector3.up);
            for (int a = 0; a < ns; a++)
                for (int b = 0; b < nt; b++)
                {
                    Vector3 p00 = Mb(a / (float)ns, b / (float)nt), p10 = Mb((a + 1) / (float)ns, b / (float)nt), p11 = Mb((a + 1) / (float)ns, (b + 1) / (float)nt), p01 = Mb(a / (float)ns, (b + 1) / (float)nt);
                    var nn = Vector3.Cross(p10 - p00, p01 - p00).normalized; if (nn.y < 0f) nn = -nn;
                    sm.Quad(p00, p10, p11, p01, nn);
                }
            ze.Rura(Mb(0f, 0f) + Vector3.up * .12f, Mb(0f, 1f) + Vector3.up * .12f, .16f, 8);                                // żebro
            ze.Rura(Wsp(i, rE) + Vector3.up * (yE + .05f), Wsp(i + 1, rE) + Vector3.up * (yE + .05f), .22f, 8);               // belka obwodowa
            ze.Rura(Wsp(i, rP) + Vector3.up * (yE + .25f + hP), Wsp(i + 1, rP) + Vector3.up * (yE + .25f + hP), .26f, 8);    // pierścień
            BraxUliceKit.S(szklo).Tri(Wsp(i, rP) + Vector3.up * (yE + .4f + hP), Wsp(i + 1, rP) + Vector3.up * (yE + .4f + hP), c + Vector3.up * (yE + hP + 1.6f), Vector3.up);
            var ai = Wsp(i, R - .5f) + Vector3.up * (y0 + Hs - .4f); var bi = Wsp(i + 1, R - .5f) + Vector3.up * (y0 + Hs - .4f); var ci = c + Vector3.up * (y0 + Hs - .4f);
            strop.Tri(ci, ai, bi, Vector3.down);
            var ap0 = Wsp(i, R - .45f); var bp0 = Wsp(i + 1, R - .45f);
            BraxUliceKit.S(posadzka).Tri(c + Vector3.up * (y0 + .15f), bp0 + Vector3.up * (y0 + .15f), ap0 + Vector3.up * (y0 + .15f), Vector3.up);
        }
        // kolizja posadzki: wypukły ośmiobok (pudło kwadratowe wystawałoby na plac w narożnikach)
        var fm = new UlSiatka(); for (int i = 0; i < nb; i++) { var a = Wsp(i, R - .3f); var b = Wsp(i + 1, R - .3f); fm.Quad(a + Vector3.up * (y0 - .1f), b + Vector3.up * (y0 - .1f), b + Vector3.up * (y0 + .15f), a + Vector3.up * (y0 + .15f), Vector3.up); }
        var fmesh = fm.Zbuduj("A: posadzka areny (kolizja)"); BraxZapisAssetu.Utworz(fmesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/posadzka_kolizja.asset"));
        var kd = new GameObject("kolizja: posadzka areny"); kd.transform.SetParent(kol, false); var kdm = kd.AddComponent<MeshCollider>(); kdm.sharedMesh = fmesh; kdm.convex = true;
        // podjazdy 1:10 przed drzwiami (posadzka hali 15 cm nad placem — wejście idąc, bez progu)
        {
            var a = Wsp(wej, R); var b = Wsp(wej + 1, R); var dl = Vector3.Distance(a, b); var Rr = (b - a) / dl; var N = Vector3.Cross(Rr, Vector3.up).normalized; if (Vector3.Dot(N, (a + b) * .5f - c) < 0) N = -N;
            var pj = BraxUliceKit.S(beton);
            foreach (float s in new[] { dl * .5f - 4.2f, dl * .5f, dl * .5f + 4.2f })
            {
                var m0 = a + Rr * (s - 1.4f) + Vector3.up * y0; var m1 = a + Rr * (s + 1.4f) + Vector3.up * y0;
                pj.Quad(m0 + N * 1.5f, m1 + N * 1.5f, m1 + Vector3.up * .15f, m0 + Vector3.up * .15f, (Vector3.up * 1.5f + N * .15f).normalized);
                pj.Tri(m0 + N * 1.5f, m0 + Vector3.up * .15f, m0, -Rr); pj.Tri(m1 + N * 1.5f, m1, m1 + Vector3.up * .15f, Rr);
                var r = new GameObject("kolizja: podjazd do areny"); r.transform.SetParent(kol, false);
                r.transform.position = a + Rr * s + N * .75f + Vector3.up * (y0 + .075f - .05f);
                r.transform.rotation = Quaternion.LookRotation(N, Vector3.up) * Quaternion.Euler(Mathf.Atan2(.15f, 1.5f) * Mathf.Rad2Deg, 0, 0);
                r.AddComponent<BoxCollider>().size = new Vector3(2.8f, .1f, 1.52f);
            }
        }
        var ks = new GameObject("kolizja: strop areny"); ks.transform.SetParent(kol, false); ks.transform.position = c + Vector3.up * (y0 + Hs - .2f);
        ks.AddComponent<BoxCollider>().size = new Vector3(R * 2f, .4f, R * 2f);
        float F = y0 + .15f;
        void KolO(string nazwa, Vector3 srodek, Vector3 przod, Vector3 rozmiar)
        {
            var g = new GameObject(nazwa); g.transform.SetParent(kol, false); g.transform.position = srodek;
            przod.y = 0f; g.transform.rotation = Quaternion.LookRotation(przod.normalized, Vector3.up); g.AddComponent<BoxCollider>().size = rozmiar;
        }

        // TRYBUNY (A 2.11 16:04 „trybuny z rzędami i schodami, loże VIP”): 6 sektorów (bez sektora holu i sektora tunelu
        // zawodników), 9 rzędów 0.9·k × 0.42·k. Rzędy to CIĘCIWY ośmioboku — fotele i kolizja leżą na cięciwie (wcześniej po
        // łuku: w środku sektora fotel wchodził w rząd wyżej, kolizja była przesunięta o 0.6–1.2 m). Sąsiednie sektory sklejone,
        // na stykach PRZEJŚCIA ze stopniami (dwa na rząd, 0.21·k × 0.45·k), na górze i na końcach barierki; w dwóch sektorach
        // przy holu LOŻE VIP (rzędy 7–9 → taras lóż z parapetem ze szkła, przegrodami i fotelami klubowymi).
        int tunel = (wej + 4) % nb; var fs = BraxUliceKit.S(fotel); var tb = BraxUliceKit.S(trybM);
        var stopM = BraxUliceKit.S(BraxMiastoA.Mat("Arena: stopnie przejść (beton jasny)", new Color(.56f, .57f, .58f), .85f));
        var tk = new UlSiatka();   // kolizja trybun = ta sama bryła co widać (siatka), nie pudła po łuku
        Color32[] kolory = { BraxUliceKit.C(.08f, .08f, .09f), BraxUliceKit.C(.25f, .7f, .15f) };
        float r0 = 9.5f, rz = .9f * K, hz = .42f * K; const int rzedy = 9, rzVip = 6;
        int Mod(int i) => ((i % nb) + nb) % nb;
        bool Jest(int i) => Mod(i) != wej && Mod(i) != tunel;
        bool Vip(int i) => Mod(i) == Mod(wej + 1) || Mod(i) == Mod(wej - 1);
        int Rzedow(int i) => !Jest(i) ? 0 : Vip(i) ? rzVip + 1 : rzedy;
        int Szczyt(int i, int j) => Vip(i) || Vip(j) ? rzVip : rzedy;          // do którego rzędu sięga przejście na styku i|j
        float RA(int q) => r0 + q * rz;
        float RB(int i, int q) => Vip(i) && q == rzVip ? r0 + rzedy * rz : r0 + (q + 1) * rz;
        Vector3 P(float r, float ang, float y) => new Vector3(c.x + Mathf.Cos(ang) * r, y, c.z + Mathf.Sin(ang) * r);
        var poreczM = BraxUliceKit.S(alu); var szkloP = BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste);
        var skora = BraxUliceKit.S(BraxMiastoA.Mat("Arena: fotele lóż VIP (skóra)", new Color(.09f, .09f, .1f), .45f));
        var szron = BraxUliceKit.S(BraxMiastoA.Mat("Arena: przegrody lóż (szkło satynowe)", new Color(.8f, .85f, .86f, .55f), .2f));
        var orzech = BraxUliceKit.S(BraxMiastoA.Mat("Arena: stoliki lóż (orzech)", new Color(.32f, .2f, .12f), .45f));
        var widownia = new List<(Vector3 p, Vector3 f)>(); int nVip = 0;
        void Barierka(Vector3 a, Vector3 b, Vector3 przod)
        {
            float L = Vector3.Distance(a, b); int np = Mathf.Max(1, Mathf.CeilToInt(L / 1.6f));
            poreczM.Rura(a + Vector3.up * 1.1f, b + Vector3.up * 1.1f, .03f, 8); poreczM.Rura(a + Vector3.up * .55f, b + Vector3.up * .55f, .015f, 6);
            for (int s = 0; s <= np; s++) { var p = Vector3.Lerp(a, b, s / (float)np); poreczM.Rura(p, p + Vector3.up * 1.1f, .025f, 6); }
            KolO("kolizja: barierka trybun", (a + b) * .5f + Vector3.up * .55f, przod, new Vector3(L, 1.1f, .1f));
        }
        for (int i = 0; i < nb; i++)
        {
            if (!Jest(i)) continue;
            float a0 = (i + .5f) * Mathf.PI * 2f / nb, a1 = (i + 1.5f) * Mathf.PI * 2f / nb, am = (a0 + a1) * .5f, ch = Mathf.Cos((a1 - a0) * .5f);
            var dm = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am)); var rgS = Vector3.Cross(Vector3.up, dm);
            var e0 = Vector3.Cross(Vector3.up, new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0))).normalized;    // na zewnątrz końca a0
            var e1 = -Vector3.Cross(Vector3.up, new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1))).normalized;   // na zewnątrz końca a1
            int nq = Rzedow(i);
            for (int q = 0; q < nq; q++)
            {
                float ra = RA(q), rb = RB(i, q), yt = F + (q + 1) * hz;
                Vector3 A0 = P(ra, a0, yt), A1 = P(ra, a1, yt), B0 = P(rb, a0, yt), B1 = P(rb, a1, yt);
                Vector3 A0d = P(ra, a0, yt - hz), A1d = P(ra, a1, yt - hz);
                tb.Quad(A0, A1, B1, B0, Vector3.up); tk.Quad(A0, A1, B1, B0, Vector3.up);
                tb.Quad(A0d, A1d, A1, A0, -dm); tk.Quad(A0d, A1d, A1, A0, -dm);                                          // czoło (cięciwa)
                if (q == nq - 1) { tb.Quad(P(rb, a0, F), P(rb, a1, F), B1, B0, dm); tk.Quad(P(rb, a0, F), P(rb, a1, F), B1, B0, dm); }
                // końce sektora tylko tam, gdzie obok nie ma tego rzędu (sektor pominięty albo loże VIP niżej)
                bool k0 = Rzedow(i - 1) <= q, k1 = Rzedow(i + 1) <= q;
                if (k0) { tb.Quad(P(ra, a0, F), P(rb, a0, F), B0, A0, e0); tk.Quad(P(ra, a0, F), P(rb, a0, F), B0, A0, e0); Barierka(P(ra + .1f, a0, yt) - e0 * .08f, P(rb - .1f, a0, yt) - e0 * .08f, e0); }
                if (k1) { tb.Quad(P(ra, a1, F), P(rb, a1, F), B1, A1, e1); tk.Quad(P(ra, a1, F), P(rb, a1, F), B1, A1, e1); Barierka(P(ra + .1f, a1, yt) - e1 * .08f, P(rb - .1f, a1, yt) - e1 * .08f, e1); }
                float dl = Vector3.Distance(A0, A1); var rr = (A1 - A0) / dl;
                if (Vip(i) && q == rzVip)
                {
                    // LOŻE VIP: taras (głęb. 3 rzędy) — szklany parapet z poręczą, loże co ~3 m z przegrodami z satynowego szkła,
                    // w każdej 2 fotele klubowe i stolik; z tyłu korytarz lóż 1.1 m; wejście z przejścia na styku sektorów
                    float gl = (rb - ra) * ch, m0 = Jest(i - 1) ? .85f : .25f, m1 = Jest(i + 1) ? .85f : .25f;
                    var pp0 = A0 + rr * m0 + dm * .1f; var pp1 = A1 - rr * m1 + dm * .1f;
                    szkloP.OBox((pp0 + pp1) * .5f + Vector3.up * .5f * K, rr, Vector3.up, dm, Vector3.Distance(pp0, pp1), 1.0f * K, .02f);
                    poreczM.Rura(pp0 + Vector3.up * 1.0f * K, pp1 + Vector3.up * 1.0f * K, .035f, 8);
                    for (int s = 0; s <= 6; s++) { var p = Vector3.Lerp(pp0, pp1, s / 6f); poreczM.Rura(p, p + Vector3.up * 1.0f * K, .02f, 6); }
                    KolO("kolizja: parapet lóż VIP", (pp0 + pp1) * .5f + Vector3.up * .5f * K, dm, new Vector3(Vector3.Distance(pp0, pp1), 1.0f * K, .1f));
                    int nl = Mathf.Max(1, Mathf.FloorToInt((dl - m0 - m1) / 3.0f)); float wl = (dl - m0 - m1) / nl, lp = gl - 1.25f;
                    for (int L = 0; L < nl; L++)
                    {
                        float s0 = m0 + L * wl;
                        if (L > 0)
                        {
                            var pc = A0 + rr * s0 + dm * (.12f + lp * .5f) + Vector3.up * .8f * K;
                            szron.OBox(pc, dm, Vector3.up, rr, lp, 1.6f * K, .03f);
                            KolO("kolizja: przegroda loży", pc, rr, new Vector3(lp, 1.6f * K, .08f));
                        }
                        foreach (float fx in new[] { .24f, .76f })
                        {
                            var sp = A0 + rr * (s0 + wl * fx) + dm * .95f;
                            skora.OBox(sp + Vector3.up * .25f * K, rr, Vector3.up, dm, .78f, .5f * K, .78f);                          // siedzisko z podstawą
                            skora.OBox(sp + Vector3.up * .7f * K + dm * .33f, rr, Vector3.up, dm, .74f, .4f * K, .14f);              // oparcie (na siedzisku)
                            foreach (float sg in new[] { -1f, 1f }) skora.OBox(sp + rr * sg * .33f + Vector3.up * .61f * K, rr, Vector3.up, dm, .12f, .22f * K, .7f);   // podłokietniki (na siedzisku)
                            BraxMiastoA.Siedzisko(root, "FOTEL VIP", sp + Vector3.up * .5f * K - dm * .05f, -dm);
                            KolO("kolizja: fotel VIP", sp + Vector3.up * .35f * K, dm, new Vector3(.78f, .7f * K, .78f));
                            Czlowiek("Loża VIP", "gość VIP", "siedzi", sp + Vector3.up * .5f * K - dm * .05f, -dm, KlSiedzi); nVip++;
                        }
                        var st = A0 + rr * (s0 + wl * .5f) + dm * .95f;
                        orzech.Walec(st, .05f, .52f * K, 10); orzech.Walec(st + Vector3.up * .52f * K, .28f, .04f, 18); orzech.Walec(st, .2f, .02f, 14);
                    }
                    Barierka(P(rb - .1f, a0, yt) + rr * .3f, P(rb - .1f, a1, yt) - rr * .3f, dm);
                    Miejsca["arena_loze_vip_" + i] = A0 + rr * dl * .5f + dm * (gl - .55f);
                    continue;
                }
                // fotele w rzędzie (na cięciwie, 0.55·k), co trzeci rząd zielony; przy przejściu wolne 0.8 m
                float mm0 = Jest(i - 1) && q <= Szczyt(i, i - 1) ? .8f : .15f, mm1 = Jest(i + 1) && q <= Szczyt(i, i + 1) ? .8f : .15f;
                float sp0 = .55f * K; int nf = Mathf.FloorToInt((dl - mm0 - mm1) / sp0); float rest = (dl - mm0 - mm1 - nf * sp0) * .5f;
                fs.kolor = q % 3 == 1 ? kolory[1] : kolory[0];
                for (int f = 0; f < nf; f++)
                {
                    var sp = A0 + rr * (mm0 + rest + (f + .5f) * sp0) + dm * (.36f * K);
                    fs.OBox(sp + Vector3.up * .43f * K, rgS, Vector3.up, dm, .46f * K, .06f, .42f * K);
                    fs.OBox(sp + Vector3.up * .66f * K + dm * .22f * K, rgS, Vector3.up, dm, .44f * K, .46f * K, .05f);
                    fs.OBox(sp + Vector3.up * .21f * K, rgS, Vector3.up, dm, .08f, .42f * K, .08f);
                    if (((f * 7 + q * 3 + i * 5) % 10) < 3) widownia.Add((sp + Vector3.up * (.43f * K + .03f), -dm));
                }
                if (q == nq - 1) Barierka(B0 + rr * .1f - dm * .08f, B1 - rr * .1f - dm * .08f, dm);
            }
        }
        fs.kolor = BraxUliceKit.Bialy;
        // PRZEJŚCIA na stykach sektorów: stopień z posadzki przed rząd 1 i stopnie pośrednie na każdym rzędzie
        int nPrz = 0;
        for (int i = 0; i < nb; i++)
        {
            if (!Jest(i) || !Jest(i + 1)) continue;
            int szczyt = Szczyt(i, i + 1); nPrz++;
            float ab = (i + 1.5f) * Mathf.PI * 2f / nb; var dr = new Vector3(Mathf.Cos(ab), 0, Mathf.Sin(ab)); var tg = Vector3.Cross(Vector3.up, dr);
            void Stopien(float rA, float rB, float ys0, float ys1)
            {
                var cS = c + dr * ((rA + rB) * .5f) + Vector3.up * ((ys0 + ys1) * .5f);
                stopM.OBox(cS, tg, Vector3.up, dr, 1.3f, ys1 - ys0, rB - rA, false, true);
                tk.OBox(cS, tg, Vector3.up, dr, 1.3f, ys1 - ys0, rB - rA, false, true);
            }
            Stopien(r0 - rz * .5f, r0, F, F + hz * .5f);
            for (int q = 0; q < szczyt; q++) Stopien(RA(q) + rz * .5f, RA(q + 1), F + (q + 1) * hz, F + (q + 1.5f) * hz);
            Miejsca["arena_przejscie_" + i] = c + dr * (r0 - .6f) + Vector3.up * F;
        }
        var tkm = tk.Zbuduj("A: trybuny (kolizja)"); BraxZapisAssetu.Utworz(tkm, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/trybuny_kolizja.asset"));
        var tkg = new GameObject("kolizja: trybuny (bryła)"); tkg.transform.SetParent(kol, false); tkg.AddComponent<MeshCollider>().sharedMesh = tkm;
        // widownia (co ~3. fotel) — dla etapu ludzi; limit pod telefon
        for (int w = 0; w < widownia.Count; w += widownia.Count > 180 ? 2 : 1) Czlowiek("Trybuny ARENY", "kibic", "siedzi", widownia[w].p, widownia[w].f, KlKibic);

        // KLATKA: ośmiokąt (szer. między bokami 9.1 m × k) na podeście 1.0 m × k ze schodkami, mata, słupki z osłonami, siatka,
        // drzwiczki OD STRONY TUNELU ZAWODNIKÓW (wybieg z tunelu prosto do klatki); narożnik czerwony i niebieski (osłony słupków)
        float ri = 4.55f * K, rc = ri / Mathf.Cos(Mathf.PI / 8f), hp = 1.0f * K, hf = 1.8f * K;
        var top = c + Vector3.up * (F + hp - c.y);
        int czerw = (tunel + 3) % 8, nieb = (tunel + 6) % 8;
        Vector3 Kw(int i, float r) { float a = (i + .5f) * Mathf.PI * 2f / 8f; return new Vector3(c.x + Mathf.Cos(a) * r, 0, c.z + Mathf.Sin(a) * r); }
        for (int i = 0; i < 8; i++)
        {
            var a = Kw(i, rc + .6f); var b = Kw(i + 1, rc + .6f);
            BraxUliceKit.S(czarny).Quad(a + Vector3.up * F, b + Vector3.up * F, b + Vector3.up * (F + hp), a + Vector3.up * (F + hp), ((a + b) * .5f - new Vector3(c.x, 0, c.z)).normalized);
            BraxUliceKit.S(czarny).Quad(Kw(i, rc) + Vector3.up * (F + hp), Kw(i + 1, rc) + Vector3.up * (F + hp), Kw(i + 1, rc + .6f) + Vector3.up * (F + hp), Kw(i, rc + .6f) + Vector3.up * (F + hp), Vector3.up);   // obrzeże czarne (pierścień)
        }
        // mata (wierzch podestu wewnątrz siatki) — obwódka podestu czarna, mata biała: dwie strefy tej samej płaszczyzny bez nakładania
        var mm = new UlSiatka();
        for (int i = 0; i < 8; i++) mm.Tri(new Vector3(c.x, F + hp, c.z), Kw(i + 1, rc) + Vector3.up * (F + hp), Kw(i, rc) + Vector3.up * (F + hp), Vector3.up);
        BraxMiastoA.Obiekt(root, "Klatka: mata", mm, mata, true);
        var siat = BraxUliceKit.S(siatkaM); var cz = BraxUliceKit.S(czarny); var gb = BraxUliceKit.S(gabka);
        var gbC = BraxUliceKit.S(BraxMiastoA.Mat("Arena: osłona narożnika czerwonego", new Color(.72f, .07f, .07f), .7f));
        var gbN = BraxUliceKit.S(BraxMiastoA.Mat("Arena: osłona narożnika niebieskiego", new Color(.08f, .2f, .7f), .7f));
        for (int i = 0; i < 8; i++)
        {
            var a = Kw(i, rc) + Vector3.up * (F + hp); var b = Kw(i + 1, rc) + Vector3.up * (F + hp);
            (i == czerw ? gbC : i == nieb ? gbN : gb).Rura(a, a + Vector3.up * hf, .12f, 10);
            gb.Rura(a + Vector3.up * hf, b + Vector3.up * hf, .1f, 10);
            cz.Rura(a + Vector3.up * .1f, b + Vector3.up * .1f, .03f, 6);
            bool drzwi = i == tunel;
            var nrm = ((a + b) * .5f - new Vector3(c.x, a.y, c.z)).normalized;
            if (!drzwi) { siat.Quad(a + Vector3.up * .15f, b + Vector3.up * .15f, b + Vector3.up * (hf - .1f), a + Vector3.up * (hf - .1f), nrm); siat.Quad(a + Vector3.up * .15f, b + Vector3.up * .15f, b + Vector3.up * (hf - .1f), a + Vector3.up * (hf - .1f), -nrm); }
            else
            {   // drzwiczki otwarte (skrzydło obrócone na zewnątrz)
                var dd = (b - a); float L = dd.magnitude; dd /= L; float dw = 1.0f * K;
                var m0 = a + dd * (L * .5f - dw * .5f); var m1 = a + dd * (L * .5f + dw * .5f);
                foreach (var (p, q) in new[] { (a, m0), (m1, b) }) { siat.Quad(p + Vector3.up * .15f, q + Vector3.up * .15f, q + Vector3.up * (hf - .1f), p + Vector3.up * (hf - .1f), nrm); siat.Quad(p + Vector3.up * .15f, q + Vector3.up * .15f, q + Vector3.up * (hf - .1f), p + Vector3.up * (hf - .1f), -nrm); }
                var open = m0 + (Quaternion.AngleAxis(-70f, Vector3.up) * dd) * dw;
                siat.Quad(m0 + Vector3.up * .15f, open + Vector3.up * .15f, open + Vector3.up * (hf - .1f), m0 + Vector3.up * (hf - .1f), Vector3.Cross(Vector3.up, open - m0).normalized);
                cz.Rura(m0, m0 + Vector3.up * hf, .03f, 6); cz.Rura(m1, m1 + Vector3.up * hf, .03f, 6);
                // schodki do drzwiczek (4 × 0.25 m × k) od strony tunelu
                for (int s = 0; s < 4; s++)
                {
                    float hs = hp * (s + 1) / 4f, out0 = .6f + (3 - s) * .35f;
                    var cS = (m0 + m1) * .5f + nrm * (out0 + .175f); cS.y = F + hs * .5f;
                    cz.OBox(cS, dd, Vector3.up, nrm, dw + .4f, hs, .35f);
                    var kS = new GameObject("kolizja: schodek klatki"); kS.transform.SetParent(kol, false); kS.transform.position = cS; kS.transform.rotation = Quaternion.LookRotation(nrm, Vector3.up); kS.AddComponent<BoxCollider>().size = new Vector3(dw + .4f, hs, .35f);
                }
                Miejsca["arena_klatka"] = (m0 + m1) * .5f + nrm * .3f;
                BraxMiastoA.Akcja(root, "arena_klatka", "WEJDŹ DO KLATKI", "Sędzia: Do narożnika — walka o pas mistrza ARENY BRAXHORN.", (m0 + m1) * .5f + nrm * 2.4f + Vector3.down * (hp), 2.0f);
                Czlowiek("Klatka", "sędzia", "stoi", (m0 + m1) * .5f - nrm * 1.3f, -nrm, KlStoi);
            }
            var kf = new GameObject("kolizja: siatka klatki"); kf.transform.SetParent(kol, false); kf.transform.position = (a + b) * .5f + Vector3.up * hf * .5f; kf.transform.rotation = Quaternion.LookRotation(nrm, Vector3.up);
            if (!drzwi) kf.AddComponent<BoxCollider>().size = new Vector3(Vector3.Distance(a, b), hf, .1f); else Object.DestroyImmediate(kf);
        }
        // kolizja podestu (wypukła siatka ośmiokąta)
        var pm = new UlSiatka(); for (int i = 0; i < 8; i++) { var a = Kw(i, rc + .6f); var b = Kw(i + 1, rc + .6f); pm.Quad(a + Vector3.up * F, b + Vector3.up * F, b + Vector3.up * (F + hp), a + Vector3.up * (F + hp), Vector3.up); }
        var pc0 = new GameObject("kolizja: podest klatki"); pc0.transform.SetParent(kol, false); var pmesh = pm.Zbuduj("A: podest klatki (kolizja)");
        BraxZapisAssetu.Utworz(pmesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/podest_kolizja.asset"));
        var mc = pc0.AddComponent<MeshCollider>(); mc.sharedMesh = pmesh; mc.convex = true;
        // NAROŻNIKI: stołek i wiaderko trenera przy narożniku czerwonym i niebieskim (na posadzce przy podeście)
        foreach (var (kat, czer) in new[] { (czerw, true), (nieb, false) })
        {
            var dk = (Kw(kat, 1f) - new Vector3(c.x, 0, c.z)).normalized; var bk = Vector3.Cross(Vector3.up, dk);
            var pS = new Vector3(c.x, F, c.z) + dk * (rc + .6f + .75f);
            var mS = BraxUliceKit.S(czer ? BraxMiastoA.Mat("Arena: osłona narożnika czerwonego", new Color(.72f, .07f, .07f), .7f) : BraxMiastoA.Mat("Arena: osłona narożnika niebieskiego", new Color(.08f, .2f, .7f), .7f));
            BraxUliceKit.S(alu).Walec(pS, .04f, .44f * K, 8); mS.Walec(pS + Vector3.up * .44f * K, .21f, .06f, 18); BraxUliceKit.S(alu).Walec(pS, .18f, .02f, 14);
            BraxMiastoA.Siedzisko(root, "STOŁEK TRENERA", pS + Vector3.up * (.44f * K + .06f), -dk);
            var wd = pS + bk * .55f; BraxUliceKit.S(czarny).Rura(wd, wd + Vector3.up * .32f, .15f, 14, false); BraxUliceKit.S(czarny).Walec(wd, .15f, .015f, 14);
            Miejsca[czer ? "arena_naroznik_czerwony" : "arena_naroznik_niebieski"] = pS;
            Czlowiek(czer ? "Narożnik czerwony" : "Narożnik niebieski", "trener narożnika", "stoi", pS + dk * .55f - bk * .5f, -dk, KlStoi);
        }
        // RAMPA ŚWIATEŁ i TELEBIM nad klatką
        float yr = y0 + Hs - 3.2f;
        var rama = BraxUliceKit.S(alu); var swiatlo = BraxUliceKit.S(BraxMiastoA.Swieci("Arena: panele świetlne nad klatką", Color.white, new Color(2.2f, 2.2f, 2.1f)));
        for (int i = 0; i < 8; i++)
        {
            var a = Kw(i, rc + 1.5f) + Vector3.up * yr; var b = Kw(i + 1, rc + 1.5f) + Vector3.up * yr;
            rama.Rura(a, b, .08f, 8); rama.Rura(a, a + Vector3.up * 3.2f, .03f, 6);
            for (int q = 0; q < 3; q++) { var p = Vector3.Lerp(a, b, (q + .5f) / 3f); swiatlo.OBox(p - Vector3.up * .15f, (b - a).normalized, Vector3.up, Vector3.Cross(Vector3.up, (b - a).normalized), .6f, .08f, .6f); }
        }
        // telebim: kostka — boki to same ekrany (obraz JEST ścianą kostki), czarne krawędzie, góra i dół; na ekranach ZAPOWIEDŹ
        // WALKI z kodu (puste pola na nazwiska zawodników — bez wymyślonych imion)
        var tc = c + Vector3.up * (y0 + 9.8f); string zap = Zapowiedz();
        BraxUliceKit.S(czarny).Box(new Vector3(tc.x - 2.6f, tc.y - 1.6f, tc.z - 2.6f), new Vector3(tc.x + 2.6f, tc.y + 1.6f, tc.z + 2.6f), 1 | 2 | 16 | 32);
        foreach (var ox in new[] { -1f, 1f }) foreach (var oz in new[] { -1f, 1f }) BraxUliceKit.S(czarny).Box(new Vector3(tc.x + ox * 2.6f - .06f, tc.y - 1.6f, tc.z + oz * 2.6f - .06f), new Vector3(tc.x + ox * 2.6f + .06f, tc.y + 1.6f, tc.z + oz * 2.6f + .06f));
        foreach (var (n, r) in new[] { (Vector3.forward, Vector3.left), (Vector3.back, Vector3.right), (Vector3.left, Vector3.back), (Vector3.right, Vector3.forward) })
            BraxMiastoA.Obraz(root, "telebim nad klatką " + n, zap, tc + n * 2.6f - r * 2.54f - Vector3.up * 1.6f, r, 5.08f, 3.2f, n, .9f);
        rama.Rura(new Vector3(tc.x, tc.y + 1.6f, tc.z), new Vector3(tc.x, y0 + Hs - .4f, tc.z), .06f, 8);

        // SZATNIE I TUNEL ZAWODNIKÓW (A 2.11 16:07, fabuła: „szatnia Braxa — ławka, szafki, miejsce trenera i rozgrzewki;
        // tunel wyjścia zawodnika do klatki”): zamknięty blok 11.5 × 5.4 m przy ścianie w sektorze naprzeciw holu — SZATNIA
        // BRAXA (narożnik czerwony) i SZATNIA GOŚCI (niebieski), między nimi tunel z portalem; z tunelu wybieg między barierkami
        // prosto do drzwiczek klatki.
        {
            var dir = ((Wsp(tunel, R) + Wsp(tunel + 1, R)) * .5f - c).WithY0().normalized; var rgt = Vector3.Cross(Vector3.up, dir);
            float vb1 = ap - .44f - .04f, vb0 = vb1 - 5.4f, hS = 3.2f, uK = 1.3f, uS = 5.6f, t = .15f;
            Vector3 L(float u, float v, float y) => c + rgt * u + dir * v + Vector3.up * (F + y);
            var panel = BraxUliceKit.S(BraxMiastoA.Swieci("Arena: panele świetlne szatni", new Color(.95f, .95f, .92f), new Color(1.6f, 1.6f, 1.5f)));
            BraxUliceKit.S(beton).OBox(L(0, (vb0 + vb1) * .5f, hS + .075f), rgt, Vector3.up, dir, 2f * (uS + t), .15f, vb1 - vb0);   // strop bloku
            foreach (float sg in new[] { -1f, 1f })
            {
                bool brax = sg < 0; string kto = brax ? "SZATNIA BRAXA" : "SZATNIA GOŚCI";
                var sc = BraxUliceKit.S(BraxMiastoA.Mat(brax ? "Arena: szatnia czerwona (farba)" : "Arena: szatnia niebieska (farba)", brax ? new Color(.42f, .06f, .06f) : new Color(.07f, .13f, .4f), .8f));
                float uIn = sg * (uK + t * .5f), uOut = sg * (uS + t * .5f), uMid = (uIn + uOut) * .5f, lenF = Mathf.Abs(uOut - uIn) + t;
                // ściana od hali (front, na całą szerokość z narożnikami), boczna zewnętrzna i ściana od tunelu z drzwiami
                sc.OBox(L(uMid, vb0 + t * .5f, hS * .5f), rgt, Vector3.up, dir, lenF, hS, t);
                KolO("kolizja: szatnia (front)", L(uMid, vb0 + t * .5f, hS * .5f), dir, new Vector3(lenF, hS, t));
                sc.OBox(L(uOut, (vb0 + t + vb1) * .5f, hS * .5f), dir, Vector3.up, rgt, vb1 - vb0 - t, hS, t);
                KolO("kolizja: szatnia (bok)", L(uOut, (vb0 + t + vb1) * .5f, hS * .5f), rgt, new Vector3(vb1 - vb0 - t, hS, t));
                float d0 = vb0 + .7f, d1 = d0 + 1.3f, hd = 2.4f;
                foreach (var (v0, v1) in new[] { (vb0 + t, d0), (d1, vb1) })
                {
                    sc.OBox(L(uIn, (v0 + v1) * .5f, hS * .5f), dir, Vector3.up, rgt, v1 - v0, hS, t);
                    KolO("kolizja: szatnia (tunel)", L(uIn, (v0 + v1) * .5f, hS * .5f), rgt, new Vector3(v1 - v0, hS, t));
                }
                sc.OBox(L(uIn, (d0 + d1) * .5f, (hd + hS) * .5f), dir, Vector3.up, rgt, d1 - d0, hS - hd, t);
                BraxUliceKit.Napis3D(kto, L(sg * uK, (d0 + d1) * .5f, hd + .3f), -rgt * sg, .16f, .02f, bialeLit, boki);
                // szafki pod ścianą tylną (stal lakierowana, uchwyty), ławka przed nimi
                var szafM = BraxUliceKit.S(BraxMiastoA.Mat("Arena: szafki szatni (stal lakierowana)", brax ? new Color(.17f, .17f, .18f) : new Color(.2f, .21f, .23f), .45f, .4f));
                float uA = sg * (uK + t + .35f), uB = sg * (uS - .35f), hSz = 1.75f * K; int nS = Mathf.FloorToInt(Mathf.Abs(uB - uA) / .5f);
                for (int s = 0; s < nS; s++)
                {
                    float u = Mathf.Lerp(uA, uB, (s + .5f) / nS);
                    szafM.OBox(L(u, vb1 - .3f, hSz * .5f), rgt, Vector3.up, dir, .47f, hSz, .5f);
                    BraxUliceKit.S(alu).OBox(L(u + .17f, vb1 - .555f, hSz * .55f), rgt, Vector3.up, dir, .025f, .16f, .03f);
                    for (int w = 0; w < 4; w++) BraxUliceKit.S(czarny).OBox(L(u, vb1 - .552f, hSz - .18f - w * .05f), rgt, Vector3.up, dir, .26f, .015f, .02f);   // szczeliny wentylacyjne (wcięte w drzwiczki)
                }
                KolO("kolizja: szafki", L((uA + uB) * .5f, vb1 - .3f, hSz * .5f), dir, new Vector3(Mathf.Abs(uB - uA) + .1f, hSz, .5f));
                float ul = (uA + uB) * .5f, vl = vb1 - 1.45f, hl = .45f * K, ll = Mathf.Abs(uB - uA) - .6f;
                var dab = BraxUliceKit.S(BraxMiastoA.Mat("Arena: ławka szatni (dąb)", new Color(.62f, .48f, .32f), .55f));
                dab.OBox(L(ul, vl, hl - .03f), rgt, Vector3.up, dir, ll, .06f, .4f * K);
                foreach (float o in new[] { -1f, 1f }) BraxUliceKit.S(alu).OBox(L(ul + o * (ll * .5f - .2f), vl, (hl - .06f) * .5f), rgt, Vector3.up, dir, .05f, hl - .06f, .32f);
                KolO("kolizja: ławka szatni", L(ul, vl, hl * .5f), dir, new Vector3(ll, hl, .4f * K));
                foreach (float o in new[] { -.7f, .7f }) BraxMiastoA.Siedzisko(root, "ŁAWKA", L(ul + o, vl, hl), -dir);
                // miejsce trenera: krzesło i stolik z rękawicami, bandażami i wodą (przy ścianie bocznej)
                var kr = L(sg * (uS - .55f), vb0 + 2.1f, 0f); var krP = -rgt * sg;
                var krM = BraxUliceKit.S(BraxMiastoA.Mat("Arena: krzesło trenera (czarne)", new Color(.06f, .06f, .065f), .5f));
                krM.OBox(kr + Vector3.up * .46f * K, krP, Vector3.up, Vector3.Cross(krP, Vector3.up), .48f, .05f, .48f);
                krM.OBox(kr + Vector3.up * .78f * K - krP * .23f, Vector3.Cross(Vector3.up, krP), Vector3.up, krP, .48f, .42f * K, .04f);
                foreach (var o in new[] { new Vector2(-.2f, -.2f), new Vector2(.2f, -.2f), new Vector2(-.2f, .2f), new Vector2(.2f, .2f) })
                    BraxUliceKit.S(alu).Rura(kr + krP * o.x + Vector3.Cross(Vector3.up, krP) * o.y, kr + krP * o.x + Vector3.Cross(Vector3.up, krP) * o.y + Vector3.up * (.46f * K - .025f), .015f, 6);
                BraxMiastoA.Siedzisko(root, "KRZESŁO TRENERA", kr + Vector3.up * (.46f * K + .025f), krP);
                var stl = L(sg * (uS - .5f), vb0 + 3.0f, 0f); float hst = .75f * K;
                krM.OBox(stl + Vector3.up * (hst - .02f), rgt, Vector3.up, dir, .6f, .04f, .6f); BraxUliceKit.S(alu).Rura(stl, stl + Vector3.up * (hst - .04f), .03f, 8); BraxUliceKit.S(alu).Walec(stl, .22f, .02f, 14);
                KolO("kolizja: stolik trenera", stl + Vector3.up * hst * .5f, dir, new Vector3(.6f, hst, .6f));
                var rek = BraxUliceKit.S(BraxMiastoA.Mat(brax ? "Arena: rękawice czerwone" : "Arena: rękawice niebieskie", brax ? new Color(.7f, .06f, .05f) : new Color(.08f, .18f, .62f), .55f));
                foreach (float o in new[] { -.13f, .13f }) rek.OBox(stl + Vector3.up * (hst + .06f) + rgt * o, rgt, Vector3.up, dir, .12f, .11f, .24f);
                var bialy = BraxUliceKit.S(BraxMiastoA.Mat("Arena: bandaże (białe)", new Color(.93f, .93f, .9f), .8f));
                bialy.Walec(stl + Vector3.up * hst + dir * .2f + rgt * .15f, .045f, .05f, 12); bialy.Walec(stl + Vector3.up * hst + dir * .2f - rgt * .02f, .045f, .05f, 12);
                BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Walec(stl + Vector3.up * hst - dir * .2f, .04f, .23f, 12);
                // rozgrzewka: mata z pianki (osobny przedmiot, 4 cm) i worek treningowy na łańcuchu z sufitu
                var mataR = BraxUliceKit.S(BraxMiastoA.Mat(brax ? "Arena: mata rozgrzewki (czerwona)" : "Arena: mata rozgrzewki (granat)", brax ? new Color(.22f, .05f, .05f) : new Color(.05f, .08f, .22f), .9f));
                mataR.OBox(L(sg * 3.0f, vb0 + 1.75f, .02f), rgt, Vector3.up, dir, 2.4f, .04f, 2.4f, false, true);
                var wor = L(sg * 4.6f, vb0 + .75f, 0f); float wb = .5f * K, wt = wb + 1.2f * K;
                BraxUliceKit.S(czarny).Rura(wor + Vector3.up * wb, wor + Vector3.up * wt, .19f * K, 14);
                BraxUliceKit.S(alu).Rura(wor + Vector3.up * wt, wor + Vector3.up * hS, .012f, 4);
                var kw = new GameObject("kolizja: worek treningowy"); kw.transform.SetParent(kol, false); kw.transform.position = wor + Vector3.up * (wb + wt) * .5f;
                var kwc = kw.AddComponent<CapsuleCollider>(); kwc.radius = .19f * K; kwc.height = wt - wb;
                // światło: panele pod stropem (1 cm niżej, osobne oprawy) + światło szatni
                foreach (float vv in new[] { vb0 + 1.6f, vb0 + 3.8f }) panel.OBox(L(sg * 3.5f, vv, hS - .035f), rgt, Vector3.up, dir, 1.2f, .03f, .6f);
                BraxRegion.Warm(root, "Arena: światło " + kto.ToLowerInvariant(), L(sg * 3.5f, vb0 + 2.7f, hS - .45f), 1.1f, 7f);
                // akcje i ludzie
                if (brax)
                {
                    BraxMiastoA.Akcja(root, "arena_szatnia", "WEJDŹ DO SZATNI", "Szatnia Braxa — narożnik czerwony: ławka, szafki, trener i mata do rozgrzewki.", L(sg * (uK * .5f), (d0 + d1) * .5f, 0f), 1.3f);
                    BraxMiastoA.Akcja(root, "arena_wyjdz_do_walki", "WYJDŹ DO WALKI", "Trener: Rękawice sprawdzone. Tunelem prosto do klatki — idziemy.", L(sg * (uK + t + .9f), (d0 + d1) * .5f, 0f), 1.2f);
                    Miejsca["arena_szatnia_braxa"] = L(sg * 3.3f, vb0 + 2.8f, 0f);
                    Czlowiek("Szatnia Braxa", "trener", "stoi", L(sg * 4.4f, vb0 + 2.6f, 0f), -rgt * sg, KlStoi);
                }
                else
                {
                    BraxMiastoA.Akcja(root, "arena_szatnia_niebieska", "SZATNIA GOŚCI", "Szatnia narożnika niebieskiego — tu rozgrzewa się rywal.", L(sg * (uK * .5f), (d0 + d1) * .5f, 0f), 1.3f);
                    Miejsca["arena_szatnia_gosci"] = L(sg * 3.3f, vb0 + 2.8f, 0f);
                    Czlowiek("Szatnia gości", "zawodnik (rywal)", "stoi", L(sg * 3.0f, vb0 + 1.75f, .04f), -dir, KlStoi);
                    Czlowiek("Szatnia gości", "trener", "stoi", L(sg * 4.4f, vb0 + 2.6f, 0f), -rgt * sg, KlStoi);
                }
            }
            // tunel: panele pod stropem; PORTAL od strony hali (czarne filary, nadproże z napisem, pasy LED)
            foreach (float vv in new[] { vb0 + 1.5f, vb0 + 3.9f }) panel.OBox(L(0f, vv, hS - .035f), rgt, Vector3.up, dir, .8f, .03f, .5f);
            var portal = BraxUliceKit.S(czarny); var ledP = BraxUliceKit.S(led);
            foreach (float sg in new[] { -1f, 1f })
            {
                portal.OBox(L(sg * (uK + .2f), vb0 - .25f, 1.9f), rgt, Vector3.up, dir, .4f, 3.8f, .4f);
                KolO("kolizja: portal tunelu", L(sg * (uK + .2f), vb0 - .25f, 1.9f), dir, new Vector3(.4f, 3.8f, .4f));
                ledP.OBox(L(sg * (uK - .02f), vb0 - .25f, 1.45f), rgt, Vector3.up, dir, .04f, 2.8f, .1f);
            }
            portal.OBox(L(0f, vb0 - .25f, 3.35f), rgt, Vector3.up, dir, 2f * uK, .9f, .4f);
            float capP = .26f; float wP = BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN", capP); if (wP > 2f * uK + .6f) capP *= (2f * uK + .6f) / wP;
            BraxUliceKit.Napis3D("ARENA BRAXHORN", L(0f, vb0 - .45f, 3.2f), -dir, capP, .05f, bialeLit, boki);
            // wybieg z tunelu do klatki: dwie barierki (czarne panele z pasem LED na górze), otwarte na obu końcach
            float vw0 = ri + .6f + 1.4f + .35f, vw1 = vb0 - .6f, vm = (vw0 + vw1) * .5f, lw = vw1 - vw0;
            foreach (float sg in new[] { -1f, 1f })
            {
                portal.OBox(L(sg * 1.45f, vm, .55f), dir, Vector3.up, rgt, lw, 1.1f, .08f);
                ledP.OBox(L(sg * 1.45f, vm, 1.125f), dir, Vector3.up, rgt, lw, .05f, .1f, false, true);
                for (float v = vw0 + .3f; v < vw1; v += 1.8f) BraxUliceKit.S(alu).OBox(L(sg * 1.45f, v, .02f), dir, Vector3.up, rgt, .1f, .04f, .5f);
                KolO("kolizja: barierka wybiegu", L(sg * 1.45f, vm, .55f), rgt, new Vector3(lw, 1.1f, .1f));
                Czlowiek("Portal tunelu", "ochrona", "stoi", L(sg * 2.2f, vb0 - .9f, 0f), -dir, KlStoi);
            }
            BraxRegion.Warm(root, "Arena: światło tunelu", L(0f, vb0 + 2.4f, hS - .45f), .9f, 6f);
            Miejsca["arena_tunel"] = L(0f, vb0 - .8f, 0f);
            Miejsca["arena_szatnie"] = L(0f, vb0 + 2f, 0f);
        }

        // STÓŁ SĘDZIÓW I KOMENTATORÓW przy klatce od strony holu: 7 m, laptopy, mikrofony, 5 krzeseł przodem do klatki
        {
            var dw = ((Wsp(wej, R) + Wsp(wej + 1, R)) * .5f - c).WithY0().normalized; var rw = Vector3.Cross(Vector3.up, dw);
            float vt = 9.4f, lt = 6.4f, ht = .76f * K;     // 6.4 m: ≥ 0.6 m od czoła loży VIP na styku sektorów
            Vector3 Q(float u, float v, float y) => c + rw * u + dw * v + Vector3.up * (F + y);
            var mat = BraxUliceKit.S(BraxMiastoA.Mat("Arena: stół sędziów (czarny mat)", new Color(.05f, .05f, .055f), .6f));
            mat.OBox(Q(0f, vt, ht - .025f), rw, Vector3.up, dw, lt, .05f, .8f);
            mat.OBox(Q(0f, vt - .36f, (ht - .05f) * .5f), rw, Vector3.up, dw, lt, ht - .05f, .04f);
            foreach (float o in new[] { -1f, 1f }) mat.OBox(Q(o * (lt * .5f - .02f), vt + .03f, (ht - .05f) * .5f), dw, Vector3.up, rw, .74f, ht - .05f, .04f);
            BraxUliceKit.S(led).OBox(Q(0f, vt - .39f, ht - .2f), rw, Vector3.up, dw, lt - .2f, .05f, .02f, true);
            BraxUliceKit.Napis3D("SĘDZIOWIE", Q(-1.3f, vt - .38f, .22f), -dw, .13f, .015f, bialeLit, boki);
            BraxUliceKit.Napis3D("KOMENTATORZY", Q(1.95f, vt - .38f, .22f), -dw, .13f, .015f, bialeLit, boki);
            KolO("kolizja: stół sędziów", Q(0f, vt, ht * .5f), dw, new Vector3(lt, ht, .8f));
            var ekran = BraxUliceKit.S(BraxMiastoA.Swieci("Arena: ekrany laptopów", new Color(.25f, .35f, .45f), new Color(.35f, .5f, .65f)));
            var krM = BraxUliceKit.S(BraxMiastoA.Mat("Arena: krzesła stołu (czarne)", new Color(.06f, .06f, .065f), .5f));
            float[] us = { -2.6f, -1.3f, 0f, 1.3f, 2.6f };
            for (int k = 0; k < us.Length; k++)
            {
                float u = us[k]; bool kom = k >= 3;
                // laptop: podstawa na blacie, ekran pochylony do siedzącego (przodem do +dw)
                BraxUliceKit.S(alu).OBox(Q(u, vt + .12f, ht + .01f), rw, Vector3.up, dw, .34f, .02f, .24f);
                var ekr = Q(u, vt - .02f, ht + .13f); var nEk = (dw * .95f + Vector3.up * .3f).normalized;
                ekran.OBox(ekr, rw, Vector3.Cross(nEk, rw).normalized * -1f, nEk, .34f, .22f, .012f);
                if (kom) { BraxUliceKit.S(czarny).Rura(Q(u + .25f, vt + .05f, ht), Q(u + .25f, vt - .05f, ht + .3f), .008f, 6); BraxUliceKit.S(czarny).Walec(Q(u + .25f, vt - .06f, ht + .3f), .025f, .06f, 10); }
                var kp = Q(u, vt + .8f, 0f);
                krM.OBox(kp + Vector3.up * .46f * K, rw, Vector3.up, dw, .48f, .05f, .48f);
                krM.OBox(kp + Vector3.up * .78f * K + dw * .23f, rw, Vector3.up, dw, .48f, .42f * K, .04f);
                foreach (var o in new[] { new Vector2(-.2f, -.2f), new Vector2(.2f, -.2f), new Vector2(-.2f, .2f), new Vector2(.2f, .2f) })
                    BraxUliceKit.S(alu).Rura(kp + rw * o.x + dw * o.y, kp + rw * o.x + dw * o.y + Vector3.up * (.46f * K - .025f), .015f, 6);
                BraxMiastoA.Siedzisko(root, kom ? "KRZESŁO KOMENTATORA" : "KRZESŁO SĘDZIEGO", kp + Vector3.up * (.46f * K + .025f), -dw);
                Czlowiek(kom ? "Stół komentatorów" : "Stół sędziów", kom ? "komentator" : "sędzia punktowy", "siedzi", kp + Vector3.up * (.46f * K + .025f), -dw, KlSiedzi);
            }
            Miejsca["arena_stol_sedziow"] = Q(0f, vt + 1.4f, 0f);
        }
        // światła hali (wyjątek: hala sportowa — ForceVertex przez BraxRegion.InteriorLights)
        BraxRegion.Warm(root, "Arena: światło nad klatką", c + Vector3.up * (y0 + 7f), 2.6f, 14f);
        foreach (var p in new[] { Wsp(1, 14f), Wsp(5, 14f), Wsp(3, 14f), Wsp(7, 14f) }) BraxRegion.Warm(root, "Arena: światło trybun", p + Vector3.up * (y0 + 9f - p.y), 1.4f, 14f);
        BraxRegion.Warm(root, "Arena: światło holu", Wsp(wej, R - 4f) + Vector3.up * (y0 + 4f), 1.2f, 10f);
        Miejsca["arena_srodek"] = top;
        BraxMiastoA.Log("WYSPA arena: trybuny 6 sektorów × " + rzedy + " rzędów, przejść " + nPrz + ", widownia " + widownia.Count + ", foteli VIP " + nVip + ", drzwiczki klatki od tunelu, szatnia Braxa + szatnia gości, stół sędziów i komentatorów, dach membranowy");
    }

    // ---------------- PLAŻE DOOKOŁA WYSPY (A 2.11 16:04): wszędzie, gdzie nie ma przystani ani skał ----------------
    // Linia wody liczona promieniem od środka (pierwszy punkt pod lustrem), rzędy parasoli i leżaków 9 i 16 m od wody co ~7 m
    // wzdłuż brzegu, tylko na piasku (wysokość 0.4–4.8 m, spadek < 11°, z dala od placu i deptaka). Przerwy: przystań (zachód),
    // skalisty cypel (wschód) i 4 naturalne zatoczki ze skałami między plażami. 3 wieżyczki ratowników z bojami kąpieliska,
    // prysznice, kosze. Leżaki bez ręczników (nic nie leży na niczym), oparcie od strony lądu — leży się przodem do morza.
    static readonly float[] Zatoczki = { 22f, 98f, 146f, 274f };
    const float CypelOd = 308f, CypelDo = 352f;
    static readonly float[] Ratownicy = { 228f, 52f, 122f };
    // miejsca zajęte przez wyposażenie plaży (palmy, krzewy i skały stawiane później omijają je — nic nie stoi w niczym)
    static readonly List<(Vector2 p, float r)> zajete = new List<(Vector2 p, float r)>();
    static void Zajmij(Vector3 p, float r) => zajete.Add((new Vector2(p.x, p.z), r));
    static bool Zajete(float wx, float wz, float r) { var q = new Vector2(wx, wz); foreach (var (p, rr) in zajete) if ((p - q).sqrMagnitude < (r + rr) * (r + rr)) return true; return false; }
    static float KatOd(float a, float b) => Mathf.Abs(Mathf.Repeat(a - b + 180f, 360f) - 180f);
    static float LiniaWody(float ang)
    {
        float cx = Mathf.Cos(ang * Mathf.Deg2Rad), cz = Mathf.Sin(ang * Mathf.Deg2Rad), rs = 30f;
        while (rs < 150f && H(cx * rs, cz * rs) > .1f) rs += .5f;
        return rs;
    }
    static bool Plazowo(float ang) => KatOd(ang, 180f) > 11f && !(ang >= CypelOd && ang <= CypelDo) && Zatoczki.All(z => KatOd(ang, z) > 7f);
    static bool Piasek(Vector2 p)
    {
        float h = H(p.x, p.y); if (h < .4f || h > 4.8f) return false;
        float gx = H(p.x + 1f, p.y) - H(p.x - 1f, p.y), gz = H(p.x, p.y + 1f) - H(p.x, p.y - 1f);
        if (Mathf.Sqrt(gx * gx + gz * gz) * .5f > .2f) return false;
        if (Vector2.Distance(p, Plac) < PlacR + 7f) return false;
        if (Mathf.Abs(p.y - Plac.y) < 8f && p.x < Plac.x) return false;    // pomost i deptak
        return true;
    }
    static Vector3 NaBrzegu(float ang, float odWody)
    {
        float rs = LiniaWody(ang); var p = new Vector3(CX + Mathf.Cos(ang * Mathf.Deg2Rad) * (rs - odWody), 0, CZ + Mathf.Sin(ang * Mathf.Deg2Rad) * (rs - odWody));
        p.y = Y(p.x, p.z); return p;
    }
    static void Plaze(BraxNabrzeze.Zbior z, BraxNabrzezeMebel.Mat m)
    {
        var plotna = new[] { m.plotno0, m.plotno1, m.plotno2, m.plotno3 }; int nP = 0, nL = 0, nK = 0, nR = 0, nS = 0, nB = 0;
        var str = new GameObject("Wyspa: plaże (parasole, leżaki, ratownicy, prysznice, kosze)").transform; str.SetParent(root, false);
        var rnd = new System.Random(2111);
        // wieżyczki ratowników: najbliższy kąt plażowy przy zadanym
        var lg = new List<float>();
        foreach (float a0 in Ratownicy)
            foreach (float d in new[] { 0f, 3f, -3f, 6f, -6f, 9f, -9f, 12f, -12f })
            {
                float a = Mathf.Repeat(a0 + d, 360f); var p = NaBrzegu(a, 12f);
                if (Plazowo(a) && Piasek(new Vector2(p.x - CX, p.z - CZ))) { lg.Add(a); break; }
            }
        float ang = 0f; int kolumna = 0;
        while (ang < 360f)
        {
            float rs = LiniaWody(ang); var d = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0, Mathf.Sin(ang * Mathf.Deg2Rad));
            float krok = 7.2f / Mathf.Max(40f, rs) * Mathf.Rad2Deg;
            bool odRatownika = lg.All(a => KatOd(ang, a) * Mathf.Deg2Rad * rs > 6.5f);
            if (Plazowo(ang) && odRatownika)
            {
                var t = Vector3.Cross(Vector3.up, d);
                foreach (float odl in new[] { 9f, 16f })
                {
                    var p2 = new Vector2(d.x, d.z) * (rs - odl); if (!Piasek(p2)) continue;
                    var p = new Vector3(CX + p2.x, 0, CZ + p2.y); p.y = Y(p.x, p.z);
                    BraxNabrzezeMebel.Parasol(z, str, m, plotna[nP % plotna.Length], p, 2f + (nP % 3)); nP++; Zajmij(p, 1.6f);
                    float yawL = Quaternion.LookRotation(-d).eulerAngles.y;     // oparcie od lądu: leży się przodem do morza
                    foreach (float s in new[] { -1.05f, 1.05f })
                    {
                        var lp = p + t * s * K + d * 1.2f * K; lp.y = Y(lp.x, lp.z) + .01f;
                        BraxNabrzezeMebel.Lezak(z, str, m, null, lp, yawL); nL++; Zajmij(lp, 1.4f);
                        if (rnd.NextDouble() < .35)
                        {
                            var sp = lp + Quaternion.Euler(0, yawL, 0) * new Vector3(0, .32f * K + .04f * K, -1.95f * K * .12f);
                            Czlowiek("Plaża wyspy", "plażowicz", "leży", sp, d, KlLezy);
                        }
                    }
                }
                if (kolumna % 4 == 1)
                {
                    var p2 = new Vector2(d.x, d.z) * (rs - 21.5f);
                    if (Piasek(p2)) { var p = new Vector3(CX + p2.x, 0, CZ + p2.y); p.y = Y(p.x, p.z); BraxNabrzezeMebel.Kosz(z, str, m, p); nK++; Zajmij(p, .6f); }
                }
            }
            ang += krok; kolumna++;
        }
        // ratownicy: wieżyczka przodem do wody, przed nią ratownik; prysznic za rzędami; boje kąpieliska 28 m od brzegu
        var boja = BraxNabrzeze.Kolor("Wyspa: boje kąpieliska (żółte)", new Color(.95f, .78f, .1f), .5f);
        for (int k = 0; k < lg.Count; k++)
        {
            float a = lg[k], rs = LiniaWody(a); var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)); var t = Vector3.Cross(Vector3.up, d);
            var p = NaBrzegu(a, 12f); p.y -= .05f;
            var w = BraxHarborBeachBuild.Postaw(root, "lifeguard_tower.glb", "Wyspa: wieżyczka ratownika " + (k + 1), p, 6.2f, Quaternion.LookRotation(d).eulerAngles.y + 180f);
            if (w) { var bb = BraxRegion.Bounds(w); BraxNabrzeze.Kol(kol, "Wieżyczka ratownika", bb.center, bb.size * .85f); nR++; Zajmij(bb.center, Mathf.Max(bb.extents.x, bb.extents.z) + .5f); }
            Zajmij(p + d * 3f, 1.2f);
            string id = k == 0 ? "wyspa_ratownik" : "wyspa_ratownik_" + (k + 1);
            Miejsca[id] = p + d * 3f;
            BraxMiastoA.Akcja(root, id, "POROZMAWIAJ", "Ratownik: Kąpielisko strzeżone do żółtych boi. Za bojami pływają łodzie.", p + d * 3.5f, 2f);
            Czlowiek("Plaża wyspy", "ratownik", "stoi", p + d * 2.6f + t * .8f, d, KlStoi);
            var pr = new Vector3(CX, 0, CZ) + d * (rs - 20f) + t * 4f; pr.y = Y(pr.x, pr.z);
            if (Piasek(new Vector2(pr.x - CX, pr.z - CZ)) && !Zajete(pr.x, pr.z, .8f)) { BraxNabrzezeMebel.Prysznic(z, str, m, pr, Quaternion.LookRotation(d).eulerAngles.y); nS++; Zajmij(pr, 1.0f); }
            for (int b = -3; b <= 3; b++)
            {
                float ab = a + b * 8f / (rs + 28f) * Mathf.Rad2Deg; var bp = new Vector3(CX + Mathf.Cos(ab * Mathf.Deg2Rad) * (LiniaWody(ab) + 28f), W, CZ + Mathf.Sin(ab * Mathf.Deg2Rad) * (LiniaWody(ab) + 28f));
                if (H(bp.x - CX, bp.z - CZ) < -1.2f) { BraxNabrzezeMebel.Boja(z, boja, bp, .32f); nB++; }
            }
        }
        // zatoczki ze skałami (między plażami) i skalisty cypel
        var sk = new List<GameObject>(); foreach (var id in new[] { "coast_rocks_01", "coast_rocks_03", "boulder_01", "namaqualand_boulder_02" }) { var mm = BraxRegion.Model(id); if (mm) sk.Add(mm); }
        int nSk = 0;
        if (sk.Count > 0)
        {
            var zs = new GameObject("Wyspa: zatoczki ze skałami").transform; zs.SetParent(root, false);
            var katy = new List<float>(Zatoczki); for (float a = CypelOd + 6f; a < CypelDo; a += 9f) katy.Add(a);
            foreach (float a0 in katy)
                for (int q = 0; q < 6; q++)
                {
                    float a = a0 + ((float)rnd.NextDouble() - .5f) * 10f, rs = LiniaWody(a), r = rs + ((float)rnd.NextDouble() - .55f) * 9f;
                    var p = new Vector3(CX + Mathf.Cos(a * Mathf.Deg2Rad) * r, 0, CZ + Mathf.Sin(a * Mathf.Deg2Rad) * r); p.y = Y(p.x, p.z);
                    if (p.y < W - 1.4f || Zajete(p.x, p.z, 1.8f)) continue;
                    if (BraxRegion.Put(zs, sk[rnd.Next(sk.Count)], "Wyspa: skała zatoczki", p + Vector3.down * .35f, (float)rnd.NextDouble() * 360f, 1.2f + (float)rnd.NextDouble() * 1.1f, true, 3.4f)) nSk++;
                }
        }
        BraxMiastoA.Log("WYSPA plaże dookoła: parasoli " + nP + ", leżaków " + nL + ", ratowników " + nR + ", pryszniców " + nS + ", koszy " + nK + ", boi " + nB + ", skał w zatoczkach " + nSk);
    }

    // ---------------- ZAPOWIEDŹ WALKI (tekstura z kodu: krój liter przestrzennych, puste pola na nazwiska) ----------------
    static string zapowiedz;
    static string Zapowiedz()
    {
        if (zapowiedz != null) return zapowiedz;
        const int Wt = 1024, Ht = 640; var px = new Color[Wt * Ht];
        Color tlo0 = new Color(.012f, .016f, .024f), tlo1 = new Color(.03f, .06f, .04f), ziel = new Color(.45f, 1f, .2f);
        Color czer = new Color(.5f, .05f, .05f), nieb = new Color(.06f, .14f, .5f);
        for (int y = 0; y < Ht; y++)
            for (int x = 0; x < Wt; x++)
            {
                float u = x / (float)Wt, v = y / (float)Ht;
                var c = Color.Lerp(tlo0, tlo1, v * .7f + Mathf.PerlinNoise(u * 4f, v * 4f) * .3f);
                float sm = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((u * 1.6f - v) * 10f)), 30f) * .07f;
                c += new Color(sm * .5f, sm, sm * .4f);
                float vin = 1f - .55f * ((u - .5f) * (u - .5f) + (v - .55f) * (v - .55f)) * 2.2f; c *= vin; c.a = 1f;
                px[y * Wt + x] = c;
            }
        void Prost(int x0, int y0, int x1, int y1, Color k, float a = 1f)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(Ht, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(Wt, x1); x++) { var c = px[y * Wt + x]; px[y * Wt + x] = new Color(Mathf.Lerp(c.r, k.r, a), Mathf.Lerp(c.g, k.g, a), Mathf.Lerp(c.b, k.b, a), 1f); }
        }
        void Ramka(int x0, int y0, int x1, int y1, int g, Color k, float a = 1f) { Prost(x0, y0, x1, y0 + g, k, a); Prost(x0, y1 - g, x1, y1, k, a); Prost(x0, y0, x0 + g, y1, k, a); Prost(x1 - g, y0, x1, y1, k, a); }
        // pasy marki u góry i u dołu
        Prost(0, 596, Wt, 602, ziel); Prost(0, 60, Wt, 112, ziel, .92f);
        BraxUliceKit.NapisWTeksturze(px, Wt, Ht, "ARENA BRAXHORN", Wt * .5f, 520f, 54f, Color.white);
        BraxUliceKit.NapisWTeksturze(px, Wt, Ht, "GALA MISTRZOWSKA · WALKA O PAS", Wt * .5f, 470f, 24f, ziel);
        // narożniki: czerwony (lewo) i niebieski (prawo) — pole na nazwisko PUSTE (wpisze je misja / etap walki)
        foreach (var (x0, kk, napis) in new[] { (56, czer, "NAROŻNIK CZERWONY"), (588, nieb, "NAROŻNIK NIEBIESKI") })
        {
            Prost(x0, 150, x0 + 380, 430, kk, .55f); Ramka(x0, 150, x0 + 380, 430, 4, kk * 1.8f);
            BraxUliceKit.NapisWTeksturze(px, Wt, Ht, napis, x0 + 190f, 384f, 19f, Color.white);
            Prost(x0 + 34, 226, x0 + 346, 316, new Color(.02f, .02f, .025f), .85f); Ramka(x0 + 34, 226, x0 + 346, 316, 2, new Color(.85f, .85f, .85f), .7f);
            BraxUliceKit.NapisWTeksturze(px, Wt, Ht, "ZAWODNIK", x0 + 190f, 186f, 15f, new Color(.8f, .8f, .8f));
        }
        BraxUliceKit.NapisWTeksturze(px, Wt, Ht, "VS", Wt * .5f, 258f, 64f, Color.white);
        BraxUliceKit.NapisWTeksturze(px, Wt, Ht, "LIGA BRAXHORN · WALKA WIECZORU", Wt * .5f, 76f, 22f, new Color(.02f, .05f, .02f));
        var tx = new Texture2D(Wt, Ht, TextureFormat.RGB24, false); tx.SetPixels(px); tx.Apply();
        string path = BraxMiastoA.Out + "/Wyspa/zapowiedz_walki.png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.mipmapEnabled = true; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport(); }
        zapowiedz = path; return path;
    }

    // ---------------- MIEJSCA LUDZI (format etapu ludzi build 14 — BraxClaudeOsiedleLudzieMiejsca) ----------------
    // Wykonawca B podpina postacie (paczka Rocketbox) — tu tylko miejsca: gdzie, rola, poza (stoi / siedzi / leży / spacer),
    // punkt (podłoga albo siedzisko), kierunek, klipy z paczki; spacer ma trasę. Zapis: znaczniki pod „Wyspa: miejsca ludzi”
    // i plik ludzie_miejsca.json obok siatek wyspy. „Leży” (leżaki) wymaga klipu leżenia — w paczce build 14 go nie ma.
    [System.Serializable] sealed class Osoba { public string gdzie, rola, poza; public Vector3 p; public float yaw; public string[] klipy; public Vector3[] trasa; }
    [System.Serializable] sealed class ObsadaWyspy { public string format, uwaga; public List<Osoba> osoby = new List<Osoba>(); }
    static readonly List<Osoba> osoby = new List<Osoba>();
    static readonly string[] KlSiedzi = { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" };
    static readonly string[] KlKibic = { "sit_chair_idle_neutral_02", "sit_chair_breathe_01", "cheer_01" };
    static readonly string[] KlLada = { "work_mid", "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" };
    static readonly string[] KlStoi = { "idle_neutral_01", "idle_neutral_02", "idle_neutral_03", "idle_waiting_01" };
    static readonly string[] KlSpacer = { "walk_stroll_01", "walk_neutral_01", "walk_neutral_02" };
    static readonly string[] KlLezy = { };
    static void Czlowiek(string gdzie, string rola, string poza, Vector3 p, Vector3 przodem, string[] klipy, Vector3[] trasa = null)
    {
        przodem.y = 0f;
        osoby.Add(new Osoba { gdzie = gdzie, rola = rola, poza = poza, p = p, yaw = przodem.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(przodem.normalized).eulerAngles.y : 0f, klipy = klipy, trasa = trasa });
    }
    static void Ludzie()
    {
        var g = new GameObject("Wyspa: miejsca ludzi (format etapu ludzi build 14)").transform; g.SetParent(root, false);
        foreach (var o in osoby)
        {
            var t = new GameObject("osoba: " + o.poza + " · " + o.rola + " · " + o.gdzie).transform; t.SetParent(g, false);
            t.position = o.p; t.rotation = Quaternion.Euler(0, o.yaw, 0);
            if (o.trasa != null) for (int i = 0; i < o.trasa.Length; i++) { var w = new GameObject("trasa " + i).transform; w.SetParent(t, true); w.position = o.trasa[i]; }
        }
        var ob = new ObsadaWyspy
        {
            format = "BraxClaudeOsiedleLudzieMiejsca (build 14): gdzie, rola, poza (stoi / siedzi / leży / spacer), p (podłoga albo siedzisko), yaw, klipy z paczki Rocketbox, trasa (spacer)",
            uwaga = "Wykonawca A przygotował miejsca; postacie podpina etap ludzi (wykonawca B). Poza „leży” wymaga klipu leżenia.",
            osoby = osoby
        };
        File.WriteAllText(BraxMiastoA.Out + "/Wyspa/ludzie_miejsca.json", JsonUtility.ToJson(ob, true));
        AssetDatabase.ImportAsset(BraxMiastoA.Out + "/Wyspa/ludzie_miejsca.json");
        BraxMiastoA.Log("WYSPA ludzie (miejsca): razem " + osoby.Count + " — " + string.Join(", ", osoby.GroupBy(o => o.poza).Select(q => q.Key + " " + q.Count())));
    }

    // ---------------- bar plażowy (strzecha, lada, hokery, półki) ----------------
    static void BarPlazowy()
    {
        var c = new Vector3(CX + Plac.x - 18f, W + PlacH, CZ + Plac.y - 22f);   // na placu (płasko), róg południowo-zachodni
        var drewno = BraxMiastoA.Tex("Wyspa: bar drewno", 1.2f, new Color(.8f, .65f, .48f), new Color(.5f, .38f, .26f), .7f, "wood_planks_grey", "wooden_planks");
        var strzecha = BraxMiastoA.Mat("Wyspa: strzecha (trzcina)", new Color(.62f, .52f, .32f), .95f);
        var blat = BraxMiastoA.Mat("Wyspa: blat baru (teak)", new Color(.45f, .3f, .18f), .45f);
        float w = 6f, d = 4f, h = 2.9f;
        var dz = BraxUliceKit.S(drewno);
        dz.Box(new Vector3(c.x - w * .5f, c.y - .2f, c.z - d * .5f), new Vector3(c.x + w * .5f, c.y + .15f, c.z + d * .5f), 0);   // podłoga baru (podest 15 cm)
        foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) }) dz.Rura(new Vector3(c.x + o.x * (w * .5f - .15f), c.y + .15f, c.z + o.y * (d * .5f - .15f)), new Vector3(c.x + o.x * (w * .5f - .15f), c.y + h, c.z + o.y * (d * .5f - .15f)), .12f, 10);
        // dach czterospadowy ze strzechy (nawis 0.8 m)
        var s = BraxUliceKit.S(strzecha); float yd = c.y + h, ov = .8f, ap = 1.8f;
        var A = new Vector3(c.x - w * .5f - ov, yd, c.z - d * .5f - ov); var B = new Vector3(c.x + w * .5f + ov, yd, c.z - d * .5f - ov); var Cc = new Vector3(c.x + w * .5f + ov, yd, c.z + d * .5f + ov); var D = new Vector3(c.x - w * .5f - ov, yd, c.z + d * .5f + ov);
        var t0 = new Vector3(c.x - 1f, yd + ap, c.z); var t1 = new Vector3(c.x + 1f, yd + ap, c.z);
        s.Quad(A, B, t1, t0, Vector3.Cross(B - A, t0 - A).normalized * -1f); s.Quad(Cc, D, t0, t1, Vector3.Cross(D - Cc, t1 - Cc).normalized * -1f);
        s.Tri(D, A, t0, Vector3.left); s.Tri(B, Cc, t1, Vector3.right);
        s.Quad(A, B, Cc, D, Vector3.down);
        // lada (U) i półki
        var bl = BraxUliceKit.S(blat); float lh = 1.1f * K;
        dz.Box(new Vector3(c.x - w * .5f + .3f, c.y + .15f, c.z - d * .5f + .2f), new Vector3(c.x + w * .5f - .3f, c.y + lh - .05f, c.z - d * .5f + .7f));
        bl.Box(new Vector3(c.x - w * .5f + .25f, c.y + lh - .05f, c.z - d * .5f + .1f), new Vector3(c.x + w * .5f - .25f, c.y + lh, c.z - d * .5f + .75f));
        dz.Box(new Vector3(c.x - w * .5f + .3f, c.y + .15f, c.z + d * .5f - .5f), new Vector3(c.x + w * .5f - .3f, c.y + 2.2f, c.z + d * .5f - .2f));
        BraxMiastoA.Kol(kol, "bar plażowy: lada", new Vector3(c.x - w * .5f + .25f, c.y, c.z - d * .5f + .1f), new Vector3(c.x + w * .5f - .25f, c.y + lh, c.z - d * .5f + .75f));
        BraxMiastoA.Kol(kol, "bar plażowy: podłoga", new Vector3(c.x - w * .5f, c.y - .2f, c.z - d * .5f), new Vector3(c.x + w * .5f, c.y + .15f, c.z + d * .5f));
        BraxMiastoA.Kol(kol, "bar plażowy: dach", new Vector3(c.x - w * .5f - ov, yd, c.z - d * .5f - ov), new Vector3(c.x + w * .5f + ov, yd + ap, c.z + d * .5f + ov));
        var chrom = BraxMiastoA.Mat("Wyspa: stal hokerów", new Color(.7f, .7f, .72f), .3f, .9f);
        for (float x = c.x - w * .5f + .8f; x < c.x + w * .5f - .5f; x += 1.0f)
        {
            var p = new Vector3(x, 0, c.z - d * .5f - .55f); p.y = Y(p.x, p.z);
            BraxUliceKit.S(chrom).Rura(p, p + Vector3.up * .74f * K, .03f, 8); BraxUliceKit.S(blat).Walec(p + Vector3.up * .74f * K, .2f * K, .06f, 16);
            BraxMiastoA.Siedzisko(root, "HOKER", p + Vector3.up * (.74f * K + .06f), Vector3.forward);
            if (Mathf.RoundToInt(x - (c.x - w * .5f + .8f)) % 2 == 0 && x < c.x + 1f) Czlowiek("Bar plażowy", "gość baru", "siedzi", p + Vector3.up * (.74f * K + .06f), Vector3.forward, KlSiedzi);
        }
        foreach (float o in new[] { -1.2f, 1.2f }) Czlowiek("Bar plażowy", "barman", "stoi", new Vector3(c.x + o, c.y + .15f, c.z - .45f), Vector3.back, KlLada);
        BraxUliceKit.Napis3D("BEACH BAR BRAXHORN", new Vector3(c.x, yd - .45f, c.z - d * .5f - ov + .02f), Vector3.back, .26f, .04f, BraxMiastoA.Swieci("Arena: litery szyldu (podświetlane)", Color.white, new Color(2f, 2f, 2f)), BraxMiastoA.Mat("Arena: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f));
        Miejsca["wyspa_bar"] = c + Vector3.back * 3f;
        BraxMiastoA.Akcja(root, "wyspa_bar", "ZAMÓW", "Barman: Lemoniada, kokos, coś zimnego przed galą?", new Vector3(c.x, c.y, c.z - d * .5f - 1.2f), 2f);
    }

    // ---------------- PLAC WEJŚCIOWY ARENY (A 2.11 16:04 „arena festiwalowa”): pylon z napisem, telebimy plenerowe z zapowiedzią
    // walki, brama z bramkami biletowymi (tor środkowy w osi deptaka), flagi, 3 food trucki z obsługą. Wszystko stoi na płytach
    // placu (wysokość z placu), kolizje na bryłach; między elementami wolne przejścia ≥ 1.1 m.
    static void PlacWejsciowy()
    {
        float y = W + PlacH, zc = CZ + Plac.y, xw = CX + Plac.x + 4f - ArenaR * Mathf.Cos(Mathf.PI / 8f);   // lico ściany wejścia
        var czarny = BraxMiastoA.Mat("Plac areny: stal czarna", new Color(.05f, .05f, .055f), .45f, .6f);
        var stal = BraxMiastoA.Mat("Plac areny: stal nierdzewna", new Color(.62f, .63f, .65f), .3f, .9f);
        var led = BraxMiastoA.Swieci("Arena: pas LED zielony (marka)", new Color(.45f, 1f, .2f), new Color(.8f, 2.2f, .3f));
        var lit = BraxMiastoA.Swieci("Arena: litery szyldu (podświetlane)", Color.white, new Color(2f, 2f, 2f));
        var boki = BraxMiastoA.Mat("Arena: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        void KolO(string nazwa, Vector3 srodek, Vector3 przod, Vector3 rozmiar)
        {
            var g = new GameObject(nazwa); g.transform.SetParent(kol, false); g.transform.position = srodek;
            przod.y = 0f; g.transform.rotation = Quaternion.LookRotation(przod.normalized, Vector3.up); g.AddComponent<BoxCollider>().size = rozmiar;
        }
        // 1) PYLON „ARENA BRAXHORN” przy końcu deptaka (9 m, podświetlane litery z obu stron, pas LED)
        {
            var p = new Vector3(CX + Plac.x - PlacR + 4.5f, y, zc - 7f); float hp = 9f, wp = 3.4f, dp = .7f;
            BraxUliceKit.S(czarny).OBox(p + Vector3.up * hp * .5f, Vector3.forward, Vector3.up, Vector3.right, wp, hp, dp);
            KolO("kolizja: pylon areny", p + Vector3.up * hp * .5f, Vector3.right, new Vector3(wp, hp, dp));
            foreach (float sx in new[] { -1f, 1f })
            {
                var n = Vector3.right * sx; var lico = p + n * dp * .5f;
                float c1 = .62f, w1 = BraxUliceKit.SzerokoscNapisu("ARENA", c1); if (w1 > wp - .5f) c1 *= (wp - .5f) / w1;
                float c2 = .5f, w2 = BraxUliceKit.SzerokoscNapisu("BRAXHORN", c2); if (w2 > wp - .4f) c2 *= (wp - .4f) / w2;
                BraxUliceKit.Napis3D("ARENA", lico + Vector3.up * 6.9f, n, c1, .12f, lit, boki);
                BraxUliceKit.Napis3D("BRAXHORN", lico + Vector3.up * 6.0f, n, c2, .12f, lit, boki);
                BraxUliceKit.S(led).OBox(lico + Vector3.up * 5.6f + n * .02f, Vector3.forward, Vector3.up, n, wp - .4f, .08f, .04f, true);
            }
            Miejsca["arena_pylon"] = p - Vector3.right * 2f;
        }
        // 2) TELEBIMY PLENEROWE (7.2 × 4.5 m na dwóch słupach) — przód to sam obraz (ściana szafy), tył czarna szafa
        string zap = Zapowiedz(); int nT = 0;
        foreach (float sz in new[] { -1f, 1f })
        {
            var p = new Vector3(CX + Plac.x - PlacR + 7f, y, zc + sz * 12f); var N = new Vector3(-1f, 0f, -sz * .22f).normalized; var Rv = Vector3.Cross(N, Vector3.up);
            float tw = 7.2f, th = 4.5f, tb0 = 3.4f, tg = .5f;
            var srodek = p + Vector3.up * (tb0 + th * .5f) - N * tg * .5f;
            BraxUliceKit.S(czarny).OBox(srodek, Rv, Vector3.up, -N, tw + .2f, th + .2f, tg, true);   // szafa BEZ przodu — przodem jest obraz
            // lico szafy: obraz w płaszczyźnie przodu, wokół ramka z czterech listew (przed płaszczyzną, obok obrazu — nie na nim)
            BraxMiastoA.Obraz(root, "telebim plenerowy " + (sz < 0 ? "płd." : "płn."), zap, p + Vector3.up * tb0 - Rv * tw * .5f, Rv, tw, th, N, 1.0f);
            var cE = p + Vector3.up * (tb0 + th * .5f) + N * .03f; var ram = BraxUliceKit.S(czarny);
            foreach (float o in new[] { -1f, 1f })
            {
                ram.OBox(cE + Vector3.up * o * (th * .5f + .05f), Rv, Vector3.up, N, tw + .2f, .1f, .06f);
                ram.OBox(cE + Rv * o * (tw * .5f + .05f), Rv, Vector3.up, N, .1f, th, .06f);
            }
            foreach (float o in new[] { -1f, 1f }) BraxUliceKit.S(czarny).Rura(p + Rv * o * 2.6f - N * tg * .5f, p + Rv * o * 2.6f - N * tg * .5f + Vector3.up * tb0, .16f, 12);
            foreach (float o in new[] { -1f, 1f }) KolO("kolizja: słup telebimu", p + Rv * o * 2.6f - N * tg * .5f + Vector3.up * tb0 * .5f, N, new Vector3(.35f, tb0, .35f));
            nT++;
        }
        // 3) BRAMA WEJŚCIOWA z bramkami biletowymi: belka z napisem na dwóch słupach, 8 szafek bramek (szklane skrzydła
        //    schowane — tory otwarte), tor środkowy w osi deptaka
        {
            float xg = xw - 7.5f, hb = 4.6f, half = 8.6f;
            foreach (float sz in new[] { -1f, 1f })
            {
                var sp = new Vector3(xg, y, zc + sz * half);
                BraxUliceKit.S(czarny).OBox(sp + Vector3.up * hb * .5f, Vector3.forward, Vector3.up, Vector3.right, .45f, hb, .45f);
                KolO("kolizja: słup bramy", sp + Vector3.up * hb * .5f, Vector3.right, new Vector3(.45f, hb, .45f));
            }
            var belka = new Vector3(xg, y + hb - .45f, zc);
            BraxUliceKit.S(czarny).OBox(belka, Vector3.forward, Vector3.up, Vector3.right, 2f * half - .45f, .9f, .5f);
            float cg = .42f, wg = BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN · WEJŚCIE", cg); if (wg > 2f * half - 1.4f) cg *= (2f * half - 1.4f) / wg;
            BraxUliceKit.Napis3D("ARENA BRAXHORN · WEJŚCIE", belka + Vector3.left * .25f - Vector3.up * .22f, Vector3.left, cg, .06f, lit, boki);
            BraxUliceKit.S(led).OBox(belka + Vector3.left * .27f + Vector3.up * .38f, Vector3.forward, Vector3.up, Vector3.left, 2f * half - .6f, .06f, .04f, true);
            var szkloCz = BraxMiastoA.Mat("Plac areny: szkło bramek (czarne)", new Color(.03f, .03f, .035f), .08f, .3f);
            int nb = 0;
            for (int k = -4; k <= 4; k++)
            {
                if (k == 0) continue;
                float zz = zc + (k > 0 ? k - .5f : k + .5f) * 1.4f;      // szafki co 1.4 m, tor 1.15 m, środek wolny
                var cp = new Vector3(xg, y, zz);
                BraxUliceKit.S(stal).OBox(cp + Vector3.up * .48f, Vector3.forward, Vector3.up, Vector3.right, .25f, .96f, 1.4f);
                BraxUliceKit.S(szkloCz).OBox(cp + Vector3.up * .975f, Vector3.forward, Vector3.up, Vector3.right, .23f, .03f, 1.36f, false, true);
                BraxUliceKit.S(led).OBox(cp + Vector3.up * .995f + Vector3.left * .5f, Vector3.forward, Vector3.up, Vector3.right, .08f, .01f, .2f, false, true);
                KolO("kolizja: szafka bramki", cp + Vector3.up * .5f, Vector3.right, new Vector3(.25f, 1f, 1.4f));
                nb++;
            }
            // barierki między skrajnymi szafkami a słupami — wejście tylko torami bramek
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 b0 = new Vector3(xg, y, zc + sz * 5.2f), b1 = new Vector3(xg, y, zc + sz * 8.3f);
                BraxUliceKit.S(stal).Rura(b0 + Vector3.up * 1.05f, b1 + Vector3.up * 1.05f, .025f, 8); BraxUliceKit.S(stal).Rura(b0 + Vector3.up * .3f, b1 + Vector3.up * .3f, .02f, 8);
                for (int k = 0; k <= 4; k++) { var bp = Vector3.Lerp(b0, b1, k / 4f); BraxUliceKit.S(stal).Rura(bp, bp + Vector3.up * 1.05f, .022f, 6); }
                for (int k = 1; k < 26; k++) { var bp = Vector3.Lerp(b0, b1, k / 26f); BraxUliceKit.S(stal).Rura(bp + Vector3.up * .3f, bp + Vector3.up * 1.05f, .009f, 4); }
                KolO("kolizja: barierka bramy", (b0 + b1) * .5f + Vector3.up * .55f, Vector3.right, new Vector3(3.1f, 1.1f, .1f));
            }
            BraxMiastoA.Akcja(root, "arena_bramki", "POKAŻ BILET", "Bramki ARENY BRAXHORN: bilet zeskanowany — zapraszamy na galę.", new Vector3(xg - 1.2f, y, zc), 1.8f);
            Miejsca["arena_bramki"] = new Vector3(xg - 1.2f, y, zc);
            Czlowiek("Bramki ARENY", "bileter", "stoi", new Vector3(xg + 1.1f, y, zc + 1.4f), Vector3.left, KlStoi);
            Czlowiek("Bramki ARENY", "bileter", "stoi", new Vector3(xg + 1.1f, y, zc - 1.4f), Vector3.left, KlStoi);
        }
        // 4) FLAGI (8 m maszty, flagi marki zielono-czarne, sztywny płat 2 cm)
        var ziel = BraxMiastoA.Mat("Plac areny: flaga (zieleń marki)", new Color(.28f, .72f, .14f), .8f);
        var czern = BraxMiastoA.Mat("Plac areny: flaga (czerń)", new Color(.04f, .04f, .045f), .8f);
        int nF = 0;
        foreach (float sz in new[] { -1f, 1f })
            foreach (float zo in new[] { 10.4f, 12.8f, 15.2f })
            {
                var fp = new Vector3(xw - 4.2f, y, zc + sz * zo);
                BraxUliceKit.S(stal).Rura(fp, fp + Vector3.up * 8f, .055f, 10); BraxUliceKit.S(stal).Walec(fp, .2f, .05f, 14);
                BraxUliceKit.S(stal).Rura(fp + Vector3.up * 8f, fp + Vector3.up * 8.12f, .08f, 10);
                var fc = fp + Vector3.up * 7.2f + Vector3.forward * sz * .97f;
                BraxUliceKit.S(ziel).OBox(fc + Vector3.up * .3f, Vector3.forward, Vector3.up, Vector3.right, 1.8f, .6f, .02f);
                BraxUliceKit.S(czern).OBox(fc - Vector3.up * .3f, Vector3.forward, Vector3.up, Vector3.right, 1.8f, .6f, .02f);
                KolO("kolizja: maszt flagi", fp + Vector3.up * 1.5f, Vector3.right, new Vector3(.15f, 3f, .15f));
                nF++;
            }
        // 5) FOOD TRUCKI (5.4 × 2.3 m, kabina, otwarte okno wydawki z daszkiem, lada, zaplecze, szyld na dachu, obsługa)
        var trucki = new[]
        {
            (new Vector3(CX + Plac.x - 26f, y, zc + 19f), Vector3.back, "BURGERY", new Color(.12f, .42f, .2f)),
            (new Vector3(CX + Plac.x - 19f, y, zc + 19f), Vector3.back, "KAWA · LODY", new Color(.92f, .9f, .86f)),
            (new Vector3(CX + Plac.x - 14f, y, zc - 17f), Vector3.forward, "KOKOS · SOKI", new Color(.85f, .45f, .1f)),
        };
        int nTr = 0;
        foreach (var (p, f, tekst, kolor) in trucki)
        {
            nTr++;
            var s = Vector3.Cross(Vector3.up, f); float L = 5.4f, Wd = 2.3f, Lb = L - 1.5f, sb = -.75f;
            Vector3 T(float u, float yy, float v) => p + s * u + Vector3.up * yy + f * v;
            var lak = BraxUliceKit.S(BraxMiastoA.Mat("Food truck: lakier " + tekst, kolor, .35f, .2f));
            var wn = BraxUliceKit.S(BraxMiastoA.Mat("Food truck: wnętrze (stal)", new Color(.55f, .56f, .58f), .35f, .8f));
            var szk = BraxUliceKit.S(BraxMiastoA.Mat("Food truck: szyby (ciemne)", new Color(.05f, .07f, .08f), .08f, .3f));
            var opona = BraxUliceKit.S(BraxMiastoA.Mat("Food truck: opony", new Color(.03f, .03f, .03f), .9f));
            lak.OBox(T(0, .65f, 0), s, Vector3.up, f, L, .3f, Wd);                                            // podłoga / podwozie
            lak.OBox(T(L * .5f - .75f, 1.25f, 0), s, Vector3.up, f, 1.5f, .9f, Wd);                           // kabina dół
            szk.OBox(T(L * .5f - .8f, 2.1f, 0), s, Vector3.up, f, 1.3f, .8f, Wd - .1f);                       // kabina szyby
            lak.OBox(T(L * .5f - .8f, 2.55f, 0), s, Vector3.up, f, 1.3f, .1f, Wd - .1f);                      // dach kabiny
            lak.OBox(T(sb, 1.65f, -Wd * .5f + .03f), s, Vector3.up, f, Lb, 1.7f, .06f);                       // ściana tylna
            lak.OBox(T(sb, 1.1f, Wd * .5f - .03f), s, Vector3.up, f, Lb, .6f, .06f);                          // ściana pod okienkiem
            lak.OBox(T(-L * .5f + .03f, 1.65f, 0), s, Vector3.up, f, .06f, 1.7f, Wd - .12f);                  // ściana tylna (koniec)
            lak.OBox(T(L * .5f - 1.53f, 1.65f, 0), s, Vector3.up, f, .06f, 1.7f, Wd - .12f);                  // przegroda za kabiną
            foreach (float u in new[] { -L * .5f + .125f, L * .5f - 1.5f - .125f }) lak.OBox(T(u, 1.95f, Wd * .5f - .03f), s, Vector3.up, f, .25f, 1.1f, .06f);   // słupki okna
            lak.OBox(T(sb, 2.55f, 0), s, Vector3.up, f, Lb, .1f, Wd);                                          // dach
            wn.OBox(T(sb, 1.425f, Wd * .5f - .1f), s, Vector3.up, f, Lb - .55f, .04f, .5f);                    // lada (wystaje 15 cm)
            wn.OBox(T(sb, 1.25f, -Wd * .5f + .38f), s, Vector3.up, f, Lb - .3f, .9f, .6f);                     // blat roboczy przy ścianie tylnej
            BraxUliceKit.S(czarny).OBox(T(sb - .9f, 1.88f, -Wd * .5f + .38f), s, Vector3.up, f, .5f, .35f, .45f);   // ekspres / grill
            BraxUliceKit.S(BraxMiastoA.Swieci("Food truck: listwa LED wnętrza", Color.white, new Color(1.4f, 1.4f, 1.3f))).OBox(T(sb, 2.47f, Wd * .5f - .25f), s, Vector3.up, f, Lb - .6f, .03f, .06f);
            // daszek okna (uchylony 15° na zewnątrz), szyld na dachu z literami od strony klientów
            var fr = (f * Mathf.Cos(15f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(15f * Mathf.Deg2Rad)).normalized; var br = Vector3.Cross(fr, s).normalized; if (br.y < 0) br = -br;
            lak.OBox(T(sb, 2.5f, Wd * .5f) + fr * .55f, s, br, fr, Lb - .1f, .04f, 1.1f);
            BraxUliceKit.S(czarny).OBox(T(sb, 2.95f, 0), s, Vector3.up, f, Lb - .4f, .7f, .08f);
            float ct = .26f, wt = BraxUliceKit.SzerokoscNapisu(tekst, ct); if (wt > Lb - .7f) ct *= (Lb - .7f) / wt;
            BraxUliceKit.Napis3D(tekst, T(sb, 2.82f, .04f), f, ct, .03f, lit, boki);
            foreach (float u in new[] { -L * .5f + .9f, L * .5f - .9f }) foreach (float v in new[] { -1f, 1f }) opona.Rura(T(u, .38f, v * (Wd * .5f - .3f)), T(u, .38f, v * (Wd * .5f - .02f)), .38f, 16);
            // kolizje: podwozie, kabina, ściany (wnętrze wolne — tam stoi obsługa)
            KolO("kolizja: food truck podwozie", T(0, .4f, 0), f, new Vector3(L, .8f, Wd));
            KolO("kolizja: food truck kabina", T(L * .5f - .75f, 1.7f, 0), f, new Vector3(1.5f, 1.8f, Wd));
            KolO("kolizja: food truck tył", T(sb, 1.65f, -Wd * .5f + .03f), f, new Vector3(Lb, 1.7f, .1f));
            KolO("kolizja: food truck lada", T(sb, 1.1f, Wd * .5f - .1f), f, new Vector3(Lb, .65f, .3f));
            KolO("kolizja: food truck koniec", T(-L * .5f + .03f, 1.65f, 0), f, new Vector3(.1f, 1.7f, Wd));
            string id = "wyspa_food_" + nTr;
            BraxMiastoA.Akcja(root, id, "ZAMÓW", tekst + ": Proszę bardzo — na gali w ARENIE głodny nie będziesz.", T(sb, 0f, Wd * .5f + 1.1f), 1.6f);
            Miejsca[id] = T(sb, 0f, Wd * .5f + 1.1f);
            Czlowiek("Food truck " + tekst, "obsługa", "stoi", T(sb, .8f, -.15f), f, KlLada);
            Czlowiek("Food truck " + tekst, "klient", "stoi", T(sb + .6f, 0f, Wd * .5f + 1.0f), -f, KlStoi);
        }
        BraxMiastoA.Log("WYSPA plac wejściowy areny: pylon, telebimów " + nT + ", brama z bramkami, flag " + nF + ", food trucków " + nTr);
    }

    // ---------------- dwa ringi plenerowe na mniejsze walki (nie nasze) ----------------
    static void Ringi()
    {
        var plotno = BraxMiastoA.Mat("Wyspa: ring plenerowy (płótno)", new Color(.85f, .2f, .16f), .8f);
        var fartuch = BraxMiastoA.Mat("Wyspa: ring fartuch", new Color(.08f, .08f, .09f), .8f);
        var lina = BraxMiastoA.Mat("Wyspa: liny ringu", new Color(.95f, .95f, .93f), .7f);
        int n = 0;
        foreach (var off in new[] { new Vector2(-8f, 30f), new Vector2(-8f, -30f) })
        {
            var c = new Vector3(CX + Plac.x + off.x, W + PlacH, CZ + Plac.y + off.y); float s = 5.5f * K * .5f, hp = .3f;
            BraxUliceKit.S(fartuch).Box(new Vector3(c.x - s, c.y, c.z - s), new Vector3(c.x + s, c.y + hp - .02f, c.z + s), 4);
            BraxUliceKit.S(plotno).Box(new Vector3(c.x - s - .1f, c.y + hp - .02f, c.z - s - .1f), new Vector3(c.x + s + .1f, c.y + hp, c.z + s + .1f), 4);
            BraxMiastoA.Kol(kol, "ring plenerowy", new Vector3(c.x - s, c.y, c.z - s), new Vector3(c.x + s, c.y + hp, c.z + s));
            for (int i = 0; i < 4; i++)
            {
                var a = new Vector3(c.x + (i == 0 || i == 3 ? -s : s), c.y + hp, c.z + (i < 2 ? -s : s)); var b = new Vector3(c.x + ((i + 1) % 4 == 0 || (i + 1) % 4 == 3 ? -s : s), c.y + hp, c.z + ((i + 1) % 4 < 2 ? -s : s));
                BraxUliceKit.S(fartuch).Rura(a, a + Vector3.up * 1.5f * K, .07f, 8);
                foreach (float yy in new[] { .45f, .8f, 1.15f }) BraxUliceKit.S(lina).Rura(a + Vector3.up * yy * K, b + Vector3.up * yy * K, .025f, 6);
                var kf = new GameObject("kolizja: liny ringu"); kf.transform.SetParent(kol, false); kf.transform.position = (a + b) * .5f + Vector3.up * .75f * K; kf.transform.rotation = Quaternion.LookRotation(Vector3.Cross(b - a, Vector3.up), Vector3.up);
                kf.AddComponent<BoxCollider>().size = new Vector3(Vector3.Distance(a, b), 1.2f * K, .1f);
            }
            n++;
            BraxMiastoA.Akcja(root, "wyspa_ring_" + n, "OBEJRZYJ WALKĘ", "Ring plenerowy: dziś walki amatorskie — główna gala w ARENIE BRAXHORN.", new Vector3(c.x, c.y, c.z - s - 1.5f), 2.2f);
        }
    }

    // ---------------- punkt widokowy na szczycie ----------------
    static void PunktWidokowy()
    {
        Vector3 best = Vector3.zero; float hb = -99f;
        for (float x = -90f; x <= 90f; x += 3f) for (float zz = -90f; zz <= 90f; zz += 3f) { float h = H(x, zz); if (h > hb && Vector2.Distance(new Vector2(x, zz), Plac) > PlacR + 15f) { hb = h; best = new Vector3(x, h, zz); } }
        var c = new Vector3(CX + best.x, W + hb, CZ + best.z);
        var drewno = BraxMiastoA.Tex("Wyspa: bar drewno", 1.2f, new Color(.8f, .65f, .48f), new Color(.5f, .38f, .26f), .7f, "wood_planks_grey", "wooden_planks");
        float r = 3.2f, y = c.y + .4f;
        for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; var p = c + new Vector3(Mathf.Cos(a) * (r - .2f), 0, Mathf.Sin(a) * (r - .2f)); BraxUliceKit.S(drewno).Rura(p - Vector3.up * 1.5f, new Vector3(p.x, y, p.z), .12f, 8); }
        var dek = new UlSiatka(); for (int i = 0; i < 12; i++) { float a0 = i * 30f * Mathf.Deg2Rad, a1 = (i + 1) * 30f * Mathf.Deg2Rad; dek.Tri(new Vector3(c.x, y, c.z), c + new Vector3(Mathf.Cos(a1) * r, y - c.y, Mathf.Sin(a1) * r), c + new Vector3(Mathf.Cos(a0) * r, y - c.y, Mathf.Sin(a0) * r), Vector3.up); }
        BraxMiastoA.Obiekt(root, "Punkt widokowy: pokład", dek, drewno, true);
        var dc = new GameObject("kolizja: punkt widokowy"); dc.transform.SetParent(kol, false); dc.transform.position = new Vector3(c.x, y - .15f, c.z); var cc = dc.AddComponent<CapsuleCollider>(); cc.direction = 1; cc.radius = r; cc.height = .3f;
        for (int i = 0; i < 12; i++)
        {
            if (i == 9) continue;   // wejście
            float a0 = i * 30f * Mathf.Deg2Rad, a1 = (i + 1) * 30f * Mathf.Deg2Rad;
            var p0 = c + new Vector3(Mathf.Cos(a0) * r, y - c.y + 1.0f * K, Mathf.Sin(a0) * r); var p1 = c + new Vector3(Mathf.Cos(a1) * r, y - c.y + 1.0f * K, Mathf.Sin(a1) * r);
            BraxUliceKit.S(drewno).Rura(p0, p1, .04f, 6); BraxUliceKit.S(drewno).Rura(p0 - Vector3.up * 1.0f * K, p0, .045f, 6);
            var kf = new GameObject("kolizja: barierka widokowa"); kf.transform.SetParent(kol, false); kf.transform.position = (p0 + p1) * .5f - Vector3.up * .5f * K; kf.transform.rotation = Quaternion.LookRotation(Vector3.Cross(p1 - p0, Vector3.up), Vector3.up);
            kf.AddComponent<BoxCollider>().size = new Vector3(Vector3.Distance(p0, p1), 1.0f * K, .1f);
        }
        Miejsca["wyspa_punkt_widokowy"] = new Vector3(c.x, y, c.z);
        BraxMiastoA.Akcja(root, "wyspa_punkt_widokowy", "PODZIWIAJ WIDOK", "Punkt widokowy: miasto, port i ARENA BRAXHORN jak na dłoni.", new Vector3(c.x, y, c.z), 2.5f);
    }

    // ---------------- palmy z kodu (3 odmiany, pień z pierścieniami, liście pierzaste, LOD) ----------------
    static int Palmy()
    {
        var pien = BraxMiastoA.Mat("Wyspa: pień palmy (kolory pierścieni)", Color.white, .9f);
        var lisc = BraxMiastoA.Mat("Wyspa: liście palmy (dwustronne)", Color.white, .65f);
        lisc.SetFloat("_Cull", 0f); EditorUtility.SetDirty(lisc);
        var odmiany = new List<(Mesh p, Mesh l, Mesh lod)>();
        for (int v = 0; v < 3; v++) odmiany.Add(Palma(v));
        var zr = new GameObject("Wyspa: palmy").transform; zr.SetParent(root, false);
        var rng = new System.Random(4100); int n = 0;
        for (float x = -110f; x <= 110f; x += 7f)
            for (float zz = -110f; zz <= 110f; zz += 7f)
            {
                float px = x + (float)(rng.NextDouble() - .5) * 4.5f, pz = zz + (float)(rng.NextDouble() - .5) * 4.5f, h = H(px, pz);
                if (h < .8f || h > 16f) continue;
                if (Vector2.Distance(new Vector2(px, pz), Plac) < PlacR + 3f) continue;
                if (Mathf.Abs(pz - Plac.y) < 4f && px < Plac.x) continue;                 // deptak
                if (Zajete(CX + px, CZ + pz, 1.0f)) continue;                               // leżaki, parasole, ratownicy, prysznice
                float gest = h < 6f ? .3f : .12f;   // 2.11 recenzja: budżet telefonu (było .5/.2 → ~1000 rendererów palm) if (rng.NextDouble() > gest) continue;
                float hx = H(px + 1, pz) - H(px - 1, pz), hz = H(px, pz + 1) - H(px, pz - 1); if (Mathf.Sqrt(hx * hx + hz * hz) * .5f > .6f) continue;
                var o = odmiany[rng.Next(odmiany.Count)];
                var g = new GameObject("Palma"); g.transform.SetParent(zr, false);
                g.transform.position = new Vector3(CX + px, W + h - .15f, CZ + pz); g.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                float sk = .85f + (float)rng.NextDouble() * .35f; g.transform.localScale = Vector3.one * sk;
                var l0p = new GameObject("pień"); l0p.transform.SetParent(g.transform, false); l0p.AddComponent<MeshFilter>().sharedMesh = o.p; var rp = l0p.AddComponent<MeshRenderer>(); rp.sharedMaterial = pien;
                var l0l = new GameObject("liście"); l0l.transform.SetParent(g.transform, false); l0l.AddComponent<MeshFilter>().sharedMesh = o.l; var rl = l0l.AddComponent<MeshRenderer>(); rl.sharedMaterial = lisc; rl.shadowCastingMode = ShadowCastingMode.Off;
                var l1 = new GameObject("z daleka"); l1.transform.SetParent(g.transform, false); l1.AddComponent<MeshFilter>().sharedMesh = o.lod; var r1 = l1.AddComponent<MeshRenderer>(); r1.sharedMaterial = lisc; r1.shadowCastingMode = ShadowCastingMode.Off;
                var lg = g.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(.08f, new Renderer[] { rp, rl }), new LOD(.012f, new Renderer[] { rp, r1 }) }); lg.RecalculateBounds();
                var c = g.AddComponent<CapsuleCollider>(); c.center = Vector3.up * 2f; c.radius = .25f; c.height = 4f;
                n++;
            }
        pien.enableInstancing = true; lisc.enableInstancing = true;
        return n;
    }

    static (Mesh, Mesh, Mesh) Palma(int v)
    {
        var rng = new System.Random(77 + v * 31);
        float H0 = 8.5f + v * 1.6f, lean = .9f + v * .5f;
        var p = new UlSiatka(); var l = new UlSiatka(); var lo = new UlSiatka();
        Vector3 Os(float t) => new Vector3(lean * t * t, H0 * t, lean * .3f * Mathf.Sin(t * 3f));
        int seg = 14;
        for (int i = 0; i < seg; i++)
        {
            float t0 = i / (float)seg, t1 = (i + 1) / (float)seg;
            p.kolor = i % 2 == 0 ? BraxUliceKit.C(.42f, .34f, .25f) : BraxUliceKit.C(.33f, .26f, .19f);
            p.Rura(Os(t0), Os(t1), Mathf.Lerp(.24f, .15f, t0), 8, i == 0);
        }
        p.kolor = BraxUliceKit.C(.3f, .22f, .12f);
        var kor = Os(1f);
        for (int i = 0; i < 4; i++) { float a = i * 1.6f; p.Walec(kor + new Vector3(Mathf.Cos(a) * .2f, -.45f, Mathf.Sin(a) * .2f), .11f, .2f, 8); }
        int fr = 11;
        for (int f = 0; f < fr; f++)
        {
            float az = f * 360f / fr + (float)rng.NextDouble() * 12f, up = 25f - (float)rng.NextDouble() * 40f; float L = 3.6f + (float)rng.NextDouble() * 1.1f;
            var dir = Quaternion.Euler(0, az, 0) * Vector3.forward; var side = Vector3.Cross(Vector3.up, dir);
            Vector3 R(float t) { float a = (up - t * 85f) * Mathf.Deg2Rad; return kor + dir * (Mathf.Cos(a) * L * t * 1.05f) + Vector3.up * (Mathf.Sin(a) * L * t * .9f); }
            l.kolor = BraxUliceKit.C(.32f, .42f, .15f); l.Rura(R(0), R(.5f), .03f, 4, false); l.Rura(R(.5f), R(1f), .02f, 4, false);
            int nl = 12;
            for (int i = 1; i < nl; i++)
            {
                float t = i / (float)nl, len = Mathf.Sin(t * Mathf.PI) * .9f + .15f;
                var b = R(t); var tg = (R(Mathf.Min(1, t + .05f)) - R(Mathf.Max(0, t - .05f))).normalized;
                var g0 = BraxUliceKit.C(.18f + .08f * (float)rng.NextDouble(), .36f + .1f * (float)rng.NextDouble(), .1f);
                l.kolor = g0;
                foreach (float sg in new[] { -1f, 1f })
                {
                    var tip = b + side * sg * len + Vector3.down * len * .45f + tg * len * .25f;
                    var w = tg * .07f;
                    l.Quad(b - w, b + w, tip + w * .3f, tip - w * .3f, Vector3.up);
                }
            }
            lo.kolor = BraxUliceKit.C(.24f, .38f, .12f);
            var a0 = R(0); var a1 = R(1f);
            lo.Quad(a0 - side * .2f, a0 + side * .2f, a1 + side * .9f, a1 - side * .9f, Vector3.up);
        }
        Mesh Zapisz(UlSiatka s, string n) { var m = s.Zbuduj("A: palma " + v + " " + n); BraxZapisAssetu.Utworz(m, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/palma_" + v + "_" + n + ".asset")); return m; }
        return (Zapisz(p, "pien"), Zapisz(l, "liscie"), Zapisz(lo, "lod"));
    }

    // ---------------- krzewy i skały (Poly Haven CC0) ----------------
    static int Krzewy()
    {
        var krz = new List<GameObject>(); foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03" }) { var mm = BraxRegion.Model(id); if (mm) krz.Add(mm); }
        var sk = new List<GameObject>(); foreach (var id in new[] { "coast_rocks_01", "coast_rocks_03", "boulder_01", "namaqualand_boulder_02" }) { var mm = BraxRegion.Model(id); if (mm) sk.Add(mm); }
        var zr = new GameObject("Wyspa: krzewy i skały").transform; zr.SetParent(root, false);
        var rng = new System.Random(91); int n = 0;
        for (int i = 0; i < 260 && n < 70; i++)
        {
            float x = (float)(rng.NextDouble() * 200 - 100), zz = (float)(rng.NextDouble() * 200 - 100), h = H(x, zz);
            if (Vector2.Distance(new Vector2(x, zz), Plac) < PlacR + 4f || (Mathf.Abs(zz - Plac.y) < 4f && x < Plac.x)) continue;
            if (Zajete(CX + x, CZ + zz, 1.6f)) continue;
            bool skala = h > 10f && rng.NextDouble() < .4 || (h > -.3f && h < .6f && rng.NextDouble() < .15);
            var list = skala ? sk : krz; if (list.Count == 0 || h < -.3f || h > 24f) continue;
            if (!skala && h < 3f) continue;
            if (BraxRegion.Put(zr, list[rng.Next(list.Count)], skala ? "Wyspa: skała" : "Wyspa: krzew", new Vector3(CX + x, W + h - (skala ? .3f : .05f), CZ + zz), (float)rng.NextDouble() * 360f, .8f + (float)rng.NextDouble() * .6f, skala, skala ? 2.5f : 1.6f * K)) n++;
        }
        foreach (var rr in zr.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;   // 2.11 recenzja: krzewy i skały bez cieni (telefon)
        return n;
    }

    // ---------------- sylwetka z daleka (scena główna) ----------------
    static void Sylwetka(Transform cityRoot)
    {
        var sr = BraxRegion.Root(cityRoot, "CLAUDE A: wyspa z daleka (sylwetka)");
        const float step = 8f, ext = 140f; int n = Mathf.RoundToInt(ext * 2 / step) + 1;
        var s = new UlSiatka(); var idx = new int[n, n];
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
            {
                float x = -ext + i * step, zz = -ext + j * step, h = H(x, zz);
                float sand = Mathf.Clamp01((7f - h) / 5f), rock = Mathf.Clamp01((h - 24f) / 8f);
                s.kolor = h < 3f ? BraxUliceKit.C(.84f, .76f, .6f) : rock > .5f ? BraxUliceKit.C(.5f, .48f, .44f) : BraxUliceKit.C(.22f, .38f, .14f);
                idx[i, j] = s.Wierzcholki; s.v.Add(new Vector3(CX + x * .985f, W + h - .8f, CZ + zz * .985f)); s.n.Add(Vector3.up); s.t.Add(new Vector4(1, 0, 0, 1)); s.uv.Add(new Vector2(x, zz)); s.c.Add(s.kolor);
            }
        for (int i = 0; i < n - 1; i++) for (int j = 0; j < n - 1; j++) { s.i.AddRange(new[] { idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i, j], idx[i + 1, j + 1], idx[i + 1, j] }); }
        var mesh = s.Zbuduj("A: wyspa z daleka"); mesh.RecalculateNormals();
        BraxZapisAssetu.Utworz(mesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/sylwetka.asset"));
        var g = new GameObject("Wyspa z daleka: teren"); g.transform.SetParent(sr, false); g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = BraxMiastoA.Mat("Wyspa z daleka (kolory z wierzchołków)", Color.white, .9f); r.shadowCastingMode = ShadowCastingMode.Off;
        var a = new UlSiatka(); var c = AC; a.kolor = BraxUliceKit.C(.75f, .78f, .8f);
        for (int i = 0; i < 8; i++) { float a0 = i * Mathf.PI / 4f, a1 = (i + 1) * Mathf.PI / 4f; var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (ArenaR - 1f); var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (ArenaR - 1f); a.Quad(p0, p1, p1 + Vector3.up * 12.5f, p0 + Vector3.up * 12.5f, (p0 + p1 - 2 * c).normalized); a.kolor = BraxUliceKit.C(.93f, .93f, .91f); a.Tri(c + Vector3.up * 16.5f, p1 + Vector3.up * 12.5f, p0 + Vector3.up * 12.5f, Vector3.up); a.kolor = BraxUliceKit.C(.75f, .78f, .8f); }   // dach membranowy (stożek)
        var ga = BraxMiastoA.Obiekt(sr, "Wyspa z daleka: arena", a, BraxMiastoA.Mat("Wyspa z daleka (kolory z wierzchołków)", Color.white, .9f));
        var ukr = sr.gameObject.AddComponent<BraxWyspaDaleko>(); ukr.srodek = new Vector3(CX, 0, CZ); ukr.blizej = 230f; ukr.renderery = new Renderer[] { r, ga.GetComponent<Renderer>() };
    }

    static readonly List<Vector3> deptak = new List<Vector3>();
    // spacerowicze na deptaku (przystań → plac → wejście do areny) i kibice czekający na placu
    static void Spacerowicze()
    {
        if (deptak.Count < 2) return;
        var trasa = new List<Vector3>();
        if (Miejsca.TryGetValue("wyspa_przystan", out var pr)) trasa.Add(pr);
        for (int i = 0; i < deptak.Count; i += 6) trasa.Add(deptak[i]);
        trasa.Add(deptak[deptak.Count - 1]);
        trasa.Add(new Vector3(CX + Plac.x - PlacR + 4f, W + PlacH, CZ + Plac.y));
        if (Miejsca.TryGetValue("arena_wejscie", out var we)) trasa.Add(we);
        var t = trasa.ToArray(); var rnd = new System.Random(77);
        for (int k = 0; k < 8; k++)
        {
            int s = rnd.Next(t.Length - 1); var p = Vector3.Lerp(t[s], t[s + 1], (float)rnd.NextDouble());
            Czlowiek("Deptak wyspy", "spacerowicz", "spacer", p, k % 2 == 0 ? t[s + 1] - t[s] : t[s] - t[s + 1], KlSpacer, t);
        }
        var pl = new Vector3(CX + Plac.x - 30f, W + PlacH, CZ + Plac.y);     // plac przed wejściem (ściana areny x ≈ Plac.x − 18)
        foreach (var o in new[] { new Vector2(-2f, 6f), new Vector2(-1f, 7f), new Vector2(3f, -6.5f), new Vector2(-5f, -5f), new Vector2(-4.2f, -5.8f) })
            Czlowiek("Plac przed ARENĄ", "kibic", "stoi", pl + new Vector3(o.x, 0, o.y), new Vector3(1f, 0, -o.y * .1f), KlStoi);
    }

    static void Punkty()
    {
        var world = Object.FindAnyObjectByType<BraxCityWorld>(); if (!world) return;
        var lista = world.points.Where(p => p != null && !Miejsca.ContainsKey(p.id)).ToList();
        foreach (var kv in Miejsca) lista.Add(new BraxCityPoint { id = kv.Key, label = kv.Key.Replace('_', ' '), position = kv.Value });
        world.points = lista.ToArray(); EditorUtility.SetDirty(world);
    }
}

internal static class BraxWektory { public static Vector3 WithY0(this Vector3 v) => new Vector3(v.x, 0f, v.z); }

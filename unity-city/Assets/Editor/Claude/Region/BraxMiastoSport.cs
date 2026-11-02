using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BRAXHORN SPORT — SKLEP SPORTOWY (wykonawca A, 2.11). Właściciel z38: „SZATNIA · SALA WALK” z ringiem — „ARENA WALK WYLATUJE
// z miasta, ona tu nie ma być” (główna sala walk = ARENA BRAXHORN na wyspie). Zamiast pawilonu z ringiem (krok
// BraxRegionPavilion WYŁĄCZONY w BraxRegion — ring, liny, narożniki, szatnia, szyld „SZATNIA · SALA WALK” nie powstają)
// stoi w tym samym miejscu przy boiskach sklep sportowy marki: przeszklony front z drzwiami automatycznymi (bez progu),
// szyld z liter przestrzennych i logo, daszek, okładzina z cokołem, attyka, rynny, klimatyzator i wywietrzniki na dachu;
// w środku: wieszaki z ubraniami (koszulki, bluzy, spodenki), ściana butów, ściana sprzętu (rękawice bokserskie, ochraniacze,
// skakanki), stojak z hantlami, kosz z piłkami, maty, PRZYMIERZALNIE z zasłonami, lustrem i ławką, lada z kasą, punkty akcji.
// Wszystko × k, budowane w układzie lokalnym budynku (front = lokalne −z, w stronę boisk).
internal static class BraxMiastoSport
{
    const float L = 24f, W = 11f, H = 4.0f, T = .3f;
    const float SX1 = 5.7f, PX0 = 6.0f, DXc = -3f, WD0 = 8.25f, WD1 = 9.45f;   // sklep x −11.7…5.7 | zaplecze graczy x 6…11.7 (szatnia, WC, prysznice)
    static float K => BraxMiastoA.K;
    static Transform root, kol;

    static void KolL(string n, Vector3 a, Vector3 b)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b);
        var g = new GameObject("kolizja: " + n); g.transform.SetParent(kol, false); g.transform.localPosition = (lo + hi) * .5f;
        g.AddComponent<BoxCollider>().size = hi - lo;
    }

    public static void Apply(Transform cityRoot)
    {
        // 1) miejsce: wolne, płaskie, przy boiskach (jak dawny pawilon), front do boisk
        var courts = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CLAUDE boiska na podwórku");
        Bounds cb = new Bounds(new Vector3(24f, .15f, 80f), new Vector3(52f, 1f, 26f));
        if (courts) { var rs = courts.GetComponentsInChildren<Renderer>(true); if (rs.Length > 0) { cb = rs[0].bounds; foreach (var r in rs.Skip(1)) cb.Encapsulate(r.bounds); } }
        float cy = cb.min.y;
        Physics.SyncTransforms();
        Vector3? spot = null; float bestD = float.MaxValue; Vector3 front = Vector3.back;
        for (float d = 4f; d <= 40f && !spot.HasValue; d += 2f)
        {
            var proby = new List<(Vector3 p, Vector3 doBoisk)>();
            foreach (float sl in new[] { 0f, -10f, 10f, -20f, 20f })
            {
                proby.Add((new Vector3(cb.center.x + sl, 0, cb.max.z + d + W * .5f), Vector3.back));
                proby.Add((new Vector3(cb.center.x + sl, 0, cb.min.z - d - W * .5f), Vector3.forward));
            }
            proby.Add((new Vector3(cb.max.x + d + W * .5f, 0, cb.center.z), Vector3.left));
            proby.Add((new Vector3(cb.min.x - d - W * .5f, 0, cb.center.z), Vector3.right));
            foreach (var (p, doB) in proby)
            {
                if (p.z > 14f && p.z < 42f) continue;   // nie na ulicy głównej
                bool wzdluzX = Mathf.Abs(doB.z) > .5f;
                var size = wzdluzX ? new Vector3(L + 3f, H + 1.2f, W + 3f) : new Vector3(W + 3f, H + 1.2f, L + 3f);
                if (!BraxRegion.Ground(p.x, p.z, cy - 1.5f, cy + 1.5f, out var g)) continue;
                var center = new Vector3(p.x, g.point.y + .35f + size.y * .5f, p.z);
                if (!BraxRegion.Free(center, size)) continue;
                bool flat = true;
                for (float u = -.5f; u <= .5f && flat; u += .25f)
                    for (float v = -.5f; v <= .5f && flat; v += .25f)
                        if (!BraxRegion.Ground(p.x + u * size.x, p.z + v * size.z, cy - 1.5f, cy + 1.5f, out var h) || Mathf.Abs(h.point.y - g.point.y) > .3f) flat = false;
                if (!flat) continue;
                float dist = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(cb.center.x, cb.center.z));
                if (dist < bestD) { bestD = dist; spot = new Vector3(p.x, g.point.y, p.z); front = doB; }
            }
        }
        if (!spot.HasValue) { BraxMiastoA.Log("BRAXHORN SPORT: brak wolnego płaskiego miejsca przy boiskach — sklep NIE postawiony"); return; }
        BraxUliceKit.Start();
        root = BraxRegion.Root(cityRoot, "CLAUDE A: BRAXHORN SPORT (sklep sportowy)");
        root.position = spot.Value + Vector3.up * .02f;
        root.rotation = Quaternion.LookRotation(-front, Vector3.up);   // lokalne −z = front (do boisk)
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        BraxUliceKit.UstawGrupe("Sport bryła i fasada");
        Bryla();
        BraxUliceKit.UstawGrupe("Sport wnętrze", 50f);
        Wnetrze();
        int siatek = BraxUliceKit.Zapisz(root, "sport");
        BraxRegion.Rep.pavilion = true; BraxRegion.Rep.pavilionAt = root.position; BraxRegion.Rep.pavilionDoor = true;
        BraxMiastoA.Log("BRAXHORN SPORT: sklep sportowy " + L + "×" + W + " m w " + root.position.ToString("F1") + ", front do boisk " + front + ", siatek " + siatek + " (ring i sala walk — usunięte z miasta, krok pawilonu wyłączony)");
    }

    static void Bryla()
    {
        var cokol = BraxMiastoA.Tex("Sport: cokół granit", 1.2f, null, new Color(.3f, .3f, .31f), .6f, "granite_tile", "granite_tile_02");
        var okl = BraxMiastoA.Tex("Sport: okładzina panelowa", 1.6f, new Color(.85f, .87f, .9f), new Color(.55f, .57f, .6f), .6f, "exterior_wall_cladding_02", "exterior_wall_cladding");
        var posadzka = BraxMiastoA.Tex("Sport: posadzka gumowa sportowa", 1.0f, new Color(.55f, .56f, .58f), new Color(.25f, .26f, .28f), .8f, "rubber_tiles", "concrete_floor_02");
        var sciana = BraxMiastoA.Tex("Sport: tynk biały", 2.4f, new Color(.95f, .95f, .94f), new Color(.9f, .9f, .89f), .9f, "white_plaster_02");
        var sufit = BraxMiastoA.Mat("Sport: sufit (panele białe)", new Color(.92f, .92f, .92f), .85f);
        var alu = BraxMiastoA.Mat("Sport: aluminium witryn czarne", new Color(.06f, .065f, .07f), .4f, .6f);
        var blacha = BraxMiastoA.Mat("Sport: blacha attyki i rynny", new Color(.42f, .44f, .46f), .45f, .7f);
        var dach = BraxMiastoA.Mat("Sport: papa dachowa", new Color(.18f, .18f, .19f), .95f);
        var zielone = BraxMiastoA.Swieci("Sport: litery zielone (marka)", new Color(.45f, 1f, .2f), new Color(.9f, 2.4f, .35f));
        var biale = BraxMiastoA.Swieci("Sport: litery białe podświetlane", Color.white, new Color(1.7f, 1.7f, 1.7f));
        var boki = BraxMiastoA.Mat("Sport: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        float x0 = -L * .5f, x1 = L * .5f, z0 = -W * .5f, z1 = W * .5f, F = .15f;
        // płyta posadzki (podest 15 cm z łagodnym podjazdem przed drzwiami — wejście idąc, bez progu)
        BraxUliceKit.S(cokol).Box(new Vector3(x0, 0, z0), new Vector3(x1, F, z1), 4);
        KolL("płyta", new Vector3(x0, -.3f, z0), new Vector3(x1, F, z1));
        // podjazd 1:10 (1.5 m) przed drzwiami
        var pj = BraxUliceKit.S(cokol);
        foreach (float xc in new[] { DXc, (WD0 + WD1) * .5f })
        {
            float a = xc - 1.6f, b = xc + 1.6f;
            pj.Quad(new Vector3(a, 0f, z0 - 1.5f), new Vector3(b, 0f, z0 - 1.5f), new Vector3(b, F, z0), new Vector3(a, F, z0), new Vector3(0, .995f, -.1f));
            pj.Tri(new Vector3(a, 0f, z0 - 1.5f), new Vector3(a, F, z0), new Vector3(a, 0f, z0), Vector3.left);
            pj.Tri(new Vector3(b, 0f, z0 - 1.5f), new Vector3(b, 0f, z0), new Vector3(b, F, z0), Vector3.right);
            var rampa = new GameObject("kolizja: podjazd"); rampa.transform.SetParent(kol, false);
            rampa.transform.localPosition = new Vector3(xc, F * .5f - .05f, z0 - .75f); rampa.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(F, 1.5f) * Mathf.Rad2Deg, 0, 0);
            rampa.AddComponent<BoxCollider>().size = new Vector3(3.2f, .1f, 1.52f);
        }
        // wnętrze (lica od środka) z otworem na witrynę frontową
        // sklep (lica od środka; ściana frontowa = witryna) i zaplecze dla grających (szatnia, WC, prysznice) za ścianą działową
        BraxMiastoA.Pokoj(posadzka, sciana, sufit, x0 + T, SX1, z0 + T, z1 - T, F, H, new[] { new[] { Rect.MinMaxRect(.1f, F, SX1 - (x0 + T) - .1f, H - .35f) }, null, null, null });
        var plytki = BraxMiastoA.Tex("Sport: płytki szatni", 1.0f, new Color(.85f, .86f, .87f), new Color(.75f, .76f, .77f), .5f, "floor_tiles_06", "square_tiles");
        BraxMiastoA.Pokoj(posadzka, plytki, sufit, PX0, x1 - T, z0 + T, z1 - T, F, H, new[] { new[] { Rect.MinMaxRect((x1 - T) - WD1, F, (x1 - T) - WD0, F + 2.6f) }, null, null, null });
        KolL("ściana działowa", new Vector3(SX1, F, z0 + T), new Vector3(PX0, H, z1 - T));
        // ściany zewnętrzne: okładzina (boki i tył), cokół 0.6 m
        var o = BraxUliceKit.S(okl); var c = BraxUliceKit.S(cokol);
        void ScianaZew(Vector3 A, Vector3 R, Vector3 N, float dl)
        {
            BraxMiastoA.Sciana(o, A, R, N, dl, .6f, H + .55f, null);
            c.OBox(A + R * dl * .5f + Vector3.up * (F + .45f * .5f + .15f) + N * .02f, R, Vector3.up, N, dl + .04f, .6f + .15f - F, .04f, true);
        }
        ScianaZew(new Vector3(x1, 0, z1), Vector3.left, Vector3.forward, L);
        ScianaZew(new Vector3(x0, 0, z1), Vector3.back, Vector3.left, W);
        ScianaZew(new Vector3(x1, 0, z0), Vector3.forward, Vector3.right, W);
        // front: pas nad witryną i pilastry narożne z okładziny
        o.Box(new Vector3(x0, H - .35f, z0 - .02f), new Vector3(x1, H + .55f, z0 + T), 32);
        o.Box(new Vector3(x0 - .0f, F, z0 - .02f), new Vector3(x0 + .4f, H - .35f, z0 + T), 32);
        // front zaplecza (szatnia graczy): pełna ściana z okładziny z drzwiami
        BraxMiastoA.Sciana(o, new Vector3(SX1 - .1f, 0, z0 - .02f), Vector3.right, Vector3.back, x1 - (SX1 - .1f), F, H - .35f, new[] { Rect.MinMaxRect(WD0 - (SX1 - .1f), F, WD1 - (SX1 - .1f), F + 2.6f) }, T + .02f, o);
        KolL("front zaplecza", new Vector3(SX1, F, z0), new Vector3(WD0, H, z0 + T)); KolL("front zaplecza", new Vector3(WD1, F, z0), new Vector3(x1, H, z0 + T));
        KolL("nadproże zaplecza", new Vector3(WD0, F + 2.6f, z0), new Vector3(WD1, H, z0 + T));
        BraxMiastoA.DrzwiPrzesuwne(root, "BRAXHORN SPORT — szatnia graczy", root.TransformPoint(new Vector3((WD0 + WD1) * .5f, F, z0 + .12f)), root.TransformDirection(Vector3.right), WD1 - WD0, 2.6f, BraxUliceKit.SzkloPrzezroczyste, BraxMiastoA.Mat("Sport: aluminium witryn czarne", new Color(.06f, .065f, .07f), .4f, .6f));
        BraxUliceKit.Napis3D("SZATNIA · WC", new Vector3((WD0 + WD1) * .5f, F + 2.8f, z0 - .02f), Vector3.back, .2f, .04f, BraxMiastoA.Swieci("Sport: litery białe podświetlane", Color.white, new Color(1.7f, 1.7f, 1.7f)), BraxMiastoA.Mat("Sport: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f));
        KolL("ściana tył", new Vector3(x0, F, z1 - T), new Vector3(x1, H, z1)); KolL("ściana zachód", new Vector3(x0, F, z0), new Vector3(x0 + T, H, z1));
        KolL("ściana wschód", new Vector3(x1 - T, F, z0), new Vector3(x1, H, z1)); KolL("dach", new Vector3(x0, H, z0), new Vector3(x1, H + .5f, z1));
        // WITRYNA: rama aluminiowa, słupki co 1.6 m, szkło przezroczyste, drzwi automatyczne w osi
        var al = BraxUliceKit.S(alu); float wz = z0 + .12f, wy0 = F + .45f, wy1 = H - .35f, wx0 = x0 + .4f, wx1 = SX1 - .1f, dw = 2.4f, d0 = DXc - dw * .5f, d1 = DXc + dw * .5f;
        c.Box(new Vector3(wx0, F, z0 + .02f), new Vector3(d0, wy0, wz + .05f), 4); c.Box(new Vector3(d1, F, z0 + .02f), new Vector3(wx1, wy0, wz + .05f), 4);
        al.Box(new Vector3(wx0, wy1 - .08f, wz - .05f), new Vector3(wx1, wy1, wz + .05f));
        foreach (var (a, b) in new[] { (wx0, d0), (d1, wx1) })
        {
            al.Box(new Vector3(a, wy0, wz - .05f), new Vector3(b, wy0 + .08f, wz + .05f));
            int n = Mathf.Max(1, Mathf.RoundToInt((b - a) / 1.6f));
            for (int i = 0; i <= n; i++) { float x = a + (b - a) * i / n; al.Box(new Vector3(x - .04f, wy0, wz - .05f), new Vector3(x + .04f, wy1, wz + .05f)); }
            BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Quad(new Vector3(a, wy0, wz), new Vector3(b, wy0, wz), new Vector3(b, wy1, wz), new Vector3(a, wy1, wz), Vector3.back);
            KolL("witryna", new Vector3(a, F, wz - .06f), new Vector3(b, wy1, wz + .06f));
        }
        al.Box(new Vector3(d0, F + 2.85f, wz - .05f), new Vector3(d1, wy1, wz + .05f));
        BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Quad(new Vector3(d0, F + 2.93f, wz), new Vector3(d1, F + 2.93f, wz), new Vector3(d1, wy1 - .08f, wz), new Vector3(d0, wy1 - .08f, wz), Vector3.back);
        var drzwi = BraxMiastoA.DrzwiPrzesuwne(root, "BRAXHORN SPORT — wejście", root.TransformPoint(new Vector3(DXc, F, wz)), root.TransformDirection(Vector3.right), dw, 2.85f, BraxUliceKit.SzkloPrzezroczyste, alu);
        // szyld z liter przestrzennych na pasie nad witryną + logo
        float zs = z0 - .02f;
        BraxUliceKit.Napis3D("BRAXHORN SPORT", new Vector3(DXc + .9f, H - .12f, zs), Vector3.back, .5f, .1f, biale, boki);
        // logo-kaseton: rama-pierścień (bez płyty za obrazem), obraz 6 cm przed okładziną
        BraxMiastoA.Obraz(root, "BRAXHORN SPORT logo", "Assets/CityRebuild/Brand/brand-badge.png", new Vector3(DXc - 5.6f, H - .25f, zs - .06f), Vector3.right, .78f, .78f, Vector3.back, .5f);
        var ra = BraxUliceKit.S(alu);
        ra.Box(new Vector3(DXc - 5.66f, H - .31f, zs - .1f), new Vector3(DXc - 5.6f, H + .59f, zs), 32); ra.Box(new Vector3(DXc - 4.82f, H - .31f, zs - .1f), new Vector3(DXc - 4.76f, H + .59f, zs), 32);
        ra.Box(new Vector3(DXc - 5.6f, H - .31f, zs - .1f), new Vector3(DXc - 4.82f, H - .25f, zs), 32); ra.Box(new Vector3(DXc - 5.6f, H + .53f, zs - .1f), new Vector3(DXc - 4.82f, H + .59f, zs), 32);
        BraxUliceKit.S(zielone).Box(new Vector3(x0 + .4f, H - .37f, zs - .04f), new Vector3(x1 - .4f, H - .33f, zs));
        // daszek nad wejściem (dół 3.0 m)
        var bl = BraxUliceKit.S(blacha);
        bl.Box(new Vector3(DXc - 2.2f, 3.0f, z0 - 1.4f), new Vector3(DXc + 2.2f, 3.12f, z0 - .02f), 0);
        BraxUliceKit.S(biale).Box(new Vector3(DXc - 2.18f, 2.97f, z0 - 1.36f), new Vector3(DXc + 2.18f, 3.0f, z0 - 1.3f));
        // dach: papa, attyka z obróbką, wywietrzniki, klimatyzator (jednostka zewnętrzna) — z ulicy widać tylko attykę
        BraxUliceKit.S(dach).Box(new Vector3(x0 + .25f, H + .18f, z0 + .25f), new Vector3(x1 - .25f, H + .2f, z1 - .25f), 4);
        bl.Box(new Vector3(x0 - .04f, H + .55f, z0 - .04f), new Vector3(x1 + .04f, H + .6f, z1 + .04f), 4);
        var kl = new Vector3(4f, H + .2f, 1.5f);
        bl.Box(kl, kl + new Vector3(1.1f, .8f, .45f)); BraxUliceKit.S(alu).Walec(kl + new Vector3(.55f, .4f, -.01f), .28f, .02f, 20);
        foreach (var p in new[] { new Vector3(-5f, H + .2f, 2f), new Vector3(-2f, H + .2f, 3f) }) { bl.Walec(p, .14f, .6f, 12); bl.Walec(p + Vector3.up * .6f, .2f, .05f, 12); }
        // rynna i rury spustowe w narożnikach od tyłu
        foreach (float x in new[] { x0 + .15f, x1 - .15f }) bl.Rura(new Vector3(x, H + .5f, z1 + .1f), new Vector3(x, .2f, z1 + .1f), .05f * K, 10);
        bl.Rura(new Vector3(x0 + .1f, H + .52f, z1 + .12f), new Vector3(x1 - .1f, H + .52f, z1 + .12f), .07f, 10);
    }

    static void Wnetrze()
    {
        float F = .15f, x0 = -L * .5f + T, x1 = SX1, z0 = -W * .5f + T, z1 = W * .5f - T;
        var chrom = BraxMiastoA.Mat("Sport: chrom wieszaków", new Color(.82f, .83f, .85f), .15f, 1f);
        var ubrania = BraxMiastoA.Mat("Sport: tekstylia (kolory z wierzchołków)", Color.white, .9f);
        var towar = BraxMiastoA.Mat("Sport: sprzęt (kolory z wierzchołków)", Color.white, .5f, .05f);
        var dab = BraxMiastoA.Mat("Sport: dąb jasny", new Color(.62f, .48f, .32f), .55f);
        var czern = BraxMiastoA.Mat("Sport: czarny lakier", new Color(.05f, .05f, .055f), .35f, .2f);
        var zaslona = BraxMiastoA.Mat("Sport: zasłony przymierzalni", new Color(.18f, .2f, .22f), .95f);
        var lustro = BraxMiastoA.Mat("Sport: lustro", new Color(.75f, .78f, .8f), .04f, 1f);
        var led = BraxMiastoA.Swieci("Sport: listwa LED biała", Color.white, new Color(1.5f, 1.5f, 1.45f));
        var zielone = BraxMiastoA.Swieci("Sport: litery zielone (marka)", new Color(.45f, 1f, .2f), new Color(.9f, 2.4f, .35f));
        var boki = BraxMiastoA.Mat("Sport: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        var rng = new System.Random(38);
        Color32[] kol = { BraxUliceKit.C(.05f, .05f, .06f), BraxUliceKit.C(.92f, .92f, .92f), BraxUliceKit.C(.25f, .7f, .15f), BraxUliceKit.C(.15f, .25f, .55f), BraxUliceKit.C(.6f, .1f, .1f), BraxUliceKit.C(.45f, .45f, .47f) };
        var ub = BraxUliceKit.S(ubrania); var tw = BraxUliceKit.S(towar); var ch = BraxUliceKit.S(chrom);
        // linie LED w suficie
        for (float x = x0 + 1.5f; x < x1 - 1f; x += 3f) BraxUliceKit.S(led).Box(new Vector3(x - .03f, H - .05f, z0 + 1f), new Vector3(x + .03f, H - .02f, z1 - 1f));
        // wieszaki okrągłe z koszulkami i bluzami (6 szt., środek sali)
        foreach (var c in new[] { new Vector3(-7.6f, F, -1.0f), new Vector3(-4.6f, F, -1.0f), new Vector3(-1.4f, F, -1.0f), new Vector3(-7.6f, F, 1.8f), new Vector3(-4.6f, F, 1.8f), new Vector3(-1.4f, F, 1.8f) })
        {
            float hr = 1.45f * K, rr = .5f * K;
            ch.Walec(c, .3f * K, .03f, 18); ch.Rura(c, c + Vector3.up * hr, .025f, 8); ch.Pierscien(c + Vector3.up * hr, rr - .015f, rr + .015f, 0, 360, 28);
            for (int i = 0; i < 18; i++)
            {
                float a = i * 20f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var t = new Vector3(-d.z, 0, d.x);
                ub.kolor = kol[rng.Next(kol.Length)];
                var p = c + d * rr + Vector3.up * (hr - .04f);
                ub.OBox(p + Vector3.down * .38f * K, t, Vector3.up, d, .5f * K, .72f * K, .03f);
            }
            var g = new GameObject("kolizja: wieszak okrągły"); g.transform.SetParent(BraxMiastoSportKol, false); g.transform.localPosition = c + Vector3.up * hr * .5f;
            var cc = g.AddComponent<CapsuleCollider>(); cc.radius = rr + .2f; cc.height = hr;
        }
        ub.kolor = BraxUliceKit.Bialy;
        // ŚCIANA BUTÓW (zachód): półki ukośne z parami butów
        BraxUliceKit.S(czern).Box(new Vector3(x0, F, z0 + 1.0f), new Vector3(x0 + .05f, F + 2.4f * K, z1 - 1.0f), 1);
        for (int p = 0; p < 6; p++)
        {
            float y = F + .35f + p * .45f;
            BraxUliceKit.S(dab).Box(new Vector3(x0 + .05f, y - .025f, z0 + 1.05f), new Vector3(x0 + .4f, y, z1 - 1.05f), 1);
            for (float z = z0 + 1.2f; z < z1 - 1.3f; z += .42f)
            {
                tw.kolor = kol[rng.Next(kol.Length)];
                foreach (float dz in new[] { 0f, .14f })
                {
                    tw.Box(new Vector3(x0 + .08f, y, z + dz), new Vector3(x0 + .37f * K, y + .06f, z + dz + .1f));
                    tw.Box(new Vector3(x0 + .08f, y + .06f, z + dz + .01f), new Vector3(x0 + .2f, y + .12f, z + dz + .09f));
                }
            }
        }
        BraxUliceKit.Napis3D("BUTY", new Vector3(x0, F + 3.2f, 0f), Vector3.right, .24f, .04f, zielone, boki);
        KolL("ściana butów", new Vector3(x0, F, z0 + 1f), new Vector3(x0 + .4f, F + 2.4f * K, z1 - 1f));
        // ŚCIANA SPRZĘTU (tył): rękawice bokserskie, ochraniacze, skakanki na panelu z hakami; stojak z hantlami; kosz z piłkami
        BraxUliceKit.S(czern).Box(new Vector3(-8.5f, F + .6f, z1 - .05f), new Vector3(-1.5f, F + 2.8f, z1), 32);
        for (float x = -8.2f; x < -1.7f; x += .45f)
            for (int r = 0; r < 3; r++)
            {
                tw.kolor = r == 0 ? kol[4] : r == 1 ? kol[0] : kol[3];
                var p = new Vector3(x, F + .9f + r * .65f, z1 - .2f);
                tw.Box(p + new Vector3(-.12f, -.16f, -.12f), p + new Vector3(.12f, .16f, .12f));
                tw.Box(p + new Vector3(-.09f, -.3f, -.09f), p + new Vector3(.09f, -.16f, .09f));
            }
        tw.kolor = BraxUliceKit.Bialy;
        BraxUliceKit.Napis3D("SPRZĘT", new Vector3(-4.85f, F + 3.05f, z1), Vector3.back, .22f, .04f, zielone, boki);
        KolL("ściana sprzętu", new Vector3(-8.5f, F, z1 - .35f), new Vector3(-1.5f, F + 2.8f, z1));
        // stojak z hantlami (bryły z kodu) — etap „assety” 2.11: gdy jest paczka siłowni, ZAMIAST niego ekspozycja sprzętu ze
        // Sketchfaba (atlas w tym miejscu, klatka z gryfem i bieżnia w witrynie, ławka) — BraxMiastoAssety.SprzetSklepu
        bool sprzetModele = false;
        try { sprzetModele = BraxMiastoAssety.SprzetSklepu(root, BraxMiastoSportKol, F, z1); } catch (System.Exception e) { Debug.LogError("BRAXHORN_MIASTO_ASSETY BRAXHORN SPORT sprzęt DO POPRAWKI: " + e); }
        if (!sprzetModele)
        {
            var st = new Vector3(-11.2f, F, z1 - 1.6f);
            BraxUliceKit.S(czern).Box(st + new Vector3(0, 0, -.25f), st + new Vector3(1.6f, .7f, .25f));
            for (int i = 0; i < 6; i++) { var p = st + new Vector3(.15f + i * .25f, .72f, 0); tw.kolor = kol[0]; tw.Rura(p + Vector3.back * .14f, p + Vector3.forward * .14f, .02f, 6); tw.Walec(p + Vector3.back * .16f + Vector3.down * .06f, .06f + i * .008f, .12f, 10); tw.Walec(p + Vector3.forward * .04f + Vector3.down * .06f, .06f + i * .008f, .12f, 10); }
            tw.kolor = BraxUliceKit.Bialy;
            KolL("stojak hantli", st + new Vector3(0, 0, -.3f), st + new Vector3(1.6f, .8f, .3f));
        }
        // kosz z piłkami
        var ko = new Vector3(.9f, F, -3.0f);
        ch.Pierscien(ko + Vector3.up * .8f, .5f, .53f, 0, 360, 24); for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; ch.Rura(ko + new Vector3(Mathf.Cos(a) * .52f, 0, Mathf.Sin(a) * .52f), ko + new Vector3(Mathf.Cos(a) * .52f, .8f, Mathf.Sin(a) * .52f), .015f, 6); }
        for (int i = 0; i < 9; i++) { var p = ko + new Vector3(((i % 3) - 1) * .28f, .45f + (i / 3) * .2f, ((i / 3) - 1) * .22f); tw.kolor = i % 2 == 0 ? BraxUliceKit.C(.85f, .45f, .15f) : BraxUliceKit.C(.95f, .95f, .95f); tw.Walec(p - Vector3.up * .12f * K, .12f * K, .24f * K, 12); }
        tw.kolor = BraxUliceKit.Bialy;
        KolL("kosz z piłkami", ko + new Vector3(-.55f, 0, -.55f), ko + new Vector3(.55f, .8f, .55f));
        // PRZYMIERZALNIE (wschód, 3 kabiny): ścianki, zasłony, lustro, ławeczka, haczyki
        float px0 = x1 - 1.6f * K;
        for (int i = 0; i < 3; i++)
        {
            float za = z0 + 1.2f + i * 1.6f * K, zb = za + 1.6f * K;
            BraxUliceKit.S(dab).Box(new Vector3(px0, F, zb - .04f), new Vector3(x1, F + 2.3f * K, zb));
            if (i == 0) BraxUliceKit.S(dab).Box(new Vector3(px0, F, za), new Vector3(x1, F + 2.3f * K, za + .04f));
            BraxUliceKit.S(zaslona).Box(new Vector3(px0 - .03f, F + .25f, za + .05f), new Vector3(px0, F + 2.15f * K, zb - .25f));
            ch.Rura(new Vector3(px0 - .015f, F + 2.18f * K, za + .04f), new Vector3(px0 - .015f, F + 2.18f * K, zb - .04f), .012f, 6);
            BraxUliceKit.S(lustro).Box(new Vector3(x1 - .03f, F + .3f, za + .3f), new Vector3(x1 - .01f, F + 2.0f * K, zb - .3f));
            BraxUliceKit.S(dab).Box(new Vector3(x1 - .4f, F + .45f * K - .04f, za + .2f), new Vector3(x1 - .05f, F + .45f * K, za + .7f));
            KolL("przymierzalnia ścianka", new Vector3(px0, F, zb - .04f), new Vector3(x1, F + 2.3f * K, zb));
            BraxMiastoA.Siedzisko(root, "ŁAWECZKA", root.TransformPoint(new Vector3(x1 - .22f, F + .45f * K, za + .45f)), root.TransformDirection(Vector3.left));
        }
        BraxUliceKit.Napis3D("PRZYMIERZALNIE", new Vector3(px0 - .05f, F + 2.55f * K, z0 + 1.2f + 2.4f * K), Vector3.left, .16f, .03f, zielone, boki);
        // LADA z kasą (przód wschód, przodem do wejścia)
        var lc = new Vector3(1.0f, F, 3.2f);
        BraxUliceKit.S(dab).Box(lc + new Vector3(-.35f, 0, -1.4f), lc + new Vector3(.35f, 1.05f * K - .04f, 1.4f), 4);
        BraxUliceKit.S(czern).Box(lc + new Vector3(-.4f, 1.05f * K - .04f, -1.45f), lc + new Vector3(.4f, 1.05f * K, 1.45f));
        BraxUliceKit.S(czern).Box(lc + new Vector3(-.1f, 1.05f * K, .4f), lc + new Vector3(.15f, 1.05f * K + .32f, .7f));
        BraxUliceKit.S(zielone).Box(lc + new Vector3(-.38f, .08f, -1.4f), lc + new Vector3(-.35f, .12f, 1.4f));
        KolL("lada", lc + new Vector3(-.4f, 0, -1.45f), lc + new Vector3(.4f, 1.05f * K, 1.45f));
        // maty do ćwiczeń w rolkach (stojak)
        for (int i = 0; i < 6; i++) { var p = new Vector3(-11.2f + i * .22f, F, -3.4f); tw.kolor = kol[2 + i % 4]; tw.Walec(p, .07f, .62f * K, 10); }
        tw.kolor = BraxUliceKit.Bialy;
        var w = root.TransformPoint(new Vector3(-3f, 3.4f, 0)); BraxRegion.Warm(root, "Sport światło", w, 1.3f, 10f);
        BraxRegion.Warm(root, "Sport światło", root.TransformPoint(new Vector3(-8.5f, 3.4f, 1f)), 1.1f, 9f);
        Szatnia();
        BraxMiastoA.Akcja(root, "sport_kasa", "KUP", "Sprzedawca: Rękawice, buty, odzież — wszystko z linii BRAXHORN.", root.TransformPoint(lc + new Vector3(-1.2f, 0, 0)), 1.7f);
        BraxMiastoA.Akcja(root, "sport_przymierzalnia", "PRZYMIERZ", "Przymierzalnia wolna — weź rzeczy z wieszaka.", root.TransformPoint(new Vector3(px0 - .9f, F, z0 + 2.5f)), 1.6f);
    }

    // ZAPLECZE DLA GRAJĄCYCH NA BOISKACH (właściciel 15:32: „toalety i szatnia dla grających, BEZ ringu”): szatnia z szafkami
    // i ławkami, ściana działowa z przejściem, 2 kabiny WC, umywalki z lustrem, 2 prysznice.
    static void Szatnia()
    {
        float F = .15f, x0 = PX0, x1 = L * .5f - T, z0 = -W * .5f + T, z1 = W * .5f - T, zP = 1.2f;
        var szafki = BraxMiastoA.Mat("Sport: szafki metalowe szare", new Color(.55f, .58f, .6f), .45f, .6f);
        var dab = BraxMiastoA.Mat("Sport: dąb jasny", new Color(.62f, .48f, .32f), .55f);
        var plytki = BraxMiastoA.Tex("Sport: płytki szatni", 1.0f, new Color(.85f, .86f, .87f), new Color(.75f, .76f, .77f), .5f, "floor_tiles_06", "square_tiles");
        var hpl = BraxMiastoA.Mat("Sport: płyty kabin HPL", new Color(.2f, .45f, .25f), .5f);
        var ceramika = BraxMiastoA.Mat("Sport: ceramika biała", new Color(.94f, .94f, .93f), .2f);
        var chrom = BraxMiastoA.Mat("Sport: chrom wieszaków", new Color(.82f, .83f, .85f), .15f, 1f);
        var lustro = BraxMiastoA.Mat("Sport: lustro", new Color(.75f, .78f, .8f), .04f, 1f);
        // szafki wzdłuż ściany wschodniej (×k), z drzwiczkami i szczelinami
        var sz = BraxUliceKit.S(szafki);
        for (float z = z0 + .3f; z < zP - .5f; z += .42f)
        {
            sz.Box(new Vector3(x1 - .5f, F, z), new Vector3(x1, F + 1.85f * K, z + .4f));
            BraxUliceKit.S(chrom).Box(new Vector3(x1 - .52f, F + 1.0f * K, z + .3f), new Vector3(x1 - .5f, F + 1.15f * K, z + .33f));
        }
        KolL("szafki", new Vector3(x1 - .5f, F, z0 + .3f), new Vector3(x1, F + 1.85f * K, zP - .5f));
        // ławki szatni (siedzisko 0.45 m × k) z wieszakami nad ławką przy ścianie działowej sklepu
        foreach (float zb in new[] { -3.2f, -1.0f })
        {
            var d = BraxUliceKit.S(dab); float y = F + .45f * K;
            d.Box(new Vector3(7.4f, y - .05f, zb - .2f), new Vector3(10.0f, y, zb + .2f));
            foreach (float x in new[] { 7.6f, 9.8f }) BraxUliceKit.S(chrom).Box(new Vector3(x - .03f, F, zb - .15f), new Vector3(x + .03f, y - .05f, zb + .15f));
            KolL("ławka szatni", new Vector3(7.4f, F, zb - .2f), new Vector3(10.0f, y, zb + .2f));
            BraxMiastoA.Siedzisko(root, "ŁAWKA", root.TransformPoint(new Vector3(8.7f, y, zb)), root.TransformDirection(Vector3.back));
        }
        BraxUliceKit.S(dab).Box(new Vector3(x0, F + .45f * K - .05f, z0 + .4f), new Vector3(x0 + .4f, F + .45f * K, zP - .6f));
        for (float z = z0 + .6f; z < zP - .7f; z += .5f) BraxUliceKit.S(chrom).Rura(new Vector3(x0 + .01f, F + 1.7f * K, z), new Vector3(x0 + .12f, F + 1.75f * K, z), .012f, 6);
        KolL("ławka przyścienna", new Vector3(x0, F, z0 + .4f), new Vector3(x0 + .4f, F + .45f * K, zP - .6f));
        // ściana działowa szatnia | WC i prysznice (przejście 1.2 m przy ścianie zachodniej)
        var pl = BraxUliceKit.S(plytki);
        pl.Box(new Vector3(x0 + 1.6f, F, zP), new Vector3(x1, H, zP + .2f));
        KolL("ściana działowa WC", new Vector3(x0 + 1.6f, F, zP), new Vector3(x1, H, zP + .2f));
        // 2 kabiny WC (1.2 × 2.0 m) przy ścianie północnej, wschód
        var h = BraxUliceKit.S(hpl);
        for (int i = 0; i < 2; i++)
        {
            float a = x1 - (i + 1) * 1.2f * K, b = a + 1.2f * K, zc0 = z1 - 2.0f;
            h.Box(new Vector3(a - .02f, F + .12f, zc0), new Vector3(a, F + 2.1f, z1));
            h.Box(new Vector3(a, F + .12f, zc0 - .02f), new Vector3(b - .7f, F + 2.1f, zc0));
            var c = new Vector3((a + b) * .5f, F, z1 - .42f * K);
            BraxUliceKit.S(ceramika).Walec(c - Vector3.forward * .1f, .15f * K, .28f * K, 16);
            BraxUliceKit.S(ceramika).Walec(c + new Vector3(0, .28f * K, -.14f), .21f * K, .12f * K, 18);
            BraxUliceKit.S(ceramika).Box(new Vector3(c.x - .22f * K, F + .5f * K, z1 - .2f * K), new Vector3(c.x + .22f * K, F + .9f * K, z1));
            KolL("kabina WC", new Vector3(a - .02f, F, zc0 - .02f), new Vector3(b - .7f, F + 2.1f, zc0));
            KolL("miska", new Vector3(c.x - .25f * K, F, z1 - .7f * K), new Vector3(c.x + .25f * K, F + .45f * K, z1));
        }
        // umywalki z lustrem na ścianie działowej, prysznice przy ścianie zachodniej
        for (int i = 0; i < 2; i++)
        {
            var u = new Vector3(x0 + 2.6f + i * .9f, F + .85f * K, zP + .2f + .25f * K);
            BraxUliceKit.S(ceramika).Box(u + new Vector3(-.3f * K, -.12f, -.25f * K), u + new Vector3(.3f * K, 0, .25f * K));
            BraxUliceKit.S(chrom).Rura(u + new Vector3(0, 0, -.2f * K), u + new Vector3(0, .2f, -.2f * K), .014f, 8);
        }
        BraxUliceKit.S(lustro).Box(new Vector3(x0 + 2.2f, F + 1.15f * K, zP + .2f), new Vector3(x0 + 4.0f, F + 1.95f * K, zP + .22f));
        KolL("umywalki", new Vector3(x0 + 2.2f, F, zP + .2f), new Vector3(x0 + 4.0f, F + .85f * K, zP + .2f + .5f * K));
        foreach (float z in new[] { 3.0f, 4.4f })
        {
            BraxUliceKit.S(chrom).Rura(new Vector3(x0 + .02f, F + 1.1f, z), new Vector3(x0 + .02f, F + 2.2f * K, z), .014f, 8);
            BraxUliceKit.S(chrom).Rura(new Vector3(x0 + .02f, F + 2.2f * K, z), new Vector3(x0 + .35f, F + 2.2f * K, z), .014f, 8);
            BraxUliceKit.S(chrom).Walec(new Vector3(x0 + .35f, F + 2.2f * K - .03f, z), .12f, .03f, 16);
        }
        h.Box(new Vector3(x0, F + .12f, 3.68f), new Vector3(x0 + 1.1f, F + 2.0f, 3.72f));
        BraxRegion.Warm(root, "Szatnia światło", root.TransformPoint(new Vector3(9f, 3.4f, -2f)), 1.1f, 7f);
        BraxRegion.Warm(root, "WC światło", root.TransformPoint(new Vector3(9f, 3.4f, 3.2f)), 1.0f, 6f);
        BraxMiastoA.Akcja(root, "sport_szatnia", "PRZEBIERZ SIĘ", "Szatnia dla grających na boiskach — szafka 12 wolna.", root.TransformPoint(new Vector3(9.5f, F, -2.1f)), 1.6f);
    }

    static Transform BraxMiastoSportKol => kol;
}

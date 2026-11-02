using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Bud = BraxRegionMeble.Bud;

// BRAXHORN CITY — WIADUKT GRYWALNY (wykonawca „tyły”, 2.11; plan właściciela 31.10 „tyły siłowni” p. 4; zgłoszenia 13.1 p. 16, 18,
// 24–30: gołe filary, posadzka-lustro pod wiaduktem, warsztaty z ogromnymi cegłami i czarną listwą zamiast szyldu, portal tunelu =
// zielona ściana z trawy, kopuła wzgórza z szumem, wiadukt urywa się w powietrzu).
// USUNIĘTE (zasada właściciela 11:00 „ściągasz, kładziesz nową”): cały „Riveted steel viaduct” (BraxCityGround.Viaduct, pomost
// 9.35 m z szynami, z −14…120) — obiekt zniszczony razem z kolizjami; stare przęsła/portal/kopuła z BraxRegionBackyard nie powstają
// (kod przepisany). Pod wiaduktem (korytarz x −70.95…−61.45) stara posadzka oryginału wycięta przed położeniem nowej.
// NOWY WIADUKT (oś x −66.2, pomost 8.16 m — WIADUKT.md): dwie arkady ceglanych łuków odcinkowych na filarach co 12 m, pomost
// żelbetowy z jezdnią (asfalt, linie), chodnikami, krawężnikami granitowymi i ceglaną attyką z czapą; dwa przęsła stalowe
// (blachownice z żebrami, poprzecznice) nad placem ustawek i nad ulicą główną; latarnie na pomoście.
// Pod łukami: WULKANIZACJA (zamknięta), AUTO NAPRAWA (otwarta: podnośnik, kanał z autem, narzędzia), GARAŻ (zamknięty) — cegła
// red_brick_03 w skali (1.0 m × k), bramy rolowane, drzwi, okna, szyldy; plac ustawek 3 na 3 (25.9 × 15.6 m) pod przęsłem
// stalowym; przejście ulica ↔ tyły; przystanek POD WIADUKTEM (wiata, ławka, rozkład, słupek); schody na pomost;
// tunel na końcu = BRAMA DO OSIEDLA: kamienny portal w skarpie, szlaban, napis „OSIEDLE”, tunel z oprawami; wzgórze naturalne
// (skarpa skalna + łąka, szum fBm, drzewa, głazy) zamiast kopuły.
public static class BraxRegionWiadukt
{
    public const float OS = -66.2f, X0 = -70.95f, X1 = -61.45f, Mur = .9f;
    public const float XW = X0 + Mur, XE = X1 - Mur;                  // lica wewnętrzne arkad (−70.05 / −62.35)
    public const float Pomost = 8.16f, Chodnik = 8.35f, Spod = 7.30f; // jezdnia, chodniki pomostu, spód pomostu
    public const float Wezglowie = 4.0f, Klucz = 6.6f;                // łuk odcinkowy: wezgłowie i klucz
    public const float JezdniaX0 = -69.55f, JezdniaX1 = -62.85f;
    public const float PortalZ = -74f, KoniecZ = 123f;
    const float P = .15f;                                            // poziom posadzki tyłów i chodnika ulicy
    const float FilarPol = .7f;                                       // pół szerokości filara wzdłuż Z
    public static readonly float[] Podpory = { PortalZ, -62f, -50f, -38f, -8f, 4.8f, 17.6f, 39f, 51f, 63f, 75f, 87f, 99f, 111f, KoniecZ };
    static readonly Vector2[] Stalowe = { new Vector2(-38f, -8f), new Vector2(17.6f, 39f) };
    public static readonly Rect Ustawka = Rect.MinMaxRect(-74.0f, -37.2f, -58.4f, -11.3f);   // 25.9 × 15.6 m (20 × 12 m × k)
    public static readonly Rect Schody = Rect.MinMaxRect(-61.45f, 0f, -57.30f, 11.2f);

    static Transform root;
    static Material cegla, ceglaWypelnienie, kamien, betonM, asfalt, kostka, granit, stalMost, stalCiemna, farba, szklo, ciemnosc, plytkiTunelu;
    static int przesel, lukow, warsztatow, punkty;
    static readonly List<BraxCityPoint> pkt = new List<BraxCityPoint>();
    static float K => BraxRegionMeble.K;

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        przesel = lukow = warsztatow = punkty = 0; pkt.Clear();
        int usuniete = UsunStary(cityRoot);
        root = BraxRegion.Root(cityRoot, "CLAUDE wiadukt (łuki, przęsła, warsztaty, ustawki, przystanek, tunel do osiedla)");
        Materialy();
        WytnijStaraPosadzke();
        // filary i łuki/przęsła
        for (int i = 0; i < Podpory.Length - 1; i++)
        {
            float za = Podpory[i], zb = Podpory[i + 1];
            bool stal = Stalowe.Any(s => Mathf.Abs(s.x - za) < .01f && Mathf.Abs(s.y - zb) < .01f);
            var t = new GameObject((stal ? "Przęsło stalowe " : "Łuki ") + za.ToString("F1") + "…" + zb.ToString("F1")).transform; t.SetParent(root, false);
            if (stal) PrzesloStalowe(t, za, zb); else Luki(t, za, zb);
            Pomost(t, za, zb, stal);
            przesel++;
        }
        for (int i = 1; i < Podpory.Length; i++) Filar(Podpory[i]);
        Posadzki();
        Warsztaty();
        PlacUstawek();
        Przystanek();
        SchodyNaPomost();
        Portal();
        Wzgorze();
        if (world) world.points = world.points.Concat(pkt).ToArray();
        punkty = pkt.Count;
        BraxRegion.Note("wiadukt: usunięty stary („Riveted steel viaduct”: obiektów " + usuniete + "), przęseł " + przesel + ", łuków " + lukow + ", warsztatów " + warsztatow + ", punktów misji " + punkty
                        + "; oś x " + OS + ", pomost y " + Pomost + ", z " + PortalZ + "…" + KoniecZ);
        Debug.Log("BRAXHORN_WIADUKT oś x=" + OS + " lica " + X0 + "/" + X1 + " pomost y=" + Pomost + " podpory z=" + string.Join(",", Podpory.Select(z => z.ToString("F1"))) + " przęsła stalowe −38…−8, 17.6…39; ustawki " + Ustawka + "; portal z=" + PortalZ);
    }

    // ------------------------------------------------------------------ usuwanie starego
    static int UsunStary(Transform cityRoot)
    {
        int n = 0;
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true).Where(t => t && t.name == "Riveted steel viaduct").ToArray())
        { n += t.GetComponentsInChildren<Transform>(true).Length; UnityEngine.Object.DestroyImmediate(t.gameObject); }
        return n;
    }

    // Stara posadzka/teren oryginału pod nowym wiaduktem i na placach, które kładę od nowa (y −0.6…0.45): wycinana precyzyjnie.
    static void WytnijStaraPosadzke()
    {
        var src = BraxClaudeGround.SourceCity; if (!src) return;
        int n = 0;
        foreach (var r in new[] { Rect.MinMaxRect(X0, -74f, X1, 20.6f), Rect.MinMaxRect(X0, 36.1f, X1, KoniecZ + 1f), Ustawka })
        {
            var b = new Bounds(new Vector3(r.center.x, -.075f, r.center.y), new Vector3(r.width, 1.05f, r.height));
            try { n += BraxCityMeshCut.Subtract(src, b, BraxRegion.Out + "/Cuts", "wiadukt_posadzka"); } catch (Exception e) { BraxRegion.Note("wiadukt: cięcie posadzki: " + e.Message); }
        }
        BraxRegion.Note("wiadukt: stara posadzka oryginału wycięta z " + n + " siatek");
    }

    static void Materialy()
    {
        cegla = BraxRegionMeble.Tex("Wiadukt: cegła klinkierowa łuków", 1.0f, new Color(.86f, .80f, .76f), .9f, "red_brick_03", "brick_wall_10", "brick_wall_02");
        ceglaWypelnienie = BraxRegionMeble.Tex("Warsztaty: cegła zamurowań (nowsza)", 1.0f, new Color(1.02f, .92f, .86f), .9f, "red_brick_03", "brick_wall_10", "brick_wall_02");
        kamien = BraxRegionMeble.Tex("Wiadukt: piaskowiec gzymsów i cokołów", 2.6f, new Color(.86f, .84f, .80f), .85f, "white_sandstone_blocks_02", "rough_block_wall", "concrete_wall_008");
        betonM = BraxRegionMeble.Tex("Wiadukt: beton spodu pomostu", 2.0f, new Color(.74f, .73f, .70f), .92f, "concrete_floor_02", "concrete_wall_008", "concrete_pavement_02");
        asfalt = BraxRegionMeble.Tex("Wiadukt: asfalt jezdni", 4.04f, new Color(.62f, .62f, .63f), .9f, "asphalt_04", "clean_asphalt");
        kostka = BraxRegionMeble.Tex("Tyły: płyty chodnikowe", 2.0f, new Color(.88f, .88f, .86f), .85f, "concrete_pavers_02", "concrete_pavement_02");
        granit = BraxRegionMeble.Tex("Tyły: granit krawężników", 1.88f, new Color(.80f, .80f, .80f), .8f, "granite_tile_02", "concrete_pavement_02");
        stalMost = BraxRegionMeble.Tex("Wiadukt: stal malowana (zieleń mostowa, rdza)", 2.2f, new Color(.42f, .50f, .46f), .7f, "rusty_painted_metal", "painted_metal_shutter");
        stalCiemna = BraxRegionMeble.Gladki("Wiadukt: stal ciemna (łożyska, dylatacje)", new Color(.12f, .13f, .13f), .55f, .6f);
        farba = BraxRegionMeble.Gladki("Jezdnia: farba biała (oznakowanie)", new Color(.86f, .86f, .82f), .6f);
        szklo = BraxRegionMeble.Szklo;
        ciemnosc = BraxRegionMeble.Gladki("Tunel: głąb (matowa czerń)", new Color(.015f, .015f, .016f), 1f);
        plytkiTunelu = BraxRegionMeble.Tex("Tunel: okładzina ścian (płytki jasne)", 2.26f, new Color(.78f, .78f, .75f), .6f, "dirty_tiles", "square_tiles", "large_grey_tiles");
    }

    // ------------------------------------------------------------------ geometria łuków
    static float LukY(float z, float z0, float z1)
    {
        float c = z1 - z0, r = Klucz - Wezglowie; float R = (c * c * .25f + r * r) / (2f * r), zc = (z0 + z1) * .5f, yc = Klucz - R;
        return yc + Mathf.Sqrt(Mathf.Max(0f, R * R - (z - zc) * (z - zc)));
    }
    static void QuadN(Bud b, Vector3 a, Vector3 c1, Vector3 c2, Vector3 d, Vector3 hint)
    {
        var n = Vector3.Cross(c1 - a, c2 - a);
        if (Vector3.Dot(n, hint) >= 0f) b.Quad(a, c1, c2, d); else b.Quad(a, d, c2, c1);
    }

    // Ściana arkady z otworem łukowym między z0 i z1 (od ziemi do łuku), pełna nad łukiem do yTop. xo = lico zewn., xi = wewn.
    static void MurZLukiem(Bud b, float xo, float xi, float z0, float z1, float yTop)
    {
        int N = Mathf.Max(12, Mathf.CeilToInt((z1 - z0) / .5f)); float sx = Mathf.Sign(xo - xi);
        for (int j = 0; j < N; j++)
        {
            float za = Mathf.Lerp(z0, z1, (float)j / N), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / N), ya = LukY(za, z0, z1), yb = LukY(zb, z0, z1);
            QuadN(b, new Vector3(xo, ya, za), new Vector3(xo, yb, zb), new Vector3(xo, yTop, zb), new Vector3(xo, yTop, za), new Vector3(sx, 0, 0));
            QuadN(b, new Vector3(xi, ya, za), new Vector3(xi, yb, zb), new Vector3(xi, yTop, zb), new Vector3(xi, yTop, za), new Vector3(-sx, 0, 0));
            QuadN(b, new Vector3(xo, ya, za), new Vector3(xi, ya, za), new Vector3(xi, yb, zb), new Vector3(xo, yb, zb), Vector3.down);
        }
    }

    // Archiwolta: pas kamienia nad łukiem na licu zewnętrznym (wystaje 5 cm), szer. 0.40 m, + zwornik.
    static void Archiwolta(Bud k, float xo, float sx, float z0, float z1)
    {
        int N = 24; float x2 = xo + sx * .05f;
        for (int j = 0; j < N; j++)
        {
            float za = Mathf.Lerp(z0, z1, (float)j / N), zb = Mathf.Lerp(z0, z1, (float)(j + 1) / N);
            float ya = LukY(za, z0, z1), yb = LukY(zb, z0, z1);
            QuadN(k, new Vector3(x2, ya, za), new Vector3(x2, yb, zb), new Vector3(x2, yb + .4f, zb), new Vector3(x2, ya + .4f, za), new Vector3(sx, 0, 0));
            QuadN(k, new Vector3(xo, ya + .4f, za), new Vector3(x2, ya + .4f, za), new Vector3(x2, yb + .4f, zb), new Vector3(xo, yb + .4f, zb), Vector3.up);
            QuadN(k, new Vector3(xo, ya, za), new Vector3(x2, ya, za), new Vector3(x2, yb, zb), new Vector3(xo, yb, zb), Vector3.down);
        }
        float zm = (z0 + z1) * .5f;
        k.Box(new Vector3(xo + sx * .08f, Klucz + .25f, zm), new Vector3(.16f, .7f, .55f));
    }

    static void Luki(Transform t, float za, float zb)
    {
        float z0 = za == PortalZ ? PortalZ : za + FilarPol, z1 = zb - FilarPol;
        var c = new Bud(); var k = new Bud();
        foreach (var (xo, xi) in new[] { (X0, XW), (X1, XE) })
        {
            MurZLukiem(c, xo, xi, z0, z1, Spod);
            float sx = Mathf.Sign(xo - xi);
            Archiwolta(k, xo, sx, z0, z1);
            lukow++;
        }
        // gzyms (piaskowiec) i attyka ceglana z czapą — zewnętrzne pasy pomostu
        foreach (var (xo, sx) in new[] { (X0, -1f), (X1, 1f) })
        {
            float xin = xo - sx * .35f;
            k.MinMax(new Vector3(xo + sx * .12f, Spod, za), new Vector3(xin, Spod + .35f, zb));
            c.MinMax(new Vector3(xo, Spod + .35f, za), new Vector3(xin, Chodnik + 1.3f, zb));
            k.MinMax(new Vector3(xo + sx * .05f, Chodnik + 1.3f, za), new Vector3(xin - sx * .05f, Chodnik + 1.38f, zb));
            BraxRegionMeble.Kolizja(t, "Attyka: kolizja", new Vector3((xo + xin) * .5f, Chodnik + .65f, (za + zb) * .5f), new Vector3(.35f, 1.3f + .1f, zb - za));
        }
        c.Obiekt("Łuki: cegła", t, cegla, false, true);
        k.Obiekt("Łuki: kamień (archiwolty, gzyms, czapa)", t, kamien, false, true);
        // kolizje murów nad łukami (gracz nie przechodzi przez mur nad otworem; otwór pod łukiem wolny)
        foreach (var (xo, xi) in new[] { (X0, XW), (X1, XE) })
            BraxRegionMeble.Kolizja(t, "Arkada: kolizja nad łukiem", new Vector3((xo + xi) * .5f, (Klucz + Spod) * .5f + .2f, (z0 + z1) * .5f), new Vector3(Mur, Spod - Klucz - .4f, z1 - z0));
    }

    static void Filar(float z)
    {
        var t = new GameObject("Filar z=" + z.ToString("F1")).transform; t.SetParent(root, false);
        var c = new Bud(); var k = new Bud();
        foreach (var (xa, xb) in new[] { (X0, XW), (XE, X1) })
        {
            c.MinMax(new Vector3(xa, .6f, z - FilarPol), new Vector3(xb, Spod, z + FilarPol));
            k.MinMax(new Vector3(xa - .08f, 0f, z - FilarPol - .08f), new Vector3(xb + .08f, .6f, z + FilarPol + .08f));            // cokół
            k.MinMax(new Vector3(xa - .06f, Wezglowie - .28f, z - FilarPol - .06f), new Vector3(xb + .06f, Wezglowie, z + FilarPol + .06f));   // impost
            BraxRegionMeble.Kolizja(t, "Filar: kolizja", new Vector3((xa + xb) * .5f, Spod * .5f, z), new Vector3(xb - xa + .16f, Spod, 2 * FilarPol + .16f));
        }
        c.Obiekt("Filar: cegła", t, cegla); k.Obiekt("Filar: piaskowiec (cokół, impost)", t, kamien);
    }

    // ------------------------------------------------------------------ przęsło stalowe (blachownice nitowane)
    static void PrzesloStalowe(Transform t, float za, float zb)
    {
        float z0 = za + FilarPol, z1 = zb - FilarPol, dl = zb - za;
        float yU = dl > 25f ? 5.6f : 6.0f;
        var s = new Bud(); var d = new Bud();
        foreach (float xg in new[] { X0 + .45f, X1 - .45f })
        {
            s.MinMax(new Vector3(xg - .02f, yU + .05f, za), new Vector3(xg + .02f, Spod - .05f, zb));             // środnik
            s.MinMax(new Vector3(xg - .28f, Spod - .05f, za), new Vector3(xg + .28f, Spod, zb));                   // pas górny
            s.MinMax(new Vector3(xg - .28f, yU, za), new Vector3(xg + .28f, yU + .05f, zb));                       // pas dolny
            s.MinMax(new Vector3(xg - .28f, yU + .05f, za), new Vector3(xg - .27f, yU + .14f, zb));                // kątowniki pasa (z nitami w teksturze)
            s.MinMax(new Vector3(xg + .27f, yU + .05f, za), new Vector3(xg + .28f, yU + .14f, zb));
            for (float z = za + .75f; z < zb - .5f; z += 1.5f)                                                     // żebra
            {
                s.MinMax(new Vector3(xg - .2f, yU + .05f, z - .015f), new Vector3(xg - .02f, Spod - .05f, z + .015f));
                s.MinMax(new Vector3(xg + .02f, yU + .05f, z - .015f), new Vector3(xg + .2f, Spod - .05f, z + .015f));
            }
            foreach (float zz in new[] { za + .35f, zb - .35f }) d.MinMax(new Vector3(xg - .35f, yU - .18f, zz - .4f), new Vector3(xg + .35f, yU, zz + .4f));   // łożyska
            BraxRegionMeble.Kolizja(t, "Blachownica: kolizja", new Vector3(xg, (yU + Spod) * .5f, (za + zb) * .5f), new Vector3(.56f, Spod - yU, dl));
        }
        for (float z = za + 1.5f; z < zb - .5f; z += 3f)                                                            // poprzecznice
        {
            s.MinMax(new Vector3(X0 + .47f, Spod - .7f, z - .012f), new Vector3(X1 - .47f, Spod - .05f, z + .012f));
            s.MinMax(new Vector3(X0 + .47f, Spod - .72f, z - .14f), new Vector3(X1 - .47f, Spod - .7f, z + .14f));
        }
        // stężenia dolne (kratownica w poziomie)
        for (float z = za + 1.5f; z < zb - 3.4f; z += 3f)
            d.Cyl(new Vector3(X0 + .6f, Spod - .72f, z), new Vector3(X1 - .6f, Spod - .72f, z + 3f), .035f, 6);
        // osłona czoła pomostu i balustrada stalowa (zamiast attyki ceglanej)
        foreach (var (xo, sx) in new[] { (X0, -1f), (X1, 1f) })
        {
            s.MinMax(new Vector3(xo, Spod - .3f, za), new Vector3(xo - sx * .05f, Chodnik + .12f, zb));
            var b = new Bud();
            BraxRegionMeble.Barierka(t, b, new Vector3(xo - sx * .12f, Chodnik, za + .2f), new Vector3(xo - sx * .12f, Chodnik, zb - .2f));
            b.Obiekt("Przęsło: balustrada", t, stalMost);
        }
        s.Obiekt("Przęsło: blachownice i poprzecznice", t, stalMost);
        d.Obiekt("Przęsło: łożyska i stężenia", t, stalCiemna);
    }

    // ------------------------------------------------------------------ pomost (płyta, jezdnia, chodniki, oznakowanie, latarnie)
    static void Pomost(Transform t, float za, float zb, bool stal)
    {
        var b = new Bud(); var a = new Bud(); var ch = new Bud(); var kr = new Bud(); var f = new Bud(); var dyl = new Bud();
        float xin0 = stal ? X0 + .05f : X0 + .35f, xin1 = stal ? X1 - .05f : X1 - .35f;
        b.MinMax(new Vector3(xin0, Spod, za), new Vector3(xin1, Pomost - .08f, zb));
        a.MinMax(new Vector3(JezdniaX0, Pomost - .08f, za), new Vector3(JezdniaX1, Pomost, zb));
        ch.MinMax(new Vector3(xin0, Pomost - .08f, za), new Vector3(JezdniaX0 - .15f, Chodnik, zb));
        ch.MinMax(new Vector3(JezdniaX1 + .15f, Pomost - .08f, za), new Vector3(xin1, Chodnik, zb));
        kr.MinMax(new Vector3(JezdniaX0 - .15f, Pomost - .08f, za), new Vector3(JezdniaX0, Chodnik, zb));
        kr.MinMax(new Vector3(JezdniaX1, Pomost - .08f, za), new Vector3(JezdniaX1 + .15f, Chodnik, zb));
        // oznakowanie: krawędziowe ciągłe, osiowe przerywane (3 m / 6 m)
        foreach (float x in new[] { JezdniaX0 + .25f, JezdniaX1 - .25f }) f.MinMax(new Vector3(x - .06f, Pomost, za), new Vector3(x + .06f, Pomost + .004f, zb));
        for (float z = Mathf.Ceil(za / 9f) * 9f; z < zb - 3f; z += 9f) f.MinMax(new Vector3(OS - .06f, Pomost, z), new Vector3(OS + .06f, Pomost + .004f, z + 3f));
        dyl.MinMax(new Vector3(JezdniaX0, Pomost, zb - .06f), new Vector3(JezdniaX1, Pomost + .006f, zb + .06f));       // dylatacja nad podporą
        b.Obiekt("Pomost: płyta żelbetowa", t, betonM);
        a.Obiekt("Pomost: jezdnia", t, asfalt, false, false);
        ch.Obiekt("Pomost: chodniki", t, kostka, false, false);
        kr.Obiekt("Pomost: krawężniki", t, granit);
        f.Obiekt("Pomost: oznakowanie poziome", t, farba, false, false);
        dyl.Obiekt("Pomost: dylatacja", t, stalCiemna, false, false);
        BraxRegionMeble.Kolizja(t, "Pomost: jezdnia kolizja", new Vector3(OS, Pomost - .4f, (za + zb) * .5f), new Vector3(JezdniaX1 - JezdniaX0, .8f, zb - za));
        foreach (var (x0, x1) in new[] { (xin0, JezdniaX0), (JezdniaX1, xin1) })
            BraxRegionMeble.Kolizja(t, "Pomost: chodnik kolizja", new Vector3((x0 + x1) * .5f, Chodnik - .4f, (za + zb) * .5f), new Vector3(x1 - x0, .8f, zb - za));
        // latarnie co 24 m naprzemiennie (słup przy attyce)
        int idx = Array.IndexOf(Podpory, za);
        if (idx % 2 == 0 && za > PortalZ + 1f)
        {
            float x = idx % 4 == 0 ? X0 + .55f : X1 - .55f;
            BraxRegionMeble.Latarnia(t, new Vector3(x, Chodnik, za + .1f), 6.0f);
        }
    }

    // ------------------------------------------------------------------ posadzki pod wiaduktem
    static GameObject Plyta(Transform t, string nazwa, Rect r, float y, Material m, IList<Rect> dziury = null)
    {
        var b = new Bud(); var holes = dziury ?? new Rect[0];
        var xs = new SortedSet<float> { r.xMin, r.xMax }; var zs = new SortedSet<float> { r.yMin, r.yMax };
        for (float x = r.xMin + 4f; x < r.xMax - .01f; x += 4f) xs.Add(x);
        for (float z = r.yMin + 4f; z < r.yMax - .01f; z += 4f) zs.Add(z);
        foreach (var h in holes) { foreach (var x in new[] { h.xMin, h.xMax }) if (x > r.xMin && x < r.xMax) xs.Add(x); foreach (var z in new[] { h.yMin, h.yMax }) if (z > r.yMin && z < r.yMax) zs.Add(z); }
        var xx = xs.ToArray(); var zz = zs.ToArray();
        for (int i = 0; i < xx.Length - 1; i++)
            for (int j = 0; j < zz.Length - 1; j++)
            {
                var c = new Vector2((xx[i] + xx[i + 1]) * .5f, (zz[j] + zz[j + 1]) * .5f);
                if (holes.Any(h => h.Contains(c))) continue;
                b.Quad(new Vector3(xx[i], y, zz[j]), new Vector3(xx[i], y, zz[j + 1]), new Vector3(xx[i + 1], y, zz[j + 1]), new Vector3(xx[i + 1], y, zz[j]));
            }
        return b.Obiekt(nazwa, t, m, true, false);
    }

    static void Posadzki()
    {
        var t = new GameObject("Posadzki pod wiaduktem").transform; t.SetParent(root, false);
        Plyta(t, "Przejście pod wiaduktem (ulica ↔ tyły)", Rect.MinMaxRect(X0, -11.3f, X1, 20.6f), P, kostka);
        Plyta(t, "Korytarz pod łukami (północ)", Rect.MinMaxRect(X0, 36.1f, X1, KoniecZ), P, kostka);
        pkt.Add(new BraxCityPoint { id = "R1_POD_WIADUKTEM_PRZEJSCIE", label = "Przejście pod wiaduktem", position = new Vector3(OS, P, 5f) });
    }

    // ------------------------------------------------------------------ warsztaty pod łukami
    sealed class Otwor { public float z0, z1, y0, y1; public Otwor(float a, float b, float c, float d) { z0 = a; z1 = b; y0 = c; y1 = d; } }

    // Zamurowanie łuku (płaszczyzna x = xf, grubość th) z otworami; od ziemi do łuku.
    static void Zamurowanie(Bud b, float xf, float th, float sx, float z0, float z1, List<Otwor> otw)
    {
        var cuts = new SortedSet<float> { z0, z1 };
        for (float z = z0 + .5f; z < z1 - .05f; z += .5f) cuts.Add(z);
        foreach (var o in otw) { cuts.Add(o.z0); cuts.Add(o.z1); }
        var zs = cuts.ToArray(); float xo = xf + sx * th * .5f, xi = xf - sx * th * .5f;
        for (int i = 0; i < zs.Length - 1; i++)
        {
            float za = zs[i], zb = zs[i + 1]; if (zb - za < .005f) continue;
            float ya = LukY(za, z0, z1), yb = LukY(zb, z0, z1), zm = (za + zb) * .5f;
            var o = otw.FirstOrDefault(q => zm > q.z0 && zm < q.z1);
            var pasy = new List<(float, float, float, float)>();   // (dół lewy, dół prawy, góra lewa, góra prawa)
            if (o == null) pasy.Add((0f, 0f, ya, yb));
            else { if (o.y0 > .001f) pasy.Add((0f, 0f, o.y0, o.y0)); pasy.Add((o.y1, o.y1, ya, yb)); }
            foreach (var (d0, d1, g0, g1) in pasy)
            {
                if (g0 - d0 < .005f && g1 - d1 < .005f) continue;
                foreach (var (x, s) in new[] { (xo, sx), (xi, -sx) })
                    QuadN(b, new Vector3(x, d0, za), new Vector3(x, d1, zb), new Vector3(x, g1, zb), new Vector3(x, g0, za), new Vector3(s, 0, 0));
            }
        }
        foreach (var o in otw)   // ościeża, nadproża i parapety
        {
            QuadN(b, new Vector3(xo, o.y0, o.z0), new Vector3(xi, o.y0, o.z0), new Vector3(xi, o.y1, o.z0), new Vector3(xo, o.y1, o.z0), new Vector3(0, 0, 1));
            QuadN(b, new Vector3(xo, o.y0, o.z1), new Vector3(xi, o.y0, o.z1), new Vector3(xi, o.y1, o.z1), new Vector3(xo, o.y1, o.z1), new Vector3(0, 0, -1));
            QuadN(b, new Vector3(xo, o.y1, o.z0), new Vector3(xi, o.y1, o.z0), new Vector3(xi, o.y1, o.z1), new Vector3(xo, o.y1, o.z1), Vector3.down);
            if (o.y0 > .001f) QuadN(b, new Vector3(xo, o.y0, o.z0), new Vector3(xi, o.y0, o.z0), new Vector3(xi, o.y0, o.z1), new Vector3(xo, o.y0, o.z1), Vector3.up);
        }
    }

    static void Warsztaty()
    {
        var spec = new[]
        {
            (z0: PortalZ, z1: -62f, nazwa: "WULKANIZACJA", otwarty: false, tlo: new Color(.10f, .22f, .52f), litery: new Color(.98f, .98f, .96f)),
            (z0: -62f, z1: -50f, nazwa: "AUTO NAPRAWA · MECHANIKA", otwarty: true, tlo: new Color(.96f, .96f, .94f), litery: new Color(.70f, .08f, .07f)),
            (z0: -50f, z1: -38f, nazwa: "GARAŻ", otwarty: false, tlo: new Color(.15f, .16f, .17f), litery: new Color(.98f, .80f, .12f)),
        };
        foreach (var w in spec)
        {
            var t = new GameObject("Warsztat pod łukiem: " + w.nazwa).transform; t.SetParent(root, false);
            float z0 = w.z0 == PortalZ ? PortalZ : w.z0 + FilarPol, z1 = w.z1 - FilarPol, zm = (z0 + z1) * .5f;
            float k = K;
            float bramaW = 3.6f, bramaH = 3.6f, drzwiW = 1.0f * k, drzwiH = 2.1f * k, oknoW = 1.8f, oknoH = 1.2f * k;
            var brama = new Otwor(zm - bramaW * .5f - .6f, zm + bramaW * .5f - .6f, 0f, bramaH);
            var drzwi = new Otwor(brama.z1 + .55f, brama.z1 + .55f + drzwiW, 0f, drzwiH);
            var okno = new Otwor(brama.z0 - .6f - oknoW, brama.z0 - .6f, 1.0f * k, 1.0f * k + oknoH);
            var mury = new Bud(); var ramy = new Bud(); var szyby = new Bud(); var ciemne = new Bud();
            // zamurowanie od frontu (wschód) cofnięte 25 cm w ościeże łuku, tył (zachód) z jednym oknem
            Zamurowanie(mury, X1 - .40f, .30f, 1f, z0, z1, new List<Otwor> { okno, brama, drzwi });
            var oknoTyl = new Otwor(zm - 1.0f, zm + 1.0f, 2.4f, 2.4f + 1.1f);
            Zamurowanie(mury, X0 + .40f, .30f, -1f, z0, z1, new List<Otwor> { oknoTyl });
            // ściany poprzeczne pod pomostem (oddzielają warsztaty) — na linii filarów
            foreach (float zw in new[] { w.z0, w.z1 })
            {
                if (Mathf.Abs(zw - PortalZ) < .01f) continue;   // od południa ściana przyczółka
                mury.MinMax(new Vector3(XW, 0f, zw - .2f), new Vector3(XE, Spod, zw + .2f));
                BraxRegionMeble.Kolizja(t, "Warsztat: ściana kolizja", new Vector3(OS, Spod * .5f, zw), new Vector3(XE - XW, Spod, .4f));
            }
            // kolizje zamurowań (z przerwą na bramę i drzwi otwartego warsztatu)
            foreach (var (xf, zz) in new[] { (X1 - .40f, new[] { (z0, w.otwarty ? brama.z0 : z1), (w.otwarty ? drzwi.z1 : z1, z1) }), (X0 + .40f, new[] { (z0, z1) }) })
                foreach (var (a, c) in zz) if (c - a > .05f) BraxRegionMeble.Kolizja(t, "Warsztat: zamurowanie kolizja", new Vector3(xf, 3.3f, (a + c) * .5f), new Vector3(.3f, 6.6f, c - a));
            if (w.otwarty) BraxRegionMeble.Kolizja(t, "Warsztat: nad bramą kolizja", new Vector3(X1 - .40f, (bramaH + 6.6f) * .5f, zm - .6f), new Vector3(.3f, 6.6f - bramaH, drzwi.z1 - brama.z0));
            // okna: rama stalowa + szkło zbrojone (front i tył)
            foreach (var (o, xf, sx) in new[] { (okno, X1 - .40f, 1f), (oknoTyl, X0 + .40f, -1f) })
            {
                float xg = xf + sx * .05f;
                ramy.MinMax(new Vector3(xg - .03f, o.y0, o.z0), new Vector3(xg + .03f, o.y1, o.z0 + .06f)); ramy.MinMax(new Vector3(xg - .03f, o.y0, o.z1 - .06f), new Vector3(xg + .03f, o.y1, o.z1));
                ramy.MinMax(new Vector3(xg - .03f, o.y0, o.z0), new Vector3(xg + .03f, o.y0 + .06f, o.z1)); ramy.MinMax(new Vector3(xg - .03f, o.y1 - .06f, o.z0), new Vector3(xg + .03f, o.y1, o.z1));
                ramy.MinMax(new Vector3(xg - .03f, o.y0, (o.z0 + o.z1) * .5f - .03f), new Vector3(xg + .03f, o.y1, (o.z0 + o.z1) * .5f + .03f));
                ramy.MinMax(new Vector3(xg - .03f, (o.y0 + o.y1) * .5f - .03f, o.z0), new Vector3(xg + .03f, (o.y0 + o.y1) * .5f + .03f, o.z1));
                szyby.MinMax(new Vector3(xg - .006f, o.y0, o.z0), new Vector3(xg + .006f, o.y1, o.z1));
                ramy.MinMax(new Vector3(xf + sx * .15f, o.y0 - .06f, o.z0 - .05f), new Vector3(xf + sx * .27f, o.y0, o.z1 + .05f));   // parapet blaszany
            }
            // drzwi stalowe (zamknięte) z klamką
            float xd = X1 - .30f;
            ramy.MinMax(new Vector3(xd - .02f, 0f, drzwi.z0), new Vector3(xd + .02f, drzwi.y1, drzwi.z1));
            var klamka = new Bud(); klamka.Cyl(new Vector3(xd + .02f, 1.05f * k, drzwi.z0 + .12f), new Vector3(xd + .09f, 1.05f * k, drzwi.z0 + .12f), .012f, 6);
            klamka.Cyl(new Vector3(xd + .09f, 1.05f * k, drzwi.z0 + .12f), new Vector3(xd + .09f, 1.05f * k, drzwi.z0 + .26f), .012f, 6);
            // brama rolowana: skrzynka nad otworem; zamknięta = pancerz w otworze
            ramy.MinMax(new Vector3(X1 - .25f, bramaH, brama.z0 - .1f), new Vector3(X1 + .15f, bramaH + .42f, brama.z1 + .1f));
            foreach (float z in new[] { brama.z0 - .05f, brama.z1 + .05f }) ramy.MinMax(new Vector3(X1 - .25f, 0f, z - .05f), new Vector3(X1 - .12f, bramaH, z + .05f));
            var pancerz = new Bud();
            if (!w.otwarty) pancerz.MinMax(new Vector3(X1 - .22f, 0f, brama.z0), new Vector3(X1 - .18f, bramaH, brama.z1));
            // szyld: tablica na wspornikach nad bramą (lico wschodnie), napis od strony uliczki
            var tab = new Bud();
            float sy0 = bramaH + .75f, sy1 = sy0 + 1.0f, sz0 = brama.z0 - .4f, sz1 = drzwi.z1 + .2f;
            tab.MinMax(new Vector3(X1 - .02f, sy0, sz0), new Vector3(X1 + .10f, sy1, sz1));
            ramy.MinMax(new Vector3(X1 + .10f, sy0 - .04f, sz0 - .04f), new Vector3(X1 + .14f, sy1 + .04f, sz1 + .04f));
            mury.Obiekt("Warsztat: mury (cegła w skali)", t, ceglaWypelnienie);
            ramy.Obiekt("Warsztat: stal (ramy, drzwi, brama)", t, BraxRegionMeble.Gladki("Warsztat: stal malowana grafit", new Color(.16f, .18f, .19f), .55f, .4f));
            szyby.Obiekt("Warsztat: szyby zbrojone", t, szklo, false, false);
            pancerz.Obiekt("Warsztat: pancerz bramy rolowanej", t, BraxRegionMeble.Tex("Warsztat: pancerz bramy (blacha ryflowana)", 2.0f, new Color(.80f, .82f, .84f), .5f, "painted_metal_shutter", "metal_plate"));
            klamka.Obiekt("Warsztat: klamka", t, BraxRegionMeble.StalSzara, false, false);
            tab.Obiekt("Warsztat: tablica szyldu", t, BraxRegionMeble.Gladki("Szyld: tło " + w.nazwa, w.tlo, .5f));
            BraxRegionMeble.Napis(t, w.nazwa, new Vector3(X1 + .16f, (sy0 + sy1) * .5f, (sz0 + sz1) * .5f), Vector3.right, w.nazwa.Length > 10 ? .40f : .62f, w.litery);
            // posadzka wewnątrz (beton) i lampa nad drzwiami
            var dziury = new List<Rect>();
            Rect kanal = default;
            if (w.otwarty) { kanal = Rect.MinMaxRect(OS - .58f, zm - 3.2f, OS + .58f, zm + 3.2f); dziury.Add(kanal); }
            Plyta(t, "Warsztat: posadzka", Rect.MinMaxRect(X0 + .25f, z0, X1 - .25f, z1), P, betonM, dziury);
            if (w.otwarty) WnetrzeWarsztatu(t, z0, z1, zm, kanal);
            else
            {
                // w zamkniętym: ciemne wnętrze widoczne przez okno — ściana ciemna za szybą (bez pustki)
                ciemne.MinMax(new Vector3(X1 - 1.2f, 0f, okno.z0 - .2f), new Vector3(X1 - 1.16f, okno.y1 + .3f, okno.z1 + .2f));
                ciemne.Obiekt("Warsztat: zaplecze za oknem", t, BraxRegionMeble.Gladki("Warsztat: zaplecze ciemne", new Color(.09f, .085f, .08f), .9f));
            }
            BraxRegionMeble.Model(t, "Warsztat: lampa nad drzwiami", new Vector3(X1 + .02f, drzwi.y1 + .35f, (drzwi.z0 + drzwi.z1) * .5f), 90f, .35f, false, "industrial_wall_lamp", "industrial_wall_sconce");
            pkt.Add(new BraxCityPoint { id = "R1_WARSZTAT_" + (w.otwarty ? "OTWARTY" : w.nazwa.Replace("Ż", "Z").Replace(' ', '_')), label = w.nazwa + (w.otwarty ? " (otwarty)" : " (zamknięty)"), position = new Vector3(X1 + 2f, P, zm) });
            warsztatow++;
        }
        // uliczka przed warsztatami: słupki i stojak z oponami przed wulkanizacją
        var u = new GameObject("Uliczka warsztatów: wyposażenie").transform; u.SetParent(root, false);
        for (int i = 0; i < 4; i++) BraxRegionMeble.Model(u, "Wulkanizacja: opona", new Vector3(X1 + .9f, P + i * .2f * K, -71.2f), BraxRegion.Range(0, 360), .22f, i == 0, "old_tyre");
        BraxRegionMeble.Model(u, "Wulkanizacja: felgi", new Vector3(X1 + .9f, P, -69.8f), 20f, .35f, true, "rusted_wheel_rim_01", "old_tyre");
        BraxRegionMeble.Model(u, "Garaż: beczki", new Vector3(X1 + .8f, P, -39.6f), 0f, .88f, true, "Barrel_02", "barrel_03", "wooden_barrels_01");
    }

    static void WnetrzeWarsztatu(Transform t, float z0, float z1, float zm, Rect kanal)
    {
        float k = K;
        var tile = BraxRegionMeble.Tex("Warsztat: płytki kanału", 2.26f, new Color(.86f, .86f, .84f), .55f, "dirty_tiles", "square_tiles", "large_grey_tiles");
        var b = new Bud(); var ziel = new Bud(); var pasy = new Bud(); var st = new Bud(); var lamp = new Bud();
        float dno = P - 1.5f * k;
        // kanał: ściany, dno, schodki od południa, krawędź w pasy żółto-czarne
        b.MinMax(new Vector3(kanal.xMin - .1f, dno - .1f, kanal.yMin), new Vector3(kanal.xMax + .1f, dno, kanal.yMax));
        b.MinMax(new Vector3(kanal.xMin - .12f, dno, kanal.yMin), new Vector3(kanal.xMin, P, kanal.yMax));
        b.MinMax(new Vector3(kanal.xMax, dno, kanal.yMin), new Vector3(kanal.xMax + .12f, P, kanal.yMax));
        b.MinMax(new Vector3(kanal.xMin, dno, kanal.yMax), new Vector3(kanal.xMax, P, kanal.yMax + .12f));
        int nst = 6; float rise = (P - dno) / nst;
        for (int i = 0; i < nst; i++) b.MinMax(new Vector3(kanal.xMin, dno, kanal.yMin - .12f + i * .3f), new Vector3(kanal.xMax, dno + rise * (nst - i), kanal.yMin + .18f + i * .3f));
        for (int i = 0; i < 22; i++)
        {
            float z = kanal.yMin + i * (kanal.height / 22f);
            ((i % 2 == 0) ? pasy : st).MinMax(new Vector3(kanal.xMin - .12f, P, z), new Vector3(kanal.xMin, P + .006f, z + kanal.height / 22f));
            ((i % 2 == 0) ? st : pasy).MinMax(new Vector3(kanal.xMax, P, z), new Vector3(kanal.xMax + .12f, P + .006f, z + kanal.height / 22f));
        }
        b.Obiekt("Kanał: płytki", t, tile, true);
        pasy.Obiekt("Kanał: pasy ostrzegawcze żółte", t, BraxRegionMeble.Gladki("Farba ostrzegawcza żółta", new Color(.95f, .76f, .08f), .6f), false, false);
        st.Obiekt("Kanał: pasy ostrzegawcze czarne", t, BraxRegionMeble.Gladki("Farba ostrzegawcza czarna", new Color(.05f, .05f, .05f), .6f), false, false);
        BraxRegionMeble.Kolizja(t, "Kanał: dno kolizja", new Vector3(kanal.center.x, dno - .05f, kanal.center.y), new Vector3(kanal.width, .1f, kanal.height));
        // auto nad kanałem (model auta gry, długość rzeczywista × k)
        var auto = AutoNaKanale(t, new Vector3(kanal.center.x, P, kanal.center.y));
        // podnośnik dwukolumnowy (puste ramiona) przy zamurowaniu tylnym
        var pod = new Bud(); float zp = zm, xa = OS - 3.5f;
        foreach (float dz in new[] { -1.75f * k, 1.75f * k })
        {
            pod.MinMax(new Vector3(xa - .2f, P, zp + dz - .2f), new Vector3(xa + .2f, P + 3.0f * k, zp + dz + .2f));
            pod.MinMax(new Vector3(xa - .35f, P, zp + dz - .35f), new Vector3(xa + .35f, P + .02f, zp + dz + .35f));
            pod.MinMax(new Vector3(xa - .12f, P + .5f, zp + dz - .1f), new Vector3(xa + 1.3f, P + .6f, zp + dz + .1f));   // ramię
        }
        pod.MinMax(new Vector3(xa - .15f, P + 3.0f * k, zp - 1.75f * k), new Vector3(xa + .15f, P + 3.0f * k + .15f, zp + 1.75f * k));   // belka górna
        pod.Obiekt("Podnośnik dwukolumnowy", t, BraxRegionMeble.Gladki("Podnośnik: lakier czerwony", new Color(.62f, .08f, .06f), .45f, .2f));
        BraxRegionMeble.Kolizja(t, "Podnośnik: kolumna kolizja", new Vector3(xa, P + 1.5f * k, zp - 1.75f * k), new Vector3(.4f, 3f * k, .4f));
        BraxRegionMeble.Kolizja(t, "Podnośnik: kolumna kolizja", new Vector3(xa, P + 1.5f * k, zp + 1.75f * k), new Vector3(.4f, 3f * k, .4f));
        // stół warsztatowy z imadłem, tablica z narzędziami (przy ścianie północnej), szafka narzędziowa, wózek, opony, beczki
        float zs = z1 - .55f;
        var stol = new Bud(); var blat = new Bud();
        stol.MinMax(new Vector3(OS - 2.4f, P, zs - .3f), new Vector3(OS + 1.6f, P + .82f * k, zs + .3f));
        blat.MinMax(new Vector3(OS - 2.45f, P + .82f * k, zs - .35f), new Vector3(OS + 1.65f, P + .87f * k, zs + .35f));
        stol.MinMax(new Vector3(OS - 2.4f, P + 1.15f * k, z1 - .06f), new Vector3(OS + 1.6f, P + 2.05f * k, z1 - .03f));     // tablica (perforowana)
        for (int i = 0; i < 9; i++) st.MinMax(new Vector3(OS - 2.2f + i * .42f, P + 1.3f * k + (i % 3) * .2f, z1 - .1f), new Vector3(OS - 2.13f + i * .42f, P + 1.55f * k + (i % 3) * .2f, z1 - .05f));   // klucze
        st.MinMax(new Vector3(OS + 1.0f, P + .87f * k, zs - .1f), new Vector3(OS + 1.3f, P + 1.0f * k, zs + .1f));   // imadło
        stol.Obiekt("Warsztat: stół i tablica", t, BraxRegionMeble.Gladki("Warsztat: stal szara stołu", new Color(.36f, .38f, .40f), .5f, .5f));
        blat.Obiekt("Warsztat: blat bukowy", t, BraxRegionMeble.Tex("Warsztat: blat bukowy", 1.0f, new Color(.85f, .72f, .56f), .6f, "wood_floor", "plank_flooring_02", "wood_planks"));
        st.Obiekt("Warsztat: narzędzia", t, BraxRegionMeble.StalCzarna, false, false);
        BraxRegionMeble.Kolizja(t, "Stół: kolizja", new Vector3(OS - .4f, P + .45f * k, zs), new Vector3(4.1f, .9f * k, .7f));
        BraxRegionMeble.Model(t, "Warsztat: szafka narzędziowa", new Vector3(OS + 2.6f, P, z1 - .6f), 180f, 1.0f, true, "metal_tool_chest", "drawer_cabinet");
        BraxRegionMeble.Model(t, "Warsztat: wózek narzędziowy", new Vector3(kanal.xMax + 1.3f, P, zm + 2.2f), 70f, .9f, true, "tool_cart", "hand_truck");
        for (int i = 0; i < 5; i++) BraxRegionMeble.Model(t, "Warsztat: opona", new Vector3(X1 - 1.4f, P + i * .2f * K, z0 + 1.1f), BraxRegion.Range(0, 360), .22f, i == 0, "old_tyre");
        BraxRegionMeble.Model(t, "Warsztat: beczka oleju", new Vector3(X0 + 1.2f, P, z0 + 1.0f), 0f, .88f, true, "Barrel_02", "barrel_03");
        BraxRegionMeble.Model(t, "Warsztat: znak mokra podłoga", new Vector3(X1 - 2.2f, P, zm - 3.3f), 60f, .65f, false, "WetFloorSign_01");
        BraxRegionMeble.Model(t, "Warsztat: kamera", new Vector3(X1 - .6f, 5.2f, z1 - .3f), 225f, .25f, false, "security_camera_01");
        // świetlówki na łańcuchach pod stropem (emisja, bez światła czasu rzeczywistego)
        for (int i = 0; i < 3; i++)
        {
            float z = Mathf.Lerp(z0 + 2f, z1 - 2f, i / 2f);
            foreach (float x in new[] { OS - 2f, OS + 2f })
            {
                lamp.MinMax(new Vector3(x - .08f, 4.6f, z - .7f), new Vector3(x + .08f, 4.64f, z + .7f));
                st.Cyl(new Vector3(x, 4.66f, z - .6f), new Vector3(x, Spod, z - .6f), .006f, 4, -1f, false);
                st.Cyl(new Vector3(x, 4.66f, z + .6f), new Vector3(x, Spod, z + .6f), .006f, 4, -1f, false);
            }
        }
        lamp.Obiekt("Warsztat: świetlówki", t, BraxRegionMeble.Swiecacy("Świetlówka warsztatowa", new Color(.95f, .97f, 1f), 1.4f), false, false);
        BraxRegion.Warm(t, "Warsztat: światło", new Vector3(OS, 4.4f, zm), 1.2f, 9f);
        pkt.Add(new BraxCityPoint { id = "R1_WARSZTAT_KANAL", label = "Auto na kanale", position = new Vector3(kanal.center.x, P, kanal.yMin - 1.2f) });
    }

    // Model auta z paczki gry (Assets/Source/Claude/Auta, dekodowane w CI): Audi S4 / inne; długość rzeczywista × k, koła na posadzce.
    static GameObject AutoNaKanale(Transform t, Vector3 p)
    {
        foreach (var (id, dl) in new[] { ("car_s4", 4.72f), ("car_mus", 4.61f), ("car_dc", 5.0f) })
        {
            string path = "Assets/Source/Claude/Auta/" + id + ".glb";
            if (!File.Exists(path)) continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
            if (!src) continue;
            var o = BraxRegion.Put(t, src, "Auto na kanale (" + id + ")", p, 0f, 1f, false, 0f);
            if (!o) continue;
            var b = BraxRegion.Bounds(o);
            if (b.size.x > b.size.z) { o.transform.RotateAround(b.center, Vector3.up, 90f); b = BraxRegion.Bounds(o); }
            float s = dl * K / Mathf.Max(.1f, b.size.z); o.transform.localScale *= s; b = BraxRegion.Bounds(o);
            o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
            b = BraxRegion.Bounds(o);
            BraxRegionMeble.Kolizja(t, "Auto: kolizja", new Vector3(b.center.x, b.center.y + .1f, b.center.z), new Vector3(b.size.x * .92f, b.size.y * .8f, b.size.z * .95f));
            return o;
        }
        BraxRegion.Note("wiadukt: brak modelu auta (Assets/Source/Claude/Auta) — kanał bez auta");
        return null;
    }

    // ------------------------------------------------------------------ plac ustawek 3 na 3
    static void PlacUstawek()
    {
        var t = new GameObject("Plac ustawek 3 na 3 (pod przęsłem stalowym)").transform; t.SetParent(root, false);
        Plyta(t, "Ustawki: wylewka betonowa", Ustawka, P, BraxRegionMeble.Tex("Ustawki: wylewka betonowa (spękana)", 2.0f, new Color(.70f, .69f, .66f), .93f, "concrete_floor_02", "concrete_wall_008"));
        // stare linie parkingowe przy krawędziach (wyblakłe), siatka ogrodzenia od zachodu, rekwizyty tylko na obrzeżach
        var f = new Bud();
        for (float z = Ustawka.yMin + 1.0f; z < Ustawka.yMax - 1f; z += 2.7f * K) f.MinMax(new Vector3(Ustawka.xMax - 5.2f, P, z - .05f), new Vector3(Ustawka.xMax - .4f, P + .004f, z + .05f));
        f.Obiekt("Ustawki: wyblakłe linie parkingowe", t, BraxRegionMeble.Gladki("Farba wyblakła", new Color(.62f, .62f, .58f), .8f), false, false);
        var siatka = new Bud(); var slupy = new Bud();
        float xs = Ustawka.xMin + .1f;
        for (float z = Ustawka.yMin; z <= Ustawka.yMax + .01f; z += 2.5f) slupy.Cyl(new Vector3(xs, P, z), new Vector3(xs, P + 2.0f * K, z), .03f, 8);
        slupy.Cyl(new Vector3(xs, P + 2.0f * K, Ustawka.yMin), new Vector3(xs, P + 2.0f * K, Ustawka.yMax), .022f, 8);
        siatka.MinMax(new Vector3(xs - .005f, P + .05f, Ustawka.yMin), new Vector3(xs + .005f, P + 2.0f * K, Ustawka.yMax));
        slupy.Obiekt("Ustawki: słupki ogrodzenia", t, BraxRegionMeble.StalSzara);
        var siatkaMat = BraxRegionMeble.Gladki("Ogrodzenie: siatka (półprzezroczysta)", new Color(.45f, .47f, .48f, .45f), .5f, .6f);
        siatka.Obiekt("Ustawki: siatka ogrodzenia", t, siatkaMat, false, false);
        BraxRegionMeble.Kolizja(t, "Ustawki: ogrodzenie kolizja", new Vector3(xs, P + K, Ustawka.center.y), new Vector3(.1f, 2f * K, Ustawka.height));
        BraxRegionMeble.Model(t, "Ustawki: opony", new Vector3(Ustawka.xMin + 1.2f, P, Ustawka.yMin + 1.2f), 0f, .22f, true, "old_tyre");
        BraxRegionMeble.Model(t, "Ustawki: opony", new Vector3(Ustawka.xMin + 1.3f, P + .22f * K, Ustawka.yMin + 1.25f), 40f, .22f, false, "old_tyre");
        BraxRegionMeble.Model(t, "Ustawki: beczka", new Vector3(Ustawka.xMin + 1.1f, P, Ustawka.yMax - 1.2f), 0f, .88f, true, "Barrel_02", "barrel_03");
        BraxRegionMeble.Model(t, "Ustawki: palety", new Vector3(Ustawka.xMax - 1.3f, P, Ustawka.yMin + 1.0f), 15f, .45f, true, "wooden_crate_02", "plastic_crate_01", "cardboard_box_01");
        pkt.Add(new BraxCityPoint { id = "R1_USTAWKA_3V3", label = "Plac ustawek 3 na 3 (środek)", position = new Vector3(Ustawka.center.x, P, Ustawka.center.y) });
        pkt.Add(new BraxCityPoint { id = "R1_USTAWKA_3V3_STRONA_A", label = "Ustawka: strona A", position = new Vector3(Ustawka.center.x, P, Ustawka.yMin + 4f) });
        pkt.Add(new BraxCityPoint { id = "R1_USTAWKA_3V3_STRONA_B", label = "Ustawka: strona B", position = new Vector3(Ustawka.center.x, P, Ustawka.yMax - 4f) });
    }

    // ------------------------------------------------------------------ przystanek POD WIADUKTEM (chodnik południowy ulicy głównej)
    static void Przystanek()
    {
        var t = new GameObject("PRZYSTANEK POD WIADUKTEM (linia 1)").transform; t.SetParent(root, false);
        float k = K, x0 = -69.6f, x1 = -63.6f, z0 = 20.75f, z1 = 22.65f, y = P, h = 2.5f * k;
        var rama = new Bud(); var sz = new Bud(); var dach = new Bud();
        foreach (float x in new[] { x0, x0 + 2f, x1 - 2f, x1 })
        {
            rama.MinMax(new Vector3(x - .05f, y, z0 - .05f), new Vector3(x + .05f, y + h, z0 + .05f));
        }
        foreach (float x in new[] { x0, x1 }) rama.MinMax(new Vector3(x - .05f, y, z1 - .05f), new Vector3(x + .05f, y + h, z1 + .05f));
        dach.MinMax(new Vector3(x0 - .2f, y + h, z0 - .25f), new Vector3(x1 + .2f, y + h + .12f, z1 + .3f));
        sz.MinMax(new Vector3(x0, y + .1f, z0 - .012f), new Vector3(x1, y + h - .05f, z0 + .012f));                // ściana tylna (od strony budynku)
        foreach (float x in new[] { x0, x1 }) sz.MinMax(new Vector3(x - .012f, y + .1f, z0 + .1f), new Vector3(x + .012f, y + h - .05f, z1 - .35f));
        rama.Obiekt("Wiata: rama aluminiowa", t, BraxRegionMeble.Gladki("Wiata: aluminium grafit", new Color(.20f, .21f, .22f), .4f, .7f));
        dach.Obiekt("Wiata: dach", t, BraxRegionMeble.Gladki("Wiata: dach poliwęglan dymiony", new Color(.30f, .32f, .33f), .3f, .2f));
        sz.Obiekt("Wiata: szyby", t, szklo, false, false);
        BraxRegionMeble.Kolizja(t, "Wiata: tył kolizja", new Vector3((x0 + x1) * .5f, y + h * .5f, z0), new Vector3(x1 - x0, h, .1f));
        BraxRegionMeble.Lawka(t, new Vector3((x0 + x1) * .5f, y, z0 + .55f), 0f, "Ławka przystanku POD WIADUKTEM");
        // rozkład jazdy (gablota) na ścianie bocznej + nazwa przystanku na dachu
        var gab = new Bud(); gab.MinMax(new Vector3(x1 - .06f, y + 1.0f * k, z0 + .3f), new Vector3(x1 - .02f, y + 1.85f * k, z0 + 1.2f));
        gab.Obiekt("Przystanek: gablota rozkładu", t, BraxRegionMeble.Gladki("Rozkład: tablica biała", new Color(.94f, .94f, .92f), .4f));
        BraxRegionMeble.Napis(t, "ROZKŁAD JAZDY · LINIA 1", new Vector3(x1 - .07f, y + 1.75f * k, z0 + .75f), Vector3.left, .07f, new Color(.08f, .1f, .25f));
        BraxRegionMeble.Napis(t, "NABRZEŻE ↔ WYLOT ZACHODNI", new Vector3(x1 - .07f, y + 1.62f * k, z0 + .75f), Vector3.left, .055f, new Color(.1f, .1f, .1f));
        BraxRegionMeble.Napis(t, "co 12 min · 5:00–23:00", new Vector3(x1 - .07f, y + 1.5f * k, z0 + .75f), Vector3.left, .055f, new Color(.1f, .1f, .1f));
        var tab = new Bud(); tab.MinMax(new Vector3(x0 + 1.2f, y + h + .12f, z1 + .1f), new Vector3(x1 - 1.2f, y + h + .55f, z1 + .14f));
        tab.Obiekt("Przystanek: tablica nazwy", t, BraxRegionMeble.Gladki("Przystanek: tablica granat", new Color(.08f, .16f, .38f), .5f));
        BraxRegionMeble.Napis(t, "POD WIADUKTEM", new Vector3((x0 + x1) * .5f, y + h + .335f, z1 + .16f), Vector3.forward, .22f, Color.white);
        // słupek D-15 (niebieski znak z białym polem i autobusem) przy krawężniku
        var sl = new Bud(); var zn = new Bud(); var bia = new Bud(); var bus = new Bud();
        float px = -62.9f, pz = 23.3f, ph = 2.6f * k;
        sl.Cyl(new Vector3(px, y, pz), new Vector3(px, y + ph, pz), .03f * k, 10);
        zn.MinMax(new Vector3(px - .3f * k, y + ph - .6f * k, pz + .03f), new Vector3(px + .3f * k, y + ph, pz + .05f));
        bia.MinMax(new Vector3(px - .24f * k, y + ph - .54f * k, pz + .05f), new Vector3(px + .24f * k, y + ph - .06f * k, pz + .056f));
        float bx = px, by = y + ph - .3f * k;
        bus.MinMax(new Vector3(bx - .17f * k, by - .08f * k, pz + .056f), new Vector3(bx + .17f * k, by + .1f * k, pz + .062f));
        foreach (float wx in new[] { -.1f, .1f }) bus.Cyl(new Vector3(bx + wx * k, by - .09f * k, pz + .062f), new Vector3(bx + wx * k, by - .09f * k, pz + .064f), .035f * k, 12);
        sl.Obiekt("D-15: słupek", t, BraxRegionMeble.StalSzara); zn.Obiekt("D-15: tarcza niebieska", t, BraxRegionMeble.Gladki("Znak: niebieski", new Color(.05f, .22f, .62f), .5f));
        bia.Obiekt("D-15: pole białe", t, BraxRegionMeble.Gladki("Znak: biały", new Color(.95f, .95f, .95f), .5f), false, false);
        bus.Obiekt("D-15: autobus", t, BraxRegionMeble.Gladki("Znak: czarny", new Color(.04f, .04f, .04f), .5f), false, false);
        BraxRegionMeble.Kolizja(t, "D-15: kolizja", new Vector3(px, y + ph * .5f, pz), new Vector3(.1f, ph, .1f));
        BraxRegionMeble.Kosz(t, new Vector3(x1 + .7f, y, z0 + .4f));
        pkt.Add(new BraxCityPoint { id = "R1_PRZYSTANEK_POD_WIADUKTEM", label = "Przystanek POD WIADUKTEM (linia 1)", position = new Vector3((x0 + x1) * .5f, y, 23.0f) });
    }

    // ------------------------------------------------------------------ schody na pomost (wschodnia strona, x −61.45…−57.3)
    static void SchodyNaPomost()
    {
        var t = new GameObject("Schody na wiadukt").transform; t.SetParent(root, false);
        float k = K, rise = Chodnik / 38f, go = .29f * k, w = 1.9f;
        float xa0 = -59.25f, xa1 = xa0 + w, xb0 = -61.40f, xb1 = xb0 + w;        // bieg A (zewn.) i B (przy wiadukcie)
        float zA0 = 2.0f, zLand0 = zA0 + 18 * go, zLand1 = zLand0 + 2.0f;       // bieg A w górę na północ, spocznik, bieg B na południe
        var b = new Bud(); var por = new Bud();
        for (int i = 0; i < 19; i++)   // bieg A: stopnie 1..19 (z zA0 → zLand0)
        {
            float y = (i + 1) * rise, z = zA0 + i * go;
            b.MinMax(new Vector3(xa0, 0f, z), new Vector3(xa1, y, Mathf.Min(z + go, zLand0)));
        }
        b.MinMax(new Vector3(xb0, 0f, zLand0), new Vector3(xa1, 19 * rise, zLand1));                         // spocznik
        for (int i = 0; i < 19; i++)   // bieg B: z zLand0 w dół osi z do 2.0 → y 19..38 × rise
        {
            float y = (20 + i) * rise, z1 = zLand0 - i * go;
            b.MinMax(new Vector3(xb0, 19 * rise - .3f, z1 - go), new Vector3(xb1, y, z1));
        }
        b.MinMax(new Vector3(xb0, Chodnik - .25f, 0.0f), new Vector3(xb1, Chodnik, zA0));                     // podest górny przy pomoście
        b.MinMax(new Vector3(xb0, 0f, zA0), new Vector3(xb1, 19 * rise - .3f, zLand0));                     // trzon pod biegiem B
        foreach (float z in new[] { .3f, 1.7f }) b.MinMax(new Vector3(xb0 + .2f, 0f, z - .2f), new Vector3(xb0 + .6f, Chodnik - .25f, z + .2f));   // słupy podestu
        b.Obiekt("Schody: beton", t, BraxRegionMeble.Beton);
        // pochylnie kolizji (gracz to CharacterController — gładki bieg zamiast 38 progów)
        float ang = Mathf.Atan2(rise, go) * Mathf.Rad2Deg, lenA = Mathf.Sqrt(Mathf.Pow(18 * go, 2) + Mathf.Pow(19 * rise, 2));
        BraxRegionMeble.Kolizja(t, "Schody: bieg A (pochylnia)", new Vector3((xa0 + xa1) * .5f, 19 * rise * .5f - .1f, (zA0 + zLand0) * .5f), new Vector3(w, .2f, lenA + .3f), Quaternion.Euler(-ang, 0, 0));
        BraxRegionMeble.Kolizja(t, "Schody: bieg B (pochylnia)", new Vector3((xb0 + xb1) * .5f, 19 * rise + 19 * rise * .5f - .1f, (zA0 + zLand0) * .5f), new Vector3(w, .2f, lenA + .3f), Quaternion.Euler(ang, 0, 0));
        BraxRegionMeble.Kolizja(t, "Schody: spocznik", new Vector3((xb0 + xa1) * .5f, 19 * rise - .1f, (zLand0 + zLand1) * .5f), new Vector3(xa1 - xb0, .2f, zLand1 - zLand0));
        BraxRegionMeble.Kolizja(t, "Schody: podest górny", new Vector3((xb0 + xb1) * .5f, Chodnik - .1f, zA0 * .5f), new Vector3(w, .2f, zA0));
        BraxRegionMeble.Kolizja(t, "Schody: trzon", new Vector3((xa0 + xa1) * .5f, 19 * rise * .25f, (zA0 + zLand0) * .5f + .2f), new Vector3(.01f, .01f, .01f));
        // poręcze (zewnętrzne)
        BraxRegionMeble.Barierka(t, por, new Vector3(xa1 - .05f, rise, zA0), new Vector3(xa1 - .05f, 19 * rise, zLand0));
        BraxRegionMeble.Barierka(t, por, new Vector3(xa1 - .05f, 19 * rise, zLand1 - .05f), new Vector3(xb0 + .05f, 19 * rise, zLand1 - .05f));
        BraxRegionMeble.Barierka(t, por, new Vector3(xb1 - .05f, 38 * rise, zA0), new Vector3(xb1 - .05f, 20 * rise, zLand0), false);
        BraxRegionMeble.Barierka(t, por, new Vector3(xb1 - .05f, Chodnik, 0.05f), new Vector3(xb1 - .05f, Chodnik, zA0));
        por.Obiekt("Schody: poręcze", t, BraxRegionMeble.Gladki("Poręcze: stal ocynk malowana", new Color(.32f, .35f, .36f), .45f, .6f));
        pkt.Add(new BraxCityPoint { id = "R1_SCHODY_NA_WIADUKT", label = "Schody na wiadukt", position = new Vector3((xa0 + xa1) * .5f, P, zA0 - 1.5f) });
        pkt.Add(new BraxCityPoint { id = "R1_WIADUKT_POMOST", label = "Pomost wiaduktu (chodnik)", position = new Vector3(X1 - .9f, Chodnik, 1.0f) });
    }

    // ------------------------------------------------------------------ portal tunelu = BRAMA DO OSIEDLA
    static void Portal()
    {
        var t = new GameObject("BRAMA DO OSIEDLA: portal tunelu, szlaban, znak").transform; t.SetParent(root, false);
        float k = K, zf = PortalZ, zb = PortalZ - 2.0f, xL = -77.5f, xR = -54.9f, top = 19.5f;
        float ox0 = -70.8f, ox1 = -61.6f, oy0 = Pomost, oyS = 12.4f, oyK = 14.6f;   // otwór: ściany pionowe do 12.4, łuk do 14.6
        float Arch(float x) { float c = ox1 - ox0, r = oyK - oyS, R = (c * c * .25f + r * r) / (2f * r), xc = (ox0 + ox1) * .5f; return oyK - R + Mathf.Sqrt(Mathf.Max(0f, R * R - (x - xc) * (x - xc))); }
        var m = new Bud(); var tun = new Bud(); var ciem = new Bud();
        // lico i tył przyczółka z otworem (pasy pionowe), boki, góra
        var xs = new List<float> { xL, ox0, ox1, xR }; for (float x = ox0 + .4f; x < ox1 - .05f; x += .4f) xs.Add(x); xs.Sort();
        for (int i = 0; i < xs.Count - 1; i++)
        {
            float xa = xs[i], xb2 = xs[i + 1];
            bool otw = xa >= ox0 - .001f && xb2 <= ox1 + .001f;
            foreach (var (z, s) in new[] { (zf, 1f), (zb, -1f) })
            {
                if (!otw) QuadN(m, new Vector3(xa, 0, z), new Vector3(xb2, 0, z), new Vector3(xb2, top, z), new Vector3(xa, top, z), new Vector3(0, 0, s));
                else
                {
                    float ya = xa <= ox0 + .001f ? oyS : Arch(xa), yb2 = xb2 >= ox1 - .001f ? oyS : Arch(xb2);
                    if (xa > ox0 + .001f) ya = Arch(xa); if (xb2 < ox1 - .001f) yb2 = Arch(xb2);
                    QuadN(m, new Vector3(xa, 0, z), new Vector3(xb2, 0, z), new Vector3(xb2, oy0, z), new Vector3(xa, oy0, z), new Vector3(0, 0, s));
                    QuadN(m, new Vector3(xa, ya, z), new Vector3(xb2, yb2, z), new Vector3(xb2, top, z), new Vector3(xa, top, z), new Vector3(0, 0, s));
                }
            }
        }
        QuadN(m, new Vector3(xL, 0, zf), new Vector3(xL, 0, zb), new Vector3(xL, top, zb), new Vector3(xL, top, zf), Vector3.left);
        QuadN(m, new Vector3(xR, 0, zf), new Vector3(xR, 0, zb), new Vector3(xR, top, zb), new Vector3(xR, top, zf), Vector3.right);
        QuadN(m, new Vector3(xL, top, zf), new Vector3(xR, top, zf), new Vector3(xR, top, zb), new Vector3(xL, top, zb), Vector3.up);
        // gzyms i czapa przyczółka, archiwolta wokół otworu, skrzydła oporowe wchodzące w skarpę
        var k2 = new Bud();
        k2.MinMax(new Vector3(xL - .2f, top, zf + .2f), new Vector3(xR + .2f, top + .45f, zb));
        k2.MinMax(new Vector3(xL - .1f, 0f, zf), new Vector3(xR + .1f, .8f, zf + .12f));
        for (int i = 0; i < 24; i++)
        {
            float xa = Mathf.Lerp(ox0, ox1, i / 24f), xb2 = Mathf.Lerp(ox0, ox1, (i + 1) / 24f);
            k2.MinMax(new Vector3(Mathf.Min(xa, xb2), Arch((xa + xb2) * .5f) + .02f, zf), new Vector3(Mathf.Max(xa, xb2), Arch((xa + xb2) * .5f) + .55f, zf + .1f));
        }
        foreach (float x in new[] { ox0, ox1 }) k2.MinMax(new Vector3(x - (x < OS ? .55f : 0f), oy0, zf), new Vector3(x + (x < OS ? 0f : .55f), oyS, zf + .1f));
        foreach (var (x, dir) in new[] { (xL, -1f), (xR, 1f) })
        {
            // skrzydło: od lica w głąb skarpy (12 m), góra opada z 19.5 do 12 m
            var a0 = new Vector3(x, 0, zf); var a1 = new Vector3(x + dir * 3.5f, 0, zf - 12f);
            for (int i = 0; i < 6; i++)
            {
                float t0 = i / 6f, t1 = (i + 1) / 6f; var p0 = Vector3.Lerp(a0, a1, t0); var p1 = Vector3.Lerp(a0, a1, t1);
                float h0 = Mathf.Lerp(top, 12f, t0), h1 = Mathf.Lerp(top, 12f, t1);
                var nrm = new Vector3(dir, 0, .29f * dir * dir).normalized;
                QuadN(m, p0, p1, p1 + Vector3.up * h1, p0 + Vector3.up * h0, new Vector3(dir, 0, 0));
                var q0 = p0 - new Vector3(dir * 1.2f, 0, 0); var q1 = p1 - new Vector3(dir * 1.2f, 0, 0);
                QuadN(m, p0 + Vector3.up * h0, p1 + Vector3.up * h1, q1 + Vector3.up * h1, q0 + Vector3.up * h0, Vector3.up);
            }
            BraxRegionMeble.Kolizja(t, "Skrzydło: kolizja", (a0 + a1) * .5f + Vector3.up * 8f - new Vector3(dir * .6f, 0, 0), new Vector3(1.2f, 16f, 12.5f), Quaternion.LookRotation((a1 - a0).normalized));
        }
        m.Obiekt("Portal: mur kamienny przyczółka", t, BraxRegionMeble.Tex("Portal: mur z ciosów kamiennych", 2.2f, new Color(.80f, .78f, .74f), .9f, "rough_block_wall", "rock_wall_08", "white_sandstone_blocks_02"));
        k2.Obiekt("Portal: piaskowiec (gzyms, archiwolta, cokół)", t, kamien);
        BraxRegionMeble.Kolizja(t, "Przyczółek: kolizja L", new Vector3((xL + ox0) * .5f, top * .5f, (zf + zb) * .5f), new Vector3(ox0 - xL, top, zf - zb));
        BraxRegionMeble.Kolizja(t, "Przyczółek: kolizja P", new Vector3((ox1 + xR) * .5f, top * .5f, (zf + zb) * .5f), new Vector3(xR - ox1, top, zf - zb));
        BraxRegionMeble.Kolizja(t, "Przyczółek: kolizja dół", new Vector3(OS, Pomost * .5f, (zf + zb) * .5f), new Vector3(ox1 - ox0, Pomost, zf - zb));
        // tunel: ściany i sklepienie (okładzina), jezdnia i chodniki 30 m w głąb, oprawy co 4 m, koniec w mroku
        float zEnd = PortalZ - 30f;
        int N = 20;
        for (int i = 0; i < N; i++)
        {
            float xa = Mathf.Lerp(ox0, ox1, i / (float)N), xb2 = Mathf.Lerp(ox0, ox1, (i + 1) / (float)N);
            QuadN(tun, new Vector3(xa, Arch(xa), zf), new Vector3(xb2, Arch(xb2), zf), new Vector3(xb2, Arch(xb2), zEnd), new Vector3(xa, Arch(xa), zEnd), Vector3.down);
        }
        foreach (var (x, s) in new[] { (ox0, 1f), (ox1, -1f) })
            QuadN(tun, new Vector3(x, oy0, zf), new Vector3(x, oyS, zf), new Vector3(x, oyS, zEnd), new Vector3(x, oy0, zEnd), new Vector3(s, 0, 0));
        tun.Obiekt("Tunel: okładzina ścian i sklepienia", t, plytkiTunelu, false, false);
        var dr = new Bud(); var drA = new Bud(); var drK = new Bud();
        drA.MinMax(new Vector3(JezdniaX0, Pomost - .3f, zEnd), new Vector3(JezdniaX1, Pomost, zf));
        dr.MinMax(new Vector3(ox0, Pomost - .3f, zEnd), new Vector3(JezdniaX0 - .15f, Chodnik, zf));
        dr.MinMax(new Vector3(JezdniaX1 + .15f, Pomost - .3f, zEnd), new Vector3(ox1, Chodnik, zf));
        drK.MinMax(new Vector3(JezdniaX0 - .15f, Pomost - .3f, zEnd), new Vector3(JezdniaX0, Chodnik, zf));
        drK.MinMax(new Vector3(JezdniaX1, Pomost - .3f, zEnd), new Vector3(JezdniaX1 + .15f, Chodnik, zf));
        drA.Obiekt("Tunel: jezdnia", t, asfalt, false, false); dr.Obiekt("Tunel: chodniki", t, kostka, false, false); drK.Obiekt("Tunel: krawężniki", t, granit, false, false);
        BraxRegionMeble.Kolizja(t, "Tunel: jezdnia kolizja", new Vector3(OS, Pomost - .4f, (zf + zEnd) * .5f), new Vector3(JezdniaX1 - JezdniaX0, .8f, zf - zEnd));
        foreach (var (x0, x1) in new[] { (ox0, JezdniaX0), (JezdniaX1, ox1) })
            BraxRegionMeble.Kolizja(t, "Tunel: chodnik kolizja", new Vector3((x0 + x1) * .5f, Chodnik - .4f, (zf + zEnd) * .5f), new Vector3(x1 - x0, .8f, zf - zEnd));
        foreach (float x in new[] { ox0 - .2f, ox1 + .2f }) BraxRegionMeble.Kolizja(t, "Tunel: ściana kolizja", new Vector3(x, oy0 + 3.5f, (zf + zEnd) * .5f), new Vector3(.4f, 7f, zf - zEnd));
        ciem.MinMax(new Vector3(ox0, oy0, zEnd - .2f), new Vector3(ox1, oyK + .2f, zEnd));
        ciem.Obiekt("Tunel: głąb (dalej łuk w prawo — mrok)", t, ciemnosc, false, false);
        BraxRegionMeble.Kolizja(t, "Tunel: koniec kolizja", new Vector3(OS, oy0 + 3.5f, zEnd - .1f), new Vector3(ox1 - ox0, 7f, .2f));
        var opr = new Bud();
        for (float z = zf - 2f; z > zEnd + 1f; z -= 4f)
            foreach (float x in new[] { ox0 + .06f, ox1 - .06f }) opr.MinMax(new Vector3(x - .04f, oyS - .5f, z - .6f), new Vector3(x + .04f, oyS - .38f, z + .6f));
        opr.Obiekt("Tunel: oprawy LED", t, BraxRegionMeble.Swiecacy("Tunel: oprawa LED", new Color(1f, .93f, .78f), 1.6f), false, false);
        // szlaban na pomoście przed portalem (ramię przez jezdnię, słupek z napędem na chodniku wschodnim)
        float zs = PortalZ + 3.0f, xp = JezdniaX1 + .55f;
        var nap = new Bud(); var ramieB = new Bud(); var ramieC = new Bud();
        nap.MinMax(new Vector3(xp - .22f, Chodnik, zs - .2f), new Vector3(xp + .22f, Chodnik + 1.1f * k, zs + .2f));
        float ry = Chodnik + .95f * k, rx0 = JezdniaX0 + .3f, seg = (xp - .25f - rx0) / 10f;
        for (int i = 0; i < 10; i++) ((i % 2 == 0) ? ramieB : ramieC).MinMax(new Vector3(xp - .25f - (i + 1) * seg, ry - .05f, zs - .04f), new Vector3(xp - .25f - i * seg, ry + .05f, zs + .04f));
        ramieB.Cyl(new Vector3(rx0 + .2f, Pomost, zs), new Vector3(rx0 + .2f, ry - .05f, zs), .03f, 8);   // podpórka końca ramienia
        nap.Obiekt("Szlaban: napęd", t, BraxRegionMeble.Gladki("Szlaban: obudowa żółta", new Color(.93f, .70f, .08f), .5f, .1f));
        ramieB.Obiekt("Szlaban: ramię (białe)", t, BraxRegionMeble.Gladki("Szlaban: biały", new Color(.95f, .95f, .93f), .45f));
        ramieC.Obiekt("Szlaban: ramię (czerwone)", t, BraxRegionMeble.Gladki("Szlaban: czerwony", new Color(.80f, .06f, .05f), .45f));
        BraxRegionMeble.Kolizja(t, "Szlaban: kolizja", new Vector3((rx0 + xp) * .5f, ry - .3f, zs), new Vector3(xp - rx0, .7f, .12f));
        // napis OSIEDLE (litery na przyczółku nad łukiem) i znak drogowy przed szlabanem
        BraxRegionMeble.Napis(t, "OSIEDLE", new Vector3(OS, oyK + 1.9f, zf + .14f), Vector3.forward, 1.25f, new Color(.93f, .92f, .88f));
        BraxRegionMeble.Napis(t, "TUNEL · TUR POD ZAMOŚCIEM", new Vector3(OS, oyK + .95f, zf + .14f), Vector3.forward, .34f, new Color(.88f, .87f, .83f));
        var zn = new Bud(); var sl = new Bud(); float zx = X1 - .45f, zz = PortalZ + 9f, zh = 2.4f * k;
        sl.Cyl(new Vector3(zx, Chodnik, zz), new Vector3(zx, Chodnik + zh, zz), .035f * k, 10);
        zn.MinMax(new Vector3(zx - .9f * k, Chodnik + zh - .7f * k, zz + .04f), new Vector3(zx + .9f * k, Chodnik + zh, zz + .07f));
        sl.Obiekt("Znak OSIEDLE: słupek", t, BraxRegionMeble.StalSzara);
        zn.Obiekt("Znak OSIEDLE: tablica (E-17a)", t, BraxRegionMeble.Gladki("Znak: tablica zielona drogowskaz", new Color(.03f, .32f, .17f), .5f));
        BraxRegionMeble.Napis(t, "OSIEDLE ↑", new Vector3(zx, Chodnik + zh - .35f * k, zz + .08f), Vector3.forward, .26f, Color.white);
        pkt.Add(new BraxCityPoint { id = "R1_BRAMA_OSIEDLE", label = "Brama do osiedla (szlaban przed tunelem)", position = new Vector3(OS, Pomost, PortalZ + 5f) });
    }

    // ------------------------------------------------------------------ wzgórze naturalne (skarpa skalna + łąka) zamiast kopuły
    static void Wzgorze()
    {
        var t = new GameObject("Wzgórze nad tunelem (skarpa, łąka, drzewa)").transform; t.SetParent(root, false);
        const float gx0 = -118f, gx1 = -14f, gz0 = PortalZ, gz1 = -134f, step = 2f;
        int nx = Mathf.RoundToInt((gx1 - gx0) / step), nz = Mathf.RoundToInt((gz0 - gz1) / step);
        float H(float x, float z)
        {
            float s = PortalZ - z;                                                   // odległość w głąb (na południe)
            float lat = Mathf.Min(Mathf.InverseLerp(gx0, gx0 + 22f, x), Mathf.InverseLerp(gx1, gx1 - 22f, x));
            lat = lat * lat * (3f - 2f * lat);                                       // boki łagodnie schodzą do zera
            float n1 = Mathf.PerlinNoise(x * .045f + 3.1f, z * .045f + 7.3f), n2 = Mathf.PerlinNoise(x * .11f + 9.7f, z * .11f + 1.9f), n3 = Mathf.PerlinNoise(x * .27f + 5.5f, z * .27f + 2.2f);
            float skarpa = 21f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((s - .5f) / 9.5f)) * (.85f + .3f * n1);
            float stok = s > 10f ? .17f * (s - 10f) + 3.2f * (n1 - .5f) + 1.6f * (n2 - .5f) : 0f;
            float h = (skarpa + Mathf.Max(0f, stok) + .7f * (n3 - .5f) * Mathf.Clamp01(s / 4f)) * lat;
            // portal: masyw za przyczółkiem ≥ 20.3 m (nad sklepieniem tunelu 14.6 m i czapą przyczółka 19.95 m)
            float px = Mathf.Max(0f, Mathf.Abs(x - OS) - 11.9f), pw = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(px / 9f));
            if (s >= 1.6f) h = Mathf.Max(h, 20.3f * pw + (h - 20.3f * pw > 0 ? 0 : 0));
            if (s < 2.0f && Mathf.Abs(x - OS) < 11.6f) h = Mathf.Min(h, 19.4f);   // w obrysie przyczółka (schowane za murem)
            return h;
        }
        var V = new Vector3[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++) { float x = gx0 + i * step, z = gz0 - j * step; V[i, j] = new Vector3(x, H(x, z), z); }
        var trawa = new Bud(); var skala = new Bud();
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                Vector3 a = V[i, j], b = V[i + 1, j], c = V[i + 1, j + 1], d = V[i, j + 1];
                if (a.y < .05f && b.y < .05f && c.y < .05f && d.y < .05f) continue;   // płasko na zerze: nie zasłaniać posadzki (horyzont kładzie swój teren)
                var n = Vector3.Cross(d - a, b - a).normalized; if (n.y < 0) n = -n;
                ((n.y < .62f) ? skala : trawa).Quad(a, d, c, b);
            }
        var gt = trawa.Obiekt("Wzgórze: łąka", t, BraxRegionTrawa.Laka, true, false);
        if (gt) BraxRegionTrawa.Teren(gt.GetComponent<MeshRenderer>(), false, true);
        skala.Obiekt("Wzgórze: skarpa skalna", t, BraxRegionMeble.Tex("Wzgórze: skała wapienna", 3.0f, new Color(.86f, .84f, .80f), .92f, "rock_boulder_dry", "rock_face_03", "rock_wall_08"), true, false);
        Physics.SyncTransforms();
        // drzewa na łagodnej części, głazy u stóp skarpy, krzewy
        var rnd = new System.Random(7400);
        int drzew = 0, glazow = 0;
        for (int tries = 0; tries < 160 && drzew < 34; tries++)
        {
            float x = gx0 + 14f + (float)rnd.NextDouble() * (gx1 - gx0 - 28f), z = PortalZ - 14f - (float)rnd.NextDouble() * 52f;
            if (Mathf.Abs(x - OS) < 7f && z > PortalZ - 40f) continue;   // nad tunelem bez korzeni w sklepieniu (blisko portalu)
            RaycastHit h; if (!BraxRegion.Ground(x, z, 2f, 40f, out h) || h.normal.y < .85f) continue;
            if (BraxRegion.Put(t, BraxRegion.Model("island_tree_03", "pine_tree_01"), "Wzgórze: drzewo", h.point - Vector3.up * .15f, (float)rnd.NextDouble() * 360f, .9f + (float)rnd.NextDouble() * .3f, false, 15f)) drzew++;
        }
        for (int i = 0; i < 18; i++)
        {
            float x = Mathf.Lerp(-104f, -28f, i / 17f) + ((float)rnd.NextDouble() - .5f) * 3f, z = PortalZ - .5f - (float)rnd.NextDouble() * 1.5f;
            if (Mathf.Abs(x - OS) < 13f) continue;
            RaycastHit h; if (!BraxRegion.Ground(x, z, -1f, 6f, out h)) continue;
            if (BraxRegion.Put(t, BraxRegion.Model("boulder_01", "rock_moss_set_01", "stone_01"), "Wzgórze: głaz", h.point, (float)rnd.NextDouble() * 360f, 1f + (float)rnd.NextDouble() * .8f, true, 2.4f, .25f)) glazow++;
        }
        BraxRegion.Note("wzgórze: siatka " + nx + "×" + nz + " (2 m), drzew " + drzew + ", głazów " + glazow + " (kopuła z 13.1 usunięta — nie powstaje)");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — MARINA OD NOWA (przebudowa 2.11; zgłoszenia 690062d6 „zobacz, jak stoją ustawione jachty” — nachodzą na
// siebie i na pomost; eb4f459f „miało być ZEJŚCIE” — pomost kończy się barierką, łodzie w pomoście; audyt §13).
//
// USUNIĘTE przed budową: źródłowe molo drewniane (x 84…112, z 70…76: deski, pale, barierki, latarnia, polery, kolizje)
// i jego dodatki z „City finished outdoor details” (okucia, knagi, drabinka, koło ratunkowe); stara marina Claude 31.10
// (pomost na palach z=113…44, palce, stopnie z piasku, „granica kąpieliska” w poprzek zatoki) — kod skasowany.
//
// NOWE (wymiary rzeczywiste × k; woda y = −1.7):
//  • molo stałe z bulwaru (x 84…101, z 70…76) na palach betonowych, pokład z desek na poziomie bulwaru, balustrady,
//    głowica (x 101…106, z 66…80) z tablicą MARINA BRAXHORN, kołem ratunkowym, ławką i latarnią;
//  • TRAP (zejście) z głowicy na pomost pływający: 6,1 m, szer. 1,2 m × k, spadek 11°, listwy antypoślizgowe, poręcze 1,1 m × k
//    po obu stronach, zawias u góry, rolki na pomoście;
//  • pomost pływający (pływaki betonowe, pokład z desek 0,5 m × k nad wodą, listwa odbojowa), słupy prowadzące, knagi co 4 m,
//    słupki z prądem i wodą; od zachodu palce dla motorówek (cumowanie rufą), niski pomost dla skuterów wodnych;
//  • łodzie PRZY pomostach: 2 jachty burtą do pomostu (na odbijaczach, z cumami), 3 motorówki rufą do pomostu w boksach,
//    4 skutery rufą do niskiego pomostu — każda z miejscem wejścia (AKCJA „WSIĄDŹ”, BraxVessel), dziobem w stronę wolnej wody;
//  • bosmanat na bulwarze przy molo (okienko obsługi = miejsce zleceniodawcy), maszt z banderą.
public static class BraxNabrzezeMarina
{
    static float K => BraxNabrzeze.K;
    static float W => BraxNabrzeze.Woda;
    public static float Poklad => W + .5f * BraxNabrzeze.K;          // pomost pływający: 0,5 m × k nad wodą
    public static float PokladSkuter => W + .22f * BraxNabrzeze.K;   // niski pomost dla skuterów
    public const float MoloZ0 = 70f, MoloZ1 = 76f, MoloX1 = 101f, GlowX1 = 106f, GlowZ0 = 66f, GlowZ1 = 80f;
    public const float PomX0 = 112f, PomZ0 = 21f, PomZ1 = 82f;   // właściciel 11:18: marina z dala od plaży (kadłuby ≥ 35 m od linii brzegu)
    public static float PomX1 => PomX0 + 2.5f * BraxNabrzeze.K;
    public const float TrapZ = 73f;
    public static readonly Dictionary<string, Vector3> Miejsca = new Dictionary<string, Vector3>();

    public static void Buduj(Transform portRoot, Transform cityRoot)
    {
        Miejsca.Clear();
        var root = new GameObject("CLAUDE plaża: marina (od nowa)").transform;    // nazwa „CLAUDE plaża*” → obszar Nabrzeże
        root.SetParent(portRoot, false); root.position = Vector3.zero;

        // 1) ZDJĘCIE STAREGO
        var b = new Bounds(); b.SetMinMax(new Vector3(83.55f, -6f, 68.4f), new Vector3(115f, 9f, 77.6f));
        BraxNabrzeze.Wytnij(b, "molo_zrodlowe");
        var env = cityRoot ? cityRoot.Find("City finished outdoor details") : null;
        int envUsun = BraxNabrzeze.UsunObiekty(env, new Rect(83.5f, 66f, 32f, 14f), "Pier timber fastenings", "Pier mooring cleat", "Water access ladder", "Harbour life ring");

        var m = BraxNabrzezeMebel.Materialy();
        var deski = BraxNabrzeze.Mat("Marina: deski pokładu (modrzew szary)", 1.6f, new Color(.95f, .93f, .9f), "wood_planks_grey", "wooden_planks", "wood_planks");
        var pale = BraxNabrzeze.Mat("Marina: pale betonowe", 2f, new Color(.78f, .77f, .74f), "concrete_wall_008", "concrete_floor_02");
        var plywak = BraxNabrzeze.Kolor("Marina: pływak betonowy", new Color(.62f, .62f, .6f), .9f);
        var aluminium = BraxNabrzeze.Kolor("Marina: aluminium", new Color(.72f, .74f, .76f), .4f, .85f);
        var kol = new GameObject("Marina: kolizje").transform; kol.SetParent(root, false);
        var z = new BraxNabrzeze.Zbior();

        Molo(z, root, kol, m, deski, pale);
        Trap(z, kol, m, deski, aluminium);
        Pomosty(z, root, kol, m, deski, plywak, aluminium);
        PomostyDE(z, kol, m, deski, plywak, aluminium);
        Bosmanat(z, root, kol, m);
        int n = z.Emit(root, "Marina", false);
        int lodzie = Lodzie(portRoot, kol, m);
        BraxNabrzeze.Log("marina: usunięte dodatki starego mola " + envUsun + ", obiektów scalonych " + n + ", łodzi " + lodzie +
                         ", pokład pomostu y " + Poklad.ToString("F2") + ", trap z " + TrapZ);
    }

    // ---------------- molo stałe z bulwaru ----------------
    static void Molo(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m, Material deski, Material pale)
    {
        float y = BraxNabrzeze.Bulwar;
        // pokład: deski poprzeczne (szczeliny 8 mm) na legarach, czoło z deski okapowej
        void Poklad(float x0, float x1, float z0, float z1)
        {
            for (float x = x0; x < x1 - .01f; x += .16f * K)
                z[deski].BoxMM(new Vector3(x + .006f, y - .05f, z0), new Vector3(Mathf.Min(x + .16f * K, x1) - .006f, y, z1));
            z[deski].BoxMM(new Vector3(x0, y - .32f, z0 - .04f), new Vector3(x1, y - .02f, z0));          // deska czołowa
            z[deski].BoxMM(new Vector3(x0, y - .32f, z1), new Vector3(x1, y - .02f, z1 + .04f));
            z[m.stalCiemna].BoxMM(new Vector3(x0, y - .45f, z0 + .2f), new Vector3(x1, y - .05f, z0 + .45f));   // podciąg
            z[m.stalCiemna].BoxMM(new Vector3(x0, y - .45f, z1 - .45f), new Vector3(x1, y - .05f, z1 - .2f));
            BraxNabrzeze.KolMM(kol, "Molo pokład", new Vector3(x0, y - .45f, z0), new Vector3(x1, y, z1));
        }
        Poklad(BraxNabrzeze.Krawedz - .3f, MoloX1, MoloZ0, MoloZ1);
        Poklad(MoloX1, GlowX1, GlowZ0, GlowZ1);
        // pale Ø 0,40 m co 4 m w trzech rzędach (pod głowicą gęściej)
        for (float x = 87f; x <= MoloX1; x += 4f) foreach (float zz in new[] { MoloZ0 + .45f, (MoloZ0 + MoloZ1) * .5f, MoloZ1 - .45f })
            z[pale].Rura(new Vector3(x, -6f, zz), new Vector3(x, y - .45f, zz), .2f * K, 12);
        for (float x = MoloX1 + 1f; x <= GlowX1 - .3f; x += 2.2f) for (float zz = GlowZ0 + .45f; zz <= GlowZ1 - .4f; zz += 3.2f)
            z[pale].Rura(new Vector3(x, -6f, zz), new Vector3(x, y - .45f, zz), .2f * K, 12);
        // balustrady molo i głowicy (przerwa na trap po wschodniej stronie głowicy)
        float yb = y;
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(BraxNabrzeze.Krawedz + .3f, yb, MoloZ0 + .15f), new Vector3(MoloX1, yb, MoloZ0 + .15f));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(BraxNabrzeze.Krawedz + .3f, yb, MoloZ1 - .15f), new Vector3(MoloX1, yb, MoloZ1 - .15f));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MoloX1, yb, MoloZ0 + .15f), new Vector3(MoloX1, yb, GlowZ0 + .15f));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MoloX1, yb, MoloZ1 - .15f), new Vector3(MoloX1, yb, GlowZ1 - .15f));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MoloX1, yb, GlowZ0 + .15f), new Vector3(GlowX1 - .15f, yb, GlowZ0 + .15f));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(MoloX1, yb, GlowZ1 - .15f), new Vector3(GlowX1 - .15f, yb, GlowZ1 - .15f));
        float tw = 1.2f * K * .5f + .25f;
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(GlowX1 - .15f, yb, GlowZ0 + .15f), new Vector3(GlowX1 - .15f, yb, TrapZ - tw));
        BraxNabrzezeMebel.Balustrada(z, kol, m, new Vector3(GlowX1 - .15f, yb, TrapZ + tw), new Vector3(GlowX1 - .15f, yb, GlowZ1 - .15f));
        // wyposażenie głowicy
        BraxNabrzezeMebel.Tablica(z, root, m, new Vector3(103.5f, y, GlowZ1 - .9f), 180f, "MARINA BRAXHORN", "Bosmanat: bulwar przy molo · VHF kanał 9\nPomost A: jachty · B: motorówki · C: skutery", new Color(.06f, .17f, .30f), 2.0f);
        BraxNabrzezeMebel.KoloRatunkowe(z, root, m, new Vector3(GlowX1 - .6f, y, GlowZ0 + 1.2f), -90f);
        BraxNabrzezeMebel.Lawka(z, root, m, new Vector3(102.2f, y, GlowZ0 + 1.6f), 0f, "Ławka na molo");
        BraxNabrzezeMebel.Latarnia(z, root, m, new Vector3(MoloX1 + .6f, y, GlowZ1 - .8f), 0f);
        BraxNabrzezeMebel.Latarnia(z, root, m, new Vector3(92f, y, MoloZ1 - .6f), 180f);
        foreach (float x in new[] { 96f, 104f }) BraxNabrzezeMebel.Poler(z, root, m, new Vector3(x, y, MoloZ0 + .6f));
        Miejsca["marina_molo"] = new Vector3(92f, y, (MoloZ0 + MoloZ1) * .5f);
        Miejsca["marina_glowica"] = new Vector3(103.5f, y, TrapZ);
    }

    // ---------------- trap: z głowicy (y 0.15) na pomost pływający ----------------
    static void Trap(BraxNabrzeze.Zbior z, Transform kol, BraxNabrzezeMebel.Mat m, Material deski, Material alu)
    {
        var gora = new Vector3(GlowX1, BraxNabrzeze.Bulwar, TrapZ);
        var dol = new Vector3(PomX0 + .2f, Poklad, TrapZ);
        float szer = 1.2f * K, L1 = Vector3.Distance(gora, dol);
        var dir = (dol - gora).normalized; var rot = Quaternion.LookRotation(dir); var bok = rot * Vector3.right;
        var mid = (gora + dol) * .5f;
        z[alu].Box(mid + rot * new Vector3(0, -.05f, 0), new Vector3(szer, .06f, L1), rot);                 // płyta trapu
        for (float s = .2f; s < L1 - .1f; s += .35f)                                                         // listwy antypoślizgowe
            z[deski].Box(gora + dir * s + rot * new Vector3(0, -.005f, 0), new Vector3(szer - .1f, .03f, .06f), rot);
        foreach (float sg in new[] { -1f, 1f })
        {
            var a = gora + bok * (szer * .5f + .03f) * sg; var b = dol + bok * (szer * .5f + .03f) * sg;
            z[alu].Box((a + b) * .5f + rot * new Vector3(0, .14f, 0), new Vector3(.05f, .36f, L1), rot);   // dźwigar boczny (kratownica)
            for (float s = 0; s <= L1; s += .7f) { var p = Vector3.Lerp(a, b, s / L1); z[alu].Rura(p + Vector3.up * .3f, p + Vector3.up * 1.1f * K, .02f * K, 8); }
            z[alu].Rura(a + Vector3.up * 1.1f * K, b + Vector3.up * 1.1f * K, .025f * K, 10);
            z[alu].Rura(a + Vector3.up * .6f * K, b + Vector3.up * .6f * K, .015f * K, 8);
            z[m.guma].Rura(b + bok * -.15f * sg + Vector3.down * .02f, b + bok * -.4f * sg + Vector3.down * .02f, .08f, 10);   // rolki
        }
        z[m.stalCiemna].Rura(gora + bok * (szer * .55f), gora - bok * (szer * .55f), .05f, 10);   // zawias
        BraxNabrzeze.KolRot(kol, "Trap", mid + rot * new Vector3(0, -.12f, 0), new Vector3(szer, .2f, L1 + .3f), rot);
        foreach (float sg in new[] { -1f, 1f })
            BraxNabrzeze.KolRot(kol, "Trap poręcz", mid + bok * (szer * .5f + .12f) * sg + Vector3.up * 1f, new Vector3(.15f, 2f, L1), rot);
        Miejsca["marina_trap_gora"] = gora + Vector3.left * 1f;
        Miejsca["marina_trap_dol"] = new Vector3(PomX0 + 1.4f, Poklad, TrapZ);
    }

    // ---------------- pomosty pływające ----------------
    static void Pomost(BraxNabrzeze.Zbior z, Transform kol, BraxNabrzezeMebel.Mat m, Material deski, Material plywak, float x0, float x1, float z0, float z1, float pok, bool knagi = true)
    {
        bool wzdluzZ = (z1 - z0) > (x1 - x0);
        // pływaki betonowe (zanurzone do 0,35 m pod wodę) i pokład z desek układanych w poprzek
        z[plywak].BoxMM(new Vector3(x0 + .05f, W - .35f, z0 + .05f), new Vector3(x1 - .05f, pok - .08f, z1 - .05f));
        if (wzdluzZ) for (float zz = z0; zz < z1 - .01f; zz += .15f * K) z[deski].BoxMM(new Vector3(x0, pok - .08f, zz + .005f), new Vector3(x1, pok, Mathf.Min(zz + .15f * K, z1) - .005f));
        else for (float x = x0; x < x1 - .01f; x += .15f * K) z[deski].BoxMM(new Vector3(x + .005f, pok - .08f, z0), new Vector3(Mathf.Min(x + .15f * K, x1) - .005f, pok, z1));
        // listwa odbojowa (guma) dookoła
        z[m.guma].BoxMM(new Vector3(x0 - .06f, pok - .3f, z0 - .06f), new Vector3(x1 + .06f, pok - .12f, z0 + .02f));
        z[m.guma].BoxMM(new Vector3(x0 - .06f, pok - .3f, z1 - .02f), new Vector3(x1 + .06f, pok - .12f, z1 + .06f));
        z[m.guma].BoxMM(new Vector3(x0 - .06f, pok - .3f, z0), new Vector3(x0 + .02f, pok - .12f, z1));
        z[m.guma].BoxMM(new Vector3(x1 - .02f, pok - .3f, z0), new Vector3(x1 + .06f, pok - .12f, z1));
        BraxNabrzeze.KolMM(kol, "Pomost", new Vector3(x0, W - .4f, z0), new Vector3(x1, pok, z1));
        if (!knagi) return;
        if (wzdluzZ) for (float zz = z0 + 1.5f; zz < z1 - 1f; zz += 4f) { BraxNabrzezeMebel.Knaga(z, m, new Vector3(x0 + .22f, pok, zz), 0f); BraxNabrzezeMebel.Knaga(z, m, new Vector3(x1 - .22f, pok, zz), 0f); }
        else for (float x = x0 + 1.2f; x < x1 - .8f; x += 3f) { BraxNabrzezeMebel.Knaga(z, m, new Vector3(x, pok, z0 + .22f), 90f); BraxNabrzezeMebel.Knaga(z, m, new Vector3(x, pok, z1 - .22f), 90f); }
    }

    /// <summary>Niewidzialna krawędź pomostu (nie spada się do wody; wsiadanie przyciskiem AKCJA).</summary>
    static void Krawedz(Transform kol, Vector3 a, Vector3 b, float pok)
    {
        var c = (a + b) * .5f; float len = Vector3.Distance(a, b); if (len < .3f) return;
        float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
        BraxNabrzeze.Kol(kol, "Krawędź pomostu", new Vector3(c.x, pok + 1.2f, c.z), new Vector3(.2f, 2.4f, len), yaw);
    }

    static readonly List<(float z0, float z1)> palceZach = new List<(float, float)>();
    static void Pomosty(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m, Material deski, Material plywak, Material alu)
    {
        float pok = Poklad, x1 = PomX1;
        Pomost(z, kol, m, deski, plywak, PomX0, x1, PomZ0, PomZ1, pok);
        // palce zachodnie dla motorówek (boksy 3,65 m, cumowanie rufą do pomostu głównego)
        palceZach.Clear();
        float fw = 1.2f * K, fl = 8.5f;
        foreach (float zz in new[] { 30f, 35.2f, 40.4f, 45.6f })
        {
            Pomost(z, kol, m, deski, plywak, PomX0 - fl, PomX0, zz - fw * .5f, zz + fw * .5f, pok);
            palceZach.Add((zz - fw * .5f, zz + fw * .5f));
            z[alu].Rura(new Vector3(PomX0 - fl + .3f, -5f, zz), new Vector3(PomX0 - fl + .3f, pok + 2.2f * K, zz), .16f, 12);   // słup prowadzący
        }
        // niski pomost dla skuterów wodnych (C): x 106.5…112, z 52…65.5, 2 stopnie z pomostu głównego
        float sx0 = 106.5f, sz0 = 52f, sz1 = 65.5f, ps = PokladSkuter;
        Pomost(z, kol, m, deski, plywak, sx0, PomX0, sz0, sz1, ps, false);
        for (int i = 0; i < 2; i++)
        {
            float top = pok - (pok - ps) * (i + 1) / 3f, xx = PomX0 - .25f - i * .3f;
            z[deski].BoxMM(new Vector3(xx - .3f, ps - .1f, 57.2f), new Vector3(xx, top, 60.2f));
            BraxNabrzeze.KolMM(kol, "Stopień pomostu skuterów", new Vector3(xx - .3f, ps - .1f, 57.2f), new Vector3(xx, top, 60.2f));
        }
        // słupy prowadzące pomostu głównego i słupki z prądem/wodą
        for (float zz = PomZ0 + 2f; zz < PomZ1; zz += 15f)
        {
            z[alu].Rura(new Vector3(x1 - .3f, -5f, zz), new Vector3(x1 - .3f, pok + 2.4f * K, zz), .18f, 12);
            z[alu].Rura(new Vector3(x1 - .3f, pok + 2.4f * K, zz), new Vector3(x1 - .3f, pok + 2.48f * K, zz), .2f, 12, .05f);
        }
        for (float zz = PomZ0 + 8f; zz < PomZ1 - 2f; zz += 12f)
        {
            var p = new Vector3((PomX0 + x1) * .5f, pok, zz);
            z[m.bialy].BoxMM(p + new Vector3(-.15f, 0, -.15f) * K, p + new Vector3(.15f * K, 1.0f * K, .15f * K));
            z[m.zielen].BoxMM(p + new Vector3(-.17f * K, 1.0f * K, -.17f * K), p + new Vector3(.17f * K, 1.08f * K, .17f * K));
            BraxNabrzeze.Kol(kol, "Słupek energetyczny", p + Vector3.up * .6f, new Vector3(.4f, 1.2f, .4f) * K);
        }
        // niewidzialne krawędzie: wschód (przy jachtach), zachód między palcami, końce; skutery — krawędź zachodnia
        Krawedz(kol, new Vector3(x1 + .1f, 0, PomZ0), new Vector3(x1 + .1f, 0, PomZ1), pok);
        Krawedz(kol, new Vector3(PomX0, 0, PomZ0 - .1f), new Vector3(x1, 0, PomZ0 - .1f), pok);
        // (północny koniec pomostu A przechodzi w pomost poprzeczny E — bez krawędzi)
        float zc = PomZ0;
        var ciecia = new List<(float, float)>(palceZach) { (TrapZ - 1.2f, TrapZ + 1.2f), (sz0, sz1) };
        ciecia.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        foreach (var c in ciecia) { Krawedz(kol, new Vector3(PomX0 - .1f, 0, zc), new Vector3(PomX0 - .1f, 0, c.Item1), pok); zc = c.Item2; }
        Krawedz(kol, new Vector3(PomX0 - .1f, 0, zc), new Vector3(PomX0 - .1f, 0, PomZ1), pok);
        foreach (var f in palceZach)
        {
            Krawedz(kol, new Vector3(PomX0 - fl, 0, f.z0 - .1f), new Vector3(PomX0, 0, f.z0 - .1f), pok);
            Krawedz(kol, new Vector3(PomX0 - fl, 0, f.z1 + .1f), new Vector3(PomX0, 0, f.z1 + .1f), pok);
            Krawedz(kol, new Vector3(PomX0 - fl - .1f, 0, f.z0), new Vector3(PomX0 - fl - .1f, 0, f.z1), pok);
        }
        Krawedz(kol, new Vector3(sx0 - .1f, 0, sz0), new Vector3(sx0 - .1f, 0, sz1), ps);
        Krawedz(kol, new Vector3(sx0, 0, sz0 - .1f), new Vector3(PomX0, 0, sz0 - .1f), ps);
        Krawedz(kol, new Vector3(sx0, 0, sz1 + .1f), new Vector3(PomX0, 0, sz1 + .1f), ps);
        BraxNabrzezeMebel.KoloRatunkowe(z, root, m, new Vector3(x1 - .5f, pok, TrapZ + 3f), 90f);
        BraxNabrzezeMebel.Tablica(z, root, m, new Vector3(PomX0 + .5f, pok, TrapZ - 2.2f), -90f, "POMOST A · B · C", "A: jachty (wschód) · B: motorówki (zachód, południe)\nC: skutery wodne (zachód, przy trapie)", new Color(.06f, .17f, .30f), 1.4f);
        Miejsca["marina_pomost_jachty"] = new Vector3(x1 - .9f, pok, 62f);
        Miejsca["marina_pomost_motorowki"] = new Vector3(PomX0 + .8f, pok, 38f);
        Miejsca["marina_pomost_skutery"] = new Vector3(sx0 + 1.2f, ps, 58.6f);
    }

    // ---------------- A 2.11: pomost poprzeczny E i drugi pomost pływający D (jachty w rzędach jak w prawdziwej marinie) ----------------
    public const float DX0 = 136f;
    public static float DX1 => DX0 + 2.5f * BraxNabrzeze.K;
    static void PomostyDE(BraxNabrzeze.Zbior z, Transform kol, BraxNabrzezeMebel.Mat m, Material deski, Material plywak, Material alu)
    {
        float pok = Poklad, ez1 = PomZ1 + 2.6f;
        Pomost(z, kol, m, deski, plywak, PomX0, DX1, PomZ1, ez1, pok);         // E: od pomostu A do D (wzdłuż x)
        Pomost(z, kol, m, deski, plywak, DX0, DX1, PomZ0, PomZ1, pok);          // D: równoległy do A
        for (float zz = PomZ0 + 2f; zz < PomZ1; zz += 15f) z[alu].Rura(new Vector3(DX1 - .3f, -5f, zz), new Vector3(DX1 - .3f, pok + 2.4f * K, zz), .18f, 12);
        Krawedz(kol, new Vector3(PomX0, 0, ez1 + .1f), new Vector3(DX1, 0, ez1 + .1f), pok);
        Krawedz(kol, new Vector3(PomX1, 0, PomZ1 - .1f), new Vector3(DX0, 0, PomZ1 - .1f), pok);
        Krawedz(kol, new Vector3(PomX0 - .1f, 0, PomZ1), new Vector3(PomX0 - .1f, 0, ez1), pok);
        Krawedz(kol, new Vector3(DX1 + .1f, 0, PomZ0), new Vector3(DX1 + .1f, 0, ez1), pok);
        Krawedz(kol, new Vector3(DX0 - .1f, 0, PomZ0), new Vector3(DX0 - .1f, 0, PomZ1), pok);
        Krawedz(kol, new Vector3(DX0, 0, PomZ0 - .1f), new Vector3(DX1, 0, PomZ0 - .1f), pok);
        for (float zz = PomZ0 + 8f; zz < PomZ1 - 2f; zz += 12f)
        {
            var p = new Vector3((DX0 + DX1) * .5f, pok, zz);
            z[m.bialy].BoxMM(p + new Vector3(-.15f, 0, -.15f) * K, p + new Vector3(.15f * K, 1.0f * K, .15f * K));
            z[m.zielen].BoxMM(p + new Vector3(-.17f * K, 1.0f * K, -.17f * K), p + new Vector3(.17f * K, 1.08f * K, .17f * K));
            BraxNabrzeze.Kol(kol, "Słupek energetyczny", p + Vector3.up * .6f, new Vector3(.4f, 1.2f, .4f) * K);
        }
        Miejsca["marina_pomost_D"] = new Vector3((DX0 + DX1) * .5f, pok, 50f);
    }

    // ---------------- bosmanat na bulwarze (x 66.5…72.5, z 61.5…68) ----------------
    static void Bosmanat(BraxNabrzeze.Zbior z, Transform root, Transform kol, BraxNabrzezeMebel.Mat m)
    {
        float y = BraxNabrzeze.Bulwar, x0 = 66.6f, x1 = 72.4f, z0 = 61.6f, z1 = 68f, h = 3.0f * K;
        var deskowanie = BraxNabrzeze.Mat("Bosmanat: deskowanie modrzewiowe", 1.4f, new Color(.86f, .82f, .76f), "wood_plank_wall", "wood_planks_grey", "wooden_planks");
        var tynk = BraxNabrzeze.Mat("Bosmanat: tynk biały", 2f, new Color(.95f, .95f, .93f), "white_plaster_02", "concrete_wall_008");
        // cokół, ściany (deskowanie z boków i z tyłu, front od morza: przeszklenie z okienkiem obsługi i drzwi)
        z[m.granit].BoxMM(new Vector3(x0, y, z0), new Vector3(x1, y + .3f, z1));
        z[deskowanie].BoxMM(new Vector3(x0, y + .3f, z0), new Vector3(x0 + .25f, y + h, z1));          // zachód (od miasta)
        z[deskowanie].BoxMM(new Vector3(x0, y + .3f, z0), new Vector3(x1, y + h, z0 + .25f));          // południe
        z[tynk].BoxMM(new Vector3(x0, y + .3f, z1 - .25f), new Vector3(x1 - 1.6f * K, y + h, z1));      // północ (od mola) z drzwiami
        z[m.stalCiemna].BoxMM(new Vector3(x1 - 1.6f * K, y + .3f, z1 - .27f), new Vector3(x1 - .5f * K, y + 2.1f * K, z1 - .2f));   // drzwi (zamknięte: personel)
        z[tynk].BoxMM(new Vector3(x1 - .5f * K, y + .3f, z1 - .25f), new Vector3(x1, y + h, z1));
        z[tynk].BoxMM(new Vector3(x1 - 1.6f * K, y + 2.1f * K, z1 - .25f), new Vector3(x1 - .5f * K, y + h, z1));
        // front (wschód): parapet z ladą obsługi, szyba, okienko
        z[tynk].BoxMM(new Vector3(x1 - .25f, y + .3f, z0), new Vector3(x1, y + 1.05f * K, z1));
        z[m.drewno].BoxMM(new Vector3(x1 - .1f, y + 1.05f * K, z0 + .4f), new Vector3(x1 + .45f * K, y + 1.1f * K, z1 - .4f));     // lada zewnętrzna
        z[m.szklo].BoxMM(new Vector3(x1 - .18f, y + 1.1f * K, z0 + .25f), new Vector3(x1 - .14f, y + h - .3f, z1 - .25f));
        z[m.stalCiemna].BoxMM(new Vector3(x1 - .2f, y + h - .3f, z0), new Vector3(x1, y + h, z1));
        for (float zz = z0 + .25f; zz < z1; zz += 1.6f) z[m.stalCiemna].BoxMM(new Vector3(x1 - .2f, y + 1.05f * K, zz - .04f), new Vector3(x1 - .1f, y + h - .3f, zz + .04f));
        // wnętrze widoczne przez szybę: biurko, mapy na ścianie, radio VHF
        z[m.drewno].BoxMM(new Vector3(x0 + .6f, y + .3f, z0 + .6f), new Vector3(x0 + 1.4f, y + .75f * K, z1 - .6f));
        z[BraxNabrzeze.Kolor("Bosmanat: mapa morska", new Color(.75f, .82f, .86f), .9f)].BoxMM(new Vector3(x0 + .26f, y + 1.3f * K, z0 + 1.2f), new Vector3(x0 + .28f, y + 2.0f * K, z1 - 1.2f));
        z[m.stalCiemna].BoxMM(new Vector3(x0 + .8f, y + .75f * K, z0 + 2f), new Vector3(x0 + 1.2f, y + .9f * K, z0 + 2.5f));
        z[tynk].BoxMM(new Vector3(x0 + .25f, y + .02f, z0 + .25f), new Vector3(x1 - .25f, y + .3f, z1 - .25f));
        // dach płaski z okapem 0,6 m, attyka
        z[m.stalCiemna].BoxMM(new Vector3(x0 - .6f, y + h, z0 - .6f), new Vector3(x1 + .9f, y + h + .22f, z1 + .6f));
        // szyld i maszt z banderą
        BraxNabrzeze.Napis(root, "BOSMANAT", new Vector3(x1 + .92f, y + h + .11f, (z0 + z1) * .5f), 90f, .26f * K, new Color(.95f, .95f, .92f));
        BraxNabrzeze.Napis(root, "MARINA BRAXHORN · OBSŁUGA", new Vector3(x1 + .02f, y + .75f * K, (z0 + z1) * .5f), 90f, .1f * K, new Color(.12f, .14f, .16f));
        var maszt = new Vector3(x1 + 1.6f, y, z0 + .8f);
        z[m.bialy].Rura(maszt, maszt + Vector3.up * 7f * K, .05f * K, 10, .03f * K);
        var fl = maszt + Vector3.up * 6.4f * K;
        z[BraxNabrzeze.Kolor("Bandera: biel", new Color(.92f, .92f, .9f), .8f)].Quad(fl, fl + new Vector3(0, 0, 1.3f * K), fl + new Vector3(0, -.4f * K, 1.3f * K), fl + new Vector3(0, -.4f * K, 0));
        z[BraxNabrzeze.Kolor("Bandera: czerwień", new Color(.8f, .1f, .12f), .8f)].Quad(fl + new Vector3(0, -.4f * K, 0), fl + new Vector3(0, -.4f * K, 1.3f * K), fl + new Vector3(0, -.8f * K, 1.3f * K), fl + new Vector3(0, -.8f * K, 0));
        BraxNabrzeze.Kol(kol, "Maszt bandery", maszt + Vector3.up * 2f, new Vector3(.25f, 4f, .25f));
        BraxNabrzeze.KolMM(kol, "Bosmanat", new Vector3(x0, y, z0), new Vector3(x1, y + h, z1));
        BraxNabrzeze.KolMM(kol, "Bosmanat lada", new Vector3(x1, y, z0 + .4f), new Vector3(x1 + .45f * K, y + 1.1f * K, z1 - .4f));
        Miejsca["bosmanat_okienko"] = new Vector3(x1 + 1.1f * K, y, (z0 + z1) * .5f);   // miejsce zleceniodawcy (bosman w okienku)
    }

    // ---------------- łodzie przy pomostach ----------------
    struct Typ { public string plik, label; public float dlug, zanurzenie, vmax, skret, przysp; public bool siedzi; }
    static readonly Typ Jacht = new Typ { plik = "yacht.glb", label = "JACHT", dlug = 20f, zanurzenie = .11f, vmax = 7.5f, skret = 22f, przysp = 1.6f, siedzi = true };   // A 2.11: „płynę SIEDZĄC jak człowiek” — fotel sternika
    static readonly Typ Riva = new Typ { plik = "bateau_riva_boat.glb", label = "MOTORÓWKA", dlug = 8.2f, zanurzenie = .21f, vmax = 13f, skret = 45f, przysp = 3.5f, siedzi = true };
    static readonly Typ Skuter = new Typ { plik = "jetski_01.glb", label = "SKUTER", dlug = 3.3f, zanurzenie = .17f, vmax = 17f, skret = 95f, przysp = 6.5f, siedzi = true };

    static int Lodzie(Transform portRoot, Transform kol, BraxNabrzezeMebel.Mat m)
    {
        int n = 0; var z = new BraxNabrzeze.Zbior();
        var cumy = BraxNabrzeze.Kolor("Marina: cuma (lina poliestrowa)", new Color(.88f, .87f, .82f), .9f);
        var odbij = BraxNabrzeze.Kolor("Marina: odbijacz", new Color(.08f, .16f, .32f), .5f);
        float x1 = PomX1, pok = Poklad;
        // A: jachty burtą do pomostu (wschód), dziobem na południe
        foreach (float zc in new[] { 62f, 35f })
        {
            var v = Lodz(portRoot, Jacht, "Marina: jacht " + (n + 1), new Vector3(0, 0, zc), 180f, burtaX: x1 + .45f, zachod: false);
            if (v) { n++; Cumy(z, cumy, odbij, v, new Vector3(x1, pok, zc), true); }
        }
        // D: jachty po obu stronach pomostu D (burtą, dziobem na południe — wypływają na otwartą wodę ku wyspie)
        foreach (float zc in new[] { 64f, 36f })
        {
            var vz = Lodz(portRoot, Jacht, "Marina: jacht " + (n + 1), new Vector3(0, 0, zc), 180f, burtaX: DX0 - .45f, zachod: true);
            if (vz) { n++; Cumy(z, cumy, odbij, vz, new Vector3(DX0, pok, zc), true); }
            var vw = Lodz(portRoot, Jacht, "Marina: jacht " + (n + 1), new Vector3(0, 0, zc - 3f), 180f, burtaX: DX1 + .45f, zachod: false);
            if (vw) { n++; Cumy(z, cumy, odbij, vw, new Vector3(DX1, pok, zc - 3f), true); }
        }
        // motorówka do rejsu na wyspę — burtą do pomostu poprzecznego E (od północy), dziobem na wschód (prosto na wolną wodę)
        {
            var vr = Lodz(portRoot, Riva, "Marina: motorówka (rejs na wyspę)", new Vector3(125f, 0, PomZ1 + 2.6f + .45f + 1.65f), 90f);
            if (vr) { n++; vr.wejscie = vr.transform.InverseTransformPoint(new Vector3(125f, W, PomZ1 + 2.0f)); Cumy(z, cumy, odbij, vr, new Vector3(125f, pok, PomZ1 + 2.6f), true); }
        }
        // B: motorówki rufą do pomostu głównego, w boksach między palcami, dziobem na zachód
        float[] boksy = { 32.6f, 37.8f, 43.0f };
        int k = 0;
        foreach (float zc in boksy)
        {
            var v = Lodz(portRoot, Riva, "Marina: motorówka " + (++k), new Vector3(0, 0, zc), -90f, rufaX: PomX0 - .5f);
            if (v) { n++; Cumy(z, cumy, odbij, v, new Vector3(PomX0, pok, zc), false); }
        }
        // C: skutery rufą do niskiego pomostu, dziobem na zachód (wypływają prosto na wolną wodę)
        k = 0;
        foreach (float zc in new[] { 53.8f, 57.0f, 60.2f, 63.4f })
        {
            var t = Skuter; t.plik = (k % 2 == 0) ? "jetski_01.glb" : "jetski_02.glb";
            var v = Lodz(portRoot, t, "Marina: skuter wodny " + (++k), new Vector3(0, 0, zc), -90f, rufaX: 106.5f - .35f);
            if (v) n++;
        }
        z.Emit(kol.parent, "Marina cumy", false);
        return n;
    }

    /// <summary>Model łodzi w skali × k, dziobem w kierunku yaw (0 = +z), w wodzie na swoim zanurzeniu. burtaX: burta przy x
    /// (jacht przy pomoście od wschodu); rufaX: rufa przy x (łódź dziobem na zachód). Zwraca BraxVessel.</summary>
    static BraxVessel Lodz(Transform portRoot, Typ t, string name, Vector3 c, float yaw, float burtaX = float.NaN, bool zachod = true, float rufaX = float.NaN)
    {
        var model = BraxHarborBeachBuild.Model(t.plik);
        if (!model) { BraxNabrzeze.Log("brak modelu " + t.plik); return null; }
        var go = (GameObject)UnityEngine.Object.Instantiate(model);
        go.name = name + " (model)";
        // skala: długość rzeczywista × k
        var b = BraxRegion.Bounds(go);
        if (b.size.x > b.size.z) { go.transform.RotateAround(b.center, Vector3.up, 90f); b = BraxRegion.Bounds(go); }
        float s = t.dlug * K / Mathf.Max(.1f, b.size.z);
        go.transform.localScale *= s; b = BraxRegion.Bounds(go);
        // dziób = węższy koniec kadłuba przy linii wody → obracamy tak, by dziób był na +Z (lokalnie)
        if (DziobNaMinusZ(go, b)) { go.transform.RotateAround(b.center, Vector3.up, 180f); b = BraxRegion.Bounds(go); }
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(BraxRegion.CityMaterial).ToArray();
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        }
        // pojazd: korzeń na lustrze wody w środku kadłuba, model jako dziecko (lokalnie: dziób +Z)
        float zan = b.size.y * t.zanurzenie;
        var v = new GameObject(name); v.transform.SetParent(portRoot, false);
        v.transform.position = new Vector3(b.center.x, W, b.center.z);
        go.transform.SetParent(v.transform, true);
        go.transform.position += new Vector3(0, W - zan - b.min.y, 0);
        // pozycja i kierunek w marinie
        v.transform.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Inverse(Quaternion.identity);
        var bw = BraxRegion.Bounds(go);
        float pol = t.dlug * K * .5f, szer = Mathf.Min(bw.size.x, bw.size.z);
        Vector3 cel = c;
        if (!float.IsNaN(burtaX)) cel.x = zachod && burtaX > PomX1 + 1f ? burtaX - szer * .5f - .05f : burtaX + szer * .5f + .05f;   // burta przy pomoście (odbijacze 0,35 m); A 2.11: łódź na zachód od pomostu D
        if (!float.IsNaN(rufaX)) cel.x = rufaX - pol;                              // rufa przy pomoście, dziób na zachód
        v.transform.position = new Vector3(cel.x, W, cel.z);
        var vessel = v.AddComponent<BraxVessel>();
        vessel.label = t.label; vessel.waterY = W; vessel.draft = 0f;
        vessel.maxSpeed = t.vmax; vessel.turnRate = t.skret; vessel.accel = t.przysp;
        vessel.length = t.dlug * K; vessel.width = szer;
        vessel.sit = t.siedzi;
        // miejsce gracza: kokpit / siodło — promień w dół na tymczasowe kolizje modelu
        vessel.seat = Siedzenie(go, v.transform, t);
        // miejsce wejścia (AKCJA „WSIĄDŹ”): przy burcie od pomostu (jacht) albo przy rufie (motorówka, skuter)
        vessel.wejscie = !float.IsNaN(burtaX) ? v.transform.InverseTransformPoint(new Vector3(zachod && burtaX > PomX1 + 1f ? burtaX + .9f : burtaX - .9f, W, cel.z - pol * .25f))
                                              : new Vector3(0, 0, -pol - .9f);
        vessel.moor = v.transform.position; vessel.moorYaw = yaw;
        Fale(v.transform, vessel);
        return vessel;
    }

    /// <summary>Dziób = koniec kadłuba, który przy linii wody jest węższy (porównanie szerokości w 15 % długości od każdego końca).</summary>
    static bool DziobNaMinusZ(GameObject go, Bounds b)
    {
        float yCut = b.min.y + b.size.y * .35f, zA = b.min.z + b.size.z * .15f, zB = b.max.z - b.size.z * .15f;
        float minA = 1e9f, maxA = -1e9f, minB = 1e9f, maxB = -1e9f;
        foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = f.sharedMesh; if (!mesh) continue;
            Vector3[] vs; try { vs = mesh.vertices; } catch { continue; }
            var M = f.transform.localToWorldMatrix;
            for (int i = 0; i < vs.Length; i += Mathf.Max(1, vs.Length / 4000))
            {
                var p = M.MultiplyPoint3x4(vs[i]); if (p.y > yCut) continue;
                if (p.z < zA) { minA = Mathf.Min(minA, p.x); maxA = Mathf.Max(maxA, p.x); }
                else if (p.z > zB) { minB = Mathf.Min(minB, p.x); maxB = Mathf.Max(maxB, p.x); }
            }
        }
        float wA = maxA - minA, wB = maxB - minB;
        if (wA <= 0 || wB <= 0) return false;
        return wA < wB;
    }

    static Vector3 Siedzenie(GameObject model, Transform v, Typ t)
    {
        var tmp = new List<MeshCollider>();
        foreach (var f in model.GetComponentsInChildren<MeshFilter>(true)) { if (!f.sharedMesh) continue; var c = f.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = f.sharedMesh; tmp.Add(c); }
        Physics.SyncTransforms();
        float L1 = t.dlug * K;
        float zl = t.label == "JACHT" ? -.36f * L1 : t.label == "MOTORÓWKA" ? -.08f * L1 : -.12f * L1;
        var p = v.TransformPoint(new Vector3(0, 0, zl));
        float y = float.NaN;
        var hits = Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 40f, ~0, QueryTriggerInteraction.Collide);
        var nasze = hits.Where(h => tmp.Contains(h.collider as MeshCollider) && h.point.y > W + .25f).Select(h => h.point.y).OrderBy(q => q).ToList();
        if (nasze.Count > 0) y = t.label == "SKUTER" ? nasze.Last() : nasze.First();
        foreach (var c in tmp) UnityEngine.Object.DestroyImmediate(c);
        if (float.IsNaN(y)) y = W + (t.label == "JACHT" ? 1.6f : t.label == "MOTORÓWKA" ? .9f : .8f) * K;
        float ly = y - W + (t.label == "MOTORÓWKA" ? .42f * K : t.label == "JACHT" ? .45f * K : 0f);   // motorówka / jacht: siedzisko (fotel sternika) nad podłogą kokpitu
        return new Vector3(0, ly, zl);
    }

    /// <summary>Fala dziobowa i ślad za rufą (siatki z pianą; siłę ustawia BraxVessel wg prędkości) — bez cząsteczek.</summary>
    static Material pianaLodz;
    static void Fale(Transform v, BraxVessel vessel)
    {
        if (!pianaLodz) pianaLodz = BraxNabrzeze.Piana("Nabrzeże: piana łodzi", true);
        float L1 = vessel.length, B = vessel.width;
        // fala dziobowa: dwa ramiona „V” od dziobu do tyłu na zewnątrz
        var s = new BraxNabrzeze.Siatka();
        foreach (float sg in new[] { -1f, 1f })
        {
            var a0 = new Vector3(0, .03f, L1 * .5f); var a1 = new Vector3(sg * B * .5f, .03f, L1 * .32f);
            var b0 = new Vector3(sg * B * 1.6f, .03f, -L1 * .1f); var b1 = new Vector3(sg * B * 2.6f, .03f, -L1 * .45f);
            int s0 = s.v.Count;
            s.v.AddRange(new[] { a0, a1, b0, b1 }); s.n.AddRange(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            s.uv.AddRange(new[] { new Vector2(.5f, 0), new Vector2(1f, .1f), new Vector2(.2f, .7f), new Vector2(.8f, 1f) });
            s.c.AddRange(new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, .8f), new Color(1, 1, 1, .5f), new Color(1, 1, 1, 0) });
            s.t.AddRange(new[] { s0, s0 + 1, s0 + 2, s0 + 1, s0 + 3, s0 + 2 });
        }
        var bow = s.Zbuduj("Fala dziobowa " + v.name, false);
        // ślad za rufą: pas od rufy do 4 długości za łodzią
        var w = new BraxNabrzeze.Siatka();
        int segs = 8;
        for (int i = 0; i <= segs; i++)
        {
            float f = (float)i / segs, zz = -L1 * .5f - f * L1 * 3.5f, half = B * (.45f + f * 1.4f);
            w.v.Add(new Vector3(-half, .025f, zz)); w.v.Add(new Vector3(half, .025f, zz));
            w.n.Add(Vector3.up); w.n.Add(Vector3.up);
            w.uv.Add(new Vector2(0, f * 3f)); w.uv.Add(new Vector2(1, f * 3f));
            w.c.Add(new Color(1, 1, 1, 1 - f)); w.c.Add(new Color(1, 1, 1, 1 - f));
            if (i > 0) { int a = (i - 1) * 2; w.t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
        }
        var wake = w.Zbuduj("Ślad za rufą " + v.name, false);
        var rs = new List<Renderer>();
        foreach (var (mesh, nazwa) in new[] { (bow, "fala dziobowa"), (wake, "ślad za rufą") })
        {
            var g = BraxNabrzeze.Obiekt(v, v.name + ": " + nazwa, mesh, pianaLodz, false);
            GameObjectUtility.SetStaticEditorFlags(g, 0);
            g.transform.localPosition = Vector3.zero; g.transform.localRotation = Quaternion.identity;
            var r = g.GetComponent<MeshRenderer>(); r.enabled = false; rs.Add(r);
        }
        vessel.fale = rs.ToArray();
    }

    /// <summary>Cumy dziobowa i rufowa z pomostu do łodzi + odbijacze przy burcie (jacht).</summary>
    static void Cumy(BraxNabrzeze.Zbior z, Material lina, Material odbij, BraxVessel v, Vector3 pomost, bool burta)
    {
        var t = v.transform; float L1 = v.length * .5f;
        var dz = t.forward;
        foreach (float f in new[] { .8f, -.8f })
        {
            Vector3 naLodzi, naPomoscie;
            if (burta)
            {
                naLodzi = t.position + dz * L1 * f + Vector3.up * (v.seat.y * .55f) - t.right * 0f;
                naLodzi.x = pomost.x + .6f;
                naPomoscie = new Vector3(pomost.x - .22f, pomost.y + .12f, naLodzi.z + f * 1.5f);
            }
            else
            {
                // cumowanie rufą: dwie cumy z narożników rufy na knagi pomostu (szpring w „V”)
                naLodzi = t.position - dz * L1 * .92f + t.right * (v.width * .38f * Mathf.Sign(f)) + Vector3.up * (v.seat.y * .5f);
                naPomoscie = new Vector3(pomost.x + .22f, pomost.y + .12f, t.position.z + (f > 0 ? 1.6f : -1.6f));
            }
            var mid = (naLodzi + naPomoscie) * .5f + Vector3.down * .3f;
            z[lina].Rura(naPomoscie, mid, .018f * K, 6, -1, false); z[lina].Rura(mid, naLodzi, .018f * K, 6, -1, false);
        }
        if (burta)
            for (int i = -2; i <= 2; i++)
            {
                var p = new Vector3(pomost.x + .22f, W + .35f, t.position.z + i * L1 * .35f);
                z[odbij].Rura(p + Vector3.up * .32f * K, p - Vector3.up * .32f * K, .11f * K, 10, .11f * K);
                z[lina].Rura(p + Vector3.up * .32f * K, new Vector3(pomost.x + .05f, pomost.y + .05f, p.z), .008f, 4, -1, false);
            }
    }
}

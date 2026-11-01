using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// DROGI OSIEDLA A OD NOWA (audyt filmów właściciela 1.11; decyzja 16:36: „zrób od nowa, zrób wszystkie jednolite”).
// Stara sieć Codexa (three.js: world.js, civic.js) miała chodniki ciągnięte przez całą szerokość mapy — każde skrzyżowanie
// przecinały pasy kostki, zebry leżały na środku skrzyżowań (obrócone, z linią osiową przez pasy), drogi kończyły się
// na płocie granicznym albo w trawie parku. Tutaj: stare nawierzchnie, linie, zebry, latarnie i słupy tablic wycięte
// z siatek Codexa (CutVolumes) i postawione od nowa według jednej reguły, tej samej co w części B:
//   • przekrój: jezdnia | krawężnik | pas zieleni | chodnik; chodnik i krawężnik przerwane na skrzyżowaniu,
//     narożniki wyłożone płytami (chodniki się łączą), jezdnia ciągła;
//   • zebra zawsze na wlocie ulicy, w linii chodnika ulicy poprzecznej (między dwoma narożnikami), pasy wzdłuż jezdni,
//     linia osiowa przerwana metr przed zebrą i skrzyżowaniem;
//   • rondo na głównym skrzyżowaniu (Poznańska × ulica środkowa);
//   • żadna droga donikąd: końce bulwarów = parkingi przed kasynem, dyskoteką, komisariatem, szpitalem, salą
//     bokserską i halą walk; końce Wiśniowej i Bydgoskiej przy parku = parkingi parkowe; droga do lasu = brama;
//   • tablice ulic według polskiego wzoru (granat, biały napis) na słupku na narożniku chodnika; znak D-18 „P”.
// Współrzędne Unity (x = −x three.js). Układ osiedla A jest symetryczny względem x = 0.
public static partial class BraxClaudeOsiedle
{
    sealed class UlicaA
    {
        public string id, nazwa;
        public bool alongX;                // true: oś wzdłuż x (stałe z = c)
        public float c, a0, a1;            // oś poprzeczna, zakres wzdłuż osi
        public float cw, kerb = .2f, green, walkNeg, walkPos;   // półszerokość jezdni, krawężnik, zieleń, chodnik po stronie −/+
        public bool centerLine = true;
        public float HalfNeg => cw + kerb + (walkNeg > 0 ? green + walkNeg : 0f);
        public float HalfPos => cw + kerb + (walkPos > 0 ? green + walkPos : 0f);
        public float Half => Mathf.Max(HalfNeg, HalfPos);
    }
    struct WezelA { public float x, z; public bool rondo; }
    const float RondoJezdnia = 12.5f, RondoWyspa = 5.6f, RondoChodnik = 15.8f;
    const float YAsf = .055f, YPlyta = .058f, YFarba = .072f;   // nad resztkami nawierzchni Codexa (≤ 0.05 m)
    // zebra: paski 0.5 m co 1 m wzdłuż kierunku jazdy (stripesAlongZ = ulica biegnie wzdłuż z)
    static void Pasy(float x0, float x1, float z0, float z1, bool stripesAlongZ)
    {
        if (stripesAlongZ) for (float x = x0 + .25f; x < x1 - .3f; x += 1f) Top(Paint, x, x + .5f, z0, z1, YFarba);
        else for (float z = z0 + .25f; z < z1 - .3f; z += 1f) Top(Paint, x0, x1, z, z + .5f, YFarba);
    }
    // obrócona kolizja (łuki ronda)
    static void ColliderRot(Vector3 c, Vector3 s, float yaw)
    {
        var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false); g.transform.SetPositionAndRotation(c, Quaternion.Euler(0, yaw, 0));
        g.AddComponent<BoxCollider>().size = s;
    }
    // pionowa ściana po łuku (bok krawężnika / chodnika ronda); kąty w stopniach
    static void ArcWall(Material m, Vector3 c, float r, float a0, float a1, float y0, float y1, int seg, bool outward)
    {
        for (int k = 0; k < seg; k++)
        {
            float t0 = Mathf.Lerp(a0, a1, k / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (k + 1) / (float)seg) * Mathf.Deg2Rad;
            Vector3 d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)), d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
            var n = ((d0 + d1) * .5f).normalized * (outward ? 1f : -1f);
            Quad(m, c + d0 * r + Vector3.up * y0, c + d1 * r + Vector3.up * y0, c + d1 * r + Vector3.up * y1, c + d0 * r + Vector3.up * y1, n);
        }
    }

    static List<UlicaA> ulice;
    static List<WezelA> wezly;

    static void DrogiA(Transform estate, Transform physics)
    {
        group = "Osiedle A / drogi od nowa";
        ulice = new List<UlicaA>
        {
            // bulwary W–E (Codex: jezdnia 7 m, chodniki 2.8 m przy z ± 5) — końce przy x ±64 wchodzą w parkingi
            new UlicaA { id = "B1", alongX = true, c = -19f, a0 = -64f, a1 = 64f, cw = 3.5f, green = .6f, walkNeg = 2.1f, walkPos = 2.1f },
            new UlicaA { id = "B2", alongX = true, c = 39f, a0 = -64f, a1 = 64f, cw = 3.5f, green = .6f, walkNeg = 2.1f, walkPos = 2.1f },
            new UlicaA { id = "B3", alongX = true, c = 86f, a0 = -64f, a1 = 64f, cw = 3.5f, green = .6f, walkNeg = 2.1f, walkPos = 2.1f },
            // Poznańska (oś x = 0): od bulwaru B1 (podjazd willi) do bramy tunelu do miasta
            new UlicaA { id = "AV", nazwa = "ul_poznanska", alongX = false, c = 0f, a0 = -19f, a1 = 97.5f, cw = 4.5f, green = .3f, walkNeg = 2.4f, walkPos = 2.4f },
            // Wiśniowa (x −58) i Bydgoska (x 58): od parkingów przy parku do bulwaru B3
            new UlicaA { id = "N1", nazwa = "ul_wisniowa", alongX = false, c = -58f, a0 = -62f, a1 = 86f, cw = 3f, green = .3f, walkNeg = 2f, walkPos = 2f },
            new UlicaA { id = "N2", nazwa = "ul_bydgoska", alongX = false, c = 58f, a0 = -62f, a1 = 86f, cw = 3f, green = .3f, walkNeg = 2f, walkPos = 2f },
            // uliczki przy domach (x ±43), chodnik po stronie zewnętrznej; zachodnia prowadzi dalej do bramy lasu (z −57.5)
            new UlicaA { id = "S1", alongX = false, c = -43f, a0 = -57.5f, a1 = 39f, cw = 2.5f, green = .3f, walkNeg = 2f, walkPos = 0f, centerLine = false },
            new UlicaA { id = "S2", alongX = false, c = 43f, a0 = -19f, a1 = 39f, cw = 2.5f, green = .3f, walkNeg = 0f, walkPos = 2f, centerLine = false },
        };
        wezly = new List<WezelA>();
        foreach (var u in ulice.Where(q => q.alongX))
            foreach (var v in ulice.Where(q => !q.alongX))
                if (v.c >= u.a0 - .01f && v.c <= u.a1 + .01f && u.c >= v.a0 - .01f && u.c <= v.a1 + .01f)
                    wezly.Add(new WezelA { x = v.c, z = u.c, rondo = Mathf.Abs(v.c) < .1f && Mathf.Abs(u.c - 39f) < .1f });

        // 1) wycięcie starych nawierzchni Codexa: korytarze ulic, parkingi, place przed budynkami nachodzące na jezdnię
        var cut = new List<Bounds>();
        foreach (var u in ulice)
        {
            float ext = 1.2f;
            if (u.alongX) cut.Add(MinMax(u.a0 - ext, u.a1 + ext, -.06f, .13f, u.c - 6.5f, u.c + 6.5f));
            else cut.Add(MinMax(u.c - Mathf.Max(u.Half, 5.6f) - .3f, u.c + Mathf.Max(u.Half, 5.6f) + .3f, -.06f, .13f, u.a0 - ext, u.a1 + ext));
        }
        // stare bulwary Codexa dalej do x ±112, jezdnie parkowe Wiśniowej/Bydgoskiej do z −92, droga do lasu
        foreach (float z in new[] { -19f, 39f, 86f }) cut.Add(MinMax(-113f, 113f, -.06f, .13f, z - 6.6f, z + 6.6f));
        foreach (float x in new[] { -58f, 58f }) cut.Add(MinMax(x - 5.7f, x + 5.7f, -.06f, .13f, -93f, 87f));
        cut.Add(MinMax(-46f, -40f, -.06f, .13f, -60f, -25f));
        // place przed budynkami (Codex: fz+18 ± 2.5, szer. 44) — nachodzą na bulwary; odtworzone niżej w części poza jezdnią
        var budynki = new[] { (86f, -42f), (86f, 14f), (-86f, -42f), (-86f, 14f), (86f, 64f), (-86f, 64f) };
        foreach (var (fx, fz) in budynki) { cut.Add(MinMax(fx - 22.2f, fx + 22.2f, -.06f, .13f, fz + 15.3f, fz + 20.7f)); cut.Add(MinMax(fx - 22.2f, fx + 22.2f, -.06f, .13f, fz - 18.7f, fz - 15.3f)); }
        // stare latarnie i kosze Wiśniowej/Bydgoskiej (x ∓ 4, z −7/16/60/80), słupy starych tablic ulic
        foreach (float x in new[] { -58f, 58f })
            foreach (float z in new[] { -7f, 16f, 60f, 80f })
            { cut.Add(MinMax(x - 4.7f, x - 3.3f, -.06f, 5.6f, z - .6f, z + .6f)); cut.Add(MinMax(x + 3.4f, x + 4.6f, -.06f, 1.1f, z - .5f, z + .5f)); }
        foreach (var (sx, sz) in new[] { (-6f, 82f), (51f, 36f), (-51f, -22f) }) cut.Add(MinMax(sx - .3f, sx + .3f, -.06f, 3.2f, sz - .3f, sz + .3f));
        cut.Add(MinMax(49.2f, 52.8f, 2.3f, 3.2f, 35.8f, 36.2f));   // tablica BYDGOSKA Codexa (WIŚNIOWA i POZNAŃSKA wycina WawerHouse)
        CutVolumes(estate, physics, cut);

        // 2) ulice
        foreach (var u in ulice) UlicaOdcinki(u);
        // 3) skrzyżowania i rondo
        foreach (var w in wezly) { if (w.rondo) Rondo(w); else Skrzyzowanie(w); }
        // 4) końce: parkingi przy budynkach (bulwary), parkingi parkowe, brama lasu
        foreach (float s in new[] { -1f, 1f })
        {
            ParkingBulwar(s, -19f, -21.4f, -4.6f, 2);    // kasyno / komisariat (z południa) i dyskoteka / szpital (z północy)
            ParkingBulwar(s, 39f, 34.6f, 45.4f, 1);      // przed dyskoteką / szpitalem
            ParkingBulwar(s, 86f, 82.4f, 95.6f, 1);      // przed salą bokserską / halą walk
            ParkingPark(s * 58f);
        }
        // brama lasu na końcu uliczki zachodniej (Codex: płot 6 m + tablica LAS · TEREN ZAMKNIĘTY) — szlaban przed płotem
        Szlaban(-43f, -56.6f, 2.5f);
        // 5) place przed budynkami poza jezdnią (wycięte wyżej) — płyty od ściany do chodnika bulwaru
        // krawędzie parkingów: z −21.4…−4.6 (B1), 34.6…45.4 (B2), 82.4…95.6 (B3)
        var lotEdges = new[] { (-21.4f, -4.6f), (34.6f, 45.4f), (82.4f, 95.6f) };
        foreach (var (fx, fz) in budynki)
        {
            float zf0 = fz + 15f, zf1 = fz + 20.5f, zr0 = fz - 18.5f, zr1 = fz - 15f;
            float front = lotEdges.Select(e => e.Item1).Where(e => e > zf0 - .5f).DefaultIfEmpty(zf1).Min();
            if (front > zf0 + .2f) BoxMinMax(Pavers, fx - 22f, fx + 22f, 0, .1f, zf0, Mathf.Min(zf1, front));
            float back = lotEdges.Select(e => e.Item2).Where(e => e < zr1 + .5f).DefaultIfEmpty(zr0).Max();
            if (back < zr1 - .2f) BoxMinMax(Pavers, fx - 22f, fx + 22f, 0, .1f, Mathf.Max(zr0, back), zr1);
        }
        rep.notes.Add("drogi A od nowa: " + ulice.Count + " ulic, " + wezly.Count + " skrzyżowań (w tym rondo), 8 parkingów, tablice ulic, szlaban lasu");
    }

    // Odcinek ulicy z przerwami na skrzyżowania: jezdnia ciągła do środka węzła (węzeł kładzie własną płytę), krawężnik,
    // zieleń i chodnik przerwane na szerokość ulicy poprzecznej, linia osiowa przerwana przed zebrami.
    static void UlicaOdcinki(UlicaA u)
    {
        var cross = wezly.Where(w => u.alongX ? Mathf.Abs(w.z - u.c) < .1f : Mathf.Abs(w.x - u.c) < .1f)
                         .Select(w => (pos: u.alongX ? w.x : w.z, w)).OrderBy(q => q.pos).ToList();
        UlicaA Other((float pos, WezelA w) q) => ulice.First(o => o.alongX != u.alongX && Mathf.Abs(o.c - (u.alongX ? q.w.x : q.w.z)) < .1f);
        // zakres wzdłuż osi zajęty przez węzeł dla paska w odległości d od osi tej ulicy
        (float, float) Zajete((float pos, WezelA w) q, float d, bool chodnik)
        {
            if (q.w.rondo) { float r = chodnik ? RondoChodnik : RondoJezdnia + .45f; float a = Mathf.Sqrt(Mathf.Max(0f, r * r - d * d)); return (q.pos - a, q.pos + a); }
            var o = Other(q);
            // strona węzła: ulica poprzeczna może się zaczynać / kończyć na tym węźle (skrzyżowanie T) — po drugiej stronie
            // poprzecznej nie ma; wtedy chodnik tej ulicy przechodzi ciągiem (nie ma czego przecinać)
            float lo = q.pos - o.HalfNeg, hi = q.pos + o.HalfPos;
            return (lo, hi);
        }
        // pasy wzdłuż osi: (d0, d1, materiał, wysokość, typ)
        var paski = new List<(float d0, float d1, Material m, float y0, float y1, int typ)>();
        foreach (int s in new[] { -1, 1 })
        {
            float walk = s < 0 ? u.walkNeg : u.walkPos;
            float k0 = u.cw, k1 = u.cw + u.kerb;
            paski.Add((s * k0, s * k1, KerbStone, 0f, .16f, 1));
            if (walk > 0)
            {
                float g1 = k1 + u.green, w1 = g1 + walk;
                if (u.green > .01f) paski.Add((s * k1, s * g1, null, 0f, 0f, 2));   // zieleń = trawa podłoża (nic nie kładziemy)
                paski.Add((s * g1, s * w1, Pavers, 0f, .14f, 3));
            }
        }
        // jezdnia: cały zakres (z węzłami — tam płyta węzła leży wyżej o 2 mm)
        float A0 = u.a0, A1 = u.a1;
        // na końcach na węźle (T) jezdnia sięga do środka węzła — płyta węzła przykryje
        RectAlong(u, A0, A1, -u.cw, u.cw, Asphalt, YAsf, YAsf, false);
        foreach (var (d0, d1, m, y0, y1, typ) in paski)
        {
            if (typ == 2) continue;
            float dIn = Mathf.Min(Mathf.Abs(d0), Mathf.Abs(d1)), sgn = Mathf.Sign(d0 + d1);
            // przedziały wolne od węzłów
            var runs = new List<(float, float)>(); float cur = A0;
            foreach (var q in cross)
            {
                var (lo, hi) = Zajete(q, typ == 3 ? dIn : dIn, typ == 3);
                // T: jeśli poprzeczna ulica nie sięga na tę stronę osi (kończy się na węźle), pasek po tej stronie jest ciągły
                if (!q.w.rondo)
                {
                    var o = Other(q); float side = sgn;   // strona tej ulicy (−/+) w osi poprzecznej
                    float myC = u.c, oStart = o.a0, oEnd = o.a1;
                    bool siega = side < 0 ? oStart < myC - .5f : oEnd > myC + .5f;
                    if (!siega) continue;
                }
                if (lo > cur) runs.Add((cur, lo)); cur = Mathf.Max(cur, hi);
            }
            if (cur < A1) runs.Add((cur, A1));
            foreach (var (r0, r1) in runs) if (r1 - r0 > .05f) RectAlong(u, r0, r1, d0, d1, m, y0, y1, true);
        }
        // linia osiowa: kreski 3 m co 6 m, przerwana przy węzłach (z zebrami: 1 m przed zebrą)
        if (u.centerLine)
        {
            for (float a = A0 + 2f; a + 3f < A1 - 2f; a += 6f)
            {
                bool blok = cross.Any(q => { var (lo, hi) = Zajete(q, 0f, true); return a + 3f > lo - 4.2f && a < hi + 4.2f; });
                if (!blok) RectAlong(u, a, a + 3f, -.06f, .06f, Paint, YFarba, YFarba, false);
            }
        }
        // latarnie co ~24 m na zewnętrznej krawędzi chodnika (nie na skrzyżowaniach i nie przy zebrach)
        foreach (int s in new[] { -1, 1 })
        {
            float walk = s < 0 ? u.walkNeg : u.walkPos; if (walk <= 0) continue;
            float d = s * (u.cw + u.kerb + u.green + .35f);
            for (float a = A0 + 9f; a < A1 - 6f; a += 24f)
            {
                if (cross.Any(q => { var (lo, hi) = Zajete(q, Mathf.Abs(d), true); return a > lo - 3f && a < hi + 3f; })) continue;
                var p = u.alongX ? new Vector3(a, 0, u.c + d) : new Vector3(u.c + d, 0, a);
                float yaw = u.alongX ? (s > 0 ? 180f : 0f) : (s > 0 ? 270f : 90f);
                if (!IsHardLamp(p)) Lamp(p, yaw);
            }
        }
    }
    static bool IsHardLamp(Vector3 p) => Physics.CheckSphere(p + Vector3.up * 1.5f, .35f, ~0, QueryTriggerInteraction.Ignore);

    // Prostokąt wzdłuż osi ulicy: a0..a1 wzdłuż, d0..d1 w poprzek (od osi). Płaski (y0 == y1) albo bryła.
    static void RectAlong(UlicaA u, float a0, float a1, float d0, float d1, Material m, float y0, float y1, bool box)
    {
        float lo = Mathf.Min(d0, d1), hi = Mathf.Max(d0, d1);
        float x0, x1, z0, z1;
        if (u.alongX) { x0 = a0; x1 = a1; z0 = u.c + lo; z1 = u.c + hi; } else { x0 = u.c + lo; x1 = u.c + hi; z0 = a0; z1 = a1; }
        if (box && y1 > y0) BoxMinMax(m, x0, x1, y0, y1, z0, z1);
        else Top(m, x0, x1, z0, z1, y1);
    }

    // Skrzyżowanie zwykłe: płyta asfaltu, narożniki z płyt (łączą chodniki), krawężniki narożników, zebry na wlotach.
    static void Skrzyzowanie(WezelA w)
    {
        var ux = ulice.First(u => u.alongX && Mathf.Abs(u.c - w.z) < .1f);
        var uz = ulice.First(u => !u.alongX && Mathf.Abs(u.c - w.x) < .1f);
        Top(Asphalt, w.x - uz.cw, w.x + uz.cw, w.z - ux.cw, w.z + ux.cw, YPlyta);
        // ramiona: czy ulica wychodzi z węzła w danym kierunku
        bool E = ux.a1 > w.x + .5f, W = ux.a0 < w.x - .5f, N = uz.a1 > w.z + .5f, S = uz.a0 < w.z - .5f;
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                bool armX = sx > 0 ? E : W, armZ = sz > 0 ? N : S;
                float hx = sx > 0 ? uz.HalfPos : uz.HalfNeg, hz = sz > 0 ? ux.HalfPos : ux.HalfNeg;   // zasięg narożnika
                float kx = uz.cw + uz.kerb, kz = ux.cw + ux.kerb;
                // narożnik istnieje, gdy po tej stronie jest chodnik którejś z ulic
                bool walkX = (sz > 0 ? ux.walkPos : ux.walkNeg) > 0, walkZ = (sx > 0 ? uz.walkPos : uz.walkNeg) > 0;
                if (!walkX && !walkZ) continue;
                if (!armX || !armZ) continue;    // T: po stronie bez ramienia chodnik ulicy przelotowej jest ciągły (UlicaOdcinki)
                float x0 = w.x + sx * kx, x1 = w.x + sx * hx, z0 = w.z + sz * kz, z1 = w.z + sz * hz;
                BoxMinMax(Pavers, Mathf.Min(x0, x1), Mathf.Max(x0, x1), 0, .14f, Mathf.Min(z0, z1), Mathf.Max(z0, z1));
                // krawężnik narożnika (L): wzdłuż jezdni ulicy x i ulicy z
                if (armX) BoxMinMax(KerbStone, Mathf.Min(w.x + sx * kx, w.x + sx * hx), Mathf.Max(w.x + sx * kx, w.x + sx * hx), 0, .16f, Mathf.Min(w.z + sz * ux.cw, w.z + sz * kz), Mathf.Max(w.z + sz * ux.cw, w.z + sz * kz));
                if (armZ) BoxMinMax(KerbStone, Mathf.Min(w.x + sx * uz.cw, w.x + sx * kx), Mathf.Max(w.x + sx * uz.cw, w.x + sx * kx), 0, .16f, Mathf.Min(w.z + sz * kz, w.z + sz * hz), Mathf.Max(w.z + sz * kz, w.z + sz * hz));
            }
        // T bez ramienia: krawężnik i chodnik ulicy przelotowej idą ciągiem (UlicaOdcinki) — tu tylko płyta jezdni wlotu
        // zebry: na każdym ramieniu ulicy z (przez jej jezdnię) w linii chodnika ulicy x i odwrotnie
        foreach (int sz in new[] { -1, 1 })
        {
            bool arm = sz > 0 ? N : S; if (!arm) continue;
            float k = ux.cw + ux.kerb, h = sz > 0 ? ux.HalfPos : ux.HalfNeg; if (h - k < 1.5f) continue;
            Pasy(w.x - uz.cw + .2f, w.x + uz.cw - .2f, Mathf.Min(w.z + sz * (k + .2f), w.z + sz * (h - .1f)), Mathf.Max(w.z + sz * (k + .2f), w.z + sz * (h - .1f)), true);
        }
        foreach (int sx in new[] { -1, 1 })
        {
            bool arm = sx > 0 ? E : W; if (!arm) continue;
            float k = uz.cw + uz.kerb, h = sx > 0 ? uz.HalfPos : uz.HalfNeg; if (h - k < 1.5f) continue;
            Pasy(Mathf.Min(w.x + sx * (k + .2f), w.x + sx * (h - .1f)), Mathf.Max(w.x + sx * (k + .2f), w.x + sx * (h - .1f)), w.z - ux.cw + .2f, w.z + ux.cw - .2f, false);
        }
        // tablice ulic: na narożniku NE (od strony ulicy z nazwą)
        foreach (var u in new[] { uz, ux })
        {
            if (string.IsNullOrEmpty(u.nazwa)) continue;
            int sx = w.x <= 0 ? 1 : -1, sz = 1;
            if (!(sx > 0 ? (E && uz.walkPos > 0) : (W && uz.walkNeg > 0))) sx = -sx;
            var p = new Vector3(w.x + sx * (uz.cw + uz.kerb + uz.green + .5f), 0, w.z + sz * (ux.cw + ux.kerb + ux.green + .5f));
            TablicaUlicy(u.nazwa, p, u.alongX);
        }
    }

    // Rondo: jezdnia pierścieniowa, wyspa z krawężnikiem, trawą i drzewem, chodnik dookoła, zebry na czterech wlotach.
    static void Rondo(WezelA w)
    {
        var c = new Vector3(w.x, 0, w.z);
        Ring(Asphalt, c + Vector3.up * YPlyta, RondoWyspa + .25f, RondoJezdnia, 64);
        // wyspa: krawężnik najazdowy (szary) + trawa + drzewo i krzewy
        Cylinder(KerbStone, c, RondoWyspa + .25f, .12f, 48, false, true);
        Ring(KerbStone, c + Vector3.up * .12f, RondoWyspa, RondoWyspa + .25f, 48);
        Disc(Grass, c + Vector3.up * .1f, RondoWyspa, 48);
        Tree(c, 8.2f, 33f);
        foreach (int k in Enumerable.Range(0, 6)) Bush(c + Quaternion.Euler(0, k * 60f + 15f, 0) * Vector3.forward * 3.8f, 1.1f, k * 47f);
        // ramiona i ich kąty (w stopniach, oś x = 0°)
        var ux = ulice.First(u => u.alongX && Mathf.Abs(u.c - w.z) < .1f);
        var uz = ulice.First(u => !u.alongX && Mathf.Abs(u.c - w.x) < .1f);
        var arms = new List<(float ang, float half)>();
        if (ux.a1 > w.x + .5f) arms.Add((0f, ux.cw + ux.kerb));
        if (uz.a1 > w.z + .5f) arms.Add((90f, uz.cw + uz.kerb));
        if (ux.a0 < w.x - .5f) arms.Add((180f, ux.cw + ux.kerb));
        if (uz.a0 < w.z - .5f) arms.Add((270f, uz.cw + uz.kerb));
        // pierścienie zewnętrzne (krawężnik, chodnik) przerwane na ramionach
        void Luki(float r0, float r1, Material m, float y, bool box)
        {
            var gaps = arms.Select(a => (a.ang, Mathf.Asin(Mathf.Clamp01(a.half / ((r0 + r1) * .5f))) * Mathf.Rad2Deg)).OrderBy(g => g.ang).ToList();
            for (int i = 0; i < gaps.Count; i++)
            {
                float s0 = gaps[i].ang + gaps[i].Item2, s1 = gaps[(i + 1) % gaps.Count].ang - gaps[(i + 1) % gaps.Count].Item2 + (i + 1 == gaps.Count ? 360f : 0f);
                if (s1 <= s0) continue;
                int seg = Mathf.Max(4, Mathf.CeilToInt((s1 - s0) / 4f));
                RingArc(m, c + Vector3.up * y, r0, r1, s0, s1, seg);
                if (box)
                {
                    ArcWall(m, c, r0, s0, s1, 0f, y, seg, false); ArcWall(m, c, r1, s0, s1, 0f, y, seg, true);
                    for (int k = 0; k < seg; k++)   // kolizja po łuku (krótkie obrócone bryły)
                    {
                        float t = Mathf.Lerp(s0, s1, (k + .5f) / seg) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
                        ColliderRot(c + d * ((r0 + r1) * .5f) + Vector3.up * (y * .5f), new Vector3(r1 - r0, y, (s1 - s0) * Mathf.Deg2Rad * (r0 + r1) * .5f / seg + .05f), 90f - t * Mathf.Rad2Deg);
                    }
                }
            }
        }
        Luki(RondoJezdnia, RondoJezdnia + .22f, KerbStone, .16f, true);
        Luki(RondoJezdnia + .22f, RondoChodnik, Pavers, .14f, true);
        HardRound(c, 0f, RondoChodnik + .2f);
        for (float a = 0; a < 360f; a += 5f) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward * ((RondoJezdnia + RondoChodnik) * .5f); Paved(c.x + d.x - 1f, c.x + d.x + 1f, c.z + d.z - 1f, c.z + d.z + 1f); }
        // zebry na wlotach, tuż za pierścieniem chodnika
        foreach (var (ang, half) in arms)
        {
            var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0, Mathf.Sin(ang * Mathf.Deg2Rad));
            float a0 = RondoChodnik + .3f, a1 = a0 + 2.8f;
            var u = Mathf.Abs(dir.x) > .5f ? ux : uz; float cw = u.cw - .2f;
            if (Mathf.Abs(dir.x) > .5f) Pasy(Mathf.Min(c.x + dir.x * a0, c.x + dir.x * a1), Mathf.Max(c.x + dir.x * a0, c.x + dir.x * a1), c.z - cw, c.z + cw, false);
            else Pasy(c.x - cw, c.x + cw, Mathf.Min(c.z + dir.z * a0, c.z + dir.z * a1), Mathf.Max(c.z + dir.z * a0, c.z + dir.z * a1), true);
        }
        rep.notes.Add("rondo A: (" + w.x + ", " + w.z + "), jezdnia r " + RondoJezdnia + ", wyspa r " + RondoWyspa + ", " + arms.Count + " wloty z zebrami");
    }

    // Parking na końcu bulwaru (między budynkami): wjazd z bulwaru, rzędy miejsc prostopadłych 2.5 × 5 m, aleja 6 m,
    // krawężnik dookoła, zawracanie na końcu. rows = 2 (miejsca po obu stronach alei) albo 1 (miejsca od strony północnej).
    static void ParkingBulwar(float side, float zc, float z0, float z1, int rows)
    {
        float xIn = side * 64f, xOut = side * 107.5f;
        float xa = Mathf.Min(xIn, xOut), xb = Mathf.Max(xIn, xOut);
        Top(Asphalt, xa, xb, z0, z1, YAsf);
        // krawężnik obwodowy (poza wjazdem od bulwaru)
        BoxMinMax(KerbStone, xa, xb, 0, .16f, z0 - .2f, z0);
        BoxMinMax(KerbStone, xa, xb, 0, .16f, z1, z1 + .2f);
        if (side > 0) BoxMinMax(KerbStone, xb, xb + .2f, 0, .16f, z0 - .2f, z1 + .2f); else BoxMinMax(KerbStone, xa - .2f, xa, 0, .16f, z0 - .2f, z1 + .2f);
        // miejsca: linie co 2.5 m; wiersz północny (z1 − 5 … z1) i opcjonalnie południowy (z0 … z0 + 5)
        float startX = xa + 5f, endX = xb - 3f;
        int n = 0;
        void Rzad(float r0, float r1)
        {
            for (float x = startX; x + 2.5f <= endX + .01f; x += 2.5f)
            {
                Top(Paint, x - .06f, x + .06f, r0, r1, YFarba); n++;
            }
            float lineZ = Mathf.Abs(r0 - z0) < .01f ? r1 : r0;
            Top(Paint, startX, endX, lineZ - .06f, lineZ + .06f, YFarba);
        }
        if (z1 - z0 >= 10.5f) Rzad(z1 - 5f, z1);
        if (rows == 2 && z1 - z0 >= 16f) Rzad(z0, z0 + 5f);
        // strzałki kierunku: biała kreska wzdłuż alei (jezdnia dwukierunkowa) — przerywana
        float aisle = rows == 2 ? (z0 + z1) * .5f : (z0 + z1 - 5f) * .5f;
        for (float x = startX + 1f; x + 2f < endX; x += 5f) Top(Paint, x, x + 2f, aisle - .06f, aisle + .06f, YFarba);
        ZnakP(new Vector3(xIn + side * 1.2f, 0, z1 + .9f));
        rep.notes.Add("parking " + (side > 0 ? "E" : "W") + " z " + zc + ": " + n + " miejsc");
    }

    // Parking przy parku na końcu Wiśniowej / Bydgoskiej (z −62 … −95): jezdnia ulicy jako aleja, miejsca prostopadłe
    // od strony zewnętrznej, zawrotka na końcu.
    static void ParkingPark(float x)
    {
        float s = Mathf.Sign(x), z0 = -95f, z1 = -62f;
        float aIn = x - s * 3f, aOut = x + s * 8.6f;     // aleja (dawna jezdnia) + rząd miejsc 5 m + krawężnik
        float xa = Mathf.Min(aIn, aOut), xb = Mathf.Max(aIn, aOut);
        Top(Asphalt, xa, xb, z0, z1, YAsf);
        BoxMinMax(KerbStone, xa - .2f, xa, 0, .16f, z0, z1); BoxMinMax(KerbStone, xb, xb + .2f, 0, .16f, z0, z1);
        BoxMinMax(KerbStone, xa - .2f, xb + .2f, 0, .16f, z0 - .2f, z0);
        float r0 = x + s * 3.4f, r1 = x + s * 8.4f;      // rząd miejsc
        int n = 0;
        for (float z = z0 + 2f; z + 2.5f <= z1 - 1f; z += 2.5f) { Top(Paint, Mathf.Min(r0, r1), Mathf.Max(r0, r1), z - .06f, z + .06f, YFarba); n++; }
        Top(Paint, Mathf.Min(r0, r0 + s * .12f), Mathf.Max(r0, r0 + s * .12f), z0 + 2f, z0 + 2f + (n - 1) * 2.5f, YFarba);
        // chodnik wzdłuż rzędu miejsc, po stronie zewnętrznej (dojście do parku i do ulicy)
        float outer = s > 0 ? xb : xa;
        BoxMinMax(Pavers, Mathf.Min(outer + s * .2f, outer + s * 2.2f), Mathf.Max(outer + s * .2f, outer + s * 2.2f), 0, .14f, z0, z1);
        ZnakP(new Vector3(x + s * 4.2f, 0, z1 - .6f));
        rep.notes.Add("parking przy parku x " + x + ": " + n + " miejsc");
    }

    // Szlaban (biało-czerwony) z dwoma słupkami — koniec drogi do lasu.
    static void Szlaban(float x, float z, float cw)
    {
        Box(Frame, new Vector3(x - cw - .4f, .55f, z), new Vector3(.25f, 1.1f, .25f));
        Box(Frame, new Vector3(x + cw + .4f, .45f, z), new Vector3(.18f, .9f, .18f));
        for (int k = 0; k < 6; k++)
        {
            float a = x - cw - .3f + k * (2f * cw + .6f) / 6f;
            Box(k % 2 == 0 ? Plain("szlaban czerwony", new Color(.78f, .1f, .1f), .4f) : Paint, new Vector3(a + (2f * cw + .6f) / 12f, 1.0f, z), new Vector3((2f * cw + .6f) / 6f, .12f, .1f), false);
        }
        Collider(new Vector3(x, .8f, z), new Vector3(2f * cw + .8f, 1.6f, .3f));
    }

    // Tablica z nazwą ulicy (polski wzór: granat, biały napis) na słupku ocynkowanym, dwustronna, równoległa do ulicy.
    static void TablicaUlicy(string id, Vector3 p, bool streetAlongX)
    {
        if (Physics.CheckSphere(p + Vector3.up * 1.2f, .25f, ~0, QueryTriggerInteraction.Ignore)) p += (streetAlongX ? Vector3.forward : Vector3.right) * .8f;
        var pole = Plain("słupek ocynk", new Color(.62f, .64f, .66f), .45f, .7f);
        Cylinder(pole, p, .03f, 2.95f, 10, true, false);
        Collider(p + Vector3.up * 1.4f, new Vector3(.12f, 2.8f, .12f));
        Vector3 n = streetAlongX ? Vector3.forward : Vector3.right;
        var c = p + Vector3.up * 2.7f;
        Box(Frame, c, streetAlongX ? new Vector3(.94f, .26f, .03f) : new Vector3(.03f, .26f, .94f), false);
        Sign(id, c + n * .016f, .9f, .225f, n, false);
        Sign(id, c - n * .016f, .9f, .225f, -n, false);
    }

    // Znak D-18 „parking”: niebieski kwadrat z białym P na słupku.
    static void ZnakP(Vector3 p)
    {
        var pole = Plain("słupek ocynk", new Color(.62f, .64f, .66f), .45f, .7f);
        Cylinder(pole, p, .03f, 2.6f, 10, true, false);
        Collider(p + Vector3.up * 1.3f, new Vector3(.12f, 2.6f, .12f));
        var c = p + Vector3.up * 2.25f;
        Box(Frame, c, new Vector3(.62f, .62f, .03f), false);
        Sign("znak_parking", c + Vector3.forward * .016f, .6f, .6f, Vector3.forward, false);
        Sign("znak_parking", c - Vector3.forward * .016f, .6f, .6f, Vector3.back, false);
    }
}

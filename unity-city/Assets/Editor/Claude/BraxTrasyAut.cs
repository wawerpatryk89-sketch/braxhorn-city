using System;
using System.Collections.Generic;
using UnityEngine;

// TRASY AUT (build 14, 2.11) — generator linii jazdy z opisu ulic (czysty C#, ten sam w buildzie i w teście lokalnym).
// Każda trasa to ciąg węzłów (skrzyżowań) i końców; między nimi prosta środkiem prawego pasa (połowa półszerokości
// jezdni od osi). Manewry w węźle:
//  • prosto — pas ciągły;
//  • w prawo — łuk okręgu styczny do pasów (promień 3,9–6,5 m dobrany tak, by wewnętrzny bok auta minął narożnik
//    krawężnika; przed łukiem łagodne odsunięcie do osi o ≤ 0,6 m — jak kierowca, który „bierze szerzej”);
//  • w lewo — łuk o promieniu ≈ pas wjazdu + pas wyjazdu + 1,6 m przez środek skrzyżowania;
//  • rondo — wjazd krzywą Hermite’a styczną do pierścienia, jazda po pierścieniu przeciwnie do wskazówek zegara,
//    zjazd styczny do pasa wylotu.
// Zdarzenia: linia zatrzymania (przed zebrą) i wyjazd ze skrzyżowania, linia ustąpienia i zjazd z ronda, kierunkowskazy
// (30 m przed łukiem; na rondzie prawy przed zjazdem), koniec trasy otwartej. Na koniec linia przepróbkowana co ~0,5 m.
public static class BraxTrasyAut
{
    public sealed class Ulica { public string id; public bool wzdluzX; public float c, a0, a1, cw, half; }
    public sealed class Wezel { public float x, z; public bool rondo; public int rodzaj; public Ulica ux, uz; public int indeks; }
    public sealed class Definicja { public string nazwa; public Vector2[] punkty; public bool petla, ukryteKonce; public int obszar; public float y; }
    public sealed class Wynik
    {
        public readonly List<Vector3> punkty = new List<Vector3>();
        public readonly List<BraxTrasaAut> trasy = new List<BraxTrasaAut>();
        public readonly List<BraxZdarzenieAut> zdarzenia = new List<BraxZdarzenieAut>();
        public readonly List<BraxWezelAut> wezly = new List<BraxWezelAut>();
        public readonly List<string> log = new List<string>();
    }
    public static float RondoPromien = 9.4f, RondoJezdnia = 12.5f, RondoKatWjazdu = 34f, KrokProbki = .5f;

    static Vector2 Rgt(Vector2 d) => new Vector2(d.y, -d.x);
    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    static Vector2 Rot(Vector2 v, float deg) { float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }
    static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
    static float Ang(Vector2 d) { float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; return a < 0f ? a + 360f : a; }
    static float Smooth(float u) => u * u * (3f - 2f * u);

    sealed class Manewr
    {
        public Wezel w; public Vector2 cur, din, dout; public int zwrot; public bool rondo;
        public float dStop, dWyj; public float katIn, katOut;
        public List<Vector2> pts = new List<Vector2>(); public int iLuk0 = -1, iLuk1 = -1, iMig = -1;
        public int i0, i1;   // zakres w linii surowej
    }

    static Ulica UlicaMiedzy(List<Ulica> ulice, Vector2 a, Vector2 b)
    {
        bool poX = Mathf.Abs(a.y - b.y) < .1f;
        foreach (var u in ulice) if (u.wzdluzX == poX && Mathf.Abs(u.c - (poX ? a.y : a.x)) < .1f) return u;
        return null;
    }
    static Wezel WezelW(List<Wezel> wezly, Vector2 p)
    {
        foreach (var w in wezly) if (Mathf.Abs(w.x - p.x) < .1f && Mathf.Abs(w.z - p.y) < .1f) return w;
        return null;
    }

    public static Wynik Generuj(List<Ulica> ulice, List<Wezel> wezly, List<Definicja> defs)
    {
        var wy = new Wynik();
        for (int i = 0; i < wezly.Count; i++)
        {
            wezly[i].indeks = i;
            wy.wezly.Add(new BraxWezelAut { srodek = new Vector3(wezly[i].x, 0f, wezly[i].z), rodzaj = wezly[i].rondo ? 1 : wezly[i].rodzaj });
        }
        foreach (var d in defs)
        {
            try { Trasa(wy, ulice, wezly, d); }
            catch (Exception e) { wy.log.Add("trasa " + d.nazwa + ": BŁĄD " + e.Message); }
        }
        return wy;
    }

    static void Trasa(Wynik wy, List<Ulica> ulice, List<Wezel> wezly, Definicja d)
    {
        // 1) elementy: punkty trasy + węzły leżące po drodze
        var el = new List<(Vector2 p, Wezel w)>();
        int n = d.punkty.Length;
        for (int i = 0; i < n; i++)
        {
            var a = d.punkty[i]; el.Add((a, WezelW(wezly, a)));
            if (!d.petla && i == n - 1) break;
            var b = d.punkty[(i + 1) % n];
            var mid = new List<Wezel>();
            bool poX = Mathf.Abs(a.y - b.y) < .1f;
            foreach (var w in wezly)
            {
                if (poX ? Mathf.Abs(w.z - a.y) > .1f : Mathf.Abs(w.x - a.x) > .1f) continue;
                float t = poX ? (w.x - a.x) / (b.x - a.x) : (w.z - a.y) / (b.y - a.y);
                if (t > .001f && t < .999f) mid.Add(w);
            }
            mid.Sort((p, q) => (new Vector2(p.x, p.z) - a).sqrMagnitude.CompareTo((new Vector2(q.x, q.z) - a).sqrMagnitude));
            foreach (var w in mid) el.Add((new Vector2(w.x, w.z), w));
        }
        int m = el.Count;
        if (d.petla) foreach (var e in el) if (e.w == null) throw new Exception("pętla przez punkt poza węzłem " + e.p);
        // 2) manewry w węzłach
        var man = new List<Manewr>();
        for (int i = 0; i < m; i++)
        {
            if (el[i].w == null) { man.Add(null); continue; }
            if (!d.petla && (i == 0 || i == m - 1)) { man.Add(null); continue; }
            var prev = el[(i + m - 1) % m]; var next = el[(i + 1) % m]; var cur = el[i];
            var uin = UlicaMiedzy(ulice, prev.p, cur.p); var uout = UlicaMiedzy(ulice, cur.p, next.p);
            if (uin == null || uout == null) throw new Exception("brak ulicy przy " + cur.p);
            float Ein = Mathf.Min(cur.w.rondo ? 24f : 14f, (cur.p - prev.p).magnitude * (prev.w != null ? .5f : 1f) - .5f);
            float Eout = Mathf.Min(cur.w.rondo ? 24f : 14f, (next.p - cur.p).magnitude * (next.w != null ? .5f : 1f) - .5f);
            man.Add(Buduj(cur.w, cur.p, (cur.p - prev.p).normalized, (next.p - cur.p).normalized, uin, uout, Ein, Eout, wy.log, d.nazwa));
        }
        // 3) linia surowa
        var raw = new List<Vector2>();
        void Dodaj(Vector2 p) { if (raw.Count == 0 || (raw[raw.Count - 1] - p).sqrMagnitude > 1e-6f) raw.Add(p); }
        void DodajManewr(Manewr mm)
        {
            var map = new int[mm.pts.Count];
            for (int k = 0; k < mm.pts.Count; k++) { Dodaj(mm.pts[k]); map[k] = raw.Count - 1; }
            mm.i0 = map[0]; mm.i1 = map[map.Length - 1];
            mm.iLuk0 = mm.iLuk0 >= 0 ? map[mm.iLuk0] : -1; mm.iLuk1 = mm.iLuk1 >= 0 ? map[mm.iLuk1] : -1; mm.iMig = mm.iMig >= 0 ? map[Mathf.Min(mm.iMig, map.Length - 1)] : -1;
        }
        if (d.petla)
        {
            var m0 = man[0];
            Dodaj(m0.pts[m0.pts.Count - 1]);
            for (int i = 1; i < m; i++) DodajManewr(man[i]);
            DodajManewr(man[0]);
        }
        else
        {
            var lane0 = UlicaMiedzy(ulice, el[0].p, el[1].p); var dir0 = (el[1].p - el[0].p).normalized;
            Dodaj(el[0].p + Rgt(dir0) * lane0.cw * .5f);
            for (int i = 1; i < m - 1; i++) if (man[i] != null) DodajManewr(man[i]);
            var laneN = UlicaMiedzy(ulice, el[m - 2].p, el[m - 1].p); var dirN = (el[m - 1].p - el[m - 2].p).normalized;
            Dodaj(el[m - 1].p + Rgt(dirN) * laneN.cw * .5f);
        }
        if (d.petla && (raw[raw.Count - 1] - raw[0]).sqrMagnitude < 1e-4f) raw.RemoveAt(raw.Count - 1);
        // 4) długości
        int R = raw.Count;
        var cum = new float[R + 1];
        for (int i = 1; i <= R; i++) { if (i == R && !d.petla) { cum[i] = cum[i - 1]; break; } cum[i] = cum[i - 1] + (raw[i % R] - raw[i - 1]).magnitude; }
        float L = d.petla ? cum[R] : cum[R - 1];
        float S(int i, float u) => Mathf.Lerp(cum[i], cum[i + 1], u);
        // pierwszy punkt odcinka (i0..i1) spełniający warunek f >= 0 (liniowo między próbkami)
        float Szukaj(int i0, int i1, Func<Vector2, float> f)
        {
            float fp = f(raw[i0]);
            if (fp >= 0f) return cum[i0];
            for (int i = i0 + 1; i <= i1; i++)
            {
                float fc = f(raw[i % R]);
                if (fc >= 0f) { float u = fp / (fp - fc); return S(i - 1, Mathf.Clamp01(u)); }
                fp = fc;
            }
            return -1f;
        }
        int ri = wy.trasy.Count;
        foreach (var mm in man)
        {
            if (mm == null) continue;
            var z = new BraxZdarzenieAut { trasa = ri, wezel = mm.w.indeks, zwrot = mm.zwrot, wlot = new Vector3(mm.din.x, 0f, mm.din.y) };
            var c = mm.cur;
            if (mm.rondo)
            {
                z.typ = BraxRuchAutSym.Z_RONDO; z.katWjazdu = mm.katIn % 360f; z.katZjazdu = mm.katOut % 360f;
                z.s0 = Szukaj(mm.i0, mm.i1, p => (RondoJezdnia + .8f) - (p - c).magnitude);
                z.s1 = Szukaj(mm.iLuk1, mm.i1, p => (p - c).magnitude - (RondoJezdnia + .6f));
            }
            else
            {
                z.typ = BraxRuchAutSym.Z_WEZEL;
                z.s0 = Szukaj(mm.i0, mm.i1, p => Vector2.Dot(p - c, mm.din) + mm.dStop);
                z.s1 = Szukaj(mm.iLuk1 >= mm.i0 ? mm.iLuk1 : mm.i0, mm.i1, p => Vector2.Dot(p - c, mm.dout) - mm.dWyj);
            }
            if (z.s0 < 0f || z.s1 < 0f || z.s1 <= z.s0) { wy.log.Add("trasa " + d.nazwa + ": węzeł (" + c.x + ", " + c.y + ") bez linii zatrzymania/wyjazdu"); continue; }
            wy.zdarzenia.Add(z);
            if (mm.rondo && mm.iMig >= 0)
                wy.zdarzenia.Add(new BraxZdarzenieAut { trasa = ri, typ = BraxRuchAutSym.Z_MIGACZ, kier = 1, s0 = cum[mm.iMig], s1 = z.s1 + 1f });
            else if (!mm.rondo && mm.zwrot != 0 && mm.iLuk0 >= 0)
                wy.zdarzenia.Add(new BraxZdarzenieAut { trasa = ri, typ = BraxRuchAutSym.Z_MIGACZ, kier = mm.zwrot, s0 = Mathf.Max(0f, cum[mm.iLuk0] - 30f), s1 = cum[mm.iLuk1] + 1f });
        }
        if (!d.petla) wy.zdarzenia.Add(new BraxZdarzenieAut { trasa = ri, typ = BraxRuchAutSym.Z_KONIEC, kier = d.ukryteKonce ? 1 : 0, s0 = L - .5f, s1 = L });
        // 5) przepróbkowanie co ~0,5 m
        int N = Mathf.Max(4, Mathf.RoundToInt(L / KrokProbki)); float h = L / N;
        int start = wy.punkty.Count, seg = 0;
        int ile = d.petla ? N : N + 1;
        for (int k = 0; k < ile; k++)
        {
            float s = Mathf.Min(k * h, L);
            while (seg < R - 1 && cum[seg + 1] < s) seg++;
            float u = cum[seg + 1] > cum[seg] ? (s - cum[seg]) / (cum[seg + 1] - cum[seg]) : 0f;
            var p = Vector2.Lerp(raw[seg], raw[(seg + 1) % R], Mathf.Clamp01(u));
            wy.punkty.Add(new Vector3(p.x, d.y, p.y));
        }
        wy.trasy.Add(new BraxTrasaAut { nazwa = d.nazwa, start = start, ile = ile, obszar = d.obszar, petla = d.petla, dl = L, krok = h });
    }

    static Manewr Buduj(Wezel w, Vector2 cur, Vector2 din, Vector2 dout, Ulica uin, Ulica uout, float Ein, float Eout, List<string> log, string trasa)
    {
        var mm = new Manewr { w = w, cur = cur, din = din, dout = dout };
        Vector2 rin = Rgt(din), rout = Rgt(dout);
        float lin = uin.cw * .5f, lout = uout.cw * .5f;
        // linia zatrzymania: przed zebrą ulicy poprzecznej (zebra w pasie chodnika — półszerokość ulicy poprzecznej)
        Ulica poprzIn = uin.wzdluzX ? w.uz : w.ux, poprzOut = uout.wzdluzX ? w.uz : w.ux;
        mm.dStop = (poprzIn != null ? poprzIn.half : uout.half) + .6f;
        mm.dWyj = (poprzOut != null ? poprzOut.cw : uin.cw) + .5f;
        var A = cur - din * Ein + rin * lin; var B = cur + dout * Eout + rout * lout;
        if (w.rondo)
        {
            mm.rondo = true; mm.zwrot = 0;
            Vector2 uIn = -din, uOut = dout;
            float aIn = Ang(uIn) + RondoKatWjazdu, aOut = Ang(uOut) - RondoKatWjazdu;
            while (aOut < aIn + 4f) aOut += 360f;
            mm.katIn = aIn; mm.katOut = aOut;
            float dB = RondoJezdnia + 6f;
            var Bq = cur + uIn * dB + rin * lin;
            var Q1 = cur + Dir(aIn) * RondoPromien; var t1 = Dir(aIn + 90f);
            var Q2 = cur + Dir(aOut) * RondoPromien; var t2 = Dir(aOut + 90f);
            var Dq = cur + uOut * dB + rout * lout;
            mm.pts.Add(A);
            if ((A - Bq).magnitude > .3f) mm.pts.Add(Bq);
            Hermite(mm.pts, Bq, din, Q1, t1, 1.15f);
            mm.iLuk0 = mm.pts.Count - 1;
            float migKat = aOut - 50f;
            for (float a = aIn + 3f; a < aOut - .5f; a += 3f)
            {
                if (mm.iMig < 0 && a >= migKat) mm.iMig = mm.pts.Count;
                mm.pts.Add(cur + Dir(a) * RondoPromien);
            }
            if (mm.iMig < 0) mm.iMig = mm.pts.Count;
            mm.pts.Add(Q2);
            mm.iLuk1 = mm.pts.Count - 1;
            Hermite(mm.pts, Q2, t2, Dq, dout, 1.15f);
            if ((B - Dq).magnitude > .3f) mm.pts.Add(B);
            return mm;
        }
        float dot = Vector2.Dot(din, dout), cr = Cross(din, dout);
        if (dot > .9f)
        {
            mm.zwrot = 0; mm.pts.Add(A); mm.pts.Add(cur + rin * lin); mm.pts.Add(B);
            mm.iLuk0 = 1; mm.iLuk1 = 1;
            return mm;
        }
        if (cr < 0f)
        {
            // w prawo: narożnik wewnętrzny = róg skrzyżowania po prawej
            mm.zwrot = 1;
            var corner = cur + rin * uin.cw + rout * uout.cw;
            // odsunięcie do osi przed łukiem (d1, do 1 m — na wąskiej uliczce kierowca zajmuje też kawałek drugiego pasa)
            // i za łukiem (d2); wybór: największy promień przy najmniejszym odsunięciu, z miejscem na łagodne przejście
            float bestR = -1f, b1 = 0f, b2 = 0f, bestOcena = -1e9f;
            foreach (float d1r in new[] { 0f, .15f, .3f, .45f, .6f })
                foreach (float d2r in new[] { 0f, .1f, .2f, .3f })
                {
                    float d1 = Mathf.Min(d1r, Mathf.Max(0f, lin - .6f)), d2 = Mathf.Min(d2r, Mathf.Max(0f, lout - .7f));
                    if (d1 < d1r - .01f || d2 < d2r - .01f) continue;
                    float Rr = 6.5f;
                    for (; Rr >= 3.7f; Rr -= .1f)
                    {
                        var C = cur + rin * (lin - d1 + Rr) + rout * (lout - d2 + Rr);
                        if ((corner - C).magnitude <= Rr - 1f) break;
                    }
                    if (Rr < 3.7f) continue;
                    float t1 = lout - d2 + Rr, t2 = lin - d1 + Rr;
                    if (Ein - t1 < 4.5f * d1 + .01f || Eout - t2 < 4.5f * d2 + .01f) continue;
                    float ocena = Mathf.Min(Rr, 5.5f) - 1.5f * (d1 + d2);
                    if (ocena > bestOcena) { bestOcena = ocena; bestR = Rr; b1 = d1; b2 = d2; }
                }
            if (bestR < 0f) { bestR = 3.7f; b1 = b2 = 0f; log.Add("trasa " + trasa + ": ciasny skręt w prawo (" + cur.x + ", " + cur.y + ") — promień 3,7 m"); }
            var Cc = cur + rin * (lin - b1 + bestR) + rout * (lout - b2 + bestR);
            var T1 = Cc - rin * bestR; var T2 = Cc - rout * bestR;
            float tIn = lout - b2 + bestR, tOut = lin - b1 + bestR;
            // dojazd z odsunięciem do osi
            int kroki = Mathf.Max(2, Mathf.CeilToInt((Ein - tIn) / .5f));
            for (int k = 0; k < kroki; k++)
            {
                float u = k / (float)kroki; float dist = Mathf.Lerp(Ein, tIn, u);
                mm.pts.Add(cur - din * dist + rin * (lin - b1 * Smooth(u)));
            }
            mm.iLuk0 = mm.pts.Count;
            int ka = Mathf.Max(6, Mathf.CeilToInt(bestR * Mathf.PI * .5f / .3f));
            for (int k = 0; k <= ka; k++) mm.pts.Add(Cc + Rot(T1 - Cc, -90f * k / ka));
            mm.iLuk1 = mm.pts.Count - 1;
            kroki = Mathf.Max(2, Mathf.CeilToInt((Eout - tOut) / .5f));
            for (int k = 1; k <= kroki; k++)
            {
                float u = k / (float)kroki; float dist = Mathf.Lerp(tOut, Eout, u);
                mm.pts.Add(cur + dout * dist + rout * (lout - b2 * (1f - Smooth(u))));
            }
            return mm;
        }
        {
            // w lewo: łuk przez środek skrzyżowania
            mm.zwrot = -1;
            float Rl = Mathf.Clamp(lin + lout + 1.6f, 4.5f, 7f);
            var Cc = cur + rin * (lin - Rl) + rout * (lout - Rl);
            var T1 = Cc + rin * Rl; var T2 = Cc + rout * Rl;
            mm.pts.Add(A);
            mm.iLuk0 = mm.pts.Count;
            int ka = Mathf.Max(8, Mathf.CeilToInt(Rl * Mathf.PI * .5f / .3f));
            for (int k = 0; k <= ka; k++) mm.pts.Add(Cc + Rot(T1 - Cc, 90f * k / ka));
            mm.iLuk1 = mm.pts.Count - 1;
            mm.pts.Add(B);
            return mm;
        }
    }

    // Trasy osiedla (build 14). Współrzędne z danych ulic (DrogiA: B1, B2 z rondem, B3, Poznańska AV, Wiśniowa N1,
    // Bydgoska N2, uliczki S1/S2) — nic na sztywno (etap dróg może przesuwać ulice). Część A: 8 pętli — bulwary w obu
    // kierunkach, Poznańska przez rondo z Bydgoską i z Wiśniową w obu kierunkach (same skręty w lewo / same w prawo), uliczki
    // przy domach (same skręty w prawo i rondo). Trasy nie wjeżdżają za B3 w stronę tunelu (szlaban, z ≈ 98).
    // Część B: ulica główna (ślepa na obu końcach przy płocie) i ulica od parku — przejazdy otwarte (auto znika na końcu
    // poza widokiem kamery i wjeżdża od nowa).
    public static List<Definicja> TrasyOsiedla(List<Ulica> ulice, float yA, float yB, float bX0, float bX1, float bZ, float bNX, float bNZ)
    {
        var l = new List<Definicja>();
        float? C(string id) { foreach (var u in ulice) if (u.id == id) return u.c; return null; }
        float? b1 = C("B1"), b2 = C("B2"), b3 = C("B3"), av = C("AV"), n1 = C("N1"), n2 = C("N2"), s1 = C("S1"), s2 = C("S2");
        // węzeł (ulica wzdłuż z: x, ulica wzdłuż x: z)
        Vector2 V(float? x, float? z) => new Vector2(x.Value, z.Value);
        void Petla(string nazwa, params (float? x, float? z)[] w)
        {
            foreach (var p in w) if (p.x == null || p.z == null) return;
            l.Add(new Definicja { nazwa = nazwa, petla = true, obszar = 0, y = yA, punkty = Array.ConvertAll(w, p => V(p.x, p.z)) });
        }
        Petla("A pętla bulwary (przeciwnie do wskazówek)", (n1, b1), (n2, b1), (n2, b2), (av, b2), (n1, b2));
        Petla("A pętla bulwary (zgodnie ze wskazówkami)", (n1, b2), (av, b2), (n2, b2), (n2, b1), (n1, b1));
        Petla("A pętla Poznańska–Bydgoska (zgodnie)", (av, b1), (av, b2), (av, b3), (n2, b3), (n2, b1));
        Petla("A pętla Wiśniowa–Poznańska (zgodnie)", (av, b1), (n1, b1), (n1, b3), (av, b3), (av, b2));
        Petla("A pętla uliczka zachodnia (S1)", (av, b1), (s1, b1), (s1, b2), (av, b2));
        Petla("A pętla uliczka wschodnia (S2)", (av, b2), (s2, b2), (s2, b1), (av, b1));
        Petla("A pętla Poznańska–Bydgoska (przeciwnie)", (av, b3), (av, b2), (av, b1), (n2, b1), (n2, b3));
        Petla("A pętla Wiśniowa–Poznańska (przeciwnie)", (av, b3), (n1, b3), (n1, b1), (av, b1), (av, b2));
        l.Add(new Definicja { nazwa = "B zachód → wschód", petla = false, obszar = 1, y = yB, punkty = new[] { new Vector2(bX0, bZ), new Vector2(bNX, bZ), new Vector2(bX1, bZ) } });
        l.Add(new Definicja { nazwa = "B wschód → zachód", petla = false, obszar = 1, y = yB, punkty = new[] { new Vector2(bX1, bZ), new Vector2(bNX, bZ), new Vector2(bX0, bZ) } });
        l.Add(new Definicja { nazwa = "B od parku → zachód", petla = false, obszar = 1, y = yB, punkty = new[] { new Vector2(bNX, bNZ), new Vector2(bNX, bZ), new Vector2(bX0, bZ) } });
        l.Add(new Definicja { nazwa = "B wschód → do parku", petla = false, obszar = 1, y = yB, punkty = new[] { new Vector2(bX1, bZ), new Vector2(bNX, bZ), new Vector2(bNX, bNZ) } });
        return l;
    }
    // ile aut na trasie (po nazwie; domyślnie 1)
    public static int AutNaTrasie(string nazwa) => nazwa.Contains("uliczka") ? 1 : nazwa.StartsWith("B od parku") || nazwa.StartsWith("B wschód → do") ? 1 : 2;

    // krzywa Hermite’a od p0 (kierunek t0) do p1 (kierunek t1); długość stycznych = skala × odległość
    static void Hermite(List<Vector2> pts, Vector2 p0, Vector2 t0, Vector2 p1, Vector2 t1, float skala)
    {
        float L = (p1 - p0).magnitude; var m0 = t0.normalized * L * skala; var m1 = t1.normalized * L * skala;
        int n = Mathf.Max(6, Mathf.CeilToInt(L / .3f));
        for (int k = 1; k <= n; k++)
        {
            float t = k / (float)n, t2 = t * t, t3 = t2 * t;
            var p = (2 * t3 - 3 * t2 + 1) * p0 + (t3 - 2 * t2 + t) * m0 + (-2 * t3 + 3 * t2) * p1 + (t3 - t2) * m1;
            pts.Add(p);
        }
    }
}

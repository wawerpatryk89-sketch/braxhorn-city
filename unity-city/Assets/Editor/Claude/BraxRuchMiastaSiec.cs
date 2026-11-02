using System;
using System.Collections.Generic;
using UnityEngine;

// SIEĆ DRÓG MIASTA (wykonawca „ruch”, 2.11; właściciel 2.11: „sygnalizacja świetna — wszystko musi być w mieście”,
// o osiedlu: „prawdziwe samochody, jak jeżdżą normalnie, ludzie”). Czysty C# (bez UnityEditor i bez sceny) — ten sam kod
// w buildzie i w teście lokalnym poza Unity (harness z buildu 14).
// Wejście: osie jezdni zarejestrowane przez wykonawców (BraxClaudeOsiedle.OsieJezdni z prefiksem „MIASTO ”, kontrakt
// scratchpad/plan/RUCH_API.md). Wyjście:
//  • ulice (odcinki współliniowe sklejone), skrzyżowania (przecięcia osi i T — koniec jezdni przy krawężniku drugiej
//    dociągany do jej osi), końce (ukryte: tunel / kraniec mapy — auta znikają i się pojawiają zawsze; ślepe — tylko poza
//    kamerą), poziomy (pomost wiaduktu y 8.16 to osobna sieć — skrzyżowanie tylko przy |Δy| < 0.5 m);
//  • trasy dla BraxTrasyAut: pętle wokół kwartałów w obu kierunkach (same skręty w prawo / same w lewo), przejazdy między
//    końcami (najkrótsza droga z karą za skręt) aż każdy kierunek każdego odcinka ma ruch; trasa autobusu przez przystanki.
public static class BraxRuchMiastaSiec
{
    public sealed class Czesc { public float a0, a1, chodNeg, chodPos; public bool bezOzn; public string id; }
    public sealed class Droga
    {
        public string id; public bool wzdluzX; public float c, a0, a1, y, cw;
        public bool ukryty0, ukryty1, bezRuchu, bezSwiatel;
        public int kierunek;                         // 0 = dwukierunkowa, +1 = tylko a0 → a1, −1 = tylko a1 → a0
        public int poziom;
        public readonly List<Czesc> czesci = new List<Czesc>();
        public readonly List<string> zrodla = new List<string>();
        public Vector2 P(float a, float d) => wzdluzX ? new Vector2(a, c + d) : new Vector2(c + d, a);
        public float Wzdluz(Vector2 p) => wzdluzX ? p.x : p.y;
        public float Wpoprzek(Vector2 p) => wzdluzX ? p.y - c : p.x - c;
        // chodnik po stronie s (−1: mniejsze z/x, +1: większe) w miejscu a (0 = brak)
        public float Chodnik(int s, float a)
        {
            foreach (var q in czesci) if (a >= q.a0 - .5f && a <= q.a1 + .5f) return s < 0 ? q.chodNeg : q.chodPos;
            return 0f;
        }
        public float ChodnikMax(int s) { float m = 0f; foreach (var q in czesci) m = Mathf.Max(m, s < 0 ? q.chodNeg : q.chodPos); return m; }
        public bool BezOznakowania(float a) { foreach (var q in czesci) if (a >= q.a0 - .5f && a <= q.a1 + .5f) return q.bezOzn; return false; }
    }
    public sealed class Wezel
    {
        public int nr, poziom; public float x, z, y; public Droga dx, dz; public bool E, W, N, S, swiatla;
        public int Ramion => (E ? 1 : 0) + (W ? 1 : 0) + (N ? 1 : 0) + (S ? 1 : 0);
        public Vector2 P => new Vector2(x, z);
    }
    public sealed class Koniec { public int nr; public Droga d; public bool przyA1, ukryty; public Vector2 p; public int poziom; }
    // wierzchołek grafu jazdy: węzeł (skrzyżowanie / zakręt) albo koniec drogi
    public sealed class Wierz { public int nr; public Vector2 p; public Wezel w; public Koniec k; public int poziom; public readonly List<Krawedz> wyj = new List<Krawedz>(); }
    public sealed class Krawedz { public Wierz od, doW; public Droga d; public float dl; public Vector2 kier; public int nr; }
    public sealed class Siec
    {
        public readonly List<Droga> drogi = new List<Droga>();
        public readonly List<Wezel> wezly = new List<Wezel>();
        public readonly List<Koniec> konce = new List<Koniec>();
        public readonly List<Wierz> wierz = new List<Wierz>();
        public readonly List<Krawedz> kraw = new List<Krawedz>();
        public readonly List<float> poziomy = new List<float>();
        public readonly List<string> log = new List<string>();
    }
    public sealed class Przystanek { public string nazwa; public Vector2 p; public Vector2 kier; public float y; public Krawedz k; public float sNaKrawedzi; }
    public sealed class Trasa { public string nazwa; public List<Wierz> w = new List<Wierz>(); public bool petla; public int poziom; public float dl; public bool startUkryty, koniecUkryty; public bool autobus; }

    // ---------------- 1) odczyt rejestracji ----------------
    // id: "MIASTO <nazwa>[; opcje]" — opcje: chodniki L P | ukryty A | ukryty B | jednokierunkowa | bez oznakowania | bez ruchu | bez sygnalizacji
    public static Droga Parsuj(string id, Vector3 a, Vector3 b, float polowa, List<string> log)
    {
        if (string.IsNullOrEmpty(id) || !id.StartsWith("MIASTO ")) return null;
        float dx = Mathf.Abs(a.x - b.x), dz = Mathf.Abs(a.z - b.z);
        if (polowa < 1.5f || polowa > 12f) { log.Add("DO POPRAWKI: jezdnia " + id + " — polowaJezdni " + polowa + " poza 1.5…12 m, pominięta"); return null; }
        bool wx = dz < .05f && dx > 2f, wz = dx < .05f && dz > 2f;
        if (!wx && !wz) { log.Add("DO POPRAWKI: jezdnia " + id + " nie jest równoległa do osi świata (" + a + " → " + b + "), pominięta"); return null; }
        var czesci = id.Substring(7).Split(';');
        var d = new Droga { id = czesci[0].Trim(), wzdluzX = wx, c = wx ? (a.z + b.z) * .5f : (a.x + b.x) * .5f, y = (a.y + b.y) * .5f, cw = polowa };
        float pa = wx ? a.x : a.z, pb = wx ? b.x : b.z;
        bool aNizej = pa < pb;
        d.a0 = Mathf.Min(pa, pb); d.a1 = Mathf.Max(pa, pb);
        // lewa strona patrząc od a do b: (−dz, dx) w (x, z)
        Vector2 kier = new Vector2(b.x - a.x, b.z - a.z).normalized, lewa = new Vector2(-kier.y, kier.x);
        bool lewaPos = wx ? lewa.y > 0f : lewa.x > 0f;
        var cz = new Czesc { a0 = d.a0, a1 = d.a1, id = d.id };
        for (int i = 1; i < czesci.Length; i++)
        {
            var o = czesci[i].Trim().ToLowerInvariant(); if (o.Length == 0) continue;
            var t = o.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (t[0] == "chodniki" && t.Length >= 3 && float.TryParse(t[1].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var L)
                && float.TryParse(t[2].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var P))
            { if (lewaPos) { cz.chodPos = L; cz.chodNeg = P; } else { cz.chodNeg = L; cz.chodPos = P; } }
            else if (t[0] == "ukryty" && t.Length >= 2)
            {
                bool kA = t[1] == "a";
                bool przyA0 = kA == aNizej;   // koniec A leży przy a0, gdy a ma mniejszą współrzędną
                if (przyA0) d.ukryty0 = true; else d.ukryty1 = true;
            }
            else if (o == "jednokierunkowa") d.kierunek = aNizej ? 1 : -1;
            else if (o == "bez oznakowania") cz.bezOzn = true;
            else if (o == "bez ruchu") d.bezRuchu = true;
            else if (o == "bez sygnalizacji") d.bezSwiatel = true;
            else log.Add("DO POPRAWKI: jezdnia " + d.id + " — nieznana opcja „" + o + "”");
        }
        d.czesci.Add(cz); d.zrodla.Add(d.id);
        return d;
    }

    // ---------------- 2) sieć ----------------
    public static Siec Buduj(List<Droga> wejscie, List<(Vector3 srodek, bool swiatla)> wymuszone)
    {
        var s = new Siec();
        // poziomy (y co ≥ 1 m)
        foreach (var d in wejscie)
        {
            int p = s.poziomy.FindIndex(q => Mathf.Abs(q - d.y) < 1f);
            if (p < 0) { s.poziomy.Add(d.y); p = s.poziomy.Count - 1; }
            d.poziom = p;
        }
        // sklejanie odcinków współliniowych (ta sama oś, poziom, szerokość ±0.3, kierunek), stykających się lub nachodzących
        var lista = new List<Droga>(wejscie);
        lista.Sort((p, q) => p.a0.CompareTo(q.a0));
        foreach (var d in lista)
        {
            Droga cel = null;
            foreach (var e in s.drogi)
                if (e.wzdluzX == d.wzdluzX && e.poziom == d.poziom && Mathf.Abs(e.c - d.c) < .1f && Mathf.Abs(e.cw - d.cw) < .3f && e.kierunek == d.kierunek
                    && d.a0 <= e.a1 + .6f && d.a1 >= e.a0 - .6f) { cel = e; break; }
            if (cel == null) { s.drogi.Add(d); continue; }
            if (d.a0 < cel.a0 - .01f) { cel.a0 = d.a0; cel.ukryty0 = d.ukryty0; }
            if (d.a1 > cel.a1 + .01f) { cel.a1 = d.a1; cel.ukryty1 = d.ukryty1; }
            cel.bezRuchu &= d.bezRuchu; cel.bezSwiatel |= d.bezSwiatel; cel.czesci.AddRange(d.czesci); cel.zrodla.AddRange(d.zrodla);
            s.log.Add("jezdnia " + d.id + " sklejona z " + cel.id + " (oś " + (cel.wzdluzX ? "z " : "x ") + cel.c.ToString("F2") + ", " + cel.a0.ToString("F1") + "…" + cel.a1.ToString("F1") + ")");
        }
        foreach (var d in s.drogi) d.czesci.Sort((p, q) => p.a0.CompareTo(q.a0));
        // skrzyżowania: oś X × oś Z na tym samym poziomie; koniec drogi przy krawężniku drugiej (≤ cw + 1.5 m od osi) — dociągnięty do osi
        foreach (var dx in s.drogi)
        {
            if (!dx.wzdluzX) continue;
            foreach (var dz in s.drogi)
            {
                if (dz.wzdluzX || dz.poziom != dx.poziom) continue;
                float x = dz.c, z = dx.c;
                float tolX = dz.cw + 1.5f, tolZ = dx.cw + 1.5f;
                bool naX = x >= dx.a0 - tolX && x <= dx.a1 + tolX, naZ = z >= dz.a0 - tolZ && z <= dz.a1 + tolZ;
                if (!naX || !naZ) continue;
                if (dx.a0 > x) { dx.a0 = x; } if (dx.a1 < x) { dx.a1 = x; }
                if (dz.a0 > z) { dz.a0 = z; } if (dz.a1 < z) { dz.a1 = z; }
                var w = new Wezel { x = x, z = z, y = (dx.y + dz.y) * .5f, dx = dx, dz = dz, poziom = dx.poziom };
                w.E = dx.a1 > x + 1f; w.W = dx.a0 < x - 1f; w.N = dz.a1 > z + 1f; w.S = dz.a0 < z - 1f;
                if (w.Ramion < 2) continue;
                bool wym = false, wymSw = false;
                foreach (var (c, sw) in wymuszone) if (new Vector2(c.x - x, c.z - z).magnitude < Mathf.Max(dx.cw, dz.cw) + 3f && Mathf.Abs(c.y - w.y) < 1.5f) { wym = true; wymSw = sw; }
                w.swiatla = w.Ramion >= 3 && !dx.bezSwiatel && !dz.bezSwiatel && (!wym || wymSw);
                if (wym && wymSw && w.Ramion < 3) s.log.Add("skrzyżowanie (" + x + ", " + z + ") ma tylko " + w.Ramion + " ramiona — bez sygnalizacji (zakręt)");
                w.nr = s.wezly.Count; s.wezly.Add(w);
            }
        }
        // końce dróg (poza węzłami)
        foreach (var d in s.drogi)
            foreach (bool a1 in new[] { false, true })
            {
                float a = a1 ? d.a1 : d.a0;
                var p = d.P(a, 0f);
                bool wWezle = false;
                foreach (var w in s.wezly) if ((w.dx == d || w.dz == d) && (w.P - p).magnitude < .5f) wWezle = true;
                if (wWezle) continue;
                var k = new Koniec { d = d, przyA1 = a1, ukryty = a1 ? d.ukryty1 : d.ukryty0, p = p, poziom = d.poziom, nr = s.konce.Count };
                s.konce.Add(k);
            }
        // graf jazdy
        foreach (var w in s.wezly) s.wierz.Add(new Wierz { nr = s.wierz.Count, p = w.P, w = w, poziom = w.poziom });
        foreach (var k in s.konce) s.wierz.Add(new Wierz { nr = s.wierz.Count, p = k.p, k = k, poziom = k.poziom });
        foreach (var d in s.drogi)
        {
            if (d.bezRuchu) continue;
            var na = new List<(float a, Wierz v)>();
            foreach (var v in s.wierz)
            {
                if (v.poziom != d.poziom) continue;
                bool nalezy = v.w != null ? (v.w.dx == d || v.w.dz == d) : v.k.d == d;
                if (nalezy) na.Add((d.Wzdluz(v.p), v));
            }
            na.Sort((p, q) => p.a.CompareTo(q.a));
            for (int i = 0; i + 1 < na.Count; i++)
            {
                var u = na[i].v; var v = na[i + 1].v; float dl = na[i + 1].a - na[i].a;
                if (dl < .5f) continue;
                if (d.kierunek >= 0) DodajKrawedz(s, u, v, d, dl);
                if (d.kierunek <= 0) DodajKrawedz(s, v, u, d, dl);
            }
        }
        int swiatla = 0; foreach (var w in s.wezly) if (w.swiatla) swiatla++;
        s.log.Add("sieć: dróg " + s.drogi.Count + " (z " + wejscie.Count + " rejestracji), poziomów " + s.poziomy.Count + ", skrzyżowań " + s.wezly.Count + " (ze światłami " + swiatla + "), końców " + s.konce.Count + " (ukrytych " + s.konce.FindAll(k => k.ukryty).Count + "), odcinków jazdy " + s.kraw.Count);
        return s;
    }
    static void DodajKrawedz(Siec s, Wierz u, Wierz v, Droga d, float dl)
    {
        var k = new Krawedz { od = u, doW = v, d = d, dl = dl, kier = (v.p - u.p).normalized, nr = s.kraw.Count };
        s.kraw.Add(k); u.wyj.Add(k);
    }

    // ---------------- 3) trasy ----------------
    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    // Kwartały: obchodzenie ścian grafu płaskiego (zawsze najostrzej w lewo) — ściany wewnętrzne mają pole dodatnie.
    public static List<Trasa> Petle(Siec s, int maxWezlow = 10)
    {
        var wyn = new List<Trasa>();
        var bylo = new HashSet<(int, int)>();
        // nieskierowany graf węzłów (bez końców)
        var sas = new Dictionary<int, List<int>>();
        var drogaMiedzy = new Dictionary<(int, int), Droga>();
        foreach (var k in s.kraw)
        {
            if (k.od.w == null || k.doW.w == null) continue;
            if (!sas.TryGetValue(k.od.nr, out var l1)) sas[k.od.nr] = l1 = new List<int>();
            if (!sas.TryGetValue(k.doW.nr, out var l2)) sas[k.doW.nr] = l2 = new List<int>();
            if (!l1.Contains(k.doW.nr)) l1.Add(k.doW.nr);
            if (!l2.Contains(k.od.nr)) l2.Add(k.od.nr);
            drogaMiedzy[(k.od.nr, k.doW.nr)] = k.d; drogaMiedzy[(k.doW.nr, k.od.nr)] = k.d;
        }
        foreach (var start in sas.Keys)
            foreach (var nast in sas[start])
            {
                if (bylo.Contains((start, nast))) continue;
                var cyk = new List<int> { start }; int u = start, v = nast; bool ok = true;
                for (int krok = 0; krok < 40; krok++)
                {
                    bylo.Add((u, v));
                    if (v == start) break;
                    cyk.Add(v);
                    var din = (s.wierz[v].p - s.wierz[u].p).normalized;
                    int best = -1; float bestKat = -1e9f;
                    foreach (var w in sas[v])
                    {
                        if (w == u && sas[v].Count > 1) continue;
                        var dout = (s.wierz[w].p - s.wierz[v].p).normalized;
                        float kat = Mathf.Atan2(Cross(din, dout), Vector2.Dot(din, dout));   // + = w lewo
                        if (kat > bestKat) { bestKat = kat; best = w; }
                    }
                    if (best < 0) { ok = false; break; }
                    u = v; v = best;
                }
                if (!ok || v != start || cyk.Count < 3 || cyk.Count > maxWezlow) continue;
                float pole = 0f;
                for (int i = 0; i < cyk.Count; i++) { var a = s.wierz[cyk[i]].p; var b = s.wierz[cyk[(i + 1) % cyk.Count]].p; pole += a.x * b.y - b.x * a.y; }
                if (pole <= 1f) continue;   // ściana zewnętrzna albo zdegenerowana
                // tylko wierzchołki narożne (zmiana kierunku) — prosto przez węzeł BraxTrasyAut wstawi sam
                foreach (int kier in new[] { 1, -1 })
                {
                    var seq = new List<int>(cyk); if (kier < 0) seq.Reverse();
                    bool dozw = true;
                    for (int i = 0; i < seq.Count && dozw; i++)
                    {
                        int a = seq[i], b = seq[(i + 1) % seq.Count];
                        bool jest = false; foreach (var k in s.wierz[a].wyj) if (k.doW.nr == b) jest = true;
                        dozw = jest;
                    }
                    if (!dozw) continue;
                    var t = new Trasa { petla = true, poziom = s.wierz[seq[0]].poziom, nazwa = "pętla " + (kier > 0 ? "w lewo (przeciwnie do wskazówek)" : "w prawo (zgodnie ze wskazówkami)") + " wokół (" + Srodek(s, seq) + ")" };
                    foreach (int i in seq) t.w.Add(s.wierz[i]);
                    t.dl = Dlugosc(t);
                    wyn.Add(t);
                }
            }
        return wyn;
    }
    static string Srodek(Siec s, List<int> seq) { Vector2 c = Vector2.zero; foreach (int i in seq) c += s.wierz[i].p; c /= seq.Count; return c.x.ToString("F0") + ", " + c.y.ToString("F0"); }
    static float Dlugosc(Trasa t)
    {
        float L = 0f; int n = t.w.Count;
        for (int i = 0; i + (t.petla ? 0 : 1) < n; i++) L += (t.w[(i + 1) % n].p - t.w[i].p).magnitude;
        return L;
    }

    // Najkrótsza droga (Dijkstra) od wierzchołka (wjeżdżając krawędzią `zKraw`, może być null) do celu; kara za skręt 25 m,
    // zakaz zawracania. Zwraca listę krawędzi.
    public static List<Krawedz> Droga1(Siec s, Wierz od, Krawedz zKraw, Wierz cel, Krawedz celKraw = null)
    {
        // stan = krawędź, którą wjechano do wierzchołka
        var dist = new Dictionary<int, float>(); var prev = new Dictionary<int, int>();
        var q = new List<(float d, int k)>();
        foreach (var k in od.wyj)
        {
            if (zKraw != null && Vector2.Dot(zKraw.kier, k.kier) < -.9f) continue;
            float kara = zKraw != null && Vector2.Dot(zKraw.kier, k.kier) < .9f ? 25f : 0f;
            float d0 = k.dl + kara; if (!dist.TryGetValue(k.nr, out var dd) || d0 < dd) { dist[k.nr] = d0; prev[k.nr] = -1; q.Add((d0, k.nr)); }
        }
        int koniec = -1;
        while (q.Count > 0)
        {
            int bi = 0; for (int i = 1; i < q.Count; i++) if (q[i].d < q[bi].d) bi = i;
            var (d, kn) = q[bi]; q.RemoveAt(bi);
            if (d > dist[kn] + 1e-3f) continue;
            var k = s.kraw[kn];
            if (celKraw != null ? kn == celKraw.nr : k.doW == cel) { koniec = kn; break; }
            foreach (var n in k.doW.wyj)
            {
                float dot = Vector2.Dot(k.kier, n.kier); if (dot < -.9f) continue;
                float nd = d + n.dl + (dot < .9f ? 25f : 0f);
                if (!dist.TryGetValue(n.nr, out var old) || nd < old - 1e-3f) { dist[n.nr] = nd; prev[n.nr] = kn; q.Add((nd, n.nr)); }
            }
        }
        if (koniec < 0) return null;
        var wyn = new List<Krawedz>();
        for (int c = koniec; c >= 0; c = prev[c]) wyn.Add(s.kraw[c]);
        wyn.Reverse();
        return wyn;
    }

    // Przejazdy między końcami: najpierw z ukrytych do ukrytych, potem z/do ślepych; zachłannie — trasa, która pokrywa
    // najwięcej jeszcze nieobjętych kierunków odcinków; do `max` tras.
    public static List<Trasa> Przejazdy(Siec s, HashSet<int> pokryte, int max)
    {
        var wyn = new List<Trasa>();
        var kand = new List<(Trasa t, List<Krawedz> k)>();
        foreach (var a in s.wierz)
        {
            if (a.k == null) continue;
            foreach (var b in s.wierz)
            {
                if (b.k == null || b == a || b.poziom != a.poziom) continue;
                var dr = Droga1(s, a, null, b); if (dr == null || dr.Count == 0) continue;
                var t = new Trasa { petla = false, poziom = a.poziom, startUkryty = a.k.ukryty, koniecUkryty = b.k.ukryty };
                t.w.Add(a); foreach (var k in dr) t.w.Add(k.doW);
                t.dl = 0f; foreach (var k in dr) t.dl += k.dl;
                t.nazwa = "przejazd " + a.k.d.id + (a.k.przyA1 ? " (koniec b)" : " (koniec a)") + " → " + b.k.d.id + (b.k.przyA1 ? " (koniec b)" : " (koniec a)");
                kand.Add((t, dr));
            }
        }
        while (wyn.Count < max && kand.Count > 0)
        {
            int bi = -1; float bo = 0f;
            for (int i = 0; i < kand.Count; i++)
            {
                float nowe = 0f; foreach (var k in kand[i].k) if (!pokryte.Contains(k.nr)) nowe += k.dl;
                // wolimy ukryte końce (auta znikają poza kadrem) i dłuższe przejazdy
                float ocena = nowe * (kand[i].t.startUkryty ? 1.3f : 1f) * (kand[i].t.koniecUkryty ? 1.3f : 1f);
                if (ocena > bo + 1e-3f) { bo = ocena; bi = i; }
            }
            if (bi < 0) break;
            foreach (var k in kand[bi].k) pokryte.Add(k.nr);
            wyn.Add(kand[bi].t); kand.RemoveAt(bi);
        }
        return wyn;
    }

    // Krawędź jazdy, na której leży przystanek (punkt przy krawężniku, kierunek jazdy).
    public static void Przypnij(Siec s, Przystanek p)
    {
        float best = 1e9f;
        foreach (var k in s.kraw)
        {
            if (Vector2.Dot(k.kier, p.kier) < .9f) continue;
            if (Mathf.Abs(s.poziomy[k.d.poziom] - p.y) > 1.5f) continue;
            float a = k.d.Wzdluz(p.p), d = Mathf.Abs(k.d.Wpoprzek(p.p));
            float a0 = Mathf.Min(k.d.Wzdluz(k.od.p), k.d.Wzdluz(k.doW.p)), a1 = Mathf.Max(k.d.Wzdluz(k.od.p), k.d.Wzdluz(k.doW.p));
            if (a < a0 - 1f || a > a1 + 1f || d > k.d.cw + 1.5f) continue;
            if (d < best) { best = d; p.k = k; p.sNaKrawedzi = Mathf.Abs(a - k.d.Wzdluz(k.od.p)); }
        }
    }

    // Trasa autobusu: przez wszystkie przystanki (kolejność — najkrótsza z permutacji ≤ 5 przystanków). Z ukrytego końca do
    // ukrytego (albo ślepego) końca; gdy końców brak — pętla.
    public static Trasa Autobus(Siec s, List<Przystanek> przyst)
    {
        var ok = przyst.FindAll(p => p.k != null);
        if (ok.Count == 0) return null;
        if (ok.Count > 5) ok = ok.GetRange(0, 5);
        Trasa best = null; float bestL = 1e9f;
        foreach (var perm in Permutacje(ok.Count))
        {
            var kol = new List<Przystanek>(); foreach (int i in perm) kol.Add(ok[i]);
            // a) z końca do końca
            foreach (var a in s.wierz)
            {
                if (a.k == null || a.poziom != kol[0].k.d.poziom) continue;
                foreach (var b in s.wierz)
                {
                    if (b.k == null || b == a || b.poziom != a.poziom) continue;
                    var t = Lancuch(s, a, b, kol, false);
                    if (t != null) { float L = t.dl * (a.k.ukryty ? 1f : 1.4f) * (b.k.ukryty ? 1f : 1.4f); if (L < bestL) { bestL = L; best = t; } }
                }
            }
            // b) pętla
            if (best == null) { var t = Lancuch(s, null, null, kol, true); if (t != null && t.dl < bestL) { bestL = t.dl; best = t; } }
        }
        if (best != null) { best.autobus = true; best.nazwa = "autobus: " + best.nazwa; }
        return best;
    }
    static Trasa Lancuch(Siec s, Wierz a, Wierz b, List<Przystanek> kol, bool petla)
    {
        var kraw = new List<Krawedz>();
        Wierz cur = petla ? kol[0].k.doW : a; Krawedz zK = petla ? kol[0].k : null;
        int start = petla ? 1 : 0;
        for (int i = start; i < kol.Count + (petla ? 1 : 0); i++)
        {
            var p = kol[i % kol.Count];
            if (cur == p.k.od && (zK == null || Vector2.Dot(zK.kier, p.k.kier) > -.9f)) { kraw.Add(p.k); }
            else
            {
                var dr = Droga1(s, cur, zK, null, p.k); if (dr == null) return null;
                kraw.AddRange(dr);
            }
            cur = p.k.doW; zK = p.k;
        }
        if (!petla)
        {
            if (cur != b) { var dr = Droga1(s, cur, zK, b); if (dr == null) return null; kraw.AddRange(dr); }
        }
        else
        {
            // domknięcie: koniec łańcucha musi wrócić do początku krawędzi pierwszego przystanku
            if (kraw.Count == 0 || kraw[kraw.Count - 1].doW != kol[0].k.od) { var dr = Droga1(s, cur, zK, null, kol[0].k); if (dr == null) return null; dr.RemoveAt(dr.Count - 1); kraw.AddRange(dr); }
            // pętla bez powtórzeń krawędzi (BraxTrasyAut: punkty muszą być węzłami)
            var seen = new HashSet<int>(); foreach (var k in kraw) if (!seen.Add(k.nr)) return null;
            foreach (var k in kraw) if (k.od.w == null) return null;
        }
        var t = new Trasa { petla = petla, poziom = kol[0].k.d.poziom };
        if (!petla) { t.w.Add(a); t.startUkryty = a.k.ukryty; t.koniecUkryty = b.k.ukryty; }
        foreach (var k in kraw) t.w.Add(k.doW);
        if (petla) { t.w.RemoveAt(t.w.Count - 1); t.w.Insert(0, kraw[kraw.Count - 1].doW); t.w.RemoveAt(0); t.w.Insert(0, kol[0].k.od); }
        t.dl = 0f; foreach (var k in kraw) t.dl += k.dl;
        t.nazwa = petla ? "pętla przez " + kol.Count + " przystanki" : a.k.d.id + " → " + b.k.d.id + " przez " + kol.Count + " przyst.";
        return t;
    }
    static IEnumerable<int[]> Permutacje(int n)
    {
        var a = new int[n]; for (int i = 0; i < n; i++) a[i] = i;
        yield return (int[])a.Clone();
        while (true)
        {
            int i = n - 2; while (i >= 0 && a[i] >= a[i + 1]) i--;
            if (i < 0) yield break;
            int j = n - 1; while (a[j] <= a[i]) j--;
            int t = a[i]; a[i] = a[j]; a[j] = t;
            Array.Reverse(a, i + 1, n - i - 1);
            yield return (int[])a.Clone();
        }
    }

    // Uproszczenie: tylko wierzchołki, w których trasa skręca, plus końce (BraxTrasyAut sam wstawia węzły „prosto”).
    public static Vector2[] Punkty(Trasa t)
    {
        var l = new List<Vector2>(); int n = t.w.Count;
        for (int i = 0; i < n; i++)
        {
            bool koniec = !t.petla && (i == 0 || i == n - 1);
            if (!koniec)
            {
                var a = t.w[(i - 1 + n) % n].p; var b = t.w[i].p; var c = t.w[(i + 1) % n].p;
                if (Mathf.Abs(Cross((b - a).normalized, (c - b).normalized)) < .05f && Vector2.Dot(b - a, c - b) > 0f) continue;
            }
            l.Add(t.w[i].p);
        }
        return l.ToArray();
    }

    // Ulice i węzły dla BraxTrasyAut (jeden poziom). half = półszerokość jezdni + krawężnik + szerszy chodnik.
    public static void DlaTras(Siec s, int poziom, List<BraxTrasyAut.Ulica> ul, List<BraxTrasyAut.Wezel> wz, Dictionary<Droga, BraxTrasyAut.Ulica> mapa)
    {
        foreach (var d in s.drogi)
        {
            if (d.poziom != poziom) continue;
            var u = new BraxTrasyAut.Ulica { id = d.id, wzdluzX = d.wzdluzX, c = d.c, a0 = d.a0, a1 = d.a1, cw = d.cw, half = d.cw + .25f + Mathf.Max(d.ChodnikMax(-1), d.ChodnikMax(1), .6f) };
            ul.Add(u); mapa[d] = u;
        }
        foreach (var w in s.wezly)
        {
            if (w.poziom != poziom) continue;
            wz.Add(new BraxTrasyAut.Wezel { x = w.x, z = w.z, ux = mapa[w.dx], uz = mapa[w.dz], rodzaj = w.swiatla ? 2 : 0 });
        }
    }

    // Czy punkt (styk koła) leży na jezdni danego poziomu (test ruchu „poza drogą”).
    public static bool NaJezdni(Siec s, Vector3 p, float tol = .2f)
    {
        foreach (var d in s.drogi)
        {
            if (Mathf.Abs(s.poziomy[d.poziom] - p.y) > 2f) continue;
            float a = d.wzdluzX ? p.x : p.z, q = d.wzdluzX ? p.z : p.x;
            if (a >= d.a0 - tol && a <= d.a1 + tol && Mathf.Abs(q - d.c) <= d.cw + tol) return true;
        }
        return false;
    }
}

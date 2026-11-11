using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// DWORZEC R1 — DROGI I PLAC (11.11.2026). Skala (zasada globalna 11.11 „skala, skala i skala”, K = 1.294): pas ruchu 3.5 m × K =
// 4.53 m, jezdnia ul. Dworcowej 9.06 m; autobus 12 m × K = 15.5 m (szer. 3.3, wys. 4.1), promień skrętu zewn. 16.2 / wewn. 6.9 m
// → łuki ul. Dworcowej R 14 w osi (krawędzie 9.47 / 18.53 — autobus przy R zewn. 18.5 zajmuje ok. 8.5 m), pętla: kwadrat 33.6 m z wyspą R 6.0 (okrąg wpisany R 16.8 ≥ 16.2;
// wyspa 6.0 ≤ 6.9). Zatoka BUS: wjazd 4 m + 2 stanowiska × 18.5 m (autobus + 3 m) + wyjazd 4 m, szerokość 4.53 m. Zatoka TAXI/K+R:
// 30 m. Przejście dla pieszych 4 m × K = 5.2 m, pasy 0.65 m równoległe do osi jezdni. Łączniki do faweli 2 × 3.5 m (lokalne).
internal static partial class BraxMiastoDworzec
{
    // ---------------- oś drogi: próbki z kierunkiem
    internal sealed class Os
    {
        public readonly List<Vector2> p = new List<Vector2>(), t = new List<Vector2>();
        public Func<Vector2, float> y = q => YJ;
        public int N => p.Count;
        public Vector2 L(int i) => new Vector2(-t[i].y, t[i].x);   // lewa strona kierunku jazdy
    }

    internal static Os Prosta(Os o, Vector2 a, Vector2 b, float krok = 2f)
    {
        var d = b - a; float len = d.magnitude; if (len < 1e-4f) return o;
        var t = d / len; int n = Mathf.Max(1, Mathf.CeilToInt(len / krok));
        for (int i = 0; i <= n; i++)
        {
            var q = Vector2.Lerp(a, b, i / (float)n);
            if (i == 0 && o.N > 0 && (o.p[o.N - 1] - q).sqrMagnitude < 1e-4f) { o.t[o.N - 1] = t; continue; }
            o.p.Add(q); o.t.Add(t);
        }
        return o;
    }

    /// <summary>Łuk środek c, promień R, kąty a0 → a1 (stopnie, 0 = +x, 90 = +z); kierunek jazdy wg kolejności kątów.</summary>
    internal static Os Luk(Os o, Vector2 c, float R, float a0, float a1)
    {
        int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 3f)); float sg = Mathf.Sign(a1 - a0);
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad;
            var q = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R; var t = new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * sg;
            if (i == 0 && o.N > 0 && (o.p[o.N - 1] - q).sqrMagnitude < 1e-4f) { o.t[o.N - 1] = t; continue; }
            o.p.Add(q); o.t.Add(t);
        }
        return o;
    }

    /// <summary>Pas wzdłuż osi między odsunięciami d0…d1 (dodatnie = lewa strona), wierzch na wysokości yf(i); bok zewnętrzny opcjonalnie.</summary>
    static void PasWzdluz(Os o, Material m, float d0, float d1, Func<int, float> yf, float bokDo = float.NaN)
    {
        var s = S(m);
        for (int i = 0; i + 1 < o.N; i++)
        {
            Vector2 l0 = o.L(i), l1 = o.L(i + 1);
            Vector3 P(int k, Vector2 l, float d) => new Vector3(o.p[k].x + l.x * d, yf(k), o.p[k].y + l.y * d);
            s.Quad(P(i, l0, d0), P(i + 1, l1, d0), P(i + 1, l1, d1), P(i, l0, d1), Vector3.up);
            Pokryj(XZ(P(i, l0, d0)), XZ(P(i + 1, l1, d0)), XZ(P(i + 1, l1, d1)), XZ(P(i, l0, d1)));
            if (!float.IsNaN(bokDo))
            {
                float de = Mathf.Abs(d1) > Mathf.Abs(d0) ? d1 : d0; var lo = Mathf.Sign(de) * l0; var lo1 = Mathf.Sign(de) * l1;
                Vector3 a = P(i, l0, de), b = P(i + 1, l1, de);
                s.Quad(a, b, new Vector3(b.x, bokDo, b.z), new Vector3(a.x, bokDo, a.z), new Vector3((lo + lo1).x, 0f, (lo + lo1).y));
            }
        }
    }

    /// <summary>Krawężnik po stronie sg (+1 lewa, −1 prawa) przy krawędzi jezdni hw: granit, 15 cm nad jezdnią, lico od jezdni.</summary>
    static void Kraweznik(Os o, float hw, int sg)
    {
        Func<int, float> top = i => o.y(o.p[i]) + .15f;
        PasWzdluz(o, granitC, sg * hw, sg * (hw + Kr), top, YC - .12f);
        var s = S(granitC);
        for (int i = 0; i + 1 < o.N; i++)
        {
            Vector2 l0 = o.L(i) * sg, l1 = o.L(i + 1) * sg;
            var a = new Vector3(o.p[i].x + l0.x * hw, o.y(o.p[i]), o.p[i].y + l0.y * hw); var b = new Vector3(o.p[i + 1].x + l1.x * hw, o.y(o.p[i + 1]), o.p[i + 1].y + l1.y * hw);
            s.Quad(a, b, b + Vector3.up * .15f, a + Vector3.up * .15f, new Vector3(-(l0 + l1).x, 0f, -(l0 + l1).y));
        }
    }

    static void Chodnik(Os o, float hw, int sg, float szer) => PasWzdluz(o, chodnik, sg * (hw + Kr), sg * (hw + Kr + szer), i => o.y(o.p[i]) + .15f, YC - .12f);
    static void Asfalt(Os o, float hw) => PasWzdluz(o, asfalt, -hw, hw, i => o.y(o.p[i]));

    static void ZajmijOs(Os o, float pol)
    {
        foreach (var q in o.p) ZajeteKola.Add((q, pol));
    }

    /// <summary>Linia przerywana w osi (biała, w wycięciu asfaltu — ta sama płaszczyzna): kreska 3 m, przerwa 3 m.
    /// Bez kresek na przejściu dla pieszych.</summary>
    static void OsPrzerywana(Os o, float od = 0f, float doS = float.MaxValue)
    {
        float s = 0f;
        for (int i = 0; i + 1 < o.N; i++)
        {
            float len = Vector2.Distance(o.p[i], o.p[i + 1]);
            var srod = (o.p[i] + o.p[i + 1]) * .5f;
            bool zebra = Mathf.Abs(srod.x - ZebraX) < ZebraSz * .5f + .6f && Mathf.Abs(srod.y - DrogaZ) < Pas + .5f;
            if (s >= od && s <= doS && ((int)(s / 3f)) % 2 == 0 && !zebra)
            {
                Vector2 l0 = o.L(i) * .06f, l1 = o.L(i + 1) * .06f;
                Farba(farba, o.p[i] - l0, o.p[i + 1] - l1, o.p[i + 1] + l1, o.p[i] + l0);
            }
            s += len;
        }
    }

    // ============================================================================ FARBA W WYCIĘCIACH ASFALTU
    // Wzór miasta B (BudujAsfalt, 10.11): oznakowanie leży DOKŁADNIE w płaszczyźnie asfaltu, asfalt ma w tym miejscu otwór —
    // styk krawędzią, bez dwóch powierzchni jedna nad drugą (zasada „zero migotania, zakaz nakładania”). Farba: wielokąty wypukłe
    // w XZ (kreski osi, pasy zebry, trójkąty liter BUS / TAXI / K+R); wysokość z trójkąta asfaltu, w którym leżą.
    internal static readonly List<(Vector2[] w, Material m)> Farby = new List<(Vector2[], Material)>();
    const float ZebraX = -14f; static float ZebraSz => 4f * K;
    static Vector2 XZ(Vector3 p) => new Vector2(p.x, p.z);
    static float Pole2(IList<Vector2> w) { float a = 0f; for (int i = 0; i < w.Count; i++) { var p = w[i]; var q = w[(i + 1) % w.Count]; a += p.x * q.y - q.x * p.y; } return a * .5f; }
    /// <summary>Otwór bez farby (pole napisu — wypełniane pasami asfalt / farba w NapisNaJezdni).</summary>
    static void Otwor(params Vector2[] w) => Farba(null, w);
    static bool Nachodza(List<Vector2> a, Vector2[] b)
    {
        // SAT dla wielokątów wypukłych: rozdzielone na którejś osi → brak części wspólnej
        bool Rozdziel(IList<Vector2> p, IList<Vector2> q)
        {
            for (int i = 0; i < p.Count; i++)
            {
                var e = p[(i + 1) % p.Count] - p[i]; var nrm = new Vector2(-e.y, e.x);
                float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                foreach (var v in p) { float d = Vector2.Dot(v, nrm); a0 = Mathf.Min(a0, d); a1 = Mathf.Max(a1, d); }
                foreach (var v in q) { float d = Vector2.Dot(v, nrm); b0 = Mathf.Min(b0, d); b1 = Mathf.Max(b1, d); }
                if (a1 <= b0 + 1e-6f || b1 <= a0 + 1e-6f) return true;
            }
            return false;
        }
        return !Rozdziel(a, b) && !Rozdziel(b, a);
    }
    static void Farba(Material m, params Vector2[] w)
    {
        if (Mathf.Abs(Pole2(w)) < 1e-5f) return;
        if (Pole2(w) < 0f) System.Array.Reverse(w);
        Farby.Add((w, m));
    }
    // część wielokąta po lewej (lewa = true) albo prawej stronie prostej a→b
    static List<Vector2> Przytnij(List<Vector2> w, Vector2 a, Vector2 b, bool lewa)
    {
        var o = new List<Vector2>(); if (w.Count == 0) return o;
        float S2(Vector2 p) { float c = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x); return lewa ? c : -c; }
        for (int i = 0; i < w.Count; i++)
        {
            var cur = w[i]; var prv = w[(i + w.Count - 1) % w.Count]; float sc = S2(cur), sp = S2(prv);
            if (sc >= 0f) { if (sp < 0f) o.Add(Vector2.Lerp(prv, cur, sp / (sp - sc))); o.Add(cur); }
            else if (sp >= 0f) o.Add(Vector2.Lerp(prv, cur, sp / (sp - sc)));
        }
        return o;
    }
    /// <summary>Wielokąt wypukły minus wypukły (CCW): kawałki wypukłe na zewnątrz; wspolna = część wspólna.</summary>
    static List<List<Vector2>> Odejmij(List<Vector2> a, Vector2[] b, out List<Vector2> wspolna)
    {
        var wy = new List<List<Vector2>>(); var reszta = a;
        for (int i = 0; i < b.Length && reszta.Count >= 3; i++)
        {
            Vector2 p = b[i], q = b[(i + 1) % b.Length];
            var zew = Przytnij(reszta, p, q, false); if (zew.Count >= 3 && Mathf.Abs(Pole2(zew)) > 1e-6f) wy.Add(zew);
            reszta = Przytnij(reszta, p, q, true);
        }
        wspolna = reszta.Count >= 3 && Mathf.Abs(Pole2(reszta)) > 1e-6f ? reszta : null;
        if (wspolna == null && wy.Count == 0) wy.Add(a);
        return wy;
    }
    /// <summary>Asfalt partii „nawierzchnie: jezdnie” przebudowany: trójkąty bez farby + farba w otworach (ta sama płaszczyzna).</summary>
    static void WytnijFarbe()
    {
        Grupa("nawierzchnie: jezdnie");
        var sA = S(asfalt);
        var tri = new List<(Vector3 a, Vector3 b, Vector3 c)>();
        for (int k = 0; k + 2 < sA.i.Count; k += 3) tri.Add((sA.v[sA.i[k]], sA.v[sA.i[k + 1]], sA.v[sA.i[k + 2]]));
        sA.v.Clear(); sA.n.Clear(); sA.t.Clear(); sA.uv.Clear(); sA.c.Clear(); sA.i.Clear();
        var bb = Farby.Select(f => (x0: f.w.Min(q => q.x), x1: f.w.Max(q => q.x), z0: f.w.Min(q => q.y), z1: f.w.Max(q => q.y))).ToArray();
        Grupa("nawierzchnie: oznakowanie");
        int kaw = 0;
        foreach (var (a, b, c) in tri)
        {
            var nn = Vector3.Cross(b - a, c - a); if (nn.y < 0f) nn = -nn; if (nn.sqrMagnitude < 1e-12f) continue; nn.Normalize();
            float Y(Vector2 p) => a.y - (nn.x * (p.x - a.x) + nn.z * (p.y - a.z)) / nn.y;
            var t2 = new List<Vector2> { XZ(a), XZ(b), XZ(c) }; if (Pole2(t2) < 0f) t2.Reverse();
            float tx0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), tx1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x)), tz0 = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), tz1 = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
            var kawalki = new List<List<Vector2>> { t2 };
            for (int f = 0; f < Farby.Count; f++)
            {
                if (bb[f].x1 <= tx0 || bb[f].x0 >= tx1 || bb[f].z1 <= tz0 || bb[f].z0 >= tz1) continue;
                var nowe = new List<List<Vector2>>();
                foreach (var k in kawalki)
                {
                    if (!Nachodza(k, Farby[f].w)) { nowe.Add(k); continue; }
                    var wy = Odejmij(k, Farby[f].w, out var wsp);
                    nowe.AddRange(wy);
                    if (wsp != null && Farby[f].m != null) { Wachlarz(S(Farby[f].m), wsp, Y, nn); kaw++; }
                }
                kawalki = nowe;
            }
            Grupa("nawierzchnie: jezdnie");
            foreach (var k in kawalki) Wachlarz(sA, k, Y, nn);
            Grupa("nawierzchnie: oznakowanie");
        }
        Grupa("nawierzchnie: jezdnie");
        Opis("oznakowanie: " + Farby.Count + " wielokątów farby w wycięciach asfaltu (" + kaw + " kawałków, bez nakładania)");
    }
    /// <summary>Napis farbą na płaskiej jezdni (y = o.y): otwor = true — tylko prostokąt pola do wycięcia; false — wypełnienie pola
    /// pasami poziomymi (rozkład trapezowy): w każdym pasie przedziały liter = farba, reszta = asfalt (te same wierzchołki na stykach).</summary>
    static void NapisNaJezdni(string tekst, Vector3 o, Vector3 r, Vector3 up, float cap, float track, bool otwor)
    {
        r = r.normalized; up = up.normalized;
        var tri = BraxLiteryBebas.Trojkaty(tekst, o, r, up, cap, track)
            .Select(q => q.Select(p => new Vector2(Vector3.Dot(p - o, r), Vector3.Dot(p - o, up))).ToArray()).ToList();
        if (tri.Count == 0) return;
        float u0 = tri.Min(q => q.Min(p => p.x)) - .15f, u1 = tri.Max(q => q.Max(p => p.x)) + .15f, v0 = tri.Min(q => q.Min(p => p.y)) - .15f, v1 = tri.Max(q => q.Max(p => p.y)) + .15f;
        Vector3 W(float u, float v) => o + r * u + up * v;
        if (otwor) { Otwor(XZ(W(u0, v0)), XZ(W(u1, v0)), XZ(W(u1, v1)), XZ(W(u0, v1))); return; }
        var vs = new List<float> { v0, v1 }; foreach (var q in tri) foreach (var p in q) vs.Add(p.y);
        vs.Sort(); var pasy = new List<float>(); foreach (var v in vs) if (pasy.Count == 0 || v - pasy[pasy.Count - 1] > 1e-4f) pasy.Add(v);
        Grupa("nawierzchnie: jezdnie"); var sA = S(asfalt); Grupa("nawierzchnie: oznakowanie"); var sF = S(farbaZ);
        void Trapez(UlSiatka s, float la, float ra, float lb, float rb, float va, float vb)
        {
            if ((ra - la) < 1e-4f && (rb - lb) < 1e-4f) return;
            var p = new List<Vector3> { W(la, va), W(ra, va), W(rb, vb), W(lb, vb) };
            if ((ra - la) < 1e-4f) p.RemoveAt(1); else if ((rb - lb) < 1e-4f) p.RemoveAt(3);
            for (int j = 1; j + 1 < p.Count; j++) s.Tri(p[0], p[j], p[j + 1], Vector3.up);
        }
        for (int k = 0; k + 1 < pasy.Count; k++)
        {
            float va = pasy[k], vb = pasy[k + 1], vm = (va + vb) * .5f;
            var przedz = new List<(float la, float lb, float ra, float rb, float lm)>();
            foreach (var q in tri)
            {
                var kr = new List<(float a, float b, float m)>();
                for (int e = 0; e < 3; e++)
                {
                    Vector2 p = q[e], qq = q[(e + 1) % 3];
                    if ((p.y - vm) * (qq.y - vm) >= 0f) continue;
                    float U(float v) => p.x + (qq.x - p.x) * (v - p.y) / (qq.y - p.y);
                    kr.Add((U(va), U(vb), U(vm)));
                }
                if (kr.Count != 2) continue;
                if (kr[0].m > kr[1].m) kr.Reverse();
                przedz.Add((kr[0].a, kr[0].b, kr[1].a, kr[1].b, kr[0].m));
            }
            przedz.Sort((x, y) => x.lm.CompareTo(y.lm));
            // scalanie przedziałów stykających się wspólną krawędzią (trójkąty jednej litery)
            var scal = new List<(float la, float lb, float ra, float rb)>();
            foreach (var p in przedz)
            {
                if (scal.Count > 0) { var c = scal[scal.Count - 1]; if (Mathf.Abs(c.ra - p.la) < 1e-4f && Mathf.Abs(c.rb - p.lb) < 1e-4f) { scal[scal.Count - 1] = (c.la, c.lb, p.ra, p.rb); continue; } }
                scal.Add((p.la, p.lb, p.ra, p.rb));
            }
            float pa = u0, pb = u0;
            foreach (var c in scal) { Trapez(sA, pa, c.la, pb, c.lb, va, vb); Trapez(sF, c.la, c.ra, c.lb, c.rb, va, vb); pa = c.ra; pb = c.rb; }
            Trapez(sA, pa, u1, pb, u1, va, vb);
        }
        Grupa("nawierzchnie: jezdnie");
    }

    static void Wachlarz(UlSiatka s, List<Vector2> w, System.Func<Vector2, float> y, Vector3 nn)
    {
        var p = w.Select(q => new Vector3(q.x, y(q), q.y)).ToList();
        for (int j = 1; j + 1 < p.Count; j++) s.Tri(p[0], p[j], p[j + 1], nn);
    }

    // ============================================================================ DROGI
    static void Drogi()
    {
        Grupa("nawierzchnie: jezdnie");
        float hw = Pas;
        // --- ul. Dworcowa: od Alei Południowej (przez zjazd w pasie zieleni alei, poziom chodnika 0.15) na wschód
        Func<Vector2, float> yD = q => Mathf.Lerp(.15f, YJ, Mathf.InverseLerp(LicoAlei, LicoAlei + 2f, q.x));
        var d1 = Prosta(new Os { y = yD }, new Vector2(LicoAlei, DworcowaZ), new Vector2(LacznikX - LacznikH - 3.5f, DworcowaZ));
        var d2 = Prosta(new Os(), new Vector2(LacznikX - LacznikH - 3.5f, DworcowaZ), new Vector2(LacznikX + LacznikH + 3.5f, DworcowaZ));
        var d3 = Luk(Prosta(new Os(), new Vector2(LacznikX + LacznikH + 3.5f, DworcowaZ), new Vector2(Luk1X, DworcowaZ)), new Vector2(Luk1X, Luk1Z), RL, 90f, 0f);
        var d4 = Luk(new Os(), new Vector2(Luk2X, Luk2Z), RL, 180f, 270f);
        float xP = PetlaX - PetlaR;            // zachodnia krawędź jezdni pętli (35.2); krawężnik pętli 34.95…35.2
        var d5 = Prosta(new Os(), new Vector2(Luk2X, DrogaZ), new Vector2(xP, DrogaZ));
        var d5k = Prosta(new Os(), new Vector2(Luk2X, DrogaZ), new Vector2(xP - Kr, DrogaZ));
        foreach (var o in new[] { d1, d2, d3, d4 }) { Asfalt(o, hw); Kraweznik(o, hw, 1); Chodnik(o, hw, 1, 3f); OsPrzerywana(o); ZajmijOs(o, hw + Kr + 3.2f); }
        Asfalt(d5, hw); OsPrzerywana(d5); Kraweznik(d5k, hw, 1); Chodnik(d5k, hw, 1, 3f); ZajmijOs(d5, hw + Kr + 3.2f);
        Kraweznik(d1, hw, -1); Kraweznik(d3, hw, -1); Kraweznik(d4, hw, -1); Chodnik(d4, hw, -1, 3f);
        // narożniki łącznika (R 3.5) przy ul. Dworcowej
        Naroznik(new Vector2(LacznikX - LacznikH, DworcowaZ - hw), new Vector2(LacznikX - LacznikH - 3.5f, DworcowaZ - hw - 3.5f), 3.5f, 90f, 0f);
        Naroznik(new Vector2(LacznikX + LacznikH, DworcowaZ - hw), new Vector2(LacznikX + LacznikH + 3.5f, DworcowaZ - hw - 3.5f), 3.5f, 180f, 90f);
        // --- prawa strona przed dworcem: krawężnik z zatokami TAXI/K+R i BUS (skosy wjazdu/wyjazdu 4 m)
        ZatokiIKrawedz(hw);
        // --- pętla: kwadrat 33.6 m, wyspa R 6.0 (okrąg wpisany R 16.8 — autobus 16.2 / 6.9)
        Petla();
        // --- łącznik do faweli (x −94) i ul. C0 (x −300): przez las i przejazd kolejowy
        Lacznik("łącznik do faweli", LacznikX, DworcowaZ - hw, DworcowaZ - hw - 3.5f, true);
        Lacznik("ul. C0 do faweli", C0X, C0Z0, -72f, false);   // krawężniki od tyłu chodnika ul. Tramwajowej
        // --- oznakowanie: przejście dla pieszych (x −14), napisy BUS i TAXI w zatokach
        // przejście dla pieszych (pasy równoległe do osi) i napisy w zatokach — farba w wycięciach asfaltu (WytnijFarbe)
        for (float z = DrogaZ - hw + .35f; z + .65f <= DrogaZ + hw - .3f; z += 1.3f)
            Farba(farba, new Vector2(ZebraX - ZebraSz * .5f, z), new Vector2(ZebraX + ZebraSz * .5f, z), new Vector2(ZebraX + ZebraSz * .5f, z + .65f), new Vector2(ZebraX - ZebraSz * .5f, z + .65f));
        // napisy (czytane przez kierowcę jadącego na wschód: góra liter na wschód, prawa ręka na południe): pole napisu wycięte
        // z asfaltu, wypełnione pasami — asfalt i farba w jednej płaszczyźnie, bez rozdrabniania asfaltu wokół
        Vector3 rP = Vector3.back, gP = Vector3.right;
        float zB = DrogaZ - hw - ZatokaSz * .5f;
        var napisy = new List<(string t, Vector3 o, float cap, float tr)>();
        for (int k = 0; k < 2; k++) napisy.Add(("BUS", V(BusX0 + 4f + StanowiskoDl * k + 3f, YJ, zB), 1.6f, .12f));
        napisy.Add(("TAXI", V(TaxiX0 + 6f, YJ, zB), 1.2f, .1f));
        napisy.Add(("K+R", V(TaxiX1 - 9f, YJ, zB), 1.2f, .1f));
        foreach (var (t, o, cap, tr) in napisy) NapisNaJezdni(t, o, rP, gP, cap, tr, true);
        WytnijFarbe();
        foreach (var (t, o, cap, tr) in napisy) NapisNaJezdni(t, o, rP, gP, cap, tr, false);
        Opis("ul. Dworcowa: jezdnia " + (2f * hw).ToString("F2") + " m, łuki R " + RL + " (krawędzie " + (RL - hw).ToString("F2") + "/" + (RL + hw).ToString("F2") + "), zatoka BUS " + (BusX1 - BusX0).ToString("F0") + " m (2 × " + StanowiskoDl + "), pętla R " + PetlaR + " / wyspa " + PetlaWyspa);
    }

    /// <summary>Narożnik skrzyżowania: asfalt między krawędziami a łukiem (wachlarz od narożnika), krawężnik po łuku (od strony środka).</summary>
    static void Naroznik(Vector2 rog, Vector2 c, float R, float a0, float a1)
    {
        var s = S(asfalt); int n = 10; Vector3 rg = V(rog.x, YJ, rog.y);
        Vector3 Pk(float a, float r, float y) { a *= Mathf.Deg2Rad; return V(c.x + Mathf.Cos(a) * r, y, c.y + Mathf.Sin(a) * r); }
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)n), b = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
            s.Tri(rg, Pk(a, R, YJ), Pk(b, R, YJ), Vector3.up); Pokryj(XZ(rg), XZ(Pk(a, R, YJ)), XZ(Pk(b, R, YJ)));
            Pokryj(XZ(Pk(a, R - Kr, YP)), XZ(Pk(b, R - Kr, YP)), XZ(Pk(b, R, YP)), XZ(Pk(a, R, YP)));
            S(granitC).Quad(Pk(a, R - Kr, YP), Pk(b, R - Kr, YP), Pk(b, R, YP), Pk(a, R, YP), Vector3.up);
            var nn = (Pk((a + b) * .5f, 1f, 0f) - V(c.x, 0f, c.y));
            S(granitC).Quad(Pk(a, R, YJ), Pk(b, R, YJ), Pk(b, R, YP), Pk(a, R, YP), nn);
            S(granitC).Quad(Pk(a, R - Kr, YC - .12f), Pk(b, R - Kr, YC - .12f), Pk(b, R - Kr, YP), Pk(a, R - Kr, YP), -nn);
        }
    }

    // prawa strona ul. Dworcowej przed dworcem: krawężnik z zatokami; plac (0.32) styka się z krawężnikiem
    static readonly List<Vector2> krawedzPlacu = new List<Vector2>();
    static void ZatokiIKrawedz(float hw)
    {
        float zj = DrogaZ - hw, zz = zj - ZatokaSz;       // −105.06 / −109.59
        float xKon = PetlaX - PetlaR - Kr;     // 34.95 — styk z krawężnikiem zachodnim pętli
        // linia krawężnika (x, z lica) od x −60 do pętli
        var pkt = new List<Vector2> { new Vector2(Luk2X, zj), new Vector2(TaxiX0, zj), new Vector2(TaxiX0 + 4f, zz), new Vector2(TaxiX1 - 4f, zz), new Vector2(TaxiX1, zj),
                                      new Vector2(BusX0, zj), new Vector2(BusX0 + 4f, zz), new Vector2(BusX1 - 4f, zz), new Vector2(BusX1, zj), new Vector2(xKon, zj) };
        krawedzPlacu.Clear();
        var sA = S(asfalt);
        // tylna krawędź krawężnika: odsunięcie o Kr z ucięciem w narożach (przecięcie sąsiednich odcinków) — wierzch krawężnika
        // i paski placu stykają się krawędzią, bez nakładania w narożach zatok
        Vector2 Nz(int i) { var d = (pkt[i + 1] - pkt[i]).normalized; return new Vector2(d.y, -d.x); }   // na zewnątrz jezdni (na południe)
        var tyl = new Vector2[pkt.Count];
        for (int k = 0; k < pkt.Count; k++)
        {
            if (k == 0) { tyl[k] = pkt[0] + Nz(0) * Kr; continue; }
            if (k == pkt.Count - 1) { tyl[k] = pkt[k] + Nz(k - 1) * Kr; continue; }
            Vector2 p1 = pkt[k] + Nz(k - 1) * Kr, d1 = (pkt[k] - pkt[k - 1]).normalized, p2 = pkt[k] + Nz(k) * Kr, d2 = (pkt[k + 1] - pkt[k]).normalized;
            float den = d1.x * d2.y - d1.y * d2.x;
            tyl[k] = Mathf.Abs(den) < 1e-5f ? p1 : p1 + d1 * (((p2.x - p1.x) * d2.y - (p2.y - p1.y) * d2.x) / den);
        }
        for (int i = 0; i + 1 < pkt.Count; i++)
        {
            Vector2 a = pkt[i], b = pkt[i + 1];
            // asfalt zatoki między linią jezdni (zj) a krawężnikiem
            if (a.y < zj - .01f || b.y < zj - .01f) { sA.Quad(V(a.x, YJ, zj), V(b.x, YJ, zj), V(b.x, YJ, b.y), V(a.x, YJ, a.y), Vector3.up); Pokryj(new Vector2(a.x, zj), new Vector2(b.x, zj), b, a); }
            // krawężnik: lico (od jezdni), wierzch, tył
            var nOut = Nz(i);
            Vector2 a2 = tyl[i], b2 = tyl[i + 1];
            S(granitC).Quad(V(a.x, YP, a.y), V(b.x, YP, b.y), V(b2.x, YP, b2.y), V(a2.x, YP, a2.y), Vector3.up); Pokryj(a, b, b2, a2);
            S(granitC).Quad(V(a.x, YJ, a.y), V(b.x, YJ, b.y), V(b.x, YP, b.y), V(a.x, YP, a.y), V(-nOut.x, 0f, -nOut.y));
        }
        krawedzPlacu.AddRange(tyl);
        Zajmij(Luk2X, zz - .3f, xKon, zj);
        Opis("stanowiska BUS: 1 x " + (BusX0 + 4f).ToString("F1") + "…" + (BusX0 + 4f + StanowiskoDl).ToString("F1") + ", 2 x " + (BusX0 + 4f + StanowiskoDl).ToString("F1") + "…" + (BusX0 + 4f + 2f * StanowiskoDl).ToString("F1"));
    }

    static void Petla()
    {
        float x0 = PetlaX - PetlaR, x1 = PetlaX + PetlaR, z0 = PetlaZ - PetlaR, z1 = PetlaZ + PetlaR, hw = Pas;
        var sA = S(asfalt);
        // asfalt kwadratu z otworem wyspy (pierścień od R wyspy do kwadratu — wachlarz po kątach)
        int n = 96;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n, b = (i + 1) * Mathf.PI * 2f / n;
            Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            float ra = PetlaR / Mathf.Max(Mathf.Abs(da.x), Mathf.Abs(da.y)), rb = PetlaR / Mathf.Max(Mathf.Abs(db.x), Mathf.Abs(db.y));
            var c = new Vector2(PetlaX, PetlaZ);
            Vector2 ia = c + da * PetlaWyspa, ib = c + db * PetlaWyspa, oa = c + da * ra, ob = c + db * rb;
            sA.Quad(V(ia.x, YJ, ia.y), V(oa.x, YJ, oa.y), V(ob.x, YJ, ob.y), V(ib.x, YJ, ib.y), Vector3.up); Pokryj(ia, oa, ob, ib);
            // wyspa: krawężnik (R 5.75…6.0), trawa wyspy
            Vector2 ka = c + da * (PetlaWyspa - Kr), kb = c + db * (PetlaWyspa - Kr);
            S(granitC).Quad(V(ka.x, YP, ka.y), V(ia.x, YP, ia.y), V(ib.x, YP, ib.y), V(kb.x, YP, kb.y), Vector3.up);
            S(granitC).Quad(V(ia.x, YJ, ia.y), V(ib.x, YJ, ib.y), V(ib.x, YP, ib.y), V(ia.x, YP, ia.y), V((da + db).x, 0f, (da + db).y));
            S(zielenWyspy).Tri(V(c.x, YP - .02f, c.y), V(ka.x, YP - .02f, ka.y), V(kb.x, YP - .02f, kb.y), Vector3.up); Pokryj(c, ia, ib);
        }
        // krawężniki kwadratu: północ, wschód, południe; zachód z wlotem ul. Dworcowej
        void KrawX(float xa, float xb, float z, int sg)   // krawężnik wzdłuż x, sg = strona na zewnątrz (+1 północ)
        {
            B(granitC, xa, YC - .12f, sg > 0 ? z : z - Kr, xb, YP, sg > 0 ? z + Kr : z, 0); Pokryj(xa, sg > 0 ? z : z - Kr, xb, sg > 0 ? z + Kr : z);
        }
        void KrawZ(float za, float zb, float x, int sg)
        {
            B(granitC, sg > 0 ? x : x - Kr, YC - .12f, za, sg > 0 ? x + Kr : x, YP, zb, 0); Pokryj(sg > 0 ? x : x - Kr, za, sg > 0 ? x + Kr : x, zb);
        }
        KrawX(x0 - Kr, x1 + Kr, z1, 1); KrawX(x0 - Kr, x1 + Kr, z0, -1); KrawZ(z0, z1, x1, 1);
        KrawZ(z0, PetlaZ - hw, x0, -1); KrawZ(PetlaZ + hw, z1, x0, -1);   // krawężnik zach. — przerwa na wlot ul. Dworcowej
        // chodnik wokół (północ 3 m do lasu, zachód-północ do chodnika ul. Dworcowej)
        Plyta(chodnik, x0 - Kr - 3f, z1 + Kr, Mathf.Min(x1 + Kr, BraxMiastoGranice.FawelaX1 - .3f), z1 + Kr + 3f, YP);
        Plyta(chodnik, x0 - Kr - 3f, PetlaZ + hw + Kr + 3f, x0 - Kr, z1 + Kr, YP);
        // malowanie: kreskowanie narożników (nieużywane pola kwadratu)
        Zajmij(x0 - 3.5f, z0 - .3f, BraxMiastoGranice.FawelaX1, z1 + 3.5f);
        Opis("pętla: kwadrat x " + x0.ToString("F1") + "…" + x1.ToString("F1") + ", z " + z0.ToString("F1") + "…" + z1.ToString("F1") + ", okrąg wpisany R " + PetlaR + " (autobus R zewn. 16.2), wyspa R " + PetlaWyspa + " (R wewn. 6.9)");
    }

    /// <summary>Łącznik przez las i przejazd kolejowy do krawędzi faweli. zStart = krawędź jezdni drogi, z której wychodzi.</summary>
    static void Lacznik(string nazwa, float xc, float zStart, float zA, bool chodnikWsch)
    {
        float hw = LacznikH, zK = BraxRegionFawela.KrawedzSwiat(xc), yK = BraxRegionFawela.WysNaKrawedzi(xc);
        Func<Vector2, float> y = q => q.y > zK + 8f ? ProfilPrzejazdu(q.y) : Mathf.Lerp(yK + .02f, ProfilPrzejazdu(q.y), Mathf.InverseLerp(zK, zK + 8f, q.y));
        var w0 = Prosta(new Os { y = y }, new Vector2(xc, zStart), new Vector2(xc, zA));                 // pod narożnikami (bez krawężników)
        var w1 = Prosta(new Os { y = y }, new Vector2(xc, zA), new Vector2(xc, RampaN));
        var w2 = Prosta(new Os { y = y }, new Vector2(xc, RampaN), new Vector2(xc, LozeN), 1f);
        var w4 = Prosta(new Os { y = y }, new Vector2(xc, LozeS), new Vector2(xc, RampaS), 1f);
        var w5 = Prosta(new Os { y = y }, new Vector2(xc, RampaS), new Vector2(xc, zK));
        Grupa("nawierzchnie: jezdnie");
        foreach (var o in new[] { w0, w1, w2, w4, w5 }) { Asfalt(o, hw); ZajmijOs(o, hw + 3f); }
        foreach (var o in new[] { w1, w5 }) { Kraweznik(o, hw, 1); Kraweznik(o, hw, -1); OsPrzerywana(o); }   // farba wycinana w Drogi (WytnijFarbe)
        if (chodnikWsch) { Chodnik(w1, hw, 1, 2.5f); Chodnik(w5, hw, 1, 2.5f); }   // jazda na południe: lewa strona = wschód
        // przejazd: płyty betonowe w poziomie 2 cm pod główką szyny (szyny lekko wystają), mury skrzydłowe zjazdów
        Grupa("nawierzchnie: przejazdy");
        Plyta(betonMur, xc - hw, LozeS, xc + hw, LozeN, GlowkaY - .025f, .5f);   // 2.5 cm pod główką (odstęp ≥ 2 cm — bez migotania)
        foreach (int sg in new[] { -1, 1 })
        {
            float xa = xc + sg * (hw + .1f), xb = xc + sg * (hw + .6f);
            foreach (var (za, zb) in new[] { (LozeN, RampaN), (RampaS, LozeS) })
            {
                int k = 12;
                for (int i = 0; i < k; i++)
                {
                    float z0 = Mathf.Lerp(za, zb, i / (float)k), z1 = Mathf.Lerp(za, zb, (i + 1) / (float)k);
                    float yb = Mathf.Min(ProfilPrzejazdu(z0), ProfilPrzejazdu(z1)) - .3f;
                    B(betonMur, Mathf.Min(xa, xb), yb, Mathf.Min(z0, z1), Mathf.Max(xa, xb), YC + .35f, Mathf.Max(z0, z1));
                }
            }
            Kol(nazwa + " mur skrzydłowy", V(Mathf.Min(xa, xb), GlowkaY - .3f, RampaN), V(Mathf.Max(xa, xb), YC + .35f, LozeN));
            Kol(nazwa + " mur skrzydłowy", V(Mathf.Min(xa, xb), GlowkaY - .3f, LozeS), V(Mathf.Max(xa, xb), YC + .35f, RampaS));
        }
        Przejazd(nazwa, xc, hw);
        Opis(nazwa + ": x " + xc + ", jezdnia " + (2f * hw).ToString("F1") + " m, przejazd na z " + LozeN.ToString("F1") + "…" + LozeS.ToString("F1") + " (spadek " + ((YJ - GlowkaY) / (RampaN - LozeN) * 100f).ToString("F0") + " %), koniec na krawędzi faweli z " + zK.ToString("F2") + " (y " + yK.ToString("F2") + ")");
    }

    // ============================================================================ PLAC
    static void Plac()
    {
        Grupa("nawierzchnie: plac");
        float zz = DrogaZ - Pas - ZatokaSz - Kr;          // −109.84 (za krawężnikiem zatok)
        float xKon = PetlaX - PetlaR - Kr;                // 34.95 (krawężnik zachodni pętli)
        // pasek przy krawężniku (poza zatokami) — od krawędzi placu (krawędź tylna krawężnika) do −109.84
        for (int i = 0; i + 1 < krawedzPlacu.Count; i++)
        {
            Vector2 a = krawedzPlacu[i], b = krawedzPlacu[i + 1];
            if (a.y <= zz + .01f && b.y <= zz + .01f) continue;
            S(plyty).Quad(V(a.x, YP, a.y), V(b.x, YP, b.y), V(b.x, YP, zz), V(a.x, YP, zz), Vector3.up); Pokryj(a, b, new Vector2(b.x, zz), new Vector2(a.x, zz));
        }
        // plac główny przed halą, boki budynku, plac wschodni (do peronu 1 i pętli), plac zachodni (do łącznika)
        // plac główny: pasy ciemnego granitu (co 6 m) jako osobne płyty między płytami placu — ta sama wysokość, bez nakładania
        float sz0 = BudZ0 + .5f, sz1 = zz - .5f, xp = Luk2X;
        Plyta(plyty, Luk2X, BudZ0, xKon, sz0, YP); Plyta(plyty, Luk2X, sz1, xKon, zz, YP);
        for (float x = -48f; x <= 30f; x += 6f) { Plyta(plyty, xp, sz0, x - .15f, sz1, YP); Plyta(granitC, x - .15f, sz0, x + .15f, sz1, YP); xp = x + .15f; }
        Plyta(plyty, xp, sz0, xKon, sz1, YP);
        Plyta(plyty, -80f, P1Tyl, BudX0, BudZ0, YP);
        Plyta(plyty, -80f, BudZ0, Luk2X, -111.35f, YP);
        Plyta(plyty, BudX1, P1Tyl, xKon, BudZ0, YP);
        Plyta(plyty, xKon, P1Tyl, PX1, PetlaZ - PetlaR - Kr, YP);
        Plyta(plyty, BudX0, BudZ0 - .8f, -34.6f, BudZ0, YP);    // przed skrzydłem KOLEJ (lico przeszklenia z −124.8)
        Plyta(plyty, -5.8f, BudZ0 - .8f, BudX1, BudZ0, YP);     // przed skrzydłem AUTOBUSY
        // ścieżka od łącznika (chodnik wsch.) do placu zachodniego
        Plyta(chodnik, LacznikX + LacznikH + Kr + 2.5f, -133.5f, -80f, -130.5f, YP);
        Zajmij(-80f, P1Tyl, PX1, zz);
        Opis("plac: x −80…" + PX1 + ", z " + zz.ToString("F2") + "…" + P1Tyl + " (poziom " + YP + ", peron 1 bez schodów)");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// OZNAKOWANIE W WYCIĘCIACH — runda 4 (zasada właściciela: nic nie nakładamy, oznakowanie w teksturze albo w wycięciu).
// Każdy trójkąt farby z grupy oznakowania (linie, zebry, strzałki, „zęby” P-13, szewrony wysp, linie krawędziowe ronda po łuku,
// zygzaki P-17), który leży na asfalcie, wycina z asfaltu DOKŁADNIE swój kształt (odejmowanie wielokąta wypukłego od każdego
// trójkąta asfaltu — Sutherland–Hodgman po trzech krawędziach), a farba schodzi na lico asfaltu: jedna powierzchnia, bez
// „naklejki” 2 cm i bez dwóch płaszczyzn w jednym miejscu. Jeden przebieg po asfalcie (siatka 4 m — szybko).
// Kontrola: BRAXHORN_DROGI_OZNAKOWANIE nakladki=N — poziome trójkąty farby nad asfaltem (4 mm … 5 cm nad licem) w całej
// scenie (A, B, stacja, warsztat); poza asfaltem (kort, żarówki latarni) się nie liczą.
public static partial class BraxClaudeOsiedle
{
    struct FarbaTri { public Batch b; public int i0, i1, i2; public Vector2 a, c, d; public float x0, x1, z0, z1; }
    const float OznKom = 4f;
    static long KomKlucz(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

    static Vector2 Xz(Vector3 p) => new Vector2(p.x, p.z);
    static float PoleXz(List<WierzD> p) { float s = 0f; for (int k = 0; k < p.Count; k++) { var a = p[k].p; var b = p[(k + 1) % p.Count].p; s += a.x * b.z - b.x * a.z; } return Mathf.Abs(s) * .5f; }
    static bool WTrojkacie(Vector2 p, Vector2 a, Vector2 b, Vector2 c, float tol = 1e-4f)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < -tol || d2 < -tol || d3 < -tol, pos = d1 > tol || d2 > tol || d3 > tol;
        return !(neg && pos);
    }
    // część wielokąta po stronie n·p ≥ d (wewnatrz) albo n·p ≤ d (na zewnątrz) — w płaszczyźnie XZ
    static List<WierzD> KlipLinia(List<WierzD> poly, Vector2 n, float d, bool wewnatrz)
    {
        var res = new List<WierzD>(poly.Count + 2); float sg = wewnatrz ? 1f : -1f;
        for (int k = 0; k < poly.Count; k++)
        {
            var a = poly[k]; var b = poly[(k + 1) % poly.Count];
            float da = (n.x * a.p.x + n.y * a.p.z - d) * sg, db = (n.x * b.p.x + n.y * b.p.z - d) * sg;
            bool ina = da >= 0f, inb = db >= 0f;
            if (ina) res.Add(a);
            if (ina != inb) res.Add(LerpW(a, b, da / (da - db)));
        }
        return res;
    }
    // wielokąt minus trójkąt (wypukły): kawałki na zewnątrz kolejnych krawędzi; środek (część wspólna) odrzucony
    static void OdejmijTrojkat(List<WierzD> poly, Vector2 a, Vector2 b, Vector2 c, List<List<WierzD>> wyn)
    {
        var cen = (a + b + c) / 3f; var tri = new[] { a, b, c }; var rem = poly;
        for (int e = 0; e < 3; e++)
        {
            var p = tri[e]; var q = tri[(e + 1) % 3]; var n = new Vector2(-(q.y - p.y), q.x - p.x);
            if (Vector2.Dot(n, cen - p) < 0f) n = -n;
            float d = Vector2.Dot(n, p);
            var zew = KlipLinia(rem, n, d, false);
            if (zew.Count >= 3 && PoleXz(zew) > 1e-7f) wyn.Add(zew);
            rem = KlipLinia(rem, n, d, true);
            if (rem.Count < 3) return;
        }
    }

    // Wycina asfalt pod farbą grupy i obniża farbę do lica asfaltu. Zwraca (trójkąty farby wcięte, trójkąty farby poza asfaltem).
    static (int wciete, int poza, int asfaltPrzed, int asfaltPo) OznakowanieWWycieciach(string grupaFarby, Func<string, bool> grupyAsfaltu, float yAsf)
    {
        var asf = batches.Where(kv => kv.Key.Item2 == Asphalt && grupyAsfaltu(kv.Key.Item1)).Select(kv => kv.Value).ToList();
        // siatka trójkątów asfaltu (do testu „farba leży na asfalcie”)
        var asfKom = new Dictionary<long, List<(Batch b, int k)>>();
        foreach (var b in asf)
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                var p0 = b.v[b.i[k]]; var p1 = b.v[b.i[k + 1]]; var p2 = b.v[b.i[k + 2]];
                int cx0 = Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)) / OznKom), cx1 = Mathf.FloorToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)) / OznKom);
                int cz0 = Mathf.FloorToInt(Mathf.Min(p0.z, Mathf.Min(p1.z, p2.z)) / OznKom), cz1 = Mathf.FloorToInt(Mathf.Max(p0.z, Mathf.Max(p1.z, p2.z)) / OznKom);
                for (int cx = cx0; cx <= cx1; cx++) for (int cz = cz0; cz <= cz1; cz++) { var key = KomKlucz(cx, cz); if (!asfKom.TryGetValue(key, out var l)) asfKom[key] = l = new List<(Batch, int)>(); l.Add((b, k)); }
            }
        bool NaAsfalcie(Vector2 p)
        {
            if (!asfKom.TryGetValue(KomKlucz(Mathf.FloorToInt(p.x / OznKom), Mathf.FloorToInt(p.y / OznKom)), out var l)) return false;
            foreach (var (b, k) in l) if (WTrojkacie(p, Xz(b.v[b.i[k]]), Xz(b.v[b.i[k + 1]]), Xz(b.v[b.i[k + 2]]))) return true;
            return false;
        }
        // trójkąty farby (poziome, ponad licem asfaltu)
        var farby = new List<FarbaTri>(); int poza = 0;
        foreach (var kv in batches.Where(kv => kv.Key.Item1 == grupaFarby).ToList())
        {
            var b = kv.Value;
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                var p0 = b.v[b.i[k]]; var p1 = b.v[b.i[k + 1]]; var p2 = b.v[b.i[k + 2]];
                if (Mathf.Abs(p0.y - p1.y) > .002f || Mathf.Abs(p0.y - p2.y) > .002f || p0.y < yAsf + .001f) continue;   // pionowe / już na licu
                var a = Xz(p0); var c = Xz(p1); var d = Xz(p2);
                if (Mathf.Abs((c.x - a.x) * (d.y - a.y) - (d.x - a.x) * (c.y - a.y)) < 1e-6f) continue;
                if (!NaAsfalcie((a + c + d) / 3f)) { poza++; continue; }
                farby.Add(new FarbaTri { b = b, i0 = b.i[k], i1 = b.i[k + 1], i2 = b.i[k + 2], a = a, c = c, d = d,
                    x0 = Mathf.Min(a.x, Mathf.Min(c.x, d.x)), x1 = Mathf.Max(a.x, Mathf.Max(c.x, d.x)), z0 = Mathf.Min(a.y, Mathf.Min(c.y, d.y)), z1 = Mathf.Max(a.y, Mathf.Max(c.y, d.y)) });
            }
        }
        if (farby.Count == 0) return (0, poza, 0, 0);
        var farbaKom = new Dictionary<long, List<int>>();
        for (int f = 0; f < farby.Count; f++)
            for (int cx = Mathf.FloorToInt(farby[f].x0 / OznKom); cx <= Mathf.FloorToInt(farby[f].x1 / OznKom); cx++)
                for (int cz = Mathf.FloorToInt(farby[f].z0 / OznKom); cz <= Mathf.FloorToInt(farby[f].z1 / OznKom); cz++)
                { var key = KomKlucz(cx, cz); if (!farbaKom.TryGetValue(key, out var l)) farbaKom[key] = l = new List<int>(); l.Add(f); }
        // jeden przebieg po asfalcie: każdy trójkąt asfaltu minus wszystkie trójkąty farby, które go dotykają
        int przed = 0, po = 0; var kand = new HashSet<int>();
        WierzD W(Batch b, int q) => new WierzD { p = b.v[q], n = b.n[q], t = b.t[q], uv = b.uv[q] };
        foreach (var b in asf)
        {
            var nv = new List<Vector3>(b.v.Count); var nn = new List<Vector3>(b.v.Count); var nt = new List<Vector4>(b.v.Count); var nu = new List<Vector2>(b.v.Count); var ni = new List<int>(b.i.Count);
            int Dodaj(WierzD w) { nv.Add(w.p); nn.Add(w.n); nt.Add(w.t); nu.Add(w.uv); return nv.Count - 1; }
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                przed++;
                var t0 = W(b, b.i[k]); var t1 = W(b, b.i[k + 1]); var t2 = W(b, b.i[k + 2]);
                float x0 = Mathf.Min(t0.p.x, Mathf.Min(t1.p.x, t2.p.x)), x1 = Mathf.Max(t0.p.x, Mathf.Max(t1.p.x, t2.p.x)), z0 = Mathf.Min(t0.p.z, Mathf.Min(t1.p.z, t2.p.z)), z1 = Mathf.Max(t0.p.z, Mathf.Max(t1.p.z, t2.p.z));
                kand.Clear();
                for (int cx = Mathf.FloorToInt(x0 / OznKom); cx <= Mathf.FloorToInt(x1 / OznKom); cx++)
                    for (int cz = Mathf.FloorToInt(z0 / OznKom); cz <= Mathf.FloorToInt(z1 / OznKom); cz++)
                        if (farbaKom.TryGetValue(KomKlucz(cx, cz), out var l)) foreach (int f in l) { var F = farby[f]; if (F.x1 > x0 && F.x0 < x1 && F.z1 > z0 && F.z0 < z1) kand.Add(f); }
                var kawalki = new List<List<WierzD>> { new List<WierzD> { t0, t1, t2 } };
                foreach (int f in kand)
                {
                    var F = farby[f]; var nowe = new List<List<WierzD>>();
                    foreach (var kw in kawalki) OdejmijTrojkat(kw, F.a, F.c, F.d, nowe);
                    kawalki = nowe;
                    if (kawalki.Count == 0) break;
                }
                foreach (var kw in kawalki)
                {
                    int s = Dodaj(kw[0]);
                    for (int q = 1; q + 1 < kw.Count; q++) { ni.Add(s); ni.Add(Dodaj(kw[q])); ni.Add(Dodaj(kw[q + 1])); po++; }
                }
            }
            b.v.Clear(); b.v.AddRange(nv); b.n.Clear(); b.n.AddRange(nn); b.t.Clear(); b.t.AddRange(nt); b.uv.Clear(); b.uv.AddRange(nu); b.i.Clear(); b.i.AddRange(ni);
        }
        // farba na lico asfaltu
        foreach (var F in farby) foreach (int q in new[] { F.i0, F.i1, F.i2 }) { var p = F.b.v[q]; p.y = yAsf; F.b.v[q] = p; }
        return (farby.Count, poza, przed, po);
    }

    // Kontrola nakładek: poziome trójkąty farby nad asfaltem (wszystkie grupy) — 4 mm … 5 cm nad licem asfaltu pod środkiem.
    static int KontrolaNakladekFarby(out string przyklady)
    {
        var asfKom = new Dictionary<long, List<(Vector3 a, Vector3 b, Vector3 c)>>();
        foreach (var kv in batches.Where(kv => kv.Key.Item2 == Asphalt || (kv.Key.Item2 && kv.Key.Item2.name.Contains("asfalt"))))
        {
            var b = kv.Value;
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                var p0 = b.v[b.i[k]]; var p1 = b.v[b.i[k + 1]]; var p2 = b.v[b.i[k + 2]];
                if (Mathf.Abs(p0.y - p1.y) > .01f || Mathf.Abs(p0.y - p2.y) > .01f) continue;
                int cx0 = Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)) / OznKom), cx1 = Mathf.FloorToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)) / OznKom);
                int cz0 = Mathf.FloorToInt(Mathf.Min(p0.z, Mathf.Min(p1.z, p2.z)) / OznKom), cz1 = Mathf.FloorToInt(Mathf.Max(p0.z, Mathf.Max(p1.z, p2.z)) / OznKom);
                for (int cx = cx0; cx <= cx1; cx++) for (int cz = cz0; cz <= cz1; cz++) { var key = KomKlucz(cx, cz); if (!asfKom.TryGetValue(key, out var l)) asfKom[key] = l = new List<(Vector3, Vector3, Vector3)>(); l.Add((p0, p1, p2)); }
            }
        }
        int n = 0; var przy = new List<string>();
        foreach (var kv in batches.Where(kv => kv.Key.Item2 && kv.Key.Item2.name.ToLowerInvariant().Contains("farb")))
        {
            var b = kv.Value;
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                var p0 = b.v[b.i[k]]; var p1 = b.v[b.i[k + 1]]; var p2 = b.v[b.i[k + 2]];
                if (Mathf.Abs(p0.y - p1.y) > .002f || Mathf.Abs(p0.y - p2.y) > .002f) continue;
                var c = (Xz(p0) + Xz(p1) + Xz(p2)) / 3f;
                if (!asfKom.TryGetValue(KomKlucz(Mathf.FloorToInt(c.x / OznKom), Mathf.FloorToInt(c.y / OznKom)), out var l)) continue;
                foreach (var (a, bb, cc) in l)
                {
                    if (!WTrojkacie(c, Xz(a), Xz(bb), Xz(cc))) continue;
                    float dy = p0.y - (a.y + bb.y + cc.y) / 3f;
                    if (dy > .004f && dy < .05f) { n++; if (przy.Count < 8) przy.Add(kv.Key.Item1 + " (" + c.x.ToString("F1") + ", " + c.y.ToString("F1") + ") +" + (dy * 100f).ToString("F1") + " cm"); }
                    break;
                }
            }
        }
        przyklady = string.Join("; ", przy);
        return n;
    }
    static void OznakowanieKontrola(string etap)
    {
        int n = KontrolaNakladekFarby(out var przy);
        Debug.Log("BRAXHORN_DROGI_OZNAKOWANIE etap=" + etap + " nakladki=" + n + " (poziome trójkąty farby 4 mm–5 cm nad licem asfaltu, cała scena)" + (n > 0 ? " [" + przy + "]" : ""));
    }
}

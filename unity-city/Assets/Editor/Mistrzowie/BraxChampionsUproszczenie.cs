using System;
using System.Collections.Generic;
using UnityEngine;

// The region's heavy scans made light (owner 23.11, the island villa at 15-19 frames a second inside: its potted plants were
// Poly Haven scans of 70 000 and 176 000 triangles each, the same plants stand in ~60 places of the region; ZASADY 10.2: a
// small thing 5 000, a tree 20 000). Plain arithmetic the region's tools run without Unity (unity-tools/mistrzowie/lzejsze_sim):
// quadric error edge collapse (Garland & Heckbert 1997), half-edge: a point of the surface moves onto its neighbour and keeps
// nothing of its own, so every kept vertex keeps its own normal, UV and colour (no texture swims).
//  - The scan's vertices are split where its UV islands meet (a seam: one point, two vertices). The collapse works on the
//    points: each vertex of the moving point goes to the vertex of the target on its own side of the seam, and a collapse
//    with a side that has no such vertex is refused - so a seam point only moves along its seam, both sides together, and
//    the surface never cracks open.
//  - A leaf's outline (an edge of one triangle) and a seam are held by planes across them, so outlines and seams keep their
//    line; an outline point only moves along its outline.
//  - A collapse that would turn a face over, or join two sheets at one edge, is refused.
//  - Every point is held a little at its own place, so a thin twig's tip never slides down the twig (shorter twigs, leaves
//    in the air).
// A model's parts go through one queue together (Uprosc(Vector3[][], int[][], cel)): the budget goes where the shape needs it.
// Measured 23.11 on the real scans (lzejsze_sim, three.js pictures of the scan and the light one side by side): potted_plant_01
// 176 226 -> 12 000 / 16 000, potted_plant_02 69 806 -> 5 000 / 9 000, potted_plant_04 8 929 -> 4 000 triangles, the same
// plant to the eye. Not yet in the scene: the scene pass that swaps the region's instances for these is the next step.
public static class BraxChampionsUproszczenie
{
    sealed class Kolejka
    {
        // a binary min-heap of (cost, point a onto point b, the two points' stamps when it was costed)
        readonly List<(float c, int a, int b, int sa, int sb)> h = new List<(float, int, int, int, int)>();
        public int Count => h.Count;
        public void Push((float c, int a, int b, int sa, int sb) x)
        {
            h.Add(x); int i = h.Count - 1;
            while (i > 0) { int p = (i - 1) >> 1; if (h[p].c <= h[i].c) break; (h[p], h[i]) = (h[i], h[p]); i = p; }
        }
        public (float c, int a, int b, int sa, int sb) Pop()
        {
            var top = h[0]; var last = h[h.Count - 1]; h.RemoveAt(h.Count - 1);
            if (h.Count > 0)
            {
                h[0] = last; int i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, m = i;
                    if (l < h.Count && h[l].c < h[m].c) m = l;
                    if (r < h.Count && h[r].c < h[m].c) m = r;
                    if (m == i) break;
                    (h[m], h[i]) = (h[i], h[m]); i = m;
                }
            }
            return top;
        }
    }

    // a symmetric 4x4 quadric (10 numbers)
    static void Dodaj(double[] q, int o, double a, double b, double c, double d, double w)
    {
        q[o] += w * a * a; q[o + 1] += w * a * b; q[o + 2] += w * a * c; q[o + 3] += w * a * d;
        q[o + 4] += w * b * b; q[o + 5] += w * b * c; q[o + 6] += w * b * d;
        q[o + 7] += w * c * c; q[o + 8] += w * c * d; q[o + 9] += w * d * d;
    }
    static double Blad(double[] q, int o, int pa, Vector3 p)
    {
        double x = p.x, y = p.y, z = p.z;
        double Q(int k) => q[o + k] + q[pa + k];
        return Q(0) * x * x + 2 * Q(1) * x * y + 2 * Q(2) * x * z + 2 * Q(3) * x + Q(4) * y * y + 2 * Q(5) * y * z + 2 * Q(6) * y
             + Q(7) * z * z + 2 * Q(8) * z + Q(9);
    }

    /// <summary>The weight of an outline's plane (as a face of its length squared, times this).</summary>
    public const double WagaKrawedzi = 10.0, WagaSzwu = 2.0;
    /// <summary>The hold of each point at its own place, as a face of the model's mean area times this.</summary>
    public static double WagaPunktu = .1;
    public static int Odrzucone, Przewroty, Szwy, Krawedzie, Tkanka, Przebiegi;
    // Unity's .normalized gives zero under a length of 1e-5: a scan's millimetre triangles have cross products of 1e-6
    static Vector3 Jedn(Vector3 n) { float m = n.magnitude; return m > 0f ? n / m : Vector3.zero; }
    static IEnumerable<int> Zywe(bool[] zywy) { for (int f = 0; f < zywy.Length; f++) if (zywy[f]) yield return f; }

    /// <summary>A whole model's parts (each its own vertices v[i] and triangles t[i], one mesh space) brought down together to
    /// about cel triangles: one error queue over all of them, so the budget goes where the shape needs it (a pot's round
    /// silhouette keeps its triangles, a pebble in the soil gives its own). Returns each part's kept triangles.</summary>
    public static int[][] Uprosc(Vector3[][] v, int[][] t, int cel)
    {
        int n = v.Length, nv = 0, nt = 0;
        for (int i = 0; i < n; i++) { nv += v[i].Length; nt += t[i].Length; }
        var vv = new Vector3[nv]; var tt = new int[nt]; var cz = new int[nv]; var odV = new int[n + 1]; int o = 0;
        for (int i = 0; i < n; i++)
        {
            Array.Copy(v[i], 0, vv, odV[i], v[i].Length);
            for (int k = 0; k < v[i].Length; k++) cz[odV[i] + k] = i;
            for (int k = 0; k < t[i].Length; k++) tt[o + k] = t[i][k] + odV[i];
            o += t[i].Length; odV[i + 1] = odV[i] + v[i].Length;
        }
        var r = Uprosc(vv, tt, cel, cz);
        var wynik = new List<int>[n]; for (int i = 0; i < n; i++) wynik[i] = new List<int>();
        for (int k = 0; k < r.Length; k++) { int c = cz[r[k]]; wynik[c].Add(r[k] - odV[c]); }
        var w = new int[n][]; for (int i = 0; i < n; i++) w[i] = wynik[i].ToArray();
        return w;
    }

    /// <summary>The triangles t (over the vertices v) brought down to about cel triangles. Returns the kept triangles (indices
    /// into v: the kept vertices are a subset, each with its own attributes). czesc: each vertex's part (vertices of two parts
    /// at one place stay two points: a face never takes another part's vertex).</summary>
    public static int[] Uprosc(Vector3[] v, int[] t, int cel, int[] czesc = null)
    {
        Odrzucone = Przewroty = Szwy = Krawedzie = Tkanka = 0;
        int nv = v.Length, nt = t.Length / 3;
        if (nt <= cel) return (int[])t.Clone();
        // ---- the points: vertices at one place are one point (the seams)
        var pkt = new int[nv]; var miejsca = new Dictionary<(float, float, float, int), int>(nv);
        var poz = new List<Vector3>(nv);
        for (int i = 0; i < nv; i++)
        {
            var k = (v[i].x, v[i].y, v[i].z, czesc == null ? 0 : czesc[i]);
            if (!miejsca.TryGetValue(k, out int p)) { p = poz.Count; miejsca[k] = p; poz.Add(v[i]); }
            pkt[i] = p;
        }
        int np = poz.Count;
        var tri = (int[])t.Clone(); var zywy = new bool[nt];
        var przy = new List<int>[np]; for (int i = 0; i < np; i++) przy[i] = new List<int>(6);
        var q = new double[np * 10];
        var nrm = new Vector3[nt];
        for (int f = 0; f < nt; f++)
        {
            int a = pkt[tri[f * 3]], b = pkt[tri[f * 3 + 1]], c = pkt[tri[f * 3 + 2]];
            if (a == b || b == c || a == c) continue;
            zywy[f] = true; przy[a].Add(f); przy[b].Add(f); przy[c].Add(f);
            var n = Vector3.Cross(poz[b] - poz[a], poz[c] - poz[a]); float area = n.magnitude * .5f;
            if (area < 1e-14f) continue;
            n /= (2f * area); nrm[f] = n;
            double d = -Vector3.Dot(n, poz[a]);
            Dodaj(q, a * 10, n.x, n.y, n.z, d, area); Dodaj(q, b * 10, n.x, n.y, n.z, d, area); Dodaj(q, c * 10, n.x, n.y, n.z, d, area);
        }
        int P(int f, int k) => pkt[tri[f * 3 + k]];
        bool Ma(int f, int p) => P(f, 0) == p || P(f, 1) == p || P(f, 2) == p;
        int Wierz(int f, int p) { for (int k = 0; k < 3; k++) if (pkt[tri[f * 3 + k]] == p) return tri[f * 3 + k]; return -1; }
        // the kind of the edge a-b now: 0 inside, 1 an outline (one face), 2 a seam (two faces, a different vertex on each)
        var wspolne = new List<int>(4);
        int Rodzaj(int a, int b)
        {
            wspolne.Clear();
            foreach (int f in przy[a]) if (zywy[f] && Ma(f, b)) wspolne.Add(f);
            if (wspolne.Count == 1) return 1;
            if (wspolne.Count == 2 && (Wierz(wspolne[0], a) != Wierz(wspolne[1], a) || Wierz(wspolne[0], b) != Wierz(wspolne[1], b))) return 2;
            return 0;
        }
        // ---- every point held a little at its own place (as on a face of the model's mean size): a twig's tip slides along
        //      the twig's own planes at no cost and the twig grows short (potted_plant_01's top twig lost 57 mm, its top leaf
        //      left in the air); this keeps tips and needles where they are
        double pole = 0; int ile = 0;
        for (int f = 0; f < nt; f++) if (zywy[f]) { pole += Vector3.Cross(poz[P(f, 1)] - poz[P(f, 0)], poz[P(f, 2)] - poz[P(f, 0)]).magnitude * .5; ile++; }
        double wPkt = WagaPunktu * pole / Math.Max(1, ile);
        for (int i = 0; i < np; i++)
        {
            if (przy[i].Count == 0) continue;
            var x = poz[i];
            Dodaj(q, i * 10, 1, 0, 0, -x.x, wPkt); Dodaj(q, i * 10, 0, 1, 0, -x.y, wPkt); Dodaj(q, i * 10, 0, 0, 1, -x.z, wPkt);
        }
        // ---- the outlines and the seams: a plane across each
        var krawedzie = new HashSet<long>();
        long Klucz(int a, int b) => a < b ? (long)a * np + b : (long)b * np + a;
        for (int f = 0; f < nt; f++)
        {
            if (!zywy[f]) continue;
            for (int e = 0; e < 3; e++)
            {
                int a = P(f, e), b = P(f, (e + 1) % 3);
                if (!krawedzie.Add(Klucz(a, b))) continue;
                int r = Rodzaj(a, b); if (r == 0) continue;
                var ed = poz[b] - poz[a]; float len = ed.magnitude; if (len < 1e-9f) continue;
                var pn = Jedn(Vector3.Cross(ed / len, nrm[f])); if (pn == Vector3.zero) continue;
                double d = -Vector3.Dot(pn, poz[a]), w = (r == 1 ? WagaKrawedzi : WagaSzwu) * len * len;
                Dodaj(q, a * 10, pn.x, pn.y, pn.z, d, w); Dodaj(q, b * 10, pn.x, pn.y, pn.z, d, w);
            }
        }
        var stamp = new int[np]; var kol = new Kolejka();
        void Wstaw(int a, int b)
        {
            // a onto b or b onto a, whichever costs less (each at its own place: half-edge)
            double ca = Blad(q, a * 10, b * 10, poz[b]), cb = Blad(q, b * 10, a * 10, poz[a]);
            if (ca <= cb) kol.Push(((float)ca, a, b, stamp[a], stamp[b])); else kol.Push(((float)cb, b, a, stamp[b], stamp[a]));
        }
        foreach (long k in krawedzie) Wstaw((int)(k / np), (int)(k % np));
        int zostalo = 0; for (int f = 0; f < nt; f++) if (zywy[f]) zostalo++;
        var mapa = new Dictionary<int, int>(); var sasA = new HashSet<int>(); var sasB = new HashSet<int>();
        var sasiedzi = new HashSet<int>();
        int przebieg = 0, przed = zostalo;
        while (zostalo > cel)
        {
            if (kol.Count == 0)
            {
                // a refused edge is costed again only when a point of it changes: once the heap runs dry, every edge
                // left again (its neighbourhood may allow it now), while that still brings the count down
                if (zostalo == przed || ++przebieg > 8) break;
                przed = zostalo;
                foreach (int f0 in Zywe(zywy)) for (int e = 0; e < 3; e++) { int u = P(f0, e), w = P(f0, (e + 1) % 3); if (u < w || Rodzaj(u, w) == 1) Wstaw(u, w); }
                continue;
            }
            var (c, a, b, sa, sb) = kol.Pop();                                    // move point a onto point b
            if (sa != stamp[a] || sb != stamp[b]) continue;                       // costed before one of them changed
            if (przy[a].Count == 0 || przy[b].Count == 0) continue;
            bool zle = false;
            // an outline or seam point moves only along its outline / seam
            int rAB = Rodzaj(a, b), naEdge = wspolne.Count;
            if (rAB != 1)
            {
                sasA.Clear();
                foreach (int f in przy[a]) if (zywy[f]) for (int k = 0; k < 3; k++) { int u = P(f, k); if (u != a) sasA.Add(u); }
                foreach (int u in sasA) if (u != b && Rodzaj(a, u) == 1) { zle = true; break; }
                if (zle) { Krawedzie++; Odrzucone++; continue; }
            }
            // each vertex of a to the vertex of b on its own side
            mapa.Clear();
            foreach (int f in przy[a])
            {
                if (!zywy[f] || !Ma(f, b)) continue;
                int va = Wierz(f, a), vb = Wierz(f, b);
                if (mapa.TryGetValue(va, out int jest) && jest != vb) { zle = true; break; }
                mapa[va] = vb;
            }
            if (!zle) foreach (int f in przy[a]) if (zywy[f] && !Ma(f, b) && !mapa.ContainsKey(Wierz(f, a))) { zle = true; break; }
            if (zle) { Szwy++; Odrzucone++; continue; }
            // two sheets joined at one edge: the points both a and b touch are only those of the faces on a-b
            sasA.Clear(); sasB.Clear();
            foreach (int f in przy[a]) if (zywy[f]) for (int k = 0; k < 3; k++) sasA.Add(P(f, k));
            foreach (int f in przy[b]) if (zywy[f]) for (int k = 0; k < 3; k++) sasB.Add(P(f, k));
            sasA.IntersectWith(sasB); sasA.Remove(a); sasA.Remove(b);
            if (sasA.Count != naEdge) { Tkanka++; Odrzucone++; continue; }
            // no face may turn over
            foreach (int f in przy[a])
            {
                if (!zywy[f] || Ma(f, b)) continue;
                var p0 = P(f, 0) == a ? poz[b] : poz[P(f, 0)]; var p1 = P(f, 1) == a ? poz[b] : poz[P(f, 1)]; var p2 = P(f, 2) == a ? poz[b] : poz[P(f, 2)];
                var n2 = Vector3.Cross(p1 - p0, p2 - p0);
                if (n2.sqrMagnitude < 1e-24f || (nrm[f] != Vector3.zero && Vector3.Dot(Jedn(n2), nrm[f]) < .3f)) { zle = true; break; }
            }
            if (zle) { Przewroty++; Odrzucone++; continue; }
            // ---- the collapse
            foreach (int f in przy[a])
            {
                if (!zywy[f]) continue;
                int o = f * 3;
                if (Ma(f, b)) { zywy[f] = false; zostalo--; continue; }
                for (int k = 0; k < 3; k++) if (pkt[tri[o + k]] == a) tri[o + k] = mapa[tri[o + k]];
                przy[b].Add(f);
                var n2 = Vector3.Cross(poz[P(f, 1)] - poz[P(f, 0)], poz[P(f, 2)] - poz[P(f, 0)]); if (n2.sqrMagnitude > 1e-24f) nrm[f] = Jedn(n2);
            }
            przy[a].Clear();
            przy[b].RemoveAll(f => !zywy[f]);
            for (int k = 0; k < 10; k++) q[b * 10 + k] += q[a * 10 + k];
            stamp[a]++; stamp[b]++;
            // b's edges costed again (its quadric grew); the others keep theirs
            sasiedzi.Clear();
            foreach (int f in przy[b]) for (int k = 0; k < 3; k++) { int u = P(f, k); if (u != b) sasiedzi.Add(u); }
            foreach (int u in sasiedzi) Wstaw(b, u);
        }
        Przebiegi = przebieg;
        var wynik = new List<int>(zostalo * 3);
        for (int f = 0; f < nt; f++) if (zywy[f]) { wynik.Add(tri[f * 3]); wynik.Add(tri[f * 3 + 1]); wynik.Add(tri[f * 3 + 2]); }
        return wynik.ToArray();
    }
}

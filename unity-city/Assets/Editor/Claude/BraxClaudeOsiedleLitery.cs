using System.Collections.Generic;
using UnityEngine;

// LITERY PRZESTRZENNE (build 14, właściciel 2.11: „szyldy premium wszędzie zamiast płaskich tablic… litery przestrzenne,
// każda litera jako bryła z głębią”). Krój budowany kodem (bez plików czcionek — repo publiczne, zero licencji):
// geometryczny bezszeryfowy, wielkie litery z polskimi znakami, cyfry i kilka znaków. Każdy znak to linie środkowe
// kresek (wysokość wersalika = 1); kreska ma grubość s (ułamek wysokości), lico (materiał lica) i boki (materiał boków),
// bez tylnej ściany (litera stoi na ścianie / listwie / licu kasetonu). Połączenia: łagodne zakręty (łuki) — ucięcie
// na skos (styk bez szczeliny), ostre (A, M, V, W, N) — ośmiokątny węzeł. Końce kresek kwadratowe (przedłużenie o s/2).
// Geometria trafia do bieżącej grupy generatora (scalanie per grupa i materiał, jak cała reszta osiedla).
public static partial class BraxClaudeOsiedle
{
    sealed class Glif
    {
        public float w;                                            // szerokość linii środkowych (wysokość = 1)
        public readonly List<Vector2[]> kreski = new List<Vector2[]>();
        public readonly List<bool> zamkniete = new List<bool>();
        public readonly List<Vector2> kropki = new List<Vector2>();
    }
    static Dictionary<char, Glif> glify;

    static Vector2[] Pk(params float[] xy) { var r = new Vector2[xy.Length / 2]; for (int i = 0; i < r.Length; i++) r[i] = new Vector2(xy[2 * i], xy[2 * i + 1]); return r; }
    // łuk elipsy od kąta a0 do a1 (stopnie, w dowolną stronę)
    static Vector2[] Luk(float cx, float cy, float rx, float ry, float a0, float a1, int seg = 0)
    {
        if (seg <= 0) seg = Mathf.Max(3, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 11.25f));
        var r = new Vector2[seg + 1];
        for (int i = 0; i <= seg; i++) { float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad; r[i] = new Vector2(cx + rx * Mathf.Cos(a), cy + ry * Mathf.Sin(a)); }
        return r;
    }
    static Vector2[] Laczy(params Vector2[][] cz)
    {
        var l = new List<Vector2>();
        foreach (var c in cz) foreach (var p in c) if (l.Count == 0 || (l[l.Count - 1] - p).sqrMagnitude > 1e-6f) l.Add(p);
        return l.ToArray();
    }

    static void Krojownia()
    {
        if (glify != null) return;
        glify = new Dictionary<char, Glif>();
        Glif G(char c, float w) { var g = new Glif { w = w }; glify[c] = g; return g; }
        void K(Glif g, Vector2[] p, bool zamk = false) { g.kreski.Add(p); g.zamkniete.Add(zamk); }
        Glif Kopia(char c, char z) { var s = glify[z]; var g = G(c, s.w); for (int i = 0; i < s.kreski.Count; i++) K(g, s.kreski[i], s.zamkniete[i]); g.kropki.AddRange(s.kropki); return g; }
        void Ostry(Glif g, float x) => K(g, Pk(x - .07f, 1.13f, x + .07f, 1.33f));                    // kreska (ó, ś, ć, ń, ź)
        void Ogonek(Glif g, float x) => K(g, Laczy(Pk(x, 0f), Luk(x - .1f, -.11f, .1f, .11f, 0f, -150f, 6), Pk(x - .02f, -.27f)));

        var g_ = G('A', .72f); K(g_, Pk(0, 0, .36f, 1, .72f, 0)); K(g_, Pk(.115f, .32f, .605f, .32f));
        g_ = G('B', .685f); K(g_, Pk(0, 0, 0, 1));
        K(g_, Laczy(Pk(0, 1, .4f, 1), Luk(.4f, .755f, .245f, .245f, 90, -90), Pk(0, .51f)));
        K(g_, Laczy(Pk(0, .51f, .43f, .51f), Luk(.43f, .255f, .255f, .255f, 90, -90), Pk(0, 0)));
        g_ = G('C', .82f); K(g_, Luk(.41f, .5f, .41f, .5f, 42, 318, 26));
        g_ = G('D', .72f); K(g_, Laczy(Pk(0, 0, 0, 1, .25f, 1), Luk(.25f, .5f, .47f, .5f, 90, -90, 16), Pk(0, 0)), true);
        g_ = G('E', .6f); K(g_, Pk(0, 0, 0, 1)); K(g_, Pk(0, 1, .6f, 1)); K(g_, Pk(0, .5f, .52f, .5f)); K(g_, Pk(0, 0, .6f, 0));
        g_ = G('F', .58f); K(g_, Pk(0, 0, 0, 1)); K(g_, Pk(0, 1, .58f, 1)); K(g_, Pk(0, .5f, .5f, .5f));
        g_ = G('G', .82f); K(g_, Luk(.41f, .5f, .41f, .5f, 42, 360, 26)); K(g_, Pk(.46f, .47f, .82f, .47f));
        g_ = G('H', .66f); K(g_, Pk(0, 0, 0, 1)); K(g_, Pk(.66f, 0, .66f, 1)); K(g_, Pk(0, .5f, .66f, .5f));
        g_ = G('I', 0f); K(g_, Pk(0, 0, 0, 1));
        g_ = G('J', .52f); K(g_, Laczy(Pk(.52f, 1, .52f, .3f), Luk(.26f, .3f, .26f, .3f, 0, -165, 12)));
        g_ = G('K', .64f); K(g_, Pk(0, 0, 0, 1)); K(g_, Pk(.64f, 1, 0, .36f)); K(g_, Pk(.22f, .58f, .66f, 0));
        g_ = G('L', .56f); K(g_, Pk(0, 1, 0, 0, .56f, 0));
        g_ = G('M', .84f); K(g_, Pk(0, 0, 0, 1, .42f, .28f, .84f, 1, .84f, 0));
        g_ = G('N', .68f); K(g_, Pk(0, 0, 0, 1, .68f, 0, .68f, 1));
        g_ = G('O', .86f); K(g_, Luk(.43f, .5f, .43f, .5f, 0, 360, 32), true);
        g_ = G('P', .645f); K(g_, Laczy(Pk(0, 0, 0, 1, .38f, 1), Luk(.38f, .735f, .265f, .265f, 90, -90), Pk(0, .47f)));
        g_ = G('Q', .9f); K(g_, Luk(.43f, .5f, .43f, .5f, 0, 360, 32), true); K(g_, Pk(.56f, .22f, .9f, -.06f));
        g_ = G('R', .66f); K(g_, Laczy(Pk(0, 0, 0, 1, .38f, 1), Luk(.38f, .735f, .265f, .265f, 90, -90), Pk(0, .47f))); K(g_, Pk(.3f, .47f, .66f, 0));
        g_ = G('S', .64f); K(g_, Laczy(Luk(.32f, .75f, .3f, .25f, 28, 270, 16), Luk(.32f, .25f, .32f, .25f, 90, -152, 16)));
        g_ = G('T', .64f); K(g_, Pk(0, 1, .64f, 1)); K(g_, Pk(.32f, 1, .32f, 0));
        g_ = G('U', .66f); K(g_, Laczy(Pk(0, 1, 0, .33f), Luk(.33f, .33f, .33f, .33f, 180, 360, 12), Pk(.66f, 1)));
        g_ = G('V', .72f); K(g_, Pk(0, 1, .36f, 0, .72f, 1));
        g_ = G('W', 1f); K(g_, Pk(0, 1, .23f, 0, .5f, .72f, .77f, 0, 1, 1));
        g_ = G('X', .66f); K(g_, Pk(0, 0, .66f, 1)); K(g_, Pk(0, 1, .66f, 0));
        g_ = G('Y', .68f); K(g_, Pk(0, 1, .34f, .46f)); K(g_, Pk(.68f, 1, .34f, .46f, .34f, 0));
        g_ = G('Z', .62f); K(g_, Pk(0, 1, .62f, 1, 0, 0, .62f, 0));
        // cyfry
        g_ = G('0', .6f); K(g_, Luk(.3f, .5f, .3f, .5f, 0, 360, 28), true);
        g_ = G('1', .32f); K(g_, Pk(0, .76f, .3f, 1, .3f, 0));
        g_ = G('2', .62f); K(g_, Laczy(Luk(.31f, .69f, .3f, .31f, 160, -35, 14), Pk(0, 0, .62f, 0)));
        g_ = G('3', .6f); K(g_, Laczy(Luk(.29f, .75f, .27f, .25f, 150, -90, 14), Luk(.29f, .26f, .31f, .26f, 90, -150, 14)));
        g_ = G('4', .66f); K(g_, Pk(.5f, 0, .5f, 1, 0, .3f, .66f, .3f));
        g_ = G('5', .6f); K(g_, Laczy(Pk(.56f, 1, .1f, 1, .141f, .591f), Luk(.3f, .32f, .3f, .32f, 122, -150, 16)));
        g_ = G('6', .62f); K(g_, Laczy(Luk(.62f, .31f, .62f, .69f, 105, 180, 8), Luk(.31f, .31f, .31f, .31f, 180, -180, 24)));
        g_ = G('7', .6f); K(g_, Pk(0, 1, .6f, 1, .18f, 0));
        g_ = G('8', .6f); K(g_, Luk(.3f, .755f, .26f, .245f, 0, 360, 22), true); K(g_, Luk(.3f, .265f, .3f, .265f, 0, 360, 24), true);
        g_ = G('9', .62f); foreach (var k in glify['6'].kreski) { var r = new Vector2[k.Length]; for (int i = 0; i < k.Length; i++) r[i] = new Vector2(.62f - k[i].x, 1f - k[i].y); K(g_, r); }
        // polskie znaki
        g_ = Kopia('Ą', 'A'); Ogonek(g_, .68f);
        g_ = Kopia('Ć', 'C'); Ostry(g_, .45f);
        g_ = Kopia('Ę', 'E'); Ogonek(g_, .58f);
        g_ = Kopia('Ł', 'L'); K(g_, Pk(-.1f, .34f, .26f, .62f));
        g_ = Kopia('Ń', 'N'); Ostry(g_, .34f);
        g_ = Kopia('Ó', 'O'); Ostry(g_, .43f);
        g_ = Kopia('Ś', 'S'); Ostry(g_, .32f);
        g_ = Kopia('Ź', 'Z'); Ostry(g_, .31f);
        g_ = Kopia('Ż', 'Z'); g_.kropki.Add(new Vector2(.31f, 1.24f));
        // znaki
        g_ = G('·', 0f); g_.kropki.Add(new Vector2(0, .5f));
        g_ = G('.', 0f); g_.kropki.Add(new Vector2(0, 0));
        g_ = G('-', .36f); K(g_, Pk(0, .45f, .36f, .45f));
    }

    const float OdstepSlow = .42f;
    static float SzerokoscNapisu(string tekst, float cap, float s = .15f, float track = .14f)
    {
        Krojownia();
        float x = 0; int n = 0;
        foreach (char c0 in tekst)
        {
            char c = char.ToUpperInvariant(c0);
            if (c == ' ' || !glify.TryGetValue(c, out var g)) { x += OdstepSlow * cap; continue; }
            x += (g.w * (1f - s) + s) * cap + track * cap; n++;
        }
        return n > 0 ? x - track * cap : x;
    }

    static int literyRazem;
    // Napis z liter przestrzennych. o = punkt na linii bazowej (środek napisu przy center=true, lewy koniec przy false),
    // n = kierunek lica (do oglądającego), r = prawo oglądającego (domyślnie cross(góra, −n)), up = góra liter.
    // Lico liter na o + n·depth (tył na o — ściana / listwa / lico kasetonu). Zwraca szerokość napisu.
    static float Napis3D(string tekst, Vector3 o, Vector3 n, float cap, float depth, Material lico, Material boki,
                         float s = .15f, float track = .14f, bool center = true, Vector3 r = default, Vector3 up = default)
    {
        Krojownia();
        if (r == default) r = Vector3.Cross(Vector3.up, -n).normalized;
        if (up == default) up = Vector3.Cross(r, n).normalized;     // r × n = góra (dla r = up × (−n))
        float wTot = SzerokoscNapisu(tekst, cap, s, track);
        float x = center ? -wTot * .5f : 0f;
        float sw = s * cap, k = (1f - s) * cap;
        foreach (char c0 in tekst)
        {
            char c = char.ToUpperInvariant(c0);
            if (c == ' ' || !glify.TryGetValue(c, out var g)) { x += OdstepSlow * cap; continue; }
            Vector3 P(Vector2 q) => o + r * (x + sw * .5f + q.x * k) + up * (sw * .5f + q.y * k);
            for (int i = 0; i < g.kreski.Count; i++) Kreska(g.kreski[i], g.zamkniete[i], P, n, r, up, sw, depth, lico, boki);
            foreach (var d in g.kropki) Kostka(P(d), n, r, up, sw * 1.15f, sw * 1.15f, depth, lico, boki);
            x += (g.w * (1f - s) + s) * cap + track * cap;
            literyRazem++;
        }
        return wTot;
    }
    // Napis pionowy (litery jedna pod drugą, jak „PAWN SHOP” na wysięgniku ze zdjęcia-wzorca): o = środek górnej litery (góra).
    static void NapisPionowy3D(string tekst, Vector3 oGora, Vector3 n, float cap, float odstep, float depth, Material lico, Material boki, float s = .16f)
    {
        float y = 0;
        foreach (char c in tekst)
        {
            if (c != ' ') Napis3D(c.ToString(), oGora - Vector3.up * (y + cap), n, cap, depth, lico, boki, s);
            y += cap + odstep;
        }
    }
    static float WysokoscPionowego(string tekst, float cap, float odstep) => tekst.Length * cap + (tekst.Length - 1) * odstep;

    // Kreska: łamana linii środkowych → prostopadłościany (lico + boki) z ucięciem na skos na łagodnych zakrętach
    // i ośmiokątnym węzłem na ostrych; końce przedłużone o połowę grubości (kwadratowe).
    static void Kreska(Vector2[] pts, bool zamk, System.Func<Vector2, Vector3> P, Vector3 n, Vector3 r, Vector3 up, float sw, float depth, Material lico, Material boki)
    {
        int m = pts.Length; if (m < 2) return;
        var W = new Vector3[m]; for (int i = 0; i < m; i++) W[i] = P(pts[i]);
        bool closed = zamk && (W[0] - W[m - 1]).sqrMagnitude < 1e-6f;
        int segs = m - 1;
        var ext0 = new float[segs]; var ext1 = new float[segs];
        for (int i = 0; i < segs; i++) { ext0[i] = sw * .5f; ext1[i] = sw * .5f; }
        for (int v = 0; v < m; v++)
        {
            int sa = v - 1, sb = v;                                 // segment przed i za wierzchołkiem v
            if (v == 0) { if (!closed) continue; sa = segs - 1; }
            if (v == m - 1) { if (!closed) continue; else continue; } // wierzchołek końcowy = początkowy (obsłużony przy v=0)
            var da = (W[sa + 1] - W[sa]).normalized; var db = (W[sb + 1] - W[sb]).normalized;
            float th = Vector3.Angle(da, db);
            if (th < 62f) { float e = sw * .5f * Mathf.Tan(th * .5f * Mathf.Deg2Rad); ext1[sa] = e; ext0[sb] = e; }
            else { ext1[sa] = 0f; ext0[sb] = 0f; Wezel(W[v], n, r, up, sw, depth, lico, boki); }
        }
        for (int i = 0; i < segs; i++)
        {
            var a = W[i]; var b = W[i + 1]; var d = b - a; float len = d.magnitude; if (len < 1e-5f) continue; d /= len;
            Kostka((a + b) * .5f + d * (ext1[i] - ext0[i]) * .5f, n, d, Vector3.Cross(n, d).normalized, len + ext0[i] + ext1[i], sw, depth, lico, boki);
        }
    }
    // Prostopadłościan litery: środek c (na tylnej płaszczyźnie), oś a (długość la), oś b (szerokość lb), głębokość wzdłuż n.
    static void Kostka(Vector3 c, Vector3 n, Vector3 a, Vector3 b, float la, float lb, float depth, Material lico, Material boki)
    {
        Vector3 A = a * la * .5f, Bv = b * lb * .5f, D = n * depth;
        Vector3 p00 = c - A - Bv, p10 = c + A - Bv, p11 = c + A + Bv, p01 = c - A + Bv;
        QN(lico, p00 + D, p10 + D, p11 + D, p01 + D, n);
        QN(boki, p00, p10, p10 + D, p00 + D, -b); QN(boki, p01, p11, p11 + D, p01 + D, b);
        QN(boki, p00, p01, p01 + D, p00 + D, -a); QN(boki, p10, p11, p11 + D, p10 + D, a);
    }
    // Węzeł ośmiokątny (ostre połączenie kresek): lico + 8 boków, opisany na kole o średnicy grubości kreski.
    static void Wezel(Vector3 c, Vector3 n, Vector3 r, Vector3 up, float sw, float depth, Material lico, Material boki)
    {
        const int N = 8; float R = sw * .5f / Mathf.Cos(Mathf.PI / N);
        var D = n * depth; var p = new Vector3[N];
        for (int i = 0; i < N; i++) { float a = (i + .5f) * Mathf.PI * 2f / N; p[i] = c + (r * Mathf.Cos(a) + up * Mathf.Sin(a)) * R; }
        for (int i = 0; i < N; i++)
        {
            var q = p[(i + 1) % N];
            TN(lico, c + D, p[i] + D, q + D, n);
            var mid = ((p[i] + q) * .5f - c).normalized;
            QN(boki, p[i], q, q + D, p[i] + D, mid);
        }
    }

    // Czworokąt / trójkąt bez rejestracji „spodu stropu” (BraxClaudeOsiedleSufity: daszki i szyldy nie mogą tworzyć
    // płyt blokujących kamerę — właściciel 1.11: „daszki blokowały kamerę”). UV planarne w metrach jak Quad.
    static void QN(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        Planar(n, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(n, u), w) >= 0 ? 1f : -1f;
        int k = b.v.Count;
        foreach (var p in new[] { p0, p1, p2, p3 })
        {
            b.v.Add(p); b.n.Add(n); b.t.Add(new Vector4(u.x, u.y, u.z, sign));
            b.uv.Add(new Vector2(Vector3.Dot(p, u) / s, Vector3.Dot(p, w) / s));
        }
        bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0;
        if (!flip) b.i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        else b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
    }
    static void TN(Material m, Vector3 a, Vector3 c1, Vector3 c2, Vector3 n)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        Planar(n, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(n, u), w) >= 0 ? 1f : -1f;
        int k = b.v.Count;
        foreach (var p in new[] { a, c1, c2 }) { b.v.Add(p); b.n.Add(n); b.t.Add(new Vector4(u.x, u.y, u.z, sign)); b.uv.Add(new Vector2(Vector3.Dot(p, u) / s, Vector3.Dot(p, w) / s)); }
        if (Vector3.Dot(Vector3.Cross(c1 - a, c2 - a), n) >= 0) b.i.AddRange(new[] { k, k + 1, k + 2 }); else b.i.AddRange(new[] { k, k + 2, k + 1 });
    }
    // Prostopadłościan bez kolizji i bez płyty kamery (daszki, szyldy, detale elewacji ponad głową).
    static void BoxN(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool bottom = true)
    {
        if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
        QN(m, P(x0, y1, z0), P(x1, y1, z0), P(x1, y1, z1), P(x0, y1, z1), Vector3.up);
        if (bottom) QN(m, P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1), Vector3.down);
        QN(m, P(x0, y0, z0), P(x1, y0, z0), P(x1, y1, z0), P(x0, y1, z0), Vector3.back);
        QN(m, P(x0, y0, z1), P(x1, y0, z1), P(x1, y1, z1), P(x0, y1, z1), Vector3.forward);
        QN(m, P(x0, y0, z0), P(x0, y0, z1), P(x0, y1, z1), P(x0, y1, z0), Vector3.left);
        QN(m, P(x1, y0, z0), P(x1, y0, z1), P(x1, y1, z1), P(x1, y1, z0), Vector3.right);
        if (y0 < .3f) Hard(x0, x1, z0, z1);
    }
    // Prostopadłościan obrócony (oś a, b, n — ortonormalne), środek c, rozmiary sa, sb, sn; bez kolizji i bez płyty kamery.
    static void OBoxN(Material m, Vector3 c, Vector3 a, Vector3 b, Vector3 n, float sa, float sb, float sn)
    {
        Vector3 A = a * sa * .5f, Bv = b * sb * .5f, N = n * sn * .5f;
        Vector3 P(int i, int j, int k) => c + A * i + Bv * j + N * k;
        QN(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), n); QN(m, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), -n);
        QN(m, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), b); QN(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), -b);
        QN(m, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), a); QN(m, P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1), -a);
    }
    // Rura (jak Tube) bez rejestracji twardego gruntu przy ścianie ponad głową — do wsporników, ramion lamp, drabin.
    static void RuraN(Material m, Vector3 a, Vector3 b, float r, int seg = 8) => Tube(m, a, b, r, seg);
}

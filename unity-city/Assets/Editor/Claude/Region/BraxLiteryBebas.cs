using System.Collections.Generic;
using UnityEngine;

// LITERY PRZESTRZENNE Z FONTU (MIASTO R1, 11.11.2026; właściciel 14:43: „ten napis BRAXHORN GYM ma zrobić ładny w 3D”).
// Zasada napisów premium (10.11): pełne bryły (lico, boki, tył), font premium z licencją w repozytorium — Bebas Neue (SIL OFL 1.1,
// unity-tools/claude/fonts/), kontury i trójkąty lica w BraxLiteryBebasDane.cs (wygenerowane: unity-tools/claude/litery_bebas.py).
// Krzywe spłaszczone co ≤ 3.5 % wysokości wersalika, kerning z tablicy GPOS fontu. Boki: każdy odcinek konturu osobnym
// czworokątem z normalną na zewnątrz litery (kontury zewnętrzne przeciwnie do wskazówek zegara, otwory zgodnie).
internal static partial class BraxLiteryBebas
{
    static float Kern(char a, char b) => Kerning.TryGetValue(new string(new[] { a, b }), out var k) ? k : 0f;

    /// <summary>Pozycje x (w wysokościach wersalika) kolejnych znaków od początku pióra; zwraca zakres atramentu (min, max).</summary>
    static List<(char ch, float x)> Uklad(string tekst, float track, out float min, out float max)
    {
        var l = new List<(char, float)>(); float x = 0f; min = float.MaxValue; max = float.MinValue; char pop = '\0';
        foreach (char c0 in tekst)
        {
            char ch = char.ToUpperInvariant(c0);
            if (!Glify.TryGetValue(ch, out var g)) { x += Spacja + track; pop = '\0'; continue; }
            if (pop != '\0') x += Kern(pop, ch);
            l.Add((ch, x));
            for (int i = 0; i < g.pts.Length; i += 2) { min = Mathf.Min(min, x + g.pts[i]); max = Mathf.Max(max, x + g.pts[i]); }
            x += g.adv + track; pop = ch;
        }
        if (l.Count == 0) { min = max = 0f; }
        return l;
    }

    public static bool Ma(char c) => Glify.ContainsKey(char.ToUpperInvariant(c)) || c == ' ';

    /// <summary>Szerokość atramentu napisu (m) przy wysokości wersalika cap; track = odstęp między literami (w wysokościach wersalika).</summary>
    public static float Szerokosc(string tekst, float cap, float track = .05f)
    {
        Uklad(tekst, track, out float a, out float b); return (b - a) * cap;
    }

    /// <summary>Napis z brył: o = środek linii bazowej na płaszczyźnie montażu, nn = zwrot lica (do oglądającego), cap = wysokość
    /// wersalika, glebokosc = grubość liter, odstep = odległość tyłu liter od płaszczyzny montażu (litery dystansowe). Lico liter
    /// materiałem lico, boki i tył materiałem boki (BraxUliceKit.S — bieżąca grupa scalania). Zwraca szerokość atramentu.</summary>
    /// <summary>Trójkąty liter bez głębi (płaszczyzna r × up, środek atramentu w o) — do farby kładzionej w wycięciu nawierzchni
    /// (ta sama płaszczyzna co nawierzchnia, bez nakładania; zasada 10.11 „zero migotania”).</summary>
    public static List<Vector3[]> Trojkaty(string tekst, Vector3 o, Vector3 r, Vector3 up, float cap, float track = .05f)
    {
        r = r.normalized; up = up.normalized; var l = new List<Vector3[]>();
        var uklad = Uklad(tekst, track, out float a, out float b); float x0 = -(a + b) * .5f;
        foreach (var (ch, xp) in uklad)
        {
            var g = Glify[ch];
            Vector3 P(int i) => o + r * ((x0 + xp + g.pts[2 * i]) * cap) + up * (g.pts[2 * i + 1] * cap);
            for (int t = 0; t + 2 < g.tri.Length; t += 3) l.Add(new[] { P(g.tri[t]), P(g.tri[t + 1]), P(g.tri[t + 2]) });
        }
        return l;
    }

    public static float Napis(string tekst, Vector3 o, Vector3 nn, float cap, float glebokosc, Material lico, Material boki, float track = .05f, float odstep = .004f)
    {
        nn = nn.normalized;
        var r = Vector3.Cross(Vector3.up, -nn).normalized; var up = Vector3.Cross(r, nn).normalized;
        return Napis(tekst, o, r, up, nn, cap, glebokosc, lico, boki, track, odstep);
    }

    /// <summary>Napis w dowolnej płaszczyźnie: r = kierunek pisania (w prawo dla czytającego), up = góra liter, nn = zwrot lica
    /// (np. napis farbą na jezdni: nn = w górę, up = w kierunku jazdy). Pozostałe jak wyżej.</summary>
    public static float Napis(string tekst, Vector3 o, Vector3 r, Vector3 up, Vector3 nn, float cap, float glebokosc, Material lico, Material boki, float track = .05f, float odstep = .004f)
        => Napis(BraxUliceKit.S(lico), BraxUliceKit.S(boki), tekst, o, r, up, nn, cap, glebokosc, track, odstep);

    /// <summary>Jak wyżej, do podanych siatek (lico, boki) — np. meble budowane raz i stawiane wiele razy (BraxChlodnie).</summary>
    public static float Napis(UlSiatka sl, UlSiatka sb, string tekst, Vector3 o, Vector3 r, Vector3 up, Vector3 nn, float cap, float glebokosc, float track = .05f, float odstep = .004f)
    {
        r = r.normalized; up = up.normalized; nn = nn.normalized;
        var uklad = Uklad(tekst, track, out float a, out float b);
        float x0 = -(a + b) * .5f;   // środek atramentu w o
        foreach (var (ch, xp) in uklad)
        {
            var g = Glify[ch];
            int n = g.pts.Length / 2;
            var przod = new Vector3[n]; var tyl = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var p = o + r * ((x0 + xp + g.pts[2 * i]) * cap) + up * (g.pts[2 * i + 1] * cap);
                tyl[i] = p + nn * odstep; przod[i] = p + nn * (odstep + glebokosc);
            }
            for (int t = 0; t < g.tri.Length; t += 3)
            {
                sl.Tri(przod[g.tri[t]], przod[g.tri[t + 1]], przod[g.tri[t + 2]], nn);
                sb.Tri(tyl[g.tri[t]], tyl[g.tri[t + 2]], tyl[g.tri[t + 1]], -nn);
            }
            for (int k = 0; k < g.starty.Length; k++)
            {
                int s0 = g.starty[k], s1 = k + 1 < g.starty.Length ? g.starty[k + 1] : n;
                for (int i = s0; i < s1; i++)
                {
                    int j = i + 1 < s1 ? i + 1 : s0;
                    float dx = g.pts[2 * j] - g.pts[2 * i], dy = g.pts[2 * j + 1] - g.pts[2 * i + 1];
                    var n2 = new Vector2(dy, -dx); if (n2.sqrMagnitude < 1e-12f) continue; n2.Normalize();
                    sb.Quad(tyl[i], tyl[j], przod[j], przod[i], r * n2.x + up * n2.y);
                }
            }
        }
        return (b - a) * cap;
    }

    /// <summary>Napis Bebas W TEKSTURZE (ekrany, telebimy, tablice): px — piksele (wiersz 0 = dół), cx — środek atramentu,
    /// by — linia bazowa, cap — wysokość wersalika (piksele). Trójkąty lica, 4 × 4 próbki na piksel (gładkie krawędzie).</summary>
    public static void WTeksturze(Color[] px, int W, int H, string tekst, float cx, float by, float cap, Color kol, float track = .05f)
    {
        var uklad = Uklad(tekst, track, out float a, out float b); float x0 = cx - (a + b) * .5f * cap;
        foreach (var (ch, xp) in uklad)
        {
            var g = Glify[ch];
            for (int t = 0; t + 2 < g.tri.Length; t += 3)
            {
                Vector2 P(int i) => new Vector2(x0 + (xp + g.pts[2 * i]) * cap, by + g.pts[2 * i + 1] * cap);
                Trojkat(px, W, H, P(g.tri[t]), P(g.tri[t + 1]), P(g.tri[t + 2]), kol);
            }
        }
    }
    static void Trojkat(Color[] px, int W, int H, Vector2 p0, Vector2 p1, Vector2 p2, Color kol)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)))), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)))), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))));
        float Kr(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        float pole = Kr(p0, p1, p2); if (Mathf.Abs(pole) < 1e-6f) return;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
                {
                    var q = new Vector2(x + (sx + .5f) * .25f, y + (sy + .5f) * .25f);
                    float w0 = Kr(p1, p2, q), w1 = Kr(p2, p0, q), w2 = Kr(p0, p1, q);
                    if (pole > 0 ? (w0 >= 0 && w1 >= 0 && w2 >= 0) : (w0 <= 0 && w1 <= 0 && w2 <= 0)) hit++;
                }
                if (hit == 0) continue;
                float cov = hit / 16f * kol.a; int i = y * W + x; var c = px[i];
                px[i] = new Color(Mathf.Lerp(c.r, kol.r, cov), Mathf.Lerp(c.g, kol.g, cov), Mathf.Lerp(c.b, kol.b, cov), 1f);
            }
    }

    /// <summary>„Halo” — podświetlenie LED za literami dystansowymi: płaska poświata w kształcie liter powiększonych o obrys,
    /// w płaszczyźnie montażu o (na nośniku), do materiału świecącego. r, up, nn jak w Napis. Zwraca liczbę trójkątów.</summary>
    public static int Halo(UlSiatka sh, string tekst, Vector3 o, Vector3 nn, float cap, float obrys, float track = .05f)
    {
        nn = nn.normalized; var r = Vector3.Cross(Vector3.up, -nn).normalized; var up = Vector3.Cross(r, nn).normalized;
        int n0 = sh.i.Count;
        var uklad = Uklad(tekst, track, out float a, out float b); float x0 = -(a + b) * .5f;
        foreach (var (ch, xp) in uklad)
        {
            var g = Glify[ch]; int n = g.pts.Length / 2;
            var p = new Vector3[n];
            for (int i = 0; i < n; i++) p[i] = o + r * ((x0 + xp + g.pts[2 * i]) * cap) + up * (g.pts[2 * i + 1] * cap);
            for (int t = 0; t < g.tri.Length; t += 3) sh.Tri(p[g.tri[t]], p[g.tri[t + 1]], p[g.tri[t + 2]], nn);
            for (int k = 0; k < g.starty.Length; k++)
            {
                int s0 = g.starty[k], s1 = k + 1 < g.starty.Length ? g.starty[k + 1] : n;
                for (int i = s0; i < s1; i++)
                {
                    int j = i + 1 < s1 ? i + 1 : s0;
                    float dx = g.pts[2 * j] - g.pts[2 * i], dy = g.pts[2 * j + 1] - g.pts[2 * i + 1];
                    var n2 = new Vector2(dy, -dx); if (n2.sqrMagnitude < 1e-12f) continue; n2.Normalize();
                    var w = (r * n2.x + up * n2.y) * obrys; var dd = (p[j] - p[i]).normalized * obrys * .5f;
                    sh.Quad(p[i], p[j], p[j] + w, p[i] + w, nn);
                    sh.Quad(p[j], p[j] + w, p[j] + w + dd, p[j] + dd, nn);
                }
            }
        }
        return (sh.i.Count - n0) / 3;
    }
}

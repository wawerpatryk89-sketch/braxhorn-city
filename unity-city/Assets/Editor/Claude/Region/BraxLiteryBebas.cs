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
    {
        r = r.normalized; up = up.normalized; nn = nn.normalized;
        var uklad = Uklad(tekst, track, out float a, out float b);
        float x0 = -(a + b) * .5f;   // środek atramentu w o
        var sl = BraxUliceKit.S(lico); var sb = BraxUliceKit.S(boki);
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
}

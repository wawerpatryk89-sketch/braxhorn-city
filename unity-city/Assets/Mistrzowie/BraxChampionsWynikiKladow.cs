using System;
using System.Collections.Generic;
using UnityEngine;

// THE QUAD TRACK'S BEST RESULTS (owner 24.11.2026 20:31 "Tablica z najlepszymi wynikami ... na turniej albo coś takiego", his
// picks "Tablica TOP 10", "Turniej z rywalami", "Podium i puchar"). The ten best of each, as a track's results board keeps them:
//   - the race (2 laps from the gate): everybody over the line on his own time - the rivals ride the same line, so their real
//     times stand beside the player's (an estimated time, a rival still riding when the player finished, never does);
//   - the lap (the lap timer, BraxChampionsTorCzas): the player's laps;
//   - the freestyle jump (BraxChampionsKlad): the player's points for one jump;
//   - the BRAXHORN cup: its last ten winners (the newest first) and the cups the player has won.
// Each row: the value, who (TY = the player), his quad's colour, the day. Kept on this phone (PlayerPrefs, JSON - a per-player
// convenience as the single records before them, which seed the lists once); every read and write guarded: a cleared store
// or a private window just starts the board empty.
public static class BraxChampionsWynikiKladow
{
    public enum Rodzaj { Wyscig, Okrazenie, Skok, Puchar }
    [Serializable] public sealed class Wpis { public float w; public string kto = "", klad = "", data = ""; }
    [Serializable] sealed class Zbior { public List<Wpis> l = new List<Wpis>(); }

    public const int Ile = 10;
    public const string Gracz = "TY";
    static readonly string[] Klucze = { "BRAX_TOR_TOP_WYSCIG", "BRAX_TOR_TOP_OKRAZENIE", "BRAX_TOR_TOP_SKOK", "BRAX_TOR_PUCHAR_ZWYCIEZCY" };
    // the single records kept before the lists (BraxChampionsWyscig, BraxChampionsTorCzas, BraxChampionsKlad): one row each
    static readonly string[] Stare = { "BRAX_TOR_WYSCIG_REKORD", "BRAX_TOR_KLADOW_NAJLEPSZE", "BRAX_KLAD_REKORD_SKOKU", null };
    const string KluczPucharow = "BRAX_TOR_PUCHARY";
    static readonly List<Wpis>[] listy = new List<Wpis>[4];
    static int puchary = -1;

    /// <summary>Bumps on every change (the boards repaint on it).</summary>
    public static int Wersja { get; private set; }

    /// <summary>The ten best (the times shortest first, the jumps most points first, the cup's winners newest first).</summary>
    public static IList<Wpis> Lista(Rodzaj r)
    {
        int i = (int)r;
        if (listy[i] != null) return listy[i];
        var l = new List<Wpis>();
        try
        {
            string json = PlayerPrefs.GetString(Klucze[i], "");
            if (json.Length > 0) { var z = JsonUtility.FromJson<Zbior>(json); if (z != null && z.l != null) l = z.l; }
            else if (Stare[i] != null && PlayerPrefs.HasKey(Stare[i]))
            {
                float w = r == Rodzaj.Skok ? PlayerPrefs.GetInt(Stare[i], 0) : PlayerPrefs.GetFloat(Stare[i], 0f);
                if (w > 0f) l.Add(new Wpis { w = w, kto = Gracz });
            }
        }
        catch { l = new List<Wpis>(); }
        l.RemoveAll(x => x == null || float.IsNaN(x.w) || x.w <= 0f);
        if (l.Count > Ile) l.RemoveRange(Ile, l.Count - Ile);
        return listy[i] = l;
    }

    /// <summary>A lower value is better for the times; a higher for the jumps' points.</summary>
    public static bool Lepszy(Rodzaj r, float a, float b) => r == Rodzaj.Skok ? a > b : a < b;

    /// <summary>A result in: its place on the board (1 - 10), 0 off it. The cup: its winner on top, always.</summary>
    public static int Dodaj(Rodzaj r, float w, string kto, string klad)
    {
        if (float.IsNaN(w) || w <= 0f) return 0;
        var l = (List<Wpis>)Lista(r);
        var nowy = new Wpis { w = w, kto = Krotko(kto, 8), klad = Krotko(klad, 12), data = Dzis() };
        int m;
        if (r == Rodzaj.Puchar) m = 0;
        else { m = l.Count; for (int i = 0; i < l.Count; i++) if (Lepszy(r, w, l[i].w)) { m = i; break; } }   // a tie: the one there first keeps his place
        if (m >= Ile) return 0;
        l.Insert(m, nowy);
        if (l.Count > Ile) l.RemoveRange(Ile, l.Count - Ile);
        Zapisz(r);
        return m + 1;
    }

    /// <summary>The cups the player has won.</summary>
    public static int Puchary { get { if (puchary < 0) { try { puchary = PlayerPrefs.GetInt(KluczPucharow, 0); } catch { puchary = 0; } } return puchary; } }
    public static void DodajPuchar()
    {
        puchary = Puchary + 1; Wersja++;
        try { PlayerPrefs.SetInt(KluczPucharow, puchary); PlayerPrefs.Save(); } catch { }
    }

    static void Zapisz(Rodzaj r)
    {
        Wersja++;
        try { PlayerPrefs.SetString(Klucze[(int)r], JsonUtility.ToJson(new Zbior { l = (List<Wpis>)Lista(r) })); PlayerPrefs.Save(); } catch { }
    }

    static string Dzis() { try { return DateTime.Now.ToString("dd.MM", System.Globalization.CultureInfo.InvariantCulture); } catch { return ""; } }
    static string Krotko(string s, int n) { s = (s ?? "").Trim().ToUpperInvariant(); return s.Length > n ? s.Substring(0, n) : s; }

    /// <summary>The quad's colour as its AKCJA label says it ("KŁAD · CZERWONY" - CZERWONY); a rival's from his machine.</summary>
    public static string Kolor(BraxChampionsKlad k)
    {
        if (!k) return "";
        var rv = k.GetComponent<BraxChampionsKladRywal>();
        if (rv && !string.IsNullOrEmpty(rv.kolor)) return rv.kolor;
        string n = k.nazwa ?? ""; int i = n.LastIndexOf('·');
        return (i >= 0 ? n.Substring(i + 1) : n).Trim();
    }

    /// <summary>A value as the board shows it: a time (m:ss.s), the points.</summary>
    public static string Wartosc(Rodzaj r, float w) => r == Rodzaj.Skok || r == Rodzaj.Puchar ? Mathf.RoundToInt(w) + " PKT" : BraxChampionsTorCzas.Czas(w);
}

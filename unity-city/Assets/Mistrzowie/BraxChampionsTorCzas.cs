using UnityEngine;

// THE QUAD TRACK'S LAP TIMER (owner 24.11.2026 11:45: "dobry zrobić tor ... górki, pagórki, wyskoki, coś fajnego"). The track's
// centre line (world, every 5 m, from the finish line on) is written by the builder (BraxChampionsTorKladow); the ridden quad's
// place on it is followed from where it was (never jumping to a section beside it), and a lap counts when the quad crosses the
// line having passed 90 % of the track in order - a short cut through the infield is no lap. The best lap stays on the phone
// (PlayerPrefs, a per-player convenience).
[DisallowMultipleComponent]
public sealed class BraxChampionsTorCzas : MonoBehaviour
{
    public Vector3[] punkty = new Vector3[0];    // the centre line, world, every ~5 m, index 0 on the finish line
    public float polSzerokosci = 5f;             // half the track's width + its shoulder, world
    public Vector3 PunktStartu => punkty.Length > 0 ? punkty[0] + Vector3.up * 1.5f : transform.position;

    public float Biezace => jedzie ? Time.time - startOkr : 0f;
    public float Najlepsze { get; private set; }
    public float Ostatnie { get; private set; }
    public string Komunikat { get; private set; } = "";
    public float KomunikatDo { get; private set; }

    const string Klucz = "BRAX_TOR_KLADOW_NAJLEPSZE";
    BraxChampionsKlad jedzie;
    int indeks = -1, zaliczone; float startOkr; bool liczy;
    bool[] odwiedzone = new bool[0];

    void Awake() { try { Najlepsze = PlayerPrefs.GetFloat(Klucz, 0f); } catch { Najlepsze = 0f; } }

    public void Zacznij(BraxChampionsKlad k)
    {
        jedzie = k; indeks = Najblizszy(k.transform.position, -1); liczy = false; zaliczone = 0;
        if (odwiedzone.Length != punkty.Length) odwiedzone = new bool[punkty.Length];
        Pokaz("TOR BRAXHORN · PRZEJEDŹ METĘ, BY ZACZĄĆ", 3f);
    }
    public void Zakoncz(BraxChampionsKlad k) { if (jedzie == k) { jedzie = null; liczy = false; } }

    void Pokaz(string t, float s) { Komunikat = t; KomunikatDo = Time.time + s; }

    // the nearest centre-line point: anywhere (from -1), or along the track from the last one
    int Najblizszy(Vector3 p, int od)
    {
        int best = -1; float bd = float.MaxValue, n = punkty.Length;
        if (n == 0) return -1;
        if (od < 0)
        {
            for (int i = 0; i < n; i++) { float d = (punkty[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        for (int k = -6; k <= 12; k++)
        {
            int i = ((od + k) % (int)n + (int)n) % (int)n;
            float d = (punkty[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    void Update()
    {
        if (!jedzie || punkty.Length < 10) return;
        var p = jedzie.transform.position;
        int i = Najblizszy(p, indeks < 0 ? -1 : indeks);
        // off the track (cut across, or pushed off it): found again from anywhere
        if ((punkty[i] - p).sqrMagnitude > (polSzerokosci * 4f) * (polSzerokosci * 4f)) i = Najblizszy(p, -1);
        int n = punkty.Length;
        if (indeks >= 0 && i != indeks)
        {
            bool przezMete = indeks > n - 8 && i < 8;                                  // forward over the line
            if (przezMete)
            {
                if (liczy && zaliczone >= n * .9f)
                {
                    float t = Time.time - startOkr; Ostatnie = t;
                    bool rekord = Najlepsze <= 0f || t < Najlepsze;
                    if (rekord) { Najlepsze = t; try { PlayerPrefs.SetFloat(Klucz, t); PlayerPrefs.Save(); } catch { } }
                    Pokaz((rekord ? "NOWY REKORD! " : "OKRĄŻENIE ") + Czas(t), 4f);
                    Debug.Log("BRAX_TOR_KLADOW lap=" + t.ToString("F2") + " best=" + Najlepsze.ToString("F2"));
                }
                else if (!liczy) Pokaz("START! JEDŹ!", 2f);
                liczy = true; startOkr = Time.time; zaliczone = 0;
                for (int k = 0; k < odwiedzone.Length; k++) odwiedzone[k] = false;
            }
            if (liczy && !odwiedzone[i]) { odwiedzone[i] = true; zaliczone++; }
        }
        indeks = i;
    }

    public static string Czas(float t)
    {
        if (t <= 0f) return "0:00.0";
        int m = (int)(t / 60f); float s = t - m * 60f;
        return m + ":" + s.ToString("00.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}

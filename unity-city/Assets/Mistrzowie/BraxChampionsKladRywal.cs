using UnityEngine;

// A RIVAL OF THE QUAD RACE (owner 24.11.2026 12:57: "wyścig z rywalami"). The rider on one of the race's quads: it rides the
// same machine through the same physics as the player (BraxChampionsKlad - four ray-cast wheels, the grip, the jumps), only
// its hands are this AI's:
//   - the bars: pure pursuit (the way the racing games and the self-driving cars steer) of a point on its lane some metres
//     ahead along the track's centre line, the look-ahead growing with the speed;
//   - the throttle and the brake: the line's own riding speed a little ahead (BraxChampionsWyscig.vCel: the corners from
//     their radius and the berm's bank, the obstacles' caps, the braking for what comes), times the rider's skill - except
//     on a double's run-up, where the speed is the jump's own;
//   - in the air nothing (the rider's body brings the nose, BraxChampionsKlad), no tricks;
//   - a slower rider ahead on its lane: it moves over to pass; a split stretch of the track ahead: its right line;
//   - on its back, stuck or off the track for a while: back on its lane where it was (as a marshal would set it up).
[DisallowMultipleComponent]
[DefaultExecutionOrder(5090)]                                    // the hands before the quad's own physics step
public sealed class BraxChampionsKladRywal : MonoBehaviour
{
    // ---------------------------------------------------------------- set by the builder
    public string imie = "RYWAL";
    public int numer = 1;
    public float umiejetnosc = .9f;              // the share of the line's speed it rides at (0.85 - 0.97)
    public float pas;                            // its lane: metres left (+) / right (-) of the centre line
    public Vector3 parking;                      // world: where it waits in the riders' park
    public Quaternion parkingObrot = Quaternion.identity;
    public float rozstawOsi = 1.25f;             // its machine's wheelbase, real metres

    // ---------------------------------------------------------------- set by the race
    [System.NonSerialized] public BraxChampionsWyscig wyscig;
    [System.NonSerialized] public int Idx;                    // its point on the race's line
    [System.NonSerialized] public bool Aktywny, Meta;

    BraxChampionsKlad klad;
    public BraxChampionsKlad Klad { get { if (!klad) klad = GetComponent<BraxChampionsKlad>(); return klad; } }
    float pasTeraz, przewrot, stoi, zgubiony;

    /// <summary>Back to the riders' park, the engine off.</summary>
    public void Parkuj()
    {
        Aktywny = false; Meta = false;
        if (!Klad) return;
        Klad.Sam(false);
        Klad.Ustaw(parking, parkingObrot, Vector3.zero);
    }

    /// <summary>At its place behind the gate, held, the engine running.</summary>
    public void NaStart(BraxChampionsWyscig w, Vector3 p, Quaternion r)
    {
        wyscig = w; Idx = 0; Meta = false; Aktywny = false; pasTeraz = pas; przewrot = stoi = zgubiony = 0f;
        if (!Klad) return;
        Klad.Sam(true); Klad.Trzymaj(true); Klad.Ustaw(p, r, Vector3.zero);
    }

    void FixedUpdate()
    {
        var w = wyscig; var k = Klad;
        if (!Aktywny || !w || !k || !k.Rywal) return;
        int n = w.punkty.Length; if (n < 10) return;
        float dt = Time.fixedDeltaTime, K = w.K;
        float v = Mathf.Max(0f, k.PredkoscKmh / 3.6f);                       // real m/s along its nose

        // ---- off the line: back on it
        przewrot = k.transform.up.y < .35f ? przewrot + dt : 0f;
        stoi = v < 1f ? stoi + dt : 0f;
        var pt = w.punkty[Idx]; var d = k.transform.position - pt; d.y = 0f;
        zgubiony = d.magnitude / K > w.polSzerokosci + 8f ? zgubiony + dt : 0f;
        if (przewrot > 1.5f || stoi > 3f || zgubiony > 3f) { Wroc(); return; }

        // ---- the lane: its own; round a slower rider close ahead on it
        float cel = pas;
        foreach (var inny in w.Kwady())
        {
            if (!inny || inny == k) continue;
            var lp = k.transform.InverseTransformPoint(inny.transform.position) / K;
            if (lp.z > 0f && lp.z < 9f && Mathf.Abs(lp.x) < 1.6f && inny.PredkoscKmh < k.PredkoscKmh + 3f)
            { cel = pas + (lp.x > 0f ? -1.8f : 1.8f); break; }
        }
        // a split stretch ahead (the track's side lines): the right line, its rollers (the left one's gap jump is the player's)
        int jp = (Idx + Mathf.Max(2, Mathf.RoundToInt((4.5f + v * .5f) / w.krok))) % n;
        if (w.prawa.Length == n && (w.prawa[Idx] || w.prawa[jp])) cel = Mathf.Clamp(cel, -2.6f, -1.1f);
        pasTeraz = Mathf.MoveTowards(pasTeraz, Mathf.Clamp(cel, -2.6f, 2.6f), 1.4f * dt);

        // ---- the bars: pure pursuit of the lane's point ahead
        float ld = 4.5f + v * .5f;                                            // real metres
        int j = (Idx + Mathf.Max(2, Mathf.RoundToInt(ld / w.krok))) % n;
        var cel3 = w.punkty[j] + w.lewo[j] * (pasTeraz * K);
        var lc = k.transform.InverseTransformPoint(cel3);
        float alfa = Mathf.Atan2(lc.x, Mathf.Max(.01f, lc.z));
        float dl = Mathf.Max(1f, new Vector2(lc.x, lc.z).magnitude / K);
        float delta = Mathf.Atan(2f * rozstawOsi * Mathf.Sin(alfa) / dl) * Mathf.Rad2Deg;
        if (lc.z < 0f) delta = Mathf.Sign(lc.x) * 90f;                       // the point behind it: full lock round
        float s = delta / Mathf.Max(1f, k.Skret(v * 3.6f / Mathf.Max(1f, k.vMaxKmh)));

        // ---- the speed: the line's own a little ahead (it brakes for what comes)
        int jv = (Idx + Mathf.Max(1, Mathf.RoundToInt((3f + v * .35f) / w.krok))) % n;
        float vT = w.vCel[jv] * (w.skok[jv] ? 1f : umiejetnosc);
        vT = Mathf.Min(vT, k.vMaxKmh / 3.6f * .97f);
        if (Meta) vT = Mathf.Min(vT, 5f);                                    // over the line: it rolls on and slows
        float e = vT - v;
        float g = e > 0f ? Mathf.Clamp01(.35f + e * .45f) : 0f;
        float b = e < -1.2f && v > 2f ? Mathf.Clamp01(-e * .35f) : 0f;
        if (!k.NaZiemi) g = b = 0f;                                          // in the air its body lands it
        k.Wejscie(g, b, s);
    }

    /// <summary>Set back on its lane where it was, facing the way the track runs, rolling.</summary>
    void Wroc()
    {
        var w = wyscig; var k = Klad; int n = w.punkty.Length;
        int i = Idx, i2 = (Idx + 1) % n;
        var f = w.punkty[i2] - w.punkty[i]; f.y = 0f; if (f.sqrMagnitude < 1e-4f) f = k.transform.forward; f.Normalize();
        var p = w.punkty[i] + w.lewo[i] * (pas * w.K) + Vector3.up * (.6f * w.K);
        k.Ustaw(p, Quaternion.LookRotation(f, Vector3.up), f * Mathf.Min(w.vCel[i] * .5f, 6f) * w.K);
        przewrot = stoi = zgubiony = 0f; pasTeraz = pas;
        Debug.Log("BRAX_WYSCIG rywal " + imie + " wraca na tor idx=" + i);
    }
}

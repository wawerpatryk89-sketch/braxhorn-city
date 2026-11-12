using UnityEngine;

// Owner 12.11 02:47-02:54 ("masz zjeżdżalnię ... zrób basen zajebisty", "wszystko ma być funkcjonalne"): every slide of the
// BRAXHORN aquapark is RIDDEN. At the start gate on the tower AKCJA = ZJEDŹ: the player lies on his back, feet first (the
// shared BraxSeat lie pose), and slides down the slide's own centre line (written by BraxChampionsAquapark: the riding
// surface of the tube / the racer lane, with its surface normal), faster where it drops (energy with friction), turned with
// the spirals, and splashes into the landing pool, where the shared swimming (BraxSwimmingZone) takes over. Pushing the
// joystick or AKCJA on the way = getting off (back at the gate). Nothing runs while nobody rides.
public sealed class BraxChampionsZjezdzalnia : MonoBehaviour, IBraxInterakcja
{
    public string nazwa = "ZJEŻDŻALNIA";
    public Vector3[] trasa = new Vector3[0];   // world: riding surface centre line, gate -> exit
    public Vector3[] normalne = new Vector3[0];// world: the surface normal at each point (toward the tube's axis / up)
    public Vector3 start;                      // world: where the rider stands at the gate
    public Vector3 wyjscie;                    // world: where the ride ends (in the pool's water)
    public float zasieg = 2.4f;                // world: reach of the gate
    public float g = 12.7f;                    // world gravity (9.81 x K)
    public float tarcie = .30f;                // share of the drop lost to friction and water
    public float vStart = 2.6f, vMin = 3.2f, vMax = 15f;   // world m/s
    public float lezenie = .16f;               // world: back above the surface
    public BraxSeat siedzisko;                 // the rider's seat: inactive at rest, moved with him
    public Transform plusk;                    // optional splash at the exit (enabled for a moment)

    BraxGymPlayer gracz; float[] L; float dl, s, v, y0, pluskDo; bool jedzie;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        if (trasa == null || trasa.Length < 2) { enabled = false; return; }
        L = new float[trasa.Length];
        for (int i = 1; i < trasa.Length; i++) L[i] = L[i - 1] + Vector3.Distance(trasa[i - 1], trasa[i]);
        dl = L[L.Length - 1];
        if (siedzisko) siedzisko.gameObject.SetActive(false);
        if (plusk) plusk.gameObject.SetActive(false);
    }

    public string Kontekst(Vector3 p)
    {
        if (jedzie || L == null) return "";
        var d = p - start; if (Mathf.Abs(d.y) > 2.6f) return "";
        d.y = 0f; return d.magnitude < zasieg ? "ZJEDŹ · " + nazwa : "";
    }

    public bool Wykonaj(Vector3 p)
    {
        if (jedzie || !siedzisko || string.IsNullOrEmpty(Kontekst(p))) return false;
        if (!gracz) gracz = FindAnyObjectByType<BraxGymPlayer>();
        if (!gracz || gracz.seat || gracz.vessel) return false;
        siedzisko.gameObject.SetActive(true);
        Ustaw(0f);
        siedzisko.Use(gracz);
        if (gracz.seat != siedzisko) { siedzisko.gameObject.SetActive(false); return false; }
        s = 0f; v = vStart; y0 = trasa[0].y; jedzie = true;
        Polozenie();
        return true;
    }

    // the seat (pose anchor) at distance d along the line: forward = travel direction, the rider's back on the surface
    void Ustaw(float d)
    {
        Punkt(d, out var p, out var t, out var n);
        siedzisko.transform.position = p + n * lezenie;
        siedzisko.transform.rotation = Quaternion.LookRotation(t, n);
        siedzisko.surfaceY = p.y + lezenie;
    }

    void Punkt(float d, out Vector3 p, out Vector3 t, out Vector3 n)
    {
        d = Mathf.Clamp(d, 0f, dl);
        int i = 1; while (i < L.Length - 1 && L[i] < d) i++;
        float f = Mathf.InverseLerp(L[i - 1], L[i], d);
        p = Vector3.Lerp(trasa[i - 1], trasa[i], f);
        t = (trasa[i] - trasa[i - 1]).normalized;
        var a = i - 1 < normalne.Length ? normalne[i - 1] : Vector3.up; var b = i < normalne.Length ? normalne[i] : Vector3.up;
        n = Vector3.Slerp(a, b, f); n = Vector3.ProjectOnPlane(n, t); if (n.sqrMagnitude < 1e-6f) n = Vector3.up; n.Normalize();
    }

    // the rider: feet first along the travel direction, back on the riding surface, face to the normal
    void Polozenie()
    {
        Punkt(s, out var p, out var t, out var n);
        var tr = gracz.transform;
        tr.rotation = Quaternion.LookRotation(n, -t);
        tr.position = p + n * lezenie + t * .9f;
        siedzisko.transform.position = p + n * lezenie;
        siedzisko.transform.rotation = Quaternion.LookRotation(t, n);
    }

    void Update()
    {
        if (plusk && plusk.gameObject.activeSelf && Time.time > pluskDo) plusk.gameObject.SetActive(false);
        if (!jedzie) return;
        if (!gracz || gracz.seat != siedzisko) { Koniec(false); return; }      // got off on the way (joystick / AKCJA)
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Punkt(s, out var p, out _, out _);
        float spadek = Mathf.Max(0f, y0 - p.y);
        float cel = Mathf.Sqrt(vStart * vStart + 2f * g * spadek * (1f - tarcie));
        v = Mathf.Clamp(Mathf.MoveTowards(v, cel, dt * 9f), vMin, vMax);
        s += v * dt;
        if (s >= dl) { Koniec(true); return; }
        Polozenie();
    }

    void Koniec(bool dojechal)
    {
        jedzie = false;
        if (gracz && gracz.seat == siedzisko) siedzisko.Leave();
        if (dojechal && gracz)
        {
            gracz.body.enabled = false;
            gracz.transform.position = wyjscie;
            gracz.transform.rotation = Quaternion.Euler(0f, Quaternion.LookRotation(Vector3.ProjectOnPlane(trasa[trasa.Length - 1] - trasa[trasa.Length - 2], Vector3.up).normalized + Vector3.forward * 1e-4f).eulerAngles.y, 0f);
            gracz.body.enabled = true;
            if (plusk) { plusk.position = wyjscie; plusk.gameObject.SetActive(true); pluskDo = Time.time + 1.2f; }
        }
        siedzisko.gameObject.SetActive(false);
    }
}

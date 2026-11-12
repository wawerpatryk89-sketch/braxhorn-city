using UnityEngine;

// AQUAPARK BRAXHORN - LENIWA RZEKA (owner 12.11 02:53: "zrób basen zajebisty ... wszystko najlepsze"; the region's rule:
// everything works like in life). A ring channel round a palm island; the BRAXHORN swim rings float along it with the
// current all the time (bobbing, slowly turning). AKCJA next to a free ring (from the bank or swimming) = WSIĄDŹ DO KOŁA:
// the player sits in it (the shared BraxSeat sitting pose) and drifts round the island with it. Pushing the joystick or
// AKCJA = getting off: he steps out onto the nearest bank. The river itself is a normal pool (BraxSwimmingZone): you can
// also just swim in it. The rings stop moving when nobody is within 200 m (the camera).
public sealed class BraxChampionsLeniwaRzeka : MonoBehaviour, IBraxInterakcja
{
    public Vector3[] petla = new Vector3[0];     // world: the channel's centre line at the water surface, closed, in the flow's direction
    public Vector3[] brzeg = new Vector3[0];     // world: per loop point, a dry deck point beside the river (getting off there)
    public Transform[] kola = new Transform[0];  // the rings
    public BraxSeat[] siedziska = new BraxSeat[0];// one per ring, inactive at rest, moved with the ring
    public float predkosc = 1.1f;                // world m/s (the current)
    public float zasieg = 3.4f;                  // world: reach to a ring
    public float siedzenie = .10f;               // world: the seat over the ring's centre (the rider's hips in the hole)
    public float kolysanie = .035f;              // world: the bob

    float[] L; float dl; float[] s, obrot; int jedzie = -1; BraxGymPlayer gracz; Vector3 lokalnie; bool blisko = true; float sprawdz;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    void Start()
    {
        if (petla == null || petla.Length < 3 || kola == null || kola.Length == 0) { enabled = false; return; }
        L = new float[petla.Length + 1];
        for (int i = 1; i <= petla.Length; i++) L[i] = L[i - 1] + Vector3.Distance(petla[i - 1], petla[i % petla.Length]);
        dl = L[petla.Length];
        s = new float[kola.Length]; obrot = new float[kola.Length];
        for (int i = 0; i < kola.Length; i++) { s[i] = dl * i / kola.Length; obrot[i] = i * 47f; }
        foreach (var x in siedziska) if (x) x.gameObject.SetActive(false);
        Ustaw(0f);
    }

    void Punkt(float d, out Vector3 p, out Vector3 t, out int i)
    {
        d = Mathf.Repeat(d, dl);
        i = 1; while (i < petla.Length && L[i] < d) i++;
        float f = Mathf.InverseLerp(L[i - 1], L[i], d);
        var a = petla[i - 1]; var b = petla[i % petla.Length];
        p = Vector3.Lerp(a, b, f); t = Vector3.ProjectOnPlane(b - a, Vector3.up).normalized;
        if (t.sqrMagnitude < 1e-6f) t = Vector3.forward;
    }

    int Najblizsze(Vector3 p)
    {
        int best = -1; float bd = zasieg;
        for (int i = 0; i < kola.Length; i++)
        {
            if (!kola[i] || i >= siedziska.Length || !siedziska[i]) continue;
            var d = kola[i].position - p; if (Mathf.Abs(d.y) > zasieg) continue;
            d.y = 0f; if (d.magnitude < bd) { bd = d.magnitude; best = i; }
        }
        return best;
    }

    public string Kontekst(Vector3 p)
    {
        if (jedzie >= 0 || L == null) return "";
        return Najblizsze(p) >= 0 ? "LENIWA RZEKA · WSIĄDŹ DO KOŁA" : "";
    }

    public bool Wykonaj(Vector3 p)
    {
        if (jedzie >= 0 || L == null) return false;
        int i = Najblizsze(p); if (i < 0) return false;
        if (!gracz) gracz = FindAnyObjectByType<BraxGymPlayer>();
        if (!gracz || gracz.seat || gracz.vessel) return false;
        var st = siedziska[i];
        st.gameObject.SetActive(true);
        Siedzisko(i);
        st.Use(gracz);
        if (gracz.seat != st) { st.gameObject.SetActive(false); return false; }
        lokalnie = Quaternion.Inverse(st.transform.rotation) * (gracz.transform.position - st.transform.position);
        jedzie = i;
        return true;
    }

    // the seat of ring i: in its hole, facing the flow
    void Siedzisko(int i)
    {
        var st = siedziska[i]; if (!st) return;
        Punkt(s[i], out _, out var t, out _);
        st.transform.position = kola[i].position + Vector3.up * siedzenie;
        st.transform.rotation = Quaternion.LookRotation(t);
        st.surfaceY = st.transform.position.y;
    }

    void Ustaw(float dt)
    {
        float czas = Time.time;
        for (int i = 0; i < kola.Length; i++)
        {
            if (!kola[i]) continue;
            // the rings keep their spacing: a ring right behind a slower one does not pass through it (all at one speed)
            s[i] = Mathf.Repeat(s[i] + predkosc * dt, dl);
            Punkt(s[i], out var p, out _, out _);
            obrot[i] += dt * (i % 2 == 0 ? 9f : -7f);
            kola[i].position = p + Vector3.up * (Mathf.Sin(czas * 1.3f + i * 1.7f) * kolysanie);
            kola[i].rotation = Quaternion.Euler(Mathf.Sin(czas * .9f + i) * 2.5f, obrot[i], Mathf.Cos(czas * 1.1f + i) * 2.5f);
        }
    }

    void Update()
    {
        if (L == null) return;
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            var kam = Camera.main;
            blisko = jedzie >= 0 || !kam || (kam.transform.position - transform.position).sqrMagnitude < 200f * 200f;
        }
        if (!blisko) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Ustaw(dt);
        if (jedzie < 0) return;
        var st = siedziska[jedzie];
        if (!gracz || gracz.seat != st) { Zejdz(); return; }        // joystick / AKCJA: getting off
        Siedzisko(jedzie);
        var tr = gracz.transform;
        tr.position = st.transform.position + st.transform.rotation * lokalnie;
        tr.rotation = st.transform.rotation;
    }

    void Zejdz()
    {
        int i = jedzie; jedzie = -1;
        var st = siedziska[i];
        if (gracz && gracz.seat == st) st.Leave();
        if (gracz && brzeg != null && brzeg.Length > 0)
        {
            Punkt(s[i], out _, out var t, out int k);
            var b = brzeg[Mathf.Clamp(k, 0, brzeg.Length - 1)];
            var w = Vector3.ProjectOnPlane(kola[i].position - b, Vector3.up);
            gracz.body.enabled = false;
            gracz.transform.position = b;
            gracz.transform.rotation = Quaternion.LookRotation(w.sqrMagnitude > 1e-4f ? w.normalized : t);
            gracz.body.enabled = true;
        }
        if (st) st.gameObject.SetActive(false);
    }
}

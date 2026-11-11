using UnityEngine;

// D2 station train (now built in code - BraxChampionsD2Sklad; owner 11.11 13:08: "pociąg, który tam startuje i wyjeżdża za miasto ... jedzie do innego miasta i znika
// z naszej gry. Taka sztuczka"). The train (BraxChampionsD2.Train: 4 cars on track 1, its east end at the stop mark) is
// moved along the line by this component, on the local x of its parent (the station root, K-scaled offsets):
//   in the tunnel (hidden) -> runs in, braking to the stop at the platform -> stands -> leaves west, accelerating, into the
//   tunnel -> hidden -> the next one comes in on the other track. No physics; a distance check keeps it cheap.
[DisallowMultipleComponent]
public sealed class BraxChampionsD2Pociag : MonoBehaviour
{
    public float wjazdX = -600f;        // local x offset when it appears (east end ~60 m inside the portal)
    public float wyjazdX = -560f;       // local x offset when it is wholly inside the tunnel again
    public float drugiTorZ = 5.8f;      // local z offset of track 2 from track 1
    public float predkosc = 28f;        // line speed (world units / s)
    public float hamowanie = 1.0f;      // deceleration / acceleration (world units / s^2)
    public float postoj = 45f, przerwa = 35f;
    // owner 11.11 15:31 "żeby stał na razie": the train stands at platform 1 with its doors working (one can walk in and sit);
    // only the departure countdown of the boards runs (a new trip number each round). false: the runs in / out of the tunnel.
    public bool staly = true;

    enum Stan { Ukryty, Wjazd, Postoj, Odjazd }
    Stan stan = Stan.Postoj;
    float t, x, v; int tor;
    // for the departure boards (BraxChampionsD2Tablice): the track (0 = peron 1), the trip number (destination), the state
    public int Tor => tor;
    public int Kurs { get; private set; }
    public bool Stoi => stan == Stan.Postoj;
    public bool Wjezdza => stan == Stan.Wjazd;
    public float DoOdjazdu => stan == Stan.Postoj ? t : stan == Stan.Wjazd ? postoj + Mathf.Max(0f, -x) / Mathf.Max(predkosc * .6f, 1f) : stan == Stan.Ukryty ? t + 40f + postoj : 0f;
    Renderer[] rs; Collider[] cs;

    void Start()
    {
        rs = GetComponentsInChildren<Renderer>(true); cs = GetComponentsInChildren<Collider>(true);
        x = 0f; t = postoj * .5f; Pokaz(true); Ustaw();
    }
    void Pokaz(bool on) { foreach (var r in rs) if (r) r.enabled = on; foreach (var c in cs) if (c) c.enabled = on; }
    void Ustaw() { transform.localPosition = new Vector3(x, 0f, tor == 0 ? 0f : drugiTorZ); }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .1f);
        switch (stan)
        {
            case Stan.Ukryty:
                t -= dt; if (t > 0f) return;
                tor = 1 - tor; x = wjazdX; v = predkosc; Kurs++; Pokaz(true); stan = Stan.Wjazd; break;
            case Stan.Wjazd:
            {
                float d = -x;                                                   // distance to the stop mark
                float vMax = Mathf.Sqrt(2f * hamowanie * Mathf.Max(0f, d));
                v = Mathf.Min(predkosc, vMax, v + hamowanie * dt);
                x = Mathf.Min(0f, x + Mathf.Max(v, .15f) * dt);
                if (x >= -.01f) { x = 0f; v = 0f; t = postoj; stan = Stan.Postoj; }
                Ustaw(); break;
            }
            case Stan.Postoj:
                t -= dt;
                if (t <= 0f) { if (staly) { t = postoj; Kurs++; } else stan = Stan.Odjazd; }
                break;
            case Stan.Odjazd:
                v = Mathf.Min(predkosc, v + hamowanie * dt);
                x -= v * dt; Ustaw();
                if (x <= wyjazdX) { Pokaz(false); t = przerwa; stan = Stan.Ukryty; }
                break;
        }
    }
}

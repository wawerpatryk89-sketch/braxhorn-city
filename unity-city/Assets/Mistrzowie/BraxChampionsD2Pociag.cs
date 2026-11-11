using UnityEngine;

// D2 station train (owner 11.11 13:08: "pociąg, który tam startuje i wyjeżdża za miasto ... jedzie do innego miasta i znika
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

    enum Stan { Ukryty, Wjazd, Postoj, Odjazd }
    Stan stan = Stan.Postoj;
    float t, x, v; int tor;
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
                tor = 1 - tor; x = wjazdX; v = predkosc; Pokaz(true); stan = Stan.Wjazd; break;
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
                t -= dt; if (t <= 0f) stan = Stan.Odjazd; break;
            case Stan.Odjazd:
                v = Mathf.Min(predkosc, v + hamowanie * dt);
                x -= v * dt; Ustaw();
                if (x <= wyjazdX) { Pokaz(false); t = przerwa; stan = Stan.Ukryty; }
                break;
        }
    }
}

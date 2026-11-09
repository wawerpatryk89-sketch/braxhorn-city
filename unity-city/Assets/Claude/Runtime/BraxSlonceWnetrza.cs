using UnityEngine;

// SŁOŃCE MIASTA Z CIENIAMI (9.11, zasada 1 D; właściciel: „na średnim cieni nie ma”). Słońce sceny rzuca cienie w czasie
// rzeczywistym (zasięg i kaskady wg trybu BraxGrafika). Wyjątek: wnętrze siłowni startowej — sala była oświetlana tym
// światłem przez dach (bez cieni) i tak ma zostać (bez przyciemnienia sali): gdy kamera jest w jednym z pudeł `bezCieni`,
// cienie słońca są wyłączane, po wyjściu — przywracane. Sprawdzanie co 0.25 s, bez pracy w klatce.
[RequireComponent(typeof(Light))]
public sealed class BraxSlonceWnetrza : MonoBehaviour
{
    public Bounds[] bezCieni = new Bounds[0];
    public LightShadows cienie = LightShadows.Soft;

    Light slonce; float nast; int stan = -1;

    void Awake() { slonce = GetComponent<Light>(); }

    void Update()
    {
        if (Time.unscaledTime < nast || !slonce) return;
        nast = Time.unscaledTime + .25f;
        var kam = Camera.main; if (!kam) return;
        var p = kam.transform.position; bool w = false;
        foreach (var b in bezCieni) if (b.Contains(p)) { w = true; break; }
        int nowy = w ? 1 : 0;
        if (nowy == stan) return;
        stan = nowy;
        slonce.shadows = w ? LightShadows.None : cienie;
    }
}

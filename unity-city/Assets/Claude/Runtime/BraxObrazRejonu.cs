using UnityEngine;

// OBRAZ REJONU (zasada globalna 16.11: „minimum Osiedle, a chcę lepiej”). Ekspozycja światła dziennego materiałów miasta
// (Braxhorn/CityStaticPBR: słońce, otoczenie i odbicie nieba × ekspozycja) i moc słońca sceny — ustawiane przy starcie sceny,
// przywracane przy jej zamknięciu (przejście miasto ↔ osiedle tunelem nie przenosi ustawień do drugiego rejonu).
// W pudłach `bezZmian` (sala siłowni startowej — zatwierdzona, oświetlona po swojemu) ekspozycja 1 i dawna moc słońca.
// Sprawdzanie położenia kamery co 0.25 s, bez pracy w klatce.
public sealed class BraxObrazRejonu : MonoBehaviour
{
    public float ekspozycja = 1f;
    public Light slonce;
    public float slonceZewnatrz = 1.24f, slonceWewnatrz = .75f;
    public Bounds[] bezZmian = new Bounds[0];

    static readonly int Id = Shader.PropertyToID("_BraxEkspozycja");
    float nast; int stan = -1;

    void OnEnable() { stan = -1; Ustaw(false); }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .25f;
        var kam = Camera.main; if (!kam) return;
        var p = kam.transform.position; bool w = false;
        foreach (var b in bezZmian) if (b.Contains(p)) { w = true; break; }
        Ustaw(w);
    }

    void Ustaw(bool wewnatrz)
    {
        int nowy = wewnatrz ? 1 : 0; if (nowy == stan) return;
        stan = nowy;
        Shader.SetGlobalFloat(Id, wewnatrz ? 1f : ekspozycja);
        if (slonce) slonce.intensity = wewnatrz ? slonceWewnatrz : slonceZewnatrz;
    }

    void OnDisable() { Shader.SetGlobalFloat(Id, 0f); stan = -1; }   // 0 = nieustawiona (shader liczy jak 1)
}

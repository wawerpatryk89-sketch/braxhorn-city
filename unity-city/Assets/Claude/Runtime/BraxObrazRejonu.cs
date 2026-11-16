using UnityEngine;

// OBRAZ REJONU (zasada globalna 16.11: „minimum Osiedle, a chcę lepiej”). Ekspozycja światła dziennego materiałów miasta
// (Braxhorn/CityStaticPBR: słońce, otoczenie i odbicie nieba × ekspozycja) i moc słońca sceny — ustawiane przy starcie sceny,
// przywracane przy jej zamknięciu (przejście miasto ↔ osiedle tunelem nie przenosi ustawień do drugiego rejonu).
// W pudłach `bezZmian` (sala siłowni startowej — zatwierdzona, oświetlona po swojemu) ekspozycja 1 i dawna moc słońca.
// Sprawdzanie położenia kamery co 0.25 s, bez pracy w klatce.
// MGŁA: ŻADNA (właściciel 16.11 14:33: „Żadnej kurwa mgły stop”) — mgiełka shaderów miasta (_BraxMgielkaGestosc, ustawiana
// globalnie przez BraxGrafika) w tej scenie = 0; przy zamknięciu sceny wraca poprzednia wartość (inne rejony bez zmian).
public sealed class BraxObrazRejonu : MonoBehaviour
{
    public float ekspozycja = 1f;
    public Light slonce;
    public float slonceZewnatrz = 1.24f, slonceWewnatrz = .75f;
    public Bounds[] bezZmian = new Bounds[0];

    static readonly int Id = Shader.PropertyToID("_BraxEkspozycja");
    static readonly int IdMgielka = Shader.PropertyToID("_BraxMgielkaGestosc");
    float nast; int stan = -1; float mgielkaPrzed = -1f;

    void OnEnable() { stan = -1; Ustaw(false); mgielkaPrzed = Shader.GetGlobalFloat(IdMgielka); Shader.SetGlobalFloat(IdMgielka, 0f); }

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

    void OnDisable()
    {
        Shader.SetGlobalFloat(Id, 0f); stan = -1;   // 0 = nieustawiona (shader liczy jak 1)
        if (mgielkaPrzed >= 0f) Shader.SetGlobalFloat(IdMgielka, mgielkaPrzed);
    }
}

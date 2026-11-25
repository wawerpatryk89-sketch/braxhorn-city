using UnityEngine;

// OBRAZ REJONU (zasada globalna 16.11: „minimum Osiedle, a chcę lepiej”). Ekspozycja światła dziennego materiałów miasta
// (Braxhorn/CityStaticPBR: słońce, otoczenie i odbicie nieba × ekspozycja) i moc słońca sceny — ustawiane przy starcie sceny,
// przywracane przy jej zamknięciu (przejście miasto ↔ osiedle tunelem nie przenosi ustawień do drugiego rejonu).
// W pudłach `bezZmian` (sala siłowni startowej — zatwierdzona, oświetlona po swojemu) ekspozycja 1 i dawna moc słońca.
// Sprawdzanie położenia kamery co 0.25 s, bez pracy w klatce.
// MGŁA: ŻADNA (właściciel 16.11 14:33: „Żadnej kurwa mgły stop”) — mgiełka shaderów miasta (_BraxMgielkaGestosc, ustawiana
// globalnie przez BraxGrafika) w tej scenie = 0; przy zamknięciu sceny wraca poprzednia wartość (inne rejony bez zmian).
//
// DOBA — NOC I DZIEŃ (24.11, 16:31 właściciel: „próbę, noc i dzień”; potrzebne m.in. dla laserów balkonu na Starym Rynku):
// pełna doba `dzienMin + nocMin + 2 × przejscieMin` minut gry (domyślnie 12 + 5 + 2 × 1,5 = 20 min), start rano (dzień);
// zegar gry od uruchomienia (Time.time) — przejście tunelem do innego rejonu i z powrotem nie cofa pory dnia.
// Poziom światła s: 1 w dzień → `nocSwiatlo` w nocy, przejścia łagodne (smoothstep), barwa: biel dnia → pomarańcz zmierzchu
// i świtu → chłodny błękit nocy. Co 0,25 s (bez pracy w klatce):
//  • materiały miasta (CityStaticPBR): ekspozycja × s;
//  • niebo, panorama, teren i las za granicą, szkło fasad, piana, linie boiska, woda (shadery bez świateł Unity):
//    globalne _BraxSwiatlo = s, _BraxSwiatloKolor = barwa (Assets/Claude/Shaders/BraxSwiatlo.cginc);
//  • słońce sceny (cienie, drzewa, modele, shadery Standard): moc × s i barwa — w nocy słabe „światło księżyca”;
//  • otoczenie trilight i odbicia × s (zapamiętane przy starcie, przywracane przy zamknięciu sceny);
//  • w sali siłowni (`bezZmian`) jak dotąd: ekspozycja 1, dawna moc słońca — sala ma własne światło.
// Świecące rzeczy (lampy, diody, okna z pokojami, lasery) nie są przyciemniane — nocą widać je mocniej.
public sealed class BraxObrazRejonu : MonoBehaviour
{
    public float ekspozycja = 1f;
    public Light slonce;
    public float slonceZewnatrz = 1.24f, slonceWewnatrz = .75f;
    public Bounds[] bezZmian = new Bounds[0];

    [Header("Doba (noc i dzień)")]
    public bool doba = false;   // 25.11, 15:24 właściciel: „obecnie żadnej nocy nie chcę […] do testów niepotrzebne” — zawsze dzień
    public float dzienMin = 12f, nocMin = 5f, przejscieMin = 1.5f, nocSwiatlo = .1f, startMin = 2f;
    public Color barwaDnia = new Color(1f, .955f, .875f), barwaZmierzchu = new Color(1f, .62f, .38f), barwaNocy = new Color(.55f, .66f, 1f);

    /// <summary>Poziom światła dnia (1 = dzień, ok. 0,1 = noc) — do odczytu przez inne skrypty (np. lasery, latarnie).</summary>
    public static float Swiatlo { get; private set; } = 1f;

    static readonly int Id = Shader.PropertyToID("_BraxEkspozycja");
    static readonly int IdMgielka = Shader.PropertyToID("_BraxMgielkaGestosc");
    static readonly int IdSwiatlo = Shader.PropertyToID("_BraxSwiatlo");
    static readonly int IdKolor = Shader.PropertyToID("_BraxSwiatloKolor");
    float nast; int stan = -1; float mgielkaPrzed = -1f;
    float ostatnieS = -1f; Color ostatniKolor;
    Color otNiebo, otHoryzont, otZiemia; float odbicia; bool zapamietane;

    void OnEnable()
    {
        stan = -1; mgielkaPrzed = Shader.GetGlobalFloat(IdMgielka); Shader.SetGlobalFloat(IdMgielka, 0f);
        otNiebo = RenderSettings.ambientSkyColor; otHoryzont = RenderSettings.ambientEquatorColor; otZiemia = RenderSettings.ambientGroundColor;
        odbicia = RenderSettings.reflectionIntensity; zapamietane = true; ostatnieS = -1f;
        Ustaw(false);
    }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .25f;
        var kam = Camera.main; if (!kam) return;
        var p = kam.transform.position; bool w = false;
        foreach (var b in bezZmian) if (b.Contains(p)) { w = true; break; }
        Ustaw(w);
    }

    /// <summary>Poziom światła i barwa w chwili t (minuty od początku doby, która zaczyna się świtem).</summary>
    public void Doba(float tMin, out float s, out Color barwa)
    {
        float P = Mathf.Max(.01f, przejscieMin), D = Mathf.Max(.01f, dzienMin), N = Mathf.Max(.01f, nocMin), cykl = D + N + 2f * P;
        float t = Mathf.Repeat(tMin, cykl);
        float d;                                                   // 1 dzień, 0 noc
        if (t < P) d = t / P;                                      // świt
        else if (t < P + D) d = 1f;
        else if (t < 2f * P + D) d = 1f - (t - P - D) / P;         // zmierzch
        else d = 0f;
        d = Mathf.SmoothStep(0f, 1f, d);
        s = Mathf.Lerp(nocSwiatlo, 1f, d);
        float z = 1f - Mathf.Abs(d * 2f - 1f);                     // 1 w połowie przejścia
        barwa = d > .5f ? Color.Lerp(barwaDnia, barwaZmierzchu, z) : Color.Lerp(barwaNocy, barwaZmierzchu, z);
    }

    void Ustaw(bool wewnatrz)
    {
        float s = 1f; Color barwa = barwaDnia;
        if (doba) Doba(startMin + Time.time / 60f, out s, out barwa);
        int nowy = wewnatrz ? 1 : 0;
        bool zmiana = nowy != stan || Mathf.Abs(s - ostatnieS) > .004f || ((Vector4)(barwa - ostatniKolor)).sqrMagnitude > 1e-5f;
        if (!zmiana) return;
        stan = nowy; ostatnieS = s; ostatniKolor = barwa; Swiatlo = s;
        Shader.SetGlobalFloat(Id, wewnatrz ? 1f : ekspozycja * s);
        Shader.SetGlobalFloat(IdSwiatlo, s);
        Shader.SetGlobalColor(IdKolor, new Color(barwa.r, barwa.g, barwa.b, 1f));
        if (slonce)
        {
            slonce.intensity = wewnatrz ? slonceWewnatrz : slonceZewnatrz * s;
            slonce.color = wewnatrz ? barwaDnia : barwa;
        }
        if (zapamietane)
        {
            // sala siłowni (wewnątrz) — otoczenie i odbicia jak dotąd; na zewnątrz × poziom i barwa doby
            float so = wewnatrz ? 1f : s; Color bo = wewnatrz ? Color.white : barwa;
            RenderSettings.ambientSkyColor = otNiebo * bo * so;
            RenderSettings.ambientEquatorColor = otHoryzont * bo * so;
            RenderSettings.ambientGroundColor = otZiemia * bo * so;
            RenderSettings.reflectionIntensity = odbicia * Mathf.Max(so, .12f);
        }
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(Id, 0f); stan = -1;   // 0 = nieustawiona (shader liczy jak 1)
        Shader.SetGlobalFloat(IdSwiatlo, 0f); Shader.SetGlobalColor(IdKolor, new Color(1f, 1f, 1f, 0f)); Swiatlo = 1f;
        if (mgielkaPrzed >= 0f) Shader.SetGlobalFloat(IdMgielka, mgielkaPrzed);
        if (zapamietane)
        {
            RenderSettings.ambientSkyColor = otNiebo; RenderSettings.ambientEquatorColor = otHoryzont; RenderSettings.ambientGroundColor = otZiemia;
            RenderSettings.reflectionIntensity = odbicia; zapamietane = false;
        }
        if (slonce) slonce.color = barwaDnia;
    }
}

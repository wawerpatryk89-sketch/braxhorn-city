using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — JEDEN OBRAZ W KAŻDYM REJONIE (właściciel 16.11, 13:17: „Pierwsze co to parametry
// dla każdego rejonu, każdy build który odpalę ma na nich iść!!!”).
//
// Minimum ustalone na Osiedlu (zasada ZASADA-OBRAZ-MINIMUM-OSIEDLE-16-11, liczby wyjęte
// z BraxClaudeOsiedle.Daylight). Ten plik PODNOSI każdą scenę do tego minimum i nigdy nie
// obniża tego, co rejon ma lepsze — bo Osiedle jest podłogą, nie sufitem.
//
// DLACZEGO RUNTIME, A NIE W SKRYPTACH REJONÓW: każdy rejon ustawia światło we własnym
// skrypcie buildu (Osiedle w Daylight, Miasto w BraxCityBuild). Żeby wymusić jeden obraz,
// trzeba było albo wejść w pliki czterech rejonów naraz — w których w tej chwili pracują
// dwie osoby — albo zrobić to z jednego miejsca po wczytaniu scenu. Wybrane drugie: ten plik
// leży w Assets/Claude (wspólne dla całej gry) i działa po KAŻDYM wczytaniu scenu, dokładnie
// jak BraxGrafika. Żaden plik rejonu nie jest ruszony, a każdy build idzie na tych liczbach.
//
// Podział z BraxGrafika: tam są TRYBY (cienie, LOD, tekstury, klatki, zasięg) — wybór gracza.
// Tu jest OBRAZ SCENY (słońce, otoczenie, mgła, kolor, bliska płaszczyzna) — stały dla całej gry.
//
// Rejon, który naprawdę musi mieć inny obraz (ciemne wnętrze, noc, obóz), wstawia do scenu
// obiekt z komponentem BraxObrazWyjatek i wpisuje powód — wtedy ten plik go pomija i wypisuje
// to w logu, żeby było jawne, a nie po cichu.
public sealed class BraxObrazGlobalny : MonoBehaviour
{
    // ---- MINIMUM (Osiedle) -------------------------------------------------
    public const float SlonceMoc = 1.24f;
    public const float SlonceSilaCieni = .80f;
    public const float SloncePrzesuniecie = .04f;        // shadowBias — z Miasta, tam jest lepiej
    public const float SloncePrzesuniecieNormalne = .25f;
    public const float MglaGestosc = .0042f;
    public const float Nasycenie = 1.15f;
    public const float Kontrast = 1.06f;
    static readonly Color NieboMin = new Color(.60f, .68f, .80f);
    static readonly Color HoryzontMin = new Color(.50f, .53f, .50f);
    static readonly Color ZiemiaMin = new Color(.30f, .28f, .23f);
    // Barwa mgły Osiedla = średni horyzont jego nieba. Każdy rejon ma swoje niebo, więc gdy
    // rejon mgły nie ustawił, bierzemy jego własny kolor tła kamery — nie kolor Osiedla.
    static readonly Color MglaZapas = new Color(.592f, .655f, .792f);

    static BraxObrazGlobalny inst;
    int doZastosowania;
    string ostatniaScena = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst) return;
        var g = new GameObject("BRAXHORN obraz (jeden standard)");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxObrazGlobalny>();
    }

    void Awake()
    {
        SceneManager.sceneLoaded += (s, m) => doZastosowania = 3;
        doZastosowania = 3;
    }

    // Trzy klatki po wczytaniu scenu, tak jak BraxGrafika — skrypty rejonu i gracza ustawiają
    // swoje w Start/Awake, więc jednorazowe ustawienie w Awake dałoby się nadpisać.
    void LateUpdate()
    {
        if (doZastosowania <= 0) return;
        doZastosowania--;
        Zastosuj(doZastosowania == 0);
    }

    static Color Maks(Color a, Color b) =>
        new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b));

    void Zastosuj(bool wypiszLog)
    {
        var scena = SceneManager.GetActiveScene();
        var wyjatek = FindFirstObjectByType<BraxObrazWyjatek>();
        if (wyjatek)
        {
            if (wypiszLog && ostatniaScena != scena.name)
            {
                ostatniaScena = scena.name;
                Debug.Log("BRAX_OBRAZ scena=" + scena.name + " POMINIĘTA — wyjątek: " +
                          (string.IsNullOrEmpty(wyjatek.powod) ? "bez podanego powodu" : wyjatek.powod));
            }
            return;
        }

        // --- słońce ---------------------------------------------------------
        var slonce = FindObjectsByType<Light>(FindObjectsSortMode.None)
                     .FirstOrDefault(l => l.type == LightType.Directional && l.enabled &&
                                          l.cullingMask == -1);   // słońce rejonu, nie światło postaci (warstwa 8)
        float mocBylo = 0f, silaBylo = 0f;
        if (slonce)
        {
            mocBylo = slonce.intensity; silaBylo = slonce.shadowStrength;
            if (slonce.intensity < SlonceMoc) slonce.intensity = SlonceMoc;
            if (slonce.shadows == LightShadows.None) slonce.shadows = LightShadows.Soft;
            if (slonce.shadowStrength < SlonceSilaCieni) slonce.shadowStrength = SlonceSilaCieni;
            // mniejsze przesunięcie = mniej drgania cienia na powierzchniach pod kątem
            if (slonce.shadowBias > SloncePrzesuniecie) slonce.shadowBias = SloncePrzesuniecie;
            if (slonce.shadowNormalBias > SloncePrzesuniecieNormalne)
                slonce.shadowNormalBias = SloncePrzesuniecieNormalne;
        }

        // --- otoczenie ------------------------------------------------------
        if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Trilight)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Maks(RenderSettings.ambientSkyColor, NieboMin);
        RenderSettings.ambientEquatorColor = Maks(RenderSettings.ambientEquatorColor, HoryzontMin);
        RenderSettings.ambientGroundColor = Maks(RenderSettings.ambientGroundColor, ZiemiaMin);

        // --- kamery: bliska płaszczyzna, kolor kadru ------------------------
        var kamery = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                     .Where(c => c && c.targetTexture == null && c.enabled).ToArray();
        int dodanoKolor = 0, dodanoKamere = 0;
        var kolorShader = Shader.Find("Braxhorn/OsiedleKolor") ??
                          Resources.Load<Shader>("OsiedleKolor");
        foreach (var cam in kamery)
        {
            // Bliską płaszczyznę prowadzi BraxClaudeKamera: 0,25 m w otwartym terenie (2,5× lepsza
            // głębia niż 0,1 m — pasy na jezdni przestają migotać) i 0,15 m przy ścianie, żeby róg
            // kadru nie wszedł w mur. Ustawiamy komponent, nie samą liczbę.
            if (!cam.GetComponent<BraxClaudeKamera>())
            {
                cam.gameObject.AddComponent<BraxClaudeKamera>();
                dodanoKamere++;
            }
            else if (cam.nearClipPlane < BraxClaudeKamera.MinBlisko)
                cam.nearClipPlane = BraxClaudeKamera.MinBlisko;

            var kol = cam.GetComponent<BraxClaudeKolor>();
            if (!kol && kolorShader)
            {
                kol = cam.gameObject.AddComponent<BraxClaudeKolor>();
                dodanoKolor++;
            }
            if (kol)
            {
                if (!kol.shader) kol.shader = kolorShader;
                if (kol.nasycenie < Nasycenie) kol.nasycenie = Nasycenie;
                if (kol.kontrast < Kontrast) kol.kontrast = Kontrast;
            }
        }

        // --- mgła -----------------------------------------------------------
        bool mglaByla = RenderSettings.fog;
        if (!RenderSettings.fog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = MglaGestosc;
            // barwa z własnego nieba rejonu: kolor tła kamery, a dopiero w ostateczności Osiedle
            var tlo = kamery.FirstOrDefault(c => c.clearFlags == CameraClearFlags.SolidColor);
            RenderSettings.fogColor = tlo ? tlo.backgroundColor : MglaZapas;
        }
        else if (RenderSettings.fogMode == FogMode.Linear || RenderSettings.fogDensity < MglaGestosc)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = MglaGestosc;
        }

        // --- reszta obrazu --------------------------------------------------
        if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        if (QualitySettings.shadowProjection != ShadowProjection.StableFit)
            QualitySettings.shadowProjection = ShadowProjection.StableFit;

        if (!wypiszLog || ostatniaScena == scena.name) return;
        ostatniaScena = scena.name;
        Debug.Log(string.Format(
            "BRAX_OBRAZ scena={0} slonce={1:F2}->{2:F2} sila_cieni={3:F2}->{4:F2} mgla={5} " +
            "kamer={6} dodano_kolor={7} dodano_kamere={8} anizotropia={9} rzutowanie={10}",
            scena.name, mocBylo, slonce ? slonce.intensity : 0f, silaBylo,
            slonce ? slonce.shadowStrength : 0f,
            mglaByla ? "byla" : "WLACZONA", kamery.Length, dodanoKolor, dodanoKamere,
            QualitySettings.anisotropicFiltering, QualitySettings.shadowProjection));
    }
}

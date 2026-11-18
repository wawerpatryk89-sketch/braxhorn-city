using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — JEDEN OBRAZ W KAŻDYM REJONIE (właściciel 16.11, 13:17: „Pierwsze co to parametry
// dla każdego rejonu, każdy build który odpalę ma na nich iść!!!”).
//
// Minimum ustalone na Osiedlu (zasada ZASADA-OBRAZ-MINIMUM-OSIEDLE-16-11, liczby wyjęte
// z BraxClaudeOsiedle.Daylight). Ten plik PODNOSI scenę do tego minimum i nigdy nie obniża
// tego, co rejon ma lepsze — Osiedle jest podłogą, nie sufitem.
//
// DLACZEGO RUNTIME, A NIE W SKRYPTACH REJONÓW: każdy rejon ustawia światło we własnym skrypcie
// buildu (Osiedle w Daylight, Miasto w BraxCityBuild). Żeby wymusić jeden obraz, trzeba było
// albo wejść w pliki czterech rejonów naraz — w których pracują dwie osoby — albo zrobić to
// z jednego miejsca po wczytaniu sceny. Wybrane drugie, tak jak BraxGrafika: tam są TRYBY
// (cienie, LOD, tekstury, klatki, zasięg) — wybór gracza; tu jest OBRAZ SCENY.
//
// ------------------------------------------------------------------------------------------
// AUDYT WŁASNY 16.11, 14:35 (właściciel: „sprawdzić co zrobiłeś, czy to ma sens”).
// Pierwsza wersja miała TRZY błędy, które psułyby sceny, których nie sprawdziłem:
//
//  1. DOKŁADAŁA BraxClaudeKamera KAŻDEJ KAMERZE. Ten komponent w Awake ustawia
//     QualitySettings.shadowDistance na minimum 60 m. Czyli w trybie NISKIM, gdzie cienie mają
//     być WYŁĄCZONE, wracałyby cienie na 60 m — na najsłabszych telefonach. Teraz bliska
//     płaszczyzna ustawiana jest wprost, bez dokładania komponentu.
//  2. WŁĄCZAŁA MGŁĘ WSZĘDZIE. Siłownia (BraxGymBuild, fog=false) i wnętrza mają ją wyłączoną
//     CELOWO, a tło kamery siłowni jest ciemne (.14,.18,.20) — mgła w tym kolorze zaciemniłaby
//     wnętrze.
//  3. PODNOSIŁA SŁOŃCE WSZĘDZIE do 1.24 i dokładała kolor kadru KAŻDEJ kamerze, także
//     nakładkom interfejsu — więc nasycenie i kontrast mogły nałożyć się dwa razy.
//
// Dlatego parametry są teraz w dwóch grupach:
//  • BEZPIECZNE WSZĘDZIE: anizotropia, StableFit, podłoga bliskiej płaszczyzny, kolor kadru
//    (raz na scenę, na kamerze głównej) — ustawienia techniczne, nie artystyczne.
//  • TYLKO NA OTWARTYM REJONIE: słońce, światło otoczenia, mgła. Rozpoznanie po liczbach,
//    nie po nazwie sceny: zasięg kamery >= 150 m ORAZ w scenie jest niebo (skybox).
//    Zmierzone zasięgi: siłownia 100 m, pokój prób 60 m, Miasto 320–600 m, Osiedle 220–1200 m
//    (ustawiane trybem). Czyli wnętrza wypadają same, bez utrzymywania listy wyjątków.
// ------------------------------------------------------------------------------------------
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
    public const float BliskaOtwarta = .25f;             // otwarty teren
    public const float BliskaMin = .15f;                 // wnętrza
    public const float ZasiegOtwartegoRejonu = 150f;     // niżej = wnętrze, światła nie ruszamy

    static readonly Color NieboMin = new Color(.60f, .68f, .80f);
    static readonly Color HoryzontMin = new Color(.50f, .53f, .50f);
    static readonly Color ZiemiaMin = new Color(.30f, .28f, .23f);
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

    // Trzy klatki po wczytaniu sceny, tak jak BraxGrafika — skrypty rejonu i gracza ustawiają
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

        // kamery rysujące na ekran (nie cubemapy odbić, nie kamery do tekstury)
        var kamery = FindObjectsByType<Camera>(FindObjectsSortMode.None)
                     .Where(c => c && c.targetTexture == null && c.enabled).ToArray();
        if (kamery.Length == 0) return;

        // ---- OTWARTY REJON CZY WNĘTRZE: po liczbach, nie po nazwie -----------
        float najdalszy = kamery.Max(c => c.farClipPlane);
        bool jestNiebo = RenderSettings.skybox != null;
        bool otwarty = najdalszy >= ZasiegOtwartegoRejonu && jestNiebo;

        // ---- BEZPIECZNE WSZĘDZIE ---------------------------------------------
        if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        if (QualitySettings.shadowProjection != ShadowProjection.StableFit)
            QualitySettings.shadowProjection = ShadowProjection.StableFit;

        // Podłoga bliskiej płaszczyzny ustawiana WPROST. Nie dokładamy BraxClaudeKamera — ona
        // w Awake podnosi zasięg cieni do 60 m, co w trybie NISKIM wracałoby z cieniami na
        // najsłabszych telefonach. Kamery, które ten komponent już mają, zostawiamy jemu:
        // prowadzi bliską płaszczyznę co klatkę i zjeżdża przy ścianie.
        float podloga = otwarty ? BliskaOtwarta : BliskaMin;
        int poprawioneBliskie = 0;
        foreach (var cam in kamery)
            if (!cam.GetComponent<BraxClaudeKamera>() && cam.nearClipPlane < podloga)
            {
                cam.nearClipPlane = podloga;
                poprawioneBliskie++;
            }

        // Kolor kadru RAZ NA SCENĘ, na kamerze głównej. Dwie kamery z tym komponentem
        // (np. nakładka interfejsu) nałożyłyby nasycenie i kontrast dwukrotnie.
        var kolorShader = Shader.Find("Braxhorn/OsiedleKolor") ??
                          Resources.Load<Shader>("OsiedleKolor");
        var zKolorem = kamery.FirstOrDefault(c => c.GetComponent<BraxClaudeKolor>());
        int dodanoKolor = 0;
        if (kolorShader)
        {
            var kol = zKolorem ? zKolorem.GetComponent<BraxClaudeKolor>() : null;
            if (!kol)
            {
                var glowna = kamery.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? kamery[0];
                kol = glowna.gameObject.AddComponent<BraxClaudeKolor>();
                dodanoKolor++;
            }
            if (!kol.shader) kol.shader = kolorShader;
            if (kol.nasycenie < Nasycenie) kol.nasycenie = Nasycenie;
            if (kol.kontrast < Kontrast) kol.kontrast = Kontrast;
        }

        // ---- TYLKO OTWARTY REJON: światło, otoczenie, mgła -------------------
        float mocBylo = 0f, silaBylo = 0f;
        Light slonce = null;
        bool mglaByla = RenderSettings.fog;
        if (otwarty)
        {
            slonce = FindObjectsByType<Light>(FindObjectsSortMode.None)
                     .FirstOrDefault(l => l.type == LightType.Directional && l.enabled &&
                                          l.cullingMask == -1);   // słońce rejonu, nie światło postaci (warstwa 8)
            if (slonce)
            {
                mocBylo = slonce.intensity; silaBylo = slonce.shadowStrength;
                if (slonce.intensity < SlonceMoc) slonce.intensity = SlonceMoc;
                if (slonce.shadows == LightShadows.None) slonce.shadows = LightShadows.Soft;
                if (slonce.shadowStrength < SlonceSilaCieni) slonce.shadowStrength = SlonceSilaCieni;
                // PRZESUNIĘCIA CIENIA ZOSTAJĄ TAKIE, JAK USTAWIŁ REJON — i to jest poprawka
                // mojego błędu z 16.11.
                //
                // Zbijałem tu bias do 0,04 i normalBias do 0,25, bo w Mieście były niższe niż
                // w Osiedlu i wyszedłem z założenia, że niżej = lepiej. To jest prawda tylko
                // dla DRGANIA cienia na krawędzi. Dla ACNE — tego stopniowanego migotania na
                // całych powierzchniach — jest dokładnie odwrotnie: przesunięcie musi być
                // WIĘKSZE niż texel mapy cieni rzutowany na świat, a ten rośnie wprost
                // proporcjonalnie do zasięgu cieni. Osiedle ma kilkadziesiąt metrów, miasto
                // ma kilkaset — ten sam bias, który w Osiedlu wystarcza, w mieście jest
                // kilkukrotnie za mały i wtedy MIGOCZE WSZYSTKO.
                //
                // R1 miał normalBias 0,35 i działało; zbiłem mu go do 0,25.
                // To nie jest parametr, który wolno ustalić jedną liczbą dla całej gry.
                // Zostaje u rejonu, a tu tylko trafia do logu, żeby było co porównać.
            }

            if (RenderSettings.ambientMode != UnityEngine.Rendering.AmbientMode.Trilight)
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Maks(RenderSettings.ambientSkyColor, NieboMin);
            RenderSettings.ambientEquatorColor = Maks(RenderSettings.ambientEquatorColor, HoryzontMin);
            RenderSettings.ambientGroundColor = Maks(RenderSettings.ambientGroundColor, ZiemiaMin);

            // Mgła tylko tam, gdzie jest dal. Barwa z WŁASNEGO nieba rejonu (kolor tła jego
            // kamery), nie przepisana z Osiedla — każde niebo ma swój kolor.
            // SPRAWDZONE 16.11 i WAŻNE: samo włączenie mgły NIC NIE POKAŻE na powierzchniach,
            // które mają „nofog”. W Osiedlu mają go OsiedleDal.shader i OsiedleDalLas.shader —
            // czyli daleki teren i daleki las, jedyne dość odległe, by mgłę było widać. Dlatego
            // właściciel jej nie widział. Shaderów tu nie ruszamy — to jego decyzja.
            if (!RenderSettings.fog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = MglaGestosc;
                var tlo = kamery.FirstOrDefault(c => c.clearFlags == CameraClearFlags.SolidColor);
                RenderSettings.fogColor = tlo ? tlo.backgroundColor : MglaZapas;
            }
            else if (RenderSettings.fogMode == FogMode.Linear || RenderSettings.fogDensity < MglaGestosc)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = MglaGestosc;
            }
        }

        if (!wypiszLog || ostatniaScena == scena.name) return;
        ostatniaScena = scena.name;
        Debug.Log(string.Format(
            "BRAX_OBRAZ scena={0} rodzaj={1} zasieg={2:F0}m niebo={3} kamer={4} " +
            "bliska>={5:F2} poprawionych={6} kolor_kadru={7} " +
            "slonce={8:F2}->{9:F2} sila_cieni={10:F2}->{11:F2} mgla={12} " +
            "anizotropia={13} rzutowanie={14} bias={15:F3}/{16:F2}(rejonu, nietkniete)",
            scena.name, otwarty ? "otwarty_rejon" : "wnetrze",
            najdalszy, jestNiebo ? "jest" : "brak", kamery.Length,
            podloga, poprawioneBliskie,
            dodanoKolor > 0 ? "dodany" : (zKolorem ? "byl" : "BRAK_SHADERA"),
            mocBylo, slonce ? slonce.intensity : 0f,
            silaBylo, slonce ? slonce.shadowStrength : 0f,
            otwarty ? (mglaByla ? "byla" : "WLACZONA") : "pominieta_wnetrze",
            QualitySettings.anisotropicFiltering, QualitySettings.shadowProjection,
            slonce ? slonce.shadowBias : 0f, slonce ? slonce.shadowNormalBias : 0f));
    }
}

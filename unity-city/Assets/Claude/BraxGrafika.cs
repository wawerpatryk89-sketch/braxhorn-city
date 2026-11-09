using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — 3 tryby grafiki dla całej gry (decyzja właściciela 31.10, 18:35): NISKA / ŚREDNIA / WYSOKA.
// Wybór w menu (BraxTouchControls) albo klawisz F1/F2/F3 na komputerze; zapamiętany (PlayerPrefs).
// Domyślnie (build 14): wg telefonu — BraxProfilTelefonu (Edge 60 Pro / mocna grafika → WYSOKA, moto g86 → ŚREDNIA,
// < 5 GB RAM → NISKA), komputer → WYSOKA. Wybór ręczny z MENU → USTAWIENIA ma pierwszeństwo (zapamiętany).
// Każdy tryb ustawia naraz: cienie (jakość, zasięg, kaskady), wygładzanie krawędzi, LOD (lodBias = jak daleko
// modele trzymają pełny detal), tekstury (pełne / połowa), rozdzielczość renderu na telefonie, limit klatek,
// zasięg kamery, zasięg trawy 3D na osiedlu. Stosowane po każdym wczytaniu sceny (miasto ↔ osiedle), bo gracz
// i kamery są w scenie. WYSOKA = „petarda” pod komputer: pełna rozdzielczość, MSAA 4×, miękkie cienie do 120 m,
// 4 kaskady, LOD ×2.2, filtr anizotropowy, 60 kl./s.
// Światła postaci (klucz, kontra, wypełnienie — BraxHeroRefinements, tylko warstwa gracza) muszą zostać pikselowe w każdym
// trybie, inaczej Brax robi się płaski: limit świateł pikselowych 3 / 4 / 6 (koszt tylko na postaci; lampy wnętrz są ForceVertex).
public sealed class BraxGrafika : MonoBehaviour
{
    public enum Tryb { Niska = 0, Srednia = 1, Wysoka = 2 }
    public static Tryb Aktualny { get; private set; } = Tryb.Srednia;
    public static readonly string[] Nazwy = { "NISKA", "ŚREDNIA", "WYSOKA" };
    // build 14: nowy klucz — automatyczny tryb wg telefonu (BraxProfilTelefonu) zaczyna od zera, zapamiętany jest tylko wybór ręczny z MENU
    const string Klucz = "braxhorn_grafika_b14";
    public static bool Reczny { get; private set; }   // true = gracz wybrał w MENU (zapamiętane), false = automatycznie wg telefonu
    static BraxGrafika inst;
    static int natW, natH;
    int doZastosowania = 2;          // ile klatek po wczytaniu sceny jeszcze raz nadpisać (gracz w Start ustawia 60 kl./s)
    static bool rozdzielczoscUstawiona;   // telefon: rozdzielczość tylko raz na start i przy zmianie trybu (nie przy każdej scenie)

    // Przed pierwszą sceną: limit mip tekstur i rozdzielczość ustawione, zanim tekstury sceny trafią do pamięci (bez przeładowania)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (inst) return;
        var g = new GameObject("BRAXHORN grafika (3 tryby)");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxGrafika>();
    }

    void Awake()
    {
        natW = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.width;
        natH = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.height;
        // Telefon: domyślnie NISKA (audyt 31.10: 1.1 GB tekstur, część z .glb nieskompresowana — limit mip ½ przed pierwszą
        // sceną = ¼ pamięci tekstur; bez cieni w czasie rzeczywistym). ŚREDNIA / WYSOKA do wybrania w MENU i zapamiętane,
        // ale na telefonie żaden tryb nie zdejmuje limitu tekstur (Mip) — zapamiętany wybór nie może już zamknąć gry na starcie.
        // 9.11: zmiana 7.11 „mip 0 na telefonie” (23e2c7b8, bez cytatu właściciela, bez pomiaru pamięci) COFNIĘTA — to było
        // zabezpieczenie po zamykaniu gry na starcie (właściciel 31.10). Streaming mipmap (jeśli telefon wspiera) zostaje jako dodatek.
        if (Application.isMobilePlatform && SystemInfo.supportsMipStreaming)
        {
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsAddAllCameras = true;
            // 9.11 (właściciel 17:42: „zobacz jak to się rysuje… tak samo wyspa jak do niej płynę — globalnie poprawnie”): budżet
            // 384/512 MB przy redukcji do 3 poziomów zrzucał fasady 1K do 128 px („klocki”, „malowanie”). Tekstury Androida są od 9.11
            // 1K (4× mniej niż 2K z 7.11), więc budżet większy, a redukcja najwyżej o 2 poziomy (1K → 256 px w skrajnym przypadku).
            QualitySettings.streamingMipmapsMaxLevelReduction = 2;
            QualitySettings.streamingMipmapsMemoryBudget = SystemInfo.systemMemorySize >= 6144 ? 1024f : 640f;
        }
        // build 14 (właściciel 21:42–21:46): domyślnie wg telefonu — Edge 60 Pro / mocna grafika = WYSOKA, moto g86 = ŚREDNIA,
        // < 5 GB RAM = NISKA (BraxProfilTelefonu). Tekstury na telefonie i tak zawsze z limitem mip (Mip), więc wyższy tryb
        // nie zwiększa pamięci tekstur przy starcie.
        int domyslny = BraxProfilTelefonu.Domyslny();
        int t = -1;
        try { t = PlayerPrefs.GetInt(Klucz, -1); } catch { }
        Reczny = t >= 0;
        Aktualny = (Tryb)Mathf.Clamp(Reczny ? t : domyslny, 0, 2);
        Debug.Log("BRAXHORN_UI grafika: " + Nazwy[(int)Aktualny] + (Reczny ? " (wybór ręczny)" : " (automatycznie: " + BraxProfilTelefonu.Powod + ")") +
                  " · " + SystemInfo.deviceModel + " · " + SystemInfo.graphicsDeviceName + " · RAM " + SystemInfo.systemMemorySize + " MB");
        SceneManager.sceneLoaded += (s, m) => doZastosowania = 2;
        Zastosuj();
    }

    public static void Ustaw(Tryb t)
    {
        Aktualny = t; Reczny = true;
        try { PlayerPrefs.SetInt(Klucz, (int)t); PlayerPrefs.Save(); } catch { }
        rozdzielczoscUstawiona = false;
        if (inst) { inst.doZastosowania = 2; inst.Zastosuj(); }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) Ustaw(Tryb.Niska);
        else if (Input.GetKeyDown(KeyCode.F2)) Ustaw(Tryb.Srednia);
        else if (Input.GetKeyDown(KeyCode.F3)) Ustaw(Tryb.Wysoka);
    }

    void LateUpdate()
    {
        if (doZastosowania <= 0) return;
        doZastosowania--;
        Zastosuj();
    }

    void Zastosuj()
    {
        bool tel = Application.isMobilePlatform;
        switch (Aktualny)
        {
            case Tryb.Niska:
                // ZASADA 1 (9.11): cienie ze słońca ZAWSZE — Niski: twarde, 1 kaskada, 40 m
                QualitySettings.shadows = ShadowQuality.HardOnly; QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 40f; QualitySettings.shadowCascades = 1; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                Msaa(0); QualitySettings.lodBias = .7f; Mip(1);
                QualitySettings.pixelLightCount = 3; QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .65f : .8f); Klatki(30); Zasieg(1100f); Trawa(12f, 18f);
                break;
            case Tryb.Srednia:
                // ZASADA 1: Średni — cienie miękkie, 2 kaskady, 80 m (właściciel 9.11: „na średnim cieni nie ma”);
                // zasięg kamery 1600 m: wyspa (~1 km od portu) i całe miasto widoczne bez „dorysowywania” w trakcie rejsu
                QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowDistance = 80f; QualitySettings.shadowCascades = 2; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                Msaa(0); QualitySettings.lodBias = 1.25f; Mip(0);
                QualitySettings.pixelLightCount = 4; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .85f : 1f); Klatki(tel ? 30 : 60); Zasieg(1600f); Trawa(22f, 30f);
                break;
            default:
                if (tel)
                {
                    // WYSOKA na telefonie flagowym (Edge 60 Pro, cel 60 kl./s): ZASADA 1 — miękkie cienie 150 m w 4 kaskadach (9.11;
                    // wcześniej 70 m / 2), LOD ×2, zasięg 2200 m, bez odbić w czasie rzeczywistym (Dimensity 8350 szybko się grzeje);
                    // przy spadku klatek BraxTouchControls sam schodzi z 60 do 45 / 30 kl./s.
                    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 150f; QualitySettings.shadowCascades = 4; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.lodBias = 2.0f; Mip(0);
                    QualitySettings.pixelLightCount = 5; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.FourBones;
                    Klatki(60); Zasieg(2200f); Trawa(32f, 42f);
                    break;
                }
                QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 150f; QualitySettings.shadowCascades = 4; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                Msaa(4); QualitySettings.lodBias = 2.2f; Mip(0);
                QualitySettings.pixelLightCount = 6; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.realtimeReflectionProbes = true; QualitySettings.softParticles = true; QualitySettings.skinWeights = SkinWeights.FourBones;
                Rozdzielczosc(1f); Klatki(60); Zasieg(2500f); Trawa(40f, 50f);
                break;
        }
    }

    static void Rozdzielczosc(float skala)
    {
        // Telefon: rozdzielczość renderu zostaje natywna — zmiana bufora ekranu na starcie zamykała aplikację po instalacji
        // (właściciel 31.10, 21:34: „wypierdala mnie po instalacji przy włączaniu”). Płynność: cienie, LOD, tekstury, 30 kl./s.
        if (Application.isMobilePlatform) return;
        if (natW <= 0 || rozdzielczoscUstawiona) return;   // komputer / przeglądarka: okno bez zmian
        rozdzielczoscUstawiona = true;
        int w = Mathf.RoundToInt(natW * skala), h = Mathf.RoundToInt(natH * skala);
        if (Mathf.Abs(Screen.width - w) > 2 || Mathf.Abs(Screen.height - h) > 2) Screen.SetResolution(w, h, true);
    }

    // Wygładzanie krawędzi zmieniane w trakcie gry odtwarza bufor ekranu — na części telefonów (Vulkan) to zamyka aplikację
    // (właściciel 31.10, 21:33: „wypierdala z aplikacji” po APK d260250). Na telefonie MSAA zostaje takie, jak w buildzie.
    // Limit mip tekstur tylko przy zmianie wartości: każde przypisanie przeładowuje wszystkie tekstury sceny z dysku
    // (na telefonie z 4 GB to skok pamięci i zamknięcie aplikacji). Na start ustawiany przed pierwszą sceną (BeforeSceneLoad).
    // Telefon: tekstury zawsze w połowie rozdzielczości, w każdym trybie (audyt miasta 31.10: zapisany wybór ŚREDNIA dawał przy
    // kolejnym starcie pełne tekstury = znowu zamykanie gry; zmiana limitu w menu przeładowywała wszystkie tekstury naraz).
    // Limit ustawiony raz przed pierwszą sceną i nigdy nie zmieniany na telefonie. Pełne tekstury wrócą po podziale miasta na paczki.
    // 9.11: przywrócone po zmianie 7.11 (mip 0) bez pomiaru pamięci.
    // 9.11 17:42 (właściciel: „zobacz jak to się rysuje”, „ma być jak najlepsza jakość”): wymuszona połowa tekstur na telefonie
    // ZDJĘTA — od 9.11 tekstury Androida mają 1K (zasada 1), czyli pełna rozdzielczość 1K zajmuje tyle co połowa dawnych 2K, przy
    // których gra działała (APK 8.11). Połowa zostaje tylko w trybie NISKA. Pamięć pilnuje budżet streamingu mipmap.
    // 9.11 (właściciel: „zajebista realna panorama z dachu Tower”): perspektywa powietrzna w shaderze miasta (CityStaticPBR) —
    // to samo prawo i kolor mgiełki co tło nieba i teren za granicą (MiastoTlo: 1 − e^(−d·0.00042), kolor .70/.76/.83):
    // 100 m → 4 %, 500 m → 19 %, 1 km → 34 %. Miasto z dachu przechodzi w horyzont jak w rzeczywistości.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Mgielka()
    {
        Shader.SetGlobalColor("_BraxMgielkaKolor", new Color(.70f, .76f, .83f, 1f));
        Shader.SetGlobalFloat("_BraxMgielkaGestosc", .00042f);
    }

    static void Mip(int n)
    {
        if (QualitySettings.globalTextureMipmapLimit != n) QualitySettings.globalTextureMipmapLimit = n;
    }
    static void Msaa(int n)
    {
        if (Application.isMobilePlatform) return;
        if (QualitySettings.antiAliasing != n) QualitySettings.antiAliasing = n;
    }

    static void Klatki(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    static void Zasieg(float far)
    {
        foreach (var c in Camera.allCameras)
            if (c && c.targetTexture == null) c.farClipPlane = far;
    }

    // Trawa 3D osiedla (BraxClaudeTrawa) — zasięg murawy i łąki; w mieście komponentu nie ma (pętla nic nie robi).
    static void Trawa(float murawa, float laka)
    {
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!mb || mb.GetType().Name != "BraxClaudeTrawa") continue;
            var t = mb.GetType();
            var fm = t.GetField("zasiegMurawy"); var fl = t.GetField("zasiegLaki");
            if (fm != null) fm.SetValue(mb, murawa);
            if (fl != null) fl.SetValue(mb, laka);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

// BRAXHORN — 3 tryby grafiki dla całej gry (decyzja właściciela 31.10, 18:35): NISKA / ŚREDNIA / WYSOKA.
// Wybór w menu (BraxTouchControls) albo klawisz F1/F2/F3 na komputerze; zapamiętany (PlayerPrefs).
// Domyślnie: telefon z < 6 GB RAM → NISKA, inny telefon → ŚREDNIA, komputer → WYSOKA.
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
    const string Klucz = "braxhorn_grafika";
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
        int domyslny = Application.isMobilePlatform ? (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 5800 ? 0 : 1) : 2;
        int t = domyslny;
        try { t = PlayerPrefs.GetInt(Klucz, domyslny); } catch { }
        Aktualny = (Tryb)Mathf.Clamp(t, 0, 2);
        SceneManager.sceneLoaded += (s, m) => doZastosowania = 2;
        Zastosuj();
    }

    public static void Ustaw(Tryb t)
    {
        Aktualny = t;
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
                QualitySettings.shadows = ShadowQuality.Disable; QualitySettings.shadowDistance = 0f;
                Msaa(0); QualitySettings.lodBias = .45f; Mip(1);
                QualitySettings.pixelLightCount = 3; QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .65f : .8f); Klatki(30); Zasieg(220f); Trawa(12f, 18f);
                break;
            case Tryb.Srednia:
                QualitySettings.shadows = ShadowQuality.HardOnly; QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 35f; QualitySettings.shadowCascades = 1; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                Msaa(0); QualitySettings.lodBias = .8f; Mip(0);
                QualitySettings.pixelLightCount = 4; QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .85f : 1f); Klatki(tel ? 30 : 60); Zasieg(420f); Trawa(22f, 30f);
                break;
            default:
                QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 120f; QualitySettings.shadowCascades = 4; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                Msaa(4); QualitySettings.lodBias = 2.2f; Mip(0);
                QualitySettings.pixelLightCount = 6; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.realtimeReflectionProbes = true; QualitySettings.softParticles = true; QualitySettings.skinWeights = SkinWeights.FourBones;
                Rozdzielczosc(1f); Klatki(60); Zasieg(1200f); Trawa(40f, 50f);
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
    static void Mip(int n) { if (QualitySettings.globalTextureMipmapLimit != n) QualitySettings.globalTextureMipmapLimit = n; }
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

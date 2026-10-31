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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
                QualitySettings.antiAliasing = 0; QualitySettings.lodBias = .45f; QualitySettings.globalTextureMipmapLimit = 1;
                QualitySettings.pixelLightCount = 3; QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .65f : .8f); Klatki(30); Zasieg(220f); Trawa(12f, 18f);
                break;
            case Tryb.Srednia:
                QualitySettings.shadows = ShadowQuality.HardOnly; QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 35f; QualitySettings.shadowCascades = 1; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                QualitySettings.antiAliasing = 0; QualitySettings.lodBias = .8f; QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.pixelLightCount = 4; QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.realtimeReflectionProbes = false; QualitySettings.softParticles = false; QualitySettings.skinWeights = SkinWeights.TwoBones;
                Rozdzielczosc(tel ? .85f : 1f); Klatki(tel ? 30 : 60); Zasieg(420f); Trawa(22f, 30f);
                break;
            default:
                QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 120f; QualitySettings.shadowCascades = 4; QualitySettings.shadowProjection = ShadowProjection.StableFit;
                QualitySettings.antiAliasing = 4; QualitySettings.lodBias = 2.2f; QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.pixelLightCount = 6; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.realtimeReflectionProbes = true; QualitySettings.softParticles = true; QualitySettings.skinWeights = SkinWeights.FourBones;
                Rozdzielczosc(1f); Klatki(60); Zasieg(1200f); Trawa(40f, 50f);
                break;
        }
    }

    static void Rozdzielczosc(float skala)
    {
        if (!Application.isMobilePlatform || natW <= 0) return;   // na komputerze / w przeglądarce okno zostaje takie, jakie jest
        int w = Mathf.RoundToInt(natW * skala), h = Mathf.RoundToInt(natH * skala);
        if (Screen.width != w || Screen.height != h) Screen.SetResolution(w, h, true);
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

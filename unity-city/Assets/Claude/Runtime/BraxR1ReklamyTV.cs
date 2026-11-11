using UnityEngine;
using UnityEngine.Video;

// BRAXHORN — filmy właściciela na telewizorach MIASTA R1 (11.11.2026, właściciel 21:40: „Twój kolega zrobił telewizory, które
// grają, filmy odtwarzają, tak samo możesz z nich skorzystać, żeby w każdym pomieszczeniu były”; 20:56: „jak jest telewizor, to
// w telewizorach lecą filmiki moje”). Wzór: BraxChampionsReklamyTV kolegi (Miasto Mistrzów) — tu kopia R1, bez zmian w jego kodzie.
// Jeden VideoPlayer bez dźwięku → jedna mała RenderTexture (640 × 360) → obraz WSPÓLNEGO materiału ekranów R1 (wszystkie
// telewizory miasta pokazują tę samą reklamę, jak pętla demonstracyjna w sklepie). Pliki ze StreamingAssets/R1/Reklamy po kolei,
// w kółko. Gra tylko, gdy gracz (BraxGymPlayer) jest bliżej niż `zasieg` od któregoś telewizora; dalej ekrany wracają do obrazu
// stałego (oszczędność baterii). Brak plików (build bez klucza) — telewizory pokazują obraz stały.
public sealed class BraxR1ReklamyTV : MonoBehaviour
{
    public Material[] ekrany = new Material[0];
    public string katalog = "R1/Reklamy";
    public string[] pliki = new string[0];
    public Vector3[] telewizory = new Vector3[0];
    public float zasieg = 35f;

    VideoPlayer vp; RenderTexture rt; Texture[] staryObraz, staraEmisja; int nr, bledy; bool gra, wylaczone; float nastepnaKontrola, szukaj;
    Transform gracz;

    void Start()
    {
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false; vp.isLooping = false; vp.skipOnDrop = true; vp.waitForFirstFrame = true;
        vp.renderMode = VideoRenderMode.RenderTexture; vp.aspectRatio = VideoAspectRatio.FitInside;
        vp.audioOutputMode = VideoAudioOutputMode.None; vp.source = VideoSource.Url;
        vp.loopPointReached += Nastepny;
        vp.errorReceived += (s, m) => { bledy++; Debug.Log("BRAXHORN_R1_TV blad pliku " + m); if (bledy >= Mathf.Max(1, pliki.Length)) { wylaczone = true; Wylacz(); } else Nastepny(s); };
        vp.prepareCompleted += s => { bledy = 0; s.Play(); };
        Debug.Log("BRAXHORN_R1_TV telewizorow " + telewizory.Length + ", plikow " + pliki.Length);
    }

    void Update()
    {
        if (wylaczone || pliki.Length == 0 || ekrany.Length == 0 || Time.unscaledTime < nastepnaKontrola) return;
        nastepnaKontrola = Time.unscaledTime + .5f;
        if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + 1f; var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        bool blisko = false;
        if (gracz) foreach (var t in telewizory) if ((t - gracz.position).sqrMagnitude < zasieg * zasieg) { blisko = true; break; }
        if (blisko && !gra) Wlacz(); else if (!blisko && gra) Wylacz();
    }

    void Wlacz()
    {
        if (!rt) { rt = new RenderTexture(640, 360, 0) { name = "R1: reklamy TV" }; rt.Create(); }
        vp.targetTexture = rt;
        staryObraz = new Texture[ekrany.Length]; staraEmisja = new Texture[ekrany.Length];
        for (int i = 0; i < ekrany.Length; i++)
        {
            if (!ekrany[i]) continue;
            staryObraz[i] = ekrany[i].GetTexture("_MainTex"); staraEmisja[i] = ekrany[i].HasProperty("_EmissionMap") ? ekrany[i].GetTexture("_EmissionMap") : null;
            ekrany[i].SetTexture("_MainTex", rt); if (ekrany[i].HasProperty("_EmissionMap")) ekrany[i].SetTexture("_EmissionMap", rt);
        }
        gra = true; Graj();
    }

    void Graj()
    {
        vp.url = Application.streamingAssetsPath.TrimEnd('/') + "/" + katalog.Trim('/') + "/" + pliki[nr % pliki.Length];
        vp.Prepare();
    }

    void Nastepny(VideoPlayer s)
    {
        if (!gra) return;
        nr = (nr + 1) % Mathf.Max(1, pliki.Length);
        Graj();
    }

    void Wylacz()
    {
        if (vp) vp.Stop();
        if (staryObraz != null)
            for (int i = 0; i < ekrany.Length; i++)
                if (ekrany[i]) { ekrany[i].SetTexture("_MainTex", staryObraz[i]); if (ekrany[i].HasProperty("_EmissionMap")) ekrany[i].SetTexture("_EmissionMap", staraEmisja[i]); }
        gra = false;
    }

    void OnDisable() => Wylacz();
    void OnDestroy() { Wylacz(); if (rt) { rt.Release(); Destroy(rt); } }
}

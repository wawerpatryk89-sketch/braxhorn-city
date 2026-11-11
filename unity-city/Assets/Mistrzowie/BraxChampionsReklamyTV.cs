using UnityEngine;
using UnityEngine.Video;

// The owner's short films looping on every BRAXHORN TV (owner 11.11 20:46-20:47: "w tych telewizorach one leciały ... w
// sklepach, w każdym jednym telewizorze, żeby coś leciało ... niech się zapętli"). One VideoPlayer, no sound, into one
// small RenderTexture (640 x 360) that becomes the picture of the catalogue's TV materials (BraxKatalog "tv_0".."tv_3"):
// every TV in the gallery shows the same ad, like a shop's demo loop. Plays the files of StreamingAssets/<katalog> one
// after another, round and round, only while the camera is within `promien` of `srodek` (world); outside it the TVs get
// their still pictures back. A missing file (no pack in this build) is skipped; with none at all the TVs stay still.
public sealed class BraxChampionsReklamyTV : MonoBehaviour
{
    public Material[] ekrany = new Material[0];
    public string katalog = "Mistrzowie/Reklamy";
    public string[] pliki = new string[0];
    public Vector3 srodek;
    public float promien = 150f;

    VideoPlayer vp; RenderTexture rt; Texture[] staryObraz, staraEmisja; int nr, bledy; bool gra, wylaczone; float nastepnaKontrola;

    void Start()
    {
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false; vp.isLooping = false; vp.skipOnDrop = true; vp.waitForFirstFrame = true;
        vp.renderMode = VideoRenderMode.RenderTexture; vp.aspectRatio = VideoAspectRatio.FitInside;
        vp.audioOutputMode = VideoAudioOutputMode.None; vp.source = VideoSource.Url;
        vp.loopPointReached += Nastepny;
        vp.errorReceived += (s, m) => { bledy++; if (bledy >= Mathf.Max(1, pliki.Length)) { wylaczone = true; Wylacz(); } else Nastepny(s); };
        vp.prepareCompleted += s => { bledy = 0; s.Play(); };
    }

    void Update()
    {
        if (wylaczone || pliki.Length == 0 || ekrany.Length == 0 || Time.unscaledTime < nastepnaKontrola) return;
        nastepnaKontrola = Time.unscaledTime + .5f;
        var cam = Camera.main;
        bool blisko = cam && (cam.transform.position - srodek).sqrMagnitude < promien * promien;
        if (blisko && !gra) Wlacz(); else if (!blisko && gra) Wylacz();
    }

    void Wlacz()
    {
        if (!rt) { rt = new RenderTexture(640, 360, 0) { name = "reklamy TV" }; rt.Create(); }
        vp.targetTexture = rt;
        staryObraz = new Texture[ekrany.Length]; staraEmisja = new Texture[ekrany.Length];
        for (int i = 0; i < ekrany.Length; i++)
        {
            if (!ekrany[i]) continue;
            staryObraz[i] = ekrany[i].GetTexture("_MainTex"); staraEmisja[i] = ekrany[i].GetTexture("_EmissionMap");
            ekrany[i].SetTexture("_MainTex", rt); ekrany[i].SetTexture("_EmissionMap", rt);
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
                if (ekrany[i]) { ekrany[i].SetTexture("_MainTex", staryObraz[i]); ekrany[i].SetTexture("_EmissionMap", staraEmisja[i]); }
        gra = false;
    }

    void OnDisable() => Wylacz();
    void OnDestroy() { Wylacz(); if (rt) { rt.Release(); Destroy(rt); } }
}

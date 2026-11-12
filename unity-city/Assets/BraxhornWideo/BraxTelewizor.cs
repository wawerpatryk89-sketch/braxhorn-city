using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

// BRAXHORN — TELEWIZORY I FILMY W CAŁEJ GRZE (owner 11.11.2026 20:46-20:58: "w każdym jednym telewizorze, żeby coś
// leciało", "w hotelach czy w apartamentach ... globalnie, żeby były te filmiki dostępne", "każdy telewizor ma być
// włączany, ma mieć opcję włączenia, wyłączenia i ma w nim lecieć filmik"; rule docs/zasady/ZASADA_TELEWIZORY_FILMY_20261111.md).
//
// The library: StreamingAssets/BraxhornWideo/tv/*.mp4 (16:9, 640 x 360, mono sound) and .../pion/*.mp4 (9:16, 360 x 640),
// staged in CI from the encrypted pack unity-city/Prywatne/braxhorn-wideo-20261111 (the repository is public). The lists
// below are the catalogue every generator uses; a new film = the file in the pack + its name here.
//
// BraxTelewizor sits on a TV's screen (its own renderer): the AKCJA button near it says WŁĄCZ TV / WYŁĄCZ TV (the shared
// BraxInterakcje register). A TV that is on shows its still picture until it is one of the two nearest TVs that are on
// (within `zasieg`): those get one of the two shared VideoPlayers and play the catalogue round and round, each from its own
// place in the list, with the sound from the TV (3D, quiet beyond a few metres). A TV that is off is black glass.
// A missing file (no pack in this build) is skipped; with none at all (every file of a list failed in a row) that list is
// marked missing for the session and the TVs keep their still pictures (no retry loop).
public static class BraxWideo
{
    // owner 12.11 11:54: "wszystkie filmy, które ci dałem, 9/16, wypierdalasz ... tylko mają być filmy w kinie i w
    // telewizorach te krótkie reklamy, co ci wysłałem. 16/9" - the TVs play only his five 16:9 ads (the films that came as
    // 9:16 - korytarz, auto, basen, hala, teledysk - are out in both versions, and no portrait screen plays anything)
    public static readonly string[] Tv =
    {
        "tv_01_puma.mp4", "tv_02_alpy.mp4", "tv_03_roboty.mp4", "tv_04_gangsterzy.mp4", "tv_05_fabryka.mp4",
    };
    public static readonly string[] Pion = { };
    public static string Sciezka(bool pion, string plik) =>
        Application.streamingAssetsPath.TrimEnd('/') + "/BraxhornWideo/" + (pion ? "pion/" : "tv/") + plik;
}

public sealed class BraxTelewizor : MonoBehaviour, IBraxInterakcja
{
    public bool wlaczony = true;
    public bool pion;                      // a portrait screen (advertising totem): the 9:16 list
    public float zasieg = 26f;             // world units: plays only this near
    public float zasiegPrzycisku = 2.8f;   // world units: the AKCJA button
    public Material obraz, wylaczony;      // the still picture, the black glass of a TV that is off
    public int start;                      // its first film in the list

    Renderer r; int nr; Slot slot;
    static readonly List<BraxTelewizor> wszystkie = new List<BraxTelewizor>();

    sealed class Slot { public VideoPlayer vp; public AudioSource glos; public RenderTexture rt; public Material mat; public BraxTelewizor tv; public int bledy; }
    static Slot[] sloty; static GameObject host; static float nastepny;
    static readonly bool[] brak = new bool[2];   // [0] the 16:9 list, [1] the 9:16 list: every file failed - stills only
    static int Ile(bool pion) => brak[pion ? 1 : 0] ? 0 : (pion ? BraxWideo.Pion.Length : BraxWideo.Tv.Length);

    void Awake() { r = GetComponent<Renderer>(); nr = start; Pokaz(); }
    void OnEnable() { wszystkie.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { wszystkie.Remove(this); BraxInterakcje.Usun(this); Zwolnij(); }

    public string Kontekst(Vector3 gracz) => (gracz - transform.position).sqrMagnitude < zasiegPrzycisku * zasiegPrzycisku ? (wlaczony ? "WYŁĄCZ TV" : "WŁĄCZ TV") : "";
    public bool Wykonaj(Vector3 gracz) { wlaczony = !wlaczony; if (!wlaczony) Zwolnij(); Pokaz(); return true; }

    void Pokaz()
    {
        if (!r) return;
        r.sharedMaterial = !wlaczony ? (wylaczony ? wylaczony : obraz) : slot != null ? slot.mat : obraz;
    }
    void Zwolnij()
    {
        if (slot == null) return;
        slot.vp.Stop(); slot.tv = null; slot = null; Pokaz();
    }

    // the two nearest TVs that are on get the players (checked twice a second by whichever TV runs Update first)
    void Update()
    {
        if (Time.unscaledTime < nastepny) return;
        nastepny = Time.unscaledTime + .5f;
        var cam = Camera.main; if (!cam) return;
        var p = cam.transform.position;
        if (sloty == null) Utworz();
        BraxTelewizor a = null, b = null; float da = float.MaxValue, db = float.MaxValue;
        foreach (var t in wszystkie)
        {
            if (!t || !t.wlaczony || Ile(t.pion) == 0) continue;
            float d = (t.transform.position - p).sqrMagnitude;
            if (d > t.zasieg * t.zasieg) continue;
            if (d < da) { b = a; db = da; a = t; da = d; } else if (d < db) { b = t; db = d; }
        }
        foreach (var s in sloty) if (s.tv != null && s.tv != a && s.tv != b) s.tv.Zwolnij();
        foreach (var t in new[] { a, b })
        {
            if (t == null || t.slot != null) continue;
            foreach (var s in sloty) if (s.tv == null) { t.Przydziel(s); break; }
        }
    }

    static void Utworz()
    {
        host = new GameObject("BraxTelewizor | odtwarzacze (2)"); DontDestroyOnLoad(host);
        sloty = new Slot[2];
        for (int i = 0; i < 2; i++)
        {
            var g = new GameObject("odtwarzacz " + (i + 1)); g.transform.SetParent(host.transform, false);
            var s = new Slot { vp = g.AddComponent<VideoPlayer>(), glos = g.AddComponent<AudioSource>() };
            s.glos.spatialBlend = 1f; s.glos.minDistance = 1.5f; s.glos.maxDistance = 9f; s.glos.rolloffMode = AudioRolloffMode.Linear; s.glos.volume = .7f;
            s.vp.playOnAwake = false; s.vp.isLooping = false; s.vp.skipOnDrop = true; s.vp.source = VideoSource.Url;
            s.vp.renderMode = VideoRenderMode.RenderTexture; s.vp.aspectRatio = VideoAspectRatio.FitInside;
            s.vp.audioOutputMode = VideoAudioOutputMode.AudioSource; s.vp.controlledAudioTrackCount = 1; s.vp.EnableAudioTrack(0, true); s.vp.SetTargetAudioSource(0, s.glos);
            s.vp.prepareCompleted += v => { s.bledy = 0; v.Play(); };
            s.vp.loopPointReached += v => { if (s.tv != null) { s.tv.nr++; s.tv.Graj(); } };
            s.vp.errorReceived += (v, m) =>
            {
                if (s.tv == null) return;
                var tv = s.tv; s.bledy++;
                if (s.bledy >= (tv.pion ? BraxWideo.Pion.Length : BraxWideo.Tv.Length)) { brak[tv.pion ? 1 : 0] = true; s.bledy = 0; tv.Zwolnij(); }
                else { tv.nr++; tv.Graj(); }
            };
            sloty[i] = s;
        }
    }

    void Przydziel(Slot s)
    {
        s.tv = this; slot = s; s.bledy = 0;
        int w = pion ? 360 : 640, h = pion ? 640 : 360;
        if (!s.rt || s.rt.width != w || s.rt.height != h) { if (s.rt) { s.rt.Release(); Destroy(s.rt); } s.rt = new RenderTexture(w, h, 0) { name = "BraxTelewizor obraz" }; s.rt.Create(); }
        if (!s.mat) s.mat = new Material(obraz ? obraz : r.sharedMaterial) { name = "BraxTelewizor film" };
        s.mat.SetTexture("_MainTex", s.rt); s.mat.SetTextureScale("_MainTex", Vector2.one); s.mat.SetTextureOffset("_MainTex", Vector2.zero);
        if (s.mat.HasProperty("_EmissionMap")) s.mat.SetTexture("_EmissionMap", s.rt);
        s.vp.targetTexture = s.rt; s.glos.transform.position = transform.position;
        Pokaz(); Graj();
    }

    void Graj()
    {
        if (slot == null) return;
        var lista = pion ? BraxWideo.Pion : BraxWideo.Tv;
        if (lista.Length == 0) return;                                    // an empty list (no portrait films any more)
        slot.vp.url = BraxWideo.Sciezka(pion, lista[((nr % lista.Length) + lista.Length) % lista.Length]);
        slot.vp.Prepare();
    }
}

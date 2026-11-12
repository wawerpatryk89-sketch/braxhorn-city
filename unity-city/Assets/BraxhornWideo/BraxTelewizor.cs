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
// BraxTelewizor sits on a TV's screen (its own renderer): the AKCJA button near it says WŁĄCZ TV / NASTĘPNY FILM · <name> /
// WYŁĄCZ TV (the shared BraxInterakcje register). A TV that is on shows its still picture until it is one of the two nearest
// TVs that are on and in sight (within `zasieg`): those get one of the two shared VideoPlayers and play the catalogue round
// and round, with the sound from the screen (3D, quiet beyond a few metres, never through a wall). A TV that is off is black glass.
// A missing file (no pack in this build) is skipped; with none at all (every file of a list failed in a row) that list is
// marked missing for the session and the TVs keep their still pictures (no retry loop).
public static class BraxWideo
{
    // owner 12.11 11:54: "wszystkie filmy, które ci dałem, 9/16, wypierdalasz ... tylko mają być filmy w kinie i w
    // telewizorach te krótkie reklamy, co ci wysłałem. 16/9" - the TVs play only his five 16:9 ads
    public static readonly string[] Tv =
    {
        "tv_01_puma.mp4", "tv_02_alpy.mp4", "tv_03_roboty.mp4", "tv_04_gangsterzy.mp4", "tv_05_fabryka.mp4",
    };
    // owner 12.11 12:25: "tylko ten jeden, ten teledysk ... to możesz zostawić 9:16, tam gdzie ten teledysk jest ... resztę
    // 9:16 usuwasz" - the one portrait film: his music video, on the G1 gallery's totems where it played
    public static readonly string[] Pion = { "pion_10_teledysk.mp4" };
    public static string Sciezka(bool pion, string plik) =>
        Application.streamingAssetsPath.TrimEnd('/') + "/BraxhornWideo/" + (pion ? "pion/" : "tv/") + plik;
    /// <summary>the film's name for the AKCJA button: "tv_02_alpy.mp4" -> "ALPY"</summary>
    public static string Tytul(string plik)
    {
        var t = System.IO.Path.GetFileNameWithoutExtension(plik);
        var c = t.Split('_');
        return (c.Length > 2 ? string.Join(" ", c, 2, c.Length - 2) : t).ToUpperInvariant();
    }
}

// owner 12.11 12:21 / 12:25: "na korytarzu w apartamencie słychać coś z mojego filmika, ale nie ma nigdzie mojego filmika na
// żadnym telewizorze. Nie ma przycisku włącz, wybierz se filmik" / "nie ma być w żadnym korytarzu słychać żadnego
// telewizora, tylko w pokojach". The cause: the generators bake the picture quad in the building's space, so the screen's
// transform sat at the building's origin (a corridor) - the sound came from there, the "two nearest" were picked by that
// point (screens in other flats) and the AKCJA button showed only at that point. Now the screen knows its real centre and
// facing (set by the generator, BraxKatalog.WorkingScreen), it plays only in sight of the camera or the player (a line
// with no wall, closed door or floor in between) and stops the moment it is out of sight, and AKCJA picks the film:
// WŁĄCZ TV -> NASTĘPNY FILM · <name> (k/n) for each film -> WYŁĄCZ TV after the last.
public sealed class BraxTelewizor : MonoBehaviour, IBraxInterakcja
{
    public bool wlaczony = true;
    public bool pion;                      // a portrait screen (advertising totem): the 9:16 list
    public float zasieg = 14f;             // world units: plays only this near (and only in sight)
    public float zasiegPrzycisku = 3.2f;   // world units: the AKCJA button
    public Material obraz, wylaczony;      // the still picture, the black glass of a TV that is off
    public int start;                      // its first film in the list
    public bool maMiejsce;                 // the generator set the picture's centre and facing below
    public Vector3 srodekLokalny, przodLokalny = Vector3.forward;   // in this object's space

    Renderer r; int nr; Slot slot; Vector3 srodek, przod;
    static readonly List<BraxTelewizor> wszystkie = new List<BraxTelewizor>();
    static Vector3 gracz; static float graczCzas = -99f;
    static readonly RaycastHit[] trafienia = new RaycastHit[16];

    sealed class Slot { public VideoPlayer vp; public AudioSource glos; public RenderTexture rt; public Material mat; public BraxTelewizor tv; public int bledy; public bool gotowy; }
    static Slot[] sloty; static GameObject host; static float nastepny;
    static readonly bool[] brak = new bool[2];   // [0] the 16:9 list, [1] the 9:16 list: every file failed - stills only
    static int Ile(bool pion) => brak[pion ? 1 : 0] ? 0 : (pion ? BraxWideo.Pion.Length : BraxWideo.Tv.Length);
    string[] Lista => pion ? BraxWideo.Pion : BraxWideo.Tv;
    int Biezacy(int n) => n <= 0 ? 0 : ((nr % n) + n) % n;

    void Awake() { r = GetComponent<Renderer>(); nr = start; Miejsce(); Pokaz(); }
    void OnEnable() { wszystkie.Add(this); BraxInterakcje.Dodaj(this); }
    void OnDisable() { wszystkie.Remove(this); BraxInterakcje.Usun(this); Zwolnij(); }

    void Miejsce()
    {
        if (maMiejsce) { srodek = transform.TransformPoint(srodekLokalny); przod = transform.TransformDirection(przodLokalny).normalized; }
        else { srodek = r ? r.bounds.center : transform.position; przod = Vector3.zero; }
    }

    // in sight: in front of the picture and nothing solid and still (a wall, a closed door, a floor) on the line to it;
    // the player's capsule, people, cars and triggers do not count
    bool Widac(Vector3 oko)
    {
        if (przod != Vector3.zero && Vector3.Dot(oko - srodek, przod) <= .02f) return false;
        var cel = przod != Vector3.zero ? srodek + przod * .25f : srodek;
        var d = cel - oko; float dl = d.magnitude; if (dl < .01f) return true;
        int n = Physics.RaycastNonAlloc(oko, d / dl, trafienia, dl, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = trafienia[i].collider;
            if (!c || c is CharacterController || trafienia[i].rigidbody) continue;
            if (c.transform.IsChildOf(transform.parent ? transform.parent : transform)) continue;   // its own frame
            return false;
        }
        return true;
    }

    public string Kontekst(Vector3 g)
    {
        gracz = g; graczCzas = Time.unscaledTime;
        if ((g - srodek).sqrMagnitude > zasiegPrzycisku * zasiegPrzycisku) return "";
        if (!Widac(g + Vector3.up * 1.3f)) return "";
        if (!wlaczony) return "WŁĄCZ TV";
        int n = Ile(pion), cur = Biezacy(n);
        if (n > 1 && cur < n - 1) return "NASTĘPNY FILM · " + BraxWideo.Tytul(Lista[cur + 1]) + " (" + (cur + 2) + "/" + n + ")";
        return "WYŁĄCZ TV";
    }

    public bool Wykonaj(Vector3 g)
    {
        int n = Ile(pion), cur = Biezacy(n);
        if (!wlaczony) { wlaczony = true; nr = 0; nastepny = 0f; Pokaz(); return true; }
        if (n > 1 && cur < n - 1) { nr = cur + 1; if (slot != null) Graj(); return true; }
        wlaczony = false; Zwolnij(); Pokaz(); return true;
    }

    void Pokaz()
    {
        if (!r) return;
        r.sharedMaterial = !wlaczony ? (wylaczony ? wylaczony : obraz) : slot != null && slot.gotowy ? slot.mat : obraz;
    }
    void Zwolnij()
    {
        if (slot == null) return;
        var s = slot; slot = null; s.vp.Stop(); s.tv = null; s.gotowy = false; Pokaz();
    }

    // the two nearest TVs that are on and in sight get the players (checked twice a second by whichever TV runs Update first)
    void Update()
    {
        if (Time.unscaledTime < nastepny) return;
        nastepny = Time.unscaledTime + .5f;
        var cam = Camera.main; if (!cam) return;
        var p = cam.transform.position;
        bool zGracza = Time.unscaledTime - graczCzas < 2f; var oko = gracz + Vector3.up * 1.3f;
        if (sloty == null) Utworz();
        BraxTelewizor a = null, b = null; float da = float.MaxValue, db = float.MaxValue;
        foreach (var t in wszystkie)
        {
            if (!t || !t.wlaczony || Ile(t.pion) == 0) continue;
            float d = (t.srodek - p).sqrMagnitude;
            if (d > t.zasieg * t.zasieg) continue;
            if (!t.Widac(p) && !(zGracza && t.Widac(oko))) continue;
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
            s.glos.spatialBlend = 1f; s.glos.minDistance = 1.5f; s.glos.maxDistance = 7f; s.glos.rolloffMode = AudioRolloffMode.Linear; s.glos.volume = .7f;
            s.vp.playOnAwake = false; s.vp.isLooping = false; s.vp.skipOnDrop = true; s.vp.source = VideoSource.Url;
            s.vp.renderMode = VideoRenderMode.RenderTexture; s.vp.aspectRatio = VideoAspectRatio.FitInside;
            s.vp.audioOutputMode = VideoAudioOutputMode.AudioSource; s.vp.controlledAudioTrackCount = 1; s.vp.EnableAudioTrack(0, true); s.vp.SetTargetAudioSource(0, s.glos);
            s.vp.prepareCompleted += v => { s.bledy = 0; v.Play(); if (s.tv != null && !s.gotowy) { s.gotowy = true; s.tv.Pokaz(); } };
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
        s.tv = this; slot = s; s.bledy = 0; s.gotowy = false;
        int w = pion ? 360 : 640, h = pion ? 640 : 360;
        if (!s.rt || s.rt.width != w || s.rt.height != h) { if (s.rt) { s.rt.Release(); Destroy(s.rt); } s.rt = new RenderTexture(w, h, 0) { name = "BraxTelewizor obraz" }; s.rt.Create(); }
        if (!s.mat) s.mat = new Material(obraz ? obraz : r.sharedMaterial) { name = "BraxTelewizor film" };
        s.mat.SetTexture("_MainTex", s.rt); s.mat.SetTextureScale("_MainTex", Vector2.one); s.mat.SetTextureOffset("_MainTex", Vector2.zero);
        if (s.mat.HasProperty("_EmissionMap")) s.mat.SetTexture("_EmissionMap", s.rt);
        s.vp.targetTexture = s.rt; s.glos.transform.position = srodek;
        Pokaz(); Graj();
    }

    void Graj()
    {
        if (slot == null) return;
        var lista = Lista;
        if (lista.Length == 0) return;
        slot.vp.url = BraxWideo.Sciezka(pion, lista[Biezacy(lista.Length)]);
        slot.vp.Prepare();
    }
}

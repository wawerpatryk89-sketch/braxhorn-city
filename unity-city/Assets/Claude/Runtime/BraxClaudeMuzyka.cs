using UnityEngine;

// BRAXHORN R2 — muzyka w budynku (siłownia, kasyno, dyskoteka). Właściciel 1.11, 18:06: „jak jesteś na zewnątrz, słychać
// muzykę w środku; wchodzisz do środka — normalnie; oddalasz się od głośników — słabiej słychać; w siłowni głośniki
// w narożnikach u góry”. Jedno źródło (strumień pliku raz, tanio na telefonie), głośność i barwa liczone z pozycji gracza:
//  • w środku (prostopadłościan `wnetrze`): pełne pasmo, głośność z odległości do głośników (blisko głośnika głośniej,
//    w przeciwnym końcu sali ok. połowy), lekkie stereo w stronę bliższych głośników;
//  • na zewnątrz: zza ścian — filtr dolnoprzepustowy (słychać bas i stłumiony środek), ciszej, zanika do `zasiegZewn` m.
// Gracz szukany po nazwie typu (BraxGymPlayer jest w innym zestawie skryptów), raz na sekundę, dopóki go nie ma.
public class BraxClaudeMuzyka : MonoBehaviour
{
    public AudioClip utwor;
    public Bounds wnetrze;                 // świat: sala z muzyką
    public Vector3[] glosniki = new Vector3[0];   // świat: głośniki (puste = górne narożniki sali)
    public float glosnosc = .5f, wygaszenie = 4f, szybkosc = .6f;
    public float zasiegZewn = 22f, przezSciane = .42f, pasmoZewn = 650f;
    // AUDIO STREF (runda 5, koordynator: „w środku głośno, przy otwartych drzwiach ciszej, za ścianą bez drzwi cisza”):
    // na zewnątrz dźwięk idzie TYLKO przez otwory — drzwi z BraxCityDoor przy ścianach sali (gdy otwarte) i otwory stałe
    // (`otwory`: drzwi bez skrzydła, podniesione bramy). Przy otworze ok. połowa głośności, zanik do `zasiegOtworu` m,
    // stłumione; bez otworu w pobliżu — cisza. Sala bez żadnych otworów (nieznane wejście) zostaje w starym trybie zza ściany
    // i jest wypisana w BRAXHORN_AUDIO_STREFY. `synteza` — dźwięk strefy z kodu (BraxDzwiekiStref), gdy brak pliku.
    public Vector3[] otwory = new Vector3[0];
    public string synteza;
    public float zasiegOtworu = 14f, przezOtwor = .5f;
    public bool trybOtworow = true;
    BraxCityDoor[] drzwi = new BraxCityDoor[0];
    AudioSource src; AudioLowPassFilter lp; Transform gracz; float szukaj, pasmo = 22000f;

    void Start()
    {
        if (!utwor && !string.IsNullOrEmpty(synteza)) utwor = BraxDzwiekiStref.Klip(synteza);
        var e = wnetrze; e.Expand(new Vector3(3f, 1f, 3f)); var lista = new System.Collections.Generic.List<BraxCityDoor>();
        foreach (var d in BraxCityDoor.All) if (d && e.Contains(d.transform.position)) lista.Add(d);
        drzwi = lista.ToArray();
        src = gameObject.AddComponent<AudioSource>();
        src.clip = utwor; src.loop = true; src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 0f; src.priority = 64;
        lp = gameObject.AddComponent<AudioLowPassFilter>(); lp.cutoffFrequency = pasmo; lp.lowpassResonanceQ = 1f;
        if (glosniki == null || glosniki.Length == 0)
        {
            Vector3 a = wnetrze.min, b = wnetrze.max; float y = Mathf.Min(b.y - .7f, a.y + 3.6f), i = .6f;
            glosniki = new[] { new Vector3(a.x + i, y, a.z + i), new Vector3(b.x - i, y, a.z + i), new Vector3(a.x + i, y, b.z - i), new Vector3(b.x - i, y, b.z - i) };
        }
    }

    internal static Transform Gracz(ref float nastepnaProba)
    {
        if (Time.unscaledTime < nastepnaProba) return null;
        nastepnaProba = Time.unscaledTime + 1f;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb && mb.GetType().Name == "BraxGymPlayer") return mb.transform;
        return null;
    }

    // Wspólne dla muzyki w salach i wieży w willi: głośność, pasmo filtra i stereo z pozycji słuchacza.
    internal static void Licz(Vector3 p, Bounds sala, Vector3[] zrodla, float glos, float zasieg, float przezSc, float pasmoZ,
                              out float cel, out float celPasmo, out float pan)
    {
        float d = Mathf.Sqrt(sala.SqrDistance(p));
        var kam = Camera.main; Vector3 prawo = kam ? kam.transform.right : Vector3.right; prawo.y = 0f;
        Vector3 kier = Vector3.zero;
        if (d < .05f)
        {
            float s = 0f;
            foreach (var g in zrodla)
            {
                Vector3 v = g - p; float dd = v.magnitude, w = 1f / (1f + (dd / 5f) * (dd / 5f));
                s += w; if (dd > .01f) kier += v / dd * w;
            }
            cel = glos * Mathf.Clamp(s, .45f, 1f); celPasmo = 22000f;
        }
        else
        {
            float t = Mathf.Clamp01(1f - d / zasieg);
            cel = glos * przezSc * t * (.35f + .65f * t);
            celPasmo = Mathf.Lerp(pasmoZ, pasmoZ * 1.6f, Mathf.Clamp01(1f - d / 3f));   // tuż przy ścianie/drzwiach trochę jaśniej
            kier = (sala.center - p).normalized;
        }
        pan = kier.sqrMagnitude > 1e-4f ? Mathf.Clamp(Vector3.Dot(kier.normalized, prawo.normalized) * .45f, -.45f, .45f) : 0f;
    }

    // Płynne przejście (w log-skali częstotliwości), żeby wejście drzwiami nie „przeskakiwało”.
    internal static float Pasmo(float teraz, float cel, float dt) =>
        Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(teraz, 10f)), Mathf.Log(Mathf.Max(cel, 10f)), 1f - Mathf.Exp(-dt * 3.5f)));

    // na zewnątrz: najgłośniejszy otwarty otwór w zasięgu (drzwi otwarte albo otwór stały); żaden — cisza
    void PrzezOtwory(Vector3 p, out float cel, out float celPasmo, ref float pan)
    {
        float c = 0f, cp = pasmoZewn; Vector3 kier = Vector3.zero;
        void Otwor(Vector3 o)
        {
            float d = Vector3.Distance(p, o), t = Mathf.Clamp01(1f - d / zasiegOtworu), v = glosnosc * przezOtwor * t * t;
            if (v > c) { c = v; cp = Mathf.Lerp(pasmoZewn, 9000f, t * t); kier = o - p; }
        }
        foreach (var d in drzwi) if (d && d.IsOpen) Otwor(d.transform.position);
        if (otwory != null) foreach (var o in otwory) Otwor(o);
        cel = c; celPasmo = cp;
        var kam = Camera.main; Vector3 prawo = kam ? kam.transform.right : Vector3.right; prawo.y = 0f;
        pan = kier.sqrMagnitude > 1e-4f ? Mathf.Clamp(Vector3.Dot(kier.normalized, prawo.normalized) * .45f, -.45f, .45f) : 0f;
    }
    public int Drzwi => drzwi.Length;

    void Update()
    {
        if (!src || !utwor) return;
        if (!gracz) { gracz = Gracz(ref szukaj); if (!gracz) return; }
        Licz(gracz.position, wnetrze, glosniki, glosnosc, zasiegZewn, przezSciane, pasmoZewn, out float cel, out float celPasmo, out float pan);
        if (trybOtworow && (drzwi.Length > 0 || (otwory != null && otwory.Length > 0)) && wnetrze.SqrDistance(gracz.position) > .0025f)
            PrzezOtwory(gracz.position, out cel, out celPasmo, ref pan);
        src.volume = Mathf.MoveTowards(src.volume, cel, Time.deltaTime * szybkosc);
        src.panStereo = Mathf.MoveTowards(src.panStereo, pan, Time.deltaTime * 1.5f);
        pasmo = Pasmo(pasmo, celPasmo, Time.deltaTime); if (lp) lp.cutoffFrequency = pasmo;
        if (src.volume > .001f) { if (!src.isPlaying) src.Play(); }
        else if (src.isPlaying) src.Pause();   // pauza, nie stop: po powrocie gra dalej od tego miejsca
    }
}

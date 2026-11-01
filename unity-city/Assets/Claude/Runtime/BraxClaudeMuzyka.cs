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
    AudioSource src; AudioLowPassFilter lp; Transform gracz; float szukaj, pasmo = 22000f;

    void Start()
    {
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

    void Update()
    {
        if (!src || !utwor) return;
        if (!gracz) { gracz = Gracz(ref szukaj); if (!gracz) return; }
        Licz(gracz.position, wnetrze, glosniki, glosnosc, zasiegZewn, przezSciane, pasmoZewn, out float cel, out float celPasmo, out float pan);
        src.volume = Mathf.MoveTowards(src.volume, cel, Time.deltaTime * szybkosc);
        src.panStereo = Mathf.MoveTowards(src.panStereo, pan, Time.deltaTime * 1.5f);
        pasmo = Pasmo(pasmo, celPasmo, Time.deltaTime); if (lp) lp.cutoffFrequency = pasmo;
        if (src.volume > .001f) { if (!src.isPlaying) src.Play(); }
        else if (src.isPlaying) src.Pause();   // pauza, nie stop: po powrocie gra dalej od tego miejsca
    }
}

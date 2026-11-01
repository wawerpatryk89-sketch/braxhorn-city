using UnityEngine;

// DŹWIĘKI OTOCZENIA OSIEDLA (właściciel 1.11, 18:21: „cicho jest — naturalne dźwięki wszystkie, auta też niech mają dźwięk”).
// Trzy pętle 2D (ptaki, wiatr w liściach, daleki szum miasta), głośność z położenia gracza: ptaki głośniej w parkach,
// w budynku (nad głową strop — płyty kamery na warstwie 29) wszystko ciszej i stłumione. Pętle syntetyzowane przy buildzie
// (BraxClaudeOsiedleDzwieki) — bez cudzych nagrań, bez licencji. Silniki aut: BraxAuto (dźwięk 3D przy każdym aucie).
public sealed class BraxOtoczenie : MonoBehaviour
{
    public AudioClip ptaki, wiatr, szum;
    public Bounds[] parki = new Bounds[0];
    public float gPtaki = .22f, gWiatr = .09f, gSzum = .07f;
    AudioSource[] src; AudioLowPassFilter[] lp; Transform gracz; float szukaj, nast, wSrodku, wParku;

    void Start()
    {
        var clips = new[] { ptaki, wiatr, szum };
        src = new AudioSource[3]; lp = new AudioLowPassFilter[3];
        for (int i = 0; i < 3; i++)
        {
            if (!clips[i]) continue;
            var g = new GameObject("otoczenie " + i); g.transform.SetParent(transform, false);
            var s = g.AddComponent<AudioSource>(); s.clip = clips[i]; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.priority = 200;
            s.time = Random.value * clips[i].length * .9f; s.Play();
            src[i] = s; lp[i] = g.AddComponent<AudioLowPassFilter>(); lp[i].cutoffFrequency = 22000f;
        }
    }

    void Update()
    {
        if (src == null) return;
        if (!gracz) { gracz = BraxClaudeMuzyka.Gracz(ref szukaj); if (!gracz) return; }
        if (Time.unscaledTime >= nast)
        {
            nast = Time.unscaledTime + .25f;
            var p = gracz.position;
            bool dach = Physics.Raycast(p + Vector3.up * 1.8f, Vector3.up, 14f, 1 << 29, QueryTriggerInteraction.Ignore);
            wSrodku = dach ? 1f : 0f;
            float best = 999f; foreach (var b in parki) best = Mathf.Min(best, Mathf.Sqrt(b.SqrDistance(p)));
            wParku = Mathf.Clamp01(1f - best / 45f);
        }
        float dt = Time.deltaTime, w = Mathf.Lerp(1f, .22f, wSrodku), pasmo = Mathf.Lerp(22000f, 700f, wSrodku);
        float[] cel = { gPtaki * (.45f + .55f * wParku) * w, gWiatr * (.6f + .4f * wParku) * w, gSzum * Mathf.Lerp(1f, .5f, wSrodku) };
        for (int i = 0; i < 3; i++)
        {
            if (!src[i]) continue;
            src[i].volume = Mathf.MoveTowards(src[i].volume, cel[i], dt * .35f);
            lp[i].cutoffFrequency = BraxClaudeMuzyka.Pasmo(lp[i].cutoffFrequency, pasmo, dt);
        }
    }
}

using UnityEngine;

// Jeden głos na mówiącego (osobne źródło — nie kroki ani efekty).
// Build 45 (właściciel: mowa „zamulająca”, za cicha z daleka): rozmówca blisko i wyraźnie.
//  • rozmowa z graczem (mówi gracz albo postać bliżej niż 6 m × K): spatialBlend 0.35 — głównie „przy uchu”, kierunek
//    nadal słychać; mówi sam gracz — 2D; głośność 1.0;
//  • rozmowy NPC między sobą: pełne 3D, spadek logarytmiczny od minDistance 4 m × K (pełna głośność do ~5.2 m),
//    słychać do 24 m × K (~31 m); głośność 0.9;
//  • doppler 0 (ton nie „pływa”, gdy ktoś idzie), priorytet wyższy niż efekty.
// K = skala świata Braxa (2.33 / 1.80) — ta sama co BraxClaudeOsiedle.K w edytorze (runtime jej nie widzi).
public sealed class BraxGlosPostaci : MonoBehaviour
{
    public const float K = 2.33f / 1.80f;
    public const float MinOdl = 4f * K, MaxOdl = 24f * K, RozmowaOdl = 6f * K;
    public const float BlendRozmowa = .35f;
    static BraxGymPlayer gracz;
    AudioSource source;

    // Ustawienia przestrzenne (czysta funkcja — test regresji BraxTestNaprawOsiedla).
    public static void Ustawienia(bool mowiGracz, float odlDoGracza, out float blend, out float glosnosc)
    {
        bool rozmowa = mowiGracz || odlDoGracza < RozmowaOdl;
        blend = mowiGracz ? 0f : rozmowa ? BlendRozmowa : 1f;
        glosnosc = rozmowa ? 1f : .9f;
    }

    public void Mow(AudioClip clip)
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.minDistance = MinOdl; source.maxDistance = MaxOdl;
            source.rolloffMode = AudioRolloffMode.Logarithmic; source.priority = 96; source.dopplerLevel = 0f; source.spread = 30f;
        }
        if (!gracz) gracz = FindAnyObjectByType<BraxGymPlayer>();
        bool mowiGracz = gracz && (transform == gracz.transform || transform.IsChildOf(gracz.transform));
        float odl = gracz ? Vector3.Distance(gracz.transform.position, transform.position) : float.PositiveInfinity;
        Ustawienia(mowiGracz, odl, out float blend, out float glosnosc);
        source.Stop(); source.spatialBlend = blend; source.pitch = 1f; source.volume = glosnosc; source.clip = clip; source.Play();
    }
    void OnDisable() { if (source) source.Stop(); }
    void OnDestroy() { if (source) Destroy(source); }
}

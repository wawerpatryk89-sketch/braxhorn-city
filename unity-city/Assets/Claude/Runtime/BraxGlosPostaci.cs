using UnityEngine;

// Jeden głos na mówiącego (osobne źródło — nie kroki ani efekty).
// Build 45 (właściciel: mowa „zamulająca”; 20:40: „odpowiednia głośność jak się oddalam / przechodzę”):
//  • głośność z WŁASNEJ krzywej odległości od GRACZA (nie od kamery, która jest kilka metrów za nim): wyraźnie do 3.5 m × K,
//    płynnie ciszej do 12 m × K, dalej cisza; liczona co klatkę, gdy kwestia trwa — oddalasz się, głos cichnie bez skoku;
//  • rodzaj kwestii: ROZMOWA (POGADAJ, 1.0, spatialBlend 0.35 — „przy uchu”), MIJANIE (krótkie „dzień dobry”, 0.7, 0.6),
//    TŁO (NPC między sobą, 0.45, pełne 3D), OKRZYK (0.9, 3D); mówi sam gracz — 2D, 1.0;
//  • Unity bez własnego spadku (krzywa płaska do maxDistance) — jedno tłumienie, nie dwa; doppler 0.
// K = skala świata Braxa (2.33 / 1.80) — ta sama co BraxClaudeOsiedle.K w edytorze (runtime jej nie widzi).
public sealed class BraxGlosPostaci : MonoBehaviour
{
    public enum Rodzaj { Rozmowa, Mijanie, Tlo, Okrzyk }
    public const float K = 2.33f / 1.80f;
    public const float Wyraznie = 3.5f * K, Cisza = 12f * K, MinOdl = 4f * K, MaxOdl = Cisza, RozmowaOdl = 6f * K;
    public const float BlendRozmowa = .35f;
    static BraxGymPlayer gracz;
    AudioSource source;
    float baza = 1f; bool graczMowi;

    // Krzywa głośności od odległości od gracza: 1 do 3.5 m × K, gładko (smoothstep) do 0 na 12 m × K.
    public static float Krzywa(float d)
    {
        if (d <= Wyraznie) return 1f; if (d >= Cisza) return 0f;
        float t = (d - Wyraznie) / (Cisza - Wyraznie); return 1f - t * t * (3f - 2f * t);
    }
    public static void Ustawienia(Rodzaj r, bool mowiGracz, out float blend, out float glosnosc)
    {
        if (mowiGracz) { blend = 0f; glosnosc = 1f; return; }
        switch (r)
        {
            case Rodzaj.Rozmowa: blend = BlendRozmowa; glosnosc = 1f; break;
            case Rodzaj.Mijanie: blend = .6f; glosnosc = .7f; break;
            case Rodzaj.Tlo: blend = 1f; glosnosc = .45f; break;
            default: blend = 1f; glosnosc = .9f; break;
        }
    }
    // zgodność (test 45): rozmowa blisko gracza / tło
    public static void Ustawienia(bool mowiGracz, float odlDoGracza, out float blend, out float glosnosc)
        => Ustawienia(odlDoGracza < RozmowaOdl ? Rodzaj.Rozmowa : Rodzaj.Tlo, mowiGracz, out blend, out glosnosc);

    public void Mow(AudioClip clip) => Mow(clip, Rodzaj.Rozmowa);
    public void Mow(AudioClip clip, Rodzaj rodzaj)
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.minDistance = MinOdl; source.maxDistance = MaxOdl + 2f; source.priority = 96; source.dopplerLevel = 0f; source.spread = 30f;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.Constant(0f, 1f, 1f));   // spadek liczy Krzywa()
        }
        if (!gracz) gracz = FindAnyObjectByType<BraxGymPlayer>();
        graczMowi = gracz && (transform == gracz.transform || transform.IsChildOf(gracz.transform));
        Ustawienia(rodzaj, graczMowi, out float blend, out baza);
        source.Stop(); source.spatialBlend = blend; source.pitch = 1f; source.clip = clip;
        source.volume = baza * Glosnosc(); source.Play();
    }
    float Glosnosc()
    {
        if (graczMowi) return 1f;
        Transform t = gracz ? gracz.transform : (Camera.main ? Camera.main.transform : null);
        return t ? Krzywa(Vector3.Distance(t.position, transform.position)) : 1f;
    }
    void Update() { if (source && source.isPlaying) source.volume = baza * Glosnosc(); }
    void OnDisable() { if (source) source.Stop(); }
    void OnDestroy() { if (source) Destroy(source); }
}

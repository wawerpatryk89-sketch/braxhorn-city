using UnityEngine;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
// Stałe źródło 3D w miejscu (np. fontanna): gra tylko, gdy słuchacz jest w zasięgu (+5 m), poza nim pauza.
public sealed class BraxZrodloPetli : MonoBehaviour
{
    public string rodzaj = "woda"; public float glosnosc = .5f, zasieg = 30f, minOdl = 3f;
    AudioSource s; float nast;
    // Build 45 (właściciel 20:51: fontanna za głośna z bliska): spadek logarytmiczny od minOdl, ale łagodnie do ZERA na zasięgu
    // (krzywa własna) — pauza poza zasięgiem nie daje skoku głośności.
    public static float Spadek(float x, float m) { if (x <= m) return 1f; float t = Mathf.Clamp01((x - m) / Mathf.Max(1e-3f, 1f - m)); return (m / x) * (1f - t * t); }
    void Start()
    {
        s = gameObject.AddComponent<AudioSource>(); s.clip = BraxDzwiekiSwiata.Woda(); s.loop = true; s.spatialBlend = 1f;
        s.minDistance = minOdl; s.maxDistance = zasieg; s.volume = glosnosc; s.priority = 140; s.dopplerLevel = 0f;
        float m = Mathf.Clamp(minOdl / Mathf.Max(minOdl + .1f, zasieg), .01f, .9f);
        var k = new Keyframe[16];
        for (int i = 0; i < k.Length; i++) { float x = i / (float)(k.Length - 1); k[i] = new Keyframe(x, Spadek(Mathf.Max(x, 1e-3f), m)); }
        var krzywa = new AnimationCurve(k); for (int i = 0; i < k.Length; i++) krzywa.SmoothTangents(i, 0f);
        s.rolloffMode = AudioRolloffMode.Custom; s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, krzywa);
    }
    void Update()
    {
        if (Time.time < nast || !s) return; nast = Time.time + .5f;
        var kam = Camera.main; if (!kam) return;
        bool blisko = (kam.transform.position - transform.position).sqrMagnitude < (zasieg + 2f) * (zasieg + 2f);
        if (blisko && !s.isPlaying) { s.time = Random.value * s.clip.length * .9f; s.Play(); } else if (!blisko && s.isPlaying) s.Pause();
    }
}

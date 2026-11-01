using UnityEngine;

// Osobny plik (1.11, 22:20): Unity zapisuje skrypt w scenie tylko, gdy nazwa pliku = nazwa klasy; inaczej w APK „brakujący skrypt” → awaria przy wczytywaniu sceny (osiedle 13, miasto od 9).
// Stałe źródło 3D w miejscu (np. fontanna): gra tylko, gdy słuchacz jest w zasięgu (+5 m), poza nim pauza.
public sealed class BraxZrodloPetli : MonoBehaviour
{
    public string rodzaj = "woda"; public float glosnosc = .5f, zasieg = 30f;
    AudioSource s; float nast;
    void Start()
    {
        s = gameObject.AddComponent<AudioSource>(); s.clip = BraxDzwiekiSwiata.Woda(); s.loop = true; s.spatialBlend = 1f;
        s.minDistance = 3f; s.maxDistance = zasieg; s.rolloffMode = AudioRolloffMode.Logarithmic; s.volume = glosnosc; s.priority = 140;
    }
    void Update()
    {
        if (Time.time < nast || !s) return; nast = Time.time + .5f;
        var kam = Camera.main; if (!kam) return;
        bool blisko = (kam.transform.position - transform.position).sqrMagnitude < (zasieg + 5f) * (zasieg + 5f);
        if (blisko && !s.isPlaying) { s.time = Random.value * s.clip.length * .9f; s.Play(); } else if (!blisko && s.isPlaying) s.Pause();
    }
}

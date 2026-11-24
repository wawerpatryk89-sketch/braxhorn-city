using UnityEngine;

// MIASTO R1 — balkon z DJ-em na Starym Rynku (24.11, 14:09 właściciel: „balkon […] sprzęt DJ, głośniki i muzyka leci”).
// Diody na konsolecie i głowice świateł na kolumnach pulsują w rytm utworu (tempo i pierwsze uderzenie zmierzone z pliku),
// kolor wolno przechodzi róż → fiolet (jak na nagraniu). Tylko emisja jednego materiału (kopia na czas gry) — bez świateł
// czasu rzeczywistego, tanio na telefonie. Faza z AudioSource obok (BraxClaudeMuzyka), gdy gra; inaczej z zegara gry.
public class BraxPulsSwiatla : MonoBehaviour
{
    public Material material;
    public float bpm = 138f, faza = .18f, moc = 2.6f, zasieg = 160f;
    public Color kolorA = new Color(1f, .28f, .80f), kolorB = new Color(.55f, .22f, 1f);
    static readonly int Emisja = Shader.PropertyToID("_EmissionColor");
    Material kopia; AudioSource src;

    void Start()
    {
        if (!material) { enabled = false; return; }
        kopia = new Material(material) { name = material.name + " (puls)" };
        var korzen = transform.parent ? transform.parent : transform;
        foreach (var r in korzen.GetComponentsInChildren<MeshRenderer>(true))
            if (r && r.sharedMaterial == material) r.sharedMaterial = kopia;
    }

    void Update()
    {
        if (!kopia) return;
        var kam = Camera.main;
        if (kam && (kam.transform.position - transform.position).sqrMagnitude > zasieg * zasieg) return;
        if (!src) src = GetComponent<AudioSource>();
        float czas = src && src.isPlaying ? src.time - faza : Time.time;
        float t = czas * bpm / 60f, f = t - Mathf.Floor(t);
        float puls = Mathf.Lerp(.3f, 1f, Mathf.Exp(-f * 5f));             // błysk na uderzeniu, wygaszanie do następnego
        float mix = .5f + .5f * Mathf.Sin(t * Mathf.PI / 16f);             // zmiana barwy co 16 uderzeń
        kopia.SetColor(Emisja, Color.Lerp(kolorA, kolorB, mix) * (moc * puls));
    }

    void OnDestroy() { if (kopia) Destroy(kopia); }
}

using UnityEngine;

// MIASTO R1 — lampy nocą (24.11, doba „noc i dzień”): klosze latarni Starego Rynku i lampy osiedla bydgoskiego świecą, gdy
// zapada zmrok (poziom światła doby z BraxObrazRejonu.Swiatlo), w dzień nie. Zmienia tylko emisję kilku materiałów
// (raz na pół sekundy, tylko przy zmianie) — bez świateł czasu rzeczywistego.
public class BraxLampyNoc : MonoBehaviour
{
    public Material[] materialy = new Material[0];
    public Color barwa = new Color(1f, .86f, .62f);
    public float moc = 2.2f, odSwiatla = .75f, doSwiatla = .25f;
    static readonly int Emisja = Shader.PropertyToID("_EmissionColor");
    float nast, ostatnio = -1f;

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        float noc = Mathf.InverseLerp(odSwiatla, doSwiatla, BraxObrazRejonu.Swiatlo);   // 0 dzień → 1 noc
        if (Mathf.Abs(noc - ostatnio) < .01f) return;
        ostatnio = noc;
        foreach (var m in materialy)
        {
            if (!m) continue;
            m.EnableKeyword("_EMISSION");
            m.SetColor(Emisja, barwa * (moc * noc));
        }
    }

    void OnDisable()
    {
        foreach (var m in materialy) if (m) m.SetColor(Emisja, Color.black);
        ostatnio = -1f;
    }
}

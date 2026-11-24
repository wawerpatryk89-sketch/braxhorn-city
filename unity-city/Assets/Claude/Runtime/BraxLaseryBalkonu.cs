using UnityEngine;

// MIASTO R1 — lasery balkonu z DJ-em na Starym Rynku (24.11, 14:40 właściciel: „daj lasery jeszcze, że w nocy będą odpalać,
// ruszające się i migające”). Dwie głowice na narożnikach balustrady, każda wachlarz 7 promieni nad placem:
//  • tylko nocą — słońce sceny poniżej 6° nad horyzontem, słabe albo zgaszone (sprawdzane raz na sekundę);
//  • ruch: omiatanie na boki (lustrzanie dla obu głowic) i kiwanie w górę/w dół, co 8 uderzeń inne tempo;
//  • miganie w rytm utworu (faza z AudioSource BraxClaudeMuzyka obok, gdy gra): co 8 uderzeń inny układ — błysk na
//    uderzenie, stroboskop 1/8, lewa–prawa na zmianę, pełne światło z krótką przerwą, krótkie błyski; co 6. takt ciemno;
//  • barwa zmienia się co 8 uderzeń (zieleń, turkus, róż, czerwień, fiolet) — kopia materiału na czas gry.
// Promienie: shader Braxhorn/WnetrzaPromien (addytywny, bez zapisu głębi) — bez świateł czasu rzeczywistego.
public class BraxLaseryBalkonu : MonoBehaviour
{
    public Transform[] lasery = new Transform[0];
    public float bpm = 138f, faza = .18f, zasieg = 260f, slonceNoc = 6f;
    public float[] mapa;                                   // mapa uderzeń utworu (BraxRytm) — długi set ze zmiennym tempem
    public Color[] barwy = { new Color(.15f, 1f, .25f), new Color(.1f, .9f, 1f), new Color(1f, .15f, .7f), new Color(1f, .12f, .1f), new Color(.6f, .2f, 1f) };
    static readonly int Barwa = Shader.PropertyToID("_Tint");
    Renderer[][] promienie; Quaternion[] start; Material[] kopie; AudioSource src; Light slonce;
    bool noc, widac; float nastepne;

    void Start()
    {
        int n = lasery.Length;
        promienie = new Renderer[n][]; start = new Quaternion[n]; kopie = new Material[n];
        for (int i = 0; i < n; i++)
        {
            if (!lasery[i]) { promienie[i] = new Renderer[0]; continue; }
            start[i] = lasery[i].localRotation;
            promienie[i] = lasery[i].GetComponentsInChildren<Renderer>(true);
            foreach (var r in promienie[i])
            {
                if (!r) continue;
                if (!kopie[i] && r.sharedMaterial) kopie[i] = new Material(r.sharedMaterial) { name = r.sharedMaterial.name + " (gra)" };
                if (kopie[i]) r.sharedMaterial = kopie[i];
                r.enabled = false;
            }
        }
    }

    bool JestNoc()
    {
        if (!slonce) slonce = RenderSettings.sun;
        if (!slonce)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l && l.type == LightType.Directional) { slonce = l; break; }
        if (!slonce || !slonce.enabled || !slonce.gameObject.activeInHierarchy) return true;
        float wys = Mathf.Asin(Mathf.Clamp(-slonce.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        return wys < slonceNoc || slonce.intensity < .25f;
    }

    void Pokaz(int i, bool on)
    {
        foreach (var r in promienie[i]) if (r && r.enabled != on) r.enabled = on;
    }

    void Update()
    {
        if (promienie == null) return;
        if (Time.unscaledTime >= nastepne)
        {
            nastepne = Time.unscaledTime + 1f; noc = JestNoc();
            var kam = Camera.main;
            widac = !kam || (kam.transform.position - transform.position).sqrMagnitude < zasieg * zasieg;
        }
        if (!noc || !widac) { for (int i = 0; i < promienie.Length; i++) Pokaz(i, false); return; }
        if (!src) src = GetComponent<AudioSource>();
        bool gra = src && src.isPlaying;
        float u = gra ? BraxRytm.Uderzenie(src.time - faza, bpm, mapa) : Time.time * bpm / 60f; int ud = Mathf.FloorToInt(u); float f = u - ud;
        int takt = Mathf.FloorToInt(ud / 8f), uklad = ((takt * 7 + 3) % 5 + 5) % 5;
        bool przerwa = ((takt % 6) + 6) % 6 == 5;
        float tempo = takt % 2 == 0 ? .55f : 1.15f;
        for (int i = 0; i < lasery.Length; i++)
        {
            if (!lasery[i]) continue;
            float tt = Time.time * tempo + i * 1.9f, strona = i % 2 == 0 ? 1f : -1f;
            float yaw = 32f * Mathf.Sin(tt) * strona, pitch = -(10f + 9f * Mathf.Sin(tt * .7f + i));   // ujemny x = w górę
            lasery[i].localRotation = start[i] * Quaternion.Euler(pitch, yaw, 0f);
            bool on;
            switch (uklad)
            {
                case 0: on = f < .5f; break;                                  // błysk na uderzenie
                case 1: on = (f * 4f) % 1f < .5f; break;                       // stroboskop 1/8
                case 2: on = (ud + i) % 2 == 0; break;                         // lewa–prawa na zmianę
                case 3: on = !(ud % 8 == 7 && f > .4f); break;                 // pełne światło, krótka przerwa na końcu taktu
                default: on = f < .2f && ud % 2 == 0; break;                   // krótkie błyski
            }
            Pokaz(i, on && !przerwa);
            if (kopie[i]) kopie[i].SetColor(Barwa, barwy[((takt + i * 2) % barwy.Length + barwy.Length) % barwy.Length]);
        }
    }

    void OnDestroy() { if (kopie != null) foreach (var m in kopie) if (m) Destroy(m); }
}

using UnityEngine;

// AUTOMAT DO GIER (build 45 runda 2, E34 — model kasyno_automat_nermin z grafiką BRAXHORN). Przycisk AKCJA przed automatem:
// „ZAGRAJ” → dźwignia w dół i z powrotem, przycisk wciśnięty, 3 bębny kręcą się wokół własnej osi (pozioma, przez środek
// bębna — osie wyznaczone w edytorze z obrysu) i stają po 0.8 / 1.2 / 1.6 s na dyskretnych pozycjach symboli; wynik
// losowany z wagami; wygrana: górne światło miga (emisja), żetony w kieszeni wypłat, dźwięk (synteza, bez plików).
// Pieniądze — później: zdarzenie Wygrana(automat, mnożnik). Gracze-NPC: automat z flagą npc kręci sam co 8–15 s, tylko
// gdy gracz bliżej niż 15 m. Bez gracza w pobliżu — zero kosztu.
public sealed class BraxAutomat : MonoBehaviour, IBraxInterakcja
{
    public Transform[] bebny;            // osie obrotu (pivoty w środkach bębnów, oś lokalna x = oś bębna)
    public Transform dzwignia;           // pivot dźwigni (oś lokalna x)
    public Transform przycisk;           // wciskany przycisk (ruch wzdłuż -przodu)
    public Renderer swiatlo;             // górne światło (toplight)
    public Transform kieszen;            // kieszeń wypłat (żetony)
    public Mesh zeton; public Material zetonMat;
    public Vector3 przod = Vector3.forward;
    public int symbole = 10;
    public bool npc;
    public float zasieg = 1.6f;
    public static event System.Action<BraxAutomat, int> Wygrana;   // mnożnik stawki (system pieniędzy — później)

    static readonly int[] Wagi = { 30, 22, 16, 12, 8, 6, 3, 2, 1, 1 };
    static readonly int[] Mnoznik = { 2, 3, 5, 8, 10, 15, 25, 50, 100, 250 };
    readonly System.Random rnd = new System.Random();
    float t = -1f, nastNpc; int[] cel = new int[3]; float[] start = new float[3]; Quaternion[] baza; Quaternion bazaDzw; Vector3 bazaPrz;
    float migaDo; Material swiatloMat; Color swiatloKol; AudioSource glos; AudioClip klik, dzwonek; int wynik = -1;
    const float Stop0 = .8f, StopKrok = .4f;

    void OnEnable() => BraxInterakcje.Dodaj(this);
    void OnDisable() => BraxInterakcje.Usun(this);

    void Start()
    {
        baza = new Quaternion[bebny != null ? bebny.Length : 0];
        for (int i = 0; i < baza.Length; i++) baza[i] = bebny[i] ? bebny[i].localRotation : Quaternion.identity;
        if (dzwignia) bazaDzw = dzwignia.localRotation;
        if (przycisk) bazaPrz = przycisk.localPosition;
        if (swiatlo) { swiatloMat = swiatlo.material; swiatloKol = swiatloMat.HasProperty("_EmissionColor") ? swiatloMat.GetColor("_EmissionColor") : Color.black; }
        glos = gameObject.AddComponent<AudioSource>(); glos.spatialBlend = 1f; glos.minDistance = 2f; glos.maxDistance = 14f; glos.rolloffMode = AudioRolloffMode.Linear; glos.playOnAwake = false;
        klik = Ton("automat klik", 1400f, .05f); dzwonek = Ton("automat wygrana", 880f, .9f);
        nastNpc = Time.time + 3f + (float)rnd.NextDouble() * 10f;
    }
    static AudioClip Ton(string n, float f, float dl)
    {
        int sr = 22050, len = (int)(sr * dl); var d = new float[len];
        for (int i = 0; i < len; i++) { float x = i / (float)sr; d[i] = Mathf.Sin(2f * Mathf.PI * f * x * (1f + .5f * Mathf.Floor(x * 8f) / 8f)) * Mathf.Exp(-x * (dl < .1f ? 60f : 3f)) * .35f; }
        var c = AudioClip.Create(n, len, 1, sr, false); c.SetData(d, 0); return c;
    }

    public string Kontekst(Vector3 g)
    {
        if (!BraxGodzinyOtwarcia.Otwarte("kasyno") || t >= 0f) return "";
        var d = g - transform.position; d.y = 0f;
        return d.magnitude < zasieg && Vector3.Dot(d, transform.TransformDirection(przod)) > 0f ? "ZAGRAJ" : "";
    }
    public bool Wykonaj(Vector3 g) { if (t >= 0f) return false; Zakrec(); return true; }

    public static int Losuj(System.Random r, out bool wygrana)
    {
        int suma = 0; foreach (int w in Wagi) suma += w;
        int x = r.Next(suma), s = 0;
        for (int i = 0; i < Wagi.Length; i++) { s += Wagi[i]; if (x < s) { wygrana = r.NextDouble() < .18; return i; } }
        wygrana = false; return 0;
    }

    void Zakrec()
    {
        bool wyg; int sym = Losuj(rnd, out wyg) % Mathf.Max(1, symbole);
        for (int i = 0; i < 3; i++) cel[i] = wyg ? sym : (sym + i * (1 + rnd.Next(symbole - 1))) % symbole;
        if (!wyg && cel[0] == cel[1] && cel[1] == cel[2]) cel[2] = (cel[2] + 1) % symbole;
        wynik = wyg ? sym : -1; t = 0f;
        for (int i = 0; i < 3; i++) start[i] = i < baza.Length && bebny[i] ? Kat(bebny[i].localRotation, baza[i]) : 0f;
        if (glos) glos.PlayOneShot(klik);
    }
    static float Kat(Quaternion q, Quaternion b) { (Quaternion.Inverse(b) * q).ToAngleAxis(out float a, out Vector3 os); return os.x < 0f ? -a : a; }

    void Update()
    {
        var gr = BraxLudzie.Gracz; float d2 = gr ? (gr.position - transform.position).sqrMagnitude : 1e9f;
        if (npc && t < 0f && Time.time > nastNpc && d2 < 15f * 15f) { nastNpc = Time.time + 8f + (float)rnd.NextDouble() * 7f; Zakrec(); }
        if (swiatloMat && migaDo > 0f)
        {
            bool on = Time.time < migaDo && Mathf.Repeat(Time.time * 4f, 1f) < .5f;
            swiatloMat.SetColor("_EmissionColor", on ? Color.white * 2f : swiatloKol);
            if (Time.time >= migaDo) migaDo = 0f;
        }
        if (t < 0f) return;
        t += Time.deltaTime;
        if (dzwignia) dzwignia.localRotation = bazaDzw * Quaternion.Euler(Mathf.Sin(Mathf.Clamp01(t / .5f) * Mathf.PI) * 40f, 0f, 0f);
        if (przycisk) przycisk.localPosition = bazaPrz - przod * (t < .25f ? .006f : 0f);
        bool koniec = true;
        float krok = 360f / Mathf.Max(1, symbole);
        for (int i = 0; i < 3 && i < baza.Length; i++)
        {
            if (!bebny[i]) continue;
            float tStop = Stop0 + i * StopKrok, kat;
            float katCel = cel[i] * krok + 360f * (3 + i);   // pełne obroty + pozycja symbolu (dyskretna)
            if (t < tStop) { float u = t / tStop; kat = Mathf.Lerp(start[i], katCel, 1f - (1f - u) * (1f - u) * (1f - u)); koniec = false; }   // hamowanie do symbolu, bez skoku
            else kat = katCel;
            bebny[i].localRotation = baza[i] * Quaternion.Euler(kat, 0f, 0f);
        }
        if (koniec && t > Stop0 + 2 * StopKrok + .1f)
        {
            t = -1f;
            if (wynik >= 0)
            {
                migaDo = Time.time + 3f; if (glos) glos.PlayOneShot(dzwonek);
                int m = Mnoznik[Mathf.Clamp(wynik, 0, Mnoznik.Length - 1)];
                Zetony(Mathf.Clamp(m / 2, 3, 12));
                Wygrana?.Invoke(this, m);
            }
        }
    }

    void Zetony(int n)
    {
        if (!kieszen || !zeton) return;
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject("żeton"); g.transform.SetParent(kieszen, false);
            g.transform.localPosition = new Vector3(((i * 37) % 9 - 4) * .006f, i * .004f, ((i * 53) % 7 - 3) * .006f);
            g.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
            g.AddComponent<MeshFilter>().sharedMesh = zeton;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = zetonMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(g, 20f);
        }
    }
}

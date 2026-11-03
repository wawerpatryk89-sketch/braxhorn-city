using UnityEngine;

// SCHODY RUCHOME W GÓRĘ (salon samochodowy, runda 5; właściciel 01:21: „schody eleganckie i RUCHOME”). Wszystko w kodzie:
//  • stopnie (osobne obiekty z żółtymi krawędziami) jadą w pętli po ścieżce: płaski wjazd → nachylenie 35° → płaski zjazd;
//    znikają pod grzebieniem u góry i wyjeżdżają spod grzebienia na dole (jak w prawdziwych schodach);
//  • poręcze: przewijana tekstura (UV) z prędkością stopni;
//  • pasażer NPC: jedzie na stopniu, schodzi 1.6 m na antresolę, wraca na dół, gdy nie jest widoczny (bez teleportu w kadrze);
//  • gracz: w strefie stopni przesuwany po ścieżce przez CharacterController.Move (pochylnia z kolizją pod stopniami);
//  • dźwięk napędu: buczenie silnika 50/100 Hz + szum łańcucha, pętla, przestrzenny (w środku słychać, za szybą ciszej).
public sealed class BraxSchodyRuchome : MonoBehaviour
{
    public Transform[] stopnie;
    public Vector3[] sciezka;                 // lokalnie: przednia krawędź bieżni stopnia, od grzebienia dolnego do górnego
    public float predkosc = .65f;             // m/s (0.5 m/s × K)
    public Material[] porecze;
    public float uvNaMetr = 1f;
    public Transform pasazer;
    public float szerokosc = 1.3f;            // strefa gracza (lokalnie x ±szer/2)

    float[] odc; float L, faza, sPas = -1f, czekaj; AudioSource zrodlo; BraxGymPlayer gracz; Renderer[] pasR;

    void Start()
    {
        if (sciezka == null || sciezka.Length < 2) { enabled = false; return; }
        odc = new float[sciezka.Length]; L = 0f;
        for (int i = 1; i < sciezka.Length; i++) { L += Vector3.Distance(sciezka[i - 1], sciezka[i]); odc[i] = L; }
        gracz = FindAnyObjectByType<BraxGymPlayer>();
        if (pasazer) { pasR = pasazer.GetComponentsInChildren<Renderer>(); sPas = 0f; }
        zrodlo = gameObject.AddComponent<AudioSource>();
        zrodlo.clip = Buczenie(); zrodlo.loop = true; zrodlo.spatialBlend = 1f; zrodlo.volume = .35f; zrodlo.dopplerLevel = 0f;
        zrodlo.rolloffMode = AudioRolloffMode.Linear; zrodlo.minDistance = 2f; zrodlo.maxDistance = 22f; zrodlo.Play();
    }

    // punkt i kierunek na ścieżce dla długości s (0…L)
    Vector3 Punkt(float s, out Vector3 kier)
    {
        s = Mathf.Clamp(s, 0f, L);
        for (int i = 1; i < sciezka.Length; i++)
            if (s <= odc[i] || i == sciezka.Length - 1)
            {
                float d = Mathf.Max(1e-4f, odc[i] - odc[i - 1]); kier = (sciezka[i] - sciezka[i - 1]) / d;
                return Vector3.Lerp(sciezka[i - 1], sciezka[i], Mathf.Clamp01((s - odc[i - 1]) / d));
            }
        kier = Vector3.forward; return sciezka[0];
    }

    void Update()
    {
        float dt = Time.deltaTime; if (dt <= 0f) return;
        faza = (faza + predkosc * dt) % L;
        int n = stopnie != null ? stopnie.Length : 0;
        for (int k = 0; k < n; k++)
        {
            if (!stopnie[k]) continue;
            float s = (faza + k * L / n) % L;
            stopnie[k].localPosition = Punkt(s, out _);
        }
        if (porecze != null) foreach (var m in porecze) if (m) m.mainTextureOffset = new Vector2(0f, -(Time.time * predkosc * uvNaMetr) % 1f);
        Pasazer(dt);
        Gracz(dt);
    }

    void Pasazer(float dt)
    {
        if (!pasazer || sPas < 0f) return;
        if (czekaj > 0f)
        {   // na antresoli: czeka, aż zniknie z kadru, potem wraca na dolny wjazd
            czekaj -= dt;
            bool widac = false; if (pasR != null) foreach (var r in pasR) if (r && r.isVisible) { widac = true; break; }
            if (czekaj <= 0f && !widac) { sPas = 0f; czekaj = 0f; }
            else if (czekaj <= 0f) czekaj = 1f;
            if (sPas > 0f) return;
        }
        sPas += predkosc * dt;
        Vector3 kier; var p = Punkt(Mathf.Min(sPas, L), out kier);
        if (sPas > L) p += new Vector3(kier.x, 0f, kier.z).normalized * Mathf.Min(1.6f, sPas - L);   // zejście na antresolę
        pasazer.position = transform.TransformPoint(p + Vector3.up * .02f);
        var f = transform.TransformDirection(new Vector3(kier.x, 0f, kier.z)); if (f.sqrMagnitude > 1e-4f) pasazer.rotation = Quaternion.LookRotation(f);
        if (sPas > L + 1.6f) { czekaj = 6f; sPas = L + 1.6f + .001f; }
    }

    void Gracz(float dt)
    {
        if (!gracz || !gracz.body || !gracz.body.enabled) return;
        var lp = transform.InverseTransformPoint(gracz.transform.position);
        if (Mathf.Abs(lp.x) > szerokosc * .5f) return;
        // najbliższy punkt ścieżki w rzucie na z (ścieżka monotoniczna w z) i wysokość stopnia w tym miejscu
        float s0 = 0f, best = float.MaxValue;
        for (float s = 0f; s <= L; s += .1f) { var q = Punkt(s, out _); float d = Mathf.Abs(q.z - lp.z); if (d < best) { best = d; s0 = s; } }
        if (best > .3f) return;
        var pp = Punkt(s0, out var kier);
        if (lp.y < pp.y - .4f || lp.y > pp.y + 1.2f) return;
        gracz.body.Move(transform.TransformDirection(kier) * predkosc * dt);
    }

    // buczenie napędu: 50 Hz + 100 Hz + szum łańcucha (filtrowany), 2 s pętla bez trzasku (całkowite okresy)
    static AudioClip Buczenie()
    {
        const int sr = 22050; int n = sr * 2; var x = new float[n]; var r = new System.Random(77); float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr; lp += .05f * ((float)(r.NextDouble() * 2 - 1) - lp);
            x[i] = .45f * Mathf.Sin(2f * Mathf.PI * 50f * t) + .3f * Mathf.Sin(2f * Mathf.PI * 100f * t) + .08f * Mathf.Sin(2f * Mathf.PI * 150f * t)
                 + lp * .9f * (1f + .25f * Mathf.Sin(2f * Mathf.PI * 4f * t));
        }
        float m = 1e-4f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < n; i++) x[i] *= .6f / m;
        var c = AudioClip.Create("schody ruchome: napęd", n, 1, sr, false); c.SetData(x, 0); return c;
    }
}

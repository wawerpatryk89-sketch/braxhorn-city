using UnityEngine;

// SCHODY RUCHOME — JAZDA (pkt 58, metro): postać stojąca na biegu jedzie ze stopniami (CharacterController.Move wzdłuż biegu,
// góra albo dół), poręcz gumowa przewija się razem ze stopniami (UV _MainTex_ST w MaterialPropertyBlock, bez nowych materiałów),
// napęd słychać z bliska (proceduralny szum silnika 50/100/150 Hz i stukot łańcucha stopni, dźwięk 3D, pętla bez szwu).
// Taśmę stopni przesuwa BraxSchodyRuchome (osobny obiekt). Koszt: jedno sprawdzenie pozycji gracza na klatkę.
[DefaultExecutionOrder(50)] // po ruchu Braxa, a przed jego kamerą LateUpdate
public sealed class BraxSchodyRuchomeJazda : MonoBehaviour
{
    public Vector3 gora, dol;            // świat: oś biegu przy grzebieniu górnym i dolnym (poziom stopni)
    public float szer = 1f;              // szerokość stopnia (m)
    public bool wGore = true;
    public float predkosc = .5f;         // m/s wzdłuż biegu
    public float okresPoreczy = .5f;     // m na jedno powtórzenie tekstury poręczy
    public Renderer[] porecze = new Renderer[0];

    BraxGymPlayer gracz; float szukaj; MaterialPropertyBlock mpb; static AudioClip klip;
    static readonly int ST = Shader.PropertyToID("_MainTex_ST");

    void Start()
    {
        mpb = new MaterialPropertyBlock();
        try { Dzwiek(); } catch (System.Exception e) { Debug.LogWarning("BRAXHORN_SCHODY_RUCHOME dźwięk DO POPRAWKI " + e.Message); }
    }

    void Update()
    {
        float dt = Time.deltaTime; if (dt <= 0f) return;
        float off = Mathf.Repeat(Time.time * predkosc / Mathf.Max(.05f, okresPoreczy), 1f) * (wGore ? 1f : -1f);
        foreach (var r in porecze)
        {
            if (!r || !r.isVisible) continue;
            r.GetPropertyBlock(mpb); mpb.SetVector(ST, new Vector4(1f, 1f, off, 0f)); r.SetPropertyBlock(mpb);
        }
        if (!gracz && Time.unscaledTime > szukaj) { szukaj = Time.unscaledTime + 2f; gracz = FindFirstObjectByType<BraxGymPlayer>(); }
        if (!gracz || !gracz.body || !gracz.body.enabled) return;
        var p = gracz.transform.position;
        var os = dol - gora; var poz = new Vector2(os.x, os.z); float Lp = poz.magnitude; if (Lp < .1f) return;
        var kier = poz / Lp;
        var w = new Vector2(p.x - gora.x, p.z - gora.z);
        float u = Vector2.Dot(w, kier) / Lp;
        // Płynna jazda: lekki zapas przy obu grzebieniach zapobiega
        // skokowi ruchu, gdy kapsuła wchodzi lub wychodzi na spocznik.
        if (u < -.02f || u > 1.02f) return;
        float bok = Mathf.Abs(kier.x * w.y - kier.y * w.x);
        if (bok > szer * .48f) return;
        float uc = Mathf.Clamp01(u);
        float yS = Mathf.Lerp(gora.y, dol.y, uc);
        if (Mathf.Abs(p.y - yS) > .64f) return;
        // Na pierwszym i ostatnim 0.7 m prędkość przechodzi
        // płynnie od/do zera, bez teleportu ani szarpnięć kamery.
        float brzeg = Mathf.Min(uc, 1f - uc) * Lp;
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(brzeg / .7f));
        gracz.body.Move(os.normalized * (predkosc * blend * dt * (wGore ? -1f : 1f)));
    }

    void Dzwiek()
    {
        if (!klip)
        {
            const int hz = 22050; int n = hz * 2; var d = new float[n];
            var los = new System.Random(58); float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)hz;
                float silnik = .30f * Mathf.Sin(2f * Mathf.PI * 50f * t) + .22f * Mathf.Sin(2f * Mathf.PI * 100f * t) + .08f * Mathf.Sin(2f * Mathf.PI * 150f * t);
                lp += ((float)los.NextDouble() * 2f - 1f - lp) * .08f;                    // szum łożysk (dolnoprzepustowy)
                float faza = Mathf.Repeat(t * 8f, 1f);                                     // stukot łańcucha: 8 na s (16 w pętli — bez szwu)
                float stuk = faza < .02f ? (1f - faza / .02f) * ((float)los.NextDouble() * 2f - 1f) : 0f;
                d[i] = (silnik * .5f + lp * .9f + stuk * .35f) * .6f;
            }
            klip = AudioClip.Create("Schody ruchome: napęd", n, 1, hz, false); klip.SetData(d, 0);
        }
        var g = new GameObject("napęd schodów ruchomych (dźwięk)"); g.transform.SetParent(transform, false);
        g.transform.position = Vector3.Lerp(gora, dol, .5f) + Vector3.down * .6f;
        var a = g.AddComponent<AudioSource>();
        a.clip = klip; a.loop = true; a.playOnAwake = false; a.spatialBlend = 1f; a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1.5f; a.maxDistance = 14f; a.dopplerLevel = 0f; a.volume = .32f;
        a.time = Random.value * 1.9f; a.Play();
    }
}

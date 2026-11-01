using UnityEngine;

// NATURALNE DŹWIĘKI ŚWIATA (właściciel 1.11, 18:43: „naturalne dźwięki tak jak w normalnym świecie, wszędzie”).
// Wszystko syntetyzowane w grze (bez nagrań i licencji), raz, przy pierwszym użyciu:
//  • Uderzenie(pos, siła, twardość) — odbicie piłki, kontakt przedmiotu (BraxOdbicie na każdym ciele z fizyką),
//  • Drzwi(pos, otwarcie) — klamka/zamek + szum skrzydła (BraxCityDoor.Toggle),
//  • Krok(pos, głośność) — kroki przechodniów (te same próbki co kroki Braxa, ciszej),
//  • BraxZrodloPetli — stałe źródło 3D w miejscu (fontanna: plusk wody), gra tylko w zasięgu słuchu.
public static class BraxDzwiekiSwiata
{
    const int SR = 22050;
    static AudioClip[] uderzenia, drzwi; static AudioClip woda;

    static float[] Norm(float[] x, float s) { float m = 1e-5f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < x.Length; i++) x[i] *= s / m; return x; }
    static AudioClip Klip(string n, float[] x) { var c = AudioClip.Create(n, x.Length, 1, SR, false); c.SetData(x, 0); return c; }

    static void Przygotuj()
    {
        if (uderzenia != null) return;
        var r = new System.Random(9);
        // 0: miękkie (piłka do kosza / nożna — „bum”), 1: twarde (tenis — „pyk”), 2: ciężkie (piłka lekarska — głuchy łomot)
        uderzenia = new AudioClip[3];
        float[] f = { 150f, 900f, 60f }, zanik = { 30f, 90f, 22f };
        for (int k = 0; k < 3; k++)
        {
            int n = (int)(SR * .22f); var x = new float[n]; float sz = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                sz += (k == 1 ? .8f : .3f) * ((float)(r.NextDouble() * 2 - 1) - sz);
                x[i] = Mathf.Sin(2f * Mathf.PI * f[k] * t * (1f - t)) * Mathf.Exp(-t * zanik[k]) + sz * .4f * Mathf.Exp(-t * 120f);
            }
            uderzenia[k] = Klip("uderzenie " + k, Norm(x, .85f));
        }
        // drzwi: 0 = otwarcie (klik klamki + szum skrzydła), 1 = zamknięcie (szum + stuk)
        drzwi = new AudioClip[2];
        for (int k = 0; k < 2; k++)
        {
            int n = (int)(SR * .9f); var x = new float[n]; float sz = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; sz += .06f * ((float)(r.NextDouble() * 2 - 1) - sz);
                float klik = t < .03f ? (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-t * 300f) : 0f;
                float skrzydlo = sz * 3f * Mathf.Sin(Mathf.Clamp01((t - .05f) / .7f) * Mathf.PI);
                float stuk = k == 1 && t > .72f ? Mathf.Sin(2f * Mathf.PI * 95f * (t - .72f)) * Mathf.Exp(-(t - .72f) * 30f) * 1.4f : 0f;
                x[i] = (k == 0 ? klik : 0f) + skrzydlo * .5f + stuk;
            }
            drzwi[k] = Klip("drzwi " + k, Norm(x, .7f));
        }
        // woda fontanny: 8 s szumu (jasny + ciemny) z kroplami; koniec przenika w początek
        {
            int n = SR * 8; var x = new float[n]; float a = 0f, b = 0f;
            for (int i = 0; i < n; i++) { float w = (float)(r.NextDouble() * 2 - 1); a += .5f * (w - a); b += .05f * (w - b); x[i] = a * .35f + b * 1.4f; }
            for (int k = 0; k < 420; k++)
            {
                int s0 = r.Next(n - 600); float fr = 900f + (float)r.NextDouble() * 2400f, g = .2f + (float)r.NextDouble() * .5f;
                for (int i = 0; i < 500; i++) { float t = i / (float)SR; x[s0 + i] += Mathf.Sin(2f * Mathf.PI * fr * t * (1f + t * 6f)) * Mathf.Exp(-t * 160f) * g; }
            }
            int fade = SR / 2; for (int i = 0; i < fade; i++) { float u = i / (float)fade; x[i] = x[i] * u + x[n - fade + i] * (1f - u); }
            var y = new float[n - fade]; System.Array.Copy(x, y, n - fade);
            woda = Klip("woda fontanny", Norm(y, .7f));
        }
    }

    static void Graj(AudioClip c, Vector3 pos, float vol, float ton, float zasieg)
    {
        var kam = Camera.main; if (!c || !kam || (kam.transform.position - pos).sqrMagnitude > zasieg * zasieg) return;
        var g = new GameObject("dźwięk"); g.transform.position = pos;
        var s = g.AddComponent<AudioSource>(); s.clip = c; s.spatialBlend = 1f; s.minDistance = 1.5f; s.maxDistance = zasieg;
        s.rolloffMode = AudioRolloffMode.Logarithmic; s.volume = vol; s.pitch = ton; s.priority = 150; s.Play();
        Object.Destroy(g, c.length / Mathf.Max(.5f, ton) + .1f);
    }

    public static void Uderzenie(Vector3 pos, float sila, int rodzaj)
    {
        Przygotuj(); rodzaj = Mathf.Clamp(rodzaj, 0, 2);
        Graj(uderzenia[rodzaj], pos, Mathf.Clamp01(sila / 9f) * .8f, Random.Range(.92f, 1.08f), 25f);
    }
    public static void Drzwi(Vector3 pos, bool otwarcie) { Przygotuj(); Graj(drzwi[otwarcie ? 0 : 1], pos, .55f, Random.Range(.95f, 1.05f), 20f); }
    public static AudioClip Woda() { Przygotuj(); return woda; }
    public static void Krok(Vector3 pos, float vol) => Graj(BraxKroki.Losowy(), pos, vol, Random.Range(.95f, 1.15f), 14f);
}

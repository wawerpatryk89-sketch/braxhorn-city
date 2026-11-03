using UnityEngine;

// DŹWIĘKI STREF (runda 5): tło pomieszczeń syntetyzowane w kodzie (bez nagrań i licencji), pętle bez trzasku (całe okresy),
// 22 050 Hz mono, raz na rodzaj. Rodzaje: „salon” (spokojny podkład akordowy), „warsztat” (szum kompresora + grzechotka klucza),
// „wulkanizacja” (seria klucza pneumatycznego + syk powietrza), „sklep” (szum chłodziarek 50/100 Hz + delikatny podkład),
// „myjnia” (szum wody z lanc i kapanie). Głośność, pasmo i otwory liczy BraxClaudeMuzyka.
public static class BraxDzwiekiStref
{
    const int SR = 22050;
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> pamiec = new System.Collections.Generic.Dictionary<string, AudioClip>();
    public static AudioClip Klip(string rodzaj)
    {
        if (pamiec.TryGetValue(rodzaj, out var c) && c) return c;
        var x = Probki(rodzaj); if (x == null) return null;
        c = AudioClip.Create("strefa: " + rodzaj, x.Length, 1, SR, false); c.SetData(x, 0); pamiec[rodzaj] = c; return c;
    }
    public static float[] Probki(string rodzaj)
    {
        var r = new System.Random(rodzaj.GetHashCode() & 0x7fff); float lp = 0f, lp2 = 0f;
        float N() => (float)(r.NextDouble() * 2 - 1);
        int n; float[] x;
        switch (rodzaj)
        {
            case "salon":
            {   // 4 akordy po 4 s (C maj7, A min7, F maj7, G6), miękkie sinusy z powolnym wejściem
                n = SR * 16; x = new float[n];
                float[][] ak = { new[] { 261.6f, 329.6f, 392f, 493.9f }, new[] { 220f, 261.6f, 329.6f, 392f }, new[] { 174.6f, 220f, 261.6f, 329.6f }, new[] { 196f, 246.9f, 293.7f, 329.6f } };
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR; int a = (int)(t / 4f) % 4; float u = t % 4f, env = Mathf.Clamp01(u / .8f) * Mathf.Clamp01((4f - u) / .8f);
                    float s = 0f; foreach (var f in ak[a]) s += Mathf.Sin(2f * Mathf.PI * f * .5f * t) * .5f + Mathf.Sin(2f * Mathf.PI * f * t) * .25f;
                    x[i] = s * env * .25f;
                }
                break;
            }
            case "warsztat":
            {
                n = SR * 6; x = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR; lp += .02f * (N() - lp);
                    float komp = .5f * Mathf.Sin(2f * Mathf.PI * 25f * t) + .3f * Mathf.Sin(2f * Mathf.PI * 50f * t) + lp * 1.5f;
                    float gr = 0f; float u = t % 3f; if (u > 1.2f && u < 1.9f) { float f = (u * 14f) % 1f; gr = (f < .08f ? N() * (1f - f / .08f) : 0f) * .7f; }
                    x[i] = komp * .45f + gr;
                }
                break;
            }
            case "wulkanizacja":
            {
                n = SR * 8; x = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR; lp += .3f * (N() - lp); lp2 += .01f * (N() - lp2);
                    float u = t % 8f, seria = (u > 2f && u < 2.9f) || (u > 5.5f && u < 6.1f) ? 1f : 0f;
                    float klucz = seria * (Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 38f * t)) * .4f + lp * .5f) * (.7f + .3f * Mathf.Sin(2f * Mathf.PI * 3f * t));
                    float syk = (u > 6.2f && u < 7.0f ? 1f : 0f) * (N() - lp) * .25f;
                    x[i] = klucz + syk + lp2 * .8f;
                }
                break;
            }
            case "sklep":
            {
                n = SR * 4; x = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR; lp += .01f * (N() - lp);
                    x[i] = .3f * Mathf.Sin(2f * Mathf.PI * 50f * t) + .2f * Mathf.Sin(2f * Mathf.PI * 100f * t) + lp * 2f + .08f * Mathf.Sin(2f * Mathf.PI * 330f * t) * (.5f + .5f * Mathf.Sin(2f * Mathf.PI * .25f * t));
                }
                break;
            }
            case "myjnia":
            {
                n = SR * 5; x = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SR; lp += .45f * (N() - lp); lp2 += .05f * (N() - lp2);
                    float kap = (t * 3.3f % 1f) < .01f ? N() * .6f : 0f;
                    x[i] = (N() - lp) * .35f * (.8f + .2f * Mathf.Sin(2f * Mathf.PI * .4f * t)) + lp2 * .6f + kap;
                }
                break;
            }
            default: return null;
        }
        float m = 1e-4f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < x.Length; i++) x[i] *= .7f / m;
        // tony mają całe okresy w długości pętli (50 Hz × 4 s itd.) — pętla bez trzasku i bez wyciszania brzegów
        return x;
    }
}

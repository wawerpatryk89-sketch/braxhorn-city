using UnityEngine;

// DŹWIĘKI POJAZDÓW (runda 4, właściciel 23:04: „auto gracza ma mieć silnik… przy wsiadaniu trzask drzwi → odpalanie → jałowy,
// ton i głośność od prędkości; przy wysiadaniu zgaszenie + trzask drzwi; auta AI delikatnie”). Wszystko SYNTEZA w kodzie
// (bez nagrań, bez licencji), raz, przy pierwszym użyciu, 22 050 Hz mono:
//  • Rozrusznik(wariant) — rozrusznik 0.6–1.0 s (wycie silnika rozrusznika ~180 Hz z przysiadami na sprężaniu ~10 Hz, szum
//    przekładni) + zapłon: „złapanie” (niski ton 25 → 45 Hz z harmonicznymi, wyskok obrotów i opadnięcie do jałowego 32 Hz);
//  • Zgaszenie — ton silnika opada 32 → 7 Hz i cichnie (≈ 1 s), na końcu krótkie wstrząśnięcie;
//  • DrzwiOtw — klik zamka i cichy metaliczny dźwięk; DrzwiTrzask — głuche uderzenie karoserii (70/140 Hz) + zamek + drgania.
// Czyste funkcje float[] (test i log w buildzie: BRAXHORN_DZWIEKI_POJAZDOW), klipy AudioClip z nich.
public static class BraxDzwiekiAuta
{
    public const int SR = 22050;
    static AudioClip[] rozruszniki; static AudioClip zgaszenie, drzwiOtw, drzwiTrzask;

    static float[] Norm(float[] x, float s) { float m = 1e-5f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < x.Length; i++) x[i] *= s / m; return x; }
    static AudioClip Klip(string n, float[] x) { var c = AudioClip.Create(n, x.Length, 1, SR, false); c.SetData(x, 0); return c; }
    // ton silnika (te same proporcje harmonicznych co pętla silnika z buildu) dla częstotliwości zapłonu f
    static float Ton(ref float faza, float f)
    {
        faza += 2f * Mathf.PI * f / SR;
        return Mathf.Sin(faza) + .55f * Mathf.Sin(2f * faza + 1.3f) + .32f * Mathf.Sin(3f * faza + 2.6f) + .2f * Mathf.Sin(4f * faza + 3.9f) + .12f * Mathf.Sin(6f * faza + 5.2f);
    }

    // czas kręcenia rozrusznikiem [s] dla wariantu (0.6–1.0 s)
    public static float CzasRozrusznika(int wariant) => .6f + .1f * (wariant % 5);
    public static float[] Rozrusznik(int wariant)
    {
        var r = new System.Random(101 + wariant);
        float tKr = CzasRozrusznika(wariant), tZap = .75f;
        int n = (int)((tKr + tZap) * SR); var x = new float[n];
        float fazaR = 0f, fazaS = 0f, sz = 0f, fSpr = 9.5f + .8f * (wariant % 3);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR; sz += .25f * ((float)(r.NextDouble() * 2 - 1) - sz);
            // rozrusznik: wycie z przysiadami na sprężaniu (ton i głośność spadają co cylinder)
            float spr = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * fSpr * t)), 4f);
            float fR = 175f * (1f - .14f * spr) * (1f + .06f * Mathf.Clamp01(t / tKr));
            fazaR += 2f * Mathf.PI * fR / SR;
            float wycie = (Mathf.Sin(fazaR) + .45f * Mathf.Sin(2f * fazaR) + .25f * Mathf.Sin(3.02f * fazaR)) * (.55f + .45f * (1f - spr));
            float envR = Mathf.Clamp01(t / .02f) * (t < tKr ? 1f : Mathf.Exp(-(t - tKr) * 40f));
            float rozr = (wycie * .55f + sz * .35f) * envR;
            // zapłon: „złapanie” — wyskok obrotów 25 → 45 Hz i powrót do jałowego 32 Hz
            float zap = 0f;
            if (t >= tKr - .04f)
            {
                float u = t - (tKr - .04f);
                float f = u < .18f ? Mathf.Lerp(25f, 45f, u / .18f) : Mathf.Lerp(45f, 32f, Mathf.Clamp01((u - .18f) / .4f));
                float env = Mathf.Clamp01(u / .05f) * (u < .25f ? 1.15f : Mathf.Lerp(1.15f, .8f, Mathf.Clamp01((u - .25f) / .4f)));
                float wybuch = u < .06f ? (float)(r.NextDouble() * 2 - 1) * Mathf.Exp(-u * 60f) * .8f : 0f;
                zap = Ton(ref fazaS, f) * env * .5f + sz * .25f * env + wybuch;
            }
            x[i] = rozr + zap;
        }
        int wyg = (int)(.3f * SR); for (int i = 0; i < wyg; i++) x[n - wyg + i] *= 1f - i / (float)wyg;   // wygaszenie końca — przejmuje pętla jałowego
        return Norm(x, .85f);
    }
    public static float[] Zgaszenie()
    {
        var r = new System.Random(211); int n = (int)(1.1f * SR); var x = new float[n]; float faza = 0f, sz = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR; sz += .08f * ((float)(r.NextDouble() * 2 - 1) - sz);
            float f = 7f + 25f * Mathf.Exp(-t * 3.2f), env = Mathf.Exp(-t * 2.6f) * Mathf.Clamp01((1.05f - t) / .1f);
            float wstrzas = t > .82f ? Mathf.Sin(2f * Mathf.PI * 58f * (t - .82f)) * Mathf.Exp(-(t - .82f) * 22f) * .55f : 0f;
            x[i] = (Ton(ref faza, f) * .5f + sz * 1.6f) * env + wstrzas;
        }
        return Norm(x, .7f);
    }
    public static float[] DrzwiOtw()
    {
        var r = new System.Random(307); int n = (int)(.28f * SR); var x = new float[n]; float hp = 0f, prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, w = (float)(r.NextDouble() * 2 - 1); hp = .9f * (hp + w - prev); prev = w;   // szum górnoprzepustowy (klik)
            float klik = t < .012f ? hp * Mathf.Exp(-t * 250f) : 0f, klik2 = t > .05f && t < .065f ? hp * Mathf.Exp(-(t - .05f) * 300f) * .6f : 0f;
            float ping = Mathf.Sin(2f * Mathf.PI * 2150f * t) * Mathf.Exp(-t * 45f) * .25f;
            x[i] = klik + klik2 + ping;
        }
        return Norm(x, .55f);
    }
    public static float[] DrzwiTrzask()
    {
        var r = new System.Random(409); int n = (int)(.45f * SR); var x = new float[n]; float lp = 0f, hp = 0f, prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, w = (float)(r.NextDouble() * 2 - 1); lp += .12f * (w - lp); hp = .9f * (hp + w - prev); prev = w;
            float uderz = (Mathf.Sin(2f * Mathf.PI * 72f * t) + .5f * Mathf.Sin(2f * Mathf.PI * 141f * t + .7f)) * Mathf.Exp(-t * 18f);
            float zamek = t > .004f && t < .02f ? hp * Mathf.Exp(-(t - .004f) * 260f) * .8f : 0f;
            float drgania = lp * 1.4f * Mathf.Exp(-t * 14f);
            x[i] = uderz + zamek + drgania;
        }
        return Norm(x, .9f);
    }

    static void Przygotuj()
    {
        if (rozruszniki != null) return;
        rozruszniki = new AudioClip[5];
        for (int k = 0; k < 5; k++) rozruszniki[k] = Klip("auto: rozrusznik + zapłon " + k, Rozrusznik(k));
        zgaszenie = Klip("auto: zgaszenie", Zgaszenie()); drzwiOtw = Klip("auto: drzwi otwarcie", DrzwiOtw()); drzwiTrzask = Klip("auto: drzwi trzask", DrzwiTrzask());
    }
    public static AudioClip RozrusznikKlip(int wariant) { Przygotuj(); return rozruszniki[Mathf.Abs(wariant) % rozruszniki.Length]; }
    public static AudioClip ZgaszenieKlip { get { Przygotuj(); return zgaszenie; } }
    public static AudioClip DrzwiOtwKlip { get { Przygotuj(); return drzwiOtw; } }
    public static AudioClip DrzwiTrzaskKlip { get { Przygotuj(); return drzwiTrzask; } }

    // krzywa zaniku z odległością (x = odległość / maxDistance): pełna do ~8 %, łagodnie do zera na krawędzi — bez urwania
    public static AnimationCurve KrzywaZaniku()
        => new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.08f, 1f), new Keyframe(.25f, .38f), new Keyframe(.5f, .13f), new Keyframe(.8f, .03f), new Keyframe(1f, 0f));
}

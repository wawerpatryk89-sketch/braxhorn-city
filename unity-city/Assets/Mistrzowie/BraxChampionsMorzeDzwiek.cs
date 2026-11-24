using System;

// Owner 24.11 01:31 ("Wykonałeś mi te dźwięki z plaży?"): the beach's sounds, made in the game (no recordings, no
// licences - as the V8 and the estate's sounds), pure C# with no Unity calls, so a worker thread makes them while the
// scene runs (BraxChampionsSzumMorza turns them into clips):
//  - Szum: the surf zone's roar, 14 s seamless loop (two wave periods): pink noise 120 Hz - 1.2 kHz (a phone's speaker
//    plays nothing under ~200 Hz: no brown-noise rumble alone), a little low weight under it, the foam's hiss round
//    1.5 kHz swelling with each crest breaking out there;
//  - Fala: one wave reaching the shore, 6.5 s, the water arriving at UDERZENIE s - the crest's hiss and rumble coming in,
//    its collapse (a band sweeping 300 -> 1200 Hz and a low thump), the rush up the sand (around 2.5 kHz), then the backwash
//    draining with the fizz of bursting bubbles (1.8-4.5 kHz, sparser and softer as it drains);
//  - Mewy: herring gulls (Larus argentatus) as heard on a beach, 4 calls: the long call - a loud, long, high squeal
//    rising then falling, then a terminal series of short clear yelps at ~4.5 a second with little change in pitch
//    (coastal herring gulls: 8 to 14 elements, Bioacoustics); two and three "kyow" yelps (a quick rise, a long fall);
//    harmonic voice ~1 kHz with a nasal band round 2.5 kHz and a slight roughness.
public static class BraxChampionsMorzeDzwiek
{
    public const int SR = 22050;
    public const float SzumSekund = 14f, FalaSekund = 6.5f, Uderzenie = .55f;

    // the state-variable filter (topology-preserving, stable for a cutoff moving every sample)
    struct Svf
    {
        float ic1, ic2;
        public void Krok(float x, float fc, float q, out float lp, out float bp, out float hp)
        {
            float g = MathF.Tan(MathF.PI * Math.Clamp(fc, 10f, SR * .45f) / SR), k = 1f / q;
            float a1 = 1f / (1f + g * (g + k)), a2 = g * a1, a3 = g * a2;
            float v3 = x - ic2, v1 = a1 * ic1 + a2 * v3, v2 = ic2 + a2 * ic1 + a3 * v3;
            ic1 = 2f * v1 - ic1; ic2 = 2f * v2 - ic2;
            lp = v2; bp = v1; hp = x - k * v1 - v2;
        }
        public float Lp(float x, float fc, float q = .707f) { Krok(x, fc, q, out float lp, out _, out _); return lp; }
        public float Bp(float x, float fc, float q) { Krok(x, fc, q, out _, out float bp, out _); return bp; }
        public float Hp(float x, float fc, float q = .707f) { Krok(x, fc, q, out _, out _, out float hp); return hp; }
    }

    static float Szumek(Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
    static float[] Norm(float[] x, float szczyt)
    {
        float m = 1e-6f; foreach (var v in x) m = Math.Max(m, Math.Abs(v));
        for (int i = 0; i < x.Length; i++) x[i] *= szczyt / m;
        return x;
    }
    static float Gauss(float t, float c, float w) { float u = (t - c) / w; return MathF.Exp(-u * u); }

    /// <summary>The surf zone's roar: a seamless loop of SzumSekund (mono, SR).</summary>
    public static float[] Szum(int ziarno = 11)
    {
        var r = new Random(ziarno);
        int L = (int)(SR * SzumSekund), F = SR, n = L + F;
        var x = new float[n];
        Svf lo = default, lo2 = default, lo3 = default, hp = default, hi = default, hi2 = default;
        float brown = 0f, b0 = 0f, b1 = 0f, b2 = 0f;
        // the crests breaking out in the surf zone: swells of the hiss and the roar (wrapped round the loop)
        float[] c = { 1.1f, 3.6f, 5.2f, 8.1f, 10.4f, 12.6f }, w = { 1.2f, 1.6f, .9f, 1.4f, 1.1f, 1.7f }, a = { .9f, .6f, .45f, .85f, .5f, .7f };
        for (int i = 0; i < n; i++)
        {
            float t = (i % L) / (float)SR, s = 0f;
            for (int k = 0; k < c.Length; k++) s += a[k] * (Gauss(t, c[k], w[k]) + Gauss(t, c[k] + SzumSekund, w[k]) + Gauss(t, c[k] - SzumSekund, w[k]));
            float wn = Szumek(r);
            brown = brown * .996f + wn * .06f;
            b0 = .99765f * b0 + wn * .0990460f; b1 = .96300f * b1 + wn * .2965164f; b2 = .57000f * b2 + wn * 1.0526913f;
            float pink = (b0 + b1 + b2 + wn * .1848f) * .2f;
            float roar = hp.Hp(lo2.Lp(lo.Lp(pink, 1200f), 1200f), 120f) * (.65f + .8f * s);
            float weight = lo3.Lp(brown, 200f) * .15f;
            float foam = hi2.Lp(hi.Bp(wn, 1500f, .5f), 5000f) * (.10f + .65f * s);
            x[i] = roar + weight + foam;
        }
        // seamless: the extra second faded into the start
        var y = new float[L];
        for (int i = 0; i < L; i++)
        {
            if (i >= F) { y[i] = x[i]; continue; }
            float u = i / (float)F;
            y[i] = x[i] * MathF.Sqrt(u) + x[L + i] * MathF.Sqrt(1f - u);
        }
        return Norm(y, .62f);
    }

    /// <summary>One wave reaching the shore: FalaSekund long, the water arriving at Uderzenie s.</summary>
    public static float[] Fala(int ziarno = 21)
    {
        var r = new Random(ziarno);
        int n = (int)(SR * FalaSekund);
        var x = new float[n];
        Svf f1 = default, f2 = default, f3 = default, f4 = default, f5 = default, f6 = default, f7 = default;
        float brown = 0f, babel = 0f, babelF = 3000f;
        float t0 = Uderzenie;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, wn = Szumek(r);
            brown = brown * .995f + wn * .07f;
            // a) the wave coming in: a low rumble swelling to the impact, then dying away
            float a1 = t < t0 ? MathF.Pow(t / t0, 2f) : MathF.Exp(-(t - t0) / .5f);
            float rumble = f1.Lp(brown, 250f) * a1 * .5f + f7.Bp(wn, 800f, .6f) * MathF.Pow(Math.Min(t / t0, 1f), 3f) * (t < t0 ? 1f : a1) * 1.2f;
            // b) its collapse: a band sweeping up, sharp attack, and a soft thump
            float a2 = t < t0 - .05f ? 0f : (t < t0 ? (t - t0 + .05f) / .05f : MathF.Exp(-(t - t0) / .6f));
            float sweep = 300f + 900f * Math.Clamp((t - t0) / .5f, 0f, 1f);
            float crash = f2.Bp(wn, sweep, .5f) * a2 * 2.6f;
            float thump = t > t0 - .05f ? MathF.Sin(2f * MathF.PI * 55f * (t - t0 + .05f)) * MathF.Exp(-(t - t0 + .05f) / .15f) * .25f : 0f;
            // c) the rush up the sand
            float a3 = t < t0 + .05f ? 0f : (t < t0 + .45f ? (t - t0 - .05f) / .4f : MathF.Exp(-(t - t0 - .45f) / .9f));
            float rush = f3.Bp(wn, 1800f, .5f) * a3 * .9f;
            // d) the backwash: hiss and bursting bubbles over 2 kHz, the band sinking as the water drains, sparser, softer
            float tb = t - (t0 + .8f);
            float a4 = tb < 0f ? 0f : (1f - MathF.Exp(-tb / .3f)) * MathF.Exp(-tb / 2.2f);
            float hiss = f4.Hp(f5.Lp(wn, 6000f), 2200f - 900f * Math.Clamp(tb / 4f, 0f, 1f)) * a4 * .22f;
            float gestosc = .012f * MathF.Exp(-Math.Max(tb, 0f) / 1.8f);
            if (tb > 0f && r.NextDouble() < gestosc) { babel = .6f + .4f * (float)r.NextDouble(); babelF = 1800f + 2700f * (float)r.NextDouble(); }
            babel *= .985f;
            float bubbles = f6.Bp(wn * babel, babelF, 3f) * a4 * .8f;
            x[i] = rumble + crash + thump + rush + hiss + bubbles;
        }
        // a short fade at both ends
        int fa = SR / 50;
        for (int i = 0; i < fa; i++) { x[i] *= i / (float)fa; x[n - 1 - i] *= i / (float)fa; }
        return Norm(x, .9f);
    }

    // one gull note: f0(t/dur) the pitch contour, harmonics 1-6 through a nasal band, a slight roughness
    static void Nuta(float[] x, int start, float dur, float f0, Func<float, float> kontur, float glosnosc, Random r)
    {
        int n = (int)(dur * SR); float faza = 0f; Svf nos = default;
        float[] harm = { 1f, .85f, .62f, .45f, .28f, .18f };
        for (int i = 0; i < n && start + i < x.Length; i++)
        {
            float u = i / (float)n, f = f0 * kontur(u);
            faza += 2f * MathF.PI * f / SR;
            if (faza > 1e4f) faza -= 2f * MathF.PI * 1000f;
            float s = 0f;
            for (int h = 0; h < harm.Length; h++) if (f * (h + 1) < SR * .45f) s += harm[h] * MathF.Sin(faza * (h + 1));
            s *= 1f + .3f * MathF.Sin(2f * MathF.PI * 47f * i / SR) + .12f * Szumek(r);           // the hoarse edge
            s += Szumek(r) * .08f;                                                            // breath
            float v = .55f * s + .9f * nos.Bp(s, 2500f, 1.4f);
            float env = Math.Min(1f, i / (SR * .015f)) * Math.Min(1f, (n - i) / (SR * .04f));
            x[start + i] += v * env * glosnosc;
        }
    }
    static float Kyow(float u) => u < .2f ? .86f + .19f * (u / .2f) : 1.05f - .27f * ((u - .2f) / .8f);
    static float Pisk(float u) => u < .35f ? 1f + .18f * (u / .35f) : 1.18f - .38f * ((u - .35f) / .65f);

    /// <summary>The gulls' calls: 0 the long call, 1 two "kyow", 2 three short yelps, 3 one long "kee-ow".</summary>
    public static float[] Mewa(int rodzaj, int ziarno = 31)
    {
        var r = new Random(ziarno + rodzaj * 7);
        float dl = rodzaj == 0 ? 3.4f : rodzaj == 3 ? .9f : 1.3f;
        var x = new float[(int)(dl * SR)];
        float f0 = 980f + 160f * (float)r.NextDouble();
        if (rodzaj == 0)
        {
            Nuta(x, (int)(.05f * SR), .5f, f0 * 1.22f, Pisk, 1f, r);                            // the squeal: loudest, longest, highest
            int nut = 8 + r.Next(5); float t = .7f;
            for (int k = 0; k < nut; k++)
            {
                Nuta(x, (int)(t * SR), .13f, f0 * (1.02f - .006f * k) * (.98f + .04f * (float)r.NextDouble()), Kyow, .8f - .03f * k, r);
                t += .2f + .03f * (float)r.NextDouble();
            }
        }
        else if (rodzaj == 1)
        {
            Nuta(x, (int)(.03f * SR), .3f, f0, Kyow, 1f, r);
            Nuta(x, (int)(.55f * SR), .32f, f0 * .97f, Kyow, .9f, r);
        }
        else if (rodzaj == 2)
            for (int k = 0; k < 3; k++) Nuta(x, (int)((.03f + .28f * k) * SR), .15f, f0 * 1.05f, Kyow, 1f - .12f * k, r);
        else
            Nuta(x, (int)(.03f * SR), .62f, f0 * 1.1f, Pisk, 1f, r);
        return Norm(x, .8f);
    }
}

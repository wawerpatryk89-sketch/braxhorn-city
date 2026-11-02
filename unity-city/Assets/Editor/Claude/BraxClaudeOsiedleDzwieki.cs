using System.IO;
using UnityEditor;
using UnityEngine;

// DŹWIĘKI OSIEDLA (właściciel 1.11, 18:21): silniki aut i naturalne tło (ptaki, wiatr w liściach, daleki szum miasta).
// Syntetyzowane tutaj, przy buildzie, do plików WAV (mono, 22 050 Hz) — bez cudzych nagrań, więc bez licencji. Pętle bez
// przeskoku: silnik z częstotliwości będących wielokrotnością 1/okres, szumy z przenikaniem końca w początek.
// Silnik: tylko auta jeżdżące (BraxAuto.silnik); tło: BraxOtoczenie (parki z prostokątów osiedla).
public static partial class BraxClaudeOsiedle
{
    const int SR = 22050;

    static AudioClip Wav(string nazwa, float[] x, bool doPamieci)
    {
        string dir = Out + "/Dzwieki"; if (!AssetDatabase.IsValidFolder(dir)) { Directory.CreateDirectory(dir); AssetDatabase.ImportAsset(dir); }
        string path = dir + "/" + nazwa + ".wav";
        using (var f = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(f))
        {
            int n = x.Length;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + n * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n * 2);
            foreach (var v in x) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32000f), -32767, 32767));
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as AudioImporter;
        if (imp)
        {
            var ss = imp.defaultSampleSettings;
            ss.loadType = doPamieci ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            ss.compressionFormat = AudioCompressionFormat.Vorbis; ss.quality = .55f; ss.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            imp.defaultSampleSettings = ss; imp.forceToMono = true; imp.loadInBackground = true; imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    // biały szum → filtr jednobiegunowy (a = 0..1, im mniej, tym ciemniej), dwa przebiegi po okręgu = pętla bez szwu
    static float[] Szum(System.Random r, int n, float a, int przebiegi = 2)
    {
        var x = new float[n]; for (int i = 0; i < n; i++) x[i] = (float)(r.NextDouble() * 2 - 1);
        for (int p = 0; p < przebiegi; p++) { float y = x[n - 1]; for (int k = 0; k < 2; k++) for (int i = 0; i < n; i++) { y += a * (x[i] - y); if (k == 1) x[i] = y; } }
        return x;
    }
    static void Normuj(float[] x, float szczyt) { float m = 1e-6f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < x.Length; i++) x[i] *= szczyt / m; }

    static AudioClip Silnik()
    {
        // 2 s pętli; zapłon 4 cylindrów na biegu jałowym ~32 Hz (wielokrotności 0.5 Hz → pętla bez szwu), harmoniczne, puls spalania
        int n = SR * 2; var x = new float[n]; var r = new System.Random(11);
        var sz = Szum(r, n, .08f);
        float[] f = { 32f, 64f, 96f, 128f, 192f, 256f }, a = { 1f, .55f, .32f, .2f, .12f, .06f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, v = 0f;
            for (int k = 0; k < f.Length; k++) v += a[k] * Mathf.Sin(2f * Mathf.PI * f[k] * t + k * 1.3f);
            float puls = .75f + .25f * Mathf.Sin(2f * Mathf.PI * 16f * t);
            x[i] = (v * puls + sz[i] * 2.2f) * (.92f + .08f * Mathf.Sin(2f * Mathf.PI * 1.5f * t));
        }
        Normuj(x, .8f);
        return Wav("silnik_auta", x, true);
    }

    static AudioClip Ptaki()
    {
        // 24 s: frazy ćwierkania (szybkie przejścia tonu 2.4–6 kHz z wibratem), różne „gatunki”, cisza między frazami
        int n = SR * 24; var x = new float[n]; var r = new System.Random(23);
        float tt = .4f;
        while (tt < 22.5f)
        {
            int gat = r.Next(4); int ile = 2 + r.Next(gat == 2 ? 9 : 5);
            float f0 = 2400f + (float)r.NextDouble() * 2800f, glos = .35f + (float)r.NextDouble() * .65f, pan = (float)r.NextDouble();
            for (int c = 0; c < ile; c++)
            {
                float dl = gat == 0 ? .06f : gat == 1 ? .14f : gat == 2 ? .045f : .22f, sweep = gat == 1 ? -1400f : gat == 3 ? 900f : 2200f;
                int s0 = (int)(tt * SR), sn = (int)(dl * SR); float faza = 0f;
                for (int i = 0; i < sn && s0 + i < n; i++)
                {
                    float u = i / (float)sn, fr = f0 + sweep * (gat == 3 ? Mathf.Sin(u * Mathf.PI) : u) + 180f * Mathf.Sin(2f * Mathf.PI * 32f * i / SR);
                    faza += 2f * Mathf.PI * fr / SR;
                    float env = Mathf.Sin(u * Mathf.PI); env *= env;
                    x[s0 + i] += Mathf.Sin(faza) * env * glos * (.55f + .45f * pan);
                }
                tt += dl + .03f + (float)r.NextDouble() * (gat == 2 ? .04f : .12f);
            }
            tt += .5f + (float)r.NextDouble() * 2.2f;
        }
        var tlo = Szum(r, n, .02f); for (int i = 0; i < n; i++) x[i] = x[i] * .5f + tlo[i] * .9f;   // cichy szelest pod spodem
        Normuj(x, .7f);
        return Wav("ptaki", x, false);
    }

    static AudioClip WiatrLiscie()
    {
        // 20 s: szum liści (jaśniejszy) w powolnych podmuchach (obwiednia z wolnych sinusów, okresy dzielą 20 s)
        int n = SR * 20; var r = new System.Random(37);
        var a = Szum(r, n, .35f, 1); var b = Szum(r, n, .05f);
        var x = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float podmuch = .35f + .3f * Mathf.Sin(2f * Mathf.PI * t / 10f) + .2f * Mathf.Sin(2f * Mathf.PI * t / 4f + 1f) + .15f * Mathf.Sin(2f * Mathf.PI * t / 2.5f + 2f);
            x[i] = (a[i] * .35f + b[i] * 1.6f) * Mathf.Max(.08f, podmuch);
        }
        Normuj(x, .6f);
        return Wav("wiatr_liscie", x, false);
    }

    static AudioClip SzumMiasta()
    {
        // 12 s: daleki ruch uliczny (bardzo ciemny szum + wolne przejazdy), cicho pod resztą
        int n = SR * 12; var r = new System.Random(41);
        var x = Szum(r, n, .012f, 3);
        for (int i = 0; i < n; i++) { float t = i / (float)SR; x[i] *= .7f + .3f * Mathf.Sin(2f * Mathf.PI * t / 6f) * Mathf.Sin(2f * Mathf.PI * t / 4f); }
        Normuj(x, .6f);
        return Wav("szum_miasta", x, false);
    }

    static void DzwiekiOsiedla()
    {
        var silnik = Silnik();
        int auta = 0;
        foreach (var a in Object.FindObjectsByType<BraxAuto>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { a.silnik = silnik; auta++; }
        foreach (var a in Object.FindObjectsByType<BraxAutoGracza>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { a.silnik = silnik; auta++; }
        var go = new GameObject("BRAXHORN dźwięki otoczenia (ptaki, wiatr, miasto)"); go.transform.SetParent(Group("Osiedle / dźwięki"), false);
        var o = go.AddComponent<BraxOtoczenie>();
        o.ptaki = Ptaki(); o.wiatr = WiatrLiscie(); o.szum = SzumMiasta();
        o.parki = new[] { MinMax(-55f, 55f, -5f, 30f, -99f, -67f), MinMax(X0, X1, -5f, 30f, ParkZ0, FenceA) };   // Park Wiśniowy, park centralny
        var fo = new GameObject("szum fontanny"); fo.transform.SetParent(go.transform, false); fo.transform.position = new Vector3(AxisX + 6f, 1f, -132f);
        fo.AddComponent<BraxZrodloPetli>().glosnosc = .55f;
        rep.notes.Add("dźwięki: silnik w " + auta + " autach, tło (ptaki, wiatr, szum miasta) — synteza przy buildzie");
    }
}

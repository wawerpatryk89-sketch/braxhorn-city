using System.Collections.Generic;
using UnityEngine;

// LUDZIE NA OSIEDLU — wspólne sprawy wszystkich postaci (build 14). Klasa statyczna (nie komponent): bez danych w scenie.
//  • BUDŻET ANIMACJI (telefon): ludzie animowani tylko w promieniu od kamery (do 60 m); gdy w promieniu jest więcej osób
//    niż Limit, promień maleje (o 2 m na klatkę), gdy mniej — rośnie z powrotem. Dalej: poza bez animacji, ruch co 0.25 s.
//  • REAKCJE NA CIOSY (właściciel 21:59: „jak ich walnę w łeb — mają uciekać albo cokolwiek”): jedno podpięcie pod
//    BraxGymPlayer.OnCios; trafiony = najbliższa osoba przed graczem do ~1.5 m; świadkowie do 12 m.
//  • GŁOS: krótki okrzyk syntezowany w grze (formanty samogłoski na tonie krtaniowym — bez nagrań i licencji),
//    wysokość tonu osobna dla każdej postaci (pole „ton”).
public interface IBraxOsoba
{
    Vector3 Pozycja { get; }
    bool MozeReagowac { get; }
    void Trafiony(Vector3 gracz, Vector3 kierunek, bool kop);
    void Swiadek(Vector3 gracz, float odleglosc, bool ktosTrafiony);
}

public static class BraxLudzie
{
    public const float PromienMax = 60f;
    public static float Promien = PromienMax;
    public static int Limit = 52;   // 3.11: + pary rozmawiające w mieście B (ludzie mają się ruszać i rozmawiać, nie stać jak posągi)
    static int klatka = -1, wPromieniu, wPromieniuPoprz;
    static Transform kamera, gracz; static float szukaj;
    static readonly List<IBraxOsoba> osoby = new List<IBraxOsoba>();
    static bool podpiete;

    public static Transform Kamera { get { Odswiez(); return kamera; } }
    public static Transform Gracz { get { Odswiez(); return gracz; } }

    static void Odswiez()
    {
        if ((kamera && gracz) || Time.unscaledTime < szukaj) return;
        szukaj = Time.unscaledTime + 1f;
        if (!kamera && Camera.main) kamera = Camera.main.transform;
        if (!gracz) { var p = Object.FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
    }

    // E19 (osiedle e60e8e6, miasto 3.11): koszt postaci zależny od odległości od kamery.
    //  • cień tylko do 20 m × K (histereza 2 m), dalej bez cienia, • skóra 2 kości/wierzchołek dalej niż 32 m × K (4 blisko),
    //  • BEZ adaptacji limitu animowanych osób do kl./s (właściciel 3.11 10:05: ludzie mają normalnie rozmawiać —
    //    obniżanie limitu zamrażało rozmowy dalszych osób); limit stały 40 jak przed E19.
    public const float CienOdl = 20f * 1.294f, DwieKosciOdl = 32f * 1.294f;
    public const int LimitMax = 52;
    public static int Aktywni => wPromieniuPoprz;
    public static byte Detale(Renderer[] r, float d2, byte stan)
    {
        if (r == null) return stan;
        float d = Mathf.Sqrt(d2);
        bool cien = stan != 255 && (stan & 1) != 0 ? d < CienOdl + 2f : d < CienOdl;
        bool dwie = stan != 255 && (stan & 2) != 0 ? d > DwieKosciOdl - 2f : d > DwieKosciOdl;
        byte nowy = (byte)((cien ? 1 : 0) | (dwie ? 2 : 0));
        if (nowy == stan) return stan;
        foreach (var x in r)
        {
            if (!x) continue;
            x.shadowCastingMode = cien ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            if (x is SkinnedMeshRenderer smr) smr.quality = dwie ? SkinQuality.Bone2 : SkinQuality.Bone4;
        }
        return nowy;
    }

    // Czy osoba w tej odległości (kwadrat, od kamery) ma być animowana w tej klatce.
    public static bool Animuj(float d2)
    {
        if (Time.frameCount != klatka)
        {
            klatka = Time.frameCount; wPromieniuPoprz = wPromieniu; wPromieniu = 0;
            if (wPromieniuPoprz > Limit) Promien = Mathf.Max(18f, Promien - 2f);
            else if (wPromieniuPoprz < Limit - 6) Promien = Mathf.Min(PromienMax, Promien + 1f);
        }
        if (d2 > Promien * Promien) return false;
        wPromieniu++; return true;
    }

    public static float OdlKamery2(Vector3 p) { var k = Kamera; return k ? (k.position - p).sqrMagnitude : 0f; }

    public static void Dodaj(IBraxOsoba o)
    {
        if (!osoby.Contains(o)) osoby.Add(o);
        if (!podpiete) { BraxGymPlayer.OnCios += Cios; podpiete = true; }
    }
    public static void Usun(IBraxOsoba o) { osoby.Remove(o); }

    static void Cios(Vector3 pos, Vector3 fwd, bool kop)
    {
        fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) return; fwd.Normalize();
        IBraxOsoba traf = null; float best = 99f;
        for (int i = 0; i < osoby.Count; i++)
        {
            var o = osoby[i]; if (o == null || (o is Object u && !u) || !o.MozeReagowac) continue;
            var d = o.Pozycja - pos; d.y = 0f; float m = d.magnitude;
            if (m > (kop ? 1.7f : 1.5f)) continue;
            float dot = m > .01f ? Vector3.Dot(d / m, fwd) : 1f;
            if (m > .5f && dot < .35f) continue;
            float s = m - dot * .6f; if (s < best) { best = s; traf = o; }
        }
        if (traf != null) traf.Trafiony(pos, fwd, kop);
        float zasieg = traf != null ? 12f : 5f;
        for (int i = 0; i < osoby.Count; i++)
        {
            var o = osoby[i]; if (o == null || o == traf || (o is Object u && !u)) continue;
            var d = o.Pozycja - pos; d.y = 0f; float m = d.magnitude;
            if (m < zasieg) o.Swiadek(pos, m, traf != null);
        }
    }

    // ---------------- głos ----------------
    static AudioClip[] krzyki;
    // Okrzyk 0.4–0.6 s: samogłoska (a / o / e / aj) z intonacją w górę i w dół, przydech na początku; wariant 0–3.
    public static AudioClip Krzyk(int wariant)
    {
        if (krzyki == null) krzyki = new AudioClip[4];
        wariant = Mathf.Abs(wariant) % 4;
        if (krzyki[wariant]) return krzyki[wariant];
        const int sr = 22050; var r = new System.Random(31 + wariant);
        float dl = .42f + wariant * .05f; int n = (int)(sr * dl); var x = new float[n];
        float[,] F = { { 800, 1250 }, { 570, 900 }, { 600, 1850 }, { 750, 1600 } };
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr, u = t / dl;
            float f0 = 175f * (1f + .35f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.3f))) * (wariant == 3 ? 1f + .2f * u : 1f);
            ph += 2f * Mathf.PI * f0 / sr;
            float f1 = F[wariant, 0], f2 = F[wariant, 1];
            if (wariant == 3) { f1 = Mathf.Lerp(750f, 400f, u); f2 = Mathf.Lerp(1300f, 2200f, u); }   // „aj!”
            float a = 0f;
            for (int k = 1; k <= 24; k++)
            {
                float f = k * f0, g = 1f / (1f + Mathf.Pow((f - f1) / 110f, 2)) + .6f / (1f + Mathf.Pow((f - f2) / 160f, 2));
                a += g * Mathf.Sin(k * ph) / Mathf.Sqrt(k);
            }
            float env = Mathf.Min(1f, u * 14f) * Mathf.Pow(Mathf.Max(0f, 1f - u), .7f);
            float szum = ((float)r.NextDouble() * 2f - 1f) * (u < .08f ? .5f : .06f);
            x[i] = (a * .45f + szum) * env;
        }
        float m = 1e-5f; foreach (var v in x) m = Mathf.Max(m, Mathf.Abs(v)); for (int i = 0; i < n; i++) x[i] *= .85f / m;
        var c = AudioClip.Create("okrzyk " + wariant, n, 1, sr, false); c.SetData(x, 0);
        return krzyki[wariant] = c;
    }

    public static void Glos(GameObject kto, ref AudioSource zrodlo, AudioClip klip, float ton, float glosnosc, float czas = 0f)
    {
        if (!kto || !klip) return;
        var k = Kamera; if (k && (k.position - kto.transform.position).sqrMagnitude > 26f * 26f) return;
        if (!zrodlo)
        {
            zrodlo = kto.AddComponent<AudioSource>(); zrodlo.spatialBlend = 1f; zrodlo.minDistance = 2.5f; zrodlo.maxDistance = 22f;
            zrodlo.rolloffMode = AudioRolloffMode.Logarithmic; zrodlo.priority = 170; zrodlo.playOnAwake = false;
        }
        zrodlo.volume = glosnosc; zrodlo.pitch = ton * Random.Range(.96f, 1.05f); zrodlo.clip = klip; zrodlo.time = 0f;
        zrodlo.Play();
        if (czas > 0f) zrodlo.SetScheduledEndTime(AudioSettings.dspTime + czas);
    }

    // ---------------- kwestie ----------------
    static readonly System.Random los = new System.Random();
    public static string Los(string[] z) => z[los.Next(z.Length)];
    public static readonly string[] Okrzyki = { "Ała!", "Auć!", "Co ty robisz?!", "Odbiło ci?!", "Pomocy!", "Zostaw mnie!", "Hej! Spokojnie!", "Ej, co jest?!", "Wariat!", "Policja!" };
    public static readonly string[] Swiadkowie = { "Ej! Zostaw go!", "Co się dzieje?!", "Dzwonię na policję!", "Uciekajmy!", "Spokojnie, człowieku!", "O matko!", "Widzieliście to?!" };
    public static readonly string[] ZDystansu = { "Jeszcze cię dorwę!", "Zgłoszę to na policję!", "Psychol!", "Trzymaj się ode mnie z daleka!" };
    public static readonly string[] Obsluga = { "Proszę się uspokoić!", "Wzywam ochronę!", "Proszę wyjść!", "Hej! Tu nie wolno!" };
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// FAWELA NA WZGÓRZU ZA MIASTEM — ETAP 1 (9.11, właściciel 21:40–22:25: plansze „lewa i prawa strona ulicy”, „kręta droga pod górę”,
// „wąska alejka ze schodami”, „nieregularne skrzyżowanie — sklep, garaż”; „takie slamsy trzeba dobudować z delikatnym przejściem,
// żeby nie były wieżowce i nagłe slamsy”; „to nie ma być teren mały!!”; „na slamsach sala kickboxingu z siłownią — teren innego
// rywala”; „sklepy, apteki — ubogie jak na faweli, wszystko, co potrzeba; wykończenie teksturą starszy typ, żeby był klimat”;
// „na razie bez nazw”; „każdy detal ma być zrobiony, nic sztuczne”; „z Poly Haven wykończenia, tekstury wszędzie”).
// Plan: claude/braxhorn-city-MIASTO-R1-FAWELA-PROPOZYCJA-9-11.md (wersja duża).
// OBSZAR (BraxMiastoGranice.WFaweli): całe wzgórze na południe od miasta, x −470…70, od linii miasta (z −68 za parkiem wieżowców,
// z −85.45 dalej na zachód) do szczytu z −357 — ok. 540 × 290 m. Grywalne; murek granicy gry, dalszy stok, las i góry robi krok
// horyzontu (teren za granicą przedłuża stok faweli: BraxRegionHoryzont.Wys → WysTerenu).
// TEREN: wysokość rośnie z odległością od zabudowy miasta d (nie z samą współrzędną z — bez uskoku przy narożniku parku
// wieżowców): pas 1 (0–30 m) 4.5 %, pas 2 (30–90 m) 12 %, pas 3 (90 m–szczyt) 19 %, rzeźba rośnie w głąb; na wschodzie stok
// schodzi ku morzu; nad tunelem na osiedle wzgórze 10–13.6 m (zastępuje dawne „zbocze nad tunelem”). Drogi, tarasy, schody
// i działki wycinają teren pod sobą (asfalt i posadzki nie leżą w ziemi). Siatki: BraxRegionFawelaDrogi.cs.
// PRZEJŚCIE BEZ SKOKU: pas 1 — domy 3–4 kondygnacje, otynkowane i malowane; pas 2 — 2–3 kondygnacje, szary tynk cementowy, goła
// cegła w żelbetowej ramie, pustak, niedokończone piętra z prętami; (pas 3 — do szczytu: etap 2). Domy: BraxRegionFawelaDomy.cs.
// WYKOŃCZENIA: każda powierzchnia ma fotoskan CC0 z Poly Haven (lista w Materialy; brak pliku = wpis „DO POPRAWKI” w logu
// i w podsumowaniu BRAXHORN_FAWELA — nic nie zostaje po cichu gładkim kolorem). Kolor farby domów, brud przy ziemi i zacieki
// = kolor wierzchołków na fotoskanie tynku (jeden materiał na kwadrat zamiast setek).
// TELEFON: domy łączone w siatki per materiał w kwadratach 150 m (linie doczytywania BraxObszary); ściany domów zamkniętych
// jednostronne (bez niewidocznych wnętrz). Teren i drogi w scenie głównej.
public static partial class BraxRegionFawela
{
    public static readonly bool Wlaczona = true;
    internal static float K => BraxClaudeOsiedle.K;
    internal const float YC = .15f, Kw = 150f;
    public static float Hf => 2.8f * K;   // kondygnacja domu faweli
    /// <summary>Granice pasów (odległość od zabudowy miasta): pas 1 do 45 m (stok 4.5 %), pas 2 do 105 m (12 %), dalej pas 3 (19 %).</summary>
    public const float Pas1 = 45f, Pas2 = 105f;

    // ============================================================================ TEREN
    internal static float S01(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }
    internal static float Sq(float v) => v * v;
    /// <summary>Szum wartości 0…1 (własny, ten sam na każdej maszynie — plan faweli nie zależy od wersji silnika).</summary>
    internal static float Szum(float x, float y)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y); float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(H2(ix, iy), H2(ix + 1, iy), fx), Mathf.Lerp(H2(ix, iy + 1), H2(ix + 1, iy + 1), fx), fy);
    }
    static float H2(int a, int b)
    {
        unchecked { uint h = (uint)(a * 374761393 + b * 668265263); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16; return (h & 0xFFFFFF) / 16777215f; }
    }

    /// <summary>Odległość w głąb faweli od zabudowy miasta (0 na linii miasta). Miasto: x ≤ −61.4 do z −85.45, dalej na wschód do z −68.</summary>
    public static float Glebokosc(float x, float z)
    {
        const float xs = -61.4f, zB = -68f; float zA = BraxMiastoGranice.Z0;
        float dA = Mathf.Sqrt(Sq(Mathf.Max(0f, x - xs)) + Sq(Mathf.Max(0f, zA - z)));
        float dB = Mathf.Sqrt(Sq(Mathf.Max(0f, xs - x)) + Sq(Mathf.Max(0f, zB - z)));
        return Mathf.Min(dA, dB);
    }

    /// <summary>Wysokość stoku bez dróg, tarasów i działek.</summary>
    public static float WysBazowa(float x, float z)
    {
        float d = Glebokosc(x, z);
        float h = YC + .045f * Mathf.Min(d, Pas1) + .12f * Mathf.Clamp(d - Pas1, 0f, Pas2 - Pas1) + .19f * Mathf.Max(0f, d - Pas2);
        h += ((Szum(x / 95f + 1.7f, z / 95f + 4.2f) - .5f) * 10f + (Szum(x / 31f + 7.1f, z / 31f + 2.3f) - .5f) * 2.4f) * S01(40f, 160f, d);
        h = YC + (h - YC) * (1f - .85f * S01(20f, 70f, x));                         // wschód: stok schodzi ku morzu
        // wzgórze nad tunelem na osiedle: portal x −148…−104 (mur do 13.5 m) na z −85.45, rura x −132…−120 do z −146 (strop 7.6 m)
        float ax = Mathf.Abs(x + 126f), dz = BraxMiastoGranice.Z0 - z;
        if (ax < 40f && dz >= 0f)
        {
            float przyPortalu = 1f - S01(22f, 24f, ax);
            float w = (1f - S01(22f, 40f, ax)) * Mathf.Lerp(S01(0f, 18f, dz), 1f, przyPortalu) * (1f - S01(62f, 75f, dz));
            float garb = 13.65f - 3.4f * S01(0f, 60f, dz);
            h = Mathf.Max(h, Mathf.Lerp(h, garb, w));
        }
        return h;
    }

    /// <summary>Stok z drogami i tarasami (bez wcięć schodów i działek) — do niej liczone są posadzki domów i stopnie schodów.</summary>
    internal static float WysBez(float x, float z)
    {
        float h = WysBazowa(x, z);
        foreach (var t in Tarasy) { float dd = DoProst(t.r, x, z); if (dd < 7f) h = Mathf.Lerp(t.y - .3f, h, S01(.3f, 7f, dd)); }
        float rel = DoJezdni(x, z, out float yd, 10f);
        if (rel < 8f) h = Mathf.Lerp(Mathf.Lerp(yd - .25f, yd + .08f, S01(.15f, 1.8f, rel)), h, S01(3f, 8f, rel));
        return h;
    }

    /// <summary>Wysokość terenu faweli (z drogami, tarasami, schodami i działkami) — też dla terenu za granicą przy jej obrzeżu.</summary>
    public static float WysTerenu(float x, float z)
    {
        Przygotuj();
        float h = WysBez(x, z);
        float ds = DoSchodow(x, z); if (ds < 2.2f) h -= .45f * (1f - S01(1.1f, 2.2f, ds));
        if (dzKub.TryGetValue((Mathf.FloorToInt(x / 20f), Mathf.FloorToInt(z / 20f)), out var l))
            foreach (var q in l) { float m = q.lokal ? 1f : .2f; if (x > q.x0 - m && x < q.x1 + m && z > q.z0 - m && z < q.z1 + m) h = Mathf.Min(h, q.yF - .3f); }
        return h;
    }

    internal static float DoProst(Rect r, float x, float z)
    {
        float dx = Mathf.Max(0f, Mathf.Max(r.xMin - x, x - r.xMax)), dz = Mathf.Max(0f, Mathf.Max(r.yMin - z, z - r.yMax));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // ============================================================================ PLAN: DROGI, TARASY, SCHODY, DZIAŁKI (bez silnika)
    internal sealed class Droga
    {
        public string nazwa; public float szer; public bool srodek;
        public readonly List<Vector2> p = new List<Vector2>(); public readonly List<float> y = new List<float>();
        public readonly List<bool> pomin = new List<bool>();       // próbki w jezdni innej drogi (skrzyżowanie: nawierzchnia tamtej)
        public readonly Dictionary<(int, int), List<int>> kubly = new Dictionary<(int, int), List<int>>();
    }
    internal sealed class Taras { public string nazwa; public Rect r; public float y; }
    internal sealed class Dzialka
    {
        public int nr; public float x0, x1, z0, z1, yF, yFront, yMin, yMax, d; public bool polnoc, lokal; public int pas; public string rodzaj;
    }
    internal static readonly List<Droga> Drogi = new List<Droga>();
    internal static readonly List<Taras> Tarasy = new List<Taras>();
    internal static readonly List<List<Vector2>> Schody = new List<List<Vector2>>();
    static readonly List<Rect> schodyRamki = new List<Rect>();
    internal static readonly List<Dzialka> Dzialki = new List<Dzialka>();
    static readonly Dictionary<(int, int), List<Dzialka>> dzKub = new Dictionary<(int, int), List<Dzialka>>();
    public static readonly Rect Plac = Rect.MinMaxRect(-232f, -166f, -196f, -148f);
    public static readonly Rect Sala = Rect.MinMaxRect(-190f, -164f, -162f, -144f);
    public static readonly Rect PodworzeSali = Rect.MinMaxRect(-196f, -164f, -190f, -144f);
    internal static Vector2 Nawrotka; internal static float NawrotkaY; internal const float NawrotkaR = 8.5f;
    static bool gotowe, drogiGotowe;

    /// <summary>Stanowiska obsługi (sprzedawcy lokali, targ, sala) — ludzi stawia krok ludzi miasta B.</summary>
    public static readonly List<(Vector3 p, float yaw, string gdzie)> Sprzedawcy = new List<(Vector3, float, string)>();

    internal static readonly string[] RodzajeLokali =
    {
        "SKLEP SPOŻYWCZY", "APTEKA", "PIEKARNIA", "BAR", "WARZYWA · OWOCE", "FRYZJER", "SKLEP SPOŻYWCZY", "NAPRAWA TELEFONÓW", "MIĘSO · WĘDLINY",
        "BAR", "APTEKA", "WSZYSTKO DLA DOMU", "LOTERIA · OPŁATY", "SKLEP SPOŻYWCZY", "PRZYCHODNIA", "PRALNIA", "BAR", "SKLEP SPOŻYWCZY", "WULKANIZACJA"
    };

    internal static void Przygotuj()
    {
        if (gotowe) return;
        gotowe = true; drogiGotowe = false;
        Drogi.Clear(); Tarasy.Clear(); Schody.Clear(); schodyRamki.Clear(); Dzialki.Clear(); dzKub.Clear();
        // R0: ulica dolna od parkingu warsztatów (aleja manewrowa x −72, z −56) przez trawnik L9 w dół i na wschód wzdłuż pasa 1
        var r0 = NowaDroga("ul. dolna (od warsztatów)", 5.5f, true, new[] { new Vector2(-72f, -56f), new Vector2(-69.5f, -56f), new Vector2(-66.5f, -59f), new Vector2(-66.5f, -82.5f), new Vector2(-61f, -88f), new Vector2(50f, -88f) }, 1);
        // R1: serpentyna główna od ul. dolnej na szczyt
        var r1 = NowaDroga("serpentyna", 5.0f, true, new[] { new Vector2(-66.5f, -88f), new Vector2(-84f, -112f), new Vector2(-92f, -140f), new Vector2(-160f, -170f),
            new Vector2(-260f, -178f), new Vector2(-330f, -200f), new Vector2(-300f, -228f), new Vector2(-190f, -240f), new Vector2(-90f, -255f), new Vector2(0f, -262f),
            new Vector2(25f, -285f), new Vector2(-70f, -300f), new Vector2(-200f, -312f), new Vector2(-320f, -322f), new Vector2(-280f, -345f), new Vector2(-150f, -350f), new Vector2(-40f, -350f) }, 3);
        // R3: ul. dolna zachodnia (pas 1 za nowymi dzielnicami), R2: wjazd z ul. C0 (x −300) przez pas zieleni do serpentyny, R4: wjazd z ul. C1
        var r3 = NowaDroga("ul. dolna zachodnia", 4.5f, false, new[] { new Vector2(-460f, -106f), new Vector2(-160f, -106f) }, 0);
        var r2 = NowaDroga("wjazd z nowych dzielnic (ul. C0)", 4.5f, false, new[] { new Vector2(-300f, -66.75f), new Vector2(-300f, -106f), new Vector2(-297f, -150f), new Vector2(-300f, -189f) }, 2);
        var r4 = NowaDroga("wjazd z ul. C1", 4.5f, false, new[] { new Vector2(-450f, -66.75f), new Vector2(-450f, -106f) }, 0);
        Wysokosci(r0, null, .15f); Wysokosci(r1, r0, .15f); Wysokosci(r3, null, .15f); Wysokosci(r2, r3, .19f, r1); Wysokosci(r4, r3, .19f);
        Pomin(r1, r0); Pomin(r2, r3); Pomin(r2, r1); Pomin(r4, r3);
        Nawrotka = r0.p[r0.p.Count - 1] + new Vector2(6f, 0f); NawrotkaY = r0.y[r0.y.Count - 1];
        drogiGotowe = true;
        // tarasy: plac z targiem i sala rywala — na poziomie krawężnika serpentyny (najbliższy punkt drogi)
        foreach (var (nazwa, r) in new[] { ("plac z targiem", Plac), ("sala kickboxingu", Rect.MinMaxRect(PodworzeSali.xMin, Sala.yMin, Sala.xMax, Sala.yMax)) })
        {
            NaDrodze(r1, r.center.x, r.yMin, out float yr, 40f);
            Tarasy.Add(new Taras { nazwa = nazwa, r = r, y = Mathf.Round((yr + .1f) * 20f) / 20f });
        }
        // schody z miasta pod górę (etap 1: do końca pasa 2); próbka co 0.4 m = jeden stopień
        foreach (var (x, z0, z1) in new[] { (0f, -68f, -158f), (40f, -68f, -158f), (-200f, BraxMiastoGranice.Z0, Plac.yMax), (-400f, BraxMiastoGranice.Z0, -175f) })
        {
            var s = new List<Vector2>(); float xa = 1e9f, xb = -1e9f;
            for (float z = z0; z >= z1 - .01f; z -= .4f) { float xx = x + Mathf.Sin((z0 - z) / 9f) * 1.2f * S01(0f, 6f, z0 - z) * S01(0f, 6f, z - z1); s.Add(new Vector2(xx, z)); xa = Mathf.Min(xa, xx); xb = Mathf.Max(xb, xx); }
            Schody.Add(s); schodyRamki.Add(Rect.MinMaxRect(xa - 3f, z1 - 3f, xb + 3f, z0 + 3f));
        }
        Planuj();
    }

    static Droga NowaDroga(string nazwa, float szer, bool srodek, Vector2[] wezly, int wygladzenia)
    {
        var d = new Droga { nazwa = nazwa, szer = szer, srodek = srodek };
        var pkt = wezly.ToList();
        // zaokrąglenie załamań (Chaikin, końce zostają) — łuki z dużym promieniem na nawrotach
        for (int it = 0; it < Mathf.Max(1, wygladzenia) && pkt.Count > 2; it++)
        {
            var nowe = new List<Vector2> { pkt[0] };
            for (int i = 0; i + 1 < pkt.Count; i++)
            {
                var a = pkt[i]; var b = pkt[i + 1];
                if (i > 0) nowe.Add(Vector2.Lerp(a, b, .25f));
                if (i + 1 < pkt.Count - 1) nowe.Add(Vector2.Lerp(a, b, .75f));
            }
            nowe.Add(pkt[pkt.Count - 1]); pkt = nowe;
        }
        for (int i = 0; i + 1 < pkt.Count; i++)   // próbki co 1 m
        {
            float L = Vector2.Distance(pkt[i], pkt[i + 1]); int n = Mathf.Max(1, Mathf.CeilToInt(L));
            for (int k = 0; k < n; k++) d.p.Add(Vector2.Lerp(pkt[i], pkt[i + 1], k / (float)n));
        }
        d.p.Add(pkt[pkt.Count - 1]);
        for (int i = 0; i < d.p.Count; i++)
        {
            d.y.Add(0f); d.pomin.Add(false);
            var key = (Mathf.FloorToInt(d.p[i].x / 10f), Mathf.FloorToInt(d.p[i].y / 10f)); if (!d.kubly.TryGetValue(key, out var l)) d.kubly[key] = l = new List<int>(); l.Add(i);
        }
        Drogi.Add(d);
        return d;
    }

    // wysokości jezdni: teren bazowy wygładzony (±12 m), spadek ≤ 14 %; w mieście poziom miasta; przy skrzyżowaniu z drogą „a”/„b”
    // (w jej jezdni i 3 m dalej) wysokość tamtej drogi — bez schodka na styku
    static void Wysokosci(Droga d, Droga a, float poziomMiasta, Droga b = null)
    {
        int n = d.p.Count; var y = new float[n]; var stale = new bool[n];
        for (int i = 0; i < n; i++)
        {
            var q = d.p[i];
            y[i] = WysBazowa(q.x, q.y);
            if (!BraxMiastoGranice.WFaweli(q.x, q.y)) { y[i] = poziomMiasta; stale[i] = true; }
            foreach (var o in new[] { a, b })
            {
                if (o == null) continue;
                float dist = NaDrodze(o, q.x, q.y, out float yo, 12f);
                if (dist < o.szer * .5f + 3f) { y[i] = yo; stale[i] = true; }
            }
        }
        var g = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (stale[i]) { g[i] = y[i]; continue; }
            float s = 0f; int c = 0; for (int k = Mathf.Max(0, i - 12); k <= Mathf.Min(n - 1, i + 12); k++) { s += y[k]; c++; }
            g[i] = s / c;
        }
        const float spadek = .14f;
        for (int pass = 0; pass < 4; pass++)
        {
            for (int i = 1; i < n; i++) if (!stale[i]) { float ds = Vector2.Distance(d.p[i], d.p[i - 1]); g[i] = Mathf.Clamp(g[i], g[i - 1] - spadek * ds, g[i - 1] + spadek * ds); }
            for (int i = n - 2; i >= 0; i--) if (!stale[i]) { float ds = Vector2.Distance(d.p[i], d.p[i + 1]); g[i] = Mathf.Clamp(g[i], g[i + 1] - spadek * ds, g[i + 1] + spadek * ds); }
        }
        for (int i = 0; i < n; i++) d.y[i] = g[i];
    }

    static void Pomin(Droga d, Droga glowna)
    {
        for (int i = 0; i < d.p.Count; i++) if (NaDrodze(glowna, d.p[i].x, d.p[i].y, out _, 10f) < glowna.szer * .5f + .05f) d.pomin[i] = true;
    }

    /// <summary>Odległość (w poziomie) do osi drogi i wysokość jezdni w najbliższej próbce; maks = promień szukania (kubełki 10 m).</summary>
    internal static float NaDrodze(Droga d, float x, float z, out float y, float maks)
    {
        y = 0f; float best = 1e9f;
        int r = Mathf.CeilToInt(maks / 10f), cx = Mathf.FloorToInt(x / 10f), cz = Mathf.FloorToInt(z / 10f);
        for (int i = cx - r; i <= cx + r; i++)
            for (int j = cz - r; j <= cz + r; j++)
            {
                if (!d.kubly.TryGetValue((i, j), out var l)) continue;
                foreach (int k in l)
                {
                    float dx = d.p[k].x - x, dz = d.p[k].y - z, dd = dx * dx + dz * dz;
                    if (dd < best) { best = dd; y = d.y[k]; }
                }
            }
        return Mathf.Sqrt(best);
    }

    /// <summary>Odległość do krawędzi najbliższej jezdni (ujemna = na jezdni) i jej wysokość; z nawrotką ul. dolnej.</summary>
    internal static float DoJezdni(float x, float z, out float y, float maks = 14f)
    {
        float best = 1e9f; y = 0f;
        foreach (var d in Drogi) { float dist = NaDrodze(d, x, z, out float yy, maks) - d.szer * .5f; if (dist < best) { best = dist; y = yy; } }
        if (drogiGotowe) { float dn = Mathf.Sqrt(Sq(x - Nawrotka.x) + Sq(z - Nawrotka.y)) - NawrotkaR; if (dn < best) { best = dn; y = NawrotkaY; } }
        return best;
    }
    internal static float DoDrog(float x, float z, float maks = 14f) => DoJezdni(x, z, out _, maks);
    internal static float DoSchodow(float x, float z)
    {
        float best = 1e9f;
        for (int k = 0; k < Schody.Count; k++)
        {
            if (!schodyRamki[k].Contains(new Vector2(x, z))) continue;
            foreach (var q in Schody[k]) { float dd = (q.x - x) * (q.x - x) + (q.y - z) * (q.y - z); if (dd < best) best = dd; }
        }
        return Mathf.Sqrt(best);
    }
    internal static float DoDzialek(float x, float z)
    {
        float best = 1e9f; int cx = Mathf.FloorToInt(x / 20f), cz = Mathf.FloorToInt(z / 20f);
        for (int i = cx - 1; i <= cx + 1; i++)
            for (int j = cz - 1; j <= cz + 1; j++)
                if (dzKub.TryGetValue((i, j), out var l))
                    foreach (var q in l) best = Mathf.Min(best, DoProst(Rect.MinMaxRect(q.x0, q.z0, q.x1, q.z1), x, z));
        return best;
    }

    // działki: rzędy równoległe do linii miasta; wolne od dróg, schodów, tarasów, wzgórza nad tunelem i linii kwadratów 150 m
    static bool Wolne(float x0, float x1, float z0, float z1, out int pas, out float d)
    {
        pas = 0; d = 0f;
        if (Mathf.FloorToInt((x0 - 2.4f) / Kw) != Mathf.FloorToInt((x1 + 2.4f) / Kw) || Mathf.FloorToInt((z0 - 2.4f) / Kw) != Mathf.FloorToInt((z1 + 2.4f) / Kw)) return false;   // z balkonem, markizą i stolikami
        float cx = (x0 + x1) * .5f, cz = (z0 + z1) * .5f;
        foreach (var (x, z) in new[] { (x0, z0), (x1, z0), (x0, z1), (x1, z1), (cx, cz), (cx, z0), (cx, z1), (x0, cz), (x1, cz) })
        {
            if (!BraxMiastoGranice.WFaweli(x, z)) return false;
            if (x < BraxMiastoGranice.FawelaX0 + 4f || x > BraxMiastoGranice.FawelaX1 - 4f) return false;
            if (DoDrog(x, z, 12f) < 2.6f) return false;
            if (DoSchodow(x, z) < 2.4f) return false;
            foreach (var t in Tarasy) if (DoProst(t.r, x, z) < 3f) return false;
            if (Mathf.Abs(x + 126f) < 34f && BraxMiastoGranice.Z0 - z < 78f) return false;    // wzgórze nad tunelem
        }
        d = Glebokosc(cx, cz);
        bool przyPlacu = DoProst(Plac, cx, cz) < 34f || DoProst(Sala, cx, cz) < 34f;
        if (d > Pas2 && !przyPlacu && DoDrog(cx, cz, 30f) > 22f) return false;   // etap 1: pasy 1–2, domy przy placu i zabudowa wzdłuż serpentyny do szczytu
        if (Mathf.Min(Glebokosc(cx, z1), Mathf.Min(Glebokosc(x0, z1), Glebokosc(x1, z1))) < 2.5f) return false;   // nie przy samej linii miasta
        pas = d < Pas1 ? 1 : 2;
        return true;
    }

    static void Planuj()
    {
        var rnd = new System.Random(20261109);
        float R() => (float)rnd.NextDouble();
        void Dodaj(float x, float x1, float z0, float z1, int stale)
        {
            if (x1 - x <= 4.8f || !Wolne(x, x1, z0, z1, out int pas, out float d)) return;
            for (int k = 0; k < stale; k++) { var o = Dzialki[k]; if (x < o.x1 + 1f && x1 > o.x0 - 1f && z0 < o.z1 + 1.5f && z1 > o.z0 - 1.5f) return; }
            float cx = (x + x1) * .5f;
            var dz = new Dzialka { nr = Dzialki.Count, x0 = x, x1 = x1, z0 = z0, z1 = z1, pas = pas, d = d };
            // przód: do drogi (gdy jest w 4 m od którejś ściany), inaczej w dół stoku (do miasta)
            float dN = DoDrog(cx, z1 + 1f, 10f), dS = DoDrog(cx, z0 - 1f, 10f);
            dz.polnoc = !(dS < 4f && dS < dN);
            dz.yFront = dz.polnoc ? WysBez(cx, z1 + .8f) : WysBez(cx, z0 - .8f);
            dz.yMin = float.MaxValue; dz.yMax = float.MinValue;
            foreach (var (px, pz) in new[] { (x, z0), (x1, z0), (x, z1), (x1, z1), (cx, (z0 + z1) * .5f), (cx, z0), (cx, z1) }) { float h = WysBez(px, pz); dz.yMin = Mathf.Min(dz.yMin, h); dz.yMax = Mathf.Max(dz.yMax, h); }
            // posadzka: próg 10–40 cm nad terenem przed frontem, na siatce Hf / 12 (12 wspólnych materiałów okien z pokojem — FazaOkna)
            dz.yF = Mathf.Ceil((dz.yFront + .10f) / (Hf / 12f)) * (Hf / 12f);
            if (dz.yMax - dz.yMin < 3.4f) Dzialki.Add(dz);
        }
        // 1) pierzeje przy prostych ulicach (front 2.9 m od krawężnika, po obu stronach): ul. dolna i ul. dolna zachodnia
        foreach (var (xa, xb, zOs, pol) in new[] { (-58f, 48f, -88f, 2.75f), (-464f, -158f, -106f, 2.25f) })
            foreach (bool odPoludnia in new[] { true, false })
            {
                float zFront = odPoludnia ? zOs + pol + 2.9f : zOs - pol - 2.9f, x = xa + R() * 2f;
                while (x < xb - 5f)
                {
                    float x1 = Mathf.Min(x + 5.5f + R() * 4.5f, xb), gleb = 7.2f + R() * 2.6f;
                    Dodaj(x, x1, odPoludnia ? zFront : zFront - gleb, odPoludnia ? zFront + gleb : zFront, 0);
                    x = x1 + (R() < .5f ? 0f : 1.3f + R() * 1.4f);
                }
            }
        int stalych = Dzialki.Count;
        // 2) rzędy na stoku (alejki 1–3 m między rzędami, przesmyki między domami, co kilkunasta działka pusta — podwórko)
        foreach (var (xa, xb, zTop) in new[] { (BraxMiastoGranice.FawelaX0 + 4f, -63f, BraxMiastoGranice.Z0), (-59f, BraxMiastoGranice.FawelaX1 - 4f, -68f) })
        {
            for (float zf = zTop - 3.5f; zf > BraxMiastoGranice.FawelaZSzczyt + 14f; )
            {
                float gleb = 7.5f + R() * 3f;
                float x = xa + R() * 3f;
                while (x < xb - 5f)
                {
                    float x1 = Mathf.Min(x + 5.5f + R() * 4.5f, xb);
                    float z1 = zf - R() * 1.2f, z0 = zf - gleb + (R() - .5f) * 1.2f;
                    bool przesmyk = R() >= .6f, puste = R() < .06f;
                    if (!puste) Dodaj(x, x1, z0, z1, stalych);
                    x = x1 + (przesmyk ? 1.1f + R() * 1.1f : 0f);
                }
                zf -= gleb + 1.8f + R() * 1.3f;
            }
        }
        // lokale: działki frontem przy samej drodze, rozłożone po obszarze (ul. dolna, plac, wjazdy), nie obok siebie
        float Front(Dzialka q) => q.polnoc ? DoDrog((q.x0 + q.x1) * .5f, q.z1 + 1f, 10f) : DoDrog((q.x0 + q.x1) * .5f, q.z0 - 1f, 10f);
        var kand = Dzialki.Where(q => Front(q) < 3.2f && q.x1 - q.x0 > 6.4f && Mathf.Abs(q.yMax - q.yMin) < 1.6f).OrderBy(q => (q.nr * 7919) % 101).ToList();
        int li = 0; var lok = new List<Dzialka>();
        foreach (var q in kand)
        {
            if (li >= RodzajeLokali.Length) break;
            if (lok.Any(o => Mathf.Abs(o.x0 - q.x0) + Mathf.Abs(o.z0 - q.z0) < 26f)) continue;
            q.lokal = true; q.rodzaj = RodzajeLokali[li++]; lok.Add(q);
        }
        foreach (var q in Dzialki)
            for (int i = Mathf.FloorToInt((q.x0 - 1f) / 20f); i <= Mathf.FloorToInt((q.x1 + 1f) / 20f); i++)
                for (int j = Mathf.FloorToInt((q.z0 - 1f) / 20f); j <= Mathf.FloorToInt((q.z1 + 1f) / 20f); j++)
                { if (!dzKub.TryGetValue((i, j), out var l)) dzKub[(i, j)] = l = new List<Dzialka>(); l.Add(q); }
    }

    // ============================================================================ SIATKA Z KOLORAMI WIERZCHOŁKÓW
    internal sealed class Bud
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Color> c = new List<Color>(); public readonly List<int> t = new List<int>();
        public Func<Vector3, Color> kolor = q => Color.white;
        public int Count => v.Count;
        public Bud K(Color k) { kolor = q => k; return this; }
        void Dodaj(Vector3 q, Vector3 nn, Color k, Vector2 u) { k.a = 1f; v.Add(q); n.Add(nn); c.Add(k); uv.Add(u); }
        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d)
        {
            var nn = Vector3.Cross(b - a, cc - a); if (nn.sqrMagnitude < 1e-12f) nn = Vector3.Cross(cc - a, d - a);
            if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count;
            Dodaj(a, nn, kolor(a), UV(a, nn)); Dodaj(b, nn, kolor(b), UV(b, nn)); Dodaj(cc, nn, kolor(cc), UV(cc, nn)); Dodaj(d, nn, kolor(d), UV(d, nn));
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        /// <summary>Czworokąt z kolorem na wierzchołek (jezdnia: ślady kół, kurz przy krawężniku).</summary>
        public void QuadC(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color ka, Color kb, Color kc, Color kd)
        {
            var nn = Vector3.Cross(b - a, cc - a); if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count;
            Dodaj(a, nn, ka, UV(a, nn)); Dodaj(b, nn, kb, UV(b, nn)); Dodaj(cc, nn, kc, UV(cc, nn)); Dodaj(d, nn, kd, UV(d, nn));
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        /// <summary>Czworokąt z własnym UV (wycinek atlasu). Wierzchołki a, b (dół), cc, d (góra); lustro = obraz czytany od strony
        /// patrzącego (dla lica muru patrzący ma „a” po PRAWEJ ręce — wtedy lustro = true).</summary>
        public void QuadUV(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Rect u, bool lustro = false)
        {
            var nn = Vector3.Cross(b - a, cc - a); if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count; float ua = lustro ? u.xMax : u.xMin, ub = lustro ? u.xMin : u.xMax;
            Dodaj(a, nn, kolor(a), new Vector2(ua, u.yMin)); Dodaj(b, nn, kolor(b), new Vector2(ub, u.yMin));
            Dodaj(cc, nn, kolor(cc), new Vector2(ub, u.yMax)); Dodaj(d, nn, kolor(d), new Vector2(ua, u.yMax));
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        /// <summary>Czworokąt widoczny z obu stron (tkanina, pręty balustrad).</summary>
        public void Quad2(Vector3 a, Vector3 b, Vector3 cc, Vector3 d) { Quad(a, b, cc, d); Quad(d, cc, b, a); }
        static Vector2 UV(Vector3 p, Vector3 nn)
        {
            float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
            if (ax >= az) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }
        /// <summary>Prostopadłościan min–max (bez dna — leży na czymś albo jest w murze).</summary>
        public void Box(Vector3 mn, Vector3 mx, bool dno = false)
        {
            if (mx.x - mn.x < 1e-4f || mx.y - mn.y < 1e-4f || mx.z - mn.z < 1e-4f) return;
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
            float x0 = mn.x, x1 = mx.x, y0 = mn.y, y1 = mx.y, z0 = mn.z, z1 = mx.z;
            Quad(P(x0, y0, z1), P(x1, y0, z1), P(x1, y1, z1), P(x0, y1, z1));   // +z
            Quad(P(x1, y0, z0), P(x0, y0, z0), P(x0, y1, z0), P(x1, y1, z0));   // −z
            Quad(P(x0, y0, z0), P(x0, y0, z1), P(x0, y1, z1), P(x0, y1, z0));   // −x
            Quad(P(x1, y0, z1), P(x1, y0, z0), P(x1, y1, z0), P(x1, y1, z1));   // +x
            Quad(P(x0, y1, z1), P(x1, y1, z1), P(x1, y1, z0), P(x0, y1, z0));   // góra
            if (dno) Quad(P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1));
        }
        public void BoxRot(Vector3 c0, Vector3 size, Quaternion r)
        {
            var h = size * .5f;
            Vector3 P(float x, float y, float z) => c0 + r * new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)); Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
            Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1)); Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
            Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1)); Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        }
        public void Cyl(Vector3 a, Vector3 b, float r, int sides, float r2 = -1f, bool dekly = true)
        {
            if (r2 < 0) r2 = r;
            var ax = (b - a).normalized; var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(ax, u);
            for (int i = 0; i < sides; i++)
            {
                float a0 = 2 * Mathf.PI * i / sides, a1 = 2 * Mathf.PI * (i + 1) / sides;
                Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                Quad(a + d0 * r, a + d1 * r, b + d1 * r2, b + d0 * r2);
                if (dekly) { Tri(a, a + d1 * r, a + d0 * r); Tri(b, b + d0 * r2, b + d1 * r2); }
            }
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 cc)
        {
            var nn = Vector3.Cross(b - a, cc - a); if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count;
            Dodaj(a, nn, kolor(a), UV(a, nn)); Dodaj(b, nn, kolor(b), UV(b, nn)); Dodaj(cc, nn, kolor(cc), UV(cc, nn));
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }
        public GameObject Obiekt(string nazwa, Transform rodzic, Material m, bool cienie = true, bool kolizja = false)
        {
            if (v.Count == 0 || !m) return null;
            var mesh = new Mesh { name = nazwa, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false); g.transform.position = Vector3.zero;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            if (kolizja) g.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            trojkatow += t.Count / 3;
            return g;
        }
    }

    // ============================================================================ BUDOWA
    internal static Transform root;
    static readonly Dictionary<(int, int), Dictionary<Material, Bud>> komorki = new Dictionary<(int, int), Dictionary<Material, Bud>>();
    static readonly Dictionary<(int, int), Transform> rodzice = new Dictionary<(int, int), Transform>();
    static readonly HashSet<Material> bezCienia = new HashSet<Material>();
    internal static int domow, domowPas1, domowPas2, lokali, krokow, slupow, trojkatow, graffiti, aut, przylaczy, roslin;
    /// <summary>Punkty przyłączy prądu na domach (skrzynka pod dachem) — przewody od najbliższego słupa.</summary>
    internal static readonly List<Vector3> Przylacza = new List<Vector3>();

    public static void Apply(Transform cityRoot)
    {
        if (!Wlaczona) return;
        gotowe = false; Przygotuj();
        komorki.Clear(); rodzice.Clear(); bezCienia.Clear(); Sprzedawcy.Clear(); Przylacza.Clear();
        domow = domowPas1 = domowPas2 = lokali = krokow = slupow = trojkatow = graffiti = aut = przylaczy = roslin = modeli = modeliBib = luster = linRingu = 0; brakModeli.Clear();
        root = BraxRegion.Root(cityRoot, "CLAUDE fawela: wzgórze za miastem (etap 1)");
        BraxUliceKit.Start();   // litery przestrzenne szyldów (krój miasta) — partie zapisuje krok „zapis kwadratów”
        Materialy();
        Krok("teren wzgórza", Teren);
        Krok("drogi: asfalt, łaty, włazy, krawężniki, oś, murki, mury, progi, auta", DrogiBuduj);
        Krok("schody z miasta pod górę", SchodyBuduj);
        Krok("plac z targiem", PlacBuduj);
        Krok("sala kickboxingu z siłownią (rywal)", SalaBuduj);
        Krok("domy pasów 1–2 i lokale", Domy);
        Krok("zieleń dzika stoku: drzewa, krzewy, kamienie", Zielen);
        Krok("słupy, transformatory, przewody, przyłącza, latarnie", Slupy);
        Krok("zapis kwadratów", Zapisz);
        Debug.Log("BRAXHORN_FAWELA etap 1: działek " + Dzialki.Count + ", domów " + domow + " (pas 1: " + domowPas1 + ", pas 2: " + domowPas2 + "), lokali " + lokali +
                  ", stanowisk obsługi " + Sprzedawcy.Count + ", dróg " + Drogi.Count + " (" + Drogi.Sum(d => d.p.Count) + " m), stopni schodów " + krokow + ", słupów " + slupow +
                  ", przyłączy " + przylaczy + ", graffiti " + graffiti + ", aut zaparkowanych " + aut + ", luster z odbiciem " + luster + ", boków lin ringu ze splotek (BraxLinaSplot) " + linRingu + ", roślin i rekwizytów z biblioteki " + roslin + " (drzew " + drzew + ", krzewów " + krzewow + ", kamieni " + kamieni + ")" + ", modeli w lokalach " + modeli + ", modeli z biblioteki " + modeliBib + (brakModeli.Count > 0 ? " (brak: " + string.Join(", ", brakModeli) + ")" : "") + ", towaru z biblioteki " + BraxSklepyTowar.Postawione + ", trójkątów " + trojkatow + ", kwadratów 150 m " + komorki.Count +
                  ", plac y " + Tarasy[0].y.ToString("F2") + ", sala y " + Tarasy[1].y.ToString("F2") + ", fotoskany (" + opisTekstur.Count + ", odchudzonych do 1024 / 512: " + odchudzonych + "): " + string.Join(" | ", opisTekstur) +
                  (brakTekstur.Count > 0 ? " — DO POPRAWKI brak fotoskanów: " + string.Join(", ", brakTekstur) : " — wszystkie wykończenia z fotoskanów"));
    }

    static void Krok(string n, Action a)
    {
        try { a(); BraxRegion.Note("fawela: " + n + ": OK"); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA " + n + ": " + e); }
    }

    /// <summary>Siatka kwadratu 150 m dla materiału (kolor wierzchołków = k).</summary>
    internal static Bud W(float x, float z, Material m, Color k) => W(x, z, m).K(k);
    internal static Bud W(float x, float z, Material m)
    {
        var key = (Mathf.FloorToInt(x / Kw), Mathf.FloorToInt(z / Kw));
        if (!komorki.TryGetValue(key, out var d)) komorki[key] = d = new Dictionary<Material, Bud>();
        if (!d.TryGetValue(m, out var b)) d[m] = b = new Bud();
        return b;
    }
    internal static Transform Rodzic(float x, float z)
    {
        var key = (Mathf.FloorToInt(x / Kw), Mathf.FloorToInt(z / Kw));
        if (rodzice.TryGetValue(key, out var t) && t) return t;
        t = new GameObject("Fawela: kwadrat " + key.Item1 + "_" + key.Item2).transform; t.SetParent(root, false); t.position = Vector3.zero;
        return rodzice[key] = t;
    }
    static void Zapisz()
    {
        int liter = BraxUliceKit.Zapisz(Grupa("Fawela: litery szyldów (przestrzenne)"), "fawela_szyldy");
        BraxRegion.Note("fawela: siatek liter szyldów " + liter);
        foreach (var kv in komorki)
        {
            var r = Rodzic((kv.Key.Item1 + .5f) * Kw, (kv.Key.Item2 + .5f) * Kw);
            foreach (var mb in kv.Value) mb.Value.Obiekt("Fawela: " + mb.Key.name, r, mb.Key, !bezCienia.Contains(mb.Key));
        }
    }
    internal static Transform Grupa(string n) { var t = new GameObject(n).transform; t.SetParent(root, false); t.position = Vector3.zero; return t; }
    internal static void Kol(Transform tr, Vector3 mn, Vector3 mx) =>
        BraxRegionMeble.Kolizja(tr, "Fawela: kolizja", (mn + mx) * .5f, new Vector3(Mathf.Abs(mx.x - mn.x), Mathf.Abs(mx.y - mn.y), Mathf.Abs(mx.z - mn.z)));

    // ============================================================================ MATERIAŁY — wyłącznie fotoskany CC0 z Poly Haven (stare, zniszczone)
    internal static Material[] tynki, drzwiStalowe, okna; internal static Material tynkCzerwony, tynkNiebieski, cegla, pustak, beton, betonZbr, blachaRdza, blachaOcynk, drzwiDrewno, drzwiStal, roleta,
        bramaGaraz, okiennica, szyba, metal, zeliwo, alu, zbiornik, plotno, tynkRozowy, roletaSzara, plytkiCiemne, deski, podlogaSali, matySali, matPlakaty, linyRingu, deskiRingu, mataRingu, narozCzerw, narozJasny, asfalt, kraweznik, farba, zarowka, plytki, linoleum, wnetrze, lada, guma, lustro, lodowka, plastik,
        kabel, placBeton, skora, neonZielony, matGraffiti;
    static readonly List<string> opisTekstur = new List<string>(), brakTekstur = new List<string>();

    /// <summary>Zestawy pobierane tylko dla faweli (fetch_region.py: TEXTURES_FAWELA_1K) — wolno je zmniejszać; pozostałe są wspólne z miastem.</summary>
    internal static readonly HashSet<string> TylkoFawela = new HashSet<string>
    {
        "brown_mud_dry", "stony_dirt_path", "painted_plaster_wall", "plastered_wall", "damaged_plaster", "red_brick_plaster_patch_02",
        "white_rough_plaster", "rough_plaster_broken", "red_plaster_weathered", "blue_plaster_weathered", "rebar_reinforced_concrete", "red_bricks_02",
        "slumped_mortar_brick", "concrete_block_wall", "concrete_layers", "rusty_corrugated_iron", "rusty_metal_03", "corrugated_iron_02",
        "corrugated_iron", "wood_peeling_paint_weathered", "old_planks_02", "rusty_metal_04", "green_metal_rust", "blue_metal_plate",
        "rusty_metal_shutter", "rusted_shutter", "wooden_garage_door", "wood_shutter", "metal_plate_02", "asphalt_03",
        "asphalt_04", "concrete_floor_worn_001", "damaged_concrete_floor", "wood_table_worn", "cotton_jersey", "brown_leather",
        "fabric_leather_02", "fabric_leather_01", "weathered_brown_planks",
    };

    /// <summary>Wykończenie główne (ściany, dachy, nawierzchnie): fotoskan — kolor + normalna, do 1024 px.</summary>
    static Material T(string nazwa, float metry, Color tint, float szorstkosc, params string[] ids) => Fotoskan(nazwa, metry, tint, szorstkosc, 1024, false, ids);
    /// <summary>Wykończenie drobne (drzwi, kraty, tkaniny, lady…): fotoskan — sam kolor, do 512 px (pamięć telefonu).</summary>
    static Material Tm(string nazwa, float metry, Color tint, float szorstkosc, params string[] ids) => Fotoskan(nazwa, metry, tint, szorstkosc, 512, true, ids);

    static Material Fotoskan(string nazwa, float metry, Color tint, float szorstkosc, int rozmiar, bool samKolor, string[] ids)
    {
        var m = BraxRegionMeble.Tex("Fawela: " + nazwa, metry, tint, szorstkosc, ids);
        string jest = ids.FirstOrDefault(BraxRegion.HasTexture);
        opisTekstur.Add(nazwa + "=" + (jest ?? "BRAK"));
        if (jest == null) { brakTekstur.Add(nazwa); Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA brak fotoskanu dla „" + nazwa + "” (" + string.Join("/", ids) + ")"); }
        else { try { Odchudz(m, jest, rozmiar, samKolor, szorstkosc); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA odchudzanie „" + nazwa + "”: " + e.Message); } }
        return m;
    }

    // pamięć telefonu: mapa ARM odpada zawsze (szorstkość stała), przy drobnych także normalna; rozmiar map zestawów faweli ograniczony
    static void Odchudz(Material m, string slug, int rozmiar, bool samKolor, float szorstkosc)
    {
        if (!m || !TylkoFawela.Contains(slug) || !m.HasProperty("_MainTex") || !m.GetTexture("_MainTex")) return;
        foreach (string prop in new[] { "_MainTex", "_BumpMap", "_ARM" })
        {
            if (!m.HasProperty(prop)) continue;
            var tx = m.GetTexture(prop); if (!tx) continue;
            if (prop == "_ARM" || (samKolor && prop == "_BumpMap")) { m.SetTexture(prop, null); continue; }
            var imp = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tx)) as TextureImporter;
            if (imp && imp.maxTextureSize > rozmiar) { imp.maxTextureSize = rozmiar; imp.SaveAndReimport(); }
        }
        m.SetFloat("_Roughness", szorstkosc); m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_UseARMForAO")) m.SetFloat("_UseARMForAO", 0f);
        EditorUtility.SetDirty(m); odchudzonych++;
    }
    static int odchudzonych;

    static void Materialy()
    {
        opisTekstur.Clear(); brakTekstur.Clear(); odchudzonych = 0; literyMat.Clear();
        var b = Color.white;
        tynki = new[]
        {
            T("tynk malowany spłowiały", 2.0f, b, .92f, "painted_plaster_wall", "plastered_wall", "white_plaster_02"),
            T("tynk szary cementowy", 2.0f, b, .95f, "white_plaster_02", "plastered_wall", "concrete_wall_008"),
            T("tynk odpadający (widać cegłę)", 1.85f, b, .93f, "damaged_plaster", "red_brick_plaster_patch_02", "painted_plaster_wall"),
            T("farba łuszcząca się na tynku", 2.2f, b, .94f, "white_rough_plaster", "painted_plaster_wall", "plastered_wall"),
            T("tynk żółty obłupany", 2.0f, b, .94f, "rough_plaster_broken", "plastered_wall", "painted_plaster_wall"),
        };
        tynkCzerwony = T("tynk czerwony spłowiały", 2.0f, b, .92f, "red_plaster_weathered", "painted_plaster_wall");
        tynkNiebieski = T("tynk turkusowy łuszczący się", 2.0f, b, .92f, "blue_plaster_weathered", "painted_plaster_wall");
        tynkRozowy = T("farba różowa odpadająca z cegły", 2.0f, b, .92f, "rebar_reinforced_concrete", "peeling_painted_wall", "painted_plaster_wall");
        cegla = T("cegła goła (czerwona, brudna)", 1.0f, b, .9f, "red_brick_03", "red_bricks_02", "brick_wall_001");
        pustak = T("pustak z wyciekającą zaprawą", 1.6f, b, .93f, "slumped_mortar_brick", "concrete_block_wall", "concrete_wall_008");
        beton = T("beton brudny (fundamenty, słupy, murki)", 2.0f, new Color(.92f, .91f, .88f), .93f, "concrete_floor_02", "concrete_layers", "concrete_wall_008");
        betonZbr = T("beton stropów i dachów (warstwy, zacieki)", 1.55f, b, .93f, "concrete_layers", "concrete_wall_008", "concrete_floor_02");
        blachaRdza = T("blacha falista rdzawa", 1.2f, b, .7f, "rusty_corrugated_iron", "rusty_painted_metal", "rusty_metal_03");
        blachaOcynk = T("blacha falista ocynkowana (stara)", 1.4f, b, .5f, "corrugated_iron_02", "corrugated_iron", "painted_metal_shutter");
        drzwiDrewno = Tm("drzwi drewniane — łuszcząca się farba", .76f, b, .8f, "wood_peeling_paint_weathered", "old_planks_02", "wood_planks_grey");
        drzwiStalowe = new[]
        {
            Tm("drzwi stalowe żółte, rdza", 1.35f, b, .6f, "rusty_metal_03", "rusty_metal_04"),
            Tm("drzwi stalowe zielone, rdza", 1.6f, b, .6f, "green_metal_rust", "rusty_metal_04"),
            Tm("drzwi stalowe czerwone, rdza", 1.4f, b, .6f, "rusty_painted_metal", "rusty_metal_04"),
            Tm("drzwi stalowe niebieskie", 1.6f, b, .6f, "blue_metal_plate", "rusty_metal_04"),
            Tm("drzwi stalowe — rdza z resztką farby", 1.4f, b, .6f, "rusty_metal_04", "rusty_metal_03"),
        };
        drzwiStal = drzwiStalowe[2];
        roleta = Tm("roleta stalowa (rdza)", 1.6f, b, .6f, "rusty_metal_shutter", "painted_metal_shutter", "rusted_shutter");
        roletaSzara = Tm("roleta stalowa malowana", 1.6f, b, .6f, "painted_metal_shutter", "rusted_shutter", "rusty_metal_shutter");
        bramaGaraz = Tm("brama garażu z desek", 2.2f, b, .8f, "wooden_garage_door", "wood_plank_wall", "wood_planks_grey");
        okiennica = Tm("okiennice drewniane", 1.0f, b, .8f, "wood_shutter", "wood_planks", "wood_planks_grey");
        metal = Tm("stal malowana z rdzą (kraty, balustrady, okucia)", .9f, b, .55f, "rusty_metal_04", "painted_metal_shutter", "metal_plate");
        zeliwo = Tm("żeliwo włazów (blacha ryflowana)", .9f, b, .7f, "metal_plate", "metal_plate_02", "rusty_metal_04");
        alu = Tm("aluminium ram okien (matowe)", 1.0f, b, .4f, "corrugated_iron", "painted_metal_shutter", "corrugated_iron_02");
        zbiornik = Tm("zbiorniki na wodę (lakier niebieski)", 1.2f, b, .5f, "blue_metal_plate", "painted_metal_shutter");
        plastik = Tm("tworzywo (krzesła, skrzynki, rury, klimatyzatory)", .8f, b, .5f, "leather_white", "plastered_wall", "painted_plaster_wall");
        farba = Tm("farba na nawierzchni (oś, progi)", 1.5f, b, .7f, "plastered_wall", "painted_plaster_wall", "leather_white");
        kabel = Tm("przewody (guma)", .6f, new Color(.55f, .55f, .55f), .7f, "rubber_tiles", "asphalt_03");
        guma = Tm("guma (maty, talerze)", 1.0f, b, .85f, "rubber_tiles", "asphalt_03");
        asfalt = T("asfalt brudny, spękany", 3.0f, new Color(.86f, .86f, .86f), .92f, "asphalt_03", "asphalt_04", "concrete_floor_worn_001");
        kraweznik = T("krawężnik betonowy", 1.2f, new Color(.88f, .87f, .84f), .9f, "concrete_wall_008", "concrete_floor_02");
        placBeton = T("posadzka betonowa wytarta (plac, schody)", 2.4f, b, .9f, "concrete_floor_02", "concrete_floor_worn_001", "damaged_concrete_floor");
        plytki = Tm("płytki lokali (szachownica, brudne)", 1.2f, b, .55f, "square_tiles", "floor_tiles_06", "dirty_tiles");
        plytkiCiemne = Tm("płytki lokali (ceglaste, brudne)", 1.2f, b, .55f, "dirty_tiles", "square_tiles", "floor_tiles_06");
        linoleum = Tm("linoleum starte", .8f, new Color(.80f, .78f, .74f), .6f, "old_linoleum_flooring_01", "dirty_tiles", "concrete_floor_02");
        wnetrze = Tm("ściany lokali — łuszcząca się farba", 2.0f, b, .9f, "peeling_painted_wall", "painted_plaster_wall", "plastered_wall");
        lada = Tm("drewno lad i regałów (wytarte)", 1.4f, b, .7f, "wood_table_worn", "wood_cabinet_worn_long", "wood_planks");
        deski = Tm("deski (skrzynki, blaty straganów)", 1.2f, b, .8f, "wood_planks", "old_planks_02", "wood_planks_grey");
        // RING (10.11 07:13–07:20, właściciel: „obić deskami z zewnątrz, fajnymi, starszymi”, „u góry normalnie tą matę”, „narożniki
        // okrągłe”, „na około prawdziwe linki”, „stare, ale realistyczne”). Uwaga: rough_linen to NIEBIESKI len — liny wychodziły szare.
        linyRingu = Tm("liny ringu — skręcane, bawełna (pożółkłe, przybrudzone)", .25f, Color.white, .9f, "cotton_jersey", "fabric_leather_01");
        deskiRingu = T("podest ringu — stare zwietrzałe deski", 2.4f, new Color(.92f, .9f, .88f), .85f, "weathered_brown_planks", "old_planks_02");
        mataRingu = T("mata ringu — czerwone płótno bawełniane na filcu (wypłowiałe, przybrudzone)", 1.1f, new Color(.8f, .2f, .18f), .95f, "cotton_jersey", "leather_white");   // 10.11 07:52: „matę zrób czerwoną”, nie w kolorze lin
        narozCzerw = Tm("narożnik ringu — czerwona skóra (wytarta)", .7f, new Color(.88f, .26f, .21f), .55f, "leather_white", "fabric_leather_01");   // leather_red_02/03 na Poly Haven bez mapy koloru (tylko warianty coll1/coll2)
        narozJasny = Tm("narożniki ringu — jasna skóra (niebieski i neutralne)", .7f, Color.white, .55f, "leather_white", "fabric_leather_01");
        plotno = Tm("płótno (markizy, plandeki, pranie)", .9f, b, .95f, "cotton_jersey", "rough_linen", "leather_white");
        podlogaSali = T("podłoga sali — stare wytarte deski", 2.0f, new Color(.86f, .82f, .76f), .8f, "old_planks_02", "wood_planks_grey", "wood_planks");   // 10.11: „wood_floor_worn” to nowa sosna (pomarańcz) — nie do starej sali
        matySali = Tm("maty gumowe sali (czarna guma, wytarte)", 1.2f, new Color(.62f, .62f, .64f), .85f, "rubber_tiles", "asphalt_04");   // 10.11: „rubberized_track” to czerwona bieżnia
        skora = Tm("skóra worków, foteli i narożników", .8f, b, .6f, "brown_leather", "fabric_leather_02", "fabric_leather_01");
        // szkło hali sali (przezroczyste), lustra i źródła światła — jedyne powierzchnie bez fotoskanu; okna domów: shader pokoi miasta
        szyba = BraxCityMaterials.Surface("Fawela: szyba okienna (brudna)", new Color(.20f, .24f, .26f, .42f), .1f, .2f);
        {   // lustro: shader odbicia płaskiego (BraxLustro rysuje salę z kamery odbitej); bez shadera — gładki metal z sondą
            var shL = Shader.Find("Braxhorn/Lustro");
            if (shL) { lustro = new Material(shL) { name = "Fawela: lustro (tafla z odbiciem)" }; Directory.CreateDirectory(BraxRegion.Out + "/Fawela"); BraxZapisAssetu.Utworz(lustro, BraxRegion.Out + "/Fawela/lustro.mat"); }
            else { lustro = BraxCityMaterials.Surface("Fawela: lustro", new Color(.78f, .82f, .84f), .05f, .92f); Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA brak shadera Braxhorn/Lustro — lustra bez odbicia płaskiego"); }
        }
        // okna domów: „pokój za szybą” z biblioteki okien miasta (12 faz poziomu podłogi; komórka pokoju 3.2 m × kondygnacja, głębokość 4.2 m)
        okna = new Material[12];
        Directory.CreateDirectory(BraxUliceKit.Out + "/Materialy");
        for (int i = 0; i < 12; i++)
        {
            try { okna[i] = BraxUliceKit.OknoPokoj(2f * SiatkaOkien, Hf, i * Hf / 12f, 0, 5, 4.2f, 0f); } catch (Exception e) { okna[i] = null; Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA okno z pokojem: " + e.Message); }
            if (!okna[i]) okna[i] = szyba;
            bezCienia.Add(okna[i]);
        }
        opisTekstur.Add("okna=" + (okna[0] != szyba && okna[0].shader ? okna[0].shader.name : "BRAK shadera pokoi"));
        zarowka = BraxRegionMeble.Swiecacy("Fawela: żarówka / świetlówka (ciepła)", new Color(1f, .86f, .62f), 1.6f);
        lodowka = BraxRegionMeble.Swiecacy("Fawela: lodówka z napojami (podświetlona)", new Color(.85f, .93f, 1f), .7f);
        neonZielony = BraxRegionMeble.Swiecacy("Fawela: krzyż apteki (świeci)", new Color(.15f, 1f, .35f), 1.8f);
        foreach (var m in new[] { szyba, zarowka, lodowka, neonZielony, kabel }) bezCienia.Add(m);
        try { matPlakaty = Plakaty(); } catch (Exception e) { matPlakaty = null; Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA plakaty: " + e.Message); }
        try { matGraffiti = Graffiti(); } catch (Exception e) { matGraffiti = null; Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA graffiti: " + e.Message); }
        if (matGraffiti) bezCienia.Add(matGraffiti);
    }

    internal static Texture2D Mapa(string plik, params string[] slugi)
    {
        foreach (var s in slugi)
        {
            string p = BraxRegion.PH + "textures/" + s + "/" + plik;
            if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (t) return t;
        }
        return null;
    }
}

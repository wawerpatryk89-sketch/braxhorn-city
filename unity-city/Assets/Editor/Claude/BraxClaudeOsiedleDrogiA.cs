using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// DROGI OSIEDLA A — BUILD 14 (wykonawca „drogi”, 2.11; poprzednia wersja: audyt filmów 1.11, decyzja 16:36 „zrób od nowa,
// zrób wszystkie jednolite”). Przekrój jak w części B: jezdnia | krawężnik | pas zieleni | chodnik.
// Co poprawione w 14 (zdjęcia i nagrania właściciela z buildu 12):
//   • „rondo zasypane płytą chodnikową”, białe „L” przy zebrach, za dużo zebr, gołe płyty asfaltu przy Parku Wiśniowym:
//     pod nowymi drogami leżała CAŁA stara sieć z RenovateA (asfalt na tej samej wysokości co nowy = migotanie, chodniki
//     0.095 m przez rondo, przerywana linia co 8 m wchodząca w nowe zebry, stare zebry na środkach skrzyżowań, asfalt
//     dawnej drogi „do lasu” w trawie przy parku) — grupa „Osiedle A / drogi” usuwana w całości (UsunGrupe);
//   • nic się nie nakłada: odcinki jezdni kończą się na płycie węzła (płyta i jezdnia na tej samej wysokości, bez zakładki),
//     ramiona ronda kończą się na cięciwie pierścienia; farba 2 cm nad asfaltem, w osobnej grupie bez cieni;
//   • bulwar środkowy B2 przesunięty z z = 39 na z = 42: domy Codexa przy (±28, 27) sięgają z = 35.7 — chodnik B2 szedł
//     przez dom, płoty działek (z 37/38) stały na jezdni. Teraz chodnik od z 35.8, płoty na granicy działki (z 35.55);
//   • ulica S2 przesunięta na x = 43.6 (tylny płot działki x 40.5 stał na krawężniku);
//   • rondo: asfalt, opaska z kostki, wyspa z krawężnikiem, trawnikiem, kręgiem kwiatów i niskimi krzewami (środek pusty —
//     miejsce na cokół, decyzja o posągu czeka na właściciela), linia krawędziowa, „zęby” ustąpienia pierwszeństwa,
//     wyspy kierujące (Poznańska) / malowane (B2), zebry 3.6 m przed pierścieniem, znaki A-7 + C-12 na wlotach, C-9;
//   • zebry tylko przy skrzyżowaniach (na skrzyżowaniach T — tylko przez ulicę podporządkowaną) i przy wejściach do parku;
//     każda zebra = wpis w Przejscia (dane dla aut i przechodniów) + znaki D-6 (bez świateł);
//   • parkingi: zatoki 2.5 × 5.0 m × K, linie, odbojnice, krawężniki, min. 0.8 m od ścian (parking przy siłowni — zdjęcie
//     23:38 z autami w murze), każde miejsce w MiejscaParkingowe; gołe asfaltowe „parkingi” przy parku przebudowane;
//   • tablice z nazwą ulicy: dwie tarcze po obu stronach słupka, nic nie przecina napisu (zdjęcie 22:56);
//   • sygnalizacja świetlna na Poznańskiej × B3 (wjazd z tunelu) i Poznańskiej × B1 (BraxClaudeOsiedleSygnalizacja).
// Współrzędne Unity (x = −x three.js).
public static partial class BraxClaudeOsiedle
{
    sealed class UlicaA
    {
        public string id, nazwa;
        public bool alongX;                // true: oś wzdłuż x (stałe z = c)
        public float c, a0, a1;            // oś poprzeczna, zakres wzdłuż osi
        public float asf0 = float.NaN, asf1 = float.NaN;   // zakres jezdni, gdy inny niż a0..a1 (Poznańska: asfalt tunelu od z 95.5)
        public float cw, kerb = .2f, greenNeg, greenPos, walkNeg, walkPos;   // półszerokość jezdni, krawężnik, zieleń i chodnik po stronie −/+
        public bool centerLine = true;
        public float green { set { greenNeg = value; greenPos = value; } }
        public float HalfNeg => cw + kerb + (walkNeg > 0 ? greenNeg + walkNeg : 0f);
        public float HalfPos => cw + kerb + (walkPos > 0 ? greenPos + walkPos : 0f);
        public float Half => Mathf.Max(HalfNeg, HalfPos);
        public float HalfS(int s) => s < 0 ? HalfNeg : HalfPos;
        public float GreenS(int s) => s < 0 ? greenNeg : greenPos;
        public float WalkS(int s) => s < 0 ? walkNeg : walkPos;
        public Vector3 P(float a, float d, float y = 0f) => alongX ? new Vector3(a, y, c + d) : new Vector3(c + d, y, a);
        public Vector3 Wzdluz => alongX ? Vector3.right : Vector3.forward;
        public Vector3 Wpoprzek => alongX ? Vector3.forward : Vector3.right;
    }
    enum RodzajWezla { Zwykly, Rondo, Swiatla }
    sealed class WezelA
    {
        public float x, z; public RodzajWezla rodzaj; public UlicaA ux, uz; public bool E, W, N, S;
        public bool rondo => rodzaj == RodzajWezla.Rondo;
        public Vector3 C => new Vector3(x, 0, z);
        public bool Pelne => E && W && N && S;
    }
    // ramię węzła: kierunek od środka, ulica ramienia u, ulica poprzeczna o, strona s (znak dir wzdłuż osi u)
    struct Ramie
    {
        public Vector3 dir; public UlicaA u, o; public int s;
        public Vector3 R => new Vector3(-dir.z, 0, dir.x);          // prawa strona auta jadącego DO węzła (−dir)
        public bool Trzon(WezelA w) => u.alongX ? !(w.E && w.W) : !(w.N && w.S);   // ulica kończy się na węźle (T)
    }
    // zebra (dla wyposażenia: słupki, D-6, strefy)
    struct ZebraA { public Vector3 c, wzdluz, wpoprzek; public float polSzer, polDl, kraw, zielen; public bool swiatla; }

    const float RondoZ = 42f;
    const float RondoJezdnia = 12.5f, RondoOpaska = 6.6f, RondoWyspa = 4.6f, RondoChodnik = 15.8f;
    const float YAsf = .055f, YFarba = .075f;   // farba rysowana 2 cm nad asfaltem, na końcu dróg wycinana w asfalcie po kształcie i kładziona na lico (runda 4, BraxClaudeOsiedleOznakowanie)

    static List<UlicaA> ulice;
    static List<WezelA> wezly;
    // dla ruchu (BraxClaudeOsiedleRuch): osie chodników między skrzyżowaniami i miejsca parkingowe (środek, kierunek)
    static readonly List<(Vector3 a, Vector3 b)> chodnikiA = new List<(Vector3, Vector3)>();
    static readonly List<(Vector3 p, float yaw)> miejscaA = new List<(Vector3, float)>();
    // osie jezdni (dla etapu aut: trasy po osiach zamiast liczb na sztywno) — a, b na osi, półszerokość jezdni
    public struct OsJezdni { public Vector3 a, b; public float polowaJezdni; public string id; }
    public static readonly List<OsJezdni> OsieJezdni = new List<OsJezdni>();
    static readonly List<ZebraA> zebry = new List<ZebraA>();
    // strefy dla BraxStrefyDrogowe (komponent stawiany w UliceOsiedla, po części B)
    static readonly List<(Vector3 s, Vector3 k, Vector3 kontrola)> strefyRondo = new List<(Vector3, Vector3, Vector3)>();
    static readonly List<(Vector3 s, Vector3 k, int zebra)> strefyZebr = new List<(Vector3, Vector3, int)>();
    static readonly List<(Vector3 c, Vector3 pol)> zebryProstokaty = new List<(Vector3, Vector3)>();
    static int usunietePlyty, przesunietePloty, zatokiA;
    static readonly List<(float x0, float x1, float z0, float z1)> jezdnieKontrola = new List<(float, float, float, float)>();

    static void DrogiA(Transform estate, Transform physics)
    {
        group = "Osiedle A / drogi od nowa";
        chodnikiA.Clear(); miejscaA.Clear(); OsieJezdni.Clear(); zebry.Clear(); strefyRondo.Clear(); strefyZebr.Clear(); zebryProstokaty.Clear(); jezdnieKontrola.Clear();
        usunietePlyty = 0; przesunietePloty = 0; zatokiA = 0; znakiPostawione = 0;
        bezCieni.Add(GrupaFarbyA);
        // 0) stara sieć RenovateA (asfalt 0.055, chodniki 0.095, farba co 8 m, zebry na środkach skrzyżowań) — precz w całości
        usunietePlyty = UsunGrupe("Osiedle A / drogi");
        ulice = new List<UlicaA>
        {
            // bulwary W–E — końce przy x ±64 wchodzą w parkingi przed budynkami
            new UlicaA { id = "B1", alongX = true, c = -19f, a0 = -64f, a1 = 64f, cw = 3.5f, green = .6f, walkNeg = 2.1f, walkPos = 2.1f },
            // B2: z = 42 (domy Codexa sięgają z 35.7): chodnik południowy 2.0 m, zieleń 0.4 m
            new UlicaA { id = "B2", alongX = true, c = RondoZ, a0 = -64f, a1 = 64f, cw = 3.5f, greenNeg = .4f, walkNeg = 2.0f, greenPos = .6f, walkPos = 2.1f },
            new UlicaA { id = "B3", alongX = true, c = 86f, a0 = -64f, a1 = 64f, cw = 3.5f, green = .6f, walkNeg = 2.1f, walkPos = 2.1f },
            // Poznańska (oś x = 0): od bulwaru B1 do bramy tunelu (asfalt tunelu od z 95.5 — bez zakładki)
            new UlicaA { id = "AV", nazwa = "ul_poznanska", alongX = false, c = 0f, a0 = -19f, a1 = 97.5f, asf1 = 95.5f, cw = 4.5f, green = .3f, walkNeg = 2.4f, walkPos = 2.4f },
            // Wiśniowa (x −58) i Bydgoska (x 58): od parkingów przy parku do bulwaru B3
            new UlicaA { id = "N1", nazwa = "ul_wisniowa", alongX = false, c = -58f, a0 = -62f, a1 = 86f, cw = 3f, green = .3f, walkNeg = 2f, walkPos = 2f },
            new UlicaA { id = "N2", nazwa = "ul_bydgoska", alongX = false, c = 58f, a0 = -62f, a1 = 86f, cw = 3f, green = .3f, walkNeg = 2f, walkPos = 2f },
            // uliczki przy domach (chodnik po stronie zewnętrznej); S2 na x 43.6 (płot działki x 40.5 — 0.35 m od krawężnika)
            new UlicaA { id = "S1", alongX = false, c = -43f, a0 = -19f, a1 = RondoZ, cw = 2.5f, green = .3f, walkNeg = 2f, walkPos = 0f, centerLine = false },
            new UlicaA { id = "S2", alongX = false, c = 43.6f, a0 = -19f, a1 = RondoZ, cw = 2.5f, green = .3f, walkNeg = 0f, walkPos = 2f, centerLine = false },
        };
        farbaOdroczona = true; farbaOdroczonaLista.Clear();   // E3a (runda 3): farba prostokątna kładziona na końcu w wycięciach asfaltu
        try { PoszerzUliceA(); } catch (Exception e) { Debug.LogError("BRAXHORN_DROGI_POSZERZENIE DO POPRAWKI: " + e.Message); }   // E3a: przekrój 2 × 3.5 m × K tam, gdzie jest miejsce
        try { SzerokosciUlicA(); } catch (Exception e) { Debug.LogError("BRAXHORN_DROGI_SZEROKOSC DO POPRAWKI: " + e.Message); }
        wezly = new List<WezelA>();
        foreach (var u in ulice.Where(q => q.alongX))
            foreach (var v in ulice.Where(q => !q.alongX))
                if (v.c >= u.a0 - .01f && v.c <= u.a1 + .01f && u.c >= v.a0 - .01f && u.c <= v.a1 + .01f)
                {
                    var w = new WezelA { x = v.c, z = u.c, ux = u, uz = v };
                    w.E = u.a1 > w.x + .5f; w.W = u.a0 < w.x - .5f; w.N = v.a1 > w.z + .5f; w.S = v.a0 < w.z - .5f;
                    w.rodzaj = Mathf.Abs(v.c) < .1f && Mathf.Abs(u.c - RondoZ) < .1f ? RodzajWezla.Rondo
                             : Mathf.Abs(v.c) < .1f && (Mathf.Abs(u.c - 86f) < .1f || Mathf.Abs(u.c + 19f) < .1f) ? RodzajWezla.Swiatla : RodzajWezla.Zwykly;
                    wezly.Add(w);
                }
        try { LukiNaroznikow(); } catch (Exception e) { lukNaroznika.Clear(); Debug.LogError("BRAXHORN_DROGI_LUKI DO POPRAWKI: " + e.Message); }   // E3b: promienie łuków krawężników

        // 1) wycięcie z siatek Codexa: stare nawierzchnie (płasko, y < 0.13), wszystko, co stoi na nowych jezdniach i chodnikach
        //    (latarnie, kosze, słupy tablic — do 6.5 m), parkingi; domów nie ruszamy (pas starego B2 pod domami pominięty)
        var cut = new List<Bounds>();
        foreach (var u in ulice)
        {
            float ext = 1.2f;
            if (u.alongX) cut.Add(MinMax(u.a0 - ext, u.a1 + ext, -.06f, .13f, u.c - u.HalfNeg - .3f, u.c + u.HalfPos + .3f));
            else cut.Add(MinMax(u.c - Mathf.Max(u.HalfNeg, 5.6f) - .3f, u.c + Mathf.Max(u.HalfPos, 5.6f) + .3f, -.06f, .13f, u.a0 - ext, Mathf.Min(u.a1 + ext, 98.7f)));
            if (u.alongX) cut.Add(MinMax(u.a0 - .2f, u.a1 + .2f, -.06f, 6.5f, u.c - u.HalfNeg - .05f, u.c + u.HalfPos + .05f));
            else cut.Add(MinMax(u.c - u.HalfNeg - .05f, u.c + u.HalfPos + .05f, -.06f, 6.5f, u.a0 - .2f, Mathf.Min(u.a1 + .2f, 97.6f)));
        }
        cut.Add(MinMax(-16f, 16f, -.06f, 6.5f, RondoZ - RondoChodnik - .1f, RondoZ + RondoChodnik + .1f));
        // stare bulwary Codexa do x ±112 (B2 dawniej na z 39: pod domami (±28, 27) tylko poza ich obrysem)
        foreach (float z in new[] { -19f, 86f }) cut.Add(MinMax(-113f, 113f, -.06f, .13f, z - 6.6f, z + 6.6f));
        foreach (var (x0, x1) in new[] { (-113f, -34.2f), (-20.3f, 19.4f), (36.7f, 113f) }) cut.Add(MinMax(x0, x1, -.06f, .13f, 32.4f, 35.8f));
        cut.Add(MinMax(-113f, 113f, -.06f, .13f, 35.75f, 48.6f));
        foreach (float x in new[] { -58f, 58f }) cut.Add(MinMax(x - 5.7f, x + 5.7f, -.06f, .13f, -100.6f, 87f));
        cut.Add(MinMax(-46f, -40f, -.06f, .13f, -60f, -25f));
        cut.Add(MinMax(-46.6f, -39.4f, -.1f, 3.2f, -58.7f, -57.3f));   // płotek i tablica LAS · TEREN ZAMKNIĘTY (Codex)
        // place przed budynkami (Codex: fz+18 ± 2.5, szer. 44) — nachodzą na parkingi; odtworzone niżej poza parkingami
        var budynki = new[] { (86f, -42f), (86f, 14f), (-86f, -42f), (-86f, 14f), (86f, 64f), (-86f, 64f) };
        foreach (var (fx, fz) in budynki) { cut.Add(MinMax(fx - 22.2f, fx + 22.2f, -.06f, .13f, fz + 15.3f, fz + 20.7f)); cut.Add(MinMax(fx - 22.2f, fx + 22.2f, -.06f, .13f, fz - 18.7f, fz - 15.3f)); }
        // parkingi przy bulwarach i przy parku (wszystko, co stoi na nich)
        foreach (float s in new[] { -1f, 1f })
        {
            foreach (var (z0, z1) in LotyBulwarow) cut.Add(MinMax(s * 63.8f, s * 107.9f, -.06f, 6.5f, z0 - .3f, z1 + .3f));
            cut.Add(MinMax(s * 50.4f, s * 64.4f, -.06f, .13f, -98f, -61.9f));
        }
        // głaz Codexa (−51, −76) na linii nowego chodnika przy parkingu (korony drzew Verdant nad chodnikiem od 2.2 m — zostają)
        cut.Add(MinMax(-51.9f, -50.1f, -.1f, 1.2f, -76.9f, -75.1f));
        // stare latarnie i kosze Wiśniowej/Bydgoskiej, słupy starych tablic ulic
        foreach (float x in new[] { -58f, 58f })
            foreach (float z in new[] { -7f, 16f, 60f, 80f })
            { cut.Add(MinMax(x - 4.7f, x - 3.3f, -.06f, 5.6f, z - .6f, z + .6f)); cut.Add(MinMax(x + 3.4f, x + 4.6f, -.06f, 5.6f, z - .6f, z + .6f)); }
        foreach (var (sx, sz) in new[] { (-6f, 82f), (51f, 36f), (-51f, -22f) }) cut.Add(MinMax(sx - .3f, sx + .3f, -.06f, 3.2f, sz - .3f, sz + .3f));
        cut.Add(MinMax(49.2f, 52.8f, 2.3f, 3.2f, 35.8f, 36.2f));   // tablica BYDGOSKA Codexa (WIŚNIOWA i POZNAŃSKA wycina WawerHouse)
        CutVolumes(estate, physics, cut);
        WylaczWysokieKolizje(physics, cut.Where(b => b.max.y > 3f).ToList());
        glowicePojazdy = glowicePiesi = skrzyzowaniaSwiatla = 0;

        // 2) płoty działek przy nowym B2
        Guard("drogi A: płoty działek przy B2", PlotyPrzyB2);

        // 3) ulice, skrzyżowania, rondo
        foreach (var u in ulice) UlicaOdcinki(u);
        foreach (var w in wezly) { if (w.rondo) Rondo(w); else Skrzyzowanie(w); }
        // 4) końce: parkingi przy budynkach (bulwary) i przy parku
        foreach (float s in new[] { -1f, 1f })
        {
            ParkingBulwar(s, ulice[0], -21.4f, -4.9f, new[] { (-21.4f, -16.4f, 1f), (-9.9f, -4.9f, -1f) });   // kasyno/komisariat ↔ dyskoteka/szpital
            ParkingBulwar(s, ulice[1], 33.6f, 45.25f, new[] { (33.6f, 38.6f, 1f) });                            // przed dyskoteką/szpitalem
            ParkingBulwar(s, ulice[2], 83.2f, 95.6f, new[] { (90.6f, 95.6f, -1f) });                            // przed salą bokserską/halą walk
            ParkingPark(s);
        }
        // 5) place przed budynkami poza parkingami — płyty od ściany do krawężnika parkingu
        foreach (var (fx, fz) in budynki)
        {
            float zf0 = fz + 15f, zf1 = fz + 20.5f, zr0 = fz - 18.5f, zr1 = fz - 15f;
            float front = zf1, back = zr0;
            foreach (var (l0, l1) in LotyBulwarow) { if (l0 - .2f > zf0 - .5f && l0 - .2f < front) front = l0 - .2f; if (l1 + .2f < zr1 + .5f && l1 + .2f > back - .6f) back = l1 + .2f; }
            float px0 = fx < 0 ? fx - 22f : Mathf.Max(fx - 22f, bulwarKoniecE), px1 = fx < 0 ? Mathf.Min(fx + 22f, bulwarKoniecW) : fx + 22f;   // E3a: plac od nowej krawędzi Wiśniowej/Bydgoskiej
            if (front > zf0 + .2f) BoxMinMax(Pavers, px0, px1, 0, .1f, zf0, front);
            if (back < zr1 - .2f) BoxMinMax(Pavers, px0, px1, 0, .1f, back, zr1);
        }
        // 6) sygnalizacja świetlna
        foreach (var w in wezly.Where(q => q.rodzaj == RodzajWezla.Swiatla)) Guard("drogi A: sygnalizacja (" + w.x + ", " + w.z + ")", () => Sygnalizacja(w));
        group = "Osiedle A / drogi od nowa";
        Guard("drogi A: oznakowanie w wycięciach asfaltu", () => FarbaWWycieciach());
        rep.notes.Add("drogi A (build 14): " + ulice.Count + " ulic, " + wezly.Count + " węzłów (rondo (0, " + RondoZ + "), światła ×" + wezly.Count(q => q.rodzaj == RodzajWezla.Swiatla) + "), "
                      + zebry.Count + " zebr, " + zatokiA + " zatok, usunięte stare nawierzchnie RenovateA: " + usunietePlyty + " płyt, płoty B2: " + przesunietePloty + " odcinków");
        Debug.Log("BRAXHORN_DROGI_A ulice=" + ulice.Count + ", wezly=" + wezly.Count + ", zebry=" + zebry.Count + ", zatoki=" + zatokiA + ", usuniete_plyty=" + usunietePlyty + ", przesuniete_ploty=" + przesunietePloty);
    }
    static readonly (float, float)[] LotyBulwarow = { (-21.4f, -4.9f), (33.6f, 45.25f), (83.2f, 95.6f) };

    // Kolizje Codexa wyższe niż 2.2 m (latarnie 5.4 m) CutVolumes zostawia — w korytarzach ulic wyłączone (inaczej
    // niewidzialne słupy na chodniku po wyciętych latarniach).
    static void WylaczWysokieKolizje(Transform physics, List<Bounds> vols)
    {
        if (!physics) return;
        foreach (var c in physics.GetComponentsInChildren<BoxCollider>(true))
        {
            var b = c.bounds; if (!c.enabled || b.size.y <= 2.2f || b.size.y > 7f || b.size.x > 2f || b.size.z > 2f) continue;
            if (vols.Any(v => { var e = v; e.Expand(new Vector3(.3f, .6f, .3f)); return e.Contains(b.min) && e.Contains(b.max); })) { c.enabled = false; rep.oldFenceCollidersDisabled++; }
        }
    }

    // Płoty działek domów (±28, 27): RenovateA postawił płot frontowy na z 37/38 (dziś jezdnia B2) i płot od Poznańskiej
    // aż do niego. Przycięte do granicy działki z 35.55 (0.3 m przed chodnikiem), nowe odcinki płotu po obu stronach domu
    // (ściana domu Codexa, z 35.4–35.7, sama jest granicą działki).
    static void PlotyPrzyB2()
    {
        const float zG = 35.55f;
        var g = group; group = "Osiedle A / ogrodzenie";
        foreach (var r in new[] { MinMax(16.2f, 39.8f, -.5f, 3f, zG, 48.6f), MinMax(-38.8f, -15.2f, -.5f, 3f, zG, 48.6f) })
        {
            WytnijZGrupy((gr, m) => gr == "Osiedle A / ogrodzenie", r);
            PrzytnijKolizje(colliders, r, 2, true);
            lakaKand.RemoveAll(p => r.Contains(new Vector3(p.x, 0f, p.y)));
        }
        foreach (var (x0, x1) in new[] { (16.5f, 26.45f), (35.85f, 39.5f), (-38.5f, -34.15f), (-20.3f, -15.5f) }) { WoodFenceX(x0, x1, zG); przesunietePloty++; }
        foreach (float x in new[] { 16.5f, -15.5f }) Box(Plinth, new Vector3(x, .725f, zG - .2f), new Vector3(.46f, 1.45f, .46f), false);   // słupek zamyka przycięty murek
        group = g;
    }

    // Odcinek ulicy: jezdnia między płytami węzłów (bez zakładki), krawężnik, zieleń i chodnik przerwane na szerokość
    // ulicy poprzecznej, linia osiowa przerwana przed zebrami (przed światłami — ciągła 15 m), latarnie co ~24 m.
    static void UlicaOdcinki(UlicaA u)
    {
        var cross = wezly.Where(w => u.alongX ? Mathf.Abs(w.z - u.c) < .1f : Mathf.Abs(w.x - u.c) < .1f)
                         .Select(w => (pos: u.alongX ? w.x : w.z, w)).OrderBy(q => q.pos).ToList();
        UlicaA Other(WezelA w) => u.alongX ? w.uz : w.ux;
        (float lo, float hi) Zajete(float pos, WezelA w, float d, bool chodnik, int strona = 0)
        {
            if (w.rondo) { float r = chodnik ? RondoChodnik : RondoJezdnia + .22f; float a = Mathf.Sqrt(Mathf.Max(0f, r * r - d * d)); return (pos - a, pos + a); }
            var o = Other(w); float lo = pos - o.HalfNeg, hi = pos + o.HalfPos;
            // E3b: przy łuku narożnika prosty krawężnik i chodnik zaczynają się za końcem łuku (jezdnia poprzeczna + promień)
            foreach (int st in strona == 0 ? new[] { -1, 1 } : new[] { strona })
            {
                float rLo = u.alongX ? LukNaroznika(w, -1, st) : LukNaroznika(w, st, -1), rHi = u.alongX ? LukNaroznika(w, 1, st) : LukNaroznika(w, st, 1);
                if (rLo > 0f) lo = Mathf.Min(lo, pos - o.cw - rLo);
                if (rHi > 0f) hi = Mathf.Max(hi, pos + o.cw + rHi);
            }
            return (lo, hi);
        }
        float A0 = float.IsNaN(u.asf0) ? u.a0 : u.asf0, A1 = float.IsNaN(u.asf1) ? u.a1 : u.asf1;
        // 1) jezdnia
        var jezd = new List<(float, float)>(); float cur = A0;
        foreach (var (pos, w) in cross)
        {
            float lo, hi;
            if (w.rondo) { float a = Mathf.Sqrt(RondoJezdnia * RondoJezdnia - u.cw * u.cw); lo = pos - a; hi = pos + a; }
            else { var o = Other(w); lo = pos - o.cw; hi = pos + o.cw; }
            if (lo > cur) jezd.Add((cur, Mathf.Min(lo, A1)));
            cur = Mathf.Max(cur, hi);
        }
        if (cur < A1) jezd.Add((cur, A1));
        foreach (var (r0, r1) in jezd) if (r1 - r0 > .01f)
            {
                RectAlong(u, r0, r1, -u.cw, u.cw, Asphalt, YAsf, YAsf, false);
                var p0 = u.P(r0, -u.cw); var p1 = u.P(r1, u.cw);
                jezdnieKontrola.Add((Mathf.Min(p0.x, p1.x), Mathf.Max(p0.x, p1.x), Mathf.Min(p0.z, p1.z), Mathf.Max(p0.z, p1.z)));
            }
        OsieJezdni.Add(new OsJezdni { a = u.P(A0, 0f, YAsf), b = u.P(A1, 0f, YAsf), polowaJezdni = u.cw, id = u.id });
        // 2) krawężniki i chodniki
        var paski = new List<(float d0, float d1, Material m, float y1, int typ)>();
        foreach (int s in new[] { -1, 1 })
        {
            float walk = u.WalkS(s), k0 = u.cw, k1 = u.cw + u.kerb;
            paski.Add((s * k0, s * k1, KerbStone, .16f, 1));
            if (walk > 0) { float g1 = k1 + u.GreenS(s), w1 = g1 + walk; paski.Add((s * g1, s * w1, Pavers, .14f, 3)); }
        }
        foreach (var (d0, d1, m, y1, typ) in paski)
        {
            float dIn = Mathf.Min(Mathf.Abs(d0), Mathf.Abs(d1)), sgn = Mathf.Sign(d0 + d1);
            var runs = new List<(float, float)>(); float c0 = u.a0;
            foreach (var (pos, w) in cross)
            {
                var (lo, hi) = Zajete(pos, w, dIn, typ == 3, (int)sgn);
                if (!w.rondo)
                {
                    // T: ulica poprzeczna nie sięga na tę stronę osi — pasek ciągły
                    var o = Other(w);
                    bool siega = sgn < 0 ? o.a0 < u.c - .5f : o.a1 > u.c + .5f;
                    if (!siega) continue;
                }
                if (lo > c0) runs.Add((c0, lo)); c0 = Mathf.Max(c0, hi);
            }
            if (c0 < u.a1) runs.Add((c0, u.a1));
            foreach (var (r0, r1) in runs) if (r1 - r0 > .05f)
                {
                    RectAlong(u, r0, r1, d0, d1, m, 0f, y1, true);
                    if (typ == 3 && r1 - r0 > 12f)
                    {
                        float dc = (d0 + d1) * .5f;
                        chodnikiA.Add((u.P(r0 + 1f, dc, .14f), u.P(r1 - 1f, dc, .14f)));
                    }
                }
        }
        // 3) linia osiowa: kreski 3 m co 6 m; przed zebrą 2.5 m przerwy, przy rondzie do końca wyspy kierującej (23 m),
        //    przed światłami ciągła od linii zatrzymania 15 m wstecz (P-2b)
        if (u.centerLine)
        {
            var blok = new List<(float, float)>();
            if (u.id == "N1" || u.id == "N2") blok.Add((u.a0 - 1f, u.a0 + 7f));   // zebra przy wejściu do parku (z −60.4…−58)
            foreach (var (pos, w) in cross)
            {
                if (w.rondo) { blok.Add((pos - 23f, pos + 23f)); continue; }
                var (lo, hi) = Zajete(pos, w, 0f, true);
                if (w.rodzaj == RodzajWezla.Swiatla)
                {
                    blok.Add((lo - 17f, hi + 17f));
                    foreach (float sg in new[] { -1f, 1f })
                    {
                        // linia zatrzymania za zebrą ramienia (E3b: zebra za końcem łuku)
                        var ram = new Ramie { dir = u.alongX ? new Vector3(sg, 0, 0) : new Vector3(0, 0, sg), u = u, o = Other(w), s = (int)sg };
                        float zk = ZebraRamienia(w, ram).a1;
                        float s0 = pos + sg * (zk + 1.4f), s1 = s0 + sg * 15f;
                        float x0 = Mathf.Max(Mathf.Min(s0, s1), A0), x1 = Mathf.Min(Mathf.Max(s0, s1), A1);
                        if (x1 - x0 > 1f) { var a = u.P(x0, -.06f); var b = u.P(x1, .06f); Farba(a.x, b.x, a.z, b.z); }
                    }
                }
                else blok.Add((lo - 2.5f, hi + 2.5f));
            }
            for (float a = A0 + 2f; a + 3f < A1 - 2f; a += 6f)
                if (!blok.Any(q => a + 3f > q.Item1 && a < q.Item2)) { var p = u.P(a, -.06f); var q2 = u.P(a + 3f, .06f); Farba(p.x, q2.x, p.z, q2.z); }
        }
        // 4) latarnie co ~24 m przy chodniku (nie na skrzyżowaniach i nie przy zebrach)
        foreach (int s in new[] { -1, 1 })
        {
            if (u.WalkS(s) <= 0) continue;
            float d = s * (u.cw + u.kerb + u.GreenS(s) + .35f);
            for (float a = u.a0 + 9f; a < u.a1 - 6f; a += 24f)
            {
                if (cross.Any(q => { var (lo, hi) = Zajete(q.pos, q.w, Mathf.Abs(d), true); return a > lo - 3f && a < hi + 3f; })) continue;
                var p = u.P(a, d);
                float yaw = u.alongX ? (s > 0 ? 180f : 0f) : (s > 0 ? 270f : 90f);
                if (!IsHardLamp(p)) Lamp(p, yaw);
            }
        }
    }
    static bool IsHardLamp(Vector3 p) => Physics.CheckSphere(p + Vector3.up * 1.5f, .35f, ~0, QueryTriggerInteraction.Ignore);

    // Prostokąt wzdłuż osi ulicy: a0..a1 wzdłuż, d0..d1 w poprzek (od osi). Płaski (y0 == y1) albo bryła.
    static void RectAlong(UlicaA u, float a0, float a1, float d0, float d1, Material m, float y0, float y1, bool box)
    {
        float lo = Mathf.Min(d0, d1), hi = Mathf.Max(d0, d1);
        float x0, x1, z0, z1;
        if (u.alongX) { x0 = a0; x1 = a1; z0 = u.c + lo; z1 = u.c + hi; } else { x0 = u.c + lo; x1 = u.c + hi; z0 = a0; z1 = a1; }
        if (box && y1 > y0) BoxMinMax(m, x0, x1, y0, y1, z0, z1);
        else Top(m, x0, x1, z0, z1, y1);
    }

    static List<Ramie> Ramiona(WezelA w)
    {
        var l = new List<Ramie>();
        if (w.E) l.Add(new Ramie { dir = Vector3.right, u = w.ux, o = w.uz, s = 1 });
        if (w.N) l.Add(new Ramie { dir = Vector3.forward, u = w.uz, o = w.ux, s = 1 });
        if (w.W) l.Add(new Ramie { dir = Vector3.left, u = w.ux, o = w.uz, s = -1 });
        if (w.S) l.Add(new Ramie { dir = Vector3.back, u = w.uz, o = w.ux, s = -1 });
        return l;
    }
    static Vector3 PR(Vector3 c, Ramie r, float a, float d, float y = 0f) => c + r.dir * a + r.R * d + Vector3.up * y;
    // prostokąt (a0..a1 wzdłuż ramienia, d0..d1 w poprzek) → x0, x1, z0, z1 (ramiona są równoległe do osi świata)
    static (float x0, float x1, float z0, float z1) RectR(Vector3 c, Ramie r, float a0, float a1, float d0, float d1)
    {
        var p = PR(c, r, a0, d0); var q = PR(c, r, a1, d1);
        return (Mathf.Min(p.x, q.x), Mathf.Max(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.z, q.z));
    }

    // Zebra na ramieniu węzła (a0..a1 od środka węzła): pasy 0.5 m co 1 m wzdłuż kierunku jazdy, wpis do Przejscia,
    // strefy dla aut (bez świateł) i znaki D-6 po prawej stronie każdego kierunku.
    static void ZebraNaRamieniu(Vector3 c, Ramie r, float a0, float a1, bool swiatla, bool znaki = true)
    {
        var u = r.u; float cw = u.cw;
        for (float d = -cw + .25f; d + .5f <= cw - .15f; d += 1f) { var (x0, x1, z0, z1) = RectR(c, r, a0, a1, d, d + .5f); Farba(x0, x1, z0, z1); }
        float am = (a0 + a1) * .5f;
        var srodek = PR(c, r, am, 0f, YAsf);
        Przejscia.Add(new Przejscie { srodek = srodek, wzdluzJezdni = r.dir, szerokoscJezdni = 2f * cw, zeSwiatlami = swiatla });
        int sR = (int)Mathf.Sign(Vector3.Dot(r.R, u.Wpoprzek));
        float zielen = Mathf.Max(u.GreenS(sR), u.GreenS(-sR));
        zebry.Add(new ZebraA { c = srodek, wzdluz = r.dir, wpoprzek = r.R, polSzer = (a1 - a0) * .5f, polDl = cw, kraw = cw + u.kerb, zielen = zielen, swiatla = swiatla });
        if (swiatla) return;
        int zi = zebryProstokaty.Count;
        var pol = new Vector3(Mathf.Abs(r.dir.x) * ((a1 - a0) * .5f + .2f) + Mathf.Abs(r.R.x) * (cw + u.kerb + .8f), 1.2f, Mathf.Abs(r.dir.z) * ((a1 - a0) * .5f + .2f) + Mathf.Abs(r.R.z) * (cw + u.kerb + .8f));
        zebryProstokaty.Add((srodek + Vector3.up * 1.0f, pol));
        // strefy: auta jadące do węzła (−dir, prawy pas po stronie R) i od węzła (+dir, pas po stronie −R); środek strefy = miejsce
        // środka auta (przód 1 m przed pasami)
        strefyZebr.Add((PR(c, r, a1 + 3.3f, cw * .5f, YAsf), -r.dir, zi));
        strefyZebr.Add((PR(c, r, a0 - 3.3f, -cw * .5f, YAsf), r.dir, zi));
        if (!znaki) return;
        foreach (float sg in new[] { 1f, -1f })   // sg = +1: kierunek −dir (do węzła), znak przed zebrą od strony a1, po prawej (R)
        {
            int side = sg > 0 ? sR : -sR;
            float g = u.GreenS(side), off = cw + u.kerb + (g >= .3f ? g * .5f : .3f);
            var p = PR(c, r, sg > 0 ? a1 + .6f : a0 - .6f, sg * off);
            if (Physics.CheckSphere(p + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore)) p += r.dir * sg * .9f;
            ZnakPionowy(p, r.dir * sg, "D-6");
        }
    }

    // Skrzyżowanie: płyta asfaltu (styka się z jezdniami — bez zakładki), narożniki z płyt, krawężniki narożników, zebry
    // (pełne skrzyżowanie: na każdym wlocie; T: tylko przez ulicę podporządkowaną; światła: na każdym wlocie), tablice ulic.
    static void Skrzyzowanie(WezelA w)
    {
        var ux = w.ux; var uz = w.uz;
        Top(Asphalt, w.x - uz.cw, w.x + uz.cw, w.z - ux.cw, w.z + ux.cw, YAsf);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                bool armX = sx > 0 ? w.E : w.W, armZ = sz > 0 ? w.N : w.S;
                float hx = sx > 0 ? uz.HalfPos : uz.HalfNeg, hz = sz > 0 ? ux.HalfPos : ux.HalfNeg;
                float kx = uz.cw + uz.kerb, kz = ux.cw + ux.kerb;
                bool walkX = (sz > 0 ? ux.walkPos : ux.walkNeg) > 0, walkZ = (sx > 0 ? uz.walkPos : uz.walkNeg) > 0;
                if (!walkX && !walkZ) continue;
                if (!armX || !armZ) continue;    // T: po stronie bez ramienia chodnik ulicy przelotowej jest ciągły
                float lukR = LukNaroznika(w, sx, sz);
                if (lukR > 0f) { NaroznikLuk(w, sx, sz, lukR); continue; }   // E3b: łuk krawężnika zamiast narożnika pod kątem prostym
                float x0 = w.x + sx * kx, x1 = w.x + sx * hx, z0 = w.z + sz * kz, z1 = w.z + sz * hz;
                if (Mathf.Abs(x1 - x0) > .05f && Mathf.Abs(z1 - z0) > .05f) BoxMinMax(Pavers, Mathf.Min(x0, x1), Mathf.Max(x0, x1), 0, .14f, Mathf.Min(z0, z1), Mathf.Max(z0, z1));
                if (Mathf.Abs(hx - kx) > .05f) BoxMinMax(KerbStone, Mathf.Min(w.x + sx * kx, w.x + sx * hx), Mathf.Max(w.x + sx * kx, w.x + sx * hx), 0, .16f, Mathf.Min(w.z + sz * ux.cw, w.z + sz * kz), Mathf.Max(w.z + sz * ux.cw, w.z + sz * kz));
                if (Mathf.Abs(hz - kz) > .05f) BoxMinMax(KerbStone, Mathf.Min(w.x + sx * uz.cw, w.x + sx * kx), Mathf.Max(w.x + sx * uz.cw, w.x + sx * kx), 0, .16f, Mathf.Min(w.z + sz * kz, w.z + sz * hz), Mathf.Max(w.z + sz * kz, w.z + sz * hz));
            }
        bool swiatla = w.rodzaj == RodzajWezla.Swiatla;
        foreach (var r in Ramiona(w))
        {
            if (!swiatla && !w.Pelne && !r.Trzon(w)) continue;
            var (a0, a1) = ZebraRamienia(w, r);   // E3b: przy łuku — za końcem łuku
            if (a1 - a0 < 1.5f) continue;
            ZebraNaRamieniu(w.C, r, a0, a1, swiatla, !swiatla);
        }
        // tablice ulic: w zewnętrznym rogu narożnika (z dala od wejść na zebry i od słupów sygnalizacji)
        foreach (var u in new[] { uz, ux })
        {
            if (string.IsNullOrEmpty(u.nazwa)) continue;
            int sx = w.x <= 0 ? 1 : -1, sz = 1;
            if (!(sx > 0 ? (w.E && uz.walkPos > 0) : (w.W && uz.walkNeg > 0))) sx = -sx;
            var p = TablicaWLuku(w, sx, sz) ?? new Vector3(w.x + sx * (uz.HalfS(sx) - .35f), 0, w.z + sz * (ux.HalfS(sz) - .35f));   // E3b: w łuku chodnika
            TablicaUlicy(u.nazwa, p, u.alongX);
        }
        SkrzyzowaniaInfo.Add(new SkrzyzowanieInfo { srodek = new Vector3(w.x, YAsf, w.z), promien = Mathf.Max(ux.Half, uz.Half), rodzaj = swiatla ? RodzajSkrzyzowania.ZeSwiatlami : RodzajSkrzyzowania.Zwykle });
    }

    // obrócona kolizja (łuki ronda)
    static void ColliderRot(Vector3 c, Vector3 s, float yaw)
    {
        var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false); g.transform.SetPositionAndRotation(c, Quaternion.Euler(0, yaw, 0));
        g.AddComponent<BoxCollider>().size = s;
    }
    // pionowa ściana po łuku (bok krawężnika / chodnika ronda); kąty w stopniach
    static void ArcWall(Material m, Vector3 c, float r, float a0, float a1, float y0, float y1, int seg, bool outward)
    {
        for (int k = 0; k < seg; k++)
        {
            float t0 = Mathf.Lerp(a0, a1, k / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (k + 1) / (float)seg) * Mathf.Deg2Rad;
            Vector3 d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)), d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
            var n = ((d0 + d1) * .5f).normalized * (outward ? 1f : -1f);
            Quad(m, c + d0 * r + Vector3.up * y0, c + d1 * r + Vector3.up * y0, c + d1 * r + Vector3.up * y1, c + d0 * r + Vector3.up * y1, n);
        }
    }

    // Rondo: jezdnia pierścieniowa (asfalt), opaska z kostki (najazdowa, 3 cm), wyspa z wysokim krawężnikiem, trawnikiem,
    // kręgiem kwiatów i niskimi krzewami (środek pusty — miejsce na cokół), chodnik dookoła, oznakowanie, zebry, znaki.
    static void Rondo(WezelA w)
    {
        var c = w.C;
        // 1) jezdnia i opaska
        Ring(Asphalt, c + Vector3.up * YAsf, RondoOpaska, RondoJezdnia, 72);
        var kostka = Pbr("opaska ronda (kostka granitowa)", "concrete_pavers_03", 1.1f, new Color(.62f, .60f, .58f));
        Ring(kostka, c + Vector3.up * (YAsf + .03f), RondoWyspa + .15f, RondoOpaska, 72);
        ArcWall(KerbStone, c, RondoOpaska, 0f, 360f, 0f, YAsf + .03f, 72, true);
        // 2) wyspa: krawężnik wysoki (0.22), trawnik, krąg kwiatów, krzewy
        Ring(KerbStone, c + Vector3.up * .22f, RondoWyspa, RondoWyspa + .15f, 64);
        ArcWall(KerbStone, c, RondoWyspa + .15f, 0f, 360f, YAsf + .03f, .22f, 64, true);
        ArcWall(KerbStone, c, RondoWyspa, 0f, 360f, .18f, .22f, 64, false);
        Disc(Grass, c + Vector3.up * .18f, RondoWyspa, 64);
        FlowerRing(c + Vector3.up * .16f, 2.0f, 2.8f, 0f, 360f, 4242, 0, 2, 1);
        for (int k = 0; k < 10; k++) { var d = Quaternion.Euler(0, k * 36f + 18f, 0) * Vector3.forward; Bush(c + d * 3.75f + Vector3.up * .18f, .9f, k * 41f); }
        for (int k = 0; k < 24; k++) { float t = (k + .5f) * 15f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); ColliderRot(c + d * (RondoWyspa + .08f) + Vector3.up * .15f, new Vector3(.2f, .3f, 1.25f), 90f - t * Mathf.Rad2Deg); }
        // 3) ramiona
        var arms = Ramiona(w);
        var gapy = arms.Select(r => (ang: Mathf.Atan2(r.dir.z, r.dir.x) * Mathf.Rad2Deg, half: r.u.cw + r.u.kerb, r)).ToList();
        void Luki(float r0, float r1, Material m, float y, bool box)
        {
            var gaps = gapy.Select(a => (ang: (a.ang + 360f) % 360f, g: Mathf.Asin(Mathf.Clamp01(a.half / ((r0 + r1) * .5f))) * Mathf.Rad2Deg)).OrderBy(g => g.ang).ToList();
            for (int i = 0; i < gaps.Count; i++)
            {
                float s0 = gaps[i].ang + gaps[i].g, s1 = gaps[(i + 1) % gaps.Count].ang - gaps[(i + 1) % gaps.Count].g + (i + 1 == gaps.Count ? 360f : 0f);
                if (s1 <= s0) continue;
                int seg = Mathf.Max(4, Mathf.CeilToInt((s1 - s0) / 4f));
                if (!box) { var g = group; group = GrupaFarbyA; RingArc(m, c + Vector3.up * y, r0, r1, s0, s1, seg); group = g; continue; }
                RingArc(m, c + Vector3.up * y, r0, r1, s0, s1, seg);
                ArcWall(m, c, r0, s0, s1, 0f, y, seg, false); ArcWall(m, c, r1, s0, s1, 0f, y, seg, true);
                for (int k = 0; k < seg; k++)
                {
                    float t = Mathf.Lerp(s0, s1, (k + .5f) / seg) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
                    ColliderRot(c + d * ((r0 + r1) * .5f) + Vector3.up * (y * .5f), new Vector3(r1 - r0, y, (s1 - s0) * Mathf.Deg2Rad * (r0 + r1) * .5f / seg + .05f), 90f - t * Mathf.Rad2Deg);
                }
            }
        }
        Luki(RondoJezdnia, RondoJezdnia + .22f, KerbStone, .16f, true);
        Luki(RondoJezdnia + .22f, RondoChodnik, Pavers, .14f, true);
        Luki(RondoJezdnia - .45f, RondoJezdnia - .33f, Paint, YFarba, false);          // linia krawędziowa zewnętrzna (między wlotami)
        { var g = group; group = GrupaFarbyA; Ring(Paint, c + Vector3.up * YFarba, RondoOpaska + .1f, RondoOpaska + .22f, 72); group = g; }   // wewnętrzna, ciągła
        HardRound(c, 0f, RondoChodnik + .2f);
        for (float a = 0; a < 360f; a += 5f) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward * ((RondoJezdnia + RondoChodnik) * .5f); Paved(c.x + d.x - 1f, c.x + d.x + 1f, c.z + d.z - 1f, c.z + d.z + 1f); }
        Paved(c.x - RondoJezdnia * .7f, c.x + RondoJezdnia * .7f, c.z - RondoJezdnia * .7f, c.z + RondoJezdnia * .7f);
        foreach (var r in arms)
        {
            var u = r.u; float cw = u.cw; bool wyspa = cw > 4f;   // Poznańska (9 m): wyspa kierująca z krawężnika; bulwary (7 m): malowana
            float zA0 = RondoChodnik + .3f, zA1 = RondoChodnik + 2.8f;
            // „zęby” ustąpienia pierwszeństwa (P-13) na pasie wjazdowym, wierzchołkiem do nadjeżdżającego
            float dMin = wyspa ? .95f : .6f;
            for (float d = dMin; d + .5f <= cw - .1f; d += .75f)
                FarbaWielokat(new[] { PR(c, r, 12.9f, d, YFarba), PR(c, r, 12.9f, d + .5f, YFarba), PR(c, r, 13.5f, d + .25f, YFarba) });
            // wyspa kierująca / malowana, przerwana na zebrę
            foreach (var (b0, b1) in new[] { (13.2f, zA0 - .2f), (zA1 + .2f, 22.5f) })
            {
                if (wyspa)
                {
                    var (x0, x1, z0, z1) = RectR(c, r, b0, b1, -.8f, .8f);
                    BoxMinMax(KerbStone, x0, x1, 0f, .16f, z0, z1);
                }
                else
                {
                    foreach (float d in new[] { -.5f, .38f }) { var (x0, x1, z0, z1) = RectR(c, r, b0, b1, d, d + .12f); Farba(x0, x1, z0, z1); }
                    for (float a = b0 + .4f; a + .9f < b1; a += 1.4f)
                        FarbaWielokat(new[] { PR(c, r, a, -.38f, YFarba), PR(c, r, a + .18f, -.38f, YFarba), PR(c, r, a + .78f, .38f, YFarba), PR(c, r, a + .6f, .38f, YFarba) });
                }
            }
            if (wyspa) { var p = PR(c, r, 22.1f, 0f); ZnakPionowy(p, r.dir, "C-9"); }
            // zebra (3.6 m przed pierścieniem jezdni) + znaki D-6
            ZebraNaRamieniu(c, r, zA0, zA1, false);
            // A-7 + C-12 po prawej stronie wlotu (na chodniku ronda)
            {
                var p = PR(c, r, 14.0f, cw + u.kerb + .55f);
                ZnakPionowy(p, r.dir, "A-7", "C-12");
            }
            // ustąp pierwszeństwa: strefa na pasie wjazdowym, kontrola pierścienia pod prąd (ruch przeciwnie do wskazówek zegara)
            float phi = Mathf.Atan2(r.dir.z, r.dir.x) - 40f * Mathf.Deg2Rad, rm = (RondoOpaska + RondoJezdnia) * .5f;
            strefyRondo.Add((PR(c, r, 13.5f + 2.4f, (dMin + cw) * .5f, YAsf), -r.dir, c + new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi)) * rm + Vector3.up * .8f));
        }
        // latarnie na chodniku ronda (przekątne), oprawą nad jezdnię
        for (int k = 0; k < 4; k++)
        {
            float t = (45f + k * 90f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t));
            var p = c + d * (RondoChodnik - .55f);
            if (!IsHardLamp(p)) Lamp(p, Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg);
        }
        SkrzyzowaniaInfo.Add(new SkrzyzowanieInfo { srodek = c + Vector3.up * YAsf, promien = RondoJezdnia, rodzaj = RodzajSkrzyzowania.Rondo });
        rep.notes.Add("rondo A: (" + w.x + ", " + w.z + "), jezdnia r " + RondoOpaska + "–" + RondoJezdnia + ", opaska z kostki, wyspa r " + RondoWyspa + " (kwiaty, krzewy, środek wolny na cokół), "
                      + arms.Count + " wloty: zęby P-13, A-7 + C-12, zebry 3.6 m od pierścienia");
    }

    // Miejsce parkingowe: dane dla etapu aut + zgodność wstecz (miejscaA: środek, obrót).
    static void Zatoka(Vector3 srodek, Vector3 przod, bool przyScianie)
    {
        przod.y = 0; przod.Normalize();
        MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = srodek, przod = przod, rozmiar = new Vector2(2.5f * Ks, 5f * Ks), przyScianie = przyScianie });
        miejscaA.Add((srodek, Mathf.Atan2(przod.x, przod.z) * Mathf.Rad2Deg));
    }
    // Rząd zatok prostopadłych wzdłuż x: tył zatok na zTyl, przód (alejka) w kierunku sgn (±z). Linie, odbojnice, dane.
    static int RzadZatokX(float xa, float xb, float zTyl, float sgn, bool przyScianie)
    {
        float w = 2.5f * Ks, L = 5f * Ks; int n = Mathf.FloorToInt((xb - xa + .01f) / w); if (n <= 0) return 0;
        float zPrz = zTyl + sgn * L;
        for (int k = 0; k <= n; k++) { float x = xa + k * w; Farba(x - .06f, x + .06f, zTyl, zPrz); }
        for (int k = 0; k < n; k++)
        {
            float x = xa + (k + .5f) * w;
            BoxMinMax(Concrete, x - .8f * Ks, x + .8f * Ks, 0f, .14f, Mathf.Min(zTyl + sgn * .75f, zTyl + sgn * .9f), Mathf.Max(zTyl + sgn * .75f, zTyl + sgn * .9f), false);   // odbojnica
            Zatoka(new Vector3(x, YAsf, (zTyl + zPrz) * .5f), new Vector3(0, 0, sgn), przyScianie);
        }
        return n;
    }
    static int RzadZatokZ(float za, float zb, float xTyl, float sgn, bool przyScianie)
    {
        float w = 2.5f * Ks, L = 5f * Ks; int n = Mathf.FloorToInt((zb - za + .01f) / w); if (n <= 0) return 0;
        float xPrz = xTyl + sgn * L;
        for (int k = 0; k <= n; k++) { float z = za + k * w; Farba(xTyl, xPrz, z - .06f, z + .06f); }
        for (int k = 0; k < n; k++)
        {
            float z = za + (k + .5f) * w;
            BoxMinMax(Concrete, Mathf.Min(xTyl + sgn * .75f, xTyl + sgn * .9f), Mathf.Max(xTyl + sgn * .75f, xTyl + sgn * .9f), 0f, .14f, z - .8f * Ks, z + .8f * Ks, false);
            Zatoka(new Vector3((xTyl + xPrz) * .5f, YAsf, z), new Vector3(sgn, 0, 0), przyScianie);
        }
        return n;
    }

    // Parking na końcu bulwaru (między budynkami): asfalt od x ±64 do ±107.5, krawężniki dookoła (poza wjazdem z jezdni
    // bulwaru), rzędy zatok prostopadłych (2.5 × 5 m × K, przodem do alejki), odbojnice, latarnie, znak D-18.
    static void ParkingBulwar(float side, UlicaA bulwar, float z0, float z1, (float a, float b, float sgn)[] rzedy)
    {
        float xIn = side > 0 ? Mathf.Max(64f, bulwar.a1) : Mathf.Min(-64f, bulwar.a0), xOut = side * 107.5f, xa = Mathf.Min(xIn, xOut), xb = Mathf.Max(xIn, xOut);   // E3a: od końca bulwaru (nowa krawędź Wiśniowej/Bydgoskiej)
        Top(Asphalt, xa, xb, z0, z1, YAsf);
        // krawężniki (bez zakładek — narożniki należą do krawężników wzdłuż x): wzdłuż x, koniec, od strony bulwaru poza
        // jezdnią, domknięcie jezdni bulwaru poza parkingiem
        float kxa = side > 0 ? xIn : xOut - .2f, kxb = side > 0 ? xOut + .2f : xIn;
        BoxMinMax(KerbStone, kxa, kxb, 0, .16f, z0 - .2f, z0);
        BoxMinMax(KerbStone, kxa, kxb, 0, .16f, z1, z1 + .2f);
        BoxMinMax(KerbStone, Mathf.Min(xOut, xOut + side * .2f), Mathf.Max(xOut, xOut + side * .2f), 0, .16f, z0, z1);
        float j0 = bulwar.c - bulwar.cw, j1 = bulwar.c + bulwar.cw;
        float kx0 = side > 0 ? xIn : xIn - .2f, kx1 = side > 0 ? xIn + .2f : xIn;
        foreach (var (q0, q1) in new[] { (z0, Mathf.Min(j0, z1)), (Mathf.Max(j1, z0), z1) }) if (q1 - q0 > .05f) BoxMinMax(KerbStone, kx0, kx1, 0, .16f, q0, q1);
        float ex0 = side > 0 ? xIn - .2f : xIn, ex1 = side > 0 ? xIn : xIn + .2f;
        if (j0 < z0 - .01f) BoxMinMax(KerbStone, ex0, ex1, 0, .16f, j0, z0);
        if (j1 > z1 + .01f) { BoxMinMax(KerbStone, ex0, ex1, 0, .16f, z1, j1); BoxMinMax(KerbStone, Mathf.Min(xIn - side * .2f, xIn - side * 1.2f), Mathf.Max(xIn - side * .2f, xIn - side * 1.2f), 0, .16f, z1, z1 + .2f); }
        jezdnieKontrola.Add((xa, xb, z0, z1));
        int n = 0;
        float sx0 = Mathf.Min(side * 69f, side * 104.5f), sx1 = Mathf.Max(side * 69f, side * 104.5f);
        foreach (var (a, b, sgn) in rzedy) n += RzadZatokX(sx0, sx1, sgn > 0 ? a : b, sgn, false);
        zatokiA += n;
        // latarnie na krawędzi bez zatok albo za krawężnikiem rzędu (na placu), oprawą nad parking
        foreach (float lx in new[] { 74f, 90f, 104f })
        {
            bool dol = rzedy.All(r => r.sgn < 0) || rzedy.Length == 2;
            float lz = dol ? z0 - .6f : z1 + .6f;
            var p = new Vector3(side * lx, 0, lz);
            if (!IsHardLamp(p)) Lamp(p, dol ? 0f : 180f);
            if (rzedy.Length == 2) { var q = new Vector3(side * lx, 0, z1 + .6f); if (!IsHardLamp(q)) Lamp(q, 180f); }
        }
        var zp = new Vector3(xIn + side * 1.0f, 0, z1 + .75f);
        if (Physics.CheckSphere(zp + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore)) zp.z = z0 - .75f;
        ZnakPionowy(zp, new Vector3(-side, 0, 0), "D-18");
        rep.notes.Add("parking " + (side > 0 ? "E" : "W") + " przy bulwarze z " + bulwar.c + ": " + n + " zatok");
    }

    // Parking przy parku na końcu Wiśniowej / Bydgoskiej (z −62 … −95): alejka 6.1 m (x ±53…±59.1), rząd zatok od strony
    // zewnętrznej (x ±59.1…±64.1) — po stronie wschodniej stoi siłownia A (ściana x 64.9): koniec zatoki 0.8 m od ściany,
    // odbojnice. Od strony parku chodnik (odtworzona ścieżka x ±51.7 Codexa), plac łączący z aleją parku z −99, zebra
    // przez ulicę przy wejściu do parku, latarnie, znak D-18.
    static void ParkingPark(float s)
    {
        float z0 = -95f, z1 = -62f;
        float xA = s * 53f, xS = s * 59.1f, xT = s * 64.1f;     // alejka od, zatoki od, tył zatok
        float xa = Mathf.Min(xA, xT), xb = Mathf.Max(xA, xT);
        Top(Asphalt, xa, xb, z0, z1, YAsf);
        jezdnieKontrola.Add((xa, xb, z0, z1));
        float kP0 = Mathf.Min(s * 52.8f, s * 53f), kP1 = Mathf.Max(s * 52.8f, s * 53f), kT0 = Mathf.Min(xT, xT + s * .2f), kT1 = Mathf.Max(xT, xT + s * .2f);
        BoxMinMax(KerbStone, kP0, kP1, 0, .16f, z0, z1 - .2f);                       // od parku
        BoxMinMax(KerbStone, kT0, kT1, 0, .16f, z0, z1 - .2f);                       // tył zatok
        BoxMinMax(KerbStone, Mathf.Min(s * 52.8f, xT + s * .2f), Mathf.Max(s * 52.8f, xT + s * .2f), 0, .16f, z0 - .2f, z0);   // koniec
        foreach (var (a, b) in new[] { (52.8f, 55f), (61f, 64.3f) })                // od ulicy poza jezdnią
            BoxMinMax(KerbStone, Mathf.Min(s * a, s * b), Mathf.Max(s * a, s * b), 0, .16f, z1 - .2f, z1);
        bool sciana = s > 0;    // siłownia A (x 64.9…107, z −94.75…−66)
        int n = RzadZatokZ(-94.2f, z1 - 1f, xT, -s, sciana);
        zatokiA += n;
        if (sciana) BoxMinMax(Pavers, Mathf.Min(s * 64.3f, s * 64.88f), Mathf.Max(s * 64.3f, s * 64.88f), 0, .14f, z0 - .2f, z1);
        // chodnik od strony parku + plac przy alei z −99
        BoxMinMax(Pavers, Mathf.Min(s * 50.6f, s * 52.8f), Mathf.Max(s * 50.6f, s * 52.8f), 0, .14f, -97.75f, z1 - .2f);
        BoxMinMax(Pavers, Mathf.Min(s * 52.8f, s * 58f), Mathf.Max(s * 52.8f, s * 58f), 0, .14f, -97.75f, z0 - .2f);
        chodnikiA.Add((new Vector3(s * 51.7f, .14f, -96.5f), new Vector3(s * 51.7f, .14f, -63f)));
        foreach (float lz in new[] { -66f, -80.5f, -94f }) { var p = new Vector3(s * 51.05f, 0, lz); if (!IsHardLamp(p)) Lamp(p, s > 0 ? 90f : 270f); }
        // zebra przez ulicę przy wejściu do parku (z −60.4 … −58.0), z wpisem i znakami
        var u = ulice.First(q => !q.alongX && Mathf.Abs(q.c - s * 58f) < .1f);
        var r = new Ramie { dir = Vector3.forward, u = u, o = u, s = 1 };
        ZebraNaRamieniu(new Vector3(s * 58f, 0, -62f), r, 1.6f, 4.0f, false);
        var zp = new Vector3(s * 62.4f, 0, -61.4f);
        if (Physics.CheckSphere(zp + Vector3.up * 1.5f, .3f, ~0, QueryTriggerInteraction.Ignore)) zp.z += .9f;
        ZnakPionowy(zp, Vector3.forward, "D-18");
        rep.notes.Add("parking przy parku x " + (s * 58f) + ": " + n + " zatok (" + (sciana ? "0.8 m od ściany siłowni, odbojnice" : "od strony łąki") + "), chodnik do alei parku, zebra przy wejściu");
    }

    // E3a (build 45, pomiar): ile miejsca jest za chodnikiem każdej ulicy A (do pierwszej kolizji: płot, mur, budynek, słup)
    // — promienie co 4 m wzdłuż ulicy (bez 12 m przy węzłach), na wysokości 0.5 i 1.2 m, do 4 m od zewnętrznej krawędzi
    // chodnika. Cel jezdni 2 × 3.5 × K. Build 45 tylko MIERZY i zapisuje tabelę (poszerzenie przesuwa krawężniki, chodniki,
    // zatoki, zebry, sygnalizację i trasy — osobny etap; płoty działek nie wszędzie mają kolizje, więc pomiar to górna granica).
    static void SzerokosciUlicA()
    {
        Physics.SyncTransforms();
        float cel = 3.5f * K; const int maska = ~((1 << 8) | (1 << 29) | (1 << 2));
        foreach (var u in ulice)
        {
            float dN = 4f, dP = 4f; int n = 0;
            for (float a = u.a0 + 12f; a <= u.a1 - 12f; a += 4f)
            {
                bool przyWezle = ulice.Any(v => v != u && v.alongX != u.alongX && Mathf.Abs(a - v.c) < 12f && u.c >= v.a0 - 1f && u.c <= v.a1 + 1f);
                if (przyWezle) continue;
                n++;
                foreach (int sd in new[] { -1, 1 })
                {
                    float kraw = (sd < 0 ? u.HalfNeg : u.HalfPos);
                    foreach (float y in new[] { .5f, 1.2f })
                    {
                        var start = u.P(a, sd * kraw, y); var dir = u.Wpoprzek * sd;
                        float wolne = Physics.Raycast(start, dir, out var hit, 4f, maska, QueryTriggerInteraction.Ignore) ? hit.distance : 4f;
                        if (sd < 0) dN = Mathf.Min(dN, wolne); else dP = Mathf.Min(dP, wolne);
                    }
                }
            }
            float bylo = 2f * u.cw, mozna = 2f * (u.cw + Mathf.Max(0f, Mathf.Min(dN, dP) - .3f)), brak = Mathf.Max(0f, 2f * cel - mozna);
            float przed = szerPrzed.TryGetValue(u.id, out var sp) ? sp : bylo;
            Debug.Log("BRAXHORN_DROGI_SZEROKOSC ulica=" + (u.nazwa ?? u.id) + " przed=" + przed.ToString("F2") + " po=" + bylo.ToString("F2") + " [" + (szerOpis.TryGetValue(u.id, out var so) ? so : "—") + "] możliwe_jeszcze=" + Mathf.Min(mozna, 2f * cel).ToString("F2")
                + " cel=" + (2f * cel).ToString("F2") + " brak_miejsca=" + brak.ToString("F2") + " chodnik −/+ " + u.walkNeg.ToString("F1") + "/" + u.walkPos.ToString("F1") + " (≥ " + (1.5f * K).ToString("F2") + ")"
                + " zieleń −/+ " + u.greenNeg.ToString("F1") + "/" + u.greenPos.ToString("F1") + " wolne za chodnikiem −/+ " + dN.ToString("F1") + "/" + dP.ToString("F1") + " m (próbek " + n + ")");
        }
    }
}

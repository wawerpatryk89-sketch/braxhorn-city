using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// STACJA PALIW + MYJNIA (build 45b, E21; właściciel 20:59 o kostce „Poznańska 5” i długiej willi Codexa „4”: „nie wiem, po co one
// w ogóle są” → 21:03: „może być myjnia i stacja benzynowa, dobre miejsce”). Oba budynki ZWALONE (siatki Codexa wycięte w obrysach
// razem z tarasem, płotem i słupkami bramy, kolizje budynków usunięte, drzwi z manifestu: contact-front, contact-garden, villa-front,
// villa-terrace, villa-bathroom usunięte, punkty podróży domu/willi zastąpione stacją i myjnią). Nawierzchnia od nowa.
// Działka: x −37…13, z −64…−25.4 (od chodnika ulicy B1 do 3 m przed Parkiem Wiśniowym). Stare drzewo Codexa (−18, −29) stało na krawędzi
// wiaty — zwalone z budynkami; drzewo (18, −29) między stacją a warsztatem zostaje.
//   • STACJA: wiata 31 × 13.5 m (prześwit 4.5 m × K) na 3 wyspach z dystrybutorami (po 2 pistolety z wężami na stronę, słupki
//     ochronne), rynny i rury spustowe w słupach, oprawy LED w podsufitce; sklep 15.5 × 11.5 m (12 × 9 m × K): witryna z ramami,
//     drzwi przesuwne (otwarte), cokół, attyka z obróbką, opaski i parapety, rury spustowe; wewnątrz posadzka z płytek z fugą,
//     listwy przypodłogowe, lada z kasą i kasjerem, chłodziarki z podświetleniem, 2 regały z towarem, gniazdka, oprawy, kratki
//     wentylacyjne, gaśnica, znak WYJŚCIE; pylon z cenami bez marek (litery przestrzenne).
//   • MYJNIA: hala automatyczna (brama portalowa na szynach, 3 szczotki, brama rolowana podniesiona), 2 stanowiska
//     samoobsługowe (zadaszenie, przegrody, wysięgniki obrotowe z lancą, panele z wrzutnikiem), odpływy liniowe w wycięciach,
//     2 odkurzacze z wężem; ściany z paneli z fugą (szczelina 12 mm × K pokazuje podkład — nic nie leży na licu).
//   • nawierzchnia: asfalt jak ulice (0.055), beton myjni, płyty przed sklepem; krawężniki z fugą co 1 m × K; kratki, studzienki,
//     odwodnienie liniowe i oznakowanie (strzałki, linia zatrzymania, koperty odkurzaczy) W WYCIĘCIACH asfaltu (nie naklejane).
// Logi: BRAXHORN_STACJA, BRAXHORN_MYJNIA, BRAXHORN_DETALE obiekt=…. Punkty interakcji (tankowanie, mycie) jako obiekty-znaczniki.
public static partial class BraxClaudeOsiedle
{
    static readonly bool StacjaWlaczona = true;   // flaga E21 (false = stara Poznańska 5 i willa Codexa zostają)
    const float SX0 = -37f, SX1 = 13f, SZ0 = -64f, SZ1 = -25.4f;   // działka stacji (z1 = chodnik B1)
    const float YS = .055f;                                         // asfalt jak ulice A
    const string GrupaStacjiAsf = "Osiedle A / stacja paliw — nawierzchnia", GrupaStacjiZnaki = "Osiedle A / stacja paliw — oznakowanie";
    static int stDetale, stKraweznikiFugi, stKratki, stStudzienki, stOdplywy, stZnaki, stPunkty, stPanele;
    static readonly List<string> stDetaleOpis = new List<string>();

    static Material StMat(string key, Color c, float smooth = .3f, float metal = 0f) => Plain("stacja: " + key, c, smooth, metal);
    static Material StSwiatlo(string key, Color c)
    {
        string nazwa = "stacja: światło " + key;
        if (mats.TryGetValue(nazwa, out var have)) return have;
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + nazwa, enableInstancing = true };
        m.color = Color.Lerp(c, Color.white, .3f); m.SetFloat("_Glossiness", .6f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mats[nazwa] = m; tile[m] = 1f; return m;
    }
    // Obwód z 4 brył BEZ zakładek w narożach (attyki, obróbki, gzymsy): pas szerokości w, okap e tylko na zewnątrz.
    static void ObwodBez(Material m, float x0, float x1, float z0, float z1, float w, float y0, float y1, float e, bool bottom = false)
    {
        BoxMinMax(m, x0 - e, x1 + e, y0, y1, z0 - e, z0 + w, false, bottom); BoxMinMax(m, x0 - e, x1 + e, y0, y1, z1 - w, z1 + e, false, bottom);
        BoxMinMax(m, x0 - e, x0 + w, y0, y1, z0 + w, z1 - w, false, bottom); BoxMinMax(m, x1 - w, x1 + e, y0, y1, z0 + w, z1 - w, false, bottom);
    }
    // Krawędzie ulicy B1 od strony działek (południe): krawężnik i zewnętrzna krawędź chodnika — z opisu ulicy po poszerzeniu (E3a).
    static (float zKr, float zKr1, float zChod) KrawedzieB1()
    {
        var b1 = ulice?.FirstOrDefault(q => q.id == "B1");
        if (b1 == null) return (-22.5f, -22.7f, -25.4f);
        return (b1.c - b1.cw, b1.c - b1.cw - b1.kerb, b1.c - b1.HalfNeg);
    }
    static void StDetal(string obiekt, string co, int n = 1) { stDetale += n; stDetaleOpis.Add(obiekt + ":" + co + (n > 1 ? "×" + n : "")); }

    // ---------------- krok główny ----------------
    static void StacjaIMyjnia(Transform estate, Transform physics)
    {
        if (!StacjaWlaczona) { Debug.Log("BRAXHORN_STACJA wyłączona flagą (StacjaWlaczona=false)"); return; }
        stDetale = stKraweznikiFugi = stKratki = stStudzienki = stOdplywy = stZnaki = stPunkty = stPanele = 0; stDetaleOpis.Clear();
        var g0 = group;
        int zwalone = StacjaZwal(estate, physics);
        Guard("stacja: nawierzchnia, zjazdy, krawężniki, odwodnienie, oznakowanie", StacjaNawierzchnia);
        Guard("stacja: wiata i wyspy z dystrybutorami", StacjaWiata);
        Guard("stacja: pylon z cenami", StacjaPylon);
        Guard("stacja: sklep (bryła, detale, wnętrze)", StacjaSklep);
        Guard("myjnia: hala automatyczna", MyjniaHala);
        Guard("myjnia: stanowiska samoobsługowe", MyjniaSamoobslugowa);
        Guard("myjnia: odkurzacze", MyjniaOdkurzacze);
        Guard("stacja: ogrodzenie tylne, zieleń", StacjaOgrodzenie);
        Guard("stacja: punkty podróży", StacjaPunktyPodrozy);
        group = g0;
        Physics.SyncTransforms();
        // kontrola: w obrysach zwalonych budynków nie zostało nic starego z kolizją (poza nowymi kolizjami stacji)
        int stare = 0;
        foreach (var v in StacjaObrysyZwalenia())
            foreach (var c in Physics.OverlapBox(v.center, v.extents - new Vector3(.05f, .05f, .05f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (c && (estate && c.transform.IsChildOf(estate) || physics && c.transform.IsChildOf(physics)) && c.bounds.max.y > .12f) stare++;
        Debug.Log("BRAXHORN_STACJA obrys x " + SX0 + "…" + SX1 + " z " + SZ0 + "…" + SZ1 + " zwalone=" + zwalone + " (Poznańska 5 + willa Codexa: siatki, kolizje, drzwi, punkty) stare_kolizje_w_obrysie=" + stare
                  + " wiata=31.0×13.5 m prześwit=" + (4.5f * K).ToString("F2") + " m wyspy=3 dystrybutory=3 pistolety=12 sklep=15.5×11.5 m (12×9 m × K) kasjer=1 chłodziarki=4 regały=2 pylon=1 punkty_tankowania=6");
        Debug.Log("BRAXHORN_MYJNIA hala_automatyczna=9.5×13.5 m brama_portalowa=1 szczotki=3 stanowiska_samoobsługowe=2 lance=2 odkurzacze=2 odpływy_liniowe=" + stOdplywy + " panele_ścian=" + stPanele + " punkty_mycia=3");
        Debug.Log("BRAXHORN_DETALE obiekt=stacja+myjnia razem=" + stDetale + " krawężniki_fugi_co_1m=" + stKraweznikiFugi + " kratki=" + stKratki + " studzienki=" + stStudzienki + " odpływy_liniowe=" + stOdplywy
                  + " oznakowanie_w_wycięciach=" + stZnaki + " [" + string.Join(", ", stDetaleOpis) + "]");
        rep.notes.Add("stacja paliw + myjnia (E21): Poznańska 5 i willa Codexa zwalone (" + zwalone + "), wiata z 3 wyspami, sklep, myjnia automatyczna + 2 samoobsługowe, odkurzacze; detale " + stDetale);
    }

    static List<Bounds> StacjaObrysyZwalenia() => new List<Bounds>
    {
        // cała działka: oba budynki, taras, płot, słupki bramy i stare drzewo Codexa (−18, −29) — stało pniem na krawędzi wiaty
        // (rzut z liczb kodu: korona 2.7–7.4 m przecinała gzyms wiaty 5.8–7.1 m), drzewo (18, −29) między stacją a warsztatem zostaje;
        // y od 0.01: „Original virtual floor” (y −0.18…0) NIE jest cięta — podłoga z kolizją zostaje pod nową nawierzchnią;
        // od strony ulicy do 5 cm przed chodnikiem B1 (po poszerzeniu E3a chodnik sięga dalej na południe)
        MinMax(SX0, SX1, .01f, 14f, SZ0, Mathf.Min(-25.5f, KrawedzieB1().zChod - .05f)),
    };

    // Układ stacji (te same liczby co w krokach budowy) — test w BraxTestNaprawOsiedla przed generowaniem osiedla.
    static readonly (string co, float x0, float x1, float z0, float z1)[] StacjaUklad =
    {
        ("wiata", -31.5f, -.5f, -42.5f, -29f), ("sklep", -36.5f, -21f, -60.5f, -49f), ("myjnia samoobsługowa", -19f, -6f, -60.5f, -48.5f),
        ("myjnia automatyczna", -4f, 5.5f, -62f, -48.5f), ("odkurzacze", 7f, SX1, -62f, -47.5f), ("pylon", -36f, -34f, -27.4f, -26.6f),
    };
    public static string KontrolaUkladuStacji()
    {
        var b = StacjaUklad;
        foreach (var r in b) if (r.x0 < SX0 - .01f || r.x1 > SX1 + .01f || r.z0 < SZ0 - .01f || r.z1 > SZ1 + .01f) return r.co + " poza działką";
        for (int i = 0; i < b.Length; i++)
            for (int j = i + 1; j < b.Length; j++)
                if (b[i].x0 < b[j].x1 - .01f && b[j].x0 < b[i].x1 - .01f && b[i].z0 < b[j].z1 - .01f && b[j].z0 < b[i].z1 - .01f) return b[i].co + " nachodzi na " + b[j].co;
        // pasy przy wyspach (wyspa 1.55 m, z −36.53…−34.97) ≥ 4.5 m; zjazdy omijają latarnię B1 (x −31)
        if (-29f - (-34.97f) < 4.5f || -36.53f - (-42.5f) < 4.5f) return "pas przy wyspie węższy niż 4.5 m";
        foreach (var (a, c) in new[] { (-29.5f, -23f), (4.5f, 11f) }) if (c - a < 6f || (a < -30.4f && c > -31.6f)) return "zjazd koliduje z latarnią";
        // drzewo Codexa (18, −29), korona r ≈ 2.6 m, zostaje między działkami: ani stacja (do x 13), ani warsztat (od x 21.5) go nie ruszają
        if (SX1 > 18f - 2.7f || WX0 < 18f + 2.7f) return "działka nachodzi na drzewo (18, −29)";
        // warsztat (E32): działka obok stacji (między nimi stare drzewo Codexa x 18), budynek 24 × 12 m w działce, plac do z −42
        if (WX0 < SX1 + 5f) return "warsztat nachodzi na stację";
        if (23f < WX0 || 47f > WX1 || -54f < WZ0 || -42f > WZ1) return "budynek warsztatu poza działką";
        if (2.5f * K * 4f > 47f - 29f) return "4 miejsca postojowe nie mieszczą się przed warsztatem";
        return null;
    }

    // ---------------- zwalenie ----------------
    static int StacjaZwal(Transform estate, Transform physics)
    {
        group = "Osiedle A / stacja paliw";
        var vols = StacjaObrysyZwalenia();
        // geometria generatora na zwalonych budynkach: okna/drzwi Poznańskiej 5 (krok Poznanska5) i numer „4” z kinkietem willi (Szyldy)
        int grupy = UsunGrupe("Osiedle A / Poznańska 5") + UsunGrupe("Elewacje / A willa Wiśniowa 4");
        int przed = rep.oldFenceTrianglesRemoved;
        if (estate) CutVolumes(estate, physics, vols);
        int trojkaty = rep.oldFenceTrianglesRemoved - przed;
        // kolizje budynków (ściany wyższe niż 2.2 m CutVolumes zostawia) — usunięte, gdy leżą w obrysie
        int kol = 0;
        bool W(Bounds b) => vols.Any(v => { var e = v; e.Expand(.6f); return e.Contains(b.min) && e.Contains(b.max); });
        foreach (var root in new[] { estate, physics, colliders })
        {
            if (!root) continue;
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
            {
                if (!c || c is MeshCollider) continue;
                var b = c.bounds; if (b.max.y < .12f || b.size.x > 60f || b.size.z > 60f) continue;   // podłoga zostaje
                if (!W(b)) continue;
                UnityEngine.Object.DestroyImmediate(c); kol++;
            }
        }
        // geometria generatora z wcześniejszych kroków w obrysie (łączone siatki i osobne modele) — zwalona razem z budynkami;
        // podłoże (trawa y −0.01) i ulice (chodnik B1 kończy się na z −25.4) poza obrysem
        int gen = 0;
        foreach (var v in vols) gen += WytnijZGrupy((gr, m) => !gr.StartsWith("Podłoże") && !gr.StartsWith("Osiedle A / stacja") && !gr.StartsWith("Osiedle A / myjnia") && !gr.StartsWith("Osiedle A / drogi") && gr != GrupaFarbyA, v);
        if (root)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r || r.bounds.max.y < .05f || !W(r.bounds)) continue;
                UnityEngine.Object.DestroyImmediate(r.gameObject); gen++;
            }
        // drzwi z manifestu Codexa (skrzydła, kolizje, punkt OTWÓRZ DRZWI)
        var ids = new HashSet<string> { "contact-front", "contact-garden", "villa-front", "villa-terrace", "villa-bathroom" };
        int drzwi = 0;
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d && ids.Contains(d.id)) { UnityEngine.Object.DestroyImmediate(d.gameObject); drzwi++; }
        foreach (var w in UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (w.doors != null) w.doors = w.doors.Where(x => x).ToArray();
            // villa_front zostaje (stare testy CI: check.mjs / check_estate_original_player.mjs sprawdzają, że punkt jest osiągalny) —
            // przepięty na wejście sklepu stacji w StacjaPunktyPodrozy; salon, taras i dom kontaktowy usunięte
            if (w.points != null) w.points = w.points.Where(p => p == null || !(p.id == "villa_living" || p.id == "villa_terrace" || p.id == "contact")).ToArray();
        }
        rep.notes.Add("stacja: zwalone — " + trojkaty + " trójkątów siatek Codexa, " + kol + " kolizji, " + drzwi + " drzwi, " + grupy + " czworokątów okien/numeru");
        Debug.Log("BRAXHORN_STACJA zwalenie trójkąty=" + trojkaty + " kolizje=" + kol + " drzwi=" + drzwi + "/5 (contact-front, contact-garden, villa-front, villa-terrace, villa-bathroom) okna_i_numer_generatora=" + grupy + " czworokątów inne_z_generatora=" + gen);
        return trojkaty + kol + drzwi + gen;
    }

    // ---------------- nawierzchnia ----------------
    static readonly List<Bounds> stWyciecia = new List<Bounds>();
    static void StacjaNawierzchnia()
    {
        stWyciecia.Clear();
        group = GrupaStacjiAsf;
        float zj0a = -29.5f, zj1a = -23f, zj0b = 4.5f, zj1b = 11f;          // zjazd (wjazd zach., wyjazd wsch.) — x
        // 1) zjazdy przez chodnik ulicy B1: krawężnik, zieleń i chodnik wycięte, krawężnik obniżony, asfalt na 2 cm niżej od chodnika
        var (zKr, zKr1, zChod) = KrawedzieB1();   // E3a: krawędzie B1 po poszerzeniu
        foreach (var (a, b) in new[] { (zj0a, zj1a), (zj0b, zj1b) })
        {
            var wyc = MinMax(a, b, -.1f, .6f, zChod - .02f, zKr + .02f);
            WytnijZGrupy((gr, m) => gr == "Osiedle A / drogi od nowa" && m != Asphalt, wyc);
            PodzielKolizje(colliders, wyc);
            for (float x = a; x < b - .01f; x += K)   // krawężnik obniżony z fugą co 1 m × K
            {
                float x1 = Mathf.Min(b, x + K - .01f);
                BoxMinMax(KerbStone, x, x1, YS - .03f, YS + .03f, zKr1, zKr, false);
                if (x1 < b - .005f) { BoxMinMax(StMat("fuga krawężnika", new Color(.42f, .42f, .41f), .1f), x1, x1 + .01f, YS - .03f, YS + .025f, zKr1 + .005f, zKr - .005f, false); stKraweznikiFugi++; }
            }
            Top(Asphalt, a, b, Mathf.Min(zChod, zKr1), Mathf.Max(zChod, zKr1), YS + .02f);
        }
        StDetal("stacja", "zjazdy z obniżonym krawężnikiem", 2);
        // 2) asfalt placu (bez pasa zieleni od ulicy, bez wysp, bez sklepu i myjni)
        float zF0 = -47.5f, zF1 = -28.4f;
        AsfaltProst(SX0, SX1, zF0, zF1);
        AsfaltProst(zj0a, zj1a, zF1, zChod); AsfaltProst(zj0b, zj1b, zF1, zChod);
        AsfaltProst(7f, SX1, -62f, zF0);                                                  // strefa odkurzaczy
        // 3) beton myjni (stanowiska i hala) — płyty z fugą dylatacyjną co 3 m × K (szczelina pokazuje podkład)
        var beton = Pbr("stacja: beton myjni", "concrete_pavement_02", 3f, new Color(.78f, .78f, .77f));
        var fuga = StMat("fuga dylatacji", new Color(.18f, .18f, .18f), .1f);
        foreach (var (x0, x1, z0, z1) in new[] { (-19f, -6f, -60.5f, zF0), (-4f, 5.5f, -62f, zF0) })   // do krawędzi placu (bez zakładki z asfaltem)
        {
            BoxMinMax(fuga, x0, x1, YS - .06f, YS - .01f, z0, z1, false);
            for (float z = z1; z > z0 + .01f; z -= 3f * K)
            {
                float za = Mathf.Max(z0, z - 3f * K + .012f);
                BoxMinMax(beton, x0, x1, YS - .01f, YS, za, z, true);
            }
            StDetal("myjnia", "beton z dylatacją co 3 m × K");
        }
        // 4) krawężniki placu z fugą co 1 m × K (od trawnika: zachód, front przy ulicy między zjazdami)
        KrawFugi(true, zF1 + .1f, SX0, zj0a - .05f); KrawFugi(true, zF1 + .1f, zj1a + .05f, zj0b - .05f); KrawFugi(true, zF1 + .1f, zj1b + .05f, SX1);   // w pasie zieleni (asfalt kończy się na z −28.4)
        KrawFugi(false, SX0 + .1f, zF0, zF1);
        // 5) odwodnienie: liniowe pod krawędzią wiaty, kratki, studzienki — w wycięciach asfaltu (pokrywa na poziomie asfaltu)
        OdplywLiniowy(-31f, -1f, -42.9f, true);
        foreach (var (x, z) in new[] { (-34.5f, -45.5f), (10.5f, -45.5f), (8f, -30.5f), (-33.5f, -30.5f) }) Kratka(x, z);
        foreach (var (x, z) in new[] { (-21.5f, -44.8f), (2f, -33.5f) }) Studzienka(x, z);   // poza strzałkami i odpływem
        // 6) oznakowanie w wycięciach: strzałki kierunku jazdy (na wschód), linia zatrzymania przy wyjeździe, koperty odkurzaczy
        foreach (float x in new[] { -33f, -14f }) StrzalkaWschod(x, -45.5f);
        ZnakWycieciu(zj0b + .2f, zj1b - .2f, -28.9f, -28.9f + .5f * K);                        // linia zatrzymania P-12
        foreach (float z in new[] { -61.8f, -55.2f, -48.6f }) ZnakWycieciu(7.2f, SX1 - .2f, z, z + .12f * K);   // koperty odkurzaczy
        StDetal("plac", "oznakowanie w wycięciach", stZnaki);
    }
    static void AsfaltProst(float x0, float x1, float z0, float z1)
    {
        var g = group; group = GrupaStacjiAsf;
        for (float x = x0; x < x1 - .01f; x += 17f)
            for (float z = z0; z < z1 - .01f; z += 17f)
                Top(Asphalt, x, Mathf.Min(x1, x + 17f), z, Mathf.Min(z1, z + 17f), YS);
        group = g;
    }
    static void WytnijAsfalt(Bounds b) { WytnijZGrupy((gr, m) => gr == GrupaStacjiAsf && m == Asphalt, b); stWyciecia.Add(b); }
    // Krawężnik z kamieni 1 m × K i fugą 1 cm (fuga = osobna bryła w szczelinie, nic nie leży na kamieniu).
    static void KrawFugi(bool wzdluzX, float c, float a0, float a1)
    {
        var fug = StMat("fuga krawężnika", new Color(.42f, .42f, .41f), .1f);
        WytnijAsfalt(wzdluzX ? MinMax(a0, a1, YS - .05f, YS + .05f, c - .1f, c + .1f) : MinMax(c - .1f, c + .1f, YS - .05f, YS + .05f, a0, a1));   // krawężnik w wycięciu
        for (float a = a0; a < a1 - .01f; a += K)
        {
            float b = Mathf.Min(a1, a + K - .01f);
            if (wzdluzX) BoxMinMax(KerbStone, a, b, 0f, .17f, c - .1f, c + .1f, true); else BoxMinMax(KerbStone, c - .1f, c + .1f, 0f, .17f, a, b, true);
            if (b < a1 - .005f)
            {
                if (wzdluzX) BoxMinMax(fug, b, b + .01f, 0f, .165f, c - .095f, c + .095f, false); else BoxMinMax(fug, c - .095f, c + .095f, 0f, .165f, b, b + .01f, false);
                stKraweznikiFugi++;
            }
        }
    }
    static void OdplywLiniowy(float a0, float a1, float c, bool wzdluzX)
    {
        float w = .15f * K;   // korytko 15 cm × K
        var b = wzdluzX ? MinMax(a0, a1, YS - .05f, YS + .05f, c - w * .5f - .03f, c + w * .5f + .03f) : MinMax(c - w * .5f - .03f, c + w * .5f + .03f, YS - .05f, YS + .05f, a0, a1);
        WytnijAsfalt(b);
        var korpus = StMat("korytko polimerobeton", new Color(.30f, .30f, .31f), .2f); var ruszt = StMat("ruszt stalowy ocynk", new Color(.55f, .56f, .57f), .5f, .7f);
        var ciemno = StMat("wnętrze korytka", new Color(.04f, .04f, .04f), .1f);
        if (wzdluzX)
        {
            BoxMinMax(korpus, a0, a1, YS - .12f, YS, c - w * .5f - .03f, c - w * .5f, false); BoxMinMax(korpus, a0, a1, YS - .12f, YS, c + w * .5f, c + w * .5f + .03f, false);
            BoxMinMax(ciemno, a0, a1, YS - .14f, YS - .12f, c - w * .5f, c + w * .5f, false);
            for (float x = a0 + .03f; x < a1 - .03f; x += .05f) BoxMinMax(ruszt, x, x + .012f, YS - .02f, YS, c - w * .5f, c + w * .5f, false);   // pręty poprzeczne co 5 cm
        }
        else
        {
            BoxMinMax(korpus, c - w * .5f - .03f, c - w * .5f, YS - .12f, YS, a0, a1, false); BoxMinMax(korpus, c + w * .5f, c + w * .5f + .03f, YS - .12f, YS, a0, a1, false);
            BoxMinMax(ciemno, c - w * .5f, c + w * .5f, YS - .14f, YS - .12f, a0, a1, false);
            for (float z = a0 + .03f; z < a1 - .03f; z += .05f) BoxMinMax(ruszt, c - w * .5f, c + w * .5f, YS - .02f, YS, z, z + .012f, false);
        }
        stOdplywy++;
    }
    static void Kratka(float x, float z)
    {
        float h = .25f * K;   // wpust 50 × 50 cm × K
        WytnijAsfalt(MinMax(x - h - .05f, x + h + .05f, YS - .05f, YS + .05f, z - h - .05f, z + h + .05f));
        var rama = StMat("żeliwo wpustu", new Color(.12f, .12f, .12f), .35f, .6f); var ciemno = StMat("wnętrze korytka", new Color(.04f, .04f, .04f), .1f);
        BoxMinMax(rama, x - h - .05f, x + h + .05f, YS - .03f, YS, z - h - .05f, z - h, false); BoxMinMax(rama, x - h - .05f, x + h + .05f, YS - .03f, YS, z + h, z + h + .05f, false);
        BoxMinMax(rama, x - h - .05f, x - h, YS - .03f, YS, z - h, z + h, false); BoxMinMax(rama, x + h, x + h + .05f, YS - .03f, YS, z - h, z + h, false);
        BoxMinMax(ciemno, x - h, x + h, YS - .2f, YS - .18f, z - h, z + h, false);
        for (float a = x - h + .04f; a < x + h - .02f; a += .07f) BoxMinMax(rama, a, a + .025f, YS - .03f, YS, z - h, z + h, false);
        stKratki++;
    }
    static void Studzienka(float x, float z)
    {
        float h = .35f * K;   // właz 70 cm × K (kwadratowy, żeliwny, z ryflowaniem)
        WytnijAsfalt(MinMax(x - h - .08f, x + h + .08f, YS - .05f, YS + .05f, z - h - .08f, z + h + .08f));
        var zel = StMat("żeliwo włazu", new Color(.15f, .15f, .16f), .3f, .6f); var rama = StMat("rama włazu", new Color(.22f, .22f, .22f), .3f, .5f);
        BoxMinMax(rama, x - h - .08f, x + h + .08f, YS - .04f, YS, z - h - .08f, z + h + .08f, false);
        BoxMinMax(zel, x - h, x + h, YS, YS + .004f, z - h, z + h, false);
        for (float a = x - h + .05f; a < x + h - .03f; a += .09f) BoxMinMax(zel, a, a + .03f, YS + .004f, YS + .009f, z - h + .04f, z + h - .04f, false);   // ryflowanie
        stStudzienki++;
    }
    // Oznakowanie: prostokąt farby w wycięciu asfaltu (lico farby = lico asfaltu, bez nakładania).
    static void ZnakWycieciu(float x0, float x1, float z0, float z1)
    {
        WytnijAsfalt(MinMax(x0, x1, YS - .02f, YS + .02f, z0, z1));
        var g = group; group = GrupaStacjiZnaki; Top(Paint, x0, x1, z0, z1, YS); group = g; stZnaki++;
    }
    static void StrzalkaWschod(float x, float z)
    {
        float L = 4f * K, w = .15f * K;
        ZnakWycieciu(x, x + L * .62f, z - w, z + w);
        // grot jako schodki prostokątów (każdy w wycięciu) — bez trójkątów naklejanych
        // 3.11 15:02 („co to za strzałki”): 6 schodków wyglądało jak klocki — 28 pasków po ~5 cm, krawędź grotu gładka z kamery
        const int n = 28;
        for (int k = 0; k < n; k++)
        {
            float x0 = x + L * .62f + k * L * .38f / n, half = (.6f * K) * (1f - (k + .5f) / n);
            ZnakWycieciu(x0, x0 + L * .38f / n, z - half, z + half);
        }
    }

    // ---------------- wiata i wyspy ----------------
    static void StacjaWiata()
    {
        group = "Osiedle A / stacja paliw — wiata";
        float x0 = -31.5f, x1 = -.5f, z0 = -42.5f, z1 = -29f, yP = 4.5f * K, yG = yP + 1.65f * K;
        var biel = StMat("panel wiaty czarny połysk BRAXHORN", new Color(.05f, .05f, .06f), .75f, .3f); /* 3.11: stacja nie „siwa” — czerń jak szyld */ var pas = StMat("pas wiaty grafit", new Color(.16f, .17f, .19f), .5f, .3f);
        var obr = StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f); var spod = StMat("podsufitka wiaty", new Color(.86f, .87f, .88f), .4f);
        var led = StSwiatlo("oprawa LED wiaty", new Color(1f, .98f, .92f));
        // dach: podsufitka, gzyms (pas dolny grafit, pas górny biały), obróbka attyki, płyta dachu ze spadkiem do rynien
        BoxMinMax(spod, x0 + .12f, x1 - .12f, yP, yP + .04f, z0 + .12f, z1 - .12f, true, true);   // podsufitka wewnątrz pasa gzymsu (bez wspólnego lica)
        ObwodBez(pas, x0, x1, z0, z1, .12f, yP, yP + .35f, 0f, true);
        ObwodBez(biel, x0, x1, z0, z1, .12f, yP + .35f, yG - .06f, 0f);
        ObwodBez(obr, x0, x1, z0, z1, .12f, yG - .06f, yG, .03f);   // obróbka attyki (kapinos)
        ObwodBez(StSwiatlo("pas LED wiaty pomarańczowy", new Color(1f, .55f, .08f)), x0, x1, z0, z1, .12f, yP + .33f, yP + .38f, .015f);   // linia LED jak na szyldzie
        BoxMinMax(StMat("membrana dachu", new Color(.35f, .36f, .37f), .2f), x0 + .12f, x1 - .12f, yP + .04f, yP + .3f, z0 + .12f, z1 - .12f, false);
        StDetal("wiata", "gzyms dwupasmowy + obróbka attyki");
        // oprawy LED w podsufitce (wpuszczone: lico oprawy = lico podsufitki od dołu)
        int opr = 0;
        for (float x = x0 + 2.5f; x < x1 - 1.5f; x += 4f)
            foreach (float z in new[] { -39.6f, -35.75f, -31.9f })
            {
                BoxMinMax(StMat("ramka oprawy", new Color(.5f, .5f, .52f), .5f, .6f), x - .64f, x + .64f, yP - .015f, yP, z - .34f, z + .34f, false, true);
                BoxMinMax(led, x - .6f, x + .6f, yP - .02f, yP - .015f, z - .3f, z + .3f, false, true); opr++;
            }
        StDetal("wiata", "oprawy LED", opr);
        // słupy na wyspach (stal w okładzinie), rury spustowe w słupach (wylot przy podstawie do kratki)
        float[] wx = { -25f, -15.5f, -6f }; const float wz = -35.75f, wl = 6.5f, ww = 1.55f;
        var slup = StMat("okładzina słupa", new Color(.88f, .89f, .9f), .55f, .3f);
        foreach (float x in wx)
        {
            BoxMinMax(slup, x - .26f, x + .26f, YS + .2f, yP, wz - .26f, wz + .26f, true);
            BoxMinMax(pas, x - .28f, x + .28f, YS + .2f, YS + .55f, wz - .28f, wz + .28f, false);   // cokół słupa
            Tube(obr, new Vector3(x + .3f, yP, wz), new Vector3(x + .3f, YS + .5f, wz), .05f, 10);  // rura spustowa
            Tube(obr, new Vector3(x + .3f, YS + .5f, wz), new Vector3(x + .5f, YS + .25f, wz), .05f, 10);
        }
        StDetal("wiata", "rury spustowe", 3);
        // wyspy: krawężnik stalowy, wypełnienie betonowe, dystrybutor, pistolety z wężami (po 2 na stronę), słupki ochronne, kosz
        var wyspaB = Pbr("stacja: wyspa beton", "concrete_pavement_02", 1.4f, new Color(.85f, .85f, .84f));
        var stalK = StMat("kątownik wyspy", new Color(.45f, .46f, .47f), .4f, .7f);
        var obud = StMat("obudowa dystrybutora", new Color(.92f, .93f, .94f), .6f, .2f); var panel = StMat("panel dystrybutora", new Color(.12f, .13f, .15f), .7f, .1f);
        var ekran = StSwiatlo("wyświetlacz dystrybutora", new Color(.35f, .75f, 1f));
        var waz = StMat("wąż paliwowy", new Color(.03f, .03f, .03f), .45f);
        var pist = new[] { StMat("pistolet zielony", new Color(.1f, .55f, .2f), .5f), StMat("pistolet czarny", new Color(.05f, .05f, .05f), .5f), StMat("pistolet niebieski", new Color(.1f, .3f, .7f), .5f), StMat("pistolet żółty", new Color(.9f, .7f, .05f), .5f) };
        var zolty = StMat("słupek ochronny żółty", new Color(.95f, .75f, .05f), .5f); var czarny = StMat("pas słupka czarny", new Color(.05f, .05f, .05f), .4f);
        int pistolety = 0, slupki = 0;
        foreach (float x in wx)
        {
            WytnijAsfalt(MinMax(x - wl * .5f, x + wl * .5f, YS - .05f, YS + .05f, wz - ww * .5f, wz + ww * .5f));
            BoxMinMax(wyspaB, x - wl * .5f + .05f, x + wl * .5f - .05f, 0, YS + .2f, wz - ww * .5f + .05f, wz + ww * .5f - .05f, true);
            foreach (var (a0, a1, b0, b1) in new[] { (x - wl * .5f, x + wl * .5f, wz - ww * .5f, wz - ww * .5f + .05f), (x - wl * .5f, x + wl * .5f, wz + ww * .5f - .05f, wz + ww * .5f), (x - wl * .5f, x - wl * .5f + .05f, wz - ww * .5f + .05f, wz + ww * .5f - .05f), (x + wl * .5f - .05f, x + wl * .5f, wz - ww * .5f + .05f, wz + ww * .5f - .05f) })
                BoxMinMax(stalK, a0, a1, 0, YS + .21f, b0, b1, false);
            // dystrybutor 1.0 × 0.5 × 2.0 m × K (obok słupa, od zachodu)
            float dx = x - 1.9f, y0 = YS + .2f, dh = 2f * K, dw = .5f * K, dl = 1.0f * K;
            BoxMinMax(obud, dx - dl * .5f, dx + dl * .5f, y0, y0 + dh, wz - dw * .5f, wz + dw * .5f, true);
            BoxMinMax(pas, dx - dl * .5f - .02f, dx + dl * .5f + .02f, y0, y0 + .12f, wz - dw * .5f - .02f, wz + dw * .5f + .02f, false);   // cokół
            BoxMinMax(obr, dx - dl * .5f - .03f, dx + dl * .5f + .03f, y0 + dh, y0 + dh + .05f, wz - dw * .5f - .03f, wz + dw * .5f + .03f, false);   // daszek
            foreach (int s in new[] { -1, 1 })
            {
                float f = wz + s * dw * .5f;
                BoxMinMax(panel, dx - dl * .42f, dx + dl * .42f, y0 + dh * .55f, y0 + dh * .92f, Mathf.Min(f, f + s * .01f), Mathf.Max(f, f + s * .01f), false);
                BoxMinMax(ekran, dx - dl * .32f, dx + dl * .32f, y0 + dh * .74f, y0 + dh * .88f, Mathf.Min(f + s * .01f, f + s * .014f), Mathf.Max(f + s * .01f, f + s * .014f), false);
                BoxMinMax(ekran, dx - dl * .32f, dx + dl * .32f, y0 + dh * .6f, y0 + dh * .7f, Mathf.Min(f + s * .01f, f + s * .014f), Mathf.Max(f + s * .01f, f + s * .014f), false);
                for (int k = 0; k < 2; k++)
                {
                    float px = dx - dl * .25f + k * dl * .5f; var m = pist[(k + (s > 0 ? 2 : 0)) % pist.Length];
                    var kab = new Vector3(px, y0 + dh * .45f, f + s * .06f);
                    BoxMinMax(m, px - .07f, px + .07f, kab.y - .18f, kab.y + .05f, Mathf.Min(f, f + s * .12f), Mathf.Max(f, f + s * .12f), false);   // pistolet w kaburze
                    var top = new Vector3(px, y0 + dh + .2f, f + s * .05f);   // zwijacz węża nad licem (wąż nie przechodzi przez obudowę)
                    Tube(waz, new Vector3(px, kab.y + .02f, f + s * .1f), new Vector3(px + .05f, y0 + dh * .2f, f + s * .32f), .022f, 8);   // pętla węża
                    Tube(waz, new Vector3(px + .05f, y0 + dh * .2f, f + s * .32f), new Vector3(px, y0 + dh * .95f, f + s * .2f), .022f, 8);
                    Tube(waz, new Vector3(px, y0 + dh * .95f, f + s * .2f), top, .022f, 8);
                    pistolety++;
                    if (k == 0)
                    {
                        var pk = new GameObject("BRAXHORN punkt interakcji: tankowanie (" + (s < 0 ? "pas płd." : "pas płn.") + ", wyspa x " + x.ToString("F1") + ")");
                        pk.transform.SetParent(Group(group), false); pk.transform.position = new Vector3(dx, YS, wz + s * (ww * .5f + 1.6f)); stPunkty++;
                    }
                }
            }
            // słupki ochronne na końcach wysp (żółte z czarnymi pasami)
            foreach (float ex in new[] { x - wl * .5f + .3f, x + wl * .5f - .3f })
            {
                Cylinder(zolty, new Vector3(ex, YS + .2f, wz), .09f, 1.0f * K, 14, true, true);
                foreach (float yy in new[] { .45f, .85f }) Cylinder(czarny, new Vector3(ex, YS + .2f + yy, wz), .095f, .1f, 14, false, false);
                slupki++;
            }
            // kosz i stojak z ręcznikami papierowymi przy słupie
            BoxMinMax(StMat("kosz stacji", new Color(.25f, .27f, .3f), .5f, .4f), x + 1.1f, x + 1.55f, YS + .2f, YS + 1.15f, wz - .22f, wz + .22f, true);
        }
        StDetal("wyspy", "pistolety z wężami", pistolety); StDetal("wyspy", "słupki ochronne", slupki); StDetal("wyspy", "kątowniki krawędzi", 3);
        // pas wiaty: grafika BRAXHORN · STACJA PALIW (3:1) po obu stronach; bez grafiki — złote litery
        if (!SzyldPanel("stacja_pas", new Vector3((x0 + x1) * .5f, (yP + .38f + yG - .06f) * .5f, z1 + .01f), Vector3.forward, 4.5f * K, yG - yP - .54f))
            Napis3D("BRAXHORN", new Vector3((x0 + x1) * .5f, yP + .52f, z1 + .01f), Vector3.forward, .55f, .06f, StSwiatlo("napis BRAXHORN złoty", new Color(1f, .72f, .18f)), pas, .15f, .14f, true);
        if (!SzyldPanel("stacja_pas", new Vector3((x0 + x1) * .5f, (yP + .38f + yG - .06f) * .5f, z0 - .01f), Vector3.back, 4.5f * K, yG - yP - .54f))
            Napis3D("BRAXHORN", new Vector3((x0 + x1) * .5f, yP + .52f, z0 - .01f), Vector3.back, .55f, .06f, StSwiatlo("napis BRAXHORN złoty", new Color(1f, .72f, .18f)), pas, .15f, .14f, true);
    }

    // ---------------- szyldy z grafik właściciela (czat, 3.11 15:40) ----------------
    // Grafiki w zaszyfrowanej paczce „grafiki” → Assets/Source/Prywatne/Grafiki/szyldy/<plik>.jpg. Panel na licu bryły (płaski szyld,
    // podświetlony emisją). Brak paczki = dotychczasowe litery 3D.
    const string SzyldyDir = "Assets/Source/Prywatne/Grafiki/szyldy/";
    static int szyldyObraz, szyldyBrak;
    public static Material SzyldObraz(string plik)
    {
        string p = SzyldyDir + plik + ".jpg";
        if (!System.IO.File.Exists(p)) { szyldyBrak++; Debug.Log("BRAXHORN_SZYLDY_GRAFIKI brak " + p + " — litery 3D"); return null; }
        UnityEditor.AssetDatabase.ImportAsset(p, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (!tex) { szyldyBrak++; return null; }
        var m = new Material(Shader.Find("Standard")) { name = "szyld grafika " + plik, mainTexture = tex };
        m.SetFloat("_Glossiness", .35f);
        m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.6f, .6f, .6f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        return SalonZapisz(m, "szyld " + plik);
    }
    public static bool SzyldPanel(string plik, Vector3 c, Vector3 n, float w, float h)
    {
        var m = SzyldObraz(plik); if (!m) return false;
        // Cała grafika zachowuje proporcje, a panel mieści się w dostępnej powierzchni.
        if (m.mainTexture)
        {
            float ratio = (float)m.mainTexture.width / m.mainTexture.height;
            if (w / h > ratio) w = h * ratio; else h = w / ratio;
        }
        BillPlakat(Group(group), "szyld " + plik, c + n * .006f, n, w, h, m);
        Rejestruj(plik, c + n * .006f, n, w, h);
        szyldyObraz++; Debug.Log("BRAXHORN_SZYLDY_GRAFIKI " + plik + " " + w.ToString("F2") + "×" + h.ToString("F2") + " m w " + c.ToString("F1"));
        return true;
    }

    // ---------------- pylon cenowy (bez marek) ----------------
    static void StacjaPylon()
    {
        group = "Osiedle A / stacja paliw — pylon";
        float x = -35f, z = -27f, w = 1.5f * K, d = .4f * K, h = 6f * K;
        var obud = StMat("pylon obudowa", new Color(.16f, .17f, .19f), .5f, .3f); var tlo = StMat("pylon tło cen", new Color(.04f, .04f, .05f), .7f);
        var cyfry = StSwiatlo("cyfry cen", new Color(1f, .55f, .1f)); var biel = StSwiatlo("napisy pylonu", new Color(.95f, .97f, 1f));
        BoxMinMax(Concrete, x - w * .5f - .15f, x + w * .5f + .15f, 0, .3f, z - d * .5f - .15f, z + d * .5f + .15f, true);   // fundament
        BoxMinMax(obud, x - w * .5f, x + w * .5f, .3f, h, z - d * .5f, z + d * .5f, true);
        BoxMinMax(StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f), x - w * .5f - .03f, x + w * .5f + .03f, h, h + .05f, z - d * .5f - .03f, z + d * .5f + .03f, false);
        var ceny = new[] { ("PB95", "6.49"), ("PB98", "7.09"), ("ON", "6.59"), ("LPG", "3.19") };
        foreach (int s in new[] { -1, 1 })
        {
            float f = z + s * d * .5f; var n = s > 0 ? Vector3.forward : Vector3.back;
            // grafika pylonu BRAXHORN z cenami (1:3) na całe lico; bez grafiki — tło, litery i cyfry z kodu
            { float pw = w * .94f, ph = pw * 3f; if (SzyldPanel("stacja_pylon", new Vector3(x, h - .25f - ph * .5f, f), n, pw, ph)) continue; }
            BoxMinMax(tlo, x - w * .45f, x + w * .45f, h * .18f, h * .78f, Mathf.Min(f, f + s * .01f), Mathf.Max(f, f + s * .01f), false);
            Napis3D("BRAXHORN", new Vector3(x, h * .84f, f), n, .19f, .04f, StSwiatlo("napis BRAXHORN złoty", new Color(1f, .72f, .18f)), obud, .16f, .12f, true);
            for (int k = 0; k < ceny.Length; k++)
            {
                float y = h * .7f - k * h * .13f;
                var r = Vector3.Cross(Vector3.up, -n).normalized;
                // nazwa paliwa od lewej krawędzi tła (≈ 0.72 m), cena od środka (≈ 0.8 m) — obie mieszczą się w tle ±0.87 m
                Napis3D(ceny[k].Item1, new Vector3(x, y, f + s * .01f) - r * w * .44f, n, .22f, .03f, biel, tlo, .16f, .12f, false);
                Napis3D(ceny[k].Item2, new Vector3(x, y, f + s * .01f) - r * w * .04f, n, .30f, .03f, cyfry, tlo, .16f, .1f, false);
            }
        }
        StDetal("pylon", "ceny literami przestrzennymi + obróbka");
    }

    // ---------------- sklep ----------------
    static void StacjaSklep()
    {
        group = "Osiedle A / stacja paliw — sklep";
        float x0 = -36.5f, x1 = -21f, z0 = -60.5f, z1 = -49f, yF = .17f, yC = 3.4f * K, yA = yC + 1.1f; const float t = .3f;
        var sciana = Pbr("stacja: okładzina sklepu grafit", "white_stucco_02", 2.5f, new Color(.24f, .24f, .26f));   // 3.11: grafit zamiast szarej bieli
        var obr = StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f); var pas = StMat("pas wiaty grafit", new Color(.16f, .17f, .19f), .5f, .3f);
        // płyty przed sklepem z krawężnikiem (fuga co 1 m × K)
        BoxMinMax(Pavers, x0 - .5f, x1 + .5f, 0, .14f, z1, -47.7f, true);
        KrawFugi(true, -47.6f, x0 - .5f, x1 + .5f);
        // ściany: tył i boki pełne, front: filary + witryny; cokół obwodowy; drzwi przesuwne (otwarte) x −29.85…−27.65
        float dA = -29.85f, dB = -27.65f, dH = 2.3f * K;
        BoxMinMax(sciana, x0, x1, 0, yA, z0, z0 + t); BoxMinMax(sciana, x0, x0 + t, 0, yA, z0 + t, z1); BoxMinMax(sciana, x1 - t, x1, 0, yA, z0 + t, z1);
        BoxMinMax(sciana, x0 + t, x1 - t, yC - .05f, yA, z1 - t, z1);                    // nadproże frontu
        BoxMinMax(Plinth, x0 - .04f, x1 + .04f, 0, .45f, z0 - .04f, z0, false); BoxMinMax(Plinth, x0 - .04f, x0, 0, .45f, z0, z1 + .04f, false); BoxMinMax(Plinth, x1, x1 + .04f, 0, .45f, z0, z1 + .04f, false);
        StDetal("sklep", "cokół kamienny");
        // attyka z obróbką (kapinos 3 cm), gzyms grafitowy pod attyką
        ObwodBez(obr, x0, x1, z0, z1, t, yA, yA + .05f, .04f);
        BoxMinMax(pas, x0 - .06f, x1 + .06f, yC + .1f, yC + .45f, z1, z1 + .06f, false);   // gzyms frontu
        BoxMinMax(StMat("membrana dachu", new Color(.35f, .36f, .37f), .2f), x0 + t, x1 - t, yC + .25f, yC + .3f, z0 + t, z1 - t, false);
        BoxMinMax(sciana, x0 + t, x1 - t, yC, yC + .25f, z0 + t, z1 - t, false, true);   // strop
        StDetal("sklep", "attyka z obróbką + gzyms frontu");
        // front: filary i witryny w ramach, cokół witryn, parapety zewnętrzne
        var gl = ShopGlass;
        foreach (var (a, b) in new[] { (x0 + t, dA - .15f), (dB + .15f, x1 - t) })
        {
            float sx = a;
            for (float px = a; px < b - .01f; px += 2.4f)
            {
                float qx = Mathf.Min(b, px + 2.4f);
                BoxMinMax(Frame, px, px + .08f, .45f, yC - .05f, z1 - .14f, z1 - .02f, false);
                BoxMinMax(gl, px + .08f, qx - .04f, .5f, yC - .1f, z1 - .1f, z1 - .08f, false);
                BoxMinMax(Frame, px, qx, yC - .1f, yC - .05f, z1 - .14f, z1 - .02f, false);
                BoxMinMax(Frame, px, qx, .45f, .5f, z1 - .14f, z1 - .02f, false);
            }
            BoxMinMax(Frame, b - .08f, b, .45f, yC - .05f, z1 - .14f, z1 - .02f, false);
            BoxMinMax(sciana, a, b, 0, .45f, z1 - .25f, z1 - .14f, true);                       // murek pod witryną
            BoxMinMax(obr, a, b, .45f, .48f, z1 - .14f, z1 + .06f, false);                       // parapet zewnętrzny
            var kol = new GameObject("kolizja"); kol.transform.SetParent(colliders, false); kol.transform.position = new Vector3((a + b) * .5f, yC * .5f, z1 - .1f); kol.AddComponent<BoxCollider>().size = new Vector3(b - a, yC, .2f);
        }
        StDetal("sklep", "witryny w ramach + parapety", 2);
        // drzwi przesuwne (rozsunięte, skrzydła przy filarach) i opaska drzwi
        BoxMinMax(Frame, dA - .15f, dA, 0, dH + .15f, z1 - .14f, z1 - .02f, false); BoxMinMax(Frame, dB, dB + .15f, 0, dH + .15f, z1 - .14f, z1 - .02f, false);
        BoxMinMax(Frame, dA - .15f, dB + .15f, dH, dH + .15f, z1 - .14f, z1 - .02f, false);
        BoxMinMax(sciana, dA - .15f, dB + .15f, dH + .15f, yC - .05f, z1 - .2f, z1 - .02f, false);
        BoxMinMax(GlassClear, dA - 1.15f, dA - .15f, .05f, dH, z1 - .26f, z1 - .24f, false); BoxMinMax(GlassClear, dB + .15f, dB + 1.15f, .05f, dH, z1 - .26f, z1 - .24f, false);
        BoxMinMax(StMat("kaseta napędu drzwi", new Color(.55f, .56f, .58f), .5f, .5f), dA - 1.2f, dB + 1.2f, dH + .15f, dH + .45f, z1 - .34f, z1 - .14f, false);
        BoxMinMax(StMat("próg aluminiowy", new Color(.6f, .61f, .63f), .5f, .7f), dA, dB, 0, yF, z1 - .25f, z1, true);   // próg w otworze drzwi (posadzka kończy się na z1 − 0.25)
        StDetal("sklep", "drzwi przesuwne z kasetą napędu + opaska + próg");
        // rury spustowe na narożnikach tyłu (z rzygaczy attyki)
        foreach (float rx in new[] { x0 + .5f, x1 - .5f })
        {
            BoxMinMax(obr, rx - .12f, rx + .12f, yA - .45f, yA - .2f, z0 - .25f, z0, false);          // lej spustowy
            Tube(obr, new Vector3(rx, yA - .45f, z0 - .12f), new Vector3(rx, .25f, z0 - .12f), .05f, 10);
            Tube(obr, new Vector3(rx, .25f, z0 - .12f), new Vector3(rx, .08f, z0 - .4f), .05f, 10);
        }
        StDetal("sklep", "leje i rury spustowe", 2);
        // ---- wnętrze ----
        var podl = Pbr("stacja: płytki sklepu", "large_grey_tiles", 1.2f, new Color(.96f, .96f, .96f));
        BoxMinMax(Concrete, x0 + t, x1 - t, 0, yF - .04f, z0 + t, z1 - .25f, true);            // płyta (lico posadzki tylko z płytek)
        BoxMinMax(podl, x0 + t, x1 - t, yF - .04f, yF, z0 + t, z1 - .25f, false);
        var listwa = StMat("listwa przypodłogowa", new Color(.3f, .3f, .31f), .4f);
        BoxMinMax(listwa, x0 + t, x1 - t, yF, yF + .08f, z0 + t, z0 + t + .015f, false); BoxMinMax(listwa, x0 + t, x0 + t + .015f, yF, yF + .08f, z0 + t, z1 - .25f, false); BoxMinMax(listwa, x1 - t - .015f, x1 - t, yF, yF + .08f, z0 + t, z1 - .25f, false);
        StDetal("sklep", "listwy przypodłogowe", 3);
        // lada z kasą (wzdłuż ściany zach.), kasjer za ladą (obsada w kroku ludzi)
        var lada = StMat("lada front", new Color(.85f, .86f, .87f), .5f); var blat = StMat("blat lady", new Color(.2f, .2f, .22f), .7f);
        BoxMinMax(lada, x0 + 2.4f, x0 + 3.2f, yF, yF + 1.1f * K * .85f, -57.5f, -51.5f, true);
        BoxMinMax(blat, x0 + 2.35f, x0 + 3.3f, yF + 1.1f * K * .85f, yF + 1.1f * K * .85f + .05f, -57.55f, -51.45f, false);
        float yb = yF + 1.1f * K * .85f + .05f;
        BoxMinMax(StMat("kasa fiskalna", new Color(.1f, .1f, .12f), .6f), x0 + 2.6f, x0 + 3.05f, yb, yb + .12f, -54.9f, -54.4f, false);
        BoxMinMax(StSwiatlo("ekran kasy", new Color(.4f, .7f, 1f)), x0 + 2.75f, x0 + 2.78f, yb + .12f, yb + .45f, -54.9f, -54.4f, false);
        BoxMinMax(StMat("terminal płatniczy", new Color(.15f, .15f, .15f), .6f), x0 + 3.1f, x0 + 3.25f, yb, yb + .1f, -53.6f, -53.35f, false);
        stacjaKasjer = new Vector3(x0 + 1.5f, yF, -54.6f);
        // regał za kasą z drobnym towarem
        Regal(x0 + t + .02f, x0 + t + .5f, -57.8f, -51.2f, yF, false);
        // chłodziarki wzdłuż tylnej ściany (szklane drzwi, podświetlenie)
        var chlo = StMat("obudowa chłodziarki", new Color(.88f, .89f, .9f), .5f, .3f); var sw = StSwiatlo("wnętrze chłodziarki", new Color(.9f, .97f, 1f));
        for (int k = 0; k < 4; k++)
        {
            float a = x0 + 4.4f + k * 2.3f, b = a + 2.2f, zb = z0 + t, zf = zb + .8f * K, h = 2.1f * K;
            BoxMinMax(chlo, a, b, yF, yF + h, zb, zf, true);
            BoxMinMax(sw, a + .08f, b - .08f, yF + .2f, yF + h - .25f, zf - .02f, zf, false);
            foreach (float px in new[] { a + .06f, (a + b) * .5f - .03f, b - .12f }) BoxMinMax(Frame, px, px + .06f, yF + .18f, yF + h - .22f, zf, zf + .03f, false);
            for (float yy = yF + .55f; yy < yF + h - .4f; yy += .42f)
                for (float px = a + .2f; px < b - .2f; px += .16f) BoxMinMax(StMat("napoje " + ((int)(px * 7f) & 3), new[] { new Color(.8f, .1f, .1f), new Color(.1f, .4f, .8f), new Color(.95f, .8f, .1f), new Color(.2f, .6f, .2f) }[(int)(px * 7f) & 3], .6f), px, px + .08f, yy, yy + .26f, zf - .25f, zf - .15f, false);
        }
        StDetal("sklep", "chłodziarki z podświetleniem", 4);
        // 2 regały środkowe z towarem
        Regal(-31.5f, -24f, -55.6f, -55.0f, yF, true); Regal(-31.5f, -24f, -53.2f, -52.6f, yF, true);
        // gniazdka (puszki na ścianie, 30 cm × K nad podłogą), kratki wentylacyjne pod stropem, oprawy sufitowe
        var gn = StMat("gniazdko białe", new Color(.95f, .95f, .95f), .5f);
        foreach (float gx in new[] { x0 + 4f, x0 + 9f, x1 - 1.2f }) { BoxMinMax(gn, gx, gx + .09f, yF + .3f * K, yF + .3f * K + .09f, z0 + t, z0 + t + .025f, false); }
        foreach (float gz in new[] { -58f, -51.5f }) BoxMinMax(gn, x1 - t - .025f, x1 - t, yF + .3f * K, yF + .3f * K + .09f, gz, gz + .09f, false);
        StDetal("sklep", "gniazdka", 5);
        var krat = StMat("kratka wentylacyjna", new Color(.7f, .71f, .72f), .4f, .5f);
        foreach (float kx in new[] { x0 + 3f, x1 - 3f })
        {
            BoxMinMax(krat, kx - .3f, kx + .3f, yC - .45f, yC - .2f, z0 + t, z0 + t + .02f, false);
            for (float yy = yC - .42f; yy < yC - .22f; yy += .04f) BoxMinMax(StMat("szczelina kratki", new Color(.05f, .05f, .05f), .1f), kx - .27f, kx + .27f, yy, yy + .015f, z0 + t + .02f, z0 + t + .025f, false);
        }
        StDetal("sklep", "kratki wentylacyjne", 2);
        var opr = StSwiatlo("oprawa sklepu", new Color(1f, .99f, .95f)); int no = 0;
        for (float ox = x0 + 2.5f; ox < x1 - 1.5f; ox += 3.2f) foreach (float oz in new[] { -57.5f, -52.5f }) { BoxMinMax(StMat("ramka oprawy", new Color(.5f, .5f, .52f), .5f, .6f), ox - .64f, ox + .64f, yC - .03f, yC, oz - .34f, oz + .34f, false, true); BoxMinMax(opr, ox - .6f, ox + .6f, yC - .035f, yC - .03f, oz - .3f, oz + .3f, false, true); no++; }
        StDetal("sklep", "oprawy sufitowe", no);
        // gaśnica na wieszaku przy drzwiach + znak gaśnicy, znak WYJŚCIE nad drzwiami (zielony, podświetlany)
        float gzx = x1 - t - .02f;
        BoxMinMax(Steel, gzx - .03f, gzx, yF + .8f, yF + .95f, -50.5f, -50.3f, false);
        Cylinder(StMat("gaśnica czerwona", new Color(.75f, .05f, .05f), .7f), new Vector3(gzx - .14f, yF + .55f, -50.4f), .08f * K, .5f * K, 14, true, false);
        BoxMinMax(StMat("znak gaśnicy", new Color(.8f, .05f, .05f), .5f), gzx - .01f, gzx, yF + 1.55f, yF + 1.85f, -50.55f, -50.25f, false);
        var ziel = StSwiatlo("znak wyjścia", new Color(.05f, .7f, .25f));
        BoxMinMax(ziel, (dA + dB) * .5f - .4f, (dA + dB) * .5f + .4f, dH + .55f, dH + .8f, z1 - .38f, z1 - .34f, false);
        Napis3D("WYJŚCIE", new Vector3((dA + dB) * .5f, dH + .6f, z1 - .38f), Vector3.back, .13f, .01f, StSwiatlo("napis wyjścia", new Color(1f, 1f, 1f)), ziel, .16f, .12f, true);
        StDetal("sklep", "gaśnica + znak, znak WYJŚCIE");
        // szyld nad wejściem
        Napis3D("SKLEP", new Vector3((dA + dB) * .5f, yC + .15f, z1 + .06f), Vector3.forward, .28f, .05f, StSwiatlo("napis wiaty", new Color(.95f, .97f, 1f)), pas, .15f, .14f, true);
    }
    static Vector3 stacjaKasjer;
    static void Regal(float x0, float x1, float z0, float z1, float yF, bool obustronny)
    {
        var st = StMat("regał stal", new Color(.75f, .76f, .78f), .5f, .5f);
        float h = 1.6f * K;
        BoxMinMax(st, x0, x1, yF, yF + .1f, z0, z1, true);
        BoxMinMax(st, x0, x1, yF + .1f, yF + h, (z0 + z1) * .5f - .02f, (z0 + z1) * .5f + .02f, false);
        var kol = new[] { new Color(.85f, .3f, .1f), new Color(.2f, .5f, .8f), new Color(.9f, .85f, .2f), new Color(.3f, .65f, .3f), new Color(.75f, .2f, .5f) };
        // 3.11 („żadnych kolorowych klocków”): z paczką towaru — prawdziwe produkty BRAXHORN na półkach (jak w markecie)
        if (BraxOsiedleTowar.Jest)
        {
            var tw = new GameObject("Stacja: towar (paczka miasto_sklepy)").transform; tw.SetParent(Group(group), true);
            var rng = new System.Random(4100 + (int)(x0 * 10f)); int lv = 0;
            for (float y = yF + .1f; y < yF + h - .2f; y += .42f, lv++)
            {
                BoxMinMax(st, x0, x1, y, y + .025f, z0, z1, false);
                BraxOsiedleTowar.Polka("sm_g" + ((lv * 2 + (int)Mathf.Abs(x0)) % 9), tw, new Vector3(x0 + .05f, y + .025f, z0 + .03f), Vector3.right, Vector3.forward, x1 - x0 - .1f, rng, .36f);
                if (obustronny) BraxOsiedleTowar.Polka("sm_g" + ((lv * 2 + 1 + (int)Mathf.Abs(x0)) % 9), tw, new Vector3(x0 + .05f, y + .025f, z1 - .03f), Vector3.right, Vector3.back, x1 - x0 - .1f, rng, .36f);
            }
            BraxOsiedleTowar.Zakoncz(tw, "sklep stacji");
            return;
        }
        for (float y = yF + .1f; y < yF + h - .2f; y += .42f)
        {
            BoxMinMax(st, x0, x1, y, y + .025f, z0, z1, false);
            int i = 0;
            for (float x = x0 + .05f; x < x1 - .2f; x += .19f, i++)
            {
                var m = StMat("towar " + ((i + (int)(y * 10)) % 5), kol[(i + (int)(y * 10)) % 5], .45f);
                BoxMinMax(m, x, x + .15f, y + .025f, y + .025f + .2f + (i % 3) * .04f, z0 + .03f, (z0 + z1) * .5f - .03f, false);
                if (obustronny) BoxMinMax(m, x, x + .15f, y + .025f, y + .025f + .18f + (i % 2) * .05f, (z0 + z1) * .5f + .03f, z1 - .03f, false);
            }
        }
    }

    // ---------------- myjnia ----------------
    // Ściana z paneli z fugą: panele 1.2 m × K co szczelina 12 mm × K, w szczelinie widać ciemny podkład cofnięty o 4 cm (bez nakładania lic).
    static void PanelSciana(bool wzdluzX, float c, float a0, float a1, float y0, float y1, float grub, Material panel)
    {
        var pod = StMat("podkład paneli", new Color(.12f, .12f, .13f), .2f);
        float sz = .012f * K, w = 1.2f * K;
        // podkład krótszy o 5 mm na końcach i u góry — żadne jego lico nie leży w płaszczyźnie lica panelu
        if (wzdluzX) BoxMinMax(pod, a0 + .005f, a1 - .005f, y0, y1 - .005f, c - grub * .5f + .04f, c + grub * .5f - .04f, true); else BoxMinMax(pod, c - grub * .5f + .04f, c + grub * .5f - .04f, y0, y1 - .005f, a0 + .005f, a1 - .005f, true);
        for (float a = a0; a < a1 - .01f; a += w)
        {
            float b = Mathf.Min(a1, a + w - sz);
            if (wzdluzX) BoxMinMax(panel, a, b, y0, y1, c - grub * .5f, c + grub * .5f, false); else BoxMinMax(panel, c - grub * .5f, c + grub * .5f, y0, y1, a, b, false);
            stPanele++;
        }
    }
    static void MyjniaHala()
    {
        group = "Osiedle A / myjnia — hala";
        float x0 = -4f, x1 = 5.5f, z0 = -62f, z1 = -48.5f, yH = 4.2f * K, yA = yH + .6f;
        var panel = StMat("panel ściany myjni granat BRAXHORN", new Color(.06f, .15f, .30f), .62f, .3f); var obr = StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f);
        var nieb = StMat("konstrukcja portalu", new Color(.08f, .3f, .62f), .55f, .4f); var szcz = StMat("szczotka myjni", new Color(.2f, .45f, .85f), .2f);
        PanelSciana(false, x0 + .15f, z0, z1, 0, yA, .3f, panel); PanelSciana(false, x1 - .15f, z0, z1, 0, yA, .3f, panel); PanelSciana(true, z0 + .15f, x0 + .3f, x1 - .3f, 0, yA, .3f, panel);
        BoxMinMax(panel, x0 + .3f, x1 - .3f, yH, yA, z1 - .3f, z1, true, true);                        // nadproże bramy (od góry otworu — bez szczeliny)
        BoxMinMax(panel, x0 + .3f, x1 - .3f, yH, yH + .2f, z0 + .3f, z1 - .3f, false, true);            // strop
        ObwodBez(obr, x0, x1, z0, z1, .3f, yA, yA + .05f, .04f);
        BoxMinMax(Plinth, x0 - .04f, x0, 0, .4f, z0, z1, false); BoxMinMax(Plinth, x1, x1 + .04f, 0, .4f, z0, z1, false); BoxMinMax(Plinth, x0, x1, 0, .4f, z0 - .04f, z0, false);
        StDetal("myjnia", "panele z fugą, attyka z obróbką, cokół");
        // brama rolowana podniesiona (skrzynka nawoju nad otworem), prowadnice
        BoxMinMax(StMat("skrzynka bramy", new Color(.7f, .72f, .74f), .5f, .4f), x0 + .3f, x1 - .3f, yH - .5f, yH, z1 - .75f, z1 - .3f, false);
        foreach (float gx in new[] { x0 + .3f, x1 - .38f }) BoxMinMax(Steel, gx, gx + .08f, 0, yH - .5f, z1 - .42f, z1 - .32f, false);
        // portal na szynach, 3 szczotki (2 pionowe, 1 pozioma)
        foreach (float rx in new[] { x0 + .9f, x1 - .9f }) BoxMinMax(Steel, rx - .06f, rx + .06f, YS - .01f, YS + .05f, z0 + .6f, z1 - 1f, false);
        float pz = -55.5f, pw = .25f;
        // 3.11 15:02 („myjnia ma być funkcjonalna”): portal i szczotki jako osobne obiekty — BraxMyjnia rusza je, gdy auto gracza stoi w hali
        {
            float cx = (x0 + x1) * .5f;
            var pr = new GameObject("Myjnia: portal (ruchomy)").transform; pr.SetParent(Group(group), false); pr.position = new Vector3(cx, 0f, z1 - 2.2f);
            Transform Bryla(PrimitiveType t, string n, Material m, Vector3 lp, Vector3 sc, Vector3 eul)
            {
                var g = GameObject.CreatePrimitive(t); g.name = n; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
                g.transform.SetParent(pr, false); g.transform.localPosition = lp; g.transform.localRotation = Quaternion.Euler(eul); g.transform.localScale = sc;
                var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return g.transform;
            }
            float hN = yH - .4f - YS;
            Bryla(PrimitiveType.Cube, "noga lewa", nieb, new Vector3(x0 + .9f - cx, YS + hN * .5f, 0f), new Vector3(.4f, hN, pw * 2f), Vector3.zero);
            Bryla(PrimitiveType.Cube, "noga prawa", nieb, new Vector3(x1 - .9f - cx, YS + hN * .5f, 0f), new Vector3(.4f, hN, pw * 2f), Vector3.zero);
            Bryla(PrimitiveType.Cube, "belka", nieb, new Vector3(0f, yH - .6f, 0f), new Vector3(x1 - x0 - 1.4f, .4f, pw * 2f), Vector3.zero);
            Bryla(PrimitiveType.Cube, "lampki", StSwiatlo("lampki portalu", new Color(.2f, 1f, .4f)), new Vector3(0f, yH - .725f, pw + .01f), new Vector3(x1 - x0 - 2.4f, .05f, .02f), Vector3.zero);
            float hS = 2.6f * K;
            var p1 = Bryla(PrimitiveType.Cylinder, "szczotka pionowa lewa", szcz, new Vector3(x0 + 1.8f - cx, YS + .1f + hS * .5f, .9f), new Vector3(1.1f, hS * .5f, 1.1f), Vector3.zero);
            var p2 = Bryla(PrimitiveType.Cylinder, "szczotka pionowa prawa", szcz, new Vector3(x1 - 1.8f - cx, YS + .1f + hS * .5f, .9f), new Vector3(1.1f, hS * .5f, 1.1f), Vector3.zero);
            var po = Bryla(PrimitiveType.Cylinder, "szczotka pozioma", szcz, new Vector3(0f, yH - 1.4f, -.9f), new Vector3(1f, (x1 - x0 - 2.8f) * .5f, 1f), new Vector3(0f, 0f, 90f));
            var my = pr.gameObject.AddComponent<BraxMyjnia>();
            my.portal = pr; my.pion = new[] { p1, p2 }; my.poziom = po;
            my.strefaSrodek = new Vector3(cx, YS, (z0 + z1) * .5f); my.strefaRozmiar = new Vector3(x1 - x0 - .6f, 3f, z1 - z0 - 1f);
            my.zStart = z1 - 2.2f; my.zKoniec = z0 + 2.2f;
            Debug.Log("BRAXHORN_MYJNIA portal ruchomy: szyny z " + my.zStart.ToString("F1") + "…" + my.zKoniec.ToString("F1") + ", szczotki 2 pionowe + 1 pozioma, start po wjeździe auta gracza");
        }
        StDetal("myjnia", "portal na szynach + 3 szczotki");
        OdplywLiniowyBeton(-56.5f + 4f, z0 + .5f, z1 - .5f, (x0 + x1) * .5f);
        // panel sterowania z terminalem przy wjeździe (od strony kierowcy — lewa strona wjazdu)
        BoxMinMax(Concrete, x0 + .45f, x0 + 1.15f, YS, YS + .2f, z1 + .15f, z1 + .85f, true);   // na betonie przed bramą (lewa strona wjazdu)
        BoxMinMax(StMat("terminal myjni", new Color(.15f, .2f, .3f), .6f, .3f), x0 + .6f, x0 + 1.0f, YS + .2f, 1.5f * K, z1 + .3f, z1 + .7f, true);
        BoxMinMax(StSwiatlo("ekran terminala", new Color(.4f, .8f, 1f)), x0 + 1.0f, x0 + 1.02f, 1.1f * K, 1.35f * K, z1 + .35f, z1 + .65f, false);
        var pk = new GameObject("BRAXHORN punkt interakcji: mycie (myjnia automatyczna)"); pk.transform.SetParent(Group(group), false); pk.transform.position = new Vector3((x0 + x1) * .5f, YS, z1 + 2f); stPunkty++;
        if (!SzyldPanel("myjnia_szyld", new Vector3((x0 + x1) * .5f, yH + .3f, z1 + .01f), Vector3.forward, 1.68f, .56f))
            Napis3D("MYJNIA BRAXHORN", new Vector3((x0 + x1) * .5f, yH + .12f, z1 + .01f), Vector3.forward, .3f, .05f, StSwiatlo("napis myjni turkus", new Color(.2f, .85f, 1f)), nieb, .15f, .14f, true);
        // tablica z cennikiem przy wjeździe (2:3) na dwóch słupkach, przodem do wjeżdżających (3 m przed bramą, lewa strona)
        if (SzyldObraz("myjnia_tablica"))
        {
            float tx = x0 + .9f, tz = z1 + 3f, tw = 1.1f * K, th = tw * 1.5f, ty = YS + .5f * K;
            foreach (float sx in new[] { -1f, 1f }) Cylinder(Steel, new Vector3(tx + sx * (tw * .5f - .06f), YS, tz - .05f), .04f, ty + th - YS, 10, true, false);
            BoxMinMax(StMat("rama tablicy myjni", new Color(.08f, .1f, .14f), .5f, .5f), tx - tw * .5f - .04f, tx + tw * .5f + .04f, ty - .04f, ty + th + .04f, tz - .09f, tz - .01f, false);
            SzyldPanel("myjnia_tablica", new Vector3(tx, ty + th * .5f, tz - .01f), Vector3.forward, tw, th);
            Collider(new Vector3(tx, YS + (ty + th - YS) * .5f, tz - .05f), new Vector3(tw + .1f, ty + th - YS, .2f));
        }
    }
    static void OdplywLiniowyBeton(float unused, float a0, float a1, float c)
    {
        float w = .15f * K;
        var korpus = StMat("korytko polimerobeton", new Color(.30f, .30f, .31f), .2f); var ruszt = StMat("ruszt stalowy ocynk", new Color(.55f, .56f, .57f), .5f, .7f);
        var ciemno = StMat("wnętrze korytka", new Color(.04f, .04f, .04f), .1f);
        WytnijZGrupy((gr, m) => gr.StartsWith("Osiedle A / myjnia") || gr == GrupaStacjiAsf, MinMax(c - w * .5f - .03f, c + w * .5f + .03f, YS - .07f, YS + .03f, a0, a1));
        BoxMinMax(korpus, c - w * .5f - .03f, c - w * .5f, YS - .12f, YS, a0, a1, false); BoxMinMax(korpus, c + w * .5f, c + w * .5f + .03f, YS - .12f, YS, a0, a1, false);
        BoxMinMax(ciemno, c - w * .5f, c + w * .5f, YS - .14f, YS - .12f, a0, a1, false);
        for (float z = a0 + .03f; z < a1 - .03f; z += .05f) BoxMinMax(ruszt, c - w * .5f, c + w * .5f, YS - .02f, YS, z, z + .012f, false);
        stOdplywy++;
    }
    static void MyjniaSamoobslugowa()
    {
        group = "Osiedle A / myjnia — stanowiska";
        float x0 = -19f, x1 = -6f, z0 = -60.5f, z1 = -48.5f, yH = 3.6f * K;
        var panel = StMat("panel ściany myjni granat BRAXHORN", new Color(.06f, .15f, .30f), .62f, .3f); var obr = StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f);
        var nieb = StMat("konstrukcja portalu", new Color(.08f, .3f, .62f), .55f, .4f);
        // zadaszenie na słupach stalowych, przegrody (tył i między stanowiskami) z paneli z fugą
        foreach (float sx in new[] { x0 + .15f, (x0 + x1) * .5f, x1 - .15f })
            foreach (float sz in new[] { z0 + .2f, z1 - .2f }) BoxMinMax(nieb, sx - .12f, sx + .12f, YS, yH, sz - .12f, sz + .12f, true);
        BoxMinMax(panel, x0, x1, yH, yH + .5f, z0, z1, true, true);
        ObwodBez(obr, x0, x1, z0, z1, .1f, yH + .5f, yH + .55f, .03f);
        PanelSciana(true, z0 + .15f, x0 + .3f, x1 - .3f, YS, 2.4f * K, .12f, panel);
        PanelSciana(false, (x0 + x1) * .5f, z0 + .35f, z1 - 2f, YS, 2.2f * K, .1f, panel);
        // rynna z rurą spustową od strony tyłu
        BoxMinMax(obr, x0, x1, yH + .25f, yH + .4f, z0 - .18f, z0, false);
        Tube(obr, new Vector3(x0 + .3f, yH + .3f, z0 - .1f), new Vector3(x0 + .3f, .2f, z0 - .1f), .05f, 10);
        StDetal("myjnia", "zadaszenie z rynną i rurą spustową, przegrody z paneli");
        for (int k = 0; k < 2; k++)
        {
            float a = x0 + k * (x1 - x0) * .5f, b = a + (x1 - x0) * .5f, cx = (a + b) * .5f;
            OdplywLiniowyBeton(0f, z0 + .6f, z1 - .6f, cx);
            // wysięgnik obrotowy pod dachem z wężem i lancą
            Cylinder(Steel, new Vector3(cx, yH - .5f, -54.5f), .08f, .5f, 12, true, false);
            Tube(Steel, new Vector3(cx, yH - .45f, -54.5f), new Vector3(cx + 2.2f, yH - .55f, -54.5f), .05f, 10);
            Tube(StMat("wąż myjki", new Color(.05f, .05f, .05f), .4f), new Vector3(cx + 2.2f, yH - .55f, -54.5f), new Vector3(cx + 2.6f, 1.1f, -53.2f), .025f, 8);
            Tube(StMat("lanca", new Color(.6f, .62f, .65f), .6f, .7f), new Vector3(cx + 2.6f, 1.1f, -53.2f), new Vector3(cx + 2.65f, .55f, -52.4f), .02f, 8);
            // panel z wrzutnikiem i wybierakiem programów przy przegrodzie tylnej
            BoxMinMax(StMat("panel stanowiska", new Color(.85f, .55f, .1f), .55f, .3f), cx - 2.4f, cx - 1.6f, YS, 1.7f * K, z0 + .3f, z0 + .7f, true);
            BoxMinMax(StSwiatlo("ekran stanowiska", new Color(.4f, .8f, 1f)), cx - 2.3f, cx - 1.7f, 1.3f * K, 1.55f * K, z0 + .7f, z0 + .72f, false);
            for (int p = 0; p < 5; p++) BoxMinMax(StMat("przycisk programu", new Color(.1f, .1f, .12f), .6f), cx - 2.3f + p * .12f, cx - 2.22f + p * .12f, 1.05f * K, 1.12f * K, z0 + .7f, z0 + .73f, false);
            var pk = new GameObject("BRAXHORN punkt interakcji: mycie (stanowisko samoobsługowe " + (k + 1) + ")"); pk.transform.SetParent(Group(group), false); pk.transform.position = new Vector3(cx, YS, -54f); stPunkty++;
        }
        StDetal("myjnia", "wysięgniki z lancą, panele z wrzutnikiem", 2);
    }
    static void MyjniaOdkurzacze()
    {
        group = "Osiedle A / myjnia — odkurzacze";
        var obud = StMat("odkurzacz obudowa", new Color(.85f, .55f, .1f), .55f, .3f); var waz = StMat("wąż odkurzacza", new Color(.2f, .2f, .22f), .4f);
        foreach (float z in new[] { -58.6f, -51.9f })
        {
            WytnijAsfalt(MinMax(11.6f, 12.8f, YS - .05f, YS + .05f, z - .5f, z + .5f));
            BoxMinMax(Concrete, 11.6f, 12.8f, 0f, YS + .15f, z - .5f, z + .5f, true);   // cokół w wycięciu asfaltu
            BoxMinMax(obud, 11.8f, 12.6f, YS + .15f, YS + 1.3f * K, z - .35f, z + .35f, true);
            Tube(Steel, new Vector3(12.2f, YS + 1.3f * K, z), new Vector3(12.2f, 2.6f * K, z), .05f, 10);
            Tube(Steel, new Vector3(12.2f, 2.6f * K, z), new Vector3(10.6f, 2.6f * K, z), .05f, 10);
            Tube(waz, new Vector3(10.6f, 2.6f * K, z), new Vector3(10.2f, 1.0f, z + .4f), .04f, 8);
            Tube(waz, new Vector3(10.2f, 1.0f, z + .4f), new Vector3(11.7f, 1.2f, z + .3f), .04f, 8);
        }
        StDetal("myjnia", "odkurzacze z wysięgnikiem", 2);
    }
    static void StacjaOgrodzenie()
    {
        group = "Osiedle A / stacja paliw — ogrodzenie";
        // murek z bloczków z fugą (bloczki 40 cm × K, fuga 1 cm) i czapą z kapinosem — tył i bok zachodni
        var blok = Pbr("stacja: bloczek betonowy", "concrete_pavement_02", 1.2f, new Color(.72f, .72f, .7f)); var fug = StMat("fuga murku", new Color(.35f, .35f, .34f), .1f);
        void Murek(bool wzdluzX, float c, float a0, float a1)
        {
            float h = 1.2f * K, b = .4f * K;
            if (wzdluzX) BoxMinMax(fug, a0, a1, 0, h, c - .1f, c + .1f, true); else BoxMinMax(fug, c - .1f, c + .1f, 0, h, a0, a1, true);
            int rz = 0;
            for (float y = 0; y < h - .01f; y += .2f * K, rz++)
                for (float a = a0 + (rz % 2) * b * .5f; a < a1 - .01f; a += b)
                {
                    float s = Mathf.Max(a0, a), e = Mathf.Min(a1, a + b - .01f), y1 = Mathf.Min(h, y + .2f * K - .01f);
                    if (e - s < .02f) continue;
                    if (wzdluzX) BoxMinMax(blok, s, e, y, y1, c - .105f, c + .105f, false); else BoxMinMax(blok, c - .105f, c + .105f, y, y1, s, e, false);
                }
            if (wzdluzX) BoxMinMax(Concrete, a0 - .03f, a1 + .03f, h, h + .08f, c - .16f, c + .16f, false); else BoxMinMax(Concrete, c - .16f, c + .16f, h, h + .08f, a0 - .03f, a1 + .03f, false);
        }
        Murek(true, SZ0 + .3f, SX0, SX1); Murek(false, SX0 + .3f, SZ0 + .3f, -61f);
        StDetal("ogrodzenie", "murek z bloczków z fugą + czapa");
    }
    static void StacjaPunktyPodrozy()
    {
        var w = UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(q => q.area == "estate");
        if (!w) return;
        var wejscie = new Vector3(-28.75f, .2f, -48.3f);   // płyty przed drzwiami sklepu (lico 0.14)
        var vf = (w.points ?? Array.Empty<BraxCityPoint>()).FirstOrDefault(p => p != null && p.id == "villa_front");
        var nowe = new List<BraxCityPoint> { new BraxCityPoint { id = "myjnia", label = "Myjnia (przy stacji paliw)", position = new Vector3(.75f, .1f, -45.5f) } };
        if (vf != null) { vf.label = "Stacja paliw (Poznańska)"; vf.position = wejscie; }
        else nowe.Add(new BraxCityPoint { id = "stacja_paliw", label = "Stacja paliw (Poznańska)", position = wejscie });
        w.points = (w.points ?? Array.Empty<BraxCityPoint>()).Concat(nowe).ToArray();
    }
    // kasjer: wołane po kroku ludzi (Rocketbox wczytany w LudzieNaOsiedlu)
    static void StacjaKasjer()
    {
        if (!StacjaWlaczona || stacjaKasjer == Vector3.zero) return;
        if (rbPostacie == null || rbPostacie.Count == 0) { Debug.Log("BRAXHORN_STACJA kasjer=0 (brak paczki Rocketbox)"); return; }
        var m = Obsada("stacja paliw — kasa", RolaObsluga, "kasjer", stacjaKasjer, 90f, new System.Random(2121), new[] { "work_mid", "idle_neutral_01", "idle_look_around_01" }, stacjaKasjer.y);
        Debug.Log("BRAXHORN_STACJA kasjer=" + (m ? 1 : 0) + " pozycja=" + stacjaKasjer.ToString("F2"));
    }
}

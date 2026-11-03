using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// SALON SAMOCHODOWY (runda 5; właściciel 01:02 „na osiedlu musisz już teraz zbudować”, 01:21 „zajebisty, premium, cały
// przeszklony, PIĘTROWY, schody eleganckie i RUCHOME”). Strefa motoryzacyjna wzdłuż B1: SALON | stacja (E21) | warsztat (E32).
// Działka: narożnik B1 × Wiśniowa (N1), x od chodnika Wiśniowej (−50.95) do stacji (−37.05), z −64 … chodnik B1 (−26.45).
// Rzut: scratchpad/plan/SALON_OSIEDLE_RZUT.png.
//  • ZWALONE: 2 garaże Codexa (x −46.5…−34.7, z −43.6…−34.2; połówka po cięciu stacji też) i wszystko w obrysie (y > 1 cm);
//    korony drzew Wiśniowej (x ≤ −49.3, y 2.5–7.8) NIE są cięte — bryła od x −49.1, pas zachodni cięty tylko do 2.3 m;
//  • BRYŁA 11.9 × 21 m, 2 kondygnacje × 4.2 m × K: szkło na całą wysokość od Wiśniowej, placu i stacji (słupki co 1.6 m,
//    rygle, gzyms międzykondygnacyjny, gzyms dachu, attyka z obróbką i literami), ściana zaplecza z okładziną z fugą, cokół,
//    rury spustowe do ziemi; JEDNO wejście dla klientów (drzwi przesuwne, daszek szklany na cięgnach), brama aut przeszklona
//    3.5 × 2.6 m × K (ZAMKNIĘTA, nie wejście), drzwi zaplecza/ewakuacyjne (ZAMKNIĘTE);
//  • PARTER: pustka na 2 kondygnacje od fasady placu do antresoli (z −47), 2 auta na obrotowych talerzach (BraxObrotowyPodest),
//    przyjęcie serwisu pod antresolą; schody reprezentacyjne jednobiegowe (policzki stalowe, stopnie z kamienia z noskiem,
//    LED pod noskami, szkło w zaciskach, pochwyty inox) i obok schody ruchome w górę 35° (BraxSchodyRuchome: stopnie ruchome
//    z żółtymi krawędziami, grzebienie, płyty ryflowane, osłony inox, szkło, poręcze przewijane UV, pasażer NPC, gracz
//    przesuwany, dźwięk napędu);
//  • ANTRESOLA: 2 auta premium na podestach za szklaną balustradą (widoczne z parteru), 2 stanowiska sprzedawców, przeszklone
//    biuro kierownika, bar kawowy z hokerami i strefą klienta;
//  • PLAC EKSPOZYCYJNY: kostka z fugą, obrzeża granitowe z fugą co 1 m × K, odwodnienie liniowe z rusztem, 3 auta, tablice cen
//    (0 zł — EKONOMIA: miejsce na cenę), słupki z odblaskiem, 2 maszty oświetlenia, totem z emblematem (bez marek aut);
//    narożnik po łuku krawężnika (E3b) — kostka tylko w ćwiartce r = R − 2.95 m, bez trawy pod autami i tablicami;
//  • ZAPLECZE: utwardzenie z kostki, 2 kontenery na odpady, murek z bloczków z fugą i czapą.
// Auta: SalonAuta (z Auta(), szablony etapu ruchu); obsada NPC: SalonObsada (po kroku ludzi). Wszystko × K, bez nakładek.
public static partial class BraxClaudeOsiedle
{
    static readonly bool SalonWlaczony = true;
    const float SAX1 = -37.05f, SAZ0 = -64f;                                   // działka (x0 i z1 z ulic po poszerzeniu)
    const float SBX0 = -49.1f, SBX1 = -37.2f, SBZ0 = -58.6f, SBZ1 = -37.6f;    // bryła showroomu (2 kondygnacje)
    const float SYF = .17f;                                                    // posadzka
    static float SAX0 { get { var n1 = ulice?.FirstOrDefault(q => q.id == "N1"); return n1 != null ? n1.c + n1.HalfPos + .02f : -50.95f; } }
    static float SAZ1 => Mathf.Min(-25.5f, KrawedzieB1().zChod - .02f);
    // brama aut i drzwi klientów w fasadzie północnej
    static float SGX0 => -48.7f; static float SGX1 => SGX0 + 3.5f * K; static float SGH => 2.6f * K;
    static float SDX0 => SGX1 + .3f; static float SDX1 => SDX0 + 2.2f * K; static float SDH => 2.3f * K;
    // miejsca aut na placu
    static readonly float[] SalonPlacX = { -48.4f, -44.4f, -40.4f };
    const float SalonPlacZ = -33.6f;
    static Vector3 salonNarSrodek; static float salonNarR;                    // ćwiartka kostki na narożniku (R = 0: narożnik prosty)
    static int saDetale; static readonly List<string> saDetaleOpis = new List<string>();
    static void SaDetal(string obiekt, string co, int n = 1) { saDetale += n; saDetaleOpis.Add(obiekt + ":" + co + (n > 1 ? "×" + n : "")); }
    static readonly List<(Vector3 p, float yaw, string kto)> salonObsada = new List<(Vector3, float, string)>();
    static Material SaMat(string key, Color c, float smooth = .3f, float metal = 0f) => Plain("salon: " + key, c, smooth, metal);

    // ---------------- krok główny ----------------
    static void SalonSamochodowy(Transform estate, Transform physics)
    {
        if (!SalonWlaczony) { Debug.Log("BRAXHORN_SALON wyłączony flagą (SalonWlaczony=false)"); return; }
        saDetale = 0; saDetaleOpis.Clear(); salonObsada.Clear();
        var g0 = group;
        // narożnik: łuk krawężnika B1 × Wiśniowa (E3b), jeśli jest
        salonNarR = 0f; salonNarSrodek = Vector3.zero;
        foreach (var kv in lukNaroznika)
        {
            var w = kv.Key.w; if (kv.Key.sx != 1 || kv.Key.sz != -1 || Mathf.Abs(w.x + 58f) > .5f || Mathf.Abs(w.z + 19f) > .5f) continue;
            float R = kv.Value; salonNarSrodek = new Vector3(w.x + w.uz.cw + R, 0f, w.z - w.ux.cw - R); salonNarR = Mathf.Max(0f, R - 2.95f);
        }
        string uklad = KontrolaUkladuSalonu();
        if (uklad != null) Debug.LogError("BRAXHORN_SALON układ DO POPRAWKI: " + uklad);
        int zwalone = SalonZwal(estate, physics);
        Guard("salon: plac ekspozycyjny (kostka, obrzeża, odwodnienie, słupki, tablice cen, maszty, totem)", SalonPlac);
        Guard("salon: bryła showroomu (fasady szklane, brama aut, drzwi, attyka, rury spustowe)", SalonBryla);
        Guard("salon: wnętrze (podesty, stanowiska, biuro, przyjęcie serwisu, strefa klienta, sufit)", SalonWnetrze);
        Guard("salon: zaplecze (utwardzenie, kontenery, murek)", SalonZaplecze);
        Guard("salon: punkt podróży", SalonPunkt);
        group = g0;
        Physics.SyncTransforms();
        Debug.Log("BRAXHORN_SALON działka x " + SAX0.ToString("F2") + "…" + SAX1 + " z " + SAZ0 + "…" + SAZ1.ToString("F2") + " zwalone=" + zwalone + " showroom=" + (SBX1 - SBX0).ToString("F1") + "×" + (SBZ1 - SBZ0).ToString("F1") +
                  " m kondygnacje=2 (4.2 m × K = " + (4.2f * K).ToString("F2") + ") wys_do_attyki=" + (SYR + 1.15f).ToString("F2") + " talerze_obrotowe=" + SalonTalerze.Length + " auta_antresola=" + SalonAntresolaAuta.Length + " schody_stałe=1 schody_ruchome=1 plac_auta=" + SalonPlacX.Length + " tablice_cen=" + SalonPlacX.Length + " (0 zł) wejscia_klientow=1 brama_aut=" + (3.5f * K).ToString("F2") + "×" + SGH.ToString("F2") +
                  " (zamknięta) drzwi_zaplecza=1 (zamknięte) narożnik_łuk_R=" + (salonNarR > 0 ? (salonNarR + 2.95f).ToString("F2") + " kostka_r=" + salonNarR.ToString("F2") : "brak (narożnik prosty)") + " układ=" + (uklad ?? "ok"));
        Debug.Log("BRAXHORN_DETALE obiekt=salon razem=" + saDetale + " [" + string.Join(", ", saDetaleOpis) + "]");
        rep.notes.Add("salon samochodowy (runda 5): narożnik B1 × Wiśniowa, showroom " + (SBX1 - SBX0).ToString("F1") + " × " + (SBZ1 - SBZ0).ToString("F1") + " m, plac z 3 autami, detale " + saDetale);
    }

    // Test układu (te same liczby co budowa): w działce, bez nachodzenia, auta i tablice poza zielenią narożnika i poza chodnikiem.
    static string KontrolaUkladuSalonu()
    {
        if (SBX0 < SAX0 + 1.5f) return "bryła przy chodniku Wiśniowej (< 1.5 m: korony drzew x ≤ −49.3)";
        if (SBX1 > SAX1 - .1f || SBZ1 > SAZ1 - 8f || SBZ0 < SAZ0 + 4f) return "bryła poza działką";
        if (SDX1 > SSX0 + .3f) return "drzwi klientów za schodami";
        float xi0 = SBX0 + .15f, xi1 = SBX1 - .15f, zi0 = SBZ0 + .3f, zi1 = SBZ1 - .15f;
        foreach (var (tx, tz) in SalonTalerze)
        {
            if (tx - STalerzR < xi0 + .05f || tz + STalerzR > zi1 - 1f || tz - STalerzR < zi0 + 1f) return "talerz poza wnętrzem";
            if (tx + STalerzR > SSX0 - .9f) return "przejście talerz | schody < 0.9 m";
        }
        if (SalonTalerze[0].z - STalerzR - (SalonTalerze[1].z + STalerzR) < 2f) return "talerze za blisko (< 2 m)";
        if (SSX1 > SEX0 - .1f || SEX1 > xi1 - .02f) return "schody stałe / ruchome nachodzą";
        if (SSZb - 23 * .35f < SAntZ - .01f || SSZb - 23 * .35f > SAntZ + .01f) return "schody stałe nie kończą się na krawędzi antresoli";
        if (SEZt > SAntZ - .5f || SEZt - .95f < -52f) return "górna płyta schodów ruchomych poza antresolą";
        if (SSZb > zi1 - 1f || SEZb > zi1 - 1f) return "przed schodami < 1 m od fasady";
        foreach (var (ax, az) in SalonAntresolaAuta) if (ax - 1.6f < xi0 || az + 3.3f > SAntZ - .5f || ax + 1.6f > -42.05f) return "auto na antresoli koliduje (balustrada, krzesła)";
        foreach (float x in SalonPlacX)
        {
            float z0 = SalonPlacZ - 3.2f, z1 = SalonPlacZ + 3.2f, a = x - 1.25f, b = x + 1.25f;
            if (a < SAX0 + .3f || b > SAX1 - .3f || z0 < SBZ1 + .6f || z1 > SAZ1 - 1f) return "auto na placu poza działką (x " + x + ")";
            if (salonNarR > 0f)
                foreach (var (px, pz) in new[] { (a, z1), (b, z1), (a, z0) })
                    if (px < salonNarSrodek.x && pz > salonNarSrodek.z && new Vector2(px - salonNarSrodek.x, pz - salonNarSrodek.z).magnitude > salonNarR - .1f) return "auto x " + x + " poza kostką narożnika";
        }
        if (SGX0 < SBX0 + .3f) return "brama aut przy narożniku bryły";
        return null;
    }

    // ---------------- zwalenie ----------------
    static int SalonZwal(Transform estate, Transform physics)
    {
        group = "Osiedle A / salon";
        float z1 = SAZ1 - .03f;
        var vols = new List<Bounds> { MinMax(SBX0 - .05f, SAX1, .01f, 14f, SAZ0, z1), MinMax(SAX0 + .1f, SBX0 - .05f, .01f, 2.3f, SAZ0, z1) };
        int przed = rep.oldFenceTrianglesRemoved;
        if (estate) CutVolumes(estate, physics, vols);
        int troj = rep.oldFenceTrianglesRemoved - przed, kol = 0, gen = 0;
        bool W(Bounds b) => vols.Any(v => { var e = v; e.Expand(.6f); return e.Contains(b.min) && e.Contains(b.max); });
        foreach (var r0 in new[] { estate, physics, colliders })
        {
            if (!r0) continue;
            foreach (var c in r0.GetComponentsInChildren<Collider>(true))
            {
                if (!c || c is MeshCollider) continue; var b = c.bounds;
                if (b.max.y < .12f || b.size.x > 60f || b.size.z > 60f || !W(b)) continue;
                Object.DestroyImmediate(c); kol++;
            }
        }
        foreach (var v in vols)
            gen += WytnijZGrupy((gr, m) => !gr.StartsWith("Podłoże") && !gr.StartsWith("Osiedle A / salon") && !gr.StartsWith("Osiedle A / stacja") && !gr.StartsWith("Osiedle A / drogi") && gr != GrupaFarbyA, v);
        if (root)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r && r.bounds.max.y > .05f && W(r.bounds) && !r.transform.name.StartsWith("Salon")) { Object.DestroyImmediate(r.gameObject); gen++; }
        Debug.Log("BRAXHORN_SALON zwalenie trójkąty=" + troj + " kolizje=" + kol + " generator=" + gen + " (2 garaże Codexa x −46.5…−34.7 z −43.6…−34.2, w tym połówka po cięciu stacji; korony drzew Wiśniowej nietknięte)");
        return troj + kol + gen;
    }

    // ---------------- plac ekspozycyjny ----------------
    static Material SalonKostka => Pbr("salon: kostka placu", "concrete_pavers_02", 1.4f, new Color(.80f, .80f, .79f));
    static void Plyta(Material m, float x0, float x1, float z0, float z1, float y1 = .14f) { if (x1 - x0 > .005f && z1 - z0 > .005f) BoxMinMax(m, x0, x1, 0, y1, z0, z1, true); }
    // obrzeże granitowe 10 cm, równo z kostką, z fugą co 1 m × K (fuga = szczelina 1 cm do podkładu)
    static void Obrzeze(bool wzdluzX, float c, float a0, float a1)
    {
        var gr = Pbr("salon: obrzeże granit", "concrete_pavement_02", 1.0f, new Color(.42f, .43f, .45f)); var fu = SaMat("fuga obrzeża", new Color(.2f, .2f, .2f), .1f);
        for (float a = a0; a < a1 - .01f; a += K)
        {
            float e = Mathf.Min(a1, a + K - .01f);
            if (wzdluzX) BoxMinMax(gr, a, e, 0, .14f, c - .05f, c + .05f, true); else BoxMinMax(gr, c - .05f, c + .05f, 0, .14f, a, e, true);
            if (e < a1 - .005f) { if (wzdluzX) BoxMinMax(fu, e, e + .01f, 0, .12f, c - .05f, c + .05f, false); else BoxMinMax(fu, c - .05f, c + .05f, 0, .12f, e, e + .01f, false); }
        }
    }
    static void SalonPlac()
    {
        group = "Osiedle A / salon — plac";
        float x0 = SAX0, x1 = SAX1, zS = SBZ1, zN = SAZ1;
        var kos = SalonKostka; var ciem = Pbr("salon: kostka ciemna", "concrete_pavers_02", 1.4f, new Color(.36f, .36f, .37f));
        // obrzeża od chodników (zachód: Wiśniowa, północ: B1)
        Obrzeze(false, x0 + .05f, SAZ0 + .3f, zN - .1f); Obrzeze(true, zN - .05f, x0 + .1f, x1);
        SaDetal("plac", "obrzeża granitowe z fugą co 1 m × K", 2);
        float xa = x0 + .1f, zb = zN - .1f;                       // kostka od obrzeży
        // pas ciemnej kostki wzdłuż stacji (x1 − 0.4 … x1) i pas odwodnienia przed fasadą (z zS … zS + 0.4)
        float xc = x1 - .4f, zd = zS + .4f;
        // ruszt odwodnienia liniowego (stal, szczeliny 1 cm do korpusu) — wzdłuż fasady północnej, od narożnika bryły do stacji
        var korpus = SaMat("korpus odwodnienia", new Color(.08f, .08f, .09f), .2f); var ruszt = SaMat("ruszt stalowy", new Color(.30f, .31f, .33f), .45f, .7f);
        BoxMinMax(korpus, SBX0, xc, 0, .11f, zS, zd, true);
        for (float x = SBX0 + .02f; x < xc - .03f; x += .06f) BoxMinMax(ruszt, x, Mathf.Min(xc - .02f, x + .05f), .11f, .14f, zS + .03f, zd - .03f, false);
        BoxMinMax(ruszt, SBX0, xc, .11f, .14f, zS, zS + .03f, false); BoxMinMax(ruszt, SBX0, xc, .11f, .14f, zd - .03f, zd, false);
        SaDetal("plac", "odwodnienie liniowe z rusztem (szczeliny)");
        Plyta(kos, xa, SBX0, zS, zd);                                                  // przed narożnikiem bryły (pas zachodni)
        Plyta(ciem, xc, x1, zS, zb);                                                   // pas ciemny przy stacji
        SaDetal("plac", "pas ciemnej kostki przy stacji");
        // pole kostki: z zd … zb, x xa … xc — z narożnikiem po łuku (ćwiartka r salonNarR wokół środka łuku)
        if (salonNarR > 0f)
        {
            float cx = salonNarSrodek.x, cz = salonNarSrodek.z;
            Plyta(kos, cx, xc, zd, zb);                     // wschodnia część (pełny prostokąt)
            Plyta(kos, xa, cx, zd, cz);                     // zachodnia część do środka łuku
            // ćwiartka koła: wachlarz trójkątów (lico 0.14) + bok łuku do gruntu + kolizje schodkowe
            int n = 12; var top = new Vector3(cx, .14f, cz);
            for (int k = 0; k < n; k++)
            {
                float a0 = Mathf.PI * .5f + k * Mathf.PI * .5f / n, a1 = Mathf.PI * .5f + (k + 1) * Mathf.PI * .5f / n;
                var p0 = top + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * salonNarR; var p1 = top + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * salonNarR;
                p0.x = Mathf.Max(p0.x, xa); p1.x = Mathf.Max(p1.x, xa); p0.z = Mathf.Min(p0.z, zb); p1.z = Mathf.Min(p1.z, zb);
                Tri3(kos, top, p1, p0, Vector3.up);
                var d = ((p0 + p1) * .5f - top); d.y = 0; d.Normalize();
                Quad(Plinth, p0, p1, p1 - Vector3.up * .14f, p0 - Vector3.up * .14f, d);
            }
            for (int k = 0; k < 4; k++)
            {
                float zz0 = cz + k * salonNarR / 4f, zz1 = cz + (k + 1) * salonNarR / 4f, hw = Mathf.Sqrt(Mathf.Max(0f, salonNarR * salonNarR - zz1 * zz1 + 2 * zz1 * cz - cz * cz));
                if (hw > .05f) Collider(new Vector3(cx - hw * .5f, .07f, (zz0 + zz1) * .5f), new Vector3(hw, .14f, zz1 - zz0));
            }
            SaDetal("plac", "narożnik po łuku krawężnika (wachlarz kostki)");
        }
        else Plyta(kos, xa, xc, zd, zb);
        // słupki z odblaskiem od chodnika B1 (blokują wjazd, przejście między nimi 3 m)
        var odbBialy = SaMat("odblask biały", new Color(.95f, .95f, .95f), .9f); var odbCzerw = SaMat("odblask czerwony", new Color(.8f, .05f, .05f), .9f);
        foreach (float sx in new[] { -45.0f, -42.0f, -39.0f })
        {
            var p = new Vector3(sx, .14f, zN - .55f);
            Cylinder(Steel, p, .07f, .9f * K, 14, true, true);
            Cylinder(odbBialy, p + Vector3.up * (.9f * K - .22f), .074f, .06f, 14, false, false);
            Cylinder(odbCzerw, p + Vector3.up * (.9f * K - .32f), .074f, .06f, 14, false, false);
            Cylinder(Steel, p + Vector3.up * (.9f * K), .05f, .02f, 14, true, false);
        }
        SaDetal("plac", "słupki z odblaskiem", 3);
        // tablice cen (0 zł: miejsce na cenę — EKONOMIA), w przerwach między autami, przodem do B1
        var bialy = SaMat("tablica ceny", new Color(.96f, .96f, .95f), .4f); var napis = SaMat("napis ceny", new Color(.08f, .09f, .1f), .3f);
        foreach (float ax in SalonPlacX)
        {
            float tx = ax + 2.0f, tz = SalonPlacZ + 3.4f;
            BoxMinMax(Steel, tx - .3f, tx + .3f, .14f, .16f, tz - .2f, tz + .2f, false);              // stopa
            Tube(Steel, new Vector3(tx, .16f, tz), new Vector3(tx, .95f * K, tz), .025f, 8);
            BoxMinMax(Frame, tx - .34f, tx + .34f, .95f * K, .95f * K + .5f, tz - .03f, tz + .01f, false);
            BoxMinMax(bialy, tx - .31f, tx + .31f, .95f * K + .03f, .95f * K + .47f, tz + .01f, tz + .02f, false);
            Napis3D("CENA", new Vector3(tx, .95f * K + .36f, tz + .02f), Vector3.forward, .06f, .004f, napis, napis, .15f, .14f, true);
            Napis3D("0 ZŁ", new Vector3(tx, .95f * K + .12f, tz + .02f), Vector3.forward, .14f, .006f, SaMat("napis ceny czerwony", new Color(.7f, .05f, .05f), .3f), napis, .15f, .14f, true);
            Collider(new Vector3(tx, .55f, tz), new Vector3(.68f, 1.1f, .4f));
        }
        SaDetal("plac", "tablice cen 0 zł", SalonPlacX.Length);
        // maszty oświetlenia placu (6 m, wysięgnik, oprawa LED świecąca w dół)
        var opr = StSwiatlo("salon: oprawa placu", new Color(1f, .98f, .92f));
        foreach (var (mx, mz, kx) in new[] { (xa + .35f, -34.4f, 1f), (x1 - .25f, -31.0f, -1f) })
        {
            var b = new Vector3(mx, .14f, mz);
            Cylinder(Plinth, b - Vector3.up * .02f, .22f, .3f, 16, true, true);                     // fundament z fazą
            Cylinder(Frame, b + Vector3.up * .28f, .09f, 6f, 14, true, false); Collider(b + Vector3.up * 3f, new Vector3(.25f, 6f, .25f));
            Tube(Frame, b + Vector3.up * 6f, b + new Vector3(kx * 1.1f, 6.15f, 0), .04f, 8);
            var h = b + new Vector3(kx * 1.25f, 6.1f, 0);
            BoxMinMax(Frame, h.x - .35f, h.x + .35f, h.y, h.y + .12f, h.z - .14f, h.z + .14f, false);
            BoxMinMax(opr, h.x - .3f, h.x + .3f, h.y - .01f, h.y, h.z - .1f, h.z + .1f, false, true);
        }
        SaDetal("plac", "maszty oświetlenia z wysięgnikiem i oprawą", 2);
        // totem salonu: cokół, korpus grafit, pole świetlne z napisem, emblemat (pierścień z belką — bez marek aut)
        {
            float tx = -48.0f, tz = -28.9f, w = .55f, d = .16f, h = 3.6f;
            var graf = SaMat("totem grafit", new Color(.12f, .13f, .14f), .5f, .4f); var pole = StSwiatlo("salon: pole świetlne totemu", new Color(.95f, .97f, 1f));
            BoxMinMax(Concrete, tx - w - .1f, tx + w + .1f, .14f, .3f, tz - d - .1f, tz + d + .1f, true);
            BoxMinMax(graf, tx - w, tx + w, .3f, h, tz - d, tz + d, true);
            BoxMinMax(obrTotemu, tx - w - .02f, tx + w + .02f, h, h + .04f, tz - d - .02f, tz + d + .02f, false);
            BoxMinMax(pole, tx - w + .08f, tx + w - .08f, h - 1.1f, h - .15f, tz + d, tz + d + .005f, false);
            Napis3D("SALON", new Vector3(tx, h - .78f, tz + d + .005f), Vector3.forward, .2f, .01f, graf, graf, .15f, .14f, true);
            Napis3D("AUTO", new Vector3(tx, h - .48f, tz + d + .005f), Vector3.forward, .14f, .01f, graf, graf, .15f, .14f, true);
            var em = new Vector3(tx, h - 1.75f, tz + d + .03f); var srebro = SaMat("emblemat srebro", new Color(.78f, .79f, .8f), .8f, .9f);
            for (int k = 0; k < 20; k++)
            {
                float a0 = k * Mathf.PI * 2f / 20f, a1 = (k + 1) * Mathf.PI * 2f / 20f;
                Tube(srebro, em + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * .3f, em + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * .3f, .025f, 6);
            }
            Tube(srebro, em + new Vector3(-.3f, 0, 0), em + new Vector3(.3f, 0, 0), .025f, 6);
            Tube(srebro, em + new Vector3(0, -.3f, 0), em + new Vector3(0, 0, 0), .025f, 6);
            Napis3D("SALON SAMOCHODOWY", new Vector3(tx, 1.0f, tz + d + .005f), Vector3.forward, .06f, .004f, srebro, srebro, .15f, .14f, true);
            SaDetal("plac", "totem: cokół, pole świetlne, napis, emblemat pierścieniowy, obróbka");
        }
    }
    static Material obrTotemu => SaMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f);

    // ---------------- bryła (2 kondygnacje × 4.2 m × K, przeszklona) ----------------
    static float SF1 => SYF + 4.2f * K;                 // poziom antresoli (lico posadzki piętra)
    static float SYR => SF1 + 4.2f * K - .35f;          // spód stropu dachu
    const float SAntZ = -47.0f;                         // krawędź antresoli (na północ pustka na dwie kondygnacje)
    const float SEX0 = -39.1f, SEX1 = -37.4f, SEZb = -38.9f;   // schody ruchome (obrys), dolny grzebień
    const float SSX0 = -41.3f, SSX1 = -39.3f, SSZb = -38.95f;  // schody reprezentacyjne (obrys), pierwszy stopień
    static float SEZt => SEZb - 1.2f - 4.2f * K / Mathf.Tan(35f * Mathf.Deg2Rad) - 1.2f;   // górny grzebień
    static readonly (float x, float z)[] SalonTalerze = { (-45.6f, -42.2f), (-45.6f, -51.4f) };   // obrotowe podesty parteru
    const float STalerzR = 3.25f;
    static readonly (float x, float z)[] SalonAntresolaAuta = { (-47.2f, -51.5f), (-43.85f, -51.5f) };
    static readonly List<Transform> salonTalerzeObj = new List<Transform>();
    static BraxSchodyRuchome salonSchody;

    static void SalonBryla()
    {
        group = "Osiedle A / salon — bryła";
        float x0 = SBX0, x1 = SBX1, z0 = SBZ0, z1 = SBZ1, yR = SYR, yA = yR + .35f + .8f; const float t = .3f;
        var okl = Pbr("salon: okładzina elewacji", "exterior_wall_cladding_02", 2.4f, new Color(.78f, .79f, .80f));
        var tynk = Pbr("salon: tynk wewnętrzny", "white_stucco_02", 2.5f, new Color(.95f, .95f, .95f));
        var graf = SaMat("pas attyki grafit", new Color(.13f, .14f, .15f), .5f, .4f); var obr = obrTotemu;
        // ściana pełna tylko od zaplecza (południe) z drzwiami zaplecza/ewakuacyjnymi; reszta szkło na całą wysokość
        float ex0 = -45.6f, ex1 = ex0 + .9f * K, eh = 2.1f * K;
        BoxMinMax(okl, x0, ex0, 0, yA, z0, z0 + t, true); BoxMinMax(okl, ex1, x1, 0, yA, z0, z0 + t, true); BoxMinMax(okl, ex0, ex1, eh + SYF, yA, z0, z0 + t, true);
        var fug = SaMat("fuga okładziny", new Color(.25f, .25f, .26f), .1f); int nf = 0;
        for (float xx = x0 + 1.2f * K; xx < x1 - .3f; xx += 1.2f * K) { if (xx > ex0 - .2f && xx < ex1 + .2f) continue; BoxMinMax(fug, xx - .005f, xx + .005f, .45f, yR, z0 - .004f, z0, false); nf++; }
        for (float yy = 2.4f * K; yy < yR - .3f; yy += 2.4f * K) { BoxMinMax(fug, x0, ex0 - .05f, yy - .005f, yy + .005f, z0 - .004f, z0, false); BoxMinMax(fug, ex1 + .05f, x1, yy - .005f, yy + .005f, z0 - .004f, z0, false); nf += 2; }
        SaDetal("bryła", "fugi okładziny ściany zaplecza (pion co 1.2 m × K, poziom co 2.4 m × K)", nf);
        BoxMinMax(Plinth, x0 - .04f, ex0, 0, .45f, z0 - .04f, z0, false); BoxMinMax(Plinth, ex1, x1 + .04f, 0, .45f, z0 - .04f, z0, false);
        SaDetal("bryła", "cokół kamienny");
        // strop dachu, membrana, attyka (pas grafitowy), obróbka z kapinosem, gzyms dachu
        BoxMinMax(tynk, x0 + .05f, x1 - .05f, yR, yR + .35f, z0 + t, z1 - .05f, false, true);
        BoxMinMax(SaMat("membrana dachu", new Color(.35f, .36f, .37f), .2f), x0 + .3f, x1 - .3f, yR + .35f, yR + .39f, z0 + .3f, z1 - .3f, false);
        BoxMinMax(graf, x0 - .06f, x1 + .06f, yR, yA, z1 - .25f, z1 + .06f, false); BoxMinMax(graf, x0 - .06f, x0 + .25f, yR, yA, z0 + t, z1 - .25f, false);
        BoxMinMax(graf, x1 - .25f, x1 + .06f, yR, yA, z0 + t, z1 - .25f, false);
        ObwodBez(obr, x0 - .06f, x1 + .06f, z0 - .04f, z1 + .06f, .35f, yA, yA + .05f, .04f);
        foreach (var (a, b, c0, c1) in new[] { (x0 - .2f, x1 + .2f, z1 + .06f, z1 + .2f), (x0 - .2f, x0 - .06f, z0, z1 + .06f), (x1 + .06f, x1 + .2f, z0, z1 + .06f) })
            BoxMinMax(obr, a, b, yR - .05f, yR, c0, c1, false);                                     // gzyms dachu (wysunięty, kapinos)
        SaDetal("bryła", "attyka grafit + obróbka z kapinosem + gzyms dachu", 3);
        // gzyms międzykondygnacyjny (pas grafitowy na wysokości stropu antresoli, wysunięty 12 cm) na fasadach szklanych
        float yg0 = SF1 - .45f, yg1 = SF1 + .1f;
        BoxMinMax(graf, x0 - .12f, x0 + .07f, yg0, yg1, z0 + t, z1 + .12f, false); BoxMinMax(graf, x1 - .07f, x1 + .12f, yg0, yg1, z0 + t, z1 + .12f, false);
        BoxMinMax(graf, x0 - .12f, x1 + .12f, yg0, yg1, z1 - .07f, z1 + .12f, false);
        BoxMinMax(obr, x0 - .14f, x1 + .14f, yg1, yg1 + .03f, z1 - .07f, z1 + .14f, false); BoxMinMax(obr, x0 - .14f, x0 - .07f, yg1, yg1 + .03f, z0 + t, z1 - .07f, false); BoxMinMax(obr, x1 + .07f, x1 + .14f, yg1, yg1 + .03f, z0 + t, z1 - .07f, false);
        SaDetal("bryła", "gzyms międzykondygnacyjny z obróbką");
        var lit = StSwiatlo("salon: litery attyki", new Color(.96f, .97f, 1f));
        Napis3D("SALON SAMOCHODOWY", new Vector3(x0 - .06f, yR + .4f, (z0 + z1) * .5f), Vector3.left, .55f, .06f, lit, graf, .15f, .14f, true);
        Napis3D("SALON AUT", new Vector3((x0 + x1) * .5f, yR + .4f, z1 + .06f), Vector3.forward, .5f, .06f, lit, graf, .15f, .14f, true);
        SaDetal("bryła", "litery podświetlane na attyce", 2);
        // fasady szklane na całą wysokość: rygle 2.6, pas stropu (gzyms), piętro 2.6 nad antresolą
        var rygle = new[] { 2.6f, yg0 - .02f, yg1 + .02f, SF1 + 2.6f };
        BoxMinMax(Plinth, x0, x0 + .3f, 0, .3f, z0 + t, z1, true); BoxMinMax(Plinth, x1 - .3f, x1, 0, .3f, z0 + t, z1, true);
        FasadaSzklana(false, x0 + .15f, z0 + t, z1 - .15f, .3f, yR, true, rygle);
        FasadaSzklana(false, x1 - .15f, z0 + t, z1 - .15f, .3f, yR, true, rygle);
        BoxMinMax(Plinth, x0 + .3f, SGX0, 0, .3f, z1 - .3f, z1, true); BoxMinMax(Plinth, SDX1 + .15f, x1 - .3f, 0, .3f, z1 - .3f, z1, true);
        BoxMinMax(Frame, x0 + .02f, SGX0, .3f, yR, z1 - .2f, z1 - .05f, true);
        FasadaSzklana(true, z1 - .15f, SDX1 + .15f, x1 - .3f, .3f, yR, true, rygle);
        // brama aut (zamknięta, segmentowa przeszklona) i drzwi klientów — jak w rzucie, nad nimi szkło do dachu
        {
            float a = SGX0, b = SGX1, h = SYF + SGH, zf = z1 - .15f;
            BoxMinMax(Frame, a, a + .12f, 0, h + .12f, zf - .1f, zf + .1f, true); BoxMinMax(Frame, b - .12f, b, 0, h + .12f, zf - .1f, zf + .1f, true);
            BoxMinMax(Frame, a, b, h, h + .12f, zf - .1f, zf + .1f, false);
            BoxMinMax(SaMat("kaseta bramy", new Color(.55f, .56f, .58f), .5f, .5f), a + .1f, b - .1f, h + .14f, h + .5f, zf - .5f, zf - .12f, false);
            float hs = (h - SYF) / 4f;
            for (int k = 0; k < 4; k++)
            {
                float y0 = SYF + k * hs, y1 = y0 + hs;
                BoxMinMax(Frame, a + .12f, b - .12f, y0, y0 + .06f, zf - .03f, zf + .03f, false);
                for (int q = 0; q < 3; q++)
                {
                    float px = a + .12f + q * (b - a - .24f) / 3f, qx = px + (b - a - .24f) / 3f;
                    BoxMinMax(Frame, qx - .03f, qx, y0 + .06f, y1, zf - .03f, zf + .03f, false);
                    BoxMinMax(GlassClear, px + .01f, qx - .03f, y0 + .07f, y1 - .01f, zf - .01f, zf + .01f, false);
                }
            }
            BoxMinMax(Steel, a + .3f, a + .45f, SYF + 1.0f, SYF + 1.04f, zf + .03f, zf + .07f, false);
            Collider(new Vector3((a + b) * .5f, h * .5f, zf), new Vector3(b - a, h, .2f));
            FasadaSzklana(true, zf, a, b, h + .12f, yR, false, rygle);
            BoxMinMax(Frame, b, SDX0, 0, yR, zf - .1f, zf + .1f, true);
            SaDetal("bryła", "brama aut segmentowa przeszklona 3.5 × 2.6 m × K (zamknięta): rama, kaseta, rygle, uchwyt");
        }
        {
            float a = SDX0, b = SDX1, h = SYF + SDH, zf = z1 - .15f;
            BoxMinMax(Frame, a, a + .15f, 0, h + .15f, zf - .1f, zf + .1f, false); BoxMinMax(Frame, b, b + .15f, 0, h + .15f, zf - .1f, zf + .1f, true);
            BoxMinMax(Frame, a, b + .15f, h, h + .15f, zf - .1f, zf + .1f, false);
            BoxMinMax(SaMat("kaseta napędu drzwi", new Color(.55f, .56f, .58f), .5f, .5f), a - .9f, b + .9f, h + .17f, h + .45f, zf - .32f, zf - .12f, false);
            BoxMinMax(GlassClear, a - .95f, a - .05f, SYF + .02f, h, zf - .3f, zf - .28f, false); BoxMinMax(GlassClear, b + .2f, b + 1.1f, SYF + .02f, h, zf - .3f, zf - .28f, false);
            BoxMinMax(SaMat("próg aluminiowy", new Color(.6f, .61f, .63f), .5f, .7f), a + .15f, b, 0, SYF, z1 - .3f, z1 + .02f, true);
            FasadaSzklana(true, zf, a, b + .15f, h + .15f, yR, false, rygle);
            float cy = 3.55f + SYF, c0 = a - .5f, c1 = b + .65f, cz1 = z1 + 1.5f;
            BoxMinMax(Steel, c0, c1, cy, cy + .08f, z1, z1 + .06f, false); BoxMinMax(Steel, c0, c1, cy, cy + .08f, cz1 - .06f, cz1, false);
            BoxMinMax(Steel, c0, c0 + .06f, cy, cy + .08f, z1 + .06f, cz1 - .06f, false); BoxMinMax(Steel, c1 - .06f, c1, cy, cy + .08f, z1 + .06f, cz1 - .06f, false);
            BoxMinMax(GlassClear, c0 + .06f, c1 - .06f, cy + .05f, cy + .07f, z1 + .06f, cz1 - .06f, false);
            foreach (float cx in new[] { c0 + .2f, c1 - .2f }) Tube(Steel, new Vector3(cx, cy + 1.4f, z1 + .02f), new Vector3(cx, cy + .08f, cz1 - .1f), .015f, 6);
            var led = StSwiatlo("salon: oczko LED daszka", new Color(1f, .97f, .9f));
            for (float cx = c0 + .5f; cx < c1 - .3f; cx += .9f) Cylinder(led, new Vector3(cx, cy - .005f, (z1 + cz1) * .5f), .05f, .01f, 10, true, false);
            SaDetal("bryła", "drzwi przesuwne z kasetą, próg, nadświetle, daszek szklany na cięgnach z oczkami LED");
        }
        // drzwi zaplecza / ewakuacyjne (ZAMKNIĘTE)
        {
            float zf = z0 + .15f;
            BoxMinMax(Frame, ex0 - .06f, ex0, SYF, SYF + eh + .06f, z0 - .02f, z0 + t + .02f, false); BoxMinMax(Frame, ex1, ex1 + .06f, SYF, SYF + eh + .06f, z0 - .02f, z0 + t + .02f, false);
            BoxMinMax(Frame, ex0 - .06f, ex1 + .06f, SYF + eh, SYF + eh + .06f, z0 - .02f, z0 + t + .02f, false);
            BoxMinMax(SaMat("skrzydło drzwi stal", new Color(.55f, .57f, .6f), .4f, .5f), ex0, ex1, SYF, SYF + eh, zf - .03f, zf + .03f, true);
            BoxMinMax(Steel, ex0 + .1f, ex1 - .1f, SYF + 1.0f, SYF + 1.05f, zf + .03f, zf + .09f, false);
            BoxMinMax(Steel, ex1 - .18f, ex1 - .08f, SYF + 1.0f, SYF + 1.04f, zf - .1f, zf - .03f, false);
            BoxMinMax(SaMat("próg stal", new Color(.5f, .5f, .52f), .5f, .6f), ex0, ex1, 0, SYF, z0 - .1f, z0 + t, true);
            var ziel = StSwiatlo("salon: znak wyjścia", new Color(.05f, .7f, .25f));
            BoxMinMax(ziel, (ex0 + ex1) * .5f - .45f, (ex0 + ex1) * .5f + .45f, SYF + eh + .25f, SYF + eh + .5f, z0 + t, z0 + t + .03f, false);
            Napis3D("WYJŚCIE", new Vector3((ex0 + ex1) * .5f, SYF + eh + .3f, z0 + t + .03f), Vector3.forward, .12f, .01f, StSwiatlo("salon: napis wyjścia", Color.white), ziel, .15f, .14f, true);
            Napis3D("ZAPLECZE", new Vector3((ex0 + ex1) * .5f, SYF + eh + .2f, z0 - .02f), Vector3.back, .1f, .008f, SaMat("napis zaplecza", new Color(.1f, .1f, .1f), .3f), obr, .15f, .14f, true);
            SaDetal("bryła", "drzwi zaplecza/ewakuacyjne zamknięte: ościeżnica, skrzydło, antypanika, klamka, próg, znaki");
        }
        // rury spustowe do ziemi na ścianie zaplecza (rzygacz, lej, obejmy, kolano, kratka)
        foreach (float rx in new[] { x0 + .6f, x1 - .6f })
        {
            BoxMinMax(obr, rx - .14f, rx + .14f, yA - .55f, yA - .3f, z0 - .22f, z0, false);
            BoxMinMax(obr, rx - .05f, rx + .05f, yA - .25f, yA - .15f, z0 - .2f, z0, false);
            Tube(obr, new Vector3(rx, yA - .55f, z0 - .12f), new Vector3(rx, .3f, z0 - .12f), .05f, 10);
            for (float yy = .8f; yy < yA - .8f; yy += 1.8f) BoxMinMax(Steel, rx - .07f, rx + .07f, yy, yy + .04f, z0 - .18f, z0, false);
            Tube(obr, new Vector3(rx, .3f, z0 - .12f), new Vector3(rx, .12f, z0 - .45f), .05f, 10);
            BoxMinMax(SaMat("kratka odpływu", new Color(.12f, .12f, .13f), .3f, .5f), rx - .15f, rx + .15f, .14f, .155f, z0 - .6f, z0 - .3f, false);
        }
        SaDetal("bryła", "rury spustowe do ziemi z rzygaczem, lejem, obejmami i kolanem", 2);
        var kin = StSwiatlo("salon: kinkiet", new Color(1f, .95f, .85f));
        foreach (float kx in new[] { x0 + 2.5f, (x0 + x1) * .5f, x1 - 2.5f }) { BoxMinMax(Frame, kx - .08f, kx + .08f, 2.9f, 3.2f, z0 - .12f, z0, false); BoxMinMax(kin, kx - .05f, kx + .05f, 2.95f, 3.15f, z0 - .125f, z0 - .12f, false); }
        SaDetal("bryła", "kinkiety elewacyjne", 3);
    }
    // ściana kurtynowa: słupki co 1.6 m, rygle (lista wysokości), szkło, jedna kolizja
    static void FasadaSzklana(bool wzdluzX, float c, float a0, float a1, float yBot, float yTop, bool kolizja = true, float[] rygleY = null)
    {
        if (a1 - a0 < .1f || yTop - yBot < .1f) return;
        void B(Material m, float s, float e, float y0, float y1, float d0, float d1) { if (wzdluzX) BoxMinMax(m, s, e, y0, y1, c + d0, c + d1, false); else BoxMinMax(m, c + d0, c + d1, y0, y1, s, e, false); }
        int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 1.6f)); float st = (a1 - a0) / n;
        var rygle = new List<float> { yBot }; foreach (float ry in rygleY ?? new[] { 2.6f, 5.2f }) if (ry > yBot + .3f && ry < yTop - .3f) rygle.Add(ry); rygle.Add(yTop - .06f);
        for (int k = 0; k <= n; k++) { float s = a0 + k * st; B(Frame, Mathf.Max(a0, s - .04f), Mathf.Min(a1, s + .04f), yBot, yTop, -.07f, .07f); }
        foreach (float ry in rygle) B(Frame, a0, a1, ry, ry + .06f, -.06f, .06f);
        for (int k = 0; k < n; k++)
            for (int j = 0; j + 1 < rygle.Count; j++)
                B(GlassClear, a0 + k * st + .04f, a0 + (k + 1) * st - .04f, rygle[j] + .06f, rygle[j + 1], -.01f, .01f);
        if (kolizja) { var cc = wzdluzX ? new Vector3((a0 + a1) * .5f, (yBot + yTop) * .5f, c) : new Vector3(c, (yBot + yTop) * .5f, (a0 + a1) * .5f); Collider(cc, wzdluzX ? new Vector3(a1 - a0, yTop - yBot, .2f) : new Vector3(.2f, yTop - yBot, a1 - a0)); }
        SaDetal("bryła", "fasada szklana: słupki i rygle", n + 1);
    }
    // płyta pochyła (policzek, szkło balustrady, osłona): między liniami A (zA, yA) i B (zB, yB), x0…x1, grubość w dół
    static void Pochyla(Material m, float x0, float x1, float zA, float yA, float zB, float yB, float grub)
    {
        Vector3 P(float x, float z, float y) => new Vector3(x, y, z);
        var d = new Vector3(0, yB - yA, zB - zA).normalized; var nUp = Vector3.Cross(d, Vector3.right); if (nUp.y < 0) nUp = -nUp;
        Vector3 a0 = P(x0, zA, yA), a1 = P(x1, zA, yA), b0 = P(x0, zB, yB), b1 = P(x1, zB, yB), dn = Vector3.down * grub;
        Quad(m, a0, a1, b1, b0, nUp); Quad(m, a0 + dn, b0 + dn, b1 + dn, a1 + dn, -nUp);
        Quad(m, a0, b0, b0 + dn, a0 + dn, Vector3.left); Quad(m, a1, a1 + dn, b1 + dn, b1, Vector3.right);
        var nA = -new Vector3(0, 0, d.z).normalized; Quad(m, a0, a0 + dn, a1 + dn, a1, nA); Quad(m, b0, b1, b1 + dn, b0 + dn, -nA);
    }
    static void KolizjaPochyla(float x0, float x1, float zA, float yA, float zB, float yB, float grub, string nazwa = "kolizja pochyła")
    {
        var g = new GameObject(nazwa); g.transform.SetParent(colliders, false);
        var a = new Vector3((x0 + x1) * .5f, yA - grub * .5f, zA); var b = new Vector3((x0 + x1) * .5f, yB - grub * .5f, zB);
        g.transform.position = (a + b) * .5f; g.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
        g.AddComponent<BoxCollider>().size = new Vector3(x1 - x0, grub, Vector3.Distance(a, b));
    }

    // ---------------- wnętrze ----------------
    static GameObject SalonModel(GameObject src, string nazwa, Vector3 at, float yaw, float skala = 1f, bool solid = true)
    {
        if (!src) return null;
        var o = Put(src, at, yaw, .02f, false); if (!o) return null;
        o.name = "Salon: " + nazwa; o.transform.localScale *= K * skala;
        var b = RB(o); o.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        ForceOpaque(o);
        if (solid) { b = RB(o); Collider(b.center, b.size); }
        return o;
    }
    static Material SalonZapisz(Material m, string nazwa)
    {
        string p = Out + "/" + Safe("salon " + nazwa) + ".mat";
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p)) UnityEditor.AssetDatabase.DeleteAsset(p);
        UnityEditor.AssetDatabase.CreateAsset(m, p); return m;
    }
    static Material SalonMatOsobny(string nazwa, Color c, float smooth, float metal, Color? emisja = null)
    {
        var m = new Material(Shader.Find("Standard")) { name = "Salon: " + nazwa, enableInstancing = true };
        m.color = c; m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal);
        if (emisja.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emisja.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        return SalonZapisz(m, nazwa);
    }
    // osobna siatka (obiekty ruchome: talerze, stopnie, poręcze) — prostopadłościany w podsiatkach, zapis jako asset
    sealed class SiatkaSalonu
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<List<int>> sub = new List<List<int>>();
        public void Quad(int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nn, float u0 = 0, float u1 = 1, float v0 = 0, float v1 = 1)
        {
            while (sub.Count <= s) sub.Add(new List<int>());
            int k = v.Count; v.AddRange(new[] { a, b, c, d }); n.AddRange(new[] { nn, nn, nn, nn });
            uv.AddRange(new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) });
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), nn) < 0;
            sub[s].AddRange(flip ? new[] { k, k + 2, k + 1, k, k + 3, k + 2 } : new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }
        public void Box(int s, Vector3 mn, Vector3 mx)
        {
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
            Quad(s, P(mn.x, mx.y, mn.z), P(mx.x, mx.y, mn.z), P(mx.x, mx.y, mx.z), P(mn.x, mx.y, mx.z), Vector3.up);
            Quad(s, P(mn.x, mn.y, mn.z), P(mn.x, mn.y, mx.z), P(mx.x, mn.y, mx.z), P(mx.x, mn.y, mn.z), Vector3.down);
            Quad(s, P(mn.x, mn.y, mn.z), P(mx.x, mn.y, mn.z), P(mx.x, mx.y, mn.z), P(mn.x, mx.y, mn.z), Vector3.back);
            Quad(s, P(mn.x, mn.y, mx.z), P(mn.x, mx.y, mx.z), P(mx.x, mx.y, mx.z), P(mx.x, mn.y, mx.z), Vector3.forward);
            Quad(s, P(mn.x, mn.y, mn.z), P(mn.x, mx.y, mn.z), P(mn.x, mx.y, mx.z), P(mn.x, mn.y, mx.z), Vector3.left);
            Quad(s, P(mx.x, mn.y, mn.z), P(mx.x, mn.y, mx.z), P(mx.x, mx.y, mx.z), P(mx.x, mx.y, mn.z), Vector3.right);
        }
        public Mesh Zapisz(string nazwa)
        {
            var m = new Mesh { name = "Salon: " + nazwa }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.subMeshCount = sub.Count;
            for (int i = 0; i < sub.Count; i++) m.SetTriangles(sub[i], i);
            m.RecalculateBounds(); m.RecalculateTangents();
            string p = Out + "/" + Safe("salon " + nazwa) + ".asset";
            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(p)) UnityEditor.AssetDatabase.DeleteAsset(p);
            UnityEditor.AssetDatabase.CreateAsset(m, p); return m;
        }
    }
    static GameObject SalonObiekt(string nazwa, Transform rodzic, Mesh mesh, Material[] mats, Vector3 pos)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false); g.transform.position = pos;
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = mats;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; return g;
    }

    static void SalonWnetrze()
    {
        group = "Osiedle A / salon — wnętrze";
        float xi0 = SBX0 + .15f, xi1 = SBX1 - .15f, zi0 = SBZ0 + .3f, zi1 = SBZ1 - .15f, yF = SYF, F1 = SF1, yR = SYR;
        var gres = Pbr("salon: gres polerowany", "large_grey_tiles", 1.2f * K, new Color(.97f, .97f, .97f), 1f);
        var listwa = SaMat("listwa przypodłogowa", new Color(.2f, .2f, .21f), .4f);
        var led = StSwiatlo("salon: linia LED", new Color(1f, .99f, .95f)); var sufit = SaMat("sufit", new Color(.96f, .96f, .96f), .2f);
        // PARTER: płyta + gres, listwa przy ścianie zaplecza
        BoxMinMax(Concrete, xi0, xi1, 0, yF - .03f, zi0, zi1, true);
        BoxMinMax(gres, xi0, xi1, yF - .03f, yF, zi0, zi1, false);
        BoxMinMax(listwa, xi0, xi1, yF, yF + .08f, zi0, zi0 + .015f, false);
        // ANTRESOLA: strop z wycięciem na schody ruchome (x SEX0…, z SAntZ…SEZt), gres, spód z LED, czoło z aluminium
        float zT = SEZt;
        BoxMinMax(Concrete, xi0, SEX0, F1 - .35f, F1 - .03f, zi0, SAntZ, true); BoxMinMax(Concrete, SEX0, xi1, F1 - .35f, F1 - .03f, zi0, zT, true);
        BoxMinMax(gres, xi0, SEX0, F1 - .03f, F1, zi0, SAntZ, false); BoxMinMax(gres, SEX0, xi1, F1 - .03f, F1, zi0, zT, false);
        BoxMinMax(sufit, xi0, SEX0, F1 - .38f, F1 - .35f, zi0, SAntZ, false, true); BoxMinMax(sufit, SEX0, xi1, F1 - .38f, F1 - .35f, zi0, zT, false, true);
        int nl = 0;
        for (float lz = zi0 + 1.4f; lz < SAntZ - .8f; lz += 2.4f) { BoxMinMax(led, xi0 + .8f, (lz < zT ? xi1 : SEX0) - .8f, F1 - .385f, F1 - .38f, lz - .04f, lz + .04f, false, true); nl++; }
        var alu = SaMat("aluminium szczotkowane", new Color(.72f, .73f, .75f), .55f, .8f);
        BoxMinMax(alu, xi0, SEX0, F1 - .38f, F1 + .02f, SAntZ, SAntZ + .02f, false); BoxMinMax(alu, SEX0 - .02f, SEX0, F1 - .38f, F1 + .02f, zT, SAntZ, false);
        BoxMinMax(alu, SEX0, xi1, F1 - .38f, F1 + .02f, zT - .02f, zT, false);
        BoxMinMax(listwa, xi0, xi1, F1, F1 + .08f, zi0, zi0 + .015f, false);
        SaDetal("wnętrze", "antresola: strop, gres, czoło aluminiowe, spód z liniami LED", nl);
        // sufit pod dachem z liniami LED i reflektorami na szynach
        BoxMinMax(sufit, xi0, xi1, yR - .08f, yR, zi0, zi1, false, true);
        for (float lz = zi0 + 1.6f; lz < zi1 - 1f; lz += 2.4f) { BoxMinMax(led, xi0 + .8f, xi1 - .8f, yR - .085f, yR - .08f, lz - .04f, lz + .04f, false, true); nl++; }
        foreach (var (tx, tz) in SalonTalerze.Take(1))
        {
            BoxMinMax(Frame, tx - .03f, tx + .03f, yR - .16f, yR - .08f, tz - 2.6f, tz + 2.6f, false);
            for (float sz = tz - 2.2f; sz < tz + 2.4f; sz += 1.1f) { Tube(Frame, new Vector3(tx, yR - .16f, sz), new Vector3(tx + .3f, yR - .55f, sz), .06f, 10); Cylinder(StSwiatlo("salon: soczewka reflektora", new Color(1f, .98f, .9f)), new Vector3(tx + .32f, yR - .6f, sz), .05f, .02f, 10, true, false); nl++; }
        }
        SaDetal("wnętrze", "sufit z liniami LED + reflektory na szynie nad podestem w pustce", nl);
        // balustrada antresoli: szkło bezramowe w stopce aluminiowej, pochwyt inox (od zachodu do schodów; przy wycięciu schodów ruchomych)
        var szklo = GlassClear; var inox = SaMat("inox szczotkowany", new Color(.78f, .79f, .8f), .7f, .9f); float hb = 1.1f * K;
        void Balustrada(bool wzdluzX, float c, float a0, float a1)
        {
            if (a1 - a0 < .1f) return;
            if (wzdluzX) { BoxMinMax(alu, a0, a1, F1, F1 + .12f, c - .06f, c + .06f, false); BoxMinMax(szklo, a0, a1, F1 + .12f, F1 + hb - .05f, c - .01f, c + .01f, false); Tube(inox, new Vector3(a0, F1 + hb, c), new Vector3(a1, F1 + hb, c), .025f, 10); Collider(new Vector3((a0 + a1) * .5f, F1 + hb * .5f, c), new Vector3(a1 - a0, hb, .12f)); }
            else { BoxMinMax(alu, c - .06f, c + .06f, F1, F1 + .12f, a0, a1, false); BoxMinMax(szklo, c - .01f, c + .01f, F1 + .12f, F1 + hb - .05f, a0, a1, false); Tube(inox, new Vector3(c, F1 + hb, a0), new Vector3(c, F1 + hb, a1), .025f, 10); Collider(new Vector3(c, F1 + hb * .5f, (a0 + a1) * .5f), new Vector3(.12f, hb, a1 - a0)); }
        }
        Balustrada(true, SAntZ - .08f, xi0, SSX0 - .02f);
        SaDetal("wnętrze", "balustrada antresoli: szkło w stopce aluminiowej, pochwyt inox");
        SalonSchodyStale(); SalonSchodyRuchome();
        // PARTER: obrotowe podesty (talerz osobnym obiektem z komponentem obrotu; auto doklejane w kroku aut)
        salonTalerzeObj.Clear();
        var mTal = SalonMatOsobny("talerz grafit", new Color(.15f, .15f, .16f), .6f, .2f); var mAluT = SalonMatOsobny("talerz aluminium", new Color(.75f, .76f, .78f), .6f, .85f);
        var ledP = StSwiatlo("salon: LED podestu", new Color(.75f, .88f, 1f));
        foreach (var (tx, tz) in SalonTalerze)
        {
            // podstawa stała (cofnięta) z rowkiem LED, talerz obrotowy 64 segmenty z obręczą
            Cylinder(SaMat("podstawa talerza", new Color(.1f, .1f, .11f), .3f), new Vector3(tx, yF, tz), STalerzR - .08f, .04f, 48, true, true);
            Cylinder(ledP, new Vector3(tx, yF + .04f, tz), STalerzR - .06f, .02f, 48, false, false);
            var sb = new SiatkaSalonu(); int seg = 64;
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
                var d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                sb.Quad(0, Vector3.up * .12f, d1 * (STalerzR - .05f) + Vector3.up * .12f, d0 * (STalerzR - .05f) + Vector3.up * .12f, Vector3.up * .12f, Vector3.up);
                sb.Quad(1, d0 * (STalerzR - .05f) + Vector3.up * .12f, d1 * (STalerzR - .05f) + Vector3.up * .12f, d1 * STalerzR + Vector3.up * .12f, d0 * STalerzR + Vector3.up * .12f, Vector3.up);
                sb.Quad(1, d0 * STalerzR + Vector3.up * .12f, d1 * STalerzR + Vector3.up * .12f, d1 * STalerzR, d0 * STalerzR, (d0 + d1).normalized);
            }
            var mesh = sb.Zapisz("talerz " + salonTalerzeObj.Count);
            var g = SalonObiekt("Salon: talerz obrotowy " + (salonTalerzeObj.Count + 1), Group("Osiedle A / salon — ruchome"), mesh, new[] { mTal, mAluT }, new Vector3(tx, yF + .06f, tz));
            var ob = g.AddComponent<BraxObrotowyPodest>(); ob.talerz = g.GetComponent<MeshRenderer>(); ob.stopniNaSekunde = salonTalerzeObj.Count == 0 ? 7.2f : -6f;
            Collider(new Vector3(tx, yF + .09f, tz), new Vector3(STalerzR * 1.4f, .18f, STalerzR * 1.4f));
            salonTalerzeObj.Add(g.transform);
        }
        SaDetal("wnętrze", "obrotowe podesty: podstawa, rowek LED, talerz 64 segmenty z obręczą aluminiową", SalonTalerze.Length);
        // PARTER: przyjęcie serwisu (pod antresolą, przy fasadzie wschodniej): lada, blat, LED, monitor, panel SERWIS
        {
            float lx0 = -40.8f, lx1 = -40.0f, lz0 = -56.4f, lz1 = -52.6f, h = 1.1f * K * .85f;
            var front = SaMat("lada front", new Color(.88f, .89f, .9f), .5f); var blat = SaMat("blat lady", new Color(.18f, .18f, .2f), .7f);
            BoxMinMax(front, lx0, lx1, yF + .1f, yF + h, lz0, lz1, true);
            BoxMinMax(SaMat("cokół lady", new Color(.1f, .1f, .11f), .3f), lx0 + .05f, lx1 - .05f, yF, yF + .1f, lz0 + .05f, lz1 - .05f, false);
            BoxMinMax(blat, lx0 - .1f, lx1 + .05f, yF + h, yF + h + .05f, lz0 - .05f, lz1 + .05f, false);
            BoxMinMax(ledP, lx0 - .005f, lx0, yF + h - .06f, yF + h - .04f, lz0 + .1f, lz1 - .1f, false);
            float yb = yF + h + .05f;
            BoxMinMax(SaMat("monitor obudowa", new Color(.06f, .06f, .07f), .5f), lx1 - .1f, lx1 - .06f, yb + .12f, yb + .5f, -54.9f, -54.2f, false);
            BoxMinMax(StSwiatlo("salon: ekran", new Color(.35f, .55f, .85f)), lx1 - .105f, lx1 - .1f, yb + .15f, yb + .47f, -54.87f, -54.23f, false);
            var panel = SaMat("panel SERWIS", new Color(.13f, .14f, .15f), .5f, .3f);
            BoxMinMax(panel, -40.9f, -40.85f, F1 - 1.1f, F1 - .5f, lz0, lz1, false);
            Napis3D("SERWIS — PRZYJĘCIE", new Vector3(-40.9f, F1 - .95f, (lz0 + lz1) * .5f), Vector3.left, .2f, .02f, StSwiatlo("salon: napis serwis", Color.white), panel, .15f, .14f, true);
            salonObsada.Add((new Vector3(-38.9f, yF, -54.5f), 270f, "doradca serwisu"));
            SaDetal("wnętrze", "lada przyjęcia serwisu: front, cokół, blat, LED, monitor, panel z napisem");
        }
        // PIĘTRO: 2 auta premium na niskich podestach przy balustradzie (widoczne z parteru)
        var podB = SaMat("podest grafit", new Color(.16f, .16f, .17f), .55f, .2f);
        foreach (var (ax, az) in SalonAntresolaAuta)
        {
            float a0 = ax - 1.6f, a1 = ax + 1.6f, b0 = az - 3.3f, b1 = az + 3.3f;
            BoxMinMax(podB, a0 + .04f, a1 - .04f, F1, F1 + .03f, b0 + .04f, b1 - .04f, true);
            BoxMinMax(ledP, a0 + .02f, a1 - .02f, F1 + .03f, F1 + .05f, b0 + .02f, b1 - .02f, false);
            BoxMinMax(podB, a0, a1, F1 + .05f, F1 + .14f, b0, b1, true);
            ObwodBez(alu, a0, a1, b0, b1, .04f, F1 + .14f, F1 + .16f, .005f);
            BoxMinMax(podB, a0 + .04f, a1 - .04f, F1 + .14f, F1 + .155f, b0 + .04f, b1 - .04f, false);
        }
        SaDetal("wnętrze", "podesty piętra z rowkiem LED i krawędzią aluminiową", SalonAntresolaAuta.Length);
        // PIĘTRO: stanowiska sprzedawców (biurko, monitor, 2 krzesła klientów), sprzedawca NPC
        foreach (float dz in new[] { -51.9f, -54.4f })
        {
            var d = SalonModel(offDesk, "biurko sprzedawcy", new Vector3(-40.2f, F1, dz), 90f, 1f);
            float yb = d ? RB(d).max.y : F1 + .75f * K;
            if (!d)
            {
                BoxMinMax(Pbr("salon: blat dąb", "wood_floor", 1.2f, new Color(.85f, .75f, .6f)), -41.1f, -39.3f, yb - .04f, yb, dz - .5f, dz + .5f, true);
                foreach (float lx in new[] { -41.0f, -39.4f }) BoxMinMax(Steel, lx - .03f, lx + .03f, F1, yb - .04f, dz - .45f, dz + .45f, false);
                BoxMinMax(Steel, -41.0f, -39.4f, F1 + .3f, yb - .1f, dz + .4f, dz + .43f, false);
            }
            BoxMinMax(SaMat("monitor obudowa", new Color(.06f, .06f, .07f), .5f), -39.7f, -39.66f, yb + .12f, yb + .48f, dz - .3f, dz + .3f, false);
            BoxMinMax(StSwiatlo("salon: ekran", new Color(.35f, .55f, .85f)), -39.74f, -39.7f, yb + .15f, yb + .45f, dz - .27f, dz + .27f, false);
            BoxMinMax(Steel, -39.69f, -39.65f, yb, yb + .14f, dz - .03f, dz + .03f, false); BoxMinMax(Steel, -39.78f, -39.58f, yb, yb + .01f, dz - .1f, dz + .1f, false);
            foreach (float cz in new[] { dz - .45f, dz + .45f }) SalonModel(offChair, "krzesło klienta", new Vector3(-41.75f, F1, cz), 90f, 1f);
            salonObsada.Add((new Vector3(-38.6f, F1, dz), 270f, "sprzedawca"));
        }
        SaDetal("wnętrze", "stanowiska sprzedawców na piętrze: biurko, monitor, 2 krzesła klientów", 2);
        // PIĘTRO: biuro kierownika (ścianki szklane, drzwi), biurko, krzesło, szafka
        {
            float bx0 = -41.0f, bz1 = -56.0f, hB = 2.8f * K * .9f;
            FasadaSzklana(false, bx0, zi0, bz1, F1, F1 + hB, true, new float[0]);
            FasadaSzklana(true, bz1, -39.6f, xi1, F1, F1 + hB, false, new float[0]);
            BoxMinMax(Frame, -40.95f, -39.6f, F1 + 2.1f * K, F1 + hB, bz1 - .04f, bz1 + .04f, false);           // nadproże drzwi (otwór x −40.95…−39.6)
            Collider(new Vector3((-39.6f + xi1) * .5f, F1 + 1.5f, bz1), new Vector3(xi1 + 39.6f, 3f, .1f));
            SalonModel(offDesk, "biurko kierownika", new Vector3(-39.2f, F1, -57.1f), 0f, 1f);
            SalonModel(offShelf, "szafka na dokumenty", new Vector3(-37.75f, F1, -56.9f), 270f, 1f);
            salonObsada.Add((new Vector3(-39.2f, F1, -58.0f), 0f, "kierownik"));
            SaDetal("wnętrze", "biuro przeszklone na piętrze: ścianki w ramach, nadproże, biurko, krzesło, szafka");
        }
        // PIĘTRO: bar kawowy (lada z frontem z drewna, blat kamienny, ekspres, młynek, filiżanki, regał), hokery, strefa klienta
        {
            float lx0 = -48.7f, lx1 = -44.7f, lz0 = -57.1f, lz1 = -56.5f, h = 1.05f * K * .85f;
            var drewno = Pbr("salon: front baru dąb", "wood_floor", 1.0f, new Color(.62f, .48f, .34f)); var kamien = Pbr("salon: blat baru marmur", "marble_01", 1.2f, new Color(.95f, .95f, .95f));
            BoxMinMax(drewno, lx0, lx1, F1 + .1f, F1 + h, lz0, lz1, true);
            BoxMinMax(SaMat("cokół baru", new Color(.1f, .1f, .11f), .3f), lx0 + .05f, lx1 - .05f, F1, F1 + .1f, lz0 + .05f, lz1 - .05f, false);
            BoxMinMax(kamien, lx0 - .05f, lx1 + .05f, F1 + h, F1 + h + .04f, lz0 - .05f, lz1 + .15f, false);
            BoxMinMax(inox, lx0 + .1f, lx1 - .1f, F1 + .25f, F1 + .28f, lz1 + .1f, lz1 + .13f, false);                      // podnóżek
            BoxMinMax(SaMat("blat zaplecza baru", new Color(.2f, .2f, .22f), .6f), lx0, lx1, F1, F1 + .9f * K * .85f, zi0, zi0 + .6f, true);
            float yz = F1 + .9f * K * .85f;
            var ekspres = SaMat("ekspres inox", new Color(.75f, .76f, .78f), .8f, .9f);
            BoxMinMax(ekspres, -47.6f, -46.6f, yz, yz + .45f, zi0 + .05f, zi0 + .5f, false);
            BoxMinMax(SaMat("ekspres czarny", new Color(.05f, .05f, .06f), .5f), -47.55f, -46.65f, yz + .45f, yz + .5f, zi0 + .05f, zi0 + .5f, false);
            foreach (float gx in new[] { -47.35f, -46.85f }) Tube(ekspres, new Vector3(gx, yz + .3f, zi0 + .5f), new Vector3(gx, yz + .22f, zi0 + .58f), .025f, 8);
            Cylinder(SaMat("młynek", new Color(.1f, .1f, .1f), .5f, .3f), new Vector3(-46.2f, yz, zi0 + .3f), .09f, .35f, 14, true, false);
            Cylinder(SaMat("pojemnik ziaren", new Color(.6f, .6f, .62f), .9f), new Vector3(-46.2f, yz + .35f, zi0 + .3f), .07f, .15f, 14, true, false);
            for (int q = 0; q < 6; q++) Cylinder(SaMat("filiżanka", new Color(.97f, .97f, .96f), .7f), new Vector3(-45.9f + (q % 3) * .12f, yz + (q / 3) * .06f, zi0 + .2f + (q / 3) * .1f), .04f, .06f, 10, true, false);
            BoxMinMax(SaMat("półka", new Color(.18f, .18f, .2f), .5f), lx0, lx1, F1 + 1.9f, F1 + 1.94f, zi0, zi0 + .3f, false);
            var stool = BraxOsiedleAssety.ModelWnetrz("bar_hoker_skora_nosteam");
            foreach (float sx in new[] { -47.9f, -46.7f, -45.5f })
            {
                var o = stool ? BraxOsiedleAssety.Postaw(Group(group), stool, "Salon: hoker", new Vector3(sx, F1, -55.9f), 0f, .45f, null) : null;
                if (!o) { Cylinder(inox, new Vector3(sx, F1, -55.9f), .22f, .02f, 16, true, false); Tube(inox, new Vector3(sx, F1, -55.9f), new Vector3(sx, F1 + .7f * K, -55.9f), .03f, 8); Cylinder(SaMat("siedzisko skóra", new Color(.1f, .08f, .07f), .5f), new Vector3(sx, F1 + .7f * K, -55.9f), .2f, .07f, 16, true, false); }
                Collider(new Vector3(sx, F1 + .5f, -55.9f), new Vector3(.45f, 1f, .45f));
            }
            salonObsada.Add((new Vector3(-46.7f, F1, -57.4f), 0f, "barista"));
            SalonModel(offArm, "fotel klienta", new Vector3(-43.3f, F1, -57.2f), 0f, 1f); SalonModel(offArm, "fotel klienta", new Vector3(-41.9f, F1, -57.2f), 0f, 1f);
            SalonModel(coffeeTable, "stolik kawowy", new Vector3(-42.6f, F1, -56.0f), 0f, .8f);
            SaDetal("wnętrze", "bar kawowy: front dębowy, blat marmurowy, podnóżek, ekspres z kolbami, młynek, filiżanki, półka, 3 hokery; strefa klienta: 2 fotele, stolik");
        }
        // rośliny (model ×2.4), gniazdka, gaśnice ze znakiem, znak WYJŚCIE nad drzwiami
        foreach (var (px, py, pz) in new[] { (-44.3f, yF, -38.4f), (-48.5f, yF, -57.7f), (-48.4f, F1, -47.6f), (-37.8f, F1, -50.6f) })
        {
            var o = SalonModel(plantPot, "roślina w donicy", new Vector3(px, py, pz), 30f, 2.4f / K, true);
            if (!o) Cylinder(SaMat("donica", new Color(.2f, .2f, .21f), .4f), new Vector3(px, py, pz), .25f, .5f, 16, true, true);
        }
        var gn = SaMat("gniazdko białe", new Color(.95f, .95f, .95f), .5f);
        foreach (float gx in new[] { -47.0f, -43.0f, -39.0f }) foreach (float gy in new[] { yF, F1 }) BoxMinMax(gn, gx, gx + .09f, gy + .3f * K, gy + .3f * K + .09f, zi0, zi0 + .025f, false);
        foreach (float gy in new[] { yF, F1 })
        {
            float gx = -43.6f, gz = zi0 + .02f;
            BoxMinMax(Steel, gx - .1f, gx + .1f, gy + .8f, gy + .95f, gz - .02f, gz + .01f, false);
            Cylinder(SaMat("gaśnica czerwona", new Color(.75f, .05f, .05f), .7f), new Vector3(gx, gy + .55f, gz + .14f), .08f * K, .5f * K, 14, true, false);
            BoxMinMax(SaMat("znak gaśnicy", new Color(.8f, .05f, .05f), .5f), gx - .15f, gx + .15f, gy + 1.55f, gy + 1.85f, gz - .01f, gz, false);
        }
        var ziel = StSwiatlo("salon: znak wyjścia", new Color(.05f, .7f, .25f));
        float dx = (SDX0 + SDX1) * .5f, dh = yF + SDH;
        BoxMinMax(ziel, dx - .4f, dx + .4f, dh + .55f, dh + .8f, SBZ1 - .5f, SBZ1 - .46f, false);
        Napis3D("WYJŚCIE", new Vector3(dx, dh + .6f, SBZ1 - .5f), Vector3.back, .13f, .01f, StSwiatlo("salon: napis wyjścia", Color.white), ziel, .16f, .12f, true);
        SaDetal("wnętrze", "4 rośliny, 6 gniazdek, 2 gaśnice ze znakiem, znak WYJŚCIE");
        // klienci: między podestami parteru i przy biurku na piętrze
        salonObsada.Add((new Vector3(-46.0f, yF, -46.8f), 0f, "klient"));
        salonObsada.Add((new Vector3(-44.0f, F1, -47.7f), 180f, "klient"));
    }

    // schody reprezentacyjne: jednobiegowe, policzki stalowe, stopnie kamienne z noskiem, podświetlenie LED stopni, szkło + pochwyt
    static void SalonSchodyStale()
    {
        group = "Osiedle A / salon — schody";
        int nR = 24; float H = SF1 - SYF, r = H / nR, gl = .35f, x0 = SSX0, x1 = SSX1, zB = SSZb;
        var kamien = Pbr("salon: stopień kamień", "marble_01", 1.0f, new Color(.88f, .88f, .87f)); var stal = SaMat("policzek stal grafit", new Color(.14f, .15f, .16f), .5f, .6f);
        var led = StSwiatlo("salon: LED stopni", new Color(1f, .96f, .88f)); var inox = SaMat("inox szczotkowany", new Color(.78f, .79f, .8f), .7f, .9f);
        float ws = .08f;
        for (int k = 0; k < nR - 1; k++)
        {
            float y1 = SYF + (k + 1) * r, zf = zB - k * gl, zt = zf - gl;
            BoxMinMax(kamien, x0 + ws, x1 - ws, y1 - .05f, y1, zt, zf + .025f, true);                       // stopień z noskiem 2.5 cm
            BoxMinMax(led, x0 + ws + .05f, x1 - ws - .05f, y1 - .065f, y1 - .05f, zf - .03f, zf - .01f, false, true);   // LED pod noskiem
            BoxMinMax(stal, x0 + ws, x1 - ws, y1 - .05f - .04f, y1 - .05f, zt, zf - .02f, false, true);        // nośnik stopnia
        }
        float zTop = zB - (nR - 1) * gl;
        // policzki (pochyłe płyty stalowe 8 × 30 cm) od posadzki do antresoli
        foreach (float px in new[] { x0, x1 - ws })
        {
            Pochyla(stal, px, px + ws, zB + .3f, SYF + .25f, zTop - .05f, SF1 + .1f, .45f);
            KolizjaPochyla(px, px + ws, zB + .3f, SYF + 1.2f, zTop, SF1 + 1.2f, 1.2f, "kolizja policzka");
        }
        // szkło balustrady (obie strony) w zaciskach na policzku + pochwyt inox na kotwach
        float hb = 1.0f * K;
        foreach (float px in new[] { x0 + .02f, x1 - .04f })
        {
            Pochyla(GlassClear, px, px + .02f, zB + .2f, SYF + r + hb - .05f, zTop, SF1 + hb - .05f, hb - .15f);
            for (int k = 0; k < nR; k += 4) { float zz = zB - k * gl; BoxMinMax(inox, px - .01f, px + .03f, SYF + k * r + .15f, SYF + k * r + .3f, zz - .1f, zz + .1f, false); }
        }
        foreach (float px in new[] { x0 - .02f, x1 + .02f })
        {
            Tube(inox, new Vector3(px, SYF + r + hb, zB + .2f), new Vector3(px, SF1 + hb, zTop), .025f, 10);
            Tube(inox, new Vector3(px, SYF + r + hb, zB + .2f), new Vector3(px, SYF + hb - .05f, zB + .45f), .025f, 10);   // zakończenie pochwytu
        }
        SaDetal("schody", "stopnie kamienne z noskiem, LED pod noskami, nośniki, policzki stalowe, szkło w zaciskach, pochwyty inox", nR - 1);
    }

    // schody ruchome w górę: stopnie ruchome (osobne obiekty), grzebienie, płyty wejścia, osłony inox, szkło, poręcze przewijane, napęd
    static void SalonSchodyRuchome()
    {
        group = "Osiedle A / salon — schody ruchome";
        float cx = (SEX0 + SEX1) * .5f, sw = 1.0f * K, yb = SYF + .12f, H = SF1 - SYF, ang = 35f * Mathf.Deg2Rad;
        float z0 = SEZb, z1 = z0 - 1.2f, z2 = z1 - H / Mathf.Tan(ang), z3 = z2 - 1.2f;
        var sciezka = new[] { new Vector3(0, yb - .04f, z0), new Vector3(0, yb, z1), new Vector3(0, yb + H, z2), new Vector3(0, yb + H - .04f, z3) };   // końce 4 cm pod płytami (stopnie chowają się pod grzebień)
        var inox = SaMat("inox szczotkowany", new Color(.78f, .79f, .8f), .7f, .9f); var alu = SaMat("aluminium szczotkowane", new Color(.72f, .73f, .75f), .55f, .8f);
        float hs = .1f, hb = 1.0f * K;
        // osłony boczne (cokoły wewnętrzne inox) i obudowa zewnętrzna kratownicy (pochyła), płyty spodu
        foreach (float sx in new[] { -1f, 1f })
        {
            float a = cx + sx * (sw * .5f + .02f), b = cx + sx * (sw * .5f + .1f), lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
            BoxMinMax(inox, lo, hi, SYF, yb + hs + .05f, z1, z0 + .2f, true);
            Pochyla(inox, lo, hi, z1, yb + hs + .05f, z2, yb + H + hs + .05f, .9f);
            BoxMinMax(inox, lo, hi, SF1 - .2f, yb + H + hs + .05f, z3 - .2f, z2, true);
            float o0 = cx + sx * (sw * .5f + .1f), o1 = sx < 0 ? SEX0 : SEX1, oa = Mathf.Min(o0, o1), ob = Mathf.Max(o0, o1);
            Pochyla(inox, oa, ob, z1 + .3f, yb + hs + .12f, z2 + .3f, yb + H + hs + .12f, 1.0f);       // obudowa zewnętrzna (gzyms poręczy)
            // szkło balustrady nad osłoną
            Pochyla(GlassClear, cx + sx * (sw * .5f + .06f) - .01f, cx + sx * (sw * .5f + .06f) + .01f, z1 + .4f, yb + hb - .1f, z2 + .2f, yb + H + hb - .1f, hb - .23f);
        }
        Pochyla(inox, SEX0, SEX1, z1, yb - .5f, z2, yb + H - .5f, .05f);                                   // spód kratownicy 0.5 m pod stopniami (widoczny z parteru)
        KolizjaPochyla(SEX0, SEX1, z1, yb, z2, yb + H, .3f, "kolizja schodów ruchomych (pochylnia pod stopniami)");
        Collider(new Vector3(cx, yb - .1f, (z0 + z1) * .5f), new Vector3(sw, .2f, z0 - z1)); Collider(new Vector3(cx, yb + H - .1f, (z2 + z3) * .5f), new Vector3(sw, .2f, z2 - z3));
        foreach (float sx in new[] { -1f, 1f }) KolizjaPochyla(cx + sx * (sw * .5f + .05f) - .1f, cx + sx * (sw * .5f + .05f) + .1f, z0 + .2f, yb + 1.0f, z3, yb + H + 1.0f, 1.6f, "kolizja balustrady schodów ruchomych");
        // płyty wejścia/zejścia z ryflowaniem i grzebieniami (zęby co 3 cm)
        var ryf = SaMat("płyta ryflowana", new Color(.62f, .63f, .65f), .5f, .8f); var zab = SaMat("grzebień", new Color(.85f, .75f, .1f), .5f, .5f);
        BoxMinMax(ryf, cx - sw * .5f, cx + sw * .5f, SYF, yb, z0, z0 + .95f, true);
        BoxMinMax(ryf, cx - sw * .5f, cx + sw * .5f, SF1, yb + H, z3 - .95f, z3, true);
        for (float gx = cx - sw * .5f + .02f; gx < cx + sw * .5f - .02f; gx += .03f)
        {
            BoxMinMax(zab, gx, gx + .015f, yb - .01f, yb + .01f, z0 - .08f, z0, false);
            BoxMinMax(zab, gx, gx + .015f, yb + H - .01f, yb + H + .01f, z3, z3 + .08f, false);
        }
        for (float rz = z0 + .05f; rz < z0 + .9f; rz += .06f) BoxMinMax(alu, cx - sw * .5f + .03f, cx + sw * .5f - .03f, yb, yb + .005f, rz, rz + .02f, false);
        SaDetal("schody ruchome", "płyty wejścia ryflowane, grzebienie żółte (zęby co 3 cm), osłony inox, obudowa, szkło balustrad, spód kratownicy");
        // stopnie: bieżnia aluminium z rowkami (podsiatka 0), żółte krawędzie (1), podstopnica (2)
        float L = 0f; for (int i = 1; i < sciezka.Length; i++) L += Vector3.Distance(sciezka[i - 1], sciezka[i]);
        int n = Mathf.Max(8, Mathf.RoundToInt(L / (.4f * K)));
        float sd = L / n - .006f, rise = sd * Mathf.Tan(ang);   // bieżnia krótsza o 6 mm od odstępu — sąsiednie stopnie nie nachodzą (bez migotania)
        var sb = new SiatkaSalonu();
        float hw = sw * .5f - .01f;
        sb.Box(0, new Vector3(-hw, -.03f, -sd), new Vector3(hw, 0f, 0f));
        for (float gx = -hw + .03f; gx < hw - .03f; gx += .025f) sb.Box(0, new Vector3(gx, 0f, -sd + .06f), new Vector3(gx + .008f, .006f, -.06f));
        sb.Box(1, new Vector3(-hw, 0f, -.05f), new Vector3(hw, .007f, 0f));                                   // żółta krawędź (od podstopnicy)
        sb.Box(1, new Vector3(-hw, 0f, -sd), new Vector3(-hw + .04f, .007f, -.05f)); sb.Box(1, new Vector3(hw - .04f, 0f, -sd), new Vector3(hw, .007f, -.05f));
        sb.Box(2, new Vector3(-hw, -rise - .02f, -.015f), new Vector3(hw, -.03f, .0f));                      // podstopnica (ryflowana pionowo)
        var mesh = sb.Zapisz("stopień schodów ruchomych");
        var mats = new[] { SalonMatOsobny("bieżnia stopnia", new Color(.32f, .33f, .35f), .5f, .8f), SalonMatOsobny("żółta krawędź stopnia", new Color(.95f, .78f, .05f), .5f, 0f), SalonMatOsobny("podstopnica", new Color(.25f, .26f, .27f), .4f, .7f) };
        var korzen = new GameObject("Salon: schody ruchome (w górę)").transform; korzen.SetParent(Group("Osiedle A / salon — ruchome"), false); korzen.position = new Vector3(cx, 0, 0);
        var stopnie = new Transform[n];
        for (int k = 0; k < n; k++) { var g = SalonObiekt("stopień " + (k + 1), korzen, mesh, mats, Vector3.zero); g.transform.localPosition = sciezka[0]; stopnie[k] = g.transform; }
        // poręcze: pas gumowy wzdłuż balustrady (dolna pętla → pochyłość → górna pętla), tekstura z przewijanym szwem
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, true) { name = "salon poręcz", wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 64; y++) for (int x = 0; x < 8; x++) tex.SetPixel(x, y, y < 4 ? new Color(.22f, .22f, .23f) : new Color(.04f, .04f, .045f));
        tex.Apply(); string tp = Out + "/salon_porecz_tekstura.asset"; if (UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(tp)) UnityEditor.AssetDatabase.DeleteAsset(tp); UnityEditor.AssetDatabase.CreateAsset(tex, tp);
        var mP = new Material(Shader.Find("Standard")) { name = "Salon: poręcz gumowa" }; mP.mainTexture = tex; mP.SetFloat("_Glossiness", .45f); SalonZapisz(mP, "poręcz gumowa");
        var poreczPkt = new List<Vector3> { new Vector3(0, yb + hb - .35f, z0 + .9f), new Vector3(0, yb + hb, z0 + .5f), new Vector3(0, yb + hb, z1 + .3f), new Vector3(0, yb + H + hb, z2 + .3f), new Vector3(0, yb + H + hb, z3 - .4f), new Vector3(0, yb + H + hb - .35f, z3 - .8f) };
        foreach (float sx in new[] { -1f, 1f })
        {
            var pb = new SiatkaSalonu(); float px = sx * (sw * .5f + .06f), pw = .05f, ph = .03f, dl = 0f;
            for (int i = 1; i < poreczPkt.Count; i++)
            {
                var a = poreczPkt[i - 1] + Vector3.right * px; var b = poreczPkt[i] + Vector3.right * px; float d = Vector3.Distance(a, b);
                var dir = (b - a).normalized; var up = Vector3.Cross(Vector3.right, dir); if (up.y < 0) up = -up;
                Vector3 l = Vector3.left * pw, rr = Vector3.right * pw, u = up * ph;
                pb.Quad(0, a + l + u, a + rr + u, b + rr + u, b + l + u, up, 0, 1, dl, dl + d);
                pb.Quad(0, a + l - u, b + l - u, b + l + u, a + l + u, Vector3.left, 0, 1, dl, dl + d);
                pb.Quad(0, a + rr - u, a + rr + u, b + rr + u, b + rr - u, Vector3.right, 0, 1, dl, dl + d);
                pb.Quad(0, a + l - u, a + rr - u, b + rr - u, b + l - u, -up, 0, 1, dl, dl + d);
                dl += d;
            }
            var pm = pb.Zapisz("poręcz " + (sx < 0 ? "lewa" : "prawa"));
            SalonObiekt("poręcz " + (sx < 0 ? "lewa" : "prawa"), korzen, pm, new[] { mP }, korzen.position);
        }
        var sr = korzen.gameObject.AddComponent<BraxSchodyRuchome>();
        sr.stopnie = stopnie; sr.sciezka = sciezka; sr.predkosc = .5f * K; sr.porecze = new[] { mP }; sr.uvNaMetr = 1f; sr.szerokosc = sw;
        salonSchody = sr;
        SaDetal("schody ruchome", "stopnie ruchome z żółtymi krawędziami, rowkami i podstopnicą", n);
        SaDetal("schody ruchome", "poręcze gumowe przewijane (UV), napęd z dźwiękiem", 2);
        Debug.Log("BRAXHORN_SALON_SCHODY_RUCHOME stopnie=" + n + " długość_ścieżki=" + L.ToString("F2") + " m nachylenie=35° wysokość=" + H.ToString("F2") + " prędkość=" + (.5f * K).ToString("F2") + " m/s z " + z0.ToString("F2") + "…" + z3.ToString("F2") +
                  " schody_stałe: stopnie=23 podstopnica=" + ((SF1 - SYF) / 24f).ToString("F3") + " głębokość=0.35");
    }

    // ---------------- zaplecze ----------------
    static void SalonZaplecze()
    {
        group = "Osiedle A / salon — zaplecze";
        var utw = Pbr("salon: kostka zaplecza", "concrete_pavers_03", 1.4f, new Color(.72f, .72f, .71f));
        // utwardzenie: pas zachodni wzdłuż fasady (x0+0.1 … bryła) i tył (z −63.4 … bryła)
        Plyta(utw, SAX0 + .1f, SBX0, SAZ0 + .6f, SBZ1); Plyta(utw, SBX0, SAX1, SAZ0 + .6f, SBZ0);
        SaDetal("zaplecze", "utwardzenie z kostki", 2);
        // kontenery 1100 l: korpus, pokrywa z zawiasem, uchwyty, 4 koła, rant
        var ziel = SaMat("kontener zielony", new Color(.12f, .28f, .16f), .35f); var szary = SaMat("kontener szary", new Color(.33f, .34f, .35f), .35f);
        int kk = 0;
        foreach (var (cx, m) in new[] { (-48.0f, ziel), (-46.4f, szary) })
        {
            float cz = -61.6f, w = .68f, d = .55f, h = 1.2f;
            BoxMinMax(m, cx - w, cx + w, .35f, h, cz - d, cz + d, true);
            BoxMinMax(m, cx - w - .03f, cx + w + .03f, h, h + .06f, cz - d - .06f, cz + d + .02f, false);                  // pokrywa
            BoxMinMax(Steel, cx - w, cx + w, h - .1f, h - .06f, cz + d, cz + d + .04f, false);                             // zawias
            foreach (float hx in new[] { cx - w - .06f, cx + w + .02f }) BoxMinMax(Steel, hx, hx + .04f, .9f, 1.0f, cz - .3f, cz + .3f, false);   // uchwyty
            BoxMinMax(m, cx - w - .02f, cx + w + .02f, .33f, .38f, cz - d - .02f, cz + d + .02f, false);                   // rant dolny
            foreach (float wx in new[] { cx - w + .12f, cx + w - .12f }) foreach (float wz in new[] { cz - d + .12f, cz + d - .12f })
            { Tube(Steel, new Vector3(wx, .33f, wz), new Vector3(wx, .2f, wz), .02f, 6); Tube(SaMat("koło gumowe", new Color(.05f, .05f, .05f), .2f), new Vector3(wx - .04f, .1f, wz), new Vector3(wx + .04f, .1f, wz), .1f, 12); }
            kk++;
        }
        SaDetal("zaplecze", "kontenery na odpady (pokrywa, zawias, uchwyty, koła)", kk);
        // murek z bloczków z fugą i czapą (jak przy stacji) od Wiśniowej do stacji, z −63.7
        var blok = Pbr("stacja: bloczek betonowy", "concrete_pavement_02", 1.2f, new Color(.72f, .72f, .7f)); var fug = SaMat("fuga murku", new Color(.35f, .35f, .34f), .1f);
        float c = SAZ0 + .3f, a0 = SAX0 + .2f, a1 = SAX1 + .05f, hh = 1.2f * K, b = .4f * K;
        BoxMinMax(fug, a0, a1, 0, hh, c - .1f, c + .1f, true);
        int rz = 0;
        for (float y = 0; y < hh - .01f; y += .2f * K, rz++)
            for (float a = a0 + (rz % 2) * b * .5f; a < a1 - .01f; a += b)
            {
                float s = Mathf.Max(a0, a), e = Mathf.Min(a1, a + b - .01f), y1 = Mathf.Min(hh, y + .2f * K - .01f);
                if (e - s > .02f) BoxMinMax(blok, s, e, y, y1, c - .105f, c + .105f, false);
            }
        BoxMinMax(Concrete, a0 - .03f, a1 + .03f, hh, hh + .08f, c - .16f, c + .16f, false);
        SaDetal("zaplecze", "murek z bloczków z fugą + czapa");
    }

    static void SalonPunkt()
    {
        var w = Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(q => q.area == "estate");
        if (!w) return;
        w.points = (w.points ?? new BraxCityPoint[0]).Concat(new[] { new BraxCityPoint { id = "salon_samochodowy", label = "Salon samochodowy (B1 / Wiśniowa)", position = new Vector3(-42.4f, .2f, -29.2f) } }).ToArray();
    }

    // ---------------- auta (z etapu ruchu, Auta()) ----------------
    static void SalonAuta(List<AutoSzablon> szablony)
    {
        if (!SalonWlaczony || szablony == null || szablony.Count == 0) return;
        group = "Osiedle A / salon — auta";
        var rnd = new System.Random(5150);
        string[] pierwszenstwo = { "car_918", "car_r8", "car_rev", "car_mus", "car_s4", "car_dc", "car_gmc" };   // bez dostawczaka (Żuk)
        var kolej = pierwszenstwo.Select(id => szablony.FirstOrDefault(s => s.id == id)).Where(s => s != null).ToList();
        if (kolej.Count == 0) kolej = szablony.Where(s => s.id != "car_zuk").ToList();
        if (kolej.Count == 0) { Debug.Log("BRAXHORN_SALON auta=0 (brak szablonów)"); return; }
        var miejsca = new List<(Vector3 c, Vector3 f, float dlMax, string gdzie, Transform rodzic)>();
        for (int i = 0; i < SalonTalerze.Length; i++)
            miejsca.Add((new Vector3(SalonTalerze[i].x, SYF + .06f + .12f, SalonTalerze[i].z), new Vector3(1f, 0f, -1f).normalized, 2f * Mathf.Sqrt(Mathf.Pow(STalerzR - .1f, 2) - 1.3f * 1.3f), "talerz obrotowy", i < salonTalerzeObj.Count ? salonTalerzeObj[i] : null));
        foreach (var (ax, az) in SalonAntresolaAuta) miejsca.Add((new Vector3(ax, SF1 + .155f, az), Vector3.forward, 6.4f, "antresola", null));
        foreach (float x in SalonPlacX) miejsca.Add((new Vector3(x, .14f, SalonPlacZ), Vector3.forward, 6.4f, "plac", null));
        int ok = 0, k = 0; var opis = new List<string>();
        foreach (var m in miejsca)
        {
            AutoSzablon sz = null;
            for (int p = 0; p < kolej.Count && sz == null; p++) { var c = kolej[(k + p) % kolej.Count]; if (c.dl <= m.dlMax) sz = c; }
            k++;
            if (sz == null) { opis.Add(m.gdzie + ": za długie"); continue; }
            var o = AutoInstancja(sz, Group(group), "Salon · " + m.gdzie + " · " + sz.id + (AutaNC.Contains(sz.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), rnd, out var kolor);
            o.transform.SetPositionAndRotation(m.c, Quaternion.LookRotation(m.f));
            var nad = o.transform.Find("nadwozie");
            if (nad)
                foreach (var t in nad.Cast<Transform>().Where(t => t.name == "spaliny" || t.name.StartsWith("światło") || t.name.StartsWith("kierunkowskaz")).ToList())
                    Object.DestroyImmediate(t.gameObject);
            var bc = o.AddComponent<BoxCollider>(); bc.center = sz.obrys.center; bc.size = sz.obrys.size;
            LodAuta(o, .02f);
            if (m.rodzic) o.transform.SetParent(m.rodzic, true);                                   // obraca się z talerzem
            ok++; opis.Add(m.gdzie + ": " + sz.id + " " + kolor + " dł " + sz.dl.ToString("F2"));
        }
        Debug.Log("BRAXHORN_SALON auta=" + ok + "/" + miejsca.Count + " [" + string.Join("; ", opis) + "]");
    }

    // ---------------- obsada (po kroku ludzi) ----------------
    static readonly Rola RolaSprzedawca = new Rola { nazwa = "sprzedawca aut", postaci = new[] { "Business_Male", "Business_Female" }, odcien = false };
    static void SalonObsada()
    {
        if (!SalonWlaczony || salonObsada.Count == 0) return;
        if (rbPostacie == null || rbPostacie.Count == 0) { Debug.Log("BRAXHORN_SALON obsada=0 (brak paczki Rocketbox)"); return; }
        var rnd = new System.Random(5151); int n = 0;
        foreach (var (p, yaw, kto) in salonObsada)
        {
            BraxMieszkaniec m = kto == "klient"
                ? NowyMieszkaniec(RolaPrzechodzien, Group("Osiedle A / salon — ludzie"), "salon — klient", p, yaw, rnd, KlipyPostoj)
                : Obsada("salon — " + kto, kto == "doradca serwisu" ? RolaObsluga : RolaSprzedawca, kto, p, yaw, rnd, new[] { "documents_idle", "idle_neutral_01", "gestic_talk_neutral_01" }, p.y);
            if (m) n++;
        }
        int pas = 0;
        if (salonSchody && salonSchody.sciezka != null && salonSchody.sciezka.Length > 0)
        {
            var start = salonSchody.transform.TransformPoint(salonSchody.sciezka[0]);
            var m = NowyMieszkaniec(RolaPrzechodzien, Group("Osiedle A / salon — ludzie"), "salon — jedzie schodami ruchomymi", start, 180f, rnd, KlipyPostoj);
            if (m) { salonSchody.pasazer = m.transform; pas = 1; }
        }
        Debug.Log("BRAXHORN_SALON obsada=" + n + "/" + salonObsada.Count + " pasażer_schodów=" + pas + " (sprzedawcy 2 na piętrze, kierownik 1, barista 1, doradca serwisu 1, klienci 2)");
    }
}

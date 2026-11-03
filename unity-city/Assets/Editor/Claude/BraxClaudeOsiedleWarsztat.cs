using System.Collections.Generic;
using UnityEngine;

// WARSZTAT (build 45b, E32; właściciel 21:16 o blaszanych garażach „POZNAŃSKA · GARAŻE”: „te kontenery rozjebane — nie pasują”).
// 4 garaże-kontenery Codexa (x 23.5…46.5, z −43.9…−34.1) i tablica na chodniku B1 (x 24…28, z −24.8) ZWALONE; na działce
// x 21.5…48.5, z −58…−25.5 murowany warsztat 24 × 12 m (3 stanowiska z bramami segmentowymi 3 × 3.5 m × K: jedna podniesiona,
// podnośnik dwukolumnowy z ramionami, opony w stojaku, wózek narzędziowy, stół warsztatowy; biuro przyjęć z jednymi drzwiami,
// ladą, regałem, gaśnicą i znakiem WYJŚCIE), plac: wjazd z B1, alejka manewrowa 7 m, 4 miejsca postojowe (dane dla aut
// zaparkowanych), mechanicy NPC (2). Detale: cokół, attyka z obróbką, gzyms, opaski i parapety okien, rynny z rurami spustowymi,
// segmenty bram z fugą (szczelina pokazuje podkład), krawężniki z fugą co 1 m × K, odwodnienie liniowe przed bramami, kratka,
// oznakowanie miejsc w wycięciach. Auto NA podnośniku — nie w tym kroku (szablony aut powstają dopiero w etapie ruchu).
public static partial class BraxClaudeOsiedle
{
    static readonly bool WarsztatWlaczony = true;
    const float WX0 = 21.5f, WX1 = 48.5f, WZ0 = -58f, WZ1 = -25.5f;
    static readonly Rola RolaMechanik = new Rola { nazwa = "mechanik", postaci = new[] { "Male_Adult" }, kolor = new Color(.12f, .2f, .38f) };
    static readonly List<(Vector3 p, float yaw)> warsztatMechanicy = new List<(Vector3, float)>();

    static void Warsztat(Transform estate, Transform physics)
    {
        if (!WarsztatWlaczony) { Debug.Log("BRAXHORN_WARSZTAT wyłączony flagą"); return; }
        warsztatMechanicy.Clear();
        int detale0 = stDetale; stDetaleOpis.Clear();
        var g0 = group; group = "Osiedle A / warsztat";
        // 1) zwalenie: kontenery, tablica, geometria generatora i kolizje w obrysie (podłoga y ≤ 0 zostaje)
        var vol = MinMax(WX0, WX1, .01f, 14f, WZ0, Mathf.Min(WZ1, KrawedzieB1().zChod - .05f));   // do 5 cm przed chodnikiem B1 (E3a)
        int przed = rep.oldFenceTrianglesRemoved;
        if (estate) CutVolumes(estate, physics, new List<Bounds> { vol, MinMax(23.9f, 28.1f, .01f, 2.7f, -24.95f, -24.65f) });
        int troj = rep.oldFenceTrianglesRemoved - przed, kol = 0, gen = 0;
        var e = vol; e.Expand(.6f);
        foreach (var r0 in new[] { estate, physics, colliders })
        {
            if (!r0) continue;
            foreach (var c in r0.GetComponentsInChildren<Collider>(true))
            {
                if (!c || c is MeshCollider) continue; var b = c.bounds;
                if (b.max.y < .12f || b.size.x > 60f || b.size.z > 60f || !e.Contains(b.min) || !e.Contains(b.max)) continue;
                Object.DestroyImmediate(c); kol++;
            }
        }
        gen += WytnijZGrupy((gr, m) => !gr.StartsWith("Podłoże") && !gr.StartsWith("Osiedle A / warsztat") && !gr.StartsWith("Osiedle A / stacja") && !gr.StartsWith("Osiedle A / myjnia") && !gr.StartsWith("Osiedle A / drogi") && gr != GrupaFarbyA, vol);
        if (root)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r && r.bounds.max.y > .05f && e.Contains(r.bounds.min) && e.Contains(r.bounds.max)) { Object.DestroyImmediate(r.gameObject); gen++; }
        Debug.Log("BRAXHORN_WARSZTAT zwalenie trójkąty=" + troj + " kolizje=" + kol + " generator=" + gen + " (4 garaże-kontenery Codexa + tablica POZNAŃSKA · GARAŻE)");

        // 2) nawierzchnia: zjazd z B1, plac (alejka + miejsca), krawężniki z fugą, odwodnienie, oznakowanie w wycięciach
        group = GrupaStacjiAsf;
        const float dj0 = 22f, dj1 = 28.5f; var (zKr, zKr1, zChod) = KrawedzieB1();   // E3a: krawędzie B1 po poszerzeniu
        var wyc = MinMax(dj0, dj1, -.1f, .6f, zChod - .02f, zKr + .02f);
        WytnijZGrupy((gr, m) => gr == "Osiedle A / drogi od nowa" && m != Asphalt, wyc);
        PodzielKolizje(colliders, wyc);
        for (float x = dj0; x < dj1 - .01f; x += K)
        {
            float xk = Mathf.Min(dj1, x + K - .01f);
            BoxMinMax(KerbStone, x, xk, YS - .03f, YS + .03f, zKr1, zKr, false);
            if (xk < dj1 - .005f) { BoxMinMax(StMat("fuga krawężnika", new Color(.42f, .42f, .41f), .1f), xk, xk + .01f, YS - .03f, YS + .025f, zKr1 + .005f, zKr - .005f, false); stKraweznikiFugi++; }
        }
        Top(Asphalt, dj0, dj1, zChod, zKr1, YS + .02f);
        AsfaltProst(WX0, WX1, -42f, -28.5f); AsfaltProst(dj0, dj1, -28.5f, zChod);
        KrawFugi(true, -28.4f, WX0, dj0 - .05f); KrawFugi(true, -28.4f, dj1 + .05f, WX1);
        KrawFugi(false, WX0 + .1f, -42f, -28.5f); KrawFugi(false, WX1 - .1f, -42f, -28.5f);
        OdplywLiniowy(29.5f, 47f, -41.6f, true);   // przed bramami, w asfalcie placu (plac do z −42)
        Kratka(23.5f, -40f); Studzienka(44f, -37.5f);
        // 4 miejsca postojowe 2.5 × 5 m × K przodem do alejki (z −35…−28.5), linie w wycięciach; dane dla etapu aut
        float sw = 2.5f * K, sl = 5f * K, sz0 = -28.6f - sl;
        for (int k = 0; k <= 4; k++) ZnakWycieciu(29f + k * sw - .06f, 29f + k * sw + .06f, sz0, -28.7f);
        for (int k = 0; k < 4; k++)
            MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = new Vector3(29f + (k + .5f) * sw, YS, sz0 + sl * .5f), przod = Vector3.back, rozmiar = new Vector2(sw, sl), przyScianie = true });
        StDetal("warsztat", "plac: miejsca w wycięciach", 4);

        // 3) budynek
        group = "Osiedle A / warsztat — budynek";
        float x0 = 23f, x1 = 47f, z0 = -54f, z1 = -42f, yE = 4.3f * K, yA = yE + .6f; const float t = .3f;
        var mur = Pbr("warsztat: tynk", "white_stucco_02", 2.5f, new Color(.86f, .87f, .86f));
        var obr = StMat("obróbka blacharska", new Color(.62f, .63f, .65f), .5f, .6f); var pas = StMat("pas wiaty grafit", new Color(.16f, .17f, .19f), .5f, .3f);
        var posadz = StMat("posadzka żywiczna warsztatu", new Color(.45f, .47f, .48f), .65f);
        float xb = 29.5f, bw = (x1 - t - xb) / 3f, gw = 3f * K, gh = 3.5f * K, dzA = 25.1f, dzB = dzA + 1f * K;
        // ściana frontowa z otworami bram i drzwi biura, tył i boki pełne, ściana biuro/hala
        float okY0 = 1.0f * K, okY1 = 2.3f * K, wz0 = -49.5f, wz1 = -46.5f;   // okno biura w ścianie zachodniej (otwór w murze)
        BoxMinMax(mur, x0, x1, 0, yA, z0, z0 + t); BoxMinMax(mur, x1 - t, x1, 0, yA, z0 + t, z1);
        BoxMinMax(mur, x0, x0 + t, 0, yA, z0 + t, wz0); BoxMinMax(mur, x0, x0 + t, 0, yA, wz1, z1); BoxMinMax(mur, x0, x0 + t, 0, okY0, wz0, wz1); BoxMinMax(mur, x0, x0 + t, okY1, yA, wz0, wz1);
        BoxMinMax(mur, xb - t, xb, 0, yE, z0 + t, z1 - t);
        var otw = new List<(float a, float b, float h)> { (dzA, dzB, 2.1f * K) };
        for (int k = 0; k < 3; k++) { float c = xb + (k + .5f) * bw; otw.Add((c - gw * .5f, c + gw * .5f, gh)); }
        float px0 = x0 + t;
        foreach (var (a, b, h) in otw)
        {
            if (a > px0 + .01f) BoxMinMax(mur, px0, a, 0, yA, z1 - t, z1);
            BoxMinMax(mur, a, b, h, yA, z1 - t, z1);
            px0 = b;
        }
        BoxMinMax(mur, px0, x1 - t, 0, yA, z1 - t, z1);
        BoxMinMax(mur, x0 + t, xb - t, yE, yE + .25f, z0 + t, z1 - t, false, true);   // strop biura
        BoxMinMax(mur, xb, x1 - t, yE, yE + .25f, z0 + t, z1 - t, false, true);         // strop hali
        BoxMinMax(Plinth, x0 - .04f, x1 + .04f, 0, .45f, z0 - .04f, z0, false); BoxMinMax(Plinth, x0 - .04f, x0, 0, .45f, z0, z1 + .04f, false); BoxMinMax(Plinth, x1, x1 + .04f, 0, .45f, z0, z1 + .04f, false);
        ObwodBez(obr, x0, x1, z0, z1, t, yA, yA + .05f, .04f);
        BoxMinMax(pas, x0 - .06f, x1 + .06f, yE - .1f, yA, z1, z1 + .06f, false);   // 3.11 (premium): pas grafitowy frontu od gzymsu po attykę — pod szyldy
        BoxMinMax(StMat("membrana dachu", new Color(.35f, .36f, .37f), .2f), x0 + t, x1 - t, yE + .25f, yE + .3f, z0 + t, z1 - t, false);
        StDetal("warsztat", "cokół, attyka z obróbką, gzyms frontu");
        // okno biura w ścianie bocznej zachodniej (opaska, parapet zewnętrzny, rama, szyba)
        {
            float f = x0;
            BoxMinMax(Frame, f - .02f, f + t + .02f, okY0, okY0 + .06f, wz0, wz1, false);   // rama dół (w ościeżu)
            BoxMinMax(Glass, f + .12f, f + .14f, okY0 + .06f, okY1 - .06f, wz0 + .06f, wz1 - .06f, false);
            BoxMinMax(Frame, f + .1f, f + .16f, okY1 - .06f, okY1, wz0, wz1, false); BoxMinMax(Frame, f + .1f, f + .16f, okY0, okY1, wz0, wz0 + .06f, false); BoxMinMax(Frame, f + .1f, f + .16f, okY0, okY1, wz1 - .06f, wz1, false);
            BoxMinMax(obr, f - .1f, f, okY0 - .05f, okY0, wz0 - .05f, wz1 + .05f, false);    // parapet zewnętrzny
            BoxMinMax(pas, f - .03f, f, okY0, okY1 + .12f, wz0 - .12f, wz0, false); BoxMinMax(pas, f - .03f, f, okY0, okY1 + .12f, wz1, wz1 + .12f, false); BoxMinMax(pas, f - .03f, f, okY1, okY1 + .12f, wz0, wz1, false);   // opaska
        }
        StDetal("warsztat", "okno biura: rama, parapet, opaska");
        // bramy segmentowe: 1. podniesiona (pakiet pod stropem), 2. i 3. zamknięte — segmenty 0.5 m × K z fugą (podkład z tyłu)
        var seg = StMat("segment bramy", new Color(.88f, .89f, .9f), .55f, .3f); var pod = StMat("podkład paneli", new Color(.12f, .12f, .13f), .2f);
        for (int k = 0; k < 3; k++)
        {
            float c = xb + (k + .5f) * bw, a = c - gw * .5f, b = c + gw * .5f;
            foreach (float gx in new[] { a - .1f, b }) BoxMinMax(Steel, gx, gx + .1f, 0, gh, z1 - t - .12f, z1 - t, false);   // prowadnice
            BoxMinMax(obr, a - .12f, b + .12f, gh, gh + .12f, z1 - .02f, z1 + .04f, false);                                // opaska nad bramą
            BoxMinMax(Concrete, a, b, 0, .15f, z1 - t, z1, true);                                                           // próg bramy w grubości muru (bez dziury do trawy)
            if (k == 0 || k == 2)   // 1: warsztat (podnośnik), 3: wulkanizacja — podniesione
            {
                BoxMinMax(seg, a, b, gh - .05f, gh, z1 - t - 4.2f, z1 - t - .1f, false, true);                              // pakiet pod stropem
                BoxMinMax(Steel, a + .3f, a + .4f, gh, yE, z1 - t - 4.3f, z1 - t - 4.2f, false);
            }
            else
            {
                BoxMinMax(pod, a + .005f, b - .005f, .02f, gh - .005f, z1 - t - .1f, z1 - t - .06f, true);   // podkład za segmentami (bez wspólnych lic)
                for (float y = .02f; y < gh - .01f; y += .5f * K) BoxMinMax(seg, a, b, y, Mathf.Min(gh, y + .5f * K - .012f), z1 - t - .06f, z1 - t - .02f, false);
                BoxMinMax(Frame, c - .25f, c + .25f, .9f, .97f, z1 - t - .02f, z1 - t, false);                              // uchwyt
            }
        }
        StDetal("warsztat", "bramy segmentowe z fugą segmentów", 3);
        // drzwi biura: ościeżnica, skrzydło uchylone do środka, próg; znak WYJŚCIE nad drzwiami od środka
        BoxMinMax(Frame, dzA - .08f, dzA, 0, 2.1f * K + .08f, z1 - t, z1, false); BoxMinMax(Frame, dzB, dzB + .08f, 0, 2.1f * K + .08f, z1 - t, z1, false);
        BoxMinMax(Frame, dzA - .08f, dzB + .08f, 2.1f * K, 2.1f * K + .08f, z1 - t, z1, false);
        BoxMinMax(StMat("drzwi biura", new Color(.2f, .22f, .25f), .5f, .3f), dzA, dzA + .05f, .02f, 2.1f * K - .02f, z1 - t - 1f * K, z1 - t, false);
        BoxMinMax(StMat("próg aluminiowy", new Color(.6f, .61f, .63f), .5f, .7f), dzA, dzB, 0, .17f, z1 - t, z1, true);
        StDetal("warsztat", "drzwi biura: ościeżnica, skrzydło, próg");
        // rynny i rury spustowe (tył, z lejami pod attyką)
        foreach (float rx in new[] { x0 + .5f, (x0 + x1) * .5f, x1 - .5f })
        {
            BoxMinMax(obr, rx - .12f, rx + .12f, yA - .45f, yA - .2f, z0 - .25f, z0, false);
            Tube(obr, new Vector3(rx, yA - .45f, z0 - .12f), new Vector3(rx, .25f, z0 - .12f), .05f, 10);
            Tube(obr, new Vector3(rx, .25f, z0 - .12f), new Vector3(rx, .08f, z0 - .4f), .05f, 10);
        }
        StDetal("warsztat", "leje i rury spustowe", 3);
        // 3.11 (właściciel: warsztat szary, małe napisy): szyldy z grafik właściciela na pasie grafitowym; bez grafik — litery
        // 3.11 16:19: cały budynek = konkurencja WAWER (WAWER AUTO SERWIS + WAWER WULKANIZACJA); BRAXHORN SERWIS w zapasie
        if (!SzyldPanel("wawer_serwis", new Vector3(xb + bw, yE + .25f, z1 + .06f), Vector3.forward, 1.98f, .66f) && !SzyldPanel("warsztat_szyld", new Vector3(xb + bw, yE + .25f, z1 + .06f), Vector3.forward, 1.98f, .66f))
            Napis3D("WARSZTAT", new Vector3(xb + bw, yE + .3f, z1 + .06f), Vector3.forward, .26f, .05f, StSwiatlo("napis wiaty", new Color(.95f, .97f, 1f)), pas, .15f, .14f, true);
        // 3.11 16:15 (właściciel: „porób konkurencję”, „Kruk będzie”, 16:17 „Wawer konkurencja jednak”): wulkanizacja = WAWER (konkurencja obok BRAXHORN SERWIS);
        // szyld z grafiki wawer_wulkanizacja, bez niej — litery WAWER WULKANIZACJA (żółte na granacie)
        if (!SzyldPanel("wawer_wulkanizacja", new Vector3(xb + bw * 2.5f, yE + .25f, z1 + .06f), Vector3.forward, 1.98f, .66f))
            Napis3D("WAWER WULKANIZACJA", new Vector3(xb + bw * 2.5f, yE + .3f, z1 + .06f), Vector3.forward, .17f, .05f, StSwiatlo("napis WAWER żółty", new Color(1f, .82f, .12f)), StMat("pas WAWER granat", new Color(.05f, .09f, .22f), .5f, .3f), .15f, .14f, true);
        if (false)
            Napis3D("WULKANIZACJA", new Vector3(xb + bw * 2.5f, yE + .3f, z1 + .06f), Vector3.forward, .2f, .05f, StSwiatlo("napis wiaty", new Color(.95f, .97f, 1f)), pas, .15f, .14f, true);
        Napis3D("BIURO", new Vector3((x0 + xb) * .5f, yE + .32f, z1 + .06f), Vector3.forward, .2f, .04f, StSwiatlo("napis wiaty", new Color(.95f, .97f, 1f)), pas, .15f, .14f, true);

        // 4) wnętrze: posadzka, biuro przyjęć, hala (podnośnik, opony, wózek, stół)
        group = "Osiedle A / warsztat — wnętrze";
        // płyta betonowa do 0.13, na niej warstwa wykończenia (lico posadzki jedno — bez dwóch płaszczyzn na tej samej wysokości)
        wkKola = wkOpony = wkNapisy = wkDetale = 0; wkOpis.Clear();
        float cKan = xb + bw * 1.5f;
        WarsztatKanalWytnij(estate, physics, cKan);                                       // kanał w stanowisku 2 (wycięcie gruntu i kolizji)
        BoxMinMax(Concrete, x0 + t, xb, 0, .13f, z0 + t, z1 - t, true);
        BoxMinMax(Pbr("stacja: płytki sklepu", "large_grey_tiles", 1.2f, new Color(.96f, .96f, .96f)), x0 + t, xb - t, .13f, .17f, z0 + t, z1 - t, true);
        WarsztatPosadzka(xb, x1 - t, z0 + t, z1 - t, cKan, posadz);                     // hala z otworem kanału i pasem żółto-czarnym
        WarsztatKanalBuduj(cKan);
        var listwa = StMat("listwa przypodłogowa", new Color(.3f, .3f, .31f), .4f);
        BoxMinMax(listwa, x0 + t, xb - t, .17f, .25f, z0 + t, z0 + t + .015f, false); BoxMinMax(listwa, x0 + t, x0 + t + .015f, .17f, .25f, z0 + t, z1 - t, false);
        // biuro: lada przyjęć, krzesło, regał z segregatorami, gaśnica, znak wyjścia, oprawa
        BoxMinMax(StMat("lada front", new Color(.85f, .86f, .87f), .5f), x0 + 1.4f, xb - 1.2f, .17f, 1.25f, -49.2f, -48.5f, true);
        BoxMinMax(StMat("blat lady", new Color(.2f, .2f, .22f), .7f), x0 + 1.35f, xb - 1.15f, 1.25f, 1.3f, -49.25f, -48.45f, false);
        BoxMinMax(StSwiatlo("ekran kasy", new Color(.4f, .7f, 1f)), x0 + 2.4f, x0 + 2.9f, 1.3f, 1.65f, -49.1f, -49.07f, false);
        Regal(x0 + t + .02f, xb - t - .4f, z0 + t + .02f, z0 + t + .6f, .17f, false);
        float gsx = xb - t - .02f;
        Cylinder(StMat("gaśnica czerwona", new Color(.75f, .05f, .05f), .7f), new Vector3(gsx - .14f, .7f, -44.2f), .08f * K, .5f * K, 14, true, false);
        BoxMinMax(Steel, gsx - .03f, gsx, .95f, 1.1f, -44.3f, -44.1f, false);
        var ziel = StSwiatlo("znak wyjścia", new Color(.05f, .7f, .25f));
        BoxMinMax(ziel, (dzA + dzB) * .5f - .4f, (dzA + dzB) * .5f + .4f, 2.1f * K + .25f, 2.1f * K + .5f, z1 - t - .04f, z1 - t, false);
        Napis3D("WYJŚCIE", new Vector3((dzA + dzB) * .5f, 2.1f * K + .3f, z1 - t - .04f), Vector3.back, .13f, .01f, StSwiatlo("napis wyjścia", new Color(1f, 1f, 1f)), ziel, .16f, .12f, true);
        var opr = StSwiatlo("oprawa sklepu", new Color(1f, .99f, .95f));
        foreach (var (ox, oz) in new[] { ((x0 + xb) * .5f, -48f), (xb + bw * .5f, -48f), (xb + bw * 1.5f, -48f), (xb + bw * 2.5f, -48f) })
        { BoxMinMax(StMat("ramka oprawy", new Color(.5f, .5f, .52f), .5f, .6f), ox - .7f, ox + .7f, yE - .03f, yE, oz - 1.2f, oz + 1.2f, false, true); BoxMinMax(opr, ox - .65f, ox + .65f, yE - .035f, yE - .03f, oz - 1.15f, oz + 1.15f, false, true); }
        StDetal("warsztat", "biuro: lada, regał, gaśnica, WYJŚCIE, oprawy");
        // hala: strefa WARSZTAT (stanowiska 1–2) i strefa WULKANIZACJA (stanowisko 3) — BraxClaudeOsiedleWulkanizacja.cs
        float c1 = xb + bw * .5f;
        warsztatMechanicy.Add((new Vector3(c1 - 1.6f, .15f, -45.6f), 90f));                       // mechanik przy aucie na podnośniku
        Guard("warsztat: wyposażenie stref (podnośnik, kanał, narzędzia, wulkanizacja, koła)", () => WarsztatWyposazenie(x0, x1, z0, z1, xb, bw, t, yE));
        Guard("warsztat: złom na zapleczu", WarsztatZlom);
        group = "Osiedle A / warsztat — wnętrze";
        // punkt podróży „garages” (Codex) zostaje pod tym samym id — teraz warsztat (wejście do biura)
        foreach (var w in Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.points != null) foreach (var p in w.points) if (p != null && p.id == "garages") { p.label = "Warsztat (Poznańska)"; p.position = new Vector3((dzA + dzB) * .5f, .1f, z1 + 1.5f); }
        var pk = new GameObject("BRAXHORN punkt interakcji: warsztat (przyjęcie auta)"); pk.transform.SetParent(Group(group), false); pk.transform.position = new Vector3((dzA + dzB) * .5f, YS, z1 + 1.5f);
        group = g0;
        Debug.Log("BRAXHORN_WARSZTAT obrys x " + WX0 + "…" + WX1 + " z " + WZ0 + "…" + WZ1 + " budynek=24.0×12.0 m stanowiska=3 bramy_segmentowe=3 (1 otwarta) brama=" + gw.ToString("F2") + "×" + gh.ToString("F2") + " m podnośnik=1 opony=10 biuro_drzwi=1 miejsca_postojowe=4 mechanicy=" + warsztatMechanicy.Count + " (auto na podnośniku: brak — szablony aut z etapu ruchu)");
        Debug.Log("BRAXHORN_DETALE obiekt=warsztat razem=" + (stDetale - detale0) + " [" + string.Join(", ", stDetaleOpis) + "]");
        Debug.Log("BRAXHORN_WARSZTAT_STREFY warsztat=stanowiska 1–2 (podnośnik dwukolumnowy z autem, kanał " + (1f * K).ToString("F2") + "×6.5 m głęb. " + (1.6f * K).ToString("F2") + ", narzędzia, olej, części, biuro przyjęć) wulkanizacja=stanowisko 3 (montażownica, wyważarka, podnośnik nożycowy, klucz pneumatyczny, stojaki, wanienka) mechanicy=" + warsztatMechanicy.Count);
        Debug.Log("BRAXHORN_KOLA rozmiar=205/55 R16 średnica=" + (2f * KoloRo).ToString("F3") + " m (0.632 × K) szerokość=" + KoloW.ToString("F3") + " koła_na_feldze=" + wkKola + " opony=" + wkOpony + " napisy_rozmiaru=" + wkNapisy + " (bieżnik: 3 rowki obwodowe + poprzeczne co 30°, felga 5 ramion, 5 nakrętek, piasta, rant)");
        Debug.Log("BRAXHORN_DETALE obiekt=warsztat+wulkanizacja razem=" + wkDetale + " [" + string.Join(", ", wkOpis) + "]");
    }

    static void WarsztatMechanicy()
    {
        if (!WarsztatWlaczony || warsztatMechanicy.Count == 0) return;
        if (rbPostacie == null || rbPostacie.Count == 0) { Debug.Log("BRAXHORN_WARSZTAT mechanicy=0 (brak paczki Rocketbox)"); return; }
        var rnd = new System.Random(3232); int n = 0;
        foreach (var (p, yaw) in warsztatMechanicy)
            if (Obsada("warsztat — mechanik", RolaMechanik, "mechanik", p, yaw, rnd, new[] { "work_mid", "idle_neutral_01", "idle_look_around_01" }, p.y)) n++;
        Debug.Log("BRAXHORN_WARSZTAT mechanicy=" + n + "/" + warsztatMechanicy.Count);
    }
}

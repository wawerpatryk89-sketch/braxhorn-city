using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// NOWE DZIELNICE ZE SKETCHFABA (etap „assety”, 2.11; plan: scratchpad/plan/ASSETY_PLAN.md + assety_mapa.png, układ liczy
// plan/assety_uklad.py). Ta sama klasa co ulice B (partial) — DROGI ZAWSZE NASZE I JEDNOLITE (właściciel 18:59): te same
// przekroje, asfalt, krawężnik, płyty, pasy zieleni, oznakowanie W WYCIĘCIACH nawierzchni, sygnalizacja na wysięgnikach co
// rondo, aleje i ul. Zachodnia. Z modeli dzielnic (abhayexe) zostały TYLKO budynki (drogi, chodniki, krawężniki, pasy,
// plamy, trawniki, auta, drzewa, drobnica — usunięte offline, lista w raporcie paczki).
// Układ (Unity, m; × K w wymiarach modeli):
//  • ALEJA CENTRALNA — przedłużenie ul. Zachodniej za pętlą ZACHÓD (otwarty wlot zachodni pętli), oś z 28.5, profil alei
//    (jezdnia 9 m, pasy zieleni z drzewami i latarniami, chodniki 5 m), od x −262 do ul. C3 (x −750);
//  • ulice N–S C0…C3 na osiach x −300/−450/−600/−750 (= linie kwadratów doczytywania 150 m — budynki nie leżą na granicach),
//    przekrój prosty (jezdnia 9 m, krawężnik, chodnik 5 m), od ul. Tramwajowej do ul. Nowojorskiej;
//  • ul. NOWOJORSKA (z 100.75) i ul. TRAMWAJOWA (z −62.25; na odcinku C1–C2 torowisko: szyny W WYCIĘCIACH asfaltu);
//  • kwartały z płyt: rząd A (z 40.5…91), rząd B (z 110.5…149.5), rząd C (z −52.5…16.5 z promenadą −3.5…16.5);
//    budynki licem w linii chodnika, cokół na poziomie płyt (y z promienia w dół), narożne licem w obu ulicach;
//  • ATLANTA (274 m) na zamknięciu osi alei za ul. C3; skwer zachodni (x −290…−262), pas zieleni na południu.
// Kolejność jak na budowie: 1) nawierzchnie i oznakowanie → 2) budynki → 3) wyposażenie (latarnie, drzewa, sygnalizacja,
// przystanki, hydranty, skrzynki, kontenery) → 4) rejestracja jezdni. Każdy krok w Krok() (błąd = log, dalej).
public static partial class BraxRegionMiastoB
{
    // ---------------- układ (z plan/assety_uklad.py) ----------------
    public const float NX = -262f;                                     // wschodnia krawędź nowego terenu (zach. krawędź placu pętli ZACHÓD)
    public static readonly float[] NC = { -300f, -450f, -600f, -750f }; // osie ulic N–S C0…C3
    public const float NN1 = 100.75f, NS1 = -62.25f;                   // ul. Nowojorska, ul. Tramwajowa
    public const float NPR = AH + Kr + Chod;                           // 9.75 — połowa ulicy prostej z chodnikiem
    public const float APR = AH + Kr + Ziel + Chod;                    // 12 — połowa alei z zielenią i chodnikiem
    public const float NZ1 = 156f, NXA = -870f;                        // północna krawędź terenu, zachodnia krawędź placu Atlanty
    public const float RzA0 = 40.5f, RzA1 = 91f, RzB0 = 110.5f, RzB1 = 149.5f, RzC0 = -52.5f, RzC1 = 16.5f, PromenadaZ = -3.5f;
    static readonly Rect TorRect = Rect.MinMaxRect(-600f + NPR, NS1 - AH, -450f - NPR, NS1 + AH);   // jezdnia ul. Tramwajowej z torowiskiem
    const float Rozstaw = 1.435f;                                      // rozstaw szyn (normalnotorowy) — × K w świecie Braxa

    sealed class Bud
    {
        public string id, paczka, plik, wezel; public float x, z, yaw, w, d;
        public Bud(string id, string model, string wezel, float x, float z, float yaw, float w, float d)
        {
            this.id = id; this.wezel = wezel; this.x = x; this.z = z; this.yaw = yaw; this.w = w; this.d = d; plik = model;
            paczka = model == "miasto_centrum_c5" ? "miasto_centrum" : model == "miasto_kamienice_c3" ? "miasto_kamienice" : "miasto_wiezowce";
        }
    }
    // budynki (plan/assety_uklad_cs.txt): id, model, węzeł jednostki, środek x/z, obrót, szerokość × głębokość lica (× K)
    static readonly Bud[] NoweBudynki =
    {
        new Bud("LONDON", "wiezowiec_london", null, -342.54f, 62.38f, 90f, 65.59f, 43.75f),
        new Bud("C5_U12", "miasto_centrum_c5", "U12", -412.59f, 62.10f, 0f, 55.31f, 43.21f),
        new Bud("C5_U08", "miasto_centrum_c5", "U08", -525.00f, 59.26f, 0f, 115.19f, 37.53f),
        new Bud("C5_U00", "miasto_centrum_c5", "U00", -634.89f, 59.67f, 0f, 50.28f, 38.35f),
        new Bud("C5_U03", "miasto_centrum_c5", "U03", -685.66f, 59.26f, 0f, 37.53f, 37.53f),
        new Bud("C3_U02", "miasto_kamienice_c3", "U02", -725.78f, 64.70f, 0f, 28.95f, 48.39f),
        new Bud("KINNAIRD", "kamienica_kinnaird", null, -337.59f, -24.09f, 0f, 55.67f, 41.18f),
        new Bud("C5_U07", "miasto_centrum_c5", "U07", -415.11f, -22.26f, 0f, 50.28f, 37.53f),
        new Bud("C5_U04", "miasto_centrum_c5", "U04", -483.48f, -22.26f, 0f, 47.46f, 37.53f),
        new Bud("C5_U09", "miasto_centrum_c5", "U09", -565.11f, -22.26f, 0f, 50.28f, 37.53f),
        new Bud("C5_U02", "miasto_centrum_c5", "U02", -628.51f, -22.26f, 0f, 37.53f, 37.53f),
        new Bud("C5_U05", "miasto_centrum_c5", "U05", -675.00f, -22.26f, 0f, 37.53f, 37.53f),
        new Bud("C5_U06", "miasto_centrum_c5", "U06", -721.49f, -22.26f, 0f, 37.53f, 37.53f),
        new Bud("C3_U05", "miasto_kamienice_c3", "U05", -352.08f, 130.00f, 90f, 84.66f, 39.00f),
        new Bud("C5_U01", "miasto_centrum_c5", "U01", -421.49f, 129.26f, 0f, 37.53f, 37.53f),
        new Bud("C3_U00", "miasto_kamienice_c3", "U00", -485.40f, 126.49f, 90f, 51.30f, 31.99f),
        new Bud("C3_U01", "miasto_kamienice_c3", "U01", -566.16f, 128.20f, 90f, 48.18f, 35.40f),
        new Bud("C5_U10", "miasto_centrum_c5", "U10", -628.51f, 129.26f, 0f, 37.53f, 37.53f),
        new Bud("C5_U11", "miasto_centrum_c5", "U11", -675.00f, 129.26f, 0f, 37.53f, 37.53f),
        new Bud("C5_U13", "miasto_centrum_c5", "U13", -721.49f, 129.26f, 0f, 37.53f, 37.53f),
        new Bud("ATLANTA", "wiezowiec_atlanta", null, -810.88f, 28.50f, 0f, 78.74f, 125.37f),
    };

    static int ndRootStart;                        // indeks pierwszego dziecka root utworzonego przez nowe dzielnice (kontrola migotania)
    static readonly List<string> ndUsuniete = new List<string>();
    static readonly List<(Rect r, int mat)> ndWstawkiToru = new List<(Rect, int)>();   // 0 farba, 1 główka szyny (stal), 2 rowek
    static readonly List<GameObject> ndBudynki = new List<GameObject>();
    static int ndMigotanie, ndLatarnie, ndDrzewa, ndZebry, ndWiezeMurowane;
    public static bool NoweDzielnice => true;     // teren nowych dzielnic (drogi i kwartały) budowany zawsze; modele — gdy są paczki

    static void NdLog(string s) => Debug.Log("BRAXHORN_MIASTO_ASSETY " + s);
    // każdy krok budowy osobno (zasada 5): błąd = log „DO POPRAWKI”, następne kroki idą dalej
    static void NdKrok(string nazwa, Action a)
    {
        try { a(); }
        catch (Exception e) { BraxMiastoAssety.Bledy++; Debug.LogError("BRAXHORN_MIASTO_ASSETY " + nazwa + " DO POPRAWKI: " + e); }
    }

    // =====================================================================================================================
    // 0) stare na terenie nowych dzielnic (zasada 11:00 „zdejmij stare, połóż nowe”) — dziś nic tam nie ma (grunt źródła kończy
    //    się na x −144), ale gdyby cokolwiek było: całe obiekty w obszarze precz, nachodzące — wycięte trójkąty; log z listą.
    static void NdUsunStare()
    {
        ndUsuniete.Clear();
        var v = Bx(NXA - 30f, NX - .3f, -3f, 400f, BraxMiastoGranice.Z0 - 4f, NZ1 + 4f);
        int obiekty = 0, siatki = 0, kolizje = 0;
        foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!g || Chroniony(g.transform)) continue;
            foreach (var r in g.GetComponentsInChildren<Renderer>(true).ToList())
            {
                if (!r || Chroniony(r.transform) || r is SkinnedMeshRenderer) continue;
                var b = r.bounds; if (!b.Intersects(v)) continue;
                if (v.Contains(b.min) && v.Contains(b.max))
                {
                    if (obiekty < 30) ndUsuniete.Add(Sciezka(r.transform));
                    if (r.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(r.gameObject); else UnityEngine.Object.DestroyImmediate(r);
                    obiekty++;
                }
            }
            try { siatki += BraxCityMeshCut.Subtract(g.transform, v, BraxRegion.Out + "/Cuts", "nowe_dzielnice"); } catch (Exception e) { ndUsuniete.Add("cięcie " + g.name + ": " + e.Message); }
            foreach (var c in g.GetComponentsInChildren<Collider>(true).ToList())
            {
                if (!c || !c.enabled || c.isTrigger || Chroniony(c.transform)) continue;
                var b = c.bounds; if (v.Contains(b.min) && v.Contains(b.max)) { c.enabled = false; kolizje++; }
            }
        }
        ndUsuniete.Add("teren nowych dzielnic (x " + NXA + "…" + NX + ", z " + BraxMiastoGranice.Z0 + "…" + NZ1 + "): obiekty " + obiekty + ", siatki wycięte " + siatki + ", kolizje wyłączone " + kolizje);
        usuniete.AddRange(ndUsuniete);
    }

    // =====================================================================================================================
    // 1) NAWIERZCHNIE: aleja, ulice C0–C3, ul. Nowojorska i Tramwajowa, skrzyżowania (asfalt + narożniki: krawężnik + płyty),
    //    kwartały z płyt, skwer, pas zieleni, plac Atlanty, ścieżka za rzędem B. Wszystko prostokątami stykającymi się krawędzią.
    static void NoweDzielniceNawierzchnie()
    {
        ndRootStart = root.childCount; ndLatarnie = ndDrzewa = ndZebry = ndMigotanie = ndWiezeMurowane = 0; BraxRegionFawela.wiez = BraxRegionFawela.wiezOkien = BraxRegionFawela.wiezTrojkatow = BraxRegionFawela.wiezMurowanych = BraxRegionFawela.wiezSzklanych = 0; ndWstawkiToru.Clear(); ndBudynki.Clear();
        NdKrok("usunięcie starego na terenie nowych dzielnic", NdUsunStare);
        NdKrok("nawierzchnie: aleja, ulice, skrzyżowania", NdUlice);
        NdKrok("nawierzchnie: kwartały, plac Atlanty, ciąg pieszy", NdKwartaly);
        NdKrok("teren: skwer zachodni, pas zieleni, peron tramwaju", NdZielen);
        NdKrok("wyposażenie ulic (latarnie, drzewa w kratach, ławki — na gotowym podłożu)", NdWyposazenieUlic);
        NdLog("nawierzchnie: aleja 4 odcinki, ulice C0–C3, Nowojorska, Tramwajowa, skrzyżowań " + NdSkrzyzowania().Count() + ", kwartały A/B/C, plac Atlanty, skwer, pas zieleni");
    }

    static void NdUlice()
    {
        // --- ALEJA (profil alei B — PrzekrojX2: asfalt odcinka, krawężniki, pasy zieleni z drzewami i latarniami, chodniki) ---
        // przerwy w zieleni: przejścia (zebry przy skrzyżowaniach i przy wlocie pętli), peron przystanku autobusowego
        float zebraA = AH + Kr + .6f + Mathf.Max(0f, RLuku - ZebraPrzedLukiem), zebraB = zebraA + 4f * K;   // zebra wlotu za łukiem narożnika (10.11): 7.5 … 12.68 m od środka
        var odcinki = new List<(float x0, float x1)> { (NC[0] + NPR, NX) };
        for (int i = 0; i < 3; i++) odcinki.Add((NC[i + 1] + NPR, NC[i] - NPR));
        foreach (var (x0, x1) in odcinki)
        {
            var przerwy = new List<(float a, float b)>();
            przerwy.Add((x0, x0 + (zebraB - NPR) + 1.5f));                         // zebra skrzyżowania po zachodniej stronie odcinka
            if (x1 < NX - 1f) przerwy.Add((x1 - (zebraB - NPR) - 1.5f, x1));      // zebra skrzyżowania po wschodniej stronie
            else przerwy.Add((PetlaZX - PetlaPlac - 3f - 4f * K * .5f - 1f, x1));  // zebra wlotu zachodniego pętli ZACHÓD
            if (x0 <= PrzystBusX && PrzystBusX <= x1) przerwy.Add((PrzystBusX - 12f * K - 2f, PrzystBusX + 1.5f));   // peron autobusu (po stronie płn. — przedpole LONDON)
            PrzekrojX2(x1 > NC[0] ? "Aleja Centralna (wjazd z pętli ZACHÓD)" : "Aleja Centralna", RZ, AH, Ziel, Chod, x0, x1, true, przerwy.ToArray());
        }
        // --- ULICE C0–C3 (PrzekrojZ bez zieleni = jezdnia, krawężniki, chodniki 5 m) ---
        for (int i = 0; i < 4; i++)
        {
            PrzekrojZ("ul. C" + i + " (płd.)", NC[i], AH, 0f, Chod, NS1 + NPR, RZ - APR, false);
            PrzekrojZ("ul. C" + i + " (płn.)", NC[i], AH, 0f, Chod, RZ + APR, NN1 - NPR, false);
        }
        // --- ul. Nowojorska i Tramwajowa (PrzekrojX); odcinek C1–C2 Tramwajowej bez asfaltu tu — asfalt z torowiskiem w kroku oznakowania ---
        for (int i = 0; i < 3; i++)
        {
            PrzekrojX("ul. Nowojorska", NN1, AH, Chod, NC[i + 1] + NPR, NC[i] - NPR);
            if (i == 1) { Kraweznik(TorRect.xMin, TorRect.xMax, NS1 - AH - Kr, NS1 - AH); Kraweznik(TorRect.xMin, TorRect.xMax, NS1 + AH, NS1 + AH + Kr); Chodnik("ul. Tramwajowa (przystanek)", TorRect.xMin, TorRect.xMax, NS1 - NPR, NS1 - AH - Kr); Chodnik("ul. Tramwajowa", TorRect.xMin, TorRect.xMax, NS1 + AH + Kr, NS1 + NPR); }
            else PrzekrojX("ul. Tramwajowa", NS1, AH, Chod, NC[i + 1] + NPR, NC[i] - NPR);
        }
        // --- SKRZYŻOWANIA: asfalt środka, odcinki asfaltu do początku przekrojów, narożniki (krawężnik + płyty) ---
        foreach (var (c, c2, ramiona, prZ, nazwa) in NdSkrzyzowania()) NdKrok("skrzyżowanie " + nazwa, () => NdSkrzyzowanie(c, c2, ramiona, prZ, nazwa));
    }

    static void NdKwartaly()
    {
        // --- KWARTAŁY (płyty jak chodniki B) ---
        for (int i = 0; i < 3; i++)
        {
            float xa = NC[i + 1] + NPR, xb = NC[i] - NPR;
            Chodnik("kwartał A" + (i + 1) + " (płyty)", xa, xb, RzA0, RzA1);
            Chodnik("kwartał C" + (i + 1) + " z promenadą (płyty)", xa, xb, RzC0, RzC1);
        }
        Chodnik("rząd B ul. Nowojorskiej (płyty)", NC[3] - NPR, NC[0] + NPR, RzB0, RzB1);
        Chodnik("ciąg pieszy za rzędem B (płyty)", NXA, NC[0] + NPR, RzB1, NZ1);
        Plyta("Plac ATLANTA: płyty granitowe", granitPlac, NXA, NC[3] - NPR, NS1 - NPR, RzB1, YC, .3f - YA);
    }

    static void NdZielen()
    {
        // --- ZIELEŃ: skwer zachodni (między pętlą a ul. C0), pas zieleni na południu ---
        Trawa("Skwer zachodni (płn.)", NC[0] + NPR, NX, RZ + APR, NZ1);
        Trawa("Skwer zachodni (płd.)", NC[0] + NPR, NX, NS1 - NPR, RZ - APR);
        TrawaBez("Pas zieleni na południu (do granicy)", Rect.MinMaxRect(NXA, BraxMiastoGranice.Z0, NX, NS1 - NPR), true, PeronTramwaju);
        Chodnik("peron przystanku tramwajowego (płyty)", PeronTramwaju.xMin, PeronTramwaju.xMax, PeronTramwaju.yMin, PeronTramwaju.yMax);
        for (float z = RZ + APR + 8f; z < NZ1 - 6f; z += 14f) { Drzewo(new Vector3(-283f + ((int)z % 3), YC - .03f, z)); Drzewo(new Vector3(-269f, YC - .03f, z + 7f)); ndDrzewa += 2; }
        for (float z = NS1 - NPR + 8f; z < RZ - APR - 6f; z += 14f) { Drzewo(new Vector3(-283f, YC - .03f, z)); Drzewo(new Vector3(-269f + ((int)z % 3), YC - .03f, z + 7f)); ndDrzewa += 2; }
        for (float x = NXA + 10f; x < NX - 10f; x += 16f) { if (PeronTramwaju.xMin - 4f < x && x < PeronTramwaju.xMax + 4f) continue; Drzewo(new Vector3(x, YC - .03f, -79f + ((int)(x * .1f) % 3))); ndDrzewa++; }
    }

    // 9.11: przystanek „T1 · TRAMWAJOWA” na pętli T1 (odcinek C3–C2, jazda na wschód, drzwi po prawej = strona płd.)
    static readonly Rect PeronTramwaju = Rect.MinMaxRect(-672f, NS1 - NPR - 6f, -620f, NS1 - NPR);   // płyty przystanku za chodnikiem
    public const float PrzystanekAutobusuCentralnaX = -318f;
    const float PrzystBusX = PrzystanekAutobusuCentralnaX;                                                                 // przód autobusu na przystanku ALEJA CENTRALNA

    // skrzyżowania: (x, z, ramiona N/E/S/W, połowa ulicy wzdłuż x z chodnikiem (dla narożników w z), nazwa)
    static IEnumerable<(float c, float c2, bool[] ramiona, float prZ, string nazwa)> NdSkrzyzowania()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return (NC[i], RZ, new[] { true, true, true, i < 3 }, APR, "Aleja × C" + i);                               // N, E, S, W
            yield return (NC[i], NN1, new[] { false, i > 0, true, i < 3 }, NPR, "Nowojorska × C" + i);
            yield return (NC[i], NS1, new[] { true, i > 0, false, i < 3 }, NPR, "Tramwajowa × C" + i);
        }
    }

    // skrzyżowanie dwóch ulic prostopadłych (oś x = c ulicy N–S, oś z = c2 ulicy W–E): asfalt środka i wlotów do początku
    // przekrojów, narożniki: krawężnik wzdłuż obu krawędzi jezdni + płyty; brak ramienia = krawężnik i chodnik zamykają jezdnię.
    static void NdSkrzyzowanie(float c, float c2, bool[] ramiona, float prZ, string nazwa)
    {
        bool N = ramiona[0], E = ramiona[1], S = ramiona[2], W = ramiona[3];
        float hj = AH, prX = NPR;
        Asfalt("skrzyżowanie " + nazwa, c - hj, c + hj, c2 - hj, c2 + hj);
        if (E) Asfalt("wlot E " + nazwa, c + hj, c + prX, c2 - hj, c2 + hj);
        if (W) Asfalt("wlot W " + nazwa, c - prX, c - hj, c2 - hj, c2 + hj);
        if (N) Asfalt("wlot N " + nazwa, c - hj, c + hj, c2 + hj, c2 + prZ);
        if (S) Asfalt("wlot S " + nazwa, c - hj, c + hj, c2 - prZ, c2 - hj);
        // brakujące ramiona: krawężnik w linii krawężników + płyty do krawędzi chodnika
        if (!E) { Kraweznik(c + hj, c + hj + Kr, c2 - hj, c2 + hj); Chodnik(nazwa + " (koniec E)", c + hj + Kr, c + prX, c2 - hj, c2 + hj); }
        if (!W) { Kraweznik(c - hj - Kr, c - hj, c2 - hj, c2 + hj); Chodnik(nazwa + " (koniec W)", c - prX, c - hj - Kr, c2 - hj, c2 + hj); }
        if (!N) { Kraweznik(c - hj, c + hj, c2 + hj, c2 + hj + Kr); Chodnik(nazwa + " (koniec N)", c - hj, c + hj, c2 + hj + Kr, c2 + prZ); }
        if (!S) { Kraweznik(c - hj, c + hj, c2 - hj - Kr, c2 - hj); Chodnik(nazwa + " (koniec S)", c - hj, c + hj, c2 - prZ, c2 - hj - Kr); }
        foreach (int s in new[] { 1, -1 })
            foreach (int t in new[] { 1, -1 })
            {
                bool ax = s > 0 ? E : W, az = t > 0 ? N : S;
                float X(float a) => c + s * a; float Z(float a) => c2 + t * a;
                void K2(float a0, float a1, float b0, float b1) => Kraweznik(Mathf.Min(X(a0), X(a1)), Mathf.Max(X(a0), X(a1)), Mathf.Min(Z(b0), Z(b1)), Mathf.Max(Z(b0), Z(b1)));
                void P2(float a0, float a1, float b0, float b1) => Chodnik(nazwa + " (narożnik)", Mathf.Min(X(a0), X(a1)), Mathf.Max(X(a0), X(a1)), Mathf.Min(Z(b0), Z(b1)), Mathf.Max(Z(b0), Z(b1)));
                if (ax && az) { K2(hj, hj + Kr, hj, prZ); K2(hj + Kr, prX, hj, hj + Kr); P2(hj + Kr, prX, hj + Kr, prZ); }
                else if (az) { K2(hj, hj + Kr, hj, prZ); P2(hj + Kr, prX, hj, prZ); }
                else if (ax) { K2(hj, prX, hj, hj + Kr); P2(hj, prX, hj + Kr, prZ); }
                else { K2(hj, hj + Kr, hj, hj + Kr); P2(hj + Kr, prX, hj, prZ); P2(hj, hj + Kr, hj + Kr, prZ); }
            }
    }

    // wyposażenie ulic (etap 3, na gotowym podłożu): latarnie z wysięgnikiem przy krawężnikach ulic C, Nowojorskiej, Tramwajowej
    // (co 30 m, nie w narożnikach skrzyżowań — miejsca słupów sygnalizacji i zebr wolne), drzewa w kratach i ławki na promenadzie,
    // latarnie parkowe w skwerze, drzewa na placu Atlanty i w przerwach między budynkami.
    static void NdWyposazenieUlic()
    {
        for (int i = 0; i < 4; i++)
            foreach (int s in new[] { -1, 1 })
            {
                foreach (var (z0, z1) in new[] { (NS1 + NPR, RZ - APR), (RZ + APR, NN1 - NPR) })
                    for (float z = z0 + 9f; z < z1 - 6f; z += 30f)
                    { Latarnia(new Vector3(NC[i] + s * (AH + Kr + .55f), YC, z), new Vector3(-s, 0, 0), AH * .6f); ndLatarnie++; }
            }
        for (int i = 0; i < 3; i++)
            foreach (var zc in new[] { NN1, NS1 })
                foreach (int t in new[] { -1, 1 })
                    for (float x = NC[i] - NPR - 9f; x > NC[i + 1] + NPR + 6f; x -= 30f)
                    {
                        if (Mathf.Approximately(zc, NS1) && t < 0 && x < PeronTramwaju.xMax + 2f && x > PeronTramwaju.xMin - 2f) continue;
                        Latarnia(new Vector3(x, YC, zc + t * (AH + Kr + .55f)), new Vector3(0, 0, -t), AH * .6f); ndLatarnie++;
                    }
        // promenada rzędu C: dwa rzędy drzew w kratach, ławki przodem do alei, kosze, latarnie parkowe
        for (int i = 0; i < 3; i++)
        {
            float xa = NC[i + 1] + NPR, xb = NC[i] - NPR;
            for (float x = xb - 8f; x > xa + 6f; x -= 12f)
            {
                float xx = x;
                if (Mathf.Abs(xx - (PrzystBusX - 9f)) < 9f) continue;   // wiata przystanku
                Pozniej("drzewo w kracie (promenada)", () => { var p = NaPodlozu(new Vector3(xx, YC, 10.5f)); BraxRegionMeble.KrataDrzewa(root, p); DrzewoTeraz(p); });
                // R1: żadnych ławek na miejskiej promenadzie przy ulicy.
                if (((int)(-xx / 12f)) % 2 == 0) Pozniej("kosz (promenada)", () => BraxRegionMeble.Kosz(root, NaPodlozu(new Vector3(xx + 5.6f, YC, 9.9f)), 180f));
                else Pozniej("latarnia (promenada)", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(xx + 6f, YC, 4.5f))); latarnie++; });
                ndDrzewa++;
            }
        }
        // plac Atlanty: dwa rzędy drzew w kratach wzdłuż wschodniego lica i ławki
        for (float z = RzC0 + 4f; z < RzB1 - 4f; z += 13f)
        {
            float zz = z; if (Mathf.Abs(zz - RZ) < 9f) continue;   // oś alei — wejście
            Pozniej("drzewo w kracie (plac Atlanty)", () => { var p = NaPodlozu(new Vector3(NC[3] - NPR - 4.5f, YC, zz)); BraxRegionMeble.KrataDrzewa(root, p); DrzewoTeraz(p); });
            ndDrzewa++;
        }
        // skwer zachodni: latarnie parkowe i ławki przy ścieżce (aleja przecina skwer)
        for (float z = RZ + APR + 14f; z < NZ1 - 8f; z += 26f) { float zz = z; Pozniej("latarnia (skwer)", () => { BraxRegionMeble.Latarnia(root, NaPodlozu(new Vector3(-276f, YC, zz))); latarnie++; }); }
    }

    // =====================================================================================================================
    // 1b) OZNAKOWANIE W NAWIERZCHNI (wywoływane z PlanOznakowania przed BudujAsfalt): skrzyżowania alei z sygnalizacją (zebry,
    //     linie zatrzymania — SkrzyzowanieT B), zebry na skrzyżowaniach ul. Nowojorskiej i Tramwajowej, zebra wlotu zach. pętli,
    //     osie przerywane. Część farby leżąca na jezdni z torowiskiem idzie do wstawek toru (asfalt toru budowany tu, osobno).
    // zebry bez świateł nowych dzielnic — przy nich D-6 (BraxRegionMiastoB.Sygnalizacja, etap 3, gdy chodniki już stoją)
    static readonly List<(Vector3 c, Vector3 dir, float hj)> ZebryBezSwiatelNd = new List<(Vector3, Vector3, float)>();
    static void NoweDzielniceOznakowanie(Farba f)
    {
        var moja = new Farba(); ZebryBezSwiatelNd.Clear();
        for (int i = 0; i < 4; i++) { int ii = i; NdKrok("oznakowanie Aleja × C" + i, () => SkrzyzowanieT(moja, new Vector3(NC[ii], YA, RZ), AH, AH, NdWlotyAlei(ii), 0f)); }
        foreach (var (c, c2, ram, prZ, nazwa) in NdSkrzyzowania())
        {
            if (Mathf.Approximately(c2, RZ)) continue;
            float d = AH + Kr + .6f + 2f * K + OdsuniecieZebry(new Vector2(c, c2));   // 10.11: zebra za łukiem narożnika
            if (ram[1]) { Zebra(moja, new Vector3(c + d, YA, c2), Vector3.right, AH); ndZebry++; ZebryBezSwiatelNd.Add((new Vector3(c + d, YA, c2), Vector3.right, AH)); }
            if (ram[3]) { Zebra(moja, new Vector3(c - d, YA, c2), Vector3.left, AH); ndZebry++; ZebryBezSwiatelNd.Add((new Vector3(c - d, YA, c2), Vector3.left, AH)); }
            if (ram[0]) { Zebra(moja, new Vector3(c, YA, c2 + d), Vector3.forward, AH); ndZebry++; ZebryBezSwiatelNd.Add((new Vector3(c, YA, c2 + d), Vector3.forward, AH)); }
            if (ram[2]) { Zebra(moja, new Vector3(c, YA, c2 - d), Vector3.back, AH); ndZebry++; ZebryBezSwiatelNd.Add((new Vector3(c, YA, c2 - d), Vector3.back, AH)); }
        }
        Zebra(moja, new Vector3(PetlaZX - PetlaPlac - 3f, YA, RZ), Vector3.left, AH); ndZebry++;   // wlot zachodni pętli ZACHÓD
        ZebryBezSwiatelNd.Add((new Vector3(PetlaZX - PetlaPlac - 3f, YA, RZ), Vector3.left, AH));
        // osie: przerywane (3 m kreska, 4 m przerwy), 20 m od środków skrzyżowań bez kresek (linie zatrzymania, zebry)
        void OsX(float z, float xa, float xb) { for (float x = xb - 3f; x - 3f > xa; x -= 7f) moja.MinMax(new Vector3(x - 3f, YA, z - .06f), new Vector3(x, YA, z + .06f)); }
        void OsZ(float x, float za, float zb) { for (float z = za + 3f; z + 3f < zb; z += 7f) moja.MinMax(new Vector3(x - .06f, YA, z), new Vector3(x + .06f, YA, z + 3f)); }
        OsX(RZ, NC[0] + 20f, PetlaZX - PetlaPlac - 9f);
        for (int i = 0; i < 3; i++) { OsX(RZ, NC[i + 1] + 20f, NC[i] - 20f); OsX(NN1, NC[i + 1] + 16f, NC[i] - 16f); OsX(NS1, NC[i + 1] + 16f, NC[i] - 16f); }
        for (int i = 0; i < 4; i++) { OsZ(NC[i], NS1 + 16f, RZ - 20f); OsZ(NC[i], RZ + 20f, NN1 - 16f); }
        // podział: na jezdnię z torowiskiem → wstawki toru; reszta → farba B (asfalt z wycięciami)
        foreach (var q in moja.r)
        {
            var w = Przetnij(q, TorRect);
            if (Niepuste(w)) ndWstawkiToru.Add((w, 0));
            foreach (var reszta in Odejmij(q, TorRect)) if (Niepuste(reszta)) f.r.Add(reszta);
        }
        NdKrok("torowisko w nawierzchni ul. Tramwajowej", NdTorowisko);
        NdLog("oznakowanie: zebr bez świateł " + ndZebry + " + na 15 wlotach alei z sygnalizacją, pasów farby " + moja.r.Count + " — W WYCIĘCIACH asfaltu; wstawek toru " + ndWstawkiToru.Count);
    }

    static (Vector3 dir, float hj, float chod)[] NdWlotyAlei(int i)
    {
        var l = new List<(Vector3, float, float)> { (Vector3.right, AH, Chod), (Vector3.forward, AH, Chod), (Vector3.back, AH, Chod) };
        if (i < 3) l.Add((Vector3.left, AH, Chod));
        return l.ToArray();
    }

    // TOROWISKO W NAWIERZCHNI ul. Tramwajowej (C1–C2): dwa tory (pas płd. — na wschód, płn. — na zachód), szyny rowkowe jako
    // WSTAWKI w wycięciach asfaltu (główka szyny ze stali + rowek ciemny) — ta sama wysokość co asfalt, styk krawędzią (zasada
    // 15:40: nic nie leży na asfalcie). Rozstaw 1.435 m × K. Asfalt odcinka budowany tu (górna powierzchnia z otworami).
    static void NdTorowisko()
    {
        // 9.11 (właściciel: tramwaj „ma elegancko kursować”, „łamać się na zakrętach”): dawny dwutorowy odcinek C1–C2 z kozłami
        // (wahadło na prostej) ZDJĘTY — T1 jeździ pętlą wokół kwartału C2–C3 (NdTramwaj, szyny nad asfaltem wzdłuż całej pętli).
        // Tu zostaje sam asfalt odcinka C1–C2 z oznakowaniem (wstawki farby), bez szyn.
        // farba przecinająca szyny: farba ustępuje szynie (wstawki się nie nakładają)
        var szyny = ndWstawkiToru.Where(w => w.mat > 0).Select(w => w.r).ToList();
        var nowe = new List<(Rect, int)>();
        foreach (var (r, m) in ndWstawkiToru)
        {
            if (m > 0) { nowe.Add((r, m)); continue; }
            var l = new List<Rect> { r }; foreach (var sz in szyny) l = l.SelectMany(x => Odejmij(x, sz)).ToList();
            nowe.AddRange(l.Where(Niepuste).Select(x => (x, 0)));
        }
        ndWstawkiToru.Clear(); ndWstawkiToru.AddRange(nowe);
        var stalSzyny = BraxCityMaterials.Surface("Tor: główka szyny (stal wyślizgana)", new Color(.62f, .62f, .62f), .25f, .9f);
        var rowekMat = BraxCityMaterials.Surface("Tor: rowek szyny (ciemny)", new Color(.05f, .05f, .05f), .8f, .3f);
        var bTop = new BraxRegionMeble.Bud(); var bWst = new[] { new BraxRegionMeble.Bud(), new BraxRegionMeble.Bud(), new BraxRegionMeble.Bud() };
        NdWstawki(TorRect, YA, ndWstawkiToru, bTop, bWst);
        bTop.Obiekt("Jezdnia z torowiskiem (asfalt, ul. Tramwajowa)", root, asfalt, false, false);
        bWst[0].Obiekt("Torowisko: farba w wycięciach", root, farba, false, false);
        bWst[1].Obiekt("Torowisko: główki szyn (w wycięciach asfaltu)", root, stalSzyny, false, false);
        bWst[2].Obiekt("Torowisko: rowki szyn (w wycięciach asfaltu)", root, rowekMat, false, false);
        Nawierzchnia(BraxRegionMeble.Kolizja(root, "Jezdnia z torowiskiem kolizja", new Vector3(TorRect.center.x, YA - .15f, TorRect.center.y), new Vector3(TorRect.width, .3f, TorRect.height)));
        // kontrola: wstawki toru parami i z asfaltem B (ten prostokąt nie jest w liście asfaltów B — sprawdzany tu)
        for (int i = 0; i < ndWstawkiToru.Count; i++) for (int j = i + 1; j < ndWstawkiToru.Count; j++) if (Niepuste(Przetnij(ndWstawkiToru[i].r, ndWstawkiToru[j].r))) { ndMigotanie++; Debug.LogWarning("BRAXHORN_MIASTO_ASSETY_MIGOTANIE wstawki toru " + ndWstawkiToru[i].r + " × " + ndWstawkiToru[j].r); }
        foreach (var (n, r) in asfalty) if (Niepuste(Przetnij(r, TorRect))) { ndMigotanie++; Debug.LogWarning("BRAXHORN_MIASTO_ASSETY_MIGOTANIE asfalt toru × " + n); }
        NdLog("ul. Tramwajowa C1–C2: asfalt z oznakowaniem (dawne torowisko zdjęte — pętla T1 wokół kwartału C2–C3); rozstaw szyn T1 " + (Rozstaw * K).ToString("0.000") + " m = 1.435 × K) x " + (TorRect.xMin + .8f).ToString("0.0") + "…" + (TorRect.xMax - .8f).ToString("0.0") + " — wstawki w wycięciach asfaltu, wstawek " + ndWstawkiToru.Count);
    }

    // górna powierzchnia prostokąta z otworami w miejscach wstawek (każda wstawka swoim materiałem) — wariant Wstawki B
    static void NdWstawki(Rect a, float y, List<(Rect r, int mat)> wst, BraxRegionMeble.Bud bTop, BraxRegionMeble.Bud[] bW, float grub = .3f)
    {
        var xs = new SortedSet<float> { a.xMin, a.xMax }; var zs = new SortedSet<float> { a.yMin, a.yMax };
        for (float x = a.xMin + 6f; x < a.xMax - .5f; x += 6f) xs.Add(x);
        for (float z = a.yMin + 6f; z < a.yMax - .5f; z += 6f) zs.Add(z);
        var h = wst.Select(o => (r: Przetnij(o.r, a), o.mat)).Where(o => Niepuste(o.r)).ToList();
        foreach (var o in h) { xs.Add(o.r.xMin); xs.Add(o.r.xMax); zs.Add(o.r.yMin); zs.Add(o.r.yMax); }
        var X = xs.ToArray(); var Z = zs.ToArray();
        for (int i = 0; i + 1 < X.Length; i++)
            for (int j = 0; j + 1 < Z.Length; j++)
            {
                if (X[i + 1] - X[i] < .002f || Z[j + 1] - Z[j] < .002f) continue;
                var c = new Vector2((X[i] + X[i + 1]) * .5f, (Z[j] + Z[j + 1]) * .5f);
                int m = -1; foreach (var o in h) if (o.r.Contains(c)) { m = o.mat; break; }
                var b = m < 0 ? bTop : bW[m];
                b.Quad(new Vector3(X[i], y, Z[j]), new Vector3(X[i], y, Z[j + 1]), new Vector3(X[i + 1], y, Z[j + 1]), new Vector3(X[i + 1], y, Z[j]));
            }
        Vector3 P(float x, float yy, float z) => new Vector3(x, yy, z);
        bTop.Quad(P(a.xMin, y, a.yMin), P(a.xMax, y, a.yMin), P(a.xMax, y - grub, a.yMin), P(a.xMin, y - grub, a.yMin));
        bTop.Quad(P(a.xMax, y, a.yMax), P(a.xMin, y, a.yMax), P(a.xMin, y - grub, a.yMax), P(a.xMax, y - grub, a.yMax));
    }

    // =====================================================================================================================
    // 2) BUDYNKI (modele z paczek): lico w linii chodnika, podstawa na płytach (promień w dół), × K, kolizja bryły,
    //    LODGroup (dachowe urządzenia znikają z daleka), sylwetki z daleka (kwadraty doczytywane dopiero 120 m przed).
    static void NoweDzielniceBudynki(Transform cityRoot)
    {
        Physics.SyncTransforms();
        var g = BraxRegion.Root(cityRoot, "CLAUDE nowe dzielnice: budynki (Sketchfab)");
        // WŁAŚCICIEL 3.11 10:24 (zdjęcia): z dalszej odległości zamiast budynków gołe bryły (sylwetki), bo kwadraty 150 m
        // z prawdziwymi budynkami wczytują się dopiero 120 m przed. Teraz budynki (bryły z LOD) zostają w scenie głównej:
        // pusta LODGroup na korzeniu = BraxClaudeObszary.WObszarze nie dzieli korzenia na kwadraty (jak sylwetki). Sylwetki
        // niepotrzebne — nie powstają. Koszt: tekstury dzielnic stale w pamięci (~60 MB ASTC) — pomiar w BRAXHORN_KWADRATY.
        if (!g.GetComponent<LODGroup>()) g.gameObject.AddComponent<LODGroup>();
        var wezly = new Dictionary<string, GameObject>();   // instancje dzielnic (jedna kopia, węzły rozdzielane po budynkach)
        int ok = 0, brak = 0, nSzklo = 0;
        foreach (var b in NoweBudynki)
        {
            try
            {
                // 10.11 (właściciel 00:04: „wieżowce zrobić murowane… wysokie”): 14 jednostek „centrum” ze Sketchfab (czarne ściany,
                // schody pożarowe na szkle) zastąpione wieżowcami murowanymi budowanymi z kodu w tym samym obrysie (BraxRegionFawelaWieze.cs).
                // Model zostaje w paczce — wyłącznik BraxRegionFawela.WiezeMurowaneWlaczone przywraca go bez innych zmian.
                if (BraxRegionFawela.WiezeMurowaneWlaczone && b.paczka == "miasto_centrum")
                {
                    var wm = BraxRegionFawela.WiezaMurowana(g, b.id, b.x, Podloze(b.x, b.z, YC), b.z, b.w, b.d);
                    if (wm) { ndWiezeMurowane++; ok++; continue; }
                }
                if (!BraxMiastoAssety.Jest(b.paczka)) { brak++; continue; }
                var model = BraxMiastoAssety.Model(b.paczka, b.plik); if (!model) { brak++; continue; }
                float y = Podloze(b.x, b.z, YC);
                var bud = new GameObject("Budynek " + b.id).transform; bud.SetParent(g, false);
                bud.position = new Vector3(b.x, y, b.z); bud.rotation = Quaternion.Euler(0, b.yaw, 0); bud.localScale = Vector3.one * K;
                Renderer[] bryla, dach;
                if (b.wezel == null)
                {
                    var o = (GameObject)UnityEngine.Object.Instantiate(model, bud, false); o.name = b.plik;
                    o.transform.localPosition = Vector3.zero; o.transform.localRotation = Quaternion.identity; o.transform.localScale = Vector3.one;
                    bryla = o.GetComponentsInChildren<Renderer>(true); dach = new Renderer[0];
                }
                else
                {
                    if (!wezly.TryGetValue(b.plik, out var kopia) || !kopia) { kopia = (GameObject)UnityEngine.Object.Instantiate(model); kopia.name = b.plik + " (kopia robocza)"; wezly[b.plik] = kopia; }
                    var tb = BraxMiastoAssety.Znajdz(kopia.transform, b.wezel + "_bryla"); var td = BraxMiastoAssety.Znajdz(kopia.transform, b.wezel + "_dach");
                    if (!tb) { NdLog("brak węzła " + b.wezel + "_bryla w " + b.plik + " — DO POPRAWKI"); UnityEngine.Object.DestroyImmediate(bud.gameObject); brak++; continue; }
                    foreach (var t in new[] { tb, td }) { if (!t) continue; t.SetParent(bud, false); t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one; }
                    bryla = tb.GetComponentsInChildren<Renderer>(true); dach = td ? td.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                }
                BraxMiastoAssety.Przygotuj(bud.gameObject, true, true);
                nSzklo += NdSzklo(bud.gameObject, b.id == "KINNAIRD" || b.paczka == "miasto_kamienice");
                NdAudytBudynku(b.id, bud.gameObject);
                var originalBounds = BraxRegion.Bounds(bud.gameObject);
                foreach (var r in dach) r.shadowCastingMode = ShadowCastingMode.Off;
                // LOD: urządzenia dachowe do 12 % wysokości ekranu, bryła do 0.6 %
                var lod = bud.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(dach.Length > 0 ? new[] { new LOD(.12f, bryla.Concat(dach).ToArray()), new LOD(.006f, bryla) } : new[] { new LOD(.006f, bryla) });
                lod.RecalculateBounds();
                // kolizja bryły: prostokąt lica × wysokość (osie świata — obroty 0/90)
                var gr = originalBounds; // zabezpieczenie wymiarów źródłowej kolizji
                var col = new GameObject("Budynek " + b.id + " kolizja").AddComponent<BoxCollider>(); col.transform.SetParent(g, false);
                col.transform.position = new Vector3(b.x, y + (gr.max.y - y) * .5f, b.z); col.size = new Vector3(b.w - .1f, gr.max.y - y, b.d - .1f);
                ndBudynki.Add(bud.gameObject); ok++;
                NdLog("budynek " + b.id + " (" + b.plik + (b.wezel != null ? " " + b.wezel : "") + "): środek (" + b.x.ToString("0.0") + ", " + y.ToString("0.00") + ", " + b.z.ToString("0.0") + "), obrót " + b.yaw + "°, lico " + b.w.ToString("0.0") + " × " + b.d.ToString("0.0") + " m, wys. " + (gr.max.y - y).ToString("0") + " m");
            }
            catch (Exception e) { BraxMiastoAssety.Blad("budynek " + b.id, e); }
        }
        foreach (var k in wezly.Values) if (k) UnityEngine.Object.DestroyImmediate(k);   // niewykorzystane jednostki (U14, kliny) nie idą do sceny
        // 9.11 (właściciel: „Historyczny budynek brak tekstur i coś na nim wisi”; „budynki w nowej części miasta mają być premium”):
        // ZDJĘTE NdPremiumFasada (a33ea589) i NdZdejmijNadwieszenieZabytku (22da6a61/c3405fab) z 7.11.
        // Fasada rysowała płaską szybę na prostokącie w×d z tablicy BEZ obrotu budynku (LONDON yaw=90 → panel szerszy o ~11 m w osi z)
        // i bez kształtu bryły Sketchfab — to ona wystawała nad KINNAIRD („coś wisi”). Heurystyka zdejmowania szukała materiałów
        // „glass|black|metal” powyżej 25·K m i wyłączała renderery ZABYTKU (blacha, obróbki) — „brak tekstur”.
        // Premium nowej części = osobne zlecenie po nagraniu właściciela; NdSzklo (szkło zamiast czarnych okien, 3.11) zostaje.
        // sylwetki z daleka wyłączone (budynki zostają w scenie głównej — patrz wyżej)
        try { NdPrzytnijNawisy(); } catch (Exception e) { BraxMiastoAssety.Blad("nawisy budynków", e); }
        try { NdMigotanieBudynkow(); } catch (Exception e) { BraxMiastoAssety.Blad("kontrola podstaw budynków", e); }
        NdLog("szkło fasad: " + nSzklo + " materiałów na budynkach zamienione na szkło refleksyjne (czarny metal / szyby przed pustą bryłą)");
        NdLog("wieżowce z kodu: " + ndWiezeMurowane + " (murowanych " + BraxRegionFawela.wiezMurowanych + ", szklanych " + BraxRegionFawela.wiezSzklanych + ", okien " + BraxRegionFawela.wiezOkien + ", trójkątów " + BraxRegionFawela.wiezTrojkatow + ")");
        NdLog("budynki: postawione " + ok + "/" + NoweBudynki.Length + (brak > 0 ? ", bez modelu " + brak + " (paczka nieodszyfrowana — kwartał zostaje z płytami)" : ""));
    }

    // WŁAŚCICIEL 3.11 10:12–10:19 (nagrania): „czarne budynki, gdzie są okna”. W modelach Sketchfaba szkło fasad było
    // (a) metalem bez tekstury z ciemnym kolorem (CityGenbasic_rough_metal_dark — kolor 0,0,0; CityGenblack_) — metal z albedo
    // ≈ 0 nic nie odbija = czarna ściana; (b) szybą półprzezroczystą (CityGenGlass, alfa .25) przed pustą bryłą = czarna dziura.
    // Oba → szkło fasady refleksyjne jak wieżowce miasta (BraxSzyby.Refleksyjne: stalowo-niebieskie, odbicie nieba).
    // Okna malowane w teksturach elewacji zostają. Środki budynków nie są robione (właściciel: „środek nie musi być”).
    // AUDYT BUDYNKU (9.11, właściciel: „każdy budynek premium — budynek po budynku, każdy detal”): pełny spis każdego budynku
    // nowych dzielnic w logu (BRAXHORN_BUDYNEK): renderery, trójkąty, materiały z rozdzielczością tekstur i mapą normalnych,
    // oraz podejrzane miejsca — ciemne powierzchnie bez tekstury (czarne plamy), tekstury < 512 px, brak normalnych na dużych
    // ścianach, materiały przezroczyste poza szkłem. Na tej podstawie poprawki budynek po budynku (po buildzie, z logu).
    static void NdAudytBudynku(string id, GameObject o)
    {
        var rs = o.GetComponentsInChildren<Renderer>(true); long tris = 0; var mats = new HashSet<Material>();
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tris += mf.sharedMesh.GetIndexCount(k) / 3;
            foreach (var m in r.sharedMaterials) if (m) mats.Add(m);
        }
        var uwagi = new List<string>(); var opis = new List<string>();
        foreach (var m in mats)
        {
            var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            bool norm = m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap");
            var kol = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            string sh = m.shader ? m.shader.name : "?";
            opis.Add(m.name + " [" + sh.Replace("Braxhorn/", "") + (tex ? ", " + tex.width + "px" : ", bez tekstury") + (norm ? ", normalne" : "") + "]");
            if (sh == BraxSzkloFasady.ShaderName) continue;
            if (!tex && kol.grayscale < .15f) uwagi.Add("ciemna powierzchnia bez tekstury: " + m.name);
            if (tex && Mathf.Max(tex.width, tex.height) < 512) uwagi.Add("mała tekstura " + tex.width + "px: " + m.name);
            if (m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f) uwagi.Add("przezroczysty (nie szkło fasady): " + m.name);
        }
        var b = BraxRegion.Bounds(o);
        Debug.Log("BRAXHORN_BUDYNEK " + id + ": rendererów " + rs.Length + ", trójkątów " + tris + ", wymiary " + b.size.ToString("F1") + ", materiałów " + mats.Count + (uwagi.Count > 0 ? " — UWAGI (" + uwagi.Count + "): " + string.Join("; ", uwagi) : " — bez uwag"));
        Debug.Log("BRAXHORN_BUDYNEK " + id + " materiały: " + string.Join(", ", opis.Take(40)));
    }

    static Material ndSzklo, ndSzkloKamienice;
    static int NdSzklo(GameObject o, bool kamienica = false)
    {
        if (!ndSzklo)
        {
            // 9.11 (właściciel z tarasu Tower: „czarne bryły budynków”, „każdy budynek premium”): ciemny metal z odbiciem stałego
            // koloru → szkło elewacyjne Braxhorn/SzkloFasady (odbicie prawdziwego nieba, pokoje za szybą, słupki, pasy kondygnacji).
            // Modele × K: kondygnacja ≈ 3.4 m × K, moduł 3 m. Bez shadera — dawny materiał (zapasowy).
            var zapas = BraxCityMaterials.Surface("Nowe dzielnice: szkło fasady (refleksyjne)", new Color(.30f, .37f, .42f), .1f, .85f);
            if (zapas.HasProperty("_ReflectionStrength")) zapas.SetFloat("_ReflectionStrength", .95f);
            ndSzklo = BraxSzkloFasady.Mat("nowe dzielnice", 3.0f, 3.4f * K, .3f, new Color(.20f, .30f, .36f), .55f, .35f, zapas);
            // ściany kurtynowe modeli mają własne słupki i stropy w geometrii — w szkle tylko cienka linia modułu, bez pasa
            if (ndSzklo && ndSzklo.HasProperty("_Rama")) { ndSzklo.SetFloat("_Rama", .04f); ndSzklo.SetFloat("_Pas", 0f); }
            // zabytek KINNAIRD i kamienice: małe okna z własnymi ramami — szyba bez siatki elewacji, ciepłe mieszkania za szybą
            ndSzkloKamienice = BraxSzkloFasady.Mat("nowe dzielnice — kamienice i zabytek", 2.4f, 3.2f * K, .3f, new Color(.26f, .30f, .32f), .5f, .3f, zapas);
            if (ndSzkloKamienice && ndSzkloKamienice.HasProperty("_Rama")) { ndSzkloKamienice.SetFloat("_Rama", 0f); ndSzkloKamienice.SetFloat("_Pas", 0f); }
        }
        var docelowe = kamienica && ndSzkloKamienice ? ndSzkloKamienice : ndSzklo;
        int n = 0;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials; bool zm = false;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i]; if (!m || m == ndSzklo || m == ndSzkloKamienice) continue;
                bool szklo = m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
                bool ciemnyMetal = m.HasProperty("_Metallic") && m.GetFloat("_Metallic") >= .8f && m.HasProperty("_MainTex") && !m.GetTexture("_MainTex")
                                   && m.HasProperty("_Color") && m.GetColor("_Color").grayscale < .2f;
                if ((szklo || ciemnyMetal) && docelowe) { ms[i] = docelowe; zm = true; n++; }
            }
            if (zm) r.sharedMaterials = ms;
        }
        return n;
    }

    // sylwetki z daleka: bryły lica × wysokość (0.5 m w głąb), jedna siatka na kwadrat 150 m, wszystkie pod jednym korzeniem
    // z pustą LODGroup obejmującym kilka kwadratów (zostaje w scenie głównej — BraxClaudeObszary nie przenosi go do kwadratu)
    static void NdSylwetki(Transform cityRoot)
    {
        if (ndBudynki.Count == 0) return;
        var sr = new GameObject("CLAUDE nowe dzielnice: sylwetki z daleka").transform; sr.SetParent(cityRoot, false); sr.position = Vector3.zero;
        sr.gameObject.AddComponent<LODGroup>();
        var mat = BraxCityMaterials.Surface("Nowe dzielnice z daleka: bryły (jasny kamień w mgiełce)", new Color(.52f, .51f, .49f), .8f, 0f);   // było .22 — z daleka czarne bloki (właściciel 3.11)
        var grupy = new Dictionary<(int, int), (BraxRegionMeble.Bud b, Bounds ob)>();
        foreach (var o in ndBudynki)
        {
            var bb = BraxRegion.Bounds(o); var lo = new Vector3(bb.min.x + .5f, bb.min.y, bb.min.z + .5f); var hi = new Vector3(bb.max.x - .5f, bb.max.y - .5f, bb.max.z - .5f);
            var key = (Mathf.FloorToInt(bb.center.x / 150f), Mathf.FloorToInt(bb.center.z / 150f));
            if (!grupy.TryGetValue(key, out var gr)) gr = (new BraxRegionMeble.Bud(), new Bounds(bb.center, Vector3.zero));
            gr.b.MinMax(lo, hi); var ob = gr.ob; ob.Encapsulate(bb); gr.ob = ob; grupy[key] = gr;
        }
        foreach (var kv in grupy)
        {
            var go = kv.Value.b.Obiekt("Sylwetki budynków (kwadrat " + kv.Key + ")", sr, mat, false, false);
            if (!go) continue;
            var z = go.AddComponent<BraxSylwetkiZDaleka>(); z.sylwetka = go.GetComponent<Renderer>(); z.obszar = kv.Value.ob; z.ukryjBlizej = 100f;
        }
        NdLog("sylwetki z daleka: " + grupy.Count + " grup (kwadraty 150 m), budynków " + ndBudynki.Count);
    }

    // kontrola: poziome trójkąty budynków w płaszczyźnie płyt (±2 cm nad podstawą) — leżałyby na chodniku (migotanie).
    // Offline usunięte (raport paczki: „podlogi_na_podstawie_usuniete”); tu sprawdzenie na siatkach po imporcie.
    // 9.11 (właściciel: „Historyczny budynek … coś na nim wisi”; nagranie 16:58 — czarna przeszklona płyta nad KINNAIRD wysunięta
    // z wieżowca obok). Modele Sketchfab mają nawisy/wsporniki projektowane nad SĄSIEDNIM budynkiem, którego u nas nie ma — u nas
    // wiszą nad aleją i nad zabytkiem. Zasada: powyżej 6 m nad gruntem nic z bryły nie wychodzi poza obrys jej PARTERU (wierzchołki
    // do 4.5 m) o więcej niż 1.5 m. Nawis liczony z geometrii (pole trójkątów), cięcie precyzyjne (BraxCityMeshCut.Subtract).
    // Gzymsy i balkony (< 1.5 m) zostają. Jeśli „nawis” to > 25 % bryły (np. parter na słupach) — bez cięcia, ostrzeżenie.
    static void NdPrzytnijNawisy()
    {
        const float Parter = 4.5f, OdWys = 6f, Zapas = 1.5f, MinPole = 40f;
        int przyciete = 0;
        foreach (var bud in ndBudynki)
        {
            if (!bud) continue;
            float y0 = bud.transform.position.y;
            var filtry = bud.GetComponentsInChildren<MeshFilter>(true).Where(f => f && f.sharedMesh && f.GetComponent<MeshRenderer>()).ToArray();
            float fx0 = float.MaxValue, fx1 = float.MinValue, fz0 = float.MaxValue, fz1 = float.MinValue; int nieczytelne = 0;
            foreach (var f in filtry)
            {
                if (!f.sharedMesh.isReadable) { nieczytelne++; continue; }
                var m = f.transform.localToWorldMatrix;
                foreach (var v in f.sharedMesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    if (p.y > y0 + Parter) continue;
                    fx0 = Mathf.Min(fx0, p.x); fx1 = Mathf.Max(fx1, p.x); fz0 = Mathf.Min(fz0, p.z); fz1 = Mathf.Max(fz1, p.z);
                }
            }
            if (fx0 > fx1) { NdLog("nawis " + bud.name + ": brak obrysu parteru (nieczytelne siatki " + nieczytelne + ") — bez zmian"); continue; }
            fx0 -= Zapas; fx1 += Zapas; fz0 -= Zapas; fz1 += Zapas;
            double poleWszystko = 0, poleNawis = 0;
            foreach (var f in filtry)
            {
                if (!f.sharedMesh.isReadable) continue;
                var mesh = f.sharedMesh; var m = f.transform.localToWorldMatrix; var vs = mesh.vertices;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var t = mesh.GetTriangles(sub);
                    for (int i = 0; i + 2 < t.Length; i += 3)
                    {
                        Vector3 a = m.MultiplyPoint3x4(vs[t[i]]), b = m.MultiplyPoint3x4(vs[t[i + 1]]), c = m.MultiplyPoint3x4(vs[t[i + 2]]);
                        float pole = Vector3.Cross(b - a, c - a).magnitude * .5f; poleWszystko += pole;
                        var s0 = (a + b + c) / 3f;
                        if (s0.y > y0 + OdWys && (s0.x < fx0 || s0.x > fx1 || s0.z < fz0 || s0.z > fz1)) poleNawis += pole;
                    }
                }
            }
            if (poleNawis < MinPole) continue;
            if (poleNawis > poleWszystko * .25)
            {
                // parter mały (budynek na słupach / z podcieniem): granica = działka z planu (b.x ± w/2, b.z ± d/2), nie parter
                var plan = NoweBudynki.FirstOrDefault(q => "Budynek " + q.id == bud.name);
                if (plan == null) { NdLog("nawis " + bud.name + ": " + (100 * poleNawis / poleWszystko).ToString("F0") + " % bryły poza parterem, brak planu działki — DO POPRAWKI"); continue; }
                fx0 = plan.x - plan.w * .5f - Zapas; fx1 = plan.x + plan.w * .5f + Zapas; fz0 = plan.z - plan.d * .5f - Zapas; fz1 = plan.z + plan.d * .5f + Zapas;
                NdLog("nawis " + bud.name + ": " + (100 * poleNawis / poleWszystko).ToString("F0") + " % bryły poza parterem — granica = działka z planu " + plan.w.ToString("F1") + " × " + plan.d.ToString("F1") + " m");
            }
            var bb = BraxRegion.Bounds(bud);
            float yc = y0 + OdWys, yt = bb.max.y + 5f, xa = bb.min.x - 5f, xb = bb.max.x + 5f, za = bb.min.z - 5f, zb = bb.max.z + 5f;
            Bounds Pud(float ax, float ay, float az, float bx, float by, float bz) { var q = new Bounds(); q.SetMinMax(new Vector3(ax, ay, az), new Vector3(bx, by, bz)); return q; }
            int siatki = 0;
            foreach (var pud in new[] { Pud(xa, yc, za, fx0, yt, zb), Pud(fx1, yc, za, xb, yt, zb), Pud(fx0, yc, za, fx1, yt, fz0), Pud(fx0, yc, fz1, fx1, yt, zb) })
                if (pud.size.x > .01f && pud.size.z > .01f) siatki += BraxCityMeshCut.Subtract(bud.transform, pud, BraxRegion.Out + "/Cuts", "nawis_" + bud.name.Replace(' ', '_'));
            przyciete++;
            NdLog("nawis " + bud.name + ": przycięto " + poleNawis.ToString("F0") + " m² bryły wiszącej poza obrysem parteru (x " + (fx0 + Zapas).ToString("F1") + ".." +
                  (fx1 - Zapas).ToString("F1") + ", z " + (fz0 + Zapas).ToString("F1") + ".." + (fz1 - Zapas).ToString("F1") + "), siatek " + siatki);
        }
        NdLog("nawisy: przycięte budynki " + przyciete + "/" + ndBudynki.Count);
        // 9.11 (właściciel, zdjęcie 17:21: czarna przeszklona bryła z żółtym szyldem wisi nad KINNAIRD): przestrzeń NAD zabytkiem
        // jest wolna — każda siatka innego budynku (i każdy inny obiekt sceny < 300 m) nad obrysem KINNAIRD (+1 m) od wysokości
        // jego najwyższego punktu − 1 m w górę zostaje wycięta. Sam zabytek nietknięty.
        var kin = ndBudynki.FirstOrDefault(q => q && q.name == "Budynek KINNAIRD");
        if (kin)
        {
            var kb = BraxRegion.Bounds(kin);
            var nad = new Bounds(); nad.SetMinMax(new Vector3(kb.min.x - 1f, kb.max.y - 1f, kb.min.z - 1f), new Vector3(kb.max.x + 1f, kb.max.y + 400f, kb.max.z + 1f));
            int nadZabytkiem = 0; var kto = new List<string>();
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!r || !r.enabled || r.transform.IsChildOf(kin.transform)) continue;
                    var rb = r.bounds; if (!rb.Intersects(nad) || rb.size.x > 300f || rb.size.z > 300f) continue;
                    string nz = r.name.ToLowerInvariant();
                    if (nz.Contains("panoram") || nz.Contains("niebo") || nz.Contains("horyzont") || nz.Contains("chmur")) continue;
                    int n = BraxCityMeshCut.Subtract(r.transform, nad, BraxRegion.Out + "/Cuts", "nad_kinnaird");
                    if (n > 0) { nadZabytkiem += n; if (kto.Count < 8) kto.Add(r.name); }
                }
            NdLog("KINNAIRD: wolna przestrzeń nad zabytkiem (y > " + (kb.max.y - 1f).ToString("F1") + " m) — wycięte siatki " + nadZabytkiem + (kto.Count > 0 ? " [" + string.Join(", ", kto) + "]" : ""));
        }
        else NdLog("KINNAIRD: brak budynku w scenie — kontrola przestrzeni nad zabytkiem pominięta");
    }

    static void NdMigotanieBudynkow()
    {
        int pary = 0, siatek = 0, nieczytelne = 0;
        foreach (var o in ndBudynki)
        {
            float y0 = o.transform.position.y;
            foreach (var mf in o.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; if (!m) continue;
                try
                {
                    var v = m.vertices; var t = m.triangles; var M = mf.transform.localToWorldMatrix; siatek++;
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        var a = M.MultiplyPoint3x4(v[t[i]]); var b = M.MultiplyPoint3x4(v[t[i + 1]]); var c = M.MultiplyPoint3x4(v[t[i + 2]]);
                        if (Mathf.Abs(a.y - y0) > .02f || Mathf.Abs(b.y - y0) > .02f || Mathf.Abs(c.y - y0) > .02f) continue;
                        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-8f || Mathf.Abs(n.normalized.y) < .95f) continue;
                        pary++;
                    }
                }
                catch (Exception) { nieczytelne++; }
            }
        }
        ndMigotanie += pary;
        NdLog("kontrola podstaw: siatek " + siatek + ", poziomych trójkątów w płaszczyźnie płyt " + pary + (nieczytelne > 0 ? ", siatek nieczytelnych " + nieczytelne : ""));
    }

    // =====================================================================================================================
    // 3) WYPOSAŻENIE NA GOTOWYM PODŁOŻU: sygnalizacja alei, strefy przejść bez świateł, przystanek autobusowy z autobusem,
    //    przystanek tramwajowy z tramwajem i kozłami oporowymi, hydranty, skrzynki pocztowe, bankomat, kontenery na śmieci.
    static void NoweDzielniceWyposazenie()
    {
        Physics.SyncTransforms();
        for (int i = 0; i < 4; i++) { try { SkrzyzowanieT(null, new Vector3(NC[i], YA, RZ), AH, AH, NdWlotyAlei(i), 30f * i); } catch (Exception e) { BraxMiastoAssety.Blad("sygnalizacja Aleja × C" + i, e); } }
        // przejścia bez świateł (ul. Nowojorska, Tramwajowa, wlot zach. pętli): strefy zatrzymania aut jak u B
        try
        {
            var zs = new List<Vector3>(); var zp = new List<Vector3>(); var ss = new List<Vector3>(); var sk = new List<Vector3>(); var szb = new List<int>();
            void Z(Vector3 c, Vector3 dir)
            {
                var R = Vector3.Cross(Vector3.up, dir).normalized; float w = 4f * K * .5f; int i = zs.Count; zs.Add(c);
                var pol = R * (AH + Kr + .8f) + dir * w; zp.Add(new Vector3(Mathf.Abs(pol.x), 2f, Mathf.Abs(pol.z)));
                ss.Add(c - dir * (w + 2.5f) + R * AH * .5f); sk.Add(dir); szb.Add(i); ss.Add(c + dir * (w + 2.5f) - R * AH * .5f); sk.Add(-dir); szb.Add(i);
            }
            float d = AH + Kr + .6f + 2f * K;
            foreach (var (c, c2, ram, prZ, nazwa) in NdSkrzyzowania())
            {
                if (Mathf.Approximately(c2, RZ)) continue;
                if (ram[1]) Z(new Vector3(c + d, YA, c2), Vector3.right); if (ram[3]) Z(new Vector3(c - d, YA, c2), Vector3.left);
                if (ram[0]) Z(new Vector3(c, YA, c2 + d), Vector3.forward); if (ram[2]) Z(new Vector3(c, YA, c2 - d), Vector3.back);
            }
            Z(new Vector3(PetlaZX - PetlaPlac - 3f, YA, RZ), Vector3.left);
            var gz = new GameObject("Strefy zatrzymania: przejścia nowych dzielnic i wlot zach. pętli ZACHÓD"); gz.transform.SetParent(root, false);
            var sd = gz.AddComponent<BraxStrefyDrogowe>();
            // wlot zachodni pętli: bez strefy „ustąp” (w tym etapie nikt nie wjeżdża z alei — auta B zawracają na pętli; strefa
            // przy pierścieniu zatrzymywałaby je bez powodu); znaki A-7 + C-12 stoją jak na wlotach B
            var dirW = Vector3.left; var cP = new Vector3(PetlaZX, YA, RZ);
            sd.zebraSrodek = zs.ToArray(); sd.zebraPolowa = zp.ToArray(); sd.stopSrodek = ss.ToArray(); sd.stopKierunek = sk.ToArray(); sd.stopZebra = szb.ToArray();
            NdLog("strefy: przejść bez świateł " + zs.Count + " (w tym wlot zach. pętli), skrzyżowań z sygnalizacją 4 (Aleja × C0…C3)");
            ZnakRonda(NaPodlozu(cP + dirW * (PetlaPlac + 3f) - Vector3.Cross(Vector3.up, dirW) * (AH + Kr + .5f) + dirW * 4f + Vector3.up * YC), dirW);
        }
        catch (Exception e) { BraxMiastoAssety.Blad("strefy przejść", e); }
        try { NdPrzystanekAutobusu(); } catch (Exception e) { BraxMiastoAssety.Blad("przystanek autobusowy", e); }
        try { NdTramwaj(); } catch (Exception e) { BraxMiastoAssety.Blad("tramwaj", e); }
        try { NdDrobne(); } catch (Exception e) { BraxMiastoAssety.Blad("wyposażenie (hydranty, skrzynki, bankomat, kontenery)", e); }
        try { NdKontrolaNawierzchni(); } catch (Exception e) { BraxMiastoAssety.Blad("kontrola migotania nawierzchni", e); }
        Debug.Log("BRAXHORN_MIASTO_ASSETY_MIGOTANIE pary=" + ndMigotanie + " (nowe dzielnice: płyty, krawężniki, trawniki, wstawki toru, podstawy budynków; asfalt i farba — w kontroli B)");
        Debug.Log("BRAXHORN_MIASTO_ASSETY_USUNIETE (" + ndUsuniete.Count + "): " + string.Join(" | ", ndUsuniete.Take(40)));
        NdLog("wyposażenie: latarni ulic " + ndLatarnie + ", drzew " + ndDrzewa + ", modeli postawionych " + BraxMiastoAssety.Postawione + ", błędów " + BraxMiastoAssety.Bledy);
    }

    // przystanek ALEJA CENTRALNA (pas płd. — autobus jedzie na wschód, do miasta): wiata (model) na chodniku/promenadzie,
    // znacznik przystanku (format RUCH_API), autobus (model, 12 m × K) przodem przy znaczniku, 5 m przed linią zatrzymania C0.
    static void NdPrzystanekAutobusu()
    {
        var znacznik = new GameObject("BRAXHORN przystanek autobusowy: ALEJA CENTRALNA"); znacznik.transform.SetParent(root, false);
        znacznik.transform.position = new Vector3(PrzystBusX, YA, RZ - AH); znacznik.transform.rotation = Quaternion.LookRotation(Vector3.right);
        if (!BraxMiastoAssety.Jest("miasto_wyposazenie") && !BraxMiastoAssety.Jest("miasto_pojazdy")) { NdLog("przystanek: brak paczek wyposażenia i pojazdów — sam znacznik"); return; }
        var wiata = BraxMiastoAssety.Model("miasto_wyposazenie", "miasto_wyposazenie");
        if (wiata)
        {
            var kopia = (GameObject)UnityEngine.Object.Instantiate(wiata); var t = BraxMiastoAssety.Znajdz(kopia.transform, "wiata_przystanku");
            if (t)
            {
                t.SetParent(root, false); t.name = "Wiata przystanku ALEJA CENTRALNA (model)";
                t.position = NaPodlozu(new Vector3(PrzystBusX - 8f, YC, RZ - APR + .2f)); t.rotation = Quaternion.identity;   // właściciel 3.11: „stoi odwrotnie” — model: tylna ściana −z, otwarta +z (do jezdni na płn.); było 180° t.localScale = Vector3.one * K;
                BraxMiastoAssety.Przygotuj(t.gameObject); BraxMiastoAssety.Kolizja(root, t.gameObject, "Wiata kolizja", .3f);
            }
            UnityEngine.Object.DestroyImmediate(kopia);
        }
        // Autobus NIE stoi już w scenie jako oddzielny statyczny collider.
        // Zostanie dołączony do BraxRuchAutSym (te same hamowanie/kolizje
        // co wszystkie auta) w BraxRegionRuchMiasta.AutaMiastaB.
        NdLog("przystanek ALEJA CENTRALNA: wiata i znacznik gotowe; pojazd kursowy z jednej symulacji ruchu");
    }

    // TRAMWAJ T1 (9.11, właściciel 18:21: „drzwi się źle otwierają, nie łamie się na zakrętach, ma elegancko kursować”;
    // 19:44: „ma jechać naokoło całego nowego miasta”). Pętla ~1.2 km po zewnętrznych pasach dookoła całych nowych dzielnic:
    // ul. Tramwajowa na wschód (C3 → C0) → ul. C0 na północ → ul. Nowojorska na zachód (C0 → C3) → ul. C3 na południe.
    // Tylko lewe skręty R 16 m (jedyne, które 44-metrowy skład mieści z prawego pasa jezdni 9 m; zapas ~1 m do narożnika).
    // Wszystkie pętle aut nowych dzielnic jeżdżą zgodnie z ruchem wskazówek zegara (BraxRegionRuchMiasta) — na obwodzie zawsze
    // przeciwnym pasem niż tramwaj; na skrzyżowaniach auta i piesi stają przed tramwajem (BraxTramT1 → BraxStrefyZatrzymania).
    // 6 przystanków (co ~200 m, przy krawężniku po prawej, słup z tablicą + wiata): Tramwajowa Zachód/Wschód, Aleja Centralna (C0),
    // Nowojorska Wschód/Zachód, Plac Atlanta (C3). Skład: model CAF rozcięty w przegubach (BraxTramwajSklad), drzwi po prawej.
    public const float TramR = 16f;
    static void NdTramwaj()
    {
        float zS = NS1 - AH * .5f, xE = NC[0] + AH * .5f, zN = NN1 + AH * .5f, xW = NC[3] - AH * .5f;
        float yToru = YA;
        var trasa = BraxTramwajSklad.Petla(new[] { new Vector2(xW, zS), new Vector2(xE, zS), new Vector2(xE, zN), new Vector2(xW, zN) }, TramR, yToru, 8.3f, 1.1f);
        var stalSzyny = BraxCityMaterials.Surface("Tor: główka szyny (stal wyślizgana)", new Color(.62f, .62f, .62f), .25f, .9f);
        var rowekMat = BraxCityMaterials.Surface("Tor: rowek szyny (ciemny)", new Color(.05f, .05f, .05f), .8f, .3f);
        foreach (var m in new[] { stalSzyny, rowekMat }) { m.SetFloat("_OffsetFactor", -1f); m.SetFloat("_OffsetUnits", -4f); EditorUtility.SetDirty(m); }
        BraxTramwajSklad.Szyny(trasa, Rozstaw * K, .07f * K, .045f * K, root, stalSzyny, rowekMat);
        // ---- przystanki: tablica na słupie, wiata (model) ----
        var tablica = BraxCityMaterials.Surface("Przystanek: tablica granatowa", new Color(.06f, .14f, .32f), .5f);
        var wiataZr = BraxMiastoAssety.Jest("miasto_wyposazenie") ? BraxMiastoAssety.Model("miasto_wyposazenie", "miasto_wyposazenie") : null;
        void Przystanek(string nazwa, Vector3 slup, Vector3 lico, Vector3 wiata, float yawWiaty)
        {
            var sl = new BraxRegionMeble.Bud(); var ps = NaPodlozu(slup); sl.Cyl(ps, ps + Vector3.up * 3.1f * K, .04f * K, 10);
            var rotTab = Quaternion.LookRotation(lico); sl.Box(ps + Vector3.up * 2.65f * K, new Vector3(2.6f * K, .5f * K, .06f), rotTab);
            sl.Obiekt("Przystanek tramwajowy " + nazwa + ": słup z tablicą", root, tablica, false, true);
            BraxRegionMeble.Napis(root, nazwa, ps + Vector3.up * 2.65f * K + lico * .04f, lico, .2f * K, new Color(.95f, .95f, .92f));
            BraxRegionMeble.Napis(root, nazwa, ps + Vector3.up * 2.65f * K - lico * .04f, -lico, .2f * K, new Color(.95f, .95f, .92f));
            if (!wiataZr) return;
            var kopia = (GameObject)UnityEngine.Object.Instantiate(wiataZr); var t = BraxMiastoAssety.Znajdz(kopia.transform, "wiata_przystanku");
            if (t)
            {
                t.SetParent(root, false); t.name = "Wiata przystanku tramwajowego " + nazwa + " (model)";
                t.position = NaPodlozu(wiata); t.rotation = Quaternion.Euler(0f, yawWiaty, 0f); t.localScale = Vector3.one * K;
                BraxMiastoAssety.Przygotuj(t.gameObject); BraxMiastoAssety.Kolizja(root, t.gameObject, "Wiata tramwaju kolizja", .3f);
            }
            UnityEngine.Object.DestroyImmediate(kopia);
        }
        // 6 przystanków: czoło składu stoi w punkcie przystanku; słup z tablicą przy krawężniku przed czołem, wiata przy tylnej
        // krawędzi chodnika w połowie składu (tablica i wiata przodem do jezdni)
        const float DlSkladu = 33.9f * 1.294f;
        var przyst = new (string nazwa, Vector3 czolo)[] {
            ("T1 · TRAMWAJOWA ZACHÓD", new Vector3(-627f, yToru, zS)), ("T1 · TRAMWAJOWA WSCHÓD", new Vector3(-327f, yToru, zS)),
            ("T1 · ALEJA CENTRALNA", new Vector3(xE, yToru, 10f)), ("T1 · NOWOJORSKA WSCHÓD", new Vector3(-372f, yToru, zN)),
            ("T1 · NOWOJORSKA ZACHÓD", new Vector3(-672f, yToru, zN)), ("T1 · PLAC ATLANTA", new Vector3(xW, yToru, -38f)) };
        var sPrzyst = new List<float>();
        foreach (var (nazwaP, czoloP) in przyst)
        {
            float sp = BraxTramwajSklad.SNajblizej(trasa, czoloP); sPrzyst.Add(sp);
            var kier = BraxTramwajSklad.Kierunek(trasa, sp); var prawo = new Vector3(kier.z, 0f, -kier.x);
            float yaw = Mathf.Atan2(-prawo.x, -prawo.z) * Mathf.Rad2Deg;
            Przystanek(nazwaP, czoloP + kier * 2.5f + prawo * (AH * .5f + Kr + 1.0f), -prawo, czoloP - kier * (DlSkladu * .5f) + prawo * (AH * .5f + Kr + Chod - 1.4f), yaw);
        }
        // ---- skład ----
        var tram = BraxMiastoAssety.Jest("miasto_pojazdy") ? BraxMiastoAssety.Model("miasto_pojazdy", "tramwaj_caf") : null;
        if (!tram) { NdLog("tramwaj: brak modelu (paczka miasto_pojazdy) — pętla z szynami i przystankami bez tramwaju"); return; }
        float xPrzystA = przyst[0].czolo.x;
        var o = BraxMiastoAssety.Postaw(root, tram, "Tramwaj T1 (model CAF, przed podziałem)", new Vector3(xPrzystA - 22f, YA, zS), 0f, 1f, false, true);
        if (!o) { NdLog("DO POPRAWKI: nie udało się utworzyć tramwaju CAF"); return; }
        BraxTramwajSklad.Spis(o);
        // model wzdłuż +x: czoło (x max) na przystanku A, oś na torze, koła na główkach szyn
        var b0 = BraxRegion.Bounds(o);
        o.transform.position += new Vector3(xPrzystA - b0.max.x, yToru + .012f - b0.min.y, zS - b0.center.z);
        var b = BraxRegion.Bounds(o);
        var vehicle = new GameObject("BRAXHORN TRAMWAJ T1 (pętla, człony, drzwi)").transform; vehicle.SetParent(root, false); vehicle.position = new Vector3(b.center.x, yToru, zS);
        var przeguby = BraxTramwajSklad.Przeguby(o, out var jakPrzeguby);
        var granice = new List<float> { b.min.x }; granice.AddRange(przeguby.OrderBy(x => x)); granice.Add(b.max.x);
        string folder = BraxRegion.Out + "/Tramwaj";
        var czlony = new List<Transform>(); var przod = new List<float>(); var tyl = new List<float>();
        // człony od czoła (x max) do tyłu
        for (int i = granice.Count - 2; i >= 0; i--)
        {
            float xa = granice[i], xb = granice[i + 1];
            var cz = BraxTramwajSklad.Czlon(o, vehicle, czlony.Count, xa, xb, yToru, zS, folder);
            czlony.Add(cz); przod.Add(b.max.x - xb); tyl.Add(b.max.x - xa);
        }
        UnityEngine.Object.DestroyImmediate(o);
        var skrzydla = BraxTramwajSklad.Drzwi(czlony.ToArray(), b.min.y + .3f, folder, out var jakDrzwi);
        var svc = vehicle.gameObject.AddComponent<BraxTramT1>();
        svc.trasa = trasa.p.ToArray(); svc.trasaS = trasa.s.ToArray(); svc.vLimit = trasa.v.ToArray(); svc.dlugosc = trasa.dl;
        svc.czlony = czlony.ToArray(); svc.czlonPrzod = przod.ToArray(); svc.czlonTyl = tyl.ToArray();
        float sA = sPrzyst[0];
        svc.vmax = 8.3f;   // 30 km/h na prostych pętli 1.2 km (łuki ≤ √(1.1·R) ≈ 4.2 m/s)
        svc.s = sA; svc.przystanki = sPrzyst.ToArray(); svc.przystankiNazwy = przyst.Select(q => q.nazwa).ToArray();
        // wszystkie skrzyżowania siatki — te, przez które pętla nie przechodzi, odpadają (SPrzedzial = −1)
        var srodki = new List<Vector2>(); foreach (var cx in NC) foreach (var cz2 in new[] { NS1, RZ, NN1 }) srodki.Add(new Vector2(cx, cz2));
        var od = new List<float>(); var dok = new List<float>(); var sr = new List<Vector3>();
        foreach (var c in srodki) { var p = BraxTramwajSklad.SPrzedzial(trasa, c, AH + 2f); if (p.x < 0f) continue; od.Add(p.x); dok.Add(p.y); sr.Add(new Vector3(c.x, YA, c.y)); }
        svc.skrzyzowanieOd = od.ToArray(); svc.skrzyzowanieDo = dok.ToArray(); svc.skrzyzowanieSrodek = sr.ToArray(); svc.skrzyzowaniePromien = AH + 5f;
        svc.drzwi = skrzydla.Select(q => q.t).ToArray(); svc.drzwiDom = skrzydla.Select(q => q.dom).ToArray();
        svc.drzwiWysun = skrzydla.Select(q => q.wysun).ToArray(); svc.drzwiPrzesun = skrzydla.Select(q => q.przesun).ToArray();
        svc.zachodX = xW; svc.wschodX = xE; svc.torZ = zS; svc.peronX0 = PeronTramwaju.xMin; svc.peronX1 = PeronTramwaju.xMax; svc.peronZ = PeronTramwaju.center.y;
        // ustawienie członów na torze już w scenie (pierwsza klatka bez skoku)
        for (int i = 0; i < czlony.Count; i++)
        {
            Vector3 P(float x) { x %= trasa.dl; if (x < 0) x += trasa.dl; int k = trasa.s.FindLastIndex(q => q <= x); if (k < 0) k = 0; int k2 = (k + 1) % trasa.p.Count; float a0 = trasa.s[k], a1 = k2 == 0 ? trasa.dl : trasa.s[k2]; return Vector3.Lerp(trasa.p[k], trasa.p[k2], a1 > a0 ? (x - a0) / (a1 - a0) : 0f); }
            var f = P(sA - przod[i]); var r = P(sA - tyl[i]); var d = f - r; d.y = 0f;
            czlony[i].position = (f + r) * .5f; if (d.sqrMagnitude > 1e-6f) czlony[i].rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
        NdLog("tramwaj T1: pętla " + trasa.dl.ToString("F0") + " m dookoła nowych dzielnic (Tramwajowa, C0, Nowojorska, C3; łuki R " + TramR + " m), członów " + czlony.Count + " (" + jakPrzeguby + "), drzwi: " + jakDrzwi +
              ", przystanki " + przyst.Length + ": " + string.Join(", ", przyst.Select((q, i) => q.nazwa + " (s " + sPrzyst[i].ToString("F0") + ")")) + ", skrzyżowania " + od.Count);
    }

    static void NdDrobne()
    {
        if (!BraxMiastoAssety.Jest("miasto_wyposazenie")) { NdLog("wyposażenie: brak paczki miasto_wyposazenie"); return; }
        var src = BraxMiastoAssety.Model("miasto_wyposazenie", "miasto_wyposazenie"); if (!src) return;
        var kopia = (GameObject)UnityEngine.Object.Instantiate(src);
        var wzory = new Dictionary<string, GameObject>();
        foreach (var n in new[] { "hydrant", "skrzynka_pocztowa", "bankomat", "kontener_smieci" }) { var t = BraxMiastoAssety.Znajdz(kopia.transform, n); if (t) { t.SetParent(null, true); wzory[n] = t.gameObject; } }
        UnityEngine.Object.DestroyImmediate(kopia);
        int n2 = 0;
        void P(string co, Vector3 p, float yaw, bool kol = true)
        {
            if (!wzory.TryGetValue(co, out var w)) return;
            var o = (GameObject)UnityEngine.Object.Instantiate(w, root, false); o.name = co + " (model)";
            o.transform.position = NaPodlozu(p); o.transform.rotation = Quaternion.Euler(0, yaw, 0); o.transform.localScale = Vector3.one * K;
            BraxMiastoAssety.Przygotuj(o); if (kol) BraxMiastoAssety.Kolizja(root, o, co + " kolizja", .05f); n2++;
        }
        // hydranty: na chodnikach ulic C, 18 m od osi alei, przy krawężniku (poza zebrami i słupami)
        for (int i = 0; i < 4; i++) { P("hydrant", new Vector3(NC[i] + AH + Kr + 1.2f, YC, RZ + APR + 6f), 0f); P("hydrant", new Vector3(NC[i] - AH - Kr - 1.2f, YC, RZ - APR - 6f), 180f); }
        // skrzynki pocztowe
        P("skrzynka_pocztowa", new Vector3(-372f, YC, 13.6f), 0f); P("skrzynka_pocztowa", new Vector3(-520f, YC, RzB0 + 1.2f), 180f); P("skrzynka_pocztowa", new Vector3(-660f, YC, 13.6f), 0f);
        // bankomat przy banku (KINNAIRD) — przy balustradzie od promenady, ekranem do ludzi
        var kin = NoweBudynki.First(b => b.id == "KINNAIRD");
        P("bankomat", new Vector3(kin.x + 8f, YC, PromenadaZ + .45f), 0f);
        P("bankomat", new Vector3(kin.x + 11f, YC, PromenadaZ + .45f), 0f);
        // kontenery na śmieci: w przerwach między budynkami rzędów A i C (od zaplecza)
        foreach (var (z, yaw) in new[] { (RzA1 - 3f, 0f), (RzC0 + 3f, 180f) })
            for (int i = 0; i < 3; i++)
            {
                float xa = NC[i + 1] + NPR, xb = NC[i] - NPR;
                var wRzedzie = NoweBudynki.Where(b => Mathf.Abs(b.z - (z > 0 ? 60f : -22f)) < 12f && b.x > xa && b.x < xb).OrderBy(b => b.x).ToList();
                for (int k = 0; k + 1 < wRzedzie.Count; k++)
                {
                    float x = (wRzedzie[k].x + wRzedzie[k].w * .5f + wRzedzie[k + 1].x - wRzedzie[k + 1].w * .5f) * .5f;
                    P("kontener_smieci", new Vector3(x, YC, z), yaw);
                }
            }
        foreach (var w in wzory.Values) if (w) UnityEngine.Object.DestroyImmediate(w);
        NdLog("wyposażenie (modele TampaJoey): " + n2 + " szt. — hydranty, skrzynki POCZTA, bankomaty przy banku, kontenery w zapleczach");
    }

    // kontrola migotania nawierzchni nowych dzielnic: wszystkie płyty/krawężniki/trawniki utworzone w tych krokach (dzieci root od
    // ndRootStart) — pary prostopadłościanów nachodzących na siebie w rzucie z tą samą górną płaszczyzną (±1 cm)
    static void NdKontrolaNawierzchni()
    {
        var plyty = new List<(string n, Bounds b)>();
        for (int i = ndRootStart; i < root.childCount; i++)
        {
            var t = root.GetChild(i); var r = t.GetComponent<MeshRenderer>(); if (!r) continue;
            var b = r.bounds; if (b.size.y > .45f || b.max.y > YC + .05f || b.max.y < YC - .1f || b.center.x > NX + .5f) continue;   // tylko teren nowych dzielnic
            if (!(t.name.StartsWith("Chodnik") || t.name.StartsWith("Krawężnik") || t.name.StartsWith("Plac ATLANTA") || t.name.StartsWith("Podłoże trawnika"))) continue;
            plyty.Add((t.name, b));
        }
        int pary = 0;
        for (int i = 0; i < plyty.Count; i++)
            for (int j = i + 1; j < plyty.Count; j++)
            {
                var a = plyty[i].b; var c = plyty[j].b;
                if (Mathf.Abs(a.max.y - c.max.y) > .01f) continue;
                float ox = Mathf.Min(a.max.x, c.max.x) - Mathf.Max(a.min.x, c.min.x), oz = Mathf.Min(a.max.z, c.max.z) - Mathf.Max(a.min.z, c.min.z);
                if (ox > .01f && oz > .01f) { pary++; if (pary <= 8) Debug.LogWarning("BRAXHORN_MIASTO_ASSETY_MIGOTANIE " + plyty[i].n + " × " + plyty[j].n + " (" + ox.ToString("0.00") + " × " + oz.ToString("0.00") + " m)"); }
            }
        ndMigotanie += pary;
        NdLog("kontrola nawierzchni: płyt/krawężników/podłoży " + plyty.Count + ", par w tej samej płaszczyźnie " + pary);
    }

    // =====================================================================================================================
    // 4) REJESTRACJA JEZDNI (RUCH_API) — osie równoległe do osi świata; ruch aut w nowych dzielnicach: NIE w tym etapie
    static void NoweDzielniceRejestracja()
    {
        void J(string id, Vector3 a, Vector3 b, float pol) => BraxClaudeOsiedle.OsieJezdni.Add(new BraxClaudeOsiedle.OsJezdni { id = id, a = a, b = b, polowaJezdni = pol });
        J("MIASTO Aleja Centralna; chodniki " + (Ziel + Chod) + " " + (Ziel + Chod), new Vector3(PetlaZX - PetlaR, YA, RZ), new Vector3(NC[3], YA, RZ), AH);
        for (int i = 0; i < 4; i++) J("MIASTO ul. C" + i + "; chodniki " + Chod + " " + Chod, new Vector3(NC[i], YA, NS1), new Vector3(NC[i], YA, NN1), AH);
        J("MIASTO ul. Nowojorska; chodniki " + Chod + " " + Chod, new Vector3(NC[0], YA, NN1), new Vector3(NC[3], YA, NN1), AH);
        J("MIASTO ul. Tramwajowa; chodniki " + Chod + " " + Chod, new Vector3(NC[0], YA, NS1), new Vector3(NC[3], YA, NS1), AH);
        NdLog("rejestracja jezdni: Aleja Centralna, C0–C3, Nowojorska, Tramwajowa (ruch aut i piesi w nowych dzielnicach — kolejny etap)");
    }
}

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
    static int ndMigotanie, ndLatarnie, ndDrzewa, ndZebry;
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
        ndRootStart = root.childCount; ndLatarnie = ndDrzewa = ndZebry = ndMigotanie = 0; ndWstawkiToru.Clear(); ndBudynki.Clear();
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
        float zebraA = AH + Kr + .6f, zebraB = zebraA + 4f * K;   // zebra wlotu skrzyżowania: od 5.35 do 10.53 m od środka
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

    static readonly Rect PeronTramwaju = Rect.MinMaxRect(-512f, NS1 - NPR - 6f, -458f, NS1 - NPR);   // płyty przystanku za chodnikiem
    const float PrzystBusX = -318f;                                                                 // przód autobusu na przystanku ALEJA CENTRALNA

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
                Pozniej("ławka (promenada)", () => BraxRegionMeble.Lawka(root, NaPodlozu(new Vector3(xx + 3.2f, YC, 9.6f)), 180f));
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
            float d = AH + Kr + .6f + 2f * K;
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
        float roz = Rozstaw * K, glowka = .07f * K, rowek = .045f * K;
        foreach (float zt in new[] { NS1 - AH * .5f, NS1 + AH * .5f })
            foreach (int s in new[] { -1, 1 })
            {
                float zc = zt + s * (roz * .5f + glowka * .5f);
                ndWstawkiToru.Add((Rect.MinMaxRect(TorRect.xMin + .8f, zc - glowka * .5f, TorRect.xMax - .8f, zc + glowka * .5f), 1));
                float zr = zt + s * (roz * .5f - rowek * .5f);   // rowek po wewnętrznej stronie główki
                ndWstawkiToru.Add((Rect.MinMaxRect(TorRect.xMin + .8f, zr - rowek * .5f, TorRect.xMax - .8f, zr + rowek * .5f), 2));
            }
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
        NdLog("torowisko: 2 tory × 2 szyny rowkowe (rozstaw " + (Rozstaw * K).ToString("0.000") + " m = 1.435 × K) x " + (TorRect.xMin + .8f).ToString("0.0") + "…" + (TorRect.xMax - .8f).ToString("0.0") + " — wstawki w wycięciach asfaltu, wstawek " + ndWstawkiToru.Count);
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
                nSzklo += NdSzklo(bud.gameObject);
                foreach (var r in dach) r.shadowCastingMode = ShadowCastingMode.Off;
                // LOD: urządzenia dachowe do 12 % wysokości ekranu, bryła do 0.6 %
                var lod = bud.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(dach.Length > 0 ? new[] { new LOD(.12f, bryla.Concat(dach).ToArray()), new LOD(.006f, bryla) } : new[] { new LOD(.006f, bryla) });
                lod.RecalculateBounds();
                // kolizja bryły: prostokąt lica × wysokość (osie świata — obroty 0/90)
                var gr = BraxRegion.Bounds(bud.gameObject);
                var col = new GameObject("Budynek " + b.id + " kolizja").AddComponent<BoxCollider>(); col.transform.SetParent(g, false);
                col.transform.position = new Vector3(b.x, y + (gr.max.y - y) * .5f, b.z); col.size = new Vector3(b.w - .1f, gr.max.y - y, b.d - .1f);
                ndBudynki.Add(bud.gameObject); ok++;
                NdLog("budynek " + b.id + " (" + b.plik + (b.wezel != null ? " " + b.wezel : "") + "): środek (" + b.x.ToString("0.0") + ", " + y.ToString("0.00") + ", " + b.z.ToString("0.0") + "), obrót " + b.yaw + "°, lico " + b.w.ToString("0.0") + " × " + b.d.ToString("0.0") + " m, wys. " + (gr.max.y - y).ToString("0") + " m");
            }
            catch (Exception e) { BraxMiastoAssety.Blad("budynek " + b.id, e); }
        }
        foreach (var k in wezly.Values) if (k) UnityEngine.Object.DestroyImmediate(k);   // niewykorzystane jednostki (U14, kliny) nie idą do sceny
        // sylwetki z daleka wyłączone (budynki zostają w scenie głównej — patrz wyżej)
        try { NdMigotanieBudynkow(); } catch (Exception e) { BraxMiastoAssety.Blad("kontrola podstaw budynków", e); }
        NdLog("szkło fasad: " + nSzklo + " materiałów na budynkach zamienione na szkło refleksyjne (czarny metal / szyby przed pustą bryłą)");
        NdLog("budynki: postawione " + ok + "/" + NoweBudynki.Length + (brak > 0 ? ", bez modelu " + brak + " (paczka nieodszyfrowana — kwartał zostaje z płytami)" : ""));
    }

    // WŁAŚCICIEL 3.11 10:12–10:19 (nagrania): „czarne budynki, gdzie są okna”. W modelach Sketchfaba szkło fasad było
    // (a) metalem bez tekstury z ciemnym kolorem (CityGenbasic_rough_metal_dark — kolor 0,0,0; CityGenblack_) — metal z albedo
    // ≈ 0 nic nie odbija = czarna ściana; (b) szybą półprzezroczystą (CityGenGlass, alfa .25) przed pustą bryłą = czarna dziura.
    // Oba → szkło fasady refleksyjne jak wieżowce miasta (BraxSzyby.Refleksyjne: stalowo-niebieskie, odbicie nieba).
    // Okna malowane w teksturach elewacji zostają. Środki budynków nie są robione (właściciel: „środek nie musi być”).
    static Material ndSzklo;
    static int NdSzklo(GameObject o)
    {
        if (!ndSzklo)
        {
            ndSzklo = BraxCityMaterials.Surface("Nowe dzielnice: szkło fasady (refleksyjne)", new Color(.30f, .37f, .42f), .1f, .85f);
            if (ndSzklo.HasProperty("_ReflectionStrength")) ndSzklo.SetFloat("_ReflectionStrength", .95f);
        }
        int n = 0;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials; bool zm = false;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i]; if (!m || m == ndSzklo) continue;
                bool szklo = m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
                bool ciemnyMetal = m.HasProperty("_Metallic") && m.GetFloat("_Metallic") >= .8f && m.HasProperty("_MainTex") && !m.GetTexture("_MainTex")
                                   && m.HasProperty("_Color") && m.GetColor("_Color").grayscale < .2f;
                if (szklo || ciemnyMetal) { ms[i] = ndSzklo; zm = true; n++; }
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
        var bus = BraxMiastoAssety.Model("miasto_pojazdy", "autobus");
        if (bus)
        {
            float dl = 12f * K, szer = 2.55f * K;
            var o = BraxMiastoAssety.Postaw(root, bus, "Autobus na przystanku ALEJA CENTRALNA (model)", new Vector3(PrzystBusX - dl * .5f, YA, RZ - AH + szer * .5f + .35f), 90f, 1f, false, true);
            foreach (var t in o.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            BraxMiastoAssety.Kolizja(root, o, "Autobus kolizja", .1f);
            NdLog("przystanek ALEJA CENTRALNA: autobus " + dl.ToString("0.0") + " m (12 m × K) stoi przodem przy znaczniku x " + PrzystBusX + " (5 m przed linią zatrzymania C0), wiata na promenadzie");
        }
    }

    // tramwaj: przystanek przy krawężniku ul. Tramwajowej (pas płd., jazda na wschód), peron z płyt za chodnikiem, wiata (model),
    // tablica „T1 · KRAŃCÓWKA ZACHÓD”, tramwaj CAF (33.9 m × K) kołami na szynach, kozły oporowe na końcach torów.
    static void NdTramwaj()
    {
        float zt = NS1 - AH * .5f;
        var t1 = new GameObject("BRAXHORN przystanek tramwajowy: T1 KRAŃCÓWKA ZACHÓD"); t1.transform.SetParent(root, false);
        t1.transform.position = new Vector3(-462f, YA, NS1 - AH); t1.transform.rotation = Quaternion.LookRotation(Vector3.right);
        BraxRegionMeble.Napis(root, "T1 · KRAŃCÓWKA ZACHÓD", NaPodlozu(new Vector3(-468f, YC, NS1 - NPR - .4f)) + Vector3.up * 2.6f * K, Vector3.forward, .26f * K, new Color(.95f, .95f, .92f));
        var sl = new BraxRegionMeble.Bud(); var ps = NaPodlozu(new Vector3(-468f, YC, NS1 - NPR - .5f)); sl.Cyl(ps, ps + Vector3.up * 3.1f * K, .04f * K, 10);
        sl.Box(ps + Vector3.up * 2.65f * K, new Vector3(2.6f * K, .5f * K, .06f)); sl.Obiekt("Przystanek tramwajowy: słup z tablicą", root, BraxCityMaterials.Surface("Przystanek: tablica granatowa", new Color(.06f, .14f, .32f), .5f), false, true);
        // kozły oporowe (stal, pasy czerwono-białe) na obu końcach obu torów — bryły na asfalcie, nie płaszczyzny
        var koz = new BraxRegionMeble.Bud(); var pas = new BraxRegionMeble.Bud();
        foreach (float z in new[] { NS1 - AH * .5f, NS1 + AH * .5f })
            foreach (float x in new[] { TorRect.xMin + 1.2f, TorRect.xMax - 1.2f })
            {
                koz.Box(new Vector3(x, YA + .45f * K, z), new Vector3(.35f * K, .9f * K, Rozstaw * K + .5f));
                pas.Box(new Vector3(x + (x < TorRect.center.x ? .18f : -.18f) * K, YA + .62f * K, z), new Vector3(.02f, .35f * K, Rozstaw * K + .3f));
                BraxRegionMeble.Kolizja(root, "Kozioł oporowy kolizja", new Vector3(x, YA + .45f * K, z), new Vector3(.35f * K, .9f * K, Rozstaw * K + .5f));
            }
        koz.Obiekt("Kozły oporowe (stal)", root, stal, false, true); pas.Obiekt("Kozły oporowe: tarcze czerwono-białe", root, BraxCityMaterials.Surface("Kozioł: tarcza czerwona", new Color(.75f, .08f, .08f), .5f), false, false);
        if (BraxMiastoAssety.Jest("miasto_wyposazenie"))
        {
            var wiata = BraxMiastoAssety.Model("miasto_wyposazenie", "miasto_wyposazenie");
            var kopia = wiata ? (GameObject)UnityEngine.Object.Instantiate(wiata) : null; var t = kopia ? BraxMiastoAssety.Znajdz(kopia.transform, "wiata_przystanku") : null;
            if (t)
            {
                t.SetParent(root, false); t.name = "Wiata przystanku tramwajowego (model)";
                t.position = NaPodlozu(new Vector3(-478f, YC, PeronTramwaju.center.y)); t.rotation = Quaternion.identity; t.localScale = Vector3.one * K;
                BraxMiastoAssety.Przygotuj(t.gameObject); BraxMiastoAssety.Kolizja(root, t.gameObject, "Wiata tramwaju kolizja", .3f);
            }
            if (kopia) UnityEngine.Object.DestroyImmediate(kopia);
        }
        var tram = BraxMiastoAssety.Jest("miasto_pojazdy") ? BraxMiastoAssety.Model("miasto_pojazdy", "tramwaj_caf") : null;
        if (!tram) { NdLog("tramwaj: brak modelu (paczka miasto_pojazdy) — torowisko, peron i tablica bez tramwaju"); return; }
        var o = BraxMiastoAssety.Postaw(root, tram, "Tramwaj T1 na przystanku (model CAF)", new Vector3(-463.5f - 33.9f * K * .5f, YA, zt), 0f, 1f, false, true);
        var b = BraxRegion.Bounds(o);
        o.transform.position += new Vector3((-463.5f - b.max.x), YA - b.min.y, zt - b.center.z);   // przód 2.3 m przed kozłem oporowym
        foreach (var tr in o.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
        BraxMiastoAssety.Kolizja(root, o, "Tramwaj kolizja", .15f);
        b = BraxRegion.Bounds(o);
        NdLog("tramwaj: CAF " + b.size.x.ToString("0.0") + " m (33.9 × K) na torze płd. z " + zt.ToString("0.00") + ", x " + b.min.x.ToString("0.0") + "…" + b.max.x.ToString("0.0") + ", koła y " + b.min.y.ToString("0.000") + " (asfalt/szyny " + YA + ")");
    }

    // drobne: hydranty przy narożnikach ulic C, skrzynki pocztowe na promenadzie i przy ul. Nowojorskiej, bankomat przy banku
    // (KINNAIRD), kontenery na śmieci w przerwach między budynkami (zaplecza). Modele TampaJoey — tylko to, czego w grze brakowało.
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

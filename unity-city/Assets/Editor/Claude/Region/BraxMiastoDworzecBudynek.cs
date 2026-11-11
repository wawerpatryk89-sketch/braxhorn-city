using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

// DWORZEC R1 — BUDYNEK, WNĘTRZE, WIATY, KŁADKA, WYPOSAŻENIE (11.11.2026; koncepcja v3 zaakceptowana 16:16: „teraz to już od razu
// lepiej wygląda”). Hala 21.2 × 15.5 m między pylonami z kamienia, ściana kurtynowa (czarne okucia — zasada 11.11), 3 wejścia
// automatyczne, dach jednospadowy z nadwieszeniem 7 m nad placem, podsufitka z lameli teak, napis DWORZEC na czole (litery 3D,
// Bebas Neue). Wieża zegarowa (zegary chodzą — BraxZegarDworca). Skrzydła KOLEJ (poczekalnia, wyjście na peron 1) i AUTOBUSY
// (poczekalnia, biletomaty). Wnętrze hali: kasy (3 stanowiska z obsługą), informacja (wyspa), kawiarnia (lada, hokery Poly Haven),
// WC (tablica niebieska z białym napisem — właściciel 16:13). Meble kodem: rama dębowa, poduchy z gąbką obite skórą, podłokietniki
// zaokrąglone (15:55). Wszystkie tablice 3D i zamocowane: na ścianie, na cięgnach do belek albo na własnym słupie (16:08).
// Skala ×K: siedzisko 0.58 m, lada 1.36, blat 0.97, drzwi 2.92, balustrada 1.42, stopień 0.205 / 0.388.
internal static partial class BraxMiastoDworzec
{
    const float HX0 = -28.6f, HX1 = -7.4f, HZ0 = BudZ0, HZ1 = -139.55f;   // hala (wnętrze między pylonami)
    const float Y0 = YP;                                                   // posadzka hali = plac = peron
    static float Y(float h) => Y0 + h;                                     // wysokość nad posadzką
    static Material tynk;

    // ============================================================================ BUDYNEK
    static void Budynek()
    {
        tynk = T("tynk biały (sufity i ściany wnętrz)", 2.0f, Color.white, .9f, 0f, "white_plaster_02", "white_stucco");
        Grupa("budynek");
        // pylony hali (kamień), front wysunięty 0.35 m
        // pylony: wierzch schodkami pod spodem dachu (dach opada ku tyłowi) — bez szczeliny między murem a dachem
        // pylon wschodni: przejścia do WC (drzwi na zawiasach od strony hali — BraxMiastoDworzec.WC)
        // cięcia co 2 m (bez cięcia w linii frontu placu z −124 — lico kawałka nie leży na boku płyty placu)
        var cieciaZ = new List<float> { BudZ1, HZ0 + .35f }; for (float z = BudZ1 + 2f; z < HZ0 - .5f; z += 2f) cieciaZ.Add(z);
        foreach (var (a, b) in WcPrzejscia) { cieciaZ.Add(a); cieciaZ.Add(b); }
        cieciaZ.Sort();
        for (int i = 0; i + 1 < cieciaZ.Count; i++)
        {
            float z = cieciaZ[i], z1 = cieciaZ[i + 1]; if (z1 - z < .01f) continue;
            float top = SpodDachu(z) + .3f;
            B(kamien, -30.2f, Y0 - .3f, z, HX0, top, z1);
            bool przejscie = WcPrzejscia.Any(q => z >= q.a - .001f && z1 <= q.b + .001f);
            B(kamien, HX1, przejscie ? Y(WcDrzwiH + .15f) : Y0 - .3f, z, -5.8f, top, z1);
        }
        Kol("pylon zach.", V(-30.2f, Y0, BudZ1), V(HX0, Y(10f), HZ0 + .35f));
        float kz = BudZ1;
        foreach (var (a, b) in WcPrzejscia) { Kol("pylon wsch.", V(HX1, Y0, kz), V(-5.8f, Y(10f), a)); Kol("pylon wsch. nad przejściem do WC", V(HX1, Y(WcDrzwiH + .15f), a), V(-5.8f, Y(10f), b)); kz = b; }
        Kol("pylon wsch.", V(HX1, Y0, kz), V(-5.8f, Y(10f), HZ0 + .35f));
        // łącznik za wieżą (między skrzydłem KOLEJ a halą)
        B(kamien, -34.6f, Y0 - .3f, BudZ1, -30.2f, Y(7.8f), HZ0 - 3.2f);
        B(granat, -34.48f, Y(7.8f), BudZ1 - .15f, -30.2f, Y(8.05f), HZ0 - 3.2f);   // czapa od lica attyki skrzydła, pod jej nakrywą (bez wspólnej płaszczyzny)
        // ściana kurtynowa: tafle między czarnymi słupkami (1.325 m), rygle 3.1 / 6.4 / 9.0. Wejścia: drzwi automatyczne w świetle
        // dwóch pól (między słupkami xd ± 1.325, 2.57 m); słupek środkowy zaczyna się nad nadprożem, cokół przerwany. Skrzydła po
        // stronie hali (z −124.05) — otwarte chowają się za taflami bocznymi (bez przechodzenia przez słupki). Szkło w głębi słupków
        // (z +0.1) — lica szkła nie leżą na licach rygli i słupków.
        float[] drzwiX = { -23.3f, -18f, -12.7f }; const float dh = 2.92f;
        int n = 16; float krok = (HX1 - HX0) / n;
        float dw = 2f * krok - .08f;
        bool WDrzwiach(float xa, float xb) => drzwiX.Any(xd => xb > xd - krok + .05f && xa < xd + krok - .05f);
        for (int i = 0; i < n; i++)
        {
            float a = HX0 + i * krok + .04f, b = HX0 + (i + 1) * krok - .04f;
            B(szklo, a, WDrzwiach(a, b) ? Y(3.15f) : Y(.12f), HZ0 + .09f, b, Y(11.3f), HZ0 + .11f);   // nad drzwiami od wierzchu rygla 3.1
        }
        for (int i = 0; i <= n; i++) { float x = HX0 + i * krok; bool srodkowy = drzwiX.Any(xd => Mathf.Abs(x - xd) < .01f); B(czarnyAlu, x - .04f, srodkowy ? Y(3.15f) : Y(0f), HZ0, x + .04f, Y(11.3f), HZ0 + .24f); }
        foreach (float y in new[] { 3.1f, 6.4f, 9.0f }) B(czarnyAlu, HX0, Y(y - .05f), HZ0, HX1, Y(y + .05f), HZ0 + .26f);
        float xc = HX0;
        foreach (var xd in drzwiX) { B(czarnyAlu, xc, Y(0f), HZ0, xd - krok + .04f, Y(.12f), HZ0 + .3f); xc = xd + krok - .04f; }
        B(czarnyAlu, xc, Y(0f), HZ0, HX1, Y(.12f), HZ0 + .3f);
        B(czarnyAlu, HX0, Y(11.3f), HZ0 - .1f, HX1, Y(11.45f), HZ0 + .3f);
        foreach (var xd in drzwiX)
        {
            B(czarnyAlu, xd - krok + .04f, Y(dh), HZ0, xd + krok - .04f, Y(dh + .13f), HZ0 + .24f);   // nadproże drzwi między słupkami
            BraxMiastoA.DrzwiPrzesuwne(root, "hala dworca — wejście " + (xd < -20f ? "zach." : xd > -15f ? "wsch." : "środkowe"), V(xd, Y0, HZ0 - .05f), Vector3.right, dw, dh, szklo, czarnyAlu);
        }
        // kolizja ściany kurtynowej poza drzwiami
        float xp = HX0;
        foreach (var xd in drzwiX) { Kol("ściana kurtynowa", V(xp, Y0, HZ0), V(xd - krok + .04f, Y(11.4f), HZ0 + .3f)); xp = xd + krok - .04f; }
        Kol("ściana kurtynowa", V(xp, Y0, HZ0), V(HX1, Y(11.4f), HZ0 + .3f));
        // lamele teak przed górną częścią szkła; wsporniki w połowie odstępu między lamelami (co 2.5 m — nie w lameli)
        Grupa("budynek: lamele", 180f);
        for (float x = HX0 + .2f; x <= HX1 - .19f; x += .5f) B(drewno, x - .045f, Y(6.5f), HZ0 + .42f, x + .045f, Y(10.05f), HZ0 + .6f);
        for (float x = HX0 + .45f; x < HX1; x += 2.5f) B(czarnyAlu, x - .03f, Y(6.4f), HZ0 + .24f, x + .03f, Y(10.1f), HZ0 + .64f);
        Dach();
        Wieza();
        Grupa("budynek");
        // ściana tylna hali: kamień z otworem na drzwi peronowe (x −22.4…−13.6, wys. 3.45)
        float yt = SpodDachu(BudZ1) + .3f;
        B(kamien, HX0, Y0 - .3f, BudZ1, -22.4f, yt, HZ1); B(kamien, -13.6f, Y0 - .3f, BudZ1, HX1, yt, HZ1);
        B(kamien, -22.4f, Y(3.45f), BudZ1, -13.6f, yt, HZ1);
        Kol("ściana tylna hali (zach.)", V(HX0, Y0, BudZ1), V(-22.4f, yt, HZ1)); Kol("ściana tylna hali (wsch.)", V(-13.6f, Y0, BudZ1), V(HX1, yt, HZ1));
        Rama(-22.4f, -13.6f, Y(0f), Y(3.45f), HZ1 - .12f, HZ1 + .02f, .12f);
        float zm = (HZ1 + BudZ1) * .5f;
        // w grubości muru (z −140…−139.67): tafle stałe ze słupkami przy stronie peronu, skrzydła drzwi zachodnich i wschodnich
        // w dwóch płaszczyznach — przy otwarciu nachodzą na pole środkowe jedno za drugim, nie przez siebie (kieszeń w murze po bokach)
        float zg = BudZ1 + .065f;
        float[] zDrzwi = { BudZ1 + .245f, BudZ1 + .155f };
        int nd = 0;
        foreach (float xd in new[] { -20.2f, -15.8f })
            BraxMiastoA.DrzwiPrzesuwne(root, "hala dworca — wyjście na perony", V(xd, Y0, zDrzwi[nd++]), Vector3.right, 2.6f, 2.95f, szklo, czarnyAlu);
        // słupki (skrajne przy murze 12 cm, środkowe 8 cm), nadproże pełne nad drzwiami i taflami, tafle między licami słupków na
        // progu 5 cm; próg z posadzki w grubości muru (hala ↔ peron 1 bez szczeliny)
        Plyta(posadzka, -22.4f, BudZ1, -13.6f, HZ1, Y0, .3f);
        B(czarnyAlu, -22.4f, Y(2.98f), zg - .06f, -13.6f, Y(3.45f), zg + .06f);
        foreach (var (x0s, x1s) in new[] { (-22.4f, -22.28f), (-21.54f, -21.46f), (-18.94f, -18.86f), (-17.14f, -17.06f), (-14.54f, -14.46f), (-13.72f, -13.6f) })
            B(czarnyAlu, x0s, Y0, zg - .06f, x1s, Y(2.98f), zg + .06f);
        foreach (var (a, b) in new[] { (-22.28f, -21.54f), (-18.86f, -17.14f), (-14.46f, -13.72f) })
        {
            B(czarnyAlu, a, Y0, zg - .06f, b, Y(.05f), zg + .06f);
            B(szklo, a, Y(.05f), zg - .01f, b, Y(2.98f), zg + .01f);
            Kol("ściana szklana drzwi peronowych", V(a, Y0, zg - .06f), V(b, Y(3.4f), zg + .06f));
        }
        Skrzydlo(-52.6f, -34.6f, "KOLEJ", true, -43.6f);
        Skrzydlo(-5.8f, 14.6f, "AUTOBUSY", false, 4.4f);
        Zajmij(BudX0 - .5f, BudZ1, BudX1 + .5f, HZ0 + 7.5f);
        Opis("budynek: x " + BudX0 + "…" + BudX1 + ", z " + BudZ0 + "…" + BudZ1 + ", hala " + (HX1 - HX0).ToString("F1") + " × " + (HZ0 - HZ1).ToString("F2") + " m, drzwi " + dw + " × " + dh + " m (automatyczne)");
    }

    /// <summary>Wysokość spodu dachu hali nad danym z (dach od z −140 / y +9.45 do z −117 / y +12.25).</summary>
    static float SpodDachu(float z) => Y(9.45f) + 2.8f * Mathf.Clamp01((z - BudZ1) / 23f);

    static void Rama(float x0, float x1, float y0, float y1, float z0, float z1, float t = .08f)
    {
        B(czarnyAlu, x0, y0, z0, x0 + t, y1, z1); B(czarnyAlu, x1 - t, y0, z0, x1, y1, z1);
        B(czarnyAlu, x0 + t, y1 - t, z0, x1 - t, y1, z1);
    }

    // dach jednospadowy: płyta nachylona (od z −140 na 9.6 m do z −117 na 12.4 m), podsufitka z lameli, belki z LED, czoło z napisem
    static void Dach()
    {
        Grupa("budynek: dach", 400f);
        Vector3 tyl = V(-18f, Y(9.45f), BudZ1), przod = V(-18f, Y(12.25f), -117f);
        var u = (przod - tyl).normalized; var nn = Vector3.Cross(u, Vector3.right).normalized; if (nn.y < 0) nn = -nn;
        float L = (przod - tyl).magnitude; var c = (tyl + przod) * .5f;
        S(granat).OBox(c + nn * .35f, Vector3.right, nn, u, 25.6f, .7f, L + .3f);
        S(dach).OBox(c + nn * .72f, Vector3.right, nn, u, 25.7f, .04f, L + .35f, false, true);
        // lamele podsufitki — bez lameli w miejscu belek (belka nie przechodzi przez lamelę)
        for (float x = -30.6f; x <= -5.4f; x += .2f)
        {
            bool belka = false; for (float bx = -28.6f; bx <= -7.39f; bx += 2.65f) if (Mathf.Abs(x - bx) < .21f) belka = true;
            if (!belka) S(drewno).OBox(V(x, c.y, c.z) - nn * .05f, Vector3.right, nn, u, .085f, .06f, L - .2f);
        }
        for (float x = -28.6f; x <= -7.39f; x += 2.65f)
        {
            S(granat).OBox(V(x, c.y, c.z) - nn * .55f, Vector3.right, nn, u, .32f, .95f, L - 1f);
            S(led).OBox(V(x + .2f, c.y, c.z) - nn * .98f, Vector3.right, nn, u, .06f, .02f, L - 2f);
        }
        // czoło: blacha granatowa, nakrywa stalowa, napis DWORZEC (litery 3D, lico podświetlane) i KOLEJOWO · AUTOBUSOWY
        B(granat, -30.9f, Y(11.0f), -117.45f, -5.1f, Y(13.3f), -117.0f);
        B(stal, -31.0f, Y(13.3f), -117.5f, -5.0f, Y(13.4f), -116.95f);
        Grupa("napisy", 600f);
        BraxLiteryBebas.Napis("DWORZEC", V(-18f, Y(11.85f), -117.0f), Vector3.forward, 1.05f, .12f, litery, grafit, .09f);
        BraxLiteryBebas.Napis("KOLEJOWO · AUTOBUSOWY", V(-18f, Y(11.22f), -117.0f), Vector3.forward, .3f, .05f, litery, grafit, .12f);
    }

    // wieża zegarowa (zachodni narożnik hali): kamień, lamele teak, głowica z blachy granatowej, zegary na 3 ścianach
    static void Wieza()
    {
        Grupa("budynek: wieża", 600f);
        float x0 = -34.6f, x1 = -30.2f, z0 = HZ0 + 1.6f, z1 = HZ0 - 3.2f;
        B(kamien, x0, Y0 - .3f, z1, x1, Y(17.4f), z0);
        B(granat, x0 - .3f, Y(17.4f), z1 - .3f, x1 + .3f, Y(21.4f), z0 + .3f);
        B(stal, x0 - .5f, Y(21.4f), z1 - .5f, x1 + .5f, Y(21.55f), z0 + .5f);
        B(grafit, x0 + .2f, Y(21.55f), z1 + .2f, x1 - .2f, Y(22.0f), z0 - .2f);
        for (float x = x0 + .4f; x <= x1 - .35f; x += .46f) B(drewno, x - .07f, Y(3.0f), z0, x + .07f, Y(16.4f), z0 + .18f);
        B(czarnyAlu, x0, Y(2.7f), z0, x1, Y(3.0f), z0 + .3f);
        Kol("wieża zegarowa", V(x0, Y0, z1), V(x1, Y(17.4f), z0));
        float yz = Y(19.4f);
        Zegar(V((x0 + x1) * .5f, yz, z0 + .3f), Vector3.forward, 1.4f);
        Zegar(V(x0 - .3f, yz, (z0 + z1) * .5f), Vector3.left, 1.4f);
        Zegar(V(x1 + .3f, yz, (z0 + z1) * .5f), Vector3.right, 1.4f);
    }

    /// <summary>Zegar: tarcza mleczna (podświetlana), obręcz stalowa, indeksy, wskazówki ruchome (BraxZegarDworca).</summary>
    internal static void Zegar(Vector3 c, Vector3 n, float r)
    {
        var up = Vector3.up; var rt = Vector3.Cross(up, n).normalized;
        // obręcz, tarcza 3 cm (lico 3 cm przed obręczą), indeksy 3 cm na tarczy, wskazówki od 4 cm przed tarczą (co 2.5 cm)
        S(stal).Rura(c - n * .02f, c + n * .12f, r + .1f, 48);
        S(tarcza).Rura(c + n * .12f, c + n * .15f, r, 48, true);
        for (int i = 0; i < 60; i++)
        {
            float a = i * Mathf.PI / 30f; bool duzy = i % 5 == 0;
            var d = rt * Mathf.Sin(a) + up * Mathf.Cos(a);
            S(czarnyAlu).OBox(c + n * .165f + d * r * (duzy ? .8f : .87f), Vector3.Cross(d, n).normalized, d, n, duzy ? r * .06f : r * .02f, duzy ? r * .2f : r * .07f, .03f);
        }
        var piv = new GameObject("Zegar dworca"); piv.transform.SetParent(root, false); piv.transform.position = c + n * .19f;
        piv.transform.rotation = Quaternion.LookRotation(n, Vector3.up);
        Transform Wsk(string nazwa, float w, float len, float ogon, float z, Material m)
        {
            var s = new UlSiatka(); s.Box(new Vector3(-w * .5f, -ogon, z), new Vector3(w * .5f, len, z + .02f));
            var g = BraxMiastoA.Obiekt(piv.transform, "Zegar: " + nazwa, s, m, false); return g.transform;
        }
        var zg = piv.AddComponent<BraxZegarDworca>();
        zg.godzinowa = Wsk("wskazówka godzinowa", r * .07f, r * .55f, r * .12f, 0f, czarnyAlu);
        zg.minutowa = Wsk("wskazówka minutowa", r * .045f, r * .8f, r * .14f, .025f, czarnyAlu);
        zg.sekundnik = Wsk("sekundnik", r * .018f, r * .82f, r * .2f, .05f, czerwony);
    }

    // skrzydło: cokół, pilastry, przeszklenia (czarne okucia), lamele, attyka z napisem i piktogramem, wejście z daszkiem,
    // wnętrze: poczekalnia (ławki obite skórą, rośliny), wyjście na peron 1 (KOLEJ)
    static void Skrzydlo(float L, float R, string nazwa, bool kolej, float xDrzwi)
    {
        Grupa("budynek: skrzydło " + nazwa);
        const float h = 7.8f; float zf = HZ0 - .8f, zt = BudZ1 + .4f;
        B(granat, L - .12f, Y(h - 1.6f), zf + .15f, R + .12f, Y(h + .25f), zt - .15f);
        B(stal, L - .15f, Y(h + .25f), zf + .2f, R + .15f, Y(h + .35f), zt - .2f);
        // pilastry tylko na froncie (z −125…−124.55, od posadzki), przęsła co ≥ 3.6 m; drzwi w osi przęsła, które je zawiera
        int n = Mathf.Max(1, Mathf.FloorToInt((R - L) / 3.6f)); float s = (R - L) / n;
        int iD = Mathf.Clamp(Mathf.FloorToInt((xDrzwi - L) / s), 0, n - 1);
        float dA = L + iD * s + .32f, dB = L + (iD + 1) * s - .32f; xDrzwi = (dA + dB) * .5f;
        float dWid = dB - dA - .16f;                                    // światło przęsła minus ościeżnica
        // cokół (pas pod przeszkleniem, przerwany w drzwiach) i próg drzwi
        B(kamien, L, Y0 - .3f, zf - .2f, dA, Y(.85f), zf); B(kamien, dB, Y0 - .3f, zf - .2f, R, Y(.85f), zf);
        for (int i = 0; i <= n; i++) { float x = L + i * s; B(kamien, Mathf.Max(L, x - .32f), Y0, zf - .2f, Mathf.Min(R, x + .32f), Y(h - 1.6f), zf + .25f); }
        for (int i = 0; i < n; i++)
        {
            float a = L + i * s + .32f, b = L + (i + 1) * s - .32f, m = (a + b) * .5f;
            bool drzwi = i == iD;
            // szkło od wierzchu parapetu (nad drzwiami od nadproża), w polach dzielonych słupkiem — dwie tafle obok słupka
            float yd = drzwi ? Y(3.05f) : Y(.93f);
            // profile pionowe przy pilastrach (5 cm) — szkło między nimi, krawędź tafli nie leży na czole rygli
            B(czarnyAlu, a, yd, zf - .2f, a + .05f, Y(h - 1.75f), zf - .06f); B(czarnyAlu, b - .05f, yd, zf - .2f, b, Y(h - 1.75f), zf - .06f);
            if (drzwi) B(szklo, a + .05f, yd, zf - .14f, b - .05f, Y(h - 1.75f), zf - .12f);
            else { B(szklo, a + .05f, yd, zf - .14f, m - .035f, Y(h - 1.75f), zf - .12f); B(szklo, m + .035f, yd, zf - .14f, b - .05f, Y(h - 1.75f), zf - .12f); }
            B(czarnyAlu, a, Y(h - 1.75f), zf - .2f, b, Y(h - 1.6f), zf - .06f);
            if (!drzwi) B(czarnyAlu, a, Y(.85f), zf - .2f, b, Y(.93f), zf - .06f);
            B(czarnyAlu, a, Y(3.56f), zf - .2f, b, Y(3.64f), zf - .06f);
            if (!drzwi) B(czarnyAlu, m - .035f, Y(.93f), zf - .2f, m + .035f, Y(h - 1.75f), zf - .06f);
            if (i % 2 == 1 && !drzwi) for (float x = a + .15f; x < b - .1f; x += .3f) B(drewno, x - .045f, Y(.93f), zf + .12f, x + .045f, Y(h - 1.75f), zf + .3f);
            if (!drzwi) Kol("skrzydło " + nazwa + ": przeszklenie", V(a, Y0, zf - .2f), V(b, Y(h - 1.6f), zf - .06f));
        }
        // wejście: ościeżnica na całe przęsło, drzwi automatyczne po stronie poczekalni (otwarte chowają się za cokołem i pilastrami),
        // daszek z lamelami teak na dwóch cięgnach
        Rama(dA, dB, Y(0f), Y(3.05f), zf - .2f, zf - .06f);
        Plyta(posadzka, dA, zf - .2f, dB, zf, Y0, .3f);
        BraxMiastoA.DrzwiPrzesuwne(root, "skrzydło " + nazwa + " — wejście", V(xDrzwi, Y0, zf - .25f), Vector3.right, dWid, 2.92f, szklo, czarnyAlu);
        B(granat, xDrzwi - 2.2f, Y(3.3f), zf, xDrzwi + 2.2f, Y(3.55f), zf + 2.6f);
        for (float z = zf + .1f; z < zf + 2.55f; z += .18f) B(drewno, xDrzwi - 2.15f, Y(3.25f), z, xDrzwi + 2.15f, Y(3.29f), z + .07f);
        foreach (float sg in new[] { -1f, 1f }) S(stal).Rura(V(xDrzwi + sg * 2.0f, Y(3.55f), zf + 2.45f), V(xDrzwi + sg * 2.0f, Y(5.6f), zf + .25f), .018f, 8);
        // napis na attyce (litery 3D na licu attyki) i piktogram
        Grupa("napisy", 400f);
        float w = BraxLiteryBebas.Napis(nazwa, V((L + R) * .5f + .9f, Y(h - 1.25f), zf + .15f), Vector3.forward, .9f, .08f, litery, grafit, .09f);
        Piktogram(kolej, V((L + R) * .5f + .9f - w * .5f - .9f, Y(h - .78f), zf + .15f));
        // wnętrze: posadzka, sufit z oprawami, ściany, ławki, rośliny, tablica odjazdów
        Grupa("wnętrze: skrzydło " + nazwa, 90f);
        // posadzka poczekalni — w skrzydle AUTOBUSY z wycięciem na WC (tam posadzka WC w tym samym poziomie)
        PlytaBez(posadzka, L + .6f, zt + .3f, R - .6f, zf - .2f, Y0, .3f, kolej ? new Rect[0] : new[] { Rect.MinMaxRect(WcX0, WcZ0, WcX1 + .15f, WcZ1 + .15f) });
        B(tynk, L + .6f, Y(h - 1.75f), zt + .3f, R - .6f, Y(h - 1.6f), zf - .2f);
        // ściana tylna (kamień od strony peronu, z −140…−139.3) — w skrzydle KOLEJ z otworem na drzwi peronowe
        if (kolej)
        {
            B(kamien, L, Y0 - .3f, BudZ1, xDrzwi - 1.4f, Y(h - 1.6f), zt + .3f); B(kamien, xDrzwi + 1.4f, Y0 - .3f, BudZ1, R, Y(h - 1.6f), zt + .3f);
            B(kamien, xDrzwi - 1.4f, Y(3.05f), BudZ1, xDrzwi + 1.4f, Y(h - 1.6f), zt + .3f);
        }
        else B(kamien, L, Y0 - .3f, BudZ1, R, Y(h - 1.6f), zt + .3f);
        if (kolej) B(kamien, L, Y0 - .3f, zt, L + .6f, Y(h - 1.6f), zf - .2f);
        else
        {
            // ściana zachodnia skrzydła AUTOBUSY z przejściami do WC (przedłużenie przejść przez pylon hali)
            float zz = zt;
            foreach (var (a, b) in WcPrzejscia) { B(kamien, L, Y0 - .3f, zz, L + .6f, Y(h - 1.6f), a); B(kamien, L, Y(WcDrzwiH + .15f), a, L + .6f, Y(h - 1.6f), b); zz = b; }
            B(kamien, L, Y0 - .3f, zz, L + .6f, Y(h - 1.6f), zf - .2f);
        }
        B(kamien, R - .6f, Y0 - .3f, zt, R, Y(h - 1.6f), zf - .2f);
        for (float x = L + 2f; x < R - 1.5f; x += 3f) B(led, x - .6f, Y(h - 1.8f), !kolej && x < WcX1 + .6f ? WcZ1 + .5f : zt + 2.5f, x + .6f, Y(h - 1.76f), zf - 2.5f);
        if (kolej) { Kol("skrzydło " + nazwa + ": ściana tylna", V(L, Y0, BudZ1), V(xDrzwi - 1.4f, Y(h), zt + .3f)); Kol("skrzydło " + nazwa + ": ściana tylna", V(xDrzwi + 1.4f, Y0, BudZ1), V(R, Y(h), zt + .3f)); }
        else Kol("skrzydło " + nazwa + ": ściana tylna", V(L, Y0, BudZ1), V(R, Y(h), zt + .3f));
        if (kolej) Kol("skrzydło " + nazwa + ": ściana zach.", V(L, Y0, zt), V(L + .6f, Y(h), zf));
        else
        {
            float zz = zt;
            foreach (var (a, b) in WcPrzejscia) { Kol("skrzydło " + nazwa + ": ściana zach.", V(L, Y0, zz), V(L + .6f, Y(h), a)); zz = b; }
            Kol("skrzydło " + nazwa + ": ściana zach.", V(L, Y0, zz), V(L + .6f, Y(h), zf));
        }
        Kol("skrzydło " + nazwa + ": ściana wsch.", V(R - .6f, Y0, zt), V(R, Y(h), zf));
        if (kolej)
        {
            // wyjście z poczekalni na peron 1 (drzwi automatyczne w ścianie tylnej)
            Grupa("budynek: skrzydło " + nazwa);
            BraxMiastoA.DrzwiPrzesuwne(root, "poczekalnia KOLEJ — wyjście na peron 1", V(xDrzwi, Y0, (BudZ1 + zt + .3f) * .5f), Vector3.right, 2.6f, 2.92f, szklo, czarnyAlu);
            Plyta(posadzka, xDrzwi - 1.4f, BudZ1, xDrzwi + 1.4f, zt + .3f, Y0, .3f);   // próg w grubości muru
            Rama(xDrzwi - 1.4f, xDrzwi + 1.4f, Y(0f), Y(3.05f), BudZ1 - .02f, zt + .32f);
            Grupa("wnętrze: skrzydło " + nazwa, 90f);
        }
        foreach (float zr in new[] { -130f, -133.5f, -137f })
            foreach (float xb in new[] { xDrzwi - 7.3f, xDrzwi + 2.2f })
                if (xb > L + .8f && xb + 2.7f < R - .8f && (kolej || !(xb < WcX1 + .8f && zr < WcZ1 + .8f))) Lawka(xb, zr, 4, Vector3.forward, "poczekalnia " + nazwa);
        if (!kolej) WC();
        Roslina(V(L + 1.2f, Y0, zf - 1.2f)); Roslina(V(R - 1.2f, Y0, zf - 1.2f));
        TablicaOdjazdow(nazwa == "KOLEJ" ? "ODJAZDY · POCIĄGI" : "ODJAZDY · AUTOBUSY", V(xDrzwi + (kolej ? 5.2f : 0f), Y(3.4f), zt + .3f), Vector3.forward, 4.2f, 1.8f, kolej);
        if (!kolej)
            foreach (float xb in new[] { R - 2.2f, R - 3.4f }) Biletomat(V(xb, Y0, zt + 1.0f), Vector3.forward);
    }

    // piktogram pociągu / autobusu: płyta niebieska z zaokrąglonymi narożami, sylwetka z brył w bieli
    static void Piktogram(bool pociag, Vector3 c)
    {
        // płyta 8 cm, sylwetka biała 4 cm na płycie, okna niebieskie 3 cm na sylwetce (lica co ≥ 3 cm — bez migotania)
        S(niebieski).Poducha(c + V(-.55f, -.55f, 0f), c + V(.55f, .55f, .08f), .12f, 0f, 6);
        var b = c + V(0f, 0f, .08f);
        if (pociag)
        {
            S(litery).Poducha(b + V(-.3f, -.22f, 0f), b + V(.3f, .4f, .04f), .12f, 0f, 5);
            S(niebieski).Poducha(b + V(-.21f, .17f, .04f), b + V(.21f, .33f, .07f), .04f, 0f, 4);
            foreach (float sg in new[] { -1f, 1f }) S(litery).Box(b + V(sg * .16f - .03f, -.42f, 0f), b + V(sg * .16f + .03f, -.24f, .04f));
        }
        else
        {
            S(litery).Poducha(b + V(-.37f, -.17f, 0f), b + V(.37f, .33f, .04f), .08f, 0f, 5);
            S(niebieski).Poducha(b + V(-.3f, .1f, .04f), b + V(.3f, .26f, .07f), .03f, 0f, 4);
            foreach (float sg in new[] { -1f, 1f }) S(litery).Rura(b + V(sg * .22f, -.23f, 0f), b + V(sg * .22f, -.23f, .04f), .08f, 14);
        }
    }

    // ============================================================================ WNĘTRZE HALI
    static void Wnetrze()
    {
        Grupa("wnętrze: hala", 90f);
        Plyta(posadzka, HX0, HZ1, HX1, HZ0, Y0, .3f);
        for (float x = -24f; x < -10f; x += 4.4f) B(led, x - .7f, Y(10.05f), HZ1 + 2f, x + .7f, Y(10.1f), HZ0 - 2f);
        // KASY (zachód): ściana teak, 3 stanowiska z przegrodami, lada (dąb, blat marmur), szyba w czarnej ramie, numer, obsługa
        B(drewno, HX0, Y0, -137.5f, HX0 + .4f, Y(4.2f), -126.2f);
        for (int i = 0; i < 3; i++)
        {
            float z = -128.6f - i * 2.9f;
            B(dab, -26.05f, Y0, z - 1.3f, -25.45f, Y(1.31f), z + 1.3f);
            S(marmurBlat).Poducha(V(-26.15f, Y(1.31f), z - 1.36f), V(-25.3f, Y(1.37f), z + 1.36f), .02f, 0f, 4);
            B(szklo, -25.79f, Y(1.37f), z - 1.2f, -25.77f, Y(2.55f), z + 1.2f);
            B(czarnyAlu, -25.82f, Y(2.55f), z - 1.3f, -25.74f, Y(2.63f), z + 1.3f);
            foreach (float sg in new[] { -1f, 1f }) B(czarnyAlu, -25.82f, Y(1.37f), z + sg * 1.25f - .04f, -25.74f, Y(2.55f), z + sg * 1.25f + .04f);
            B(drewno, HX0 + .4f, Y0, z - 1.45f - .06f, -25.45f, Y(2.63f), z - 1.45f + .06f);
            Grupa("napisy", 120f);
            S(niebieski).Poducha(V(-25.7f, Y(2.72f), z - .32f), V(-25.6f, Y(3.32f), z + .32f), .04f, 0f, 4);
            BraxLiteryBebas.Napis((i + 1).ToString(), V(-25.6f, Y(2.83f), z), Vector3.right, .36f, .02f, litery, grafit, .05f, .002f);
            Grupa("wnętrze: hala", 90f);
            Kol("kasa " + (i + 1) + ": lada", V(-26.15f, Y0, z - 1.36f), V(-25.3f, Y(2.6f), z + 1.36f));
            Obsluga.Add((V(-27.3f, Y0, z), 90f, "kasa " + (i + 1)));
            BraxMiastoA.Siedzisko(root, "kasa " + (i + 1) + " — krzesło kasjera", V(-27.5f, Y(.58f), z), Vector3.right);
        }
        Kol("kasy: zaplecze", V(HX0, Y0, -137.5f), V(HX0 + .4f, Y(4.2f), -126.2f));
        Grupa("napisy", 160f);
        B(niebieski, -25.55f, Y(3.45f), -137.0f, -25.45f, Y(4.15f), -126.4f);
        BraxLiteryBebas.Napis("KASY BILETOWE · KOLEJ I AUTOBUSY", V(-25.45f, Y(3.6f), -131.7f), Vector3.right, .38f, .03f, litery, grafit, .07f, .003f);
        Grupa("wnętrze: hala", 90f);
        // słupki kolejki z taśmą
        for (int k = 0; k < 4; k++) { float z = -127.4f - k * 2.9f; S(stal).Walec(V(-23.2f, Y0, z), .04f, 1.2f, 12); S(stal).Walec(V(-23.2f, Y0, z), .17f, .03f, 20); }
        B(granat, -23.22f, Y(1.02f), -136.1f, -23.18f, Y(1.1f), -127.4f);
        // INFORMACJA: wyspa zaokrąglona (okleina teak, blat marmur), za nią totem niebieski z białym „i” i napisem
        float ix = -18f, iz = -131.6f;
        S(drewno).Poducha(V(ix - 1.8f, Y0, iz - .7f), V(ix + 1.8f, Y(1.3f), iz + .7f), .3f, .01f, 8);
        S(marmurBlat).Poducha(V(ix - 1.9f, Y(1.3f), iz - .8f), V(ix + 1.9f, Y(1.37f), iz + .8f), .3f, 0f, 8);
        S(czarnyAlu).Box(V(ix + .3f, Y(1.37f), iz - .25f), V(ix + .9f, Y(1.8f), iz - .2f));
        S(ekran).Box(V(ix + .33f, Y(1.42f), iz - .275f), V(ix + .87f, Y(1.76f), iz - .25f));   // ekran od strony obsługi, 2.5 cm przed obudową
        Kol("informacja: lada", V(ix - 1.9f, Y0, iz - .8f), V(ix + 1.9f, Y(1.37f), iz + .8f));
        Obsluga.Add((V(ix, Y0, iz - 1.45f), 0f, "informacja"));
        BraxMiastoA.Siedzisko(root, "informacja — krzesło", V(ix - .9f, Y(.58f), iz - 1.5f), Vector3.forward);
        Grupa("napisy", 200f);
        S(niebieski).Poducha(V(ix - .7f, Y0, iz - 2.45f), V(ix + .7f, Y(3.4f), iz - 2.15f), .1f, 0f, 6);
        S(litery).Rura(V(ix, Y(2.75f), iz - 2.15f), V(ix, Y(2.75f), iz - 2.12f), .36f, 40);
        BraxLiteryBebas.Napis("I", V(ix, Y(2.54f), iz - 2.12f), Vector3.forward, .42f, .02f, niebieski, niebieski, .05f, .001f);
        BraxLiteryBebas.Napis("INFORMACJA", V(ix, Y(1.95f), iz - 2.15f), Vector3.forward, .22f, .02f, litery, grafit, .06f, .002f);
        Kol("informacja: totem", V(ix - .7f, Y0, iz - 2.45f), V(ix + .7f, Y(3.4f), iz - 2.15f));
        Grupa("wnętrze: hala", 90f);
        // KAWIARNIA (wschód): lada zaokrąglona z okleiny, blat marmur, ekspres, hokery Poly Haven, menu niebieskie na ścianie
        float kx = -10.4f;
        S(drewno).Poducha(V(kx - .65f, Y0, -132.0f), V(kx + .65f, Y(1.3f), -126.4f), .14f, .01f, 8);
        S(marmurBlat).Poducha(V(kx - .8f, Y(1.3f), -132.15f), V(kx + .8f, Y(1.37f), -126.25f), .06f, 0f, 6);
        B(dab, HX1 - .55f, Y0, -132f, HX1, Y(1.2f), -126.4f);
        S(marmurBlat).Box(V(HX1 - .6f, Y(1.2f), -132f), V(HX1, Y(1.25f), -126.4f));
        S(stal).Poducha(V(HX1 - .5f, Y(1.25f), -129.6f), V(HX1 - .05f, Y(1.85f), -128.6f), .05f, 0f, 4);
        Kol("kawiarnia: lada", V(kx - .8f, Y0, -132.15f), V(kx + .8f, Y(1.37f), -126.25f));
        Obsluga.Add((V(-8.6f, Y0, -129.2f), 270f, "kawiarnia — barista"));
        var hoker = BraxRegion.Model("bar_chair_round_01");
        for (int k = 0; k < 4; k++)
        {
            var p = V(kx - 1.35f, Y0, -127.3f - k * 1.3f);
            if (hoker) BraxRegion.Put(root, hoker, "Dworzec: hoker kawiarni", p, 90f, 1f, false);
            BraxMiastoA.Siedzisko(root, "kawiarnia — hoker " + (k + 1), p + Vector3.up * .98f, Vector3.right);
        }
        Grupa("napisy", 160f);
        B(niebieski, HX1 - .1f, Y(2.55f), -131.6f, HX1, Y(3.35f), -126.8f);
        BraxLiteryBebas.Napis("KAWA · HERBATA · PIEKARNIA", V(HX1 - .1f, Y(2.8f), -129.2f), Vector3.left, .3f, .025f, litery, grafit, .06f, .002f);
        // WC: tablica niebieska z białym „WC” (właściciel 16:13); drzwi na zawiasach i łazienki — BraxMiastoDworzec.WC
        Grupa("napisy", 160f);
        S(niebieski).Poducha(V(HX1 - .1f, Y(2.75f), -136.9f), V(HX1, Y(3.35f), -135.4f), .05f, 0f, 4);
        BraxLiteryBebas.Napis("WC", V(HX1 - .1f, Y(2.87f), -136.15f), Vector3.left, .36f, .025f, litery, grafit, .06f, .002f);
        // tablica nad drzwiami na perony i tablica odjazdów
        B(niebieski, -22.4f, Y(3.5f), HZ1, -13.6f, Y(4.25f), HZ1 + .2f);   // tył na murze
        BraxLiteryBebas.Napis("PERONY 1 · 2", V(-18f, Y(3.66f), HZ1 + .2f), Vector3.forward, .42f, .03f, litery, grafit, .08f, .002f);
        TablicaOdjazdow("ODJAZDY · DEPARTURES", V(-10.3f, Y(4.8f), HZ1), Vector3.forward, 4.8f, 2.2f, true);
        Grupa("wnętrze: hala", 90f);
        foreach (var (x, z) in new[] { (HX0 + 1.2f, HZ0 - 1.2f), (HX0 + 1.0f, HZ1 + 1.0f) }) Roslina(V(x, Y0, z));   // bez rośliny przy WC (zasłaniała tabliczkę PANOWIE)
        Kosz(V(-21.2f, Y0, HZ0 - 1.5f), 0f); Kosz(V(-14.8f, Y0, HZ0 - 1.5f), 0f);
        foreach (var p in new[] { V(-20f, Y(9.5f), -128f), V(-14f, Y(9.5f), -135f) }) BraxRegion.Warm(root, "Dworzec: światło hali", p, 1.2f, 16f);
        Opis("wnętrze hali: kasy 3 (obsługa), informacja, kawiarnia (4 hokery), WC, tablica odjazdów; obsady " + Obsluga.Count);
    }

    // ============================================================================ WIATY
    static void Wiaty()
    {
        // peron 1: wiata od ściany budynku (z −140 do −145), słupy co 10 m, podsufitka teak, LED, tablice PERON 1 na cięgnach
        Grupa("wiaty", 260f);
        float y = Y(4.3f), z0 = P1Tyl, z1 = -145.0f;
        var slupy1 = new List<float>(); for (float x = -45f; x <= 55f; x += 10f) if (!(x > KladkaX0 - 1f && x < KladkaX1 + 1f)) slupy1.Add(x);
        foreach (var (xa, xb) in new[] { (-52f, WindaA.x - 1.4f), (WindaA.x + 1.4f, 60f) })   // przerwa na szyb windy przy kładce
        {
            B(granat, xa, y, z1, xb, y + .4f, z0);
            S(dach).Box(V(xa - .05f, y + .4f, z1 - .05f), V(xb + .05f, y + .44f, z0), 4);
            Lamele(xa, xb, z0, z1, y, z1 + .4f, z1 + .66f, slupy1, .13f);
        }
        foreach (float x in slupy1)
        {
            S(granat).Poducha(V(x - .13f, Y0, z1 + .4f), V(x + .13f, y, z1 + .66f), .06f, 0f, 4);
            Kol("wiata peronu 1: słup", V(x - .13f, Y0, z1 + .4f), V(x + .13f, y, z1 + .66f));
        }
        B(led, -51f, y - .08f, z1 + .25f, WindaA.x - 1.6f, y - .05f, z1 + .3f); B(led, WindaA.x + 1.6f, y - .08f, z1 + .25f, 59f, y - .05f, z1 + .3f);
        Grupa("napisy", 160f);
        foreach (float x in new[] { -38f, -8f, 32f, 52f }) TablicaPeronu("PERON 1", x, -144.3f, y);
        foreach (float x in new[] { -24f, 40f }) TablicaNaCiegnach(x, -143.2f, y, "PERON 1 · ODJAZDY");
        // peron 2: wiata motylkowa na słupach V (oś z −163.9, szer. 4.4 m — słupy sieci poza nią)
        Grupa("wiaty", 260f);
        float zc = -163.9f, y2 = Y(4.0f);
        foreach (float sg in new[] { -1f, 1f })
        {
            var nn = new Vector3(0f, 1f, -sg * .12f).normalized; var u = new Vector3(0f, sg * .12f, 1f).normalized * sg;
            S(granat).OBox(V(-8f, y2 + .25f, zc + sg * 1.12f), Vector3.right, nn, u, 52f, .26f, 2.3f);
            for (float k = .12f; k < 2.2f; k += .2f) S(drewno).OBox(V(-8f, y2 + .1f + k * .12f, zc + sg * k), Vector3.right, nn, u, 51.8f, .05f, .085f);
        }
        B(granat, -34f, y2 - .2f, zc - .2f, 18f, y2 + .15f, zc + .2f);
        B(led, -33f, y2 - .24f, zc - .04f, 17f, y2 - .2f, zc + .04f);
        for (float x = -29f; x <= 13f; x += 10.5f)
            foreach (float sg in new[] { -1f, 1f })
            {
                S(granat).Rura(V(x + sg * .1f, Y0, zc), V(x + sg * .55f, y2 - .2f, zc), .11f, 14);
                Kol("wiata peronu 2: słup", V(x - .2f, Y0, zc - .2f), V(x + .2f, y2, zc + .2f));
            }
        Grupa("napisy", 160f);
        foreach (float x in new[] { -18f, 8f }) TablicaPeronu("PERON 2", x, zc, y2 - .2f);
        // wiata BUS (przy stanowiskach): dach, podsufitka teak, szyby w czarnych ramach (tafle ≤ 1.5 m), numery stanowisk
        Grupa("wiaty", 220f);
        float bx0 = BusX0 + 2f, bx1 = BusX1 - 2f, bz0 = DrogaZ - Pas - ZatokaSz - .5f, bz1 = bz0 - 3.9f, by = Y(3.6f);
        B(granat, bx0, by, bz1, bx1, by + .35f, bz0);
        var slupyB = new List<float>(); for (float x = bx0 + .8f; x <= bx1 - .7f; x += 9f) slupyB.Add(x);
        Lamele(bx0, bx1, bz0, bz1, by, bz1 + .2f, bz1 + .42f, slupyB, .11f);
        B(led, bx0 + .5f, by - .08f, (bz0 + bz1) * .5f - .03f, bx1 - .5f, by - .05f, (bz0 + bz1) * .5f + .03f);
        for (float x = bx0 + .8f; x <= bx1 - .7f; x += 9f)
        {
            S(granat).Poducha(V(x - .11f, Y0, bz1 + .2f), V(x + .11f, by, bz1 + .42f), .05f, 0f, 4);
            Kol("wiata BUS: słup", V(x - .11f, Y0, bz1 + .2f), V(x + .11f, by, bz1 + .42f));
        }
        for (float x = bx0 + 1f; x < bx1 - 1f; x += 1.45f)
        {
            float x1 = Mathf.Min(x + 1.45f, bx1 - 1f);
            B(szklo, x + .03f, Y(.15f), bz1 + .02f, x1 - .03f, Y(2.6f), bz1 + .04f);
            B(czarnyAlu, x - .025f, Y(.1f), bz1, x + .025f, Y(2.65f), bz1 + .06f);
        }
        B(czarnyAlu, bx0 + 1f, Y(2.6f), bz1, bx1 - 1f, Y(2.68f), bz1 + .06f);
        Kol("wiata BUS: ściana szklana", V(bx0 + 1f, Y0, bz1), V(bx1 - 1f, Y(2.68f), bz1 + .06f));
        Grupa("napisy", 160f);
        for (int k = 0; k < 2; k++)
        {
            float xs = BusX0 + 4f + StanowiskoDl * (k + .5f);
            // numer stanowiska pod krawędzią dachu (cięgna wchodzą w dach, między lamelami)
            S(niebieski).Poducha(V(xs - .6f, by - 1.15f, bz0 - .25f), V(xs + .6f, by - .25f, bz0 - .15f), .06f, 0f, 4);
            BraxLiteryBebas.Napis((k + 1).ToString(), V(xs, by - 1.0f, bz0 - .15f), Vector3.forward, .6f, .03f, litery, grafit, .05f, .002f);
            foreach (float sg in new[] { -1f, 1f }) S(stal).Rura(V(xs + sg * .45f, by - .25f, bz0 - .2f), V(xs + sg * .45f, by + .05f, bz0 - .2f), .012f, 6);
            TablicaOdjazdow("STANOWISKO " + (k + 1), V(xs + 4.2f, Y(1.6f), bz1 + .06f), Vector3.forward, 1.1f, 1.5f, false);
            Lawka(xs - 3.2f, bz1 + 1.0f, 3, Vector3.forward, "wiata BUS, stanowisko " + (k + 1));
        }
        Zajmij(bx0, bz1 - .5f, bx1, bz0);
        Opis("wiaty: peron 1 (x −52…60), peron 2 (x −30…50), BUS (x " + bx0 + "…" + bx1 + ", 2 stanowiska z numerami)");
    }

    /// <summary>Lamele podsufitki wzdłuż x (co 0.2 m w z) — lamela przecinana przez słupy (x ± pol) ma przerwę na słup.</summary>
    static void Lamele(float xa, float xb, float zA, float zB, float y, float zs0, float zs1, List<float> slupy, float pol)
    {
        for (float z = zA - .1f; z > zB + .05f; z -= .2f)
        {
            float xp = xa + .1f;
            if (z + .043f > zs0 && z - .043f < zs1)
                foreach (float sx in slupy) { if (sx - pol <= xp || sx + pol >= xb - .1f) continue; B(drewno, xp, y - .05f, z - .043f, sx - pol, y, z + .043f, 8); xp = sx + pol; }
            B(drewno, xp, y - .05f, z - .043f, xb - .1f, y, z + .043f, 8);   // bez wierzchu (przy dachu) — spód widoczny z peronu
        }
    }

    static void TablicaPeronu(string tekst, float x, float z, float yBelki)
    {
        float w = BraxLiteryBebas.Szerokosc(tekst, .34f, .07f) + .7f;
        S(niebieski).Poducha(V(x - w * .5f, yBelki - 1.25f, z - .05f), V(x + w * .5f, yBelki - .65f, z + .05f), .05f, 0f, 4);
        BraxLiteryBebas.Napis(tekst, V(x, yBelki - 1.12f, z + .05f), Vector3.forward, .34f, .02f, litery, grafit, .07f, .002f);
        BraxLiteryBebas.Napis(tekst, V(x, yBelki - 1.12f, z - .05f), Vector3.back, .34f, .02f, litery, grafit, .07f, .002f);
        foreach (float sg in new[] { -1f, 1f }) S(stal).Rura(V(x + sg * (w * .5f - .25f), yBelki - .65f, z), V(x + sg * (w * .5f - .25f), yBelki + .05f, z), .012f, 6);   // cięgno wchodzi w belkę
    }

    static void TablicaNaCiegnach(float x, float z, float yBelki, string tytul)
    {
        S(czarnyAlu).Box(V(x - 1.2f, yBelki - 1.5f, z - .07f), V(x + 1.2f, yBelki - .45f, z + .07f));
        TablicaOdjazdow(tytul, V(x, yBelki - .975f, z + .07f), Vector3.forward, 2.2f, .95f, true);
        TablicaOdjazdow(tytul, V(x, yBelki - .975f, z - .07f), Vector3.back, 2.2f, .95f, true);
        foreach (float sg in new[] { -1f, 1f }) S(stal).Rura(V(x + sg * .9f, yBelki - .45f, z), V(x + sg * .9f, yBelki + .05f, z), .012f, 6);
    }

    // ============================================================================ KŁADKA (plac → nad peronem 1 i torami → peron 2)
    internal static readonly Vector2 WindaA = new Vector2(27.5f, -143.3f), WindaB = new Vector2(20.5f, -164.6f);   // środki szybów (x, z)

    static void Kladka()
    {
        Grupa("kładka", 300f);
        float x0 = KladkaX0, x1 = KladkaX1, zN = -141f, zS = P2Tyl, y = KladkaY, ys = KladkaSpod;
        // pomost: płyta, dźwigary boczne pełne (parapet 1.42 m), szkło nad parapetem w czarnych ramach, dach
        Plyta(beton, x0, zS, x1, zN, y, y - ys);
        foreach (float sg in new[] { -1f, 1f })
        {
            float xa = sg < 0 ? x0 - .3f : x1, xb = sg < 0 ? x0 : x1 + .3f;
            B(grafit, xa, ys - .25f, zS, xb, y, zN);   // dźwigar pod pomostem — ciągły
            // parapet z szybą nad pomostem — przerwy: wschód — schody na peron 2 i winda od peronu 1, zachód — winda od peronu 2
            // (przy windach z miejscem na słupek przywołania na dźwigarze)
            var otwory = sg > 0 ? new[] { (-166.2f, -162.6f), (WindaA.y - 1.3f, zN) } : new[] { (WindaB.y - 1.3f, WindaB.y + 2.0f) };
            float zz = zS;
            foreach (var (o0, o1) in otwory.OrderBy(q => q.Item1)) { Parapet(xa, xb, zz, o0); zz = o1; }
            Parapet(xa, xb, zz, zN);
        }
        void Parapet(float xa, float xb, float za, float zb)
        {
            if (zb - za < .05f) return;
            B(grafit, xa, y, za, xb, y + 1.42f, zb);
            for (float z = za; z < zb - .05f; z += 1.45f)
            {
                float z1 = Mathf.Min(z + 1.45f, zb);
                B(szklo, xa + .13f, y + 1.42f, z + .03f, xb - .13f, y + 3.0f, z1 - .03f);
                B(czarnyAlu, xa + .1f, y + 1.42f, z - .025f, xb - .1f, y + 3.05f, z + .025f);
            }
            B(czarnyAlu, xa + .1f, y + 1.42f, zb - .025f, xb - .1f, y + 3.05f, zb);
            Kol("kładka: parapet", V(xa, y, za), V(xb, y + 3.0f, zb));
        }
        B(granat, x0 - .41f, y + 3.05f, zS - .3f, x1 + .41f, y + 3.4f, zN + .3f);   // okap kończy się przed licem szybów wind
        for (float x = x0 + .1f; x < x1 - .05f; x += .2f) B(drewno, x - .043f, y + 3.0f, zS, x + .043f, y + 3.05f, zN, 8);
        B(led, x0 + 1.9f, y + 2.97f, zS + .5f, x1 - 1.9f, y + 3.0f, zN - .5f);   // oprawa 3 cm pod lamelami
        foreach (float z in new[] { -146.0f, -161.0f })   // filary przy krawędziach peronów — przed drzwiami wind wolne
            foreach (float x in new[] { x0 + .3f, x1 - .3f })
            {
                S(grafit).Poducha(V(x - .2f, Y0, z - .2f), V(x + .2f, ys, z + .2f), .05f, 0f, 4);
                Kol("kładka: filar", V(x - .2f, Y0, z - .2f), V(x + .2f, ys, z + .2f));
            }
        Kol("kładka: pomost", V(x0, ys, zS), V(x1, y, zN));
        // windy (właściciel 18:12: „Winda przy kładce”): od peronu 1 / placu (drzwi na zachód, wprost na pomost) i od peronu 2
        // (drzwi na wschód); progi między dźwigarem pomostu a drzwiami szybu
        Plyta(beton, x1 + .3f, WindaA.y - .75f, WindaA.x - SzybPolD, WindaA.y + .75f, y, .3f);   // próg do lica szybu (nie na ryglu)
        Plyta(beton, WindaB.x + SzybPolD, WindaB.y - .75f, x0 - .3f, WindaB.y + .75f, y, .3f);
        Kol("kładka: próg windy A", V(x1, y - .3f, WindaA.y - .75f), V(WindaA.x - SzybPolD, y, WindaA.y + .75f));
        Kol("kładka: próg windy B", V(WindaB.x + SzybPolD, y - .3f, WindaB.y - .75f), V(x0, y, WindaB.y + .75f));
        Winda("KŁADKA · PERON 1", V(WindaA.x, Y0, WindaA.y), Vector3.left, new[] { Y0, y }, new[] { "0 PERON 1 · PLAC", "1 KŁADKA" });
        Winda("KŁADKA · PERON 2", V(WindaB.x, Y0, WindaB.y), Vector3.right, new[] { Y0, y }, new[] { "0 PERON 2", "1 KŁADKA" }, -1f);
        // schody na plac (na północ): 2 biegi po 19 stopni, spocznik 2 m
        Schody(V((x0 + x1) * .5f, y, zN), Vector3.forward, 3.4f, "kładka — schody na plac");
        // schody na peron 2 (na wschód, przy tylnej krawędzi peronu)
        Schody(V(x1 + .3f, y, (-166.2f + -162.6f) * .5f), Vector3.right, 3.4f, "kładka — schody na peron 2");   // od lica dźwigara
        Zajmij(x0 - 1f, zS, x1 + 1f, -124f);
        Opis("kładka: x " + x0 + "…" + x1 + ", pomost y " + y + " (spód " + ys + " nad przewodem jezdnym " + (GlowkaY + SiecNadGlowka).ToString("F2") + "), schody 2 × 19 stopni 0.205 / 0.388 m");
    }

    /// <summary>Schody z pomostu (góra = start, y startu) w dół w kierunku kier: 2 biegi po 19 stopni (0.205 × 0.388), spocznik 2 m,
    /// policzki z betonu, balustrady szklane w czarnych słupkach (tafle 1.3 m), pochwyt; kolizja = pochylnie.</summary>
    static void Schody(Vector3 start, Vector3 kier, float szer, string nazwa)
    {
        var rt = Vector3.Cross(Vector3.up, kier).normalized;
        float h = (start.y - Y0) / 38f, t = .388f;
        Vector3 p = start;
        for (int bieg = 0; bieg < 2; bieg++)
        {
            Vector3 p0 = p;
            for (int i = 0; i < 19; i++)
            {
                var a = p + kier * (i * t); float yTop = start.y - (bieg * 19 + i + 1) * h;
                if (yTop < Y0 + .01f) continue;   // ostatni podest = posadzka placu / peronu (bez płyty stopnia na płycie)
                var c = a + kier * (t * .5f); c.y = yTop - .1f;
                S(granitC).OBox(c, rt, Vector3.up, kier, szer, .2f, t + .02f);
            }
            var koniec = p + kier * (19 * t); koniec.y = start.y - (bieg + 1) * 19 * h;
            // płyta biegu pod stopniami (spód skośny)
            var srodek = (p0 + koniec) * .5f; var u = (koniec - p0).normalized; var nn = Vector3.Cross(u, rt).normalized; if (nn.y < 0) nn = -nn;
            S(beton).OBox(srodek - nn * .28f, rt, nn, u, szer - .2f, .3f, (koniec - p0).magnitude);   // płyta biegu 10 cm węższa z boków — boki stopni nie leżą na jej boku
            BraxRegionMeble.Kolizja(root, "Dworzec: " + nazwa + " — bieg " + (bieg + 1), srodek - nn * .1f, new Vector3(szer, .2f, (koniec - p0).magnitude), Quaternion.LookRotation(u, nn));
            // balustrady: tafle szkła 1.3 m między czarnymi słupkami, pochwyt na 1.42 m
            foreach (float sg in new[] { -1f, 1f })
            {
                var bok = rt * (sg * (szer * .5f - .05f));
                for (float s = 0f; s < 19 * t - .05f; s += 1.3f)
                {
                    float s1 = Mathf.Min(s + 1.3f, 19 * t);
                    Vector3 qa = p0 + kier * s + bok, qb = p0 + kier * s1 + bok;
                    qa.y = Mathf.Lerp(p0.y, koniec.y, s / (19 * t)); qb.y = Mathf.Lerp(p0.y, koniec.y, s1 / (19 * t));
                    S(czarnyAlu).Box(qa + V(-.025f, 0f, -.025f), qa + V(.025f, 1.42f, .025f));
                    S(szklo).Quad(qa + Vector3.up * .1f, qb + Vector3.up * .1f, qb + Vector3.up * 1.3f, qa + Vector3.up * 1.3f, rt * sg);
                    S(szklo).Quad(qa + Vector3.up * .1f, qa + Vector3.up * 1.3f, qb + Vector3.up * 1.3f, qb + Vector3.up * .1f, -rt * sg);
                    S(czarnyAlu).Rura(qa + Vector3.up * 1.42f, qb + Vector3.up * 1.42f, .025f, 8);
                }
            }
            p = koniec;
            if (bieg == 0)
            {
                // spocznik 2 m
                var s0 = p; var s1 = p + kier * 2f;
                var mn = Vector3.Min(s0 - rt * szer * .5f, s1 + rt * szer * .5f); var mx = Vector3.Max(s0 - rt * szer * .5f, s1 + rt * szer * .5f);
                S(granitC).Box(V(mn.x, p.y - .2f, mn.z), V(mx.x, p.y, mx.z));
                // podpora spocznika cofnięta 10 cm z boków i 2 cm z końców (boki nie leżą na licach płyty i stopni)
                var bmn = Vector3.Min(s0 - rt * (szer * .5f - .1f) + kier * .02f, s1 + rt * (szer * .5f - .1f) - kier * .02f);
                var bmx = Vector3.Max(s0 - rt * (szer * .5f - .1f) + kier * .02f, s1 + rt * (szer * .5f - .1f) - kier * .02f);
                S(beton).Box(V(bmn.x, p.y - .5f, bmn.z), V(bmx.x, p.y - .2f, bmx.z));
                S(grafit).Poducha(V((mn.x + mx.x) * .5f - .2f, Y0, (mn.z + mx.z) * .5f - .2f), V((mn.x + mx.x) * .5f + .2f, p.y - .5f, (mn.z + mx.z) * .5f + .2f), .05f, 0f, 4);
                BraxMiastoA.Kol(root, "Dworzec: " + nazwa + " — spocznik", V(mn.x, p.y - .5f, mn.z), V(mx.x, p.y, mx.z));
                p = s1;
            }
        }
    }

    // ============================================================================ WYPOSAŻENIE
    static void Wyposazenie()
    {
        Grupa("wyposażenie", 120f);
        // ławki na peronach (wzdłuż peronu, przodem do toru), kosze
        foreach (float x in new[] { -46f, -30f, -16f, 4f, 36f, 50f }) Lawka(x, -142.4f, 4, Vector3.back, "peron 1");
        foreach (float x in new[] { -27f, -6f, 9f, 47f }) Lawka(x, -162.6f, 4, Vector3.forward, "peron 2");
        foreach (float x in new[] { -38f, 12f, 44f }) Kosz(V(x, Y0, -141.0f), 0f);
        foreach (float x in new[] { -16f, 52f }) Kosz(V(x, Y0, -165.3f), 180f);
        // plac: ławki Poly Haven, donice z krzewami Poly Haven, kosze, stojaki rowerowe, biletomaty, totem informacyjny
        var lawPH = BraxRegion.Model("modular_street_seating", "painted_wooden_bench");
        foreach (float x in new[] { -42f, -31f })
        {
            if (lawPH) BraxRegion.Put(root, lawPH, "Dworzec: ławka na placu", V(x, Y0, -118.5f), 0f, 1f, true);
            BraxMiastoA.Siedzisko(root, "plac — ławka", V(x, Y(.58f), -118.5f), Vector3.forward);
        }
        foreach (float x in new[] { -44f, -36f, 26f, 32f })
        {
            // donica: ścianki z granitu 0.2 m, w środku ziemia 5 cm poniżej obrzeża (bez warstwy ziemi na pełnej bryle)
            S(granitC).Poducha(V(x - 1.8f, Y0, -123.4f), V(x + 1.8f, Y(.6f), -123.2f), .05f, 0f, 4);
            S(granitC).Poducha(V(x - 1.8f, Y0, -121.4f), V(x + 1.8f, Y(.6f), -121.2f), .05f, 0f, 4);
            S(granitC).Poducha(V(x - 1.8f, Y0, -123.2f), V(x - 1.6f, Y(.6f), -121.4f), .05f, 0f, 4);
            S(granitC).Poducha(V(x + 1.6f, Y0, -123.2f), V(x + 1.8f, Y(.6f), -121.4f), .05f, 0f, 4);
            Plyta(sciolka, x - 1.6f, -123.2f, x + 1.6f, -121.4f, Y(.55f), .55f);
            var kr = BraxRegion.Model("shrub_02", "shrub_03", "shrub_04");
            if (kr) BraxRegion.Put(root, kr, "Dworzec: krzew w donicy", V(x, Y(.55f), -122.3f), x * 7f, 1f, false, 1.4f);
            Kol("donica", V(x - 1.8f, Y0, -123.4f), V(x + 1.8f, Y(.6f), -121.2f));
        }
        foreach (float x in new[] { -48f, -14f, 33f }) Kosz(V(x, Y0, -114.2f), 0f);
        for (int k = 0; k < 6; k++)
        {
            float x = -57.5f + k * .95f;
            S(stal).Rura(V(x, Y0, -120.4f), V(x, Y(.75f), -120.4f), .025f, 8); S(stal).Rura(V(x, Y0, -119.6f), V(x, Y(.75f), -119.6f), .025f, 8);
            S(stal).Rura(V(x, Y(.75f), -120.4f), V(x, Y(.75f), -119.6f), .025f, 8);
        }
        foreach (float x in new[] { -27.4f, -8.6f }) Biletomat(V(x, Y0, HZ0 + 1.2f), Vector3.forward);
        // totem przy wejściu na plac od ul. Dworcowej: niebieski, piktogramy, napis DWORZEC
        Grupa("napisy", 300f);
        S(niebieski).Poducha(V(-56.7f, Y0, -114.6f), V(-55.3f, Y(6.4f), -114.0f), .14f, 0f, 6);
        Piktogram(true, V(-56f, Y(5.2f), -114.0f)); Piktogram(false, V(-56f, Y(3.8f), -114.0f));
        BraxLiteryBebas.Napis("DWORZEC", V(-56f, Y(1.6f), -114.0f), Vector3.forward, .34f, .03f, litery, grafit, .07f, .002f);
        Kol("totem DWORZEC", V(-56.7f, Y0, -114.6f), V(-55.3f, Y(6.4f), -114.0f));
        // latarnie ul. Dworcowej (słup grafit 8 m, wysięgnik nad jezdnię, oprawa LED) i placu (4.5 m)
        Grupa("wyposażenie: latarnie", 200f);
        int lat = 0;
        for (float x = -52f; x < 34f; x += 24f) { Latarnia(V(x, Y0, DrogaZ + Pas + Kr + 1.2f), Vector3.back, 8f, 2.2f); lat++; }
        for (float x = -110f; x < -88f; x += 22f) { Latarnia(V(x, Y0, DworcowaZ + Pas + Kr + 1.2f), Vector3.back, 8f, 2.2f); lat++; }
        foreach (var (x, z) in new[] { (-46f, -115.5f), (-70f, -125f), (33f, -128f), (48f, -124f), (56f, -134f) }) { Latarnia(V(x, Y0, z), Vector3.back, 4.5f, 0f); lat++; }
        foreach (float x in new[] { -40f, 46f, 60f }) { Latarnia(V(x, Y0, -165.4f), Vector3.forward, 4.5f, 0f); lat++; }
        foreach (var p in new[] { V(-20f, Y(4f), -143f), V(20f, Y(4f), -143f), V(-8f, Y(3.6f), -163.9f), V(10f, Y(3.2f), DrogaZ - Pas - ZatokaSz - 2.5f) }) BraxRegion.Warm(root, "Dworzec: światło peronu / wiaty", p, 1.1f, 18f);
        Opis("wyposażenie: ławek peronowych 10, latarni " + lat + ", biletomatów 4, totem, stojak rowerowy 6");
    }

    /// <summary>Ławka kodem: rama z dębu, nóżki stalowe, poduchy siedzisk i oparć z gąbką obite skórą, podłokietniki zaokrąglone;
    /// x = lewy koniec (wzdłuż osi x), z = oś siedziska, przodem = kierunek siedzących (±z). Każde miejsce = BraxSeat.</summary>
    internal static void Lawka(float x, float z, int n, Vector3 przodem, string gdzie)
    {
        const float sz = .65f, wys = .58f;   // miejsce 0.5 m × K, siedzisko 0.45 m × K
        float L = n * sz + .14f; float f = Mathf.Sign(przodem.z);
        S(dab).Poducha(V(x, Y(wys - .14f), z - .3f), V(x + L, Y(wys - .06f), z + .3f), .03f, 0f, 4);
        for (int i = 0; i < n; i++)
        {
            float cx = x + .07f + sz * (i + .5f);
            S(skora).Poducha(V(cx - sz * .5f + .02f, Y(wys - .06f), z - .27f), V(cx + sz * .5f - .02f, Y(wys + .06f), z + .27f), .05f, .012f, 6);
            S(skora).Poducha(V(cx - sz * .5f + .02f, Y(wys + .1f), z - f * .3f - .06f), V(cx + sz * .5f - .02f, Y(wys + .62f), z - f * .3f + .06f), .05f, .012f, 6);
            BraxMiastoA.Siedzisko(root, gdzie + " — ławka", V(cx, Y(wys + .06f), z + f * .02f), przodem);
        }
        S(dab).Poducha(V(x, Y(wys + .08f), z - f * .38f - .03f), V(x + L, Y(wys + .66f), z - f * .38f + .03f), .02f, 0f, 4);
        for (int i = 0; i <= n; i++)
        {
            float ax = x + .07f + i * sz;
            S(dab).Poducha(V(ax - .045f, Y(wys + .2f), z - .3f), V(ax + .045f, Y(wys + .27f), z + .26f), .03f, 0f, 4);
        }
        foreach (float ex in new[] { x + .2f, x + L - .2f })
            foreach (float dz in new[] { .22f, -.24f }) S(stal).Rura(V(ex, Y0, z + dz), V(ex, Y(wys - .14f), z + dz), .025f, 10);
        BraxMiastoA.Kol(root, "Dworzec: ławka (" + gdzie + ")", V(x, Y0, z - .42f), V(x + L, Y(wys + .1f), z + .32f));
    }

    static void Kosz(Vector3 p, float yaw)
    {
        var m = BraxRegion.Model("metal_trash_can");
        if (m) BraxRegion.Put(root, m, "Dworzec: kosz", p, yaw, 1f, true);
    }

    static void Roslina(Vector3 p)
    {
        var m = BraxRegion.Model("potted_plant_01", "potted_plant_02");
        if (m) BraxRegion.Put(root, m, "Dworzec: roślina w donicy", p, p.x * 13f, 1.25f, true);
    }

    static void Biletomat(Vector3 p, Vector3 przod)
    {
        var rt = Vector3.Cross(Vector3.up, przod).normalized;
        S(granat).Poducha(p - rt * .38f - przod * .25f, p + rt * .38f + przod * .25f + Vector3.up * 1.75f, .08f, 0f, 5);
        S(ekran).Box(p + przod * .25f + Vector3.up * 1.05f - rt * .26f, p + przod * .275f + Vector3.up * 1.5f + rt * .26f);   // 2.5 cm przed obudową
        S(stal).Box(p + przod * .25f + Vector3.up * .85f - rt * .15f, p + przod * .285f + Vector3.up * .9f + rt * .15f);
        BraxMiastoA.Kol(root, "Dworzec: biletomat", p - rt * .38f - przod * .25f, p + rt * .38f + przod * .25f + Vector3.up * 1.75f);
    }

    static void Latarnia(Vector3 p, Vector3 doJezdni, float h, float wysiegnik)
    {
        S(grafit).Rura(p, p + Vector3.up * h, .09f, 14);
        S(grafit).Walec(p, .16f, .5f, 16);
        var g = p + Vector3.up * h + doJezdni * wysiegnik;
        if (wysiegnik > 0f) S(grafit).Rura(p + Vector3.up * (h - .1f), g, .05f, 10);
        var rt = Vector3.Cross(Vector3.up, doJezdni).normalized; if (rt.sqrMagnitude < .1f) rt = Vector3.right;
        S(grafit).Poducha(g - rt * .2f - doJezdni * .45f + Vector3.up * -.08f, g + rt * .2f + doJezdni * .45f + Vector3.up * .08f, .04f, 0f, 4);
        S(led).Box(g - rt * .15f - doJezdni * .35f - Vector3.up * .11f, g + rt * .15f + doJezdni * .35f - Vector3.up * .08f);   // oprawa 3 cm pod obudową
        BraxMiastoA.Kol(root, "Dworzec: latarnia", p - V(.12f, 0f, .12f), p + V(.12f, h, .12f));
    }

    // ============================================================================ TABLICE ODJAZDÓW (tekstura z napisami, lico świecące)
    static readonly Dictionary<string, Material> tablice = new Dictionary<string, Material>();
    static void TablicaOdjazdow(string tytul, Vector3 srodek, Vector3 n, float w, float h, bool pociagi)
    {
        if (!tablice.TryGetValue(tytul, out var m) || !m) tablice[tytul] = m = MaterialTablicy(tytul, w / h, pociagi);
        // srodek = punkt na licu, do którego tablica jest przykręcona (mur, belka, skrzynka); rama 8 cm od lica, ekran — bryła
        // 2.5 cm przed ramą (lico ekranu nie leży na licu ramy)
        var rt = Vector3.Cross(Vector3.up, -n).normalized;
        S(czarnyAlu).Poducha(srodek - rt * (w * .5f + .06f) - Vector3.up * (h * .5f + .06f), srodek + rt * (w * .5f + .06f) + Vector3.up * (h * .5f + .06f) + n * .08f, .03f, 0f, 4);
        var s = S(m);
        Vector3 a = srodek - rt * w * .5f - Vector3.up * h * .5f + n * .105f, b = srodek + rt * w * .5f - Vector3.up * h * .5f + n * .105f;
        Vector3 cU = b + Vector3.up * h, dU = a + Vector3.up * h, dn = n * .025f;
        var sb = S(czarnyAlu);
        sb.Quad(a - dn, b - dn, b, a, Vector3.down); sb.Quad(dU - dn, cU - dn, cU, dU, Vector3.up);
        sb.Quad(a - dn, dU - dn, dU, a, -rt); sb.Quad(b - dn, cU - dn, cU, b, rt);
        int k = s.v.Count;
        s.Quad(a, b, b + Vector3.up * h, a + Vector3.up * h, n);
        // UV całej tekstury (nie metry): lewy dół → prawy góra (czytane od strony patrzącego)
        s.uv[k] = new Vector2(0f, 0f); s.uv[k + 1] = new Vector2(1f, 0f); s.uv[k + 2] = new Vector2(1f, 1f); s.uv[k + 3] = new Vector2(0f, 1f);
    }

    static Material MaterialTablicy(string tytul, float proporcja, bool pociagi)
    {
        int W = 1024, H = Mathf.Clamp(Mathf.RoundToInt(1024f / proporcja), 256, 1024);
        var px = new Color[W * H];
        var tlo = new Color(.02f, .04f, .09f); var pas = new Color(.06f, .2f, .45f); var zolty = new Color(1f, .82f, .2f);
        for (int i = 0; i < px.Length; i++) px[i] = tlo;
        for (int y = H - Mathf.RoundToInt(H * .18f); y < H; y++) for (int x = 0; x < W; x++) px[y * W + x] = pas;
        BraxUliceKit.NapisWTeksturze(px, W, H, tytul, W * .5f, H * .865f, H * .075f, Color.white);
        string[] cele = pociagi ? new[] { "MIASTO MISTRZÓW", "MIASTO MISTRZÓW", "MIASTO MISTRZÓW", "MIASTO MISTRZÓW" } : new[] { "OSIEDLE", "CENTRUM", "OSIEDLE", "CENTRUM" };
        for (int r = 0; r < 4; r++)
        {
            float yb = H * (.66f - r * .16f);
            int godz = 6 + r * 2;
            BraxUliceKit.NapisWTeksturze(px, W, H, (godz < 10 ? "0" : "") + godz + ".15", W * .12f, yb, H * .06f, zolty);
            BraxUliceKit.NapisWTeksturze(px, W, H, cele[r], W * .48f, yb, H * .06f, Color.white);
            BraxUliceKit.NapisWTeksturze(px, W, H, pociagi ? "PERON " + (r % 2 + 1) : "STAN. " + (r % 2 + 1), W * .86f, yb, H * .055f, Color.white);
        }
        var tx = new Texture2D(W, H, TextureFormat.RGB24, false); tx.SetPixels(px); tx.Apply();
        string dir = BraxMiastoA.Out + "/Dworzec"; Directory.CreateDirectory(dir);
        string path = dir + "/tablica_" + BraxUliceKit.Bezpieczna(tytul) + ".png";
        File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var m = BraxMiastoA.Swieci("Dworzec: tablica " + tytul, Color.white, new Color(.15f, .15f, .15f));
        m.SetTexture("_MainTex", t);
        if (m.HasProperty("_EmissionMap")) { m.SetTexture("_EmissionMap", t); m.SetColor("_EmissionColor", new Color(.9f, .9f, .9f)); }
        EditorUtility.SetDirty(m);
        return m;
    }
}

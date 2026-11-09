using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// WARSZTATY UL. WARSZTATOWEJ OD NOWA — PREMIUM (9.11, właściciel: „cały teren za siłownią… tam, gdzie wulkanizacja, warsztat…
// od A do Z”; „opony… koła”). Dotąd: 4 pudła z cegły 10 × 13.5 m z bramą i szyldem, płaski betonowy dach z daszkiem (z góry
// „biały klocek”), ciemne wnętrze jedynego otwartego warsztatu (z ulicy „czarny otwór”), szczelina 35 cm pod każdą bramą
// (ściana stała na chodniku, posadzka kończyła się przed murem — prześwit do wody pod miastem), bez okien, drzwi i rynien.
// Teraz (ten sam obrys x −112…−72, lico z −24.75, tył z −38.25 — parking, trawniki i ruch bez zmian), × k gdzie ludzka skala:
//  • ELEWACJA: cegła z cokołem z granitu grafitowego, lizeny na granicach lokali, gzyms z cegły i stalowa czapa attyki
//    (attyka zasłania dach), w każdym lokalu: brama w stalowej ościeżnicy z nadprożem, drzwi dla klientów z szybą, okno
//    przy bramie i dwa okna górne (ramy stalowe, szprosy, parapety), szyld-kaseton z literami (WULKANIZACJA żółty, AUTO
//    NAPRAWA niebieski, GARAŻ 12 i MAGAZYN grafit), tablica usług nad drzwiami, numer na lizenie, dwie lampy „gęsia szyja”
//    nad szyldem, rury spustowe z lejami; boki (od Alei Płd. i od placu wieżowców) i tył (od parkingu): okna, drzwi
//    zaplecza z lampą, rury spustowe; na boku od alei napis WARSZTATY · OPONY · SERWIS;
//  • DACH: membrana grafitowa za attyką, świetliki nad lokalami, agregat i wywiewki — z tarasów nie ma „białego klocka”;
//  • PROGI: posadzka lokalu sięga lica muru (pod bramą próg stalowy) — koniec szczeliny z wodą pod bramami;
//  • WNĘTRZA OTWARTE (oba z jasną posadzką z żywicy, liniami BHP w wycięciach, lamperią, tynkiem, oprawami LED świecącymi
//    na każdym telefonie i ciepłym światłem): WULKANIZACJA — montażownica i wyważarka z kołami, regał z kołami, stosy opon,
//    kompresor ze zwijadłem, podnośnik „żaba”, auto klienta na stanowisku; AUTO NAPRAWA — podnośnik dwukolumnowy z
//    PODNIESIONYM autem, stół z tablicą narzędzi, szafka narzędziowa, wózek, kompresor, beczki, regał z kołami;
//    zamknięte GARAŻ 12 i MAGAZYN — bramy rolowane z prowadnicami i listwą dolną;
//  • KOŁA (opona + felga) i auta w warsztatach = te same modele co auta miasta (krok ruchu: AutaWarsztatow, KolaWarsztatow);
//    przed wulkanizacją stosy opon pod oknem;
//  • PLAC USTAWEK bez zmian (asfalt z liniami w wycięciach, ławki kibiców).
public static partial class BraxRegionMiastoB
{
    public const float WarX0 = -112f, WarModul = 10f;
    public const int WarIle = 4;
    public static float WarZf => SZ - SH - Kr - 2.5f;   // lico (−24.75)
    public static float WarZb => WarZf - 13.5f;         // tył (−38.25)
    public static readonly string[] WarNazwy = { "WULKANIZACJA", "AUTO NAPRAWA", "GARAŻ 12", "MAGAZYN" };
    static readonly string[] WarUslugi = { "OPONY · FELGI · WYWAŻANIE", "MECHANIKA · DIAGNOSTYKA", "WYNAJEM GARAŻY", "DOSTAWY · ODBIÓR" };
    /// <summary>Auta w warsztatach (krok ruchu stawia model auta miasta): środek na podłożu kół, przód, uniesienie (podnośnik).</summary>
    public static readonly List<(Vector3 c, Vector3 przod, float podnies, string nazwa)> AutaWarsztatow = new List<(Vector3, Vector3, float, string)>();
    /// <summary>Koła (opona + felga z modelu auta): punkt podparcia, oś koła (pionowa = leży), liczba w stosie.</summary>
    public static readonly List<(Vector3 podparcie, Vector3 os, int ile, string nazwa)> KolaWarsztatow = new List<(Vector3, Vector3, int, string)>();
    public static Vector3 MechanikAutoNaprawa = new Vector3(-100.1f, .2f, -36.3f), MechanikWulkanizacja = new Vector3(-109.5f, .2f, -28f);
    static int warOkien, warLamp;

    static void Warsztaty()
    {
        AutaWarsztatow.Clear(); KolaWarsztatow.Clear(); warOkien = warLamp = 0;
        float zf = WarZf, zb = WarZb, h = 5.2f * K, t = .35f, yT = YC + h, yA = yT + .6f;
        var w = new GameObject("Warsztaty ul. Warsztatowej").transform; w.SetParent(root, false);
        // materiały
        var cokol = BraxRegionMeble.Tex("Warsztaty: cokół granit grafit", 1.2f, new Color(.32f, .32f, .33f), .7f, "granite_tile_02", "rough_block_wall");
        var stalG = BraxCityMaterials.Surface("Warsztat: stal grafit", new Color(.12f, .13f, .14f), .5f, .6f);
        var szybaO = BraxCityMaterials.Surface("Warsztat: szyba okien (odbicie)", new Color(.22f, .27f, .30f), .07f, .35f);
        var membrana = BraxCityMaterials.Surface("Warsztaty: dach — papa termozgrzewalna grafit", new Color(.22f, .23f, .24f), .9f);
        var swietlik = BraxCityMaterials.Surface("Warsztaty: świetlik poliwęglan", new Color(.74f, .78f, .80f), .25f, .1f);
        var cynk = BraxCityMaterials.Surface("Warsztaty: rynny tytan-cynk", new Color(.50f, .52f, .53f), .45f, .7f);
        var lampaM = BraxRegionMeble.Swiecacy("Warsztaty: lampa elewacyjna (ciepła)", new Color(1f, .88f, .70f), 1.4f);
        var brama = BraxCityMaterials.Surface("Warsztat: brama rolowana (ocynk lakier)", new Color(.62f, .64f, .66f), .45f, .6f);
        var drzwiM = BraxCityMaterials.Surface("Warsztat: drzwi stalowe (RAL antracyt)", new Color(.20f, .22f, .24f), .5f, .4f);
        var agregatM = BraxCityMaterials.Surface("Warsztaty: agregat (ocynk lakier)", new Color(.76f, .77f, .78f), .5f, .4f);
        var licoSzyldu = new[] { BraxRegionMeble.Swiecacy("Warsztat: szyld WULKANIZACJA (żółty)", new Color(.95f, .74f, .08f), .35f),
                                 BraxRegionMeble.Swiecacy("Warsztat: szyld AUTO NAPRAWA (niebieski)", new Color(.06f, .24f, .58f), .35f), null, null };
        var literySzyldu = new[] { new Color(.07f, .07f, .07f), Color.white, new Color(.95f, .93f, .86f), new Color(.95f, .93f, .86f) };
        var sc = new BraxRegionMeble.Bud(); var ck = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud(); var sz = new BraxRegionMeble.Bud();
        var bet = new BraxRegionMeble.Bud(); var dach = new BraxRegionMeble.Bud(); var gl = new BraxRegionMeble.Bud(); var cz = new BraxRegionMeble.Bud();
        var lp = new BraxRegionMeble.Bud(); var br = new BraxRegionMeble.Bud(); var dz = new BraxRegionMeble.Bud(); var ag = new BraxRegionMeble.Bud();
        var pos = new BraxRegionMeble.Bud();
        float xE0 = WarX0, xE1 = WarX0 + WarIle * WarModul;
        float bw = 3.6f * K, bh = 3.6f * K, dw = 1.0f * K, dh = 2.2f * K;
        // ---------------- ELEWACJA FRONTOWA (lico z = zf, na zewnątrz +z) ----------------
        var otworyF = new List<Rect>();
        for (int i = 0; i < WarIle; i++)
        {
            float a = xE0 + i * WarModul, b = a + WarModul, m = (a + b) * .5f;
            otworyF.Add(Rect.MinMaxRect(m - bw * .5f, YC, m + bw * .5f, YC + bh));                 // brama
            otworyF.Add(Rect.MinMaxRect(a + .75f, YC, a + .75f + dw, YC + dh));                    // drzwi klientów
            otworyF.Add(Rect.MinMaxRect(b - 2.25f, YC + 1.0f, b - .65f, YC + 2.9f));                // okno przy bramie
            otworyF.Add(Rect.MinMaxRect(a + .6f, YC + 3.45f, a + 2.2f, YC + 5.0f));                 // okna górne
            otworyF.Add(Rect.MinMaxRect(b - 2.25f, YC + 3.45f, b - .65f, YC + 5.0f));
        }
        MurZOtworami(sc, true, zf - t, zf, xE0, xE1, YC, yA, otworyF);
        MurZOtworami(ck, true, zf - .02f, zf + .04f, xE0 - .04f, xE1 + .04f, YC, YC + .55f, otworyF.Where(q => q.yMin < YC + .55f).ToList());
        for (int i = 0; i <= WarIle; i++)
        {
            float x = xE0 + i * WarModul, x0 = i == 0 ? x : x - .35f, x1 = i == WarIle ? x : x + .35f;
            sc.MinMax(new Vector3(x0, YC, zf - .05f), new Vector3(x1, yT - .55f, zf + .12f));      // lizena
        }
        sc.MinMax(new Vector3(xE0 - .14f, yT - .55f, zf - .05f), new Vector3(xE1 + .14f, yT - .40f, zf + .14f));   // gzyms z cegły (wspornikowy)
        st.MinMax(new Vector3(xE0 - .05f, yA, zf - t - .05f), new Vector3(xE1 + .05f, yA + .07f, zf + .06f));   // czapa attyki
        for (int i = 0; i < WarIle; i++)
        {
            float a = xE0 + i * WarModul, b = a + WarModul, m = (a + b) * .5f;
            bool otwarty = i <= 1;
            // ościeżnica bramy i nadproże (stal), szyld-kaseton z literami, tablica usług, numer, lampy „gęsia szyja”
            st.MinMax(new Vector3(m - bw * .5f - .14f, YC, zf - .02f), new Vector3(m - bw * .5f, YC + bh, zf + .05f));
            st.MinMax(new Vector3(m + bw * .5f, YC, zf - .02f), new Vector3(m + bw * .5f + .14f, YC + bh, zf + .05f));
            st.MinMax(new Vector3(m - bw * .5f - .35f, YC + bh, zf - .02f), new Vector3(m + bw * .5f + .35f, YC + bh + .26f, zf + .05f));
            float sy0 = YC + bh + .32f, sy1 = YC + bh + 1.12f;
            st.MinMax(new Vector3(m - 2.7f, sy0, zf + .02f), new Vector3(m + 2.7f, sy1, zf + .2f));
            if (licoSzyldu[i]) { var pl = new BraxRegionMeble.Bud(); pl.MinMax(new Vector3(m - 2.62f, sy0 + .06f, zf + .2f), new Vector3(m + 2.62f, sy1 - .06f, zf + .215f)); pl.Obiekt("Warsztat " + WarNazwy[i] + ": lico szyldu", w, licoSzyldu[i], false, false); }
            BraxRegionMeble.Napis(w, WarNazwy[i], new Vector3(m, (sy0 + sy1) * .5f, zf + .23f), Vector3.forward, .36f * K, literySzyldu[i]);
            var tab = new BraxRegionMeble.Bud(); tab.MinMax(new Vector3(a + .6f, YC + dh + .12f, zf + .0f), new Vector3(a + 2.2f, YC + dh + .42f, zf + .05f));
            tab.Obiekt("Warsztat " + WarNazwy[i] + ": tablica usług", w, BraxCityMaterials.Surface("Warsztat: tablica usług (grafit)", new Color(.16f, .17f, .18f), .5f, .3f), false, false);
            BraxRegionMeble.Napis(w, WarUslugi[i], new Vector3(a + 1.4f, YC + dh + .27f, zf + .06f), Vector3.forward, .085f * K, new Color(.95f, .93f, .86f));
            BraxRegionMeble.Napis(w, "Warsztatowa " + (2 * i + 2), new Vector3(b - 1.45f, YC + 3.17f, zf + .02f), Vector3.forward, .09f * K, Color.white);
            foreach (float dx in new[] { -1.9f, 1.9f })
            {
                float lx = m + dx, ly = YC + bh + 1.45f;
                st.Cyl(new Vector3(lx, ly, zf), new Vector3(lx, ly, zf + .48f), .022f, 8);
                st.Cyl(new Vector3(lx, ly - .02f, zf + .5f), new Vector3(lx, ly - .16f, zf + .5f), .05f, 14, .2f);
                lp.MinMax(new Vector3(lx - .07f, ly - .2f, zf + .43f), new Vector3(lx + .07f, ly - .16f, zf + .57f)); warLamp++;
            }
            // drzwi klientów: ościeżnica, skrzydło z szybą, klamka; nad drzwiami daszek nie (tablica usług)
            float d0 = a + .75f, d1 = d0 + dw;
            st.MinMax(new Vector3(d0 - .07f, YC, zf - .2f), new Vector3(d0, YC + dh + .07f, zf - .08f)); st.MinMax(new Vector3(d1, YC, zf - .2f), new Vector3(d1 + .07f, YC + dh + .07f, zf - .08f));
            st.MinMax(new Vector3(d0, YC + dh, zf - .2f), new Vector3(d1, YC + dh + .07f, zf - .08f));
            dz.MinMax(new Vector3(d0, YC, zf - .2f), new Vector3(d1, YC + .95f * K, zf - .15f));
            dz.MinMax(new Vector3(d0, YC + 1.9f * K, zf - .2f), new Vector3(d1, YC + dh, zf - .15f));
            dz.MinMax(new Vector3(d0, YC + .95f * K, zf - .2f), new Vector3(d0 + .1f, YC + 1.9f * K, zf - .15f)); dz.MinMax(new Vector3(d1 - .1f, YC + .95f * K, zf - .2f), new Vector3(d1, YC + 1.9f * K, zf - .15f));
            sz.MinMax(new Vector3(d0 + .1f, YC + .95f * K, zf - .18f), new Vector3(d1 - .1f, YC + 1.9f * K, zf - .17f));
            st.MinMax(new Vector3(d1 - .2f, YC + 1.05f * K, zf - .15f), new Vector3(d1 - .08f, YC + 1.1f * K, zf - .1f));
            BraxRegionMeble.Kolizja(w, "Warsztat: drzwi klientów (zamknięte)", new Vector3((d0 + d1) * .5f, YC + dh * .5f, zf - .17f), new Vector3(dw, dh, .1f));
            // okna: szyba w głębi 0.2, rama, szpros, parapet
            foreach (var o in otworyF.Where(q => q.xMin >= a && q.xMax <= b && q.yMin > YC + .5f)) Okno(st, sz, bet, true, zf, o, -1f);
            // brama
            st.MinMax(new Vector3(m - bw * .5f - .1f, YC + bh - .02f, zf - .34f), new Vector3(m + bw * .5f + .1f, YC + bh + .3f * K, zf - .05f));   // skrzynka rolety
            if (!otwarty)
            {
                for (float y = YC + .05f; y < YC + bh - .05f; y += .1f * K) br.MinMax(new Vector3(m - bw * .5f, y, zf - .3f), new Vector3(m + bw * .5f, y + .085f * K, zf - .24f));
                st.MinMax(new Vector3(m - bw * .5f, YC + .02f, zf - .31f), new Vector3(m + bw * .5f, YC + .14f, zf - .23f));   // listwa dolna
                foreach (float s in new[] { -1f, 1f }) st.MinMax(new Vector3(m + s * bw * .5f - .05f, YC, zf - .33f), new Vector3(m + s * bw * .5f + .05f, YC + bh, zf - .21f));   // prowadnice
                st.MinMax(new Vector3(m - .1f, YC + .45f, zf - .24f), new Vector3(m + .1f, YC + .5f, zf - .2f));   // uchwyt z zamkiem
                BraxRegionMeble.Kolizja(w, "Brama zamknięta", new Vector3(m, YC + bh * .5f, zf - .27f), new Vector3(bw, bh, .1f));
            }
            // posadzka do lica muru (próg) — bez szczeliny pod bramą
            if (!otwarty) pos.MinMax(new Vector3(a + (i == 0 ? t : 0f), YC - .25f, zb + t), new Vector3(b - t, YC + .05f, zf));
            else st.MinMax(new Vector3(m - bw * .5f, YC - .06f, zf - .07f), new Vector3(m + bw * .5f, YC + .08f, zf + .005f));   // próg stalowy bramy otwartej (3 cm nad żywicą)
            BraxRegionMeble.Kolizja(w, "Warsztat posadzka", new Vector3(m, YC - .1f, (zb + zf) * .5f), new Vector3(WarModul, .3f, zf - zb));
            // kolizje muru frontu (przy bramie otwartej przejście)
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja front L", new Vector3((a + m - bw * .5f) * .5f, YC + h * .5f, zf - .17f), new Vector3(m - bw * .5f - a, h, .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja front P", new Vector3((m + bw * .5f + b) * .5f, YC + h * .5f, zf - .17f), new Vector3(b - m - bw * .5f, h, .35f));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja nad bramą", new Vector3(m, YC + bh + (h - bh) * .5f, zf - .17f), new Vector3(bw, h - bh, .35f));
            // ściana boczna między lokalami (wewnątrz) i ściana tylna z drzwiami zaplecza i oknami górnymi
            if (i < WarIle - 1) sc.MinMax(new Vector3(b - t, YC, zb + t), new Vector3(b, yT - .3f, zf - t));   // ostatni lokal: ściana szczytowa z oknami (niżej)
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja bok", new Vector3(b - .17f, YC + h * .5f, (zb + zf) * .5f), new Vector3(.35f, h, zf - zb));
            BraxRegionMeble.Kolizja(w, "Warsztat kolizja tył", new Vector3(m, YC + h * .5f, zb + .17f), new Vector3(WarModul, h, .35f));
            if (otwarty) Wnetrze(w, i, a + (i == 0 ? t : 0f), b - t, zb + t, zf - t, YC + .05f, yT - .3f);
            else world_point("R1_WARSZTAT_" + (i == 2 ? "GARAZ_12" : "MAGAZYN"), WarNazwy[i] + " (zamknięty)", new Vector3(m, YC, zf + 1.2f));
        }
        BraxRegionMeble.Kolizja(w, "Warsztat kolizja bok", new Vector3(xE0 + .17f, YC + h * .5f, (zb + zf) * .5f), new Vector3(.35f, h, zf - zb));
        // ---------------- TYŁ (od parkingu, lico z = zb, na zewnątrz −z) ----------------
        var otworyT = new List<Rect>();
        for (int i = 0; i < WarIle; i++)
        {
            float a = xE0 + i * WarModul;
            otworyT.Add(Rect.MinMaxRect(a + 1.0f, YC, a + 1.0f + dw, YC + dh));
            foreach (float s in new[] { 3.4f, 5.7f, 8.0f }) otworyT.Add(Rect.MinMaxRect(a + s - .7f, YC + 3.55f, a + s + .7f, YC + 4.95f));
        }
        MurZOtworami(sc, true, zb, zb + t, xE0, xE1, YC, yA, otworyT);
        MurZOtworami(ck, true, zb - .04f, zb + .02f, xE0 - .04f, xE1 + .04f, YC, YC + .55f, otworyT.Where(q => q.yMin < YC + .55f).ToList());
        st.MinMax(new Vector3(xE0 - .05f, yA, zb - .06f), new Vector3(xE1 + .05f, yA + .07f, zb + t + .05f));
        foreach (var o in otworyT.Where(q => q.yMin > YC + .5f)) Okno(st, sz, bet, true, zb, o, 1f);
        foreach (var o in otworyT.Where(q => q.yMin < YC + .5f))
        {
            dz.MinMax(new Vector3(o.xMin, YC, zb + .15f), new Vector3(o.xMax, o.yMax, zb + .2f));
            st.MinMax(new Vector3(o.xMin - .07f, YC, zb + .08f), new Vector3(o.xMin, o.yMax + .07f, zb + .2f)); st.MinMax(new Vector3(o.xMax, YC, zb + .08f), new Vector3(o.xMax + .07f, o.yMax + .07f, zb + .2f));
            st.MinMax(new Vector3(o.xMin, o.yMax, zb + .08f), new Vector3(o.xMax, o.yMax + .07f, zb + .2f));
            st.MinMax(new Vector3(o.xMin + .08f, YC + 1.05f * K, zb + .1f), new Vector3(o.xMin + .2f, YC + 1.1f * K, zb + .15f));
            lp.MinMax(new Vector3((o.xMin + o.xMax) * .5f - .15f, o.yMax + .35f, zb - .12f), new Vector3((o.xMin + o.xMax) * .5f + .15f, o.yMax + .5f, zb)); warLamp++;
            st.MinMax(new Vector3(o.xMin - .3f, o.yMax + .22f, zb - .9f), new Vector3(o.xMax + .3f, o.yMax + .3f, zb));   // daszek nad drzwiami zaplecza
            BraxRegionMeble.Kolizja(w, "Warsztat: drzwi zaplecza (zamknięte)", new Vector3((o.xMin + o.xMax) * .5f, YC + dh * .5f, zb + .17f), new Vector3(dw, dh, .1f));
        }
        // ---------------- BOKI (zachód od Alei Płd., wschód od placu wieżowców) ----------------
        foreach (var (xs, zew) in new[] { (xE0, -1f), (xE1, 1f) })
        {
            float xi = zew < 0 ? xs + t : xs - t;
            var otworyB = new List<Rect>();
            foreach (float zc in new[] { zb + 3.6f, zb + 6.75f, zb + 9.9f })
            {
                otworyB.Add(Rect.MinMaxRect(zc - .8f, YC + 1.0f, zc + .8f, YC + 2.9f));
                otworyB.Add(Rect.MinMaxRect(zc - .8f, YC + 3.45f, zc + .8f, YC + 5.0f));
            }
            MurZOtworami(sc, false, Mathf.Min(xs, xi), Mathf.Max(xs, xi), zb + t, zf - t, YC, yA, otworyB);
            MurZOtworami(ck, false, zew < 0 ? xs - .04f : xs - .02f, zew < 0 ? xs + .02f : xs + .04f, zb - .04f, zf + .04f, YC, YC + .55f, new List<Rect>());
            st.MinMax(new Vector3(zew < 0 ? xs - .06f : xi - .05f, yA, zb), new Vector3(zew < 0 ? xi + .05f : xs + .06f, yA + .07f, zf));
            foreach (var o in otworyB) OknoBok(st, sz, bet, xs, zew, o);
            sc.MinMax(new Vector3(zew < 0 ? xs - .14f : xs - .05f, yT - .55f, zb), new Vector3(zew < 0 ? xs + .05f : xs + .14f, yT - .40f, zf));   // gzyms
            if (zew < 0) BraxRegionMeble.Napis(w, "WARSZTATY · OPONY · SERWIS", new Vector3(xs - .16f, yT + .1f, (zb + zf) * .5f), Vector3.left, .34f * K, new Color(.93f, .90f, .82f));
        }
        // ---------------- DACH, RYNNY ----------------
        dach.MinMax(new Vector3(xE0 + t, yT - .3f, zb + t), new Vector3(xE1 - t, yT, zf - t));
        for (int i = 0; i < WarIle; i++)
        {
            float a = xE0 + i * WarModul, m = a + WarModul * .5f, zc = (zb + zf) * .5f;
            dach.MinMax(new Vector3(m - .9f, yT, zc - 1.8f), new Vector3(m + .9f, yT + .3f, zc + 1.8f));
            gl.MinMax(new Vector3(m - .85f, yT + .3f, zc - 1.75f), new Vector3(m + .85f, yT + .38f, zc + 1.75f));
            ag.Cyl(new Vector3(a + 2.0f, yT, zb + 2.2f), new Vector3(a + 2.0f, yT + .55f, zb + 2.2f), .12f, 10);
            st.Cyl(new Vector3(a + 2.0f, yT + .55f, zb + 2.2f), new Vector3(a + 2.0f, yT + .65f, zb + 2.2f), .2f, 10, .04f);
        }
        ag.MinMax(new Vector3(-99.2f, yT, zb + 1.2f), new Vector3(-97.2f, yT + 1.1f, zb + 2.5f));   // agregat chłodzenia nad AUTO NAPRAWA
        st.Cyl(new Vector3(-98.7f, yT + 1.1f, zb + 1.85f), new Vector3(-98.7f, yT + 1.14f, zb + 1.85f), .4f, 16); st.Cyl(new Vector3(-97.7f, yT + 1.1f, zb + 1.85f), new Vector3(-97.7f, yT + 1.14f, zb + 1.85f), .4f, 16);
        foreach (var (x, zz, s) in new[] { (-101.55f, zf, 1f), (-81.55f, zf, 1f), (-111.4f, zb, -1f), (-92.25f, zb, -1f), (-72.6f, zb, -1f) })
        {
            float zp = zz + s * .22f;
            cz.MinMax(new Vector3(x - .14f, yT - .38f, Mathf.Min(zz, zz + s * .34f)), new Vector3(x + .14f, yT - .05f, Mathf.Max(zz, zz + s * .34f)));
            cz.Cyl(new Vector3(x, yT - .38f, zp), new Vector3(x, YC + .4f, zp), .055f, 10);
            cz.Cyl(new Vector3(x, YC + .4f, zp), new Vector3(x, YC + .1f, zz + s * .42f), .055f, 10);
            for (float y = YC + 1.2f; y < yT - .6f; y += 1.8f) cz.MinMax(new Vector3(x - .06f, y, Mathf.Min(zz, zp)), new Vector3(x + .06f, y + .04f, Mathf.Max(zz, zp)));
        }
        // ---------------- obiekty ----------------
        sc.Obiekt("Warsztaty: cegła (mur, lizeny, gzyms, attyka)", w, cegla, false, true);
        ck.Obiekt("Warsztaty: cokół (granit grafit)", w, cokol, false, true);
        st.Obiekt("Warsztaty: stal (ościeżnice, nadproża, ramy okien, czapa attyki, lampy, kasetony)", w, stalG, false, true);
        sz.Obiekt("Warsztaty: szyby okien i drzwi", w, szybaO, false, false);
        bet.Obiekt("Warsztaty: parapety (beton)", w, beton, false, true);
        dach.Obiekt("Warsztaty: dach (papa, krawężniki świetlików)", w, membrana, false, true);
        gl.Obiekt("Warsztaty: świetliki", w, swietlik, false, true);
        cz.Obiekt("Warsztaty: rynny i rury spustowe", w, cynk, false, true);
        lp.Obiekt("Warsztaty: lampy (świecą)", w, lampaM, false, false);
        br.Obiekt("Warsztaty: bramy rolowane", w, brama, false, true);
        dz.Obiekt("Warsztaty: drzwi stalowe", w, drzwiM, false, true);
        ag.Obiekt("Warsztaty: agregat i wywiewki", w, agregatM, false, true);
        pos.Obiekt("Warsztaty: posadzka zamkniętych lokali (do lica)", w, beton, false, false);
        // opony przed wulkanizacją (pod oknem przy bramie) — koła z modeli aut (krok ruchu)
        foreach (float x in new[] { -104.15f, -103.42f, -102.69f }) KolaWarsztatow.Add((new Vector3(x, YC, zf + .42f), Vector3.up, x < -104f ? 5 : 4, "przed wulkanizacją"));
        BraxRegion.Note("miasto B: warsztaty premium — 4 lokale (2 otwarte z wnętrzem), okien " + warOkien + ", lamp " + warLamp + ", aut w warsztatach " + AutaWarsztatow.Count + ", miejsc na koła " + KolaWarsztatow.Count);
        PlacUstawek();
    }

    // mur z otworami: wzdX = mur wzdłuż x (grubość z c0…c1), inaczej wzdłuż z (grubość x c0…c1); otwory w (a, y)
    static void MurZOtworami(BraxRegionMeble.Bud b, bool wzdX, float c0, float c1, float a0, float a1, float y0, float y1, List<Rect> otwory)
    {
        var xs = new SortedSet<float> { a0, a1 };
        foreach (var o in otwory) { if (o.xMin > a0 && o.xMin < a1) xs.Add(o.xMin); if (o.xMax > a0 && o.xMax < a1) xs.Add(o.xMax); }
        var X = xs.ToList();
        for (int i = 0; i + 1 < X.Count; i++)
        {
            float a = X[i], c = X[i + 1]; if (c - a < .002f) continue; float m = (a + c) * .5f;
            var zaj = otwory.Where(o => o.xMin <= m && o.xMax >= m).OrderBy(o => o.yMin).ToList();
            float y = y0;
            void Pas(float ya, float yb) { if (yb - ya < .002f) return; if (wzdX) b.MinMax(new Vector3(a, ya, c0), new Vector3(c, yb, c1)); else b.MinMax(new Vector3(c0, ya, a), new Vector3(c1, yb, c)); }
            foreach (var o in zaj) { float oa = Mathf.Max(y0, o.yMin), ob = Mathf.Min(y1, o.yMax); if (ob <= oa) continue; Pas(y, oa); y = Mathf.Max(y, ob); }
            Pas(y, y1);
        }
    }

    // okno w murze wzdłuż x: lico z = zl, s = −1 (mur za licem w −z: front) albo +1 (tył: mur w +z); szyba 0.2 w głębi
    static void Okno(BraxRegionMeble.Bud st, BraxRegionMeble.Bud sz, BraxRegionMeble.Bud bet, bool wzdX, float zl, Rect o, float s)
    {
        float g = zl + s * .2f, g1 = zl + s * .16f, g2 = zl + s * .24f, f = .06f;
        Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        sz.MinMax(V(o.xMin, o.yMin, g - s * .005f), V(o.xMax, o.yMax, g + s * .005f));
        st.MinMax(V(o.xMin, o.yMin, g1), V(o.xMin + f, o.yMax, g2)); st.MinMax(V(o.xMax - f, o.yMin, g1), V(o.xMax, o.yMax, g2));
        st.MinMax(V(o.xMin + f, o.yMin, g1), V(o.xMax - f, o.yMin + f, g2)); st.MinMax(V(o.xMin + f, o.yMax - f, g1), V(o.xMax - f, o.yMax, g2));
        float sx = (o.xMin + o.xMax) * .5f, sy = o.yMin + (o.yMax - o.yMin) * .62f;
        st.MinMax(V(sx - .025f, o.yMin + f, g1 + s * .01f), V(sx + .025f, o.yMax - f, g2 - s * .01f));
        st.MinMax(V(o.xMin + f, sy - .025f, g1 + s * .01f), V(o.xMax - f, sy + .025f, g2 - s * .01f));
        bet.MinMax(V(o.xMin - .05f, o.yMin - .07f, zl + s * .2f), V(o.xMax + .05f, o.yMin, zl - s * .07f));
        warOkien++;
    }

    // okno w murze bocznym (wzdłuż z): lico x = xs, zew = −1 (zachód) / +1 (wschód)
    static void OknoBok(BraxRegionMeble.Bud st, BraxRegionMeble.Bud sz, BraxRegionMeble.Bud bet, float xs, float zew, Rect o)
    {
        float g = xs - zew * .2f, g1 = xs - zew * .16f, g2 = xs - zew * .24f, f = .06f;
        Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        sz.MinMax(V(g - .005f, o.yMin, o.xMin), V(g + .005f, o.yMax, o.xMax));
        st.MinMax(V(g1, o.yMin, o.xMin), V(g2, o.yMax, o.xMin + f)); st.MinMax(V(g1, o.yMin, o.xMax - f), V(g2, o.yMax, o.xMax));
        st.MinMax(V(g1, o.yMin, o.xMin + f), V(g2, o.yMin + f, o.xMax - f)); st.MinMax(V(g1, o.yMax - f, o.xMin + f), V(g2, o.yMax, o.xMax - f));
        float sx = (o.xMin + o.xMax) * .5f, sy = o.yMin + (o.yMax - o.yMin) * .62f;
        st.MinMax(V(g1, o.yMin + f, sx - .025f), V(g2, o.yMax - f, sx + .025f));
        st.MinMax(V(g1, sy - .025f, o.xMin + f), V(g2, sy + .025f, o.xMax - f));
        bet.MinMax(V(xs - zew * .2f, o.yMin - .07f, o.xMin - .05f), V(xs + zew * .07f, o.yMin, o.xMax + .05f));
        warOkien++;
    }

    // wnętrze otwartego lokalu: posadzka z żywicy z liniami BHP w wycięciach, tynk z lamperią, sufit z oprawami LED, wyposażenie
    static void Wnetrze(Transform w, int i, float x0, float x1, float z0, float z1, float yp, float ys)
    {
        float m = (x0 + x1) * .5f, zc = (z0 + z1) * .5f;
        string n = WarNazwy[i];
        var tynk = BraxRegionMeble.Tex("Warsztat: tynk wnętrza", 1.5f, new Color(.90f, .90f, .88f), .9f, "white_plaster_02", "white_stucco_02");
        var lamperia = BraxCityMaterials.Surface("Warsztat: lamperia (farba olejna grafit)", new Color(.27f, .29f, .32f), .45f);
        var zywica = BraxCityMaterials.Surface("Warsztat: posadzka żywica epoksydowa", new Color(.50f, .52f, .54f), .35f);
        var bhp = BraxCityMaterials.Surface("Warsztat: linia BHP żółta", new Color(.95f, .76f, .06f), .5f);
        var led = BraxRegionMeble.Swiecacy("Warsztat: oprawa LED (jasna)", new Color(.96f, .96f, .92f), 1.6f);
        var czerwony = BraxCityMaterials.Surface("Warsztat: lakier czerwony", new Color(.62f, .08f, .06f), .4f, .3f);
        var niebieski = BraxCityMaterials.Surface("Warsztat: regał stal niebieska", new Color(.12f, .22f, .45f), .45f, .5f);
        var ocynk = BraxCityMaterials.Surface("Warsztat: blat stal ocynk", new Color(.55f, .57f, .58f), .45f, .7f);
        var czarny = BraxCityMaterials.Surface("Warsztat: czarny mat (gumy, uchwyty)", new Color(.05f, .05f, .05f), .85f);
        // posadzka: korpus 3 cm pod żywicą + żywica i linie BHP w wycięciach (styk krawędzią)
        var korpus = new BraxRegionMeble.Bud(); korpus.MinMax(new Vector3(x0, yp - .33f, z0), new Vector3(x1, yp - .03f, z1 + .35f));
        korpus.Obiekt("Warsztat " + n + ": posadzka (korpus)", w, BraxRegionMeble.Beton, false, false);
        var stan = i == 1 ? Rect.MinMaxRect(m - 2.3f * K, zc - 3.4f * K, m + 2.3f * K, zc + 2.6f * K) : Rect.MinMaxRect(m - 1.5f, zc - 2.6f * K, m + 2.9f, zc + 2.6f * K);
        var linie = new List<Rect> { Rect.MinMaxRect(stan.xMin, stan.yMin, stan.xMax, stan.yMin + .1f), Rect.MinMaxRect(stan.xMin, stan.yMax - .1f, stan.xMax, stan.yMax),
                                     Rect.MinMaxRect(stan.xMin, stan.yMin + .1f, stan.xMin + .1f, stan.yMax - .1f), Rect.MinMaxRect(stan.xMax - .1f, stan.yMin + .1f, stan.xMax, stan.yMax - .1f) };
        var pz = new BraxRegionMeble.Bud(); var pb = new BraxRegionMeble.Bud();
        var xsS = new SortedSet<float> { x0, x1 }; var zsS = new SortedSet<float> { z0, z1 + .35f };
        foreach (var l in linie) { xsS.Add(l.xMin); xsS.Add(l.xMax); zsS.Add(l.yMin); zsS.Add(l.yMax); }
        var XS = xsS.ToList(); var ZS = zsS.ToList();
        for (int a = 0; a + 1 < XS.Count; a++)
            for (int b = 0; b + 1 < ZS.Count; b++)
            {
                var c = new Vector2((XS[a] + XS[a + 1]) * .5f, (ZS[b] + ZS[b + 1]) * .5f);
                var bud = linie.Any(l => l.Contains(c)) ? pb : pz;
                bud.Quad(new Vector3(XS[a], yp, ZS[b]), new Vector3(XS[a], yp, ZS[b + 1]), new Vector3(XS[a + 1], yp, ZS[b + 1]), new Vector3(XS[a + 1], yp, ZS[b]));
            }
        pz.Obiekt("Warsztat " + n + ": posadzka (żywica epoksydowa)", w, zywica, false, false);
        pb.Obiekt("Warsztat " + n + ": linie BHP (w wycięciach)", w, bhp, false, false);
        // ściany: tynk 2 cm przed murem, lamperia 2 cm przed tynkiem do 1.4 m × k; sufit 3 cm pod stropem
        // otwory ściany tylnej (drzwi zaplecza, 3 okna górne) i szczytowej zachodniej lokalu 0 (okna) — w tynku i lamperii te same
        float ua = WarX0 + i * WarModul, udw = 1.0f * K, udh = 2.2f * K, zbM = WarZb;
        var otwT = new List<Rect> { Rect.MinMaxRect(ua + 1.0f, YC, ua + 1.0f + udw, YC + udh) };
        foreach (float s in new[] { 3.4f, 5.7f, 8.0f }) otwT.Add(Rect.MinMaxRect(ua + s - .7f, YC + 3.55f, ua + s + .7f, YC + 4.95f));
        var otwB = new List<Rect>();
        if (i == 0) foreach (float zcO in new[] { zbM + 3.6f, zbM + 6.75f, zbM + 9.9f }) { otwB.Add(Rect.MinMaxRect(zcO - .8f, YC + 1.0f, zcO + .8f, YC + 2.9f)); otwB.Add(Rect.MinMaxRect(zcO - .8f, YC + 3.45f, zcO + .8f, YC + 5.0f)); }
        var tk = new BraxRegionMeble.Bud(); var lm = new BraxRegionMeble.Bud();
        float yl = yp + 1.4f * K;
        MurZOtworami(tk, true, z0, z0 + .02f, x0, x1, yp, ys - .03f, otwT);
        MurZOtworami(tk, false, x0, x0 + .02f, z0 + .02f, z1, yp, ys - .03f, otwB);
        tk.MinMax(new Vector3(x1 - .02f, yp, z0 + .02f), new Vector3(x1, ys - .03f, z1));
        tk.MinMax(new Vector3(x0, ys - .06f, z0), new Vector3(x1, ys - .03f, z1));
        MurZOtworami(lm, true, z0 + .02f, z0 + .04f, x0 + .02f, x1 - .02f, yp, yl, otwT);
        MurZOtworami(lm, false, x0 + .02f, x0 + .04f, z0 + .04f, z1, yp, yl, otwB);
        lm.MinMax(new Vector3(x1 - .04f, yp, z0 + .04f), new Vector3(x1 - .02f, yl, z1));
        tk.Obiekt("Warsztat " + n + ": tynk ścian i sufit", w, tynk, false, false);
        lm.Obiekt("Warsztat " + n + ": lamperia", w, lamperia, false, false);
        // oprawy LED liniowe 2 × 3 (świecą bez świateł — każdy telefon) + ciepłe światło
        var op = new BraxRegionMeble.Bud(); var opr = new BraxRegionMeble.Bud();
        foreach (float dx in new[] { -2.2f, 2.2f })
            foreach (float dzz in new[] { -4.0f, 0f, 4.0f })
            {
                var c = new Vector3(m + dx, ys - .16f, zc + dzz);
                opr.MinMax(c + new Vector3(-.09f, 0f, -.7f * K), c + new Vector3(.09f, .1f, .7f * K));
                op.MinMax(c + new Vector3(-.07f, -.02f, -.68f * K), c + new Vector3(.07f, 0f, .68f * K));
            }
        opr.Obiekt("Warsztat " + n + ": oprawy (obudowy)", w, BraxRegionMeble.StalSzara, false, false);
        op.Obiekt("Warsztat " + n + ": oprawy LED (świecą)", w, led, false, false);
        foreach (float dx in new[] { -2.2f, 2.2f }) BraxRegion.Warm(w, "Warsztat " + n + ": światło", new Vector3(m + dx, ys - .6f, zc), 1.8f, 10f);
        // wyposażenie wspólne: kompresor (zbiornik poziomy, silnik, osłona), zwijadło węża
        var kr = new BraxRegionMeble.Bud(); var ks = new BraxRegionMeble.Bud();
        var kp = new Vector3(x1 - .7f, yp, z0 + .6f);
        kr.Cyl(kp + new Vector3(-.5f * K, .38f * K, 0f), kp + new Vector3(.5f * K, .38f * K, 0f), .26f * K, 16);
        ks.MinMax(kp + new Vector3(-.2f, .64f * K, -.18f), kp + new Vector3(.2f, .9f * K, .18f));
        foreach (float sx in new[] { -.35f, .35f }) ks.MinMax(kp + new Vector3(sx * K - .04f, 0f, -.2f), kp + new Vector3(sx * K + .04f, .14f * K, .2f));
        ks.Cyl(new Vector3(x1 - .03f, yp + 1.7f * K, z0 + 2.2f), new Vector3(x1 - .25f, yp + 1.7f * K, z0 + 2.2f), .22f, 16);
        kr.Obiekt("Warsztat " + n + ": kompresor (zbiornik)", w, czerwony, false, true);
        ks.Obiekt("Warsztat " + n + ": kompresor, zwijadło węża", w, czarny, false, true);
        BraxRegionMeble.Kolizja(w, "Kompresor kolizja", kp + new Vector3(0f, .45f * K, 0f), new Vector3(1.1f * K, .9f * K, .6f * K));
        if (i == 1)
        {
            // AUTO NAPRAWA: podnośnik z PODNIESIONYM autem, stół z tablicą narzędzi, szafka, wózek, beczki, regał z kołami
            float hp = 1.55f;
            Podnosnik(w, new Vector3(m, yp, zc - .2f), hp);
            AutaWarsztatow.Add((new Vector3(m, yp + hp + .1f - .2f, zc - .2f), Vector3.back, hp, "AUTO NAPRAWA — na podnośniku"));
            float sx0 = x0 + 2.6f;
            var stol = new BraxRegionMeble.Bud();
            stol.MinMax(new Vector3(sx0, yp + .9f * K, z0 + .05f), new Vector3(sx0 + 2.2f * K, yp + .95f * K, z0 + .05f + .75f * K));
            foreach (float dx in new[] { .05f, 2.1f }) foreach (float dzz in new[] { .05f, .68f }) stol.MinMax(new Vector3(sx0 + dx * K, yp, z0 + .05f + dzz * K), new Vector3(sx0 + (dx + .06f) * K, yp + .9f * K, z0 + .05f + (dzz + .06f) * K));
            stol.Obiekt("Warsztat AUTO NAPRAWA: stół warsztatowy", w, ocynk, false, true);
            BraxRegionMeble.Kolizja(w, "Stół kolizja", new Vector3(sx0 + 1.1f * K, yp + .48f * K, z0 + .05f + .38f * K), new Vector3(2.2f * K, .95f * K, .75f * K));
            var tab = new BraxRegionMeble.Bud(); var nar = new BraxRegionMeble.Bud();
            tab.MinMax(new Vector3(sx0, yp + 1.15f * K, z0 + .04f), new Vector3(sx0 + 2.2f * K, yp + 2.0f * K, z0 + .06f));
            for (int k = 0; k < 9; k++) { float tx = sx0 + (.2f + k * .23f) * K, th = (.18f + (k % 3) * .07f) * K; nar.MinMax(new Vector3(tx - .02f, yp + 1.75f * K - th, z0 + .06f), new Vector3(tx + .03f, yp + 1.75f * K, z0 + .085f)); }
            for (int k = 0; k < 6; k++) { float tx = sx0 + (.3f + k * .32f) * K; nar.MinMax(new Vector3(tx - .06f, yp + 1.3f * K, z0 + .06f), new Vector3(tx + .06f, yp + 1.36f * K, z0 + .085f)); }
            tab.Obiekt("Warsztat AUTO NAPRAWA: tablica narzędziowa", w, niebieski, false, false);
            nar.Obiekt("Warsztat AUTO NAPRAWA: narzędzia na tablicy", w, BraxRegionMeble.StalSzara, false, false);
            MechanikAutoNaprawa = new Vector3(sx0 + 1.1f * K, yp, z0 + 1.6f);
            var sf = new BraxRegionMeble.Bud();
            sf.MinMax(new Vector3(x1 - 2.2f - .7f * K, yp, z0 + .05f), new Vector3(x1 - 2.2f, yp + 1.8f * K, z0 + .05f + .5f * K));
            for (int k = 1; k < 6; k++) sf.MinMax(new Vector3(x1 - 2.2f - .66f * K, yp + k * .3f * K, z0 + .05f + .5f * K), new Vector3(x1 - 2.2f - .04f * K, yp + k * .3f * K + .02f, z0 + .07f + .5f * K));
            sf.MinMax(new Vector3(m + 2.6f, yp + .1f, zc + 3.2f), new Vector3(m + 2.6f + .8f * K, yp + .95f * K, zc + 3.2f + .5f * K));   // wózek narzędziowy
            sf.Obiekt("Warsztat AUTO NAPRAWA: szafka i wózek narzędziowy (czerwone)", w, czerwony, false, true);
            BraxRegionMeble.Kolizja(w, "Szafka kolizja", new Vector3(x1 - 2.2f - .35f * K, yp + .9f * K, z0 + .05f + .25f * K), new Vector3(.7f * K, 1.8f * K, .5f * K));
            var bk = new BraxRegionMeble.Bud();
            bk.Cyl(new Vector3(x0 + .55f, yp, z1 - 3.0f), new Vector3(x0 + .55f, yp + .88f * K, z1 - 3.0f), .29f * K, 16);
            bk.Cyl(new Vector3(x0 + .55f, yp, z1 - 3.75f), new Vector3(x0 + .55f, yp + .88f * K, z1 - 3.75f), .29f * K, 16);
            bk.Obiekt("Warsztat AUTO NAPRAWA: beczki oleju", w, niebieski, false, true);
            RegalKol(w, niebieski, x1 - .05f, -1f, z1 - 4.6f, z1 - 1.0f, yp, 2, 4, "AUTO NAPRAWA — regał z kołami");
            world_point("R1_WARSZTAT_AUTO_NAPRAWA", "AUTO NAPRAWA (otwarty warsztat)", new Vector3(m, yp, z1 - 2f));
        }
        else
        {
            // WULKANIZACJA: montażownica i wyważarka z kołami, regał z kołami, stosy opon, podnośnik „żaba”, auto klienta
            var mo = new BraxRegionMeble.Bud(); var ms = new BraxRegionMeble.Bud();
            var pm = new Vector3(x0 + 1.2f, yp, zc + 2.4f);   // montażownica (bok przy oknie)
            mo.MinMax(pm + new Vector3(-.45f * K, 0f, -.4f * K), pm + new Vector3(.45f * K, .82f * K, .4f * K));
            ms.Cyl(pm + new Vector3(0f, .82f * K, 0f), pm + new Vector3(0f, .88f * K, 0f), .34f * K, 20);
            ms.MinMax(pm + new Vector3(-.08f * K, .82f * K, -.5f * K), pm + new Vector3(.08f * K, 1.75f * K, -.36f * K));
            ms.MinMax(pm + new Vector3(-.06f * K, 1.62f * K, -.5f * K), pm + new Vector3(.06f * K, 1.72f * K, .05f * K));
            ms.MinMax(pm + new Vector3(-.04f * K, 1.25f * K, -.04f * K), pm + new Vector3(.04f * K, 1.68f * K, .04f * K));
            KolaWarsztatow.Add((pm + new Vector3(0f, .88f * K, 0f), Vector3.up, 1, "montażownica"));
            var pw = new Vector3(x0 + 1.0f, yp, zc - 1.6f);   // wyważarka: korpus, wałek, osłona, wyświetlacz
            mo.MinMax(pw + new Vector3(-.35f * K, 0f, -.45f * K), pw + new Vector3(.35f * K, 1.0f * K, .45f * K));
            ms.Cyl(pw + new Vector3(.35f * K, .75f * K, 0f), pw + new Vector3(.75f * K, .75f * K, 0f), .03f * K, 10);
            ms.MinMax(pw + new Vector3(-.3f * K, 1.0f * K, -.2f * K), pw + new Vector3(.3f * K, 1.32f * K, .2f * K));
            KolaWarsztatow.Add((pw + new Vector3(.62f * K, .75f * K, 0f) - Vector3.up * .33f, Vector3.right, 1, "wyważarka"));
            mo.Obiekt("Warsztat WULKANIZACJA: montażownica i wyważarka (korpusy)", w, czerwony, false, true);
            ms.Obiekt("Warsztat WULKANIZACJA: montażownica i wyważarka (stal)", w, czarny, false, true);
            BraxRegionMeble.Kolizja(w, "Montażownica kolizja", pm + new Vector3(0f, .5f * K, 0f), new Vector3(.9f * K, 1.0f * K, .9f * K));
            BraxRegionMeble.Kolizja(w, "Wyważarka kolizja", pw + new Vector3(.1f, .5f * K, 0f), new Vector3(.9f * K, 1.0f * K, .9f * K));
            var lc = BraxRegionMeble.Swiecacy("Warsztat: wyświetlacz wyważarki", new Color(.2f, .6f, 1f), .9f);
            var ekr = new BraxRegionMeble.Bud(); ekr.MinMax(pw + new Vector3(-.36f * K, .8f * K, -.18f * K), pw + new Vector3(-.35f * K, .95f * K, .18f * K)); ekr.Obiekt("Warsztat WULKANIZACJA: wyświetlacz", w, lc, false, false);
            MechanikWulkanizacja = pm + new Vector3(.95f, 0f, 0f);
            RegalKol(w, niebieski, x0 + .05f, 1f, z0 + .3f, z0 + 3.9f, yp, 3, 4, "WULKANIZACJA — regał z kołami");
            foreach (var (sxp, szp, ile) in new[] { (x1 - 1.0f, z1 - 1.0f, 5), (x1 - 1.0f, z1 - 1.8f, 4), (x1 - 1.8f, z1 - 1.0f, 3) })
                KolaWarsztatow.Add((new Vector3(sxp, yp, szp), Vector3.up, ile, "WULKANIZACJA — stos opon"));
            // podnośnik „żaba” (lewarek warsztatowy na kółkach) przy aucie
            var zb2 = new BraxRegionMeble.Bud();
            var pj = new Vector3(m + 1.9f, yp, zc + .2f);
            zb2.MinMax(pj + new Vector3(-.15f, .05f, -.6f), pj + new Vector3(.15f, .2f, .5f));
            zb2.Cyl(pj + new Vector3(0f, .2f, .45f), pj + new Vector3(0f, .9f, 1.1f), .025f, 8);
            zb2.Obiekt("Warsztat WULKANIZACJA: podnośnik żaba", w, czerwony, false, true);
            AutaWarsztatow.Add((new Vector3(m + .7f, yp, zc + .3f), Vector3.back, 0f, "WULKANIZACJA — auto klienta"));
            world_point("R1_WULKANIZACJA", "WULKANIZACJA (otwarta: wymiana opon)", new Vector3(m, yp, z1 - 2f));
        }
    }

    // regał z kołami przy ścianie bocznej: x lica ściany xs, strona +1 (regał w +x od ściany) / −1; półki, koła stojące (oś x)
    static void RegalKol(Transform w, Material mat, float xs, float strona, float za, float zbk, float yp, int polek, int naPolce, string nazwa)
    {
        var r = new BraxRegionMeble.Bud();
        float g = .62f * K, x0 = Mathf.Min(xs, xs + strona * g), x1 = Mathf.Max(xs, xs + strona * g);
        float hp = .78f * K;
        for (int p = 0; p < polek; p++)
        {
            float y = yp + .12f + p * hp;
            r.MinMax(new Vector3(x0, y - .04f, za), new Vector3(x1, y, zbk));
            for (int k = 0; k < naPolce; k++)
            {
                float z = Mathf.Lerp(za, zbk, (k + .5f) / naPolce);
                KolaWarsztatow.Add((new Vector3((x0 + x1) * .5f, y, z), Vector3.right, 1, nazwa));
            }
        }
        foreach (float z in new[] { za, zbk }) foreach (float x in new[] { x0, x1 - .05f }) r.MinMax(new Vector3(x, yp, z - .025f), new Vector3(x + .05f, yp + .12f + polek * hp, z + .025f));
        r.Obiekt("Warsztat: regał (" + nazwa + ")", w, mat, false, true);
        BraxRegionMeble.Kolizja(w, "Regał kolizja", new Vector3((x0 + x1) * .5f, yp + (polek * hp) * .5f, (za + zbk) * .5f), new Vector3(x1 - x0, polek * hp + .12f, Mathf.Abs(zbk - za)));
    }

    // podnośnik dwukolumnowy 4 t: kolumny 3.0 m × k, wózki z ramionami podniesione na wysokość hp (auto w górze), belka górna
    static void Podnosnik(Transform w, Vector3 p, float hp)
    {
        var b = new BraxRegionMeble.Bud(); var r = new BraxRegionMeble.Bud();
        foreach (float s in new[] { -1f, 1f })
        {
            var c = p + new Vector3(s * 1.55f * K, 0, 0);
            b.MinMax(c + new Vector3(-.2f * K, 0, -.25f * K), c + new Vector3(.2f * K, 3.0f * K, .25f * K));
            b.MinMax(c + new Vector3(-.3f * K, 0, -.35f * K), c + new Vector3(.3f * K, .05f, .35f * K));
            r.MinMax(c + new Vector3(-s * .2f * K - s * .12f, hp - .1f, -.2f * K), c + new Vector3(-s * .2f * K, hp + .35f, .2f * K));   // wózek
            foreach (float dz in new[] { -1f, 1f }) r.Box(c + new Vector3(-s * .65f * K, hp, dz * .5f * K), new Vector3(1.0f * K, .08f * K, .12f * K), Quaternion.Euler(0, dz * s * 22f, 0));
            BraxRegionMeble.Kolizja(w, "Podnośnik kolumna", c + Vector3.up * 1.5f * K, new Vector3(.4f * K, 3f * K, .5f * K));
        }
        b.MinMax(p + new Vector3(-1.75f * K, 3.0f * K, -.12f * K), p + new Vector3(1.75f * K, 3.12f * K, .12f * K));
        b.Obiekt("Podnośnik: kolumny (niebieski lakier)", w, BraxCityMaterials.Surface("Podnośnik: lakier niebieski", new Color(.1f, .25f, .55f), .4f, .4f), false, true);
        r.Obiekt("Podnośnik: wózki i ramiona", w, BraxCityMaterials.Surface("Podnośnik: ramiona stal", new Color(.25f, .26f, .27f), .45f, .6f), false, true);
    }

    // PLAC USTAWEK 3 na 3: x −104…−78, z −13.25…2.5 (≥ 20 × 12 × k), asfalt z liniami obwiedni w wycięciach, ławki kibiców
    static void PlacUstawek()
    {
        float px0 = -104f, px1 = -78f, pz0 = SZ + SH + Kr + 2.5f, pz1 = 2.5f;
        var linie = new List<Rect> { Rect.MinMaxRect(px0 + 1f, pz0 + 1f, px1 - 1f, pz0 + 1.08f), Rect.MinMaxRect(px0 + 1f, pz1 - 1.08f, px1 - 1f, pz1 - 1f),
                                     Rect.MinMaxRect(px0 + 1f, pz0 + 1f, px0 + 1.08f, pz1 - 1f), Rect.MinMaxRect(px1 - 1.08f, pz0 + 1f, px1 - 1f, pz1 - 1f) };
        var pt = new BraxRegionMeble.Bud(); var pl = new BraxRegionMeble.Bud();
        Wstawki("Plac ustawek", Rect.MinMaxRect(px0, pz0, px1, pz1), YC, linie, asfalt, farba, pt, pl, .3f - YA);
        pt.Obiekt("Plac ustawek: nawierzchnia (asfalt)", root, asfalt, false, false);
        pl.Obiekt("Plac ustawek: linie (farba w wycięciach)", root, farba, false, false);
        for (float x = px0 + 3f; x < px1 - 2f; x += 6.5f) { float xx = x; Pozniej("ławka kibiców", () => BraxRegionMeble.Lawka(root, NaPodlozu(new Vector3(xx, YC, pz1 + .1f + .3f * K)), 180f, "Ławka kibiców")); }
        world_point("R1_PLAC_USTAWEK", "plac ustawek 3 na 3", new Vector3((px0 + px1) * .5f, YC, (pz0 + pz1) * .5f));
    }
}

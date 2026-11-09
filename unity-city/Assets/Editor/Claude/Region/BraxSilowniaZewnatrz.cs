using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// SIŁOWNIA Z ZEWNĄTRZ I PODWÓRZE ZA KAMIENICAMI (9.11, właściciel ze zdjęciami: „cały teren za siłownią, ten budynek
// z tyłu”; „z siłowni w lewo, potem znowu w lewo — za budynkiem jest ta woda, i cały budynek prześwietl”).
// POMIAR (oryginalny model braxhorn-original-gym.glb, rzut i elewacje 9.11): siłownia to sala x −18.6…18.6, z −16.6…11.6,
// posadzka y 0, strop od środka y 8.5 (z tyłu, z −16.6…−13.6, niższy strop 6.5). Model był robiony TYLKO OD ŚRODKA:
//  • ściany zachodnia i wschodnia z zewnątrz = goła kremowa płaszczyzna z odbitymi śladami wypalonego światła wnętrza
//    („biały klocek”), u podstawy czarny fundament (pod spodem nie było gruntu — „czarny otwór”);
//  • ściana południowa = 4 ciemnoszare pola między kremowymi lizenami;
//  • DACHU NIE MA — strop jest tylko od spodu; z tarasu Tower widać wnętrze sali przez kratownice.
// Podwórze za kamienicami pierzei płd. (x −61.45…−18.6, z −6…11.6), przejście na wschód od siłowni (x 18.6…26) i pas za
// APTEKĄ i DELIKATESAMI (z −6…−4) NIE MIAŁY ŻADNEJ NAWIERZCHNI — oryginał miasta kończył się za pierzeją, a plac wieżowców
// (tyły B) zaczyna się dopiero od z −6. Gracz chodził po niewidzialnej podłodze y 0, a widać było wodę miasta y −1.7.
// POŁOŻONE (wnętrze siłowni bez zmian — wszystko jest NA ZEWNĄTRZ jej ścian i NAD jej stropem):
//  1) podwórze i przejścia: płyty granitowe placu wieżowców (ta sama faktura i poziom 0.15, styk krawędzią z placem z −6),
//     kolizja; drzewa w kratach, latarnie, ławki, kosze;
//  2) elewacja siłowni (zachód, południe, wschód) 3 cm przed ścianą: cegła klinkierowa grafitowo-brązowa, cokół z granitu,
//     lizeny w osiach słupów sali, okna przemysłowe w stalowych ramach z parapetami, gzyms stalowy, attyka z czapą,
//     napis BRAXHORN GYM (od placu wieżowców i od ul. Wiaduktowej), lampy nad oknami, rynny i rury spustowe, wyjście
//     ewakuacyjne (drzwi stalowe, daszek, lampa, zielony znak);
//  3) dach płaski nad stropem (od dołu niewidoczny — strop sali go zasłania): membrana, attyka, 3 pasma świetlików,
//     2 agregaty wentylacji, wywiewki, wyłaz dachowy — z tarasu Tower widać porządny dach zamiast wnętrza sali.
// Log BRAXHORN_SILOWNIA_ZEWNATRZ: co położone, ile okien, powierzchnia podwórza.
public static class BraxSilowniaZewnatrz
{
    public const float X0 = -18.6f, X1 = 18.6f, Z0 = -16.6f, Z1 = 11.6f;      // obrys sali (lica ścian zewnętrznych)
    public const float YC = .15f, Wys = 8.5f;
    public static readonly Rect[] Podworze =
    {
        Rect.MinMaxRect(-61.45f, -6f, X0, Z1),     // podwórze za kamienicami pierzei płd. (od ul. Wiaduktowej do siłowni)
        Rect.MinMaxRect(X1, -6f, 26f, Z1),         // przejście na wschód od siłowni (do ściany DELIKATESÓW x 26)
        Rect.MinMaxRect(26f, -6f, 52f, -4f),       // pas za DELIKATESAMI i APTEKĄ (tył budynków z −4)
    };

    static Transform root;
    static Material cegla, cokol, stal, szyba, membrana, swietlik, beton, cynk, lampa, zielony, agregat, granit;
    static int okien, lamp;

    public static void Apply(Transform cityRoot)
    {
        okien = lamp = 0;
        root = BraxRegion.Root(cityRoot, "CLAUDE siłownia z zewnątrz: elewacje, dach, podwórze za kamienicami");
        Materialy();
        Krok("podwórze i przejścia (płyty granitowe, kolizja)", PodworzeNawierzchnia);
        Krok("elewacja zachodnia (od ul. Wiaduktowej)", () => Elewacja("zachód", new Vector3(X0, 0f, Z0), Vector3.forward, Vector3.left, Z1 - Z0,
            new[] { 7.6f, 13.6f, 19.6f, 25.6f }, "BRAXHORN GYM", 16.6f, -1f));
        Krok("elewacja południowa (od placu wieżowców)", () => Elewacja("południe", new Vector3(X1, 0f, Z0), Vector3.left, Vector3.back, X1 - X0,
            new[] { 9.6f, 18.6f, 27.6f }, "BRAXHORN GYM", 23.1f, 14.1f));
        Krok("elewacja wschodnia (przejście przy DELIKATESACH)", () => Elewacja("wschód", new Vector3(X1, 0f, Z1), Vector3.back, Vector3.right, Z1 - Z0,
            new[] { 2.6f, 8.6f, 14.6f, 20.6f }, null, 0f, -1f));
        Krok("narożniki, gzyms narożny", Narozniki);
        Krok("dach: membrana, attyka, świetliki, wentylacja", Dach);
        Krok("podwórze: drzewa w kratach, latarnie, ławki, kosze", Wyposazenie);
        Debug.Log("BRAXHORN_SILOWNIA_ZEWNATRZ elewacje 3 (cegła klinkierowa, cokół, lizeny, okna " + okien + ", lampy " + lamp + ", rynny), dach nad stropem (membrana, świetliki, agregaty)," +
                  " podwórze " + Pole().ToString("0") + " m² (płyty granitowe y " + YC + "), wnętrze sali bez zmian");
    }

    static void Krok(string n, Action a)
    {
        try { a(); BraxRegion.Note("siłownia z zewnątrz: " + n + ": OK"); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_SILOWNIA_ZEWNATRZ " + n + ": " + e); }
    }

    static float Pole() { float p = 0f; foreach (var r in Podworze) p += r.width * r.height; return p; }

    static void Materialy()
    {
        cegla = BraxRegionMeble.Tex("Siłownia z zewnątrz: cegła klinkierowa (grafit-brąz)", 1.0f, new Color(.52f, .44f, .41f), .85f, "red_brick_03", "brick_wall_001", "brick_wall_10");
        cokol = BraxRegionMeble.Tex("Siłownia z zewnątrz: cokół granit grafit", 1.2f, new Color(.33f, .33f, .34f), .7f, "granite_tile_02", "rough_block_wall");
        stal = BraxCityMaterials.Surface("Siłownia z zewnątrz: stal grafit (ramy, gzyms, czapa)", new Color(.10f, .11f, .12f), .45f, .6f);
        szyba = BraxCityMaterials.Surface("Siłownia z zewnątrz: szyba okien przemysłowych", new Color(.16f, .20f, .23f), .06f, .4f);
        membrana = BraxCityMaterials.Surface("Siłownia z zewnątrz: dach — membrana PVC grafit", new Color(.25f, .26f, .27f), .85f);
        swietlik = BraxCityMaterials.Surface("Siłownia z zewnątrz: świetlik poliwęglan", new Color(.72f, .77f, .80f), .25f, .1f);
        beton = BraxRegionMeble.Beton;
        cynk = BraxCityMaterials.Surface("Siłownia z zewnątrz: rynny tytan-cynk", new Color(.50f, .52f, .53f), .45f, .7f);
        lampa = BraxRegionMeble.Swiecacy("Siłownia z zewnątrz: lampa elewacyjna (ciepła)", new Color(1f, .88f, .70f), 1.4f);
        zielony = BraxRegionMeble.Swiecacy("Siłownia z zewnątrz: znak wyjścia (zielony)", new Color(.05f, .75f, .25f), .9f);
        agregat = BraxCityMaterials.Surface("Siłownia z zewnątrz: agregat wentylacji (ocynk lakier)", new Color(.76f, .77f, .78f), .5f, .4f);
        granit = BraxRegionMeble.Tex("Tyły B: płyty granitowe placu", 1.2f, new Color(.70f, .69f, .67f), .85f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
    }

    // ---------------------------------------------------------------------------------------------------------------
    static void PodworzeNawierzchnia()
    {
        var b = new BraxRegionMeble.Bud();
        foreach (var r in Podworze)
        {
            b.MinMax(new Vector3(r.xMin, YC - .3f, r.yMin), new Vector3(r.xMax, YC, r.yMax));
            BraxRegionMeble.Kolizja(root, "Podwórze: kolizja nawierzchni", new Vector3(r.center.x, YC - .15f, r.center.y), new Vector3(r.width, .3f, r.height));
        }
        var g = b.Obiekt("Podwórze za kamienicami: płyty granitowe (jak plac wieżowców)", root, granit, false, false);
        if (g) g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // elewacja ściany: A = narożnik startowy na licu ściany sali, R = kierunek wzdłuż ściany, N = na zewnątrz, L = długość;
    // lizeny (s osi), napis (s środka; null = bez), drzwi ewakuacyjne (s środka; < 0 = bez)
    static void Elewacja(string nazwa, Vector3 A, Vector3 R, Vector3 N, float L, float[] lizeny, string napis, float sNapisu, float sDrzwi)
    {
        Vector3 P(float s, float y, float d) => A + R * s + Vector3.up * y + N * d;
        var sc = new BraxRegionMeble.Bud(); var ck = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud(); var sz = new BraxRegionMeble.Bud();
        var bt = new BraxRegionMeble.Bud(); var cz = new BraxRegionMeble.Bud(); var lp = new BraxRegionMeble.Bud(); var zn = new BraxRegionMeble.Bud();
        const float y0 = .05f, yC = .75f, yG = 8.1f, yGz = 8.32f, yA = 9.0f;   // dół, cokół, gzyms, attyka
        // przęsła między lizenami (narożnik zajmuje s 0…0.4 i L−0.4…L) → okno w środku przęsła
        var granice = new List<float> { .4f }; foreach (var s in lizeny) { granice.Add(s - .35f); granice.Add(s + .35f); } granice.Add(L - .4f);
        var otwory = new List<Rect>();
        for (int i = 0; i + 1 < granice.Count; i += 2)
        {
            float a = granice[i], b = granice[i + 1], szer = b - a;
            if (szer < 3.2f) continue;
            float w = Mathf.Min(4.0f, szer - 1.6f), m = (a + b) * .5f;
            bool drzwi = sDrzwi >= 0f && sDrzwi > a && sDrzwi < b;
            float yd = drzwi ? 3.4f : 2.6f;
            otwory.Add(Rect.MinMaxRect(m - w * .5f, yd, m + w * .5f, 7.2f));
            if (drzwi) otwory.Add(Rect.MinMaxRect(sDrzwi - 1.0f, YC, sDrzwi + 1.0f, 2.55f));
        }
        // mur licowy z otworami (8 cm, 3 cm przed licem sali) — do attyki
        Sciana(sc, P, 0f, L, y0, yA, .03f, .11f, otwory);
        // cokół (granit, wystaje 4 cm) bez otworu drzwi
        var otworyCokolu = otwory.FindAll(o => o.yMin < yC);
        Sciana(ck, P, 0f, L, y0, yC, .11f, .15f, otworyCokolu);
        // lizeny 0.7 × 0.15 (cegła) od cokołu do gzymsu
        foreach (var s in lizeny) sc.MinMax(P(s - .35f, yC, .1f), P(s + .35f, yG, .26f));
        // gzyms stalowy i czapa attyki
        st.MinMax(P(0f, yG, .1f), P(L, yGz, .32f));
        st.MinMax(P(0f, yA, -.02f), P(L, yA + .07f, .17f));
        // okna: rama zewnętrzna, słupki i ślemię (siatka 3 × 3), szyba w głębi, parapet; nad oknem lampa
        foreach (var o in otwory)
        {
            bool drzwiOtw = o.yMin < 1f;
            if (drzwiOtw) continue;
            sz.MinMax(P(o.xMin, o.yMin, .04f), P(o.xMax, o.yMax, .05f));
            float f = .08f;
            st.MinMax(P(o.xMin, o.yMin, .05f), P(o.xMin + f, o.yMax, .09f)); st.MinMax(P(o.xMax - f, o.yMin, .05f), P(o.xMax, o.yMax, .09f));
            st.MinMax(P(o.xMin + f, o.yMin, .05f), P(o.xMax - f, o.yMin + f, .09f)); st.MinMax(P(o.xMin + f, o.yMax - f, .05f), P(o.xMax - f, o.yMax, .09f));
            for (int k = 1; k < 3; k++)
            {
                float sx = Mathf.Lerp(o.xMin, o.xMax, k / 3f), sy = Mathf.Lerp(o.yMin, o.yMax, k / 3f);
                st.MinMax(P(sx - .03f, o.yMin + f, .055f), P(sx + .03f, o.yMax - f, .085f));
                st.MinMax(P(o.xMin + f, sy - .03f, .055f), P(o.xMax - f, sy + .03f, .085f));
            }
            bt.MinMax(P(o.xMin - .06f, o.yMin - .08f, .04f), P(o.xMax + .06f, o.yMin, .19f));
            okien++;
            float lm = (o.xMin + o.xMax) * .5f;
            st.MinMax(P(lm - .03f, 7.42f, .11f), P(lm + .03f, 7.48f, .38f));
            st.MinMax(P(lm - .16f, 7.36f, .30f), P(lm + .16f, 7.44f, .46f));
            lp.MinMax(P(lm - .12f, 7.33f, .32f), P(lm + .12f, 7.36f, .44f));
            lamp++;
        }
        // wyjście ewakuacyjne: rama, dwa skrzydła stalowe z dźwignią antypaniczną, daszek na cięgnach, lampa, zielony znak
        if (sDrzwi >= 0f)
        {
            float d0 = sDrzwi - 1.0f, d1 = sDrzwi + 1.0f;
            st.MinMax(P(d0 - .12f, YC, .03f), P(d0, 2.67f, .13f)); st.MinMax(P(d1, YC, .03f), P(d1 + .12f, 2.67f, .13f)); st.MinMax(P(d0, 2.55f, .03f), P(d1, 2.67f, .13f));
            var drz = new BraxRegionMeble.Bud();
            drz.MinMax(P(d0, YC, .05f), P(sDrzwi - .01f, 2.55f, .08f)); drz.MinMax(P(sDrzwi + .01f, YC, .05f), P(d1, 2.55f, .08f));
            drz.Obiekt("Siłownia " + nazwa + ": wyjście ewakuacyjne — skrzydła", root, BraxCityMaterials.Surface("Siłownia z zewnątrz: drzwi stalowe (RAL grafit)", new Color(.17f, .19f, .21f), .5f, .4f), false, true);
            st.MinMax(P(d0 + .15f, 1.0f, .08f), P(d1 - .15f, 1.06f, .13f));
            st.MinMax(P(d0 - .3f, 2.85f, .11f), P(d1 + .3f, 2.95f, 1.35f));
            st.Cyl(P(d0 - .2f, 3.9f, .12f), P(d0 - .2f, 2.95f, 1.25f), .015f, 6); st.Cyl(P(d1 + .2f, 3.9f, .12f), P(d1 + .2f, 2.95f, 1.25f), .015f, 6);
            lp.MinMax(P(sDrzwi - .25f, 2.81f, .5f), P(sDrzwi + .25f, 2.85f, .8f)); lamp++;
            zn.MinMax(P(sDrzwi - .45f, 3.0f, .11f), P(sDrzwi + .45f, 3.25f, .15f));
            BraxRegionMeble.Napis(root, "WYJŚCIE", P(sDrzwi, 3.125f, .16f), N, .12f, Color.white);
            BraxRegionMeble.Kolizja(root, "Siłownia " + nazwa + ": daszek kolizja", P(sDrzwi, 2.9f, .73f), new Vector3(R.x != 0f ? 2.6f : 1.24f, .1f, R.z != 0f ? 2.6f : 1.24f));
        }
        // rury spustowe przy narożnikach: lej pod gzymsem, rura z obejmami, kolano wylotowe
        foreach (float s in new[] { 1.0f, L - 1.0f })
        {
            cz.MinMax(P(s - .14f, 7.7f, .11f), P(s + .14f, 8.05f, .34f));
            cz.Cyl(P(s, 7.7f, .22f), P(s, .45f, .22f), .055f, 10);
            cz.Cyl(P(s, .45f, .22f), P(s, .22f, .42f), .055f, 10);
            for (float y = 1.5f; y < 7.5f; y += 2f) cz.MinMax(P(s - .07f, y, .11f), P(s + .07f, y + .04f, .26f));
        }
        // napis z liter przestrzennych w pasie nad oknami (przed lizenami)
        if (!string.IsNullOrEmpty(napis)) BraxRegionMeble.Napis(root, napis, P(sNapisu, 7.72f, .3f), N, .62f, new Color(.95f, .94f, .90f));
        sc.Obiekt("Siłownia " + nazwa + ": mur licowy (cegła klinkierowa)", root, cegla, false, true);
        ck.Obiekt("Siłownia " + nazwa + ": cokół (granit)", root, cokol, false, true);
        st.Obiekt("Siłownia " + nazwa + ": stal (ramy okien, gzyms, czapa attyki, lampy)", root, stal, false, true);
        sz.Obiekt("Siłownia " + nazwa + ": szyby okien", root, szyba, false, false);
        bt.Obiekt("Siłownia " + nazwa + ": parapety (beton)", root, beton, false, true);
        cz.Obiekt("Siłownia " + nazwa + ": rynny i rury spustowe", root, cynk, false, true);
        lp.Obiekt("Siłownia " + nazwa + ": lampy (świecą)", root, lampa, false, false);
        zn.Obiekt("Siłownia " + nazwa + ": znak wyjścia (zielony)", root, zielony, false, false);
        // kolizja okładziny (od placu — ściana sali ma własną kolizję w głębi)
        var c = P(L * .5f, 4.5f, .2f);
        BraxRegionMeble.Kolizja(root, "Siłownia " + nazwa + ": kolizja elewacji", c, new Vector3(R.x != 0f ? L : .4f, 9f, R.z != 0f ? L : .4f));
    }

    // mur z prostokątnymi otworami (s, y) → prostopadłościany przęseł między krawędziami otworów
    static void Sciana(BraxRegionMeble.Bud b, Func<float, float, float, Vector3> P, float s0, float s1, float y0, float y1, float d0, float d1, List<Rect> otwory)
    {
        var xs = new SortedSet<float> { s0, s1 };
        foreach (var o in otwory) { if (o.xMin > s0 && o.xMin < s1) xs.Add(o.xMin); if (o.xMax > s0 && o.xMax < s1) xs.Add(o.xMax); }
        var X = new List<float>(xs);
        for (int i = 0; i + 1 < X.Count; i++)
        {
            float a = X[i], c = X[i + 1]; if (c - a < .002f) continue; float m = (a + c) * .5f;
            var zajete = otwory.FindAll(o => o.xMin <= m && o.xMax >= m);
            zajete.Sort((p, q) => p.yMin.CompareTo(q.yMin));
            float y = y0;
            foreach (var o in zajete)
            {
                float oa = Mathf.Max(y0, o.yMin), ob = Mathf.Min(y1, o.yMax);
                if (ob <= oa) continue;
                if (oa - y > .002f) b.MinMax(P(a, y, d0), P(c, oa, d1));
                y = Mathf.Max(y, ob);
            }
            if (y1 - y > .002f) b.MinMax(P(a, y, d0), P(c, y1, d1));
        }
    }

    static void Narozniki()
    {
        var sc = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud(); var ck = new BraxRegionMeble.Bud();
        foreach (float sx in new[] { -1f, 1f })
        {
            float xo = sx < 0 ? X0 : X1;
            // narożnik muru (cegła) i cokołu, gzyms i czapa nad narożnikiem
            sc.MinMax(new Vector3(xo + sx * -.05f, .75f, Z0 + .05f), new Vector3(xo + sx * .45f, 8.1f, Z0 - .45f));
            sc.MinMax(new Vector3(xo + sx * -.0f, 8.32f, Z0 + .0f), new Vector3(xo + sx * .11f, 9.0f, Z0 - .11f));
            ck.MinMax(new Vector3(xo + sx * -.05f, .05f, Z0 + .05f), new Vector3(xo + sx * .49f, .75f, Z0 - .49f));
            st.MinMax(new Vector3(xo, 8.1f, Z0), new Vector3(xo + sx * .5f, 8.32f, Z0 - .5f));
            st.MinMax(new Vector3(xo, 9.0f, Z0), new Vector3(xo + sx * .17f, 9.07f, Z0 - .17f));
        }
        sc.Obiekt("Siłownia: narożniki (cegła)", root, cegla, false, true);
        ck.Obiekt("Siłownia: narożniki cokołu (granit)", root, cokol, false, true);
        st.Obiekt("Siłownia: gzyms i czapa narożników", root, stal, false, true);
    }

    // dach nad stropem sali: od dołu tylko płaszczyzna skierowana w górę (z sali niewidoczna — zasłania ją strop y 8.5)
    static void Dach()
    {
        const float y = 8.53f;
        var m = new BraxRegionMeble.Bud();
        float x0 = X0 - .03f, x1 = X1 + .03f, z0 = Z0 - .03f, z1 = Z1;
        m.Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0));
        m.Obiekt("Siłownia: dach — membrana", root, membrana, false, false);
        var kr = new BraxRegionMeble.Bud(); var gl = new BraxRegionMeble.Bud(); var ag = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud();
        // 3 pasma świetlików (krawężnik + kopuła płaska z poliwęglanu)
        foreach (float z in new[] { -8f, 0f, 7f })
        {
            kr.MinMax(new Vector3(-14f, y, z - .8f), new Vector3(14f, y + .32f, z + .8f));
            gl.MinMax(new Vector3(-13.95f, y + .32f, z - .75f), new Vector3(13.95f, y + .42f, z + .75f));
        }
        // agregaty wentylacji nad niższą częścią z tyłu sali, wywiewki, wyłaz dachowy
        foreach (float x in new[] { -10f, 10f })
        {
            ag.MinMax(new Vector3(x - 1.1f, y, -15.3f), new Vector3(x + 1.1f, y + 1.2f, -13.9f));
            st.Cyl(new Vector3(x - .5f, y + 1.2f, -14.6f), new Vector3(x - .5f, y + 1.24f, -14.6f), .42f, 18);
            st.Cyl(new Vector3(x + .55f, y + 1.2f, -14.6f), new Vector3(x + .55f, y + 1.24f, -14.6f), .42f, 18);
            st.MinMax(new Vector3(x - 1.0f, y, -13.9f), new Vector3(x - .9f, y + .1f, -12.5f));
        }
        foreach (var (x, z) in new[] { (-16f, -4f), (-16f, 4f), (16f, -4f), (16f, 4f), (-4f, 10f), (4f, 10f) })
        {
            ag.Cyl(new Vector3(x, y, z), new Vector3(x, y + .6f, z), .14f, 12);
            st.Cyl(new Vector3(x, y + .6f, z), new Vector3(x, y + .72f, z), .24f, 12, .05f);
        }
        kr.MinMax(new Vector3(-16.5f, y, 9.2f), new Vector3(-15.5f, y + .45f, 10.2f));
        kr.Obiekt("Siłownia: dach — krawężniki świetlików, wyłaz", root, membrana, false, true);
        gl.Obiekt("Siłownia: dach — świetliki (poliwęglan)", root, swietlik, false, true);
        ag.Obiekt("Siłownia: dach — agregaty i wywiewki", root, agregat, false, true);
        st.Obiekt("Siłownia: dach — wentylatory, kratki, kanały", root, stal, false, true);
    }

    static void Wyposazenie()
    {
        Physics.SyncTransforms();
        var drzewo = BraxRegion.Model("drzewo_verdant");
        foreach (var (x, z) in new[] { (-55f, 3.5f), (-44f, 3.5f), (-33f, 3.5f), (-25.5f, -1.5f) })
        {
            var p = new Vector3(x, YC, z);
            BraxRegionMeble.KrataDrzewa(root, p);
            if (drzewo)
            {
                var o = BraxRegion.Put(root, drzewo, "Podwórze: drzewo", p, (x * 31f + z * 17f) % 360f, 1f, false, 9f);
                if (o) { var c = new GameObject("Podwórze: pień kolizja").AddComponent<CapsuleCollider>(); c.transform.SetParent(root, false); c.transform.position = p + Vector3.up * 1.5f; c.radius = .3f; c.height = 3f; }
            }
        }
        foreach (var (x, z) in new[] { (-58.5f, -2.5f), (-49.5f, 8.8f), (-38.5f, -2.5f), (-27.5f, 8.8f), (22.3f, -3.5f) }) BraxRegionMeble.Latarnia(root, new Vector3(x, YC, z));
        foreach (var (x, z) in new[] { (-50f, 1.0f), (-39f, 1.0f) }) { BraxRegionMeble.Lawka(root, new Vector3(x, YC, z), 180f, "Podwórze: ławka"); BraxRegionMeble.Kosz(root, new Vector3(x + 1.6f, YC, z - .2f), 180f); }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using M = BraxWnetrzaKit.Mat;

// BRAXHORN FITNESS NYXY (pkt 59, rzut zaakceptowany 01:5x, wariant 2): skromny, typowy fitness dla kobiet w lokalu parteru
// „gallery” na płn. stronie ul. Portowej (x −10…2, hol 7.05 m w głąb z BraxCityArchitecture: z 36.1…43.01, posadzka 0.11,
// sufit 3.66), naprzeciw wyjścia z BRAXHORN GYM. Jedno wejście (oś x ≈ −5.5). Układ:
//  • przód zach.: STREFA CARDIO — 3 bieżnie przodem do witryny (model z paczki miasto_silownia_metro, bez paczki — z kodu);
//  • przód wsch.: RECEPCJA — lada biało-różowa z blatem i cokołem, napis, NPC (miejsce obsady), lodówka z napojami, roślina,
//    ławka przy witrynie; przejście 1.3 m przed ladą do szatni;
//  • tył zach.: SALA ZAJĘĆ za szklaną ścianką z drzwiami w osi wejścia — lustra na ścianie zach., maty, piłki, stepy,
//    stojak hantelek, drążek z TRX pod sufitem; MURAL na ścianie płn. (5.76 × 3.24 m, dół 0.30 m nad posadzką, wyśrodkowany)
//    — quad z materiałem „mural_fitness” (grafika billboardy/02.jpg, gdy jest w projekcie), czarna rama, 4 kinkiety;
//  • tył wsch.: SZATNIA DAMSKA — drzwi z korytarza (przed ladą) i z sali; szafki z drzwiczkami, ławka, umywalka z lustrem,
//    kabina prysznicowa.
// Kolory: biel / róż / czerń. Wymiary × K. Log BRAXHORN_FITNESS.
internal static class BraxMiastoFitness
{
    static float K => BraxMiastoA.K;
    const float X0 = -9.66f, X1 = 1.66f, ZF = 36.55f, ZT = 43.01f, Y0 = .11f, YS = 3.66f, ZP = 39.5f, XS = -1.8f;
    public const string MuralPlik = "Assets/Source/Prywatne/Grafiki/billboardy/02.jpg";

    public static void Buduj(Transform bg, Vector3 drzwi)
    {
        var g = new GameObject("BRAXHORN FITNESS (Nyxa) — wnętrze").transform; g.SetParent(bg, true); g.position = Vector3.zero;
        W.Start(g); W.Wnetrze = true; W.Komorka = 14f;
        var biel = M.Plain("fitness: biel (lakier)", new Color(.95f, .95f, .94f), .45f);
        var roz = M.Plain("fitness: róż", new Color(.93f, .45f, .62f), .5f);
        var czern = M.Plain("fitness: czerń (aluminium)", new Color(.06f, .06f, .07f), .4f, .6f);
        var stal = M.Plain("fitness: stal szczotkowana", new Color(.70f, .71f, .72f), .3f, .9f);
        var guma = M.Plain("fitness: guma", new Color(.05f, .05f, .055f), .9f);
        var szklo = M.Plain("fitness: szkło", new Color(.75f, .85f, .88f), .05f, .2f);
        var lustro = M.Plain("fitness: lustro", new Color(.82f, .86f, .88f), .02f, 1f);
        var drewno = M.Plain("fitness: dąb (ławki)", new Color(.62f, .47f, .32f), .6f);
        var zielen = M.Plain("fitness: liście rośliny", new Color(.18f, .42f, .16f), .8f);
        var led = M.Plain("fitness: oprawa LED liniowa", new Color(1f, .98f, .95f), .2f);
        int modele = 0;
        float xd = drzwi.x;

        // ---- ścianki działowe: szklana ścianka sali (z drzwiami w osi wejścia), ściany szatni (dwie pary drzwi) ----
        float dw = .9f * K, dh = 2.1f * K;
        void Scianka(float x0, float x1, bool szkl)
        {
            if (szkl)
            {
                W.MM(szklo, x0, x1, Y0, YS - .05f, ZP - .006f, ZP + .006f);
                W.MM(czern, x0, x1, Y0, Y0 + .06f, ZP - .03f, ZP + .03f, true);
                W.MM(czern, x0, x1, YS - .05f, YS, ZP - .03f, ZP + .03f);
                for (float x = x0; x < x1 - .05f; x += 1.2f) W.MM(czern, x, x + .04f, Y0 + .06f, YS - .05f, ZP - .03f, ZP + .03f);
                W.KolizjaMM(x0, x1, Y0, YS, ZP - .04f, ZP + .04f);
            }
            else { W.MM(biel, x0, x1, Y0, YS, ZP - .05f, ZP + .05f, true); W.MM(czern, x0, x1, Y0, Y0 + .1f, ZP - .065f, ZP + .065f); }
        }
        float sd0 = Mathf.Clamp(xd - dw * .5f, X0 + 1f, XS - dw - .3f), sd1 = sd0 + dw;      // drzwi sali w osi wejścia
        Scianka(X0, sd0 - .04f, true); Scianka(sd1 + .04f, XS, true);
        Oscieznica(czern, stal, sd0, sd1, ZP, true);
        float zd0 = XS + .5f, zd1 = zd0 + dw;                                                 // drzwi szatni z korytarza
        Scianka(XS, zd0 - .04f, false); Scianka(zd1 + .04f, X1, false);
        Oscieznica(czern, stal, zd0, zd1, ZP, true);
        // ściana sala | szatnia (x = XS) z drzwiami z sali
        float zs0 = ZP + 1.6f, zs1 = zs0 + dw;
        W.MM(biel, XS - .05f, XS + .05f, Y0, YS, ZP + .05f, zs0 - .04f, true); W.MM(biel, XS - .05f, XS + .05f, Y0, YS, zs1 + .04f, ZT, true);
        W.MM(biel, XS - .05f, XS + .05f, Y0 + dh + .06f, YS, zs0 - .04f, zs1 + .04f);
        W.MM(czern, XS - .065f, XS + .065f, Y0, Y0 + .1f, ZP + .05f, zs0 - .04f); W.MM(czern, XS - .065f, XS + .065f, Y0, Y0 + .1f, zs1 + .04f, ZT);
        Oscieznica(czern, stal, zs0, zs1, XS, false);
        W.Napis("SZATNIA", new Vector3((zd0 + zd1) * .5f, Y0 + dh + .25f, ZP - .056f), Vector3.back, .09f, new Color(.93f, .45f, .62f));
        W.Napis("SALA ZAJĘĆ", new Vector3((sd0 + sd1) * .5f, Y0 + dh + .25f, ZP - .036f), Vector3.back, .09f, new Color(.93f, .45f, .62f));

        // ---- recepcja (na wschód od wejścia, przodem do wchodzących) ----
        float lx0 = xd + dw * .5f + 1.1f, lx1 = lx0 + 2.0f * K * .8f, lz0 = 37.9f, lz1 = lz0 + .65f;
        W.MM(roz, lx0, lx1, Y0, Y0 + .1f, lz0 + .05f, lz1, true);                               // cokół
        W.MM(biel, lx0, lx1, Y0 + .1f, Y0 + 1.05f * K - .05f, lz0, lz1, true);                  // korpus
        W.MM(roz, lx0 - .02f, lx1 + .02f, Y0 + .55f, Y0 + .62f, lz0 - .02f, lz0);               // pas różowy na froncie (wpuszczony w lico)
        W.MM(czern, lx0 - .04f, lx1 + .04f, Y0 + 1.05f * K - .05f, Y0 + 1.05f * K, lz0 - .04f, lz1 + .04f);   // blat
        W.MM(biel, lx0 + .1f, lx1 - .1f, Y0 + .72f, Y0 + .76f, lz1 - .05f, lz1 + .35f);          // blat roboczy od strony obsługi
        W.Napis("BRAXHORN FITNESS", new Vector3((lx0 + lx1) * .5f, Y0 + .9f, lz0 - .006f), Vector3.back, .1f, new Color(.93f, .45f, .62f));
        W.MM(czern, lx1 - .6f, lx1 - .25f, Y0 + 1.05f * K, Y0 + 1.05f * K + .3f, lz0 + .25f, lz0 + .28f);   // monitor kasy
        W.MM(czern, lx1 - .45f, lx1 - .4f, Y0 + 1.05f * K, Y0 + 1.05f * K + .1f, lz0 + .29f, lz0 + .36f);
        W.Obsada("recepcja fitness (Nyxa / recepcjonistka)", new Vector3((lx0 + lx1) * .5f, Y0, lz1 + .55f), 180f);
        // lodówka z napojami Z KODU (11.11, właściciel 20:53: „te lodówki z napojami… puste… lepiej jak ty stworzysz taką lodówkę
        // z napojami”): BraxChlodnie — szklane drzwi, półki pełne napojów (woda, izotoniki, puszki), LED; przy prawej ścianie
        // frontem do witryny; roślina, ławka przy witrynie
        BraxChlodnie.LodowkaNapojow(g, "Fitness: lodówka z napojami", new Vector3(X1 - .05f - BraxChlodnie.Szer(1) * .5f, Y0, 38.65f), Vector3.back, 1, 1);
        W.Walec(biel, new Vector3(X1 - .4f, Y0, 37.0f), .22f, .45f, 16, true);
        for (int i = 0; i < 7; i++) { float an = i * 51f * Mathf.Deg2Rad; W.Walec(zielen, new Vector3(X1 - .4f + Mathf.Cos(an) * .1f, Y0 + .45f, 37.0f + Mathf.Sin(an) * .1f), .12f, .5f + .1f * (i % 3), 8); }
        Lawka(drewno, stal, lx0 + .2f, lx1 - .2f, 36.85f);
        W.Siedzisko(new Vector3((lx0 + lx1) * .5f, Y0 + .45f * K, 36.85f), Vector3.back, "ŁAWKA");

        // ---- strefa cardio: 3 bieżnie przodem do witryny ----
        for (int i = 0; i < 3; i++)
        {
            var p = new Vector3(X0 + .7f + i * 1.2f, Y0, 38.0f);
            bool ok = false;
            try { ok = BraxMiastoAssety.Bieznia(g, p, 180f); } catch (Exception e) { Debug.LogWarning("BRAXHORN_FITNESS bieżnia DO POPRAWKI " + e.Message); }
            if (ok) { modele++; continue; }
            // bez paczki: bieżnia z kodu (rama, pas z rolkami, kolumny, konsola, poręcze) — 0.8 × 2.0 m × K
            float w = .8f * K, L = 2.0f * K, z0 = p.z - L * .5f, z1 = p.z + L * .5f;
            W.MM(czern, p.x - w * .5f, p.x + w * .5f, Y0, Y0 + .18f, z0, z1, true);
            W.MM(guma, p.x - w * .38f, p.x + w * .38f, Y0 + .18f, Y0 + .2f, z0 + .1f, z1 - .1f);
            foreach (float zr in new[] { z0 + .08f, z1 - .08f }) W.Rura(stal, new Vector3(p.x - w * .4f, Y0 + .12f, zr), new Vector3(p.x + w * .4f, Y0 + .12f, zr), .05f, 12);
            foreach (float sx in new[] { -1f, 1f })
            {
                var a = new Vector3(p.x + sx * w * .45f, Y0 + .18f, z0 + .25f); var b = a + new Vector3(0, 1.1f * K, -.2f);
                W.Rura(czern, a, b, .04f, 10);
                W.Rura(guma, b + new Vector3(0, -.05f, .02f), b + new Vector3(0, -.05f, .55f), .025f, 8);       // poręcz boczna
            }
            W.OBox(czern, new Vector3(p.x, Y0 + 1.1f * K + .15f, z0 + .02f), new Vector3(w * .95f, .32f, .1f), Quaternion.Euler(-25f, 0, 0));
            W.OBox(M.Plain("fitness: wyświetlacz konsoli", new Color(.1f, .35f, .5f), .1f), new Vector3(p.x, Y0 + 1.1f * K + .17f, z0 + .08f), new Vector3(w * .6f, .16f, .01f), Quaternion.Euler(-25f, 0, 0));
        }

        // ---- sala zajęć ----
        // lustra na ścianie zach. (ramka czarna, 3 cm od ściany)
        W.MM(lustro, X0 + .03f, X0 + .05f, Y0 + .25f, Y0 + .25f + 2.0f * K, ZP + .3f, ZT - .3f);
        W.MM(czern, X0, X0 + .06f, Y0 + .21f, Y0 + .25f, ZP + .26f, ZT - .26f); W.MM(czern, X0, X0 + .06f, Y0 + .25f + 2.0f * K, Y0 + .29f + 2.0f * K, ZP + .26f, ZT - .26f);
        W.MM(czern, X0, X0 + .06f, Y0 + .25f, Y0 + .25f + 2.0f * K, ZP + .26f, ZP + .3f); W.MM(czern, X0, X0 + .06f, Y0 + .25f, Y0 + .25f + 2.0f * K, ZT - .3f, ZT - .26f);
        // maty (0.6 × 1.8 m × K), dwa rzędy po 3, przodem do lustra
        for (int r = 0; r < 2; r++)
            for (int i = 0; i < 3; i++)
            {
                float mx0 = X0 + .5f + i * (1.8f * K + .25f), mz0 = ZP + 1.2f + r * (.6f * K + .5f);   // za łukiem skrzydła drzwi sali
                if (mx0 + 1.8f * K > XS - 1.2f) continue;
                W.MM(i % 2 == r % 2 ? roz : guma, mx0, mx0 + 1.8f * K, Y0, Y0 + .006f * K + .004f, mz0, mz0 + .6f * K);
            }
        // piłki (Ø 0.65 m × K), stepy, stojak hantelek — pas przy ścianie szatni
        for (int i = 0; i < 3; i++)
        {
            float r = .325f * K; var c = new Vector3(XS - .55f, Y0 + r, ZP + .55f + i * (2f * r + .08f));
            W.Walec(i == 1 ? biel : roz, c - Vector3.up * r * .87f, r * .5f, .02f, 16);
            for (int j = -3; j <= 3; j++) { float t = j / 4f, rr = r * Mathf.Sqrt(1f - t * t); W.Walec(i == 1 ? biel : roz, c + Vector3.up * (t * r - r * .125f), rr, r * .25f, 20); }
        }
        for (int i = 0; i < 3; i++)   // stepy ułożone w stos: płyta z gumą + nóżki
        {
            float y = Y0 + i * .16f; float sx0 = XS - 2.3f, sx1 = XS - 1.35f, sz0 = ZT - .95f, sz1 = ZT - .6f;
            W.MM(czern, sx0 + .05f, sx0 + .17f, y, y + .1f, sz0, sz1); W.MM(czern, sx1 - .17f, sx1 - .05f, y, y + .1f, sz0, sz1);
            W.MM(roz, sx0, sx1, y + .1f, y + .15f, sz0, sz1); W.MM(guma, sx0 + .03f, sx1 - .03f, y + .15f, y + .158f, sz0 + .03f, sz1 - .03f);
        }
        {
            float hx0 = XS - 1.2f, hx1 = XS - .25f, hz = ZT - .3f;
            W.Rura(stal, new Vector3(hx0, Y0, hz), new Vector3(hx0, Y0 + .9f, hz), .02f, 8); W.Rura(stal, new Vector3(hx1, Y0, hz), new Vector3(hx1, Y0 + .9f, hz), .02f, 8);
            for (int p2 = 0; p2 < 2; p2++)
            {
                float y = Y0 + .35f + p2 * .45f; W.Rura(stal, new Vector3(hx0, y, hz), new Vector3(hx1, y, hz), .015f, 6);
                for (int i = 0; i < 4; i++)
                {
                    float x = hx0 + .12f + i * .22f, rr = .035f + i * .008f;
                    W.Rura(roz, new Vector3(x - .06f, y + rr, hz - .02f), new Vector3(x - .03f, y + rr, hz - .02f), rr, 8);
                    W.Rura(roz, new Vector3(x + .03f, y + rr, hz - .02f), new Vector3(x + .06f, y + rr, hz - .02f), rr, 8);
                    W.Rura(stal, new Vector3(x - .03f, y + rr, hz - .02f), new Vector3(x + .03f, y + rr, hz - .02f), .012f, 6);
                }
            }
            W.KolizjaMM(hx0, hx1, Y0, Y0 + .95f, hz - .2f, hz + .05f);
        }
        // drążek pod sufitem z dwiema taśmami TRX (uchwyty)
        W.Rura(czern, new Vector3(X0 + .6f, YS - .3f, ZP + .45f), new Vector3(XS - 1.4f, YS - .3f, ZP + .45f), .03f, 10);
        foreach (float x in new[] { X0 + .6f, XS - 1.4f }) W.Rura(stal, new Vector3(x, YS - .3f, ZP + .45f), new Vector3(x, YS, ZP + .45f), .02f, 8);
        foreach (float x in new[] { X0 + 2.2f, X0 + 3.4f })
        {
            var a = new Vector3(x, YS - .33f, ZP + .45f); var b = new Vector3(x, Y0 + 1.15f * K, ZP + .45f);
            W.Rura(M.Plain("fitness: taśma TRX", new Color(.85f, .75f, .1f), .7f), a, b, .015f, 4);
            W.Rura(guma, b + new Vector3(-.09f, 0, 0), b + new Vector3(.09f, 0, 0), .02f, 8);
        }
        // MURAL: quad „mural_fitness” 5.76 × 3.24 m na ścianie płn. sali, dół 0.30 m nad posadzką, czarna rama, 4 kinkiety
        float salaC = (X0 + XS) * .5f, mw = 5.76f, mh = 3.24f, my0 = Y0 + .30f, my1 = my0 + mh, mz = ZT - .03f;
        float mx0b = salaC - mw * .5f, mx1b = salaC + mw * .5f;
        bool grafika = Mural(g, mx0b, mx1b, my0, Mathf.Min(my1, YS - .005f), mz);
        W.MM(czern, mx0b - .06f, mx1b + .06f, my0 - .06f, my0, mz - .01f, ZT);               // rama: dół
        W.MM(czern, mx0b - .06f, mx0b, my0, Mathf.Min(my1, YS - .005f), mz - .01f, ZT);       // boki
        W.MM(czern, mx1b, mx1b + .06f, my0, Mathf.Min(my1, YS - .005f), mz - .01f, ZT);
        W.MM(czern, mx0b - .06f, mx1b + .06f, Mathf.Min(my1, YS - .005f), YS, mz - .01f, ZT);   // góra (do sufitu)
        foreach (float x in new[] { mx0b - .4f, mx1b + .4f })
            foreach (float y in new[] { Y0 + 1.3f, Y0 + 2.6f })
            {
                W.MM(czern, x - .07f, x + .07f, y - .1f, y + .1f, ZT - .02f, ZT);                       // rozeta
                W.Rura(stal, new Vector3(x, y, ZT - .02f), new Vector3(x, y + .05f, ZT - .2f), .012f, 6);      // ramię
                W.Walec(roz, new Vector3(x, y + .02f, ZT - .22f), .07f, .14f, 14);                       // klosz
                W.MM(led, x - .04f, x + .04f, y + .015f, y + .02f, ZT - .26f, ZT - .18f);
            }
        W.Lampa("fitness: kinkiety muralu (zach.)", new Vector3(mx0b - .4f, Y0 + 2f, ZT - .4f), .45f, 4.5f, new Color(1f, .9f, .85f));
        W.Lampa("fitness: kinkiety muralu (wsch.)", new Vector3(mx1b + .4f, Y0 + 2f, ZT - .4f), .45f, 4.5f, new Color(1f, .9f, .85f));
        W.Obsada("Nyxa — trenerka przy lustrze", new Vector3(X0 + .6f, Y0, ZP + 2.6f), 90f);
        W.Obsada("klientka fitness — mata", new Vector3(X0 + 1.6f, Y0, ZP + 1.6f), 270f);
        W.Obsada("klientka fitness — mata", new Vector3(X0 + 4.1f, Y0, ZP + 1.6f), 270f);
        W.Obsada("klientka fitness — bieżnia", new Vector3(X0 + 1.9f, Y0 + .2f, 38.2f), 180f);

        // ---- szatnia damska ----
        for (int i = 0; i < 4; i++)   // szafki 0.36 × 0.45 m (× K), dwa poziomy, drzwiczki z wentylacją i numerem
        {
            float z0 = ZP + .25f + i * .36f * K, z1 = z0 + .36f * K - .01f;
            W.MM(biel, X1 - .45f * K, X1 - .02f, Y0 + .1f, Y0 + 1.8f * K, z0, z1, true);
            foreach (var (y0, y1, nr) in new[] { (Y0 + .15f, Y0 + .9f * K, i * 2 + 1), (Y0 + .9f * K + .03f, Y0 + 1.8f * K - .05f, i * 2 + 2) })
            {
                W.MM(roz, X1 - .45f * K - .02f, X1 - .45f * K, y0, y1, z0 + .01f, z1 - .01f);
                for (int s = 0; s < 3; s++) W.MM(czern, X1 - .45f * K - .022f, X1 - .45f * K - .02f, y1 - .14f - s * .04f, y1 - .125f - s * .04f, z0 + .06f, z1 - .06f);
                W.MM(stal, X1 - .45f * K - .05f, X1 - .45f * K - .02f, (y0 + y1) * .5f - .05f, (y0 + y1) * .5f + .05f, z1 - .07f, z1 - .05f);
                W.Napis(nr.ToString(), new Vector3(X1 - .45f * K - .025f, y1 - .25f, (z0 + z1) * .5f), Vector3.left, .05f, Color.white);
            }
        }
        W.MM(czern, X1 - .45f * K, X1 - .02f, Y0, Y0 + .1f, ZP + .25f, ZP + .25f + 4 * .36f * K);   // cokół szafek
        Lawka2(drewno, stal, XS + 1.6f, ZP + .9f, ZP + 2.5f);
        W.Siedzisko(new Vector3(XS + 1.6f, Y0 + .45f * K, ZP + 1.7f), Vector3.left, "ŁAWKA");
        // umywalka z lustrem (ściana płn., zach. część szatni)
        float ux = XS + .75f;
        W.MM(biel, ux - .4f, ux + .4f, Y0 + .82f, Y0 + .88f, ZT - .5f, ZT, true);
        W.Walec(biel, new Vector3(ux, Y0 + .7f, ZT - .25f), .18f, .14f, 18);
        W.Rura(stal, new Vector3(ux, Y0 + .88f, ZT - .05f), new Vector3(ux, Y0 + 1.1f, ZT - .05f), .015f, 8); W.Rura(stal, new Vector3(ux, Y0 + 1.1f, ZT - .05f), new Vector3(ux, Y0 + 1.08f, ZT - .2f), .012f, 8);
        W.MM(lustro, ux - .35f, ux + .35f, Y0 + 1.15f, Y0 + 2.0f, ZT - .03f, ZT - .02f);
        W.MM(czern, ux - .37f, ux + .37f, Y0 + 1.13f, Y0 + 1.15f, ZT - .035f, ZT - .015f);
        // kabina prysznicowa (narożnik płn.-wsch.)
        float px0 = X1 - 1.0f, pz0 = ZT - 1.0f;
        W.MM(biel, px0, X1, Y0, Y0 + .05f, pz0, ZT, true);
        W.MM(szklo, px0, px0 + .01f, Y0 + .05f, Y0 + 2.0f * K * .95f, pz0, ZT); W.MM(szklo, px0, X1 - .45f * K - .05f, Y0 + .05f, Y0 + 2.0f * K * .95f, pz0, pz0 + .01f);
        W.MM(stal, px0 - .01f, px0 + .02f, Y0 + .05f, Y0 + 2.0f * K * .95f, pz0 - .01f, pz0 + .02f);
        W.Rura(stal, new Vector3(X1 - .3f, Y0 + .9f, ZT - .03f), new Vector3(X1 - .3f, Y0 + 2.1f * K, ZT - .03f), .012f, 8);
        W.Walec(stal, new Vector3(X1 - .3f, Y0 + 2.1f * K, ZT - .2f), .1f, .02f, 16);
        W.Rura(stal, new Vector3(X1 - .3f, Y0 + 2.1f * K + .01f, ZT - .03f), new Vector3(X1 - .3f, Y0 + 2.1f * K + .01f, ZT - .2f), .012f, 6);
        W.KolizjaMM(px0, X1, Y0, Y0 + 2.2f, pz0, pz0 + .03f);

        // ---- sufit: oprawy liniowe LED (2 cm pod sufitem) i światło ----
        for (float z = 37.2f; z < ZT - .5f; z += 1.6f) W.MM(led, X0 + .6f, X1 - .6f, YS - .03f, YS - .01f, z - .04f, z + .04f);
        W.Lampa("fitness: światło recepcji", new Vector3(lx0, 2.6f, 38.2f), .8f, 7f);
        W.Lampa("fitness: światło cardio", new Vector3(X0 + 1.8f, 2.6f, 37.8f), .8f, 7f);
        W.Lampa("fitness: światło sali", new Vector3(salaC, 2.8f, ZP + 1.8f), .9f, 8f);
        W.Lampa("fitness: światło szatni", new Vector3(XS + 1.6f, 2.6f, ZP + 1.8f), .6f, 5f);
        W.Punkt("FITNESS_NYXY", "BRAXHORN FITNESS — recepcja", new Vector3(xd, Y0, 37.4f));
        int siatki = W.Zapisz("fitness_nyxy");
        Debug.Log("BRAXHORN_FITNESS lokal=gallery x " + X0 + "…" + X1 + " z " + ZF + "…" + ZT + " wejście_x=" + xd.ToString("F2") + " bieżnie_model=" + modele + "/3 mural=" + (grafika ? "02.jpg" : "BRAK grafiki (rama + quad mural_fitness)")
            + " mural_m=" + mw + "×" + mh + " szatnia_drzwi=korytarz+sala siatek=" + siatki);
    }

    static void Oscieznica(Material rama, Material stal, float a0, float a1, float plaszczyzna, bool wzdluzX)
    {
        float dh = 2.1f * BraxMiastoA.K, t = .06f, g = .09f;
        Vector3 P(float u, float y, float w) => wzdluzX ? new Vector3(u, y, plaszczyzna + w) : new Vector3(plaszczyzna + w, y, u);
        void B(float u0, float u1, float y0, float y1) { var p = P(u0, y0, -g); var q = P(u1, y1, g); W.MM(rama, Mathf.Min(p.x, q.x), Mathf.Max(p.x, q.x), y0, y1, Mathf.Min(p.z, q.z), Mathf.Max(p.z, q.z)); }
        B(a0 - t, a0, Y0, Y0 + dh + t); B(a1, a1 + t, Y0, Y0 + dh + t); B(a0, a1, Y0 + dh, Y0 + dh + t);
        // skrzydło uchylone (otwarte do środka pomieszczenia za ścianką), klamki po obu stronach
        var os = P(a0, Y0, g); var kier = wzdluzX ? new Vector3(.35f, 0, .94f) : new Vector3(.94f, 0, .35f);
        var sr = os + kier * ((a1 - a0) * .5f) + Vector3.up * (dh * .5f);
        W.OBox(M.Plain("fitness: skrzydło drzwi (biel)", new Color(.96f, .96f, .95f), .4f), sr, new Vector3(.04f, dh - .02f, a1 - a0 - .02f), Quaternion.LookRotation(kier) * Quaternion.Euler(0, 90, 0));
        var kl = os + kier * (a1 - a0 - .1f) + Vector3.up * 1.05f * BraxMiastoA.K;
        W.Rura(stal, kl, kl + Vector3.Cross(Vector3.up, kier) * .12f, .011f, 6);
    }

    static void Lawka(Material drewno, Material stal, float x0, float x1, float z)
    {
        float h = .45f * BraxMiastoA.K, d = .4f;
        for (int i = 0; i < 3; i++) W.MM(drewno, x0, x1, Y0 + h - .04f, Y0 + h, z - d * .5f + i * .135f, z - d * .5f + i * .135f + .12f);
        foreach (float x in new[] { x0 + .1f, x1 - .1f }) { W.MM(stal, x - .02f, x + .02f, Y0, Y0 + h - .04f, z - d * .5f, z - d * .5f + .04f); W.MM(stal, x - .02f, x + .02f, Y0, Y0 + h - .04f, z + d * .5f - .04f, z + d * .5f); W.MM(stal, x - .02f, x + .02f, Y0 + h - .08f, Y0 + h - .04f, z - d * .5f, z + d * .5f); }
        W.KolizjaMM(x0, x1, Y0, Y0 + h, z - d * .5f, z + d * .5f);
    }
    static void Lawka2(Material drewno, Material stal, float x, float z0, float z1)
    {
        float h = .45f * BraxMiastoA.K, d = .4f;
        for (int i = 0; i < 3; i++) W.MM(drewno, x - d * .5f + i * .135f, x - d * .5f + i * .135f + .12f, Y0 + h - .04f, Y0 + h, z0, z1);
        foreach (float z in new[] { z0 + .1f, z1 - .1f }) { W.MM(stal, x - d * .5f, x - d * .5f + .04f, Y0, Y0 + h - .04f, z - .02f, z + .02f); W.MM(stal, x + d * .5f - .04f, x + d * .5f, Y0, Y0 + h - .04f, z - .02f, z + .02f); W.MM(stal, x - d * .5f, x + d * .5f, Y0 + h - .08f, Y0 + h - .04f, z - .02f, z + .02f); }
        W.KolizjaMM(x - d * .5f, x + d * .5f, Y0, Y0 + h, z0, z1);
    }

    /// <summary>Quad muralu z materiałem dokładnie „mural_fitness” (UV 0…1, przód do sali, −z). Grafika 02.jpg, gdy jest.</summary>
    static bool Mural(Transform g, float x0, float x1, float y0, float y1, float z)
    {
        bool jest = false; Material m = null;
        try
        {
            string dir = W.Out + "/Fitness"; Directory.CreateDirectory(dir);
            m = new Material(Shader.Find("Braxhorn/CityStaticPBR")) { name = "mural_fitness" };
            Texture2D tex = null;
            if (File.Exists(MuralPlik)) { AssetDatabase.ImportAsset(MuralPlik, ImportAssetOptions.ForceSynchronousImport); tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MuralPlik); }
            if (tex) { m.SetTexture("_MainTex", tex); m.SetColor("_Color", Color.white); jest = true; }
            else m.SetColor("_Color", new Color(.18f, .17f, .18f));
            if (m.HasProperty("_Roughness")) m.SetFloat("_Roughness", .8f);
            BraxZapisAssetu.Utworz(m, dir + "/mural_fitness.mat");
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_FITNESS mural DO POPRAWKI " + e.Message); if (!m) return false; }
        var mesh = new Mesh { name = "mural_fitness (quad 5.76 × 3.24)" };
        mesh.vertices = new[] { new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 }; mesh.RecalculateBounds();
        BraxMiastoA.Obiekt(g, "mural_fitness", mesh, m);
        return jest;
    }
}

using System.Collections.Generic;
using UnityEngine;

// DWORZEC R1 — TORY, PERONY, SIEĆ TRAKCYJNA, PRZEJAZDY, PORTAL ZACHODNI, MURY OPOROWE (11.11.2026).
// Wymiary pod pociąg MIASTA MISTRZÓW (właściciel 16:32): rozstaw 1.857 m (osie szyn ±0.975), główka szyny 0.984 m pod peronem,
// krawędź 2.233 m od osi, osie torów 5.825 m, przewód jezdny 6.86 m nad główką (pod kładką bez zmian — kładka wyżej).
// Szyna 60E1 × K: główka 0.093 × 0.065, szyjka 0.021, stopka 0.194; podkład betonowy 2.6 × 0.26 × 0.22 m × K co 0.6 m × K.
internal static partial class BraxMiastoDworzec
{
    public const float KladkaX0 = 22f, KladkaX1 = 26f, KladkaY = 8.1f, KladkaSpod = 7.55f;
    static float XToruOd => PortalX - TunelDl;

    // ============================================================================ TORY
    static void Tory()
    {
        Grupa("tory", 260f);
        float xa = XToruOd, xb = KozlyX + .6f;
        float yPod = GlowkaY - SzynaH;            // wierzch podkładu
        int podkladow = 0;
        foreach (float zt in new[] { T1, T2 })
        {
            // podsypka: korona 0.04 m pod wierzchem podkładu, skarpy do dna przekopu
            float yk = yPod - .04f, hk = PodkladDl * .5f + .3f, hs = hk + 1.2f;
            var s = S(zwir);
            s.Quad(V(xa, yk, zt - hk), V(xb, yk, zt - hk), V(xb, yk, zt + hk), V(xa, yk, zt + hk), Vector3.up);
            s.Quad(V(xa, yk, zt + hk), V(xb, yk, zt + hk), V(xb, Formacja, zt + hs), V(xa, Formacja, zt + hs), new Vector3(0f, 1f, 1f));
            s.Quad(V(xa, yk, zt - hk), V(xb, yk, zt - hk), V(xb, Formacja, zt - hs), V(xa, Formacja, zt - hs), new Vector3(0f, 1f, -1f));
            // podkłady (pomijane na płytach przejazdów — tam płyty betonowe)
            var sp = S(podklad);
            for (float x = xa + .5f; x < KozlyX - .3f; x += PodkladRozstaw)
            {
                if (Mathf.Abs(x - LacznikX) < LacznikH + .3f || Mathf.Abs(x - C0X) < LacznikH + .3f) continue;
                sp.Box(V(x - PodkladSz * .5f, yPod - PodkladH, zt - PodkladDl * .5f), V(x + PodkladSz * .5f, yPod, zt + PodkladDl * .5f), 4);
                podkladow++;
            }
            // szyny 60E1 × K (główka, szyjka, stopka)
            var sz = S(szyna);
            foreach (float sg in new[] { -1f, 1f })
            {
                float zr = zt + sg * SzynaOs;
                // koniec szyn wewnątrz ramy kozła (0.25 m przed jej tylną ścianą) — czoło szyny nie leży na licu ramy
                sz.Box(V(xa, GlowkaY - .065f, zr - .0465f), V(KozlyX - .25f, GlowkaY, zr + .0465f), 0);
                sz.Box(V(xa, yPod + .025f, zr - .0105f), V(KozlyX - .25f, GlowkaY - .065f, zr + .0105f), 12);
                sz.Box(V(xa, yPod, zr - .097f), V(KozlyX - .25f, yPod + .025f, zr + .097f), 4);
            }
            // kozioł oporowy: rama stalowa czerwona, dwa zderzaki, blok betonowy, tablica (biało-czerwona)
            Grupa("tory: kozły", 160f);
            B(betonMur, KozlyX, Formacja, zt - 1.9f, KoniecTorow, yPod + .6f, zt + 1.9f);
            // zderzaki na wysokości 1.06 m × K nad główką, rozstaw 1.75 m × K (jak w pociągu)
            float yz = GlowkaY + 1.06f * 1.2944f, rz = 1.75f * 1.2944f * .5f;
            B(czerwony, KozlyX - .5f, yPod, zt - rz - .35f, KozlyX, yz + .45f, zt + rz + .35f);
            foreach (float sg in new[] { -1f, 1f })
            {
                S(stal).Rura(V(KozlyX - .53f, yz, zt + sg * rz), V(KozlyX - 1.0f, yz, zt + sg * rz), .22f, 16);   // od lica pasów
                S(czarnyAlu).Rura(V(KozlyX - 1.0f, yz, zt + sg * rz), V(KozlyX - 1.06f, yz, zt + sg * rz), .32f, 20);
            }
            for (int k = 0; k < 3; k++) B(farba, KozlyX - .53f, yz - .75f + k * .3f, zt - rz - .3f, KozlyX - .5f, yz - .6f + k * .3f, zt + rz + .3f);
            Grupa("tory", 260f);
        }
        // mur końcowy przekopu za kozłami
        B(betonMur, KoniecTorow, Formacja - .2f, P2Kraw - .4f, KoniecTorow + 1f, YP + .9f, P1Kraw + .4f);
        Kol("mur końcowy torów", V(KoniecTorow, Formacja, P2Kraw), V(KoniecTorow + 1f, YP + .9f, P1Kraw));
        // mury przekopu za peronami (do kozłów): północ i południe
        B(betonMur, PX1, Formacja - .2f, P1Kraw, KoniecTorow, YP, P1Kraw + .4f);
        B(betonMur, PX1, Formacja - .2f, P2Kraw - .4f, KoniecTorow, YP, P2Kraw);
        Zajmij(XToruOd, LozeS - Skarpa - 1f, KoniecTorow + 1f, LozeN + Skarpa + 1f);
        Portal();
        Opis("tory: 2 × " + (KozlyX - xa).ToString("F0") + " m, osie z " + T1.ToString("F3") + " / " + T2.ToString("F3") + " (" + OsieTorow + " m), główka szyny y " + GlowkaY.ToString("F3") + ", podkładów " + podkladow + ", kozły x " + KozlyX);
    }

    // portal zachodni: mur czołowy z otworem (dwa tory, sieć), skrzydła po wzgórzu, tunel 40 m z ciemnym końcem
    static void Portal()
    {
        Grupa("tory: portal zachodni", 300f);
        float xa = PortalX - 2f, xb = PortalX, zn = T1 + 3.6f, zs = T2 - 3.6f, yo = GlowkaY + TunelH;
        for (float z = -198f; z < -100f; z += 2f)
        {
            float z1 = Mathf.Min(z + 2f, -100f), zc = (z + z1) * .5f;
            float top = Mathf.Max(WzgorzePortalu(xa - .1f, z), WzgorzePortalu(xa - .1f, z1)) + .45f;
            if (top < YC + .6f) top = YC + .6f;
            float bot = Mathf.Min(Przekop(xb + .1f, zc), YC) - .4f;
            if (zc < zn && zc > zs) { if (top > yo) B(kamienMur, xa, yo, z, xb, top, z1); }
            else B(kamienMur, xa, bot, z, xb, top, z1);
        }
        // rama otworu (beton), gzyms
        B(betonMur, xb, Formacja - .2f, zn, xb + .6f, yo + .9f, zn + .8f);
        B(betonMur, xb, Formacja - .2f, zs - .8f, xb + .6f, yo + .9f, zs);
        B(betonMur, xb, yo, zs - .8f, xb + .6f, yo + .9f, zn + .8f);
        // tunel: ściany, strop, dno, ciemny koniec
        float xe = PortalX - TunelDl;
        var ciemny = T("wnętrze tunelu (beton zakopcony)", 2.2f, new Color(.32f, .31f, .3f), .95f, 0f, "concrete_wall_008", "concrete_floor_01");
        var koniec = T("koniec tunelu (ciemność)", 2.2f, new Color(.03f, .03f, .03f), 1f, 0f, "concrete_wall_008", "concrete_floor_01");
        B(ciemny, xe, Formacja - .3f, zn, xa, yo, zn + .5f); B(ciemny, xe, Formacja - .3f, zs - .5f, xa, yo, zs);
        B(ciemny, xe, yo, zs - .5f, xa, yo + .5f, zn + .5f);
        Plyta(zwir, xe, zs, xa, zn, Formacja, .2f);
        B(koniec, xe - .5f, Formacja - .3f, zs, xe, yo, zn);
        Kol("tunel kolejowy: ściana płn.", V(xe, Formacja, zn), V(xa, yo, zn + .5f));
        Kol("tunel kolejowy: ściana płd.", V(xe, Formacja, zs - .5f), V(xa, yo, zs));
        Kol("tunel kolejowy: koniec", V(xe - .5f, Formacja, zs), V(xe, yo, zn));
        Opis("portal zachodni x " + PortalX + ", otwór " + (zn - zs).ToString("F1") + " × " + (yo - Formacja).ToString("F1") + " m, tunel " + TunelDl + " m");
    }

    // ============================================================================ PRZEJAZD (krzyże św. Andrzeja, sygnalizatory, rogatki podniesione)
    static void Przejazd(string nazwa, float xc, float hw)
    {
        Grupa("przejazdy: wyposażenie", 160f);
        // po prawej stronie wjazdu z obu kierunków: od miasta (jadąc na południe — prawa = zachód), od faweli (prawa = wschód)
        foreach (var (x, z, zw) in new[] { (xc - hw - .9f, LozeN + 2.2f, 1f), (xc + hw + .9f, LozeS - 2.2f, -1f) })
        {
            float y0 = YC + .35f;
            S(grafit).Rura(V(x, y0, z), V(x, y0 + 3.6f, z), .07f, 12);
            // krzyż św. Andrzeja (dwa ramiona biało-czerwone, płyty 3D na słupie, lico w stronę nadjeżdżających)
            var nn = new Vector3(0f, 0f, zw); float yk = y0 + 3.1f;
            foreach (float sg in new[] { -1f, 1f })
            {
                var dir = new Vector3(.766f * sg, .643f, 0f);
                S(farba).OBox(V(x, yk, z + zw * .09f), dir, Vector3.Cross(nn, dir).normalized, nn, 1.55f, .2f, .04f);
                // czerwone pola na licu białego ramienia (3 cm grubości — lico 3 cm przed białym, tył na białym)
                for (int k = -1; k <= 1; k += 2) S(czerwony).OBox(V(x, yk, z + zw * .125f) + dir * (k * .45f), dir, Vector3.Cross(nn, dir).normalized, nn, .32f, .2f, .03f);
            }
            // sygnalizator: dwie latarnie (czerwone, zgaszone — pociągu jeszcze nie ma) na tarczy czarnej
            B(czarnyAlu, x - .55f, y0 + 1.9f, z + zw * .08f - .03f, x + .55f, y0 + 2.4f, z + zw * .08f + .03f);
            foreach (float sg in new[] { -1f, 1f }) S(czerwony).Rura(V(x + sg * .3f, y0 + 2.15f, z + zw * .1f), V(x + sg * .3f, y0 + 2.15f, z + zw * .18f), .12f, 16);
            // rogatka: napęd przy słupie, ramię podniesione (pionowe), pasy biało-czerwone
            float xr = x + (x < xc ? .45f : -.45f);
            B(grafit, xr - .25f, y0, z - .25f, xr + .25f, y0 + 1.1f, z + .25f);
            for (int k = 0; k < 8; k++) B(k % 2 == 0 ? farba : czerwony, xr - .05f, y0 + 1.1f + k * .55f, z - .05f, xr + .05f, y0 + 1.65f + k * .55f, z + .05f);
            Kol(nazwa + ": słup przejazdu", V(x - .2f, y0, z - .2f), V(x + .2f, y0 + 3.6f, z + .2f));
        }
    }

    // ============================================================================ PERONY
    static void Perony()
    {
        Grupa("nawierzchnie: perony");
        Peron(1, P1Kraw, P1Tyl, -1f);
        Peron(2, P2Kraw, P2Tyl, 1f);
        // ścieżka od łącznika (strona faweli) do peronu 2 i do schodów faweli; wyjście z peronu 2 na las (furtka)
        Plyta(chodnik, LacznikX + LacznikH + Kr, P2Tyl, PX0, LozeS - Skarpa - .2f, YP);
        Grupa("perony: balustrada peronu 2", 120f);
        for (float x = PX0 + 3f; x < PX1 - .5f; x += 2f)
        {
            if (x > 43.5f && x < 48.5f) continue;   // furtka do ścieżki w lesie (do schodów faweli)
            float x1 = Mathf.Min(x + 2f, PX1 - .3f);
            S(czarnyAlu).Rura(V(x, YP, P2Tyl + .12f), V(x, YP + 1.42f, P2Tyl + .12f), .03f, 8);
            S(stal).Rura(V(x, YP + 1.42f, P2Tyl + .12f), V(x1, YP + 1.42f, P2Tyl + .12f), .035f, 10);
            for (float y = .25f; y < 1.3f; y += .32f) S(stal).Rura(V(x, YP + y, P2Tyl + .12f), V(x1, YP + y, P2Tyl + .12f), .012f, 6);
        }
        Kol("balustrada peronu 2 (zach.)", V(PX0 + 3f, YP, P2Tyl), V(43.5f, YP + 1.42f, P2Tyl + .25f));
        Kol("balustrada peronu 2 (wsch.)", V(48.5f, YP, P2Tyl), V(PX1, YP + 1.42f, P2Tyl + .25f));
        // ścieżka z furtki peronu 2 do schodów faweli (x 40) przez las
        Plyta(chodnik, 44f, BraxRegionFawela.KrawedzSwiat(46f) + .2f, 48f, P2Tyl, YP - .12f);
        Zajmij(PX0, P2Tyl - 1f, PX1, P1Tyl);
        Zajmij(43f, BraxRegionFawela.KrawedzSwiat(46f), 49f, P2Tyl);
        Opis("perony: 1 z " + P1Tyl + "…" + P1Kraw.ToString("F3") + ", 2 z " + P2Kraw.ToString("F3") + "…" + P2Tyl + ", długość " + (PX1 - PX0) + " m (skład " + SkladDl + " m), krawędź " + KrawedzOdOsi + " m od osi, wysokość " + PeronNadGlowka + " m nad główką");
    }

    /// <summary>Peron boczny: korpus betonowy od dna przekopu, wierzch z pasów (krawężnik granitowy, płytki z wypustkami, biała linia, płyty).
    /// kier = kierunek od krawędzi w głąb peronu (+1 na południe dla peronu 2, −1… dla peronu 1 to +z, więc znak jak w wywołaniu).</summary>
    static void Peron(int nr, float zKraw, float zTyl, float kier)
    {
        // kier: −1 → peron 1 (głąb na +z), +1 → peron 2 (głąb na −z)
        float g = -kier;   // znak przyrostu z w głąb peronu
        float Zd(float d) => zKraw + g * d;
        B(betonMur, PX0, Formacja - .1f, Mathf.Min(zKraw, zTyl), PX1, YP - .05f, Mathf.Max(zKraw, zTyl), 0);
        // pasy wierzchu (od krawędzi): krawężnik granit 0.32, płyty, płytki z wypustkami 0.40, płyty, biała linia 0.08, płyty
        var pasy = new (float d0, float d1, Material m)[] { (0f, .32f, granitC), (.32f, .45f, plyty), (.45f, .85f, dotyk), (.85f, .95f, plyty), (.95f, 1.03f, farba), (1.03f, Mathf.Abs(zTyl - zKraw), plyty) };
        // otwory na szyby wind (kabina staje w poziomie peronu — pod nią szyb, nie płyta)
        foreach (var (d0, d1, m) in pasy) PlytaBez(m, PX0, Zd(d0), PX1, Zd(d1), YP, .05f, OtworySzybow);
        // lico krawędzi peronu (nad torem): wysunięty gzyms granitu 3 cm
        B(granitC, PX0, YP - .3f, Mathf.Min(zKraw, zKraw - g * .03f), PX1, YP - .05f, Mathf.Max(zKraw, zKraw - g * .03f), 0);
        Kol("peron " + nr, V(PX0, Formacja, Mathf.Min(zKraw, zTyl)), V(PX1, YP, Mathf.Max(zKraw, zTyl)));
    }

    // ============================================================================ SIEĆ TRAKCYJNA
    static void Siec()
    {
        Grupa("sieć trakcyjna", 220f);
        float yJezdny = GlowkaY + SiecNadGlowka, yNosny = GlowkaY + 7.5f, ySlup = GlowkaY + 7.9f;
        int slupow = 0;
        for (float x = PortalX + 8f; x < KozlyX - 4f; x += 38f)
        {
            if (x > KladkaX0 - 3f && x < KladkaX1 + 3f) continue;
            bool stacja = x >= PX0 && x <= PX1;
            foreach (var (zt, sg) in new[] { (T1, 1f), (T2, -1f) })
            {
                // na peronach 2.4 m od krawędzi (poza wiatami), poza peronami w dnie przekopu przy skarpie (3.38 m od osi toru)
                float zs = stacja ? (sg > 0 ? P1Kraw + 2.4f : P2Kraw - 2.4f) : (sg > 0 ? LozeN - .35f : LozeS + .35f);
                float yb = stacja ? YP : Formacja;
                // słup: dwuteownik (półki i środnik)
                B(grafit, x - .14f, yb - .3f, zs - .02f, x + .14f, ySlup, zs + .02f);
                B(grafit, x - .14f, yb - .3f, zs - .16f, x - .1f, ySlup, zs + .16f);
                B(grafit, x + .1f, yb - .3f, zs - .16f, x + .14f, ySlup, zs + .16f);
                // wysięgnik nad oś toru, wieszak, izolatory
                S(stal).Rura(V(x, yNosny + .25f, zs), V(x, yNosny + .25f, zt - sg * .4f), .045f, 10);
                S(stal).Rura(V(x, yJezdny + .55f, zs), V(x, yJezdny + .55f, zt), .035f, 10);
                S(stal).Rura(V(x, yJezdny + .55f, zt), V(x, yJezdny + .007f, zt), .006f, 6, false);   // wieszak na wierzchu przewodu (bez denka na przewodzie)
                S(czarnyAlu).Rura(V(x, yNosny + .25f, zs - sg * .25f), V(x, yNosny + .25f, zs - sg * .6f), .07f, 12);
                Kol("słup sieci", V(x - .16f, yb, zs - .18f), V(x + .16f, ySlup, zs + .18f));
                slupow++;
            }
        }
        // przewody: jezdny i nośny nad każdym torem (z zygzakiem pominiętym — ±0.2 m nie widać z peronu)
        foreach (float zt in new[] { T1, T2 })
        {
            B(grafit, PortalX + 2f, yJezdny - .007f, zt - .007f, KozlyX - 2f, yJezdny + .007f, zt + .007f);
            B(grafit, PortalX + 2f, yNosny - .007f, zt - .007f, KozlyX - 2f, yNosny + .007f, zt + .007f);
            for (float x = PortalX + 6f; x < KozlyX - 3f; x += 9.5f) B(grafit, x - .004f, yJezdny, zt - .004f, x + .004f, yNosny, zt + .004f);
        }
        Opis("sieć: słupów " + slupow + ", przewód jezdny y " + yJezdny.ToString("F3") + " (" + SiecNadGlowka + " m nad główką), nośny " + yNosny.ToString("F2") + ", spód kładki " + KladkaSpod);
    }
}

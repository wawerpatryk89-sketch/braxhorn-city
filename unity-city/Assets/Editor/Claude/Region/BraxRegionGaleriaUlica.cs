using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ul. KOLEJOWA (na wschód od wiaduktu), otoczenie galerii, dojazd, parking, plac ustawek (scena główna) + bryły z daleka,
// punkty świata, miejsca ludzi. Wiadukt (arkady, korytarz pod pomostem z posadzką) to zakres „tyły” — tu nic na x −70.95…−61.45.
// Z ulicy głównej w lewo (na zachód) i w prawo (na północ) korytarzem pod wiaduktem → łukami: na zachód plac przed galerią
// (wejście główne naprzeciw łuku z 63.7…74.3), na wschód ul. Kolejowa z pierzeją i lombardem. Samochody: Kolejowa ma wjazd
// z korytarza pod wiaduktem łukiem z 51.7…62.3 (zjazd z obniżonym krawężnikiem), parking galerii — z ulicy S1 dzielnicy (x −111.5).
public static partial class BraxRegionGaleria
{
    /// <summary>Płyta gruntu z kolizją; wierzch y, spód −0.3.</summary>
    static void Plyta(float x0, float x1, float z0, float z1, float y, Material m, string grupa)
    {
        if (x1 - x0 < .01f || z1 - z0 < .01f) return;
        G(grupa, ulica); BX(x0, x1, y - .3f, y, z0, z1, m, 1); Kol(x0, x1, y - .3f, y, z0, z1);
    }
    static void Kraweznik(float x0, float x1, float z0, float z1, float y = YC)
    {
        G("krawężniki", ulica); BX(x0, x1, YJ - .25f, y, z0, z1, mKraweznik, 1); Kol(x0, x1, YJ - .25f, y, z0, z1);
    }
    static void Zebra(float x0, float x1, float z0, float z1, bool wzdluzZ)
    {
        G("oznakowanie", ulica);
        if (wzdluzZ) for (float x = x0 + .3f; x < x1 - .35f; x += 1.0f) BX(x, x + .5f, YJ, YJ + .006f, z0, z1, mFarbaBiala, 1);
        else for (float z = z0 + .3f; z < z1 - .35f; z += 1.0f) BX(x0, x1, YJ, YJ + .006f, z, z + .5f, mFarbaBiala, 1);
    }
    static void Kratka(float x, float z, bool wzdluzZ)
    {
        G("kratki i włazy", ulica);
        float hx = wzdluzZ ? .25f : .45f, hz = wzdluzZ ? .45f : .25f;
        BX(x - hx, x + hx, YJ, YJ + .01f, z - hz, z + hz, mZeliwo, 1);
        for (int i = 0; i < 6; i++)
        {
            if (wzdluzZ) BX(x - .23f, x + .23f, YJ + .01f, YJ + .016f, z - .38f + i * .15f, z - .32f + i * .15f, mStalGrafit, 1);
            else BX(x - .38f + i * .15f, x - .32f + i * .15f, YJ + .01f, YJ + .016f, z - .23f, z + .23f, mStalGrafit, 1);
        }
    }

    // ======================================================================== ULICA KOLEJOWA + otoczenie galerii
    static void UlicaKolejowa()
    {
        // ---- Kolejowa: jezdnia, krawężniki, chodniki
        Plyta(KJX0, KJX1, KZ0, KZ1, YJ, mAsfalt, "jezdnia");
        // zawrotka (koło r 6 m) na końcu
        {
            G("jezdnia", ulica); var s = S(mAsfalt); var k = S(mKraweznik); var c = new Vector3(ZawX, YJ, ZawZ);
            int n = 28;
            for (int i = 0; i < n; i++)
            {
                float a0 = 2 * Mathf.PI * i / n, a1 = 2 * Mathf.PI * (i + 1) / n;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                s.Tri(c, c + d1 * ZawR, c + d0 * ZawR, g.bake);
                bool wlot = Mathf.Abs(Mathf.Atan2(d0.z + d1.z, d0.x + d1.x) * Mathf.Rad2Deg + 90f) < 34f;   // od pd. wjeżdża jezdnia
                if (wlot) continue;
                Vector3 p0 = c + d0 * ZawR, p1 = c + d1 * ZawR, q0 = c + d0 * (ZawR + .22f), q1 = c + d1 * (ZawR + .22f);
                k.Quad(new Vector3(p0.x, YC, p0.z), new Vector3(p1.x, YC, p1.z), new Vector3(q1.x, YC, q1.z), new Vector3(q0.x, YC, q0.z), g.bake);
                k.Quad(new Vector3(p1.x, YJ, p1.z), new Vector3(p0.x, YJ, p0.z), new Vector3(p0.x, YC, p0.z), new Vector3(p1.x, YC, p1.z), g.bake);
                var mid = (p0 + q1) * .5f; KolR(new Vector3(mid.x, (YJ + YC) * .5f - .05f, mid.z), new Vector3(.22f, .25f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, Vector3.up), "zawrotka: krawężnik kolizja");
            }
            Kol(ZawX - ZawR, ZawX + ZawR, YJ - .3f, YJ, ZawZ - ZawR, ZawZ + ZawR);
        }
        // krawężniki (zach. z przerwą na zjazd w łuk wiaduktu z 52.4…61.6)
        Kraweznik(KJX0 - .2f, KJX0, KZ0, 52.4f); Kraweznik(KJX0 - .2f, KJX0, 61.6f, KZ1 + .2f);
        Kraweznik(KJX1, KJX1 + .2f, KZ0, KZ1 + .2f);
        Kraweznik(KJX0 - .2f, KJX1 + .2f, KZ0 - .2f, KZ0);
        // chodniki: zach. (przy arkadach), wsch. (przy pierzei), pd. (przejście do zaułka), wokół zawrotki
        Plyta(WiaduktX1, KJX0 - .2f, 48f, 52.4f, YC, mPlyty, "chodniki");
        Plyta(WiaduktX1, KJX0 - .2f, 61.6f, ZawZ - ZawR + .5f, YC, mPlyty, "chodniki");
        Plyta(KJX1 + .2f, KCX1, 50.3f, 108.5f, YC, mPlyty, "chodniki");
        Plyta(WiaduktX1, -33f, 48f, KZ0 - .2f, YC, mPlytyCiemne, "chodniki");
        Plyta(KCX1, -33f, 50.3f, 51.5f, YC, mPlytyCiemne, "chodniki");
        Plyta(-37f, -33f, 51.5f, 108.5f, YC, mPlytyCiemne, "chodniki");                 // zaułek za pierzeją
        Plyta(KJX1 + .2f, -33f, 108.5f, 110.6f, YC, mPlyty, "chodniki");
        // zjazd w łuk wiaduktu (asfalt od jezdni do posadzki korytarza y 0.15 — obniżony krawężnik)
        G("jezdnia", ulica);
        BR(new Vector3((WiaduktX1 + KJX0) * .5f, (YJ + .15f) * .5f - .1f, 57f), new Vector3(KJX0 - WiaduktX1 + .02f, .2f, 9.2f), Quaternion.Euler(0, 0, (Mathf.Atan2(.13f, KJX0 - WiaduktX1) * Mathf.Rad2Deg)), mAsfalt);
        KolR(new Vector3((WiaduktX1 + KJX0) * .5f, (YJ + .15f) * .5f - .1f, 57f), new Vector3(KJX0 - WiaduktX1 + .02f, .2f, 9.2f), Quaternion.Euler(0, 0, (Mathf.Atan2(.13f, KJX0 - WiaduktX1) * Mathf.Rad2Deg)), "zjazd w łuk: kolizja");
        BX(KJX0 - .2f, KJX0, YJ - .25f, YJ + .03f, 52.4f, 61.6f, mKraweznik, 1);
        // trawniki wokół zawrotki i do skraju lasu (las = „horyzont”)
        Plyta(KJX1 + .2f, -33f, 110.6f, 126f, YC - .03f, mTrawa, "trawniki");
        Plyta(WiaduktX1, KJX1 + .2f, 121.4f, 126f, YC - .03f, mTrawa, "trawniki");
        Plyta(WiaduktX1, ZawX - ZawR, ZawZ - ZawR + .5f, 121.4f, YC - .03f, mTrawa, "trawniki");
        // oznakowanie poziome Kolejowej (sygnalizację i znaki stawia wykonawca „ruch”)
        G("oznakowanie", ulica);
        float osK = (KJX0 + KJX1) * .5f;
        for (float z = KZ0 + 2f; z < KZ1 - 3f; z += 6f) { if (z > 65.5f && z < 72f || z > 89.5f && z < 96f) continue; BX(osK - .06f, osK + .06f, YJ, YJ + .006f, z, z + 3f, mFarbaBiala, 1); }
        Zebra(KJX0, KJX1, 67f, 71f, true); Zebra(KJX0, KJX1, 91f, 95f, true);             // naprzeciw łuków z 63.7…74.3 i 87.7…98.3
        foreach (var z in new[] { 67f, 91f })
        {
            BX(KJX0 - .2f, KJX0, YJ - .25f, YJ + .03f, z, z + 4f, mKraweznik, 1);
            BX(KJX1, KJX1 + .2f, YJ - .25f, YJ + .03f, z, z + 4f, mKraweznik, 1);
            G("chodniki", ulica);
            BR(new Vector3(KJX0 - .2f - .55f, (YC + YJ) * .5f + .015f, z + 2f), new Vector3(1.1f, .05f, 4f), Quaternion.Euler(0, 0, -7f), mPlyty);
            BR(new Vector3(KJX1 + .2f + .55f, (YC + YJ) * .5f + .015f, z + 2f), new Vector3(1.1f, .05f, 4f), Quaternion.Euler(0, 0, 7f), mPlyty);
        }
        foreach (float z in new[] { 56f, 78f, 100f }) { Kratka(KJX0 + .35f, z, true); Kratka(KJX1 - .35f, z, true); }
        G("kratki i włazy", ulica);
        foreach (float z in new[] { 64f, 86f }) Walec(new Vector3(osK, YJ, z), new Vector3(osK, YJ + .012f, z), .36f, mZeliwo, 16);
        // barierka (U-12) na końcu zawrotki, za nią skraj lasu
        G("barierka zawrotki", ulica);
        var pasy = TeksturaPasy("barierka_bialo_czerwona", new Color(.82f, .08f, .08f), Color.white);
        float bz = ZawZ + ZawR + .9f;
        for (float x = ZawX - 4.5f; x <= ZawX + 4.51f; x += 1.5f) BX(x - .06f, x + .06f, YC - .3f, YC + .85f, bz - .06f, bz + .06f, mStalGrafit);
        Plansza(new Vector3(ZawX, YC + .66f, bz - .07f), Vector3.back, 9.4f, .3f, pasy);
        Plansza(new Vector3(ZawX, YC + .66f, bz + .07f), Vector3.forward, 9.4f, .3f, pasy);
        Kol(ZawX - 4.8f, ZawX + 4.8f, YC, YC + 1.1f, bz - .1f, bz + .1f);

        // ---- otoczenie galerii (zach. od wiaduktu)
        Plyta(GX1, WiaduktX0, 48f, PZ1, YC, mPlyty, "chodniki");                          // chodnik wsch. galerii (do arkad), plac przed wejściem
        G("plac przed wejściem", ulica);
        BX(GX1, WiaduktX0, YC - .02f, YC + .004f, 63.7f, 74.3f, mKostka, 1);                // przed wejściem głównym — kostka granitowa naprzeciw łuku
        BX(GX1, WiaduktX0, YC - .02f, YC + .005f, 63.3f, 63.7f, mPlytyCiemne, 1); BX(GX1, WiaduktX0, YC - .02f, YC + .005f, 74.3f, 74.7f, mPlytyCiemne, 1);
        Plyta(GX0, GX1, 48f, GZ0, YC, mPlyty, "chodniki");                                 // ścieżka pd. (za pierzeją ulicy głównej)
        Plyta(GX0, GX1, GZ1, DZ0 - .2f, YC, mPlyty, "chodniki");                           // plac przed wejściem pn. i rampa
        Plyta(GX0, GX1, 51f, GZ1, YC - .02f, mBeton, "grunt pod budynkami");               // zabezpieczenie pod galerią (przykryte posadzką)
        Plyta(KCX1, -37f, 51.5f, 108.5f, YC - .02f, mBeton, "grunt pod budynkami");        // i pod pierzeją
        // dojazd od ulicy S1 (dzielnica) wzdłuż pn. ściany galerii
        Plyta(GX0, GX1, DZ0, DZ1, YJ, mAsfalt, "jezdnia");
        Kraweznik(GX0, GX1, DZ0 - .2f, DZ0);
        foreach (float x in new[] { -104f, -90f, -80f }) Kratka(x, DZ0 + .35f, false);
        G("oznakowanie", ulica);
        for (float x = GX0 + 1f; x < GX1 - 4f; x += 6f) BX(x, x + 3f, YJ, YJ + .006f, (DZ0 + DZ1) * .5f - .06f, (DZ0 + DZ1) * .5f + .06f, mFarbaBiala, 1);
        Zebra(-96.2f, -89.8f, DZ0, DZ1, false);                                            // przejście naprzeciw wejścia pn.

        BraxClaudeOsiedle.Przejscia.Add(new BraxClaudeOsiedle.Przejscie { srodek = new Vector3(osK, YJ, 69f), wzdluzJezdni = Vector3.forward, szerokoscJezdni = KJX1 - KJX0, zeSwiatlami = false });
        BraxClaudeOsiedle.Przejscia.Add(new BraxClaudeOsiedle.Przejscie { srodek = new Vector3(osK, YJ, 93f), wzdluzJezdni = Vector3.forward, szerokoscJezdni = KJX1 - KJX0, zeSwiatlami = false });
        BraxClaudeOsiedle.Przejscia.Add(new BraxClaudeOsiedle.Przejscie { srodek = new Vector3(-93f, YJ, (DZ0 + DZ1) * .5f), wzdluzJezdni = Vector3.right, szerokoscJezdni = DZ1 - DZ0, zeSwiatlami = false });
    }

    // ======================================================================== PARKING, PLAC USTAWEK
    const float PlacX0 = -111f, PlacX1 = -95f, PlacZ0 = 110.5f, PlacZ1 = 136.5f;     // 16 × 26 m (≥ 12 × 20 m × k = 15.5 × 25.9)
    static void ParkingIPlac()
    {
        Plyta(-95f, GX1, DZ1, PZ1, YJ, mAsfalt, "parking");
        Kraweznik(GX1 - .2f, GX1, DZ1, PZ1); Kraweznik(-95f, GX1, PZ1, PZ1 + .2f);
        // plac ustawek: płyty betonowe, żółta obwódka, środek pusty
        Plyta(PlacX0, PlacX1, PlacZ0, PlacZ1, YJ + .01f, mBeton, "plac ustawek");
        Plyta(GX0, PlacX0, DZ1, PZ1, YJ + .01f, mBeton, "plac ustawek");
        Kraweznik(GX0, -95f, PZ1, PZ1 + .2f);
        G("plac ustawek", ulica);
        foreach (var (x0, x1, z0, z1) in new[] { (PlacX0, PlacX1, PlacZ0, PlacZ0 + .15f), (PlacX0, PlacX1, PlacZ1 - .15f, PlacZ1), (PlacX0, PlacX0 + .15f, PlacZ0, PlacZ1), (PlacX1 - .15f, PlacX1, PlacZ0, PlacZ1) })
            BX(x0, x1, YJ + .01f, YJ + .017f, z0, z1, mFarbaZolta, 1);
        var tz = TeksturaNapisu("plac_manewrowy", 1024, 128, new Color(0, 0, 0, 0), new Color(.92f, .78f, .15f), "PLAC MANEWROWY", .62f);
        G("plac ustawek", ulica);
        PlanszaPozioma(new Vector3((PlacX0 + PlacX1) * .5f, YJ + .018f, PlacZ0 + 1.6f), 9f, 1.1f, 0f, tz);
        // słupki: od dojazdu (z przerwą na wjazd) i od parkingu
        G("słupki i stojaki", ulica);
        void Slupek(float x, float z)
        {
            Walec(new Vector3(x, YJ, z), new Vector3(x, YJ + .95f, z), .075f, mStalGrafit, 10);
            BX(x - .08f, x + .08f, YJ + .62f, YJ + .7f, z - .08f, z + .08f, mFarbaZolta);
            Kol(x - .08f, x + .08f, YJ, YJ + .95f, z - .08f, z + .08f);
        }
        for (float x = PlacX0 + .6f; x <= PlacX1 - .5f; x += 1.7f) { if (x > -105.5f && x < -101f) continue; Slupek(x, PlacZ0 - .4f); }
        for (float z = PlacZ0 + .8f; z <= PlacZ1 - .5f; z += 1.7f) Slupek(-94.6f, z);
        // parking: 2 rzędy × 7 stanowisk (3.2 × 6.4 m ≈ 2.5 × 5 m × k), aleja 7 m
        G("parking: linie", ulica);
        float[] rz = { -94.2f, -81.2f };
        for (int r = 0; r < 2; r++)
        {
            float x0 = rz[r], x1 = x0 + 6.4f;
            for (int i = 0; i <= 7; i++)
            {
                float z = 111.5f + i * 3.2f;
                BX(x0, x1, YJ, YJ + .006f, z - .06f, z + .06f, mFarbaBiala, 1);
                if (i == 7) break;
                float zc = z + 1.6f; bool wsch = r == 1; float xo = wsch ? x1 - .5f : x0 + .5f;
                BX(xo - .1f, xo + .1f, YJ, YJ + .13f, zc - .8f, zc + .8f, mBeton);
                BraxClaudeOsiedle.MiejscaParkingowe.Add(new BraxClaudeOsiedle.MiejsceParkingowe { srodek = new Vector3((x0 + x1) * .5f, YJ, zc), przod = wsch ? Vector3.left : Vector3.right, rozmiar = new Vector2(3.2f, 6.4f), przyScianie = false });
            }
            BX(r == 0 ? x1 - .06f : x0 - .06f, r == 0 ? x1 + .06f : x0 + .06f, YJ, YJ + .006f, 111.5f, 133.9f, mFarbaBiala, 1);
        }
        var koperta = Gladki("koperta niebieska", new Color(.12f, .32f, .62f), .7f);
        for (int i = 0; i < 2; i++) BX(-81.0f, -75f, YJ + .002f, YJ + .005f, 111.7f + i * 3.2f, 114.5f + i * 3.2f, koperta, 1);
        // trawnik za parkingiem do skraju lasu
        Plyta(GX0, WiaduktX0, PZ1 + .2f, 140f, YC - .03f, mTrawa, "trawniki");
    }

    // ======================================================================== MAŁA ARCHITEKTURA
    static void MalaArchitektura()
    {
        var M = new GameObject("Mała architektura (Kolejowa, galeria, parking)").transform; M.SetParent(ulica, false);
        var latarnie = new List<Vector3>();
        for (float z = 55f; z <= 106f; z += 14f) latarnie.Add(new Vector3(KCX1 - .7f + -2.0f, YC, z));            // wsch. chodnik Kolejowej
        foreach (float z in new[] { 52f, 80f, 94f, 120f, 134f }) latarnie.Add(new Vector3(-72.0f, YC, z));       // chodnik galerii przy arkadach
        foreach (float z in new[] { 116f, 128f }) latarnie.Add(new Vector3(-84.7f, YJ, z));                       // aleja parkingu
        foreach (var p in new[] { new Vector3(-110.6f, YJ + .01f, 111f), new Vector3(-110.6f, YJ + .01f, 136f), new Vector3(-95.4f, YJ + .01f, 136f) }) latarnie.Add(p);
        foreach (var p in latarnie)
            if (Model(M, "Ulica: latarnia", p, 0f, 6.2f, true, "street_lamp_02", "street_lamp_01") == null)
            { G("latarnie (zapas)", M); Walec(p, p + Vector3.up * 6.2f * K, .09f, mStalGrafit, 10); BX(p.x - .3f, p.x + .3f, p.y + 6.2f * K, p.y + 6.2f * K + .15f, p.z - .2f, p.z + .2f, mStalGrafit); }
            else nLamp++;
        // drzewa w kratach (Kolejowa wsch.), na trawniku przy zawrotce i za parkingiem
        var drzewa = new List<(Vector3 p, bool krata)>();
        foreach (float z in new[] { 62f, 76f, 90f, 104f }) drzewa.Add((new Vector3(KJX1 + 1.6f, YC, z), true));
        foreach (float x in new[] { -46f, -40f, -35f }) drzewa.Add((new Vector3(x, YC - .03f, 116f), false));
        foreach (float x in new[] { -108f, -100f, -90f, -82f, -76f }) drzewa.Add((new Vector3(x, YC - .03f, 138.5f), false));
        foreach (var (p, krata) in drzewa)
        {
            var o = Model(M, "Ulica: drzewo", p, BraxRegion.Range(0, 360), 0f, false, "island_tree_03", "drzewo_verdant");
            if (!o) continue;
            var b = BraxRegion.Bounds(o); float maxH = krata ? 8.5f : 11f;
            if (b.size.y > maxH) { o.transform.localScale *= maxH / b.size.y; var b2 = BraxRegion.Bounds(o); o.transform.position += new Vector3(0, p.y - b2.min.y, 0); }
            var c = new GameObject("Ulica: pień kolizja").AddComponent<CapsuleCollider>(); c.transform.SetParent(M, false); c.transform.position = p + Vector3.up * 1.6f; c.radius = .3f; c.height = 3.2f;
            nDrzew++;
            if (krata)
            {
                G("kraty drzew", M);
                BX(p.x - .8f, p.x + .8f, YC, YC + .012f, p.z - .8f, p.z + .8f, mZeliwo, 1);
                for (int i = 0; i < 7; i++) BX(p.x - .75f, p.x + .75f, YC + .012f, YC + .02f, p.z - .66f + i * .22f, p.z - .6f + i * .22f, mStalGrafit, 1);
            }
        }
        // ławki, kosze, hydrant, słupki przed wejściem głównym, stojak rowerowy przy wejściu pn.
        foreach (float z in new[] { 83f, 97f }) Model(M, "Kolejowa: ławka", new Vector3(-49.2f, YC, z), 270f, .95f, true, "painted_wooden_bench", "modular_street_seating");
        foreach (float x in new[] { -104f, -82f }) Model(M, "Galeria: ławka", new Vector3(x, YC, 101.6f), 0f, .95f, true, "painted_wooden_bench", "modular_street_seating");
        foreach (var p in new[] { new Vector3(-72f, YC, 62.5f), new Vector3(-72f, YC, 75.5f), new Vector3(-49f, YC, 70f), new Vector3(-49f, YC, 96f), new Vector3(-86f, YC, 101.4f), new Vector3(-109f, YC, 49.5f) })
            Model(M, "Ulica: kosz", p, BraxRegion.Range(0, 360), .95f, true, "metal_trash_can");
        Model(M, "Ulica: hydrant", new Vector3(-48.6f, YC, 66f), 0f, .8f, true, "fire_hydrant");
        G("słupki i stojaki", M);
        for (float z = 63.9f; z <= 74.2f; z += 1.7f)
        {
            if (z > 66.3f && z < 70.8f) continue;
            Walec(new Vector3(-71.5f, YC, z), new Vector3(-71.5f, YC + .97f, z), .1f, mStalGrafit, 12);
            Kol(-71.62f, -71.38f, YC, YC + 1f, z - .12f, z + .12f);
        }
        for (int i = 0; i < 5; i++)
        {
            float x = -86.5f + i * 1.0f, z = 101.9f;
            Walec(new Vector3(x, YC, z), new Vector3(x, YC + .8f, z), .03f, mStalSzczotk, 8, -1, false);
            Walec(new Vector3(x + .7f, YC, z), new Vector3(x + .7f, YC + .8f, z), .03f, mStalSzczotk, 8, -1, false);
            Walec(new Vector3(x, YC + .8f, z), new Vector3(x + .7f, YC + .8f, z), .03f, mStalSzczotk, 8);
        }
        // parkomat przy parkingu
        G("parkomat", M);
        BK(-75.4f, -74.9f, YC, YC + 1.5f, 110.6f, 111.1f, mStalGrafit);
        var tp = TeksturaNapisu("parkomat", 256, 128, new Color(.08f, .25f, .55f), Color.white, "P · BILET", .45f);
        Plansza(new Vector3(-75.41f, YC + 1.2f, 110.85f), Vector3.left, .42f, .21f, tp);
    }

    // ======================================================================== BRYŁY Z DALEKA (scena główna)
    static void BrylyZDaleka()
    {
        // lekkie bryły galerii i pierzei 0.35 m w głąb od prawdziwych elewacji — widoczne, dopóki obszar się nie wczyta
        G("galeria z daleka", bryly);
        BX(GX0 + .35f, GX1 - .35f, YG, YD + 1.0f, GZ0 + .35f, GZ1 - .35f, mOkladzina, 1);
        BX(GX1 - .4f, GX1 - .36f, 1f, 5.4f, GZ0 + 1f, GZ1 - 1f, mSzkloCiemne); BX(GX1 - .4f, GX1 - .36f, 7.6f, 11.8f, GZ0 + 1f, GZ1 - 1f, mSzkloCiemne);
        BX(VX0 - .5f, VX1 + .5f, YD + 1.0f, YD + 2.6f, VZ0, VZ1, mSzkloDach);
        G("pierzeja z daleka", bryly);
        foreach (var lot in Dzialki)
            BX(KCX1 + .35f, -37f - .3f, YC, YC + lot.wys - .3f, lot.z0 + .1f, lot.z1 - .1f, lot.cegla ? mCegla : mTynkCieply, 1);
        var z = bryly.gameObject.AddComponent<BraxZastepstwoObszaru>();
        z.scena = ScenaObszaru;
        BraxRegion.Note("galeria: bryły z daleka (przełącza BraxZastepstwoObszaru po wczytaniu " + ScenaObszaru + ")");
    }

    // ======================================================================== MIEJSCA LUDZI (format etapu ludzi buildu 14)
    /// <summary>Miejsce obsady / klienta / kibica: gdzie (opis), rola (obsługa, recepcja, ochrona, klient, kibic), rolaKwestii
    /// (klucz kwestii jak w BraxClaudeOsiedleLudzieMiejsca), pozycja stóp, yaw (przód), klipy. W scenie: pusty obiekt
    /// „BRAXHORN miejsce: …” (dla etapu ludzi miasta), lista w Assets/PlayableGenerated/ClaudeRegion/Galeria/miejsca.json.</summary>
    [Serializable] public sealed class MiejsceLudzi { public string gdzie, rola, rolaKwestii; public Vector3 p; public float yaw; public string[] klipy; }
    [Serializable] sealed class MiejscaJson { public List<MiejsceLudzi> miejsca = new List<MiejsceLudzi>(); }
    public static readonly List<MiejsceLudzi> Miejsca = new List<MiejsceLudzi>();
    static readonly string[] KlipyLada = { "work_mid", "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" };
    static readonly string[] KlipyStoi = { "idle_neutral_01", "idle_neutral_02", "idle_neutral_03", "idle_waiting_01" };
    static readonly string[] KlipyKibic = { "idle_neutral_01", "idle_look_around_01", "idle_waiting_01" };
    static void Miejsce(Transform rodzic, string gdzie, string rola, string kwestie, Vector3 p, float yaw, string[] klipy)
    {
        Miejsca.Add(new MiejsceLudzi { gdzie = gdzie, rola = rola, rolaKwestii = kwestie, p = p, yaw = yaw, klipy = klipy });
        var o = new GameObject("BRAXHORN miejsce: " + rola + " · " + gdzie); o.transform.SetParent(rodzic, false);
        o.transform.position = p; o.transform.rotation = Yaw(yaw);
    }
    static void ZapiszMiejsca()
    {
        // kibice placu ustawek: dookoła pustego środka, w pasie przy krawędziach placu
        var K2 = new GameObject("Miejsca ludzi: kibice placu ustawek").transform; K2.SetParent(ulica, false);
        int n = 0;
        for (float z = PlacZ0 + 2f; z <= PlacZ1 - 1.5f; z += 2.6f)
        {
            Miejsce(K2, "plac ustawek — kibic zach. " + (++n), "kibic", "kibic", new Vector3(PlacX0 + .5f, YJ + .01f, z), 90f, KlipyKibic);
            Miejsce(K2, "plac ustawek — kibic wsch. " + n, "kibic", "kibic", new Vector3(PlacX1 + .9f, YJ, z), 270f, KlipyKibic);
        }
        for (float x = PlacX0 + 2f; x <= PlacX1 - 2f; x += 2.6f)
            Miejsce(K2, "plac ustawek — kibic pn. " + (++n), "kibic", "kibic", new Vector3(x, YJ + .01f, PlacZ1 - .5f), 180f, KlipyKibic);
        var json = new MiejscaJson { miejsca = Miejsca.ToList() };
        File.WriteAllText(Dir + "/miejsca.json", JsonUtility.ToJson(json, true));
        Debug.Log("BRAXHORN_GALERIA_MIEJSCA razem=" + Miejsca.Count + " obsada=" + Miejsca.Count(m => m.rola == "obsługa" || m.rola == "recepcja" || m.rola == "ochrona") +
                  " klienci=" + Miejsca.Count(m => m.rola == "klient") + " kibice=" + Miejsca.Count(m => m.rola == "kibic"));
    }

    // ======================================================================== PUNKTY ŚWIATA
    static void PunktySwiata()
    {
        if (!swiat) return;
        swiat.points = swiat.points.Concat(new[]
        {
            new BraxCityPoint { id = "R1_KOLEJOWA", label = "ul. Kolejowa", position = new Vector3((KJX0 + KJX1) * .5f, YJ, 60f) },
            new BraxCityPoint { id = "R1_GALERIA_WEJSCIE", label = "GALERIA BRAXHORN — wejście główne", position = new Vector3(-72.8f, YC, WejscieZ) },
            new BraxCityPoint { id = "R1_GALERIA_ATRIUM", label = "GALERIA BRAXHORN — atrium", position = new Vector3(-93f, YG, 76f) },
            new BraxCityPoint { id = "R1_GALERIA_WEJSCIE_PN", label = "GALERIA BRAXHORN — wejście pn.", position = new Vector3(-93f, YC, 101.5f) },
            new BraxCityPoint { id = "R1_GALERIA_DEWELOPER", label = "BIURO DEWELOPERA (zakup domu)", position = new Vector3(-84f, YG, 75.5f) },
            new BraxCityPoint { id = "R1_GALERIA_BRAX_SPORT", label = "BRAX SPORT", position = new Vector3(-101f, YG, 79f) },
            new BraxCityPoint { id = "R1_GALERIA_FOOD_COURT", label = "FOOD COURT (piętro)", position = new Vector3(-97f, YP, 95f) },
            new BraxCityPoint { id = "R1_LOMBARD", label = "LOMBARD (ul. Kolejowa)", position = new Vector3(KJX1 + 2.5f, YC, 90.5f) },
            new BraxCityPoint { id = "R1_PLAC_USTAWEK", label = "plac ustawek 3 na 3 (za galerią)", position = new Vector3((PlacX0 + PlacX1) * .5f, YJ, (PlacZ0 + PlacZ1) * .5f) },
            new BraxCityPoint { id = "R1_BAR24", label = "BAR 24", position = new Vector3(-6.5f, .15f, 22f) },
        }).ToArray();
        RejestrRuchu();
    }

    // Rejestr jezdni dla wykonawcy „ruch” (scratchpad/plan/RUCH_API.md) — do wpięcia, gdy API będzie w bazie; na razie dziennik.
    static void RejestrRuchu()
    {
        Debug.Log("BRAXHORN_GALERIA_JEZDNIE Kolejowa: x " + KJX0 + "…" + KJX1 + " z " + KZ0 + "…" + KZ1 + " (2 pasy, oś x " + ((KJX0 + KJX1) * .5f) + ", zawrotka (" + ZawX + ", " + ZawZ + ") r " + ZawR +
                  ", wjazd łukiem wiaduktu z 52.4…61.6) | dojazd galerii: z " + DZ0 + "…" + DZ1 + " x " + GX0 + "…" + GX1 + " (od ulicy S1) | parking x −94.2…−74.8 z 111.5…133.9 (14 miejsc)");
    }
}

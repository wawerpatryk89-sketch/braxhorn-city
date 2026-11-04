using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// PIERZEJE ULICY GŁÓWNEJ OD NOWA (właściciel 2.11 po teście 13.1: „elewacja do zmiany”, „budynki migotają”; 11:00 „zawsze
// ściągasz, kładziesz nową, a nie na to”; wzorzec Urban City + film kierunku: gęsta siatka okien z głębią, gzymsy, cokoły,
// witryny, szyldy). Dla 17 budynków z BraxCityArchitecture.StreetBuildings (bez BRAXHORN TOWER — wieżowiec ma własny
// front i drzwi u wykonawcy „wnętrza”):
//   1) ZDEJMUJE stare: grupę „Street frontage” (moduły zestawu + czarne płyty wnęk + szyld-listwa), odsłonięte boki
//      „Exposed … return” tam, gdzie bok widać z ulicy (7 boków), wszystkie „Return course …” (kamienne pasy wystające
//      17 cm przed lico, PODWÓJNE na styku sąsiadów — ta sama bryła dwa razy w tym samym miejscu = migotanie), sufit
//      i boczne ściany holu (lico w płaszczyźnie elewacji = migotanie) i bryłę pięter (lico 15 cm za elewacją
//      zasłaniało szyby okien) — te trzy stawia od nowa cofnięte za ścianę;
//   2) STAWIA nową elewację: jedna ściana z prawdziwymi otworami (bez nakładek), ościeża w głąb muru, okna z ramą,
//      słupkiem i ślemieniem, szyba „pokój za szybą” (shader Braxhorn/UliceOkno — pokoje wyrównane do osi okien),
//      parapety, opaski, nadproża ze zwornikiem albo gzymsiki nadokienne, balustrady francuskie, gzyms nad parterem,
//      gzymsy kondygnacyjne, gzyms wieńczący z kroksztynami, attyka z czapą, lizeny na krawędziach, cokół z granitu,
//      rura spustowa; parter: witryny w ramach aluminiowych (prawdziwe szkło, za nim prawdziwe wnętrze lokalu albo
//      — w budynkach z holem wieżowca — witryna z wnętrzem sklepu za szybą), drzwi (działające, z zestawu) w osi,
//      szyld z liter przestrzennych na listwie, markizy w paski, szyld na wysięgniku, tabliczka z numerem;
//   3) URZĄDZA lokal za witryną (lada, regały z towarem, stoliki/hokery, lampy) i zapisuje punkty pod misje
//      (BraxCityWorld.points: „ulica_<id>_drzwi” przed drzwiami, „ulica_<id>_lada” — miejsce zleceniodawcy za ladą).
// Skala: detale × k (Brax 2.33 m); kondygnacje i obrysy budynków bez zmian (windy, wieżowce, hole w nich osadzone).
public static class BraxUlicePierzeje
{
    const float Parter = 3.75f, Kond = 3.2f, Chodnik = .15f;
    const float Lizena = .42f, GlebOkna = .3f, GlebWitryny = .2f;

    sealed class Styl
    {
        public string nazwa; public Material sciana, parter, detal, stolarka, cokol, zelazo;
        public bool cegla, opaski, gzymsyPieter, balkonFr, kroksztyny; public float szerOkna = 1.4f;
    }
    sealed class Lokal
    {
        public string szyld, typ; public int numer, styl, wariantWitryny = 7; public bool markiza, wysiegnik;
        public Color licoSzyldu = new Color(.95f, .93f, .86f); public Color32 markizaA, markizaB;
        public Lokal(string szyld, string typ, int numer, int styl) { this.szyld = szyld; this.typ = typ; this.numer = numer; this.styl = styl; }
    }
    sealed class Fasada
    {
        public BraxCityArchitecture.Building b; public Vector3 A, R, N; public float W, H, yTop; public int pietra, bays; public float bay;
        public bool front; public Styl st; public Lokal lok; public float drzwiS = -1f; public bool wiezowiec;
        public int ulica = 0;   // bok: −1 = koniec od ulicy przy s = 0, +1 = przy s = W (narożnik z frontem), 0 = front
        public bool gora;        // A 2.11: tylko piętra nad lokalem (nadbudowa NOVA / Nutrition / brama podwórka) — parter robi lokal
        public Rect szkloOkien;  // okna w tym prostokącie (s, y) ze szkłem przezroczystym (mieszkanie Braxa — widok z pokoju)
        public Vector3 P(float s, float y, float d) => A + R * s + Vector3.up * y + N * d;
    }

    // Lokale (szyldy bez powtórzeń — audyt: 4× BRAXHORN, 2× KAWIARNIA, 2× PRACOWNIA; nazwy rodzajowe, bez marek).
    // Numery: strona południowa nieparzyste, północna parzyste, od morza (wschód) na zachód.
    static readonly Dictionary<string, Lokal> Lokale = new Dictionary<string, Lokal>
    {
        { "apothecary",          new Lokal("APTEKA SOBCZAK", "apteka", 5, 3) { wysiegnik = true, licoSzyldu = new Color(.10f, .55f, .28f), wariantWitryny = 7 } },
        { "corner_store",        new Lokal("SOBCZAK DELIKATESY", "market", 7, 0) { markiza = true, markizaA = BraxUliceKit.C(.12f, .34f, .20f), markizaB = BraxUliceKit.C(.92f, .90f, .84f), wariantWitryny = 7 } },
        { "bar",                 new Lokal("BAR 24", "bar", 13, 2) { wysiegnik = true, licoSzyldu = new Color(.92f, .70f, .22f) } },
        { "restaurant",          new Lokal("BISTRO", "bistro", 15, 4) { markiza = true, markizaA = BraxUliceKit.C(.55f, .10f, .10f), markizaB = BraxUliceKit.C(.93f, .91f, .86f), wariantWitryny = 6 } },
        { "workshop_shop",       new Lokal("KWIACIARNIA", "kwiaty", 17, 1) { markiza = true, markizaA = BraxUliceKit.C(.20f, .42f, .28f), markizaB = BraxUliceKit.C(.95f, .94f, .90f) } },
        { "hotel",               new Lokal("HOTEL", "hotel", 19, 5) { wysiegnik = true, licoSzyldu = new Color(.86f, .72f, .40f), wariantWitryny = 6 } },
        { "viaduct_corner",      new Lokal("KANTOR", "kantor", 21, 3) { wysiegnik = true, licoSzyldu = new Color(.95f, .80f, .20f) } },
        { "workshop",            new Lokal("WARSZTAT ROWEROWY", "warsztat", 23, 2) },
        { "estate_corner",       new Lokal("SOBCZAK KAWIARNIA", "kawiarnia", 25, 0) { markiza = true, markizaA = BraxUliceKit.C(.32f, .20f, .13f), markizaB = BraxUliceKit.C(.90f, .84f, .72f), wariantWitryny = 6 } },
        { "bakery",              new Lokal("SOBCZAK PIEKARNIA", "piekarnia", 6, 4) { markiza = true, markizaA = BraxUliceKit.C(.62f, .38f, .12f), markizaB = BraxUliceKit.C(.95f, .92f, .84f) } },
        { "gallery",             new Lokal("BRAXHORN FITNESS", "fitness", 10, 3) { wysiegnik = true, licoSzyldu = new Color(.93f, .45f, .62f), wariantWitryny = 5 } },   // pkt 59: fitness Nyxy (wcześniej GALERIA SZTUKI)
        { "office_entrance",     new Lokal("POCZTA", "poczta", 12, 1) { wysiegnik = true, licoSzyldu = new Color(.95f, .78f, .10f) } },
        { "apartments_1",        new Lokal("KSIĘGARNIA", "ksiegarnia", 14, 0) { wariantWitryny = 3 } },
        { "apartments_2",        new Lokal("SOBCZAK CUKIERNIA", "cukiernia", 16, 5) { markiza = true, markizaA = BraxUliceKit.C(.78f, .45f, .55f), markizaB = BraxUliceKit.C(.97f, .95f, .93f) } },
        { "apartments_3",        new Lokal("OPTYK", "optyk", 18, 4) { wysiegnik = true, licoSzyldu = new Color(.15f, .35f, .62f) } },
        { "estate_apartments_1", new Lokal("SOBCZAK WARZYWA OWOCE", "warzywniak", 20, 1) { markiza = true, markizaA = BraxUliceKit.C(.18f, .40f, .16f), markizaB = BraxUliceKit.C(.95f, .93f, .86f) } },
        { "estate_apartments_2", new Lokal("PIZZERIA", "pizzeria", 22, 2) { markiza = true, markizaA = BraxUliceKit.C(.60f, .12f, .10f), markizaB = BraxUliceKit.C(.20f, .38f, .18f), wariantWitryny = 6 } },
    };
    // Boki widoczne z ulicy (bez sąsiada; analiza obrysów kolizji oryginału): zachód / wschód.
    static readonly HashSet<string> BokZachod = new HashSet<string> { "estate_corner", "viaduct_corner", "apartments_3", "estate_apartments_2" };
    static readonly HashSet<string> BokWschod = new HashSet<string> { "workshop", "bar", "estate_apartments_1" };

    static Styl[] style;
    static int nOkien, nWitryn, nLokali, nBokow;

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxUliceKit.WyczyscOkna(); BraxUliceKit.Usuniete.Clear();
        nOkien = nWitryn = nLokali = nBokow = 0;
        var root = BraxRegion.Root(cityRoot, "CLAUDE ulice: pierzeje (elewacje od nowa)");
        // Grafiki WAWER / SOBCZAK muszą należeć do miasta. Dawniej kontener
        // tworzył się przy korzeniu sceny i mógł wypaść z paczki obszaru.
        szyldyFirm = new GameObject("CLAUDE: szyldy firm z grafik (WAWER, SOBCZAK)").transform;
        szyldyFirm.SetParent(root, false);
        var kolizje = new GameObject("kolizje parterów i brył").transform; kolizje.SetParent(root, false);
        style = Style();
        var wieze = new HashSet<string>(BraxCityStreetSkyline.HighRises.Select(h => h.id));
        var gotowe = new List<string>();
        foreach (var b in BraxCityArchitecture.StreetBuildings)
        {
            if (b.id == "quay_corner" || !Lokale.TryGetValue(b.id, out var lok)) continue;
            var bg = cityRoot.Find("Building " + b.id);
            if (!bg) { BraxRegion.Note("pierzeje: brak budynku " + b.id); continue; }
            ZdejmijStare(bg, b);
            bool wiez = wieze.Contains(b.id);
            CofnijHol(bg, b, wiez, kolizje);
            var f = Front(b, lok, wiez);
            BraxUliceKit.UstawGrupe("Elewacja " + lok.szyld + " (" + b.id + ")");
            Elewacja(f, kolizje);
            BraxUliceKit.UstawGrupe("Szyldy i detale " + lok.szyld, 90f);
            Szyldy(f);
            if (BokZachod.Contains(b.id)) { var s = Bok(b, lok, true); BraxUliceKit.UstawGrupe("Elewacja boczna " + lok.szyld); Elewacja(s, kolizje); nBokow++; }
            if (BokWschod.Contains(b.id)) { var s = Bok(b, lok, false); BraxUliceKit.UstawGrupe("Elewacja boczna " + lok.szyld); Elewacja(s, kolizje); nBokow++; }
            // wnętrze Bar 24 robi wykonawca „galeria” (ETAP-GALERIA p. 4) — tu tylko fasada
            if (wiez && lok.typ == "apteka")
            {
                // #64: APTEKA była wieżowcem i przez !wiez całe wnętrze apteki
                // pomijano. Tylko apteka: własna ekspozycja w bocznych zatokach,
                // środkowy korytarz do czynnej windy pozostaje wolny.
                BraxUliceKit.UstawGrupe("Wnętrze lokalu APTEKA — leki w wieżowcu", 45f);
                AptekaWiezowca(bg, cityRoot);
                nLokali++;
            }
            else if (!wiez && b.id != "bar")
            {
                BraxUliceKit.UstawGrupe("Wnętrze lokalu " + lok.szyld, 45f);
                if (lok.typ == "fitness") { try { BraxMiastoFitness.Buduj(bg, f.A + f.R * f.drzwiS); } catch (System.Exception e) { Debug.LogWarning("BRAXHORN_FITNESS DO POPRAWKI " + e); } }
                else Wnetrze(f, bg, kolizje);
                nLokali++;
            }
            Drzwi(bg, b, lok, f);
            gotowe.Add(lok.szyld + " nr " + lok.numer);
        }
        // A 2.11 (właściciel 15:39–15:40: „elewacja od siłowni CAŁA wymieniona… starą zwalić, nie nakładać”): stare bryły,
        // których lico leżało w płaszczyźnie nowej elewacji (dach „Weatherproof roof”, grafitowe „shared structural return”
        // na styku sąsiadów — pas pod attyką i pionowe pasy na granicach budynków = migotanie), zdjęte i postawione od nowa
        // 6 cm za elewacją.
        int skrocone = 0;
        foreach (var b in BraxCityArchitecture.StreetBuildings)
        {
            if (b.id == "quay_corner") continue;
            var bg = cityRoot.Find("Building " + b.id); if (!bg) continue;
            foreach (Transform c in bg.Cast<Transform>().ToArray())
                if (c.name == "Weatherproof roof" || c.name == "West shared graphite structural return" || c.name == "East shared graphite structural return")
                    if (Cofnij(c, b.far ? b.z0 : b.z1, b.far ? 1f : -1f)) skrocone++;
        }
        // NADBUDOWY nad lokalami (Nutrition, NOVA, brama podwórka): stara elewacja od ulicy zdjęta, nowa w tym samym generatorze
        // (bez parteru — parter to lokal A), boki i dach nadbudowy cofnięte 6 cm za lico. NOVA osobno (pod grupą nadbudowy —
        // mieszkanie Braxa odczytuje z niej okna: okna mieszkania ze szkłem przezroczystym).
        var nadbudowy = new List<(string id, float x0, float x1, float z0, float z1, float baseY, int floors, bool far, int styl)>
        {
            ("BRAXHORN NUTRITION", 14.5f, 26f, 11.6f, 20.6f, 5.1f, 6, false, 0),
            ("Courtyard gateway", 2f, 18f, 36.1f, 48f, 4.6f, 6, true, 1),
        };
        foreach (var n in nadbudowy) Nadbudowa(cityRoot, n.id, n.x0, n.x1, n.z0, n.z1, n.baseY, n.floors, n.far, n.styl, Rect.zero, ref skrocone);
        int siatek = BraxUliceKit.Zapisz(root, "pierzeje");
        var gNova = cityRoot.Find("NOVA preserved interior upper storeys");
        if (gNova)
        {
            BraxUliceKit.Start();
            Nadbudowa(cityRoot, "NOVA", 32f, 52f, 36.1f, 62f, 5.1f, 5, true, 2, Rect.MinMaxRect(6.2f, Parter, 19.4f, Parter + Kond), ref skrocone);   // pkt 46: mieszkanie Braxa x 38.2…51.4
            siatek += BraxUliceKit.Zapisz(gNova, "pierzeje_nova_gora");
        }
        BraxRegion.Note("pierzeje: cofnięte stare bryły w płaszczyźnie elewacji: " + skrocone);
        string usun = string.Join("; ", BraxUliceKit.Usuniete.GroupBy(x => x).Select(g => g.Key + (g.Count() > 1 ? " ×" + g.Count() : "")));
        BraxRegion.Note("pierzeje: budynków " + gotowe.Count + " (" + string.Join(", ", gotowe) + "), okien " + nOkien + ", witryn " + nWitryn + ", boków " + nBokow + ", lokali urządzonych " + nLokali + ", siatek " + siatek);
        Debug.Log("BRAXHORN_ULICE_PIERZEJE budynki=" + gotowe.Count + " okna=" + nOkien + " witryny=" + nWitryn + " boki=" + nBokow + " lokale=" + nLokali + " siatki=" + siatek + " k=" + BraxUliceKit.K.ToString("F3"));
        Debug.Log("BRAXHORN_ULICE_USUNIETE pierzeje: " + BraxUliceKit.Usuniete.Count + " obiektów — " + usun);
    }

    // Stara bryła z licem w płaszczyźnie elewacji: zdjęta (DestroyImmediate) i postawiona od nowa krótsza o 6 cm od ulicy.
    static bool Cofnij(Transform t, float zUlicy, float wGlab)
    {
        var mr = t.GetComponent<MeshRenderer>(); if (!mr) return false;
        var bb = mr.bounds; var mat = mr.sharedMaterial;
        float zNear = Mathf.Abs(bb.min.z - zUlicy) < Mathf.Abs(bb.max.z - zUlicy) ? bb.min.z : bb.max.z;
        if (Mathf.Abs(zNear - zUlicy) > .05f) return false;
        var lo = bb.min; var hi = bb.max;
        if (wGlab > 0) lo.z = zUlicy + .06f; else hi.z = zUlicy - .06f;
        if (hi.z - lo.z < .05f) return false;
        string nazwa = t.name; var parent = t.parent;
        BraxUliceKit.Usun(t.gameObject, "lico w płaszczyźnie elewacji (migotanie) — cofnięte 6 cm: " + nazwa);
        if (mat) BraxCityArchitecture.Box(nazwa + " (cofnięte 6 cm za elewację)", parent, (lo + hi) * .5f, hi - lo, mat, false);
        return true;
    }

    static void Nadbudowa(Transform cityRoot, string id, float x0, float x1, float z0, float z1, float baseY, int floors, bool far, int styl, Rect szklo, ref int skrocone)
    {
        var g = cityRoot.Find(id + " preserved interior upper storeys");
        if (!g) { BraxRegion.Note("pierzeje: brak nadbudowy " + id); return; }
        var stara = g.Find(far ? "Upper rear" : "Upper front");
        if (stara) BraxUliceKit.Usun(stara.gameObject, "stara elewacja nadbudowy od ulicy " + id);
        foreach (Transform c in g.Cast<Transform>().ToArray())
            if (c.name == "Upper side west" || c.name == "Upper side east" || c.name == "Upper roof")
                if (Cofnij(c, far ? z0 : z1, far ? 1f : -1f)) skrocone++;
        var b = new BraxCityArchitecture.Building("nadbudowa_" + id, id, x0, x1, z0, z1, floors, far);
        var lok = new Lokal("", "nadbudowa", 0, styl);
        var f = Front(b, lok, false);
        f.gora = true; f.szkloOkien = szklo;
        f.A = new Vector3(f.A.x, baseY - Parter, f.A.z);
        f.pietra = floors + 1; f.yTop = Parter + f.pietra * Kond; f.H = Parter + 3.75f + floors * 3.2f + .75f;
        BraxUliceKit.UstawGrupe("CLAUDE A: elewacja nadbudowy " + id);
        Elewacja(f, null);
        nBokow++;
    }

    // DETALE WZORCA URBAN CITY (× k): klimatyzatory pod częścią okien pięter (wsporniki, kratka wentylatora), schody pożarowe
    // na bokach widocznych z ulicy (podesty z kraty na każdym piętrze, balustrady, biegi skośne, wsporniki, drabina opuszczana
    // kończąca się 3 m nad chodnikiem), na dachu kominy z czapą i nasadami, anteny (maszt, Yagi, talerz). Bez płaszczyzn
    // na płaszczyznach: każdy element to bryła odsunięta od muru (wsporniki dotykają muru krawędzią).
    static void Detale(Fasada f, List<Otwor> otwory)
    {
        float k = BraxUliceKit.K; var st = f.st; var z = BraxUliceKit.S(st.zelazo);
        int ziarno = Mathf.Abs((f.lok.szyld + f.b.id + (f.front ? "F" : "B")).GetHashCode());
        var rng = new System.Random(ziarno);
        // klimatyzatory (front, piętra od 2.): ok. 1 na 8 okien, pod parapetem
        if (f.front && !f.wiezowiec)
        {
            var obud = BraxUliceKit.Mat("klimatyzator: obudowa biała", new Color(.86f, .87f, .86f), .5f, .1f);
            var krat = BraxUliceKit.Mat("klimatyzator: kratka wentylatora", new Color(.18f, .19f, .2f), .5f, .5f);
            foreach (var o in otwory)
            {
                if (o.witryna || o.drzwi || o.r.yMin < Parter + Kond + .5f || rng.NextDouble() > .13) continue;
                float c = o.r.center.x, y1 = o.r.yMin - .2f, w = .8f * k, h = .55f * k, d = .3f * k;
                var cc = f.P(c, y1 - h * .5f, .06f + d * .5f);
                BraxUliceKit.S(obud).OBox(cc, f.R, Vector3.up, f.N, w, h, d);
                var kr = BraxUliceKit.S(krat); var fc = f.P(c - w * .18f, y1 - h * .5f, .06f + d + .005f);
                kr.OBox(fc, f.R, Vector3.up, f.N, h * .7f, h * .7f, .01f);
                foreach (float sx in new[] { c - w * .4f, c + w * .4f })
                { z.Rura(f.P(sx, y1 - h, 0f), f.P(sx, y1 - h, .06f + d), .012f, 6); z.Rura(f.P(sx, y1 - h - .35f, 0f), f.P(sx, y1 - h, .06f + d * .9f), .01f, 6); }
            }
        }
        // schody pożarowe na boku widocznym z ulicy (środkowe przęsło), od 1. piętra w górę
        if (!f.front && !f.gora && f.N.x < -.5f && f.pietra >= 4 && f.bays >= 3)   // tylko boki zachodnie (przy wiadukcie i na końcu ulicy — wolne pole, bez siłowni)
        {
            int bay = f.bays / 2; float s0 = bay * f.bay + .15f, s1 = (bay + 1) * f.bay - .15f, dep = 1.25f;
            var krata = BraxUliceKit.Mat("schody pożarowe: krata podestu", new Color(.08f, .085f, .09f), .6f, .5f);
            for (int fl = 0; fl < f.pietra; fl++)
            {
                float y = Parter + fl * Kond - .06f;
                BraxUliceKit.S(krata).OBox(f.P((s0 + s1) * .5f, y, .05f + dep * .5f), f.R, Vector3.up, f.N, s1 - s0, .05f, dep);
                float yr = y + 1.0f * k;
                z.Rura(f.P(s0, yr, .05f + dep), f.P(s1, yr, .05f + dep), .022f, 6);
                z.Rura(f.P(s0, y + .5f * k, .05f + dep), f.P(s1, y + .5f * k, .05f + dep), .015f, 6);
                foreach (float sx in new[] { s0, s1 }) { z.Rura(f.P(sx, yr, .05f), f.P(sx, yr, .05f + dep), .022f, 6); }
                for (float sx = s0; sx <= s1 + .01f; sx += .3f) z.Rura(f.P(sx, y + .03f, .05f + dep), f.P(sx, yr, .05f + dep), .01f, 4, false);
                // wsporniki (zastrzały do muru)
                foreach (float sx in new[] { s0 + .1f, s1 - .1f }) z.Rura(f.P(sx, y - .9f, 0f), f.P(sx, y - .03f, .05f + dep - .1f), .025f, 6);
                if (fl == f.pietra - 1) break;
                // bieg schodów do następnego podestu (w lewej połowie podestu, wzdłuż elewacji)
                float a0 = s0 + .1f, a1 = (s0 + s1) * .5f, b0 = y + .05f, b1 = y + Kond, dz = .05f + dep * .5f;
                int stop = 12;
                for (int i = 0; i < 2; i++) { float dd = dz + (i == 0 ? -.3f : .3f); z.Rura(f.P(a1, b0, dd), f.P(a0, b1, dd), .02f, 6); }
                for (int i = 1; i < stop; i++)
                {
                    float t = i / (float)stop, sx = Mathf.Lerp(a1, a0, t), sy = Mathf.Lerp(b0, b1, t);
                    BraxUliceKit.S(krata).OBox(f.P(sx, sy, dz), f.R, Vector3.up, f.N, .2f, .03f, .6f);
                }
            }
            // drabina opuszczana z 1. podestu: kończy się 3 m nad chodnikiem
            float ld = (s0 + s1) * .5f + .3f;
            foreach (float dd in new[] { .05f + dep - .2f, .05f + dep - .6f }) z.Rura(f.P(ld, Parter - .06f, dd), f.P(ld, 3.0f, dd), .018f, 6);
            for (float y = 3.1f; y < Parter - .1f; y += .3f) z.Rura(f.P(ld, y, .05f + dep - .6f), f.P(ld, y, .05f + dep - .2f), .012f, 4, false);
        }
        // dach: kominy z czapą i nasadami (2–3 na budynek), antena co drugi budynek, talerz satelitarny
        if (!f.front && !f.gora || f.wiezowiec) return;   // na budynkach z wieżowcem dach zajmuje wieża
        var komin = st.cegla ? st.sciana : st.detal;
        var czapa = BraxUliceKit.Mat("komin: czapa betonowa", new Color(.55f, .55f, .53f), .9f);
        var blacha = BraxUliceKit.Mat("blacha tytanowo-cynkowa (rury, obróbki)", new Color(.46f, .48f, .49f), .5f, .7f);
        int ile = f.W > 13f ? 3 : 2;
        for (int i = 0; i < ile; i++)
        {
            float sc = f.W * (i + .5f) / ile + (float)(rng.NextDouble() - .5) * 1.2f, dd = -(1.6f + (float)rng.NextDouble() * 1.4f), w = .75f * k, hk = 1.5f + (float)rng.NextDouble() * .6f;
            var cc = f.P(sc, f.H - .7f + hk * .5f, dd);
            BraxUliceKit.S(komin).OBox(cc, f.R, Vector3.up, f.N, w, hk, w);
            BraxUliceKit.S(czapa).OBox(f.P(sc, f.H - .7f + hk + .05f, dd), f.R, Vector3.up, f.N, w + .12f, .1f, w + .12f);
            foreach (float o2 in new[] { -.16f, .16f }) BraxUliceKit.S(czapa).Walec(f.P(sc + o2, f.H - .7f + hk + .1f, dd), .09f, .32f, 10);
        }
        if (rng.NextDouble() < .55)
        {
            float sa = f.W * (.3f + (float)rng.NextDouble() * .4f), da = -3.2f;
            var p0 = f.P(sa, f.H - .5f, da); var p1 = p0 + Vector3.up * 3.6f;
            BraxUliceKit.S(blacha).Rura(p0, p1, .03f, 8);
            for (int i = 0; i < 4; i++) { var py = p1 - Vector3.up * (.25f + i * .28f); BraxUliceKit.S(blacha).Rura(py - f.R * (.55f - i * .08f), py + f.R * (.55f - i * .08f), .009f, 4); }
            if (rng.NextDouble() < .6)
            {
                var pd = f.P(sa + 1.2f, f.H + .3f, da + .4f);
                BraxUliceKit.S(blacha).Rura(pd - Vector3.up * .8f, pd, .025f, 6);
                var kier = (f.N * .6f + Vector3.up * .5f).normalized;
                BraxUliceKit.S(czapa).Rura(pd, pd + kier * .06f, .38f * k * .5f, 14);
                BraxUliceKit.S(blacha).Rura(pd + kier * .06f, pd + kier * .45f, .01f, 4);
            }
        }
    }

    // ---------------- style ----------------
    static Styl[] Style()
    {
        const string CR = "Assets/Source/CityRebuild/textures/", OS = "Assets/Source/Claude/Osiedle/textures/";
        Material Cegla(string nazwa, Color odcien)
        {
            try { var m = BraxCityArchitecture.SurfaceTexture("Ulice: " + nazwa, "brick_wall_001", "brick_wall_001_diffuse_2k.jpg", 2.6f * BraxUliceKit.K / 1.294f); m.SetColor("_Color", odcien); EditorUtility.SetDirty(m); return m; }
            catch (Exception) { return BraxUliceKit.Mat(nazwa + " (bez tekstury)", new Color(.52f, .27f, .20f), .9f); }
        }
        Material Tynk(string nazwa, Color odcien, Color zam) => BraxUliceKit.Tex(nazwa, 2.4f, odcien, zam, .92f, "white_plaster_02", "plastered_wall_04", "painted_plaster_wall");
        Material Kamien(string nazwa, Color odcien, Color zam) => BraxUliceKit.Tex(nazwa, 1.8f, odcien, zam, .88f, "concrete_wall_008", "granite_tile_02");
        var granit = BraxUliceKit.Tex("cokół granit ciemny", 1.4f, new Color(.55f, .55f, .56f), new Color(.24f, .24f, .25f), .7f, "granite_tile_02", "granite_tile");
        var boniowanie = BraxUliceKit.TexPlik("parter boniowany kamień", OS, "rough_block_wall", 2.2f, new Color(.86f, .84f, .80f), new Color(.55f, .53f, .50f), .9f);
        var piaskowiec = BraxUliceKit.TexPlik("piaskowiec bloki", OS, "white_sandstone_blocks_02", 2.6f, new Color(.98f, .93f, .84f), new Color(.80f, .74f, .62f), .9f);
        var zelazo = BraxUliceKit.Mat("żeliwo i stal malowana (balustrady, wsporniki)", new Color(.07f, .075f, .08f), .55f, .45f);
        var bialaStol = BraxUliceKit.Mat("stolarka okienna biała", new Color(.90f, .90f, .88f), .45f);
        var antracyt = BraxUliceKit.Mat("stolarka okienna antracyt", new Color(.13f, .14f, .15f), .45f, .2f);
        var zielona = BraxUliceKit.Mat("stolarka okienna butelkowa zieleń", new Color(.10f, .20f, .15f), .5f);
        var braz = BraxUliceKit.Mat("stolarka okienna orzech", new Color(.27f, .17f, .10f), .55f);
        var klinkier = BraxUliceKit.Tex("klinkier grafitowy", 2.0f, null, new Color(.22f, .20f, .19f), .85f, "exterior_wall_cladding_02", "exterior_wall_cladding");
        return new[]
        {
            new Styl { nazwa = "kamienica z czerwonej cegły", sciana = Cegla("cegła czerwona", new Color(1f, .96f, .93f)), parter = boniowanie, detal = Kamien("detal: jasny kamień", new Color(.98f, .95f, .89f), new Color(.82f, .79f, .72f)), stolarka = bialaStol, cokol = granit, zelazo = zelazo, cegla = true, gzymsyPieter = false, balkonFr = true, kroksztyny = true, szerOkna = 1.35f },
            new Styl { nazwa = "kamienica tynkowana kremowa", sciana = Tynk("tynk kremowy", new Color(.97f, .91f, .79f), new Color(.88f, .82f, .70f)), parter = Kamien("parter: tynk boniowany szary", new Color(.82f, .80f, .76f), new Color(.62f, .60f, .57f)), detal = Tynk("detal: biały tynk", new Color(1f, .99f, .96f), new Color(.94f, .93f, .90f)), stolarka = zielona, cokol = granit, zelazo = zelazo, opaski = true, gzymsyPieter = true, balkonFr = true, kroksztyny = true, szerOkna = 1.3f },
            new Styl { nazwa = "kamienica z klinkieru grafitowego", sciana = klinkier, parter = granit, detal = Kamien("detal: beton architektoniczny", new Color(.80f, .79f, .76f), new Color(.62f, .61f, .58f)), stolarka = antracyt, cokol = granit, zelazo = zelazo, gzymsyPieter = true, szerOkna = 1.55f },
            new Styl { nazwa = "kamienica tynkowana jasnoszara", sciana = Tynk("tynk jasnoszary", new Color(.84f, .85f, .86f), new Color(.70f, .71f, .72f)), parter = granit, detal = Tynk("detal: tynk biały chłodny", new Color(.97f, .97f, .97f), new Color(.92f, .92f, .92f)), stolarka = antracyt, cokol = granit, zelazo = zelazo, opaski = true, gzymsyPieter = true, szerOkna = 1.4f },
            new Styl { nazwa = "kamienica tynkowana ochrowa", sciana = Tynk("tynk ochra", new Color(.94f, .80f, .56f), new Color(.82f, .66f, .42f)), parter = boniowanie, detal = Tynk("detal: tynk kość słoniowa", new Color(.99f, .96f, .88f), new Color(.92f, .88f, .80f)), stolarka = bialaStol, cokol = granit, zelazo = zelazo, opaski = true, gzymsyPieter = true, balkonFr = true, kroksztyny = true, szerOkna = 1.3f },
            new Styl { nazwa = "kamienica z piaskowca", sciana = piaskowiec, parter = boniowanie, detal = Kamien("detal: piaskowiec jasny", new Color(1f, .96f, .88f), new Color(.86f, .80f, .70f)), stolarka = braz, cokol = granit, zelazo = zelazo, cegla = true, kroksztyny = true, balkonFr = true, szerOkna = 1.35f },
        };
    }

    // ---------------- zdejmowanie starego ----------------
    static void ZdejmijStare(Transform bg, BraxCityArchitecture.Building b)
    {
        var front = bg.Find("Street frontage"); if (front) BraxUliceKit.Usun(front.gameObject, "stara elewacja frontowa " + b.id);
        if (BokZachod.Contains(b.id)) { var t = bg.Find("Exposed west return"); if (t) BraxUliceKit.Usun(t.gameObject, "stary bok zachodni " + b.id); }
        if (BokWschod.Contains(b.id)) { var t = bg.Find("Exposed east return"); if (t) BraxUliceKit.Usun(t.gameObject, "stary bok wschodni " + b.id); }
        foreach (Transform c in bg.Cast<Transform>().ToArray())
            if (c.name.StartsWith("Return course")) BraxUliceKit.Usun(c.gameObject, "pas kamienny przed licem (podwójny na styku = migotanie)");
    }

    // Hol parteru i bryła pięter: lica, które leżały w płaszczyźnie elewacji (sufit holu, boczne ściany) albo 15 cm za nią
    // (bryła pięter zasłaniała szyby okien) — zdjęte i postawione od nowa, cofnięte za mur elewacji.
    static void CofnijHol(Transform bg, BraxCityArchitecture.Building b, bool wiez, Transform kolizje)
    {
        float street = b.far ? b.z0 : b.z1, dir = b.far ? 1f : -1f;   // dir: w głąb budynku
        foreach (Transform c in bg.Cast<Transform>().ToArray())
        {
            bool sufit = c.name == "Lobby ceiling " + b.id, sciana = c.name == "Walkable lobby sidewall " + b.id, bryla = c.name == "Solid nonpublic upper floors " + b.id;
            if (!sufit && !sciana && !bryla) continue;
            var mr = c.GetComponent<MeshRenderer>(); var mat = mr ? mr.sharedMaterial : null;
            var bc = c.GetComponent<BoxCollider>();
            Bounds bb = bc ? new Bounds(c.TransformPoint(bc.center), Vector3.Scale(bc.size, c.lossyScale)) : (mr ? mr.bounds : new Bounds());
            if (bb.size == Vector3.zero) continue;
            float cof = bryla ? .6f : .32f;
            float zNear = street + dir * cof, zFar = Mathf.Abs(bb.min.z - street) < Mathf.Abs(bb.max.z - street) ? bb.max.z : bb.min.z;
            var lo = new Vector3(bb.min.x, bb.min.y, Mathf.Min(zNear, zFar)); var hi = new Vector3(bb.max.x, bb.max.y, Mathf.Max(zNear, zFar));
            string nazwa = c.name;
            BraxUliceKit.Usun(c.gameObject, bryla ? "bryła pięter 15 cm za elewacją (zasłaniała szyby)" : sufit ? "sufit holu w płaszczyźnie elewacji (migotanie)" : "ściana holu w płaszczyźnie elewacji (migotanie)");
            if (bryla)
            {
                var g = new GameObject(nazwa + " (kolizja, cofnięta 0.6 m za elewację)"); g.transform.SetParent(bg, false);
                var col = g.AddComponent<BoxCollider>(); col.center = (lo + hi) * .5f; col.size = hi - lo;
            }
            else if (mat) BraxCityArchitecture.Box(nazwa, bg, (lo + hi) * .5f, hi - lo, mat, true);
        }
    }

    // ---------------- geometria fasad ----------------
    static Fasada Front(BraxCityArchitecture.Building b, Lokal lok, bool wiez)
    {
        var f = new Fasada { b = b, front = true, lok = lok, st = style[lok.styl % style.Length], pietra = b.floors, H = b.Height, wiezowiec = wiez };
        f.yTop = Parter + b.floors * Kond; f.W = b.x1 - b.x0;
        if (b.far) { f.A = new Vector3(b.x0, 0, b.z0); f.R = Vector3.right; f.N = Vector3.back; }
        else { f.A = new Vector3(b.x1, 0, b.z1); f.R = Vector3.left; f.N = Vector3.forward; }
        // oś drzwi = środek środkowego przęsła zestawu (BuildStreetAccessDoor) — tam stoi działające skrzydło
        f.bays = Mathf.Max(1, Mathf.RoundToInt(f.W / 3f)); f.bay = f.W / f.bays;
        float frontX = b.far ? b.x1 : b.x0, kier = b.far ? -1f : 1f;
        float portalX = frontX + kier * (f.bays / 2 + .5f) * f.bay;
        f.drzwiS = Vector3.Dot(new Vector3(portalX, 0, f.A.z) - f.A, f.R);
        return f;
    }
    static Fasada Bok(BraxCityArchitecture.Building b, Lokal lok, bool zachod)
    {
        var f = new Fasada { b = b, front = false, lok = lok, st = style[lok.styl % style.Length], pietra = b.floors, H = b.Height };
        f.yTop = Parter + b.floors * Kond; f.W = b.z1 - b.z0;
        if (zachod) { f.N = Vector3.left; f.R = Vector3.back; f.A = new Vector3(b.x0, 0, b.z1); }
        else { f.N = Vector3.right; f.R = Vector3.forward; f.A = new Vector3(b.x1, 0, b.z0); }
        // front od ulicy: bliższa strona z1 (rząd południowy) albo z0 (rząd północny)
        float zUlicy = b.far ? b.z0 : b.z1;
        f.ulica = Mathf.Abs(f.A.z - zUlicy) < .01f ? -1 : 1;
        f.bays = Mathf.Max(1, Mathf.RoundToInt((f.W - .6f) / 3.1f)); f.bay = f.W / f.bays;
        return f;
    }

    sealed class Otwor { public Rect r; public float gleb; public bool parapet, witryna, drzwi; }

    static void Elewacja(Fasada f, Transform kolizje)
    {
        var st = f.st; float k = BraxUliceKit.K;
        var otwory = new List<Otwor>();
        int drzwiBay = f.front ? Mathf.Clamp(Mathf.FloorToInt(f.drzwiS / f.bay), 0, f.bays - 1) : -1;
        // --- parter ---
        if (f.front && !f.gora)
        {
            otwory.Add(new Otwor { r = Rect.MinMaxRect(f.drzwiS - 1.17f, Chodnik, f.drzwiS + 1.17f, 3.2f), gleb = .3f, drzwi = true });
            for (int i = 0; i < f.bays; i++)
            {
                if (i == drzwiBay) continue;
                float s0 = Mathf.Max(i * f.bay + .28f, Lizena + .16f), s1 = Mathf.Min((i + 1) * f.bay - .28f, f.W - Lizena - .16f);
                if (i == drzwiBay - 1) s1 = Mathf.Min(s1, f.drzwiS - 1.17f - .45f);
                if (i == drzwiBay + 1) s0 = Mathf.Max(s0, f.drzwiS + 1.17f + .45f);
                if (s1 - s0 < .9f) continue;
                otwory.Add(new Otwor { r = Rect.MinMaxRect(s0, .62f, s1, 3.05f), gleb = GlebWitryny, witryna = true });
            }
        }
        else if (!f.front)
        {
            // bok: parter z mniejszymi oknami w osiach (zaplecze lokalu), z kratami
            for (int i = 0; i < f.bays; i++)
            {
                float c = (i + .5f) * f.bay, w = Mathf.Min(1.1f, f.bay - 1f); if (w < .6f) continue;
                otwory.Add(new Otwor { r = Rect.MinMaxRect(c - w * .5f, 1.35f, c + w * .5f, 2.75f), gleb = GlebOkna, parapet = true });
            }
        }
        // --- piętra ---
        for (int fl = 0; fl < f.pietra; fl++)
        {
            float yF = Parter + fl * Kond; bool gora = fl == f.pietra - 1; bool fr = st.balkonFr && fl == 0 && f.front;
            float y0 = fr ? yF + .28f : yF + (gora ? 1.0f : .9f), wh = fr ? 2.47f : gora ? 1.6f : 1.85f;
            for (int i = 0; i < f.bays; i++)
            {
                float c = (i + .5f) * f.bay, ww = Mathf.Min(st.szerOkna, f.bay - .95f);
                if (ww < .7f) continue;
                otwory.Add(new Otwor { r = Rect.MinMaxRect(c - ww * .5f, y0, c + ww * .5f, y0 + wh), gleb = GlebOkna, parapet = true });
            }
        }
        // --- mur z otworami (jedna płaszczyzna, bez nakładek) ---
        float yMur = f.H - .1f, yDol = f.gora ? Parter : Chodnik;
        var xs = new SortedSet<float> { 0f, f.W }; var ys = new SortedSet<float> { yDol, Parter, yMur };
        foreach (var o in otwory) { xs.Add(o.r.xMin); xs.Add(o.r.xMax); ys.Add(o.r.yMin); ys.Add(o.r.yMax); }
        var X = xs.Where(x => x >= 0f && x <= f.W).ToArray(); var Y = ys.Where(y => y >= yDol && y <= yMur).ToArray();
        for (int i = 0; i < X.Length - 1; i++)
            for (int j = 0; j < Y.Length - 1; j++)
            {
                float cx = (X[i] + X[i + 1]) * .5f, cy = (Y[j] + Y[j + 1]) * .5f;
                if (X[i + 1] - X[i] < .002f || Y[j + 1] - Y[j] < .002f) continue;
                if (otwory.Any(o => o.r.Contains(new Vector2(cx, cy)))) continue;
                var m = cy < Parter ? st.parter : st.sciana;
                BraxUliceKit.S(m).Quad(f.P(X[i], Y[j], 0), f.P(X[i + 1], Y[j], 0), f.P(X[i + 1], Y[j + 1], 0), f.P(X[i], Y[j + 1], 0), f.N);
            }
        // --- ościeża (głębia muru) ---
        foreach (var o in otwory)
        {
            var m = o.r.yMax <= Parter + .01f ? st.parter : st.detal; var s = BraxUliceKit.S(m); float D = o.gleb;
            Rect r = o.r;
            s.Quad(f.P(r.xMin, r.yMin, 0), f.P(r.xMin, r.yMin, -D), f.P(r.xMin, r.yMax, -D), f.P(r.xMin, r.yMax, 0), f.R);
            s.Quad(f.P(r.xMax, r.yMin, 0), f.P(r.xMax, r.yMin, -D), f.P(r.xMax, r.yMax, -D), f.P(r.xMax, r.yMax, 0), -f.R);
            s.Quad(f.P(r.xMin, r.yMax, 0), f.P(r.xMax, r.yMax, 0), f.P(r.xMax, r.yMax, -D), f.P(r.xMin, r.yMax, -D), Vector3.down);
            if (!o.parapet && !o.drzwi) s.Quad(f.P(r.xMin, r.yMin, 0), f.P(r.xMax, r.yMin, 0), f.P(r.xMax, r.yMin, -D), f.P(r.xMin, r.yMin, -D), Vector3.up);
        }
        // --- okna mieszkań ---
        float offU = f.A.x * f.N.z - f.A.z * f.N.x;   // dot(A, T), T = (N.z, 0, −N.x) — jak w shaderze UliceOkno
        var szybaPokoj = BraxUliceKit.OknoPokoj(f.bay, Kond, Parter + f.A.y, 0, 5, 4.2f, offU);
        // Apteka w wieżowcu ma PRAWDZIWE regały za przezroczystą witryną.
        // Stary warunek f.wiezowiec ustawiał tu atrapę pokoju i zasłaniał leki.
        var szybaSklep = (f.wiezowiec && f.front && f.lok.typ == "apteka") || (f.front && !f.wiezowiec)
            ? BraxUliceKit.SzkloPrzezroczyste
            : BraxUliceKit.OknoPokoj(f.bay, 3.6f, Chodnik + f.A.y, f.lok.wariantWitryny, 1, 3.8f, offU);
        var szkloMieszkania = BraxUliceKit.Mat("szkło okna mieszkania (glass, przezroczyste)", new Color(.62f, .70f, .72f, .2f), .05f, .1f);
        var aluminium = BraxUliceKit.Mat("aluminium witryn grafit", new Color(.09f, .10f, .105f), .4f, .55f);
        foreach (var o in otwory)
        {
            if (o.drzwi) continue;
            if (o.witryna) { Witryna(f, o, szybaSklep, aluminium); nWitryn++; continue; }
            Okno(f, o, f.szkloOkien.width > 0f && f.szkloOkien.Contains(o.r.center) ? szkloMieszkania : szybaPokoj, !f.front && o.r.yMax < Parter); nOkien++;
        }
        // --- detale wzorca (właściciel 15:20: „od razu DETALE wszędzie”): klimatyzatory, schody pożarowe, kominy i anteny ---
        Detale(f, otwory);
        // --- lizeny na krawędziach (cokół lizeny głębszy), cokół między nimi ---
        foreach (float s0 in new[] { 0f, f.W - Lizena })
        {
            if (!f.gora) OBox(f, st.cokol, s0, s0 + Lizena, Chodnik, .62f, 0f, .09f);
            OBox(f, st.cegla ? st.detal : st.sciana, s0, s0 + Lizena, f.gora ? Parter : .62f, f.yTop, 0f, .06f);
        }
        float pasDrzwi0 = f.front ? f.drzwiS - 1.17f : -1f, pasDrzwi1 = f.front ? f.drzwiS + 1.17f : -1f;
        if (f.front && !f.gora)
        {
            OBox(f, st.cokol, Lizena, pasDrzwi0, Chodnik, .62f, 0f, .04f);
            OBox(f, st.cokol, pasDrzwi1, f.W - Lizena, Chodnik, .62f, 0f, .04f);
        }
        else if (!f.gora) OBox(f, st.cokol, Lizena, f.W - Lizena, Chodnik, .62f, 0f, .04f);
        // --- gzyms nad parterem (dwie warstwy, bez wspólnych płaszczyzn) ---
        if (!f.gora) { OBox(f, st.detal, 0f, f.W, 3.50f, 3.62f, 0f, .13f); OBox(f, st.detal, 0f, f.W, 3.62f, 3.84f, 0f, .26f); }
        // --- gzymsy kondygnacyjne ---
        if (st.gzymsyPieter) for (int fl = 1; fl < f.pietra; fl++) { float yF = Parter + fl * Kond; OBox(f, st.detal, 0f, f.W, yF - .05f, yF + .07f, 0f, .05f); }
        // --- gzyms wieńczący z kroksztynami, attyka, czapa ---
        float yt = f.yTop;
        OBox(f, st.detal, 0f, f.W, yt - .40f, yt, 0f, .025f);                         // fryz
        if (st.kroksztyny)
            for (float s = .35f; s < f.W - .3f; s += .58f) OBox(f, st.detal, s - .07f, s + .07f, yt - .32f, yt, 0f, .36f);
        OBox(f, st.detal, 0f, f.W, yt, yt + .20f, 0f, .48f, false);                  // płyta okapowa
        OBox(f, st.detal, 0f, f.W, yt + .20f, yt + .30f, 0f, .40f, false);           // gzymsik
        // czapa attyki (obróbka); na boku kończy się 0.32 m przed narożnikiem (tam leży czapa frontu), narożnik domyka kostka 6 cm
        float cz0 = f.ulica == -1 ? .32f : 0f, cz1 = f.ulica == 1 ? f.W - .32f : f.W;
        BraxUliceKit.S(st.detal).Box(f.P(cz0, yMur, -.32f), f.P(cz1, f.H, .06f));
        if (f.ulica == -1) BraxUliceKit.S(st.detal).Box(f.P(-.06f, yMur, 0f), f.P(.32f, f.H, .06f));
        if (f.ulica == 1) BraxUliceKit.S(st.detal).Box(f.P(f.W - .32f, yMur, 0f), f.P(f.W + .06f, f.H, .06f));
        // --- rura spustowa (front: lewa krawędź) ---
        if (f.front && !f.gora)
        {
            var blacha = BraxUliceKit.Mat("blacha tytanowo-cynkowa (rury, obróbki)", new Color(.46f, .48f, .49f), .5f, .7f);
            var rs = BraxUliceKit.S(blacha); float rr = .05f * k, sd = .15f, dd = .13f;
            rs.Rura(f.P(sd, yt - .05f, dd), f.P(sd, .55f, dd), rr, 10);
            rs.Rura(f.P(sd, .55f, dd), f.P(sd, .32f, dd + .16f), rr, 10);                // kolano wylotu
            for (float y = 1.2f; y < yt - .3f; y += 2.1f) { rs.Box(f.P(sd - .025f, y - .025f, 0f), f.P(sd + .025f, y + .025f, dd)); rs.Rura(f.P(sd, y - .04f, dd), f.P(sd, y + .04f, dd), rr + .012f, 10, false); }
            rs.Rura(f.P(sd, yt - .05f, dd), f.P(sd, yt + .1f, .3f), rr, 8);              // przejście przez okap do rynny
        }
        // --- kolizja parteru (witryny i filary; bez otworu drzwi) ---
        if (f.front && !f.gora)
        {
            KolizjaPas(f, kolizje, 0f, pasDrzwi0); KolizjaPas(f, kolizje, pasDrzwi1, f.W);
        }
    }

    static void KolizjaPas(Fasada f, Transform kolizje, float s0, float s1)
    {
        if (s1 - s0 < .05f) return;
        var g = new GameObject("kolizja: parter " + f.lok.szyld); g.transform.SetParent(kolizje, false);
        var a = f.P(s0, Chodnik, -.28f); var b = f.P(s1, Parter - .1f, 0f);
        var c = g.AddComponent<BoxCollider>(); c.center = (a + b) * .5f; c.size = new Vector3(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
    }

    // Prostopadłościan „na ścianie”: s0..s1, y0..y1, d0..d1 (d wzdłuż N), bez ściany tylnej (przylega do muru).
    static void OBox(Fasada f, Material m, float s0, float s1, float y0, float y1, float d0, float d1, bool bezTylu = true)
    {
        // bok budynku: element biegnący do narożnika od ulicy wydłużony o własną głębokość (domyka narożnik z frontem;
        // styk z frontem to ściany o przeciwnych normalnych albo krawędź — nigdy dwie powierzchnie w jednej płaszczyźnie)
        if (f.ulica == -1 && s0 <= .001f) s0 = -d1;
        if (f.ulica == 1 && s1 >= f.W - .001f) s1 = f.W + d1;
        if (s1 - s0 < .002f || y1 - y0 < .002f || d1 - d0 < .002f) return;
        var c = f.P((s0 + s1) * .5f, (y0 + y1) * .5f, (d0 + d1) * .5f);
        BraxUliceKit.S(m).OBox(c, f.R, Vector3.up, f.N, s1 - s0, y1 - y0, d1 - d0, bezTylu);
    }

    static void Okno(Fasada f, Otwor o, Material szyba, bool krata)
    {
        var st = f.st; var r = o.r; float D = o.gleb; float s0 = r.xMin, s1 = r.xMax, y0 = r.yMin, y1 = r.yMax, c = (s0 + s1) * .5f;
        // parapet zewnętrzny (wchodzi do ramy; zastępuje dolne ościeże)
        OBox(f, st.detal, s0 - .06f, s1 + .06f, y0 - .06f, y0, -D + .06f, .06f, false);
        // opaska albo nadproże z kluczem
        if (st.opaski)
        {
            OBox(f, st.detal, s0 - .12f, s0, y0, y1, 0f, .02f); OBox(f, st.detal, s1, s1 + .12f, y0, y1, 0f, .02f);
            OBox(f, st.detal, s0 - .12f, s1 + .12f, y1, y1 + .12f, 0f, .02f);
            OBox(f, st.detal, s0 - .2f, s1 + .2f, y1 + .12f, y1 + .27f, 0f, .1f, false);   // gzymsik nadokienny
        }
        else if (st.cegla)
        {
            OBox(f, st.detal, s0 - .1f, s1 + .1f, y1, y1 + .24f, 0f, .04f);                 // nadproże kamienne
            OBox(f, st.detal, c - .11f, c + .11f, y1 - .04f, y1 + .3f, 0f, .07f, false);    // zwornik
        }
        else OBox(f, st.detal, s0 - .04f, s1 + .04f, y1, y1 + .16f, 0f, .03f);             // nadproże betonowe
        // rama ościeżnicy w głębi
        var ms = st.stolarka; float fr = .07f, fd0 = -D, fd1 = -D + .08f;
        OBox(f, ms, s0, s0 + fr, y0, y1, fd0, fd1, false); OBox(f, ms, s1 - fr, s1, y0, y1, fd0, fd1, false);
        OBox(f, ms, s0 + fr, s1 - fr, y1 - fr, y1, fd0, fd1, false); OBox(f, ms, s0 + fr, s1 - fr, y0, y0 + fr, fd0, fd1, false);
        // słupek i ślemię (ślemię w dwóch częściach — bez nakładania na słupek)
        float yt = y0 + (y1 - y0) * .7f, md0 = -D + .045f, md1 = -D + .075f;
        bool slupek = s1 - s0 > 1.0f;
        if (slupek) OBox(f, ms, c - .03f, c + .03f, y0 + fr, y1 - fr, md0, md1);
        if (slupek) { OBox(f, ms, s0 + fr, c - .03f, yt - .03f, yt + .03f, md0, md1); OBox(f, ms, c + .03f, s1 - fr, yt - .03f, yt + .03f, md0, md1); }
        else OBox(f, ms, s0 + fr, s1 - fr, yt - .03f, yt + .03f, md0, md1);
        // szyba (pokój za szybą)
        BraxUliceKit.S(szyba).Quad(f.P(s0, y0, -D + .04f), f.P(s1, y0, -D + .04f), f.P(s1, y1, -D + .04f), f.P(s0, y1, -D + .04f), f.N);
        // balustrada francuska (okna-drzwi pierwszego piętra)
        if (y0 - (Parter + Mathf.Floor((y0 - Parter) / Kond) * Kond) < .5f && y0 > Parter)
        {
            float yF = Parter + Mathf.Floor((y0 - Parter) / Kond) * Kond, top = yF + 1.1f * BraxUliceKit.K, d = .1f;
            var z = BraxUliceKit.S(st.zelazo);
            z.Rura(f.P(s0 - .05f, top, d), f.P(s1 + .05f, top, d), .022f, 8);
            z.Rura(f.P(s0 - .05f, y0 + .12f, d), f.P(s1 + .05f, y0 + .12f, d), .016f, 6);
            z.Rura(f.P(s0 - .05f, top - .18f, d), f.P(s1 + .05f, top - .18f, d), .012f, 6);
            for (float s = s0 + .06f; s < s1 - .02f; s += .115f) z.Rura(f.P(s, y0 + .12f, d), f.P(s, top, d), .009f, 5, false);
            foreach (float s in new[] { s0 - .05f, s1 + .05f }) { z.Rura(f.P(s, y0 + .05f, d), f.P(s, top + .02f, d), .02f, 8); z.Box(f.P(s - .02f, top - .3f, 0f), f.P(s + .02f, top - .26f, d)); z.Box(f.P(s - .02f, y0 + .2f, 0f), f.P(s + .02f, y0 + .24f, d)); }
        }
        // krata w oknie parteru od podwórka/boku
        if (krata)
        {
            var z = BraxUliceKit.S(st.zelazo); float d = -.06f;
            for (float s = s0 + .12f; s < s1 - .05f; s += .13f) z.Rura(f.P(s, y0 + .02f, d), f.P(s, y1 - .02f, d), .011f, 5, false);
            foreach (float y in new[] { y0 + .25f, y1 - .25f }) z.Box(f.P(s0 + .01f, y - .015f, d - .012f), f.P(s1 - .01f, y + .015f, d + .012f));
        }
    }

    static void Witryna(Fasada f, Otwor o, Material szyba, Material alu)
    {
        var r = o.r; float D = o.gleb, s0 = r.xMin, s1 = r.xMax, y0 = r.yMin, y1 = r.yMax, fr = .065f, fd0 = -D, fd1 = -D + .09f;
        OBox(f, alu, s0, s0 + fr, y0, y1, fd0, fd1, false); OBox(f, alu, s1 - fr, s1, y0, y1, fd0, fd1, false);
        OBox(f, alu, s0 + fr, s1 - fr, y1 - fr, y1, fd0, fd1, false); OBox(f, alu, s0 + fr, s1 - fr, y0, y0 + .11f, fd0, fd1, false);
        float yt = 2.42f;   // ślemię: naświetle nad witryną
        int n = Mathf.Max(1, Mathf.RoundToInt((s1 - s0) / 1.6f)); float seg = (s1 - s0 - 2 * fr) / n;
        for (int i = 1; i < n; i++) { float x = s0 + fr + i * seg; OBox(f, alu, x - .03f, x + .03f, y0 + .11f, y1 - fr, fd0 + .005f, fd1 - .005f, false); }
        for (int i = 0; i < n; i++)
        {
            float a = s0 + fr + i * seg + (i > 0 ? .03f : 0f), b = s0 + fr + (i + 1) * seg - (i < n - 1 ? .03f : 0f);
            OBox(f, alu, a, b, yt - .03f, yt + .03f, fd0 + .005f, fd1 - .005f, false);
        }
        BraxUliceKit.S(szyba).Quad(f.P(s0, y0, -D + .045f), f.P(s1, y0, -D + .045f), f.P(s1, y1, -D + .045f), f.P(s0, y1, -D + .045f), f.N);
    }

    // ---------------- szyldy, markizy, wysięgniki, numery ----------------
    static Transform szyldyFirm;
    static void Szyldy(Fasada f)
    {
        var lok = f.lok; float k = BraxUliceKit.K;
        var lico = BraxUliceKit.Mat("litery przestrzenne: lico " + lok.szyld + " (podświetlenie nocą)", lok.licoSzyldu, .45f, .05f);
        var boki = BraxUliceKit.Mat("litery przestrzenne: boki grafit", new Color(.06f, .06f, .065f), .5f, .4f);
        var listwa = BraxUliceKit.Mat("listwa montażowa szyldu (aluminium grafit)", new Color(.12f, .125f, .13f), .45f, .55f);
        // napis nad witrynami: na listwie, w pasie między nadprożem (3.2) a gzymsem (3.5)
        float maxW = f.W - 2f * Lizena - .8f, cap = .26f;
        float w = BraxUliceKit.SzerokoscNapisu(lok.szyld, cap); if (w > maxW) { cap *= maxW / w; w = maxW; }
        float sc = Mathf.Clamp(f.drzwiS, Lizena + w * .5f + .2f, f.W - Lizena - w * .5f - .2f);
        // 3.11 16:20 (właściciel: trzy firmy — BRAXHORN, WAWER, SOBCZAK — na wszystkie usługi): szyld z grafiki firmy (arkusz 5:1),
        // jeśli jest w paczce grafik; inaczej litery przestrzenne jak dotąd
        string firma = lok.typ == "apteka" ? "sobczak_apteka" : lok.typ == "piekarnia" ? "sobczak_piekarnia" : lok.typ == "cukiernia" ? "sobczak_cukiernia"
                     : lok.typ == "kawiarnia" ? "sobczak_kawiarnia" : lok.typ == "warzywniak" ? "sobczak_warzywa" : lok.typ == "market" ? "sobczak_delikatesy" : null;   // 16:34: właściciel dał SOBCZAK DELIKATESY
        float wG = Mathf.Min(maxW, 2.6f), hG = wG / 5f;
        if (firma != null)
        {
            if (!szyldyFirm) throw new InvalidOperationException("Brak kontenera szyldów firmy w korzeniu miasta");
            if (BraxMiastoBillboardy.Mural(szyldyFirm, firma, f.P(sc, 3.33f, .06f), f.N, wG, hG))
            {
                OBox(f, listwa, sc - wG * .5f - .06f, sc + wG * .5f + .06f,
                     3.33f - hG * .5f - .04f, 3.33f + hG * .5f + .04f, 0f, .05f);
                Debug.Log("BRAXHORN_SZYLD_POTWIERDZONY " + lok.szyld + " -> " + firma);
                goto poSzyldzie;
            }
            Debug.LogError("BRAXHORN_SZYLD_NIEGOTOWY: " + lok.szyld +
                " brak oryginalnej grafiki " + firma + " w obu dostarczonych paczkach");
        }
        OBox(f, listwa, sc - w * .5f - .25f, sc + w * .5f + .25f, 3.26f, 3.40f, 0f, .045f);
        BraxUliceKit.Napis3D(lok.szyld, f.P(sc, 3.215f, .045f), f.N, cap, .07f, lico, boki);
        poSzyldzie:
        // markizy nad witrynami (dół falbany 2.52 m — Brax 2.33 m przechodzi)
        if (lok.markiza)
        {
            var plotno = BraxUliceKit.Mat("markiza: płótno akrylowe (paski z koloru wierzchołków)", Color.white, .9f);
            var stal = BraxUliceKit.Mat("markiza: stal malowana", new Color(.10f, .10f, .11f), .5f, .5f);
            var ws = BraxUliceKit.S(plotno); var ss = BraxUliceKit.S(stal);
            int drzwiBay = Mathf.FloorToInt(f.drzwiS / f.bay);
            for (int i = 0; i < f.bays; i++)
            {
                if (i == drzwiBay) continue;
                float s0 = Mathf.Max(i * f.bay + .2f, Lizena + .08f), s1 = Mathf.Min((i + 1) * f.bay - .2f, f.W - Lizena - .08f);
                if (i == drzwiBay - 1) s1 = Mathf.Min(s1, f.drzwiS - 1.3f); if (i == drzwiBay + 1) s0 = Mathf.Max(s0, f.drzwiS + 1.3f);
                if (s1 - s0 < .9f) continue;
                float yA = 3.12f, yB = 2.74f, dB = 1.05f, yC = 2.52f, pas = .26f;
                int np = Mathf.Max(2, Mathf.RoundToInt((s1 - s0) / pas)); float pw = (s1 - s0) / np;
                for (int p = 0; p < np; p++)
                {
                    ws.kolor = p % 2 == 0 ? lok.markizaA : lok.markizaB; float a = s0 + p * pw, b = a + pw;
                    var n1 = Vector3.Cross(f.R, (f.P(a, yB, dB) - f.P(a, yA, .02f)).normalized).normalized; if (Vector3.Dot(n1, Vector3.up) < 0) n1 = -n1;
                    ws.Quad(f.P(a, yA, .02f), f.P(b, yA, .02f), f.P(b, yB, dB), f.P(a, yB, dB), n1);
                    ws.Quad(f.P(a, yA, .02f), f.P(b, yA, .02f), f.P(b, yB, dB), f.P(a, yB, dB), -n1);
                    // falbana z ząbkami (fala co pasek)
                    ws.Quad(f.P(a, yB, dB), f.P(b, yB, dB), f.P(b, yC + .05f, dB), f.P(a, yC, dB), f.N);
                    ws.Quad(f.P(a, yB, dB - .004f), f.P(b, yB, dB - .004f), f.P(b, yC + .05f, dB - .004f), f.P(a, yC, dB - .004f), -f.N);
                }
                ws.kolor = lok.markizaA;
                foreach (var (s, dirR) in new[] { (s0, -1f), (s1, 1f) })
                {
                    ws.Tri(f.P(s, yA, .02f), f.P(s, yB, dB), f.P(s, yB, .02f), f.R * dirR);
                    ws.Tri(f.P(s, yA, .02f), f.P(s, yB, dB), f.P(s, yB, .02f), -f.R * dirR);
                }
                ws.kolor = BraxUliceKit.Bialy;
                ss.Rura(f.P(s0, yB, dB), f.P(s1, yB, dB), .025f, 8);
                foreach (float s in new[] { s0 + .1f, s1 - .1f }) { ss.Rura(f.P(s, 2.9f, .0f), f.P(s, yB + .02f, dB - .03f), .018f, 6); ss.Box(f.P(s - .05f, 2.84f, 0f), f.P(s + .05f, 2.96f, .03f)); }
            }
        }
        // szyld na wysięgniku (dwustronny kaseton, prostopadle do ściany) przy prawej lizenie, na wysokości 1. piętra
        if (lok.wysiegnik)
        {
            var rama = BraxUliceKit.Mat("kaseton: rama aluminium", new Color(.16f, .17f, .18f), .45f, .6f);
            var tlo = lok.typ == "apteka" ? BraxUliceKit.Mat("kaseton: lico białe (podświetlenie nocą)", new Color(.95f, .96f, .95f), .4f)
                                          : BraxUliceKit.Mat("kaseton: lico grafit (podświetlenie nocą)", new Color(.08f, .09f, .10f), .4f);
            float s = f.W - Lizena * .5f, y = Parter + 1.6f, wys = .95f, dl = .9f, gr = .16f, dyst = .14f;
            var t = f.R; var c = f.P(s, y, dyst + dl * .5f);
            // kaseton: rama-obwód (4 listwy), a lica kasetonu to SAME tła z treścią (nic przyklejonego do lica)
            {
                var rr0 = BraxUliceKit.S(rama);
                rr0.OBox(c + Vector3.up * (wys * .5f - .02f), f.N, Vector3.up, t, dl, .04f, gr);
                rr0.OBox(c - Vector3.up * (wys * .5f - .02f), f.N, Vector3.up, t, dl, .04f, gr);
                rr0.OBox(c + f.N * (dl * .5f - .02f), f.N, Vector3.up, t, .04f, wys - .08f, gr);
                rr0.OBox(c - f.N * (dl * .5f - .02f), f.N, Vector3.up, t, .04f, wys - .08f, gr);
            }
            foreach (float sg in new[] { 1f, -1f })
            {
                var fn = t * sg; var fc = c + fn * (gr * .5f);
                var rr = Vector3.Cross(Vector3.up, -fn).normalized;
                BraxUliceKit.S(tlo).Quad(fc - rr * (dl * .5f - .04f) - Vector3.up * (wys * .5f - .04f), fc + rr * (dl * .5f - .04f) - Vector3.up * (wys * .5f - .04f), fc + rr * (dl * .5f - .04f) + Vector3.up * (wys * .5f - .04f), fc - rr * (dl * .5f - .04f) + Vector3.up * (wys * .5f - .04f), fn);
                if (lok.typ == "apteka")
                {   // zielony krzyż apteczny
                    var kz = BraxUliceKit.S(lico); float a = .25f, bb = .085f, ram = (a - bb);
                    kz.OBox(fc + fn * .01f, rr, Vector3.up, fn, bb * 2f, a * 2f, .02f, true);                                   // pionowe ramię (całe)
                    kz.OBox(fc + fn * .01f - rr * (bb + ram * .5f), rr, Vector3.up, fn, ram, bb * 2f, .02f, true);              // lewe
                    kz.OBox(fc + fn * .01f + rr * (bb + ram * .5f), rr, Vector3.up, fn, ram, bb * 2f, .02f, true);              // prawe
                }
                else
                {
                    string tx = lok.szyld.Length > 7 ? lok.szyld.Split(' ')[0] : lok.szyld;
                    float cp = Mathf.Min(.24f, (dl - .16f) / Mathf.Max(.1f, BraxUliceKit.SzerokoscNapisu(tx, 1f)));
                    BraxUliceKit.Napis3D(tx, fc - Vector3.up * cp * .5f, fn, cp, .015f, lico, boki);
                }
            }
            var z = BraxUliceKit.S(f.st.zelazo);
            z.Box(f.P(s - .07f, y - wys * .5f - .15f, 0f), f.P(s + .07f, y + wys * .5f + .15f, .03f));
            foreach (float yy in new[] { y + wys * .5f + .05f, y - wys * .5f - .05f }) z.Rura(f.P(s, yy, .03f), f.P(s, yy, dyst + dl), .02f, 6);
        }
        // tabliczka z numerem przy drzwiach (emalia: białe pole, granatowa ramka, cyfry wypukłe)
        {
            var emalia = BraxUliceKit.Mat("tabliczka adresowa: emalia biała", new Color(.94f, .94f, .92f), .25f);
            var granat = BraxUliceKit.Mat("tabliczka adresowa: granat", new Color(.08f, .14f, .32f), .3f, .1f);
            float s = Mathf.Min(f.drzwiS + 1.17f + .38f, f.W - Lizena - .25f), y = 2.35f, pw = .30f * k, ph = .22f * k;
            // ramka granatowa (pierścień) i pole emaliowane w jej otworze — jedno lico, nic na niczym
            float a0 = s - pw * .5f, a1 = s + pw * .5f, b0 = y - ph * .5f, b1 = y + ph * .5f;
            OBox(f, granat, a0, a0 + .02f, b0, b1, 0f, .018f); OBox(f, granat, a1 - .02f, a1, b0, b1, 0f, .018f);
            OBox(f, granat, a0 + .02f, a1 - .02f, b0, b0 + .02f, 0f, .018f); OBox(f, granat, a0 + .02f, a1 - .02f, b1 - .02f, b1, 0f, .018f);
            OBox(f, emalia, a0 + .02f, a1 - .02f, b0 + .02f, b1 - .02f, 0f, .018f);
            BraxUliceKit.Napis3D(f.lok.numer.ToString(), f.P(s, y - .09f, .018f), f.N, .18f, .01f, granat, granat, .17f, .1f);
        }
    }

    // ---------------- drzwi: etykieta, punkty pod misje ----------------
    static void Drzwi(Transform bg, BraxCityArchitecture.Building b, Lokal lok, Fasada f)
    {
        foreach (var d in bg.GetComponentsInChildren<BraxCityDoor>(true))
            if (d.id == "R1_PUBLIC_DOOR_" + b.id) { d.label = lok.szyld; EditorUtility.SetDirty(d); }
        BraxUliceKit.Punkt("ulica_" + b.id + "_drzwi", "przed drzwiami: " + lok.szyld + " (nr " + lok.numer + ") — AKCJA = drzwi; miejsce zlecenia przy wejściu", f.P(f.drzwiS, Chodnik, 1.2f));
    }

    // APTEKA SOBCZAK — sklep pod wieżowcem (adres 5). Winda budynku nadal działa.
    // Jest osobna ekspozycja w dwóch zatokach: wolny środek (x=43..47)
    // zapewnia przejście od drzwi frontowych do windy przy x=45, z około 15.75.
    static void AptekaWiezowca(Transform bg, Transform cityRoot)
    {
        if (!BraxMiastoAssety.Jest("miasto_sklepy"))
        {
            Debug.LogError("BRAXHORN_APTEKA_NIEGOTOWA: brak paczki miasto_sklepy z etykietami lek_01..24");
            return;
        }

        // Stary biurowy wystrój tego konkretnego parteru kolidowałby z ekspozycją
        // leków. Usuwamy tylko jego grupę DEKORACYJNĄ, nie szyb windy, kabinę,
        // drzwi zewnętrzne ani wspólne konstrukcje wieżowca.
        var dekoracje = cityRoot.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "Real PBR tower lobby apothecary");
        if (dekoracje) BraxUliceKit.Usun(dekoracje.gameObject, "apteka zamiast biurowej dekoracji holu");

        var material = BraxRegionMeble.Tex("APTEKA: biały laminat na regałach",
            1.1f, new Color(.94f, .94f, .91f), .55f, "white_plaster_02", "painted_plaster_wall");
        var stal = BraxUliceKit.Mat("APTEKA: listwy anodowane", new Color(.70f, .73f, .73f), .35f, .75f);
        var polki = BraxUliceKit.S(material);
        var obram = BraxUliceKit.S(stal);
        var towar = BraxSklepyTowar.Korzen(bg, "APTEKA SOBCZAK — leki z etykietami");
        var rng = new System.Random(20261104);

        // Dwie fizyczne zatoki 3.0 m × 0.55 m, z tyłu części frontowej;
        // nie zasłaniają szyby ani przejścia centralnego i kończą się przed windą.
        foreach (var (x0, x1) in new[] { (38.85f, 42.05f), (48.00f, 51.20f) })
        {
            const float z0 = 17.85f, z1 = 18.40f;
            float y0 = .15f, h = 2.75f;
            polki.Box(new Vector3(x0, y0, z0), new Vector3(x0 + .055f, y0 + h, z1));
            polki.Box(new Vector3(x1 - .055f, y0, z0), new Vector3(x1, y0 + h, z1));
            polki.Box(new Vector3(x0, y0, z0), new Vector3(x1, y0 + h, z0 + .045f)); // plecy, nie dodatkowa fasada
            // Regały mają rzeczywistą kolizję tylko w bocznych zatokach.
            // Centralny korytarz x=43…47 jest wolny od dekoracji i colliderek;
            // nie blokujemy wejścia ani działającej windy wieżowca.
            BraxRegionMeble.Kolizja(bg, "APTEKA: regał z prawdziwymi lekami",
                new Vector3((x0 + x1) * .5f, y0 + h * .5f, (z0 + z1) * .5f),
                new Vector3(x1 - x0, h, z1 - z0));
            for (int p = 0; p < 5; p++)
            {
                float y = y0 + .13f + p * .55f;
                polki.Box(new Vector3(x0 + .055f, y - .045f, z0 + .045f),
                    new Vector3(x1 - .055f, y, z1));
                obram.Box(new Vector3(x0 + .07f, y - .052f, z1 - .025f),
                    new Vector3(x1 - .07f, y - .046f, z1));
                if (p == 4) continue;
                BraxSklepyTowar.Polka("apteka", towar,
                    new Vector3(x0 + .13f, y + .006f, (z0 + z1) * .5f),
                    Vector3.right, Vector3.back, x1 - x0 - .26f, rng, .51f);
            }
        }

        // Krzyż i napis wewnątrz witryny (nie na szybie; nie zastępują logo właściciela).
        var znak = BraxUliceKit.S(BraxMiastoA.Mat("APTEKA: zielony krzyż wewnętrzny",
            new Color(.15f, .65f, .28f), .40f));
        znak.Box(new Vector3(40.12f, 2.99f, 18.61f), new Vector3(40.48f, 3.35f, 18.65f));
        znak.Box(new Vector3(39.92f, 3.11f, 18.61f), new Vector3(40.68f, 3.23f, 18.65f));

        BraxSklepyTowar.Zakoncz(towar, "APTEKA SOBCZAK");
        Debug.Log("BRAXHORN_APTEKA_WIEZOWIEC fizyczne regały=2; wolny korytarz x 43..47; winda nienaruszona");
    }

    // ---------------- wnętrze lokalu za witryną ----------------
    static void Wnetrze(Fasada f, Transform bg, Transform kolizje)
    {
        var b = f.b; float k = BraxUliceKit.K; var lok = f.lok;
        float gl = Mathf.Min(7.05f, (b.z1 - b.z0) - .55f);        // głębokość holu (BraxCityArchitecture.Build)
        float sIn0 = .34f, sIn1 = f.W - .34f, dTyl = -(gl - .14f), y0 = .11f, ySuf = 3.66f;
        var korpus = BraxUliceKit.Mat("meble sklepowe: korpus (laminat)", Color.white, .6f);
        var blat = BraxUliceKit.Mat("meble sklepowe: blat", new Color(.20f, .19f, .18f), .35f, .1f);
        var towar = BraxUliceKit.Mat("towar na półkach (kolory z wierzchołków)", Color.white, .55f);
        var lampa = BraxUliceKit.Mat("oprawy sufitowe (podświetlenie nocą)", new Color(.96f, .95f, .90f), .3f);
        var rng = new System.Random(lok.numer * 7919);
        // 3.11: w lokalach spożywczych prawdziwy towar (BraxSklepyTowar) zamiast kolorowych klocków na półkach
        bool prawdziwy = BraxSklepyTowar.Zastepuje(lok.typ); Transform tw = prawdziwy ? BraxSklepyTowar.Korzen(bg, lok.szyld) : null;
        Color32 Kol(float r, float g, float bb) => BraxUliceKit.C(r, g, bb);
        Color32 KolTowaru()
        {
            Color32[] p = lok.typ switch
            {
                "apteka" => new[] { Kol(.95f, .95f, .95f), Kol(.30f, .60f, .35f), Kol(.85f, .30f, .30f), Kol(.35f, .55f, .85f), Kol(.92f, .92f, .70f) },
                "ksiegarnia" => new[] { Kol(.60f, .15f, .15f), Kol(.15f, .25f, .50f), Kol(.80f, .70f, .40f), Kol(.25f, .45f, .30f), Kol(.90f, .88f, .82f), Kol(.20f, .20f, .22f) },
                "kwiaty" => new[] { Kol(.85f, .20f, .30f), Kol(.95f, .80f, .20f), Kol(.90f, .55f, .70f), Kol(.30f, .55f, .25f), Kol(.95f, .95f, .92f) },
                "piekarnia" or "cukiernia" => new[] { Kol(.80f, .58f, .30f), Kol(.70f, .45f, .22f), Kol(.92f, .80f, .60f), Kol(.55f, .32f, .18f) },
                "warzywniak" => new[] { Kol(.85f, .25f, .15f), Kol(.95f, .70f, .15f), Kol(.35f, .60f, .20f), Kol(.55f, .30f, .45f), Kol(.90f, .85f, .40f) },
                "bar" => new[] { Kol(.35f, .55f, .30f), Kol(.70f, .45f, .15f), Kol(.85f, .85f, .80f), Kol(.50f, .20f, .15f) },
                _ => new[] { Kol(.85f, .25f, .20f), Kol(.95f, .80f, .25f), Kol(.25f, .55f, .30f), Kol(.25f, .40f, .75f), Kol(.92f, .92f, .88f), Kol(.90f, .50f, .20f) },
            };
            return p[rng.Next(p.Length)];
        }
        var sk = BraxUliceKit.S(korpus); var sb = BraxUliceKit.S(blat); var st = BraxUliceKit.S(towar);
        // regał przyścienny z półkami i towarem: s0..s1 wzdłuż ściany tylnej (albo bocznej)
        void Regal(Vector3 o, Vector3 wzdl, Vector3 wglab, float dl, float glb, float wys, int polek)
        {
            Vector3 P(float a, float y, float g) => o + wzdl * a + Vector3.up * y + wglab * g;
            sk.kolor = lok.typ == "ksiegarnia" || lok.typ == "kawiarnia" || lok.typ == "cukiernia" ? Kol(.55f, .40f, .27f) : Kol(.93f, .93f, .91f);
            sk.Box(P(0, y0, 0), P(.03f, y0 + wys, glb)); sk.Box(P(dl - .03f, y0, 0), P(dl, y0 + wys, glb));   // boki
            sk.Box(P(.03f, y0, glb - .02f), P(dl - .03f, y0 + wys, glb));                                     // plecy
            for (int p = 0; p < polek; p++)
            {
                float y = y0 + .12f + p * (wys - .2f) / Mathf.Max(1, polek - 1);
                sk.Box(P(.03f, y - .025f, .0f), P(dl - .03f, y, glb - .02f));
                if (p == polek - 1) break;
                if (prawdziwy) { BraxSklepyTowar.Polka(lok.typ, tw, P(.06f, y + .004f, (glb - .02f) * .5f), wzdl, wglab, dl - .12f, rng); continue; }
                float x = .06f;
                while (x < dl - .12f)
                {
                    float wsz = .06f + (float)rng.NextDouble() * .16f, h = .12f + (float)rng.NextDouble() * .2f, g2 = .12f + (float)rng.NextDouble() * (glb - .2f);
                    if (lok.typ == "ksiegarnia") { wsz = .03f + (float)rng.NextDouble() * .04f; h = .2f + (float)rng.NextDouble() * .1f; g2 = glb - .06f; }
                    st.kolor = KolTowaru(); st.Box(P(x, y + .004f, .02f), P(Mathf.Min(x + wsz, dl - .06f), y + .004f + h, .02f + g2));
                    x += wsz + .015f;
                }
            }
            sk.kolor = BraxUliceKit.Bialy; st.kolor = BraxUliceKit.Bialy;
        }
        Vector3 W(float s, float y, float d) => f.P(s, y, d);
        float hR = Mathf.Min(2.2f * k, ySuf - y0 - .25f);
        // regał na ścianie tylnej (cała szerokość) i przy ścianie lewej (od strony bez drzwi)
        Regal(W(sIn0 + .1f, 0, dTyl + .5f), f.R, -f.N, sIn1 - sIn0 - .2f, .45f, hR, 5);
        if (f.drzwiS > 3.2f) Regal(W(sIn0 + .42f, 0, -1.1f), -f.N, -f.R, Mathf.Max(1f, gl - 2.4f), .42f, hR, 5);
        else Regal(W(sIn1 - .42f, 0, dTyl + 1.1f), f.N, f.R, Mathf.Max(1f, gl - 2.4f), .42f, hR, 5);
        // lada (wysokość 0.95 m × k) z blatem, przed regałem tylnym, z boku osi drzwi
        float ladaS0 = Mathf.Max(sIn0 + .6f, f.drzwiS + .9f), ladaS1 = Mathf.Min(sIn1 - .6f, ladaS0 + 2.4f * k);
        if (ladaS1 - ladaS0 < 1.2f) { ladaS1 = Mathf.Min(sIn1 - .6f, f.drzwiS - .9f); ladaS0 = Mathf.Max(sIn0 + .6f, ladaS1 - 2.4f * k); }
        float ladaD = dTyl + 1.6f * k, ladaH = .95f * k, ladaG = .6f * k;
        sk.kolor = lok.typ == "bar" ? Kol(.30f, .18f, .10f) : lok.typ == "kantor" || lok.typ == "poczta" ? Kol(.75f, .74f, .70f) : Kol(.88f, .86f, .82f);
        var a0 = W(ladaS0, y0, ladaD); var a1 = W(ladaS1, y0 + ladaH - .04f, ladaD - ladaG);
        sk.Box(a0, a1); sk.kolor = BraxUliceKit.Bialy;
        sb.Box(W(ladaS0 - .03f, y0 + ladaH - .04f, ladaD + .04f), W(ladaS1 + .03f, y0 + ladaH, ladaD - ladaG - .02f));
        var kl = new GameObject("kolizja: lada " + lok.szyld); kl.transform.SetParent(kolizje, false);
        var bc = kl.AddComponent<BoxCollider>(); var lo = Vector3.Min(a0, a1); var hi = Vector3.Max(a0, a1) + Vector3.up * .04f; bc.center = (lo + hi) * .5f; bc.size = hi - lo;
        if (lok.typ == "kantor" || lok.typ == "poczta" || lok.typ == "apteka")
        {   // szyba nad ladą (okienko kasowe)
            var szk = BraxUliceKit.SzkloPrzezroczyste;
            BraxUliceKit.S(szk).Quad(W(ladaS0, y0 + ladaH, ladaD - ladaG * .5f), W(ladaS1, y0 + ladaH, ladaD - ladaG * .5f), W(ladaS1, y0 + ladaH + .75f, ladaD - ladaG * .5f), W(ladaS0, y0 + ladaH + .75f, ladaD - ladaG * .5f), f.N);
        }
        if (lok.typ == "apteka")
        {
            // APTEKA (A 2.11): regał-wyspa z parafarmaceutykami (dwustronny) po stronie bez drzwi, zielony krzyż świetlny nad
            // regałem tylnym (3 bryły obok siebie — bez nakładania ramion), krzesełko przy ciśnieniomierzu przy witrynie
            float Lg = Mathf.Min(2.0f, (sIn1 - sIn0) * .3f), gs0 = f.drzwiS > (sIn0 + sIn1) * .5f ? sIn0 + 1.9f : sIn1 - 1.9f - Lg, gd = -3.2f, gw = .6f, gh = 1.45f * k;
            if (gs0 > sIn0 + 1.5f && gs0 + Lg < sIn1 - 1.5f && (gs0 > f.drzwiS + 1.1f || gs0 + Lg < f.drzwiS - 1.1f) && gd - gw * .5f > ladaD - ladaG + .3f)
            {
                sk.kolor = Kol(.93f, .93f, .91f);
                sk.Box(W(gs0, y0, gd + gw * .5f), W(gs0 + Lg, y0 + .1f, gd - gw * .5f));                                  // cokół
                sk.Box(W(gs0, y0 + .1f, gd + .02f), W(gs0 + Lg, y0 + gh, gd - .02f));                                   // plecy środkowe
                sk.Box(W(gs0, y0 + .1f, gd + gw * .5f), W(gs0 + .03f, y0 + gh, gd - gw * .5f)); sk.Box(W(gs0 + Lg - .03f, y0 + .1f, gd + gw * .5f), W(gs0 + Lg, y0 + gh, gd - gw * .5f));
                for (int p = 0; p < 4; p++)
                {
                    float y = y0 + .35f + p * (gh - .45f) / 3f;
                    foreach (float sg in new[] { -1f, 1f })
                    {
                        float dA = gd + sg * .02f, dB = gd + sg * gw * .5f;
                        sk.Box(W(gs0 + .03f, y - .02f, dA), W(gs0 + Lg - .03f, y, dB));
                        if (p == 3) continue;
                        if (prawdziwy) { BraxSklepyTowar.Polka(lok.typ, tw, W(gs0 + .07f, y + .004f, gd + sg * gw * .25f), f.R, -sg * f.N, Lg - .14f, rng); continue; }
                        for (float x = gs0 + .07f; x < gs0 + Lg - .12f;)
                        {
                            float wsz = .07f + (float)rng.NextDouble() * .12f, h = .1f + (float)rng.NextDouble() * .16f;
                            st.kolor = KolTowaru(); st.Box(W(x, y + .004f, dA + sg * .02f), W(Mathf.Min(x + wsz, gs0 + Lg - .06f), y + .004f + h, dA + sg * (gw * .5f - .04f)));
                            x += wsz + .015f;
                        }
                    }
                }
                sk.kolor = BraxUliceKit.Bialy; st.kolor = BraxUliceKit.Bialy;
                var kg = new GameObject("kolizja: regał-wyspa apteki"); kg.transform.SetParent(kolizje, false); var bcg = kg.AddComponent<BoxCollider>();
                var g0 = W(gs0, y0, gd + gw * .5f); var g1 = W(gs0 + Lg, y0 + gh, gd - gw * .5f); bcg.center = (g0 + g1) * .5f; bcg.size = new Vector3(Mathf.Abs(g1.x - g0.x), gh, Mathf.Abs(g1.z - g0.z));
            }
            // zielony krzyż świetlny nad regałem tylnym (na dwóch linkach z sufitu)
            var krz = BraxUliceKit.S(BraxMiastoA.Swieci("apteka: krzyż świetlny (zieleń)", new Color(.1f, .75f, .3f), new Color(.2f, 1.6f, .5f)));
            float kc = (sIn0 + sIn1) * .5f, kd = dTyl + .5f + .12f, ky = y0 + hR + .38f, ra = .2f, rb = .065f;
            krz.Box(W(kc - rb, ky - ra, kd + .03f), W(kc + rb, ky + ra, kd - .03f));
            krz.Box(W(kc - ra, ky - rb, kd + .03f), W(kc - rb, ky + rb, kd - .03f)); krz.Box(W(kc + rb, ky - rb, kd + .03f), W(kc + ra, ky + rb, kd - .03f));
            foreach (float o in new[] { -.12f, .12f }) BraxUliceKit.S(blat).Rura(W(kc + o, ky + ra, kd), W(kc + o, ySuf, kd), .004f, 4);
        }
        // kasa na ladzie
        st.kolor = Kol(.15f, .15f, .16f); st.Box(W(ladaS0 + .3f, y0 + ladaH, ladaD - .15f), W(ladaS0 + .62f, y0 + ladaH + .22f, ladaD - .45f)); st.kolor = BraxUliceKit.Bialy;
        if (prawdziwy)
        {   // towar na ladzie (za kasą, przodem do klienta) i wózki przy wejściu marketu (wsunięte, równolegle do witryny, od drzwi w stronę wolnej ściany)
            BraxSklepyTowar.Lada(lok.typ, tw, W(ladaS0 + .75f, y0 + ladaH, ladaD - ladaG * .5f), f.R, -f.N, ladaS1 - ladaS0 - .85f, rng);
            // tablica menu z grafiki właściciela (3.11 14:37) wisząca nad ladą na dwóch prętach, przodem do klienta
            string menu = lok.typ == "kawiarnia" ? "menu_1" : lok.typ == "pizzeria" ? "menu_3" : lok.typ == "bar" ? "menu_4" : null;
            var mm = menu != null && BraxMiastoAssety.Jest("miasto_sklepy") ? BraxMiastoAssety.Model("miasto_sklepy", menu) : null;
            if (mm)
            {
                float sc = (ladaS0 + ladaS1) * .5f, dm = ladaD - ladaG * .5f, yGora = ySuf - .1f;
                BraxMiastoAssety.Postaw(tw, mm, "Menu nad ladą: " + lok.szyld, W(sc, yGora - .675f * k, dm), Quaternion.LookRotation(f.N, Vector3.up).eulerAngles.y, 1f, true, false);
                foreach (float o in new[] { -.45f * k, .45f * k }) BraxUliceKit.S(blat).Rura(W(sc + o, yGora, dm), W(sc + o, ySuf, dm), .004f, 4);
            }
            if (lok.typ == "market")
            {
                bool wPrawo = f.drzwiS + 1.0f + 1.6f * k < sIn1 - .3f;
                BraxSklepyTowar.Wozki(tw, kolizje, W(f.drzwiS + (wPrawo ? .95f : -.95f), y0, -.6f), wPrawo ? f.R : -f.R, 2);
            }
        }
        // lampy sufitowe (panele)
        for (float s = sIn0 + 1.4f; s < sIn1 - .8f; s += 2.6f)
            foreach (float d in new[] { -1.8f, dTyl + 1.9f }) BraxUliceKit.S(lampa).Box(W(s - .5f, ySuf - .05f, d + .3f), W(s + .5f, ySuf, d - .3f), 8);
        // lokale gastronomiczne: stoliki i krzesła (osobne obiekty — siedziska BraxSeat), bar: hokery przy ladzie
        if (lok.typ == "kawiarnia" || lok.typ == "bistro" || lok.typ == "cukiernia" || lok.typ == "pizzeria" || lok.typ == "piekarnia")
        {
            int ile = 0;
            for (float s = sIn0 + 1.1f; s < f.drzwiS - 1.6f && ile < 3; s += 1.9f, ile++) Stolik(W(s, y0, -1.5f), f, bg, kolizje);
            for (float s = f.drzwiS + 1.8f; s < sIn1 - 1.0f && ile < 4; s += 1.9f, ile++) if (s < ladaS0 - .9f || s > ladaS1 + .9f) Stolik(W(s, y0, -1.5f), f, bg, kolizje);
        }
        if (lok.typ == "bar")
            for (float s = ladaS0 + .35f; s < ladaS1 - .2f; s += .7f) Hoker(W(s, y0, ladaD + .45f), bg);
        if (prawdziwy) BraxSklepyTowar.Zakoncz(tw, lok.szyld);
        BraxUliceKit.Punkt("ulica_" + b.id + "_lada", "za ladą: " + lok.szyld + " — miejsce zleceniodawcy (sprzedawca / właściciel lokalu)", W((ladaS0 + ladaS1) * .5f, y0, ladaD - ladaG - .55f));
    }

    static readonly Dictionary<string, Mesh> meble = new Dictionary<string, Mesh>();
    static Mesh Siatka(string klucz, Action<UlSiatka> buduj)
    {
        if (meble.TryGetValue(klucz, out var m) && m) return m;
        var s = new UlSiatka(); buduj(s); m = s.Zbuduj("Ulice: " + klucz);
        System.IO.Directory.CreateDirectory(BraxUliceKit.Out);
        BraxZapisAssetu.Utworz(m, BraxUliceKit.Out + "/mebel_" + BraxUliceKit.Bezpieczna(klucz) + ".asset");
        meble[klucz] = m; return m;
    }
    static GameObject Mebel(string nazwa, Transform rodzic, Mesh mesh, Material mat, Vector3 at, float yaw)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false); g.transform.position = at; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }
    static void Stolik(Vector3 at, Fasada f, Transform bg, Transform kolizje)
    {
        float k = BraxUliceKit.K;
        var drewno = BraxUliceKit.Mat("meble kawiarni: drewno dębowe", new Color(.46f, .32f, .20f), .55f);
        var stal = BraxUliceKit.Mat("meble kawiarni: stal czarna", new Color(.06f, .06f, .065f), .45f, .5f);
        var blat = Siatka("stolik kawiarniany", s => { s.Walec(new Vector3(0, .72f * k - .03f, 0), .35f * k, .03f, 20); });
        var noga = Siatka("noga stolika", s => { s.Walec(Vector3.zero, .22f * k, .02f, 16); s.Rura(new Vector3(0, .02f, 0), new Vector3(0, .72f * k - .03f, 0), .03f, 8); });
        Mebel("Stolik kawiarniany", bg, blat, drewno, at, 0); Mebel("Stolik kawiarniany: noga", bg, noga, stal, at, 0);
        var siedzisko = Siatka("krzesło kawiarniane", s =>
        {
            float h = .45f * k, w = .42f * k;
            s.Box(new Vector3(-w * .5f, h - .03f, -w * .5f), new Vector3(w * .5f, h, w * .5f));
            s.Box(new Vector3(-w * .5f, h, w * .5f - .03f), new Vector3(w * .5f, h + .42f * k, w * .5f));
            foreach (float x in new[] { -w * .5f + .02f, w * .5f - .02f }) foreach (float z in new[] { -w * .5f + .02f, w * .5f - .02f }) s.Rura(new Vector3(x, 0, z), new Vector3(x, h - .03f, z), .014f, 6);
        });
        foreach (float sg in new[] { -1f, 1f })
        {
            var p = at + f.R * sg * .62f * k;
            var g = Mebel("Krzesło kawiarniane", bg, siedzisko, drewno, p, Quaternion.LookRotation(-f.R * sg).eulerAngles.y);
            var c = g.AddComponent<BoxCollider>(); c.center = new Vector3(0, .3f * k, 0); c.size = new Vector3(.42f * k, .6f * k, .42f * k);
        }
        var kc = new GameObject("kolizja: stolik"); kc.transform.SetParent(kolizje, false); kc.transform.position = at + Vector3.up * .36f * k;
        var col = kc.AddComponent<CapsuleCollider>(); col.radius = .3f * k; col.height = .72f * k;
    }
    static void Hoker(Vector3 at, Transform bg)
    {
        float k = BraxUliceKit.K;
        var skora = BraxUliceKit.Mat("hoker: skóra czarna", new Color(.06f, .05f, .05f), .5f);
        var m = Siatka("hoker barowy", s => { s.Walec(Vector3.zero, .2f * k, .02f, 16); s.Rura(new Vector3(0, .02f, 0), new Vector3(0, .74f * k, 0), .035f, 8); s.Walec(new Vector3(0, .74f * k, 0), .19f * k, .07f, 18); s.Pierscien(new Vector3(0, .3f * k, 0), .15f * k, .17f * k); });
        var g = Mebel("Hoker barowy", bg, m, skora, at, 0);
        var c = g.AddComponent<CapsuleCollider>(); c.center = new Vector3(0, .4f * k, 0); c.radius = .2f * k; c.height = .8f * k;
    }
}

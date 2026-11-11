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
        { "apothecary",          new Lokal("APTEKA", "apteka", 5, 3) { wysiegnik = true, licoSzyldu = new Color(.10f, .55f, .28f), wariantWitryny = 7 } },
        { "corner_store",        new Lokal("DELIKATESY", "market", 7, 0) { markiza = true, markizaA = BraxUliceKit.C(.12f, .34f, .20f), markizaB = BraxUliceKit.C(.92f, .90f, .84f), wariantWitryny = 7 } },
        { "bar",                 new Lokal("BAR 24", "bar", 13, 2) { wysiegnik = true, licoSzyldu = new Color(.92f, .70f, .22f) } },
        { "restaurant",          new Lokal("BISTRO", "bistro", 15, 4) { markiza = true, markizaA = BraxUliceKit.C(.55f, .10f, .10f), markizaB = BraxUliceKit.C(.93f, .91f, .86f), wariantWitryny = 6 } },
        { "workshop_shop",       new Lokal("KWIACIARNIA", "kwiaty", 17, 1) { markiza = true, markizaA = BraxUliceKit.C(.20f, .42f, .28f), markizaB = BraxUliceKit.C(.95f, .94f, .90f) } },
        { "hotel",               new Lokal("HOTEL", "hotel", 19, 5) { wysiegnik = true, licoSzyldu = new Color(.86f, .72f, .40f), wariantWitryny = 6 } },
        { "viaduct_corner",      new Lokal("KANTOR", "kantor", 21, 3) { wysiegnik = true, licoSzyldu = new Color(.95f, .80f, .20f) } },
        { "workshop",            new Lokal("WARSZTAT ROWEROWY", "warsztat", 23, 2) },
        { "estate_corner",       new Lokal("KAWIARNIA", "kawiarnia", 25, 0) { markiza = true, markizaA = BraxUliceKit.C(.32f, .20f, .13f), markizaB = BraxUliceKit.C(.90f, .84f, .72f), wariantWitryny = 6 } },
        { "bakery",              new Lokal("PIEKARNIA", "piekarnia", 6, 4) { markiza = true, markizaA = BraxUliceKit.C(.62f, .38f, .12f), markizaB = BraxUliceKit.C(.95f, .92f, .84f) } },
        { "gallery",             new Lokal("BRAXHORN FITNESS", "fitness", 10, 3) { wysiegnik = true, licoSzyldu = new Color(.93f, .45f, .62f), wariantWitryny = 5 } },   // pkt 59: fitness Nyxy (wcześniej GALERIA SZTUKI)
        { "office_entrance",     new Lokal("POCZTA", "poczta", 12, 1) { wysiegnik = true, licoSzyldu = new Color(.95f, .78f, .10f) } },
        { "apartments_1",        new Lokal("KSIĘGARNIA", "ksiegarnia", 14, 0) { wariantWitryny = 3 } },
        { "apartments_2",        new Lokal("CUKIERNIA", "cukiernia", 16, 5) { markiza = true, markizaA = BraxUliceKit.C(.78f, .45f, .55f), markizaB = BraxUliceKit.C(.97f, .95f, .93f) } },
        { "apartments_3",        new Lokal("OPTYK", "optyk", 18, 4) { wysiegnik = true, licoSzyldu = new Color(.15f, .35f, .62f) } },
        { "estate_apartments_1", new Lokal("WARZYWA OWOCE", "warzywniak", 20, 1) { markiza = true, markizaA = BraxUliceKit.C(.18f, .40f, .16f), markizaB = BraxUliceKit.C(.95f, .93f, .86f) } },
        { "estate_apartments_2", new Lokal("PIZZERIA", "pizzeria", 22, 2) { markiza = true, markizaA = BraxUliceKit.C(.60f, .12f, .10f), markizaB = BraxUliceKit.C(.20f, .38f, .18f), wariantWitryny = 6 } },
    };
    // Boki widoczne z ulicy (bez sąsiada; analiza obrysów kolizji oryginału): zachód / wschód.
    static readonly HashSet<string> BokZachod = new HashSet<string> { "estate_corner", "viaduct_corner", "apartments_3", "estate_apartments_2" };
    static readonly HashSet<string> BokWschod = new HashSet<string> { "workshop", "bar", "estate_apartments_1" };

    static Styl[] style;
    static int nOkien, nWitryn, nLokali, nBokow, nDrzwi;
    // 9.11 (z buildu 6.11, 9d9aff5): dokładne otwory okien mieszkania Braxa z TEGO generatora elewacji nadbudowy NOVA.
    // Bez tego mieszkanie szukało szyb po nazwie materiału „glass” w nieistniejącej już starej fasadzie → okna=0 (APK 9.11).
    static readonly List<Rect> oknaMieszkaniaNOVA = new List<Rect>();
    public static Rect[] OknaMieszkaniaNOVA => oknaMieszkaniaNOVA.ToArray();

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxUliceKit.WyczyscOkna(); BraxUliceKit.Usuniete.Clear();
        nOkien = nWitryn = nLokali = nBokow = nDrzwi = 0; licaZrodla = 0; oknaMieszkaniaNOVA.Clear();
        var root = BraxRegion.Root(cityRoot, "CLAUDE ulice: pierzeje (elewacje od nowa)");
        var kolizje = new GameObject("kolizje parterów i brył").transform; kolizje.SetParent(root, false);
        ZdejmijWindyWiezowcow();
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
            // 10.11 (właściciel 08:00: „każdy lokal po kolei… zdjęcie, jak wygląda w środku, każdy jeden”): parter wieżowca ma ten sam hol co
            // reszta kamienic (wieża stoi od wysokości budynku w górę) — APTEKA, DELIKATESY, BISTRO, HOTEL, KAWIARNIA, KSIĘGARNIA, PIZZERIA
            // dostają wnętrze i szybę przezroczystą (wcześniej szyba „pokój za szybą” i pusto); fitness w wieży galerii bez zmian.
            // Bar 24 (ETAP-GALERIA p. 4 — R1, nikt go nie zrobił): bar z butelkami, hokery, loże, tablica zakładów.
            if (!(wiez && lok.typ == "fitness"))
            {
                BraxUliceKit.UstawGrupe("Wnętrze lokalu " + lok.szyld, 45f);
                if (lok.typ == "fitness") { try { BraxMiastoFitness.Buduj(bg, f.A + f.R * f.drzwiS); } catch (System.Exception e) { Debug.LogWarning("BRAXHORN_FITNESS DO POPRAWKI " + e); } }
                else { UlSiatka.ZaokraglenieBox = .008f; try { Wnetrze(f, bg, kolizje); } finally { UlSiatka.ZaokraglenieBox = 0f; } }
                nLokali++;
            }
            Drzwi(bg, b, lok, f);
            gotowe.Add(lok.szyld + " nr " + lok.numer);
        }
        // 10.11: lica oryginału na bokach WSZYSTKICH budynków pierzei (x0, x1) — renowacja cięła 5 mm do środka, więc ściany
        // oryginału dokładnie na granicy zostały: bok BAR 24 od dziedzińca siłowni (x −1.0), BRAXHORN TOWER od nabrzeża (x 66,
        // stara elewacja biurowa CityMesh_237 0…18 m), boki przy ul. Wiaduktowej i na końcach ulicy. Między dwoma naszymi
        // budynkami to ściany wewnątrz brył (niewidoczne) — zdjęcie ich niczego nie odsłania.
        foreach (var b in BraxCityArchitecture.StreetBuildings)
        {
            float yG = Mathf.Max(b.Height, 20f) + .6f;
            licaZrodla += ZrodloWLicuX(b.x0, b.z0, b.z1, .18f, yG, "bok_zach_" + b.id);
            licaZrodla += ZrodloWLicuX(b.x1, b.z0, b.z1, .18f, yG, "bok_wsch_" + b.id);
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
            // 10.11 (właściciel 10:41, zdjęcie z APK 9.11: „nad tą fasadą jakaś przerwa jest między budynkiem”): elewacja liczona
            // we współrzędnych świata, a grupa nadbudowy stoi na y = 5.1 — cała elewacja NOVA wisiała 5.1 m za wysoko (od 10.2 m),
            // między parterem klubu a elewacją widać było gołą ścianę mieszkania i pokoje. Zapis pod uchwyt w układzie świata.
            var uchwyt = new GameObject("CLAUDE A: elewacja nadbudowy NOVA (układ świata)").transform;
            uchwyt.SetParent(gNova, false); uchwyt.position = Vector3.zero; uchwyt.rotation = Quaternion.identity;
            siatek += BraxUliceKit.Zapisz(uchwyt, "pierzeje_nova_gora");
            BraxRegion.Note("pierzeje: elewacja nadbudowy NOVA w układzie świata (grupa nadbudowy y " + gNova.position.y.ToString("F2") + ", uchwyt y " + uchwyt.position.y.ToString("F2") + ")");
        }
        BraxRegion.Note("pierzeje: cofnięte stare bryły w płaszczyźnie elewacji: " + skrocone);
        Debug.Log("BRAXHORN_ULICE_LICA_ZRODLA zdjęte lica oryginału miasta w płaszczyźnie nowych elewacji (boki, nadbudowy): siatek " + licaZrodla);
        string usun = string.Join("; ", BraxUliceKit.Usuniete.GroupBy(x => x).Select(g => g.Key + (g.Count() > 1 ? " ×" + g.Count() : "")));
        BraxRegion.Note("pierzeje: budynków " + gotowe.Count + " (" + string.Join(", ", gotowe) + "), okien " + nOkien + ", witryn " + nWitryn + ", boków " + nBokow + ", lokali urządzonych " + nLokali + ", siatek " + siatek);
        Debug.Log("BRAXHORN_ULICE_PIERZEJE budynki=" + gotowe.Count + " okna=" + nOkien + " witryny=" + nWitryn + " boki=" + nBokow + " lokale=" + nLokali + " drzwi_premium=" + nDrzwi + " siatki=" + siatek + " k=" + BraxUliceKit.K.ToString("F3"));
        Debug.Log("BRAXHORN_ULICE_USUNIETE pierzeje: " + BraxUliceKit.Usuniete.Count + " obiektów — " + usun);
    }

    // 10.11 (właściciel 09:21, zdjęcia z APK: „elewacja koło siłowni migota”, „tam, gdzie boiska — tak samo migota”).
    // PRZYCZYNA (pomiar braxhorn-city.glb, oś x odbita jak w imporcie): renowacja wycina oryginał w bryle budynku z marginesem
    // 5 mm DO ŚRODKA (x0 + .005 … x1 − .005; nadbudowy dokładnie po obrysie), a cięcie zostawia trójkąty leżące NA granicy bryły.
    // Zostały więc zewnętrzne ściany oryginału dokładnie w licach nowych elewacji: bok BAR 24 x −1.0 (CityMesh_12, 0…14 m),
    // tył nadbudowy NOVA z 62.0 i boki x 32 / 52 (CityMesh_680, czerwona cegła 4.8…10.5 m — widać ją było w oknach od boisk),
    // tył i bok nadbudowy NUTRITION (z 11.6, x 14.5). Dwie powierzchnie w jednej płaszczyźnie = migotanie, a we wnękach okien
    // stała cegła oryginału. Teraz z oryginału (tylko siatki CityMesh_*, nasze elewacje nietknięte) zdejmujemy płytę ±4 cm wokół
    // każdego takiego lica.
    static int licaZrodla;
    internal static int ZrodloWPlycie(Vector3 lo, Vector3 hi, string etykieta)
    {
        var plyta = new Bounds(); plyta.SetMinMax(lo, hi);
        int n = 0;
        foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!mf || !mf.gameObject.name.StartsWith("CityMesh_", StringComparison.Ordinal)) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled || !mf.sharedMesh || !r.bounds.Intersects(plyta)) continue;
            try { n += BraxCityMeshCut.Subtract(mf.transform, plyta, BraxUliceKit.Out + "/LicaZrodla", "lico_" + etykieta); }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_ULICE_LICA_ZRODLA " + etykieta + " " + mf.name + ": " + e.Message); }
        }
        return n;
    }
    static int ZrodloWLicuX(float x, float z0, float z1, float y0, float y1, string etykieta) =>
        ZrodloWPlycie(new Vector3(x - .04f, y0, z0 - .02f), new Vector3(x + .04f, y1, z1 + .02f), etykieta);
    static int ZrodloWLicuZ(float z, float x0, float x1, float y0, float y1, string etykieta) =>
        ZrodloWPlycie(new Vector3(x0 - .02f, y0, z - .04f), new Vector3(x1 + .02f, y1, z + .04f), etykieta);

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
        // 10.11 (właściciel 10:41: „nad fasadą NOVA jakaś przerwa między budynkiem”): bounds są w świecie, a Box rysuje w układzie
        // rodzica. Grupy nadbudów („… preserved interior upper storeys”) stoją na y = baseY (5.1 / 4.6 m), więc boki i dach nadbudów
        // NUTRITION, NOVA i bramy podwórka lądowały 5.1 / 4.6 m za wysoko (szpara nad parterem, dach w powietrzu). Teraz środek
        // przeliczony do układu rodzica (grupy bez obrotu i skali).
        if (mat) BraxCityArchitecture.Box(nazwa + " (cofnięte 6 cm za elewację)", parent, parent ? parent.InverseTransformPoint((lo + hi) * .5f) : (lo + hi) * .5f, hi - lo, mat, false);
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
        // lica oryginału na obrysie nadbudowy (boki x0 / x1 i tył) — od poziomu nadbudowy w górę; parter (lokal) nietknięty
        float yN0 = baseY + .02f, yN1 = baseY + 3.75f + floors * 3.2f + 1.4f;
        licaZrodla += ZrodloWLicuX(x0, z0, z1, yN0, yN1, "nadbudowa_" + id + "_zach");
        licaZrodla += ZrodloWLicuX(x1, z0, z1, yN0, yN1, "nadbudowa_" + id + "_wsch");
        licaZrodla += ZrodloWLicuZ(far ? z1 : z0, x0, x1, yN0, yN1, "nadbudowa_" + id + "_tyl");
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
    // 11.11 (właściciel 19:33 „w sklepach, w piekarniach windy są powstawiane”; lista zatwierdzona 20:08: „wszystkie te windy, które
    // wchodzą do sklepu, wypierdalają… nie musi każdy jeden budynek być dostępny”): 8 szybów BraxCityTowerLiftNetwork stało 4.85 m za
    // frontem — w środku lokali (APTEKA, DELIKATESY, BISTRO, HOTEL, KAWIARNIA, BRAXHORN FITNESS, KSIĘGARNIA, PIZZERIA). Zdjęte całe
    // (szyb, kabina, drzwi, płyty i barierki dachu), wykreślone ze świata (windy i punkty R1_TOWER_<id>_*). Winda BRAXHORN TOWER
    // przy nabrzeżu (quay_corner) zostaje.
    static readonly string[] WindyWLokalach = { "apothecary", "corner_store", "restaurant", "hotel", "estate_corner", "gallery", "apartments_1", "estate_apartments_2" };
    static void ZdejmijWindyWiezowcow()
    {
        var zdjete = new List<string>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t || !t.name.StartsWith("REAL SKYSCRAPER ELEVATOR ")) continue;
            string id = t.name.Substring("REAL SKYSCRAPER ELEVATOR ".Length);
            if (System.Array.IndexOf(WindyWLokalach, id) < 0) continue;
            zdjete.Add(id); UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        var swiat = UnityEngine.Object.FindFirstObjectByType<BraxCityWorld>(FindObjectsInactive.Include);
        if (swiat)
        {
            swiat.additionalTowerElevators = swiat.additionalTowerElevators.Where(e => e).ToArray();
            swiat.points = swiat.points.Where(p => p == null || !WindyWLokalach.Any(id => p.id != null && p.id.StartsWith("R1_TOWER_" + id + "_"))).ToArray();
        }
        Debug.Log("BRAXHORN_WINDY_W_LOKALACH zdjęte " + zdjete.Count + "/8: " + string.Join(", ", zdjete) + " (zostaje winda BRAXHORN TOWER)");
    }

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
        var szybaSklep = (f.wiezowiec && f.lok.typ == "fitness") || !f.front ? BraxUliceKit.OknoPokoj(f.bay, 3.6f, Chodnik + f.A.y, f.lok.wariantWitryny, 1, 3.8f, offU) : BraxUliceKit.SzkloPrzezroczyste;
        var szkloMieszkania = BraxUliceKit.Mat("szkło okna mieszkania (glass, przezroczyste)", new Color(.62f, .70f, .72f, .2f), .05f, .1f);
        var aluminium = BraxUliceKit.Mat("aluminium witryn grafit", new Color(.09f, .10f, .105f), .4f, .55f);
        foreach (var o in otwory)
        {
            if (o.drzwi) continue;
            if (o.witryna) { Witryna(f, o, szybaSklep, aluminium); nWitryn++; continue; }
            bool oknoMieszkania = f.szkloOkien.width > 0f && f.szkloOkien.Contains(o.r.center);
            if (oknoMieszkania && f.gora && f.b.id == "nadbudowa_NOVA")
            {
                var wa = f.P(o.r.xMin, o.r.yMin, -o.gleb + .04f); var wb = f.P(o.r.xMax, o.r.yMax, -o.gleb + .04f);
                oknaMieszkaniaNOVA.Add(Rect.MinMaxRect(Mathf.Min(wa.x, wb.x), Mathf.Min(wa.y, wb.y), Mathf.Max(wa.x, wb.x), Mathf.Max(wa.y, wb.y)));
            }
            Okno(f, o, oknoMieszkania ? szkloMieszkania : szybaPokoj, !f.front && o.r.yMax < Parter); nOkien++;
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
            if (!szyldyFirm) szyldyFirm = new GameObject("CLAUDE: szyldy firm z grafik (WAWER, SOBCZAK)").transform;
            if (BraxMiastoBillboardy.Mural(szyldyFirm, firma, f.P(sc, 3.33f, .06f), f.N, wG, hG)) { OBox(f, listwa, sc - wG * .5f - .06f, sc + wG * .5f + .06f, 3.33f - hG * .5f - .04f, 3.33f + hG * .5f + .04f, 0f, .05f); goto poSzyldzie; }
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
        // 11.11 (właściciel 19:33–19:45: „straszne te drzwi do lokali. Nie otwierają się… słabe, z przerwami… mają być konkretne…
        // klamki”): stare drzwi przebudowy (skrzydła-klocki, szczeliny 12 cm, otwarty pas nad nadprożem) zdjęte, w otworze elewacji
        // (drzwiS ± 1.17 m, Chodnik…3.2 m, ościeże 0.3 m) nowe drzwi premium: ościeżnica, naświetle, dwa szklane skrzydła z pochwytami
        string id = "R1_PUBLIC_DOOR_" + b.id;
        int stare = BraxDrzwiLokalu.UsunStare(id);
        BraxDrzwiLokalu.Podwojne(bg, id, lok.szyld, f.P(f.drzwiS, Chodnik, 0f), f.N, 2.34f, 3.2f - Chodnik, .3f, 0f, BraxDrzwiLokalu.ModelDla(lok.typ));
        nDrzwi++; if (stare == 0) Debug.LogWarning("BRAXHORN_DRZWI_LOKALI brak starych drzwi " + id + " (nowe postawione)");
        BraxUliceKit.Punkt("ulica_" + b.id + "_drzwi", "przed drzwiami: " + lok.szyld + " (nr " + lok.numer + ") — AKCJA = drzwi; miejsce zlecenia przy wejściu", f.P(f.drzwiS, Chodnik, 1.2f));
    }

    // ---------------- wnętrze lokalu za witryną ----------------
    // wysokość sufitu lokalu (nad chodnikiem 0.15 układu ulicy); ulica główna 3.66, kamienice miasta B — pod fryzem (WnetrzeKamienicy)
    static float SufitLokalu = 3.66f;
    // odsunięcie ścian bocznych wnętrza od lica (ulica 0.33; kamienice 0.36 — 4 cm od wnęk witryn sąsiednich stron, bez migotania)
    static float OdsuniecieScian = .33f;

    /// <summary>11.11 (właściciel 19:35–20:06: FRYZJER z regałem książek, KEBAB z półkami sklepowymi — w kamienicach miasta B za
    /// witryną był tylko obrazek pokoju; „każdy lokal… wejście… kebab ma być kebabem… wszystko ma być porobione”): prawdziwe wnętrze
    /// lokalu kamienicy — ten sam generator branż co ulica główna (posadzka, ściany, sufit, lady, regały, towar, rożna, stoliki).
    /// Układ jak elewacja ulicy: A = punkt lica na poziomie (chodnik − 0.15), R wzdłuż lica (s 0…W), N na zewnątrz; drzwiS = oś drzwi.</summary>
    public static void WnetrzeKamienicy(Transform bud, string szyld, string typ, int numer, Vector3 A, Vector3 R, Vector3 N, float W, float glebokosc, float drzwiS, float ySufitu, float odsuniecieScian = .33f)
    {
        var kol = new GameObject("kolizje wnętrza lokalu " + szyld).transform; kol.SetParent(bud, false);
        var lok = new Lokal(szyld, typ, numer, 0);
        var b = new BraxCityArchitecture.Building("kamienica_" + numer + "_" + BraxUliceKit.Bezpieczna(szyld), szyld, 0f, W, 0f, glebokosc, 1);
        var f = new Fasada { b = b, A = A, R = R, N = N, W = W, lok = lok, drzwiS = drzwiS, front = true };
        float stary = SufitLokalu, staryOds = OdsuniecieScian; SufitLokalu = ySufitu; OdsuniecieScian = odsuniecieScian;
        BraxUliceKit.UstawGrupe("Wnętrze lokalu " + szyld + " (kamienica miasta B)", 45f);
        UlSiatka.ZaokraglenieBox = .008f;
        try { Wnetrze(f, bud, kol); }
        finally { SufitLokalu = stary; OdsuniecieScian = staryOds; UlSiatka.ZaokraglenieBox = 0f; }
        BraxUliceKit.Zapisz(bud, "kamienica_lokal_" + numer);
        nLokaliKamienic++;
    }
    public static int nLokaliKamienic;

    static void Wnetrze(Fasada f, Transform bg, Transform kolizje)
    {
        var b = f.b; float k = BraxUliceKit.K; var lok = f.lok;
        float gl = Mathf.Min(7.05f, (b.z1 - b.z0) - .55f);        // głębokość holu (BraxCityArchitecture.Build)
        float sIn0 = OdsuniecieScian + .01f, sIn1 = f.W - OdsuniecieScian - .01f, dTyl = -(gl - .14f), y0 = .11f, ySuf = SufitLokalu;
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
        // MIASTO EKSKLUZYWNE (właściciel 10.11 09:04: „w mieście bardziej ekskluzywnie… fawela to fawela”): regały i lady z okleiny
        // drewnianej (dąb — sklepy, orzech — gastronomia, hotel, kantor), blaty z polerowanego granitu; apteka biała (nowoczesna), warsztat stal
        bool ciemne = lok.typ == "bistro" || lok.typ == "pizzeria" || lok.typ == "bar" || lok.typ == "hotel" || lok.typ == "kantor";
        var okleina = lok.typ == "apteka" ? null : lok.typ == "warsztat" ? BraxUliceKit.Mat("regał warsztatu: stal malowana", new Color(.32f, .34f, .36f), .5f, .4f)
            : BraxUliceKit.Tex(ciemne ? "meble: okleina orzech" : "meble: okleina dąb", 1.6f, ciemne ? new Color(.62f, .5f, .4f) : (Color?)null, ciemne ? new Color(.3f, .2f, .12f) : new Color(.6f, .45f, .3f), .45f, "teak_veneer", "dark_wood");
        var granit = BraxUliceKit.Tex("blat: granit polerowany", 1.2f, null, new Color(.2f, .19f, .18f), .22f, "granite_tile_02", "granite_tile");
        // delikatesy (10.11 08:09 „prawdziwe jedzenie”): dół — warzywa i owoce w skrzynkach, środek — towar z etykiet, góra — pieczywo
        string TypPolki(int p, int polek) => lok.typ == "market" ? (p == 0 ? "warzywa_dol" : p == polek - 2 ? "pieczywo" : new[] { "market", "sm_g3", "sm_g5", "sm_g4" }[p % 4]) : lok.typ;
        Wykonczenie(f, gl);
        // regał przyścienny z półkami i towarem: s0..s1 wzdłuż ściany tylnej (albo bocznej)
        void Regal(Vector3 o, Vector3 wzdl, Vector3 wglab, float dl, float glb, float wys, int polek)
        {
            Vector3 P(float a, float y, float g) => o + wzdl * a + Vector3.up * y + wglab * g;
            var sr = okleina ? BraxUliceKit.S(okleina) : sk; sk.kolor = Kol(.95f, .95f, .94f);
            bool optyk = lok.typ == "optyk" && Vector3.Dot(wglab, f.N) < -.9f;   // ściana oprawek na ścianie tylnej (boczny regał — towar)
            sr.Box(P(0, y0, 0), P(.03f, y0 + wys, glb)); sr.Box(P(dl - .03f, y0, 0), P(dl, y0 + wys, glb));   // boki
            // plecy; optyk — ŚCIANA OPRAWEK: plecy podświetlone (mleczny panel), oprawki z kodu w pasie 0.75–2.1 m × K
            (optyk ? BraxUliceKit.S(BraxMiastoA.Swieci("optyk: plecy ekspozycji (podświetlone)", new Color(.95f, .95f, .94f), new Color(.55f, .55f, .52f))) : sr)
                .Box(P(.03f, y0, glb - .02f), P(dl - .03f, y0 + wys, glb));
            for (int p = 0; p < polek; p++)
            {
                float y = y0 + .12f + p * (wys - .2f) / Mathf.Max(1, polek - 1);
                sr.Box(P(.03f, y - .025f, .0f), P(dl - .03f, y, glb - .02f));
                if (p == polek - 1) break;
                if (optyk && y > y0 + .85f * k && y < y0 + 2.0f * k)
                {   // rząd oprawek co 27 cm × K, przodem do sali; rzędy na zmianę: korekcyjne (szkła przezroczyste) i przeciwsłoneczne
                    bool sl = p % 2 == 1;
                    for (float a = .17f * k; a < dl - .17f * k; a += .27f * k) { LosujOkulary(rng, sl, out int mo, out var ko, out int pz); Okulary(P(a, y + .0005f, .05f), -wglab, ko, k, mo, pz); }
                    continue;
                }
                if (prawdziwy)
                {   // apteka: leki w dwóch warstwach (jak w prawdziwej aptece — bez pustej ściany nad pudełkami)
                    float odst = (wys - .2f) / Mathf.Max(1, polek - 1);
                    for (int wa = 0; wa < (lok.typ == "apteka" ? 2 : 1); wa++)
                        BraxSklepyTowar.Polka(TypPolki(p, polek), tw, P(.06f, y + .004f + wa * .068f, (glb - .02f) * .5f), wzdl, wglab, dl - .12f, rng, odst - .05f - wa * .068f);
                    continue;
                }
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
        bool regaly = lok.typ != "hotel" && lok.typ != "kantor" && lok.typ != "fryzjer";   // hotel: recepcja i hol, kantor: tablica kursów, fryzjer: stanowiska — bez regałów z towarem
        int nPolek = lok.typ == "apteka" ? 9 : lok.typ == "optyk" ? 8 : 5;   // apteka: gęste półki co ok. 0.33 m; optyk: ściana oprawek
        if (regaly) Regal(W(sIn0 + .1f, 0, dTyl + .5f), f.R, -f.N, sIn1 - sIn0 - .2f, .45f, hR, nPolek);
        // gastronomia (bistro, pizzeria, kawiarnia, bar): regał tylko za ladą — na sali stoliki, nie półki jak w sklepie
        bool gastro = lok.typ == "bistro" || lok.typ == "pizzeria" || lok.typ == "kawiarnia" || lok.typ == "bar";
        if (regaly && lok.typ != "warsztat" && !gastro)
        {
            if (f.drzwiS > 3.2f) Regal(W(sIn0 + .42f, 0, -1.1f), -f.N, -f.R, Mathf.Max(1f, gl - 2.4f), .42f, hR, nPolek);
            else Regal(W(sIn1 - .42f, 0, dTyl + 1.1f), f.N, f.R, Mathf.Max(1f, gl - 2.4f), .42f, hR, nPolek);
        }
        // lada (wysokość 0.95 m × k) z blatem, przed regałem tylnym, z boku osi drzwi
        float ladaS0 = Mathf.Max(sIn0 + .6f, f.drzwiS + .9f), ladaS1 = Mathf.Min(sIn1 - .6f, ladaS0 + 2.4f * k);
        if (ladaS1 - ladaS0 < 1.2f) { ladaS1 = Mathf.Min(sIn1 - .6f, f.drzwiS - .9f); ladaS0 = Mathf.Max(sIn0 + .6f, ladaS1 - 2.4f * k); }
        float ladaD = dTyl + 1.6f * k, ladaH = .95f * k, ladaG = .6f * k;
        sk.kolor = lok.typ == "bar" ? Kol(.30f, .18f, .10f) : lok.typ == "hotel" ? Kol(.38f, .24f, .14f) : lok.typ == "kantor" || lok.typ == "poczta" ? Kol(.75f, .74f, .70f) : Kol(.88f, .86f, .82f);
        var a0 = W(ladaS0, y0, ladaD); var a1 = W(ladaS1, y0 + ladaH - .04f, ladaD - ladaG);
        if (okleina)
        {   // lada: front i boki z okleiny na cofniętym cokole, blat z granitu z nawisem
            BraxUliceKit.S(BraxUliceKit.Mat("lada: cokół (czarny)", new Color(.06f, .06f, .06f), .5f)).Box(W(ladaS0 + .05f, y0, ladaD - .07f), W(ladaS1 - .05f, y0 + .09f, ladaD - ladaG + .05f));
            BraxUliceKit.S(okleina).Box(W(ladaS0, y0 + .09f, ladaD), W(ladaS1, y0 + ladaH - .04f, ladaD - ladaG));
            BraxUliceKit.S(granit).Box(W(ladaS0 - .03f, y0 + ladaH - .04f, ladaD + .05f), W(ladaS1 + .03f, y0 + ladaH, ladaD - ladaG - .02f));
        }
        else
        {
            sk.Box(a0, a1); sk.kolor = BraxUliceKit.Bialy;
            sb.Box(W(ladaS0 - .03f, y0 + ladaH - .04f, ladaD + .04f), W(ladaS1 + .03f, y0 + ladaH, ladaD - ladaG - .02f));
        }
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
        // kasa na ladzie: model Poly Haven (CashRegister_01; w hotelu laptop recepcji) przodem do sprzedawcy; bez modelu — bryła
        if (!PH(bg, lok.typ == "hotel" ? "classic_laptop" : "CashRegister_01", "Kasa: " + lok.szyld, W(ladaS0 + .46f, y0 + ladaH, ladaD - .3f), Quaternion.LookRotation(-f.N, Vector3.up).eulerAngles.y, (lok.typ == "hotel" ? .25f : .35f) * k))
        { st.kolor = Kol(.15f, .15f, .16f); st.Box(W(ladaS0 + .3f, y0 + ladaH, ladaD - .15f), W(ladaS0 + .62f, y0 + ladaH + .22f, ladaD - .45f)); st.kolor = BraxUliceKit.Bialy; }
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
                // 11.11: wózki poza przejściem od drzwi (dotąd pierwszy wózek 1.1 m od osi drzwi — w wejściu)
                bool wPrawo = f.drzwiS + 1.45f + 1.6f * k < sIn1 - .3f;
                BraxSklepyTowar.Wozki(tw, kolizje, W(f.drzwiS + (wPrawo ? 1.4f : -1.4f), y0, -.6f), wPrawo ? f.R : -f.R, 2);
            }
        }
        // lampy sufitowe (panele)
        for (float s = sIn0 + 1.4f; s < sIn1 - .8f; s += 2.6f)
            foreach (float d in new[] { -1.8f, dTyl + 1.9f }) BraxUliceKit.S(lampa).Box(W(s - .5f, ySuf - .05f, d + .3f), W(s + .5f, ySuf, d - .3f), 8);
        // lokale gastronomiczne: stoliki i krzesła (osobne obiekty — siedziska BraxSeat), bar: hokery przy ladzie
        if (lok.typ == "kawiarnia" || lok.typ == "bistro" || lok.typ == "cukiernia" || lok.typ == "pizzeria" || lok.typ == "piekarnia")
        {
            int ile = 0;
            // 11.11 (właściciel 14:14: „odblokowujesz każde wejście, bo jakieś siedziska stoją w wejściu”): od drzwi w głąb lokalu
            // wolne przejście 2.3 m (oś drzwi ±1.15 m) — krzesło stoi 0.85 m od środka stolika i ma 0.3 m pół szerokości, więc
            // stolik co najmniej 2.3 m od osi drzwi (dotąd 1.6 / 1.8 m: krzesło KAWIARNI na rogu i PIEKARNI 0.9–1.0 m od osi drzwi)
            for (float s = sIn0 + 1.1f; s < f.drzwiS - WolneOdDrzwi && ile < 3; s += 1.9f, ile++) Stolik(W(s, y0, -1.5f), f, bg, kolizje);
            for (float s = f.drzwiS + WolneOdDrzwi; s < sIn1 - 1.0f && ile < 4; s += 1.9f, ile++) if (s < ladaS0 - .9f || s > ladaS1 + .9f) Stolik(W(s, y0, -1.5f), f, bg, kolizje);
            // drugi rząd stolików w głębi sali (bistro, pizzeria, kawiarnia) — między pierwszym rzędem a ladą, poza przejściem od drzwi
            if (lok.typ != "cukiernia" && lok.typ != "piekarnia" && ladaD + .35f < -3.6f)
                for (float s = sIn0 + 1.1f; s < sIn1 - 1.0f; s += 1.9f) if (Mathf.Abs(s - f.drzwiS) > WolneOdDrzwi) Stolik(W(s, y0, -3.35f), f, bg, kolizje);
        }
        if (lok.typ == "bar")
            for (float s = ladaS0 + .35f; s < ladaS1 - .2f; s += .7f) Hoker(W(s, y0, ladaD + .45f), bg);
        // 10.11 (właściciel 10:12–10:13: „widziałeś sklep z kwiatami, że są tylko kwiaty na półkach?”, „przeglądasz każde jedno,
        // poprawiasz”): dotąd każdy sklep = półki pod ścianami i pusta posadzka. Środek sali wg branży — jak w prawdziwych sklepach:
        // delikatesy regały-wyspy, warzywniak stoły ze skrzynkami, piekarnia regał z koszami chleba, cukiernia witryna chłodnicza
        // z ciastami, kwiaciarnia schodkowe stojaki z wiadrami ciętych kwiatów (+ wiadra pod ścianą), księgarnia stoły z książkami,
        // optyk gabloty z oprawkami, poczta pulpit do wypełniania druków i paczki.
        if (prawdziwy && !gastro && lok.typ != "apteka" && lok.typ != "hotel" && lok.typ != "kantor" && lok.typ != "warsztat")
        {
            bool regalLewy = f.drzwiS > 3.2f;                         // regał boczny przy s0 (jak wyżej)
            float sMin = regalLewy ? sIn0 + .42f + .95f : sIn0 + .7f, sMax = regalLewy ? sIn1 - .7f : sIn1 - .42f - .95f;
            float dA = -1.55f, dB = Mathf.Max(ladaD + .75f, dA - 3.2f), dC = (dA + dB) * .5f;
            var odcinki = new List<(float, float)> { (sMin, f.drzwiS - 1.15f), (f.drzwiS + 1.15f, sMax) };
            var zyw = BraxUliceKit.S(towar); var cynk = BraxUliceKit.S(BraxUliceKit.Mat("kwiaciarnia: wiadra ocynkowane", new Color(.62f, .64f, .66f), .45f, .7f));
            var szyb = BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste); var nogi = BraxUliceKit.S(BraxUliceKit.Mat("meble: stal szczotkowana (nogi, ramy)", new Color(.55f, .56f, .58f), .35f, .85f));
            var mebel = okleina ? BraxUliceKit.S(okleina) : sk;
            int wysp = 0;
            void KolW(Vector3 a, Vector3 c) { var g = new GameObject("kolizja: wyspa " + lok.szyld); g.transform.SetParent(kolizje, false); var bx = g.AddComponent<BoxCollider>(); var lo2 = Vector3.Min(a, c); var hi2 = Vector3.Max(a, c); bx.center = (lo2 + hi2) * .5f; bx.size = hi2 - lo2; }
            void Bukiet(Vector3 p, Color32 kwiat, int n)
            {   // pęk ciętych kwiatów w wiadrze: łodygi rozchylone, kwiaty (kielichy) na różnych wysokościach
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2.39996f, r = .03f + .05f * (float)rng.NextDouble(), hh = .42f + .22f * (float)rng.NextDouble();
                    var top = p + new Vector3(Mathf.Cos(a) * r * 2.2f, hh, Mathf.Sin(a) * r * 2.2f);
                    zyw.kolor = Kol(.22f, .45f, .18f); zyw.Rura(p + new Vector3(Mathf.Cos(a) * r * .5f, 0, Mathf.Sin(a) * r * .5f), top, .005f, 4);
                    zyw.kolor = Kol(.25f, .5f, .2f); zyw.Box(top + new Vector3(-.03f, -.12f, -.006f), top + new Vector3(.03f, -.08f, .006f));   // liść
                    zyw.kolor = kwiat; zyw.Walec(top - Vector3.up * .02f, .032f + .012f * (float)rng.NextDouble(), .05f, 8);
                    zyw.kolor = Kol(kwiat.r / 255f * .7f, kwiat.g / 255f * .6f, kwiat.b / 255f * .6f); zyw.Walec(top + Vector3.up * .028f, .016f, .012f, 6);
                }
                zyw.kolor = BraxUliceKit.Bialy;
            }
            Color32[] kolKwiatow = { Kol(.86f, .12f, .22f), Kol(.98f, .82f, .18f), Kol(.95f, .55f, .72f), Kol(.98f, .97f, .94f), Kol(.92f, .45f, .15f), Kol(.55f, .25f, .7f), Kol(.98f, .7f, .78f) };
            foreach (var (o0, o1) in odcinki)
            {
                float L = Mathf.Min(2.6f, o1 - o0); if (L < 1.4f) continue;
                float s0w = (o0 + o1) * .5f - L * .5f, s1w = s0w + L;
                switch (lok.typ)
                {
                    case "market": case "piekarnia":
                    {   // regał-wyspa dwustronny (delikatesy 1.45 m; piekarnia 1.25 m — kosze z pieczywem na skośnych półkach)
                        bool pk = lok.typ == "piekarnia"; float gw = .62f, gh = (pk ? 1.25f : 1.45f) * k; int ile = pk ? 3 : 4;
                        mebel.Box(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + .1f, dC - gw * .5f));
                        mebel.Box(W(s0w, y0 + .1f, dC + .02f), W(s1w, y0 + gh, dC - .02f));
                        mebel.Box(W(s0w, y0 + .1f, dC + gw * .5f), W(s0w + .03f, y0 + gh, dC - gw * .5f)); mebel.Box(W(s1w - .03f, y0 + .1f, dC + gw * .5f), W(s1w, y0 + gh, dC - gw * .5f));
                        for (int p = 0; p < ile; p++)
                        {
                            float y = y0 + .3f + p * (gh - .42f) / (ile - 1);
                            foreach (float sg in new[] { -1f, 1f })
                            {
                                mebel.Box(W(s0w + .03f, y - .025f, dC + sg * .02f), W(s1w - .03f, y, dC + sg * gw * .5f));
                                if (p == ile - 1 && !pk) continue;
                                string typ = pk ? "piekarnia" : new[] { "market", "sm_g3", "sm_g4" }[p % 3];
                                BraxSklepyTowar.Polka(typ, tw, W(s0w + .07f, y + .004f, dC + sg * gw * .25f), f.R, -sg * f.N, L - .14f, rng, (gh - .42f) / (ile - 1) - .05f);
                            }
                        }
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + gh, dC - gw * .5f)); wysp++; break;
                    }
                    case "warzywniak":
                    {   // stół targowy ze skrzynkami na dwóch poziomach (niski z przodu, wyższy w środku)
                        float gw = 1.1f;
                        foreach (var (d0, d1, h) in new[] { (dC + gw * .5f, dC, .55f * k), (dC, dC - gw * .5f, .55f * k), (dC + .25f, dC - .25f, .82f * k) })
                        {
                            mebel.Box(W(s0w, y0 + h - .04f, d0), W(s1w, y0 + h, d1));
                            foreach (float s in new[] { s0w + .05f, s1w - .09f }) foreach (float d in new[] { d0 - .05f, d1 + .05f }) nogi.Box(W(s, y0, d), W(s + .04f, y0 + h - .04f, d - Mathf.Sign(d0 - d1) * .04f));
                        }
                        foreach (float sg in new[] { -1f, 1f })
                            BraxSklepyTowar.Polka("warzywniak", tw, W(s0w + .06f, y0 + .55f * k + .004f, dC + sg * gw * .3f), f.R, -sg * f.N, L - .12f, rng);
                        BraxSklepyTowar.Polka("warzywniak", tw, W(s0w + .06f, y0 + .82f * k + .004f, dC), f.R, f.N, L - .12f, rng);
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + .9f * k, dC - gw * .5f)); wysp++; break;
                    }
                    case "cukiernia": case "optyk":
                    {   // witryna: korpus z okleiny, blat z granitu, nad nim szkło (cukiernia — chłodnicza, wyższa), towar w środku
                        bool ck = lok.typ == "cukiernia"; float gw = ck ? .85f : .7f, hb = .88f * k, hs = ck ? .42f : .28f;
                        mebel.Box(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + hb - .03f, dC - gw * .5f));
                        BraxUliceKit.S(granit).Box(W(s0w - .02f, y0 + hb - .03f, dC + gw * .5f + .02f), W(s1w + .02f, y0 + hb, dC - gw * .5f - .02f));
                        if (ck) BraxSklepyTowar.Polka(lok.typ, tw, W(s0w + .08f, y0 + hb + .004f, dC), f.R, f.N, L - .16f, rng, hs - .04f);
                        // optyk: oprawki z kodu w dwóch rzędach plecami do siebie, przodem do klientów z obu stron; rzędy przy szybach
                        // (± 0.32 głębokości), drugi rząd przesunięty o pół kroku — zauszniki (0.19 m) nie sięgają drugiego rzędu
                        else foreach (float sg in new[] { -1f, 1f })
                                for (float a = s0w + (sg < 0f ? .15f : .29f) * k; a < s1w - .15f * k; a += .28f * k)
                                { LosujOkulary(rng, true, out int mo, out var ko, out int pz); Okulary(W(a, y0 + hb + .0005f, dC + sg * gw * .32f), sg * f.N, ko, k, mo, pz); }   // gablota: przeciwsłoneczne
                        szyb.Box(W(s0w + .01f, y0 + hb, dC + gw * .5f - .01f), W(s1w - .01f, y0 + hb + hs, dC - gw * .5f + .01f));
                        nogi.Box(W(s0w, y0 + hb + hs, dC + gw * .5f), W(s1w, y0 + hb + hs + .025f, dC - gw * .5f));
                        if (ck) BraxUliceKit.S(BraxMiastoA.Swieci("witryna chłodnicza: listwa LED", new Color(1f, .97f, .9f), new Color(1.3f, 1.25f, 1.1f))).Box(W(s0w + .05f, y0 + hb + hs - .03f, dC + .05f), W(s1w - .05f, y0 + hb + hs - .005f, dC - .05f));
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + hb + hs + .03f, dC - gw * .5f)); wysp++; break;
                    }
                    case "ksiegarnia":
                    {   // stół z nowościami: blat z okleiny, książki stojące w dwóch rzędach i dolna półka
                        float gw = .9f, hb = .78f * k;
                        mebel.Box(W(s0w, y0 + hb - .04f, dC + gw * .5f), W(s1w, y0 + hb, dC - gw * .5f));
                        mebel.Box(W(s0w + .08f, y0 + .18f, dC + gw * .5f - .06f), W(s1w - .08f, y0 + .21f, dC - gw * .5f + .06f));
                        foreach (float s in new[] { s0w + .04f, s1w - .1f }) foreach (float d in new[] { dC + gw * .5f - .04f, dC - gw * .5f + .1f }) mebel.Box(W(s, y0, d), W(s + .06f, y0 + hb - .04f, d - .06f));
                        foreach (float sg in new[] { -1f, 1f }) BraxSklepyTowar.Polka("ksiegarnia", tw, W(s0w + .08f, y0 + hb + .004f, dC + sg * gw * .22f), f.R, -sg * f.N, L - .16f, rng, .34f);
                        BraxSklepyTowar.Polka("ksiegarnia", tw, W(s0w + .14f, y0 + .214f, dC), f.R, f.N, L - .28f, rng, hb - .3f);
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + hb, dC - gw * .5f)); wysp++; break;
                    }
                    case "kwiaty":
                    {   // schodkowy stojak (3 stopnie) z wiadrami ciętych kwiatów — po obu stronach, przodem do klientów
                        float gw = 1.2f;
                        for (int st3 = 0; st3 < 3; st3++)
                        {
                            float h = (.12f + st3 * .3f) * k, d0 = dC + gw * .5f - st3 * gw / 3f, d1 = d0 - gw / 3f;
                            mebel.Box(W(s0w, y0, d0), W(s1w, y0 + h, d1));
                            int nW = Mathf.FloorToInt((L - .1f) / .36f);
                            for (int i = 0; i < nW; i++)
                            {
                                var c = W(s0w + .23f + i * (L - .46f) / Mathf.Max(1, nW - 1), y0 + h, (d0 + d1) * .5f);
                                cynk.Walec(c, .14f, .3f, 12); zyw.kolor = Kol(.3f, .35f, .4f); zyw.Walec(c + Vector3.up * .26f, .125f, .01f, 12); zyw.kolor = BraxUliceKit.Bialy;
                                Bukiet(c + Vector3.up * .27f, kolKwiatow[rng.Next(kolKwiatow.Length)], 9);
                            }
                        }
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + .75f * k, dC - gw * .5f)); wysp++; break;
                    }
                    case "poczta":
                    {   // pulpit do wypełniania druków (stojący) i paczki przy nim
                        float gw = .55f, hb = 1.05f * k;
                        mebel.Box(W(s0w + .3f, y0, dC + .08f), W(s1w - .3f, y0 + hb - .04f, dC - .08f));
                        BraxUliceKit.S(granit).Box(W(s0w, y0 + hb - .04f, dC + gw * .5f), W(s1w, y0 + hb, dC - gw * .5f));
                        BraxSklepyTowar.Polka("poczta", tw, W(s0w + .1f, y0 + hb + .004f, dC), f.R, f.N, L - .2f, rng, .25f);
                        KolW(W(s0w, y0, dC + gw * .5f), W(s1w, y0 + hb, dC - gw * .5f)); wysp++; break;
                    }
                }
            }
            if (lok.typ == "kwiaty")
            {   // wiadra z ciętymi kwiatami wzdłuż wolnej ściany bocznej (bez regału), od witryny do linii lady — kwiaty cięte stoją
                // na posadzce, na półkach ozdoby, doniczki i wazony (jak w prawdziwej kwiaciarni)
                float sBok = regalLewy ? sIn1 - .26f : sIn0 + .26f;
                for (float d = -1.5f; d > ladaD + .2f; d -= .38f)
                {
                    var c = W(sBok, y0, d);
                    cynk.Walec(c, .15f, .34f, 12); zyw.kolor = Kol(.3f, .35f, .4f); zyw.Walec(c + Vector3.up * .3f, .135f, .01f, 12); zyw.kolor = BraxUliceKit.Bialy;
                    Bukiet(c + Vector3.up * .31f, kolKwiatow[rng.Next(kolKwiatow.Length)], 10);
                }
                KolW(W(sBok - .18f, y0, -1.3f), W(sBok + .18f, y0 + .7f, ladaD + .1f));
            }
            BraxRegion.Note("pierzeje: środek sali " + lok.szyld + " — wysp " + wysp);
        }
        Wyposazenie(f, bg, kolizje, tw, gl, sIn0, sIn1, dTyl, ladaS0, ladaS1, ladaD, ladaG, ladaH, y0, ySuf, hR, rng);
        if (prawdziwy) BraxSklepyTowar.Zakoncz(tw, lok.szyld);
        BraxUliceKit.Punkt("ulica_" + b.id + "_lada", "za ladą: " + lok.szyld + " — miejsce zleceniodawcy (sprzedawca / właściciel lokalu)", W((ladaS0 + ladaS1) * .5f, y0, ladaD - ladaG - .55f));
    }

    // ---------------- wykończenie lokalu (10.11): posadzka, ściany, sufit wg branży ----------------
    // Wcześniej każdy lokal miał szary kamień holu i grafitowe ściany. Teraz fotoskany Poly Haven (pobierane w CI — TEXTURES w
    // fetch_region.py) jak w prawdziwych sklepach: apteka biała, delikatesy i warzywniak płytki, kawiarnia i bistro deski i cegła,
    // pizzeria cegła, hotel marmur i okleina, kwiaciarnia beton i bielona cegła, warsztat beton i cegła. Lica 4–5 mm przed murem holu,
    // posadzka 4 mm nad posadzką holu (zasada anty-migotania), listwa przypodłogowa jako bryła.
    static void Wykonczenie(Fasada f, float gl)
    {
        // ściany boczne 0.33 m od lica budynku: przykrywają ościeża okien parteru z elewacji bocznych (wnęka 0.3 m — z wnętrza czarne ramki)
        string t = f.lok.typ; float W = f.W, y0 = .11f, dT = -(gl - .12f) + .004f, s0 = OdsuniecieScian, s1 = W - OdsuniecieScian, ySuf = SufitLokalu + .016f;
        string[] pod = { "floor_tiles_06", "square_tiles" }, sc = { "white_stucco", "white_plaster_02" }; float mp = 1.2f, ms = 3.0f; Color? kp = null, ks = null;
        switch (t)
        {
            case "apteka": pod = new[] { "marble_01", "floor_tiles_06" }; mp = 1.6f; sc = new[] { "white_stucco", "white_plaster_02" }; ms = 3.0f; ks = new Color(1f, 1f, 1f); break;
            case "market": case "warzywniak": pod = new[] { "granite_tile_03", "granite_tile" }; mp = 1.4f; sc = new[] { "white_stucco", "white_plaster_02" }; ks = new Color(.98f, .95f, .88f); break;
            case "piekarnia": case "cukiernia": pod = new[] { "marble_01", "floor_tiles_06" }; mp = 1.6f; sc = new[] { "white_stucco", "white_plaster_02" }; ks = new Color(1f, .93f, .84f); break;
            case "kawiarnia": case "bistro": case "bar": pod = new[] { "wood_floor", "plank_flooring_02" }; mp = 2.0f; sc = new[] { "red_brick_03", "brick_wall_001" }; ms = 2.2f; break;
            case "pizzeria": pod = new[] { "wood_floor", "plank_flooring_02" }; mp = 2.0f; sc = new[] { "red_brick_03", "brick_wall_001" }; ms = 2.2f; break;
            case "ksiegarnia": case "optyk": pod = new[] { "wood_floor", "plank_flooring_02" }; mp = 2.0f; ks = new Color(.95f, .93f, .88f); break;
            case "fryzjer": pod = new[] { "marble_01", "floor_tiles_06" }; mp = 1.6f; sc = new[] { "white_stucco", "white_plaster_02" }; ks = new Color(.97f, .96f, .94f); break;
            // 10.11: concrete_floor_02 (brudny, chropowaty jak ziemia) → szlifowany beton concrete_floor_worn_001
            case "kwiaty": pod = new[] { "concrete_floor_worn_001", "concrete_floor_02" }; mp = 2.5f; sc = new[] { "brick_wall_001", "red_brick_03" }; ms = 2.6f; ks = new Color(.93f, .93f, .9f); break;
            case "warsztat": pod = new[] { "concrete_floor_worn_001", "concrete_floor_02" }; mp = 2.5f; kp = new Color(.92f, .92f, .92f); sc = new[] { "red_brick_03", "brick_wall_001" }; ms = 2.2f; break;
            case "hotel": pod = new[] { "marble_01", "granite_tile_03" }; mp = 1.6f; sc = new[] { "teak_veneer", "wood_plank_wall" }; ms = 2.0f; break;
            case "kantor": case "poczta": pod = new[] { "granite_tile_03", "granite_tile" }; mp = 1.4f; ks = new Color(.93f, .92f, .86f); break;
        }
        var mPod = BraxUliceKit.Tex("posadzka lokalu (" + t + ")", mp, kp, new Color(.72f, .7f, .67f), .45f, pod);
        var mSc = BraxUliceKit.Tex("ściany lokalu (" + t + ")", ms, ks, new Color(.9f, .88f, .84f), .9f, sc);
        var mSuf = BraxUliceKit.Tex("sufit lokalu (gładź biała)", 3.0f, new Color(1f, 1f, .99f), new Color(.94f, .94f, .93f), .95f, "white_stucco", "white_plaster_02");
        var mLis = BraxUliceKit.Mat("listwa przypodłogowa lokalu", t == "hotel" || t == "kawiarnia" || t == "bistro" ? new Color(.22f, .14f, .09f) : new Color(.36f, .35f, .34f), .6f);
        Vector3 P(float s, float y, float d) => f.P(s, y, d);
        BraxUliceKit.S(mPod).Quad(P(s0, y0 + .004f, -.04f), P(s1, y0 + .004f, -.04f), P(s1, y0 + .004f, dT), P(s0, y0 + .004f, dT), Vector3.up);
        BraxUliceKit.S(mSuf).Quad(P(s0, ySuf, -.04f), P(s1, ySuf, -.04f), P(s1, ySuf, dT), P(s0, ySuf, dT), Vector3.down);
        var ss = BraxUliceKit.S(mSc);
        ss.Quad(P(s0, y0, -.04f), P(s0, y0, dT), P(s0, ySuf, dT), P(s0, ySuf, -.04f), f.R);
        ss.Quad(P(s1, y0, -.04f), P(s1, y0, dT), P(s1, ySuf, dT), P(s1, ySuf, -.04f), -f.R);
        ss.Quad(P(s0, y0, dT), P(s1, y0, dT), P(s1, ySuf, dT), P(s0, ySuf, dT), f.N);
        var sl = BraxUliceKit.S(mLis);
        sl.Box(P(s0, y0 + .004f, -.06f), P(s0 + .014f, y0 + .085f, dT + .014f)); sl.Box(P(s1 - .014f, y0 + .004f, -.06f), P(s1, y0 + .085f, dT + .014f));
        sl.Box(P(s0 + .014f, y0 + .004f, dT), P(s1 - .014f, y0 + .085f, dT + .014f));
        // lamperia z okleiny (do 1.05 m × K) z listwą u góry — sklepy, księgarnia, optyk, kantor, poczta (miasto ekskluzywne)
        if (t == "market" || t == "warzywniak" || t == "piekarnia" || t == "cukiernia" || t == "ksiegarnia" || t == "optyk" || t == "kantor" || t == "poczta")
        {
            float k = BraxUliceKit.K, yL = y0 + 1.05f * k, o = .006f;
            var lam = BraxUliceKit.S(BraxUliceKit.Tex(t == "kantor" ? "lamperia: orzech" : "lamperia: dąb", 1.6f, t == "kantor" ? new Color(.62f, .5f, .4f) : (Color?)null, new Color(.55f, .4f, .27f), .5f, "teak_veneer", "dark_wood"));
            lam.Quad(P(s0 + o, y0 + .085f, -.06f), P(s0 + o, y0 + .085f, dT + .02f), P(s0 + o, yL, dT + .02f), P(s0 + o, yL, -.06f), f.R);
            lam.Quad(P(s1 - o, y0 + .085f, -.06f), P(s1 - o, y0 + .085f, dT + .02f), P(s1 - o, yL, dT + .02f), P(s1 - o, yL, -.06f), -f.R);
            lam.Quad(P(s0 + .02f, y0 + .085f, dT + o), P(s1 - .02f, y0 + .085f, dT + o), P(s1 - .02f, yL, dT + o), P(s0 + .02f, yL, dT + o), f.N);
            sl.Box(P(s0, yL, -.06f), P(s0 + .022f, yL + .035f, dT)); sl.Box(P(s1 - .022f, yL, -.06f), P(s1, yL + .035f, dT)); sl.Box(P(s0, yL, dT), P(s1, yL + .035f, dT + .022f));
        }
    }

    // ---------------- wyposażenie wg branży (10.11) ----------------
    static GameObject PH(Transform rodzic, string id, string nazwa, Vector3 p, float yaw, float maxWys = 0f)
    {
        var src = BraxRegion.Model(id); if (!src) return null;
        return BraxRegion.Put(rodzic, src, nazwa, p, yaw, BraxUliceKit.K, false, maxWys);
    }
    static void Wyposazenie(Fasada f, Transform bg, Transform kolizje, Transform tw, float gl, float sIn0, float sIn1, float dTyl, float ladaS0, float ladaS1, float ladaD, float ladaG, float ladaH, float y0, float ySuf, float hR, System.Random rng)
    {
        string t = f.lok.typ; float k = BraxUliceKit.K; var rodzic = tw ? tw : bg;
        Vector3 W(float s, float y, float d) => f.P(s, y, d);
        float yawDoUlicy = Quaternion.LookRotation(f.N, Vector3.up).eulerAngles.y, yawWGlab = yawDoUlicy + 180f;
        bool drzwiPrawo = f.drzwiS > (sIn0 + sIn1) * .5f;          // po której stronie są drzwi — wolna strona na meble
        float sWolna0 = drzwiPrawo ? sIn0 + .5f : f.drzwiS + 1.4f, sWolna1 = drzwiPrawo ? f.drzwiS - 1.4f : sIn1 - .5f;
        // TELEWIZOR z filmami właściciela — TYLKO hotel (właściciel 21:48: „na ulicach żadnych filmów… zakazane globalnie… W mieszkaniach,
        // w hotelach, w apartamentach… Tam, gdzie są telewizory”): w holu na ścianie bocznej bez drzwi, środek 2.1 m × K, 55"
        if (t == "hotel")
            BraxTelewizory.NaScianie(rodzic, f.lok.szyld, W(drzwiPrawo ? sIn0 : sIn1, y0 + 2.1f * k, dTyl * .5f), drzwiPrawo ? f.R : -f.R, 1.0f * k);
        if (t == "fryzjer")
        {   // SALON FRYZJERSKI (11.11, właściciel 19:35: „FRYZJER… w środku regał z książkami” — wnętrze z innego lokalu): stanowiska
            // wzdłuż ściany tylnej — lustro z prawdziwym odbiciem na całej długości w czarnej ramie, blat dębowy z szufladami pod lustrem,
            // fotele fryzjerskie z kodu przodem do lustra (skóra, chrom, podnóżek), myjka do głów (misa toczona) przy ścianie bocznej,
            // poczekalnia: krzesła tapicerowane przy witrynie, rośliny. Lada recepcji z kasą jak w innych lokalach (przy drzwiach).
            float fs0 = sIn0 + .5f, fs1 = sIn1 - .5f, ym = y0 + .85f * k, dB = dTyl + .02f;
            var dabF = BraxUliceKit.Tex("fryzjer: blat i szafki (dąb)", 1.6f, null, new Color(.6f, .45f, .3f), .45f, "oak_veneer_01", "teak_veneer");
            var czarnyF = BraxUliceKit.Mat("fryzjer: rama lustra (czarny mat)", new Color(.05f, .05f, .055f), .5f, .3f);
            BraxUliceKit.S(dabF).Poducha(Vector3.Min(W(fs0, ym - .35f, dB), W(fs1, ym, dB - .42f)), Vector3.Max(W(fs0, ym - .35f, dB), W(fs1, ym, dB - .42f)), .01f, 0f, 3);
            BraxCeramika.Lustro(rodzic, "fryzjer " + f.lok.szyld, W((fs0 + fs1) * .5f, ym + .2f + .55f * k, dB - .03f), f.N, fs1 - fs0 - .08f, 1.1f * k);
            foreach (var (a0, a1, b0, b1) in new[] { (fs0, fs0 + .04f, ym + .16f, ym + .24f + 1.1f * k), (fs1 - .04f, fs1, ym + .16f, ym + .24f + 1.1f * k), (fs0 + .04f, fs1 - .04f, ym + .16f, ym + .2f), (fs0 + .04f, fs1 - .04f, ym + .2f + 1.1f * k, ym + .24f + 1.1f * k) })
                BraxUliceKit.S(czarnyF).Poducha(Vector3.Min(W(a0, b0, dB), W(a1, b1, dB - .05f)), Vector3.Max(W(a0, b0, dB), W(a1, b1, dB - .05f)), .006f, 0f, 3);
            int nSt = Mathf.Clamp((int)((fs1 - fs0) / (1.35f * k)), 1, 4); float krok = (fs1 - fs0) / nSt;
            for (int q = 0; q < nSt; q++) BraxCeramika.FotelFryzjerski(W(fs0 + krok * (q + .5f), y0, dB - 1.25f * k), -f.N, kolizje);
            // myjka do głów przy ścianie bocznej bez drzwi: szafka, misa toczona, bateria
            float sm = drzwiPrawo ? sIn0 + .45f : sIn1 - .45f; var bok = drzwiPrawo ? f.R : -f.R;
            Vector3 mc = W(sm, y0, dB - 2.2f * k) + bok * 0f;
            BraxUliceKit.S(dabF).Poducha(Vector3.Min(mc - bok * .3f - f.N * .35f, mc + bok * .3f + f.N * .35f + Vector3.up * .8f * k), Vector3.Max(mc - bok * .3f - f.N * .35f, mc + bok * .3f + f.N * .35f + Vector3.up * .8f * k), .01f, 0f, 3);
            BraxCeramika.Umywalka(mc + Vector3.up * .8f * k);
            BraxMiastoA.Kol(kolizje, "myjka fryzjera", mc - bok * .3f - f.N * .35f, mc + bok * .3f + f.N * .35f + Vector3.up * .95f * k);
            // poczekalnia przy witrynie (strona wolna)
            for (int q = 0; q < 2; q++) KrzesloTapicerowane("Fryzjer: krzesło poczekalni", bg, W(sWolna0 + .45f + q * .75f, y0, -1.0f), yawWGlab);
            PH(rodzic, "doniczka_kalatea_3", "Fryzjer: roślina", W(sWolna1 - .3f, y0 + .004f, -.7f), 40f);
        }
        if (t == "hotel")
        {   // RECEPCJA HOTELU: tablica z kluczami za ladą, napis RECEPCJA, hol z sofą, fotelami, stolikiem, zegarem stojącym, roślinami, walizki
            var drewno = BraxUliceKit.Mat("hotel: tablica kluczy (orzech)", new Color(.25f, .15f, .09f), .45f);
            var mosiadz = BraxUliceKit.Mat("hotel: mosiądz (haczyki, klucze)", new Color(.78f, .6f, .3f), .3f, .9f);
            float kc = (ladaS0 + ladaS1) * .5f, kd = dTyl + .02f, ky0 = 1.35f * k, ky1 = 2.2f * k;
            BraxUliceKit.S(drewno).Box(W(kc - .8f, y0 + ky0, kd + .04f), W(kc + .8f, y0 + ky1, kd));
            var ms = BraxUliceKit.S(mosiadz);
            for (int r = 0; r < 4; r++) for (int c = 0; c < 8; c++)
            {
                float hs = kc - .7f + c * .2f, hy = y0 + ky0 + .15f + r * (ky1 - ky0 - .25f) / 3f;
                ms.Rura(W(hs, hy, kd + .04f), W(hs, hy, kd + .075f), .006f, 6);
                if ((r * 8 + c) % 3 != 0) { ms.Box(W(hs - .012f, hy - .1f, kd + .05f), W(hs + .012f, hy - .02f, kd + .06f)); ms.Box(W(hs - .03f, hy - .16f, kd + .045f), W(hs + .03f, hy - .1f, kd + .065f)); }
            }
            BraxUliceKit.Napis3D("RECEPCJA", W(kc, y0 + ky1 + .15f, kd + .02f), f.N, .17f * k, .03f, mosiadz, mosiadz);
            // hol: sofa pod ścianą boczną (wolna strona), dwa fotele, stolik, rośliny, zegar stojący, walizki przy ladzie
            float sB = drzwiPrawo ? sIn0 + .55f : sIn1 - .55f, kier = drzwiPrawo ? 1f : -1f, yawSofa = Quaternion.LookRotation(f.R * kier, Vector3.up).eulerAngles.y;
            PH(bg, "sofa_02", "Hotel: sofa w holu", W(sB, y0, dTyl + 1.0f + (gl - 2.4f) * .5f), yawSofa);
            PH(bg, "coffee_table_round_01", "Hotel: stolik", W(sB + kier * 1.25f, y0, dTyl + 1.0f + (gl - 2.4f) * .5f), 0f, .55f * k);
            foreach (float o in new[] { -.95f, .95f }) PH(bg, "ArmChair_01", "Hotel: fotel", W(sB + kier * 2.1f, y0, dTyl + 1.0f + (gl - 2.4f) * .5f + o), Quaternion.LookRotation(-f.R * kier, Vector3.up).eulerAngles.y + (o > 0 ? 25f : -25f));
            PH(bg, "vintage_grandfather_clock_01", "Hotel: zegar stojący", W(drzwiPrawo ? sIn0 + .5f : sIn1 - .5f, y0, dTyl + .35f), yawDoUlicy);
            foreach (var (ps, pd) in new[] { (drzwiPrawo ? sIn0 + .45f : sIn1 - .45f, -.7f), (ladaS1 + .55f, dTyl + .45f) })
                PH(bg, "doniczka_kalatea_3", "Hotel: roślina w donicy", W(ps, y0, pd), (float)rng.NextDouble() * 360f);
            PH(bg, "vintage_suitcase", "Hotel: walizki gościa", W(ladaS0 - .2f, y0, ladaD + .55f), yawDoUlicy + 80f, .6f * k);
        }
        if (t == "bar")
        {   // BAR 24 (ETAP-GALERIA p. 4): dwie loże (kanapy ze skóry z wysokim oparciem, stolik), tablica zakładów na ustawki na ścianie.
            // 10.11 (właściciel 10:06–10:13: „jeden wielki kloc”, „obija pikowaną skórą, drewienko”): loże jak w klubie NOVA — oparcia
            // ze skóry pikowanej (tekstura rombów z guzikami), siedziska z gładkiej skóry na cokole z orzecha, boki loży od przejścia
            // i listwa na oparciu z orzecha, stolik z blatem z orzecha i mosiężnym obrzeżem. Tablica zakładów nad lożami (ściana boczna)
            // — wcześniej wisiała za regałem z butelkami (widać było tylko górę, wiersze nachodziły na siebie).
            var bordo = new Color(.50f, .11f, .12f);
            var pik = BraxUliceKit.S(BraxUliceKit.TexPlik("bar: oparcia lóż — skóra pikowana bordo", "Assets/Source/Claude/Nova/tekstury/", "nova_skora_pikowana", .64f, bordo, bordo * .5f, .42f));
            var gladka = BraxUliceKit.S(BraxMiastoA.Tex("bar: siedziska lóż — skóra bordo", .7f, bordo, bordo * .5f, .45f, "leather_red_02", "brown_leather"));
            var orzech = BraxUliceKit.S(BraxMiastoA.Tex("bar: orzech ciemny (fornir)", 1.4f, new Color(.52f, .36f, .27f), new Color(.20f, .12f, .07f), .42f, "teak_veneer", "dark_wood"));
            var mosB = BraxUliceKit.S(BraxMiastoA.Mat("bar: mosiądz szczotkowany", new Color(.72f, .56f, .30f), .3f, .9f));
            float gL = 1.5f * k, sL = drzwiPrawo ? sIn0 + .05f : sIn1 - .05f - gL;   // loże prostopadle do ściany bocznej po stronie bez drzwi
            float sP0 = drzwiPrawo ? sL + gL - .045f : sL, sP1 = sP0 + .045f;                 // skrzydło oparcia od przejścia (orzech)
            float sC0 = drzwiPrawo ? sL : sP1 + .004f, sC1 = drzwiPrawo ? sP0 - .004f : sL + gL; // oparcie między ścianą a skrzydłem
            float sS0 = drzwiPrawo ? sL : sL + .01f, sS1 = drzwiPrawo ? sL + gL - .01f : sL + gL; // poduchy siedziska aż do przejścia (widać miękkość)
            float hC = .16f * k, hS = .46f * k, hO = 1.12f * k, hB = 1.16f * k, dSrod = 0f;
            for (int i = 0; i < 2; i++)
            {
                float dA = -1.3f - i * 2.25f * k, sz = .52f * k, op = .14f * k, st = .74f * k, dB = dA - op - sz - st;   // dA: plecy kanapy A od ulicy
                foreach (var (dPl, sg) in new[] { (dA, -1f), (dB - sz - op, 1f) })   // plecy kanapy, kierunek ku stolikowi
                {
                    float dO = dPl + sg * op, dS = dO + sg * sz;                                   // lico oparcia, przód siedziska
                    orzech.Box(W(sS0 + .03f, y0, dPl), W(sS1 - .03f, y0 + hC, dS - sg * .04f));   // cokół pod oparciem i siedziskiem (cofnięty 3–4 cm)
                    float sM = (sS0 + sS1) * .5f;                                                     // dwie miękkie poduchy siedziska (10:29)
                    foreach (var (u0, u1) in new[] { (sS0, sM - .006f), (sM + .006f, sS1) })
                        gladka.Poducha(W(u0, y0 + hC, dO), W(u1, y0 + hS, dS), .06f * k, .02f);
                    pik.Poducha(W(sC0, y0 + hC, dPl), W(sC1, y0 + hO, dO), .05f * k, .022f);         // oparcie pikowane — miękka poducha
                    orzech.Box(W(sL, y0 + hO, dPl - sg * .01f), W(sL + gL, y0 + hB, dO + sg * .015f));   // listwa na oparciu
                    orzech.Box(W(sP0, y0 + hS, dPl), W(sP1, y0 + hO, dO));                          // skrzydło oparcia od przejścia
                }
                float dm = dA - op - sz - st * .5f; dSrod += dm * .5f;
                orzech.Box(W(sL + .06f, y0 + .725f * k, dm + st * .4f), W(sL + gL - .06f, y0 + .76f * k, dm - st * .4f));
                mosB.Box(W(sL + .055f, y0 + .715f * k, dm + st * .4f + .005f), W(sL + gL - .055f, y0 + .728f * k, dm - st * .4f - .005f));
                mosB.Rura(W(sL + gL * .5f, y0, dm), W(sL + gL * .5f, y0 + .715f * k, dm), .035f, 10); mosB.Walec(W(sL + gL * .5f, y0, dm), .22f, .015f, 18);
                var kl = new GameObject("kolizja: loża baru " + (i + 1)); kl.transform.SetParent(kolizje, false); var bcl = kl.AddComponent<BoxCollider>();
                var a = W(sL, y0, dA); var bb = W(sL + gL, y0 + hB, dB - sz - op); bcl.center = (a + bb) * .5f; bcl.size = new Vector3(Mathf.Abs(bb.x - a.x), hB, Mathf.Abs(bb.z - a.z));
            }
            // tablica zakładów: na ścianie bocznej nad lożami (czarna w mosiężnej ramie, kursy bursztynem), 2.2 × 0.95 m
            var tab = BraxUliceKit.Mat("bar: tablica zakładów (czarna)", new Color(.03f, .03f, .035f), .3f, .2f);
            var led = BraxMiastoA.Swieci("bar: kursy zakładów (bursztyn)", new Color(1f, .62f, .12f), new Color(2.2f, 1.2f, .2f));
            Vector3 nT = drzwiPrawo ? f.R : -f.R; float sT = drzwiPrawo ? sIn0 + .005f : sIn1 - .005f, ty0 = 1.55f * k, ty1 = 2.5f * k, tL = 1.1f;
            Vector3 T(float y, float d, float o) => W(sT, y, d) + nT * o;
            BraxUliceKit.S(tab).Box(T(y0 + ty0, dSrod - tL, 0f), T(y0 + ty1, dSrod + tL, .04f));
            foreach (var (pa, pb) in new[] { (T(y0 + ty0 - .03f, dSrod - tL - .03f, 0f), T(y0 + ty0, dSrod + tL + .03f, .05f)), (T(y0 + ty1, dSrod - tL - .03f, 0f), T(y0 + ty1 + .03f, dSrod + tL + .03f, .05f)),
                                             (T(y0 + ty0, dSrod - tL - .03f, 0f), T(y0 + ty1, dSrod - tL, .05f)), (T(y0 + ty0, dSrod + tL, 0f), T(y0 + ty1, dSrod + tL + .03f, .05f)) })
                mosB.Box(pa, pb);
            string[] zaklady = { "ZAKŁADY", "USTAWKA 3 NA 3   1.85", "WALKA WIECZOREM   2.40", "LIGA SOBOTA   1.60" };
            for (int i = 0; i < zaklady.Length; i++)
                BraxUliceKit.Napis3D(zaklady[i], T(y0 + ty1 - .2f * k - i * .21f * k, dSrod, .045f), nT, (i == 0 ? .11f : .075f) * k, .006f, led, led);
        }
        if (t == "bistro" || t == "pizzeria" || t == "bar")
        {   // ROŻEN Z KURCZAKAMI (właściciel 10.11 09:03) na stalowym stoliku obok lady: szafka z szybą od klienta, grzałka z tyłu świeci,
            // dwa rożna po 3 kurczaki z biblioteki właściciela
            float rs0 = ladaS1 + .15f, rs1 = rs0 + 1.1f;
            if (rs1 > sIn1 - .4f || Mathf.Abs((rs0 + rs1) * .5f - f.drzwiS) < 1.2f) { rs1 = ladaS0 - .15f; rs0 = rs1 - 1.1f; }
            if (rs0 > sIn0 + .4f && Mathf.Abs((rs0 + rs1) * .5f - f.drzwiS) > 1.2f)
            {
                var stal = BraxUliceKit.S(BraxUliceKit.Mat("gastronomia: stal nierdzewna", new Color(.78f, .79f, .8f), .25f, .9f));
                var szk = BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste);
                var zar = BraxUliceKit.S(BraxMiastoA.Swieci("rożen: grzałka (żar)", new Color(1f, .45f, .12f), new Color(2.4f, .9f, .2f)));
                float dP = ladaD - .02f, dT = ladaD - ladaG + .04f, yb = y0 + ladaH;
                stal.Box(W(rs0, y0, dP), W(rs1, yb, dT));                                        // stolik roboczy
                stal.Box(W(rs0, yb, dP), W(rs1, yb + .04f, dT)); stal.Box(W(rs0, yb + .7f, dP), W(rs1, yb + .74f, dT));
                foreach (float sp in new[] { rs0, rs1 - .04f }) stal.Box(W(sp, yb, dP), W(sp + .04f, yb + .74f, dT));
                szk.Quad(W(rs0 + .04f, yb + .04f, dP), W(rs1 - .04f, yb + .04f, dP), W(rs1 - .04f, yb + .7f, dP), W(rs0 + .04f, yb + .7f, dP), f.N);
                zar.Quad(W(rs0 + .06f, yb + .1f, dT + .01f), W(rs1 - .06f, yb + .1f, dT + .01f), W(rs1 - .06f, yb + .62f, dT + .01f), W(rs0 + .06f, yb + .62f, dT + .01f), f.N);
                var kur = BraxMiastoAssety.Jest("miasto_sklepy") ? BraxMiastoAssety.Model("miasto_sklepy", "kurczak") : null;
                float dm = (dP + dT) * .5f;
                foreach (float yr in new[] { yb + .22f, yb + .47f })
                {
                    stal.Rura(W(rs0 + .04f, yr, dm), W(rs1 - .04f, yr, dm), .006f, 6);
                    if (!kur) continue;
                    // 11.11 (właściciel 19:40: „kurczak z rożna ma być też, obkręcać się, rożno ma być”): kurczaki pod osobnym węzłem
                    // na osi pręta — obraca się (BraxObrot, oś pręta f.R), kurczaki nie statyczne (bez batchingu)
                    var rozen = new GameObject("Rożen kurczaków (obraca się)").transform; rozen.SetParent(rodzic, false); rozen.position = W((rs0 + rs1) * .5f, yr, dm);
                    for (int q = 0; q < 3; q++) BraxMiastoAssety.Postaw(rozen, kur, "Kurczak na rożnie", W(rs0 + .22f + q * .33f, yr - .07f, dm), Quaternion.LookRotation(f.R, Vector3.up).eulerAngles.y, 1f, false, false);
                    foreach (var tt in rozen.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tt.gameObject, (StaticEditorFlags)0);
                    var obr = rozen.gameObject.AddComponent<BraxObrot>(); obr.os = f.R; obr.osSwiata = true; obr.stopniNaSekunde = 40f;
                }
                var kl = new GameObject("kolizja: rożen z kurczakami"); kl.transform.SetParent(kolizje, false); var bcr = kl.AddComponent<BoxCollider>();
                var ra = W(rs0, y0, dP); var rb = W(rs1, yb + .74f, dT); bcr.center = (ra + rb) * .5f; bcr.size = new Vector3(Mathf.Abs(rb.x - ra.x), rb.y - ra.y, Mathf.Abs(rb.z - ra.z));
            }
        }
        if (t == "bistro" || t == "bar")
        {   // ROŻEN KEBABA (10.11 09:10): stalowa maszyna z palnikami na stoliku po drugiej stronie lady, mięso kręci się (BraxObrot)
            float rc0 = ladaS1 + .15f, rc1 = rc0 + 1.1f;   // tam stoi rożen z kurczakami (ta sama reguła co wyżej) — kebab po drugiej stronie lady
            bool kurczakLewo = rc1 > sIn1 - .4f || Mathf.Abs((rc0 + rc1) * .5f - f.drzwiS) < 1.2f;
            float kx1 = kurczakLewo ? ladaS1 + .74f : ladaS0 - .12f, kx0 = kx1 - .62f, kx = (kx0 + kx1) * .5f;
            if (kx0 > sIn0 + .3f && kx1 < sIn1 - .3f && Mathf.Abs(kx - f.drzwiS) > 1.2f)
            {
                var stal = BraxUliceKit.S(BraxUliceKit.Mat("gastronomia: stal nierdzewna", new Color(.78f, .79f, .8f), .25f, .9f));
                var zar = BraxUliceKit.S(BraxMiastoA.Swieci("rożen: grzałka (żar)", new Color(1f, .45f, .12f), new Color(2.4f, .9f, .2f)));
                float dP = ladaD - .02f, dT = ladaD - ladaG + .04f, yb = y0 + ladaH, topP = yb + .035f + .76f * k, dM = dP - .26f;
                stal.Box(W(kx0, y0, dP), W(kx1, yb, dT));                                                  // stolik
                stal.Box(W(kx - .22f, yb, dP - .02f), W(kx + .22f, yb + .035f, dT + .1f));                 // tacka
                stal.Box(W(kx - .22f, yb, dT + .09f), W(kx + .22f, topP + .02f, dT + .01f));               // obudowa palników
                for (int q = 0; q < 3; q++) zar.Quad(W(kx - .15f, yb + .14f + q * .26f * k, dT + .094f), W(kx + .15f, yb + .14f + q * .26f * k, dT + .094f), W(kx + .15f, yb + .14f + q * .26f * k + .12f * k, dT + .094f), W(kx - .15f, yb + .14f + q * .26f * k + .12f * k, dT + .094f), f.N);
                foreach (float sb in new[] { kx - .23f, kx + .22f }) stal.Box(W(sb, yb + .035f, dM - .02f), W(sb + .012f, topP - .05f, dT + .09f));
                stal.Box(W(kx - .05f, topP, dM - .04f), W(kx + .05f, topP + .08f, dT + .09f));            // ramię z silnikiem
                var mk = BraxRegion.Model("kebab_mieso");
                var ob = mk ? BraxRegion.Put(rodzic, mk, "Kebab na rożnie (kręci się)", W(kx, yb + .035f, dM), 0f, BraxUliceKit.K, false) : null;
                if (ob)
                {
                    foreach (var tt in ob.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tt.gameObject, (StaticEditorFlags)0);
                    ob.AddComponent<BraxObrot>();
                }
                var kk = new GameObject("kolizja: rożen kebaba"); kk.transform.SetParent(kolizje, false); var bck = kk.AddComponent<BoxCollider>();
                var ka = W(kx0, y0, dP); var kb = W(kx1, topP + .08f, dT); bck.center = (ka + kb) * .5f; bck.size = new Vector3(Mathf.Abs(kb.x - ka.x), kb.y - ka.y, Mathf.Abs(kb.z - ka.z));
            }
        }
        if (t == "pizzeria")
        {   // PIEC DO PIZZY z cegły w tylnym narożniku po stronie bez drzwi: cokół, płyta z granitu, komora, skośna czapa, otwór z żarem, rura
            var cegla = BraxUliceKit.S(BraxUliceKit.Tex("piec do pizzy: cegła", 1.0f, null, new Color(.55f, .25f, .16f), .9f, "red_brick_03", "brick_wall_001"));
            var gr = BraxUliceKit.S(BraxUliceKit.Tex("blat: granit polerowany", 1.2f, null, new Color(.2f, .19f, .18f), .22f, "granite_tile_02", "granite_tile"));
            float p0 = drzwiPrawo ? sIn0 + .08f : sIn1 - 1.38f, p1 = p0 + 1.3f, d0 = dTyl + .58f, d1 = dTyl + 1.8f, hc = .85f * k, hk = 1.45f * k, sm = (p0 + p1) * .5f;
            cegla.Box(W(p0, y0, d1), W(p1, y0 + hc, d0));
            gr.Box(W(p0 - .03f, y0 + hc, d1 + .04f), W(p1 + .03f, y0 + hc + .05f, d0));
            cegla.Box(W(p0 + .07f, y0 + hc + .05f, d1 - .12f), W(p1 - .07f, y0 + hk, d0));
            cegla.Quad(W(p0 + .07f, y0 + hk, d1 - .12f), W(p1 - .07f, y0 + hk, d1 - .12f), W(p1 - .25f, y0 + hk + .25f, d0 + .4f), W(p0 + .25f, y0 + hk + .25f, d0 + .4f), Vector3.up + f.N);
            cegla.Quad(W(p0 + .25f, y0 + hk + .25f, d0 + .4f), W(p1 - .25f, y0 + hk + .25f, d0 + .4f), W(p1 - .25f, y0 + hk + .25f, d0), W(p0 + .25f, y0 + hk + .25f, d0), Vector3.up);
            float yo0 = y0 + hc + .1f, yo1 = yo0 + .32f * k, dO = d1 - .12f + .006f;
            BraxUliceKit.S(BraxUliceKit.Mat("piec: wnętrze (sadza)", new Color(.04f, .035f, .03f), .9f)).Quad(W(sm - .3f, yo0 + .1f, dO), W(sm + .3f, yo0 + .1f, dO), W(sm + .3f, yo1, dO), W(sm - .3f, yo1, dO), f.N);
            BraxUliceKit.S(BraxMiastoA.Swieci("piec: żar", new Color(1f, .42f, .1f), new Color(2.4f, .8f, .15f))).Quad(W(sm - .3f, yo0, dO), W(sm + .3f, yo0, dO), W(sm + .3f, yo0 + .1f, dO), W(sm - .3f, yo0 + .1f, dO), f.N);
            BraxUliceKit.S(BraxUliceKit.Mat("gastronomia: stal nierdzewna", new Color(.78f, .79f, .8f), .25f, .9f)).Rura(W(sm, y0 + hk + .25f, (d0 + d1) * .5f - .1f), W(sm, ySuf, (d0 + d1) * .5f - .1f), .1f, 12);
            var kp = new GameObject("kolizja: piec do pizzy"); kp.transform.SetParent(kolizje, false); var bcp = kp.AddComponent<BoxCollider>();
            var pa = W(p0, y0, d1 + .04f); var pb = W(p1, y0 + hk + .25f, d0); bcp.center = (pa + pb) * .5f; bcp.size = new Vector3(Mathf.Abs(pb.x - pa.x), pb.y - pa.y, Mathf.Abs(pb.z - pa.z));
        }
        if (t == "kawiarnia" || t == "bistro" || t == "pizzeria" || t == "bar" || t == "cukiernia" || t == "market" || t == "hotel" || t == "ksiegarnia")
        {   // lampy wiszące nad ladą (miasto ekskluzywne): trzy klosze z Poly Haven, górą pod sufitem
            for (int i = 0; i < 3; i++)
            {
                var lp = PH(bg, "modern_ceiling_lamp_01", "Lampa wisząca nad ladą", W(Mathf.Lerp(ladaS0 + .3f, ladaS1 - .3f, i * .5f), 2.2f * k, ladaD - ladaG * .5f), 0f, .6f * k);
                if (lp) { var lb = BraxRegion.Bounds(lp); lp.transform.position += Vector3.up * (ySuf - .002f - lb.max.y); }
            }
        }
        if (t == "kantor")
        {   // KANTOR: tablica kursów za ladą (czarna, cyfry świecące bursztynem), krzesła dla czekających, roślina
            var tab = BraxUliceKit.Mat("kantor: tablica kursów (czarna)", new Color(.03f, .03f, .035f), .3f, .2f);
            var led = BraxMiastoA.Swieci("kantor: cyfry kursów (bursztyn)", new Color(1f, .62f, .12f), new Color(2.2f, 1.2f, .2f));
            float kc = (ladaS0 + ladaS1) * .5f, kd = dTyl + .02f, ty0 = 1.25f * k, ty1 = 2.45f * k;
            BraxUliceKit.S(tab).Box(W(kc - .95f, y0 + ty0, kd + .05f), W(kc + .95f, y0 + ty1, kd));
            string[] wiersze = { "KUPNO  SPRZEDAŻ", "USD  3.96  4.04", "EUR  4.29  4.37", "GBP  5.01  5.13", "CHF  4.46  4.58", "NOK  0.36  0.39" };
            for (int i = 0; i < wiersze.Length; i++)
                BraxUliceKit.Napis3D(wiersze[i], W(kc, y0 + ty1 - .2f - i * (ty1 - ty0 - .2f) / wiersze.Length, kd + .055f), f.N, (i == 0 ? .075f : .095f) * k, .006f, led, led);
            {   // 10.11 (właściciel 10:13 „przeglądasz każde jedno”): zamiast plastikowych krzeseł ogrodowych — ławka poczekalni przy ścianie
                // bocznej: stelaż ze stali szczotkowanej, siedzisko i oparcie z grafitowej skóry
                float sW = drzwiPrawo ? sIn0 : sIn1, kr = drzwiPrawo ? 1f : -1f, d0 = -1.7f, d1 = d0 - 1.8f * k;
                var skG = BraxUliceKit.S(BraxMiastoA.Tex("kantor: ławka — skóra grafitowa", .7f, new Color(.32f, .32f, .34f), new Color(.12f, .12f, .13f), .45f, "brown_leather", "leather_white"));
                var inox = BraxUliceKit.S(BraxUliceKit.Mat("kantor: stal szczotkowana", new Color(.7f, .71f, .72f), .3f, .9f));
                float sA = sW + kr * .06f, sB = sW + kr * .56f;
                foreach (float d in new[] { d0 - .08f, d1 + .12f })
                {
                    inox.Box(W(sA, y0, d), W(sB, y0 + .03f, d - .04f));                      // płoza
                    inox.Box(W(sB - kr * .05f, y0, d), W(sB - kr * .01f, y0 + .4f * k, d - .04f)); // noga przednia
                    inox.Box(W(sA, y0, d), W(sA + kr * .04f, y0 + .82f * k, d - .04f));      // noga tylna z wspornikiem oparcia
                }
                inox.Box(W(sA + kr * .04f, y0 + .36f * k, d0), W(sB, y0 + .4f * k, d1));      // rama siedziska
                skG.Poducha(W(sA + kr * .05f, y0 + .4f * k, d0 - .02f), W(sB + kr * .02f, y0 + .49f * k, d1 + .02f), .035f, .01f);   // siedzisko
                skG.Poducha(W(sA + kr * .045f, y0 + .52f * k, d0 - .02f), W(sA + kr * .13f, y0 + .84f * k, d1 + .02f), .03f, .01f); // oparcie
                var kb = new GameObject("kolizja: ławka kantoru"); kb.transform.SetParent(kolizje, false); var bcb = kb.AddComponent<BoxCollider>();
                var ka = W(sA, y0, d0); var kz = W(sB + kr * .02f, y0 + .84f * k, d1); bcb.center = (ka + kz) * .5f; bcb.size = new Vector3(Mathf.Abs(kz.x - ka.x), kz.y - ka.y, Mathf.Abs(kz.z - ka.z));
            }
            PH(bg, "wall_clock", "Kantor: zegar", W(kc + 1.35f, y0 + 2.2f * k, dTyl + .03f), yawDoUlicy, .32f * k);
            float sSc = drzwiPrawo ? sIn0 + .03f : sIn1 - .03f;
            PH(bg, "hanging_picture_frame_02", "Kantor: obraz na ścianie", W(sSc, y0 + 1.55f * k, -3.2f), Quaternion.LookRotation(drzwiPrawo ? f.R : -f.R, Vector3.up).eulerAngles.y, .7f * k);
            PH(bg, "doniczka_kalatea_3", "Kantor: roślina", W(sWolna1 - .2f, y0, -.7f), 30f);
        }
        if (t == "kwiaty")
        {   // KWIACIARNIA: duże rośliny w donicach na posadzce przy witrynie i pod regałem, wiadra z kwiatami
            string[] duze = { "doniczka_kalatea_3", "doniczka_anturium_3", "doniczka_paproc_2", "doniczka_kalatea_2", "doniczka_anturium_2" };
            int q = 0;
            for (float s = sWolna0; s < sWolna1 && q < 5; s += .7f * k, q++) PH(rodzic, duze[q % duze.Length], "Kwiaciarnia: roślina na posadzce", W(s, y0 + .004f, -1.0f - (q % 2) * .35f), (float)rng.NextDouble() * 360f);
            for (float s = sIn0 + .6f; s < sIn1 - .6f && q < 11; s += .8f * k, q++) PH(rodzic, duze[q % duze.Length], "Kwiaciarnia: roślina pod regałem", W(s, y0 + .004f, dTyl + 1.05f), (float)rng.NextDouble() * 360f);
        }
        if (t == "warsztat")
        {   // WARSZTAT ROWEROWY: rowery (z kodu — Poly Haven nie ma roweru) przy ścianie bocznej, koła na ścianie, stół z imadłem, skrzynia narzędzi
            Color[] lakiery = { new Color(.55f, .08f, .07f), new Color(.08f, .22f, .45f), new Color(.12f, .35f, .18f), new Color(.85f, .75f, .1f) };
            float sR = drzwiPrawo ? sIn0 + .45f : sIn1 - .45f;
            for (int i = 0; i < 3; i++) Rower(W(sR, y0 + .004f, -1.6f - i * 1.35f * k), -f.N, lakiery[i % lakiery.Length], k, drzwiPrawo ? 1f : -1f, f.R);
            foreach (var (o, ki) in new[] { (-.6f, 0), (.6f, 1) }) KoloRoweru(W(drzwiPrawo ? sIn0 + .32f : sIn1 - .32f, y0 + 2.25f * k, -2.0f + o), f.R, k);
            PH(bg, "metal_tool_chest", "Warsztat: skrzynia narzędzi", W(ladaS1 + .6f, y0, ladaD - .3f), yawDoUlicy, .65f * k);
            PH(bg, "tool_cart", "Warsztat: wózek narzędziowy", W(sWolna1 - .2f, y0, -2.6f), yawDoUlicy + 15f);
            PH(bg, "tire_pump", "Warsztat: pompka", W(ladaS0 - .35f, y0, ladaD - .1f), yawDoUlicy);
            // 10.11 (właściciel 10:13 „przeglądasz każde jedno”): środek warsztatu — rower na stojaku serwisowym (zacisk na rurze
            // podsiodłowej, trójnóg), pod nim gumowa mata; między rowerami przy ścianie a przejściem od drzwi
            float sMin = drzwiPrawo ? sR + .7f : f.drzwiS + 1.2f, sMax = drzwiPrawo ? f.drzwiS - 1.2f : sR - .7f;
            float dCs = (-1.55f + Mathf.Min(-2.4f, ladaD + .75f)) * .5f;
            if (sMax - sMin > 2.1f)
            {
                float sc = (sMin + sMax) * .5f; Vector3 wR = f.R, bokR = Vector3.Cross(Vector3.up, wR).normalized, upK = Vector3.up * k;
                var fl = W(sc, y0, dCs);
                BraxUliceKit.S(BraxUliceKit.Mat("warsztat: mata gumowa", new Color(.07f, .07f, .075f), .9f)).Box(fl - wR * 1.0f - bokR * .5f + Vector3.up * .008f, fl + wR * 1.0f + bokR * .5f + Vector3.up * .018f);
                var p = fl + Vector3.up * (.018f + .3f * k);
                Rower(p, wR, new Color(.85f, .75f, .1f), k, 1f, f.R);
                var stS = BraxUliceKit.S(BraxUliceKit.Mat("warsztat: stojak serwisowy (stal malowana)", new Color(.08f, .09f, .1f), .5f, .6f));
                var czer = BraxUliceKit.S(BraxUliceKit.Mat("warsztat: zacisk stojaka (czerwony)", new Color(.7f, .08f, .06f), .4f, .3f));
                var zac = p + wR * (-.15f * k) + upK * .66f;                               // zacisk na rurze podsiodłowej
                var baza = fl + wR * (-.15f * k) + bokR * .42f + Vector3.up * .018f;
                float hZ = zac.y - baza.y;
                stS.Rura(baza + Vector3.up * .2f, baza + Vector3.up * (hZ + .1f), .024f, 10);
                stS.Rura(baza + Vector3.up * (hZ + .02f), zac + bokR * .05f, .018f, 8);
                czer.Box(zac + bokR * .07f - wR * .05f - Vector3.up * .05f, zac + bokR * .02f + wR * .05f + Vector3.up * .05f);
                foreach (float a in new[] { 90f, 210f, 330f })
                {
                    var kier = Quaternion.AngleAxis(a, Vector3.up) * wR;
                    stS.Rura(baza + Vector3.up * .2f, baza + kier * .42f + Vector3.up * .015f, .016f, 8);
                    czer.Box(baza + kier * .42f - new Vector3(.03f, 0f, .03f), baza + kier * .42f + new Vector3(.03f, .03f, .03f));
                }
                var ks = new GameObject("kolizja: rower na stojaku"); ks.transform.SetParent(kolizje, false); var bcs = ks.AddComponent<BoxCollider>();
                var k0 = fl - wR * .95f - bokR * .2f; var k1 = fl + wR * .95f + bokR * .85f + Vector3.up * 1.4f;
                bcs.center = (k0 + k1) * .5f; bcs.size = new Vector3(Mathf.Abs(k1.x - k0.x), k1.y - k0.y, Mathf.Abs(k1.z - k0.z));
            }
        }
        if (t == "poczta")
        {   // POCZTA: paczki na posadzce za ladą i przy wejściu
            for (int i = 0; i < 4; i++) PH(bg, "cardboard_box_01", "Poczta: paczka " + (i + 1), W(ladaS0 + .4f + i * .5f, y0 + (i == 3 ? .34f * k * .7f : 0f), ladaD - ladaG - .6f), yawDoUlicy + i * 9f, (.34f - i * .03f) * k * .7f);
        }
        if (t == "apteka" || t == "optyk" || t == "ksiegarnia" || t == "kawiarnia" || t == "cukiernia")
        {   // rośliny w donicach przy witrynie (każdy lokal „żywy”)
            PH(rodzic, t == "apteka" ? "doniczka_paproc_2" : "doniczka_kalatea_3", "Roślina przy witrynie", W(drzwiPrawo ? sIn0 + .45f : sIn1 - .45f, y0 + .004f, -.75f), (float)rng.NextDouble() * 360f);
        }
        if (t == "optyk")
        {   // okulary na ladzie + tablica do badania wzroku (litery malejące rzędami) na ścianie bez regału, krzesło badanego naprzeciw
            // okulary na ladzie (z kodu, ta sama skala co na ścianie) i lupa
            for (int i = 0; i < 3; i++) { LosujOkulary(rng, i == 1, out int mo, out var ko, out int pz); Okulary(W(ladaS0 + .3f + i * .3f * k, y0 + ladaH + .0005f, ladaD - ladaG * .55f), f.N, ko, k, mo, pz); }
            PH(rodzic, "magnifying_glass_01", "Optyk: lupa na ladzie", W(ladaS1 - .35f, y0 + ladaH + .004f, ladaD - ladaG * .5f), yawDoUlicy + 20f, .2f * k);
            bool prawa = f.drzwiS > 3.2f;                       // regał boczny stoi przy lewej ścianie → tablica na prawej (i odwrotnie)
            Vector3 nT = prawa ? -f.R : f.R; float sT = prawa ? sIn1 - .02f : sIn0 + .02f, dT = -2.6f;
            var bialy = BraxUliceKit.Mat("optyk: tablica Snellena (biała)", new Color(.96f, .96f, .95f), .5f);
            var czarny = BraxUliceKit.Mat("optyk: litery tablicy (czarne)", new Color(.03f, .03f, .03f), .5f);
            var c0 = W(sT, 1.05f * k, dT); var tb = BraxUliceKit.S(bialy);
            tb.OBox(c0 + nT * .01f + Vector3.up * .55f * k, Vector3.Cross(Vector3.up, nT), Vector3.up, nT, .62f * k, 1.1f * k, .02f);
            string[] rz = { "E", "F P", "T O Z", "L P E D", "P E C F D", "E D F C Z P" };
            float yy = 1.05f * k + 1.0f * k;
            for (int i = 0; i < rz.Length; i++)
            {
                float cap = (.16f - i * .023f) * k; yy -= cap + .05f * k;
                BraxUliceKit.Napis3D(rz[i], W(sT, yy, dT) + nT * .021f, nT, cap, .004f, czarny, czarny);
            }
            // 10.11 (12:25 „każde jedno krzesło ma być obite”): krzesło tapicerowane zamiast szkolnego (sklejka) z Poly Haven
            KrzesloTapicerowane("Optyk: krzesło do badania wzroku (tapicerowane)", bg, W(prawa ? sIn0 + 1.6f : sIn1 - 1.6f, y0, dT), Quaternion.LookRotation(-nT, Vector3.up).eulerAngles.y);
        }
    }

    // ROWER (warsztat rowerowy): rama z rur lakierowanych, koła (opona, obręcz, 18 szprych, piasta), widelec, kierownica, siodełko, korba.
    // p = punkt na posadzce pod środkiem roweru, w = kierunek jazdy, wymiary roweru miejskiego 28" × k.
    static void Rower(Vector3 p, Vector3 w, Color lakier, float k, float strona, Vector3 R)
    {
        w.y = 0; w.Normalize(); var bok = Vector3.Cross(Vector3.up, w).normalized;
        var rama = BraxUliceKit.S(BraxUliceKit.Mat("rower: rama lakierowana " + ColorUtility.ToHtmlStringRGB(lakier), lakier, .35f, .3f));
        var stal = BraxUliceKit.S(BraxUliceKit.Mat("rower: stal chromowana", new Color(.72f, .73f, .75f), .25f, .9f));
        var czarny = BraxUliceKit.S(BraxUliceKit.Mat("rower: siodełko i chwyty", new Color(.07f, .065f, .06f), .6f));
        Vector3 P(float x, float y, float b = 0f) => p + w * (x * k) + Vector3.up * (y * k) + bok * (b * k);
        foreach (float ox in new[] { -.53f, .53f }) KoloRoweru(P(ox, .35f), bok, k);
        var bb = P(-.04f, .29f); var siod = P(-.22f, .84f); var glG = P(.36f, .86f); var glD = P(.41f, .70f);
        rama.Rura(bb, siod, .016f * k, 8); rama.Rura(siod, glG, .015f * k, 8); rama.Rura(bb, glD, .02f * k, 8); rama.Rura(glD, glG, .02f * k, 8);
        foreach (float b in new[] { -.045f, .045f })
        {
            rama.Rura(bb, P(-.53f, .35f, b), .009f * k, 6); rama.Rura(P(-.2f, .8f), P(-.53f, .35f, b), .008f * k, 6);
            rama.Rura(glD, P(.53f, .35f, b * 1.1f), .011f * k, 6);
        }
        stal.Rura(siod, P(-.25f, .95f), .012f * k, 6);
        czarny.Box(P(-.37f, .95f, -.075f), P(-.13f, 1.0f, .075f));
        stal.Rura(glG, P(.33f, 1.0f), .013f * k, 6); stal.Rura(P(.33f, 1.0f, -.28f), P(.33f, 1.0f, .28f), .011f * k, 8);
        foreach (float b in new[] { -.28f, .28f }) czarny.Rura(P(.33f, 1.0f, b * .78f), P(.33f, 1.0f, b), .016f * k, 8);
        stal.Rura(P(-.04f, .29f, -.07f), P(-.04f, .29f, .07f), .02f * k, 8);
        foreach (float sg in new[] { -1f, 1f }) { stal.Rura(P(-.04f, .29f, sg * .07f), P(-.04f + sg * .12f, .29f - sg * .1f, sg * .07f), .008f * k, 6); czarny.Box(P(-.04f + sg * .12f - .045f, .29f - sg * .1f - .01f, sg * .07f - .02f), P(-.04f + sg * .12f + .045f, .29f - sg * .1f + .01f, sg * .07f + .02f)); }
    }
    static void KoloRoweru(Vector3 c, Vector3 os, float k)
    {
        os.Normalize(); var u = Vector3.Cross(os, Vector3.up).sqrMagnitude > .01f ? Vector3.Cross(os, Vector3.up).normalized : Vector3.forward; var v = Vector3.Cross(os, u).normalized;
        var guma = BraxUliceKit.S(BraxUliceKit.Mat("rower: opona (guma)", new Color(.05f, .05f, .05f), .9f));
        var stal = BraxUliceKit.S(BraxUliceKit.Mat("rower: stal chromowana", new Color(.72f, .73f, .75f), .25f, .9f));
        float R = .35f * k; int n = 28;
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            Vector3 d0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
            guma.Rura(c + d0 * (R - .018f * k), c + d1 * (R - .018f * k), .019f * k, 8, false);
            stal.Rura(c + d0 * (R - .042f * k), c + d1 * (R - .042f * k), .008f * k, 5, false);
        }
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2f / 18; var d = u * Mathf.Cos(a) + v * Mathf.Sin(a);
            stal.Rura(c + os * ((i % 2 == 0 ? .025f : -.025f) * k), c + d * (R - .048f * k), .0013f * k, 3, false);
        }
        stal.Rura(c - os * .05f * k, c + os * .05f * k, .018f * k, 8);
    }

    /// <summary>11.11: odległość środka stolika od osi drzwi lokalu (wolne przejście 2.3 m od drzwi w głąb).</summary>
    const float WolneOdDrzwi = 2.3f;
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
        // 10.11 (właściciel 12:25: „każde jedno krzesło ma być obite… sofa, wszystko pobijane, nic sztucznego”): bez ogrodowego
        // kompletu z Poly Haven (gołe drewniane siedziska) i bez krzeseł z brył. Stolik: blat z orzecha z fazą, czarna stalowa
        // noga na talerzu; krzesła tapicerowane (KrzesloTapicerowane: rama z orzecha, miękkie siedzisko i oparcie ze skóry pikowanej).
        var orzech = BraxMiastoA.Tex("meble: orzech ciemny (fornir)", 1.4f, new Color(.52f, .36f, .27f), new Color(.20f, .12f, .07f), .42f, "teak_veneer", "dark_wood");
        var stal = BraxUliceKit.Mat("meble kawiarni: stal czarna", new Color(.06f, .06f, .065f), .45f, .5f);
        var blat = Siatka("stolik kawiarniany (orzech)", s => { s.Walec(new Vector3(0, .72f * k - .035f, 0), .36f * k, .035f, 28); s.Walec(new Vector3(0, .72f * k - .05f, 0), .33f * k, .015f, 28); });
        var noga = Siatka("noga stolika (talerz)", s => { s.Walec(Vector3.zero, .24f * k, .025f, 24); s.Rura(new Vector3(0, .025f, 0), new Vector3(0, .72f * k - .05f, 0), .035f, 12); });
        Mebel("Stolik kawiarniany", bg, blat, orzech, at, 0); Mebel("Stolik kawiarniany: noga", bg, noga, stal, at, 0);
        foreach (float sg in new[] { -1f, 1f })
            KrzesloTapicerowane("Krzesło kawiarniane (tapicerowane)", bg, at + f.R * sg * .66f * k, Quaternion.LookRotation(-f.R * sg).eulerAngles.y);
        var kc = new GameObject("kolizja: stolik"); kc.transform.SetParent(kolizje, false); kc.transform.position = at + Vector3.up * .36f * k;
        var col = kc.AddComponent<CapsuleCollider>(); col.radius = .3f * k; col.height = .72f * k;
    }
    // KRZESŁO TAPICEROWANE (właściciel 10.11 12:25: „każde jedno krzesło ma być obite… nic sztucznego”, „czarną skórę… pikowaną”):
    // siedzący patrzy w +z lokalnie. Rama z orzecha: cztery nogi, oskrzynienie, tylne nogi przechodzą w słupki oparcia (lekko
    // odchylone). Tapicerka: miękkie siedzisko 8 cm i poduszka oparcia ze skóry pikowanej czarnej (UlSiatka.Poducha).
    internal static GameObject KrzesloTapicerowane(string nazwa, Transform bg, Vector3 at, float yaw)
    {
        float k = BraxUliceKit.K;
        var orzech = BraxMiastoA.Tex("meble: orzech ciemny (fornir)", 1.4f, new Color(.52f, .36f, .27f), new Color(.20f, .12f, .07f), .42f, "teak_veneer", "dark_wood");
        var skora = BraxUliceKit.TexPlik("meble: skóra pikowana czarna", "Assets/Source/Claude/Nova/tekstury/", "nova_skora_pikowana", .5f, new Color(.2f, .18f, .18f), new Color(.1f, .09f, .09f), .42f);
        float w = .23f * k, hs = .40f * k;
        var rama = Siatka("krzesło tapicerowane: rama", s =>
        {
            foreach (float x in new[] { -w + .025f, w - .025f })
            {
                s.Rura(new Vector3(x, 0, w - .025f), new Vector3(x, hs, w - .025f), .019f, 8);                      // nogi przednie
                s.Rura(new Vector3(x, 0, -w + .025f), new Vector3(x, hs, -w + .025f), .019f, 8);                    // nogi tylne
                s.Rura(new Vector3(x, hs, -w + .025f), new Vector3(x, .9f * k, -w - .02f * k), .017f, 8);           // słupki oparcia (odchylone)
            }
            s.Box(new Vector3(-w, hs - .05f, -w), new Vector3(w, hs, w));                                            // oskrzynienie
            s.Box(new Vector3(-w + .01f, .88f * k, -w - .04f * k), new Vector3(w - .01f, .92f * k, -w - .01f * k)); // górna listwa oparcia
        });
        var tap = Siatka("krzesło tapicerowane: tapicerka", s =>
        {
            s.Poducha(new Vector3(-w - .005f, hs, -w + .01f), new Vector3(w + .005f, hs + .08f * k, w + .01f), .03f, .014f);     // siedzisko
            s.Poducha(new Vector3(-w + .03f, hs + .14f * k, -w - .03f * k), new Vector3(w - .03f, .86f * k, -w + .03f * k), .028f, .012f);   // oparcie
        });
        var g = Mebel(nazwa, bg, rama, orzech, at, yaw);
        var t = new GameObject(nazwa + ": tapicerka"); t.transform.SetParent(g.transform, false); t.transform.position = at; t.transform.rotation = Quaternion.Euler(0, yaw, 0);
        t.AddComponent<MeshFilter>().sharedMesh = tap; t.AddComponent<MeshRenderer>().sharedMaterial = skora;
        var c = g.AddComponent<BoxCollider>(); c.center = new Vector3(0, .3f * k, 0); c.size = new Vector3(.46f * k, .6f * k, .46f * k);
        return g;
    }

    static void Hoker(Vector3 at, Transform bg)
    {
        float k = BraxUliceKit.K;
        var skora = BraxUliceKit.TexPlik("meble: skóra pikowana czarna", "Assets/Source/Claude/Nova/tekstury/", "nova_skora_pikowana", .5f, new Color(.2f, .18f, .18f), new Color(.1f, .09f, .09f), .42f);
        var chrom = BraxUliceKit.Mat("hoker: stal chromowana", new Color(.62f, .63f, .65f), .18f, .95f);
        // 10.11 (właściciel 12:19: „siedziska za płaskie”): gruba miękka poducha 10 cm × k z zaokrągloną krawędzią zamiast 7-cm krążka,
        // talerz pod siedziskiem, podnóżek z rury
        var m = Siatka("hoker barowy: stelaż", s => {
            s.Walec(Vector3.zero, .2f * k, .022f, 20); s.Rura(new Vector3(0, .02f, 0), new Vector3(0, .66f * k, 0), .035f, 10);
            s.Walec(new Vector3(0, .66f * k, 0), .14f * k, .03f, 20);
            for (int q = 0; q < 16; q++) { float a0 = q * Mathf.PI / 8f, a1 = (q + 1) * Mathf.PI / 8f, rN = .16f * k; s.Rura(new Vector3(Mathf.Cos(a0) * rN, .3f * k, Mathf.Sin(a0) * rN), new Vector3(Mathf.Cos(a1) * rN, .3f * k, Mathf.Sin(a1) * rN), .011f, 6); }
            for (int q = 0; q < 4; q++) { float a = q * Mathf.PI * .5f + Mathf.PI * .25f; s.Rura(new Vector3(0, .3f * k, 0), new Vector3(Mathf.Cos(a) * .16f * k, .3f * k, Mathf.Sin(a) * .16f * k), .008f, 6); }
            });
        var siedz = Siatka("hoker barowy: siedzisko", s => s.PoduchaOkragla(new Vector3(0, .69f * k, 0), .2f * k, .1f * k, .042f * k, .016f));
        var g = Mebel("Hoker barowy", bg, m, chrom, at, 0);
        var sg = new GameObject("Hoker barowy: siedzisko (skóra pikowana)"); sg.transform.SetParent(g.transform, false); sg.transform.position = at;
        sg.AddComponent<MeshFilter>().sharedMesh = siedz; sg.AddComponent<MeshRenderer>().sharedMaterial = skora;
        var c = g.AddComponent<CapsuleCollider>(); c.center = new Vector3(0, .4f * k, 0); c.radius = .2f * k; c.height = .8f * k;
    }

    // OKULARY Z KODU (11.11, właściciel 20:05: „skala… okulary u optyka za małe”; 21:36: „modele okularów też mogę zrobić więcej.
    // Prostokątne… różne są modele okularów” + wzornik okularów przeciwsłonecznych) — oprawki jak w życiu × K, 9 modeli:
    // korekcyjne: 0 klasyczne, 1 prostokątne, 2 okrągłe metalowe, 3 „browline” (gruba górna krawędź, dół z cienkiego drutu);
    // przeciwsłoneczne: 4 pilotki (metal, kropla, podwójny mostek), 5 kocie oko, 6 duże „motylkowe”, 7 sportowe owijane,
    // 8 duże kwadratowe. Obręcz = taśma wokół obrysu szkła (lico, krawędzie, tył; grubość zmienna — browline), szkła przezroczyste
    // albo przyciemniane z gradientem (ciemniej u góry), mostek łukiem, zauszniki rozłożone do tyłu i opadające na półkę;
    // acetat „havana” w cętki (kolor na czworokąt). Oprawka odchylona 7° do tyłu — leży na obręczach i końcówkach zauszników.
    // c = środek dolnej krawędzi oprawki na półce, przod = kierunek, w który patrzą szkła (do klienta).
    struct ModelOprawki { public float a, b, p, wr, gr, most, kropla, kot, brew, owiniecie, rZaus; public bool podwojny, metal; }
    static ModelOprawki MO(float a, float b, float p, float wr, float gr, float most, float rZaus, float kropla = 0f, float kot = 0f, float brew = 0f, float owiniecie = 0f, bool podwojny = false, bool metal = false)
        => new ModelOprawki { a = a, b = b, p = p, wr = wr, gr = gr, most = most, rZaus = rZaus, kropla = kropla, kot = kot, brew = brew, owiniecie = owiniecie, podwojny = podwojny, metal = metal };
    static readonly ModelOprawki[] ModeleOprawek =
    {
        MO(.026f, .020f, 2.8f, .005f, .0045f, .009f, .0026f),                                      // 0 klasyczne
        MO(.028f, .016f, 5.0f, .0055f, .005f, .009f, .0028f),                                      // 1 prostokątne
        MO(.022f, .022f, 2.0f, .0028f, .0026f, .010f, .0017f, metal: true),                        // 2 okrągłe metalowe
        MO(.026f, .018f, 3.6f, .0018f, .004f, .009f, .0026f, brew: .0075f),                        // 3 browline
        MO(.029f, .023f, 2.2f, .0026f, .0026f, .008f, .0017f, kropla: .007f, podwojny: true, metal: true),   // 4 pilotki
        MO(.027f, .019f, 3.0f, .006f, .005f, .009f, .003f, kot: .008f),                            // 5 kocie oko
        MO(.033f, .027f, 2.5f, .007f, .006f, .008f, .0034f, kropla: .006f, kot: .004f),            // 6 motylkowe
        MO(.034f, .019f, 3.4f, .007f, .006f, .007f, .0036f, owiniecie: .014f),                     // 7 sportowe owijane
        MO(.031f, .026f, 4.2f, .0075f, .006f, .009f, .0034f),                                      // 8 kwadratowe duże
    };
    static readonly Color32 Havana = new Color32(1, 2, 3, 255);   // znacznik: acetat w cętki
    static readonly Color32[] KoloryOprawek = { new Color32(12, 12, 14, 255), Havana, new Color32(28, 38, 88, 255), new Color32(104, 18, 24, 255), new Color32(48, 70, 52, 255), new Color32(150, 40, 90, 255), new Color32(235, 232, 225, 255) };
    static readonly Color32[] KoloryMetalu = { new Color32(190, 192, 198, 255), new Color32(72, 74, 80, 255), new Color32(20, 20, 22, 255) };
    static readonly Color32[] Przyciemnienia = { new Color32(70, 72, 76, 255), new Color32(120, 72, 30, 255), new Color32(80, 60, 96, 255), new Color32(46, 70, 60, 255) };
    static Material okularyRama, okularySzkla, okularySzklaPrzyc;

    /// <summary>Losuje oprawkę: korekcyjne (modele 0–3, szkła przezroczyste) albo przeciwsłoneczne (4–8, przyciemniane).</summary>
    internal static void LosujOkulary(System.Random r, bool sloneczne, out int model, out Color32 kolor, out int przyc)
    {
        model = sloneczne ? 4 + r.Next(5) : r.Next(4);
        kolor = ModeleOprawek[model].metal ? KoloryMetalu[r.Next(KoloryMetalu.Length)] : KoloryOprawek[r.Next(KoloryOprawek.Length)];
        przyc = sloneczne ? 1 + r.Next(Przyciemnienia.Length) : 0;
    }

    internal static void Okulary(Vector3 c, Vector3 przod, Color32 kolor, float k, int model = 0, int przyc = 0)
    {
        if (!okularyRama)
        {
            okularyRama = BraxUliceKit.Mat("okulary: oprawki (acetat / metal, połysk)", Color.white, .22f, 0f);
            okularySzkla = BraxUliceKit.Mat("okulary: szkła (przezroczyste)", new Color(.8f, .86f, .9f, .18f), .03f, 0f);
            okularySzklaPrzyc = BraxUliceKit.Mat("okulary: szkła przeciwsłoneczne (przyciemniane)", new Color(1f, 1f, 1f, .78f), .03f, 0f);
        }
        var M = ModeleOprawek[Mathf.Clamp(model, 0, ModeleOprawek.Length - 1)];
        przod.y = 0f; przod.Normalize(); var prawo = Vector3.Cross(Vector3.up, przod);
        var rm = BraxUliceKit.S(okularyRama); var sz = BraxUliceKit.S(przyc > 0 ? okularySzklaPrzyc : okularySzkla); var stary = rm.kolor;
        bool havana = kolor.r == Havana.r && kolor.g == Havana.g && kolor.b == Havana.b; var rngH = new System.Random(c.GetHashCode());
        Color32 KolorQ() => havana ? (Color32)Color.Lerp(new Color(.22f, .11f, .05f), new Color(.62f, .38f, .14f), (float)rngH.NextDouble()) : kolor;
        rm.kolor = KolorQ();
        const int N = 20; const float sT = .1219f, cT = .9925f;   // obręcz z 20 odcinków; odchylenie 7°
        Vector3 L(float x, float y, float z) => c + Vector3.up * .001f + (prawo * x + Vector3.up * (y * cT + z * sT) + przod * (z * cT - y * sT)) * k;
        // obrys szkła w układzie (u na zewnątrz — ku zawiasowi, v w górę), off = przesunięcie (± pół obręczy)
        float Se(float x) => Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 2f / M.p);
        Vector2 Obrys(float t, float off)
        {
            float co = Mathf.Cos(t), si = Mathf.Sin(t);
            float u = (M.a + off) * Se(co), v = (M.b + off) * Se(si);
            if (M.kropla > 0f)
            {   // kropla (pilotki, motylkowe): góra spłaszczona, dół głębszy po stronie zewnętrznej i wysunięty na zewnątrz
                if (si > 0f) v *= 1f - .9f * M.kropla / M.b;
                else { v *= 1f + M.kropla / M.b * (.5f + 1.1f * Mathf.Max(0f, co)); u += M.kropla * .8f * Mathf.Max(0f, co) * -si; }
            }
            if (si > 0f) v += M.kot * Mathf.Pow(Mathf.Max(0f, co), 2f) * si;                             // kocie oko: zewnętrzny górny róg uniesiony
            return new Vector2(u, v);
        }
        float Wr(float t) => M.wr + M.brew * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t)), 1.5f);              // browline: gruba górna krawędź
        float Zg(float u) => -M.owiniecie * Mathf.Pow(Mathf.Max(0f, u / M.a), 2f);                       // owinięcie (sportowe): zewnętrze do tyłu
        // położenie szkła: wewnętrzna krawędź obręczy przy mostku, dół obręczy na półce
        float uMin = 0f, uMax = 0f, vMin = 0f, vMax = 0f;
        for (int i = 0; i < N; i++) { float t = i * Mathf.PI * 2f / N; var q = Obrys(t, Wr(t) * .5f); uMin = Mathf.Min(uMin, q.x); uMax = Mathf.Max(uMax, q.x); vMin = Mathf.Min(vMin, q.y); vMax = Mathf.Max(vMax, q.y); }
        float cy = -vMin, gr = M.gr;
        foreach (float sg in new[] { -1f, 1f })
        {
            float cx = sg * (M.most - uMin);
            Vector3 P(Vector2 q, float z) => L(cx + sg * q.x, cy + q.y, z + Zg(q.x));
            var srodek = P(Vector2.zero, -gr * .5f);
            for (int i = 0; i < N; i++)
            {
                float t0 = i * Mathf.PI * 2f / N, t1 = (i + 1) * Mathf.PI * 2f / N;
                Vector2 o0 = Obrys(t0, Wr(t0) * .5f), o1 = Obrys(t1, Wr(t1) * .5f), w0 = Obrys(t0, -Wr(t0) * .5f), w1 = Obrys(t1, -Wr(t1) * .5f);
                var nz = (o0 + o1).normalized; var nn = prawo * (sg * nz.x) + Vector3.up * nz.y;
                rm.kolor = KolorQ(); rm.Quad(P(o0, 0f), P(o1, 0f), P(w1, 0f), P(w0, 0f), przod);       // lico obręczy
                rm.kolor = KolorQ(); rm.Quad(P(o0, 0f), P(o1, 0f), P(o1, -gr), P(o0, -gr), nn);        // krawędź zewnętrzna
                rm.kolor = KolorQ(); rm.Quad(P(w0, 0f), P(w1, 0f), P(w1, -gr), P(w0, -gr), -nn);       // krawędź wewnętrzna
                rm.Quad(P(o0, -gr), P(o1, -gr), P(w1, -gr), P(w0, -gr), -przod);                         // tył
                // szkło (wachlarz ze środka, obie strony); przyciemniane — gradient: ciemniej u góry, jaśniej u dołu
                var q0 = P(w0, -gr * .5f); var q1 = P(w1, -gr * .5f);
                Color32 Kg(float v) { if (przyc == 0) return new Color32(255, 255, 255, 255); var b = (Color)Przyciemnienia[(przyc - 1) % Przyciemnienia.Length]; return (Color32)Color.Lerp(b * .55f, Color.Lerp(b, Color.white, .35f), Mathf.Clamp01(.5f - v / (2f * M.b))); }
                foreach (var nnS in new[] { przod, -przod })
                {
                    int k0 = sz.v.Count;
                    foreach (var (pp, vv) in new[] { (srodek, 0f), (q0, w0.y), (q1, w1.y) })
                    { sz.v.Add(pp); sz.n.Add(nnS); sz.t.Add(new Vector4(prawo.x, prawo.y, prawo.z, 1f)); sz.c.Add(Kg(vv)); sz.uv.Add(Vector2.zero); }
                    if (Vector3.Dot(Vector3.Cross(q0 - srodek, q1 - srodek), nnS) >= 0f) sz.i.AddRange(new[] { k0, k0 + 1, k0 + 2 }); else sz.i.AddRange(new[] { k0, k0 + 2, k0 + 1 });
                }
            }
            // zausznik: zawias przy zewnętrznej krawędzi obręczy, do tyłu z lekkim rozchyleniem, końcówka opada na półkę
            float xz = sg * (M.most - uMin + uMax), yz = cy + vMax * .55f, zz = Zg(uMax);
            rm.kolor = KolorQ();
            Vector3 z0 = L(xz, yz, zz - gr), z1 = L(xz + sg * .008f, yz + .004f, -.11f), z2 = L(xz + sg * .01f, .004f + .145f * sT / cT, -.145f);
            rm.Rura(L(xz - sg * .002f, yz, zz - .0005f), z0, M.rZaus * 1.1f, 4); rm.Rura(z0, z1, M.rZaus, 4, false); rm.Rura(z1, z2, M.rZaus, 4);
        }
        // mostek (łuk nad nosem); pilotki — druga belka nad szkłami
        rm.kolor = KolorQ();
        float ym = cy + vMax * .45f, rM = M.metal ? .0018f : .0026f;
        Vector3 m0 = L(-M.most - .001f, ym, -gr * .5f), m1 = L(0f, ym + .004f, -gr * .5f), m2 = L(M.most + .001f, ym, -gr * .5f);
        rm.Rura(m0, m1, rM, 4, false); rm.Rura(m1, m2, rM, 4, false);
        if (M.podwojny) { float yp = cy + vMax * .96f; rm.Rura(L(-M.most - .014f, yp, -gr * .5f), L(M.most + .014f, yp, -gr * .5f), .0016f, 4); }   // belka górna pilotek (od obręczy do obręczy)
        rm.kolor = stary;
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIESZKANIE BRAXA NAD CLUB NOVA (właściciel 31.10, 21:09–21:10: „mieszkanie braxa nad Club nova… to ma być biedne
// mieszkanie od którego zacznie się jego kariera”). Opus, 1.11, na podstawie sondy bryły (nova-probe.json z buildu 5):
//   • budynek NOVA: x 32…52, z 36…62, ulica po stronie z < 36 (front klubu z = 35.9…36.1); parter do y 4.80 (strop
//     CityMesh_709 y 3.38…4.80 nad x 32.3…51.7, z 36.4…55), wyżej pusta skorupa z elewacją (okna + płaszczyzny
//     „Deep_window_interior” = udawane pokoje za szybą), piętra co ~3 m do y 25.6.
//   • lewy przedsionek klubu (x 32.3…38.0, z 36.4…41.5) był pełną bryłą kolizji („Source room”) — tu KLATKA SCHODOWA:
//     własne drzwi od ulicy w lewym panelu frontu NOVA (między pilastrami), schody dwubiegowe 2 × 13 stopni (0.19 m),
//     spocznik, podest na piętrze, drzwi do mieszkania.
//   • MIESZKANIE nad wejściem do klubu: x 38.0…45.2, z 36.45…43.4, podłoga y 5.10, sufit 7.95 — okna od ulicy z elewacji
//     (prawdziwe szyby fasady; za nimi wycięte udawane pokoje), łuszcząca się farba, stara tapeta, linoleum, goła żarówka,
//     stare łóżko, szafa, stół z krzesłem, telewizor kineskopowy na skrzynce, aneks (kuchenka, mikrofala, czajnik),
//     regał z konserwami, kartony, puste półki na przyszłe trofea, worek treningowy w kącie, łazienka z brudnymi kaflami.
// Wszystko z Poly Haven (CC0) i kopii worka z siłowni. Nic nie świeci (żarówka = ForceVertex). Kolizje: stare bryły
// przycięte (BraxCityMeshCut.SubtractBoxColliders), widok przycięty (Subtract) tylko w otworach.
public static class BraxRegionBraxFlat
{
    const float Street = .15f, Fl = 5.10f, Ceil = 7.95f;
    // klatka
    const float SX0 = 32.42f, SX1 = 37.98f, SZ0 = 36.45f, SZ1 = 41.45f;
    // drzwi od ulicy (między pilastrami x 32.9 i 37.3 frontu NOVA)
    // pkt 46 (właściciel 02:22): brama klatki 0.9 × 2.1 m × K (1.16 × 2.72) w otworze okładziny NOVA (x 33.0…34.36, do y 3.01)
    const float DX0 = 32.99f, DX1 = 34.49f, DoorH = 2.72f, FrontZ = 36.0f; // wejście 1.50 m, dopasowane do prawdziwego otworu NOVA
    // mieszkanie
    // R1: mieszkanie powiększone w głąb budynku nad NOVA (bez wyjścia poza jej obrys x<=51.4, z<=61.4).
    const float AX0 = 37.98f, AX1 = 51.4f, AZ0 = 36.45f, AZ1 = 44.8f;
    const float InnerFaceZ = 36.66f;   // wewnętrzna ściana od ulicy (za płaszczyznami udawanych pokoi z = 36.5…36.6)

    static Transform root;
    static int props;

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        props = 0;
        root = BraxRegion.Root(cityRoot, "CLAUDE mieszkanie Braxa nad NOVA (klatka + pokój)");
        var city = BraxClaudeGround.SourceCity;
        var phys = BraxClaudeGround.SourcePhysics;

        // ---------------- materiały (Poly Haven; pierwsza dostępna z listy) ----------------
        Material M(string n, float m, Color? tint, params string[] slugs)
        {
            try { return BraxRegion.Pbr(n, m, tint, slugs); }
            catch { return BraxRegion.Pbr(n, m, tint, "concrete_wall_008", "granite_tile", "exterior_wall_cladding"); }
        }
        var peel = M("Mieszkanie: łuszcząca się farba", 2.2f, null, "peeling_painted_wall", "worn_cracked_plaster", "concrete_wall_008");
        var paper = M("Mieszkanie: stara tapeta", 1.6f, null, "decrepit_wallpaper", "peeling_painted_wall", "worn_cracked_plaster");
        var plaster = M("Mieszkanie: spękany tynk", 2.4f, null, "worn_cracked_plaster", "concrete_wall_008", "peeling_painted_wall");
        var lino = M("Mieszkanie: linoleum", 1.8f, null, "old_linoleum_flooring_01", "dirty_carpet", "wood_planks");
        var tiles = M("Mieszkanie: brudne kafle", 1.2f, null, "dirty_tiles", "rectangular_facade_tiles", "granite_tile");
        var concrete = M("Klatka: beton schodów", 1.5f, null, "concrete_wall_008", "granite_tile", "worn_cracked_plaster");
        var rust = M("Klatka: rdzawa stal", 1.0f, null, "rusty_painted_metal", "concrete_wall_008");
        var oldWood = M("Mieszkanie: zniszczone drewno", 1.2f, null, "wood_cabinet_worn_long", "wood_planks", "teak_veneer");
        var steel = BraxRegion.Fitting("Mieszkanie: okucia", new Color(.36f, .36f, .35f), .5f, .7f);

        // ---------------- 1) okna mieszkania z elewacji (szyby fasady na 1. piętrze) ----------------
        var windows = FacadeWindows(cityRoot, AX0 + .25f, AX1 - .25f, Fl + .2f, Ceil + .3f);
        BraxRegion.Note("mieszkanie Braxa: okna z elewacji " + windows.Count + (windows.Count > 0 ? " (" + string.Join("; ", windows.Select(w => w.xMin.ToString("0.00") + ".." + w.xMax.ToString("0.00") + " × " + w.yMin.ToString("0.00") + ".." + w.yMax.ToString("0.00"))) + ")" : " — ściana od ulicy pełna"));

        // ---------------- 2) cięcia: widok i stare kolizje ----------------
        int cut = 0, colCut = 0;
        Bounds V(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }
        var vols = new List<Bounds>
        {
            V(DX0, DX1, Street + .015f, Street + DoorH, FrontZ - .4f, SZ0 + .02f),         // otwór drzwi od ulicy (chodnik zostaje)
            V(SX0 + .03f, SX1 - .03f, Street + .02f, Ceil, SZ0 + .02f, SZ1 - .02f),         // wnętrze przedsionka + strop parteru nad klatką
            V(AX0 + .02f, AX1 - .02f, Fl - .05f, Ceil, AZ0 + .05f, AZ1 - .02f),             // pusta skorupa mieszkania
        };
        foreach (var w in windows) vols.Add(V(w.xMin - .04f, w.xMax + .04f, w.yMin - .04f, w.yMax + .04f, 36.42f, InnerFaceZ + .02f));   // udawane pokoje za oknami
        for (int i = 0; i < vols.Count; i++)
        {
            try { if (city) cut += BraxCityMeshCut.Subtract(city, vols[i], BraxRegion.Out + "/Cuts", "braxflat_" + i); } catch (Exception e) { BraxRegion.Note("mieszkanie: cięcie miasta " + i + ": " + e.Message); }
            try { cut += BraxCityMeshCut.Subtract(cityRoot, vols[i], BraxRegion.Out + "/Cuts", "braxflat_r" + i); } catch (Exception e) { BraxRegion.Note("mieszkanie: cięcie renowacji " + i + ": " + e.Message); }
        }
        var colVols = new[]
        {
            V(DX0, DX1, Street + .02f, Street + DoorH, FrontZ - .5f, SZ0 + .01f),
            V(SX0 - .05f, SX1 + .01f, Street + .02f, 10.6f, SZ0 - .02f, SZ1 + .01f),
            V(AX0 - .01f, AX1, Fl - .25f, Ceil + .1f, AZ0 - .02f, AZ1 + .01f),
        };
        foreach (var v in colVols)
        {
            if (phys) try { colCut += BraxCityMeshCut.SubtractBoxColliders(phys, v); } catch (Exception e) { BraxRegion.Note("mieszkanie: kolizje miasta: " + e.Message); }
            try { colCut += ClipRenovationColliders(cityRoot, v); } catch (Exception e) { BraxRegion.Note("mieszkanie: kolizje renowacji: " + e.Message); }
        }

        // ---------------- 3) klatka schodowa ----------------
        var K = Group("Klatka schodowa");
        // ściany klatki od piętra w górę (parter: ściany przedsionka zostają z miasta) + własne ściany parteru od środka
        B(K, "Klatka: ściana zachodnia", SX0 - .02f, SX0 + .06f, Street, Ceil, SZ0, SZ1, plaster, true);
        B(K, "Klatka: ściana tylna", SX0, SX1, Street, Ceil, SZ1 - .06f, SZ1 + .02f, plaster, true);
        B(K, "Klatka: ściana wschodnia (dół)", SX1 - .06f, SX1 + .02f, Street, Fl, SZ0, SZ1, plaster, true);
        B(K, "Klatka: ściana od ulicy (wnętrze)", SX0, SX1, Street, Ceil, SZ0 - .02f, SZ0 + .05f, plaster, false, DX0, DX1, Street + DoorH);
        B(K, "Klatka: posadzka", SX0, SX1, Street - .05f, Street + .005f, SZ0, SZ1, concrete, true);
        B(K, "Klatka: sufit", SX0, SX1, Ceil, Ceil + .12f, SZ0, SZ1, plaster, false);
        const int N = 13; const float tread = .25f, run = (N - 1) * tread;
        float rise = (Fl - Street) / (2 * N);
        float f1x0 = SX0 + .05f, f1x1 = f1x0 + 1.45f, f2x0 = f1x1 + .2f, f2x1 = f2x0 + 1.45f; // 2 biegi 1.45 m zamiast 1.10 m
        float zBottom = SZ0 + 1.1f, zTop = zBottom + run, yMid = Street + N * rise;
        for (int i = 0; i < N; i++)
        {
            float top1 = Street + (i + 1) * rise, z0 = zBottom + i * tread;
            B(K, "Klatka: stopień " + (i + 1), f1x0, f1x1, Street, top1, z0, i == N - 1 ? SZ1 - .06f : z0 + tread, concrete, true);
            float top2 = yMid + (i + 1) * rise, z2 = zTop - (i + 1) * tread;
            B(K, "Klatka: stopień " + (N + i + 1), f2x0, f2x1, top2 - .32f, top2, i == N - 1 ? SZ0 + .03f : z2, z2 + tread, concrete, true);
        }
        // spocznik (górny stopień biegu 1 przedłużony do ściany wschodniej biegu 2) i podest piętra
        B(K, "Klatka: spocznik", f1x0, f2x1, yMid - .3f, yMid, zTop, SZ1 - .06f, concrete, true);
        B(K, "Klatka: podest piętra", f2x0, SX1, Fl - .28f, Fl, SZ0 + .03f, zBottom, concrete, true);
        // ścianka między biegami i ściana od komórki pod schodami (pełne — zamiast balustrad)
        B(K, "Klatka: ścianka między biegami", f1x1, f2x0, Street, Ceil, zBottom, zTop, plaster, true);
        B(K, "Klatka: ściana komórki", f2x1, f2x1 + .1f, Street, Ceil, zBottom, SZ1, plaster, true);
        B(K, "Klatka: ściana komórki (przód)", f2x1 + .1f, SX1 - .06f, Street, Fl - .28f, zBottom - .06f, zBottom, plaster, true);
        B(K, "Klatka: komórka (drzwi zamknięte)", f2x1 + .4f, f2x1 + 1.2f, Street, Street + 2.0f, zBottom - .09f, zBottom - .06f, oldWood, true);
        B(K, "Klatka: korytarz piętra nad komórką", f2x1, SX1, Fl - .28f, Fl, zBottom, SZ1 - .06f, concrete, true);
        // poręcz na ścianie biegu 1 (rdzawa stal, pochylona wzdłuż biegu)
        // pkt 46: poręcze okrągłe ze wspornikami (stal malowana), skrzynki pocztowe (6 z drzwiczkami, wrzutem, wizytówką,
        // zamkiem), kinkiety zamiast gołych żarówek
        W.Start(K); W.Wnetrze = true;
        var stalK = BraxRegion.Fitting("Klatka: stal malowana (poręcze, skrzynki)", new Color(.16f, .17f, .18f), .4f, .5f);
        var mosK = BraxRegion.Fitting("Klatka: mosiądz (zamki, wizytówki)", new Color(.72f, .58f, .3f), .3f, .9f);
        foreach (var (a, b, xw) in new[] { (new Vector3(SX0 + .1f, Street + .95f, zBottom - .1f), new Vector3(SX0 + .1f, yMid + .95f, zTop + .1f), SX0 + .02f),
                                           (new Vector3(f2x1 - .08f, yMid + .95f, zTop + .1f), new Vector3(f2x1 - .08f, Fl + .95f, zBottom - .1f), f2x1) })
        {
            W.Rura(stalK, a, b, .022f, 10);
            for (int i = 0; i <= 3; i++) { var q = Vector3.Lerp(a, b, i / 3f); W.Rura(stalK, q, new Vector3(xw, q.y - .06f, q.z), .01f, 6); W.Walec(stalK, new Vector3(xw + Mathf.Sign(q.x - xw) * .005f, q.y - .1f, q.z), .035f, .08f, 10); }
        }
        for (int r = 0; r < 2; r++) for (int c = 0; c < 3; c++)
        {
            float mz0 = SZ0 + .15f + c * .3f, my0 = Street + 1.15f + r * .36f;
            W.MM(stalK, SX1 - .34f, SX1 - .06f, my0, my0 + .34f, mz0, mz0 + .29f);
            W.MM(stalK, SX1 - .36f, SX1 - .34f, my0 + .02f, my0 + .32f, mz0 + .02f, mz0 + .27f);
            W.MM(BraxRegion.Fitting("Klatka: wrzut (szczelina)", new Color(.02f, .02f, .02f), .9f, 0f), SX1 - .365f, SX1 - .36f, my0 + .24f, my0 + .27f, mz0 + .05f, mz0 + .24f);
            W.MM(mosK, SX1 - .365f, SX1 - .36f, my0 + .14f, my0 + .19f, mz0 + .07f, mz0 + .22f);
            W.Walec(mosK, new Vector3(SX1 - .37f, my0 + .06f, mz0 + .22f), .012f, .01f, 8);
        }
        foreach (var lp in new[] { new Vector3(SX0 + .07f, Street + 2.3f, SZ0 + .6f), new Vector3(SX0 + .07f, Fl + 2.0f, (SZ0 + zBottom) * .5f) })
        {
            W.MM(stalK, lp.x - .02f, lp.x, lp.y - .1f, lp.y + .1f, lp.z - .08f, lp.z + .08f);
            W.Walec(BraxRegionMeble.Swiecacy("Klatka: klosz kinkietu", new Color(1f, .95f, .85f), 1.0f), lp + Vector3.right * .1f - Vector3.up * .08f, .09f, .16f, 16);
        }
        W.Zapisz("klatka_braxa");
        Light(K, "Klatka: kinkiet (parter)", new Vector3(SX0 + .4f, Street + 2.3f, SZ0 + .6f), 1.0f, 7f);
        Light(K, "Klatka: kinkiet (piętro)", new Vector3(SX0 + .4f, Fl + 2.0f, (SZ0 + zBottom) * .5f), .9f, 6f);

        // drzwi od ulicy (pkt 46: brama 1.16 × 2.72 z ościeżnicą, domofonem, numerem, daszkiem i kinkietem)
        StreetDoor(world, M("Klatka: brama (dąb lakierowany)", 1.0f, null, "teak_veneer", "wood_cabinet_worn_long", "wood_planks"), BraxRegion.Fitting("Klatka: rama bramy (grafit)", new Color(.1f, .1f, .11f), .4f, .6f));

        // ---------------- 4) mieszkanie (właściciel 02:22: „normalne mieszkanie, urządzone”) ----------------
        // Rzut (x 37.98…51.4, z 36.66…43.4, podłoga 5.10, sufit 7.95): PRZEDPOKÓJ (zach.-przód) → ŁAZIENKA (zach.-tył, płytki
        // na całej wysokości), SALON z ANEKSEM KUCHENNYM (środek; kuchnia przy ścianie tylnej, stół z krzesłami), SYPIALNIA
        // (wsch.; łóżko 160, szafa, szafki nocne). Okna od ulicy z elewacji (szkło przezroczyste), grzejniki pod oknami,
        // listwy przypodłogowe, gniazdka, włączniki, ościeżnice z klamkami. Ściany działowe z warstwami (płytki od łazienki,
        // farba od pokoju — dwie bryły stykające się, bez nakładania). Wszystko × K, kolizje mebli.
        var A = Group("Mieszkanie (przedpokój, łazienka, salon z aneksem, sypialnia)");
        W.Start(A); W.Wnetrze = true; W.Komorka = 14f;
        var paint = M("Mieszkanie: farba ścian (ciepła biel)", 2.4f, new Color(.97f, .95f, .91f), "white_plaster_02", "worn_cracked_plaster", "concrete_wall_008");
        var paintAkcent = M("Mieszkanie: ściana akcentowa (szałwia)", 2.4f, new Color(.62f, .70f, .62f), "white_plaster_02", "worn_cracked_plaster");
        var parkiet = M("Mieszkanie: parkiet dębowy", 1.6f, null, "wood_floor_deck", "wood_planks", "teak_veneer");
        var plytki = M("Mieszkanie: płytki łazienki", .6f, new Color(.95f, .95f, .94f), "square_tiles", "rectangular_facade_tiles", "granite_tile");
        var plytkiPodl = M("Mieszkanie: płytki podłogi łazienki", .6f, new Color(.55f, .56f, .57f), "granite_tile", "square_tiles");
        var biel = BraxRegion.Fitting("Mieszkanie: biel lakier (meble, ceramika)", new Color(.95f, .95f, .94f), .3f, 0f);
        var dab = M("Mieszkanie: dąb (fronty, blaty)", 1.0f, null, "teak_veneer", "wood_cabinet_worn_long", "wood_planks");
        var grafit = BraxRegion.Fitting("Mieszkanie: grafit (blat, AGD)", new Color(.12f, .12f, .13f), .35f, .3f);
        var chrom = BraxRegion.Fitting("Mieszkanie: chrom (baterie, uchwyty)", new Color(.82f, .83f, .85f), .12f, 1f);
        var szklo = BraxRegion.Fitting("Mieszkanie: szkło (kabina, lustro)", new Color(.78f, .86f, .9f), .03f, .6f);
        var tkanina = BraxRegion.Fitting("Mieszkanie: tkanina (pościel, poduszki)", new Color(.82f, .80f, .76f), .9f, 0f);
        var tkaninaC = BraxRegion.Fitting("Mieszkanie: tkanina ciemna (narzuta, dywan)", new Color(.22f, .27f, .33f), .95f, 0f);
        float y = Fl, k = BraxMiastoA.K;
        float XW = 40.9f, XS = 47.0f, ZH = 39.4f, ZF = InnerFaceZ + .08f, ZB = AZ1 - .06f;   // ściany działowe; lico ściany od ulicy; lico ściany tylnej
        float dz0 = 36.8f, dz1 = dz0 + .9f * k, dH = 2.1f * k;                                   // drzwi z klatki (0.9 × 2.1 m × K)
        // podłogi (każde pomieszczenie osobno — bez nakładania)
        B(A, "Przedpokój: parkiet", AX0, XW, Fl - .26f, Fl, AZ0, ZH, parkiet, true);
        B(A, "Łazienka: płytki podłogi", AX0, XW, Fl - .26f, Fl, ZH, AZ1, plytkiPodl, true);
        B(A, "Salon i sypialnia: parkiet", XW, AX1, Fl - .26f, Fl, AZ0, AZ1, parkiet, true);
        B(A, "Mieszkanie: sufit", AX0, AX1, Ceil, Ceil + .12f, AZ0, AZ1, paint, true);
        // ściana tylna: łazienka w płytkach, pokój — farba, w aneksie pas płytek nad blatem (warstwy w wysokości, nie na ścianie)
        B(A, "Łazienka: ściana tylna (płytki)", AX0, XW, Fl, Ceil, ZB, AZ1, plytki, true);
        float kx0 = XW + .2f, kx1 = 44.9f;                                                       // aneks kuchenny przy ścianie tylnej
        B(A, "Salon: ściana tylna", XW, kx0, Fl, Ceil, ZB, AZ1, paint, true);
        B(A, "Aneks: ściana tylna (dół)", kx0, kx1, Fl, Fl + .9f, ZB, AZ1, paint, true);
        B(A, "Aneks: ściana tylna (pas płytek nad blatem)", kx0, kx1, Fl + .9f, Fl + 1.5f, ZB, AZ1, plytki, true);
        B(A, "Aneks: ściana tylna (góra)", kx0, kx1, Fl + 1.5f, Ceil, ZB, AZ1, paint, true);
        B(A, "Salon: ściana tylna (wsch.)", kx1, XS, Fl, Ceil, ZB, AZ1, paint, true);
        B(A, "Sypialnia: ściana tylna (akcent za łóżkiem)", XS, AX1, Fl, Ceil, ZB, AZ1, paintAkcent, true);
        B(A, "Mieszkanie: ściana wschodnia", AX1 - .1f, AX1, Fl, Ceil, AZ0, AZ1, paint, true);
        // ściana od klatki (x 37.98) z drzwiami; w łazience od środka płytki (dwie warstwy)
        B(A, "Mieszkanie: ściana od klatki (przy oknie)", AX0 - .06f, AX0 + .06f, Fl, Ceil, AZ0, dz0, paint, true);
        B(A, "Mieszkanie: ściana od klatki", AX0 - .06f, AX0 + .06f, Fl, Ceil, dz1, ZH, paint, true);
        B(A, "Mieszkanie: nadproże drzwi", AX0 - .06f, AX0 + .06f, Fl + dH, Ceil, dz0, dz1, paint, true);
        B(A, "Łazienka: ściana zach. (klatka)", AX0 - .06f, AX0, Fl, Ceil, ZH, AZ1, plaster, true);
        B(A, "Łazienka: ściana zach. (płytki)", AX0, AX0 + .06f, Fl, Ceil, ZH, AZ1, plytki, true);
        InnerStreetWall(A, windows, paint, biel);
        FlatDoor(world, dab, chrom, dz0, dz1);
        Oscieznica(A, dab, chrom, AX0, dz0, dz1, false, Fl, dH, false);

        // ---- ściany działowe ----
        // W1: x 40.6…40.7 (przedpokój / łazienka | salon): przejście z przedpokoju do salonu z 37.3…38.6 (ościeżnica bez skrzydła)
        float pz0 = 37.18f, pz1 = pz0 + 1.58f;
        B(A, "Ściana W1 (przy oknie)", XW, XW + .1f, Fl, Ceil, ZF, pz0, paint, true);
        B(A, "Ściana W1 (nad przejściem)", XW, XW + .1f, Fl + dH, Ceil, pz0, pz1, paint, true);
        B(A, "Ściana W1 (przedpokój)", XW, XW + .1f, Fl, Ceil, pz1, ZH, paint, true);
        B(A, "Ściana W1 łazienka (płytki)", XW, XW + .05f, Fl, Ceil, ZH, ZB, plytki, true);
        B(A, "Ściana W1 łazienka (salon)", XW + .05f, XW + .1f, Fl, Ceil, ZH, ZB, paint, true);
        Oscieznica(A, dab, chrom, XW + .05f, pz0, pz1, false, Fl, dH, false);
        // W2: z 39.4…39.5 (przedpokój | łazienka) z drzwiami łazienki
        float bd0 = 39.0f, bd1 = bd0 + .8f * k;
        B(A, "Ściana W2 (przedpokój)", AX0 + .06f, bd0, Fl, Ceil, ZH - .05f, ZH, paint, true);
        B(A, "Ściana W2 (płytki)", AX0 + .06f, bd0, Fl, Ceil, ZH, ZH + .05f, plytki, true);
        B(A, "Ściana W2 (przedpokój, przy W1)", bd1, XW, Fl, Ceil, ZH - .05f, ZH, paint, true);
        B(A, "Ściana W2 (płytki, przy W1)", bd1, XW, Fl, Ceil, ZH, ZH + .05f, plytki, true);
        B(A, "Ściana W2 (nadproże)", bd0, bd1, Fl + 2.1f * k * .97f, Ceil, ZH - .05f, ZH + .05f, paint, true);
        Oscieznica(A, dab, chrom, ZH, bd0, bd1, true, Fl, 2.1f * k * .97f);
        // W3: x 47.0…47.1 (salon | sypialnia) z drzwiami sypialni przy ścianie tylnej
        float sd0 = 41.4f, sd1 = sd0 + .8f * k;
        B(A, "Ściana W3", XS, XS + .1f, Fl, Ceil, ZF, sd0, paint, true);
        B(A, "Ściana W3 (przy tylnej)", XS, XS + .1f, Fl, Ceil, sd1, ZB, paint, true);
        B(A, "Ściana W3 (nadproże)", XS, XS + .1f, Fl + 2.1f * k * .97f, Ceil, sd0, sd1, paint, true);
        Oscieznica(A, dab, chrom, XS + .05f, sd0, sd1, false, Fl, 2.1f * k * .97f);

        // ---- listwy przypodłogowe (dąb, 8 cm, przy licach ścian pokoi) ----
        void Listwa(float x0, float x1, float z0, float z1) { if (x1 - x0 > .05f || z1 - z0 > .05f) W.MM(dab, x0, x1, y, y + .08f, z0, z1); }
        Listwa(XW + .1f, XS, ZF, ZF + .015f); Listwa(XS + .1f, AX1 - .1f, ZF, ZF + .015f);
        Listwa(XW + .1f, XS, ZB - .015f, ZB); Listwa(XS + .1f, AX1 - .1f, ZB - .015f, ZB);
        Listwa(AX1 - .115f, AX1 - .1f, ZF + .015f, ZB - .015f);
        Listwa(XS - .015f, XS, ZF + .015f, sd0); Listwa(XS + .1f, XS + .115f, ZF + .015f, sd0);
        Listwa(AX0 + .06f, XW, ZF, ZF + .015f); Listwa(AX0 + .06f, bd0, ZH - .065f, ZH - .05f);

        // ---- gniazdka i włączniki (płytka 8 × 8 cm, 1 cm od ściany) ----
        void Gniazdko(Vector3 p, Vector3 n, bool wlacznik)
        {
            var q = Quaternion.LookRotation(n);
            W.OBox(biel, p + n * .006f, new Vector3(.08f, .08f, .012f), q);
            if (wlacznik) W.OBox(biel, p + n * .014f, new Vector3(.05f, .03f, .006f), q);
            else foreach (float s in new[] { -.012f, .012f }) W.OBox(grafit, p + n * .013f + Vector3.Cross(Vector3.up, n).normalized * s, new Vector3(.006f, .006f, .002f), q);
        }
        Gniazdko(new Vector3(AX0 + .06f, y + 1.1f, dz1 + .15f), Vector3.right, true);                 // przedpokój: włącznik przy drzwiach
        Gniazdko(new Vector3(bd1 + .12f, y + 1.1f, ZH - .05f), Vector3.back, true);                     // włącznik łazienki
        Gniazdko(new Vector3(XS, y + .3f, 38.0f), Vector3.left, false);                                 // za sofą / TV
        Gniazdko(new Vector3(XW + .1f, y + .3f, 39.0f), Vector3.right, false);
        Gniazdko(new Vector3(42.2f, y + 1.15f, ZB), Vector3.back, false);                               // aneks nad blatem
        Gniazdko(new Vector3(43.8f, y + 1.15f, ZB), Vector3.back, false);
        Gniazdko(new Vector3(XS + .1f, y + 1.1f, sd0 - .15f), Vector3.right, true);                     // sypialnia: włącznik
        Gniazdko(new Vector3(48.05f, y + .3f, ZB), Vector3.back, false); Gniazdko(new Vector3(50.6f, y + .3f, ZB), Vector3.back, false);

        // ---- grzejniki pod oknami (panel z żebrami, zawór, rury do podłogi) ----
        foreach (var w in windows.Where(w => w.xMax > XW + .3f && w.xMin < AX1 - .3f))
        {
            float wc = (Mathf.Max(XW + .4f, w.xMin) + Mathf.Min(AX1 - .4f, w.xMax)) * .5f, ww = Mathf.Min(1.2f, w.width - .2f);
            if (ww < .4f) continue;
            float gz = ZF + .07f;
            W.MM(biel, wc - ww * .5f, wc + ww * .5f, y + .15f, y + .72f, gz, gz + .1f, true);
            for (float x = wc - ww * .5f + .05f; x < wc + ww * .5f - .04f; x += .05f) W.MM(biel, x, x + .012f, y + .17f, y + .7f, gz + .1f, gz + .115f);
            W.Rura(chrom, new Vector3(wc + ww * .5f - .05f, y, gz + .05f), new Vector3(wc + ww * .5f - .05f, y + .15f, gz + .05f), .012f, 8);
            W.Walec(biel, new Vector3(wc + ww * .5f + .03f, y + .62f, gz + .05f), .025f, .08f, 10);
        }

        // ---- PRZEDPOKÓJ: wieszak, lustro, szafka na buty, wycieraczka ----
        // R1: usunięto wiszący wieszak i haki z osi przejścia W1.
        // Przejście pozostaje wolne na pełnej wysokości postaci Braxa.
        W.MM(grafit, AX0 + .06f, AX0 + .09f, y + .8f, y + .8f + 1.2f * k, 37.95f + .2f, 38.75f + .2f);      // rama lustra
        W.MM(szklo, AX0 + .09f, AX0 + .1f, y + .83f, y + .77f + 1.2f * k, 37.98f + .2f, 38.72f + .2f);
        // Dawna szafka na buty wchodziła w otwór drzwi W1 (z 37.30…38.60) i blokowała postać.
        // Usunięta razem z frontem, blatem i uchwytami; nie dokładamy innego mebla w przejściu.
        W.MM(tkaninaC, AX0 + .3f, AX0 + 1.3f, y, y + .012f, dz0 + .05f, dz1 - .1f);                      // wycieraczka

        // ---- ŁAZIENKA: kabina prysznicowa, WC, umywalka z szafką, lustro z oświetleniem, wieszak na ręcznik ----
        float lx0 = AX0 + .06f, lz1 = ZB;
        float kb = .9f * k;                                                                              // brodzik 0.9 × 0.9 m × K
        W.MM(biel, lx0, lx0 + kb, y, y + .06f, lz1 - kb, lz1, true);
        W.MM(chrom, lx0 + kb * .5f - .05f, lx0 + kb * .5f + .05f, y + .06f, y + .062f, lz1 - kb * .5f - .05f, lz1 - kb * .5f + .05f);   // odpływ
        W.MM(szklo, lx0 + kb - .008f, lx0 + kb, y + .06f, y + 2.0f * k, lz1 - kb, lz1);                  // ścianka szklana (bok)
        W.MM(szklo, lx0, lx0 + kb * .45f, y + .06f, y + 2.0f * k, lz1 - kb, lz1 - kb + .008f);           // ścianka stała (przód), wejście obok
        W.MM(chrom, lx0 + kb - .012f, lx0 + kb + .004f, y + .06f, y + 2.0f * k, lz1 - kb - .004f, lz1 - kb + .012f);
        W.Rura(chrom, new Vector3(lx0 + .08f, y + 1.0f, lz1 - .25f), new Vector3(lx0 + .08f, y + 2.05f, lz1 - .25f), .012f, 8);        // drążek
        W.Walec(chrom, new Vector3(lx0 + .25f, y + 2.05f, lz1 - .25f), .1f, .015f, 16);                                                 // deszczownica
        W.Rura(chrom, new Vector3(lx0 + .02f, y + 2.07f, lz1 - .25f), new Vector3(lx0 + .25f, y + 2.07f, lz1 - .25f), .01f, 6);
        W.OBox(chrom, new Vector3(lx0 + .04f, y + 1.05f, lz1 - .25f), new Vector3(.06f, .12f, .14f), Quaternion.identity);              // bateria
        // WC (kompakt: postument, misa, deska, zbiornik, przycisk)
        float wcx = XW - .55f, wcz = lz1 - .32f;
        W.Walec(biel, new Vector3(wcx, y, wcz + .02f), .16f, .3f, 18);
        W.Walec(biel, new Vector3(wcx, y + .3f, wcz - .02f), .21f, .1f, 20);
        W.Walec(biel, new Vector3(wcx, y + .4f, wcz - .02f), .22f, .025f, 20);
        W.OBox(biel, new Vector3(wcx, y + .62f, lz1 - .09f), new Vector3(.4f, .42f, .17f), Quaternion.identity, true);
        W.Walec(chrom, new Vector3(wcx, y + .83f, lz1 - .09f), .03f, .01f, 12);
        W.Rura(chrom, new Vector3(XW - .02f, y + .75f, wcz - .35f), new Vector3(XW - .1f, y + .75f, wcz - .35f), .008f, 6);              // uchwyt papieru
        W.Walec(biel, new Vector3(XW - .1f, y + .69f, wcz - .35f), .055f, .1f, 12);
        // umywalka na szafce (front dąb, uchwyt), bateria, lustro z listwą LED, wieszak z ręcznikiem
        float ux = AX0 + .45f, uz0 = ZH + .25f, uz1 = uz0 + .8f;
        W.MM(dab, AX0 + .06f, AX0 + .52f, y + .25f, y + .82f, uz0, uz1, true);
        W.MM(biel, AX0 + .06f, AX0 + .54f, y + .82f, y + .86f, uz0 - .02f, uz1 + .02f);
        W.Walec(biel, new Vector3(ux - .1f, y + .86f, (uz0 + uz1) * .5f), .2f, .12f, 22);
        W.Rura(chrom, new Vector3(AX0 + .1f, y + .86f, (uz0 + uz1) * .5f), new Vector3(AX0 + .1f, y + 1.1f, (uz0 + uz1) * .5f), .013f, 8);
        W.Rura(chrom, new Vector3(AX0 + .1f, y + 1.1f, (uz0 + uz1) * .5f), new Vector3(AX0 + .25f, y + 1.06f, (uz0 + uz1) * .5f), .011f, 8);
        W.Rura(chrom, new Vector3(AX0 + .51f, y + .55f, uz0 + .15f), new Vector3(AX0 + .51f, y + .55f, uz1 - .15f), .008f, 6);
        W.MM(szklo, AX0 + .06f, AX0 + .08f, y + 1.2f, y + 1.95f, uz0 + .05f, uz1 - .05f);
        W.MM(BraxRegionMeble.Swiecacy("Mieszkanie: LED lustra", new Color(1f, .97f, .92f), 1.2f), AX0 + .06f, AX0 + .1f, y + 1.97f, y + 2.0f, uz0 + .1f, uz1 - .1f);
        W.Rura(chrom, new Vector3(XW - .02f, y + 1.3f, uz0 + .1f), new Vector3(XW - .1f, y + 1.3f, uz0 + .1f), .01f, 6);
        W.Rura(chrom, new Vector3(XW - .1f, y + 1.3f, uz0 + .1f), new Vector3(XW - .1f, y + 1.3f, uz0 + .7f), .01f, 6);
        W.MM(tkanina, XW - .13f, XW - .07f, y + .85f, y + 1.32f, uz0 + .15f, uz0 + .65f);                 // ręcznik

        // ---- ANEKS KUCHENNY (ściana tylna x kx0…kx1): szafki dolne z cokołem, blat, zlew wpuszczony, płyta indukcyjna,
        // piekarnik, szafki górne, okap, lodówka; pas płytek nad blatem jest częścią ściany ----
        float kz = ZB - .6f, yb = y + .86f;
        W.MM(grafit, kx0, kx1 - .75f, y, y + .1f, kz + .05f, ZB);                                        // cokół
        W.MM(biel, kx0, kx1 - .75f, y + .1f, yb - .04f, kz + .02f, ZB, true);                            // korpusy
        for (float x = kx0; x < kx1 - .8f; x += .6f)
        {
            W.MM(dab, x + .005f, Mathf.Min(x + .595f, kx1 - .755f), y + .11f, yb - .05f, kz, kz + .02f); // front
            W.Rura(chrom, new Vector3(x + .12f, yb - .14f, kz - .03f), new Vector3(x + .48f, yb - .14f, kz - .03f), .008f, 6);
            W.Rura(chrom, new Vector3(x + .12f, yb - .14f, kz - .03f), new Vector3(x + .12f, yb - .14f, kz), .007f, 4);
            W.Rura(chrom, new Vector3(x + .48f, yb - .14f, kz - .03f), new Vector3(x + .48f, yb - .14f, kz), .007f, 4);
        }
        float zl0 = kx0 + 1.4f, zl1 = zl0 + .6f, pl0 = kx0 + 2.4f, pl1 = pl0 + .6f;                     // zlew, płyta
        W.MM(grafit, kx0 - .01f, zl0, yb - .04f, yb, kz - .03f, ZB);
        W.MM(grafit, zl1, pl0, yb - .04f, yb, kz - .03f, ZB);
        W.MM(grafit, pl1, kx1 - .74f, yb - .04f, yb, kz - .03f, ZB);
        W.MM(grafit, zl0, zl1, yb - .04f, yb, kz - .03f, kz + .07f); W.MM(grafit, zl0, zl1, yb - .04f, yb, ZB - .1f, ZB);
        W.MM(chrom, zl0 + .02f, zl1 - .02f, yb - .2f, yb - .19f, kz + .07f, ZB - .1f);                    // dno komory
        foreach (var (a0, a1, b0, b1) in new[] { (zl0, zl0 + .02f, kz + .07f, ZB - .1f), (zl1 - .02f, zl1, kz + .07f, ZB - .1f) }) W.MM(chrom, a0, a1, yb - .2f, yb - .04f, b0, b1);
        W.MM(chrom, zl0, zl1, yb - .2f, yb - .04f, kz + .07f, kz + .09f); W.MM(chrom, zl0, zl1, yb - .2f, yb - .04f, ZB - .12f, ZB - .1f);
        W.Rura(chrom, new Vector3((zl0 + zl1) * .5f, yb, ZB - .06f), new Vector3((zl0 + zl1) * .5f, yb + .32f, ZB - .06f), .015f, 8);
        W.Rura(chrom, new Vector3((zl0 + zl1) * .5f, yb + .32f, ZB - .06f), new Vector3((zl0 + zl1) * .5f, yb + .26f, ZB - .3f), .013f, 8);
        W.MM(M("Aneks: płyta indukcyjna (czarne szkło)", .6f, new Color(.04f, .04f, .05f), "granite_tile"), pl0, pl1, yb - .04f, yb - .005f, kz - .03f, ZB);
        foreach (var (dx, dzz, r) in new[] { (.15f, .15f, .09f), (.45f, .15f, .07f), (.15f, .42f, .07f), (.45f, .42f, .09f) })
            W.Walec(grafit, new Vector3(pl0 + dx, yb - .004f, kz + dzz), r, .002f, 20);
        W.MM(grafit, pl0 + .02f, pl1 - .02f, y + .2f, yb - .1f, kz - .005f, kz + .02f);                  // piekarnik (front)
        W.MM(szklo, pl0 + .08f, pl1 - .08f, y + .3f, yb - .22f, kz - .007f, kz - .004f);
        W.Rura(chrom, new Vector3(pl0 + .08f, yb - .16f, kz - .04f), new Vector3(pl1 - .08f, yb - .16f, kz - .04f), .01f, 8);
        // szafki górne i okap
        float yg0 = y + 1.5f, yg1 = y + 2.25f;
        W.MM(biel, kx0, pl0 - .05f, yg0, yg1, ZB - .35f, ZB, true);
        for (float x = kx0; x < pl0 - .1f; x += .6f) W.MM(dab, x + .005f, Mathf.Min(x + .595f, pl0 - .055f), yg0 + .01f, yg1 - .01f, ZB - .37f, ZB - .35f);
        W.MM(grafit, pl0 - .02f, pl1 + .02f, yg0 + .1f, yg0 + .2f, ZB - .5f, ZB);                        // okap
        W.MM(grafit, pl0 + .2f, pl1 - .2f, yg0 + .2f, Ceil, ZB - .25f, ZB);
        W.MM(BraxRegionMeble.Swiecacy("Aneks: LED pod szafkami", new Color(1f, .96f, .88f), 1.0f), kx0 + .05f, pl0 - .1f, yg0 - .015f, yg0, ZB - .32f, ZB - .3f);
        // lodówka (słupek 0.7 m, dwoje drzwi, uchwyty)
        float lo0 = kx1 - .72f, lo1 = kx1 - .02f, loH = 1.85f * k;
        W.MM(biel, lo0, lo1, y, y + loH, ZB - .66f, ZB, true);
        W.MM(biel, lo0 + .01f, lo1 - .01f, y + .02f, y + loH * .62f, ZB - .68f, ZB - .66f); W.MM(biel, lo0 + .01f, lo1 - .01f, y + loH * .62f + .01f, y + loH - .01f, ZB - .68f, ZB - .66f);
        foreach (var (h0, h1) in new[] { (y + loH * .4f, y + loH * .6f), (y + loH * .64f, y + loH * .8f) }) W.Rura(chrom, new Vector3(lo0 + .06f, h0, ZB - .71f), new Vector3(lo0 + .06f, h1, ZB - .71f), .01f, 8);
        // stół z krzesłami (stół Poly Haven albo z kodu) — 4 krzesła przodem do stołu
        float tx = 43.0f, tz = 40.9f;
        var stol = Prop(A, "Aneks: stół jadalniany", new Vector3(tx, y, tz), 0f, 1f, true, .8f * k, "dining_table", "WoodenTable_02", "WoodenTable_01");
        if (!stol) { W.MM(dab, tx - .7f, tx + .7f, y + .74f * k - .04f, y + .74f * k, tz - .45f, tz + .45f, true); foreach (float sx in new[] { -.62f, .62f }) foreach (float sz in new[] { -.38f, .38f }) W.Rura(grafit, new Vector3(tx + sx, y, tz + sz), new Vector3(tx + sx, y + .74f * k - .04f, tz + sz), .025f, 8); }
        var tb = stol ? BraxRegion.Bounds(stol) : new Bounds(new Vector3(tx, y + .5f, tz), new Vector3(1.4f, 1f, .9f));
        // R1: dwa krzesła od strony zaplecza, bez pary w ciasnym przejściu od salonu.
        foreach (var (cx, cz, fx, fz) in new[] { (tx - .35f, tb.max.z + .32f, 0f, -1f), (tx + .35f, tb.max.z + .32f, 0f, -1f) })
            Krzeslo(dab, grafit, new Vector3(cx, y, cz), new Vector3(fx, 0, fz));

        // ---- SALON: sofa przodem do TV na ścianie W1, stolik, dywan, fotel, regał, rośliny, obrazy, worek i hantle w kącie ----
        float tvx = XS - .45f;                                                                              // RTV przy W3 (salon | sypialnia)
        W.MM(dab, tvx, XS, y, y + .5f, 37.4f, 39.2f, true);                                                  // szafka RTV
        W.MM(grafit, tvx - .02f, tvx, y + .06f, y + .44f, 37.45f, 38.27f); W.MM(grafit, tvx - .02f, tvx, y + .06f, y + .44f, 38.33f, 39.15f);
        W.MM(grafit, XS - .06f, XS - .02f, y + 1.0f, y + 1.0f + .8f * k, 37.6f, 37.6f + 1.4f * k);          // TV (panel, ramka)
        W.MM(M("Salon: ekran TV (wyłączony)", .5f, new Color(.03f, .035f, .04f), "granite_tile"), XS - .065f, XS - .06f, y + 1.02f, y + .98f + .8f * k, 37.62f, 37.58f + 1.4f * k);
        W.MM(tkaninaC, 42.0f, 45.2f, y, y + .012f, 37.2f, 39.4f);                                          // dywan
        Prop(A, "Salon: sofa", new Vector3(42.4f, y, 38.3f), 90f, 1f, true, .95f * k, "Sofa_01", "sofa_02");
        Prop(A, "Salon: stolik kawowy", new Vector3(43.8f, y, 38.3f), 0f, 1f, true, .45f * k, "modern_coffee_table_01", "coffee_table_round_01", "industrial_coffee_table");
        Prop(A, "Salon: fotel", new Vector3(44.6f, y, 39.6f), 225f, 1f, true, 1.0f * k, "ArmChair_01", "modern_arm_chair_01", "mid_century_lounge_chair");
        // R1: regał stojący obok przejścia usunięty, żeby odzyskać przestrzeń ruchu.
        Prop(A, "Salon: roślina przy oknie", new Vector3(45.6f, y, ZF + .35f), 0f, 1f, true, 1.4f, "potted_plant_02", "potted_plant_04", "potted_plant_01");
        Prop(A, "Salon: roślina przy RTV", new Vector3(XS - .4f, y, 36.95f + .1f), 0f, 1f, true, 1.1f, "potted_plant_04", "potted_plant_01");
        Prop(A, "Salon: obraz", new Vector3(45.8f, y + 1.55f, ZB - .03f), 180f, 1f, false, .8f, "hanging_picture_frame_02", "hanging_picture_frame_01");
        Prop(A, "Salon: zegar", new Vector3(XS - .03f, y + 2.0f, 40.4f), 270f, 1f, false, .35f, "wall_clock");
        Bag(A, new Vector3(46.4f, Ceil, 40.5f));
        for (int i = 0; i < 2; i++)   // hantle na podłodze przy worku (gryf + talerze)
        {
            float zz = 39.6f + i * .28f;
            W.Rura(chrom, new Vector3(45.65f, y + .07f, zz), new Vector3(46.05f, y + .07f, zz), .014f, 8);
            foreach (float xx in new[] { 45.7f, 46.0f }) W.Rura(grafit, new Vector3(xx - .03f, y + .07f, zz), new Vector3(xx + .03f, y + .07f, zz), .07f, 14);
        }

        // ---- SYPIALNIA: łóżko 160 z zagłówkiem i pościelą, szafki nocne z lampkami, szafa trzydrzwiowa, roślina ----
        float bw = 1.6f * k, bl = 2.0f * k, bx0 = (XS + .1f + AX1 - .1f) * .5f - bw * .5f, bx1 = bx0 + bw, bz1 = ZB - .02f, bz0 = bz1 - bl;
        W.MM(dab, bx0 - .04f, bx1 + .04f, y + .12f, y + .38f, bz0, bz1 - .1f, true);                       // rama
        foreach (float sx in new[] { bx0 + .05f, bx1 - .05f }) foreach (float sz in new[] { bz0 + .05f, bz1 - .15f }) W.Rura(grafit, new Vector3(sx, y, sz), new Vector3(sx, y + .12f, sz), .025f, 8);
        W.MM(dab, bx0 - .06f, bx1 + .06f, y, y + 1.15f, bz1 - .1f, bz1, true);                             // zagłówek
        W.MM(tkaninaC, bx0 + .04f, bx1 - .04f, y + .5f, y + 1.05f, bz1 - .14f, bz1 - .1f);                 // tapicerowany panel
        W.MM(tkanina, bx0, bx1, y + .38f, y + .58f, bz0 + .02f, bz1 - .12f);                                // materac
        W.MM(tkaninaC, bx0 - .02f, bx1 + .02f, y + .58f, y + .62f, bz0 - .02f, bz1 - .75f);                 // narzuta
        W.MM(tkaninaC, bx0 - .02f, bx1 + .02f, y + .3f, y + .58f, bz0 - .02f, bz0);
        foreach (float sx in new[] { bx0 + bw * .27f, bx0 + bw * .73f })                                     // poduszki
            W.OBox(tkanina, new Vector3(sx, y + .67f, bz1 - .38f), new Vector3(bw * .4f, .14f, .42f), Quaternion.Euler(-12f, 0, 0));
        W.Siedzisko(new Vector3((bx0 + bx1) * .5f, y + .6f, (bz0 + bz1) * .5f), Vector3.back, "ODPOCZNIJ", true);
        W.Punkt("BRAX_MIESZKANIE_ODPOCZYNEK", "Mieszkanie Braxa — odpocznij (zapis gry)", new Vector3((bx0 + bx1) * .5f, y, bz0 - .5f));
        foreach (float nx in new[] { bx0 - .55f, bx1 + .07f })                                               // szafki nocne
        {
            W.MM(dab, nx, nx + .48f, y + .1f, y + .55f, bz1 - .5f, bz1 - .02f, true);
            W.MM(grafit, nx + .02f, nx + .46f, y, y + .1f, bz1 - .48f, bz1 - .04f);
            W.Rura(chrom, new Vector3(nx + .16f, y + .42f, bz1 - .53f), new Vector3(nx + .32f, y + .42f, bz1 - .53f), .007f, 6);
            W.Walec(grafit, new Vector3(nx + .24f, y + .55f, bz1 - .25f), .07f, .03f, 14);                 // lampka
            W.Rura(chrom, new Vector3(nx + .24f, y + .58f, bz1 - .25f), new Vector3(nx + .24f, y + .9f, bz1 - .25f), .008f, 6);
            W.Walec(tkanina, new Vector3(nx + .24f, y + .88f, bz1 - .25f), .12f, .16f, 16);
            W.Lampa("Sypialnia: lampka nocna", new Vector3(nx + .24f, y + .95f, bz1 - .25f), .25f, 2.5f);
        }
        float sx0 = XS + .1f, sz0 = 37.1f, sz1 = sz0 + 2.4f;                                                // szafa przy W3
        W.MM(biel, sx0, sx0 + .62f, y + .08f, y + 2.3f, sz0, sz1, true);
        W.MM(grafit, sx0 + .02f, sx0 + .6f, y, y + .08f, sz0 + .02f, sz1 - .02f);
        for (int i = 0; i < 3; i++)
        {
            float a0 = sz0 + i * .8f + .005f, a1 = a0 + .79f;
            W.MM(dab, sx0 + .62f, sx0 + .64f, y + .1f, y + 2.28f, a0, a1);
            W.Rura(chrom, new Vector3(sx0 + .67f, y + .95f, i == 0 ? a1 - .06f : a0 + .06f), new Vector3(sx0 + .67f, y + 1.35f, i == 0 ? a1 - .06f : a0 + .06f), .009f, 8);
        }
        Prop(A, "Sypialnia: roślina", new Vector3(AX1 - .45f, y, ZF + .4f), 0f, 1f, true, 1.2f, "potted_plant_01", "potted_plant_02");
        Prop(A, "Sypialnia: obraz", new Vector3(bx0 - .9f, y + 1.6f, ZB - .03f), 180f, 1f, false, .7f, "hanging_picture_frame_03", "hanging_picture_frame_01");

        // ---- oświetlenie: lampy sufitowe (model albo oprawa z kodu) + światło ciepłe (dzień: okna elewacji) ----
        var lampyKto = new List<Vector3> { new Vector3((AX0 + XW) * .5f, Ceil, 38.1f), new Vector3((AX0 + XW) * .5f, Ceil, 41.4f), new Vector3(43.0f, Ceil, 38.3f), new Vector3(43.0f, Ceil, 41.0f), new Vector3((XS + AX1) * .5f, Ceil, 39.6f) };
        foreach (var lp in lampyKto)
        {
            if (!Prop(A, "Mieszkanie: lampa sufitowa", lp - Vector3.up * .5f, 0f, 1f, false, .5f, "modern_ceiling_lamp_01"))
            {
                W.Walec(biel, lp - Vector3.up * .06f, .22f, .05f, 24);
                W.Walec(BraxRegionMeble.Swiecacy("Mieszkanie: klosz oprawy", new Color(1f, .96f, .9f), 1.1f), lp - Vector3.up * .065f, .2f, .006f, 24);
            }
            Light(A, "Mieszkanie: światło", lp - Vector3.up * .4f, .85f, 6.5f);
        }
        int siatkiM = W.Zapisz("mieszkanie_braxa");
        Debug.Log("BRAXHORN_MIESZKANIE_BRAXA powierzchnia_m2=" + ((AX1 - AX0) * (AZ1 - InnerFaceZ)).ToString("F0") + " pokoje=przedpokój,łazienka,salon+aneks,sypialnia okna=" + windows.Count
            + " drzwi_klatki=" + (DX1 - DX0).ToString("F2") + "×" + DoorH.ToString("F2") + " modele=" + props + " siatki=" + siatkiM + " odpoczynek=ŁÓŻKO(ODPOCZNIJ)");

        BraxRegion.Note("mieszkanie Braxa nad NOVA: klatka (2 × " + N + " stopni po " + rise.ToString("0.000") + " m), pokój " +
                        (AX1 - AX0).ToString("0.0") + " × " + (AZ1 - AZ0).ToString("0.0") + " m na y=" + Fl + ", wyposażenie " + props +
                        ", cięte siatki " + cut + ", kolizje przycięte " + colCut);
    }

    // ---------------------------------------------------------------- pomocnicze

    static Transform Group(string name) { var t = new GameObject(name).transform; t.SetParent(root, false); return t; }

    /// <summary>Prostopadłościan z min/max; opcjonalnie otwór drzwi (ox0..ox1 do wysokości oy1) w ścianie poziomej wzdłuż x.</summary>
    static void B(Transform p, string name, float x0, float x1, float y0, float y1, float z0, float z1, Material m, bool solid,
                  float ox0 = float.NaN, float ox1 = float.NaN, float oy1 = float.NaN)
    {
        if (x1 - x0 < .005f || y1 - y0 < .005f || z1 - z0 < .005f) return;
        if (!float.IsNaN(ox0))
        {
            B(p, name + " L", x0, ox0, y0, y1, z0, z1, m, solid);
            B(p, name + " P", ox1, x1, y0, y1, z0, z1, m, solid);
            B(p, name + " nad otworem", ox0, ox1, oy1, y1, z0, z1, m, solid);
            return;
        }
        BraxRegion.Box(name, p, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), m, solid, 1f);
    }

    static void Rail(Transform p, string name, Vector3 a, Vector3 b, Material m)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        o.transform.SetParent(p, false);
        o.transform.position = (a + b) * .5f;
        o.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
        o.transform.localScale = new Vector3(.045f, .045f, (b - a).magnitude);
        o.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    static void Light(Transform p, string name, Vector3 at, float intensity, float range) { BraxRegion.Warm(p, name, at, intensity, range); }

    static GameObject Prop(Transform p, string label, Vector3 floor, float yaw, float scale, bool solid, float maxH, params string[] ids)
    {
        var src = BraxRegion.Model(ids);
        if (!src) { BraxRegion.Note("mieszkanie: brak modelu " + string.Join("/", ids)); return null; }
        var o = BraxRegion.Put(p, src, label, floor, yaw, scale, solid, maxH);
        if (o) props++;
        return o;
    }

    // Ściana od ulicy od środka (z = 36.66), z otworami dokładnie w oknach elewacji; parapet i ościeża z drewna.
    static void InnerStreetWall(Transform p, List<Rect> windows, Material wall, Material wood)
    {
        float z0 = InnerFaceZ, z1 = InnerFaceZ + .08f;
        var ws = windows.Where(w => w.xMax > AX0 + .1f && w.xMin < AX1 - .1f).OrderBy(w => w.xMin).ToList();
        float x = AX0;
        foreach (var w in ws)
        {
            float wx0 = Mathf.Max(AX0 + .1f, w.xMin), wx1 = Mathf.Min(AX1 - .1f, w.xMax);
            float wy0 = Mathf.Max(Fl + .2f, w.yMin), wy1 = Mathf.Min(Ceil - .05f, w.yMax);
            B(p, "Mieszkanie: ściana od ulicy", x, wx0, Fl, Ceil, z0, z1, wall, true);
            B(p, "Mieszkanie: ściana pod oknem", wx0, wx1, Fl, wy0, z0, z1, wall, true);
            B(p, "Mieszkanie: ściana nad oknem", wx0, wx1, wy1, Ceil, z0, z1, wall, true);
            B(p, "Mieszkanie: parapet", wx0 - .05f, wx1 + .05f, wy0 - .03f, wy0, z0 - .25f, z1 + .06f, wood, false);
            // kolizja w oknie (nie wypadniemy przez szybę elewacji)
            var c = new GameObject("Mieszkanie: okno (kolizja)").AddComponent<BoxCollider>();
            c.transform.SetParent(p, false); c.transform.position = new Vector3((wx0 + wx1) * .5f, (wy0 + wy1) * .5f, (z0 + z1) * .5f); c.size = new Vector3(wx1 - wx0, wy1 - wy0, .08f);
            x = wx1;
        }
        B(p, "Mieszkanie: ściana od ulicy", x, AX1, Fl, Ceil, z0, z1, wall, true);
    }

    // Okna 1. piętra na elewacji od ulicy: trójkąty siatek „glass” pod „NOVA preserved interior upper storeys”, skupione w prostokąty.
    static List<Rect> FacadeWindows(Transform cityRoot, float x0, float x1, float y0, float y1)
    {
        // Najpierw dokładne otwory z tej samej siatki i tego samego generatora,
        // który wycina elewację NOVA. Dzięki temu szkło ma tę samą geometrię,
        // nawet gdy Unity połączy trójkąty w batche lub zmieni nazwy materiałów.
        var projekt = BraxUlicePierzeje.OknaMieszkaniaNOVA
            .Where(w => w.xMin >= x0 && w.xMax <= x1 &&
                        w.yMin >= y0 && w.yMax <= y1 &&
                        w.width > .4f && w.height > .6f)
            .OrderBy(w => w.xMin).ToList();
        if (projekt.Count > 0)
        {
            Debug.Log("BRAXHORN_MIESZKANIE_BRAXA_OKNA_Z_ELEWACJI liczba=" + projekt.Count +
                " (dokładne otwory z generatora, bez fikcyjnych okien)");
            return projekt;
        }
        var rects = new List<Rect>();
        foreach (var mf in cityRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!BraxRegion.Under(mf.transform, "NOVA preserved interior upper storeys")) continue;
            var r = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh;
            if (!r || !mesh || !mesh.isReadable) continue;
            var mats = r.sharedMaterials;
            var v = mesh.vertices;
            for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
            {
                if (!mats[s] || !mats[s].name.ToLowerInvariant().Contains("glass")) continue;
                var idx = mesh.GetTriangles(s);
                for (int i = 0; i + 2 < idx.Length; i += 3)
                {
                    var a = mf.transform.TransformPoint(v[idx[i]]); var b = mf.transform.TransformPoint(v[idx[i + 1]]); var c = mf.transform.TransformPoint(v[idx[i + 2]]);
                    float zc = (a.z + b.z + c.z) / 3f;
                    if (Mathf.Abs(zc - 36.2f) > .45f) continue;
                    float tx0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), tx1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                    float ty0 = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), ty1 = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                    if (tx1 < x0 || tx0 > x1 || ty1 < y0 || ty0 > y1) continue;
                    rects.Add(Rect.MinMaxRect(tx0, ty0, tx1, ty1));
                }
            }
        }
        // scalanie nakładających się / stykających prostokątów (2 trójkąty szyby = 1 okno; szprosy dzielą okno na tafle)
        bool merged = true;
        while (merged)
        {
            merged = false;
            for (int i = 0; i < rects.Count && !merged; i++)
                for (int j = i + 1; j < rects.Count; j++)
                {
                    var a = rects[i]; var b = rects[j];
                    if (a.xMin - .12f <= b.xMax && b.xMin - .12f <= a.xMax && a.yMin - .12f <= b.yMax && b.yMin - .12f <= a.yMax)
                    {
                        rects[i] = Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
                        rects.RemoveAt(j); merged = true; break;
                    }
                }
        }
        // tylko okna tego piętra (dół szyby nad podłogą, góra pod sufitem) i sensownej wielkości
        var wynik = rects.Where(w => w.width > .4f && w.height > .6f &&
            w.yMin < Ceil - .6f && w.yMax > Fl + .8f).ToList();
        if (wynik.Count > 0) return wynik;

        // Nie próbuj odtwarzać nieistniejącej "Upper rear" — generator pierzei
        // usuwa tę starą fasadę przed budową mieszkania. Nie wolno wycinać
        // ściany na podstawie fikcyjnych współrzędnych.
        Debug.LogWarning("BRAXHORN_MIESZKANIE_BRAXA_OKNA_NIEGOTOWE: brak zarejestrowanych otworów w nowej elewacji NOVA; sprawdź generator pierzei");
        return wynik;
    }

    // Kolizje renowacji (front NOVA z kodu: panel, pilastry) — przycięcie jak w BraxCityMeshCut, tylko w zadanej bryle.
    static int ClipRenovationColliders(Transform cityRoot, Bounds vol)
    {
        int n = 0;
        foreach (var c in cityRoot.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!c || !c.enabled || c.isTrigger || !c.bounds.Intersects(vol)) continue;
            if (BraxRegion.Under(c.transform, "CLAUDE mieszkanie Braxa")) continue;
            if (c.GetComponentInParent<BraxCityDoor>()) continue;
            if (Quaternion.Angle(c.transform.rotation, Quaternion.identity) > .01f) continue;
            if (c.attachedRigidbody) continue;
            var b = c.bounds;
            var lo = Vector3.Max(b.min, vol.min); var hi = Vector3.Min(b.max, vol.max);
            if (hi.x - lo.x < .01f || hi.y - lo.y < .01f || hi.z - lo.z < .01f) continue;
            // reszta bryły poza przecięciem: do 6 prostopadłościanów
            void Add(Vector3 a, Vector3 z)
            {
                var s = z - a; if (s.x < .01f || s.y < .01f || s.z < .01f) return;
                var g = new GameObject(c.name + " (reszta po mieszkaniu)").AddComponent<BoxCollider>();
                g.transform.position = (a + z) * .5f; g.transform.SetParent(root, true);
                var ls = g.transform.lossyScale; g.size = new Vector3(s.x / Mathf.Max(1e-4f, ls.x), s.y / Mathf.Max(1e-4f, ls.y), s.z / Mathf.Max(1e-4f, ls.z));
            }
            Add(b.min, new Vector3(lo.x, b.max.y, b.max.z));
            Add(new Vector3(hi.x, b.min.y, b.min.z), b.max);
            Add(new Vector3(lo.x, b.min.y, b.min.z), new Vector3(hi.x, lo.y, b.max.z));
            Add(new Vector3(lo.x, hi.y, b.min.z), new Vector3(hi.x, b.max.y, b.max.z));
            Add(new Vector3(lo.x, lo.y, b.min.z), new Vector3(hi.x, hi.y, lo.z));
            Add(new Vector3(lo.x, lo.y, hi.z), new Vector3(hi.x, hi.y, b.max.z));
            c.enabled = false; n++;
        }
        return n;
    }

    static void StreetDoor(BraxCityWorld world, Material wood, Material frame)
    {
        float dw = DX1 - DX0;
        var F = Group("Drzwi od ulicy");
        B(F, "Klatka: futryna lewa", DX0 - .07f, DX0, Street, Street + DoorH + .07f, FrontZ - .14f, SZ0 + .02f, frame, true);
        B(F, "Klatka: futryna prawa", DX1, DX1 + .07f, Street, Street + DoorH + .07f, FrontZ - .14f, SZ0 + .02f, frame, true);
        B(F, "Klatka: nadproże", DX0 - .07f, DX1 + .07f, Street + DoorH, Street + DoorH + .07f, FrontZ - .14f, SZ0 + .02f, frame, true);
        B(F, "Klatka: próg", DX0, DX1, Street, Street + .02f, FrontZ - .2f, SZ0, frame, false);
        var entrance = new GameObject("Source-kit working entrance KLATKA").transform;
        entrance.SetParent(F, false); entrance.position = new Vector3((DX0 + DX1) * .5f, Street, FrontZ); entrance.rotation = Quaternion.identity;
        var hinge = new GameObject("Actual hinged source door leaf").transform;
        hinge.SetParent(entrance, false); hinge.localPosition = new Vector3(-dw * .5f, 0, 0);
        BraxCityArchitecture.Box("Klatka: skrzydło drzwi", hinge, new Vector3(dw * .5f, DoorH * .5f, 0), new Vector3(dw - .02f, DoorH - .02f, .05f), wood);
        BraxCityArchitecture.Box("Klatka: klamka", hinge, new Vector3(dw - .12f, 1.05f, -.05f), new Vector3(.12f, .025f, .025f), frame);
        BraxCityArchitecture.Box("Klatka: numer domu", hinge, new Vector3(dw * .5f, 1.85f, -.035f), new Vector3(.16f, .1f, .01f), frame);
        foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        var col = hinge.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(dw * .5f, DoorH * .5f, 0); col.size = new Vector3(dw, DoorH, .08f);
        var door = entrance.gameObject.AddComponent<BraxCityDoor>();
        door.id = "BRAX_FLAT_STAIRS"; door.label = "KLATKA"; door.leaves = new[] { hinge }; door.sides = new[] { 1f };
        if (world) world.doors = world.doors.Concat(new[] { door }).ToArray();
        // pkt 46: szyba w skrzydle (górna część), domofon z panelem, numer, daszek szklany na wspornikach, kinkiet — na licu
        // okładziny NOVA (z 35.95) w polu między opaską portalu (x 34.48) a lizeną (x 34.94); nic nie leży na powierzchni
        BraxCityArchitecture.Box("Klatka: szyba w bramie", hinge, new Vector3(dw * .5f, DoorH * .72f, -.028f), new Vector3(dw * .55f, DoorH * .32f, .006f), BraxRegion.Fitting("Klatka: szyba bramy", new Color(.7f, .8f, .85f), .05f, .5f));
        W.Start(F); W.Wnetrze = false;
        float zc = 35.95f;
        var ciemny = BraxRegion.Fitting("Klatka: domofon (grafit)", new Color(.08f, .08f, .09f), .35f, .6f);
        var stalD = BraxRegion.Fitting("Klatka: stal nierdzewna domofonu", new Color(.75f, .76f, .78f), .2f, .9f);
        W.MM(stalD, 34.55f, 34.82f, Street + 1.15f, Street + 1.62f, zc - .025f, zc);
        W.MM(ciemny, 34.6f, 34.77f, Street + 1.48f, Street + 1.58f, zc - .03f, zc - .025f);                       // głośnik (kratka)
        for (int i = 0; i < 3; i++) W.Walec(stalD, new Vector3(34.685f, Street + 1.22f + i * .08f, zc - .025f), .018f, .012f, 10);
        W.MM(ciemny, 34.6f, 34.77f, Street + 1.3f, Street + 1.36f, zc - .028f, zc - .025f);                       // wyświetlacz
        W.MM(stalD, 34.55f, 34.82f, Street + 1.75f, Street + 1.9f, zc - .03f, zc);                                // tabliczka z numerem (nad domofonem)
        W.Napis("7", new Vector3(34.685f, Street + 1.825f, zc - .032f), Vector3.back, .1f, new Color(.1f, .1f, .1f));
        float yd = Street + DoorH + .32f;                                                                          // daszek nad bramą
        W.MM(stalD, 32.85f, 34.5f, yd, yd + .05f, zc - .8f, zc - .74f);
        W.MM(BraxRegion.Fitting("Klatka: szkło daszku", new Color(.75f, .85f, .88f), .05f, .4f), 32.88f, 34.47f, yd + .05f, yd + .065f, zc - .8f, zc);
        foreach (float x in new[] { 32.95f, 34.4f })
        {
            W.Rura(stalD, new Vector3(x, yd + .06f, zc - .78f), new Vector3(x, yd + .55f, zc - .01f), .012f, 8);      // cięgno
            W.Walec(stalD, new Vector3(x, yd + .55f, zc - .02f), .03f, .02f, 10);
            W.Rura(stalD, new Vector3(x, yd + .02f, zc - .76f), new Vector3(x, yd + .02f, zc - .01f), .015f, 8);      // wspornik
        }
        W.MM(ciemny, 34.66f, 34.78f, Street + 2.45f, Street + 2.7f, zc - .03f, zc);                               // kinkiet
        W.Walec(BraxRegionMeble.Swiecacy("Klatka: klosz kinkietu bramy", new Color(1f, .93f, .8f), 1.2f), new Vector3(34.72f, Street + 2.47f, zc - .1f), .05f, .2f, 14);
        W.Zapisz("brama_klatki");
        Light(F, "Klatka: kinkiet przy bramie", new Vector3(34.72f, Street + 2.6f, zc - .4f), .5f, 4f);
    }

    static void FlatDoor(BraxCityWorld world, Material wood, Material steel, float z0, float z1)
    {
        float dw = z1 - z0, h = 2.1f * BraxMiastoA.K - .02f;
        var F = Group("Drzwi do mieszkania");
        var entrance = new GameObject("Source-kit working entrance MIESZKANIE").transform;
        entrance.SetParent(F, false); entrance.position = new Vector3(AX0, Fl, (z0 + z1) * .5f); entrance.rotation = Quaternion.Euler(0, 90f, 0);
        var hinge = new GameObject("Actual hinged source door leaf").transform;
        hinge.SetParent(entrance, false); hinge.localPosition = new Vector3(-dw * .5f, 0, 0);
        BraxCityArchitecture.Box("Mieszkanie: skrzydło drzwi", hinge, new Vector3(dw * .5f, h * .5f, 0), new Vector3(dw - .02f, h - .02f, .05f), wood);
        BraxCityArchitecture.Box("Mieszkanie: klamka", hinge, new Vector3(dw - .12f, 1.0f, -.05f), new Vector3(.12f, .025f, .025f), steel);
        BraxCityArchitecture.Box("Mieszkanie: wizjer", hinge, new Vector3(dw * .5f, 1.55f, -.03f), new Vector3(.03f, .03f, .01f), steel);
        foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        var col = hinge.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(dw * .5f, h * .5f, 0); col.size = new Vector3(dw, h, .08f);
        var door = entrance.gameObject.AddComponent<BraxCityDoor>();
        door.id = "BRAX_FLAT"; door.label = "MIESZKANIE BRAXA"; door.leaves = new[] { hinge }; door.sides = new[] { 1f };
        if (world) world.doors = world.doors.Concat(new[] { door }).ToArray();
    }

    // Worek treningowy: kopia „Original training bag” z siłowni (ten sam model i materiał), dopasowany do sufitu 2.85 m.
    static void Bag(Transform p, Vector3 ceilingPoint)
    {
        var src = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                  .FirstOrDefault(t => t.name.StartsWith("Original training bag"));
        if (!src) { BraxRegion.Note("mieszkanie: brak worka z siłowni do skopiowania"); return; }
        var o = UnityEngine.Object.Instantiate(src.gameObject, p, true);
        o.name = "Mieszkanie: worek treningowy (kopia z siłowni)";
        foreach (var mb in o.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(mb);
        foreach (var rb in o.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
        var b = BraxRegion.Bounds(o);
        float fit = (Ceil - Fl - .35f) / Mathf.Max(.5f, b.size.y);
        if (fit < 1f) o.transform.localScale *= fit;
        b = BraxRegion.Bounds(o);
        o.transform.position += new Vector3(ceilingPoint.x - b.center.x, ceilingPoint.y - b.max.y, ceilingPoint.z - b.center.z);
        foreach (var t in o.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        props++;
    }

    /// <summary>Ościeżnica (opaski po obu stronach ściany + nadproże) i opcjonalnie skrzydło uchylone do pomieszczenia z klamkami.
    /// wzdluzX: otwór w ścianie wzdłuż x na płaszczyźnie z = plaszczyzna; inaczej w ścianie wzdłuż z na x = plaszczyzna.</summary>
    static void Oscieznica(Transform p, Material drewno, Material stal, float plaszczyzna, float a0, float a1, bool wzdluzX, float y0, float h, bool skrzydlo = true)
    {
        float t = .06f, g = .085f;
        Vector3 P(float u, float yy, float w) => wzdluzX ? new Vector3(u, yy, plaszczyzna + w) : new Vector3(plaszczyzna + w, yy, u);
        void Bx(float u0, float u1, float yy0, float yy1) { var a = P(u0, yy0, -g); var b = P(u1, yy1, g); W.MM(drewno, Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x), yy0, yy1, Mathf.Min(a.z, b.z), Mathf.Max(a.z, b.z)); }
        Bx(a0 - t, a0, y0, y0 + h + t); Bx(a1, a1 + t, y0, y0 + h + t); Bx(a0 - t, a1 + t, y0 + h, y0 + h + t);
        if (!skrzydlo) return;
        var os = P(a0 + .02f, y0, g); var kier = wzdluzX ? new Vector3(.4f, 0, .92f) : new Vector3(.92f, 0, .4f);
        float dl = a1 - a0 - .03f;
        W.OBox(drewno, os + kier * (dl * .5f) + Vector3.up * (h * .5f), new Vector3(.04f, h - .02f, dl), Quaternion.LookRotation(kier) * Quaternion.Euler(0, 90, 0));
        var kl = os + kier * (dl - .08f) + Vector3.up * 1.05f * BraxMiastoA.K; var bok = Vector3.Cross(Vector3.up, kier).normalized;
        foreach (float s in new[] { -1f, 1f }) { W.Rura(stal, kl + bok * s * .025f, kl + bok * s * .06f, .01f, 6); W.Rura(stal, kl + bok * s * .06f, kl + bok * s * .06f - kier * .12f, .009f, 6); }
    }

    /// <summary>Krzesło (nogi stalowe, siedzisko i oparcie z listew) — siedzący przodem do stołu (twarz).</summary>
    static void Krzeslo(Material drewno, Material stal, Vector3 podloga, Vector3 twarz)
    {
        float k = BraxMiastoA.K, hs = .45f * k, sw = .44f * k * .9f;
        var f = twarz.normalized; var r = Vector3.Cross(Vector3.up, f).normalized; var q = Quaternion.LookRotation(f);
        foreach (float a in new[] { -1f, 1f }) foreach (float b in new[] { -1f, 1f })
            W.Rura(stal, podloga + r * a * (sw * .42f) + f * b * (sw * .42f), podloga + r * a * (sw * .42f) + f * b * (sw * .42f) + Vector3.up * (hs - .04f), .015f, 8);
        W.OBox(drewno, podloga + Vector3.up * (hs - .02f), new Vector3(sw, .04f, sw), q, true);
        var tyl = podloga - f * (sw * .45f);
        foreach (float a in new[] { -1f, 1f }) W.Rura(stal, tyl + r * a * (sw * .42f) + Vector3.up * hs, tyl + r * a * (sw * .42f) - f * .04f + Vector3.up * (hs + .42f * k), .013f, 8);
        for (int i = 0; i < 3; i++) W.OBox(drewno, tyl - f * .02f + Vector3.up * (hs + .18f * k + i * .1f * k), new Vector3(sw * .9f, .05f, .02f), q * Quaternion.Euler(-8f, 0, 0));
    }
}

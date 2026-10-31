using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
    const float DX0 = 33.15f, DX1 = 34.10f, DoorH = 2.25f, FrontZ = 36.0f;
    // mieszkanie
    const float AX0 = 37.98f, AX1 = 45.2f, AZ0 = 36.45f, AZ1 = 43.4f;
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
        float f1x0 = SX0 + .05f, f1x1 = f1x0 + 1.1f, f2x0 = f1x1 + .2f, f2x1 = f2x0 + 1.1f;
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
        B(K, "Klatka: ścianka między biegami", f1x1, f2x0, Street, Ceil, zBottom, zTop, peel, true);
        B(K, "Klatka: ściana komórki", f2x1, f2x1 + .1f, Street, Ceil, zBottom, SZ1, peel, true);
        B(K, "Klatka: ściana komórki (przód)", f2x1 + .1f, SX1 - .06f, Street, Fl - .28f, zBottom - .06f, zBottom, peel, true);
        B(K, "Klatka: komórka (drzwi zamknięte)", f2x1 + .4f, f2x1 + 1.2f, Street, Street + 2.0f, zBottom - .09f, zBottom - .06f, oldWood, true);
        B(K, "Klatka: korytarz piętra nad komórką", f2x1, SX1, Fl - .28f, Fl, zBottom, SZ1 - .06f, concrete, true);
        // poręcz na ścianie biegu 1 (rdzawa stal, pochylona wzdłuż biegu)
        Rail(K, "Klatka: poręcz biegu 1", new Vector3(SX0 + .1f, Street + .95f, zBottom), new Vector3(SX0 + .1f, yMid + .95f, zTop), rust);
        Rail(K, "Klatka: poręcz biegu 2", new Vector3(f2x1 - .02f, yMid + .95f, zTop), new Vector3(f2x1 - .02f, Fl + .95f, zBottom), rust);
        // skrzynki na listy, licznik, żarówka
        B(K, "Klatka: skrzynki na listy", SX1 - .5f, SX1 - .08f, Street + 1.2f, Street + 1.75f, SZ0 + .2f, SZ0 + .9f, rust, false);
        Light(K, "Klatka: goła żarówka (parter)", new Vector3((SX0 + SX1) * .5f + 1.2f, Fl - .6f, SZ0 + .6f), 1.0f, 7f);
        Light(K, "Klatka: goła żarówka (piętro)", new Vector3((f2x0 + SX1) * .5f, Ceil - .35f, (SZ0 + zBottom) * .5f), .9f, 6f);

        // drzwi od ulicy (stare drewniane, w lewym panelu frontu NOVA)
        StreetDoor(world, oldWood, rust);

        // ---------------- 4) mieszkanie ----------------
        var A = Group("Pokój");
        B(A, "Mieszkanie: podłoga (linoleum)", AX0, AX1, Fl - .26f, Fl, AZ0, AZ1, lino, true);
        B(A, "Mieszkanie: sufit", AX0, AX1, Ceil, Ceil + .12f, AZ0, AZ1, plaster, true);
        B(A, "Mieszkanie: ściana tylna", AX0, AX1, Fl, Ceil, AZ1 - .1f, AZ1, paper, true);
        B(A, "Mieszkanie: ściana wschodnia", AX1 - .1f, AX1, Fl, Ceil, AZ0, AZ1, paper, true);
        // ściana od klatki z drzwiami (otwór z 36.55…37.45, wys. 2.1)
        float dz0 = SZ0 + .1f, dz1 = dz0 + .9f;
        B(A, "Mieszkanie: ściana od klatki", AX0 - .06f, AX0 + .06f, Fl, Ceil, dz1, AZ1, peel, true);
        B(A, "Mieszkanie: ściana od klatki (przy oknie)", AX0 - .06f, AX0 + .06f, Fl, Ceil, AZ0, dz0, peel, true);
        B(A, "Mieszkanie: nadproże drzwi", AX0 - .06f, AX0 + .06f, Fl + 2.1f, Ceil, dz0, dz1, peel, true);
        InnerStreetWall(A, windows, paper, oldWood);
        FlatDoor(world, oldWood, steel, dz0, dz1);

        // łazienka w rogu (x 43.0…45.2, z 40.6…43.4)
        float bx = 43.0f, bz = 40.6f;
        B(A, "Łazienka: ściana", bx - .06f, bx + .06f, Fl, Ceil, bz + 1.0f, AZ1 - .1f, tiles, true);
        B(A, "Łazienka: ściana (przód)", bx, AX1 - .1f, Fl, Ceil, bz - .06f, bz + .06f, tiles, true);
        B(A, "Łazienka: nadproże", bx - .06f, bx + .06f, Fl + 2.05f, Ceil, bz + .06f, bz + 1.0f, tiles, false);
        B(A, "Łazienka: kafle na podłodze", bx + .06f, AX1 - .1f, Fl, Fl + .012f, bz + .06f, AZ1 - .1f, tiles, false);
        B(A, "Łazienka: kafle na ścianie", AX1 - .12f, AX1 - .1f, Fl, Fl + 1.6f, bz + .06f, AZ1 - .1f, tiles, false);
        B(A, "Łazienka: brodzik", AX1 - 1.0f, AX1 - .12f, Fl, Fl + .12f, AZ1 - 1.0f, AZ1 - .12f, tiles, true);

        // wyposażenie (Poly Haven) — pozycje w pokoju x 38.1…43.0, z 36.7…43.3
        float y = Fl;
        Prop(A, "Mieszkanie: stare łóżko", new Vector3(44.0f, y, 38.2f), 90f, 1f, true, 0f, "old_bed_frame");
        Prop(A, "Mieszkanie: szafa", new Vector3(42.4f, y, AZ1 - .45f), 180f, 1f, true, 2.1f, "painted_wooden_cabinet_02", "modern_wooden_cabinet", "drawer_cabinet");
        var table = Prop(A, "Mieszkanie: stół", new Vector3(40.2f, y, 40.6f), 0f, 1f, true, .8f, "WoodenTable_03", "WoodenTable_02", "WoodenTable_01");
        Prop(A, "Mieszkanie: krzesło", new Vector3(40.2f, y, 41.45f), 180f, 1f, true, 1.0f, "SchoolChair_01", "wooden_stool_01", "metal_stool_01");
        float tableTop = table ? BraxRegion.Bounds(table).max.y : y + .78f;
        Prop(A, "Mieszkanie: budzik na stole", new Vector3(39.95f, tableTop, 40.45f), 20f, 1f, false, .14f, "alarm_clock_01");
        var crate = Prop(A, "Mieszkanie: skrzynka pod telewizor", new Vector3(42.0f, y, 37.25f), 0f, 1f, true, .45f, "plastic_crate_01", "wooden_crate_01");
        float tvY = crate ? BraxRegion.Bounds(crate).max.y : y + .45f;
        Prop(A, "Mieszkanie: telewizor kineskopowy", new Vector3(42.0f, tvY, 37.25f), 0f, 1f, false, .55f, "Television_01");
        // aneks kuchenny przy ścianie od klatki
        B(A, "Aneks: szafka", AX0 + .07f, AX0 + .67f, y, y + .86f, 38.4f, 40.0f, oldWood, true);
        B(A, "Aneks: blat", AX0 + .05f, AX0 + .7f, y + .86f, y + .9f, 38.38f, 40.02f, tiles, false);
        Prop(A, "Aneks: kuchenka elektryczna", new Vector3(AX0 + .4f, y + .9f, 38.75f), 90f, 1f, false, .3f, "electric_stove");
        Prop(A, "Aneks: mikrofala", new Vector3(AX0 + .38f, y + .9f, 39.55f), 90f, 1f, false, .32f, "vintage_microwave");
        Prop(A, "Aneks: czajnik", new Vector3(AX0 + .35f, y + .9f, 39.15f), 90f, 1f, false, .25f, "vintage_electric_kettle");
        B(A, "Aneks: stara lodówka", AX0 + .07f, AX0 + .67f, y, y + 1.45f, 40.1f, 40.7f, rust, true);
        // regał z konserwami i kartony
        var shelf = Prop(A, "Mieszkanie: regał", new Vector3(AX0 + .3f, y, 42.6f), 90f, 1f, true, 1.8f, "Shelf_01", "steel_frame_shelves_01", "worn_metal_rack");
        if (shelf) { var sb = BraxRegion.Bounds(shelf); Prop(A, "Mieszkanie: konserwy", new Vector3(sb.center.x, sb.min.y + sb.size.y * .55f, sb.center.z), 90f, 1f, false, .2f, "russian_food_cans_01"); }
        Prop(A, "Mieszkanie: karton", new Vector3(40.6f, y, AZ1 - .5f), 12f, 1f, true, .45f, "cardboard_box_01");
        Prop(A, "Mieszkanie: karton 2", new Vector3(41.15f, y, AZ1 - .45f), -8f, .85f, true, .4f, "cardboard_box_01");
        Prop(A, "Mieszkanie: radio na podłodze", new Vector3(43.3f, y, 39.9f), -30f, 1f, false, .3f, "boombox");
        // puste półki na przyszłe trofea (kariera zaczyna się tutaj)
        for (int i = 0; i < 2; i++) B(A, "Mieszkanie: pusta półka na trofea " + (i + 1), 40.0f, 41.6f, y + 1.55f + i * .45f, y + 1.58f + i * .45f, AZ1 - .32f, AZ1 - .1f, oldWood, false);
        // worek treningowy w kącie (kopia worka z siłowni, przeskalowana pod niski sufit)
        Bag(A, new Vector3(39.0f, Ceil, 37.4f));
        // goła żarówka na kablu
        Prop(A, "Mieszkanie: goła żarówka", new Vector3(41.0f, Ceil - .45f, 39.6f), 0f, 1f, false, .14f, "lightbulb_01");
        B(A, "Mieszkanie: kabel żarówki", 40.99f, 41.01f, Ceil - .3f, Ceil, 39.59f, 39.61f, steel, false);
        Light(A, "Mieszkanie: światło żarówki", new Vector3(41.0f, Ceil - .5f, 39.6f), 1.15f, 7.5f);
        Light(A, "Łazienka: światło", new Vector3(44.1f, Ceil - .4f, 42.0f), .7f, 4f);

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
        return rects.Where(w => w.width > .4f && w.height > .6f && w.yMin < Ceil - .6f && w.yMax > Fl + .8f).ToList();
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
    }

    static void FlatDoor(BraxCityWorld world, Material wood, Material steel, float z0, float z1)
    {
        float dw = z1 - z0, h = 2.08f;
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
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — „tyły siłowni” (decyzja właściciela 31.10): teren za siłownią od A do Z.
//
// Stan w oryginalnym mieście (Unity, po lustrze X): południe od z=−6 nie ma żadnego podłoża; stoją tam dwa
// wieżowce 43,5 m: A = CityMesh_185 (x 22..58, z −49..−35), B = CityMesh_188 (x −54..−18, z −52..−36),
// niski blok 186/187 (x −13..17, z −55..−37, 28 m) i wiadukt (x −70.7..−61.7), który urywa się na z=−14.
//
// Tu: 1) podłoże: trawnik + plac z granitu między siłownią a wieżowcami, chodniki wokół budynków,
//        drzewa, ławki, latarnie, kosze, donice;
//     2) wieżowiec A użytkowy: wejście z daszkiem, hol, winda na piętro biurowe w połowie wysokości
//        (21 m), piętro za przezroczystą szybą: recepcja, open space, gabinet, sala konferencyjna,
//        kuchnia; pas walki 12 m przy oknach (arena misji);
//     3) wieżowiec B: uczciwe wejście — daszek, szklane drzwi (działają), mały hol z recepcją;
//     4) wiadukt przedłużony na południe (6 przęseł) i zakończony przyczółkiem z portalem tunelu
//        w zalesionym wzgórzu; pod łukami warsztaty i garaże z bramami rolowanymi.
public static class BraxRegionBackyard
{
    // Wieżowce (oryginalne siatki)
    static readonly Bounds TowerA = Bx(22f, 58f, 0f, 43.5f, -49f, -35f);
    static readonly Bounds TowerB = Bx(-54f, -18f, 0f, 43.5f, -52f, -36f);
    static readonly Bounds LowBlock = Bx(-13f, 17f, 0f, 28.5f, -55f, -37f);
    const float ZoneX0 = -75f, ZoneX1 = 66f, ZoneZ0 = -66f, ZoneZ1 = -6f;
    const float Paving = .15f, Lawn = .12f;
    const float OfficeY = 21.15f;            // piętro biurowe (6 kondygnacji po 3,5 m)
    const float ViaductEndZ = -14f, PortalZ = -74f;

    static Bounds Bx(float x0, float x1, float y0, float y1, float z0, float z1)
    { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    static Material granite, concrete, plaster, brick, wood, steelM, glassM, darkGlass, lawnMat, ceiling, rubber, rockM;
    static Transform root;
    static int trees, props, doors;

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        root = BraxRegion.Root(cityRoot, "CLAUDE tyły siłowni: plac, wieżowce, wiadukt");
        trees = props = doors = 0;
        Materials();
        Physics.SyncTransforms();
        Ground();
        Viaduct();
        TowerEntranceB(world);
        TowerAOffices(world);
        Greenery();
        BraxRegion.Note("tyły siłowni: drzew " + trees + ", dodatków " + props + ", drzwi " + doors);
    }

    static Material Pbr(string name, float metres, Color? tint, params string[] slugs)
    {
        try { return BraxRegion.Pbr(name, metres, tint, slugs); }
        catch (Exception e) { BraxRegion.Note("tyły: brak tekstur " + name + ": " + e.Message); return null; }
    }

    static void Materials()
    {
        granite = Pbr("Tyły: granit placu", 1.2f, null, "granite_tile_02", "floor_tiles_06", "square_tiles") ?? BraxCityMaterials.Surface("Tyły: granit", new Color(.52f, .52f, .50f), .85f);
        concrete = Pbr("Tyły: beton", 2f, null, "concrete_wall_008", "concrete_floor_02") ?? BraxCityMaterials.Surface("Tyły: beton", new Color(.55f, .54f, .50f), .9f);
        plaster = Pbr("Tyły: tynk wnętrz", 1.5f, new Color(.93f, .92f, .89f), "white_plaster_02", "rectangular_facade_tiles") ?? BraxCityMaterials.Surface("Tyły: tynk", new Color(.9f, .89f, .86f), .9f);
        brick = Pbr("Tyły: cegła warsztatów", 3f, null, "brick_wall_001", "red_brick_03", "brick_wall_02") ?? BraxCityMaterials.Surface("Tyły: cegła", new Color(.55f, .32f, .25f), .9f);
        wood = Pbr("Tyły: drewno", 1f, null, "wood_planks", "wooden_planks", "dark_wood") ?? BraxCityMaterials.Surface("Tyły: drewno", new Color(.5f, .38f, .26f), .8f);
        rockM = Pbr("Tyły: skała wzgórza", 4f, null, "rock_wall_08", "rock_face_03", "aerial_rocks_02", "concrete_wall_008") ?? concrete;
        steelM = BraxRegion.Fitting("Tyły: stal czarna", new Color(.14f, .15f, .16f), .45f, .7f);
        glassM = BraxRegion.Fitting("Tyły: szkło", BraxRegion.Glass, .12f, .05f);
        darkGlass = BraxRegion.Fitting("Tyły: szyba biurowa", new Color(.55f, .68f, .74f, .30f), .10f, .05f);
        ceiling = BraxCityMaterials.Surface("Tyły: sufit jasny", new Color(.9f, .9f, .88f), .95f);
        rubber = BraxCityMaterials.Surface("Tyły: guma", new Color(.12f, .12f, .12f), .9f);
        lawnMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/ClaudeGround/Ziemia__prawdziwa_trawa_Grass004.mat");
        if (!lawnMat) { try { lawnMat = BraxClaudeGround.Grass(); } catch (Exception) { lawnMat = BraxCityMaterials.Surface("Tyły: trawa", new Color(.35f, .48f, .22f), .95f); } }
    }

    static GameObject Box(string n, Vector3 at, Vector3 size, Material m, bool solid = false, float metres = 1f)
        => BraxCityArchitecture.Box(n, root, at, size, m, solid, metres);
    static GameObject Box(string n, Transform parent, Vector3 at, Vector3 size, Material m, bool solid = false, float metres = 1f)
        => BraxCityArchitecture.Box(n, parent, at, size, m, solid, metres);
    static void Slab(string n, float x0, float x1, float z0, float z1, float top, float thick, Material m, float metres = 1f)
        => Box(n, new Vector3((x0 + x1) * .5f, top - thick * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, thick, z1 - z0), m, true, metres);

    // ---------------------------------------------------------------- 1) podłoże i plac
    static void Ground()
    {
        // Trawnik bazowy na całej strefie (pod budynkami i tak niewidoczny), z kolizją.
        Slab("Tyły: trawnik bazowy", ZoneX0, ZoneX1, ZoneZ0, ZoneZ1, Lawn, .24f, lawnMat);
        // Plac z granitu między siłownią (z=−13) a wieżowcami (z=−35/−36): x −58..60, z −36..−13.
        Slab("Plac: granit", -58f, 60f, -36f, -13f, Paving, .06f, granite);
        // Chodniki 3 m wokół wieżowców i niskiego bloku + dojścia do promenady (wschód) i pod wiadukt (zachód).
        Ring(TowerA, 3f); Ring(TowerB, 3f); Ring(LowBlock, 3f);
        Slab("Chodnik: do promenady", 58f, 66f, -13f, -6f, Paving, .06f, granite);
        Slab("Chodnik: pod wiadukt", -75f, -58f, -16f, -6f, Paving, .06f, granite);
        Slab("Chodnik: wzdłuż wiaduktu", -61.5f, -58f, -74f, -16f, Paving, .06f, granite);
        // Krawężniki wokół placu (granit, 12 cm nad trawą).
        foreach (var e in new[] { new Vector4(-58f, 60f, -36f, -36f), new Vector4(-58f, 60f, -13f, -13f) })
            Box("Plac: krawężnik", new Vector3((e.x + e.y) * .5f, Lawn + .06f, e.z), new Vector3(e.y - e.x, .12f, .25f), concrete, true);
        // Kwatery trawnika na placu (trawa w granitowych obrzeżach) — pod drzewa.
        foreach (var q in LawnQuads())
        {
            Slab("Plac: kwatera trawnika", q.xMin, q.xMax, q.yMin, q.yMax, Paving + .01f, .04f, lawnMat);
            Box("Plac: obrzeże", new Vector3(q.center.x, Paving + .04f, q.yMin), new Vector3(q.width + .3f, .08f, .15f), concrete, true);
            Box("Plac: obrzeże", new Vector3(q.center.x, Paving + .04f, q.yMax), new Vector3(q.width + .3f, .08f, .15f), concrete, true);
            Box("Plac: obrzeże", new Vector3(q.xMin, Paving + .04f, q.center.y), new Vector3(.15f, .08f, q.height), concrete, true);
            Box("Plac: obrzeże", new Vector3(q.xMax, Paving + .04f, q.center.y), new Vector3(.15f, .08f, q.height), concrete, true);
        }
    }

    static IEnumerable<Rect> LawnQuads()
    {
        yield return new Rect(-50f, -31f, 20f, 10f);   // x −50..−30, z −31..−21 (przed wieżowcem B)
        yield return new Rect(-10f, -32f, 20f, 10f);   // przed niskim blokiem
        yield return new Rect(28f, -31f, 20f, 10f);    // przed wieżowcem A
    }

    static void Ring(Bounds b, float w)
    {
        // bez pasa północnego: tam leży plac (ta sama wysokość = migotanie)
        Slab("Chodnik wokół budynku", b.min.x - w, b.max.x + w, b.min.z - w, b.min.z, Paving, .06f, granite);
        Slab("Chodnik wokół budynku", b.min.x - w, b.min.x, b.min.z, b.max.z, Paving, .06f, granite);
        Slab("Chodnik wokół budynku", b.max.x, b.max.x + w, b.min.z, b.max.z, Paving, .06f, granite);
    }

    // ---------------------------------------------------------------- 2) zieleń i mała architektura
    static void Greenery()
    {
        var treeModels = new List<GameObject>();
        foreach (var id in new[] { "island_tree_03", "pine_tree_01", "fir_tree_01", "jacaranda_tree", "island_tree_01" }) { var m = BraxRegion.Model(id); if (m) treeModels.Add(m); }
        var shrubs = new List<GameObject>();
        foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03", "park_shrub" }) { var m = BraxRegion.Model(id); if (m) shrubs.Add(m); }
        var bench = BraxRegion.Model("painted_wooden_bench", "modular_street_seating");
        var lamp = BraxRegion.Model("street_lamp_02", "street_lamp_01");
        var bin = BraxRegion.Model("metal_trash_can");
        var planter = BraxRegion.Model("planter_box_02", "planter_box_03", "planter_box_01");
        Physics.SyncTransforms();
        // drzewa w kwaterach placu (3 na kwaterę), za wieżowcami (południe) i wzdłuż wiaduktu
        var spots = new List<Vector3>();
        foreach (var q in LawnQuads()) foreach (float t in new[] { .2f, .5f, .8f }) spots.Add(new Vector3(q.xMin + q.width * t, 0, q.center.y + BraxRegion.Range(-2.5f, 2.5f)));
        for (float x = -56f; x <= 60f; x += 7f) spots.Add(new Vector3(x + BraxRegion.Range(-1.5f, 1.5f), 0, -59f + BraxRegion.Range(-3f, 2f)));
        for (float z = -70f; z <= -18f; z += 8f) spots.Add(new Vector3(-57f + BraxRegion.Range(-1f, 1f), 0, z + BraxRegion.Range(-1.5f, 1.5f)));
        foreach (var s in spots)
        {
            RaycastHit h; if (!BraxRegion.Ground(s.x, s.z, -1f, 2f, out h)) continue;
            if (!BraxRegion.Free(h.point + Vector3.up * 5f, new Vector3(2.4f, 8f, 2.4f))) continue;
            if (treeModels.Count == 0) break;
            var o = BraxRegion.Put(root, BraxRegion.Pick(treeModels), "Tyły: drzewo", h.point, BraxRegion.Range(0, 360), BraxRegion.Range(.85f, 1.1f), false, 13f);
            if (!o) continue;
            var c = new GameObject("Tyły: pień kolizja").AddComponent<CapsuleCollider>();
            c.transform.SetParent(root, false); c.transform.position = h.point + Vector3.up * 1.5f; c.radius = .32f; c.height = 3f;
            trees++;
            if (shrubs.Count > 0 && trees % 2 == 0)
            {
                var sp = h.point + new Vector3(BraxRegion.Range(-2.2f, 2.2f), 0, BraxRegion.Range(-2.2f, 2.2f));
                RaycastHit h2; if (BraxRegion.Ground(sp.x, sp.z, -1f, 2f, out h2) && BraxRegion.Free(h2.point + Vector3.up, new Vector3(1.6f, 1.4f, 1.6f)))
                    if (BraxRegion.Put(root, BraxRegion.Pick(shrubs), "Tyły: krzew", h2.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.25f), false, 2.2f, .04f)) props++;
            }
        }
        // ławki wzdłuż placu (z=−16.5 i z=−33), latarnie w rogach kwater, kosze, donice przy wejściach
        if (bench)
            foreach (float x in new[] { -44f, -36f, -4f, 4f, 34f, 42f })
                foreach (float z in new[] { -16.5f, -33.5f })
                    if (BraxRegion.Free(new Vector3(x, Paving + .6f, z), new Vector3(2f, 1f, .9f)))
                        if (BraxRegion.Put(root, bench, "Plac: ławka", new Vector3(x, Paving, z), z < -25f ? 0f : 180f, 1f, true, 1.0f)) props++;
        if (lamp)
            foreach (float x in new[] { -52f, -28f, -12f, 14f, 26f, 50f })
                foreach (float z in new[] { -19f, -34f })
                    if (BraxRegion.Free(new Vector3(x, Paving + 2.5f, z), new Vector3(.8f, 5f, .8f)))
                        if (BraxRegion.Put(root, lamp, "Plac: latarnia", new Vector3(x, Paving, z), 0f, 1f, true, 6.5f)) props++;
        if (bin)
            foreach (var at in new[] { new Vector3(-40f, Paving, -15f), new Vector3(0f, Paving, -15f), new Vector3(38f, Paving, -15f), new Vector3(-30f, Paving, -34.5f), new Vector3(46f, Paving, -33.5f) })
                if (BraxRegion.Free(at + Vector3.up * .5f, new Vector3(.7f, .9f, .7f)))
                    if (BraxRegion.Put(root, bin, "Plac: kosz", at, BraxRegion.Range(0, 360), 1f, true, .9f)) props++;
        if (planter)
            foreach (var at in new[] { new Vector3(36f, Paving, -33.2f), new Vector3(44f, Paving, -33.2f), new Vector3(-40f, Paving, -34.2f), new Vector3(-32f, Paving, -34.2f) })
                if (BraxRegion.Free(at + Vector3.up * .5f, new Vector3(1.4f, 1f, 1.0f)))
                    if (BraxRegion.Put(root, planter, "Plac: donica przy wejściu", at, 0f, 1f, true, 1.0f)) props++;
    }

    // ---------------------------------------------------------------- 3) wiadukt: przedłużenie, portal, warsztaty
    static void Viaduct()
    {
        var steel = BraxCityMaterials.Surface("Viaduct weathered graphite steel", new Color(.19f, .21f, .21f), .76f, .48f);
        var abut = BraxCityMaterials.Surface("Viaduct stone abutments", new Color(.45f, .44f, .40f), .96f);
        var vroot = new GameObject("Wiadukt: przedłużenie na południe").transform; vroot.SetParent(root, false);
        // Sześć przęseł po 10 m (z −74..−14), ta sama konstrukcja co BraxCityGround.Viaduct.
        for (float z = PortalZ; z < ViaductEndZ; z += 10f)
        {
            var segment = new GameObject("Viaduct bay " + z).transform; segment.SetParent(vroot, false);
            var frame = new BraxCitySystemMesh();
            foreach (float x in new[] { -70.7f, -61.7f })
            {
                frame.Box(new Vector3(x, 8.7f, z + 5), new Vector3(.13f, 1.05f, 10));
                frame.Box(new Vector3(x, 8.2f, z + 5), new Vector3(.52f, .13f, 10));
                frame.Box(new Vector3(x, 9.2f, z + 5), new Vector3(.52f, .13f, 10));
                for (float k = z; k < z + 10; k += 1)
                {
                    frame.Box(new Vector3(x, 8.7f, k), new Vector3(.32f, 1.05f, .085f));
                    foreach (float y in new[] { 8.37f, 9.03f }) frame.Tube(new Vector3(x - .11f, y, k), new Vector3(x + .11f, y, k), .025f, 8);
                }
            }
            for (float k = z; k < z + 10; k += 2.5f)
            {
                frame.Box(new Vector3(-66.2f, 8.37f, k), new Vector3(9.4f, .43f, .12f));
                frame.Box(new Vector3(-66.2f, 8.18f, k), new Vector3(9.4f, .07f, .32f));
                frame.Tube(new Vector3(-70.7f, 8.35f, k), new Vector3(-61.7f, 8.35f, k + 2.4f), .045f, 8);
            }
            BraxCityArchitecture.MeshObject("Viaduct riveted steel frame", segment, frame, steel);
            Box("Viaduct deck", segment, new Vector3(-66.2f, 8.05f, z + 5), new Vector3(9.4f, .22f, 10f), concrete, true, 2f);
            foreach (float x in new[] { -70.7f, -61.7f })
            {
                Box("Viaduct pier footing", segment, new Vector3(x, .18f, z + 5), new Vector3(1.25f, .36f, 1.25f), abut, true);
                Box("Viaduct structural column", segment, new Vector3(x, 4.23f, z + 5), new Vector3(.72f, 8.1f, .78f), abut, true);
            }
        }
        // Przyczółek i portal tunelu w zboczu (z=−74): mur kamienny, łuk, czarne wnętrze 14 m w głąb.
        Box("Przyczółek: mur", vroot, new Vector3(-66.2f, 4.5f, PortalZ - .6f), new Vector3(14f, 9f, 1.2f), rockM, true, 3f);
        Box("Portal: nadproże", vroot, new Vector3(-66.2f, 14.6f, PortalZ - .6f), new Vector3(14f, 2.8f, 1.4f), rockM, true, 3f);
        foreach (float x in new[] { -72.4f, -60f })
            Box("Portal: filar", vroot, new Vector3(x, 11.1f, PortalZ - .6f), new Vector3(2.6f, 4.2f, 1.4f), rockM, true, 3f);
        var dark = BraxCityMaterials.Surface("Tunel: mrok", new Color(.02f, .02f, .02f), 1f);
        Box("Tunel: wnętrze", vroot, new Vector3(-66.2f, 11.2f, PortalZ - 8f), new Vector3(9.4f, 6f, 14f), dark, false, 4f);
        Box("Tunel: podłoga", vroot, new Vector3(-66.2f, 8.05f, PortalZ - 8f), new Vector3(9.4f, .22f, 14f), concrete, true, 2f);
        Box("Tunel: koniec (ściana)", vroot, new Vector3(-66.2f, 11.2f, PortalZ - 15.2f), new Vector3(9.6f, 6.4f, .4f), dark, true);
        // Wzgórze za portalem: pole wysokości, trawa, kilka skał i drzew (zasłania koniec świata).
        Hill(vroot);
        // Warsztaty pod łukami (4 przęsła z −70..−30): cegła, bramy rolowane, szyld; wnętrze zamknięte.
        var shutter = BraxRegion.Model("rollershutter_door");
        string[] names = { "AUTO SERWIS", "WULKANIZACJA", "GARAŻ 12", "MAGAZYN" };
        int i = 0;
        for (float z = -70f; z < -30f; z += 10f, i++)
        {
            var w = new GameObject("Warsztat pod łukiem: " + names[i]).transform; w.SetParent(vroot, false);
            float zc = z + 5f;
            Box("Warsztat: ściana tylna", w, new Vector3(-70.3f, 4.1f, zc), new Vector3(.3f, 7.9f, 9.2f), brick, true, 3f);
            Box("Warsztat: ściana boczna", w, new Vector3(-66.2f, 4.1f, z + .4f), new Vector3(8.4f, 7.9f, .3f), brick, true, 3f);
            Box("Warsztat: ściana boczna", w, new Vector3(-66.2f, 4.1f, z + 9.6f), new Vector3(8.4f, 7.9f, .3f), brick, true, 3f);
            Box("Warsztat: podłoga", w, new Vector3(-66.2f, Paving - .03f, zc), new Vector3(9f, .06f, 9.4f), concrete, true, 2f);
            // front (wschód, x=−61.7): cegła nad bramą + brama rolowana 3.6×3.4 + okno
            Box("Warsztat: front nad bramą", w, new Vector3(-61.9f, 6.0f, zc), new Vector3(.3f, 4.1f, 9.2f), brick, true, 3f);
            Box("Warsztat: front obok bramy", w, new Vector3(-61.9f, 1.8f, zc + 3.3f), new Vector3(.3f, 3.6f, 2.6f), brick, true, 3f);
            Box("Warsztat: okno", w, new Vector3(-61.85f, 2.2f, zc + 3.3f), new Vector3(.06f, 1.2f, 1.6f), darkGlass, false);
            Box("Warsztat: front obok bramy", w, new Vector3(-61.9f, 1.8f, zc - 3.6f), new Vector3(.3f, 3.6f, 2.0f), brick, true, 3f);
            if (shutter)
            {
                var o = BraxRegion.Put(w, shutter, "Warsztat: brama rolowana", new Vector3(-61.75f, Paving, zc - .3f), 90f, 1f, true, 3.5f);
                if (o) props++;
            }
            else Box("Warsztat: brama (stal)", w, new Vector3(-61.8f, 1.75f, zc - .3f), new Vector3(.1f, 3.5f, 3.6f), steelM, true);
            BraxCityArchitecture.Sign(w, names[i], new Vector3(-61.6f, 4.3f, zc), .32f, new Color(.92f, .92f, .88f));
            Box("Warsztat: szyld tło", w, new Vector3(-61.72f, 4.3f, zc), new Vector3(.05f, .7f, 5.2f), steelM, false);
        }
        var cart = BraxRegion.Model("tool_cart", "metal_tool_chest", "hand_truck");
        var tyre = BraxRegion.Model("old_tyre", "rusted_wheel_rim_01");
        var barrels = BraxRegion.Model("wooden_barrels_01", "barrel_03", "Barrel_02");
        foreach (var at in new[] { new Vector3(-59.5f, Paving, -66f), new Vector3(-59.3f, Paving, -47f), new Vector3(-59.6f, Paving, -38f) })
        {
            var choices = new List<GameObject> { cart, tyre, barrels }.Where(g => g).ToList();
            if (choices.Count == 0) break;
            var m = BraxRegion.Pick(choices);
            if (BraxRegion.Free(at + Vector3.up * .5f, new Vector3(1.2f, 1f, 1.2f)))
                if (BraxRegion.Put(root, m, "Warsztat: sprzęt przed bramą", at, BraxRegion.Range(0, 360), 1f, true, 1.2f)) props++;
        }
    }

    static void Hill(Transform parent)
    {
        const float cx = -66f, cz = -96f, rx = 50f, rz = 40f, H = 19f; const int n = 44;
        var verts = new List<Vector3>(); var idx = new List<int>(); var uv = new List<Vector2>();
        float x0 = cx - rx, z0 = cz - rz, sx = 2 * rx / n, sz = 2 * rz / n;
        for (int i = 0; i <= n; i++)
            for (int k = 0; k <= n; k++)
            {
                float x = x0 + i * sx, z = z0 + k * sz;
                float u = (x - cx) / rx, v = (z - cz) / rz, r2 = u * u + v * v;
                float h = r2 >= 1f ? -.6f : Lawn - .5f + H * Mathf.Pow(1f - r2, .85f);
                // tunel wchodzi w zbocze: nad portalem wzgórze nie opada poniżej 16.5 m
                if (Mathf.Abs(x - cx) < 8f && z > PortalZ - 18f && z < PortalZ + 2f) h = Mathf.Max(h, 16.5f + (PortalZ - 18f - z) * -.02f);
                verts.Add(new Vector3(x, h, z)); uv.Add(new Vector2(x * .25f, z * .25f));
            }
        for (int i = 0; i < n; i++)
            for (int k = 0; k < n; k++)
            {
                int a = i * (n + 1) + k, b = a + 1, c = a + (n + 1), d = c + 1;
                idx.Add(a); idx.Add(b); idx.Add(c); idx.Add(b); idx.Add(d); idx.Add(c);
            }
        var mesh = new Mesh { name = "Wzgórze za wiaduktem", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetTriangles(idx, 0); mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        Directory.CreateDirectory(BraxRegion.Out + "/Meshes");
        AssetDatabase.CreateAsset(mesh, BraxRegion.Out + "/Meshes/backyard_hill.asset");
        var g = new GameObject("Wzgórze za wiaduktem (trawa)"); g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = lawnMat; r.shadowCastingMode = ShadowCastingMode.Off;
        g.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        Physics.SyncTransforms();
        var rocks = new List<GameObject>(); foreach (var id in new[] { "rock_moss_set_01", "boulder_01", "coast_land_rocks_02", "stone_01" }) { var m = BraxRegion.Model(id); if (m) rocks.Add(m); }
        var pines = new List<GameObject>(); foreach (var id in new[] { "pine_tree_01", "fir_tree_01", "island_tree_03" }) { var m = BraxRegion.Model(id); if (m) pines.Add(m); }
        for (int t = 0; t < 26; t++)
        {
            float a = BraxRegion.Range(0, Mathf.PI * 2), rr = BraxRegion.Range(.25f, .92f);
            float x = cx + Mathf.Cos(a) * rx * rr, z = cz + Mathf.Sin(a) * rz * rr;
            if (z > PortalZ - 3f && Mathf.Abs(x - cx) < 9f) continue;   // nie w portalu
            RaycastHit h; if (!BraxRegion.Ground(x, z, -1f, 30f, out h) || h.collider.gameObject != g) continue;
            if (t % 4 == 0 && rocks.Count > 0) { if (BraxRegion.Put(parent, BraxRegion.Pick(rocks), "Wzgórze: skała", h.point, BraxRegion.Range(0, 360), BraxRegion.Range(.8f, 1.6f), true, 2.2f, .2f)) props++; }
            else if (pines.Count > 0) { if (BraxRegion.Put(parent, BraxRegion.Pick(pines), "Wzgórze: drzewo", h.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.15f), false, 15f)) trees++; }
        }
    }

    // ---------------------------------------------------------------- 4) wieżowiec B: uczciwe wejście + mały hol
    static void TowerEntranceB(BraxCityWorld world)
    {
        float cx = -36f, face = TowerB.max.z;   // z=−36, wejście od północy (plac)
        var cut = Bx(cx - 4.5f, cx + 4.5f, -.1f, 3.9f, face - 6.5f, face + .3f);
        int n = Cut(cut, "towerB_lobby");
        Lobby(world, "RESIDENCE", "TOWER_B_ENTRY", cx, face, 9f, 6.2f, 3.6f, false);
        BraxRegion.Note("wieżowiec B: hol wycięty z " + n + " siatek");
    }

    // Hol: podłoga granit, ściany tynk, sufit, szklany front z drzwiami (mechanizm BraxCityDoor), recepcja, rośliny, daszek.
    static void Lobby(BraxCityWorld world, string title, string doorId, float cx, float face, float width, float depth, float h, bool withLift)
    {
        var L = new GameObject("Hol: " + title).transform; L.SetParent(root, false);
        float x0 = cx - width * .5f, x1 = cx + width * .5f, zBack = face - depth, zFront = face - .02f;
        Box("Hol: podłoga", L, new Vector3(cx, Paving - .03f, (zBack + zFront) * .5f), new Vector3(width, .06f, depth), granite, true, 1.2f);
        if (!withLift)
        {
            Box("Hol: sufit", L, new Vector3(cx, Paving + h, (zBack + zFront) * .5f), new Vector3(width, .08f, depth), ceiling, false);
            Box("Hol: ściana tylna", L, new Vector3(cx, Paving + h * .5f, zBack + .1f), new Vector3(width, h, .2f), plaster, true, 1.5f);
        }
        else
        {
            // otwór na szyb windy (x cx±2.0, z zBack..zBack+4.0) w suficie i w ścianie tylnej
            float sh = 2.0f, shaftFront = zBack + 4.0f;
            Box("Hol: sufit L", L, new Vector3((x0 + cx - sh) * .5f, Paving + h, (zBack + zFront) * .5f), new Vector3(cx - sh - x0, .08f, depth), ceiling, false);
            Box("Hol: sufit P", L, new Vector3((cx + sh + x1) * .5f, Paving + h, (zBack + zFront) * .5f), new Vector3(x1 - cx - sh, .08f, depth), ceiling, false);
            Box("Hol: sufit środek", L, new Vector3(cx, Paving + h, (shaftFront + zFront) * .5f), new Vector3(sh * 2f, .08f, zFront - shaftFront), ceiling, false);
            Box("Hol: ściana tylna L", L, new Vector3((x0 + cx - sh) * .5f, Paving + h * .5f, zBack + .1f), new Vector3(cx - sh - x0, h, .2f), plaster, true, 1.5f);
            Box("Hol: ściana tylna P", L, new Vector3((cx + sh + x1) * .5f, Paving + h * .5f, zBack + .1f), new Vector3(x1 - cx - sh, h, .2f), plaster, true, 1.5f);
        }
        Box("Hol: ściana lewa", L, new Vector3(x0 + .1f, Paving + h * .5f, (zBack + zFront) * .5f), new Vector3(.2f, h, depth), plaster, true, 1.5f);
        Box("Hol: ściana prawa", L, new Vector3(x1 - .1f, Paving + h * .5f, (zBack + zFront) * .5f), new Vector3(.2f, h, depth), plaster, true, 1.5f);
        // front: szkło po bokach drzwi, słupki, nadproże
        float dw = 2.1f;
        foreach (float sgn in new[] { -1f, 1f })
        {
            float gx = cx + sgn * (dw * .5f + (width * .5f - dw * .5f) * .5f);
            Box("Hol: witryna", L, new Vector3(gx, Paving + h * .5f, zFront), new Vector3(width * .5f - dw * .5f - .1f, h - .1f, .04f), glassM, true);
            Box("Hol: słupek", L, new Vector3(cx + sgn * (dw * .5f + .05f), Paving + h * .5f, zFront), new Vector3(.1f, h, .12f), steelM, true);
        }
        Box("Hol: nadproże", L, new Vector3(cx, Paving + 2.76f, zFront), new Vector3(dw + .3f, .16f, .14f), steelM, true);
        Box("Hol: belka frontu", L, new Vector3(cx, Paving + h - .05f, zFront), new Vector3(width, .1f, .16f), steelM, true);
        // daszek nad wejściem
        Box("Wejście: daszek", L, new Vector3(cx, Paving + 3.25f, face + 1.2f), new Vector3(dw + 3.4f, .12f, 2.4f), steelM, true);
        Box("Wejście: daszek szkło", L, new Vector3(cx, Paving + 3.19f, face + 1.2f), new Vector3(dw + 3.0f, .02f, 2.2f), glassM, false);
        foreach (float sgn in new[] { -1f, 1f })
            Box("Wejście: cięgno daszka", L, new Vector3(cx + sgn * (dw * .5f + 1.4f), Paving + 3.9f, face + .9f), new Vector3(.05f, 1.6f, .05f), steelM, false);
        Box("Wejście: próg", L, new Vector3(cx, Paving + .005f, face + .7f), new Vector3(dw + 2.4f, .02f, 1.5f), granite, true, 1.2f);
        // drzwi (jedno skrzydło szklane, mechanizm jak w pawilonie)
        var entrance = new GameObject("Source-kit working entrance " + title).transform;
        entrance.SetParent(L, false); entrance.localPosition = new Vector3(cx, Paving, zFront); entrance.localRotation = Quaternion.identity;
        var hinge = new GameObject("Actual hinged source door leaf").transform;
        hinge.SetParent(entrance, false); hinge.localPosition = new Vector3(-dw * .5f, 0, 0);
        BraxCityArchitecture.Box("Wejście: szyba drzwi", hinge, new Vector3(dw * .5f, 1.37f, 0), new Vector3(dw - .16f, 2.56f, .02f), glassM);
        BraxCityArchitecture.Box("Wejście: rama góra", hinge, new Vector3(dw * .5f, 2.67f, 0), new Vector3(dw, .06f, .06f), steelM);
        BraxCityArchitecture.Box("Wejście: rama dół", hinge, new Vector3(dw * .5f, .06f, 0), new Vector3(dw, .12f, .06f), steelM);
        BraxCityArchitecture.Box("Wejście: rama lewa", hinge, new Vector3(.03f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), steelM);
        BraxCityArchitecture.Box("Wejście: rama prawa", hinge, new Vector3(dw - .03f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), steelM);
        foreach (float side in new[] { -.06f, .06f })
            BraxCityArchitecture.Box("Wejście: uchwyt", hinge, new Vector3(dw - .25f, 1.1f, side), new Vector3(.035f, 1.0f, .035f), steelM);
        foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        var col = hinge.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(dw * .5f, 1.35f, 0); col.size = new Vector3(dw, 2.7f, .12f);
        var door = entrance.gameObject.AddComponent<BraxCityDoor>();
        door.id = doorId; door.label = title; door.leaves = new[] { hinge }; door.sides = new[] { 1f };
        if (world) world.doors = world.doors.Concat(new[] { door }).ToArray();
        doors++;
        // wyposażenie: lada recepcji, rośliny, obraz, zegar, światło
        float ry = Paving;
        Box("Hol: lada recepcji", L, new Vector3(cx + width * .25f, ry + .55f, zBack + 1.6f), new Vector3(2.6f, 1.1f, .7f), wood, true, 1f);
        Box("Hol: blat recepcji", L, new Vector3(cx + width * .25f, ry + 1.12f, zBack + 1.6f), new Vector3(2.75f, .05f, .85f), granite, false, 1.2f);
        BraxCityArchitecture.Sign(L, title, new Vector3(cx, ry + 2.6f, zBack + .22f), .26f, new Color(.15f, .15f, .15f));
        var pot = BraxRegion.Model("potted_plant_02", "potted_plant_04", "potted_plant_01", "pachira_aquatica_01");
        if (pot) foreach (float sgn in new[] { -1f, 1f })
            if (BraxRegion.Put(L, pot, "Hol: roślina", new Vector3(cx + sgn * (width * .5f - .8f), ry, zBack + .9f), BraxRegion.Range(0, 360), 1f, true, 1.6f)) props++;
        var frame = BraxRegion.Model("hanging_picture_frame_02", "hanging_picture_frame_01");
        if (frame)
        {
            var o = BraxRegion.Put(L, frame, "Hol: obraz", new Vector3(x0 + .2f, ry + 1.2f, (zBack + zFront) * .5f), 90f, 1f, false, 1.1f);
            if (o) { o.transform.position += new Vector3(.02f, 0, 0); props++; }
        }
        var clock = BraxRegion.Model("wall_clock");
        if (clock) { var o = BraxRegion.Put(L, clock, "Hol: zegar", new Vector3(x1 - .25f, ry + 2.3f, (zBack + zFront) * .5f), -90f, 1f, false, .45f); if (o) props++; }
        BraxRegion.Warm(L, "Hol: światło", new Vector3(cx, ry + h - .3f, (zBack + zFront) * .5f), 1.3f, 8f);
        if (!withLift)
        {
            // atrapa drzwi windy w ścianie tylnej (zamknięte) — uczciwa, bo hol jest ślepy
            Box("Hol: drzwi windy", L, new Vector3(cx - width * .2f, ry + 1.3f, zBack + .22f), new Vector3(1.6f, 2.5f, .06f), steelM, true);
            Box("Hol: wskaźnik piętra", L, new Vector3(cx - width * .2f, ry + 2.75f, zBack + .24f), new Vector3(.5f, .12f, .03f), rubber, false);
        }
    }

    // ---------------------------------------------------------------- 5) wieżowiec A: hol, winda, piętro biurowe
    static void TowerAOffices(BraxCityWorld world)
    {
        float cx = 40f, face = TowerA.max.z;   // z=−35, wejście od północy
        int n = Cut(Bx(cx - 6.5f, cx + 6.5f, -.1f, 4.4f, face - 9f, face + .3f), "towerA_lobby");
        Lobby(world, "BRAXHORN OFFICE", "TOWER_A_ENTRY", cx, face, 13f, 8.6f, 4.0f, true);
        // Winda: szyb w holu przy ścianie tylnej (sz = face − 7.2), drzwi od strony placu (+z).
        float sx = cx, sz = face - 7.2f;
        Lift(world, "BRAXHORN OFFICE", sx, sz, Paving, OfficeY);
        // Piętro biurowe: pas elewacji wycięty (y 20.9..24.7) i zastąpiony przezroczystą szybą.
        float y0 = OfficeY, y1 = OfficeY + 3.4f;
        int nf = Cut(Bx(TowerA.min.x - .3f, TowerA.max.x + .3f, y0 - .35f, y1 + .35f, TowerA.min.z - .3f, TowerA.max.z + .3f), "towerA_office_band");
        var F = new GameObject("Piętro biurowe BRAXHORN OFFICE (y=" + OfficeY + ")").transform; F.SetParent(root, false);
        float fx0 = TowerA.min.x + .35f, fx1 = TowerA.max.x - .35f, fz0 = TowerA.min.z + .35f, fz1 = TowerA.max.z - .35f;
        // strop w 4 częściach wokół otworu szybu (x sx±1.9, z sz±1.9), wykładzina tak samo
        float ox0 = sx - 1.95f, ox1 = sx + 1.95f, oz0 = sz - 1.95f, oz1 = sz + 1.95f;
        foreach (var part in new[] { new Vector4(TowerA.min.x, ox0, TowerA.min.z, TowerA.max.z), new Vector4(ox1, TowerA.max.x, TowerA.min.z, TowerA.max.z), new Vector4(ox0, ox1, TowerA.min.z, oz0), new Vector4(ox0, ox1, oz1, TowerA.max.z) })
        {
            Box("Biuro: strop", F, new Vector3((part.x + part.y) * .5f, y0 - .18f, (part.z + part.w) * .5f), new Vector3(part.y - part.x, .36f, part.w - part.z), concrete, true, 2f);
            Box("Biuro: wykładzina", F, new Vector3((part.x + part.y) * .5f, y0 + .01f, (part.z + part.w) * .5f), new Vector3(part.y - part.x, .02f, part.w - part.z), rubber, false, 1f);
        }
        Box("Biuro: sufit", F, new Vector3(TowerA.center.x, y1 + .18f, TowerA.center.z), new Vector3(TowerA.size.x, .36f, TowerA.size.z), ceiling, true, 2f);
        // ściana osłonowa: szyba po obwodzie + słupki co 1.5 m
        foreach (var side in new[] { 0, 1, 2, 3 })
        {
            bool alongX = side < 2; float fixedC = side == 0 ? fz1 : side == 1 ? fz0 : side == 2 ? fx0 : fx1;
            float a0 = alongX ? fx0 : fz0, a1 = alongX ? fx1 : fz1;
            var center = alongX ? new Vector3((a0 + a1) * .5f, (y0 + y1) * .5f, fixedC) : new Vector3(fixedC, (y0 + y1) * .5f, (a0 + a1) * .5f);
            var size = alongX ? new Vector3(a1 - a0, y1 - y0, .05f) : new Vector3(.05f, y1 - y0, a1 - a0);
            Box("Biuro: szyba elewacji", F, center, size, darkGlass, true);
            for (float a = a0; a <= a1 + .01f; a += 1.5f)
                Box("Biuro: słupek elewacji", F, alongX ? new Vector3(a, (y0 + y1) * .5f, fixedC) : new Vector3(fixedC, (y0 + y1) * .5f, a), new Vector3(.08f, y1 - y0, .08f), steelM, false);
        }
        // trzon: szyb windy + klatka + WC (zamknięte drzwi), ściany tynk
        float cz0 = sz - 2.4f, cz1 = sz + 2.4f, cx0 = sx - 5.5f, cx1 = sx + 5.5f;
        foreach (var w in new[] { new Vector4(cx0, cx1, cz0, cz0), new Vector4(cx0, cx0, cz0, cz1), new Vector4(cx1, cx1, cz0, cz1) })
            Box("Biuro: ściana trzonu", F, new Vector3((w.x + w.y) * .5f, (y0 + y1) * .5f, (w.z + w.w) * .5f), new Vector3(Mathf.Max(.2f, w.y - w.x), y1 - y0, Mathf.Max(.2f, w.w - w.z)), plaster, true, 1.5f);
        Box("Biuro: ściana trzonu (front, bez windy)", F, new Vector3(cx0 + 1.9f, (y0 + y1) * .5f, cz1), new Vector3(3.8f, y1 - y0, .2f), plaster, true, 1.5f);
        Box("Biuro: ściana trzonu (front, bez windy)", F, new Vector3(cx1 - 1.9f, (y0 + y1) * .5f, cz1), new Vector3(3.8f, y1 - y0, .2f), plaster, true, 1.5f);
        Box("Biuro: drzwi klatki schodowej", F, new Vector3(cx0 + .3f, y0 + 1.05f, cz0 + 1.2f), new Vector3(.08f, 2.1f, .95f), wood, false);
        Box("Biuro: drzwi WC", F, new Vector3(cx1 - .3f, y0 + 1.05f, cz0 + 1.2f), new Vector3(.08f, 2.1f, .95f), wood, false);
        BraxCityArchitecture.Sign(F, "BRAXHORN OFFICE · 6. PIĘTRO", new Vector3(sx, y0 + 2.9f, cz1 + .12f), .22f, new Color(.15f, .15f, .15f));
        // recepcja piętra przy windzie, open space (biurka w 2 rzędach, północ), sala konferencyjna (wschód),
        // gabinet (zachód), kuchnia (róg SW); pas walki 12 m przy szybie południowej (z −47..−44) zostaje pusty.
        var desk = BraxRegion.Model("metal_office_desk", "WoodenTable_01");
        var chair = BraxRegion.Model("SchoolChair_01", "modern_arm_chair_01", "GreenChair_01");
        var laptop = BraxRegion.Model("classic_laptop");
        var lampD = BraxRegion.Model("desk_lamp_arm_01");
        var notepads = BraxRegion.Model("office_notepads", "stationery_supplies");
        var cabinet = BraxRegion.Model("modern_wooden_cabinet", "drawer_cabinet", "steel_frame_shelves_01");
        var sofa = BraxRegion.Model("Sofa_01", "sofa_02");
        var table = BraxRegion.Model("dining_table", "WoodenTable_02");
        var pot = BraxRegion.Model("potted_plant_02", "pachira_aquatica_01", "calathea_orbifolia_01", "potted_plant_01");
        var frame = BraxRegion.Model("hanging_picture_frame_01", "hanging_picture_frame_03");
        var clock = BraxRegion.Model("wall_clock");
        Physics.SyncTransforms();
        int placed = 0;
        // recepcja piętra
        Box("Biuro: lada recepcji", F, new Vector3(sx + 4.5f, y0 + .55f, cz1 + 1.6f), new Vector3(2.4f, 1.1f, .7f), wood, true);
        Box("Biuro: blat recepcji", F, new Vector3(sx + 4.5f, y0 + 1.12f, cz1 + 1.6f), new Vector3(2.55f, .05f, .85f), granite, false, 1.2f);
        // open space: 2 rzędy × 5 biurek, północna połowa (z −41..−37)
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 5; c++)
            {
                float dx = 25.5f + c * 2.6f, dz = -40.6f + r * 2.4f;
                if (Mathf.Abs(dx - sx) < 6f) continue;   // trzon
                var at = new Vector3(dx, y0 + .02f, dz);
                if (desk && BraxRegion.Free(at + Vector3.up * .5f, new Vector3(1.6f, 1f, .9f)))
                {
                    var d = BraxRegion.Put(F, desk, "Biuro: biurko", at, r == 0 ? 180f : 0f, 1f, true, .78f);
                    if (d)
                    {
                        placed++;
                        var db = BraxRegion.Bounds(d);
                        if (laptop && c % 2 == 0) { if (BraxRegion.Put(F, laptop, "Biuro: laptop", new Vector3(db.center.x - .25f, db.max.y, db.center.z), r == 0 ? 180f : 0f, 1f, false, .3f)) placed++; }
                        if (lampD && c % 2 == 1) { if (BraxRegion.Put(F, lampD, "Biuro: lampka", new Vector3(db.max.x - .25f, db.max.y, db.center.z), BraxRegion.Range(0, 360), 1f, false, .5f)) placed++; }
                        if (notepads && c == 2) { if (BraxRegion.Put(F, notepads, "Biuro: notatniki", new Vector3(db.center.x + .35f, db.max.y, db.center.z), BraxRegion.Range(0, 360), 1f, false, .12f)) placed++; }
                        if (chair) { var ch = BraxRegion.Put(F, chair, "Biuro: krzesło", at + new Vector3(0, 0, r == 0 ? .75f : -.75f), r == 0 ? 180f : 0f, 1f, true, .95f); if (ch) placed++; }
                    }
                }
            }
        // sala konferencyjna (wschód x 50..57): szklana ścianka, stół, krzesła
        Box("Biuro: ścianka sali", F, new Vector3(50f, (y0 + y1) * .5f, (fz0 + fz1) * .5f), new Vector3(.04f, y1 - y0, fz1 - fz0 - 1.2f), glassM, true);
        Box("Biuro: słupek ścianki", F, new Vector3(50f, (y0 + y1) * .5f, fz1 - 1.2f), new Vector3(.08f, y1 - y0, .08f), steelM, false);
        if (table) { var t = BraxRegion.Put(F, table, "Biuro: stół konferencyjny", new Vector3(53.7f, y0 + .02f, -42f), 90f, 1.2f, true, .78f); if (t) placed++; }
        if (chair) foreach (var at in new[] { new Vector3(52.3f, y0 + .02f, -40.5f), new Vector3(52.3f, y0 + .02f, -43.5f), new Vector3(55.1f, y0 + .02f, -40.5f), new Vector3(55.1f, y0 + .02f, -43.5f) })
            if (BraxRegion.Put(F, chair, "Biuro: krzesło konferencyjne", at, at.x < 53.7f ? 90f : -90f, 1f, true, .95f)) placed++;
        // gabinet (zachód x 23..30): ścianka, biurko, sofa, szafka
        Box("Biuro: ścianka gabinetu", F, new Vector3(30f, (y0 + y1) * .5f, (fz0 + fz1) * .5f), new Vector3(.04f, y1 - y0, fz1 - fz0 - 1.2f), glassM, true);
        if (desk) { var d = BraxRegion.Put(F, desk, "Gabinet: biurko", new Vector3(25.5f, y0 + .02f, -46.5f), 90f, 1f, true, .78f); if (d) { placed++; if (laptop) BraxRegion.Put(F, laptop, "Gabinet: laptop", new Vector3(25.5f, BraxRegion.Bounds(d).max.y, -46.5f), 90f, 1f, false, .3f); } }
        if (chair) { if (BraxRegion.Put(F, chair, "Gabinet: fotel", new Vector3(24.3f, y0 + .02f, -46.5f), 90f, 1f, true, .95f)) placed++; }
        if (sofa) { if (BraxRegion.Put(F, sofa, "Gabinet: sofa", new Vector3(26.5f, y0 + .02f, -38.5f), 180f, 1f, true, .9f)) placed++; }
        if (cabinet) { if (BraxRegion.Put(F, cabinet, "Gabinet: szafka", new Vector3(23.2f, y0 + .02f, -41.5f), 90f, 1f, true, 1.9f)) placed++; }
        // kuchnia (SW przy trzonie): blat, szafki
        Box("Biuro: blat kuchenny", F, new Vector3(33.5f, y0 + .9f, -47.8f), new Vector3(4f, .06f, .62f), granite, true, 1.2f);
        Box("Biuro: szafki kuchenne", F, new Vector3(33.5f, y0 + .43f, -47.8f), new Vector3(4f, .86f, .58f), wood, true);
        // rośliny, obrazy, zegar, światła
        if (pot) foreach (var at in new[] { new Vector3(fx0 + 1f, y0 + .02f, fz1 - 1f), new Vector3(fx1 - 1f, y0 + .02f, fz1 - 1f), new Vector3(sx + 5.8f, y0 + .02f, cz0 - .9f), new Vector3(fx0 + 1f, y0 + .02f, fz0 + 1f) })
            if (BraxRegion.Put(F, pot, "Biuro: roślina", at, BraxRegion.Range(0, 360), 1f, true, 1.7f)) placed++;
        if (frame) foreach (float x in new[] { cx0 + 2f, cx1 - 2f })
            { var o = BraxRegion.Put(F, frame, "Biuro: obraz", new Vector3(x, y0 + 1.3f, cz1 - .02f), 180f, 1f, false, 1.0f); if (o) { o.transform.position += new Vector3(0, 0, .25f); placed++; } }
        if (clock) { var o = BraxRegion.Put(F, clock, "Biuro: zegar", new Vector3(sx, y0 + 2.4f, cz1 - .02f), 180f, 1f, false, .45f); if (o) { o.transform.position += new Vector3(0, 0, .25f); placed++; } }
        foreach (var at in new[] { new Vector3(28f, y1 - .25f, -39f), new Vector3(28f, y1 - .25f, -46f), new Vector3(46f, y1 - .25f, -39f), new Vector3(46f, y1 - .25f, -46f), new Vector3(53.5f, y1 - .25f, -42f), new Vector3(sx, y1 - .25f, cz1 + 2.2f) })
            BraxRegion.Warm(F, "Biuro: oprawa sufitowa", at, 1.4f, 9f);
        props += placed;
        // punkty świata (misje): hol, winda, piętro, pas walki
        if (world)
            world.points = world.points.Concat(new[]
            {
                new BraxCityPoint { id = "R1_OFFICE_TOWER_LOBBY", label = "BRAXHORN OFFICE hol", position = new Vector3(cx, Paving, face - 3f) },
                new BraxCityPoint { id = "R1_OFFICE_TOWER_FLOOR", label = "BRAXHORN OFFICE 6. piętro", position = new Vector3(sx, OfficeY, cz1 + 2.5f) },
                new BraxCityPoint { id = "R1_OFFICE_TOWER_ARENA", label = "BRAXHORN OFFICE pas walki przy szybie", position = new Vector3(40f, OfficeY, -45.5f) },
            }).ToArray();
        BraxRegion.Note("wieżowiec A: hol wycięty z " + n + " siatek, pas elewacji z " + nf + ", wyposażenie biura " + placed);
    }

    // Winda dwuprzystankowa (jak sieć wind wieżowców): szyb, kabina, drzwi, panel, sterownik BraxCityElevator.
    static void Lift(BraxCityWorld world, string id, float sx, float sz, float lowerY, float upperY)
    {
        const float HalfShaft = 1.83f; float dir = -1f;   // drzwi od +z (plac/hol)
        var steel = BraxCityMaterials.Surface("Premium black-titanium elevator frame", new Color(.12f, .145f, .16f), .46f, .62f);
        var stone = BraxCityMaterials.Surface("Solid shaft matte stone and fire partition", new Color(.33f, .34f, .35f), .88f, .04f);
        var glass = BraxCityMaterials.Surface("Lift panoramic smoke tempered safety glass", new Color(.26f, .36f, .39f, .68f), .21f, .10f);
        var deck = BraxCityMaterials.Surface("Non-slip roof and lift floor textured basalt", new Color(.42f, .42f, .40f), .92f, .02f);
        var illumination = BraxCityMaterials.Surface("Warm satin daylight LED diffuser / unlit", new Color(.78f, .71f, .57f), .43f, .03f);
        var black = BraxCityMaterials.Surface("Graphite in-cabin vertical floor-control panel", new Color(.08f, .09f, .1f), .6f, .3f);
        var L = new GameObject("REAL OFFICE ELEVATOR " + id).transform; L.SetParent(root, false);
        float top = upperY + 3.6f, mid = (lowerY + top) * .5f, hgt = top - lowerY + .3f;
        Box("Szyb: ściana W", L, new Vector3(sx - HalfShaft - .06f, mid, sz), new Vector3(.15f, hgt, 3.86f), stone, true);
        Box("Szyb: ściana E", L, new Vector3(sx + HalfShaft + .06f, mid, sz), new Vector3(.15f, hgt, 3.86f), stone, true);
        Box("Szyb: ściana tylna", L, new Vector3(sx, mid, sz + dir * (HalfShaft + .05f)), new Vector3(3.85f, hgt, .16f), stone, true);
        float front = sz - dir * (HalfShaft + .06f);
        float lowerHead = lowerY + 3.10f, upperBottom = upperY - .2f;
        Box("Szyb: ściana frontowa między przystankami", L, new Vector3(sx, (lowerHead + upperBottom) * .5f, front), new Vector3(3.85f, upperBottom - lowerHead, .16f), stone, true);
        Box("Szyb: nad górnym przystankiem", L, new Vector3(sx, upperY + 3.25f + .2f, front), new Vector3(3.85f, .5f, .16f), stone, true);
        foreach (float yy in new[] { lowerY + 2.94f, upperY + 2.94f })
            Box("Szyb: nadproże przystanku", L, new Vector3(sx, yy, front), new Vector3(3.78f, .20f, .2f), steel);
        // kabina
        var cabin = new GameObject("Actual moving premium elevator car " + id).transform; cabin.SetParent(L, false); cabin.localPosition = new Vector3(sx, lowerY, sz);
        cabin.gameObject.AddComponent<BraxCityMovingLiftPart>();
        Box("Kabina: podłoga", cabin, new Vector3(0, -.077f, 0), new Vector3(3.28f, .154f, 3.25f), deck, true);
        Box("Kabina: sufit", cabin, new Vector3(0, 3.13f, 0), new Vector3(3.28f, .18f, 3.25f), steel, true);
        foreach (float xx in new[] { -1.59f, 1.59f })
        {
            Box("Kabina: szyba boczna", cabin, new Vector3(xx, 1.55f, 0), new Vector3(.10f, 3.05f, 3.02f), glass, true);
            Box("Kabina: poręcz", cabin, new Vector3(xx * .91f, 1.06f, 0), new Vector3(.10f, .09f, 2.64f), steel);
        }
        Box("Kabina: tył", cabin, new Vector3(0, 1.5f, dir * 1.55f), new Vector3(3.07f, 3.0f, .10f), glass, true);
        foreach (float xx in new[] { -1.10f, 1.10f }) Box("Kabina: oprawa LED", cabin, new Vector3(xx, 3.028f, 0), new Vector3(.095f, .025f, 2.70f), illumination);
        Transform Leaf(string label, Transform parent, Vector3 centre)
        {
            var leaf = new GameObject(label).transform; leaf.SetParent(parent, false); leaf.localPosition = centre;
            leaf.gameObject.AddComponent<BraxCityMovingLiftPart>();
            Box("Drzwi: szkło", leaf, new Vector3(0, 1.39f, 0), new Vector3(1.47f, 2.78f, .075f), glass, true);
            foreach (float xx in new[] { -.68f, .68f }) Box("Drzwi: ramiak", leaf, new Vector3(xx, 1.39f, -dir * .052f), new Vector3(.035f, 2.78f, .08f), steel);
            Box("Drzwi: próg", leaf, new Vector3(0, .08f, -dir * .052f), new Vector3(1.47f, .12f, .075f), steel);
            return leaf;
        }
        var cabL = Leaf("Premium cabin moving left", cabin, new Vector3(-.77f, 0, -dir * 1.55f));
        var cabR = Leaf("Premium cabin moving right", cabin, new Vector3(.77f, 0, -dir * 1.55f));
        float landingFront = sz - dir * (HalfShaft + .02f);
        var lowerL = Leaf("Lobby sliding door left", L, new Vector3(sx - .77f, lowerY, landingFront));
        var lowerR = Leaf("Lobby sliding door right", L, new Vector3(sx + .77f, lowerY, landingFront));
        var upperL = Leaf("Office sliding door left", L, new Vector3(sx - .77f, upperY, landingFront));
        var upperR = Leaf("Office sliding door right", L, new Vector3(sx + .77f, upperY, landingFront));
        float callZ = landingFront - dir * .90f;
        foreach (float y in new[] { lowerY, upperY })
        {
            var call = new GameObject("Physical elevator call panel").transform; call.SetParent(L, false); call.localPosition = new Vector3(sx - 1.45f, y, callZ);
            Box("Panel: front", call, new Vector3(0, 1.25f, 0), new Vector3(.24f, .58f, .15f), black);
            Box("Panel: przycisk", call, new Vector3(0, 1.36f, -dir * .09f), new Vector3(.11f, .11f, .025f), illumination);
        }
        BraxCityArchitecture.Sign(L, "WINDA · 6. PIĘTRO BIURA", new Vector3(sx, lowerY + 2.75f, callZ + .08f * dir), .22f, Color.white);
        BraxCityArchitecture.Sign(L, "WINDA · HOL", new Vector3(sx, upperY + 2.75f, callZ + .08f * dir), .22f, Color.white);
        var controller = L.gameObject.AddComponent<BraxCityElevator>();
        controller.player = world ? world.player : null; controller.cabin = cabin;
        controller.cabinDoorLeft = cabL; controller.cabinDoorRight = cabR;
        controller.lowerDoorLeft = lowerL; controller.lowerDoorRight = lowerR;
        controller.upperDoorLeft = upperL; controller.upperDoorRight = upperR;
        controller.lowerY = lowerY; controller.upperY = upperY;
        controller.entranceZ = landingFront; controller.shaftCenter = new Vector2(sx, sz);
        controller.lowerCall = new Vector3(sx - 1.45f, lowerY + 1.2f, callZ);
        controller.upperCall = new Vector3(sx - 1.45f, upperY + 1.2f, callZ);
        if (world) { var all = world.additionalTowerElevators.ToList(); all.Add(controller); world.additionalTowerElevators = all.ToArray(); }
        foreach (var moving in L.GetComponentsInChildren<BraxCityMovingLiftPart>(true))
            foreach (var t in moving.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
    }

    static int Cut(Bounds volume, string label)
    {
        var original = BraxClaudeGround.SourceCity; if (!original) return 0;
        try
        {
            int n = BraxCityMeshCut.Subtract(original, volume, BraxRegion.Out + "/Cuts", label);
            if (BraxClaudeGround.SourcePhysics)
                foreach (var col in BraxClaudeGround.SourcePhysics.GetComponentsInChildren<Collider>(true))
                    if (col.enabled && !col.isTrigger && col.bounds.Intersects(volume) && col is BoxCollider && volume.Contains(col.bounds.center)) col.enabled = false;
            return n;
        }
        catch (Exception e) { BraxRegion.Note("tyły: cięcie " + label + ": " + e.Message); return 0; }
    }
}

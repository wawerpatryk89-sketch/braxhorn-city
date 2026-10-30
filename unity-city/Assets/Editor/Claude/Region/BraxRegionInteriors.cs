using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BRAXHORN CITY — wykończenie wnętrz: sklep BRAXHORN NUTRITION i klub NOVA.
// Nutrition: spójne ściany (tynk), deska na podłodze, ciepłe światło, zegar, rośliny, kosz.
// NOVA: loże (fotele + stoliki), obrazy na ścianach, rośliny w rogach — tylko w wolnych miejscach
// (kolizje sprawdzane), wyposażenie Claude (DJ, lasery, bar, kula) i drzwi zostają.
public static class BraxRegionInteriors
{
    static readonly Bounds Nutrition = new Bounds(new Vector3(20.25f, 2.2f, 16.1f), new Vector3(12.5f, 7.8f, 10.9f));
    static readonly Bounds Nova = new Bounds(new Vector3(43.5f, 1.6f, 48.5f), new Vector3(25f, 3.8f, 15f));

    public static void Apply(Transform cityRoot)
    {
        try { NutritionShop(cityRoot); BraxRegion.Rep.nutrition = true; }
        catch (Exception e) { BraxRegion.Note("Nutrition: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_REGION Nutrition " + e); }
        try { NovaClub(cityRoot); BraxRegion.Rep.nova = true; }
        catch (Exception e) { BraxRegion.Note("NOVA: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_REGION NOVA " + e); }
        try { NovaFront(cityRoot); }
        catch (Exception e) { BraxRegion.Note("NOVA: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_REGION NOVA " + e); }
    }

    static bool Inside(Bounds b, Renderer r) { return r && b.Intersects(r.bounds); }

    static void NutritionShop(Transform cityRoot)
    {
        var plaster = BraxRegion.Pbr("Nutrition: tynk ścian", 1.5f, new Color(.93f, .91f, .87f), "white_plaster_02", "rectangular_facade_tiles");
        var walnut = BraxRegion.Pbr("Nutrition: ściana tylna z drewna", 1.2f, new Color(.62f, .50f, .40f), "wood_plank_wall", "wood_planks");
        var floor = BraxRegion.Pbr("Nutrition: deska podłogowa", 1.6f, null, "plank_flooring_02", "wood_floor", "wood_planks");
        var glazing = BraxRegion.Fitting("Nutrition: szyba witryny", new Color(.72f, .82f, .86f, .28f), .04f, 0f);
        int walls = 0, floors = 0;
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!Inside(Nutrition, r)) continue;
            string n = r.gameObject.name;
            if (n.StartsWith("mineral side wall", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = plaster; walls++; }
            else if (n.StartsWith("rear mineral wall", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = walnut; walls++; }
            else if (n.StartsWith("Nutrition continuous original-level floor", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = floor; floors++; }
            else if (n.StartsWith("shopfront window glazing", StringComparison.OrdinalIgnoreCase) || n.StartsWith("clear warm shopfront glazing", StringComparison.OrdinalIgnoreCase))
            { r.sharedMaterial = glazing; walls++; }   // widoczna szyba witryny (lekki odcień i odbicie), nie „otwarty front”
        }
        // Podłoga sklepu: najwyższe podłoże w środku.
        RaycastHit g;
        float y = BraxRegion.Ground(Nutrition.center.x, Nutrition.center.z, -1f, 1.5f, out g) ? g.point.y : .15f;
        var root = BraxRegion.Root(cityRoot, "CLAUDE Nutrition: wykończenie");
        BraxRegion.Warm(root, "Nutrition: światło nad ladą", new Vector3(Nutrition.center.x - 2.5f, y + 2.9f, Nutrition.center.z), 1.2f, 7f);
        BraxRegion.Warm(root, "Nutrition: światło nad półkami", new Vector3(Nutrition.center.x + 2.5f, y + 2.9f, Nutrition.center.z - 2f), 1.1f, 7f);
        BraxRegion.Warm(root, "Nutrition: światło przy witrynie", new Vector3(Nutrition.center.x, y + 2.9f, Nutrition.max.z - 2.2f), 1.0f, 6f);
        var pot = BraxRegion.Model("potted_plant_02", "potted_plant_04", "potted_plant_01");
        var bin = BraxRegion.Model("metal_trash_can");
        var clock = BraxRegion.Model("wall_clock");
        int props = 0;
        foreach (var at in new[] { new Vector3(Nutrition.min.x + 1.0f, y, Nutrition.max.z - 1.2f), new Vector3(Nutrition.max.x - 1.0f, y, Nutrition.max.z - 1.2f) })
            if (pot && BraxRegion.Free(at + Vector3.up * .8f, new Vector3(.9f, 1.4f, .9f)))
            { if (BraxRegion.Put(root, pot, "Nutrition: roślina przy witrynie", at, BraxRegion.Range(0, 360), 1f, true, 1.5f)) props++; }
        var binAt = new Vector3(Nutrition.min.x + 1.0f, y, Nutrition.min.z + 1.5f);
        if (bin && BraxRegion.Free(binAt + Vector3.up * .5f, new Vector3(.7f, .9f, .7f)))
        { if (BraxRegion.Put(root, bin, "Nutrition: kosz", binAt, 0f, 1f, true, .9f)) props++; }
        if (clock)
        {
            var o = (GameObject)UnityEngine.Object.Instantiate(clock, root, false);
            o.name = "Nutrition: zegar"; o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.Euler(0, 180, 0) * o.transform.rotation;
            var b = BraxRegion.Bounds(o); float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (big > .45f && big > .001f) { o.transform.localScale *= .45f / big; b = BraxRegion.Bounds(o); }
            o.transform.position = new Vector3(Nutrition.center.x, y + 2.5f, Nutrition.min.z + .08f) - b.center;
            props++;
        }
        BraxRegion.Note("Nutrition: ściany " + walls + ", podłogi " + floors + ", dodatki " + props);
    }

    // Front klubu NOVA od ulicy (parter x 32..52 przy z=36.1 to czarna bryła ze starego miasta):
    // ciemne panele, granitowe pilastry, nadproże i daszek nad wejściem, napis NOVA, kinkiety (bez poświaty),
    // słupki z czerwoną liną (kolejka), donice z roślinami. Otwór drzwi (x 40.9..43.1) i skrzydło zostają wolne.
    static void NovaFront(Transform cityRoot)
    {
        const float X0 = 32.2f, X1 = 51.8f, Z = 36.0f, H = 5.0f, DoorL = 40.4f, DoorR = 43.6f, DoorH = 2.95f;
        var root = BraxRegion.Root(cityRoot, "CLAUDE NOVA: front od ulicy");
        var panel = BraxRegion.Pbr("NOVA front: ciemne panele", 1.4f, new Color(.16f, .16f, .18f), "exterior_wall_cladding_02", "exterior_wall_cladding", "concrete_wall_008");
        var pilaster = BraxRegion.Pbr("NOVA front: granitowe pilastry", 1.0f, new Color(.30f, .30f, .32f), "granite_tile", "granite_tile_02");
        var soffit = BraxRegion.Pbr("NOVA front: spód daszka", 1.0f, new Color(.45f, .36f, .30f), "dark_wood", "wood_planks");
        var rope = BraxRegion.Pbr("NOVA front: czerwona lina", .4f, null, "leather_red_02", "leather_white", "wood_planks");
        var champagne = BraxRegion.Fitting("NOVA front: stal szampańska", BraxRegion.Champagne, .30f, .92f);
        var black = BraxRegion.Fitting("NOVA front: czarna stal", new Color(.06f, .06f, .065f), .55f, .6f);
        float y0 = .15f;
        // panele po bokach wejścia, od cokołu do pasa nad parterem
        BraxRegion.Box("NOVA front: panel lewy", root, new Vector3((X0 + DoorL) * .5f, y0 + H * .5f, Z), new Vector3(DoorL - X0, H, .12f), panel, true);
        BraxRegion.Box("NOVA front: panel prawy", root, new Vector3((DoorR + X1) * .5f, y0 + H * .5f, Z), new Vector3(X1 - DoorR, H, .12f), panel, true);
        BraxRegion.Box("NOVA front: nadproże", root, new Vector3((DoorL + DoorR) * .5f, y0 + DoorH + (H - DoorH) * .5f, Z), new Vector3(DoorR - DoorL, H - DoorH, .12f), panel, true);
        BraxRegion.Box("NOVA front: cokół", root, new Vector3((X0 + X1) * .5f, y0 + .3f, Z - .04f), new Vector3(X1 - X0, .6f, .2f), pilaster, true);
        for (float px = X0 + .4f; px <= X1 - .3f; px += 4.9f)
        {
            if (px > DoorL - 1.2f && px < DoorR + 1.2f) continue;
            BraxRegion.Box("NOVA front: pilaster", root, new Vector3(px, y0 + H * .5f, Z - .07f), new Vector3(.5f, H, .26f), pilaster, true);
        }
        // portal wejścia: granit po bokach, daszek ze stali szampańskiej, czarny spód z ciepłym światłem
        BraxRegion.Box("NOVA front: portal lewy", root, new Vector3(DoorL - .3f, y0 + DoorH * .5f, Z - .1f), new Vector3(.6f, DoorH, .32f), pilaster, true);
        BraxRegion.Box("NOVA front: portal prawy", root, new Vector3(DoorR + .3f, y0 + DoorH * .5f, Z - .1f), new Vector3(.6f, DoorH, .32f), pilaster, true);
        BraxRegion.Box("NOVA front: portal góra", root, new Vector3((DoorL + DoorR) * .5f, y0 + DoorH + .25f, Z - .1f), new Vector3(DoorR - DoorL + 1.2f, .5f, .32f), pilaster, true);
        BraxRegion.Box("NOVA front: daszek", root, new Vector3((DoorL + DoorR) * .5f, y0 + 3.55f, Z - 1.1f), new Vector3(7.0f, .16f, 2.2f), champagne, true);
        BraxRegion.Box("NOVA front: spód daszka", root, new Vector3((DoorL + DoorR) * .5f, y0 + 3.46f, Z - 1.1f), new Vector3(6.8f, .02f, 2.1f), soffit);
        foreach (float sx in new[] { (DoorL + DoorR) * .5f - 3.2f, (DoorL + DoorR) * .5f + 3.2f })
            BraxRegion.Box("NOVA front: cięgno daszka", root, new Vector3(sx, y0 + 4.2f, Z - 1.1f), new Vector3(.04f, 1.4f, .04f), champagne);
        BraxRegion.Warm(root, "NOVA front: światło pod daszkiem", new Vector3((DoorL + DoorR) * .5f, y0 + 3.3f, Z - 1.0f), 1.0f, 6f);
        // napis NOVA nad daszkiem (test głębi daje BraxRegionSignage)
        var sign = new GameObject("NOVA front: napis NOVA");
        sign.transform.SetParent(root, false);
        sign.transform.position = new Vector3((DoorL + DoorR) * .5f, y0 + 4.35f, Z - .2f);
        sign.transform.rotation = Quaternion.identity;   // czytelny od strony ulicy (-Z)
        var tm = sign.AddComponent<TextMesh>();
        tm.text = "NOVA"; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = .95f * 10f / 64f; tm.color = new Color(.95f, .95f, .93f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var smr = sign.GetComponent<MeshRenderer>(); if (smr && tm.font) smr.sharedMaterial = tm.font.material;
        BraxRegion.Box("NOVA front: tło napisu", root, new Vector3((DoorL + DoorR) * .5f, y0 + 4.35f, Z - .06f), new Vector3(4.6f, .9f, .06f), black);
        // kinkiety po bokach wejścia (modele Poly Haven, bez poświaty)
        var lamp = BraxRegion.Model("industrial_wall_lamp", "industrial_wall_sconce", "industrial_caged_sconce");
        if (lamp)
            foreach (float lx in new[] { DoorL - 1.3f, DoorR + 1.3f })
            {
                var o = (GameObject)UnityEngine.Object.Instantiate(lamp, root, false);
                o.name = "NOVA front: kinkiet"; o.transform.position = Vector3.zero;
                o.transform.rotation = Quaternion.Euler(0, 180, 0) * o.transform.rotation;
                var b = BraxRegion.Bounds(o); float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (big > .6f && big > .001f) { o.transform.localScale *= .6f / big; b = BraxRegion.Bounds(o); }
                o.transform.position = new Vector3(lx, y0 + 2.7f, Z - .08f - b.extents.z) - new Vector3(b.center.x, b.center.y, 0) + new Vector3(0, 0, 0);
            }
        // kolejka: słupki ze stali i czerwona lina, po lewej stronie wejścia wzdłuż chodnika
        for (int i = 0; i < 4; i++)
        {
            float px = DoorL - .9f - i * 1.4f, pz = Z - 1.9f;
            BraxRegion.Box("NOVA front: słupek kolejki", root, new Vector3(px, y0 + .5f, pz), new Vector3(.06f, 1.0f, .06f), champagne, true);
            BraxRegion.Box("NOVA front: podstawa słupka", root, new Vector3(px, y0 + .02f, pz), new Vector3(.3f, .04f, .3f), champagne);
            if (i > 0) BraxRegion.Box("NOVA front: lina", root, new Vector3(px + .7f, y0 + .9f, pz), new Vector3(1.3f, .05f, .05f), rope);
        }
        // donice z roślinami po obu stronach, kosz
        var planter = BraxRegion.Model("planter_box_02", "planter_box_01");
        var plant = BraxRegion.Model("potted_plant_04", "potted_plant_02");
        if (planter)
            foreach (float px in new[] { DoorL - 3.4f, DoorR + 3.4f })
            {
                BraxRegion.Put(root, planter, "NOVA front: donica", new Vector3(px, y0, Z - .7f), 0f, 1f, true, .6f);
                if (plant) BraxRegion.Put(root, plant, "NOVA front: roślina", new Vector3(px, y0 + .3f, Z - .7f), BraxRegion.Range(0, 360), 1f, false, 1.4f);
            }
        var bin = BraxRegion.Model("metal_trash_can");
        if (bin) BraxRegion.Put(root, bin, "NOVA front: kosz", new Vector3(DoorR + 1.9f, y0, Z - 1.0f), 0f, 1f, true, .95f);
        BraxRegion.Note("NOVA: front od ulicy zbudowany (x " + X0 + ".." + X1 + ")");
    }

    static float WallDistance(Vector3 eye, Vector3 dir, float max)
    {
        RaycastHit h;
        return Physics.Raycast(eye, dir, out h, max, ~0, QueryTriggerInteraction.Ignore) ? h.distance : max;
    }

    static void NovaClub(Transform cityRoot)
    {
        RaycastHit g;
        float y = BraxRegion.Ground(Nova.center.x, Nova.center.z, -1f, 1.5f, out g) ? g.point.y : .15f;
        var eye = new Vector3(Nova.center.x, y + 1.3f, Nova.center.z);
        float wx0 = eye.x - WallDistance(eye, Vector3.left, Nova.extents.x + 1f);
        float wx1 = eye.x + WallDistance(eye, Vector3.right, Nova.extents.x + 1f);
        float wz0 = eye.z - WallDistance(eye, Vector3.back, Nova.extents.z + 1f);
        float wz1 = eye.z + WallDistance(eye, Vector3.forward, Nova.extents.z + 1f);
        var root = BraxRegion.Root(cityRoot, "CLAUDE NOVA: loże, obrazy, rośliny");

        var table = BraxRegion.Model("coffee_table_round_01", "modern_coffee_table_01", "industrial_coffee_table");
        var chair = BraxRegion.Model("modern_arm_chair_01", "ArmChair_01", "mid_century_lounge_chair");
        var plant = BraxRegion.Model("potted_plant_04", "potted_plant_02");
        var frames = new List<GameObject>();
        foreach (var id in new[] { "hanging_picture_frame_01", "hanging_picture_frame_02", "hanging_picture_frame_03" })
        { var m = BraxRegion.Model(id); if (m) frames.Add(m); }

        // Loże: szukamy wolnych kwadratów 3×3 m wzdłuż ścian wschodniej i zachodniej.
        int lounges = 0, plants = 0, pictures = 0;
        if (table && chair)
            foreach (float wx in new[] { wx0 + 1.9f, wx1 - 1.9f })
                for (float z = wz0 + 2.2f; z < wz1 - 2.2f && lounges < 6; z += 3.4f)
                {
                    var c = new Vector3(wx, y, z);
                    if (!BraxRegion.Free(c + Vector3.up * 1.0f, new Vector3(3.0f, 1.8f, 3.0f))) continue;
                    if (!BraxRegion.Put(root, table, "NOVA: stolik loży", c, 0f, 1f, true, .6f)) continue;
                    float toward = wx < Nova.center.x ? 90f : -90f;   // fotele przodem do sali
                    foreach (float dz in new[] { -1.0f, 1.0f })
                        BraxRegion.Put(root, chair, "NOVA: fotel loży", c + new Vector3(0, 0, dz), toward + (dz < 0 ? 30f : -30f), 1f, true, 1.1f);
                    lounges++;
                }
        // Rośliny w rogach sali (poza przejściami).
        if (plant)
            foreach (var at in new[] { new Vector3(wx0 + .7f, y, wz0 + .7f), new Vector3(wx1 - .7f, y, wz0 + .7f), new Vector3(wx0 + .7f, y, wz1 - .7f), new Vector3(wx1 - .7f, y, wz1 - .7f) })
                if (BraxRegion.Free(at + Vector3.up * .8f, new Vector3(1.0f, 1.5f, 1.0f)))
                { if (BraxRegion.Put(root, plant, "NOVA: roślina w rogu", at, BraxRegion.Range(0, 360), 1f, true, 1.8f)) plants++; }
        // Obrazy na ścianach bocznych (na wysokości 1.7 m), co 4 m, tylko gdzie ściana jest wolna.
        if (frames.Count > 0)
            foreach (float wx in new[] { wx0, wx1 })
                for (float z = wz0 + 3f; z < wz1 - 2f; z += 4f)
                {
                    float inward = wx < Nova.center.x ? 1f : -1f;
                    var at = new Vector3(wx + inward * .06f, y + 1.7f, z);
                    if (!BraxRegion.Free(at + new Vector3(inward * .35f, 0, 0), new Vector3(.5f, 1.2f, 1.2f))) continue;
                    var src = BraxRegion.Pick(frames);
                    var o = (GameObject)UnityEngine.Object.Instantiate(src, root, false);
                    o.name = "NOVA: obraz na ścianie"; o.transform.position = Vector3.zero;
                    o.transform.rotation = Quaternion.Euler(0, inward > 0 ? 90f : -90f, 0) * o.transform.rotation;
                    var b = BraxRegion.Bounds(o); float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (big > 1.1f && big > .001f) { o.transform.localScale *= 1.1f / big; b = BraxRegion.Bounds(o); }
                    o.transform.position = at - b.center;
                    pictures++;
                }
        BraxRegion.Warm(root, "NOVA: ciepłe światło loży W", new Vector3(wx0 + 1.9f, y + 2.6f, Nova.center.z), .9f, 6f);
        BraxRegion.Warm(root, "NOVA: ciepłe światło loży E", new Vector3(wx1 - 1.9f, y + 2.6f, Nova.center.z), .9f, 6f);
        BraxRegion.Note("NOVA: loże " + lounges + ", rośliny " + plants + ", obrazy " + pictures + " (ściany x " + wx0.ToString("F1") + ".." + wx1.ToString("F1") + ", z " + wz0.ToString("F1") + ".." + wz1.ToString("F1") + ")");
    }
}

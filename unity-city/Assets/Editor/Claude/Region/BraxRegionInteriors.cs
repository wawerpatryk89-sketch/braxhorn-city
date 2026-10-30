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
    }

    static bool Inside(Bounds b, Renderer r) { return r && b.Intersects(r.bounds); }

    static void NutritionShop(Transform cityRoot)
    {
        var plaster = BraxRegion.Pbr("Nutrition: tynk ścian", 1.5f, new Color(.93f, .91f, .87f), "white_plaster_02", "rectangular_facade_tiles");
        var walnut = BraxRegion.Pbr("Nutrition: ściana tylna z drewna", 1.2f, new Color(.62f, .50f, .40f), "wood_plank_wall", "wood_planks");
        var floor = BraxRegion.Pbr("Nutrition: deska podłogowa", 1.6f, null, "plank_flooring_02", "wood_floor", "wood_planks");
        int walls = 0, floors = 0;
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!Inside(Nutrition, r)) continue;
            string n = r.gameObject.name;
            if (n.StartsWith("mineral side wall", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = plaster; walls++; }
            else if (n.StartsWith("rear mineral wall", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = walnut; walls++; }
            else if (n.StartsWith("Nutrition continuous original-level floor", StringComparison.OrdinalIgnoreCase)) { r.sharedMaterial = floor; floors++; }
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

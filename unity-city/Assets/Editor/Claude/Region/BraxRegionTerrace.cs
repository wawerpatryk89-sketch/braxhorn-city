using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BRAXHORN CITY — taras widokowy wieżowca: kwiaty, rabaty, pergole, rośliny w donicach.
// Uzupełnia taras Codexa/Claude (deski, szklana balustrada, donice, ławki, stoliki):
// kwiaty Poly Haven (gazania, ursinia, barwinek, glistnik) w donicach i rabatach z granitowym
// obrzeżem, dwie pergole nad stolikami, drzewka w donicach. Środek dachu (miejsce walki 7×7 m),
// przejście do windy i tylne narożniki (HVAC) zostają wolne. Nic nie świeci.
public static class BraxRegionTerrace
{
    const string TowerRoot = "R1_TOWER accessible premium lobby lift and roof";
    const float ShaftZ = 53f;

    public static void Apply(Transform cityRoot)
    {
        var tower = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == TowerRoot);
        if (!tower) { BraxRegion.Note("taras: brak wieżowca " + TowerRoot); return; }
        var bld = BraxCityArchitecture.StreetBuildings.FirstOrDefault(x => x.id == BraxCityStreetSkyline.AccessibleTowerId);
        var plan = BraxCityStreetSkyline.HighRises.FirstOrDefault(x => x.id == BraxCityStreetSkyline.AccessibleTowerId);
        if (bld == null || plan == null) { BraxRegion.Note("taras: brak planu wieżowca"); return; }
        float roofY = BraxCityStreetSkyline.RoofHeight(bld, plan.additionalFloors);
        float x0 = bld.x0, x1 = bld.x1, z0 = bld.z0, z1 = bld.z1, cx = (x0 + x1) * .5f, width = x1 - x0;

        var root = BraxRegion.Root(tower, "CLAUDE taras: kwiaty, rabaty i pergole");

        var flowers = new List<GameObject>();
        foreach (var id in new[] { "flower_gazania", "flower_ursinia", "periwinkle_plant", "celandine_01", "flower_heliophila" })
        { var m = BraxRegion.Model(id); if (m) flowers.Add(m); }
        var leafy = new List<GameObject>();
        foreach (var id in new[] { "anthurium_botany_01", "calathea_orbifolia_01", "potted_plant_01", "fern_02" })
        { var m = BraxRegion.Model(id); if (m) leafy.Add(m); }
        var treePot = BraxRegion.Model("pachira_aquatica_01", "potted_plant_04");
        var planter = BraxRegion.Model("planter_box_02", "planter_box_01");
        var clayPot = BraxRegion.Model("planter_pot_clay", "potted_plant_02");

        var granite = BraxRegion.Pbr("Taras: granitowe obrzeże rabaty", 1.2f, null, "granite_tile", "granite_tile_02");
        var soil = BraxRegion.Pbr("Taras: ziemia rabaty", 1.0f, null, "brown_mud_leaves_01", "forest_floor", "wood_planks");
        var beam = BraxRegion.Pbr("Taras: belki pergoli", 1.0f, new Color(.55f, .42f, .30f), "wood_planks", "wooden_planks");

        var duel = new Vector3(cx, roofY, z0 + 7.4f);
        Func<Vector3, float, bool> inRing = (p, r) => Mathf.Abs(p.x - duel.x) < 3.5f + r && Mathf.Abs(p.z - duel.z) < 3.5f + r;
        Func<Vector3, bool> nearLift = p => Mathf.Abs(p.z - ShaftZ) < 3.4f && Mathf.Abs(p.x - cx) < 3.2f;
        Func<Vector3, bool> rearCorner = p => p.z > z1 - 4.5f && (p.x < x0 + 4.5f || p.x > x1 - 4.5f);

        int planted = 0, plants = 0;

        // 1) Kwiaty w istniejących donicach wzdłuż boków (te same pozycje co donice Claude).
        if (flowers.Count > 0)
            for (float z = z0 + 4.0f; z < z1 - 5.5f; z += 4.0f)
            {
                if (Mathf.Abs(z - ShaftZ) < 3.2f) continue;
                foreach (float x in new[] { x0 + .95f, x1 - .95f })
                    for (int k = 0; k < 3; k++)
                    {
                        var at = new Vector3(x, roofY + .34f, z - .5f + k * .5f);
                        var o = BraxRegion.Put(root, BraxRegion.Pick(flowers), "Taras: kwiaty w donicy", at, BraxRegion.Range(0, 360), 1f, false, .55f);
                        if (o) planted++;
                    }
            }

        // 2) Rabata wzdłuż tylnej balustrady (między narożnikami HVAC, poza windą): obrzeże, ziemia, kwiaty i liście.
        float bz = z1 - 1.15f;
        foreach (var seg in new[] { new Vector2(x0 + 4.8f, cx - 3.6f), new Vector2(cx + 3.6f, x1 - 4.8f) })
        {
            if (seg.y - seg.x < 2f) continue;
            float mx = (seg.x + seg.y) * .5f, len = seg.y - seg.x;
            BraxRegion.Box("Taras: obrzeże rabaty", root, new Vector3(mx, roofY + .18f, bz), new Vector3(len, .36f, 1.1f), granite, true);
            BraxRegion.Box("Taras: ziemia rabaty", root, new Vector3(mx, roofY + .33f, bz), new Vector3(len - .2f, .08f, .9f), soil);
            for (float x = seg.x + .4f; x < seg.y - .3f; x += .45f)
            {
                var at = new Vector3(x, roofY + .36f, bz + BraxRegion.Range(-.28f, .28f));
                var src = (flowers.Count > 0 && (BraxRegion.Rnd.Next(3) != 0 || leafy.Count == 0)) ? BraxRegion.Pick(flowers) : (leafy.Count > 0 ? BraxRegion.Pick(leafy) : null);
                if (!src) continue;
                var o = BraxRegion.Put(root, src, "Taras: rabata", at, BraxRegion.Range(0, 360), 1f, false, .7f);
                if (o) planted++;
            }
        }

        // 3) Rabaty przed frontową balustradą po bokach ławek (kwiaty widoczne z miasta).
        foreach (var seg in new[] { new Vector2(x0 + 2.2f, cx - width * .25f - 1.6f), new Vector2(cx + width * .25f + 1.6f, x1 - 2.2f) })
        {
            if (seg.y - seg.x < 1.5f) continue;
            float mx = (seg.x + seg.y) * .5f, len = seg.y - seg.x, fz = z0 + 1.1f;
            BraxRegion.Box("Taras: obrzeże rabaty frontowej", root, new Vector3(mx, roofY + .18f, fz), new Vector3(len, .36f, .9f), granite, true);
            BraxRegion.Box("Taras: ziemia rabaty frontowej", root, new Vector3(mx, roofY + .33f, fz), new Vector3(len - .2f, .08f, .7f), soil);
            for (float x = seg.x + .35f; x < seg.y - .3f; x += .42f)
            {
                if (flowers.Count == 0) break;
                var at = new Vector3(x, roofY + .36f, fz + BraxRegion.Range(-.2f, .2f));
                var o = BraxRegion.Put(root, BraxRegion.Pick(flowers), "Taras: rabata frontowa", at, BraxRegion.Range(0, 360), 1f, false, .55f);
                if (o) planted++;
            }
        }

        // 4) Pergole nad stolikami (jak w tarasie Claude: x0+3.2 i x1-3.2, z = min(z1-6.5, ShaftZ-4.5)).
        float tz = Mathf.Min(z1 - 6.5f, ShaftZ - 4.5f);
        foreach (float px in new[] { x0 + 3.2f, x1 - 3.2f })
        {
            var p = new Vector3(px, roofY, tz);
            if (width < 12f || inRing(p, 2.6f) || Mathf.Abs(px - cx) < 3.2f) continue;
            float s = 2.1f, h = 2.6f;
            foreach (float ox in new[] { -s, s })
                foreach (float oz in new[] { -s, s })
                    BraxRegion.Box("Pergola: słup", root, p + new Vector3(ox, h * .5f, oz), new Vector3(.14f, h, .14f), beam, true);
            foreach (float oz in new[] { -s, s })
                BraxRegion.Box("Pergola: belka", root, p + new Vector3(0, h + .08f, oz), new Vector3(s * 2 + .5f, .16f, .14f), beam);
            for (float ox = -s - .2f; ox <= s + .21f; ox += .55f)
                BraxRegion.Box("Pergola: listwa", root, p + new Vector3(ox, h + .24f, 0), new Vector3(.08f, .14f, s * 2 + .6f), beam);
            if (clayPot)
                foreach (float oz in new[] { -s + .35f, s - .35f })
                {
                    var o = BraxRegion.Put(root, clayPot, "Pergola: donica gliniana", p + new Vector3(px < cx ? -s + .4f : s - .4f, 0, oz), BraxRegion.Range(0, 360), 1f, true, .6f);
                    if (o) plants++;
                    if (o && flowers.Count > 0)
                    {
                        var f = BraxRegion.Put(root, BraxRegion.Pick(flowers), "Pergola: kwiaty w donicy", p + new Vector3(px < cx ? -s + .4f : s - .4f, .3f, oz), BraxRegion.Range(0, 360), 1f, false, .45f);
                        if (f) planted++;
                    }
                }
        }

        // 5) Drzewka w donicach przy wejściu na dach (obok windy, poza przejściem) i w wolnych narożnikach przednich.
        if (treePot)
            foreach (var at in new[] { new Vector3(cx - 3.0f, roofY, ShaftZ - 1.0f), new Vector3(cx + 3.0f, roofY, ShaftZ - 1.0f) })
            {
                if (nearLift(at) || inRing(at, 1f)) continue;
                var o = BraxRegion.Put(root, treePot, "Taras: drzewko w donicy przy windzie", at, BraxRegion.Range(0, 360), 1f, true, 2.6f);
                if (o) plants++;
            }
        if (planter && leafy.Count > 0)
            foreach (var at in new[] { new Vector3(x0 + 2.4f, roofY, z0 + 3.2f), new Vector3(x1 - 2.4f, roofY, z0 + 3.2f) })
            {
                if (inRing(at, 1.2f)) continue;
                var o = BraxRegion.Put(root, planter, "Taras: donica z liśćmi", at, 0f, 1f, true, .6f);
                if (o)
                {
                    plants++;
                    var l = BraxRegion.Put(root, BraxRegion.Pick(leafy), "Taras: liście w donicy", at + Vector3.up * .3f, BraxRegion.Range(0, 360), 1f, false, .9f);
                    if (l) plants++;
                }
            }

        // 6) Winda: raport (mechanika bez zmian; sprawdzamy tylko, czy istnieje i ma oba przystanki).
        var cabin = tower.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Moving physical elevator cabin");
        var lift = tower.GetComponentInChildren<BraxCityElevator>(true);
        BraxRegion.Note(cabin && lift
            ? "winda: kabina OK, przystanki y=" + lift.lowerY.ToString("F2") + " i y=" + lift.upperY.ToString("F2") + " (dach " + roofY.ToString("F2") + ")"
            : "winda: UWAGA brak kabiny lub sterownika windy");

        // 7) Przed wejściem do wieżowca od ulicy: donice z kwiatami po bokach portalu.
        if (planter)
            foreach (float px in new[] { cx - 4.1f, cx + 4.1f })
            {
                var at = new Vector3(px, .15f, z0 - 1.1f);
                var o = BraxRegion.Put(cityRoot, planter, "Wejście wieżowca: donica z kwiatami", at, 0f, 1f, true, .6f);
                if (o && flowers.Count > 0)
                    for (int k = 0; k < 3; k++)
                    {
                        var f = BraxRegion.Put(cityRoot, BraxRegion.Pick(flowers), "Wejście wieżowca: kwiaty", at + new Vector3(-.45f + k * .45f, .3f, 0), BraxRegion.Range(0, 360), 1f, false, .5f);
                        if (f) planted++;
                    }
            }

        BraxRegion.Rep.terraceFlowers = planted;
        BraxRegion.Rep.terracePlants = plants;
        BraxRegion.Note("taras: kwiatów " + planted + ", roślin " + plants + ", roofY=" + roofY.ToString("F2"));
    }
}

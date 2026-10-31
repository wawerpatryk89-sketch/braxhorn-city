using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BRAXHORN CITY — zieleń naturalna i mała architektura w całym regionie R1.
// Trawniki (podwórko, boiska, nabrzeże, cypel): krzewy, kępy trawy, kwiaty, kamienie i dodatkowe
// drzewa Poly Haven (bez odchudzania), ławki i latarnie. Plaża: koła ratunkowe, boje, skrzynie,
// kamienie przy brzegu, trawy wydmowe. Chodniki przy ulicy: donice z kwiatami, kosze, hydrant.
// Wszystko stawiane tylko na wolnym, płaskim podłożu (kolizje sprawdzane), z dala od jezdni.
public static class BraxRegionVegetation
{
    const float StreetZ0 = 19.5f, StreetZ1 = 37.5f;   // pas ulicy z chodnikami: tu nie sadzimy na trawie

    static List<GameObject> Load(params string[] ids)
    {
        var list = new List<GameObject>();
        foreach (var id in ids) { var m = BraxRegion.Model(id); if (m) list.Add(m); }
        return list;
    }

    public static void Apply(Transform cityRoot)
    {
        var root = BraxRegion.Root(cityRoot, "CLAUDE zieleń naturalna i mała architektura");
        var shrubs = Load("shrub_01", "shrub_02", "shrub_03", "park_shrub");
        var grass = Load("grass_medium_01", "grass_medium_02");
        var flowers = Load("flower_gazania", "flower_ursinia", "periwinkle_plant", "celandine_01");
        var rocks = Load("rock_moss_set_01", "boulder_01", "stone_01", "park_rock");
        var shoreRocks = Load("coast_rocks_01", "coast_rocks_03", "sand_rocks_small_01");
        var trees = Load("island_tree_03", "pine_tree_01", "fir_tree_01");
        var bench = BraxRegion.Model("modular_street_seating", "painted_wooden_bench", "wooden_picnic_table");
        var lamp = BraxRegion.Model("street_lamp_02", "street_lamp_01");
        var bin = BraxRegion.Model("metal_trash_can");
        var planter = BraxRegion.Model("planter_box_02", "planter_box_01", "planter_box_03");
        var lifebuoy = BraxRegion.Model("lifebuoy");
        var buoy = BraxRegion.Model("ocean_buoy", "lateral_sea_marker");
        var crate = BraxRegion.Model("wooden_crate_02", "wooden_crate_01", "wooden_barrels_01");
        var hydrant = BraxRegion.Model("fire_hydrant");

        // 1) Trawniki: wszystkie renderery trawy (stare miasto + renowacja), bez ulicy i bez cypla plaży.
        var lawns = new List<Bounds>();
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!BraxRegion.IsGrassRenderer(r)) continue;
            if (BraxRegion.Under(r.transform, "CLAUDE")) continue;
            var b = r.bounds;
            if (b.size.x < 3f || b.size.z < 3f || b.size.y > 3f || b.max.y > 2.5f || b.min.y < -3f) continue;
            lawns.Add(b);
        }
        int shrubN = 0, grassN = 0, flowerN = 0, rockN = 0, treeN = 0, propN = 0;
        const int MaxShrubs = 60, MaxGrass = 80, MaxFlowers = 50, MaxRocks = 24, MaxTrees = 20;
        foreach (var lawn in lawns)
        {
            float step = 4.5f;
            for (float x = lawn.min.x + 1.5f; x < lawn.max.x - 1.5f; x += step)
                for (float z = lawn.min.z + 1.5f; z < lawn.max.z - 1.5f; z += step)
                {
                    float px = x + BraxRegion.Range(-1.4f, 1.4f), pz = z + BraxRegion.Range(-1.4f, 1.4f);
                    if (pz > StreetZ0 && pz < StreetZ1) continue;
                    if (px > 66f && pz > 100f) continue;                 // cypel i plaża: osobno
                    RaycastHit h;
                    if (!BraxRegion.Ground(px, pz, lawn.min.y - 1f, lawn.max.y + 1f, out h)) continue;
                    var hr = h.collider.GetComponent<MeshRenderer>();
                    bool onGrass = hr ? BraxRegion.IsGrassRenderer(hr) : Mathf.Abs(h.point.y - lawn.max.y) < .35f;
                    if (!onGrass) continue;
                    var p = h.point;
                    double roll = BraxRegion.Rnd.NextDouble();
                    if (roll < .10 && trees.Count > 0 && treeN < MaxTrees)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * 4f, new Vector3(5f, 8f, 5f))) continue;
                        var o = BraxRegion.Put(root, BraxRegion.Pick(trees), "Zieleń: drzewo", p, BraxRegion.Range(0, 360), 1f, false, 13f);
                        if (o)
                        {
                            var c = new GameObject("Zieleń: pień kolizja").AddComponent<CapsuleCollider>();
                            c.transform.SetParent(root, false); c.transform.position = p + Vector3.up * 1.5f; c.radius = .3f; c.height = 3f;
                            treeN++;
                        }
                    }
                    else if (roll < .40 && shrubs.Count > 0 && shrubN < MaxShrubs)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .9f, new Vector3(2.2f, 1.6f, 2.2f))) continue;
                        var o = BraxRegion.Put(root, BraxRegion.Pick(shrubs), "Zieleń: krzew", p, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.3f), false, 2.4f, .04f);
                        if (o)
                        {
                            var b = BraxRegion.Bounds(o);
                            var c = new GameObject("Zieleń: krzew kolizja").AddComponent<CapsuleCollider>();
                            c.transform.SetParent(root, false); c.transform.position = new Vector3(b.center.x, p.y + b.size.y * .45f, b.center.z);
                            c.radius = Mathf.Max(.3f, Mathf.Min(b.size.x, b.size.z) * .35f); c.height = b.size.y * .9f;
                            shrubN++;
                        }
                    }
                    else if (roll < .72 && grass.Count > 0 && grassN < MaxGrass)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .4f, new Vector3(1.2f, .7f, 1.2f))) continue;
                        if (BraxRegion.Put(root, BraxRegion.Pick(grass), "Zieleń: kępa trawy", p, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.4f), false, .9f, .03f)) grassN++;
                    }
                    else if (roll < .92 && flowers.Count > 0 && flowerN < MaxFlowers)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .4f, new Vector3(1.0f, .7f, 1.0f))) continue;
                        if (BraxRegion.Put(root, BraxRegion.Pick(flowers), "Zieleń: kwiaty", p, BraxRegion.Range(0, 360), 1f, false, .6f, .02f)) flowerN++;
                    }
                    else if (rocks.Count > 0 && rockN < MaxRocks)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .5f, new Vector3(1.8f, .9f, 1.8f))) continue;
                        if (BraxRegion.Put(root, BraxRegion.Pick(rocks), "Zieleń: kamień", p, BraxRegion.Range(0, 360), 1f, true, 1.2f, .08f)) rockN++;
                    }
                }
        }

        // 2) Ławki i latarnie przy większych trawnikach (krawędź od strony podwórka), kosze obok ławek.
        foreach (var lawn in lawns.Where(l => l.size.x * l.size.z > 300f).Take(6))
        {
            var edge = new Vector3(lawn.center.x, lawn.max.y, lawn.min.z + 1.2f);
            if (edge.z > StreetZ0 - 2f && edge.z < StreetZ1 + 2f) edge = new Vector3(lawn.min.x + 1.2f, lawn.max.y, lawn.center.z);
            RaycastHit h;
            if (!BraxRegion.Ground(edge.x, edge.z, lawn.min.y - 1f, lawn.max.y + 1f, out h)) continue;
            var p = h.point;
            if (bench && BraxRegion.Free(p + Vector3.up * .6f, new Vector3(2.6f, 1.2f, 1.4f)))
            {
                if (BraxRegion.Put(root, bench, "Podwórko: ławka", p, 0f, 1f, true, 1.2f)) propN++;
                if (bin && BraxRegion.Free(p + new Vector3(1.8f, .5f, 0), new Vector3(.8f, 1f, .8f)))
                    if (BraxRegion.Put(root, bin, "Podwórko: kosz", p + new Vector3(1.8f, 0, 0), 0f, 1f, true, 1.0f)) propN++;
            }
            var lp = p + new Vector3(-2.6f, 0, 0);
            if (lamp && BraxRegion.Free(lp + Vector3.up * 2.5f, new Vector3(.8f, 5f, .8f)))
                if (BraxRegion.Put(root, lamp, "Podwórko: latarnia", lp, 0f, 1f, true, 5.5f)) propN++;
        }

        // 3) Plaża i cypel: trawy wydmowe na granicy piasku, kamienie przy brzegu, koła ratunkowe, boje, skrzynie.
        var beach = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLowerInvariant().Contains("port + beach"));
        int beachN = 0;
        if (beach)
        {
            var sand = beach.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.ToLowerInvariant().Contains("suchy piasek")).ToArray();
            var wet = beach.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.ToLowerInvariant().Contains("mokry piasek")).ToArray();
            foreach (var r in sand)
            {
                var b = r.bounds;
                // trawy wydmowe wzdłuż tylnej krawędzi plaży (od strony lądu) i kwiaty nadmorskie
                for (float z = b.min.z + 1f; z < b.max.z - 1f; z += 2.2f)
                {
                    float x = b.min.x + BraxRegion.Range(.4f, 2.4f);
                    RaycastHit h;
                    if (!BraxRegion.Ground(x, z, b.min.y - 1f, b.max.y + 1f, out h) || h.collider.transform != r.transform) continue;
                    if (!BraxRegion.Free(h.point + Vector3.up * .4f, new Vector3(1.2f, .7f, 1.2f))) continue;
                    var src = grass.Count > 0 && BraxRegion.Rnd.Next(4) != 0 ? (grass.Count > 0 ? BraxRegion.Pick(grass) : null) : (flowers.Count > 0 ? BraxRegion.Pick(flowers) : null);
                    if (src && BraxRegion.Put(root, src, "Plaża: trawa wydmowa", h.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.3f), false, .8f, .03f)) beachN++;
                }
                // kamienie przy brzegu: na mokrym piasku, w linii wody
                foreach (var w in wet)
                {
                    var wb = w.bounds;
                    for (float z = wb.min.z + 2f; z < wb.max.z - 2f; z += 6.5f)
                    {
                        float x = wb.min.x + BraxRegion.Range(.5f, 3.5f);
                        RaycastHit h;
                        if (!BraxRegion.Ground(x, z, wb.min.y - 2f, wb.max.y + 1f, out h) || h.collider.transform != w.transform) continue;
                        if (shoreRocks.Count == 0 || !BraxRegion.Free(h.point + Vector3.up * .5f, new Vector3(2.2f, 1f, 2.2f))) continue;
                        if (BraxRegion.Put(root, BraxRegion.Pick(shoreRocks), "Brzeg: kamienie", h.point, BraxRegion.Range(0, 360), 1f, true, 1.1f, .15f)) beachN++;
                    }
                }
                // koło ratunkowe na słupku i skrzynie przy budce ratownika
                var tower = beach.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLowerInvariant().Contains("budka ratownika"));
                if (tower)
                {
                    var tb = BraxRegion.Bounds(tower.gameObject);
                    var at = new Vector3(tb.max.x + 1.2f, 0, tb.center.z);
                    RaycastHit h;
                    if (BraxRegion.Ground(at.x, at.z, b.min.y - 1f, b.max.y + 1f, out h))
                    {
                        var post = BraxRegion.Pbr("Plaża: słupek drewniany", 1f, null, "wood_planks_grey", "wood_planks", "wooden_planks");
                        BraxRegion.Box("Plaża: słupek koła ratunkowego", root, h.point + Vector3.up * .9f, new Vector3(.12f, 1.8f, .12f), post, true);
                        if (lifebuoy)
                        {
                            var o = BraxRegion.Put(root, lifebuoy, "Plaża: koło ratunkowe", h.point + new Vector3(0, 1.1f, -.09f), 0f, 1f, false, .75f);
                            if (o) beachN++;
                        }
                        if (crate && BraxRegion.Free(h.point + new Vector3(0, .5f, 1.6f), new Vector3(1.4f, 1f, 1.4f)))
                            if (BraxRegion.Put(root, crate, "Plaża: skrzynie ratownika", h.point + new Vector3(0, 0, 1.6f), BraxRegion.Range(0, 360), 1f, true, 1.0f)) beachN++;
                    }
                }
            }
            // boje w wodzie, 18–26 m od brzegu, poza pomostem
            if (buoy)
            {
                var jetty = beach.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLowerInvariant().Contains("pomost"));
                Bounds? jb = jetty ? BraxRegion.Bounds(jetty.gameObject) : (Bounds?)null;
                foreach (var r in wet)
                {
                    var wb = r.bounds;
                    for (float z = wb.min.z + 4f; z < wb.max.z - 4f; z += 11f)
                    {
                        var at = new Vector3(wb.max.x + BraxRegion.Range(16f, 24f), BraxCityWaterBuild.WaterY, z);
                        if (jb.HasValue && Mathf.Abs(at.z - jb.Value.center.z) < jb.Value.extents.z + 6f && at.x < jb.Value.max.x + 8f) continue;
                        var o = BraxRegion.Put(root, buoy, "Morze: boja", at, BraxRegion.Range(0, 360), 1f, false, 1.6f, .45f);
                        if (o) beachN++;
                    }
                }
            }
        }
        else BraxRegion.Note("zieleń: nie znaleziono korzenia portu i plaży");

        // 4) Chodniki przy ulicy: donice z kwiatami co ok. 24 m, naprzemiennie strony, blisko fasad; hydrant przy sklepie.
        int streetN = 0;
        if (planter)
            for (float x = -96f; x < 62f; x += 24f)
            {
                float z = ((int)((x + 96f) / 24f)) % 2 == 0 ? 21.3f : 35.4f;
                RaycastHit h;
                if (!BraxRegion.Ground(x, z, -.5f, 1.0f, out h)) continue;
                if (!BraxRegion.Free(h.point + Vector3.up * .5f, new Vector3(1.6f, 1f, 1.2f))) continue;
                var o = BraxRegion.Put(root, planter, "Ulica: donica z kwiatami", h.point, 0f, 1f, true, .6f);
                if (!o) continue;
                streetN++;
                if (flowers.Count > 0)
                    for (int k = 0; k < 3; k++)
                        if (BraxRegion.Put(root, BraxRegion.Pick(flowers), "Ulica: kwiaty w donicy", h.point + new Vector3(-.45f + k * .45f, .3f, 0), BraxRegion.Range(0, 360), 1f, false, .5f)) streetN++;
            }
        if (hydrant)
        {
            RaycastHit h;
            if (BraxRegion.Ground(26.5f, 21.1f, -.5f, 1f, out h) && BraxRegion.Free(h.point + Vector3.up * .5f, new Vector3(.8f, 1f, .8f)))
                if (BraxRegion.Put(root, hydrant, "Ulica: hydrant", h.point, 0f, 1f, true, .9f)) streetN++;
        }

        BraxRegion.Rep.shrubs = shrubN; BraxRegion.Rep.grassClumps = grassN; BraxRegion.Rep.flowers = flowerN;
        BraxRegion.Rep.rocks = rockN; BraxRegion.Rep.trees = treeN; BraxRegion.Rep.streetProps = streetN + propN; BraxRegion.Rep.beachProps = beachN;
        BraxRegion.Note("zieleń: trawników " + lawns.Count + ", krzewów " + shrubN + ", kęp " + grassN + ", kwiatów " + flowerN + ", kamieni " + rockN + ", drzew " + treeN + ", plaża " + beachN + ", ulica " + (streetN + propN));
    }
}

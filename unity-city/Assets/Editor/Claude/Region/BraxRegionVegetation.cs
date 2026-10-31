using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

        // 0) Łąka leśna za promenadą (oryginalny CityMesh_257, x −60..84 po cięciu morza, z 112.5..240): materiał
        //    glTFast bez _Color, więc BraxClaudeGround.ReplaceGrass jej nie poznał. Dostaje tę samą prawdziwą trawę.
        var grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/ClaudeGround/Ziemia__prawdziwa_trawa_Grass004.mat");
        int meadow = 0;
        if (grassMat)
            foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
            {
                if (r.name != "CityMesh_257" || !r.enabled) continue;
                r.sharedMaterials = r.sharedMaterials.Select(_ => grassMat).ToArray(); meadow++;
            }
        BraxRegion.Note("łąka leśna (CityMesh_257) z prawdziwą trawą: " + meadow + (grassMat ? "" : " (brak materiału trawy)"));

        // 1) Trawniki: wszystkie renderery trawy (stare miasto + renowacja), bez ulicy i bez cypla plaży,
        //    tylko w regionie R1 (x −20..90, z 40..178): podwórko, promenada, las za promenadą.
        var region = new Rect(-20f, 40f, 110f, 138f);
        var lawns = new List<Bounds>();
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!BraxRegion.IsGrassRenderer(r)) continue;
            if (BraxRegion.Under(r.transform, "CLAUDE")) continue;
            var b = r.bounds;
            if (b.size.x < 3f || b.size.z < 3f || b.size.y > 6f || b.max.y > 4f || b.min.y < -4f) continue;
            if (b.max.x < region.xMin || b.min.x > region.xMax || b.max.z < region.yMin || b.min.z > region.yMax) continue;
            var min = Vector3.Max(b.min, new Vector3(region.xMin, b.min.y, region.yMin));
            var max = Vector3.Min(b.max, new Vector3(region.xMax, b.max.y, region.yMax));
            var clipped = new Bounds(); clipped.SetMinMax(min, max);
            lawns.Add(clipped);
        }
        int shrubN = 0, grassN = 0, flowerN = 0, rockN = 0, treeN = 0, propN = 0, sampled = 0, rejGround = 0, rejFree = 0;
        const int MaxShrubs = 60, MaxGrass = 80, MaxFlowers = 50, MaxRocks = 24, MaxTrees = 20;
        foreach (var lawn in lawns)
        {
            float step = 4.5f;
            for (float x = lawn.min.x + 1.5f; x < lawn.max.x - 1.5f; x += step)
                for (float z = lawn.min.z + 1.5f; z < lawn.max.z - 1.5f; z += step)
                {
                    float px = x + BraxRegion.Range(-1.4f, 1.4f), pz = z + BraxRegion.Range(-1.4f, 1.4f);
                    if (pz > StreetZ0 && pz < StreetZ1) continue;
                    RaycastHit h;
                    if (!BraxRegion.Ground(px, pz, lawn.min.y - 1f, lawn.max.y + 1f, out h)) continue;
                    if (BraxRegion.Under(h.collider.transform, "BRAXHORN PORT") || BraxRegion.Under(h.collider.transform, "CLAUDE")) { rejGround++; continue; }   // piasek i dobudowane: osobno
                    var hr = h.collider.GetComponent<MeshRenderer>();
                    bool onGrass = hr ? BraxRegion.IsGrassRenderer(hr) : Mathf.Abs(h.point.y - lawn.max.y) < .6f;
                    if (!onGrass) { rejGround++; continue; }
                    sampled++;
                    var p = h.point;
                    double roll = BraxRegion.Rnd.NextDouble();
                    if (roll < .10 && trees.Count > 0 && treeN < MaxTrees)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * 4.3f, new Vector3(4f, 7.4f, 4f))) { rejFree++; continue; }
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
                        if (!BraxRegion.Free(p + Vector3.up * 1.0f, new Vector3(2.0f, 1.4f, 2.0f))) { rejFree++; continue; }
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
                        if (!BraxRegion.Free(p + Vector3.up * .55f, new Vector3(1.2f, .6f, 1.2f))) { rejFree++; continue; }
                        if (BraxRegion.Put(root, BraxRegion.Pick(grass), "Zieleń: kępa trawy", p, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.4f), false, .9f, .03f)) grassN++;
                    }
                    else if (roll < .92 && flowers.Count > 0 && flowerN < MaxFlowers)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .55f, new Vector3(1.0f, .6f, 1.0f))) { rejFree++; continue; }
                        if (BraxRegion.Put(root, BraxRegion.Pick(flowers), "Zieleń: kwiaty", p, BraxRegion.Range(0, 360), 1f, false, .6f, .02f)) flowerN++;
                    }
                    else if (rocks.Count > 0 && rockN < MaxRocks)
                    {
                        if (!BraxRegion.Free(p + Vector3.up * .65f, new Vector3(1.8f, .8f, 1.8f))) { rejFree++; continue; }
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

        // 3) Plaża na cyplu (BraxHarborBeachBuild.CapeBeach): trawy wydmowe na tylnej krawędzi (północ i zachód),
        //    kamienie w linii wody (południe i wschód), koło ratunkowe i skrzynie przy budce, boje w zatoce i na wschód.
        var beach = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLowerInvariant().Contains("port + beach"));
        int beachN = 0;
        if (beach)
        {
            float cx0 = BraxHarborBeachBuild.CapeX0, cx1 = BraxHarborBeachBuild.CapeX1, cz0 = BraxHarborBeachBuild.CapeZ0, cz1 = BraxHarborBeachBuild.CapeZ1;
            bool OnDry(float x, float z, out RaycastHit h)
            {
                if (!BraxRegion.Ground(x, z, -1f, 1.5f, out h)) return false;
                return h.collider.name.ToLowerInvariant().Contains("suchy piasek");
            }
            // trawy wydmowe i kwiaty nadmorskie: tylna krawędź północna (z≈cz1−1.5) i zachodnia (x≈cx0+1.5)
            var backEdge = new List<Vector2>();
            for (float x = cx0 + 2f; x < cx1 - 1f; x += 2.2f) backEdge.Add(new Vector2(x + BraxRegion.Range(-.6f, .6f), cz1 - BraxRegion.Range(.6f, 2.6f)));
            for (float z = cz0 + 2.5f; z < cz1 - 2f; z += 2.2f) backEdge.Add(new Vector2(cx0 + BraxRegion.Range(.5f, 2.5f), z + BraxRegion.Range(-.6f, .6f)));
            foreach (var e in backEdge)
            {
                RaycastHit h; if (!OnDry(e.x, e.y, out h)) continue;
                if (!BraxRegion.Free(h.point + Vector3.up * .4f, new Vector3(1.2f, .7f, 1.2f))) continue;
                var src = grass.Count > 0 && BraxRegion.Rnd.Next(4) != 0 ? BraxRegion.Pick(grass) : (flowers.Count > 0 ? BraxRegion.Pick(flowers) : (grass.Count > 0 ? BraxRegion.Pick(grass) : null));
                if (src && BraxRegion.Put(root, src, "Plaża: trawa wydmowa", h.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.3f), false, .8f, .03f)) beachN++;
            }
            // kamienie w linii wody: co 6.5 m wzdłuż południowego i wschodniego brzegu (1–3 m za krawędzią plateau)
            var shoreLine = new List<Vector2>();
            for (float x = 92f; x < cx1 - 3f; x += 6.5f) shoreLine.Add(new Vector2(x + BraxRegion.Range(-1.5f, 1.5f), cz0 - BraxRegion.Range(1f, 3f)));
            for (float z = cz0 + 4f; z < cz1 - 3f; z += 6.5f) shoreLine.Add(new Vector2(cx1 + BraxRegion.Range(1f, 3f), z + BraxRegion.Range(-1.5f, 1.5f)));
            foreach (var e in shoreLine)
            {
                if (Mathf.Abs(e.x - BraxHarborBeachBuild.CapeJettyX) < 5f && e.y < cz0 + 1f) continue;   // nie przy pomoście
                RaycastHit h; if (!BraxRegion.Ground(e.x, e.y, -3f, 1f, out h) || !h.collider.name.ToLowerInvariant().Contains("piasek")) continue;
                if (shoreRocks.Count == 0 || !BraxRegion.Free(h.point + Vector3.up * .5f, new Vector3(2.2f, 1f, 2.2f))) continue;
                if (BraxRegion.Put(root, BraxRegion.Pick(shoreRocks), "Brzeg: kamienie", h.point, BraxRegion.Range(0, 360), 1f, true, 1.1f, .15f)) beachN++;
            }
            // koło ratunkowe na słupku i skrzynie przy budce ratownika
            var tower = beach.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.ToLowerInvariant().Contains("budka ratownika"));
            if (tower)
            {
                var tb = BraxRegion.Bounds(tower.gameObject);
                RaycastHit h;
                if (OnDry(tb.max.x + 1.6f, tb.center.z, out h))
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
            // boje: w zatoce 22 m od brzegu (z dala od pomostu) i na wschód od cypla
            if (buoy)
            {
                var buoys = new List<Vector3>();
                for (float x = 96f; x < cx1; x += 12f) if (Mathf.Abs(x - BraxHarborBeachBuild.CapeJettyX) > 9f) buoys.Add(new Vector3(x, BraxCityWaterBuild.WaterY, cz0 - BraxRegion.Range(19f, 25f)));
                for (float z = cz0 + 6f; z < cz1; z += 12f) buoys.Add(new Vector3(cx1 + BraxRegion.Range(19f, 25f), BraxCityWaterBuild.WaterY, z));
                foreach (var at in buoys)
                {
                    var o = BraxRegion.Put(root, buoy, "Morze: boja", at, BraxRegion.Range(0, 360), 1f, false, 1.6f, .45f);
                    if (o) beachN++;
                }
            }
        }
        else BraxRegion.Note("zieleń: nie znaleziono korzenia portu i plaży");

        // 4) Chodniki przy ulicy: donice z kwiatami co ok. 24 m, naprzemiennie strony, blisko fasad; hydrant przy sklepie.
        int streetN = 0;
        if (planter)
            for (float x = -96f; x < 62f; x += 24f)
            {
                float z = ((int)((x + 96f) / 24f)) % 2 == 0 ? 22.1f : 34.6f;   // środek chodnika (20.6..23.6 i 33.4..36.1)
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
            if (BraxRegion.Ground(27.5f, 22.3f, -.5f, 1f, out h) && BraxRegion.Free(h.point + Vector3.up * .55f, new Vector3(.7f, .9f, .7f)))
                if (BraxRegion.Put(root, hydrant, "Ulica: hydrant", h.point, 0f, 1f, true, .9f)) streetN++;
        }

        BraxRegion.Rep.shrubs += shrubN; BraxRegion.Rep.grassClumps = grassN; BraxRegion.Rep.flowers = flowerN;
        BraxRegion.Rep.rocks = rockN; BraxRegion.Rep.trees += treeN; BraxRegion.Rep.streetProps = streetN + propN; BraxRegion.Rep.beachProps = beachN;
        BraxRegion.Note("zieleń: próbek " + sampled + " (odrzucone podłoże " + rejGround + ", zajęte " + rejFree + "), trawniki: " + string.Join("; ", lawns.Take(6).Select(l => l.center.x.ToString("F0") + "," + l.center.z.ToString("F0") + " " + l.size.x.ToString("F0") + "x" + l.size.z.ToString("F0"))) + " | trawników " + lawns.Count + ", krzewów " + shrubN + ", kęp " + grassN + ", kwiatów " + flowerN + ", kamieni " + rockN + ", drzew " + treeN + ", plaża " + beachN + ", ulica " + (streetN + propN));
    }
}

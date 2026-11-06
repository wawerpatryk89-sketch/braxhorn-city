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

        // 3) Plaża: od przebudowy 2.11 całe wyposażenie plaży (trawy wydmowe, kamienie falochronu, koło ratunkowe, boje kąpieliska)
        //    stawia BraxNabrzezePlaza — stara plaża na cyplu (CapeBeach) i jej dekoracje usunięte (wykonawca „nabrzeże”).
        int beachN = 0;

        // 4) Chodniki przy ulicy: donice z kwiatami co ok. 24 m, naprzemiennie strony, blisko fasad; hydrant przy sklepie.
        int streetN = 0;
        if (planter)
            for (float x = -96f; x < 62f; x += 24f)
            {
                // Od strony jezdni, z bezpiecznym odsunięciem od krawężnika:
                // 1.5+ m przejścia przy fasadach, bez donic na osi drzwi.
                float z = ((int)((x + 96f) / 24f)) % 2 == 0 ? 23.0f : 34.0f;
                if (!BraxMiastoUlica.MiejsceWolneDlaDodatkow(x, z, .8f)) continue;
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
            if (BraxMiastoUlica.MiejsceWolneDlaDodatkow(27.5f, 23.0f, .40f) &&
                BraxRegion.Ground(27.5f, 23.0f, -.5f, 1f, out h) &&
                BraxRegion.Free(h.point + Vector3.up * .55f, new Vector3(.7f, .9f, .7f)))
                if (BraxRegion.Put(root, hydrant, "Ulica: hydrant", h.point, 0f, 1f, true, .9f)) streetN++;
        }

        BraxRegion.Rep.shrubs += shrubN; BraxRegion.Rep.grassClumps = grassN; BraxRegion.Rep.flowers = flowerN;
        BraxRegion.Rep.rocks = rockN; BraxRegion.Rep.trees += treeN; BraxRegion.Rep.streetProps = streetN + propN; BraxRegion.Rep.beachProps = beachN;
        BraxRegion.Note("zieleń: próbek " + sampled + " (odrzucone podłoże " + rejGround + ", zajęte " + rejFree + "), trawniki: " + string.Join("; ", lawns.Take(6).Select(l => l.center.x.ToString("F0") + "," + l.center.z.ToString("F0") + " " + l.size.x.ToString("F0") + "x" + l.size.z.ToString("F0"))) + " | trawników " + lawns.Count + ", krzewów " + shrubN + ", kęp " + grassN + ", kwiatów " + flowerN + ", kamieni " + rockN + ", drzew " + treeN + ", plaża " + beachN + ", ulica " + (streetN + propN));
    }
}

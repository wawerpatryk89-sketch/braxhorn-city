using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — MORZE (przebudowa 2.11, wykonawca „nabrzeże”). Zgłoszenie: zdjęcie CI 13.1 nr 35 — Brax STOI NA WODZIE
// (na wyspę dało się dojść pieszo po morzu).
// Przyczyna: źródłowe miasto ma niewidzialną podłogę y = 0 (BaseFloor, „Original virtual floor”) pod całą mapą, także
// pod morzem x 84…884. Stara wersja tego kroku usuwała ją tylko w zatoce (x 84…166) — reszta morza była „chodnikiem”.
// Teraz:
//  1) podłoga y=0 usunięta nad CAŁYM morzem (x ≥ 84), z wyjątkiem łąki starego miasta na północy (x 84…150, z 178…240),
//     na której stoi las — tam podłoga zostaje (las ma po czym chodzić);
//  2) dno morza: kolizja głęboko (y −7) — na plaży, falochronie i wyspie stoją ich własne kolizje terenu;
//  3) strażnik głębokiej wody (BraxWodaGlebia, runtime): kąpiel po pas przy plaży, głębiej → powrót na suchy ląd;
//  4) las za plażą: drzewa na ściółce plaży (z 168…177) — teren stawia BraxNabrzezePlaza (stare cięcia łąki i lasu z tego
//     kroku przeniesione tam: wycinamy całe źródło w prostokącie plaży, zanim położymy nowy teren).
// Poziom wody: y = −1.7 (BraxCityWaterBuild.WaterY).
public static class BraxRegionSea
{
    public static void Apply(Transform root)
    {
        var physics = BraxClaudeGround.SourcePhysics;
        if (!physics) { BraxRegion.Note("morze: brak kolizji źródła (SourcePhysics), pomijam"); return; }
        // 1) podłoga y=0 nad morzem
        int boxes = 0;
        foreach (var fl in new[]
        {
            Bx(84f, 900f, -520f, 178f),     // całe morze na południe od lasu (zatoka, marina, port, wyspa, otwarte morze)
            Bx(150f, 900f, 178f, 470f),     // na wschód od łąki
            Bx(84f, 150f, 240f, 470f),      // za łąką
        })
        {
            try { boxes += BraxCityMeshCut.SubtractBoxColliders(physics, fl); }
            catch (Exception e) { BraxRegion.Note("morze: podłoga nad wodą: " + e.Message); }
        }
        // Audyt po usunięciu: na wodzie nie wolno pozostawić kolizji "Original virtual floor".
        // Badamy wyłącznie źródłowe podłoże, NIE pomosty ani jednostki wody.
        Physics.SyncTransforms();
        int podlogiPozostaly = 0;
        foreach (float x in new[] { 96f, 125f, 160f, 205f, 260f })
            foreach (float z in new[] { -45f, -5f, 35f, 75f, 95f })
                foreach (var hit in Physics.RaycastAll(new Vector3(x, 1.5f, z), Vector3.down, 2.5f,
                             ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider || !hit.collider.transform.IsChildOf(physics) || hit.point.y < -.1f) continue;
                    string nazwa = hit.collider.name.ToLowerInvariant();
                    if (!nazwa.Contains("virtual floor") && !nazwa.Contains("basefloor")) continue;
                    podlogiPozostaly++;
                    if (podlogiPozostaly <= 8)
                        Debug.LogError("BRAXHORN_MORZE_BLEDNA_PODLOGA: " + hit.collider.name +
                                       " nad wodą w " + hit.point.ToString("F1"));
                }
        if (podlogiPozostaly > 0)
            BraxRegion.Note("morze: NIEWIDZIALNE PODLOGI NAD WODĄ PO WYCIĘCIU = " + podlogiPozostaly +
                            " — scena NIEGOTOWA do publikacji");
        else
            Debug.Log("BRAXHORN_MORZE_PODLOGA w próbce portu i morza = 0 (25 punktów)");
        // 2) dno morza (głęboko; płycizny, plaża i wyspa mają własne kolizje terenu)
        var dno = new GameObject("CLAUDE morze: dno (kolizja)"); dno.transform.SetParent(root, false);
        var bc = dno.AddComponent<BoxCollider>(); bc.center = new Vector3(492f, -7.5f, -25f); bc.size = new Vector3(816f, 1f, 990f);
        // 3) strażnik głębokiej wody (bez renderera i kolizji — zostaje w scenie głównej przy podziale na obszary)
        var g = new GameObject("BRAXHORN nabrzeże: głęboka woda (powrót na brzeg)"); g.transform.SetParent(root, false);
        var straz = g.AddComponent<BraxWodaGlebia>(); straz.woda = BraxNabrzeze.Woda;
        // 4) las za plażą
        int trees = TreeBelt(root);
        BraxRegion.Note("morze: usunięte podłogi y=0 nad wodą " + boxes + ", dno y −7, strażnik głębokiej wody, drzew za plażą " + trees + ", poziom wody y " + BraxNabrzeze.Woda);
        Debug.Log("BRAXHORN_MORZE podłoga nad wodą usunięta (" + boxes + " brył), woda y " + BraxNabrzeze.Woda + ", dno y −7, strażnik głębokości " + straz.glebokosc + " m");
    }

    static Bounds Bx(float x0, float x1, float z0, float z1)
    {
        // Poprzednio górna granica .02 m pozostawiała część źródłowej podłogi, jeśli
        // jej wierzch leżał na wysokości chodnika .15 m. To dawało chodzenie po morzu.
        // Usuwamy pozostałości TYLKO w masce morza (zachowując osobną chronioną łąkę).
        var b = new Bounds();
        b.SetMinMax(new Vector3(x0, -1.1f, z0), new Vector3(x1, .40f, z1));
        return b;
    }

    static int TreeBelt(Transform root)
    {
        var drzewo = BraxRegion.Model("drzewo_verdant");
        var shrubs = new List<GameObject>();
        foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03", "park_shrub" }) { var m = BraxRegion.Model(id); if (m) shrubs.Add(m); }
        if (!drzewo) { BraxRegion.Note("las za plażą: brak modelu drzewa"); return 0; }
        var parent = BraxRegion.Root(root, "CLAUDE las za plażą");
        Physics.SyncTransforms();
        int planted = 0, bushes = 0;
        var spots = new List<Vector3>();
        for (float x = 67f; x <= 146f; x += 5.2f)
        {
            spots.Add(new Vector3(x + BraxRegion.Range(-1.4f, 1.4f), 0, 169.5f + BraxRegion.Range(-1f, 1f)));
            spots.Add(new Vector3(x + 2.6f + BraxRegion.Range(-1.4f, 1.4f), 0, 175f + BraxRegion.Range(-1.2f, 1.2f)));
        }
        foreach (var s in spots)
        {
            if (s.x > BraxNabrzezePlaza.LiniaWody(s.z) - 9f) continue;                 // nie przy wodzie
            if (s.x < BraxNabrzezePlaza.MurX && s.z < BraxNabrzezePlaza.Z1 + 1.5f) continue;   // nie na bulwarze
            RaycastHit hit; if (!BraxRegion.Ground(s.x, s.z, -1.5f, 3f, out hit)) continue;
            if (!BraxRegion.Free(hit.point + Vector3.up * 5f, new Vector3(2.2f, 8f, 2.2f))) continue;
            var o = BraxRegion.Put(parent, drzewo, "Las za plażą: drzewo", hit.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.15f), false, 16f);
            if (!o) continue;
            var c = new GameObject("Las za plażą: pień kolizja").AddComponent<CapsuleCollider>();
            c.transform.SetParent(parent, false); c.transform.position = hit.point + Vector3.up * 1.5f; c.radius = .35f; c.height = 3f;
            planted++;
            if (shrubs.Count > 0 && planted % 2 == 0)
            {
                var sp = hit.point + new Vector3(BraxRegion.Range(-2.5f, 2.5f), 0, BraxRegion.Range(-3f, -1.5f));
                RaycastHit h2;
                if (BraxRegion.Ground(sp.x, sp.z, -1.5f, 3f, out h2) && BraxRegion.Free(h2.point + Vector3.up * 1f, new Vector3(1.8f, 1.4f, 1.8f)))
                    if (BraxRegion.Put(parent, BraxRegion.Pick(shrubs), "Las za plażą: krzew", h2.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.3f), false, 2.4f, .04f)) bushes++;
            }
        }
        BraxRegion.Rep.trees += planted; BraxRegion.Rep.shrubs += bushes;
        return planted;
    }
}

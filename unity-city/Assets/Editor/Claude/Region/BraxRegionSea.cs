using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — cypel: stara łąka i las z prostokąta plaży, zatoka bez niewidzialnej podłogi, las za plażą.
//
// Diagnoza (build 62be06c, zdjęcia 13–17): jachty „zatopione w trawie”, pomost widoczny tylko palami.
// W oryginalnym mieście (braxhorn-city.glb, po lustrze X glTFast) półwysep leśny leży NAD wodą:
//   CityMesh_257  łąka   x -60..150  y -0.6..0   z 112.5..240  (kolor 0.48/0.55/0.35 = zielony)
//   CityMesh_195/200/206/208/209/210/211  las i skarpy  x 0..135  y -24..35  z 94..229
// Decyzja właściciela (31.10, film): CAŁY ten narożnik (x 84..150, z 112.5..160) to plaża, za nią dużo drzew,
// marina w zatoce na południe. Piasek stawia BraxHarborBeachBuild.CapeBeach; tu:
//  1) łąka, sosny i skarpy wycięte z prostokąta plaży (razem z pasem lądu x 66..84 za promenadą),
//  2) kolizje oryginału w tych bryłach,
//  3) niewidzialna „Original virtual floor” y=0 usunięta nad zatoką i przy wschodnim brzegu → dno −2.6 m (woda po pas),
//  4) pas drzew (Poly Haven, pełna jakość) za plażą: północ z 160..174 i zachód x 50..66.
public static class BraxRegionSea
{
    const float Shore = 84.3f, East = BraxHarborBeachBuild.CapeX1, Z0 = BraxHarborBeachBuild.CapeZ0, Z1 = BraxHarborBeachBuild.CapeZ1 + 2f;
    const float LandX0 = 66f, LandZ0 = 113.7f;   // od 113.7: krawężnik promenady (CityMesh_232/233) sięga z=113.6

    public static void Apply(Transform root)
    {
        var original = BraxClaudeGround.SourceCity;
        var physics = BraxClaudeGround.SourcePhysics;
        if (!original) { BraxRegion.Note("cypel: brak hierarchii oryginału (SourceCity), pomijam"); return; }
        string outDir = BraxRegion.Out + "/Cuts";
        Directory.CreateDirectory(outDir);

        // 1) Łąka, las i skarpy w prostokącie plaży nad wodą (x 84.3..150, z 112.5..162, cała wysokość).
        var sea = new Bounds(new Vector3((Shore + East) * .5f, 20f, (Z0 + Z1) * .5f), new Vector3(East - Shore, 90f, Z1 - Z0));
        int seaMeshes = Cut(original, sea, outDir, "cape_beach");
        // 2) Sosny i krzaki nad lądową częścią plaży (x 66..84.3, z 113.7..162), od −0.3 m w górę.
        var land = new Bounds(new Vector3((LandX0 + Shore) * .5f, 30f, (LandZ0 + Z1) * .5f), new Vector3(Shore - LandX0, 60.6f, Z1 - LandZ0));
        int landMeshes = Cut(original, land, outDir, "beach_trees");
        // 3) Kolizje oryginału (siatki fizyki bez renderera) w tych samych bryłach.
        int colTris = CutColliders(physics, sea) + CutColliders(physics, land);
        // 4) Niewidzialna podłoga y=0 nad wodą: zatoka na południe od cypla (x 84.3..166, z 60..112.5) i wschodni brzeg
        //    (x 150..166, z 112.5..162) → dno morza 2.6 m pod lustrem (woda po pas). Pod piaskiem podłoga zostaje
        //    tylko tam, gdzie piasek leży wyżej (plateau), a pod spadkiem (6 m od brzegu) też ją usuwamy.
        int boxes = 0;
        foreach (var fl in new[]
        {
            new Bounds(new Vector3((Shore + 166f) * .5f, -.09f, (60f + Z0 + 7f) * .5f), new Vector3(166f - Shore, .6f, Z0 + 7f - 60f)),       // zatoka + 7 m spadku
            new Bounds(new Vector3((East - 7f + 166f) * .5f, -.09f, (Z0 + Z1) * .5f), new Vector3(166f - East + 7f, .6f, Z1 - Z0)),           // wschód + 7 m spadku
        })
        {
            try { boxes += BraxCityMeshCut.SubtractBoxColliders(physics, fl); }
            catch (Exception e) { BraxRegion.Note("cypel: podłoga nad wodą: " + e.Message); }
        }
        Seabed(root, "CLAUDE dno zatoki (woda po pas)", new Vector3((Shore + 166f) * .5f, -2.75f, (60f + Z0) * .5f), new Vector3(166f - Shore, .3f, Z0 - 60f));
        Seabed(root, "CLAUDE dno przy wschodnim brzegu", new Vector3((East + 166f) * .5f, -2.75f, (Z0 + Z1) * .5f), new Vector3(166f - East, .3f, Z1 - Z0));

        // 5) Las za plażą: pas drzew na północ (z 160..174, x 84..150) i na zachód (x 50..66, z 114..162), pełna jakość.
        int trees = TreeBelt(root);

        BraxRegion.Note("cypel: wycięte siatki łąki/lasu z plaży " + seaMeshes + " + nad piaskiem " + landMeshes
                        + ", trójkątów kolizji " + colTris + ", podłóg nad wodą " + boxes + ", drzew za plażą " + trees);
    }

    static void Seabed(Transform root, string name, Vector3 center, Vector3 size)
    {
        var bed = new GameObject(name); bed.transform.SetParent(root, false);
        var bc = bed.AddComponent<BoxCollider>(); bc.center = center; bc.size = size;
        GameObjectUtility.SetStaticEditorFlags(bed, StaticEditorFlags.BatchingStatic);
    }

    static int TreeBelt(Transform root)
    {
        var trees = new List<GameObject>();
        foreach (var id in new[] { "island_tree_03", "pine_tree_01", "fir_tree_01" }) { var m = BraxRegion.Model(id); if (m) trees.Add(m); }
        var shrubs = new List<GameObject>();
        foreach (var id in new[] { "shrub_01", "shrub_02", "shrub_03", "park_shrub" }) { var m = BraxRegion.Model(id); if (m) shrubs.Add(m); }
        if (trees.Count == 0) { BraxRegion.Note("cypel: brak modeli drzew"); return 0; }
        var parent = BraxRegion.Root(root, "CLAUDE las za plażą");
        Physics.SyncTransforms();
        int planted = 0, bushes = 0;
        var spots = new List<Vector3>();
        // dwa rzędy na północ od plaży (przesunięte o pół kroku), co 5 m z losowym rozrzutem
        for (float x = 86f; x <= 148f; x += 5f) { spots.Add(new Vector3(x + BraxRegion.Range(-1.2f, 1.2f), 0, 162.5f + BraxRegion.Range(-.8f, .8f))); spots.Add(new Vector3(x + 2.5f + BraxRegion.Range(-1.2f, 1.2f), 0, 168f + BraxRegion.Range(-1.5f, 1.5f))); }
        // rząd na zachód (za pasem lądu, przy promenadzie)
        for (float z = 115f; z <= 160f; z += 5.5f) spots.Add(new Vector3(58f + BraxRegion.Range(-3f, 3f), 0, z + BraxRegion.Range(-1f, 1f)));
        foreach (var s in spots)
        {
            RaycastHit hit; if (!BraxRegion.Ground(s.x, s.z, -1f, 3f, out hit)) continue;
            if (BraxRegion.Under(hit.collider.transform, "BRAXHORN PORT")) continue;      // nie na piasku
            if (!BraxRegion.Free(hit.point + Vector3.up * 5f, new Vector3(2.2f, 8f, 2.2f))) continue;
            var o = BraxRegion.Put(parent, BraxRegion.Pick(trees), "Las za plażą: drzewo", hit.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.15f), false, 16f);
            if (!o) continue;
            var c = new GameObject("Las za plażą: pień kolizja").AddComponent<CapsuleCollider>();
            c.transform.SetParent(parent, false); c.transform.position = hit.point + Vector3.up * 1.5f; c.radius = .35f; c.height = 3f;
            planted++;
            if (shrubs.Count > 0 && planted % 2 == 0)
            {
                var sp = hit.point + new Vector3(BraxRegion.Range(-2.5f, 2.5f), 0, BraxRegion.Range(1.5f, 3f));
                RaycastHit h2; if (BraxRegion.Ground(sp.x, sp.z, -1f, 3f, out h2) && !BraxRegion.Under(h2.collider.transform, "BRAXHORN PORT")
                    && BraxRegion.Free(h2.point + Vector3.up * 1f, new Vector3(1.8f, 1.4f, 1.8f)))
                    if (BraxRegion.Put(parent, BraxRegion.Pick(shrubs), "Las za plażą: krzew", h2.point, BraxRegion.Range(0, 360), BraxRegion.Range(.9f, 1.3f), false, 2.4f, .04f)) bushes++;
            }
        }
        BraxRegion.Rep.trees += planted; BraxRegion.Rep.shrubs += bushes;
        return planted;
    }

    static int Cut(Transform original, Bounds volume, string outDir, string label)
    {
        try { return BraxCityMeshCut.Subtract(original, volume, outDir, label); }
        catch (Exception e) { BraxRegion.Note("cypel: " + label + ": " + e.Message); return 0; }
    }

    // Siatki kolizji bez renderera: usuwamy trójkąty, których środek leży w bryle.
    static int CutColliders(Transform physics, Bounds volume)
    {
        if (!physics) return 0;
        int removedTotal = 0;
        foreach (var col in physics.GetComponentsInChildren<MeshCollider>(true))
        {
            if (!col.sharedMesh || col.GetComponent<MeshRenderer>() || !col.bounds.Intersects(volume)) continue;
            var src = col.sharedMesh; Vector3[] v;
            try { v = src.vertices; } catch { continue; }
            if (v == null || v.Length == 0) continue;
            var M = col.transform.localToWorldMatrix;
            var w = v.Select(p => M.MultiplyPoint3x4(p)).ToArray();
            var copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + "_kolizja_bez_plazy";
            int removed = 0;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tris = src.GetTriangles(s); var keep = new List<int>(tris.Length);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    var c = (w[tris[i]] + w[tris[i + 1]] + w[tris[i + 2]]) / 3f;
                    if (volume.Contains(c)) { removed++; continue; }
                    keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
                }
                copy.SetTriangles(keep, s);
            }
            if (removed == 0) { UnityEngine.Object.DestroyImmediate(copy); continue; }
            string p = BraxRegion.Out + "/Cuts/" + Guid.NewGuid().ToString("N").Substring(0, 8) + "_kolizja.asset";
            AssetDatabase.CreateAsset(copy, p);
            col.sharedMesh = copy;
            removedTotal += removed;
        }
        return removedTotal;
    }
}

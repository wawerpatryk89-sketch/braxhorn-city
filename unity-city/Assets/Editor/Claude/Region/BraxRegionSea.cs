using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — morze przy plaży i marinie bez starej łąki i lasu.
//
// Diagnoza (build 62be06c, zdjęcia 13–17): jachty „zatopione w trawie”, pomost widoczny tylko palami.
// W oryginalnym mieście (braxhorn-city.glb, po lustrze X glTFast) półwysep leśny leży NAD wodą:
//   CityMesh_257  łąka   x -60..150  y -0.6..0   z 112.5..240  (kolor 0.48/0.55/0.35 = zielony)
//   CityMesh_195/200/206/208/209/210/211  las i skarpy  x 0..135  y -24..35  z 94..229
// Port (BraxHarborBeachBuild) wycina trawę tylko na lądzie (x ≤ 84), więc od brzegu (x=84)
// do x≈135 łąka i sosny zostają na wysokości 0 — 1.7 m NAD lustrem wody.
// Tu: prostokąt morza przy plaży i marinie wycinany ze starych siatek (renderery + kolizje),
// niewidzialna podłoga y=0 nad wodą usunięta i zastąpiona dnem (woda po pas), drzewa znad piasku usunięte.
public static class BraxRegionSea
{
    const float Shore = 84.3f, East = 150f, Z0 = 112.5f, Z1 = 172f;   // z do 172: jacht na kotwicy 20 m (z≈156+10)
    const float LandX0 = 66f, BeachZ0 = 113.7f, BeachZ1 = 153f;   // od 113.7: krawężnik promenady (CityMesh_232/233) sięga z=113.6

    public static void Apply(Transform root)
    {
        var original = BraxClaudeGround.SourceCity;
        var physics = BraxClaudeGround.SourcePhysics;
        if (!original) { BraxRegion.Note("morze: brak hierarchii oryginału (SourceCity), pomijam"); return; }
        string outDir = BraxRegion.Out + "/Cuts";
        Directory.CreateDirectory(outDir);

        // 1) Łąka, las i skarpy nad morzem (x 84.3..150, z 112.5..172, cała wysokość).
        var sea = new Bounds(new Vector3((Shore + East) * .5f, 20f, (Z0 + Z1) * .5f), new Vector3(East - Shore, 90f, Z1 - Z0));
        int seaMeshes = Cut(original, sea, outDir, "sea_forest");
        // 2) Sosny i krzaki nad lądową częścią plaży (piasek x 66..84.3, z 113..153), od -0.3 m w górę.
        var land = new Bounds(new Vector3((LandX0 + Shore) * .5f, 30f, (BeachZ0 + BeachZ1) * .5f), new Vector3(Shore - LandX0, 60.6f, BeachZ1 - BeachZ0));
        int landMeshes = Cut(original, land, outDir, "beach_trees");
        // 3) Kolizje oryginału (siatki fizyki bez renderera) w tych samych bryłach.
        int colTris = CutColliders(physics, sea) + CutColliders(physics, land);
        // 4) Niewidzialna podłoga y=0 („Original virtual floor”) nad wodą → dno morza 2.6 m pod lustrem (woda po pas).
        var floor = new Bounds(new Vector3((Shore + East) * .5f, -.09f, (100f + Z1) * .5f), new Vector3(East - Shore, .6f, Z1 - 100f));
        int boxes = 0;
        try { boxes = BraxCityMeshCut.SubtractBoxColliders(physics, floor); }
        catch (Exception e) { BraxRegion.Note("morze: podłoga nad wodą: " + e.Message); }
        var bed = new GameObject("CLAUDE dno morza przy plaży i marinie");
        bed.transform.SetParent(root, false);
        var bc = bed.AddComponent<BoxCollider>();
        bc.center = new Vector3((Shore + East) * .5f, -2.75f, (100f + Z1) * .5f);
        bc.size = new Vector3(East - Shore, .3f, Z1 - 100f);
        GameObjectUtility.SetStaticEditorFlags(bed, StaticEditorFlags.BatchingStatic);

        BraxRegion.Note("morze: wycięte siatki lasu/łąki nad wodą " + seaMeshes + ", nad piaskiem " + landMeshes
                        + ", trójkątów kolizji " + colTris + ", podłóg nad wodą " + boxes);
    }

    static int Cut(Transform original, Bounds volume, string outDir, string label)
    {
        try { return BraxCityMeshCut.Subtract(original, volume, outDir, label); }
        catch (Exception e) { BraxRegion.Note("morze: " + label + ": " + e.Message); return 0; }
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
            var copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + "_kolizja_bez_morza";
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

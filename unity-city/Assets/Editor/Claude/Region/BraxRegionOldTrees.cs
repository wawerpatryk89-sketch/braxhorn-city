using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — stare „kule na patykach” na podwórku (zdjęcia 11/12 z buildu 62be06c).
// W oryginalnym glb trzy drzewa-atrapy to DWIE scalone siatki (dlatego BraxClaudeTrees ich nie łapie):
//   CityMesh_178  korony: 12 kul (4 na drzewo), tekstura city_baked_70
//   CityMesh_177  pnie:   3 słupki, kolor 0.07/0.04/0.03
// Pozycje (Unity, po lustrze X): (54.0, 68.0), (-12.0, 94.0), (30.0, 96.5).
// Tu: obie siatki wyłączone (renderer + kolizja), w ich miejscu prawdziwe drzewa z Poly Haven.
public static class BraxRegionOldTrees
{
    static readonly Vector3[] Spots =
    {
        new Vector3(54.0f, .15f, 68.0f), new Vector3(-12.0f, .15f, 94.0f), new Vector3(30.0f, .15f, 96.5f),
    };
    static readonly string[] Meshes = { "CityMesh_177", "CityMesh_178" };

    public static void Apply(Transform root)
    {
        var original = BraxClaudeGround.SourceCity;
        var physics = BraxClaudeGround.SourcePhysics;
        int renderersOff = 0, collidersOff = 0;
        foreach (var scope in new[] { original, physics, root })
        {
            if (!scope) continue;
            foreach (var t in scope.GetComponentsInChildren<Transform>(true))
            {
                if (!Meshes.Contains(t.name)) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) if (r.enabled) { r.enabled = false; renderersOff++; }
                foreach (var c in t.GetComponentsInChildren<Collider>(true)) if (c.enabled) { c.enabled = false; collidersOff++; }
            }
        }

        var parent = BraxRegion.Root(root, "CLAUDE drzewa zamiast kul na patykach");
        var trees = new List<GameObject>();
        foreach (var id in new[] { "island_tree_03", "pine_tree_01", "fir_tree_01" }) { var m = BraxRegion.Model(id); if (m) trees.Add(m); }
        int planted = 0;
        if (trees.Count > 0)
            foreach (var s in Spots)
            {
                RaycastHit hit; var floor = s;
                if (BraxRegion.Ground(s.x, s.z, -1f, 3f, out hit)) floor = hit.point;
                var o = BraxRegion.Put(parent, trees[planted % trees.Count], "Podwórko: drzewo", floor, BraxRegion.Range(0, 360), 1f, false, 14f);
                if (!o) continue;
                var c = new GameObject("Podwórko: pień kolizja").AddComponent<CapsuleCollider>();
                c.transform.SetParent(parent, false); c.transform.position = floor + Vector3.up * 1.5f; c.radius = .35f; c.height = 3f;
                // Stary pień ma osobną kolizję w hierarchii fizyki (słup 0.5 m) — wycinamy wąski słup.
                if (physics)
                    try { BraxCityMeshCut.SubtractBoxColliders(physics, new Bounds(new Vector3(s.x, 2.6f, s.z), new Vector3(1.4f, 4.6f, 1.4f))); }
                    catch (System.Exception e) { BraxRegion.Note("stare drzewa: kolizja pnia: " + e.Message); }
                planted++;
            }
        else BraxRegion.Note("stare drzewa: brak modeli drzew (island_tree_03 / pine_tree_01 / fir_tree_01)");
        BraxRegion.Rep.trees += planted;
        BraxRegion.Note("stare drzewa-atrapy: wyłączone renderery " + renderersOff + ", kolizje " + collidersOff + ", posadzone " + planted);
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Owner 24.11 00:50 ("dałaś nam ten lepszy piasek na tej plaży?"): the seafront villa's private beach has the city beach's
// sand already (the site's sand is the foundation's sand_dry.mat, which BraxChampionsPlaza.Piasek turns into ChampionsPiasek
// - Aerial Sand without the tyre tracks, the wet band at the waterline - before the villas are built), but its sea was the
// plain far sea: no surf, no run-up, no clear shallows. Now as the island's (BraxChampionsWyspaMorze): the beach's real sea
// (ChampionsMorze) over the seafront's water window - the site's patch from its south rim up to the far sea's east part's
// top (BraxChampionsHorizon.Sea), with its own shore field from the site's ground (the same Grunt its ground mesh is made
// of, on the same 2 m grid): the waves come in to the private beach, break and climb its sand between its stone walls; the
// far sea leaves a hole of exactly this window. The shore runs along x (the waves vary along it as on the city beach).
//  - the ground the field sees is the ground one sees (BraxChampionsWilleDzialki.Widoczny): the site's, and in its rim band
//    (the outer 25 m, where its ground goes under the far land) the far land's mesh where that is higher - its coast there;
//  - either side the window ends on the far land's coast (the region's quay west, the far sea east): the surf and the
//    run-up die out over its outer Boki metres (the shader's _Boki), clear of the private beach between its walls.
// Checked without Unity on the real code: wille_preview "nabrzeze_morze" (the far land over the beach, the field, the water).
public static class BraxChampionsNabrzezeMorze
{
    public const string Nazwa = "WILLA 3 | morze przy prywatnej plaży (fale na jej piasek)";
    public const float Gora = -99f;                 // the window's north edge (z, world nominal): over the far sea's east top (-100)
    public const float Dno = 15f;                   // the open sea's depth at the window's rim, under the sea (the coast's bed -10 at 70 m out)
    public const float Boki = 24f;                  // the surf's fade at the window's two sides (m; the beach's walls 26 and 32 m in)
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static float K => BraxClaudeOsiedle.K;
    public static int Trojkaty, Odcinki;

    static BraxChampionsWilleDzialki.Dzialka Nabrzeze()
    {
        foreach (var d in BraxChampionsWilleDzialki.Dzialki) if (d.rodzaj == BraxChampionsWilleDzialki.Rodzaj.Nabrzeze) return d;
        return null;
    }
    /// <summary>The hole in the far sea for the seafront's water (world nominal x0, z0, x1, z1).</summary>
    public static Vector4 Otwor { get { var d = Nabrzeze(); return new Vector4(d.x0, d.z0, d.x1, Gora); } }
    /// <summary>The seafront's water is in the open scene (only then may the far sea leave its hole).</summary>
    public static bool JestWScenie()
    {
        var o = GameObject.Find(Nazwa);
        var f = o ? o.GetComponent<MeshFilter>() : null;
        return o && o.GetComponent<MeshRenderer>() && f && f.sharedMesh;
    }

    /// <summary>The seafront's shore field and its water (under the site g).</summary>
    public static void Zbuduj(Transform g, BraxChampionsWilleDzialki.Dzialka d)
    {
        float sea = BraxChampionsWilleDzialki.SeaY;
        // ---- the field: the ground seen on the 2 m grid over the window, the rim (the far sea beyond) deep
        var pole = BraxChampionsWyspaWoda.Pole(d.x0, d.z0, d.x1, Gora, BraxChampionsWyspaMorze.Krok, K, sea, BraxChampionsWyspaMorze.PoleN,
                                               (x, z) => BraxChampionsWilleDzialki.Widoczny(d, x, z), (sea - Dno) * K, BraxChampionsWyspaMorze.RantPas * K);
        Odcinki = pole.Odcinki;
        var mat = BraxChampionsPlaza.MorzeZPola(pole, Out + "/Textures/nabrzeze_morze_brzeg.asset", Out + "/Materials/morze_nabrzeze.mat", "Mistrzowie morze przy prywatnej plaży", 0f);
        mat.SetFloat("_Boki", Boki * K); EditorUtility.SetDirty(mat);

        // ---- the water: a 2 m grid at the sea level over the window, the dry land left out, its outer band to the corners
        var v = new List<Vector3>(); var t = new List<int>();
        int dry = BraxChampionsWyspaWoda.Siatka(d.x0, d.z0, d.x1, Gora, BraxChampionsWyspaMorze.Krok, BraxChampionsWyspaMorze.Pas, K, sea * K,
            (x, z) => BraxChampionsWilleDzialki.Widoczny(d, x, z) > sea + BraxChampionsWyspaMorze.Suchy, v, t);
        var mesh = new Mesh { name = "wille nabrzeze morze", indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetTriangles(t, 0);
        var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up; mesh.normals = n;
        mesh.RecalculateBounds();
        var bb = mesh.bounds; bb.Expand(new Vector3(0f, 2f, 0f)); mesh.bounds = bb;          // the run-up and the swell lift it
        string folder = Out + "/Meshes/Wille"; System.IO.Directory.CreateDirectory(folder);
        string path = folder + "/wille_nabrzeze_morze.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(Nazwa); go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
        Trojkaty = t.Count / 3;
        Debug.Log("MISTRZOWIE_NABRZEZE_MORZE field=" + BraxChampionsWyspaMorze.PoleN + "x" + BraxChampionsWyspaMorze.PoleN + " window=(" + d.x0 + "," + d.z0 + ")-(" + d.x1 + "," + Gora +
                  ") waterline=" + pole.Odcinki + " landTexels=" + pole.komorekLadu + " triangles=" + Trojkaty + " dryCells=" + dry + " (owner 24.11: the private beach's real waves)");
        if (pole.Odcinki == 0) BraxBuildQuality.Warn("DO POPRAWKI NABRZEZE_MORZE_NO_WATERLINE the seafront's ground never crosses the sea level");
        // 24.11 the beach's sounds; the surf softer in the window's side fade, as the shader's _Boki
        BraxChampionsPlaza.SzumMorza(g, pole, 0f, w => Mathf.SmoothStep(0f, 1f, Mathf.Min(w.x - d.x0 * K, d.x1 * K - w.x) / (Boki * K)), "WILLA 3 | szum morza, fale i mewy");
    }
}

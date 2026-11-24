using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Owner 23.11 19:53 ("zobacz, gdzie ty zrobiłeś tą wyspę ... Wyspa jest na statek ... ta wyspa w ogóle ma być oddalona
// kawałek"): the villa island stood in the harbour, the cruise quay and a 289 m ship through it. It stands now alone in the
// open sea ~420 m off the shore (BraxChampionsWilleDzialki), and the sea round it is the beach's real sea (ChampionsMorze,
// owner 23.11 19:12 "woda jak prawdziwa"), with its own shore field: the waves run in to its beach from every side and climb
// its sand, the lagoon round it is clear turquoise over white sand, deep blue past its edge.
//  - the field (BraxChampionsWyspaWoda.Pole) over the island's patch from its ground (the same Grunt its ground mesh is made
//    of, on the same 2 m grid), the bed at the patch's rim taken down to the open sea's depth;
//  - the water (BraxChampionsWyspaWoda.Siatka): a 2 m grid at the sea level over the patch, dry land left out, its outer
//    10 m band a fan to the patch's corners; the far sea (BraxChampionsHorizon) has a hole of exactly the patch;
//  - the shader lets the swell die out at the field's rim (the edge with the flat far sea stays flat) and varies the waves
//    along the shore every way round (_WzdluzZ 1).
public static class BraxChampionsWyspaMorze
{
    public const float Krok = 2f, Pas = 10f, Suchy = .5f, RantPas = 20f;
    public const int PoleN = 512;
    public const string Nazwa = "WILLA 2 | morze wokół wyspy (fale do jej plaży, laguna)";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static float K => BraxClaudeOsiedle.K;
    public static int Trojkaty, Odcinki;

    static BraxChampionsWilleDzialki.Dzialka Wyspa()
    {
        foreach (var d in BraxChampionsWilleDzialki.Dzialki) if (d.rodzaj == BraxChampionsWilleDzialki.Rodzaj.Wyspa) return d;
        return null;
    }
    /// <summary>The hole in the far sea for the island's water (world nominal x0, z0, x1, z1): the island's patch.</summary>
    public static Vector4 Otwor { get { var d = Wyspa(); return new Vector4(d.x0, d.z0, d.x1, d.z1); } }
    /// <summary>The island's water is in the open scene (only then may the far sea leave its hole).</summary>
    public static bool JestWScenie()
    {
        var o = GameObject.Find(Nazwa);
        var f = o ? o.GetComponent<MeshFilter>() : null;
        return o && o.GetComponent<MeshRenderer>() && f && f.sharedMesh;
    }

    /// <summary>The island's shore field and its water (under the site g).</summary>
    public static void Zbuduj(Transform g, BraxChampionsWilleDzialki.Dzialka d)
    {
        float sea = BraxChampionsWilleDzialki.SeaY;
        // ---- the field: the island's ground on the 2 m grid (its pad's slabs and pool count as land), the rim deep
        var pole = BraxChampionsWyspaWoda.Pole(d.x0, d.z0, d.x1, d.z1, Krok, K, sea, PoleN, (x, z) => BraxChampionsWilleDzialki.Grunt(d, x, z),
                                               (sea - BraxChampionsWilleDzialki.WyspaDno - 5f) * K, RantPas * K);
        Odcinki = pole.Odcinki;
        var mat = BraxChampionsPlaza.MorzeZPola(pole, Out + "/Textures/wyspa_morze_brzeg.asset", Out + "/Materials/morze_wyspa.mat", "Mistrzowie morze wokół wyspy", 1f);

        // ---- the water
        var v = new List<Vector3>(); var t = new List<int>();
        int dry = BraxChampionsWyspaWoda.Siatka(d.x0, d.z0, d.x1, d.z1, Krok, Pas, K, sea * K,
            (x, z) => BraxChampionsWilleDzialki.Grunt(d, x, z) > sea + Suchy, v, t);
        var mesh = new Mesh { name = "wille wyspa morze", indexFormat = v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetTriangles(t, 0);
        var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up; mesh.normals = n;
        mesh.RecalculateBounds();
        var bb = mesh.bounds; bb.Expand(new Vector3(0f, 2f, 0f)); mesh.bounds = bb;          // the run-up and the swell lift it
        string folder = Out + "/Meshes/Wille"; System.IO.Directory.CreateDirectory(folder);
        string path = folder + "/wille_wyspa_morze.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(Nazwa); go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
        Trojkaty = t.Count / 3;
        Debug.Log("MISTRZOWIE_WYSPA_MORZE field=" + PoleN + "x" + PoleN + " patch=(" + d.x0 + "," + d.z0 + ")-(" + d.x1 + "," + d.z1 + ") waterline=" + pole.Odcinki +
                  " landTexels=" + pole.komorekLadu + " triangles=" + Trojkaty + " dryCells=" + dry + " (owner 23.11: the island alone in the open sea, real waves to its beach)");
        if (pole.Odcinki == 0) BraxBuildQuality.Warn("DO POPRAWKI WYSPA_MORZE_NO_WATERLINE the island's ground never crosses the sea level");
        BraxChampionsPlaza.SzumMorza(g, pole, 1f, null, "WILLA 2 | szum morza, fale i mewy");                       // 24.11 the beach's sounds
    }
}

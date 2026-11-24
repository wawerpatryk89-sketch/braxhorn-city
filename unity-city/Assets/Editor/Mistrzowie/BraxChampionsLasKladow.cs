using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T = BraxChampionsTorKladow;

/// <summary>THE QUAD TRACK'S FOREST (owner 24.11.2026 14:01-14:20: "w jakichś błotach albo po jakichś lasach", "Coś takiego lasy,
/// górki", the plan's map accepted with "Tak, ale więcej lasu"). A Polish pine forest round the track, the way his two photos
/// show it: the wooded hill the woods' trail winds over, a forest wall round the whole site (outside its fence and some way
/// in), clumps of pines and rows of trees between the loops of the motocross field, young trees at the forest's edges and
/// along the trail.
/// The trees are Poly Haven's scanned pines and firs (CC0: pine_tree_01 a/b/c, fir_tree_01 a/b/c - the fir from the owner's
/// list; fir_sapling, pine_sapling_small for the young ones). A phone cannot draw thousands of 2-8 million triangle scans
/// (the region dropped them in October for 1.2 GB of meshes), so each tree is two crossed cards - the project's billboard
/// standard - their pictures rendered from the scans themselves (unity-tools/mistrzowie/las: wypal_drzewa.js renders each
/// from the side at 0 and 90 degrees with the needles' own alpha, atlas_lasu.py packs the 16 into one 4096 x 2048 atlas:
/// Assets/Source/Mistrzowie/Las/las_atlas.png). Real sizes x K: the pines 15-20 m, the firs 14-19 m, each scaled 0.62-1.05
/// (younger trees), the saplings 1.2-2.9 m. Cut-out (0.5), both sides, normals up and out (lit as a crown, not as a sheet),
/// each tree tinted a little (no two alike) and darker low and deep in the forest (its own shade). One mesh and one draw
/// call per 60 m square (with its shadows). Every grown tree's trunk is a capsule collider (the quads hit them), the
/// saplings bend under a wheel (none). The ground under the forest is its needle litter (Poly Haven Forest Floor, 2.14 m):
/// BraxChampionsTorKladow.Teren splits the grass by Las().
/// Clear of the track (the woods' trail 2.5 m past its shoulders, the field and the start 6 m), the paddock, the freestyle
/// zone, the road, the LED board and the fence.</summary>
public static class BraxChampionsLasKladow
{
    static float K => BraxClaudeOsiedle.K;
    public const string Atlas = "Assets/Source/Mistrzowie/Las/las_atlas.png";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/TorKladow";
    public const int AtlasKol = 8, AtlasWier = 2, AtlasW = 4096, AtlasH = 2048;

    // ---------------------------------------------------------------- the cards (atlas_lasu.py prints this table)
    public struct Karta
    {
        public string nazwa; public float wys, ramka, stopa; public int komorka;
        // wys: the scanned tree's height; ramka: its picture's height (metres; the picture is half as wide, centred on the
        // trunk); stopa: the picture's bottom edge below the trunk's foot; komorka: its 0-degree cell (the 90-degree next)
        public Karta(string nazwa, float wys, float ramka, float stopa, int komorka) { this.nazwa = nazwa; this.wys = wys; this.ramka = ramka; this.stopa = stopa; this.komorka = komorka; }
        public bool Mloda => wys < 3f;
    }
    public static readonly Karta[] Karty =
    {
        new Karta("sosna_a", 20.37f, 20.78f, -0.08f, 0),
        new Karta("sosna_b", 14.88f, 16.60f, -0.09f, 2),
        new Karta("sosna_c", 17.54f, 17.89f, -0.09f, 4),
        new Karta("jodla_a", 18.93f, 19.30f, -0.08f, 6),
        new Karta("jodla_b", 14.06f, 14.34f, -0.09f, 8),
        new Karta("jodla_c", 14.52f, 14.81f, -0.09f, 10),
        new Karta("mloda_jodla", 1.30f, 1.78f, -0.01f, 12),
        new Karta("mloda_sosna", 1.30f, 1.74f, -0.02f, 14),
    };
    // the mix of grown trees: a Polish pine forest - Scots pine the most (~60 %), spruce / fir the rest (the sparse fir_c less)
    static readonly float[] Udzialy = { .22f, .20f, .18f, .16f, .14f, .10f };

    // ---------------------------------------------------------------- where the forest stands (nominal metres)
    public const float Oczko = 4.6f, OczkoMlodych = 3.2f, Kwadrat = 60f, PoleGestosc = .32f;
    public const float OdLesnejSciezki = 2.5f, OdToru = 6f, OdPaddocku = 12f, OdFreestyle = 18f, OdTablicy = 14f, OdPlotu = 2.5f;

    static float Hash(int x, int y)
    {
        unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16; return (h & 0xFFFFFF) / 16777215f; }
    }
    /// <summary>Smooth value noise 0..1 (the clumps' shapes).</summary>
    public static float Szum(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        float a = Mathf.Lerp(Hash(x0, y0), Hash(x0 + 1, y0), fx), b = Mathf.Lerp(Hash(x0, y0 + 1), Hash(x0 + 1, y0 + 1), fx);
        return Mathf.Lerp(a, b, fy);
    }
    static float Smooth(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }

    /// <summary>The forest (0..1, its edge at 0.45): the hill, the wall round the site (not on the city's side), the woods
    /// north-west of the hill, the field's clumps. The ground under it is the forest floor.</summary>
    public static float Las(float x, float z)
    {
        if (!T.WTorze(x, z)) return 0f;
        float kr = T.DoKrawedzi(x, z);
        // the wall: the rim band outside the fence and 12 m in; it thins out east of x -45 (the road, the city's edge)
        // the edges ragged, as a forest's are (+-7 m on the wall, +-20 m round the hill)
        float brzeg = Szum(x / 23f - 11f, z / 23f + 4f) - .5f;
        float wschod = Smooth(-20f, -45f, x);
        float sciana = (1f - Smooth(T.FenceInset - 2f, T.FenceInset + 12f, kr + brzeg * 14f)) * wschod;
        float dh = Mathf.Sqrt((x - T.GoraX) * (x - T.GoraX) + (z - T.GoraZ) * (z - T.GoraZ)) + brzeg * 40f;
        float gora = 1f - Smooth(T.GoraR * .9f, T.GoraR * 1.1f, dh);
        float nw = Smooth(-298f, -318f, x + brzeg * 16f) * Smooth(868f, 898f, z);
        float n = Szum(x / 42f + 3.1f, z / 42f - 5.7f) * .65f + Szum(x / 17f + 31f, z / 17f - 7f) * .35f;
        float kepa = Smooth(.60f, .70f, n) * wschod;
        return Mathf.Max(Mathf.Max(sciana, gora), Mathf.Max(nw, kepa));
    }
    /// <summary>The motocross field (its rows of trees between the loops).</summary>
    public static bool NaPolu(float x, float z) => x > -300f && x < -25f && z > 680f && z < 905f;

    /// <summary>How far (x, z) is past the track's shoulder, and the clearance there (the woods' trail or the field).</summary>
    static bool PrzyTorze(float x, float z, out float e, out float odstep)
    {
        e = 99f; odstep = OdToru;
        if (!T.Najblizszy(x, z, 30f, out int i, out float u)) return false;
        var o = T.Linia(); e = Mathf.Abs(u) - (o.w[i] * .5f + T.Pobocze); odstep = o.w[i] < 6f ? OdLesnejSciezki : OdToru;
        return true;
    }
    /// <summary>Kept clear of trees: the paddock, the freestyle zone, the road, the LED board, the fence line.</summary>
    public static bool Wolne(float x, float z)
    {
        if (T.DoPaddocku(x, z) < OdPaddocku || T.DoFreestyle(x, z) < OdFreestyle) return true;
        if (x > T.DrogaX0 - 15f && Mathf.Abs(z - T.DrogaZ) < T.DrogaPol + 8f) return true;
        if (x > T.PlotWschodX - 25f) return true;                                          // the city's side
        float dx = x - T.TablicaX, dz = z - T.TablicaZ; if (dx * dx + dz * dz < OdTablicy * OdTablicy) return true;
        // the fence (x0, z0 .. x1, z1): its line stays clear
        float fx0 = T.X0 + T.FenceInset, fz0 = T.Z0 + T.FenceInset, fz1 = T.Z1 - T.FenceInset, fx1 = T.PlotWschodX;
        bool wX = x > fx0 - OdPlotu && x < fx1 + OdPlotu, wZ = z > fz0 - OdPlotu && z < fz1 + OdPlotu;
        if (wZ && (Mathf.Abs(x - fx0) < OdPlotu || Mathf.Abs(x - fx1) < OdPlotu)) return true;
        if (wX && (Mathf.Abs(z - fz0) < OdPlotu || Mathf.Abs(z - fz1) < OdPlotu)) return true;
        return false;
    }

    // ---------------------------------------------------------------- the placing (one seed: the same forest every build)
    public struct Drzewo { public float x, z, y, skala, obrot, odcien; public int karta; }
    static List<Drzewo> drzewa;
    /// <summary>Trees others plant (the quad club's planters: BraxChampionsKlubKladow), at their own height; cleared each build.</summary>
    public static readonly List<Drzewo> Dodatkowe = new List<Drzewo>();
    public static int Drzew, Mlodych, Pni, Kwadratow, Trojkaty;
    public static List<Drzewo> Drzewa()
    {
        if (drzewa != null) return drzewa;
        drzewa = new List<Drzewo>(); var rnd = new System.Random(20261124);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        int Wybierz() { float r = (float)rnd.NextDouble(), s = 0f; for (int k = 0; k < Udzialy.Length; k++) { s += Udzialy[k]; if (r < s) return k; } return Udzialy.Length - 1; }
        // the grown trees: a jittered 4.6 m grid, each kept by the forest's density there
        for (float gx = T.X0 + 1f; gx < T.X1; gx += Oczko)
            for (float gz = T.Z0 + 1f; gz < T.Z1; gz += Oczko)
            {
                float x = gx + R(-.42f, .42f) * Oczko, z = gz + R(-.42f, .42f) * Oczko, los = R(0f, 1f);
                if (!T.WTorze(x, z) || Wolne(x, z)) continue;
                float d = Smooth(.40f, .55f, Las(x, z));
                if (NaPolu(x, z)) d = Mathf.Max(d, PoleGestosc);                            // the rows between the field's loops
                if (d <= 0f || los >= d) continue;
                if (PrzyTorze(x, z, out float e, out float od) && (e < od || los >= d * Mathf.Lerp(.55f, 1f, Smooth(od, od + 3f, e)))) continue;
                drzewa.Add(new Drzewo { x = x, z = z, karta = Wybierz(), skala = R(.62f, 1.05f), obrot = R(0f, 180f), odcien = R(-1f, 1f) });
            }
        // the young trees: at the forest's edges and along the trail, a few under the old ones
        for (float gx = T.X0 + 2f; gx < T.X1; gx += OczkoMlodych)
            for (float gz = T.Z0 + 2f; gz < T.Z1; gz += OczkoMlodych)
            {
                float x = gx + R(-.45f, .45f) * OczkoMlodych, z = gz + R(-.45f, .45f) * OczkoMlodych, los = R(0f, 1f);
                if (!T.WTorze(x, z) || Wolne(x, z)) continue;
                float las = Las(x, z), p = .07f * Smooth(.45f, .6f, las) + .30f * Smooth(.25f, .45f, las) * (1f - Smooth(.55f, .75f, las));
                if (PrzyTorze(x, z, out float e, out float od))
                {
                    if (e < 1.6f) continue;
                    if (las > .45f && e < od + 5f) p = Mathf.Max(p, .32f);                   // the trail's edge
                }
                if (los >= p) continue;
                drzewa.Add(new Drzewo { x = x, z = z, karta = R(0f, 1f) < .5f ? 6 : 7, skala = R(.9f, 2.2f), obrot = R(0f, 180f), odcien = R(-1f, 1f) });
            }
        // the ground under each (sunk by its slope: the trunk's foot never floats)
        for (int k = 0; k < drzewa.Count; k++)
        {
            var t = drzewa[k]; const float ep = 1f;
            float gx = (T.Grunt(t.x + ep, t.z) - T.Grunt(t.x - ep, t.z)) * .5f, gz = (T.Grunt(t.x, t.z + ep) - T.Grunt(t.x, t.z - ep)) * .5f;
            t.y = T.Grunt(t.x, t.z) - .1f - Mathf.Sqrt(gx * gx + gz * gz) * .35f * Mathf.Max(1f, t.skala);
            drzewa[k] = t;
        }
        return drzewa;
    }

    // ---------------------------------------------------------------- the build
    public static Material Material()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
        // without its atlas the forest is left out and said so - the track itself is built all the same
        if (!tex) { Debug.LogWarning("DO POPRAWKI MISTRZOWIE_TOR_KLADOW_LAS no atlas " + Atlas + " (unity-tools/mistrzowie/las): no forest"); return null; }
        var m = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "tor kładów las (karty drzew Poly Haven)", enableInstancing = true };
        m.mainTexture = tex; m.SetColor("_Color", Color.white);
        m.SetFloat("_VertexTint", 1f); m.SetFloat("_Metallic", 0f); m.SetFloat("_Roughness", .88f);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0f);
        m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450;
        Directory.CreateDirectory(Out + "/Materials");
        string path = Out + "/Materials/tor_kladow_las.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    sealed class Paczka { public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<Color> c = new List<Color>(); public readonly List<int> t = new List<int>(); public readonly List<Drzewo> pnie = new List<Drzewo>(); }

    /// <summary>The forest under the track's group: one mesh per 60 m square, the trunks' capsules with it.</summary>
    public static void Zbuduj(Transform g)
    {
        var mat = Material(); if (!mat) return;
        Directory.CreateDirectory(Folder);
        var lista = new List<Drzewo>(Drzewa()); lista.AddRange(Dodatkowe);
        var paczki = new SortedDictionary<long, Paczka>();
        Drzew = Mlodych = Pni = Kwadratow = Trojkaty = 0;
        foreach (var d in lista)
        {
            var ka = Karty[d.karta];
            int cx = Mathf.FloorToInt((d.x - T.X0) / Kwadrat), cz = Mathf.FloorToInt((d.z - T.Z0) / Kwadrat); long key = cx * 1000L + cz;
            if (!paczki.TryGetValue(key, out var p)) paczki[key] = p = new Paczka();
            float h = ka.ramka * d.skala, hw = h * .25f, y0 = d.y + ka.stopa * d.skala;
            // its tint: +-7 % brightness, a little warmer / cooler; darker at its foot and deep in the forest (its own shade)
            float las = Las(d.x, d.z), jas = 1f + .07f * d.odcien, ciepl = .03f * Hash(Mathf.RoundToInt(d.x * 7f), Mathf.RoundToInt(d.z * 7f)) - .015f;
            var gora = new Color(jas * (1f + ciepl), jas, jas * (1f - ciepl), 1f);
            float cien = ka.Mloda ? .9f : Mathf.Lerp(.86f, .68f, Smooth(.5f, 1f, las));
            var dol = new Color(gora.r * cien, gora.g * cien, gora.b * cien, 1f);
            for (int w = 0; w < 2; w++)
            {
                // the 0-degree picture faces along the tree's z, the 90-degree one along its x (as the scan was rendered)
                // (the 0-degree camera looked down -z, its right +x; the 90-degree one down -x, its right -z: both the tree's
                // local axes turned by its yaw)
                float a = (d.obrot + w * 90f) * Mathf.Deg2Rad; var os = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));   // the picture's right
                int c = ka.komorka + w, col = c % AtlasKol, row = c / AtlasKol;
                float u0 = (col * 512f + .5f) / AtlasW, u1 = ((col + 1) * 512f - .5f) / AtlasW;
                float v1 = 1f - (row * 1024f + .5f) / AtlasH, v0 = 1f - ((row + 1) * 1024f - .5f) / AtlasH;
                var stopa = new Vector3(d.x, y0, d.z); int b = p.v.Count;
                var nl = (Vector3.up - os * .45f).normalized; var nr = (Vector3.up + os * .45f).normalized;
                p.v.Add((stopa - os * hw) * K); p.n.Add(nl); p.uv.Add(new Vector2(u0, v0)); p.c.Add(dol);
                p.v.Add((stopa - os * hw + Vector3.up * h) * K); p.n.Add(nl); p.uv.Add(new Vector2(u0, v1)); p.c.Add(gora);
                p.v.Add((stopa + os * hw + Vector3.up * h) * K); p.n.Add(nr); p.uv.Add(new Vector2(u1, v1)); p.c.Add(gora);
                p.v.Add((stopa + os * hw) * K); p.n.Add(nr); p.uv.Add(new Vector2(u1, v0)); p.c.Add(dol);
                p.t.Add(b); p.t.Add(b + 1); p.t.Add(b + 2); p.t.Add(b); p.t.Add(b + 2); p.t.Add(b + 3);
            }
            if (ka.Mloda) Mlodych++; else { Drzew++; p.pnie.Add(d); }
        }
        var grupa = new GameObject("TOR DO KŁADÓW | las: sosny i jodły (Poly Haven, karty)").transform; grupa.SetParent(g, false);
        foreach (var kv in paczki)
        {
            var p = kv.Value; long cx = kv.Key / 1000L, cz = kv.Key % 1000L;
            string plik = "tor_kladow_las_" + cx + "_" + cz;
            var mesh = new Mesh { name = plik, indexFormat = p.v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(p.v); mesh.SetNormals(p.n); mesh.SetUVs(0, p.uv); mesh.colors = p.c.ToArray(); mesh.SetTriangles(p.t, 0);
            mesh.RecalculateBounds();
            string path = Folder + "/" + plik + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject("TOR DO KŁADÓW | las: drzewa " + cx + "," + cz); go.transform.SetParent(grupa, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            // the grown trees' trunks: a capsule each (0.22 m round at its size, up to its crown), on one object per square
            if (p.pnie.Count > 0)
            {
                var pk = new GameObject("TOR DO KŁADÓW | las: pnie (kolizje) " + cx + "," + cz); pk.transform.SetParent(grupa, false);
                foreach (var d in p.pnie)
                {
                    var ka = Karty[d.karta]; float hp = ka.wys * d.skala * .55f;
                    var cc = pk.AddComponent<CapsuleCollider>(); cc.direction = 1;
                    cc.radius = Mathf.Max(.14f, .22f * d.skala) * K; cc.height = hp * K;
                    cc.center = new Vector3(d.x, d.y + hp * .5f, d.z) * K;
                    Pni++;
                }
            }
            Kwadratow++; Trojkaty += p.t.Count / 3;
        }
        Debug.Log("MISTRZOWIE_TOR_KLADOW_LAS trees=" + Drzew + " young=" + Mlodych + " trunks=" + Pni + " squares=" + Kwadratow + " triangles=" + Trojkaty +
                  " atlas=" + Atlas + " (Poly Haven pine_tree_01, fir_tree_01, fir_sapling, pine_sapling_small; owner 24.11 14:20 \"więcej lasu\")");
    }
}

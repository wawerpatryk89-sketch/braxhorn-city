using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN R2 — rozbudowa osiedla (Claude, gałąź claude/osiedle-20261031).
// Dwa osiedla domków przedzielone parkiem:
//   osiedle A (istniejące)  z  -102 ..  99
//   Park Centralny          z  -150 .. -102  (aleje, fontanna, ławki, drzewa)
//   osiedle B (nowe)        z  -236 .. -150  (13 nowoczesnych willi, ulica z chodnikami)
// Współrzędne Unity (X już odwrócone względem źródła three.js). Materiały: Poly Haven / ambientCG (CC0),
// siatki łączone per materiał i obszar (mało wywołań rysowania na telefonie), kolizje BoxCollider.
public static class BraxClaudeOsiedle
{
    const string Tex = "Assets/Source/Claude/Osiedle/textures/";
    const string Mdl = "Assets/Source/Claude/Osiedle/models/";
    const string Out = "Assets/PlayableGenerated/ClaudeOsiedle";

    // Układ
    const float X0 = -114f, X1 = 114f;
    const float FenceA = -102f;                    // dawny płot południowy osiedla A
    const float ParkZ0 = -150f;                    // granica park / osiedle B
    const float SouthZ = -236f;
    const float AxisX = -20f;                      // oś: brama osiedla A -> aleja parku -> ulica B
    const float StreetZ = -193f;                   // oś ulicy głównej osiedla B
    const float Carriage = 3.5f, Kerb = .2f, Green = .7f, Walk = 2.3f;
    static float Half => Carriage + Kerb + Green + Walk; // 6.7 m od osi do granicy działek

    [Serializable] sealed class Report
    {
        public string status = "NIE URUCHOMIONO", error = "";
        public int tufts, ballTreesReplaced, shops, tennis, facades, villas, pools, parkTrees, plotTrees, shrubs, benches, lamps, bins, tables, oldFenceTrianglesRemoved,
                   oldFenceCollidersDisabled, meshes, triangles, materials, points;
        public List<string> notes = new List<string>();
    }
    static Report rep;
    static Transform root;

    // ---------------- materiały ----------------
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<Material, float> tile = new Dictionary<Material, float>();

    static Texture2D Map(string path, bool normal, bool srgb)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) throw new Exception("brak mapy " + path);
        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.sRGBTexture = srgb; imp.maxTextureSize = 1024; imp.mipmapEnabled = true;
        imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.filterMode = FilterMode.Trilinear;
        imp.textureCompression = TextureImporterCompression.Compressed;
        if (path.EndsWith("mg.png")) imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // id = katalog tekstury, metres = rozmiar jednego powtórzenia w metrach
    static Material Pbr(string key, string id, float metres, Color tint, float smooth = 1f, float metal = 0f)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var d = Tex + id + "/";
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, color = tint, enableInstancing = true };
        m.mainTexture = Map(d + "albedo.jpg", false, true);
        m.SetTexture("_BumpMap", Map(d + "normal.jpg", true, false)); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f);
        m.SetTexture("_MetallicGlossMap", Map(d + "mg.png", false, false)); m.EnableKeyword("_METALLICGLOSSMAP");
        m.SetFloat("_GlossMapScale", smooth); m.SetFloat("_Metallic", metal);
        m.SetTexture("_OcclusionMap", Map(d + "ao.jpg", false, false)); m.SetFloat("_OcclusionStrength", 1f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat");
        mats[key] = m; tile[m] = metres; return m;
    }
    static Material Plain(string key, Color c, float smooth, float metal = 0f)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, color = c, enableInstancing = true };
        m.SetFloat("_Glossiness", smooth); m.SetFloat("_Metallic", metal);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat");
        mats[key] = m; tile[m] = 1f; return m;
    }
    static string Safe(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

    static Material Asphalt => Pbr("jezdnia asfalt", "asphalt_04", 3.5f, new Color(.50f, .51f, .53f), .9f);
    static Material Pavers => Pbr("chodnik płyty", "concrete_pavement_02", 1.6f, new Color(.86f, .90f, .96f));
    static Material KerbStone => Pbr("krawężnik granit", "concrete_pavement_02", 1.2f, new Color(.92f, .92f, .93f));
    static Material Slabs => Pbr("płyty ścieżek", "concrete_pavement_02", 2.4f, new Color(1.06f, 1.05f, 1.02f));
    static Material Herring => Pbr("podjazd jodełka", "herringbone_brick_02", 2.4f, new Color(1f, 1f, 1f));
    static Material ParkPath => Pbr("aleja parku", "concrete_pavers_03", 3f, new Color(.92f, .9f, .88f));
    static Material Stucco => Pbr("tynk biały", "white_stucco_02", 2.5f, new Color(1.04f, 1.04f, 1.02f));
    static Material StuccoWarm => Pbr("tynk ciepły", "white_stucco_02", 2.5f, new Color(.98f, .93f, .86f));
    static Material Wood => Pbr("deska elewacyjna", "weathered_planks", 2.4f, new Color(.95f, .80f, .64f), .8f);
    static Material WoodDark => Pbr("deska płotu ciemna", "weathered_planks", 2.4f, new Color(.42f, .36f, .31f), .8f);
    static Material Stone => Pbr("kamień elewacyjny", "white_sandstone_blocks_02", 2.6f, new Color(1f, .98f, .95f));
    static Material Plinth => Pbr("cokół kamienny", "rough_block_wall", 2.2f, new Color(.82f, .80f, .78f));
    static Material Gravel => Pbr("żwir dachu", "bicolour_gravel", 2.5f, new Color(.9f, .9f, .9f));
    static Material Deck => Pbr("taras deska", "wood_floor_deck", 2.2f, new Color(.80f, .79f, .77f), .8f);
    static Material PoolTiles => Pbr("niecka basenu", "blue_floor_tiles_01", 1.6f, new Color(.9f, 1f, 1.05f));
    // Trawa: własny shader (plamy koloru, przebarwienia, pasy koszenia), awaryjnie Standard.
    static Material Grass
    {
        get
        {
            if (mats.TryGetValue("trawnik", out var have)) return have;
            var sh = Shader.Find("Braxhorn/OsiedleTrawa");
            if (!sh) { rep.notes.Add("brak shadera Braxhorn/OsiedleTrawa — Standard"); var p = Pbr("trawnik std", "grass004", 3.5f, new Color(.93f, 1f, .93f), .6f); mats["trawnik"] = p; return p; }
            var d = Tex + "grass004/";
            var m = new Material(sh) { name = "Osiedle: trawnik (fotoskan, plamy, pasy koszenia)", color = new Color(.90f, 1f, .88f) };
            m.mainTexture = Map(d + "albedo.jpg", false, true);
            m.SetTexture("_BumpMap", Map(d + "normal.jpg", true, false));
            m.SetTexture("_MetallicGlossMap", Map(d + "mg.png", false, false));
            m.SetTexture("_OcclusionMap", Map(d + "ao.jpg", false, false));
            AssetDatabase.CreateAsset(m, Out + "/trawnik.mat"); mats["trawnik"] = m; tile[m] = 3.5f; return m;
        }
    }
    static Material Glass => Plain("szkło przyciemniane", new Color(.10f, .13f, .15f), .93f, .55f);
    static Material Frame => Plain("aluminium antracyt", new Color(.07f, .075f, .08f), .45f, .6f);
    static Material Steel => Plain("stal czarna płotów", new Color(.06f, .065f, .07f), .35f, .5f);
    static Material GarageDoor => Plain("brama garażowa", new Color(.20f, .21f, .22f), .5f, .4f);
    static Material Paint => Plain("biała farba drogowa", new Color(.93f, .93f, .9f), .3f);
    static Material Water => Plain("woda", new Color(.18f, .42f, .52f), .96f, .1f);
    static Material Rubber => Plain("nawierzchnia placu zabaw", new Color(.42f, .17f, .13f), .15f);
    static Material PlayGreen => Plain("zieleń sprzętu zabaw", new Color(.20f, .42f, .26f), .45f, .1f);
    static Material GlassClear
    {
        get
        {
            if (mats.TryGetValue("szkło balustrad", out var have)) return have;
            var m = new Material(Shader.Find("Standard")) { name = "Osiedle: szkło balustrad", color = new Color(.62f, .74f, .78f, .28f) };
            m.SetFloat("_Mode", 3); m.SetFloat("_Glossiness", .95f); m.SetFloat("_Metallic", .1f);
            m.SetInt("_SrcBlend", (int)BlendMode.One); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(m, Out + "/szklo_balustrad.mat"); mats["szkło balustrad"] = m; tile[m] = 1f; return m;
        }
    }

    // ---------------- łączenie geometrii ----------------
    sealed class Batch
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector4> t = new List<Vector4>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> i = new List<int>();
        public bool shadows = true;
    }
    static readonly Dictionary<(string, Material), Batch> batches = new Dictionary<(string, Material), Batch>();
    static string group = "osiedle";

    static Batch B(Material m)
    {
        if (!batches.TryGetValue((group, m), out var b)) batches[(group, m)] = b = new Batch();
        return b;
    }
    static void Planar(Vector3 n, out Vector3 u, out Vector3 w)
    {
        if (Mathf.Abs(n.y) > .5f) { u = Vector3.right; w = Vector3.forward; }
        else if (Mathf.Abs(n.x) > Mathf.Abs(n.z)) { u = Vector3.forward; w = Vector3.up; }
        else { u = Vector3.right; w = Vector3.up; }
    }
    // Czworokąt p0..p3 (dowolna kolejność obwodu), normalna n, UV planarne w metrach / tile.
    static void Quad(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        Planar(n, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(n, u), w) >= 0 ? 1f : -1f;
        int k = b.v.Count;
        foreach (var p in new[] { p0, p1, p2, p3 })
        {
            b.v.Add(p); b.n.Add(n); b.t.Add(new Vector4(u.x, u.y, u.z, sign));
            b.uv.Add(new Vector2(Vector3.Dot(p, u) / s, Vector3.Dot(p, w) / s));
        }
        bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n) < 0;
        if (!flip) b.i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        else b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
    }
    static void Top(Material m, float x0, float x1, float z0, float z1, float y)
        => Quad(m, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up);

    // Prostopadłościan wyrównany do osi; bottom=false pomija spód (niewidoczny), solid = BoxCollider.
    static void Box(Material m, Vector3 c, Vector3 s, bool solid = true, bool bottom = false)
    {
        var h = s * .5f;
        Vector3 P(float x, float y, float z) => c + new Vector3(x * h.x, y * h.y, z * h.z);
        Quad(m, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Vector3.up);
        if (bottom) Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), Vector3.down);
        Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), Vector3.back);
        Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), Vector3.forward);
        Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), Vector3.left);
        Quad(m, P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), Vector3.right);
        if (solid) Collider(c, s);
    }
    static void BoxMinMax(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool solid = true, bool bottom = false)
        => Box(m, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), y1 - y0, Mathf.Abs(z1 - z0)), solid, bottom);

    static Transform colliders;
    static void Collider(Vector3 c, Vector3 s)
    {
        var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false); g.transform.position = c;
        g.AddComponent<BoxCollider>().size = s;
    }

    static void Ring(Material m, Vector3 c, float r0, float r1, int seg = 32)
    {
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Quad(m, c + d0 * r0, c + d0 * r1, c + d1 * r1, c + d1 * r0, Vector3.up);
        }
    }
    static void Cylinder(Material m, Vector3 baseCenter, float r, float h, int seg = 24, bool top = true, bool solid = true, bool inward = false)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            int i0 = b.v.Count;
            var verts = new[] { baseCenter + d0 * r, baseCenter + d1 * r, baseCenter + d1 * r + Vector3.up * h, baseCenter + d0 * r + Vector3.up * h };
            var norms = inward ? new[] { -d0, -d1, -d1, -d0 } : new[] { d0, d1, d1, d0 };
            var us = new[] { a0 * r / s, a1 * r / s, a1 * r / s, a0 * r / s };
            for (int q = 0; q < 4; q++)
            {
                b.v.Add(verts[q]); b.n.Add(norms[q]);
                var tg = Vector3.Cross(Vector3.up, norms[q]).normalized;
                b.t.Add(new Vector4(tg.x, tg.y, tg.z, -1));
                b.uv.Add(new Vector2(us[q], (verts[q].y - baseCenter.y) / s));
            }
            if (!inward) b.i.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
            else b.i.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }
        if (top) Disc(m, baseCenter + Vector3.up * h, r, seg);
        if (solid) Collider(baseCenter + Vector3.up * (h * .5f), new Vector3(r * 1.6f, h, r * 1.6f));
    }
    static void Disc(Material m, Vector3 c, float r, int seg = 32)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        int ic = b.v.Count;
        void V(Vector3 p) { b.v.Add(p); b.n.Add(Vector3.up); b.t.Add(new Vector4(1, 0, 0, 1)); b.uv.Add(new Vector2(p.x / s, p.z / s)); }
        V(c);
        for (int k = 0; k <= seg; k++) { float a = k * Mathf.PI * 2 / seg; V(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r); }
        for (int k = 0; k < seg; k++) b.i.AddRange(new[] { ic, ic + k + 2, ic + k + 1 });
    }

    static void Flush()
    {
        int n = 0;
        foreach (var kv in batches)
        {
            var b = kv.Value; if (b.v.Count == 0) continue;
            var parent = Group(kv.Key.Item1);
            var mesh = new Mesh { name = "Osiedle " + kv.Key.Item1 + " / " + kv.Key.Item2.name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(b.v); mesh.SetNormals(b.n); mesh.SetTangents(b.t); mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.i, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Out + "/mesh_" + (n++) + ".asset");
            var g = new GameObject(kv.Key.Item2.name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item2;
            r.shadowCastingMode = kv.Key.Item2 == Grass || kv.Key.Item2 == Asphalt || kv.Key.Item2 == ParkPath ? ShadowCastingMode.Off : ShadowCastingMode.On;
            r.receiveShadows = true;
            rep.meshes++; rep.triangles += b.i.Count / 3;
        }
        batches.Clear();
    }
    static readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
    static Transform Group(string name)
    {
        if (groups.TryGetValue(name, out var t)) return t;
        t = new GameObject(name).transform; t.SetParent(root, false); groups[name] = t; return t;
    }

    // ---------------- modele ----------------
    static GameObject Model(string id)
    {
        var p = Mdl + id + "/" + id + ".gltf";
        if (!File.Exists(p)) p = Mdl + id + "/" + id + ".glb";
        if (!File.Exists(p)) { rep.notes.Add("brak modelu " + id); return null; }
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
        if (!go) rep.notes.Add("glTFast nie zaimportował " + id);
        return go;
    }
    static Bounds RB(GameObject o)
    {
        var rs = o.GetComponentsInChildren<Renderer>(true);
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    // Model w skali natywnej (Poly Haven = metry), spód na y; obrót yaw * własny obrót modelu.
    static GameObject Put(GameObject src, Vector3 at, float yaw, float cull = .02f, bool solid = true)
    {
        if (!src) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false);
        o.name = src.name; o.transform.position = Vector3.zero;
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
        if (o.GetComponentsInChildren<Renderer>(true).Length == 0) { UnityEngine.Object.DestroyImmediate(o); return null; }
        var b = RB(o);
        o.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        var rs = o.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rs) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        var lod = o.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs) }); lod.RecalculateBounds();
        if (solid) { b = RB(o); Collider(b.center, b.size); }
        return o;
    }

    // Prawdziwe drzewa i krzewy z paczki Verdant, które już są w scenie osiedla (te same siatki,
    // co w remoncie R2): kopie bez nowej geometrii w pamięci.
    static Dictionary<string, MeshRenderer> verdant;
    // Drzewo do gry: pień + liście Verdant w 3 gęstościach (49k / 28k / 13k trójkątów zamiast 175k),
    // ten sam wygląd korony; LOD zmienia gęstość liści z odległością.
    static GameObject treeSrc; static bool treeTried;
    static GameObject Tree(Vector3 at, float height, float yaw, bool trunkCollider = true)
    {
        if (!treeTried) { treeTried = true; treeSrc = Model("drzewo_verdant"); if (!treeSrc) rep.notes.Add("brak drzewo_verdant.glb"); }
        if (!treeSrc) return null;
        var t = (GameObject)UnityEngine.Object.Instantiate(treeSrc, Group(group), false); t.name = "Drzewo (Verdant, LOD)";
        t.transform.position = Vector3.zero; t.transform.localScale = Vector3.one;
        Renderer R(string nm) => t.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == nm);
        Renderer pien = R("pien"), l0 = R("liscie_0"), l1 = R("liscie_1"), l2 = R("liscie_2");
        if (!pien || !l0) { UnityEngine.Object.DestroyImmediate(t); rep.notes.Add("drzewo_verdant: brak węzłów pien/liscie"); treeSrc = null; return null; }
        var all = new[] { pien, l0, l1, l2 }.Where(r => r).ToArray();
        var b = pien.bounds; foreach (var r in all) b.Encapsulate(r.bounds);
        var pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (Transform c in t.transform) c.position -= pivot;
        t.transform.localScale = Vector3.one * (height / Mathf.Max(.5f, b.size.y));
        t.transform.rotation = Quaternion.Euler(0, yaw, 0) * t.transform.rotation;
        t.transform.position = at;
        pien.shadowCastingMode = ShadowCastingMode.On; l0.shadowCastingMode = ShadowCastingMode.On;
        if (l1) l1.shadowCastingMode = ShadowCastingMode.Off; if (l2) l2.shadowCastingMode = ShadowCastingMode.Off;
        var lod = t.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(.25f, new[] { pien, l0 }), new LOD(.10f, new[] { pien, l1 ? l1 : l0 }), new LOD(.03f, new[] { pien, l2 ? l2 : (l1 ? l1 : l0) }) });
        lod.RecalculateBounds();
        if (trunkCollider)
        {
            var trunk = new GameObject("pień kolizja"); trunk.transform.SetParent(colliders, false); trunk.transform.position = at + Vector3.up * 1.4f;
            var cap = trunk.AddComponent<CapsuleCollider>(); cap.radius = .3f; cap.height = 2.8f;
        }
        return t;
    }
    static GameObject Shrub(Vector3 at, float scale, float yaw)
    {
        if (!verdant.TryGetValue("EstateMesh_467", out var src)) return null;
        var p = new GameObject("Krzew (Verdant)"); p.transform.SetParent(Group(group), false);
        var o = new GameObject("krzew"); o.transform.SetParent(p.transform, false);
        o.transform.localPosition = src.transform.position; o.transform.localRotation = src.transform.rotation; o.transform.localScale = src.transform.lossyScale;
        o.AddComponent<MeshFilter>().sharedMesh = src.GetComponent<MeshFilter>().sharedMesh;
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterials = src.sharedMaterials; r.shadowCastingMode = ShadowCastingMode.Off;
        var b = r.bounds; var pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        o.transform.localPosition -= pivot;
        p.transform.localScale = Vector3.one * scale; p.transform.rotation = Quaternion.Euler(0, yaw, 0); p.transform.position = at;
        var lod = p.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.02f, new Renderer[] { r }) }); lod.RecalculateBounds();
        rep.shrubs++;
        return p;
    }

    // Nowoczesna latarnia (słup + płaska oprawa), bez świecenia — tylko dzień.
    static void Lamp(Vector3 at, float yaw)
    {
        Box(Frame, at + Vector3.up * 2.4f, new Vector3(.12f, 4.8f, .12f));
        var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        var head = at + Vector3.up * 4.75f + dir * .35f;
        Box(Frame, head, new Vector3(Mathf.Abs(dir.x) > .5f ? .9f : .28f, .1f, Mathf.Abs(dir.z) > .5f ? .9f : .28f), false);
        Box(Paint, head - Vector3.up * .055f, new Vector3(Mathf.Abs(dir.x) > .5f ? .7f : .2f, .01f, Mathf.Abs(dir.z) > .5f ? .7f : .2f), false);
        rep.lamps++;
    }

    // Płot premium: kamienny murek + słupki + poziome stalowe lamele. Przerwy: lista (od, do) wzdłuż biegu.
    static void FenceX(float x0, float x1, float z, float h, params (float, float)[] gaps) => FenceRun(true, x0, x1, z, h, gaps);
    static void FenceZ(float z0, float z1, float x, float h, params (float, float)[] gaps) => FenceRun(false, z0, z1, x, h, gaps);
    static void FenceRun(bool alongX, float a0, float a1, float fixedCoord, float h, (float, float)[] gaps)
    {
        if (a1 < a0) { var tmp = a0; a0 = a1; a1 = tmp; }
        var segs = new List<(float, float)>(); float cur = a0;
        foreach (var g in gaps.OrderBy(q => q.Item1)) { if (g.Item1 > cur) segs.Add((cur, g.Item1)); cur = Mathf.Max(cur, g.Item2); }
        if (cur < a1) segs.Add((cur, a1));
        Vector3 P(float a, float y) => alongX ? new Vector3(a, y, fixedCoord) : new Vector3(fixedCoord, y, a);
        Vector3 S(float len, float hy, float th) => alongX ? new Vector3(len, hy, th) : new Vector3(th, hy, len);
        foreach (var (s0, s1) in segs)
        {
            float len = s1 - s0; if (len < .4f) continue;
            float mid = (s0 + s1) * .5f;
            Box(Plinth, P(mid, .3f), S(len, .6f, .36f), true);
            Box(Plinth, P(mid, .63f), S(len + .04f, .06f, .42f), false);
            int bays = Mathf.Max(1, Mathf.RoundToInt(len / 3.2f)); float step = len / bays;
            for (int k = 0; k <= bays; k++) Box(Plinth, P(s0 + k * step, h * .5f), S(.46f, h, .46f), false);
            for (int sl = 0; sl < 6; sl++)
            {
                float y = .78f + sl * (h - .95f) / 5f;
                Box(Steel, P(mid, y), S(len, .07f, .05f), false);
            }
            Collider(P(mid, h * .5f), S(len, h, .4f));
        }
    }
    // Płot boczny działki: ciemne poziome deski na słupkach.
    static void WoodFenceZ(float z0, float z1, float x, float h = 1.7f)
    {
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        float len = z1 - z0, mid = (z0 + z1) * .5f;
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
        for (int k = 0; k <= posts; k++) Box(Frame, new Vector3(x, h * .5f, z0 + k * len / posts), new Vector3(.09f, h, .09f), false);
        for (int sl = 0; sl < 9; sl++)
        {
            float y = .12f + sl * (h - .2f) / 8.5f;
            Box(WoodDark, new Vector3(x, y + .07f, mid), new Vector3(.04f, .14f, len), false);
        }
        Collider(new Vector3(x, h * .5f, mid), new Vector3(.12f, h, len));
    }

    // ---------------- ulice ----------------
    // Przekrój: jezdnia | krawężnik | pas zieleni | chodnik.
    static void StreetX(float x0, float x1, float zc, params (float, float)[] sideGapsNorth)
    {
        Top(Asphalt, x0, x1, zc - Carriage, zc + Carriage, .03f);
        // środkowa linia przerywana
        for (float x = x0 + 1.5f; x + 3f < x1; x += 6f) Top(Paint, x, x + 3f, zc - .06f, zc + .06f, .046f);
        foreach (int side in new[] { -1, 1 })
        {
            float kz = zc + side * (Carriage + Kerb * .5f), wz = zc + side * (Carriage + Kerb + Green + Walk * .5f);
            var runs = new List<(float, float)> { (x0, x1) };
            if (side > 0 && sideGapsNorth.Length > 0)
            {
                runs.Clear(); float cur = x0;
                foreach (var g in sideGapsNorth.OrderBy(q => q.Item1)) { if (g.Item1 > cur) runs.Add((cur, g.Item1)); cur = g.Item2; }
                if (cur < x1) runs.Add((cur, x1));
            }
            foreach (var (a, b) in runs)
            {
                BoxMinMax(KerbStone, a, b, 0, .16f, kz - Kerb * .5f, kz + Kerb * .5f);
                BoxMinMax(Pavers, a, b, 0, .14f, wz - Walk * .5f, wz + Walk * .5f);
                for (float x = a + 10f; x < b - 4f; x += 22f) Lamp(new Vector3(x, 0, zc + side * (Carriage + Kerb + Green * .5f)), side > 0 ? 180 : 0);
            }
        }
    }
    static void StreetNS(float z0, float z1, float xc)
    {
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        Top(Asphalt, xc - Carriage, xc + Carriage, z0, z1, .03f);
        for (float z = z0 + 1.5f; z + 3f < z1; z += 6f) Top(Paint, xc - .06f, xc + .06f, z, z + 3f, .046f);
        foreach (int side in new[] { -1, 1 })
        {
            float kx = xc + side * (Carriage + Kerb * .5f), wx = xc + side * (Carriage + Kerb + Green + Walk * .5f);
            BoxMinMax(KerbStone, kx - Kerb * .5f, kx + Kerb * .5f, 0, .16f, z0, z1);
            BoxMinMax(Pavers, wx - Walk * .5f, wx + Walk * .5f, 0, .14f, z0, z1);
            for (float z = z0 + 8f; z < z1 - 4f; z += 20f) Lamp(new Vector3(xc + side * (Carriage + Kerb + Green * .5f), 0, z), side > 0 ? 270 : 90);
        }
    }
    static void Zebra(float x0, float x1, float z0, float z1, bool stripesAlongZ)
    {
        if (stripesAlongZ) for (float x = x0 + .25f; x < x1 - .3f; x += 1f) Top(Paint, x, x + .5f, z0, z1, .047f);
        else for (float z = z0 + .25f; z < z1 - .3f; z += 1f) Top(Paint, x0, x1, z, z + .5f, .047f);
    }

    // ---------------- willa ----------------
    static GameObject outdoorSet, bench, bin, picnic, planter; static int binTurn;
    static GameObject[] tufts = new GameObject[0], flowers = new GameObject[0]; static System.Random tuftRnd = new System.Random(77);
    // Kępa trawy (lub kwiat) w 3D: losowy gatunek, obrót i skala, bez kolizji, znika z daleka.
    static void Tuft(Vector3 at, bool flower = false, float scale = 1f)
    {
        var set = flower ? flowers : tufts; if (set.Length == 0) return;
        var o = Put(set[tuftRnd.Next(set.Length)], at, tuftRnd.Next(360), flower ? .015f : .012f, false);
        if (!o) return;
        float s = scale * (.8f + (float)tuftRnd.NextDouble() * .5f); o.transform.localScale *= s;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        rep.tufts++;
    }
    static void TuftLine(Vector3 a, Vector3 b, float step, float side)
    {
        var d = b - a; float len = d.magnitude; if (len < .5f) return; d /= len; var n = new Vector3(-d.z, 0, d.x) * side;
        for (float t = step * .5f; t < len; t += step) Tuft(a + d * t + n * (.35f + (float)tuftRnd.NextDouble() * .3f), false, .9f);
    }
    // swap = działka „bokiem” (ulica wzdłuż Z, głębokość wzdłuż X) — tak stoją domy osiedla A.
    static void Villa(int index, float u0, float u1, float front, float dir, bool swap = false, float depthOverride = 0f,
                      bool garageOn = true, bool fencesOn = true, string label = null)
    {
        group = label ?? "Osiedle B / willa " + (index + 1);
        float W = u1 - u0, depth = depthOverride > 0 ? depthOverride : dir > 0 ? ParkZ0 - front : front - SouthZ;
        bool mirror = index % 2 == 1; int variant = index % 3; float yawFix = swap ? 90f : 0f;
        float Z(float w) => front + dir * w;                         // głębokość działki -> świat
        float U(float a) => mirror ? u1 - a : u0 + a;                 // pozycja wzdłuż ulicy -> świat
        Vector3 Pw(float a, float y, float w) => swap ? new Vector3(Z(w), y, U(a)) : new Vector3(U(a), y, Z(w));
        void BoxL(Material m, float a0, float a1, float y0, float y1, float w0, float w1, bool solid = true, bool bottom = false)
        {
            Vector3 p = Pw(a0, y0, w0), q = Pw(a1, y1, w1);
            BoxMinMax(m, Mathf.Min(p.x, q.x), Mathf.Max(p.x, q.x), y0, y1, Mathf.Min(p.z, q.z), Mathf.Max(p.z, q.z), solid, bottom);
        }

        float Wm = garageOn ? Mathf.Min(W - 13f, 18f) : Mathf.Min(W - 6f, 16f), garage = garageOn ? 6.5f : 0f, s0 = (W - (Wm + garage)) * .5f;
        float h0 = s0, h1 = s0 + Wm, g0 = h1, g1 = h1 + garage;     // dom i garaż wzdłuż ulicy
        float f0 = 7f, f1 = 18f;                                      // front i tył parteru (głębokość)
        var wallG = variant == 1 ? Stone : Stucco;
        var wallU = variant == 0 ? Wood : variant == 1 ? Stucco : StuccoWarm;

        // parter + cokół
        BoxL(wallG, h0, h1, 0, 3.4f, f0, f1);
        BoxL(Plinth, h0 - .04f, h1 + .04f, 0, .35f, f0 - .04f, f1 + .04f, false);
        // stropodach parteru: attyka + żwir
        BoxL(wallG, h0, h1, 3.4f, 3.75f, f0, f0 + .25f, false); BoxL(wallG, h0, h1, 3.4f, 3.75f, f1 - .25f, f1, false);
        BoxL(wallG, h0, h0 + .25f, 3.4f, 3.75f, f0, f1, false); BoxL(wallG, h1 - .25f, h1, 3.4f, 3.75f, f0, f1, false);
        BoxL(Gravel, h0 + .25f, h1 - .25f, 3.4f, 3.46f, f0 + .25f, f1 - .25f, false);
        // piętro przesunięte i wysunięte nad wejście
        float uw = Wm * .62f, uA = h0, uB = uA + uw, e0 = 5.4f, e1 = 14.6f;   // nad wejściem (U() odbija stronę)
        BoxL(wallU, uA, uB, 3.46f, 6.7f, e0, e1, true, true);
        BoxL(Stucco, uA - .05f, uB + .05f, 6.7f, 7.05f, e0 - .05f, e1 + .05f, false);
        BoxL(Gravel, uA + .2f, uB - .2f, 7.05f, 7.08f, e0 + .2f, e1 - .2f, false);
        // taras na dachu parteru: szklana balustrada ze stalowym pochwytem tam, gdzie nie ma piętra
        void Rail(float a0, float a1, float w0, float w1)
        {
            BoxL(GlassClear, a0, a1, 3.75f, 4.75f, w0, w1, false);
            float pa = Mathf.Min(w0, w1), pb = Mathf.Max(w0, w1);
            BoxL(Frame, a0, a1, 4.75f, 4.81f, pa - .02f, pb + .02f, false);
        }
        Rail(uB + .05f, h1 - .15f, f0 + .12f, f0 + .15f);
        Rail(h1 - .15f, h1 - .12f, f0 + .12f, f1 - .12f);
        Rail(h0 + .12f, h1 - .15f, f1 - .15f, f1 - .12f);
        Rail(h0 + .12f, h0 + .15f, e1 + .05f, f1 - .12f);
        // garaż
        if (garageOn)
        {
            BoxL(Stucco, g0, g1, 0, 3.0f, f0, f0 + 6.5f);
            BoxL(Stucco, g0, g1, 3.0f, 3.25f, f0, f0 + 6.5f, false);
            BoxL(GarageDoor, g0 + .7f, g1 - .7f, .02f, 2.45f, f0 - .06f, f0 + .02f, false);
            for (int k = 1; k < 6; k++) BoxL(Frame, g0 + .7f, g1 - .7f, .02f + k * .4f, .04f + k * .4f, f0 - .075f, f0 - .055f, false);
        }

        // okna: szkło wysunięte 3 cm, rama antracyt
        void Pane(float a0, float a1, float y0, float y1, float w, float outward)
        {
            float wa = w - outward * .03f, wb = w + outward * .015f;
            BoxL(Glass, a0, a1, y0, y1, Mathf.Min(wa, wb), Mathf.Max(wa, wb), false);
            float fw = .07f, fa = w - outward * .05f, fb = w + outward * .01f;
            BoxL(Frame, a0 - fw, a1 + fw, y0 - fw, y0, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a0 - fw, a1 + fw, y1, y1 + fw, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a0 - fw, a0, y0, y1, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a1, a1 + fw, y0, y1, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
        }
        // front parteru: drzwi przy krawędzi, dalej trzy duże przeszklenia
        float door0 = garageOn ? h0 + 1.4f : h0 + Wm * .5f - .62f, door1 = door0 + 1.25f;
        BoxL(Wood, door0, door1, .02f, 2.45f, f0 - .06f, f0 + .02f, false);
        BoxL(Frame, door1 - .25f, door1 - .2f, 1.0f, 1.5f, f0 - .1f, f0 - .06f, false);
        BoxL(Stucco, door0 - .9f, door1 + 1.2f, 2.95f, 3.15f, f0 - 1.8f, f0, false, true);   // daszek nad wejściem
        float gl0 = door1 + 1.0f, gl1 = h1 - 1.0f; int panes = Mathf.Max(1, Mathf.RoundToInt((gl1 - gl0) / 2.6f));
        for (int k = 0; k < panes; k++) { float a = gl0 + k * (gl1 - gl0) / panes + .12f, b = gl0 + (k + 1) * (gl1 - gl0) / panes - .12f; Pane(a, b, .4f, 2.9f, f0, -1); }
        if (!garageOn && door0 - .8f - (h0 + 1f) > 1.2f) Pane(h0 + 1f, door0 - .8f, .4f, 2.9f, f0, -1);
        // tył parteru: przesuwne drzwi tarasowe
        int rear = Mathf.Max(2, Mathf.RoundToInt((Wm - 2f) / 2.8f));
        for (int k = 0; k < rear; k++) { float a = h0 + 1f + k * (Wm - 2f) / rear + .1f, b = h0 + 1f + (k + 1) * (Wm - 2f) / rear - .1f; Pane(a, b, .3f, 3.0f, f1, 1); }
        // piętro: wstęga okien z przodu, dwa okna z tyłu, boczne
        Pane(uA + .8f, uB - .8f, 4.1f, 6.15f, e0, -1);
        int mull = Mathf.Max(1, Mathf.RoundToInt((uw - 1.6f) / 1.7f));
        for (int k = 1; k < mull; k++) { float a = uA + .8f + k * (uw - 1.6f) / mull; BoxL(Frame, a - .035f, a + .035f, 4.1f, 6.15f, e0 - .05f, e0 + .01f, false); }
        if (planter) { Put(planter, Pw(h1 - 1.4f, 3.46f, f0 + 1.0f), (mirror ? 270 : 90) + yawFix, .03f, false); Put(planter, Pw(door1 + .7f, 0, f0 - .6f), yawFix, .03f, false); }
        foreach (float a in new[] { door0 - .35f, door1 + .35f }) BoxL(Frame, a - .07f, a + .07f, 2.05f, 2.35f, f0 - .12f, f0, false);
        Pane(uA + .9f, uA + uw * .45f, 4.1f, 6.15f, e1, 1); Pane(uA + uw * .55f, uB - .9f, 4.1f, 6.15f, e1, 1);
        // tarasy i basen
        BoxL(Deck, h0, h1, 0, .18f, f1, f1 + 4.2f);
        if (outdoorSet) Put(outdoorSet, Pw(h0 + Wm * .35f, .18f, f1 + 2.2f), (mirror ? 180 : 0) + yawFix, .03f, true);
        bool pool = W >= 33f;
        if (pool)
        {
            float p0 = h0 + Wm * .5f - 4.8f, p1 = h0 + Wm * .5f + 4.8f, q0 = f1 + 5.2f, q1 = f1 + 9.0f;
            BoxL(Slabs, p0 - 1f, p1 + 1f, 0, .2f, q0 - 1f, q0, false); BoxL(Slabs, p0 - 1f, p1 + 1f, 0, .2f, q1, q1 + 1f, false);
            BoxL(Slabs, p0 - 1f, p0, 0, .2f, q0, q1, false); BoxL(Slabs, p1, p1 + 1f, 0, .2f, q0, q1, false);
            BoxL(PoolTiles, p0, p1, -.02f, .04f, q0, q1, false);
            var wm = Water; Vector3 pp = Pw(p0, 0, q0), qq = Pw(p1, 0, q1);
            float wa = Mathf.Min(pp.x, qq.x), wb = Mathf.Max(pp.x, qq.x), za = Mathf.Min(pp.z, qq.z), zb = Mathf.Max(pp.z, qq.z);
            Top(wm, wa, wb, za, zb, .12f);
            Collider(new Vector3((wa + wb) * .5f, .4f, (za + zb) * .5f), new Vector3(wb - wa + 2f, .8f, zb - za + 2f));
            rep.pools++;
        }
        // podjazd (jodełka) i ścieżka (płyty) od chodnika
        if (garageOn) BoxL(Herring, g0 + .5f, g1 - .5f, 0, .04f, 0, f0, false);
        BoxL(Slabs, door0 - .1f, door1 + .1f, 0, .04f, 0, f0 - .05f, false);
        if (fencesOn)
        {
            // płot frontowy z przerwami na podjazd i furtkę
            float fz = Z(.3f);
            (float, float) Gap(float a, float b) => (Mathf.Min(U(a), U(b)), Mathf.Max(U(a), U(b)));
            var gaps = garageOn ? new[] { Gap(g0 + .5f, g1 - .5f), Gap(door0 - .2f, door1 + .2f) } : new[] { Gap(door0 - .2f, door1 + .2f) };
            if (!swap) FenceX(u0, u1, fz, 1.55f, gaps); else FenceZ(u0, u1, fz, 1.55f, gaps);
            var mb = Pw(door1 + .6f, .65f, .75f); Box(Frame, mb, swap ? new Vector3(.32f, 1.3f, .38f) : new Vector3(.38f, 1.3f, .32f), true);    // skrzynka na listy
            // boczne płoty: lewa granica każdej działki (prawa zamyka sąsiad / ostatnia działka)
            if (!swap) WoodFenceZ(Z(.6f), Z(depth - .3f), u0 + .1f); else WoodFenceX(Z(.6f), Z(depth - .3f), u0 + .1f);
            // tylny płot (od parku lub od granicy osiedla)
            if (!swap) FenceX(u0, u1, Z(depth - .3f), 1.55f); else FenceZ(u0, u1, Z(depth - .3f), 1.55f);
        }
        // zieleń działki
        if (Tree(Pw(W - 3f, 0, 3.4f), 6.5f + (index % 3) * .8f, index * 47f)) rep.plotTrees++;
        TuftLine(Pw(.2f, 0, 1.2f), Pw(.2f, 0, depth - 1.2f), 3.2f, mirror ? -1f : 1f);
        for (int k = 0; k < 4; k++) Tuft(Pw(W - 3f + Mathf.Cos(k * 1.7f) * 1.1f, 0, 3.4f + Mathf.Sin(k * 1.7f) * 1.1f));
        for (int k = 0; k < 5; k++) Tuft(Pw(1.2f + k * .45f, 0, 1.0f + (k % 2) * .4f), true, 1.1f);
        Shrub(Pw(gl0 + .6f, 0, f0 - .9f), .38f, index * 31f);
        Shrub(Pw((gl0 + gl1) * .5f, 0, f0 - .9f), .34f, index * 17f);
        Shrub(Pw(gl1 - .4f, 0, f0 - .9f), .4f, index * 53f);
        rep.villas++;
    }

    // ---------------- szyldy, sklepy, kort, stróżówka ----------------
    static Material SignMat(string id)
    {
        string key = "szyld " + id; if (mats.TryGetValue(key, out var have)) return have;
        string path = "Assets/Source/Claude/Osiedle/signs/" + id + ".png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (!imp) { rep.notes.Add("brak szyldu " + id); return Frame; }
        imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.SaveAndReimport();
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        m.SetFloat("_Glossiness", .35f); AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = 1f; return m;
    }
    // Szyld: płyta grafitowa + front z napisem (UV 0..1 na całą tablicę), normalna nx/nz (kierunek do oglądającego).
    static void Sign(string id, Vector3 c, float w, float h, Vector3 n)
    {
        bool alongX = Mathf.Abs(n.z) > .5f;
        Box(Frame, c - n * .06f, alongX ? new Vector3(w + .1f, h + .1f, .1f) : new Vector3(.1f, h + .1f, w + .1f), false);
        var m = SignMat(id); var b = B(m); int k = b.v.Count;
        Vector3 right = alongX ? (n.z < 0 ? Vector3.right : Vector3.left) : (n.x > 0 ? Vector3.back : Vector3.forward);
        var f0 = c + n * .005f;
        var p = new[] { f0 - right * w * .5f - Vector3.up * h * .5f, f0 + right * w * .5f - Vector3.up * h * .5f, f0 + right * w * .5f + Vector3.up * h * .5f, f0 - right * w * .5f + Vector3.up * h * .5f };
        var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        for (int q = 0; q < 4; q++) { b.v.Add(p[q]); b.n.Add(n); b.t.Add(new Vector4(right.x, right.y, right.z, 1)); b.uv.Add(uv[q]); }
        if (Vector3.Dot(Vector3.Cross(p[1] - p[0], p[2] - p[0]), n) >= 0) b.i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        else b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
    }

    // Pierzeja: parter ze sklepami (witryny, wejścia, markizy, szyldy), piętro z apartamentami i balkonami,
    // placyk przed budynkiem ze stolikami kawiarni. front = granica działek od ulicy (z), budynek w głąb (+z).
    static void ShopRow(float x0, float x1, float front)
    {
        group = "Osiedle B / pierzeja sklepów";
        float zf = front + 6f, zb = zf + 14f, H0 = 4.2f, H1 = 7.6f;
        Top(Slabs, x0, x1, front, zf, .045f);                                      // placyk
        BoxMinMax(Stucco, x0, x1, 0, H0, zf, zb);                                   // parter (bryła)
        BoxMinMax(Plinth, x0 - .04f, x1 + .04f, 0, .3f, zf - .04f, zb + .04f, false);
        BoxMinMax(StuccoWarm, x0 + .3f, x1 - .3f, H0, H1, zf + .8f, zb - .3f);      // piętro cofnięte (balkony z przodu)
        BoxMinMax(Stucco, x0 + .2f, x1 - .2f, H1, H1 + .4f, zf + .7f, zb - .2f, false);
        BoxMinMax(Gravel, x0 + .45f, x1 - .45f, H1 + .4f, H1 + .43f, zf + .95f, zb - .45f, false);
        BoxMinMax(Gravel, x0 + .2f, x1 - .2f, H0, H0 + .05f, zf + .05f, zf + .8f, false);     // posadzka balkonów
        BoxMinMax(GlassClear, x0 + .2f, x1 - .2f, H0 + .05f, H0 + 1.05f, zf + .05f, zf + .09f, false);
        BoxMinMax(Frame, x0 + .2f, x1 - .2f, H0 + 1.05f, H0 + 1.1f, zf + .03f, zf + .11f, false);
        string[] shops = { "butik", "shake", "kawiarnia", "market" };
        float unit = (x1 - x0) / shops.Length;
        for (int k = 0; k < shops.Length; k++)
        {
            float a = x0 + k * unit, b = a + unit;
            // witryna na całą szerokość lokalu (szkło wysunięte, ramy co 2.4 m), drzwi szklane
            BoxMinMax(Glass, a + .6f, b - .6f, .35f, 3.5f, zf - .03f, zf + .02f, false);
            for (float x = a + .6f; x <= b - .55f; x += Mathf.Max(1.8f, (b - a - 1.2f) / Mathf.Max(1, Mathf.Round((b - a - 1.2f) / 2.4f))))
                BoxMinMax(Frame, x - .04f, x + .04f, .35f, 3.5f, zf - .06f, zf + .01f, false);
            BoxMinMax(Frame, a + .6f, b - .6f, 3.5f, 3.58f, zf - .06f, zf + .01f, false);
            BoxMinMax(Frame, a + .6f, b - .6f, .3f, .38f, zf - .06f, zf + .01f, false);
            float d0 = (a + b) * .5f - .9f;
            BoxMinMax(GlassClear, d0, d0 + 1.8f, .02f, 2.6f, zf - .08f, zf - .04f, false);
            BoxMinMax(Steel, d0 + .85f, d0 + .95f, .9f, 1.7f, zf - .12f, zf - .08f, false);
            // markiza (stal) i szyld nad nią
            BoxMinMax(Frame, a + .4f, b - .4f, 3.62f, 3.72f, zf - 1.6f, zf, false, true);
            Sign(shops[k], new Vector3((a + b) * .5f, 3.95f, zf - .12f), Mathf.Min(6f, unit - 2f), 1.5f * Mathf.Min(6f, unit - 2f) / 6f, Vector3.back);
            // okna apartamentów
            for (float x = a + 1.4f; x < b - 1.2f; x += 3.2f)
            {
                BoxMinMax(Glass, x, x + 2.0f, H0 + .6f, H0 + 2.9f, zf + .77f, zf + .83f, false);
                BoxMinMax(Frame, x - .06f, x + 2.06f, H0 + 2.9f, H0 + 2.97f, zf + .75f, zf + .84f, false);
            }
            if (k > 0) BoxMinMax(Plinth, a - .15f, a + .15f, 0, H0, zf - .05f, zf + .02f, false);   // pilastry między lokalami
        }
        // ogródek kawiarni, donice i ławki na placyku
        float cx = x0 + 2.5f * unit;
        if (outdoorSet) for (int k = 0; k < 4; k++) Put(outdoorSet, new Vector3(cx - 4.5f + k * 3f, .045f, front + 3f), k * 25f, .03f, true);
        if (planter) for (float x = x0 + 3f; x < x1 - 2f; x += 9f) Put(planter, new Vector3(x, .045f, front + .9f), 0, .03f, false);
        if (bench) { Put(bench, new Vector3(x0 + unit * .5f, .045f, front + 1.4f), 180, .03f); Put(bench, new Vector3(x0 + unit * 3.5f, .045f, front + 1.4f), 180, .03f); rep.benches += 2; }
        rep.shops = shops.Length;
    }

    // Klub tenisowy: kort deblowy 23.77 x 10.97 z wybiegami, siatka, ogrodzenie 3 m z ciemnozieloną osłoną,
    // pawilon klubowy z tarasem. Działka x0..x1, od ulicy front, w głąb dir.
    static void TennisClub(float x0, float x1, float front, float dir)
    {
        group = "Osiedle B / klub tenisowy";
        var acryl = Plain("kort akryl niebieski", new Color(.16f, .33f, .52f), .25f); var out_ = Plain("kort akryl zielony", new Color(.20f, .38f, .29f), .25f);
        var screen = Plain("osłona kortu", new Color(.08f, .2f, .14f), .2f);
        float cx = (x0 + x1) * .5f, cz = front + dir * 13f;
        Top(out_, cx - 18f, cx + 18f, cz - 9f, cz + 9f, .035f);
        Top(acryl, cx - 11.885f, cx + 11.885f, cz - 5.485f, cz + 5.485f, .04f);
        void Line(float a0, float a1, float b0, float b1) => Top(Paint, Mathf.Min(a0, a1), Mathf.Max(a0, a1), Mathf.Min(b0, b1), Mathf.Max(b0, b1), .046f);
        foreach (float z in new[] { -5.485f, 5.485f, -4.115f, 4.115f }) Line(cx - 11.885f, cx + 11.885f, cz + z - .025f, cz + z + .025f);
        foreach (float x in new[] { -11.885f, 11.885f }) Line(cx + x - .025f, cx + x + .025f, cz - 5.485f, cz + 5.485f);
        foreach (float x in new[] { -6.4f, 6.4f }) Line(cx + x - .025f, cx + x + .025f, cz - 4.115f, cz + 4.115f);
        Line(cx - 6.4f, cx + 6.4f, cz - .025f, cz + .025f);
        // siatka
        foreach (float z in new[] { -6.4f, 6.4f }) Box(Steel, new Vector3(cx, .53f, cz + z), new Vector3(.08f, 1.07f, .08f));
        Box(screen, new Vector3(cx, .6f, cz), new Vector3(.02f, .9f, 12.8f), false);
        Box(Paint, new Vector3(cx, 1.06f, cz), new Vector3(.03f, .06f, 12.8f), false);
        Collider(new Vector3(cx, .5f, cz), new Vector3(.1f, 1f, 12.8f));
        // ogrodzenie 3 m: słupki co 3 m + ciemna osłona do 2.4 m, wejście od strony ulicy
        float fx0 = cx - 18f, fx1 = cx + 18f, fz0 = cz - 9f, fz1 = cz + 9f;
        for (float x = fx0; x <= fx1 + .01f; x += 3f) foreach (float z in new[] { fz0, fz1 }) Box(Steel, new Vector3(x, 1.5f, z), new Vector3(.08f, 3f, .08f), false);
        for (float z = fz0; z <= fz1 + .01f; z += 3f) foreach (float x in new[] { fx0, fx1 }) Box(Steel, new Vector3(x, 1.5f, z), new Vector3(.08f, 3f, .08f), false);
        float gate = dir > 0 ? fz0 : fz1;
        foreach (float z in new[] { fz0, fz1 })
        {
            if (z == gate) { BoxMinMax(screen, fx0, cx - 1.2f, .1f, 2.4f, z - .02f, z + .02f, true); BoxMinMax(screen, cx + 1.2f, fx1, .1f, 2.4f, z - .02f, z + .02f, true); }
            else BoxMinMax(screen, fx0, fx1, .1f, 2.4f, z - .02f, z + .02f, true);
        }
        foreach (float x in new[] { fx0, fx1 }) BoxMinMax(screen, x - .02f, x + .02f, .1f, 2.4f, fz0, fz1, true);
        foreach (float z in new[] { fz0, fz1 }) BoxMinMax(Steel, fx0, fx1, 2.98f, 3.04f, z - .03f, z + .03f, false);
        // pawilon klubowy w głębi działki
        float pz0 = cz + dir * 10.5f, pz1 = cz + dir * 16.5f;
        BoxMinMax(Wood, cx - 7f, cx + 7f, 0, 3.2f, Mathf.Min(pz0, pz1), Mathf.Max(pz0, pz1));
        BoxMinMax(Stucco, cx - 7.6f, cx + 7.6f, 3.2f, 3.5f, Mathf.Min(pz0, pz1) - .9f, Mathf.Max(pz0, pz1) + .3f, false, true);
        float face = pz0 - dir * .02f;
        BoxMinMax(Glass, cx - 5.5f, cx + 5.5f, .3f, 2.9f, Mathf.Min(face, face - dir * .05f), Mathf.Max(face, face - dir * .05f), false);
        Sign("kort", new Vector3(cx + 6f, 2.5f, cz - dir * 9.12f), 5f, 1.25f, new Vector3(0, 0, -dir));
        if (bench) { Put(bench, new Vector3(cx - 4f, .035f, cz - dir * 10.2f), dir > 0 ? 0 : 180, .03f); Put(bench, new Vector3(cx + 4f, .035f, cz - dir * 10.2f), dir > 0 ? 0 : 180, .03f); rep.benches += 2; }
        // ścieżka z ulicy do bramki kortu i płot działki
        Top(Slabs, cx - 1.2f, cx + 1.2f, Mathf.Min(front, gate), Mathf.Max(front, gate), .045f);
        FenceX(x0, x1, front + dir * .3f, 1.55f, (cx - 1.4f, cx + 1.4f));
        WoodFenceZ(front + dir * .6f, dir > 0 ? ParkZ0 - .3f : SouthZ + .3f, x0 + .1f);
        rep.tennis = 1;
    }

    static void Guardhouse(float x, float z)
    {
        group = "Park Centralny / stróżówka";
        BoxMinMax(Stucco, x - 1.6f, x + 1.6f, 0, 2.8f, z - 1.6f, z + 1.6f);
        BoxMinMax(Frame, x - 1.8f, x + 1.8f, 2.8f, 3.0f, z - 1.8f, z + 1.8f, false, true);
        BoxMinMax(Glass, x + 1.58f, x + 1.63f, .9f, 2.3f, z - 1.2f, z + 1.2f, false);
        BoxMinMax(Glass, x - 1.2f, x + 1.2f, .9f, 2.3f, z + 1.58f, z + 1.63f, false);
        BoxMinMax(Wood, x - .5f, x + .5f, .02f, 2.2f, z - 1.66f, z - 1.58f, false);
        Sign("ochrona", new Vector3(x, 2.5f, z + 1.68f), 1.8f, .45f, Vector3.forward);
    }

    // Elewacje budynków Codexa (kasyno, dyskoteka, komisariat, szpital, sala bokserska, hala walk):
    // okładzina 3 cm przed starymi ścianami (bez kolizji, stare ściany zostają), z otworami wejść,
    // obłożone pilastry i nowy gzyms. Okna, szyldy, daszek i drzwi zostają na wierzchu.
    static void CivicFacades()
    {
        group = "Osiedle A / elewacje budynków";
        var panel = Plain("panele kompozytowe antracyt", new Color(.15f, .16f, .17f), .5f, .2f);
        var list = new (string id, float x, float z, float h, Material m)[]
        {
            ("kasyno", 86f, -42f, 7f, Stone), ("dyskoteka", 86f, 14f, 7f, WoodDark), ("komisariat", -86f, -42f, 4.5f, StuccoWarm),
            ("szpital", -86f, 14f, 4.5f, Stucco), ("sala bokserska", 86f, 64f, 7f, Plinth), ("hala walk", -86f, 64f, 7f, panel),
        };
        const float w = 40f, d = 30f, t = .03f, off = .2f;   // okładzina 15.185..15.215: między starą ścianą (15.175) a czołem okien (15.24)
        foreach (var (id, cx, cz, h, m) in list)
        {
            foreach (int side in new[] { -1, 1 })   // ściany boczne
                BoxMinMax(m, cx + side * (w * .5f + off) - t * .5f, cx + side * (w * .5f + off) + t * .5f, 0, h, cz - d * .5f - off, cz + d * .5f + off, false);
            foreach (int face in new[] { -1, 1 })   // przód i tył: dwa skrzydła + nadproże nad wejściem (otwór 4 x 3.1 m)
            {
                float zf = cz + face * (d * .5f + off);
                foreach (int side in new[] { -1, 1 }) BoxMinMax(m, Mathf.Min(cx + side * 2f, cx + side * (w * .5f + off)), Mathf.Max(cx + side * 2f, cx + side * (w * .5f + off)), 0, h, zf - t * .5f, zf + t * .5f, false);
                BoxMinMax(m, cx - 2f, cx + 2f, 3.1f, h, zf - t * .5f, zf + t * .5f, false);
            }
            for (float a = -w * .5f; a <= w * .5f + .01f; a += w / 8f)   // pilastry frontu
                if (Mathf.Abs(a) > 2f) BoxMinMax(m, cx + a - .19f, cx + a + .19f, 0, h, cz + d * .5f + .05f, cz + d * .5f + .56f, false);
            BoxMinMax(Frame, cx - w * .5f - .54f, cx + w * .5f + .54f, h - .02f, h + .34f, cz - d * .5f - .54f, cz + d * .5f + .54f, false);
            rep.facades++;
        }
    }

    // ---------------- park ----------------
    static void Park()
    {
        group = "Park Centralny";
        float z0 = ParkZ0, z1 = FenceA - .6f, zc = (z0 + z1) * .5f;     // z1 = -102.6
        // aleja N-S (oś bramy) i W-E, plac z fontanną na skrzyżowaniu
        float r = 9f;
        Top(ParkPath, AxisX - 3f, AxisX + 3f, z0, zc - r + .4f, .03f);
        Top(ParkPath, AxisX - 3f, AxisX + 3f, zc + r - .4f, z1 + .6f, .03f);
        Top(ParkPath, X0 + 4f, AxisX - r + .4f, zc - 2f, zc + 2f, .03f);
        Top(ParkPath, AxisX + r - .4f, X1 - 4f, zc - 2f, zc + 2f, .03f);
        Disc(ParkPath, new Vector3(AxisX, .046f, zc), r, 48);
        // fontanna: kamienna misa, woda, kolumna z czaszą
        var fc = new Vector3(AxisX, 0, zc);
        Cylinder(Stone, fc, 3.6f, .55f, 40, false, true);
        Ring(Stone, fc + Vector3.up * .55f, 3.05f, 3.6f, 40);
        Cylinder(Stone, fc + Vector3.up * .42f, 3.05f, .13f, 40, false, false, true);
        Disc(Water, fc + Vector3.up * .42f, 3.05f, 40);
        Cylinder(Stone, fc + Vector3.up * .42f, .45f, 1.25f, 20, true, false);
        Cylinder(Stone, fc + Vector3.up * 1.6f, 1.15f, .2f, 28, true, false);
        Disc(Water, fc + Vector3.up * 1.81f, 1.0f, 28);
        // ławki wokół placu i wzdłuż alei
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 30f) * Mathf.Deg2Rad; var p = fc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 7.6f;
            if (bench && Put(bench, new Vector3(p.x, .03f, p.z), 90f - k * 60f - 30f + 180f, .03f)) rep.benches++;
        }
        for (float x = X0 + 14f; x < X1 - 10f; x += 18f)
        {
            if (Mathf.Abs(x - AxisX) < 14f) continue;
            bool northSide = ((int)((x - X0) / 18f)) % 2 == 0;
            if (bench && Put(bench, new Vector3(x, .03f, zc + (northSide ? 3.1f : -3.1f)), northSide ? 180 : 0, .03f)) rep.benches++;
            Lamp(new Vector3(x + 9f, 0, zc + (northSide ? -2.6f : 2.6f)), northSide ? 0 : 180);
            if (bin && (++binTurn) % 2 == 0 && Put(bin, new Vector3(x + 1.6f, .03f, zc + (northSide ? 3.2f : -3.2f)), 0, .02f)) rep.bins++;
        }
        for (float z = z0 + 6f; z < z1 - 4f; z += 14f)
        {
            if (Mathf.Abs(z - zc) < 12f) continue;
            Lamp(new Vector3(AxisX - 3.6f, 0, z), 90);
            if (bench && Put(bench, new Vector3(AxisX + 3.7f, .03f, z + 7f), 270, .03f)) rep.benches++;
        }
        // plac zabaw (bezpieczna nawierzchnia, huśtawki, wieża z daszkiem, ławeczka)
        {
            float px = -72f, pz = -140f;
            Top(Rubber, px - 7f, px + 7f, pz - 5f, pz + 5f, .035f);
            foreach (float sx in new[] { -4.2f, -.6f })            // dwie ramy huśtawki (A) + belka
            {
                Box(Steel, new Vector3(px + sx, 1.25f, pz - 2.6f), new Vector3(.1f, 2.5f, .1f)); Box(Steel, new Vector3(px + sx, 1.25f, pz - 1.4f), new Vector3(.1f, 2.5f, .1f));
            }
            Box(Steel, new Vector3(px - 2.4f, 2.48f, pz - 2f), new Vector3(3.8f, .12f, .12f), false);
            foreach (float sx in new[] { -3.3f, -1.5f })
            {
                Box(Steel, new Vector3(px + sx - .25f, 1.6f, pz - 2f), new Vector3(.02f, 1.7f, .02f), false); Box(Steel, new Vector3(px + sx + .25f, 1.6f, pz - 2f), new Vector3(.02f, 1.7f, .02f), false);
                Box(PlayGreen, new Vector3(px + sx, .72f, pz - 2f), new Vector3(.6f, .06f, .25f), false);
            }
            foreach (float tx in new[] { 2.2f, 4.2f }) foreach (float tz in new[] { -1.2f, .8f }) Box(Deck, new Vector3(px + tx, 1.5f, pz + tz), new Vector3(.14f, 3f, .14f));
            Box(Deck, new Vector3(px + 3.2f, 1.2f, pz - .2f), new Vector3(2.2f, .12f, 2.2f), false);
            Box(PlayGreen, new Vector3(px + 3.2f, 3.05f, pz - .2f), new Vector3(2.5f, .1f, 2.5f), false);
            Box(PlayGreen, new Vector3(px + 3.2f, .6f, pz + 1.6f), new Vector3(.7f, .06f, 2.0f), false);     // ześlizg (prosty)
            if (bench) { Put(bench, new Vector3(px - 1f, .035f, pz + 4.2f), 180, .03f); rep.benches++; }
        }
        // pergola nad aleją przy placu (drewno, bez pnączy)
        foreach (float gx in new[] { AxisX - 26f, AxisX + 16f })
        {
            for (float x = gx; x <= gx + 10f; x += 2.5f) foreach (float side in new[] { -2.3f, 2.3f }) Box(Deck, new Vector3(x, 1.35f, zc + side), new Vector3(.18f, 2.7f, .18f));
            foreach (float side in new[] { -2.3f, 2.3f }) Box(Deck, new Vector3(gx + 5f, 2.78f, zc + side), new Vector3(10.6f, .16f, .2f), false);
            for (float x = gx - .2f; x <= gx + 10.2f; x += .6f) Box(Deck, new Vector3(x, 2.92f, zc), new Vector3(.08f, .12f, 5.4f), false);
        }
        // donice z kwiatami wokół placu z fontanną
        if (planter) for (int k = 0; k < 6; k++) { float a = k * 60f * Mathf.Deg2Rad; Put(planter, new Vector3(AxisX + Mathf.Cos(a) * 5.3f, .046f, zc + Mathf.Sin(a) * 5.3f), k * 60f + 90f, .03f, false); }
        // kępy trawy przy alejach i kwiaty przy placu
        for (float x = X0 + 6f; x < X1 - 6f; x += 5.5f)
        {
            if (Mathf.Abs(x - AxisX) < 11f) continue;
            Tuft(new Vector3(x, 0, zc + 2.45f + (float)tuftRnd.NextDouble() * .4f)); Tuft(new Vector3(x + 2.7f, 0, zc - 2.45f - (float)tuftRnd.NextDouble() * .4f));
        }
        for (int k = 0; k < 18; k++) { float a = k * 20f * Mathf.Deg2Rad; Tuft(new Vector3(AxisX + Mathf.Cos(a) * 9.6f, 0, zc + Mathf.Sin(a) * 9.6f), true, 1.2f); }
        // stoły piknikowe na trawnikach
        foreach (var p in new[] { new Vector2(-80, -114), new Vector2(-60, -139), new Vector2(40, -113), new Vector2(75, -138) })
            if (picnic && Put(picnic, new Vector3(p.x, 0, p.y), 15f, .03f)) rep.tables++;
        // aleje drzew: wzdłuż alei W-E i N-S, kilka grup na trawnikach
        var rnd = new System.Random(2031);
        for (float x = X0 + 10f; x < X1 - 6f; x += 22f)
        {
            if (Mathf.Abs(x - AxisX) < 16f) continue;
            foreach (float side in new[] { -1f, 1f })
                if (Tree(new Vector3(x + (float)rnd.NextDouble() * 2f, 0, zc + side * 6.2f), 7f + (float)rnd.NextDouble() * 2.2f, rnd.Next(360))) rep.parkTrees++;
        }
        for (float z = z0 + 6f; z < z1 - 3f; z += 15f)
        {
            if (Mathf.Abs(z - zc) < 13f) continue;
            foreach (float side in new[] { -1f, 1f })
                if (Tree(new Vector3(AxisX + side * 6.5f, 0, z), 6.5f + (float)rnd.NextDouble() * 2f, rnd.Next(360))) rep.parkTrees++;
        }
        foreach (var t in Group(group).GetComponentsInChildren<LODGroup>(true).Where(l => l.name.StartsWith("Drzewo")).Select(l => l.transform.position).ToList())
            for (int k = 0; k < 3; k++) Tuft(t + new Vector3(Mathf.Cos(k * 2.1f + t.x) * .9f, 0, Mathf.Sin(k * 2.1f + t.z) * .9f));
        foreach (var p in new[] { new Vector2(-95, -140), new Vector2(-88, -110), new Vector2(15, -140), new Vector2(60, -110), new Vector2(98, -142) })
        {
            if (Tree(new Vector3(p.x, 0, p.y), 8.5f, rnd.Next(360))) rep.parkTrees++;
            for (int k = 0; k < 3; k++) Shrub(new Vector3(p.x + 3f + k * 1.4f, 0, p.y - 2.5f + k), .36f + k * .03f, rnd.Next(360));
        }
    }

    // ---------------- stary płot południowy osiedla A ----------------
    // Wycina z zbiorczych siatek osiedla A trójkąty leżące w całości w którejś z brył (stare płoty,
    // sztuczne żywopłoty) i wyłącza ich stare kolizje. Jedno przejście po siatkach.
    static void CutVolumes(Transform estate, Transform physics, List<Bounds> vols)
    {
        foreach (var mf in estate.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mf.sharedMesh) continue;
            var hit = vols.Where(v => mr.bounds.Intersects(v)).ToArray(); if (hit.Length == 0) continue;
            var src = mf.sharedMesh; Vector3[] v3;
            try { v3 = src.vertices; } catch { rep.notes.Add("nieczytelna siatka " + src.name); continue; }
            if (v3 == null || v3.Length == 0) continue;
            var M = mf.transform.localToWorldMatrix; var w = v3.Select(p => M.MultiplyPoint3x4(p)).ToArray();
            bool Inside(int a, int b, int c) { foreach (var v in hit) if (v.Contains(w[a]) && v.Contains(w[b]) && v.Contains(w[c])) return true; return false; }
            Mesh copy = null; int removed = 0;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tri = src.GetTriangles(s); var keep = new List<int>(tri.Length);
                for (int k = 0; k < tri.Length; k += 3)
                {
                    if (Inside(tri[k], tri[k + 1], tri[k + 2])) { removed++; continue; }
                    keep.Add(tri[k]); keep.Add(tri[k + 1]); keep.Add(tri[k + 2]);
                }
                if (keep.Count != tri.Length)
                {
                    if (!copy) { copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + "_remont"; }
                    copy.SetTriangles(keep, s);
                }
            }
            if (copy)
            {
                AssetDatabase.CreateAsset(copy, Out + "/cut_" + Safe(mf.name) + ".asset");
                mf.sharedMesh = copy; var mc = mf.GetComponent<MeshCollider>(); if (mc) mc.sharedMesh = copy;
                rep.oldFenceTrianglesRemoved += removed; rep.notes.Add("remont A: " + mf.name + " -" + removed + " trójkątów");
            }
        }
        if (physics)
            foreach (var c in physics.GetComponentsInChildren<BoxCollider>(true))
            {
                var b = c.bounds; if (!c.enabled || b.size.y > 2.2f) continue;
                if (vols.Any(v => { var e = v; e.Expand(new Vector3(.3f, .6f, .3f)); return e.Contains(b.min) && e.Contains(b.max); }))
                { c.enabled = false; rep.oldFenceCollidersDisabled++; }
            }
    }
    static Bounds MinMax(float x0, float x1, float y0, float y1, float z0, float z1)
    { var b = new Bounds(); b.SetMinMax(new Vector3(Mathf.Min(x0, x1), y0, Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), y1, Mathf.Max(z0, z1))); return b; }

    static void WoodFenceX(float x0, float x1, float z, float h = 1.6f)
    {
        if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
        float len = x1 - x0, mid = (x0 + x1) * .5f;
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
        for (int k = 0; k <= posts; k++) Box(Frame, new Vector3(x0 + k * len / posts, h * .5f, z), new Vector3(.09f, h, .09f), false);
        for (int sl = 0; sl < 9; sl++)
        {
            float y = .12f + sl * (h - .2f) / 8.5f;
            Box(WoodDark, new Vector3(mid, y + .07f, z), new Vector3(len, .14f, .04f), false);
        }
        Collider(new Vector3(mid, h * .5f, z), new Vector3(len, h, .12f));
    }

    // ---------------- remont osiedla A ----------------
    // Źródło: dev/osiedle/world.js i civic.js (współrzędne three.js; Unity X = -X, układ symetryczny).
    static readonly (float x, float z, float hd)[] Houses = { (-28, 27, 10), (27, 28, 10), (-29, 0, 12), (28, 0, 12), (-28, 59, 10), (28, 59, 10) };
    static void RenovateA(Transform estate, Transform physics)
    {
        // 1) bryły do wycięcia: obwodowe żywopłoty (bez bramy północnej), płoty działek
        var vols = new List<Bounds>
        {
            new Bounds(new Vector3(0, 1.48f, -102.05f), new Vector3(230f, 2.97f, 1.7f)),             // południe (pod park)
            MinMax(-114.9f, -113.1f, -.005f, 1.45f, -101.2f, 99.3f), MinMax(113.1f, 114.9f, -.005f, 1.45f, -101.2f, 99.3f), // wschód / zachód
            MinMax(-114.25f, -5.85f, -.005f, 1.45f, 97.15f, 98.85f), MinMax(5.85f, 114.25f, -.005f, 1.45f, 97.15f, 98.85f),  // północ bez bramy
        };
        // stare krawężniki ulicy głównej (box .18 x .14, x=±4.6, z -24..98) — zastępują je granitowe z przerwami
        vols.Add(MinMax(-4.72f, -4.48f, -.005f, .16f, -24.1f, 98.1f)); vols.Add(MinMax(4.48f, 4.72f, -.005f, .16f, -24.1f, 98.1f));
        foreach (var h in Houses)
        {
            float xu = -h.x, fxu = -(h.x < 0 ? h.x + 11.5f : h.x - 11.5f);
            vols.Add(MinMax(xu - 11.75f, xu + 11.75f, -.005f, 1.45f, h.z - h.hd - .25f, h.z - h.hd + .25f));
            vols.Add(MinMax(xu - 11.75f, xu + 11.75f, -.005f, 1.45f, h.z + h.hd - .25f, h.z + h.hd + .25f));
            vols.Add(MinMax(fxu - .25f, fxu + .25f, -.005f, 1.45f, h.z - h.hd - .1f, h.z + h.hd + .1f));
        }
        CutVolumes(estate, physics, vols);

        // 1a) dom-07: szara bryła bez elewacji (sama skorupa) -> nowa willa na tej samej działce (x 16.5..39.5, z 49..69)
        var dom07 = MinMax(17.9f, 39.2f, .05f, 14f, 49.6f, 68.4f);
        CutVolumes(estate, physics, new List<Bounds> { dom07 });
        if (physics)
            foreach (var col in physics.GetComponentsInChildren<Collider>(true))
            {
                var cb = col.bounds; var e = dom07; e.Expand(new Vector3(.4f, 1f, .4f));
                if (col.enabled && e.Contains(cb.min) && e.Contains(cb.max)) { col.enabled = false; rep.oldFenceCollidersDisabled++; }
            }
        Villa(13, 49f, 69f, 16.5f, 1f, true, 23f, false, false, "Osiedle A / dom-07 nowa willa");
        rep.notes.Add("dom-07: szara bryła usunięta, nowa willa na działce");

        // 1b) „kulkowe” drzewa (zielone sfery na brązowym walcu) -> drzewo Verdant w tym samym miejscu.
        //     Kolizja pnia zostaje oryginalna (okrągły collider w fizyce osiedla).
        var rends = estate.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterial).ToArray();
        bool Mat(MeshRenderer r, string n) => r.sharedMaterial.name.Replace(" (Instance)", "") == n;
        var trunks = rends.Where(r => Mat(r, "estate_lit_1") && r.bounds.size.x < .7f && r.bounds.size.z < .7f && r.bounds.size.y > 2.5f && r.bounds.size.y < 8f).ToList();
        var crowns = rends.Where(r => (Mat(r, "estate_lit_2") || Mat(r, "estate_lit_3")) && Mathf.Max(r.bounds.size.x, r.bounds.size.y, r.bounds.size.z) < 5f && r.bounds.min.y > .8f).ToList();
        group = "Osiedle A / drzewa zamiast kulek";
        foreach (var tr in trunks)
        {
            var c = tr.bounds.center; if (dom07.Contains(new Vector3(c.x, 1f, c.z))) continue;
            var mine = crowns.Where(k => Mathf.Abs(k.bounds.center.x - c.x) < 2.2f && Mathf.Abs(k.bounds.center.z - c.z) < 2.2f).ToList();
            if (mine.Count == 0) continue;
            float top = mine.Max(k => k.bounds.max.y);
            foreach (var k in mine) k.enabled = false; tr.enabled = false;
            if (Tree(new Vector3(c.x, 0, c.z), Mathf.Clamp(top * 1.15f, 6.5f, 10f), (c.x * 37f + c.z * 11f) % 360f, false)) rep.ballTreesReplaced++;
        }

        // 2) nowe ogrodzenie obwodowe (kamień + stal) i płoty działek
        group = "Osiedle A / ogrodzenie";
        FenceZ(FenceA, 98.6f, X0 + .25f, 1.6f); FenceZ(FenceA, 98.6f, X1 - .25f, 1.6f);
        FenceX(X0, X1, 98f, 1.6f, (-6.2f, 6.2f));
        foreach (var h in Houses)
        {
            float xu = -h.x, fxu = -(h.x < 0 ? h.x + 11.5f : h.x - 11.5f);
            FenceZ(h.z - h.hd, h.z + h.hd, fxu, 1.45f, (h.z - 1.8f, h.z + 1.8f));
            WoodFenceX(xu - 11.5f, xu + 11.5f, h.z - h.hd); WoodFenceX(xu - 11.5f, xu + 11.5f, h.z + h.hd);
            TuftLine(new Vector3(xu - 11f, 0, h.z - h.hd), new Vector3(xu + 11f, 0, h.z - h.hd), 3.4f, 1f);
            TuftLine(new Vector3(xu - 11f, 0, h.z + h.hd), new Vector3(xu + 11f, 0, h.z + h.hd), 3.4f, -1f);
        }

        // 3) nawierzchnie: nowy asfalt, chodniki z kostki, malowanie (wyżej niż stare, bez migotania)
        group = "Osiedle A / drogi";
        const float ya = .055f, yw = .095f, yp = .062f;
        foreach (var (cx, cz, w, d) in new[] { (0f, 37f, 9f, 122f), (0f, -19f, 91f, 8f), (-43f, 10f, 5f, 53f), (43f, 10f, 5f, 53f), (0f, 39f, 91f, 5f),
                                               (0f, -19f, 224f, 7f), (0f, 39f, 224f, 7f), (0f, 86f, 224f, 7f), (-58f, 16f, 6f, 140f), (58f, 16f, 6f, 140f), (43f, -43f, 5f, 33f) })
            Top(Asphalt, -cx - w * .5f, -cx + w * .5f, cz - d * .5f, cz + d * .5f, ya);
        var walks = new List<(float, float, float, float)> { (-6f, 37f, 2.8f, 122f), (6f, 37f, 2.8f, 122f), (0f, -25f, 94f, 3.3f), (0f, -13f, 94f, 3.3f), (-47f, 10f, 2f, 53f), (47f, 10f, 2f, 53f) };
        foreach (float z in new[] { -19f, 39f, 86f }) foreach (float off in new[] { -5f, 5f }) walks.Add((0f, z + off, 224f, 2.8f));
        foreach (float x in new[] { -58f, 58f }) foreach (float a in new[] { -4.5f, 4.5f }) walks.Add((x + a, 16f, 2f, 140f));
        foreach (var (cx, cz, w, d) in walks)
        {
            float x0 = -cx - w * .5f, x1 = -cx + w * .5f, z0 = cz - d * .5f, z1 = cz + d * .5f;
            Box(Pavers, new Vector3((x0 + x1) * .5f, yw * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, yw, z1 - z0), false);
        }
        foreach (float z in new[] { -19f, 39f, 86f })
        {
            for (float x = -109f; x <= 109f; x += 8f) Top(Paint, x - 1.5f, x + 1.5f, z - .06f, z + .06f, yp);
            foreach (float x in new[] { -58f, 0f, 58f }) for (int a = -2; a <= 2; a++) Top(Paint, x + a - .25f, x + a + .25f, z - 1.2f, z + 1.2f, yp);
        }
        for (float z = -10f; z < 54f; z += 7f) Top(Paint, -.06f, .06f, z - 1.5f, z + 1.5f, yp);
        foreach (float z in new[] { -10f, 33f }) for (int x = -3; x <= 3; x++) Top(Paint, x - .275f, x + .275f, z - 1.2f, z + 1.2f, yp);
        // krawężniki wzdłuż ulicy głównej (jak w oryginale x=±4.6, tylko wyższe i granitowe), z przerwą na zebry
        foreach (float x in new[] { -4.6f, 4.6f })
            foreach (var (z0, z1) in new[] { (-15f, -11.4f), (-8.6f, 31.6f), (34.4f, 35.5f), (42.5f, 82.5f), (89.5f, 98f) })
                BoxMinMax(KerbStone, x - .1f, x + .1f, 0, .12f, z0, z1, false);
        rep.notes.Add("remont A: asfalt 11 pól, chodniki " + walks.Count + " pasów, płoty " + Houses.Length + " działek");
    }

    static void Sky()
    {
        const string path = "Assets/Source/Claude/Osiedle/sky/niebo_4k.jpg";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) { rep.notes.Add("brak nieba " + path); return; }
        imp.textureShape = TextureImporterShape.Texture2D; imp.sRGBTexture = true; imp.mipmapEnabled = false;
        imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp; imp.maxTextureSize = 4096;
        imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var shader = Shader.Find("Skybox/Panoramic"); if (!shader || !tex) { rep.notes.Add("brak shadera Skybox/Panoramic"); return; }
        var m = new Material(shader) { name = "Osiedle: niebo z chmurami" };
        m.SetTexture("_MainTex", tex); m.SetFloat("_Mapping", 1); m.SetFloat("_ImageType", 0); m.SetFloat("_Exposure", 1.05f); m.SetFloat("_Rotation", 40f);
        AssetDatabase.CreateAsset(m, Out + "/niebo.mat");
        RenderSettings.skybox = m;
        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) cam.clearFlags = CameraClearFlags.Skybox;
        rep.notes.Add("niebo: Skybox/Panoramic 4K");
    }

    // ---------------- główne ----------------
    public static void Apply()
    {
        rep = new Report(); mats.Clear(); tile.Clear(); batches.Clear(); groups.Clear(); group = "osiedle";
        try { Build(); rep.status = "ZBUDOWANO"; }
        catch (Exception e) { rep.status = "BŁĄD"; rep.error = e.Message; Debug.LogError("BRAXHORN_CLAUDE_OSIEDLE " + e); }
        rep.materials = mats.Count;
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/osiedle-claude.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_OSIEDLE " + JsonUtility.ToJson(rep));
    }

    static void Build()
    {
        if (!Directory.Exists(Tex)) throw new Exception("brak tekstur osiedla (unity-tools/claude/fetch_osiedle.py)");
        Directory.CreateDirectory(Out); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var estate = GameObject.Find("braxhorn-estate")?.transform;
        if (!estate) throw new Exception("brak korzenia braxhorn-estate");
        var physics = GameObject.Find("Original estate collisions")?.transform;
        verdant = estate.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "EstateMesh_439" || r.name == "EstateMesh_446" ||
                  r.name == "EstateMesh_453" || r.name == "EstateMesh_460" || r.name == "EstateMesh_467").GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First());
        if (verdant.Count < 5) rep.notes.Add("Verdant: znaleziono " + verdant.Count + "/5 części drzew");

        root = new GameObject("CLAUDE osiedle: park + osiedle B").transform;
        colliders = new GameObject("kolizje").transform; colliders.SetParent(root, false);
        outdoorSet = Model("outdoor_table_chair_set_01"); bench = Model("painted_wooden_bench"); bin = Model("metal_trash_can");
        picnic = Model("wooden_picnic_table"); planter = Model("planter_box_02");
        tufts = new[] { Model("grass_bermuda_01"), Model("grass_medium_02") }.Where(g => g).ToArray();
        flowers = new[] { Model("flower_gazania"), Model("dandelion_01") }.Where(g => g).ToArray();

        // 0) niebo z chmurami zamiast jednolitego tła; trawa osiedla A na fotoskanie (jedna, bez nakładania)
        Sky();
        var oldGround = estate.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "EstateMesh_273");
        if (oldGround && oldGround.bounds.size.x > 200f && oldGround.bounds.max.y < .1f)
        {
            oldGround.enabled = false; group = "Podłoże A";
            for (float z = -103f; z < 101f - .01f; z += 34f) Top(Grass, -116f, 116f, z, Mathf.Min(z + 34f, 101f), -.01f);
            rep.notes.Add("trawa A: EstateMesh_273 zastąpiona fotoskanem Grass004");
        }
        else rep.notes.Add("trawa A: nie znaleziono EstateMesh_273 o oczekiwanym kształcie");

        // 1) podłoże nowej części: trawa + fizyczna podłoga
        group = "Podłoże";
        float zA = FenceA - .6f, zG = -103f;     // stara trawa (three.js box 232x204) kończy się na z=-103
        for (float z = SouthZ; z < zG - .01f; z += 34f) Top(Grass, X0, X1, z, Mathf.Min(z + 34f, zG), -.02f);
        var floor = new GameObject("podłoga nowej części"); floor.transform.SetParent(colliders, false);
        floor.transform.position = new Vector3(0, -.15f, (SouthZ + zA) * .5f);
        floor.AddComponent<BoxCollider>().size = new Vector3(X1 - X0, .3f, zA - SouthZ);

        // 2) stary płot -> nowy, z bramami do parku
        RenovateA(estate, physics);
        CivicFacades();
        group = "Ogrodzenie";
        FenceX(X0, X1, FenceA, 1.6f, (AxisX - 4f, AxisX + 4f), (28f, 32f));
        foreach (float gx in new[] { AxisX - 4.3f, AxisX + 4.3f, 27.7f, 32.3f }) Box(Plinth, new Vector3(gx, 1.15f, FenceA), new Vector3(.62f, 2.3f, .62f));
        FenceZ(SouthZ, FenceA, X0 + .25f, 1.6f); FenceZ(SouthZ, FenceA, X1 - .25f, 1.6f); FenceX(X0, X1, SouthZ + .25f, 1.6f);

        // 3) park
        Park();

        // 4) osiedle B: ulica główna W-E, ulica N-S od alei parku, zebry
        group = "Osiedle B / ulice";
        float gapA = AxisX - Carriage - Kerb, gapB = AxisX + Carriage + Kerb;
        StreetX(X0 + .6f, X1 - .6f, StreetZ, (gapA - Green - Walk, gapB + Green + Walk));
        StreetNS(ParkZ0, StreetZ + Carriage, AxisX);
        Zebra(AxisX - Carriage, AxisX + Carriage, StreetZ + Carriage + .2f, StreetZ + Carriage + 3.2f, true);
        Zebra(AxisX + 9f, AxisX + 12f, StreetZ - Carriage, StreetZ + Carriage, false);
        // wjazd do parku: niska brama z kamiennymi słupami
        foreach (float gx in new[] { AxisX - 3.5f, AxisX + 3.5f }) Box(Plinth, new Vector3(gx, 1.1f, ParkZ0 - .2f), new Vector3(.6f, 2.2f, .6f));

        // 5) działki z willami
        float north = StreetZ + Half, south = StreetZ - Half;     // przednie granice działek
        int idx = 0;
        void Row(float a, float b, int count, float front, float dir)
        {
            float w = (b - a) / count;
            for (int k = 0; k < count; k++) Villa(idx++, a + k * w, a + (k + 1) * w, front, dir);
        }
        Row(X0 + .6f, AxisX - Half, 3, north, 1f);
        // pierzeja sklepów zamiast dwóch willi (od ulicy N-S), dalej dwie wille
        float shopEnd = AxisX + Half + 62f;
        ShopRow(AxisX + Half + .5f, shopEnd, north);
        WoodFenceZ(north + 6f, ParkZ0 - .3f, shopEnd + .2f);
        Row(shopEnd + .6f, X1 - .6f, 2, north, 1f);
        // rząd południowy: 6 działek, czwarta = klub tenisowy (miejsce na ustawkę 3 na 3)
        float pw = (X1 - X0 - 1.2f) / 6f;
        for (int k = 0; k < 6; k++)
        {
            float a = X0 + .6f + k * pw, b = a + pw;
            if (k == 3) TennisClub(a, b, south, -1f); else Villa(idx++, a, b, south, -1f);
        }
        Guardhouse(AxisX - 7f, FenceA - 2.4f);
        group = "Osiedle B / ogrodzenie";
        WoodFenceZ(north + .6f, ParkZ0 - .3f, X1 - .7f); WoodFenceZ(south - .6f, SouthZ + .3f, X1 - .7f);
        FenceZ(north + .3f, ParkZ0 - .3f, AxisX - Half - .25f, 1.55f);

        Flush();

        // 6) punkty podróży (TravelPoint) do nowych miejsc
        var world = UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w.area == "estate");
        if (world)
        {
            var add = new[]
            {
                new BraxCityPoint { id = "park_centralny", label = "Park Centralny", position = new Vector3(AxisX + 1.5f, .08f, -112f) },
                new BraxCityPoint { id = "fontanna", label = "Fontanna w parku", position = new Vector3(AxisX + 6f, .08f, -132f) },
                new BraxCityPoint { id = "osiedle_b", label = "Osiedle B", position = new Vector3(AxisX + 1.5f, .1f, -170f) },
                new BraxCityPoint { id = "osiedle_b_ulica", label = "Osiedle B, ulica", position = new Vector3(30f, .1f, StreetZ) },
                new BraxCityPoint { id = "willa_b", label = "Willa z basenem", position = new Vector3(55f, .1f, south - 1.5f) },
                new BraxCityPoint { id = "sklepy_b", label = "Sklepy na osiedlu B", position = new Vector3(AxisX + Half + 15f, .1f, north + 2.5f) },
                new BraxCityPoint { id = "kort_tenisowy", label = "Klub tenisowy (3 na 3)", position = new Vector3(X0 + .6f + 3.5f * (X1 - X0 - 1.2f) / 6f, .1f, south - 3f) },
            };
            world.points = world.points.Concat(add).ToArray(); rep.points = add.Length;
        }
        else rep.notes.Add("brak BraxCityWorld estate — bez nowych punktów");
        AssetDatabase.SaveAssets();
    }
}

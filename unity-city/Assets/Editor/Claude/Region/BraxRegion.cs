using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — region R1 „na gotowo” (Claude, testowy build gałęzi claude/region-20261030).
// Wejście: BraxRegion.Apply(world, root) wywołane na końcu BraxCityRebuild.Apply.
// Zasady właściciela: tylko materiały i modele z Poly Haven (CC0) i z biblioteki w repo,
// żadnych kolorowych klocków, rośliny i drzewa bez odchudzania, nic nie świeci.
// Każdy krok jest osobno zabezpieczony: błąd jednego kroku nie przerywa buildu.
public static class BraxRegion
{
    public const string PH = "Assets/Source/Claude/PolyHaven/";
    public const string Lib = "Assets/Source/Claude/Library/";
    public const string Out = "Assets/PlayableGenerated/ClaudeRegion";

    [Serializable]
    public sealed class Report
    {
        public string status = "NOT_RUN";
        public List<string> steps = new List<string>();
        public List<string> notes = new List<string>();
        public List<string> missingAssets = new List<string>();
        public int offsetMaterials, offsetRenderers;
        public int shrubs, grassClumps, flowers, rocks, trees, streetProps, beachProps;
        public int terraceFlowers, terracePlants;
        public bool pavilion, pavilionDoor, nutrition, nova;
        public Vector3 pavilionAt;
    }

    public static Report Rep = new Report();
    public static bool Enabled => BraxClaudeTowerTerrace.Enabled;
    public static System.Random Rnd = new System.Random(20261030);

    public static void Apply(BraxCityWorld world, Transform root)
    {
        if (!Enabled) return;
        Rep = new Report();
        Rnd = new System.Random(20261030);
        cityMaterials.Clear(); converted = 0; keptForeign = 0;
        Directory.CreateDirectory(Out);
        Directory.CreateDirectory(Out + "/Materials");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Physics.SyncTransforms();

        // Najpierw morze: stara łąka i las znad wody (jachty „w trawie”), zanim zieleń szuka trawników.
        Step("morze bez łąki i lasu nad wodą", () => BraxRegionSea.Apply(root));
        Step("wyspa na morzu (oktagon Ligi, pomost, otwarta)", () => BraxRegionIsland.Apply(root));
        Step("pawilon sportowy (szatnia + sala walk)", () => BraxRegionPavilion.Apply(world, root));
        Step("tyły siłowni: plac, wieżowce A/B, wiadukt z portalem, warsztaty", () => BraxRegionBackyard.Apply(world, root));
        Step("taras wieżowca: kwiaty i rośliny", () => BraxRegionTerrace.Apply(root));
        Step("wnętrza: Nutrition i NOVA", () => BraxRegionInteriors.Apply(root));
        Step("interior mapping: pokoje za oknami budynków nieużytkowych", () => BraxRegionInteriorMap.Apply(root));
        Step("elewacje ulicy: prawdziwe okna, gzymsy, obraz w lobby", () => BraxRegionFacades.Apply(root));
        Step("stare drzewa-atrapy (kule na patykach) → prawdziwe drzewa", () => BraxRegionOldTrees.Apply(root));
        Step("zieleń naturalna i mała architektura", () => BraxRegionVegetation.Apply(root));
        // Lampy wnętrz: CityStaticPBR liczy światła punktowe tylko ze slotów „vertex light” (ForceVertex).
        Step("siedziska: sofy, ławki, hokery, leżaki → BraxSeat", () => BraxRegionSeats.Apply(root));
        Step("lampy wnętrz w trybie ForceVertex", () => InteriorLights(root));
        // Na końcu: przesunięcie głębi dla wszystkiego, co dobudowane (anty-migotanie).
        Step("anty-migotanie (offset głębi, trilinear)", () => BraxRegionFlicker.Apply(root));

        // Siatki zrobione po BraxCityArchitecture.Finish nie mają plików: zapisujemy je, żeby build był deterministyczny.
        Step("utrwalenie siatek", () => PersistMeshes(root));
        // Budżet: nic wyłączonego nie jedzie do paczki (397 siatek aut, zastąpione elewacje, atrapy), kompresja siatek.
        Step("odchudzenie danych: wyłączone renderery i kompresja siatek", () => Slim(root));
        Note("materiały modeli → CityStaticPBR: " + converted + ", bez konwersji: " + keptForeign);
        Rep.status = "BUILT";
        File.WriteAllText(Out + "/region.json", JsonUtility.ToJson(Rep, true));
        Debug.Log("BRAXHORN_CLAUDE_REGION " + JsonUtility.ToJson(Rep));
    }

    // Wszystkie światła punktowe/stożkowe pod korzeniem renowacji (hol wieżowca, metro, NOVA, pawilon, sklep)
    // na ForceVertex: wtedy trafiają do unity_4LightPos* i CityStaticPBR liczy je per piksel.
    static void InteriorLights(Transform root)
    {
        int n = 0;
        foreach (var l in root.GetComponentsInChildren<Light>(true))
        {
            if (l.type != LightType.Point && l.type != LightType.Spot) continue;
            l.renderMode = LightRenderMode.ForceVertex;
            l.shadows = LightShadows.None;
            // Tłumienie Unity (1/(1+25·d²/r²)) jest ostre: przy 3 m od lampy o zasięgu 8 m zostaje 22 %.
            // Wartości dobierane wcześniej „na oko” pod ForcePixel podbijamy, żeby wnętrza były jasne.
            l.intensity *= 1.7f;
            l.range = Mathf.Max(l.range, 8f);
            n++;
        }
        Note("lampy wnętrz (ForceVertex): " + n);
    }

    // Wyłączony renderer nadal pakuje siatkę i tekstury do buildu. Usuwamy komponent (i MeshFilter, jeśli nie ma
    // na obiekcie włączonej kolizji z tą siatką). Kompresja siatek (Low) zmniejsza plik, nie pamięć ani jakość.
    static void Slim(Transform root)
    {
        int removed = 0, compressed = 0; var seen = new HashSet<Mesh>();
        foreach (var scope in new[] { root, BraxClaudeGround.SourceCity })
        {
            if (!scope) continue;
            foreach (var r in scope.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.enabled) continue;
                var mf = r.GetComponent<MeshFilter>();
                bool colliderUsesMesh = false;
                if (mf && mf.sharedMesh) foreach (var mc in r.GetComponents<MeshCollider>()) if (mc.enabled && mc.sharedMesh == mf.sharedMesh) colliderUsesMesh = true;
                UnityEngine.Object.DestroyImmediate(r);
                if (mf && !colliderUsesMesh) UnityEngine.Object.DestroyImmediate(mf);
                removed++;
            }
        }
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = mf.sharedMesh; if (!m || seen.Contains(m)) continue; seen.Add(m);
            string path = AssetDatabase.GetAssetPath(m);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/PlayableGenerated/")) continue;
            if (MeshUtility.GetMeshCompression(m) != ModelImporterMeshCompression.Off) continue;
            MeshUtility.SetMeshCompression(m, ModelImporterMeshCompression.Low); EditorUtility.SetDirty(m); compressed++;
        }
        Note("odchudzenie: usunięte wyłączone renderery " + removed + ", skompresowane siatki " + compressed);
    }

    static void PersistMeshes(Transform root)
    {
        Directory.CreateDirectory(Out + "/Meshes");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        int saved = 0, serial = 0;
        var seen = new HashSet<Mesh>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter ? filter.sharedMesh : null;
                if (!mesh || seen.Contains(mesh) || AssetDatabase.Contains(mesh)) continue;
                seen.Add(mesh);
                AssetDatabase.CreateAsset(mesh, Out + "/Meshes/region_" + (serial++).ToString("D5") + ".asset");
                saved++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        Note("utrwalone siatki: " + saved);
    }

    static void Step(string name, Action action)
    {
        try { action(); Rep.steps.Add(name + ": OK"); }
        catch (Exception e)
        {
            Rep.steps.Add(name + ": BŁĄD " + e.Message);
            Debug.LogError("BRAXHORN_CLAUDE_REGION " + name + ": " + e);
        }
    }

    public static void Note(string text) { Rep.notes.Add(text); Debug.Log("BRAXHORN_CLAUDE_REGION " + text); }

    // ---------------- tekstury Poly Haven ----------------

    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    static Texture2D Tex(string path)
    {
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>Czy dany zestaw tekstur Poly Haven jest dostępny (pobrany w buildzie).</summary>
    public static bool HasTexture(string slug)
    {
        return File.Exists(PH + "textures/" + slug + "/diffuse.jpg") ||
               File.Exists("Assets/Source/CityRebuild/textures/" + slug + "/2k/" + slug + "_diff_2k.jpg");
    }

    /// <summary>
    /// Materiał PBR z tekstur Poly Haven. Pierwszy dostępny slug z listy; metres = ile metrów
    /// świata na jedno powtórzenie tekstury. Brak wszystkich slugów = wyjątek (krok zgłosi błąd).
    /// </summary>
    public static Material Pbr(string name, float metres, Color? tint, params string[] slugs)
    {
        foreach (var slug in slugs)
        {
            Texture2D d = null, n = null, a = null;
            string ph = PH + "textures/" + slug + "/";
            string cr = "Assets/Source/CityRebuild/textures/" + slug + "/2k/";
            if (File.Exists(ph + "diffuse.jpg")) { d = Tex(ph + "diffuse.jpg"); n = Tex(ph + "normal.jpg"); a = Tex(ph + "arm.jpg"); }
            else if (File.Exists(cr + slug + "_diff_2k.jpg")) { d = Tex(cr + slug + "_diff_2k.jpg"); n = Tex(cr + slug + "_nor_gl_2k.png"); a = Tex(cr + slug + "_arm_2k.jpg"); }
            if (!d) { if (!Rep.missingAssets.Contains("tex:" + slug)) Rep.missingAssets.Add("tex:" + slug); continue; }
            string key = name + "|" + slug + "|" + metres.ToString("F3") + "|" + (tint.HasValue ? tint.Value.ToString() : "-");
            Material m;
            if (materials.TryGetValue(key, out m) && m) return m;
            string tintTag = tint.HasValue ? " " + ColorUtility.ToHtmlStringRGB(tint.Value) : "";
            m = BraxCityMaterials.Textured(name + " [" + slug + tintTag + "]", d, n, a, 1f / Mathf.Max(.05f, metres));
            if (tint.HasValue) { m.SetColor("_Color", tint.Value); EditorUtility.SetDirty(m); }
            materials[key] = m;
            return m;
        }
        throw new FileNotFoundException("Poly Haven: brak tekstur " + string.Join("/", slugs));
    }

    /// <summary>Drobne okucia (stal, szkło): materiał bez tekstury, tylko dla małych elementów.</summary>
    public static Material Fitting(string name, Color color, float roughness, float metallic)
    {
        return BraxCityMaterials.Surface(name, color, roughness, metallic);
    }

    public static readonly Color Steel = new Color(.62f, .63f, .64f);
    public static readonly Color Champagne = new Color(.66f, .58f, .46f);
    public static readonly Color Glass = new Color(.70f, .80f, .82f, .24f);

    // ---------------- modele Poly Haven i biblioteki ----------------

    static readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();

    /// <summary>Pierwszy istniejący model z listy id (Poly Haven .glb/.gltf lub biblioteka .glb). null = brak.</summary>
    // Drzewa Poly Haven to skany po 2–17 mln trójkątów (pine_tree_01 = 1,2 GB w paczce, raport 0a75e9f).
    // Na telefon: jedno drzewo do gry „drzewo_verdant” (pień + liście w 3 gęstościach, 49k/28k/13k, LOD) — wspólne z osiedlem Opusa.
    static readonly HashSet<string> HeavyTrees = new HashSet<string> { "pine_tree_01", "fir_tree_01", "island_tree_01", "island_tree_02", "island_tree_03", "jacaranda_tree" };
    public static GameObject Model(params string[] ids)
    {
        foreach (var rawId in ids)
        {
            string id = HeavyTrees.Contains(rawId) ? "drzewo_verdant" : rawId;
            GameObject cached;
            if (models.TryGetValue(id, out cached) && cached) return cached;
            foreach (var p in new[] { PH + "models/" + id + "/" + id + ".glb", PH + "models/" + id + "/" + id + ".gltf", Lib + id + ".glb" })
            {
                if (!File.Exists(p)) continue;
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
                if (go) { models[id] = go; return go; }
            }
            if (!Rep.missingAssets.Contains("model:" + id)) Rep.missingAssets.Add("model:" + id);
        }
        return null;
    }

    /// <summary>drzewo_verdant: węzły pien, liscie_0/1/2 → LODGroup 25 % / 10 % / 3 % (jak na osiedlu).</summary>
    public static void TreeLods(GameObject o)
    {
        Transform pien = null, l0 = null, l1 = null, l2 = null;
        foreach (var t in o.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "pien") pien = t; else if (t.name == "liscie_0") l0 = t; else if (t.name == "liscie_1") l1 = t; else if (t.name == "liscie_2") l2 = t;
        }
        if (!pien || !l0 || o.GetComponent<LODGroup>()) return;
        Renderer[] R(params Transform[] ts) => ts.Where(t => t).SelectMany(t => t.GetComponentsInChildren<Renderer>(true)).ToArray();
        var lod = o.AddComponent<LODGroup>();
        lod.SetLODs(new[] { new LOD(.25f, R(pien, l0)), new LOD(.10f, R(pien, l1 ? l1 : l0)), new LOD(.03f, R(pien, l2 ? l2 : (l1 ? l1 : l0))) });
        lod.RecalculateBounds();
        foreach (var r in R(l0, l1, l2)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
    }

    // Materiały z importu glTFast (Poly Haven, biblioteka) → CityStaticPBR: to samo słońce, ambient i lampy,
    // maska alfa (liście, kwiaty) i dwustronność co reszta miasta. Build 62be06c: kwiaty i trawy z shaderem
    // glTFast były czarnymi plamami. Tekstury zostają te same (bez kopii i ponownej kompresji jak w
    // BraxCityMaterials.Imported — przy ~200 teksturach to godziny buildu). Nieznany materiał zostaje bez zmian.
    static readonly Dictionary<Material, Material> cityMaterials = new Dictionary<Material, Material>();
    static int converted, keptForeign;
    public static Material CityMaterial(Material m)
    {
        if (!m) return m;
        Material c;
        if (cityMaterials.TryGetValue(m, out c)) return c;
        try { c = ConvertGltf(m); converted++; }
        catch (Exception e)
        {
            c = m; keptForeign++;
            if (keptForeign <= 8) Note("materiał bez konwersji: " + m.name + " (" + (m.shader ? m.shader.name : "?") + "): " + e.Message);
        }
        cityMaterials[m] = c;
        return c;
    }

    static Material ConvertGltf(Material src)
    {
        if (BraxCityMaterials.IsCityMaterial(src)) return src;
        if (!src.HasProperty("baseColorFactor") || !src.HasProperty("roughnessFactor") || !src.HasProperty("metallicFactor"))
        {
            // 31.10: drzewa za plażą były czarne — materiał nie był z glTFast (URP/Standard z paczki drzewo_verdant).
            // Konwersja z tego, co jest: kolor + mapa bazowa + normalna, reszta domyślna; liście jako cutout.
            string baseMap = src.HasProperty("_BaseMap") ? "_BaseMap" : src.HasProperty("_MainTex") ? "_MainTex" : null;
            string baseCol = src.HasProperty("_BaseColor") ? "_BaseColor" : src.HasProperty("_Color") ? "_Color" : null;
            if (baseMap == null && baseCol == null) throw new InvalidOperationException("nie jest materiałem glTFast ani Standard/URP");
            var sh2 = Shader.Find(BraxCityMaterials.ShaderName);
            if (!sh2) throw new InvalidOperationException("brak shadera " + BraxCityMaterials.ShaderName);
            var c2 = new Material(sh2) { name = src.name + " (miasto)" };
            c2.SetColor("_Color", baseCol != null ? src.GetColor(baseCol) : Color.white);
            c2.SetFloat("_Roughness", src.HasProperty("_Smoothness") ? 1f - src.GetFloat("_Smoothness") : src.HasProperty("_Glossiness") ? 1f - src.GetFloat("_Glossiness") : .85f);
            c2.SetFloat("_Metallic", src.HasProperty("_Metallic") ? src.GetFloat("_Metallic") : 0f);
            if (baseMap != null) CopyMap(src, c2, baseMap, "_MainTex");
            CopyMap(src, c2, "_BumpMap", "_BumpMap");
            c2.SetFloat("_NormalEncoding", 1); c2.SetFloat("_UseARMForAO", 0);
            bool cutout = src.IsKeywordEnabled("_ALPHATEST_ON") || src.renderQueue >= 2450 && src.renderQueue < 3000 || src.HasProperty("_Cutoff") && src.GetFloat("_Cutoff") > 0f && src.HasProperty("_AlphaClip") && src.GetFloat("_AlphaClip") > 0f;
            float cull2 = src.HasProperty("_Cull") ? src.GetFloat("_Cull") : 2f;
            c2.SetFloat("_Cull", cutout ? 0f : cull2); c2.doubleSidedGI = cutout;
            c2.SetFloat("_AlphaClip", cutout ? 1 : 0); c2.SetFloat("_Cutoff", cutout ? (src.HasProperty("_Cutoff") ? Mathf.Clamp01(src.GetFloat("_Cutoff")) : .5f) : 0);
            c2.SetFloat("_Transparent", 0); c2.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One); c2.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero); c2.SetFloat("_ZWrite", 1);
            c2.renderQueue = cutout ? 2450 : 2000; c2.SetOverrideTag("RenderType", cutout ? "TransparentCutout" : "Opaque");
            Directory.CreateDirectory(Out + "/Materials");
            AssetDatabase.CreateAsset(c2, Out + "/Materials/model-" + cityMaterials.Count + ".mat");
            return c2;
        }
        var sh = Shader.Find(BraxCityMaterials.ShaderName);
        if (!sh) throw new InvalidOperationException("brak shadera " + BraxCityMaterials.ShaderName);
        var c = new Material(sh) { name = src.name + " (miasto)" };
        c.SetColor("_Color", src.GetColor("baseColorFactor"));
        c.SetFloat("_Roughness", src.GetFloat("roughnessFactor"));
        c.SetFloat("_Metallic", src.GetFloat("metallicFactor"));
        CopyMap(src, c, "baseColorTexture", "_MainTex");
        CopyMap(src, c, "normalTexture", "_BumpMap");
        CopyMap(src, c, "metallicRoughnessTexture", "_ARM");
        CopyMap(src, c, "occlusionTexture", "_OcclusionMap");
        CopyMap(src, c, "emissiveTexture", "_EmissionMap");
        c.SetFloat("_BumpScale", BraxCityMaterials.Value(src, "normalTexture_scale", 1));
        c.SetFloat("_OcclusionStrength", BraxCityMaterials.Value(src, "occlusionTexture_strength", 1));
        var bump = c.GetTexture("_BumpMap");
        var bumpImporter = bump ? AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(bump)) as TextureImporter : null;
        c.SetFloat("_NormalEncoding", bumpImporter && bumpImporter.textureType == TextureImporterType.NormalMap ? 1 : 0);
        c.SetFloat("_UseARMForAO", 0);
        if (src.HasProperty("emissiveFactor")) c.SetColor("_EmissionColor", src.GetColor("emissiveFactor"));
        float cull = src.HasProperty("_CullMode") ? src.GetFloat("_CullMode") : BraxCityMaterials.Value(src, "_Cull", 2);
        c.SetFloat("_Cull", cull);
        c.doubleSidedGI = cull == 0;
        int mode = BraxCityMaterials.AlphaMode(src);
        float cutoff = BraxCityMaterials.Value(src, "alphaCutoff", .5f);
        c.SetFloat("_AlphaClip", mode == 1 ? 1 : 0);
        c.SetFloat("_Cutoff", mode == 1 ? Mathf.Clamp01(cutoff) : 0);
        c.SetFloat("_Transparent", mode == 2 ? 1 : 0);
        c.SetFloat("_SrcBlend", (float)(mode == 2 ? UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.One));
        c.SetFloat("_DstBlend", (float)(mode == 2 ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
        c.SetFloat("_ZWrite", mode == 2 ? 0 : 1);
        c.renderQueue = mode == 2 ? 3000 : mode == 1 ? 2450 : 2000;
        c.SetOverrideTag("RenderType", mode == 2 ? "Transparent" : mode == 1 ? "TransparentCutout" : "Opaque");
        Directory.CreateDirectory(Out + "/Materials");
        AssetDatabase.CreateAsset(c, Out + "/Materials/model-" + cityMaterials.Count + ".mat");
        return c;
    }

    static void CopyMap(Material src, Material dst, string from, string to)
    {
        if (!src.HasProperty(from)) return;
        var t = src.GetTexture(from);
        if (!t) return;
        dst.SetTexture(to, t);
        dst.SetTextureScale(to, src.GetTextureScale(from));
        dst.SetTextureOffset(to, src.GetTextureOffset(from));
        if (src.HasProperty(from + "_Rotation") && dst.HasProperty(to + "_Rotation")) dst.SetVector(to + "_Rotation", src.GetVector(from + "_Rotation"));
        if (src.HasProperty(from + "_texCoord") && dst.HasProperty(to + "_texCoord")) dst.SetFloat(to + "_texCoord", Mathf.Clamp01(src.GetFloat(from + "_texCoord")));
    }

    public static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>
    /// Stawia kopię modelu tak, że środek podstawy stoi w punkcie floor (y = poziom podłoża).
    /// scale: mnożnik; maxHeight > 0 ogranicza wysokość (skaluje w dół); solid: kolizja (box).
    /// </summary>
    public static GameObject Put(Transform parent, GameObject source, string label, Vector3 floor, float yaw,
                                 float scale = 1f, bool solid = true, float maxHeight = 0f, float sink = 0f)
    {
        if (!source) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(source, parent, false);
        o.name = label;
        o.transform.position = Vector3.zero;
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;   // obrót z importu zostaje (Sketchfab Z-up)
        if (Mathf.Abs(scale - 1f) > .0001f) o.transform.localScale *= scale;
        var b = Bounds(o);
        if (maxHeight > 0f && b.size.y > maxHeight) { o.transform.localScale *= maxHeight / b.size.y; b = Bounds(o); }
        o.transform.position = floor - new Vector3(b.center.x, b.min.y + sink, b.center.z);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            r.sharedMaterials = r.sharedMaterials.Select(CityMaterial).ToArray();
        }
        TreeLods(o);
        if (solid)
        {
            b = Bounds(o);
            var c = new GameObject(label + " kolizja").AddComponent<BoxCollider>();
            c.transform.SetParent(parent, false);
            c.transform.position = b.center;
            c.size = b.size;
        }
        return o;
    }

    public static GameObject Box(string name, Transform parent, Vector3 at, Vector3 size, Material m, bool solid = false, float metres = 1f)
    {
        return BraxCityArchitecture.Box(name, parent, at, size, m, solid, metres);
    }

    public static Light Warm(Transform parent, string name, Vector3 at, float intensity, float range)
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.transform.SetParent(parent, false);
        l.transform.position = at;
        l.type = LightType.Point;
        l.color = new Color(1f, .92f, .80f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForceVertex;   // CityStaticPBR: sloty vertex light liczone per piksel
        return l;
    }

    // ---------------- geometria i wolne miejsce ----------------

    public static bool Free(Vector3 center, Vector3 size)
    {
        return !Physics.CheckBox(center, size * .5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Najwyższe podłoże pod punktem (x, z) w zakresie yMin..yMax, normalna w górę.</summary>
    public static bool Ground(float x, float z, float yMin, float yMax, out RaycastHit best)
    {
        best = default(RaycastHit);
        bool found = false;
        foreach (var h in Physics.RaycastAll(new Vector3(x, yMax + 30f, z), Vector3.down, yMax - yMin + 30f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.normal.y < .75f || h.point.y < yMin || h.point.y > yMax) continue;
            if (!found || h.point.y > best.point.y) { best = h; found = true; }
        }
        return found;
    }

    public static float Range(float a, float b) { return a + (float)Rnd.NextDouble() * (b - a); }
    public static T Pick<T>(IList<T> list) { return list[Rnd.Next(list.Count)]; }

    public static bool IsGrassRenderer(Renderer r)
    {
        if (!r || !r.enabled) return false;
        foreach (var m in r.sharedMaterials)
        {
            if (!m) continue;
            string mn = m.name.ToLowerInvariant();
            if (mn.Contains("boisko") || mn.Contains("sztuczn") || mn.Contains("turf") || mn.Contains("kort")) return false;   // murawa boiska to nie trawnik
            if (m.shader && m.shader.name == "Braxhorn/ClaudeWorldGround" && mn.Contains("tra")) return true;
            if (BraxClaudeGround.LooksLikeGrass(m)) return true;
        }
        return false;
    }

    public static bool Under(Transform t, string prefix)
    {
        for (var p = t; p; p = p.parent)
            if (p.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public static Transform Root(Transform parent, string name)
    {
        var old = parent.Find(name);
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = Vector3.zero;
        return t;
    }
}

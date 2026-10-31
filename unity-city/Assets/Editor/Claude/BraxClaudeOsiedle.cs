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
        public int grassClumps, flowerClumps, flowerBeds, bushes, roofs, fakeRoomMaterials, fakeRoomWindowsA, forSale, flowerPlants, flowerPatches;
        public int tufts, ballTreesReplaced, shops, tennis, facades, villas, pools, parkTrees, plotTrees, shrubs, benches, lamps, bins, tables, oldFenceTrianglesRemoved,
                   oldFenceCollidersDisabled, meshes, triangles, materials, points;
        public List<string> notes = new List<string>();
        public List<string> smoothness = new List<string>();     // id: średnia mapy × mnożnik = gładkość efektywna (cel)
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
        // kolor i normalne w pełnych 2K (ostrość z bliska), mapy gładkości/AO 1K
        // budżet mobilny: 2K tylko to, co gracz ma pod nogami (trawa, chodniki, ścieżki, asfalt, tarasy, rabaty, posadzki);
        // elewacje i dachy 1K, normalne 1K (trawa 2K), gładkość/AO 512
        bool near = new[] { "grass004", "concrete_pavers", "concrete_pavement", "asphalt_04", "herringbone", "wood_floor_deck", "yellow_flower_mulch", "large_grey_tiles", "wood_floor/" }.Any(q => path.Contains(q));
        int size = path.EndsWith("albedo.jpg") ? (near ? 2048 : 1024) : path.EndsWith("normal.jpg") ? (path.Contains("grass004") ? 2048 : 1024) : 512;
        imp.sRGBTexture = srgb; imp.maxTextureSize = size; imp.mipmapEnabled = true;
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
        m.SetFloat("_GlossMapScale", GlossScale(id, d + "mg.png", smooth)); m.SetFloat("_Metallic", metal);
        m.SetTexture("_OcclusionMap", Map(d + "ao.jpg", false, false)); m.SetFloat("_OcclusionStrength", 1f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat");
        mats[key] = m; tile[m] = metres; return m;
    }
    // Gładkość liczona, nie na oko: build czyta średnią kanału A mapy mg.png (gładkość = 1 - szorstkość fotoskanu)
    // i dobiera mnożnik tak, żeby średnia efektywna = cel dla danej powierzchni (szorstkości typowe dla PBR).
    // Mnożnik w Standard ≤ 1 (tylko zmniejsza). Bez celu w tabeli zostaje mnożnik podany w wywołaniu.
    static readonly Dictionary<string, float> SmoothTarget = new Dictionary<string, float>
    {
        { "grass004", .10f },                    // trawnik, szorstkość ~0.9 (fotoskan ma 0.737 — błędnie gładki)
        { "asphalt_04", .18f }, { "concrete_pavement_02", .18f }, { "concrete_pavers_02", .15f }, { "concrete_pavers_03", .15f },
        { "herringbone_brick_02", .15f }, { "white_stucco_02", .12f }, { "painted_plaster_wall", .12f }, { "weathered_planks", .18f },
        { "white_sandstone_blocks_02", .16f }, { "rough_block_wall", .10f }, { "bicolour_gravel", .12f }, { "yellow_flower_mulch", .06f },
        { "wood_floor_deck", .25f },             // deska tarasowa olejowana (na zewnątrz)
        { "roof_slates_03", .30f }, { "clay_roof_tiles_02", .25f }, { "exterior_wall_cladding_02", .22f },
        { "wood_floor", .55f }, { "large_grey_tiles", .45f },     // wnętrza: parkiet lakierowany, gres
    };
    static readonly Dictionary<string, float> smoothMean = new Dictionary<string, float>();
    static float SmoothMean(string png)
    {
        if (smoothMean.TryGetValue(png, out var v)) return v;
        v = -1f;
        try
        {
            if (File.Exists(png))
            {
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t.LoadImage(File.ReadAllBytes(png)))
                {
                    var px = t.GetPixels32(); int step = Mathf.Max(1, px.Length / 262144), n = 0; double sum = 0;
                    for (int i = 0; i < px.Length; i += step) { sum += px[i].a; n++; }
                    v = (float)(sum / (255.0 * Mathf.Max(1, n)));
                }
                UnityEngine.Object.DestroyImmediate(t);
            }
        }
        catch (Exception e) { rep.notes.Add("gładkość " + png + ": " + e.Message); }
        smoothMean[png] = v; return v;
    }
    // Średni kolor albedo w przestrzeni liniowej (jpg → sRGB → liniowo), co 1/262144 piksela.
    static Vector3 AlbedoMeanLinear(string jpg)
    {
        var r = Vector3.zero;
        try
        {
            if (!File.Exists(jpg)) return r;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (t.LoadImage(File.ReadAllBytes(jpg)))
            {
                var px = t.GetPixels32(); int step = Mathf.Max(1, px.Length / 262144), n = 0; double sr = 0, sg = 0, sb = 0;
                for (int i = 0; i < px.Length; i += step)
                { sr += Mathf.GammaToLinearSpace(px[i].r / 255f); sg += Mathf.GammaToLinearSpace(px[i].g / 255f); sb += Mathf.GammaToLinearSpace(px[i].b / 255f); n++; }
                r = new Vector3((float)(sr / n), (float)(sg / n), (float)(sb / n));
            }
            UnityEngine.Object.DestroyImmediate(t);
        }
        catch (Exception e) { rep.notes.Add("albedo " + jpg + ": " + e.Message); }
        return r;
    }
    static float GlossScale(string id, string png, float fallback)
    {
        float mean = SmoothMean(png);
        float scale = mean > .01f && SmoothTarget.TryGetValue(id, out var target) ? Mathf.Clamp01(target / mean) : fallback;
        string line = id + ": " + (mean < 0 ? "brak mapy" : mean.ToString("0.000")) + " × " + scale.ToString("0.000") + " = " + (mean < 0 ? "?" : (mean * scale).ToString("0.000"))
                      + (SmoothTarget.TryGetValue(id, out var tg) ? " (cel " + tg.ToString("0.00") + ")" : " (bez celu)");
        if (!rep.smoothness.Contains(line)) rep.smoothness.Add(line);
        return scale;
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
    static Material Slate => Pbr("dach łupek grafit", "roof_slates_03", 2.2f, new Color(.80f, .82f, .86f), .9f);
    static Material ClayRoof => Pbr("dach dachówka ceramiczna", "clay_roof_tiles_02", 2.6f, new Color(.88f, .80f, .76f));
    static Material Clinker => Pbr("klinkier elewacyjny", "exterior_wall_cladding_02", 2.0f, new Color(1f, .97f, .95f));
    static Material Mulch => Pbr("rabata kora", "yellow_flower_mulch", 1.8f, new Color(.78f, .74f, .70f), .7f);
    static Material Graphite => Pbr("tynk grafitowy", "white_stucco_02", 2.5f, new Color(.30f, .31f, .33f));
    static Material Sand => Pbr("tynk piaskowy", "white_stucco_02", 2.5f, new Color(.93f, .86f, .74f));
    static Material Concrete => Pbr("beton architektoniczny", "concrete_pavement_02", 2.8f, new Color(.80f, .80f, .79f));
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
            m.SetFloat("_Gloss", GlossScale("grass004", d + "mg.png", .14f));      // ~0.10 efektywnie (było 0.44 — bielenie)
            m.SetFloat("_Tile", 2.5f);                                               // kafel 2.5 m (zlecenie trawy: 2–3 m, nie większy)
            // kolor liczony: średnia tekstury (liniowo) → plamy i suche miejsca względem średniej, odcień tak,
            // żeby luminancja albedo = 0.133 (wzorzec ColorChecker „Foliage”, sRGB 87/108/67)
            var am = AlbedoMeanLinear(d + "albedo.jpg");
            if (am.y > .01f)
            {
                m.SetFloat("_MacroMean", .3f * am.x + .59f * am.y + .11f * am.z); m.SetFloat("_DryMean", am.y);
                var baseLin = new Color(.90f, 1f, .88f).linear;
                float Y = .2126f * am.x * baseLin.r + .7152f * am.y * baseLin.g + .0722f * am.z * baseLin.b, k = .133f / Y;
                var lin = new Color(baseLin.r * k, baseLin.g * k, baseLin.b * k, 1f);
                m.color = lin.gamma;
                rep.notes.Add("trawnik: średnia tekstury liniowo " + am.ToString("F4") + ", luminancja tekstury " + (.2126f * am.x + .7152f * am.y + .0722f * am.z).ToString("0.000")
                              + ", odcień liniowo " + ((Vector4)lin).ToString("F3") + " → albedo Y 0.133");
            }
            AssetDatabase.CreateAsset(m, Out + "/trawnik.mat"); mats["trawnik"] = m; tile[m] = 3.5f; grassMat = m; return m;
        }
    }
    static Material grassMat;
    // Woda: ruchoma tafla (fale z sumy sinusów w shaderze), odbija niebo; awaryjnie gładki Standard.
    static Material Water
    {
        get
        {
            if (mats.TryGetValue("woda", out var have)) return have;
            var sh = Shader.Find("Braxhorn/OsiedleWoda");
            Material m;
            if (sh) m = new Material(sh) { name = "Osiedle: woda (ruchoma tafla)" };
            else { rep.notes.Add("brak shadera Braxhorn/OsiedleWoda — Standard"); m = new Material(Shader.Find("Standard")) { name = "Osiedle: woda", color = new Color(.18f, .42f, .52f) }; m.SetFloat("_Glossiness", .96f); }
            AssetDatabase.CreateAsset(m, Out + "/woda.mat"); mats["woda"] = m; tile[m] = 1f; return m;
        }
    }
    // Materiał z pierwszą teksturą koloru z importu glTFast (różne nazwy właściwości w zależności od shadera).
    static Texture SrcTex(Material m, params string[] names)
    {
        if (!m) return null;
        foreach (var n in names) if (m.HasProperty(n)) { var t = m.GetTexture(n); if (t) return t; }
        return null;
    }
    static Texture BaseTex(Material m) { if (!m) return null; var t = SrcTex(m, "baseColorTexture", "_BaseMap", "_MainTex", "_BaseColorMap"); return t ? t : m.mainTexture; }
    // Liście: shader korony (normalne od środka korony, obie strony jasne, świeża zieleń); środek/promień w układzie siatki.
    static Material LeafMat(string key, Material src, Bounds meshBounds, float meanGamma, float meanLinear, float recolor, Color leaf, Color leaf2)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var sh = Shader.Find("Braxhorn/OsiedleLiscie"); var tex = BaseTex(src);
        if (!sh || !tex) { rep.notes.Add("liście " + key + ": brak shadera lub tekstury — zostaje import"); return null; }
        if (key == "drzewa") { var own = TreeTex("liscie.png", false); if (own) tex = own; }
        var m = new Material(sh) { name = "Osiedle: liście " + key, enableInstancing = true, mainTexture = tex };
        m.SetColor("_Leaf", leaf); m.SetColor("_Leaf2", leaf2);
        m.SetFloat("_MeanGamma", meanGamma); m.SetFloat("_MeanLinear", meanLinear); m.SetFloat("_Recolor", recolor);
        m.SetVector("_Center", meshBounds.center + new Vector3(0, meshBounds.extents.y * .15f, 0));
        m.SetVector("_Radius", meshBounds.extents);
        if (src && src.HasProperty("alphaCutoff")) m.SetFloat("_Cutoff", Mathf.Clamp(src.GetFloat("alphaCutoff"), .3f, .6f));
        AssetDatabase.CreateAsset(m, Out + "/" + Safe("liscie " + key) + ".mat"); mats[key] = m; tile[m] = 1f; return m;
    }
    // Tekstury drzewa wyjęte z GLB przy pobieraniu (fetch_osiedle.py) — importowane skompresowane (glTFast trzyma je bez kompresji).
    static Texture TreeTex(string file, bool normal)
    {
        string p = Mdl + "drzewo_verdant/" + file;
        var imp = AssetImporter.GetAtPath(p) as TextureImporter; if (!imp) return null;
        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default; imp.sRGBTexture = !normal;
        imp.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = !normal;
        imp.mipmapEnabled = true; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    // Pień: zwykły Standard z teksturą kory (instancing — setki drzew to kilka wywołań rysowania).
    static Material TrunkMat(Material src)
    {
        if (mats.TryGetValue("pień", out var have)) return have;
        var tex = TreeTex("kora.png", false) ?? BaseTex(src); if (!tex) return null;
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: kora drzew", enableInstancing = true, mainTexture = tex, color = new Color(.92f, .88f, .84f) };
        var n = TreeTex("kora_n.png", true) ?? SrcTex(src, "normalTexture", "_BumpMap", "_NormalMap");
        if (n) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", .8f); }
        m.SetFloat("_Glossiness", .08f);
        AssetDatabase.CreateAsset(m, Out + "/kora.mat"); mats["pień"] = m; tile[m] = 1f; return m;
    }
    // Źdźbła trawy i kwiaty (kępy z dwóch skrzyżowanych kart): wycięcie alfą, wiatr, zanik z odległością.
    static Material ClumpMat(string key, string png, float wind, float macroMix, Color tint, float fadeStart, float fadeEnd)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var sh = Shader.Find("Braxhorn/OsiedleKepki"); string path = "Assets/Source/Claude/Osiedle/detail/" + png;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!sh || !imp) { rep.notes.Add("kępy " + key + ": brak shadera lub " + path); mats[key] = null; return null; }
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048;
        imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var m = new Material(sh) { name = "Osiedle: " + key, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path), color = tint };
        m.SetFloat("_Wind", wind); m.SetFloat("_MacroMix", macroMix); m.SetFloat("_FadeStart", fadeStart); m.SetFloat("_FadeEnd", fadeEnd);
        if (macroMix > 0 && grassMat)
        {
            // kępy trawy liczą kolor dokładnie jak trawnik pod nimi (te same mapy i parametry)
            m.SetTexture("_GroundTex", grassMat.mainTexture); m.SetFloat("_Ground", 1f); m.SetColor("_GroundColor", grassMat.color);
            foreach (var p in new[] { "_Tile", "_MacroTile", "_MacroMix", "_DryMix", "_Stripe", "_StripeMix" }) if (grassMat.HasProperty(p)) m.SetFloat(p, grassMat.GetFloat(p));
            if (grassMat.HasProperty("_Dry")) m.SetColor("_Dry", grassMat.GetColor("_Dry"));
        }
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = 1f; return m;
    }
    static Material Blades => ClumpMat("trawa 3D źdźbła", "zdzbla.png", .035f, .8f, new Color(.80f, .90f, .74f), 16f, 30f);
    static Material Blooms => ClumpMat("kwiaty rabat", "kwiaty.png", .05f, 0f, Color.white, 34f, 60f);
    // Szyby domów bez wnętrza: pokój za szybą (shader Braxhorn/OsiedleOkno); awaryjnie ciemne szkło.
    static Material OknoMat(string key, float variant, float variants, float cellH, float floor0)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        var sh = Shader.Find("Braxhorn/OsiedleOkno"); const string path = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!sh || !imp) { rep.notes.Add("okna: brak shadera lub atlasu pokoi — ciemne szkło"); var g = Plain("szkło przyciemniane", new Color(.10f, .13f, .15f), .93f, .55f); mats[key] = g; return g; }
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var m = new Material(sh) { name = "Osiedle: " + key };
        m.SetTexture("_Rooms", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_Variant", variant); m.SetFloat("_Variants", variants); m.SetFloat("_CellH", cellH); m.SetFloat("_Floor0", floor0);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = 1f; rep.fakeRoomMaterials++; return m;
    }
    static Material Glass => OknoMat("okna z pokojami (domy)", 0, 5, 3.4f, 0f);
    static Material ShopGlass => OknoMat("witryny z wnętrzem sklepu", 5, 3, 4.2f, 0f);
    static Material FlatGlass => OknoMat("okna mieszkań nad sklepami", 0, 5, 3.4f, .8f);
    static Material Frame => Plain("aluminium antracyt", new Color(.07f, .075f, .08f), .45f, .6f);
    static Material Steel => Plain("stal czarna płotów", new Color(.06f, .065f, .07f), .35f, .5f);
    static Material GarageDoor => Plain("brama garażowa", new Color(.20f, .21f, .22f), .5f, .4f);
    static Material Paint => Plain("biała farba drogowa", new Color(.93f, .93f, .9f), .3f);
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
    {
        Quad(m, new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), Vector3.up);
        if (y < .2f && m != grassMat) Hard(x0, x1, z0, z1);
    }
    // Trójkąt (np. szczyt dachu dwuspadowego).
    static void Tri(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 n) => Quad(m, a, b, c, c, n);
    static Vector3 Normal(Vector3 a, Vector3 b, Vector3 c, Vector3 towards)
    {
        var n = Vector3.Cross(b - a, c - a).normalized; return Vector3.Dot(n, towards) < 0 ? -n : n;
    }

    // ---------------- zajęte miejsca na ziemi (ścieżki, budynki, płoty) ----------------
    // Na tej mapie sadzone są drzewa, krzewy, kwiaty i źdźbła trawy — nic nie wyrasta z chodnika ani ze ściany.
    const float HardCell = 8f;
    static readonly Dictionary<long, List<Vector4>> hardCells = new Dictionary<long, List<Vector4>>();
    static readonly List<Vector4> hardRound = new List<Vector4>();   // x, z, r0 (wewnętrzny), r1 (zewnętrzny)
    static long CellKey(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
    static void Hard(float x0, float x1, float z0, float z1)
    {
        if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        var r = new Vector4(x0, z0, x1, z1);
        for (int cx = Mathf.FloorToInt(x0 / HardCell); cx <= Mathf.FloorToInt(x1 / HardCell); cx++)
            for (int cz = Mathf.FloorToInt(z0 / HardCell); cz <= Mathf.FloorToInt(z1 / HardCell); cz++)
            {
                var k = CellKey(cx, cz);
                if (!hardCells.TryGetValue(k, out var l)) hardCells[k] = l = new List<Vector4>();
                l.Add(r);
            }
    }
    static void HardRound(Vector3 c, float r0, float r1) => hardRound.Add(new Vector4(c.x, c.z, r0, r1));
    static bool IsHard(float x, float z, float margin)
    {
        for (int cx = Mathf.FloorToInt((x - margin) / HardCell); cx <= Mathf.FloorToInt((x + margin) / HardCell); cx++)
            for (int cz = Mathf.FloorToInt((z - margin) / HardCell); cz <= Mathf.FloorToInt((z + margin) / HardCell); cz++)
                if (hardCells.TryGetValue(CellKey(cx, cz), out var l))
                    foreach (var r in l) if (x > r.x - margin && x < r.z + margin && z > r.y - margin && z < r.w + margin) return true;
        foreach (var c in hardRound)
        {
            float d = Mathf.Sqrt((x - c.x) * (x - c.x) + (z - c.y) * (z - c.y));
            if (d < c.w + margin && d > c.z - margin) return true;
        }
        return false;
    }

    // Prostopadłościan obrócony o yaw (stopnie) wokół pionu — ławki wokół fontanny, elementy pod kątem.
    static void OBox(Material m, Vector3 c, Vector3 s, float yaw, bool solid = false) => OBoxQ(m, c, s, Quaternion.Euler(0, yaw, 0), solid);
    static void OBoxQ(Material m, Vector3 c, Vector3 s, Quaternion q, bool solid = false)
    {
        var h = s * .5f;
        Vector3 P(float x, float y, float z) => c + q * new Vector3(x * h.x, y * h.y, z * h.z);
        Vector3 R = q * Vector3.right, F = q * Vector3.forward, Up = q * Vector3.up;
        Quad(m, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), Up);
        Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), -Up);
        Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), -F);
        Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), F);
        Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), -R);
        Quad(m, P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), R);
        if (solid)
        {
            var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false); g.transform.position = c; g.transform.rotation = q;
            g.AddComponent<BoxCollider>().size = s;
        }
        var ext = new Vector3(Mathf.Abs(R.x) * h.x + Mathf.Abs(Up.x) * h.y + Mathf.Abs(F.x) * h.z, Mathf.Abs(R.y) * h.x + Mathf.Abs(Up.y) * h.y + Mathf.Abs(F.y) * h.z, Mathf.Abs(R.z) * h.x + Mathf.Abs(Up.z) * h.y + Mathf.Abs(F.z) * h.z);
        if (c.y - ext.y < .3f) Hard(c.x - ext.x, c.x + ext.x, c.z - ext.z, c.z + ext.z);
    }

    // Prostopadłościan wyrównany do osi; bottom=false pomija spód (niewidoczny), solid = BoxCollider.
    static void Box(Material m, Vector3 c, Vector3 s, bool solid = true, bool bottom = false)
    {
        var h = s * .5f;
        if (c.y - h.y < .3f && m != grassMat) Hard(c.x - h.x, c.x + h.x, c.z - h.z, c.z + h.z);
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
        if (baseCenter.y < .1f && !inward) HardRound(baseCenter, 0, r);
    }
    static void Disc(Material m, Vector3 c, float r, int seg = 32)
    {
        var b = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        int ic = b.v.Count;
        void V(Vector3 p) { b.v.Add(p); b.n.Add(Vector3.up); b.t.Add(new Vector4(1, 0, 0, 1)); b.uv.Add(new Vector2(p.x / s, p.z / s)); }
        V(c);
        for (int k = 0; k <= seg; k++) { float a = k * Mathf.PI * 2 / seg; V(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r); }
        for (int k = 0; k < seg; k++) b.i.AddRange(new[] { ic, ic + k + 2, ic + k + 1 });
        if (c.y < .1f && m != grassMat) HardRound(c, 0, r);
    }
    // Wycinek pierścienia (rabata wokół placu z przerwami na aleje); kąty w stopniach.
    static void RingArc(Material m, Vector3 c, float r0, float r1, float a0, float a1, int seg)
    {
        for (int k = 0; k < seg; k++)
        {
            float t0 = Mathf.Lerp(a0, a1, k / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (k + 1) / (float)seg) * Mathf.Deg2Rad;
            Vector3 d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)), d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
            Quad(m, c + d0 * r0, c + d0 * r1, c + d1 * r1, c + d1 * r0, Vector3.up);
        }
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
    // ---------------- kępy: źdźbła trawy i kwiaty ----------------
    // Dwie skrzyżowane karty na kępę, siatki w kwadratach 12 x 12 m (znikają z odległością), bez cieni rzucanych.
    sealed class Clumps { public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> i = new List<int>(); }
    static readonly Dictionary<(int, int, Material), Clumps> clumps = new Dictionary<(int, int, Material), Clumps>();
    const float ClumpChunk = 12f;
    static void Clump(Material m, Vector3 p, float w, float h, float yaw, float u0, float u1, Vector3 lean)
    {
        if (!m) return;
        var key = (Mathf.FloorToInt(p.x / ClumpChunk), Mathf.FloorToInt(p.z / ClumpChunk), m);
        if (!clumps.TryGetValue(key, out var c)) clumps[key] = c = new Clumps();
        for (int q = 0; q < 2; q++)
        {
            float a = (yaw + q * 90f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (w * .5f);
            int k = c.v.Count; var up = Vector3.up * h + lean;
            c.v.Add(p - d); c.v.Add(p + d); c.v.Add(p + d + up); c.v.Add(p - d + up);
            c.uv.Add(new Vector2(u0, 0)); c.uv.Add(new Vector2(u1, 0)); c.uv.Add(new Vector2(u1, 1)); c.uv.Add(new Vector2(u0, 1));
            c.i.Add(k); c.i.Add(k + 1); c.i.Add(k + 2); c.i.Add(k); c.i.Add(k + 2); c.i.Add(k + 3);
        }
    }
    static int clumpMeshes;
    static void FlushClumps()
    {
        foreach (var kv in clumps)
        {
            var c = kv.Value; if (c.v.Count == 0) continue;
            var mesh = new Mesh { name = "Kępy " + kv.Key.Item3.name + " " + kv.Key.Item1 + "_" + kv.Key.Item2, indexFormat = c.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(c.v); mesh.SetUVs(0, c.uv); mesh.SetNormals(Enumerable.Repeat(Vector3.up, c.v.Count).ToList()); mesh.SetTriangles(c.i, 0);
            mesh.RecalculateBounds(); var bb = mesh.bounds; bb.Expand(new Vector3(.3f, .2f, .3f)); mesh.bounds = bb;
            AssetDatabase.CreateAsset(mesh, Out + "/kepy_" + (clumpMeshes++) + ".asset");
            var g = new GameObject(kv.Key.Item3.name); g.transform.SetParent(Group(kv.Key.Item3 == Blooms ? "Kwiaty rabat" : "Trawa 3D"), false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item3; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var lod = g.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(kv.Key.Item3 == Blooms ? .12f : .22f, new Renderer[] { r }) }); lod.RecalculateBounds();
            rep.meshes++; rep.triangles += c.i.Count / 3;
        }
        clumps.Clear();
    }
    // Trawa w 3D na wszystkich trawnikach parku i osiedla B: gęsta, niska (trawnik koszony), wyższa przy płotach, pniach i krawędziach.
    static void GrassBlades(float x0, float x1, float z0, float z1, float step)
    {
        var m = Blades; if (!m) return;
        var rnd = new System.Random(4242); int n = 0;
        for (float z = z0; z < z1; z += step)
            for (float x = x0; x < x1; x += step)
            {
                float px = x + (float)rnd.NextDouble() * step, pz = z + (float)rnd.NextDouble() * step;
                if (IsHard(px, pz, .12f)) continue;
                bool edge = IsHard(px, pz, .7f);
                float h = edge ? .20f + (float)rnd.NextDouble() * .16f : .09f + (float)rnd.NextDouble() * .08f;
                float w = edge ? .55f : .46f + (float)rnd.NextDouble() * .16f;
                float u = rnd.Next(4) * .25f;   // atlas: 4 odmiany kępy w jednej teksturze (bez powtarzania się wzoru)
                var lean = new Vector3((float)rnd.NextDouble() - .5f, 0, (float)rnd.NextDouble() - .5f) * h * .5f;
                Clump(m, new Vector3(px, 0, pz), w, h, rnd.Next(180), u, u + .25f, lean); n++;
            }
        rep.grassClumps += n;
    }
    // Rabata: ściółka z kory + gęste kępy kwiatów (atlas 4 gatunków: pelargonie, nagietki, stokrotki, lawenda).
    static void FlowerBed(float x0, float x1, float z0, float z1, int seed, params int[] kinds)
    {
        if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        BoxMinMax(Plinth, x0 - .08f, x1 + .08f, 0, .1f, z0 - .08f, z0, false); BoxMinMax(Plinth, x0 - .08f, x1 + .08f, 0, .1f, z1, z1 + .08f, false);
        BoxMinMax(Plinth, x0 - .08f, x0, 0, .1f, z0, z1, false); BoxMinMax(Plinth, x1, x1 + .08f, 0, .1f, z0, z1, false);
        Top(Mulch, x0, x1, z0, z1, .06f);
        FlowersIn(x0, x1, z0, z1, seed, kinds);
        rep.flowerBeds++;
    }
    static void FlowersIn(float x0, float x1, float z0, float z1, int seed, params int[] kinds)
    {
        var m = Blooms; if (!m) return;
        if (kinds.Length == 0) kinds = new[] { 0, 1, 2, 3 };
        var rnd = new System.Random(seed); const float st = .26f;
        for (float z = z0 + .1f; z < z1 - .1f; z += st)
            for (float x = x0 + .1f; x < x1 - .1f; x += st)
            {
                // gatunki pasami (jak w prawdziwej rabacie), nie losowa sieczka
                int band = Mathf.FloorToInt(((x - x0) + (z - z0) * .35f) / 1.6f);
                int kind = kinds[Mathf.Abs(band) % kinds.Length];
                float px = x + ((float)rnd.NextDouble() - .5f) * st, pz = z + ((float)rnd.NextDouble() - .5f) * st;
                float h = (kind == 3 ? .42f : .30f) + (float)rnd.NextDouble() * .14f;
                Clump(m, new Vector3(px, .05f, pz), .36f + (float)rnd.NextDouble() * .12f, h, rnd.Next(180), kind * .25f, kind * .25f + .25f, Vector3.zero);
                rep.flowerClumps++;
            }
    }
    static void FlowerRing(Vector3 c, float r0, float r1, float a0, float a1, int seed, params int[] kinds)
    {
        RingArc(Mulch, c + Vector3.up * .06f, r0, r1, a0, a1, Mathf.Max(4, Mathf.RoundToInt((a1 - a0) / 6f)));
        HardRound(c, r0, r1);
        var m = Blooms; if (!m) return;
        var rnd = new System.Random(seed);
        float rm = (r0 + r1) * .5f; int n = Mathf.RoundToInt((a1 - a0) * Mathf.Deg2Rad * rm / .24f);
        for (int k = 0; k < n; k++)
            for (float r = r0 + .15f; r < r1 - .1f; r += .26f)
            {
                float a = Mathf.Lerp(a0, a1, (k + (float)rnd.NextDouble()) / n) * Mathf.Deg2Rad;
                int kind = kinds[(k / 7) % kinds.Length];
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r + ((float)rnd.NextDouble() - .5f) * .15f);
                Clump(m, p + Vector3.up * .05f, .4f, (kind == 3 ? .42f : .30f) + (float)rnd.NextDouble() * .14f, rnd.Next(180), kind * .25f, kind * .25f + .25f, Vector3.zero);
                rep.flowerClumps++;
            }
        rep.flowerBeds++;
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
        if (solid) { b = RB(o); Collider(b.center, b.size); Hard(b.min.x, b.max.x, b.min.z, b.max.z); }
        return o;
    }
    // Model z wybranymi częściami (np. jedna kompletna ławka z modułowego zestawu Poly Haven).
    static GameObject PutParts(GameObject src, string[] drop, Vector3 at, float yaw, float cull, bool solid)
    {
        if (!src) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false);
        o.name = src.name; o.transform.position = Vector3.zero;
        foreach (var t in o.GetComponentsInChildren<Transform>(true).Where(q => q != o.transform && drop.Any(d => q.name.StartsWith(d))).ToList())
            if (t) UnityEngine.Object.DestroyImmediate(t.gameObject);
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
        var rs = o.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) { UnityEngine.Object.DestroyImmediate(o); return null; }
        var b = RB(o);
        o.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        foreach (var r in rs) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
        var lod = o.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs) }); lod.RecalculateBounds();
        if (solid)
        {
            b = RB(o);
            var g = new GameObject("kolizja"); g.transform.SetParent(colliders, false); g.transform.position = b.center; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            g.AddComponent<BoxCollider>().size = new Vector3(2.3f, b.size.y, .7f);
            Hard(b.min.x, b.max.x, b.min.z, b.max.z);
        }
        return o;
    }
    // Ławka parkowa Poly Haven (modular_street_seating): jedna kompletna ławka 2.3 m z podłokietnikami;
    // yaw = kierunek, w który patrzy siedzący. Bez modelu — ławka z listew (awaryjnie).
    static readonly string[] BenchDrop = { "suspended_support_02", "connector", "seat_bench" };   // jedna ławka 2.3 m, bez drugiego modułu i łączników
    static void ParkBench(Vector3 at, float yaw)
    {
        if (bench && PutParts(bench, BenchDrop, at, yaw, .05f, true)) { rep.benches++; return; }
        Bench(at, yaw);
    }
    // Latarnia parkowa Poly Haven (street_lamp_01): klosz matowy, żarówka bez emisji (nic nie świeci w dzień).
    static void ParkLamp(Vector3 at, float yaw)
    {
        var o = parkLamp ? Put(parkLamp, at, yaw, .06f, false) : null;
        if (!o) { Lamp(at, yaw); return; }
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            for (int k = 0; k < ms.Length; k++)
            {
                if (!ms[k]) continue; var n = ms[k].name.ToLowerInvariant();
                if (n.Contains("bulb")) ms[k] = Paint; else if (n.Contains("glass")) ms[k] = LampGlass;
            }
            r.sharedMaterials = ms;
        }
        Collider(at + Vector3.up * 1.9f, new Vector3(.3f, 3.8f, .3f)); Hard(at.x - .3f, at.x + .3f, at.z - .3f, at.z + .3f);
        rep.lamps++;
    }
    static Material LampGlass => Plain("klosz latarni mleczny", new Color(.86f, .87f, .86f), .7f);

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
        Renderer pien = R("pien"), l0 = R("liscie_0"), l1 = R("liscie_1"), l2 = R("liscie_2"), l3 = R("liscie_3");
        if (!pien || !l0) { UnityEngine.Object.DestroyImmediate(t); rep.notes.Add("drzewo_verdant: brak węzłów pien/liscie"); treeSrc = null; return null; }
        var all = new[] { pien, l0, l1, l2, l3 }.Where(r => r).ToArray();
        var b = pien.bounds; foreach (var r in all) b.Encapsulate(r.bounds);
        var pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (Transform c in t.transform) c.position -= pivot;
        // zielone korony zamiast czarnych: shader liści + zwykła kora z instancingiem
        var leafMesh = l0.GetComponent<MeshFilter>() ? l0.GetComponent<MeshFilter>().sharedMesh : null;
        var leaf = leafMesh ? LeafMat("drzewa", l0.sharedMaterial, leafMesh.bounds, .1182f, .0158f, .85f, new Color(.30f, .46f, .15f), new Color(.40f, .49f, .17f)) : null;
        if (leaf) foreach (var r in new[] { l0, l1, l2, l3 }) if (r) r.sharedMaterial = leaf;
        var bark = TrunkMat(pien.sharedMaterial); if (bark) pien.sharedMaterial = bark;
        // każde drzewo trochę inne: wysokość, szerokość korony, obrót
        float spread = 1f + (float)(treeRnd.NextDouble() - .5) * .26f;
        float s = height / Mathf.Max(.5f, b.size.y);
        t.transform.localScale = new Vector3(s * spread, s, s * spread * (1f + (float)(treeRnd.NextDouble() - .5) * .12f));
        t.transform.rotation = Quaternion.Euler(0, yaw, 0) * t.transform.rotation;
        t.transform.position = at;
        // cienie rzucają bliskie poziomy (do ok. 40 m); dalsze korony bez cienia — budżet trójkątów na telefon
        foreach (var r in all) { r.shadowCastingMode = r == l2 || r == l3 ? ShadowCastingMode.Off : ShadowCastingMode.On; r.receiveShadows = true; }
        var lod = t.AddComponent<LODGroup>();
        var lods = new List<LOD> { new LOD(.5f, new[] { pien, l0 }) };
        if (l1) lods.Add(new LOD(.25f, new[] { pien, l1 }));
        if (l2) lods.Add(new LOD(.16f, new[] { pien, l2 }));
        if (l3) lods.Add(new LOD(.012f, new[] { pien, l3 }));
        lod.SetLODs(lods.ToArray());
        lod.RecalculateBounds();
        treeSpots.Add(new Vector3(at.x, height, at.z));
        GameObject trunkGo = null;
        if (trunkCollider)
        {
            trunkGo = new GameObject("pień kolizja"); trunkGo.transform.SetParent(colliders, false); trunkGo.transform.position = at + Vector3.up * 1.4f;
            var cap = trunkGo.AddComponent<CapsuleCollider>(); cap.radius = .3f; cap.height = 2.8f;
        }
        trees.Add(new TreeRec { go = t, trunk = trunkGo, leaves = l0, at = at, height = height, group = group });
        return t;
    }
    static readonly System.Random treeRnd = new System.Random(909);
    sealed class TreeRec { public GameObject go, trunk; public Renderer leaves; public Vector3 at; public float height; public string group; }
    static readonly List<TreeRec> trees = new List<TreeRec>();
    static readonly List<Bounds> roofBounds = new List<Bounds>();     // dachy pochyłe (bez kolizji) — do sprawdzania koron
    // Korona nie może wchodzić w budynek: po zbudowaniu wszystkiego sprawdzamy każdą koronę (elipsoida z granic liści,
    // 25 punktów na 85% promieni) z kolizjami sceny (ściany, dachy płaskie, latarnie; bez pni drzew) i z bryłami dachów.
    // Kolizja → drzewo zmniejszane (×0.9, ×0.8, ×0.7, nie niżej niż 4.5 m), a jeśli dalej koliduje — usuwane.
    static int CrownHits(TreeRec tr, Collider[] buf)
    {
        var b = tr.leaves ? tr.leaves.bounds : new Bounds(tr.at + Vector3.up * tr.height * .66f, new Vector3(tr.height * 1.3f, tr.height * .68f, tr.height * 1.1f));
        Vector3 c = b.center, e = b.extents * .85f; int hits = 0;
        bool Bad(Vector3 p)
        {
            foreach (var rb in roofBounds) { var x = rb; x.Expand(.4f); if (x.Contains(p)) return true; }
            int n = Physics.OverlapSphereNonAlloc(p, .2f, buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var col = buf[i]; if (!col || col is CapsuleCollider || col.gameObject.name == "pień kolizja") continue; if (tr.go && col.transform.IsChildOf(tr.go.transform)) continue; return true; }
            return false;
        }
        if (Bad(c + Vector3.up * e.y)) hits++;
        foreach (float el in new[] { -30f, 0f, 35f })
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f, ce = Mathf.Cos(el * Mathf.Deg2Rad), se = Mathf.Sin(el * Mathf.Deg2Rad);
                if (Bad(c + new Vector3(Mathf.Cos(a) * e.x * ce, se * e.y, Mathf.Sin(a) * e.z * ce))) hits++;
            }
        return hits;
    }
    static void PruneCrowns()
    {
        Physics.SyncTransforms();
        var buf = new Collider[32]; int ok = 0, shrunk = 0, removed = 0; var where = new Dictionary<string, int>();
        foreach (var tr in trees)
        {
            if (!tr.go) continue;
            if (CrownHits(tr, buf) == 0) { ok++; continue; }
            bool fixedIt = false;
            var s0 = tr.go.transform.localScale;
            foreach (float f in new[] { .9f, .8f, .7f })
            {
                if (tr.height * f < 4.5f) break;
                tr.go.transform.localScale = s0 * f; Physics.SyncTransforms();
                if (CrownHits(tr, buf) == 0) { fixedIt = true; tr.height *= f; break; }
            }
            if (fixedIt) { shrunk++; continue; }
            UnityEngine.Object.DestroyImmediate(tr.go); if (tr.trunk) UnityEngine.Object.DestroyImmediate(tr.trunk);
            treeSpots.RemoveAll(q => Mathf.Abs(q.x - tr.at.x) < .01f && Mathf.Abs(q.z - tr.at.z) < .01f);
            removed++; where[tr.group ?? "?"] = (where.TryGetValue(tr.group ?? "?", out var w) ? w : 0) + 1;
        }
        rep.notes.Add("korony vs budynki: " + ok + " wolnych, " + shrunk + " zmniejszonych, " + removed + " usuniętych"
                      + (where.Count > 0 ? " (" + string.Join(", ", where.Select(kv => kv.Key + ": " + kv.Value)) + ")" : ""));
    }
    static readonly List<Vector3> treeSpots = new List<Vector3>();   // x, wysokość, z — żeby nie sadzić drzewa w drzewie
    static bool NearTree(float x, float z, float d) => treeSpots.Any(t => (t.x - x) * (t.x - x) + (t.z - z) * (t.z - z) < d * d);
    // Krzew z liści drzewa (te same karty, korona w skali krzewu): gęsta kopuła 1–1.6 m, ta sama świeża zieleń.
    static Mesh bushMesh; static bool bushTried;
    static GameObject Bush(Vector3 at, float width, float yaw)
    {
        if (!bushTried)
        {
            bushTried = true;
            if (!treeTried) { treeTried = true; treeSrc = Model("drzewo_verdant"); }
            var f = treeSrc ? treeSrc.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(q => q.name == "liscie_3") : null;
            var f0 = treeSrc ? treeSrc.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(q => q.name == "liscie_0") : null;
            if (f && f0) { bushMesh = f.sharedMesh; LeafMat("drzewa", f0.GetComponent<MeshRenderer>().sharedMaterial, f0.sharedMesh.bounds, .1182f, .0158f, .85f, new Color(.30f, .46f, .15f), new Color(.40f, .49f, .17f)); }
            else rep.notes.Add("krzewy: brak liscie_3 w drzewo_verdant");
        }
        mats.TryGetValue("drzewa", out var leaf);
        if (!bushMesh || !leaf) return null;
        var p = new GameObject("Krzew (liście)"); p.transform.SetParent(Group(group), false);
        var o = new GameObject("krzew"); o.transform.SetParent(p.transform, false);
        o.AddComponent<MeshFilter>().sharedMesh = bushMesh;
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = leaf; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
        var mb = bushMesh.bounds; float s = width / Mathf.Max(.5f, Mathf.Max(mb.size.x, mb.size.z));
        o.transform.localScale = new Vector3(s, s * (1.05f + (float)treeRnd.NextDouble() * .35f), s);
        o.transform.localPosition = -Vector3.Scale(new Vector3(mb.center.x, mb.min.y, mb.center.z), o.transform.localScale) - Vector3.up * .05f;
        p.transform.rotation = Quaternion.Euler(0, yaw, 0); p.transform.position = at;
        var lod = p.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.035f, new Renderer[] { r }) }); lod.RecalculateBounds();
        rep.bushes++;
        return p;
    }
    static GameObject Shrub(Vector3 at, float scale, float yaw)
    {
        if (!verdant.TryGetValue("EstateMesh_467", out var src)) return null;
        var p = new GameObject("Krzew (Verdant)"); p.transform.SetParent(Group(group), false);
        var o = new GameObject("krzew"); o.transform.SetParent(p.transform, false);
        o.transform.localPosition = src.transform.position; o.transform.localRotation = src.transform.rotation; o.transform.localScale = src.transform.lossyScale;
        var sm = src.GetComponent<MeshFilter>().sharedMesh;
        o.AddComponent<MeshFilter>().sharedMesh = sm;
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterials = src.sharedMaterials; r.shadowCastingMode = ShadowCastingMode.On;
        // wierzba: liście przemalowane tylko tam, gdzie tekstura jest zielona (kora zostaje brązowa)
        var wl = LeafMat("krzew wierzba", src.sharedMaterial, sm.bounds, .345f, .1146f, 0f, new Color(.36f, .47f, .22f), new Color(.42f, .50f, .24f));
        if (wl) r.sharedMaterial = wl;
        var b = r.bounds; var pivot = new Vector3(b.center.x, b.min.y, b.center.z);
        o.transform.localPosition -= pivot;
        p.transform.localScale = Vector3.one * scale; p.transform.rotation = Quaternion.Euler(0, yaw, 0); p.transform.position = at;
        var lod = p.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.02f, new Renderer[] { r }) }); lod.RecalculateBounds();
        rep.shrubs++;
        return p;
    }

    // Nowoczesna latarnia (słup + płaska oprawa), bez świecenia — tylko dzień.
    // Latarnia: stopa, okrągły słup zwężający się ku górze, wysięgnik i płaska oprawa LED (bez świecenia — dzień).
    static void Lamp(Vector3 at, float yaw)
    {
        var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        Cylinder(Frame, at, .17f, .55f, 14, true, false);
        Cylinder(Frame, at + Vector3.up * .55f, .11f, .08f, 14, true, false);
        Cylinder(Frame, at + Vector3.up * .63f, .085f, 2.4f, 12, false, false);
        Cylinder(Frame, at + Vector3.up * 3.03f, .07f, 1.75f, 12, true, false);
        OBox(Frame, at + Vector3.up * 4.74f + dir * .5f, new Vector3(.07f, .07f, 1.0f), yaw);
        OBox(Frame, at + Vector3.up * 4.7f + dir * 1.02f, new Vector3(.30f, .1f, .62f), yaw);
        OBox(Paint, at + Vector3.up * 4.645f + dir * 1.02f, new Vector3(.22f, .012f, .5f), yaw);
        Collider(at + Vector3.up * 2.4f, new Vector3(.3f, 4.8f, .3f));
        rep.lamps++;
    }
    // Ławka parkowa: dwie stalowe ramy boczne, siedzisko i oparcie z drewnianych listew. yaw = kierunek, w który patrzy siedzący.
    static void Bench(Vector3 at, float yaw)
    {
        var q = Quaternion.Euler(0, yaw, 0); Vector3 F = q * Vector3.forward, R = q * Vector3.right;
        Vector3 P(float x, float y, float z) => at + R * x + Vector3.up * y + F * z;
        foreach (float x in new[] { -.82f, .82f })
        {
            OBox(Frame, P(x, .22f, .14f), new Vector3(.06f, .44f, .06f), yaw);            // noga przednia
            OBox(Frame, P(x, .38f, -.2f), new Vector3(.06f, .76f, .06f), yaw);            // noga tylna i wspornik oparcia
            OBox(Frame, P(x, .43f, -.03f), new Vector3(.06f, .05f, .48f), yaw);           // wspornik siedziska
            OBox(Frame, P(x, .62f, .02f), new Vector3(.05f, .04f, .42f), yaw);            // podłokietnik
        }
        for (int k = 0; k < 5; k++) OBox(Wood, P(0, .47f, .17f - k * .085f), new Vector3(1.9f, .035f, .07f), yaw);
        for (int k = 0; k < 3; k++) OBox(Wood, P(0, .58f + k * .1f, -.24f - k * .018f), new Vector3(1.9f, .075f, .03f), yaw);
        var g = new GameObject("ławka kolizja"); g.transform.SetParent(colliders, false); g.transform.position = P(0, .4f, -.03f); g.transform.rotation = q;
        g.AddComponent<BoxCollider>().size = new Vector3(1.9f, .8f, .55f);
        rep.benches++;
    }
    // Kosz: grafitowy walec z drewnianymi listwami i daszkiem.
    static void Bin(Vector3 at)
    {
        Cylinder(Frame, at, .24f, .82f, 14, false, true);
        for (int k = 0; k < 10; k++)
        {
            float a = k * 36f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
            OBox(Wood, at + d * .25f + Vector3.up * .44f, new Vector3(.09f, .62f, .025f), a);
        }
        Cylinder(Frame, at + Vector3.up * .82f, .27f, .06f, 14, true, false);
        rep.bins++;
    }
    // Donica betonowa z kwiatami (zamiast pustych skrzynek).
    static void Planter(Vector3 at, float w, float d, int seed, params int[] kinds)
    {
        BoxMinMax(Concrete, at.x - w * .5f, at.x + w * .5f, at.y, at.y + .55f, at.z - d * .5f, at.z + d * .5f, true);
        Top(Mulch, at.x - w * .5f + .08f, at.x + w * .5f - .08f, at.z - d * .5f + .08f, at.z + d * .5f - .08f, at.y + .56f);
        var m = Blooms; if (!m) return;
        var rnd = new System.Random(seed);
        for (float x = at.x - w * .5f + .2f; x < at.x + w * .5f - .15f; x += .24f)
            for (float z = at.z - d * .5f + .2f; z < at.z + d * .5f - .15f; z += .24f)
            {
                int kind = kinds.Length > 0 ? kinds[rnd.Next(kinds.Length)] : rnd.Next(4);
                Clump(m, new Vector3(x, at.y + .56f, z), .38f, .32f + (float)rnd.NextDouble() * .12f, rnd.Next(180), kind * .25f, kind * .25f + .25f, Vector3.zero);
                rep.flowerClumps++;
            }
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
    // junctionsNorth = osie ulic dochodzących z północy: krawężnik przerwany na szerokość jezdni, chodnik dochodzi
    // do krawężnika poprzecznej ulicy, a narożnik (z pasami zieleni) wyłożony płytami — chodniki się łączą.
    static void StreetX(float x0, float x1, float zc, params float[] junctionsNorth)
    {
        Top(Asphalt, x0, x1, zc - Carriage, zc + Carriage, .03f);
        // środkowa linia przerywana
        for (float x = x0 + 1.5f; x + 3f < x1; x += 6f) Top(Paint, x, x + 3f, zc - .06f, zc + .06f, .046f);
        List<(float, float)> Runs(float gapHalf, int side)
        {
            var runs = new List<(float, float)>(); float cur = x0;
            if (side > 0) foreach (var j in junctionsNorth.OrderBy(q => q)) { if (j - gapHalf > cur) runs.Add((cur, j - gapHalf)); cur = Mathf.Max(cur, j + gapHalf); }
            if (cur < x1) runs.Add((cur, x1));
            return runs;
        }
        foreach (int side in new[] { -1, 1 })
        {
            float kz = zc + side * (Carriage + Kerb * .5f), wz = zc + side * (Carriage + Kerb + Green + Walk * .5f);
            foreach (var (a, b) in Runs(Carriage, side)) BoxMinMax(KerbStone, a, b, 0, .16f, kz - Kerb * .5f, kz + Kerb * .5f);
            foreach (var (a, b) in Runs(Carriage + Kerb, side))
            {
                BoxMinMax(Pavers, a, b, 0, .14f, wz - Walk * .5f, wz + Walk * .5f);
                for (float x = a + 10f; x < b - 4f; x += 22f) Lamp(new Vector3(x, 0, zc + side * (Carriage + Kerb + Green * .5f)), side > 0 ? 180 : 0);
            }
        }
        foreach (var j in junctionsNorth)
            foreach (int s in new[] { -1, 1 })
                BoxMinMax(Pavers, Mathf.Min(j + s * (Carriage + Kerb), j + s * Half), Mathf.Max(j + s * (Carriage + Kerb), j + s * Half), 0, .14f, zc + Carriage + Kerb, zc + Half);
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
    static GameObject outdoorSet, bench, picnic, parkLamp; static int binTurn;
    static GameObject[] tufts = new GameObject[0], flowers = new GameObject[0]; static System.Random tuftRnd = new System.Random(77);
    // Rośliny Poly Haven są zestawami wariantów ustawionych obok siebie (np. 17 kępek w jednym pliku):
    // stawiamy JEDEN wariant (węzeł) na raz. Ciężkie warianty (> 4000 trójkątów) i duże krzaczki (> 1 m) pomijamy.
    static List<Transform> grassNodes = new List<Transform>(), flowerNodes = new List<Transform>();
    static List<List<Transform>> flowerSpecies = new List<List<Transform>>();
    static List<Transform> PlantNodes(GameObject model) => PlantNodes(model, 0f);
    static List<Transform> PlantNodes(GameObject model, float minSize)
    {
        var l = new List<Transform>(); if (!model) return l;
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var n = mf.transform; if (!mf.sharedMesh) continue;
            if (n.name.Contains("_LOD") && !n.name.Contains("_LOD0")) continue;
            var ls = mf.transform.lossyScale; var sz = Vector3.Scale(mf.sharedMesh.bounds.size, new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z)));   // rozmiar w metrach (węzły mają skalę, np. gazania 0.14–0.22)
            long tri = 0; for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) tri += mf.sharedMesh.GetIndexCount(sm) / 3;
            if (tri > 4000 || Mathf.Max(sz.x, sz.y, sz.z) > 1.0f || Mathf.Max(sz.x, sz.y, sz.z) < minSize) continue;
            l.Add(n);
        }
        return l;
    }
    // Materiał roślin: wycięcie alfą, obie strony, normalna częściowo w górę (jak trawnik), bez przemalowania tekstury.
    static readonly Dictionary<Material, Material> plantMats = new Dictionary<Material, Material>();
    static Material PlantMat(Material src)
    {
        if (!src) return src;
        if (plantMats.TryGetValue(src, out var have)) return have;
        var sh = Shader.Find("Braxhorn/OsiedleLiscie"); var tex = BaseTex(src);
        if (!sh || !tex) { plantMats[src] = src; return src; }
        var m = new Material(sh) { name = "Osiedle: roślina " + src.name, enableInstancing = true, mainTexture = tex };
        m.SetFloat("_NoRecolor", 1f); m.SetFloat("_Gain", 1.05f); m.SetFloat("_Sphere", 0f); m.SetFloat("_Up", .55f); m.SetFloat("_InnerDark", 0f);
        m.SetFloat("_Cutoff", src.HasProperty("alphaCutoff") ? Mathf.Clamp(src.GetFloat("alphaCutoff"), .2f, .6f) : .3f);
        m.SetFloat("_Wind", .006f); m.SetFloat("_Wrap", .55f); m.SetFloat("_Trans", .25f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe("roslina " + src.name) + ".mat"); plantMats[src] = m; mats["roślina " + src.name] = m; return m;
    }
    static GameObject PlantPart(Transform node, Vector3 at, float scale, float yaw, float cull)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(node.gameObject, Group(group), false);
        o.name = node.name;
        o.transform.localScale = node.lossyScale * scale;
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * node.rotation;
        o.transform.position = at + Vector3.down * .03f;                      // wtopione 3 cm w ziemię
        foreach (var r in o.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(PlantMat).ToArray();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        }
        var rs = o.GetComponentsInChildren<Renderer>(true);
        var lod = o.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs) }); lod.RecalculateBounds();
        return o;
    }
    // Kępa trawy (lub kwiat): jeden wariant, skala 0.9–1.3, losowy obrót, bez kolizji, znika z daleka (~10 m).
    static void Tuft(Vector3 at, bool flower = false, float scale = 1f)
    {
        var set = flower ? flowerNodes : grassNodes; if (set.Count == 0) return;
        float s = scale * (.9f + (float)tuftRnd.NextDouble() * .4f);
        PlantPart(set[tuftRnd.Next(set.Count)], at, s, tuftRnd.Next(360), flower ? .02f : .03f);
        if (flower) rep.flowerPlants++; else rep.tufts++;
    }
    static void FlowerPatch(Vector3 c, float radius, int n)
    {
        if (flowerSpecies.Count == 0) return;
        var sp = flowerSpecies[tuftRnd.Next(flowerSpecies.Count)];
        for (int k = 0; k < n; k++)
        {
            float a = (float)tuftRnd.NextDouble() * Mathf.PI * 2, d = Mathf.Sqrt((float)tuftRnd.NextDouble()) * radius;
            var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * d;
            if (IsHard(p.x, p.z, .1f)) continue;
            PlantPart(sp[tuftRnd.Next(sp.Count)], p, .9f + (float)tuftRnd.NextDouble() * .4f, tuftRnd.Next(360), .02f); rep.flowerPlants++;
        }
        rep.flowerPatches++;
    }
    // Trawnik parku i osiedla B: fotoskan na ziemi + kępy tam, gdzie trawa naturalnie jest wyższa
    // (przy ścieżkach, płotach, murkach i u stóp drzew), na otwartym trawniku rzadko (~1 na 5 m²), między nimi plamy kwiatów.
    static void LawnPlants(float x0, float x1, float z0, float z1)
    {
        if (grassNodes.Count == 0) { rep.notes.Add("kępy: brak modeli trawy"); return; }
        group = "Trawnik: kępy i kwiaty";
        var rnd = new System.Random(5150);
        float st = 2.24f;
        for (float z = z0; z < z1; z += st)
            for (float x = x0; x < x1; x += st)
            {
                float px = x + (float)rnd.NextDouble() * st, pz = z + (float)rnd.NextDouble() * st;
                if (!IsHard(px, pz, .35f)) Tuft(new Vector3(px, 0, pz));
            }
        for (float z = z0; z < z1; z += .9f)
            for (float x = x0; x < x1; x += .9f)
            {
                float px = x + (float)rnd.NextDouble() * .9f, pz = z + (float)rnd.NextDouble() * .9f;
                if (!IsHard(px, pz, .6f) || IsHard(px, pz, .16f) || rnd.NextDouble() > .3) continue;
                int n = 2 + rnd.Next(2);
                for (int k = 0; k < n; k++)
                {
                    float qx = px + ((float)rnd.NextDouble() - .5f) * .7f, qz = pz + ((float)rnd.NextDouble() - .5f) * .7f;
                    if (!IsHard(qx, qz, .12f)) Tuft(new Vector3(qx, 0, qz), false, 1.1f);
                }
            }
        foreach (var t in treeSpots.Where(t => t.x > x0 && t.x < x1 && t.z > z0 && t.z < z1).ToList())
        {
            int n = 3 + rnd.Next(4);
            for (int k = 0; k < n; k++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2, d = .45f + (float)rnd.NextDouble() * 1.2f;
                float qx = t.x + Mathf.Cos(a) * d, qz = t.z + Mathf.Sin(a) * d;
                if (!IsHard(qx, qz, .15f)) Tuft(new Vector3(qx, 0, qz), false, 1.15f);
            }
        }
        int patches = Mathf.RoundToInt((x1 - x0) * (z1 - z0) / 140f);
        for (int k = 0, tries = 0; k < patches && tries < patches * 6; tries++)
        {
            float px = x0 + (float)rnd.NextDouble() * (x1 - x0), pz = z0 + (float)rnd.NextDouble() * (z1 - z0);
            if (IsHard(px, pz, 1.2f)) continue;
            FlowerPatch(new Vector3(px, 0, pz), .9f, 5 + rnd.Next(6)); k++;
        }
        rep.notes.Add("trawnik: " + rep.tufts + " kęp, " + rep.flowerPatches + " plam kwiatów");
    }
    static void TuftLine(Vector3 a, Vector3 b, float step, float side)
    {
        var d = b - a; float len = d.magnitude; if (len < .5f) return; d /= len; var n = new Vector3(-d.z, 0, d.x) * side;
        for (float t = step * .5f; t < len; t += step) Tuft(a + d * t + n * (.35f + (float)tuftRnd.NextDouble() * .3f), false, .9f);
    }
    // swap = działka „bokiem” (ulica wzdłuż Z, głębokość wzdłuż X) — tak stoją domy osiedla A.
    // Trzy style (index % 3):
    //   0 — parter biały tynk, piętro drewno, płaski dach z wysuniętą płytą (okap 55 cm, grafit);
    //   1 — parter kamień, piętro biały tynk, dach dwuspadowy z łupka („nowoczesna stodoła”, szczyt do ulicy);
    //   2 — parter klinkier, piętro tynk piaskowy, dach kopertowy z dachówki ceramicznej, komin.
    // Wszystkie: okna z ramami, parapety, drzwi, rynny i rury spustowe, cokół, okna w bocznych ścianach.
    static void Villa(int index, float u0, float u1, float front, float dir, bool swap = false, float depthOverride = 0f,
                      bool garageOn = true, bool fencesOn = true, string label = null, bool forSale = false)
    {
        group = label ?? (forSale ? "Osiedle B / willa na sprzedaż " : "Osiedle B / willa ") + (index + 1);
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
        Vector3 Aax = (Pw(1, 0, 0) - Pw(0, 0, 0)).normalized, Wax = (Pw(0, 0, 1) - Pw(0, 0, 0)).normalized;

        float Wm = garageOn ? Mathf.Min(W - 13f, 18f) : Mathf.Min(W - 6f, 16f), garage = garageOn ? 6.5f : 0f, s0 = (W - (Wm + garage)) * .5f;
        float h0 = s0, h1 = s0 + Wm, g0 = h1, g1 = h1 + garage;     // dom i garaż wzdłuż ulicy
        float f0 = 7f, f1 = 18f;                                      // front i tył parteru (głębokość)
        var wallG = variant == 0 ? Stucco : variant == 1 ? Stone : Clinker;
        var wallU = variant == 0 ? Wood : variant == 1 ? Stucco : Sand;

        // parter + cokół (willa na sprzedaż: ściany z otworami niżej)
        if (!forSale) BoxL(wallG, h0, h1, 0, 3.4f, f0, f1);
        if (!forSale) BoxL(Plinth, h0 - .04f, h1 + .04f, 0, .35f, f0 - .04f, f1 + .04f, false);   // willa na sprzedaż: cokół obwodowy niżej (pełny blok zakryłby parkiet)
        // stropodach parteru: attyka z obróbką + żwir
        BoxL(wallG, h0, h1, 3.4f, 3.75f, f0, f0 + .25f, false); BoxL(wallG, h0, h1, 3.4f, 3.75f, f1 - .25f, f1, false);
        BoxL(wallG, h0, h0 + .25f, 3.4f, 3.75f, f0, f1, false); BoxL(wallG, h1 - .25f, h1, 3.4f, 3.75f, f0, f1, false);
        BoxL(Frame, h0 - .03f, h1 + .03f, 3.75f, 3.8f, f0 - .03f, f1 + .03f, false);
        BoxL(Gravel, h0 + .25f, h1 - .25f, 3.4f, 3.46f, f0 + .25f, f1 - .25f, false);
        // piętro przesunięte i wysunięte nad wejście
        float uw = Wm * .62f, uA = h0, uB = uA + uw, e0 = 5.4f, e1 = 14.6f, am = (uA + uB) * .5f;
        BoxL(wallU, uA, uB, 3.46f, 6.7f, e0, e1, true, true);
        BoxL(Plinth, uA - .03f, uB + .03f, 3.46f, 3.62f, e0 - .03f, e1 + .03f, false);   // gzyms między kondygnacjami
        // taras na dachu parteru: szklana balustrada ze stalowym pochwytem tam, gdzie nie ma piętra
        void Rail(float a0, float a1, float w0, float w1)
        {
            BoxL(GlassClear, a0, a1, 3.8f, 4.8f, w0, w1, false);
            float pa = Mathf.Min(w0, w1), pb = Mathf.Max(w0, w1);
            BoxL(Frame, a0, a1, 4.8f, 4.86f, pa - .02f, pb + .02f, false);
        }
        Rail(uB + .05f, h1 - .15f, f0 + .12f, f0 + .15f);
        Rail(h1 - .15f, h1 - .12f, f0 + .12f, f1 - .12f);
        Rail(h0 + .12f, h1 - .15f, f1 - .15f, f1 - .12f);
        Rail(h0 + .12f, h0 + .15f, e1 + .05f, f1 - .12f);
        Top(Deck, Mathf.Min(Pw(uB + .2f, 0, f0 + .3f).x, Pw(h1 - .3f, 0, f1 - .3f).x), Mathf.Max(Pw(uB + .2f, 0, f0 + .3f).x, Pw(h1 - .3f, 0, f1 - .3f).x),
            Mathf.Min(Pw(uB + .2f, 0, f0 + .3f).z, Pw(h1 - .3f, 0, f1 - .3f).z), Mathf.Max(Pw(uB + .2f, 0, f0 + .3f).z, Pw(h1 - .3f, 0, f1 - .3f).z), 3.5f);
        // garaż: płaski dach z obróbką
        if (garageOn)
        {
            BoxL(Stucco, g0, g1, 0, 3.0f, f0, f0 + 6.5f);
            BoxL(Plinth, g0, g1 + .04f, 0, .35f, f0 - .04f, f0 + 6.54f, false);
            BoxL(Frame, g0, g1 + .05f, 3.0f, 3.12f, f0 - .05f, f0 + 6.55f, false, true);
            BoxL(GarageDoor, g0 + .7f, g1 - .7f, .02f, 2.45f, f0 - .06f, f0 + .02f, false);
            for (int k = 1; k < 6; k++) BoxL(Frame, g0 + .7f, g1 - .7f, .02f + k * .4f, .04f + k * .4f, f0 - .075f, f0 - .055f, false);
            BoxL(Frame, g0 + .55f, g1 - .55f, 2.45f, 2.6f, f0 - .1f, f0, false);                                // nadproże
        }

        // okna: szkło wysunięte 3 cm, rama antracyt, parapet zewnętrzny na piętrze
        void Pane(float a0, float a1, float y0, float y1, float w, float outward, Material gm = null)
        {
            float wa = w - outward * .03f, wb = w + outward * .015f;
            BoxL(gm ? gm : Glass, a0, a1, y0, y1, Mathf.Min(wa, wb), Mathf.Max(wa, wb), false);
            float fw = .07f, fa = w - outward * .05f, fb = w + outward * .01f;
            BoxL(Frame, a0 - fw, a1 + fw, y0 - fw, y0, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a0 - fw, a1 + fw, y1, y1 + fw, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a0 - fw, a0, y0, y1, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            BoxL(Frame, a1, a1 + fw, y0, y1, Mathf.Min(fa, fb), Mathf.Max(fa, fb), false);
            if (y0 > 3.5f) { float sa = w + outward * .01f, sb = w + outward * .09f; BoxL(Frame, a0 - .12f, a1 + .12f, y0 - fw - .05f, y0 - fw, Mathf.Min(sa, sb), Mathf.Max(sa, sb), false); }
        }
        // okno w ścianie bocznej (stałe a, szerokość wzdłuż głębokości w)
        void PaneSide(float w0, float w1, float y0, float y1, float a, float outward)
        {
            float aa = a + outward * .015f, ab = a - outward * .03f;
            BoxL(Glass, Mathf.Min(aa, ab), Mathf.Max(aa, ab), y0, y1, w0, w1, false);
            float fw = .07f, fa = a + outward * .01f, fb = a - outward * .05f, lo = Mathf.Min(fa, fb), hi = Mathf.Max(fa, fb);
            BoxL(Frame, lo, hi, y0 - fw, y0, w0 - fw, w1 + fw, false); BoxL(Frame, lo, hi, y1, y1 + fw, w0 - fw, w1 + fw, false);
            BoxL(Frame, lo, hi, y0, y1, w0 - fw, w0, false); BoxL(Frame, lo, hi, y0, y1, w1, w1 + fw, false);
        }
        // front parteru: drzwi przy krawędzi, dalej duże przeszklenia
        float door0 = garageOn ? h0 + 1.4f : h0 + Wm * .5f - .62f, door1 = door0 + 1.25f;
        if (!forSale)
        {
            BoxL(WoodDark, door0, door1, .02f, 2.45f, f0 - .06f, f0 + .02f, false);
            BoxL(Frame, door0 - .07f, door1 + .07f, .02f, 2.52f, f0 - .08f, f0 - .04f, false);
            BoxL(Steel, door1 - .28f, door1 - .22f, .7f, 1.9f, f0 - .14f, f0 - .08f, false);                  // pochwyt
        }
        else
        {
            // drzwi otwarte do środka (skrzydło obrócone o 90°), ościeżnica
            foreach (float a in new[] { door0 - .07f, door1 }) BoxL(Frame, a, a + .07f, .02f, 2.52f, f0 - .04f, f0 + .34f, false);
            BoxL(Frame, door0 - .07f, door1 + .07f, 2.45f, 2.52f, f0 - .04f, f0 + .34f, false);
            BoxL(WoodDark, door0 + .02f, door0 + .08f, .05f, 2.45f, f0 + .34f, f0 + .34f + (door1 - door0) - .05f, false);
        }
        BoxL(Stucco, door0 - .9f, door1 + 1.2f, 2.95f, 3.15f, f0 - 1.8f, f0, false, true);                   // daszek nad wejściem
        BoxL(Concrete, door0 - .5f, door1 + .5f, 0, .16f, f0 - 1.3f, f0, false);                               // stopień
        float gl0 = door1 + 1.0f, gl1 = h1 - 1.0f; int panes = Mathf.Max(1, Mathf.RoundToInt((gl1 - gl0) / 2.6f));
        for (int k = 0; k < panes; k++) { float a = gl0 + k * (gl1 - gl0) / panes + .12f, b = gl0 + (k + 1) * (gl1 - gl0) / panes - .12f; Pane(a, b, .4f, 2.9f, f0, -1, forSale ? GlassClear : null); }
        if (!garageOn && door0 - .8f - (h0 + 1f) > 1.2f) Pane(h0 + 1f, door0 - .8f, .4f, 2.9f, f0, -1);
        // tył parteru: przesuwne drzwi tarasowe
        int rear = Mathf.Max(2, Mathf.RoundToInt((Wm - 2f) / 2.8f));
        for (int k = 0; k < rear; k++) { if (forSale && k == rear / 2) continue; float a = h0 + 1f + k * (Wm - 2f) / rear + .1f, b = h0 + 1f + (k + 1) * (Wm - 2f) / rear - .1f; Pane(a, b, .3f, 3.0f, f1, 1, forSale ? GlassClear : null); }
        // WILLA NA SPRZEDAŻ: parter do wejścia — ściany z otworami (drzwi otwarte, przeszklenia z kolizją,
        // jedno skrzydło drzwi tarasowych otwarte), wnętrze: kuchnia z wyspą, jadalnia, salon z TV, parkiet,
        // sufit, lampy wiszące; tabliczka NA SPRZEDAŻ przy furtce. Piętro zamknięte (okna z pokojami).
        if (forSale)
        {
            const float wt = .3f, FY = .05f, CH = 3.15f;
            var paint = InteriorPaint;
            void ColL(float a0, float a1, float y0, float y1, float w0, float w1)
            {
                Vector3 p = Pw(a0, y0, w0), q = Pw(a1, y1, w1);
                Collider((p + q) * .5f, new Vector3(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y), Mathf.Abs(q.z - p.z)));
            }
            void Seg(float a0, float a1, float y0, float y1, float w0, float w1, float ia0, float ia1, float iw0, float iw1, bool solid = true)
            {
                BoxL(wallG, a0, a1, y0, y1, w0, w1, solid);
                BoxL(paint, ia0, ia1, Mathf.Max(y0, FY), Mathf.Min(y1, CH), iw0, iw1, false);
            }
            // boki
            Seg(h0, h0 + wt, 0, 3.4f, f0, f1, h0 + wt, h0 + wt + .02f, f0 + wt, f1 - wt);
            Seg(h1 - wt, h1, 0, 3.4f, f0, f1, h1 - wt - .02f, h1 - wt, f0 + wt, f1 - wt);
            // front: pełne, drzwi (otwarte), pełne, przeszklenie (parapet + nadproże), pełne
            Seg(h0, door0, 0, 3.4f, f0, f0 + wt, h0 + wt, door0, f0 + wt, f0 + wt + .02f);
            Seg(door0, door1, 2.45f, 3.4f, f0, f0 + wt, door0, door1, f0 + wt, f0 + wt + .02f, false);
            Seg(door1, gl0, 0, 3.4f, f0, f0 + wt, door1, gl0, f0 + wt, f0 + wt + .02f);
            Seg(gl0, gl1, 0, .4f, f0, f0 + wt, gl0, gl1, f0 + wt, f0 + wt + .02f);
            Seg(gl0, gl1, 2.9f, 3.4f, f0, f0 + wt, gl0, gl1, f0 + wt, f0 + wt + .02f, false);
            Seg(gl1, h1, 0, 3.4f, f0, f0 + wt, gl1, h1 - wt, f0 + wt, f0 + wt + .02f);
            ColL(gl0, gl1, .4f, 2.9f, f0 - .05f, f0 + .05f);
            // tył: słupy przy narożnikach, nadproże i próg; szyby z kolizją, środkowe skrzydło otwarte
            Seg(h0, h0 + 1f, 0, 3.4f, f1 - wt, f1, h0 + wt, h0 + 1f, f1 - wt - .02f, f1 - wt);
            Seg(h1 - 1f, h1, 0, 3.4f, f1 - wt, f1, h1 - 1f, h1 - wt, f1 - wt - .02f, f1 - wt);
            Seg(h0 + 1f, h1 - 1f, 3.0f, 3.4f, f1 - wt, f1, h0 + 1f, h1 - 1f, f1 - wt - .02f, f1 - wt, false);
            BoxL(Frame, h0 + 1f, h1 - 1f, 0, .06f, f1 - wt, f1, false);
            // cokół jako obwód na zewnątrz ścian (front z przerwą na drzwi, tył z przerwą na otwarte skrzydło, boki)
            float ra = h0 + 1f + (rear / 2) * (Wm - 2f) / rear, rb = ra + (Wm - 2f) / rear;
            BoxL(Plinth, h0 - .04f, door0 - .07f, 0, .35f, f0 - .04f, f0, false); BoxL(Plinth, door1 + .07f, h1 + .04f, 0, .35f, f0 - .04f, f0, false);
            BoxL(Plinth, h0 - .04f, ra, 0, .35f, f1, f1 + .04f, false); BoxL(Plinth, rb, h1 + .04f, 0, .35f, f1, f1 + .04f, false);
            BoxL(Plinth, h0 - .04f, h0, 0, .35f, f0 - .04f, f1 + .04f, false); BoxL(Plinth, h1, h1 + .04f, 0, .35f, f0 - .04f, f1 + .04f, false);
            // słupki między szybami frontu i tyłu (bez szczelin), parapet pod szybami tyłu (0.06–0.23), poza otwartym skrzydłem
            for (int k = 0; k <= panes; k++) { float xb = gl0 + k * (gl1 - gl0) / panes; Seg(xb - .13f, xb + .13f, .4f, 2.9f, f0, f0 + wt, xb - .13f, xb + .13f, f0 + wt, f0 + wt + .02f, false); }
            for (int k = 0; k <= rear; k++) { float xb = h0 + 1f + k * (Wm - 2f) / rear; Seg(xb - .11f, xb + .11f, .06f, 3.0f, f1 - wt, f1, xb - .11f, xb + .11f, f1 - wt - .02f, f1 - wt, false); }
            for (int k = 0; k < rear; k++)
            {
                if (k == rear / 2) continue;
                float pa = h0 + 1f + k * (Wm - 2f) / rear, pb = pa + (Wm - 2f) / rear;
                Seg(pa, pb, .06f, .23f, f1 - wt, f1, pa, pb, f1 - wt - .02f, f1 - wt, false);
            }
            for (int k = 0; k < rear; k++)
            {
                if (k == rear / 2) continue;
                float a = h0 + 1f + k * (Wm - 2f) / rear, b = h0 + 1f + (k + 1) * (Wm - 2f) / rear; ColL(a, b, .06f, 3.0f, f1 - .05f, f1 + .05f);
            }
            // posadzka (parkiet), sufit, cokolik
            var floorW = Pbr("parkiet dębowy", "wood_floor", 1.8f, new Color(.92f, .88f, .84f), .7f);
            var ceil = Plain("sufit biały", new Color(.93f, .93f, .92f), .1f);
            Vector3 fa = Pw(h0 + wt, 0, f0 + wt), fb = Pw(h1 - wt, 0, f1 - wt);
            Top(floorW, Mathf.Min(fa.x, fb.x), Mathf.Max(fa.x, fb.x), Mathf.Min(fa.z, fb.z), Mathf.Max(fa.z, fb.z), FY);
            BoxL(ceil, h0 + wt, h1 - wt, CH, CH + .05f, f0 + wt, f1 - wt, false, true);
            // kuchnia przy lewej ścianie: szafki dolne z blatem, górne, lodówka, wyspa
            var front_ = Plain("fronty kuchni grafit mat", new Color(.22f, .23f, .24f), .35f);
            float ka = h0 + wt, kw0 = f0 + 1.6f, kw1 = f0 + 6.4f;
            BoxL(front_, ka, ka + .6f, FY, .9f, kw0, kw1); BoxL(Concrete, ka, ka + .64f, .9f, .94f, kw0, kw1, false);
            BoxL(front_, ka, ka + .35f, 1.5f, 2.25f, kw0, kw1, false);
            for (float w = kw0 + .6f; w < kw1 - .1f; w += .6f) { BoxL(Steel, ka + .6f, ka + .62f, .2f, .75f, w - .01f, w + .01f, false); BoxL(Steel, ka + .35f, ka + .37f, 1.6f, 2.1f, w - .01f, w + .01f, false); }
            BoxL(Steel, ka, ka + .7f, FY, 2.1f, kw1, kw1 + .75f);
            BoxL(WoodDark, ka + 2.0f, ka + 3.0f, FY, .9f, kw0 + .6f, kw1 - .8f); BoxL(Concrete, ka + 1.9f, ka + 3.1f, .9f, .95f, kw0 + .5f, kw1 - .7f, false);
            // jadalnia i salon (meble Poly Haven), dywan, lampy wiszące
            float Yaw(Vector3 dirW) => Mathf.Atan2(dirW.x, dirW.z) * Mathf.Rad2Deg;
            float da = h0 + Wm * .42f, dw = f1 - 3.2f;
            if (diningTable) Put(diningTable, Pw(da, FY, dw), Yaw(Wax), .03f, true);   // dłuższy bok stołu (x modelu) wzdłuż a, krzesła przy długich bokach
            if (offChair) foreach (var (oa, ow, fdir) in new[] { (-.55f, -.85f, 1f), (.55f, -.85f, 1f), (-.55f, .85f, -1f), (.55f, .85f, -1f) })
                    Put(offChair, Pw(da + oa, FY, dw + ow), Yaw(Wax * fdir), .03f, false);
            var rug = Plain("dywan salonu", new Color(.62f, .58f, .52f), .05f);
            float la = h1 - wt - 2.6f, lw = f0 + 5.2f;
            Vector3 r0 = Pw(la - 1.7f, 0, lw - 1.3f), r1 = Pw(la + 1.7f, 0, lw + 1.3f);
            Top(rug, Mathf.Min(r0.x, r1.x), Mathf.Max(r0.x, r1.x), Mathf.Min(r0.z, r1.z), Mathf.Max(r0.z, r1.z), FY + .01f);
            if (tvCabinet) Put(tvCabinet, Pw(h1 - wt - .3f, FY, lw), Yaw(-Aax), .03f, true);
            BoxL(Plain("telewizor ekran", new Color(.03f, .03f, .035f), .9f), h1 - wt - .06f, h1 - wt - .02f, 1.2f, 2.0f, lw - .75f, lw + .75f, false);
            if (offSofa) Put(offSofa, Pw(la - 2.0f, FY, lw), Yaw(Aax), .03f, true);
            if (offArm) Put(offArm, Pw(la - .5f, FY, lw + 1.9f), Yaw(-Wax), .03f, true);
            if (coffeeTable) Put(coffeeTable, Pw(la - .7f, FY, lw), Yaw(Wax), .03f, true);
            if (offLamp) foreach (var (pa, pw2) in new[] { (ka + 2.5f, (kw0 + kw1) * .5f), (da, dw) })
                {
                    var o = Put(offLamp, Pw(pa, 0, pw2), 0f, .02f, false);
                    if (o) { var lb = RB(o); o.transform.position += Vector3.up * (CH - lb.max.y); }
                }
            // tabliczka NA SPRZEDAŻ przy furtce, na dwóch słupkach, od strony ulicy
            var sp = Pw(door1 + 1.9f, 0, .1f);
            foreach (float off in new[] { -.75f, .75f }) Box(Frame, sp + Aax * off + Vector3.up * .9f, new Vector3(.06f, 1.8f, .06f), false);
            Sign("sprzedaz", sp + Vector3.up * 1.55f - Wax * .05f, 1.8f, .45f, -Wax);
            saleDoors.Add(Pw((door0 + door1) * .5f, .1f, f0 + 1.0f));   // 0.7 m za ścianą frontu, 1.2 m przed wyspą (wyspa od f0 + 2.2)
            rep.forSale++;
        }

        // boki parteru: wąskie wysokie okna (strona garażu bez okien)
        if (!forSale) { PaneSide(f0 + 2.5f, f0 + 3.6f, .6f, 2.9f, h0, -1); PaneSide(f1 - 4.2f, f1 - 2.4f, .6f, 2.9f, h0, -1); }
        if (forSale) { }
        else if (!garageOn) { PaneSide(f0 + 2.5f, f0 + 3.6f, .6f, 2.9f, h1, 1); PaneSide(f1 - 4.2f, f1 - 2.4f, .6f, 2.9f, h1, 1); }
        else PaneSide(f0 + 8f, f1 - 1.2f, .9f, 2.9f, h1, 1);
        // piętro: wstęga okien z przodu, dwa okna z tyłu, boki, drzwi na taras
        Pane(uA + .8f, uB - .8f, 4.1f, 6.15f, e0, -1);
        int mull = Mathf.Max(1, Mathf.RoundToInt((uw - 1.6f) / 1.7f));
        for (int k = 1; k < mull; k++) { float a = uA + .8f + k * (uw - 1.6f) / mull; BoxL(Frame, a - .035f, a + .035f, 4.1f, 6.15f, e0 - .05f, e0 + .01f, false); }
        Pane(uA + .9f, uA + uw * .45f, 4.1f, 6.15f, e1, 1); Pane(uA + uw * .55f, uB - .9f, 4.1f, 6.15f, e1, 1);
        PaneSide(e0 + 1.6f, e0 + 2.6f, 4.3f, 6.0f, uA, -1); PaneSide(e1 - 3.2f, e1 - 1.4f, 4.3f, 6.0f, uA, -1);
        PaneSide(e0 + 2.0f, e0 + 4.4f, 3.85f, 6.0f, uB, 1);                                                   // drzwi na taras
        foreach (float a in new[] { door0 - .35f, door1 + .35f }) BoxL(Frame, a - .07f, a + .07f, 2.05f, 2.35f, f0 - .12f, f0, false);   // kinkiety (bez świecenia)
        Planter(Pw(door1 + .95f, .02f, f0 - .55f), .7f, .7f, index * 13 + 1, 0, 2);

        // dach piętra
        float yTop = 6.7f; var gut = Frame;
        void Gutter(Vector3 a, Vector3 b) { var c = (a + b) * .5f; var d = b - a; float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; OBox(gut, c - Vector3.up * .06f, new Vector3(.13f, .11f, d.magnitude), yaw); }
        void Downpipe(Vector3 p, float y0, float y1) => Cylinder(gut, new Vector3(p.x, y0, p.z), .05f, y1 - y0, 8, false, false);
        // płyta dachu pochyłego: wierzch (pokrycie), spód (podbitka) i krawędzie (obróbka)
        void Plane(Material top, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var n = Normal(a, b, c, Vector3.up); var t = Vector3.down * .16f;
            Quad(top, a, b, c, d, n); Quad(WoodDark, a + t, b + t, c + t, d + t, -n);
            Quad(Frame, a, b, b + t, a + t, Normal(a, b, b + t, ((a + b) * .5f - (a + b + c + d) * .25f)));
            Quad(Frame, b, c, c + t, b + t, Normal(b, c, c + t, ((b + c) * .5f - (a + b + c + d) * .25f)));
            Quad(Frame, c, d, d + t, c + t, Normal(c, d, d + t, ((c + d) * .5f - (a + b + c + d) * .25f)));
            Quad(Frame, d, a, a + t, d + t, Normal(d, a, a + t, ((d + a) * .5f - (a + b + c + d) * .25f)));
        }
        if (variant == 0)
        {
            BoxL(wallU, uA, uB, yTop, yTop + .25f, e0, e1, false);
            BoxL(Frame, uA - .55f, uB + .55f, yTop + .25f, yTop + .47f, e0 - .55f, e1 + .55f, false, true);
            BoxL(Gravel, uA - .4f, uB + .4f, yTop + .47f, yTop + .5f, e0 - .4f, e1 + .4f, false);
            Downpipe(Pw(uA - .12f, 0, e1 - .3f), 3.8f, yTop + .25f); Downpipe(Pw(uB + .12f, 0, e1 - .3f), 3.8f, yTop + .25f);
            rep.roofs++;
        }
        else if (variant == 1)
        {
            // dwuspadowy, kalenica w głąb działki (szczyt do ulicy), łupek grafitowy, okap 45 cm, szczyt 35 cm
            float ov = .45f, ovg = .35f, pitch = Mathf.Tan(38f * Mathf.Deg2Rad), rh = (uw * .5f) * pitch, yE = yTop - ov * pitch, yR = yTop + rh;
            float wa = e0 - ovg, wb = e1 + ovg;
            Plane(Slate, Pw(uA - ov, yE, wa), Pw(am, yR, wa), Pw(am, yR, wb), Pw(uA - ov, yE, wb));
            Plane(Slate, Pw(uB + ov, yE, wa), Pw(am, yR, wa), Pw(am, yR, wb), Pw(uB + ov, yE, wb));
            BoxL(Frame, am - .09f, am + .09f, yR - .02f, yR + .1f, wa, wb, false);                                 // gąsior
            var nf = -Wax; var nb = Wax;
            Tri(wallU, Pw(uA, yTop, e0), Pw(uB, yTop, e0), Pw(am, yR, e0), nf); Tri(wallU, Pw(uA, yTop, e1), Pw(uB, yTop, e1), Pw(am, yR, e1), nb);
            Pane(am - .7f, am + .7f, yTop + .25f, yTop + rh * .62f, e0, -1);                                         // okno w szczycie
            Pane(am - .55f, am + .55f, yTop + .25f, yTop + rh * .55f, e1, 1);
            Gutter(Pw(uA - ov, yE, wa), Pw(uA - ov, yE, wb)); Gutter(Pw(uB + ov, yE, wa), Pw(uB + ov, yE, wb));
            Downpipe(Pw(uA - ov + .06f, 0, e1 - .2f), 3.8f, yE); Downpipe(Pw(uB + ov - .06f, 0, e1 - .2f), 3.8f, yE);
            rep.roofs++;
        }
        else
        {
            // kopertowy, kalenica wzdłuż ulicy, dachówka ceramiczna, okap 50 cm, komin z klinkieru
            float ov = .5f, pitch = Mathf.Tan(32f * Mathf.Deg2Rad), dW = (e1 - e0) + 2 * ov, rh = dW * .5f * pitch, yE = yTop - ov * pitch, yR = yTop + (dW * .5f - ov) * pitch;
            float aL = uA - ov, aR = uB + ov, wa = e0 - ov, wb = e1 + ov, wm = (e0 + e1) * .5f;
            float r0 = aL + dW * .5f, r1 = aR - dW * .5f; if (r1 < r0) { r0 = r1 = (aL + aR) * .5f; }
            Plane(ClayRoof, Pw(aL, yE, wa), Pw(aR, yE, wa), Pw(r1, yR, wm), Pw(r0, yR, wm));
            Plane(ClayRoof, Pw(aL, yE, wb), Pw(aR, yE, wb), Pw(r1, yR, wm), Pw(r0, yR, wm));
            Plane(ClayRoof, Pw(aL, yE, wa), Pw(aL, yE, wb), Pw(r0, yR, wm), Pw(r0, yR, wm));
            Plane(ClayRoof, Pw(aR, yE, wa), Pw(aR, yE, wb), Pw(r1, yR, wm), Pw(r1, yR, wm));
            if (r1 > r0) BoxL(Frame, r0, r1, yR - .02f, yR + .1f, wm - .09f, wm + .09f, false);
            BoxL(Clinker, am + uw * .18f - .35f, am + uw * .18f + .35f, yTop, yR + .8f, wm + .7f, wm + 1.4f, false);
            BoxL(Frame, am + uw * .18f - .42f, am + uw * .18f + .42f, yR + .8f, yR + .9f, wm + .63f, wm + 1.47f, false);
            Gutter(Pw(aL, yE, wa), Pw(aR, yE, wa)); Gutter(Pw(aL, yE, wb), Pw(aR, yE, wb));
            Downpipe(Pw(aL + .1f, 0, wb - .1f), 3.8f, yE); Downpipe(Pw(aR - .1f, 0, wb - .1f), 3.8f, yE);
            rep.roofs++;
        }
        { Vector3 ra = Pw(uA - .6f, yTop - .6f, e0 - .6f), rb2 = Pw(uB + .6f, yTop + 4.8f, e1 + .6f);
          roofBounds.Add(MinMax(ra.x, rb2.x, ra.y, rb2.y, ra.z, rb2.z)); }
        // rury spustowe parteru (tylne narożniki)
        Downpipe(Pw(h0 - .1f, 0, f1 - .15f), 0f, 3.75f); Downpipe(Pw(h1 + (garageOn ? -.15f : .1f), 0, f1 - .15f), 0f, 3.75f);
        Gutter(Pw(h0, 3.86f, f1 + .08f), Pw(h1, 3.86f, f1 + .08f));

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
        // podjazd (jodełka) i ścieżka (płyty) od chodnika; podjazd przechodzi przez pas zieleni do jezdni
        if (garageOn) BoxL(Herring, g0 + .5f, g1 - .5f, 0, .04f, 0, f0, false);
        if (garageOn && !swap && depthOverride <= 0) BoxL(Herring, g0 + .5f, g1 - .5f, 0, .15f, -(Walk + Green + .02f), -Walk + .02f, false);
        BoxL(Slabs, door0 - .1f, door1 + .1f, 0, .04f, 0, f0 - 1.3f, false);
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
        // zieleń działki: drzewo przy ulicy, dwa w ogrodzie, krzewy przy płotach, rabaty przed domem i przy tarasie
        if (Tree(Pw(W - 3f, 0, 3.4f), 6.5f + (index % 3) * .8f, index * 47f)) rep.plotTrees++;
        foreach (var (ta, tw, th) in new[] { (2.6f, depth - 2.8f, 7.6f), (W - 2.8f, depth - 3.6f, 8.6f), (W - 2.6f, f1 + 1.5f, 6.4f) })
        {
            var p = Pw(ta, 0, tw);
            if (tw > 1f && tw < depth - 1f && !IsHard(p.x, p.z, 1.3f) && !NearTree(p.x, p.z, 4.5f) && Tree(p, th + (index % 2) * .7f, index * 31f + ta * 7f)) rep.plotTrees++;
        }
        for (float a = 4.5f; a < W - 3.5f; a += 2.4f)
        {
            var p = Pw(a, 0, depth - 1.2f);
            if (!IsHard(p.x, p.z, .55f) && !NearTree(p.x, p.z, 2.2f)) Bush(p, 1.3f + (float)treeRnd.NextDouble() * .6f, a * 41f);
        }
        for (float w = f1 + 4.6f; w < depth - 3.5f; w += 2.7f)
        {
            var p = Pw(.95f, 0, w);
            if (!IsHard(p.x, p.z, .5f) && !NearTree(p.x, p.z, 2f)) Bush(p, 1.3f + (float)treeRnd.NextDouble() * .5f, w * 23f);
        }
        {
            Vector3 a = Pw(gl0 - .1f, 0, 1.0f), b = Pw(gl1 + .1f, 0, 2.3f);
            FlowerBed(Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.z, b.z), index * 7 + 3, index % 2 == 0 ? new[] { 0, 2, 3 } : new[] { 1, 3, 2 });
            Vector3 c = Pw(h0 + .4f, 0, f1 + 4.5f), d = Pw(h0 + Math.Min(5.5f, Wm * .4f), 0, f1 + 5.6f);
            if (!pool && !IsHard((c.x + d.x) * .5f, (c.z + d.z) * .5f, .1f))
                FlowerBed(Mathf.Min(c.x, d.x), Mathf.Max(c.x, d.x), Mathf.Min(c.z, d.z), Mathf.Max(c.z, d.z), index * 11 + 5, 3, 0);
        }
        TuftLine(Pw(.2f, 0, 2.6f), Pw(.2f, 0, f1 + 4f), 3.2f, mirror ? -1f : 1f);
        for (int k = 0; k < 4; k++) Tuft(Pw(W - 3f + Mathf.Cos(k * 1.7f) * 1.1f, 0, 3.4f + Mathf.Sin(k * 1.7f) * 1.1f));
        Shrub(Pw(gl0 + .6f, 0, f0 - .9f), .38f, index * 31f);
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

    // Wnętrza lokali w pierzei B (do wejścia): butik, shake bar, kawiarnia, market. Posadzka, sufit z panelami
    // (matowe, nie świecą), meble z brył i modeli Poly Haven, produkty jako drobne bryły (scalone w siatki).
    static Material Pal(int i)
    {
        var cols = new[] { new Color(.70f, .14f, .12f), new Color(.88f, .70f, .14f), new Color(.20f, .45f, .25f), new Color(.18f, .30f, .58f),
                           new Color(.92f, .91f, .88f), new Color(.85f, .45f, .12f), new Color(.12f, .12f, .13f), new Color(.55f, .52f, .48f) };
        string[] names = { "czerwony", "żółty", "zielony", "niebieski", "biały", "pomarańczowy", "czarny", "szary" };
        i = ((i % cols.Length) + cols.Length) % cols.Length;
        return Plain("towar " + names[i], cols[i], .35f);
    }
    static void ShopInterior(int k, string kind, float a, float b, float zf, float zb, float H0, float FY)
    {
        var rnd = new System.Random(900 + k);
        var wood = Pbr("parkiet dębowy", "wood_floor", 1.8f, new Color(.92f, .88f, .84f), .7f);
        var ceil = Plain("sufit biały", new Color(.93f, .93f, .92f), .1f);
        Top(kind == "market" || kind == "nutrition" ? PbrFloor : kind == "shake" ? Concrete : wood, a + .1f, b - .1f, zf, zb, FY);
        BoxMinMax(ceil, a + .1f, b - .1f, H0 - .35f, H0 - .3f, zf, zb, false, true);
        if (kind != "nutrition")   // Nutrition ma własne lampy liniowe z zestawu
            for (float x = a + 2f; x < b - 1.5f; x += 3f) for (float z = zf + 2f; z < zb - 1f; z += 3f) BoxMinMax(Paint, x - .3f, x + .3f, H0 - .37f, H0 - .35f, z - .3f, z + .3f, false);
        float cx = (a + b) * .5f;
        if (kind == "nutrition") NutritionInterior(a, b, zf, zb, H0, FY);
        else if (kind == "butik")   // butik: wieszaki przy ścianie, dwa stojaki, stoły z ubraniami, lada, lustro, przymierzalnie
        {
            void Rail(float x0, float x1, float z, float y)
            {
                Tube(Steel, new Vector3(x0, y, z), new Vector3(x1, y, z), .02f, 8);
                for (float x = x0 + .1f; x < x1 - .05f; x += .13f) BoxMinMax(Pal(rnd.Next(8)), x - .04f, x + .04f, y - .9f + (float)rnd.NextDouble() * .15f, y - .05f, z - .22f, z + .22f, false);
            }
            Rail(a + 1f, b - 4f, zb - .45f, 1.65f);
            foreach (float rz in new[] { zf + 4.5f, zf + 8f })
            {
                foreach (float rx in new[] { cx - 2.5f, cx + 2.5f }) Tube(Steel, new Vector3(rx, FY, rz), new Vector3(rx, 1.6f, rz), .025f, 8);
                Rail(cx - 2.5f, cx + 2.5f, rz, 1.55f); Collider(new Vector3(cx, .9f, rz), new Vector3(5f, 1.6f, .6f));
            }
            foreach (float tz in new[] { zf + 2.2f })
                foreach (float tx in new[] { cx - 3.5f, cx + 3.5f })
                {
                    BoxMinMax(WoodDark, tx - .8f, tx + .8f, FY, .8f, tz - .5f, tz + .5f);
                    for (int s2 = 0; s2 < 6; s2++) BoxMinMax(Pal(rnd.Next(8)), tx - .7f + (s2 % 3) * .5f, tx - .32f + (s2 % 3) * .5f, .8f, .8f + .06f * (1 + rnd.Next(4)), tz - .4f + (s2 / 3) * .45f, tz - .05f + (s2 / 3) * .45f, false);
                }
            BoxMinMax(WoodDark, b - 3.4f, b - .8f, FY, 1.0f, zf + 9.5f, zf + 10.2f); BoxMinMax(Concrete, b - 3.45f, b - .75f, 1.0f, 1.05f, zf + 9.45f, zf + 10.25f, false);
            BoxMinMax(Plain("lustro", new Color(.75f, .78f, .8f), .98f, 1f), a + .12f, a + .16f, .3f, 2.4f, zf + 3f, zf + 5f, false);
            foreach (float px in new[] { b - 3.5f, b - 2.2f })
            {
                BoxMinMax(InteriorPaint, px - .05f, px + .05f, FY, 2.3f, zb - 1.6f, zb, true);
                BoxMinMax(Plain("zasłona przymierzalni", new Color(.35f, .30f, .26f), .1f), px, px + 1.25f, .2f, 2.25f, zb - 1.62f, zb - 1.58f, false);
            }
        }
        else if (kind == "shake")   // shake bar: lada z hokerami, regał z shakerami i odżywkami, lodówka, stoliki
        {
            float bz = zf + 6f;
            BoxMinMax(WoodDark, a + 2f, b - 4f, FY, 1.05f, bz - .35f, bz + .35f); BoxMinMax(Concrete, a + 1.95f, b - 3.95f, 1.05f, 1.1f, bz - .45f, bz + .45f, false);
            for (float x = a + 2.6f; x < b - 4.4f; x += .9f) { Cylinder(Frame, new Vector3(x, FY, bz - .85f), .04f, .72f, 8, false, false); Cylinder(Plain("siedzisko skóra", new Color(.15f, .15f, .16f), .4f), new Vector3(x, .74f, bz - .85f), .2f, .06f, 14, true, false); Collider(new Vector3(x, .4f, bz - .85f), new Vector3(.4f, .8f, .4f)); }
            BoxMinMax(Steel, a + 2.2f, a + 2.9f, 1.1f, 1.45f, bz - .2f, bz + .2f, false);   // blender
            var shaker = nutriKit ? nutriKit.GetComponentsInChildren<Transform>(true).FirstOrDefault(q => q.name == "Shaker") : null;
            if (shaker) for (int s3 = 0; s3 < 6; s3++) { var o = (GameObject)UnityEngine.Object.Instantiate(shaker.gameObject, Group(group), false); o.transform.position = new Vector3(a + 3.4f + s3 * .35f, 1.1f, bz); o.transform.rotation = Quaternion.Euler(0, s3 * 30f, 0); }
            BoxMinMax(WoodDark, a + 1f, b - 1f, FY, 2.4f, zb - .45f, zb, false);
            for (int s2 = 0; s2 < 4; s2++)
            {
                float y = .5f + s2 * .5f; BoxMinMax(Concrete, a + 1f, b - 1f, y, y + .03f, zb - .45f, zb - .05f, false);
                for (float x = a + 1.3f; x < b - 1.2f; x += .26f)
                {
                    bool tub = (s2 + (int)(x * 3)) % 3 == 0;
                    Cylinder(Pal(tub ? 6 : rnd.Next(8)), new Vector3(x, y + .03f, zb - .25f), tub ? .09f : .05f, tub ? .22f : .2f, 12, true, false);
                    if (!tub) Cylinder(Pal(6), new Vector3(x, y + .23f, zb - .25f), .052f, .04f, 12, true, false);   // nakrętka shakera
                }
            }
            BoxMinMax(Steel, b - 2.8f, b - 1.4f, FY, 2.1f, zf + 1f, zf + 1.7f); BoxMinMax(GlassClear, b - 2.75f, b - 1.45f, .3f, 2.0f, zf + 1.71f, zf + 1.74f, false);
            foreach (float tx in new[] { a + 2.5f, a + 5f })
            {
                Cylinder(Frame, new Vector3(tx, FY, zf + 2f), .05f, 1.0f, 10, false, false); Cylinder(Concrete, new Vector3(tx, 1.05f, zf + 2f), .35f, .04f, 16, true, false);
                Collider(new Vector3(tx, .55f, zf + 2f), new Vector3(.7f, 1.1f, .7f));
            }
        }
        else if (kind == "kawiarnia")   // kawiarnia: lada z witryną ciast i ekspresem, tablica menu, stoliki z krzesłami, donice
        {
            float lz = zb - 2.2f;
            BoxMinMax(WoodDark, a + 2f, b - 2f, FY, 1.0f, lz - .35f, lz + .35f); BoxMinMax(Concrete, a + 1.95f, b - 1.95f, 1.0f, 1.05f, lz - .45f, lz + .45f, false);
            BoxMinMax(GlassClear, a + 3f, a + 5f, 1.05f, 1.5f, lz - .4f, lz + .1f, false);
            for (int c2 = 0; c2 < 6; c2++) Cylinder(Pal(c2 % 2 == 0 ? 5 : 7), new Vector3(a + 3.2f + c2 * .3f, 1.07f, lz - .15f), .1f, .06f, 12, true, false);
            BoxMinMax(Steel, b - 4.5f, b - 3.6f, 1.05f, 1.55f, lz - .2f, lz + .3f, false);   // ekspres
            BoxMinMax(Plain("tablica menu", new Color(.10f, .11f, .10f), .2f), a + 3f, b - 3f, 2.0f, 3.0f, zb - .06f, zb - .02f, false);
            for (int r2 = 0; r2 < 5; r2++) BoxMinMax(Paint, a + 3.4f, a + 3.4f + 2f + (float)rnd.NextDouble() * 3f, 2.75f - r2 * .15f, 2.79f - r2 * .15f, zb - .07f, zb - .065f, false);
            foreach (var (tx, tz) in new[] { (a + 2.5f, zf + 2.2f), (cx, zf + 2.2f), (b - 2.5f, zf + 2.2f), (a + 4f, zf + 5.5f), (b - 4f, zf + 5.5f) })
            {
                Cylinder(Frame, new Vector3(tx, FY, tz), .05f, .72f, 10, false, false); Cylinder(wood, new Vector3(tx, .72f, tz), .38f, .04f, 16, true, false);
                Collider(new Vector3(tx, .38f, tz), new Vector3(.8f, .76f, .8f));
                if (offChair) { Put(offChair, new Vector3(tx - .7f, FY, tz), 90f, .03f, false); Put(offChair, new Vector3(tx + .7f, FY, tz), 270f, .03f, false); }
            }
            Planter(new Vector3(a + .8f, FY, zf + .8f), .8f, .8f, 811, 2, 3); Planter(new Vector3(b - .8f, FY, zf + .8f), .8f, .8f, 812, 0, 1);
        }
        else if (kind == "market")   // market: trzy regały wyspowe z towarem, lodówki pod tylną ścianą, kasa przy wejściu
        {
            foreach (float gx in new[] { cx - 3.6f, cx, cx + 3.6f })
            {
                float g0 = zf + 3.2f, g1 = zb - 3f;
                BoxMinMax(Plain("regał sklepowy", new Color(.82f, .82f, .80f), .4f), gx - .5f, gx + .5f, FY, 1.75f, g0, g1);
                for (int sh = 0; sh < 4; sh++)
                    foreach (int side in new[] { -1, 1 })
                    {
                        float y = .25f + sh * .4f, xs = gx + side * .5f;
                        BoxMinMax(Frame, Mathf.Min(xs, xs + side * .3f), Mathf.Max(xs, xs + side * .3f), y - .03f, y, g0, g1, false);
                        for (float z = g0 + .05f; z < g1 - .1f;)
                        {
                            float w = .08f + (float)rnd.NextDouble() * .12f, hh = .12f + (float)rnd.NextDouble() * .2f;
                            BoxMinMax(Pal(rnd.Next(8)), Mathf.Min(xs, xs + side * .26f), Mathf.Max(xs, xs + side * .26f), y, y + hh, z, z + w, false); z += w + .015f;
                        }
                    }
            }
            BoxMinMax(Steel, a + 1f, b - 1f, FY, 2.1f, zb - .8f, zb, true);
            BoxMinMax(GlassClear, a + 1.05f, b - 1.05f, .25f, 2.0f, zb - .83f, zb - .8f, false);
            for (float x = a + 1.3f; x < b - 1.3f; x += .18f) for (int sh = 0; sh < 3; sh++) Cylinder(Pal(rnd.Next(6)), new Vector3(x, .32f + sh * .58f, zb - .5f), .045f, .28f, 8, true, false);
            BoxMinMax(Frame, b - 4f, b - 1.2f, FY, .9f, zf + 1.5f, zf + 2.2f); BoxMinMax(Concrete, b - 4.05f, b - 1.15f, .9f, .95f, zf + 1.45f, zf + 2.25f, false);
            BoxMinMax(Frame, b - 1.9f, b - 1.4f, .95f, 1.3f, zf + 1.6f, zf + 2.0f, false);   // kasa
        }
    }

    // Pierzeja: parter ze sklepami (witryny, wejścia, markizy, szyldy), piętro z apartamentami i balkonami,
    // placyk przed budynkiem ze stolikami kawiarni. front = granica działek od ulicy (z), budynek w głąb (+z).
    // Parter w klinkierze, piętro tynk, gzyms i attyka; okna i balkony także z boków i od parku (żadnych gładkich ścian).
    static void ShopRow(float x0, float x1, float front)
    {
        group = "Osiedle B / pierzeja sklepów";
        float zf = front + 6f, zb = zf + 14f, H0 = 4.2f, H1 = 7.6f, PY = .14f;
        BoxMinMax(Slabs, x0 - .55f, x1, 0, PY, front, zf, false);                     // placyk na wysokości chodnika
        BoxMinMax(Slabs, x0 - .55f, x0, 0, PY, zf, zb + 1.5f, false);                  // przejście wzdłuż ściany od ulicy N-S
        // parter do wejścia: ściany z klinkieru (tył, boki), front z filarów i witryn, ściany działowe między lokalami
        const float wt = .3f;
        BoxMinMax(Clinker, x0, x1, 0, H0, zb - wt, zb);
        BoxMinMax(Clinker, x0, x0 + wt, 0, H0, zf, zb - wt); BoxMinMax(Clinker, x1 - wt, x1, 0, H0, zf, zb - wt);
        // cokół obwodowy (pełny blok zakryłby posadzki lokali): boki tutaj, front i tył w pętli lokali (przerwy na drzwi)
        BoxMinMax(Plinth, x0 - .04f, x0, 0, .3f, zf - .04f, zb + .04f, false); BoxMinMax(Plinth, x1, x1 + .04f, 0, .3f, zf - .04f, zb + .04f, false);
        BoxMinMax(Concrete, x0 - .05f, x1 + .05f, H0 - .3f, H0, zf - .05f, zb + .05f, false);   // wieniec nad parterem
        BoxMinMax(StuccoWarm, x0 + .3f, x1 - .3f, H0, H1, zf + .8f, zb - .3f);      // piętro cofnięte (balkony z przodu)
        BoxMinMax(Stucco, x0 + .2f, x1 - .2f, H1, H1 + .4f, zf + .7f, zb - .2f, false);
        BoxMinMax(Frame, x0 + .1f, x1 - .1f, H1 + .4f, H1 + .52f, zf + .6f, zb - .1f, false, true);   // gzyms
        BoxMinMax(Gravel, x0 + .45f, x1 - .45f, H1 + .52f, H1 + .55f, zf + .95f, zb - .45f, false);
        BoxMinMax(Gravel, x0 + .2f, x1 - .2f, H0, H0 + .05f, zf + .05f, zf + .8f, false);     // posadzka balkonów
        BoxMinMax(GlassClear, x0 + .2f, x1 - .2f, H0 + .05f, H0 + 1.05f, zf + .05f, zf + .09f, false);
        BoxMinMax(Frame, x0 + .2f, x1 - .2f, H0 + 1.05f, H0 + 1.1f, zf + .03f, zf + .11f, false);
        string[] shops = { "butik", "shake", "nutrition", "kawiarnia", "market" };   // Nutrition obok Shake Baru (dokument PREFABY)
        float unit = (x1 - x0) / shops.Length;
        for (int k = 0; k < shops.Length; k++)
        {
            float a = x0 + k * unit, b = a + unit;
            // witryna na całą szerokość lokalu (szkło wysunięte, ramy co 2.4 m), drzwi szklane
            float d0 = (a + b) * .5f - .9f;
            BoxMinMax(Clinker, a, a + .6f, 0, H0, zf, zf + wt); BoxMinMax(Clinker, b - .6f, b, 0, H0, zf, zf + wt);
            BoxMinMax(Clinker, a + .6f, b - .6f, 3.5f, H0, zf, zf + wt, false);
            BoxMinMax(Clinker, a + .6f, d0, 0, .35f, zf, zf + wt, false); BoxMinMax(Clinker, d0 + 1.8f, b - .6f, 0, .35f, zf, zf + wt, false);
            BoxMinMax(GlassClear, a + .6f, d0, .35f, 3.5f, zf - .03f, zf + .02f, false); BoxMinMax(GlassClear, d0 + 1.8f, b - .6f, .35f, 3.5f, zf - .03f, zf + .02f, false);
            BoxMinMax(GlassClear, d0, d0 + 1.8f, 2.65f, 3.5f, zf - .03f, zf + .02f, false); BoxMinMax(Frame, d0, d0 + 1.8f, 2.6f, 2.65f, zf - .06f, zf + .01f, false);
            Collider(new Vector3((a + .6f + d0) * .5f, 1.9f, zf + .1f), new Vector3(d0 - a - .6f, 3.2f, .25f)); Collider(new Vector3((d0 + 1.8f + b - .6f) * .5f, 1.9f, zf + .1f), new Vector3(b - .6f - d0 - 1.8f, 3.2f, .25f));
            if (k > 0) BoxMinMax(InteriorPaint, a - .1f, a + .1f, 0, H0, zf + wt, zb - wt);
            ShopInterior(k, shops[k], a + (k == 0 ? wt : .1f), b - (k == shops.Length - 1 ? wt : .1f), zf + wt, zb - wt, H0, PY);
            for (float x = a + .6f; x <= b - .55f; x += Mathf.Max(1.8f, (b - a - 1.2f) / Mathf.Max(1, Mathf.Round((b - a - 1.2f) / 2.4f))))
                if (x < d0 - .1f || x > d0 + 1.9f) BoxMinMax(Frame, x - .04f, x + .04f, .35f, 3.5f, zf - .06f, zf + .01f, false);
            foreach (float x in new[] { d0, d0 + 1.8f }) BoxMinMax(Frame, x - .05f, x + .05f, 0, 3.5f, zf - .06f, zf + .01f, false);   // ościeżnica wejścia
            BoxMinMax(Frame, a + .6f, b - .6f, 3.5f, 3.58f, zf - .06f, zf + .01f, false);
            BoxMinMax(Frame, a + .6f, d0, .3f, .38f, zf - .06f, zf + .01f, false); BoxMinMax(Frame, d0 + 1.8f, b - .6f, .3f, .38f, zf - .06f, zf + .01f, false);
            // drzwi automatyczne otwarte (wejście bez skrzydła)
            // markiza (stal) i szyld nad nią
            BoxMinMax(Frame, a + .4f, b - .4f, 3.62f, 3.72f, zf - 1.6f, zf, false, true);
            Sign(shops[k], new Vector3((a + b) * .5f, 3.95f, zf - .12f), Mathf.Min(6f, unit - 2f), 1.5f * Mathf.Min(6f, unit - 2f) / 6f, Vector3.back);
            // okna apartamentów: front (za balkonem) i tył (od parku) z balkonami francuskimi
            for (float x = a + 1.4f; x < b - 1.2f; x += 3.2f)
            {
                BoxMinMax(FlatGlass, x, x + 2.0f, H0 + .6f, H0 + 2.9f, zf + .77f, zf + .83f, false);
                BoxMinMax(Frame, x - .06f, x + 2.06f, H0 + 2.9f, H0 + 2.97f, zf + .75f, zf + .84f, false);
                BoxMinMax(FlatGlass, x + .2f, x + 1.8f, H0 + .4f, H0 + 2.9f, zb - .33f, zb - .27f, false);
                BoxMinMax(Frame, x + .14f, x + 1.86f, H0 + 2.9f, H0 + 2.97f, zb - .34f, zb - .24f, false);
                BoxMinMax(Frame, x + .14f, x + 1.86f, H0 + .33f, H0 + .4f, zb - .34f, zb - .16f, false);
                BoxMinMax(GlassClear, x + .14f, x + 1.86f, H0 + .45f, H0 + 1.4f, zb - .2f, zb - .17f, false);
                BoxMinMax(Frame, x + .14f, x + 1.86f, H0 + 1.4f, H0 + 1.45f, zb - .21f, zb - .16f, false);
            }
            // tył parteru: okna zaplecza i drzwi serwisowe
            BoxMinMax(ShopGlass, a + 1.5f, a + 3.3f, 1.2f, 2.8f, zb, zb + .04f, false);
            BoxMinMax(Frame, a + 1.43f, a + 3.37f, 1.13f, 1.2f, zb, zb + .1f, false);
            BoxMinMax(GarageDoor, b - 2.6f, b - 1.5f, .02f, 2.3f, zb, zb + .05f, false);
            BoxMinMax(Plinth, a, d0 - .05f, 0, .3f, zf - .04f, zf, false); BoxMinMax(Plinth, d0 + 1.85f, b, 0, .3f, zf - .04f, zf, false);
            BoxMinMax(Plinth, a, b - 2.65f, 0, .3f, zb, zb + .04f, false); BoxMinMax(Plinth, b - 1.45f, b, 0, .3f, zb, zb + .04f, false);
            if (k > 0) BoxMinMax(Plinth, a - .15f, a + .15f, 0, H0, zf - .05f, zf + .02f, false);   // pilastry między lokalami
        }
        // boki: okna parteru i piętra (narożnik od ulicy N-S widać z daleka)
        foreach (float x in new[] { x0, x1 })
        {
            float o = x == x0 ? -1f : 1f, xg = x + o * .02f, xu = x + o * (-.3f + .02f);
            for (float z = zf + 2.5f; z < zb - 2f; z += 4f)
            {
                BoxMinMax(FlatGlass, Mathf.Min(xu, xu + o * .03f), Mathf.Max(xu, xu + o * .03f), H0 + .6f, H0 + 2.8f, z + .1f, z + 1.5f, false);
                BoxMinMax(Frame, Mathf.Min(xu, xu + o * .06f), Mathf.Max(xu, xu + o * .06f), H0 + 2.8f, H0 + 2.87f, z + .04f, z + 1.56f, false);
                BoxMinMax(Frame, Mathf.Min(xu, xu + o * .12f), Mathf.Max(xu, xu + o * .12f), H0 + .5f, H0 + .56f, z, z + 1.6f, false);
            }
        }
        // rury spustowe na narożnikach
        foreach (float x in new[] { x0 + .1f, x1 - .1f }) foreach (float z in new[] { zf + .1f, zb - .1f })
            Cylinder(Frame, new Vector3(x, 0, z), .06f, H1 + .45f, 8, false, false);
        // ogródek kawiarni, donice z kwiatami i ławki na placyku
        float cx = x0 + (Array.IndexOf(shops, "kawiarnia") + .5f) * unit;     // przed kawiarnią
        if (outdoorSet) for (int k = 0; k < 4; k++) Put(outdoorSet, new Vector3(cx - 4.5f + k * 3f, PY, front + 3f), k * 25f, .03f, true);
        int pk = 0;
        for (float x = x0 + 3f; x < x1 - 2f; x += 9f) Planter(new Vector3(x, PY, front + .9f), 1.4f, .7f, 300 + pk++);
        ParkBench(new Vector3(x0 + unit * .5f, PY, front + 1.6f), 0); ParkBench(new Vector3(x1 - unit * .5f, PY, front + 1.6f), 0);
        // zieleń za budynkiem (od parku): drzewa i krzewy
        for (float x = x0 + 4f; x < x1 - 3f; x += 10f)
        {
            if (Tree(new Vector3(x, 0, zb + 5.5f), 7.5f + (float)treeRnd.NextDouble() * 1.5f, x * 13f)) rep.plotTrees++;
            Bush(new Vector3(x + 5f, 0, zb + 2.2f), 1.8f, x * 7f);
        }
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
        BoxMinMax(ShopGlass, cx - 5.5f, cx + 5.5f, .3f, 2.9f, Mathf.Min(face, face - dir * .05f), Mathf.Max(face, face - dir * .05f), false);
        Sign("kort", new Vector3(cx + 6f, 2.5f, cz - dir * 9.12f), 5f, 1.25f, new Vector3(0, 0, -dir));
        ParkBench(new Vector3(cx - 4f, .035f, cz - dir * 10.2f), dir > 0 ? 180 : 0); ParkBench(new Vector3(cx + 4f, .035f, cz - dir * 10.2f), dir > 0 ? 180 : 0);
        // zieleń klubu: szpaler drzew za kortem i krzewy przy ogrodzeniu
        for (float x = cx - 15f; x <= cx + 15f; x += 7.5f) if (Tree(new Vector3(x, 0, cz + dir * 12.5f + (x > cx - 8 && x < cx + 8 ? dir * 6f : 0)), 7.2f + (float)treeRnd.NextDouble(), x * 11f)) rep.plotTrees++;
        for (float x = cx - 16f; x <= cx + 16f; x += 3.2f) if (Mathf.Abs(x - cx) > 2.5f && !IsHard(x, cz - dir * 9.9f, .5f)) Bush(new Vector3(x, 0, cz - dir * 9.9f), 1.2f, x * 5f);
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
        Top(ParkPath, AxisX - Half, AxisX + Half, z0, z0 + 5f, .035f);                 // wejście od osiedla B (na szerokość ulicy z chodnikami)
        Top(ParkPath, AxisX - 3f, AxisX + 3f, z0 + 5f, zc - r + .4f, .03f);
        Top(ParkPath, AxisX - 3f, AxisX + 3f, zc + r - .4f, z1 + .6f, .03f);
        Top(ParkPath, AxisX - 3.6f, AxisX + 3.6f, z1 + .6f, -100.05f, .035f);           // przez bramę aż do chodnika osiedla A (była trawa)
        Top(ParkPath, X0 + 4f, AxisX - r + .4f, zc - 2f, zc + 2f, .03f);
        Top(ParkPath, AxisX + r - .4f, X1 - 4f, zc - 2f, zc + 2f, .03f);
        Top(ParkPath, 28.4f, 31.6f, zc + 1.9f, -100.05f, .03f);                         // druga brama (x 28–32) do alei W-E
        Disc(ParkPath, new Vector3(AxisX, .046f, zc), r, 48);
        // słupki na końcu jezdni od osiedla B (ulica się kończy, dalej park)
        foreach (float bx in new[] { -2.6f, -.9f, .9f, 2.6f }) Cylinder(Frame, new Vector3(AxisX + bx, .035f, z0 + .5f), .1f, .9f, 10, true, true);
        // fontanna: kamienna misa, woda, kolumna z czaszą, płynąca woda (cząsteczki)
        var fc = new Vector3(AxisX, 0, zc);
        Cylinder(Stone, fc, 3.6f, .55f, 40, false, true);
        Ring(Stone, fc + Vector3.up * .55f, 3.05f, 3.6f, 40);
        Cylinder(Stone, fc + Vector3.up * .42f, 3.05f, .13f, 40, false, false, true);
        Disc(Water, fc + Vector3.up * .42f, 3.05f, 40);
        Cylinder(Stone, fc + Vector3.up * .42f, .45f, 1.25f, 20, true, false);
        Cylinder(Stone, fc + Vector3.up * 1.6f, 1.15f, .2f, 28, true, false);
        Disc(Water, fc + Vector3.up * 1.81f, 1.0f, 28);
        Cylinder(Steel, fc + Vector3.up * 1.81f, .07f, .14f, 10, true, false);
        Guard("woda fontanny", () => FountainWater(fc));
        // rabaty: pierścień przy misie (na placu) i pierścień wokół placu, z przerwami na aleje
        foreach (var (a0, a1) in new[] { (22f, 68f), (112f, 158f), (202f, 248f), (292f, 338f) })
            FlowerRing(fc, 4.3f, 5.3f, a0, a1, (int)a0, 0, 2);
        foreach (var (a0, a1) in new[] { (14f, 70f), (110f, 166f), (194f, 250f), (290f, 346f) })
            FlowerRing(fc, 9.5f, 11.2f, a0, a1, (int)a1 + 7, 3, 1, 0);
        // ławki wokół placu na przekątnych, przodem do fontanny, przy każdej kosz
        foreach (float deg in new[] { 45f, 135f, 225f, 315f })
        {
            float a = deg * Mathf.Deg2Rad; var p = fc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 7.4f;
            ParkBench(new Vector3(p.x, .046f, p.z), Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg);
            float ab = (deg + 24f) * Mathf.Deg2Rad; Bin(fc + new Vector3(Mathf.Cos(ab), 0, Mathf.Sin(ab)) * 7.4f + Vector3.up * .046f);   // kosz obok ławki, nie na niej
        }
        // aleja W-E: ławki naprzemiennie po obu stronach, latarnie, kosze, rabaty naprzeciw ławek
        for (float x = X0 + 14f; x < X1 - 10f; x += 18f)
        {
            if (Mathf.Abs(x - AxisX) < 14f) continue;
            bool northSide = ((int)((x - X0) / 18f)) % 2 == 0;
            ParkBench(new Vector3(x, .03f, zc + (northSide ? 3.0f : -3.0f)), northSide ? 180 : 0);
            ParkLamp(new Vector3(x + 9f, .03f, zc + (northSide ? -2.7f : 2.7f)), northSide ? 0 : 180);
            if ((++binTurn) % 2 == 0) Bin(new Vector3(x + 1.75f, .03f, zc + (northSide ? 2.75f : -2.75f)));
            float bz = northSide ? -1f : 1f;
            FlowerBed(x - 3.2f, x + 3.2f, zc + bz * 2.6f, zc + bz * 3.9f, (int)x + 200, northSide ? new[] { 0, 2 } : new[] { 1, 3 });
        }
        for (float z = z0 + 6f; z < z1 - 4f; z += 14f)
        {
            if (Mathf.Abs(z - zc) < 12f) continue;
            ParkLamp(new Vector3(AxisX - 3.7f, .03f, z), 90);
            ParkBench(new Vector3(AxisX + 3.8f, .03f, z + 7f), 270);
        }
        // rabaty przy bramach (A od wschodu bramy — od zachodu stoi stróżówka; B po obu stronach wejścia)
        FlowerBed(AxisX + 4.6f, AxisX + 10.5f, FenceA - 3.6f, FenceA - 1.3f, 501, 0, 2, 3);
        FlowerBed(AxisX - Half - 5.2f, AxisX - Half - .6f, z0 + 1.0f, z0 + 3.2f, 502, 1, 3);
        FlowerBed(AxisX + Half + .6f, AxisX + Half + 5.2f, z0 + 1.0f, z0 + 3.2f, 503, 0, 2);
        // plac zabaw: nawierzchnia EPDM, niskie drewniane ogrodzenie, huśtawka na ramach A, wieża ze zjeżdżalnią, piaskownica, bujak
        {
            float px = -72f, pz = -140f;
            var epdm = Plain("nawierzchnia EPDM zielona", new Color(.18f, .33f, .22f), .12f); var sand = Plain("piasek piaskownicy", new Color(.80f, .70f, .52f), .05f);
            var red = Plain("sprzęt zabaw czerwony", new Color(.62f, .14f, .10f), .5f, .05f); var yellow = Plain("sprzęt zabaw żółty", new Color(.85f, .62f, .12f), .5f, .05f);
            Top(epdm, px - 8f, px + 8f, pz - 6f, pz + 6f, .035f);
            // ogrodzenie z furtką od alei (północ)
            for (float x = px - 8f; x <= px + 8.01f; x += 1.6f) foreach (float z in new[] { pz - 6f, pz + 6f }) if (z < pz || Mathf.Abs(x - px) > 1.2f) Box(Deck, new Vector3(x, .45f, z), new Vector3(.09f, .9f, .09f), false);
            for (float z = pz - 6f; z <= pz + 6.01f; z += 1.6f) foreach (float x in new[] { px - 8f, px + 8f }) Box(Deck, new Vector3(x, .45f, z), new Vector3(.09f, .9f, .09f), false);
            foreach (float y in new[] { .3f, .75f })
            {
                BoxMinMax(Deck, px - 8f, px + 8f, y, y + .07f, pz - 6.03f, pz - 5.97f, false);
                BoxMinMax(Deck, px - 8f, px - 1.2f, y, y + .07f, pz + 5.97f, pz + 6.03f, false); BoxMinMax(Deck, px + 1.2f, px + 8f, y, y + .07f, pz + 5.97f, pz + 6.03f, false);
                BoxMinMax(Deck, px - 8.03f, px - 7.97f, y, y + .07f, pz - 6f, pz + 6f, false); BoxMinMax(Deck, px + 7.97f, px + 8.03f, y, y + .07f, pz - 6f, pz + 6f, false);
            }
            Collider(new Vector3(px, .45f, pz - 6f), new Vector3(16f, .9f, .1f)); Collider(new Vector3(px - 8f, .45f, pz), new Vector3(.1f, .9f, 12f)); Collider(new Vector3(px + 8f, .45f, pz), new Vector3(.1f, .9f, 12f));
            // huśtawka: dwie ramy A z pochylonych słupów, belka, dwa siedziska na łańcuchach
            float sz = pz - 2.8f;
            foreach (float sx in new[] { -5.6f, -1.4f })
                foreach (float lean in new[] { -1f, 1f })
                    OBoxQ(Steel, new Vector3(px + sx, 1.2f, sz + lean * .45f), new Vector3(.09f, 2.5f, .09f), Quaternion.Euler(lean * 20f, 0, 0));
            Box(Steel, new Vector3(px - 3.5f, 2.38f, sz), new Vector3(4.4f, .11f, .11f), false);
            foreach (float sx in new[] { -4.4f, -2.6f })
            {
                foreach (float cx in new[] { -.22f, .22f }) Box(Steel, new Vector3(px + sx + cx, 1.55f, sz), new Vector3(.025f, 1.6f, .025f), false);
                Box(red, new Vector3(px + sx, .72f, sz), new Vector3(.55f, .05f, .24f), false);
            }
            // wieża z daszkiem dwuspadowym, drabinka i zjeżdżalnia
            float tx = px + 3.6f, tz = pz - 1.0f;
            foreach (float ox in new[] { -.9f, .9f }) foreach (float oz in new[] { -.9f, .9f }) Box(Deck, new Vector3(tx + ox, 1.6f, tz + oz), new Vector3(.12f, 3.2f, .12f));
            Box(Deck, new Vector3(tx, 1.25f, tz), new Vector3(2.0f, .1f, 2.0f), false);
            foreach (float s in new[] { -1f, 1f }) OBoxQ(red, new Vector3(tx + s * .55f, 3.45f, tz), new Vector3(1.25f, .07f, 2.3f), Quaternion.Euler(0, 0, -s * 32f));
            BoxMinMax(Deck, tx - .95f, tx + .95f, 1.3f, 1.95f, tz - .97f, tz - .93f, false); BoxMinMax(Deck, tx + .93f, tx + .97f, 1.3f, 1.95f, tz - .95f, tz + .95f, false);
            for (int k = 0; k < 5; k++) Box(Deck, new Vector3(tx - .5f, .25f + k * .25f, tz + 1.15f + k * .02f), new Vector3(.6f, .05f, .08f), false);   // drabinka
            OBoxQ(yellow, new Vector3(tx - 2.0f, .68f, tz - .2f), new Vector3(2.7f, .05f, .62f), Quaternion.Euler(0, 0, 25f));                  // ześlizg
            foreach (float s in new[] { -.31f, .31f }) OBoxQ(yellow, new Vector3(tx - 2.0f, .78f, tz - .2f + s), new Vector3(2.7f, .2f, .04f), Quaternion.Euler(0, 0, 25f));
            // piaskownica z drewnianym obrzeżem
            float bx0 = px - 6.8f, bz0 = pz + 1.4f;
            Top(sand, bx0, bx0 + 3.6f, bz0, bz0 + 3.2f, .08f);
            BoxMinMax(Deck, bx0 - .12f, bx0 + 3.72f, 0, .3f, bz0 - .12f, bz0, false); BoxMinMax(Deck, bx0 - .12f, bx0 + 3.72f, 0, .3f, bz0 + 3.2f, bz0 + 3.32f, false);
            BoxMinMax(Deck, bx0 - .12f, bx0, 0, .3f, bz0, bz0 + 3.2f, false); BoxMinMax(Deck, bx0 + 3.6f, bx0 + 3.72f, 0, .3f, bz0, bz0 + 3.2f, false);
            // bujak na sprężynie
            Cylinder(Steel, new Vector3(px + .5f, .035f, pz + 2.6f), .07f, .45f, 10, true, false);
            OBoxQ(red, new Vector3(px + .5f, .62f, pz + 2.6f), new Vector3(.25f, .3f, .8f), Quaternion.identity);
            ParkBench(new Vector3(px - 2.5f, .035f, pz + 4.9f), 180); ParkBench(new Vector3(px + 3.5f, .035f, pz + 4.9f), 180);
        }
        // pergole nad aleją przy placu (drewno)
        foreach (float gx in new[] { AxisX - 26f, AxisX + 16f })
        {
            for (float x = gx; x <= gx + 10f; x += 2.5f) foreach (float side in new[] { -2.3f, 2.3f }) Box(Deck, new Vector3(x, 1.35f, zc + side), new Vector3(.18f, 2.7f, .18f));
            foreach (float side in new[] { -2.3f, 2.3f }) Box(Deck, new Vector3(gx + 5f, 2.78f, zc + side), new Vector3(10.6f, .16f, .2f), false);
            for (float x = gx - .2f; x <= gx + 10.2f; x += .6f) Box(Deck, new Vector3(x, 2.92f, zc), new Vector3(.08f, .12f, 5.4f), false);
        }
        // siłownia plenerowa (przed sadzeniem drzew, żeby drzewa jej nie zasłoniły)
        Guard("siłownia plenerowa", () => OutdoorGym(50f, -119.6f));
        group = "Park Centralny";
        // stoły piknikowe na trawnikach
        var picnicSpots = new[] { new Vector2(-80, -114), new Vector2(-48, -142), new Vector2(40, -113), new Vector2(75, -138) };
        foreach (var p in picnicSpots)
            if (picnic && Put(picnic, new Vector3(p.x, 0, p.y), 15f, .04f)) rep.tables++;

        // DRZEWA: szpalery wzdłuż alej (co 9 m), potem gaje na trawnikach (losowanie z minimalnym odstępem),
        // z polanami wokół stołów piknikowych i placu zabaw. Wysokości i korony różne.
        var rnd = new System.Random(2031);
        float H(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
        bool Clear(float x, float z, float m)
        {
            if (IsHard(x, z, m)) return false;
            if ((x - fc.x) * (x - fc.x) + (z - fc.z) * (z - fc.z) < 13.5f * 13.5f) return false;
            foreach (var p in picnicSpots) if ((x - p.x) * (x - p.x) + (z - p.y) * (z - p.y) < 5.5f * 5.5f) return false;
            if (x > -82f && x < -62f && z > -148.5f && z < -127f) return false;      // plac zabaw (widok z alei bez drzew)
            if (x > 47f && x < 69f && z > -121.5f && z < -103f) return false;        // siłownia plenerowa
            return true;
        }
        int before = treeSpots.Count;
        for (float x = X0 + 8f; x < X1 - 5f; x += 9f)
        {
            if (Mathf.Abs(x - AxisX) < 13f) continue;
            foreach (float side in new[] { -1f, 1f })
            {
                float tx = x + H(-.6f, .6f), tz = zc + side * H(6.0f, 6.8f);
                if (Clear(tx, tz, 1.0f) && !NearTree(tx, tz, 5.5f) && Tree(new Vector3(tx, 0, tz), H(7.5f, 9.8f), rnd.Next(360))) rep.parkTrees++;
            }
        }
        for (float z = z0 + 8f; z < z1 - 3f; z += 8f)
        {
            if (Mathf.Abs(z - zc) < 13f) continue;
            foreach (float side in new[] { -1f, 1f })
                if (Clear(AxisX + side * 6.8f, z, 1.0f) && !NearTree(AxisX + side * 6.8f, z, 5.5f) && Tree(new Vector3(AxisX + side * 6.8f, 0, z), H(7f, 9f), rnd.Next(360))) rep.parkTrees++;
        }
        for (int tries = 0, placed = 0; tries < 9000 && placed < 150; tries++)
        {
            float tx = H(X0 + 3.5f, X1 - 3.5f), tz = H(z0 + 3.5f, z1 - 2.5f);
            if (!Clear(tx, tz, 1.6f) || NearTree(tx, tz, 6.2f)) continue;
            if (Tree(new Vector3(tx, 0, tz), H(6.5f, 10.8f), rnd.Next(360))) { rep.parkTrees++; placed++; }
        }
        // krzewy: grupy przy części drzew, szpaler wzdłuż płotu osiedla A i płotów bocznych
        var parkTrees = treeSpots.Skip(before).ToList();
        for (int k = 0; k < parkTrees.Count; k += 2)
        {
            var t = parkTrees[k]; int n = 1 + rnd.Next(3);
            for (int q = 0; q < n; q++)
            {
                float a = H(0, Mathf.PI * 2), d = H(2.4f, 3.6f), bx = t.x + Mathf.Cos(a) * d, bz = t.z + Mathf.Sin(a) * d;
                if (Clear(bx, bz, .9f)) Bush(new Vector3(bx, 0, bz), H(1.3f, 2.1f), rnd.Next(360));
            }
        }
        for (float x = X0 + 2f; x < X1 - 2f; x += 3.1f)
        {
            float bz = FenceA - 1.5f;
            if (Clear(x, bz, .7f) && !NearTree(x, bz, 2.2f)) Bush(new Vector3(x, 0, bz), H(1.4f, 2.0f), rnd.Next(360));
        }
        foreach (float bx in new[] { X0 + 1.6f, X1 - 1.6f })
            for (float z = z0 + 2f; z < z1 - 1f; z += 3.2f)
                if (Clear(bx, z, .7f) && !NearTree(bx, z, 2.2f)) Bush(new Vector3(bx, 0, z), H(1.4f, 2.0f), rnd.Next(360));
        // kępy trawy przy alejach i u stóp drzew
        for (float x = X0 + 6f; x < X1 - 6f; x += 5.5f)
        {
            if (Mathf.Abs(x - AxisX) < 11f) continue;
            if (!IsHard(x, zc + 2.6f, .2f)) Tuft(new Vector3(x, 0, zc + 2.45f + (float)tuftRnd.NextDouble() * .4f));
            if (!IsHard(x + 2.7f, zc - 2.6f, .2f)) Tuft(new Vector3(x + 2.7f, 0, zc - 2.45f - (float)tuftRnd.NextDouble() * .4f));
        }
        foreach (var t in parkTrees.Where((q, i) => i % 3 == 0))
            for (int k = 0; k < 3; k++) Tuft(new Vector3(t.x + Mathf.Cos(k * 2.1f + t.x) * .9f, 0, t.z + Mathf.Sin(k * 2.1f + t.z) * .9f));
    }

    // Rura między dwoma punktami (drążki, poręcze, ramy sprzętu): pobocznica bez denek, normalne promieniste.
    static void Tube(Material m, Vector3 a, Vector3 b, float r, int seg = 10)
    {
        var bt = B(m); var axis = (b - a); float len = axis.magnitude; if (len < 1e-3f) return; axis /= len;
        var side = Vector3.Cross(axis, Mathf.Abs(axis.y) > .9f ? Vector3.right : Vector3.up).normalized; var up = Vector3.Cross(side, axis);
        float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        for (int k = 0; k < seg; k++)
        {
            float a0 = k * Mathf.PI * 2 / seg, a1 = (k + 1) * Mathf.PI * 2 / seg;
            Vector3 d0 = side * Mathf.Cos(a0) + up * Mathf.Sin(a0), d1 = side * Mathf.Cos(a1) + up * Mathf.Sin(a1);
            int i0 = bt.v.Count;
            var vs = new[] { a + d0 * r, a + d1 * r, b + d1 * r, b + d0 * r }; var ns = new[] { d0, d1, d1, d0 };
            var us = new[] { a0 * r / s, a1 * r / s, a1 * r / s, a0 * r / s }; var ws = new[] { 0f, 0f, len / s, len / s };
            for (int q = 0; q < 4; q++) { bt.v.Add(vs[q]); bt.n.Add(ns[q]); bt.t.Add(new Vector4(axis.x, axis.y, axis.z, 1)); bt.uv.Add(new Vector2(us[q], ws[q])); }
            bt.i.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
        }
        if (Mathf.Min(a.y, b.y) < .3f) { var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b); Hard(lo.x - r, hi.x + r, lo.z - r, hi.z + r); }
    }
    // Siłownia plenerowa w parku (jak na polskich osiedlach): nawierzchnia EPDM, zestaw street workout
    // (drążki na trzech wysokościach, poręcze do pompek, drabinka pozioma, ławka skośna do brzuszków),
    // cztery urządzenia na słupach (orbitrek, wioślarz, twister, prasa nożna), tablica z zasadami, ławki.
    static void OutdoorGym(float x0, float z0)
    {
        group = "Park Centralny / siłownia plenerowa";
        var green = Plain("siłownia plenerowa zieleń", new Color(.16f, .42f, .24f), .55f, .2f);
        var yellow = Plain("siłownia plenerowa żółć", new Color(.86f, .66f, .10f), .55f, .2f);
        var steel = Plain("stal ocynkowana", new Color(.62f, .64f, .66f), .6f, .7f);
        // kwadrat z kostki brukowej 16 x 16 m z obrzeżem — i tyle (sam sprzęt pod gołym niebem)
        var kostka = Pbr("kostka brukowa", "concrete_pavers_02", 1.8f, new Color(.90f, .90f, .91f));
        float x1 = x0 + 16f, z1 = z0 + 16f, y = .045f;
        Top(kostka, x0, x1, z0, z1, y);
        BoxMinMax(KerbStone, x0 - .1f, x1 + .1f, 0, .08f, z0 - .1f, z0, false); BoxMinMax(KerbStone, x0 - .1f, x1 + .1f, 0, .08f, z1, z1 + .1f, false);
        BoxMinMax(KerbStone, x0 - .1f, x0, 0, .08f, z0, z1, false); BoxMinMax(KerbStone, x1, x1 + .1f, 0, .08f, z0, z1, false);
        // street workout: rama z drążkami 1.6 / 2.0 / 2.4 m
        float sx = x0 + 1.5f, sz = z0 + 2f;
        float[] hs = { 1.6f, 2.0f, 2.4f };
        for (int k = 0; k <= 3; k++) Tube(green, new Vector3(sx + k * 1.4f, y, sz), new Vector3(sx + k * 1.4f, 2.6f, sz), .06f);
        for (int k = 0; k < 3; k++) Tube(steel, new Vector3(sx + k * 1.4f, hs[k], sz), new Vector3(sx + (k + 1) * 1.4f, hs[k], sz), .02f, 8);
        Collider(new Vector3(sx + 2.1f, 1.3f, sz), new Vector3(4.4f, 2.6f, .14f));
        // poręcze do pompek na poręczach (dipy)
        float px = x0 + 8f, pz = z0 + 1.6f;
        foreach (float dz in new[] { 0f, .55f })
        {
            foreach (float dx in new[] { 0f, 2f }) Tube(green, new Vector3(px + dx, y, pz + dz), new Vector3(px + dx, 1.2f, pz + dz), .05f);
            Tube(steel, new Vector3(px - .1f, 1.2f, pz + dz), new Vector3(px + 2.1f, 1.2f, pz + dz), .025f, 8);
        }
        Collider(new Vector3(px + 1f, .6f, pz + .27f), new Vector3(2.2f, 1.2f, .7f));
        // drabinka pozioma (małpi gaj)
        float lx = x0 + 1.5f, lz = z0 + 5.5f;
        foreach (float dz in new[] { 0f, .7f })
        {
            foreach (float dx in new[] { 0f, 3.6f }) Tube(green, new Vector3(lx + dx, y, lz + dz), new Vector3(lx + dx, 2.3f, lz + dz), .06f);
            Tube(green, new Vector3(lx, 2.3f, lz + dz), new Vector3(lx + 3.6f, 2.3f, lz + dz), .045f);
        }
        for (float dx = .3f; dx < 3.4f; dx += .4f) Tube(steel, new Vector3(lx + dx, 2.3f, lz), new Vector3(lx + dx, 2.3f, lz + .7f), .018f, 6);
        Collider(new Vector3(lx + 1.8f, 1.15f, lz + .35f), new Vector3(3.8f, 2.3f, .9f));
        // ławka skośna do brzuszków
        OBoxQ(yellow, new Vector3(x0 + 7.5f, .55f, z0 + 6.2f), new Vector3(.45f, .06f, 1.7f), Quaternion.Euler(-18f, 0, 0), true);
        Tube(green, new Vector3(x0 + 7.5f, y, z0 + 5.5f), new Vector3(x0 + 7.5f, .3f, z0 + 5.5f), .05f); Tube(green, new Vector3(x0 + 7.5f, y, z0 + 6.9f), new Vector3(x0 + 7.5f, .82f, z0 + 6.9f), .05f);
        Tube(steel, new Vector3(x0 + 7.25f, .42f, z0 + 7.05f), new Vector3(x0 + 7.75f, .42f, z0 + 7.05f), .03f, 8);
        // urządzenia na słupach: słup + ramiona + pedały/siedziska (żółte), co 2.4 m wzdłuż tylnej krawędzi
        for (int k = 0; k < 4; k++)
        {
            float ux = x0 + 9.6f + k * 1.7f, uz = z0 + 8.4f;
            Tube(green, new Vector3(ux, y, uz), new Vector3(ux, 1.7f, uz), .07f);
            Cylinder(green, new Vector3(ux, y, uz), .14f, .06f, 12, true, false);
            Tube(green, new Vector3(ux, 1.55f, uz), new Vector3(ux, 1.55f, uz - .55f), .04f);
            if (k == 0)   // orbitrek: dwa ramiona i płozy
            {
                foreach (float s2 in new[] { -.22f, .22f }) { Tube(steel, new Vector3(ux + s2, 1.55f, uz - .5f), new Vector3(ux + s2, .35f, uz - .75f), .025f, 8); OBox(yellow, new Vector3(ux + s2, .32f, uz - .85f), new Vector3(.14f, .04f, .36f), 0); }
            }
            else if (k == 1)   // wioślarz: siedzisko, podnóżek, drążek
            {
                OBox(yellow, new Vector3(ux, .45f, uz - .7f), new Vector3(.4f, .06f, .35f), 0); OBox(yellow, new Vector3(ux, .25f, uz - 1.25f), new Vector3(.45f, .04f, .2f), 0);
                Tube(steel, new Vector3(ux - .3f, 1.0f, uz - .45f), new Vector3(ux + .3f, 1.0f, uz - .45f), .025f, 8);
            }
            else if (k == 2)   // twister: obrotowy talerz i uchwyty
            {
                Cylinder(yellow, new Vector3(ux, .2f, uz - .6f), .3f, .05f, 16, true, false);
                Tube(steel, new Vector3(ux - .25f, 1.2f, uz - .3f), new Vector3(ux + .25f, 1.2f, uz - .3f), .025f, 8);
            }
            else   // prasa nożna: siedzisko z oparciem i płyta
            {
                OBox(yellow, new Vector3(ux, .5f, uz - .55f), new Vector3(.42f, .06f, .4f), 0); OBoxQ(yellow, new Vector3(ux, .8f, uz - .35f), new Vector3(.42f, .5f, .05f), Quaternion.Euler(-12f, 0, 0));
                OBoxQ(yellow, new Vector3(ux, .45f, uz - 1.15f), new Vector3(.4f, .35f, .04f), Quaternion.Euler(15f, 0, 0));
            }
            Collider(new Vector3(ux, .85f, uz - .5f), new Vector3(.7f, 1.7f, 1.2f));
        }
        // tablica z zasadami i ławki
        Tube(green, new Vector3(x1 - 1f, y, z0 + 1f), new Vector3(x1 - 1f, 2.2f, z0 + 1f), .05f);
        Sign("silownia", new Vector3(x1 - 1f, 1.9f, z0 + .93f), 2.0f, .5f, Vector3.back);
        ParkBench(new Vector3(x0 + 4f, .035f, z1 - .9f), 180); ParkBench(new Vector3(x0 + 11f, .035f, z0 - 1.2f), 0);
        rep.notes.Add("park: siłownia plenerowa (street workout + 4 urządzenia)");
    }

    // Płynąca woda fontanny: strumień w górę z dyszy, przelew z górnej czaszy (kurtyna kropel), rozbryzgi i kręgi na tafli.
    // Krople półprzezroczyste, mieszane alfą (bez dodawania światła — nic nie świeci).
    static Material PartMat(string key, string png, Color c)
    {
        if (mats.TryGetValue(key, out var have)) return have;
        string path = "Assets/Source/Claude/Osiedle/detail/" + png;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        var sh = Shader.Find("Particles/Standard Unlit"); bool std = sh;
        if (!sh) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (!imp || !sh) { rep.notes.Add("fontanna: brak " + (imp ? "shadera cząsteczek" : path)); mats[key] = null; return null; }
        imp.textureType = TextureImporterType.Default; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.sRGBTexture = true; imp.SaveAndReimport();
        var m = new Material(sh) { name = "Osiedle: " + key, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        if (std)
        {
            m.SetFloat("_Mode", 2f); m.SetFloat("_ColorMode", 0f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000; m.SetColor("_Color", c);
        }
        else m.SetColor("_TintColor", c * .5f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = 1f; return m;
    }
    static ParticleSystem Emitter(string name, Vector3 pos, Material mat, ParticleSystemRenderMode mode, int max)
    {
        var g = new GameObject(name); g.transform.SetParent(Group(group), false); g.transform.position = pos;
        var ps = g.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = mode;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = .045f; r.lengthScale = 1.6f; }
        var main = ps.main; main.loop = true; main.prewarm = true; main.playOnAwake = true; main.duration = 4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = max; main.gravityModifier = 1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                   new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .1f), new GradientAlphaKey(.85f, .75f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(gr);
        return ps;
    }
    static void FountainWater(Vector3 fc)
    {
        var drop = PartMat("woda fontanny krople", "kropla.png", new Color(.86f, .93f, 1f, .62f));
        var ring = PartMat("woda fontanny kręgi", "fala.png", new Color(.9f, .96f, 1f, .5f));
        if (!drop) return;
        // strumień: 6.6 m/s w górę (szczyt ok. 2.2 m nad dyszą), opada do górnej czaszy
        var jet = Emitter("fontanna: strumień", fc + Vector3.up * 1.95f, drop, ParticleSystemRenderMode.Stretch, 700);
        { var m = jet.main; m.startLifetime = new ParticleSystem.MinMaxCurve(1.15f, 1.35f); m.startSpeed = new ParticleSystem.MinMaxCurve(6.3f, 6.9f); m.startSize = new ParticleSystem.MinMaxCurve(.07f, .13f); m.startColor = new Color(1, 1, 1, .8f); }
        { var e = jet.emission; e.rateOverTime = 420f; }
        { var s = jet.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Cone; s.angle = 3.2f; s.radius = .035f; s.rotation = new Vector3(-90f, 0, 0); }
        // przelew z górnej czaszy: kurtyna kropel na całym obwodzie
        var curtain = Emitter("fontanna: przelew", fc + Vector3.up * 1.79f, drop, ParticleSystemRenderMode.Stretch, 1400);
        { var m = curtain.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .56f); m.startSpeed = new ParticleSystem.MinMaxCurve(.35f, .55f); m.startSize = new ParticleSystem.MinMaxCurve(.05f, .09f); m.startColor = new Color(1, 1, 1, .7f); }
        { var e = curtain.emission; e.rateOverTime = 1100f; }
        { var s = curtain.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = 1.17f; s.radiusThickness = 0f; s.arc = 360f; s.rotation = new Vector3(90f, 0, 0); }
        // rozbryzgi: dolna misa (pod kurtyną) i górna czasza (pod strumieniem)
        foreach (var (y, rad, rate) in new[] { (.45f, 1.42f, 260f), (1.84f, .42f, 120f) })
        {
            var sp = Emitter("fontanna: rozbryzgi", fc + Vector3.up * y, drop, ParticleSystemRenderMode.Billboard, 300);
            var m = sp.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.22f, .36f); m.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f); m.startSize = new ParticleSystem.MinMaxCurve(.05f, .11f); m.startColor = new Color(1, 1, 1, .5f);
            var e = sp.emission; e.rateOverTime = rate;
            var s = sp.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = rad; s.radiusThickness = y > 1 ? 1f : .12f; s.rotation = new Vector3(90f, 0, 0);
            var v = sp.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(0f, 0f); v.y = new ParticleSystem.MinMaxCurve(1.1f, 1.8f); v.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }
        // kręgi na tafli (płaskie, rosną i znikają)
        if (ring)
            foreach (var (y, rad, thick, size, rate) in new[] { (.432f, 3.0f, .55f, 1.6f, 9f), (1.822f, .9f, 1f, .7f, 6f) })
            {
                var rp = Emitter("fontanna: kręgi", fc + Vector3.up * y, ring, ParticleSystemRenderMode.HorizontalBillboard, 40);
                var m = rp.main; m.startLifetime = 1.6f; m.startSpeed = 0f; m.startSize = size; m.gravityModifier = 0f; m.startColor = new Color(1, 1, 1, .55f);
                var e = rp.emission; e.rateOverTime = rate;
                var s = rp.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Circle; s.radius = rad; s.radiusThickness = thick; s.rotation = new Vector3(90f, 0, 0);
                var sz = rp.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .25f, 1f, 1f));
            }
        rep.notes.Add("fontanna: strumień, przelew, rozbryzgi i kręgi (cząsteczki)");
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
            if (!tr) continue;                                   // mógł zniknąć razem z rodzicem usuniętej korony
            var c = tr.bounds.center; if (dom07.Contains(new Vector3(c.x, 1f, c.z))) continue;
            var mine = crowns.Where(k => k && Mathf.Abs(k.bounds.center.x - c.x) < 2.2f && Mathf.Abs(k.bounds.center.z - c.z) < 2.2f).ToList();
            if (mine.Count == 0) continue;
            float top = mine.Max(k => k.bounds.max.y);
            foreach (var k in mine) if (k) UnityEngine.Object.DestroyImmediate(k.gameObject);   // nie pakować ukrytych
            if (tr) UnityEngine.Object.DestroyImmediate(tr.gameObject);
            crowns.RemoveAll(k => !k);
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
        imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp; imp.maxTextureSize = 2048;   // Master V4: 4K nigdy
        imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var shader = Shader.Find("Skybox/Panoramic"); if (!shader || !tex) { rep.notes.Add("brak shadera Skybox/Panoramic"); return; }
        var m = new Material(shader) { name = "Osiedle: niebo z chmurami" };
        m.SetTexture("_MainTex", tex); m.SetFloat("_Mapping", 1); m.SetFloat("_ImageType", 0); m.SetFloat("_Exposure", 1.05f); m.SetFloat("_Rotation", 40f);
        AssetDatabase.CreateAsset(m, Out + "/niebo.mat");
        RenderSettings.skybox = m;
        // odbicia nieba w szkle, wodzie i gładkich materiałach: kopia panoramy jako cubemapa (bez wypalania światła)
        const string cubePath = "Assets/Source/Claude/Osiedle/sky/niebo_odbicia.jpg";
        if (!File.Exists(cubePath)) File.Copy(path, cubePath);
        AssetDatabase.ImportAsset(cubePath, ImportAssetOptions.ForceSynchronousImport);
        var ci = AssetImporter.GetAtPath(cubePath) as TextureImporter;
        if (ci)
        {
            ci.textureShape = TextureImporterShape.TextureCube; ci.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            ci.sRGBTexture = true; ci.mipmapEnabled = true; ci.maxTextureSize = 512; ci.textureCompression = TextureImporterCompression.Compressed;
            ci.SaveAndReimport();
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
            if (cube) { RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture = cube; RenderSettings.reflectionIntensity = 1f; rep.notes.Add("odbicia: cubemapa nieba 512"); }
            else rep.notes.Add("odbicia: cubemapa nie powstała");
        }
        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) cam.clearFlags = CameraClearFlags.Skybox;
        rep.notes.Add("niebo: Skybox/Panoramic 2K");
    }

    // Światło i kolor dnia: cieplejsze słońce nieco niżej (dłuższe, wyraźniejsze cienie), jaśniejsze otoczenie od ziemi,
    // lekka mgła powietrzna w oddali (głębia zamiast płaskiego tła), ostrzejsze cienie dalej od kamery.
    static void Daylight()
    {
        var sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun)
        {
            sun.intensity = 1.24f; sun.color = new Color(1f, .955f, .875f); sun.shadowStrength = .8f;
            sun.transform.rotation = Quaternion.Euler(43f, -35f, 0f); sun.shadows = LightShadows.Soft;
        }
        else rep.notes.Add("światło: brak słońca w scenie");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.60f, .68f, .80f); RenderSettings.ambientEquatorColor = new Color(.50f, .53f, .50f); RenderSettings.ambientGroundColor = new Color(.30f, .28f, .23f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0042f; RenderSettings.fogColor = new Color(.74f, .80f, .87f);
        QualitySettings.shadowDistance = 60f; QualitySettings.shadowResolution = ShadowResolution.High; QualitySettings.shadowCascades = 2;
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 1f);
        rep.notes.Add("światło: słońce 1.24 ciepłe, mgła exp2 0.0042, cienie 60 m High");
    }

    // Osiedle A: zieleń na działkach starych domów (te same zasady co w B). Wolne miejsce sprawdzane promieniem
    // w dół: wszystko, co wystaje ponad trawnik (dom, płot, ścieżka, taras), odrzuca miejsce. Ścieżki i tarasy
    // dostają na chwilę kolizje siatki tylko do tego sprawdzenia.
    static void GreenA(Transform estate, Transform physics)
    {
        group = "Osiedle A / zieleń";
        var temp = new List<MeshCollider>();
        foreach (var r in estate.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.enabled || r.name == "EstateMesh_273" || r.name == "EstateMesh_274") continue;
            var b = r.bounds; if (b.max.y > .35f || b.size.y > .35f) continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || r.GetComponent<Collider>()) continue;
            try { var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; temp.Add(mc); } catch { }
        }
        Physics.SyncTransforms();
        bool Free(float x, float z, float m)
        {
            if (IsHard(x, z, m)) return false;
            for (int k = -1; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f, d = k < 0 ? 0 : m;
                if (Physics.Raycast(new Vector3(x + Mathf.Cos(a) * d, 12f, z + Mathf.Sin(a) * d), Vector3.down, out var hit, 14f, ~0, QueryTriggerInteraction.Ignore) && hit.point.y > .03f) return false;
            }
            return true;
        }
        var rnd = new System.Random(707);
        float H(float lo, float hi) => lo + (float)rnd.NextDouble() * (hi - lo);
        int trees0 = rep.plotTrees;
        foreach (var h in Houses)
        {
            float xu = -h.x, fxu = -(h.x < 0 ? h.x + 11.5f : h.x - 11.5f), inward = Mathf.Sign(xu - fxu), back = fxu + inward * 23f;
            // drzewa: do trzech na działkę, z dala od domu i płotów
            for (int t = 0, placed = 0; t < 120 && placed < 4; t++)
            {
                float x = H(Mathf.Min(fxu, back) + 2f, Mathf.Max(fxu, back) - 2f), z = H(h.z - h.hd + 2f, h.z + h.hd - 2f);
                if (!Free(x, z, 1.9f) || NearTree(x, z, 5f)) continue;
                if (Tree(new Vector3(x, 0, z), H(6.5f, 9f), rnd.Next(360))) { placed++; rep.plotTrees++; }
            }
            // krzewy wzdłuż płotów bocznych
            foreach (float sz in new[] { h.z - h.hd + 1.1f, h.z + h.hd - 1.1f })
                for (float x = Mathf.Min(fxu, back) + 2f; x < Mathf.Max(fxu, back) - 1.5f; x += 2.6f)
                    if (Free(x, sz, .7f) && !NearTree(x, sz, 2f)) Bush(new Vector3(x, 0, sz), H(1.3f, 1.9f), rnd.Next(360));
            // rabaty wzdłuż płotu od ulicy (po obu stronach furtki)
            float bx0 = fxu + inward * .7f, bx1 = fxu + inward * 1.9f;
            foreach (var (za, zb) in new[] { (h.z - h.hd + .9f, h.z - 2.9f), (h.z + 2.9f, h.z + h.hd - .9f) })
            {
                float xa = Mathf.Min(bx0, bx1), xb = Mathf.Max(bx0, bx1);
                if (zb - za > 2f && Free(xa + .6f, za + .3f, .25f) && Free(xb - .6f, zb - .3f, .25f) && Free((xa + xb) * .5f, (za + zb) * .5f, .3f))
                    FlowerBed(xa, xb, za, zb, (int)(h.z * 10 + za), 0, 2, 1);
            }
            // kępy traw: rzadko na trawniku, gęściej przy płotach; plama kwiatów w ogrodzie
            for (float x = Mathf.Min(fxu, back) + .8f; x < Mathf.Max(fxu, back) - .8f; x += 2.24f)
                for (float z = h.z - h.hd + .8f; z < h.z + h.hd - .8f; z += 2.24f)
                {
                    float px = x + H(0, 2.2f), pz = z + H(0, 2.2f);
                    if (Free(px, pz, .3f)) Tuft(new Vector3(px, 0, pz), false, Mathf.Abs(pz - h.z) > h.hd - 1.5f ? 1.2f : 1f);
                }
            for (int t = 0; t < 2; t++) { float px = H(Mathf.Min(fxu, back) + 3f, Mathf.Max(fxu, back) - 3f), pz = H(h.z - h.hd + 2f, h.z + h.hd - 2f); if (Free(px, pz, 1.2f)) FlowerPatch(new Vector3(px, 0, pz), .8f, 6); }
        }
        // szpaler drzew wzdłuż ulicy głównej osiedla A (pas trawy między chodnikiem a płotami działek)
        for (float z = -8f; z < 95f; z += 11f)
            foreach (float x in new[] { -11.5f, 11.5f })
                if (Free(x, z, 2.2f) && !NearTree(x, z, 6f) && Tree(new Vector3(x, 0, z), H(7.5f, 9f), rnd.Next(360))) rep.plotTrees++;
        foreach (var mc in temp) if (mc) UnityEngine.Object.DestroyImmediate(mc);
        rep.notes.Add("osiedle A: " + (rep.plotTrees - trees0) + " nowych drzew na działkach i przy ulicy, krzewy i rabaty");
    }

    // Stare domy osiedla A (bez wnętrz): przezroczyste szyby na działkach -> pokój za szybą.
    static void FakeRoomsA(Transform estate)
    {
        var glass = Glass; if (!glass || glass.shader.name != "Braxhorn/OsiedleOkno") return;
        int n = 0;
        foreach (var r in estate.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.enabled) continue;
            var b = r.bounds; bool inPlot = false;
            foreach (var h in Houses) { float xu = -h.x; if (Mathf.Abs(b.center.x - xu) < 12f && Mathf.Abs(b.center.z - h.z) < h.hd + .5f) inPlot = true; }
            if (!inPlot) continue;
            var ms = r.sharedMaterials; bool changed = false;
            for (int k = 0; k < ms.Length; k++)
            {
                var m = ms[k]; if (!m) continue;
                bool transparent = m.renderQueue > 2450 || m.name.ToLowerInvariant().Contains("glass") || m.name.ToLowerInvariant().Contains("szk");
                if (transparent) { ms[k] = glass; changed = true; }
            }
            if (changed) { r.sharedMaterials = ms; r.shadowCastingMode = ShadowCastingMode.Off; n++; }
        }
        rep.fakeRoomWindowsA = n;
        rep.notes.Add("osiedle A: szyby z pokojami w " + n + " siatkach starych domów");
    }

    // Stare drzewa Verdant z remontu R2 (Codex) w osiedlu A: ten sam ciemny liść — ten sam shader korony co nowe drzewa.
    static void FixOldLeaves(Transform estate)
    {
        int n = 0;
        foreach (var r in estate.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.sharedMaterial || !r.sharedMaterial.name.StartsWith("estate_lit_194")) continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            var m = LeafMat("drzewo A " + r.name, r.sharedMaterial, mf.sharedMesh.bounds, .1182f, .0158f, .85f, new Color(.30f, .46f, .15f), new Color(.40f, .49f, .17f));
            if (m) { r.sharedMaterial = m; n++; }
        }
        rep.notes.Add("stare korony Verdant w A: " + n + " na nowym shaderze liści");
    }

    // Krok dodatkowy: błąd w nim nie przerywa budowy reszty osiedla (trafia do raportu).
    static void Guard(string what, Action step)
    {
        var g = group;
        try { step(); }
        catch (Exception e) { rep.notes.Add("BŁĄD w kroku " + what + ": " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_OSIEDLE krok " + what + ": " + e); }
        group = g;
    }
    // ---------------- centrum sportowe B: siłownia BRAXHORN i BRAXHORN Nutrition (kopie z miasta) ----------------
    // Siłownia: ten sam oryginalny model (braxhorn-original-gym.glb, wypalone światło, materiały BakedPremium
    // z buildu siłowni) i te same kolizje (gym-collisions.json); bez zaułka za halą. Na zewnątrz nowa bryła:
    // ściany z klinkieru, dach, front z trzema wejściami i szyldem. Sklep: zestaw modeli właściciela
    // (NutritionKit.glb) w nowym układzie (przemeblowany), z muralem, scalony do kilku siatek.
    [Serializable] sealed class GymBox { public float minX, maxX, minZ, maxZ, top, bottom; public string tag; }
    [Serializable] sealed class GymCircle { public float x, z, r, top, bottom; }
    [Serializable] sealed class GymWorld { public GymBox[] boxes; public GymCircle[] circles; }
    static int mergeN;
    static Transform MergeStatic(Transform root, string name, float cull)
    {
        var byMat = new Dictionary<Material, List<CombineInstance>>();
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mf.sharedMesh) continue;
            if (!mf.sharedMesh.isReadable) { rep.notes.Add("scalanie " + name + ": siatka nieczytelna — zostaje bez scalania"); return root; }
            var ms = mr.sharedMaterials;
            for (int s = 0; s < mf.sharedMesh.subMeshCount && s < ms.Length; s++)
            {
                if (!ms[s]) continue;
                if (!byMat.TryGetValue(ms[s], out var l)) byMat[ms[s]] = l = new List<CombineInstance>();
                l.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = mf.transform.localToWorldMatrix });
            }
        }
        var parent = new GameObject(name).transform; parent.SetParent(Group(group), false);
        var rs = new List<Renderer>();
        foreach (var kv in byMat)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, name = name + " / " + kv.Key.name };
            mesh.CombineMeshes(kv.Value.ToArray(), true, true);
            AssetDatabase.CreateAsset(mesh, Out + "/scal_" + (mergeN++) + ".asset");
            var g = new GameObject(kv.Key.name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            rs.Add(r); rep.meshes++; rep.triangles += (int)(mesh.GetIndexCount(0) / 3);
        }
        if (rs.Count > 0) { var lod = parent.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs.ToArray()) }); lod.RecalculateBounds(); }
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        return parent;
    }

    // Siłownia BRAXHORN w osiedlu A, za kasynem (decyzja właściciela 31.10, wariant 3): pusty trawnik x 60–112, z -100…-58
    // (w geometrii osiedla 0 obiektów i 0 dróg). Hala w orientacji oryginału: jedyne wejście hali to drzwi w ścianie
    // „tylnej” modelu (x 5.2..6.8, z 11.0..11.6, zawias (6.8, 11.56), otwarte = -98° jak w grze) — patrzą na północ,
    // na plac przy tylnym wyjściu kasyna. Zaułek za drzwiami (model z > 11.75) odcięty, drzwi stoją otwarte
    // (kolizja skrzydła obrócona razem z nim). „Przód” modelu (z -16.6, przerwy zasłonięte blokami 8.5 m) — od płotu parku.
    // Świat = (gx + x_modelu_Unity, gz + z_modelu); x_modelu_Unity = -x z pliku three.js. Tymczasowo: kopia oryginalnej
    // hali; podmiana na prefab Fable (dokument PREFABY) w tym samym miejscu.
    static float gymDoorX, gymDoorZ;
    static void GymA(float gx, float frontFace)
    {
        group = "Osiedle A / siłownia BRAXHORN";
        const float t = .3f, H = 9.9f, zCut = 11.75f;
        float gz = frontFace - t - zCut;                            // frontFace = zewnętrzna ściana od drzwi (północ)
        float hx0 = gx - 21.1f, hx1 = gx + 21.1f, hzS = gz + zCut, hzP = gz - 16.7f;   // hzS: ściana z drzwiami, hzP: od parku
        float dX = gx + 6.0f;                                       // oś drzwi w świecie (model x 6.0)
        gymDoorX = dX; gymDoorZ = frontFace;
        const string gymPath = "Assets/Source/braxhorn-original-gym.glb";
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(gymPath) ?? AssetDatabase.LoadAllAssetsAtPath(gymPath).OfType<GameObject>().FirstOrDefault();
        if (!src) rep.notes.Add("siłownia A: brak " + gymPath);
        else
        {
            var g = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false);
            g.name = "Siłownia BRAXHORN (kopia oryginału, osiedle A)";
            g.transform.position = new Vector3(gx, 0, gz); g.transform.rotation = Quaternion.identity;
            // drzwi otwarte: części Gym_Mesh_24..31 na zawiasie (model 6.8, 0, 11.56), obrót -98° jak w grze
            var hinge = new GameObject("drzwi (otwarte)").transform; hinge.SetParent(g.transform, false); hinge.localPosition = new Vector3(6.8f, 0, 11.56f);
            int doorParts = 0;
            for (int i = 24; i <= 31; i++)
            {
                var part = g.GetComponentsInChildren<Transform>(true).FirstOrDefault(q => q.name == "Gym_Mesh_" + i);
                if (part) { part.SetParent(hinge, true); doorParts++; }
            }
            hinge.localRotation = Quaternion.Euler(0, -98f, 0);
            int dropped = 0;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true).ToList())
                if (r && r.bounds.min.z > hzS - .05f && !r.transform.IsChildOf(hinge)) { UnityEngine.Object.DestroyImmediate(r.gameObject); dropped++; }
            CutVolumes(g.transform, null, new List<Bounds> { MinMax(gx - 30f, gx + 30f, -3f, 12f, hzS + .05f, gz + 30f) });
            int conv = 0;
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++)
                {
                    var m = ms[k]; if (!m || !m.name.Contains("_baked") || m.renderQueue >= 2450) continue;
                    var baked = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/" + new string(m.name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray()) + ".mat");
                    if (baked) { ms[k] = baked; conv++; }
                }
                r.sharedMaterials = ms; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
            var gw = File.Exists("Assets/Source/gym-collisions.json") ? JsonUtility.FromJson<GymWorld>(File.ReadAllText("Assets/Source/gym-collisions.json")) : null;
            int cols = 0;
            if (gw != null)
            {
                foreach (var b in gw.boxes ?? new GymBox[0])
                {
                    if (b.top <= b.bottom || b.maxX <= b.minX || b.maxZ <= b.minZ || b.minZ > zCut) continue;
                    float ux = -(b.minX + b.maxX) * .5f, uz0 = b.minZ, uz1 = Mathf.Min(b.maxZ, zCut);
                    if (b.tag == "door")
                    {
                        // skrzydło otwarte: środek obrócony wokół zawiasu o -98°, kolizja obrócona tak samo
                        var lc = Quaternion.Euler(0, -98f, 0) * new Vector3(ux - 6.8f, 0, (uz0 + uz1) * .5f - 11.56f) + new Vector3(6.8f, (b.top + b.bottom) * .5f, 11.56f);
                        var dc = new GameObject("kolizja drzwi (otwarte)"); dc.transform.SetParent(colliders, false);
                        dc.transform.position = g.transform.TransformPoint(lc); dc.transform.rotation = g.transform.rotation * Quaternion.Euler(0, -98f, 0);
                        dc.AddComponent<BoxCollider>().size = new Vector3(b.maxX - b.minX, b.top - b.bottom, uz1 - uz0); cols++;
                        continue;
                    }
                    Collider(new Vector3(gx + ux, (b.top + b.bottom) * .5f, gz + (uz0 + uz1) * .5f), new Vector3(b.maxX - b.minX, b.top - b.bottom, uz1 - uz0)); cols++;
                }
                foreach (var c in gw.circles ?? new GymCircle[0])
                {
                    if (c.z > zCut || c.top <= c.bottom) continue;
                    Collider(new Vector3(gx - c.x, (c.top + c.bottom) * .5f, gz + c.z), new Vector3(c.r * 2, c.top - c.bottom, c.r * 2)); cols++;
                }
            }
            rep.notes.Add("siłownia A: kopia oryginału (drzwi na północ, x " + dX.ToString("0.0") + "), " + doorParts + "/8 części drzwi otwartych, "
                          + conv + " materiałów wypalonych, " + cols + " kolizji, bez zaułka: " + dropped + " siatek");
        }
        // bryła zewnętrzna: klinkier, cokół, attyka, dach; od północy jedno wejście w osi drzwi hali, daszek, szyld
        float fz0 = hzS, fz1 = hzS + t;                                   // ściana z drzwiami (zewnętrzna twarz = frontFace)
        Hard(hx0 - t, hx1 + t, hzP - t, fz1);
        BoxMinMax(Clinker, hx0 - t, hx0, 0, H, hzP - t, fz1, true); BoxMinMax(Clinker, hx1, hx1 + t, 0, H, hzP - t, fz1, true);
        BoxMinMax(Clinker, hx0 - t, hx1 + t, 0, H, hzP - t, hzP, true);
        const float dw = .9f, dh = 3.1f;                                  // otwór: drzwi hali 1.6 m + 10 cm z każdej strony
        BoxMinMax(Clinker, hx0 - t, dX - dw, 0, H, fz0, fz1, true); BoxMinMax(Clinker, dX + dw, hx1 + t, 0, H, fz0, fz1, true);
        BoxMinMax(Clinker, dX - dw, dX + dw, dh, H, fz0, fz1, false);
        BoxMinMax(Concrete, dX - dw, dX + dw, 0, .02f, fz0 - .3f, fz1 + .05f, false);                       // próg (między ścianą hali a obudową)
        BoxMinMax(Frame, dX - dw - .08f, dX + dw + .08f, dh - .05f, dh + .08f, fz0, fz1 + .05f, false);      // nadproże
        foreach (float sx in new[] { dX - dw - .08f, dX + dw }) BoxMinMax(Frame, sx, sx + .08f, 0, dh, fz0, fz1 + .05f, false);   // ościeżnica
        BoxMinMax(Frame, dX - 1.6f, dX + 1.6f, 3.3f, 3.42f, fz1, fz1 + 1.4f, false, true);                    // daszek
        BoxMinMax(Plinth, hx0 - t - .04f, dX - dw - .08f, 0, .5f, fz1, fz1 + .04f, false); BoxMinMax(Plinth, dX + dw + .08f, hx1 + t + .04f, 0, .5f, fz1, fz1 + .04f, false);
        BoxMinMax(Frame, hx0 - t - .1f, hx1 + t + .1f, H, H + .25f, hzP - t - .1f, fz1 + .1f, false, true);
        BoxMinMax(Gravel, hx0, hx1, H - .2f, H - .1f, hzP, hzS, false);
        BoxMinMax(Concrete, hx0, hx1, H - .24f, H - .2f, hzP, hzS, false, true);          // strop od spodu (gdyby hala nie miała sufitu)
        for (float x = hx0 + 3f; x < hx1 - 2f; x += 6f) if (Mathf.Abs(x - dX) > 2.2f && Mathf.Abs(x - gx) > 6.4f) BoxMinMax(Plinth, x - .25f, x + .25f, 0, H, fz1, fz1 + .2f, false);   // pilastry (nie przez szyld 12 m)
        Sign("braxhorn", new Vector3(gx, 6.6f, fz1 + .12f), 12f, 3f, Vector3.forward);
        // plac przed wejściem: od ściany do fartucha kasyna (z -61.5) i na zachód do ścieżki osiedla A (x 55)
        Top(Slabs, 55f, hx1 + t, fz1, -61.5f, .05f);
        Planter(new Vector3(dX - 3.2f, .05f, fz1 + .8f), 2.2f, .7f, 701, 1, 0); Planter(new Vector3(dX + 3.2f, .05f, fz1 + .8f), 2.2f, .7f, 702, 1, 0);
        foreach (float bx in new[] { gx - 14f, gx - 8f }) Bench(new Vector3(bx, .05f, -62.4f), 180f);
        // od parku i z boków: pas krzewów (pas do płotu 6.9 m — korona drzewa weszłaby w ścianę, więc bez drzew)
        for (float x = hx0 + 1.5f; x < hx1 - 1f; x += 3.4f) if (!IsHard(x, hzP - t - 2.4f, .6f)) Bush(new Vector3(x, 0, hzP - t - 2.4f), 1.5f + (float)treeRnd.NextDouble() * .5f, x * 11f);
        for (float z = hzP + 1.5f; z < fz1 - 2f; z += 3.6f) if (!IsHard(hx1 + t + 2.4f, z, .6f)) Bush(new Vector3(hx1 + t + 2.4f, 0, z), 1.4f + (float)treeRnd.NextDouble() * .5f, z * 13f);
        rep.notes.Add("siłownia A: hala " + (hx1 - hx0 + 2 * t).ToString("0.0") + " × " + (fz1 - hzP + t).ToString("0.0") + " m, drzwi (" + dX.ToString("0.0") + ", " + frontFace.ToString("0.0") + ")");
    }

    static GameObject nutriKit;
    static Transform KitPart(string name, Transform parent, Vector3 at, float yaw)
    {
        var o = nutriKit ? nutriKit.GetComponentsInChildren<Transform>(true).FirstOrDefault(q => q.name == name) : null;
        if (!o) return null;
        var c = (GameObject)UnityEngine.Object.Instantiate(o.gameObject, parent, false);
        c.transform.position = at; c.transform.rotation = Quaternion.Euler(0, yaw, 0);
        return c.transform;
    }
    // BRAXHORN Nutrition jako lokal w pierzei obok Shake Baru (dokument PREFABY): zestaw sklepu z miasta (NutritionKit.glb),
    // ten sam mural; różni się tylko układem (przemeblowanie) i kolorem ścian (szałwia zamiast bieli).
    // Lokal: a..b (ok. 12 m), zf (witryna od ulicy) .. zb (tył), posadzka FY, sufit H0 - 0.35.
    static void NutritionInterior(float a, float b, float zf, float zb, float H0, float FY)
    {
        if (!nutriKit) nutriKit = Model("nutrition_kit");
        float cx = (a + b) * .5f, ceilY = H0 - .35f;
        var wall = Pbr("ściany Nutrition szałwia", "painted_plaster_wall", 2.2f, new Color(.74f, .82f, .76f));
        BoxMinMax(wall, a, a + .02f, FY, ceilY, zf + .05f, zb, false); BoxMinMax(wall, b - .02f, b, FY, ceilY, zf + .05f, zb, false);
        BoxMinMax(wall, a, b, FY, ceilY, zb - .02f, zb, false);
        // mural właściciela na prawej ścianie, za ladą w głąb lokalu
        var muralTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/Claude/Osiedle/models/nutrition_kit/nutrition-mural.jpg");
        if (muralTex)
        {
            var mm = new Material(Shader.Find("Standard")) { name = "Osiedle: mural Nutrition", mainTexture = muralTex }; mm.SetFloat("_Glossiness", .2f);
            AssetDatabase.CreateAsset(mm, Out + "/mural_nutrition.mat");
            int sw = muralTex.width, sh = muralTex.height;
            var mimp = AssetImporter.GetAtPath("Assets/Source/Claude/Osiedle/models/nutrition_kit/nutrition-mural.jpg") as TextureImporter;
            if (mimp) mimp.GetSourceTextureWidthAndHeight(out sw, out sh);                   // proporcje z pliku (import NPOT skaluje do 2048×1024)
            float asp = sw / Mathf.Max(1f, (float)sh);
            float y0 = FY + 1.0f, mh = Mathf.Min((zb - zf - 3f) / asp, ceilY - .25f - y0), mw = mh * asp, mc = zf + 4.1f + mw * .5f, mx = b - .03f;
            var bb = B(mm); int k = bb.v.Count;
            var p = new[] { new Vector3(mx, y0, mc + mw * .5f), new Vector3(mx, y0, mc - mw * .5f), new Vector3(mx, y0 + mh, mc - mw * .5f), new Vector3(mx, y0 + mh, mc + mw * .5f) };
            var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            for (int q = 0; q < 4; q++) { bb.v.Add(p[q]); bb.n.Add(Vector3.left); bb.t.Add(new Vector4(0, 0, -1, 1)); bb.uv.Add(uv[q]); }
            bb.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            tile[mm] = 1f;
            rep.notes.Add("Nutrition: mural " + mw.ToString("0.00") + " × " + mh.ToString("0.00") + " m, z " + (mc - mw * .5f).ToString("0.0") + "…" + (mc + mw * .5f).ToString("0.0"));
        }
        else rep.notes.Add("Nutrition: brak muralu");
        if (!nutriKit) { rep.notes.Add("Nutrition: brak NutritionKit.glb — sam lokal"); return; }
        var shop = new GameObject("Nutrition wyposażenie").transform; shop.SetParent(Group(group), false);
        string[] prod = { "WheyTub", "CreatineJar", "Shaker" };
        int bays = 0, items = 0;
        void Bay(Vector3 at, float yaw, int variant)
        {
            if (!KitPart("ShelvingBay", shop, at, yaw)) return; bays++;
            var q = Quaternion.Euler(0, yaw, 0);
            for (int row = 0; row < 4; row++) for (int col = 0; col < 3; col++)
            {
                var local = new Vector3(-.5f + col * .5f, .364f + row * .51f, .03f);
                if (KitPart(prod[((row + variant + col) % 3 + 3) % 3], shop, at + q * local, yaw + (col - 1) * 6f)) items++;   // % w C# zachowuje znak (z < 0)
            }
            Collider(at + Vector3.up * 1.2f, Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > .5f ? new Vector3(.4f, 2.4f, 1.62f) : new Vector3(1.62f, 2.4f, .4f));
        }
        // regały: lewa ściana (6 × 1.7 m od zf + 2) i tylna (5 × 1.7 m), twarzą do środka
        for (float z = zf + 2.0f; z <= zb - 1.6f; z += 1.7f) Bay(new Vector3(a + .3f, FY, z), 90f, (int)(z * 3));
        for (float x = a + 2.2f; x <= b - 2.2f + .01f; x += 1.7f) Bay(new Vector3(x, FY, zb - .3f), 180f, (int)(x * 5));
        // wyspa na środku z produktami, lada przy wejściu po prawej
        var isl = KitPart("DisplayIsland", shop, new Vector3(cx - .5f, FY, zf + 6.8f), 0f);
        if (isl) { Collider(new Vector3(cx - .5f, .6f, zf + 6.8f), new Vector3(1.7f, 1.0f, .9f)); for (int k2 = 0; k2 < 3; k2++) if (KitPart(prod[k2], shop, new Vector3(cx - 1.0f + k2 * .5f, FY + .96f, zf + 6.8f), k2 * 20f)) items++; }
        if (KitPart("Counter", shop, new Vector3(b - 1.6f, FY, zf + 2.4f), 270f)) Collider(new Vector3(b - 1.6f, .6f, zf + 2.4f), new Vector3(.9f, 1.2f, 2.8f));
        int lamps = 0;
        foreach (float x in new[] { cx - 2.6f, cx + 2.6f }) foreach (float z in new[] { zf + 3.5f, zf + 7.5f, zf + 11.0f }) if (KitPart("LinearPendant", shop, new Vector3(x, ceilY - .72f, z), 0f)) lamps++;
        // bez świecenia: dyfuzory lamp matowe
        foreach (var r in shop.GetComponentsInChildren<MeshRenderer>(true))
        {
            var ms = r.sharedMaterials;
            for (int k = 0; k < ms.Length; k++) if (ms[k] && ms[k].name.Contains("opal")) ms[k] = LampGlass;
            r.sharedMaterials = ms;
        }
        MergeStatic(shop, "BRAXHORN Nutrition — wyposażenie (scalone)", .2f);
        rep.notes.Add("Nutrition (pierzeja B): " + bays + " regałów, " + items + " produktów, wyspa, lada, " + lamps + " lamp (scalone)");
    }

    // ---------------- komisariat: nowe wnętrze ----------------
    // Stare wnętrze (gotowy model ze Sketchfab: wypalone światło, kartony, amerykańskie flagi i plakaty)
    // usunięte z paczki w całości (nie tylko wyłączone). Nowe: hol z poczekalnią, lada dyżurnego z szybą,
    // sala operacyjna z biurkami w rzędach i krzesłami ustawionymi równo, gabinet komendanta za szkłem,
    // pokój przesłuchań z lustrem weneckim, dwie cele z kratami, korytarz do tylnego wyjścia.
    // Bryła: x -106..-66, z -57..-27 (ściany 0.2–0.5 m), sufit 4.39, drzwi frontowe i tylne na osi x = -86.
    static GameObject offDesk, offChair, offArm, offSofa, offLamp, offShelf, offPads, offBoard;
    static GameObject diningTable, coffeeTable, tvCabinet;
    static readonly List<Vector3> saleDoors = new List<Vector3>();
    static void PoliceInterior(Transform estate, Transform physics)
    {
        group = "Komisariat / wnętrze";
        const float ix0 = -105.8f, ix1 = -66.2f, iz0 = -56.8f, iz1 = -27.2f, FY = .07f, CY = 4.38f, ax = -86f;
        int gone = 0;
        foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true).ToList())
        {
            if (!mr || !mr.name.StartsWith("EstateMesh_") || !int.TryParse(mr.name.Substring(11), out var id)) continue;
            if (id >= 364 && id <= 384) { UnityEngine.Object.DestroyImmediate(mr.gameObject); gone++; }
        }
        // wewnętrzne drzwi starego modelu i stopnie na jego podest
        CutVolumes(estate, physics, new List<Bounds> { MinMax(ax - 2.4f, ax + 2.4f, -.01f, 3.3f, -36.7f, -32.1f) });
        int cols = 0;
        if (physics)
            foreach (var c in physics.GetComponentsInChildren<Collider>(true).ToList())
            {
                var b = c.bounds;
                if (b.min.x > ix0 - .1f && b.max.x < ix1 + .1f && b.min.z > iz0 - .1f && b.max.z < iz1 + .1f && b.size.y < 4.2f) { UnityEngine.Object.DestroyImmediate(c); cols++; }
            }
        rep.notes.Add("komisariat: usunięto stare wnętrze (" + gone + " siatek, " + cols + " kolizji)");

        var floor = PbrFloor; var wall = InteriorPaint; var wood = Plain("lada dyżurnego drewno", new Color(.45f, .33f, .22f), .35f);
        Top(floor, ix0, ix1, iz0, iz1, FY);
        // ściany wewnętrzne: przegroda sali i zaplecza (z = -47) z przejściami, ściany pokoi z tyłu
        float pz = -47f, wt = .15f;
        void WallX(float x0, float x1, float z) { BoxMinMax(wall, x0, x1, FY, CY, z - wt * .5f, z + wt * .5f); }
        void WallZ(float z0, float z1, float x) { BoxMinMax(wall, x - wt * .5f, x + wt * .5f, FY, CY, Mathf.Min(z0, z1), Mathf.Max(z0, z1)); }
        void DoorFrame(float x, float z, bool alongX)
        {
            if (alongX) { BoxMinMax(Frame, x - .55f, x - .48f, FY, 2.25f, z - .1f, z + .1f, false); BoxMinMax(Frame, x + .48f, x + .55f, FY, 2.25f, z - .1f, z + .1f, false); BoxMinMax(Frame, x - .55f, x + .55f, 2.18f, 2.25f, z - .1f, z + .1f, false); BoxMinMax(wall, x - .48f, x + .48f, 2.25f, CY, z - wt * .5f, z + wt * .5f, false); }
            else { BoxMinMax(Frame, x - .1f, x + .1f, FY, 2.25f, z - .55f, z - .48f, false); BoxMinMax(Frame, x - .1f, x + .1f, FY, 2.25f, z + .48f, z + .55f, false); BoxMinMax(Frame, x - .1f, x + .1f, 2.18f, 2.25f, z - .55f, z + .55f, false); BoxMinMax(wall, x - wt * .5f, x + wt * .5f, 2.25f, CY, z - .48f, z + .48f, false); }
        }
        // przegroda z = -47: przejście korytarza (ax ± 2), drzwi do pokoju przesłuchań (x -99)
        WallX(ix0, -99.55f, pz); DoorFrame(-99f, pz, true); WallX(-98.45f, ax - 2f, pz); WallX(ax + 2f, ix1, pz);
        // pokój przesłuchań (x ix0..-93), korytarz (-93..-79), cele (-79..ix1)
        WallZ(pz, iz0, -93f);
        // od korytarza do cel (x = -79) nie ma ściany — same kraty (w buildzie 2 pełna ściana zasłaniała kraty i cele)
        // gabinet komendanta: szklane ścianki (x ix0..-97, z -36..pz)
        BoxMinMax(GlassClear, -97.03f, -96.97f, FY + .9f, 2.8f, -46.9f, -36.6f, false); BoxMinMax(wall, -97.06f, -96.94f, FY, FY + .9f, -46.9f, -36.6f);
        BoxMinMax(GlassClear, ix0, -98.2f, FY + .9f, 2.8f, -36.03f, -35.97f, false); BoxMinMax(wall, ix0, -98.2f, FY, FY + .9f, -36.06f, -35.94f);
        BoxMinMax(Frame, -97.06f, -96.94f, 2.8f, 2.9f, -46.9f, -35.94f, false); BoxMinMax(Frame, ix0, -96.94f, 2.8f, 2.9f, -36.06f, -35.94f, false);
        // lada dyżurnego z szybą (z = -34.2), furtka po prawej
        float cz = -34.2f;
        BoxMinMax(wood, -96f, -78.2f, FY, 1.1f, cz - .35f, cz + .35f); BoxMinMax(Concrete, -96.05f, -78.15f, 1.1f, 1.16f, cz - .45f, cz + .45f, false);
        BoxMinMax(GlassClear, -96f, -78.2f, 1.16f, 2.2f, cz - .02f, cz + .02f, false);
        for (float x = -96f; x <= -78.1f; x += 2.97f) BoxMinMax(Frame, x - .03f, x + .03f, 1.16f, 2.24f, cz - .04f, cz + .04f, false);
        BoxMinMax(Frame, -96f, -78.2f, 2.2f, 2.26f, cz - .04f, cz + .04f, false);
        BoxMinMax(wood, -76.8f, -75.6f, FY, 1.0f, cz - .05f, cz + .05f, false);                           // furtka
        Sign("policja", new Vector3(ax, 3.3f, pz + .1f), 6f, 1.5f, Vector3.forward);                        // szyld widoczny z holu
        // poczekalnia: krzesła w rzędzie pod ścianą zachodnią, kosz, tablica
        int seats = 0;
        if (offChair) for (float z = -28.6f; z > -33.4f; z -= .75f) { Put(offChair, new Vector3(ix0 + .55f, FY, z), 90f, .04f, true); seats++; }
        if (offArm) { Put(offArm, new Vector3(-74f, FY, -29f), 180f, .04f, true); Put(offArm, new Vector3(-71.5f, FY, -29f), 180f, .04f, true); }
        Bin(new Vector3(-90f, FY, -28.2f));
        if (offBoard) Put(offBoard, new Vector3(-69f, FY, -32f), 270f, .04f, false);
        // sala operacyjna: 3 rzędy biurek, krzesła równo za biurkami (siedzący patrzy w stronę holu)
        int desks = 0;
        foreach (float rz in new[] { -38.2f, -41.6f, -45f })
            foreach (float x in new[] { -94.5f, -91.6f, -80.4f, -77.5f, -72.4f, -69.5f })
            {
                if (offDesk && Put(offDesk, new Vector3(x, FY, rz), 0f, .03f, true)) desks++;
                if (offChair) Put(offChair, new Vector3(x, FY, rz - .95f), 0f, .03f, false);
                if (offPads && desks % 2 == 0) Put(offPads, new Vector3(x + .4f, FY + .79f, rz + .1f), 15f * (desks % 3), .015f, false);
            }
        // gabinet komendanta
        if (offDesk) Put(offDesk, new Vector3(-101.5f, FY, -40f), 90f, .03f, true);
        if (offChair) Put(offChair, new Vector3(-102.6f, FY, -40f), 90f, .03f, false);
        if (offSofa) Put(offSofa, new Vector3(-101.5f, FY, -45.9f), 0f, .03f, true);
        if (offShelf) Put(offShelf, new Vector3(-105.1f, FY, -43.5f), 90f, .03f, true);
        // szafy na akta wzdłuż przegrody (strona sali)
        if (offShelf) foreach (float x in new[] { -82.6f, -76f, -72.2f, -68.4f }) Put(offShelf, new Vector3(x, FY, pz + .45f), 180f, .03f, true);
        // pokój przesłuchań: stół, dwa krzesła, lustro weneckie od korytarza
        BoxMinMax(Steel, -100.8f, -98.8f, FY + .72f, FY + .76f, -52.4f, -51.4f, false);
        foreach (float x in new[] { -100.6f, -99f }) foreach (float z in new[] { -52.3f, -51.5f }) BoxMinMax(Steel, x - .03f, x + .03f, FY, FY + .72f, z - .03f, z + .03f, false);
        Collider(new Vector3(-99.8f, FY + .4f, -51.9f), new Vector3(2f, .8f, 1f));
        if (offChair) { Put(offChair, new Vector3(-99.8f, FY, -53.1f), 0f, .03f, false); Put(offChair, new Vector3(-99.8f, FY, -50.7f), 180f, .03f, false); }
        BoxMinMax(Plain("lustro weneckie", new Color(.12f, .14f, .15f), .97f, .7f), -93.1f, -92.9f, FY + 1.0f, 2.3f, -53.5f, -50f, false);
        // cele: dwie, kraty od korytarza (x = -79), betonowa ławka w każdej
        WallX(-79f, ix1, -51.9f);
        foreach (var (c0, c1) in new[] { (-51.9f, pz), (iz0, -51.9f) })
        {
            BoxMinMax(Concrete, -77.5f, -70f, FY, FY + .45f, c0 + .1f, c0 + .7f);
            for (float z = Mathf.Min(c0, c1) + .2f; z < Mathf.Max(c0, c1) - .1f; z += .14f)
                if (Mathf.Abs(z - (c0 + c1) * .5f) > .55f) BoxMinMax(Steel, -79.03f, -78.97f, FY, CY, z - .015f, z + .015f, false);
            BoxMinMax(Steel, -79.05f, -78.95f, 2.1f, 2.16f, Mathf.Min(c0, c1), Mathf.Max(c0, c1), false);
            BoxMinMax(Steel, -79.05f, -78.95f, FY, CY, (c0 + c1) * .5f - .55f, (c0 + c1) * .5f - .5f, false);
            Collider(new Vector3(-79f, CY * .5f, (c0 + c1) * .5f), new Vector3(.12f, CY, Mathf.Abs(c1 - c0)));
        }
        // lampy wiszące (bez świecenia) nad salą, holem i korytarzem
        int lamps = 0;
        if (offLamp)
            foreach (var p in new[] { new Vector2(-93f, -39.9f), new Vector2(-79f, -39.9f), new Vector2(-71f, -39.9f), new Vector2(-93f, -43.3f), new Vector2(-79f, -43.3f), new Vector2(-71f, -43.3f),
                                      new Vector2(-92f, -30.5f), new Vector2(-80f, -30.5f), new Vector2(-101.5f, -41f), new Vector2(-86f, -52f), new Vector2(-99.8f, -51.9f) })
            {
                var o = Put(offLamp, new Vector3(p.x, 0, p.y), 0f, .02f, false);
                if (o) { var b = RB(o); o.transform.position += Vector3.up * (CY - b.max.y); lamps++; }
            }
        rep.notes.Add("komisariat: nowe wnętrze — " + desks + " biurek, " + seats + " krzeseł w poczekalni, " + lamps + " lamp, 2 cele");
    }
    static Material PbrFloor => Pbr("posadzka komisariatu", "large_grey_tiles", 2.4f, new Color(.95f, .95f, .95f));
    static Material InteriorPaint => Pbr("ściany wnętrz farba", "painted_plaster_wall", 2.2f, new Color(.93f, .92f, .89f));

    // ---------------- główne ----------------
    public static void Apply()
    {
        rep = new Report(); mats.Clear(); tile.Clear(); batches.Clear(); groups.Clear(); group = "osiedle"; saleDoors.Clear(); treeSpots.Clear(); trees.Clear(); roofBounds.Clear();
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
        outdoorSet = Model("outdoor_table_chair_set_01"); bench = Model("modular_street_seating"); parkLamp = Model("street_lamp_01");
        picnic = Model("wooden_picnic_table");
        offDesk = Model("metal_office_desk"); offChair = Model("dining_chair_02"); offArm = Model("modern_arm_chair_01"); offSofa = Model("sofa_02");
        nutriKit = Model("nutrition_kit");
        diningTable = Model("dining_table"); coffeeTable = Model("modern_coffee_table_01"); tvCabinet = Model("modern_wooden_cabinet");
        offLamp = Model("modern_ceiling_lamp_01"); offShelf = Model("drawer_cabinet"); offPads = Model("office_notepads"); offBoard = Model("standing_chalkboard_01");
        tufts = new[] { Model("grass_medium_01"), Model("grass_medium_02"), Model("grass_bermuda_01") }.Where(g => g).ToArray();
        flowers = new[] { Model("flower_empodium"), Model("periwinkle_plant"), Model("flower_ursinia"), Model("dandelion_01"), Model("flower_gazania") }.Where(g => g).ToArray();
        grassNodes = tufts.SelectMany(g => PlantNodes(g, .12f)).ToList();     // bez drobinek < 12 cm (z daleka i tak niewidoczne)
        flowerSpecies = flowers.Select(g => PlantNodes(g)).Where(l => l.Count > 0).ToList(); flowerNodes = flowerSpecies.SelectMany(l => l).ToList();
        rep.notes.Add("rośliny: " + grassNodes.Count + " wariantów kęp trawy, " + flowerNodes.Count + " wariantów kwiatów (" + flowerSpecies.Count + " gatunków)");

        // 0) niebo z chmurami zamiast jednolitego tła, światło i kolor dnia; trawa osiedla A na fotoskanie (jedna, bez nakładania)
        Guard("niebo", Sky); Guard("światło dnia", Daylight);
        var oldGround = estate.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "EstateMesh_273");
        if (oldGround && oldGround.bounds.size.x > 200f && oldGround.bounds.max.y < .1f)
        {
            UnityEngine.Object.DestroyImmediate(oldGround.gameObject); group = "Podłoże A";
            // płaskie zielone trawniki działek (jednolity kolor) też precz — spod nich widać fotoskan
            var plotGrass = estate.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "EstateMesh_274");
            if (plotGrass && plotGrass.bounds.max.y < .1f) { UnityEngine.Object.DestroyImmediate(plotGrass.gameObject); rep.notes.Add("trawa A: płaskie trawniki działek (EstateMesh_274) usunięte"); }
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
        Guard("siłownia A (za kasynem)", () => GymA(86f, -66f));
        group = "Ogrodzenie";
        // furtka x 51.6–55.4: siłownia plenerowa w parku ↔ wschodnia ścieżka osiedla A (x 52–55, od z -101) ↔ siłownia A
        FenceX(X0, X1, FenceA, 1.6f, (AxisX - 4f, AxisX + 4f), (28f, 32f), (51.6f, 55.4f));
        foreach (float gx in new[] { AxisX - 4.3f, AxisX + 4.3f, 27.7f, 32.3f, 51.3f, 55.7f }) Box(Plinth, new Vector3(gx, 1.15f, FenceA), new Vector3(.62f, 2.3f, .62f));
        Top(Slabs, 52f, 55f, -103.5f, -101f, .05f);
        FenceZ(SouthZ, FenceA, X0 + .25f, 1.6f); FenceZ(SouthZ, FenceA, X1 - .25f, 1.6f); FenceX(X0, X1, SouthZ + .25f, 1.6f);

        // 3) park
        Park();

        // 4) osiedle B: ulica główna W-E, ulica N-S od alei parku, zebry
        group = "Osiedle B / ulice";
        StreetX(X0 + .6f, X1 - .6f, StreetZ, AxisX);
        StreetNS(ParkZ0, StreetZ + Carriage, AxisX);
        Zebra(AxisX - Carriage, AxisX + Carriage, StreetZ + Carriage + .2f, StreetZ + Carriage + 3.2f, true);
        Zebra(AxisX + 9f, AxisX + 12f, StreetZ - Carriage, StreetZ + Carriage, false);
        foreach (int sd in new[] { -1, 1 })   // dojścia do zebry przez pasy zieleni (bez trawy między chodnikiem a przejściem)
            BoxMinMax(Pavers, AxisX + 8.6f, AxisX + 12.4f, 0, .14f, Mathf.Min(StreetZ + sd * (Carriage + Kerb), StreetZ + sd * (Carriage + Kerb + Green)), Mathf.Max(StreetZ + sd * (Carriage + Kerb), StreetZ + sd * (Carriage + Kerb + Green)));
        // wjazd do parku: kamienne słupy na narożnikach (jezdnia kończy się słupkami, chodniki wchodzą na placyk parku)
        foreach (float gx in new[] { AxisX - Half - .25f, AxisX + Half + .3f }) Box(Plinth, new Vector3(gx, 1.1f, ParkZ0 - .2f), new Vector3(.6f, 2.2f, .6f));

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
            if (k == 3) TennisClub(a, b, south, -1f); else Villa(idx++, a, b, south, -1f, forSale: k == 0 || k == 2 || k == 4);
        }
        Guardhouse(AxisX - 7f, FenceA - 2.4f);
        group = "Osiedle B / ogrodzenie";
        WoodFenceZ(north + .6f, ParkZ0 - .3f, X1 - .7f); WoodFenceZ(south - .6f, SouthZ + .3f, X1 - .7f);
        FenceZ(north + .3f, ParkZ0 - .3f, AxisX - Half - .25f, 1.55f);

        // 6) osiedle A tak samo jak B: drzewa, krzewy i rabaty na działkach, szpaler wzdłuż ulicy głównej; zielone korony starych drzew
        Guard("zieleń osiedla A", () => GreenA(estate, physics));
        Guard("korony starych drzew A", () => FixOldLeaves(estate));
        Guard("okna z pokojami w starych domach A", () => FakeRoomsA(estate));
        Guard("komisariat — nowe wnętrze", () => PoliceInterior(estate, physics));
        Guard("korony drzew z dala od budynków", PruneCrowns);
        // 7) trawa w 3D na wszystkich trawnikach parku i osiedla B (po wszystkim, co zajmuje ziemię)
        group = "Trawa 3D";
        Guard("kępy traw i kwiaty na trawnikach", () => LawnPlants(X0 + .4f, X1 - .4f, SouthZ + .4f, FenceA - .4f));

        Flush();
        FlushClumps();

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
                new BraxCityPoint { id = "silownia", label = "Siłownia BRAXHORN (osiedle A, za kasynem)", position = new Vector3(gymDoorX, .1f, gymDoorZ + 2.5f) },
                new BraxCityPoint { id = "nutrition_b", label = "BRAXHORN Nutrition (pierzeja B, obok Shake Baru)", position = new Vector3(AxisX + Half + .5f + 2.5f * (shopEnd - (AxisX + Half + .5f)) / 5f, .1f, north + 2.5f) },
                new BraxCityPoint { id = "kort_tenisowy", label = "Klub tenisowy (3 na 3)", position = new Vector3(X0 + .6f + 3.5f * (X1 - X0 - 1.2f) / 6f, .1f, south - 3f) },
            };
            add = add.Concat(saleDoors.Select((p, i) => new BraxCityPoint { id = "willa_na_sprzedaz_" + (i + 1), label = "Willa na sprzedaż " + (i + 1), position = p })).ToArray();
            world.points = world.points.Concat(add).ToArray(); rep.points = add.Length;
        }
        else rep.notes.Add("brak BraxCityWorld estate — bez nowych punktów");
        // pomiar kadru do zdjęć (działa tylko z ?qa=1): trójkąty / obiekty / materiały widoczne w kadrze
        new GameObject("BRAXHORN pomiar kadru (tylko ?qa=1)").AddComponent<BraxClaudeKadr>();
        AssetDatabase.SaveAssets();
    }
}

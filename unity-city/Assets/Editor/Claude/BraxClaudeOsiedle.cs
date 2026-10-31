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
        public int tufts, ballTreesReplaced, shops, tennis, facades, villas, pools, parkTrees, plotTrees, shrubs, benches, benchPads, treesMovedOffPaving, treesRemovedOnPaving, avenueTrees, arenaSeats, lamps, bins, tables, oldFenceTrianglesRemoved,
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
        bool near = new[] { "grass004", "concrete_pavers", "concrete_pavement", "asphalt_04", "herringbone", "wood_floor_deck", "yellow_flower_mulch", "large_grey_tiles", "wood_floor/", "brown_mud" }.Any(q => path.Contains(q));
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
    static Material Anthracite => Pbr("tynk antracyt", "white_stucco_02", 2.5f, new Color(.20f, .20f, .21f));   // willa 3 (nowoczesna)
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
            m.SetFloat("_Tile", 2f);                                                 // kafel 2 m (zlecenie trawy: 1.5–2 m)
            // pasy koszenia: szerokość 1.8 m (zlecenie 1.5–2 m), jasny / ciemny = (1 + 0.045) / (1 − 0.045) = 1.094 → 9.4% (zlecenie 8–10%)
            m.SetFloat("_Stripe", 1.8f); m.SetFloat("_StripeMix", .045f);
            // kolor liczony: średnia tekstury (liniowo) → plamy i suche miejsca względem średniej, odcień tak,
            // żeby luminancja albedo = 0.133 (wzorzec ColorChecker „Foliage”, sRGB 87/108/67)
            var am = AlbedoMeanLinear(d + "albedo.jpg");
            if (am.y > .01f)
            {
                m.SetFloat("_MacroMean", .3f * am.x + .59f * am.y + .11f * am.z); m.SetFloat("_DryMean", am.y);
                // odcień liniowo: pomiar buildu 3 — trawnik w słońcu sRGB 113–117/135–139/69 (odcień 79°, oliwka, nasycenie 0.49);
                // cel 100/142/60 (odcień 89°, żywa zieleń, nasycenie 0.58, ta sama luminancja): mnożniki liniowe R 0.713, G 1.046, B 0.75
                var baseLin = new Color(.536f, 1f, .536f);
                float Y = .2126f * am.x * baseLin.r + .7152f * am.y * baseLin.g + .0722f * am.z * baseLin.b, k = .133f / Y;
                var lin = new Color(baseLin.r * k, baseLin.g * k, baseLin.b * k, 1f);
                m.color = lin.gamma; grassLin = new Vector4(lin.r, lin.g, lin.b, 1f);
                grassMeanLin = new Vector3(am.x * lin.r, am.y * lin.g, am.z * lin.b);    // średnie albedo trawnika (liniowo) — wzorzec koloru kęp
                rep.notes.Add("trawnik: średnia tekstury liniowo " + am.ToString("F4") + ", luminancja tekstury " + (.2126f * am.x + .7152f * am.y + .0722f * am.z).ToString("0.000")
                              + ", odcień liniowo " + ((Vector4)lin).ToString("F3") + " → albedo Y 0.133");
            }
            // warstwa ziemi (przy ścieżkach i pod drzewami): ziemia z liśćmi Poly Haven, kafel 1,3 m (wymiar skanu);
            // maska powstaje na końcu budowy (BuildSoilMask), do tego czasu bez ziemi (_SoilMix 0)
            var sd = Tex + "brown_mud_leaves_01/";
            if (File.Exists(sd + "albedo.jpg"))
            {
                m.SetTexture("_SoilTex", Map(sd + "albedo.jpg", false, true)); m.SetTexture("_SoilBump", Map(sd + "normal.jpg", true, false));
                var sm = AlbedoMeanLinear(sd + "albedo.jpg"); float sy = .2126f * sm.x + .7152f * sm.y + .0722f * sm.z;
                // ziemia o luminancji 0.10 (wilgotna ziemia z liśćmi; trawa 0.133) — liczone, nie na oko
                float sk = sy > .005f ? .10f / sy : 1f; m.SetVector("_SoilTint", new Vector4(sk, sk, sk, 1f));
                rep.notes.Add("ziemia: średnia tekstury Y " + sy.ToString("0.000") + " → mnożnik " + sk.ToString("0.00"));
            }
            m.SetFloat("_SoilMix", 0f);
            AssetDatabase.CreateAsset(m, Out + "/trawnik.mat"); mats["trawnik"] = m; tile[m] = 3.5f; grassMat = m; return m;
        }
    }
    static Material grassMat;
    static Vector4 grassLin = Vector4.one;      // odcień trawnika liniowo (do kęp: kolor nasady = kolor podłoża)
    static Vector3 grassMeanLin = Vector3.zero; // średnie albedo trawnika liniowo (tekstura × odcień)
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
        m.SetFloat("_InnerDark", .35f);   // wnętrze korony: zdjęcia buildu 3 — korony średnio V 0.27–0.29 (za ciemne), -30% przyciemnienia
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
        imp.mipmapEnabled = true; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Compressed;
        // liście z alfą: mipmapy zachowują pokrycie przy progu cięcia (0.42) — korony z daleka nie chudną i nie migoczą
        if (!normal) { imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .42f; }
        imp.SaveAndReimport();
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
        if (y < .2f && IsPavingMat(m)) Paved(x0, x1, z0, z1);
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

    // Nawierzchnie (chodniki, alejki, place, jezdnie) — osobno od Hard (Hard = też budynki, płoty, latarnie): drzewo nie stoi
    // na chodniku, ławka patrzy na najbliższą alejkę (właściciel 31.10). Rejestrowane: Top/Box/OBox z materiałem nawierzchni
    // oraz nawierzchnie Codexa z three.js (world.js, civic.js; x lustrzanie) — te ostatnie także jako Hard.
    static readonly Dictionary<long, List<Vector4>> pavedCells = new Dictionary<long, List<Vector4>>();
    static HashSet<Material> pavingSet;
    static bool IsPavingMat(Material m)
    {
        if (!m) return false;
        if (pavingSet == null) pavingSet = new HashSet<Material>(new[] { Pavers, Slabs, Herring, ParkPath, Asphalt, KerbStone }.Where(q => q));
        return pavingSet.Contains(m);
    }
    static void Paved(float x0, float x1, float z0, float z1)
    {
        if (x1 < x0) { var t = x0; x0 = x1; x1 = t; }
        if (z1 < z0) { var t = z0; z0 = z1; z1 = t; }
        var r = new Vector4(x0, z0, x1, z1);
        for (int cx = Mathf.FloorToInt(x0 / HardCell); cx <= Mathf.FloorToInt(x1 / HardCell); cx++)
            for (int cz = Mathf.FloorToInt(z0 / HardCell); cz <= Mathf.FloorToInt(z1 / HardCell); cz++)
            {
                var k = CellKey(cx, cz);
                if (!pavedCells.TryGetValue(k, out var l)) pavedCells[k] = l = new List<Vector4>();
                l.Add(r);
            }
    }
    static bool OnPaving(float x, float z, float margin)
    {
        for (int cx = Mathf.FloorToInt((x - margin) / HardCell); cx <= Mathf.FloorToInt((x + margin) / HardCell); cx++)
            for (int cz = Mathf.FloorToInt((z - margin) / HardCell); cz <= Mathf.FloorToInt((z + margin) / HardCell); cz++)
                if (pavedCells.TryGetValue(CellKey(cx, cz), out var l))
                    foreach (var r in l) if (x > r.x - margin && x < r.z + margin && z > r.y - margin && z < r.w + margin) return true;
        return false;
    }
    // Nawierzchnie osiedla A z kodu Codexa (three.js: środek x, z, szerokość, głębokość); Unity x = −x.
    static void CodexPaving()
    {
        var r = new List<(float x, float z, float w, float d)> {
            (-6, 37, 2.8f, 122), (6, 37, 2.8f, 122), (0, -25, 94, 3.3f), (0, -13, 94, 3.3f), (0, 37, 9, 122), (0, -19, 91, 8), (0, 39, 91, 5),
            (-43, 10, 5, 53), (43, 10, 5, 53), (-47, 10, 2, 53), (47, 10, 2, 53),
            (-30, -35, 25, 22), (0, -28, 18, 8), (30, -35, 16, 22), (43, -43, 5, 33), (0, -38, 24, 16), (30, -28, 3, 9), (30, -46.5f, 3, 10), (30, -53, 11, 8),
            (0, -67, 116, 2.5f), (0, -99, 116, 2.5f), (-58, -73, 6, 38), (58, -73, 6, 38), (-53.5f, -83, 2, 33), (53.5f, -83, 2, 33),
            (0, -83, 3, 32), (28, -83, 54, 2.5f), (-28, -83, 28, 15) };
        foreach (float z in new[] { -19f, 39f, 86f }) { r.Add((0, z, 224, 7)); r.Add((0, z - 5, 224, 2.8f)); r.Add((0, z + 5, 224, 2.8f)); }
        foreach (float x in new[] { -58f, 58f }) { r.Add((x, 16, 6, 140)); r.Add((x - 4.5f, 16, 2, 140)); r.Add((x + 4.5f, 16, 2, 140)); }
        foreach (var (fx, fz) in new[] { (-86f, -42f), (-86f, 14f), (86f, -42f), (86f, 14f), (-86f, 64f), (86f, 64f) })
        { r.Add((fx, fz, 40, 30)); r.Add((fx, fz + 18, 44, 5)); r.Add((fx - 22, fz, 2.7f, 40)); r.Add((fx + 22, fz, 2.7f, 40)); r.Add((fx, fz - 17, 44, 3)); }
        foreach (var h in Houses) r.Add((h.x < 0 ? -12f : 12f, h.z, 13, 2.4f));
        foreach (var q in r) { float x0 = -q.x - q.w * .5f, x1 = -q.x + q.w * .5f, z0 = q.z - q.d * .5f, z1 = q.z + q.d * .5f; Paved(x0, x1, z0, z1); Hard(x0, x1, z0, z1); }
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
        if (c.y - ext.y < .3f && IsPavingMat(m)) Paved(c.x - ext.x, c.x + ext.x, c.z - ext.z, c.z + ext.z);
    }

    // Prostopadłościan wyrównany do osi; bottom=false pomija spód (niewidoczny), solid = BoxCollider.
    static void Box(Material m, Vector3 c, Vector3 s, bool solid = true, bool bottom = false)
    {
        var h = s * .5f;
        if (c.y - h.y < .3f && m != grassMat) Hard(c.x - h.x, c.x + h.x, c.z - h.z, c.z + h.z);
        if (c.y - h.y < .3f && IsPavingMat(m)) Paved(c.x - h.x, c.x + h.x, c.z - h.z, c.z + h.z);
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
    // Materiały modelu nieprzezroczyste (build 4, zdjęcia 42/63/66: sofa_03 z glTF w trybie przenikania wyglądała jak brązowe szkło):
    // kopia materiału z wyłączonym mieszaniem (renderQueue 2000, ZWrite, Src/Dst 1/0, bez słów kluczowych alfa), alfa koloru 1.
    static void ForceOpaque(GameObject o)
    {
        if (!o) return; int fixedM = 0;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                var m0 = ms[i]; if (!m0 || m0.renderQueue < 2450) continue;
                var m = new Material(m0) { name = m0.name + " (nieprzezroczysty)", renderQueue = 2000 };
                m.SetOverrideTag("RenderType", "Opaque");
                foreach (var (prop, v) in new[] { ("_SrcBlend", 1f), ("_DstBlend", 0f), ("_ZWrite", 1f), ("_Mode", 0f), ("_Surface", 0f), ("_AlphaCutoff", 0f) }) if (m.HasProperty(prop)) m.SetFloat(prop, v);
                foreach (var kw in new[] { "_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON", "_ALPHATEST_ON", "_BUILTIN_ALPHATEST_ON", "_BUILTIN_TRANSPARENT_RECEIVE_SHADOWS", "_SURFACE_TYPE_TRANSPARENT" }) m.DisableKeyword(kw);
                foreach (var cp in new[] { "_Color", "baseColorFactor", "_BaseColor" }) if (m.HasProperty(cp)) { var c = m.GetColor(cp); c.a = 1f; m.SetColor(cp, c); }
                AssetDatabase.CreateAsset(m, Out + "/" + Safe("opaque " + m0.name + " " + (++opaqueN)) + ".mat");
                ms[i] = m; fixedM++;
            }
            r.sharedMaterials = ms;
        }
        if (fixedM > 0) rep.notes.Add("nieprzezroczyste: " + o.name + " (" + fixedM + " mat.)");
    }
    static int opaqueN;
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
        BenchSpot(ref at, ref yaw, 2.3f);
        if (bench && PutParts(bench, BenchDrop, at, yaw, .05f, true)) { rep.benches++; return; }
        Bench(at, yaw);
    }
    // Ławka przy alejce (właściciel 31.10: „jak przy chodniku jest ławka, to powinna być obrócona w stronę chodnika… każda
    // jedna. I powinien być taki kwadracik z kostki na ławeczkę… wszędzie”). Ławka na trawie: siedzący patrzy na najbliższą
    // nawierzchnię (szukanie co 15°, do 4 m), ławka staje 1 m od jej krawędzi, pod nią plac z kostki (jodełka) od oparcia
    // do alejki, z krawężnikiem z trzech stron. Ławka już na nawierzchni zostaje bez zmian.
    static void BenchSpot(ref Vector3 at, ref float yaw, float len)
    {
        if (OnPaving(at.x, at.z, 0f) || IsHard(at.x, at.z, 0f)) return;   // na placu, tarasie, alejce — bez zmian
        float best = 99f; var dir = Vector3.zero;
        for (int a = 0; a < 24; a++)
        {
            var d = Quaternion.Euler(0, a * 15f, 0) * Vector3.forward;
            for (float s = .3f; s <= 4f && s < best; s += .1f)
                if (OnPaving(at.x + d.x * s, at.z + d.z * s, 0f)) { best = s; dir = d; break; }
        }
        float reach = 1.0f;                                       // od środka siedziska do krawędzi alejki
        if (best < 90f)
        {
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (best > 1.2f)
            {
                var na = at + dir * (best - 1.0f);
                if (!IsHard(na.x, na.z, .25f)) at = na; else reach = best;
            }
            else reach = best;
        }
        at.y = Mathf.Max(at.y, .055f);
        var q = Quaternion.Euler(0, yaw, 0); Vector3 F = q * Vector3.forward, R = q * Vector3.right;
        float back = -.75f, front = best < 90f ? reach + .06f : 1.0f, half = len * .5f + .4f;
        var c = at + F * ((back + front) * .5f); c.y = .027f;
        OBox(Herring, c, new Vector3(half * 2f, .05f, front - back), yaw);
        var at0 = at;
        Vector3 E(float x, float z) => new Vector3(at0.x, .04f, at0.z) + R * x + F * z;
        OBox(KerbStone, E(0, back - .05f), new Vector3(half * 2f + .2f, .08f, .1f), yaw);
        foreach (float sx in new[] { -half - .05f, half + .05f }) OBox(KerbStone, E(sx, (back + front) * .5f), new Vector3(.1f, .08f, front - back), yaw);
        rep.benchPads++;
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
        var leaf = leafMesh ? LeafMat("drzewa", l0.sharedMaterial, leafMesh.bounds, .1182f, .0158f, .85f, new Color(.310f, .522f, .147f), new Color(.408f, .553f, .164f)) : null;
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
            if (f && f0) { bushMesh = f.sharedMesh; LeafMat("drzewa", f0.GetComponent<MeshRenderer>().sharedMaterial, f0.sharedMesh.bounds, .1182f, .0158f, .85f, new Color(.310f, .522f, .147f), new Color(.408f, .553f, .164f)); }
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
        BenchSpot(ref at, ref yaw, 1.9f);
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
    // Wysokość powierzchni mebla w punkcie (promień w dół na tymczasowe kolizje siatki tego mebla) — obwiednia daje najwyższy punkt
    // (np. fałda obrusa 0.873 m), a środek stołu jest niżej (0.81–0.83 m): wazon i karteczka stoją wtedy na blacie, nie w powietrzu.
    static float SurfaceTop(GameObject go, Vector3 at, float fallback)
    {
        if (!go) return fallback;
        var tmp = new List<MeshCollider>();
        try
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) { if (!mf.sharedMesh) continue; var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; tmp.Add(mc); }
            Physics.SyncTransforms();
            float best = float.NegativeInfinity;
            foreach (var h2 in Physics.RaycastAll(new Vector3(at.x, at.y + 3f, at.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                if (h2.collider is MeshCollider mc2 && tmp.Contains(mc2)) best = Mathf.Max(best, h2.point.y);
            return float.IsNegativeInfinity(best) ? fallback : best;
        }
        finally { foreach (var mc in tmp) if (mc) UnityEngine.Object.DestroyImmediate(mc); }
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
    // Kępa trawy: wyższa trawa (łąka) przy płotach i u stóp drzew — 3 kępy łąki w promieniu 0.25 m (rysowane instancjami);
    // kwiat: model Poly Haven.
    static void Tuft(Vector3 at, bool flower = false, float scale = 1f)
    {
        if (!flower)
        {
            for (int k = 0; k < 3; k++) lakaKand.Add(new Vector2(at.x + ((float)tuftRnd.NextDouble() - .5f) * .5f, at.z + ((float)tuftRnd.NextDouble() - .5f) * .5f));
            return;
        }
        var set = flowerNodes; if (set.Count == 0) return;
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
    // ---------------- trawa 3D (zlecenie „trawa — murawa jak na boisku + gęsta łąka”, 31.10) ----------------
    // Warstwa 2 = kępy rysowane instancjami: karty kęp Poly Haven (grass_medium_01) w atlasie 2K, 5 wariantów,
    // siatka 3 płaszczyzn (6 trójkątów), obrót losowy, przechył ±6°. Rysuje BraxClaudeTrawa (kawałki 9 × 9 m, zasięg 32 m).
    //  • MURAWA (domyślnie, wszędzie): wysokość 6.5 cm × 0.9–1.1 (5.9–7.2 cm), 1 kępa na 0.3 m (siatka 0.3 m z przesunięciem,
    //    ~11 / m²), zanik w shaderze: pełna do 10 m, połowa do 18 m, zero na 22 m; bez wiatru;
    //  • ŁĄKA (pasy przy płotach obwodowych osiedla, wokół kortu od zewnątrz, kępy przy płotach działek i u stóp drzew):
    //    0.32 m × 0.8–1.25 (26–40 cm), ta sama siatka 0.3 m, przejście do murawy na 1.5 m; zanik 12 / 25 / 30 m; lekki wiatr.
    // Warstwa 1 = maska ziemi 0,5 m/px tylko pod drzewami (wąski pierścień: pełna do 0.8 m od pnia, zanik do 1.6 m,
    // brzeg nieregularny z szumu); przy chodnikach murawa równa, z ostrą krawędzią (bez ziemi). Pod pierścieniem mniej kęp.
    static readonly List<Vector2> kepyKand = new List<Vector2>(), lakaKand = new List<Vector2>();
    static Rect tennisFence;   // ogrodzenie kortu (do pasa łąki wokół niego)
    static Vector2 tennisLotX;  // działka kortu x0..x1 (łąka nie wchodzi do ogrodów sąsiednich willi)
    [Serializable] sealed class KepyKomorka { public float u, v, w, h, aspekt; }
    [Serializable] sealed class KepyAtlas { public string zrodlo; public KepyKomorka[] komorki; }
    const float MaskX0 = -116f, MaskZ0 = -236f, MaskRes = .5f; const int MaskW = 464, MaskH = 674;
    static float[,] soil;
    static Texture2D soilTex;
    static float SoilAt(float x, float z)
    {
        int i = (int)((x - MaskX0) / MaskRes), j = (int)((z - MaskZ0) / MaskRes);
        return soil == null || i < 0 || j < 0 || i >= MaskW || j >= MaskH ? 0f : soil[i, j];
    }
    static void BuildSoilMask()
    {
        soil = new float[MaskW, MaskH];
        var cells = new Dictionary<long, List<Vector3>>();
        foreach (var t in treeSpots) { long k = CellKey(Mathf.FloorToInt(t.x / 4f), Mathf.FloorToInt(t.z / 4f)); if (!cells.TryGetValue(k, out var l)) cells[k] = l = new List<Vector3>(); l.Add(t); }
        var px = new Color32[MaskW * MaskH]; double sum = 0; int lawn = 0;
        for (int j = 0; j < MaskH; j++)
            for (int i = 0; i < MaskW; i++)
            {
                float x = MaskX0 + (i + .5f) * MaskRes, z = MaskZ0 + (j + .5f) * MaskRes, sv = 0f;
                if (!IsHard(x, z, 0f))
                {
                    // wąski pierścień ziemi z liśćmi pod drzewem: 1 do 0.8 m od pnia, liniowo do 0 na 1.6 m (zlecenie: „wąski pierścień”)
                    int cx = Mathf.FloorToInt(x / 4f), cz = Mathf.FloorToInt(z / 4f);
                    for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
                        if (cells.TryGetValue(CellKey(cx + a, cz + b), out var l))
                            foreach (var t in l)
                            {
                                float d = Mathf.Sqrt((t.x - x) * (t.x - x) + (t.z - z) * (t.z - z));
                                sv = Mathf.Max(sv, d < .8f ? 1f : d < 1.6f ? 1f - (d - .8f) / .8f : 0f);
                            }
                    // szum tylko w pierścieniu (nieregularny brzeg ×0.7–1.2); na otwartym trawniku 0 — bez plam ziemi na środku
                    if (sv > 0f) sv = Mathf.Clamp01(sv * (.7f + Mathf.PerlinNoise(x * .9f + 11.3f, z * .9f + 7.7f) * .5f)) * .9f;
                    sum += sv; lawn++;
                }
                soil[i, j] = sv; byte v8 = (byte)Mathf.RoundToInt(sv * 255f); px[j * MaskW + i] = new Color32(v8, v8, v8, v8);   // maska we wszystkich kanałach
            }
        var tex = new Texture2D(MaskW, MaskH, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
        const string path = "Assets/PlayableGenerated/ClaudeOsiedle/maska_ziemi.png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp)
        {
            // audyt (M1): typ Single Channel bierze domyślnie alfę → format Alpha8, a shader czyta .r = 0. Kanał czerwony jawnie
            // (format R8), alfa w PNG też = maska (na wszelki wypadek), bez skalowania 464×674 do potęgi 2, z mipmapami (bez migotania z daleka).
            imp.textureType = TextureImporterType.SingleChannel;
            var ts = new TextureImporterSettings(); imp.ReadTextureSettings(ts); ts.singleChannelComponent = TextureImporterSingleChannelComponent.Red; imp.SetTextureSettings(ts);
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.npotScale = TextureImporterNPOTScale.None; imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
        }
        soilTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (soilTex) rep.notes.Add("maska ziemi: format " + soilTex.format + ", " + soilTex.width + "×" + soilTex.height + ", mipmapy " + soilTex.mipmapCount);
        if (grassMat && soilTex && grassMat.HasProperty("_Mask"))
        {
            grassMat.SetTexture("_Mask", soilTex);
            grassMat.SetVector("_MaskRect", new Vector4(MaskX0, MaskZ0, 1f / (MaskW * MaskRes), 1f / (MaskH * MaskRes)));
            grassMat.SetFloat("_SoilMix", grassMat.GetTexture("_SoilTex") ? 1f : 0f);
            EditorUtility.SetDirty(grassMat);
        }
        rep.notes.Add("maska ziemi " + MaskW + "×" + MaskH + " (0,5 m/px): średnio " + (lawn > 0 ? sum / lawn : 0).ToString("0.000") + " na trawniku");
    }
    static Mesh KepaMesh()
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var nr = new List<Vector3>(); var idx = new List<int>();
        for (int q = 0; q < 3; q++)
        {
            float a = q * 60f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .5f; int b = v.Count;
            v.Add(-d); v.Add(d); v.Add(d + Vector3.up); v.Add(-d + Vector3.up);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            for (int k = 0; k < 4; k++) nr.Add(Vector3.up);
            idx.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        var m = new Mesh { name = "kępa (3 płaszczyzny, 6 trójkątów)" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(nr); m.SetTriangles(idx, 0);
        m.bounds = new Bounds(new Vector3(0, .5f, 0), new Vector3(3f, 1.2f, 3f));   // szerokość karty skaluje shader (do 2,7 × wysokość)
        AssetDatabase.CreateAsset(m, Out + "/kepa.asset");
        return m;
    }
    // Średni kolor nieprzezroczystych pikseli (liniowo, ważony alfą).
    static Vector3 CardMeanLinear(string png)
    {
        var r = Vector3.zero;
        try
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (File.Exists(png) && t.LoadImage(File.ReadAllBytes(png)))
            {
                var px = t.GetPixels32(); double sr = 0, sg = 0, sb = 0, sa = 0;
                for (int i = 0; i < px.Length; i += 3)
                {
                    float a = px[i].a / 255f; if (a < .5f) continue;
                    sr += Mathf.GammaToLinearSpace(px[i].r / 255f) * a; sg += Mathf.GammaToLinearSpace(px[i].g / 255f) * a; sb += Mathf.GammaToLinearSpace(px[i].b / 255f) * a; sa += a;
                }
                if (sa > 0) r = new Vector3((float)(sr / sa), (float)(sg / sa), (float)(sb / sa));
            }
            UnityEngine.Object.DestroyImmediate(t);
        }
        catch (Exception e) { rep.notes.Add("atlas kęp: " + e.Message); }
        return r;
    }
    // Waga łąki (0..1): pasy 2 m przy płotach bocznych parku (x ±114; ogrody willi B zostają murawą) i 2.5 m wokół ogrodzenia kortu
    // od zewnątrz; dalej liniowo do 0 na kolejnych 1.5 m (miękkie przejście do murawy, zlecenie: 1–2 m).
    static float MeadowW(float x, float z)
    {
        float W(float d, float band) => d < 0f ? 0f : d < band ? 1f : d < band + 1.5f ? 1f - (d - band) / 1.5f : 0f;
        float w = 0f;
        if (z < FenceA && z > ParkZ0) w = W(X1 - Mathf.Abs(x), 2f);   // tylko skraje parku przy płotach bocznych
        if (tennisFence.width > 0f && x > tennisLotX.x && x < tennisLotX.y)
        {
            float dx = Mathf.Max(tennisFence.xMin - x, x - tennisFence.xMax), dz = Mathf.Max(tennisFence.yMin - z, z - tennisFence.yMax);
            if (dx > 0f || dz > 0f) w = Mathf.Max(w, W(Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dz, 0f) * Mathf.Max(dz, 0f)), 2.5f));
        }
        return w;
    }
    static void GrassField(float x0, float x1, float z0, float z1)
    {
        group = "Trawa 3D";
        BuildSoilMask();
        // siatka 0,3 m z losowym przesunięciem (1 kępa na 0.3 m, ~11 / m²); 6 cm od krawędzi utwardzeń (ostra krawędź murawy)
        var rnd = new System.Random(5150);
        for (float z = z0; z < z1 - .3f; z += .3f)          // audyt: przesunięcie do +0.3 nie może wyjść poza trawnik
            for (float x = x0; x < x1 - .3f; x += .3f)
            {
                float px = x + (float)rnd.NextDouble() * .3f, pz = z + (float)rnd.NextDouble() * .3f;
                if (!IsHard(px, pz, .06f)) kepyKand.Add(new Vector2(px, pz));
            }
        // typ kępy: w pasie łąki (waga 1 → 0 na 1.5 m) losowo łąka, poza nim murawa; ziemia pod drzewem: 90% mniej kęp przy pełnej masce
        const float hMur = .065f / .32f;     // skala siatki (wysokość 1 × dane.wysokosc 0.32 m) dla murawy 6.5 cm
        var list = new List<Vector4>(kepyKand.Count + lakaKand.Count); var ys = new List<float>(kepyKand.Count + lakaKand.Count); int nMur = 0, nLaka = 0;
        // nasada: trawnik A na −0.01, B i park na −0.02; zagłębienie na przechył ±6° (pół szerokości karty × sin 6°):
        // murawa 0.07 m × 0.105 = 0.7 cm → 1.2 cm, łąka 0.42 m × 0.105 = 4.4 cm → 3 cm (dół karty to i tak przezroczyste źdźbła)
        float Y(float z, bool laka) => (z > FenceA ? -.01f : -.02f) - (laka ? .03f : .012f);
        foreach (var p in kepyKand)
        {
            if (IsHard(p.x, p.y, .04f)) continue;                                // place pod ławkami i wszystko dodane po zebraniu kandydatów
            if (rnd.NextDouble() < SoilAt(p.x, p.y) / .9) continue;              // pełna maska pierścienia (0.9) = bez kęp
            bool laka = rnd.NextDouble() < MeadowW(p.x, p.y);
            float sc = laka ? .8f + (float)rnd.NextDouble() * .45f : hMur * (.9f + (float)rnd.NextDouble() * .2f);
            list.Add(new Vector4(p.x, p.y, (float)rnd.NextDouble() * 360f, sc)); ys.Add(Y(p.y, laka)); if (laka) nLaka++; else nMur++;
        }
        foreach (var p in lakaKand)
        {
            if (IsHard(p.x, p.y, .06f) || rnd.NextDouble() < SoilAt(p.x, p.y) * .5) continue;
            list.Add(new Vector4(p.x, p.y, (float)rnd.NextDouble() * 360f, .8f + (float)rnd.NextDouble() * .45f)); ys.Add(Y(p.y, true)); nLaka++;
        }
        rep.tufts = list.Count;
        const string atlasPath = "Assets/Source/Claude/Osiedle/detail/kepy_atlas.png", jsonPath = "Assets/Source/Claude/Osiedle/detail/kepy_atlas.json";
        var imp = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
        var sh = Shader.Find("Braxhorn/OsiedleKepyInst");
        if (!imp || !sh || !File.Exists(jsonPath)) { rep.notes.Add("trawa 3D: brak atlasu kęp / shadera — bez kęp"); return; }
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f;   // kępy nie chudną z odległością
        imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        var info = JsonUtility.FromJson<KepyAtlas>(File.ReadAllText(jsonPath));
        var m = new Material(sh) { name = "Osiedle: kępy trawy (instancje)", mainTexture = atlas, enableInstancing = true };
        int nCells = Mathf.Min(5, info.komorki.Length);
        for (int i = 0; i < nCells; i++) { var c = info.komorki[i]; m.SetVector("_C" + i, new Vector4(c.u, c.v, c.w, c.h)); }
        m.SetFloat("_Cells", nCells); m.SetFloat("_AtlasAspect", atlas.width / (float)atlas.height);
        if (grassMat)
        {
            m.SetTexture("_GroundTex", grassMat.mainTexture); m.SetVector("_GroundTint", grassLin);
            foreach (var pn in new[] { "_Tile", "_MacroTile", "_MacroMix", "_MacroMean", "_DryMix", "_DryMean", "_DryThr", "_DryGain", "_Stripe", "_StripeMix" }) if (grassMat.HasProperty(pn)) m.SetFloat(pn, grassMat.GetFloat(pn));
            if (grassMat.HasProperty("_Dry")) m.SetVector("_Dry", grassMat.GetVector("_Dry"));   // Vector (bez konwersji sRGB), jak w OsiedleTrawa
        }
        // kolor kart liczony: średni kolor kart = 1.15 × średnie albedo trawnika, osobno R, G, B (ten sam odcień i nasycenie
        // co podłoże — „kolor pęczków = kolor podłoża”, czubki odrobinę jaśniejsze); bez pomiaru trawnika: tylko luminancja
        var cm = CardMeanLinear(atlasPath); float cy = .2126f * cm.x + .7152f * cm.y + .0722f * cm.z;
        float k = cy > .005f ? .133f * 1.15f / cy : 1f;
        var tip = new Vector4(k, k, k, 1);
        if (grassMeanLin.y > .005f && cm.x > .002f && cm.y > .002f && cm.z > .002f)
            tip = new Vector4(Mathf.Clamp(grassMeanLin.x * 1.15f / cm.x, 0f, 4f), Mathf.Clamp(grassMeanLin.y * 1.15f / cm.y, 0f, 4f), Mathf.Clamp(grassMeanLin.z * 1.15f / cm.z, 0f, 4f), 1f);
        m.SetVector("_TipTint", tip);
        AssetDatabase.CreateAsset(m, Out + "/kepy_trawy.mat"); mats["kępy instancje"] = m;
        var dane = ScriptableObject.CreateInstance<BraxClaudeTrawaDane>(); dane.kepy = list.ToArray(); dane.y = ys.ToArray(); dane.wysokosc = .32f; dane.granicaLaki = .15f;
        AssetDatabase.CreateAsset(dane, Out + "/kepy_dane.asset");
        var go = new GameObject("Trawa 3D (kępy, instancje)"); go.transform.SetParent(Group(group), false);
        var tr = go.AddComponent<BraxClaudeTrawa>(); tr.dane = dane; tr.siatka = KepaMesh(); tr.material = m;
        rep.notes.Add("trawa 3D: " + list.Count + " kęp — murawa " + nMur + ", łąka " + nLaka + " (kandydatów " + kepyKand.Count + " + " + lakaKand.Count + "), karty " + nCells
                      + ", średnia kart liniowo " + cm.ToString("F3") + " → odcień kart " + ((Vector3)tip).ToString("F2"));
        kepyKand.Clear(); lakaKand.Clear();
    }
    // Kwiaty rzadko, plamami (zlecenie: nigdy kolorowe klocki na całym trawniku): 1 plama na ~400 m².
    static void FlowerPatches(float x0, float x1, float z0, float z1)
    {
        group = "Trawnik: kwiaty";
        var rnd = new System.Random(5151);
        int patches = Mathf.RoundToInt((x1 - x0) * (z1 - z0) / 400f);
        for (int k = 0, tries = 0; k < patches && tries < patches * 6; tries++)
        {
            float px = x0 + (float)rnd.NextDouble() * (x1 - x0), pz = z0 + (float)rnd.NextDouble() * (z1 - z0);
            if (IsHard(px, pz, 1.2f) || SoilAt(px, pz) > .4f) continue;
            FlowerPatch(new Vector3(px, 0, pz), .9f, 5 + rnd.Next(6)); k++;
        }
        rep.notes.Add("kwiaty: " + rep.flowerPatches + " plam, " + rep.flowerPlants + " roślin");
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
                      bool garageOn = true, bool fencesOn = true, string label = null, bool forSale = false, bool leftFence = true)
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
        // wille na sprzedaż (właściciel 16:54: „naprawdę ekskluzywne, wow, w kamieniach … nie muszą być jednakowo bogate”):
        // 1 — najlepsza: parter z kamienia, piętro z ciemnego drewna, dach z dachówki (wariant 2), większy basen, boisko do siatkówki;
        // 2 — cała biała: biały tynk, kamienny cokół, dach z łupka (wariant 1); 3 — nowoczesna: antracyt + ciepłe drewno, płaski dach (wariant 0)
        int saleNo = forSale ? rep.forSale % 3 : -1;
        if (forSale) variant = new[] { 2, 1, 0 }[saleNo];
        var wallG = variant == 0 ? Stucco : variant == 1 ? Stone : Clinker;
        var wallU = variant == 0 ? Wood : variant == 1 ? Stucco : Sand;
        if (forSale) { wallG = new[] { Stone, Stucco, Anthracite }[saleNo]; wallU = new[] { WoodDark, Stucco, Wood }[saleNo]; }

        // parter + cokół (willa na sprzedaż: ściany z otworami niżej)
        if (!forSale) BoxL(wallG, h0, h1, 0, 3.4f, f0, f1);
        if (!forSale) BoxL(Plinth, h0 - .04f, h1 + .04f, 0, .35f, f0 - .04f, f1 + .04f, false);   // willa na sprzedaż: cokół obwodowy niżej (pełny blok zakryłby parkiet)
        // piętro przesunięte i wysunięte nad wejście (wymiary potrzebne już przy stropodachu)
        float uw = Wm * .62f, uA = h0, uB = uA + uw, e0 = 5.4f, e1 = 14.6f, am = (uA + uB) * .5f;
        // willa na sprzedaż (build 5): piętro do wejścia — otwór schodów w stropie a uA+4.9…uA+6.1, w 8.3…13.6;
        // podłoga piętra i tarasu na 3.5, sufit piętra 6.4 (2.9 m w świetle)
        float sa0 = uA + 4.9f, sa1 = uA + 6.1f; const float sw0 = 8.3f, sw1 = 13.6f, Y2 = 3.5f, C2 = 6.4f;
        // stropodach parteru: attyka z obróbką + żwir (willa na sprzedaż: bez attyki frontu pod piętrem — tam jest hol)
        BoxL(wallG, forSale ? uB : h0, h1, 3.4f, 3.75f, f0, f0 + .25f, false); BoxL(wallG, h0, h1, 3.4f, 3.75f, f1 - .25f, f1, false);
        BoxL(wallG, h0, h0 + .25f, 3.4f, 3.75f, f0, f1, false); BoxL(wallG, h1 - .25f, h1, 3.4f, 3.75f, f0, f1, false);
        if (!forSale)
        {
            BoxL(Frame, h0 - .03f, h1 + .03f, 3.75f, 3.8f, f0 - .03f, f1 + .03f, false);
            BoxL(Gravel, h0 + .25f, h1 - .25f, 3.4f, 3.46f, f0 + .25f, f1 - .25f, false);
            BoxL(wallU, uA, uB, 3.46f, 6.7f, e0, e1, true, true);
            BoxL(Plinth, uA - .03f, uB + .03f, 3.46f, 3.62f, e0 - .03f, e1 + .03f, false);   // gzyms między kondygnacjami
        }
        else
        {
            // obróbka tylko na murkach attyki (pełna płyta 3.75–3.8 zakrywała deskę tarasu), żwir wokół otworu schodów,
            // podbitka wysunięcia piętra nad wejściem, gzyms jako opaska na zewnątrz ścian piętra (przerwa na drzwi tarasu)
            BoxL(Frame, uB - .03f, h1 + .03f, 3.75f, 3.8f, f0 - .03f, f0 + .28f, false); BoxL(Frame, h0 - .03f, h1 + .03f, 3.75f, 3.8f, f1 - .28f, f1 + .03f, false);
            BoxL(Frame, h1 - .28f, h1 + .03f, 3.75f, 3.8f, f0 + .28f, f1 - .28f, false); BoxL(Frame, h0 - .03f, h0 + .28f, 3.75f, 3.8f, e1, f1 - .28f, false);
            foreach (var (ga0, ga1, gw0, gw1) in new[] { (h0 + .25f, sa0, f0 + .25f, f1 - .25f), (sa1, h1 - .25f, f0 + .25f, f1 - .25f), (sa0, sa1, f0 + .25f, sw0), (sa0, sa1, sw1, f1 - .25f) })
                BoxL(Gravel, ga0, ga1, 3.4f, 3.46f, gw0, gw1, false);
            BoxL(wallU, uA, uB, 3.2f, 3.49f, e0, f0, false, true);
            BoxL(Plinth, uA - .03f, uB + .03f, 3.46f, 3.62f, e0 - .03f, e0, false); BoxL(Plinth, uA - .03f, uB + .03f, 3.46f, 3.62f, e1, e1 + .03f, false);
            BoxL(Plinth, uA - .03f, uA, 3.46f, 3.62f, e0, e1, false);
            BoxL(Plinth, uB, uB + .03f, 3.46f, 3.62f, e0, e0 + 2.0f, false); BoxL(Plinth, uB, uB + .03f, 3.46f, 3.62f, e0 + 4.4f, e1, false);
        }
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
            BoxL(Frame, g0 + .55f, g1 - .55f, 2.45f, 2.6f, f0 - .1f, f0, false, true);                          // nadproże (ze spodem)
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
        void PaneSide(float w0, float w1, float y0, float y1, float a, float outward, Material gm = null)
        {
            float aa = a + outward * .015f, ab = a - outward * .03f;
            BoxL(gm ? gm : Glass, Mathf.Min(aa, ab), Mathf.Max(aa, ab), y0, y1, w0, w1, false);
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
        // sufit, lampy wiszące; tabliczka NA SPRZEDAŻ przy furtce. Build 5: piętro do wejścia (schody, hol, siłownia,
        // sypialnia, łazienka), taras na dachu, wieża grająca w salonie.
        if (forSale)
        {
            const float wt = .3f, FY = .05f, CH = 3.15f;
            var paint = InteriorPaint;
            void ColL(float a0, float a1, float y0, float y1, float w0, float w1)
            {
                Vector3 p = Pw(a0, y0, w0), q = Pw(a1, y1, w1);
                Collider((p + q) * .5f, new Vector3(Mathf.Abs(q.x - p.x), Mathf.Abs(q.y - p.y), Mathf.Abs(q.z - p.z)));
            }
            void Seg(float a0, float a1, float y0, float y1, float w0, float w1, float ia0, float ia1, float iw0, float iw1, bool solid = true, bool bottom = false)
            {
                BoxL(wallG, a0, a1, y0, y1, w0, w1, solid, bottom);                 // bottom: nadproże nad otworem (spód widać od dołu)
                BoxL(paint, ia0, ia1, Mathf.Max(y0, FY), Mathf.Min(y1, CH), iw0, iw1, false, bottom);
            }
            // boki
            Seg(h0, h0 + wt, 0, 3.4f, f0, f1, h0 + wt, h0 + wt + .02f, f0 + wt, f1 - wt);
            Seg(h1 - wt, h1, 0, 3.4f, f0, f1, h1 - wt - .02f, h1 - wt, f0 + wt, f1 - wt);
            // front: pełne, drzwi (otwarte), pełne, przeszklenie (parapet + nadproże), pełne
            Seg(h0, door0, 0, 3.4f, f0, f0 + wt, h0 + wt, door0, f0 + wt, f0 + wt + .02f);
            Seg(door0, door1, 2.45f, 3.4f, f0, f0 + wt, door0, door1, f0 + wt, f0 + wt + .02f, false, true);
            Seg(door1, gl0, 0, 3.4f, f0, f0 + wt, door1, gl0, f0 + wt, f0 + wt + .02f);
            Seg(gl0, gl1, 0, .4f, f0, f0 + wt, gl0, gl1, f0 + wt, f0 + wt + .02f);
            Seg(gl0, gl1, 2.9f, 3.4f, f0, f0 + wt, gl0, gl1, f0 + wt, f0 + wt + .02f, false, true);
            Seg(gl1, h1, 0, 3.4f, f0, f0 + wt, gl1, h1 - wt, f0 + wt, f0 + wt + .02f);
            ColL(gl0, gl1, .4f, 2.9f, f0 - .05f, f0 + .05f);
            // tył: słupy przy narożnikach, nadproże i próg; szyby z kolizją, środkowe skrzydło otwarte
            Seg(h0, h0 + 1f, 0, 3.4f, f1 - wt, f1, h0 + wt, h0 + 1f, f1 - wt - .02f, f1 - wt);
            Seg(h1 - 1f, h1, 0, 3.4f, f1 - wt, f1, h1 - 1f, h1 - wt, f1 - wt - .02f, f1 - wt);
            Seg(h0 + 1f, h1 - 1f, 3.0f, 3.4f, f1 - wt, f1, h0 + 1f, h1 - 1f, f1 - wt - .02f, f1 - wt, false, true);
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
            // sufit z otworem schodów (a sa0…sa1, w sw0…sw1); boki otworu malowane od sufitu do podłogi piętra
            BoxL(ceil, h0 + wt, sa0, CH, CH + .05f, f0 + wt, f1 - wt, false, true); BoxL(ceil, sa1, h1 - wt, CH, CH + .05f, f0 + wt, f1 - wt, false, true);
            BoxL(ceil, sa0, sa1, CH, CH + .05f, f0 + wt, sw0, false, true); BoxL(ceil, sa0, sa1, CH, CH + .05f, sw1, f1 - wt, false, true);
            BoxL(paint, sa0 - .02f, sa0, CH, Y2 - .005f, sw0, sw1, false); BoxL(paint, sa1, sa1 + .02f, CH, Y2 - .005f, sw0, sw1, false);
            BoxL(paint, sa0, sa1, CH, Y2 - .005f, sw0 - .02f, sw0, false); BoxL(paint, sa0, sa1, CH, Y2 - .005f, sw1, sw1 + .02f, false);
            // kuchnia przy lewej ścianie: szafki dolne z blatem, górne, lodówka, wyspa
            var front_ = Plain("fronty kuchni grafit mat", new Color(.22f, .23f, .24f), .35f);
            float ka = h0 + wt, kw0 = f0 + 1.6f, kw1 = f0 + 6.4f;
            BoxL(front_, ka, ka + .6f, FY, .9f, kw0, kw1); BoxL(Concrete, ka, ka + .64f, .9f, .94f, kw0, kw1, false);
            BoxL(front_, ka, ka + .35f, 1.5f, 2.25f, kw0, kw1, false);
            for (float w = kw0 + .6f; w < kw1 - .1f; w += .6f) { BoxL(Steel, ka + .6f, ka + .62f, .2f, .75f, w - .01f, w + .01f, false); BoxL(Steel, ka + .35f, ka + .37f, 1.6f, 2.1f, w - .01f, w + .01f, false); }
            BoxL(Steel, ka, ka + .7f, FY, 2.1f, kw1, kw1 + .75f);
            int sn = rep.forSale;   // 0, 1, 2 — każda z trzech willi na sprzedaż ma własny zestaw kolorów
            var marbleTop = Plain("marmur biały blatów", new Color(.90f, .89f, .87f), .78f);
            BoxL(WoodDark, ka + 2.0f, ka + 3.0f, FY, .9f, kw0 + .6f, kw1 - .8f); BoxL(marbleTop, ka + 1.9f, ka + 3.1f, .9f, .95f, kw0 + .5f, kw1 - .7f, false, true);
            // jadalnia i salon (meble Poly Haven), dywan, lampy wiszące
            float Yaw(Vector3 dirW) => Mathf.Atan2(dirW.x, dirW.z) * Mathf.Rad2Deg;
            float da = h0 + Wm * .42f, dw = f1 - 3.2f;
            var tableGo = diningTable ? Put(diningTable, Pw(da, FY, dw), Yaw(Wax), .03f, true) : null;   // dłuższy bok stołu (x modelu) wzdłuż a, krzesła przy długich bokach
            if (offChair) foreach (var (oa, ow, fdir) in new[] { (-.55f, -.85f, 1f), (.55f, -.85f, 1f), (-.55f, .85f, -1f), (.55f, .85f, -1f) })
                    Put(offChair, Pw(da + oa, FY, dw + ow), Yaw(Wax * fdir), .03f, false);
            var rug = new[] { Plain("dywan salonu granat", new Color(.08f, .12f, .28f), .05f), Plain("dywan salonu szmaragd", new Color(.05f, .24f, .18f), .05f), Plain("dywan salonu bordo", new Color(.32f, .05f, .09f), .05f) }[sn % 3];
            float la = h1 - wt - 2.6f, lw = f0 + 5.2f;
            Vector3 r0 = Pw(la - 1.7f, 0, lw - 1.3f), r1 = Pw(la + 1.7f, 0, lw + 1.3f);
            Top(rug, Mathf.Min(r0.x, r1.x), Mathf.Max(r0.x, r1.x), Mathf.Min(r0.z, r1.z), Mathf.Max(r0.z, r1.z), FY + .01f);
            var tvCab = tvCabinet ? Put(tvCabinet, Pw(h1 - wt - .3f, FY, lw), Yaw(-Aax), .03f, true) : null;
            // ściana akcentowa za telewizorem (4.4 m × do sufitu): drewno / kamień / ciemne drewno — każda willa inna
            var accent = new[] { Wood, Stone, WoodDark }[sn % 3];
            BoxL(accent, h1 - wt - .04f, h1 - wt - .01f, FY, CH, lw - 2.2f, lw + 2.2f, false);
            // płaski telewizor 65" na ścianie: ramka 1.64 × 0.94 m (4 cm grubości), ekran szklany ciemny, listwa dźwiękowa pod spodem
            BoxL(Plain("telewizor ramka", new Color(.02f, .02f, .022f), .6f), h1 - wt - .09f, h1 - wt - .05f, 1.13f, 2.07f, lw - .82f, lw + .82f, false);
            BoxL(Plain("telewizor ekran", new Color(.012f, .016f, .024f), .95f), h1 - wt - .1f, h1 - wt - .09f, 1.16f, 2.04f, lw - .79f, lw + .79f, false);
            BoxL(Plain("listwa dźwiękowa", new Color(.04f, .04f, .045f), .4f), h1 - wt - .14f, h1 - wt - .05f, .98f, 1.06f, lw - .55f, lw + .55f, false, true);
            if (sofaLux || offSofa) ForceOpaque(Put(sofaLux ? sofaLux : offSofa, Pw(la - 2.0f, FY, lw), Yaw(Aax), .03f, true));   // build 4: sofa_03 była przezroczysta (brązowe „szkło”)
            if (offArm) Put(offArm, Pw(la - .5f, FY, lw + 1.9f), Yaw(-Wax), .03f, true);
            if (coffeeTable) Put(coffeeTable, Pw(la - .7f, FY, lw), Yaw(Wax), .03f, true);
            if (offLamp) foreach (var (pa, pw2) in chandelier ? new[] { (ka + 2.5f, (kw0 + kw1) * .5f) } : new[] { (ka + 2.5f, (kw0 + kw1) * .5f), (da, dw) })
                {
                    var o = Put(offLamp, Pw(pa, 0, pw2), 0f, .02f, false);
                    if (o) { var lb = RB(o); o.transform.position += Vector3.up * (CH - lb.max.y); }
                }
            // ---- luksus (właściciel 31.10: „trzy wille do kupienia, w środku elegancko, naprawdę luksusowe, z basenem i jacuzzi”) ----
            // salon: fotel klubowy z podnóżkiem przy oknie, rośliny w donicach, obraz nad jadalnią (abstrakcja w kolorach willi),
            // trzecia lampa nad salonem; kuchnia: marmurowy blat wyspy i 3 hokery (chrom + skóra); jadalnia: 6 krzeseł
            var leather = new[] { Plain("skóra koniak", new Color(.45f, .25f, .12f), .45f), Plain("skóra kremowa", new Color(.85f, .80f, .70f), .45f), Plain("skóra czarna", new Color(.08f, .08f, .09f), .5f) }[sn % 3];
            var chrome = Plain("chrom", new Color(.78f, .79f, .80f), .92f, 1f);
            if (lounge) Put(lounge, Pw(la + .9f, FY, lw - 1.9f), Yaw(Wax), .03f, true);
            if (ottoman) Put(ottoman, Pw(la + .9f, FY, lw - .95f), Yaw(Wax), .03f, true);
            // rośliny: potted_plant_04 ma 27 cm (audyt) — na podłodze ×2.4 (65 cm, duży sukulent w donicy), na blacie kuchni bez skalowania
            GameObject BigPlant(Vector3 at, float yaw)
            {
                var o = Put(plantPot, at, yaw, .03f, false); if (!o) return null;
                o.transform.localScale *= 2.4f; var b = RB(o); o.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
                b = RB(o); Collider(b.center, b.size); return o;
            }
            if (plantPot) { BigPlant(Pw(h1 - wt - .55f, FY, f0 + wt + .55f), 20f); BigPlant(Pw(da + 2.4f, FY, f1 - wt - .55f), 70f); Put(plantPot, Pw(ka + .35f, .94f, kw0 + .35f), 0f, .03f, false); }
            Sign("obraz" + (sn % 3 + 1), Pw(h0 + wt + .08f, 1.75f, 16.0f), 2.4f, 1.2f, Aax);
            foreach (float ow in new[] { -.9f, 0f, .9f })
            {
                var sc = Pw(ka + 3.45f, FY, (kw0 + kw1) * .5f - .1f + ow);
                Cylinder(chrome, sc, .2f, .03f, 16, true, false);
                Cylinder(chrome, sc + Vector3.up * .03f, .03f, .7f, 8, false, false);
                Cylinder(leather, sc + Vector3.up * .73f, .19f, .07f, 16, true, false);
                Collider(sc + Vector3.up * .4f, new Vector3(.4f, .8f, .4f));
            }
            if (offChair) { Put(offChair, Pw(da - 1.42f, FY, dw), Yaw(Aax), .03f, false); Put(offChair, Pw(da + 1.42f, FY, dw), Yaw(-Aax), .03f, false); }   // audyt: przy 1.3 wchodziły 12 cm w obrus
            if (offLamp) { var o = Put(offLamp, Pw(la - .7f, 0, lw), 0f, .02f, false); if (o) { var lb = RB(o); o.transform.position += Vector3.up * (CH - lb.max.y); } }
            if (chandelier) { var o = Put(chandelier, Pw(da, 0, dw), 0f, .02f, false); if (o) { var lb = RB(o); o.transform.position += Vector3.up * (CH - lb.max.y); } }   // żyrandol nad stołem
            // build 5 (galeria: „ciemne wnętrza”): ciepłe światło pod lampami kuchni i salonu i pod żyrandolem (ForceVertex, STANDARD §5)
            foreach (var (pa, pw2) in new[] { (ka + 2.5f, (kw0 + kw1) * .5f), (la - .7f, lw), (da, dw) }) PointLamp(Pw(pa, CH - .9f, pw2), 7f, .9f, new Color(1f, .88f, .72f));
            // konsola pod obrazem (lewa ściana), na niej ramka ze zdjęciem i wazon z kwiatami; wazon z kwiatami na stole; misa na wyspie
            void Bouquet(Vector3 at, float r)
            {
                if (flowerNodes.Count == 0) return;
                for (int q = 0; q < 6; q++)
                {
                    float an = q * 1.047f + sn;
                    PlantPart(flowerNodes[(q * 7 + sn * 3) % flowerNodes.Count], at + new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)) * r * (q % 2 == 0 ? .4f : .9f), .7f, q * 60f, .02f);
                }
            }
            if (console)
            {
                const float ca = .32f;   // konsola ma 0.59 m głębokości: środek 0.32 m od ściany (audyt: przy 0.25 tył wchodził 6.5 cm w ścianę)
                var con = Put(console, Pw(h0 + wt + ca, FY, 16.0f), Yaw(Aax), .03f, true);
                if (con)
                {
                    float topF = SurfaceTop(con, Pw(h0 + wt + ca, 0, 15.45f), RB(con).max.y), topV = SurfaceTop(con, Pw(h0 + wt + ca, 0, 16.55f), RB(con).max.y);
                    if (photoFrame) Put(photoFrame, Pw(h0 + wt + ca, topF, 15.45f), Yaw(Aax) + 15f, .02f, false);
                    if (vase) { var v = Put(vase, Pw(h0 + wt + ca, topV, 16.55f), 0f, .02f, false); if (v) Bouquet(Pw(h0 + wt + ca, RB(v).max.y - .06f, 16.55f), .05f); }
                }
            }
            if (tableGo && vase)
            {
                float tableTop = SurfaceTop(tableGo, Pw(da, 0, dw), RB(tableGo).max.y);   // blat w środku stołu (promień), nie fałda obrusa
                var v = Put(vase, Pw(da, tableTop, dw), 0f, .02f, false);
                // willa 1 (właściciel 16:44): bukiet czerwonych róż na stole i karteczka „Kocham cię, Monika” przodem do wejścia z tarasu
                if (v && sn == 0) { RoseBouquet(Pw(da, RB(v).max.y - .03f, dw), 22, 31); LoveCard(Pw(da, SurfaceTop(tableGo, Pw(da, 0, dw + .35f), tableTop), dw + .35f), Wax); }
                else if (v) Bouquet(Pw(da, RB(v).max.y - .06f, dw), .05f);
            }
            if (fruitBowl) Put(fruitBowl, Pw(ka + 2.5f, .95f, (kw0 + kw1) * .5f), 30f, .02f, false);

            // ---- PIĘTRO (build 5, właściciel 18:01–18:08: „na te wille, co pisaliśmy”; ZYCIE-PARY §3: łóżko, szafa, trofea,
            // lustro, domowa siłownia w willi). Układ (a od uA, w = głębokość działki):
            //   hol z oknem na całą szerokość w 5.7–8.3; otwór schodów a 4.9–6.1, w 8.3–13.6 (balustrady szklane);
            //   łazienka a 0.3–4.75, w 8.45–14.3; siłownia a 6.1–10.86, w 5.7–10.2 (wyjście na taras na dachu);
            //   sypialnia a 6.25–10.86, w 10.35–14.3. Podłoga 3.5, sufit 6.4.
            {
                var marble = Pbr("marmur łazienki", "marble_01", 2.0f, new Color(.97f, .96f, .95f), .9f);
                var porcelain = Plain("porcelana biała", new Color(.95f, .95f, .94f), .85f);
                var mirrorM = Plain("lustro", new Color(.80f, .82f, .84f), .97f, 1f);
                var blackLeather = Plain("skóra czarna ławki", new Color(.05f, .05f, .055f), .45f);
                var gold = Plain("złoto pucharów", new Color(.83f, .64f, .24f), .85f, 1f);
                var blackStone = Plain("czarny marmur cokołów", new Color(.04f, .04f, .045f), .8f);
                void TopL(Material m, float a0, float a1, float w0, float w1, float y)
                {
                    Vector3 p = Pw(a0, 0, w0), q = Pw(a1, 0, w1);
                    Top(m, Mathf.Min(p.x, q.x), Mathf.Max(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.z, q.z), y);
                }
                // ściana zewnętrzna piętra (z kolizją) + lico wewnętrzne malowane w pasie podłoga–sufit
                void SegU(float a0, float a1, float y0, float y1, float w0, float w1, float ia0, float ia1, float iw0, float iw1, bool bottom = false)
                {
                    BoxL(wallU, a0, a1, y0, y1, w0, w1, true, bottom);
                    float iy0 = Mathf.Max(y0, Y2), iy1 = Mathf.Min(y1, C2);
                    if (iy1 > iy0) BoxL(paint, ia0, ia1, iy0, iy1, iw0, iw1, false, bottom);
                }
                // ścianka działowa 15 cm (farba z obu stron, kolizja)
                void Wall(float a0, float a1, float w0, float w1, float y0 = Y2, float y1 = C2, bool bottom = false) => BoxL(paint, a0, a1, y0, y1, w0, w1, true, bottom);
                float fI = e0 + wt, bI = e1 - wt, lI = uA + wt, rI = uB - wt;     // lica wewnętrzne ścian zewnętrznych piętra
                // przód: wstęga okien a uA+0.8…uB-0.8, y 4.1–6.15
                SegU(uA, uA + .8f, 3.46f, 6.7f, e0, fI, lI, uA + .8f, fI, fI + .02f);
                SegU(uB - .8f, uB, 3.46f, 6.7f, e0, fI, uB - .8f, rI, fI, fI + .02f);
                SegU(uA + .8f, uB - .8f, 3.46f, 4.1f, e0, fI, uA + .8f, uB - .8f, fI, fI + .02f);
                SegU(uA + .8f, uB - .8f, 6.15f, 6.7f, e0, fI, uA + .8f, uB - .8f, fI, fI + .02f, true);
                // tył: dwa okna
                float r0a = uA + .9f, r0b = uA + uw * .45f, r1a = uA + uw * .55f, r1b = uB - .9f;
                foreach (var (pa, pb) in new[] { (uA, r0a), (r0b, r1a), (r1b, uB) })
                    SegU(pa, pb, 3.46f, 6.7f, bI, e1, Mathf.Max(pa, lI), Mathf.Min(pb, rI), bI - .02f, bI);
                foreach (var (pa, pb) in new[] { (r0a, r0b), (r1a, r1b) })
                {
                    SegU(pa, pb, 3.46f, 4.1f, bI, e1, pa, pb, bI - .02f, bI);
                    SegU(pa, pb, 6.15f, 6.7f, bI, e1, pa, pb, bI - .02f, bI, true);
                }
                // lewy bok: dwa okna, y 4.3–6.0
                foreach (var (pw0, pw1) in new[] { (fI, e0 + 1.6f), (e0 + 2.6f, e1 - 3.2f), (e1 - 1.4f, bI) })
                    SegU(uA, lI, 3.46f, 6.7f, pw0, pw1, lI, lI + .02f, pw0, pw1);
                foreach (var (pw0, pw1) in new[] { (e0 + 1.6f, e0 + 2.6f), (e1 - 3.2f, e1 - 1.4f) })
                {
                    SegU(uA, lI, 3.46f, 4.3f, pw0, pw1, lI, lI + .02f, pw0, pw1);
                    SegU(uA, lI, 6.0f, 6.7f, pw0, pw1, lI, lI + .02f, pw0, pw1, true);
                }
                // prawy bok: drzwi na taras (od podłogi do 6.0), próg
                foreach (var (pw0, pw1) in new[] { (fI, e0 + 2.0f), (e0 + 4.4f, bI) })
                    SegU(rI, uB, 3.46f, 6.7f, pw0, pw1, rI - .02f, rI, pw0, pw1);
                SegU(rI, uB, 6.0f, 6.7f, e0 + 2.0f, e0 + 4.4f, rI - .02f, rI, e0 + 2.0f, e0 + 4.4f, true);
                BoxL(Frame, rI, uB, 3.46f, Y2 + .015f, e0 + 2.0f, e0 + 4.4f, false);
                // kolizje szyb piętra (otwarte skrzydło drzwi tarasu bez kolizji)
                ColL(uA + .8f, uB - .8f, 4.1f, 6.15f, e0 - .05f, e0 + .05f);
                ColL(r0a, r0b, 4.1f, 6.15f, e1 - .05f, e1 + .05f); ColL(r1a, r1b, 4.1f, 6.15f, e1 - .05f, e1 + .05f);
                ColL(uA - .05f, uA + .05f, 4.3f, 6.0f, e0 + 1.6f, e0 + 2.6f); ColL(uA - .05f, uA + .05f, 4.3f, 6.0f, e1 - 3.2f, e1 - 1.4f);
                ColL(uB - .12f, uB + .05f, Y2, 6.0f, e0 + 2.0f, e0 + 3.2f);
                // podłoga piętra, dachu i tarasu: kolizje 3.2–3.5 wokół otworu schodów (dach płaski za piętrem też do chodzenia)
                ColL(uA, sa0, 3.2f, Y2, e0, f1); ColL(sa1, uB, 3.2f, Y2, e0, f1); ColL(uB, h1, 3.2f, Y2, f0, f1);
                ColL(sa0, sa1, 3.2f, Y2, e0, sw0); ColL(sa0, sa1, 3.2f, Y2, sw1, f1);
                TopL(floorW, lI, rI, fI, sw0, Y2); TopL(floorW, sa1, rI, sw0, bI, Y2); TopL(floorW, sa0, sa1, sw1, bI, Y2);
                TopL(marble, lI, sa0, sw0, bI, Y2);
                BoxL(ceil, lI, rI, C2, C2 + .05f, fI, bI, false, true);
                // ścianki: łazienka (drzwi z holu a uA+1.6…2.5), ściana łazienki przy schodach; sypialnia (od w 10.2, drzwi a uA+6.45…7.35)
                const float bw = 10.2f;
                Wall(lI, uA + 1.6f, sw0, sw0 + .15f); Wall(uA + 2.5f, sa0, sw0, sw0 + .15f); Wall(uA + 1.6f, uA + 2.5f, sw0, sw0 + .15f, 5.6f, C2, true);
                Wall(sa0 - .15f, sa0, sw0 + .15f, bI);
                Wall(sa1, sa1 + .15f, bw, bI);
                Wall(sa1 + .15f, uA + 6.45f, bw, bw + .15f); Wall(uA + 7.35f, rI, bw, bw + .15f); Wall(uA + 6.45f, uA + 7.35f, bw, bw + .15f, 5.6f, C2, true);
                foreach (var (oa0, oa1, ow0, ow1) in new[] { (uA + 1.6f, uA + 2.5f, sw0, sw0 + .15f), (uA + 6.45f, uA + 7.35f, bw, bw + .15f) })
                {
                    BoxL(WoodDark, oa0 - .06f, oa0, Y2, 5.66f, ow0 - .02f, ow1 + .02f, false); BoxL(WoodDark, oa1, oa1 + .06f, Y2, 5.66f, ow0 - .02f, ow1 + .02f, false);
                    BoxL(WoodDark, oa0 - .06f, oa1 + .06f, 5.6f, 5.66f, ow0 - .02f, ow1 + .02f, false, true);
                }
                // balustrady otworu na piętrze: szkło 1.0 m + pochwyt (od strony siłowni do ściany sypialni i na końcu otworu)
                BoxL(GlassClear, sa1 - .01f, sa1 + .01f, Y2, Y2 + 1.0f, sw0, bw, false); BoxL(chrome, sa1 - .025f, sa1 + .025f, Y2 + 1.0f, Y2 + 1.05f, sw0, bw, false, true);
                BoxL(GlassClear, sa0, sa1, Y2, Y2 + 1.0f, sw1 - .01f, sw1 + .01f, false); BoxL(chrome, sa0, sa1, Y2 + 1.0f, Y2 + 1.05f, sw1 - .025f, sw1 + .025f, false, true);
                ColL(sa1 - .03f, sa1 + .03f, Y2, Y2 + 1.05f, sw0, bw); ColL(sa0, sa1, Y2, Y2 + 1.05f, sw1 - .03f, sw1 + .03f);

                // SCHODY: 19 podniesień po 18.2 cm, 18 stopni po 29.4 cm; dąb 6 cm na stalowych policzkach, szkło po obu stronach
                // z pochwytem; pod spodem rampa (kolizja) przez połowy podniesień — kąt 31.7°, gracz wchodzi płynnie
                {
                    const int nR = 19; float rr = (Y2 - FY) / nR, tt = (sw1 - sw0) / (nR - 1);
                    var tread = new[] { WoodDark, Wood, Wood }[sn % 3];
                    for (int i = 0; i < nR - 1; i++)
                    {
                        float ty = FY + (i + 1) * rr, wHi = sw1 - i * tt;
                        BoxL(tread, sa0 + .06f, sa1 - .06f, ty - .06f, ty, wHi - tt - .02f, wHi, false, true);
                    }
                    Vector3 n0 = Pw(sa0, FY + rr, sw1), n1 = Pw(sa0, Y2, sw0);           // linia nosków
                    var sq = Quaternion.LookRotation((n1 - n0).normalized, Vector3.up); float sl = (n1 - n0).magnitude;
                    Vector3 nUp = sq * Vector3.up;
                    foreach (float sa in new[] { sa0 + .05f, sa1 - .05f })
                    {
                        Vector3 m0 = (Pw(sa, FY + rr, sw1) + Pw(sa, Y2, sw0)) * .5f;
                        OBoxQ(Steel, m0 - Vector3.up * .17f, new Vector3(.04f, .3f, sl), sq);                       // policzek
                        OBoxQ(GlassClear, m0 + nUp * .55f, new Vector3(.02f, 1.0f, sl), sq, true);                    // szkło z kolizją
                        OBoxQ(chrome, m0 + nUp * 1.07f, new Vector3(.05f, .05f, sl), sq);                             // pochwyt
                    }
                    Vector3 q0 = Pw((sa0 + sa1) * .5f, FY, sw1 + tt * .5f), q1 = Pw((sa0 + sa1) * .5f, Y2, sw0 - tt * .5f);
                    var rq = Quaternion.LookRotation((q1 - q0).normalized, Vector3.up);
                    var ramp = new GameObject("kolizja schody"); ramp.transform.SetParent(colliders, false);
                    ramp.transform.position = (q0 + q1) * .5f - (rq * Vector3.up) * .125f; ramp.transform.rotation = rq;
                    ramp.AddComponent<BoxCollider>().size = new Vector3(sa1 - sa0 - .12f, .25f, (q1 - q0).magnitude);
                    if (plantPot) BigPlant(Pw((sa0 + sa1) * .5f, FY, sw0 + .5f), 40f);                          // pod najwyższymi stopniami
                }

                // SIŁOWNIA DOMOWA: mata gumowa, worek bokserski na łańcuchu, stojak z 10 parami hantli, ławka płaska, lustro
                {
                    var rubber = Plain("mata gumowa siłowni", new Color(.05f, .05f, .055f), .15f);
                    var bagLeather = Pbr("skóra worka", "leather_red_02", .9f, new Color(.95f, .95f, .95f), .6f);
                    var hexM = Plain("guma hantli", new Color(.07f, .07f, .075f), .3f);
                    TopL(rubber, uA + 6.45f, rI - .05f, fI + .1f, 7.6f, Y2 + .012f);
                    var bag = Pw(uA + 7.25f, 0, 6.65f);
                    Cylinder(bagLeather, new Vector3(bag.x, Y2 + .45f, bag.z), .18f, 1.35f, 20, true, false);
                    Collider(new Vector3(bag.x, Y2 + 1.1f, bag.z), new Vector3(.36f, 1.35f, .36f));
                    Cylinder(Steel, new Vector3(bag.x, Y2 + 1.8f, bag.z), .012f, C2 - Y2 - 1.85f, 6, false, false);
                    Cylinder(Steel, new Vector3(bag.x, C2 - .05f, bag.z), .06f, .05f, 12, false, false);
                    float ra0 = rI - .6f, rw0 = fI + .2f, rw1 = e0 + 1.85f;
                    BoxL(Steel, ra0, rI - .02f, Y2, Y2 + .05f, rw0, rw1, false);
                    foreach (float pw in new[] { rw0 + .03f, rw1 - .03f }) BoxL(Steel, ra0 + .2f, ra0 + .26f, Y2, Y2 + .82f, pw - .03f, pw + .03f, false);
                    foreach (var (ty, tlo) in new[] { (Y2 + .38f, ra0 + .05f), (Y2 + .78f, ra0 + .2f) })
                    {
                        BoxL(Steel, tlo, rI - .05f, ty - .03f, ty, rw0, rw1, false, true);
                        float ac = (tlo + rI - .05f) * .5f;
                        for (int k = 0; k < 5; k++)
                        {
                            float pw = rw0 + .14f + k * (rw1 - rw0 - .28f) / 4f, pr = .05f + k * .008f + (ty > Y2 + .5f ? 0f : .012f);
                            BoxL(hexM, ac - .15f, ac - .08f, ty, ty + 2 * pr, pw - pr, pw + pr, false);
                            BoxL(hexM, ac + .08f, ac + .15f, ty, ty + 2 * pr, pw - pr, pw + pr, false);
                            Tube(chrome, Pw(ac - .08f, ty + pr, pw), Pw(ac + .08f, ty + pr, pw), .016f, 8);
                        }
                    }
                    ColL(ra0, rI, Y2, Y2 + .85f, rw0, rw1);
                    BoxL(blackLeather, uA + 8.2f, uA + 9.4f, Y2 + .38f, Y2 + .48f, 6.45f, 6.75f, false);
                    foreach (float la3 in new[] { uA + 8.35f, uA + 9.25f }) BoxL(Steel, la3 - .03f, la3 + .03f, Y2, Y2 + .38f, 6.5f, 6.7f, false);
                    BoxL(Steel, uA + 8.3f, uA + 9.3f, Y2, Y2 + .04f, 6.56f, 6.64f, false);
                    ColL(uA + 8.2f, uA + 9.4f, Y2, Y2 + .48f, 6.45f, 6.75f);
                    // lustro na ścianie sypialni od strony siłowni (lustro „statystyk” z ZYCIE-PARY §1)
                    BoxL(mirrorM, uA + 7.6f, uA + 10.6f, Y2 + .1f, Y2 + 2.2f, bw - .012f, bw, false);
                    BoxL(Frame, uA + 7.57f, uA + 10.63f, Y2 + 2.2f, Y2 + 2.23f, bw - .02f, bw, false, true);
                    BoxL(Frame, uA + 7.57f, uA + 10.63f, Y2 + .07f, Y2 + .1f, bw - .02f, bw, false);
                }

                // ŚCIANA TROFEÓW (hol, lico ściany łazienki): 3 półki po 4 puchary na czarnych cokołach, nad nimi pas mistrzowski
                {
                    float ta0 = uA + 2.85f, ta1 = sa0 - .25f;
                    for (int s2 = 0; s2 < 3; s2++)
                    {
                        float sy = Y2 + .95f + s2 * .5f;
                        BoxL(WoodDark, ta0, ta1, sy - .04f, sy, sw0 - .26f, sw0, false, true);
                        for (int k = 0; k < 4; k++)
                        {
                            float ca2 = ta0 + .2f + k * (ta1 - ta0 - .4f) / 3f, hgt = .22f + ((k + s2) % 3) * .06f;
                            var cb = Pw(ca2, sy, sw0 - .13f);
                            BoxL(blackStone, ca2 - .06f, ca2 + .06f, sy, sy + .06f, sw0 - .19f, sw0 - .07f, false);
                            Cylinder(gold, cb + Vector3.up * .06f, .02f, hgt * .45f, 10, false, false);
                            Cylinder(gold, cb + Vector3.up * (.06f + hgt * .45f), .06f, hgt * .55f, 16, true, false);
                            Tube(gold, cb + Vector3.up * (.06f + hgt * .8f) + Aax * .06f, cb + Vector3.up * (.06f + hgt * .8f) + Aax * .1f, .012f, 6);
                            Tube(gold, cb + Vector3.up * (.06f + hgt * .8f) - Aax * .06f, cb + Vector3.up * (.06f + hgt * .8f) - Aax * .1f, .012f, 6);
                        }
                    }
                    float by = Y2 + 2.45f, bm = (ta0 + ta1) * .5f;
                    BoxL(blackLeather, bm - .55f, bm + .55f, by - .055f, by + .055f, sw0 - .03f, sw0, false);
                    BoxL(gold, bm - .17f, bm + .17f, by - .13f, by + .13f, sw0 - .05f, sw0 - .02f, false);
                    foreach (float off in new[] { -.36f, -.27f, .27f, .36f }) BoxL(gold, bm + off - .035f, bm + off + .035f, by - .05f, by + .05f, sw0 - .04f, sw0 - .02f, false);
                }

                // SYPIALNIA: łóżko 1.8 × 2.1 m wezgłowiem do prawej ściany (pełnej), tapicerowane wezgłowie na ścianie akcentowej,
                // pościel, poduszki, pled; szafki nocne Poly Haven z lampkami; szafa 2.7 m; ławka w nogach; dywan; obraz
                {
                    float bc = 12.3f, bA = rI - .12f;
                    var fabric = new[] { Plain("tkanina wezgłowia grafit", new Color(.18f, .18f, .2f), .15f), Plain("tkanina wezgłowia beż", new Color(.62f, .55f, .46f), .15f), Plain("tkanina wezgłowia butelkowa zieleń", new Color(.08f, .22f, .17f), .15f) }[sn % 3];
                    var linen = Plain("pościel biała", new Color(.94f, .94f, .92f), .1f);
                    var throwM = new[] { Plain("pled bordo", new Color(.38f, .06f, .1f), .1f), Plain("pled granat", new Color(.09f, .13f, .3f), .1f), Plain("pled musztarda", new Color(.68f, .48f, .12f), .1f) }[sn % 3];
                    var rug2 = new[] { Plain("dywan sypialni krem", new Color(.80f, .76f, .68f), .05f), Plain("dywan sypialni szary", new Color(.45f, .46f, .48f), .05f), Plain("dywan sypialni piasek", new Color(.72f, .62f, .48f), .05f) }[sn % 3];
                    BoxL(accent, rI - .03f, rI, Y2, C2, bc - 1.75f, bc + 1.75f, false);
                    BoxL(fabric, bA, rI - .03f, Y2 + .3f, Y2 + 1.35f, bc - 1.0f, bc + 1.0f, false, true);
                    BoxL(WoodDark, bA - 2.0f, bA, Y2 + .08f, Y2 + .36f, bc - .92f, bc + .92f, true);
                    BoxL(Frame, bA - 1.9f, bA - .1f, Y2, Y2 + .08f, bc - .8f, bc + .8f, false);
                    BoxL(linen, bA - 1.97f, bA - .02f, Y2 + .36f, Y2 + .58f, bc - .89f, bc + .89f, false);
                    BoxL(linen, bA - 2.02f, bA - .62f, Y2 + .52f, Y2 + .61f, bc - .94f, bc + .94f, false);
                    foreach (float sd in new[] { -1f, 1f }) BoxL(linen, bA - 2.02f, bA - .62f, Y2 + .3f, Y2 + .61f, bc + sd * .94f - .01f, bc + sd * .94f + .01f, false);
                    BoxL(linen, bA - 2.04f, bA - 2.02f, Y2 + .3f, Y2 + .61f, bc - .95f, bc + .95f, false);
                    BoxL(throwM, bA - 2.06f, bA - 1.5f, Y2 + .61f, Y2 + .64f, bc - .97f, bc + .97f, false);
                    foreach (float sd in new[] { -1f, 1f }) BoxL(throwM, bA - 2.06f, bA - 1.5f, Y2 + .25f, Y2 + .64f, bc + sd * .97f - .015f, bc + sd * .97f + .015f, false);
                    foreach (float sd in new[] { -.45f, .45f }) BoxL(linen, bA - .5f, bA - .08f, Y2 + .58f, Y2 + .78f, bc + sd - .38f, bc + sd + .38f, false);
                    foreach (float sd in new[] { -.42f, 0f, .42f }) BoxL(throwM, bA - .62f, bA - .48f, Y2 + .6f, Y2 + .95f, bc + sd - .19f, bc + sd + .19f, false);
                    TopL(rug2, bA - 3.0f, bA + .05f, bc - 1.55f, bc + 1.55f, Y2 + .008f);
                    foreach (float sd in new[] { -1.3f, 1.3f })
                    {
                        var ns = nightstand ? Put(nightstand, Pw(rI - .3f, Y2, bc + sd), Yaw(-Aax), .03f, true) : null;
                        float top = ns ? RB(ns).max.y : Y2 + .55f;
                        if (!ns) BoxL(WoodDark, rI - .52f, rI - .08f, Y2, Y2 + .55f, bc + sd - .24f, bc + sd + .24f, true);
                        var lp = Pw(rI - .3f, top, bc + sd);
                        Cylinder(chrome, lp, .07f, .02f, 16, true, false); Cylinder(chrome, lp, .012f, .32f, 8, false, false);
                        Cylinder(Plain("abażur len", new Color(.90f, .86f, .78f), .1f), lp + Vector3.up * .3f, .14f, .2f, 20, true, false);
                    }
                    var wardM = new[] { WoodDark, Plain("lakier biały szafy", new Color(.9f, .9f, .88f), .7f), Wood }[sn % 3];
                    float wa0 = sa1 + .15f, wa1 = wa0 + .62f, ww0 = 11.4f, ww1 = bI - .05f;
                    BoxL(wardM, wa0, wa1, Y2, Y2 + 2.45f, ww0, ww1, true);
                    for (int k = 1; k < 4; k++) { float wq = ww0 + k * (ww1 - ww0) / 4f; BoxL(Frame, wa1, wa1 + .005f, Y2 + .05f, Y2 + 2.4f, wq - .004f, wq + .004f, false); }
                    foreach (int k in new[] { 1, 3 }) { float wq = ww0 + k * (ww1 - ww0) / 4f; foreach (float off in new[] { -.07f, .07f }) BoxL(chrome, wa1, wa1 + .03f, Y2 + .9f, Y2 + 1.5f, wq + off - .012f, wq + off + .012f, false); }
                    BoxL(leather, bA - 2.62f, bA - 2.2f, Y2, Y2 + .45f, bc - .7f, bc + .7f, true);
                    Sign("obraz" + ((sn + 1) % 3 + 1), Pw(rI - .045f, Y2 + 1.95f, bc), 1.5f, .75f, -Aax, false);
                }

                // ŁAZIENKA (marmur): okładzina ścian wokół okien, prysznic walk-in w rogu za szkłem, wanna wolnostojąca Ø1.56 m pod
                // oknem z wodą, bateria stojąca, podwójna umywalka z lustrem, WC podwieszane, grzejnik-drabinka z ręcznikiem, roślina
                {
                    float ba1 = sa0 - .15f, bw0 = sw0 + .15f;
                    float ml0 = lI + .02f, ml1 = lI + .032f, mr0 = bI - .032f, mr1 = bI - .02f;   // okładzina przed licem farby
                    BoxL(marble, ml0, ml1, Y2, C2, bw0, e1 - 3.2f, false); BoxL(marble, ml0, ml1, Y2, C2, e1 - 1.4f, bI, false);
                    BoxL(marble, ml0, ml1, Y2, 4.3f, e1 - 3.2f, e1 - 1.4f, false); BoxL(marble, ml0, ml1, 6.0f, C2, e1 - 3.2f, e1 - 1.4f, false, true);
                    BoxL(marble, lI, r0a, Y2, C2, mr0, mr1, false); BoxL(marble, r0a, ba1, Y2, 4.1f, mr0, mr1, false); BoxL(marble, r0a, ba1, 6.15f, C2, mr0, mr1, false, true);
                    BoxL(marble, ba1 - .012f, ba1, Y2, C2, bw0, bI, false);
                    BoxL(marble, lI, uA + 1.6f, Y2, C2, bw0, bw0 + .012f, false); BoxL(marble, uA + 2.5f, ba1, Y2, C2, bw0, bw0 + .012f, false);
                    BoxL(marble, uA + 1.6f, uA + 2.5f, 5.6f, C2, bw0, bw0 + .012f, false, true);
                    // prysznic: posadzka grafit, szyba wzdłuż w 10.1, deszczownica pod sufitem, bateria na ścianie
                    TopL(Plain("łupek prysznica", new Color(.12f, .12f, .13f), .4f), uA + 3.25f, ba1, bw0, 10.1f, Y2 + .006f);
                    BoxL(GlassClear, uA + 3.25f, ba1, Y2, Y2 + 2.0f, 10.09f, 10.11f, false); BoxL(chrome, uA + 3.25f, ba1, Y2 + 2.0f, Y2 + 2.03f, 10.08f, 10.12f, false, true);
                    ColL(uA + 3.25f, ba1, Y2, Y2 + 2.0f, 10.07f, 10.13f);
                    BoxL(chrome, uA + 3.85f, uA + 4.15f, C2 - .04f, C2 - .02f, 9.0f, 9.3f, false, true);
                    BoxL(chrome, ba1 - .03f, ba1 - .012f, Y2 + 1.0f, Y2 + 1.2f, 9.2f, 9.3f, false);
                    // wanna wolnostojąca (woda), bateria stojąca
                    var tc = Pw(uA + 2.45f, Y2, 13.4f);
                    Cylinder(porcelain, tc, .78f, .58f, 40, false, false);
                    Cylinder(porcelain, tc + Vector3.up * .08f, .71f, .5f, 40, false, false, true);
                    Disc(porcelain, tc + Vector3.up * .08f, .71f, 40);
                    Disc(Water, tc + Vector3.up * .46f, .71f, 40);
                    Ring(porcelain, tc + Vector3.up * .58f, .71f, .78f, 40);
                    Collider(tc + Vector3.up * .3f, new Vector3(1.5f, .6f, 1.5f));
                    var tap = Pw(uA + 3.42f, Y2, 13.4f);
                    Cylinder(chrome, tap, .025f, 1.0f, 10, true, false);
                    Tube(chrome, tap + Vector3.up * .98f, Pw(uA + 3.12f, Y2 + .94f, 13.4f), .018f, 8);
                    // podwójna umywalka (szafka podwieszana, blat marmurowy, misy nablatowe), lustro
                    BoxL(WoodDark, ml1, lI + .55f, Y2 + .42f, Y2 + .82f, 9.6f, 11.3f, true, true);
                    BoxL(marbleTop, ml1, lI + .58f, Y2 + .82f, Y2 + .86f, 9.58f, 11.32f, false, true);
                    foreach (float vw in new[] { 10.03f, 10.87f })
                    {
                        var vb = Pw(lI + .32f, Y2 + .86f, vw);
                        Cylinder(porcelain, vb, .19f, .13f, 24, false, false); Cylinder(porcelain, vb + Vector3.up * .02f, .17f, .11f, 24, false, false, true);
                        Disc(porcelain, vb + Vector3.up * .02f, .17f, 24); Ring(porcelain, vb + Vector3.up * .13f, .17f, .19f, 24);
                        Tube(chrome, Pw(ml1, Y2 + 1.12f, vw), Pw(lI + .22f, Y2 + 1.12f, vw), .012f, 8);
                    }
                    BoxL(mirrorM, ml1, ml1 + .015f, Y2 + 1.05f, Y2 + 1.95f, 9.65f, 11.25f, false);
                    // WC podwieszane przy ścianie, przycisk spłuczki
                    var wc = Pw(lI + .38f, Y2 + .22f, 8.95f);
                    BoxL(porcelain, ml1, lI + .25f, Y2 + .22f, Y2 + .55f, 8.77f, 9.13f, false, true);
                    Cylinder(porcelain, wc, .19f, .18f, 24, true, false);
                    BoxL(chrome, ml1, ml1 + .012f, Y2 + 1.0f, Y2 + 1.16f, 8.87f, 9.03f, false);
                    Collider(wc + Vector3.up * .1f, new Vector3(.45f, .4f, .45f));
                    // grzejnik-drabinka z ręcznikiem na ścianie od schodów
                    foreach (float tw in new[] { 11.2f, 11.8f }) BoxL(chrome, ba1 - .06f, ba1 - .04f, Y2 + .2f, Y2 + 1.6f, tw - .012f, tw + .012f, false);
                    for (float ty2 = Y2 + .35f; ty2 < Y2 + 1.6f; ty2 += .25f) BoxL(chrome, ba1 - .06f, ba1 - .04f, ty2 - .01f, ty2 + .01f, 11.2f, 11.8f, false, true);
                    BoxL(new[] { Plain("ręcznik szary", new Color(.55f, .56f, .58f), .05f), Plain("ręcznik biały", new Color(.93f, .93f, .9f), .05f), Plain("ręcznik szałwia", new Color(.48f, .58f, .5f), .05f) }[sn % 3],
                         ba1 - .1f, ba1 - .065f, Y2 + .7f, Y2 + 1.36f, 11.25f, 11.75f, false, true);
                    if (plantPot) BigPlant(Pw(ba1 - .4f, Y2, bI - .45f), 15f);
                }
                // hol: roślina przy oknie bocznym; lampy sufitowe (hol, siłownia, sypialnia, łazienka)
                if (plantPot) BigPlant(Pw(uA + .75f, Y2, fI + .5f), 200f);
                if (offLamp) foreach (var (pa, pw) in new[] { (uA + 2.4f, (fI + sw0) * .5f), (uA + 8.4f, 8.9f), (uA + 8.0f, 12.3f), (uA + 2.6f, 11.2f) })
                    {
                        var o = Put(offLamp, Pw(pa, 0, pw), 0f, .02f, false);
                        if (o) { var lb = RB(o); o.transform.position += Vector3.up * (C2 - lb.max.y); }
                    }
                // TARAS NA DACHU (wyjście z siłowni): 2 leżaki z materacami i stolik przy prawej krawędzi, donice
                {
                    var whiteT = Plain("biały lakier leżaków", new Color(.92f, .92f, .90f), .5f);
                    var cushT = Plain("materac tarasu len", new Color(.84f, .80f, .72f), .1f);
                    foreach (float lw2 in new[] { 10.9f, 12.6f })
                    {
                        BoxL(whiteT, h1 - 2.55f, h1 - .65f, Y2, Y2 + .3f, lw2 - .34f, lw2 + .34f, true);
                        BoxL(cushT, h1 - 2.5f, h1 - .7f, Y2 + .3f, Y2 + .38f, lw2 - .31f, lw2 + .31f, false);
                        BoxL(cushT, h1 - 1.0f, h1 - .7f, Y2 + .38f, Y2 + .62f, lw2 - .31f, lw2 + .31f, false);
                    }
                    Cylinder(whiteT, Pw(h1 - .95f, Y2, 11.75f), .22f, .45f, 20, true, true);
                    if (planterBox) foreach (float pw in new[] { f0 + 1.0f, f1 - 1.1f }) Put(planterBox, Pw(h1 - .9f, Y2, pw), 0f, .03f, true);
                    // krawędzie dachu: murek attyki + szklana balustrada (Rail) nie miały kolizji — teraz do 1.3 m nad deską
                    ColL(uB, h1, Y2, Y2 + 1.3f, f0, f0 + .28f); ColL(h1 - .28f, h1, Y2, Y2 + 1.3f, f0, f1);
                    ColL(h0, h1, Y2, Y2 + 1.3f, f1 - .28f, f1); ColL(h0, h0 + .28f, Y2, Y2 + 1.3f, e1, f1);
                }
            }
            // WIEŻA GRAJĄCA w salonie (właściciel 18:11: „sprzęt grający, dobry, że podejdę i sam odpalę muzykę”):
            // 2 kolumny podłogowe po bokach telewizora (fortepianowa czerń / orzech), na szafce RTV wzmacniacz z VU-metrami
            // i gramofon z płytą; przy wzmacniaczu przycisk akcji / E / przycisk na ekranie włącza i wyłącza muzykę
            {
                var spk = new[] { Plain("lakier fortepianowy", new Color(.02f, .02f, .025f), .92f), WoodDark, Plain("lakier fortepianowy", new Color(.02f, .02f, .025f), .92f) }[sn % 3];
                var alu = Plain("aluminium szczotkowane", new Color(.72f, .72f, .74f), .55f, 1f);
                foreach (float sd in new[] { -1.85f, 1.85f })
                {
                    float sw = lw + sd, sa2 = h1 - wt - .11f;
                    BoxL(Frame, sa2 - .4f, sa2, FY, FY + .03f, sw - .2f, sw + .2f, false);
                    BoxL(spk, sa2 - .38f, sa2 - .02f, FY + .03f, FY + 1.17f, sw - .16f, sw + .16f, true);
                    Sign("glosnik", Pw(sa2 - .38f, FY + .6f, sw), .26f, 1.04f, -Aax, false);
                }
                float cabTop = tvCab ? SurfaceTop(tvCab, Pw(h1 - wt - .3f, 0, lw + .62f), RB(tvCab).max.y) : FY + .5f;
                float am2 = h1 - wt - .3f, aw = lw + .62f, tw2 = lw - .62f;
                BoxL(alu, am2 - .18f, am2 + .18f, cabTop, cabTop + .13f, aw - .22f, aw + .22f, false);
                Sign("wzmacniacz", Pw(am2 - .18f, cabTop + .065f, aw), .44f, .13f, -Aax, false);
                BoxL(WoodDark, am2 - .18f, am2 + .18f, cabTop, cabTop + .1f, tw2 - .22f, tw2 + .22f, false);
                var pc2 = Pw(am2, cabTop + .1f, tw2 + .03f);
                Cylinder(alu, pc2, .15f, .02f, 32, true, false);
                Cylinder(Plain("płyta winylowa", new Color(.015f, .015f, .02f), .7f), pc2 + Vector3.up * .02f, .148f, .004f, 32, true, false);
                Cylinder(Plain("naklejka płyty", new Color(.62f, .1f, .12f), .3f), pc2 + Vector3.up * .024f, .05f, .002f, 20, true, false);
                Tube(alu, Pw(am2 + .12f, cabTop + .16f, tw2 - .17f), Pw(am2 - .02f, cabTop + .135f, tw2 + .08f), .006f, 6);
                var hifi = new GameObject("wieża grająca (muzyka)"); hifi.transform.SetParent(Group(group), false);
                hifi.transform.position = Pw(am2 - .6f, cabTop, aw);
                var gr = hifi.AddComponent<BraxClaudeGrajek>();
                gr.utwor = AudioAsset("willa_muzyka.ogg"); if (!gr.utwor) gr.utwor = AudioAsset("industrial_pulse_petla.ogg");
                Vector3 da2 = Pw(h0, 0, e0), db2 = Pw(h1, 6.7f, f1 + 4.2f);
                gr.dom = MinMax(Mathf.Min(da2.x, db2.x), Mathf.Max(da2.x, db2.x), 0f, 6.7f, Mathf.Min(da2.z, db2.z), Mathf.Max(da2.z, db2.z));
                rep.notes.Add("wieża w willi " + (sn + 1) + ": " + (gr.utwor ? gr.utwor.name : "BRAK muzyki"));
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
        var gU = forSale ? GlassClear : null;                                                                 // willa na sprzedaż: piętro do wejścia
        Pane(uA + .8f, uB - .8f, 4.1f, 6.15f, e0, -1, gU);
        int mull = Mathf.Max(1, Mathf.RoundToInt((uw - 1.6f) / 1.7f));
        for (int k = 1; k < mull; k++) { float a = uA + .8f + k * (uw - 1.6f) / mull; BoxL(Frame, a - .035f, a + .035f, 4.1f, 6.15f, e0 - .05f, e0 + .01f, false); }
        Pane(uA + .9f, uA + uw * .45f, 4.1f, 6.15f, e1, 1, gU); Pane(uA + uw * .55f, uB - .9f, 4.1f, 6.15f, e1, 1, gU);
        PaneSide(e0 + 1.6f, e0 + 2.6f, 4.3f, 6.0f, uA, -1, gU); PaneSide(e1 - 3.2f, e1 - 1.4f, 4.3f, 6.0f, uA, -1, gU);
        if (!forSale) PaneSide(e0 + 2.0f, e0 + 4.4f, 3.85f, 6.0f, uB, 1);                                      // drzwi na taras
        else { PaneSide(e0 + 2.0f, e0 + 3.2f, Y2 + .02f, 6.0f, uB, 1, GlassClear); PaneSide(e0 + 2.05f, e0 + 3.25f, Y2 + .02f, 6.0f, uB - .07f, 1, GlassClear); }   // przesuwne: skrzydło za stałym, przejście 1.2 m
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
            bool best = forSale && saleNo == 0;   // willa 1: basen 12 × 4.6 m (pozostałe 9.6 × 3.8 m)
            float ph = best ? 6f : 4.8f, p0 = h0 + Wm * .5f - ph, p1 = h0 + Wm * .5f + ph, q0 = f1 + 5.2f, q1 = f1 + (best ? 9.8f : 9.0f);
            BoxL(Slabs, p0 - 1f, p1 + 1f, 0, .2f, q0 - 1f, q0, false); BoxL(Slabs, p0 - 1f, p1 + 1f, 0, .2f, q1, q1 + 1f, false);
            BoxL(Slabs, p0 - 1f, p0, 0, .2f, q0, q1, false); BoxL(Slabs, p1, p1 + 1f, 0, .2f, q0, q1, false);
            BoxL(PoolTiles, p0, p1, -.02f, .04f, q0, q1, false);
            var wm = Water; Vector3 pp = Pw(p0, 0, q0), qq = Pw(p1, 0, q1);
            float wa = Mathf.Min(pp.x, qq.x), wb = Mathf.Max(pp.x, qq.x), za = Mathf.Min(pp.z, qq.z), zb = Mathf.Max(pp.z, qq.z);
            Top(wm, wa, wb, za, zb, .12f);
            Collider(new Vector3((wa + wb) * .5f, .4f, (za + zb) * .5f), new Vector3(wb - wa + 2f, .8f, zb - za + 2f));
            rep.pools++;
            if (forSale)
            {
                // jacuzzi: okrągła wanna z drewnianą obudową (r 1.25, 0.8 m) na podeście 3.4 × 3.4 m obok basenu, woda 0.7 m,
                // kamienny rant, schodek; za basenem pas płyt z 4 leżakami (materace, poduszki), parasol, donice na tarasie
                float ja = p1 + 2.9f, jw = (q0 + q1) * .5f; var jc = Pw(ja, 0, jw);
                BoxL(Deck, ja - 1.7f, ja + 1.7f, 0, .15f, jw - 1.7f, jw + 1.7f, false);
                Cylinder(Deck, jc + Vector3.up * .15f, 1.25f, .65f, 32, false, false);
                Cylinder(PoolTiles, jc + Vector3.up * .2f, 1.1f, .6f, 32, false, false, true);
                Disc(PoolTiles, jc + Vector3.up * .2f, 1.1f, 32);
                Disc(Water, jc + Vector3.up * .7f, 1.1f, 32);
                Ring(Stone, jc + Vector3.up * .8f, 1.08f, 1.32f, 32);
                BoxL(Deck, ja - .45f, ja + .45f, .15f, .4f, jw - 1.7f, jw - 1.25f, false);
                Collider(jc + Vector3.up * .45f, new Vector3(2.6f, .9f, 2.6f));
                var white = Plain("biały lakier leżaków", new Color(.92f, .92f, .90f), .5f);
                var cushion = new[] { Plain("materac leżaka granat", new Color(.12f, .18f, .36f), .1f), Plain("materac leżaka szmaragd", new Color(.07f, .33f, .25f), .1f), Plain("materac leżaka bordo", new Color(.42f, .08f, .12f), .1f) }[(rep.forSale + 2) % 3];   // wnętrze już podbiło licznik (= sn)
                BoxL(Slabs, p0 - 1f, p1 + 1f, 0, .2f, q1 + 1f, q1 + 3.4f, false);
                for (int li = 0; li < 4; li++)
                {
                    float la2 = p0 + 1.2f + li * 2.4f;
                    BoxL(white, la2 - .36f, la2 + .36f, .2f, .42f, q1 + 1.2f, q1 + 3.15f);
                    BoxL(cushion, la2 - .33f, la2 + .33f, .42f, .5f, q1 + 1.25f, q1 + 3.1f, false);
                    BoxL(white, la2 - .33f, la2 + .33f, .5f, .62f, q1 + 2.75f, q1 + 3.1f, false);
                }
                var pc = Pw((p0 + p1) * .5f, 0, q1 + 3.0f);
                Cylinder(white, pc + Vector3.up * .2f, .03f, 2.2f, 8, false, false);
                BoxL(Plain("płótno parasola", new Color(.93f, .90f, .82f), .1f), (p0 + p1) * .5f - 1.4f, (p0 + p1) * .5f + 1.4f, 2.35f, 2.42f, q1 + 1.6f, q1 + 4.4f, false, true);   // kwadratowy parasol 2.8 m, widoczny też od spodu
                if (planterBox) foreach (float pa in new[] { h0 + .7f, h1 - .7f }) Put(planterBox, Pw(pa, .18f, f1 + 3.7f), 0f, .03f, true);
                // kamienne palenisko (bez ognia — nic nie świeci) na kwadracie z płyt obok pasa leżaków
                BoxL(Slabs, p0 - 4.4f, p0 - 1.2f, 0, .2f, q1 + 1.2f, q1 + 4.4f, false);
                if (firePit) Put(firePit, Pw(p0 - 2.8f, .2f, q1 + 2.8f), 0f, .03f, true);
                if (best)
                {
                    // boisko do siatkówki plażowej (pół boiska: 6 × 11.6 m) za garażem: piasek, linie 5 cm, słupki, siatka 1 m (góra 2.43 m)
                    const float va0 = 27.4f, va1 = 33.4f, vw0 = 22.4f, vw1 = 34.0f, vwm = (vw0 + vw1) * .5f;
                    var sandV = Plain("piasek boiska", new Color(.86f, .76f, .56f), .05f); var net = Plain("siatka boiska", new Color(.06f, .06f, .07f), .3f);
                    BoxL(sandV, va0, va1, 0, .06f, vw0, vw1, false);
                    foreach (var (la0, la1, lw0, lw1) in new[] { (va0, va1, vw0, vw0 + .05f), (va0, va1, vw1 - .05f, vw1), (va0, va0 + .05f, vw0, vw1), (va1 - .05f, va1, vw0, vw1), (va0, va1, vwm - .025f, vwm + .025f) })
                        BoxL(Paint, la0, la1, .06f, .066f, lw0, lw1, false);
                    foreach (float pa in new[] { va0 - .5f, va1 + .5f }) { var post = Pw(pa, 0, vwm); Cylinder(white, post, .05f, 2.55f, 12, true, false); Collider(post + Vector3.up * 1.27f, new Vector3(.12f, 2.55f, .12f)); }   // nie „pp” (zajęte w bloku basenu)
                    for (float pa = va0 - .45f; pa <= va1 + .451f; pa += .2f) BoxL(net, pa - .006f, pa + .006f, 1.43f, 2.38f, vwm - .006f, vwm + .006f, false);
                    for (float y = 1.43f; y < 2.38f; y += .19f) BoxL(net, va0 - .45f, va1 + .45f, y - .006f, y + .006f, vwm - .006f, vwm + .006f, false, true);
                    BoxL(white, va0 - .45f, va1 + .45f, 2.38f, 2.43f, vwm - .03f, vwm + .03f, false, true);
                }
            }
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
            // boczne płoty: lewa granica każdej działki (prawa zamyka sąsiad / ostatnia działka). Willa na sprzedaż: płot premium
            // (kamienny murek, słupki, stalowe lamele) 1.8 m z obu stron i z tyłu; sąsiad z prawej nie stawia wtedy swojego lewego
            if (forSale && !swap) { FenceZ(Z(.6f), Z(depth - .3f), u0 + .25f, 1.8f); FenceZ(Z(.6f), Z(depth - .3f), u1 - .25f, 1.8f); }
            else if (leftFence) { if (!swap) WoodFenceZ(Z(.6f), Z(depth - .3f), u0 + .1f); else WoodFenceX(Z(.6f), Z(depth - .3f), u0 + .1f); }
            // tylny płot (od parku lub od granicy osiedla)
            if (!swap) FenceX(u0, u1, Z(depth - .3f), forSale ? 1.8f : 1.55f); else FenceZ(u0, u1, Z(depth - .3f), 1.55f);
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
            var p = Pw(forSale ? 1.5f : .95f, 0, w);   // willa na sprzedaż: płot premium 1.8 m na a 0.25 (murek do ~0.4) — krzewy dalej
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
    // Muzyka (Assets/Claude/Audio): klip do komponentów sceny (referencja trafia do buildu). Brak pliku = null.
    static AudioClip AudioAsset(string file)
    {
        var path = "Assets/Claude/Audio/" + file;
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    // Szyld: płyta grafitowa + front z napisem (UV 0..1 na całą tablicę), normalna nx/nz (kierunek do oglądającego).
    static void Sign(string id, Vector3 c, float w, float h, Vector3 n, bool plate = true)
    {
        bool alongX = Mathf.Abs(n.z) > .5f;
        if (plate) Box(Frame, c - n * .06f, alongX ? new Vector3(w + .1f, h + .1f, .1f) : new Vector3(.1f, h + .1f, w + .1f), false);
        var m = SignMat(id); var b = B(m); int k = b.v.Count;
        // prawo oglądającego, który patrzy w stronę −n: cross(góra, −n). Dla normalnych ±z jak dotąd (−z → +x, +z → −x);
        // dla ±x poprawione (wcześniej odbite lustrzanie — dotąd żaden szyld nie patrzył wzdłuż x)
        Vector3 right = Vector3.Cross(Vector3.up, -n).normalized;
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
        // build 5 (galeria: „kolorowe klocki w markecie”): towar z nadrukiem (etykiety, napisy, kod kreskowy) w kolorze towaru
        string key = "towar " + names[i]; if (mats.TryGetValue(key, out var have)) return have;
        const string path = "Assets/Source/Claude/Osiedle/signs/opakowania.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) return Plain(key, cols[i], .35f);
        imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.maxTextureSize = 512; imp.SaveAndReimport();
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, color = cols[i], mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path), enableInstancing = true };
        m.SetFloat("_Glossiness", .4f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = .32f; return m;
    }
    // Butik zamknięty (szyby bielone, tabliczka WKRÓTCE OTWARCIE) do czasu prawdziwych modeli ubrań — zlecenie 31.10:
    // „ubrania = kolorowe klocki (zakaz właściciela) — wieszaki z realnymi modelami albo lokal zamknięty do czasu modeli”.
    static readonly bool ButikZModelami = false;
    static void ShopInterior(int k, string kind, float a, float b, float zf, float zb, float H0, float FY)
    {
        var rnd = new System.Random(900 + k);
        var wood = Pbr("parkiet dębowy", "wood_floor", 1.8f, new Color(.92f, .88f, .84f), .7f);
        var ceil = Plain("sufit biały", new Color(.93f, .93f, .92f), .1f);
        Top(kind == "market" || kind == "nutrition" ? PbrFloor : kind == "shake" ? Concrete : wood, a + .1f, b - .1f, zf, zb, FY);
        BoxMinMax(ceil, a + .1f, b - .1f, H0 - .35f, H0 - .3f, zf, zb, false, true);
        if (kind != "nutrition")   // Nutrition ma własne lampy liniowe z zestawu
            for (float x = a + 2f; x < b - 1.5f; x += 3f) for (float z = zf + 2f; z < zb - 1f; z += 3f) BoxMinMax(Paint, x - .3f, x + .3f, H0 - .37f, H0 - .35f, z - .3f, z + .3f, false, true);   // ze spodem (build 3: widać było tylko krawędzie)
        float cx = (a + b) * .5f;
        if (kind == "nutrition") NutritionInterior(a, b, zf, zb, H0, FY);
        else if (kind == "butik" && ButikZModelami)   // wyposażenie butiku — wyłączone, póki ubrania byłyby kolorowymi klockami (zakaz właściciela)
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
            BoxMinMax(Clinker, a + .6f, b - .6f, 3.5f, H0, zf, zf + wt, false, true);   // nadproże witryny: spód widać z wnętrza i przez szybę
            BoxMinMax(Clinker, a + .6f, d0, 0, .35f, zf, zf + wt, false); BoxMinMax(Clinker, d0 + 1.8f, b - .6f, 0, .35f, zf, zf + wt, false);
            // butik: lokal zamknięty do czasu prawdziwych modeli ubrań (zlecenie 31.10: kolorowe klocki zamiast ubrań = zakaz właściciela) —
            // szyby matowe (bielone), drzwi zamknięte z kolizją, na witrynie tabliczka WKRÓTCE OTWARCIE
            bool closed = shops[k] == "butik";
            var win = closed ? Plain("szkło bielone (lokal w przygotowaniu)", new Color(.80f, .82f, .83f), .55f) : GlassClear;
            BoxMinMax(win, a + .6f, d0, .35f, 3.5f, zf - .03f, zf + .02f, false); BoxMinMax(win, d0 + 1.8f, b - .6f, .35f, 3.5f, zf - .03f, zf + .02f, false);
            BoxMinMax(win, d0, d0 + 1.8f, 2.65f, 3.5f, zf - .03f, zf + .02f, false); BoxMinMax(Frame, d0, d0 + 1.8f, 2.6f, 2.65f, zf - .06f, zf + .01f, false);
            if (closed)
            {
                BoxMinMax(win, d0, d0 + 1.8f, .02f, 2.6f, zf - .03f, zf + .02f, true);
                Sign("wkrotce", new Vector3((a + .6f + d0) * .5f, 1.9f, zf - .1f), 3.2f, .8f, Vector3.back);
            }
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
    static void TennisClub(float x0, float x1, float front, float dir, bool leftFence = true)
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
        tennisFence = Rect.MinMaxRect(fx0, fz0, fx1, fz1); tennisLotX = new Vector2(x0, x1);
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
        if (leftFence) WoodFenceZ(front + dir * .6f, dir > 0 ? ParkZ0 - .3f : SouthZ + .3f, x0 + .1f);
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

    // Szyldy budynków (właściciel 31.10, 18:50: „policja z odznaką, szpital ze znakiem szpitala, szyldy muszą być zajebiste”).
    // Stara płyta z napisem (three.js civic.js: tablica 13 m na y 4.05, z = lico + 0.4) usunięta z paczki po prostopadłościanie,
    // w jej miejscu tablica 12 × 2.25 m z grafiką (fetch_osiedle.py → tekstury_b5.py) w ramie 3D 12 cm głębokości.
    static void CivicSigns(Transform estate)
    {
        group = "Osiedle A / szyldy budynków";
        var silver = Plain("rama szyldu srebrna", new Color(.78f, .80f, .84f), .8f, .9f);
        var red = Plain("rama szyldu czerwona", new Color(.62f, .06f, .08f), .6f, .3f);
        int gone = 0;
        foreach (var (id, cx, cz, frame) in new[] { ("policja_odznaka", -86f, -42f, silver), ("szpital_h", -86f, 14f, silver), ("hala_walk", -86f, 64f, red) })
        {
            float front = cz + 15.2f, zf = front + .62f;   // przed pilastrami (lico + 0.56)
            foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true).ToList())
            {
                if (!mr) continue; var b = mr.bounds;
                if (b.size.x > 6f && b.size.x < 14.5f && b.size.z < .8f && b.min.y > 2.6f && b.max.y < 5.8f && Mathf.Abs(b.center.x - cx) < 1.5f && b.center.z > front - .2f && b.center.z < front + 1f)
                { UnityEngine.Object.DestroyImmediate(mr.gameObject); gone++; }
            }
            const float w = 12f, h = 2.25f, y = 4.05f;
            Sign(id, new Vector3(cx, y, zf), w, h, Vector3.forward);
            BoxMinMax(frame, cx - w * .5f - .14f, cx + w * .5f + .14f, y + h * .5f, y + h * .5f + .14f, zf - .1f, zf + .06f, false, true);
            BoxMinMax(frame, cx - w * .5f - .14f, cx + w * .5f + .14f, y - h * .5f - .14f, y - h * .5f, zf - .1f, zf + .06f, false, true);
            BoxMinMax(frame, cx - w * .5f - .14f, cx - w * .5f, y - h * .5f, y + h * .5f, zf - .1f, zf + .06f, false);
            BoxMinMax(frame, cx + w * .5f, cx + w * .5f + .14f, y - h * .5f, y + h * .5f, zf - .1f, zf + .06f, false);
            foreach (float x in new[] { cx - w * .3f, cx + w * .3f }) BoxMinMax(Steel, x - .05f, x + .05f, y - h * .5f - .9f, y - h * .5f - .14f, zf - .25f, zf - .15f, false);   // wsporniki do ściany
        }
        rep.notes.Add("szyldy: policja (odznaka), szpital (H + krzyż medyczny), hala walk (oktagon); stare tablice usunięte: " + gone);
    }

    // Wylot tunelu z miasta na północnym końcu osiedla A (STAN R2: „wylot tunelu z miasta pod estakadą na wjeździe — brak”):
    // nasyp z kamienia 40 × 9 m na z 99.5–101, portal 9.4 × 6 m na osi ulicy (x 0), w środku ciemny tunel 14 m (z 101–115)
    // z posadzką i ścianą końcową; nad portalem nadproże i szyld MIASTO. Wejście 8 m w głąb tunelu wczytuje miasto
    // (BraxCityWorld.InEstateTunnel), z miasta przyjeżdża się tu (z 95.5, przodem do osiedla).
    static void CityTunnelPortal(Transform estate, Transform physics)
    {
        group = "Osiedle A / wylot tunelu z miasta";
        const float pz = 99.5f, depth = 14f, half = 4.7f, ht = 6f;
        var rock = Pbr("kamień portalu tunelu", "rough_block_wall", 2.4f, new Color(.78f, .76f, .72f));
        var dark = Plain("tunel mrok", new Color(.015f, .015f, .018f), .1f);
        CutVolumes(estate, physics, new List<Bounds> { MinMax(-half - .2f, half + .2f, -.5f, ht + .5f, pz - .3f, pz + depth + 2f) });   // stary płot / krzewy w przejściu
        BoxMinMax(rock, -20f, -half, 0, 9f, pz, pz + 1.5f); BoxMinMax(rock, half, 20f, 0, 9f, pz, pz + 1.5f);
        BoxMinMax(rock, -half, half, ht, 9f, pz, pz + 1.5f, true, true);
        BoxMinMax(Frame, -20.2f, 20.2f, 9f, 9.25f, pz - .2f, pz + 1.7f, false);
        foreach (float x in new[] { -half - .6f, half }) BoxMinMax(Concrete, x, x + .6f, 0, ht + .3f, pz - .25f, pz, false);   // obramienie portalu
        BoxMinMax(Concrete, -half - .6f, half + .6f, ht, ht + .35f, pz - .25f, pz, false, true);
        // tunel: ściany, strop, posadzka, ściana końcowa (ciemne — nic nie świeci)
        BoxMinMax(dark, -half - .3f, -half, 0, ht, pz + 1.5f, pz + depth); BoxMinMax(dark, half, half + .3f, 0, ht, pz + 1.5f, pz + depth);
        BoxMinMax(dark, -half, half, ht, ht + .3f, pz + 1.5f, pz + depth, false, true);
        BoxMinMax(Asphalt, -half, half, 0, .06f, pz - 4f, pz + depth, false);
        BoxMinMax(dark, -half, half, 0, ht, pz + depth, pz + depth + .3f);
        Collider(new Vector3(0, -.15f, pz + depth * .5f), new Vector3(half * 2f, .3f, depth + 8f));
        Sign("miasto", new Vector3(0, 7.5f, pz - .1f), 6f, 1.5f, Vector3.back);
        rep.notes.Add("wylot tunelu z miasta: portal x ±" + half + " m, z " + pz + "–" + (pz + depth) + " (wejście do miasta 8 m w głąb)");
    }

    // Muzyka (BraxClaudeMuzyka: gra tylko w środku, cichnie przy wyjściu): kasyno — Industrial Pulse (pętla 2:41, właściciel 18:09),
    // siłownia A — utwór z filmu właściciela (18:48, pętla 3:57).
    static void MusicZones()
    {
        group = "Osiedle A / muzyka";
        foreach (var (name, file, b, vol) in new[] { ("kasyno", "industrial_pulse_petla.ogg", MinMax(65.6f, 106.4f, 0f, 7f, -57.4f, -26.6f), .5f),
                                                     ("siłownia A", "silownia_muzyka.ogg", gymHall, .4f) })
        {
            var clip = AudioAsset(file);
            if (!clip || b.size == Vector3.zero) { rep.notes.Add("muzyka " + name + ": BRAK (" + file + ")"); continue; }
            var go = new GameObject("muzyka: " + name); go.transform.SetParent(Group(group), false); go.transform.position = b.center;
            var mz = go.AddComponent<BraxClaudeMuzyka>(); mz.utwor = clip; mz.wnetrze = b; mz.glosnosc = vol; mz.wygaszenie = 3f;
            rep.notes.Add("muzyka " + name + ": " + clip.name + " (" + clip.length.ToString("F0") + " s)");
        }
    }
    static Bounds gymHall;

    // Materiał z wygenerowanej tekstury (signs/*.png) kładziony w metrach (dywan kasyna: 2 m na powtórzenie).
    static Material TexMat(string id, float metres, float gloss = .2f)
    {
        string key = "tekstura " + id; if (mats.TryGetValue(key, out var have)) return have;
        string path = "Assets/Source/Claude/Osiedle/signs/" + id + ".png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (!imp) { rep.notes.Add("brak tekstury " + id); return Frame; }
        imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.maxTextureSize = 1024; imp.anisoLevel = 4; imp.SaveAndReimport();
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        m.SetFloat("_Glossiness", gloss); AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = metres; return m;
    }

    // Przedsionek kasyna (zdjęcie 57: szare ściany i płytki — „nie pasuje do luksusu”). Przestrzeń z three.js civic.js: x 66.2–105.8,
    // z −34.57 (ściana z drzwiami SALA GIER, x 88.0–90.8 do 3.35 m) … −27.18 (front z wejściem x 84–88), sufit 6.9 m.
    // Posadzka z marmuru, chodnik w art déco od wejścia do SALA GIER, boazeria z ciemnego drewna ze złotą listwą, nad nią
    // ściany w kolorze szampana, złoty gzyms, 3 kryształowe żyrandole (Poly Haven) z ciepłym światłem (ForceVertex — lampy
    // wnętrz wg STANDARDU §5, bez emisji), KASA · ŻETONY po lewej i ZAKŁADY z tablicą walk po prawej, słupki z liną, rośliny.
    static void CasinoFoyer()
    {
        group = "Osiedle A / kasyno — przedsionek";
        const float x0 = 66.25f, x1 = 105.75f, zb = -34.57f, zf = -27.2f, yF = .062f, yC = 6.85f, dx0 = 88.0f, dx1 = 90.8f, dh = 3.35f, dcx = 89.4f;
        var marble = Pbr("marmur przedsionka kasyna", "marble_01", 1.6f, new Color(.98f, .95f, .90f), .9f);
        var carpet = TexMat("dywan_kasyna", 2f, .05f);
        var wood = Plain("boazeria orzech lakier", new Color(.20f, .10f, .05f), .7f);
        var silk = Plain("tapeta szampan", new Color(.80f, .66f, .44f), .35f);
        var gold = Plain("złoto kasyna (mosiądz szczotkowany)", new Color(.80f, .62f, .25f), .72f, .9f);
        var cream = Plain("sufit kremowy", new Color(.93f, .88f, .78f), .2f);
        var rope = Plain("lina aksamitna czerwona", new Color(.42f, .03f, .05f), .25f);
        Top(marble, x0, x1, zb + .01f, zf - .01f, yF);
        Top(carpet, 84.8f, 87.2f, -32.6f, zf - .02f, yF + .008f); Top(carpet, 84.8f, 91.2f, zb + .03f, -32.6f, yF + .008f);
        // ściana z drzwiami (lico +z): boazeria do 1.2 m, złota listwa, tapeta do sufitu; nad drzwiami tapeta; złota rama drzwi
        foreach (var (a0, a1) in new[] { (x0, dx0), (dx1, x1) })
        {
            BoxMinMax(wood, a0, a1, yF, 1.2f, zb, zb + .04f, false); BoxMinMax(gold, a0, a1, 1.2f, 1.26f, zb, zb + .05f, false, true);
            BoxMinMax(silk, a0, a1, 1.26f, yC, zb, zb + .03f, false);
        }
        BoxMinMax(silk, dx0, dx1, dh, yC, zb, zb + .03f, false, true);
        BoxMinMax(gold, dx0 - .16f, dx0, yF, dh + .16f, zb, zb + .1f, false); BoxMinMax(gold, dx1, dx1 + .16f, yF, dh + .16f, zb, zb + .1f, false);
        BoxMinMax(gold, dx0 - .16f, dx1 + .16f, dh, dh + .16f, zb, zb + .1f, false, true);
        Sign("sala_gier", new Vector3(dcx, 4.05f, zb + .14f), 3.8f, .95f, Vector3.forward);
        // ściany boczne (lica wewnętrzne x 66.175 / 105.825) w przedsionku
        foreach (var (a0, a1) in new[] { (66.175f, 66.215f), (105.785f, 105.825f) })
        {
            BoxMinMax(wood, a0, a1, yF, 1.2f, zb, zf, false); BoxMinMax(gold, a0, a1, 1.2f, 1.26f, zb, zf, false, true);
            BoxMinMax(silk, a0, a1, 1.26f, yC, zb, zf, false);
        }
        // sufit kremowy ze złotym gzymsem
        BoxMinMax(cream, x0, x1, yC, yC + .05f, zb, zf, false, true);
        BoxMinMax(gold, x0, x1, yC - .22f, yC, zb + .03f, zb + .14f, false, true);
        foreach (float a in new[] { x0, x1 - .11f }) BoxMinMax(gold, a, a + .11f, yC - .22f, yC, zb, zf, false, true);
        // żyrandole kryształowe (średnica 1.5 m, wierzch pod sufitem) + ciepłe światło
        int lustres = 0;
        foreach (float lx in new[] { 73f, 86f, 99f })
        {
            var at = new Vector3(lx, 0, -31f);
            var o = crystalChandelier ? Put(crystalChandelier, at, 0f, .02f, false) : null;
            if (o)
            {
                var b = RB(o); float k = 1.5f / Mathf.Max(.2f, Mathf.Max(b.size.x, b.size.z)); o.transform.localScale *= k;
                b = RB(o); o.transform.position += new Vector3(at.x - b.center.x, yC - b.max.y, at.z - b.center.z); lustres++;
            }
            var lg = new GameObject("światło żyrandola (ciepłe)"); lg.transform.SetParent(Group(group), false); lg.transform.position = new Vector3(lx, yC - 1.4f, -31f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 11f; l.intensity = 1.1f; l.color = new Color(1f, .82f, .6f); l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;
        }
        // KASA · ŻETONY (lewo) i ZAKŁADY (prawo): lada z orzecha ze złotem i marmurem, szyba nad ladą, szyld; tablica walk
        var marbleTop = Plain("marmur czarny polerowany", new Color(.035f, .035f, .04f), .88f);
        foreach (var (c0, c1, id) in new[] { (68.6f, 75.4f, "kasa"), (96.6f, 103.4f, "zaklady") })
        {
            const float lz0 = -33.6f, lz1 = -32.7f;
            BoxMinMax(wood, c0, c1, yF, 1.08f, lz0, lz1); BoxMinMax(marbleTop, c0 - .05f, c1 + .05f, 1.08f, 1.13f, lz0 - .05f, lz1 + .08f, false, true);
            BoxMinMax(gold, c0, c1, .95f, 1.0f, lz1, lz1 + .02f, false); BoxMinMax(gold, c0, c1, yF, .14f, lz1, lz1 + .02f, false);
            BoxMinMax(GlassClear, c0, c1, 1.13f, 2.3f, lz1 - .05f, lz1 - .03f, false);
            for (float gx = c0; gx <= c1 + .01f; gx += (c1 - c0) / 4f) BoxMinMax(gold, gx - .03f, gx + .03f, 1.13f, 2.36f, lz1 - .07f, lz1 - .01f, false);
            BoxMinMax(gold, c0, c1, 2.3f, 2.36f, lz1 - .07f, lz1 - .01f, false, true);
            float mid = (c0 + c1) * .5f;
            if (id == "kasa")
            {
                Sign("kasa_zetony", new Vector3(mid, 3.25f, zb + .14f), 4.4f, 1.1f, Vector3.forward);
                var chipC = new[] { Plain("żeton czerwony", new Color(.62f, .05f, .07f), .5f), Plain("żeton czarny", new Color(.03f, .03f, .035f), .5f), Plain("żeton zielony", new Color(.05f, .35f, .18f), .5f) };
                for (int k = 0; k < 6; k++) Cylinder(chipC[k % 3], new Vector3(c0 + .8f + k * .9f, 1.13f, lz0 + .45f), .02f, .04f + .02f * (k % 4), 12, true, false);
            }
            else
            {
                Sign("tablica_zakladow", new Vector3(mid, 3.6f, zb + .14f), 5.2f, 3.25f, Vector3.forward);
                Sign("zaklady", new Vector3(mid, 5.75f, zb + .14f), 3.6f, .9f, Vector3.forward);
            }
            rep.notes.Add("kasyno: lada " + id + " x " + c0 + "–" + c1);
        }
        // słupki z liną wzdłuż chodnika (jak przy wejściu z ulicy) i rośliny w narożnikach
        foreach (float x in new[] { 84.45f, 87.55f })
        {
            foreach (float z in new[] { -28.2f, -29.8f, -31.4f })
            {
                Cylinder(gold, new Vector3(x, yF, z), .14f, .04f, 16, true, false); Cylinder(gold, new Vector3(x, yF + .04f, z), .03f, .86f, 10, true, false);
                Cylinder(gold, new Vector3(x, yF + .9f, z), .05f, .06f, 10, true, false);
            }
            BoxMinMax(rope, x - .025f, x + .025f, .82f, .87f, -31.4f, -28.2f, false, true);
        }
        if (plantPot)
            foreach (var pp in new[] { new Vector3(67.0f, yF, -28.0f), new Vector3(105.0f, yF, -28.0f), new Vector3(67.0f, yF, -34.0f), new Vector3(105.0f, yF, -34.0f), new Vector3(82.6f, yF, -33.8f), new Vector3(92.4f, yF, -33.8f) })
            {
                var o = Put(plantPot, pp, pp.x * 7f, .03f, false); if (!o) continue;
                o.transform.localScale *= 3.2f; var b = RB(o); o.transform.position += new Vector3(pp.x - b.center.x, pp.y - b.min.y, pp.z - b.center.z);
                b = RB(o); Collider(b.center, b.size);
            }
        rep.notes.Add("kasyno — przedsionek: marmur, dywan art déco, boazeria + złoto, " + lustres + " żyrandole, kasa, zakłady z tablicą walk");
    }

    // Kasyno „bogate” (właściciel 31.10: „kasyno to szopka … ma bogate być”): wejście jak w kasynie hotelowym —
    // markiza 12 × 3.6 m nad chodnikiem na 4 kolumnach z czarnego marmuru ze złotymi bazami i głowicami, złota listwa
    // markizy, szyld KASYNO złotymi literami na czarnym tle stojący na markizie, portal wejścia w złotej ramie z ciemnym
    // szkłem nad drzwiami, czerwony dywan od drzwi do krawędzi chodnika z barierkami (złote słupki, czerwona lina),
    // donice z kulami krzewów po bokach wejścia, złote głowice i bazy pilastrów elewacji. Stary szyld (płyta z napisem:
    // EstateMesh_136/137, x 79.5–92.5, y 2.8–5.3, z −26.6) usunięty z paczki — nowy go zastępuje.
    // Wymiary z planu: lico okładziny z −26.8, chodnik z −25.4…−22.6 (wierzch płyt y 0.095), jezdnia od z −22.5,
    // otwór wejścia x 84–88 do 3.1 m. Nic nie świeci (złoto = metal z odbiciem nieba, bez emisji).
    static void CasinoEntrance(Transform estate, Transform physics)
    {
        group = "Osiedle A / kasyno";
        const float cx = 86f, zf = -26.8f, zc = -23.4f, yw = .095f, hy0 = 4.2f, hy1 = 4.6f;
        int gone = 0;
        // audyt: stary daszek (three.js civic.js: 7 × 3.5 m, y 3.10–3.35, x 82.5–89.5, z −27.15…−23.65) i jego dwa słupki
        // (x 82.9 / 89.1, z −24.2, z kolizjami) zostawały pod nową markizą — wycięte z siatek, kolizje słupków wyłączone
        CutVolumes(estate, physics, new List<Bounds> { MinMax(82.2f, 89.8f, 2.95f, 3.5f, -27.4f, -23.4f), MinMax(82.5f, 83.3f, -.05f, 3.4f, -24.6f, -23.8f), MinMax(88.7f, 89.5f, -.05f, 3.4f, -24.6f, -23.8f) });
        int postCols = 0;
        if (physics)
            foreach (var c in physics.GetComponentsInChildren<BoxCollider>(true))
            {
                var b = c.bounds;
                if (c.enabled && b.size.x < 1f && b.size.z < 1f && b.center.z > -24.7f && b.center.z < -23.7f && ((b.center.x > 82.4f && b.center.x < 83.4f) || (b.center.x > 88.6f && b.center.x < 89.6f))) { c.enabled = false; postCols++; }
            }
        // podest z płyt pod wejściem (stary placyk y 0.05 i pas trawy y −0.01 między licem a chodnikiem) — dywan i donice na równym
        BoxMinMax(Slabs, cx - 6.5f, cx + 6.5f, 0, yw, zf, -25.4f, false);
        foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true).ToList())
        {
            if (!mr || (mr.name != "EstateMesh_136" && mr.name != "EstateMesh_137")) continue;
            var b = mr.bounds;
            if (b.min.x > 79f && b.max.x < 93f && b.min.z > -27.5f && b.max.z < -26f && b.min.y > 2.5f && b.max.y < 5.6f) { UnityEngine.Object.DestroyImmediate(mr.gameObject); gone++; }
        }
        var gold = Plain("złoto kasyna (mosiądz szczotkowany)", new Color(.80f, .62f, .25f), .72f, .9f);
        var marble = Plain("marmur czarny polerowany", new Color(.035f, .035f, .04f), .88f);
        var lacquer = Plain("lakier czarny markizy", new Color(.03f, .03f, .035f), .55f);
        var carpet = Plain("czerwony dywan", new Color(.50f, .04f, .06f), .08f);
        var smoke = Plain("szkło przyciemniane", new Color(.03f, .045f, .05f), .95f, .3f);
        var rope = Plain("lina aksamitna czerwona", new Color(.42f, .03f, .05f), .25f);
        // markiza: x 80–92, z −26.8…−23.2, 4.2–4.6 m, spód widoczny; złota listwa z przodu i z boków
        BoxMinMax(lacquer, cx - 6f, cx + 6f, hy0, hy1, zf, zc + .2f, false, true);
        BoxMinMax(gold, cx - 6.03f, cx + 6.03f, hy0 + .05f, hy1 - .05f, zc + .2f, zc + .26f, false, true);
        foreach (int sd in new[] { -1, 1 }) BoxMinMax(gold, cx + sd * 6f - .03f, cx + sd * 6f + .03f, hy0 + .05f, hy1 - .05f, zf, zc + .26f, false, true);
        // kolumny: x 80.6 / 84.2 / 87.8 / 91.4 (obok dywanu x 84.8–87.2), z −23.4 — 0.8 m od jezdni; r 0.26, baza i głowica 0.34
        foreach (float x in new[] { cx - 5.4f, cx - 1.8f, cx + 1.8f, cx + 5.4f })
        {
            Cylinder(gold, new Vector3(x, yw, zc), .34f, .22f, 24, true, false);
            Cylinder(marble, new Vector3(x, yw + .22f, zc), .26f, hy0 - yw - .44f, 24, false, false);
            Cylinder(gold, new Vector3(x, hy0 - .22f, zc), .34f, .22f, 24, true, false);
            Collider(new Vector3(x, hy0 * .5f, zc), new Vector3(.6f, hy0, .6f));
        }
        // szyld KASYNO na markizie (6 × 1.5 m, 4.65–6.25 m z ramą), przodem do ulicy
        Sign("kasyno", new Vector3(cx, hy1 + .05f + .75f, zc + .23f), 6f, 1.5f, Vector3.forward);
        // portal wejścia: złota rama wokół otworu x 84–88 do 3.1 m, ciemne szkło nad drzwiami do markizy
        BoxMinMax(gold, cx - 2.2f, cx - 2f, 0, 3.3f, zf, zf + .15f, false); BoxMinMax(gold, cx + 2f, cx + 2.2f, 0, 3.3f, zf, zf + .15f, false);
        BoxMinMax(gold, cx - 2.2f, cx + 2.2f, 3.1f, 3.3f, zf, zf + .15f, false, true);
        BoxMinMax(smoke, cx - 2f, cx + 2f, 3.3f, hy0, zf + .02f, zf + .04f, false);
        // czerwony dywan od drzwi do krawędzi chodnika, barierki: złote słupki z kulą, czerwona lina na 0.85 m
        Top(carpet, cx - 1.2f, cx + 1.2f, zf, -22.7f, yw + .012f);
        foreach (float x in new[] { cx - 1.45f, cx + 1.45f })
        {
            foreach (float z in new[] { zf + .7f, zf + 1.8f })
            {
                Cylinder(gold, new Vector3(x, yw, z), .14f, .04f, 16, true, false);
                Cylinder(gold, new Vector3(x, yw + .04f, z), .03f, .86f, 10, true, false);
                Cylinder(gold, new Vector3(x, yw + .9f, z), .05f, .06f, 10, true, false);
            }
            BoxMinMax(rope, x - .025f, x + .025f, .82f, .87f, zf + .7f, zf + 1.8f, false, true);
        }
        // donice z kulami krzewów po obu stronach wejścia (między kolumnami, przy elewacji)
        foreach (float x in new[] { cx - 3.6f, cx + 3.6f })
        {
            Planter(new Vector3(x, yw, zf + .55f), 1.6f, .7f, 731 + (int)x, 2, 0);
            Bush(new Vector3(x, yw + .56f, zf + .55f), 1.0f, x * 7f);
        }
        // złote bazy i głowice pilastrów elewacji (pilastry: x = cx ± 5k, z −26.95…−26.44, wys. 7 m)
        int pil = 0;
        for (float a = -20f; a <= 20.01f; a += 5f)
        {
            if (Mathf.Abs(a) <= 2f) continue;
            BoxMinMax(gold, cx + a - .23f, cx + a + .23f, 6.25f, 6.5f, -26.97f, -26.40f, false, true);
            BoxMinMax(gold, cx + a - .23f, cx + a + .23f, 0f, .3f, -26.97f, -26.40f, false);
            pil++;
        }
        // PIĘTRO I WIEŻA (właściciel 14:40: „budynek kasyna jak szopa z zewnątrz — do przebudowy w górę”). Parter: lico z −26.8,
        // gzyms (czarny, cały dach) y 6.98–7.34, rzut x 65.46–106.54, z −57.54…−26.46. Na nim kondygnacja 5.06 m (7.34–12.4),
        // cofnięta 1.4 m od lica (front z −28.2) — przed nią taras ze szklaną balustradą (0.95 m, złota poręcz) nad gzymsem;
        // boki i tył z tego samego kamienia co parter, front cały ze szkła przyciemnianego w złotych słupkach co 2.5 m
        // (rytm pilastrów parteru 5 m) i złotych ryglach; attyka czarna 0.6 m ze złotą listwą. Nad wejściem wieża
        // x 79–93 do 16.2 m, wysunięta 0.6 m przed piętro, w złotej ramie, z dużym szyldem KASYNO 10 × 2.5 m (13.05–15.55 m) —
        // widać go z ulicy z daleka (z bliska kamera gracza patrzy min. 7° w dół i łapie markizę z szyldem na 4.65–6.25 m).
        const float y0 = 7.34f, y1 = 12.4f, ux0 = cx - 19f, ux1 = cx + 19f, ubz = -55.5f, ufz = zf - 1.4f, ty1 = 16.2f, tz0 = ufz - 1.6f, tz1 = ufz + .6f;
        BoxMinMax(Stone, ux0, ux1, y0, y1, ubz, ufz, false);
        BoxMinMax(smoke, ux0 + .3f, ux1 - .3f, y0 + .45f, y1 - .5f, ufz, ufz + .03f, false);
        for (float x = cx - 17.5f; x <= cx + 17.51f; x += 2.5f) BoxMinMax(gold, x - .06f, x + .06f, y0 + .45f, y1 - .5f, ufz, ufz + .12f, false);   // rytm pilastrów parteru (oś 86, co 2.5 m)
        foreach (float x in new[] { ux0 + .3f, ux1 - .3f }) BoxMinMax(gold, x - .06f, x + .06f, y0 + .45f, y1 - .5f, ufz, ufz + .12f, false);
        foreach (float y in new[] { y0 + .45f, (y0 + y1) * .5f, y1 - .5f }) BoxMinMax(gold, ux0 + .24f, ux1 - .24f, y - .05f, y + .05f, ufz, ufz + .12f, false, true);
        // build 5 — boki i tył piętra (właściciel: „luksusowe… boki nie jak pudło”): to samo szkło przyciemniane w złotych słupkach
        // co 2.5 m i trzech złotych ryglach co z frontu (piętro to szklana bryła w złotej siatce z trzech stron + tył)
        foreach (float sx in new[] { ux0, ux1 })
        {
            float f0 = sx < cx ? sx - .03f : sx, f1 = f0 + .03f, m0 = sx < cx ? sx - .12f : sx, m1 = m0 + .12f;
            BoxMinMax(smoke, f0, f1, y0 + .45f, y1 - .5f, ubz + .3f, ufz - .3f, false);
            for (float z = ufz - .3f; z >= ubz + .29f; z -= 2.5f) BoxMinMax(gold, m0, m1, y0 + .45f, y1 - .5f, z - .06f, z + .06f, false);
            foreach (float y in new[] { y0 + .45f, (y0 + y1) * .5f, y1 - .5f }) BoxMinMax(gold, m0, m1, y - .05f, y + .05f, ubz + .24f, ufz - .24f, false, true);
        }
        BoxMinMax(smoke, ux0 + .3f, ux1 - .3f, y0 + .45f, y1 - .5f, ubz - .03f, ubz, false);
        for (float x = cx - 17.5f; x <= cx + 17.51f; x += 2.5f) BoxMinMax(gold, x - .06f, x + .06f, y0 + .45f, y1 - .5f, ubz - .12f, ubz, false);
        foreach (float y in new[] { y0 + .45f, (y0 + y1) * .5f, y1 - .5f }) BoxMinMax(gold, ux0 + .24f, ux1 - .24f, y - .05f, y + .05f, ubz - .12f, ubz, false, true);
        BoxMinMax(lacquer, ux0 - .3f, ux1 + .3f, y1, y1 + .6f, ubz - .3f, ufz + .3f, false, true);
        BoxMinMax(gold, ux0 - .3f, ux1 + .3f, y1 + .6f, y1 + .68f, ufz + .22f, ufz + .3f, false);
        BoxMinMax(GlassClear, cx - 20f, cx + 20f, y0, y0 + .95f, zf + .08f, zf + .1f, false);                 // balustrada tarasu
        BoxMinMax(gold, cx - 20f, cx + 20f, y0 + .95f, y0 + 1.0f, zf + .06f, zf + .12f, false, true);
        foreach (float x in new[] { cx - 20f, cx + 20f }) BoxMinMax(GlassClear, x - .01f, x + .01f, y0, y0 + .95f, ufz, zf + .1f, false);
        BoxMinMax(Stone, cx - 7f, cx + 7f, y1, ty1, tz0, tz1, false);
        foreach (float x in new[] { cx - 7f, cx + 6.85f }) BoxMinMax(gold, x, x + .15f, y1, ty1, tz1, tz1 + .1f, false, true);
        BoxMinMax(lacquer, cx - 7.3f, cx + 7.3f, ty1, ty1 + .4f, tz0 - .3f, tz1 + .3f, false, true);
        BoxMinMax(gold, cx - 7.3f, cx + 7.3f, ty1 + .4f, ty1 + .48f, tz1 + .22f, tz1 + .3f, false);
        Sign("kasyno", new Vector3(cx, 14.3f, tz1 + .12f), 10f, 2.5f, Vector3.forward);
        // szyldy „ekstra” (właściciel 16:43: „szyld ekstra, złoty, nie prosty”): tekstura art déco (fetch_osiedle.py: złote litery
        // szeryfowe w gradiencie, podwójna rama z rombami, promienie, ♠ ♦, gwiazdki) + gruba złota rama 3D z „żarówkami”
        // jak w kasynie (kremowe, błyszczące kostki co 0.5 m — nie świecą). Wieża: szyld 10 × 2.5 m, markiza: 6 × 1.5 m.
        var bulb = Plain("żarówki szyldu (nie świecą)", new Color(.98f, .92f, .74f), .9f);
        void Marquee(float mx, float my, float w, float h, float zFace)
        {
            float mx0 = mx - w * .5f - .15f, mx1 = mx + w * .5f + .15f, my0 = my - h * .5f - .15f, my1 = my + h * .5f + .15f, mz0 = zFace - .1f, mz1 = zFace + .1f;
            BoxMinMax(gold, mx0, mx1, my1 - .15f, my1, mz0, mz1, false, true); BoxMinMax(gold, mx0, mx1, my0, my0 + .15f, mz0, mz1, false, true);
            BoxMinMax(gold, mx0, mx0 + .15f, my0 + .15f, my1 - .15f, mz0, mz1, false); BoxMinMax(gold, mx1 - .15f, mx1, my0 + .15f, my1 - .15f, mz0, mz1, false);
            for (float x = mx0 + .3f; x < mx1 - .2f; x += .5f) foreach (float y in new[] { my0 + .075f, my1 - .075f }) BoxMinMax(bulb, x - .05f, x + .05f, y - .05f, y + .05f, mz1, mz1 + .06f, false);
            for (float y = my0 + .55f; y < my1 - .4f; y += .5f) foreach (float x in new[] { mx0 + .075f, mx1 - .075f }) BoxMinMax(bulb, x - .05f, x + .05f, y - .05f, y + .05f, mz1, mz1 + .06f, false);
        }
        Marquee(cx, 14.3f, 10f, 2.5f, tz1 + .125f);
        Marquee(cx, hy1 + .05f + .75f, 6f, 1.5f, zc + .235f);
        rep.notes.Add("kasyno: markiza 12 × 3.6 m na 4 kolumnach (marmur + złoto), szyld na markizie, portal w złotej ramie, czerwony dywan z barierkami, 2 donice, "
                      + pil + " pilastrów ze złotymi głowicami; piętro 7.34–12.4 m (szkło w złotych słupkach, taras z balustradą), wieża do "
                      + (ty1 + .48f).ToString("0.0") + " m z szyldem 10 × 2.5 m; stary szyld usunięty: " + gone + " siatek, stare słupki daszku: " + postCols + " kolizji wyłączonych");
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
        // wyższa trawa u stóp drzew (przy alejach murawa równa, z ostrą krawędzią — zlecenie trawy)
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
    static int cutAssets;
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
                AssetDatabase.CreateAsset(copy, Out + "/cut_" + Safe(mf.name) + "_" + (cutAssets++) + ".asset");   // build 5: numer — kolejne cięcia tej samej siatki nie nadpisują pliku
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
        // własny shader nieba: to samo mapowanie co Skybox/Panoramic + nasycenie błękitu 2.1 (pomiar: błękit w grze
        // sRGB 109/126/152, nasycenie 0.28 → cel 0.50, sRGB 87/127/176; chmury zostają białe)
        var shader = Shader.Find("Braxhorn/OsiedleNiebo") ?? Shader.Find("Skybox/Panoramic"); if (!shader || !tex) { rep.notes.Add("brak shadera nieba"); return; }
        var m = new Material(shader) { name = "Osiedle: niebo z chmurami" };
        m.SetTexture("_MainTex", tex); m.SetFloat("_Mapping", 1); m.SetFloat("_ImageType", 0); m.SetFloat("_Exposure", 1.05f); m.SetFloat("_Rotation", 40f);
        if (m.HasProperty("_Saturation")) m.SetFloat("_Saturation", 2.1f);
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
        rep.notes.Add("niebo: " + shader.name + " 2K, nasycenie " + (m.HasProperty("_Saturation") ? m.GetFloat("_Saturation").ToString("0.0") : "1.0"));
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
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0042f; RenderSettings.fogColor = new Color(.592f, .655f, .792f);   // = średni horyzont nieba (0–10°) po nasyceniu 2.1, sRGB 151/167/202
        QualitySettings.shadowDistance = 60f; QualitySettings.shadowResolution = ShadowResolution.High; QualitySettings.shadowCascades = 2;
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 1f);
        // migotanie w oddali: cienie dopasowane stabilnie (nie drgają przy ruchu kamery) i 2.5× dokładniejsza głębia
        // (bliska płaszczyzna 0.25 m zamiast 0.1 m: przy 24-bitowej głębi błąd = z² / (near · 2^24) — pasy na asfalcie
        // 1.6 cm nad jezdnią przestają migotać nie od 164 m, tylko od 259 m, gdzie mgła zakrywa już ~70%).
        // Przy ścianie (wnętrza, wąskie przejścia) BraxClaudeKamera wraca do 0.1 m, żeby róg bliskiej płaszczyzny nie wszedł w ścianę.
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        int kam = 0;
        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam.targetTexture) continue;                                   // tylko kamery na ekran (nie cubemapa odbić)
            cam.nearClipPlane = .25f;
            if (!cam.GetComponent<BraxClaudeKamera>()) cam.gameObject.AddComponent<BraxClaudeKamera>();
            // kolor kadru wg zasady kolorów: nasycenie +15 %, kontrast +6 % (jedno przejście, bez bloomu)
            var kol = cam.GetComponent<BraxClaudeKolor>(); if (!kol) kol = cam.gameObject.AddComponent<BraxClaudeKolor>();   // bez ?? (w edytorze GetComponent zwraca „fałszywy null”)
            kol.shader = Shader.Find("Braxhorn/OsiedleKolor");
            if (!kol.shader) rep.notes.Add("kolor kadru: brak shadera Braxhorn/OsiedleKolor");
            kam++;
        }
        rep.notes.Add("kamera: bliska płaszczyzna 0.25 m (0.1 m przy ścianie), cienie StableFit, kolor kadru (nasycenie 1.15, kontrast 1.06) — kamer " + kam);
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
        var tempAll = new List<MeshCollider>();
        try   // audyt: kolizje tymczasowe usuwane także po wyjątku (nie mogą trafić do sceny gry)
        {
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
            // murawa (pole kęp instancjami): siatka 0,3 m z przesunięciem (~11 / m²), tylko wolny trawnik działki
            for (float x = Mathf.Min(fxu, back) + .3f; x < Mathf.Max(fxu, back) - .3f; x += .3f)
                for (float z = h.z - h.hd + .3f; z < h.z + h.hd - .3f; z += .3f)
                {
                    float px = x + H(0, .3f), pz = z + H(0, .3f);
                    if (Free(px, pz, .08f)) kepyKand.Add(new Vector2(px, pz));
                }
            for (int t = 0; t < 2; t++) { float px = H(Mathf.Min(fxu, back) + 3f, Mathf.Max(fxu, back) - 3f), pz = H(h.z - h.hd + 2f, h.z + h.hd - 2f); if (Free(px, pz, 1.2f)) FlowerPatch(new Vector3(px, 0, pz), .8f, 6); }
        }
        // szpaler drzew wzdłuż ulicy głównej osiedla A (pas trawy między chodnikiem a płotami działek)
        for (float z = -8f; z < 95f; z += 11f)
            foreach (float x in new[] { -11.5f, 11.5f })
                if (Free(x, z, 2.2f) && !NearTree(x, z, 6f) && Tree(new Vector3(x, 0, z), H(7.5f, 9f), rnd.Next(360))) rep.plotTrees++;
        // otwarte trawniki osiedla A poza działkami (właściciel 31.10: „trawnik popraw, gdzie nie weszło”): murawa jak w B —
        // siatka 0.3 m z przesunięciem; wolne miejsce = nie utwardzone (IsHard 6 cm) i promień w dół nie trafia niczego wyżej
        // niż 1.5 cm nad zerem (trawnik A leży na −0.01; stare chodniki, podjazdy, place mają ≥ 2 cm i dostały kolizje wyżej)
        // na czas sprawdzenia kolizje siatki dla WSZYSTKICH starych siatek A (dachy, posadzki budynków, place): promień z góry
        // trafia dach → odrzucone (bez trawy we wnętrzach kasyna, szpitala, hali, komisariatu)
        foreach (var r in estate.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r || !r.enabled || r.GetComponent<Collider>()) continue;
            if (r.sharedMaterial && r.sharedMaterial.name.StartsWith("estate_lit_194")) continue;   // korony starych drzew: bez kolizji (audyt — inaczej koła bez trawy pod koronami)
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
            try { var mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; tempAll.Add(mc); } catch { }
        }
        Physics.SyncTransforms();
        var notLawn = new HashSet<MeshCollider>(tempAll.Where(c => c && c.name != "EstateMesh_273"));
        var plots = Houses.Select(h => { float fx = -(h.x < 0 ? h.x + 11.5f : h.x - 11.5f), bk = fx + Mathf.Sign(-h.x - fx) * 23f; return new Rect(Mathf.Min(fx, bk), h.z - h.hd, 23f, 2f * h.hd); }).ToList();
        var hits = new RaycastHit[16]; int open = 0;
        for (float z = FenceA + .4f; z < 98.6f - .3f; z += .3f)
            for (float x = X0 + .4f; x < X1 - .7f; x += .3f)
            {
                float px = x + H(0, .3f), pz = z + H(0, .3f);
                bool inPlot = false; foreach (var pr in plots) if (pr.Contains(new Vector2(px, pz))) { inPlot = true; break; }
                if (inPlot || IsHard(px, pz, .06f)) continue;
                int n = Physics.RaycastNonAlloc(new Vector3(px, 12f, pz), Vector3.down, hits, 14f, ~0, QueryTriggerInteraction.Ignore);
                bool ok = true;
                // build 4 (zdjęcie 38): kępy wyrastały przez płyty leżące równo z trawnikiem — odrzucone też trafienie w każdą starą
                // siatkę A, która nie jest trawnikiem (EstateMesh_273), nawet na wysokości trawnika
                for (int q = 0; q < n && ok; q++)
                    if (hits[q].point.y > .015f || (hits[q].collider is MeshCollider hm && notLawn.Contains(hm))) ok = false;
                if (ok) { kepyKand.Add(new Vector2(px, pz)); open++; }
            }
        rep.notes.Add("osiedle A: " + (rep.plotTrees - trees0) + " nowych drzew na działkach i przy ulicy, krzewy i rabaty; murawa na otwartych trawnikach: " + open + " kęp (kandydaci)");
        }
        finally
        {
            foreach (var mc in tempAll) if (mc) UnityEngine.Object.DestroyImmediate(mc);
            foreach (var mc in temp) if (mc) UnityEngine.Object.DestroyImmediate(mc);
        }
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
            var m = LeafMat("drzewo A " + r.name, r.sharedMaterial, mf.sharedMesh.bounds, .1182f, .0158f, .85f, new Color(.310f, .522f, .147f), new Color(.408f, .553f, .164f));
            if (m) { r.sharedMaterial = m; n++; }
        }
        rep.notes.Add("stare korony Verdant w A: " + n + " na nowym shaderze liści");
    }

    // Drzewa nie stoją na chodnikach (właściciel 31.10, galeria: drzewa na rogu chodnika przy kasynie, na placach przed
    // komisariatem / szpitalem / halą): pień na nawierzchni (z zapasem 0.5 m) → przesunięcie na najbliższy trawnik do 3.5 m
    // (pień ≥ 0.9 m od krawędzi nawierzchni, nie w budynku, płocie ani w innym drzewie); bez miejsca — drzewo usuwane.
    // Drzewa po „kulkach” Codexa nie mają własnej kolizji pnia (została oryginalna) — przy przesunięciu stara kolizja
    // wyłączana, nowa (kapsuła) stawiana pod pniem.
    static void TreesOffPaving(Transform physics)
    {
        foreach (var tr in trees.ToList())
        {
            if (tr == null || !tr.go || !OnPaving(tr.at.x, tr.at.z, .5f)) continue;
            var old = tr.at; Vector3 best = old; bool found = false;
            for (float r = .5f; r <= 3.5f && !found; r += .25f)
                for (int a = 0; a < 24 && !found; a++)
                {
                    var p = old + Quaternion.Euler(0, a * 15f, 0) * Vector3.forward * r;
                    if (OnPaving(p.x, p.z, .9f) || IsHard(p.x, p.z, .6f)) continue;
                    bool crowded = false;
                    foreach (var q in treeSpots)
                        if ((Mathf.Abs(q.x - old.x) > .01f || Mathf.Abs(q.z - old.z) > .01f) && (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z) < 9f) { crowded = true; break; }
                    if (crowded) continue;
                    best = p; found = true;
                }
            treeSpots.RemoveAll(q => Mathf.Abs(q.x - old.x) < .01f && Mathf.Abs(q.z - old.z) < .01f);
            if (physics && !tr.trunk)
                foreach (var c in physics.GetComponentsInChildren<Collider>(true))
                {
                    var b = c.bounds;
                    if (c.enabled && b.size.x < .9f && b.size.z < .9f && b.size.y < 4f && Mathf.Abs(b.center.x - old.x) < .45f && Mathf.Abs(b.center.z - old.z) < .45f) c.enabled = false;
                }
            if (found)
            {
                var d = best - old; d.y = 0;
                tr.go.transform.position += d;
                if (tr.trunk) tr.trunk.transform.position += d;
                else
                {
                    tr.trunk = new GameObject("pień kolizja"); tr.trunk.transform.SetParent(colliders, false); tr.trunk.transform.position = new Vector3(best.x, 1.4f, best.z);
                    var cap = tr.trunk.AddComponent<CapsuleCollider>(); cap.radius = .3f; cap.height = 2.8f;
                }
                tr.at = new Vector3(best.x, old.y, best.z); treeSpots.Add(new Vector3(best.x, tr.height, best.z)); rep.treesMovedOffPaving++;
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(tr.go); if (tr.trunk) UnityEngine.Object.DestroyImmediate(tr.trunk);
                trees.Remove(tr); rep.treesRemovedOnPaving++;
            }
        }
        rep.notes.Add("drzewa z chodników: " + rep.treesMovedOffPaving + " przesuniętych na trawnik, " + rep.treesRemovedOnPaving + " usuniętych (bez miejsca)");
    }

    // Aleja wzdłuż ulicy głównej osiedla A (właściciel 31.10: „jedna ulica taka biedna… dwa, trzy drzewka”; galeria 12:
    // trawniki od z -10 do 20 bez drzew): po obu stronach w pasie trawy między chodnikiem (x ±7.4) a płotami działek
    // (x ±16.5), co 7.5 m, z dala od podjazdów, skrzyżowań, latarni i innych drzew.
    static void AvenueA()
    {
        group = "Osiedle A / aleja ulicy głównej";
        foreach (float x in new[] { -10.6f, 10.6f })
            for (float z = -9f; z < 97f; z += 7.5f)
                if (!OnPaving(x, z, 1.4f) && !IsHard(x, z, 1.2f) && !NearTree(x, z, 5f)
                    && Tree(new Vector3(x, 0, z), 7.4f + Mathf.Abs(z * 7.3f) % 1.4f, Mathf.Abs(z * 23f + x * 3f) % 360f)) rep.avenueTrees++;
        rep.notes.Add("aleja A: " + rep.avenueTrees + " drzew wzdłuż ulicy głównej");
    }

    // Ławki Codexa w parku osiedla A (civic.js: (12,−80), (28,−80), (42,−80), (18,−96), (36,−96)) stały tyłem do alejek
    // (oparcie od strony ścieżki z −83 / z −99) i bez utwardzenia — wycięte, w ich miejscu ławki parkowe przodem do alejki
    // na placach z kostki (BenchSpot).
    static void CodexBenches(Transform estate, Transform physics)
    {
        group = "Osiedle A / ławki przy alejkach";
        var spots = new[] { (12f, -80f), (28f, -80f), (42f, -80f), (18f, -96f), (36f, -96f) };
        CutVolumes(estate, physics, spots.Select(s => MinMax(-s.Item1 - 1.15f, -s.Item1 + 1.15f, -.02f, 1.25f, s.Item2 - .45f, s.Item2 + .35f)).ToList());
        int before = rep.benches;
        foreach (var (x, z) in spots) ParkBench(new Vector3(-x, .035f, z), 0f);
        rep.notes.Add("park A: " + (rep.benches - before) + " ławek Codexa obróconych do alejek, na placach z kostki");
    }

    // Trójkąt / czworokąt z własnymi UV (mata oktagonu, fartuch, siatka) — reszta geometrii ma UV planarne w metrach.
    static void TriUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 n)
    {
        var bt = B(m); int k = bt.v.Count;
        var tg = Vector3.Cross(Vector3.up, n); if (tg.sqrMagnitude < .01f) tg = Vector3.right; tg.Normalize();
        bt.v.Add(a); bt.v.Add(b); bt.v.Add(c);
        for (int q = 0; q < 3; q++) { bt.n.Add(n); bt.t.Add(new Vector4(tg.x, tg.y, tg.z, 1)); }
        bt.uv.Add(ua); bt.uv.Add(ub); bt.uv.Add(uc);
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0) bt.i.AddRange(new[] { k, k + 1, k + 2 }); else bt.i.AddRange(new[] { k, k + 2, k + 1 });
    }
    static void QuadUV(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 u0, Vector2 u1, Vector2 u2, Vector2 u3, Vector3 n)
    { TriUV(m, p0, p1, p2, u0, u1, u2, n); TriUV(m, p0, p2, p3, u0, u2, u3, n); }
    // Siatka oktagonu: PNG z kanałem alfa (druty), Standard Cutout, mipmapy z zachowaniem pokrycia (siatka nie znika z daleka).
    static Material CutoutMat(string id, float metres)
    {
        string key = "siatka " + id; if (mats.TryGetValue(key, out var have)) return have;
        string path = "Assets/Source/Claude/Osiedle/signs/" + id + ".png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (!imp) { rep.notes.Add("brak tekstury " + id); return null; }
        imp.sRGBTexture = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .5f; imp.maxTextureSize = 256; imp.SaveAndReimport();
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + key, mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        m.SetFloat("_Mode", 1f); m.SetFloat("_Cutoff", .5f); m.EnableKeyword("_ALPHATEST_ON"); m.SetOverrideTag("RenderType", "TransparentCutout"); m.renderQueue = 2450;
        m.SetFloat("_Glossiness", .45f); m.SetFloat("_Metallic", .6f);
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(key) + ".mat"); mats[key] = m; tile[m] = metres; return m;
    }
    static void PointLamp(Vector3 p, float range, float intensity, Color c)
    {
        var lg = new GameObject("światło wnętrza"); lg.transform.SetParent(Group(group), false); lg.transform.position = p;
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForceVertex;
    }

    // Hala walk · oktagon (właściciel 31.10: „oktagon też musi być premium… nasza wizytówka… w środku tak samo premium.
    // Siedziska dla widzów, wszystko”). Budynek Codexa: civic.js arena (86, 64) → Unity x −106…−66, z 49…79; wejście z ulicy
    // x −88…−84 w ścianie z 79, wyjście zawodników w ścianie z 49 (oba do 3.1 m), lica ścian x −105.825 / −66.175,
    // z 49.175 / 78.825, sufit 6.89 m. Stare wnętrze (oktagon z siatki, szafki, ławki, bloki, tablice) wycięte razem z kolizjami.
    // Nowe: posadzka z grafitowej żywicy, ściany: panele akustyczne do 2.4 m, czerwony pas, grafit do sufitu, czarny sufit;
    // oktagon na podeście 0.6 m (mata z logo BRAXHORN, fartuch z napisami, 8 słupów z obiciem, siatka, górna rura z obiciem),
    // schody do furtki od wejścia; trybuny z 4 stron (4–5 rzędów, czerwone krzesełka, co 9. czarne), fotele VIP przy klatce,
    // stół komentatorów i stół sędziów (z logo), czerwony chodnik od drzwi ze słupkami i liną, wejście zawodników w ramie,
    // kratownica oświetleniowa z reflektorami, telebim z logo, banery na ścianach, szyld HALA WALK · OKTAGON nad wyjściem.
    // Nic nie świeci: światła punktowe ForceVertex (lampy wnętrz wg STANDARDU §5).
    static void ArenaInterior(Transform estate, Transform physics)
    {
        group = "Osiedle A / hala walk — oktagon premium";
        const float X0 = -105.82f, X1 = -66.18f, Z0 = 49.18f, Z1 = 78.82f, yF = .055f, yC = 6.86f, cx = -86f, cz = 63.3f, dA = -88.05f, dB = -83.95f, dH = 3.1f;
        var cut = MinMax(X0 + .04f, X1 - .04f, .06f, 6.84f, Z0 + .04f, Z1 - .04f);
        CutVolumes(estate, physics, new List<Bounds> { cut });
        int offCols = 0;
        if (physics)
        {
            var e = cut; e.Expand(new Vector3(0f, .14f, 0f));
            foreach (var c in physics.GetComponentsInChildren<Collider>(true))
                if (c.enabled && e.Contains(c.bounds.min) && e.Contains(c.bounds.max)) { c.enabled = false; offCols++; }
        }
        var epoxy = Plain("hala walk posadzka żywica grafit", new Color(.12f, .12f, .13f), .6f);
        var panel = Plain("hala walk panele akustyczne", new Color(.07f, .07f, .08f), .2f);
        var wallUp = Plain("hala walk ściana grafit", new Color(.19f, .19f, .21f), .2f);
        var red = Plain("hala walk czerwień BRAXHORN", new Color(.60f, .04f, .05f), .45f);
        var ceil = Plain("hala walk sufit czarny mat", new Color(.09f, .09f, .10f), .05f);
        var black = Plain("oktagon czarny winyl", new Color(.03f, .03f, .035f), .55f);
        var steel = Plain("oktagon stal ciemna", new Color(.16f, .17f, .18f), .5f, .8f);
        var chrome = Plain("słupki chrom", new Color(.75f, .76f, .78f), .85f, .95f);
        var carpet = Plain("hala walk chodnik czerwony", new Color(.45f, .03f, .04f), .1f);
        var rope = Plain("lina aksamitna czerwona", new Color(.42f, .03f, .05f), .25f);
        var tier = Plain("trybuny beton grafit", new Color(.15f, .15f, .16f), .3f);
        var nose = Plain("trybuny listwa aluminium", new Color(.55f, .56f, .58f), .55f, .6f);
        var seatRed = Plain("krzesełko trybun czerwone", new Color(.55f, .04f, .05f), .4f);
        var seatBlk = Plain("krzesełko trybun czarne", new Color(.04f, .04f, .045f), .4f);
        var leather = Plain("fotel VIP skóra czarna", new Color(.05f, .045f, .045f), .6f);
        var deskTop = Plain("marmur czarny polerowany", new Color(.035f, .035f, .04f), .88f);
        var screen = Plain("ekran wyłączony", new Color(.02f, .02f, .025f), .92f);
        var mat = TexMat("oktagon_mata", 1f, .15f);
        var apron = TexMat("oktagon_fartuch", 1f, .35f);
        var wire = CutoutMat("siatka_oktagonu", .3f);

        // posadzka (płyty 2 m — światło punktowe liczone w wierzchołkach)
        for (float x = X0; x < X1 - .01f; x += 2f)
            for (float z = Z0; z < Z1 - .01f; z += 2f) Top(epoxy, x, Mathf.Min(x + 2f, X1), z, Mathf.Min(z + 2f, Z1), yF);
        // ściany i sufit
        void Clad(bool alongX, float f, float s, float a0, float a1)
        {
            float g0 = s > 0 ? f + .005f : f - .045f, g1 = g0 + .04f, r0 = g0 + s * .012f, r1 = g1 + s * .012f;
            if (alongX)
            {
                BoxMinMax(panel, a0, a1, yF, 2.4f, g0, g1, false); BoxMinMax(red, a0, a1, 2.4f, 2.56f, Mathf.Min(r0, r1), Mathf.Max(r0, r1), false);
                BoxMinMax(wallUp, a0, a1, 2.56f, yC, g0, g1, false);
            }
            else
            {
                BoxMinMax(panel, g0, g1, yF, 2.4f, a0, a1, false); BoxMinMax(red, Mathf.Min(r0, r1), Mathf.Max(r0, r1), 2.4f, 2.56f, a0, a1, false);
                BoxMinMax(wallUp, g0, g1, 2.56f, yC, a0, a1, false);
            }
        }
        Clad(false, X0, 1f, Z0, Z1); Clad(false, X1, -1f, Z0, Z1);
        foreach (var (f, s) in new[] { (Z0, 1f), (Z1, -1f) })
        {
            Clad(true, f, s, X0, dA); Clad(true, f, s, dB, X1);
            float g0 = s > 0 ? f + .005f : f - .045f;
            BoxMinMax(wallUp, dA, dB, dH, yC, g0, g0 + .04f, false, true);
        }
        BoxMinMax(ceil, X0, X1, yC, yC + .02f, Z0, Z1, false, true);

        // oktagon: wierzchołki co 45° (boki prostopadłe do osi i przekątnych), bok i=1 (V1–V2) patrzy na +z — furtka i schody
        const float P = .6f, Rv = 4.6f; float ap = Rv * Mathf.Cos(Mathf.PI / 8f), yT = yF + P;
        Vector3 V(int i, float y) { float a = Mathf.PI / 8f + i * Mathf.PI / 4f; return new Vector3(cx + Rv * Mathf.Cos(a), y, cz + Rv * Mathf.Sin(a)); }
        Vector2 MatUV(Vector3 p) => new Vector2((cx + Rv - p.x) / (2f * Rv), (cz + Rv - p.z) / (2f * Rv));   // logo czytelne od wejścia (+z)
        float YawOf(Vector3 d) => Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
        var ctr = new Vector3(cx, yT, cz);
        for (int i = 0; i < 8; i++)
        {
            Vector3 a = V(i, yT), b = V(i + 1, yT);
            TriUV(mat, ctr, a, b, MatUV(ctr), MatUV(a), MatUV(b), Vector3.up);
            var n = (a + b) * .5f - ctr; n.y = 0; n.Normalize();
            var right = Vector3.Cross(Vector3.up, -n);
            bool aLeft = Vector3.Dot(a - b, right) < 0; Vector3 l = aLeft ? a : b, r = aLeft ? b : a;
            QuadUV(apron, new Vector3(l.x, yF, l.z), new Vector3(r.x, yF, r.z), new Vector3(r.x, yT, r.z), new Vector3(l.x, yT, l.z),
                   new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), n);
        }
        for (int i = 0; i < 8; i++)
        {
            var p = V(i, yT);
            Cylinder(steel, p, .07f, 1.95f, 12, true, false);
            Cylinder(black, p, .17f, 1.7f, 16, true, true);                           // obicie słupa (z kolizją)
            Cylinder(red, p + Vector3.up * 1.7f, .175f, .06f, 16, true, false);
        }
        for (int i = 0; i < 8; i++)
        {
            Vector3 a = V(i, yT), b = V(i + 1, yT), d = (b - a).normalized, mid = (a + b) * .5f;
            float len = Vector3.Distance(a, b), yw = YawOf(d);
            var n = mid - ctr; n.y = 0; n.Normalize();
            OBox(black, mid + Vector3.up * 1.88f, new Vector3(len - .2f, .14f, .16f), yw);                       // górna rura z obiciem
            if (i == 1) { OBox(steel, mid + Vector3.up * .012f, new Vector3(len - .34f, .02f, .3f), yw); continue; }   // furtka otwarta
            OBox(black, mid + Vector3.up * .11f, new Vector3(len - .3f, .22f, .1f), yw);                            // dolne obicie
            if (wire)
            {
                Vector3 a1 = a + d * .17f, b1 = b - d * .17f; float L = len - .34f, h0 = .22f, h1 = 1.81f;
                foreach (var nn in new[] { n, -n })
                    QuadUV(wire, a1 + Vector3.up * h0, b1 + Vector3.up * h0, b1 + Vector3.up * h1, a1 + Vector3.up * h1,
                           new Vector2(0, h0 / .3f), new Vector2(L / .3f, h0 / .3f), new Vector2(L / .3f, h1 / .3f), new Vector2(0, h1 / .3f), nn);
            }
            var g = new GameObject("kolizja siatki oktagonu"); g.transform.SetParent(colliders, false);
            g.transform.position = mid + Vector3.up * 1.1f; g.transform.rotation = Quaternion.Euler(0, yw, 0);
            g.AddComponent<BoxCollider>().size = new Vector3(len, 2.2f, .14f);
        }
        // kolizja podestu: ośmiokątny graniastosłup (siatka zapisana jako asset)
        var pm = new Mesh { name = "oktagon podest kolizja" };
        var pv = new List<Vector3> { new Vector3(cx, yT, cz), new Vector3(cx, 0f, cz) }; var pi = new List<int>();
        for (int i = 0; i < 8; i++) { pv.Add(V(i, yT)); pv.Add(V(i, 0f)); }
        for (int i = 0; i < 8; i++)
        {
            int t0 = 2 + 2 * i, b0 = t0 + 1, t1 = 2 + 2 * ((i + 1) % 8), b1 = t1 + 1;
            pi.AddRange(new[] { 0, t1, t0, 1, b0, b1, t0, t1, b1, t0, b1, b0 });
        }
        pm.SetVertices(pv); pm.SetTriangles(pi, 0); pm.RecalculateNormals(); pm.RecalculateBounds();
        AssetDatabase.CreateAsset(pm, Out + "/oktagon_podest_kolizja.asset");
        var pg = new GameObject("kolizja podestu oktagonu"); pg.transform.SetParent(colliders, false); pg.AddComponent<MeshCollider>().sharedMesh = pm;
        // schody do furtki (3 × 0.2 m) + rampa kolizji
        float zs = cz + ap;
        for (int k = 0; k < 3; k++)
        {
            float z0 = zs + (2 - k) * .45f, top = yF + P * (k + 1) / 3f;
            BoxMinMax(black, cx - 1.1f, cx + 1.1f, 0f, top, z0, z0 + .45f, false);
            BoxMinMax(red, cx - 1.1f, cx + 1.1f, top - .03f, top + .003f, z0 + .41f, z0 + .452f, false);
        }
        {
            float run = 1.35f, ang = Mathf.Atan2(P, run) * Mathf.Rad2Deg, L = Mathf.Sqrt(run * run + P * P);
            var rg = new GameObject("rampa schodów oktagonu"); rg.transform.SetParent(colliders, false);
            rg.transform.rotation = Quaternion.Euler(ang, 0, 0);
            rg.transform.position = new Vector3(cx, (yF + yT) * .5f, zs + run * .5f) - rg.transform.up * .05f;
            rg.AddComponent<BoxCollider>().size = new Vector3(2.2f, .1f, L);
        }
        foreach (float sx in new[] { cx - 1.18f, cx + 1.18f })
        {
            Cylinder(chrome, new Vector3(sx, .2f, zs + 1.25f), .025f, .9f, 8, true, false);
            Cylinder(chrome, new Vector3(sx, yT, zs + .1f), .025f, .9f, 8, true, false);
            OBoxQ(chrome, new Vector3(sx, (.2f + yT) * .5f + .9f, zs + .675f), new Vector3(.04f, .04f, 1.3f), Quaternion.Euler(Mathf.Atan2(yT - .2f, 1.15f) * Mathf.Rad2Deg, 0, 0));
        }

        // trybuny: rząd 0.9 m głębokości, 0.4 m wyżej; krzesełko (stopka, siedzisko, oparcie) co 0.56 m
        void Seat(Material m, bool alongX, float a, float pc, float y, float s)
        {
            float p0 = pc - .2f, p1 = pc + .2f, bk0 = s > 0 ? pc - .26f : pc + .2f, bk1 = bk0 + .06f;
            if (alongX)
            {
                BoxMinMax(steel, a - .05f, a + .05f, y, y + .38f, pc - .08f, pc + .08f, false);
                BoxMinMax(m, a - .23f, a + .23f, y + .38f, y + .46f, p0, p1, false);
                BoxMinMax(m, a - .23f, a + .23f, y + .46f, y + .88f, bk0, bk1, false);
            }
            else
            {
                BoxMinMax(steel, pc - .08f, pc + .08f, y, y + .38f, a - .05f, a + .05f, false);
                BoxMinMax(m, p0, p1, y + .38f, y + .46f, a - .23f, a + .23f, false);
                BoxMinMax(m, bk0, bk1, y + .46f, y + .88f, a - .23f, a + .23f, false);
            }
            rep.arenaSeats++;
        }
        void Stand(bool alongX, float front, float s, float a0, float a1, int rows)
        {
            for (int k = 0; k < rows; k++)
            {
                float p0 = front - s * k * .9f, p1 = front - s * (k + 1) * .9f, top = yF + .4f * (k + 1);
                float lo = Mathf.Min(p0, p1), hi = Mathf.Max(p0, p1), e0 = Mathf.Min(p0, p0 - s * .05f), e1 = Mathf.Max(p0, p0 - s * .05f);
                if (alongX) { BoxMinMax(tier, a0, a1, 0f, top, lo, hi, true); BoxMinMax(nose, a0, a1, top - .03f, top + .004f, e0 - .002f, e1 + .002f, false); }
                else { BoxMinMax(tier, lo, hi, 0f, top, a0, a1, true); BoxMinMax(nose, e0 - .002f, e1 + .002f, top - .03f, top + .004f, a0, a1, false); }
                int j = 0;
                for (float a = a0 + .45f; a < a1 - .3f; a += .56f, j++) Seat((j + k) % 9 == 4 ? seatBlk : seatRed, alongX, a, p0 - s * .45f, top, s);
            }
        }
        Stand(false, -95.6f, 1f, 51.5f, 72.5f, 5); Stand(false, -76.4f, -1f, 51.5f, 72.5f, 5);
        Stand(true, 56.0f, 1f, -95.4f, -88.4f, 4); Stand(true, 56.0f, 1f, -83.6f, -76.6f, 4);
        Stand(true, 73.4f, -1f, -95.4f, -88.4f, 4); Stand(true, 73.4f, -1f, -83.6f, -76.6f, 4);
        // fotele VIP przy klatce (2 rzędy po obu stronach chodnika, przodem do oktagonu)
        foreach (float zr in new[] { 70.2f, 71.4f })
            foreach (var (a0, a1) in new[] { (-94.6f, -88.9f), (-83.1f, -77.4f) })
                for (float a = a0; a <= a1 + .01f; a += .7f) Seat(leather, true, a, zr, yF, -1f);
        // stół komentatorów (zachód, przodem do klatki) i stół sędziów (wschód)
        foreach (var (x0, x1, s) in new[] { (-94.0f, -93.3f, 1f), (-78.7f, -78.0f, -1f) })
        {
            BoxMinMax(black, x0, x1, yF, .76f, 61.0f, 65.6f, true); BoxMinMax(deskTop, x0 - .05f, x1 + .05f, .76f, .8f, 60.9f, 65.7f, false, true);
            Sign("braxhorn_arena", new Vector3(s > 0 ? x1 + .02f : x0 - .02f, .44f, cz), 2.6f, .65f, new Vector3(s, 0, 0), false);
            foreach (float z in new[] { 61.8f, 63.3f, 64.8f })
            {
                float sx = s > 0 ? x0 + .15f : x1 - .15f;
                BoxMinMax(steel, sx - .02f, sx + .02f, .8f, .9f, z - .04f, z + .04f, false);
                BoxMinMax(screen, sx - .02f, sx + .02f, .9f, 1.2f, z - .26f, z + .26f, false, true);
                Seat(leather, false, z, s > 0 ? x0 - .75f : x1 + .75f, yF, s);
            }
        }
        // czerwony chodnik od drzwi z ulicy do schodów i od wyjścia zawodników do klatki; słupki z liną
        Top(carpet, -87.3f, -84.7f, zs + 1.37f, Z1 - .05f, yF + .006f);
        Top(carpet, -87.3f, -84.7f, Z0 + .05f, cz - ap - .08f, yF + .006f);
        foreach (float x in new[] { -87.55f, -84.45f })
        {
            for (float z = 70.0f; z <= 77.5f; z += 1.5f)
            {
                Cylinder(chrome, new Vector3(x, yF, z), .14f, .04f, 16, true, false); Cylinder(chrome, new Vector3(x, yF + .04f, z), .03f, .86f, 10, true, false);
                Cylinder(chrome, new Vector3(x, yF + .9f, z), .05f, .06f, 10, true, false);
            }
            BoxMinMax(rope, x - .025f, x + .025f, .82f, .87f, 70.0f, 77.5f, false, true);
        }
        // wejście zawodników: czarna rama z czerwonym pasem
        BoxMinMax(black, dA - .35f, dA, yF, dH + .35f, Z0 + .045f, Z0 + .3f, false); BoxMinMax(black, dB, dB + .35f, yF, dH + .35f, Z0 + .045f, Z0 + .3f, false);
        BoxMinMax(black, dA - .35f, dB + .35f, dH, dH + .35f, Z0 + .045f, Z0 + .3f, false, true);
        BoxMinMax(red, dA - .02f, dB + .02f, dH - .06f, dH, Z0 + .045f, Z0 + .31f, false, true);
        // kratownica nad klatką, reflektory, telebim
        const float tr = 5.6f, ty0 = 6.05f, ty1 = 6.35f;
        BoxMinMax(steel, cx - tr, cx + tr, ty0, ty1, cz - tr, cz - tr + .3f, false, true); BoxMinMax(steel, cx - tr, cx + tr, ty0, ty1, cz + tr - .3f, cz + tr, false, true);
        BoxMinMax(steel, cx - tr, cx - tr + .3f, ty0, ty1, cz - tr, cz + tr, false, true); BoxMinMax(steel, cx + tr - .3f, cx + tr, ty0, ty1, cz - tr, cz + tr, false, true);
        foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            Cylinder(steel, new Vector3(cx + o.x * (tr - .15f), ty1, cz + o.y * (tr - .15f)), .03f, yC - ty1, 8, false, false);
        for (int k = -2; k <= 2; k++)
            foreach (var (ox, oz) in new[] { (k * 2.1f, -tr + .15f), (k * 2.1f, tr - .15f), (-tr + .15f, k * 2.1f), (tr - .15f, k * 2.1f) })
            {
                if (Mathf.Abs(k) == 2 && Mathf.Abs(ox) > tr - .3f && Mathf.Abs(oz) > tr - .3f) continue;
                Cylinder(black, new Vector3(cx + ox, ty0 - .34f, cz + oz), .13f, .34f, 10, true, false);
            }
        BoxMinMax(black, cx - 1.3f, cx + 1.3f, 5.0f, 6.0f, cz - 1.3f, cz + 1.3f, false, true);
        Cylinder(steel, new Vector3(cx, 6.0f, cz), .05f, yC - 6.0f, 8, false, false);
        foreach (var n in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            Sign("braxhorn_arena", new Vector3(cx, 5.5f, cz) + n * 1.305f, 2.4f, .6f, n, false);
        // banery na ścianach bocznych, szyld nad wyjściem zawodników, logo nad wejściem
        foreach (float z in new[] { 54.5f, 60.5f, 66.5f, 72.5f })
        {
            Sign("baner_oktagon", new Vector3(X0 + .06f, 4.75f, z), 1.4f, 2.8f, Vector3.right, false);
            Sign("baner_oktagon", new Vector3(X1 - .06f, 4.75f, z), 1.4f, 2.8f, Vector3.left, false);
        }
        Sign("hala_walk", new Vector3(cx, 4.75f, Z0 + .06f), 9.6f, 1.8f, Vector3.forward, false);
        Sign("braxhorn_arena", new Vector3(cx, 4.9f, Z1 - .06f), 6f, 1.5f, Vector3.back, false);
        // rośliny w narożnikach od wejścia
        if (plantPot)
            foreach (var pp in new[] { new Vector3(-104.6f, yF, 77.6f), new Vector3(-67.4f, yF, 77.6f) })
            {
                var o = Put(plantPot, pp, pp.x * 7f, .03f, false); if (!o) continue;
                o.transform.localScale *= 3.2f; var bb = RB(o); o.transform.position += new Vector3(pp.x - bb.center.x, pp.y - bb.min.y, pp.z - bb.center.z);
                bb = RB(o); Collider(bb.center, bb.size);
            }
        // światło: 4 białe nad klatką, 8 ciepłych nad trybunami
        foreach (var o in new[] { new Vector2(-3.4f, -3.4f), new Vector2(3.4f, -3.4f), new Vector2(-3.4f, 3.4f), new Vector2(3.4f, 3.4f) })
            PointLamp(new Vector3(cx + o.x, 5.3f, cz + o.y), 9f, 1.25f, new Color(1f, .97f, .92f));
        foreach (var p in new[] { new Vector3(-99f, 4.6f, 57f), new Vector3(-99f, 4.6f, 67f), new Vector3(-73f, 4.6f, 57f), new Vector3(-73f, 4.6f, 67f),
                                  new Vector3(-92f, 4.6f, 75.5f), new Vector3(-80f, 4.6f, 75.5f), new Vector3(-92f, 4.6f, 53.5f), new Vector3(-80f, 4.6f, 53.5f) })
            PointLamp(p, 9f, .8f, new Color(1f, .86f, .7f));
        rep.notes.Add("hala walk: oktagon premium (podest 0.6 m, mata z logo, siatka" + (wire ? "" : " — BRAK tekstury siatki") + "), " + rep.arenaSeats
                      + " miejsc (trybuny 4 strony + VIP + stoły), stare wnętrze wycięte, kolizji wyłączonych: " + offCols);
    }

    // Siłownia A (właściciel 31.10): „ściany mógłbyś przemalować oprócz muralu… podłogę, żeby nie była płaska”. Ściany
    // (Gym_Mesh_9, 17 boczne, 36 z drzwiami, 70 tylna, 74 wnęki) — kopia materiału z kolorem grafitowym (cegła zostaje widoczna,
    // wypalone światło zostaje); mural to osobny obiekt — bez zmian. Podłoga (Gym_Mesh_153) — mocniejsza faktura gumy
    // i delikatny połysk. Materiały kopiowane — siłownia w mieście bez zmian.
    static void GymRepaint(GameObject g)
    {
        var tint = new Color(.47f, .48f, .51f);
        var walls = new HashSet<string> { "Gym_Mesh_9", "Gym_Mesh_17", "Gym_Mesh_36", "Gym_Mesh_70", "Gym_Mesh_74" };
        var done = new Dictionary<Material, Material>(); int w = 0, f = 0; bool floorNormal = false;
        foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
        {
            bool wall = walls.Contains(r.name), floor = r.name == "Gym_Mesh_153";
            if (!wall && !floor) continue;
            var ms = r.sharedMaterials;
            for (int k = 0; k < ms.Length; k++)
            {
                var m = ms[k]; if (!m) continue;
                if (!done.TryGetValue(m, out var nm))
                {
                    nm = new Material(m) { name = m.name + (wall ? " (grafit A)" : " (faktura A)") };
                    if (wall) { if (nm.HasProperty("_Color")) nm.SetColor("_Color", nm.GetColor("_Color") * tint); }
                    else
                    {
                        if (nm.HasProperty("_NormalStrength")) nm.SetFloat("_NormalStrength", Mathf.Max(1.15f, nm.GetFloat("_NormalStrength") * 1.8f));
                        if (nm.HasProperty("_Specular")) nm.SetFloat("_Specular", .03f);
                        if (nm.HasProperty("_Smoothness")) nm.SetFloat("_Smoothness", .34f);
                        if (nm.HasProperty("_RoughnessInfluence")) nm.SetFloat("_RoughnessInfluence", .9f);
                        if (nm.HasProperty("_NormalMap") && nm.GetTexture("_NormalMap")) floorNormal = true;
                    }
                    AssetDatabase.CreateAsset(nm, Out + "/silowniaA_" + done.Count + "_" + Safe(m.name) + (wall ? "_grafit" : "_faktura") + ".mat");
                    done[m] = nm;
                }
                ms[k] = nm; if (wall) w++; else f++;
            }
            r.sharedMaterials = ms;
        }
        rep.notes.Add("siłownia A: ściany grafit (" + w + " materiałów), podłoga z fakturą (" + f + ", mapa normalnych: " + (floorNormal ? "tak" : "BRAK") + "), mural bez zmian");
    }

    // Recepcja siłowni A (właściciel 31.10: „recepcja chujowa… w narożniku przy wejściu taką zaokrągloną… z napisem Braxhorn…
    // z drewna”). Stara lada (model x −0.1…4.1, z 9.9…10.6 → świat x 81.9…86.1, z −68.15…−67.45) wycięta razem z kolizją.
    // Nowa: lada w kształcie „stadionu” przy ścianie z drzwiami, na zachód od drzwi (x 82.0…87.9): łuk r 1.6 m od ściany,
    // prosty front x 83.6…86.4 (z −68.75), łuk do x 87.9 z przejściem dla obsługi przy ścianie; korpus z orzecha, pionowe
    // lamele z ciemnego dębu, blat z białego kwarcu, logo BRAXHORN na froncie; za ladą ściana z lameli z logo, monitor.
    static void GymReception(Transform g)
    {
        group = "Osiedle A / siłownia BRAXHORN — recepcja";
        const float zW = -67.15f, r = 1.6f, xa = 83.6f, xb = 86.4f, H = 1.05f;
        CutVolumes(g, null, new List<Bounds> { MinMax(81.6f, 86.4f, .02f, 1.7f, -68.35f, -67.3f) });
        var walnut = Pbr("recepcja orzech", "wood_floor", 1.2f, new Color(.62f, .44f, .30f), .6f);
        var oak = Plain("recepcja lamele dąb ciemny", new Color(.24f, .15f, .09f), .5f);
        var quartz = Plain("blat kwarc biały", new Color(.93f, .92f, .90f), .8f);
        var black = Plain("recepcja cokół czarny", new Color(.03f, .03f, .035f), .4f);
        var path = new List<Vector3>();
        for (int k = 0; k <= 12; k++) { float a = Mathf.Deg2Rad * (180f + 90f * k / 12f); path.Add(new Vector3(xa + r * Mathf.Cos(a), 0, zW + r * Mathf.Sin(a))); }
        for (int k = 1; k <= 4; k++) path.Add(new Vector3(xa + (xb - xa) * k / 4f, 0, zW - r));
        for (int k = 1; k <= 9; k++) { float a = Mathf.Deg2Rad * (270f + 70f * k / 9f); path.Add(new Vector3(xb + r * Mathf.Cos(a), 0, zW + r * Mathf.Sin(a))); }
        for (int k = 0; k + 1 < path.Count; k++)
        {
            Vector3 a = path[k], b = path[k + 1], d = (b - a).normalized, mid = (a + b) * .5f, n = Vector3.Cross(Vector3.up, d);
            float len = Vector3.Distance(a, b), yw = Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg;
            OBox(walnut, mid - n * .25f + Vector3.up * (H * .5f + .04f), new Vector3(len + .02f, H - .08f, .5f), yw, true);
            OBox(black, mid - n * .27f + Vector3.up * .04f, new Vector3(len + .02f, .08f, .44f), yw);
            OBox(quartz, mid - n * .22f + Vector3.up * (H + .025f), new Vector3(len + .03f, .05f, .64f), yw);
            bool underLogo = Mathf.Abs(mid.z - (zW - r)) < .01f && mid.x > 84.0f && mid.x < 86.0f;
            if (underLogo) continue;
            for (float t = .055f; t < len; t += .11f) OBox(oak, a + d * t + n * .02f + Vector3.up * (H * .5f + .02f), new Vector3(.035f, H - .1f, .04f), yw);
        }
        Sign("recepcja_braxhorn", new Vector3((xa + xb) * .5f, .58f, zW - r - .07f), 2.2f, .55f, Vector3.back, false);
        // ściana za ladą: lamele dębowe do 3.2 m z logo
        BoxMinMax(oak, 81.4f, 89.6f, 0f, 3.2f, zW - .05f, zW - .01f, false);
        for (float x = 81.5f; x < 89.55f; x += .16f) BoxMinMax(walnut, x, x + .06f, 0f, 3.2f, zW - .08f, zW - .05f, false);
        Sign("recepcja_braxhorn", new Vector3(85.5f, 2.3f, zW - .1f), 3.6f, .9f, Vector3.back, false);
        // monitor i terminal na blacie
        BoxMinMax(black, 84.95f, 85.05f, H + .05f, H + .2f, -68.55f, -68.47f, false);
        BoxMinMax(black, 84.7f, 85.3f, H + .2f, H + .55f, -68.52f, -68.48f, false, true);
        BoxMinMax(black, 86.0f, 86.14f, H + .05f, H + .1f, -68.62f, -68.45f, false);
        rep.notes.Add("siłownia A: nowa recepcja (zaokrąglona, orzech + lamele, blat kwarc, logo BRAXHORN) przy wejściu, stara lada usunięta");
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
        gymDoorX = dX; gymDoorZ = frontFace; gymHall = MinMax(hx0, hx1, 0f, H, hzP, hzS);
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
                    if (b.minX > -.5f && b.maxX < 4.5f && b.minZ > 9.6f && b.maxZ < 10.9f && b.top < 1.6f) continue;   // stara lada recepcji (nowa: GymReception)
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
            Guard("siłownia A: mural właściciela", () => GymMural(g.transform, gx, gz));
            Guard("siłownia A: ściany grafit, podłoga z fakturą", () => GymRepaint(g));
            Guard("siłownia A: recepcja", () => GymReception(g.transform));
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
        BoxMinMax(Frame, dX - dw - .08f, dX + dw + .08f, dh - .05f, dh + .08f, fz0, fz1 + .05f, false, true);   // nadproże (ze spodem)
        foreach (float sx in new[] { dX - dw - .08f, dX + dw }) BoxMinMax(Frame, sx, sx + .08f, 0, dh, fz0, fz1 + .05f, false);   // ościeżnica
        BoxMinMax(Frame, dX - 1.6f, dX + 1.6f, 3.3f, 3.42f, fz1, fz1 + 1.4f, false, true);                    // daszek
        BoxMinMax(Plinth, hx0 - t - .04f, dX - dw - .08f, 0, .5f, fz1, fz1 + .04f, false); BoxMinMax(Plinth, dX + dw + .08f, hx1 + t + .04f, 0, .5f, fz1, fz1 + .04f, false);
        BoxMinMax(Frame, hx0 - t - .1f, hx1 + t + .1f, H, H + .25f, hzP - t - .1f, fz1 + .1f, false, true);
        BoxMinMax(Gravel, hx0, hx1, H - .2f, H - .1f, hzP, hzS, false);
        BoxMinMax(Concrete, hx0, hx1, H - .24f, H - .2f, hzP, hzS, false, true);          // strop od spodu (gdyby hala nie miała sufitu)
        // szyld nad wejściem: build 3 — szyld 12 × 3 m na 6.6 m był poza kadrem (plac do kasyna 8.5 m, kamera patrzy min. 7° w dół:
        // z kamery 8.1 m od ściany na wys. 2.24 m górna krawędź kadru = 2.24 + 8.07·tg(27.5° − 7°) = 5.26 m). Teraz 5.6 × 1.4 m
        // (proporcje tekstury 4:1) na 3.75–5.15 m: nad daszkiem (linia wzroku nad jego krawędzią: 3.49 m > 3.42 m) i w kadrze.
        const float sW = 5.6f, sH = 1.4f, sY0 = 3.75f;
        for (float x = hx0 + 3f; x < hx1 - 2f; x += 6f) if (Mathf.Abs(x - dX) > sW * .5f + .35f) BoxMinMax(Plinth, x - .25f, x + .25f, 0, H, fz1, fz1 + .2f, false);   // pilastry (nie przez szyld)
        Sign("braxhorn", new Vector3(dX, sY0 + sH * .5f, fz1 + .12f), sW, sH, Vector3.forward);
        // drugi szyld od parku (ściana południowa, zachodnia część: widać go z polany siłowni plenerowej, x 47–69 bez drzew)
        Sign("braxhorn", new Vector3(hx0 - t + 7.4f, 6.6f, hzP - t - .12f), 12f, 3f, Vector3.back);
        // plac przed wejściem: od ściany do fartucha kasyna (z -61.5) i na zachód do ścieżki osiedla A (x 55)
        Top(Slabs, 55f, hx1 + t, fz1, -61.5f, .05f);
        Planter(new Vector3(dX - 3.2f, .05f, fz1 + .8f), 2.2f, .7f, 701, 1, 0); Planter(new Vector3(dX + 3.2f, .05f, fz1 + .8f), 2.2f, .7f, 702, 1, 0);
        foreach (float bx in new[] { gx - 14f, gx - 8f }) Bench(new Vector3(bx, .05f, -62.4f), 180f);
        // od parku i z boków: pas krzewów (pas do płotu 6.9 m — korona drzewa weszłaby w ścianę, więc bez drzew)
        for (float x = hx0 + 1.5f; x < hx1 - 1f; x += 3.4f) if (!IsHard(x, hzP - t - 2.4f, .6f)) Bush(new Vector3(x, 0, hzP - t - 2.4f), 1.5f + (float)treeRnd.NextDouble() * .5f, x * 11f);
        for (float z = hzP + 1.5f; z < fz1 - 2f; z += 3.6f) if (!IsHard(hx1 + t + 2.4f, z, .6f)) Bush(new Vector3(hx1 + t + 2.4f, 0, z), 1.4f + (float)treeRnd.NextDouble() * .5f, z * 13f);
        rep.notes.Add("siłownia A: hala " + (hx1 - hx0 + 2 * t).ToString("0.0") + " × " + (fz1 - hzP + t).ToString("0.0") + " m, drzwi (" + dX.ToString("0.0") + ", " + frontFace.ToString("0.0") + ")");
    }

    // Mural właściciela „BRAXHORN STREET FIGHTER” w kopii siłowni — dokładnie jak w siłowni miasta (R1,
    // BraxGymReferenceRebuild.BuildMuralWall, gałąź claude/region-20261030; dokument PREFABY: „mural ten sam”):
    // ten sam plik (MuralPaint.jpg 1536×614, SHA-256 e7147416…), ten sam shader Braxhorn/PaintedBrickMural (farba na cegle,
    // nieregularna krawędź, cegła i fugi widoczne pod farbą), cegła brick_wall_10 (Poly Haven, jak w R1), te same wymiary
    // 19.2 × 7.675 m (proporcje obrazu 2.502:1, bez przycinania), środek w układzie hali (−3.4, 4.48, −12.986) na ścianie
    // tylnej (Gym_Mesh_70, lico z = −13.0), Brax z lewej, Nyxa z prawej patrząc od wejścia. Stara nakładka na tej ścianie
    // (Gym_Mesh_71: x ±16.82, z −13.0…−12.88) zasłaniałaby mural — usuwana z paczki (nie tylko wyłączana).
    // Teksturę muralu wczytuje się z bajtów (jak w R1: importer Unity na Linuksie/WebGL potrafił zwrócić null).
    static void GymMural(Transform gym, float gx, float gz)
    {
        const string muralPath = "Assets/Kickboxing/Textures/MuralPaint.jpg";
        const string muralSha = "e714741664dbedddc379a97a87f7d8757856c73438eeb34af10df058846628e8";
        if (!File.Exists(muralPath)) { rep.notes.Add("siłownia A: brak muralu " + muralPath); return; }
        var bytes = File.ReadAllBytes(muralPath);
        string sha; using (var digest = System.Security.Cryptography.SHA256.Create()) sha = BitConverter.ToString(digest.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        if (sha != muralSha) { rep.notes.Add("siłownia A: mural ma inne bajty niż zatwierdzony (" + sha.Substring(0, 12) + ") — nie stawiam"); return; }
        var shader = Shader.Find("Braxhorn/PaintedBrickMural");
        if (!shader) { rep.notes.Add("siłownia A: brak shadera Braxhorn/PaintedBrickMural"); return; }
        var art = new Texture2D(2, 2, TextureFormat.RGB24, true) { name = "mural BRAXHORN STREET FIGHTER (oryginalne piksele)" };
        if (!ImageConversion.LoadImage(art, bytes, false) || art.width != 1536 || art.height != 614) { rep.notes.Add("siłownia A: mural nie wczytał się 1536×614"); return; }
        art.wrapMode = TextureWrapMode.Clamp; art.filterMode = FilterMode.Trilinear; art.anisoLevel = 4; art.Apply(true, false);
        AssetDatabase.CreateAsset(art, Out + "/mural_silowni.asset");
        // stara nakładka na ścianie tylnej (sprawdzenie jak w R1: szerokość ≥ 30 m, z −13.1…−12.7 w układzie hali)
        var old = gym.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "Gym_Mesh_71"); bool removed = false;
        if (old)
        {
            var ob = old.bounds;
            if (ob.size.x >= 30f && ob.min.z >= gz - 13.1f && ob.max.z <= gz - 12.7f) { UnityEngine.Object.DestroyImmediate(old.gameObject); removed = true; }
            else { rep.notes.Add("siłownia A: Gym_Mesh_71 ma inny kształt (" + ob.min.ToString("F2") + "…" + ob.max.ToString("F2") + ") — muralu nie stawiam, żeby go nie zasłoniła"); return; }
        }
        var m = new Material(shader) { name = "Osiedle: mural siłowni (farba na cegle)" };
        m.SetTexture("_MainTex", art);
        var bd = Tex + "brick_wall_10/";
        if (File.Exists(bd + "albedo.jpg")) { m.SetTexture("_BrickTex", Map(bd + "albedo.jpg", false, true)); m.SetTexture("_BumpMap", Map(bd + "normal.jpg", true, false)); }
        else rep.notes.Add("siłownia A: brak cegły brick_wall_10 — mural bez cegły pod farbą");
        m.SetFloat("_PaintOpacity", .92f);
        AssetDatabase.CreateAsset(m, Out + "/mural_silowni.mat");
        const float w = 19.2f, h = 7.675f, cx = -3.4f, cy = 4.48f;
        var mesh = new Mesh
        {
            name = "mural siłowni (farba na ścianie tylnej)",
            vertices = new[] { new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(w / 2, h / 2, 0), new Vector3(-w / 2, h / 2, 0) },
            uv = new[] { new Vector2(1, 0), Vector2.zero, new Vector2(0, 1), Vector2.one },          // U odwrócone: Brax z lewej od wejścia
            uv2 = new[] { new Vector2((cx - w / 2) / 2.2f, (cy - h / 2) / 2.2f), new Vector2((cx + w / 2) / 2.2f, (cy - h / 2) / 2.2f),
                          new Vector2((cx + w / 2) / 2.2f, (cy + h / 2) / 2.2f), new Vector2((cx - w / 2) / 2.2f, (cy + h / 2) / 2.2f) },   // cegła 2.2 m / kafel
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        mesh.tangents = new[] { new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1), new Vector4(1, 0, 0, 1) };   // audyt: cegła (uv2) idzie w +x — bez odbicia rzeźby (jak R1 bez przeliczania)
        AssetDatabase.CreateAsset(mesh, Out + "/mural_silowni_siatka.asset");
        var go = new GameObject("Mural BRAXHORN STREET FIGHTER (farba na ścianie tylnej)"); go.transform.SetParent(gym, false);
        go.transform.localPosition = new Vector3(cx, cy, -12.986f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;   // jak reszta hali (bez cienia dachu)
        rep.notes.Add("siłownia A: mural właściciela 19.2 × 7.675 m na ścianie tylnej (x " + (gx + cx - w / 2).ToString("0.0") + "…" + (gx + cx + w / 2).ToString("0.0")
                      + ", z " + (gz - 12.986f).ToString("0.00") + "), SHA-256 zgodne, stara nakładka " + (removed ? "usunięta" : "nieobecna"));
    }

    // Róże (Poly Haven nie ma róż — kwiat zbudowany z płatków): kielich z 4 pierścieni płatków (3, 5, 6, 7 płatków,
    // promień 0.6–4 cm, górne brzegi lekko wywinięte na zewnątrz, płatki dwustronne), łodyga 45 cm, 2 liście. Kolor
    // aksamitnej czerwieni (dwa odcienie na przemian), zieleń łodyg. Jedna siatka róży (~180 trójkątów), 12 sztuk w wazonie:
    // kwiaty 22–38 cm nad szyjką, łodygi odchylone 4–18° na zewnątrz, obrót losowy. Nic nie świeci.
    static Mesh roseMesh;
    static Mesh RoseMesh()
    {
        if (roseMesh) return roseMesh;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var petals = new List<int>(); var green = new List<int>();
        void Patch(Vector3[,] g, List<int> tri)   // siatka punktów (wiersze × kolumny), dwie strony
        {
            int R = g.GetLength(0), C = g.GetLength(1);
            for (int side = 0; side < 2; side++)
            {
                int b0 = v.Count;
                for (int r = 0; r < R; r++) for (int c = 0; c < C; c++) { v.Add(g[r, c]); n.Add(Vector3.up); uv.Add(new Vector2(c / (C - 1f), r / (R - 1f))); }
                for (int r = 0; r < R - 1; r++) for (int c = 0; c < C - 1; c++)
                {
                    int a = b0 + r * C + c, b = a + 1, d = a + C, e = d + 1;
                    if (side == 0) tri.AddRange(new[] { a, d, b, b, d, e }); else tri.AddRange(new[] { a, b, d, b, e, d });
                }
            }
        }
        var rings = new[] { (3, .006f, .012f, -.010f, .016f, .002f), (5, .010f, .020f, -.012f, .014f, .004f), (6, .014f, .030f, -.016f, .010f, .007f), (7, .016f, .040f, -.020f, .004f, .010f) };
        for (int ri = 0; ri < rings.Length; ri++)
        {
            var (cnt, r0, r1, y0, y1, curl) = rings[ri];
            float wid = Mathf.PI * 2f / cnt * 1.35f, off = ri * .7f;
            for (int k = 0; k < cnt; k++)
            {
                float th = off + k * Mathf.PI * 2f / cnt; var g = new Vector3[3, 4];
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 4; c++)
                    {
                        float t = r / 2f, sx = c / 3f * 2f - 1f, a = th + sx * wid * .5f;
                        float rad = (r0 + (r1 - r0) * t + curl * t * t) * (1f - .12f * sx * sx), y = y0 + (y1 - y0) * t - curl * .6f * t * t * (ri == 3 ? 1f : 0f);
                        g[r, c] = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                    }
                Patch(g, petals);
            }
        }
        // łodyga (4 ściany, 6 mm) i dwa liście
        float sw = .003f;
        for (int k = 0; k < 4; k++)
        {
            float a0 = k * Mathf.PI / 2f, a1 = a0 + Mathf.PI / 2f; var g = new Vector3[2, 2];
            g[0, 0] = new Vector3(Mathf.Cos(a0) * sw, -.45f, Mathf.Sin(a0) * sw); g[0, 1] = new Vector3(Mathf.Cos(a1) * sw, -.45f, Mathf.Sin(a1) * sw);
            g[1, 0] = new Vector3(Mathf.Cos(a0) * sw, -.018f, Mathf.Sin(a0) * sw); g[1, 1] = new Vector3(Mathf.Cos(a1) * sw, -.018f, Mathf.Sin(a1) * sw);
            Patch(g, green);
        }
        foreach (var (ly, la) in new[] { (-.17f, .4f), (-.26f, 3.5f) })
        {
            var dir = new Vector3(Mathf.Cos(la), .35f, Mathf.Sin(la)).normalized; var side = Vector3.Cross(dir, Vector3.up).normalized * .016f;
            var bse = new Vector3(0, ly, 0); var g = new Vector3[2, 3];
            g[0, 0] = bse; g[0, 1] = bse + dir * .03f - side; g[0, 2] = bse + dir * .065f;
            g[1, 0] = bse; g[1, 1] = bse + dir * .03f + side; g[1, 2] = bse + dir * .065f;
            Patch(g, green);
        }
        var m = new Mesh { name = "róża (płatki + łodyga)" };
        m.SetVertices(v); m.SetUVs(0, uv); m.subMeshCount = 2; m.SetTriangles(petals, 0); m.SetTriangles(green, 1);
        m.RecalculateNormals(); m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, Out + "/roza.asset");
        return roseMesh = m;
    }
    static void RoseBouquet(Vector3 mouth, int count, int seed)
    {
        var rnd = new System.Random(seed); var mesh = RoseMesh();
        var red = new[] { Plain("róża płatki czerwień", new Color(.46f, .02f, .05f), .38f), Plain("róża płatki ciemna czerwień", new Color(.33f, .01f, .04f), .38f) };
        var stem = Plain("róża łodyga i liście", new Color(.10f, .26f, .07f), .25f);
        var root = new GameObject("Bukiet czerwonych róż"); root.transform.SetParent(Group(group), false);
        for (int i = 0; i < count; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = .01f + (float)rnd.NextDouble() * .045f, lift = .2f + (float)rnd.NextDouble() * .2f, tilt = 3f + (float)rnd.NextDouble() * 20f;   // build 5: 22 róże, pełniejszy bukiet
            var outDir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            var rot = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, outDir)) * Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            var go = new GameObject("róża " + (i + 1)); go.transform.SetParent(root.transform, false);
            go.transform.position = mouth + outDir * rr + rot * (Vector3.up * lift) + Vector3.up * 0f;   // kwiat nad szyjką, łodyga sięga do wnętrza wazonu
            go.transform.rotation = rot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { red[i % 2], stem }; mr.shadowCastingMode = ShadowCastingMode.Off;
        }
        var lodR = root.AddComponent<LODGroup>(); lodR.SetLODs(new[] { new LOD(.02f, root.GetComponentsInChildren<Renderer>()) }); lodR.RecalculateBounds();   // znikają z daleka
        rep.notes.Add("willa 1: bukiet " + count + " czerwonych róż na stole (" + mouth.ToString("F2") + ")");
    }
    // Karteczka „Kocham cię, Monika” (właściciel 16:44): składana karta 20 × 13 cm (namiot, ścianki 20° od pionu), przód
    // z napisem (tekstura z fetch_osiedle.py: kremowy papier, złota ramka, czerwony atrament, kursywa szeryfowa), tył kremowy.
    static void LoveCard(Vector3 basePos, Vector3 face)
    {
        const float w = .20f, h = .13f;
        face = new Vector3(face.x, 0, face.z).normalized; var right = Vector3.Cross(Vector3.up, -face).normalized;
        float lean = 20f * Mathf.Deg2Rad, dz = Mathf.Sin(lean) * h, hy = Mathf.Cos(lean) * h;
        Vector3 aL = basePos - right * w * .5f + Vector3.up * (hy + .003f), aR = basePos + right * w * .5f + Vector3.up * (hy + .003f);
        Vector3 fL = basePos - right * w * .5f + face * dz + Vector3.up * .003f, fR = basePos + right * w * .5f + face * dz + Vector3.up * .003f;
        Vector3 bL = basePos - right * w * .5f - face * dz + Vector3.up * .003f, bR = basePos + right * w * .5f - face * dz + Vector3.up * .003f;
        void Panel(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 nrm, Vector3 tan)
        {
            var b = B(m); int k = b.v.Count; var p = new[] { p0, p1, p2, p3 };
            var uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            for (int q = 0; q < 4; q++) { b.v.Add(p[q]); b.n.Add(nrm); b.t.Add(new Vector4(tan.x, tan.y, tan.z, 1)); b.uv.Add(uv[q]); }
            if (Vector3.Dot(Vector3.Cross(p[1] - p[0], p[2] - p[0]), nrm) >= 0) b.i.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            else b.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            tile[m] = 1f;
        }
        var nf = (face * Mathf.Cos(lean) + Vector3.up * Mathf.Sin(lean)).normalized; var nb = (-face * Mathf.Cos(lean) + Vector3.up * Mathf.Sin(lean)).normalized;
        var cream = Plain("karteczka papier", new Color(.96f, .93f, .86f), .2f);
        Panel(SignMat("kartka_monika"), fL, fR, aR, aL, nf, right);            // przód z napisem
        Panel(cream, bR, bL, aL, aR, nb, -right);                             // tył
        rep.notes.Add("willa 1: karteczka „Kocham cię, Monika” na stole");
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
    static GameObject diningTable, coffeeTable, tvCabinet, lounge, ottoman, plantPot, planterBox,   // wille na sprzedaż (luksus)
                      sofaLux, chandelier, console, vase, fruitBowl, photoFrame, firePit, nightstand, crystalChandelier;
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

        // posadzka: ciepły parkiet dębowy (właściciel 31.10: „podłogę zrób ładną … drewno”; ten sam materiał co w willach
        // i sklepach — jedna kopia), ściany bez zmian; w celach beton (nakładka 4 mm nad parkietem)
        var floor = Pbr("parkiet dębowy", "wood_floor", 1.8f, new Color(.92f, .88f, .84f), .7f); var wall = InteriorPaint; var wood = Plain("lada dyżurnego drewno", new Color(.45f, .33f, .22f), .35f);
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
        // cele: dwie obok siebie, każda 4.0 × 4.9 m (właściciel: „dwie cele, ładny układ”; build 3: dwie kiszki 12.8 × 4.9 m
        // z jedną ławką). Krata od korytarza (x = -79) z zamkniętymi drzwiami (rama + zamek), ściana działowa z = -51.9,
        // ściana tylna x = -75 (za nią zamknięty depozyt). W każdej: betonowa posadzka, prycza z materacem przy ścianie tylnej,
        // stalowy sedes z umywalką w drugim rogu, lampa; nad kratą tabliczka CELA 1 / CELA 2.
        const float cb = -75f;
        WallX(-79f, cb, -51.9f); WallZ(iz0, pz, cb);
        var mattress = Plain("materac celi", new Color(.20f, .27f, .38f), .2f); var bunkWood = Plain("prycza drewno", new Color(.50f, .37f, .24f), .3f);
        int cell = 0;
        foreach (var (c0, c1) in new[] { (-51.9f, pz), (iz0, -51.9f) })
        {
            float za = Mathf.Min(c0, c1), zb = Mathf.Max(c0, c1), zc = (za + zb) * .5f; cell++;
            bool lowPart = Mathf.Abs(za - (-51.9f)) < .01f;   // cela 1 (ściana działowa od dołu) / cela 2 (od góry) — układ lustrzany
            Top(Concrete, -79f + .05f, cb - .08f, za + .08f, zb - .08f, FY + .004f);
            // krata: pręty co 12 cm na całej szerokości, poprzeczki na 1.0 i 2.1 m, rama drzwi (zc ± 0.5) z zamkiem
            for (float z = za + .12f; z < zb - .06f; z += .12f) BoxMinMax(Steel, -79.02f, -78.98f, FY, CY, z - .013f, z + .013f, false);
            foreach (float y in new[] { 1.0f, 2.1f }) BoxMinMax(Steel, -79.05f, -78.95f, y, y + .06f, za, zb, false);
            foreach (float dz in new[] { -.5f, .5f }) BoxMinMax(Steel, -79.06f, -78.94f, FY, 2.25f, zc + dz - .03f, zc + dz + .03f, false);
            BoxMinMax(Steel, -79.06f, -78.94f, 2.22f, 2.28f, zc - .53f, zc + .53f, false, true);
            BoxMinMax(Plain("zamek celi", new Color(.55f, .56f, .58f), .55f, .8f), -79.12f, -78.88f, 1.0f, 1.25f, zc + .36f, zc + .46f, false, true);
            Collider(new Vector3(-79f, CY * .5f, zc), new Vector3(.12f, CY, zb - za));
            Sign("cela" + cell, new Vector3(-79.13f, 2.75f, zc), 1.2f, .3f, Vector3.left);
            // prycza przy ścianie tylnej (x cb), od ściany działowej: 2.0 × 0.8 m, wys. 0.45 m, materac 8 cm
            float bz0 = lowPart ? za + .05f : zb - 2.05f;   // prycza zawsze przy ścianie działowej
            BoxMinMax(Steel, cb - .85f, cb - .02f, FY, FY + .37f, bz0 + .05f, bz0 + 1.95f);
            BoxMinMax(bunkWood, cb - .87f, cb - .02f, FY + .37f, FY + .45f, bz0, bz0 + 2f, false);
            BoxMinMax(mattress, cb - .82f, cb - .07f, FY + .45f, FY + .53f, bz0 + .05f, bz0 + 1.95f, false);
            // sedes i umywalka (stal) w przeciwnym rogu przy ścianie tylnej
            float tz = lowPart ? zb - .55f : za + .55f;     // sedes w przeciwnym rogu
            var inox = Plain("stal nierdzewna celi", new Color(.62f, .63f, .65f), .65f, .9f);
            Cylinder(inox, new Vector3(cb - .35f, FY, tz), .2f, .42f, 18, true, true);
            BoxMinMax(inox, cb - .12f, cb - .02f, FY, FY + .75f, tz - .2f, tz + .2f);
            BoxMinMax(inox, cb - .42f, cb - .02f, .82f, .92f, tz + (lowPart ? -.75f : .35f), tz + (lowPart ? -.35f : .75f), true, true);
        }
        // lampy wiszące (bez świecenia) nad salą, holem i korytarzem
        int lamps = 0;
        if (offLamp)
            foreach (var p in new[] { new Vector2(-93f, -39.9f), new Vector2(-79f, -39.9f), new Vector2(-71f, -39.9f), new Vector2(-93f, -43.3f), new Vector2(-79f, -43.3f), new Vector2(-71f, -43.3f),
                                      new Vector2(-92f, -30.5f), new Vector2(-80f, -30.5f), new Vector2(-101.5f, -41f), new Vector2(-86f, -52f), new Vector2(-99.8f, -51.9f),
                                      new Vector2(-77f, -49.45f), new Vector2(-77f, -54.35f) })   // + po jednej w każdej celi
            {
                var o = Put(offLamp, new Vector3(p.x, 0, p.y), 0f, .02f, false);
                if (o) { var b = RB(o); o.transform.position += Vector3.up * (CY - b.max.y); lamps++; }
            }
        rep.notes.Add("komisariat: nowe wnętrze — " + desks + " biurek, " + seats + " krzeseł w poczekalni, " + lamps + " lamp, 2 cele 4.0 × 4.9 m (prycza, sedes, umywalka), parkiet");
    }
    static Material PbrFloor => Pbr("posadzka komisariatu", "large_grey_tiles", 2.4f, new Color(.95f, .95f, .95f));
    static Material InteriorPaint => Pbr("ściany wnętrz farba", "painted_plaster_wall", 2.2f, new Color(.93f, .92f, .89f));

    // ---------------- główne ----------------
    public static void Apply()
    {
        rep = new Report(); gymHall = default; mats.Clear(); tile.Clear(); batches.Clear(); groups.Clear(); group = "osiedle"; saleDoors.Clear(); treeSpots.Clear(); trees.Clear(); roofBounds.Clear(); kepyKand.Clear(); lakaKand.Clear(); tennisFence = default; tennisLotX = default; soil = null; soilTex = null; grassMeanLin = Vector3.zero; grassLin = Vector4.one;
        pavedCells.Clear(); pavingSet = null; CodexPaving();
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
        lounge = Model("mid_century_lounge_chair"); ottoman = Model("Ottoman_01"); plantPot = Model("potted_plant_04"); planterBox = Model("planter_box_01");
        sofaLux = Model("sofa_03"); chandelier = Model("lantern_chandelier_01"); console = Model("ClassicConsole_01"); vase = Model("ceramic_vase_02");
        fruitBowl = Model("wooden_bowl_02"); photoFrame = Model("standing_picture_frame_02"); firePit = Model("stone_fire_pit");
        nightstand = Model("ClassicNightstand_01"); crystalChandelier = Model("Chandelier_01");
        offLamp = Model("modern_ceiling_lamp_01"); offShelf = Model("drawer_cabinet"); offPads = Model("office_notepads"); offBoard = Model("standing_chalkboard_01");
        flowers = new[] { Model("flower_empodium"), Model("periwinkle_plant"), Model("flower_ursinia"), Model("dandelion_01"), Model("flower_gazania") }.Where(g => g).ToArray();
        flowerSpecies = flowers.Select(g => PlantNodes(g)).Where(l => l.Count > 0).ToList(); flowerNodes = flowerSpecies.SelectMany(l => l).ToList();
        rep.notes.Add("rośliny: " + flowerNodes.Count + " wariantów kwiatów (" + flowerSpecies.Count + " gatunków)");

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
        Guard("kasyno — bogate wejście", () => CasinoEntrance(estate, physics));
        Guard("szyldy budynków (policja, szpital, hala walk)", () => CivicSigns(estate));
        Guard("kasyno — przedsionek luksusowy", CasinoFoyer);
        Guard("wylot tunelu z miasta", () => CityTunnelPortal(estate, physics));
        Guard("siłownia A (za kasynem)", () => GymA(86f, -66f));
        Guard("muzyka (kasyno, siłownia A)", MusicZones);
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
            bool sale = k == 0 || k == 2 || k == 4, prevSale = k == 1 || k == 3 || k == 5;   // sąsiad z lewej = willa na sprzedaż (ma płot premium)
            if (k == 3) TennisClub(a, b, south, -1f, !prevSale); else Villa(idx++, a, b, south, -1f, forSale: sale, leftFence: !prevSale);
        }
        Guardhouse(AxisX - 7f, FenceA - 2.4f);
        group = "Osiedle B / ogrodzenie";
        WoodFenceZ(north + .6f, ParkZ0 - .3f, X1 - .7f); WoodFenceZ(south - .6f, SouthZ + .3f, X1 - .7f);
        FenceZ(north + .3f, ParkZ0 - .3f, AxisX - Half - .25f, 1.55f);

        // 6) osiedle A tak samo jak B: drzewa, krzewy i rabaty na działkach, szpaler wzdłuż ulicy głównej; zielone korony starych drzew
        Guard("ławki Codexa przodem do alejek", () => CodexBenches(estate, physics));
        Guard("aleja ulicy głównej A", AvenueA);
        Guard("hala walk — oktagon premium", () => ArenaInterior(estate, physics));
        Guard("zieleń osiedla A", () => GreenA(estate, physics));
        Guard("korony starych drzew A", () => FixOldLeaves(estate));
        Guard("okna z pokojami w starych domach A", () => FakeRoomsA(estate));
        Guard("komisariat — nowe wnętrze", () => PoliceInterior(estate, physics));
        Guard("drzewa z chodników", () => TreesOffPaving(physics));
        Guard("korony drzew z dala od budynków", PruneCrowns);
        // 7) trawa w 3D na wszystkich trawnikach parku i osiedla B (po wszystkim, co zajmuje ziemię)
        group = "Trawa 3D";
        Guard("trawa 3D (kępy instancjami, maska ziemi)", () => GrassField(X0 + .4f, X1 - .4f, SouthZ + .4f, FenceA - .4f));
        Guard("kwiaty na trawnikach", () => FlowerPatches(X0 + .4f, X1 - .4f, SouthZ + .4f, FenceA - .4f));

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

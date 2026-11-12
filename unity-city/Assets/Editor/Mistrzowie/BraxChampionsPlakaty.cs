using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Owner 12.11 02:25-02:37 (two BRAXHORN NUTRITION posters with his two friends): "billboardy albo plakaty na sklepach i w
// ogóle ... W siłowniach, w sklepach z odżywkami sportowych ... z rozrywkami". The posters (encrypted pack
// braxhorn-sklep-plakaty-01, global: plakaty_01/*.jpg; the adidas logo on the first one masked - only BRAXHORN in the game)
// hang as BACK-LIT light boxes: a black aluminium case, the print lit from inside (emission = the picture, like the arena
// murals), double-sided on a foot in the open, single-sided on a wall:
//  * G1 SPORT ARENA: two on the BRAXHORN NUTRITION wall either side of the green 3D name, two citylights in the mall at
//    the store's door;
//  * the promenade: a citylight in the palm row every ~110 m (between a lamp and a bench) and one at the outdoor gym;
//  * E1 CENTRUM ROZRYWKI: two citylights on the forecourt by the taxi bay.
// Gyms and training halls get theirs when they are built (Plakat() is public for them).
public static class BraxChampionsPlakaty
{
    const string Paczka = "plakaty_01", Out = "Assets/PlayableGenerated/Mistrzowie/Plakaty";
    public static readonly string[] Pliki = { "plakat_braxhorn_nutrition_whey", "plakat_braxhorn_nutrition_kreatyna" };
    public const float PrintW = 1.60f, PrintH = 1.75f;           // the owner's 1199 x 1312 (0.914) untouched
    static float K => BraxClaudeOsiedle.K;
    static Material frame, glow;
    static readonly List<Material> prints = new List<Material>();
    public static int Count;

    static bool Materials()
    {
        prints.Clear();
        foreach (var f in Pliki)
        {
            string p = BraxMiastoAssety.Baza + Paczka + "/" + f + ".jpg";
            if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                bool dirty = false;
                if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; dirty = true; }
                if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; dirty = true; }
                if (ti.anisoLevel != 4) { ti.anisoLevel = 4; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (!tex) continue;
            var m = Mat("print " + f, Color.white * .30f, .35f, 0f, new Color(.92f, .92f, .92f));
            m.SetTexture("_MainTex", tex); m.SetTexture("_EmissionMap", tex); m.SetFloat("_Cull", 2f);
            BraxZapisAssetu.Utworz(m, Out + "/" + f + ".mat");
            prints.Add(m);
        }
        if (prints.Count == 0) return false;
        frame = Mat("case black aluminium", new Color(.035f, .035f, .04f), .32f, .75f, Color.black);
        BraxZapisAssetu.Utworz(frame, Out + "/plakat_obudowa.mat");
        glow = Mat("case light line", new Color(.95f, .92f, .88f), .4f, 0f, new Color(.9f, .86f, .8f));
        BraxZapisAssetu.Utworz(glow, Out + "/plakat_linia.mat");
        return true;
    }

    static Material Mat(string name, Color c, float rough, float metal, Color emission)
    {
        var sh = Shader.Find(BraxCityMaterials.ShaderName);
        var m = new Material(sh) { name = "BRAXHORN plakat " + name, enableInstancing = true };
        m.SetColor("_Color", c); m.SetFloat("_Roughness", rough); m.SetFloat("_Metallic", metal); m.SetFloat("_VertexTint", 0f);
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        if (emission.maxColorComponent > 0f) m.SetColor("_EmissionColor", emission);
        return m;
    }

    /// <summary>One light box (nominal metres, world frame). centre = middle of the print; facing = from the print toward the
    /// viewer; twoSided = a citylight on a foot (the other poster on the back), else a wall box 6 cm deep.</summary>
    public static void Plakat(BraxChampionsMeshBatch m, Vector3 centre, Vector3 facing, int which, bool twoSided)
    {
        if (prints.Count == 0) return;
        facing.y = 0f; facing.Normalize();
        var up = Vector3.up;
        float d = twoSided ? .16f : .06f, bw = .085f;
        var back = twoSided ? centre : centre - facing * d * .5f;
        var rot = Quaternion.LookRotation(facing, Vector3.up);
        m.RoundedBox(frame, back, new Vector3(PrintW + 2f * bw, PrintH + 2f * bw, d), .02f, rot, 2);
        void Print(Vector3 f, int i)
        {
            var r = Vector3.Cross(Vector3.up, -f).normalized;
            var c = back + f * (d * .5f + .004f);
            Vector3 bl = c - r * PrintW * .5f - up * PrintH * .5f, br = c + r * PrintW * .5f - up * PrintH * .5f;
            Vector3 tr = br + up * PrintH, tl = bl + up * PrintH;
            var mat = prints[i % prints.Count];
            if (Vector3.Dot(Vector3.Cross(tl - bl, tr - bl), f) > 0f) m.QuadUV(mat, bl, tl, tr, br, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            else m.QuadUV(mat, bl, br, tr, tl, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
            // thin warm line under the print (the light of the box)
            m.RoundedBox(glow, c - up * (PrintH * .5f + bw * .55f), new Vector3(PrintW, .012f, .006f), .002f, Quaternion.LookRotation(f, Vector3.up), 1);
        }
        Print(facing, which);
        if (twoSided) Print(-facing, which + 1);
        Count++;
    }

    // a citylight standing on the ground at nominal (x, z): print centre 1.62 m above the ground
    static void Citylight(BraxChampionsMeshBatch m, float x, float z, Vector3 facing, int which)
    {
        var g = Grunt(x, z);
        var c = new Vector3(x, g + 1.62f, z);
        Plakat(m, c, facing, which, true);
        var rot = Quaternion.LookRotation(facing, Vector3.up); var right = Vector3.Cross(Vector3.up, -facing).normalized;
        float bottom = c.y - PrintH * .5f - .085f;
        foreach (float s in new[] { -.55f, .55f })
            m.RoundedBox(frame, new Vector3(x, (g + bottom) * .5f, z) + right * s, new Vector3(.09f, bottom - g, .09f), .01f, rot, 1);
        m.RoundedBox(frame, new Vector3(x, g + .04f, z), new Vector3(1.45f, .08f, .32f), .02f, rot, 2);
    }

    static float Grunt(float x, float z)
    {
        var p = new Vector3(x, 0f, z) * K;
        if (Physics.Raycast(p + Vector3.up * 30f * K, Vector3.down, out var hit, 40f * K, ~0, QueryTriggerInteraction.Ignore)) return hit.point.y / K;
        return .16f;
    }

    public static int Generate()
    {
        Directory.CreateDirectory(Out); Count = 0;
        if (!Materials()) { BraxBuildQuality.Warn("MISTRZOWIE_PLAKATY brak paczki " + Paczka + " (braxhorn-sklep-plakaty-01)"); return 0; }
        Physics.SyncTransforms();
        var root = new GameObject("MIASTO MISTRZÓW | plakaty BRAXHORN NUTRITION (citylighty, ściany)").transform;
        var m = new BraxChampionsMeshBatch();
        // G1 SPORT ARENA (G1 frame + 940/175): the NUTRITION wall (south, face z -54.62, first floor 6.32), the green name at
        // x 61..71 / y 9.42..9.74 - posters either side, over the 2.2 m shelving
        float gx = BraxChampionsD2.G1CentreX, gz = BraxChampionsD2.G1CentreZ, f1 = BraxChampionsD2.G1F1;
        float wallZ = -BraxChampionsD2.G1HD + .3f + .08f;
        Plakat(m, new Vector3(gx + 55.5f, f1 + 3.25f, gz + wallZ + .03f), Vector3.forward, 0, false);
        Plakat(m, new Vector3(gx + 76.5f, f1 + 3.25f, gz + wallZ + .03f), Vector3.forward, 1, false);
        // the store's door from the north mall (x 47, z 28..40): two citylights in the mall 3 m in front of it
        foreach (var (z, i) in new[] { (29.4f, 0), (38.6f, 1) })
        {
            var c = new Vector3(gx + 44.0f, f1 + 1.62f, gz + z);
            Plakat(m, c, Vector3.left, i, true);
            var rot = Quaternion.LookRotation(Vector3.left, Vector3.up);
            float bottom = c.y - PrintH * .5f - .085f;
            foreach (float s in new[] { -.55f, .55f })
                m.RoundedBox(frame, new Vector3(c.x, (f1 + bottom) * .5f, c.z + s), new Vector3(.09f, bottom - f1, .09f), .01f, rot, 1);
            m.RoundedBox(frame, new Vector3(c.x, f1 + .04f, c.z), new Vector3(1.45f, .08f, .32f), .02f, rot, 2);
        }
        // the promenade palm row (z -143): palms every 18 m from x 62, lamps 3.7 m either side, benches at +9; a citylight
        // between a lamp and a bench (+6.35), facing along the walkway, every 6th palm
        int k = 0;
        foreach (int pi in new[] { 4, 10, 16, 22, 28, 34, 40, 45 })
            Citylight(m, 62f + 18f * pi + 6.35f, -143f, (k++ % 2 == 0) ? Vector3.right : Vector3.left, pi);
        // the outdoor gym (zone 430 / -101, 22 x 14): one at its west edge, facing the zone path
        Citylight(m, 417.6f, -95.5f, Vector3.right, 0);
        // E1 forecourt (facade z -38, taxi bay x 517..553 at the road): either side, facing the street
        Citylight(m, 514.5f, -45f, Vector3.back, 0);
        Citylight(m, 556.5f, -45f, Vector3.back, 1);
        m.Build(root, "plakaty BRAXHORN NUTRITION", Out, false);
        Debug.Log("MISTRZOWIE_PLAKATY skrzynki=" + Count + " wydruki=" + prints.Count + " (G1 SPORT ARENA 4, promenada 9, E1 2)");
        return Count;
    }
}

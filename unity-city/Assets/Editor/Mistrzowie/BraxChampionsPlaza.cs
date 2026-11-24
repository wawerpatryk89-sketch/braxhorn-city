using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Owner 23.11 19:12 (the GTA beach pictures: "chcę taki piasek i fale na morzu i woda jak prawdziwa"), 19:27 ("fale niech
// się wylewają na plażę jak normalnie", "te ścieżki usuń z plaży całkowicie"): the region's beach and the sea at it.
//   - the sand (ChampionsPiasek): one material for the dry and the wet beach (no seam between their meshes): Poly Haven
//     Aerial Sand (trampled, footprints) without its tyre tracks (morze_plaza.py, 19:36 "ten piasek tu lepiej wyglądał ...
//     taki nierówny"), darker and glossy where the waves reach, with Damp Beach Sand's fine texture;
//   - the sea (ChampionsMorze): its shore field (BraxChampionsMorzeBrzeg, an RGHalf texture), waves to the shore, surf,
//     run-up with a foam lip, clear turquoise shallows over the sand, blue farther out;
//   - the shallows over the wet sand (płycizna): the sea's meshes stop at the sand's outer edge, so the run-up had no water
//     to climb with - a copy of each wet-sand mesh at the sea level, with the sea's material, is the water it climbs with;
//   - the old static foam strip goes (the sea draws its own foam, moving with the waves).
public static class BraxChampionsPlaza
{
    const string Output = "Assets/PlayableGenerated/Mistrzowie";
    public const string SzumPath = "Assets/Source/Mistrzowie/Textures/morze_szum.png";
    public const string PiasekDiff = "Assets/Source/Mistrzowie/Textures/plaza_piasek_diff_4k.jpg", PiasekNor = "Assets/Source/Mistrzowie/Textures/plaza_piasek_nor_4k.jpg";
    const string PiasekJson = "Assets/Source/Mistrzowie/plaza-piasek.json";
    // the linear means (the track-free sand as measured by morze_plaza.py on 23.11, Damp Beach Sand on its 2k map; the sand's
    // own is read from plaza-piasek.json when the build made it) and the colours they are brought to (sRGB)
    public static readonly Vector3 MeanPiasek = new Vector3(.36795f, .23936f, .22694f), MeanDampBeachSand = new Vector3(.1526f, .1192f, .0749f);
    public static readonly Vector3 Suchy = new Vector3(216, 205, 186), Mokry = new Vector3(180, 168, 149);
    // the waves: length and period (game units, s), the run-up's rise over the sea (game units) and how far out it starts
    public const float Dlugosc = 20f, Okres = 7f, Wbieganie = .27f, ZasiegWbiegania = 22f;
    public const int PoleW = 1024, PoleH = 512;
    static float K => BraxClaudeOsiedle.K;
    public static int Plycizny;

    static Vector4 Tint(Vector3 srgb, Vector3 mean) => new Vector4(
        Mathf.GammaToLinearSpace(srgb.x / 255f) / mean.x, Mathf.GammaToLinearSpace(srgb.y / 255f) / mean.y, Mathf.GammaToLinearSpace(srgb.z / 255f) / mean.z, 1f);

    [Serializable] sealed class Srednia { public float[] linear; }
    static Vector3 PiasekSrednia()
    {
        try
        {
            if (File.Exists(PiasekJson))
            {
                var s = JsonUtility.FromJson<Srednia>(File.ReadAllText(PiasekJson));
                if (s != null && s.linear != null && s.linear.Length == 3) return new Vector3(s.linear[0], s.linear[1], s.linear[2]);
            }
        }
        catch (Exception e) { Debug.LogWarning("plaza-piasek.json: " + e.Message); }
        return MeanPiasek;
    }

    static Texture2D Szum()
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(SzumPath);
        if (!t) BraxBuildQuality.Warn("A1_PLAZA_SZUM_MISSING " + SzumPath + " - the sea has no foam lace");
        return t;
    }

    public static Material Piasek(Material m, float sea)
    {
        var shader = Shader.Find("Braxhorn/ChampionsPiasek");
        if (!shader) throw new InvalidOperationException("Champions beach sand shader missing");
        var dryC = AssetDatabase.LoadAssetAtPath<Texture2D>(PiasekDiff);
        var dryN = AssetDatabase.LoadAssetAtPath<Texture2D>(PiasekNor);
        if (!dryC || !dryN)
        {
            BraxBuildQuality.Warn("A1_PLAZA_PIASEK_MISSING " + PiasekDiff + " - the beach keeps its old sand");
            return m;
        }
        var wet = BraxChampionsFoundation.SourceMaterial("plaza_mokry", "damp_beach_sand", 2f, Color.white);
        m.shader = shader;
        m.SetTexture("_MainTex", dryC); m.SetTexture("_BumpMap", dryN);
        m.SetTexture("_Mokry", wet.GetTexture("_MainTex")); m.SetTexture("_MokryBump", wet.GetTexture("_BumpMap"));
        m.SetTexture("_Szum", Szum());
        m.SetVector("_SuchyTint", Tint(Suchy, PiasekSrednia()));
        m.SetVector("_MokryTint", Tint(Mokry, MeanDampBeachSand));
        m.SetVector("_Kafle", new Vector4(15f * K, 11f * K, 0f, 2f * K));
        m.SetFloat("_Poziom", sea);
        m.SetVector("_Mokro", new Vector4(Wbieganie + .02f, Wbieganie + .20f, 0, 0));
        EditorUtility.SetDirty(m);
        return m;
    }

    // the shore field over the sea's extent (and a margin inland for the run-up), from the beach's sand
    public static BraxChampionsMorzeBrzeg Pole(IEnumerable<BraxChampionsFoundation.MeshData> meshes, float sea)
    {
        float sx0 = float.MaxValue, sz0 = float.MaxValue, sx1 = float.MinValue, sz1 = float.MinValue;
        foreach (var m in meshes)
        {
            if (m.role != "sea") continue;
            for (int i = 0; i + 2 < m.positions.Length; i += 3)
            { sx0 = Mathf.Min(sx0, m.positions[i]); sx1 = Mathf.Max(sx1, m.positions[i]); sz0 = Mathf.Min(sz0, m.positions[i + 2]); sz1 = Mathf.Max(sz1, m.positions[i + 2]); }
        }
        var pole = new BraxChampionsMorzeBrzeg(sx0 - 8f, sz0 - 8f, sx1 + 8f, sz1 + 40f, PoleW, PoleH, sea);
        foreach (var m in meshes)
        {
            if (m.role != "sand_dry" && m.role != "sand_wet") continue;
            var p = m.positions; var t = m.triangles;
            Vector3 V(int i) => new Vector3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
            for (int i = 0; i + 2 < t.Length; i += 3) pole.Piasek(V(t[i]), V(t[i + 1]), V(t[i + 2]));
        }
        pole.Licz();
        return pole;
    }

    // 24.11 the city beach's field, kept for its sounds (BraxChampionsFoundation adds them once the scene's root is there)
    public static BraxChampionsMorzeBrzeg PoleMiasta;
    public static Material Morze(IEnumerable<BraxChampionsFoundation.MeshData> meshes, float sea)
    {
        var pole = Pole(meshes, sea); PoleMiasta = pole;
        var m = MorzeZPola(pole, Output + "/Textures/morze_brzeg.asset", Output + "/Materials/morze.mat", "Mistrzowie morze", 0f);
        Debug.Log("A1_PLAZA_MORZE field=" + pole.w + "x" + pole.h + " rect=(" + pole.x0.ToString("F0") + "," + pole.z0.ToString("F0") + ")-(" +
                  pole.x1.ToString("F0") + "," + pole.z1.ToString("F0") + ") sea=" + sea.ToString("F3") + " sandTriangles=" + pole.trojkaty +
                  " waterline=" + pole.Odcinki + " landTexels=" + pole.komorekLadu + " runUp=" + Wbieganie + " (owner 23.11: real beach, waves onto the sand)");
        if (pole.Odcinki == 0) BraxBuildQuality.Warn("A1_PLAZA_NO_WATERLINE the sand never crosses the sea level - no surf, no run-up");
        return m;
    }

    /// <summary>The sea's material over a shore field (the beach's; the villa island's, BraxChampionsWyspaMorze): the field
    /// saved as an RGHalf texture (distance to the waterline, the bed), the waves, the sand under the water. wzdluzZ: 0 for a
    /// shore along x (the beach), 1 for a shore every way round (an island).</summary>
    public static Material MorzeZPola(BraxChampionsMorzeBrzeg pole, string texPath, string matPath, string nazwa, float wzdluzZ)
    {
        var shader = Shader.Find("Braxhorn/ChampionsMorze");
        if (!shader) throw new InvalidOperationException("Champions sea shader missing");
        var tex = new Texture2D(pole.w, pole.h, TextureFormat.RGHalf, false, true)
        { name = nazwa + " brzeg", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, anisoLevel = 0 };
        var px = new ushort[pole.w * pole.h * 2];
        for (int i = 0; i < pole.d.Length; i++)
        {
            px[i * 2] = Mathf.FloatToHalf(Mathf.Clamp(pole.d[i], -2000f, 20000f));
            px[i * 2 + 1] = Mathf.FloatToHalf(pole.dno[i]);
        }
        tex.SetPixelData(px, 0);
        tex.Apply(false, false);
        Directory.CreateDirectory(Output + "/Textures");
        AssetDatabase.DeleteAsset(texPath); AssetDatabase.CreateAsset(tex, texPath);
        var wet = BraxChampionsFoundation.SourceMaterial("plaza_mokry", "damp_beach_sand", 2f, Color.white);
        var m = new Material(shader) { name = nazwa };
        m.SetTexture("_Brzeg", tex);
        m.SetVector("_BrzegRect", new Vector4(pole.x0, pole.z0, 1f / (pole.x1 - pole.x0), 1f / (pole.z1 - pole.z0)));
        m.SetFloat("_Poziom", pole.sea);
        m.SetTexture("_Szum", Szum());
        m.SetTexture("_Dno", wet.GetTexture("_MainTex"));
        m.SetVector("_DnoTint", Tint(Mokry, MeanDampBeachSand));
        m.SetFloat("_DnoKafel", 2f * K);
        m.SetVector("_Fala", new Vector4(Dlugosc, Okres, Wbieganie, ZasiegWbiegania));
        m.SetFloat("_WzdluzZ", wzdluzZ);
        AssetDatabase.DeleteAsset(matPath); AssetDatabase.CreateAsset(m, matPath);
        return m;
    }

    /// <summary>24.11 (owner 01:31 "Wykonałeś mi te dźwięki z plaży?") the sounds of a real sea's beach under parent: its
    /// waterline every 8 units (BraxChampionsMorzeBrzeg.Brzeg), the sea's wave numbers and _WzdluzZ (the waves heard in
    /// time with the picture), sila the surf's strength at a point (null = 1; the seafront's side fade).</summary>
    public static BraxChampionsSzumMorza SzumMorza(Transform parent, BraxChampionsMorzeBrzeg pole, float wzdluzZ, Func<Vector3, float> sila, string nazwa)
    {
        if (pole == null || pole.Odcinki == 0) { BraxBuildQuality.Warn("DO POPRAWKI SZUM_MORZA_NO_WATERLINE " + nazwa); return null; }
        var punkty = pole.Brzeg(8f);
        var go = new GameObject(nazwa); go.transform.SetParent(parent, false); go.transform.position = Vector3.zero;
        var c = go.AddComponent<BraxChampionsSzumMorza>();
        c.brzeg = punkty.ToArray();
        c.sila = new float[c.brzeg.Length];
        for (int i = 0; i < c.brzeg.Length; i++) c.sila[i] = sila == null ? 1f : Mathf.Clamp01(sila(c.brzeg[i]));
        c.wzdluzZ = wzdluzZ; c.dlugosc = Dlugosc; c.okres = Okres;
        Debug.Log("MISTRZOWIE_SZUM_MORZA " + nazwa + " points=" + c.brzeg.Length + " waterline=" + pole.Odcinki + " (owner 24.11: the beach's sounds, the waves in time with the picture)");
        return c;
    }

    // the water the run-up climbs with: the wet sand's mesh at the sea level (the sea's own meshes stop at the sand's edge)
    public static Mesh Plycizna(Mesh piasek, float sea)
    {
        var v = piasek.vertices;
        for (int i = 0; i < v.Length; i++) v[i].y = sea;
        var mesh = new Mesh { name = piasek.name + " plycizna", indexFormat = piasek.indexFormat };
        mesh.vertices = v; mesh.triangles = piasek.triangles;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var b = mesh.bounds; b.Expand(new Vector3(0f, 1f, 0f)); mesh.bounds = b;
        Plycizny++;
        return mesh;
    }
}

// the track-free sand: the colour sRGB, the normal linear (decoded in the shader from RGB, as the region's other GL normals),
// tiled, mipmapped, 2048 px ASTC 6x6 on the phone like the region's ground maps
public sealed class BraxChampionsPlazaPiasekImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    void OnPreprocessTexture()
    {
        bool c = assetPath == BraxChampionsPlaza.PiasekDiff, n = assetPath == BraxChampionsPlaza.PiasekNor;
        if (!c && !n) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default; t.textureShape = TextureImporterShape.Texture2D;
        t.sRGBTexture = c; t.alphaSource = TextureImporterAlphaSource.None;
        t.mipmapEnabled = true; t.isReadable = false; t.streamingMipmaps = true;
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 8;
        t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.CompressedHQ;
        var a = t.GetPlatformTextureSettings("Android");
        a.name = "Android"; a.overridden = true; a.maxTextureSize = 2048; a.format = TextureImporterFormat.ASTC_6x6; a.compressionQuality = 100;
        t.SetPlatformTextureSettings(a);
    }
}

// the sea's noise: linear, tiled, mipmapped, uncompressed (the lace's thin edges and the caustics stay crisp), 512 px
public sealed class BraxChampionsMorzeSzumImport : AssetPostprocessor
{
    public override uint GetVersion() => 1;
    void OnPreprocessTexture()
    {
        if (assetPath != BraxChampionsPlaza.SzumPath) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default; t.textureShape = TextureImporterShape.Texture2D;
        t.sRGBTexture = false; t.alphaSource = TextureImporterAlphaSource.None;
        t.mipmapEnabled = true; t.isReadable = false; t.streamingMipmaps = false;
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 2;
        t.maxTextureSize = 512; t.textureCompression = TextureImporterCompression.Uncompressed;
        var a = t.GetPlatformTextureSettings("Android");
        a.name = "Android"; a.overridden = true; a.maxTextureSize = 512; a.format = TextureImporterFormat.RGB24;
        t.SetPlatformTextureSettings(a);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

// Owner 10.11 19:44-19:51 ("I panoramy nie widzę tylko niebo", "daj panoramę w tle"): beyond the 1.2 x 1.5 km region there
// was nothing - the camera saw the HDR sky's own lower half under the horizon. Now the region stands in a real landscape,
// built here (no download, a few thousand triangles per chunk):
//
// * the SEA goes on to the horizon: the region's own sea shader (sea.mat) on flat strips round the region's sea rectangle,
//   edge to edge with it (x 0 / 1200, z -500), out to 13 km nominal;
// * the COAST goes on both ways from the quays (z -150 at x 0 and 1200) and bends south into two headlands, so from the
//   beach the bay is framed by land on both sides;
// * the LAND round the region: low green hills from the region's edge, forest, then mountain ridges (up to ~1.3 km)
//   2-13 km away, grey rock on the steep and high parts - no brown, no beige (sand is pale grey);
// * the HAZE of distance is the scene's exponential-squared fog in the colour of the HDR sky at the horizon (measured in
//   kloofendal_48d_partly_cloudy_puresky x exposure 1.18): the ridges pale into the sky, the far sea melts into the
//   horizon. CityStaticPBR has no fog, so the city itself stays crisp; at the region's far edge the fog is ~4 %.
//
// Nominal metres (x 1..1200 east, z -500..1000 north) x K once.
public static class BraxChampionsHorizon
{
    public const float X0 = 0f, X1 = 1200f, Z0 = -500f, Z1 = 1000f, QuayZ = -150f, SeaLevel = -2.0f;
    public const float CentreX = 600f, CentreZ = 250f, RingIn = 560f, RingOut = 10000f, SeaOut = 13000f;
    public const int Columns = 320, Rings = 80, Chunks = 8;
    public const float FogDensity = .00014f;                                // per world metre, exponential squared
    public static readonly Color Fog = new Color(.723f, .748f, .815f);     // HDR sky at the horizon (0..1 deg), sRGB
    public const float ViewDistance = 18000f;                               // world metres (camera far plane)

    // ---------------------------------------------------------------- noise (hash value noise, fbm, ridged)
    static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / 4294967295f;
        }
    }
    static float Value(float x, float y)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
    public static float Fbm(float x, float y, int octaves = 5)
    {
        float sum = 0, amp = 1, norm = 0;
        for (int i = 0; i < octaves; i++) { sum += Value(x, y) * amp; norm += amp; amp *= .5f; x = x * 2.03f + 17.1f; y = y * 2.03f - 9.3f; }
        return sum / norm;
    }
    static float Ridged(float x, float y)
    {
        float sum = 0, amp = 1, norm = 0, weight = 1;
        for (int i = 0; i < 6; i++)
        {
            float n = 1 - MathF.Abs(2 * Value(x, y) - 1); n *= n; n *= weight;
            weight = Math.Clamp(n * 1.6f, 0, 1);
            sum += n * amp; norm += amp; amp *= .5f; x = x * 2.1f + 5.7f; y = y * 2.1f + 3.9f;
        }
        return sum / norm;
    }
    static float Smooth(float a, float b, float v) { float t = Math.Clamp((v - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }

    // ---------------------------------------------------------------- the landscape (nominal metres)
    // Coast line outside the region: z -150 at the quays (x 0 and 1200), bending south into the two headlands.
    public static float Coast(float x)
    {
        if (x <= X0)
        {
            float t = X0 - x, f = Smooth(0, 900, t);
            return QuayZ - 1500 * Smooth(300, 6000, t) - f * (90 * MathF.Sin(t / 640) + 45 * MathF.Sin(t / 210 + 1.3f));
        }
        if (x >= X1)
        {
            float t = x - X1, f = Smooth(0, 900, t);
            return QuayZ - 2600 * Smooth(400, 8000, t) - f * (110 * MathF.Sin(t / 760 + .4f) + 50 * MathF.Sin(t / 260 + 2.1f));
        }
        return QuayZ;
    }
    public static bool Inside(float x, float z) => x > X0 && x < X1 && z > Z0 && z < Z1;
    static float RectDistance(float x, float z)
    {
        float dx = MathF.Max(MathF.Max(X0 - x, x - X1), 0), dz = MathF.Max(MathF.Max(Z0 - z, z - Z1), 0);
        return MathF.Sqrt(dx * dx + dz * dz);
    }
    // ground height of the coarse far land: under the D2 railway hill (BraxChampionsD2.Hill*) it is pressed down, so the fine
    // hill patch (RailHill) with the cutting and the tunnel lies over it and nothing of the coarse land shows in the tunnel
    public static float Height(float x, float z)
    {
        float h = FarHeight(x, z);
        if (!Inside(x, z) && BraxChampionsD2.HillR2(x, z) < .78f) h = MathF.Min(h, 0f);
        return h;
    }
    // ground height; inside the region rectangle the far land stays under the region's own ground and sea
    public static float FarHeight(float x, float z)
    {
        if (Inside(x, z)) return z < QuayZ ? SeaLevel - 2f : -.6f;
        float dc = z - Coast(x);
        if (dc <= 0) return SeaLevel - 1.5f - MathF.Min(40f, -dc * .03f);                        // sea bed
        float d = RectDistance(x, z);
        float hills = 75f * Fbm(x / 1100f + 3.1f, z / 1100f - 7.7f) * Smooth(60, 1200, d);
        float ridge = Ridged(x / 3800f + 11.3f, z / 3800f + 2.9f);
        float mountains = 1300f * MathF.Pow(ridge, 1.55f) * Smooth(1700, 7500, d);
        float h = -.25f + (2.2f + hills + mountains) * Smooth(0, 260, dc);
        float r = MathF.Sqrt((x - CentreX) * (x - CentreX) + (z - CentreZ) * (z - CentreZ));
        return h * (1 - .65f * Smooth(8500, RingOut, r));                                        // the outer ring stays lower
    }
    // albedo (sRGB): pale grey sand at the water, meadow and forest, grey rock on the steep and the high
    public static Color Albedo(float x, float z, float h, float slope)
    {
        var sand = new Color(.74f, .74f, .71f); var meadow = new Color(.36f, .45f, .22f); var forest = new Color(.15f, .24f, .11f);
        var rock = new Color(.47f, .48f, .50f); var high = new Color(.58f, .59f, .61f);
        if (h < SeaLevel) return new Color(.20f, .27f, .28f);
        float dc = z - Coast(x);
        if (h < 4f && dc < 60f) return sand;
        float f = Smooth(.40f, .64f, Fbm(x / 420f - 2.2f, z / 420f + 5.1f, 4)) * (1 - Smooth(700, 1000, h)) + .35f * Smooth(120, 400, h) * (1 - Smooth(700, 1000, h));
        var c = Color.Lerp(meadow, forest, Math.Clamp(f, 0, 1));
        c = Color.Lerp(c, rock, Smooth(.30f, .58f, slope) * .85f + Smooth(850, 1150, h) * .9f);
        return Color.Lerp(c, high, Smooth(1050, 1300, h) * .8f);
    }

    // Owner 11.11 13:08-13:09 (D2: "tory ... za miasto", the train goes into a tunnel and disappears): the green hill west of
    // the region with the railway's cutting and the tunnel portal (BraxChampionsD2.HillGround), a fine patch (2 m near the
    // line, 6 m elsewhere) over the coarse land, in the same vertex albedo. Columns either side of the portal 10 cm apart:
    // the cutting floor and the hill above the tube meet inside the portal's headwall.
    public static Chunk RailHill(float k)
    {
        var ch = new Chunk();
        float cx = BraxChampionsD2.HillWX, cz = BraxChampionsD2.HillWZ, R = BraxChampionsD2.HillRX, RZ = BraxChampionsD2.HillRZ, xp = BraxChampionsD2.PortalWX;
        var xs = new List<float>(); var zs = new List<float>();
        for (float x = cx - R - 4f; x <= -40f; x += 6f) xs.Add(x);
        for (float x = -260f; x <= -60f; x += 2f) xs.Add(x);
        xs.RemoveAll(x => MathF.Abs(x - xp) < .5f); xs.Add(xp - .05f); xs.Add(xp + .05f);
        for (float z = cz - RZ - 4f; z <= cz + RZ + 4f; z += 6f) zs.Add(z);
        for (float z = cz - 50f; z <= cz + 50f; z += 2f) zs.Add(z);
        xs.Sort(); zs.Sort();
        Dedup(xs); Dedup(zs);
        int nx = xs.Count, nz = zs.Count;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float x = xs[i], z = zs[j], h = BraxChampionsD2.HillGround(x, z);
                float e = 1f, sx = x >= xp ? 1f : -1f;                                      // one-sided at the portal
                float hx = (BraxChampionsD2.HillGround(x + sx * e, z) - h) / (sx * e), hz = (BraxChampionsD2.HillGround(x, z + e) - BraxChampionsD2.HillGround(x, z - e)) / (2 * e);
                var nn = new Vector3(-hx, 1, -hz).normalized;
                ch.v.Add(new Vector3(x, h, z) * k); ch.n.Add(nn); ch.c.Add(Albedo(x, z, h, 1 - nn.y));
            }
        for (int i = 0; i + 1 < nx; i++)
            for (int j = 0; j + 1 < nz; j++)
            {
                float mx = (xs[i] + xs[i + 1]) * .5f, mz = (zs[j] + zs[j + 1]) * .5f;
                if (BraxChampionsD2.HillR2(mx, mz) > 1.01f) continue;
                int a = i * nz + j, b = (i + 1) * nz + j, c = a + 1, d = b + 1;
                // x east, z north: (i,j) -> (i,j+1) -> (i+1,j) faces up in Unity's winding
                ch.t.Add(a); ch.t.Add(c); ch.t.Add(b);
                ch.t.Add(c); ch.t.Add(d); ch.t.Add(b);
            }
        return ch;
    }
    static void Dedup(List<float> l) { for (int i = l.Count - 1; i > 0; i--) if (l[i] - l[i - 1] < .04f) l.RemoveAt(i); }

    public sealed class Chunk { public List<Vector3> v = new List<Vector3>(); public List<Vector3> n = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<int> t = new List<int>(); }

    // polar grid round the region centre, geometric rings 560 m -> 10 km nominal, 8 chunks of 40 columns (frustum culling)
    public static List<Chunk> Land(float k)
    {
        var chunks = new List<Chunk>();
        int per = Columns / Chunks;
        for (int ci = 0; ci < Chunks; ci++)
        {
            var ch = new Chunk();
            var keep = new float[per + 1, Rings + 1];
            for (int a = 0; a <= per; a++)
                for (int ri = 0; ri <= Rings; ri++)
                {
                    float ang = (ci * per + a) * 2 * MathF.PI / Columns;
                    float r = RingIn * MathF.Pow(RingOut / RingIn, ri / (float)Rings);
                    float x = CentreX + r * MathF.Cos(ang), z = CentreZ + r * MathF.Sin(ang);
                    float h = Height(x, z), e = 4f;
                    float hx = (Height(x + e, z) - Height(x - e, z)) / (2 * e), hz = (Height(x, z + e) - Height(x, z - e)) / (2 * e);
                    var nn = new Vector3(-hx, 1, -hz).normalized;
                    ch.v.Add(new Vector3(x, h, z) * k); ch.n.Add(nn); ch.c.Add(Albedo(x, z, h, 1 - nn.y));
                    keep[a, ri] = Inside(x, z) ? 0 : h < SeaLevel - .5f ? -1 : 1;                 // 0 region, -1 under the sea, 1 land
                }
            for (int a = 0; a < per; a++)
                for (int ri = 0; ri < Rings; ri++)
                {
                    int i00 = a * (Rings + 1) + ri, i01 = i00 + 1, i10 = i00 + Rings + 1, i11 = i10 + 1;
                    bool any = keep[a, ri] > 0 || keep[a, ri + 1] > 0 || keep[a + 1, ri] > 0 || keep[a + 1, ri + 1] > 0;
                    if (!any) continue;                                                                // region or open sea only
                    // angle grows anticlockwise seen from above (x east, z north): (r, a) -> (r+1, a) -> (r, a+1) faces up
                    ch.t.Add(i00); ch.t.Add(i10); ch.t.Add(i01);
                    ch.t.Add(i01); ch.t.Add(i10); ch.t.Add(i11);
                }
            chunks.Add(ch);
        }
        return chunks;
    }

    // the sea round the region's own sea rectangle (x 0..1200, z -500..shore), flush with its edges, under the coast
    public static List<(Vector3 a, Vector3 b, Vector3 c, Vector3 d)> Sea(float k)
    {
        float y = SeaLevel * k, R = SeaOut, top = -100f;
        var q = new List<(Vector3, Vector3, Vector3, Vector3)>();
        void Rect(float x0, float z0, float x1, float z1) =>
            q.Add((new Vector3(x0 * k, y, z0 * k), new Vector3(x0 * k, y, z1 * k), new Vector3(x1 * k, y, z1 * k), new Vector3(x1 * k, y, z0 * k)));
        Rect(CentreX - R, CentreZ - R, X0, top);              // west, under the west coast
        Rect(X1, CentreZ - R, CentreX + R, top);              // east, under the east coast
        Rect(X0, CentreZ - R, X1, Z0);                        // south, edge to edge with the region's sea at z -500
        return q;
    }

#if UNITY_EDITOR
    const string Template = "Assets/Editor/Mistrzowie/BraxChampionsHorizon.shader";
    const string ShaderPath = "Assets/PlayableGenerated/Mistrzowie/MistrzowieHorizon.shader";
    const string SourceName = "Shader \"Braxhorn/MistrzowieHorizonSource\"";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";

    public static void Fogging()
    {
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = FogDensity; RenderSettings.fogColor = Fog;
    }

    public static int Generate()
    {
        if (!System.IO.File.Exists(Template)) throw new System.IO.FileNotFoundException("Horizon source shader missing", Template);
        string src = System.IO.File.ReadAllText(Template);
        if (!src.Contains(SourceName)) throw new InvalidOperationException("Horizon shader identity changed");
        System.IO.File.WriteAllText(ShaderPath, src.Replace(SourceName, "Shader \"Braxhorn/MistrzowieHorizon\""));
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Generated horizon shader failed import: " + ShaderPath);
        var land = new Material(shader) { name = "Champions horizon | far land (vertex albedo, city sun, sky fog)" };
        land.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        string matPath = Out + "/Materials/champions_horizon_land.mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (old) { EditorUtility.CopySerialized(land, old); UnityEngine.Object.DestroyImmediate(land); land = old; } else AssetDatabase.CreateAsset(land, matPath);
        Fogging();

        float k = BraxClaudeOsiedle.K;
        var root = new GameObject("Miasto Mistrzow | HORIZON | far coast, hills and mountains 2-13 km, sea to the horizon (owner 10.11 19:51)");
        int tris = 0, parts = 0;
        var chunks = Land(k);
        for (int i = 0; i < chunks.Count; i++)
        {
            var ch = chunks[i]; if (ch.t.Count == 0) continue;
            var mesh = new Mesh { name = "champions horizon land " + i };
            mesh.SetVertices(ch.v); mesh.SetNormals(ch.n); mesh.SetColors(ch.c); mesh.SetTriangles(ch.t, 0); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Out + "/Meshes/champions_horizon_land_" + i + ".asset");
            Renderer(root.transform, "far land " + i, mesh, land); tris += ch.t.Count / 3; parts++;
        }
        {
            var hill = RailHill(k);
            var mesh = new Mesh { name = "champions horizon D2 rail hill", indexFormat = hill.v.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(hill.v); mesh.SetNormals(hill.n); mesh.SetColors(hill.c); mesh.SetTriangles(hill.t, 0); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Out + "/Meshes/champions_horizon_d2_hill.asset");
            Renderer(root.transform, "D2 railway hill (cutting + tunnel)", mesh, land); tris += hill.t.Count / 3; parts++;
        }
        var sea = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/sea.mat");
        if (!sea) BraxBuildQuality.Warn("DO POPRAWKI CHAMPIONS_HORIZON_SEA_MATERIAL_MISSING " + Out + "/Materials/sea.mat");
        else
        {
            var q = Sea(k); var v = new List<Vector3>(); var t = new List<int>();
            foreach (var (a, b, c, d) in q) { int s = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); t.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 }); }
            var mesh = new Mesh { name = "champions horizon sea" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); var bounds = mesh.bounds; bounds.Expand(new Vector3(0, 1, 0)); mesh.bounds = bounds;
            AssetDatabase.CreateAsset(mesh, Out + "/Meshes/champions_horizon_sea.asset");
            Renderer(root.transform, "far sea to the horizon", mesh, sea); parts++;
        }
        Debug.Log("CHAMPIONS_HORIZON parts=" + parts + " landTriangles=" + tris + " ring=" + RingIn + ".." + RingOut + " seaOut=" + SeaOut + " fog=exp2 " + FogDensity);
        return parts;
    }

    static void Renderer(Transform parent, string name, Mesh mesh, Material m)
    {
        var o = new GameObject(name); o.transform.SetParent(parent, false);
        o.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = m.shader.name.Contains("Horizon") ? ReflectionProbeUsage.Off : ReflectionProbeUsage.BlendProbes;
        GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.OccludeeStatic);
    }
#endif
}

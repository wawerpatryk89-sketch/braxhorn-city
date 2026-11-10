using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

// Owner 10.11 19:14 (photo of the A1 arena from the Hotel Sobczak roof: an empty black roof): "Arena na dachu można mojego
// byka dać żeby z góry był widoczny". The owner's BRAXHORN bull mark as an 80 m 3D relief on the arena roof
// (build_arena_roof_bull.py -> ArenaRoofBull.bytes): brushed steel plates 45 cm thick with bevelled edges, the mark's dark
// seams left open to the black roof, and a green LED band round the silhouette like the mark's green glow. Laid on the roof
// slab (top 27.35 m, build_exteriors.arena) with its +Z front up; the head points west, so it reads upright from the hotel
// and the whole east of the district.
public static class BraxChampionsArenaRoofBull
{
    const string Source = "Assets/Source/Mistrzowie/ArenaRoofBull.bytes";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    public const float RoofTop = 27.35f, Yaw = 90f;

    [Serializable] sealed class SignFile { public float scaleK; public SignData[] signs; }
    [Serializable] sealed class SignData { public string id; public BraxChampionsFoundation.MeshData[] meshes; }

    // CityStaticPBR colour material (as BraxChampionsHarbour.Surface: graphite powder coat, warm-emissive LED)
    static Material Plain(string id, Color c, float roughness, float metallic, Color? emission)
    {
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("A1 roof bull requires the CityStaticPBR shader");
        var m = new Material(shader) { name = "A1 roof bull " + id, enableInstancing = true };
        m.SetColor("_Color", c); m.SetFloat("_Roughness", roughness); m.SetFloat("_Metallic", metallic); m.SetFloat("_VertexTint", 0);
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        if (emission.HasValue) m.SetColor("_EmissionColor", emission.Value);
        string path = Out + "/Materials/" + id + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old) { EditorUtility.CopySerialized(m, old); UnityEngine.Object.DestroyImmediate(m); return old; }
        AssetDatabase.CreateAsset(m, path); return m;
    }

    public static int Generate(Transform arenaRoot)
    {
        if (!File.Exists(Source)) { BraxBuildQuality.Warn("DO POPRAWKI A1_ROOF_BULL_MISSING " + Source + " (build_arena_roof_bull.py)"); return 0; }
        SignFile data;
        using (var f = File.OpenRead(Source)) using (var zip = new GZipStream(f, CompressionMode.Decompress)) using (var r = new StreamReader(zip))
            data = JsonUtility.FromJson<SignFile>(r.ReadToEnd());
        if (data?.signs == null || data.signs.Length != 1) { BraxBuildQuality.Warn("DO POPRAWKI A1_ROOF_BULL_EMPTY"); return 0; }
        // neutral polished steel (the Metal048A map is warm: it read beige from the hotel), like the mark's silver plates
        var steel = Plain("a1_roof_bull_polished_steel", new Color(.82f, .84f, .87f), .26f, .85f, null);
        var edge = Plain("a1_roof_bull_graphite_edges", new Color(.09f, .095f, .10f), .42f, .55f, null);
        var led = Plain("a1_roof_bull_green_led", new Color(.32f, .95f, .22f), .35f, 0f, new Color(.40f, 1.45f, .28f));
        float k = BraxClaudeOsiedle.K;
        var group = new GameObject("A1 ROOF | BRAXHORN bull mark 80 m, steel plates + green LED (owner 10.11 19:14)").transform;
        group.SetParent(arenaRoot, false);
        group.localPosition = new Vector3(0, RoofTop * k, 0);
        group.localRotation = Quaternion.Euler(-90f, Yaw, 0f);          // +Z front up, the head to the west
        int parts = 0, tris = 0;
        foreach (var m in data.signs[0].meshes)
        {
            int count = m.positions == null ? 0 : m.positions.Length / 3; if (count == 0 || m.triangles == null) continue;
            var v = new Vector3[count]; var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                v[i] = new Vector3(m.positions[3 * i], m.positions[3 * i + 1], m.positions[3 * i + 2]);
                uv[i] = m.uv != null && m.uv.Length >= 2 * count ? new Vector2(m.uv[2 * i], m.uv[2 * i + 1]) : Vector2.zero;
            }
            var mesh = new Mesh { name = "A1 roof bull " + m.role, indexFormat = count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = m.triangles;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Out + "/Meshes/a1_roof_bull_" + m.role + ".asset");
            var o = new GameObject("A1 roof bull | " + m.role); o.transform.SetParent(group, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = o.AddComponent<MeshRenderer>();
            rend.sharedMaterial = m.role == "led" ? led : m.role == "steel_edge" ? edge : steel;
            GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            parts++; tris += m.triangles.Length / 3;
        }
        Debug.Log("A1_ROOF_BULL owner mark 80m parts=" + parts + " triangles=" + tris + " roofTop=" + RoofTop + " yaw=" + Yaw);
        return parts;
    }
}

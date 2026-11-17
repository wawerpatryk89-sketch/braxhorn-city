// Stand-ins for the villa preview: the aquapark's water materials and the swim zone (runtime) as plain records.
using UnityEngine;
public static partial class BraxChampionsAquapark
{
    internal static Material Water(string id, Color deep, Color shallow, float opacity, float ripple, float bubbles)
        => new Material { name = "water:" + id, color = new Color(shallow.r, shallow.g, shallow.b, .55f) };
    internal static Material Volume(string id, Color deep, Color shallow, float density, float caustics)
        => new Material { name = "waterbody:" + id, color = new Color(deep.r, deep.g, deep.b, .35f) };
}
public class BraxSwimmingZone : MonoBehaviour { public Transform waterSurface; public bool allowsSwimming = true; public string basinId; }
public sealed class BraxChampionsPoolSwimVolume : BraxSwimmingZone { }

// the villa sites' preview (BraxChampionsWilleDzialki): the far land's D2 hill is not drawn here, the promenade's materials
public static class BraxChampionsD2
{
    public const float HillWX = -330f, HillWZ = 186.5f, HillRX = 250f, HillRZ = 400f, PortalWX = -185f;
    public static float HillR2(float wx, float wz) { float a = (wx - HillWX) / HillRX, b = (wz - HillWZ) / HillRZ; return Mathf.Sqrt(a * a + b * b); }
    public static float HillGround(float wx, float wz) => BraxChampionsHorizon.FarHeight(wx, wz);
}
public static class BraxChampionsPromenadeFurniture
{
    public static Material Teak, Kerb, Deck;
    public static void Materials()
    {
        Teak = BraxChampionsFoundation.SourceMaterial("prom_bench_silver_teak_slats", "oak_wood_planks", .9f, new Color(.975f, 1.32f, 1.76f));
        Kerb = BraxChampionsFoundation.SourceMaterial("prom_kerb", "granite_tile_03", 1.6f, new Color(.8f, .82f, .86f));
    }
}

// R1's model helpers (Assets/Editor/Claude/Region/BraxRegion.cs, read-only): the region's trees are its drzewo_verdant;
// the preview draws the real .glb (LOD 0, render3d hides liscie_1/2) where the file is, placed as BraxRegion.Put does
public static class BraxRegion
{
    public const string PH = "Assets/Source/Claude/PolyHaven/", Lib = "Assets/Source/Claude/Library/";
    public static GameObject Model(params string[] ids)
    {
        foreach (var id in ids)
            foreach (var p in new[] { PH + "models/" + id + "/" + id + ".glb", Lib + id + ".glb" })
            {
                if (!System.IO.File.Exists(p)) continue;
                var g = new GameObject(id); GameObject.Registry.Remove(g);
                var pm = g.AddComponent<PreviewModel>(); pm.path = p; (pm.min, pm.max) = GltfBounds.Read(p);
                g.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true };
                return g;
            }
        return null;
    }
    public static GameObject Put(Transform parent, GameObject source, string label, Vector3 floor, float yaw,
                                 float scale = 1f, bool solid = true, float maxHeight = 0f, float sink = 0f, bool skalaK = true)
    {
        if (!source) return null;
        float kk = BraxClaudeOsiedle.K;
        if (skalaK && Mathf.Abs(scale - kk) > .002f) { scale *= kk; if (maxHeight > 0f) maxHeight *= kk; }
        var o = UnityEngine.Object.Instantiate(source); o.transform.SetParent(parent, false); o.name = label;
        o.transform.position = Vector3.zero;
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
        if (Mathf.Abs(scale - 1f) > .0001f) o.transform.localScale *= scale;
        Bounds B() { var rs = o.GetComponentsInChildren<Renderer>(true); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); return bb; }
        var b = B();
        if (maxHeight > 0f && b.size.y > maxHeight) { o.transform.localScale *= maxHeight / b.size.y; b = B(); }
        o.transform.position = floor - new Vector3(b.center.x, b.min.y + sink, b.center.z);
        return o;
    }
}

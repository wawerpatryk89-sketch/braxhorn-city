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

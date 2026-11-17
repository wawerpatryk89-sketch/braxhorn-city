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

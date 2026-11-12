// Runtime parts used by the aquapark (Unity only): the preview records their configuration fields.
using UnityEngine;
public class BraxSwimmingZone : MonoBehaviour { public Transform waterSurface; public bool allowsSwimming = true; public string basinId; }
public sealed class BraxChampionsPoolSwimVolume : BraxSwimmingZone { }
public sealed class BraxChampionsZjezdzalnia : MonoBehaviour
{
    public string nazwa; public Vector3[] trasa = new Vector3[0], normalne = new Vector3[0]; public Vector3 start, wyjscie;
    public float zasieg, g, tarcie, vStart, vMin, vMax, lezenie; public BraxSeat siedzisko; public Transform plusk;
}
// the rigged walkers are Unity-only (skinned meshes, legacy clips): the preview keeps their routes
public static class BraxChampionsPostacie
{
    public static readonly System.Collections.Generic.List<Vector3[]> Routes = new System.Collections.Generic.List<Vector3[]>();
    public static int Aquapark(Transform root, System.Collections.Generic.IList<Vector3[]> trasy) { Routes.AddRange(trasy); return trasy.Count; }
}
// the region's looping 3D water sound (R1 runtime): the preview records only its settings
public sealed class BraxZrodloPetli : MonoBehaviour { public string rodzaj = "woda"; public float glosnosc = .5f, zasieg = 30f; }
// the lazy river's drifting rings (Unity only): the preview records the loop and the rings
public sealed class BraxChampionsLeniwaRzeka : MonoBehaviour
{
    public Vector3[] petla = new Vector3[0], brzeg = new Vector3[0]; public Transform[] kola = new Transform[0]; public BraxSeat[] siedziska = new BraxSeat[0];
    public float predkosc, zasieg, siedzenie, kolysanie;
}

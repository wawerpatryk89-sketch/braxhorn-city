// Runtime parts used by the aquapark (Unity only): the preview records their configuration fields.
using UnityEngine;
public class BraxSwimmingZone : MonoBehaviour { public Transform waterSurface; public bool allowsSwimming = true; public string basinId; }
public sealed class BraxChampionsPoolSwimVolume : BraxSwimmingZone { }
public sealed class BraxChampionsZjezdzalnia : MonoBehaviour
{
    public string nazwa; public Vector3[] trasa = new Vector3[0], normalne = new Vector3[0]; public Vector3 start, wyjscie;
    public float zasieg, g, tarcie, vStart, vMin, vMax, lezenie; public BraxSeat siedzisko; public Transform plusk;
}

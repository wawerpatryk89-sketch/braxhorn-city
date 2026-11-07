using UnityEngine;

// Explicit non-solid environmental surface. The audit measures its real
// vertical depth to the registered terrain instead of treating water as a
// floor-standing prop.
[DisallowMultipleComponent]
public sealed class BraxObozFluidSurface : MonoBehaviour
{
    public Renderer surface;
    public MeshCollider bed;
    public float minimumDepthMetres=.05f;
    public float maximumDepthMetres=1.25f;
}

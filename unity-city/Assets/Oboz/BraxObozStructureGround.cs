using UnityEngine;

// Explicit reference to the scene's actual terrain mesh. A collider higher
// in the hierarchy, or an arbitrary floor-shaped object, is not a foundation.
[DisallowMultipleComponent]
public sealed class BraxObozStructureGround : MonoBehaviour
{
    public MeshCollider surface;
    public MeshFilter geometry;
    public Renderer visibleSurface;
}


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

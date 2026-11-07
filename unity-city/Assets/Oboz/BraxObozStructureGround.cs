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

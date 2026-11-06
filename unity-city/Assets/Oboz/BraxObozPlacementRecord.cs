using UnityEngine;

// Persisted evidence about the complete placed object. The scene audit must
// exclude this entire hierarchy from support rays, not just one mesh child.
[DisallowMultipleComponent]
public sealed class BraxObozPlacementRecord : MonoBehaviour
{
    public enum SupportKind { Unspecified, Ground, Wall, Suspended }
    public SupportKind support;
    public Vector3 wallDirection;
    public Collider verticalSupport;
    public float expectedDimensionMetres;
    public int dimensionAxis;
    public Transform dimensionFrame;
    public float heightAboveFloorMetres = -1f;
    public int heightReference;
    public Renderer[] heightRenderers = new Renderer[0];
    public string label;

    public static BraxObozPlacementRecord For(Transform model)
    {
        var record = model.GetComponent<BraxObozPlacementRecord>();
        return record ? record : model.gameObject.AddComponent<BraxObozPlacementRecord>();
    }
}

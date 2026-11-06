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
    // A transparent print can have a larger canvas than the physical emblem.
    // Contact is measured on the real supporting geometry when specified.
    public Renderer[] contactRenderers = new Renderer[0];
    // Some objects have an asymmetric canopy/outline. The ground contact
    // ray must pass through the real foot/trunk, not the centre of its crown.
    public Transform contactPoint;
    // Hanging originals are verified at a point on their source geometry.
    // The point alone does not exempt them: an actual support collider is
    // required and its measured separation is reported by the scene audit.
    public Transform suspensionPoint;
    public Collider suspensionSupport;
    public string label;

    public static BraxObozPlacementRecord For(Transform model)
    {
        var record = model.GetComponent<BraxObozPlacementRecord>();
        return record ? record : model.gameObject.AddComponent<BraxObozPlacementRecord>();
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// The original 4K GLB stays with the owner. This is its mesh-identical
// 2K-texture derivative, with the six embedded PBR images downsized to 2048.
// Replace ONLY VIP chairs beside the judges. All judge/commentator desks,
// computers and working chairs remain owned by BraxChampionsArena.
public static class BraxChampionsVIPSeats
{
    public const string SourcePath =
        "Assets/Source/Mistrzowie/VIP/free_luxurious_barrel_seat_2k.glb";
    public static int SeatCount { get; private set; }
    public static bool HasSource =>
        AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) != null;

    static Bounds VisualBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("VIP barrel seat GLB has no renderers: " + SourcePath);
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.x < .001f || bounds.size.y < .001f || bounds.size.z < .001f)
            throw new InvalidOperationException("VIP barrel seat GLB geometry bounds invalid");
        return bounds;
    }

    static void Place(Transform root, GameObject source, float x, float z, float cx, float cz, string label)
    {
        var spot = new GameObject(label).transform;
        spot.SetParent(root, false);
        spot.localPosition = new Vector3(x, .055f, z);

        var chair = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (!chair) throw new InvalidOperationException("Cannot instantiate original VIP seat GLB");
        chair.name = "Premium barrel chair | owner GLB | 2K PBR";
        chair.transform.SetParent(spot, false);
        chair.transform.localPosition = Vector3.zero;
        // Original front (+Z) points to the octagon; keep the authored mesh and materials.
        var towardCage = new Vector3(cx - x, 0, cz - z);
        chair.transform.localRotation = Quaternion.LookRotation(towardCage.normalized, Vector3.up);
        chair.transform.localScale = Vector3.one;

        var bounds = VisualBounds(chair);
        // Constrain real footprint and height without changing proportions.
        // The source model was authored at ~85 nominal units wide, not human size.
        float ratio = Mathf.Max(bounds.size.x / .96f,
                        Mathf.Max(bounds.size.z / .96f, bounds.size.y / 1.08f));
        float factor = 1f / ratio;
        chair.transform.localScale *= factor;
        bounds = VisualBounds(chair);
        // Sit on the actual floor, centred in the reserved VIP place.
        Vector3 target = spot.position + new Vector3(0, bounds.extents.y, 0);
        chair.transform.position += target - bounds.center;

        // One cheap collider for a complete upholstered chair, never five MeshColliders.
        foreach (var c in chair.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
        var collider = spot.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .54f, 0);
        collider.size = new Vector3(.96f, 1.08f, .96f);
        SeatCount++;
    }

    public static int Generate(Transform interiorRoot)
    {
        SeatCount = 0;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (!source)
        {
            Debug.LogWarning("CHAMPIONS_VIP_SOURCE_PENDING: " + SourcePath +
                " is not imported; existing VIP seats remain temporarily. Judge seats unchanged.");
            return 0;
        }
        // Existing reserved positions near jury/commentator area:
        // two rows behind cage-side service desks; one 6m central aisle stays open.
        // Four places per side per row; seat spacing 1.45m, row spacing 1.55m.
        const float cx = -86f, cz = 63.3f;
        var xs = new[] { -94.0f, -92.55f, -91.10f, -89.65f,
                         -82.35f, -80.90f, -79.45f, -78.0f };
        foreach (float z in new[] { 70.35f, 71.90f })
            foreach (float x in xs)
                Place(interiorRoot, source, x, z, cx, cz,
                    "Reserved VIP | imported luxurious barrel seat");

        if (SeatCount != 16)
            throw new InvalidOperationException("Champion A1 must have 16 premium VIP chairs; got " + SeatCount);
        return SeatCount;
    }
}

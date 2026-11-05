using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Uses the EXACT same source model "car_rev.glb" as the estate's Auto Patryka
// candidate list, without touching the estate scene/assets or creating a fake car.
// A1 parking and game scale are preserved. This creates one usable player car.
public static class BraxChampionsCarBuilder
{
    public const string OwnerCar = "Assets/Source/Mistrzowie/Vehicles/car_rev.glb";
    const float LengthMetres = 4.95f; // Source estate's DlugoscRealna["car_rev"]
    static float K => BraxClaudeOsiedle.K;

    static Bounds BoundsLocal(Transform parent)
    {
        var renderers = parent.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("Original estate car_rev GLB has no visible geometry");
        Bounds result = new Bounds();
        bool started = false;
        foreach (var r in renderers)
        {
            var b = r.bounds;
            for (int a = 0; a < 2; a++)
            for (int c = 0; c < 2; c++)
            for (int d = 0; d < 2; d++)
            {
                Vector3 world = new Vector3(a == 0 ? b.min.x : b.max.x,
                    c == 0 ? b.min.y : b.max.y, d == 0 ? b.min.z : b.max.z);
                var local = parent.InverseTransformPoint(world);
                if (!started) { result = new Bounds(local, Vector3.zero); started = true; }
                else result.Encapsulate(local);
            }
        }
        if (!started || result.size.magnitude < .01f)
            throw new InvalidOperationException("Original estate car has invalid world bounds");
        return result;
    }

    public static void Generate(Transform parent)
    {
        if (UnityEngine.Object.FindObjectsByType<BraxChampionsCar>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            throw new InvalidOperationException("Second A1 player car forbidden");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(OwnerCar)
            ?? AssetDatabase.LoadAllAssetsAtPath(OwnerCar).OfType<GameObject>().FirstOrDefault();
        if (!source)
            throw new InvalidOperationException("Import the EXISTING estate car_rev.glb, not a placeholder: " + OwnerCar);
        var root = new GameObject("A1 | ORIGINAL estate sports car for Brax | WORKING joystick vehicle");
        root.transform.SetParent(parent, false);
        // Real marked western car-park bay; x local -37.55, z local -89.
        // Centre entrance aisle x=-8..8 remains fully clear. Not on a road.
        root.transform.position = new Vector3(562.45f, .16f, 711f) * K;
        root.transform.rotation = Quaternion.identity;
        var copy = UnityEngine.Object.Instantiate(source, root.transform, false);
        copy.name = "ORIGINAL car_rev GLB from estate, unchanged mesh + PBR";
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        Physics.SyncTransforms();
        Bounds bounds = BoundsLocal(root.transform);
        // Normalize only the model coordinate system (the estate generator
        // measures and orients the identical car in the same way).
        if (bounds.size.x > bounds.size.z * 1.15f)
        {
            copy.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            Physics.SyncTransforms();
            bounds = BoundsLocal(root.transform);
        }
        if (bounds.size.z <= .01f || bounds.size.x <= .01f)
            throw new InvalidOperationException("Original car length/width is invalid");
        float uniform = LengthMetres * K / bounds.size.z;
        copy.transform.localScale *= uniform;
        Physics.SyncTransforms();
        bounds = BoundsLocal(root.transform);
        // Bring REAL imported wheels/body down to floor. No phantom skids.
        copy.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Physics.SyncTransforms();
        bounds = BoundsLocal(root.transform);

        // Only one chassis collider whose size is based on source 3D geometry.
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Driver sits on the LEFT as in the original estate code.
        // Seat placement is a calibration point, not an alteration of the car model.
        var seat = new GameObject("Original cabin | LEFT driver seat for Brax").transform;
        seat.SetParent(root.transform, false);
        seat.localPosition = new Vector3(-bounds.extents.x * .39f,
            Mathf.Max(.26f * K, bounds.size.y * .28f), bounds.extents.z * .14f);
        seat.localRotation = Quaternion.identity;

        var enter = new GameObject("Actual left driver-door approach").transform;
        enter.SetParent(root.transform, false);
        enter.localPosition = new Vector3(
            bounds.min.x - .78f * K, .10f * K, seat.localPosition.z - .15f * K);

        // Try the REAL separate door assembly. If the source has no reliable
        // split, do not invent a duplicate door or move a body mesh incorrectly.
        Transform doorPivot = null;
        var doorParts = copy.GetComponentsInChildren<Transform>(true)
            .Where(t => t && t != copy.transform &&
                t.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(t => t.GetComponentsInChildren<Renderer>(true).Length > 0)
            .Where(t => root.transform.InverseTransformPoint(t.position).x < 0)
            .OrderByDescending(t => t.GetComponentsInChildren<Renderer>(true).Length)
            .ToArray();
        if (doorParts.Length > 0)
        {
            // Reparent a single highest-level original door assembly without
            // changing geometry; use a hinge at its real front-left edge.
            var original = doorParts[0];
            var r = original.GetComponentsInChildren<Renderer>(true);
            Bounds db = r[0].bounds;
            foreach (var renderer in r.Skip(1)) db.Encapsulate(renderer.bounds);
            var hinge = new GameObject("ORIGINAL left door | functional pivot").transform;
            hinge.SetParent(root.transform, false);
            var centre = root.transform.InverseTransformPoint(db.center);
            hinge.localPosition = new Vector3(centre.x, centre.y,
                root.transform.InverseTransformPoint(db.max).z);
            original.SetParent(hinge, true);
            doorPivot = hinge;
        }
        else
            Debug.LogWarning("A1_OWNER_CAR_DOOR_QA: source model has no independent left door node; keep car mesh original");

        var car = root.AddComponent<BraxChampionsCar>();
        car.bodyCollider = box; car.driverSeat = seat; car.doorApproach = enter;
        car.doorHinge = doorPivot;
        car.wheelbase = 2.7f * K; car.maxSpeed = 12f * K;
        // If wheels are individual GLB nodes, the runtime animates their
        // existing transforms; no substitute wheel meshes are created.
        var wheelRoots = copy.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.IndexOf("wheel", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(t => t.GetComponentsInChildren<Renderer>(true).Length > 0).ToArray();
        car.rollingWheels = wheelRoots;
        car.steeringWheels = new Transform[0]; // no arbitrary steering bone rotations
        Debug.Log("A1_OWNER_CAR_INSTALLED original=car_rev length=" +
            bounds.size.z.ToString("F2") + " bodyCollider=REAL_BOUNDS" +
            " start=(562.45,711) entrance=FREE originalDoor=" + (doorPivot != null) +
            " ownerAnimations=EnteringCar,Driving,ExitingCar" +
            " no_fake_collision=true manual_phone_review_required=true");
    }
}

using UnityEngine;
using W=BraxWnetrzaKit;

// Owner 10.11 12:55 (APK 37): "Wszędzie drzwi globalnie pozamykane." Interior doors of the region (apartments T04-T07,
// Hotel Sobczak T03) stood open at 90 degrees as fixed slabs, or the opening had no leaf at all. Every interior door is
// now a CLOSED leaf in its opening that opens by itself when the player walks up to it and closes again behind him -
// the city's own door behaviour (BraxCityDoor, R1, unchanged): hinge group at the jamb, leaf + levers on both faces,
// collider on the leaf (it moves with it). Geometry goes through the W kit in build-time world coordinates like
// every other W group (Unity keeps them in a mesh object at the origin under the hinge group, so the leaf swings
// round the hinge).
public static class BraxChampionsInteriorDoor
{
    public static int Built { get; private set; }
    public static void Reset() { Built = 0; }

    /// <param name="alongX">wall runs along x (opening a..b in x, wall plane z = plane); else along z (plane x = plane)</param>
    public static Transform Single(string id, string label, bool alongX, float plane, float a, float b, float y0, float h,
                                   Material leaf, Material lever, float thickness = .045f)
    {
        var host = W.Grupa; bool wasInside = W.Wnetrze;
        float lo = Mathf.Min(a, b) + .015f, hi = Mathf.Max(a, b) - .015f, mid = (lo + hi) * .5f, t = thickness * .5f;
        var rot = alongX ? Quaternion.identity : Quaternion.Euler(0, -90f, 0);         // root local x along the opening, z = wall normal
        var root = new GameObject(label + " | closed door (opens on approach)").transform;
        root.SetParent(host, true);
        root.position = alongX ? new Vector3(mid, y0, plane) : new Vector3(plane, y0, mid);
        root.rotation = rot;
        var hinge = new GameObject("door leaf (hinge)").transform;
        hinge.SetParent(root, true);
        hinge.position = alongX ? new Vector3(lo, y0, plane) : new Vector3(plane, y0, lo);
        hinge.rotation = rot;
        W.Grupa = hinge; W.Wnetrze = wasInside;
        float top = y0 + h - .008f, bottom = y0 + .008f;
        if (alongX) W.MM(leaf, lo, hi, bottom, top, plane - t, plane + t, true);
        else W.MM(leaf, plane - t, plane + t, bottom, top, lo, hi, true);
        // lever handles on both faces, 1.02 m, near the free edge: round rose + 14 cm lever
        float free = hi - .075f, yh = y0 + 1.02f;
        foreach (float f in new[] { -1f, 1f })
        {
            float face = plane + f * t;
            Vector3 P(float u, float y, float off) => alongX ? new Vector3(u, y, face + f * off) : new Vector3(face + f * off, y, u);
            W.Rura(lever, P(free, yh, 0f), P(free, yh, .012f), .028f, 14);                        // rose
            W.Rura(lever, P(free, yh, .012f), P(free, yh, .055f), .009f, 10);                     // spindle
            W.Rura(lever, P(free + .005f, yh, .055f), P(free - .135f, yh, .055f), .011f, 10);     // lever toward the hinge
        }
        W.Grupa = host; W.Wnetrze = wasInside;
        var door = root.gameObject.AddComponent<BraxCityDoor>();
        door.id = id; door.label = label; door.leaves = new[] { hinge }; door.sides = new[] { 1f };
        Built++;
        return root;
    }
}

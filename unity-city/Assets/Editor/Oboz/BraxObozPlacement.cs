using System;
using System.Linq;
using UnityEngine;

// OBÓZ: jeden punkt kalibracji rzeczywistego wymiaru, bez wciskania GLB/FBX
// w wirtualne pudelko min(width/height/depth). Kazda os zachowuje proporcje.
public static class BraxObozPlacement
{
    public const float K = 1.294f;
    public enum Axis { X, Y, Z }
    public enum HeightReference { Bottom, Centre, Top }

    static Vector3 Direction(Axis axis)
    {
        return axis == Axis.X ? Vector3.right : axis == Axis.Y ? Vector3.up : Vector3.forward;
    }

    // Renderer.bounds is a WORLD axis-aligned box. Transforming that box back
    // into a rotated cabin inflates it a second time. Start with each renderer's
    // own local box and transform all eight corners into the requested frame.
    public static Bounds BoundsInFrame(Transform frame, Renderer[] renderers)
    {
        Bounds result = new Bounds();
        bool first = true;
        foreach (var renderer in renderers)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            var local = renderer.localBounds;
            var matrix = renderer.localToWorldMatrix;
            if (frame) matrix = frame.worldToLocalMatrix * matrix;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var point = matrix.MultiplyPoint3x4(local.center +
                    Vector3.Scale(local.extents, new Vector3(x,y,z)));
                if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                else result.Encapsulate(point);
            }
        }
        if (first) throw new InvalidOperationException("Brak aktywnej geometrii ustawianego modelu");
        return result;
    }

    public static float Dimension(Renderer[] renderers, Axis axis, Transform axisFrame = null)
    {
        var size = BoundsInFrame(axisFrame, renderers).size;
        float local = axis == Axis.X ? size.x : axis == Axis.Y ? size.y : size.z;
        float frameScale = axisFrame ? axisFrame.TransformVector(Direction(axis)).magnitude : 1f;
        return local * frameScale;
    }

    public static Bounds Bounds(Renderer[] renderers)
    {
        var active = renderers.Where(r => r && r.enabled && r.gameObject.activeInHierarchy).ToArray();
        if (active.Length == 0) throw new InvalidOperationException("Brak aktywnej geometrii ustawianego modelu");
        var bounds = active[0].bounds;
        foreach (var r in active.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    public static void RealDimension(Transform model, Renderer[] renderers,
        float metres, Axis axis, string label, Transform axisFrame = null)
    {
        if (metres <= 0 || float.IsNaN(metres) || float.IsInfinity(metres))
            throw new ArgumentOutOfRangeException(nameof(metres));
        // Callers may specify the cabin axes explicitly. The default is the
        // parent frame, so turning the complete building cannot swap length/width.
        if (!axisFrame) axisFrame = model.parent;
        float measured = Dimension(renderers, axis, axisFrame);
        if (measured < .0001f || float.IsNaN(measured) || float.IsInfinity(measured))
            throw new InvalidOperationException("Niepoprawny wymiar źródła: " + label);
        float factor = metres * K / measured; // jedna OS rzeczywista, NIE min(trzech osi)
        model.localScale *= factor;
        var record = BraxObozPlacementRecord.For(model);
        record.expectedDimensionMetres = metres;
        record.dimensionAxis = (int)axis;
        record.dimensionFrame = axisFrame;
        record.label = label;
        Debug.Log("BRAXHORN_OBOZ_REAL_DIMENSION " + label + " axis=" + axis +
                  " source=" + measured + " target=" + (metres*K) +
                  " after=" + Bounds(renderers).size);
    }

    public static void NativeMetres(Transform model, Renderer[] renderers, string label)
    {
        // Gdy rodzic ma K, nie stosuj K po raz drugi.
        float parentScale = model.parent ? Mathf.Abs(model.parent.lossyScale.y) : 1f;
        if (parentScale < .0001f) throw new InvalidOperationException("Zerowa skala rodzica: " + label);
        model.localScale *= K / parentScale;
        var measured = Bounds(renderers).size;
        if (measured.x > 15*K || measured.y > 9*K || measured.z > 15*K)
            Debug.LogWarning("BRAXHORN_OBOZ_ORIGINAL_SCALE_REVIEW " + label +
                             " źródło bez zatwierdzonego wymiaru referencyjnego: " + measured);
    }

    public static void VerifyDimension(Renderer[] renderers, float realMetres,
        Axis axis, string label, float tolerance = .15f, Transform axisFrame = null)
    {
        float actual = Dimension(renderers, axis, axisFrame);
        if (Mathf.Abs(actual - realMetres*K) > realMetres*K*tolerance)
            Debug.LogWarning("BRAXHORN_OBOZ_PROPORTIONS_REVIEW " + label + " " + axis +
                             " actual=" + actual + " expected=" + (realMetres*K));
    }

    public static void StandingSurfaceHeight(Transform model, Renderer[] renderers,
        Renderer[] surfaceRenderers, float metres, string label)
    {
        // Legs must remain on the floor; the specified washbasin height ends
        // at the basin, not at the top of its original faucet.
        float measured = Bounds(surfaceRenderers).max.y - Bounds(renderers).min.y;
        if (measured < .0001f) throw new InvalidOperationException("Brak wysokości powierzchni: " + label);
        model.localScale *= metres*K/measured;
        var record = BraxObozPlacementRecord.For(model);
        record.label = label;
        record.heightAboveFloorMetres = metres;
        record.heightReference = (int)HeightReference.Top;
        record.heightRenderers = surfaceRenderers;
    }

    public static void AtBottom(Transform model, Renderer[] renderers, Vector3 bottomWorld)
    {
        var b = Bounds(renderers);
        model.position += new Vector3(bottomWorld.x-b.center.x,
                                      bottomWorld.y-b.min.y,
                                      bottomWorld.z-b.center.z);
        Physics.SyncTransforms();
    }

    public static bool OnGround(Transform model, Renderer[] renderers,
        Vector3 approximateBottomWorld, string label)
    {
        var record = BraxObozPlacementRecord.For(model);
        record.support = BraxObozPlacementRecord.SupportKind.Ground;
        record.label = label;
        AtBottom(model, renderers, approximateBottomWorld);
        // Tylko kolizje poziome nie należące do przenoszonego modelu.
        var bounds = Bounds(renderers);
        Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + .35f*K, bounds.center.z);
        var candidates = Physics.RaycastAll(origin, Vector3.down, bounds.size.y + 1.4f*K,
            ~0, QueryTriggerInteraction.Ignore);
        var support = candidates.Where(h => h.collider && !h.collider.transform.IsChildOf(model) &&
                    h.normal.y > .7f && h.point.y <= approximateBottomWorld.y+.20f*K &&
                    h.point.y >= approximateBottomWorld.y-.75f*K)
                .OrderByDescending(h => h.point.y).ToArray();
        if (support.Length == 0)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_NO_FLOOR_RAYCAST " + label +
                " y=" + approximateBottomWorld.y + " — pozostawiono pozycję bez zgadywania");
            return false;
        }
        var b = Bounds(renderers);
        record.verticalSupport = support[0].collider;
        model.position += Vector3.up*(support[0].point.y-b.min.y);
        Physics.SyncTransforms();
        return true;
    }

    // Height is measured above the actual floor, never above an arbitrary
    // building pivot. Wall placement follows this vertical calibration.
    public static void AtHeightAboveGround(Transform model, Renderer[] renderers,
        Vector3 approximateFloorWorld, float metres, HeightReference reference, string label,
        Renderer[] referenceRenderers = null)
    {
        if (!OnGround(model, renderers, approximateFloorWorld, label)) return;
        var bounds = Bounds(renderers);
        var referenceBounds = referenceRenderers != null && referenceRenderers.Length > 0 ?
            Bounds(referenceRenderers) : bounds;
        float edge = reference == HeightReference.Top ? referenceBounds.max.y :
                     reference == HeightReference.Centre ? referenceBounds.center.y : referenceBounds.min.y;
        model.position += Vector3.up*(bounds.min.y + metres*K - edge);
        var record = BraxObozPlacementRecord.For(model);
        record.heightAboveFloorMetres = metres;
        record.heightReference = (int)reference;
        record.heightRenderers = referenceRenderers ?? Array.Empty<Renderer>();
        Physics.SyncTransforms();
    }

    // Explicit support avoids choosing another piece of furniture or a lower
    // shelf. The support's real collider must exist before this call.
    public static bool OnSupport(Transform model, Renderer[] renderers,
        Transform support, Vector3 approximateBottomWorld, string label)
    {
        var record = BraxObozPlacementRecord.For(model);
        record.support = BraxObozPlacementRecord.SupportKind.Ground;
        record.label = label;
        AtBottom(model, renderers, approximateBottomWorld);
        var b = Bounds(renderers);
        Vector3 origin = new Vector3(b.center.x, b.max.y + .35f*K, b.center.z);
        var hits = Physics.RaycastAll(origin, Vector3.down, b.size.y + 2f*K,
                ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.collider && h.collider.transform.IsChildOf(support) &&
                        !h.collider.transform.IsChildOf(model) && h.normal.y > .7f)
            .OrderByDescending(h => h.point.y).ToArray();
        if (hits.Length == 0)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_NO_SUPPORT_RAYCAST " + label +
                " support=" + support.name + " — brak trafienia w powierzchnię źródłową");
            return false;
        }
        record.verticalSupport = hits[0].collider;
        model.position += Vector3.up*(hits[0].point.y-b.min.y);
        Physics.SyncTransforms();
        return true;
    }

    public static void ToWall(Transform model, Renderer[] renderers,
        Vector3 from, Vector3 towardWall, string label)
    {
        var d = towardWall.normalized;
        var record = BraxObozPlacementRecord.For(model);
        record.support = BraxObozPlacementRecord.SupportKind.Wall;
        record.wallDirection = d;
        record.label = label;
        Physics.SyncTransforms();
        var hits = Physics.RaycastAll(from, d, 2f*K, ~0,
            QueryTriggerInteraction.Ignore)
            .Where(h => h.collider && !h.collider.transform.IsChildOf(model) &&
                        Vector3.Dot(h.normal, -d) > .55f)
            .OrderBy(h => h.distance).ToArray();
        if (hits.Length == 0)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_NO_WALL_RAYCAST " + label +
                " — montaż wymaga weryfikacji ściany");
            return;
        }
        // Project original local boxes along the wall normal. A rotated world
        // AABB would otherwise leave a gap or push the object into the wall.
        float rear = float.NegativeInfinity;
        foreach (var renderer in renderers)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            var b = renderer.localBounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var p = renderer.localToWorldMatrix.MultiplyPoint3x4(b.center +
                    Vector3.Scale(b.extents, new Vector3(x,y,z)));
                rear = Mathf.Max(rear, Vector3.Dot(p,d));
            }
        }
        float delta = Vector3.Dot(hits[0].point,d) - rear - .005f*K;
        model.position += d*delta;
        Physics.SyncTransforms();
    }
}

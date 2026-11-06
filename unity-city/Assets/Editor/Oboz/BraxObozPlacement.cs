using System;
using System.Linq;
using UnityEngine;

// OBÓZ: jeden punkt kalibracji rzeczywistego wymiaru, bez wciskania GLB/FBX
// w wirtualne pudelko min(width/height/depth). Kazda os zachowuje proporcje.
public static class BraxObozPlacement
{
    public const float K = 1.294f;
    public enum Axis { X, Y, Z }

    public static Bounds Bounds(Renderer[] renderers)
    {
        var active = renderers.Where(r => r && r.enabled && r.gameObject.activeInHierarchy).ToArray();
        if (active.Length == 0) throw new InvalidOperationException("Brak aktywnej geometrii ustawianego modelu");
        var bounds = active[0].bounds;
        foreach (var r in active.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    public static void RealDimension(Transform model, Renderer[] renderers,
        float metres, Axis axis, string label)
    {
        if (metres <= 0 || float.IsNaN(metres) || float.IsInfinity(metres))
            throw new ArgumentOutOfRangeException(nameof(metres));
        var size = Bounds(renderers).size;
        float measured = axis == Axis.X ? size.x : axis == Axis.Y ? size.y : size.z;
        if (measured < .0001f || float.IsNaN(measured) || float.IsInfinity(measured))
            throw new InvalidOperationException("Niepoprawny wymiar źródła: " + label);
        float factor = metres * K / measured; // jedna OS rzeczywista, NIE min(trzech osi)
        model.localScale *= factor;
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
        Axis axis, string label, float tolerance = .15f)
    {
        float actual = axis == Axis.X ? Bounds(renderers).size.x :
                       axis == Axis.Y ? Bounds(renderers).size.y : Bounds(renderers).size.z;
        if (Mathf.Abs(actual - realMetres*K) > realMetres*K*tolerance)
            Debug.LogWarning("BRAXHORN_OBOZ_PROPORTIONS_REVIEW " + label + " " + axis +
                             " actual=" + actual + " expected=" + (realMetres*K));
    }

    public static void AtBottom(Transform model, Renderer[] renderers, Vector3 bottomWorld)
    {
        var b = Bounds(renderers);
        model.position += new Vector3(bottomWorld.x-b.center.x,
                                      bottomWorld.y-b.min.y,
                                      bottomWorld.z-b.center.z);
        Physics.SyncTransforms();
    }

    public static void OnGround(Transform model, Renderer[] renderers,
        Vector3 approximateBottomWorld, string label)
    {
        AtBottom(model, renderers, approximateBottomWorld);
        // Tylko kolizje poziome nie należące do przenoszonego modelu.
        Vector3 origin = approximateBottomWorld + Vector3.up*(.35f*K);
        var candidates = Physics.RaycastAll(origin, Vector3.down, 1.4f*K,
            ~0, QueryTriggerInteraction.Ignore);
        var support = candidates.Where(h => h.collider && !h.collider.transform.IsChildOf(model) &&
                    h.normal.y > .7f && h.point.y <= approximateBottomWorld.y+.20f*K &&
                    h.point.y >= approximateBottomWorld.y-.75f*K)
                .OrderByDescending(h => h.point.y).ToArray();
        if (support.Length == 0)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_NO_FLOOR_RAYCAST " + label +
                " y=" + approximateBottomWorld.y + " — pozostawiono pozycję bez zgadywania");
            return;
        }
        var b = Bounds(renderers);
        model.position += Vector3.up*(support[0].point.y-b.min.y);
        Physics.SyncTransforms();
    }

    public static void ToWall(Transform model, Renderer[] renderers,
        Vector3 from, Vector3 towardWall, string label)
    {
        var d = towardWall.normalized;
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
        var b = Bounds(renderers);
        Vector3 rear = b.center + Vector3.Scale(b.extents,
            new Vector3(Mathf.Sign(d.x), Mathf.Sign(d.y), Mathf.Sign(d.z)));
        float delta = Vector3.Dot(hits[0].point-rear,d) - .005f*K;
        model.position += d*delta;
        Physics.SyncTransforms();
    }
}

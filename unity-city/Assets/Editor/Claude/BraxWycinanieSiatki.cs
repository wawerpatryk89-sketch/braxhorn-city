using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Subtract boxes from triangles, including triangles that cross a box without a
// vertex inside. Keep all vertex channels, material slots and the original mesh.
public static class BraxWycinanieSiatki
{
    public static List<Bounds> Odejmij(Bounds source, IEnumerable<Bounds> cuts)
    {
        var pieces = new List<Bounds> { source };
        foreach (var cut in cuts)
        {
            var next = new List<Bounds>();
            foreach (var piece in pieces)
            {
                Vector3 lo = piece.min, hi = piece.max;
                if (hi.x <= cut.min.x || lo.x >= cut.max.x || hi.y <= cut.min.y || lo.y >= cut.max.y || hi.z <= cut.min.z || lo.z >= cut.max.z)
                { next.Add(piece); continue; }
                for (int axis = 0; axis < 3; axis++)
                {
                    if (hi[axis] > cut.max[axis]) { var a = lo; a[axis] = cut.max[axis]; var b = new Bounds(); b.SetMinMax(a, hi); next.Add(b); hi[axis] = cut.max[axis]; }
                    if (lo[axis] < cut.min[axis]) { var a = hi; a[axis] = cut.min[axis]; var b = new Bounds(); b.SetMinMax(lo, a); next.Add(b); lo[axis] = cut.min[axis]; }
                }
            }
            pieces = next;
        }
        return pieces;
    }

    public static bool WytnijKolizje(BoxCollider collider, IEnumerable<Bounds> cuts)
    {
        if (!collider.enabled || collider.attachedRigidbody) return false;
        var t = collider.transform;
        // A world AABB is exact only for boxes aligned to the world axes (also 90° rotations).
        bool Axis(Vector3 d) => Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z)) > .99999f;
        if (!Axis(t.right) || !Axis(t.up) || !Axis(t.forward)) return false;
        var original = collider.bounds;
        var pieces = Odejmij(original, cuts);
        if (pieces.Count == 1 && pieces[0] == original) return false;
        foreach (var piece in pieces)
        {
            if (piece.size.x < .001f || piece.size.y < .001f || piece.size.z < .001f) continue;
            var go = new GameObject(collider.name + " przycięta kolizja");
            go.layer = collider.gameObject.layer; go.transform.position = piece.center;
            go.transform.SetParent(t, true);
            var c = go.AddComponent<BoxCollider>(); c.size = piece.size;
            c.sharedMaterial = collider.sharedMaterial; c.isTrigger = collider.isTrigger; c.contactOffset = collider.contactOffset;
        }
        collider.enabled = false;
        return true;
    }

    public static Mesh Wytnij(Mesh mesh, Matrix4x4 world, Bounds[] boxes, out int changed)
    {
        changed = 0;
        var positions = new List<Vector3>(mesh.vertices);
        var normals = new List<Vector3>(mesh.normals);
        var tangents = new List<Vector4>(mesh.tangents);
        var colors = new List<Color>(mesh.colors);
        var uv = new List<Vector4>[8];
        for (int ch = 0; ch < 8; ch++) { uv[ch] = new List<Vector4>(); mesh.GetUVs(ch, uv[ch]); }
        int originalCount = positions.Count;
        bool hasNormals = normals.Count == originalCount, hasTangents = tangents.Count == originalCount, hasColors = colors.Count == originalCount;
        var hasUV = new bool[8]; for (int ch = 0; ch < 8; ch++) hasUV[ch] = uv[ch].Count == originalCount;
        int Blend(int a, int b, float t)
        {
            int id = positions.Count;
            positions.Add(Vector3.Lerp(positions[a], positions[b], t));
            if (hasNormals) normals.Add(Vector3.Lerp(normals[a], normals[b], t).normalized);
            if (hasTangents) tangents.Add(Vector4.Lerp(tangents[a], tangents[b], t));
            if (hasColors) colors.Add(Color.Lerp(colors[a], colors[b], t));
            for (int ch = 0; ch < 8; ch++) if (hasUV[ch]) uv[ch].Add(Vector4.Lerp(uv[ch][a], uv[ch][b], t));
            return id;
        }
        List<int> Half(List<int> polygon, int axis, float plane, bool above)
        {
            var result = new List<int>();
            for (int i = 0; i < polygon.Count; i++)
            {
                int a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                float da = (world.MultiplyPoint3x4(positions[a])[axis] - plane) * (above ? 1 : -1);
                float db = (world.MultiplyPoint3x4(positions[b])[axis] - plane) * (above ? 1 : -1);
                if (da >= 0) result.Add(a);
                if ((da >= 0) != (db >= 0)) result.Add(Blend(a,b,da/(da-db)));
            }
            return result;
        }
        var submeshes = new List<int>[mesh.subMeshCount];
        for (int sub = 0; sub < submeshes.Length; sub++)
        {
            var output = submeshes[sub] = new List<int>();
            var indices = mesh.GetTriangles(sub);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var pieces = new List<List<int>> { new List<int> { indices[i], indices[i+1], indices[i+2] } };
                bool touched = false;
                foreach (var box in boxes)
                {
                    var outside = new List<List<int>>();
                    foreach (var piece in pieces)
                    {
                        var bounds = new Bounds(world.MultiplyPoint3x4(positions[piece[0]]), Vector3.zero);
                        foreach (int id in piece) bounds.Encapsulate(world.MultiplyPoint3x4(positions[id]));
                        if (bounds.max.x <= box.min.x || bounds.min.x >= box.max.x ||
                            bounds.max.y <= box.min.y || bounds.min.y >= box.max.y ||
                            bounds.max.z <= box.min.z || bounds.min.z >= box.max.z) { outside.Add(piece); continue; }
                        touched = true;
                        var remaining = piece;
                        for (int axis = 0; axis < 3 && remaining.Count >= 3; axis++)
                        {
                            var high = Half(remaining,axis,box.max[axis],true); if (high.Count >= 3) outside.Add(high);
                            remaining = Half(remaining,axis,box.max[axis],false);
                            if (remaining.Count < 3) break;
                            var low = Half(remaining,axis,box.min[axis],false); if (low.Count >= 3) outside.Add(low);
                            remaining = Half(remaining,axis,box.min[axis],true);
                        }
                    }
                    pieces = outside;
                    if (pieces.Count == 0) break;
                }
                if (touched) changed++;
                foreach (var piece in pieces) for (int k = 1; k + 1 < piece.Count; k++)
                {
                    if (Vector3.Cross(positions[piece[k]] - positions[piece[0]], positions[piece[k+1]] - positions[piece[0]]).sqrMagnitude < 1e-16f) continue;
                    output.Add(piece[0]); output.Add(piece[k]); output.Add(piece[k+1]);
                }
            }
        }
        if (changed == 0) return null;
        // Intermediate clipping vertices must not inflate subsequent cuts or saved assets.
        var used = new List<int>(); var remap = new Dictionary<int, int>();
        foreach (var indices in submeshes) for (int i = 0; i < indices.Count; i++)
        {
            int old = indices[i];
            if (!remap.TryGetValue(old, out int id)) { id = used.Count; remap.Add(old, id); used.Add(old); }
            indices[i] = id;
        }
        List<T> Compact<T>(List<T> values) { var result = new List<T>(used.Count); foreach (int id in used) result.Add(values[id]); return result; }
        var copy = new Mesh { name = mesh.name + "_przyciete", indexFormat = used.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        copy.SetVertices(Compact(positions));
        if (hasNormals) copy.SetNormals(Compact(normals));
        if (hasTangents) copy.SetTangents(Compact(tangents));
        if (hasColors) copy.SetColors(Compact(colors));
        for (int ch = 0; ch < 8; ch++) if (hasUV[ch]) copy.SetUVs(ch,Compact(uv[ch]));
        copy.subMeshCount = submeshes.Length;
        for (int sub = 0; sub < submeshes.Length; sub++) copy.SetTriangles(submeshes[sub],sub);
        copy.RecalculateBounds();
        return copy;
    }
}

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

    // ---------------- ODEJMOWANIE TRÓJKĄTÓW (build 45, migotanie: „starą zwalić, nie nakładać”) ----------------
    // Z trójkątów siatki leżących w płaszczyźnie osiowej (oś os: 0 = X, 1 = Y, 2 = Z; zwrot znak: 1 = normalna dodatnia;
    // położenie d ± tol w świecie) odejmuje sumę podanych trójkątów 2D (u, v jak w BraxKontrolaMigotania: U = z dla osi X,
    // inaczej x; V = z dla osi Y, inaczej y) — dokładnie pod górną warstwą, nie po prostokątach obrysu. Kawałki przecięte
    // brzegiem dostają nowe wierzchołki z interpolacji barycentrycznej wszystkich kanałów (normalne, styczne, kolory, UV 0–7).
    // tylkoSub ≥ 0: tnie tylko ten materiał. Brak zmian = null (oryginał nietknięty).
    public static float U2(Vector3 p, int os) => os == 0 ? p.z : p.x;
    public static float V2(Vector3 p, int os) => os == 1 ? p.z : p.y;
    static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    struct Pkt { public Vector2 p; public Vector3 w; }
    static List<Pkt> Przytnij(List<Pkt> poly, Vector2 ea, Vector2 eb, float s, bool wewnatrz)
    {
        var res = new List<Pkt>(poly.Count + 2); var e = eb - ea;
        for (int i = 0; i < poly.Count; i++)
        {
            var a = poly[i]; var b = poly[(i + 1) % poly.Count];
            float da = s * Cross2(e, a.p - ea), db = s * Cross2(e, b.p - ea);
            if (!wewnatrz) { da = -da; db = -db; }
            if (da >= 0) res.Add(a);
            if ((da >= 0) != (db >= 0)) { float t = da / (da - db); res.Add(new Pkt { p = Vector2.Lerp(a.p, b.p, t), w = Vector3.Lerp(a.w, b.w, t) }); }
        }
        return res;
    }
    static float Pole(List<Pkt> poly)
    {
        float a = 0; for (int i = 1; i + 1 < poly.Count; i++) a += Cross2(poly[i].p - poly[0].p, poly[i + 1].p - poly[0].p);
        return Mathf.Abs(a) * .5f;
    }

    public static Mesh OdejmijTrojkaty(Mesh mesh, Matrix4x4 world, int tylkoSub, int os, int znak, float d, float tol, IList<Vector2[]> gorne, out int changed, out float polePrzed, out float polePo)
    {
        changed = 0; polePrzed = 0; polePo = 0;
        if (gorne == null || gorne.Count == 0) return null;
        // indeks górnych trójkątów w komórkach 2 m
        const float C = 2f;
        var siatka = new Dictionary<long, List<int>>();
        var obrysy = new Rect[gorne.Count];
        for (int i = 0; i < gorne.Count; i++)
        {
            var t = gorne[i];
            float u0 = Mathf.Min(t[0].x, Mathf.Min(t[1].x, t[2].x)), u1 = Mathf.Max(t[0].x, Mathf.Max(t[1].x, t[2].x));
            float v0 = Mathf.Min(t[0].y, Mathf.Min(t[1].y, t[2].y)), v1 = Mathf.Max(t[0].y, Mathf.Max(t[1].y, t[2].y));
            obrysy[i] = Rect.MinMaxRect(u0, v0, u1, v1);
            for (int cu = Mathf.FloorToInt(u0 / C); cu <= Mathf.FloorToInt(u1 / C); cu++)
                for (int cv = Mathf.FloorToInt(v0 / C); cv <= Mathf.FloorToInt(v1 / C); cv++)
                {
                    long k = ((long)(cu + 100000) << 32) | (uint)(cv + 100000);
                    if (!siatka.TryGetValue(k, out var l)) siatka[k] = l = new List<int>();
                    l.Add(i);
                }
        }
        var positions = new List<Vector3>(mesh.vertices);
        var normals = new List<Vector3>(mesh.normals);
        var tangents = new List<Vector4>(mesh.tangents);
        var colors = new List<Color>(mesh.colors);
        var uv = new List<Vector4>[8];
        for (int ch = 0; ch < 8; ch++) { uv[ch] = new List<Vector4>(); mesh.GetUVs(ch, uv[ch]); }
        int n0 = positions.Count;
        bool hasN = normals.Count == n0, hasT = tangents.Count == n0, hasC = colors.Count == n0;
        var hasUV = new bool[8]; for (int ch = 0; ch < 8; ch++) hasUV[ch] = uv[ch].Count == n0;
        var wp = new Vector3[n0]; for (int i = 0; i < n0; i++) wp[i] = world.MultiplyPoint3x4(positions[i]);
        int Nowy(int ia, int ib, int ic, Vector3 w)
        {
            int id = positions.Count;
            positions.Add(positions[ia] * w.x + positions[ib] * w.y + positions[ic] * w.z);
            if (hasN) normals.Add((normals[ia] * w.x + normals[ib] * w.y + normals[ic] * w.z).normalized);
            if (hasT) tangents.Add(tangents[ia] * w.x + tangents[ib] * w.y + tangents[ic] * w.z);
            if (hasC) colors.Add(colors[ia] * w.x + colors[ib] * w.y + colors[ic] * w.z);
            for (int ch = 0; ch < 8; ch++) if (hasUV[ch]) uv[ch].Add(uv[ch][ia] * w.x + uv[ch][ib] * w.y + uv[ch][ic] * w.z);
            return id;
        }
        var kandydaci = new HashSet<int>();
        var submeshes = new List<int>[mesh.subMeshCount];
        for (int sub = 0; sub < submeshes.Length; sub++)
        {
            var output = submeshes[sub] = new List<int>();
            var indices = mesh.GetTriangles(sub);
            if (tylkoSub >= 0 && sub != tylkoSub) { output.AddRange(indices); continue; }
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                Vector3 a = wp[ia], b = wp[ib], c = wp[ic];
                var nn = Vector3.Cross(b - a, c - a); float len = nn.magnitude;
                bool wPlaszczyznie = len > 1e-9f;
                if (wPlaszczyznie)
                {
                    float no = nn[os] / len;
                    wPlaszczyznie = Mathf.Abs(no) >= .995f && (no > 0) == (znak == 1) && Mathf.Abs((a[os] + b[os] + c[os]) / 3f - d) <= tol;
                }
                if (!wPlaszczyznie) { output.Add(ia); output.Add(ib); output.Add(ic); continue; }
                Vector2 a2 = new Vector2(U2(a, os), V2(a, os)), b2 = new Vector2(U2(b, os), V2(b, os)), c2 = new Vector2(U2(c, os), V2(c, os));
                var ob = Rect.MinMaxRect(Mathf.Min(a2.x, Mathf.Min(b2.x, c2.x)), Mathf.Min(a2.y, Mathf.Min(b2.y, c2.y)), Mathf.Max(a2.x, Mathf.Max(b2.x, c2.x)), Mathf.Max(a2.y, Mathf.Max(b2.y, c2.y)));
                kandydaci.Clear();
                for (int cu = Mathf.FloorToInt(ob.xMin / C); cu <= Mathf.FloorToInt(ob.xMax / C); cu++)
                    for (int cv = Mathf.FloorToInt(ob.yMin / C); cv <= Mathf.FloorToInt(ob.yMax / C); cv++)
                        if (siatka.TryGetValue(((long)(cu + 100000) << 32) | (uint)(cv + 100000), out var l)) foreach (var k in l) if (obrysy[k].Overlaps(ob)) kandydaci.Add(k);
                float pole0 = Mathf.Abs(Cross2(b2 - a2, c2 - a2)) * .5f;
                if (kandydaci.Count == 0) { output.Add(ia); output.Add(ib); output.Add(ic); continue; }
                var kawalki = new List<List<Pkt>> { new List<Pkt> { new Pkt { p = a2, w = new Vector3(1, 0, 0) }, new Pkt { p = b2, w = new Vector3(0, 1, 0) }, new Pkt { p = c2, w = new Vector3(0, 0, 1) } } };
                bool dotkniety = false;
                foreach (int k in kandydaci)
                {
                    var t = gorne[k]; float sk = Cross2(t[1] - t[0], t[2] - t[0]);
                    if (Mathf.Abs(sk) < 1e-8f) continue;
                    float s = Mathf.Sign(sk);
                    var nowe = new List<List<Pkt>>();
                    foreach (var kaw in kawalki)
                    {
                        float ku0 = float.MaxValue, ku1 = float.MinValue, kv0 = float.MaxValue, kv1 = float.MinValue;
                        foreach (var q in kaw) { ku0 = Mathf.Min(ku0, q.p.x); ku1 = Mathf.Max(ku1, q.p.x); kv0 = Mathf.Min(kv0, q.p.y); kv1 = Mathf.Max(kv1, q.p.y); }
                        if (ku1 <= obrysy[k].xMin || ku0 >= obrysy[k].xMax || kv1 <= obrysy[k].yMin || kv0 >= obrysy[k].yMax) { nowe.Add(kaw); continue; }
                        var reszta = kaw; var czesci = new List<List<Pkt>>(3);
                        for (int e = 0; e < 3 && reszta.Count >= 3; e++)
                        {
                            var ea = t[e]; var eb = t[(e + 1) % 3];
                            var poza = Przytnij(reszta, ea, eb, s, false);
                            if (poza.Count >= 3 && Pole(poza) > 1e-7f) czesci.Add(poza);
                            reszta = Przytnij(reszta, ea, eb, s, true);
                        }
                        // część pod górnym trójkątem odpada; bez części wspólnej — kawałek zostaje w całości (bez nowych wierzchołków)
                        if (reszta.Count >= 3 && Pole(reszta) > 1e-7f) { dotkniety = true; nowe.AddRange(czesci); }
                        else nowe.Add(kaw);
                    }
                    kawalki = nowe;
                    if (kawalki.Count == 0) break;
                }
                polePrzed += pole0;
                if (!dotkniety) { output.Add(ia); output.Add(ib); output.Add(ic); polePo += pole0; continue; }
                changed++;
                foreach (var kaw in kawalki)
                {
                    if (kaw.Count < 3 || Pole(kaw) < 1e-6f) continue;
                    polePo += Pole(kaw);
                    int i0 = Nowy(ia, ib, ic, kaw[0].w);
                    for (int q = 1; q + 1 < kaw.Count; q++) { output.Add(i0); output.Add(Nowy(ia, ib, ic, kaw[q].w)); output.Add(Nowy(ia, ib, ic, kaw[q + 1].w)); }
                }
            }
        }
        if (changed == 0) return null;
        var used = new List<int>(); var remap = new Dictionary<int, int>();
        foreach (var ind in submeshes) for (int i = 0; i < ind.Count; i++)
        {
            int old = ind[i];
            if (!remap.TryGetValue(old, out int id)) { id = used.Count; remap.Add(old, id); used.Add(old); }
            ind[i] = id;
        }
        List<T> Compact<T>(List<T> values) { var result = new List<T>(used.Count); foreach (int id in used) result.Add(values[id]); return result; }
        var copy = new Mesh { name = mesh.name + "_bez_nakladania", indexFormat = used.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        copy.SetVertices(Compact(positions));
        if (hasN) copy.SetNormals(Compact(normals));
        if (hasT) copy.SetTangents(Compact(tangents));
        if (hasC) copy.SetColors(Compact(colors));
        for (int ch = 0; ch < 8; ch++) if (hasUV[ch]) copy.SetUVs(ch, Compact(uv[ch]));
        copy.subMeshCount = submeshes.Length;
        for (int sub = 0; sub < submeshes.Length; sub++) copy.SetTriangles(submeshes[sub], sub);
        copy.RecalculateBounds();
        return copy;
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

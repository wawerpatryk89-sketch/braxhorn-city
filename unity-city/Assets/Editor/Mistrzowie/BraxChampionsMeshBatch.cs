using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Shared geometry batch of the region's waterfront (owner 10.11 "promenada ... port porządny"): railings, pontoon
// fittings, kiosks and street furniture are hundreds of small parts. Instead of one GameObject per primitive they are
// collected per material in NOMINAL metres and written as a few combined meshes (x K once), each saved as an asset.
// Faces are wound like Unity's own cube (clockwise seen from outside); UVs are planar metres (the CityStaticPBR
// materials tile by metres, like the foundation and the W kit).
public sealed class BraxChampionsMeshBatch
{
    sealed class Part { public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>(); }
    readonly Dictionary<Material, Part> parts = new Dictionary<Material, Part>();
    readonly List<Material> order = new List<Material>();
    static int saved;
    static float K => BraxClaudeOsiedle.K;

    Part Of(Material m)
    {
        if (!m) throw new ArgumentNullException(nameof(m));
        if (!parts.TryGetValue(m, out var p)) { p = new Part(); parts.Add(m, p); order.Add(m); }
        return p;
    }

    public int Triangles { get { int n = 0; foreach (var p in parts.Values) n += p.t.Count / 3; return n; } }
    public bool Empty { get { foreach (var p in parts.Values) if (p.t.Count > 0) return false; return true; } }

    static Vector2 Planar(Vector3 p, Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
        if (ax >= az) return new Vector2(p.z * Mathf.Sign(n.x), p.y);
        return new Vector2(-p.x * Mathf.Sign(n.z), p.y);
    }

    /// <summary>Quad a-b-c-d, front face clockwise seen from the front (Unity winding).</summary>
    public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var p = Of(m); int i = p.v.Count;
        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - a, d - a);
        n.Normalize();
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d); p.n.Add(n); p.n.Add(n); p.n.Add(n); p.n.Add(n);
        p.uv.Add(Planar(a, n)); p.uv.Add(Planar(b, n)); p.uv.Add(Planar(c, n)); p.uv.Add(Planar(d, n));
        p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2); p.t.Add(i); p.t.Add(i + 2); p.t.Add(i + 3);
    }

    public void Triangle(Material m, Vector3 a, Vector3 b, Vector3 c)
    {
        var p = Of(m); int i = p.v.Count;
        var n = Vector3.Cross(b - a, c - a).normalized;
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.n.Add(n); p.n.Add(n); p.n.Add(n);
        p.uv.Add(Planar(a, n)); p.uv.Add(Planar(b, n)); p.uv.Add(Planar(c, n));
        p.t.Add(i); p.t.Add(i + 1); p.t.Add(i + 2);
    }

    /// <summary>Oriented box: centre, full size, rotation (nominal metres). bottom=false leaves the underside open.</summary>
    public void Box(Material m, Vector3 c, Vector3 size, Quaternion r, bool bottom = true)
    {
        Vector3 h = size * .5f, X = r * new Vector3(h.x, 0, 0), Y = r * new Vector3(0, h.y, 0), Z = r * new Vector3(0, 0, h.z);
        Vector3 P(int sx, int sy, int sz) => c + X * sx + Y * sy + Z * sz;
        Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));      // +Z
        Quad(m, P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));  // -Z
        Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));  // -X
        Quad(m, P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));      // +X
        Quad(m, P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1));      // +Y
        if (bottom) Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
    }
    public void Box(Material m, Vector3 c, Vector3 size) => Box(m, c, size, Quaternion.identity);

    /// <summary>Box with EVERY edge and corner rounded to radius r and smooth normals (owner 10.11 20:45: "nie rób
    /// wszystkich kwadratowych, tylko zaokrąglone" - armrests, cushions, table tops, cabinets). seg = steps per quarter
    /// round. The flat middle of every face is one quad.</summary>
    public void RoundedBox(Material m, Vector3 c, Vector3 size, float r, Quaternion rot, int seg = 4)
    {
        var h = size * .5f;
        r = Mathf.Min(r, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .98f);
        if (r <= 1e-4f) { Box(m, c, size, rot); return; }
        float[] Axis(float e)
        {
            var s = new List<float>();
            for (int i = 0; i <= seg; i++) s.Add(-e + r * i / seg);
            for (int i = 0; i <= seg; i++) { float v = e - r + r * i / seg; if (v > s[s.Count - 1] + 1e-5f) s.Add(v); }
            return s.ToArray();
        }
        float[] ax = Axis(h.x), ay = Axis(h.y), az = Axis(h.z);
        var inner = h - Vector3.one * r;
        void Map(Vector3 q, out Vector3 pos, out Vector3 nrm)
        {
            var k = new Vector3(Mathf.Clamp(q.x, -inner.x, inner.x), Mathf.Clamp(q.y, -inner.y, inner.y), Mathf.Clamp(q.z, -inner.z, inner.z));
            var d = q - k; float l = d.magnitude;
            nrm = rot * (d / l); pos = c + rot * (k + d * (r / l));
        }
        var p = Of(m);
        void Face(float[] us, float[] vs, Func<float, float, Vector3> P)
        {
            for (int i = 0; i + 1 < us.Length; i++)
                for (int j = 0; j + 1 < vs.Length; j++)
                {
                    Map(P(us[i], vs[j]), out var a, out var na); Map(P(us[i + 1], vs[j]), out var b, out var nb);
                    Map(P(us[i + 1], vs[j + 1]), out var cc, out var nc); Map(P(us[i], vs[j + 1]), out var d, out var nd);
                    var fn = (na + nb + nc + nd).normalized;
                    int s = p.v.Count;
                    if (Vector3.Dot(Vector3.Cross(cc - a, d - b), fn) > 0) { p.v.Add(a); p.v.Add(b); p.v.Add(cc); p.v.Add(d); p.n.Add(na); p.n.Add(nb); p.n.Add(nc); p.n.Add(nd); }
                    else { p.v.Add(d); p.v.Add(cc); p.v.Add(b); p.v.Add(a); p.n.Add(nd); p.n.Add(nc); p.n.Add(nb); p.n.Add(na); }
                    for (int q = 0; q < 4; q++) p.uv.Add(Planar(p.v[s + q], fn));
                    p.t.Add(s); p.t.Add(s + 1); p.t.Add(s + 2); p.t.Add(s); p.t.Add(s + 2); p.t.Add(s + 3);
                }
        }
        Face(ay, az, (y, z) => new Vector3(h.x, y, z)); Face(ay, az, (y, z) => new Vector3(-h.x, y, z));
        Face(ax, az, (x, z) => new Vector3(x, h.y, z)); Face(ax, az, (x, z) => new Vector3(x, -h.y, z));
        Face(ax, ay, (x, y) => new Vector3(x, y, h.z)); Face(ax, ay, (x, y) => new Vector3(x, y, -h.z));
    }
    public void RoundedBox(Material m, Vector3 c, Vector3 size, float r, int seg = 4) => RoundedBox(m, c, size, r, Quaternion.identity, seg);
    public void RoundedBoxMinMax(Material m, float x0, float x1, float y0, float y1, float z0, float z1, float r, int seg = 4)
        => RoundedBox(m, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), r, Quaternion.identity, seg);
    public void BoxMinMax(Material m, float x0, float x1, float y0, float y1, float z0, float z1)
        => Box(m, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)));

    /// <summary>Vertical cylinder (or along the rotation's up axis) from its base centre.</summary>
    public void Cylinder(Material m, Vector3 baseCentre, float radius, float height, int sides, Quaternion r, bool top = true, bool bottom = false, float topRadius = -1f)
    {
        if (topRadius < 0) topRadius = radius;
        var up = r * Vector3.up;
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 d0 = r * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = r * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            Vector3 b0 = baseCentre + d0 * radius, b1 = baseCentre + d1 * radius, t0 = baseCentre + up * height + d0 * topRadius, t1 = baseCentre + up * height + d1 * topRadius;
            int q = Of(m).v.Count;
            Quad(m, b1, b0, t0, t1);
            if (sides >= 10)
            {
                // smooth shading round the side (rounded furniture, legs, lamps): radial normals, tilted for a cone
                var p = Of(m); var face = p.n[q];
                Vector3 n0 = (d0 * height + up * (radius - topRadius)).normalized, n1 = (d1 * height + up * (radius - topRadius)).normalized;
                if (Vector3.Dot(face, n0 + n1) < 0f) { n0 = -n0; n1 = -n1; }
                p.n[q] = n1; p.n[q + 1] = n0; p.n[q + 2] = n0; p.n[q + 3] = n1;
            }
            if (top && topRadius > 0) Triangle(m, baseCentre + up * height, t1, t0);
            if (bottom) Triangle(m, baseCentre, b0, b1);
        }
    }
    public void Cylinder(Material m, Vector3 baseCentre, float radius, float height, int sides = 12, bool top = true)
        => Cylinder(m, baseCentre, radius, height, sides, Quaternion.identity, top);

    /// <summary>Round tube from a to b (railing, rope, pole); sag bends it down in the middle (rope).</summary>
    public void Tube(Material m, Vector3 a, Vector3 b, float radius, int sides = 8, int divisions = 1, float sag = 0f)
    {
        var dir = b - a; if (dir.sqrMagnitude < 1e-8f) return; dir.Normalize();
        var basis = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > .9f ? Vector3.forward : Vector3.up;
        var right = Vector3.Cross(dir, basis).normalized; var up = Vector3.Cross(right, dir).normalized;
        Vector3 Ring(int k, int j)
        {
            float t = (float)k / divisions, ang = j * Mathf.PI * 2f / sides;
            var c = Vector3.Lerp(a, b, t) - Vector3.up * (sag * Mathf.Sin(Mathf.PI * t));
            return c + (right * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * radius;
        }
        Vector3 N(int j) { float ang = j * Mathf.PI * 2f / sides; return right * Mathf.Cos(ang) + up * Mathf.Sin(ang); }
        for (int k = 0; k < divisions; k++)
            for (int j = 0; j < sides; j++)
            {
                int q = Of(m).v.Count;
                Quad(m, Ring(k, j), Ring(k + 1, j), Ring(k + 1, j + 1), Ring(k, j + 1));
                if (sides >= 8)
                {
                    var p = Of(m); float sg = Vector3.Dot(p.n[q], N(j) + N(j + 1)) < 0f ? -1f : 1f;
                    p.n[q] = N(j) * sg; p.n[q + 1] = N(j) * sg; p.n[q + 2] = N(j + 1) * sg; p.n[q + 3] = N(j + 1) * sg;
                }
            }
    }

    /// <summary>Writes one mesh per material under parent (x K), saved as assets; returns the group.</summary>
    /// <param name="unit">metres per nominal unit: K (default) under an unscaled parent, 1 under a parent that already
    /// carries K (apartments, hotel, arena interiors, the vitrine)</param>
    public Transform Build(Transform parent, string name, string assetFolder, bool shadows = false, float unit = 0f)
    {
        var g = new GameObject(name).transform; g.SetParent(parent, false);
        float u = unit > 0f ? unit : K;
        foreach (var m in order)
        {
            var p = parts[m]; if (p.t.Count == 0) continue;
            var v = new Vector3[p.v.Count]; for (int i = 0; i < v.Length; i++) v[i] = p.v[i] * u;
            var mesh = new Mesh { name = name + " | " + m.name, indexFormat = v.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = v; mesh.uv = p.uv.ToArray(); mesh.triangles = p.t.ToArray();
            mesh.normals = p.n.ToArray(); mesh.RecalculateTangents(); mesh.RecalculateBounds();       // smooth on rounded parts
            mesh = Save(mesh, assetFolder + "/batch_" + (saved++).ToString("D4") + ".asset");
            var o = new GameObject(name + " | " + m.name); o.transform.SetParent(g, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = shadows && m.renderQueue < 3000 ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = m.renderQueue < 3000;
            GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
        }
        parts.Clear(); order.Clear();
        return g;
    }

    static Mesh Save(Mesh mesh, string path)
    {
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old) { EditorUtility.CopySerialized(mesh, old); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    /// <summary>Invisible box collider (nominal centre/size, rotation about Y) for railings, posts, counters.</summary>
    public static void Collider(Transform parent, string name, Vector3 centre, Vector3 size, float yaw = 0f)
    {
        var o = new GameObject(name); o.transform.SetParent(parent, false);
        o.transform.localPosition = centre * K; o.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        var c = o.AddComponent<BoxCollider>(); c.size = size * K;
    }
}

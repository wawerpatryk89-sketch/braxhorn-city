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

    /// <summary>Quad with its own UVs (a picture on a screen: 0..1 or an atlas tile), same winding as Quad.</summary>
    public void QuadUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
    {
        var p = Of(m); int i = p.v.Count;
        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - a, d - a);
        n.Normalize();
        p.v.Add(a); p.v.Add(b); p.v.Add(c); p.v.Add(d); p.n.Add(n); p.n.Add(n); p.n.Add(n); p.n.Add(n);
        p.uv.Add(ua); p.uv.Add(ub); p.uv.Add(uc); p.uv.Add(ud);
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

    /// <summary>
    /// Turned solid (bottle, glass, basin, stool seat, lamp shade): profile = (radius, height) points from the bottom up about
    /// the base centre, smooth normals along the profile; a repeated point is a crease (hard edge). A profile may turn back
    /// down (a glass: outside up, rim, inside down) - the faces follow its direction. Caps close a first / last ring of r &gt; 0.
    /// </summary>
    public void Lathe(Material m, Vector3 c, Vector2[] prof, int sides, Quaternion r, bool capBottom = true, bool capTop = true)
    {
        var up = r * Vector3.up; int n = prof.Length; if (n < 2) return;
        var en = new Vector2[n - 1];
        for (int i = 0; i + 1 < n; i++) { var e = prof[i + 1] - prof[i]; en[i] = e.sqrMagnitude < 1e-12f ? Vector2.zero : new Vector2(e.y, -e.x).normalized; }
        Vector2 Smooth(int edge, int other)
        {
            if (other < 0 || other >= en.Length || en[other].sqrMagnitude < 1e-12f) return en[edge];
            var s = en[edge] + en[other]; return s.sqrMagnitude < 1e-6f ? en[edge] : s.normalized;
        }
        for (int i = 0; i + 1 < n; i++)
        {
            if (en[i].sqrMagnitude < 1e-12f || (prof[i].x < 1e-6f && prof[i + 1].x < 1e-6f)) continue;
            Vector2 na = Smooth(i, i - 1), nb = Smooth(i, i + 1);
            for (int j = 0; j < sides; j++)
            {
                float a0 = j * Mathf.PI * 2f / sides, a1 = (j + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = r * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = r * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = c + d0 * prof[i].x + up * prof[i].y, b1 = c + d1 * prof[i].x + up * prof[i].y;
                Vector3 t0 = c + d0 * prof[i + 1].x + up * prof[i + 1].y, t1 = c + d1 * prof[i + 1].x + up * prof[i + 1].y;
                var p = Of(m); int q = p.v.Count;
                if (prof[i].x < 1e-6f) Triangle(m, b0, t0, t1);
                else if (prof[i + 1].x < 1e-6f) Triangle(m, b1, b0, t0);
                else Quad(m, b1, b0, t0, t1);
                Vector3 N(Vector2 v, Vector3 d) => (d * v.x + up * v.y).normalized;
                var face = p.n[q]; var want = N(en[i], (d0 + d1).normalized);
                float s = Vector3.Dot(face, want) < 0f ? -1f : 1f;      // the faces follow the profile's direction
                if (prof[i].x < 1e-6f) { p.n[q] = s * N(na, d0); p.n[q + 1] = s * N(nb, d0); p.n[q + 2] = s * N(nb, d1); }
                else if (prof[i + 1].x < 1e-6f) { p.n[q] = s * N(na, d1); p.n[q + 1] = s * N(na, d0); p.n[q + 2] = s * N(nb, d0); }
                else { p.n[q] = s * N(na, d1); p.n[q + 1] = s * N(na, d0); p.n[q + 2] = s * N(nb, d0); p.n[q + 3] = s * N(nb, d1); }
            }
        }
        for (int j = 0; j < sides; j++)
        {
            float a0 = j * Mathf.PI * 2f / sides, a1 = (j + 1) * Mathf.PI * 2f / sides;
            Vector3 d0 = r * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = r * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
            if (capBottom && prof[0].x > 1e-6f) { var cb = c + up * prof[0].y; Triangle(m, cb, cb + d0 * prof[0].x, cb + d1 * prof[0].x); }
            if (capTop && prof[n - 1].x > 1e-6f) { var ct = c + up * prof[n - 1].y; Triangle(m, ct, ct + d1 * prof[n - 1].x, ct + d0 * prof[n - 1].x); }
        }
    }
    public void Lathe(Material m, Vector3 c, Vector2[] prof, int sides = 24) => Lathe(m, c, prof, sides, Quaternion.identity);

    /// <summary>Smooth parametric surface P(u, v), u and v 0..1 over nu x nv cells (porcelain shells: a urinal, a pan);
    /// keep(u, v) false skips a cell (an opening), flip turns the faces (the inside of a shell). Per-vertex normals from the
    /// surface's derivatives, turned to the face's side.</summary>
    public void Surface(Material m, Func<float, float, Vector3> P, int nu, int nv, bool flip = false, Func<float, float, bool> keep = null)
    {
        Vector3 Nrm(float u, float v)
        {
            const float e = 1e-3f;
            var du = P(Mathf.Min(1f, u + e), v) - P(Mathf.Max(0f, u - e), v);
            var dv = P(u, Mathf.Min(1f, v + e)) - P(u, Mathf.Max(0f, v - e));
            var n = Vector3.Cross(du, dv);
            if (n.sqrMagnitude < 1e-14f) { float uu = Mathf.Clamp(u, .02f, .98f), vv = Mathf.Clamp(v, .02f, .98f); if (uu != u || vv != v) return Nrm(uu, vv); }
            return n.normalized;
        }
        for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                float u0 = i / (float)nu, u1 = (i + 1) / (float)nu, v0 = j / (float)nv, v1 = (j + 1) / (float)nv;
                if (keep != null && !keep((u0 + u1) * .5f, (v0 + v1) * .5f)) continue;
                Vector3 a = P(u0, v0), b = P(u1, v0), c = P(u1, v1), d = P(u0, v1);
                if ((a - b).sqrMagnitude + (c - d).sqrMagnitude < 1e-12f) continue;
                var p = Of(m); int q = p.v.Count;
                if (flip) Quad(m, d, c, b, a); else Quad(m, a, b, c, d);
                if (p.v.Count == q) continue;
                var face = p.n[q];
                float[] us = flip ? new[] { u0, u1, u1, u0 } : new[] { u0, u1, u1, u0 }, vs = flip ? new[] { v1, v1, v0, v0 } : new[] { v0, v0, v1, v1 };
                for (int k = 0; k < 4; k++)
                {
                    var n = Nrm(us[k], vs[k]); if (Vector3.Dot(n, face) < 0f) n = -n; p.n[q + k] = n;
                }
            }
    }

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

    /// <summary>A fountain's moving water (owner 11.11 19:49: "woda ma lecieć ... realistycznie"): a GameObject at c
    /// (nominal, x unit) with BraxChampionsFontanna, which rebuilds the streams, spray, ripples every frame. Jets a -> b
    /// with the apex h over the higher end, width w, n streaks, plusk = rings / splash where it lands; the basins' surfaces
    /// lc / lr get a drifting ripple layer. Everything nominal metres relative to c.</summary>
    public static GameObject FountainWater(Transform parent, string name, Vector3 c, float unit, Vector3[] a, Vector3[] b, float[] h,
                                           float[] w, int[] n, bool[] plusk, Vector3[] lc, float[] lr, Material water, float sensor = 0f)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = c * unit;
        go.AddComponent<MeshFilter>(); var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = water;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        var f = go.AddComponent<BraxChampionsFontanna>();
        Vector3[] Sc(Vector3[] v) { var o = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) o[i] = v[i] * unit; return o; }
        float[] Sf(float[] v) { var o = new float[v.Length]; for (int i = 0; i < v.Length; i++) o[i] = v[i] * unit; return o; }
        f.a = Sc(a); f.b = Sc(b); f.h = Sf(h); f.w = Sf(w); f.n = n; f.plusk = plusk; f.lustroC = Sc(lc); f.lustroR = Sf(lr);
        f.g = 9.81f * unit; f.lift = .02f * unit; f.woda = water; f.tafla = water; f.czujnik = sensor * unit;
        return go;
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

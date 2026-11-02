using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// NARZĘDZIA DRÓG (build 14, wykonawca „drogi”): operacje na już zbudowanej geometrii osiedla, bez ruszania kroków,
// które ją postawiły (RenovateA, StreetNS…):
//   • UsunGrupe — wyrzuca z łączonych siatek całą grupę (stare nawierzchnie RenovateA leżały pod nowymi drogami DrogiA:
//     ten sam asfalt na tej samej wysokości = migotanie, stare zebry i przerywane linie = białe „L” przy nowych zebrach);
//   • WytnijZGrupy — wycina z siatek grupy wszystko w prostopadłościanie, trójkąty przecięte granicą są docinane
//     (Sutherland–Hodgman) — płoty działek wchodzące na chodnik, krawężnik i chodnik na zjeździe do parkingu;
//   • PrzytnijKolizje — kolizje BoxCollider w tym samym prostopadłościanie: całe w środku usuwane, przecięte skracane;
//   • Siatka/ObiektSiatki — mała siatka z własnym rendererem (szkła sygnalizatorów: materiał przełączany w grze).
public static partial class BraxClaudeOsiedle
{
    const string GrupaFarbyA = "Osiedle A / oznakowanie poziome", GrupaFarbyB = "Osiedle B / oznakowanie poziome";
    static int siatkiDrog;

    // Farba drogowa w osobnej grupie bez rzucania cieni (pasy 2 cm nad asfaltem nie kładą „obwódki” cienia).
    static void Farba(float x0, float x1, float z0, float z1, float y = YFarba, Material m = null)
    {
        if (farbaOdroczona && m == null && Mathf.Abs(y - YFarba) < 1e-4f) { farbaOdroczonaLista.Add((Mathf.Min(x0, x1), Mathf.Max(x0, x1), Mathf.Min(z0, z1), Mathf.Max(z0, z1))); return; }   // E3a: w wycięciu (na końcu DrogiA)
        var g = group; group = GrupaFarbyA;
        Top(m ?? Paint, Mathf.Min(x0, x1), Mathf.Max(x0, x1), Mathf.Min(z0, z1), Mathf.Max(z0, z1), y);
        group = g;
    }
    // Płaski wielokąt wypukły farbą (trójkąty „zębów”, strzałki, szrafura) — wierzchołki w kolejności obwodu, y = YFarba.
    static void FarbaWielokat(Vector3[] p, Material m = null)
    {
        var g = group; group = GrupaFarbyA;
        for (int k = 1; k + 1 < p.Length; k++) Tri3(m ?? Paint, p[0], p[k], p[k + 1], Vector3.up);
        group = g;
    }
    // Pojedynczy trójkąt do łączonej siatki (UV planarne w metrach jak w Quad).
    static void Tri3(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
    {
        var bt = B(m); float s = tile.TryGetValue(m, out var tl) ? tl : 1f;
        Planar(n, out var u, out var w);
        float sign = Vector3.Dot(Vector3.Cross(n, u), w) >= 0 ? 1f : -1f;
        int k = bt.v.Count;
        foreach (var p in new[] { a, b, c })
        {
            bt.v.Add(p); bt.n.Add(n); bt.t.Add(new Vector4(u.x, u.y, u.z, sign));
            bt.uv.Add(new Vector2(Vector3.Dot(p, u) / s, Vector3.Dot(p, w) / s));
        }
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), n) >= 0) bt.i.AddRange(new[] { k, k + 1, k + 2 });
        else bt.i.AddRange(new[] { k, k + 2, k + 1 });
    }
    // Ściana obu stron (np. daszek sygnalizatora widoczny od środka i z zewnątrz).
    static void QuadObustronny(Material m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
    {
        Quad(m, p0, p1, p2, p3, n); Quad(m, p0, p1, p2, p3, -n);
    }

    // ---------------- grupy łączonych siatek ----------------
    static int UsunGrupe(string grupa)
    {
        int quads = 0;
        foreach (var key in batches.Keys.Where(k => k.Item1 == grupa).ToList()) { quads += batches[key].i.Count / 6; batches.Remove(key); }
        return quads;
    }
    // Przenosi farbę z grupy do grupy bez cieni i podnosi ją o dy (zasada: oznakowanie ≥ 2 cm nad nawierzchnią).
    static int PrzeniesFarbe(string zGrupy, string doGrupy, float dy)
    {
        if (!batches.TryGetValue((zGrupy, Paint), out var b)) return 0;
        for (int k = 0; k < b.v.Count; k++) b.v[k] += Vector3.up * dy;
        batches.Remove((zGrupy, Paint));
        if (batches.TryGetValue((doGrupy, Paint), out var d))
        {
            int off = d.v.Count;
            d.v.AddRange(b.v); d.n.AddRange(b.n); d.t.AddRange(b.t); d.uv.AddRange(b.uv); d.i.AddRange(b.i.Select(q => q + off));
        }
        else batches[(doGrupy, Paint)] = b;
        bezCieni.Add(doGrupy);
        return b.i.Count / 3;
    }

    struct WierzD { public Vector3 p, n; public Vector4 t; public Vector2 uv; }
    static WierzD LerpW(WierzD a, WierzD b, float t)
        => new WierzD { p = Vector3.Lerp(a.p, b.p, t), n = Vector3.Lerp(a.n, b.n, t).normalized, t = Vector4.Lerp(a.t, b.t, t), uv = Vector2.Lerp(a.uv, b.uv, t) };
    // Sutherland–Hodgman: część wielokąta po stronie p[axis] >= val (wiekszeOd) albo <= val.
    static List<WierzD> Klip(List<WierzD> poly, int axis, float val, bool wiekszeOd)
    {
        var res = new List<WierzD>(poly.Count + 2);
        for (int k = 0; k < poly.Count; k++)
        {
            var a = poly[k]; var b = poly[(k + 1) % poly.Count];
            float da = (a.p[axis] - val) * (wiekszeOd ? 1f : -1f), db = (b.p[axis] - val) * (wiekszeOd ? 1f : -1f);
            bool ina = da >= 0f, inb = db >= 0f;
            if (ina) res.Add(a);
            if (ina != inb) res.Add(LerpW(a, b, da / (da - db)));
        }
        return res;
    }
    // Wycina z siatek grup (filtr po grupie i materiale) wszystko w prostopadłościanie v; zwraca liczbę usuniętych/przyciętych trójkątów.
    static int WytnijZGrupy(Func<string, Material, bool> ktore, Bounds v)
    {
        int ruszone = 0;
        foreach (var key in batches.Keys.Where(k => ktore(k.Item1, k.Item2)).ToList())
        {
            var b = batches[key];
            var nv = new List<Vector3>(b.v.Count); var nn = new List<Vector3>(b.v.Count); var nt = new List<Vector4>(b.v.Count); var nu = new List<Vector2>(b.v.Count); var ni = new List<int>(b.i.Count);
            int Dodaj(WierzD w) { nv.Add(w.p); nn.Add(w.n); nt.Add(w.t); nu.Add(w.uv); return nv.Count - 1; }
            WierzD W(int q) => new WierzD { p = b.v[q], n = b.n[q], t = b.t[q], uv = b.uv[q] };
            for (int k = 0; k + 2 < b.i.Count; k += 3)
            {
                var tri = new List<WierzD> { W(b.i[k]), W(b.i[k + 1]), W(b.i[k + 2]) };
                Vector3 lo = Vector3.Min(Vector3.Min(tri[0].p, tri[1].p), tri[2].p), hi = Vector3.Max(Vector3.Max(tri[0].p, tri[1].p), tri[2].p);
                bool styka = hi.x > v.min.x + 1e-4f && lo.x < v.max.x - 1e-4f && hi.y > v.min.y + 1e-4f && lo.y < v.max.y - 1e-4f && hi.z > v.min.z + 1e-4f && lo.z < v.max.z - 1e-4f;
                if (!styka) { ni.Add(Dodaj(tri[0])); ni.Add(Dodaj(tri[1])); ni.Add(Dodaj(tri[2])); continue; }
                ruszone++;
                var poly = tri;
                for (int axis = 0; axis < 3 && poly.Count >= 3; axis++)
                {
                    var poza = Klip(poly, axis, v.max[axis], true);
                    if (poza.Count >= 3) { int i0 = Dodaj(poza[0]); for (int q = 1; q + 1 < poza.Count; q++) { ni.Add(i0); ni.Add(Dodaj(poza[q])); ni.Add(Dodaj(poza[q + 1])); } }
                    poly = Klip(poly, axis, v.max[axis], false);
                    if (poly.Count < 3) break;
                    poza = Klip(poly, axis, v.min[axis], false);
                    if (poza.Count >= 3) { int i0 = Dodaj(poza[0]); for (int q = 1; q + 1 < poza.Count; q++) { ni.Add(i0); ni.Add(Dodaj(poza[q])); ni.Add(Dodaj(poza[q + 1])); } }
                    poly = Klip(poly, axis, v.min[axis], true);
                }
                // reszta (poly) leży w prostopadłościanie — wycięta
            }
            b.v.Clear(); b.v.AddRange(nv); b.n.Clear(); b.n.AddRange(nn); b.t.Clear(); b.t.AddRange(nt); b.uv.Clear(); b.uv.AddRange(nu); b.i.Clear(); b.i.AddRange(ni);
        }
        return ruszone;
    }
    // Kolizje (BoxCollider bez obrotu) w prostopadłościanie: całe w środku — usunięte; przecięte granicą na osi `axis` —
    // skrócone do tej granicy (zostaje część po stronie `zostawPonizej`).
    static int PrzytnijKolizje(Transform korzen, Bounds v, int axis, bool zostawPonizej)
    {
        if (!korzen) return 0;
        int n = 0;
        foreach (var c in korzen.GetComponentsInChildren<BoxCollider>(true).ToList())
        {
            if (!c || !c.enabled) continue;
            var t = c.transform; if (Quaternion.Angle(t.rotation, Quaternion.identity) > .5f) continue;
            var b = c.bounds; if (!b.Intersects(v)) continue;
            bool wSrodku = v.Contains(b.min + Vector3.one * .01f) && v.Contains(b.max - Vector3.one * .01f);
            if (wSrodku) { if (c.gameObject.name == "kolizja") UnityEngine.Object.DestroyImmediate(c.gameObject); else c.enabled = false; n++; continue; }
            Vector3 lo = b.min, hi = b.max;
            if (zostawPonizej && hi[axis] > v.min[axis] && lo[axis] < v.min[axis]) hi[axis] = v.min[axis];
            else if (!zostawPonizej && lo[axis] < v.max[axis] && hi[axis] > v.max[axis]) lo[axis] = v.max[axis];
            else continue;
            var s = t.lossyScale; if (Mathf.Abs(s.x) < 1e-4f || Mathf.Abs(s.y) < 1e-4f || Mathf.Abs(s.z) < 1e-4f) continue;
            var nb = new Bounds(); nb.SetMinMax(lo, hi);
            c.center = t.InverseTransformPoint(nb.center); c.size = new Vector3(nb.size.x / Mathf.Abs(s.x), nb.size.y / Mathf.Abs(s.y), nb.size.z / Mathf.Abs(s.z));
            n++;
        }
        return n;
    }

    // ---------------- mała siatka z własnym rendererem ----------------
    sealed class Siatka
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<int> i = new List<int>();
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 nn)
        {
            int k = v.Count; v.Add(a); v.Add(b); v.Add(c); n.Add(nn); n.Add(nn); n.Add(nn);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), nn) >= 0) { i.Add(k); i.Add(k + 1); i.Add(k + 2); } else { i.Add(k); i.Add(k + 2); i.Add(k + 1); }
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nn) { Tri(a, b, c, nn); Tri(a, c, d, nn); }
        // tarcza (koło) w płaszczyźnie (r, u), normalna nn
        public void Dysk(Vector3 c, Vector3 r, Vector3 u, Vector3 nn, float rad, int seg = 20)
        {
            for (int k = 0; k < seg; k++)
            {
                float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                Tri(c, c + (r * Mathf.Cos(a0) + u * Mathf.Sin(a0)) * rad, c + (r * Mathf.Cos(a1) + u * Mathf.Sin(a1)) * rad, nn);
            }
        }
        // prostokąt w lokalnych (x, y) tarczy
        public void Prost(Vector3 o, Vector3 r, Vector3 u, Vector3 nn, float x0, float x1, float y0, float y1)
            => Quad(o + r * x0 + u * y0, o + r * x1 + u * y0, o + r * x1 + u * y1, o + r * x0 + u * y1, nn);
        // odcinek o grubości w (kończyny ludzika)
        public void Kreska(Vector3 o, Vector3 r, Vector3 u, Vector3 nn, Vector2 a, Vector2 b, float w)
        {
            var d = (b - a).normalized; var p = new Vector2(-d.y, d.x) * (w * .5f);
            Vector3 P(Vector2 q) => o + r * q.x + u * q.y;
            Quad(P(a - p), P(b - p), P(b + p), P(a + p), nn);
        }
    }
    static MeshRenderer ObiektSiatki(string name, Transform parent, Siatka s, Material m)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var mesh = new Mesh { name = "Drogi " + name };
        mesh.SetVertices(s.v); mesh.SetNormals(s.n); mesh.SetTriangles(s.i, 0); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, Out + "/drogi_siatka_" + (siatkiDrog++) + ".asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return r;
    }
    // Materiał szkła sygnalizatora: zgaszony (ciemne szkło) albo świecący emisją (tylko w komponencie, nie na rendererze
    // przy zapisie sceny — krok „bez świecenia” zgasiłby go; w grze BraxSwiatloDrogowe podmienia materiał).
    static Material Szklo(string key, Color c, bool swieci)
    {
        string nazwa = "sygnalizator " + key + (swieci ? " (świeci)" : " (zgaszony)");
        if (mats.TryGetValue(nazwa, out var have)) return have;
        var m = new Material(Shader.Find("Standard")) { name = "Osiedle: " + nazwa, enableInstancing = true };
        if (swieci)
        {
            m.color = Color.Lerp(c, Color.white, .25f); m.SetFloat("_Glossiness", .85f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        else { m.color = new Color(c.r * .16f + .02f, c.g * .16f + .02f, c.b * .16f + .02f); m.SetFloat("_Glossiness", .9f); m.SetFloat("_Metallic", .1f); }
        AssetDatabase.CreateAsset(m, Out + "/" + Safe(nazwa) + ".mat");
        mats[nazwa] = m; tile[m] = 1f; return m;
    }
}

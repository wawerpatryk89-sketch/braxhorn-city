using System;
using System.Collections.Generic;
using UnityEngine;

// TUNEL — WNĘTRZE WOLNE (właściciel 3.11 14:57/14:59: w otworze tunelu nadal ściana z trawy; „ma być atrapa 3D, że tam jest
// tunel w środku… sztuczka jakaś”). Teren za płotem i wzgórze nad tunelem powstają PO tunelu (KraniecMapy: Teren, DalszyLas),
// a ich duże trójkąty przecinają wnętrze. Po scaleniu wszystkich siatek: każdy trójkąt spoza tunelu, który zahacza o obrys
// wnętrza (jezdnia + chodniki × wysokość sklepienia, od portalu w głąb), jest dzielony na mniejsze (do ~0.4 m) i części
// leżące w obrysie są usuwane — reszta wzgórza zostaje bez dziur. Kolizje siatek (MeshCollider) dostają tę samą siatkę.
public static partial class BraxClaudeOsiedle
{
    static void TunelWolny()
    {
        var box = new Bounds(new Vector3(0f, TunSzczyt * .5f + .05f, TunZ0 + .2f + (TunDl - .4f) * .5f), new Vector3(2f * TunHalf - .1f, TunSzczyt - .1f, TunDl - .4f));
        int usun = 0, obiekty = 0;
        foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!mf || !mf.sharedMesh) continue;
            var r = mf.GetComponent<Renderer>(); if (!r || !r.bounds.Intersects(box)) continue;
            bool swoj = false;
            for (var t = mf.transform; t; t = t.parent) if (t.name.IndexOf("tunel", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("szlaban", StringComparison.OrdinalIgnoreCase) >= 0) { swoj = true; break; }
            if (swoj) continue;
            var m = mf.sharedMesh; if (!m.isReadable) continue;
            var w = mf.transform.localToWorldMatrix;
            var v = new List<Vector3>(m.vertices); var n = new List<Vector3>(m.normals); var uv = new List<Vector2>(m.uv); var c = new List<Color>(m.colors);
            bool mN = n.Count == v.Count, mU = uv.Count == v.Count, mC = c.Count == v.Count;
            var wv = new List<Vector3>(v.Count); foreach (var p in v) wv.Add(w.MultiplyPoint3x4(p));
            int przed = usun; var nowe = new List<int>[m.subMeshCount];
            int Srodek(int a, int b)
            {
                v.Add((v[a] + v[b]) * .5f); wv.Add((wv[a] + wv[b]) * .5f);
                if (mN) n.Add(((n[a] + n[b]) * .5f).normalized); if (mU) uv.Add((uv[a] + uv[b]) * .5f); if (mC) c.Add((c[a] + c[b]) * .5f);
                return v.Count - 1;
            }
            void Tri(int a, int b, int d, List<int> wyn, int glebia)
            {
                Vector3 A = wv[a], B = wv[b], D = wv[d];
                var tb = new Bounds(A, Vector3.zero); tb.Encapsulate(B); tb.Encapsulate(D);
                if (!tb.Intersects(box)) { wyn.Add(a); wyn.Add(b); wyn.Add(d); return; }
                if (box.Contains((A + B + D) / 3f) && (glebia >= 8 || (box.Contains(A) && box.Contains(B) && box.Contains(D)))) { usun++; return; }
                float dl = Mathf.Max((A - B).sqrMagnitude, Mathf.Max((B - D).sqrMagnitude, (D - A).sqrMagnitude));
                if (glebia >= 8 || dl < .16f) { if (box.Contains((A + B + D) / 3f)) usun++; else { wyn.Add(a); wyn.Add(b); wyn.Add(d); } return; }
                int ab = Srodek(a, b), bd = Srodek(b, d), da = Srodek(d, a);
                Tri(a, ab, da, wyn, glebia + 1); Tri(ab, b, bd, wyn, glebia + 1); Tri(da, bd, d, wyn, glebia + 1); Tri(ab, bd, da, wyn, glebia + 1);
            }
            for (int s = 0; s < m.subMeshCount; s++)
            {
                var t = m.GetTriangles(s); var wyn = new List<int>(t.Length);
                for (int i = 0; i + 2 < t.Length; i += 3) Tri(t[i], t[i + 1], t[i + 2], wyn, 0);
                nowe[s] = wyn;
            }
            if (usun == przed) continue;
            var k = new Mesh { name = m.name + " (bez wnętrza tunelu)", indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : m.indexFormat };
            k.SetVertices(v); if (mN) k.SetNormals(n); if (mU) k.SetUVs(0, uv); if (mC) k.SetColors(c);
            k.subMeshCount = nowe.Length; for (int s = 0; s < nowe.Length; s++) k.SetTriangles(nowe[s], s);
            if (!mN) k.RecalculateNormals(); k.RecalculateBounds();
            var mc = mf.GetComponent<MeshCollider>(); if (mc && mc.sharedMesh == m) mc.sharedMesh = k;
            mf.sharedMesh = k; obiekty++;
            Debug.Log("BRAXHORN_TUNEL wnętrze wolne: " + mf.name + " −" + (usun - przed) + " trójkątów");
        }
        Debug.Log("BRAXHORN_TUNEL wnętrze wolne: wycięto " + usun + " trójkątów z " + obiekty + " obiektów (teren/wzgórze/trawa w obrysie tunelu " + box.min.ToString("F1") + "…" + box.max.ToString("F1") + ")");
    }
}

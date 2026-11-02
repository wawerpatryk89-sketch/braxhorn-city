using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// E29b (build 45 runda 2, właściciel: „drzwi jak do garażu”): bramy Codexa 4 × 3 m na WEJŚCIACH budynków publicznych
// (id …-front: kasyno, dyskoteka, komisariat, szpital, sala bokserska, arena) zwężone do otworu 1.8 × 2.2 m × K z dwoma
// skrzydłami: zawiasy przesunięte do krawędzi nowego otworu, skrzydła przeskalowane (szer., wys.; kolizje skrzydeł skalują się
// z nimi), reszta otworu ZAMUROWANA WSTAWKĄ W OTWORZE (nie nakładka: bryła między licami ściany, grubość i materiał z siatki
// ściany obok otworu), nowa ościeżnica (stal grafit, 8 cm, wystaje 3 cm z obu lic), wstawki z kolizją. Punkt „OTWÓRZ DRZWI”
// bez zmian (obiekt drzwi stoi, gdzie stał). BEZPIECZNIK: po zmianie skrzydła na próbę otwarte o 90° i test kapsułą gracza
// w środku nowego otworu — zablokowane albo skrzydło poza otworem = wszystko cofnięte (log DO POPRAWKI), wejście zostaje
// jak było. Log BRAXHORN_DRZWI typ=wejście szer=… wys=… budynek=… (przed → po), podsumowanie BRAXHORN_WEJSCIA_ZWEZENIE.
public static partial class BraxClaudeOsiedle
{
    static int klocekN;
    static Mesh KlocekSiatka(Vector3 s)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>(); var trTyl = new List<int>();
        var h = s * .5f;
        void F(Vector3 nn, Vector3 a, Vector3 b, float sa, float sb)
        {
            var tr0 = nn.z < -.5f ? trTyl : tr;   // podsiatka 1 = lico od strony −przod (materiał drugiego lica ściany)
            int k = v.Count; var c = Vector3.Scale(nn, h);
            var pa = Vector3.Scale(a, h); var pb = Vector3.Scale(b, h);
            v.Add(c - pa - pb); v.Add(c + pa - pb); v.Add(c + pa + pb); v.Add(c - pa + pb);
            for (int i = 0; i < 4; i++) n.Add(nn);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(sa, 0)); uv.Add(new Vector2(sa, sb)); uv.Add(new Vector2(0, sb));
            bool zgodnie = Vector3.Dot(Vector3.Cross(v[k + 1] - v[k], v[k + 2] - v[k]), nn) > 0;   // przód trójkąta: normalna = cross(v1 − v0, v2 − v0)
            if (zgodnie) tr0.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 }); else tr0.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
        }
        F(Vector3.forward, Vector3.right, Vector3.up, s.x, s.y); F(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
        F(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y); F(Vector3.left, Vector3.forward, Vector3.up, s.z, s.y);
        F(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z); F(Vector3.down, Vector3.right, Vector3.forward, s.x, s.z);
        var m = new Mesh { name = "wstawka " + s.x.ToString("F2") + "×" + s.y.ToString("F2") + "×" + s.z.ToString("F2") };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.subMeshCount = 2; m.SetTriangles(tr, 0); m.SetTriangles(trTyl, 1); m.RecalculateBounds(); m.RecalculateTangents();
        AssetDatabase.CreateAsset(m, Out + "/wstawka_" + (++klocekN) + ".asset");   // siatka zapisana jak reszta osiedla
        return m;
    }

    // Lica ściany w punkcie p wzdłuż „przod” (±zasieg): wszystkie przecięcia z trójkątami siatek (obrys ≥ 1.6 m) — min/max t
    // i materiał trafionego trójkąta (podsiatka).
    static bool LicaSciany(Vector3 p, Vector3 przod, float zasieg, List<MeshFilter> siatki, Dictionary<Mesh, Vector3[]> pamiec, out float tMin, out float tMax, out Material matMin, out Material matMax)
    {
        tMin = float.MaxValue; tMax = float.MinValue; matMin = null; matMax = null;
        Vector3 a = p - przod * zasieg, b = p + przod * zasieg;
        foreach (var mf in siatki)
        {
            if (!mf || !mf.sharedMesh) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled) continue;
            var rb = r.bounds; if (rb.size.y < 1.6f) continue;
            var e = rb; e.Expand(zasieg * 2f); if (!e.Contains(p)) continue;
            var m = mf.sharedMesh; if (!m.isReadable) continue;
            if (!pamiec.TryGetValue(m, out var v)) pamiec[m] = v = m.vertices;
            var M = mf.transform.localToWorldMatrix;
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                var t = m.GetTriangles(sub);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 v0 = M.MultiplyPoint3x4(v[t[i]]), v1 = M.MultiplyPoint3x4(v[t[i + 1]]), v2 = M.MultiplyPoint3x4(v[t[i + 2]]);
                    if (Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y)) < p.y - .01f || Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y)) > p.y + .01f) continue;
                    if (!OdcinekTrojkat(a, b, v0, v1, v2)) continue;
                    var nn = Vector3.Cross(v1 - v0, v2 - v0); float den = Vector3.Dot(nn, b - a); if (Mathf.Abs(den) < 1e-9f) continue;
                    float tt = Vector3.Dot(nn, v0 - a) / den * zasieg * 2f - zasieg;   // odległość od p wzdłuż przod
                    var ms = r.sharedMaterials; var mm = ms.Length > 0 ? ms[Mathf.Min(sub, ms.Length - 1)] : null;
                    if (tt < tMin) { tMin = tt; matMin = mm; }
                    if (tt > tMax) { tMax = tt; matMax = mm; }
                }
            }
        }
        return tMin < float.MaxValue;
    }

    static void ZwezenieBram()
    {
        Physics.SyncTransforms();
        float Wn = 1.8f * K, Hn = 2.2f * K, ram = .08f;
        var siatki = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(mf => mf && !Ruchome(mf.transform)).ToList();
        var pamiec = new Dictionary<Mesh, Vector3[]>();
        var stal1 = Plain("ościeżnica stal grafit (wejścia)", new Color(.12f, .125f, .13f), .45f, .6f); var stal = new[] { stal1, stal1 };
        var zapas = Plain("wstawka tynk (brak materiału lica)", new Color(.8f, .79f, .76f), .15f);
        const int maska = ~((1 << 8) | (1 << 29) | (1 << 2));
        int zwezone = 0, pominiete = 0, cofniete = 0;
        var root = new GameObject("E29b: zwężone wejścia (wstawki, ościeżnice)").transform; root.SetParent(colliders ? colliders.parent : null, false);
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!d || d.leaves == null || Klasa(d.id) != KlasaDrzwi.Wejscie) continue;
            string bud = d.id.Split('-')[0];
            try
            {
                Bounds ob = default; bool jest = false; Vector3 prawo = d.transform.right, przod = d.transform.forward;
                foreach (var l in d.leaves)
                {
                    var c = l ? l.GetComponent<BoxCollider>() : null; if (!c) continue;
                    prawo = l.right; przod = l.forward;
                    for (int i = 0; i < 8; i++)
                    {
                        var p = l.TransformPoint(c.center + Vector3.Scale(c.size * .5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                        if (!jest) { ob = new Bounds(p, Vector3.zero); jest = true; } else ob.Encapsulate(p);
                    }
                }
                if (!jest) { pominiete++; continue; }
                prawo.y = 0; prawo = prawo.sqrMagnitude < 1e-6f ? Vector3.right : prawo.normalized; przod.y = 0; przod = przod.sqrMagnitude < 1e-6f ? Vector3.Cross(prawo, Vector3.up) : przod.normalized;
                float W = 2f * Mathf.Abs(Vector3.Dot(ob.extents, new Vector3(Mathf.Abs(prawo.x), 0, Mathf.Abs(prawo.z)))), H = ob.size.y, y0 = ob.min.y;
                string przed = W.ToString("F2") + "×" + H.ToString("F2");
                if (d.leaves.Length != 2) { pominiete++; Debug.Log("BRAXHORN_DRZWI typ=wejście budynek=" + bud + " id=" + d.id + " " + przed + " m — " + d.leaves.Length + " skrzydło, bez zmian"); continue; }
                if (W < Wn + .15f) { pominiete++; Debug.Log("BRAXHORN_DRZWI typ=wejście budynek=" + bud + " id=" + d.id + " " + przed + " m — już wąskie, bez zmian"); continue; }
                // grubość i materiał ściany obok otworu
                var sp = new Vector3(ob.center.x, y0 + 1.2f, ob.center.z) + prawo * (W * .5f + .3f);
                float T = .3f, off = 0f; Material mPlus = null, mMinus = null;
                if (LicaSciany(sp, przod, 1.0f, siatki, pamiec, out float t0, out float t1, out mMinus, out mPlus) && t1 - t0 > .05f && t1 - t0 < 1.2f) { T = t1 - t0; off = Vector3.Dot(sp + przod * ((t0 + t1) * .5f) - ob.center, przod); }
                else if (LicaSciany(sp - prawo * (W + .6f), przod, 1.0f, siatki, pamiec, out t0, out t1, out mMinus, out mPlus) && t1 - t0 > .05f && t1 - t0 < 1.2f) { T = t1 - t0; off = Vector3.Dot(sp - prawo * (W + .6f) + przod * ((t0 + t1) * .5f) - ob.center, przod); }
                if (!mPlus) mPlus = mMinus ? mMinus : zapas; if (!mMinus) mMinus = mPlus;
                var mat = new[] { mPlus, mMinus };
                var pc = new Vector3(ob.center.x, 0f, ob.center.z) + przod * off;   // środek otworu w płaszczyźnie środka ściany (y = 0)
                var q = Quaternion.LookRotation(przod, Vector3.up);
                // stan do cofnięcia
                var stan = d.leaves.Select(l => (l.position, l.localScale, l.localRotation)).ToArray();
                var nowe = new List<GameObject>();
                GameObject Klocek(string nazwa, Material[] m, Vector3 srodek, Vector3 rozm)
                {
                    var g = new GameObject(nazwa + " — " + d.id); g.transform.SetParent(root, false); g.transform.SetPositionAndRotation(srodek, q);
                    g.AddComponent<MeshFilter>().sharedMesh = KlocekSiatka(rozm);
                    var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = m;
                    g.AddComponent<BoxCollider>().size = rozm; nowe.Add(g); return g;
                }
                // skrzydła: zawias do krawędzi nowego otworu, szerokość i wysokość do nowego otworu
                foreach (var l in d.leaves)
                {
                    var doS = ob.center - l.position; doS.y = 0f; float s = Mathf.Sign(Vector3.Dot(doS, prawo));
                    l.position += prawo * s * (W - Wn) * .5f;
                    l.localScale = Vector3.Scale(l.localScale, new Vector3(Wn / W, Hn / H, 1f));
                }
                // wstawki (zamurowanie) i ościeżnica
                float boki = (W - Wn) * .5f - ram;
                foreach (int sgn in new[] { -1, 1 })
                {
                    if (boki > .02f) Klocek("wstawka w otworze (bok)", mat, pc + prawo * sgn * (Wn * .5f + ram + boki * .5f) + Vector3.up * (y0 - .05f + (H + .1f) * .5f), new Vector3(boki, H + .1f, T));   // 5 cm w posadzkę i w nadproże — bez szczeliny
                    Klocek("ościeżnica (słupek)", stal, pc + prawo * sgn * (Wn * .5f + ram * .5f) + Vector3.up * (y0 - .02f + (Hn + ram + .02f) * .5f), new Vector3(ram, Hn + ram + .02f, T + .06f));
                }
                Klocek("ościeżnica (nadproże)", stal, pc + Vector3.up * (y0 + Hn + ram * .5f), new Vector3(Wn + 2f * ram, ram, T + .06f));
                float gora = H - Hn - ram;
                if (gora > .02f) Klocek("wstawka w otworze (nad drzwiami)", mat, pc + Vector3.up * (y0 + Hn + ram + (gora + .05f) * .5f), new Vector3(Wn + 2f * ram, gora + .05f, T));
                // BEZPIECZNIK: skrzydła na próbę otwarte, kapsuła gracza w środku nowego otworu
                Physics.SyncTransforms();
                bool wOtworze = d.leaves.All(l => { var c = l.GetComponent<BoxCollider>(); if (!c) return true; var w = l.TransformPoint(c.center); return Mathf.Abs(Vector3.Dot(w - ob.center, prawo)) <= Wn * .5f + .05f; });
                for (int i = 0; i < d.leaves.Length; i++) d.leaves[i].localRotation = stan[i].Item3 * Quaternion.Euler(0, (d.sides != null && i < d.sides.Length ? d.sides[i] : 1f) * 90f, 0);
                Physics.SyncTransforms();
                var cc = new Vector3(ob.center.x, 0, ob.center.z);
                bool wolne = !Physics.CheckCapsule(cc + Vector3.up * (y0 + .65f), cc + Vector3.up * (y0 + 1.75f), .3f, maska, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < d.leaves.Length; i++) d.leaves[i].localRotation = stan[i].Item3;
                if (!wOtworze || !wolne)
                {
                    foreach (var g in nowe) UnityEngine.Object.DestroyImmediate(g);
                    for (int i = 0; i < d.leaves.Length; i++) { d.leaves[i].position = stan[i].Item1; d.leaves[i].localScale = stan[i].Item2; }
                    Physics.SyncTransforms(); cofniete++;
                    Debug.LogError("BRAXHORN_DRZWI DO POPRAWKI typ=wejście budynek=" + bud + " id=" + d.id + " zwężenie COFNIĘTE (" + (!wOtworze ? "skrzydło poza nowym otworem" : "przejście zablokowane przy otwartych skrzydłach") + ") — brama " + przed + " m bez zmian");
                    continue;
                }
                zwezone++;
                Debug.Log("BRAXHORN_DRZWI typ=wejście szer=" + Wn.ToString("F2") + " wys=" + Hn.ToString("F2") + " budynek=" + bud + " id=" + d.id + " (przed " + przed + " m; skrzydła 2 × " + (Wn * .5f).ToString("F2")
                          + " m; wstawki: boki " + Mathf.Max(0f, boki).ToString("F2") + " m, góra " + Mathf.Max(0f, gora).ToString("F2") + " m, grubość " + T.ToString("F2") + " m, materiał " + (mPlus ? mPlus.name : "—") + " / " + (mMinus ? mMinus.name : "—") + ")");
            }
            catch (Exception e) { Debug.LogError("BRAXHORN_DRZWI DO POPRAWKI zwężenie " + d.id + ": " + e.Message); }
        }
        Debug.Log("BRAXHORN_WEJSCIA_ZWEZENIE zwezone=" + zwezone + " pominiete=" + pominiete + " cofniete=" + cofniete + " (cel otworu " + Wn.ToString("F2") + " × " + Hn.ToString("F2") + " m = 1.8 × 2.2 m × K; tylne -rear bez zmian w 45)");
        rep.notes.Add("E29b: zwężone wejścia " + zwezone + ", pominięte " + pominiete + ", cofnięte " + cofniete);
    }
}

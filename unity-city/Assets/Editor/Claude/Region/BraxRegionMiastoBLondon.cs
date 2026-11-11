using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// LONDON — OKRĄGŁY WIEŻOWIEC PORZĄDNIE (właściciel 11.11 12:48: „Ten budynek jest brzydki”, 12:49: „Zrób ten budynek
// porządnie”). Model właściciela (paczka miasto_wiezowce, 215 m, zaokrąglona bryła zwężająca się ku górze) — BRYŁA BEZ ZMIAN.
// Dlaczego był brzydki (APK 2bfadac): elewacja to dwie warstwy — półprzezroczysta skóra (LONDON_wieza_3: szkło alfa .7/.8,
// tekstura 1024 px na 215 m) nad ciemną elewacją z oknami (metal 1.0, tekstura okien rozciągnięta po łuku). Dwie siatki okien
// przez siebie = faliste pasy (mora), wszystko ciemne, u góry białe smugi szwów tekstury.
// Teraz skóra jest PRAWDZIWĄ ścianą kurtynową (Braxhorn/SzkloFasady): nieprzezroczysta, siatka słupków i stropów liczona
// po obwodzie bryły (UV2 = metry po łuku przekroju, przekroje co 3 m) — na łuku słupki równe i pionowe, stropy poziome, za
// szybą pokoje biurowe, część z zapalonym światłem; odbija to samo niebo co reszta miasta (BraxNieboJedno). Wewnętrzna
// warstwa z oknami zostaje w modelu (zasłonięta). Cokół, wejście i szkło holu bez zmian.
public static partial class BraxRegionMiastoB
{
    /// <summary>Wariant szkła LONDON: 'A' srebrno-błękitne z aluminium (wybrany przez właściciela 11.11 14:07), 'B' grafitowe.</summary>
    public const char LondonWariant = 'A';
    const float LondonModul = 1.8f, LondonPrzekroj = 3f;

    static void NdLondonFasada(Transform g)
    {
        var bud = g ? g.Cast<Transform>().FirstOrDefault(t => t && t.name == "Budynek LONDON") : null;
        if (!bud) { NdLog("LONDON: brak budynku w scenie — elewacja bez zmian"); return; }
        var skora = bud.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "LONDON_wieza_3");
        var mf = skora ? skora.GetComponent<MeshFilter>() : null;
        if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) { NdLog("LONDON: brak skóry LONDON_wieza_3 (albo siatka nieczytelna) — elewacja bez zmian, DO POPRAWKI"); return; }
        var src = mf.sharedMesh; var tr = skora.transform; var mats = skora.sharedMaterials;
        var V = src.vertices; var W = V.Select(v => tr.TransformPoint(v)).ToArray();
        // podsiatki skóry: materiał przezroczysty z teksturą, wysokość > 10 m (bez szkła holu na dole)
        var cel = new List<int>(); float yBaza = bud.position.y, yCokol = yBaza;
        for (int s = 0; s < src.subMeshCount && s < mats.Length; s++)
        {
            var m = mats[s]; var tri = src.GetTriangles(s); if (!m || tri.Length == 0) continue;
            float y0 = tri.Min(i => W[i].y), y1 = tri.Max(i => W[i].y);
            bool przezr = m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f;
            bool tekst = m.HasProperty("_MainTex") && m.GetTexture("_MainTex");
            if (przezr && tekst && y1 - y0 > 10f) cel.Add(s);
            else if (!przezr) yCokol = Mathf.Max(yCokol, Mathf.Min(y1, yBaza + 20f));   // cokół (ciemny metal) — od niego liczą się stropy
        }
        if (cel.Count == 0) { NdLog("LONDON: nie znaleziono skóry (przezroczyste podsiatki z teksturą) — elewacja bez zmian, DO POPRAWKI"); return; }

        // ---- przekroje poziome skóry co 3 m: środek, kąt → metry po obwodzie ----
        var tris = new List<int>(); foreach (var s in cel) tris.AddRange(src.GetTriangles(s));
        float ymin = tris.Min(i => W[i].y), ymax = tris.Max(i => W[i].y);
        var przekroje = new List<(float y, Vector2 c, float[] kat, float[] dl, float obwod)>();
        for (float h = ymin + .4f; h < ymax - .2f; h += LondonPrzekroj)
        {
            var pkt = new List<Vector2>();
            for (int t = 0; t < tris.Count; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    var a = W[tris[t + e]]; var b = W[tris[t + (e + 1) % 3]];
                    if ((a.y - h) * (b.y - h) >= 0f || Mathf.Abs(a.y - b.y) < 1e-5f) continue;
                    float k = (h - a.y) / (b.y - a.y); pkt.Add(new Vector2(Mathf.Lerp(a.x, b.x, k), Mathf.Lerp(a.z, b.z, k)));
                }
            if (pkt.Count < 8) continue;
            var c = new Vector2(pkt.Average(p => p.x), pkt.Average(p => p.y));
            var sort = pkt.Select(p => (a: Mathf.Atan2(p.y - c.y, p.x - c.x), p)).OrderBy(q => q.a).ToList();
            var kat = new float[sort.Count]; var dl = new float[sort.Count]; float L = 0;
            for (int i = 0; i < sort.Count; i++) { if (i > 0) L += Vector2.Distance(sort[i - 1].p, sort[i].p); kat[i] = sort[i].a; dl[i] = L; }
            float obwod = L + Vector2.Distance(sort[sort.Count - 1].p, sort[0].p);
            przekroje.Add((h, c, kat, dl, obwod));
        }
        // przekroje przez koronę mają podwójną powierzchnię (zewnętrzna i wewnętrzna) — obwód „zygzakiem” kilka razy za długi;
        // pominięte, korona bierze najbliższy prawidłowy przekrój pod sobą (APK 2bfadac: 111…176 m, korona 606 m)
        if (przekroje.Count > 0)
        {
            float med = przekroje.Select(p => p.obwod).OrderBy(x => x).ElementAt(przekroje.Count / 2);
            przekroje = przekroje.Where(p => p.obwod <= 1.6f * med).ToList();
        }
        if (przekroje.Count < 4) { NdLog("LONDON: za mało przekrojów skóry (" + przekroje.Count + ") — elewacja bez zmian, DO POPRAWKI"); return; }
        // stała liczba słupków na całej wysokości (bryła się zwęża — moduł węższy u góry, słupki ciągłe w pionie)
        var obwody = przekroje.Select(p => p.obwod).OrderBy(x => x).ToList();
        float obwodRef = obwody[obwody.Count / 2]; int N = Mathf.Max(8, Mathf.RoundToInt(obwodRef / LondonModul));

        float Udzial(int k, Vector3 w)
        {
            var p = przekroje[k]; float a = Mathf.Atan2(w.z - p.c.y, w.x - p.c.x);
            int n = p.kat.Length; if (a < p.kat[0] || a > p.kat[n - 1])
            {   // między ostatnim a pierwszym punktem (szew ±π)
                float a0 = p.kat[n - 1], a1 = p.kat[0] + Mathf.PI * 2f, aa = a < p.kat[0] ? a + Mathf.PI * 2f : a;
                float tt = Mathf.InverseLerp(a0, a1, aa); return Mathf.Repeat(Mathf.Lerp(p.dl[n - 1], p.obwod, tt) / p.obwod, 1f);
            }
            int lo = 0, hi = n - 1; while (hi - lo > 1) { int mid = (lo + hi) / 2; if (p.kat[mid] <= a) lo = mid; else hi = mid; }
            float t2 = Mathf.InverseLerp(p.kat[lo], p.kat[hi], a); return Mathf.Lerp(p.dl[lo], p.dl[hi], t2) / p.obwod;
        }
        float UdzialW(Vector3 w)
        {
            int k1 = przekroje.FindIndex(p => p.y >= w.y);
            if (k1 <= 0) return Udzial(k1 == 0 ? 0 : przekroje.Count - 1, w);
            int k0 = k1 - 1; float f0 = Udzial(k0, w), f1 = Udzial(k1, w);
            if (f1 - f0 > .5f) f1 -= 1f; else if (f0 - f1 > .5f) f1 += 1f;
            return Mathf.Repeat(Mathf.Lerp(f0, f1, Mathf.InverseLerp(przekroje[k0].y, przekroje[k1].y, w.y)), 1f);
        }

        // ---- nowa siatka: UV2.x = metry po obwodzie (f · N · moduł), szew rozcięty (kopie wierzchołków z f + 1) ----
        var mesh = UnityEngine.Object.Instantiate(src); mesh.name = src.name + " (LONDON ściana kurtynowa)";
        var verts = new List<Vector3>(V); var nrm = new List<Vector3>(src.normals); var tan = new List<Vector4>(src.tangents);
        var uv0 = new List<Vector2>(src.uv); var kol = new List<Color>(src.colors);
        var f = new float[V.Length]; var ma = new bool[V.Length];
        foreach (var i in tris) if (!ma[i]) { ma[i] = true; f[i] = UdzialW(W[i]); }
        var uv2 = Enumerable.Range(0, V.Length).Select(i => new Vector2(ma[i] ? f[i] * N * LondonModul : 0f, W[i].y)).ToList();
        var kopie = new Dictionary<int, int>(); int szew = 0;
        int Kopia(int i)
        {
            if (kopie.TryGetValue(i, out var j)) return j;
            j = verts.Count; verts.Add(verts[i]);
            if (nrm.Count > 0) nrm.Add(nrm[i]); if (tan.Count > 0) tan.Add(tan[i]); if (uv0.Count > 0) uv0.Add(uv0[i]); if (kol.Count > 0) kol.Add(kol[i]);
            uv2.Add(new Vector2((f[i] + 1f) * N * LondonModul, W[i].y)); kopie[i] = j; return j;
        }
        var noweTri = new Dictionary<int, int[]>();
        foreach (var s in cel)
        {
            var t = src.GetTriangles(s);
            for (int q = 0; q < t.Length; q += 3)
            {
                float a = f[t[q]], b = f[t[q + 1]], c = f[t[q + 2]];
                if (Mathf.Max(a, Mathf.Max(b, c)) - Mathf.Min(a, Mathf.Min(b, c)) <= .5f) continue;
                for (int e = 0; e < 3; e++) if (f[t[q + e]] < .5f) t[q + e] = Kopia(t[q + e]);
                szew++;
            }
            noweTri[s] = t;
        }
        if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts); if (nrm.Count > 0) mesh.SetNormals(nrm); if (tan.Count > 0) mesh.SetTangents(tan);
        if (uv0.Count > 0) mesh.SetUVs(0, uv0); if (kol.Count > 0) mesh.SetColors(kol); mesh.SetUVs(1, uv2);
        foreach (var p in noweTri) mesh.SetTriangles(p.Value, p.Key, false);
        mesh.RecalculateBounds();
        BraxZapisAssetu.Utworz(mesh, BraxRegion.Out + "/Meshes/london_sciana_kurtynowa.asset");
        mf.sharedMesh = mesh;

        // ---- szkło ściany kurtynowej (nieprzezroczyste, cień rzuca) ----
        float kond = 3.4f * K;
        bool A = LondonWariant != 'B';
        var zapas = BraxCityMaterials.Surface("LONDON: szkło (zapasowe)", A ? new Color(.34f, .42f, .48f) : new Color(.16f, .19f, .22f), .12f, .8f);
        var szklo = BraxSzkloFasady.Mat("LONDON — ściana kurtynowa na łuku " + LondonWariant, LondonModul, kond, yCokol, A ? new Color(.30f, .40f, .46f) : new Color(.13f, .17f, .21f), A ? .6f : .5f, .32f, zapas);
        if (szklo && szklo.shader && szklo.shader.name == BraxSzkloFasady.ShaderName)
        {
            szklo.SetFloat("_SiatkaUV", 1f);
            szklo.SetFloat("_Rama", A ? .07f : .06f); szklo.SetColor("_RamaKolor", A ? new Color(.74f, .77f, .80f) : new Color(.26f, .28f, .30f));
            szklo.SetFloat("_Pas", .85f); szklo.SetColor("_PasKolor", A ? new Color(.26f, .32f, .37f) : new Color(.09f, .11f, .13f));
            szklo.SetFloat("_Typ", 0f); szklo.SetFloat("_FilarCo", 0f); szklo.SetFloat("_Tafla", .012f);
            EditorUtility.SetDirty(szklo);
        }
        for (int s = 0; s < mats.Length; s++) if (cel.Contains(s)) mats[s] = szklo;
        skora.sharedMaterials = mats;
        // wewnętrzna elewacja z oknami (metal 1.0, tekstura okien, przez całą wysokość) jest teraz za nieprzezroczystą skórą;
        // APK 2bfadac: ~0.4 % jej wierzchołków wystaje 15–40 cm przed skórę — zamiast czarnych plamek to samo szkło (siatka z rzutu)
        int wew = 0;
        var szkloWew = BraxSzkloFasady.Mat("LONDON — elewacja wewnętrzna " + LondonWariant, LondonModul, kond, yCokol, A ? new Color(.30f, .40f, .46f) : new Color(.13f, .17f, .21f), .5f, .3f, zapas);
        if (szkloWew && szkloWew.HasProperty("_RamaKolor") && szklo && szklo.HasProperty("_RamaKolor"))
        { szkloWew.SetColor("_RamaKolor", szklo.GetColor("_RamaKolor")); szkloWew.SetColor("_PasKolor", szklo.GetColor("_PasKolor")); szkloWew.SetFloat("_Pas", .85f); EditorUtility.SetDirty(szkloWew); }
        foreach (var r in bud.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r == skora) continue; var f2 = r.GetComponent<MeshFilter>(); if (!f2 || !f2.sharedMesh || !f2.sharedMesh.isReadable) continue;
            var ms = r.sharedMaterials; bool zm = false; var vv = f2.sharedMesh.vertices;
            for (int s = 0; s < ms.Length && s < f2.sharedMesh.subMeshCount; s++)
            {
                var m = ms[s]; if (!m || !(m.HasProperty("_Metallic") && m.GetFloat("_Metallic") >= .8f && m.HasProperty("_MainTex") && m.GetTexture("_MainTex"))) continue;
                var t = f2.sharedMesh.GetTriangles(s); if (t.Length == 0) continue;
                float y0 = t.Min(i => r.transform.TransformPoint(vv[i]).y), y1 = t.Max(i => r.transform.TransformPoint(vv[i]).y);
                if (y1 - y0 < 100f) continue;
                ms[s] = szkloWew; zm = true; wew++;
            }
            if (zm) r.sharedMaterials = ms;
        }
        skora.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        // poza łączeniem statycznym (UV2 elewacji zostaje nietknięte)
        GameObjectUtility.SetStaticEditorFlags(skora.gameObject, GameObjectUtility.GetStaticEditorFlags(skora.gameObject) & ~StaticEditorFlags.BatchingStatic);
        NdLog("LONDON: skóra = ściana kurtynowa " + (A ? "A (srebrno-błękitna, słupki aluminium)" : "B (grafitowa)") + " — podsiatki " + string.Join(",", cel) +
              ", przekrojów " + przekroje.Count + " co " + LondonPrzekroj + " m, obwód " + obwody[0].ToString("0") + "…" + obwody[obwody.Count - 1].ToString("0") + " m, słupków " + N +
              " (moduł " + LondonModul + " m przy obwodzie " + obwodRef.ToString("0") + " m), kondygnacja " + kond.ToString("0.00") + " m od " + (yCokol - yBaza).ToString("0.0") + " m, szew " + szew + " trójkątów, wierzchołków " + V.Length + " → " + verts.Count + ", elewacja wewnętrzna → szkło: " + wew + " podsiatek");
    }
}

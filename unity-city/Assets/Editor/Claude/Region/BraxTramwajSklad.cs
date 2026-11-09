using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// TRAMWAJ T1: PĘTLA, SKŁAD PRZEGUBOWY, DRZWI (9.11, właściciel 18:21: „drzwi się źle otwierają, nie łamie się na zakrętach,
// ma elegancko kursować”). Narzędzia buildu dla BraxRegionMiastoB.NdTramwaj (dane dla BraxTramT1):
//  • Petla — oś toru: prostokąt prawych pasów z łukami R na narożnikach (lewe skręty), punkty co `krok` m, limit prędkości;
//  • Przeguby — miejsca harmonijek w modelu CAF z geometrii (przewężenie pudła na wysokości okien), zapasowo układ 5 członów;
//  • Czlony — kopie modelu rozcięte w przegubach (BraxCityMeshCut), każda pod własnym węzłem „Człon i” (oś +z = kierunek jazdy);
//  • Drzwi — skrzydła z węzłów modelu (nazwy door/drzwi/puerta/porta…), a gdy ich nie ma: szyby drzwiowe z siatki (szkło
//    sięgające do podłogi po prawej stronie) wycięte do osobnych skrzydeł; ruch odskokowo-przesuwny;
//  • Szyny — dwie szyny rowkowe (główka + rowek) wzdłuż całej pętli, 12 mm nad asfaltem, przesunięcie głębi (bez migotania);
//  • Spis — pełny spis węzłów i materiałów modelu w logu (BRAXHORN_TRAM_MODEL) — do kontroli po buildzie.
public static class BraxTramwajSklad
{
    public const float Krok = .5f;
    static readonly string[] DrzwiNazwy = { "door", "drzwi", "puerta", "porta", "porte", "tuer", "tür", "portiere" };
    static readonly string[] SzkloNazwy = { "glass", "vidrio", "cristal", "window", "szyb", "okno", "ventana", "glas", "vetro" };

    public sealed class Trasa { public List<Vector3> p = new List<Vector3>(); public List<float> s = new List<float>(); public List<float> v = new List<float>(); public float dl; }

    /// <summary>Zamknięta pętla przez narożniki (kolejno), łuki promienia R; y stałe.</summary>
    public static Trasa Petla(Vector2[] rogi, float R, float y, float vmax, float aBok)
    {
        int n = rogi.Length; var tr = new Trasa();
        var t1 = new Vector2[n]; var t2 = new Vector2[n]; var c = new Vector2[n]; var a0 = new float[n]; var da = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = rogi[(i + n - 1) % n], p1 = rogi[i], p2 = rogi[(i + 1) % n];
            Vector2 d1 = (p1 - p0).normalized, d2 = (p2 - p1).normalized;
            float kat = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d1, d2), -1f, 1f));
            float off = R * Mathf.Tan(kat * .5f);
            t1[i] = p1 - d1 * off; t2[i] = p1 + d2 * off;
            float cr = d1.x * d2.y - d1.y * d2.x; var lewa = new Vector2(-d1.y, d1.x);
            c[i] = t1[i] + (cr > 0 ? lewa : -lewa) * R;
            a0[i] = Mathf.Atan2(t1[i].y - c[i].y, t1[i].x - c[i].x);
            da[i] = (cr > 0 ? 1f : -1f) * kat;
        }
        void Dodaj(Vector2 q, float vl)
        {
            var p = new Vector3(q.x, y, q.y);
            if (tr.p.Count > 0) tr.dl += Vector3.Distance(tr.p[tr.p.Count - 1], p);
            tr.p.Add(p); tr.s.Add(tr.dl); tr.v.Add(vl);
        }
        for (int i = 0; i < n; i++)
        {
            // łuk na narożniku i, potem prosta do łuku i+1
            int m = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(da[i]) * R / Krok));
            float vl = Mathf.Min(vmax, Mathf.Sqrt(aBok * R));
            for (int k = 0; k < m; k++) { float a = a0[i] + da[i] * k / m; Dodaj(c[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R, vl); }
            var odP = c[i] + new Vector2(Mathf.Cos(a0[i] + da[i]), Mathf.Sin(a0[i] + da[i])) * R; var doP = t1[(i + 1) % n];
            int ms = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(odP, doP) / Krok));
            for (int k = 0; k < ms; k++) Dodaj(Vector2.Lerp(odP, doP, (float)k / ms), vmax);
        }
        tr.dl += Vector3.Distance(tr.p[tr.p.Count - 1], tr.p[0]);
        return tr;
    }

    /// <summary>Odległość wzdłuż trasy najbliższego punktu do q (płasko).</summary>
    public static float SNajblizej(Trasa t, Vector3 q)
    {
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < t.p.Count; i++) { var d = t.p[i] - q; d.y = 0; if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = i; } }
        return t.s[best];
    }

    /// <summary>Przedział s (od, do), w którym oś toru jest w kwadracie skrzyżowania (półbok h). Pętla może zaczynać się w środku
    /// skrzyżowania — wtedy przedział przechodzi przez koniec trasy (do &lt; od; BraxTramT1 liczy go modulo długość).</summary>
    public static Vector2 SPrzedzial(Trasa t, Vector2 srodek, float h)
    {
        int n = t.p.Count; var w = new bool[n];
        for (int i = 0; i < n; i++) w[i] = Mathf.Abs(t.p[i].x - srodek.x) < h && Mathf.Abs(t.p[i].z - srodek.y) < h;
        if (!w.Any(q => q)) return new Vector2(-1f, -1f);
        // początek przedziału: pierwszy punkt „w środku”, którego poprzednik (modulo) jest „na zewnątrz”
        int start = -1; for (int i = 0; i < n; i++) if (w[i] && !w[(i + n - 1) % n]) { start = i; break; }
        if (start < 0) return new Vector2(0f, t.dl);   // cała pętla w polu (nie zdarza się)
        int k = start; while (w[(k + 1) % n] && (k + 1) % n != start) k = (k + 1) % n;
        return new Vector2(t.s[start], t.s[k]);
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Spis modelu do logu (węzły z siatką, materiały, wymiary) — po buildzie wiadomo, z czego jest tramwaj.</summary>
    public static void Spis(GameObject o)
    {
        var rs = o.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log("BRAXHORN_TRAM_MODEL rendererów " + rs.Length + ", granice " + BraxRegion.Bounds(o).size.ToString("F2"));
        int i = 0;
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>(); int tris = 0; if (mf && mf.sharedMesh) for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tris += (int)mf.sharedMesh.GetIndexCount(k) / 3;
            Debug.Log("BRAXHORN_TRAM_MODEL " + (i++) + " „" + r.name + "” (rodzic „" + (r.transform.parent ? r.transform.parent.name : "-") + "”) tri " + tris + " środek " + r.bounds.center.ToString("F2") + " rozmiar " + r.bounds.size.ToString("F2") +
                      " materiały: " + string.Join(", ", r.sharedMaterials.Select(m => m ? m.name : "-")));
            if (i >= 80) { Debug.Log("BRAXHORN_TRAM_MODEL … (dalsze pominięte)"); break; }
        }
    }

    /// <summary>Przeguby (x świata) z przewężeń pudła na wysokości okien; model ustawiony wzdłuż osi x. Zapasowo 5 członów.</summary>
    public static List<float> Przeguby(GameObject o, out string jak)
    {
        var b = BraxRegion.Bounds(o); float zc = b.center.z, y0 = b.min.y + 1.1f, y1 = b.min.y + 2.4f;
        const float bin = .1f; int nb = Mathf.CeilToInt(b.size.x / bin) + 1;
        var w = new float[nb]; var jest = new bool[nb];
        foreach (var mf in o.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.sharedMesh) continue; var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled) continue;
            var m = mf.transform.localToWorldMatrix;
            foreach (var lv in mf.sharedMesh.vertices)
            {
                var p = m.MultiplyPoint3x4(lv); if (p.y < y0 || p.y > y1) continue;
                int k = Mathf.Clamp((int)((p.x - b.min.x) / bin), 0, nb - 1);
                w[k] = Mathf.Max(w[k], Mathf.Abs(p.z - zc)); jest[k] = true;
            }
        }
        var lista = new List<float>();
        var pelne = w.Where((wx, wi) => jest[wi]).OrderBy(wx => wx).ToList();
        if (pelne.Count > 20)
        {
            float W = pelne[(int)(pelne.Count * .9f)], prog = W * .9f;
            int start = -1, ost = -1;
            for (int k = 0; k <= nb; k++)
            {
                bool waski = k < nb && jest[k] && w[k] < prog;
                bool pelny = k < nb && jest[k] && w[k] >= prog;
                if (waski) { if (start < 0) start = k; ost = k; }
                if ((pelny || k == nb) && start >= 0)
                {
                    float x0 = b.min.x + start * bin, x1 = b.min.x + (ost + 1) * bin, dl = x1 - x0, xc = (x0 + x1) * .5f;
                    if (dl >= .15f && dl <= 1.8f && xc - b.min.x > 4f && b.max.x - xc > 4f) lista.Add(xc);
                    start = -1;
                }
            }
            // przeguby nie bliżej niż 3.5 m od siebie
            var odf = new List<float>(); foreach (var px in lista.OrderBy(q => q)) if (odf.Count == 0 || px - odf[odf.Count - 1] > 3.5f) odf.Add(px);
            lista = odf;
        }
        if (lista.Count >= 2 && lista.Count <= 6) { jak = "z geometrii (przewężenia harmonijek): " + lista.Count; return lista; }
        jak = "zapasowo układ 5 członów (geometria dała " + lista.Count + ")";
        return new[] { .26f, .41f, .59f, .74f }.Select(f => b.min.x + f * b.size.x).ToList();
    }

    /// <summary>Kopia modelu z zachowanym przedziałem x [a, b] (reszta wycięta), pod węzłem członu (oś +z = +x świata).</summary>
    public static Transform Czlon(GameObject model, Transform rodzic, int nr, float a, float b, float yOs, float zOs, string folder)
    {
        var mb = BraxRegion.Bounds(model);
        var kopia = UnityEngine.Object.Instantiate(model, model.transform.parent);
        kopia.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation); kopia.transform.localScale = model.transform.localScale;
        kopia.name = "człon " + (nr + 1) + " (model CAF)";
        foreach (var c in kopia.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        Vector3 lo = mb.min - Vector3.one, hi = mb.max + Vector3.one;
        var przed = new Bounds(); przed.SetMinMax(new Vector3(lo.x, lo.y, lo.z), new Vector3(a, hi.y, hi.z));
        var za = new Bounds(); za.SetMinMax(new Vector3(b, lo.y, lo.z), new Vector3(hi.x, hi.y, hi.z));
        BraxCityMeshCut.Subtract(kopia.transform, przed, folder, "czlon" + nr + "a");
        BraxCityMeshCut.Subtract(kopia.transform, za, folder, "czlon" + nr + "b");
        var cz = new GameObject("Człon " + (nr + 1)).transform; cz.SetParent(rodzic, true);
        cz.position = new Vector3((a + b) * .5f, yOs, zOs); cz.rotation = Quaternion.Euler(0f, 90f, 0f);
        kopia.transform.SetParent(cz, true);
        foreach (var t in cz.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        // kolizja członu (oś lokalna z = długość) + kinematyczne ciało (ruchoma bryła)
        var bb = BraxRegion.Bounds(kopia);
        var box = cz.gameObject.AddComponent<BoxCollider>();
        box.center = cz.InverseTransformPoint(bb.center); box.size = new Vector3(bb.size.z, bb.size.y, Mathf.Max(.5f, b - a - .1f));
        var rb = cz.gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        return cz;
    }

    // ---------------------------------------------------------------------------------------------------------------
    public sealed class Skrzydlo { public Transform t; public Vector3 dom, wysun, przesun; }

    static bool Pasuje(string s, string[] slowa) { s = (s ?? "").ToLowerInvariant(); return slowa.Any(x => s.Contains(x)); }

    /// <summary>Skrzydła drzwi po prawej stronie członów (lokalne +x). Najpierw węzły modelu, potem szyby drzwiowe z siatki.</summary>
    public static List<Skrzydlo> Drzwi(Transform[] czlony, float podloga, string folder, out string jak)
    {
        var wynik = new List<Skrzydlo>();
        // (a) węzły / materiały nazwane jak drzwi
        foreach (var cz in czlony)
            foreach (var r in cz.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !Pasuje(r.name, DrzwiNazwy) && !r.sharedMaterials.Any(m => m && Pasuje(m.name, DrzwiNazwy))) continue;
                var lb = Lokalne(cz, r.bounds);
                if (lb.center.x < .4f || lb.size.y < 1.3f || lb.size.y > 3f || lb.size.z < .35f || lb.size.z > 2.8f || lb.size.x > .6f) continue;
                wynik.Add(new Skrzydlo { t = r.transform });
            }
        if (wynik.Count >= 2) { jak = "węzły modelu: " + wynik.Count; Ruch(czlony, wynik); return wynik; }
        wynik.Clear();
        // (b) szyby drzwiowe: szkło po prawej stronie sięgające do podłogi → osobne skrzydła
        int n = 0;
        foreach (var cz in czlony)
        {
            var lustro = new List<(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Material m)>();
            foreach (var mf in cz.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>(); var src = mf.sharedMesh; if (!r || !r.enabled || !src) continue;
                var mats = r.sharedMaterials; bool zmiana = false;
                var v = src.vertices; var uv = src.uv; var l2w = mf.transform.localToWorldMatrix;
                var nowe = new int[src.subMeshCount][];
                for (int sm = 0; sm < src.subMeshCount; sm++)
                {
                    var tri = src.GetTriangles(sm); var m = sm < mats.Length ? mats[sm] : null;
                    bool szklo = m && (Pasuje(m.name, SzkloNazwy) || (m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f));
                    if (!szklo) { nowe[sm] = tri; continue; }
                    var zost = new List<int>(tri.Length);
                    for (int j = 0; j + 2 < tri.Length; j += 3)
                    {
                        Vector3 A = l2w.MultiplyPoint3x4(v[tri[j]]), B = l2w.MultiplyPoint3x4(v[tri[j + 1]]), C = l2w.MultiplyPoint3x4(v[tri[j + 2]]);
                        Vector3 la = cz.InverseTransformPoint(A), lb2 = cz.InverseTransformPoint(B), lc = cz.InverseTransformPoint(C);
                        bool prawa = la.x > .9f && lb2.x > .9f && lc.x > .9f;
                        float minY = Mathf.Min(A.y, Mathf.Min(B.y, C.y));
                        if (prawa && minY < podloga + .7f)
                        {
                            Vector2 ua = uv.Length == v.Length ? uv[tri[j]] : Vector2.zero, ub = uv.Length == v.Length ? uv[tri[j + 1]] : Vector2.zero, uc = uv.Length == v.Length ? uv[tri[j + 2]] : Vector2.zero;
                            lustro.Add((A, B, C, ua, ub, uc, m)); zmiana = true;
                        }
                        else { zost.Add(tri[j]); zost.Add(tri[j + 1]); zost.Add(tri[j + 2]); }
                    }
                    nowe[sm] = zost.ToArray();
                }
                if (!zmiana) continue;
                var kopia = UnityEngine.Object.Instantiate(src); kopia.name = src.name + "_bez_szyb_drzwi";
                kopia.subMeshCount = nowe.Length; for (int sm = 0; sm < nowe.Length; sm++) kopia.SetTriangles(nowe[sm], sm, false);
                Directory.CreateDirectory(folder);
                BraxZapisAssetu.Utworz(kopia, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + BraxUliceKit.Bezpieczna(kopia.name) + ".asset"));
                mf.sharedMesh = kopia;
            }
            if (lustro.Count == 0) continue;
            // grupowanie wzdłuż członu (lokalne z): przerwa > 0.15 m = osobne drzwi
            var lz = lustro.Select(tq => { var za = cz.InverseTransformPoint(tq.a).z; var zb = cz.InverseTransformPoint(tq.b).z; var zc = cz.InverseTransformPoint(tq.c).z; return (min: Mathf.Min(za, Mathf.Min(zb, zc)), max: Mathf.Max(za, Mathf.Max(zb, zc)), q: tq); }).OrderBy(tq => tq.min).ToList();
            var grupy = new List<List<(float min, float max, (Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Material m) q)>>();
            float koniec = float.MinValue;
            foreach (var e in lz) { if (grupy.Count == 0 || e.min > koniec + .15f) grupy.Add(new List<(float, float, (Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, Material))>()); grupy[grupy.Count - 1].Add(e); koniec = Mathf.Max(koniec, e.max); }
            foreach (var g in grupy)
            {
                float z0 = g.Min(e => e.min), z1 = g.Max(e => e.max), szer = z1 - z0;
                if (szer < .5f || szer > 2.8f) { n += DodajSkrzydlo(cz, g.Select(e => e.q).ToList(), 0f, folder, wynik, "szyba (nie drzwi) " + szer.ToString("F2")); continue; }
                float zm = (z0 + z1) * .5f;
                var lewa = g.Where(e => (e.min + e.max) * .5f < zm).Select(e => e.q).ToList(); var prawa = g.Where(e => (e.min + e.max) * .5f >= zm).Select(e => e.q).ToList();
                if (szer > 1f && lewa.Count > 0 && prawa.Count > 0)
                {
                    n += DodajSkrzydlo(cz, lewa, -(szer * .5f) * .92f, folder, wynik, "lewe");
                    n += DodajSkrzydlo(cz, prawa, (szer * .5f) * .92f, folder, wynik, "prawe");
                }
                else n += DodajSkrzydlo(cz, g.Select(e => e.q).ToList(), -szer * .92f, folder, wynik, "pojedyncze");
            }
        }
        jak = wynik.Count > 0 ? "szyby drzwiowe wycięte z siatki: skrzydeł " + wynik.Count : "BRAK drzwi w modelu (ani węzłów, ani szyb do podłogi po prawej)";
        return wynik;
    }

    // jedno skrzydło z trójkątów (świat) — węzeł pod członem; przesuw wzdłuż członu (lokalne z), odskok na zewnątrz (+x)
    static int DodajSkrzydlo(Transform cz, List<(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Material m)> q, float przesun, string folder, List<Skrzydlo> wynik, string opis)
    {
        if (q.Count == 0) return 0;
        var srodek = Vector3.zero; foreach (var e in q) srodek += (e.a + e.b + e.c) / 3f; srodek /= q.Count;
        var g = new GameObject("Drzwi: skrzydło " + opis).transform; g.SetParent(cz, false); g.position = srodek; g.localRotation = Quaternion.identity;
        var mats = q.Select(e => e.m).Distinct().ToList();
        var V = new List<Vector3>(); var U = new List<Vector2>(); var subs = mats.Select(_ => new List<int>()).ToList();
        foreach (var e in q)
        {
            int k = V.Count; V.Add(g.InverseTransformPoint(e.a)); V.Add(g.InverseTransformPoint(e.b)); V.Add(g.InverseTransformPoint(e.c));
            U.Add(e.ua); U.Add(e.ub); U.Add(e.uc);
            var s = subs[mats.IndexOf(e.m)]; s.Add(k); s.Add(k + 1); s.Add(k + 2);
        }
        var mesh = new Mesh { name = "Tramwaj: " + g.name };
        mesh.SetVertices(V); mesh.SetUVs(0, U); mesh.subMeshCount = subs.Count; for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        BraxZapisAssetu.Utworz(mesh, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + BraxUliceKit.Bezpieczna(mesh.name) + ".asset"));
        g.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterials = mats.ToArray();
        if (Mathf.Abs(przesun) > .01f)
            wynik.Add(new Skrzydlo { t = g, dom = g.localPosition, wysun = new Vector3(.06f, 0f, 0f), przesun = new Vector3(0f, 0f, przesun) });
        return 1;
    }

    // ruch skrzydeł nazwanych (węzły modelu w dowolnej hierarchii): para wzdłuż członu rozsuwa się, pojedyncze w stronę -z
    static void Ruch(Transform[] czlony, List<Skrzydlo> s)
    {
        foreach (var cz in czlony)
        {
            var moje = s.Where(sk => sk.t.IsChildOf(cz)).OrderBy(sk => cz.InverseTransformPoint(sk.t.GetComponent<Renderer>().bounds.center).z).ToList();
            for (int i = 0; i < moje.Count; i++)
            {
                var x = moje[i]; var lb = Lokalne(cz, x.t.GetComponent<Renderer>().bounds);
                bool paraZPrawej = i + 1 < moje.Count && Lokalne(cz, moje[i + 1].t.GetComponent<Renderer>().bounds).min.z - lb.max.z < .3f;
                bool paraZLewej = i > 0 && lb.min.z - Lokalne(cz, moje[i - 1].t.GetComponent<Renderer>().bounds).max.z < .3f;
                float kier = paraZLewej ? 1f : paraZPrawej ? -1f : -1f;
                var p = x.t.parent;
                x.dom = x.t.localPosition;
                x.wysun = p ? p.InverseTransformVector(cz.TransformDirection(Vector3.right) * .06f) : cz.TransformDirection(Vector3.right) * .06f;
                x.przesun = p ? p.InverseTransformVector(cz.TransformDirection(Vector3.forward) * kier * lb.size.z * .92f) : cz.TransformDirection(Vector3.forward) * kier * lb.size.z * .92f;
            }
        }
    }

    static Bounds Lokalne(Transform cz, Bounds w)
    {
        var b = new Bounds(cz.InverseTransformPoint(w.center), Vector3.zero);
        for (int i = 0; i < 8; i++) b.Encapsulate(cz.InverseTransformPoint(new Vector3(i % 2 == 0 ? w.min.x : w.max.x, (i / 2) % 2 == 0 ? w.min.y : w.max.y, i / 4 == 0 ? w.min.z : w.max.z)));
        return b;
    }

    // ---------------------------------------------------------------------------------------------------------------
    /// <summary>Szyny rowkowe wzdłuż pętli: główka (stal, 12 mm nad asfaltem) i rowek (ciemny, 4 mm) po wewnętrznej stronie.</summary>
    public static void Szyny(Trasa t, float rozstaw, float glowka, float rowek, Transform rodzic, Material stal, Material ciemny)
    {
        var bs = new BraxRegionMeble.Bud(); var br = new BraxRegionMeble.Bud();
        int n = t.p.Count;
        foreach (float strona in new[] { -1f, 1f })
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = t.p[i], p1 = t.p[(i + 1) % n];
                Vector3 N0 = Normalna(t, i), N1 = Normalna(t, (i + 1) % n);
                float c = strona * (rozstaw * .5f + glowka * .5f);
                // główka: wierzch + dwa boki
                Vector3 a0 = p0 + N0 * (c - glowka * .5f), b0 = p0 + N0 * (c + glowka * .5f), a1 = p1 + N1 * (c - glowka * .5f), b1 = p1 + N1 * (c + glowka * .5f);
                Vector3 h = Vector3.up * .012f, h2 = Vector3.up * .004f;
                Gora(bs, a0 + h, b0 + h, b1 + h, a1 + h);
                Bok(bs, a0, a1, a1 + h, a0 + h, -N0); Bok(bs, b0, b1, b1 + h, b0 + h, N0);
                // rowek po wewnętrznej stronie (bliżej osi toru)
                float rc = strona * (rozstaw * .5f - rowek * .5f);
                Vector3 r0a = p0 + N0 * (rc - rowek * .5f), r0b = p0 + N0 * (rc + rowek * .5f), r1a = p1 + N1 * (rc - rowek * .5f), r1b = p1 + N1 * (rc + rowek * .5f);
                Gora(br, r0a + h2, r0b + h2, r1b + h2, r1a + h2);
            }
        }
        bs.Obiekt("Tramwaj T1: główki szyn (pętla)", rodzic, stal, false, false);
        br.Obiekt("Tramwaj T1: rowki szyn (pętla)", rodzic, ciemny, false, false);
    }
    static Vector3 Normalna(Trasa t, int i)
    {
        int n = t.p.Count; var d = t.p[(i + 1) % n] - t.p[(i + n - 1) % n]; d.y = 0; d.Normalize();
        return new Vector3(-d.z, 0f, d.x);
    }
    static void Gora(BraxRegionMeble.Bud b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        if (Vector3.Cross(p1 - p0, p2 - p0).y >= 0f) b.Quad(p0, p1, p2, p3); else b.Quad(p0, p3, p2, p1);
    }
    static void Bok(BraxRegionMeble.Bud b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 na)
    {
        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), na) >= 0f) b.Quad(p0, p1, p2, p3); else b.Quad(p0, p3, p2, p1);
    }
}

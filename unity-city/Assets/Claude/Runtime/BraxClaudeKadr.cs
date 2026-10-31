using System;
using System.Collections.Generic;
using UnityEngine;

// BRAXHORN R2 — pomiar kadru tylko w trybie przeglądu (adres z ?qa=1): co sekundę jedna linia do konsoli:
// BRAXHORN_KADR trojkaty=… obiekty=… materialy=… z_cieniami=… trawa_kepy=… trawa_trojkaty=… trawa_wywolania=… najciezsze=…
//  • trojkaty / obiekty / materialy — renderery, których prostopadłościan otaczający jest w ostrosłupie kamery (to rysuje
//    kamera; bez zasłaniania, bo osiedle nie ma wypalonego occlusion culling) + kępy trawy z BraxClaudeTrawa;
//  • z_cieniami — trójkąty z Renderer.isVisible (kamera LUB mapa cieni; build 3 liczył tylko to — górne oszacowanie);
//  • najciezsze — 6 grup (rodzic/rodzic) z największą liczbą trójkątów w kadrze kamery.
// Poza trybem przeglądu komponent sam się wyłącza (zero kosztu w grze).
public class BraxClaudeKadr : MonoBehaviour
{
    Renderer[] all = Array.Empty<Renderer>();
    readonly Dictionary<Renderer, int> tris = new Dictionary<Renderer, int>(), mats = new Dictionary<Renderer, int>();
    readonly Dictionary<Renderer, string> grp = new Dictionary<Renderer, string>();   // grupa liczona raz (bez alokacji co sekundę)
    readonly Dictionary<string, long> top = new Dictionary<string, long>();
    readonly Plane[] planes = new Plane[6];
    BraxClaudeTrawa[] trawa = Array.Empty<BraxClaudeTrawa>();
    float next;

    void Start()
    {
        bool qa = false;
        if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var url))
            foreach (var v in url.Query.TrimStart('?').Split('&')) if (v == "qa=1") qa = true;
        if (!qa) { enabled = false; return; }
        all = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var r in all)
        {
            tris[r] = Count(r); mats[r] = Mathf.Max(1, r.sharedMaterials.Length);
            var p = r.transform.parent;   // grupa = nazwa rodzica-grupy (2 poziomy w górę) — żeby wiedzieć, co waży w kadrze
            grp[r] = (p ? (p.parent ? p.parent.name + "/" + p.name : p.name) : r.name).Replace(' ', '_');
        }
        trawa = FindObjectsByType<BraxClaudeTrawa>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    static int Count(Renderer r)
    {
        Mesh m = null;
        if (r is SkinnedMeshRenderer s) m = s.sharedMesh;
        else { var f = r.GetComponent<MeshFilter>(); if (f) m = f.sharedMesh; }
        if (!m) return 0;
        long n = 0;
        for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i) / 3;
        return (int)Math.Min(n, int.MaxValue);
    }

    static Camera Cam()
    {
        var c = Camera.main;
        if (c && c.isActiveAndEnabled) return c;
        foreach (var k in Camera.allCameras) if (k && k.isActiveAndEnabled && k.targetTexture == null) return k;
        return null;
    }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 1f;
        var cam = Cam();
        if (!cam) return;
        GeometryUtility.CalculateFrustumPlanes(cam, planes);
        long t = 0, tv = 0; int objs = 0, calls = 0;
        top.Clear();
        foreach (var r in all)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!tris.TryGetValue(r, out var c)) continue;
            if (r.isVisible) tv += c;
            if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
            objs++; t += c;
            if (mats.TryGetValue(r, out var mc)) calls += mc;
            var g = grp[r];
            top[g] = (top.TryGetValue(g, out var v) ? v : 0) + c;
        }
        long tk = 0; int kepy = 0, wyw = 0, kaw = 0;
        foreach (var g in trawa) if (g) { kepy += g.ostatnioKep; wyw += g.ostatnioWywolan; kaw += g.kawalkowWPamieci; tk += (long)g.ostatnioKep * (g.siatka ? (int)(g.siatka.GetIndexCount(0) / 3) : 6); }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(top, q => q.Value), 6)) sb.Append(kv.Key).Append(':').Append(kv.Value).Append(';');
        Debug.Log("BRAXHORN_KADR trojkaty=" + (t + tk) + " obiekty=" + objs + " materialy=" + (calls + wyw) + " z_cieniami=" + tv
                  + " trawa_kepy=" + kepy + " trawa_trojkaty=" + tk + " trawa_wywolania=" + wyw + " trawa_kawalki_w_pamieci=" + kaw + " najciezsze=" + sb);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

// BRAXHORN R2 — pomiar kadru tylko w trybie przeglądu (adres z ?qa=1): co sekundę liczy, ile trójkątów i obiektów
// rysuje kamera, i wypisuje jedną linię do konsoli: BRAXHORN_KADR trojkaty=… obiekty=… materialy=…
// Liczone renderery z Renderer.isVisible (widoczne dla kamery LUB dla mapy cieni — górne oszacowanie).
// Poza trybem przeglądu komponent sam się wyłącza (zero kosztu w grze).
public class BraxClaudeKadr : MonoBehaviour
{
    Renderer[] all = Array.Empty<Renderer>();
    readonly Dictionary<Renderer, int> tris = new Dictionary<Renderer, int>(), mats = new Dictionary<Renderer, int>();
    float next;

    void Start()
    {
        bool qa = false;
        if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var url))
            foreach (var v in url.Query.TrimStart('?').Split('&')) if (v == "qa=1") qa = true;
        if (!qa) { enabled = false; return; }
        all = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var r in all) { tris[r] = Count(r); mats[r] = Mathf.Max(1, r.sharedMaterials.Length); }
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

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 1f;
        long t = 0; int objs = 0, calls = 0;
        foreach (var r in all)
        {
            if (!r || !r.enabled || !r.isVisible) continue;
            objs++;
            if (mats.TryGetValue(r, out var mc)) calls += mc;
            if (tris.TryGetValue(r, out var c)) t += c;
        }
        Debug.Log("BRAXHORN_KADR trojkaty=" + t + " obiekty=" + objs + " materialy=" + calls);
    }
}

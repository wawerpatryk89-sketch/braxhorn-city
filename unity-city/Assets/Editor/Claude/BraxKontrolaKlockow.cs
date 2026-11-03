using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ZAKAZ „KLOCKÓW” (pkt 26 / 38, właściciel 23:43: „Nigdy więcej nie chcę takich rzeczy widzieć”): audyt grup obiektów miasta
// złożonych w > 80 % z gołych prymitywów (sześcian / walec / kula / kapsuła albo bryła 24 wierzchołków = pudełko) bez tekstury.
// Grupa = obiekt z ≥ 5 rendererami-dziećmi (najwyżej położony taki przodek). Pojazdy i maszyny z klocków (ciężarówki, wózki,
// ciągniki, dźwigi samojezdne) są USUWANE (z kolizjami); reszta trafia na listę do wymiany. Log BRAXHORN_KLOCKI.
public static class BraxKontrolaKlockow
{
    static readonly string[] Prymitywy = { "Cube", "Cylinder", "Sphere", "Capsule", "Quad" };
    static readonly string[] Pojazdy = { "truck", "ciężar", "ciezar", "wózek", "wozek", "forklift", "ciągnik", "ciagnik", "tractor", "lorry", "naczepa", "trailer", "pojazd", "vehicle", "koparka", "ładowark", "ladowark", "reach stacker" };

    static bool Klocek(Renderer r)
    {
        var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) return false;
        bool prym = Prymitywy.Contains(m.name) || (m.vertexCount <= 24 && m.vertexCount >= 8);
        if (!prym) return false;
        var mat = r.sharedMaterial;
        return !mat || !mat.mainTexture || mat.mainTexture.width <= 4;
    }

    public static void Wykonaj(Transform root)
    {
        var rend = root.GetComponentsInChildren<MeshRenderer>(true);
        var ile = new Dictionary<Transform, int>();                  // renderery w poddrzewie (jeden przebieg)
        foreach (var r in rend) for (var q = r.transform.parent; q && q != root; q = q.parent) { ile.TryGetValue(q, out int n); ile[q] = n + 1; }
        var grupy = new Dictionary<Transform, (int all, int kl)>();
        foreach (var r in rend)
        {
            // grupa: najwyższy przodek (poniżej korzenia miasta) z 5…60 rendererami w poddrzewie
            Transform g = null;
            for (var q = r.transform.parent; q && q != root; q = q.parent)
            {
                int n = ile[q];
                if (n > 60) break;
                if (n >= 5) g = q;
            }
            if (!g) continue;
            grupy.TryGetValue(g, out var v);
            v.all++; if (Klocek(r)) v.kl++;
            grupy[g] = v;
        }
        var lista = new List<string>(); var usuniete = new List<string>(); var doUsun = new List<GameObject>();
        foreach (var kv in grupy)
        {
            var (all, kl) = kv.Value; if (all < 5 || kl < all * .8f) continue;
            var t = kv.Key; if (!t) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            string sc = Sciezka(t, root), wpis = sc + " (" + kl + "/" + all + " prymitywów, " + b.size.x.ToString("F1") + "×" + b.size.y.ToString("F1") + "×" + b.size.z.ToString("F1") + " m)";
            string n = sc.ToLowerInvariant();
            bool grywalny = t.GetComponentInChildren<BraxAuto>(true) || t.GetComponentInChildren<BraxVessel>(true);   // auta i łodzie gracza nigdy
            if (Pojazdy.Any(n.Contains) && !grywalny) { usuniete.Add(wpis); doUsun.Add(t.gameObject); }
            else lista.Add(wpis);
        }
        foreach (var g in doUsun) if (g) UnityEngine.Object.DestroyImmediate(g);
        string linia = "BRAXHORN_KLOCKI lista=" + lista.Count + " usuniete_pojazdy=" + usuniete.Count
            + (usuniete.Count > 0 ? " usunięte=[" + string.Join("; ", usuniete.Take(25)) + "]" : "")
            + (lista.Count > 0 ? " do_wymiany=[" + string.Join("; ", lista.Take(40)) + (lista.Count > 40 ? "; …" : "") + "]" : "");
        if (lista.Count == 0) Debug.Log(linia); else Debug.LogWarning(linia);
    }

    static string Sciezka(Transform t, Transform root)
    {
        var s = new List<string>();
        for (var q = t; q && q != root; q = q.parent) s.Add(q.name);
        s.Reverse(); return string.Join("/", s);
    }
}

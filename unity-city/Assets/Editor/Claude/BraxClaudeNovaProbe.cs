using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Sonda geometrii bloku nad klubem NOVA (właściciel 31.10, 21:09: „mieszkanie Braxa nad Club NOVA… biedne mieszkanie,
// od którego zacznie się jego kariera”). Zapisuje do raportu wszystkie renderery i kolizje w pudle x 22…62, y −1…24,
// z 28…62 (nazwa, granice, materiały, trójkąty, ścieżka w hierarchii) — na tej podstawie mieszkanie (klatka z boku klubu,
// piętro nad NOVA) jest wstawiane dokładnie w bryłę, bez zgadywania. Nic w scenie nie zmienia.
public static class BraxClaudeNovaProbe
{
    [Serializable] sealed class Item { public string name, path, kind, mats; public Vector3 min, max; public int tris; public bool enabled; }
    [Serializable] sealed class Report { public List<Item> renderers = new List<Item>(), colliders = new List<Item>(); public string note = ""; }

    public static void Apply()
    {
        var rep = new Report();
        var box = new Bounds(); box.SetMinMax(new Vector3(22f, -1f, 28f), new Vector3(62f, 24f, 62f));
        string Path(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 220; p = p.parent) s = p.name + "/" + s; return s; }
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!r || !box.Intersects(r.bounds)) continue;
            var mf = r.GetComponent<MeshFilter>(); int tris = 0;
            try { if (mf && mf.sharedMesh) tris = (int)(mf.sharedMesh.GetIndexCount(0) / 3); } catch { }
            rep.renderers.Add(new Item { name = r.name, path = Path(r.transform), kind = "renderer", mats = string.Join("|", r.sharedMaterials.Where(m => m).Select(m => m.name)), min = r.bounds.min, max = r.bounds.max, tris = tris, enabled = r.enabled && r.gameObject.activeInHierarchy });
            if (rep.renderers.Count > 4000) break;
        }
        foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c || !box.Intersects(c.bounds)) continue;
            rep.colliders.Add(new Item { name = c.name, path = Path(c.transform), kind = c.GetType().Name, min = c.bounds.min, max = c.bounds.max, enabled = c.enabled && c.gameObject.activeInHierarchy });
            if (rep.colliders.Count > 4000) break;
        }
        rep.note = "renderery: " + rep.renderers.Count + ", kolizje: " + rep.colliders.Count;
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeNova");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeNova/nova-probe.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_NOVA_PROBE " + rep.note);
    }
}

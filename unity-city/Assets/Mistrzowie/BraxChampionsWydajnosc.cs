using System.Collections.Generic;
using UnityEngine;

// Owner 12.11 11:52-12:23: "gra zacina, zamula", "ta gra ma płynnie chodzić ... na średnim", "w mieście R1 ... się nic nie
// tnie". Measured in APK 39 (the scene bundle): 1 044 point lights and 520 live planar mirrors in one scene, every renderer
// drawn to the 18 km far plane. Three things per frame that R1 does not pay for, kept in check here (region rig only):
//  - LAMPS: a point / spot light is on only while the camera is within its range + 28 world units (each lit lamp is sorted
//    against every renderer on screen, every frame);
//  - MIRRORS: only one live mirror renders at a time - the nearest one the camera really sees (in front of it, in its
//    range, no wall, door or floor on the line). Each rendering mirror draws the scene once more: mirrors of the rooms
//    behind the wall and on the floors above and below were all doing it;
//  - SMALL THINGS: layers 20 / 21 (set at build time on objects under 4 m / 1.2 m, BraxChampionsExplorer) are drawn to
//    260 / 90 world units, a sphere round the camera; buildings, ground, trees and the horizon keep the full view.
[DisallowMultipleComponent]
public sealed class BraxChampionsWydajnosc : MonoBehaviour
{
    public const int WarstwaMale = 20, WarstwaDrobne = 21;
    public const float ZasiegMale = 260f, ZasiegDrobne = 90f, ZapasLampy = 28f;
    public Camera kamera;

    Light[] lampy; BraxGymPlanarMirror[] lustra; BraxGymPlanarMirror czynne;
    float nastepneLampy, nastepneLustra; int kursor;
    static readonly RaycastHit[] trafienia = new RaycastHit[16];

    void Start()
    {
        if (!kamera) kamera = Camera.main;
        var wszystkie = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var l = new List<Light>();
        foreach (var x in wszystkie) if (x && x.type != LightType.Directional) l.Add(x);
        lampy = l.ToArray();
        lustra = FindObjectsByType<BraxGymPlanarMirror>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in lustra) if (m) m.enabled = false;
        Debug.Log("A1_WYDAJNOSC lamps=" + lampy.Length + " (on within range+" + ZapasLampy + ") mirrors=" + lustra.Length + " (one in sight at a time) small layers 20/21 to " + ZasiegMale + "/" + ZasiegDrobne);
    }

    void LateUpdate()
    {
        if (!kamera) { kamera = Camera.main; if (!kamera) return; }
        Warstwy();
        float t = Time.unscaledTime; var p = kamera.transform.position;
        if (t >= nastepneLampy) { nastepneLampy = t + .1f; Lampy(p); }
        if (t >= nastepneLustra) { nastepneLustra = t + .25f; Lustra(p); }
    }

    // the graphics modes rewrite the camera when they change: keep the two small layers' distances on it
    void Warstwy()
    {
        var d = kamera.layerCullDistances;
        if (d[WarstwaMale] == ZasiegMale && d[WarstwaDrobne] == ZasiegDrobne && kamera.layerCullSpherical) return;
        d[WarstwaMale] = ZasiegMale; d[WarstwaDrobne] = ZasiegDrobne;
        kamera.layerCullDistances = d; kamera.layerCullSpherical = true;
    }

    // a quarter of the lamps each tenth of a second (all of them every 0.4 s)
    void Lampy(Vector3 p)
    {
        int n = lampy.Length; if (n == 0) return;
        int ile = Mathf.Max(64, n / 4);
        for (int i = 0; i < ile; i++)
        {
            var x = lampy[kursor]; kursor = (kursor + 1) % n;
            if (!x) continue;
            float r = x.range + ZapasLampy; bool blisko = (x.transform.position - p).sqrMagnitude < r * r;
            if (x.enabled != blisko) x.enabled = blisko;
        }
    }

    void Lustra(Vector3 p)
    {
        BraxGymPlanarMirror najlepsze = null; float bd = float.MaxValue;
        foreach (var m in lustra)
        {
            if (!m || !m.sourceCamera || !m.gameObject.activeInHierarchy) continue;
            var tr = m.transform; var d = p - tr.position; float d2 = d.sqrMagnitude;
            if (d2 > m.maximumViewDistance * m.maximumViewDistance || d2 >= bd) continue;
            if (Vector3.Dot(tr.forward, d) <= .03f) continue;                 // behind the glass
            if (!Widac(p, tr.position + tr.forward * .06f)) continue;
            najlepsze = m; bd = d2;
        }
        if (najlepsze == czynne) return;
        if (czynne) czynne.enabled = false;
        czynne = najlepsze;
        if (czynne) czynne.enabled = true;
    }

    // nothing solid and still on the line (people, the player, cars and triggers do not count)
    static bool Widac(Vector3 oko, Vector3 cel)
    {
        var d = cel - oko; float dl = d.magnitude; if (dl < .01f) return true;
        int n = Physics.RaycastNonAlloc(oko, d / dl, trafienia, dl, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = trafienia[i].collider;
            if (!c || c is CharacterController || trafienia[i].rigidbody) continue;
            return false;
        }
        return true;
    }
}

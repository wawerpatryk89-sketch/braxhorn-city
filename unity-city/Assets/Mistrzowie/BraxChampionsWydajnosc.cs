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
//    FROZEN PICTURES (owner 12.11 16:58 "u fryzjera są lustra a połowa nie ma luster ... są dwa lustra, reszta bez"): the
//    other mirrors near the player keep the last picture they rendered (a 384 x 192 copy with its own projection), so a
//    salon wall of mirrors shows the room in every one of them, not the dark backing. A mirror in view that has never
//    rendered is woken for a quarter of a second (two at a time) to take its picture. At most 40 copies (the farthest let go).
//  - SMALL THINGS: layers 20 / 21 (set at build time on objects under 4 m / 1.2 m, BraxChampionsExplorer) are drawn to
//    260 / 90 world units, a sphere round the camera; buildings, ground, trees and the horizon keep the full view.
[DisallowMultipleComponent]
public sealed class BraxChampionsWydajnosc : MonoBehaviour
{
    public const int WarstwaMale = 20, WarstwaDrobne = 21;
    public const float ZasiegMale = 260f, ZasiegDrobne = 90f, ZapasLampy = 28f;
    public Camera kamera;

    Light[] lampy; BraxGymPlanarMirror[] lustra; BraxGymPlanarMirror czynne;
    sealed class Kopia { public RenderTexture rt; public Matrix4x4 mac; public Vector4 plaszczyzna; }
    readonly Dictionary<BraxGymPlanarMirror, Kopia> kopie = new Dictionary<BraxGymPlanarMirror, Kopia>();
    readonly List<BraxGymPlanarMirror> budzone = new List<BraxGymPlanarMirror>();
    readonly Plane[] stozek = new Plane[6];
    MaterialPropertyBlock mpb;
    const int MaxKopii = 40, BudzNaRaz = 2;
    static readonly int TexId = Shader.PropertyToID("_ReflectionTex"), MacId = Shader.PropertyToID("_ReflectionTextureMatrix"),
        GotoweId = Shader.PropertyToID("_ReflectionReady"), PlaszczyznaId = Shader.PropertyToID("_MirrorPlane");
    float nastepneLampy, nastepneLustra; int kursor;
    static readonly RaycastHit[] trafienia = new RaycastHit[16];

    // The region is read in parts (BraxChampionsObszary): the lamps and mirrors of a building arrive with its scene and leave
    // with it, so the lists are taken again whenever a scene comes or goes.
    bool skanuj = true;
    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += ScenaDoszla; UnityEngine.SceneManagement.SceneManager.sceneUnloaded += ScenaOdeszla; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= ScenaDoszla; UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= ScenaOdeszla; }
    void ScenaDoszla(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) { skanuj = true; }
    void ScenaOdeszla(UnityEngine.SceneManagement.Scene s) { skanuj = true; }

    void Start() { if (!kamera) kamera = Camera.main; }

    void Przeskanuj()
    {
        skanuj = false;
        var wszystkie = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var l = new List<Light>();
        foreach (var x in wszystkie) if (x && x.type != LightType.Directional) l.Add(x);
        lampy = l.ToArray(); kursor = 0;
        lustra = FindObjectsByType<BraxGymPlanarMirror>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // a mirror that is new here sleeps until it is seen; the live one and the ones showing their copy stay as they are
        foreach (var m in lustra) if (m && m != czynne && !kopie.ContainsKey(m) && !budzone.Contains(m)) m.enabled = false;
        // the copies of mirrors that left with their scene
        List<BraxGymPlanarMirror> martwe = null;
        foreach (var para in kopie) if (!para.Key) (martwe ?? (martwe = new List<BraxGymPlanarMirror>())).Add(para.Key);
        if (martwe != null) foreach (var m in martwe) { var k = kopie[m]; kopie.Remove(m); if (k.rt) { k.rt.Release(); Destroy(k.rt); } }
        budzone.RemoveAll(b => !b);
        Debug.Log("A1_WYDAJNOSC lamps=" + lampy.Length + " (on within range+" + ZapasLampy + ") mirrors=" + lustra.Length + " (one in sight at a time) small layers 20/21 to " + ZasiegMale + "/" + ZasiegDrobne);
    }

    void LateUpdate()
    {
        if (!kamera) { kamera = Camera.main; if (!kamera) return; }
        if (skanuj || lampy == null) Przeskanuj();
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
        // the mirrors woken last time: their picture is taken, they go back to sleep showing it
        foreach (var b in budzone) if (b && b != czynne) Wylacz(b);
        budzone.Clear();
        if (czynne) Zrob(czynne);                                           // the live one's copy kept fresh
        GeometryUtility.CalculateFrustumPlanes(kamera, stozek);
        BraxGymPlanarMirror najlepsze = null; float bd = float.MaxValue;
        foreach (var m in lustra)
        {
            if (!m || !m.sourceCamera || !m.gameObject.activeInHierarchy) continue;
            var tr = m.transform; var d = p - tr.position; float d2 = d.sqrMagnitude;
            if (d2 > m.maximumViewDistance * m.maximumViewDistance) continue;
            if (Vector3.Dot(tr.forward, d) <= .03f) continue;                 // behind the glass
            bool maKopie = kopie.ContainsKey(m);
            if (d2 >= bd && maKopie) continue;                                 // neither nearer nor in need of a picture
            if (!Widac(p, tr.position + tr.forward * .06f)) continue;
            if (d2 < bd) { najlepsze = m; bd = d2; }
            if (!maKopie && budzone.Count < BudzNaRaz && m.mirrorRenderer && GeometryUtility.TestPlanesAABB(stozek, m.mirrorRenderer.bounds)) budzone.Add(m);
        }
        budzone.Remove(najlepsze);
        if (najlepsze != czynne)
        {
            if (czynne) Wylacz(czynne);
            czynne = najlepsze;
            if (czynne) czynne.enabled = true;
        }
        foreach (var b in budzone) b.enabled = true;
        if (kopie.Count > MaxKopii) Sprzataj(p);
    }

    // a copy of the mirror's live picture with its projection (taken only once it has really rendered)
    void Zrob(BraxGymPlanarMirror m)
    {
        if (!m || !m.Ready || !m.mirrorRenderer) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        if (!kopie.TryGetValue(m, out var k)) { k = new Kopia(); kopie[m] = k; }
        if (!k.rt) { k.rt = new RenderTexture(384, 192, 0, RenderTextureFormat.ARGB32) { name = "BRAXHORN lustro kopia" }; k.rt.Create(); }
        Graphics.Blit(m.ReflectionTexture, k.rt);
        m.mirrorRenderer.GetPropertyBlock(mpb); k.mac = mpb.GetMatrix(MacId); k.plaszczyzna = mpb.GetVector(PlaszczyznaId);
    }

    // switched off: the copy goes on the glass instead of the dark backing the mirror clears to
    void Wylacz(BraxGymPlanarMirror m)
    {
        if (!m) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        Zrob(m); m.enabled = false;
        if (!kopie.TryGetValue(m, out var k) || !k.rt || !m.mirrorRenderer) return;
        m.mirrorRenderer.GetPropertyBlock(mpb);
        mpb.SetTexture(TexId, k.rt); mpb.SetMatrix(MacId, k.mac); mpb.SetVector(PlaszczyznaId, k.plaszczyzna); mpb.SetFloat(GotoweId, 1f);
        m.mirrorRenderer.SetPropertyBlock(mpb);
    }

    // too many copies: the farthest ones let go (their glass back to the backing)
    void Sprzataj(Vector3 p)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        var lista = new List<BraxGymPlanarMirror>(kopie.Keys);
        lista.Sort((a, b) => (a ? (a.transform.position - p).sqrMagnitude : 0f).CompareTo(b ? (b.transform.position - p).sqrMagnitude : 0f));
        for (int i = MaxKopii; i < lista.Count; i++)
        {
            var m = lista[i]; var k = kopie[m]; kopie.Remove(m);
            if (m && m != czynne && m.mirrorRenderer) { m.mirrorRenderer.GetPropertyBlock(mpb); mpb.SetFloat(GotoweId, 0f); mpb.SetTexture(TexId, Texture2D.blackTexture); m.mirrorRenderer.SetPropertyBlock(mpb); }
            if (k.rt) { k.rt.Release(); Destroy(k.rt); }
        }
    }

    void OnDestroy() { foreach (var k in kopie.Values) if (k.rt) { k.rt.Release(); Destroy(k.rt); } kopie.Clear(); }

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

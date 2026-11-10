using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owner 10.11 15:59: "Piasek na plaży ma być zapadający się pod nogami, zostawiając ślady, jak idę po nim. To jest
// globalnie na każdą plażę." and 16:39 (asked how long the prints stay): "Znikają po ok. 1 min".
// Dopinany sam do gracza po wczytaniu sceny (jak BraxKroki, bez zmian w BraxGymPlayer). On sand (the foundation's
// sand_dry / sand_wet ground, or any collider / material whose name says sand or piasek):
//   - the feet sink in: the player's visual sinks 4 cm into the sand (eased in and out, never on paving),
//   - every footfall (the foot bone comes down after a lift, as in BraxKroki) leaves a real 3D footprint where it lands:
//     a sole of compressed, darker sand 10 mm over the surface inside a ridge of pushed-up sand 35 mm high whose outer
//     slope starts 10 mm BELOW the surface - real thickness, nothing lies on the sand 1-5 mm apart (zero migotania),
//   - after 50 s a print settles back into the sand over 10 s (it sinks under the surface, no transparency) and is
//     reused; at most 160 prints (a minute of walking).
[DisallowMultipleComponent]
public sealed class BraxChampionsSandSteps : MonoBehaviour
{
    public const float Sink = .04f, Life = 60f, Settle = 10f;
    public const int MaxPrints = 160;
    Transform lewa, prawa, visual; CharacterController cc;
    Vector3 visualBase; float sunk;
    float spocznikL = float.NaN, spocznikP = float.NaN; bool gorL, gorP;
    float nextGround; bool onSand;

    struct Print { public Transform t; public float born; public Vector3 at; public Vector3 up; }
    static readonly List<Print> prints = new List<Print>();
    static int next;                                                    // the next print to reuse once all are out
    static Mesh mesh;
    static readonly Dictionary<Material, Material[]> mats = new Dictionary<Material, Material[]>();
    static Transform holder;
    float nextFade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { Dopnij(); SceneManager.sceneLoaded += (s, m) => { prints.Clear(); next = 0; holder = null; Dopnij(); }; }
    static void Dopnij()
    {
        var p = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (p && !p.GetComponent<BraxChampionsSandSteps>()) p.gameObject.AddComponent<BraxChampionsSandSteps>();
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        var player = GetComponent<BraxGymPlayer>();
        visual = player ? player.visual : null;
        if (visual == transform) visual = null;                      // never move the body that carries the controller
        if (visual) visualBase = visual.localPosition;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (!lewa && t.name.EndsWith("LeftFoot")) lewa = t;
            if (!prawa && t.name.EndsWith("RightFoot")) prawa = t;
        }
    }

    public static bool IsSand(Collider c)
    {
        if (!c) return false;
        if (Says(c.name) || Says(c.sharedMaterial ? c.sharedMaterial.name : null)) return true;
        var r = c.GetComponent<Renderer>();
        return r && r.sharedMaterial && Says(r.sharedMaterial.name);
    }
    static bool Says(string s) { if (string.IsNullOrEmpty(s)) return false; s = s.ToLowerInvariant(); return s.Contains("sand") || s.Contains("piasek") || s.Contains("piask"); }

    void LateUpdate()
    {
        if (!cc || !cc.enabled) return;
        if (Time.time >= nextGround)
        {
            nextGround = Time.time + .15f;
            onSand = cc.isGrounded && Ground(transform.position + Vector3.up * .5f, 1.2f, out var hit) && IsSand(hit.collider);
        }
        // the feet sink in (eased), back out on any other ground
        sunk = Mathf.MoveTowards(sunk, onSand ? Sink : 0f, Time.deltaTime * .25f);
        if (visual) visual.localPosition = visualBase + visual.parent.InverseTransformVector(Vector3.down * sunk);
        float v = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
        bool ruch = cc.isGrounded && v > .3f;
        Stopa(lewa, ref spocznikL, ref gorL, ruch, true);
        Stopa(prawa, ref spocznikP, ref gorP, ruch, false);
        if (Time.time >= nextFade) { nextFade = Time.time + .2f; Fade(); }
    }

    // the nearest ground under a point, never the player's own colliders
    bool Ground(Vector3 from, float dist, out RaycastHit best)
    {
        best = default; float d = float.MaxValue; bool found = false;
        foreach (var h in Physics.RaycastAll(from, Vector3.down, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.distance >= d) continue;
            best = h; d = h.distance; found = true;
        }
        return found;
    }

    void Stopa(Transform s, ref float spocznik, ref bool gora, bool ruch, bool left)
    {
        if (!s) return;
        float h = s.position.y - transform.position.y;
        if (float.IsNaN(spocznik) || (!ruch && cc.isGrounded)) { spocznik = float.IsNaN(spocznik) ? h : Mathf.Lerp(spocznik, h, .1f); gora = false; return; }
        if (h > spocznik + .07f) gora = true;
        else if (gora && h < spocznik + .03f)
        {
            gora = false;
            if (!ruch || !onSand) return;
            if (Ground(s.position + Vector3.up * .6f, 1.4f, out var hit) && IsSand(hit.collider)) Leave(hit, left);
        }
    }

    void Leave(RaycastHit hit, bool left)
    {
        var sand = hit.collider.GetComponent<Renderer>() ? hit.collider.GetComponent<Renderer>().sharedMaterial : null;
        if (!sand) return;
        if (!holder) holder = new GameObject("BRAXHORN | ślady na piasku").transform;
        if (!mesh) mesh = Build();
        if (!mats.TryGetValue(sand, out var m))
        {
            var sole = new Material(sand) { name = sand.name + " | ubity, ciemniejszy (ślad)" };
            if (sole.HasProperty("_Color")) sole.SetColor("_Color", sole.GetColor("_Color") * new Color(.76f, .74f, .72f, 1f));
            m = new[] { sole, sand };
            mats[sand] = m;
        }
        int idx;
        if (prints.Count < MaxPrints)
        {
            var o = new GameObject("ślad");
            o.transform.SetParent(holder, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            prints.Add(new Print { t = o.transform });
            idx = prints.Count - 1;
        }
        else { idx = next; next = (next + 1) % MaxPrints; }               // the oldest print is reused
        var p = prints[idx];
        var fwd = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
        if (fwd.sqrMagnitude < .1f) fwd = Vector3.ProjectOnPlane(Vector3.forward, hit.normal).normalized;
        float k = transform.lossyScale.y;                                   // the player's own scale (K in the region)
        p.t.position = hit.point;
        p.t.rotation = Quaternion.LookRotation(fwd, hit.normal);
        p.t.localScale = new Vector3(left ? -k : k, k, k);                // mirrored for the left foot
        p.t.GetComponent<MeshRenderer>().sharedMaterials = m;
        p.t.gameObject.SetActive(true);
        p.born = Time.time; p.at = hit.point; p.up = hit.normal;
        prints[idx] = p;
    }

    static void Fade()
    {
        float now = Time.time;
        for (int i = 0; i < prints.Count; i++)
        {
            var p = prints[i];
            if (!p.t || !p.t.gameObject.activeSelf) continue;
            float age = now - p.born;
            if (age >= Life) { p.t.gameObject.SetActive(false); continue; }
            if (age > Life - Settle)
            {
                // settles back into the sand: down by its full height (4.5 cm in model metres) over the last 10 s
                float t = (age - (Life - Settle)) / Settle;
                p.t.position = p.at - p.up * (.045f * t * p.t.lossyScale.y);
            }
        }
    }

    // Footprint in model metres: x across (right foot; mirrored for the left), z along (toes +z), y up. Outline of a
    // 27 x 10 cm shoe sole. Submesh 0: the sole (compressed sand, +10 mm), submesh 1: the ridge (crest +35 mm, outer
    // foot 10 mm under the surface).
    static Mesh Build()
    {
        const int n = 28;
        var outline = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float z = Mathf.Sin(a) * .135f;                                 // -.135 heel .. +.135 toe
            float w = z > 0f ? Mathf.Lerp(.040f, .052f, Mathf.Sin(z / .135f * Mathf.PI * .5f)) : Mathf.Lerp(.040f, .036f, -z / .135f);
            if (z > -.02f && z < .04f) w *= .88f;                           // the arch
            outline[i] = new Vector2(Mathf.Cos(a) * w + (z > 0f ? .006f * z / .135f : 0f), z);
        }
        Vector3 At(Vector2 q, float s, float y) => new Vector3(q.x * s, y, q.y * s);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var sole = new List<int>(); var rim = new List<int>();
        int Add(Vector3 p) { v.Add(p); uv.Add(new Vector2(p.x, p.z) * 4f); return v.Count - 1; }
        int c = Add(new Vector3(.003f, .010f, 0f));
        var inner = new int[n]; var crest = new int[n]; var foot = new int[n];
        for (int i = 0; i < n; i++)
        {
            inner[i] = Add(At(outline[i], 1.00f, .010f));
            crest[i] = Add(At(outline[i], 1.22f, .035f));
            foot[i] = Add(At(outline[i], 1.58f, -.010f));
        }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            sole.AddRange(new[] { c, inner[j], inner[i] });
            rim.AddRange(new[] { inner[i], inner[j], crest[j], inner[i], crest[j], crest[i] });
            rim.AddRange(new[] { crest[i], crest[j], foot[j], crest[i], foot[j], foot[i] });
        }
        var m = new Mesh { name = "BRAXHORN ślad buta na piasku" };
        m.SetVertices(v); m.SetUVs(0, uv); m.subMeshCount = 2;
        m.SetTriangles(sole, 0); m.SetTriangles(rim, 1);
        // face up: fix the winding from the first triangle of each submesh
        for (int s = 0; s < 2; s++)
        {
            var t = m.GetTriangles(s);
            var nrm = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]);
            if (nrm.y < 0f) { for (int i = 0; i < t.Length; i += 3) { int x = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = x; } m.SetTriangles(t, s); }
        }
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

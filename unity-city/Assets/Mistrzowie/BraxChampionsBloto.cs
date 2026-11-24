using UnityEngine;

// THE QUAD TRACK'S MUD AND DUST (owner 24.11.2026 12:57, his pick "Boczne linie i błoto": "pit z błotem, pył i błoto spod
// kół"). The mud pits of the track (BraxChampionsTorKladow: the wet mud's own strip of the riding surface, 0.3 m down) and
// what the quads' wheels throw: the quad (BraxChampionsKlad) asks WBlocie for each wheel on the ground - in the mud its
// tyre holds 55 % and the mud drags at it (rolling resistance 0.25 of the load: deep wet mud) - and calls Wyrzuc as it
// rides: a dry track at speed lifts a dust cloud from the rear wheels, the mud throws clumps back and up.
// The particles: one pool of soft billboards in one mesh (the project has no particle module; the way the fountain does
// it), the dust drifting, growing and fading, the clumps on gravity x K; two draw calls.
[DisallowMultipleComponent]
[DefaultExecutionOrder(5200)]                                    // after the quads moved and the camera was set
public sealed class BraxChampionsBloto : MonoBehaviour
{
    // ---------------------------------------------------------------- set by the builder
    public Vector3[] srodek = new Vector3[0];    // each mud pit: its centre (world, on the riding surface)
    public Vector3[] wzdluz = new Vector3[0];    // its direction along the track (flat, unit)
    public float[] polDl = new float[0], polSzer = new float[0];   // half its length and half its width, world
    public Material pyl, grudki;                 // the dust (transparent, vertex alpha) and the clumps of mud
    public float K = 1f;

    static BraxChampionsBloto ten;
    void OnEnable() { ten = this; }
    void OnDisable() { if (ten == this) ten = null; }

    /// <summary>Is this point (world) in a mud pit?</summary>
    public static bool WBlocie(Vector3 p)
    {
        var t = ten; if (!t) return false;
        for (int i = 0; i < t.srodek.Length; i++)
        {
            var d = p - t.srodek[i]; float a = Vector3.Dot(d, t.wzdluz[i]);
            if (Mathf.Abs(a) > t.polDl[i] || Mathf.Abs(d.y) > 3f * t.K) continue;
            var bok = d - t.wzdluz[i] * a; bok.y = 0f;
            if (bok.sqrMagnitude <= t.polSzer[i] * t.polSzer[i]) return true;
        }
        return false;
    }

    /// <summary>A wheel throws: p where it meets the ground, v the throw (world), mud or dust.</summary>
    public static void Wyrzuc(Vector3 p, Vector3 v, bool mokre)
    {
        var t = ten; if (!t) return;
        (mokre ? t.pBloto : t.pPyl).Dodaj(p, v, t.K, mokre);
    }

    // ---------------------------------------------------------------- the particles
    sealed class Pula
    {
        public readonly int max; public readonly Vector3[] p, v, vv; public readonly float[] wiek, zycie, r0, r1; public readonly Color32[] c;
        public int nast; public Mesh mesh; public Vector2[] uv; public Color32[] kol;
        public Pula(int max)
        {
            this.max = max; p = new Vector3[max]; v = new Vector3[max]; wiek = new float[max]; zycie = new float[max]; r0 = new float[max]; r1 = new float[max];
            c = new Color32[max]; vv = new Vector3[max * 4]; uv = new Vector2[max * 4]; kol = new Color32[max * 4];
            for (int i = 0; i < max; i++) { zycie[i] = 0f; uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(0, 1); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(1, 0); }
        }
        public void Dodaj(Vector3 pos, Vector3 vel, float K, bool mokre)
        {
            int i = nast; nast = (nast + 1) % max;
            p[i] = pos; v[i] = vel; wiek[i] = 0f;
            if (mokre)
            {
                zycie[i] = Random.Range(.7f, 1.1f); float r = Random.Range(.05f, .09f) * K; r0[i] = r; r1[i] = r * .8f;
                byte g = (byte)Random.Range(30, 46); c[i] = new Color32((byte)(g + 12), g, (byte)(g - 8), 255);
            }
            else
            {
                zycie[i] = Random.Range(1.3f, 2.0f); r0[i] = Random.Range(.25f, .4f) * K; r1[i] = Random.Range(1.4f, 2.2f) * K;
                byte g = (byte)Random.Range(150, 176); c[i] = new Color32((byte)(g + 22), g, (byte)(g - 30), 255);
            }
        }
    }
    Pula pPyl, pBloto;
    MeshRenderer rPyl, rBloto;

    void Awake()
    {
        pPyl = new Pula(160); pBloto = new Pula(140);
        var miekki = Miekki();
        rPyl = Warstwa("pył spod kół", pPyl, pyl, miekki);
        rBloto = Warstwa("błoto spod kół", pBloto, grudki, miekki);
    }

    // a soft round dot (the billboards' shape): white, its alpha falling to the edge
    static Texture2D Miekki()
    {
        const int n = 32; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "tor kładów miękka kropka" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r); a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        t.SetPixels32(px); t.Apply(false);
        return t;
    }

    MeshRenderer Warstwa(string nazwa, Pula q, Material m, Texture2D tex)
    {
        var o = new GameObject(nazwa); o.transform.SetParent(transform, false);
        o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.identity;
        q.mesh = new Mesh { name = nazwa }; q.mesh.MarkDynamic();
        var t = new int[q.max * 6];
        for (int i = 0; i < q.max; i++) { int a = i * 4, k = i * 6; t[k] = a; t[k + 1] = a + 1; t[k + 2] = a + 2; t[k + 3] = a; t[k + 4] = a + 2; t[k + 5] = a + 3; }
        var nrm = new Vector3[q.max * 4]; for (int i = 0; i < nrm.Length; i++) nrm[i] = Vector3.up;   // lit as the ground they rise from
        q.mesh.vertices = q.vv; q.mesh.normals = nrm; q.mesh.uv = q.uv; q.mesh.colors32 = q.kol; q.mesh.triangles = t;
        q.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e5f);
        o.AddComponent<MeshFilter>().sharedMesh = q.mesh;
        var r = o.AddComponent<MeshRenderer>();
        if (m) { var kopia = new Material(m) { name = nazwa }; kopia.SetTexture("_MainTex", tex); r.sharedMaterial = kopia; }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        r.enabled = false;
        return r;
    }

    void LateUpdate()
    {
        var cam = Camera.main; if (!cam) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        Krok(pPyl, rPyl, cam, dt, false);
        Krok(pBloto, rBloto, cam, dt, true);
    }

    void Krok(Pula q, MeshRenderer r, Camera cam, float dt, bool mokre)
    {
        if (q == null || !r) return;
        var right = cam.transform.right; var up = cam.transform.up;
        int zywe = 0;
        for (int i = 0; i < q.max; i++)
        {
            int a = i * 4;
            if (q.zycie[i] <= 0f || q.wiek[i] >= q.zycie[i])
            {
                if (q.zycie[i] > 0f) { q.zycie[i] = 0f; }
                q.vv[a] = q.vv[a + 1] = q.vv[a + 2] = q.vv[a + 3] = Vector3.zero; q.kol[a].a = q.kol[a + 1].a = q.kol[a + 2].a = q.kol[a + 3].a = 0;
                continue;
            }
            zywe++;
            q.wiek[i] += dt; float f = Mathf.Clamp01(q.wiek[i] / q.zycie[i]);
            if (mokre) q.v[i] += Vector3.down * (9.81f * K * dt);                       // the clump falls (gravity x K)
            else { q.v[i] *= Mathf.Exp(-2.2f * dt); q.v[i] += Vector3.up * (.35f * K * dt); }   // the dust slows and lifts
            q.p[i] += q.v[i] * dt;
            float rr = Mathf.Lerp(q.r0[i], q.r1[i], mokre ? f : Mathf.Sqrt(f));
            var c = q.c[i]; c.a = (byte)((mokre ? 1f - f * f : .5f * (1f - f) * Mathf.Clamp01(q.wiek[i] * 6f)) * 255f);
            var R = right * rr; var U = up * rr; var p0 = q.p[i];
            q.vv[a] = p0 - R - U; q.vv[a + 1] = p0 - R + U; q.vv[a + 2] = p0 + R + U; q.vv[a + 3] = p0 + R - U;
            q.kol[a] = q.kol[a + 1] = q.kol[a + 2] = q.kol[a + 3] = c;
        }
        r.enabled = zywe > 0;
        if (zywe > 0) { q.mesh.vertices = q.vv; q.mesh.colors32 = q.kol; }
    }
}

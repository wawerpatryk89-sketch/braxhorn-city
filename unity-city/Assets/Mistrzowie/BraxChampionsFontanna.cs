using System.Collections.Generic;
using UnityEngine;

// A fountain's moving water (owner 11.11 19:49: "woda ma lecieć, widzieć jak tryska woda ... realistycznie"). The project
// has no ParticleSystem module, so every frame this rebuilds one small dynamic mesh from BraxChampionsFontannaKlatka:
// streams on real ballistic arcs, spray, ripple rings and splashes where the water lands, a drifting ripple layer on the
// basins. Two runtime material copies of the generator's transparent water (vertex colour on, an atlas / a ripple tile made
// here). Only animates when the camera is within `zasieg`; the arrays are filled by the editor (local, game units).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class BraxChampionsFontanna : MonoBehaviour
{
    public Vector3[] a = new Vector3[0], b = new Vector3[0];
    public float[] h = new float[0], w = new float[0];
    public int[] n = new int[0];
    public bool[] plusk = new bool[0];
    public Vector3[] lustroC = new Vector3[0];
    public float[] lustroR = new float[0];
    public float g = 9.81f * 1.2944f;      // gravity in game units (the world is x K)
    public float lift = .026f;              // ripples 2 cm over the water (zero flicker rule, x K)
    public float zasieg = 180f;
    public float czujnik = 0f;              // > 0: a sensor tap / shower - runs only while the camera is this near
    public Material woda, tafla;

    BraxChampionsFontannaKlatka.Strumien[] s;
    Mesh mesh; Material mw, mt; bool pusty = true;
    readonly List<Vector3> V = new List<Vector3>(4096), N = new List<Vector3>(4096);
    readonly List<Vector2> U = new List<Vector2>(4096); readonly List<Color> C = new List<Color>(4096);
    readonly List<int> T0 = new List<int>(8192), T1 = new List<int>(1024);
    static Texture2D atlas, fale;

    void Start()
    {
        s = new BraxChampionsFontannaKlatka.Strumien[a.Length];
        var lo = Vector3.one * float.MaxValue; var hi = Vector3.one * float.MinValue;
        for (int i = 0; i < a.Length; i++)
        {
            s[i] = new BraxChampionsFontannaKlatka.Strumien { a = a[i], b = b[i], h = h[i], w = w[i], n = n[i], plusk = plusk[i] };
            float top = Mathf.Max(a[i].y, b[i].y) + h[i];
            lo = Vector3.Min(lo, Vector3.Min(a[i], b[i])); hi = Vector3.Max(hi, Vector3.Max(Vector3.Max(a[i], b[i]), new Vector3(a[i].x, top, a[i].z)));
        }
        for (int i = 0; i < lustroC.Length; i++) { lo = Vector3.Min(lo, lustroC[i] - new Vector3(lustroR[i], 0, lustroR[i])); hi = Vector3.Max(hi, lustroC[i] + new Vector3(lustroR[i], .2f, lustroR[i])); }
        mesh = new Mesh { name = "fontanna — płynąca woda" }; mesh.MarkDynamic();
        if (a.Length + lustroC.Length > 0) mesh.bounds = new Bounds((lo + hi) * .5f, hi - lo + Vector3.one * 2f);
        GetComponent<MeshFilter>().sharedMesh = mesh;
        if (!atlas)
        {
            atlas = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "fontanna atlas" };
            atlas.SetPixels32(BraxChampionsFontannaKlatka.Atlas()); atlas.Apply(false, true);
            fale = new Texture2D(64, 64, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "fontanna fale" };
            fale.SetPixels32(BraxChampionsFontannaKlatka.Fale()); fale.Apply(true, true);
        }
        mw = Kopia(woda, atlas, new Color(.90f, .95f, 1f, 1f));
        mt = Kopia(tafla ? tafla : woda, fale, new Color(1f, 1f, 1f, .75f));
        GetComponent<MeshRenderer>().sharedMaterials = new[] { mw, mt };
    }
    static Material Kopia(Material src, Texture2D tex, Color c)
    {
        var m = src ? new Material(src) : new Material(Shader.Find("Braxhorn/CityStaticPBR"));
        m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", Vector2.one); m.SetTextureOffset("_MainTex", Vector2.zero);
        m.SetColor("_Color", c); m.SetFloat("_VertexTint", 1f); m.SetFloat("_Cull", 0f); m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Transparent", 1f); m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f); m.renderQueue = 3001;
        return m;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        float r = czujnik > 0f ? czujnik : zasieg;
        if (cam && (cam.transform.position - transform.position).sqrMagnitude > r * r)
        {
            if (!pusty) { mesh.Clear(); pusty = true; }
            return;
        }
        BraxChampionsFontannaKlatka.Klatka(s, lustroC, lustroR, g, lift, Time.time, V, N, U, C, T0, T1);
        mesh.Clear(false);
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U); mesh.SetColors(C);
        mesh.subMeshCount = 2; mesh.SetTriangles(T0, 0, false); mesh.SetTriangles(T1, 1, false);
        pusty = false;
    }

    void OnDestroy()
    {
        if (mw) Destroy(mw);
        if (mt) Destroy(mt);
        if (mesh) Destroy(mesh);
    }
}

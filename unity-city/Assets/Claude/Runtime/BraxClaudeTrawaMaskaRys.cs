using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO R1 — rysowanie kęp z maski trawnika osiedla bydgoskiego (BraxClaudeTrawaMaska): kawałki 8 × 8 m w zasięgu kamery.
[DefaultExecutionOrder(10000)]
public class BraxClaudeTrawaMaskaRys : MonoBehaviour
{
    public BraxClaudeTrawaMaska maska;
    public Mesh siatka;
    public Material material;
    public float kawalek = 8f, zasieg = 22f, zwolnijPo = 40f;

    readonly Dictionary<long, Matrix4x4[][]> pamiec = new Dictionary<long, Matrix4x4[][]>();
    readonly List<long> doUsuniecia = new List<long>();
    readonly Plane[] plaszczyzny = new Plane[6];
    public int ostatnioKep, kawalkowWPamieci;

    Matrix4x4[][] Policz(int cx, int cz)
    {
        var lista = new List<Matrix4x4>();
        float krok = 1f / Mathf.Sqrt(Mathf.Max(1f, maska.gestosc));
        int n = Mathf.CeilToInt(kawalek / krok);
        float x0 = cx * kawalek, z0 = cz * kawalek;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                uint h = (uint)((cx * 73856093) ^ (cz * 19349663) ^ (i * 83492791) ^ (j * 2654435761u));
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                float px = x0 + (i + (h & 1023u) / 1023f) * krok, pz = z0 + (j + ((h >> 10) & 1023u) / 1023f) * krok;
                if (!maska.Trawa(px, pz)) continue;
                float s = maska.wysokoscKepy / maska.skalaSiatki * (.85f + ((h >> 20) & 255u) / 255f * .35f);
                float rot = ((h >> 4) & 511u) / 511f * 360f;
                lista.Add(Matrix4x4.TRS(new Vector3(px, maska.y - .012f, pz), Quaternion.Euler(0f, rot, 0f), new Vector3(s, s, s)));
            }
        var porcje = new Matrix4x4[(lista.Count + 1022) / 1023][];
        for (int p = 0; p < porcje.Length; p++) porcje[p] = lista.GetRange(p * 1023, Mathf.Min(1023, lista.Count - p * 1023)).ToArray();
        return porcje;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam || !maska || !siatka || !material) return;
        GeometryUtility.CalculateFrustumPlanes(cam, plaszczyzny);
        var p = cam.transform.position;
        int r = Mathf.CeilToInt(zasieg / kawalek), cx0 = Mathf.FloorToInt(p.x / kawalek), cz0 = Mathf.FloorToInt(p.z / kawalek);
        int kep = 0;
        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                int cx = cx0 + dx, cz = cz0 + dz;
                var b = new Bounds(new Vector3((cx + .5f) * kawalek, maska.y + .3f, (cz + .5f) * kawalek), new Vector3(kawalek + 1f, 1f, kawalek + 1f));
                if ((b.center - new Vector3(p.x, b.center.y, p.z)).sqrMagnitude > (zasieg + kawalek) * (zasieg + kawalek)) continue;
                if (!GeometryUtility.TestPlanesAABB(plaszczyzny, b)) continue;
                long key = ((long)cx << 32) ^ (uint)cz;
                if (!pamiec.TryGetValue(key, out var porcje)) { porcje = Policz(cx, cz); pamiec[key] = porcje; }
                foreach (var arr in porcje) { Graphics.DrawMeshInstanced(siatka, 0, material, arr, arr.Length, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off); kep += arr.Length; }
            }
        if (pamiec.Count > 200)
        {
            doUsuniecia.Clear();
            foreach (var kv in pamiec)
            {
                float cx = ((int)(kv.Key >> 32) + .5f) * kawalek, cz = ((int)(uint)kv.Key + .5f) * kawalek;
                if ((cx - p.x) * (cx - p.x) + (cz - p.z) * (cz - p.z) > (zasieg + zwolnijPo) * (zasieg + zwolnijPo)) doUsuniecia.Add(kv.Key);
            }
            foreach (var k in doUsuniecia) pamiec.Remove(k);
        }
        ostatnioKep = kep; kawalkowWPamieci = pamiec.Count;
    }
}

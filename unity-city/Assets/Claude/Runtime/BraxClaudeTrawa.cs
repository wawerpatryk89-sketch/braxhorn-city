using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN R2 — trawa 3D (warstwa 2 trawnika): kępy rysowane instancjami (Graphics.DrawMeshInstanced),
// pogrupowane w kawałki 9 × 9 m, osobno murawa i łąka (inny zasięg: murawa znika w shaderze na 22 m, łąka na 30 m).
// Macierze kawałka liczone dopiero, gdy kawałek wejdzie w zasięg, i zwalniane, gdy oddali się o 45 m poza zasięg —
// w pamięci tylko okolica gracza (cała trawa osiedla to kilkaset tysięcy kęp). Co klatkę: tylko kawałki w zasięgu
// i w polu widzenia kamery; wywołania po ≤ 1023 kęp. Cienie: odbiera, nie rzuca. Zanik z odległością robi shader
// (OsiedleKepyInst). Kolejność 10000: po ustawieniu kamery gracza (BraxGymPlayer.LateUpdate), bez opóźnienia o klatkę.
[DefaultExecutionOrder(10000)]
public class BraxClaudeTrawa : MonoBehaviour
{
    public BraxClaudeTrawaDane dane;
    public Mesh siatka;
    public Material material;
    public float kawalek = 9f, zasiegMurawy = 22f, zasiegLaki = 30f, zwolnijPo = 45f;

    int[][] idxM = new int[0][], idxL = new int[0][];          // kawałek → indeksy kęp w dane.kepy
    Matrix4x4[][][] macM = new Matrix4x4[0][][], macL = new Matrix4x4[0][][];   // kawałek → porcje ≤ 1023 (null = nie policzone)
    Bounds[] granice = new Bounds[0];
    readonly Plane[] plaszczyzny = new Plane[6];
    public int ostatnioKep, ostatnioWywolan, kawalkowWPamieci;

    void Awake()
    {
        if (!dane || dane.kepy == null) return;
        var cele = new Dictionary<long, int>(); var m = new List<List<int>>(); var l = new List<List<int>>(); var g = new List<Bounds>();
        float granica = dane.granicaLaki;
        for (int i = 0; i < dane.kepy.Length; i++)
        {
            var k = dane.kepy[i];
            int cx = Mathf.FloorToInt(k.x / kawalek), cz = Mathf.FloorToInt(k.y / kawalek);
            long key = ((long)cx << 32) ^ (uint)cz;
            if (!cele.TryGetValue(key, out var c))
            {
                cele[key] = c = g.Count; m.Add(new List<int>()); l.Add(new List<int>());
                g.Add(new Bounds(new Vector3((cx + .5f) * kawalek, .2f, (cz + .5f) * kawalek), new Vector3(kawalek + 2f, 1.2f, kawalek + 2f)));
            }
            (k.w * dane.wysokosc > granica ? l[c] : m[c]).Add(i);
        }
        idxM = new int[g.Count][]; idxL = new int[g.Count][];
        for (int c = 0; c < g.Count; c++) { idxM[c] = m[c].ToArray(); idxL[c] = l[c].ToArray(); }
        macM = new Matrix4x4[g.Count][][]; macL = new Matrix4x4[g.Count][][];
        granice = g.ToArray();
    }

    // Macierze kawałka: przechył ±6° z ziarna = indeks kępy (zawsze ten sam po ponownym policzeniu).
    Matrix4x4[][] Policz(int[] idx)
    {
        var porcje = new Matrix4x4[(idx.Length + 1022) / 1023][];
        bool maY = dane.y != null && dane.y.Length == dane.kepy.Length;
        for (int p = 0; p < porcje.Length; p++)
        {
            int n = Mathf.Min(1023, idx.Length - p * 1023); var arr = new Matrix4x4[n];
            for (int j = 0; j < n; j++)
            {
                int i = idx[p * 1023 + j]; var k = dane.kepy[i];
                uint h = (uint)i * 2654435761u; float tx = ((h & 1023u) / 1023f - .5f) * 12f, tz = (((h >> 10) & 1023u) / 1023f - .5f) * 12f;
                float s = k.w * dane.wysokosc;
                arr[j] = Matrix4x4.TRS(new Vector3(k.x, maY ? dane.y[i] : 0f, k.y), Quaternion.Euler(tx, k.z, tz), new Vector3(s, s, s));
            }
            porcje[p] = arr;
        }
        return porcje;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (!cam || !siatka || !material || granice.Length == 0) return;
        GeometryUtility.CalculateFrustumPlanes(cam, plaszczyzny);
        Vector3 p = cam.transform.position; float r = kawalek * .75f;   // promień kawałka (pół przekątnej 0.707 × bok)
        float zm2 = (zasiegMurawy + r) * (zasiegMurawy + r), zl2 = (zasiegLaki + r) * (zasiegLaki + r), zw2 = (zasiegLaki + zwolnijPo) * (zasiegLaki + zwolnijPo);
        int kep = 0, wyw = 0, wPam = 0;
        for (int i = 0; i < granice.Length; i++)
        {
            var c = granice[i].center; float dx = c.x - p.x, dz = c.z - p.z, d2 = dx * dx + dz * dz;
            if (d2 > zw2) { macM[i] = null; macL[i] = null; continue; }
            if (macM[i] != null || macL[i] != null) wPam++;
            if (d2 > zl2 || !GeometryUtility.TestPlanesAABB(plaszczyzny, granice[i])) continue;
            if (d2 <= zm2 && idxM[i].Length > 0)
            {
                if (macM[i] == null) macM[i] = Policz(idxM[i]);
                foreach (var arr in macM[i]) { Graphics.DrawMeshInstanced(siatka, 0, material, arr, arr.Length, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off); kep += arr.Length; wyw++; }
            }
            if (idxL[i].Length > 0)
            {
                if (macL[i] == null) macL[i] = Policz(idxL[i]);
                foreach (var arr in macL[i]) { Graphics.DrawMeshInstanced(siatka, 0, material, arr, arr.Length, null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off); kep += arr.Length; wyw++; }
            }
        }
        ostatnioKep = kep; ostatnioWywolan = wyw; kawalkowWPamieci = wPam;
    }
}

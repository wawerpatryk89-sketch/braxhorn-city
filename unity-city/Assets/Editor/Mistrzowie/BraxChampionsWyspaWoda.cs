using System;
using System.Collections.Generic;
using UnityEngine;

// The villa island's water (BraxChampionsWyspaMorze), plain arithmetic the region's tools run without Unity
// (unity-tools/mistrzowie/morze_sim --wyspa):
//  - Siatka: a grid at the sea level over the island's patch (cells on dry land left out), its outer band a fan to the
//    patch's four corners - the far sea round it (BraxChampionsHorizon) meets it at those corners only: no vertex of one in
//    the middle of the other's edge, so no crack and no sparkle on the line;
//  - Pole: its shore field from its ground (BraxChampionsMorzeBrzeg), the bed at the patch's rim taken down to the open
//    sea's depth (Rant: no colour step to the far sea round it).
public static class BraxChampionsWyspaWoda
{
    static float Smooth(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3 - 2 * t); }

    /// <summary>The water over the patch x0..x1, z0..z1 (nominal metres; x k = game units) at height y (game units): a grid
    /// of krok over the patch less its outer band pas, the band a fan to the corners. sucho(x, z): the ground there is dry
    /// land (a cell with its four corners dry is left out). Returns the number of cells left out.</summary>
    public static int Siatka(float x0, float z0, float x1, float z1, float krok, float pas, float k, float y, Func<float, float, bool> sucho,
                             List<Vector3> v, List<int> t)
    {
        v.Clear(); t.Clear();
        float ix0 = x0 + pas, iz0 = z0 + pas;
        int mx = Mathf.RoundToInt((x1 - pas - ix0) / krok), mz = Mathf.RoundToInt((z1 - pas - iz0) / krok);
        for (int i = 0; i <= mx; i++)
            for (int j = 0; j <= mz; j++) v.Add(new Vector3((ix0 + i * krok) * k, y, (iz0 + j * krok) * k));
        int Id(int i, int j) => i * (mz + 1) + j;
        void Tri(int a, int b, int c)
        {
            // face up (x east, z north)
            if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).y < 0f) { t.Add(a); t.Add(c); t.Add(b); } else { t.Add(a); t.Add(b); t.Add(c); }
        }
        var dry = new bool[(mx + 1) * (mz + 1)];
        for (int i = 0; i <= mx; i++) for (int j = 0; j <= mz; j++) dry[Id(i, j)] = sucho(ix0 + i * krok, iz0 + j * krok);
        int left = 0;
        for (int i = 0; i < mx; i++)
            for (int j = 0; j < mz; j++)
            {
                if (dry[Id(i, j)] && dry[Id(i + 1, j)] && dry[Id(i, j + 1)] && dry[Id(i + 1, j + 1)]) { left++; continue; }
                Tri(Id(i, j), Id(i, j + 1), Id(i + 1, j)); Tri(Id(i, j + 1), Id(i + 1, j + 1), Id(i + 1, j));
            }
        int sw = v.Count; v.Add(new Vector3(x0 * k, y, z0 * k));
        int se = v.Count; v.Add(new Vector3(x1 * k, y, z0 * k));
        int ne = v.Count; v.Add(new Vector3(x1 * k, y, z1 * k));
        int nw = v.Count; v.Add(new Vector3(x0 * k, y, z1 * k));
        // each side of the band: the grid's edge fanned to the two corners at its ends
        void Fan(List<int> e, int a, int b)
        {
            int mid = e.Count / 2;
            for (int q = 0; q < mid; q++) Tri(e[q], e[q + 1], a);
            for (int q = mid; q + 1 < e.Count; q++) Tri(e[q], e[q + 1], b);
            Tri(e[mid], a, b);
        }
        var south = new List<int>(); var north = new List<int>(); var west = new List<int>(); var east = new List<int>();
        for (int i = 0; i <= mx; i++) { south.Add(Id(i, 0)); north.Add(Id(i, mz)); }
        for (int j = 0; j <= mz; j++) { west.Add(Id(0, j)); east.Add(Id(mx, j)); }
        Fan(south, sw, se); Fan(north, nw, ne); Fan(west, sw, nw); Fan(east, se, ne);
        return left;
    }

    /// <summary>The island's shore field over its patch (nominal metres; n x n texels, game units inside): its ground grunt(x, z)
    /// (nominal metres) on the krok grid, two triangles a cell (its pad and pool count as land); the bed within rantJ (game
    /// units) of the rim brought down to deepY (game units).</summary>
    public static BraxChampionsMorzeBrzeg Pole(float x0, float z0, float x1, float z1, float krok, float k, float sea, int n, Func<float, float, float> grunt,
                                              float deepY, float rantJ)
    {
        var pole = new BraxChampionsMorzeBrzeg(x0 * k, z0 * k, x1 * k, z1 * k, n, n, sea * k);
        int nx = Mathf.RoundToInt((x1 - x0) / krok), nz = Mathf.RoundToInt((z1 - z0) / krok);
        var gh = new float[(nx + 1) * (nz + 1)];
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++) gh[i * (nz + 1) + j] = grunt(x0 + i * krok, z0 + j * krok);
        Vector3 G(int i, int j) => new Vector3((x0 + i * krok) * k, gh[i * (nz + 1) + j] * k, (z0 + j * krok) * k);
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++) { pole.Piasek(G(i, j), G(i, j + 1), G(i + 1, j)); pole.Piasek(G(i, j + 1), G(i + 1, j + 1), G(i + 1, j)); }
        pole.Licz();
        Rant(pole, deepY, rantJ);
        return pole;
    }

    /// <summary>The field's bed within pasJ (game units) of its rim brought down to deepY (game units), never up.</summary>
    public static void Rant(BraxChampionsMorzeBrzeg pole, float deepY, float pasJ)
    {
        for (int iz = 0; iz < pole.h; iz++)
            for (int ix = 0; ix < pole.w; ix++)
            {
                float r = Mathf.Min(Mathf.Min(ix + .5f, pole.w - ix - .5f) * pole.cw, Mathf.Min(iz + .5f, pole.h - iz - .5f) * pole.ch);
                int q = iz * pole.w + ix;
                pole.dno[q] = Mathf.Min(pole.dno[q], Mathf.Lerp(deepY, pole.dno[q], Smooth(0f, pasJ, r)));
            }
    }
}

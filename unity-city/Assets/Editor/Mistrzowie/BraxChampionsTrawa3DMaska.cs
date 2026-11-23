using System;
using System.Collections.Generic;
using UnityEngine;

// Owner 23.11 ("trawa ... nie płaska", "długa", "to jest dobra trawa"): where the 3D grass may grow, worked out from the
// finished scene's triangles (BraxChampionsTrawa3DBuild gathers them; this part is plain arithmetic, the region's tools
// run it on the real lawn without Unity: unity-tools/mistrzowie/trawa_sim).
//   - a lawn cell (25 cm) is one whose centre lies on an upward lawn triangle; its height is that triangle's there;
//   - a cell is blocked when anything else stands in it near that lawn: any triangle reaching from 5 mm over the lawn to
//     the obstacle height (kerbs, edgings, paths laid on the grass, benches, beds, trunks, walls, steps, flowers ...);
//     a blocked cell is found conservatively (the triangle touches the cell at all), so no blade grows through a slab;
//   - the soil under the trees (the lawn shader's soil mask) is no grass either;
//   - a cell keeps its grass only when its four neighbours are lawn too: one cell (25 cm) of flat lawn is left at every
//     edge, the clumps' blades reach over it, not through the kerb.
public sealed class BraxChampionsTrawa3DMaska
{
    public const int N = BraxChampionsTrawa3DDane.Komorek;
    public readonly float kawalek, komorka, wysokosc;
    public readonly float x0, z0;
    public readonly int nx, nz;

    public sealed class Latka
    {
        public int k, yq; public float y; public bool gleba;
        public readonly uint[] trawnik = new uint[N], blok = new uint[N];
        public uint[] wynik;
    }
    readonly Dictionary<long, Latka> latki = new Dictionary<long, Latka>();
    readonly Dictionary<int, List<Latka>> poKawalku = new Dictionary<int, List<Latka>>();
    public int trojkatyTrawnika, trojkatyPrzeszkod, komorekTrawnika, komorekZablokowanych, komorekGleby, komorekKrawedzi, komorekTrawy;
    public float minY = float.MaxValue, maxY = float.MinValue;

    static long Klucz(int k, int yq) => ((long)k << 24) ^ (uint)(yq + (1 << 23));

    // the grid: whole pieces around the lawn's extent (one piece of margin)
    public BraxChampionsTrawa3DMaska(float minx, float minz, float maxx, float maxz, float kawalek = 8f, float wysokosc = .8f)
    {
        this.kawalek = kawalek; komorka = kawalek / N; this.wysokosc = wysokosc;
        x0 = Mathf.Floor(minx / kawalek) * kawalek - kawalek; z0 = Mathf.Floor(minz / kawalek) * kawalek - kawalek;
        nx = Mathf.CeilToInt((maxx - x0) / kawalek) + 1; nz = Mathf.CeilToInt((maxz - z0) / kawalek) + 1;
    }

    public IEnumerable<Latka> Latki => latki.Values;
    public int LiczbaLatek => latki.Count;

    Latka Daj(int k, int yq, float y)
    {
        long key = Klucz(k, yq);
        if (latki.TryGetValue(key, out var l)) return l;
        l = new Latka { k = k, yq = yq, y = y };
        latki.Add(key, l);
        if (!poKawalku.TryGetValue(k, out var lista)) poKawalku.Add(k, lista = new List<Latka>());
        lista.Add(l);
        return l;
    }

    // the x extent of the triangle (xz) inside the strip z0..z1 (z0 == z1: on the line); false if it does not reach it
    static bool PasX(Vector2 a, Vector2 b, Vector2 c, float za, float zb, out float xmin, out float xmax)
    {
        float lo = float.MaxValue, hi = float.MinValue;
        void P(Vector2 p) { if (p.y >= za && p.y <= zb) { if (p.x < lo) lo = p.x; if (p.x > hi) hi = p.x; } }
        void E(Vector2 p, Vector2 q, float zz)
        {
            if ((p.y - zz) * (q.y - zz) < 0f) { float x = p.x + (q.x - p.x) * (zz - p.y) / (q.y - p.y); if (x < lo) lo = x; if (x > hi) hi = x; }
        }
        P(a); P(b); P(c);
        E(a, b, za); E(b, c, za); E(c, a, za);
        if (zb != za) { E(a, b, zb); E(b, c, zb); E(c, a, zb); }
        xmin = lo; xmax = hi;
        return lo <= hi;
    }

    public void Trawnik(Vector3 a, Vector3 b, Vector3 c, bool gleba)
    {
        var n = Vector3.Cross(b - a, c - a);
        if (Mathf.Abs(n.y) < 1e-9f) return;
        trojkatyTrawnika++;
        Vector2 A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z), C = new Vector2(c.x, c.z);
        int gz0 = Mathf.CeilToInt((Mathf.Min(A.y, Mathf.Min(B.y, C.y)) - z0) / komorka - .5f);
        int gz1 = Mathf.FloorToInt((Mathf.Max(A.y, Mathf.Max(B.y, C.y)) - z0) / komorka - .5f);
        for (int gz = Mathf.Max(0, gz0); gz <= Mathf.Min(nz * N - 1, gz1); gz++)
        {
            float zc = z0 + (gz + .5f) * komorka;
            if (!PasX(A, B, C, zc, zc, out float xa, out float xb)) continue;
            int gx0 = Mathf.Max(0, Mathf.CeilToInt((xa - x0) / komorka - .5f)), gx1 = Mathf.Min(nx * N - 1, Mathf.FloorToInt((xb - x0) / komorka - .5f));
            for (int gx = gx0; gx <= gx1; gx++)
            {
                float xc = x0 + (gx + .5f) * komorka;
                float y = a.y - (n.x * (xc - a.x) + n.z * (zc - a.z)) / n.y;
                int yq = Mathf.RoundToInt(y * 100f);
                var l = Daj((gz / N) * nx + gx / N, yq, y);
                int cx = gx % N, cz = gz % N;
                if ((l.trawnik[cz] & (1u << cx)) == 0) { l.trawnik[cz] |= 1u << cx; komorekTrawnika++; }
                l.gleba |= gleba;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
        }
    }

    // a triangle of anything else: blocks every cell it touches of each lawn it stands on (from 5 mm over it up to wysokosc)
    public void Przeszkoda(Vector3 a, Vector3 b, Vector3 c)
    {
        float ymin = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), ymax = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        if (ymax <= minY + .005f || ymin >= maxY + wysokosc) return;
        Vector2 A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z), C = new Vector2(c.x, c.z);
        int gz0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(A.y, Mathf.Min(B.y, C.y)) - z0) / komorka));
        int gz1 = Mathf.Min(nz * N - 1, Mathf.FloorToInt((Mathf.Max(A.y, Mathf.Max(B.y, C.y)) - z0) / komorka));
        bool any = false;
        for (int gz = gz0; gz <= gz1; gz++)
        {
            float za = z0 + gz * komorka;
            if (!PasX(A, B, C, za, za + komorka, out float xa, out float xb)) continue;
            int gx0 = Mathf.Max(0, Mathf.FloorToInt((xa - x0) / komorka)), gx1 = Mathf.Min(nx * N - 1, Mathf.FloorToInt((xb - x0) / komorka));
            int cz = gz % N, iz = gz / N;
            for (int ix = gx0 / N; ix <= gx1 / N; ix++)
            {
                if (!poKawalku.TryGetValue(iz * nx + ix, out var lista)) continue;
                int c0 = Mathf.Max(gx0, ix * N) - ix * N, c1 = Mathf.Min(gx1, ix * N + N - 1) - ix * N;
                uint maska = (c1 == 31 ? 0xFFFFFFFFu : (1u << (c1 + 1)) - 1u) & ~((1u << c0) - 1u);
                foreach (var l in lista)
                    if (ymin < l.y + wysokosc && ymax > l.y + .005f) { l.blok[cz] |= maska; any = true; }
            }
        }
        if (any) trojkatyPrzeszkod++;
    }

    // the soil under the trees: ziemia(x, z) true = bare soil at that cell's centre (only lawns painted with the soil mask)
    public void Gleba(Func<float, float, bool> ziemia)
    {
        foreach (var l in latki.Values)
        {
            if (!l.gleba) continue;
            int ix = l.k % nx, iz = l.k / nx;
            for (int cz = 0; cz < N; cz++)
            {
                uint t = l.trawnik[cz] & ~l.blok[cz];
                if (t == 0) continue;
                for (int cx = 0; cx < N; cx++)
                    if ((t & (1u << cx)) != 0 && ziemia(x0 + (ix * N + cx + .5f) * komorka, z0 + (iz * N + cz + .5f) * komorka))
                    { l.blok[cz] |= 1u << cx; komorekGleby++; }
            }
        }
    }

    // the lawn of a piece's row at about this height (within 2 cm: a gently sloping lawn is one lawn across its patches)
    uint Wiersz(int k, int yq, int cz)
    {
        if (k < 0 || !poKawalku.TryGetValue(k, out var lista)) return 0u;
        uint r = 0u;
        foreach (var l in lista) if (Math.Abs(l.yq - yq) <= 2) r |= l.trawnik[cz] & ~l.blok[cz];
        return r;
    }

    // grass = lawn, not blocked, with its four neighbours lawn too (across the pieces' edges)
    public void Zakoncz()
    {
        foreach (var l in latki.Values)
        {
            int ix = l.k % nx, iz = l.k / nx;
            int kl = ix > 0 ? l.k - 1 : -1, kp = ix < nx - 1 ? l.k + 1 : -1, kd = iz > 0 ? l.k - nx : -1, kg = iz < nz - 1 ? l.k + nx : -1;
            l.wynik = new uint[N];
            for (int cz = 0; cz < N; cz++)
            {
                uint r = l.trawnik[cz] & ~l.blok[cz];
                if (r == 0) continue;
                uint tu = Wiersz(l.k, l.yq, cz);
                uint lewy = (tu << 1) | (Wiersz(kl, l.yq, cz) >> 31);
                uint prawy = (tu >> 1) | ((Wiersz(kp, l.yq, cz) & 1u) << 31);
                uint dol = cz > 0 ? Wiersz(l.k, l.yq, cz - 1) : Wiersz(kd, l.yq, N - 1);
                uint gora = cz < N - 1 ? Wiersz(l.k, l.yq, cz + 1) : Wiersz(kg, l.yq, 0);
                l.wynik[cz] = r & lewy & prawy & dol & gora;
            }
        }
        komorekZablokowanych = 0; komorekKrawedzi = 0; komorekTrawy = 0;
        foreach (var l in latki.Values)
            for (int cz = 0; cz < N; cz++)
            {
                komorekZablokowanych += Bity(l.trawnik[cz] & l.blok[cz]);
                komorekKrawedzi += Bity(l.trawnik[cz] & ~l.blok[cz] & ~l.wynik[cz]);
                komorekTrawy += Bity(l.wynik[cz]);
            }
        komorekZablokowanych -= komorekGleby;
    }

    static int Bity(uint v) { int n = 0; while (v != 0) { v &= v - 1; n++; } return n; }

    // into the runtime data: patches by piece then height; a full patch has no bits, an empty one is left out
    public int Zapisz(BraxChampionsTrawa3DDane d, out int pelne, out int czesciowe)
    {
        var lista = new List<Latka>();
        foreach (var l in latki.Values) { bool cos = false; for (int cz = 0; cz < N && !cos; cz++) cos = l.wynik[cz] != 0; if (cos) lista.Add(l); }
        lista.Sort((p, q) => p.k != q.k ? p.k.CompareTo(q.k) : p.yq.CompareTo(q.yq));
        var bity = new List<byte>(); pelne = 0; czesciowe = 0;
        d.x0 = x0; d.z0 = z0; d.kawalek = kawalek; d.nx = nx; d.nz = nz;
        d.latkaKawalek = new int[lista.Count]; d.latkaY = new float[lista.Count]; d.latkaBity = new int[lista.Count];
        for (int i = 0; i < lista.Count; i++)
        {
            var l = lista[i];
            d.latkaKawalek[i] = l.k; d.latkaY[i] = l.y;
            bool full = true; for (int cz = 0; cz < N && full; cz++) full = l.wynik[cz] == 0xFFFFFFFFu;
            if (full) { d.latkaBity[i] = -1; pelne++; continue; }
            d.latkaBity[i] = bity.Count; czesciowe++;
            for (int cz = 0; cz < N; cz++) for (int j = 0; j < 4; j++) bity.Add((byte)(l.wynik[cz] >> (8 * j)));
        }
        d.bity = bity.ToArray();
        return lista.Count;
    }
}

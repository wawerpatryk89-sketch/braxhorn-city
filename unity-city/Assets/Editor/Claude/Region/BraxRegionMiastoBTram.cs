using System;
using System.Collections.Generic;
using UnityEngine;

// BRAXHORN CITY — T1: jedna zamknieta linia po ulicach nowych dzielnic.
// Tramwajowa -> C0 -> Nowojorska -> C3 -> Tramwajowa; zadnego biegu wstecznego.
// R=26 m w czterech lukach; ustawienie tak, by dlugi model CAF nie dotykal
// zinwentaryzowanych bryl nowych kwartalow (ostateczna kontrola w Unity/Android).
public static partial class BraxRegionMiastoB
{
    const float T1R = 26f;
    static Vector3[] t1Trasa;

    static Vector3[] NdTrasaT1()
    {
        if (t1Trasa != null && t1Trasa.Length > 100) return t1Trasa;
        float ex = NC[0] + AH * .5f, wx = NC[3] - AH * .5f;
        float sz = NS1 - AH * .5f, nz = NN1 - AH * .5f;
        float rad = T1R;
        var q = new List<Vector3>(1400);
        void Prosta(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(d / 1.5f));
            for (int i = 0; i < n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                if (q.Count == 0 || Vector3.Distance(q[q.Count - 1], new Vector3(p.x, YA, p.y)) > .04f)
                    q.Add(new Vector3(p.x, YA, p.y));
            }
        }
        void Luk(float x, float z, float a, float b)
        {
            int n = Mathf.CeilToInt(Mathf.Abs(b - a) * rad / 1.5f);
            for (int i = 0; i < n; i++)
            {
                float k = Mathf.Lerp(a, b, i / (float)n);
                var p = new Vector3(x + rad * Mathf.Cos(k), YA, z + rad * Mathf.Sin(k));
                if (q.Count == 0 || Vector3.Distance(q[q.Count - 1], p) > .04f) q.Add(p);
            }
        }
        // Południe: Tramwajowa -> wschód.
        Prosta(new Vector2(wx + rad, sz), new Vector2(ex - rad, sz));
        Luk(ex - rad, sz + rad, -Mathf.PI * .5f, 0f);
        // Wschód: C0 -> północ.
        Prosta(new Vector2(ex, sz + rad), new Vector2(ex, nz - rad));
        Luk(ex - rad, nz - rad, 0f, Mathf.PI * .5f);
        // Północ: Nowojorska -> zachód; pas południowy unika fasad rzędu B.
        Prosta(new Vector2(ex - rad, nz), new Vector2(wx + rad, nz));
        Luk(wx + rad, nz - rad, Mathf.PI * .5f, Mathf.PI);
        // Zachód: C3 -> południe.
        Prosta(new Vector2(wx, nz - rad), new Vector2(wx, sz + rad));
        Luk(wx + rad, sz + rad, Mathf.PI, Mathf.PI * 1.5f);
        t1Trasa = q.ToArray();
        return t1Trasa;
    }

    static int NdIndeksTram(Vector3[] punkty, float x, float z)
    {
        int best = 0; float dd = float.MaxValue;
        var q = new Vector3(x, YA, z);
        for (int i = 0; i < punkty.Length; i++)
        {
            float d = (punkty[i] - q).sqrMagnitude;
            if (d < dd) { dd = d; best = i; }
        }
        return best;
    }

    // Wstawki stalowej główki i rowka 13 mm nad jezdnią.
    // Nie budujemy drugiego asfaltu na starej nawierzchni: jego jedynym
    // właścicielem pozostaje BudujAsfalt. Stal to fizyczna część toru,
    // a nie nakładana na stare fasady nowa tekstura.
    static void NdSzynyTramT1()
    {
        var trasa = NdTrasaT1();
        float gauge = Rozstaw * K;
        var metal = BraxCityMaterials.Surface("T1: stal szynowa szlifowana",
            new Color(.57f, .59f, .61f), .25f, .80f);
        var rowek = BraxCityMaterials.Surface("T1: rowek szyny",
            new Color(.10f, .105f, .11f), .90f, .18f);
        var steel = new BraxRegionMeble.Bud();
        var dark = new BraxRegionMeble.Bud();
        Vector3 P(Vector3 v, float y) => new Vector3(v.x, y, v.z);
        void Pas(BraxRegionMeble.Bud b, Vector3 p0, Vector3 p1, Vector3 side,
                 float along, float width, float y)
        {
            var a = P(p0 + side * (along - width * .5f), y);
            var b0 = P(p1 + side * (along - width * .5f), y);
            var b1 = P(p1 + side * (along + width * .5f), y);
            var a1 = P(p0 + side * (along + width * .5f), y);
            b.Quad(a, b0, b1, a1);
        }
        int cnt = 0;
        for (int i = 0; i < trasa.Length; i++)
        {
            var a = trasa[i]; var b = trasa[(i + 1) % trasa.Length];
            var dir = b - a; dir.y = 0;
            if (dir.sqrMagnitude < .02f) continue;
            var side = Vector3.Cross(Vector3.up, dir.normalized);
            foreach (float off in new[] { -gauge * .5f, gauge * .5f })
            {
                Pas(dark, a, b, side, off, .115f * K, YA + .003f);
                Pas(steel, a, b, side, off, .070f * K, YA + .015f);
            }
            // Osobne meshe co ~120m: zwiedzanie Android nie renderuje calych 1.2km naraz.
            if (++cnt % 80 == 0 || i == trasa.Length - 1)
            {
                steel.Obiekt("T1: szyny jezdne odcinek " + cnt / 80, root, metal, false, false);
                dark.Obiekt("T1: rowki szyn odcinek " + cnt / 80, root, rowek, false, false);
                steel = new BraxRegionMeble.Bud(); dark = new BraxRegionMeble.Bud();
            }
        }
        NdLog("TRAM_T1_TRASA geometria zamknieta: punkty=" + trasa.Length +
              ", promien lukow=" + T1R.ToString("F1") +
              " m, szyny bez podwojnego asfaltu, podzielone na male paczki do cullingu");
    }
}

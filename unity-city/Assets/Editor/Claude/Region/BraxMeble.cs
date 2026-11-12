using System.Collections.Generic;
using UnityEngine;

// MEBLE R1 — miękkie i zaokrąglone (12.11, właściciel 14:04–14:05: „Zakaz poduszek kwadratowych”, „czemu nie korzystasz
// z zasobów kolegi, które stworzył, poobijał”). Kopia (nie edycja) wspólnych rozwiązań z Miasta Mistrzów:
//   • BraxChampionsHotelSobczakT03.Soft — superkwadryka na poduszki, materace, kołdry (właściciel 9.11 23:02: „poduszki mogą być
//     zaokrąglone, nie muszą być kwadratowe”); tu z gładkimi normalnymi (uśrednione w wierzchołkach), bez kantów;
//   • BraxChampionsMeshBatch.RoundedBox — bryła z KAŻDĄ krawędzią i narożnikiem zaokrąglonym (właściciel 10.11 20:45: „nie rób
//     wszystkich kwadratowych, tylko zaokrąglone”), gładkie normalne;
//   • BraxChampionsG1Meble.HBed / HSofa / HDiningTable / HDiningChair / HNightstand / HTableLamp — łóżko z kołdrą opadającą
//     na boki, wywinięciem i pledem w nogach, poduszki do spania i ozdobne, zagłówek pikowany kanałami; sofa z wałkami
//     podłokietników, osobnymi poduchami siedziska i oparcia; stół z oskrzyniem; krzesło tapicerowane; szafka nocna z lampką.
// Działa na BraxNabrzeze.Siatka (UV w metrach świata, materiały Poly Haven). Wymiary nominalne (m) × skala ramy s (w R1: K).
internal static class BraxMeble
{
    /// <summary>Rama mebla: c = podłoga na środku PRZEDNIEJ krawędzi, przod = kierunek, w który mebel „patrzy”;
    /// P(a, h, d): a w prawo (patrząc od frontu), h w górę, d w głąb od frontu (m nominalne × s).</summary>
    internal struct Rama
    {
        public Vector3 c, f, u; public Quaternion rot; public float s;
        public Rama(Vector3 c, Vector3 przod, float s)
        {
            przod.y = 0; przod.Normalize(); this.c = c; f = przod; u = Vector3.Cross(Vector3.up, przod).normalized;
            rot = Quaternion.LookRotation(przod, Vector3.up); this.s = s;
        }
        public Vector3 P(float a, float h, float d) => c + (u * a + Vector3.up * h - f * d) * s;
        public Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * s;
        /// <summary>Obrót płaskiej rzeczy pochylonej do tyłu o kąt (górą od frontu), z przechyłem bocznym.</summary>
        public Quaternion Pochyl(float stopnie, float przechyl = 0f) => rot * Quaternion.Euler(-stopnie, 0f, przechyl);
    }

    static Vector2 Rzut(Vector3 p, Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
        if (ax > az) return new Vector2(p.z, p.y);
        return new Vector2(p.x, p.y);
    }
    static int V(BraxNabrzeze.Siatka s, Vector3 p, Vector3 n) { s.v.Add(p); s.n.Add(n); s.uv.Add(Rzut(p, n)); s.c.Add(Color.white); return s.v.Count - 1; }

    /// <summary>Bryła z zaokrąglonymi krawędziami i narożnikami (promień r), gładkie normalne; seg = kroki na ćwierć łuku.</summary>
    public static void Zaokr(BraxNabrzeze.Siatka s, Vector3 c, Vector3 size, float r, Quaternion rot, int seg = 3)
    {
        var h = size * .5f;
        r = Mathf.Min(r, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .98f);
        if (r <= 1e-4f) { s.Box(c, size, rot); return; }
        float[] Os(float e)
        {
            var l = new List<float>();
            for (int i = 0; i <= seg; i++) l.Add(-e + r * i / seg);
            for (int i = 0; i <= seg; i++) { float v = e - r + r * i / seg; if (v > l[l.Count - 1] + 1e-5f) l.Add(v); }
            return l.ToArray();
        }
        float[] ax = Os(h.x), ay = Os(h.y), az = Os(h.z);
        var wn = h - Vector3.one * r;
        void Map(Vector3 q, out Vector3 pos, out Vector3 nrm)
        {
            var k = new Vector3(Mathf.Clamp(q.x, -wn.x, wn.x), Mathf.Clamp(q.y, -wn.y, wn.y), Mathf.Clamp(q.z, -wn.z, wn.z));
            var d = q - k; float l = Mathf.Max(d.magnitude, 1e-6f);
            nrm = rot * (d / l); pos = c + rot * (k + d * (r / l));
        }
        void Sciana(float[] us, float[] vs, System.Func<float, float, Vector3> P)
        {
            for (int i = 0; i + 1 < us.Length; i++)
                for (int j = 0; j + 1 < vs.Length; j++)
                {
                    Map(P(us[i], vs[j]), out var a, out var na); Map(P(us[i + 1], vs[j]), out var b, out var nb);
                    Map(P(us[i + 1], vs[j + 1]), out var cc, out var nc); Map(P(us[i], vs[j + 1]), out var d, out var nd);
                    var fn = (na + nb + nc + nd).normalized;
                    int i0, i1, i2, i3;
                    if (Vector3.Dot(Vector3.Cross(cc - a, d - b), fn) > 0) { i0 = V(s, a, na); i1 = V(s, b, nb); i2 = V(s, cc, nc); i3 = V(s, d, nd); }
                    else { i0 = V(s, d, nd); i1 = V(s, cc, nc); i2 = V(s, b, nb); i3 = V(s, a, na); }
                    s.t.Add(i0); s.t.Add(i1); s.t.Add(i2); s.t.Add(i0); s.t.Add(i2); s.t.Add(i3);
                }
        }
        Sciana(ay, az, (y, z) => new Vector3(h.x, y, z)); Sciana(ay, az, (y, z) => new Vector3(-h.x, y, z));
        Sciana(ax, az, (x, z) => new Vector3(x, h.y, z)); Sciana(ax, az, (x, z) => new Vector3(x, -h.y, z));
        Sciana(ax, ay, (x, y) => new Vector3(x, y, h.z)); Sciana(ax, ay, (x, y) => new Vector3(x, y, -h.z));
    }

    /// <summary>Kształt miękki (superkwadryka): poduszka, materac, kołdra, poducha. size = (szerokość x, GRUBOŚĆ y, długość z)
    /// przed obrotem q; brzeg — zaokrąglenie przez grubość (1 = w pełni okrągły), naroze — zarys (małe = prostokąt
    /// z zaokrąglonymi rogami, 1 = elipsa). Gładkie normalne uśrednione w wierzchołkach.</summary>
    public static void Miekki(BraxNabrzeze.Siatka s, Vector3 c, Vector3 size, Quaternion q, float brzeg, float naroze, int nu = 24, int nv = 10)
    {
        float C(float w, float e) => Mathf.Sign(Mathf.Cos(w)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(w)), e);
        float S(float w, float e) => Mathf.Sign(Mathf.Sin(w)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(w)), e);
        var pts = new Vector3[nu + 1, nv + 1];
        for (int i = 0; i <= nu; i++) for (int j = 0; j <= nv; j++)
            {
                float u = -Mathf.PI + 2f * Mathf.PI * i / nu, v = -Mathf.PI * .5f + Mathf.PI * j / nv, r = Mathf.Max(0f, C(v, brzeg));
                pts[i, j] = c + q * new Vector3(size.x * .5f * r * C(u, naroze), size.y * .5f * S(v, brzeg), size.z * .5f * r * S(u, naroze));
            }
        var nrm = new Vector3[nu + 1, nv + 1];
        var tris = new List<(int, int, int, int, int, int)>();
        void Dodaj(int ai, int aj, int bi, int bj, int ci, int cj)
        {
            var a = pts[ai, aj]; var b = pts[bi, bj]; var cc = pts[ci, cj];
            var n = Vector3.Cross(b - a, cc - a); if (n.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(n, (a + b + cc) / 3f - c) < 0) { var t = (bi, bj); bi = ci; bj = cj; ci = t.Item1; cj = t.Item2; n = -n; }
            nrm[ai, aj] += n; nrm[bi, bj] += n; nrm[ci, cj] += n; tris.Add((ai, aj, bi, bj, ci, cj));
        }
        for (int i = 0; i < nu; i++) for (int j = 0; j < nv; j++) { Dodaj(i, j, i + 1, j, i + 1, j + 1); Dodaj(i, j, i + 1, j + 1, i, j + 1); }
        var oy = q * Vector3.up;
        var idx = new int[nu + 1, nv + 1];
        for (int i = 0; i <= nu; i++) for (int j = 0; j <= nv; j++)
            {
                var n = j == 0 ? -oy : j == nv ? oy : nrm[i, j].sqrMagnitude > 1e-16f ? nrm[i, j].normalized : (pts[i, j] - c).normalized;
                idx[i, j] = V(s, pts[i, j], n);
            }
        foreach (var (ai, aj, bi, bj, ci, cj) in tris) { s.t.Add(idx[ai, aj]); s.t.Add(idx[bi, bj]); s.t.Add(idx[ci, cj]); }
    }

    /// <summary>Bryła obrotowa (profil (promień, wysokość) od dołu do góry), oś pionowa w p, gładkie normalne.</summary>
    public static void Toczenie(BraxNabrzeze.Siatka s, Vector2[] pr, Vector3 p, int seg = 24)
    {
        for (int j = 0; j + 1 < pr.Length; j++)
        {
            var a = pr[j]; var b = pr[j + 1]; var e = b - a; if (e.sqrMagnitude < 1e-12f) continue;
            for (int k = 0; k < seg; k++)
            {
                float t0 = k * Mathf.PI * 2f / seg, t1 = (k + 1) * Mathf.PI * 2f / seg;
                var d0 = new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)); var d1 = new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1));
                Vector3 N(Vector3 d) => (d * e.y - Vector3.up * e.x).normalized;
                var A = p + d0 * a.x + Vector3.up * a.y; var B = p + d1 * a.x + Vector3.up * a.y; var Cc = p + d1 * b.x + Vector3.up * b.y; var D = p + d0 * b.x + Vector3.up * b.y;
                var fn = N((d0 + d1).normalized);
                int i0, i1, i2, i3;
                if (Vector3.Dot(Vector3.Cross(Cc - A, D - B), fn) > 0) { i0 = V(s, A, N(d0)); i1 = V(s, B, N(d1)); i2 = V(s, Cc, N(d1)); i3 = V(s, D, N(d0)); }
                else { i0 = V(s, D, N(d0)); i1 = V(s, Cc, N(d1)); i2 = V(s, B, N(d1)); i3 = V(s, A, N(d0)); }
                s.t.Add(i0); s.t.Add(i1); s.t.Add(i2); s.t.Add(i0); s.t.Add(i2); s.t.Add(i3);
            }
        }
    }

    static Quaternion Lezaca(Rama k, float stopnie)
    {   // płaska rzecz leżąca, wezgłowiem (−przód) w górę o kąt: grubość (lokalne y) w górę i ku nogom łóżka
        float a = stopnie * Mathf.Deg2Rad; var n = Vector3.up * Mathf.Cos(a) + k.f * Mathf.Sin(a); var dl = -k.f * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a);
        return Quaternion.LookRotation(dl, n);
    }
    static Quaternion Stojaca(Rama k, float stopnie, float przechyl = 0f)
    {   // poducha stojąca, oparta do tyłu o kąt: grubość (lokalne y) ku frontowi i lekko w górę, wysokość (lokalne z) w górę
        float a = stopnie * Mathf.Deg2Rad; var n = k.f * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a); var wys = Vector3.up * Mathf.Cos(a) - k.f * Mathf.Sin(a);
        return Quaternion.AngleAxis(przechyl, n) * Quaternion.LookRotation(wys, n);
    }

    /// <summary>Łóżko (z BraxChampionsG1Meble.HBed): nóżki toczone, podstawa tapicerowana, materac, kołdra z bokami
    /// opadającymi i wywinięciem, pled w nogach, 2 poduszki do spania, 2 poduszki ozdobne i wałek (miękkie, nie kwadratowe),
    /// zagłówek pikowany kanałami. Lb = 2.0 m, szerokość Wb, wysokość zagłówka hZ (nad podłogą, nominalnie).</summary>
    public static void Lozko(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, float Wb, float hZ,
        Material podstawa, Material zaglowek, Material posciel, Material pled, Material ozdobne, Material noga)
    {
        float Lb = 2.0f, s = k.s;
        foreach (float a in new[] { -Wb * .5f, 0f, Wb * .5f }) foreach (float d in new[] { .05f, Lb })
                Toczenie(S(noga), new[] { new Vector2(.022f * s, 0f), new Vector2(.026f * s, .02f * s), new Vector2(.020f * s, .07f * s), new Vector2(0f, .07f * s) }, k.P(a * .94f, 0f, d), 12);
        Zaokr(S(podstawa), k.P(0f, .20f, (Lb + .10f) * .5f - .05f), k.V(Wb + .12f, .26f, Lb + .10f), .05f * s, k.rot, 2);
        Miekki(S(posciel), k.P(0f, .45f, Lb * .5f), k.V(Wb, .24f, Lb - .04f), k.rot, .35f, .12f, 28, 8);   // materac (boki zaokrąglone)
        float dl = Lb - .55f;   // kołdra na dolnych 2/3, boki opadają, wywinięcie
        Zaokr(S(posciel), k.P(0f, .605f, dl * .5f - .03f), k.V(Wb + .10f, .07f, dl + .06f), .035f * s, k.rot, 3);
        foreach (float sg in new[] { -1f, 1f }) Zaokr(S(posciel), k.P(sg * (Wb * .5f + .035f), .49f, dl * .5f - .03f), k.V(.03f, .25f, dl + .04f), .014f * s, k.rot, 2);
        Miekki(S(posciel), k.P(0f, .645f, dl - .08f), k.V(Wb + .08f, .09f, .24f), k.rot, .9f, .2f, 28, 8);
        Zaokr(S(pled), k.P(0f, .655f, .34f), k.V(Wb + .16f, .028f, .56f), .013f * s, k.rot, 2);
        foreach (float sg in new[] { -1f, 1f }) Zaokr(S(pled), k.P(sg * (Wb * .5f + .072f), .50f, .34f), k.V(.026f, .30f, .56f), .012f * s, k.rot, 2);
        // poduszki do spania: leżą oparte wezgłowiem o zagłówek (22°), zaokrąglone przez grubość i w zarysie
        foreach (float sg in new[] { -1f, 1f }) Miekki(S(posciel), k.P(sg * Wb * .25f, .67f, Lb - .30f), k.V(Wb * .5f - .06f, .17f, .50f), Lezaca(k, 22f), .9f, .35f);
        // poduszki ozdobne (aksamit) i wałek — stojące, oparte, lekko obrócone
        foreach (float sg in new[] { -1f, 1f }) Miekki(S(ozdobne), k.P(sg * Wb * .2f, .76f, Lb - .55f), k.V(.48f, .15f, .42f), Stojaca(k, 16f, sg * 6f), .85f, .45f);
        Miekki(S(zaglowek), k.P(0f, .72f, Lb - .70f), k.V(.50f, .16f, .26f), Stojaca(k, 12f), .9f, .5f);
        // zagłówek pikowany kanałami (pionowe wałki na płycie tylnej), do wysokości hZ (pod parapetem okna)
        float hw = Wb + .30f, h0 = .32f, hc = Mathf.Max(.5f, hZ - h0); int nc = Mathf.RoundToInt(hw / .15f); float cw = hw / nc;
        Zaokr(S(zaglowek), k.P(0f, h0 + hc * .5f, Lb + .14f), k.V(hw + .02f, hc, .06f), .025f * s, k.rot, 2);
        for (int i = 0; i < nc; i++) Zaokr(S(zaglowek), k.P(-hw * .5f + (i + .5f) * cw, h0 + hc * .5f, Lb + .07f), k.V(cw - .008f, hc - .04f, .12f), .055f * s, k.rot, 3);
    }

    /// <summary>Szafka nocna (z HNightstand): korpus dębowy zaokrąglony, 2 szuflady z frontami 12 mm przed korpusem,
    /// czarne uchwyty, nóżki; na niej lampka toczona z abażurem lnianym (świeci światło pokoju — lampka sama nie świeci).</summary>
    public static void SzafkaNocna(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, Material korpus, Material czarny, Material ceramika, Material abazur)
    {
        float s = k.s;
        foreach (float a in new[] { -.21f, .21f }) foreach (float d in new[] { .05f, .35f }) Zaokr(S(czarny), k.P(a, .07f, d), k.V(.025f, .14f, .025f), .007f * s, k.rot, 1);
        Zaokr(S(korpus), k.P(0f, .35f, .22f), k.V(.50f, .42f, .40f), .02f * s, k.rot, 2);
        foreach (float yy in new[] { .255f, .455f })
        {
            Zaokr(S(korpus), k.P(0f, yy, .01f), k.V(.46f, .18f, .024f), .008f * s, k.rot, 1);
            Zaokr(S(czarny), k.P(0f, yy + .04f, -.014f), k.V(.14f, .014f, .024f), .006f * s, k.rot, 1);
        }
        var b = k.P(.06f, .56f, .24f);
        Toczenie(S(ceramika), new[] { new Vector2(.07f * s, 0f), new Vector2(.085f * s, .03f * s), new Vector2(.10f * s, .12f * s), new Vector2(.075f * s, .20f * s), new Vector2(.02f * s, .24f * s), new Vector2(0f, .24f * s) }, b, 24);
        S(czarny).Rura(b + Vector3.up * .24f * s, b + Vector3.up * .30f * s, .007f * s, 8);
        // abażur: stożek ścięty, dwustronny (od środka też widać len)
        var a0 = b + Vector3.up * .25f * s; var a1 = b + Vector3.up * .42f * s;
        Toczenie(S(abazur), new[] { new Vector2(.17f * s, 0f), new Vector2(.13f * s, .17f * s) }, a0, 28);
        Toczenie(S(abazur), new[] { new Vector2(.125f * s, .17f * s), new Vector2(.165f * s, 0f) }, a0, 28);
    }

    /// <summary>Komoda (z HSideboard, bez złota): korpus dębowy, 3 rzędy szuflad z frontami i szczelinami, czarne uchwyty
    /// i nóżki, blat kamienny z zaokrąglonym brzegiem.</summary>
    public static void Komoda(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, float L, Material korpus, Material blat, Material czarny)
    {
        float s = k.s, h = .80f, Dp = .46f;
        foreach (float a in new[] { -L * .5f + .08f, L * .5f - .08f }) foreach (float d in new[] { .08f, Dp - .08f }) Zaokr(S(czarny), k.P(a, .07f, d), k.V(.03f, .14f, .03f), .008f * s, k.rot, 1);
        Zaokr(S(korpus), k.P(0f, (.14f + h - .03f) * .5f, Dp * .5f + .012f), k.V(L, h - .03f - .14f, Dp - .024f), .015f * s, k.rot, 2);
        float fh = (h - .03f - .14f - .02f) / 3f;
        for (int r = 0; r < 3; r++)
        {
            float yy = .15f + fh * (r + .5f);
            foreach (float a in new[] { -L * .25f, L * .25f })
            {
                Zaokr(S(korpus), k.P(a, yy, .006f), k.V(L * .5f - .012f, fh - .008f, .024f), .006f * s, k.rot, 1);
                Zaokr(S(czarny), k.P(a, yy + fh * .22f, -.018f), k.V(.16f, .014f, .024f), .006f * s, k.rot, 1);
            }
        }
        Zaokr(S(blat), k.P(0f, h - .015f, Dp * .5f), k.V(L + .02f, .03f, Dp + .02f), .01f * s, k.rot, 2);
    }

    /// <summary>Stół (z HDiningTable): blat 5.5 cm z zaokrąglonym brzegiem, oskrzynia, 4 nogi zwężane z podkładkami.</summary>
    public static void Stol(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, float L, float Wd, Material blat, Material noga)
    {
        float s = k.s, h = .76f;
        Zaokr(S(blat), k.P(0f, h - .0275f, Wd * .5f), k.V(L, .055f, Wd), .02f * s, k.rot, 2);
        foreach (float d in new[] { .14f, Wd - .14f }) Zaokr(S(noga), k.P(0f, h - .10f, d), k.V(L - .30f, .09f, .035f), .01f * s, k.rot, 1);
        foreach (float a in new[] { -L * .5f + .14f, L * .5f - .14f }) Zaokr(S(noga), k.P(a, h - .10f, Wd * .5f), k.V(.035f, .09f, Wd - .30f), .01f * s, k.rot, 1);
        foreach (float a in new[] { -L * .5f + .13f, L * .5f - .13f }) foreach (float d in new[] { .13f, Wd - .13f })
                Toczenie(S(noga), new[] { new Vector2(.026f * s, 0f), new Vector2(.03f * s, .02f * s), new Vector2(.038f * s, (h - .055f) * s), new Vector2(0f, (h - .055f) * s) }, k.P(a, 0f, d), 12);
    }

    /// <summary>Krzesło (z HDiningChair): rama dębowa, siedzisko tapicerowane, nogi przednie toczone, tylne przechodzą
    /// w słupki oparcia, poprzeczki, oparcie tapicerowane z trzech paneli (środkowy pochylony, skrzydła skręcone).</summary>
    public static void Krzeslo(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, Material siedzisko, Material drewno)
    {
        float s = k.s;
        Zaokr(S(drewno), k.P(0f, .425f, .23f), k.V(.46f, .045f, .44f), .012f * s, k.rot, 1);
        Miekki(S(siedzisko), k.P(0f, .475f, .225f), k.V(.47f, .07f, .45f), k.rot, .45f, .12f, 24, 8);
        foreach (float sg in new[] { -1f, 1f })
        {
            Toczenie(S(drewno), new[] { new Vector2(.014f * s, 0f), new Vector2(.02f * s, .405f * s), new Vector2(0f, .405f * s) }, k.P(sg * .20f, 0f, .05f), 10);
            S(drewno).Rura(k.P(sg * .20f, 0f, .42f), k.P(sg * .215f, .93f, .49f), .017f * s, 10);
            S(drewno).Rura(k.P(sg * .20f, .13f, .05f), k.P(sg * .20f, .13f, .42f), .010f * s, 8);
        }
        S(drewno).Rura(k.P(-.20f, .15f, .25f), k.P(.20f, .15f, .25f), .010f * s, 8);
        Zaokr(S(siedzisko), k.P(0f, .76f, .47f), k.V(.25f, .34f, .055f), .025f * s, k.Pochyl(8f), 3);
        foreach (float sg in new[] { -1f, 1f }) Zaokr(S(siedzisko), k.P(sg * .178f, .76f, .452f), k.V(.13f, .34f, .055f), .025f * s, k.rot * Quaternion.Euler(-8f, -sg * 14f, 0f), 2);
    }

    /// <summary>Sofa (z HSofa): nóżki toczone, podstawa, oparcie i podłokietniki (duży promień — wałki), osobne poduchy
    /// siedziska i oparcia (miękkie), 2 poduszki ozdobne. Głębokość 0.98 m.</summary>
    public static void Sofa(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, float W, Material obicie, Material ozdobne, Material noga, float podl = .22f)
    {
        float s = k.s, D = .98f; int n = Mathf.Max(1, Mathf.RoundToInt((W - 2f * podl) / .78f)); float cw = (W - 2f * podl) / n;
        var la = new List<float> { -W * .5f + .09f, W * .5f - .09f }; if (W > 2.0f) la.Add(0f);
        foreach (float a in la) foreach (float d in new[] { .09f, D - .09f })
                Toczenie(S(noga), new[] { new Vector2(.018f * s, 0f), new Vector2(.026f * s, .12f * s), new Vector2(0f, .12f * s) }, k.P(a, 0f, d), 12);
        Zaokr(S(obicie), k.P(0f, .21f, D * .5f), k.V(W - .02f, .18f, D - .02f), .05f * s, k.rot, 2);
        Zaokr(S(obicie), k.P(0f, .58f, D - .11f), k.V(W, .58f, .22f), .09f * s, k.rot, 3);
        foreach (float sg in new[] { -1f, 1f }) Zaokr(S(obicie), k.P(sg * (W * .5f - podl * .5f), .39f, D * .5f), k.V(podl, .54f, D), .10f * s, k.rot, 3);
        for (int i = 0; i < n; i++)
        {
            float a = -W * .5f + podl + (i + .5f) * cw;
            Miekki(S(obicie), k.P(a, .38f, (D - .22f) * .5f), k.V(cw - .012f, .17f, D - .22f), k.rot, .4f, .1f, 24, 8);
            Miekki(S(obicie), k.P(a, .68f, D - .30f), k.V(cw - .02f, .21f, .46f), Stojaca(k, 12f), .5f, .15f, 24, 8);
        }
        if (W > 1.3f) foreach (float sg in new[] { -1f, 1f }) Miekki(S(ozdobne), k.P(sg * (W * .5f - podl - .27f), .66f, D - .48f), k.V(.46f, .14f, .44f), Stojaca(k, 18f, sg * 7f), .85f, .45f);
    }

    /// <summary>Stolik kawowy (z HCoffeeRect): blat dębowy z zaokrąglonym brzegiem, czarna rama, półka dolna.</summary>
    public static void StolikKawowy(System.Func<Material, BraxNabrzeze.Siatka> S, Rama k, float L, float Wd, float h, Material blat, Material rama)
    {
        float s = k.s;
        Zaokr(S(blat), k.P(0f, h - .022f, Wd * .5f), k.V(L, .044f, Wd), .018f * s, k.rot, 2);
        foreach (float a in new[] { -L * .5f + .07f, L * .5f - .07f }) foreach (float d in new[] { .07f, Wd - .07f }) Zaokr(S(rama), k.P(a, (h - .044f) * .5f, d), k.V(.03f, h - .044f, .03f), .008f * s, k.rot, 1);
        Zaokr(S(blat), k.P(0f, .12f, Wd * .5f), k.V(L - .10f, .025f, Wd - .10f), .01f * s, k.rot, 1);
    }

    /// <summary>Zasłona: tkanina w fałdach (sinusoida), dwie warstwy 5 mm (widać ją z obu stron), od karnisza w dół.
    /// a — lewy górny róg (przy karniszu), wzdluz — kierunek w prawo, nOdSciany — od ściany do pokoju.</summary>
    public static void Zaslona(BraxNabrzeze.Siatka s, Vector3 a, Vector3 wzdluz, Vector3 nOdSciany, float szer, float wys, float amp = .03f, float fala = .14f)
    {
        int nx = Mathf.Max(8, Mathf.CeilToInt(szer / fala * 6f));
        var pts = new Vector3[nx + 1]; var nn = new Vector3[nx + 1];
        for (int i = 0; i <= nx; i++)
        {
            float x = szer * i / nx, ph = x / fala * Mathf.PI * 2f;
            pts[i] = a + wzdluz * x + nOdSciany * (Mathf.Sin(ph) * amp);
            nn[i] = (nOdSciany - wzdluz * (Mathf.Cos(ph) * amp * Mathf.PI * 2f / fala)).normalized;
        }
        for (int warstwa = 0; warstwa < 2; warstwa++)
        {
            float off = warstwa == 0 ? .0025f : -.0025f;
            for (int i = 0; i < nx; i++)
            {
                var n0 = warstwa == 0 ? nn[i] : -nn[i]; var n1 = warstwa == 0 ? nn[i + 1] : -nn[i + 1];
                var p0 = pts[i] + nOdSciany * off; var p1 = pts[i + 1] + nOdSciany * off;
                var A = p0; var B = p1; var Cc = p1 + Vector3.down * wys; var D = p0 + Vector3.down * wys;
                int i0 = V(s, A, n0), i1 = V(s, B, n1), i2 = V(s, Cc, n1), i3 = V(s, D, n0);
                var fn = (n0 + n1).normalized;
                if (Vector3.Dot(Vector3.Cross(B - A, Cc - A), fn) > 0) { s.t.Add(i0); s.t.Add(i1); s.t.Add(i2); s.t.Add(i0); s.t.Add(i2); s.t.Add(i3); }
                else { s.t.Add(i0); s.t.Add(i2); s.t.Add(i1); s.t.Add(i0); s.t.Add(i3); s.t.Add(i2); }
            }
        }
    }
}

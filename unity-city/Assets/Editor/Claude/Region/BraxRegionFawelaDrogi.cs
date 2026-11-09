using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// FAWELA — teren, drogi, schody, słupy (część BraxRegionFawela; plan i zasady w BraxRegionFawela.cs).
// TEREN: jedna siatka na część (zachód / wschód), oczko 2 m; warstwy mieszane płynnie kolorem wierzchołka w shaderze
// Braxhorn/NabrzezeTeren (R = ubita ziemia przy domach, drogach, schodach i na wydeptanych łatach, B = trawa, A = skała na
// stromiznach) — bez „zębów” na styku ziemi i trawy. DROGI: wstęga z próbek co 1 m (normalne uśrednione — bez szczelin na
// łukach), kolor wierzchołków: kurz przy krawężniku, ciemne ślady kół; łaty 2 cm nad jezdnią; krawężnik 12 cm z licem 0.5 m
// w dół (zakrywa styk z terenem). SCHODY: stopień = próbka 0.4 m, wysokość 0.17 m × K, policzki i poręcze z rur.
public static partial class BraxRegionFawela
{
    // teren: R = ubita ziemia, G = (nieużywane), B = trawa, A = skała
    static Material MatTerenu()
    {
        var sh = Shader.Find("Braxhorn/NabrzezeTeren");
        var ziemiaSl = new[] { "brown_mud_dry", "stony_dirt_path", "brown_mud_leaves_01" }; var trawaSl = new[] { "grass004", "brown_mud_leaves_01" }; var skalaSl = new[] { "rock_face_03", "rock_wall_08" };
        var z = Mapa("diffuse.jpg", ziemiaSl);
        if (!sh || !z) { brakTekstur.Add("teren (mieszanie warstw)"); Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA teren bez mieszania warstw (shader " + (sh ? "jest" : "BRAK") + ", ziemia " + (z ? "jest" : "BRAK") + ")"); return null; }
        var m = new Material(sh) { name = "Fawela: teren (ziemia ubita / trawa / skała)" };
        m.SetTexture("_Piasek", z); var zn = Mapa("normal.jpg", ziemiaSl); if (zn) m.SetTexture("_PiasekN", zn);
        m.SetTexture("_Mokry", z);
        var t = Mapa("diffuse.jpg", trawaSl); if (t) { m.SetTexture("_Trawa", t); var tn = Mapa("normal.jpg", trawaSl); if (tn) m.SetTexture("_TrawaN", tn); }
        var s = Mapa("diffuse.jpg", skalaSl); if (s) { m.SetTexture("_Skala", s); var sn = Mapa("normal.jpg", skalaSl); if (sn) m.SetTexture("_SkalaN", sn); }
        m.SetColor("_KPiasek", new Color(.93f, .88f, .80f)); m.SetColor("_KMokry", new Color(.72f, .67f, .6f));
        m.SetColor("_KTrawa", t ? new Color(.76f, .77f, .58f) : new Color(.33f, .42f, .22f)); m.SetColor("_KSkala", s ? new Color(.9f, .88f, .84f) : new Color(.48f, .46f, .43f));
        m.SetFloat("_TPiasek", 2.6f * K); m.SetFloat("_TTrawa", 2.2f * K); m.SetFloat("_TSkala", 4f * K); m.SetFloat("_Makro", 37f); m.SetFloat("_MakroMix", .28f);
        m.enableInstancing = false;
        Directory.CreateDirectory(BraxRegion.Out + "/Fawela");
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Fawela/teren_faweli.mat");
        opisTekstur.Add("teren=" + ziemiaSl.FirstOrDefault(BraxRegion.HasTexture) + "/" + (t ? trawaSl.FirstOrDefault(BraxRegion.HasTexture) : "BRAK") + "/" + (s ? skalaSl.FirstOrDefault(BraxRegion.HasTexture) : "BRAK"));
        if (!t) brakTekstur.Add("teren: trawa"); if (!s) brakTekstur.Add("teren: skała");
        return m;
    }

    static void Teren()
    {
        var mat = MatTerenu(); bool mieszany = mat;
        if (!mat) mat = T("ziemia stoku (bez mieszania)", 2.6f, new Color(.86f, .84f, .78f), .95f, "brown_mud_leaves_01", "concrete_floor_02");
        foreach (var (x0, x1, zTop, nazwa) in new[] { (BraxMiastoGranice.FawelaX0, -61.4f, BraxMiastoGranice.Z0, "zachód"), (-61.4f, BraxMiastoGranice.FawelaX1, -68f, "wschód") })
        {
            var xs = new List<float>(); for (float x = x0; x < x1 - .01f; x += 2f) xs.Add(x); xs.Add(x1);
            var zs = new List<float>(); for (float z = zTop; z > BraxMiastoGranice.FawelaZSzczyt + .01f; z -= 2f) zs.Add(z); zs.Add(BraxMiastoGranice.FawelaZSzczyt);
            int nx = xs.Count, nz = zs.Count;
            var hh = new float[nx, nz];
            for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) hh[i, j] = WysTerenu(xs[i], zs[j]);
            var v = new List<Vector3>(nx * nz); var uv = new List<Vector2>(nx * nz); var uv2 = new List<Vector2>(nx * nz); var c = new List<Color>(nx * nz);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = xs[i], z = zs[j];
                    v.Add(new Vector3(x, hh[i, j], z)); uv.Add(new Vector2(x, z));
                    int ia = Mathf.Max(0, i - 1), ib = Mathf.Min(nx - 1, i + 1), ja = Mathf.Max(0, j - 1), jb = Mathf.Min(nz - 1, j + 1);
                    float gx = (hh[ib, j] - hh[ia, j]) / Mathf.Max(.01f, xs[ib] - xs[ia]), gz = (hh[i, jb] - hh[i, ja]) / Mathf.Max(.01f, Mathf.Abs(zs[jb] - zs[ja]));
                    float stromo = Mathf.Sqrt(gx * gx + gz * gz);
                    // ubita ziemia: przy domach, drogach, schodach, tarasach; w zabudowie plamy trawy, dalej łąka z wydeptanymi łatami
                    float przy = Mathf.Min(Mathf.Min(DoDzialek(x, z) - 3.5f, DoDrog(x, z, 8f) - 1.5f), Mathf.Min(DoSchodow(x, z) - 1.2f, Mathf.Min(DoProst(Plac, x, z), DoProst(Sala, x, z)) - 2f));
                    float ziemia = 1f - S01(0f, 5f, przy);
                    float plama = Szum(x / 9f + 3.1f, z / 9f + 8.7f) * .7f + Szum(x / 2.7f, z / 2.7f) * .3f;
                    ziemia = Mathf.Max(ziemia, Glebokosc(x, z) < Pas2 ? S01(.36f, .58f, plama) : S01(.5f, .7f, plama) * .8f);   // wyżej wydeptane łaty i ścieżki
                    float skala = S01(.62f, .95f, stromo) * (przy > 2f ? 1f : 0f);
                    if (mieszany) c.Add(new Color(ziemia * (1f - skala), 0f, (1f - ziemia) * (1f - skala), skala));
                    else c.Add(Color.Lerp(new Color(.8f, .86f, .7f), Color.white, ziemia));
                    uv2.Add(new Vector2(S01(.5f, 1.1f, stromo), 0f));
                }
            var tr = new List<int>();
            for (int i = 0; i + 1 < nx; i++)
                for (int j = 0; j + 1 < nz; j++)
                {
                    int a = i * nz + j, b = a + 1, cc = a + nz, d = cc + 1;
                    tr.AddRange(new[] { a, cc, b, b, cc, d });
                }
            var m = new Mesh { name = "Fawela: teren " + nazwa, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetUVs(1, uv2); m.SetColors(c); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            if (m.normals.Length > 0 && m.normals[m.normals.Length / 2].y < 0f)
            {
                var tt = m.triangles; for (int q = 0; q < tt.Length; q += 3) { int s = tt[q + 1]; tt[q + 1] = tt[q + 2]; tt[q + 2] = s; }
                m.triangles = tt; m.RecalculateNormals(); m.RecalculateTangents();
            }
            var go = new GameObject(m.name); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
            go.AddComponent<MeshCollider>().sharedMesh = m;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            trojkatow += tr.Count / 3;
        }
    }

    // ---------------------------------------------------------------------------- drogi
    static bool WJezdniInnej(Droga d, float x, float z, float zapas)
    {
        foreach (var o in Drogi) if (o != d && NaDrodze(o, x, z, out _, 10f) < o.szer * .5f + zapas) return true;
        return d == Drogi[0] && Mathf.Sqrt(Sq(x - Nawrotka.x) + Sq(z - Nawrotka.y)) < NawrotkaR + zapas * .5f;
    }

    static void DrogiBuduj()
    {
        var tr = Grupa("Fawela: drogi");
        var bA = new Bud(); var bK = new Bud(); var bF = new Bud(); var bMur = new Bud(); var bMurek = new Bud(); var bWlaz = new Bud(); var bGr = new Bud();
        var rnd = new System.Random(4411);
        Color zolty = new Color(.93f, .72f, .08f), czarny = new Color(.09f, .09f, .09f), bialy = new Color(.9f, .9f, .86f);
        foreach (var d in Drogi)
        {
            float h = d.szer * .5f; int n = d.p.Count;
            var nor = new Vector2[n];
            for (int i = 0; i < n; i++) { var t = (d.p[Mathf.Min(n - 1, i + 1)] - d.p[Mathf.Max(0, i - 1)]).normalized; nor[i] = new Vector2(-t.y, t.x); }
            Vector3 V(int k, float off, float dy) => new Vector3(d.p[k].x + nor[k].x * off, d.y[k] + dy, d.p[k].y + nor[k].y * off);
            float[] offs = { h, h * .45f, 0f, -h * .45f, -h };
            Color KA(int k, int q)
            {
                float sz = .86f + .14f * Szum(d.p[k].x / 6f + q, d.p[k].y / 6f);
                float baza = q == 0 || q == 4 ? 1f : q == 2 ? .9f : .74f;   // kurz przy krawężniku, ślady kół ciemne
                return new Color(baza * sz, baza * sz * .99f, baza * sz * .97f);
            }
            int lata = 0, wlaz = rnd.Next(10, 30), auto = rnd.Next(25, 50);
            for (int i = 0; i + 1 < n; i++)
            {
                if (d.pomin[i] && d.pomin[i + 1]) continue;
                bool wMiescie = !BraxMiastoGranice.WFaweli(d.p[i].x, d.p[i].y);
                for (int q = 0; q < 4; q++) bA.QuadC(V(i, offs[q], 0f), V(i + 1, offs[q], 0f), V(i + 1, offs[q + 1], 0f), V(i, offs[q + 1], 0f), KA(i, q), KA(i + 1, q), KA(i + 1, q + 1), KA(i, q + 1));
                // krawężniki 0.25 m, 12 cm nad jezdnią, lico zewnętrzne 0.5 m w dół; miejscami malowane w żółto-białe pasy
                bool malowany = Szum(i / 37f + d.szer, d.p[0].x) > .74f;
                foreach (float s in new[] { 1f, -1f })
                {
                    var pk = d.p[i] + nor[i] * s * (h + .12f);
                    if (WJezdniInnej(d, pk.x, pk.y, .35f)) continue;
                    bK.K(malowany ? (i % 2 == 0 ? zolty : bialy) : Color.white);
                    float o0 = s * h, o1 = s * (h + .25f);
                    if (s > 0)
                    {
                        bK.Quad(V(i, o1, .12f), V(i + 1, o1, .12f), V(i + 1, o0, .12f), V(i, o0, .12f));
                        bK.Quad(V(i, o0, .12f), V(i + 1, o0, .12f), V(i + 1, o0, 0f), V(i, o0, 0f));
                        bK.Quad(V(i, o1, -.5f), V(i + 1, o1, -.5f), V(i + 1, o1, .12f), V(i, o1, .12f));
                    }
                    else
                    {
                        bK.Quad(V(i, o0, .12f), V(i + 1, o0, .12f), V(i + 1, o1, .12f), V(i, o1, .12f));
                        bK.Quad(V(i, o0, 0f), V(i + 1, o0, 0f), V(i + 1, o0, .12f), V(i, o0, .12f));
                        bK.Quad(V(i, o1, .12f), V(i + 1, o1, .12f), V(i + 1, o1, -.5f), V(i, o1, -.5f));
                    }
                }
                // oś przerywana żółta (3 m / 3 m), wytarta
                if (d.srodek && (i / 3) % 2 == 0 && !d.pomin[i] && !d.pomin[i + 1])
                {
                    float wyt = .55f + .45f * Szum(i / 5f, d.szer); bF.K(Color.Lerp(new Color(.45f, .43f, .38f), zolty, wyt));
                    bF.Quad(V(i, .06f, .012f), V(i + 1, .06f, .012f), V(i + 1, -.06f, .012f), V(i, -.06f, .012f));
                }
                if (wMiescie) continue;
                // łaty asfaltu (ciemniejsze — nowe, jaśniejsze — beton), 2 cm nad jezdnią
                if (--lata <= 0 && i + 3 < n)
                {
                    lata = rnd.Next(9, 26); int dl = rnd.Next(1, 3); float oa = ((float)rnd.NextDouble() - .5f) * (d.szer - 2.2f), sz = .5f + (float)rnd.NextDouble() * .7f;
                    float odc = rnd.NextDouble() < .7 ? .45f + (float)rnd.NextDouble() * .15f : 1.15f; var ko = new Color(odc, odc, odc);
                    for (int q = 0; q < dl; q++) bA.QuadC(V(i + q, oa + sz, .02f), V(i + q + 1, oa + sz, .02f), V(i + q + 1, oa - sz, .02f), V(i + q, oa - sz, .02f), ko, ko, ko, ko);
                }
                // włazy żeliwne
                if (--wlaz <= 0)
                {
                    wlaz = rnd.Next(38, 60); var c0 = V(i, (rnd.NextDouble() < .5 ? -1f : 1f) * h * .42f, .004f);
                    bWlaz.K(new Color(.55f, .53f, .5f)); bWlaz.Cyl(c0, c0 + Vector3.up * .016f, .33f * K, 14);
                }
                // strona spadku: betonowy murek w żółto-czarne pasy; strona stoku: mur z pustaka z graffiti (odcinki)
                foreach (float s in new[] { 1f, -1f })
                {
                    var pz = d.p[i] + nor[i] * s * (h + 5f); float tz = WysBazowa(pz.x, pz.y), ya = d.y[i];
                    var pm = d.p[i] + nor[i] * s * (h + 1.2f);
                    if (DoDzialek(pm.x, pm.y) < 4.5f || DoSchodow(pm.x, pm.y) < 3f || WJezdniInnej(d, pm.x, pm.y, 2.5f) || Mathf.Min(DoProst(Plac, pm.x, pm.y), DoProst(Sala, pm.x, pm.y)) < 9f) continue;
                    if (tz < ya - 1.3f)
                    {
                        float a0 = s * (h + .5f), a1 = s * (h + .72f), hm = .62f * K;
                        bMurek.K((i / 2) % 2 == 0 ? zolty : czarny);
                        Lico(bMurek, V(i, a0, 0f), V(i + 1, a0, 0f), V(i, a1, 0f), V(i + 1, a1, 0f), -.6f, hm, s > 0);
                    }
                    else if (tz > ya + 1.6f && Szum(i / 23f + s, d.szer * 3f) > .38f)
                    {
                        float a0 = s * (h + 2.3f), a1 = s * (h + 2.5f), hm = 2.0f * K;
                        bMur.kolor = q => Color.white * Mathf.Lerp(.62f, 1f, S01(ya, ya + 1.2f, q.y));
                        Lico(bMur, V(i, a0, 0f), V(i + 1, a0, 0f), V(i, a1, 0f), V(i + 1, a1, 0f), -.4f, hm, s > 0);
                        // graffiti na licu od strony drogi (co kilka metrów, 3 m × 1.5 m × K)
                        if (matGraffiti && i % 5 == 0 && i + 3 < n && rnd.NextDouble() < .7)
                        {
                            float ag = s * (h + 2.3f - .015f), y0 = .35f, y1 = .35f + 1.5f * K;
                            var u = KomorkaGraffiti(rnd.Next(16)); bGr.K(Color.white);
                            if (s > 0) bGr.QuadUV(V(i + 3, ag, y0), V(i, ag, y0), V(i, ag, y1), V(i + 3, ag, y1), u, true);
                            else bGr.QuadUV(V(i, ag, y0), V(i + 3, ag, y0), V(i + 3, ag, y1), V(i, ag, y1), u, true);
                            graffiti++;
                        }
                    }
                }
                // zaparkowane auta przy krawężniku tam, gdzie stoją domy (modele aut miasta — krok ruchu)
                if (--auto <= 0 && i > 3 && i + 4 < n && d.szer >= 5f)
                {
                    auto = rnd.Next(30, 70); float s = rnd.NextDouble() < .5 ? 1f : -1f;
                    var pa = d.p[i] + nor[i] * s * (h + 3f);
                    if (DoDzialek(pa.x, pa.y) < 5f && !WJezdniInnej(d, d.p[i].x, d.p[i].y, 9f))
                    {
                        var c0 = V(i, s * (h - 1.15f), .01f); var c1 = V(i + 2, s * (h - 1.15f), .01f); var c2 = V(i - 2, s * (h - 1.15f), .01f);
                        var przod = (c1 - c2).normalized * (rnd.NextDouble() < .5 ? 1f : -1f);
                        BraxRegionMiastoB.AutaWarsztatow.Add((c0, przod, 0f, "fawela — zaparkowane przy " + d.nazwa)); aut++;
                    }
                }
            }
        }
        // progi zwalniające przed placem i salą (żółto-czarne), na serpentynie
        var r1 = Drogi[1];
        foreach (float xp in new[] { Plac.xMin - 6f, Sala.xMax + 6f })
        {
            int ib = -1; float bd = 1e9f;
            for (int i = 2; i + 3 < r1.p.Count; i++) { float dd = Mathf.Abs(r1.p[i].x - xp); if (r1.p[i].y > -190f && r1.p[i].y < -150f && dd < bd) { bd = dd; ib = i; } }
            if (ib < 0) continue;
            var t = (r1.p[ib + 1] - r1.p[ib - 1]).normalized; var nn = new Vector2(-t.y, t.x); float hh = r1.szer * .5f, sp = r1.y[ib + 1] - r1.y[ib];
            Vector3 P(float along, float off, float dy) => new Vector3(r1.p[ib].x + t.x * along + nn.x * off, r1.y[ib] + sp * along + dy, r1.p[ib].y + t.y * along + nn.y * off);
            for (int q = 0; q < 8; q++)
            {
                float o0 = -hh + q * (r1.szer / 8f), o1 = o0 + r1.szer / 8f; bF.K(q % 2 == 0 ? zolty : czarny);
                bF.Quad(P(-.5f, o1, .012f), P(-.15f, o1, .07f), P(-.15f, o0, .07f), P(-.5f, o0, .012f));
                bF.Quad(P(-.15f, o1, .07f), P(.15f, o1, .07f), P(.15f, o0, .07f), P(-.15f, o0, .07f));
                bF.Quad(P(.15f, o1, .07f), P(.5f, o1, .012f), P(.5f, o0, .012f), P(.15f, o0, .07f));
            }
        }
        // nawrotka na końcu ul. dolnej: tarcza asfaltu z krawężnikiem (otwarta od strony jezdni)
        var cK = new Vector3(Nawrotka.x, NawrotkaY, Nawrotka.y);
        for (int k = 0; k < 28; k++)
        {
            float a0 = k * Mathf.PI * 2f / 28f, a1 = (k + 1) * Mathf.PI * 2f / 28f;
            Vector3 D(float a, float r, float dy) => cK + new Vector3(Mathf.Cos(a) * r, dy, Mathf.Sin(a) * r);
            var j0 = new Color(.78f, .78f, .77f); var j1 = new Color(.95f, .94f, .92f);
            bA.QuadC(cK, D(a1, NawrotkaR * .5f, 0f), D(a0, NawrotkaR * .5f, 0f), cK, j0, j0, j0, j0);
            bA.QuadC(D(a0, NawrotkaR * .5f, 0f), D(a1, NawrotkaR * .5f, 0f), D(a1, NawrotkaR, 0f), D(a0, NawrotkaR, 0f), j0, j0, j1, j1);
            if (Mathf.Cos((a0 + a1) * .5f) < -.82f) continue;   // wjazd z ul. dolnej (od zachodu)
            bK.K(Color.white);
            bK.Quad(D(a0, NawrotkaR, .12f), D(a1, NawrotkaR, .12f), D(a1, NawrotkaR + .25f, .12f), D(a0, NawrotkaR + .25f, .12f));
            bK.Quad(D(a0, NawrotkaR, 0f), D(a1, NawrotkaR, 0f), D(a1, NawrotkaR, .12f), D(a0, NawrotkaR, .12f));
            bK.Quad(D(a0, NawrotkaR + .25f, .12f), D(a1, NawrotkaR + .25f, .12f), D(a1, NawrotkaR + .25f, -.5f), D(a0, NawrotkaR + .25f, -.5f));
        }
        bA.Obiekt("Fawela: jezdnie (asfalt spękany, łaty)", tr, asfalt, false, true);
        bK.Obiekt("Fawela: krawężniki", tr, kraweznik, false, true);
        bF.Obiekt("Fawela: oś jezdni i progi zwalniające (farba)", tr, farba, false, false);
        bMurek.Obiekt("Fawela: murki betonowe w żółto-czarne pasy", tr, beton, true, true);
        bMur.Obiekt("Fawela: mury z pustaka przy drogach", tr, pustak, true, true);
        bWlaz.Obiekt("Fawela: włazy żeliwne", tr, zeliwo, false, false);
        if (matGraffiti) bGr.Obiekt("Fawela: graffiti na murach", tr, matGraffiti, false, false);
    }

    /// <summary>Mur wzdłuż odcinka drogi: lico od jezdni (punkty a0, b0), lico zewnętrzne (a1, b1), od „dol” do „gora” nad punktami.
    /// plus = mur po stronie dodatniej normalnej drogi.</summary>
    static void Lico(Bud b, Vector3 a0, Vector3 b0, Vector3 a1, Vector3 b1, float dol, float gora, bool plus)
    {
        Vector3 U(Vector3 p, float dy) => new Vector3(p.x, p.y + dy, p.z);
        if (plus)
        {
            b.Quad(U(a0, gora), U(b0, gora), U(b0, dol), U(a0, dol));
            b.Quad(U(a1, dol), U(b1, dol), U(b1, gora), U(a1, gora));
            b.Quad(U(a1, gora), U(b1, gora), U(b0, gora), U(a0, gora));
        }
        else
        {
            b.Quad(U(a0, dol), U(b0, dol), U(b0, gora), U(a0, gora));
            b.Quad(U(a1, gora), U(b1, gora), U(b1, dol), U(a1, dol));
            b.Quad(U(a0, gora), U(b0, gora), U(b1, gora), U(a1, gora));
        }
    }

    // ---------------------------------------------------------------------------- schody (stopień co próbkę 0.4 m, policzki, poręcze z rur)
    static void SchodyBuduj()
    {
        var tr = Grupa("Fawela: schody i alejki");
        var st = new Bud(); var por = new Bud(); var pol = new Bud();
        por.K(new Color(.45f, .62f, .8f)); float stopien = .17f * K, pw = .85f * K;
        foreach (var s in Schody)
        {
            float yPop = WysBez(s[0].x, s[0].y);
            for (int i = 0; i + 1 < s.Count; i++)
            {
                var a = s[i]; var b = s[i + 1];
                float yt = WysBez(b.x, b.y);
                if (DoDrog(a.x, a.y, 10f) < .5f || DoDrog(b.x, b.y, 10f) < .5f || !BraxMiastoGranice.WFaweli(b.x, b.y)) { yPop = yt; continue; }
                float y = yPop;
                if (yt - yPop > stopien * .55f) y = yPop + stopien; else if (yPop - yt > stopien * .55f) y = yPop - stopien;
                if (Mathf.Abs(y - yt) > stopien * 1.6f) y = yt;      // za drogą / przy tarasie: wyrównanie do terenu
                var dir = (b - a).normalized; var nn = new Vector2(-dir.y, dir.x);
                Vector3 P(Vector2 p, float off, float yy) => new Vector3(p.x + nn.x * off, yy, p.y + nn.y * off);
                float dol = Mathf.Min(y, yt) - .9f, yy0 = y;
                st.kolor = q => Color.white * (q.y > yy0 - .01f ? Mathf.Lerp(.78f, 1f, Szum(q.x * 1.3f, q.z * 1.3f)) : .8f);
                st.Quad(P(a, pw, y), P(b, pw, y), P(b, -pw, y), P(a, -pw, y));                // wierzch stopnia
                st.Quad(P(a, -pw, y), P(a, -pw, dol), P(a, pw, dol), P(a, pw, y));            // czoło od strony miasta
                st.Quad(P(b, pw, y), P(b, pw, dol), P(b, -pw, dol), P(b, -pw, y));            // czoło od strony góry
                pol.kolor = q => Color.white * Mathf.Lerp(.7f, .95f, S01(dol, yy0, q.y));
                Lico(pol, P(a, pw, y), P(b, pw, y), P(a, pw + .22f, y), P(b, pw + .22f, y), dol - y, .28f, true);
                Lico(pol, P(a, -pw, y), P(b, -pw, y), P(a, -pw - .22f, y), P(b, -pw - .22f, y), dol - y, .28f, false);
                krokow++;
                if (i % 5 == 0)
                {
                    var p0 = P(a, pw + .11f, y + .28f);
                    por.Cyl(p0, p0 + Vector3.up * .9f * K, .024f, 6);
                    int j = Mathf.Min(s.Count - 1, i + 5); var nj = s[j];
                    var p1 = new Vector3(nj.x + nn.x * (pw + .11f), WysBez(nj.x, nj.y) + .28f, nj.y + nn.y * (pw + .11f));
                    if (DoDrog(nj.x, nj.y, 10f) > 1f && DoDrog(a.x, a.y, 10f) > 1f) por.Cyl(p0 + Vector3.up * .9f * K, p1 + Vector3.up * .9f * K, .026f, 6);
                }
                yPop = y;
            }
        }
        st.Obiekt("Fawela: stopnie schodów (beton wytarty)", tr, placBeton, true, true);
        pol.Obiekt("Fawela: policzki schodów (beton)", tr, beton, true, true);
        por.Obiekt("Fawela: poręcze schodów (rury malowane)", tr, metal, true, false);
    }

    // ---------------------------------------------------------------------------- słupy, transformatory, przewody, przyłącza, latarnie
    static void Slupy()
    {
        var tr = Grupa("Fawela: słupy, przewody, latarnie");
        var sl = new Bud(); var pr = new Bud(); var lp = new Bud(); var st = new Bud();
        var szczyty = new List<Vector3>(); var rnd = new System.Random(733);
        void Przewod(Vector3 A, Vector3 Bp, float zwis, float gr, int seg)
        {
            Vector3 prev = A;
            for (int k = 1; k <= seg; k++) { float tt = k / (float)seg; var q = Vector3.Lerp(A, Bp, tt) + Vector3.down * (4f * tt * (1f - tt) * zwis); pr.Cyl(prev, q, gr, 3, -1f, false); prev = q; }
        }
        foreach (var d in Drogi)
        {
            Vector3? pop = null; Vector3 popBok = Vector3.right; int nr = 0;
            for (int i = 6; i < d.p.Count - 3; i += 28)
            {
                var a = d.p[i]; var b = d.p[Mathf.Min(d.p.Count - 1, i + 1)];
                var dir = (b - a).normalized; var nn = new Vector2(-dir.y, dir.x);
                float hl = WysBazowa(a.x + nn.x * 6f, a.y + nn.y * 6f), hr = WysBazowa(a.x - nn.x * 6f, a.y - nn.y * 6f), strona = hl < hr ? 1f : -1f;
                var q2 = a + nn * strona * (d.szer * .5f + .95f);
                if (!BraxMiastoGranice.WFaweli(q2.x, q2.y) || DoDzialek(q2.x, q2.y) < .5f || DoSchodow(q2.x, q2.y) < 1.5f || WJezdniInnej(d, q2.x, q2.y, 1.2f)) { pop = null; continue; }
                float y = WysTerenu(q2.x, q2.y);
                var p0 = new Vector3(q2.x, y - .4f, q2.y); var top = p0 + Vector3.up * 9.4f;
                // słup żelbetowy (zwężany), poprzeczki z izolatorami
                sl.kolor = q => Color.white * Mathf.Lerp(.7f, 1f, S01(y, y + 2f, q.y));
                sl.Cyl(p0, top, .17f, 8, .11f);
                var poprz = new Vector3(dir.x, 0f, dir.y); var bok = new Vector3(nn.x, 0f, nn.y);
                st.K(new Color(.62f, .6f, .58f)); st.BoxRot(top + Vector3.down * .5f, new Vector3(.08f, .1f, 1.9f), Quaternion.LookRotation(bok));
                st.BoxRot(top + Vector3.down * 1.35f, new Vector3(.07f, .08f, 1.3f), Quaternion.LookRotation(bok));
                st.K(new Color(.8f, .5f, .35f));
                foreach (float off in new[] { -.85f, -.3f, .3f, .85f }) st.Cyl(top + bok * off + Vector3.down * .45f, top + bok * off + Vector3.down * .27f, .04f, 6);
                // co trzeci słup: transformator (kadź z przepustami na wsporniku), co drugi: latarnia sodowa na wysięgniku
                if (nr % 3 == 1)
                {
                    var tc = top + Vector3.down * 2.6f + poprz * .45f; st.K(new Color(.72f, .75f, .74f));
                    st.Cyl(tc + Vector3.down * .5f, tc + Vector3.up * .5f, .32f, 10);
                    st.Cyl(tc + Vector3.up * .5f, tc + Vector3.up * .72f, .05f, 5); st.Cyl(tc + Vector3.up * .5f + bok * .15f, tc + Vector3.up * .7f + bok * .15f, .05f, 5);
                    st.BoxRot(tc + Vector3.down * .56f - poprz * .2f, new Vector3(.7f, .1f, .9f), Quaternion.LookRotation(poprz));
                }
                if (nr % 2 == 0)
                {
                    var ra = top + Vector3.down * 1.9f; var rb = ra - bok * strona * 1.7f + Vector3.up * .35f;
                    st.K(new Color(.7f, .7f, .7f)); st.Cyl(ra, rb, .035f, 6);
                    st.BoxRot(rb + Vector3.down * .02f, new Vector3(.24f, .1f, .55f), Quaternion.LookRotation(bok));
                    lp.K(Color.white); lp.BoxRot(rb + Vector3.down * .085f, new Vector3(.18f, .03f, .42f), Quaternion.LookRotation(bok));
                }
                Kol(tr, p0 - new Vector3(.17f, 0f, .17f), p0 + new Vector3(.17f, 9.4f, .17f));
                // przewody między słupami: 4 linie na poprzeczce górnej, 3 niżej i wiązka telekomunikacyjna
                if (pop.HasValue)
                {
                    foreach (float off in new[] { -.85f, -.3f, .3f, .85f }) Przewod(pop.Value + popBok * off + Vector3.down * .27f, top + bok * off + Vector3.down * .27f, .55f, .011f, 6);
                    foreach (float off in new[] { -.55f, 0f, .55f }) Przewod(pop.Value + popBok * off + Vector3.down * 1.3f, top + bok * off + Vector3.down * 1.3f, .7f, .013f, 6);
                    for (int k = 0; k < 3; k++) Przewod(pop.Value + Vector3.down * (2.9f + k * .12f), top + Vector3.down * (2.9f + k * .1f), .9f + k * .25f, .016f, 6);
                }
                pop = top; popBok = bok; szczyty.Add(top); slupow++; nr++;
            }
        }
        // przyłącza do domów: od najbliższego słupa (do 32 m) do skrzynki pod dachem — plątanina typowa dla faweli
        foreach (var p in Przylacza)
        {
            Vector3 naj = Vector3.zero; float bd = 32f * 32f;
            foreach (var s in szczyty) { float dd = Sq(s.x - p.x) + Sq(s.z - p.z); if (dd < bd) { bd = dd; naj = s; } }
            if (bd >= 32f * 32f - .01f) continue;
            int ile = 1 + rnd.Next(2);
            for (int k = 0; k < ile; k++) Przewod(naj + Vector3.down * (1.5f + k * .25f + (float)rnd.NextDouble() * .4f), p + Vector3.up * k * .08f, .5f + (float)rnd.NextDouble() * .9f, .009f, 5);
            przylaczy++;
        }
        sl.Obiekt("Fawela: słupy żelbetowe", tr, beton, true, false);
        st.Obiekt("Fawela: osprzęt słupów (poprzeczki, izolatory, transformatory, oprawy)", tr, metal, true, false);
        pr.Obiekt("Fawela: przewody i przyłącza", tr, kabel, false, false);
        lp.Obiekt("Fawela: latarnie (świecą)", tr, zarowka, false, false);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// FAWELA — domy, lokale, plac, sala kickboxingu, graffiti (część BraxRegionFawela; plan i zasady w BraxRegionFawela.cs).
// DOM: posadzka = teren przed frontem + próg 17 cm (dom wcięty w stok z tyłu, od strony spadku betonowy fundament); ściany
// jednostronne z ościeżami; wykończenie jednego z 7 typów (tynk malowany, goła cegła w żelbetowej ramie, parter tynk + piętra
// cegła, szary tynk cementowy, pustak, tynk odpadający, tynk czerwony / niebieski łuszczący się); stropy z betonu z prętami
// widoczne na elewacji; okna: aluminiowe przesuwne, drewniane z okiennicami albo stalowe szprosowe — szyba, za nią zasłona
// (fotoskan tkaniny), parapet, krata na parterze; drzwi stalowe malowane albo drewniane z progiem i kratą; balkony (pręty albo
// murek) z praniem; dach: attyka, blacha falista na słupkach albo niedokończone piętro (słupy z prętami, mur na 2–3 warstwy);
// zbiorniki na wodę, anteny, talerze, klimatyzatory, rury spustowe, skrzynka licznika z przyłączem, daszki z blachy.
// LOKAL (19): otwarty front z roletą, szyld-bryła 3D z literami (zasada szyldów 9.11), markiza w pasy, wnętrze z okładziną
// ścian i lamperią, posadzka z płytek / linoleum, lada, regały z towarem, lodówka, świetlówka, wyposażenie wg branży,
// zaplecze za ścianką; kolizje ścian z przejściem; stanowisko sprzedawcy (Sprzedawcy).
public static partial class BraxRegionFawela
{
    /// <summary>Rama ściany: A = lewy dolny róg (patrząc OD ŚRODKA domu na zewnątrz… czyli s rośnie w prawo dla stojącego w domu),
    /// N = normalna na zewnątrz, R = up × N. Punkt: s wzdłuż ściany, y bezwzględne, d w głąb domu (ujemne = przed lico).</summary>
    internal struct Fr
    {
        public Vector3 A, R, N; public float L;
        public Vector3 P(float s, float y, float d) => new Vector3(A.x + R.x * s - N.x * d, y, A.z + R.z * s - N.z * d);
        public static Fr Nowa(Vector3 a, Vector3 n, float l) => new Fr { A = a, N = n, R = Vector3.Cross(Vector3.up, n), L = l };
        public float Yaw => Mathf.Atan2(N.x, N.z) * Mathf.Rad2Deg;
    }

    // ściana jednostronna z otworami i ościeżami (wewn != null: także lico wewnętrzne w odległości grub — lokale)
    static void Sciana(Bud b, Fr F, float s0, float s1, float y0, float y1, List<Rect> otw, float pas, float glebia, Bud wewn = null, float grub = .22f)
    {
        var xs = new SortedSet<float> { s0, s1 };
        foreach (var o in otw) { if (o.xMin > s0 && o.xMin < s1) xs.Add(o.xMin); if (o.xMax > s0 && o.xMax < s1) xs.Add(o.xMax); }
        var X = xs.ToList();
        for (int i = 0; i + 1 < X.Count; i++)
        {
            float a = X[i], c = X[i + 1]; if (c - a < .002f) continue; float m = (a + c) * .5f;
            var ys = new SortedSet<float> { y0, y1 };
            if (!float.IsNaN(pas) && pas > y0 && pas < y1) ys.Add(pas);
            var zaj = otw.Where(o => o.xMin <= m && o.xMax >= m).ToList();
            foreach (var o in zaj) { if (o.yMin > y0 && o.yMin < y1) ys.Add(o.yMin); if (o.yMax > y0 && o.yMax < y1) ys.Add(o.yMax); }
            var Y = ys.ToList();
            for (int j = 0; j + 1 < Y.Count; j++)
            {
                float ya = Y[j], yb = Y[j + 1], ym = (ya + yb) * .5f;
                if (yb - ya < .002f || zaj.Any(o => o.yMin <= ym && o.yMax >= ym)) continue;
                b.Quad(F.P(a, ya, 0f), F.P(c, ya, 0f), F.P(c, yb, 0f), F.P(a, yb, 0f));
                if (wewn != null) wewn.Quad(F.P(c, ya, grub), F.P(a, ya, grub), F.P(a, yb, grub), F.P(c, yb, grub));
            }
        }
        float g = wewn != null ? grub : glebia;
        foreach (var o in otw)
        {
            b.Quad(F.P(o.xMin, o.yMin, 0f), F.P(o.xMin, o.yMin, g), F.P(o.xMin, o.yMax, g), F.P(o.xMin, o.yMax, 0f));
            b.Quad(F.P(o.xMax, o.yMin, g), F.P(o.xMax, o.yMin, 0f), F.P(o.xMax, o.yMax, 0f), F.P(o.xMax, o.yMax, g));
            b.Quad(F.P(o.xMin, o.yMax, 0f), F.P(o.xMin, o.yMax, g), F.P(o.xMax, o.yMax, g), F.P(o.xMax, o.yMax, 0f));
            if (o.yMin > y0 + .01f) b.Quad(F.P(o.xMin, o.yMin, g), F.P(o.xMin, o.yMin, 0f), F.P(o.xMax, o.yMin, 0f), F.P(o.xMax, o.yMin, g));
        }
    }

    /// <summary>Bryła w ramie ściany: s0…s1, y0…y1, d0 (bliżej zewnątrz) … d1.</summary>
    static void BoxF(Bud b, Fr F, float s0, float s1, float y0, float y1, float d0, float d1, bool tyl = false, bool dno = true)
    {
        if (s1 - s0 < 1e-4f || y1 - y0 < 1e-4f || d1 - d0 < 1e-4f) return;
        b.Quad(F.P(s0, y0, d0), F.P(s1, y0, d0), F.P(s1, y1, d0), F.P(s0, y1, d0));
        b.Quad(F.P(s0, y0, d1), F.P(s0, y0, d0), F.P(s0, y1, d0), F.P(s0, y1, d1));
        b.Quad(F.P(s1, y0, d0), F.P(s1, y0, d1), F.P(s1, y1, d1), F.P(s1, y1, d0));
        b.Quad(F.P(s0, y1, d0), F.P(s1, y1, d0), F.P(s1, y1, d1), F.P(s0, y1, d1));
        if (dno) b.Quad(F.P(s0, y0, d1), F.P(s1, y0, d1), F.P(s1, y0, d0), F.P(s0, y0, d0));
        if (tyl) b.Quad(F.P(s1, y0, d1), F.P(s0, y0, d1), F.P(s0, y1, d1), F.P(s1, y1, d1));
    }
    /// <summary>Płaski pasek na licu (rama okna, pręt kraty) na głębokości d.</summary>
    static void Pasek(Bud b, Fr F, float s0, float s1, float y0, float y1, float d) => b.Quad(F.P(s0, y0, d), F.P(s1, y0, d), F.P(s1, y1, d), F.P(s0, y1, d));
    static void KolF(Transform tr, Fr F, float s0, float s1, float y0, float y1, float d0, float d1)
    {
        var a = F.P(s0, y0, d0); var c = F.P(s1, y1, d1); Kol(tr, Vector3.Min(a, c), Vector3.Max(a, c));
    }
    /// <summary>Graffiti na licu ściany (1.5 cm przed licem); obraz czytany od patrzącego.</summary>
    static void Naklejka(Fr F, float s0, float s1, float y0, float y1, int komorka, float cx, float cz)
    {
        if (!matGraffiti) return;
        var b = W(cx, cz, matGraffiti, Color.white);
        b.QuadUV(F.P(s0, y0, -.015f), F.P(s1, y0, -.015f), F.P(s1, y1, -.015f), F.P(s0, y1, -.015f), KomorkaGraffiti(komorka), true);
        graffiti++;
    }

    static readonly Color[] Pastele =
    {
        new Color(.95f, .82f, .46f), new Color(.52f, .70f, .88f), new Color(.93f, .68f, .64f), new Color(.66f, .83f, .62f), new Color(.92f, .70f, .44f),
        new Color(.95f, .93f, .86f), new Color(.50f, .80f, .76f), new Color(.96f, .74f, .56f), new Color(.78f, .70f, .88f), new Color(.86f, .88f, .54f),
        new Color(.90f, .55f, .50f), new Color(.60f, .72f, .60f)
    };
    // szyldy malowane: (tablica, litery) — żółć / czerwień / granat / zieleń, bez bieli
    static readonly (Color, Color)[] ParySzyldow =
    {
        (new Color(.93f, .76f, .16f), new Color(.70f, .10f, .08f)), (new Color(.70f, .13f, .10f), new Color(.97f, .82f, .18f)), (new Color(.12f, .28f, .58f), new Color(.97f, .82f, .18f)),
        (new Color(.10f, .42f, .24f), new Color(.97f, .82f, .18f)), (new Color(.90f, .48f, .10f), new Color(.08f, .10f, .30f)), (new Color(.88f, .80f, .58f), new Color(.70f, .10f, .08f))
    };
    static readonly Color[] KolDrzwi =
    {
        new Color(.25f, .50f, .36f), new Color(.24f, .40f, .66f), new Color(.62f, .24f, .20f), new Color(.86f, .84f, .78f), new Color(.45f, .32f, .22f), new Color(.80f, .62f, .20f), new Color(.35f, .36f, .38f)
    };

    static void Domy()
    {
        foreach (var dz in Dzialki)
        {
            try { Dom(dz); }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_FAWELA dom " + dz.nr + " (" + dz.x0.ToString("F0") + ", " + dz.z0.ToString("F0") + "): " + e.Message); }
        }
        // rośliny w donicach przy wejściach domów pasa 1 i drzewa przy placu (modele z Poly Haven / biblioteki — ograniczona liczba)
        var rnd = new System.Random(5150);
        var donice = new[] { "potted_plant_01", "potted_plant_02", "potted_plant_04", "planter_pot_clay", "shrub_02", "shrub_01" }.Select(id => BraxRegion.Model(id)).Where(m => m).ToList();
        if (donice.Count > 0)
            foreach (var dz in Dzialki.Where(q => q.pas == 1 && !q.lokal && q.nr % 4 == 0).Take(40))
            {
                float x = dz.x0 + .6f + (float)rnd.NextDouble() * .4f, z = dz.polnoc ? dz.z1 + .45f : dz.z0 - .45f;
                if (DoDrog(x, z, 6f) < .8f) continue;
                if (BraxRegion.Put(Rodzic(x, z), donice[rnd.Next(donice.Count)], "Fawela: roślina w donicy", new Vector3(x, WysTerenu(x, z), z), rnd.Next(360), 1f, false, 1.1f)) roslin++;
            }
        // rekwizyty ulicy z biblioteki (modele Poly Haven pobierane dla miasta): kosze, skrzynki, beczki, opony, kartony, stołki
        // przy frontach domów przy drogach — co trzeci dom, po 1–2 sztuki (kolizja pudełkowa)
        string[][] rekw = { new[] { "metal_trash_can" }, new[] { "wooden_crate_01", "wooden_crate_02" }, new[] { "plastic_crate_01" }, new[] { "cardboard_box_01" }, new[] { "wooden_barrels_01", "barrel_03", "Barrel_02" },
                            new[] { "old_tyre" }, new[] { "rusted_wheel_rim_01", "old_tyre" }, new[] { "wooden_stool_01", "metal_stool_01" }, new[] { "hand_truck" }, new[] { "wicker_basket_01", "wicker_basket_02" } };
        float[] rekwWys = { .9f, .5f, .32f, .4f, .95f, .25f, .2f, .48f, 1.1f, .3f };
        foreach (var dz in Dzialki.Where(q => q.nr % 3 == 1))
        {
            float zf = dz.polnoc ? dz.z1 + .55f : dz.z0 - .55f;
            if (DoDrog((dz.x0 + dz.x1) * .5f, zf, 8f) > 5f) continue;
            for (int q = 0; q < 1 + rnd.Next(2); q++)
            {
                int kt = rnd.Next(rekw.Length); float x = dz.x1 - .5f - q * .8f - (float)rnd.NextDouble() * .4f;
                if (DoDrog(x, zf, 6f) < .9f) continue;
                var m = BraxRegion.Model(rekw[kt]); if (!m) continue;
                if (BraxRegion.Put(Rodzic(x, zf), m, "Fawela: " + rekw[kt][0], new Vector3(x, Mathf.Max(WysTerenu(x, zf), dz.yFront + .05f), zf), rnd.Next(360), 1f, true, rekwWys[kt])) roslin++;
            }
        }
        // stoły piknikowe i kosz na placu (Poly Haven)
        var stol = BraxRegion.Model("wooden_picnic_table");
        if (stol) foreach (float sx in new[] { -9f, 9f }) if (BraxRegion.Put(Rodzic(Plac.center.x, Plac.center.y), stol, "Fawela: stół na placu", new Vector3(Plac.center.x + sx, Tarasy[0].y, Plac.center.y - 1.5f), 90f, 1f, true, .8f)) roslin++;
        var drzewo = BraxRegion.Model("drzewo_verdant");
        if (drzewo)
            foreach (var (x, z) in new[] { (Plac.xMin + 2.5f, Plac.yMax - 2.5f), (Plac.xMax - 2.5f, Plac.yMin + 2.5f), (Plac.center.x, Plac.center.y + 1.5f) })
                if (BraxRegion.Put(Rodzic(x, z), drzewo, "Fawela: drzewo na placu", new Vector3(x, Tarasy[0].y - .03f, z), rnd.Next(360), 1f, false, 9f)) roslin++;
    }

    /// <summary>Okna stoją na siatce 1.6 m świata (środki w (k + ½) · 1.6 m wzdłuż ściany): pokój za szybą (shader okien miasta, komórka
    /// 3.2 m) nigdy nie ma ściany działowej w środku szyby. Zwraca s środka okna najbliższe podanemu.</summary>
    internal const float SiatkaOkien = 1.6f;
    static float NaSiatce(Fr F, float s)
    {
        bool wzdX = Mathf.Abs(F.R.x) > .5f; float a = wzdX ? F.A.x : F.A.z, kier = wzdX ? F.R.x : F.R.z;
        float w = a + kier * s, wq = (Mathf.Floor(w / SiatkaOkien) + .5f) * SiatkaOkien;
        return (wq - a) / kier;
    }

    sealed class Otw { public Rect r; public int rodzaj; }   // 0 okno, 1 drzwi, 2 drzwi balkonu, 3 front lokalu, 4 brama garażu, 5 okienko

    static void Dom(Dzialka dz)
    {
        var r = new System.Random(9176 + dz.nr * 131);
        float x0 = dz.x0, x1 = dz.x1, z0 = dz.z0, z1 = dz.z1, cx = (x0 + x1) * .5f, cz = (z0 + z1) * .5f, yF = dz.yF, hf = Hf, k = K;
        int n = dz.pas == 1 ? 3 + (r.NextDouble() < .45 ? 1 : 0) : 2 + (r.NextDouble() < .5 ? 1 : 0);
        if (dz.d > Pas2) n = 2 + (r.NextDouble() < .3 ? 1 : 0);
        // wykończenie: 0 tynk malowany, 1 cegła w ramie, 2 parter tynk + cegła, 3 szary tynk cementowy, 4 pustak, 5 tynk odpadający, 6 czerwony / niebieski
        int typ; double los = r.NextDouble();
        if (dz.pas == 1) typ = los < .5 ? 0 : los < .68 ? 3 : los < .8 ? 5 : los < .9 ? 6 : 2;
        else typ = los < .2 ? 0 : los < .45 ? 1 : los < .6 ? 2 : los < .8 ? 3 : los < .9 ? 4 : 5;
        bool rama = typ == 1 || typ == 2 || typ == 4;
        bool niedok = dz.pas == 2 && rama && r.NextDouble() < .5;
        var farbaDomu = Pastele[r.Next(Pastele.Length)]; int tk = r.Next(2) == 0 ? 0 : 3, kol6 = r.Next(3), tk5 = r.Next(2) == 0 ? 2 : 4;
        float szary = .84f + (float)r.NextDouble() * .16f, brud = dz.pas == 1 ? .74f : .58f, yb = yF;
        float Gr(Vector3 p) => Mathf.Lerp(brud, 1f, S01(yb, yb + 1.3f, p.y)) * (.93f + .07f * Szum(p.x * .6f + p.y * .4f, p.z * .6f + p.y * .3f));
        Bud BW(int i)
        {
            Material m; Color kol;
            if (typ == 0 || (typ == 2 && i == 0)) { m = tynki[tk]; kol = farbaDomu; }
            else if (typ == 1 || typ == 2) { m = cegla; kol = Color.white; }
            else if (typ == 3) { m = tynki[1]; kol = new Color(szary, szary, szary * .98f); }
            else if (typ == 4) { m = pustak; kol = Color.white; }
            else if (typ == 5) { m = tynki[tk5]; kol = Color.Lerp(Color.white, farbaDomu, .25f); }
            else { m = kol6 == 0 ? tynkCzerwony : kol6 == 1 ? tynkNiebieski : tynkRozowy; kol = Color.white; }
            var bb = W(cx, cz, m); bb.kolor = p => kol * Gr(p); return bb;
        }
        bool pn = dz.polnoc; float L = x1 - x0, D = z1 - z0;
        var fF = pn ? Fr.Nowa(new Vector3(x0, 0f, z1), Vector3.forward, L) : Fr.Nowa(new Vector3(x1, 0f, z0), Vector3.back, L);
        var fB = pn ? Fr.Nowa(new Vector3(x1, 0f, z0), Vector3.back, L) : Fr.Nowa(new Vector3(x0, 0f, z1), Vector3.forward, L);
        var fL = Fr.Nowa(new Vector3(x0, 0f, z0), Vector3.left, D); var fP = Fr.Nowa(new Vector3(x1, 0f, z1), Vector3.right, D);
        float dw = 1.0f * k, dh = 2.1f * k, ow = 1.1f * k, oh = 1.05f * k, os = .95f * k, yTop = yF + n * hf;
        int styl = r.Next(3); var kolRamy = styl == 0 ? Color.white : styl == 1 ? KolDrzwi[r.Next(KolDrzwi.Length)] : new Color(.5f, .5f, .5f);
        var kolDrzwi = KolDrzwi[r.Next(KolDrzwi.Length)]; var kolKraty = r.Next(3) == 0 ? new Color(.9f, .9f, .88f) : new Color(.3f, .3f, .3f);
        var matDrzwi = r.NextDouble() < .6 ? drzwiStalowe[r.Next(drzwiStalowe.Length)] : drzwiDrewno;
        var bet = W(cx, cz, beton); var betZ = W(cx, cz, betonZbr);
        // fundament (od najniższego punktu terenu do posadzki) — od strony spadku widać beton
        if (yF - dz.yMin > .05f) { bet.kolor = p => Color.white * Mathf.Lerp(.6f, .92f, S01(dz.yMin, yF, p.y)); bet.Box(new Vector3(x0 - .06f, dz.yMin - .5f, z0 - .06f), new Vector3(x1 + .06f, yF - .03f, z1 + .06f)); }
        // chodnik przed domem przy drodze: wylewka betonowa od ściany do krawężnika (poziom krawężnika; na pochyłej ulicy schodkami dom po domu)
        float doDrogi = DoDrog(cx, pn ? z1 : z0, 8f);
        if (doDrogi > .8f && doDrogi < 4.6f)
        {
            float odc = .8f + (float)r.NextDouble() * .2f; var ch = W(cx, cz, placBeton); ch.kolor = p => Color.white * odc * (.9f + .1f * Szum(p.x * .8f, p.z * .8f));
            BoxF(ch, fF, -.12f, L + .12f, dz.yFront - .7f, dz.yFront + .05f, -(doDrogi - .32f), 0f, false, false);
            KolF(Rodzic(cx, cz), fF, -.12f, L + .12f, dz.yFront - .7f, dz.yFront + .05f, -(doDrogi - .32f), 0f);
        }
        bool garaz = !dz.lokal && dz.pas == 2 && L > 6.8f && r.NextDouble() < .22 && DoDrog(cx, pn ? z1 + 1f : z0 - 1f, 10f) < 6f;
        float sLicznik = L - .34f, witS0 = .55f, witS1 = Mathf.Min(.55f + 2.7f * k, L - .7f);
        for (int i = 0; i < n; i++)
        {
            float y0 = yF + i * hf, y1 = y0 + hf; var b = BW(i);
            float pas = i == 0 ? yF + 1.3f : float.NaN;
            // ---- front
            var of = new List<Otw>();
            if (i == 0)
            {
                if (dz.lokal)
                {
                    of.Add(new Otw { r = Rect.MinMaxRect(witS0, y0, witS1, y0 + 2.3f * k), rodzaj = 3 });
                    if (L - witS1 > dw + 1.0f) of.Add(new Otw { r = Rect.MinMaxRect(L - .5f - dw, y0, L - .5f, y0 + dh), rodzaj = 1 });
                }
                else if (garaz)
                {
                    of.Add(new Otw { r = Rect.MinMaxRect(.6f, y0, .6f + 2.5f * k, y0 + 2.25f * k), rodzaj = 4 });
                    of.Add(new Otw { r = Rect.MinMaxRect(L - .55f - dw, y0, L - .55f, y0 + dh), rodzaj = 1 });
                }
                else
                {
                    float ds = .6f + (float)r.NextDouble() * Mathf.Max(0f, L - 1.4f - dw - ow - .7f);
                    of.Add(new Otw { r = Rect.MinMaxRect(ds, y0, ds + dw, y0 + dh), rodzaj = 1 });
                    foreach (float proba in new[] { ds + dw + .7f + ow * .5f, ds - .7f - ow * .5f, ds + dw + 1.9f + ow * .5f, ds - 1.9f - ow * .5f })
                    {
                        float oc = NaSiatce(fF, proba), oks = oc - ow * .5f;
                        if (oks < .4f || oks + ow > L - .4f || (oks < ds + dw + .3f && oks + ow > ds - .3f)) continue;
                        of.Add(new Otw { r = Rect.MinMaxRect(oks, y0 + os, oks + ow, y0 + os + oh), rodzaj = 0 }); break;
                    }
                }
            }
            else
            {
                int nO = Mathf.Clamp(Mathf.FloorToInt(L / 3.1f), 1, 3);
                bool balkon = !(niedok && i == n - 1) && r.NextDouble() < (dz.pas == 1 ? .5 : .3);
                float cPop = -99f, cBalkonu = float.NaN;
                for (int q = 0; q < nO; q++)
                {
                    float c = NaSiatce(fF, L * (q + .5f) / nO);
                    if (c - ow * .5f < .4f || c + ow * .5f > L - .4f || c - cPop < SiatkaOkien - .01f) continue;
                    cPop = c;
                    if (balkon && float.IsNaN(cBalkonu)) { cBalkonu = c; of.Add(new Otw { r = Rect.MinMaxRect(c - dw * .5f, y0, c + dw * .5f, y0 + dh), rodzaj = 2 }); }
                    else of.Add(new Otw { r = Rect.MinMaxRect(c - ow * .5f, y0 + os, c + ow * .5f, y0 + os + oh), rodzaj = 0 });
                }
                if (balkon && !float.IsNaN(cBalkonu)) Balkon(fF, cBalkonu, y0, BW(i), r, cx, cz, kolKraty);
            }
            if (dz.lokal && i == 0) Sciana(b, fF, 0f, L, y0, y1, of.Select(o => o.r).ToList(), pas, .22f, W(cx, cz, wnetrze, new Color(.92f, .9f, .84f)));
            else Sciana(b, fF, 0f, L, y0, y1, of.Select(o => o.r).ToList(), pas, .14f);
            foreach (var o in of) Wypelnij(dz, fF, o, i, r, cx, cz, styl, kolRamy, kolDrzwi, kolKraty, matDrzwi);
            // ---- tył: okna (niedokończone piętro: puste otwory)
            var ot = new List<Otw>(); int nT = Mathf.Max(1, Mathf.FloorToInt(L / 4f));
            float cT = -99f;
            for (int q = 0; q < nT; q++)
            {
                float c = NaSiatce(fB, L * (q + .5f) / nT); if (c - ow * .45f < .4f || c + ow * .45f > L - .4f || c - cT < SiatkaOkien - .01f) continue;
                cT = c; ot.Add(new Otw { r = Rect.MinMaxRect(c - ow * .45f, y0 + os, c + ow * .45f, y0 + os + oh * .9f), rodzaj = 0 });
            }
            b = BW(i); Sciana(b, fB, 0f, L, y0, y1, ot.Select(o => o.r).ToList(), pas, .14f);
            foreach (var o in ot) Wypelnij(dz, fB, o, i, r, cx, cz, styl, kolRamy, kolDrzwi, kolKraty, matDrzwi);
            // ---- boki: okienko łazienki na co drugim piętrze
            int bokNr = 0;
            foreach (var fb in new[] { fL, fP })
            {
                var ob = new List<Otw>();
                float cb = NaSiatce(fb, D * .5f);
                if ((i + bokNr) % 2 == 1 && cb > 1f && cb < D - 1f) ob.Add(new Otw { r = Rect.MinMaxRect(cb - .32f * k, y0 + os + .35f, cb + .32f * k, y0 + os + .35f + .6f * k), rodzaj = 5 });
                b = BW(i); Sciana(b, fb, 0f, D, y0, y1, ob.Select(o => o.r).ToList(), pas, .14f);
                foreach (var o in ob) Wypelnij(dz, fb, o, i, r, cx, cz, 2, new Color(.5f, .5f, .5f), kolDrzwi, kolKraty, matDrzwi);
                bokNr++;
            }
            // ---- strop: pas betonu z prętami widoczny na elewacji (ostatni: płyta dachu)
            betZ.kolor = p => Color.white * (.86f + .14f * Szum(p.x * .5f, p.z * .5f));
            betZ.Box(new Vector3(x0 - .05f, y1 - .22f, z0 - .05f), new Vector3(x1 + .05f, y1, z1 + .05f));
        }
        // żelbetowa rama (słupy narożne 3 cm przed licem) przy cegle i pustaku
        float ySl = typ == 2 ? yF + hf : yF, yGs = yTop + (niedok ? .95f : 0f);
        if (rama)
        {
            bet.kolor = p => Color.white * (.82f + .14f * Szum(p.x * 2f + p.y, p.z * 2f));
            foreach (var (sx, sz) in new[] { (x0 - .03f, z0 - .03f), (x1 - .21f, z0 - .03f), (x0 - .03f, z1 - .21f), (x1 - .21f, z1 - .21f) })
            {
                bet.Box(new Vector3(sx, ySl, sz), new Vector3(sx + .24f, yGs, sz + .24f));
                if (niedok) { var pr = W(cx, cz, metal, new Color(.75f, .45f, .3f)); foreach (var (ax, az) in new[] { (.05f, .05f), (.19f, .05f), (.05f, .19f), (.19f, .19f) }) pr.Cyl(new Vector3(sx + ax, yGs, sz + az), new Vector3(sx + ax + .02f, yGs + .55f + (float)r.NextDouble() * .3f, sz + az), .011f, 4, -1f, false); }
            }
        }
        // ---- dach
        double dl = r.NextDouble();
        if (niedok)
        {   // mur następnego piętra zaczęty na 2–4 warstwy, stos cegieł
            var bm = BW(n - 1); float hm = .25f + (float)r.NextDouble() * .5f;
            bm.Box(new Vector3(x0, yTop, z0), new Vector3(x1, yTop + hm, z0 + .18f)); bm.Box(new Vector3(x0, yTop, z0 + .18f), new Vector3(x0 + .18f, yTop + hm * .7f, z1));
            var st = W(cx, cz, cegla, Color.white); st.Box(new Vector3(cx - .5f, yTop, cz - .3f), new Vector3(cx + .4f, yTop + .55f, cz + .3f));
        }
        else if (dl < (dz.pas == 1 ? .22 : .42))
        {   // blacha falista na drewnianych słupkach nad płytą dachu (spadek ku tyłowi), okap 35 cm
            bool rdza = r.NextDouble() < .65; var bl = W(cx, cz, rdza ? blachaRdza : blachaOcynk, Color.white * (.85f + (float)r.NextDouble() * .15f));
            float hA = 1.05f, hB = .4f;
            bl.Quad2(fF.P(-.35f, yTop + hA, -.35f), fF.P(L + .35f, yTop + hA, -.35f), fF.P(L + .35f, yTop + hB, D + .35f), fF.P(-.35f, yTop + hB, D + .35f));
            var sl = W(cx, cz, drzwiDrewno, new Color(.7f, .62f, .52f));
            foreach (float ss in new[] { .15f, L * .5f, L - .15f })
            {
                BoxF(sl, fF, ss - .05f, ss + .05f, yTop, yTop + hA - .05f, .1f, .2f, true);
                BoxF(sl, fF, ss - .05f, ss + .05f, yTop, yTop + hB - .02f, D - .2f, D - .1f, true);
            }
            BoxF(sl, fF, 0f, L, yTop + hA - .1f, yTop + hA - .02f, .08f, .2f, true); BoxF(sl, fF, 0f, L, yTop + hB - .08f, yTop + hB - .01f, D - .2f, D - .08f, true);
        }
        else
        {   // attyka (murek dachu) w wykończeniu ostatniego piętra, z betonową czapką
            var at = BW(n - 1); float ha = .55f + (float)r.NextDouble() * .4f;
            at.Box(new Vector3(x0, yTop, z0), new Vector3(x1, yTop + ha, z0 + .15f)); at.Box(new Vector3(x0, yTop, z1 - .15f), new Vector3(x1, yTop + ha, z1));
            at.Box(new Vector3(x0, yTop, z0 + .15f), new Vector3(x0 + .15f, yTop + ha, z1 - .15f)); at.Box(new Vector3(x1 - .15f, yTop, z0 + .15f), new Vector3(x1, yTop + ha, z1 - .15f));
            if (r.NextDouble() < .35 && L > 6f)
            {   // sznur z praniem na dachu
                var sz = W(cx, cz, metal, new Color(.6f, .6f, .6f)); float zl = cz + ((float)r.NextDouble() - .5f) * (D - 3f), yl = yTop + 1.75f;
                sz.Cyl(new Vector3(x0 + .6f, yTop, zl), new Vector3(x0 + .6f, yl + .05f, zl), .02f, 5); sz.Cyl(new Vector3(x1 - .6f, yTop, zl), new Vector3(x1 - .6f, yl + .05f, zl), .02f, 5);
                Pranie(new Vector3(x0 + .6f, yl, zl), new Vector3(x1 - .6f, yl, zl), r, cx, cz);
            }
            if (dz.pas == 2 && r.NextDouble() < .22 && L > 6.5f)
            {   // dobudówka na dachu z blaszanym daszkiem
                var db = BW(n - 1); float ax = x0 + .5f, az = pn ? z0 + .5f : z1 - 2.9f;
                db.Box(new Vector3(ax, yTop, az), new Vector3(ax + 2.6f, yTop + 2.3f, az + 2.4f));
                W(cx, cz, blachaRdza, Color.white).Box(new Vector3(ax - .2f, yTop + 2.3f, az - .2f), new Vector3(ax + 2.8f, yTop + 2.36f, az + 2.6f), true);
                W(cx, cz, drzwiDrewno, kolDrzwi).Box(new Vector3(ax + 2.6f, yTop, az + .7f), new Vector3(ax + 2.63f, yTop + 1.95f, az + 1.6f));
            }
        }
        // zbiornik na wodę (niebieski lakier albo szary fibrocement) na podstawie z pustaków
        if (r.NextDouble() < .72)
        {
            float tx = Mathf.Lerp(x0 + 1.1f, x1 - 1.1f, (float)r.NextDouble()), tz = Mathf.Lerp(z0 + 1.1f, z1 - 1.1f, (float)r.NextDouble()), ty = yTop + (niedok ? .0f : .0f);
            W(cx, cz, pustak, Color.white).Box(new Vector3(tx - .5f, ty, tz - .5f), new Vector3(tx + .5f, ty + .38f, tz + .5f));
            bool nieb = r.NextDouble() < .7; var zb = W(cx, cz, nieb ? zbiornik : beton, nieb ? Color.white : new Color(.9f, .9f, .9f));
            zb.Cyl(new Vector3(tx, ty + .38f, tz), new Vector3(tx, ty + .38f + .78f * k, tz), .5f * k, 14, .56f * k);
            zb.Cyl(new Vector3(tx, ty + .38f + .78f * k, tz), new Vector3(tx, ty + .38f + .9f * k, tz), .58f * k, 14, .22f * k);
            W(cx, cz, plastik, new Color(.75f, .75f, .75f)).Cyl(new Vector3(tx + .45f * k, ty + .5f, tz), new Vector3(tx + .45f * k, ty + .05f, tz + .9f), .03f, 5, -1f, false);
        }
        // talerz satelitarny, antena
        if (r.NextDouble() < .38)
        {
            var tal = W(cx, cz, alu, new Color(.95f, .95f, .95f)); float sx = x0 + .4f + (float)r.NextDouble() * (L - .8f); var p0 = new Vector3(sx, yTop, pn ? z1 - .25f : z0 + .25f);
            tal.Cyl(p0, p0 + Vector3.up * .9f, .022f, 5);
            var os2 = new Vector3(.35f, .55f, pn ? .75f : -.75f).normalized; tal.Cyl(p0 + Vector3.up * .9f, p0 + Vector3.up * .9f + os2 * .05f, .4f * k, 12, .36f * k);
            tal.Cyl(p0 + Vector3.up * .9f, p0 + Vector3.up * .9f + os2 * .42f, .012f, 4, -1f, false);
        }
        if (r.NextDouble() < .3)
        {
            var an = W(cx, cz, alu, new Color(.8f, .8f, .8f)); var p0 = new Vector3(x1 - .5f, yTop, cz + ((float)r.NextDouble() - .5f) * 2f);
            an.Cyl(p0, p0 + Vector3.up * 2.6f, .016f, 4, -1f, false);
            for (int q = 0; q < 4; q++) an.Cyl(p0 + new Vector3(-.4f + q * .05f, 1.7f + q * .25f, 0f), p0 + new Vector3(.4f - q * .05f, 1.7f + q * .25f, 0f), .008f, 3, -1f, false);
        }
        // skrzynka licznika z rurą osłonową do dachu (przyłącze prądu), rura spustowa, klimatyzator, daszek nad drzwiami
        {
            var skrz = W(cx, cz, metal, new Color(.72f, .72f, .7f)); float ys = yF + 1.35f * k;
            BoxF(skrz, fF, sLicznik - .3f, sLicznik, ys, ys + .42f, -.13f, 0f);
            W(cx, cz, plastik, new Color(.6f, .6f, .6f)).Cyl(fF.P(sLicznik - .15f, ys + .42f, -.05f), fF.P(sLicznik - .15f, yTop - .25f, -.05f), .022f, 5, -1f, false);
            Przylacza.Add(fF.P(sLicznik - .15f, yTop - .25f, -.08f));
        }
        if (r.NextDouble() < .45) W(cx, cz, plastik, r.Next(2) == 0 ? new Color(.82f, .82f, .8f) : new Color(.55f, .4f, .3f)).Cyl(fF.P(.14f, yTop, -.08f), fF.P(.14f, dz.yFront - .1f, -.08f), .05f, 6, -1f, false);
        if (n > 1 && r.NextDouble() < (dz.pas == 1 ? .4 : .2))
        {
            float sa = L * .5f + ((float)r.NextDouble() - .5f) * .8f, ya = yF + hf + .28f; var kl = W(cx, cz, plastik, new Color(.93f, .93f, .9f));
            BoxF(kl, fF, sa - .42f, sa + .42f, ya, ya + .56f, -.3f, 0f);
            W(cx, cz, metal, new Color(.35f, .35f, .35f)).Cyl(fF.P(sa - .1f, ya + .28f, -.31f), fF.P(sa - .1f, ya + .28f, -.3f), .2f, 10);
            BoxF(W(cx, cz, metal, new Color(.5f, .5f, .5f)), fF, sa - .45f, sa + .45f, ya - .04f, ya, -.32f, 0f);
        }
        // graffiti na ślepej ścianie boku parteru
        if (matGraffiti && r.NextDouble() < .34 && D > 6f) Naklejka(fL, D * .5f - 1.7f, D * .5f + 1.7f, yF + .4f, yF + .4f + 1.7f * k * .8f, r.Next(16), cx, cz);
        var rd = Rodzic(cx, cz);
        if (dz.lokal) Lokal(dz, rd, fF, n, witS0, witS1, r);
        else Kol(rd, new Vector3(x0, dz.yMin - .3f, z0), new Vector3(x1, yTop + .5f, z1));
        domow++; if (dz.pas == 1) domowPas1++; else domowPas2++;
    }

    static void Pranie(Vector3 a, Vector3 b, System.Random r, float cx, float cz)
    {
        W(cx, cz, kabel, Color.white).Cyl(a, b, .006f, 3, -1f, false);
        var dir = (b - a); float len = dir.magnitude; dir /= Mathf.Max(.01f, len);
        var pr = W(cx, cz, plotno);
        for (float s = .35f; s < len - .5f; s += .55f + (float)r.NextDouble() * .25f)
        {
            var kol = Color.HSVToRGB((float)r.NextDouble(), r.NextDouble() < .3 ? .05f : .55f, .92f); pr.K(kol);
            float w = .32f + (float)r.NextDouble() * .2f, h = .45f + (float)r.NextDouble() * .45f; var p = a + dir * s;
            pr.Quad2(p + dir * -w * .5f + Vector3.down * h, p + dir * w * .5f + Vector3.down * h, p + dir * w * .5f, p + dir * -w * .5f);
        }
    }

    // balkon: płyta betonowa z prętami, balustrada z prętów albo murek, czasem pranie
    static void Balkon(Fr F, float c, float y0, Bud mur, System.Random r, float cx, float cz, Color kolKraty)
    {
        float w = 1.15f * K, g = .95f * K, hb = .95f * K;
        var pl = W(cx, cz, betonZbr, Color.white * .92f); BoxF(pl, F, c - w, c + w, y0 - .16f, y0 + .01f, -g, 0f);
        if (r.NextDouble() < .45)
        {   // murek w wykończeniu domu
            BoxF(mur, F, c - w, c + w, y0 + .01f, y0 + hb, -g, -g + .12f, true);
            BoxF(mur, F, c - w, c - w + .12f, y0 + .01f, y0 + hb, -g + .12f, 0f, true); BoxF(mur, F, c + w - .12f, c + w, y0 + .01f, y0 + hb, -g + .12f, 0f, true);
        }
        else
        {
            var bal = W(cx, cz, metal, kolKraty);
            BoxF(bal, F, c - w, c + w, y0 + hb - .05f, y0 + hb, -g, -g + .05f, true);
            BoxF(bal, F, c - w, c - w + .04f, y0 + hb - .05f, y0 + hb, -g + .05f, 0f, true); BoxF(bal, F, c + w - .04f, c + w, y0 + hb - .05f, y0 + hb, -g + .05f, 0f, true);
            for (float s = c - w + .02f; s <= c + w - .03f; s += .14f) bal.Quad2(F.P(s, y0 + .01f, -g + .02f), F.P(s + .022f, y0 + .01f, -g + .02f), F.P(s + .022f, y0 + hb - .05f, -g + .02f), F.P(s, y0 + hb - .05f, -g + .02f));
            foreach (float s in new[] { c - w + .02f, c + w - .02f })
                for (float d = -g + .16f; d < -.05f; d += .16f) bal.Quad2(F.P(s, y0 + .01f, d), F.P(s, y0 + .01f, d + .022f), F.P(s, y0 + hb - .05f, d + .022f), F.P(s, y0 + hb - .05f, d));
        }
        if (r.NextDouble() < .5) Pranie(F.P(c - w + .1f, y0 + hb + .55f, -g * .6f), F.P(c + w - .1f, y0 + hb + .55f, -g * .6f), r, cx, cz);
    }

    // wypełnienie otworu: okno (zasłona, szyba, rama, parapet, okiennice, krata), drzwi (skrzydło, ościeżnica, próg, klamka, krata), brama garażu
    internal static int FazaOkna(float yF) { float q = Hf / 12f; return ((Mathf.RoundToInt(yF / q) % 12) + 12) % 12; }

    static void Wypelnij(Dzialka dz, Fr F, Otw ot, int pietro, System.Random r, float cx, float cz, int styl, Color kolRamy, Color kolDrzwi, Color kolKraty, Material matDrzwi)
    {
        var o = ot.r; float g = .11f;
        if (ot.rodzaj == 3) return;   // front lokalu: otwarty (roleta i wnętrze — Lokal)
        if (ot.rodzaj == 4)
        {   // brama garażu (deski albo roleta) i betonowy podjazd
            int br2 = r.Next(3); Pasek(W(cx, cz, br2 == 0 ? bramaGaraz : br2 == 1 ? roleta : roletaSzara, br2 == 2 ? Color.Lerp(Color.white, kolDrzwi, .6f) : Color.white), F, o.xMin, o.xMax, o.yMin, o.yMax, .09f);
            var pj = W(cx, cz, beton, Color.white * .88f); float yl = dz.yFront - .12f;
            pj.Quad(F.P(o.xMin - .15f, yl, -1.4f), F.P(o.xMax + .15f, yl, -1.4f), F.P(o.xMax + .15f, o.yMin, 0f), F.P(o.xMin - .15f, o.yMin, 0f));
            pj.Quad(F.P(o.xMin - .15f, yl, 0f), F.P(o.xMin - .15f, yl, -1.4f), F.P(o.xMin - .15f, o.yMin, 0f), F.P(o.xMin - .15f, o.yMin, 0f));
            pj.Quad(F.P(o.xMax + .15f, yl, -1.4f), F.P(o.xMax + .15f, yl, 0f), F.P(o.xMax + .15f, o.yMin, 0f), F.P(o.xMax + .15f, o.yMin, 0f));
            return;
        }
        if (ot.rodzaj == 1)
        {
            bool stalowe = matDrzwi != drzwiDrewno; var sk = W(cx, cz, matDrzwi, stalowe ? Color.white : Color.Lerp(Color.white, kolDrzwi, .3f));
            Pasek(sk, F, o.xMin + .05f, o.xMax - .05f, o.yMin + .02f, o.yMax - .05f, g);
            var os = W(cx, cz, stalowe ? metal : drzwiDrewno, stalowe ? new Color(.55f, .55f, .55f) : Color.Lerp(Color.white, kolDrzwi, .5f) * .8f);   // ościeżnica
            Pasek(os, F, o.xMin, o.xMin + .05f, o.yMin, o.yMax, g - .02f); Pasek(os, F, o.xMax - .05f, o.xMax, o.yMin, o.yMax, g - .02f); Pasek(os, F, o.xMin, o.xMax, o.yMax - .05f, o.yMax, g - .02f);
            BoxF(W(cx, cz, alu, new Color(.75f, .72f, .6f)), F, o.xMax - .2f, o.xMax - .12f, o.yMin + 1.0f * K, o.yMin + 1.0f * K + .12f, g - .05f, g);   // klamka z szyldem
            // próg i stopień przed drzwiami (od terenu do posadzki)
            if (pietro == 0) BoxF(W(cx, cz, beton, Color.white * .9f), F, o.xMin - .25f, o.xMax + .25f, dz.yFront - .4f, o.yMin, -.55f, 0f, false, false);
            if (pietro == 0 && r.NextDouble() < .3)
            {   // krata przed drzwiami
                var kr = W(cx, cz, metal, kolKraty);
                for (float s = o.xMin + .06f; s < o.xMax - .05f; s += .13f) Pasek(kr, F, s, s + .02f, o.yMin + .03f, o.yMax - .06f, .02f);
                foreach (float yy in new[] { o.yMin + .25f, (o.yMin + o.yMax) * .5f, o.yMax - .3f }) Pasek(kr, F, o.xMin + .05f, o.xMax - .05f, yy, yy + .025f, .018f);
            }
            if (pietro == 0 && r.NextDouble() < .3)
            {   // daszek z blachy nad drzwiami na dwóch wspornikach
                var bl = W(cx, cz, blachaRdza, Color.white); float yd = o.yMax + .3f;
                bl.Quad2(F.P(o.xMin - .3f, yd, 0f), F.P(o.xMax + .3f, yd, 0f), F.P(o.xMax + .3f, yd - .28f, -.95f), F.P(o.xMin - .3f, yd - .28f, -.95f));
                var ws = W(cx, cz, metal, new Color(.4f, .4f, .4f));
                foreach (float s in new[] { o.xMin - .2f, o.xMax + .2f }) ws.Cyl(F.P(s, yd - .55f, 0f), F.P(s, yd - .27f, -.9f), .014f, 4, -1f, false);
            }
            return;
        }
        // okna i drzwi balkonu: SZYBA Z POKOJEM ZA NIĄ (właściciel 9.11 23:07: „szyby to chociaż mogłyby być”) — ten sam shader co okna
        // kamienic miasta (Braxhorn/UliceOkno: odbicie nieba, pokój z meblami, firanki w części okien), rama, szprosy
        Pasek(W(cx, cz, okna[FazaOkna(dz.yF)], Color.white), F, o.xMin, o.xMax, o.yMin, o.yMax, g);
        var br = W(cx, cz, styl == 0 ? alu : styl == 1 ? okiennica : metal, kolRamy);
        float f = styl == 2 ? .035f : .055f, mid = (o.xMin + o.xMax) * .5f;
        void Rama(float s0, float s1, float y0, float y1) => BoxF(br, F, s0, s1, y0, y1, g - .045f, g, false, false);   // rama z głębią 4.5 cm przed szybą
        Rama(o.xMin, o.xMin + f, o.yMin, o.yMax); Rama(o.xMax - f, o.xMax, o.yMin, o.yMax); Rama(o.xMin + f, o.xMax - f, o.yMax - f, o.yMax); Rama(o.xMin + f, o.xMax - f, o.yMin, o.yMin + f);
        if (ot.rodzaj != 5) Rama(mid - f * .5f, mid + f * .5f, o.yMin + f, o.yMax - f);
        if (styl == 2 && ot.rodzaj == 0) foreach (float t in new[] { .34f, .67f }) Rama(o.xMin + f, o.xMax - f, Mathf.Lerp(o.yMin, o.yMax, t) - .012f, Mathf.Lerp(o.yMin, o.yMax, t) + .012f);
        if (ot.rodzaj == 2) return;
        BoxF(W(cx, cz, beton, Color.white * .9f), F, o.xMin - .07f, o.xMax + .07f, o.yMin - .07f, o.yMin, -.07f, .1f);   // parapet
        if (styl == 1 && ot.rodzaj == 0 && pietro > 0 && r.NextDouble() < .5)
        {   // okiennice otwarte na ścianę
            var bo = W(cx, cz, okiennica, kolRamy); float w2 = o.width * .5f;
            BoxF(bo, F, o.xMin - w2, o.xMin - .01f, o.yMin, o.yMax, -.04f, 0f); BoxF(bo, F, o.xMax + .01f, o.xMax + w2, o.yMin, o.yMax, -.04f, 0f);
        }
        if (pietro == 0 || (dz.pas == 2 && r.NextDouble() < .3))
        {   // krata w ościeżu
            var kr = W(cx, cz, metal, kolKraty); int nb = Mathf.Max(3, Mathf.RoundToInt(o.width / .16f));
            for (int q = 1; q < nb; q++) { float s = Mathf.Lerp(o.xMin, o.xMax, q / (float)nb); Pasek(kr, F, s - .011f, s + .011f, o.yMin, o.yMax, .03f); }
            foreach (float t in new[] { .25f, .75f }) Pasek(kr, F, o.xMin, o.xMax, Mathf.Lerp(o.yMin, o.yMax, t) - .012f, Mathf.Lerp(o.yMin, o.yMax, t) + .012f, .028f);
        }
    }

    // ---------------------------------------------------------------------------- LOKAL w parterze
    static void Lokal(Dzialka dz, Transform rd, Fr F, int n, float w0, float w1, System.Random r)
    {
        float cx = (dz.x0 + dz.x1) * .5f, cz = (dz.z0 + dz.z1) * .5f, yF = dz.yF, hf = Hf, k = K, L = F.L, gl = dz.z1 - dz.z0;
        float G = .22f, D = Mathf.Min(gl - .22f, 5.8f), ys = yF + hf - .22f; string rodzaj = dz.rodzaj;
        bool bar = rodzaj == "BAR", apteka = rodzaj == "APTEKA", mieso = rodzaj.StartsWith("MIĘSO"), warzywa = rodzaj.StartsWith("WARZYWA"), piekarnia = rodzaj == "PIEKARNIA";
        bool fryzjer = rodzaj == "FRYZJER", pralnia = rodzaj == "PRALNIA", wulk = rodzaj == "WULKANIZACJA", przych = rodzaj == "PRZYCHODNIA", loteria = rodzaj.StartsWith("LOTERIA"), tel = rodzaj.StartsWith("NAPRAWA");
        // posadzka (płytki / linoleum / beton w wulkanizacji) i próg-rampa przed frontem
        var pos = W(cx, cz, wulk ? placBeton : (apteka || mieso || piekarnia || przych) ? plytki : (bar || warzywa) ? plytkiCiemne : linoleum, Color.white);
        pos.Quad(F.P(G, yF, 0f), F.P(L - G, yF, 0f), F.P(L - G, yF, D), F.P(G, yF, D));
        var rampa = W(cx, cz, beton, Color.white * .9f);
        rampa.Quad(F.P(w0 - .1f, dz.yFront - .1f, -.9f), F.P(w1 + .1f, dz.yFront - .1f, -.9f), F.P(w1 + .1f, yF, 0f), F.P(w0 - .1f, yF, 0f));
        BraxRegionMeble.Kolizja(rd, "Fawela: rampa lokalu", (F.P(w0 - .1f, dz.yFront - .1f, -.9f) + F.P(w1 + .1f, yF, 0f)) * .5f + Vector3.down * .06f, new Vector3(w1 - w0 + .2f, .1f, 1.0f),
            Quaternion.LookRotation(F.N, Vector3.up) * Quaternion.Euler(Mathf.Atan2(yF - dz.yFront + .1f, .9f) * Mathf.Rad2Deg, 0f, 0f));
        // okładzina ścian: farba łusząca się + lamperia (ciemniejszy pas do 1.2 m × K); mięsny i apteka — płytki
        var jasny = (apteka || mieso || przych) ? new Color(.95f, .95f, .93f) : Color.Lerp(Color.white, Pastele[r.Next(Pastele.Length)], .45f);
        var okl = W(cx, cz, (mieso || apteka) ? plytki : wnetrze); float yl2 = yF + 1.2f * k;
        void Okl(Vector3 a, Vector3 b, float ya, float yb2) { okl.Quad(new Vector3(a.x, ya, a.z), new Vector3(b.x, ya, b.z), new Vector3(b.x, yb2, b.z), new Vector3(a.x, yb2, a.z)); }
        foreach (var (ya, yb2, kol) in new[] { (yF, yl2, jasny * .72f), (yl2, ys, jasny) })
        {
            okl.K(kol);
            Okl(F.P(G, 0f, G), F.P(G, 0f, D), ya, yb2);            // ściana lewa (patrzy w +R)
            Okl(F.P(L - G, 0f, D), F.P(L - G, 0f, G), ya, yb2);    // ściana prawa
            Okl(F.P(G, 0f, D), F.P(L - G, 0f, D), ya, yb2);        // ścianka zaplecza (patrzy na front)
        }
        var suf = W(cx, cz, wnetrze, new Color(.9f, .89f, .85f)); suf.Quad(F.P(G, ys - .03f, D), F.P(L - G, ys - .03f, D), F.P(L - G, ys - .03f, 0f), F.P(G, ys - .03f, 0f));
        BoxF(W(cx, cz, zarowka, Color.white), F, L * .5f - .65f, L * .5f + .65f, ys - .1f, ys - .03f, 2.0f, 2.12f, true);
        // drzwi na zaplecze w ściance
        Pasek(W(cx, cz, drzwiDrewno, new Color(.75f, .7f, .62f)), F, L - G - 1.3f, L - G - .35f, yF, yF + 2.05f * k, D - .012f);
        // roleta zwinięta nad frontem (skrzynka), prowadnice
        var rol = W(cx, cz, roleta, Color.white); float yo = yF + 2.3f * k;
        BoxF(rol, F, w0 - .06f, w1 + .06f, yo - .32f, yo + .02f, -.1f, G, true);
        foreach (float s in new[] { w0, w1 - .05f }) BoxF(W(cx, cz, metal, new Color(.5f, .5f, .5f)), F, s, s + .05f, yF, yo - .32f, .02f, .07f, true);
        // szyld-bryła 3D nad frontem (30 cm, boki ciemny metal, lico malowane) + litery 3D
        float sy0 = yo + .22f, sy1 = Mathf.Min(sy0 + .62f * k, yF + hf - .3f), sx0 = Mathf.Max(.2f, w0 - .3f), sx1 = Mathf.Min(L - .2f, w1 + .3f);
        BoxF(W(cx, cz, metal, new Color(.16f, .16f, .17f)), F, sx0, sx1, sy0, sy1, -.3f, 0f);
        // WŁAŚCICIEL 9.11 23:04: „zakaz takich napisów białych” — szyld malowany ręcznie: tablica w kolorze farby, litery w kontrastowej farbie (nigdy białe)
        var (kolSz, kolLit) = apteka || przych ? (new Color(.90f, .86f, .70f), new Color(.05f, .45f, .2f)) : ParySzyldow[dz.nr % ParySzyldow.Length];
        Pasek(W(cx, cz, plastik, kolSz), F, sx0 + .03f, sx1 - .03f, sy0 + .03f, sy1 - .03f, -.305f);
        float wl = Mathf.Min(.3f * k, (sx1 - sx0 - .3f) / (rodzaj.Length * .62f));
        BraxRegionMeble.Napis(rd, rodzaj, F.P((sx0 + sx1) * .5f, (sy0 + sy1) * .5f, -.32f), F.N, wl, kolLit);
        if (apteka || przych)
        {   // krzyż świecący na wysięgniku
            var kr = W(cx, cz, neonZielony, Color.white); float ks = sx1 - .1f, ky = sy1 + .45f;
            BoxF(kr, F, ks - .3f, ks + .3f, ky - .1f, ky + .1f, -.5f, -.42f, true); BoxF(kr, F, ks - .1f, ks + .1f, ky - .3f, ky + .3f, -.5f, -.42f, true);
            BoxF(W(cx, cz, metal, new Color(.3f, .3f, .3f)), F, ks - .03f, ks + .03f, ky - .03f, ky + .03f, -.42f, 0f);
        }
        // markiza w pasy (płótno) na dwóch prętach
        {
            var mk = W(cx, cz, plotno); var k1 = Color.HSVToRGB((dz.nr * .291f) % 1f, .7f, .8f); var k2 = new Color(.93f, .91f, .84f); int pasow = 8; float ym = yo + .15f;
            for (int q = 0; q < pasow; q++)
            {
                float a = Mathf.Lerp(w0 - .2f, w1 + .2f, q / (float)pasow), b = Mathf.Lerp(w0 - .2f, w1 + .2f, (q + 1) / (float)pasow); mk.K(q % 2 == 0 ? k1 : k2);
                mk.Quad2(F.P(a, ym, 0f), F.P(b, ym, 0f), F.P(b, ym - .55f, -1.5f), F.P(a, ym - .55f, -1.5f));
                mk.Quad2(F.P(a, ym - .55f, -1.5f), F.P(b, ym - .55f, -1.5f), F.P(b, ym - .78f, -1.5f), F.P(a, ym - .78f, -1.5f));
            }
            var pr = W(cx, cz, metal, new Color(.45f, .45f, .45f));
            foreach (float s in new[] { w0 - .15f, w1 + .15f }) pr.Cyl(F.P(s, ym - .9f, 0f), F.P(s, ym - .55f, -1.48f), .014f, 4, -1f, false);
        }
        // lada (drewno wytarte) — sprzedawca za nią, przodem do wejścia
        float l0 = Mathf.Max(G + .5f, w0 + .2f), l1 = Mathf.Min(l0 + 2.4f, L - G - 1.5f), ld = 2.5f;
        var bl = W(cx, cz, lada, Color.white);
        if (!fryzjer && !pralnia && !wulk)
        {
            BoxF(bl, F, l0, l1, yF, yF + .92f * k, ld, ld + .6f, true, false);
            BoxF(W(cx, cz, lada, new Color(.8f, .78f, .74f)), F, l0 - .04f, l1 + .04f, yF + .92f * k, yF + .96f * k, ld - .05f, ld + .65f, true);
            KolF(rd, F, l0, l1, yF, yF + .96f * k, ld, ld + .6f);
            BoxF(W(cx, cz, metal, new Color(.3f, .3f, .32f)), F, l1 - .55f, l1 - .15f, yF + .96f * k, yF + .96f * k + .28f, ld + .12f, ld + .5f, true);   // kasa
        }
        Sprzedawcy.Add((F.P((l0 + l1) * .5f, yF, ld + 1.35f), F.Yaw, rodzaj.ToLowerInvariant()));
        // TOWAR Z BIBLIOTEKI (właściciel 9.11 23:03: „dlaczego produktów do sklepów nie bierzesz z naszej biblioteki… mamy etykiety”):
        // te same modele co w sklepach ulicy i supermarkecie (BraxSklepyTowar — pudełka, puszki, butelki, kartony, leki, wędliny,
        // sery, owoce, pieczywo z etykietami właściciela + Poly Haven). Brak modelu = luka na półce i wpis w logu, nigdy klocek.
        string[] typPolek = rodzaj == "SKLEP SPOŻYWCZY" ? new[] { "sm_g5", "market", "sm_g3", "sm_g4" } : apteka ? new[] { "apteka", "apteka", "apteka", "apteka" }
            : piekarnia ? new[] { "piekarnia", "piekarnia", "cukiernia", "piekarnia" } : bar ? new[] { "bar", "sm_g4", "bar", "sm_g7" } : warzywa ? new[] { "warzywniak", "warzywniak", "sm_g3", "warzywniak" }
            : mieso ? new[] { "ch_dol", "ch_srodek", "ch_srodek", "ch_gora" } : rodzaj.StartsWith("WSZYSTKO") ? new[] { "sm_g8", "sm_g8", "sm_g7", "sm_g4" } : loteria ? new[] { "sm_g6", "sm_g6", "sm_g4", "sm_g6" } : null;
        string typLady = rodzaj == "SKLEP SPOŻYWCZY" ? "market" : apteka ? "apteka" : piekarnia ? "piekarnia" : bar ? "bar" : warzywa ? "warzywniak" : null;
        Transform tw = typPolek != null || przych ? BraxSklepyTowar.Korzen(rd, "FAWELA " + rodzaj + " " + dz.nr) : null;
        // regały pod ścianką zaplecza (4 półki) z towarem; lodówka z napojami
        float rg0 = G + .15f, rg1 = L - G - 1.5f;
        if (!fryzjer && !pralnia && !wulk && !przych)
        {
            foreach (float s in new[] { rg0, (rg0 + rg1) * .5f - .02f, rg1 - .04f }) BoxF(bl, F, s, s + .04f, yF, yF + 2.0f * k, D - .42f, D - .02f, true);
            for (int p = 0; p < 4; p++)
            {
                float y = yF + .12f + p * .5f * k; bl.K(Color.white); BoxF(bl, F, rg0, rg1, y, y + .035f, D - .42f, D - .02f, true);
                BoxF(W(cx, cz, lada, new Color(.8f, .78f, .74f)), F, rg0, rg1, y, y + .5f * k, D - .03f, D - .012f);   // plecy regału
                if (typPolek != null)
                {   // ubogi sklep: półki zastawione w 60–90 %, od losowego końca
                    float dlP = (rg1 - rg0 - .2f) * (.6f + (float)r.NextDouble() * .3f), odP = r.Next(2) == 0 ? rg0 + .1f : rg1 - .1f - dlP;
                    BraxSklepyTowar.Polka(typPolek[p], tw, F.P(odP, y + .039f, D - .22f), F.R, -F.N, dlP, r, .5f * k - .07f);
                }
                else for (float s = rg0 + .08f; s < rg1 - .28f; s += .27f) Towar(F, s, y + .035f, D - .36f, r, cx, cz, tel ? 2 : 0);   // naprawa telefonów: pudełka z akcesoriami (biblioteka nie ma elektroniki)
            }
            var lodM = !apteka && BraxMiastoAssety.Jest("miasto_sklepy") ? BraxMiastoAssety.Model("miasto_sklepy", "lodowka") : null;
            if (lodM)
            {   // lodówka z biblioteki właściciela (0.85 × 0.94 × 2.18 m × K) pod prawą ścianą, frontem do środka sklepu
                float fs = L - G - .944f * k * .5f - .03f, fd = G + 1.1f;
                BraxMiastoAssety.Postaw(tw ? tw : rd, lodM, "Fawela: lodówka (model właściciela)", F.P(fs, yF, fd), Mathf.Atan2(F.R.x, F.R.z) * Mathf.Rad2Deg, 1f, true, false);
                KolF(rd, F, fs - .5f * k, L - G, yF, yF + 2.18f * k, fd - .45f * k, fd + .45f * k);
            }
            else if (!apteka)
            {
                float f0 = L - G - 1.25f; BoxF(W(cx, cz, plastik, new Color(.9f, .9f, .9f)), F, f0, f0 + .8f, yF, yF + 1.95f * k, .5f + G, 1.2f + G, true);
                Pasek(W(cx, cz, lodowka, Color.white), F, f0 + .06f, f0 + .74f, yF + .3f, yF + 1.85f * k, .5f + G - .004f);   // podświetlony front (widać od wejścia? — tył do frontu: front lodówki patrzy na wejście)
                KolF(rd, F, f0, f0 + .8f, yF, yF + 1.95f * k, .5f + G, 1.2f + G);
            }
        }
        // wyposażenie wg branży
        if (bar)
        {   // hokery przy ladzie, stolik i krzesła z tworzywa pod markizą
            var st = W(cx, cz, metal, new Color(.5f, .5f, .5f)); var sd = W(cx, cz, skora, Color.white);
            for (float s = l0 + .35f; s < l1 - .2f; s += .75f) { st.Cyl(F.P(s, yF, ld - .45f), F.P(s, yF + .7f * k, ld - .45f), .03f, 6); sd.Cyl(F.P(s, yF + .7f * k, ld - .45f), F.P(s, yF + .76f * k, ld - .45f), .19f, 10); }
            StolikZKrzeslami(F, w1 + .9f < L - .6f ? w1 + .7f : w0 + .8f, dz.yFront - .02f, -1.7f, r, cx, cz, rd);
        }
        if (warzywa)
        {   // skośne skrzynki z owocami i warzywami przed frontem i w środku
            for (int q = 0; q < 4; q++)
            {
                float s = w0 + .25f + q * .8f; if (s + .7f > w1) break; bool zewn = q % 2 == 0; float d0 = zewn ? -.75f : .5f, yb = zewn ? dz.yFront : yF;
                BoxF(bl, F, s, s + .7f, yb, yb + .55f, d0, d0 + .5f, true);
                BraxSklepyTowar.Polka("warzywniak", tw, F.P(s + .02f, yb + .555f, d0 + .25f), F.R, -F.N, .68f, r);
            }
        }
        if (mieso) BraxSklepyTowar.Polka("ch_dol", tw, F.P(l0 + .15f, yF + .965f * k, ld + .28f), F.R, -F.N, l1 - l0 - .95f, r, .3f);
        if (piekarnia || mieso || tel || apteka)
        {   // szklana gablota na ladzie
            Pasek(W(cx, cz, szyba, Color.white), F, l0 + .1f, l1 - .7f, yF + .96f * k, yF + .96f * k + .32f, ld + .02f);
            BoxF(W(cx, cz, alu, Color.white), F, l0 + .1f, l1 - .7f, yF + .96f * k + .32f, yF + .96f * k + .345f, ld, ld + .5f, true);
            if (tel) for (float s = l0 + .2f; s < l1 - .9f; s += .22f) BoxF(W(cx, cz, plastik, new Color(.1f, .1f, .12f)), F, s, s + .14f, yF + .96f * k, yF + .96f * k + .02f, ld + .15f, ld + .38f, true);
        }
        if (fryzjer)
        {   // dwa fotele przed lustrami na lewej ścianie, półka, ławka dla czekających
            for (int q = 0; q < 2; q++)
            {
                float d = 1.4f + q * 1.9f; var ft = W(cx, cz, skora, Color.white);
                BoxF(ft, F, G + .9f, G + 1.5f, yF + .45f * k, yF + .55f * k, d, d + .6f, true); BoxF(ft, F, G + 1.42f, G + 1.52f, yF + .55f * k, yF + 1.15f * k, d, d + .6f, true);
                W(cx, cz, metal, new Color(.6f, .6f, .6f)).Cyl(F.P(G + 1.2f, yF, d + .3f), F.P(G + 1.2f, yF + .45f * k, d + .3f), .05f, 8);
                W(cx, cz, lustro, Color.white).Quad(F.P(G + .012f, yF + .9f * k, d - .1f), F.P(G + .012f, yF + .9f * k, d + .7f), F.P(G + .012f, yF + 1.85f * k, d + .7f), F.P(G + .012f, yF + 1.85f * k, d - .1f));
                BoxF(bl, F, G, G + .3f, yF + .82f * k, yF + .86f * k, d - .2f, d + .8f, true);
                KolF(rd, F, G + .9f, G + 1.52f, yF, yF + 1.15f * k, d, d + .6f);
            }
            BoxF(bl, F, L - G - 1.9f, L - G - .2f, yF + .42f * k, yF + .47f * k, .6f, 1.0f, true);
        }
        if (pralnia)
        {   // pralki pod lewą ścianą, stół do składania
            for (int q = 0; q < 4; q++)
            {
                float d = .7f + q * .85f; BoxF(W(cx, cz, plastik, new Color(.93f, .93f, .92f)), F, G + .02f, G + .7f, yF, yF + .9f * k, d, d + .72f, true);
                W(cx, cz, szyba, Color.white).Cyl(F.P(G + .7f, yF + .5f * k, d + .36f), F.P(G + .715f, yF + .5f * k, d + .36f), .2f, 12);
            }
            KolF(rd, F, G, G + .7f, yF, yF + .9f * k, .7f, 4.1f);
            BoxF(bl, F, L - G - 2.2f, L - G - .3f, yF + .85f * k, yF + .9f * k, 2.2f, 3.0f, true); KolF(rd, F, L - G - 2.2f, L - G - .3f, yF, yF + .9f * k, 2.2f, 3.0f);
        }
        if (wulk)
        {   // sprężarka, stosy opon (koła z modeli aut — krok ruchu), stół z narzędziami
            var sp = W(cx, cz, drzwiStalowe[2], Color.white); sp.Cyl(F.P(G + .5f, yF + .45f, D - .9f), F.P(G + 1.6f, yF + .45f, D - .9f), .3f, 12);
            BoxF(bl, F, L - G - 2.3f, L - G - .3f, yF + .88f * k, yF + .93f * k, D - .8f, D - .1f, true); KolF(rd, F, L - G - 2.3f, L - G - .3f, yF, yF + .93f * k, D - .8f, D - .1f);
            BraxRegionMiastoB.KolaWarsztatow.Add((F.P(G + .7f, yF, 1.2f), Vector3.up, 4, "wulkanizacja na faweli (w środku)"));
            BraxRegionMiastoB.KolaWarsztatow.Add((F.P(G + .7f, yF, 2.1f), Vector3.up, 3, "wulkanizacja na faweli (w środku)"));
            BraxRegionMiastoB.KolaWarsztatow.Add((F.P(w1 + .5f, WysTerenu(F.P(w1 + .5f, 0f, -.8f).x, F.P(w1 + .5f, 0f, -.8f).z), -.8f), Vector3.up, 4, "wulkanizacja na faweli (przed lokalem)"));
        }
        if (przych)
        {   // ławka poczekalni, biała szafka, biurko rejestracji już jest (lada)
            BoxF(bl, F, G + .05f, G + .5f, yF + .42f * k, yF + .47f * k, .6f, 2.2f, true); KolF(rd, F, G, G + .5f, yF, yF + .47f * k, .6f, 2.2f);
            BoxF(W(cx, cz, plastik, new Color(.95f, .95f, .95f)), F, rg0, rg0 + 1.0f, yF, yF + 1.8f * k, D - .45f, D - .02f, true);
        }
        if ((loteria || tel || bar || rodzaj == "SKLEP SPOŻYWCZY") && BraxMiastoAssety.Jest("miasto_sklepy"))
        {   // plakaty promocji z grafik właściciela na lewej ścianie (zamiast kolorowych prostokątów)
            for (int q = 0; q < 3; q++)
            {
                var pl = BraxMiastoAssety.Model("miasto_sklepy", "plakat_0" + (1 + (dz.nr + q) % 5)); if (!pl) break;
                BraxMiastoAssety.Postaw(tw ? tw : rd, pl, "Fawela: plakat " + (q + 1), F.P(G + .02f, yF + 1.25f * k, 1.0f + q * 1.25f), Mathf.Atan2(F.R.x, F.R.z) * Mathf.Rad2Deg, 1f, true, false);
            }
        }
        if (!wulk && !fryzjer)
        {   // skrzynki z butelkami / worki przy wejściu
            for (int q = 0; q < 2; q++) BoxF(W(cx, cz, plastik, q == 0 ? new Color(.8f, .15f, .12f) : new Color(.9f, .7f, .1f)), F, w1 + .12f, w1 + .52f, dz.yFront + q * .3f, dz.yFront + .28f + q * .3f, -.55f, -.2f, true);
        }
        if (bar && BraxMiastoAssety.Jest("miasto_sklepy"))
        {   // tablica menu z grafiki właściciela nad ladą, przodem do klienta
            var mm = BraxMiastoAssety.Model("miasto_sklepy", "menu_4");
            if (mm) BraxMiastoAssety.Postaw(tw, mm, "Fawela: menu nad ladą", F.P((l0 + l1) * .5f, ys - .12f - .675f * k, ld + .3f), F.Yaw, 1f, true, false);
        }
        if (typLady != null) BraxSklepyTowar.Lada(typLady, tw, F.P(l0 + .12f, yF + .965f * k, ld + .3f), F.R, -F.N, l1 - l0 - .85f, r);
        if (przych) BraxSklepyTowar.Polka("apteka", tw, F.P(rg0 + .05f, yF + 1.8f * k + .005f, D - .24f), F.R, -F.N, .9f, r);   // leki na szafce
        if (tw) BraxSklepyTowar.Zakoncz(tw, "FAWELA " + rodzaj);
        // skrzynka przy wejściu (model Poly Haven)
        if (!wulk && !fryzjer && !przych) ModelPH(rd, F.P(w0 - .35f > .3f ? w0 - .3f : w1 + .9f, dz.yFront - .02f, -.6f), .5f, r, "wooden_crate_01", "plastic_crate_01", "cardboard_box_01");
        // kolizje: ściany parteru z przejściem frontu, piętra jako bryła
        float top = yF + n * hf + .5f;
        Kol(rd, new Vector3(dz.x0, yF + hf, dz.z0), new Vector3(dz.x1, top, dz.z1));
        KolF(rd, F, 0f, w0, yF - .3f, yF + hf, 0f, G); KolF(rd, F, w1, L, yF - .3f, yF + hf, 0f, G); KolF(rd, F, w0, w1, yF + 2.3f * k, yF + hf, 0f, G);
        KolF(rd, F, 0f, G, yF - .3f, yF + hf, G, gl); KolF(rd, F, L - G, L, yF - .3f, yF + hf, G, gl); KolF(rd, F, G, L - G, yF - .3f, yF + hf, D, gl);
        KolF(rd, F, 0f, L, yF - .4f, yF, 0f, gl);   // posadzka
        lokali++;
    }

    // towar na półce (zamiast gładkich klocków): 0 pudełko z etykietą, 1 butelka z nakrętką, 2 pudełko apteczne (białe z paskiem), 3 bochenek, 4 puszka z banderolą
    static void Towar(Fr F, float s, float y, float d, System.Random r, float cx, float cz, int rodzaj)
    {
        var kol = Color.HSVToRGB((float)r.NextDouble(), .4f + (float)r.NextDouble() * .3f, .55f + (float)r.NextDouble() * .3f);
        if (rodzaj == 1)
        {
            var szklo = r.Next(3) == 0 ? new Color(.25f, .4f, .2f) : r.Next(2) == 0 ? new Color(.45f, .28f, .12f) : new Color(.8f, .82f, .8f); float h = .17f + (float)r.NextDouble() * .08f;
            for (int q = 0; q < 2; q++)
            {
                var c0 = F.P(s + .06f + q * .12f, y, d + .12f);
                W(cx, cz, plastik, szklo).Cyl(c0, c0 + Vector3.up * h, .038f, 6, -1f, false); W(cx, cz, plastik, szklo).Cyl(c0 + Vector3.up * h, c0 + Vector3.up * (h + .07f), .038f, 6, .016f, false);
                W(cx, cz, plastik, kol).Cyl(c0 + Vector3.up * (h + .07f), c0 + Vector3.up * (h + .09f), .018f, 5);
                W(cx, cz, plastik, new Color(.93f, .9f, .8f)).Cyl(c0 + Vector3.up * h * .3f, c0 + Vector3.up * h * .7f, .0395f, 6, -1f, false);   // etykieta
            }
            return;
        }
        if (rodzaj == 3) { W(cx, cz, deski, new Color(1f, .78f, .48f)).Cyl(F.P(s, y + .07f, d + .14f), F.P(s + .24f, y + .07f, d + .14f), .07f, 7); return; }
        if (rodzaj == 4)
        {
            for (int q = 0; q < 2; q++)
            {
                var c0 = F.P(s + .06f + q * .12f, y, d + .12f);
                W(cx, cz, alu, Color.white).Cyl(c0, c0 + Vector3.up * .11f, .042f, 7); W(cx, cz, plastik, kol).Cyl(c0 + Vector3.up * .02f, c0 + Vector3.up * .09f, .0435f, 7, -1f, false);
            }
            return;
        }
        float hh = .14f + (float)r.NextDouble() * .14f, w = .2f + (float)r.NextDouble() * .04f; if (rodzaj == 2) { kol = new Color(.93f, .93f, .9f); hh = .1f + (float)r.NextDouble() * .06f; }
        BoxF(W(cx, cz, plastik, kol), F, s, s + w, y, y + hh, d, d + .24f);
        Pasek(W(cx, cz, plastik, rodzaj == 2 ? Color.HSVToRGB((float)r.NextDouble(), .6f, .7f) : new Color(.95f, .93f, .85f)), F, s + .02f, s + w - .02f, y + hh * .3f, y + hh * .7f, d - .003f);
        if (rodzaj != 2) Pasek(W(cx, cz, plastik, kol * .55f), F, s + .04f, s + w - .04f, y + hh * .42f, y + hh * .58f, d - .005f);
    }

    static int modeli;
    /// <summary>Model z Poly Haven (skrzynka, puszki, owoce, pieczywo…) postawiony w lokalu; brak modelu w buildzie = nic (towar z kodu zostaje).</summary>
    static void ModelPH(Transform rd, Vector3 p, float maxH, System.Random r, params string[] ids)
    {
        var m = BraxRegion.Model(ids);
        if (m && BraxRegion.Put(rd, m, "Fawela: " + ids[0], p, r.Next(360), 1f, false, maxH)) modeli++;
    }

    static void StolikZKrzeslami(Fr F, float s, float y, float d, System.Random r, float cx, float cz, Transform rd)
    {
        var kol = r.Next(2) == 0 ? new Color(.92f, .92f, .9f) : new Color(.8f, .16f, .14f); var pl = W(cx, cz, plastik, kol);
        pl.Cyl(F.P(s, y + .7f * K, d), F.P(s, y + .73f * K, d), .42f, 12); pl.Cyl(F.P(s, y, d), F.P(s, y + .7f * K, d), .04f, 6);
        foreach (float sx in new[] { -1f, 1f })
        {
            float ks = s + sx * .75f;
            BoxF(pl, F, ks - .2f, ks + .2f, y + .42f * K, y + .46f * K, d - .2f, d + .2f, true);
            BoxF(pl, F, sx < 0 ? ks - .2f : ks + .16f, sx < 0 ? ks - .16f : ks + .2f, y + .46f * K, y + .85f * K, d - .2f, d + .2f, true);
            foreach (float a in new[] { -.17f, .17f }) foreach (float b in new[] { -.17f, .17f }) pl.Cyl(F.P(ks + a, y, d + b), F.P(ks + a, y + .42f * K, d + b), .018f, 4, -1f, false);
        }
        KolF(rd, F, s - .42f, s + .42f, y, y + .73f * K, d - .42f, d + .42f);
    }

    // ---------------------------------------------------------------------------- PLAC Z TARGIEM
    static void PlacBuduj()
    {
        var t = Tarasy[0]; var rc = t.r; float y = t.y; var tr = Grupa("Fawela: plac z targiem"); var r = new System.Random(77);
        var pl = new Bud(); pl.kolor = p => Color.white * (.84f + .16f * Szum(p.x * .4f, p.z * .4f));
        pl.Box(new Vector3(rc.xMin, y - .8f, rc.yMin), new Vector3(rc.xMax, y, rc.yMax));
        pl.Obiekt("Plac: posadzka betonowa wytarta", tr, placBeton, false, true);
        MurkiTarasu(tr, rc, y, new Rect(Plac.xMax - 6f, Plac.yMax - .1f, 5f, .2f));
        float pcx = rc.center.x, pcz = rc.center.y;
        // stragany: stelaż z rur, blat z desek, plandeka w kolorze, skrzynki z towarem
        var kolory = new[] { new Color(.85f, .22f, .18f), new Color(.15f, .45f, .80f), new Color(.95f, .75f, .15f), new Color(.20f, .60f, .35f), new Color(.85f, .45f, .15f), new Color(.6f, .3f, .7f) };
        var twTarg = BraxSklepyTowar.Korzen(tr, "FAWELA targ");
        for (int q = 0; q < 6; q++)
        {
            float sx = rc.xMin + 5f + (q % 3) * 11f, sz = q < 3 ? rc.yMax - 3.4f : rc.yMin + 3.4f, w = 2.7f, dd = 1.7f, hs = 2.3f * K;
            var F = Fr.Nowa(new Vector3(sx - w * .5f, 0f, sz + (q < 3 ? -dd * .5f : dd * .5f) * -1f), q < 3 ? Vector3.back : Vector3.forward, w);
            F = q < 3 ? Fr.Nowa(new Vector3(sx + w * .5f, 0f, sz - dd * .5f), Vector3.back, w) : Fr.Nowa(new Vector3(sx - w * .5f, 0f, sz + dd * .5f), Vector3.forward, w);
            var rura = W(pcx, pcz, metal, new Color(.55f, .55f, .55f));
            foreach (float s in new[] { .03f, w - .03f }) foreach (float d in new[] { .03f, dd - .03f }) rura.Cyl(F.P(s, y, d), F.P(s, y + hs + (d < .5f ? .25f : -.1f), d), .022f, 5);
            BoxF(W(pcx, pcz, deski, Color.white), F, 0f, w, y + .82f * K, y + .87f * K, 0f, dd, true);
            var pd = W(pcx, pcz, plotno, kolory[q]);
            pd.Quad2(F.P(-.25f, y + hs + .27f, -.3f), F.P(w + .25f, y + hs + .27f, -.3f), F.P(w + .25f, y + hs - .12f, dd + .2f), F.P(-.25f, y + hs - .12f, dd + .2f));
            pd.Quad2(F.P(-.25f, y + hs + .02f, -.3f), F.P(w + .25f, y + hs + .02f, -.3f), F.P(w + .25f, y + hs + .27f, -.3f), F.P(-.25f, y + hs + .27f, -.3f));
            BraxSklepyTowar.Polka(new[] { "warzywniak", "piekarnia", "market", "warzywniak", "sm_g3", "sm_g4" }[q], twTarg, F.P(.12f, y + .875f * K, dd * .5f), F.R, -F.N, w - .24f, r, .5f);
            KolF(tr, F, 0f, w, y, y + .87f * K, 0f, dd);
            if (q == 0 || q == 4) Sprzedawcy.Add((F.P(w * .5f, y, dd + .7f), F.Yaw, "targ — stragan"));
        }
        BraxSklepyTowar.Zakoncz(twTarg, "FAWELA targ");
        var wozek = BraxMiastoAssety.Jest("miasto_jedzenie") ? BraxMiastoAssety.Model("miasto_jedzenie", "wozek_jedzenie") : null;
        if (wozek)
        {   // wózek z jedzeniem (model właściciela) przy wejściu na plac od strony drogi, obsługa obok
            var pw = new Vector3(rc.xMax - 4.5f, y, rc.yMin + 3.2f);
            var ow = BraxMiastoAssety.Postaw(tr, wozek, "Fawela: wózek z jedzeniem (model właściciela)", pw, 180f, 1f, true, true);
            var bw = BraxMiastoAssety.Granice(ow); ow.transform.position += new Vector3(pw.x - bw.center.x, pw.y - bw.min.y, pw.z - bw.center.z);
            BraxMiastoAssety.Kolizja(tr, ow, "Fawela: wózek z jedzeniem kolizja", .05f);
            Sprzedawcy.Add((pw + new Vector3(0f, 0f, 1.6f), 180f, "targ — wózek z jedzeniem"));
        }
        BraxRegionMeble.Lawka(tr, new Vector3(pcx - 4f, y, pcz), 0f, "Fawela: ławka na placu");
        BraxRegionMeble.Lawka(tr, new Vector3(pcx + 6f, y, pcz), 180f, "Fawela: ławka na placu");
        BraxRegionMeble.Kosz(tr, new Vector3(pcx + 1f, y, pcz + 1f));
        StolikZKrzeslami(Fr.Nowa(new Vector3(rc.xMin + 2f, 0f, pcz), Vector3.forward, 4f), 1.5f, y, 0f, r, pcx, pcz, tr);
    }

    // murek wokół tarasu: od strony, gdzie teren obok jest niżej — parapet 0.6 m × K; wyżej — mur oporowy z graffiti; przerwy = wejścia
    static void MurkiTarasu(Transform tr, Rect rc, float y, params Rect[] wejscia)
    {
        var m = new Bud(); float cx = rc.center.x, cz = rc.center.y; var rnd = new System.Random((int)(rc.xMin * 3f));
        void Bok(Vector3 a, Vector3 n, float L)
        {
            var F = Fr.Nowa(a, n, L);
            for (float s = 0f; s < L - .01f; s += 2f)
            {
                float s1 = Mathf.Min(L, s + 2f); var pm = F.P((s + s1) * .5f, 0f, -2.2f); var pe = F.P((s + s1) * .5f, 0f, 0f);
                if (wejscia.Any(w => w.Contains(new Vector2(pe.x, pe.z))) || DoDrog(pm.x, pm.z, 8f) < 5.5f || DoSchodow(pe.x, pe.z) < 2.2f) continue;
                float ht = WysTerenu(pm.x, pm.z); float top = ht > y + .5f ? Mathf.Min(ht + .3f, y + 2.6f) : y + .6f * K;
                m.kolor = p => Color.white * Mathf.Lerp(.66f, 1f, S01(y - 1f, y + .8f, p.y));
                BoxF(m, F, s, s1, Mathf.Min(ht, y) - .6f, top, 0f, .25f, true, false);
                if (ht > y + 1.6f && rnd.NextDouble() < .5 && s1 - s > 1.9f) NaklejkaTyl(F, s, s1, y + .3f, y + 1.6f, rnd.Next(16), cx, cz);
            }
        }
        Bok(new Vector3(rc.xMin, 0f, rc.yMax), Vector3.forward, rc.width); Bok(new Vector3(rc.xMax, 0f, rc.yMin), Vector3.back, rc.width);
        Bok(new Vector3(rc.xMin, 0f, rc.yMin), Vector3.left, rc.height); Bok(new Vector3(rc.xMax, 0f, rc.yMax), Vector3.right, rc.height);
        m.Obiekt("Fawela: murki tarasu (beton)", tr, beton, true, true);
    }
    // graffiti na wewnętrznym licu murka tarasu (lico d = 0.25 patrzy do środka tarasu)
    static void NaklejkaTyl(Fr F, float s0, float s1, float y0, float y1, int komorka, float cx, float cz)
    {
        if (!matGraffiti) return;
        W(cx, cz, matGraffiti, Color.white).QuadUV(F.P(s1, y0, .265f), F.P(s0, y0, .265f), F.P(s0, y1, .265f), F.P(s1, y1, .265f), KomorkaGraffiti(komorka), true);
        graffiti++;
    }

    // ---------------------------------------------------------------------------- SALA KICKBOXINGU Z SIŁOWNIĄ (teren rywala)
    static void SalaBuduj()
    {
        var t = Tarasy[1]; float y = t.y, k = K; var s = Sala; var tr = Grupa("Fawela: SALA KICKBOXINGU z siłownią (teren rywala)"); var r = new System.Random(4242);
        float cx = s.center.x, cz = s.center.y, hs = 7.0f, G = .3f, L = s.width, D = s.height;
        var pod = new Bud(); pod.kolor = p => Color.white * (.84f + .16f * Szum(p.x * .4f, p.z * .4f));
        pod.Box(new Vector3(t.r.xMin, y - .8f, t.r.yMin), new Vector3(t.r.xMax, y, t.r.yMax));
        pod.Obiekt("Sala: posadzka i podwórze (beton wytarty)", tr, placBeton, false, true);
        MurkiTarasu(tr, t.r, y, new Rect(s.xMin, s.yMin - .2f, s.width, .4f));
        // front od serpentyny (południe, z = s.yMin): wejście 1.7 m × K i brama rolowana; okna górne dookoła
        var fF = Fr.Nowa(new Vector3(s.xMax, 0f, s.yMin), Vector3.back, L); var fB = Fr.Nowa(new Vector3(s.xMin, 0f, s.yMax), Vector3.forward, L);
        var fL = Fr.Nowa(new Vector3(s.xMin, 0f, s.yMin), Vector3.left, D); var fP = Fr.Nowa(new Vector3(s.xMax, 0f, s.yMax), Vector3.right, D);
        float e0 = L * .5f + 3f, e1 = e0 + 1.7f * k, b0 = L * .5f - 8.5f, b1 = b0 + 4.6f, yd = y + 2.35f * k, yg0 = y + 4.6f, yg1 = y + 6.0f;
        List<Rect> Gorne(float len) { var l = new List<Rect>(); for (float a = 2f; a < len - 4.4f; a += 4.5f) l.Add(Rect.MinMaxRect(a, yg0, a + 2.4f, yg1)); return l; }
        var zolty = new Color(.95f, .78f, .36f); var lin = W(cx, cz, wnetrze, new Color(.9f, .88f, .8f));
        Bud Tynk() { var b = W(cx, cz, tynki[3]); b.kolor = p => zolty * Mathf.Lerp(.6f, 1f, S01(y, y + 1.5f, p.y)) * (.93f + .07f * Szum(p.x * .5f + p.y * .3f, p.z * .5f)); return b; }
        Bud Cegla() { var b = W(cx, cz, cegla); b.kolor = p => Color.white * Mathf.Lerp(.66f, 1f, S01(y, y + 1.5f, p.y)); return b; }
        var oF = Gorne(L); oF.Add(Rect.MinMaxRect(e0, y, e1, yd)); oF.Add(Rect.MinMaxRect(b0, y, b1, y + 3.6f));
        Sciana(Tynk(), fF, 0f, L, y, y + hs, oF, y + 1.4f, G, lin, G);
        Sciana(Cegla(), fB, 0f, L, y, y + hs, Gorne(L), y + 1.4f, G, lin, G);
        var oL = Gorne(D); float bd0 = D * .5f - .7f * k, bd1 = D * .5f + .7f * k; oL.Add(Rect.MinMaxRect(bd0, y, bd1, yd));
        Sciana(Tynk(), fL, 0f, D, y, y + hs, oL, y + 1.4f, G, lin, G);
        Sciana(Cegla(), fP, 0f, D, y, y + hs, Gorne(D), y + 1.4f, G, lin, G);
        foreach (var (F, len) in new[] { (fF, L), (fB, L), (fL, D), (fP, D) })
            foreach (var o in Gorne(len))
            {
                Pasek(W(cx, cz, szyba, Color.white), F, o.xMin, o.xMax, o.yMin, o.yMax, .14f);
                var br = W(cx, cz, metal, new Color(.3f, .3f, .3f));
                for (int q = 0; q <= 4; q++) { float a = Mathf.Lerp(o.xMin, o.xMax, q / 4f); Pasek(br, F, a - .02f, a + .02f, o.yMin, o.yMax, .13f); }
                Pasek(br, F, o.xMin, o.xMax, o.yMin, o.yMin + .04f, .13f); Pasek(br, F, o.xMin, o.xMax, o.yMax - .04f, o.yMax, .13f); Pasek(br, F, o.xMin, o.xMax, (o.yMin + o.yMax) * .5f - .02f, (o.yMin + o.yMax) * .5f + .02f, .13f);
            }
        // wieniec, dach dwuspadowy z blachy rdzawej, brama rolowana (zamknięta), drzwi boczne otwarte na podwórze
        W(cx, cz, betonZbr, Color.white).Box(new Vector3(s.xMin - .05f, y + hs, s.yMin - .05f), new Vector3(s.xMax + .05f, y + hs + .3f, s.yMax + .05f), true);
        var dach = W(cx, cz, blachaRdza, Color.white); float yk = y + hs + 1.5f, yo = y + hs + .3f;
        dach.Quad2(new Vector3(s.xMin - .5f, yo, s.yMin - .5f), new Vector3(s.xMax + .5f, yo, s.yMin - .5f), new Vector3(s.xMax + .5f, yk, cz), new Vector3(s.xMin - .5f, yk, cz));
        dach.Quad2(new Vector3(s.xMin - .5f, yk, cz), new Vector3(s.xMax + .5f, yk, cz), new Vector3(s.xMax + .5f, yo, s.yMax + .5f), new Vector3(s.xMin - .5f, yo, s.yMax + .5f));
        foreach (float xs in new[] { s.xMin, s.xMax }) { var sz = Cegla(); sz.Tri(new Vector3(xs, yo, s.yMin), new Vector3(xs, yk, cz), new Vector3(xs, yo, s.yMax)); sz.Tri(new Vector3(xs, yo, s.yMax), new Vector3(xs, yk, cz), new Vector3(xs, yo, s.yMin)); }
        Pasek(W(cx, cz, roleta, new Color(.8f, .8f, .82f)), fF, b0, b1, y, y + 3.6f, .1f);
        BoxF(W(cx, cz, roleta, Color.white), fF, b0 - .08f, b1 + .08f, y + 3.6f, y + 3.95f, -.12f, .1f);
        // szyld-bryła 3D nad wejściem (bez nazwy rywala — właściciel nada) + litery
        float sz0 = e0 - 3.4f, sz1 = e1 + 3.4f, sy0 = yd + .35f, sy1 = sy0 + 1.15f;
        BoxF(W(cx, cz, metal, new Color(.14f, .14f, .15f)), fF, sz0, sz1, sy0, sy1, -.32f, 0f);
        Pasek(W(cx, cz, plastik, new Color(.1f, .1f, .1f)), fF, sz0 + .04f, sz1 - .04f, sy0 + .04f, sy1 - .04f, -.325f);
        BraxRegionMeble.Napis(tr, "SALA KICKBOXINGU", fF.P((sz0 + sz1) * .5f, sy0 + .72f, -.34f), fF.N, .5f * k, new Color(.92f, .18f, .12f));
        BraxRegionMeble.Napis(tr, "SIŁOWNIA · BOKS · KICKBOXING", fF.P((sz0 + sz1) * .5f, sy0 + .22f, -.34f), fF.N, .17f * k, new Color(.97f, .82f, .18f));
        Naklejka(fF, L - 7.5f, L - 2.5f, y + .3f, y + 2.4f, 14, cx, cz); Naklejka(fP, 3f, 9f, y + .3f, y + 2.6f, 15, cx, cz); Naklejka(fB, 4f, 8f, y + .3f, y + 2.1f, 10, cx, cz);
        // kolizje ścian (przejścia: wejście i drzwi boczne)
        KolF(tr, fF, 0f, e0, y, y + hs, 0f, G); KolF(tr, fF, e1, L, y, y + hs, 0f, G); KolF(tr, fF, e0, e1, yd, y + hs, 0f, G);
        KolF(tr, fB, 0f, L, y, y + hs, 0f, G); KolF(tr, fP, 0f, D, y, y + hs, 0f, G);
        KolF(tr, fL, 0f, bd0, y, y + hs, 0f, G); KolF(tr, fL, bd1, D, y, y + hs, 0f, G); KolF(tr, fL, bd0, bd1, yd, y + hs, 0f, G);
        // WNĘTRZE: maty gumowe, ring 6 × 6 m (podest, narożniki skórzane, 4 liny), 6 worków na łańcuchach, strefa ciężarów, lustra, szafki
        var mat = W(cx, cz, guma, Color.white); mat.Box(new Vector3(s.xMin + .5f, y, s.yMin + .5f), new Vector3(s.xMax - 11.5f, y + .03f, s.yMax - .5f));
        float rcx = s.xMax - 6.2f, rcz = cz, rs = 3.0f * k * .95f;
        var ring = W(cx, cz, plotno, new Color(.3f, .42f, .7f)); ring.Box(new Vector3(rcx - rs, y, rcz - rs), new Vector3(rcx + rs, y + .9f, rcz + rs));
        W(cx, cz, guma, Color.white).Box(new Vector3(rcx - rs - .02f, y, rcz - rs - .02f), new Vector3(rcx + rs + .02f, y + .78f, rcz + rs + .02f));
        var stal = W(cx, cz, metal, new Color(.65f, .65f, .65f)); var skr = W(cx, cz, skora, Color.white); var liny = W(cx, cz, plotno);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz2 in new[] { -1f, 1f })
        {
            var c0 = new Vector3(rcx + sx * (rs - .1f), y + .9f, rcz + sz2 * (rs - .1f));
            stal.Cyl(c0, c0 + Vector3.up * 1.5f * k, .06f, 8); skr.K(sx * sz2 > 0 ? Color.white : new Color(.35f, .45f, .9f)); skr.Box(c0 + new Vector3(-.13f, .35f, -.13f), c0 + new Vector3(.13f, 1.45f * k, .13f), true);
        }
        int li = 0;
        foreach (float hl in new[] { .45f, .75f, 1.05f, 1.35f })
        {
            float yl = y + .9f + hl * k, q = rs - .1f; liny.K(li++ % 2 == 0 ? new Color(.9f, .15f, .12f) : new Color(.95f, .95f, .95f));
            liny.Cyl(new Vector3(rcx - q, yl, rcz - q), new Vector3(rcx + q, yl, rcz - q), .022f, 5, -1f, false); liny.Cyl(new Vector3(rcx - q, yl, rcz + q), new Vector3(rcx + q, yl, rcz + q), .022f, 5, -1f, false);
            liny.Cyl(new Vector3(rcx - q, yl, rcz - q), new Vector3(rcx - q, yl, rcz + q), .022f, 5, -1f, false); liny.Cyl(new Vector3(rcx + q, yl, rcz - q), new Vector3(rcx + q, yl, rcz + q), .022f, 5, -1f, false);
        }
        Kol(tr, new Vector3(rcx - rs, y, rcz - rs), new Vector3(rcx + rs, y + .9f, rcz + rs));
        W(cx, cz, lada, Color.white).Box(new Vector3(rcx - rs - 1.0f, y, rcz - .6f), new Vector3(rcx - rs, y + .45f, rcz + .6f));   // schodki na ring
        Kol(tr, new Vector3(rcx - rs - 1.0f, y, rcz - .6f), new Vector3(rcx - rs, y + .45f, rcz + .6f));
        for (int q = 0; q < 6; q++)
        {
            float bx = s.xMin + 3.2f + (q % 3) * 3.3f, bz = q < 3 ? s.yMin + 4.2f : s.yMax - 5.6f;
            skr.K(q % 2 == 0 ? Color.white : new Color(.25f, .25f, .28f)); skr.Cyl(new Vector3(bx, y + .5f * k, bz), new Vector3(bx, y + 1.7f * k, bz), .2f * k, 14);
            stal.Cyl(new Vector3(bx, y + 1.7f * k, bz), new Vector3(bx, y + hs, bz), .012f, 4, -1f, false);
            Kol(tr, new Vector3(bx - .2f * k, y + .5f * k, bz - .2f * k), new Vector3(bx + .2f * k, y + 1.7f * k, bz + .2f * k));
        }
        // SIŁOWNIA: sprzęt z biblioteki właściciela (miasto_silownia_metro: klatka do przysiadów z gryfem, ławka skośna, bieżnie, atlas);
        // bez paczki — stojak ze sztangą i ławka z kodu (żeby sala nie była pusta)
        float wx = s.xMin + 13f, wz = s.yMax - 2.6f; var tal = W(cx, cz, guma, Color.white);
        bool zBiblioteki = BraxMiastoAssety.Klatka(tr, new Vector3(wx, y + .03f, wz - .3f), 0f);
        if (zBiblioteki)
        {
            BraxMiastoAssety.Lawka(tr, new Vector3(wx + 3.1f, y + .03f, wz - .5f), 0f);
            BraxMiastoAssety.Bieznia(tr, new Vector3(s.xMin + 1.6f, y + .03f, s.yMax - 2.2f), 0f); BraxMiastoAssety.Bieznia(tr, new Vector3(s.xMin + 3.5f, y + .03f, s.yMax - 2.2f), 0f);
            var atlas = BraxMiastoAssety.Model("miasto_silownia_metro", "silownia_atlas");
            if (atlas) { var oa = BraxMiastoAssety.Postaw(tr, atlas, "Sala: atlas wielofunkcyjny (model właściciela)", new Vector3(s.xMax - 5.5f, y + .03f, s.yMax - 1.9f), 180f); BraxMiastoAssety.Kolizja(tr, oa, "Sala: atlas kolizja", .05f); }
        }
        else
        {
        
            stal.Box(new Vector3(wx - 1.2f, y, wz - .08f), new Vector3(wx - 1.1f, y + 1.8f * k, wz + .08f)); stal.Box(new Vector3(wx + 1.1f, y, wz - .08f), new Vector3(wx + 1.2f, y + 1.8f * k, wz + .08f));
            stal.Cyl(new Vector3(wx - 1.5f, y + 1.35f * k, wz - .15f), new Vector3(wx + 1.5f, y + 1.35f * k, wz - .15f), .022f, 6);
            
            foreach (float sx in new[] { -1.4f, -1.33f, 1.33f, 1.4f }) tal.Cyl(new Vector3(wx + sx - .03f, y + 1.35f * k, wz - .15f), new Vector3(wx + sx + .03f, y + 1.35f * k, wz - .15f), .23f * k, 14);
            skr.K(new Color(.25f, .25f, .28f)); skr.Box(new Vector3(wx - .2f, y + .42f * k, wz - 2.0f), new Vector3(wx + .2f, y + .5f * k, wz - .5f), true);
            stal.Box(new Vector3(wx - .15f, y, wz - 1.9f), new Vector3(wx + .15f, y + .42f * k, wz - .6f));
            Kol(tr, new Vector3(wx - 1.5f, y, wz - 2f), new Vector3(wx + 1.5f, y + 1.8f * k, wz + .1f));
        }
        for (int q = 0; q < 5; q++) { float hx = s.xMin + 9f + q * .5f; stal.Cyl(new Vector3(hx, y + .9f, s.yMax - G - .25f), new Vector3(hx + .32f, y + .9f, s.yMax - G - .25f), .02f, 5); tal.Cyl(new Vector3(hx, y + .9f, s.yMax - G - .25f), new Vector3(hx + .07f, y + .9f, s.yMax - G - .25f), .09f + q * .012f, 8); tal.Cyl(new Vector3(hx + .25f, y + .9f, s.yMax - G - .25f), new Vector3(hx + .32f, y + .9f, s.yMax - G - .25f), .09f + q * .012f, 8); }
        stal.Box(new Vector3(s.xMin + 8.8f, y, s.yMax - G - .45f), new Vector3(s.xMin + 11.6f, y + .82f, s.yMax - G - .05f));
        // lustra na ścianie tylnej, szafki stalowe przy ścianie wschodniej, świetlówki
        W(cx, cz, lustro, Color.white).Quad(fB.P(L - 8f, y + .4f, G + .012f), fB.P(L - 2f, y + .4f, G + .012f), fB.P(L - 2f, y + 2.3f * k, G + .012f), fB.P(L - 8f, y + 2.3f * k, G + .012f));
        for (int q = 0; q < 7; q++)
        {
            var kolS = q % 2 == 0 ? new Color(.35f, .45f, .6f) : new Color(.6f, .6f, .62f);
            W(cx, cz, metal, kolS).Box(new Vector3(s.xMin + G, y, s.yMin + .8f + q * .62f), new Vector3(s.xMin + G + .55f, y + 1.85f * k, s.yMin + 1.4f + q * .62f));
        }
        Kol(tr, new Vector3(s.xMin + G, y, s.yMin + .8f), new Vector3(s.xMin + G + .55f, y + 1.85f * k, s.yMin + 5.2f));
        for (float x = s.xMin + 3f; x < s.xMax - 2f; x += 4.5f) foreach (float zz in new[] { cz - 4.5f, cz + 4.5f }) W(cx, cz, zarowka, Color.white).Box(new Vector3(x - .75f, y + hs - .4f, zz - .06f), new Vector3(x + .75f, y + hs - .34f, zz + .06f), true);
        BraxRegion.Warm(tr, "Sala: światło", new Vector3(cx, y + hs - 1.2f, cz), 2.2f, 18f);
        // regał z odżywkami przy wejściu (modele z etykiet właściciela — biblioteka BraxSklepyTowar)
        {
            float o0 = e1 + .7f, o1 = e1 + 3.1f; var reg = W(cx, cz, lada, Color.white); var twO = BraxSklepyTowar.Korzen(tr, "FAWELA sala — odżywki");
            foreach (float so in new[] { o0, o1 - .04f }) BoxF(reg, fF, so, so + .04f, y, y + 1.9f * k, G + .02f, G + .45f, true);
            string[] typO = { "odz_dol", "odz_polka", "odz_gora" };
            for (int p = 0; p < 3; p++)
            {
                float yp = y + .15f + p * .6f * k; BoxF(reg, fF, o0, o1, yp, yp + .035f, G + .02f, G + .45f, true);
                BraxSklepyTowar.Polka(typO[p], twO, fF.P(o0 + .08f, yp + .039f, G + .24f), fF.R, fF.N, o1 - o0 - .16f, r, .6f * k - .08f);
            }
            KolF(tr, fF, o0, o1, y, y + 1.9f * k, G, G + .45f); BraxSklepyTowar.Zakoncz(twO, "FAWELA sala — odżywki");
        }
        // podwórze sali: stosy opon (koła z modeli aut — krok ruchu), liny bojowe, worek z piaskiem
        var pw = PodworzeSali;
        foreach (float zz in new[] { pw.yMin + 2.2f, pw.yMin + 5.2f, pw.yMin + 8.2f }) BraxRegionMiastoB.KolaWarsztatow.Add((new Vector3(pw.center.x - .6f, y, zz), Vector3.up, zz < pw.yMin + 3f ? 4 : 3, "podwórze sali kickboxingu"));
        var lb = W(cx, cz, plotno, new Color(.25f, .22f, .2f));
        foreach (float zz in new[] { pw.yMax - 3.2f, pw.yMax - 2.4f }) { Vector3 pp = new Vector3(pw.xMin + .8f, y + .05f, zz); for (int q = 1; q <= 8; q++) { var p2 = new Vector3(pw.xMin + .8f + q * .55f, y + .05f, zz + Mathf.Sin(q * 1.3f) * .18f); lb.Cyl(pp, p2, .05f, 6, -1f, false); pp = p2; } }
        Sprzedawcy.Add((fF.P(e0 - 1.6f, y, G + 2.4f), fF.Yaw, "sala kickboxingu — przy wejściu"));
        Sprzedawcy.Add((new Vector3(rcx - rs - 1.8f, y, rcz + 1.6f), 90f, "sala kickboxingu — trener przy ringu"));
        lokali++;
    }

    // ---------------------------------------------------------------------------- GRAFFITI: atlas 4 × 4 malowany kodem (tagi, „bąble”, pixação, murale)
    internal static Rect KomorkaGraffiti(int k) { k = ((k % 16) + 16) % 16; return new Rect((k % 4) * .25f + .004f, (k / 4) * .25f + .004f, .242f, .242f); }

    static Material Graffiti()
    {
        const int N = 1024, C = 256; var px = new Color32[N * N]; var rnd = new System.Random(1312);
        void Stempel(int ox, int oy, float x, float y, float rad, Color32 kol)
        {
            int x0 = Mathf.Max(4, Mathf.FloorToInt(x - rad)), x1 = Mathf.Min(C - 5, Mathf.CeilToInt(x + rad)), y0 = Mathf.Max(4, Mathf.FloorToInt(y - rad)), y1 = Mathf.Min(C - 5, Mathf.CeilToInt(y + rad));
            for (int j = y0; j <= y1; j++) for (int i = x0; i <= x1; i++) if ((i - x) * (i - x) + (j - y) * (j - y) <= rad * rad) px[(oy + j) * N + ox + i] = kol;
        }
        void Kreska(int ox, int oy, Vector2 a, Vector2 b, float rad, Color32 kol) { int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / Mathf.Max(1f, rad * .5f))); for (int q = 0; q <= n; q++) { var p = Vector2.Lerp(a, b, q / (float)n); Stempel(ox, oy, p.x, p.y, rad, kol); } }
        Color32 Farba() { var c = Color.HSVToRGB((float)rnd.NextDouble(), .75f + (float)rnd.NextDouble() * .25f, .85f + (float)rnd.NextDouble() * .15f); return new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255); }
        var czarny = new Color32(16, 16, 18, 255); var bialy = new Color32(240, 240, 236, 255);
        for (int k = 0; k < 16; k++)
        {
            int ox = (k % 4) * C, oy = (k / 4) * C; int rodzaj = k < 6 ? 0 : k < 10 ? 1 : k < 14 ? 2 : 3;
            if (rodzaj == 3)
            {   // mural: słońce nad dachami w pasach koloru
                var tlo = Farba(); for (int q = 0; q < 5; q++) { var kp = Farba(); for (float x = 20; x < 236; x += 3f) Stempel(ox, oy, x, 60f + q * 30f + Mathf.Sin(x * .05f + q) * 8f, 16f, q == 0 ? tlo : kp); }
                Stempel(ox, oy, 128f, 150f, 42f, new Color32(250, 205, 60, 255)); for (int q = 0; q < 12; q++) { float a = q * Mathf.PI / 6f; Kreska(ox, oy, new Vector2(128f + Mathf.Cos(a) * 50f, 150f + Mathf.Sin(a) * 50f), new Vector2(128f + Mathf.Cos(a) * 66f, 150f + Mathf.Sin(a) * 66f), 4f, new Color32(250, 205, 60, 255)); }
                for (int q = 0; q < 7; q++) { float x = 30f + q * 30f, h = 20f + rnd.Next(30); Kreska(ox, oy, new Vector2(x, 34f), new Vector2(x, 34f + h), 12f, czarny); }
                continue;
            }
            int liter = rodzaj == 1 ? 6 : 4; float szer = 200f / liter; var fill = Farba(); var fill2 = Farba();
            for (int etap = 0; etap < (rodzaj == 1 ? 1 : 2); etap++)
                for (int l = 0; l < liter; l++)
                {
                    var lr = new System.Random(k * 97 + l * 13 + 5); float bx = 28f + l * szer, by = rodzaj == 1 ? 40f : 70f, bw = szer * .8f, bh = rodzaj == 1 ? 170f : 110f;
                    if (rodzaj == 1)
                    {   // pixação: wysokie kanciaste litery, cienka czarna kreska
                        var p0 = new Vector2(bx, by); var p1 = new Vector2(bx + (float)lr.NextDouble() * 6f, by + bh);
                        Kreska(ox, oy, p0, p1, 3f, czarny);
                        for (int q = 0; q < 2 + lr.Next(2); q++) { float yy = by + bh * (float)lr.NextDouble(); Kreska(ox, oy, new Vector2(bx, yy), new Vector2(bx + bw, yy + ((float)lr.NextDouble() - .5f) * 50f), 3f, czarny); }
                        if (lr.Next(2) == 0) Kreska(ox, oy, new Vector2(bx + bw, by + bh * .3f), new Vector2(bx + bw, by + bh), 3f, czarny);
                        continue;
                    }
                    // tag / „bąbel”: łamana z 4–5 punktów; najpierw czarny obrys, potem wypełnienie
                    int np = 4 + lr.Next(2); var pts = new Vector2[np];
                    for (int q = 0; q < np; q++) pts[q] = new Vector2(bx + (float)lr.NextDouble() * bw, by + (float)lr.NextDouble() * bh);
                    float rad = rodzaj == 2 ? 15f : 7f; var kol = etap == 0 ? czarny : (l % 2 == 0 ? fill : fill2);
                    for (int q = 0; q + 1 < np; q++) Kreska(ox, oy, pts[q], pts[q + 1], etap == 0 ? rad + 4f : rad, kol);
                    if (etap == 1 && rodzaj == 2) Stempel(ox, oy, pts[0].x - 4f, pts[0].y + 5f, 4f, bialy);
                    if (etap == 1 && lr.Next(3) == 0) Kreska(ox, oy, pts[np - 1], pts[np - 1] + new Vector2(0f, -30f - lr.Next(30)), 2.5f, kol);   // zaciek
                }
        }
        Directory.CreateDirectory(BraxRegion.Out + "/Fawela");
        string path = BraxRegion.Out + "/Fawela/graffiti_atlas.png";
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, false); tx.SetPixels32(px); tx.Apply();
        File.WriteAllBytes(path, tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.maxTextureSize = 1024; imp.SaveAndReimport(); }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!tex) throw new Exception("atlas graffiti nie wczytany: " + path);
        var m = BraxCityMaterials.Textured("Fawela: graffiti (atlas malowany kodem)", tex, null, null, 1f);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Roughness", .9f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
        EditorUtility.SetDirty(m);
        return m;
    }
}

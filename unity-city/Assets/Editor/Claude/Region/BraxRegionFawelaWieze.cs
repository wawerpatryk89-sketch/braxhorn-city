using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// WIEŻOWCE MUROWANE NOWYCH DZIELNIC (właściciel 10.11 00:04: „wieżowce zrobić murowane, w stylu jak policja, szpital fajnie
// zrobiony… te twoje to tragedie… wieżowce mają być wysokie”). Budowane Z KODU tym samym zestawem co fawela (siatka Bud, rama
// ściany Fr, Sciana z ościeżami, okna z pokojem za szybą) — dlatego w tej klasie; stoją w nowych dzielnicach w miejscu 14
// jednostek „centrum” ze Sketchfab (czarne ściany, schody pożarowe na szkle).
// BRYŁA: cokół z kamienia z witrynami i portalami wejść (2 kondygnacje), trzon, uskok, korona z ciężkim gzymsem; uskoki jak
// w wieżowcach lat 30. OKNA na siatce 3.6 m świata (pokój za szybą nigdy nie ma ściany w środku okna), pionowo w osiach przez
// wszystkie uskoki. ELEWACJA: filary z cegły między oknami, parapety i nadproża z kamienia, pasy kamienne co kilka pięter,
// gzymsy na każdym uskoku, attyki z kamienną czapką; na dachu nadbudówka, zbiornik wody na stalowej ramie, maszt.
// 5 stylów muru (cegła czerwona / piaskowa / ciemny klinkier / wapień / brąz) — fotoskany Poly Haven. Wysokość 22–31 kondygnacji.
public static partial class BraxRegionFawela
{
    public static readonly bool WiezeMurowaneWlaczone = true;
    internal static int wiez, wiezOkien, wiezTrojkatow;
    const float WG = 3.6f;                                   // siatka okien (m świata) = komórka pokoju za szybą
    static float WHk => 3.4f * K;                            // kondygnacja
    static float WHp => 1.3f * 3.4f * K;                     // parter (witryny, portale)

    sealed class StylWiezy { public string nazwa; public Material mur, kamien, cokol; public Color murK = Color.white, kamienK = Color.white, cokolK = Color.white; }
    static StylWiezy[] stylWiez; static Material wRama, wDach, wDrzwi, wStal, wPlotno, wDrewno, wLampa, wMiedz;
    static readonly Dictionary<string, Material> wOkna = new Dictionary<string, Material>();

    static void WiezeMaterialy()
    {
        if (stylWiez != null && stylWiez[0].mur) return;
        var b = Color.white;
        var piaskowiec = T("wieże: piaskowiec ciosany (gzymsy, parapety)", 2.4f, b, .85f, "sandstone_blocks_08", "white_plaster_02", "concrete_wall_008");
        var wapien = T("wieże: wapień w płytach (cokoły, portale)", 2.2f, b, .7f, "marble_01", "sandstone_blocks_08", "white_plaster_02");
        var granit = T("wieże: granit szary w płytach", 2.0f, b, .55f, "granite_tile_03", "granite_tile_02", "concrete_wall_008");
        var granitC = T("wieże: granit ciemny (cokoły)", 2.0f, b, .5f, "granite_tile_04", "granite_tile_02", "concrete_wall_008");
        stylWiez = new[]
        {
            new StylWiezy { nazwa = "cegła czerwona, piaskowiec", mur = T("wieże: cegła czerwona", 1.9f, b, .9f, "red_brick", "brick_wall_001", "red_brick_03"), kamien = piaskowiec, cokol = wapien },
            new StylWiezy { nazwa = "cegła piaskowa, wapień", mur = T("wieże: cegła piaskowa", 1.9f, b, .9f, "brick_wall_12", "brick_wall_001", "red_brick_03"), kamien = wapien, cokol = granit, murK = new Color(.93f, .90f, .84f) },
            new StylWiezy { nazwa = "ciemny klinkier, granit", mur = T("wieże: klinkier ciemny", 1.9f, b, .85f, "dark_brick_wall", "brick_wall_001", "red_brick_03"), kamien = granit, cokol = granitC, murK = new Color(1f, .96f, .92f) },
            new StylWiezy { nazwa = "wapień, granit", mur = piaskowiec, kamien = wapien, cokol = granit, murK = new Color(.98f, .96f, .92f) },
            new StylWiezy { nazwa = "cegła brązowa, piaskowiec", mur = T("wieże: cegła brązowa", 1.9f, b, .9f, "factory_brick", "brick_wall_001", "red_brick_03"), kamien = piaskowiec, cokol = granitC },
        };
        wRama = Tm("wieże: stolarka okien (lakier grafitowy)", .9f, new Color(.20f, .21f, .22f), .45f, "metal_plate", "painted_metal_shutter");
        wStal = Tm("wieże: stal malowana (daszki, rama zbiornika, maszt)", .9f, new Color(.30f, .31f, .32f), .5f, "metal_plate", "painted_metal_shutter");
        wDach = T("wieże: dach płaski (papa z posypką)", 2.0f, new Color(.42f, .42f, .43f), .95f, "concrete_floor_02", "concrete_wall_008");
        wDrzwi = Tm("wieże: drzwi wejściowe (ciemne drewno)", 1.2f, b, .5f, "dark_wood", "wood_cabinet_worn_long", "wood_planks");
        wDrewno = Tm("wieże: klepki zbiornika wody", 1.2f, new Color(.72f, .6f, .48f), .85f, "wood_planks", "wood_plank_wall", "wood_planks_grey");
        wPlotno = Tm("wieże: markizy (płótno)", .9f, b, .95f, "rough_linen", "leather_white", "terry_cloth");
        wMiedz = Tm("wieże: blacha miedziana z patyną (dachy namiotowe)", 1.4f, new Color(.62f, .92f, .80f), .6f, "metal_plate", "painted_metal_shutter", "corrugated_iron");
        wLampa = BraxRegionMeble.Swiecacy("Wieże: lampa wejścia (ciepła)", new Color(1f, .88f, .66f), 1.5f);
    }

    static Material WOkno(float kondygnacja, float floor0)
    {
        string kl = kondygnacja.ToString("F2") + "|" + Mathf.Repeat(floor0, kondygnacja).ToString("F2");
        if (wOkna.TryGetValue(kl, out var m) && m) return m;
        try { m = BraxUliceKit.OknoPokoj(WG, kondygnacja, floor0, 0, 5, 4.6f, 0f); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_WIEZE okno z pokojem: " + e.Message); }
        if (!m) m = BraxCityMaterials.Surface("Wieże: szyba (zapasowa)", new Color(.16f, .2f, .24f), .1f, .6f);
        return wOkna[kl] = m;
    }

    /// <summary>Wieżowiec murowany w obrysie w × d (osie świata, środek x/z, podstawa y). Zwraca korzeń „Budynek id” (siatki per
    /// materiał, kolizja bryły, LODGroup). Styl i wysokość z kolejności (pewna różnorodność w rzędzie) i z nazwy.</summary>
    public static GameObject WiezaMurowana(Transform rodzic, string id, float x, float yBase, float z, float w, float d)
    {
        WiezeMaterialy();
        int nr = wiez++; int ziarno = 0; foreach (char ch in id) ziarno = ziarno * 31 + ch;
        var r = new System.Random(ziarno); var st = stylWiez[nr % stylWiez.Length];
        var korzen = new GameObject("Budynek " + id).transform; korzen.SetParent(rodzic, false); korzen.position = Vector3.zero;
        var siatki = new Dictionary<Material, Bud>();
        Bud WB(Material m) { if (!siatki.TryGetValue(m, out var bb)) siatki[m] = bb = new Bud(); return bb; }
        float Hk = WHk, Hp = WHp, k = K;
        float Y(int i) => yBase + (i <= 0 ? 0f : Hp + (i - 1) * Hk);
        // ---- uskoki: cokół (0–2), trzon, uskok górny, korona
        bool dlugi = Mathf.Max(w, d) > 1.6f * Mathf.Min(w, d);
        int N = dlugi ? 22 + r.Next(4) : 24 + r.Next(8);
        var R0 = Rect.MinMaxRect(x - w * .5f, z - d * .5f, x + w * .5f, z + d * .5f);
        Rect Wciecie(Rect q, float a, float wzdluz) { bool wx = q.width >= q.height; return Rect.MinMaxRect(q.xMin + a + (wx ? wzdluz : 0f), q.yMin + a + (wx ? 0f : wzdluz), q.xMax - a - (wx ? wzdluz : 0f), q.yMax - a - (wx ? 0f : wzdluz)); }
        float dl = Mathf.Max(w, d), kr = Mathf.Min(w, d);
        var R1 = Wciecie(R0, 1.4f + (float)r.NextDouble() * .8f, 0f);
        var R2 = Wciecie(R1, 2.6f + (float)r.NextDouble() * 1.2f, dlugi ? (dl - kr * 1.5f) * .5f * .55f : 0f);
        var R3 = Wciecie(R2, 2.4f + (float)r.NextDouble() * 1.0f, dlugi ? (dl - kr * 1.5f) * .5f * .3f : 0f);
        int n1 = 3, n2 = Mathf.RoundToInt(N * (dlugi ? .5f : .62f)), n3 = Mathf.RoundToInt(N * .86f);
        var pietra = new[] { (R0, 0, n1), (R1, n1, n2), (R2, n2, n3), (R3, n3, N) };
        float brud0 = yBase;
        Bud Mur() { var bb = WB(st.mur); bb.kolor = p => st.murK * (.9f + .1f * Szum(p.x * .13f + p.y * .07f, p.z * .13f + p.y * .05f)) * Mathf.Lerp(.84f, 1f, S01(brud0, brud0 + 4f, p.y)); return bb; }
        Bud Kam() { var bb = WB(st.kamien); bb.kolor = p => st.kamienK * (.92f + .08f * Szum(p.x * .2f + p.y * .1f, p.z * .2f)); return bb; }
        Bud Cok() { var bb = WB(st.cokol); bb.kolor = p => st.cokolK * (.9f + .1f * Szum(p.x * .2f, p.z * .2f + p.y * .1f)) * Mathf.Lerp(.8f, 1f, S01(brud0, brud0 + 1.5f, p.y)); return bb; }
        var rama = WB(wRama).K(Color.white); var oknaP = WB(WOkno(Hk, yBase + Hp)); var oknaW = WB(WOkno(Hp, yBase));
        var kolMarkiz = new[] { new Color(.55f, .14f, .12f), new Color(.12f, .30f, .22f), new Color(.14f, .20f, .38f), new Color(.82f, .72f, .5f) }[nr % 4];
        const float Gl = .26f, Fil = .45f, Wys = .14f;   // głębokość ościeża, pół szerokości filara, wysunięcie filara
        float ww = 1.9f, wh = 2.35f, par = .98f;

        for (int ti = 0; ti < pietra.Length; ti++)
        {
            var (rc, f0, f1) = pietra[ti]; bool ostatni = ti == pietra.Length - 1; float y0 = Y(f0), y1 = Y(f1), att = ostatni ? 1.5f : 1.1f;
            var sciany = new[]
            {
                Fr.Nowa(new Vector3(rc.xMin, 0f, rc.yMax), Vector3.forward, rc.width), Fr.Nowa(new Vector3(rc.xMax, 0f, rc.yMin), Vector3.back, rc.width),
                Fr.Nowa(new Vector3(rc.xMin, 0f, rc.yMin), Vector3.left, rc.height), Fr.Nowa(new Vector3(rc.xMax, 0f, rc.yMax), Vector3.right, rc.height)
            };
            for (int si = 0; si < 4; si++)
            {
                var F = sciany[si]; float L = F.L;
                // linie siatki świata na tej ścianie (osie filarów)
                bool wzdX = Mathf.Abs(F.R.x) > .5f; float a0 = wzdX ? F.A.x : F.A.z, kier = wzdX ? F.R.x : F.R.z;
                var linie = new List<float>();
                for (int j = Mathf.FloorToInt(Mathf.Min(a0, a0 + kier * L) / WG) - 1; j <= Mathf.CeilToInt(Mathf.Max(a0, a0 + kier * L) / WG) + 1; j++)
                { float s = (j * WG - a0) / kier; if (s > Fil + .15f && s < L - Fil - .15f) linie.Add(s); }
                linie.Sort();
                var osie = new List<float>(); for (int j = 0; j + 1 < linie.Count; j++) osie.Add((linie[j] + linie[j + 1]) * .5f);
                int wejscie = (f0 == 0 && si < 2 && osie.Count >= 3) || (f0 == 0 && si >= 2 && osie.Count >= 7) ? osie.Count / 2 : -1;
                // ---- otwory: witryny / portal na parterze, okna wyżej
                var otwP = new List<Rect>(); var otwG = new List<(Rect r, int pietro)>();
                for (int i = f0; i < f1; i++)
                    for (int q = 0; q < osie.Count; q++)
                    {
                        float sc = osie[q], yy = Y(i);
                        if (i == 0) otwP.Add(q == wejscie ? Rect.MinMaxRect(sc - 1.25f, yy, sc + 1.25f, yy + 3.7f) : Rect.MinMaxRect(sc - 1.28f, yy + .55f, sc + 1.28f, yy + Hp - 1.25f));
                        else otwG.Add((Rect.MinMaxRect(sc - ww * .5f, yy + par, sc + ww * .5f, yy + par + (ostatni && i >= f1 - 2 ? wh + .25f : wh)), i));
                    }
                // ---- mur: parter z kamienia cokołu, wyżej cegła; attyka nad uskokiem
                float yMur0 = y0;
                if (f0 == 0) { Sciana(Cok(), F, 0f, L, y0, Y(1), otwP, float.NaN, Gl); yMur0 = Y(1); }
                // korona: dwie ostatnie kondygnacje i attyka w kamieniu (wieńczy bryłę jak w wieżowcach lat 30.)
                float yKor = ostatni ? Y(f1 - 2) : y1 + att;
                Sciana(Mur(), F, 0f, L, yMur0, yKor, otwG.Where(o => o.r.yMin < yKor).Select(o => o.r).ToList(), float.NaN, Gl);
                if (ostatni) Sciana(Kam(), F, 0f, L, yKor, y1 + att, otwG.Where(o => o.r.yMin >= yKor).Select(o => o.r).ToList(), float.NaN, Gl);
                // lico wewnętrzne attyki (widać z wyższych pięter i z tarasu Tower)
                Mur().Quad(F.P(L, y1, .32f), F.P(0f, y1, .32f), F.P(0f, y1 + att, .32f), F.P(L, y1 + att, .32f));
                // ---- filary w osiach siatki i masywy narożne
                foreach (float s in linie)
                {
                    if (f0 == 0) BoxF(Cok(), F, s - Fil - .04f, s + Fil + .04f, y0, Y(1), -Wys - .05f, 0f, false, false);
                    BoxF(Mur(), F, s - Fil, s + Fil, yMur0, yKor, -Wys, 0f, false, false);
                    if (ostatni)
                    {   // filary korony z kamienia, wyciągnięte ponad attykę (sterczyny)
                        BoxF(Kam(), F, s - Fil, s + Fil, yKor, y1 + att + .45f, -Wys - .04f, .3f, true, false);
                    }
                }
                if (linie.Count > 0)
                    foreach (var (sa, sb) in new[] { (-Wys, linie[0]), (linie[linie.Count - 1], L + Wys) })
                    {
                        if (sb - sa < .3f) continue;
                        if (f0 == 0) BoxF(Cok(), F, sa - .04f, sb + .04f, y0, Y(1), -Wys - .05f, 0f, false, false);
                        BoxF(Mur(), F, sa, sb, yMur0, yKor, -Wys, 0f, false, false);
                        if (ostatni) BoxF(Kam(), F, sa, sb, yKor, y1 + att + .5f, -Wys - .04f, 0f, false, false);
                    }
                // ---- okna pięter
                foreach (var (o, i) in otwG)
                {
                    bool dokladne = i <= 7 || ostatni;
                    Pasek(oknaP, F, o.xMin, o.xMax, o.yMin, o.yMax, Gl - .06f);
                    float cs = (o.xMin + o.xMax) * .5f, gd = Gl - .075f;
                    Pasek(rama, F, cs - .03f, cs + .03f, o.yMin, o.yMax, gd);
                    if (dokladne)
                    {
                        Pasek(rama, F, o.xMin, o.xMin + .06f, o.yMin, o.yMax, gd); Pasek(rama, F, o.xMax - .06f, o.xMax, o.yMin, o.yMax, gd);
                        Pasek(rama, F, o.xMin, o.xMax, o.yMin, o.yMin + .06f, gd); Pasek(rama, F, o.xMin, o.xMax, o.yMax - .06f, o.yMax, gd);
                        Pasek(rama, F, o.xMin, o.xMax, o.yMin + (o.yMax - o.yMin) * .56f - .025f, o.yMin + (o.yMax - o.yMin) * .56f + .025f, gd);
                    }
                    BoxF(Kam(), F, o.xMin - .14f, o.xMax + .14f, o.yMin - .16f, o.yMin, -.1f, .05f, false, dokladne);             // parapet kamienny
                    BoxF(Kam(), F, o.xMin - .1f, o.xMax + .1f, o.yMax, o.yMax + .34f, -.05f, 0f, false, true);                    // nadproże
                    // płycina podokienna (kamień ciemniejszy) od nadproża niższego okna do parapetu — pionowy pas okien między filarami
                    if (i > f0 || f0 > 0) { var pl = WB(st.kamien); pl.kolor = pp => st.kamienK * .78f * (.94f + .06f * Szum(pp.x * .3f, pp.y * .3f + pp.z * .3f)); Pasek(pl, F, o.xMin, o.xMax, Mathf.Max(o.yMin - (Hk - (o.yMax - o.yMin)) + .34f, yMur0 + .14f), o.yMin - .16f, -.03f); }
                    wiezOkien++;
                }
                // ---- parter: cokolik, witryny z naświetlem i markizą, portal wejścia
                if (f0 == 0)
                {
                    BoxF(WB(st.cokol).K(st.cokolK * .7f), F, -Wys - .1f, L + Wys + .1f, y0 - .3f, y0 + .5f, -Wys - .12f, 0f, false, false);
                    for (int q = 0; q < otwP.Count; q++)
                    {
                        var o = otwP[q]; float gd = Gl - .075f;
                        if (q == wejscie) { PortalWiezy(F, o, WB, st, oknaW, rama, id, si); continue; }
                        Pasek(oknaW, F, o.xMin, o.xMax, o.yMin, o.yMax, Gl - .06f);
                        float yn = o.yMin + (o.yMax - o.yMin) * .74f;
                        foreach (float s in new[] { o.xMin, o.xMin + (o.xMax - o.xMin) / 3f - .03f, o.xMin + (o.xMax - o.xMin) * 2f / 3f - .03f, o.xMax - .07f }) Pasek(rama, F, s, s + .07f, o.yMin, o.yMax, gd);
                        Pasek(rama, F, o.xMin, o.xMax, o.yMin, o.yMin + .08f, gd); Pasek(rama, F, o.xMin, o.xMax, o.yMax - .08f, o.yMax, gd); Pasek(rama, F, o.xMin, o.xMax, yn - .04f, yn + .04f, gd);
                        BoxF(Kam(), F, o.xMin - .1f, o.xMax + .1f, o.yMin - .12f, o.yMin, -.08f, .05f, false, true);
                        if ((q * 7 + nr * 3 + si) % 5 < 2)
                        {   // markiza płócienna nad witryną (prześwit 2.6 m × K)
                            var mk = WB(wPlotno).K(kolMarkiz); float ym = Mathf.Max(yn, y0 + 2.7f * k);
                            mk.Quad2(F.P(o.xMin - .1f, ym + .55f, 0f), F.P(o.xMax + .1f, ym + .55f, 0f), F.P(o.xMax + .1f, ym, -1.3f), F.P(o.xMin - .1f, ym, -1.3f));
                            mk.Quad2(F.P(o.xMin - .1f, ym, -1.3f), F.P(o.xMax + .1f, ym, -1.3f), F.P(o.xMax + .1f, ym - .22f, -1.3f), F.P(o.xMin - .1f, ym - .22f, -1.3f));
                            foreach (float s in new[] { o.xMin - .08f, o.xMax + .08f }) WB(wStal).K(Color.white).Cyl(F.P(s, ym - .3f, 0f), F.P(s, ym, -1.28f), .014f, 4, -1f, false);
                        }
                    }
                    // gzyms nad parterem
                    BoxF(Kam(), F, -Wys - .2f, L + Wys + .2f, Y(1) - .28f, Y(1) + .12f, -Wys - .22f, 0f, false, true);
                }
                // ---- pasy kamienne co 5 pięter w trzonie, gzyms i czapka attyki na uskoku
                for (int i = Mathf.Max(f0, 2) + 1; i < f1; i++) if ((i - n1) % 5 == 0 && i > n1) BoxF(Kam(), F, -Wys - .03f, L + Wys + .03f, Y(i) - .1f, Y(i) + .22f, -Wys - .04f, 0f, false, true);
                float gz = ostatni ? .6f : .36f;
                BoxF(Kam(), F, -Wys - gz, L + Wys + gz, y1 - .35f, y1 + .1f, -Wys - gz, 0f, false, true);
                if (ostatni) BoxF(Kam(), F, -Wys - gz - .25f, L + Wys + gz + .25f, y1 + .1f, y1 + .42f, -Wys - gz - .25f, 0f, false, true);
                BoxF(Kam(), F, -Wys - .05f, L + Wys + .05f, y1 + att, y1 + att + .14f, -Wys - .05f, .36f, true, true);
            }
            // dach uskoku (taras techniczny)
            var dach = WB(wDach).K(Color.white);
            dach.Quad(new Vector3(rc.xMin + .3f, y1 + .02f, rc.yMax - .3f), new Vector3(rc.xMax - .3f, y1 + .02f, rc.yMax - .3f), new Vector3(rc.xMax - .3f, y1 + .02f, rc.yMin + .3f), new Vector3(rc.xMin + .3f, y1 + .02f, rc.yMin + .3f));
        }
        // ---- dach korony: nadbudówka maszynowni, zbiornik wody na stalowej ramie, maszt, wywiewki
        {
            float yd = Y(N) + .02f; var rc = R3; var c0 = rc.center;
            float nx = Mathf.Min(7f, rc.width * .4f), nz = Mathf.Min(5.5f, rc.height * .4f);
            if (!(nr % 3 == 1 && !dlugi)) {
            var nb = Mur(); nb.Box(new Vector3(c0.x - nx * .5f - 1.5f, yd, c0.y - nz * .5f), new Vector3(c0.x + nx * .5f - 1.5f, yd + 3.6f, c0.y + nz * .5f));
            Kam().Box(new Vector3(c0.x - nx * .5f - 1.7f, yd + 3.6f, c0.y - nz * .5f - .2f), new Vector3(c0.x + nx * .5f - 1.3f, yd + 3.85f, c0.y + nz * .5f + .2f), true);
            WB(wDrzwi).K(new Color(.5f, .5f, .5f)).Box(new Vector3(c0.x + nx * .5f - 1.5f, yd, c0.y - .5f), new Vector3(c0.x + nx * .5f - 1.46f, yd + 2.1f * k, c0.y + .5f));
            }
            float zx = c0.x + nx * .5f + 1.2f, zz = c0.y + (nr % 2 == 0 ? 1.2f : -1.2f), zr = 1.7f, zy = yd + 2.6f;
            bool namiot = nr % 3 == 1 && !dlugi;
            if (namiot)
            {   // dach namiotowy z miedzi z patyną nad koroną, z latarnią i iglicą (sylweta miasta)
                var md = WB(wMiedz).K(Color.white); float hn = Mathf.Min(rc.width, rc.height) * .55f, ya = yd + 1.3f;
                var a = new Vector3(rc.xMin + .5f, ya, rc.yMin + .5f); var b2 = new Vector3(rc.xMax - .5f, ya, rc.yMin + .5f); var c2 = new Vector3(rc.xMax - .5f, ya, rc.yMax - .5f); var d2 = new Vector3(rc.xMin + .5f, ya, rc.yMax - .5f);
                var sz = new Vector3(c0.x, ya + hn, c0.y);
                md.Tri(b2, a, sz); md.Tri(c2, b2, sz); md.Tri(d2, c2, sz); md.Tri(a, d2, sz);
                Kam().Box(new Vector3(rc.xMin + .4f, yd, rc.yMin + .4f), new Vector3(rc.xMax - .4f, ya, rc.yMax - .4f));
                WB(wStal).K(Color.white).Cyl(sz - Vector3.up * .4f, sz + Vector3.up * 6f, .1f, 6, .02f, false);
            }
            else if (zx + zr < rc.xMax - 1.2f)
            {
                var stal = WB(wStal).K(Color.white);
                foreach (float ax in new[] { -1.2f, 1.2f }) foreach (float az in new[] { -1.2f, 1.2f }) { stal.Box(new Vector3(zx + ax - .07f, yd, zz + az - .07f), new Vector3(zx + ax + .07f, zy, zz + az + .07f)); }
                stal.Box(new Vector3(zx - 1.4f, zy - .12f, zz - 1.4f), new Vector3(zx + 1.4f, zy, zz + 1.4f), true);
                foreach (float az in new[] { -1.2f, 1.2f }) stal.Cyl(new Vector3(zx - 1.2f, yd + .3f, zz + az), new Vector3(zx + 1.2f, zy - .2f, zz + az), .03f, 4, -1f, false);
                var kl = WB(wDrewno).K(Color.white);
                kl.Cyl(new Vector3(zx, zy, zz), new Vector3(zx, zy + 3.1f, zz), zr, 16, zr * .96f, false);
                kl.Cyl(new Vector3(zx, zy + 3.1f, zz), new Vector3(zx, zy + 4.0f, zz), zr * 1.04f, 16, .12f, false);
                foreach (float hy in new[] { .5f, 1.6f, 2.7f }) stal.Cyl(new Vector3(zx, zy + hy, zz), new Vector3(zx, zy + hy + .06f, zz), zr + .02f, 16, zr + .02f, false);
            }
            var maszt = new Vector3(c0.x - 1.5f, yd + 3.85f, c0.y);
            if (!namiot) WB(wStal).K(Color.white).Cyl(maszt, maszt + Vector3.up * (7f + (float)r.NextDouble() * 5f), .07f, 6, .03f, false);
            for (int q = 0; q < 3 && !namiot; q++)
            {
                float vx = rc.xMin + 2f + (float)r.NextDouble() * Mathf.Max(.1f, rc.width - 4f), vz = rc.yMin + 1.6f + (q % 2) * Mathf.Max(.1f, rc.height - 3.2f);
                if (Mathf.Abs(vx - c0.x + 1.5f) < nx * .5f + 1f && Mathf.Abs(vz - c0.y) < nz * .5f + 1f) continue;
                WB(wStal).K(new Color(.8f, .8f, .8f)).Box(new Vector3(vx - .5f, yd, vz - .4f), new Vector3(vx + .5f, yd + .9f, vz + .4f));
            }
        }
        // ---- siatki, kolizja bryły, LOD
        var rend = new List<Renderer>();
        foreach (var kv in siatki)
        {
            bool szklo = kv.Key.shader && kv.Key.shader.name.Contains("Okno");
            var o = kv.Value.Obiekt("Wieża " + id + ": " + kv.Key.name, korzen, kv.Key, !szklo, false);
            if (o) { rend.Add(o.GetComponent<Renderer>()); wiezTrojkatow += kv.Value.t.Count / 3; }
        }
        float top = Y(N) + 1.5f;
        var col = new GameObject("Budynek " + id + " kolizja").AddComponent<BoxCollider>(); col.transform.SetParent(korzen, false);
        col.transform.position = new Vector3(x, (yBase + top) * .5f, z); col.size = new Vector3(w + 2f * Wys, top - yBase, d + 2f * Wys);
        var lod = korzen.gameObject.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.004f, rend.ToArray()) }); lod.RecalculateBounds();
        Debug.Log("BRAXHORN_WIEZA " + id + ": styl „" + st.nazwa + "”, kondygnacji " + N + ", wysokość " + (top - yBase).ToString("F0") + " m, obrys " + w.ToString("F1") + " × " + d.ToString("F1") + " m, okien dotąd " + wiezOkien + ", trójkątów dotąd " + wiezTrojkatow);
        return korzen.gameObject;
    }

    // portal wejścia: obramienie z kamienia wysunięte 30 cm, dwuskrzydłowe drzwi z przeszkleniem, naświetle, daszek na cięgnach, lampy, stopień
    static void PortalWiezy(Fr F, Rect o, Func<Material, Bud> WB, StylWiezy st, Bud szklo, Bud rama, string id, int si)
    {
        var km = WB(st.kamien).K(st.kamienK); float cs = (o.xMin + o.xMax) * .5f, yD = o.yMin + 2.55f * K * .9f, g = .2f;
        BoxF(km, F, o.xMin - .55f, o.xMin, o.yMin, o.yMax + .5f, -.3f, 0f, false, false); BoxF(km, F, o.xMax, o.xMax + .55f, o.yMin, o.yMax + .5f, -.3f, 0f, false, false);
        BoxF(km, F, o.xMin - .7f, o.xMax + .7f, o.yMax, o.yMax + .62f, -.36f, 0f, false, true);
        BoxF(WB(st.cokol).K(st.cokolK * .8f), F, o.xMin - .7f, o.xMax + .7f, o.yMin - .3f, o.yMin + .02f, -1.1f, 0f, false, false);   // stopień
        var dr = WB(wDrzwi).K(Color.white);
        foreach (var (a, b) in new[] { (o.xMin + .06f, cs - .02f), (cs + .02f, o.xMax - .06f) })
        {
            BoxF(dr, F, a, b, o.yMin + .03f, yD, g - .05f, g, false, false);
            Pasek(szklo, F, a + .16f, b - .16f, o.yMin + 1.0f, yD - .2f, g - .055f);
            BoxF(WB(wStal).K(new Color(.85f, .75f, .45f)), F, (a + b) * .5f + (a < cs ? .32f : -.4f), (a + b) * .5f + (a < cs ? .4f : -.32f), o.yMin + 1.0f, o.yMin + 1.5f, g - .1f, g - .05f, false, true);   // pochwyt mosiężny
        }
        Pasek(szklo, F, o.xMin + .06f, o.xMax - .06f, yD + .08f, o.yMax - .06f, g - .01f);                                             // naświetle
        foreach (float s in new[] { o.xMin, o.xMax - .06f }) Pasek(rama, F, s, s + .06f, o.yMin, o.yMax, g - .03f);
        Pasek(rama, F, o.xMin, o.xMax, yD, yD + .08f, g - .03f); Pasek(rama, F, o.xMin, o.xMax, o.yMax - .06f, o.yMax, g - .03f); Pasek(rama, F, cs - .03f, cs + .03f, yD + .08f, o.yMax - .06f, g - .02f);
        var stal = WB(wStal).K(Color.white); float yk = o.yMax + .72f;
        BoxF(stal, F, o.xMin - .9f, o.xMax + .9f, yk, yk + .14f, -1.9f, 0f, false, true);                                             // daszek
        foreach (float s in new[] { o.xMin - .7f, o.xMax + .7f }) stal.Cyl(F.P(s, yk + .14f, -1.7f), F.P(s, yk + 1.5f, -.02f), .018f, 5, -1f, false);
        foreach (float s in new[] { o.xMin - .28f, o.xMax + .28f })
        {
            BoxF(stal, F, s - .07f, s + .07f, o.yMin + 2.0f * K, o.yMin + 2.0f * K + .1f, -.5f, -.3f, false, true);
            BoxF(WB(wLampa).K(Color.white), F, s - .09f, s + .09f, o.yMin + 2.0f * K - .32f, o.yMin + 2.0f * K, -.52f, -.34f, false, true);
        }
    }
}

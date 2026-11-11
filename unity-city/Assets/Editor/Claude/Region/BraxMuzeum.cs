using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// MUZEUM MIASTA BRAXHORN w zabytku KINNAIRD (11.11.2026, właściciel 20:06: „chciałem, żebyś ten zabytkowy budynek zrobił
// funkcjonalny. Że tam można było wejść, jakieś takie muzeum… A z tego też jest sporo takich rzeczy różnych, ta Poly Haven pod to”;
// 21:52: „Wszystko ma być dopieszczone”). Wejście od promenady (lico północne zabytku, z = PromenadaZ), w osi budynku:
//  • WEJŚCIE: lico fasady wyszukane z wierzchołków modelu (parter, oś ±2.5 m), w modelu wycięty otwór 3.1 × 3.8 m (trójkąty
//    w bryle otworu usuwane z kopii siatek), ościeża z kamienia w grubości muru, drzwi dwuskrzydłowe dębowe płycinowe z klamkami
//    (BraxDrzwiLokalu.Drewniane, otwierane) W LICU FASADY (23:09 — wcześniej cofnięte), schody nie są potrzebne (posadzka =
//    poziom promenady). PORTAL z kamienia: pilastry na cokołach z kapitelami, fryz z literami 3D MUZEUM MIASTA BRAXHORN (Bebas,
//    grafit), gzyms; na pilastrach kinkiety (BraxMuzeumKod.Kinkiet) zamiast latarni na chodniku.
//  • SALA na parterze (4.3 m — pod stropem pierwszego piętra modelu, cała szerokość zabytku bez 1.5 m przy ścianach): posadzka z marmuru, ściany z tynku w ciepłym
//    odcieniu z boazerią dębową do 1.1 m i listwą, gzyms pod sufitem, sufit tynkowy, dwa rzędy kolumn toczonych (baza, trzon,
//    kapitel), recepcja z kasą biletów, ławki muzealne (skóra), barierki z linką na słupkach chrom, gabloty szklane na postumentach
//    z eksponatami Poly Haven (popiersie, rzeźba konia, wazy, zegar, maszyna do pisania, telewizor i radio — dział techniki),
//    obrazy w ramach na ścianach, tabliczki opisowe; światło ciepłe z sufitu.
//  • KOLIZJE: kolizja całej bryły zastąpiona: ściany sali z przerwą na drzwi, bryła nad salą (od 6.2 m) i kolizje mebli.
public static partial class BraxRegionMiastoB
{
    static int muzEksponatow, muzWycietych;

    static void Muzeum()
    {
        var kin = ndBudynki.FirstOrDefault(q => q && q.name == "Budynek KINNAIRD");
        var b = NoweBudynki.First(q => q.id == "KINNAIRD");
        if (!kin) { NdLog("MUZEUM: brak zabytku KINNAIRD w scenie (paczka?) — muzeum nie powstało"); return; }
        float y0 = kin.transform.position.y, k = K;
        float sx0 = b.x - b.w * .5f, sx1 = b.x + b.w * .5f, sz0 = b.z - b.d * .5f, sz1 = b.z + b.d * .5f;
        // 1) lico fasady przy wejściu: najdalej wysunięte (+z) wierzchołki parteru w osi ±2.5 m
        float zf = float.NegativeInfinity;
        var siatki = kin.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh).ToList();
        foreach (var mf in siatki)
        {
            var M = mf.transform.localToWorldMatrix;
            try { foreach (var v in mf.sharedMesh.vertices) { var w = M.MultiplyPoint3x4(v); if (w.y > y0 + .3f && w.y < y0 + 4f && Mathf.Abs(w.x - b.x) < 2.5f && w.z < sz1 + 1.5f) zf = Mathf.Max(zf, w.z); } }
            catch (Exception) { }
        }
        if (float.IsNegativeInfinity(zf)) zf = sz1;
        float dw = 3.1f, dh = 3.8f, gMur = 1.6f;                      // otwór (szer., wys.), głębokość wycięcia w murze
        var otwor = new Bounds(new Vector3(b.x, y0 + dh * .5f - .05f, zf - gMur * .5f + .15f), new Vector3(dw, dh + .1f, gMur + .3f));
        // 2) otwór w modelu: kopie siatek bez trójkątów w bryle otworu
        foreach (var mf in siatki)
        {
            var m = mf.sharedMesh; var M = mf.transform.localToWorldMatrix; bool zmiana = false;
            Vector3[] v; try { v = m.vertices; } catch (Exception) { continue; }
            var nowe = new List<int[]>();
            for (int s = 0; s < m.subMeshCount; s++)
            {
                var t = m.GetTriangles(s); var zost = new List<int>(t.Length);
                for (int i = 0; i < t.Length; i += 3)
                {
                    var c = (M.MultiplyPoint3x4(v[t[i]]) + M.MultiplyPoint3x4(v[t[i + 1]]) + M.MultiplyPoint3x4(v[t[i + 2]])) / 3f;
                    if (otwor.Contains(c)) { zmiana = true; muzWycietych++; continue; }
                    zost.Add(t[i]); zost.Add(t[i + 1]); zost.Add(t[i + 2]);
                }
                nowe.Add(zost.ToArray());
            }
            if (!zmiana) continue;
            var kopia = UnityEngine.Object.Instantiate(m); kopia.name = m.name + " (muzeum: otwór wejścia)";
            for (int s = 0; s < nowe.Count; s++) kopia.SetTriangles(nowe[s], s);
            System.IO.Directory.CreateDirectory(BraxRegion.Out + "/Muzeum"); BraxZapisAssetu.Utworz(kopia, BraxRegion.Out + "/Muzeum/otwor_" + BraxUliceKit.Bezpieczna(m.name) + "_" + muzWycietych + ".asset");
            mf.sharedMesh = kopia;
        }
        var g = new GameObject("MUZEUM MIASTA BRAXHORN (zabytek KINNAIRD)").transform; g.SetParent(kin.transform.parent, false);
        // 3) kolizje: bryła zastąpiona (ściany sali z przejściem, bryła nad salą)
        var stara = kin.transform.parent.Find("Budynek KINNAIRD kolizja"); if (stara) UnityEngine.Object.DestroyImmediate(stara.gameObject);
        float hS = 4.3f, xi0 = sx0 + 1.5f, xi1 = sx1 - 1.5f, zi0 = sz0 + 1.5f, zi1 = zf - gMur;   // sala
        float yTop = BraxRegion.Bounds(kin).max.y;
        void Kol(string n, Vector3 a, Vector3 c) => BraxRegionMeble.Kolizja(g, "Muzeum kolizja: " + n, (a + c) * .5f, new Vector3(Mathf.Abs(c.x - a.x), Mathf.Abs(c.y - a.y), Mathf.Abs(c.z - a.z)));
        Kol("bryła nad salą", new Vector3(sx0 + .05f, y0 + hS + .2f, sz0 + .05f), new Vector3(sx1 - .05f, Mathf.Max(yTop, y0 + hS + 1f), sz1 - .05f));
        Kol("ściana zach.", new Vector3(sx0 + .05f, y0, sz0 + .05f), new Vector3(xi0, y0 + hS + .2f, sz1 - .05f));
        Kol("ściana wsch.", new Vector3(xi1, y0, sz0 + .05f), new Vector3(sx1 - .05f, y0 + hS + .2f, sz1 - .05f));
        Kol("ściana płd.", new Vector3(xi0, y0, sz0 + .05f), new Vector3(xi1, y0 + hS + .2f, zi0));
        Kol("fasada zach. od wejścia", new Vector3(xi0, y0, zi1), new Vector3(b.x - dw * .5f, y0 + hS + .2f, sz1 - .05f));
        Kol("fasada wsch. od wejścia", new Vector3(b.x + dw * .5f, y0, zi1), new Vector3(xi1, y0 + hS + .2f, sz1 - .05f));
        Kol("nadproże wejścia", new Vector3(b.x - dw * .5f, y0 + dh, zi1), new Vector3(b.x + dw * .5f, y0 + hS + .2f, sz1 - .05f));
        // 4) wnętrze z kodu
        UlSiatka.ZaokraglenieBox = .008f;
        try { Sala(g, b, y0, xi0, xi1, zi0, zi1, hS, zf, dw, dh, gMur); }
        finally { UlSiatka.ZaokraglenieBox = 0f; }
        BraxUliceKit.Punkt("muzeum_wejscie", "MUZEUM MIASTA BRAXHORN — wejście od promenady (zabytek KINNAIRD)", new Vector3(b.x, y0, zf + 1.5f));
        BraxUliceKit.Punkt("muzeum_sala", "MUZEUM — sala wystawowa (parter)", new Vector3(b.x, y0, (zi0 + zi1) * .5f));
        NdLog("MUZEUM w KINNAIRD: lico wejścia z " + zf.ToString("0.00") + " (obrys " + sz1.ToString("0.00") + "), wycięte trójkąty otworu " + muzWycietych + ", sala " + (xi1 - xi0).ToString("0.0") + " × " + (zi1 - zi0).ToString("0.0") + " × " + hS + " m, eksponatów " + muzEksponatow);
    }

    static void Sala(Transform g, Bud b, float y0, float xi0, float xi1, float zi0, float zi1, float hS, float zf, float dw, float dh, float gMur)
    {
        float k = K;
        BraxUliceKit.UstawGrupe("Muzeum: sala", 90f);
        var marmur = BraxUliceKit.Tex("muzeum: posadzka marmur", 1.6f, null, new Color(.9f, .88f, .85f), .2f, "marble_01");
        // ściany: jasny stiuk w ciepłym odcieniu (white_plaster_02 wychodził szary jak beton — podgląd 23:10)
        var tynk = BraxUliceKit.Tex("muzeum: ściany stiuk jasny (ciepły)", 2.0f, new Color(.97f, .94f, .88f), new Color(.93f, .9f, .84f), .9f, "white_stucco", "white_plaster_02");
        var dab = BraxUliceKit.Tex("muzeum: boazeria dąb", 1.2f, null, new Color(.55f, .4f, .26f), .5f, "oak_veneer_01", "dark_wood");
        var kamien = BraxUliceKit.Tex("muzeum: kamień (ościeża, kolumny, gzyms)", 1.6f, new Color(.95f, .93f, .9f), new Color(.85f, .82f, .76f), .6f, "sandstone_blocks_08", "marble_01");
        var sufit = BraxUliceKit.Mat("muzeum: sufit tynk", new Color(.95f, .94f, .91f), .9f);
        var mosiadz = BraxCityMaterials.Surface("muzeum: chrom (słupki, ramy gablot)", new Color(.86f, .87f, .88f), .12f, 1f);
        var lina = BraxUliceKit.Mat("muzeum: linka barierki (czerwony aksamit)", new Color(.55f, .05f, .08f), .8f);
        var skora = BraxUliceKit.Tex("muzeum: ławki skóra", .8f, new Color(.35f, .22f, .15f), new Color(.3f, .18f, .12f), .5f, "brown_leather", "leather_red_02");
        var swiatlo = BraxMiastoA.Swieci("muzeum: oprawy sufitowe (ciepłe)", new Color(1f, .95f, .85f), new Color(1.3f, 1.2f, 1.0f));
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        // posadzka (2 cm nad poziomem — nie w płaszczyźnie promenady), sufit, ściany z boazerią i gzymsem
        BraxUliceKit.S(marmur).Box(new Vector3(xi0, y0, zi0), new Vector3(xi1, y0 + .02f, zf - .05f), 16 | 4);
        BraxUliceKit.S(sufit).Quad(new Vector3(xi0, y0 + hS, zi0), new Vector3(xi1, y0 + hS, zi0), new Vector3(xi1, y0 + hS, zi1), new Vector3(xi0, y0 + hS, zi1), Vector3.down);
        void Sciana(Vector3 a, Vector3 c, Vector3 n)   // lico ściany a→c (poziomo), od posadzki do sufitu; boazeria 1.1 m × K (2.5 cm przed licem), gzyms
        {
            float yb = y0 + .02f, yz = y0 + 1.1f * k;
            BraxUliceKit.S(tynk).Quad(new Vector3(a.x, yb, a.z), new Vector3(c.x, yb, c.z), new Vector3(c.x, y0 + hS, c.z), new Vector3(a.x, y0 + hS, a.z), n);
            var lo = Vector3.Min(a, c) + n * 0f; var hi = Vector3.Max(a, c) + n * .025f;
            BraxUliceKit.S(dab).Box(new Vector3(Mathf.Min(lo.x, hi.x), yb, Mathf.Min(lo.z, hi.z)), new Vector3(Mathf.Max(lo.x, hi.x), yz, Mathf.Max(lo.z, hi.z)));
            BraxUliceKit.S(dab).Box(new Vector3(Mathf.Min(lo.x, hi.x + n.x * .02f), yz, Mathf.Min(lo.z, hi.z + n.z * .02f)), new Vector3(Mathf.Max(lo.x, hi.x + n.x * .02f), yz + .05f, Mathf.Max(lo.z, hi.z + n.z * .02f)));   // listwa
            var g0 = Vector3.Min(a, c); var g1 = Vector3.Max(a, c) + n * .22f;
            BraxUliceKit.S(kamien).Box(new Vector3(Mathf.Min(g0.x, g1.x), y0 + hS - .3f, Mathf.Min(g0.z, g1.z)), new Vector3(Mathf.Max(g0.x, g1.x), y0 + hS - .02f, Mathf.Max(g0.z, g1.z)));   // gzyms
        }
        Sciana(new Vector3(xi0, 0, zi0), new Vector3(xi1, 0, zi0), Vector3.forward);
        Sciana(new Vector3(xi0, 0, zi1), new Vector3(xi0, 0, zi0), Vector3.right);
        Sciana(new Vector3(xi1, 0, zi0), new Vector3(xi1, 0, zi1), Vector3.left);
        // ściana od wejścia (północna) z otworem drzwi: dwa odcinki i nadproże
        Sciana(new Vector3(b.x - dw * .5f, 0, zi1), new Vector3(xi0, 0, zi1), Vector3.back);
        Sciana(new Vector3(xi1, 0, zi1), new Vector3(b.x + dw * .5f, 0, zi1), Vector3.back);
        BraxUliceKit.S(tynk).Quad(new Vector3(b.x + dw * .5f, y0 + dh, zi1), new Vector3(b.x - dw * .5f, y0 + dh, zi1), new Vector3(b.x - dw * .5f, y0 + hS, zi1), new Vector3(b.x + dw * .5f, y0 + hS, zi1), Vector3.back);
        // ościeża otworu (kamień) w grubości muru — od lica sali do 12 cm przed lico fasady (opaska 30 cm wokół drzwi)
        // portal i opaska drzwi z jasnego, gładkiego kamienia (odcina się od lica zabytku)
        var portal = BraxUliceKit.Tex("muzeum: portal (kamień jasny gładki)", 1.6f, new Color(.9f, .86f, .78f), new Color(.88f, .84f, .76f), .6f, "white_stucco", "white_plaster_02");
        var os = BraxUliceKit.S(portal);
        // (bez lica od sali i spodu — tam ściana sali i posadzka; zero równoległych lic)
        float hw = dw * .5f;
        os.Box(new Vector3(b.x - hw - .3f, y0, zi1), new Vector3(b.x - hw, y0 + dh, zf + .12f), 16 | 4);
        os.Box(new Vector3(b.x + hw, y0, zi1), new Vector3(b.x + hw + .3f, y0 + dh, zf + .12f), 16 | 4);
        os.Box(new Vector3(b.x - hw - .3f, y0 + dh, zi1), new Vector3(b.x + hw + .3f, y0 + dh + .45f, zf + .12f), 16);
        BraxUliceKit.S(marmur).Box(new Vector3(b.x - hw, y0, zi1), new Vector3(b.x + hw, y0 + .02f, zf + .3f), 4);   // próg
        // 23:09 (właściciel: „drzwi… są w środku, jakoś dziwnie są”): drzwi dębowe płycinowe w LICU fasady — ościeżnica 8 cm za
        // licem, skrzydła równo z murem (wcześniej cofnięte o 35–50 cm w kamiennym „tunelu”); za nimi przedsionek w grubości muru
        BraxDrzwiLokalu.UsunStare("R1_MUZEUM");
        BraxDrzwiLokalu.Podwojne(g, "R1_MUZEUM", "MUZEUM MIASTA BRAXHORN", new Vector3(b.x, y0 + .04f, zf + .07f), Vector3.forward, dw, dh - .02f, .3f, 0f, BraxDrzwiLokalu.Drewniane);
        // PORTAL z kamienia przed licem fasady: pilastry na cokołach z kapitelami, belka (fryz) z literami 3D, gzyms wysunięty;
        // tył brył na licu fasady pominięty (bez wspólnej płaszczyzny z modelem), spód na chodniku pominięty
        foreach (float sx in new[] { -1f, 1f })
        {
            float xa = b.x + sx * (hw + .3f), xb = b.x + sx * (hw + .75f), xc = b.x + sx * (hw + .25f), xd = b.x + sx * (hw + .8f);
            os.Box(new Vector3(Mathf.Min(xa, xb), y0, zf), new Vector3(Mathf.Max(xa, xb), y0 + dh + .25f, zf + .22f), 16 | 4);      // trzon
            os.Box(new Vector3(Mathf.Min(xc, xd), y0, zf), new Vector3(Mathf.Max(xc, xd), y0 + .5f, zf + .3f), 16 | 4);             // cokół
            os.Box(new Vector3(Mathf.Min(xc, xd), y0 + dh + .25f, zf), new Vector3(Mathf.Max(xc, xd), y0 + dh + .45f, zf + .3f), 16); // kapitel
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: pilaster portalu", new Vector3((xc + xd) * .5f, y0 + (dh + .45f) * .5f, zf + .15f), new Vector3(.55f, dh + .45f, .3f));
            // kinkiet na pilastrze (23:09 — zamiast latarni na chodniku)
            BraxMuzeumKod.Kinkiet(g, "portal muzeum", new Vector3(b.x + sx * (hw + .525f), y0 + 2.45f, zf + .22f), Vector3.forward);
        }
        os.Box(new Vector3(b.x - hw - .85f, y0 + dh + .45f, zf), new Vector3(b.x + hw + .85f, y0 + dh + 1.2f, zf + .24f), 16);     // fryz
        os.Poducha(new Vector3(b.x - hw - 1.0f, y0 + dh + 1.2f, zf - .05f), new Vector3(b.x + hw + 1.0f, y0 + dh + 1.42f, zf + .38f), .03f, 0f, 4);   // gzyms
        // litery 3D na fryzie (grafit — bez złota), wysokość dopasowana do szerokości fryzu
        float capP = Mathf.Min(.36f, (dw + 1.2f) / BraxLiteryBebas.Szerokosc("MUZEUM MIASTA BRAXHORN", 1f));
        BraxRegionMeble.Napis(g, "MUZEUM MIASTA BRAXHORN", new Vector3(b.x, y0 + dh + .825f, zf + .24f), Vector3.forward, capP / .72f, new Color(.11f, .11f, .12f));
        BraxRegion.Warm(g, "Muzeum: światło wejścia", new Vector3(b.x, y0 + 2.9f, zf + 1.3f), .9f, 7f);
        // kolumny: dwa rzędy co ~5 m wzdłuż sali (baza, trzon, kapitel)
        var bl = new[] { new Color32(255, 255, 255, 255) };
        float zk0 = zi0 + (zi1 - zi0) * .3f, zk1 = zi0 + (zi1 - zi0) * .7f;
        var kol = BraxUliceKit.S(kamien);
        foreach (float zk in new[] { zk0, zk1 })
            for (float x = xi0 + 5f; x < xi1 - 4f; x += 6f)
            {
                var pr = new List<Vector3> { new Vector3(.42f, y0 + .02f, 0), new Vector3(.42f, y0 + .3f, 0), new Vector3(.36f, y0 + .38f, 0), new Vector3(.3f, y0 + .45f, 0),
                    new Vector3(.27f, y0 + hS - .75f, 0), new Vector3(.33f, y0 + hS - .6f, 0), new Vector3(.42f, y0 + hS - .45f, 0), new Vector3(.42f, y0 + hS - .3f, 0), new Vector3(.36f, y0 + hS - .3f, 0), new Vector3(.36f, y0 + hS, 0) };
                BraxChlodnie.Toczony(kol, new Vector3(x, 0f, zk), pr, bl, 24);
                BraxRegionMeble.Kolizja(g, "Muzeum kolizja: kolumna", new Vector3(x, y0 + hS * .5f, zk), new Vector3(.8f, hS, .8f));
            }
        // oprawy sufitowe (świecą) i światło
        int nSw = 0;
        for (float x = xi0 + 3f; x < xi1 - 2f; x += 6f)
            for (float z = zi0 + 3f; z < zi1 - 2f; z += 6f, nSw++)
            {
                BraxUliceKit.S(swiatlo).Box(new Vector3(x - .5f, y0 + hS - .06f, z - .5f), new Vector3(x + .5f, y0 + hS - .02f, z + .5f), 8);
                if (nSw % 4 == 0) BraxRegion.Warm(g, "Muzeum: światło sali", new Vector3(x, y0 + hS - .6f, z), 1.2f, 14f);   // co czwarta oprawa ze światłem (telefon)
            }
        // recepcja z kasą biletów (przy wejściu, po prawej idącego)
        float rx = b.x + dw * .5f + 2.5f, rz = zi1 - 2.2f;
        BraxUliceKit.S(dab).Poducha(new Vector3(rx - 1.4f, y0 + .02f, rz - .35f), new Vector3(rx + 1.4f, y0 + 1.05f * k, rz + .35f), .012f, 0f, 3);
        BraxUliceKit.S(kamien).Poducha(new Vector3(rx - 1.45f, y0 + 1.05f * k, rz - .4f), new Vector3(rx + 1.45f, y0 + 1.05f * k + .04f, rz + .4f), .01f, 0f, 3);
        BraxRegionMeble.Napis(g, "KASA · BILETY", new Vector3(rx, y0 + .7f * k, rz - .37f), Vector3.back, .16f * k, new Color(.85f, .78f, .6f));   // od zwiedzających (sala); kasjer od strony wejścia
        BraxRegionMeble.Kolizja(g, "Muzeum kolizja: recepcja", new Vector3(rx, y0 + .55f * k, rz), new Vector3(2.9f, 1.1f * k, .8f));
        var kasa = BraxRegion.Model("CashRegister_01"); if (kasa) BraxRegion.Put(g, kasa, "Muzeum: kasa biletowa", new Vector3(rx + .6f, y0 + 1.05f * k + .04f, rz), 0f, .35f * k, false);
        // gabloty na postumentach z eksponatami Poly Haven (popiersie, koń, wazy, zegar, technika) — w osiach między kolumnami
        string[][] eksponaty = { new[] { "marble_bust_01" }, new[] { "horse_statue_01" }, new[] { "antique_ceramic_vase_01", "ceramic_vase_02" }, new[] { "brass_vase_03", "brass_vase_01" },
            new[] { "tea_set_01" }, new[] { "carved_wooden_plate" }, new[] { "television_02" }, new[] { "boombox" }, new[] { "ceramic_vase_04" }, new[] { "round_spectacles", "magnifying_glass_01" } };
        int ie = 0; float zg = (zk0 + zk1) * .5f;
        for (float x = xi0 + 8f; x < xi1 - 7f && ie < eksponaty.Length; x += 6f, ie++)
        {
            var model = BraxRegion.Model(eksponaty[ie]);
            Gablota(g, new Vector3(x, y0 + .02f, zg), marmur, mosiadz, szklo, model, "eksponat " + (ie + 1));
        }
        // zegar stojący i obrazy w ramach na ścianach (Poly Haven), tabliczki
        var zegar = BraxRegion.Model("vintage_grandfather_clock_01"); if (zegar) { BraxRegion.Put(g, zegar, "Muzeum: zegar stojący (eksponat)", new Vector3(xi0 + .6f, y0 + .02f, zi0 + 1.2f), 90f, 1f, true, 2.1f * k); muzEksponatow++; }
        string[] ramy = { "hanging_picture_frame_01", "hanging_picture_frame_02", "hanging_picture_frame_03" };
        int ir = 0;
        for (float x = xi0 + 4f; x < xi1 - 3f; x += 4.5f, ir++)
        {
            var rm = BraxRegion.Model(ramy[ir % ramy.Length]);
            if (rm && BraxRegion.Put(g, rm, "Muzeum: obraz w ramie", new Vector3(x, y0 + 1.45f * k, zi0 + .06f), 0f, 1f, false, 1.2f * k)) muzEksponatow++;   // przodem do sali (+z)
        }
        // ławki muzealne (skóra na chromowanych nogach) i barierki z linką wzdłuż ściany z obrazami
        var sk = BraxUliceKit.S(skora); var ch = BraxUliceKit.S(mosiadz);
        for (float x = xi0 + 7f; x < xi1 - 6f; x += 12f)
        {
            float zb = zi0 + 3.2f;
            sk.Poducha(new Vector3(x - .9f, y0 + .42f * k, zb - .25f), new Vector3(x + .9f, y0 + .5f * k, zb + .25f), .03f, .01f, 6);
            foreach (float dx in new[] { -.8f, .8f }) ch.Box(new Vector3(x + dx - .03f, y0 + .02f, zb - .2f), new Vector3(x + dx + .03f, y0 + .42f * k, zb + .2f));
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: ławka", new Vector3(x, y0 + .25f * k, zb), new Vector3(1.8f, .5f * k, .5f));
            BraxMiastoA.Siedzisko(g, "Muzeum: ławka", new Vector3(x, y0 + .5f * k, zb), Vector3.forward);
        }
        float zs = zi0 + 1.4f;
        for (float x = xi0 + 2f; x < xi1 - 1.5f; x += 2.2f)
        {
            ch.Rura(new Vector3(x, y0 + .02f, zs), new Vector3(x, y0 + .95f * k, zs), .025f, 10);
            BraxChlodnie.Toczony(ch, new Vector3(x, y0 + .02f, zs), new List<Vector3> { new Vector3(.16f, 0f, 0), new Vector3(.16f, .02f, 0), new Vector3(.03f, .035f, 0), new Vector3(0f, .035f, 0) }, bl, 16);
            if (x + 2.2f < xi1 - 1.5f) BraxUliceKit.S(lina).Rura(new Vector3(x, y0 + .88f * k, zs), new Vector3(x + 2.2f, y0 + .88f * k, zs), .015f, 8);
        }
        // eksponat centralny: kotwica portowa (miasto portowe — ul. Portowa) na kamiennym cokole, z kodu: trzon, poprzeczka, ramiona
        // z łapami, pierścień; tablice działów na ścianach (litery 3D): PORT · SZTUKA · TECHNIKA
        {
            float cx = (xi0 + xi1) * .5f, cz = (zk0 + zk1) * .5f + (zk1 - zk0) * .25f;
            var stalK = BraxCityMaterials.Surface("muzeum: kotwica (stal kuta, ciemna)", new Color(.16f, .15f, .14f), .55f, .8f);
            BraxUliceKit.S(kamien).Poducha(new Vector3(cx - 1.1f, y0 + .02f, cz - .7f), new Vector3(cx + 1.1f, y0 + .5f, cz + .7f), .03f, 0f, 3);
            var kt = BraxUliceKit.S(stalK); float yb = y0 + .5f;
            kt.Rura(new Vector3(cx, yb + .35f, cz), new Vector3(cx, yb + 2.6f, cz), .07f, 12);                                 // trzon
            kt.Rura(new Vector3(cx - .75f, yb + 2.25f, cz), new Vector3(cx + .75f, yb + 2.25f, cz), .045f, 10);                // poprzeczka
            BraxChlodnie.Toczony(kt, new Vector3(cx, yb + 2.6f, cz), new List<Vector3> { new Vector3(.2f, .2f, 0), new Vector3(.2f, .05f, 0), new Vector3(.12f, 0f, 0), new Vector3(.12f, .25f, 0), new Vector3(.2f, .2f, 0) }, bl, 16);   // pierścień (gruba obręcz)
            for (int i = 0; i < 8; i++)
            {   // ramiona — łuk z segmentów od dołu trzonu w obie strony, zakończone łapami
                float a0 = i * Mathf.PI / 8f, a1 = (i + 1) * Mathf.PI / 8f; float R = .95f;
                Vector3 P(float a) => new Vector3(cx - Mathf.Cos(a) * R, yb + .35f + R - Mathf.Sin(a) * R, cz);
                kt.Rura(P(a0), P(a1), .06f, 10);
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                var t = new Vector3(cx + sx * .95f, yb + 1.3f, cz);
                kt.Poducha(new Vector3(t.x - .16f, t.y - .05f, cz - .05f), new Vector3(t.x + .16f, t.y + .22f, cz + .05f), .02f, 0f, 3);   // łapy
            }
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: kotwica", new Vector3(cx, y0 + 1.5f, cz), new Vector3(2.2f, 3f, 1.4f));
            BraxRegionMeble.Napis(g, "KOTWICA PORTOWA", new Vector3(cx, y0 + .3f, cz + .72f), Vector3.forward, .09f * k, new Color(.85f, .78f, .6f));
            foreach (var (tekst, p, n) in new[] { ("PORT", new Vector3(xi0 + .06f, y0 + 3.72f, (zi0 + zi1) * .5f), Vector3.right), ("SZTUKA", new Vector3(cx, y0 + 3.72f, zi0 + .06f), Vector3.forward), ("TECHNIKA", new Vector3(xi1 - .06f, y0 + 3.72f, (zi0 + zi1) * .5f), Vector3.left) })
                BraxRegionMeble.Napis(g, tekst, p, n, .38f * k, new Color(.55f, .42f, .25f));
        }
        BraxUliceKit.Zapisz(g, "muzeum");
    }

    // gablota: postument z marmuru (0.9 m × K), szklany klosz 0.9 × 0.6 × 0.6 m z chromowaną ramą, eksponat w środku, tabliczka
    static void Gablota(Transform g, Vector3 p, Material marmur, Material chrom, Material szklo, GameObject model, string nazwa)
    {
        float k = K, hp = .9f * k, a = .45f, c = .3f, hg = .6f * k;
        BraxUliceKit.S(marmur).Poducha(new Vector3(p.x - a, p.y, p.z - c), new Vector3(p.x + a, p.y + hp, p.z + c), .02f, 0f, 3);
        var s = BraxUliceKit.S(szklo); float y1 = p.y + hp, y2 = y1 + hg;
        foreach (var (n, q0, q1, q2, q3) in new[] {
            (Vector3.forward, new Vector3(p.x - a + .02f, y1, p.z + c - .02f), new Vector3(p.x + a - .02f, y1, p.z + c - .02f), new Vector3(p.x + a - .02f, y2, p.z + c - .02f), new Vector3(p.x - a + .02f, y2, p.z + c - .02f)),
            (Vector3.back, new Vector3(p.x + a - .02f, y1, p.z - c + .02f), new Vector3(p.x - a + .02f, y1, p.z - c + .02f), new Vector3(p.x - a + .02f, y2, p.z - c + .02f), new Vector3(p.x + a - .02f, y2, p.z - c + .02f)),
            (Vector3.right, new Vector3(p.x + a - .02f, y1, p.z + c - .02f), new Vector3(p.x + a - .02f, y1, p.z - c + .02f), new Vector3(p.x + a - .02f, y2, p.z - c + .02f), new Vector3(p.x + a - .02f, y2, p.z + c - .02f)),
            (Vector3.left, new Vector3(p.x - a + .02f, y1, p.z - c + .02f), new Vector3(p.x - a + .02f, y1, p.z + c - .02f), new Vector3(p.x - a + .02f, y2, p.z + c - .02f), new Vector3(p.x - a + .02f, y2, p.z - c + .02f)),
            (Vector3.up, new Vector3(p.x - a + .02f, y2, p.z - c + .02f), new Vector3(p.x + a - .02f, y2, p.z - c + .02f), new Vector3(p.x + a - .02f, y2, p.z + c - .02f), new Vector3(p.x - a + .02f, y2, p.z + c - .02f)) })
        { s.Quad(q0, q1, q2, q3, n); s.Quad(q3, q2, q1, q0, -n); }
        var r = BraxUliceKit.S(chrom);
        foreach (float x in new[] { p.x - a, p.x + a - .02f }) foreach (float z in new[] { p.z - c, p.z + c - .02f }) r.Box(new Vector3(x, y1, z), new Vector3(x + .02f, y2 + .02f, z + .02f));
        foreach (float z in new[] { p.z - c, p.z + c - .02f }) r.Box(new Vector3(p.x - a + .02f, y2, z), new Vector3(p.x + a - .02f, y2 + .02f, z + .02f));
        foreach (float x in new[] { p.x - a, p.x + a - .02f }) r.Box(new Vector3(x, y2, p.z - c + .02f), new Vector3(x + .02f, y2 + .02f, p.z + c - .02f));
        BraxRegionMeble.Kolizja(g, "Muzeum kolizja: gablota", new Vector3(p.x, p.y + (hp + hg) * .5f, p.z), new Vector3(2f * a, hp + hg, 2f * c));
        if (model && BraxRegion.Put(g, model, "Muzeum: " + nazwa + " (" + model.name + ")", new Vector3(p.x, y1 + .004f, p.z), 180f, 1f, false, hg - .08f)) muzEksponatow++;
        // tabliczka opisowa na postumencie (od zwiedzającego, +z)
        BraxRegionMeble.Napis(g, model ? model.name.Replace('_', ' ').ToUpperInvariant() : "EKSPONAT", new Vector3(p.x, p.y + hp * .78f, p.z + c + .01f), Vector3.forward, .05f * k, new Color(.85f, .78f, .6f));
    }
}

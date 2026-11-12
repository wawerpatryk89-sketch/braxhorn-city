using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// MUZEUM BRAXHORN w zabytku KINNAIRD (11.11.2026; nazwa — właściciel 12.11 00:04: „Muzeum Braxhorn, nie miasta”; właściciel 20:06: „chciałem, żebyś ten zabytkowy budynek zrobił
// funkcjonalny. Że tam można było wejść, jakieś takie muzeum… A z tego też jest sporo takich rzeczy różnych, ta Poly Haven pod to”;
// 21:52: „Wszystko ma być dopieszczone”). Wejście od promenady (lico północne zabytku, z = PromenadaZ), w osi budynku:
//  • WEJŚCIE: lico fasady wyszukane z wierzchołków modelu (parter, oś ±2.5 m), w modelu wycięty otwór 3.1 × 3.8 m (trójkąty
//    w bryle otworu usuwane z kopii siatek), ościeża z kamienia w grubości muru, drzwi dwuskrzydłowe dębowe płycinowe z klamkami
//    (BraxDrzwiLokalu.Drewniane, otwierane) W LICU FASADY (23:09 — wcześniej cofnięte), schody nie są potrzebne (posadzka =
//    poziom promenady). PORTAL z kamienia: pilastry na cokołach z kapitelami, fryz z grafitowym pasem i literami 3D MUZEUM BRAXHORN
//    (Bebas, stal szczotkowana, 8 cm grubości — 12.11 00:05 „napis zawsze w 3D”), linia LED pod literami, gzyms; na pilastrach
//    kinkiety (BraxMuzeumKod.Kinkiet) zamiast latarni na chodniku.
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
        var g = new GameObject("MUZEUM BRAXHORN (zabytek KINNAIRD)").transform; g.SetParent(kin.transform.parent, false);
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
        BraxUliceKit.Punkt("muzeum_wejscie", "MUZEUM BRAXHORN — wejście od promenady (zabytek KINNAIRD)", new Vector3(b.x, y0, zf + 1.5f));
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
        BraxDrzwiLokalu.Podwojne(g, "R1_MUZEUM", "MUZEUM BRAXHORN", new Vector3(b.x, y0 + .04f, zf + .07f), Vector3.forward, dw, dh - .02f, .3f, 0f, BraxDrzwiLokalu.Drewniane);
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
        // szyld (12.11 00:04–00:05: „Muzeum Braxhorn, nie miasta… napis zawsze w 3D, nie napis zwykły w ścianie”): grafitowy pas
        // wysunięty 6 cm z fryzu, na nim litery przestrzenne ze stali szczotkowanej 8 cm grubości (2 mm od pasa), pod literami
        // linia LED (ciepła biel) — bez złota; wysokość liter dopasowana do szerokości fryzu
        float capP = Mathf.Min(.36f, (dw + 1.2f) / BraxLiteryBebas.Szerokosc("MUZEUM BRAXHORN", 1f)), szP = BraxLiteryBebas.Szerokosc("MUZEUM BRAXHORN", capP), yN = y0 + dh + .825f;
        var pasSzyldu = BraxCityMaterials.Surface("muzeum: pas szyldu (grafit)", new Color(.1f, .1f, .11f), .45f, .3f);
        BraxUliceKit.S(pasSzyldu).Poducha(new Vector3(b.x - szP * .5f - .28f, yN - capP * .5f - .14f, zf + .2f), new Vector3(b.x + szP * .5f + .28f, yN + capP * .5f + .12f, zf + .3f), .012f, 0f, 3);
        BraxRegionMeble.Napis(g, "MUZEUM BRAXHORN", new Vector3(b.x, yN + .02f, zf + .3f), Vector3.forward, capP / .72f, new Color(.8f, .81f, .83f), .08f);
        var led = BraxMiastoA.Swieci("muzeum: linia LED pod literami (ciepła biel)", new Color(1f, .95f, .86f), new Color(1.6f, 1.45f, 1.2f));
        float yL = yN + .02f - capP * .5f - .07f;
        BraxUliceKit.S(led).Box(new Vector3(b.x - szP * .5f - .08f, yL, zf + .3f), new Vector3(b.x + szP * .5f + .08f, yL + .014f, zf + .322f), 16);   // lico 2.2 cm przed pasem
        BraxRegion.Warm(g, "Muzeum: światło wejścia", new Vector3(b.x, y0 + 2.9f, zf + 1.3f), .9f, 7f);
        // kolumny: dwa rzędy, symetrycznie względem osi wejścia co 6 m (baza, trzon, kapitel)
        var bl = new[] { new Color32(255, 255, 255, 255) };
        float cx = b.x, W = xi1 - xi0, D = zi1 - zi0, zk0 = zi0 + D * .3f, zk1 = zi0 + D * .7f, zg = (zk0 + zk1) * .5f;
        var kol = BraxUliceKit.S(kamien);
        foreach (float zk in new[] { zk0, zk1 })
            for (float dx = 3f; cx + dx < xi1 - 4f; dx += 6f)
                foreach (float x in new[] { cx - dx, cx + dx })
                {
                    var pr = new List<Vector3> { new Vector3(.42f, y0 + .02f, 0), new Vector3(.42f, y0 + .3f, 0), new Vector3(.36f, y0 + .38f, 0), new Vector3(.3f, y0 + .45f, 0),
                        new Vector3(.27f, y0 + hS - .75f, 0), new Vector3(.33f, y0 + hS - .6f, 0), new Vector3(.42f, y0 + hS - .45f, 0), new Vector3(.42f, y0 + hS - .3f, 0), new Vector3(.36f, y0 + hS - .3f, 0), new Vector3(.36f, y0 + hS, 0) };
                    BraxChlodnie.Toczony(kol, new Vector3(x, 0f, zk), pr, bl, 24);
                    BraxRegionMeble.Kolizja(g, "Muzeum kolizja: kolumna", new Vector3(x, y0 + hS * .5f, zk), new Vector3(.8f, hS, .8f));
                }
        // oprawy sufitowe (świecą) w osiach między kolumnami i światło
        int nSw = 0;
        for (float x = cx - 6f * Mathf.Floor((cx - xi0 - 2f) / 6f); x < xi1 - 2f; x += 6f)
            for (float z = zi0 + 3f; z < zi1 - 2f; z += 6f, nSw++)
            {
                BraxUliceKit.S(swiatlo).Box(new Vector3(x - .5f, y0 + hS - .06f, z - .5f), new Vector3(x + .5f, y0 + hS - .02f, z + .5f), 8);
                if (nSw % 4 == 0) BraxRegion.Warm(g, "Muzeum: światło sali", new Vector3(x, y0 + hS - .6f, z), 1.2f, 14f);   // co czwarta oprawa ze światłem (telefon)
            }
        // recepcja z kasą biletów (przy wejściu, po prawej idącego)
        float rx = b.x + hw + 2.5f, rz = zi1 - 2.2f;
        BraxUliceKit.S(dab).Poducha(new Vector3(rx - 1.4f, y0 + .02f, rz - .35f), new Vector3(rx + 1.4f, y0 + 1.05f * k, rz + .35f), .012f, 0f, 3);
        BraxUliceKit.S(kamien).Poducha(new Vector3(rx - 1.45f, y0 + 1.05f * k, rz - .4f), new Vector3(rx + 1.45f, y0 + 1.05f * k + .04f, rz + .4f), .01f, 0f, 3);
        BraxRegionMeble.Napis(g, "KASA · BILETY", new Vector3(rx, y0 + .7f * k, rz - .37f), Vector3.back, .16f * k, new Color(.86f, .87f, .89f));   // od zwiedzających (sala); kasjer od strony wejścia
        BraxRegionMeble.Kolizja(g, "Muzeum kolizja: recepcja", new Vector3(rx, y0 + .55f * k, rz), new Vector3(2.9f, 1.1f * k, .8f));
        var kasa = BraxRegion.Model("CashRegister_01"); if (kasa) BraxRegion.Put(g, kasa, "Muzeum: kasa biletowa", new Vector3(rx + .6f, y0 + 1.05f * k + .04f, rz), 0f, .35f * k, false);
        // hol: plan wystawy (wolnostojąca plansza) po lewej idącego
        BraxMuzeumKod.Plansza(g, "hol", new Vector3(cx - hw - 2.8f, y0 + .02f, zi1 - 3.2f), Vector3.forward, 5, true);

        // ---- pomocnicze: słupki z linką, ławki, pulpity z opisem, obrazy na ścianie (nad boazerią)
        var sk = BraxUliceKit.S(skora); var ch = BraxUliceKit.S(mosiadz);
        var pulpitM = BraxCityMaterials.Surface("muzeum: pulpit opisu (grafit)", new Color(.09f, .09f, .1f), .4f, .3f);
        var grafit = new Color(.13f, .13f, .14f);
        void Slupek(float x, float z)
        {
            ch.Rura(new Vector3(x, y0 + .02f, z), new Vector3(x, y0 + .95f * k, z), .025f, 10);
            BraxChlodnie.Toczony(ch, new Vector3(x, y0 + .02f, z), new List<Vector3> { new Vector3(.16f, 0f, 0), new Vector3(.16f, .02f, 0), new Vector3(.03f, .035f, 0), new Vector3(0f, .035f, 0) }, bl, 16);
            BraxChlodnie.Toczony(ch, new Vector3(x, y0 + .95f * k, z), new List<Vector3> { new Vector3(0f, -.02f, 0), new Vector3(.035f, 0f, 0), new Vector3(.03f, .03f, 0), new Vector3(0f, .045f, 0) }, bl, 16);
        }
        void Barierka(params Vector3[] pkt)   // słupki co ≤ 2.2 m wzdłuż łamanej, linka między nimi
        {
            for (int i = 0; i + 1 < pkt.Length; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(pkt[i], pkt[i + 1]) / 2.2f));
                for (int j = 0; j < n; j++)
                {
                    var p0 = Vector3.Lerp(pkt[i], pkt[i + 1], (float)j / n); var p1 = Vector3.Lerp(pkt[i], pkt[i + 1], (float)(j + 1) / n);
                    Slupek(p0.x, p0.z);
                    BraxUliceKit.S(lina).Rura(new Vector3(p0.x, y0 + .88f * k, p0.z), new Vector3(p1.x, y0 + .88f * k, p1.z), .015f, 8);
                }
            }
            var ost = pkt[pkt.Length - 1]; if ((pkt[0] - ost).sqrMagnitude > .01f) Slupek(ost.x, ost.z);
        }
        void Lawka(float x, float zb, Vector3 patrzy)
        {
            sk.Poducha(new Vector3(x - .9f, y0 + .42f * k, zb - .25f), new Vector3(x + .9f, y0 + .5f * k, zb + .25f), .03f, .01f, 6);
            foreach (float dx in new[] { -.8f, .8f }) ch.Box(new Vector3(x + dx - .03f, y0 + .02f, zb - .2f), new Vector3(x + dx + .03f, y0 + .42f * k, zb + .2f));
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: ławka", new Vector3(x, y0 + .25f * k, zb), new Vector3(1.8f, .5f * k, .5f));
            BraxMiastoA.Siedzisko(g, "Muzeum: ławka", new Vector3(x, y0 + .5f * k, zb), patrzy);
        }
        void Pulpit(float x, float z, Vector3 n, string tekst)
        {   // słupek ze stali i pochylona płyta z opisem (litery 3D)
            n.y = 0f; n.Normalize(); var r = Vector3.Cross(Vector3.up, n).normalized; var pn = (n + Vector3.up * 1.2f).normalized; var gora = Vector3.Cross(pn, r).normalized;
            float yP = y0 + .95f * k;
            ch.Rura(new Vector3(x, y0 + .02f, z), new Vector3(x, yP - .03f, z), .022f, 10);
            BraxChlodnie.Toczony(ch, new Vector3(x, y0 + .02f, z), new List<Vector3> { new Vector3(.14f, 0f, 0), new Vector3(.14f, .015f, 0), new Vector3(.03f, .03f, 0), new Vector3(0f, .03f, 0) }, bl, 16);
            BraxUliceKit.S(pulpitM).OBox(new Vector3(x, yP, z), r, gora, pn, .44f, .28f, .02f);
            BraxRegionMeble.Napis(g, tekst, new Vector3(x, yP, z) + pn * .0101f, pn, .05f * k, new Color(.86f, .87f, .89f));
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: pulpit", new Vector3(x, y0 + .5f * k, z), new Vector3(.3f, 1f * k, .3f));
        }
        float SzerObrazu(int nr, float skala) { float a = BraxMuzeumKod.Aspekt(nr); return (a > 1.1f ? 1.4f : a < .9f ? .95f : 1.1f) * skala; }
        void ObrazNaScianie(Vector3 lico, Vector3 n, int nr)
        {   // dolna krawędź ramy 1.55 m nad posadzką (nad listwą boazerii)
            float sz = SzerObrazu(nr, 1f), h = BraxMuzeumKod.WysokoscObrazu(nr, sz);
            BraxMuzeumKod.Obraz(g, "ściana", new Vector3(lico.x, y0 + 1.55f + h * .5f, lico.z), n, nr, sz); muzEksponatow++;
        }

        // 12.11 01:42 — eksponaty właściciela z Tripo (plansza z promptu R1, paczka globalna braxhorn-sklep-muzeum-01): fregata,
        // hełm nurka, koło sterowe, soczewka Fresnela, galion, marmurowy bokser, gramofon, globus, pas mistrzowski BRAXHORN;
        // bez paczki (brak klucza w CI) — eksponaty z kodu jak dotąd (bicykl z planszy odrzucony: porwane szprychy)
        GameObject TR(string plik) => BraxMiastoAssety.Model("muzeum_eksponaty_01", plik);
        // ---- działy: PORT (zachód) · SZTUKA (środek, oś wejścia) · TECHNIKA (wschód)
        float sw = Mathf.Min(12f, W * .23f), xP = (xi0 + cx - sw) * .5f, xT = (xi1 + cx + sw) * .5f, dz = Mathf.Min(4.5f, (zk1 - zk0) * .3f);
        // PORT: model żaglowca w gablocie, armata za barierką, hełm nurka w gablocie, koło sterowe na ścianie, plansze, marynistyka
        var fregata = TR("fregata_model");
        if (fregata)
        {   // model fregaty właściciela w dużej gablocie, burtą do zwiedzających (kadłub modelu wzdłuż z → obrót 90°)
            BraxMuzeumKod.GablotaDuza(g, "fregata", new Vector3(xP, y0 + .02f, zg), Vector3.forward, out var blatF, out float hF);
            if (BraxRegion.Put(g, fregata, "Muzeum: model fregaty (Tripo właściciela)", blatF, 90f, 1f, false, (hF - .05f) / k)) muzEksponatow++;
            Pulpit(xP + 1.75f, zg + 1.0f, Vector3.forward, "MODEL FREGATY");
        }
        else { BraxMuzeumKod.Zaglowiec(g, "dział PORT", new Vector3(xP, y0 + .02f, zg), Vector3.forward); muzEksponatow++; Pulpit(xP + 1.75f, zg + 1.0f, Vector3.forward, "MODEL ŻAGLOWCA"); }
        BraxMuzeumKod.Armata(g, "dział PORT", new Vector3(xP, y0 + .02f, zg - dz), Vector3.right); muzEksponatow++;
        { float zA = zg - dz; Barierka(new Vector3(xP - 1.85f, 0f, zA - 1.85f), new Vector3(xP + 2.15f, 0f, zA - 1.85f), new Vector3(xP + 2.15f, 0f, zA + 1.45f), new Vector3(xP - 1.85f, 0f, zA + 1.45f), new Vector3(xP - 1.85f, 0f, zA - 1.85f)); }
        Pulpit(xP + 2.75f, zg - dz + 1.0f, Vector3.right, "ARMATA OKRĘTOWA");
        var helm = TR("helm_nurka");
        Gablota(g, new Vector3(xP, y0 + .02f, zg + dz), marmur, mosiadz, szklo, helm, "hełm nurka", "HEŁM NURKA", helm ? null : (System.Action<Vector3>)(q => BraxMuzeumKod.HelmNurka(g, "dział PORT", q, Vector3.forward)), .5f, .5f, .66f, 0f);
        var kolo = TR("kolo_sterowe");
        if (kolo) { if (BraxRegion.Put(g, kolo, "Muzeum: koło sterowe na stojaku (Tripo właściciela)", new Vector3(xi0 + .75f, y0 + .02f, zg), 90f, 1f, true, 1.3f)) muzEksponatow++; }
        else { BraxMuzeumKod.KoloSterowe(g, "dział PORT", new Vector3(xi0, y0 + 1.95f, zg), Vector3.right); muzEksponatow++; }
        Pulpit(xi0 + 1.4f, zg + 1.3f, Vector3.right, "KOŁO STEROWE");
        BraxMuzeumKod.Plansza(g, "dział PORT", new Vector3(xi0, y0 + 2.2f, zg - 3.2f), Vector3.right, 0);
        BraxMuzeumKod.Plansza(g, "dział PORT", new Vector3(xi0, y0 + 2.2f, zg + 3.2f), Vector3.right, 1);
        int[] zachod = { 0, 6, 8, 14 }, wschod = { 7, 12, 2, 9 }; float[] zSciany = { zg - 11f, zg - 6.8f, zg + 6.8f, zg + 11f };
        for (int i = 0; i < 4; i++)
            if (zSciany[i] > zi0 + 1.6f && zSciany[i] < zi1 - 1.6f) { ObrazNaScianie(new Vector3(xi0, 0f, zSciany[i]), Vector3.right, zachod[i]); ObrazNaScianie(new Vector3(xi1, 0f, zSciany[i]), Vector3.left, wschod[i]); }
        // TECHNIKA: gramofon z tubą, bicykl za barierką, globus, gabloty z techniką (Poly Haven), zegar stojący, plansze
        var gramofon = TR("gramofon");
        if (gramofon) NaPostumencie(gramofon, xT, zg + dz, -90f, .8f, .42f, .62f, "GRAMOFON Z TUBĄ", Vector3.left, xT - 1.2f, zg + dz + .9f);
        else { BraxMuzeumKod.Gramofon(g, "dział TECHNIKA", new Vector3(xT, y0 + .02f, zg + dz), Vector3.left); muzEksponatow++; Pulpit(xT - 1.2f, zg + dz + .9f, Vector3.left, "GRAMOFON Z TUBĄ"); }
        BraxMuzeumKod.Bicykl(g, "dział TECHNIKA", new Vector3(xT, y0 + .02f, zg), Vector3.left); muzEksponatow++;
        Barierka(new Vector3(xT - .95f, 0f, zg - 1.9f), new Vector3(xT + .95f, 0f, zg - 1.9f), new Vector3(xT + .95f, 0f, zg + 2.1f), new Vector3(xT - .95f, 0f, zg + 2.1f), new Vector3(xT - .95f, 0f, zg - 1.9f));
        Pulpit(xT - 1.55f, zg - 1.3f, Vector3.left, "BICYKL");
        var globus = TR("globus");
        if (globus) { if (BraxRegion.Put(g, globus, "Muzeum: globus na stojaku (Tripo właściciela)", new Vector3(xT, y0 + .02f, zg - dz), -90f, 1f, true, 1.2f)) muzEksponatow++; }
        else { BraxMuzeumKod.Globus(g, "dział TECHNIKA", new Vector3(xT, y0 + .02f, zg - dz), Vector3.left); muzEksponatow++; }
        Pulpit(xT - 1.2f, zg - dz + .9f, Vector3.left, "GLOBUS");
        float xG = Mathf.Min(xT + 3.6f, xi1 - 2.2f);
        string[][] technika = { new[] { "television_02" }, new[] { "boombox" }, new[] { "round_spectacles", "magnifying_glass_01" } };
        for (int i = 0; i < technika.Length; i++) Gablota(g, new Vector3(xG, y0 + .02f, zg + (i - 1) * dz), marmur, mosiadz, szklo, BraxRegion.Model(technika[i]), "technika " + (i + 1));
        BraxMuzeumKod.Plansza(g, "dział TECHNIKA", new Vector3(xi1, y0 + 2.2f, zg - 3.2f), Vector3.left, 3);
        BraxMuzeumKod.Plansza(g, "dział TECHNIKA", new Vector3(xi1, y0 + 2.2f, zg + 3.2f), Vector3.left, 4);
        var zegar = BraxRegion.Model("vintage_grandfather_clock_01"); if (zegar) { BraxRegion.Put(g, zegar, "Muzeum: zegar stojący (eksponat)", new Vector3(xi1 - .6f, y0 + .02f, zi0 + 1.2f), 270f, 1f, true, 2.1f * k); muzEksponatow++; }
        // SZTUKA: gabloty z rzeźbą i ceramiką (Poly Haven) przed kotwicą, ścianki ekspozycyjne z obrazami, ławki
        string[][] sztuka = { new[] { "marble_bust_01" }, new[] { "horse_statue_01" }, new[] { "antique_ceramic_vase_01", "ceramic_vase_02" }, new[] { "tea_set_01" }, new[] { "carved_wooden_plate" }, new[] { "ceramic_vase_04", "ceramic_vase_03" } };
        float[] xSz = { -2.5f, 2.5f, -5.5f, 5.5f, -8.5f, 8.5f };
        for (int i = 0; i < sztuka.Length; i++) if (Mathf.Abs(xSz[i]) < sw - 1.5f) Gablota(g, new Vector3(cx + xSz[i], y0 + .02f, zg - 1.8f), marmur, mosiadz, szklo, BraxRegion.Model(sztuka[i]), "sztuka " + (i + 1));
        var tynkS = BraxUliceKit.Tex("muzeum: ścianki ekspozycyjne (bordo)", 2.0f, new Color(.46f, .12f, .14f), new Color(.4f, .09f, .11f), .85f, "painted_plaster_wall", "white_stucco");
        float zS = zi0 + (zk0 - zi0) * .55f;
        int[][] scianki = { new[] { 3, 10 }, new[] { 4, 11 } };
        for (int i = 0; i < 2; i++)
        {
            float xs = cx + (i == 0 ? -5.5f : 5.5f);
            BraxUliceKit.S(dab).Box(new Vector3(xs - 2.48f, y0 + .02f, zS - .13f), new Vector3(xs + 2.48f, y0 + .14f, zS + .13f), 4);           // cokół
            BraxUliceKit.S(tynkS).Box(new Vector3(xs - 2.5f, y0 + .14f, zS - .15f), new Vector3(xs + 2.5f, y0 + 3.1f, zS + .15f));              // ścianka
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: ścianka ekspozycyjna", new Vector3(xs, y0 + 1.6f, zS), new Vector3(5f, 3.1f, .3f));
            for (int s2 = 0; s2 < 2; s2++)
            {
                int nr = scianki[i][s2]; float szO = SzerObrazu(nr, 1.15f), h = BraxMuzeumKod.WysokoscObrazu(nr, szO); var n = s2 == 0 ? Vector3.forward : Vector3.back;
                BraxMuzeumKod.Obraz(g, "ścianka", new Vector3(xs, y0 + 1.75f, zS) + n * .15f, n, nr, szO); muzEksponatow++;
            }
            Lawka(xs, zi0 + 3.4f, Vector3.back); Lawka(xs, zS + 3.0f, Vector3.back);
        }
        Lawka(xP, zi0 + 3.4f, Vector3.back); Lawka(xT, zi0 + 3.4f, Vector3.back);
        // ---- 12.11 00:31 (właściciel: „wyposażenie musi zrobić większe, bo tam nic nie ma”): cała sala zapełniona
        // HOL: szatnia i sklep muzealny pod ścianą wejściową, dzwon okrętowy, luneta, dwie rzeźby, rośliny
        BraxMuzeumKod.Szatnia(g, "hol", new Vector3(xi0 + 3.2f, y0 + .02f, zi1), Vector3.back);
        BraxMuzeumKod.Sklepik(g, "hol", new Vector3(xi1 - 3.2f, y0 + .02f, zi1), Vector3.back);
        BraxMuzeumKod.Dzwon(g, "hol", new Vector3(cx - 12f, y0 + .02f, zk1 + 4.5f), Vector3.forward); muzEksponatow++;
        Pulpit(cx - 10.7f, zk1 + 5.4f, Vector3.forward, "DZWON OKRĘTOWY");
        BraxMuzeumKod.Luneta(g, "hol", new Vector3(cx + 12f, y0 + .02f, zk1 + 4.5f), Vector3.left); muzEksponatow++;
        Pulpit(cx + 13.3f, zk1 + 5.4f, Vector3.forward, "LUNETA");
        string[] rzezby = { "RZEŹBA · PIERŚCIEŃ", "RZEŹBA · SPIRALA", "RZEŹBA · TRZY KULE", "RZEŹBA · ŁUK" };
        bool gotycka = BraxRegion.Model("gothic_statue");
        foreach (var (x, z, w) in new[] { (cx - 6f, zk1 + 2.6f, 1), (cx + 6f, zk1 + 2.6f, 3), (cx - 9.5f, zg + 2.8f, 2), (cx + 9.5f, zg + 2.8f, 0), (cx, zS, 1) })
        {
            if (Mathf.Abs(x - cx) > sw - 1f || (z == zS && gotycka)) continue;   // między ściankami rzeźba gotycka (Poly Haven), gdy jest
            if ((w == 1 || w == 2) && z != zS && BraxMiastoAssety.Jest("muzeum_eksponaty_01")) continue;   // spirala → marmurowy bokser, trzy kule → galion (Tripo)
            BraxMuzeumKod.Rzezba(g, "dział SZTUKA", new Vector3(x, y0 + .02f, z), Vector3.forward, w); muzEksponatow++;
            if (z != zS) Pulpit(x + 1.05f, z + .8f, Vector3.forward, rzezby[w]);
        }
        foreach (var (x, z, m) in new[] { (cx - hw - 1.2f, zi1 - .7f, "potted_plant_01"), (cx + hw + 1.2f, zi1 - .7f, "potted_plant_01"), (xi0 + 6.6f, zi1 - .7f, "potted_plant_04"), (xi1 - 6.9f, zi1 - .7f, "potted_plant_04") })
        {   // rośliny w donicach (Poly Haven) w holu
            var r = BraxRegion.Model(m); if (r) BraxRegion.Put(g, r, "Muzeum: roślina w holu", new Vector3(x, y0 + .02f, z), 0f, 1f, true, 1.6f * k);
        }
        // PORT: dwa kolejne modele statków (bryg, kuter), amfory, ładunek portowy, łódź wiosłowa za barierką
        BraxMuzeumKod.Zaglowiec(g, "dział PORT · bryg", new Vector3(xP + 4.8f, y0 + .02f, zg + 2.6f), Vector3.forward, 1); muzEksponatow++;
        Pulpit(xP + 6.55f, zg + 3.6f, Vector3.forward, "MODEL BRYGU");
        BraxMuzeumKod.Zaglowiec(g, "dział PORT · kuter", new Vector3(xP + 4.8f, y0 + .02f, zg - 2.6f), Vector3.forward, 2); muzEksponatow++;
        Pulpit(xP + 6.55f, zg - 1.6f, Vector3.forward, "MODEL KUTRA");
        Gablota(g, new Vector3(xP - 4.3f, y0 + .02f, zg - 3f), marmur, mosiadz, szklo, null, "amfory", "AMFORY", q => BraxMuzeumKod.Amfory(g, "dział PORT", q, Vector3.forward));
        BraxMuzeumKod.Ladunek(g, "dział PORT", new Vector3(xP - 4.0f, y0 + .02f, zg + 3f), Vector3.forward); muzEksponatow++;
        BraxMuzeumKod.Lodz(g, "dział PORT", new Vector3(xP, y0 + .02f, zi0 + 6.6f), Vector3.forward); muzEksponatow++;
        { float zL = zi0 + 6.6f; Barierka(new Vector3(xP - 3.5f, 0f, zL - 1.6f), new Vector3(xP + 3.5f, 0f, zL - 1.6f), new Vector3(xP + 3.5f, 0f, zL + 1.6f), new Vector3(xP - 3.5f, 0f, zL + 1.6f), new Vector3(xP - 3.5f, 0f, zL - 1.6f)); }
        Pulpit(xP + 4.1f, zi0 + 8.0f, Vector3.forward, "ŁÓDŹ WIOSŁOWA");
        // TECHNIKA: lampy naftowe w gablocie, kamera filmowa, maszyna parowa za barierką
        Gablota(g, new Vector3(xT - 4.5f, y0 + .02f, zg + 3f), marmur, mosiadz, szklo, null, "lampy naftowe", "LAMPY NAFTOWE", q => BraxMuzeumKod.LampyNaftowe(g, "dział TECHNIKA", q, Vector3.forward));
        BraxMuzeumKod.Kamera(g, "dział TECHNIKA", new Vector3(xT - 4.5f, y0 + .02f, zg - 3f), Vector3.left); muzEksponatow++;
        Pulpit(xT - 5.6f, zg - 2.1f, Vector3.left, "KAMERA FILMOWA");
        BraxMuzeumKod.MaszynaParowa(g, "dział TECHNIKA", new Vector3(xT, y0 + .02f, zi0 + 6.6f), Vector3.forward); muzEksponatow++;
        { float zM = zi0 + 6.6f; Barierka(new Vector3(xT - 2.75f, 0f, zM - 1.65f), new Vector3(xT + 3.25f, 0f, zM - 1.65f), new Vector3(xT + 3.25f, 0f, zM + 1.65f), new Vector3(xT - 2.75f, 0f, zM + 1.65f), new Vector3(xT - 2.75f, 0f, zM - 1.65f)); }
        Pulpit(xT - 3.4f, zi0 + 8.1f, Vector3.forward, "MASZYNA PAROWA");
        // ---- 12.11 00:53 (właściciel: „jak coś możesz ściągnąć z Polyheaven zabytkowego, to ściągnij do tej muzeum”): zabytki Poly Haven
        // (fetch_region.py); brak modelu = brak eksponatu (bez pustych gablot i postumentów)
        GameObject M(params string[] ids) => BraxRegion.Model(ids);
        void NaPostumencie(GameObject m, float x, float z, float yaw, float maxH, float a, float hc, string opis, Vector3 pn, float px, float pz)
        {   // model na postumencie z marmuru, kolizja, pulpit z opisem
            if (!m) return;
            BraxUliceKit.S(marmur).Poducha(new Vector3(x - a, y0 + .02f, z - a), new Vector3(x + a, y0 + .02f + hc, z + a), .02f, 0f, 3);
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: postument", new Vector3(x, y0 + hc * .5f, z), new Vector3(2f * a, hc, 2f * a));
            if (BraxRegion.Put(g, m, "Muzeum: " + opis.ToLowerInvariant() + " (Poly Haven)", new Vector3(x, y0 + .02f + hc, z), yaw, 1f, true, maxH)) muzEksponatow++;
            Pulpit(px, pz, pn, opis);
        }
        // PORT: model statku holenderskiego w dużej gablocie, armata na drewnianym podeście, latarnie w gablotach
        var sh = M("dutch_ship_medium");
        if (sh)
        {
            BraxMuzeumKod.GablotaDuza(g, "statek holenderski", new Vector3(cx - 12.6f, y0 + .02f, zi0 + 6.6f), Vector3.forward, out var blat, out float hW);
            if (BraxRegion.Put(g, sh, "Muzeum: model statku holenderskiego (Poly Haven)", blat, 0f, 1f, false, (hW - .05f) / k)) muzEksponatow++;   // kadłub wzdłuż gabloty (sprawdzone na podglądzie)
            Pulpit(cx - 10.85f, zi0 + 7.6f, Vector3.forward, "STATEK HOLENDERSKI");
        }
        // (12.11 01:10 właściciel: „jeżeli coś się nie nadaje, to eliminuj od razu, wyrzucaj” — armata cannon_01 z Poly Haven ma szkielet
        // i nie wyświetla się poprawnie: wyrzucona; dział PORT ma własną armatę okrętową z kodu)
        Gablota(g, new Vector3(cx - 12f, y0 + .02f, zk1 + 7.4f), marmur, mosiadz, szklo, M("Lantern_01"), "latarnia");
        Gablota(g, new Vector3(cx - 9f, y0 + .02f, zk1 + 7.4f), marmur, mosiadz, szklo, M("wooden_lantern_01"), "latarnia drewniana");
        // SZTUKA: rzeźba gotycka między ściankami (zamiast spirali), wieloryb z brązu, rekin w gablocie, głowy konia i byka
        var got = M("gothic_statue");
        if (got) NaPostumencie(got, cx, zS, 180f, 1.9f, .85f, .3f, "RZEŹBA GOTYCKA", Vector3.forward, cx + 1.6f, zS + 1.4f);
        NaPostumencie(M("bronze_whale_statue"), cx - 6f, zg + 5.5f, 180f, 1.1f, .6f, .55f, "WIELORYB Z BRĄZU", Vector3.forward, cx - 4.8f, zg + 6.4f);
        // eksponaty właściciela (Tripo, 01:42; „jeżeli coś, co ci wysłałem, jest lepsze od tego, co mamy, zastępuj”): marmurowy bokser
        // i galion zamiast abstrakcji z kodu, soczewka latarni morskiej w PORCIE, pas mistrzowski BRAXHORN w gablocie na osi wejścia
        NaPostumencie(TR("bokser_marmur"), cx - 6f, zk1 + 2.6f, 0f, 1.4f, .6f, .35f, "BOKSER · MARMUR", Vector3.forward, cx - 4.95f, zk1 + 3.4f);
        NaPostumencie(TR("galion"), cx - 9.5f, zg + 2.8f, 0f, 1.75f, .55f, .3f, "GALION OKRĘTOWY", Vector3.forward, cx - 8.45f, zg + 3.6f);
        NaPostumencie(TR("soczewka_fresnela"), xP + 4.8f, zg, 0f, 1.85f, .62f, .25f, "SOCZEWKA LATARNI MORSKIEJ", Vector3.forward, xP + 3.4f, zg + .95f);
        var pas = TR("pas_mistrzowski");
        if (pas) Gablota(g, new Vector3(cx, y0 + .02f, zk1 + 4.0f), marmur, mosiadz, szklo, pas, "pas mistrzowski", "PAS MISTRZOWSKI BRAXHORN", null, .5f, .32f, .7f, 0f);
        Gablota(g, new Vector3(cx + 6f, y0 + .02f, zg + 5.5f), marmur, mosiadz, szklo, M("bronze_shark_statue"), "rekin z brązu");
        Gablota(g, new Vector3(cx - 2.5f, y0 + .02f, zi0 + 3.6f), marmur, mosiadz, szklo, M("horse_head"), "głowa konia");
        Gablota(g, new Vector3(cx + 2.5f, y0 + .02f, zi0 + 3.6f), marmur, mosiadz, szklo, M("bull_head"), "głowa byka");
        // BROŃ DAWNA (hol, strona portu): dwie wysokie gabloty — miecz, katana, młot bojowy | tarcza rycerska, buzdygan, sztylet
        string[][] bron = { new[] { "antique_estoc", "antique_katana_01", "ornate_war_hammer" }, new[] { "kite_shield", "ornate_medieval_mace", "ornate_medieval_dagger" } };
        float[] dxB = { -.4f, 0f, .4f }; int ileBroni = 0;
        for (int i = 0; i < 2; i++)
        {
            var ms = bron[i].Select(id => M(id)).ToArray(); if (ms.All(q => !q)) continue;
            var pg = new Vector3(cx - 18.6f + i * 2.4f, y0 + .02f, zk1 + 5.2f);
            BraxMuzeumKod.GablotaWysoka(g, "broń dawna " + (i + 1), pg, Vector3.forward, out var dno, out float hB);
            for (int j = 0; j < 3; j++)
                if (ms[j] && BraxRegion.Put(g, ms[j], "Muzeum: broń dawna (" + bron[i][j] + ", Poly Haven)", dno + Vector3.right * (dxB[j] * k), 180f, 1f, false, (hB - .1f) / k)) { muzEksponatow++; ileBroni++; }
        }
        if (ileBroni > 0) Pulpit(cx - 17.4f, zk1 + 3.5f, Vector3.back, "BROŃ DAWNA");
        // TECHNIKA: gabloty z aparatem, projektorem, kamerą, maską gazową, lornetką i radiostacją (część południowa), telefon na ścianie
        string[][] techStara = { new[] { "Camera_01" }, new[] { "filmstrip_projector_8mm" }, new[] { "vintage_video_camera" }, new[] { "old_gas_mask" }, new[] { "binoculars" }, new[] { "vintage_radio_transceiver" } };
        float[] zTS = { zi0 + 3.0f, zi0 + 6.6f, zi0 + 9.6f };
        for (int i = 0; i < techStara.Length; i++)
            Gablota(g, new Vector3(xT + (i < 3 ? -4.6f : 4.6f), y0 + .02f, zTS[i % 3]), marmur, mosiadz, szklo, M(techStara[i]), "technika dawna " + (i + 1), null, null, .45f, .3f, .6f, i == 1 ? 90f : 180f);
        var tel = M("vintage_telephone_wall_clock"); if (tel && BraxRegion.Put(g, tel, "Muzeum: telefon ścienny (Poly Haven)", new Vector3(xi1 - .14f, y0 + 1.5f, zg), -90f, 1f, false, .7f)) muzEksponatow++;
        // SALON MIESZCZAŃSKI (hol, strona techniki): podest z parkietu, ściana z tapetą, szafa gotycka, konsola z zegarem i lustrem,
        // fotel, kołowrotek; barierka z linką (12.11 01:10 „jeżeli coś się nie nadaje… wyrzucaj”: mosiężny żyrandol i mosiężna lampa
        // naftowa z Poly Haven wyrzucone — zasada 10.11 bez złota i mosiądzu)
        var meble = new[] { M("GothicCabinet_01"), M("ClassicConsole_01"), M("ArmChair_01") };
        if (meble.Any(q => q))
        {
            float sx = cx + 17.5f, sz = zk1 + 5.2f;
            var tapeta = BraxUliceKit.Tex("muzeum: salon — ściana malowana (zieleń)", 2.0f, new Color(.55f, .66f, .56f), new Color(.4f, .5f, .42f), .85f, "painted_plaster_wall", "white_stucco");
            BraxUliceKit.S(dab).Box(new Vector3(sx - 2.1f, y0 + .02f, sz - 1.6f), new Vector3(sx + 2.1f, y0 + .14f, sz + 1.45f), 4);                // podest (parkiet)
            BraxUliceKit.S(tapeta).Box(new Vector3(sx - 2.1f, y0 + .02f, sz + 1.45f), new Vector3(sx + 2.1f, y0 + 2.9f, sz + 1.65f), 4);            // ściana salonu
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: salon", new Vector3(sx, y0 + 1.45f, sz), new Vector3(4.2f, 2.9f, 3.3f));
            float yp = y0 + .14f;
            if (meble[0] && BraxRegion.Put(g, meble[0], "Muzeum: szafa gotycka (Poly Haven)", new Vector3(sx - 1.1f, yp, sz + .85f), 180f, 1f, false, 2.2f)) muzEksponatow++;
            var kons = meble[1] ? BraxRegion.Put(g, meble[1], "Muzeum: konsola (Poly Haven)", new Vector3(sx + 1.0f, yp, sz + 1.15f), 180f, 1f, false, 1.0f) : null;
            if (kons)
            {
                muzEksponatow++; var bk = BraxRegion.Bounds(kons);
                var zeg = M("mantel_clock_01"); if (zeg && BraxRegion.Put(g, zeg, "Muzeum: zegar kominkowy (Poly Haven)", new Vector3(bk.center.x, bk.max.y, bk.center.z), 180f, 1f, false, .4f)) muzEksponatow++;
                var lus = M("ornate_mirror_01"); if (lus && BraxRegion.Put(g, lus, "Muzeum: lustro (Poly Haven)", new Vector3(bk.center.x, bk.max.y + .55f, sz + 1.45f - .05f), 180f, 1f, false, .8f)) muzEksponatow++;
            }
            if (meble[2] && BraxRegion.Put(g, meble[2], "Muzeum: fotel (Poly Haven)", new Vector3(sx + .1f, yp, sz - .4f), 200f, 1f, false, 1.2f)) muzEksponatow++;
            var kolow = M("spinning_wheel_01"); if (kolow && BraxRegion.Put(g, kolow, "Muzeum: kołowrotek (Poly Haven)", new Vector3(sx + 1.5f, yp, sz - .6f), 120f, 1f, false, 1.0f)) muzEksponatow++;
            Barierka(new Vector3(sx - 2.35f, 0f, sz + 1.6f), new Vector3(sx - 2.35f, 0f, sz - 1.9f), new Vector3(sx + 2.35f, 0f, sz - 1.9f), new Vector3(sx + 2.35f, 0f, sz + 1.6f));
            Pulpit(sx - 3.0f, sz - 2.2f, Vector3.back, "SALON MIESZCZAŃSKI");
        }
        // ściana południowa: obrazy z kodu na przemian z obrazami w ramach (Poly Haven), barierka z linką
        string[] ramy = { "hanging_picture_frame_01", "hanging_picture_frame_02", "hanging_picture_frame_03" };
        int[] poludnie = { 2, 9, 12, 5, 13, 0, 7, 6, 8, 14, 1, 15 }; int ir = 0, io = 0;
        for (float x = xi0 + 4f; x < xi1 - 3f; x += 4.5f, ir++)
        {
            var rm = ir % 2 == 1 ? BraxRegion.Model(ramy[(ir / 2) % ramy.Length]) : null;
            if (rm && BraxRegion.Put(g, rm, "Muzeum: obraz w ramie", new Vector3(x, y0 + 1.45f * k, zi0 + .06f), 0f, 1f, false, 1.2f * k)) { muzEksponatow++; continue; }   // przodem do sali (+z)
            ObrazNaScianie(new Vector3(x, 0f, zi0), Vector3.forward, poludnie[io++ % poludnie.Length]);
        }
        Barierka(new Vector3(xi0 + 2f, 0f, zi0 + 1.4f), new Vector3(xi1 - 1.5f, 0f, zi0 + 1.4f));
        // ściana od wejścia (od strony sali): obrazy po obu stronach holu
        int[] polnoc = { 1, 13, 5, 15 }; float[] xN = { -15f, -9f, 9f, 15f };
        for (int i = 0; i < 4; i++)   // (obrazy olejne Poly Haven w złotych ramach wyrzucone 12.11 01:10 — bez złota)
        {
            float x = cx + xN[i]; if (x < xi0 + 2f || x > xi1 - 2f || Mathf.Abs(x - rx) < 3f) continue;
            ObrazNaScianie(new Vector3(x, 0f, zi1), Vector3.back, polnoc[i]);
        }
        // eksponat centralny: kotwica portowa (miasto portowe — ul. Portowa) na kamiennym cokole, z kodu: trzon, poprzeczka, ramiona
        // z łapami, pierścień; tablice działów na ścianach (litery 3D): PORT · SZTUKA · TECHNIKA
        {
            float cz = zg + (zk1 - zk0) * .25f;
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
            foreach (float sx in new[] { -1f, 1f })   // łapy: płaskie płyty w kształcie łopaty na końcach ramion (prostopadle do płaszczyzny kotwicy)
                kt.OBox(new Vector3(cx + sx * .95f, yb + 1.32f, cz), Vector3.up, Vector3.forward, Vector3.right * sx, .42f, .3f, .035f);
            BraxRegionMeble.Kolizja(g, "Muzeum kolizja: kotwica", new Vector3(cx, y0 + 1.5f, cz), new Vector3(2.2f, 3f, 1.4f));
            BraxRegionMeble.Napis(g, "KOTWICA PORTOWA", new Vector3(cx, y0 + .3f, cz + .72f), Vector3.forward, .09f * k, grafit);
            muzEksponatow++;
            foreach (var (tekst, p, n) in new[] { ("PORT", new Vector3(xi0, y0 + 3.72f, (zi0 + zi1) * .5f), Vector3.right), ("SZTUKA", new Vector3(cx, y0 + 3.72f, zi0), Vector3.forward), ("TECHNIKA", new Vector3(xi1, y0 + 3.72f, (zi0 + zi1) * .5f), Vector3.left) })
                BraxRegionMeble.Napis(g, tekst, p, n, .38f * k, grafit, .07f);   // litery 7 cm grubości na licu ściany
        }
        BraxUliceKit.Zapisz(g, "muzeum");
    }

    static string OpisEksponatu(string n)
    {
        n = (n ?? "").ToLowerInvariant();
        if (n.Contains("bust")) return "POPIERSIE MARMUROWE";
        if (n.Contains("horse_head")) return "GŁOWA KONIA";
        if (n.Contains("bull_head")) return "GŁOWA BYKA";
        if (n.Contains("shark")) return "REKIN Z BRĄZU";
        if (n.Contains("video_camera")) return "KAMERA AMATORSKA";
        if (n.Contains("camera")) return "APARAT FOTOGRAFICZNY";
        if (n.Contains("projector")) return "PROJEKTOR FILMOWY";
        if (n.Contains("gas_mask")) return "MASKA GAZOWA";
        if (n.Contains("binocular")) return "LORNETKA";
        if (n.Contains("radio_transceiver")) return "RADIOSTACJA";
        if (n.Contains("wooden_lantern")) return "LATARNIA DREWNIANA";
        if (n.Contains("lantern")) return "LATARNIA";
        if (n.Contains("horse")) return "RZEŹBA KONIA";
        if (n.Contains("tea_set")) return "SERWIS DO HERBATY";
        if (n.Contains("plate")) return "TALERZ RZEŹBIONY";
        if (n.Contains("vase")) return "WAZA CERAMICZNA";
        if (n.Contains("television")) return "TELEWIZOR LAMPOWY";
        if (n.Contains("boombox")) return "RADIOMAGNETOFON";
        if (n.Contains("spectacles")) return "OKULARY";
        if (n.Contains("magnifying")) return "LUPA";
        return "EKSPONAT";
    }

    // gablota: postument z marmuru (0.9 m × K), szklany klosz z chromowaną ramą, eksponat w środku (model Poly Haven albo z kodu),
    // tabliczka; bez eksponatu gablota nie powstaje (żadnych pustych gablot)
    static void Gablota(Transform g, Vector3 p, Material marmur, Material chrom, Material szklo, GameObject model, string nazwa, string opis = null, System.Action<Vector3> kod = null, float a = .45f, float c = .3f, float hgM = .6f, float yawModelu = 180f)
    {
        if (!model && kod == null) return;
        float k = K, hp = .9f * k, hg = hgM * k;
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
        if (model) { if (BraxRegion.Put(g, model, "Muzeum: " + nazwa + " (" + model.name + ")", new Vector3(p.x, y1 + .004f, p.z), yawModelu, 1f, false, (hg - .08f) / k)) muzEksponatow++; }   // Put mnoży limit przez K
        else { kod(new Vector3(p.x, y1, p.z)); muzEksponatow++; }
        // tabliczka opisowa na postumencie (od zwiedzającego, +z), litery grafitowe na marmurze
        BraxRegionMeble.Napis(g, opis ?? OpisEksponatu(model ? model.name : null), new Vector3(p.x, p.y + hp * .78f, p.z + c + .01f), Vector3.forward, .05f * k, new Color(.13f, .13f, .14f));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// NOWE DZIELNICE — MIEJSCA POD GRĘ (MIASTO R1, 16.11.2026). Właściciel 17:38: „tą nową część miasta… zagospodarować… coś pod grę…
// sklepy”; 17:57: „rób te ustawki, zrób jakieś miejsce… szare miejsce, ciemne na ustawki… biuro deweloperskie… buchmacherkę…
// mały komis samochodów używanych… koło tramwaju”; 17:55: „wszystko realistyczne… najlepsze materiały”.
//  1) LOKALE W PARTERACH WIEŻOWCÓW (BraxRegionFawelaWiezeLokale): płyty kwartałów A i C bez posadzek lokali (posadzka lokalu
//     z generatora wnętrz — nie dwie powierzchnie w jednej płaszczyźnie); po budynkach kontrola: lokal bez wieżowca → łata płyt.
//  2) PLAC USTAWEK ZA ATLANTĄ: betonowe podwórze w cieniu wieżowca (274 m) — zużyty beton zamiast granitu placu, ogrodzenie
//     z prefabrykatów betonowych 2 m × K (słupki, 4 płyty na przęsło, różne odcienie zabrudzenia), wjazd od placu, jedno wyłamane
//     przęsło od ciągu pieszego (droga ucieczki), dwie stare latarnie sodowe (jedna nie świeci), dwa rdzawe kontenery 20′ (model
//     właściciela), bariery betonowe New Jersey (profil jak w życiu) jako ławki kibiców. AKCJA: USTAWKA 3 NA 3.
//  3) KOMIS SAMOCHODÓW UŻYWANYCH przy przystanku T1 TRAMWAJOWA (pas między ul. Tramwajową a granicą miasta): plac z asfaltu,
//     5 aut na sprzedaż (modele właściciela z tablicami BX — ekspozycja, bez silnika i bez ruchu), kontener biurowy z oknem
//     i drzwiami, pylon KOMIS AUTO, słupki z łańcuchem od chodnika (wjazd), maszty z lampami i girlandą chorągiewek.
public static partial class BraxRegionMiastoB
{
    // ---------------- 1) lokale w parterach wieżowców ----------------
    static List<(string klucz, Rect r)> ndPosadzki = new List<(string, Rect)>();
    static List<(string, Rect)> NdPosadzkiLokali()
    {
        var l = new List<(string, Rect)>();
        if (!BraxRegionFawela.WiezeMurowaneWlaczone) return l;
        foreach (var b in NoweBudynki.Where(b => b.paczka == "miasto_centrum")) l.AddRange(BraxRegionFawela.PosadzkiLokali(b.id, b.x, b.z, b.w, b.d));
        return l;
    }

    // chodnik (płyty) na prostokącie bez wskazanych prostokątów (podział bez nakładania — Odejmij)
    static void ChodnikBez(string n, Rect a, IEnumerable<Rect> bez)
    {
        var l = new List<Rect> { a };
        foreach (var b in bez) l = l.SelectMany(x => Odejmij(x, b)).Where(Niepuste).ToList();
        foreach (var r in l) Chodnik(n, r.xMin, r.xMax, r.yMin, r.yMax);
    }

    // po budynkach: lokal, którego wieżowiec nie powstał (paczka, błąd) → płyty z powrotem (bez dziury w chodniku)
    static void NdLokaleKontrola()
    {
        int laty = 0;
        foreach (var (klucz, r) in ndPosadzki)
            if (!BraxRegionFawela.lokaleWiezPlan.Contains(klucz)) { Chodnik("łata płyt (lokal niezbudowany: " + klucz + ")", r.xMin, r.xMax, r.yMin, r.yMax); laty++; }
        var bezWnetrza = BraxRegionFawela.lokaleWiezPlan.Where(k => !BraxRegionFawela.lokaleWiezZbudowane.Contains(k)).ToList();
        string s = "BRAXHORN_LOKALE_WIEZ razem: posadzek " + ndPosadzki.Count + ", zaplanowanych " + BraxRegionFawela.lokaleWiezPlan.Count + ", zbudowanych " + BraxRegionFawela.lokaleWiez
                   + (laty > 0 ? ", łaty płyt " + laty : "") + (bezWnetrza.Count > 0 ? " — DO POPRAWKI (bez wnętrza): " + string.Join(", ", bezWnetrza) : "");
        if (bezWnetrza.Count > 0 || laty > 0) Debug.LogWarning(s); else Debug.Log(s);
        BraxRegion.Note("nowe dzielnice: lokale w parterach wieżowców " + BraxRegionFawela.lokaleWiez + "/" + ndPosadzki.Count);
    }

    // ---------------- 2) plac ustawek za ATLANTĄ ----------------
    internal static readonly Rect PlacUstawekA = Rect.MinMaxRect(-869f, 108f, -821f, 147.5f);
    static Material betonUstawek;
    static Material BetonUstawek => betonUstawek ? betonUstawek : (betonUstawek = BraxRegionMeble.Tex("Plac ustawek: beton zużyty (ciemny)", 2.4f, new Color(.56f, .56f, .57f), .94f, "concrete_floor_worn_001", "concrete_floor_02"));

    // plac Atlanty z płyt granitowych bez podwórza ustawek + podwórze z zużytego betonu (ta sama wysokość, styk krawędzią)
    static void NdPlacAtlanty()
    {
        var plac = Rect.MinMaxRect(NXA, NS1 - NPR, NC[3] - NPR, RzB1);
        foreach (var r in Odejmij(plac, PlacUstawekA)) if (Niepuste(r)) Plyta("Plac ATLANTA: płyty granitowe", granitPlac, r.xMin, r.xMax, r.yMin, r.yMax, YC, .3f - YA);
        Plyta("Plac ustawek za ATLANTĄ: beton zużyty", BetonUstawek, PlacUstawekA.xMin, PlacUstawekA.xMax, PlacUstawekA.yMin, PlacUstawekA.yMax, YC, .3f - YA);
    }

    static void NdPlacUstawek()
    {
        var g = new GameObject("CLAUDE nowe dzielnice: PLAC USTAWEK za ATLANTĄ (szare podwórze)").transform; g.SetParent(root, false);
        var R = PlacUstawekA; float y = Podloze(R.center.x, R.center.y, YC);
        // ogrodzenie z prefabrykatów: słupek 12 × 12 cm, przęsło 2.0 m, 4 płyty 200 × 50 × 5 cm (× K), czapka słupka
        var tony = new[]
        {
            BraxRegionMeble.Tex("Płot betonowy: płyta (szara)", 1.6f, new Color(.62f, .62f, .61f), .93f, "concrete_wall_008", "concrete_floor_02"),
            BraxRegionMeble.Tex("Płot betonowy: płyta (zabrudzona)", 1.6f, new Color(.50f, .50f, .49f), .95f, "concrete_wall_008", "concrete_floor_02"),
            BraxRegionMeble.Tex("Płot betonowy: płyta (zawilgocona)", 1.6f, new Color(.43f, .44f, .44f), .96f, "concrete_wall_008", "concrete_floor_02"),
        };
        var slupMat = BraxRegionMeble.Tex("Płot betonowy: słupek", 1.2f, new Color(.55f, .55f, .54f), .93f, "concrete_wall_008", "concrete_floor_02");
        var panele = tony.Select(_ => new BraxRegionMeble.Bud()).ToArray(); var slupy = new BraxRegionMeble.Bud();
        var rnd = new System.Random(4242);
        float prz = 2.0f * K, hp = .5f * K, gp = .05f * K, ss = .12f * K, H = 4 * hp + .1f * K;
        int przesel = 0, wylamane = 0;
        // linia płotu: od a do b; przerwy = przęsła bez płyt (wjazd, wyłamane); kolizja ciągłych odcinków
        // narożne słupki stawiają linie południowa i północna (zachodnia i wschodnia bez słupków końcowych — bez dwóch brył w jednym miejscu)
        void Plot(Vector3 a, Vector3 b, Func<int, int, bool> bezPlyt, string nazwa, bool slupkiKoncowe = true)
        {
            var dir = (b - a); float L = dir.magnitude; dir /= L; var bok = Vector3.Cross(Vector3.up, dir);
            int n = Mathf.Max(1, Mathf.RoundToInt(L / prz)); float p = L / n; int runStart = 0;
            for (int i = 0; i <= n; i++)
            {
                var c = a + dir * (p * i); c.y = y;
                if (slupkiKoncowe || (i > 0 && i < n)) {
                slupy.Box(c + Vector3.up * (H + .04f * K) * .5f, new Vector3(ss, H + .04f * K, ss));
                slupy.Box(c + Vector3.up * (H + .04f * K + .025f * K), new Vector3(ss + .03f * K, .05f * K, ss + .03f * K));   // czapka
                }
                if (i == n) break;
                bool pusto = bezPlyt(i, n);
                if (!pusto)
                {
                    var m0 = a + dir * (p * i + ss * .5f); var m1 = a + dir * (p * (i + 1) - ss * .5f); var mc = (m0 + m1) * .5f; mc.y = y; float dl = (m1 - m0).magnitude;
                    for (int r = 0; r < 4; r++)
                    {
                        var cc = mc + Vector3.up * (.06f * K + hp * r + (hp - .012f) * .5f);
                        panele[rnd.Next(panele.Length)].Box(cc, Mathf.Abs(dir.x) > .5f ? new Vector3(dl, hp - .012f, gp) : new Vector3(gp, hp - .012f, dl));
                    }
                    przesel++;
                }
                if (pusto || i == n - 1)
                {
                    int koniec = pusto ? i : i + 1;
                    if (koniec > runStart)
                    {
                        var k0 = a + dir * (p * runStart); var k1 = a + dir * (p * koniec); var kc = (k0 + k1) * .5f; kc.y = y + H * .5f;
                        BraxRegionMeble.Kolizja(g, "Płot betonowy: kolizja (" + nazwa + ")", kc, Mathf.Abs(dir.x) > .5f ? new Vector3((k1 - k0).magnitude, H, ss + .04f) : new Vector3(ss + .04f, H, (k1 - k0).magnitude));
                    }
                    runStart = i + 1;
                    if (pusto) wylamane++;
                }
            }
        }
        Plot(new Vector3(R.xMin, y, R.yMin), new Vector3(R.xMax, y, R.yMin), (i, n) => false, "południe");
        Plot(new Vector3(R.xMin, y, R.yMin), new Vector3(R.xMin, y, R.yMax), (i, n) => false, "zachód", false);
        Plot(new Vector3(R.xMin, y, R.yMax), new Vector3(R.xMax, y, R.yMax), (i, n) => i == n - 5, "północ (jedno przęsło wyłamane — przejście na ciąg pieszy)");
        Plot(new Vector3(R.xMax, y, R.yMin), new Vector3(R.xMax, y, R.yMax), (i, n) => i >= n / 2 - 1 && i <= n / 2 + 1, "wschód (wjazd od placu Atlanty)", false);
        for (int i = 0; i < panele.Length; i++) panele[i].Obiekt("Płot betonowy: płyty (" + tony[i].name + ")", g, tony[i], false, true);
        slupy.Obiekt("Płot betonowy: słupki z czapkami", g, slupMat, false, true);

        // latarnie sodowe na starych słupach stalowych (wysięgnik, oprawa); NW świeci pomarańczowo, SE nie świeci
        var stalS = BraxRegionMeble.Tex("Plac ustawek: stal słupa (rdzawa farba)", 1.0f, new Color(.42f, .40f, .38f), .7f, "painted_metal_shutter", "metal_plate");
        var oprawa = BraxCityMaterials.Surface("Plac ustawek: oprawa latarni (aluminium szare)", new Color(.38f, .39f, .40f), .5f, .6f);
        var sod = BraxRegionMeble.Swiecacy("Plac ustawek: lampa sodowa (pomarańczowa)", new Color(1f, .62f, .26f), 1.4f);
        var zgasla = BraxCityMaterials.Surface("Plac ustawek: klosz zgaszonej lampy (zmatowiały)", new Color(.45f, .42f, .36f), .4f);
        foreach (var (p, wys, swieci) in new[] { (new Vector3(R.xMin + 3f, y, R.yMax - 3f), 7.5f * K * .8f, true), (new Vector3(R.xMax - 3f, y, R.yMin + 3f), 7.5f * K * .8f, false) })
        {
            var sl = new BraxRegionMeble.Bud(); var op = new BraxRegionMeble.Bud(); var kl = new BraxRegionMeble.Bud();
            var ku = (R.center - new Vector2(p.x, p.z)).normalized; var kier = new Vector3(ku.x, 0f, ku.y);
            sl.Cyl(p, p + Vector3.up * wys, .1f * K, 12, .065f * K);
            sl.Cyl(p + Vector3.up * (wys - .05f), p + Vector3.up * (wys + .25f * K) + kier * 1.4f * K, .04f * K, 8);
            var h = p + Vector3.up * (wys + .25f * K) + kier * 1.55f * K;
            op.Box(h + Vector3.up * .06f * K, new Vector3(.55f * K, .14f * K, .55f * K), Quaternion.LookRotation(kier));
            kl.Box(h - Vector3.up * .03f * K, new Vector3(.42f * K, .04f * K, .42f * K), Quaternion.LookRotation(kier));
            sl.Obiekt("Plac ustawek: słup latarni", g, stalS, false, true); op.Obiekt("Plac ustawek: oprawa latarni", g, oprawa, false, true);
            kl.Obiekt(swieci ? "Plac ustawek: klosz lampy sodowej (świeci)" : "Plac ustawek: klosz lampy (zgaszony)", g, swieci ? sod : zgasla, false, false);
            BraxRegionMeble.Kolizja(g, "Plac ustawek: kolizja słupa", p + Vector3.up * wys * .5f, new Vector3(.22f * K, wys, .22f * K));
        }

        // kontenery 20′ (model właściciela, rdzawe) wzdłuż płotu północnego i zachodniego — zakamarki podwórza
        int kont = 0;
        foreach (var (p, yaw, odc) in new[] { (new Vector3(R.xMin + 10f, y, R.yMax - 1.75f * K - .25f), 0f, 1), (new Vector3(R.xMin + 1.75f * K + .25f, y, R.yMin + 12f), 90f, 2) })
        {
            var k = BraxMiastoAssety.Kontener20(g, p, yaw, odc);
            if (!k) continue;
            var b = BraxRegion.Bounds(k); k.transform.position += new Vector3(0f, y - b.min.y + .002f, 0f);
            BraxMiastoAssety.Kolizja(g, k, "Plac ustawek: kolizja kontenera 20′"); kont++;
        }

        // bariery betonowe New Jersey (3.0 × 0.61 × 0.81 m × K, profil F): ławki kibiców pod płotem południowym i przy wjeździe
        var jer = BraxRegionMeble.Tex("Bariera betonowa New Jersey (beton)", 1.4f, new Color(.66f, .66f, .64f), .92f, "concrete_floor_02", "concrete_wall_008");
        var bj = new BraxRegionMeble.Bud(); int barier = 0;
        void Jersey(Vector3 c, Vector3 os)
        {
            os.y = 0f; os.Normalize(); var bok = Vector3.Cross(Vector3.up, os); float L = 3.0f * K * .5f;
            // profil F (od dołu): podstawa 61 cm × 7.5 cm, załamanie na 33 cm (szer. 40 cm), korona 15 cm na 81 cm (× K)
            var prof = new[] { new Vector2(-.305f, 0f), new Vector2(-.305f, .075f), new Vector2(-.20f, .33f), new Vector2(-.075f, .81f), new Vector2(.075f, .81f), new Vector2(.20f, .33f), new Vector2(.305f, .075f), new Vector2(.305f, 0f) };
            Vector3 P(Vector2 q, float s) => c + bok * q.x * K + Vector3.up * q.y * K + os * s;
            for (int i = 0; i + 1 < prof.Length; i++) bj.Quad(P(prof[i], -L), P(prof[i], L), P(prof[i + 1], L), P(prof[i + 1], -L));   // lica na zewnątrz
            foreach (float s in new[] { -L, L })
                for (int i = 1; i + 1 < prof.Length; i++) { if (s < 0) bj.Tri(P(prof[0], s), P(prof[i], s), P(prof[i + 1], s)); else bj.Tri(P(prof[0], s), P(prof[i + 1], s), P(prof[i], s)); }
            BraxRegionMeble.Kolizja(g, "Bariera New Jersey: kolizja", c + Vector3.up * .405f * K, Mathf.Abs(os.x) > .5f ? new Vector3(2f * L, .81f * K, .5f * K) : new Vector3(.5f * K, .81f * K, 2f * L));
            barier++;
        }
        for (int i = 0; i < 3; i++) Jersey(new Vector3(R.xMin + 14f + i * (3.0f * K + .6f), y, R.yMin + 1.4f), Vector3.right);
        Jersey(new Vector3(R.xMax - 1.6f, y, R.yMin + 5f), Vector3.forward);
        Jersey(new Vector3(R.xMax - 1.6f, y, R.yMax - 6f), Vector3.forward);
        bj.Obiekt("Bariery betonowe New Jersey", g, jer, false, true);

        // gra: AKCJA ustawki na środku podwórza, punkt świata
        var mz = new GameObject("BRAXHORN AKCJA: USTAWKA 3 NA 3 (plac za ATLANTĄ)"); mz.transform.SetParent(g, false); mz.transform.position = new Vector3(R.center.x + 4f, y, R.center.y);
        var z = mz.AddComponent<BraxMiejsceZadania>(); z.id = "ustawka_atlanta"; z.napis = "USTAWKA 3 NA 3"; z.promien = 7f;
        world_point("R1_PLAC_USTAWEK_ATLANTA", "plac ustawek 3 na 3 za ATLANTĄ (betonowe podwórze, nocą)", mz.transform.position);
        NdLog("plac ustawek za ATLANTĄ: " + R.width.ToString("F0") + " × " + R.height.ToString("F0") + " m, przęseł płotu " + przesel + " (bez płyt " + wylamane + "), kontenerów " + kont + ", barier " + barier + ", latarnie 2 (jedna zgaszona)");
    }

    // ---------------- 3) komis samochodów używanych przy przystanku T1 TRAMWAJOWA ----------------
    internal static readonly Rect KomisRect = Rect.MinMaxRect(-738f, -83.2f, -688f, NS1 - NPR);
    const string KomisPaczka = "pojazdy_komis_01";
    static readonly (string plik, string opis)[] KomisAuta =
    {
        ("Braxhorn_Coupe_BX10013", "coupé BX 10013 — pierwszy właściciel, serwisowane w ASO"),
        ("Braxhorn_Graphite_Coupe_BX10014", "coupé grafitowe BX 10014 — bezwypadkowe, komplet kluczyków"),
        ("Braxhorn_GT6_Coupe_BX10007", "GT6 coupé BX 10007 — niski przebieg, opony nowe"),
        ("Braxhorn_Roadster_BX10009", "roadster BX 10009 — dach sprawny, garażowany"),
        ("Braxhorn_S2_Spider_BX10008", "S2 spider BX 10008 — po wymianie rozrządu"),
    };

    static void NdKomisNawierzchnia()
    {
        Plyta("Komis: plac z asfaltu", asfalt, KomisRect.xMin, KomisRect.xMax, KomisRect.yMin, KomisRect.yMax, YC, .3f - YA);
    }

    static void NdKomis()
    {
        var g = new GameObject("CLAUDE nowe dzielnice: KOMIS SAMOCHODÓW UŻYWANYCH (przy T1 TRAMWAJOWA)").transform; g.SetParent(root, false);
        var R = KomisRect; float y = Podloze(R.center.x, R.center.y, YC);
        var stalM = BraxCityMaterials.Surface("Komis: stal ocynkowana", new Color(.62f, .64f, .66f), .45f, .75f);
        // --- auta na sprzedaż: przodem do ulicy (+z), przód 1.2 m od linii słupków; przód auta z kół (Wheel_FL/FR przed RL/RR)
        int aut = 0; float zPrzod = R.yMax - 1.25f;
        for (int i = 0; i < KomisAuta.Length; i++)
        {
            var (plik, opis) = KomisAuta[i];
            float x = R.xMin + 13.5f + i * 4.55f;
            if (!BraxMiastoAssety.Jest(KomisPaczka)) break;
            var m = BraxMiastoAssety.Model(KomisPaczka, plik); if (!m) continue;
            var o = BraxMiastoAssety.Postaw(g, m, "Komis: " + opis.Split('—')[0].Trim() + " (ekspozycja — bez silnika, bez ruchu)", new Vector3(x, y, R.center.y), 0f, 1f, true, true);
            if (!o) continue;
            var ts = o.GetComponentsInChildren<Transform>(true);
            Vector3 Sr(string a, string b) { var p = ts.Where(t => t.name == a || t.name == b).Select(t => t.position).ToList(); return p.Count > 0 ? p.Aggregate(Vector3.zero, (s, v) => s + v) / p.Count : Vector3.zero; }
            var przod = Sr("Wheel_FL", "Wheel_FR") - Sr("Wheel_RL", "Wheel_RR"); przod.y = 0f;
            if (przod.sqrMagnitude > .01f) o.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(przod, Vector3.forward, Vector3.up), 0f) * o.transform.rotation;   // obrót tylko wokół pionu
            var b = BraxRegion.Bounds(o);
            o.transform.position += new Vector3(x - b.center.x, y + .002f - b.min.y, zPrzod - b.max.z);
            BraxMiastoAssety.Kolizja(g, o, "Komis: kolizja auta " + (i + 1), .05f);
            BraxMiastoA.Akcja(g, "komis_auto_" + (i + 1), "OBEJRZYJ AUTO", "Komisant: " + opis + ". Cena do negocjacji, jazda próbna po spisaniu dowodu.", new Vector3(x, y, zPrzod + .7f), 1.4f);
            aut++;
        }
        // --- kontener biurowy (6.06 × 2.44 × 2.60 m × K): rama stalowa, ściany z blachy trapezowej, drzwi i okno od placu, szyld na dachu
        {
            float L = 6.06f * K, B = 2.44f * K, Hc = 2.60f * K, x0 = R.xMin + .6f, z0 = R.yMin + .5f, x1 = x0 + L, z1 = z0 + B;
            var rama = new BraxRegionMeble.Bud(); var sc = new BraxRegionMeble.Bud(); var szk = new BraxRegionMeble.Bud(); var dr = new BraxRegionMeble.Bud();
            float r = .12f * K;
            foreach (float xx in new[] { x0, x1 - r }) foreach (float zz in new[] { z0, z1 - r }) rama.MinMax(new Vector3(xx, y, zz), new Vector3(xx + r, y + Hc, zz + r));
            foreach (float yy in new[] { y, y + Hc - r }) { rama.MinMax(new Vector3(x0, yy, z0), new Vector3(x1, yy + r, z0 + r)); rama.MinMax(new Vector3(x0, yy, z1 - r), new Vector3(x1, yy + r, z1)); rama.MinMax(new Vector3(x0, yy, z0 + r), new Vector3(x0 + r, yy + r, z1 - r)); rama.MinMax(new Vector3(x1 - r, yy, z0 + r), new Vector3(x1, yy + r, z1 - r)); }
            sc.MinMax(new Vector3(x0 + .02f, y + r, z0 + .02f), new Vector3(x1 - .02f, y + Hc - r, z1 - .02f));   // korpus (ściany cofnięte 2 cm za ramę)
            sc.MinMax(new Vector3(x0 + .02f, y + Hc - r, z0 + .02f), new Vector3(x1 - .02f, y + Hc - .01f, z1 - .02f));
            // drzwi stalowe 0.9 × 2.0 m × K i okno 1.2 × 1.0 m × K na licu północnym (od placu), 1 cm przed blachą
            float dx = x1 - 1.3f * K, zf = z1 - .01f;
            dr.MinMax(new Vector3(dx - .45f * K, y + r, zf), new Vector3(dx + .45f * K, y + r + 2.0f * K, zf + .025f));
            var ok0 = new Vector3(x0 + 1.4f * K, y + .95f * K, zf); var ok1 = new Vector3(x0 + 2.6f * K, y + 1.95f * K, zf + .02f);
            rama.MinMax(ok0 - new Vector3(.05f, .05f, 0f), new Vector3(ok1.x + .05f, ok1.y + .05f, zf + .015f));
            szk.Quad(new Vector3(ok0.x, ok0.y, zf + .021f), new Vector3(ok1.x, ok0.y, zf + .021f), new Vector3(ok1.x, ok1.y, zf + .021f), new Vector3(ok0.x, ok1.y, zf + .021f));
            rama.Obiekt("Komis: kontener biurowy — rama stalowa", g, BraxRegionMeble.Tex("Komis: kontener — rama (stal malowana grafit)", 1.0f, new Color(.30f, .31f, .32f), .55f, "painted_metal_shutter", "metal_plate"), false, true);
            sc.Obiekt("Komis: kontener biurowy — blacha trapezowa", g, BraxRegionMeble.Tex("Komis: kontener — blacha trapezowa (biała)", 1.2f, new Color(.90f, .91f, .90f), .55f, "corrugated_iron", "painted_metal_shutter"), false, true);
            dr.Obiekt("Komis: kontener biurowy — drzwi stalowe", g, BraxRegionMeble.Tex("Komis: drzwi kontenera (stal malowana)", 1.0f, new Color(.55f, .57f, .58f), .5f, "painted_metal_shutter", "metal_plate"), false, true);
            szk.Obiekt("Komis: kontener biurowy — szyba (przyciemniana, rolety)", g, BraxCityMaterials.Surface("Komis: szyba okna (przyciemniana)", new Color(.13f, .16f, .18f), .06f, .6f), false, false);
            BraxRegionMeble.Kolizja(g, "Komis: kolizja kontenera biurowego", new Vector3((x0 + x1) * .5f, y + Hc * .5f, (z0 + z1) * .5f), new Vector3(L, Hc, B));
            BraxMiastoA.Akcja(g, "komis_biuro", "ZAPYTAJ O AUTO", "Komisant: Skup, sprzedaż, zamiana. Wszystkie auta z dokumentami — wybierz, pogadamy o cenie.", new Vector3(dx, y, z1 + 1.2f), 1.6f);
            world_point("R1_KOMIS_AUT", "komis samochodów używanych (biuro w kontenerze, przy T1 TRAMWAJOWA)", new Vector3(dx, y, z1 + 1.2f));
        }
        // --- słupki z łańcuchem wzdłuż chodnika (wjazd 8 m przy pylonie) i pylon KOMIS AUTO
        {
            var sl = new BraxRegionMeble.Bud(); float zs = R.yMax - .35f, xWj0 = R.xMax - 9.2f, xWj1 = R.xMax - 1.2f;
            var pts = new List<float>(); for (float x = R.xMin + .4f; x <= xWj0 + .01f; x += 2.4f) pts.Add(x);
            if (pts[pts.Count - 1] < xWj0 - .2f) pts.Add(xWj0);
            foreach (float x in pts) { sl.Cyl(new Vector3(x, y, zs), new Vector3(x, y + .75f * K, zs), .035f * K, 10); sl.Cyl(new Vector3(x, y + .75f * K, zs), new Vector3(x, y + .78f * K, zs), .045f * K, 10); }
            var lan = new BraxRegionMeble.Bud();
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector3 a = new Vector3(pts[i], y + .68f * K, zs), b = new Vector3(pts[i + 1], y + .68f * K, zs), m = (a + b) * .5f - Vector3.up * .12f * K;
                lan.Cyl(a, m, .008f * K, 5); lan.Cyl(m, b, .008f * K, 5);
            }
            sl.Obiekt("Komis: słupki od chodnika", g, stalM, false, true);
            lan.Obiekt("Komis: łańcuch między słupkami", g, stalM, false, false);
            BraxRegionMeble.Kolizja(g, "Komis: kolizja słupków i łańcucha", new Vector3((pts[0] + pts[pts.Count - 1]) * .5f, y + .4f * K, zs), new Vector3(pts[pts.Count - 1] - pts[0], .8f * K, .12f));
            // pylon: dwa słupy stalowe, tablica 3.4 × 1.1 m, napis z obu stron
            float xp = R.xMax - .6f; var pyl = new BraxRegionMeble.Bud(); var tab = new BraxRegionMeble.Bud();
            float yt0 = y + 2.4f * K, yt1 = yt0 + 1.1f * K;
            foreach (float dz in new[] { -1.5f * K, 1.5f * K }) pyl.Cyl(new Vector3(xp, y, zs - 1.9f + dz), new Vector3(xp, yt1 + .1f, zs - 1.9f + dz), .06f * K, 12);
            tab.MinMax(new Vector3(xp - .08f, yt0, zs - 1.9f - 1.7f * K), new Vector3(xp + .08f, yt1, zs - 1.9f + 1.7f * K));
            pyl.Obiekt("Komis: pylon — słupy", g, stalM, false, true);
            tab.Obiekt("Komis: pylon — tablica (granat)", g, BraxCityMaterials.Surface("Komis: tablica pylonu (granatowa, lakier)", new Color(.05f, .12f, .32f), .35f, .2f), false, true);
            foreach (var n in new[] { Vector3.right, Vector3.left })
            {
                var c = new Vector3(xp, 0f, zs - 1.9f) + n * .09f;
                BraxRegionMeble.Napis(g, "KOMIS AUTO", new Vector3(c.x, yt0 + .62f * K, c.z), n, .36f * K, new Color(.98f, .82f, .2f), .02f);
                BraxRegionMeble.Napis(g, "SKUP  SPRZEDAŻ  ZAMIANA", new Vector3(c.x, yt0 + .2f * K, c.z), n, .14f * K, Color.white, .015f);
            }
            BraxRegionMeble.Kolizja(g, "Komis: kolizja pylonu", new Vector3(xp, y + (yt1 - y) * .5f, zs - 1.9f), new Vector3(.2f, yt1 - y, 3.4f * K));
        }
        // --- maszty z lampami z tyłu placu i girlanda chorągiewek (czerwień, biel, żółć, granat) od dachu kontenera
        {
            var ms = new BraxRegionMeble.Bud(); var lin = new BraxRegionMeble.Bud(); var lampy = new BraxRegionMeble.Bud();
            float zb = R.yMin + .7f, hm = 5.0f * K;
            var xs = new[] { R.xMin + 22f, R.xMin + 34f, R.xMin + 46f };
            foreach (float x in xs)
            {
                ms.Cyl(new Vector3(x, y, zb), new Vector3(x, y + hm, zb), .07f * K, 12, .05f * K);
                lampy.Box(new Vector3(x, y + hm - .2f * K, zb + .3f * K), new Vector3(.5f * K, .1f * K, .28f * K), Quaternion.Euler(-25f, 0f, 0f));
                BraxRegionMeble.Kolizja(g, "Komis: kolizja masztu", new Vector3(x, y + hm * .5f, zb), new Vector3(.16f * K, hm, .16f * K));
            }
            ms.Obiekt("Komis: maszty oświetlenia", g, stalM, false, true);
            lampy.Obiekt("Komis: naświetlacze LED na masztach", g, BraxRegionMeble.Swiecacy("Komis: naświetlacz LED (biały ciepły)", new Color(1f, .94f, .84f), 1.6f), false, false);
            var flagi = new[] { new Color(.78f, .08f, .08f), new Color(.95f, .95f, .93f), new Color(.98f, .78f, .1f), new Color(.08f, .16f, .42f) }.Select(c => new BraxRegionMeble.Bud()).ToArray();
            var kolF = new[] { new Color(.78f, .08f, .08f), new Color(.95f, .95f, .93f), new Color(.98f, .78f, .1f), new Color(.08f, .16f, .42f) };
            var punkty = new List<Vector3> { new Vector3(R.xMin + .6f + 6.06f * K, y + 2.6f * K + .05f, R.yMin + .5f + 2.44f * K) };
            foreach (float x in xs) punkty.Add(new Vector3(x, y + hm - .05f, zb));
            int nf = 0;
            for (int i = 0; i + 1 < punkty.Count; i++)
            {
                Vector3 a = punkty[i], b = punkty[i + 1]; float L = Vector3.Distance(a, b); int seg = Mathf.Max(2, Mathf.RoundToInt(L / .9f));
                Vector3 Q(float t) => Vector3.Lerp(a, b, t) - Vector3.up * (4f * t * (1f - t) * .06f * L);   // zwis linki ~6 % rozpiętości
                for (int s = 0; s < seg; s++) lin.Cyl(Q(s / (float)seg), Q((s + 1) / (float)seg), .006f * K, 4, -1f, false);
                for (float t = .04f; t < .97f; t += .45f / Mathf.Max(1f, L))
                {
                    var p0 = Q(t); var kier = (b - a); kier.y = 0f; kier.Normalize(); float w = .13f * K, h = .38f * K;
                    var A = p0 - kier * w; var B2 = p0 + kier * w; var C = p0 - Vector3.up * h;
                    var f = flagi[nf % flagi.Length]; f.Tri(A, B2, C); f.Tri(B2, A, C); nf++;
                }
            }
            lin.Obiekt("Komis: linka girlandy", g, stalM, false, false);
            for (int i = 0; i < flagi.Length; i++) flagi[i].Obiekt("Komis: chorągiewki girlandy", g, BraxCityMaterials.Surface("Komis: chorągiewka (folia PE) " + i, kolF[i], .6f), false, false);
            NdLog("komis aut przy T1 TRAMWAJOWA: aut na sprzedaż " + aut + "/" + KomisAuta.Length + (BraxMiastoAssety.Jest(KomisPaczka) ? "" : " (brak paczki " + KomisPaczka + " — plac bez aut)") + ", chorągiewek " + nf + ", masztów " + xs.Length);
        }
    }
}

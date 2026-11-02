using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ULICE OSIEDLA A I B — WYPOSAŻENIE, PARKINGI B, STREFY ZATRZYMANIA, KONTROLA (build 14, wykonawca „drogi”).
// Wołane raz z Build po „drzewa z chodników” (część A i B już stoją, drzewa przesunięte z nawierzchni), przed koronami
// drzew, trawą 3D i ruchem (auta czytają MiejscaParkingowe / Przejscia / SkrzyzowaniaInfo).
//   • część B: oznakowanie 2 cm nad asfaltem i bez cieni; parking dla klientów pawilonu (za sklepami, wjazd z ulicy N-S
//     z obniżonym krawężnikiem), podjazdy willi jako miejsca (przed garażem), przejścia i skrzyżowanie w danych, znaki D-6
//     (pytanie właściciela 20:59: „porobiłeś tam czy tylko pod A?” — ronda w B nie ma, decyzja 22:59);
//   • wyposażenie wg zdjęcia-wzorca Urban City: słupki przy przejściach, hydranty, przystanki z wiatą (A: Poznańska
//     w obie strony, B: przy klubie tenisowym) z ławką, koszem z klapą, znakiem D-15 i żółtą linią przystankową,
//     barierki U-12a przy przejściach do Parku Wiśniowego (dzieci), drzewa w żeliwnych kratach (rondo, plac pawilonu),
//     kosze z klapą przy parkingach i skrzyżowaniach ze światłami. Wszystko × K, z kolizją;
//   • komponent BraxStrefyDrogowe (rondo „ustąp”, przejścia bez świateł) i kontrola: nic nie stoi na jezdni, zatoki wolne,
//     ≥ 0.8 m od ścian (log BRAXHORN_DROGI_KONTROLA).
public static partial class BraxClaudeOsiedle
{
    static int zatokiB, podjazdyB, slupkiN, hydrantyN, przystankiN, barierkiN, koszeN, kratyN;
    const float YB = .03f;   // asfalt części B

    static void UliceOsiedla()
    {
        if (farbaOdroczonaLista.Count > 0) Guard("drogi A: oznakowanie w wycięciach (zaległe po błędzie w DrogiA)", () => FarbaWWycieciach());
        farbaOdroczona = false;
        var g0 = group;
        zatokiB = podjazdyB = slupkiN = hydrantyN = przystankiN = barierkiN = koszeN = kratyN = 0;
        int farbaB = PrzeniesFarbe("Osiedle B / ulice", GrupaFarbyB, .006f);
        rep.notes.Add("część B: oznakowanie poziome podniesione o 6 mm (2.2 cm nad asfaltem), bez cieni — " + farbaB + " trójkątów");
        Guard("drogi A: tablica PARK WIŚNIOWY z nowego chodnika", TablicaParkuA);
        Guard("drogi B: parking pawilonu", ParkingPawilonuB);
        Guard("drogi B: podjazdy willi", PodjazdyB);
        Guard("drogi B: przejścia i skrzyżowanie", PrzejsciaB);
        Guard("ulice: wyposażenie A i B", Wyposazenie);
        Guard("drogi: strefy zatrzymania (rondo, przejścia)", KomponentStref);
        Guard("drogi: kontrola", KontrolaDrog);
        Debug.Log("BRAXHORN_DROGI rondo=(0," + RondoZ + ") jezdnia r " + RondoOpaska + "–" + RondoJezdnia + " m, opaska z kostki, wyspa r " + RondoWyspa + " (kwiaty, krzewy, środek wolny na cokół), wloty=4 (P-13, A-7+C-12)"
                  + ", zebry=" + Przejscia.Count + " (ze światłami " + Przejscia.Count(p => p.zeSwiatlami) + ")"
                  + ", zatoki=" + MiejscaParkingowe.Count + " (A " + zatokiA + ", B parking " + zatokiB + ", podjazdy willi B " + podjazdyB + ")"
                  + ", sygnalizatory=" + (glowicePojazdy + glowicePiesi) + " (pojazdy " + glowicePojazdy + ", piesi " + glowicePiesi + ", skrzyżowania " + skrzyzowaniaSwiatla + ")"
                  + ", przesunięte_płoty=" + przesunietePloty + ", usunięte_płyty=" + usunietePlyty
                  + ", skrzyżowania=" + SkrzyzowaniaInfo.Count + ", znaki=" + znakiPostawione + ", słupki=" + slupkiN + ", hydranty=" + hydrantyN
                  + ", przystanki=" + przystankiN + ", barierki=" + barierkiN + ", kosze=" + koszeN + ", drzewa_w_kratach=" + kratyN);
        group = g0;
    }

    static string GrupaWyposazenia(Vector3 p) => p.z < FenceA + .5f ? "Osiedle B / wyposażenie ulic" : "Osiedle A / wyposażenie ulic";
    static bool Wolne(Vector3 p, float r, float y = .8f) => !Physics.CheckSphere(p + Vector3.up * y, r, ~0, QueryTriggerInteraction.Ignore);

    // Tablica PARK WIŚNIOWY (ParkA: słupki x −51.55 / −47.45, z −65.1) — lewy słupek wypadał na nowym chodniku parkingu przy
    // parku (x −52.8…−50.6): tablica przesunięta o 2 m na wschód (1 m od chodnika), ten sam wzór.
    static void TablicaParkuA()
    {
        const string gr = "Osiedle A / park Wiśniowy";
        var box = MinMax(-51.65f, -47.35f, .05f, 2.45f, -65.22f, -64.94f);
        int n = WytnijZGrupy((g, m) => g == gr, box);
        if (n == 0) { rep.notes.Add("tablica PARK WIŚNIOWY: nie znaleziona w " + gr); return; }
        PrzytnijKolizje(colliders, MinMax(-51.65f, -47.35f, -.05f, 2.45f, -65.22f, -64.94f), 0, true);
        var g0 = group; group = gr;
        foreach (float sx in new[] { -49.55f, -45.45f }) Box(Frame, new Vector3(sx, 1.15f, -65.1f), new Vector3(.1f, 2.3f, .1f));
        Sign("park_wisniowy", new Vector3(-47.5f, 1.95f, -65.04f), 4f, .8f, Vector3.forward);
        group = g0;
        rep.notes.Add("tablica PARK WIŚNIOWY przesunięta o 2 m na wschód (z nowego chodnika przy parkingu), " + n + " trójkątów wyciętych");
    }

    // ---------------- część B ----------------
    // Parking dla klientów pawilonu handlowego (pierzeja sklepów B): za budynkiem (x −13.3…26.2, z −162.4…−150.8), wjazd
    // z ulicy N-S. Rząd zatok od strony parku (przodem do alejki), alejka 6.6 m, za nią pas obsługi (drzwi serwisowe sklepów).
    static void ParkingPawilonuB()
    {
        group = "Osiedle B / parking pawilonu";
        float xE = AxisX + Half, x1 = 26.2f, z0 = -162.4f, z1 = -150.8f, e0 = -159.0f, e1 = -153.0f;
        // drzewa i łąka z miejsca parkingu (sadzone przy pawilonie przed tym krokiem)
        int drzewa = 0;
        foreach (var tr in trees.ToList())
        {
            if (tr == null || !tr.go || tr.at.x < xE - .5f || tr.at.x > x1 + .8f || tr.at.z < z0 - .8f || tr.at.z > z1 + .8f) continue;
            treeSpots.RemoveAll(q => Mathf.Abs(q.x - tr.at.x) < .01f && Mathf.Abs(q.z - tr.at.z) < .01f);
            Object.DestroyImmediate(tr.go); if (tr.trunk) Object.DestroyImmediate(tr.trunk); trees.Remove(tr); drzewa++;
        }
        lakaKand.RemoveAll(p => p.x > xE - .5f && p.x < x1 + .8f && p.y > z0 - .8f && p.y < z1 + .8f);
        // zjazd: krawężnik i chodnik ulicy N-S wycięte na 6 m, krawężnik obniżony, przez zieleń asfalt, chodnik obniżony
        float kx0 = AxisX + Carriage, kx1 = kx0 + Kerb, gx1 = kx1 + Green;
        var wyc = new Bounds(); wyc.SetMinMax(new Vector3(kx0 - .02f, -.1f, e0), new Vector3(xE + .02f, .5f, e1));
        WytnijZGrupy((gr, m) => gr == "Osiedle B / ulice" && (m == KerbStone || m == Pavers), wyc);
        PodzielKolizje(colliders, wyc);
        BoxMinMax(KerbStone, kx0, kx1, 0f, .045f, e0, e1, false);
        BoxMinMax(Asphalt, kx1, gx1, 0f, .05f, e0, e1, false);
        BoxMinMax(Pavers, gx1, xE, 0f, .07f, e0, e1);
        foreach (var (z, s) in new[] { (e0, 1f), (e1, -1f) })
        {
            var n = new Vector3(0, 0, s);
            Quad(Pavers, new Vector3(gx1, .07f, z), new Vector3(xE, .07f, z), new Vector3(xE, .14f, z), new Vector3(gx1, .14f, z), n);
            Quad(KerbStone, new Vector3(kx0, .045f, z), new Vector3(kx1, .045f, z), new Vector3(kx1, .16f, z), new Vector3(kx0, .16f, z), n);
        }
        // nawierzchnia, krawężniki (poza wjazdem), zatoki
        Top(Asphalt, xE, x1, z0, z1, YAsf);
        jezdnieKontrola.Add((xE, x1, z0, z1));
        BoxMinMax(KerbStone, xE, x1 + .2f, 0, .16f, z0 - .2f, z0);
        BoxMinMax(KerbStone, xE, x1 + .2f, 0, .16f, z1, z1 + .2f);
        BoxMinMax(KerbStone, x1, x1 + .2f, 0, .16f, z0, z1);
        BoxMinMax(KerbStone, xE, xE + .2f, 0, .16f, z0, e0); BoxMinMax(KerbStone, xE, xE + .2f, 0, .16f, e1, z1);
        int n0 = MiejscaParkingowe.Count;
        zatokiB += RzadZatokX(xE + 4f, x1 - .5f, z1, -1f, false);
        // latarnie od strony pasa obsługi, znak D-18 przy wjeździe (dla jadących ulicą N-S na północ), kosz z klapą
        foreach (float lx in new[] { -6f, 8f, 22f }) { var p = new Vector3(lx, 0, z0 - .55f); if (Wolne(p, .35f, 1.5f)) Lamp(p, 0f); }
        ZnakPionowy(new Vector3(AxisX + Carriage + Kerb + Green * .5f, 0, e0 - .8f), Vector3.back, "D-18");
        KoszZKlapa(new Vector3(xE - 1.2f, .14f, e1 + 1.0f), 90f);
        rep.notes.Add("parking pawilonu B: " + zatokiB + " zatok (" + (MiejscaParkingowe.Count - n0) + " w danych), wjazd z ulicy N-S z obniżonym krawężnikiem, usunięte drzewa z miejsca parkingu: " + drzewa);
    }
    // Kolizja ciągła (krawężnik, chodnik) przecięta wjazdem: podział na dwie części poza wycięciem.
    static void PodzielKolizje(Transform korzen, Bounds v)
    {
        foreach (var c in korzen.GetComponentsInChildren<BoxCollider>(true).ToList())
        {
            if (!c || !c.enabled || c.gameObject.name != "kolizja" || Quaternion.Angle(c.transform.rotation, Quaternion.identity) > .5f) continue;
            var b = c.bounds;
            if (!(b.min.z < v.min.z && b.max.z > v.max.z && b.max.x > v.min.x && b.min.x < v.max.x && b.max.y > v.min.y && b.min.y < v.max.y)) continue;
            foreach (var (q0, q1) in new[] { (b.min.z, v.min.z), (v.max.z, b.max.z) })
                Collider(new Vector3(b.center.x, b.center.y, (q0 + q1) * .5f), new Vector3(b.size.x, b.size.y, q1 - q0));
            Object.DestroyImmediate(c.gameObject);
        }
    }

    // Podjazdy willi B (jodełka przed garażem — Villa: garaż g0…g1 wzdłuż ulicy, drzwi garażu 7 m od frontu działki):
    // jedno miejsce na podjeździe, przodem do garażu, „przy ścianie” (brama garażu 0.7 m przed autem).
    static void PodjazdyB()
    {
        float north = StreetZ + Half, south = StreetZ - Half;
        var wille = new List<(int idx, float u0, float u1, float front, float dir)>();
        int idx = 0; float w = (AxisX - Half - (X0 + .6f)) / 3f;
        for (int k = 0; k < 3; k++) wille.Add((idx++, X0 + .6f + k * w, X0 + .6f + (k + 1) * w, north, 1f));
        float shopEnd = AxisX + Half + 62f; w = (X1 - .6f - (shopEnd + .6f)) / 2f;
        for (int k = 0; k < 2; k++) wille.Add((idx++, shopEnd + .6f + k * w, shopEnd + .6f + (k + 1) * w, north, 1f));
        float pw = (X1 - X0 - 1.2f) / 6f;
        for (int k = 0; k < 6; k++) { if (k == 3) continue; float a = X0 + .6f + k * pw; wille.Add((idx++, a, a + pw, south, -1f)); }
        foreach (var (i, u0, u1, front, dir) in wille)
        {
            float W = u1 - u0, Wm = Mathf.Min(W - 13f, 18f), s0 = (W - (Wm + 6.5f)) * .5f, g0 = s0 + Wm, g1 = g0 + 6.5f;
            bool mirror = i % 2 == 1; float ac = (g0 + g1) * .5f, x = mirror ? u1 - ac : u0 + ac;
            Zatoka(new Vector3(x, .04f, front + dir * 3.8f), new Vector3(0, 0, dir), true); podjazdyB++;
        }
    }

    // Przejścia B (dwie zebry Opusa: przez ulicę N-S przy skrzyżowaniu i przez ulicę główną przy pawilonie), skrzyżowanie,
    // znaki D-6, strefy dla aut.
    static void PrzejsciaB()
    {
        void Zebra(Vector3 c, Vector3 wzdluz, float polSzer)
        {
            var wp = new Vector3(wzdluz.z, 0, wzdluz.x);
            Przejscia.Add(new Przejscie { srodek = c, wzdluzJezdni = wzdluz, szerokoscJezdni = 2f * Carriage, zeSwiatlami = false });
            zebry.Add(new ZebraA { c = c, wzdluz = wzdluz, wpoprzek = wp, polSzer = polSzer, polDl = Carriage, kraw = Carriage + Kerb, zielen = Green, swiatla = false });
            int zi = zebryProstokaty.Count;
            var pol = new Vector3(Mathf.Abs(wzdluz.x) * (polSzer + .2f) + Mathf.Abs(wp.x) * (Carriage + Kerb + .8f), 1.2f, Mathf.Abs(wzdluz.z) * (polSzer + .2f) + Mathf.Abs(wp.z) * (Carriage + Kerb + .8f));
            zebryProstokaty.Add((c + Vector3.up * 1.0f, pol));
            foreach (float sg in new[] { 1f, -1f })
            {
                var t = -wzdluz * sg;                          // kierunek jazdy do zebry
                var right = new Vector3(t.z, 0, -t.x);         // prawa strona jadącego
                strefyZebr.Add((c - t * (polSzer + 3.3f) + right * (Carriage * .5f), t, zi));
                var p = c - t * (polSzer + .6f) + right * (Carriage + Kerb + Green * .5f); p.y = 0f;
                if (Mathf.Abs(p.z - StreetZ) < Carriage + Kerb + .3f) p.z = StreetZ + Carriage + Kerb + .35f;   // przed zebrą przy skrzyżowaniu: narożnik
                if (!Wolne(p, .3f, 1.5f)) p -= t * .9f;
                ZnakPionowy(p, -t, "D-6");
            }
        }
        jezdnieKontrola.Add((X0 + .6f, X1 - .6f, StreetZ - Carriage, StreetZ + Carriage));
        jezdnieKontrola.Add((AxisX - Carriage, AxisX + Carriage, StreetZ + Carriage, ParkZ0));
        Zebra(new Vector3(AxisX, YB, StreetZ + Carriage + 1.7f), Vector3.forward, 1.5f);
        Zebra(new Vector3(AxisX + 10.5f, YB, StreetZ), Vector3.right, 1.5f);
        SkrzyzowaniaInfo.Add(new SkrzyzowanieInfo { srodek = new Vector3(AxisX, YB, StreetZ), promien = Half, rodzaj = RodzajSkrzyzowania.Zwykle });
        OsieJezdni.Add(new OsJezdni { a = new Vector3(X0 + .6f, YB, StreetZ), b = new Vector3(X1 - .6f, YB, StreetZ), polowaJezdni = Carriage, id = "B-główna" });
        OsieJezdni.Add(new OsJezdni { a = new Vector3(AxisX, YB, StreetZ + Carriage), b = new Vector3(AxisX, YB, ParkZ0), polowaJezdni = Carriage, id = "B-N-S" });
    }

    // ---------------- wyposażenie ----------------
    static void Wyposazenie()
    {
        Physics.SyncTransforms();
        // 1) słupki przy każdej zebrze (po dwa na każdym końcu, na chodniku za krawężnikiem)
        foreach (var z in zebry.Where(q => !ParkowaZebra(q)))   // przy wejściu do parku barierki zamiast słupków
            foreach (int sd in new[] { 1, -1 })
                foreach (int q in new[] { 1, -1 })
                {
                    var p = z.c + z.wpoprzek * sd * (z.kraw + Mathf.Max(.35f, Mathf.Min(z.zielen, .6f) * .5f + .1f)) + z.wzdluz * q * (z.polSzer + .3f); p.y = 0f;
                    if (Slupek(p)) slupkiN++;
                }
        // 2) hydranty naziemne (co ~100 m, w pasie zieleni przy krawężniku)
        var hydr = new List<Vector3>
        {
            new Vector3(-30f, 0, -19f + 3.5f + .2f + .3f), new Vector3(30f, 0, -19f - 3.5f - .2f - .3f),
            new Vector3(-28f, 0, RondoZ + 3.5f + .2f + .3f), new Vector3(28f, 0, RondoZ - 3.5f - .2f - .2f),
            new Vector3(-32f, 0, 86f + 3.5f + .2f + .3f), new Vector3(32f, 0, 86f - 3.5f - .2f - .3f),
            new Vector3(4.5f + .2f + .15f, 0, 2f), new Vector3(-4.5f - .2f - .15f, 0, 66f),
            new Vector3(-58f + 3.35f, 0, 10f), new Vector3(58f - 3.35f, 0, 25f), new Vector3(58f + 3.35f, 0, -40f), new Vector3(-58f - 3.35f, 0, 62f),
            new Vector3(-70f, 0, StreetZ + Carriage + Kerb + Green * .5f), new Vector3(12f, 0, StreetZ - Carriage - Kerb - Green * .5f),
            new Vector3(82f, 0, StreetZ + Carriage + Kerb + Green * .5f), new Vector3(AxisX + Carriage + Kerb + Green * .5f, 0, -172f),
        };
        foreach (var h in hydr)
        {
            Vector3 p = h; bool ok = false;
            foreach (float da in new[] { 0f, 2f, -2f, 4f, -4f })
            {
                var q = p + (Mathf.Abs(p.z - StreetZ) < 8f || Mathf.Abs(p.z + 19f) < 8f || Mathf.Abs(p.z - RondoZ) < 8f || Mathf.Abs(p.z - 86f) < 8f ? Vector3.right : Vector3.forward) * da;
                if (Wolne(q, .35f, .6f) && Wolne(q, .3f, 1.6f)) { p = q; ok = true; break; }
            }
            if (ok) { Hydrant(p); hydrantyN++; }
        }
        // 3) przystanki autobusowe z wiatą (A: Poznańska w obie strony, B: ulica główna przy klubie tenisowym)
        Przystanek(new Vector3(8.4f, 0, 9.8f), Vector3.left, new Vector3(4.85f, 0, 15.6f), Vector3.back, 3.8f, 4.45f, 2f, 17f, false);
        Przystanek(new Vector3(-8.4f, 0, 2.6f), Vector3.right, new Vector3(-4.85f, 0, -3.2f), Vector3.forward, -4.45f, -3.8f, -6f, 9f, false);
        Przystanek(new Vector3(10f, 0, StreetZ - Half - 1.0f), Vector3.forward, new Vector3(14.2f, 0, StreetZ - Carriage - Kerb - Green * .5f), Vector3.left, StreetZ - 3.35f, StreetZ - 2.7f, 1f, 17f, true);
        // 4) barierki U-12a przy przejściach do Parku Wiśniowego (wejście z parkingów — dzieci z parku)
        foreach (var z in zebry.Where(ParkowaZebra))
            foreach (int sd in new[] { 1, -1 })
                foreach (int q in new[] { 1, -1 })
                {
                    var a = z.c + z.wpoprzek * sd * (z.kraw + z.zielen + .2f) + z.wzdluz * q * (z.polSzer + .45f); a.y = 0f;   // krawędź chodnika (znaki D-6 stoją w zieleni)
                    var b = a + z.wzdluz * q * 2.4f;
                    if (b.z < -61.8f) b.z = -61.8f;
                    if ((b - a).magnitude > .8f) { Barierka(a, b); barierkiN++; }
                }
        // 5) kosze z klapą: przy wjazdach na parkingi, przy skrzyżowaniach ze światłami
        var kosze = new List<Vector3>();
        foreach (float s in new[] { -1f, 1f }) { kosze.Add(new Vector3(s * 66.4f, .1f, -4.15f)); kosze.Add(new Vector3(s * 66.4f, .1f, 46.1f)); kosze.Add(new Vector3(s * 62.4f, .14f, -60.4f)); kosze.Add(new Vector3(s * 51.7f, .14f, -96.9f)); }
        kosze.Add(new Vector3(9.6f, .14f, 91.9f)); kosze.Add(new Vector3(-9.6f, .14f, 80.1f)); kosze.Add(new Vector3(-9.6f, .14f, -13.1f)); kosze.Add(new Vector3(9.6f, .14f, -13.1f));
        foreach (var p in kosze) KoszZKlapa(p, Mathf.Atan2(-p.x, 0f) * Mathf.Rad2Deg);
        // 6) drzewa w żeliwnych kratach: chodnik ronda (między latarniami i znakami), plac przed pawilonem B
        for (int k = 0; k < 4; k++)
            foreach (float da in new[] { 30f, 60f })
            {
                float t = (k * 90f + da) * Mathf.Deg2Rad;
                KrataZDrzewem(new Vector3(Mathf.Cos(t) * 14.3f, .14f, RondoZ + Mathf.Sin(t) * 14.3f), 7.0f);
            }
        foreach (float x in new[] { 3.7f, 12.7f, 21.7f }) KrataZDrzewem(new Vector3(x, .14f, StreetZ + Half + 1.0f), 6.6f);
    }

    static bool ParkowaZebra(ZebraA z) => Mathf.Abs(Mathf.Abs(z.c.x) - 58f) < .5f && z.c.z < -55f;
    static bool Slupek(Vector3 p)
    {
        float k = Ks; p.y = 0f;
        if (!Wolne(p, .22f, .6f) || OnRoad(p)) return false;
        var g = group; group = GrupaWyposazenia(p);
        var ant = Plain("słupek drogowy antracyt", new Color(.16f, .17f, .18f), .5f, .4f);
        float h = .95f * k, r = .05f * k;
        Cylinder(ant, p, r, h - .03f, 12, false, true);
        Cylinder(ant, p + Vector3.up * (h - .03f), r * .8f, .03f, 12, true, false);
        Cylinder(ZnBialy, p + Vector3.up * (h - .2f * k), r + .004f, .05f * k, 12, false, false);
        Cylinder(ZnBialy, p + Vector3.up * (h - .33f * k), r + .004f, .05f * k, 12, false, false);
        group = g; return true;
    }
    static bool OnRoad(Vector3 p) => jezdnieKontrola.Any(j => p.x > j.x0 - .05f && p.x < j.x1 + .05f && p.z > j.z0 - .05f && p.z < j.z1 + .05f);

    // Hydrant naziemny (polski wzór, czerwony, ~0.95 m): kołnierz, korpus, pierścień, głowica, pokrywa z nakrętką,
    // dwie nasady boczne z pokrywami.
    static void Hydrant(Vector3 p)
    {
        float k = Ks; p.y = 0f;
        var g = group; group = GrupaWyposazenia(p);
        var red = Plain("hydrant czerwony lakier", new Color(.72f, .07f, .05f), .55f, .2f);
        Cylinder(red, p, .17f * k, .06f * k, 14, true, false);
        Cylinder(red, p + Vector3.up * .06f * k, .1f * k, .62f * k, 14, false, false);
        Cylinder(red, p + Vector3.up * .68f * k, .125f * k, .05f * k, 14, true, false);
        Cylinder(red, p + Vector3.up * .73f * k, .095f * k, .14f * k, 14, false, false);
        Cylinder(red, p + Vector3.up * .87f * k, .07f * k, .04f * k, 12, true, false);
        Cylinder(Steel, p + Vector3.up * .91f * k, .025f * k, .04f * k, 5, true, false);
        foreach (var d in new[] { Vector3.right, Vector3.left })
        {
            var o = p + Vector3.up * .79f * k;
            Tube(red, o + d * .08f * k, o + d * .17f * k, .04f * k, 10);
            Tube(Steel, o + d * .17f * k, o + d * .19f * k, .048f * k, 10);
        }
        Collider(p + Vector3.up * .48f * k, new Vector3(.38f * k, .96f * k, .38f * k));
        group = g;
    }

    // Barierka U-12a (ocynk, 1.0 m): słupki co ~2 m, pochwyt, pręt środkowy, pionowe pręty; kolizja wzdłuż.
    static void Barierka(Vector3 a, Vector3 b)
    {
        float k = Ks; a.y = 0f; b.y = 0f;
        var d = b - a; float len = d.magnitude; if (len < .5f) return; d /= len;
        var g = group; group = GrupaWyposazenia(a);
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
        for (int i = 0; i <= n; i++) Cylinder(Ocynk, a + d * (len * i / n), .03f * k, 1.0f * k, 8, true, false);
        Tube(Ocynk, a + Vector3.up * 1.0f * k, b + Vector3.up * 1.0f * k, .025f * k, 8);
        Tube(Ocynk, a + Vector3.up * .5f * k, b + Vector3.up * .5f * k, .018f * k, 6);
        Tube(Ocynk, a + Vector3.up * .12f * k, b + Vector3.up * .12f * k, .018f * k, 6);
        for (float t = .16f; t < len - .1f; t += .16f) Tube(Ocynk, a + d * t + Vector3.up * .12f * k, a + d * t + Vector3.up * 1.0f * k, .008f * k, 4);
        ColliderRot((a + b) * .5f + Vector3.up * .5f * k, new Vector3(.1f, 1.0f * k, len), Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
        Hard(Mathf.Min(a.x, b.x) - .1f, Mathf.Max(a.x, b.x) + .1f, Mathf.Min(a.z, b.z) - .1f, Mathf.Max(a.z, b.z) + .1f);
        group = g;
    }

    // Kosz uliczny z klapą (stal lakierowana grafit-zieleń): korpus, pokrywa, otwór z klapą wrzutową na zawiasie, stopa.
    static void KoszZKlapa(Vector3 p, float yaw)
    {
        float k = Ks; var q = Quaternion.Euler(0, yaw, 0); var F = q * Vector3.forward;
        if (!Wolne(new Vector3(p.x, 0, p.z), .32f, .5f) || OnRoad(p)) return;
        var g = group; group = GrupaWyposazenia(p);
        var lak = Plain("kosz uliczny lakier grafitowo-zielony", new Color(.11f, .19f, .15f), .5f, .35f);
        OBox(Frame, p + Vector3.up * .03f * k, new Vector3(.4f * k, .06f * k, .3f * k), yaw);
        OBox(lak, p + Vector3.up * .39f * k, new Vector3(.46f * k, .66f * k, .36f * k), yaw, true);
        OBox(lak, p + Vector3.up * .745f * k, new Vector3(.5f * k, .05f * k, .4f * k), yaw);
        OBox(ZnCzarny, p + Vector3.up * .56f * k + F * (.18f * k + .004f), new Vector3(.34f * k, .16f * k, .008f), yaw);           // otwór wrzutowy
        OBox(lak, p + Vector3.up * .6f * k + F * (.18f * k + .03f), new Vector3(.36f * k, .1f * k, .012f), yaw);                     // klapa (uchylona do góry)
        OBox(Frame, p + Vector3.up * .655f * k + F * (.185f * k), new Vector3(.38f * k, .022f * k, .022f * k), yaw);                  // zawias
        group = g; koszeN++;
    }

    // Przystanek: wiata (stal + szkło, dach), posadzka z płyt połączona z chodnikiem, ławka, rozkład jazdy, kosz z klapą,
    // znak D-15 przy krawężniku, żółta linia przystankowa P-17 na pasie (d0..d1 w poprzek — x albo z, s0..s1 wzdłuż).
    static void Przystanek(Vector3 c, Vector3 f, Vector3 znak, Vector3 znakLico, float d0, float d1, float s0, float s1, bool wzdluzX)
    {
        float k = Ks, w = 3.2f * k, d = 1.5f * k, h = 2.45f * k;
        c.y = 0f; var R = Vector3.Cross(Vector3.up, f).normalized; float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        var test = c + Vector3.up * 1.3f;
        if (Physics.CheckBox(test, new Vector3(w * .5f, .9f, d * .5f), Quaternion.Euler(0, yaw, 0), ~0, QueryTriggerInteraction.Ignore)) { rep.notes.Add("przystanek przy " + c.ToString("F1") + ": miejsce zajęte — pominięty"); return; }
        var g = group; group = GrupaWyposazenia(c);
        OBox(Pavers, c + f * .25f + Vector3.up * .06f, new Vector3(w + 1.6f * k, .12f, d + .5f), yaw);   // 2 cm pod chodnikiem tam, gdzie się z nim styka
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
                Cylinder(Frame, c + R * sx * (w * .5f - .05f) + f * sz * (d * .5f - .05f) + Vector3.up * .12f, .04f * k, h - .12f, 10, false, false);
        OBox(Frame, c + Vector3.up * (h + .05f * k) + f * .12f * k, new Vector3(w + .3f * k, .1f * k, d + .4f * k), yaw);
        OBox(ZnBialy, c + Vector3.up * (h + .005f) + f * (d * .5f + .315f * k), new Vector3(w + .3f * k, .09f * k, .02f), yaw);   // listwa czołowa
        OBox(GlassClear, c - f * (d * .5f - .05f) + Vector3.up * (.4f * k + .975f * k), new Vector3(w - .1f, 1.95f * k, .02f), yaw);
        foreach (int sx in new[] { -1, 1 }) OBox(GlassClear, c + R * sx * (w * .5f - .05f) - f * .1f * k + Vector3.up * (.4f * k + .975f * k), new Vector3(.02f, 1.95f * k, d * .75f), yaw);
        OBox(Frame, c - f * (d * .5f - .05f) + Vector3.up * .37f * k, new Vector3(w - .1f, .06f * k, .05f), yaw);
        OBox(ZnBialy, c + R * (w * .5f - .09f) - f * .1f * k + Vector3.up * 1.45f * k, new Vector3(.015f, .6f * k, .45f * k), yaw);   // rozkład jazdy
        ColliderRot(c - f * (d * .5f - .05f) + Vector3.up * 1.3f, new Vector3(w, 2.0f, .1f), yaw);
        foreach (int sx in new[] { -1, 1 }) ColliderRot(c + R * sx * (w * .5f - .05f) - f * .1f * k + Vector3.up * 1.3f, new Vector3(.1f, 2.0f, d * .75f), yaw);
        Bench(c - f * (d * .18f) + Vector3.up * .12f, yaw);
        KoszZKlapa(c + R * (w * .5f + .5f * k) + f * .2f + Vector3.up * .12f, yaw);
        group = g;
        ZnakPionowy(znak, znakLico, "D-15");
        // P-17: żółta linia zygzakowata wzdłuż krawężnika na pasie przystanku
        var zolta = Plain("farba żółta (linia przystankowa)", new Color(.95f, .72f, .05f), .3f);
        float y = wzdluzX ? YB + .022f : YFarba;
        var gg = group; group = wzdluzX ? GrupaFarbyB : GrupaFarbyA;
        for (float s = s0; s + 1f <= s1; s += 1f)
        {
            float a0 = (Mathf.RoundToInt(s - s0) % 2 == 0) ? d0 : d1, a1 = a0 == d0 ? d1 : d0;
            Vector3 P(float along, float across) => wzdluzX ? new Vector3(along, y, across) : new Vector3(across, y, along);
            var pa = P(s, a0); var pb = P(s + 1f, a1); var dir = (pb - pa).normalized; var nn = Vector3.Cross(Vector3.up, dir) * .06f;
            Quad(zolta, pa - nn, pb - nn, pb + nn, pa + nn, Vector3.up);
        }
        group = gg;
        przystankiN++;
    }

    // Drzewo w żeliwnej kracie 1.2 × 1.2 m na chodniku/placu (krata 2 cm nad płytami, żebra 2 cm, otwór na pień).
    static void KrataZDrzewem(Vector3 p, float wys)
    {
        float k = Ks, a = .6f * k, y0 = p.y;
        if (!Wolne(new Vector3(p.x, 0, p.z), .8f, 1.6f) || OnRoad(p)) return;
        var g = group; group = GrupaWyposazenia(p);
        var zeliwo = Plain("krata drzewa żeliwo", new Color(.09f, .09f, .095f), .35f, .6f);
        BoxMinMax(zeliwo, p.x - a, p.x + a, y0, y0 + .02f, p.z - a, p.z + a, false);
        foreach (float r in new[] { .55f * k, .38f * k })
        {
            BoxMinMax(zeliwo, p.x - r, p.x + r, y0 + .02f, y0 + .04f, p.z - r, p.z - r + .025f, false); BoxMinMax(zeliwo, p.x - r, p.x + r, y0 + .02f, y0 + .04f, p.z + r - .025f, p.z + r, false);
            BoxMinMax(zeliwo, p.x - r, p.x - r + .025f, y0 + .02f, y0 + .04f, p.z - r, p.z + r, false); BoxMinMax(zeliwo, p.x + r - .025f, p.x + r, y0 + .02f, y0 + .04f, p.z - r, p.z + r, false);
        }
        for (int q = 0; q < 8; q++) { float t = q * Mathf.PI / 4f; var d = new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)); OBox(zeliwo, p + d * .4f * k + Vector3.up * .03f, new Vector3(.02f, .02f, .32f * k), Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg); }
        Cylinder(Mulch, p + Vector3.up * .02f, .2f * k, .021f, 16, true, false);
        group = "Osiedle " + (p.z < FenceA + .5f ? "B" : "A") + " / drzewa w kratach";
        if (Tree(new Vector3(p.x, y0, p.z), wys, p.x * 17f + p.z * 3f)) kratyN++;
        group = g;
    }

    // ---------------- strefy zatrzymania (rondo, przejścia bez świateł) ----------------
    static void KomponentStref()
    {
        var go = new GameObject("Strefy zatrzymania (rondo, przejścia bez świateł)"); go.transform.SetParent(Group("Osiedle A / drogi od nowa"), false);
        var s = go.AddComponent<BraxStrefyDrogowe>();
        s.rondoSrodek = strefyRondo.Select(q => q.s).ToArray(); s.rondoKierunek = strefyRondo.Select(q => q.k).ToArray(); s.rondoKontrola = strefyRondo.Select(q => q.kontrola).ToArray();
        s.zebraSrodek = zebryProstokaty.Select(q => q.c).ToArray(); s.zebraPolowa = zebryProstokaty.Select(q => q.pol).ToArray();
        s.stopSrodek = strefyZebr.Select(q => q.s).ToArray(); s.stopKierunek = strefyZebr.Select(q => q.k).ToArray(); s.stopZebra = strefyZebr.Select(q => q.zebra).ToArray();
        rep.notes.Add("strefy zatrzymania: rondo " + strefyRondo.Count + " wloty, przejścia bez świateł " + zebryProstokaty.Count + " (" + strefyZebr.Count + " stref dla aut)");
    }

    // ---------------- kontrola ----------------
    static string Sciezka(Transform t) { var s = t.name; for (var p = t.parent; p && s.Length < 90; p = p.parent) s = p.name + "/" + s; return s; }
    static void KontrolaDrog()
    {
        Physics.SyncTransforms();
        var buf = new Collider[48];
        int naJezdni = 0; var nazwy = new List<string>();
        foreach (var (x0, x1, z0, z1) in jezdnieKontrola)
        {
            var c = new Vector3((x0 + x1) * .5f, 1.35f, (z0 + z1) * .5f); var half = new Vector3(Mathf.Max(.05f, (x1 - x0) * .5f - .15f), 1.0f, Mathf.Max(.05f, (z1 - z0) * .5f - .15f));
            int n = Physics.OverlapBoxNonAlloc(c, half, buf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var col = buf[i]; if (!col || !col.enabled) continue; naJezdni++; if (nazwy.Count < 12) nazwy.Add(Sciezka(col.transform) + " @" + col.bounds.center.ToString("F1")); }
        }
        int zajete = 0, ciasne = 0, sprawdzone = 0;
        foreach (var mp in MiejscaParkingowe)
        {
            if (mp.przyScianie && mp.srodek.y < .05f) continue;   // podjazdy willi (garaż przed autem z założenia)
            sprawdzone++;
            var q = Quaternion.LookRotation(mp.przod, Vector3.up);
            int n = Physics.OverlapBoxNonAlloc(mp.srodek + Vector3.up * 1.0f, new Vector3(mp.rozmiar.x * .5f - .2f, .6f, mp.rozmiar.y * .5f - .2f), buf, q, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (buf[i] && buf[i].enabled) { zajete++; break; }
            bool blisko = false;
            foreach (float e in new[] { 1f, -1f })
            {
                var kon = mp.srodek + mp.przod * e * (mp.rozmiar.y * .5f + .4f) + Vector3.up * 1.4f;
                n = Physics.OverlapBoxNonAlloc(kon, new Vector3(mp.rozmiar.x * .5f - .3f, .8f, .38f), buf, q, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n && !blisko; i++) { var b = buf[i]; if (b && b.enabled && b.bounds.size.y > 2.5f && (b.bounds.size.x > 1.5f || b.bounds.size.z > 1.5f)) blisko = true; }
            }
            if (blisko) ciasne++;
        }
        Debug.Log("BRAXHORN_DROGI_KONTROLA jezdnia_przeszkody=" + naJezdni + (nazwy.Count > 0 ? " [" + string.Join("; ", nazwy) + "]" : "") + ", zatoki_sprawdzone=" + sprawdzone + ", zatoki_zajete=" + zajete + ", zatoki_sciana_blizej_0.8m=" + ciasne);
        rep.notes.Add("kontrola dróg: przeszkody na jezdniach " + naJezdni + ", zatoki zajęte " + zajete + "/" + sprawdzone + ", ściana bliżej 0.8 m: " + ciasne);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// RUCH AUT W MIEŚCIE — ETAP 2 (wykonawca B, 2.11; właściciel 15:12: „chociaż parę aut, nie musi być dużo, żeby nie było tłoczno —
// coś się dzieje”; jadą po głównych ulicach i stają na czerwonym). NOWY PLIK (pliki osiedla bez zmian): ta sama klasa częściowa,
// więc woła gotowy system aut osiedla 14 — modele i szablony (AutaModele, PrzygotujAuto, lakiery), trasy (BraxTrasyAut.Generuj:
// łuki skrętów, rondo przeciwnie do wskazówek zegara, migacze), zarządca BraxRuchAut, auta BraxAuto, symulacja i test BraxRuchAutSym.
// Sieć miasta B: ulica główna (rondo ↔ skrzyżowanie po wiadukcie), Aleja Południowa (rondo ↔ Warsztatowa), Aleja Północna z pętlą
// (rondo PÓŁNOC — zawracanie), ul. Warsztatowa, ul. Wiaduktowa. Skrzyżowania ze światłami (J1, J2): auta stają w strefach
// zatrzymania BraxSwiatloDrogowe (rodzaj = 2: bez prawej ręki, decyduje sygnalizacja). Trasy zamknięte (pętle) — auta nie
// znikają na oczach gracza. Ulica główna na wschód od skrzyżowania po wiadukcie — zakres A (bez aut w tym etapie).
// ETAP 2: czwarta pętla przez ul. Zachodnią z zawracaniem na pętli ZACHÓD (oś ulicy głównej przedłużona na zachód).
// Wydajność: 8 aut w ruchu (LOD znika przy ~1.5 % ekranu), symulacja BraxRuchAut jak na osiedlu.
public static partial class BraxClaudeOsiedle
{
    public static void RuchMiastaB(Transform cityRoot)
    {
        var repPrev = rep; var rootPrev = root; var colPrev = colliders; var groupPrev = group;
        float rpPrev = BraxTrasyAut.RondoPromien, rjPrev = BraxTrasyAut.RondoJezdnia; var lukPrev = BraxTrasyAut.OdsuniecieZebr;
        try
        {
            Directory.CreateDirectory(Out); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            rep = new Report(); groups.Clear(); group = "Miasto B: ruch aut";
            var stary = cityRoot.Find("CLAUDE miasto B: ruch aut"); if (stary) UnityEngine.Object.DestroyImmediate(stary.gameObject);
            root = new GameObject("CLAUDE miasto B: ruch aut").transform; root.SetParent(cityRoot, false); root.position = Vector3.zero;
            // NIEPODZIELNY przy podziale miasta na kwadraty 150 m (BraxGymBuild → BraxClaudeObszary.WObszarze: poddrzewo z LODGroup
            // jest całe w obszarze albo całe zostaje): zarządca BraxRuchAut odwołuje się do aut — rozdzielone między sceny straciłyby
            // odwołania (auta stałyby w miejscu). Pusta LODGroup (bez poziomów) niczego nie ukrywa.
            root.gameObject.AddComponent<LODGroup>();
            colliders = new GameObject("kolizje").transform; colliders.SetParent(root, false);
            lakiery.Clear(); szablonSwiatla.Clear(); quadSwiatla = null; matStop = matMigacz = matDym = null; autoSiatki = 0;
            var modele = AutaModele();
            if (modele.Count == 0) { Debug.LogWarning("BRAXHORN_RUCH_MIASTO_B brak modeli aut (" + AutaDir + ") — miasto bez ruchu"); return; }
            var tmp = new GameObject("auta — szablony (tymczasowe)").transform;
            var szablony = new List<AutoSzablon>();
            foreach (var src in modele)
            {
                if (AutaPominiete.Contains(src.name)) continue;
                try { var s = PrzygotujAuto(src, tmp); if (s != null) szablony.Add(s); } catch (Exception e) { Debug.LogError("BRAXHORN_RUCH_MIASTO_B model " + src.name + ": " + e.Message); }
            }
            if (szablony.Count > 0) AutaMiastaB(szablony);
            if (szablony.Count > 0) ParkingMiastaB(szablony);
            if (szablony.Count > 0) { try { WarsztatyMiastaB(szablony); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_WARSZTATY auta i koła: " + e); } }
            UnityEngine.Object.DestroyImmediate(tmp.gameObject);
            foreach (var n in rep.notes) Debug.Log("BRAXHORN_RUCH_MIASTO_B " + n);
        }
        finally
        {
            rep = repPrev; root = rootPrev; colliders = colPrev; group = groupPrev;
            BraxTrasyAut.RondoPromien = rpPrev; BraxTrasyAut.RondoJezdnia = rjPrev; BraxTrasyAut.OdsuniecieZebr = lukPrev;
            groups.Clear();
        }
    }

    // Auta zaparkowane miasta B: stanowiska parkingu przy warsztatach (BraxRegionMiastoB.MiejscaParkingu) przez gotowy
    // AutaZaparkowane osiedla (zajętość ~55 %, kontrola kolizji, bez świateł i spalin) — wspólna lista MiejscaParkingowe
    // podmieniona na czas wywołania i przywrócona.
    static void ParkingMiastaB(List<AutoSzablon> szablony)
    {
        var miejsca = BraxRegionMiastoB.MiejscaParkingu;
        if (miejsca.Count == 0) { Debug.Log("BRAXHORN_RUCH_MIASTO_B parking: brak stanowisk (krok miasta B nie zbudował parkingu)"); return; }
        var zapas = MiejscaParkingowe.ToList();
        var g = group;
        // 12.11 (właściciel 01:30 „motory globalnie”, 02:05 „dokończ”): 3 stanowiska na motocykle właściciela (2 + 2 + 1) — pierwsze,
        // środkowe i ostatnie z listy (parking warsztatów i supermarketu); bez paczki stanowiska wracają do aut
        var dlaMotorow = new HashSet<int>();
        if (BraxMiastoMotory.SaMotory && miejsca.Count >= 6)
        {
            var gm = new GameObject("CLAUDE miasto B: motocykle na parkingu").transform; gm.SetParent(root.parent ? root.parent : root, false); gm.position = Vector3.zero;
            int nr = 0, motorow = 0;
            foreach (int i in new[] { 0, miejsca.Count / 2, miejsca.Count - 1 })
            {
                if (!dlaMotorow.Add(i)) continue;
                int ile = nr < 2 ? 2 : 1; var m = miejsca[i];
                motorow += BraxMiastoMotory.NaStanowisku(gm, m.c, m.przod, m.rozmiar, nr * 2, ile); nr++;
            }
            Debug.Log("BRAXHORN_RUCH_MIASTO_B motocykle właściciela na parkingu: " + motorow + " na " + dlaMotorow.Count + " stanowiskach");
        }
        try
        {
            MiejscaParkingowe.Clear();
            for (int i = 0; i < miejsca.Count; i++)
            {
                if (dlaMotorow.Contains(i)) continue;
                var m = miejsca[i]; MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = m.c, przod = m.przod, rozmiar = m.rozmiar, przyScianie = false });
            }
            group = "Miasto B: auta zaparkowane";
            AutaZaparkowane(szablony);
        }
        finally { MiejscaParkingowe.Clear(); MiejscaParkingowe.AddRange(zapas); group = g; }
    }

    // WARSZTATY UL. WARSZTATOWEJ (9.11, właściciel: „opony… koła”): auta w lokalach (AUTO NAPRAWA — na podniesionym podnośniku,
    // WULKANIZACJA — auto klienta na stanowisku) i KOŁA (opona + felga) z tych samych modeli aut co w ruchu: regały, stosy opon,
    // montażownica, wyważarka, stosy przed wulkanizacją. Koło = siatka „koło …/obrót” szablonu (środek w osi, oś = x szablonu),
    // najlżejsza z kół o promieniu 0.28…0.42 m; stoją bez skryptów, z LOD (znikają przy ~3 % ekranu), cienie włączone.
    static void WarsztatyMiastaB(List<AutoSzablon> szablony)
    {
        // pod korzeniem miasta (nie pod niepodzielnym korzeniem ruchu): auta i koła warsztatów doczytują się z kwadratem 150 m
        var stary = root.parent ? root.parent.Find("CLAUDE miasto B: warsztaty — auta i koła") : null; if (stary) UnityEngine.Object.DestroyImmediate(stary.gameObject);
        var g = new GameObject("CLAUDE miasto B: warsztaty — auta i koła").transform; g.SetParent(root.parent ? root.parent : root, false); g.position = Vector3.zero;
        var rnd = new System.Random(911);
        // auta
        int aut = 0;
        foreach (var (c, przod, podnies, nazwa) in BraxRegionMiastoB.AutaWarsztatow)
        {
            var kand = szablony.Where(s => s.dl <= 4.75f && s.szerNadw <= 1.95f && s.wys <= 1.6f).ToList();
            if (kand.Count == 0) kand = szablony.ToList();
            var sz = kand[rnd.Next(kand.Count)];
            var o = AutoInstancja(sz, g, "Auto w warsztacie · " + nazwa + " · " + sz.id, rnd, out var kolor);
            o.transform.SetPositionAndRotation(c, Quaternion.LookRotation(przod));
            var nad = o.transform.Find("nadwozie");
            if (nad)
                foreach (var t in nad.Cast<Transform>().Where(t => t.name == "spaliny" || t.name.StartsWith("światło") || t.name.StartsWith("kierunkowskaz")).ToList())
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            if (podnies <= 0f) { var bc = o.AddComponent<BoxCollider>(); bc.center = sz.obrys.center; bc.size = sz.obrys.size; }
            LodAuta(o, .02f);
            aut++;
        }
        // KOŁA Z KODU (11.11, właściciel 19:36: „Felgi są brzydkie w wulkanizacji” — wcześniej koła wycinane z modeli aut): opona
        // 205/55 R16 i felga aluminiowa z kodu (BraxWarsztatKod.Kolo — 3 wzory szprych × 3 kolory), w stosach i na regałach
        float kR = BraxWarsztatKod.R * BraxClaudeOsiedle.K, kSz = BraxWarsztatKod.Sz * BraxClaudeOsiedle.K;
        int n = 0;
        foreach (var (podparcie, os, ile, nazwa) in BraxRegionMiastoB.KolaWarsztatow)
        {
            bool lezy = Mathf.Abs(os.y) > .7f; int wariant = rnd.Next(9);
            var e = new GameObject("Koła · " + nazwa).transform; e.SetParent(g, false); e.position = podparcie;
            for (int q = 0; q < Mathf.Max(1, ile); q++)
            {
                var p = lezy ? podparcie + Vector3.up * (kSz * .5f + q * (kSz + .004f)) : podparcie + Vector3.up * kR;
                if (lezy && rnd.Next(3) == 0) wariant = rnd.Next(9);   // w stosie komplety po kilka tych samych
                BraxWarsztatKod.Kolo(e, nazwa, p, lezy ? Vector3.up : os, wariant, rnd.Next(0, 360));
                n++;
            }
            BraxChlodnie.Lod(e, 30f);
            float hS = lezy ? Mathf.Max(1, ile) * (kSz + .004f) : 2f * kR;
            var cc = new GameObject("Koła kolizja · " + nazwa); cc.transform.SetParent(e, false);
            cc.transform.position = podparcie + Vector3.up * hS * .5f;
            var bx = cc.AddComponent<BoxCollider>(); bx.size = lezy ? new Vector3(2f * kR, hS, 2f * kR) : new Vector3(Mathf.Abs(os.x) > .5f ? kSz : 2f * kR, hS, Mathf.Abs(os.z) > .5f ? kSz : 2f * kR);
        }
        string opis = "BRAXHORN_WARSZTATY auta " + aut + " (" + string.Join(", ", BraxRegionMiastoB.AutaWarsztatow.Select(a => a.nazwa)) + "), kół z kodu " + n;
        Debug.Log(opis); rep.notes.Add(opis.Replace("BRAXHORN_WARSZTATY ", "warsztaty: "));
    }

    // TAKSÓWKA (właściciel: „taksówki” w ruchu miasta): lakier żółty zamiast wylosowanego, kogut TAXI na dachu (świecący, napis
    // z obu stron) — dzieci auta bez flag static (jadą z autem).
    static Material lakierTaxi;
    static bool Taksowka(GameObject o, AutoSzablon sz)
    {
        if (!sz.lakier) return false;
        if (!lakierTaxi)
        {
            var c = new Color(.95f, .72f, .05f);
            lakierTaxi = new Material(sz.lakier) { name = "Auto: lakier taxi (żółty)" };
            if (lakierTaxi.HasProperty("baseColorFactor")) lakierTaxi.SetVector("baseColorFactor", new Vector4(c.r, c.g, c.b, 1f));
            if (lakierTaxi.HasProperty("_Color")) lakierTaxi.SetColor("_Color", c);
            if (lakierTaxi.HasProperty("_BaseColor")) lakierTaxi.SetColor("_BaseColor", c);
            BraxZapisAssetu.Utworz(lakierTaxi, Out + "/auto_lakier_taxi_miasto_b.mat");
        }
        int zm = 0;
        foreach (var rr in o.GetComponentsInChildren<Renderer>(true))
        {
            var ms = rr.sharedMaterials; bool z = false;
            for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith(sz.lakier.name, StringComparison.Ordinal)) { ms[i] = lakierTaxi; z = true; zm++; }
            if (z) rr.sharedMaterials = ms;
        }
        if (zm == 0) return false;
        var nad = o.transform.Find("nadwozie"); var rodzic = nad ? nad : o.transform;
        var b = sz.obrys; float k = K, y = b.max.y;
        var kog = new BraxRegionMeble.Bud();
        kog.MinMax(new Vector3(b.center.x - .42f * k, y, b.center.z - .14f * k), new Vector3(b.center.x + .42f * k, y + .2f * k, b.center.z + .14f * k));
        var g = kog.Obiekt("Taksówka: kogut TAXI", rodzic, BraxRegionMeble.Swiecacy("Taksówka: kogut (świeci)", new Color(1f, .93f, .55f), 1.1f), false, false);
        foreach (var (zz, f) in new[] { (b.center.z + .15f * k, Vector3.forward), (b.center.z - .15f * k, Vector3.back) })
            BraxRegionMeble.Napis(rodzic, "TAXI", new Vector3(b.center.x, y + .1f * k, zz), f, .12f * k, new Color(.05f, .05f, .05f));
        foreach (var t in rodzic.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        return g;
    }

    static void AutaMiastaB(List<AutoSzablon> szablony)
    {
        const float YA = .003f;
        float rx = BraxRegionMiastoB.RX, rz = BraxRegionMiastoB.RZ, ax = BraxRegionMiastoB.AX, wx = BraxRegionMiastoB.WX, sz = BraxRegionMiastoB.SZ, pz = BraxRegionMiastoB.PetlaZ;
        float ah = BraxRegionMiastoB.AH, kr = BraxRegionMiastoB.Kr;
        // ulice (cw = połowa jezdni, half = połowa ulicy z chodnikiem)
        var G = new BraxTrasyAut.Ulica { id = "MIASTO Główna i ul. Zachodnia", wzdluzX = true, c = rz, a0 = BraxRegionMiastoB.PetlaZX, a1 = wx + 30f, cw = 4.5f, half = 4.5f + kr + 3.1f };
        var A = new BraxTrasyAut.Ulica { id = "MIASTO Aleje", wzdluzX = false, c = ax, a0 = sz - 30f, a1 = pz, cw = ah, half = ah + kr + BraxRegionMiastoB.Ziel + BraxRegionMiastoB.Chod };
        var Wd = new BraxTrasyAut.Ulica { id = "MIASTO Wiaduktowa", wzdluzX = false, c = wx, a0 = sz, a1 = rz, cw = BraxRegionMiastoB.WH, half = BraxRegionMiastoB.WH + kr + 1.55f };
        var Ws = new BraxTrasyAut.Ulica { id = "MIASTO Warsztatowa", wzdluzX = true, c = sz, a0 = ax, a1 = BraxRegionMiastoB.SX1, cw = BraxRegionMiastoB.SH, half = BraxRegionMiastoB.SH + kr + 2.5f };
        var Pd = new BraxTrasyAut.Ulica { id = "MIASTO pętla (umowna oś x)", wzdluzX = true, c = pz, a0 = ax - 1f, a1 = ax + 1f, cw = ah, half = ah };
        var Pw = new BraxTrasyAut.Ulica { id = "MIASTO pętla ZACHÓD (umowna oś z)", wzdluzX = false, c = BraxRegionMiastoB.PetlaZX, a0 = rz - 1f, a1 = rz + 1f, cw = ah, half = ah };
        var ul = new List<BraxTrasyAut.Ulica> { G, A, Wd, Ws, Pd, Pw };
        var RONDO = new BraxTrasyAut.Wezel { x = rx, z = rz, rondo = true, ux = G, uz = A };
        // Pętla północna przy sklepie to teraz parking, NIE rondo. Zachowujemy
        // węzeł tylko jako punkt orientacyjny, bez bramek kołowych.
        var PETLA = new BraxTrasyAut.Wezel { x = ax, z = pz, rondo = false, ux = Pd, uz = A };
        var J1 = new BraxTrasyAut.Wezel { x = wx, z = rz, ux = G, uz = Wd, rodzaj = 2 };
        var J2 = new BraxTrasyAut.Wezel { x = ax, z = sz, ux = Ws, uz = A, rodzaj = 2 };
        var W = new BraxTrasyAut.Wezel { x = wx, z = sz, ux = Ws, uz = Wd };
        var PETLA_W = new BraxTrasyAut.Wezel { x = BraxRegionMiastoB.PetlaZX, z = rz, rondo = true, ux = G, uz = Pw };
        var wz = new List<BraxTrasyAut.Wezel> { RONDO, PETLA, J1, J2, W, PETLA_W };
        Vector2 P(BraxTrasyAut.Wezel w) => new Vector2(w.x, w.z);
        var defs = new List<BraxTrasyAut.Definicja>
        {
            new BraxTrasyAut.Definicja { nazwa = "M pętla Wiaduktowa–Warsztatowa (w lewo)", petla = true, y = YA, punkty = new[] { P(RONDO), P(J2), P(W), P(J1) } },
            new BraxTrasyAut.Definicja { nazwa = "M pętla Wiaduktowa–Warsztatowa (w prawo)", petla = true, y = YA, punkty = new[] { P(RONDO), P(J1), P(W), P(J2) } },
            // Zjazd na parking północny pozostaje dostępny od alei, ale nie
            // wysyłamy ruchu miejskiego na tor dawnej pętli i stanowiska.
            new BraxTrasyAut.Definicja { nazwa = "M pętla przez ul. Zachodnią (zawracanie na pętli ZACHÓD)", petla = true, y = YA, punkty = new[] { P(RONDO), P(PETLA_W), P(RONDO), P(J2), P(W), P(J1) } },
        };
        // WŁAŚCICIEL 3.11 10:15 („gdzie ruch na ulicy?”) — NOWE DZIELNICE: siatka ulic z buildu (log „nawierzchnie: aleja 4 odcinki,
        // ulice C0–C3, Nowojorska, Tramwajowa, skrzyżowań 12”): Aleja Centralna (z = RZ), ul. Nowojorska (z = NN1), ul. Tramwajowa
        // (z = NS1) × ulice C0…C3 (x = NC). Pętle wokół kwartałów, osobne od pętli miasta B (bez łączenia przez pętlę ZACHÓD).
        // Skrzyżowania zwykłe (rodzaj 0) — sygnalizację i linie zatrzymania mają już strefy drogowe dzielnic (BraxStrefyDrogowe).
        {
            var NC = BraxRegionMiastoB.NC; float nn = BraxRegionMiastoB.NN1, ns = BraxRegionMiastoB.NS1, npr = BraxRegionMiastoB.NPR, apr = BraxRegionMiastoB.APR;
            var AC = new BraxTrasyAut.Ulica { id = "NOWE Aleja Centralna", wzdluzX = true, c = rz, a0 = NC[3], a1 = NC[0], cw = ah, half = apr };
            var NY = new BraxTrasyAut.Ulica { id = "NOWE ul. Nowojorska", wzdluzX = true, c = nn, a0 = NC[3], a1 = NC[0], cw = ah, half = npr };
            var TR = new BraxTrasyAut.Ulica { id = "NOWE ul. Tramwajowa", wzdluzX = true, c = ns, a0 = NC[3], a1 = NC[0], cw = ah, half = npr };
            ul.Add(AC); ul.Add(NY); ul.Add(TR);
            var nA = new BraxTrasyAut.Wezel[4]; var nN = new BraxTrasyAut.Wezel[4]; var nT = new BraxTrasyAut.Wezel[4];
            for (int i = 0; i < 4; i++)
            {
                var C = new BraxTrasyAut.Ulica { id = "NOWE C" + i, wzdluzX = false, c = NC[i], a0 = ns, a1 = nn, cw = ah, half = npr }; ul.Add(C);
                nA[i] = new BraxTrasyAut.Wezel { x = NC[i], z = rz, ux = AC, uz = C }; nN[i] = new BraxTrasyAut.Wezel { x = NC[i], z = nn, ux = NY, uz = C };
                nT[i] = new BraxTrasyAut.Wezel { x = NC[i], z = ns, ux = TR, uz = C };
                wz.Add(nA[i]); wz.Add(nN[i]); wz.Add(nT[i]);
            }
            // 9.11 19:44 (właściciel: tramwaj „ma jechać naokoło całego nowego miasta”): T1 jeździ przeciwnie do wskazówek zegara
            // zewnętrznymi pasami obwodu (Tramwajowa na wschód, C0 na północ, Nowojorska na zachód, C3 na południe). WSZYSTKIE pętle
            // aut nowych dzielnic jadą ZGODNIE z ruchem wskazówek (skręty w prawo) — na obwodzie zawsze pasem przeciwnym niż tramwaj.
            defs.Add(new BraxTrasyAut.Definicja { nazwa = "N pętla C0–C1 północ", petla = true, y = YA, punkty = new[] { P(nA[1]), P(nN[1]), P(nN[0]), P(nA[0]) } });
            defs.Add(new BraxTrasyAut.Definicja { nazwa = "N pętla C2–C3 północ", petla = true, y = YA, punkty = new[] { P(nA[3]), P(nN[3]), P(nN[2]), P(nA[2]) } });
            // Torowisko C1–C2 (ul. Tramwajowa) jest TYLKO dla tramwaju: auta nie jadą po jego osi i nie wjeżdżają w wagon na przystanku.
            // 9.11: 7.11 zdjęto razem z tym 2 z 4 pętli aut (C1–C2 południe, duża) i liczność 8→6 — mniej samochodów, choć właściciel
            // 3.11 10:15 skarżył się „samochody zniknęły”. Zastępcze pętle OMIJAJĄ torowisko: duża po Nowojorskiej + C0/C3 + Alei,
            // oraz C2–C3 południe po Tramwajowej poza odcinkiem torowiska (tor leży między C1 a C2).
            defs.Add(new BraxTrasyAut.Definicja { nazwa = "N pętla C2–C3 południe (zgodnie z zegarem: C2, Tramwajowa, C3, Aleja)", petla = true, y = YA, punkty = new[] { P(nA[2]), P(nT[2]), P(nT[3]), P(nA[3]) } });
            defs.Add(new BraxTrasyAut.Definicja { nazwa = "N pętla duża (Aleja, C3, Nowojorska, C0)", petla = true, y = YA, punkty = new[] { P(nA[0]), P(nA[1]), P(nA[2]), P(nA[3]), P(nN[3]), P(nN[2]), P(nN[1]), P(nN[0]) } });
            // Kurs autobusu okrąża kwartał C0–C1: na Alei Centralnej wraca w kierunku +X, przy właściwej krawędzi zatoki (-Z).
            defs.Add(new BraxTrasyAut.Definicja { nazwa = "AUTOBUS Aleja Centralna C0-C1 kurs", petla = true, y = YA,
                punkty = new[] { P(nA[0]), P(nT[0]), P(nT[1]), P(nA[1]) } });
        }
        // rondo miasta: tor jazdy w osi pierścienia (7.5…14.5 m → 11 m), krawędź zewnętrzna 14.5 m (obie pętle ronda tej samej wielkości)
        BraxTrasyAut.RondoPromien = (BraxRegionMiastoB.RWyspa + BraxRegionMiastoB.RJezdnia) * .5f; BraxTrasyAut.RondoJezdnia = BraxRegionMiastoB.RJezdnia;
        // 10.11: narożniki łukiem — zebry za łukiem, linia zatrzymania przed zebrą przesunięta o tyle samo (BraxRegionMiastoBLuki)
        BraxRegionMiastoB.LukiPlanJesliPusty();
        BraxTrasyAut.OdsuniecieZebr = c => BraxRegionMiastoB.OdsuniecieZebry(c);
        var wy = BraxTrasyAut.Generuj(ul, wz, defs);
        foreach (var l in wy.log) rep.notes.Add("trasy: " + l);
        if (wy.trasy.Count == 0) { rep.notes.Add("DO POPRAWKI: brak tras aut w mieście"); return; }
        Debug.Log("BRAXHORN_RUCH_MIASTO_B_TRASY tras " + wy.trasy.Count + ", punktów " + wy.punkty.Count + ", zdarzeń " + wy.zdarzenia.Count + ", skrzyżowań " + wy.wezly.Count + " (ze światłami 2): " +
                  string.Join("; ", wy.trasy.Select(t => t.nazwa + " " + t.dl.ToString("F0") + " m")));
        var mg = new GameObject("BRAXHORN ruch aut miasta (trasy, pierwszeństwo, auta)"); mg.transform.SetParent(root, false);
        var ra = mg.AddComponent<BraxRuchAut>();
        int busRoute = wy.trasy.FindIndex(q => q.nazwa.Contains("AUTOBUS Aleja Centralna C0-C1 kurs"));
        ra.punkty = wy.punkty.ToArray(); ra.trasy = wy.trasy.ToArray(); ra.zdarzenia = wy.zdarzenia.ToArray(); ra.wezly = wy.wezly.ToArray(); ra.ziarno = 2111;
        var rnd = new System.Random(2111); var pula = PulaAut(szablony); var klakson = Klakson();
        var auta = new List<BraxAuto>(); int taksowki = 0;
        int[] ile = { 5, 5, 4, 3, 3, 3, 4 };   // 3 pętle miasta B (14 aut) + 4 pętle nowych dzielnic (13 aut); autobus osobno (busRoute → 0 aut z puli).
                                              // właściciel 3.11 10:15: „gdzie ruch na ulicy, samochody zniknęły”; 9.11: po zdjęciu pętli północnej (parking) i C1–C2 (torowisko) auta rozłożone na pętle zastępcze, łącznie 27 jak przed 7.11
        for (int r = 0; r < wy.trasy.Count; r++)
            for (int k = 0; k < (r == busRoute ? 0 : r < ile.Length ? ile[r] : 1); k++)
            {
                var sz2 = pula[rnd.Next(pula.Count)];
                var o = AutoInstancja(sz2, root, "Auto w ruchu (miasto) · " + sz2.id + (AutaNC.Contains(sz2.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), rnd, out var kolor);
                if (k == 0 && (r == 0 || r == 3) && Taksowka(o, sz2)) { o.name = "Taksówka (miasto) · " + sz2.id; taksowki++; }
                var a = o.AddComponent<BraxAuto>();
                a.trasaNr = r; a.sStart = wy.trasy[r].dl * (k + .3f) / Mathf.Max(1, ile[Mathf.Min(r, ile.Length - 1)]); a.trasa = PunktyTrasy(wy, r);
                a.vmax = 9.0f + (float)rnd.NextDouble() * 2.2f;   // 32–40 km/h w mieście
                a.dl = sz2.dl; a.szer = sz2.szerNadw + .04f; a.rozstaw = sz2.rozstaw; a.osTyl = sz2.osTyl; a.promienKola = sz2.promienKola;
                a.nadwozie = o.transform.Find("nadwozie");
                a.kola = new[] { "LP", "PP", "LT", "PT" }.Select(n => o.transform.Find("koło " + n + "/obrót")).Where(t => t).ToArray();
                a.skret = new[] { "LP", "PP" }.Select(n => o.transform.Find("koło " + n)).Where(t => t).ToArray();
                if (a.nadwozie)
                {
                    a.stop = Szukaj(a.nadwozie, "światło STOP L", "światło STOP P");
                    a.migL = Szukaj(a.nadwozie, "kierunkowskaz tył L", "kierunkowskaz przód L");
                    a.migP = Szukaj(a.nadwozie, "kierunkowskaz tył P", "kierunkowskaz przód P");
                    var dym = a.nadwozie.Find("spaliny"); if (dym) { var kl = new List<Transform>(); foreach (Transform t in dym) kl.Add(t); a.dym = kl.ToArray(); a.dymSkala = K; }
                }
                a.klakson = klakson;
                var bc = o.AddComponent<BoxCollider>(); bc.center = sz2.obrys.center; bc.size = sz2.obrys.size;
                var rb = o.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                LodAuta(o, .015f);
                auta.Add(a);
            }
        // REALNY autobus z istniejącego modelu. Wspólny indeks w ra.auta
        // oznacza, że BraxRuchAutSym widzi jego gabaryty i wszystkie auta
        // hamują przed nim; brak oddzielnej nieruchomej kopii/collidera.
        if (busRoute >= 0 && BraxMiastoAssety.Jest("miasto_pojazdy"))
        {
            var source = BraxMiastoAssety.Model("miasto_pojazdy", "autobus");
            if (source)
            {
                var bus = BraxMiastoAssety.Postaw(root, source,
                    "Autobus KURSOWY ALEJA CENTRALNA (wspólna symulacja)",
                    new Vector3(-345f, YA, rz - ah*.5f), 0f, 1f, false, true);
                if (bus)
                {
                    var bb = BraxRegion.Bounds(bus);
                    float busLength = Mathf.Max(bb.size.x, bb.size.z);
                    float busWidth = Mathf.Min(bb.size.x, bb.size.z);
                    if (busLength < 10f || busLength > 22f || busWidth < 2f || busWidth > 5f)
                        Debug.LogWarning("BRAXHORN_BUS DO POPRAWKI: oryginalny model ma nieprawidłowy obrys " + bb.size);
                    else
                    {
                        var a = bus.AddComponent<BraxAuto>();
                        a.trasaNr = busRoute; a.trasa = PunktyTrasy(wy,busRoute);
                        a.vmax = 8f; a.dl = busLength; a.szer = busWidth;
                        a.rozstaw = Mathf.Min(9.2f, busLength*.63f);
                        a.osTyl = -busLength*.40f; a.promienKola = .5f;
                        a.nadwozie = null; // nie nadpisuj rotacji części modelu
                        var bounds = BraxRegion.Bounds(bus);
                        var col = bus.AddComponent<BoxCollider>();
                        col.center = bus.transform.InverseTransformPoint(bounds.center);
                        col.size = bounds.size / Mathf.Max(.0001f, bus.transform.lossyScale.x);
                        var rigid = bus.AddComponent<Rigidbody>(); rigid.isKinematic = true; rigid.useGravity = false;
                        foreach (var tr in bus.GetComponentsInChildren<Transform>(true))
                            GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);
                        // Szukamy punktu postoju na prawym pasie +X z przystankiem po prawej (-Z).
                        var route = wy.trasy[busRoute];
                        var target = new Vector3(BraxRegionMiastoB.PrzystanekAutobusuCentralnaX -
                            busLength*.5f, YA, rz - ah*.5f);
                        float best = float.MaxValue, bestS = -1f;
                        for (int i = 0; i + 1 < route.ile; i++)
                        {
                            var p = wy.punkty[route.start+i];
                            var next = wy.punkty[route.start+i+1];
                            if (next.x < p.x + .01f) continue;
                            float dd = (p-target).sqrMagnitude;
                            if (dd < best) {best=dd;bestS=i*route.krok;}
                        }
                        if (bestS < 0 || best > 60f)
                            Debug.LogWarning("BRAXHORN_BUS DO POPRAWKI: nie znaleziono postoju na pasie +X (odległość2 " + best.ToString("F1") + ")");
                        else
                        {
                            a.sStart = Mathf.Max(2f,bestS-35f);
                            ra.autobusIndex = auta.Count;
                            ra.autobusTrasa = busRoute;
                            ra.autobusStopS = bestS;
                            ra.autobusStopCzas = 8f;
                            auta.Add(a);
                            Debug.Log("BRAXHORN_BUS włączony_w_ruch=1 trasa=" + busRoute +
                                " długość=" + busLength.ToString("F1") +
                                "m postój_s=" + bestS.ToString("F1") +
                                " wszystkie_auta_widzą_autobus=tak");
                        }
                    }
                    if (!auta.Contains(bus.GetComponent<BraxAuto>()))
                        UnityEngine.Object.DestroyImmediate(bus);
                }
            }
        }
        ra.auta = auta.ToArray();
        float szerMax = auta.Max(a => a.szer), dlMax = auta.Max(a => a.dl);
        var s0 = new BraxRuchAutSym(ra.punkty, ra.trasy, ra.zdarzenia, ra.wezly, ra.ziarno, szerMax, dlMax);
        foreach (var a in auta) s0.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax);
        s0.UstawStart();
        for (int i = 0; i < auta.Count; i++) auta[i].transform.SetPositionAndRotation(s0.auta[i].poz, Quaternion.LookRotation(s0.auta[i].kier));
        // test: 10 min ruchu bez renderu (zakleszczenia, nachodzenie, wyjazd poza jezdnię miasta B)
        var st = new BraxRuchAutSym(ra.punkty, ra.trasy, ra.zdarzenia, ra.wezly, ra.ziarno, szerMax, dlMax);
        foreach (var a in auta) st.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax);
        st.UstawStart();
        // 10.11 (właściciel: „te auta przez to się korkują”): każde zakleszczenie z miejscem, trasą i czołem kolejki w logu —
        // naprawa dróg po dowodach, nie na ślepo (pierwsze 40 wpisów).
        int opisane = 0;
        st.naZakleszczenie = o => { if (++opisane <= 40) Debug.Log("BRAXHORN_RUCH_MIASTO_B_ZAKLESZCZENIE " + o); };
        var wt = st.Test(600f, .1f, p => BraxRegionMiastoB.NaJezdni(p));
        Debug.Log("BRAXHORN_RUCH_MIASTO_B aut=" + auta.Count + " (taksówek " + taksowki + ") tras=" + wy.trasy.Count + " test 600 s: zakleszczenia=" + wt.zakleszczenia + " nachodzenia=" + wt.nachodzenia +
                  " poza_droga=" + wt.pozaDroga + " przejazdy skrzyżowań=" + wt.przejazdyWezlow + " śr. prędkość " + (wt.sredniaV * 3.6f).ToString("F0") + " km/h");
    }
}

// LUDZIE W MIEŚCIE — ETAP 2 (B; właściciel 15:12: „przenieś naszych ludzi z osiedla 14 — Rocketbox, wzrost ≈ Brax; mają CHODZIĆ:
// przechodnie na chodnikach, przejścia na zebrach na zielonym, obsada w lokalach”). Ten sam system co na osiedlu (paczka Rocketbox,
// NowyPrzechodzien → BraxPieszy, Siedzacy, Obsada → BraxMieszkaniec), sieć chodników miasta B zbudowana tu (osie chodników
// alej, placu ronda, ul. Warsztatowej i Wiaduktowej, ulicy głównej od ronda do skrzyżowania po wiadukcie i dalej do x −40, placu
// wieżowców i skweru). Przez jezdnię tylko po zebrach: przy światłach (J1, J2) piesi czekają na zielonego ludzika (strefy
// BraxSwiatloDrogowe), bez świateł (wloty ronda i pętli) auta ustępują (BraxStrefyDrogowe, etap wyposażenia). Kolejność budowy
// (16:04): ten krok idzie po ruchu aut — na gotowych nawierzchniach i wyposażeniu (węzły sprawdzane: nie na jezdni, przeszkody).
// Ławki: tylko ławki miasta B (korzenie „CLAUDE miasto B…”, „CLAUDE tyły B…”); zajęta ławka traci BraxSeat (gracz nie siada na
// człowieku). Animacje tylko blisko kamery (BraxLudzie, jak na osiedlu). Trenerzy i ćwiczący w siłowni — poza tym krokiem.
public static partial class BraxClaudeOsiedle
{
    // pkt 33 / 37 (skala × K, Brax 2.33 m): audyt drzwi (skrzydło ≥ 2.1 m × K − 15 cm) i siedzisk (0.45 m × K ± 0.12 nad podłogą
    // przed siedziskiem) w całej scenie miasta; log z listą niezgodnych (naprawa po obejrzeniu listy — bez zgadywania skali).
    static void AudytSkali()
    {
        Physics.SyncTransforms();
        var bledy = new List<string>(); int drzwi = 0, siedzisk = 0;
        float hDrzwi = 2.1f * K - .15f, hSiedz = .45f * K;
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d.leaves == null) continue;
            foreach (var l in d.leaves)
            {
                if (!l) continue; var rs = l.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); drzwi++;
                if (b.size.y < hDrzwi) bledy.Add("drzwi " + (string.IsNullOrEmpty(d.label) ? d.name : d.label) + " h=" + b.size.y.ToString("F2") + " (≥" + hDrzwi.ToString("F2") + ") @" + b.center.ToString("F0"));
            }
        }
        foreach (var q in UnityEngine.Object.FindObjectsByType<BraxSeat>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (q.lie) continue;
            var f = q.transform.forward; f.y = 0f; if (f.sqrMagnitude < 1e-4f) f = Vector3.forward; f.Normalize();
            var o = q.transform.position + f * .7f; o.y = q.surfaceY + .3f;
            if (!Physics.Raycast(o, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) continue;
            float h = q.surfaceY - hit.point.y; siedzisk++;
            if (Mathf.Abs(h - hSiedz) > .12f) bledy.Add("siedzisko " + q.label + " h=" + h.ToString("F2") + " (" + hSiedz.ToString("F2") + "±0.12) @" + q.transform.position.ToString("F0"));
        }
        string linia = "BRAXHORN_SKALA niezgodne=" + bledy.Count + " drzwi=" + drzwi + " siedzisk=" + siedzisk + (bledy.Count > 0 ? " [" + string.Join("; ", bledy.Take(30)) + (bledy.Count > 30 ? "; …" : "") + "]" : "");
        if (bledy.Count == 0) Debug.Log(linia); else Debug.LogWarning(linia);
    }

    // pkt 42 (właściciel: „siatka przez całą jezdnię za placem z BANKIEM”): ogrodzenia / siatki / bariery / szlabany leżące w poprzek
    // jezdni miasta B są usuwane razem z kolizjami (drogi przejezdne). Port (brama z podniesionym szlabanem) i osiedle — bez zmian.
    static void ZaporyNaJezdni(Transform cityRoot)
    {
        string[] slowa = { "ogrodz", "płot", "plot ", "fence", "barier", "barrier", "szlaban", "zapora", "siatka ogrodz", "chain", "kratownica ogrodz", "pachoł", "pachol", "traffic cone", "traffic_cone", "jersey", "kozioł", "kozlo", "roadblock", "road block", "znak", "road sign", "traffic sign", "sign post", "signpost", "sign_" };   // znaki: tylko gdy słup stoi NA jezdni (właściciel 3.11: „znaki na środku ulicy”)
        // właściciel 3.11 10:21 (zdjęcie): siatka + szlaban + blok + pachołki w poprzek ulicy — NaJezdni zna tylko jezdnie miasta B;
        // teraz jezdnia także z nawierzchni pod punktem (materiał / obiekt „asfalt|jezdnia|asphalt|road”, nie chodnik).
        bool Jezdnia(Vector3 q)
        {
            if (BraxRegionMiastoB.NaJezdni(q)) return true;
            foreach (var h in Physics.RaycastAll(q + Vector3.up * 3f, Vector3.down, 7f, ~0, QueryTriggerInteraction.Ignore))
            {
                var rr = h.collider.GetComponent<Renderer>();
                string t = (h.collider.name + " " + (rr && rr.sharedMaterial ? rr.sharedMaterial.name : "")).ToLowerInvariant();
                if (t.Contains("chodnik") || t.Contains("sidewalk") || t.Contains("pavement") || t.Contains("krawęż") || t.Contains("curb")) return false;
                if (t.Contains("asfalt") || t.Contains("jezdni") || t.Contains("asphalt") || t.Contains("road")) return true;
            }
            return false;
        }
        var usun = new List<GameObject>(); var opis = new List<string>(); int zostalo = 0;
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.name.ToLowerInvariant() + " " + (r.sharedMaterial ? r.sharedMaterial.name.ToLowerInvariant() : "");
            if (!slowa.Any(n.Contains)) continue;
            bool port = false; for (var q = r.transform; q; q = q.parent) if (q.name.StartsWith("Port", StringComparison.Ordinal) || q.name.Contains("PORT") || q.name.Contains("osiedl")) { port = true; break; }
            if (port) continue;
            var b = r.bounds; if (b.size.y > 4f) continue;
            // 9.11 (log APK 8.11: BRAXHORN_DROGI_ZAPORY skasował „słupy znaków” ulicy głównej jako JEDNĄ siatkę 163,8 m, farbę jezdni
            // 645 m / 691 m / 63 m i kozły oporowe 127 m — właściciel: „źle ustawione znaki”, „latarnie na środku drogi”).
            // Zapora stoi W POPRZEK jezdni: długość ≤ 16 m (jezdnia ≤ 12 m + pobocza). Dłuższe = infrastruktura WZDŁUŻ ulicy
            // (scalone słupy znaków, farba, balustrady) — nigdy nie zapora. Farba (≤ 10 cm wysokości) i „Oznakowanie” — nigdy.
            if (b.size.y < .10f || n.Contains("oznakowanie") || n.Contains("farba") || n.Contains("słupy znaków") || n.Contains("slupy znakow")) continue;
            bool dlX = b.size.x >= b.size.z; float L = dlX ? b.size.x : b.size.z;
            if (L > 16f) continue;
            if (L < 2f)
            {   // pachołek / blok / kozioł na jezdni (nie przy krawędzi): środek i ±0.8 m z jezdnią
                var c0 = b.center; var pq = dlX ? new Vector3(0, 0, .8f) : new Vector3(.8f, 0, 0);
                bool znak = n.Contains("znak") || n.Contains("sign");
                var pq2 = dlX ? new Vector3(.8f, 0, 0) : new Vector3(0, 0, .8f);   // znak: jezdnia także w drugiej osi (nie pas zieleni / chodnik)
                if (znak ? (b.size.y < 4.5f && Jezdnia(c0) && Jezdnia(c0 + pq) && Jezdnia(c0 - pq) && Jezdnia(c0 + pq2) && Jezdnia(c0 - pq2))
                         : (L > .15f && b.size.y < 1.6f && Jezdnia(c0) && Jezdnia(c0 + pq) && Jezdnia(c0 - pq))) { usun.Add(r.gameObject); opis.Add(r.name + " @" + c0.ToString("F0") + " (drobna przeszkoda)"); }
                continue;
            }
            int na = 0;
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f; var p = b.center;
                if (dlX) p.x = Mathf.Lerp(b.min.x, b.max.x, t); else p.z = Mathf.Lerp(b.min.z, b.max.z, t);
                // prowadzący (audyt 3.11): liczy się tylko punkt z jezdnią PO OBU stronach zapory (zapora w poprzek / na środku
                // jezdni) — balustrady i barierki wzdłuż krawędzi (wiadukt, nabrzeże, chodnik) zostają
                var pr = dlX ? new Vector3(0, 0, 1.5f) : new Vector3(1.5f, 0, 0);
                if (Jezdnia(p) && Jezdnia(p + pr) && Jezdnia(p - pr)) na++;
            }
            if (na < 3) continue;
            usun.Add(r.gameObject); opis.Add(r.name + " @" + b.center.ToString("F0") + " dł " + L.ToString("F1") + " na_jezdni " + na + "/13");
        }
        foreach (var g in usun.Distinct())
        {
            if (!g) continue;
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            if (g.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(g); else { var r = g.GetComponent<MeshRenderer>(); if (r) UnityEngine.Object.DestroyImmediate(r); zostalo++; }
        }
        Debug.Log("BRAXHORN_DROGI_ZAPORY usuniete=" + usun.Count + " (z dziećmi, tylko renderer: " + zostalo + ")" + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(20)) + "]" : "") + " (9.11: tylko siatki ≤ 16 m w poprzek jezdni; farba i słupy znaków chronione)");
    }

    // ---- MIEJSCA LUDZI z innych kroków (wyspa: „osoba: poza · rola · gdzie”; wnętrza: „BRAXHORN obsada: rola”) ----
    // Wcześniej nikt ich nie zamieniał na postacie (arena bez kibiców, plaża i fitness bez ludzi). Postacie wyspy idą pod korzeń
    // wyspy (scena BraxhornCity_Wyspa — nie wiszą nad morzem, gdy wyspa niewczytana). Widownia ≤ 150 (telefon): bliskie rzędy
    // animowane, dalsze w pozie (budżet BraxLudzie.Animuj). Poza „leży” — brak klipu: osoba siedzi na leżaku.
    static readonly Rola RolaRatownik = new Rola { nazwa = "ratownik", postaci = new[] { "Sports", "Male_Adult" }, kolor = new Color(.78f, .08f, .07f) };
    static readonly Rola RolaSport = new Rola { nazwa = "zawodnik / trener", postaci = new[] { "Sports" } };
    static Rola RolaDla(string r)
    {
        r = r.ToLowerInvariant();
        if (r.Contains("ochron")) return RolaOchrona;
        if (r.Contains("ratownik")) return RolaRatownik;
        if (r.Contains("recepcj") || r.Contains("kasjer") || r.Contains("bileter")) return RolaRecepcja;
        if (r.Contains("barman") || r.Contains("obsług") || r.Contains("sędzia") || r.Contains("dyżurny")) return RolaObsluga;
        if (r.Contains("trener") || r.Contains("zawodni") || r.Contains("klientka") || r.Contains("nyxa")) return RolaSport;
        return RolaPrzechodzien;
    }
    static void LudzieZMiejsc(Transform cityRoot, System.Random rnd)
    {
        string[] stoi = { "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" };
        string[] siedzi = { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" };
        int kibice = 0, inni = 0, obsada = 0, pominiete = 0;
        var wyspa = cityRoot.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("CLAUDE wyspa", StringComparison.Ordinal));
        var miejsca = wyspa ? wyspa.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Wyspa: miejsca ludzi", StringComparison.Ordinal)) : null;
        if (miejsca)
        {
            var gW = new GameObject("Wyspa: ludzie (kibice, plaża, obsada)").transform; gW.SetParent(wyspa, false);
            var lista = miejsca.Cast<Transform>().ToList();
            int nKib = lista.Count(t => t.name.Contains("· kibic ·")), krokKib = Mathf.Max(1, Mathf.CeilToInt(nKib / 150f)), iKib = 0;
            foreach (var t in lista)
            {
                var cz = t.name.Substring("osoba: ".Length).Split(new[] { " · " }, StringSplitOptions.None);
                if (cz.Length < 3) { pominiete++; continue; }
                string poza = cz[0], rola = cz[1], gdzie = cz[2];
                bool kib = rola == "kibic" && poza == "siedzi";
                if (kib && (iKib++ % krokKib) != 0) continue;
                var R = RolaDla(rola);
                BraxMieszkaniec m = null;
                if (poza == "siedzi" || poza == "leży") m = Siedzacy(R, gW, gdzie + " — " + rola, t.position, t.forward, rnd, siedzi);
                else m = NowyMieszkaniec(R, gW, gdzie + " — " + rola, t.position, t.eulerAngles.y, rnd, stoi);
                if (!m) { pominiete++; continue; }
                if (kib) kibice++; else inni++;
            }
        }
        // miejsca obsady z wnętrz (metro, fitness Nyxy) — stoją, przodem jak znacznik
        var gO = Group("Ludzie / obsada wnętrz (metro, fitness)");
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true).Where(x => x.name.StartsWith("BRAXHORN obsada: ", StringComparison.Ordinal)).ToList())
        {
            string rola = t.name.Substring("BRAXHORN obsada: ".Length);
            var m = rola.Contains("mata") ? Siedzacy(RolaDla(rola), gO, rola, t.position + Vector3.up * .02f, t.forward, rnd, siedzi)
                                          : NowyMieszkaniec(RolaDla(rola), gO, rola, t.position, t.eulerAngles.y, rnd, stoi);
            if (m) obsada++; else pominiete++;
        }
        Debug.Log("BRAXHORN_LUDZIE_WYSPA kibice=" + kibice + " inni=" + inni + " obsada_wnetrz=" + obsada + " pominiete=" + pominiete + (miejsca ? "" : " (brak miejsc wyspy)"));
    }

    // ---- AUDYT WYSPY (właściciel: „sprawdź ją 10 razy”): lista kontrolna z nazw obiektów i materiałów pod korzeniem wyspy ----
    static void KompletWyspy(Transform cityRoot)
    {
        var wyspa = cityRoot.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("CLAUDE wyspa", StringComparison.Ordinal));
        if (!wyspa) { Debug.LogWarning("BRAXHORN_WYSPA_KOMPLET brak wyspy"); return; }
        var tekst = new List<string>();
        foreach (var t in wyspa.GetComponentsInChildren<Transform>(true)) tekst.Add(t.name.ToLowerInvariant());
        foreach (var r in wyspa.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m) tekst.Add(m.name.ToLowerInvariant());
        int Ile(params string[] w) => tekst.Count(x => w.Any(x.Contains));
        int ludzie = wyspa.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
        var pozycje = new (string grupa, string co, int ile, int min)[]
        {
            ("plaża", "piasek", Ile("piasek", "piask"), 1), ("plaża", "wieżyczka ratownika", Ile("ratownik"), 1), ("plaża", "flaga", Ile("flag"), 1),
            ("plaża", "boje kąpieliska", Ile("boj"), 1), ("plaża", "leżaki", Ile("leżak", "lezak"), 1), ("plaża", "parasole", Ile("parasol"), 1),
            ("plaża", "prysznice", Ile("prysznic"), 1), ("plaża", "przebieralnie", Ile("przebieral"), 1), ("plaża", "WC (budynek)", Ile("wc", "toalet"), 1),
            ("plaża", "bar plażowy", Ile("bar plaż", "beach bar"), 1), ("plaża", "wypożyczalnia", Ile("wypożycz", "wypozycz"), 1), ("plaża", "kosze", Ile("kosz"), 1),
            ("plaża", "ławki", Ile("ławk", "lawk"), 1), ("plaża", "kładki / deptak", Ile("kładk", "deptak", "pomost z desek"), 1), ("plaża", "oświetlenie", Ile("latarn", "lamp", "oprawa"), 1),
            ("ośrodek", "domki", Ile("domek"), 22), ("ośrodek", "recepcja", Ile("recepcj"), 1), ("ośrodek", "sklep", Ile("sklep"), 1), ("ośrodek", "plac zabaw", Ile("plac zabaw", "huśtaw", "zjeżdżal"), 1),
            ("ośrodek", "chodniki", Ile("chodnik"), 1), ("ośrodek", "krawężniki", Ile("krawęż", "krawez"), 1), ("ośrodek", "zieleń (krzewy)", Ile("krzew", "shrub"), 1),
            ("przystań", "pomost", Ile("pomost", "przystań"), 1), ("przystań", "cumowanie (polery / knagi)", Ile("poler", "knag", "cum"), 1), ("przystań", "tablica WYSPA", Ile("tablica wyspa", "wyspa: tablica"), 1),
            ("arena", "trybuny", Ile("trybun"), 1), ("arena", "fotele", Ile("fotel"), 1), ("arena", "loże VIP", Ile("vip", "loż"), 1), ("arena", "klatka / oktagon", Ile("klatk"), 1),
            ("arena", "ring z linami", Ile("lin ring", "ring:", "ringi"), 1), ("arena", "narożniki", Ile("narożn"), 1), ("arena", "stół sędziów", Ile("sędzi"), 1),
            ("arena", "telebim", Ile("telebim"), 1), ("arena", "rampa świateł", Ile("rampa", "kratownic"), 1), ("arena", "tunel zawodników", Ile("tunel"), 1),
            ("arena", "szatnie", Ile("szatni"), 1), ("arena", "kasy", Ile("kas"), 1), ("arena", "toalety w arenie", Ile("arena: wc", "arena: toalet", "toalety areny"), 1),
            ("zieleń", "palmy", Ile("palm"), 1), ("zieleń", "drzewa Poly Haven (pine/fir/island_tree)", Ile("pine_tree", "fir_tree", "island_tree"), 1),
            ("zieleń", "krzewy Poly Haven", Ile("shrub_0"), 1), ("skały", "coast_rocks / boulder / cliff", Ile("coast_rocks", "boulder", "coastal_cliff", "skał", "skal"), 1),
            ("brzeg", "piana", Ile("pian"), 1),
        };
        var brak = pozycje.Where(q => q.ile < q.min).Select(q => q.grupa + ": " + q.co + " (" + q.ile + "/" + q.min + ")").ToList();
        string linia = "BRAXHORN_WYSPA_KOMPLET jest=" + (pozycje.Length - brak.Count) + "/" + pozycje.Length + " ludzie=" + ludzie + " auta=" + wyspa.GetComponentsInChildren<BraxAuto>(true).Length
            + " | " + string.Join(", ", pozycje.Select(q => q.co + "=" + q.ile)) + (brak.Count > 0 ? " | BRAKI: " + string.Join("; ", brak) : "");
        if (brak.Count == 0) Debug.Log(linia); else Debug.LogWarning(linia);
    }

    public static void LudzieMiastaB(Transform cityRoot)
    {
        var repPrev = rep; var rootPrev = root; var colPrev = colliders; var groupPrev = group;
        try
        {
            rep = new Report(); groups.Clear(); group = "Miasto B: ludzie";
            var stary = cityRoot.Find("CLAUDE miasto B: ludzie"); if (stary) UnityEngine.Object.DestroyImmediate(stary.gameObject);
            root = new GameObject("CLAUDE miasto B: ludzie").transform; root.SetParent(cityRoot, false); root.position = Vector3.zero;
            root.gameObject.AddComponent<LODGroup>();   // niepodzielny przy podziale na kwadraty (jak ruch aut): przechodzień idzie przez wiele kwadratów
            colliders = new GameObject("kolizje").transform; colliders.SetParent(root, false);
            if (!LadujRocketbox()) { Debug.Log("BRAXHORN_LUDZIE_MIASTO_B brak paczki Rocketbox — bez ludzi"); return; }
            Physics.SyncTransforms();
            var rnd = new System.Random(2111);
            int obs = 0, siedz = 0, prz = 0;
            float k = K; const float YC = .15f;
            // obsada (stoją za ladą, przodem do wchodzących; yaw: 0 = +z, 90 = +x)
            var lada = new[] { "work_mid", "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" };
            void O(string gdzie, Rola r, string kw, Vector3 p, float yaw) { if (Obsada(gdzie, r, kw, p, yaw, rnd, lada)) obs++; }
            O("BRAXHORN TOWER — recepcja", RolaRecepcja, "recepcja", new Vector3(52.7f, YC, 45.4f), 90f);                 // lada x 53.2…54.1, z 42.1…46.5
            O("BOSMANAT — bosman w okienku", RolaObsluga, "recepcja", new Vector3(71.3f, BraxNabrzeze.Bulwar + .3f, 64.8f), 90f);   // (podłoga na cokole 0.3 m) właściciel 3.11 11:01: „mógłby ktoś pracować” — za ladą okienka (front na wschód)
            O("BRAXHORN OFFICE — recepcja", RolaRecepcja, "recepcja", new Vector3(48.3f, YC, -40.35f), 0f);              // lada z −39.6…−38.7, wejście od +z
            O("PARK TOWER — recepcja", RolaRecepcja, "recepcja", new Vector3(-26.8f, YC, -42.35f), 0f);
            O("PARK TOWER — kawiarnia", RolaObsluga, "barista", new Vector3(-51.2f, YC, -40.35f), 90f);                  // bar x −50.5…−49.5
            O("Warsztat AUTO NAPRAWA — mechanik", RolaObsluga, "mechanik", BraxRegionMiastoB.MechanikAutoNaprawa, 180f);     // przy stole warsztatowym (9.11: wnętrze od nowa)
            O("WULKANIZACJA — wulkanizator", RolaObsluga, "mechanik", BraxRegionMiastoB.MechanikWulkanizacja, 270f);        // przy montażownicy
            // FAWELA (9.11): sprzedawcy lokali, targ i sala kickboxingu rywala — posadzki leżą na stoku, więc poziom podany wprost (bez szukania podłogi przy y ≈ 0)
            foreach (var (pf, yawF, gdzieF) in BraxRegionFawela.Sprzedawcy)
            {
                bool wSali = gdzieF.StartsWith("sala", StringComparison.Ordinal);
                if (Obsada("FAWELA — " + gdzieF, wSali ? RolaSport : RolaObsluga, wSali ? "trener" : gdzieF.StartsWith("bar", StringComparison.Ordinal) ? "barman" : "kasjer", pf, yawF, rnd, lada, pf.y + .02f)) obs++;
            }
            // DWORZEC R1 (11.11): kasy, informacja, kawiarnia — stanowiska obsługi wg BraxMiastoDworzec (posadzka hali podana wprost)
            foreach (var (pd, yawD, gdzieD) in BraxMiastoDworzec.Obsluga)
                if (Obsada("DWORZEC — " + gdzieD, gdzieD.StartsWith("kasa", StringComparison.Ordinal) || gdzieD.StartsWith("informacja", StringComparison.Ordinal) ? RolaRecepcja : RolaObsluga, gdzieD.StartsWith("kawiarnia", StringComparison.Ordinal) ? "barista" : "kasjer", pd, yawD, rnd, lada, pd.y + .02f)) obs++;
            // ul. Zachodnia (ETAP 2): dyżurny komisariatu i rejestracja szpitala za ladą holu, policjant przed wejściem, lekarz w holu szpitala
            var lk = BraxRegionTylyB.LadaHolu(BraxRegionMiastoB.Komisariat); var ls = BraxRegionTylyB.LadaHolu(BraxRegionMiastoB.Szpital);
            O("KOMISARIAT — dyżurny", RolaPolicja, "policja", new Vector3(lk.x + 1.0f * k, YC, lk.z - .95f * k), 0f);
            O("KOMISARIAT — policjant przed wejściem", RolaPolicja, "policja", new Vector3(BraxRegionMiastoB.Komisariat.center.x + 3.2f * k, YC, BraxRegionMiastoB.Komisariat.yMax + 1.6f), 0f);
            O("SZPITAL — rejestracja", RolaRecepcja, "recepcja", new Vector3(ls.x + 1.0f * k, YC, ls.z - .95f * k), 0f);
            // CENTRUM (ETAP 3): recepcje holi 6 wieżowców; GALERIA: informacja i baristka w pasażu
            foreach (var w in BraxRegionMiastoB.WiezeCentrum) { var lw = BraxRegionTylyB.LadaHolu(w); O("CENTRUM — recepcja wieżowca", RolaRecepcja, "recepcja", new Vector3(lw.x + 1.0f * k, YC, lw.z - .95f * k), 0f); }
            var gl = BraxRegionMiastoB.Galeria; var lg = BraxRegionTylyB.LadaHolu(gl);
            O("GALERIA — informacja", RolaRecepcja, "recepcja", new Vector3(lg.x + 1f + 1.0f * k, YC, lg.z - .95f * k), 0f);
            O("GALERIA — baristka", RolaObsluga, "barista", new Vector3(gl.xMin + 4f - 1.2f, YC, lg.z + .8f), 90f);
            O("SZPITAL — lekarz w izbie przyjęć", RolaMedyk, "medyk", new Vector3(BraxRegionMiastoB.Szpital.center.x - 4f, YC, BraxRegionMiastoB.Szpital.yMax - 3.5f), 200f);
            // ławki miasta B: część zajęta (jedna osoba na ławkę)
            bool MojaB(Transform t)
            {
                for (var q = t; q; q = q.parent)
                    if ((q.name.StartsWith("CLAUDE miasto B", StringComparison.Ordinal) && q.name != "CLAUDE miasto B: ludzie") || q.name.StartsWith("CLAUDE tyły B", StringComparison.Ordinal)) return true;
                return false;
            }
            List<BraxSeat> lawki = UnityEngine.Object.FindObjectsByType<BraxSeat>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(q => q.label == "ŁAWKA" && !q.lie && MojaB(q.transform)).OrderBy(q => q.transform.position.x * 7.1f + q.transform.position.z).ToList();
            var g = Group("Ludzie / na ławkach (miasto B)");
            foreach (var seat in lawki.OrderBy(_ => rnd.Next()).ToList())
            {
                if (siedz >= 18) break;
                if (rnd.NextDouble() < .55) continue;
                var F = seat.transform.forward; F.y = 0f; F.Normalize(); var R = Vector3.Cross(Vector3.up, F);
                var pnt = seat.transform.position + F * .04f; pnt.y = seat.surfaceY;
                var m = Siedzacy(RolaPrzechodzien, g, "Na ławce", pnt + R * (rnd.NextDouble() < .5 ? -.42f : .42f) * k, F, rnd, new[] { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" });
                if (m) { siedz++; UnityEngine.Object.DestroyImmediate(seat); }
            }
            // przechodnie
            var s = SiecMiastaB();
            var start = Enumerable.Range(0, s.w.Count).Where(j => !s.zebry.Contains(j) && s.sas[j].Count > 0 && Wolne(s.w[j])).ToList();
            var gp = Group("Ludzie / przechodnie (miasto B)");
            for (int i = 0; i < 44 && start.Count > 0; i++)   // 32 (ETAP 2) + 12 na ul. Zachodnią, CENTRUM i GALERIĘ
            {
                int st = start[rnd.Next(start.Count)];
                var (pts, pr) = Trasa(s, st, rnd);
                if (pts.Length < 2) continue;
                if (NowyPrzechodzien(gp, pts, pr, rnd, 900 + i)) prz++;
            }
            // WŁAŚCICIEL 3.11 10:34 („ludzie mają rozmawiać”): stojące PARY rozmawiające na chodnikach miasta B — jak na osiedlu
            // (BraxMieszkaniec lider + rozmówca: kwestie w dymkach co kilka sekund, widoczne do 24 m). Miejsca: węzły sieci pieszych
            // (nie zebry), 1.6 m obok węzła (nie w osi przejścia), wolne od kolizji i poza jezdnią. Najwyżej 16 par.
            int parRozm = 0; var gRozm = Group("Ludzie / rozmowy (miasto B)");
            foreach (int j in Enumerable.Range(0, s.w.Count).Where(j => !s.zebry.Contains(j)).OrderBy(_ => rnd.Next()).ToList())
            {
                if (parRozm >= 16) break;
                float yaw = (float)rnd.NextDouble() * 360f; var bok = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var p0 = s.w[j] + bok * 1.6f;
                var os = Quaternion.Euler(0, yaw + 90f, 0) * Vector3.forward; float d = Mathf.Lerp(1.05f, 1.3f, (float)rnd.NextDouble());
                Vector3 a1 = p0 - os * d * .5f, b1 = p0 + os * d * .5f;
                if (!Wolne(a1, .3f) || !Wolne(b1, .3f) || BraxRegionMiastoB.NaJezdni(a1) || BraxRegionMiastoB.NaJezdni(b1)) continue;
                var para = new GameObject("Rozmowa (para, miasto B)").transform; para.SetParent(gRozm, false); para.position = p0;
                var m1 = NowyMieszkaniec(RolaPrzechodzien, para, "Rozmawia", a1, Quaternion.LookRotation(b1 - a1).eulerAngles.y, rnd, KlipySlucha, kubek: rnd.NextDouble() < .25);
                var m2 = NowyMieszkaniec(RolaPrzechodzien, para, "Rozmawia", b1, Quaternion.LookRotation(a1 - b1).eulerAngles.y, rnd, KlipySlucha);
                if (!m1 || !m2) { UnityEngine.Object.DestroyImmediate(para.gameObject); continue; }
                m1.rozmowca = m2; m2.rozmowca = m1; m1.lider = true; parRozm++;
            }
            Debug.Log("BRAXHORN_LUDZIE_ROZMOWY miasto B: par rozmawiających " + parRozm + "/16 (dymki do 24 m) + przechodnie zaczepiają się w ruchu");
            // NOWE DZIELNICE (właściciel 3.11: „dwóch ludzi tylko”): pary rozmawiające na chodnikach Alei Centralnej i Nowojorskiej,
            // w połowie każdego odcinka między ulicami C (obie strony) — do 12 par.
            int parNowe = 0;
            {
                var NC = BraxRegionMiastoB.NC; float rzA = BraxRegionMiastoB.RZ, nn = BraxRegionMiastoB.NN1;
                float offA = BraxRegionMiastoB.APR - BraxRegionMiastoB.Chod * .5f, offN = BraxRegionMiastoB.NPR - BraxRegionMiastoB.Chod * .5f;
                for (int i = 0; i + 1 < NC.Length; i++)
                    foreach (var (zc, off) in new[] { (rzA, offA), (nn, offN) })
                        foreach (float sgn in new[] { -1f, 1f })
                        {
                            float xm = (NC[i] + NC[i + 1]) * .5f + (float)(rnd.NextDouble() - .5) * 20f;
                            var p0 = new Vector3(xm, 0f, zc + sgn * off);
                            if (Physics.Raycast(p0 + Vector3.up * 5f, Vector3.down, out var hg, 10f, ~0, QueryTriggerInteraction.Ignore)) p0.y = hg.point.y; else continue;
                            float d = Mathf.Lerp(1.05f, 1.3f, (float)rnd.NextDouble());
                            Vector3 a1 = p0 - Vector3.right * d * .5f, b1 = p0 + Vector3.right * d * .5f;
                            if (!Wolne(a1, .3f) || !Wolne(b1, .3f) || BraxRegionMiastoB.NaJezdni(a1) || BraxRegionMiastoB.NaJezdni(b1)) continue;
                            var para = new GameObject("Rozmowa (para, nowe dzielnice)").transform; para.SetParent(gRozm, false); para.position = p0;
                            var m1 = NowyMieszkaniec(RolaPrzechodzien, para, "Rozmawia", a1, 90f, rnd, KlipySlucha, kubek: rnd.NextDouble() < .25);
                            var m2 = NowyMieszkaniec(RolaPrzechodzien, para, "Rozmawia", b1, -90f, rnd, KlipySlucha);
                            if (!m1 || !m2) { UnityEngine.Object.DestroyImmediate(para.gameObject); continue; }
                            m1.rozmowca = m2; m2.rozmowca = m1; m1.lider = true; parNowe++;
                        }
            }
            Debug.Log("BRAXHORN_LUDZIE_ROZMOWY nowe dzielnice: par " + parNowe + "/12");
            int ileLawekB = lawki.Count;
            int wezlow = s.w.Count;
            int przejsc = s.zebry.Count / 2;
            Debug.Log("BRAXHORN_LUDZIE_MIASTO_B obsada=" + obs + "/17 na_lawkach=" + siedz + " (ławek B " + ileLawekB + ") przechodnie=" + prz +
                      " siec: węzłów " + wezlow + ", przejść po zebrach " + przejsc);
            // pkt 41: kierowca w każdym jadącym aucie (te same fotele i klipy co na osiedlu); zaparkowane puste
            try
            {
                KierowcyAut(rnd);
                var jad = UnityEngine.Object.FindObjectsByType<BraxAuto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int bez = jad.Count(a => !a.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Kierowca", StringComparison.Ordinal)));
                Debug.Log("BRAXHORN_AUTA_KIEROWCY jadace=" + jad.Length + " jadace_bez=" + bez + " kierowcow=" + rbKierowcy + (bez > 0 ? " (cel 0 — auto bez węzła „" + FotelKierowcy + "”?)" : ""));
            }
            catch (Exception e) { Debug.LogWarning("BRAXHORN_AUTA_KIEROWCY DO POPRAWKI " + e.Message); }
            try { LudzieZMiejsc(cityRoot, rnd); } catch (Exception e) { Debug.LogWarning("BRAXHORN_LUDZIE_WYSPA DO POPRAWKI " + e); }
            try { KompletWyspy(cityRoot); } catch (Exception e) { Debug.LogWarning("BRAXHORN_WYSPA_KOMPLET DO POPRAWKI " + e.Message); }
            try
            {   // E19 w mieście (osiedle e60e8e6): koszt postaci i aut ograniczany odległością w BraxLudzie / BraxAuto (runtime)
                int ludzie = UnityEngine.Object.FindObjectsByType<BraxMieszkaniec>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + UnityEngine.Object.FindObjectsByType<BraxPieszy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                var smr = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var auta = UnityEngine.Object.FindObjectsByType<BraxAuto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int cullNie = smr.Count(x => { var an = x.GetComponentInParent<Animation>(); return an && an.cullingType != AnimationCullingType.BasedOnRenderers; });
                Debug.Log("BRAXHORN_WYDAJNOSC miasto ludzie=" + ludzie + " skinned=" + smr.Length + " animacje_bez_cullingu=" + cullNie + " auta=" + auta.Length + " zrodla_audio_silnikow=" + auta.Count(x => x.silnik) + " (grają najbliższe " + BraxAuto.SilnikiMax + ")"
                    + " animowani_limit=" + BraxLudzie.LimitMax + " stały (bez adaptacji do kl./s) cienie_postaci_zasieg=" + BraxLudzie.CienOdl.ToString("F1") + " m skóra_2_kości_od=" + BraxLudzie.DwieKosciOdl.ToString("F1") + " m cienie_aut_zasieg=" + BraxAuto.CienAutaOdl.ToString("F1") + " m");
            }
            catch (Exception e) { Debug.LogWarning("BRAXHORN_WYDAJNOSC DO POPRAWKI " + e.Message); }
            Debug.Log(RaportStrojow());
            Debug.Log(RaportSkaliLudzi());
            try { AudytSkali(); } catch (Exception e) { Debug.LogWarning("BRAXHORN_SKALA DO POPRAWKI " + e.Message); }
            try { ZaporyNaJezdni(cityRoot); } catch (Exception e) { Debug.LogWarning("BRAXHORN_DROGI_ZAPORY DO POPRAWKI " + e.Message); }
            foreach (var n in rep.notes) Debug.Log("BRAXHORN_LUDZIE_MIASTO_B " + n);
        }
        finally { rep = repPrev; root = rootPrev; colliders = colPrev; group = groupPrev; groups.Clear(); }
    }

    // Sieć chodników miasta B: odcinki po osiach chodników (węzły co ≤ 4 m), połączenia na styku odcinków (≤ 2.2 m — chodniki po
    // dwóch stronach jezdni są ≥ 6 m od siebie, więc nic nie łączy się przez jezdnię) i zebry (przez jezdnię, oba końce na chodniku).
    // Odcinek z węzłem na jezdni (BraxRegionMiastoB.NaJezdni) albo z ≥ 40 % węzłów w przeszkodzie — odrzucony i zgłoszony.
    static SiecPieszych SiecMiastaB()
    {
        var s = new SiecPieszych(); int seg = 0, odrz = 0;
        const float Y = .15f;
        void O(float x0, float z0, float x1, float z1)
        {
            var A = new Vector3(x0, Y, z0); var B = new Vector3(x1, Y, z1); int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(A, B) / 4f));
            var pts = Enumerable.Range(0, n + 1).Select(i => Vector3.Lerp(A, B, i / (float)n)).ToList();
            int jezdnia = pts.Count(BraxRegionMiastoB.NaJezdni), zle = pts.Count(q => !Wolne(q));
            if (jezdnia > 0 || zle > pts.Count * .4f)
            {
                odrz++; rep.notes.Add("DO POPRAWKI: chodnik (" + x0.ToString("F1") + ", " + z0.ToString("F1") + ") → (" + x1.ToString("F1") + ", " + z1.ToString("F1") + ") odrzucony: na jezdni " + jezdnia + ", w przeszkodzie " + zle + "/" + pts.Count);
                return;
            }
            int prev = -1;
            foreach (var q in pts) { int j = s.Dodaj(q, seg); if (prev >= 0) s.Polacz(prev, j, false); prev = j; }
            s.konce.Add(s.w.Count - pts.Count); s.konce.Add(s.w.Count - 1);
            seg++;
        }
        int N(float x, float z) { var p = new Vector3(x, Y, z); int best = -1; float bd = 2.6f; for (int i = 0; i < s.w.Count; i++) { float d = Vector3.Distance(s.w[i], p); if (d < bd) { bd = d; best = i; } } return best; }
        int zebr = 0, zebrBrak = 0;
        void Z(float xa, float za, float xb, float zb)
        {
            int a = N(xa, za), b = N(xb, zb);
            if (a < 0 || b < 0 || s.segOd[a] == s.segOd[b]) { zebrBrak++; rep.notes.Add("DO POPRAWKI: zebra (" + xa.ToString("F1") + ", " + za.ToString("F1") + ") ↔ (" + xb.ToString("F1") + ", " + zb.ToString("F1") + ") bez węzła chodnika"); return; }
            s.Polacz(a, b, true); s.zebry.Add(a); s.zebry.Add(b); zebr++;
        }
        float xw = -135.5f, xe = -116.5f, gS = 22.2f, gN = 34.7f, wW = -70.2f, wE = -62.2f, sN = -14.5f, sS = -23.5f;
        // aleje (zach. i wsch. chodnik; wschodni Alei Południowej przerwany wlotem ul. Warsztatowej)
        O(xw, 51f, xw, 80f); O(xe, 51f, xe, 80f);
        O(xw, 6f, xw, -80f); O(xe, 6f, xe, sN); O(xe, sS, xe, -80f);
        // plac ronda: łuki chodnika między wylotami (przekątne po płytach placu, poza pierścieniem jezdni)
        O(xe, 51f, -104f, gN); O(xe, 6f, -104f, gS); O(xw, 51f, -148f, 38f); O(xw, 6f, -148f, 19f);
        // ulica główna: południowy chodnik przerwany wlotem ul. Wiaduktowej, północny ciągły
        O(-104f, gS, wW, gS); O(wE, gS, -40f, gS); O(-104f, gN, -40f, gN);
        // ul. Wiaduktowa (od ulicy głównej do Warsztatowej) i ul. Warsztatowa (północny przerwany wlotem Wiaduktowej, południowy do placu)
        O(wW, gS, wW, sN); O(wE, gS, wE, sN);
        O(xe, sN, wW, sN); O(wE, sN, -61.6f, sN); O(xe, sS, -58f, sS);
        // plac wieżowców (dwie aleje W-E i łączniki) i skwer po dawnym wiadukcie
        O(-61.6f, -10f, 60f, -10f); O(-58f, -10f, -58f, -34f); O(-58f, -34f, 60f, -34f); O(60f, -10f, 60f, -34f);
        O(-66.2f, gN + 1.8f, -66.2f, 118f);
        // ul. Zachodnia (ETAP 2): chodniki od placu ronda do placu pętli ZACHÓD (wejścia komisariatu i szpitala przy chodniku płd.)
        O(-148f, 38f, BraxRegionMiastoB.ZachX0, 38f); O(-148f, 19f, BraxRegionMiastoB.ZachX0, 19f);
        // CENTRUM (ETAP 3): przejście z ul. Zachodniej (x −216) i między wieżowcami, aleje A1 (z 86) i A2 (z 116), łącznik do pętli PÓŁNOC
        O(-216f, 38f, -216f, 59f); O(-216f, 59f, -228f, 59f); O(-228f, 59f, -228f, 86f);
        O(-260f, 86f, -149.5f, 86f); O(-149.5f, 86f, xw, 80f); O(-260f, 116f, -158f, 116f);
        O(-260f, 86f, -260f, 116f); O(-228f, 86f, -228f, 116f); O(-160f, 86f, -160f, 116f);
        // GALERIA: przejście z ul. Zachodniej (x −216) i przedpole
        O(-216f, 19f, -216f, -4.5f); O(-260f, -4.5f, -198f, -4.5f);
        // styki odcinków
        for (int i = 0; i < s.w.Count; i++)
            for (int j = i + 1; j < s.w.Count; j++)
                if (s.segOd[i] != s.segOd[j] && Vector3.Distance(s.w[i], s.w[j]) < 2.2f) s.Polacz(i, j, false);
        // zebry: rondo (wsch. ramię = ulica główna, płn., płd.), pętla, J1 (ulica główna po obu stronach + Wiaduktowa), J2 (aleja ×2 + Warsztatowa)
        Z(-101f, gS, -101f, gN);
        Z(xw, 53.5f, xe, 53.5f); Z(xw, 3.5f, xe, 3.5f); Z(xw, 79f, xe, 79f);
        Z(-59.8f, gS, -59.8f, gN); Z(-72.6f, gS, -72.6f, gN); Z(wW, 20.6f, wE, 20.6f);
        Z(xw, -12.6f, xe, -12.6f); Z(xw, -25.4f, xe, -25.4f); Z(-118f, sN, -118f, sS);
        Z(-151f, 19f, -151f, 38f); Z(BraxRegionMiastoB.ZachX0 + 3f, 19f, BraxRegionMiastoB.ZachX0 + 3f, 38f);
        rep.notes.Add("sieć chodników miasta B: węzłów " + s.w.Count + ", odcinków " + seg + " (odrzuconych " + odrz + "), zebr " + zebr + "/12" + (zebrBrak > 0 ? " — BRAK " + zebrBrak : ""));
        return s;
    }
}

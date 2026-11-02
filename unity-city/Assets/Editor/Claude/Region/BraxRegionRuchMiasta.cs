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
// Wydajność: 6 aut w ruchu (LOD znika przy ~1.5 % ekranu), symulacja BraxRuchAut jak na osiedlu.
public static partial class BraxClaudeOsiedle
{
    public static void RuchMiastaB(Transform cityRoot)
    {
        var repPrev = rep; var rootPrev = root; var colPrev = colliders; var groupPrev = group;
        float rpPrev = BraxTrasyAut.RondoPromien, rjPrev = BraxTrasyAut.RondoJezdnia;
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
            UnityEngine.Object.DestroyImmediate(tmp.gameObject);
            foreach (var n in rep.notes) Debug.Log("BRAXHORN_RUCH_MIASTO_B " + n);
        }
        finally
        {
            rep = repPrev; root = rootPrev; colliders = colPrev; group = groupPrev;
            BraxTrasyAut.RondoPromien = rpPrev; BraxTrasyAut.RondoJezdnia = rjPrev;
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
        try
        {
            MiejscaParkingowe.Clear();
            foreach (var m in miejsca) MiejscaParkingowe.Add(new MiejsceParkingowe { srodek = m.c, przod = m.przod, rozmiar = m.rozmiar, przyScianie = false });
            group = "Miasto B: auta zaparkowane";
            AutaZaparkowane(szablony);
        }
        finally { MiejscaParkingowe.Clear(); MiejscaParkingowe.AddRange(zapas); group = g; }
    }

    static void AutaMiastaB(List<AutoSzablon> szablony)
    {
        const float YA = .003f;
        float rx = BraxRegionMiastoB.RX, rz = BraxRegionMiastoB.RZ, ax = BraxRegionMiastoB.AX, wx = BraxRegionMiastoB.WX, sz = BraxRegionMiastoB.SZ, pz = BraxRegionMiastoB.PetlaZ;
        float ah = BraxRegionMiastoB.AH, kr = BraxRegionMiastoB.Kr;
        // ulice (cw = połowa jezdni, half = połowa ulicy z chodnikiem)
        var G = new BraxTrasyAut.Ulica { id = "MIASTO Główna", wzdluzX = true, c = rz, a0 = rx, a1 = wx + 30f, cw = 4.5f, half = 4.5f + kr + 3.1f };
        var A = new BraxTrasyAut.Ulica { id = "MIASTO Aleje", wzdluzX = false, c = ax, a0 = sz - 30f, a1 = pz, cw = ah, half = ah + kr + BraxRegionMiastoB.Ziel + BraxRegionMiastoB.Chod };
        var Wd = new BraxTrasyAut.Ulica { id = "MIASTO Wiaduktowa", wzdluzX = false, c = wx, a0 = sz, a1 = rz, cw = BraxRegionMiastoB.WH, half = BraxRegionMiastoB.WH + kr + 1.55f };
        var Ws = new BraxTrasyAut.Ulica { id = "MIASTO Warsztatowa", wzdluzX = true, c = sz, a0 = ax, a1 = BraxRegionMiastoB.SX1, cw = BraxRegionMiastoB.SH, half = BraxRegionMiastoB.SH + kr + 2.5f };
        var Pd = new BraxTrasyAut.Ulica { id = "MIASTO pętla (umowna oś x)", wzdluzX = true, c = pz, a0 = ax - 1f, a1 = ax + 1f, cw = ah, half = ah };
        var ul = new List<BraxTrasyAut.Ulica> { G, A, Wd, Ws, Pd };
        var RONDO = new BraxTrasyAut.Wezel { x = rx, z = rz, rondo = true, ux = G, uz = A };
        var PETLA = new BraxTrasyAut.Wezel { x = ax, z = pz, rondo = true, ux = Pd, uz = A };
        var J1 = new BraxTrasyAut.Wezel { x = wx, z = rz, ux = G, uz = Wd, rodzaj = 2 };
        var J2 = new BraxTrasyAut.Wezel { x = ax, z = sz, ux = Ws, uz = A, rodzaj = 2 };
        var W = new BraxTrasyAut.Wezel { x = wx, z = sz, ux = Ws, uz = Wd };
        var wz = new List<BraxTrasyAut.Wezel> { RONDO, PETLA, J1, J2, W };
        Vector2 P(BraxTrasyAut.Wezel w) => new Vector2(w.x, w.z);
        var defs = new List<BraxTrasyAut.Definicja>
        {
            new BraxTrasyAut.Definicja { nazwa = "M pętla Wiaduktowa–Warsztatowa (w lewo)", petla = true, y = YA, punkty = new[] { P(RONDO), P(J2), P(W), P(J1) } },
            new BraxTrasyAut.Definicja { nazwa = "M pętla Wiaduktowa–Warsztatowa (w prawo)", petla = true, y = YA, punkty = new[] { P(RONDO), P(J1), P(W), P(J2) } },
            new BraxTrasyAut.Definicja { nazwa = "M pętla przez Aleję Północną (zawracanie na rondzie PÓŁNOC)", petla = true, y = YA, punkty = new[] { P(RONDO), P(PETLA), P(RONDO), P(J1), P(W), P(J2) } },
        };
        // rondo miasta: tor jazdy w osi pierścienia (7.5…14.5 m → 11 m), krawędź zewnętrzna 14.5 m (obie pętle ronda tej samej wielkości)
        BraxTrasyAut.RondoPromien = (BraxRegionMiastoB.RWyspa + BraxRegionMiastoB.RJezdnia) * .5f; BraxTrasyAut.RondoJezdnia = BraxRegionMiastoB.RJezdnia;
        var wy = BraxTrasyAut.Generuj(ul, wz, defs);
        foreach (var l in wy.log) rep.notes.Add("trasy: " + l);
        if (wy.trasy.Count == 0) { rep.notes.Add("DO POPRAWKI: brak tras aut w mieście"); return; }
        Debug.Log("BRAXHORN_RUCH_MIASTO_B_TRASY tras " + wy.trasy.Count + ", punktów " + wy.punkty.Count + ", zdarzeń " + wy.zdarzenia.Count + ", skrzyżowań " + wy.wezly.Count + " (ze światłami 2): " +
                  string.Join("; ", wy.trasy.Select(t => t.nazwa + " " + t.dl.ToString("F0") + " m")));
        var mg = new GameObject("BRAXHORN ruch aut miasta (trasy, pierwszeństwo, auta)"); mg.transform.SetParent(root, false);
        var ra = mg.AddComponent<BraxRuchAut>();
        ra.punkty = wy.punkty.ToArray(); ra.trasy = wy.trasy.ToArray(); ra.zdarzenia = wy.zdarzenia.ToArray(); ra.wezly = wy.wezly.ToArray(); ra.ziarno = 2111;
        var rnd = new System.Random(2111); var pula = PulaAut(szablony); var klakson = Klakson();
        var auta = new List<BraxAuto>();
        int[] ile = { 2, 2, 2 };
        for (int r = 0; r < wy.trasy.Count; r++)
            for (int k = 0; k < (r < ile.Length ? ile[r] : 1); k++)
            {
                var sz2 = pula[rnd.Next(pula.Count)];
                var o = AutoInstancja(sz2, root, "Auto w ruchu (miasto) · " + sz2.id + (AutaNC.Contains(sz2.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), rnd, out var kolor);
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
        var wt = st.Test(600f, .1f, p => BraxRegionMiastoB.NaJezdni(p));
        Debug.Log("BRAXHORN_RUCH_MIASTO_B aut=" + auta.Count + " tras=" + wy.trasy.Count + " test 600 s: zakleszczenia=" + wt.zakleszczenia + " nachodzenia=" + wt.nachodzenia +
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
            O("BRAXHORN OFFICE — recepcja", RolaRecepcja, "recepcja", new Vector3(48.3f, YC, -40.35f), 0f);              // lada z −39.6…−38.7, wejście od +z
            O("PARK TOWER — recepcja", RolaRecepcja, "recepcja", new Vector3(-26.8f, YC, -42.35f), 0f);
            O("PARK TOWER — kawiarnia", RolaObsluga, "barista", new Vector3(-51.2f, YC, -40.35f), 90f);                  // bar x −50.5…−49.5
            O("Warsztat AUTO NAPRAWA — mechanik", RolaObsluga, "mechanik", new Vector3(-100.1f, YC + .05f, -36.3f), 180f); // przy stole warsztatowym
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
                if (siedz >= 14) break;
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
            for (int i = 0; i < 32 && start.Count > 0; i++)
            {
                int st = start[rnd.Next(start.Count)];
                var (pts, pr) = Trasa(s, st, rnd);
                if (pts.Length < 2) continue;
                if (NowyPrzechodzien(gp, pts, pr, rnd, 900 + i)) prz++;
            }
            int ileLawekB = lawki.Count;
            int wezlow = s.w.Count;
            int przejsc = s.zebry.Count / 2;
            Debug.Log("BRAXHORN_LUDZIE_MIASTO_B obsada=" + obs + "/5 na_lawkach=" + siedz + " (ławek B " + ileLawekB + ") przechodnie=" + prz +
                      " siec: węzłów " + wezlow + ", przejść po zebrach " + przejsc);
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
        O(xe, 51f, -104f, gN); O(xe, 6f, -104f, gS); O(xw, 51f, -146f, 28.5f); O(-146f, 28.5f, xw, 6f);
        // ulica główna: południowy chodnik przerwany wlotem ul. Wiaduktowej, północny ciągły
        O(-104f, gS, wW, gS); O(wE, gS, -40f, gS); O(-104f, gN, -40f, gN);
        // ul. Wiaduktowa (od ulicy głównej do Warsztatowej) i ul. Warsztatowa (północny przerwany wlotem Wiaduktowej, południowy do placu)
        O(wW, gS, wW, sN); O(wE, gS, wE, sN);
        O(xe, sN, wW, sN); O(wE, sN, -61.6f, sN); O(xe, sS, -58f, sS);
        // plac wieżowców (dwie aleje W-E i łączniki) i skwer po dawnym wiadukcie
        O(-61.6f, -10f, 60f, -10f); O(-58f, -10f, -58f, -34f); O(-58f, -34f, 60f, -34f); O(60f, -10f, 60f, -34f);
        O(-66.2f, gN + 1.8f, -66.2f, 118f);
        // styki odcinków
        for (int i = 0; i < s.w.Count; i++)
            for (int j = i + 1; j < s.w.Count; j++)
                if (s.segOd[i] != s.segOd[j] && Vector3.Distance(s.w[i], s.w[j]) < 2.2f) s.Polacz(i, j, false);
        // zebry: rondo (wsch. ramię = ulica główna, płn., płd.), pętla, J1 (ulica główna po obu stronach + Wiaduktowa), J2 (aleja ×2 + Warsztatowa)
        Z(-101f, gS, -101f, gN);
        Z(xw, 53.5f, xe, 53.5f); Z(xw, 3.5f, xe, 3.5f); Z(xw, 79f, xe, 79f);
        Z(-59.8f, gS, -59.8f, gN); Z(-72.6f, gS, -72.6f, gN); Z(wW, 20.6f, wE, 20.6f);
        Z(xw, -12.6f, xe, -12.6f); Z(xw, -25.4f, xe, -25.4f); Z(-118f, sN, -118f, sS);
        rep.notes.Add("sieć chodników miasta B: węzłów " + s.w.Count + ", odcinków " + seg + " (odrzuconych " + odrz + "), zebr " + zebr + "/10" + (zebrBrak > 0 ? " — BRAK " + zebrBrak : ""));
        return s;
    }
}

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

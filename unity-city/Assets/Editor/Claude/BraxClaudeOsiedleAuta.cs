using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// AUTA NA OSIEDLU (build 14, 2.11) — w ruchu i zaparkowane. Zdjęcia właściciela z buildu 12: jazda tyłem, obrót w miejscu,
// koła stoją, zakleszczenie przy rondzie pod tunelem, auta zaparkowane wjechane w ścianę, jedno w drugim, krzywo, ten sam
// czerwony model. Teraz:
//  • modele przygotowane z geometrii (BraxClaudeOsiedleAutaModel): przód, koła wydzielone i obracane, fotele, światła;
//  • trasy po prawym pasie (BraxTrasyAut): pętle w części A (każda ulica, rondo przeciwnie do wskazówek), przejazdy
//    z tunelu z miasta i z powrotem, przejazdy ulicą osiedla B (ślepą — auto znika na końcu poza widokiem);
//  • jazda i pierwszeństwo: BraxRuchAut / BraxRuchAutSym (ta sama symulacja w grze i w teście przy buildzie);
//  • test przy buildzie: 10 min ruchu krokiem 0,1 s bez renderu → BRAXHORN_RUCH_TEST zakleszczenia/nachodzenia/poza_droga;
//  • zaparkowane: zatoki z etapu dróg (MiejscaParkingowe), inaczej dotychczasowe miejsca z kontrolą kolizji i odstępem
//    ≥ 0,5 m od ściany; ~45 % miejsc wolnych (dyspozytor 2.11: zajętość 40–60 % wg audytu), różne modele i kolory, każde prosto w swojej zatoce, przodem do alejki.
public static partial class BraxClaudeOsiedle
{
    // Supersamochody z kogutem i napisem POLICE (Marussia „enforcer”, Diablo z belką świateł) — nie jako zwykłe auta
    // ani radiowozy (właściciel 23:38: „radiowóz = zwykły radiowóz, nie supersamochód z naklejką POLICE”).
    static readonly string[] AutaPominiete = { "car_mar", "car_dia" };
    // Częstość modeli (osiedle: najczęściej zwykłe osobówki i dostawczaki, supersamochody rzadko).
    static readonly Dictionary<string, int> AutaWaga = new Dictionary<string, int>
    { { "car_s4", 6 }, { "car_gmc", 4 }, { "car_zuk", 3 }, { "car_mus", 3 }, { "car_dc", 3 }, { "car_r8", 2 }, { "car_918", 1 }, { "car_rev", 1 } };
    // Lakier (sRGB): biały, czarny, srebrny, szary, granat — częściej; czerwony rzadziej.
    static readonly (string n, Color c, int w)[] KoloryAut =
    {
        ("biały", new Color(.92f, .92f, .91f), 22), ("czarny", new Color(.03f, .03f, .035f), 20), ("srebrny", new Color(.64f, .65f, .67f), 20),
        ("szary", new Color(.32f, .33f, .35f), 16), ("granat", new Color(.05f, .08f, .2f), 12), ("czerwony", new Color(.52f, .04f, .04f), 6),
    };
    static readonly Dictionary<(Material, int), Material> lakiery = new Dictionary<(Material, int), Material>();

    static List<GameObject> AutaModele()
    {
        var l = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(AutaDir)) { rep.notes.Add("DO POPRAWKI: brak " + AutaDir); return l; }
        foreach (var id in new[] { "car_s4", "car_gmc", "car_zuk", "car_mus", "car_dc", "car_r8", "car_918", "car_rev", "car_mar", "car_dia" })
        {
            string p = AutaDir + "/" + id + ".glb"; if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
            if (go) l.Add(go);
        }
        return l;
    }

    static void Auta()
    {
        lakiery.Clear(); szablonSwiatla.Clear(); quadSwiatla = null; matStop = matMigacz = matDym = null; autoSiatki = 0;
        var modele = AutaModele(); if (modele.Count == 0) return;
        var tmp = new GameObject("auta — szablony (tymczasowe)").transform;
        var szablony = new List<AutoSzablon>();
        foreach (var src in modele)
        {
            if (AutaPominiete.Contains(src.name))
            {
                rep.notes.Add("auto " + src.name + " pominięte: supersamochód z kogutem/POLICE — nierealny jako auto osiedla i jako radiowóz");
                Debug.Log("BRAXHORN_AUTA_POMINIETE " + src.name + " (supersamochód z kogutem / napisem POLICE)");
                continue;
            }
            try { var s = PrzygotujAuto(src, tmp); if (s != null) szablony.Add(s); else rep.notes.Add("DO POPRAWKI: auto " + src.name + " — brak siatek"); }
            catch (Exception e) { rep.notes.Add("DO POPRAWKI: auto " + src.name + ": " + e.Message); Debug.LogError("BRAXHORN_AUTA błąd modelu " + src.name + ": " + e); }
        }
        if (szablony.Count > 0)
        {
            Guard("auta w ruchu (trasy, pierwszeństwo, test ruchu)", () => AutaWRuchu(szablony));
            Guard("prywatne auto Patryka pod areną", () => AutoPatryka(szablony));
            Guard("auta zaparkowane (zatoki, kolory, kolizje)", () => AutaZaparkowane(szablony));
            Guard("salon samochodowy: auta na talerzach, antresoli i placu", () => SalonAuta(szablony));
            Guard("warsztat: auto na podnośniku", () => WarsztatAuto(szablony));
            Guard("autobusy (E23): linie A i B, przystanki na trasie", Autobusy);
        }
        else rep.notes.Add("DO POPRAWKI: żaden model auta nie przygotowany");
        UnityEngine.Object.DestroyImmediate(tmp.gameObject);
    }

    static Material Lakieruj(Material baza, int kolor)
    {
        if (lakiery.TryGetValue((baza, kolor), out var m)) return m;
        var c = KoloryAut[kolor].c;
        m = new Material(baza) { name = baza.name + " · " + KoloryAut[kolor].n };
        if (m.HasProperty("baseColorFactor")) m.SetVector("baseColorFactor", new Vector4(c.r, c.g, c.b, 1f));   // glTFast trzyma tu kolor sRGB
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        AssetDatabase.CreateAsset(m, Out + "/auto_lakier_" + Safe(baza.name) + "_" + kolor + ".mat");
        lakiery[(baza, kolor)] = m; return m;
    }
    static int LosujKolor(System.Random rnd)
    {
        int suma = KoloryAut.Sum(k => k.w), x = rnd.Next(suma);
        for (int i = 0; i < KoloryAut.Length; i++) { x -= KoloryAut[i].w; if (x < 0) return i; }
        return 0;
    }
    static List<AutoSzablon> PulaAut(List<AutoSzablon> sz) => sz.SelectMany(s => Enumerable.Repeat(s, AutaWaga.TryGetValue(s.id, out var w) ? w : 2)).ToList();

    static GameObject AutoInstancja(AutoSzablon sz, Transform rodzic, string nazwa, System.Random rnd, out string kolor)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(sz.go, rodzic, false); o.name = nazwa; o.SetActive(true);
        kolor = "fabryczny";
        if (sz.lakier)
        {
            int k = LosujKolor(rnd); var nowy = Lakieruj(sz.lakier, k); kolor = KoloryAut[k].n;
            foreach (var r in o.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool zm = false;
                for (int i = 0; i < ms.Length; i++) if (ms[i] == sz.lakier) { ms[i] = nowy; zm = true; }
                if (zm) r.sharedMaterials = ms;
            }
        }
        return o;
    }
    static Renderer[] Szukaj(Transform korzen, params string[] nazwy) =>
        nazwy.Select(n => korzen.Find(n)).Where(t => t).Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
    static void LodAuta(GameObject o, float cull)
    {
        var rs = o.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.sharedMaterial != matStop && r.sharedMaterial != matMigacz).ToArray();
        if (rs.Length == 0) return;
        var lod = o.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(cull, rs) }); lod.RecalculateBounds();
    }

    // Klakson: dwa tony (ok. 415 i 500 Hz, jak typowy klakson osobówki) z harmonicznymi, 0,55 s.
    static AudioClip Klakson()
    {
        int n = (int)(SR * .55f); var x = new float[n];
        var tony = new[] { (415f, 1f), (830f, .45f), (1245f, .2f), (498f, .9f), (996f, .4f), (1494f, .18f) };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, env = Mathf.Clamp01(t / .02f) * Mathf.Clamp01((.55f - t) / .06f), s = 0f;
            foreach (var (f, a) in tony) { float sn = Mathf.Sin(2f * Mathf.PI * f * t); s += a * (.7f * sn + .3f * Mathf.Sign(sn)); }
            x[i] = s * env;
        }
        Normuj(x, .7f);
        return Wav("klakson", x, true);
    }

    // Jezdnie (do testu „poza drogą”): ulice A z tunelem, rondo bez wyspy, ulice B.
    static Vector3 rondoAuta = new Vector3(0f, 0f, 39f);
    static bool NaJezdniAut(Vector3 p)
    {
        const float tol = .2f;
        float rx = p.x - rondoAuta.x, rz = p.z - rondoAuta.z, rr = Mathf.Sqrt(rx * rx + rz * rz);
        if (rr < Mathf.Max(RondoWyspa + .25f, 6.4f) - tol) return false;   // wyspa (i opaska z kostki wokół niej)
        if (rr <= RondoJezdnia + tol) return true;
        if (ulice != null)
            foreach (var u in ulice)
            {
                float a0 = Mathf.Min(u.a0, u.a1), a1 = Mathf.Max(u.a0, u.a1);
                if (u.alongX) { if (p.x >= a0 - tol && p.x <= a1 + tol && Mathf.Abs(p.z - u.c) <= u.cw + tol) return true; }
                else if (p.z >= a0 - tol && p.z <= a1 + tol && Mathf.Abs(p.x - u.c) <= u.cw + tol) return true;
            }
        foreach (var kv in lukNaroznika)   // E3b: asfalt łuku narożnika (kwadrat [róg jezdni, róg + R] poza kołem krawężnika)
        {
            var w = kv.Key.w; int sx = kv.Key.sx, sz = kv.Key.sz; float R = kv.Value;
            float ox = w.x + sx * w.uz.cw, oz = w.z + sz * w.ux.cw, lx = (p.x - ox) * sx, lz = (p.z - oz) * sz;
            if (lx >= -tol && lx <= R + tol && lz >= -tol && lz <= R + tol && new Vector2(p.x - (ox + sx * R), p.z - (oz + sz * R)).magnitude >= R - tol) return true;
        }
        if (p.x >= X0 + .6f - tol && p.x <= X1 - .6f + tol && Mathf.Abs(p.z - StreetZ) <= Carriage + tol) return true;
        if (Mathf.Abs(p.x - AxisX) <= Carriage + tol && p.z >= StreetZ - tol && p.z <= ParkZ0 + tol) return true;
        return false;
    }

    static void AutaWRuchu(List<AutoSzablon> szablony)
    {
        // 1) ulice i skrzyżowania: część A z DrogiA, część B (ulica główna + ulica od parku, skrzyżowanie T)
        var ul = new List<BraxTrasyAut.Ulica>(); var mapa = new Dictionary<UlicaA, BraxTrasyAut.Ulica>();
        if (ulice != null) foreach (var u in ulice) { var g = new BraxTrasyAut.Ulica { id = u.id, wzdluzX = u.alongX, c = u.c, a0 = u.a0, a1 = u.a1, cw = u.cw, half = u.Half }; ul.Add(g); mapa[u] = g; }
        var wz = new List<BraxTrasyAut.Wezel>();
        if (wezly != null)
            foreach (var w in wezly)
            {
                var ux = ulice.FirstOrDefault(u => u.alongX && Mathf.Abs(u.c - w.z) < .1f); var uz = ulice.FirstOrDefault(u => !u.alongX && Mathf.Abs(u.c - w.x) < .1f);
                if (ux == null || uz == null) continue;
                var bw = new BraxTrasyAut.Wezel { x = w.x, z = w.z, rondo = w.rondo, ux = mapa[ux], uz = mapa[uz] };
                // E3b: łuki krawężników i linie zatrzymania za zebrami (te same dane co geometria dróg)
                foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 }) bw.luk[(sx > 0 ? 1 : 0) + (sz > 0 ? 2 : 0)] = LukNaroznika(w, sx, sz);
                if (!w.rondo)
                    foreach (var r in Ramiona(w))
                    {
                        int arm = Mathf.Abs(r.dir.x) > .5f ? (r.dir.x > 0 ? 0 : 1) : (r.dir.z > 0 ? 2 : 3);
                        bw.stop[arm] = ZebraNaRamieniuJest(w, r) ? ZebraRamienia(w, r).a1 : Mathf.Max(r.o.HalfS(r.s), r.o.cw + LukRamienia(w, r));
                    }
                wz.Add(bw);
                if (w.rondo) rondoAuta = new Vector3(w.x, 0f, w.z);
            }
        var bm = new BraxTrasyAut.Ulica { id = "B", wzdluzX = true, c = StreetZ, a0 = X0 + .6f, a1 = X1 - .6f, cw = Carriage, half = Half };
        var bn = new BraxTrasyAut.Ulica { id = "BN", wzdluzX = false, c = AxisX, a0 = StreetZ, a1 = ParkZ0, cw = Carriage, half = Half };
        ul.Add(bm); ul.Add(bn); wz.Add(new BraxTrasyAut.Wezel { x = AxisX, z = StreetZ, ux = bm, uz = bn });
        // skrzyżowania ze światłami (etap dróg / sygnalizacji) — tam bez prawej ręki, decydują strefy świateł
        int swiatla = 0;
        foreach (var w in wz)
            foreach (var si in SkrzyzowaniaInfo)
                if (si.rodzaj == RodzajSkrzyzowania.ZeSwiatlami && new Vector2(si.srodek.x - w.x, si.srodek.z - w.z).magnitude < 3f && !w.rondo) { w.rodzaj = 2; swiatla++; }
        var defs = BraxTrasyAut.TrasyOsiedla(ul, YAsf, .03f, X0 + 4f, X1 - 4f, StreetZ, AxisX, ParkZ0 - 3f);
        BraxTrasyAut.RondoJezdnia = RondoJezdnia;
        var wy = BraxTrasyAut.Generuj(ul, wz, defs);
        foreach (var l in wy.log) rep.notes.Add("trasy aut: " + l);
        Guard("ruch: kontrola toru skrętu (E3b)", () => KontrolaToruSkretu(wy, 1.8f * K, 4.5f * K));   // auto osobowe × K
        if (wy.trasy.Count == 0) { rep.notes.Add("DO POPRAWKI: brak tras aut"); return; }
        Debug.Log("BRAXHORN_RUCH_TRASY tras " + wy.trasy.Count + ", punktów " + wy.punkty.Count + ", zdarzeń " + wy.zdarzenia.Count + ", skrzyżowań " + wy.wezly.Count + " (ze światłami " + swiatla + "): " +
                  string.Join("; ", wy.trasy.Select(t => t.nazwa + " " + t.dl.ToString("F0") + " m")));
        // 2) zarządca ruchu
        var mg = new GameObject("BRAXHORN ruch aut (trasy, pierwszeństwo, auta)"); mg.transform.SetParent(Group(group), false);
        var ra = mg.AddComponent<BraxRuchAut>();
        ra.punkty = wy.punkty.ToArray(); ra.trasy = wy.trasy.ToArray(); ra.zdarzenia = wy.zdarzenia.ToArray(); ra.wezly = wy.wezly.ToArray(); ra.ziarno = 2026;
        ra.vZakret = 15f / 3.6f * K;   // E3d/e: zakręty ~15 km/h × K
        // 3) auta
        var rnd = new System.Random(2026); var pula = PulaAut(szablony); var klakson = Klakson();
        var auta = new List<BraxAuto>(); var opis = new Dictionary<string, int>();
        for (int r = 0; r < wy.trasy.Count; r++)
        {
            int ile = BraxTrasyAut.AutNaTrasie(wy.trasy[r].nazwa);
            for (int k = 0; k < ile; k++)
            {
                var sz = pula[rnd.Next(pula.Count)];
                var o = AutoInstancja(sz, Group(group), "Auto w ruchu · " + sz.id + (AutaNC.Contains(sz.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), rnd, out var kolor);
                var a = o.AddComponent<BraxAuto>();
                a.trasaNr = r; a.sStart = wy.trasy[r].dl * (k + .3f) / ile; a.trasa = PunktyTrasy(wy, r);
                a.vmax = (wy.trasy[r].obszar == 0 ? 7.6f : 8.2f) + (float)rnd.NextDouble() * 1.8f;   // 27–36 km/h na osiedlu
                a.dl = sz.dl; a.szer = sz.szerNadw + .04f; a.rozstaw = sz.rozstaw; a.osTyl = sz.osTyl; a.promienKola = sz.promienKola;
                a.nadwozie = o.transform.Find("nadwozie");
                a.kola = new[] { "LP", "PP", "LT", "PT" }.Select(n => o.transform.Find("koło " + n + "/obrót")).Where(t => t).ToArray();
                a.skret = new[] { "LP", "PP" }.Select(n => o.transform.Find("koło " + n)).Where(t => t).ToArray();
                if (a.nadwozie)
                {
                    a.stop = Szukaj(a.nadwozie, "światło STOP L", "światło STOP P");
                    a.migL = Szukaj(a.nadwozie, "kierunkowskaz tył L", "kierunkowskaz przód L");
                    a.migP = Szukaj(a.nadwozie, "kierunkowskaz tył P", "kierunkowskaz przód P");
                    var d = a.nadwozie.Find("spaliny"); if (d) { var kl = new List<Transform>(); foreach (Transform t in d) kl.Add(t); a.dym = kl.ToArray(); a.dymSkala = K; }
                }
                a.klakson = klakson;
                var bc = o.AddComponent<BoxCollider>(); bc.center = sz.obrys.center; bc.size = sz.obrys.size;
                var rb = o.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                LodAuta(o, .015f);
                auta.Add(a);
                string key = sz.id + " " + kolor; opis[key] = opis.TryGetValue(key, out var x) ? x + 1 : 1;
            }
        }
        ra.auta = auta.ToArray();
        float szerMax = auta.Max(a => a.szer), dlMax = auta.Max(a => a.dl);
        // 4) położenia startowe (te same, które policzy gra) — scena wygląda tak samo przed uruchomieniem
        var s0 = new BraxRuchAutSym(ra.punkty, ra.trasy, ra.zdarzenia, ra.wezly, ra.ziarno, szerMax, dlMax, ra.vZakret);
        foreach (var a in auta) s0.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax);
        s0.UstawStart();
        for (int i = 0; i < auta.Count; i++) auta[i].transform.SetPositionAndRotation(s0.auta[i].poz, Quaternion.LookRotation(s0.auta[i].kier));
        rep.notes.Add("auta w ruchu: " + auta.Count + " na " + wy.trasy.Count + " trasach (" + string.Join(", ", opis.Select(kv => kv.Key + " ×" + kv.Value)) + ")");
        // 5) test ruchu: 10 min czasu gry krokiem 0,1 s, bez renderu
        var st = new BraxRuchAutSym(ra.punkty, ra.trasy, ra.zdarzenia, ra.wezly, ra.ziarno, szerMax, dlMax, ra.vZakret);
        foreach (var a in auta) st.Dodaj(a.trasaNr, a.sStart, a.dl, a.szer, a.rozstaw, a.osTyl, a.vmax);
        st.UstawStart();
        var zdarz = new List<string>(); st.naZakleszczenie = o => { if (zdarz.Count < 8) zdarz.Add(o); };
        var t0 = DateTime.Now;
        var wt = st.Test(600f, .1f, NaJezdniAut);
        string wynik = "BRAXHORN_RUCH_TEST zakleszczenia=" + wt.zakleszczenia + " nachodzenia=" + wt.nachodzenia + " poza_droga=" + wt.pozaDroga +
                       " (aut " + auta.Count + ", 600 s krokiem 0,1 s, przejazdy skrzyżowań " + wt.przejazdyWezlow + ", nowe przejazdy z tunelu/ulicy B " + wt.nowePrzejazdy +
                       ", śr. prędkość " + (wt.sredniaV * 3.6f).ToString("F0") + " km/h, stanie na auto " + wt.czasStania.ToString("F0") + " s, par kolidujących ruchów " + wt.konflikty +
                       ", zakręty: limit " + (ra.vZakret * 3.6f).ToString("F1") + " km/h (15 × K), maks. " + (wt.vZakretMax * 3.6f).ToString("F1") + " km/h, za_szybko=" + wt.zaSzybkoWZakrecie + "/" + wt.probkiZakretu + " próbek" +
                       ", czas testu " + (DateTime.Now - t0).TotalSeconds.ToString("F1") + " s)";
        Debug.Log(wynik); rep.notes.Add(wynik.Replace("BRAXHORN_RUCH_TEST ", "test ruchu: "));
        foreach (var z in zdarz) Debug.Log("BRAXHORN_RUCH_TEST_ZDARZENIE " + z);
        if(wt.nachodzenia>0)Debug.LogError("BRAXHORN_RUCH_TEST DO POPRAWKI: test wykrył "+wt.nachodzenia+" przecięć samochodów");
    }

    // Punkty trasy co ~3 m (pole BraxAuto.trasa — czytane przez MENU → UTKNĄŁEM jako „droga”); trasa otwarta: tam i z powrotem,
    // żeby odcinek „ostatni → pierwszy” nie przecinał posesji.
    static Vector3[] PunktyTrasy(BraxTrasyAut.Wynik wy, int r)
    {
        var t = wy.trasy[r]; int co = Mathf.Max(1, Mathf.RoundToInt(3f / Mathf.Max(.1f, t.krok)));
        var l = new List<Vector3>();
        for (int i = 0; i < t.ile; i += co) l.Add(wy.punkty[t.start + i]);
        if (!t.petla) { l.Add(wy.punkty[t.start + t.ile - 1]); for (int i = l.Count - 2; i >= 0; i--) l.Add(l[i]); }
        return l.ToArray();
    }

    static void AutaZaparkowane(List<AutoSzablon> szablony)
    {
        // zatoki: z etapu dróg; zapas — dotychczasowe miejsca (miejscaA, yaw = kierunek do ściany/krawężnika) → przodem do alejki
        var miejsca = new List<(Vector3 c, Vector3 przod, float dl, float szer, bool sciana)>();
        bool zDrog = MiejscaParkingowe.Count > 0;
        if (zDrog)
            foreach (var m in MiejscaParkingowe)
            {
                var f = m.przod; f.y = 0f; if (f.sqrMagnitude < 1e-4f) continue;
                miejsca.Add((m.srodek, f.normalized, m.rozmiar.y > 1f ? m.rozmiar.y : 5f, m.rozmiar.x > 1f ? m.rozmiar.x : 2.5f, m.przyScianie));
            }
        else
            // (DrogiA bazy 14: yaw miejsca = kierunek do krawędzi/ściany → auto przodem do alejki = odwrotnie)
            foreach (var m in miejscaA) { var doSciany = Quaternion.Euler(0f, m.yaw, 0f) * Vector3.forward; miejsca.Add((m.p, -doSciany, 5f, 2.5f, true)); }
        Physics.SyncTransforms();
        var rnd = new System.Random(1402); var pula = PulaAut(szablony);
        var stoja = new List<(Vector3 c, Vector3 f, float hw, float hl)>();
        int ok = 0, wolne = 0, odrzSciana = 0, odrzAuto = 0, za = 0; var kolory = new Dictionary<string, int>(); var modele = new Dictionary<string, int>();
        int nrM = 0;
        foreach (var m in miejsca)
        {
            // 3.11 15:11 („między samochodami ma być przejście, wszędzie, na każdym parkingu”): zajęte co trzecie miejsce —
            // każde auto ma obok wolną zatokę (przejście do drzwi), także auto Patryka stawiane później na wolnym miejscu
            if (nrM++ % 3 != 0) { wolne++; continue; }   // ok. 45 % miejsc wolnych = zajętość ~55 % (audyt 1.11: 40–60 %, parkingi były przepełnione; mniej ciężkich modeli dla telefonu)
            AutoSzablon sz = null; Vector3 c = default;
            for (int proba = 0; proba < 8 && sz == null; proba++)
            {
                var k = pula[rnd.Next(pula.Count)];
                if (k.dl > m.dl + .2f || k.szerNadw > m.szer - .15f) { za++; continue; }
                // położenie w osi zatoki: środek, a gdy z przodu lub z tyłu jest ściana/słupek — przesunięcie o 0,2–0,6 m,
                // tak by z obu końców zostało ≥ 0,5 m (fizyka od 0,3 m w górę — krawężniki, odbojnice i płyty są niżej)
                var rot = Quaternion.LookRotation(m.przod);
                var half = new Vector3(k.szer * .5f + .05f, Mathf.Max(.2f, (k.wys - .3f) * .5f), k.dl * .5f + .5f);
                bool wolneMiejsce = false; Vector3 cc = m.c;
                foreach (float off in new[] { 0f, .2f, -.2f, .4f, -.4f, .6f, -.6f })
                {
                    cc = m.c + m.przod * off;
                    if (!Physics.CheckBox(cc + Vector3.up * (.3f + half.y), half, rot, ~0, QueryTriggerInteraction.Ignore)) { wolneMiejsce = true; break; }
                }
                if (!wolneMiejsce) { odrzSciana++; continue; }
                if (stoja.Any(q => BraxRuchAutSym.Prost(cc, m.przod, k.szer * .5f + .05f, k.dl * .5f + .05f, q.c, q.f, q.hw, q.hl))) { odrzAuto++; continue; }
                sz = k; c = cc;
            }
            if (sz == null) continue;
            var o = AutoInstancja(sz, Group(group), "Auto zaparkowane · " + sz.id + (AutaNC.Contains(sz.id) ? " (DO WYMIANY PRZED SPRZEDAŻĄ)" : ""), rnd, out var kolor);
            o.transform.SetPositionAndRotation(new Vector3(c.x, m.c.y, c.z), Quaternion.LookRotation(m.przod));
            // stoi z wyłączonym silnikiem: bez spalin i świateł
            var nad = o.transform.Find("nadwozie");
            if (nad)
                foreach (var t in nad.Cast<Transform>().Where(t => t.name == "spaliny" || t.name.StartsWith("światło") || t.name.StartsWith("kierunkowskaz")).ToList())
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            var bc = o.AddComponent<BoxCollider>(); bc.center = sz.obrys.center; bc.size = sz.obrys.size;
            LodAuta(o, .02f);
            stoja.Add((c, m.przod, sz.szer * .5f, sz.dl * .5f));
            Physics.SyncTransforms();
            ok++; kolory[kolor] = kolory.TryGetValue(kolor, out var x) ? x + 1 : 1; modele[sz.id] = modele.TryGetValue(sz.id, out var y) ? y + 1 : 1;
        }
        string opis = "BRAXHORN_AUTA_PARKING miejsc " + miejsca.Count + " (" + (zDrog ? "MiejscaParkingowe z etapu dróg" : "zapas: dotychczasowe miejsca DrogiA") + "), aut " + ok +
                      ", wolnych " + wolne + ", odrzucone: ściana/latarnia/słupek " + odrzSciana + ", inne auto " + odrzAuto + ", za długie/szerokie " + za +
                      "; modele: " + string.Join(", ", modele.Select(kv => kv.Key + " ×" + kv.Value)) + "; kolory: " + string.Join(", ", kolory.Select(kv => kv.Key + " ×" + kv.Value));
        Debug.Log(opis); rep.notes.Add(opis.Replace("BRAXHORN_AUTA_PARKING ", "auta zaparkowane: "));
    }
}

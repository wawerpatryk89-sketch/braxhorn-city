using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// LUDZIE NA OSIEDLU — BUILD 14, część 2: GDZIE są ludzie (paczka i fabryka osoby: BraxClaudeOsiedleLudzie.cs).
// Etap wołany NA KOŃCU budowy osiedla (hak w Build: „ludzie: przechodnie, obsada, kierowcy”) — wtedy stoją już
// wnętrza, ławki, auta (fotele kierowców) i drogi z przejściami.
//  • PRZECHODNIE (×3 względem 13, parametr GestoscOsiedle): sieć chodników — osie chodników osiedla A (DrogiA),
//    ulic osiedla B i alejek parku (plac fontanny dookoła), węzły co ~4 m, połączenia na narożnikach (bez przecinania
//    jezdni i przeszkód), przez jezdnię tylko po przejściach: BraxClaudeOsiedle.Przejscia (etap dróg); gdy lista pusta —
//    zebry z układu ulic (jak w DrogiA) i dwie zebry osiedla B. Każdy przechodzień: losowy spacer po sieci 220–520 m
//    (prosto chętniej niż w bok), tam i z powrotem; różne postacie, wzrost, odcienie ubrań, akcesoria.
//  • ŁAWKI: część zajęta (1–2 osoby siedzące, klip siedzenia na krześle). PARY rozmawiające z gestami na placach,
//    pojedynczo z telefonem / kawą.
//  • OBSADA (podstawowa w 14): za ladami (recepcja siłowni, kasa i zakłady kasyna, dyżurny komisariatu, Shake Bar,
//    kawiarnia ×2, kasa marketu, bar i szatnia dyskoteki), ochroniarz przed wejściem do klubu, 2 policjantów, lekarz
//    i pielęgniarka w szpitalu, tańczący na parkiecie i goście przy barze klubu. Pozycje z układu wnętrz generatora,
//    każda sprawdzona (wolne miejsce, podłoga) — gdy zajęte albo brak — pominięta z notatką (nic nie burzymy).
//  • KIEROWCY: każdy węzeł „BRAXHORN fotel kierowcy” w aucie w ruchu (BraxAuto; auta — inny etap) dostaje kierowcę (klip siedzenia przy stole,
//    ramiona lekko w górę do kierownicy — obrót po animacji), czasem pasażera w „BRAXHORN fotel pasażera”.
//    Fotel = środek powierzchni siedziska, przód = kierunek jazdy. Brak foteli = brak kierowców (bez błędu).
public static partial class BraxClaudeOsiedle
{
    const float GranicaObszarow = -101.5f;   // podział osiedla na 2 sceny (BraxCityBuild: z < −101.5 = park i osiedle B)

    // Hak w Build (jedna linia): wszystko o ludziach Rocketbox.
    static void LudzieNaOsiedlu()
    {
        if (!LadujRocketbox())
        {
            rep.notes.Add("ludzie Rocketbox: brak paczki (unity-tools/claude/fetch_rocketbox.py) — zostają przechodnie z build 13");
            Debug.Log("BRAXHORN_LUDZIE postacie=0 przechodnie=0 obsada=0 kierowcy=0 bledy_kosci=0 tpoza=0 (brak paczki Rocketbox)");
            return;
        }
        Physics.SyncTransforms();
        var rnd = new System.Random(1414);
        wzrostyM.Clear(); wzrostyK.Clear();
        group = "Ludzie (Rocketbox)";
        // najpierw ludzie w miejscu (sprawdzanie wolnego miejsca nie widzi jeszcze kolizji przechodniów), na końcu przechodnie
        Guard("ludzie: obsada lokali", () => ObsadaLokali(rnd));
        Guard("ludzie: pary i pojedynczo na placach", () => LudzieNaPlacach(rnd));
        Guard("ludzie: ławki", () => LudzieNaLawkach(rnd));
        KabinyReset();
        Guard("ludzie: kierowcy i pasażerowie", () => KierowcyAut(rnd));
        Guard("ludzie: wsiadanie i wysiadanie na parkingach", () => PodrozniParkingow(rnd));
        Guard("ludzie: kabiny — podsumowanie", () => rep.notes.Add(KabinyPodsumowanie()));
        Guard("ludzie: przechodnie", () => PrzechodnieRocketbox(rnd));
        Debug.Log("BRAXHORN_LUDZIE_KOPIE_KLIPOW " + kopieKlipow + " (oczekiwane 0: klip nazwany jak plik; >0 = wspólne kopie .anim, nie w scenie)");
        string msg = "BRAXHORN_LUDZIE postacie=" + rbPostacie.Count + " przechodnie=" + rbPrzechodnie + " obsada=" + rbObsada + " kierowcy=" + rbKierowcy +
                     " bledy_kosci=" + rbBledyKosci + " tpoza=" + rbTpoza + " siedzacy=" + rbSiedzacy + " pary=" + rbPary + " inni=" + rbInni + " razem=" + rbLudzie +
                     " klipy=" + rbKlipy.Count + " wzrost_bohatera=" + WzrostBohatera.ToString("F2");
        Debug.Log(msg); rep.notes.Add(msg);
        string Stat(List<float> l) => l.Count == 0 ? "—" : "n=" + l.Count + " śr " + l.Average().ToString("F3") + " min " + l.Min().ToString("F3") + " max " + l.Max().ToString("F3");
        {   // E19 (build 45): stan wydajności postaci i aut w scenie (koszt runtime ograniczany odległością w BraxLudzie / BraxAuto)
            int ludzie = UnityEngine.Object.FindObjectsByType<BraxMieszkaniec>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + UnityEngine.Object.FindObjectsByType<BraxPieszy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int skinned = UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var auta = UnityEngine.Object.FindObjectsByType<BraxAuto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("BRAXHORN_WYDAJNOSC ludzie=" + ludzie + " skinned=" + skinned + " auta=" + auta.Length + " zrodla_audio_silnikow=" + auta.Count(a => a.silnik) + " (grają najbliższe " + BraxAuto.SilnikiMax + ")"
                + " LOD_ludzi=nie (cull: animacja do " + BraxLudzie.PromienMax + " m, limit " + BraxLudzie.LimitMax + "→" + BraxLudzie.LimitMin + " przy < 35 kl./s) cienie_postaci_zasieg=" + BraxLudzie.CienOdl.ToString("F1") + " m"
                + " skóra_2_kości_od=" + BraxLudzie.DwieKosciOdl.ToString("F1") + " m cienie_aut_zasieg=" + BraxAuto.CienAutaOdl.ToString("F1") + " m");
        }
        try
        {   // E4b (build 45): rozmowy — kwestie wg sytuacji (indeks nagrań), kto zagaduje, limity
            string json = File.Exists("Assets/Resources/OsiedleGlosy/indeks.json") ? File.ReadAllText("Assets/Resources/OsiedleGlosy/indeks.json") : "";
            var syt = System.Text.RegularExpressions.Regex.Matches(json, "\"sytuacja\": *\"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>().GroupBy(m => m.Groups[1].Value).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count());
            int kw = System.Text.RegularExpressions.Regex.Matches(json, "\"tekst\":").Count;
            var piesi = UnityEngine.Object.FindObjectsByType<BraxPieszy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int zag = piesi.Count(p => BraxLudzie.Zagadujacy(BraxLudzie.Ziarno(p.name, p.transform.position)));
            Debug.Log("BRAXHORN_ROZMOWY kwestii=" + kw + " wg sytuacji: " + string.Join(", ", syt) + " | zagadujący=" + (piesi.Length > 0 ? (100f * zag / piesi.Length).ToString("F0") : "—") + "% (" + zag + "/" + piesi.Length
                + " przechodniów; obsługa wita zawsze, z limitem) cooldown=osoba " + BraxLudzie.CooldownOsoby + " s, globalny " + BraxLudzie.CooldownGlobalny + " s, nie gdy gracz > " + BraxLudzie.GraczBiegnie + " m/s albo w aucie"
                + " | głośność: wyraźnie do " + BraxGlosPostaci.Wyraznie.ToString("F1") + " m, cisza od " + BraxGlosPostaci.Cisza.ToString("F1") + " m, rozmowa 1.0 / mijanie 0.7 / tło 0.45");
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_ROZMOWY DO POPRAWKI: " + e.Message); }
        Debug.Log("BRAXHORN_LUDZIE_REKWIZYTY plecaki=0 (klocek z 3 brył bez szelek wyłączony; pominięto " + rbPlecakiPominiete + " losowań) torby=zaokrąglone z paskiem, kubki/telefony/czapki/okulary bez zmian");
        Debug.Log("BRAXHORN_SKALA ludzie: k świata=" + K.ToString("F3") + " baza ludzi=" + WzrostBazaLudzi.ToString("F2") + " m (#44: 2.27) mężczyźni " + Stat(wzrostyM) + " kobiety " + Stat(wzrostyK)
            + " (oczekiwane: mężczyźni 2.202–2.270, kobiety 2.145–2.213)");
    }
    static readonly List<float> wzrostyM = new List<float>(), wzrostyK = new List<float>();
    static List<float> WzrostyLudzi(bool kobieta) => kobieta ? wzrostyK : wzrostyM;

    // ---------------- sieć chodników ----------------
    sealed class SiecPieszych
    {
        public readonly List<Vector3> w = new List<Vector3>();
        public readonly List<List<(int j, bool przez)>> sas = new List<List<(int, bool)>>();
        public readonly List<int> segOd = new List<int>();     // numer odcinka (węzły tego samego odcinka nie łączą się na narożnikach)
        public readonly HashSet<int> konce = new HashSet<int>(), zebry = new HashSet<int>();
        public int Dodaj(Vector3 p, int seg) { w.Add(p); sas.Add(new List<(int, bool)>()); segOd.Add(seg); return w.Count - 1; }
        public void Polacz(int a, int b, bool przez)
        {
            if (a == b) return;
            if (!sas[a].Any(e => e.j == b)) sas[a].Add((b, przez));
            if (!sas[b].Any(e => e.j == a)) sas[b].Add((a, przez));
        }
    }

    static bool NaJezdni(Vector3 p)
    {
        if (ulice != null)
            foreach (var u in ulice)
            {
                float wz = u.alongX ? p.z : p.x, wd = u.alongX ? p.x : p.z;
                if (Mathf.Abs(wz - u.c) < u.cw + u.kerb - .05f && wd > u.a0 - .5f && wd < u.a1 + .5f) return true;
            }
        foreach (var s in SkrzyzowaniaInfo.Count > 0 ? SkrzyzowaniaInfo.Where(q => q.rodzaj == RodzajSkrzyzowania.Rondo).Select(q => (q.srodek, q.promien)) : new[] { (new Vector3(0f, 0f, 39f), 12.9f) })
        {
            var d = p - s.Item1; d.y = 0f; if (d.magnitude < s.Item2) return true;
        }
        // osiedle B: ulica główna W-E i ulica N-S (od parku do ulicy głównej)
        if (Mathf.Abs(p.z - StreetZ) < Carriage + Kerb - .05f && p.x > X0 && p.x < X1) return true;
        if (Mathf.Abs(p.x - AxisX) < Carriage + Kerb - .05f && p.z < ParkZ0 - 1f && p.z > StreetZ) return true;
        return false;
    }
    static bool PrzezJezdnie(Vector3 a, Vector3 b)
    {
        int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / .4f));
        for (int k = 0; k <= n; k++) if (NaJezdni(Vector3.Lerp(a, b, k / (float)n))) return true;
        return false;
    }
    static bool Wolne(Vector3 p, float r = .22f) => !Physics.CheckCapsule(p + Vector3.up * (r + .25f), p + Vector3.up * 1.6f, r, ~0, QueryTriggerInteraction.Ignore);
    static bool WidacProsto(Vector3 a, Vector3 b) => !Physics.Linecast(a + Vector3.up * .9f, b + Vector3.up * .9f, ~0, QueryTriggerInteraction.Ignore);

    static SiecPieszych BudujSiec()
    {
        var s = new SiecPieszych();
        var odc = new List<(Vector3 a, Vector3 b)>();
        if (chodnikiA != null) odc.AddRange(chodnikiA);
        // osiedle B: osie chodników ulicy głównej (przerwa na ulicę N-S) i ulicy N-S
        float wB = Carriage + Kerb + Green + Walk * .5f;
        foreach (int sd in new[] { -1, 1 })
        {
            odc.Add((new Vector3(X0 + 8f, .14f, StreetZ + sd * wB), new Vector3(AxisX - 10f, .14f, StreetZ + sd * wB)));
            odc.Add((new Vector3(AxisX + 10f, .14f, StreetZ + sd * wB), new Vector3(X1 - 8f, .14f, StreetZ + sd * wB)));
            // N-S do samej linii chodnika ulicy głównej (narożnik) — wcześniej kończył się 3 m wyżej, przy końcu płotu działek
            odc.Add((new Vector3(AxisX + sd * wB, .14f, ParkZ0 - 2f), new Vector3(AxisX + sd * wB, .14f, StreetZ + wB)));
        }
        // park: aleja N-S (od bramy osiedla A do placu fontanny i dalej do wejścia od B), aleja W-E, ścieżka do drugiej bramy
        float zc = (ParkZ0 + FenceA - .6f) * .5f, rPl = 9f;
        odc.Add((new Vector3(AxisX + 1.2f, .04f, FenceA - 1.6f), new Vector3(AxisX + 1.2f, .04f, zc + rPl)));
        odc.Add((new Vector3(AxisX - 1.2f, .04f, zc - rPl), new Vector3(AxisX - 1.2f, .04f, ParkZ0 + 3f)));
        odc.Add((new Vector3(X0 + 6f, .04f, zc - .6f), new Vector3(AxisX - rPl, .04f, zc - .6f)));
        odc.Add((new Vector3(AxisX + rPl, .04f, zc + .6f), new Vector3(X1 - 6f, .04f, zc + .6f)));
        odc.Add((new Vector3(30f, .04f, zc + 2.4f), new Vector3(30f, .04f, FenceA - 1.6f)));
        int seg = 0, odrzucone = 0;
        foreach (var (a, b) in odc)
        {
            float len = Vector3.Distance(a, b); if (len < 3f) continue;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 4f));
            var pts = Enumerable.Range(0, n + 1).Select(k => Vector3.Lerp(a, b, k / (float)n)).ToList();
            int zle = pts.Count(q => !Wolne(q)), jezdnia = pts.Count(NaJezdni);
            bool przezGranice = (a.z - GranicaObszarow) * (b.z - GranicaObszarow) < 0f;
            if (zle > pts.Count * .4f || jezdnia > 0 || przezGranice) { odrzucone++; continue; }
            int prev = -1;
            foreach (var q in pts) { int id = s.Dodaj(q, seg); if (prev >= 0) s.Polacz(prev, id, false); prev = id; }
            s.konce.Add(s.w.Count - pts.Count); s.konce.Add(s.w.Count - 1);
            seg++;
        }
        // plac fontanny: pierścień wokół misy (między rabatami), połączony z alejami
        {
            var fc = new Vector3(AxisX, .05f, zc); int first = -1, prev = -1; const int N = 14;
            for (int k = 0; k < N; k++)
            {
                float a = k * Mathf.PI * 2f / N; var p = fc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 6.4f;
                int id = s.Dodaj(p, seg); s.konce.Add(id);
                if (prev >= 0) s.Polacz(prev, id, false); else first = id; prev = id;
            }
            if (first >= 0) s.Polacz(prev, first, false);
            seg++;
        }
        // narożniki: koniec odcinka ↔ najbliższy węzeł innego odcinka (do 8 m), bez jezdni i przeszkody pomiędzy
        int narozniki = 0;
        foreach (int e in s.konce.ToList())
        {
            var pe = s.w[e];
            var kand = Enumerable.Range(0, s.w.Count).Where(j => s.segOd[j] != s.segOd[e] && (s.w[j] - pe).sqrMagnitude < 64f)
                                 .OrderBy(j => (s.w[j] - pe).sqrMagnitude).Take(3);
            foreach (int j in kand)
            {
                bool granica = (pe.z - GranicaObszarow) * (s.w[j].z - GranicaObszarow) < 0f;
                if (granica || PrzezJezdnie(pe, s.w[j]) || !WidacProsto(pe, s.w[j])) continue;
                s.Polacz(e, j, false); narozniki++; break;
            }
        }
        // przejścia przez jezdnię: wspólna lista etapu dróg; gdy pusta — zebry z układu ulic
        var przejscia = Przejscia.Count > 0 ? Przejscia.ToList() : ZebryZUkladu();
        int zebry = 0;
        foreach (var pz in przejscia)
        {
            var wz = pz.wzdluzJezdni; wz.y = 0f; if (wz.sqrMagnitude < 1e-4f) continue; wz.Normalize();
            var p = Vector3.Cross(Vector3.up, wz); float half = Mathf.Max(1.5f, pz.szerokoscJezdni * .5f);
            var srodek = pz.srodek; float y = Mathf.Max(.02f, srodek.y);
            Vector3 c1 = srodek + p * (half - .15f), c2 = srodek - p * (half - .15f); c1.y = c2.y = y;
            int n1 = NajblizszyPoStronie(s, c1, srodek, p, half), n2 = NajblizszyPoStronie(s, c2, srodek, -p, half);
            if (n1 < 0 || n2 < 0) continue;
            int i1 = s.Dodaj(c1, -1), i2 = s.Dodaj(c2, -1);
            s.Polacz(n1, i1, false); s.Polacz(i1, i2, true); s.Polacz(i2, n2, false);
            s.zebry.Add(i1); s.zebry.Add(i2); zebry++;
        }
        rep.notes.Add("ludzie: sieć chodników — węzłów " + s.w.Count + ", odcinków " + seg + " (odrzuconych " + odrzucone + "), narożników " + narozniki +
                      ", przejść " + zebry + "/" + przejscia.Count + (Przejscia.Count > 0 ? " (z etapu dróg)" : " (zebry z układu ulic — lista Przejscia pusta)"));
        return s;
    }

    static int NajblizszyPoStronie(SiecPieszych s, Vector3 c, Vector3 srodek, Vector3 strona, float half)
    {
        int best = -1; float bd = 5.5f * 5.5f;
        for (int j = 0; j < s.w.Count; j++)
        {
            if (s.segOd[j] < 0) continue;
            var d = s.w[j] - c; d.y = 0f; float m = d.sqrMagnitude; if (m > bd) continue;
            var o = s.w[j] - srodek; o.y = 0f; if (Vector3.Dot(o, strona) < half - .3f) continue;     // ta sama strona jezdni
            if ((s.w[j].z - GranicaObszarow) * (c.z - GranicaObszarow) < 0f) continue;
            if (PrzezJezdnie(s.w[j], c + (c - srodek).normalized * .35f)) continue;
            best = j; bd = m;
        }
        return best;
    }

    // Zebry z układu ulic (jak w DrogiA.Skrzyzowanie: na każdym ramieniu w linii chodnika ulicy poprzecznej) + dwie w B.
    static List<Przejscie> ZebryZUkladu()
    {
        var l = new List<Przejscie>();
        if (ulice != null)
            foreach (var ux in ulice.Where(q => q.alongX))
                foreach (var uz in ulice.Where(q => !q.alongX))
                {
                    if (!(uz.c >= ux.a0 - .01f && uz.c <= ux.a1 + .01f && ux.c >= uz.a0 - .01f && ux.c <= uz.a1 + .01f)) continue;
                    if (Mathf.Abs(uz.c) < .1f && Mathf.Abs(ux.c - 39f) < .1f) continue;    // rondo
                    float x = uz.c, z = ux.c;
                    bool E = ux.a1 > x + .5f, W = ux.a0 < x - .5f, N = uz.a1 > z + .5f, S = uz.a0 < z - .5f;
                    foreach (int sz in new[] { -1, 1 })
                    {
                        if (!(sz > 0 ? N : S)) continue;
                        float k = ux.cw + ux.kerb, h = sz > 0 ? ux.HalfPos : ux.HalfNeg; if (h - k < 1.5f) continue;
                        l.Add(new Przejscie { srodek = new Vector3(x, .06f, z + sz * (k + h) * .5f), wzdluzJezdni = Vector3.forward, szerokoscJezdni = uz.cw * 2f });
                    }
                    foreach (int sx in new[] { -1, 1 })
                    {
                        if (!(sx > 0 ? E : W)) continue;
                        float k = uz.cw + uz.kerb, h = sx > 0 ? uz.HalfPos : uz.HalfNeg; if (h - k < 1.5f) continue;
                        l.Add(new Przejscie { srodek = new Vector3(x + sx * (k + h) * .5f, .06f, z), wzdluzJezdni = Vector3.right, szerokoscJezdni = ux.cw * 2f });
                    }
                }
        l.Add(new Przejscie { srodek = new Vector3(AxisX, .06f, StreetZ + Carriage + 1.7f), wzdluzJezdni = Vector3.forward, szerokoscJezdni = Carriage * 2f });
        l.Add(new Przejscie { srodek = new Vector3(AxisX + 10.5f, .06f, StreetZ), wzdluzJezdni = Vector3.right, szerokoscJezdni = Carriage * 2f });
        return l;
    }

    // Losowy spacer po sieci: prosto chętniej niż w bok, bez zawracania; przejścia jako odcinki „przez jezdnię”.
    static (Vector3[] pts, bool[] prz) Trasa(SiecPieszych s, int start, System.Random rnd)
    {
        var pts = new List<Vector3> { s.w[start] }; var prz = new List<bool>();
        int cur = start, prev = -1; float len = 0f, cel = Mathf.Lerp(220f, 520f, (float)rnd.NextDouble());
        for (int krok = 0; krok < 500 && len < cel; krok++)
        {
            var opcje = s.sas[cur].Where(e => e.j != prev).ToList(); if (opcje.Count == 0) opcje = s.sas[cur].ToList(); if (opcje.Count == 0) break;
            var dPrev = prev >= 0 ? s.w[cur] - s.w[prev] : Vector3.zero; dPrev.y = 0f;
            var wagi = opcje.Select(e =>
            {
                var d = s.w[e.j] - s.w[cur]; d.y = 0f;
                float ang = dPrev.sqrMagnitude > 1e-4f && d.sqrMagnitude > 1e-4f ? Vector3.Angle(dPrev, d) : 0f;
                float w = ang < 30f ? 4f : ang < 100f ? 1.4f : .5f;
                if (s.zebry.Contains(e.j)) w *= 1.3f;
                return w;
            }).ToList();
            float sum = wagi.Sum(), r = (float)rnd.NextDouble() * sum; int wyb = 0;
            for (int k = 0; k < wagi.Count; k++) { r -= wagi[k]; if (r <= 0f) { wyb = k; break; } }
            var ed = opcje[wyb];
            len += Vector3.Distance(s.w[cur], s.w[ed.j]);
            prz.Add(ed.przez); pts.Add(s.w[ed.j]);
            prev = cur; cur = ed.j;
        }
        // uproszczenie: punkty pośrednie na prostej (ten sam rodzaj odcinka) out
        for (int k = pts.Count - 2; k >= 1; k--)
        {
            if (prz[k - 1] || prz[k]) continue;
            var a = pts[k] - pts[k - 1]; var b = pts[k + 1] - pts[k]; a.y = b.y = 0f;
            if (a.sqrMagnitude > 1e-4f && b.sqrMagnitude > 1e-4f && Vector3.Angle(a, b) < 3f && Mathf.Abs(pts[k].y - pts[k - 1].y) < .02f) { pts.RemoveAt(k); prz.RemoveAt(k); }
        }
        var arr = new bool[pts.Count]; for (int k = 0; k < prz.Count; k++) arr[k] = prz[k];
        return (pts.ToArray(), arr);
    }

    static void PrzechodnieRocketbox(System.Random rnd)
    {
        var s = BudujSiec();
        int ile = Mathf.RoundToInt(66 * GestoscOsiedle);         // build 13: 22 przechodniów → ×3 (właściciel 21:01)
        var start = Enumerable.Range(0, s.w.Count).Where(j => s.segOd[j] >= 0 && s.sas[j].Count > 0 && Wolne(s.w[j])).ToList();
        var A = start.Where(j => s.w[j].z > GranicaObszarow).ToList(); var B = start.Where(j => s.w[j].z <= GranicaObszarow).ToList();
        if (start.Count == 0) { rep.notes.Add("DO POPRAWKI: ludzie — sieć chodników pusta, brak przechodniów"); return; }
        var gA = Group("Ludzie / przechodnie osiedle A"); var gB = Group("Ludzie / przechodnie park i osiedle B");
        for (int k = 0; k < ile; k++)
        {
            var pula = (k % 5 < 3 && A.Count > 0) || B.Count == 0 ? A : B;
            int st = pula[rnd.Next(pula.Count)];
            var (pts, prz) = Trasa(s, st, rnd);
            if (pts.Length < 2) continue;
            NowyPrzechodzien(s.w[st].z > GranicaObszarow ? gA : gB, pts, prz, rnd, k + 1);
        }
        rep.notes.Add("ludzie: przechodniów " + rbPrzechodnie + " (osiedle A " + gA.childCount + ", park i B " + gB.childCount + ")");
    }

    // ---------------- ławki ----------------
    static void LudzieNaLawkach(System.Random rnd)
    {
        var g = Group("Ludzie / na ławkach");
        var lawki = new List<(Vector3 c, Vector3 F, Vector3 R, float y, float pol)>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.name == "modular_street_seating")
            {
                var rs = t.GetComponentsInChildren<MeshRenderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var F = Quaternion.Euler(0, t.eulerAngles.y, 0) * Vector3.forward;
                F = PrzodSiedziska(rs, b, F);
                float y = b.min.y + Mathf.Clamp(b.size.y * .55f, .38f, .5f);
                lawki.Add((new Vector3(b.center.x, 0, b.center.z) + F * .06f, F, Vector3.Cross(Vector3.up, F), y, .55f));
            }
            else if (t.name == "ławka kolizja")
            {
                var F = t.rotation * Vector3.forward; F.y = 0f; F.Normalize();
                lawki.Add((new Vector3(t.position.x, 0, t.position.z) + F * .06f, F, Vector3.Cross(Vector3.up, F), t.position.y + .09f, .5f));
            }
        }
        int cel = Mathf.RoundToInt(20 * GestoscOsiedle), n = 0;
        foreach (var l in lawki.OrderBy(_ => rnd.Next()))
        {
            if (n >= cel) break;
            if (Mathf.Abs(l.c.z - GranicaObszarow) < 3f || rnd.NextDouble() < .35) continue;
            int ile = rnd.NextDouble() < .4 ? 2 : 1;
            var miejsca = ile == 2 ? new[] { -l.pol, l.pol } : new[] { rnd.NextDouble() < .5 ? -l.pol : l.pol };
            foreach (float o in miejsca)
            {
                var p = l.c + l.R * o; p.y = l.y;
                var m = Siedzacy(rnd.NextDouble() < .15 ? RolaImpreza : RolaPrzechodzien, g, "Na ławce", p, l.F, rnd, new[] { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" });
                if (m) n++;
            }
        }
        rep.notes.Add("ludzie: na ławkach " + n + " (ławek znalezionych " + lawki.Count + ")");
    }

    // Strona bez oparcia: w połowie bryły od strony siedzącego wierzchołki są niżej (siatki do odczytu); inaczej yaw.
    static Vector3 PrzodSiedziska(MeshRenderer[] rs, Bounds b, Vector3 F)
    {
        float sp = 0f, sm = 0f; int np = 0, nm = 0;
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
            var v = mf.sharedMesh.vertices; int step = Mathf.Max(1, v.Length / 3000);
            for (int i = 0; i < v.Length; i += step)
            {
                var w = r.transform.TransformPoint(v[i]); float d = Vector3.Dot(w - b.center, F);
                if (d > .05f) { sp += w.y; np++; } else if (d < -.05f) { sm += w.y; nm++; }
            }
        }
        if (np > 20 && nm > 20 && sp / np > sm / nm + .05f) return -F;    // od strony „przodu” wyżej = tam oparcie
        return F;
    }

    // ---------------- pary i pojedynczo na placach ----------------
    static void LudzieNaPlacach(System.Random rnd)
    {
        var g = Group("Ludzie / na placach");
        float north = StreetZ + Half, x0 = AxisX + Half + .5f, zc = (ParkZ0 + FenceA - .6f) * .5f;
        // (punkt, kierunek osi pary w stopniach): place przed budynkami osiedla A, pierzeja sklepów B, plac fontanny
        var pary = new List<(Vector3 p, float yaw)>
        {
            (new Vector3(77f, .1f, -24.6f), 90f), (new Vector3(-77f, .1f, -24.6f), 90f), (new Vector3(-76f, .1f, 31.4f), 0f), (new Vector3(74f, .1f, 31.6f), 0f),
            (new Vector3(x0 + 6f, .14f, north + 4.2f), 90f), (new Vector3(x0 + 18f, .14f, north + 4.4f), 0f),
            (new Vector3(AxisX + 6.8f, .05f, zc + 3.6f), 30f), (new Vector3(AxisX - 6.6f, .05f, zc - 4f), 120f),
        };
        foreach (var (p0, yaw) in pary)
        {
            if (Mathf.Abs(p0.z - GranicaObszarow) < 3f) continue;
            var os = Quaternion.Euler(0, yaw, 0) * Vector3.right; float d = Mathf.Lerp(1.05f, 1.3f, (float)rnd.NextDouble());
            Vector3 a = p0 - os * d * .5f, b = p0 + os * d * .5f;
            if (!Wolne(a, .28f) || !Wolne(b, .28f) || NaJezdni(a) || NaJezdni(b)) { rep.notes.Add("ludzie: para przy " + p0 + " — miejsce zajęte, pominięta"); continue; }
            var para = new GameObject("Rozmowa (para)").transform; para.SetParent(g, false); para.position = p0;
            var r1 = rnd.NextDouble() < .2 ? RolaImpreza : RolaPrzechodzien;
            var m1 = NowyMieszkaniec(r1, para, "Rozmawia", a, Quaternion.LookRotation(b - a).eulerAngles.y, rnd, KlipySlucha, kubek: rnd.NextDouble() < .25);
            var m2 = NowyMieszkaniec(RolaPrzechodzien, para, "Rozmawia", b, Quaternion.LookRotation(a - b).eulerAngles.y, rnd, KlipySlucha);
            if (!m1 || !m2) { UnityEngine.Object.DestroyImmediate(para.gameObject); continue; }
            m1.rozmowca = m2; m2.rozmowca = m1; m1.lider = true; rbPary++;
        }
        // pojedynczo: telefon / kawa, czekają (przy przejściach, przed sklepami, w parku)
        var sami = new List<Vector3> { new Vector3(x0 + 25f, .14f, north + 4.6f), new Vector3(AxisX - 2.6f, .05f, zc + 14f), new Vector3(20f, .14f, -13.2f), new Vector3(-53.2f, .14f, 50f), new Vector3(x0 + 52f, .14f, north + 3.8f) };
        foreach (var p in sami)
        {
            if (!Wolne(p, .28f) || NaJezdni(p)) continue;
            bool tel = rnd.NextDouble() < .6;
            var m = NowyMieszkaniec(RolaPrzechodzien, g, tel ? "Z telefonem" : "Z kawą", p, (float)rnd.NextDouble() * 360f, rnd,
                                    tel ? new[] { "cell_phone_textmessage", "cell_phone_talk_01" } : new[] { "drink_drinking", "idle_neutral_01", "idle_look_around_01" }, telefon: tel, kubek: !tel);
            if (m) rbInni++;
        }
    }

    // ---------------- obsada lokali ----------------
    static float Podloga(Vector3 p, float domysl)
    {
        if (!float.IsNaN(domysl)) return domysl;
        if (Physics.Raycast(p + Vector3.up * 1.3f, Vector3.down, out var h, 2.5f, ~0, QueryTriggerInteraction.Ignore) && h.point.y > -.3f && h.point.y < .6f) return h.point.y;
        return .03f;
    }
    // Wolne miejsce przy punkcie (do 0.45 m dookoła), inaczej null.
    static Vector3? Miejsce(Vector3 p, float r = .24f)
    {
        if (Wolne(p, r)) return p;
        for (float d = .15f; d <= .45f; d += .15f)
            for (int k = 0; k < 8; k++) { var q = p + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * d; if (Wolne(q, r)) return q; }
        return null;
    }

    static BraxMieszkaniec Obsada(string gdzie, Rola rola, string rolaKwestii, Vector3 p, float yaw, System.Random rnd, string[] klipy, float podloga = float.NaN)
    {
        p.y = Podloga(p, podloga);
        var m0 = Miejsce(p);
        if (!m0.HasValue) { rep.notes.Add("ludzie: obsada „" + gdzie + "” — miejsce zajęte (" + p.x.ToString("F1") + ", " + p.z.ToString("F1") + "), pominięta"); return null; }
        var g = Group("Ludzie / obsada");
        var m = NowyMieszkaniec(rola, g, gdzie, m0.Value, yaw, rnd, klipy);
        if (!m) return null;
        m.obsluga = true; m.rola = rolaKwestii; rbObsada++;
        return m;
    }

    static void ObsadaLokali(System.Random rnd)
    {
        var lada = new[] { "work_mid", "idle_neutral_01", "idle_neutral_02", "idle_look_around_01" };
        var stoi = new[] { "idle_neutral_01", "idle_neutral_02", "idle_neutral_03", "idle_waiting_01" };
        // pierzeja sklepów B (ShopRow): butik | shake | nutrition | kawiarnia | market
        float x0 = AxisX + Half + .5f, x1 = AxisX + Half + 62f, u = (x1 - x0) / 5f, zf = StreetZ + Half + 6f + .3f, zb = StreetZ + Half + 6f + 14f - .3f;
        {   // Shake Bar: między ladą a regałem ścianki zaplecza
            float a = x0 + u + .1f, b = x0 + 2f * u - .1f, zp = zf + 9f;
            Obsada("Shake Bar — za ladą", RolaObsluga, "barman", new Vector3((a + 1.2f + b - 3.4f) * .5f, 0, zp - .8f), 180f, rnd, lada, .14f);
        }
        {   // kawiarnia: barista przy ekspresie i druga osoba przy witrynie z ciastami
            float a = x0 + 3f * u + .1f, b = x0 + 4f * u - .1f, lz = zb - 2.2f;
            Obsada("kawiarnia — barista", RolaObsluga, "barista", new Vector3(b - 4.05f, 0, lz + .9f), 180f, rnd, lada, .14f);
            Obsada("kawiarnia — kasa", RolaObsluga, "barista", new Vector3(a + 4.0f, 0, lz + .9f), 180f, rnd, lada, .14f);
        }
        {   // market: kasjer między witryną a ladą kasy, przodem do sali
            float b = x0 + 5f * u - .3f;
            Obsada("market — kasa", RolaObsluga, "kasjer", new Vector3(b - 1.25f, 0, zf + .9f), 0f, rnd, lada, .14f);
        }
        rep.notes.Add("ludzie: butik zamknięty (WKRÓTCE OTWARCIE, brak lady i wejścia) — bez obsady");
        // siłownia A: recepcja (za ladą, przodem do sali)
        Obsada("siłownia — recepcja", RolaRecepcja, "recepcja", new Vector3(85.0f, 0, -68.05f), 180f, rnd, lada);
        // kasyno: KASA · ŻETONY i ZAKŁADY (między ladą a ścianą z drzwiami do sali, przodem do wejścia)
        Obsada("kasyno — kasa", RolaRecepcja, "kasyno", new Vector3(72f, 0, -34.12f), 0f, rnd, lada, .062f);
        Obsada("kasyno — zakłady", RolaRecepcja, "kasyno", new Vector3(100f, 0, -34.12f), 0f, rnd, lada, .062f);
        // komisariat: dyżurny za ladą (stoi), drugi policjant przy biurku (siedzi)
        Obsada("komisariat — dyżurny", RolaPolicja, "policja", new Vector3(-90.3f, 0, -35.2f), 0f, rnd, stoi, .07f);
        {
            var krzeslo = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => t.name == "dining_chair_02").OrderBy(t => (t.position - new Vector3(-67.05f, 0, -40.4f)).sqrMagnitude).FirstOrDefault();
            if (krzeslo && (krzeslo.position - new Vector3(-67.05f, krzeslo.position.y, -40.4f)).magnitude < 1.2f)
            {
                var rs = krzeslo.GetComponentsInChildren<MeshRenderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float y = b.min.y + Mathf.Clamp(b.size.y * .42f, .36f, .52f);
                var m = Siedzacy(RolaPolicja, Group("Ludzie / obsada"), "komisariat — przy biurku", new Vector3(b.center.x, y, b.center.z), Vector3.left, rnd, new[] { "sit_table_breathe_01" });
                if (m) { m.obsluga = true; m.rola = "policja"; rbObsada++; rbSiedzacy--; }
            }
            else Obsada("komisariat — przy szafach", RolaPolicja, "policja", new Vector3(-80.6f, 0, -45.9f), 0f, rnd, new[] { "documents_idle", "idle_neutral_01" }, .07f);
        }
        // szpital (wnętrze z modelu osiedla): lekarz i pielęgniarka rozmawiają w holu przy wejściu
        {
            Vector3? a = null;
            for (float z = 26.2f; z > 14f && !a.HasValue; z -= 1.2f)
                for (float dx = 0f; dx < 9f && !a.HasValue; dx += 1.5f)
                    foreach (float x in new[] { -86f - dx, -86f + dx })
                    {
                        var p = new Vector3(x, 0, z); p.y = Podloga(p, float.NaN);
                        var q = p + Vector3.right * 1.2f;
                        if (Wolne(p, .28f) && Wolne(q, .28f) && WidacProsto(p, q)) { a = p; break; }
                    }
            if (a.HasValue)
            {
                var para = new GameObject("Szpital — lekarz i pielęgniarka").transform; para.SetParent(Group("Ludzie / obsada"), false); para.position = a.Value;
                var p2 = a.Value + Vector3.right * 1.2f;
                var med = rbPostacie.Where(p => p.nazwa.StartsWith("Medical")).ToList();
                var m1 = NowyMieszkaniec(RolaMedyk, para, "szpital — lekarz", a.Value, 90f, rnd, KlipySlucha, wymus: med.FirstOrDefault(p => !p.kobieta));
                var m2 = NowyMieszkaniec(RolaMedyk, para, "szpital — pielęgniarka", p2, 270f, rnd, KlipySlucha, wymus: med.FirstOrDefault(p => p.kobieta));
                foreach (var m in new[] { m1, m2 }) if (m) { m.obsluga = true; m.rola = "lekarz"; rbObsada++; }
                if (m1 && m2) { m1.rozmowca = m2; m2.rozmowca = m1; m1.lider = true; }
            }
            else rep.notes.Add("ludzie: szpital — brak wolnego miejsca w holu, bez lekarza");
        }
        // dyskoteka: barmani za barem (przodem do sali), szatniarz, ochroniarz przed wejściem (od ulicy, obok drzwi)
        Obsada("dyskoteka — barman", RolaObsluga, "barman", new Vector3(68.3f, 0, 13.2f), 90f, rnd, lada, .06f);
        Obsada("dyskoteka — barmanka", RolaObsluga, "barman", new Vector3(68.3f, 0, 8.4f), 90f, rnd, lada, .06f);
        Obsada("dyskoteka — szatnia", RolaObsluga, "szatnia", new Vector3(97.9f, 0, 26.3f), 270f, rnd, stoi, .06f);
        Obsada("dyskoteka — ochrona przy wejściu", RolaOchrona, "ochrona", new Vector3(89.5f, 0, 30.1f), 0f, rnd, new[] { "idle_neutral_01", "idle_waiting_01", "idle_look_around_01" });
        // goście klubu: przy barze z drinkiem, na parkiecie tańczą (twarzą do sceny DJ na wschodzie)
        foreach (float z in new[] { 9.8f, 15.8f })
        {
            var p = new Vector3(71.35f, .06f, z); if (!Wolne(p)) continue;
            var m = NowyMieszkaniec(RolaImpreza, Group("Ludzie / dyskoteka"), "Przy barze", p, 270f, rnd, new[] { "drink_drinking", "idle_neutral_01" }, kubek: false);
            if (m) rbInni++;
        }
        var taniec = new[] { "dancing_cool", "dancing_neutral", "dancing_silly", "cheer_01" };
        for (int k = 0; k < 9; k++)
        {
            var p = new Vector3(87.6f + (k % 3) * 4.2f + (float)rnd.NextDouble() * 1.4f, .11f, 7.6f + (k / 3) * 3.7f + (float)rnd.NextDouble() * 1.4f);
            if (!Wolne(p)) continue;
            var m = NowyMieszkaniec(RolaImpreza, Group("Ludzie / dyskoteka"), "Tańczy", p, 90f + (float)(rnd.NextDouble() * 100 - 50), rnd, taniec);
            if (m) { m.zmianaMin = 8f; m.zmianaMax = 20f; rbInni++; }
        }
    }

    // ---------------- kierowcy i pasażerowie ----------------
    static void KierowcyAut(System.Random rnd)
    {
        var fotele = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == FotelKierowcy || t.name == FotelPasazera).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
        int pas = 0, stoja = 0;
        foreach (var f in fotele)
        {
            bool kier = f.name == FotelKierowcy;
            if (!f.GetComponentInParent<BraxAuto>(true)) { stoja++; continue; }      // auto zaparkowane — bez kierowcy (realnie stoi puste)
            if (!kier && rnd.NextDouble() > .35) continue;
            var m = Siedzacy(RolaKierowca, f, kier ? "Kierowca" : "Pasażer", f.position, f.forward, rnd,
                             kier ? new[] { "sit_table_breathe_01" } : new[] { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" }, wAucie: true);
            if (!m) continue;
            m.transform.SetParent(f, true);
            DopasujKabine(m, f);
            // ramiona: kierowca do kierownicy (16°) + oddanie odchylenia tułowia (ręce zostają przy kierownicy), pasażer — oddanie odchylenia
            if (kier) { m.korektaRak = 16f + m.odchylenie; rbKierowcy++; } else { m.korektaRak = m.odchylenie; pas++; }
            rbSiedzacy--;
            foreach (var c in m.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var rb in m.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
        }
        rep.notes.Add("ludzie: foteli " + fotele.Count() + ", kierowców " + rbKierowcy + ", pasażerów " + pas + ", foteli w autach zaparkowanych (puste) " + stoja + (fotele.Count() == 0 ? " (auta bez foteli — etap aut)" : ""));
    }
}

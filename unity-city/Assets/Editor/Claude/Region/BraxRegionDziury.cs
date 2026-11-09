using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// DZIURY W GRUNCIE — WODA POD MIASTEM (9.11, właściciel ze zdjęciami: podwórko przy białej ścianie, pas wzdłuż tyłów kamienic
// przy placu wieżowców: „te niebieskie, nie wiem, co to jest, ma poznikać… od A do Z przejrzeć cały teren za siłownią, koło
// budynków, port, aż po wulkanizację”).
// PRZYCZYNA: źródło miasta ma pod całą mapą NIEWIDZIALNĄ podłogę y = 0 (kolizja „Original virtual floor”), a pod nią taflę wody
// miasta y −1.7 (shader fal — niebieskie „paski”). Tam, gdzie widoczną nawierzchnię zdjęto (cięcia gruntu oryginału, stare
// korzenie tyłów usunięte przy przebudowie) i nic nowego nie położono, gracz chodzi po niewidzialnej podłodze, a widać wodę.
// ROZWIĄZANIE (bez zgadywania miejsc — zmierzone przy każdym buildzie, po wszystkich krokach, które kładą nawierzchnie):
//  1) siatka 12.5 cm na obszarach terenu za siłownią (od Alei Południowej i wulkanizacji po nabrzeże portu, od ulicy głównej
//     po skarpę i park); każda widoczna powierzchnia skierowana w górę na wysokości gruntu (y −1.3…0.8: jezdnie, chodniki,
//     place, trawniki, kwatery, posadzki parterów) zaznacza komórki, które przykrywa (rzut trójkąta), z jej wysokością;
//     woda (pas y −2.1…−1.3) się nie liczy; komórki z głębszą posadzką (schody w dół, piwnice, y < −2.1) są wyłączone;
//  2) komórki bez żadnej nawierzchni = DZIURA; spójne dziury łączone w prostokąty (wiersze → pasy → prostokąty);
//  3) każda dziura dostaje płytę z płyt granitowych placu (ta sama faktura co plac wieżowców) 3 cm pod mediana wysokości
//     sąsiednich nawierzchni (zasada: żadna płaszczyzna nie leży na innej — odstęp ≥ 2 cm; kontrola migotania szuka < 2 cm),
//     wsuniętą 10 cm pod wyższe sąsiednie nawierzchnie (bez szczelin na styku), z kolizją (stopień 3 cm, nie 15 cm w dół);
//  4) log BRAXHORN_DZIURY: pokrycie, liczba dziur, powierzchnia, 30 największych z obrysem i wysokością — do sprawdzenia, że
//     w kolejnym buildzie lista jest pusta albo zostały tylko szczeliny pod ścianami (płyta pod murem jest niewidoczna).
// Trawniki obszaru: każdy „Trawnik: …” musi mieć obrzeże (krawężnik betonowy) — brakujące dokładane (log BRAXHORN_TRAWNIKI).
public static class BraxRegionDziury
{
    public const float Komorka = .125f;
    public const float YMin = -1.3f, YMax = .8f;          // pas gruntu (woda miasta y −1.7 ± fale poza nim)
    public const float WodaDol = -2.1f;                   // poniżej: posadzki zagłębione (schody w dół, piwnice) — wyłączone
    public const float Odstep = .03f;                     // płyta dziury pod sąsiednią nawierzchnią (≥ 2 cm)
    public const float Wsuniecie = .10f;                  // wsunięcie płyty pod wyższą nawierzchnię sąsiada
    public static readonly (string nazwa, Rect obszar)[] Obszary =
    {
        ("zachód: Aleja Płd. (pierzeja wsch.), wulkanizacja i warsztaty, parking, plac ustawek, ul. Wiaduktowa", Rect.MinMaxRect(-114f, BraxMiastoGranice.Z0, -61.4f, 23.5f)),
        ("wschód: tyły kamienic i siłowni, plac wieżowców, wieżowce A/B, park, nabrzeże, port z dźwigiem", Rect.MinMaxRect(-61.4f, -68f, 83.55f, 23.5f)),
    };

    public static int Wypelnione, Komorek;
    public static float Powierzchnia;
    public static readonly List<string> Raport = new List<string>();

    public static void Apply(Transform cityRoot)
    {
        Wypelnione = 0; Komorek = 0; Powierzchnia = 0f; Raport.Clear();
        var root = BraxRegion.Root(cityRoot, "CLAUDE grunt: zamknięte dziury (teren za siłownią → port)");
        var mat = BraxRegionMeble.Tex("Tyły B: płyty granitowe placu", 1.2f, new Color(.70f, .69f, .67f), .85f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
        Physics.SyncTransforms();
        var rs = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var (nazwa, obszar) in Obszary)
        {
            try { Obszar(root, nazwa, obszar, rs, mat); }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_DZIURY " + nazwa + ": " + e); }
        }
        int obrzeza = Trawniki(cityRoot);
        Debug.Log("BRAXHORN_DZIURY podsumowanie: wypełnionych dziur " + Wypelnione + ", komórek " + Komorek + ", powierzchnia " + Powierzchnia.ToString("0.0") + " m²" +
                  ", dołożone obrzeża trawników " + obrzeza + (Raport.Count > 0 ? " | " + string.Join(" | ", Raport.Take(30)) : ""));
        BraxRegion.Note("grunt: dziury z wodą pod miastem zamknięte — " + Wypelnione + " (" + Powierzchnia.ToString("0") + " m²), obrzeża trawników dołożone " + obrzeza);
    }

    sealed class Siatka
    {
        public int nx, nz; public float x0, z0;
        public bool[] pokryte, wylaczone; public float[] h;
        public int I(int i, int j) => j * nx + i;
        public float X(int i) => x0 + (i + .5f) * Komorka;
        public float Z(int j) => z0 + (j + .5f) * Komorka;
    }

    static bool Szklo(Material m) => m && (m.name.IndexOf("szyb", StringComparison.OrdinalIgnoreCase) >= 0 || m.name.IndexOf("szkł", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          m.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0 || m.name.IndexOf("woda", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          m.name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0);

    static void Obszar(Transform root, string nazwa, Rect a, MeshRenderer[] rs, Material mat)
    {
        var s = new Siatka { x0 = a.xMin, z0 = a.yMin, nx = Mathf.CeilToInt(a.width / Komorka), nz = Mathf.CeilToInt(a.height / Komorka) };
        int N = s.nx * s.nz;
        s.pokryte = new bool[N]; s.wylaczone = new bool[N]; s.h = new float[N];
        for (int k = 0; k < N; k++) s.h[k] = float.PositiveInfinity;
        var obj = new Bounds(); obj.SetMinMax(new Vector3(a.xMin, -12f, a.yMin), new Vector3(a.xMax, YMax + .2f, a.yMax));
        int rend = 0, troj = 0;
        foreach (var r in rs)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r.gameObject.layer == 4) continue;
            if (!r.bounds.Intersects(obj)) continue;
            if (r.transform.IsChildOf(root)) continue;
            var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null; if (!m) continue;
            Vector3[] v; try { v = m.vertices; } catch (Exception) { continue; }
            if (v == null || v.Length == 0) continue;
            var M = r.transform.localToWorldMatrix; var w = new Vector3[v.Length];
            for (int q = 0; q < v.Length; q++) w[q] = M.MultiplyPoint3x4(v[q]);
            var mats = r.sharedMaterials; bool uzyty = false;
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                if (sub < mats.Length && Szklo(mats[sub])) continue;
                int[] t; try { t = m.GetTriangles(sub); } catch (Exception) { continue; }
                for (int q = 0; q + 2 < t.Length; q += 3)
                {
                    Vector3 A = w[t[q]], B = w[t[q + 1]], C = w[t[q + 2]];
                    var n = Vector3.Cross(B - A, C - A); float dl = n.magnitude; if (dl < 1e-7f) continue;
                    if (n.y / dl < .5f) continue;                                  // tylko powierzchnie skierowane w górę
                    float yc = (A.y + B.y + C.y) / 3f;
                    bool grunt = yc >= YMin && yc <= YMax, gleboko = yc < WodaDol && yc > -12f;
                    if (!grunt && !gleboko) continue;
                    Rasteryzuj(s, A, B, C, grunt);
                    troj++; uzyty = true;
                }
            }
            if (uzyty) rend++;
        }
        // dziury: komórki bez nawierzchni i nie wyłączone; spójne składowe (4-sąsiedztwo)
        var komp = new int[N]; for (int k = 0; k < N; k++) komp[k] = -1;
        int nKomp = 0, pokr = 0; foreach (var b in s.pokryte) if (b) pokr++;
        var kolejka = new Queue<int>();
        var bud = new BraxRegionMeble.Bud(); int prost = 0;
        var wyniki = new List<(float pole, string opis)>();
        for (int k0 = 0; k0 < N; k0++)
        {
            if (s.pokryte[k0] || s.wylaczone[k0] || komp[k0] >= 0) continue;
            var komorki = new List<int>(); komp[k0] = nKomp; kolejka.Enqueue(k0);
            while (kolejka.Count > 0)
            {
                int k = kolejka.Dequeue(); komorki.Add(k);
                int i = k % s.nx, j = k / s.nx;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ii = i + di, jj = j + dj; if (ii < 0 || jj < 0 || ii >= s.nx || jj >= s.nz) continue;
                    int kk = s.I(ii, jj);
                    if (s.pokryte[kk] || s.wylaczone[kk] || komp[kk] >= 0) continue;
                    komp[kk] = nKomp; kolejka.Enqueue(kk);
                }
            }
            // wysokość odniesienia: mediana wysokości nawierzchni sąsiadujących z dziurą
            var sas = new List<float>();
            foreach (int k in komorki)
            {
                int i = k % s.nx, j = k / s.nx;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ii = i + di, jj = j + dj; if (ii < 0 || jj < 0 || ii >= s.nx || jj >= s.nz) continue;
                    int kk = s.I(ii, jj); if (s.pokryte[kk]) sas.Add(s.h[kk]);
                }
            }
            nKomp++;
            if (sas.Count == 0) { wyniki.Add((komorki.Count * Komorka * Komorka, "bez sąsiedniej nawierzchni (pominięta) " + Obrys(s, komorki))); continue; }
            sas.Sort(); float href = sas[sas.Count / 2];
            if (href < -.3f || href > .6f) { wyniki.Add((komorki.Count * Komorka * Komorka, "nietypowa wysokość sąsiadów " + href.ToString("F2") + " (pominięta) " + Obrys(s, komorki))); continue; }
            float top = href - Odstep;
            // prostokąty: przebiegi w wierszach, sklejane z identycznymi przebiegami kolejnych wierszy
            var wKomp = new HashSet<int>(komorki);
            var wiersze = new SortedDictionary<int, List<(int i0, int i1)>>();
            foreach (int k in komorki.OrderBy(kk0 => kk0))
            {
                int i = k % s.nx, j = k / s.nx;
                if (!wiersze.TryGetValue(j, out var l)) wiersze[j] = l = new List<(int, int)>();
                if (l.Count > 0 && l[l.Count - 1].i1 == i - 1) l[l.Count - 1] = (l[l.Count - 1].i0, i); else l.Add((i, i));
            }
            var otwarte = new Dictionary<(int, int), int>();   // przebieg → j0
            var gotowe = new List<(int i0, int i1, int j0, int j1)>();
            int jPop = int.MinValue;
            foreach (var kv in wiersze)
            {
                int j = kv.Key;
                var teraz = new HashSet<(int, int)>(kv.Value);
                foreach (var o in otwarte.ToList())
                    if (j != jPop + 1 || !teraz.Contains(o.Key)) { gotowe.Add((o.Key.Item1, o.Key.Item2, o.Value, jPop)); otwarte.Remove(o.Key); }
                foreach (var p in kv.Value) if (!otwarte.ContainsKey(p)) otwarte[p] = j;
                jPop = j;
            }
            foreach (var o in otwarte) gotowe.Add((o.Key.Item1, o.Key.Item2, o.Value, jPop));
            bool Wyzszy(int ii, int jj) { if (ii < 0 || jj < 0 || ii >= s.nx || jj >= s.nz) return false; int kk = s.I(ii, jj); return !wKomp.Contains(kk) && s.pokryte[kk] && s.h[kk] >= top + Odstep - .005f; }
            bool Bok(int ia, int ib, int ja, int jb) { for (int ii = ia; ii <= ib; ii++) for (int jj = ja; jj <= jb; jj++) if (!Wyzszy(ii, jj)) return false; return true; }
            foreach (var (i0, i1, j0, j1) in gotowe)
            {
                float x0 = s.x0 + i0 * Komorka, x1 = s.x0 + (i1 + 1) * Komorka, z0 = s.z0 + j0 * Komorka, z1 = s.z0 + (j1 + 1) * Komorka;
                if (Bok(i0 - 1, i0 - 1, j0, j1)) x0 -= Wsuniecie;
                if (Bok(i1 + 1, i1 + 1, j0, j1)) x1 += Wsuniecie;
                if (Bok(i0, i1, j0 - 1, j0 - 1)) z0 -= Wsuniecie;
                if (Bok(i0, i1, j1 + 1, j1 + 1)) z1 += Wsuniecie;
                bud.Quad(new Vector3(x0, top, z0), new Vector3(x0, top, z1), new Vector3(x1, top, z1), new Vector3(x1, top, z0));
                if ((x1 - x0) * (z1 - z0) >= .25f)
                    BraxRegionMeble.Kolizja(root, "Dziura zamknięta: kolizja", new Vector3((x0 + x1) * .5f, top - .15f, (z0 + z1) * .5f), new Vector3(x1 - x0, .3f, z1 - z0));
                prost++;
                if (bud.Count > 60000) { Zapisz(bud, root, nazwa, mat); bud = new BraxRegionMeble.Bud(); }
            }
            float pole = komorki.Count * Komorka * Komorka;
            Wypelnione++; Komorek += komorki.Count; Powierzchnia += pole;
            wyniki.Add((pole, Obrys(s, komorki) + " " + pole.ToString("0.00") + " m², płyta y " + top.ToString("F3") + " (sąsiedzi: mediana " + href.ToString("F3") + ", " + sas.Count + " styków), prostokątów " + gotowe.Count));
        }
        Zapisz(bud, root, nazwa, mat);
        foreach (var w in wyniki.OrderByDescending(q => q.pole).Take(15)) Raport.Add(w.opis);
        Debug.Log("BRAXHORN_DZIURY obszar „" + nazwa + "”: komórek " + N + " (" + Komorka + " m), pokrytych " + (100f * pokr / Mathf.Max(1, N)).ToString("0.0") + " %, rendererów " + rend +
                  ", trójkątów gruntu " + troj + ", dziur " + nKomp + ", prostokątów płyt " + prost +
                  (wyniki.Count > 0 ? " | największe: " + string.Join(" | ", wyniki.OrderByDescending(q => q.pole).Take(30).Select(q => q.opis)) : " | brak dziur"));
    }

    static string Obrys(Siatka s, List<int> komorki)
    {
        int i0 = int.MaxValue, i1 = int.MinValue, j0 = int.MaxValue, j1 = int.MinValue;
        foreach (int k in komorki) { int i = k % s.nx, j = k / s.nx; i0 = Mathf.Min(i0, i); i1 = Mathf.Max(i1, i); j0 = Mathf.Min(j0, j); j1 = Mathf.Max(j1, j); }
        return "x " + (s.x0 + i0 * Komorka).ToString("0.0") + "…" + (s.x0 + (i1 + 1) * Komorka).ToString("0.0") + ", z " + (s.z0 + j0 * Komorka).ToString("0.0") + "…" + (s.z0 + (j1 + 1) * Komorka).ToString("0.0");
    }

    static void Zapisz(BraxRegionMeble.Bud bud, Transform root, string nazwa, Material mat)
    {
        if (bud.Count == 0) return;
        var g = bud.Obiekt("Grunt: płyty w miejscu dziur (" + nazwa.Split(':')[0] + ")", root, mat, false, false);
        if (g) { var r = g.GetComponent<MeshRenderer>(); if (r) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; } }
    }

    // rzut trójkąta na siatkę: środki komórek wewnątrz trójkąta (współrzędne barycentryczne w xz), wysokość z płaszczyzny
    static void Rasteryzuj(Siatka s, Vector3 A, Vector3 B, Vector3 C, bool grunt)
    {
        float minx = Mathf.Min(A.x, Mathf.Min(B.x, C.x)), maxx = Mathf.Max(A.x, Mathf.Max(B.x, C.x));
        float minz = Mathf.Min(A.z, Mathf.Min(B.z, C.z)), maxz = Mathf.Max(A.z, Mathf.Max(B.z, C.z));
        int i0 = Mathf.Max(0, Mathf.CeilToInt((minx - s.x0) / Komorka - .5f)), i1 = Mathf.Min(s.nx - 1, Mathf.FloorToInt((maxx - s.x0) / Komorka - .5f));
        int j0 = Mathf.Max(0, Mathf.CeilToInt((minz - s.z0) / Komorka - .5f)), j1 = Mathf.Min(s.nz - 1, Mathf.FloorToInt((maxz - s.z0) / Komorka - .5f));
        if (i0 > i1 || j0 > j1) return;
        float d = (B.z - C.z) * (A.x - C.x) + (C.x - B.x) * (A.z - C.z);
        if (Mathf.Abs(d) < 1e-9f) return;
        const float eps = -1e-4f;
        for (int j = j0; j <= j1; j++)
        {
            float pz = s.Z(j);
            for (int i = i0; i <= i1; i++)
            {
                float px = s.X(i);
                float l1 = ((B.z - C.z) * (px - C.x) + (C.x - B.x) * (pz - C.z)) / d;
                float l2 = ((C.z - A.z) * (px - C.x) + (A.x - C.x) * (pz - C.z)) / d;
                float l3 = 1f - l1 - l2;
                if (l1 < eps || l2 < eps || l3 < eps) continue;
                int k = s.I(i, j);
                if (grunt)
                {
                    float y = l1 * A.y + l2 * B.y + l3 * C.y;
                    s.pokryte[k] = true; if (y < s.h[k]) s.h[k] = y;
                }
                else s.wylaczone[k] = true;
            }
        }
    }

    // trawniki w obszarach: każdy „Trawnik: …” z obrzeżem (krawężnik betonowy) — brakujące dokładane jak w BraxRegionTrawa
    static int Trawniki(Transform cityRoot)
    {
        int dolozone = 0, bez = 0, ok = 0; var lista = new List<string>();
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!t || !t.name.StartsWith("Trawnik: ", StringComparison.Ordinal) || t.name.StartsWith("Trawnik: obrzeże", StringComparison.Ordinal)) continue;
            var r = t.GetComponent<MeshRenderer>(); if (!r) continue;
            var b = r.bounds; var c = new Vector2(b.center.x, b.center.z);
            if (!Obszary.Any(o => o.obszar.Contains(c))) continue;
            bool kwatera = t.name.IndexOf("kwatera", StringComparison.OrdinalIgnoreCase) >= 0;   // kwatery mają murek granitowy dookoła
            bool ma = kwatera || t.Cast<Transform>().Any(ch => ch.name.StartsWith("Trawnik: obrzeże", StringComparison.Ordinal));
            if (ma) { ok++; continue; }
            bez++;
            // obrzeże z obrysu trawnika (pas 8 cm wewnątrz krawędzi, 4.5 cm nad trawą — jak BraxRegionTrawa.Trawnik)
            float e = BraxRegionTrawa.GrubyObrzeza, top = b.max.y + BraxRegionTrawa.ObrzezeNadTrawa, h = .22f, cy = top - h * .5f;
            var bud = new BraxRegionMeble.Bud();
            bud.Box(new Vector3(b.center.x, cy, b.min.z + e * .5f), new Vector3(b.size.x, h, e));
            bud.Box(new Vector3(b.center.x, cy, b.max.z - e * .5f), new Vector3(b.size.x, h, e));
            bud.Box(new Vector3(b.min.x + e * .5f, cy, b.center.z), new Vector3(e, h, b.size.z - 2 * e));
            bud.Box(new Vector3(b.max.x - e * .5f, cy, b.center.z), new Vector3(e, h, b.size.z - 2 * e));
            var mat = BraxRegionMeble.Tex("Trawnik: obrzeże betonowe (dołożone)", 1.0f, new Color(.74f, .73f, .70f), .85f, "concrete_floor_02", "concrete_pavement_02");
            if (bud.Obiekt("Trawnik: obrzeże betonowe (dołożone) " + t.name.Substring(9), t, mat, false, false)) { dolozone++; lista.Add(t.name.Substring(9)); }
        }
        Debug.Log("BRAXHORN_TRAWNIKI teren za siłownią: z obrzeżem " + ok + ", bez obrzeża " + bez + ", dołożone " + dolozone + (lista.Count > 0 ? ": " + string.Join(", ", lista.Take(20)) : ""));
        return dolozone;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// DRZWI BEZ ŚCIANY (build 45, E29 — właściciel 3. raz: „ściana na środku dyskoteki”): drzwi z manifestu Codexa (data.doors,
// BraxCityBuild) mają skrzydła dynamiczne — przebudowa wnętrza (CutVolumes) wycina ścianę, a skrzydła zostają i stoją same na
// środku sali (club-hall w dyskotece, casino-hall, police-office, hospital-room-*, villa-bathroom…).
// Test każdych drzwi w scenie po zbudowaniu wnętrz: po OBU bokach otworu (0.3 m za krawędzią skrzydeł, na wysokości 1.2 m)
// musi być ściana — 1) kolizja nie z drzwi, nie wyzwalacz, wysoka ≥ 1.6 m; 2) zapasowo SIATKA (audyt rundy 1: kolizje Codexa
// z manifestu bywają dziurawe): MeshRenderer z obrysem ≥ 1.6 m wysokości w tym miejscu ORAZ trójkąt jego siatki przecięty
// odcinkiem w poprzek otworu (sam obrys nie wystarcza — scalone siatki budynku obejmują całą salę).
// Runda 2 (audyt 9f658d4): drzwi WEJŚCIOWE (id …-front / …-front-door oraz jedyne wejście budynku po E25) NIGDY nie są usuwane
// automatycznie — brak ściany = log DO POPRAWKI, a gdy ściana jest w siatce, a nie ma kolizji — dołożona kolizja ściany po
// boku otworu. Usuwane automatycznie tylko drzwi wewnętrzne (-hall, -office, -room-*, -bathroom; -rear, gdy po E25 nie są
// jedynym wejściem) — w całości (skrzydła, kolizje, punkt „OTWÓRZ DRZWI” z rejestru BraxCityDoor.All). Inne id — zostają
// (log DO POPRAWKI). Log BRAXHORN_DRZWI_BEZ_SCIANY i BRAXHORN_DRZWI (rozmiary pozostałych).
public static partial class BraxClaudeOsiedle
{
    // jedyne wejścia budynków po E25 (wypełnia spis wejść; do tego czasu chronione są wszystkie …-front)
    public static readonly HashSet<string> JedyneWejscia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public enum KlasaDrzwi { Wejscie, Wewnetrzne, Nieznane }
    public static KlasaDrzwi Klasa(string id)
    {
        if (string.IsNullOrEmpty(id)) return KlasaDrzwi.Nieznane;
        string s = id.ToLowerInvariant();
        if (JedyneWejscia.Contains(id) || s.EndsWith("-front") || s.Contains("-front-")) return KlasaDrzwi.Wejscie;
        if (s.EndsWith("-hall") || s.EndsWith("-office") || s.Contains("-room-") || s.EndsWith("-room") || s.EndsWith("-bathroom") || s.EndsWith("-rear"))
            return KlasaDrzwi.Wewnetrzne;
        return KlasaDrzwi.Nieznane;
    }

    // Odcinek a→b przecina trójkąt (Möller–Trumbore, obie strony trójkąta).
    public static bool OdcinekTrojkat(Vector3 a, Vector3 b, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        var d = b - a; var e1 = v1 - v0; var e2 = v2 - v0; var p = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-9f) return false;
        float inv = 1f / det; var t0 = a - v0; float u = Vector3.Dot(t0, p) * inv; if (u < 0f || u > 1f) return false;
        var q = Vector3.Cross(t0, e1); float v = Vector3.Dot(d, q) * inv; if (v < 0f || u + v > 1f) return false;
        float t = Vector3.Dot(e2, q) * inv; return t >= 0f && t <= 1f;
    }

    // Ściana w SIATCE w punkcie p: renderer (nie drzwi, nie ruchomy) z obrysem ≥ 1.6 m wysokości obejmującym p i trójkąt
    // przecięty odcinkiem p ± przod × pol. Siatka nieczytelna — sam obrys nie wystarcza (zwraca false, opis „nieczytelna”).
    public static bool ScianaWSiatce(Vector3 p, Vector3 przod, float pol, IEnumerable<MeshFilter> siatki, out string co, Dictionary<Mesh, Vector3[]> pamiec = null)
    {
        co = null; Vector3 a = p - przod * pol, b = p + przod * pol;
        foreach (var mf in siatki)
        {
            if (!mf || !mf.sharedMesh) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (!r || !r.enabled) continue;
            var rb = r.bounds; if (rb.size.y < 1.6f) continue;
            var e = rb; e.Expand(.02f); if (!e.Contains(p)) continue;
            var m = mf.sharedMesh; if (!m.isReadable) { co = co ?? mf.name + " (nieczytelna)"; continue; }
            var M = mf.transform.localToWorldMatrix; Vector3[] v;
            if (pamiec == null) v = m.vertices; else if (!pamiec.TryGetValue(m, out v)) pamiec[m] = v = m.vertices;
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                var tr = m.GetTriangles(sub);
                for (int i = 0; i + 2 < tr.Length; i += 3)
                {
                    Vector3 v0 = M.MultiplyPoint3x4(v[tr[i]]), v1 = M.MultiplyPoint3x4(v[tr[i + 1]]), v2 = M.MultiplyPoint3x4(v[tr[i + 2]]);
                    if (Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y)) < p.y - .01f || Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y)) > p.y + .01f) continue;
                    if (OdcinekTrojkat(a, b, v0, v1, v2)) { co = mf.name; return true; }
                }
            }
        }
        return false;
    }

    static bool Ruchome(Transform t) => t.GetComponentInParent<BraxCityDoor>() || t.GetComponentInParent<Rigidbody>() || t.GetComponentInParent<Animation>() || t.GetComponentInParent<Animator>();

    static void DrzwiBezSciany()
    {
        Physics.SyncTransforms();
        var usuniete = new List<string>(); var zostaly = new List<string>(); var doPoprawki = new List<string>(); int sprawdzone = 0, dolozone = 0;
        var drzwi = UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var wszystkieDrzwiKol = new HashSet<Collider>(drzwi.SelectMany(d => d.GetComponentsInChildren<Collider>(true)));
        var siatki = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(mf => mf && !Ruchome(mf.transform)).ToList();
        var pamiec = new Dictionary<Mesh, Vector3[]>();
        const int maska = ~((1 << 8) | (1 << 29) | (1 << 2));
        var bufor = new Collider[64];
        foreach (var d in drzwi)
        {
            if (!d || d.leaves == null || d.leaves.Length == 0) continue;
            sprawdzone++;
            // obrys otworu z kolizji skrzydeł (zamknięte), oś szerokości = prawo skrzydła
            Bounds ob = default; bool jest = false; Vector3 prawo = d.transform.right, przod = d.transform.forward;
            foreach (var l in d.leaves)
            {
                if (!l) continue;
                var c = l.GetComponent<BoxCollider>(); if (!c) continue;
                prawo = l.right; przod = l.forward;
                for (int i = 0; i < 8; i++)
                {
                    var p = l.TransformPoint(c.center + Vector3.Scale(c.size * .5f, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    if (!jest) { ob = new Bounds(p, Vector3.zero); jest = true; } else ob.Encapsulate(p);
                }
            }
            if (!jest) continue;
            prawo.y = 0; if (prawo.sqrMagnitude < 1e-6f) prawo = Vector3.right; prawo.Normalize(); przod.y = 0; przod = przod.sqrMagnitude < 1e-6f ? Vector3.Cross(prawo, Vector3.up) : przod.normalized;
            float polSzer = Mathf.Abs(Vector3.Dot(ob.extents, new Vector3(Mathf.Abs(prawo.x), 0, Mathf.Abs(prawo.z))));
            float wys = ob.size.y; var srodek = new Vector3(ob.center.x, ob.min.y + Mathf.Min(1.2f, wys * .5f), ob.center.z);
            var obrot = Quaternion.LookRotation(przod, Vector3.up);
            bool Kolizja(Vector3 p)
            {
                int n = Physics.OverlapBoxNonAlloc(p, new Vector3(.12f, .5f, .3f), bufor, obrot, maska, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var c = bufor[i]; if (!c || wszystkieDrzwiKol.Contains(c)) continue;
                    if (c.bounds.size.y >= 1.6f) return true;
                }
                return false;
            }
            // 0 = brak ściany, 1 = kolizja, 2 = tylko siatka (ściana widoczna bez kolizji)
            int Sciana(int strona)
            {
                var p = srodek + prawo * strona * (polSzer + .3f);
                if (Kolizja(p)) return 1;
                return ScianaWSiatce(p, przod, .45f, siatki, out _, pamiec) ? 2 : 0;
            }
            int lewa = Sciana(-1), prawa = Sciana(1);
            var klasa = Klasa(d.id);
            string opis = d.id + " (" + (string.IsNullOrEmpty(d.label) ? "—" : d.label) + ") " + (polSzer * 2f).ToString("F2") + "×" + wys.ToString("F2") + " m " + BraxKontrola.F(srodek);
            if (lewa == 1 && prawa == 1) { zostaly.Add(opis); continue; }
            if (lewa > 0 && prawa > 0 || klasa != KlasaDrzwi.Wewnetrzne)
            {
                // ściana jest (choćby w siatce) albo drzwi chronione: drzwi zostają; po boku ze ścianą tylko w siatce — dołożona kolizja
                foreach (var (strona, st) in new[] { (-1, lewa), (1, prawa) })
                {
                    if (st != 2) continue;
                    float dl = .3f;   // długość ściany od krawędzi otworu: marsz co 0.25 m do 3 m, póki siatka trwa
                    for (float x = .55f; x <= 3.01f; x += .25f) { if (ScianaWSiatce(srodek + prawo * strona * (polSzer + x), przod, .45f, siatki, out _, pamiec)) dl = x; else break; }
                    var g = new GameObject("kolizja ściany przy drzwiach (" + d.id + ", z siatki)");
                    g.transform.SetParent(colliders ? colliders : d.transform.parent, false);
                    g.transform.SetPositionAndRotation(new Vector3(srodek.x, ob.min.y, srodek.z) + prawo * strona * (polSzer + dl * .5f) + Vector3.up * Mathf.Max(wys, 2.5f) * .5f, obrot);
                    var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(dl, Mathf.Max(wys, 2.5f), .3f);
                    dolozone++;
                }
                if (lewa > 0 && prawa > 0) { zostaly.Add(opis + " [kolizja ściany dołożona z siatki]"); continue; }
                string bok = lewa > 0 || prawa > 0 ? "ściana tylko z jednej strony" : "bez ścian";
                doPoprawki.Add(opis);
                Debug.LogError("BRAXHORN_DRZWI_BEZ_SCIANY DO POPRAWKI " + (klasa == KlasaDrzwi.Wejscie ? "wejscie=" : "drzwi_nieznanego_typu=") + d.id + " [" + bok + "] " + opis
                               + " — drzwi zostają (nie usuwam automatycznie " + (klasa == KlasaDrzwi.Wejscie ? "wejścia" : "drzwi spoza listy -hall/-office/-room-*/-bathroom/-rear") + ")");
                zostaly.Add(opis + " [" + bok + " — DO POPRAWKI]");
                continue;
            }
            usuniete.Add(opis + (lewa > 0 || prawa > 0 ? " [ściana tylko z jednej strony]" : " [bez ścian]"));
            UnityEngine.Object.DestroyImmediate(d.gameObject);
        }
        // tablica BraxCityWorld.doors bez usuniętych
        foreach (var w in UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.doors != null) w.doors = w.doors.Where(x => x).ToArray();
        Debug.Log("BRAXHORN_DRZWI_BEZ_SCIANY sprawdzone=" + sprawdzone + " usuniete=" + usuniete.Count + (usuniete.Count > 0 ? " [" + string.Join("; ", usuniete) + "]" : "")
                  + " kolizje_dolozone=" + dolozone + " do_poprawki=" + doPoprawki.Count + " (wejścia i drzwi nieznanego typu nie są usuwane automatycznie)");
        foreach (var z in zostaly) Debug.Log("BRAXHORN_DRZWI " + z + " — norma: zwykłe 0.9–1.0 × 2.05–2.1 m × K (" + (.9f * K).ToString("F2") + "–" + (1f * K).ToString("F2") + " × " + (2.05f * K).ToString("F2") + "–" + (2.1f * K).ToString("F2") + "), publiczne 2 × 0.9 m × K");
        rep.notes.Add("drzwi bez ściany: sprawdzone " + sprawdzone + ", usunięte " + usuniete.Count + ", kolizje ścian dołożone " + dolozone + ", do poprawki " + doPoprawki.Count);
        if (doPoprawki.Count > 0)
            rep.notes.Add("DO POPRAWKI: " + doPoprawki.Count +
                " wejść/drzwi nie ma ścian z obu stron — " + string.Join("; ", doPoprawki.Take(8)));
        try { KontrolaSkaliSwiata(); } catch (Exception e) { Debug.LogWarning("BRAXHORN_SKALA_KONTROLA DO POPRAWKI: " + e.Message); }
    }

    // E10 (build 45): kontrola norm świata ×K — tylko raport (nic nie zmienia): drzwi, pasy ruchu, chodniki, auta względem pasa.
    // Stopnie schodów i wysokości kondygnacji — niemierzone w 45 (brak spisu schodów/stropów z kodu), wypisane jako „—”.
    static void KontrolaSkaliSwiata()
    {
        void Kat(string kat, int spr, List<string> poza, string norma)
            => Debug.Log("BRAXHORN_SKALA_KONTROLA kategoria=" + kat + " sprawdzone=" + spr + " poza_norma=" + poza.Count + " norma: " + norma + (poza.Count > 0 ? " najgorsze: " + string.Join("; ", poza.Take(12)) : ""));
        // drzwi: prześwit ≥ 2.10 m × K wysokości, ≥ 0.90 m × K szerokości (jedno skrzydło)
        var dz = new List<string>(); int nd = 0;
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!d || d.leaves == null) continue;
            foreach (var l in d.leaves)
            {
                var c = l ? l.GetComponent<BoxCollider>() : null; if (!c) continue; nd++;
                var sz = Vector3.Scale(c.size, l.lossyScale);
                if (sz.y < 2.10f * K - .02f || sz.x < .90f * K - .02f) dz.Add(d.id + " " + sz.x.ToString("F2") + "×" + sz.y.ToString("F2") + " " + BraxKontrola.F(l.position));
            }
        }
        Kat("drzwi", nd, dz, "skrzydło ≥ " + (.9f * K).ToString("F2") + " × " + (2.1f * K).ToString("F2") + " m");
        if (ulice != null)
        {
            var pas = ulice.Where(u => u.cw < 3.0f * K).Select(u => (u.nazwa ?? u.id) + " pas " + u.cw.ToString("F2") + " m").ToList();
            Kat("pas_ruchu", ulice.Count, pas, "≥ " + (3f * K).ToString("F2") + " m");
            var ch = ulice.SelectMany(u => new[] { (u, -1, u.walkNeg), (u, 1, u.walkPos) }).Where(x => x.Item3 > 0f && x.Item3 < 1.5f * K)
                .Select(x => (x.u.nazwa ?? x.u.id) + (x.Item2 < 0 ? " −" : " +") + " " + x.Item3.ToString("F2") + " m").ToList();
            Kat("chodnik", ulice.Sum(u => (u.walkNeg > 0 ? 1 : 0) + (u.walkPos > 0 ? 1 : 0)), ch, "≥ " + (1.5f * K).ToString("F2") + " m");
            float pasMin = ulice.Min(u => u.cw);
            var auta = UnityEngine.Object.FindObjectsByType<BraxAuto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var za = auta.Where(a => a.szer > .65f * pasMin).GroupBy(a => a.name).Select(g => g.Key + " szer " + g.First().szer.ToString("F2") + " m (" + g.Count() + "×) vs pas " + pasMin.ToString("F2")).ToList();
            Kat("auta_wzgledem_pasa", auta.Length, za, "szerokość auta ≤ 0.65 × najwęższy pas (" + (.65f * pasMin).ToString("F2") + " m)");
        }
        Debug.Log("BRAXHORN_SKALA_KONTROLA kategoria=stopnie sprawdzone=0 (—, brak spisu schodów w 45) kategoria=kondygnacje sprawdzone=0 (—)");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// DRZWI BEZ ŚCIANY (build 45, E29 — właściciel 3. raz: „ściana na środku dyskoteki”): drzwi z manifestu Codexa (data.doors,
// BraxCityBuild) mają skrzydła dynamiczne — przebudowa wnętrza (CutVolumes) wycina ścianę, a skrzydła zostają i stoją same na
// środku sali (club-hall w dyskotece, casino-hall, police-office, hospital-room-*, villa-bathroom…).
// Test każdych drzwi w scenie po zbudowaniu wnętrz: po OBU bokach otworu (0.3 m za krawędzią skrzydeł, na wysokości 1.2 m)
// musi być ściana — kolizja nie z drzwi, nie wyzwalacz, wysoka ≥ 1.6 m; brak = drzwi usunięte w całości (skrzydła, kolizje,
// punkt „OTWÓRZ DRZWI” z rejestru BraxCityDoor.All). Log BRAXHORN_DRZWI_BEZ_SCIANY i BRAXHORN_DRZWI (rozmiary pozostałych).
public static partial class BraxClaudeOsiedle
{
    static void DrzwiBezSciany()
    {
        Physics.SyncTransforms();
        var usuniete = new List<string>(); var zostaly = new List<string>(); int sprawdzone = 0;
        var drzwi = UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var wszystkieDrzwiKol = new HashSet<Collider>(drzwi.SelectMany(d => d.GetComponentsInChildren<Collider>(true)));
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
            bool Sciana(int strona)
            {
                var p = srodek + prawo * strona * (polSzer + .3f);
                int n = Physics.OverlapBoxNonAlloc(p, new Vector3(.12f, .5f, .3f), bufor, Quaternion.LookRotation(przod, Vector3.up), maska, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var c = bufor[i]; if (!c || wszystkieDrzwiKol.Contains(c)) continue;
                    if (c.bounds.size.y >= 1.6f) return true;
                }
                return false;
            }
            bool lewa = Sciana(-1), prawa = Sciana(1);
            string opis = d.id + " (" + (string.IsNullOrEmpty(d.label) ? "—" : d.label) + ") " + (polSzer * 2f).ToString("F2") + "×" + wys.ToString("F2") + " m " + BraxKontrola.F(srodek);
            if (lewa && prawa) { zostaly.Add(opis); continue; }
            usuniete.Add(opis + (lewa || prawa ? " [ściana tylko z jednej strony]" : " [bez ścian]"));
            UnityEngine.Object.DestroyImmediate(d.gameObject);
        }
        // tablica BraxCityWorld.doors bez usuniętych
        foreach (var w in UnityEngine.Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (w.doors != null) w.doors = w.doors.Where(x => x).ToArray();
        Debug.Log("BRAXHORN_DRZWI_BEZ_SCIANY sprawdzone=" + sprawdzone + " usuniete=" + usuniete.Count + (usuniete.Count > 0 ? " [" + string.Join("; ", usuniete) + "]" : "") + " pozostałe_bez_ściany=0");
        foreach (var z in zostaly) Debug.Log("BRAXHORN_DRZWI " + z + " — norma: zwykłe 0.9–1.0 × 2.05–2.1 m × K (" + (.9f * K).ToString("F2") + "–" + (1f * K).ToString("F2") + " × " + (2.05f * K).ToString("F2") + "–" + (2.1f * K).ToString("F2") + "), publiczne 2 × 0.9 m × K");
        rep.notes.Add("drzwi bez ściany: sprawdzone " + sprawdzone + ", usunięte " + usuniete.Count);
    }
}

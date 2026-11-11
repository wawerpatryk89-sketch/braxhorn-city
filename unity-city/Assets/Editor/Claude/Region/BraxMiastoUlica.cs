using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ULICA GŁÓWNA — WYPOSAŻENIE I REKLAMY (wykonawca A, 2.11; właściciel z48–z49: „LAMPY i ZNAKI mają stać odpowiednio i być
// prawdziwe”, „BILLBOARDY PREMIUM w odpowiednich miejscach”; audyt §1: „na ulicy nie ma drzew w kratach, słupków, ławek ani
// stojaków”). Kolejność jak w budownictwie (właściciel 16:04): nawierzchnia → budynki → WYPOSAŻENIE (ten krok, po pierzejach
// i lokalach) → ludzie. Wysokość każdej rzeczy z nawierzchni pod spodem (promień w dół), nic nie wisi i nic nie zakopane.
//  • drzewa w kratach żeliwnych (krata wpuszczona: chodnik pod nią wycina kontrola migotania, pod kratą ściółka 8 cm niżej),
//    ławki miejskie × k z oparciem i podłokietnikami (siedziska BraxSeat), kosze na śmieci z daszkiem, stojaki rowerowe,
//    słupki przy przejściu, hydranty (Poly Haven CC0), tablice nazw ulic w polskim standardzie (niebieska tablica, biały
//    napis, na słupie przy krawężniku);
//  • REKLAMY: stare tablice „BRAXHORN original artwork billboard *” (płaskie płyty 0.6 m przed oknami nowych elewacji)
//    ZDJĘTE; nowe: ekrany LED w ramach na ścianach wieżowców nad ulicą (bez okien za ekranem — ekran ma własną bryłę bez
//    tylnej ściany, stoi przy elewacji wieży), widoczne w osi ulicy z obu końców; treść — grafiki marki z repo (bez cudzych).
// Wolne strefy: drzwi lokali, wejście NOVA z kolejką, brama podwórka, przejście z sygnalizacją (x 49…55), wiadukt, metro.
internal static class BraxMiastoUlica
{
    const float Chodnik = .15f, KrawPld = 23.755f, KrawPln = 33.245f;
    static float K => BraxMiastoA.K;
    static Transform root, kol;

    static bool Wolne(float x, bool polnoc)
    {
        if (x > 48.5f && x < 55.5f) return false;                 // przejście z sygnalizacją
        if (x > -73f && x < -59f) return false;                   // wiadukt
        if (x > 64.5f || x < -102f) return false;                 // końce ulicy (metro, wylot)
        if (polnoc && x > 1.5f && x < 18.5f) return false;        // brama podwórka
        if (polnoc && x > 32.5f && x < 45.5f) return false;       // NOVA: kolejka i wejście
        if (!polnoc && x > 18.8f && x < 23.2f) return false;      // Nutrition: drzwi
        if (!polnoc && x > 2.5f && x < 9.5f) return false;        // siłownia: wyjście
        foreach (float l in new[] { -98f, -82f, -66f, -50f, -34f, -2f, 14f, 30f, 46f, 62f }) if (Mathf.Abs(x - l) < 1.4f) return false;   // latarnie
        // 11.11 (właściciel 14:14: „odblokowujesz każde wejście”): przed każdymi drzwiami lokalu wolne 2.3 m od krawężnika do drzwi
        foreach (var d in drzwi) if (d.polnoc == polnoc && Mathf.Abs(x - d.x) < DrzwiZapas) return false;
        foreach (var z in zajete) if (z.polnoc == polnoc && x > z.x0 - .5f && x < z.x1 + .5f) return false;
        return true;
    }

    // ---------------- 11.11 PORTOWA: porządek (właściciel 14:14: „z całej ulicy usuwasz drzewa i odblokowujesz każde wejście, bo jakieś
    // siedziska stoją w wejściu… całą ulicę sprzątasz porządnie — Portowa”). Dane z APK 2bfadac: 14 drzew w misach (w drzwiach HOTELU,
    // CUKIERNI, TOWER), wiata przystanku z ławką w drzwiach BISTRO (usunięta — autobus tu nie jeździ), niewidzialne okrągłe kolizje oryginału przed BISTRO, HOTELEM
    // i DELIKATESAMI, kosze w drzwiach KWIACIARNI i KSIĘGARNI, stojaki rowerowe w drzwiach PIEKARNI, drugi hydrant przy kwiaciarni.
    const float DrzwiZapas = 2.3f;   // od osi drzwi: skrzydła ±1.0 m + wolny pas
    static readonly List<(float x, bool polnoc, string nazwa)> drzwi = new List<(float, bool, string)>();
    static readonly List<(float x0, float x1, bool polnoc)> zajete = new List<(float, float, bool)>();
    static readonly List<string> porzadek = new List<string>();
    static bool PasChodnika(float z, out bool polnoc)
    {
        polnoc = z > 28.5f;
        return polnoc ? z > KrawPln - .1f && z < 36.25f : z > 20.45f && z < KrawPld + .1f;
    }

    /// <summary>Drzwi lokali po obu stronach Portowej — z gotowej sceny (wejścia oryginału, drzwi automatyczne, klatka Braxa).</summary>
    static void ZbierzDrzwi(Transform cityRoot)
    {
        drzwi.Clear(); zajete.Clear(); porzadek.Clear();
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            bool wej = n.StartsWith("PUBLIC WORKING ENTRANCE", System.StringComparison.Ordinal) || n.StartsWith("Source-kit working entrance", System.StringComparison.Ordinal)
                       || n.StartsWith("Drzwi automatyczne:", System.StringComparison.Ordinal) || n == "Tower: drzwi automatyczne";
            if (!wej) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true);
            Vector3 c = t.position;
            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); c = b.center; }
            if (c.y > 3f || c.x < -106f || c.x > 68f) continue;
            bool pln = Mathf.Abs(c.z - 36.1f) < 1.6f, pld = Mathf.Abs(c.z - 20.6f) < 1.6f;
            if (!pln && !pld) continue;
            if (drzwi.Any(d => d.polnoc == pln && Mathf.Abs(d.x - c.x) < .8f)) continue;
            drzwi.Add((c.x, pln, n.Replace("PUBLIC WORKING ENTRANCE ", "").Replace("Source-kit working entrance ", "")));
        }
        BraxMiastoA.Log("PORTOWA drzwi lokali: " + drzwi.Count + " — " + string.Join(", ", drzwi.OrderBy(d => d.polnoc).ThenBy(d => d.x).Select(d => d.nazwa + " x" + d.x.ToString("0.0") + (d.polnoc ? " N" : " S"))));
    }

    /// <summary>Drobne przeszkody już stojące na chodniku (krzewy, kosze, hydranty, słupki) — nowe rzeczy nie stają na nich.</summary>
    /// Tylko te, które w poprzek chodnika zachodzą na pas zmin…zmax (rury spustowe i futryny przy licu nie blokują wiaty stojącej 1 m od lica).
    static List<(float x0, float x1)> Przeszkody(bool polnoc, Transform pomin, float zmin = -1e9f, float zmax = 1e9f)
    {
        var l = new List<(float, float)>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r || !r.enabled || r is SkinnedMeshRenderer || r is ParticleSystemRenderer || (pomin && r.transform.IsChildOf(pomin)) || r.transform.IsChildOf(root)) continue;
            var b = r.bounds;
            if (b.size.x > 3f || b.size.z > 3f || b.max.y < .3f || b.min.y > 1.5f) continue;
            if (b.max.z < zmin || b.min.z > zmax) continue;
            if (!PasChodnika(b.center.z, out bool p) || p != polnoc || b.center.x < -106f || b.center.x > 68f) continue;
            l.Add((b.min.x, b.max.x));
        }
        return l;
    }

    /// <summary>Najbliższe x (początek odcinka długości dl), w którym cały odcinek jest wolny (drzwi, latarnie, zajęte, przeszkody).
    /// Najpierw w stronę +x (wschód), potem −x; NaN, gdy w promieniu 14 m nie ma miejsca.</summary>
    static float Miejsce(float x, bool polnoc, float dl, List<(float x0, float x1)> przeszkody = null)
    {
        bool Ok(float s)
        {
            for (float u = s; u <= s + dl + .001f; u += .25f) if (!Wolne(u, polnoc)) return false;
            if (!Wolne(s + dl, polnoc)) return false;   // koniec odcinka dokładnie (krok .25 m mógł go przeskoczyć)
            if (przeszkody != null) foreach (var q in przeszkody) if (q.x1 > s - .3f && q.x0 < s + dl + .3f) return false;
            return true;
        }
        for (float d = 0f; d <= 14f; d += .25f) { if (Ok(x + d)) return x + d; if (d > 0f && Ok(x - d)) return x - d; }
        return float.NaN;
    }

    /// <summary>Niewidzialne kolizje na chodnikach Portowej — walce kolizji ORYGINAŁU („Original city collisions”), których przedmiot
    /// dawno zniknął (APK 2bfadac, sprawdzone wierzchołkami wszystkich siatek: przed DELIKATESAMI x 34, HOTELEM x −42, BISTRO x −18,
    /// przy NUTRITION x 20 i 24.5, rząd słupków x 44…50 przed NOVA): wyłączone. Kolizja zostaje, gdy w jej obrysie (0.3 m nad
    /// chodnikiem i wyżej) leży choć jeden wierzchołek widocznej siatki — latarnie, hydrant, kosz oryginału zostają z kolizją.
    /// Własne kolizje kroków (z bryłami) nie są ruszane.</summary>
    static int UsunNiewidzialneKolizje()
    {
        var rendery = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(r => r && r.enabled && r.bounds.max.x > -110f && r.bounds.min.x < 72f && r.bounds.max.z > 16f && r.bounds.min.z < 41f).ToArray();
        var wierzcholki = new Dictionary<Mesh, Vector3[]>();
        // obrys BEZ zapasu: zapas 0.1 m łapał gzymsy i pilastry nowych elewacji (lico 20.6, występy do 21.08) — kolizja przed
        // DELIKATESAMI i HOTELEM zostałaby, choć nic w niej nie stoi
        bool Widoczna(Bounds bb)
        {
            foreach (var r in rendery)
            {
                var rb = r.bounds; if (!rb.Intersects(bb)) continue;
                var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null;
                if (!m || !m.isReadable)
                {
                    // siatki nieczytelnej nie da się sprawdzić — drobna bryła, której obrys obejmuje środek kolizji, liczy się jako widoczna
                    if (rb.size.x <= 3f && rb.size.z <= 3f && rb.size.y >= .25f && Mathf.Abs(rb.center.x - bb.center.x) < rb.extents.x + .05f && Mathf.Abs(rb.center.z - bb.center.z) < rb.extents.z + .05f) return true;
                    continue;
                }
                if (!wierzcholki.TryGetValue(m, out var v)) { v = m.vertices; wierzcholki[m] = v; }
                var M = r.transform.localToWorldMatrix;
                foreach (var a in v) { var w = M.MultiplyPoint3x4(a); if (w.y > .3f && bb.Contains(w)) return true; }
            }
            return false;
        }
        int n = 0;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!c || !c.enabled || c.isTrigger || c.transform.IsChildOf(root)) continue;
            bool oryginal = c.name.StartsWith("Original round collider", System.StringComparison.Ordinal) || (c.transform.parent && c.transform.parent.name == "Original city collisions");
            if (!oryginal) continue;
            var b = c.bounds;
            if (b.size.x > 1.6f || b.size.z > 1.6f || b.max.y < .4f || b.min.y > 1.5f) continue;
            if (!PasChodnika(b.center.z, out _) || b.center.x < -106f || b.center.x > 66f) continue;
            if (c.GetComponentInParent<Rigidbody>()) continue;
            if (Widoczna(b)) continue;
            c.enabled = false; n++;
            porzadek.Add("niewidzialna kolizja " + c.name.Trim() + " (" + b.center.x.ToString("0.0") + ", " + b.center.z.ToString("0.0") + ", Ø " + b.size.x.ToString("0.00") + " m)");
        }
        return n;
    }

    /// <summary>11.11 (właściciel 14:50: „na ulicy Portowej nie musi być przystanków, bo tam nie będzie jeździć autobus — nawet nie ma
    /// jak zawrócić”): wiata przystanku oryginału (BraxCityEnvironment.BusShelter: słupy, dach, szyby, ławka) stała w drzwiach
    /// BISTRO — USUNIĘTA w całości, razem z tablicą reklamową w jej boku (CityMesh_128) i kolizjami oryginału w jej obrysie
    /// (ściany boczna i tylna „Source”, walec przy słupie).</summary>
    static string UsunPrzystanek(Transform cityRoot)
    {
        var w = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Finished BRAXHORN bus shelter");
        if (!w) return "przystanek: brak w scenie";
        var rs = w.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) { BraxUliceKit.Usun(w.gameObject, "przystanek na Portowej (bez brył)"); return "przystanek: usunięty (bez brył)"; }
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (!PasChodnika(b.center.z, out _)) return "przystanek: poza chodnikiem Portowej (" + b.center.ToString("F1") + ") — bez zmian";
        var bb = b; bb.Expand(new Vector3(.3f, .2f, .3f));
        var usun = new List<GameObject> { w.gameObject }; var wylacz = new List<Collider>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r && !r.transform.IsChildOf(w) && r.name.StartsWith("CityMesh_", System.StringComparison.Ordinal) && r.bounds.size.x < 3f && r.bounds.size.z < 3f && r.bounds.size.y > .5f
                && bb.Contains(r.bounds.center) && r.transform.childCount == 0) usun.Add(r.gameObject);
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c || c.transform.IsChildOf(w) || c.bounds.size.x > 5f || c.bounds.size.z > 5f || !bb.Contains(c.bounds.center)) continue;
            if (c.GetComponentInParent<Rigidbody>()) continue;
            // sam obiekt kolizji (bez brył i dzieci) — usunięty; kolizja na obiekcie z bryłą — tylko wyłączona
            if (c.transform.childCount == 0 && !c.GetComponent<Renderer>() && c.GetComponents<Component>().Length <= 3) usun.Add(c.gameObject); else wylacz.Add(c);
        }
        foreach (var c in wylacz) c.enabled = false;
        int n = 0; foreach (var g in usun.Distinct().ToList()) if (g) { BraxUliceKit.Usun(g, "przystanek na Portowej (autobus tu nie jeździ)"); n++; }
        string o = "przystanek z wiatą i ławką przed BISTRO (x " + b.min.x.ToString("0.0") + "…" + b.max.x.ToString("0.0") + "): USUNIĘTY — obiektów " + n + " (wiata, tablica reklamowa, kolizje), wyłączonych kolizji " + wylacz.Count;
        porzadek.Add(o); return o;
    }
    static float Grunt(float x, float z)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(x, 6f, z), Vector3.down, 9f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            if (h.normal.y > .8f && h.point.y < 1f && h.point.y > -.5f) return h.point.y;
        return Chodnik;
    }

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxMiastoA.WyczyscPamiec();
        // 1) stare reklamy — zdjęte
        int stare = 0;
        foreach (var t in cityRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("BRAXHORN original artwork billboard")).ToArray())
            if (t) { BraxUliceKit.Usun(t.gameObject, "stara tablica reklamowa przed oknami elewacji"); stare++; }
        root = BraxRegion.Root(cityRoot, "CLAUDE A: ulica główna — wyposażenie i reklamy");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        Physics.SyncTransforms();
        // 11.11 PORTOWA: drzwi, przystanek (usunięty), niewidzialne kolizje — przed rozstawieniem koszy, stojaków i hydrantów
        ZbierzDrzwi(cityRoot);
        string wiata = UsunPrzystanek(cityRoot);
        Physics.SyncTransforms();
        int niewidzialne = UsunNiewidzialneKolizje();
        Physics.SyncTransforms();
        BraxUliceKit.UstawGrupe("Ulica: wyposażenie chodników", 70f);
        int drzew = 0, lawek = 0, koszy = 0;
        foreach (bool polnoc in new[] { false, true })
        {
            float zl = polnoc ? KrawPln + .95f : KrawPld - .95f;   // linia mebli 0.95 m od krawężnika
            var przeszk = Przeszkody(polnoc, null, zl - .6f, zl + .6f);   // to, co już stoi w linii mebli (hydrant oryginału, stare latarnie)
            foreach (float x in new[] { -90f, -74f, -42f, -26f, -10f, 6f, 22f, 38f, 58f })
            {
                // 11.11 (właściciel 14:14: „z całej ulicy usuwasz drzewa”): drzew w misach NIE MA (Drzewo() nie jest wołane — z nim
                // znikają misa, trawa, która wchodziła do BAR 24 i kwiaciarni, i kolizja pnia). Odbiór R1 (7.11): brak ławek.
                // Kosz zostaje co 16 m, ale nigdy przed drzwiami (Wolne: oś drzwi ±2.3 m).
                float xk = x - 2.1f;
                if (Wolne(xk, polnoc) && !przeszk.Any(q => q.x1 > xk - .6f && q.x0 < xk + .6f)) { Kosz(new Vector3(xk, Grunt(xk, zl), zl)); koszy++; zajete.Add((xk - .3f, xk + .3f, polnoc)); }
                else porzadek.Add("kosz x" + xk.ToString("0.0") + (polnoc ? " N" : " S") + ": nie stawiany (drzwi / latarnia / przeszkoda)");
            }
            // stojaki rowerowe przy piekarni / delikatesach — 11.11: cały rząd poza drzwiami (dotąd 3 stojaki w drzwiach PIEKARNI)
            // miejsce hydrantu, który później stawia krok zieleni (BraxRegionVegetation: x 27.5, z 22.3) — stojaki nie wchodzą na niego
            if (!polnoc) zajete.Add((26.9f, 28.1f, false));
            float xs = Miejsce(polnoc ? 24.5f : 30.5f, polnoc, 1.8f, przeszk);
            if (!float.IsNaN(xs))
            {
                for (int i = 0; i < 3; i++) Stojak(new Vector3(xs + i * .9f, Grunt(xs, zl), zl));
                zajete.Add((xs - .4f, xs + 2.2f, polnoc));
                if (Mathf.Abs(xs - (polnoc ? 24.5f : 30.5f)) > .01f) porzadek.Add("stojaki rowerowe " + (polnoc ? "N" : "S") + ": x " + (polnoc ? 24.5f : 30.5f).ToString("0.0") + " → " + xs.ToString("0.0") + " (poza drzwiami)");
            }
            // słupki przy przejściu (narożniki, 0.5 m od krawężnika)
            foreach (float x in new[] { 48.2f, 55.8f }) foreach (float d in new[] { .5f, 1.4f })
                { var p = new Vector3(x, 0, polnoc ? KrawPln + d : KrawPld - d); p.y = Grunt(p.x, p.z); Slupek(p); }
            // hydrant przy krawężniku — 11.11: nie przed drzwiami i nie drugi obok istniejącego (APK 2bfadac: hydrant oryginału x −26
            // i nasz x −30 przy drzwiach KWIACIARNI)
            float xh0 = polnoc ? -20f : -30f, zh = polnoc ? KrawPln + .55f : KrawPld - .55f;
            bool jest = cityRoot.GetComponentsInChildren<Transform>(true).Any(t => t && !t.IsChildOf(root) && t.name.IndexOf("hydrant", System.StringComparison.OrdinalIgnoreCase) >= 0
                        && Mathf.Abs(t.position.z - zh) < 2.5f && Mathf.Abs(t.position.x - xh0) < 12f);
            float xh = jest ? float.NaN : Miejsce(xh0, polnoc, 0f, przeszk);
            if (!float.IsNaN(xh))
            {
                var hp = new Vector3(xh, 0, zh); hp.y = Grunt(hp.x, hp.z);
                var hm = BraxRegion.Model("fire_hydrant"); if (hm) BraxRegion.Put(root, hm, "Hydrant", hp, polnoc ? 180f : 0f, 1f, true, .85f * K);
                zajete.Add((xh - .4f, xh + .4f, polnoc));
            }
            else porzadek.Add("hydrant " + (polnoc ? "N" : "S") + " x" + xh0.ToString("0") + ": nie stawiany (" + (jest ? "hydrant oryginału obok" : "brak miejsca poza drzwiami") + ")");
        }
        // tablice nazw ulic (polski standard) przy końcach i przy przejściu
        foreach (var (p, polnoc, tekst) in new[] { (new Vector3(63.2f, 0, KrawPln + .45f), true, "ul. PORTOWA"), (new Vector3(63.2f, 0, KrawPld - .45f), false, "BULWAR NADMORSKI"), (new Vector3(-100.5f, 0, KrawPld - .45f), false, "ul. PORTOWA"), (new Vector3(10.5f, 0, KrawPld - .45f), false, "ul. PORTOWA") })
        {
            var q = p; float xt = Miejsce(p.x, polnoc, 0f); if (!float.IsNaN(xt)) q.x = xt;   // 11.11: słup tablicy nie przed drzwiami
            q.y = Grunt(q.x, q.z); TablicaUlicy(q, polnoc, tekst);
        }
        BraxUliceKit.UstawGrupe("Ulica: ekrany LED na wieżowcach");
        int ekr = Ekrany(cityRoot);
        int siatek = BraxUliceKit.Zapisz(root, "ulica_wyposazenie");
        BraxMiastoA.Log("ULICA: drzew w kratach " + drzew + " (11.11: usunięte z całej Portowej), ławek " + lawek + ", koszy " + koszy + ", ekranów LED " + ekr + ", siatek " + siatek + "; zdjęte stare tablice reklamowe: " + stare);
        Debug.Log("BRAXHORN_PORTOWA porządek: drzwi " + drzwi.Count + ", wyłączone niewidzialne kolizje " + niewidzialne + ", " + wiata + "; " + string.Join("; ", porzadek));
    }

    static bool Drzewo(Transform cityRoot, Vector3 p)
    {
        // chodnik pod kratą ZDJĘTY (krata i ściółka w jego miejscu, nie na nim)
        { float a0 = .75f * K; BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(p.x - a0, p.x + a0, p.y - .12f, p.y + .04f, p.z - a0, p.z + a0), "krata_drzewa", root); }
        // pkt 2 (właściciel: „jak jest drzewo na mieście, to ma być zielono koło drzewa”): zamiast żeliwnej kraty ZIELONA MISA —
        // obrzeże z granitowych krawężników (po 2 elementy na bok, fuga 4 mm, wierzch 2 cm nad chodnikiem), wewnątrz okrywa
        // zielona 3 cm poniżej chodnika (w wycięciu chodnika, nie na nim) i kępy bylin (niskie walce z liśćmi w dwóch odcieniach).
        float a = .75f * K, bw = .12f;
        var granit = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: obrzeże misy drzewa (granit)", new Color(.62f, .61f, .58f), .7f, 0f));
        foreach (var (x0, x1, z0, z1) in new[] { (p.x - a, p.x + a, p.z - a, p.z - a + bw), (p.x - a, p.x + a, p.z + a - bw, p.z + a), (p.x - a, p.x - a + bw, p.z - a + bw, p.z + a - bw), (p.x + a - bw, p.x + a, p.z - a + bw, p.z + a - bw) })
        {
            bool wzdX = x1 - x0 > z1 - z0;
            float mid = wzdX ? (x0 + x1) * .5f : (z0 + z1) * .5f;
            foreach (var (u0, u1) in new[] { (wzdX ? x0 : z0, mid - .002f), (mid + .002f, wzdX ? x1 : z1) })
                granit.Box(wzdX ? new Vector3(u0, p.y - .15f, z0) : new Vector3(x0, p.y - .15f, u0), wzdX ? new Vector3(u1, p.y + .02f, z1) : new Vector3(x1, p.y + .02f, u1));
        }
        var okr = BraxUliceKit.S(BraxMiastoA.Tex("Ulica: okrywa zielona misy drzewa", 1.2f, new Color(.85f, 1f, .8f), new Color(.20f, .36f, .14f), .95f, "leafy_grass", "aerial_grass_rock", "forest_floor"));
        float cIn = a - bw;
        okr.Quad(new Vector3(p.x - cIn, p.y - .03f, p.z - cIn), new Vector3(p.x - cIn, p.y - .03f, p.z + cIn), new Vector3(p.x + cIn, p.y - .03f, p.z + cIn), new Vector3(p.x + cIn, p.y - .03f, p.z - cIn), Vector3.up);
        // 9.11 (właściciel, zdjęcia: „zielone klocki” pod drzewami): byliny z walców 7–9-kątnych wyglądały jak zielone klocki — USUNIĘTE.
        // W misie: okrywa zielona (wyżej) + 3 kępy trawy z modeli Poly Haven (grass_medium), niskie (≤ 0.35 m), bez kolizji.
        var trawa = BraxRegion.Model("grass_medium_01", "grass_medium_02");
        if (trawa)
            for (int i = 0; i < 3; i++)
            {
                float an = (i * 120f + 35f) * Mathf.Deg2Rad, rr = cIn * .55f;
                BraxRegion.Put(root, trawa, "Drzewo w kracie: kępa trawy", new Vector3(p.x + Mathf.Cos(an) * rr, p.y - .03f, p.z + Mathf.Sin(an) * rr),
                               (i * 97f + p.x * 13f) % 360f, 1f, false, .35f * K);
            }
        BraxMiastoA.Kol(kol, "krata drzewa", new Vector3(p.x - a, p.y - .3f, p.z - a), new Vector3(p.x + a, p.y, p.z + a));
        var m = BraxRegion.Model("drzewo_verdant");
        if (!m) return false;
        var t = BraxRegion.Put(root, m, "Drzewo w kracie (ulica)", p - Vector3.up * .08f, (p.x * 37f) % 360f, 1f, false, 6.0f * K);
        var c = new GameObject("kolizja: pień drzewa"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * 1.2f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = .2f; cc.height = 2.4f;
        return t;
    }

    static void Lawka(Vector3 p, bool polnoc)
    {
        // ławka miejska 1.8 m × k: rama żeliwna, siedzisko i oparcie z listew, podłokietniki; przodem do budynków (plecami do jezdni)
        float L = 1.8f * K, hs = .45f * K, gl = .45f * K; var przod = polnoc ? Vector3.forward : Vector3.back; var bok = Vector3.right;
        var drewno = BraxUliceKit.S(BraxMiastoA.Tex("Ulica: ławka listwy (drewno)", 1.0f, new Color(.85f, .7f, .52f), new Color(.5f, .36f, .24f), .7f, "teak_veneer", "wood_planks"));
        var zel = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: rama ławki (żeliwo grafit)", new Color(.1f, .1f, .11f), .5f, .6f));
        for (int i = 0; i < 4; i++) drewno.OBox(p + Vector3.up * (hs - .02f) + przod * (gl * .5f - .06f - i * .11f), bok, Vector3.up, przod, L, .04f, .09f);
        for (int i = 0; i < 3; i++) { var q = Quaternion.AngleAxis(-15f, bok); drewno.OBox(p + Vector3.up * (hs + .18f + i * .13f) - przod * (gl * .5f + .02f + i * .03f), bok, q * Vector3.up, q * przod, L, .1f, .035f); }
        foreach (float s in new[] { -L * .5f + .08f, L * .5f - .08f })
        {
            var b = p + bok * s;
            zel.OBox(b + Vector3.up * hs * .5f + przod * (gl * .35f), bok, Vector3.up, przod, .06f, hs, .06f);
            zel.OBox(b + Vector3.up * ((hs + .5f) * .5f) - przod * (gl * .5f), bok, Vector3.up, przod, .06f, hs + .5f, .06f);   // noga tylna z oparciem
            zel.OBox(b + Vector3.up * (hs + .22f) + przod * .02f, bok, Vector3.up, przod, .06f, .05f, gl + .05f);   // podłokietnik
        }
        var c = new GameObject("kolizja: ławka"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * hs * .5f; c.AddComponent<BoxCollider>().size = new Vector3(L, hs, gl);
        BraxMiastoA.Siedzisko(root, "ŁAWKA", p + Vector3.up * hs, przod);
    }

    static void Kosz(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: kosz na śmieci (stal grafit)", new Color(.16f, .17f, .18f), .5f, .6f));
        float r = .24f * K, h = .9f * K;
        st.Walec(p, r, h - .1f, 16); st.Walec(p + Vector3.up * (h - .1f), r + .03f, .04f, 16);
        st.Walec(p + Vector3.up * (h - .06f), r - .02f, .06f, 16);   // daszek z otworem wrzutowym (czarny pierścień niżej)
        var c = new GameObject("kolizja: kosz"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * h * .5f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = r; cc.height = h;
    }

    static void Stojak(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: stal nierdzewna (stojaki, słupki)", new Color(.62f, .63f, .65f), .35f, .9f));
        float w = .7f * K, h = .8f * K;
        st.Rura(p + Vector3.left * w * .5f, p + Vector3.left * w * .5f + Vector3.up * (h - .2f), .025f, 8);
        st.Rura(p + Vector3.right * w * .5f, p + Vector3.right * w * .5f + Vector3.up * (h - .2f), .025f, 8);
        var prev = p + Vector3.left * w * .5f + Vector3.up * (h - .2f);
        for (int i = 1; i <= 8; i++) { float a = Mathf.PI * i / 8f; var q = p + new Vector3(-Mathf.Cos(a) * w * .5f, h - .2f + Mathf.Sin(a) * .2f, 0); st.Rura(prev, q, .025f, 8, false); prev = q; }
        var c = new GameObject("kolizja: stojak rowerowy"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * h * .5f; c.AddComponent<BoxCollider>().size = new Vector3(w, h, .08f);
    }

    static void Slupek(Vector3 p)
    {
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: słupek (stal grafit)", new Color(.16f, .17f, .18f), .5f, .6f));
        st.Walec(p, .07f, .9f * K, 12); st.Walec(p + Vector3.up * .9f * K, .08f, .04f, 12);
        BraxUliceKit.S(BraxMiastoA.Mat("Ulica: pasek odblaskowy słupka", new Color(.92f, .92f, .9f), .3f)).Walec(p + Vector3.up * (.9f * K - .2f), .072f, .06f, 12);
        var c = new GameObject("kolizja: słupek"); c.transform.SetParent(kol, false); c.transform.position = p + Vector3.up * .5f; var cc = c.AddComponent<CapsuleCollider>(); cc.radius = .08f; cc.height = 1f;
    }

    static void TablicaUlicy(Vector3 p, bool polnoc, string tekst)
    {
        // słup ocynkowany 2.9 m × k, tablica niebieska 0.9 × 0.25 m × k z białą obwódką (pierścień) i napisem z liter (polski wzór)
        var st = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: stal ocynkowana (słupy znaków)", new Color(.65f, .66f, .67f), .45f, .85f));
        var nieb = BraxMiastoA.Mat("Ulica: tablica nazwy ulicy (niebieska)", new Color(.05f, .22f, .55f), .4f);
        var biel = BraxMiastoA.Mat("Ulica: tablica nazwy ulicy (biel)", new Color(.95f, .95f, .95f), .4f);
        float hs = 2.9f * K, w = Mathf.Max(.9f, BraxUliceKit.SzerokoscNapisu(tekst, .1f * K) + .25f), h = .25f * K;
        st.Walec(p, .045f, hs, 10); st.Walec(p + Vector3.up * hs, .05f, .03f, 10);
        var c = p + Vector3.up * (hs - .3f); var r = Vector3.right; var n = polnoc ? Vector3.back : Vector3.forward;
        // tablica dwustronna: obwódka biała (pierścień 2 cm), pole niebieskie w otworze, napis z liter po obu stronach
        BraxUliceKit.S(biel).OBox(c + Vector3.up * (h * .5f - .01f), r, Vector3.up, n, w, .02f, .02f); BraxUliceKit.S(biel).OBox(c - Vector3.up * (h * .5f - .01f), r, Vector3.up, n, w, .02f, .02f);
        BraxUliceKit.S(biel).OBox(c + r * (w * .5f - .01f), r, Vector3.up, n, .02f, h - .04f, .02f); BraxUliceKit.S(biel).OBox(c - r * (w * .5f - .01f), r, Vector3.up, n, .02f, h - .04f, .02f);
        BraxUliceKit.S(nieb).OBox(c, r, Vector3.up, n, w - .04f, h - .04f, .02f);
        foreach (var nn in new[] { n, -n }) BraxUliceKit.Napis3D(tekst, c + nn * .01f - Vector3.up * .05f * K, nn, .1f * K, .006f, biel, biel);
        foreach (float y in new[] { -h * .3f, h * .3f }) st.Box(p + Vector3.up * (hs - .3f + y) + new Vector3(-.06f, -.02f, -.04f), p + Vector3.up * (hs - .3f + y) + new Vector3(.06f, .02f, .04f));
        var k = new GameObject("kolizja: słup tablicy"); k.transform.SetParent(kol, false); k.transform.position = p + Vector3.up * 1.2f; var cc = k.AddComponent<CapsuleCollider>(); cc.radius = .06f; cc.height = 2.4f;
    }

    // ekrany LED na wieżowcach nad ulicą (bryła ekranu bez tylnej ściany, przy elewacji wieży; treść — grafiki marki, podświetlenie)
    static int Ekrany(Transform cityRoot)
    {
        int n = 0;
        if (n == 0) return 0;   // pkt 48b (właściciel): grafiki NIGDY na wieżowcach — ekrany LED na wieżach wyłączone (nośniki: BraxMiastoBillboardy)
        var plany = new[] { ("corner_store", "brand-poster.png", false), ("restaurant", "brax-fighter.jpg", true), ("hotel", "nyxa-pink.jpg", true), ("gallery", "brand-poster.png", true) };   // ekran na ścianie wolnej od sąsiedniej wieży, w osi ulicy
        foreach (var (id, plik, naWschod) in plany)
        {
            var plan = BraxCityStreetSkyline.HighRises.FirstOrDefault(p => p.id == id); var b = BraxCityArchitecture.StreetBuildings.FirstOrDefault(x => x.id == id);
            if (plan == null || b == null) continue;
            var fp = BraxCityStreetSkyline.Footprint(b, plan); float w = Mathf.Min(fp.height - 1.2f, 8f), h = w * 1.5f, y0 = b.Height + 6f;   // ekran pionowy 2:3 (grafiki pionowe)
            // ekran na ścianie wieży skierowanej wzdłuż ulicy (wschód albo zachód) — widoczny w osi ulicy
            float xs = naWschod ? fp.xMax : fp.xMin; var N = naWschod ? Vector3.right : Vector3.left; float zc = fp.center.y;
            var rama = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: rama ekranu LED (aluminium czarne)", new Color(.05f, .05f, .055f), .45f, .6f));
            // rama-pierścień 0.35 m głębokości (bez tylnej ściany) i ekran jako lico w jej otworze
            var c = new Vector3(xs, y0 + h * .5f, zc) + N * .175f; var R = Vector3.Cross(Vector3.up, -N).normalized;
            rama.OBox(c + Vector3.up * (h * .5f + .1f), R, Vector3.up, N, w + .4f, .2f, .35f, true); rama.OBox(c - Vector3.up * (h * .5f + .1f), R, Vector3.up, N, w + .4f, .2f, .35f, true);
            rama.OBox(c + R * (w * .5f + .1f), R, Vector3.up, N, .2f, h, .35f, true); rama.OBox(c - R * (w * .5f + .1f), R, Vector3.up, N, .2f, h, .35f, true);
            var ld = new Vector3(xs, y0, zc) + N * .35f - R * w * .5f;
            BraxMiastoA.Obraz(root, "ekran LED " + id, "Assets/CityRebuild/Brand/" + plik, ld, R, w, h, N, 1.1f, .3f);
            // osłona boczna ekranu (blacha) między ekranem a elewacją wieży — ekran nie „wisi” przed szybą
            var bl = BraxUliceKit.S(BraxMiastoA.Mat("Ulica: obudowa ekranu (blacha grafit)", new Color(.08f, .08f, .09f), .6f, .4f));
            foreach (float sg in new[] { -1f, 1f })
                bl.Quad(new Vector3(xs, y0, zc) + R * sg * (w * .5f + .2f) - Vector3.up * .2f, new Vector3(xs, y0, zc) + R * sg * (w * .5f + .2f) + N * .35f - Vector3.up * .2f, new Vector3(xs, y0 + h, zc) + R * sg * (w * .5f + .2f) + N * .35f + Vector3.up * .2f, new Vector3(xs, y0 + h, zc) + R * sg * (w * .5f + .2f) + Vector3.up * .2f, R * sg);
            n++;
        }
        return n;
    }
}

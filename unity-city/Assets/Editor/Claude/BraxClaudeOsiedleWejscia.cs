using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// JEDNO WEJŚCIE DLA GOŚCI (runda 5, koordynator: „jedno wejście dla gości wszędzie, drugie tylko jako zaplecze lub ewakuacja
// (zamknięte)”). Audyt wszystkich drzwi BraxCityDoor osiedla przy końcu budowy:
//  • drzwi zewnętrzne = z jednej strony przykryte (strop/dach nad punktem 1.2 m od skrzydła), z drugiej nie; wewnętrzne pomijane;
//  • jeden budynek = drzwi, między których punktami wewnętrznymi (1.2 m nad posadzką) nic nie stoi (Linecast bez kolizji);
//  • w budynku z ≥ 2 wejściami zostaje główne (id/etykieta „front/wejście/główne”, inaczej najbliżej ulicy), pozostałe:
//    ZAMKNIĘTE na stałe (komponent wyłączony — nie otwiera się przy podejściu ani AKCJĄ), etykieta „ZAPLECZE / WYJŚCIE
//    EWAKUACYJNE”, zielony znak nad skrzydłem od środka;
//  • pomijane: domy i wille (wejście + ogród/taras to nie wejścia dla gości), drzwi z tras testów CI (backstage, back-room).
// Budynki wykonawcy B (salon, sklep stacji, biuro warsztatu) mają z budowy jedno wejście i zamknięte drzwi zaplecza.
// ZABEZPIECZENIA (koordynator po e7f2515 — „do kasyna nie dało się wejść, to nie może wrócić”):
//  • główne drzwi WPROST z kodu: Klasa(id) == Wejscie (…-front, JedyneWejscia) — kasyno, dyskoteka, sala bokserska, arena,
//    komisariat, szpital, siłownia A; takich drzwi NIGDY się nie zamyka; drzwi wewnętrzne z kodu (-hall/-office/-room/-rear)
//    też nie (w tym przejście kasyno → zaplecze, casino-zaplecze-hall); zgadywanie tylko dla pozostałych budynków;
//  • łączność: po zamknięciu siatka 0.5 m (kapsuła gracza, drzwi otwieralne przejezdne) od głównego wejścia musi dojść do
//    każdego pomieszczenia z kontekstem (drzwi wewnętrzne, punkty podróży) w okolicy; jeśli nie — zamknięcie cofnięte.
// Log BRAXHORN_WEJSCIA podwojne_przed=… podwojne=… zamkniete=… cofniete=… [główne=źródło (kod|zgadnięte), zamknięte, cofnięte].
public static partial class BraxClaudeOsiedle
{
    static readonly string[] WejMieszkalne = { "villa", "willa", "house", "dom", "contact", "kontakt", "bedroom", "bathroom", "łazienk", "sypialn", "mieszkan", "flat", "backyard", "pavilion" };
    static readonly string[] WejChronione = { "backstage", "back-room", "nova", "zaplecze-hall", "lift", "winda", "elevator" };
    static readonly string[] WejBudynkiZListy = { "casino", "kasyno", "club", "klub", "dyskotek", "boxing", "bokser", "arena", "police", "komisariat", "hospital", "szpital", "gym", "silown", "siłown" };
    static readonly string[] WejGlowne = { "front", "wejście", "wejscie", "główne", "glowne", "main", "entrance" };
    // BFS po siatce 0.5 m na wysokości drzwi (kapsuła r 0.25 od 0.35 do 1.6 m); drzwi włączone = przejezdne (otwierają się),
    // wyłączone = ściana. Cele: drzwi wewnętrzne i punkty podróży do 15 m od drzwi budynku. Zwraca nieosiągalne cele.
    static List<string> Lacznosc(Vector3 start, List<Vector3> drzwiBud, out int nCele)
    {
        const int maska = ~((1 << 8) | (1 << 2)); const float h = .5f;
        var b = new Bounds(start, Vector3.zero); foreach (var p in drzwiBud) b.Encapsulate(p); b.Expand(new Vector3(30f, 0f, 30f));
        float y = start.y < .5f ? .1f : start.y;
        BraxCityDoor[] tab = Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var aktywneDrzwi = tab.Where(d => d && d.enabled).ToArray();
        var otwieralne = aktywneDrzwi
            .Where(d => b.Contains(new Vector3(d.transform.position.x, b.center.y, d.transform.position.z)))
            .Select(d => d.transform.position).ToList();
        // Zwolnione są wyłącznie KOLIDERY AKTYWNYCH DRZWI.
        // Nigdy ściana, półka albo podłoga stojąca w tym samym miejscu.
        var kolideryDrzwi = new HashSet<Collider>(
            aktywneDrzwi.SelectMany(d => d.GetComponentsInChildren<Collider>(true)));
        var cele = new List<(string n, Vector3 p)>();
        foreach (var d in tab) if (d && d.enabled && Klasa(d.id) == KlasaDrzwi.Wewnetrzne && drzwiBud.Any(q => Vector3.Distance(q, d.transform.position) < 15f)) cele.Add((d.id, d.transform.position));
        BraxCityWorld[] swiaty = Object.FindObjectsByType<BraxCityWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var w in swiaty) if (w.points != null) foreach (var pt in w.points) if (pt != null && drzwiBud.Any(q => Vector3.Distance(q, pt.position) < 15f)) cele.Add(("punkt " + pt.id, pt.position));
        // This is a single-floor 2D flood fill. Previously, only upstairs
        // targets counted as non-empty, then were skipped at the end, giving
        // a false PASS and permitting a potentially unsafe door closure.
        // Require at least one actual reachable target on this SAME floor.
        var celeNaPoziomie = cele.Where(c => Mathf.Abs(c.p.y - y) <= 2f).ToList();
        nCele = celeNaPoziomie.Count;
        int nx = Mathf.CeilToInt(b.size.x / h), nz = Mathf.CeilToInt(b.size.z / h);
        // Poprzednio duży budynek lub brak celów zwracał PUSTĄ listę,
        // czyli fałszywe PASS. W takiej sytuacji nie wolno zamknąć drzwi.
        if (nx <= 0 || nz <= 0 || (long)nx * nz > 40000)
            return new List<string> { "obszar poza limitem wiarygodnego testu przejścia" };
        if (celeNaPoziomie.Count == 0)
            return new List<string> { "brak celów pozwalających zweryfikować dostęp do wnętrza na poziomie wejścia" };
        var wolne = new sbyte[nx * nz]; var odw = new bool[nx * nz];
        var buforKolizji = new Collider[64];
        Vector3 P(int ix, int iz) => new Vector3(b.min.x + (ix + .5f) * h, y, b.min.z + (iz + .5f) * h);
        bool Wolne(int ix, int iz)
        {
            int k = iz * nx + ix; if (wolne[k] != 0) return wolne[k] > 0;
            var p = P(ix, iz);
            var bot = p + Vector3.up * .35f;
            var top = p + Vector3.up * 1.6f;
            bool w = !Physics.CheckCapsule(bot, top, .25f, maska, QueryTriggerInteraction.Ignore);
            if (!w && otwieralne.Any(q => (new Vector2(q.x - p.x, q.z - p.z)).sqrMagnitude < .64f))
            {
                int count = Physics.OverlapCapsuleNonAlloc(
                    bot, top, .25f, buforKolizji, maska, QueryTriggerInteraction.Ignore);
                // Przepełnienie bufora = wynik niepewny, więc blokujemy.
                w = count < buforKolizji.Length;
                for (int i = 0; i < count && w; i++)
                    if (buforKolizji[i] && !kolideryDrzwi.Contains(buforKolizji[i])) w = false;
            }
            wolne[k] = (sbyte)(w ? 1 : -1); return w;
        }
        int sx = Mathf.Clamp((int)((start.x - b.min.x) / h), 0, nx - 1), sz = Mathf.Clamp((int)((start.z - b.min.z) / h), 0, nz - 1);
        if (!Wolne(sx, sz))
            return new List<string> { "próg głównego wejścia jest fizycznie zablokowany" };
        var kol = new Queue<int>(); kol.Enqueue(sz * nx + sx); odw[sz * nx + sx] = true;
        while (kol.Count > 0)
        {
            int k = kol.Dequeue(), ix = k % nx, iz = k / nx;
            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int jx = ix + dx, jz = iz + dz; if (jx < 0 || jz < 0 || jx >= nx || jz >= nz) continue;
                int j = jz * nx + jx; if (odw[j] || !Wolne(jx, jz)) continue; odw[j] = true; kol.Enqueue(j);
            }
        }
        var brak = new List<string>();
        foreach (var (n, p) in celeNaPoziomie)
        {
            int ix = Mathf.Clamp((int)((p.x - b.min.x) / h), 0, nx - 1), iz = Mathf.Clamp((int)((p.z - b.min.z) / h), 0, nz - 1); bool ok = false;
            for (int ddx = -2; ddx <= 2 && !ok; ddx++) for (int ddz = -2; ddz <= 2 && !ok; ddz++) { int jx = ix + ddx, jz = iz + ddz; if (jx >= 0 && jz >= 0 && jx < nx && jz < nz && odw[jz * nx + jx]) ok = true; }
            if (!ok) brak.Add(n);
        }
        return brak;
    }
    static void JednoWejscie()
    {
        Physics.SyncTransforms();
        const int maska = ~((1 << 8) | (1 << 2));
        bool Przykryty(Vector3 p) =>
            Physics.Raycast(p + Vector3.up * 2.2f, Vector3.up, 16f, maska, QueryTriggerInteraction.Ignore) ||
            spodyStropow.Any(b => p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z && b.min.y > p.y);
        BraxCityDoor[] tab = Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<BraxCityDoor> wszDrzwi = tab.Where(d => d && d.enabled).ToList();
        var zew = new List<(BraxCityDoor d, Vector3 wew, Vector3 zewP)>();
        int wewn = 0, nieokr = 0;
        foreach (var d in wszDrzwi)
        {
            var n = d.transform.forward; n.y = 0f; if (n.sqrMagnitude < 1e-4f) continue; n.Normalize();
            var p = d.transform.position; p.y = Mathf.Max(p.y, .2f);
            Vector3 a = p + n * 1.2f, b = p - n * 1.2f; bool ka = Przykryty(a), kb = Przykryty(b);
            if (ka && kb) { wewn++; continue; }
            if (!ka && !kb) { nieokr++; continue; }
            zew.Add((d, (ka ? a : b) + Vector3.up * 1.0f, (ka ? b : a)));
        }
        // budynki: łączenie drzwi, których punkty wewnętrzne się widzą (do 40 m)
        int m = zew.Count; var rodzic = Enumerable.Range(0, m).ToArray();
        int F(int i) { while (rodzic[i] != i) i = rodzic[i] = rodzic[rodzic[i]]; return i; }
        for (int i = 0; i < m; i++)
            for (int j = i + 1; j < m; j++)
            {
                if (Vector3.Distance(zew[i].wew, zew[j].wew) > 40f) continue;
                if (!Physics.Linecast(zew[i].wew, zew[j].wew, maska, QueryTriggerInteraction.Ignore)) rodzic[F(i)] = F(j);
            }
        bool Ma(BraxCityDoor d, string[] slowa) { string s = ((d.id ?? "") + " " + (d.label ?? "")).ToLowerInvariant(); return slowa.Any(s.Contains); }
        float DoUlicy(Vector3 p)
        {
            float best = Mathf.Abs(p.z - StreetZ);
            if (ulice != null) foreach (var u in ulice) best = Mathf.Min(best, u.alongX ? (p.x >= Mathf.Min(u.a0, u.a1) && p.x <= Mathf.Max(u.a0, u.a1) ? Mathf.Abs(p.z - u.c) : 999f) : (p.z >= Mathf.Min(u.a0, u.a1) && p.z <= Mathf.Max(u.a0, u.a1) ? Mathf.Abs(p.x - u.c) : 999f));
            return best;
        }
        int podwojnePrzed = 0, podwojnePo = 0, zamkniete = 0, cofniete = 0; var lista = new List<string>();
        foreach (var grupa in Enumerable.Range(0, m).GroupBy(F).Where(g => g.Count() > 1))
        {
            var g = grupa.ToList();
            if (g.Any(i => Ma(zew[i].d, WejMieszkalne))) continue;   // dom/willa — nie budynek dla gości
            podwojnePrzed++;
            var zKodu = g.Where(i => Klasa(zew[i].d.id) == KlasaDrzwi.Wejscie).ToList();
            if (zKodu.Count == 0 && g.Any(i => Ma(zew[i].d, WejBudynkiZListy)))
            {   // budynek z listy (kasyno, dyskoteka, …) bez drzwi głównych rozpoznanych z kodu — nie zgaduję, nic nie zamykam
                podwojnePo++; lista.Add("{budynek z listy bez wejścia z kodu — nie zamykam: " + string.Join(", ", g.Select(i => zew[i].d.id)) + "}"); continue;
            }
            int glowne = zKodu.Count > 0 ? zKodu[0] : g.OrderByDescending(i => Ma(zew[i].d, WejGlowne) ? 1 : 0).ThenBy(i => DoUlicy(zew[i].zewP)).First();
            int zostalo = 1; var nazwy = new List<string> { "główne=" + (zKodu.Count > 0 ? "kod" : "zgadnięte") + ": " + (zew[glowne].d.id ?? zew[glowne].d.label) };
            var zamknieteTu = new List<BraxCityDoor>();
            foreach (int i in g.Where(i => i != glowne))
            {
                var d = zew[i].d;
                if (Klasa(d.id) != KlasaDrzwi.Nieznane) { zostalo++; nazwy.Add("z kodu (" + Klasa(d.id) + ", nie zamykam): " + d.id); continue; }
                if (Ma(d, WejChronione)) { zostalo++; nazwy.Add("chronione (test CI / zaplecze kasyna): " + (d.id ?? d.label)); continue; }
                zamknieteTu.Add(d);
                d.label = "ZAPLECZE / WYJŚCIE EWAKUACYJNE (zamknięte) — " + d.label; d.enabled = false; zamkniete++;
                nazwy.Add("zamknięte: " + (d.id ?? "?"));
            }
            // łączność po zamknięciu: od głównego wejścia do każdego pomieszczenia z kontekstem w okolicy
            if (zamknieteTu.Count > 0)
            {
                var brak = Lacznosc(zew[glowne].zewP, g.Select(i => zew[i].d.transform.position).ToList(), out int cele);
                if (brak.Count > 0)
                {
                    foreach (var d in zamknieteTu) { d.enabled = true; d.label = d.label.Replace("ZAPLECZE / WYJŚCIE EWAKUACYJNE (zamknięte) — ", ""); zamkniete--; cofniete++; zostalo++; nazwy.Add("cofnięte (łączność): " + d.id); }
                    nazwy.Add("nieosiągalne po zamknięciu: " + string.Join(", ", brak.Take(4)));
                }
                else
                {
                    // Zielone znaki należą tylko do drzwi faktycznie zamkniętych.
                    // Poprzednio rysowano je PRZED testem łączności i pozostały
                    // nawet wtedy, gdy zamknięcie cofnięto z powodu blokady.
                    var znak = StSwiatlo("znak wyjścia", new Color(.05f, .7f, .25f));
                    foreach (var d in zamknieteTu)
                    {
                        var wejscie = zew.First(k => k.d == d);
                        var dn = d.transform.position;
                        var kier = wejscie.wew - dn; kier.y = 0;
                        if (kier.sqrMagnitude < 1e-6f) kier = d.transform.forward;
                        kier.y = 0; kier.Normalize();
                        var c = dn + kier * .12f + Vector3.up * 2.35f;
                        var pr = Vector3.Cross(Vector3.up, kier) * .4f;
                        Quad(znak, c - pr, c + pr, c + pr + Vector3.up * .22f,
                            c - pr + Vector3.up * .22f, kier);
                    }
                    nazwy.Add("łączność ok (" + cele + " celów)");
                }
            }
            if (zostalo > 1) podwojnePo++;
            lista.Add("{" + string.Join(", ", nazwy) + "}");
        }
        string log = "BRAXHORN_WEJSCIA podwojne_przed=" + podwojnePrzed + " podwojne=" + podwojnePo + " zamkniete=" + zamkniete + " cofniete=" + cofniete + " drzwi=" + wszDrzwi.Count + " zewnętrzne=" + m +
                     " wewnętrzne=" + wewn + " nieokreślone=" + nieokr + " (budynki B: salon 1 wejście + zaplecze zamknięte, sklep stacji 1, biuro warsztatu 1; bramy aut to nie wejścia dla gości) [" + string.Join(" ", lista) + "]";
        Debug.Log(log); rep.notes.Add(log.Replace("BRAXHORN_WEJSCIA ", "wejścia: "));
        if (cofniete > 0)
            rep.notes.Add("DO POPRAWKI: " + cofniete +
                " dodatkowych wejść przywrócono, bo fizyczna łączność budynku nie została potwierdzona");
    }
}

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
// Log BRAXHORN_WEJSCIA podwojne_przed=… podwojne=… zamkniete=… [lista].
public static partial class BraxClaudeOsiedle
{
    static readonly string[] WejMieszkalne = { "villa", "willa", "house", "dom", "contact", "kontakt", "bedroom", "bathroom", "łazienk", "sypialn", "mieszkan", "flat", "backyard", "pavilion" };
    static readonly string[] WejChronione = { "backstage", "back-room", "nova", "zaplecze-hall", "lift", "winda", "elevator" };
    static readonly string[] WejGlowne = { "front", "wejście", "wejscie", "główne", "glowne", "main", "entrance" };
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
            if (ulice != null) foreach (var u in ulice) best = Mathf.Min(best, u.alongX ? (p.x >= u.a0 && p.x <= u.a1 ? Mathf.Abs(p.z - u.c) : 999f) : (p.z >= Mathf.Min(u.a0, u.a1) && p.z <= Mathf.Max(u.a0, u.a1) ? Mathf.Abs(p.x - u.c) : 999f));
            return best;
        }
        int podwojnePrzed = 0, podwojnePo = 0, zamkniete = 0; var lista = new List<string>();
        foreach (var grupa in Enumerable.Range(0, m).GroupBy(F).Where(g => g.Count() > 1))
        {
            var g = grupa.ToList();
            if (g.Any(i => Ma(zew[i].d, WejMieszkalne))) continue;   // dom/willa — nie budynek dla gości
            podwojnePrzed++;
            int glowne = g.OrderByDescending(i => Ma(zew[i].d, WejGlowne) ? 1 : 0).ThenBy(i => DoUlicy(zew[i].zewP)).First();
            int zostalo = 1; var nazwy = new List<string> { "główne: " + (zew[glowne].d.label ?? zew[glowne].d.id) };
            foreach (int i in g.Where(i => i != glowne))
            {
                var d = zew[i].d;
                if (Ma(d, WejChronione)) { zostalo++; nazwy.Add("chronione (test CI): " + (d.label ?? d.id)); continue; }
                d.label = "ZAPLECZE / WYJŚCIE EWAKUACYJNE (zamknięte) — " + d.label; d.enabled = false; zamkniete++;
                var w = zew[i].wew; var dn = d.transform.position; var kier = (w - dn); kier.y = 0; kier.Normalize();
                var znak = StSwiatlo("znak wyjścia", new Color(.05f, .7f, .25f)); var c = dn + kier * .12f + Vector3.up * (2.35f);
                var pr = Vector3.Cross(Vector3.up, kier) * .4f;
                Quad(znak, c - pr, c + pr, c + pr + Vector3.up * .22f, c - pr + Vector3.up * .22f, kier);
                nazwy.Add("zamknięte: " + (d.id ?? "?"));
            }
            if (zostalo > 1) podwojnePo++;
            lista.Add("{" + string.Join(", ", nazwy) + "}");
        }
        string log = "BRAXHORN_WEJSCIA podwojne_przed=" + podwojnePrzed + " podwojne=" + podwojnePo + " zamkniete=" + zamkniete + " drzwi=" + wszDrzwi.Count + " zewnętrzne=" + m +
                     " wewnętrzne=" + wewn + " nieokreślone=" + nieokr + " (budynki B: salon 1 wejście + zaplecze zamknięte, sklep stacji 1, biuro warsztatu 1; bramy aut to nie wejścia dla gości) [" + string.Join(" ", lista) + "]";
        Debug.Log(log); rep.notes.Add(log.Replace("BRAXHORN_WEJSCIA ", "wejścia: "));
    }
}

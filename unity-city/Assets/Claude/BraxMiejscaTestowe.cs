using System.Collections.Generic;
using UnityEngine;

// MENU → TEST → TELEPORT (build 14; właściciel 22:54: „teleport do miejsc — każdy lokal, park, boiska, tunel… szybkie
// sprawdzanie bez chodzenia”). Lista budowana z punktów podróży świata (BraxCityWorld.points: punkty ze źródła rejonu —
// kasyno, dyskoteka, komisariat, szpital, sala bokserska, hala walk, willa, park, boisko, wylot tunelu — oraz punkty
// dopisane przy budowie osiedla: Park Centralny, fontanna, osiedle B, sklepy, siłownia, Nutrition, kort, wille na sprzedaż).
// Każdy punkt przypisany do kategorii po słowach w id/nazwie, w kolejności ze zlecenia: kawiarnia, market, siłownia,
// arena, kasyno, szpital, komisariat, park, boiska, tunel, wille, potem reszta („INNE”).
// Kawiarnia i market osiedla B nie mają własnych punktów — liczone z układu pierzei sklepów (BraxClaudeOsiedle.ShopRow:
// 5 lokali od x = −12.8 do 48.7, front działek z = −186.3; plac przed witrynami) — sprawdzić na zdjęciu po zmianach osiedla.
public static class BraxMiejscaTestowe
{
    public sealed class Miejsce { public string kategoria, klucz, nazwa, id; public Vector3 pozycja; public float yaw = float.NaN; }

    static readonly (string kat, string[] slowa)[] Kategorie =
    {
        ("kawiarnia", new[] { "kawiar", "cafe", "café" }),
        ("market", new[] { "market", "sklep", "shop", "nutrition" }),
        ("silownia", new[] { "silown", "siłown", "gym" }),
        ("arena", new[] { "arena", "hala walk", "oktagon", "boxing", "bokser" }),
        ("kasyno", new[] { "casino", "kasyn" }),
        ("szpital", new[] { "hospital", "szpital" }),
        ("komisariat", new[] { "police", "komisariat", "policj" }),
        ("park", new[] { "park", "fontann" }),
        ("boiska", new[] { "court", "boisk", "kort", "fight1" }),
        ("tunel", new[] { "tunel", "tunnel", "estate_entry" }),
        ("wille", new[] { "villa", "will", "wiśniowa" }),
    };

    // czytelne nazwy punktów źródłowych osiedla (w źródle: „park”, „court”…)
    static readonly Dictionary<string, string> Nazwy = new Dictionary<string, string>
    {
        { "estate_entry", "Wylot tunelu z miasta" }, { "park", "Park Wiśniowy" }, { "court", "Boisko do kosza (Park Wiśniowy)" },
        { "villa_front", "Wiśniowa 4 — wejście" }, { "villa_living", "Wiśniowa 4 — salon" }, { "villa_terrace", "Wiśniowa 4 — taras" },
        { "garages", "Garaże" }, { "contact", "Poznańska 5" }, { "arena", "ARENA BRAXHORN (hala walk)" },
    };

    public static List<Miejsce> Lista(BraxGymPlayer gracz)
    {
        var wynik = new List<Miejsce>();
        var w = gracz ? gracz.cityWorld : null;
        var reszta = new List<Miejsce>();
        var kat = new List<Miejsce>[Kategorie.Length];
        for (int i = 0; i < kat.Length; i++) kat[i] = new List<Miejsce>();
        if (w != null && w.points != null)
            foreach (var p in w.points)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                string nazwa = Nazwy.TryGetValue(p.id, out var n) ? n : string.IsNullOrEmpty(p.label) ? p.id : p.label;
                var m = new Miejsce { id = p.id, nazwa = nazwa, pozycja = p.position };
                string tekst = (p.id + " " + p.label).ToLowerInvariant();
                int k = Kategoria(tekst);
                if (k >= 0) { m.kategoria = Kategorie[k].kat; kat[k].Add(m); } else { m.kategoria = "inne"; reszta.Add(m); }
            }
        // osiedle: kawiarnia i market w pierzei sklepów osiedla B (bez własnych punktów podróży)
        if (w != null && w.area == "estate")
        {
            const float x0 = -12.8f, unit = (48.7f - x0) / 5f, z = -183.8f;
            if (kat[0].Count == 0) kat[0].Insert(0, new Miejsce { kategoria = "kawiarnia", nazwa = "Kawiarnia (osiedle B, pierzeja sklepów)", pozycja = new Vector3(x0 + 3.5f * unit, .3f, z), yaw = 180f });
            if (!kat[1].Exists(m => m.nazwa.ToLowerInvariant().Contains("market")))
                kat[1].Insert(0, new Miejsce { kategoria = "market", nazwa = "Market (osiedle B, pierzeja sklepów)", pozycja = new Vector3(x0 + 4.5f * unit, .3f, z), yaw = 180f });
        }
        // miasto: wjazd do tunelu na osiedle (koniec Alei Południowej, jak przyjazd z osiedla — BraxCityWorld.CityPortal; wiadukt usunięty 2.11)
        if (w != null && w.area == "city" && kat[9].Count == 0)
            kat[9].Add(new Miejsce { kategoria = "tunel", nazwa = "Koniec Alei Południowej (dawny tunel, ul. Dworcowa)", pozycja = new Vector3(-126f, .3f, -78f), yaw = 180f });
        foreach (var l in kat) wynik.AddRange(l);
        wynik.AddRange(reszta);
        foreach (var m in wynik) m.klucz = "miejsce." + m.kategoria;   // klucz tłumaczenia raz (bez sklejania napisów co klatkę)
        return wynik;
    }

    static int Kategoria(string tekst)
    {
        for (int i = 0; i < Kategorie.Length; i++)
            foreach (var s in Kategorie[i].slowa)
                if (tekst.Contains(s)) return i;
        return -1;
    }

    public static string NazwaKategorii(Miejsce m) => BraxTeksty.T(m.klucz ?? "miejsce.inne");

    public static void Przenies(BraxGymPlayer g, Miejsce m)
    {
        if (!g || m == null) return;
        if (g.seat) g.seat.Leave();
        if (g.vessel && !g.vessel.Dismount()) g.Command("reset");
        if (!string.IsNullOrEmpty(m.id) && g.cityWorld) g.cityWorld.GoTo(m.id);
        else if (float.IsNaN(m.yaw)) g.TravelTo(m.pozycja);
        else g.TravelTo(m.pozycja, m.yaw);
        Debug.Log("BRAXHORN_UI teleport: " + m.kategoria + " · " + m.nazwa + " · " + m.pozycja);
    }
}

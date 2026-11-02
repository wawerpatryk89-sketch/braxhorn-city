using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// MENU → UTKNĄŁEM (build 14; propozycja zatwierdzona przez właściciela 21:27: „przycisk UTKNĄŁEM = powrót na najbliższy chodnik”).
// Kolizje chodników nie mają nazw (osiedle: zbiorcze pudełka „kolizja”), więc chodnik i drogę rozpoznajemy po tym, kto po nich
// chodzi i jeździ:
//  1) chodnik = odcinki przechodniów (BraxPieszy: pola a, b — punkty co 1.5 m),
//  2) droga = trasy aut (BraxAuto: pole trasa) — tylko gdy chodnik jest daleko (> 40 m),
//  3) w ostateczności najbliższy punkt podróży rejonu (BraxCityWorld.points), a gdy nic — start rejonu.
// Każdy kandydat sprawdzany: podłoże pod spodem (promień w dół) i wolne miejsce na kapsułę postaci (CheckCapsule),
// co najmniej 1.5 m od miejsca, w którym gracz utknął. Pola czytane przez refleksję po nazwie — zmiana tych klas przez
// inne zlecenia (ludzie, auta) nie zepsuje kompilacji, najwyżej przejdzie do następnego źródła.
// Wołane raz, na dotknięcie przycisku (alokacje tylko wtedy).
public static class BraxUtknalem
{
    const int Maska = ~((1 << 8) | (1 << 29));   // bez gracza i bez przeszkód „tylko dla kamery”

    public static string Ratuj(BraxGymPlayer g)
    {
        if (!g) return "";
        if (g.seat) g.seat.Leave();
        if (g.vessel && !g.vessel.Dismount()) { g.Command("reset"); return BraxTeksty.T("msg.reset"); }
        Vector3 p = g.transform.position;
        float r = g.body ? g.body.radius : .35f, h = g.body ? g.body.height : 2.2f;
        var chodnik = new List<Vector3>(); var droga = new List<Vector3>();
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!mb) continue;
            var typ = mb.GetType(); string n = typ.Name;
            if (n == "BraxPieszy")
            {
                if (Pole(mb, typ, "a", out Vector3 a) && Pole(mb, typ, "b", out Vector3 b)) Probki(a, b, 1.5f, chodnik);
            }
            else if (n == "BraxAuto")
            {
                var f = typ.GetField("trasa", BindingFlags.Public | BindingFlags.Instance);
                if (f != null && f.GetValue(mb) is Vector3[] t && t.Length > 1)
                    for (int i = 0; i < t.Length; i++) Probki(t[i], t[(i + 1) % t.Length], 2f, droga);
            }
        }
        if (Najblizszy(chodnik, p, r, h, 120f, out Vector3 c)) { Przenies(g, c); return BraxTeksty.F("msg.chodnik", Mathf.RoundToInt(Plasko(c - p))); }
        if (Najblizszy(droga, p, r, h, 120f, out Vector3 d)) { Przenies(g, d); return BraxTeksty.F("msg.droga", Mathf.RoundToInt(Plasko(d - p))); }
        var w = g.cityWorld;
        if (w != null && w.points != null && w.points.Length > 0)
        {
            BraxCityPoint best = null; float bd = float.MaxValue;
            foreach (var pt in w.points) { if (pt == null) continue; float dd = (pt.position - p).sqrMagnitude; if (dd < bd && dd > 2.25f) { bd = dd; best = pt; } }
            if (best != null) { w.GoTo(best.id); return BraxTeksty.F("msg.punkt", string.IsNullOrEmpty(best.label) ? best.id : best.label); }
        }
        g.Command("reset");
        return BraxTeksty.T("msg.reset");
    }

    static float Plasko(Vector3 v) { v.y = 0f; return v.magnitude; }

    static bool Pole(MonoBehaviour mb, System.Type typ, string nazwa, out Vector3 v)
    {
        v = default;
        var f = typ.GetField(nazwa, BindingFlags.Public | BindingFlags.Instance);
        if (f == null || f.FieldType != typeof(Vector3)) return false;
        v = (Vector3)f.GetValue(mb); return true;
    }

    static void Probki(Vector3 a, Vector3 b, float co, List<Vector3> lista)
    {
        float l = Vector3.Distance(a, b); int n = Mathf.Clamp(Mathf.CeilToInt(l / co), 1, 400);
        for (int i = 0; i <= n; i++) lista.Add(Vector3.Lerp(a, b, i / (float)n));
    }

    // najbliższy kandydat w zasięgu, z podłożem i wolnym miejscem na kapsułę postaci
    static bool Najblizszy(List<Vector3> lista, Vector3 p, float r, float h, float zasieg, out Vector3 wynik)
    {
        wynik = default;
        if (lista.Count == 0) return false;
        lista.Sort((x, y) => Odl(x, p).CompareTo(Odl(y, p)));
        int prob = 0;
        foreach (var k in lista)
        {
            float d = Odl(k, p);
            if (d > zasieg * zasieg) break;
            if (d < 1.5f * 1.5f) continue;
            if (++prob > 80) break;
            if (!Physics.Raycast(k + Vector3.up * 2.2f, Vector3.down, out RaycastHit hit, 5f, Maska, QueryTriggerInteraction.Ignore)) continue;
            if (hit.normal.y < .7f) continue;
            Vector3 gp = hit.point;
            if (Physics.CheckCapsule(gp + Vector3.up * (r + .12f), gp + Vector3.up * Mathf.Max(r + .13f, h - r), r * .95f, Maska, QueryTriggerInteraction.Ignore)) continue;
            wynik = gp + Vector3.up * .08f; return true;
        }
        return false;
    }

    // odległość z karą za różnicę wysokości (piętro wyżej / niżej to nie „najbliższy chodnik”)
    static float Odl(Vector3 a, Vector3 p) { var v = a - p; return v.x * v.x + v.z * v.z + v.y * v.y * 9f; }

    static void Przenies(BraxGymPlayer g, Vector3 c)
    {
        g.TravelTo(c);
        Debug.Log("BRAXHORN_UI utknąłem: przeniesiono na " + c);
    }
}

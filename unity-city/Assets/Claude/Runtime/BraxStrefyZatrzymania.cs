using System;
using System.Collections.Generic;
using UnityEngine;

// WSPÓLNE API BUILDU 14 (2.11, dyspozytor): strefy, w których auta / piesi mają się zatrzymać — światła na skrzyżowaniach,
// przejścia dla pieszych, ustąpienie pierwszeństwa na rondzie, szlaban, przeszkoda. Rejestrują je sygnalizacja, rondo,
// przejścia; pytają auta (BraxAuto) i przechodnie (BraxPieszy). Klasa statyczna (nie komponent) — bez danych w scenie.
public static class BraxStrefyZatrzymania
{
    public struct Strefa
    {
        public Vector3 srodek;        // środek strefy (linia zatrzymania)
        public Vector3 kierunek;      // kierunek ruchu, którego dotyczy (płaski); zero = każdy kierunek
        public float promien;         // zasięg w metrach (płasko)
        public bool dlaPieszych;      // true = dotyczy pieszych, false = aut
        public Func<bool> aktywna;    // true = stój (np. czerwone światło); null = zawsze aktywna
    }

    static readonly Dictionary<int, Strefa> strefy = new Dictionary<int, Strefa>();
    static int nast = 1;

    public static int Dodaj(Strefa s) { int id = nast++; strefy[id] = s; return id; }
    public static void Usun(int id) { strefy.Remove(id); }
    public static void Wyczysc() { strefy.Clear(); }

    // Czy obiekt w punkcie `pozycja`, jadący/idący w kierunku `kierunekRuchu`, ma się zatrzymać przed strefą?
    public static bool CzyStop(Vector3 pozycja, Vector3 kierunekRuchu, bool pieszy)
    {
        kierunekRuchu.y = 0f; if (kierunekRuchu.sqrMagnitude > 1e-4f) kierunekRuchu.Normalize();
        foreach (var kv in strefy)
        {
            var s = kv.Value;
            if (s.dlaPieszych != pieszy) continue;
            var d = s.srodek - pozycja; d.y = 0f;
            if (d.sqrMagnitude > s.promien * s.promien) continue;
            if (s.kierunek.sqrMagnitude > 1e-4f && kierunekRuchu.sqrMagnitude > 1e-4f && Vector3.Dot(s.kierunek.normalized, kierunekRuchu) < .5f) continue;
            if (Vector3.Dot(d, kierunekRuchu) < -0.5f) continue;   // strefa już za nami
            if (s.aktywna == null || s.aktywna()) return true;
        }
        return false;
    }
}

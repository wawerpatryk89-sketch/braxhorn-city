using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// E3a (runda 3, właściciel 22:41: „drogi są szersze?”) — POSZERZENIE ULIC A do 2 × 3.5 m × K (9.06 m) tam, gdzie jest miejsce.
// Wołane w DrogiA zaraz po opisie ulic (przed węzłami, wycięciami i budową) — cały generator dróg (jezdnie, płyty węzłów, rondo,
// krawężniki, chodniki, zebry, latarnie, sygnalizacja, parkingi bulwarów, trasy aut i pieszych) liczy się z UlicaA, więc
// przebudowa idzie od nowa z nowym przekrojem (stara nawierzchnia nie jest przykrywana — powstaje tylko nowa).
//  • miejsce po każdej stronie = zieleń między krawężnikiem a chodnikiem + wolne za chodnikiem (lub za krawężnikiem, gdy po tej
//    stronie nie ma chodnika) minus 0.1 m zapasu; wolne = min z: promieni (co 2 m, 0.5 i 1.2 m nad ziemią, do 8 m, poza
//    węzłami), obrysów działek z kodu (Houses — płoty nie wszędzie mają kolizje; działki przy B2 do ścian domów z 35.75),
//    obrysów budynków (6 budynków usługowych, siłownia A) i NOWYCH obrysów ulic już poszerzonych (ulice równoległe);
//  • chodnik nigdy węższy niż dziś (szerokość chodnika bez zmian, przesuwa się na zewnątrz tylko o tyle, ile zabraknie zieleni);
//  • oś bez zmian (rondo, światła, tunel); wyjątek: uliczki S1/S2 (jedna strona = płot działek) — oś odsunięta od działek;
//  • końce bulwarów B1–B3 (parkingi przed budynkami) i place przed budynkami dosunięte do nowej krawędzi Wiśniowej/Bydgoskiej;
//  • gdzie miejsca brak — ulica węższa niż cel, z logiem (co ogranicza).
// Log: BRAXHORN_DROGI_SZEROKOSC (przed/po dla każdej ulicy), BRAXHORN_DROGI_POSZERZENIE (podsumowanie).
public static partial class BraxClaudeOsiedle
{
    static readonly Dictionary<string, float> szerPrzed = new Dictionary<string, float>();
    static readonly Dictionary<string, string> szerOpis = new Dictionary<string, string>();

    struct ProstPrzeszkoda { public string co; public float x0, x1, z0, z1; }

    static List<ProstPrzeszkoda> PrzeszkodyDrog()
    {
        var l = new List<ProstPrzeszkoda>();
        foreach (var h in Houses)
        {
            float xu = -h.x, z0 = h.z - h.hd, z1 = h.z + h.hd;
            if (Mathf.Abs(h.z - 27.5f) < 1.5f) z1 = Mathf.Min(z1, 35.75f);   // działki przy B2: płot z 35.55, ściany domów Codexa do z 35.7
            l.Add(new ProstPrzeszkoda { co = "działka (" + xu.ToString("F0") + ", " + h.z.ToString("F0") + ")", x0 = xu - 11.5f, x1 = xu + 11.5f, z0 = z0, z1 = z1 });
        }
        foreach (var (fx, fz) in new[] { (86f, -42f), (86f, 14f), (-86f, -42f), (-86f, 14f), (86f, 64f), (-86f, 64f) })
            l.Add(new ProstPrzeszkoda { co = "budynek (" + fx.ToString("F0") + ", " + fz.ToString("F0") + ")", x0 = fx - 20f, x1 = fx + 20f, z0 = fz - 15f, z1 = fz + 15f });
        l.Add(new ProstPrzeszkoda { co = "siłownia A", x0 = 63f, x1 = 106f, z0 = -91f, z1 = -62f });
        return l;
    }

    // Wolne miejsce za krawędzią zewnętrzną strony s ulicy u (do 8 m). Zwraca odległość i co ogranicza.
    static (float wolne, string co) WolneZa(UlicaA u, int s, float zewn, List<ProstPrzeszkoda> prost)
    {
        const int maska = ~((1 << 8) | (1 << 29) | (1 << 2));
        float best = 8f; string co = "—";
        for (float a = u.a0 + 2f; a <= u.a1 - 2f; a += 2f)
        {
            bool wezel = ulice.Any(v => v != u && v.alongX != u.alongX && u.c >= v.a0 - 1f && u.c <= v.a1 + 1f && Mathf.Abs(a - v.c) < v.Half + 1.5f);
            bool rondo = u.alongX ? Mathf.Abs(u.c - RondoZ) < .1f && Mathf.Abs(a) < RondoChodnik + 1f : Mathf.Abs(u.c) < .1f && Mathf.Abs(a - RondoZ) < RondoChodnik + 1f;
            if (wezel || rondo) continue;
            // promienie (kolizje: płoty, mury, słupy)
            foreach (float y in new[] { .5f, 1.2f })
            {
                var start = u.P(a, s * zewn, y); var dir = u.Wpoprzek * s;
                if (Physics.Raycast(start, dir, out var hit, 8f, maska, QueryTriggerInteraction.Ignore) && hit.distance < best)
                { best = hit.distance; co = "kolizja " + (hit.collider ? hit.collider.name : "?") + " przy " + BraxKontrola.F(hit.point); }
            }
            // obrysy z kodu (działki, budynki) i nowe obrysy innych ulic
            float kr = u.c + s * zewn;   // współrzędna krawędzi (z dla ulicy wzdłuż x, x dla ulicy wzdłuż z)
            void Prost(string nazwa, float x0, float x1, float z0, float z1)
            {
                float a0 = u.alongX ? x0 : z0, a1 = u.alongX ? x1 : z1, p0 = u.alongX ? z0 : x0, p1 = u.alongX ? z1 : x1;
                if (a < a0 - .2f || a > a1 + .2f) return;
                float d = s > 0 ? p0 - kr : kr - p1;
                if (s > 0 ? p1 <= kr : p0 >= kr) return;    // cały obrys po drugiej stronie krawędzi (wewnątrz pasa — to nie przeszkoda tej strony)
                d = Mathf.Max(0f, d);
                if (d < best) { best = d; co = nazwa; }
            }
            foreach (var r in prost) Prost(r.co, r.x0, r.x1, r.z0, r.z1);
            foreach (var v in ulice)
            {
                if (v == u || v.alongX != u.alongX) continue;   // ulice prostopadłe = węzły (pominięte wyżej)
                float lo = v.c - v.HalfNeg, hi = v.c + v.HalfPos;
                if (v.alongX) Prost("ulica " + (v.nazwa ?? v.id), v.a0, v.a1, lo, hi); else Prost("ulica " + (v.nazwa ?? v.id), lo, hi, v.a0, v.a1);
            }
        }
        return (best, co);
    }

    static void PoszerzUliceA()
    {
        Physics.SyncTransforms();
        szerPrzed.Clear(); szerOpis.Clear();
        float cel = 3.5f * K; const float zapas = .1f;
        var prost = PrzeszkodyDrog();
        int doCelu = 0, czesc = 0, bez = 0;
        // kolejność: bulwary, Poznańska, Wiśniowa/Bydgoska, na końcu uliczki S1/S2 (ich wolne miejsce zależy od nowej Wiśniowej/Bydgoskiej)
        foreach (var u in ulice.OrderBy(q => q.id.StartsWith("S") ? 2 : q.id.StartsWith("N") ? 1 : 0).ToList())
        try
        {
            szerPrzed[u.id] = 2f * u.cw;
            if (u.cw >= cel - .005f) { szerOpis[u.id] = "już ≥ cel"; doCelu++; continue; }
            var cap = new float[2]; var ogr = new string[2]; var wol = new float[2];
            foreach (int s in new[] { -1, 1 })
            {
                int i = s < 0 ? 0 : 1;
                float zewn = s < 0 ? u.HalfNeg : u.HalfPos;
                var (w, co) = WolneZa(u, s, zewn, prost);
                wol[i] = w; ogr[i] = co;
                cap[i] = (u.WalkS(s) > 0 ? u.GreenS(s) : 0f) + Mathf.Max(0f, w - zapas);
            }
            float need = cel - u.cw;
            bool osWolna = u.id == "S1" || u.id == "S2";
            float eN, eP;
            if (osWolna)
            {
                float tot = Mathf.Min(2f * need, cap[0] + cap[1]);
                eN = Mathf.Min(cap[0], tot * .5f); eP = Mathf.Min(cap[1], tot * .5f);
                float reszta = tot - eN - eP;
                if (reszta > 0f) { float dN = Mathf.Min(reszta, cap[0] - eN); eN += dN; reszta -= dN; eP += Mathf.Min(reszta, cap[1] - eP); }
            }
            else { eN = eP = Mathf.Max(0f, Mathf.Min(need, Mathf.Min(cap[0], cap[1]))); }
            if (eN + eP < .02f) { szerOpis[u.id] = "brak miejsca (ogranicza: −" + ogr[0] + " / +" + ogr[1] + ")"; bez++; continue; }
            float c0 = u.c, cw0 = u.cw, gN0 = u.greenNeg, gP0 = u.greenPos;
            u.c = c0 + (eP - eN) * .5f; u.cw = cw0 + (eN + eP) * .5f;
            if (u.walkNeg > 0) u.greenNeg = Mathf.Max(0f, gN0 - eN);
            if (u.walkPos > 0) u.greenPos = Mathf.Max(0f, gP0 - eP);
            float przesN = u.walkNeg > 0 ? Mathf.Max(0f, eN - gN0) : eN, przesP = u.walkPos > 0 ? Mathf.Max(0f, eP - gP0) : eP;
            bool pelne = u.cw >= cel - .01f; if (pelne) doCelu++; else czesc++;
            szerOpis[u.id] = (pelne ? "do celu" : "częściowo — ogranicza " + (cap[0] <= cap[1] ? "−: " + ogr[0] : "+: " + ogr[1]))
                + "; oś " + (Mathf.Abs(u.c - c0) > .005f ? c0.ToString("F2") + "→" + u.c.ToString("F2") : "bez zmian")
                + "; krawędź zewn. −/+ odsunięta o " + przesN.ToString("F2") + "/" + przesP.ToString("F2") + " m; zieleń −/+ " + gN0.ToString("F2") + "→" + u.greenNeg.ToString("F2") + "/" + gP0.ToString("F2") + "→" + u.greenPos.ToString("F2")
                + "; wolne było −/+ " + wol[0].ToString("F2") + "/" + wol[1].ToString("F2");
        }
        catch (Exception e) { szerOpis[u.id] = "BŁĄD pomiaru — bez zmian: " + e.Message; bez++; Debug.LogError("BRAXHORN_DROGI_POSZERZENIE DO POPRAWKI " + u.id + ": " + e.Message); }
        // końce bulwarów (parkingi przed budynkami) i place przed budynkami — do nowej krawędzi Wiśniowej / Bydgoskiej
        var n1 = ulice.FirstOrDefault(q => q.id == "N1"); var n2 = ulice.FirstOrDefault(q => q.id == "N2");
        float kW = Mathf.Min(-64f, n1 != null ? n1.c - n1.HalfNeg : -64f), kE = Mathf.Max(64f, n2 != null ? n2.c + n2.HalfPos : 64f);
        foreach (var u in ulice.Where(q => q.alongX)) { u.a0 = Mathf.Min(u.a0, kW); u.a1 = Mathf.Max(u.a1, kE); }
        bulwarKoniecW = kW; bulwarKoniecE = kE;
        Debug.Log("BRAXHORN_DROGI_POSZERZENIE ulic=" + ulice.Count + " do_celu=" + doCelu + " częściowo=" + czesc + " bez_zmian=" + bez + " cel=" + (2f * cel).ToString("F2")
                  + " m końce_bulwarów x " + kW.ToString("F2") + " / " + kE.ToString("F2") + " (chodniki bez zwężenia, oś stała poza S1/S2)");
    }
    static float bulwarKoniecW = -64f, bulwarKoniecE = 64f;

    // ---------------- oznakowanie w wycięciach (DrogiA) ----------------
    // Farba prostokątna z DrogiA zbierana w trakcie budowy dróg i kładziona NA KOŃCU: asfalt (wszystkie grupy A) wycięty
    // w obrysie farby, farba na wysokości asfaltu (lico farby = lico asfaltu, bez nakładania i bez 2 cm „naklejki”).
    // Wielokąty (strzałki, „zęby”) — jak dotąd 2 cm nad asfaltem (wycięcie prostokątne zostawiłoby dziury wokół grotu).
    static bool farbaOdroczona;
    static readonly List<(float x0, float x1, float z0, float z1)> farbaOdroczonaLista = new List<(float, float, float, float)>();
    static int FarbaWWycieciach()
    {
        farbaOdroczona = false;
        int n = 0, ciete = 0;
        var g = group; group = GrupaFarbyA;
        foreach (var (x0, x1, z0, z1) in farbaOdroczonaLista)
        {
            ciete += WytnijZGrupy((gr, m) => m == Asphalt && gr.StartsWith("Osiedle A"), MinMax(x0, x1, YAsf - .02f, YAsf + .02f, z0, z1));
            Top(Paint, x0, x1, z0, z1, YAsf); n++;
        }
        group = g;
        farbaOdroczonaLista.Clear();
        Debug.Log("BRAXHORN_DROGI_OZNAKOWANIE w_wycięciach=" + n + " prostokątów (trójkątów asfaltu przyciętych " + ciete + ") wielokąty_2cm_nad_asfaltem=pozostają");
        return n;
    }
}

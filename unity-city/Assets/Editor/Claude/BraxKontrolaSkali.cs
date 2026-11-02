using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using J = BraxKontrola.Jednostka;

// SKALA WZGLĘDEM BOHATERA (właściciel 1.11, 20:58 i 21:00: „krzesła małe… skala programu wszystkiego”, „ławka i kosz jak dla
// dziecka”). Miarą świata jest postać gracza: k = H / 1.80 m (BraxClaudeOsiedle.ZmierzBohatera). Każdy rozpoznany mebel /
// przedmiot jest mierzony i — jeśli wychodzi poza normę (wymiar rzeczywisty × k, tolerancja ±10 % wokół środka normy) —
// skalowany jednolicie wokół podstawy (środek spodu zostaje na podłodze) do najbliższej granicy normy (najmniejsza zmiana,
// np. sofa 1.81 m → 2.0 × k; krzesło: siedzisko 0.45 × k). Razem z meblem: jego osobne
// kolizje z generatora, rzeczy na nim (wazon, poduszka) i punkty siedzenia (BraxSeat.surfaceY).
// Pomiar: wysokość (stół 0.75, lada, kosz, drzwi…), długość dłuższego boku w poziomie (sofa 2.0–2.4, leżak, łóżko, rower)
// albo wysokość siedziska / blatu — wysokość największej powierzchni zwróconej w górę (histogram pól trójkątów co 2 cm;
// blat: najwyższa z powierzchni ≥ 60 % największej — stół z obrusem i świecznikiem mierzony po blacie, nie po świeczniku).
// Nie skaluje: budynków, aut, ludzi (pominięte w spisie), siatek scalonych (wiele rzeczy w jednej siatce), kopii
// zatwierdzonej siłowni, typów „tylko raport” (drzwi — otwór w ścianie się nie zmienia; lada — wbudowana między ściany;
// sprzęt siłowni i stoły do gry — wymiary zależne od modelu). Te trafiają do raportu, gdy są poza normą.
public static class BraxKontrolaSkali
{
    sealed class Wynik { public string typ, opis; public float przed, po, cel; public bool poprawiony, poza; public Vector3 gdzie; public string pomiar; }

    public static void Wykonaj(BraxKontrola.Spis spis)
    {
        float k = BraxKontrola.K; normalne.Clear();
        var wyniki = new List<Wynik>();
        foreach (var j in spis.jednostki)
        {
            if (j.typ == null) continue;
            try { var w = Sprawdz(j, k); if (w != null) wyniki.Add(w); }
            catch (Exception e) { BraxKontrola.Zapisz("skala: błąd przy " + j.opis + ": " + e.Message); }
        }
        // drzwi BraxCityDoor (skrzydła z oryginalnego modelu): tylko raport wysokości skrzydła
        try
        {
            foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (d.leaves == null) continue;
                bool any = false; Bounds b = default;
                foreach (var l in d.leaves) if (l) foreach (var r in l.GetComponentsInChildren<Renderer>(false)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (!any) continue;
                float c = 2.10f * k, h = b.size.y; bool poza = h < c * .9f || h > c * 1.1f;
                wyniki.Add(new Wynik { typ = "drzwi", opis = "BraxCityDoor " + d.id + " " + d.label, przed = h, po = h, cel = c, poza = poza, gdzie = b.center, pomiar = "wysokość skrzydła" });
            }
        }
        catch (Exception e) { BraxKontrola.Zapisz("skala: drzwi BraxCityDoor: " + e.Message); }

        var sb = new StringBuilder();
        sb.Append("BRAXHORN_SKALA_RAPORT ").Append(BraxKontrola.Obszar).Append(": wzrost bohatera ").Append(BraxKontrola.H.ToString("F2")).Append(" m, k ").Append(k.ToString("F3"))
          .Append(", sprawdzone=").Append(wyniki.Count).Append(" poprawione=").Append(wyniki.Count(w => w.poprawiony)).Append(" poza_norma_po=").Append(wyniki.Count(w => w.poza));
        foreach (var g in wyniki.GroupBy(w => w.typ).OrderBy(g => g.Key))
            sb.Append("\nBRAXHORN_SKALA_RAPORT ").Append(g.Key).Append(": sprawdzone=").Append(g.Count()).Append(" poprawione=").Append(g.Count(w => w.poprawiony)).Append(" poza_norma_po=").Append(g.Count(w => w.poza));
        sb.Append("\nBRAXHORN_SKALA_RAPORT najgorsze (przed → po, cel):");
        foreach (var w in wyniki.OrderByDescending(w => Mathf.Abs(Mathf.Log(Mathf.Max(.01f, w.przed) / Mathf.Max(.01f, w.cel)))).Take(20))
        {
            string l = "  " + w.typ + " | " + w.opis + " | " + w.pomiar + " " + w.przed.ToString("F2") + " → " + w.po.ToString("F2") + " m (cel " + w.cel.ToString("F2") + ")" +
                       (w.poprawiony ? " POPRAWIONE" : "") + (w.poza ? " POZA NORMĄ" : "") + " " + BraxKontrola.F(w.gdzie);
            sb.Append('\n').Append(l); BraxKontrola.Zapisz("skala: " + l.Trim());
        }
        foreach (var w in wyniki.Where(w => w.poza)) BraxKontrola.Zapisz("skala poza normą: " + w.typ + " | " + w.opis + " " + w.pomiar + " " + w.po.ToString("F2") + " (cel " + w.cel.ToString("F2") + ") " + BraxKontrola.F(w.gdzie));
        Debug.Log(sb.ToString());
    }

    // norma: [min(lo, c·0.9), max(hi, c·1.1)] · k, cel = c · k, c = środek normy
    static bool WNormie(float m, float lo, float hi, float k) { float c = (lo + hi) * .5f; return m >= Mathf.Min(lo, c * .9f) * k && m <= Mathf.Max(hi, c * 1.1f) * k; }

    static Wynik Sprawdz(J j, float k)
    {
        var typ = j.typ;
        float lo = typ.lo1, hi = typ.hi1; var p = typ.p1;
        if (typ.kosz && (j.wewnatrz = Wewnatrz(j))) { lo = .30f; hi = .75f; }      // kosz w pomieszczeniu: 30–75 cm (biurowy … kuchenny)
        if (typ.nazwa == "zestaw") { p = BraxKontrola.Pomiar.Wysokosc; lo = .72f; hi = .78f; }
        float m = Zmierz(j, p, out string opisPomiaru);
        if (float.IsNaN(m) && typ.p2 != BraxKontrola.Pomiar.Brak) { p = typ.p2; lo = typ.lo2; hi = typ.hi2; m = Zmierz(j, p, out opisPomiaru); }
        if (float.IsNaN(m) || m <= .01f) return null;
        float cel = Mathf.Clamp(m, lo * k, hi * k);                               // najbliższa granica normy
        var w = new Wynik { typ = typ.nazwa + (typ.kosz && j.wewnatrz ? " (wnętrze)" : ""), opis = j.opis, przed = m, po = m, cel = cel, gdzie = j.Podstawa, pomiar = opisPomiaru };
        if (WNormie(m, lo, hi, k)) return w;
        if (typ.raport || j.chroniony) { w.poza = true; return w; }
        float f = Mathf.Clamp(cel / m, .25f, 5f);
        j.Skaluj(f, j.Podstawa);
        float po = Zmierz(j, p, out _);
        w.po = float.IsNaN(po) ? m * f : po; w.poprawiony = true;
        w.poza = !WNormie(w.po, lo, hi, k);
        // druga miara po skalowaniu — tylko informacja (np. sofa: długość w normie, a siedzisko?)
        if (typ.p2 != BraxKontrola.Pomiar.Brak && p == typ.p1)
        {
            float m2 = Zmierz(j, typ.p2, out string o2);
            if (!float.IsNaN(m2) && !WNormie(m2, typ.lo2 * .85f, typ.hi2 * 1.15f, k)) BraxKontrola.Zapisz("skala: " + typ.nazwa + " " + j.opis + " po skalowaniu " + o2 + " " + m2.ToString("F2") + " m — proporcje nietypowe (norma " + (typ.lo2 * k).ToString("F2") + "–" + (typ.hi2 * k).ToString("F2") + ")");
        }
        return w;
    }

    public static float Zmierz(J j, BraxKontrola.Pomiar p, out string opis)
    {
        j.Odswiez();
        switch (p)
        {
            case BraxKontrola.Pomiar.Wysokosc:
                if (j.typ != null && j.typ.nazwa == "zestaw") { opis = "wysokość stołu w zestawie"; return WysokoscStoluWZestawie(j); }
                opis = "wysokość"; return j.b.size.y;
            case BraxKontrola.Pomiar.Dlugosc: opis = "długość"; return j.dl;
            case BraxKontrola.Pomiar.Siedzisko: opis = "siedzisko"; return Poziom(j, .2f, false);
            case BraxKontrola.Pomiar.Blat: opis = "blat"; return Poziom(j, .35f, true);
        }
        opis = "—"; return float.NaN;
    }

    static float WysokoscStoluWZestawie(J j)
    {
        bool any = false; float top = 0;
        foreach (var r in j.rs)
        {
            var n = BraxKontrola.Norm(r.name) + BraxKontrola.Norm(r.transform.parent ? r.transform.parent.name : "");
            var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) n += BraxKontrola.Norm(mf.sharedMesh.name);
            if (!System.Text.RegularExpressions.Regex.IsMatch(n, @"\b(table\w*|stol\w*)\b")) continue;
            if (!any || r.bounds.max.y > top) top = r.bounds.max.y; any = true;
        }
        return any ? top - j.b.min.y : float.NaN;
    }

    // Wysokość siedziska: histogram (co 2 cm) pól trójkątów zwróconych w górę (n.y ≥ 0.85) między 20 % a 100 % wysokości mebla.
    // Szczyt histogramu = siedzisko (największa pozioma powierzchnia: siedzisko krzesła, poduchy sofy, deski ławki). Wymaga
    // pola szczytu ≥ 3 % obrysu — inaczej NaN (pomiar zapasowy: wysokość / długość).
    public static float Poziom(J j, float odUlamka, bool najwyzszy)
    {
        float H = j.b.size.y; if (H < .1f) return float.NaN;
        const float krok = .02f; int nb = Mathf.Clamp(Mathf.CeilToInt(H / krok) + 1, 4, 400);
        var hist = new float[nb]; float y0 = j.b.min.y;
        foreach (var r in j.rs)
        {
            var mf = r ? r.GetComponent<MeshFilter>() : null;
            if (!mf || !BraxKontrola.Dane(mf.sharedMesh, out var v, out var t)) continue;
            var m = r.transform.localToWorldMatrix; int step = t.Length > 300000 ? 3 * (t.Length / 300000 + 1) : 3;
            for (int i = 0; i + 2 < t.Length; i += step)
            {
                Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
                var n = Vector3.Cross(b - a, c - a); float pole2 = n.magnitude; if (pole2 < 1e-7f) continue;
                float ny = n.y / pole2;
                if (Mathf.Abs(ny) < .85f) continue;
                if (ny < 0) { if (Vector3.Dot(m.MultiplyVector(Normalna(mf.sharedMesh, t[i])), Vector3.up) < .5f) continue; }   // nawinięcie odwrotne, normalna w górę
                float y = (a.y + b.y + c.y) / 3f - y0; if (y < H * odUlamka || y > H + .001f) continue;
                int bi = Mathf.Clamp(Mathf.FloorToInt(y / krok), 0, nb - 1); hist[bi] += pole2 * .5f * step / 3f;
            }
        }
        float best = 0; int bk = -1; var gl = new float[nb];
        for (int i = 0; i < nb; i++) { gl[i] = hist[i] + (i > 0 ? hist[i - 1] : 0) + (i + 1 < nb ? hist[i + 1] : 0); if (gl[i] > best) { best = gl[i]; bk = i; } }
        if (bk < 0 || best < .03f * Mathf.Max(.05f, j.dl * j.sz)) return float.NaN;
        if (najwyzszy) for (int i = nb - 1; i > bk; i--) if (gl[i] >= best * .6f && hist[i] >= gl[i] * .34f) { bk = i; break; }
        // środek ciężkości okna 3 przedziałów
        float sw = 0, sy = 0; for (int i = Mathf.Max(0, bk - 1); i <= Mathf.Min(nb - 1, bk + 1); i++) { sw += hist[i]; sy += hist[i] * (i + .5f) * krok; }
        return sw > 0 ? sy / sw : (bk + .5f) * krok;
    }
    static readonly Dictionary<Mesh, Vector3[]> normalne = new Dictionary<Mesh, Vector3[]>();
    static Vector3 Normalna(Mesh m, int vi)
    {
        if (!normalne.TryGetValue(m, out var n)) { try { n = m.normals; } catch { n = null; } normalne[m] = n; }
        return n != null && vi < n.Length ? n[vi] : Vector3.zero;
    }

    // Kosz w pomieszczeniu: nad nim strop (≤ 6 m), a wokół ściany w ≥ 3 z 4 kierunków (≤ 8 m) — inaczej kosz uliczny.
    public static bool Wewnatrz(J j)
    {
        var p = new Vector3(j.b.center.x, j.b.max.y + .05f, j.b.center.z);
        int maska = BraxKontrola.MaskaStala | BraxKontrola.MaskaSond;
        if (!BraxKontrola.Promien(p, Vector3.up, 6f, maska, j, out _)) return false;
        int sciany = 0; var s = new Vector3(j.b.center.x, j.b.min.y + 1.5f * BraxKontrola.K, j.b.center.z);
        foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) if (BraxKontrola.Promien(s, d, 8f, maska, j, out _)) sciany++;
        return sciany >= 3;
    }
}

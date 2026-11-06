using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using J = BraxKontrola.Jednostka;

// KOLIZJE DLA KAŻDEGO MEBLA I PRZEDMIOTU (właściciel 1.11, 22:22: „czy nie przenika postać przez coś”; 22:23: przechodzenie
// przez stół ruletki). Każdy mebel / przedmiot z widoczną siatką w pasie 0–2 m nad swoją podłogą, który nie ma kolizji
// (własnej ani osobnej z generatora pokrywającej ≥ 50 % jego obrysu), dostaje BoxCollider dopasowany do kształtu w tym pasie:
// prostokąt o najmniejszym polu z wierzchołków siatki leżących w pasie (latarnia → sam słup, a nie wysięgnik; parasol i latarnia
// tylko do 1.2 m — pod czaszą parasola da się przejść). Pomijane: trawa, kwiaty, liście, dywany (płaskie < 6 cm), drobiazgi
// < 25 cm, rzeczy, których spód jest > 2.3 m nad podłogą, drzwi (mają własne skrzydła), kopia zatwierdzonej siłowni,
// a z przedmiotów ogólnych — kształty z otworem (rama drzwi, łuk, pergola: promień przez środek nie trafia w siatkę) i schody
// (≥ 3 poziomy stopni co 10–25 cm), żeby nowa kolizja nie zamknęła przejścia.
public static class BraxKontrolaKolizji
{
    static readonly Regex Otwor = new Regex(@"\b(otwor\w*|portal\w*|rama|ramy|frame\w*|arch\w*|luk|luki|brama\w*|gate\w*|pergol\w*|wiat\w*|altan\w*|schod\w*|stair\w*|stopn\w*|rampa\w*|ramp)\b");
    static readonly Collider[] bufor = new Collider[64];
    static readonly RaycastHit[] bufR = new RaycastHit[32];

    // M1 posiada własne, precyzyjne kolizje podłóg, schodów, poręczy i mebli
    // dodawane w BraxWnetrzaKit. Nie wolno zamieniać scalonych siatek dekoracyjnych
    // (szkło wiaty, atlas oznakowania, listwy) w wielkie blokujące BoxCollider.
    // Potwierdzono w logu APK R1 #98: „BRAXHORN kolizja (kontrola)” blokowała
    // wejście i schody — mimo że oryginalne kolizje stacji były poprawne.
    static bool AutorskieKolizjeMetra(Transform t)
    {
        for (var p = t; p; p = p.parent)
            if (p.name.StartsWith("CLAUDE metro M1", StringComparison.Ordinal)) return true;
        return false;
    }

    // Centralna oś wejścia NOVA (x=42, z=35.73..36.68, y do 2.4 m).
    // Nie dodajemy wtórnego pudełka kontroli przecinającego ten korytarz.
    // Oryginalne mury i rzeczywiste drzwi nie są tu wyłączane ani modyfikowane.
    static bool ZajmujePortalNova(Vector2 c, float kat, float dl, float sz, float y0, float y1)
    {
        if (y0 > 2.4f || y1 < .25f) return false;
        var odwrot = Quaternion.Inverse(Quaternion.Euler(0f, kat, 0f));
        for (float z = 35.73f; z <= 36.70f; z += .24f)
        {
            var d = odwrot * new Vector3(42.0f - c.x, 0f, z - c.y);
            if (Mathf.Abs(d.x) <= dl * .5f + .34f &&
                Mathf.Abs(d.z) <= sz * .5f + .34f)
                return true;
        }
        return false;
    }

    public static void Wykonaj(BraxKontrola.Spis spis)
    {
        int dodane = 0, maja = 0, nad = 0, male = 0, plaskie = 0, otwory = 0, schody = 0, chronione = 0;
        var dodaneTypy = new Dictionary<string, int>();
        Transform wspolny = null;
        Physics.SyncTransforms();
        // 3.11 (metro „nie da się wejść”, dyskoteka „nie można wejść”): pudełko kontroli nie powstaje w przejściu drzwi —
        // punkty skrzydeł wszystkich drzwi (BraxCityDoor), promień = pół obrysu pudełka + 1.2 m
        var drzwiPkt = new List<Vector3>(); int przyDrzwiach = 0;
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!d) continue; drzwiPkt.Add(d.transform.position);
            if (d.leaves != null) foreach (var l in d.leaves) if (l) { var rr = l.GetComponentInChildren<Renderer>(true); drzwiPkt.Add(rr ? rr.bounds.center : l.position); }
        }
        // 3.11 (dyskoteka „nie można wejść”): także drzwi automatyczne przesuwne (BraxDrzwiPrzesuwne — NOVA, arena, wieżowce,
        // szpital, komisariat): środek otworu i skrzydła; skrzydła tych drzwi nigdy nie dostają pudełka kontroli
        var skrzydlaPrzes = new HashSet<Transform>();
        foreach (var d in UnityEngine.Object.FindObjectsByType<BraxDrzwiPrzesuwne>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!d) continue; drzwiPkt.Add(d.transform.position + Vector3.up * 1.2f);
            foreach (var r in d.GetComponentsInChildren<Renderer>(true)) { drzwiPkt.Add(r.bounds.center); skrzydlaPrzes.Add(r.transform); }
        }
        foreach (var j in spis.jednostki)
        {
            try
            {
                if (j.chroniony) { chronione++; continue; }
                // R1: żadnych sztucznych kolizji w metrze M1. Obiekty w pełni
                // należące do stacji mają fizykę authored; pozostałe obszary
                // (NOVA, miasto, port) przechodzą przez dotychczasowy audyt.
                if (AutorskieKolizjeMetra(j.t) ||
                    (j.rs.Any() && j.rs.All(r => r && AutorskieKolizjeMetra(r.transform))))
                { chronione++; continue; }
                if (j.typ != null && (j.typ.nazwa == "drzwi" || j.typ.wisi)) continue;
                if (j.rs.Any(r => r && (skrzydlaPrzes.Contains(r.transform) || r.GetComponentInParent<BraxDrzwiPrzesuwne>()))) { przyDrzwiach++; continue; }
                if (j.rs.All(BraxKontrola.Roslina2)) continue;
                j.Odswiez();
                var b = j.b;
                if (b.size.y < .06f) { plaskie++; continue; }
                if (Mathf.Max(j.dl, Mathf.Max(j.sz, b.size.y)) < .25f) { male++; continue; }
                float podl = b.min.y;
                if (BraxKontrola.Promien(new Vector3(j.srodekXZ.x, b.min.y + .05f, j.srodekXZ.y), Vector3.down, 30f, BraxKontrola.MaskaStala | BraxKontrola.MaskaSond, j, out var h)) podl = h.point.y;
                if (b.min.y - podl > 2.3f) { nad++; continue; }
                float pas = j.typ != null && (j.typ.nazwa == "parasol" || j.typ.nazwa == "latarnia") ? 1.2f : 2.0f;
                float y0 = b.min.y, y1 = Mathf.Min(b.max.y, Mathf.Max(b.min.y, podl) + pas);
                if (y1 - y0 < .05f) { nad++; continue; }
                if (MaKolizje(j, y0, y1)) { maja++; continue; }
                // wierzchołki w pasie
                var pts = new List<Vector3>(2048); float ymin = float.MaxValue, ymax = float.MinValue;
                foreach (var r in j.rs)
                {
                    var mf = r ? r.GetComponent<MeshFilter>() : null;
                    if (!mf || !BraxKontrola.Dane(mf.sharedMesh, out var v, out _)) { if (r) BraxKontrola.Narozniki(r, pts); continue; }
                    var m = r.transform.localToWorldMatrix; int step = Mathf.Max(1, v.Length / 3000);
                    for (int i = 0; i < v.Length; i += step) { var p = m.MultiplyPoint3x4(v[i]); if (p.y < y0 - .01f || p.y > y1 + .01f) continue; pts.Add(p); if (p.y < ymin) ymin = p.y; if (p.y > ymax) ymax = p.y; }
                }
                if (pts.Count < 4) { pts.Clear(); foreach (var r in j.rs) if (r) BraxKontrola.Narozniki(r, pts); ymin = y0; ymax = y1; }
                ymin = Mathf.Max(ymin, y0); ymax = Mathf.Min(Mathf.Max(ymax, ymin + .05f), y1);
                BraxKontrola.Prostokat(pts, out float kat, out Vector2 c, out float dl, out float sz);
                if (dl < .05f) continue;
                sz = Mathf.Max(sz, .04f);
                if (j.typ == null)
                {
                    if (Otwor.IsMatch(j.tekst)) { otwory++; continue; }
                    if (ymax - ymin >= 1.6f && dl >= .7f && Pusty(j, c, kat, dl, sz, ymin, ymax)) { otwory++; BraxKontrola.Zapisz("kolizje: pominięty kształt z otworem " + j.opis + " " + BraxKontrola.F(j.Podstawa)); continue; }
                    if (dl >= .8f && Schody(j)) { schody++; BraxKontrola.Zapisz("kolizje: pominięte schody " + j.opis + " " + BraxKontrola.F(j.Podstawa)); continue; }
                }
                { float pr = Mathf.Max(dl, sz) * .5f + 1.2f; bool blok = false;
                  foreach (var dp in drzwiPkt) { if (Mathf.Abs(dp.y - (ymin + ymax) * .5f) > 3f) continue; var dd = new Vector2(dp.x - c.x, dp.z - c.y); if (dd.sqrMagnitude < pr * pr) { blok = true; break; } }
                  if (blok) { przyDrzwiach++; BraxKontrola.Zapisz("kolizje: pominięta przy drzwiach " + j.opis + " " + BraxKontrola.F(j.Podstawa)); continue; } }
                if (ZajmujePortalNova(c, kat, dl, sz, ymin, ymax))
                {
                    przyDrzwiach++;
                    BraxKontrola.Zapisz("kolizje: NOVA zachowany prześwit wejścia — bez pudełka kontroli " + j.opis);
                    continue;
                }
                // nowa kolizja: dziecko mebla (jedzie z nim przy podziale sceny), przy niejednolitej skali rodzica — wspólny korzeń
                var g = new GameObject("BRAXHORN kolizja (kontrola)");
                var ls = j.t.lossyScale; float s = (Mathf.Abs(ls.x) + Mathf.Abs(ls.y) + Mathf.Abs(ls.z)) / 3f;
                bool jednolita = s > 1e-4f && Mathf.Abs(Mathf.Abs(ls.x) - s) < s * .01f && Mathf.Abs(Mathf.Abs(ls.y) - s) < s * .01f && Mathf.Abs(Mathf.Abs(ls.z) - s) < s * .01f;
                if (jednolita) g.transform.SetParent(j.t, false);
                else { if (!wspolny) wspolny = new GameObject("BRAXHORN kolizje z kontroli").transform; g.transform.SetParent(wspolny, false); s = 1f; }
                g.layer = j.t.gameObject.layer == BraxKontrola.WarstwaSondy ? 0 : j.t.gameObject.layer;
                g.transform.position = new Vector3(c.x, (ymin + ymax) * .5f, c.y);
                g.transform.rotation = Quaternion.Euler(0, kat, 0);
                g.transform.localScale = Vector3.one;
                var bc = g.AddComponent<BoxCollider>(); bc.size = new Vector3(dl, ymax - ymin, sz) / s;
                j.kol.Add(bc); spis.poKolizji[bc] = j;
                dodane++; string tn = j.typ != null ? j.typ.nazwa : "przedmiot ogólny";
                dodaneTypy[tn] = dodaneTypy.TryGetValue(tn, out var q) ? q + 1 : 1;
                if (dodane <= 300) BraxKontrola.Zapisz("kolizje: dodana " + tn + " " + j.opis + " " + dl.ToString("F2") + " × " + sz.ToString("F2") + " × " + (ymax - ymin).ToString("F2") + " m " + BraxKontrola.F(j.Podstawa));
            }
            catch (Exception e) { BraxKontrola.Zapisz("kolizje: błąd " + j.opis + ": " + e.Message); }
        }
        Physics.SyncTransforms();
        Debug.Log("BRAXHORN_KOLIZJE " + BraxKontrola.Obszar + ": dodane=" + dodane + " (" + string.Join(", ", dodaneTypy.OrderByDescending(x => x.Value).Select(x => x.Key + " " + x.Value)) + ")" +
                  ", już z kolizją " + maja + ", pominięte: nad 2.3 m " + nad + ", drobne " + male + ", płaskie " + plaskie + ", z otworem " + otwory + ", schody " + schody + ", chronione " + chronione + ", przy drzwiach (bez pudełka) " + przyDrzwiach);
    }

    // kolizja własna (komponent w meblu) albo osobna kolizja pokrywająca ≥ 50 % obrysu pasu 0–2 m
    static bool MaKolizje(J j, float y0, float y1)
    {
        foreach (var c in j.kol) if (c && c.enabled && !c.isTrigger && c.gameObject.layer != BraxKontrola.WarstwaKamery && c.gameObject.layer != BraxKontrola.WarstwaSondy) return true;
        var b = j.b; float pole = Mathf.Max(1e-4f, b.size.x * b.size.z);
        var half = new Vector3(Mathf.Max(.02f, j.dl * .4f), Mathf.Max(.02f, (y1 - y0) * .4f), Mathf.Max(.02f, j.sz * .4f));
        int n = Physics.OverlapBoxNonAlloc(new Vector3(j.srodekXZ.x, (y0 + y1) * .5f, j.srodekXZ.y), half, bufor, Quaternion.Euler(0, j.kat, 0), BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var cb = bufor[i].bounds;
            if (cb.max.y <= y0 + .05f) continue;                         // podłoga
            float ix = Mathf.Min(cb.max.x, b.max.x) - Mathf.Max(cb.min.x, b.min.x), iz = Mathf.Min(cb.max.z, b.max.z) - Mathf.Max(cb.min.z, b.min.z);
            if (ix > 0 && iz > 0 && ix * iz >= pole * .5f) return true;
        }
        return false;
    }

    // promienie wzdłuż krótszego boku przez środek na 3 wysokościach: żaden nie trafia w siatkę przedmiotu → otwór (rama, łuk)
    static bool Pusty(J j, Vector2 c, float kat, float dl, float sz, float y0, float y1)
    {
        var q = Quaternion.Euler(0, kat, 0); var v = q * Vector3.forward;
        foreach (float f in new[] { .2f, .5f, .8f })
        {
            var start = new Vector3(c.x, Mathf.Lerp(y0, y1, f), c.y) - v * (sz * .5f + .15f);
            int n = Physics.RaycastNonAlloc(start, v, bufR, sz + .3f, BraxKontrola.MaskaSond, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var r = BraxKontrola.Sondy.Czyj(bufR[i].collider); if (r && j.rs.Contains(r)) return false; }
        }
        return true;
    }

    // ≥ 3 poziomy powierzchni zwróconych w górę (≥ 0.03 m² każdy), kolejne co 10–25 cm → schody
    static bool Schody(J j)
    {
        var poziomy = new Dictionary<int, float>();
        foreach (var r in j.rs)
        {
            var mf = r ? r.GetComponent<MeshFilter>() : null;
            if (!mf || !BraxKontrola.Dane(mf.sharedMesh, out var v, out var t) || t.Length > 300000) continue;
            var m = r.transform.localToWorldMatrix;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), cc = m.MultiplyPoint3x4(v[t[i + 2]]);
                var n = Vector3.Cross(b - a, cc - a); float p2 = n.magnitude; if (p2 < 1e-6f || n.y / p2 < .95f) continue;
                int k = Mathf.RoundToInt((a.y + b.y + cc.y) / 3f / .02f);
                poziomy[k] = (poziomy.TryGetValue(k, out var s) ? s : 0) + p2 * .5f;
            }
        }
        var lv = poziomy.Where(x => x.Value >= .03f).Select(x => x.Key * .02f).OrderBy(x => x).ToList();
        int ciag = 1;
        for (int i = 1; i < lv.Count; i++)
        {
            float d = lv[i] - lv[i - 1];
            if (d >= .1f && d <= .25f) { if (++ciag >= 3) return true; }
            else if (d > .03f) ciag = 1;
        }
        return false;
    }
}

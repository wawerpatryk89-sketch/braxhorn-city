using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

// ROBOT KOLIZJI (właściciel 1.11, 22:22: „puść tego robota… czy nie przenika postać przez coś… w 14 ma być wszystko
// sprawdzone, globalnie”). W edytorze, po zbudowaniu sceny, bez trybu gry:
//   • siatka punktów co 2.5 m (promienie w dół przez wszystkie piętra): punkt = powierzchnia o nachyleniu ≤ granicy
//     kontrolera gracza, nad którą mieści się kapsuła postaci (r 0.3 × k, wysokość = wzrost bohatera); bez blatów mebli;
//   • kapsuła idzie z każdego punktu do sąsiadów (+x, +z) i przez każde drzwi BraxCityDoor (skrzydła otwarte — ich kolizje
//     wyłączone na czas próby): ruch z poślizgiem po ścianach (Physics.CapsuleCast), wejście na stopnie do wysokości stopnia
//     kontrolera gracza, opadanie na podłogę (SphereCast w dół);
//   • UTKNIĘCIE: kapsułę zatrzymała kolizja, przy której nie ma żadnej widocznej siatki (niewidzialna ściana), kapsuła
//     zakleszczona w kolizji albo drzwi, przez które nie da się przejść;
//   • PRZENIKANIE: kapsuła (pas 0.4 m … wzrost − 0.1 m, promień 0.8 r) weszła ≥ 5 cm w widoczną siatkę (sonda) — mebel, ściana
//     albo rzecz bez kolizji, przez którą postać przechodzi;
//   • WYPADNIĘCIE: pod kapsułą nie ma już niczego (dziura w podłodze, koniec mapy bez barierki).
// Wynik: BRAXHORN_ROBOT punkty=N utkniecia=N przenikania=N wypadniecia=N + 20 najgorszych miejsc ze współrzędnymi (do zdjęć).
public static class BraxRobotKolizji
{
    sealed class Zd { public string rodzaj, co; public Vector3 gdzie; public float waga; }
    sealed class Pkt { public Vector3 p; }
    static readonly RaycastHit[] bufK = new RaycastHit[256];
    static readonly Collider[] bufC = new Collider[48];
    static readonly Regex Miekkie = new Regex(@"\b(zaslon\w*|curtain\w*|firan\w*|laser\w*|beam\w*|promien\w*|swiatl\w*|light\w*|woda|wody|water\w*|mgla|fog|cien|shadow\w*|dym|smoke|para|steam|ogien|fire|plomien\w*|flame\w*|hologram\w*|flag\w*|chorag\w*|baner\w*|banner\w*|lin[ay]|rope\w*|siatk\w*|net|nets|sonda)\b");
    static readonly Regex Granica = new Regex(@"\b(granic\w*|bound\w*|edge|limit\w*|niewidz\w*|invisible|kraniec\w*|barier\w* niewidz\w*|blokad\w*)\b");

    static float r, h, krokStopnia, cosSpadku, rp;
    static readonly Dictionary<Renderer, Zd> przenikania = new Dictionary<Renderer, Zd>();
    static readonly List<Zd> utkniecia = new List<Zd>(), wypadniecia = new List<Zd>(), spadki = new List<Zd>();
    static readonly HashSet<long> bylyU = new HashSet<long>(), bylyW = new HashSet<long>();
    static CapsuleCollider proba;
    static HashSet<Transform> skrzydla;
    static Dictionary<long, List<Bounds>> widoczne;
    static BraxKontrola.Spis spisR;

    public static void Wykonaj(Scene scena, BraxKontrola.Spis spis)
    {
        float k = BraxKontrola.K; spisR = spis;
        r = .3f * k; h = BraxKontrola.H; krokStopnia = .36f; float spadek = 48f;
        var gracz = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (gracz && gracz.body) { krokStopnia = gracz.body.stepOffset; spadek = gracz.body.slopeLimit; }
        cosSpadku = Mathf.Cos(spadek * Mathf.Deg2Rad); rp = r * .8f;
        przenikania.Clear(); utkniecia.Clear(); wypadniecia.Clear(); spadki.Clear(); bylyU.Clear(); bylyW.Clear();
        float t0 = BraxKontrola.Sekundy, limit = Mathf.Clamp(265f - t0, 25f, 150f);
        // drzwi otwarte na czas próby: kolizje skrzydeł wyłączone (w grze otwierają się przy podejściu)
        var drzwi = Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        skrzydla = new HashSet<Transform>(); var wylaczone = new List<Collider>();
        foreach (var d in drzwi) if (d.leaves != null) foreach (var l in d.leaves) if (l)
        {
            skrzydla.Add(l);
            foreach (var c in l.GetComponentsInChildren<Collider>(false)) if (c.enabled && c.gameObject.layer != BraxKontrola.WarstwaSondy) { c.enabled = false; wylaczone.Add(c); }
        }
        var tmp = new GameObject("BRAXHORN robot: kapsuła próbna") { hideFlags = HideFlags.DontSave, layer = 2 };
        proba = tmp.AddComponent<CapsuleCollider>(); proba.radius = rp; proba.height = Mathf.Max(rp * 2f + .01f, h - .5f); proba.direction = 1;
        int punktyN = 0, odcinki = 0, doszly = 0, krawedzie = 0, drzwiOk = 0, drzwiZle = 0, naszeBlokady = 0; bool przerwane = false;
        try
        {
            Physics.SyncTransforms();
            Widoczne(scena);
            // obszar: kolizje stałe (bez gigantów)
            bool any = false; Bounds obszar = default;
            foreach (var root in scena.GetRootGameObjects())
                foreach (var c in root.GetComponentsInChildren<Collider>(false))
                {
                    if (!c.enabled || c.isTrigger || ((1 << c.gameObject.layer) & BraxKontrola.MaskaStala) == 0) continue;
                    var b = c.bounds; if (b.size.x > 3000f || b.size.z > 3000f || b.size.y > 1000f) continue;
                    if (!any) { obszar = b; any = true; } else obszar.Encapsulate(b);
                }
            if (!any) { Debug.Log("BRAXHORN_ROBOT " + BraxKontrola.Obszar + ": brak kolizji w scenie"); return; }
            var mn = Vector3.Max(obszar.min, new Vector3(-900, -60, -900)); var mx = Vector3.Min(obszar.max, new Vector3(900, 300, 900));
            float s = Mathf.Max(2.5f, Mathf.Sqrt((mx.x - mn.x) * (mx.z - mn.z) / 45000f));
            int nx = Mathf.Max(1, Mathf.FloorToInt((mx.x - mn.x) / s)), nz = Mathf.Max(1, Mathf.FloorToInt((mx.z - mn.z) / s));
            var kol = new List<Pkt>[nx, nz]; var pusta = new bool[nx, nz];
            float gora = mx.y + 2f, dlug = mx.y - mn.y + 4f;
            for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++)
            {
                var o = new Vector3(mn.x + (ix + .5f) * s, gora, mn.z + (iz + .5f) * s);
                int n = Physics.RaycastNonAlloc(o, Vector3.down, bufK, dlug, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore);
                if (n == 0) { pusta[ix, iz] = true; continue; }
                Array.Sort(bufK, 0, n, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
                List<Pkt> l = null; float ostatnie = float.MaxValue;
                for (int i = 0; i < n && (l == null || l.Count < 4); i++)
                {
                    var hh = bufK[i]; if (hh.normal.y < cosSpadku) continue;
                    if (ostatnie - hh.point.y < h * .9f) continue;                          // poziom za blisko poprzedniego
                    if (spis.poKolizji.TryGetValue(hh.collider, out var jj) && jj.typ != null) continue;   // blat mebla
                    var p = hh.point;
                    if (Physics.CheckCapsule(p + Vector3.up * (r + .07f), p + Vector3.up * Mathf.Max(r + .08f, h - r), r * .95f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) continue;
                    (l ??= new List<Pkt>()).Add(new Pkt { p = p }); ostatnie = p.y; punktyN++;
                }
                kol[ix, iz] = l;
            }
            // odcinki do sąsiadów (+x, +z) i próby krawędzi (sąsiad bez niczego pod spodem)
            int licznik = 0;
            for (int ix = 0; ix < nx && !przerwane; ix++) for (int iz = 0; iz < nz && !przerwane; iz++)
            {
                var l = kol[ix, iz]; if (l == null) continue;
                foreach (var a in l)
                    foreach (var (dx, dz) in new[] { (1, 0), (0, 1), (-1, 0), (0, -1) })
                    {
                        if ((++licznik & 63) == 0 && BraxKontrola.Sekundy - t0 > limit) { przerwane = true; break; }
                        int jx = ix + dx, jz = iz + dz;
                        bool poza = jx < 0 || jz < 0 || jx >= nx || jz >= nz;
                        if (!poza && !pusta[jx, jz])
                        {
                            if (dx < 0 || dz < 0) continue;                                     // odcinek liczony raz
                            var lb = kol[jx, jz]; if (lb == null) continue;
                            Pkt cel = null; foreach (var b in lb) if (Mathf.Abs(b.p.y - a.p.y) <= krokStopnia + .1f) { cel = b; break; }
                            if (cel == null) continue;
                            odcinki++;
                            var wy = Idz(a.p, cel.p, out var bl, out var gd);
                            if (wy == Wynik.Doszedl) doszly++;
                            else if (wy == Wynik.Sciana && bl && bl.name == "BRAXHORN kolizja (kontrola)")
                            {
                                naszeBlokady++;
                                if (naszeBlokady <= 40) BraxKontrola.Zapisz("robot: odcinek zatrzymany przez kolizję dodaną przez kontrolę (" + BraxKontrola.Sciezka(bl.transform) + ") " + BraxKontrola.F(gd));
                            }
                        }
                        else
                        {
                            krawedzie++;
                            Idz(a.p, a.p + new Vector3(dx, 0, dz) * s * 1.2f, out _, out _);
                        }
                    }
            }
            // drzwi
            foreach (var d in drzwi)
            {
                if (BraxKontrola.Sekundy - t0 > limit + 15f) { przerwane = true; break; }
                var f = d.transform.forward; f.y = 0; if (f.sqrMagnitude < 1e-4f) continue; f.Normalize();
                var c = d.transform.position;
                Vector3? A = Stan(c - f * 2f) ?? Stan(c - f * 3f), B = Stan(c + f * 2f) ?? Stan(c + f * 3f);
                string nazwa = "drzwi „" + (string.IsNullOrEmpty(d.label) ? d.id : d.label) + "”";
                if (A == null || B == null) { drzwiZle++; Utk(c, nazwa + ": brak miejsca na postać po " + (A == null ? "jednej" : "drugiej") + " stronie", 2.5f, true); continue; }
                var w = Idz(A.Value, B.Value, out var blok, out var gdzie, true);
                if (w == Wynik.Doszedl) drzwiOk++;
                else { drzwiZle++; Utk(gdzie, nazwa + ": nie da się przejść (" + (blok ? blok.name : w.ToString()) + ")", 2.5f, true); }
            }
        }
        finally
        {
            foreach (var c in wylaczone) if (c) c.enabled = true;
            Object.DestroyImmediate(tmp);
            Physics.SyncTransforms();
        }
        // raport
        var wszystkie = new List<Zd>();
        wszystkie.AddRange(wypadniecia); wszystkie.AddRange(utkniecia); wszystkie.AddRange(przenikania.Values);
        var sb = new StringBuilder();
        sb.Append("BRAXHORN_ROBOT ").Append(BraxKontrola.Obszar).Append(": punkty=").Append(punktyN).Append(" utkniecia=").Append(utkniecia.Count)
          .Append(" przenikania=").Append(przenikania.Count).Append(" wypadniecia=").Append(wypadniecia.Count)
          .Append(" (odcinki ").Append(odcinki).Append(", doszło ").Append(doszly).Append(", próby krawędzi ").Append(krawedzie)
          .Append(", zatrzymane przez kolizje z kontroli ").Append(naszeBlokady).Append(", drzwi przejściowe ").Append(drzwiOk).Append("/").Append(drzwiOk + drzwiZle).Append(", spadki > 2 m ").Append(spadki.Count)
          .Append(", kapsuła r ").Append(r.ToString("F2")).Append(" h ").Append(h.ToString("F2")).Append(" stopień ").Append(krokStopnia.ToString("F2"))
          .Append(", sondy ").Append(BraxKontrola.Sondy.Liczba).Append(", czas ").Append((BraxKontrola.Sekundy - t0).ToString("F1")).Append(" s").Append(przerwane ? ", PRZERWANE limitem czasu" : "").Append(")");
        sb.Append("\nBRAXHORN_ROBOT najgorsze miejsca (do zdjęć: kamera 3 m od punktu na wysokości oczu, patrzy na punkt):");
        var komorki = new HashSet<long>(); int nr = 0;
        foreach (var z in wszystkie.OrderByDescending(z => z.waga))
        {
            if (!komorki.Add(BraxKontrola.Klucz(z.gdzie, 3f))) continue;
            string l = "  " + (++nr) + ". " + z.rodzaj + " " + BraxKontrola.F(z.gdzie) + " — " + z.co;
            sb.Append('\n').Append(l);
            if (nr >= 20) break;
        }
        foreach (var z in wszystkie.OrderByDescending(z => z.waga).Take(400)) BraxKontrola.Zapisz("robot: " + z.rodzaj + " " + BraxKontrola.F(z.gdzie) + " — " + z.co);
        Debug.Log(sb.ToString());
    }

    enum Wynik { Doszedl, Sciana, Utknal, Wypadl, Spadl, Limit }

    // punkt startowy przy drzwiach: podłoga pod spodem i miejsce na kapsułę
    static Vector3? Stan(Vector3 p)
    {
        if (!Physics.Raycast(p + Vector3.up * 1.6f, Vector3.down, out var g, 3.5f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) return null;
        var f = g.point;
        if (Physics.CheckCapsule(f + Vector3.up * (r + .07f), f + Vector3.up * Mathf.Max(r + .08f, h - r), r * .95f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) return null;
        return f;
    }

    // kapsuła od a do b; zwraca wynik, ostatnią kolizję blokującą i miejsce zdarzenia
    static Wynik Idz(Vector3 a, Vector3 b, out Collider blok, out Vector3 gdzie, bool drzwi = false)
    {
        blok = null; var F = a; gdzie = a;
        float dl = new Vector2(b.x - a.x, b.z - a.z).magnitude;
        int maxIt = Mathf.CeilToInt(dl / .25f) * 3 + 6, stoi = 0;
        RaycastHit ost = default; bool bylBlok = false;
        for (int it = 0; it < maxIt; it++)
        {
            var d = b - F; d.y = 0; float dist = d.magnitude;
            if (dist < .2f) return Wynik.Doszedl;
            var dir = d / dist; float krok = Mathf.Min(.25f, dist);
            var przed = F;
            if (Ruch(ref F, dir * krok, out var hit)) { ost = hit; bylBlok = true; }
            var ruszyl = new Vector2(F.x - przed.x, F.z - przed.z).magnitude;
            gdzie = F;
            // podłoga
            var g = Grunt(ref F, out float spad);
            if (g == Wynik.Wypadl) { Wyp(F, "brak podłogi pod postacią (dziura / koniec mapy bez barierki)"); return g; }
            if (g == Wynik.Spadl) { if (spad > 2f) spadki.Add(new Zd { rodzaj = "SPADEK", gdzie = F, co = spad.ToString("F1") + " m", waga = .5f }); return g; }
            // przenikanie przez widoczne siatki
            Przenik(F);
            // zakleszczenie w kolizji
            if (Physics.CheckCapsule(F + Vector3.up * (r + krokStopnia + .02f), F + Vector3.up * Mathf.Max(r + krokStopnia + .03f, h - r), r * .85f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore))
            {
                int n = Physics.OverlapCapsuleNonAlloc(F + Vector3.up * (r + krokStopnia + .02f), F + Vector3.up * Mathf.Max(r + krokStopnia + .03f, h - r), r * .85f, bufC, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore);
                string kto = n > 0 && bufC[0] ? bufC[0].name : "?";
                Utk(F, "kapsuła zakleszczona w kolizji „" + kto + "”", 2f, false); blok = n > 0 ? bufC[0] : null; return Wynik.Utknal;
            }
            if (ruszyl < .01f) { if (++stoi >= 3) break; } else stoi = 0;
        }
        if (bylBlok)
        {
            blok = ost.collider;
            if (!drzwi && !Widac(ost.point) && !(ost.collider && Granica.IsMatch(BraxKontrola.Norm(ost.collider.name))))
            {
                Utk(ost.point, "niewidzialna przeszkoda: kolizja „" + (ost.collider ? BraxKontrola.Sciezka(ost.collider.transform) : "?") + "” bez widocznej siatki", 2f, false);
                return Wynik.Utknal;
            }
            return Wynik.Sciana;
        }
        return Wynik.Limit;
    }

    // ruch z poślizgiem po przeszkodach; kapsuła podniesiona o wysokość stopnia (próg, krawężnik, schody)
    static bool Ruch(ref Vector3 F, Vector3 delta, out RaycastHit blok)
    {
        blok = default; bool zablok = false; const float skin = .02f;
        var rem = delta;
        for (int k = 0; k < 3; k++)
        {
            float len = rem.magnitude; if (len < 1e-4f) break;
            var dir = rem / len;
            var p1 = F + Vector3.up * (r + krokStopnia); var p2 = F + Vector3.up * Mathf.Max(r + krokStopnia + .01f, h - r);
            if (Physics.CapsuleCast(p1, p2, r - skin * .5f, dir, out var hit, len + skin, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore))
            {
                float adv = Mathf.Max(0, hit.distance - skin);
                F += dir * adv; rem -= dir * adv;
                blok = hit; zablok = true;
                var n = hit.normal; n.y = 0; if (n.sqrMagnitude < 1e-4f) break; n.Normalize();
                rem -= n * Vector3.Dot(rem, n);
                if (Vector3.Dot(rem, delta) <= 0) break;
            }
            else { F += rem; break; }
        }
        return zablok;
    }

    // opadanie na podłogę: kula w dół do wysokości stopnia + 0.6 m; brak → spadek albo pustka
    static Wynik Grunt(ref Vector3 F, out float spad)
    {
        spad = 0; float rr = r * .6f;
        var o = F + Vector3.up * (rr + krokStopnia);
        if (Physics.SphereCast(o, rr, Vector3.down, out var g, krokStopnia + .6f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore))
        { F.y = o.y - g.distance - rr; return Wynik.Doszedl; }
        if (Physics.Raycast(F + Vector3.up * .1f, Vector3.down, out var low, 400f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore))
        { spad = F.y - low.point.y; F = low.point; return Wynik.Spadl; }
        return Wynik.Wypadl;
    }

    static void Przenik(Vector3 F)
    {
        var p1 = F + Vector3.up * (.4f + rp); var p2 = F + Vector3.up * Mathf.Max(.41f + rp, h - .1f - rp);
        int n = Physics.OverlapCapsuleNonAlloc(p1, p2, rp, bufC, BraxKontrola.MaskaSond, QueryTriggerInteraction.Ignore);
        if (n == 0) return;
        var srodek = (p1 + p2) * .5f;
        for (int i = 0; i < n; i++)
        {
            var c = bufC[i]; var rend = BraxKontrola.Sondy.Czyj(c); if (!rend) continue;
            if (Skrzydlo(rend.transform) || Miekkie.IsMatch(BraxKontrola.Norm(rend.name) + BraxKontrola.Norm(rend.sharedMaterial ? rend.sharedMaterial.name : ""))) continue;
            if (!Physics.ComputePenetration(proba, srodek, Quaternion.identity, c, c.transform.position, c.transform.rotation, out _, out float glebia)) continue;
            if (glebia < .05f) continue;
            if (przenikania.TryGetValue(rend, out var z)) { if (glebia + 1f > z.waga) { z.waga = 1f + glebia; z.gdzie = F; z.co = Opis(rend, glebia); } }
            else przenikania[rend] = new Zd { rodzaj = "PRZENIKANIE", gdzie = F, waga = 1f + glebia, co = Opis(rend, glebia) };
        }
    }
    static string Opis(Renderer r, float g)
    {
        string kto = BraxKontrola.Sciezka(r.transform);
        if (spisR != null) foreach (var j in spisR.jednostki) if (j.rs.Contains(r)) { kto = (j.typ != null ? j.typ.nazwa + " " : "") + j.opis; break; }
        return "postać weszła " + (g * 100f).ToString("F0") + " cm w „" + kto + "” (widoczna siatka bez kolizji w tym miejscu)";
    }
    static bool Skrzydlo(Transform t) { for (var p = t; p; p = p.parent) if (skrzydla.Contains(p)) return true; return false; }

    static void Utk(Vector3 p, string co, float waga, bool zawsze)
    {
        if (!zawsze && !bylyU.Add(BraxKontrola.Klucz(p, 1f))) return;
        utkniecia.Add(new Zd { rodzaj = "UTKNIĘCIE", gdzie = p, co = co, waga = waga });
    }
    static void Wyp(Vector3 p, string co)
    {
        if (!bylyW.Add(BraxKontrola.Klucz(p, 2f))) return;
        wypadniecia.Add(new Zd { rodzaj = "WYPADNIĘCIE", gdzie = p, co = co, waga = 3f });
    }

    // czy przy punkcie jest coś widocznego: sonda (siatka) w promieniu 0.35 m albo obrys dowolnego renderera (także roślin)
    static bool Widac(Vector3 p)
    {
        if (Physics.CheckSphere(p, .35f, BraxKontrola.MaskaSond, QueryTriggerInteraction.Ignore)) return true;
        if (widoczne != null && widoczne.TryGetValue(BraxKontrola.Klucz(p, 4f), out var l)) foreach (var b in l) { var e = b; e.Expand(.6f); if (e.Contains(p)) return true; }
        return false;
    }
    static void Widoczne(Scene scena)
    {
        widoczne = new Dictionary<long, List<Bounds>>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var rr in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!rr.enabled || rr.gameObject.layer == 8) continue;
                var mf = rr.GetComponent<MeshFilter>(); if (mf && BraxKontrola.Scalona(mf.sharedMesh)) continue;   // kępy trawy, scalone siatki (te mają sondy)
                var b = rr.bounds; if (b.size.x > 30f || b.size.z > 30f) continue;
                for (int x = Mathf.FloorToInt((b.min.x - .6f) / 4f); x <= Mathf.FloorToInt((b.max.x + .6f) / 4f); x++)
                    for (int z = Mathf.FloorToInt((b.min.z - .6f) / 4f); z <= Mathf.FloorToInt((b.max.z + .6f) / 4f); z++)
                    {
                        long k = ((long)x << 32) ^ (uint)z;
                        if (!widoczne.TryGetValue(k, out var l)) widoczne[k] = l = new List<Bounds>();
                        l.Add(b);
                    }
            }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ŁUKI NAROŻNIKÓW SKRZYŻOWAŃ — Miasto R1 (właściciel 10.11, 15:39–15:45: „każda droga lekko zaokrąglona, żeby nie były
// trójkątne… w 90 stopniach… narożniki zaokrąglone, łatwiej będą samochody wjeżdżać”; „globalnie, na całą grę”;
// „nie rób takich ostrych zakrętów, tylko zrób zaokrąglone zakręty — łuki zaokrąglone”).
// Każdy narożnik kwartału przy skrzyżowaniu: krawężnik po łuku (promień RLuku = 3,5 m × K), asfalt narożnika dopełniony do łuku,
// chodnik i trawnik obchodzą łuk. Jak to działa:
//  • plan łuków (LukiPlan) powstaje z tych samych stałych układu, PRZED nawierzchniami;
//  • każda płyta (Plyta: chodnik, krawężnik, podłoże) i trawnik (Trawa) w kwadracie narożnika jest przycinana: część poza
//    kwadratem bez zmian, część w kwadracie tylko wewnątrz koła (R − krawężnik) — wielokąt z kolizją (ten sam materiał);
//  • na końcu etapu nawierzchni (LukiBuduj): krawężnik łukiem (kamień po łuku), asfalt narożnika (kwadrat bez koła), na wylocie
//    ul. Wiaduktowej z ulicy głównej A — wycięcie chodnika A w kwadracie i chodnik łukiem;
//  • zebry i linie zatrzymania odsunięte tak, by zebra zaczynała się, gdzie łuk prawie się kończy (OdsuniecieZebry), ruch aut:
//    NaJezdni zna asfalt łuków, linia zatrzymania tras przesunięta o tyle samo (BraxTrasyAut.OdsuniecieZebr);
//  • wyposażenie stawiane przez NaPodlozu, które wypadłoby na asfalt łuku, przesuwa się na chodnik (PozaLukiem).
public static partial class BraxRegionMiastoB
{
    public const float RLuku = 4.5f;   // 3,5 m × K
    const int LukSeg = 16;

    public sealed class LukNaroznika
    {
        public string n; public Vector2 P; public int sx, sz; public float R; public bool ulicaA;
        public Vector2 C => new Vector2(P.x + sx * R, P.y + sz * R);
        public Rect Kw { get { var c = C; return Rect.MinMaxRect(Mathf.Min(P.x, c.x), Mathf.Min(P.y, c.y), Mathf.Max(P.x, c.x), Mathf.Max(P.y, c.y)); } }
        // punkt łuku o promieniu r: th = 0 → na linii x = P.x (krawędź jezdni wzdłuż z), th = 90° → na linii z = P.y
        public Vector2 Q(float r, float th) { var c = C; return new Vector2(c.x - sx * r * Mathf.Cos(th), c.y - sz * r * Mathf.Sin(th)); }
        public bool WAsfalcie(Vector2 q, float zapas = 0f)
        {
            var k = Kw; k.xMin -= zapas; k.yMin -= zapas; k.xMax += zapas; k.yMax += zapas;
            return k.Contains(q) && (q - C).magnitude > R - zapas;
        }
    }
    public static readonly List<LukNaroznika> Luki = new List<LukNaroznika>();
    // łuki ZEWNĘTRZNE zakrętów (np. L: Warsztatowa → Wiaduktowa): P = róg jezdni, (sx, sz) w stronę jezdni, C = środek; wewnątrz
    // koła R jezdnia (wycinek asfaltu), krawężnik R … R + Kr, dalej do rogu chodnik. Płyty wokół układa wywołujący (bez przycinania).
    public static readonly List<LukNaroznika> LukiZew = new List<LukNaroznika>();
    static void LukZew(string n, float px, float pz, int sx, int sz, float r) => LukiZew.Add(new LukNaroznika { n = n, P = new Vector2(px, pz), sx = sx, sz = sz, R = r });

    // ---------------- wjazdy na rondo / pętlę: styk prostej krawędzi wlotu z pierścieniem łukiem ----------------
    public const float RLukuWlotu = 6f;   // ≈ 4,6 m realnie
    static int lukiWlotow;
    sealed class LukWl { public Vector2 Cf; public float rf, a; }
    // okrąg styczny do krawędzi jezdni wlotu (odsunięcie h od osi d, strona nn) i zewnętrznie do pierścienia (środek c, promień R);
    // a = odległość punktu styku z prostą od środka wzdłuż osi; mniejszy promień, gdy łuk nie mieści się w placu (S)
    static LukWl LukWlotu(Vector2 c, Vector2 d, Vector2 nn, float h, float R, float S)
    {
        for (float rf = RLukuWlotu; rf >= 1.5f; rf -= .5f)
        {
            float a2 = (R + rf) * (R + rf) - (h + rf) * (h + rf);
            if (a2 <= 0f) continue;
            float a = Mathf.Sqrt(a2);
            if (a > S - .6f) continue;
            return new LukWl { Cf = c + d * a + nn * (h + rf), rf = rf, a = a };
        }
        return null;
    }
    // punkty wewnętrzne łuku okręgu (Cf, r) od kierunku u0 do u1 krótszą drogą (bez końców)
    static List<Vector2> LukPunkty(Vector2 Cf, float r, Vector2 u0, Vector2 u1, int n)
    {
        float a0 = Mathf.Atan2(u0.y, u0.x), a1 = Mathf.Atan2(u1.y, u1.x), da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        var l = new List<Vector2>();
        for (int i = 1; i < n; i++) { float t = a0 + da * i / n; l.Add(Cf + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r); }
        return l;
    }
    // triangulacja wielokąta prostego „ucho po uchu” (trójkąty jako trójki punktów); null = nie udało się
    static List<Vector2> TriangulacjaUszu(List<Vector2> wej)
    {
        var p = new List<Vector2>();
        foreach (var q in wej) if (p.Count == 0 || (q - p[p.Count - 1]).sqrMagnitude > 1e-8f) p.Add(q);
        while (p.Count > 2 && (p[0] - p[p.Count - 1]).sqrMagnitude <= 1e-8f) p.RemoveAt(p.Count - 1);
        if (p.Count < 3) return null;
        if (Pole(p) < 0f) p.Reverse();
        var wy = new List<Vector2>(); var idx = Enumerable.Range(0, p.Count).ToList();
        float Kr2(Vector2 a, Vector2 b, Vector2 q) => (b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x);
        int bez = 0;
        while (idx.Count > 3 && bez < idx.Count * 2)
        {
            bool ucho = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                Vector2 a = p[ia], b = p[ib], cc = p[ic];
                float kr = Kr2(a, b, cc);
                if (kr <= 1e-7f) { if (Mathf.Abs(kr) <= 1e-7f) { idx.RemoveAt(i); ucho = true; break; } continue; }   // wklęsły (lub współliniowy — usuń)
                bool wsrodku = false;
                foreach (int j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    var q = p[j];
                    if (Kr2(a, b, q) >= 0f && Kr2(b, cc, q) >= 0f && Kr2(cc, a, q) >= 0f) { wsrodku = true; break; }
                }
                if (wsrodku) continue;
                wy.Add(a); wy.Add(b); wy.Add(cc); idx.RemoveAt(i); ucho = true; break;
            }
            if (!ucho) bez++;
        }
        if (idx.Count == 3) { wy.Add(p[idx[0]]); wy.Add(p[idx[1]]); wy.Add(p[idx[2]]); }
        return idx.Count <= 3 ? wy : null;
    }
    static int lukiKawalkow, lukiPrzesuniete;
    static bool lukiZbudowane;

    static void Luk(string n, float px, float pz, int sx, int sz, float r = RLuku, bool ulicaA = false)
    {
        if (r < .6f) { Debug.LogWarning("BRAXHORN_MIASTO_B_LUKI pominięty (za mało miejsca): " + n); return; }
        Luki.Add(new LukNaroznika { n = n, P = new Vector2(px, pz), sx = sx, sz = sz, R = r, ulicaA = ulicaA });
    }

    // Plan: wszystkie narożniki kwartałów przy skrzyżowaniach Miasta R1 (stałe układu — przed budową nawierzchni).
    static void LukiPlan()
    {
        Luki.Clear(); LukiZew.Clear(); lukiKawalkow = lukiPrzesuniete = lukiWlotow = 0; lukiZbudowane = false;
        // J1: ulica główna × ul. Wiaduktowa (wylot od płd.); chodnik ulicy głównej (A) wycinany w LukiBuduj
        Luk("Główna × Wiaduktowa (płd.-zach.)", WX - WH, 24f, -1, -1, RLuku, true);
        Luk("Główna × Wiaduktowa (płd.-wsch.)", WX + WH, 24f, 1, -1, RLuku, true);
        // zakręt ul. Warsztatowa → ul. Wiaduktowa (L): łuk wewnętrzny tu, łuk zewnętrzny w UliceBoczne (LukZew)
        Luk("Wiaduktowa × Warsztatowa (pn.-zach.)", WX - WH, SZ + SH, -1, 1);
        // wjazd z Alei Północnej na parking supermarketu (jezdnia alei wchodzi w kwadrat parkingu)
        Luk("Aleja Płn. → parking supermarketu (wsch.)", AX + AH, PetlaZ - PetlaPlac, 1, -1);
        Luk("Aleja Płn. → parking supermarketu (zach.)", AX - AH, PetlaZ - PetlaPlac, -1, -1);
        // Aleja Południowa × ul. Warsztatowa (T od wsch.)
        Luk("Aleja Płd. × Warsztatowa (pn.-wsch.)", AX + AH, SZ + SH, 1, 1);
        Luk("Aleja Płd. × Warsztatowa (pd.-wsch.)", AX + AH, SZ - SH, 1, -1);
        // nowe dzielnice: narożnik tylko tam, gdzie są oba ramiona (bez ramienia krawężnik idzie prosto)
        foreach (var (c, c2, ram, prZ, nazwa) in NdSkrzyzowania())
            foreach (int s in new[] { 1, -1 })
                foreach (int t in new[] { 1, -1 })
                {
                    bool ax = s > 0 ? ram[1] : ram[3], az = t > 0 ? ram[0] : ram[2];
                    if (ax && az) Luk(nazwa + (t > 0 ? " (pn." : " (pd.") + (s > 0 ? "-wsch.)" : "-zach.)"), c + s * AH, c2 + t * AH, s, t);
                }
    }

    // ruch aut liczony bez budowy dróg w tym przebiegu (np. sama scena ruchu) — plan łuków z tych samych stałych
    public static void LukiPlanJesliPusty() { if (Luki.Count == 0) { LukiPlan(); lukiZbudowane = true; } }

    // promień łuków skrzyżowania o środku c (0 = bez łuków): zebry i linie zatrzymania stoją za łukiem
    public static float PromienLukuSkrzyzowania(Vector2 c)
    {
        float r = 0f;
        foreach (var L in Luki) if (Mathf.Abs(L.P.x - c.x) <= 6.5f && Mathf.Abs(L.P.y - c.y) <= 6.5f) r = Mathf.Max(r, L.R);
        return r;
    }
    static float PromienLukuSkrzyzowania(Vector3 c) => PromienLukuSkrzyzowania(new Vector2(c.x, c.z));
    // Odsunięcie zebr skrzyżowania od narożnika: początek zebry tam, gdzie łuk krawężnika cofa się już tylko o ~0,25 m
    // (R − 1,5 od rogu jezdni; dawniej 0,85 m od rogu). Mniej niż pełny promień — kolejka aut między J2 a rondem
    // (47,5 m) zostaje prawie taka jak była (test ruchu: pełne odsunięcie dokładało zakleszczeń przy rondzie).
    public const float ZebraPrzedLukiem = 2.35f;
    public const float ZebraParkingu = 5.5f;   // środek zebry wejścia na parking od krawędzi parkingu (dawniej 3 — teraz za łukami wjazdu)
    public static float OdsuniecieZebry(Vector2 c) => Mathf.Max(0f, PromienLukuSkrzyzowania(c) - ZebraPrzedLukiem);
    static float OdsuniecieZebry(Vector3 c) => OdsuniecieZebry(new Vector2(c.x, c.z));

    public static bool WLukuAsfalt(Vector2 q, float zapas = 0f)
    {
        foreach (var L in Luki) if (L.WAsfalcie(q, zapas)) return true;
        foreach (var L in LukiZew) { var k = L.Kw; k.xMin -= zapas; k.yMin -= zapas; k.xMax += zapas; k.yMax += zapas; if (k.Contains(q) && (q - L.C).magnitude <= L.R + zapas) return true; }
        return false;
    }

    // wyposażenie, które wypadłoby na asfalt łuku (albo na sam krawężnik łuku), przesuwa się promieniście na chodnik
    static Vector3 PozaLukiem(Vector3 p)
    {
        var q = new Vector2(p.x, p.z);
        foreach (var L in Luki)
        {
            var k = L.Kw; if (!k.Contains(q)) continue;
            var d = q - L.C; float m = d.magnitude, gr = L.R - Kr - .35f;
            if (m <= gr || m < 1e-4f) continue;
            var n = L.C + d / m * Mathf.Max(.2f, L.R - Kr - .5f);
            p.x = n.x; p.z = n.y; lukiPrzesuniete++;
        }
        return p;
    }

    // prostokąt bez kwadratów narożników (do 4 prostokątów na każdy przecięty narożnik)
    static List<Rect> BezNaroznikow(Rect r, out List<(LukNaroznika L, Rect w)> wNaroznikach)
    {
        var l = new List<Rect> { r }; wNaroznikach = null;
        foreach (var L in Luki)
        {
            var w = Przetnij(r, L.Kw);
            if (!Niepuste(w)) continue;
            (wNaroznikach ??= new List<(LukNaroznika, Rect)>()).Add((L, w));
            l = l.SelectMany(x => Odejmij(x, L.Kw)).Where(Niepuste).ToList();
        }
        return l;
    }

    // prostokąt (już w kwadracie narożnika) ∩ koło (C, r): wycinek koła (C + łuk) jest wypukły — Sutherland–Hodgman
    static List<Vector2> WKole(Rect a, LukNaroznika L, float r)
    {
        var poly = new List<Vector2> { new Vector2(a.xMin, a.yMin), new Vector2(a.xMax, a.yMin), new Vector2(a.xMax, a.yMax), new Vector2(a.xMin, a.yMax) };
        var klin = new List<Vector2> { L.C };
        for (int i = 0; i <= LukSeg; i++) klin.Add(L.Q(r, i * Mathf.PI * .5f / LukSeg));
        if (Pole(klin) < 0f) klin.Reverse();
        for (int e = 0; e < klin.Count && poly.Count > 0; e++)
        {
            Vector2 A = klin[e], B = klin[(e + 1) % klin.Count];
            float Strona(Vector2 p) => (B.x - A.x) * (p.y - A.y) - (B.y - A.y) * (p.x - A.x);   // > 0: po lewej (wnętrze CCW)
            var wy = new List<Vector2>();
            for (int i = 0; i < poly.Count; i++)
            {
                var P0 = poly[i]; var P1 = poly[(i + 1) % poly.Count];
                float s0 = Strona(P0), s1 = Strona(P1);
                if (s0 >= 0f) wy.Add(P0);
                if ((s0 >= 0f) != (s1 >= 0f)) wy.Add(P0 + (P1 - P0) * (s0 / (s0 - s1)));
            }
            poly = wy;
        }
        // usuń prawie powtórzone wierzchołki
        var cz = new List<Vector2>();
        foreach (var p in poly) if (cz.Count == 0 || (p - cz[cz.Count - 1]).sqrMagnitude > 1e-6f) cz.Add(p);
        if (cz.Count > 2 && (cz[0] - cz[cz.Count - 1]).sqrMagnitude <= 1e-6f) cz.RemoveAt(cz.Count - 1);
        return cz.Count >= 3 && Mathf.Abs(Pole(cz)) > 1e-4f ? cz : null;
    }
    static float Pole(List<Vector2> p) { float s = 0f; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += a.x * b.y - b.x * a.y; } return s * .5f; }
    static bool CalyWKole(Rect a, LukNaroznika L, float r)
    {
        foreach (var p in new[] { new Vector2(a.xMin, a.yMin), new Vector2(a.xMax, a.yMin), new Vector2(a.xMax, a.yMax), new Vector2(a.xMin, a.yMax) })
            if ((p - L.C).magnitude > r) return false;
        return true;
    }

    // graniastosłup na wielokącie wypukłym: góra na „top”, boki do top − grub; kolizja z siatki (nawierzchnia dla Podloze)
    static void PlytaWielokat(string n, Material m, List<Vector2> poly, float top, float grub, bool kolizja)
    {
        if (poly == null || poly.Count < 3 || !m) return;
        var b = new BraxRegionMeble.Bud();
        Vector3 V(Vector2 v, float y) => new Vector3(v.x, y, v.y);
        for (int i = 1; i + 1 < poly.Count; i++)
        {
            Vector3 a = V(poly[0], top), bb = V(poly[i], top), c = V(poly[i + 1], top);
            if (Vector3.Cross(bb - a, c - a).y < 0f) b.Tri(a, c, bb); else b.Tri(a, bb, c);
        }
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 u = V(poly[i], top), w = V(poly[(i + 1) % poly.Count], top), u0 = V(poly[i], top - grub), w0 = V(poly[(i + 1) % poly.Count], top - grub);
            b.Quad(u, w, w0, u0); b.Quad(w, u, u0, w0);
        }
        var g = b.Obiekt(n, root, m, kolizja, false);
        if (kolizja) Nawierzchnia(g);
        lukiKawalkow++;
    }

    // część płyty w kwadracie narożnika: tylko wewnątrz koła (R − krawężnik) — tam, gdzie zostaje chodnik
    static void PlytaWLukach(string n, Material m, List<(LukNaroznika L, Rect w)> wN, float top, float grub, bool kolizja)
    {
        if (wN == null) return;
        foreach (var (L, w) in wN) PlytaWielokat(n + " (łuk narożnika)", m, WKole(w, L, L.R - Kr), top, grub, kolizja);
    }

    // krawężniki łukiem, asfalt narożników, chodnik ulicy głównej (A) na wylocie Wiaduktowej — koniec etapu nawierzchni
    static void LukiBuduj()
    {
        if (lukiZbudowane) return; lukiZbudowane = true;
        var kraw = new BraxRegionMeble.Bud(); var asf = new BraxRegionMeble.Bud();
        Vector3 V(Vector2 v, float y) => new Vector3(v.x, y, v.y);
        int krawLukow = 0, wycietychA = 0;
        Transform ulicaA = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t && t.name == "Street pavement and dry asphalt") { ulicaA = t; break; }
        // chodnik A (kostka granitowa BraxCityGround) — łuk na wylocie Wiaduktowej w tym samym materiale, bez jasnej łaty z płyt B
        Material kostkaA = null;
        if (ulicaA) foreach (var r in ulicaA.GetComponentsInChildren<MeshRenderer>(true)) if (r && r.name.StartsWith("Near pavement hand-laid") && r.sharedMaterial) { kostkaA = r.sharedMaterial; break; }
        foreach (var L in Luki)
        {
            // a) wylot Wiaduktowej z ulicy głównej: chodnik i krawężnik A w kwadracie narożnika wycięte, w ich miejsce płyty B łukiem
            if (L.ulicaA)
            {
                var k = L.Kw; var cut = Bx(k.xMin, k.xMax, -.6f, .6f, Mathf.Max(k.yMin, 20.5f), 23.999f);
                if (ulicaA) try { wycietychA += BraxCityMeshCut.Subtract(ulicaA, cut, BraxRegion.Out + "/Cuts", "luk_wylotu_wiaduktowej"); } catch (Exception e) { Debug.LogWarning("BRAXHORN_MIASTO_B_LUKI cięcie chodnika A: " + e.Message); }
                if (BraxClaudeGround.SourcePhysics) try { BraxCityMeshCut.SubtractBoxColliders(BraxClaudeGround.SourcePhysics, cut); } catch (Exception e) { Debug.LogWarning("BRAXHORN_MIASTO_B_LUKI kolizje A: " + e.Message); }
                var pas = Przetnij(k, Rect.MinMaxRect(k.xMin, 20.6f, k.xMax, 23.995f));
                if (Niepuste(pas)) PlytaWielokat("Chodnik: ulica główna przy Wiaduktowej (łuk narożnika)", kostkaA ? kostkaA : plyty, WKole(pas, L, L.R - Kr), YC, .3f - YA, true);
            }
            // b) krawężnik po łuku: pierścień R − Kr … R, kamienie ~1 m (fuga = osobne ścianki co segment)
            int n = Mathf.Max(6, Mathf.CeilToInt(L.R * Mathf.PI * .5f / .5f));
            for (int i = 0; i < n; i++)
            {
                float t0 = i * Mathf.PI * .5f / n, t1 = (i + 1) * Mathf.PI * .5f / n;
                Vector2 a0 = L.Q(L.R - Kr, t0), a1 = L.Q(L.R - Kr, t1), b0 = L.Q(L.R, t0), b1 = L.Q(L.R, t1);
                Vector3 A0 = V(a0, YC), A1 = V(a1, YC), B0 = V(b0, YC), B1 = V(b1, YC);
                if (Vector3.Cross(A1 - A0, B1 - A0).y < 0f) kraw.Quad(A0, B0, B1, A1); else kraw.Quad(A0, A1, B1, B0);
                // lico od jezdni (promień R) do spodu krawężnika
                Vector3 B0d = V(b0, YC - (.3f - YA)), B1d = V(b1, YC - (.3f - YA));
                var nOut = new Vector3(b0.x + b1.x - 2f * L.C.x, 0f, b0.y + b1.y - 2f * L.C.y);
                if (Vector3.Dot(Vector3.Cross(B1 - B0, B1d - B0), nOut) > 0f) kraw.Quad(B0, B1, B1d, B0d); else kraw.Quad(B1, B0, B0d, B1d);
            }
            krawLukow++;
            // c) asfalt narożnika: wachlarz od rogu jezdni P do łuku R (kwadrat bez koła), na wysokości asfaltu
            for (int i = 0; i < LukSeg; i++)
            {
                Vector3 p = V(L.P, YA), q0 = V(L.Q(L.R, i * Mathf.PI * .5f / LukSeg), YA), q1 = V(L.Q(L.R, (i + 1) * Mathf.PI * .5f / LukSeg), YA);
                if (Vector3.Cross(q0 - p, q1 - p).y < 0f) asf.Tri(p, q1, q0); else asf.Tri(p, q0, q1);
            }
            // kontrola: asfalt łuku nie nachodzi na zarejestrowany asfalt (ta sama wysokość = migotanie)
            var probka = Vector2.Lerp(L.P, L.Q(L.R, Mathf.PI * .25f), .5f);
            foreach (var (an, ar) in asfalty) if (ar.Contains(probka)) { MigotaniePary++; Debug.LogWarning("BRAXHORN_MIASTO_B_MIGOTANIE asfalt łuku " + L.n + " × " + an); }
        }
        // łuki zewnętrzne zakrętów: wycinek asfaltu (wachlarz ze środka), krawężnik R … R + Kr, chodnik od łuku do rogu (wachlarz z rogu)
        var chodZew = new BraxRegionMeble.Bud();
        foreach (var L in LukiZew)
        {
            int n = Mathf.Max(8, Mathf.CeilToInt(L.R * Mathf.PI * .5f / .5f));
            for (int i = 0; i < n; i++)
            {
                float t0 = i * Mathf.PI * .5f / n, t1 = (i + 1) * Mathf.PI * .5f / n;
                Vector3 c3 = V(L.C, YA), q0 = V(L.Q(L.R, t0), YA), q1 = V(L.Q(L.R, t1), YA);
                if (Vector3.Cross(q0 - c3, q1 - c3).y < 0f) asf.Tri(c3, q1, q0); else asf.Tri(c3, q0, q1);
                Vector3 A0 = V(L.Q(L.R, t0), YC), A1 = V(L.Q(L.R, t1), YC), B0 = V(L.Q(L.R + Kr, t0), YC), B1 = V(L.Q(L.R + Kr, t1), YC);
                if (Vector3.Cross(A1 - A0, B1 - A0).y < 0f) kraw.Quad(A0, B0, B1, A1); else kraw.Quad(A0, A1, B1, B0);
                Vector3 A0d = V(L.Q(L.R, t0), YC - (.3f - YA)), A1d = V(L.Q(L.R, t1), YC - (.3f - YA));
                var nIn = new Vector3(L.C.x - (A0.x + A1.x) * .5f, 0f, L.C.y - (A0.z + A1.z) * .5f);   // lico krawężnika do jezdni (do środka)
                if (Vector3.Dot(Vector3.Cross(A1 - A0, A1d - A0), nIn) > 0f) kraw.Quad(A0, A1, A1d, A0d); else kraw.Quad(A1, A0, A0d, A1d);
                Vector3 p = V(L.P, YC), s0 = V(L.Q(L.R + Kr, t0), YC), s1 = V(L.Q(L.R + Kr, t1), YC);
                if (Vector3.Cross(s0 - p, s1 - p).y < 0f) chodZew.Tri(p, s1, s0); else chodZew.Tri(p, s0, s1);
            }
            krawLukow++;
        }
        var gcz = chodZew.Obiekt("Chodnik: łuki zewnętrzne zakrętów", root, plyty, true, false); Nawierzchnia(gcz);
        var gk = kraw.Obiekt("Krawężniki łukiem (narożniki skrzyżowań)", root, krawMat, true, false); Nawierzchnia(gk);
        var ga = asf.Obiekt("Jezdnie: asfalt narożników (łuki)", root, asfalt, true, false); Nawierzchnia(ga);
        Debug.Log("BRAXHORN_MIASTO_B_LUKI wjazdów na rondo/pętlę łukiem=" + lukiWlotow + " (R " + RLukuWlotu + "); narożników=" + Luki.Count + " krawężników łukiem=" + krawLukow + " promień " + RLuku + " (3,5 m × K)" +
                  " kawałków chodnika/trawnika przyciętych do łuku=" + lukiKawalkow + " wycięte siatki chodnika A=" + wycietychA +
                  " | " + string.Join("; ", Luki.Select(L => L.n + " R " + L.R.ToString("0.0"))) + " | zewnętrzne: " + string.Join("; ", LukiZew.Select(L => L.n + " R " + L.R.ToString("0.0"))));
    }
}

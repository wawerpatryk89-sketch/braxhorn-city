using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// MIGOTANIE (właściciel 1.11, 21:13: „migotanie jeszcze!!!”). Przyczyna nr 1 — Z-FIGHTING: dwie powierzchnie równoległe
// w odstępie < 2 cm z nakładającymi się granicami (płyty skrzyżowań nad asfaltem, pasy, chodniki, dywan nad posadzką, obrus,
// szyld na ścianie) — na telefonie (głębia 24 bit) w oddali raz wygrywa jedna, raz druga.
// Szukanie: każdy trójkąt widocznej siatki (bez roślin) zwrócony wzdłuż osi świata (±X, ±Y, ±Z) → prostokąt na płaszczyźnie
// o odległości d; indeks: oś, zwrot, d co 2 cm, komórki 4 × 4 m; pary prostokątów z różnych powierzchni (renderer + materiał),
// ten sam zwrot, |Δd| < 2 cm, wspólne pole ≥ 0.02 m².
// Naprawa: przesunięcie MNIEJSZEJ powierzchni wzdłuż normalnej tak, by odstęp był 2 cm (przed większą, gdy była z przodu
// albo w tej samej płaszczyźnie; za nią, gdy była schowana) — tylko gdy cała jej siatka jest płaska wzdłuż tej osi (< 12 cm:
// pasy, dywany, płyty, szyldy, podłogi z jednej warstwy), inaczej próbujemy większej, a gdy żadnej się nie da — zgłoszenie.
// Kamera gracza: bliska płaszczyzna ≥ 0.15 m (także „blisko” w BraxClaudeKamera przy ścianie, wcześniej 0.1 m).
// BUILD 45 (właściciel: „Starą zwalić, nie nakładać. NIGDY nic nie nakładamy”; „Arena migocze!!!!!”, „Kasyno mruga podłoga”):
// na OSIEDLU naprawa już nie przesuwa powierzchni o 2 cm — WYCINA dolną (schowaną) warstwę dokładnie pod górną
// (BraxWycinanieSiatki.OdejmijTrojkaty: odejmowanie trójkątów górnej warstwy w płaszczyźnie dolnej, wszystkie kanały
// wierzchołków zachowane, tylko ten materiał). Kto jest „dolny”: dalej wzdłuż normalnej (grunt pod posadzką, ściana za
// muralem); w tej samej płaszczyźnie — większa (podkład) pod mniejszą (nakładką). Górna przezroczysta: szkło przed
// nieprzezroczystym — wycinane szkło; nakładka z alfą (napis, kalkomania) — zgłoszenie (ma mieć grubość ≥ 3 cm albo być
// w teksturze). Nie tniemy: ruchomych (drzwi, auta, ludzie, worki…), chronionej kopii siłowni, gracza. Po naprawie
// kontrola liczy pary jeszcze raz (pary_po). ARENA BRAXHORN (hala walk): próg 3 cm i 0.05 m² (właściciel 20:37),
// osobny raport BRAXHORN_ARENA_MIGOTANIE. Miasto: jak dotąd (przesunięcie płaskich) — tylko osiedle tnie.
public static class BraxKontrolaMigotania
{
    struct Prost { public int pl; public float u0, u1, v0, v1; }
    sealed class Pl { public int pow, os, znak; public float d, pole; public readonly List<Vector2[]> tri = new List<Vector2[]>(); }
    sealed class Pow { public Renderer r; public int sub; public string nazwa; public float pole; public Material mat; }
    sealed class Para { public int a, b, plA, plB; public float pole, dd; public Vector3 gdzie; public bool arena; public readonly HashSet<(int, int)> plany = new HashSet<(int, int)>(); }
    sealed class Wykrycie
    {
        public readonly List<Pow> pows = new List<Pow>(); public readonly List<Pl> pls = new List<Pl>(); public readonly List<Prost> prosty = new List<Prost>(1 << 16);
        public List<Para> lista = new List<Para>(); public long trojkaty; public int renderery, kubelki; public bool przerwane; public float czas;
    }

    // Obrys ARENY BRAXHORN (osiedle, ustawia BraxClaudeOsiedle.ArenaInterior) — z elewacją, szyldem i banerami.
    public static Bounds? Arena;
    public const float Prog = .02f, ProgArena = .03f, PoleMin = .02f, PoleArena = .05f;   // osiedle: 3 cm wszędzie, w arenie pole > 0.05 m²
    static bool WArenie(Vector3 p) => Arena.HasValue && BraxKontrola.Obszar == "osiedle" && Arena.Value.Contains(p);

    // POSADZKI (właściciel 20:42: „Kasyno mruga podłoga” — zasada ogólna: posadzka nigdy nie leży na gruncie). Jedna funkcja po
    // zbudowaniu wnętrz osiedla: wszystkie poziome powierzchnie zwrócone w górę na wysokości −0.2…0.3 m (posadzki lokali z kodu:
    // kasyno, ARENA, sala bokserska, klub, siłownia, sklepy, komisariat, domy, garaże…), pary w odstępie < 3 cm → dolna
    // (grunt osiedla EstateMesh_*, stary chodnik, stara podłoga) wycięta dokładnie pod górną. Log BRAXHORN_POSADZKI.
    public static void Posadzki(Scene scena)
    {
        string obszar = BraxKontrola.Obszar; BraxKontrola.Obszar = "osiedle";
        try
        {
            var w = Wykryj(scena, 90f, true);
            var wyn = Wytnij(w, null);
            Physics.SyncTransforms();
            var po = Wykryj(scena, 90f, true);
            var sb = new StringBuilder("BRAXHORN_POSADZKI wyciete_pod=" + wyn.grupyGorne.Count + " lokali/grup [" + string.Join("; ", wyn.grupyGorne.OrderBy(x => x).Take(60)) + "]"
                + " trojkatow_gruntu_usunietych=" + wyn.trojkaty + " pole=" + wyn.pole.ToString("F1") + " m² powierzchni_wyciętych=" + wyn.powierzchnie
                + " pary_przed=" + w.lista.Count + " pary_po=" + po.lista.Count + (w.przerwane || po.przerwane ? " PRZERWANE limitem czasu" : ""));
            foreach (var l in wyn.opisy.Take(40)) sb.Append("\n  ").Append(l);
            foreach (var p in po.lista.Take(15))
                sb.Append("\n  zostało: ").Append(po.pows[p.a].nazwa + " ↔ " + po.pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie)
                    + (wyn.powody.TryGetValue(Klucz2(po.pows[p.a].r, po.pows[p.b].r), out var pw) ? " — " + pw : ""));
            Debug.Log(sb.ToString());
        }
        finally { BraxKontrola.Obszar = obszar; }
    }
    // Wycięcie podkładu pod konkretną nakładką (np. mural siłowni na ścianie chronionej kopii — kontrola tam tylko raportuje,
    // więc generator wycina ścianę sam w chwili stawiania muralu): płaszczyzny osiowe nakładki, w podkładach trójkąty równoległe
    // od 0 do maxOdstep ZA nakładką (i w jej płaszczyźnie) odjęte dokładnie pod jej trójkątami. Zwraca liczbę zmienionych siatek.
    public static int WytnijPodNakladka(Renderer gorny, IEnumerable<Renderer> podklady, float maxOdstep, string folder, out float pole)
    {
        pole = 0; int zmienione = 0;
        var mf = gorny ? gorny.GetComponent<MeshFilter>() : null; if (!mf || !mf.sharedMesh) return 0;
        var m = gorny.transform.localToWorldMatrix; var v = mf.sharedMesh.vertices; var t = mf.sharedMesh.triangles;
        var plaszczyzny = new Dictionary<(int, int, int), (float d, List<Vector2[]> tri)>();
        for (int i = 0; i + 2 < t.Length; i += 3)
        {
            Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
            var n = Vector3.Cross(b - a, c - a); float len = n.magnitude; if (len < 1e-8f) continue; n /= len;
            int os = Mathf.Abs(n.x) > Mathf.Abs(n.y) ? (Mathf.Abs(n.x) > Mathf.Abs(n.z) ? 0 : 2) : (Mathf.Abs(n.y) > Mathf.Abs(n.z) ? 1 : 2);
            float no = os == 0 ? n.x : os == 1 ? n.y : n.z; if (Mathf.Abs(no) < .995f) continue;
            int znak = no > 0 ? 1 : 0; float d = (Os(a, os) + Os(b, os) + Os(c, os)) / 3f;
            var k = (os, znak, Mathf.RoundToInt(d * 1000f));
            if (!plaszczyzny.TryGetValue(k, out var pl)) plaszczyzny[k] = pl = (d, new List<Vector2[]>());
            pl.tri.Add(new[] { new Vector2(U(a, os), V(a, os)), new Vector2(U(b, os), V(b, os)), new Vector2(U(c, os), V(c, os)) });
        }
        foreach (var r in podklady)
        {
            if (!r || r == gorny) continue;
            var pf = r.GetComponent<MeshFilter>(); if (!pf || !pf.sharedMesh) continue;
            if (!r.bounds.Intersects(gorny.bounds) && (r.bounds.SqrDistance(gorny.bounds.center) > (gorny.bounds.extents.magnitude + maxOdstep) * (gorny.bounds.extents.magnitude + maxOdstep))) continue;
            var oryginal = pf.sharedMesh; var mesh = oryginal;
            foreach (var kv in plaszczyzny)
            {
                var (os, znak, _) = kv.Key; float d = kv.Value.d;
                float srodek = d - (znak == 1 ? 1f : -1f) * maxOdstep * .5f;       // pas ZA nakładką: od jej płaszczyzny do maxOdstep w tył
                var nowa = BraxWycinanieSiatki.OdejmijTrojkaty(mesh, r.transform.localToWorldMatrix, -1, os, znak, srodek, maxOdstep * .5f + .0005f, kv.Value.tri, out _, out float p0, out float p1);
                if (!nowa) continue;
                if (mesh != oryginal) UnityEngine.Object.DestroyImmediate(mesh);
                mesh = nowa; pole += p0 - p1;
            }
            if (mesh == oryginal) continue;
            mesh.name = oryginal.name + " (pod nakładką wycięte)";
            AssetDatabase.CreateAsset(mesh, folder + "/" + Bezpieczna(r.name) + "_pod_" + Bezpieczna(gorny.name) + "_" + (zapisane++) + ".asset");
            pf.sharedMesh = mesh; zmienione++;
        }
        return zmienione;
    }

    static string Grupa(Transform t)
    {
        for (var x = t; x; x = x.parent) if (x.name.Contains(" / ") || x.name.Contains(" — ")) return x.name;
        return t.parent ? t.parent.name : t.name;
    }

    public static void Wykonaj(Scene scena, BraxKontrola.Spis spis)
    {
        try { Kamera(); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola migotania — kamera: " + e.Message); }
        float t0 = BraxKontrola.Sekundy;
        var w = Wykryj(scena, 60f);
        bool tniemy = BraxKontrola.Obszar == "osiedle";
        if (!tniemy) { WykonajPrzesuwanie(w, spis, t0); return; }
        var wynik = Wytnij(w, spis);
        Physics.SyncTransforms();
        var po = Wykryj(scena, 60f);
        var sb = new StringBuilder("BRAXHORN_MIGOTANIE " + BraxKontrola.Obszar + ": pary=" + w.lista.Count + " wycięte_pary=" + wynik.pary + " pary_po=" + po.lista.Count
            + " pary_po_powyżej_1m²=" + po.lista.Count(p => p.pole > 1f) + " (wycięte powierzchnie " + wynik.powierzchnie + ", trójkątów ruszonych " + wynik.trojkaty
            + ", pole usunięte " + wynik.pole.ToString("F1") + " m², przesunięte 0 — zasada: dolna warstwa wycięta; renderery " + w.renderery + ", trójkąty osiowe " + w.trojkaty
            + ", prostokąty " + w.prosty.Count + ", czas " + (BraxKontrola.Sekundy - t0).ToString("F1") + " s" + (w.przerwane || po.przerwane ? ", PRZERWANE limitem czasu" : "") + ")");
        sb.Append("\nBRAXHORN_MIGOTANIE nierozwiązane po wycięciu (największe, do zdjęć):");
        foreach (var p in po.lista.Take(25))
        {
            string l = "  " + po.pows[p.a].nazwa + " ↔ " + po.pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, wspólne " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie)
                + (wynik.powody.TryGetValue(Klucz2(po.pows[p.a].r, po.pows[p.b].r), out var pw) ? " — " + pw : "");
            sb.Append('\n').Append(l); BraxKontrola.Zapisz("migotanie: " + l.Trim());
        }
        foreach (var l in wynik.opisy.Take(300)) BraxKontrola.Zapisz("migotanie: " + l);
        Debug.Log(sb.ToString());
        // E26 (build 45): raport per lokal/grupa (kasyno — sala, VIP, biuro, WC; hala walk; klub; komisariat; sklepy; wille…)
        string Lokal(Wykrycie x, Para p) { var ga = Grupa(x.pows[p.a].r.transform); var gb = Grupa(x.pows[p.b].r.transform); return ga.StartsWith("Osiedle") || !gb.StartsWith("Osiedle") ? ga : gb; }
        var przedL = w.lista.GroupBy(p => Lokal(w, p)).ToDictionary(g => g.Key, g => g.Count());
        var poL = po.lista.GroupBy(p => Lokal(po, p)).ToDictionary(g => g.Key, g => g.Count());
        var wn = new StringBuilder("BRAXHORN_WNETRZA_MIGOTANIE (próg 3 cm, lokale/grupy z parami przed wycięciem):");
        foreach (var kv in przedL.OrderByDescending(kv => kv.Value).Take(40))
            wn.Append("\n  lokal=").Append(kv.Key).Append(" pary_przed=").Append(kv.Value).Append(" po=").Append(poL.TryGetValue(kv.Key, out var ile) ? ile : 0);
        Debug.Log(wn.ToString());
        if (Arena.HasValue)
        {
            var przed = w.lista.Where(p => p.arena).ToList(); var zostaly = po.lista.Where(p => p.arena).ToList();
            var a = new StringBuilder("BRAXHORN_ARENA_MIGOTANIE pary_przed=" + przed.Count + " pary_po=" + zostaly.Count + " (próg " + (ProgArena * 100f).ToString("F0") + " cm, pole > " + PoleArena.ToString("F2") + " m², obrys " + BraxKontrola.F(Arena.Value.min) + "–" + BraxKontrola.F(Arena.Value.max) + ") usunięte:");
            foreach (var l in wynik.opisyAreny.Take(60)) a.Append("\n  ").Append(l);
            if (zostaly.Count > 0)
            {
                a.Append("\n  zostały:");
                foreach (var p in zostaly.Take(30)) a.Append("\n  ").Append(po.pows[p.a].nazwa + " ↔ " + po.pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie)
                    + (wynik.powody.TryGetValue(Klucz2(po.pows[p.a].r, po.pows[p.b].r), out var pw) ? " — " + pw : ""));
            }
            if (zostaly.Count > 0) Debug.LogError(a.ToString() + "\nBRAXHORN_ARENA_MIGOTANIE DO POPRAWKI: zostały pary w arenie");
            else Debug.Log(a.ToString());
        }
    }

    static (Renderer, Renderer) Klucz2(Renderer a, Renderer b) => a.GetInstanceID() < b.GetInstanceID() ? (a, b) : (b, a);

    static Wykrycie Wykryj(Scene scena, float limit, bool tylkoPosadzki = false)
    {
        var W = new Wykrycie();
        float t0 = BraxKontrola.Sekundy;
        var pows = W.pows; var pls = W.pls; var prosty = W.prosty;
        var plIdx = new Dictionary<(int, int, int, int), int>();
        var indeks = new Dictionary<long, List<int>>();
        var bylyProst = new HashSet<(int, int, int, int, int)>();
        long trojkaty = 0; int renderery = 0; bool przerwane = false;
        foreach (var root in scena.GetRootGameObjects())
        {
            if (przerwane) break;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (BraxKontrola.Sekundy - t0 > limit * .6f) { przerwane = true; break; }
                if (!BraxKontrola.Widoczny(r) || r.gameObject.layer == BraxKontrola.WarstwaKamery || BraxKontrola.Roslina2(r)) continue;
                var mf = r.GetComponent<MeshFilter>(); var mesh = mf ? mf.sharedMesh : null;
                if (!mesh || !BraxKontrola.Dane(mesh, out var v, out _)) continue;
                var m = r.transform.localToWorldMatrix; var wv = new Vector3[v.Length];
                for (int i = 0; i < v.Length; i++) wv[i] = m.MultiplyPoint3x4(v[i]);
                renderery++;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    int[] t; try { t = mesh.GetTriangles(s); } catch { continue; }
                    if (t.Length > 3000000) continue;
                    int pow = pows.Count; float polePow = 0;
                    var mat = s < r.sharedMaterials.Length ? r.sharedMaterials[s] : r.sharedMaterial;
                    pows.Add(new Pow { r = r, sub = s, mat = mat, nazwa = r.name + (mat && mat.name != r.name ? " [" + mat.name + "]" : "") });
                    for (int i = 0; i + 2 < t.Length; i += 3)
                    {
                        Vector3 a = wv[t[i]], b = wv[t[i + 1]], c = wv[t[i + 2]];
                        var n = Vector3.Cross(b - a, c - a); float p2 = n.magnitude; if (p2 < .005f) continue;   // pole < 0.0025 m²
                        n /= p2; int os = Mathf.Abs(n.x) > Mathf.Abs(n.y) ? (Mathf.Abs(n.x) > Mathf.Abs(n.z) ? 0 : 2) : (Mathf.Abs(n.y) > Mathf.Abs(n.z) ? 1 : 2);
                        float no = os == 0 ? n.x : os == 1 ? n.y : n.z; if (Mathf.Abs(no) < .995f) continue;
                        int znak = no > 0 ? 1 : 0;
                        float d = (Os(a, os) + Os(b, os) + Os(c, os)) / 3f;
                        if (tylkoPosadzki && (os != 1 || znak != 1 || d > .3f || d < -.2f)) continue;
                        trojkaty++;
                        float u0 = Mathf.Min(U(a, os), Mathf.Min(U(b, os), U(c, os))), u1 = Mathf.Max(U(a, os), Mathf.Max(U(b, os), U(c, os)));
                        float v0 = Mathf.Min(V(a, os), Mathf.Min(V(b, os), V(c, os))), v1 = Mathf.Max(V(a, os), Mathf.Max(V(b, os), V(c, os)));
                        if (u1 - u0 < .02f || v1 - v0 < .02f) continue;
                        var kp = (pow, os, znak, Mathf.RoundToInt(d * 1000f));
                        if (!plIdx.TryGetValue(kp, out int pl)) { pl = pls.Count; plIdx[kp] = pl; pls.Add(new Pl { pow = pow, os = os, znak = znak, d = d }); }
                        pls[pl].tri.Add(new[] { new Vector2(U(a, os), V(a, os)), new Vector2(U(b, os), V(b, os)), new Vector2(U(c, os), V(c, os)) });   // build 45: do wycinania pod górną warstwą
                        // dwa trójkąty czworokąta mają ten sam prostokąt — raz
                        if (!bylyProst.Add((pl, Mathf.RoundToInt(u0 * 200f), Mathf.RoundToInt(u1 * 200f), Mathf.RoundToInt(v0 * 200f), Mathf.RoundToInt(v1 * 200f)))) continue;
                        float pole = (u1 - u0) * (v1 - v0); pls[pl].pole += pole; polePow += pole;
                        int id = prosty.Count; prosty.Add(new Prost { pl = pl, u0 = u0, u1 = u1, v0 = v0, v1 = v1 });
                        int cu0 = Mathf.FloorToInt(u0 / 4f), cu1 = Mathf.FloorToInt(u1 / 4f), cv0 = Mathf.FloorToInt(v0 / 4f), cv1 = Mathf.FloorToInt(v1 / 4f);
                        if ((long)(cu1 - cu0 + 1) * (cv1 - cv0 + 1) > 4000) continue;
                        int db = Mathf.FloorToInt(d / ProgArena);                         // kubełek 3 cm (próg areny); pary sprawdzane z kubełkiem wyżej
                        for (int cu = cu0; cu <= cu1; cu++) for (int cv = cv0; cv <= cv1; cv++)
                        {
                            long key = Klucz(os, znak, db, cu, cv);
                            if (!indeks.TryGetValue(key, out var l)) indeks[key] = l = new List<int>(4);
                            l.Add(id);
                        }
                    }
                    pows[pow].pole = polePow;
                }
            }
        }
        bylyProst.Clear();
        // pary: kubełek (d) z kubełkiem (d + 1) i sam ze sobą; zamiatanie po u
        var pary = new Dictionary<(int, int), Para>();
        var tmp = new List<(int id, bool gora)>();
        int kubelki = 0;
        foreach (var kv in indeks)
        {
            if (BraxKontrola.Sekundy - t0 > limit) { przerwane = true; break; }
            Rozpakuj(kv.Key, out int os, out int znak, out int db, out int cu, out int cv);
            tmp.Clear();
            foreach (var id in kv.Value) tmp.Add((id, false));
            if (indeks.TryGetValue(Klucz(os, znak, db + 1, cu, cv), out var wyzej)) foreach (var id in wyzej) tmp.Add((id, true));
            if (tmp.Count < 2) continue;
            kubelki++;
            tmp.Sort((x, y) => prosty[x.id].u0.CompareTo(prosty[y.id].u0));
            for (int i = 0; i < tmp.Count; i++)
            {
                var A = prosty[tmp[i].id]; var plA = pls[A.pl];
                for (int k = i + 1; k < tmp.Count && prosty[tmp[k].id].u0 < A.u1; k++)
                {
                    if (tmp[i].gora && tmp[k].gora) continue;                    // para z kubełka wyżej — liczona tam
                    var B = prosty[tmp[k].id]; var plB = pls[B.pl];
                    if (plA.pow == plB.pow) continue;
                    float dd = Mathf.Abs(plA.d - plB.d); if (dd >= ProgArena) continue;
                    float iu0 = Mathf.Max(A.u0, B.u0), iu1 = Mathf.Min(A.u1, B.u1), iv0 = Mathf.Max(A.v0, B.v0), iv1 = Mathf.Min(A.v1, B.v1);
                    if (iu1 <= iu0 || iv1 <= iv0) continue;
                    if (dd >= Prog && BraxKontrola.Obszar != "osiedle") continue;   // build 45: na osiedlu próg 3 cm wszędzie (wnętrza: dywany, panele, sufity — E26), miasto 2 cm
                    // para prostokątów widziana w kilku komórkach — liczona w komórce lewego dolnego rogu części wspólnej
                    if (Mathf.FloorToInt(iu0 / 4f) != cu || Mathf.FloorToInt(iv0 / 4f) != cv) continue;
                    var key = plA.pow < plB.pow ? (plA.pow, plB.pow) : (plB.pow, plA.pow);
                    if (!pary.TryGetValue(key, out var p)) pary[key] = p = new Para { a = key.Item1, b = key.Item2, plA = plA.pow == key.Item1 ? A.pl : B.pl, plB = plA.pow == key.Item1 ? B.pl : A.pl, dd = dd };
                    float pole = (iu1 - iu0) * (iv1 - iv0);
                    if (pole > p.pole * .5f || p.pole == 0) p.gdzie = Swiat(os, plA.d, (iu0 + iu1) * .5f, (iv0 + iv1) * .5f);
                    p.pole += pole; if (pole > .01f) p.dd = Mathf.Min(p.dd, dd);
                    p.plany.Add(plA.pow == key.Item1 ? (A.pl, B.pl) : (B.pl, A.pl));
                }
            }
        }
        indeks.Clear();
        foreach (var p in pary.Values) p.arena = WArenie(p.gdzie);
        W.lista = pary.Values.Where(p => p.arena ? p.pole > PoleArena : p.pole >= PoleMin).OrderByDescending(p => p.pole).ToList();
        W.trojkaty = trojkaty; W.renderery = renderery; W.kubelki = kubelki; W.przerwane = przerwane; W.czas = BraxKontrola.Sekundy - t0;
        return W;
    }

    // ---------------- naprawa: wycięcie dolnej warstwy (osiedle) ----------------
    sealed class WynikWyciecia
    {
        public int pary, powierzchnie, trojkaty; public float pole;
        public readonly List<string> opisy = new List<string>(), opisyAreny = new List<string>();
        public readonly Dictionary<(Renderer, Renderer), string> powody = new Dictionary<(Renderer, Renderer), string>();
        public readonly HashSet<string> grupyGorne = new HashSet<string>();
    }

    static bool Przezroczysty(Material m)
    {
        if (!m) return false;
        if (m.renderQueue >= 2450) return true;
        if (m.IsKeywordEnabled("_ALPHATEST_ON") || m.IsKeywordEnabled("_ALPHABLEND_ON") || m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")) return true;
        if (m.HasProperty("_Mode") && m.GetFloat("_Mode") > .5f && m.shader && m.shader.name == "Standard") return true;
        string sh = m.shader ? m.shader.name.ToLowerInvariant() : "";
        return sh.Contains("transparent") || sh.Contains("cutout") || sh.Contains("particle") || sh.Contains("fade");
    }
    static readonly System.Text.RegularExpressions.Regex Szklo = new System.Text.RegularExpressions.Regex(@"szk|szyb|glass|okn|window|balustrad", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    static readonly Type[] Ruchome = { typeof(Rigidbody), typeof(Animation), typeof(Animator), typeof(BraxCityDoor), typeof(BraxCityElevator), typeof(BraxCityMovingLiftPart),
        typeof(BraxVessel), typeof(BraxAuto), typeof(BraxAutoGracza), typeof(BraxRuchAut), typeof(BraxWorek), typeof(BraxPilka), typeof(BraxLina), typeof(BraxSzlaban),
        typeof(BraxMieszkaniec), typeof(BraxPieszy), typeof(BraxGymPlayer), typeof(BraxPodroznyAuta) };
    static readonly Dictionary<Renderer, bool> ruchomeCache = new Dictionary<Renderer, bool>();
    static bool Ruchomy(Renderer r)
    {
        if (ruchomeCache.TryGetValue(r, out var b)) return b;
        b = false; foreach (var t in Ruchome) if (r.GetComponentInParent(t)) { b = true; break; }
        // auta z generatora („Auto zaparkowane · …”, „Auto · car_…”, „Auto Patryka …”) — nie tniemy (siatka na każdy egzemplarz = rozmiar APK)
        if (!b) for (var x = r.transform; x; x = x.parent) if (x.name.StartsWith("Auto ")) { b = true; break; }
        ruchomeCache[r] = b; return b;
    }

    static WynikWyciecia Wytnij(Wykrycie W, BraxKontrola.Spis spis)
    {
        var wyn = new WynikWyciecia(); ruchomeCache.Clear();
        if (spis != null && chronioneZ != spis) { chronione.Clear(); chronioneZ = spis; foreach (var j in spis.jednostki) if (j.chroniony) foreach (var x in j.rs) chronione.Add(x); }
        if (spis == null) { chronione.Clear(); chronioneZ = null; }
        bool Nietykalny(Renderer r) => chronione.Contains(r) || Chroniony(r.transform) || r.GetComponentInParent<BraxGymPlayer>();
        // zadania: (renderer, materiał, oś, zwrot, płaszczyzna mm) → trójkąty górnej warstwy
        var zadania = new Dictionary<(Renderer, int, int, int, int), (float d, List<Vector2[]> gorne, bool arena, HashSet<string> nad)>();
        foreach (var p in W.lista)
        {
            var A = W.pows[p.a]; var B = W.pows[p.b];
            string powod = null; int dodane = 0;
            foreach (var (ia, ib) in p.plany)
            {
                var pa = W.pls[ia]; var pb = W.pls[ib];
                float ea = Pozycja(pa), eb = Pozycja(pb);
                Pow ciety, gorny; Pl plC, plG;
                bool przA = Przezroczysty(A.mat), przB = Przezroczysty(B.mat);
                if (Mathf.Abs(ea - eb) < .001f)
                {   // ta sama płaszczyzna: podkład (większy) pod nakładką (mniejszą)
                    bool aPodklad = A.pole >= B.pole;
                    (ciety, plC, gorny, plG) = aPodklad ? (A, pa, B, pb) : (B, pb, A, pa);
                    if (Przezroczysty(gorny.mat)) { powod = "nakładka z alfą w tej samej płaszczyźnie (" + gorny.nazwa + ") — do tekstury albo grubość ≥ 3 cm"; continue; }
                }
                else
                {
                    bool aPrzod = ea > eb;
                    (gorny, plG, ciety, plC) = aPrzod ? (A, pa, B, pb) : (B, pb, A, pa);
                    if (Przezroczysty(gorny.mat))
                    {
                        if (!Przezroczysty(ciety.mat) && gorny.mat && Szklo.IsMatch(gorny.mat.name + " " + gorny.r.name)) { var t = (ciety, plC); (ciety, plC) = (gorny, plG); (gorny, plG) = t; }   // szkło przed ramą: tniemy szkło
                        else { powod = "przednia warstwa z alfą (" + gorny.nazwa + ") — nie wycinam tła (byłaby dziura)"; continue; }
                    }
                }
                if (Ruchomy(ciety.r) || Ruchomy(gorny.r)) { powod = "ruchoma część (drzwi/auto/worek…) — bez wycinania"; continue; }
                if (Nietykalny(ciety.r)) { powod = "chroniona kopia siłowni / gracz — tylko raport"; continue; }
                var key = (ciety.r, ciety.sub, plC.os, plC.znak, Mathf.RoundToInt(plC.d * 1000f));
                if (!zadania.TryGetValue(key, out var z)) zadania[key] = z = (plC.d, new List<Vector2[]>(), false, new HashSet<string>());
                z.gorne.AddRange(plG.tri); z.nad.Add(gorny.nazwa); wyn.grupyGorne.Add(Grupa(gorny.r.transform));
                if (p.arena && !z.arena) zadania[key] = z = (z.d, z.gorne, true, z.nad);
                dodane++;
            }
            if (dodane > 0) wyn.pary++;
            else if (powod != null) wyn.powody[Klucz2(A.r, B.r)] = powod;
        }
        // wykonanie: po rendererze, kolejne zadania na wyniku poprzedniego
        if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets", "PlayableGenerated");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/PlayableGenerated", "KontrolaMigotania");
        foreach (var grupa in zadania.GroupBy(kv => kv.Key.Item1))
        {
            var r = grupa.Key; var mf = r ? r.GetComponent<MeshFilter>() : null; if (!mf || !mf.sharedMesh) continue;
            var oryginal = mf.sharedMesh; var mesh = oryginal; int zmian = 0; float usuniete = 0; bool arena = false; var nad = new HashSet<string>();
            try
            {
                foreach (var kv in grupa)
                {
                    var (_, sub, os, znak, _) = kv.Key; var z = kv.Value;
                    var nowa = BraxWycinanieSiatki.OdejmijTrojkaty(mesh, r.transform.localToWorldMatrix, sub, os, znak, z.d, .002f, z.gorne, out int ch, out float p0, out float p1);
                    if (!nowa) continue;
                    if (mesh != oryginal) UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = nowa; zmian += ch; usuniete += p0 - p1; arena |= z.arena; nad.UnionWith(z.nad);
                }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: migotanie — wycinanie " + r.name + ": " + e.Message); if (mesh != oryginal) UnityEngine.Object.DestroyImmediate(mesh); continue; }
            if (mesh == oryginal) continue;
            mesh.name = oryginal.name + " (bez nakładania)";
            AssetDatabase.CreateAsset(mesh, Folder + "/" + Bezpieczna(r.name) + "_" + (zapisane++) + ".asset");
            mf.sharedMesh = mesh;                                   // tylko ten renderer (siatka bywa wspólna dla kilku obiektów); kolizje bez zmian
            wyn.powierzchnie++; wyn.trojkaty += zmian; wyn.pole += usuniete;
            string opis = r.name + ": wycięte " + usuniete.ToString("F2") + " m² (" + zmian + " trójkątów) pod: " + string.Join(", ", nad.Take(4)) + (nad.Count > 4 ? " …" : "") + " " + BraxKontrola.F(r.bounds.center);
            wyn.opisy.Add(opis); if (arena) wyn.opisyAreny.Add(opis);
        }
        return wyn;
    }
    const string Folder = "Assets/PlayableGenerated/KontrolaMigotania";
    static int zapisane;
    static string Bezpieczna(string s) { var sb = new StringBuilder(); foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_'); return sb.Length > 40 ? sb.ToString(0, 40) : sb.ToString(); }

    // ---------------- dotychczasowa naprawa (miasto): przesunięcie płaskiej powierzchni o 2 cm ----------------
    static void WykonajPrzesuwanie(Wykrycie W, BraxKontrola.Spis spis, float t0)
    {
        var pows = W.pows; var pls = W.pls; var lista = W.lista.Where(p => p.dd < Prog).ToList();
        // naprawa
        var ruch = new Dictionary<Renderer, (int os, float delta)>();
        float Teraz(Pow w, Pl pl) => Pozycja(pl) + (ruch.TryGetValue(w.r, out var rr) && rr.os == pl.os ? (pl.znak == 1 ? rr.delta : -rr.delta) : 0f);
        int popr = 0;
        foreach (var p in lista)
        {
            var A = pows[p.a]; var B = pows[p.b]; var pa = pls[p.plA]; var pb = pls[p.plB];
            if (Mathf.Abs(Teraz(A, pa) - Teraz(B, pb)) >= .0195f) continue;   // już rozsunięte (np. przy poprzedniej parze)
            bool mniejszaA = A.pole <= B.pole;
            var (m1, pm1, o1, po1) = mniejszaA ? (A, pa, B, pb) : (B, pb, A, pa);
            if (!Przesun(m1, pm1, Teraz(o1, po1), ruch, spis)) Przesun(o1, po1, Teraz(m1, pm1), ruch, spis);
        }
        var nierozwiazane = new List<Para>();
        foreach (var p in lista)
        {
            var A = pows[p.a]; var B = pows[p.b];
            if (Mathf.Abs(Teraz(A, pls[p.plA]) - Teraz(B, pls[p.plB])) >= .0195f) popr++; else nierozwiazane.Add(p);
        }
        Physics.SyncTransforms();
        var sb = new StringBuilder("BRAXHORN_MIGOTANIE " + BraxKontrola.Obszar + ": pary=" + lista.Count + " poprawione=" + popr + " (przesunięte powierzchnie " + ruch.Count +
            ", renderery " + W.renderery + ", trójkąty osiowe " + W.trojkaty + ", prostokąty " + W.prosty.Count + ", kubełki " + W.kubelki + ", czas " + (BraxKontrola.Sekundy - t0).ToString("F1") + " s" + (W.przerwane ? ", PRZERWANE limitem czasu" : "") + ")");
        sb.Append("\nBRAXHORN_MIGOTANIE nierozwiązane (największe, do zdjęć):");
        foreach (var p in nierozwiazane.Take(20))
        {
            string l = "  " + pows[p.a].nazwa + " ↔ " + pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, wspólne " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie);
            sb.Append('\n').Append(l); BraxKontrola.Zapisz("migotanie: " + l.Trim());
        }
        foreach (var kv in ruch.Take(200)) BraxKontrola.Zapisz("migotanie: przesunięto " + kv.Key.name + " o " + (kv.Value.delta * 100f).ToString("F1") + " cm wzdłuż " + "XYZ"[kv.Value.os]);
        Debug.Log(sb.ToString());
    }

    static float Pozycja(Pl p) => p.znak == 1 ? p.d : -p.d;                 // położenie wzdłuż normalnej (przód = większe)
    static readonly HashSet<Renderer> chronione = new HashSet<Renderer>();
    static BraxKontrola.Spis chronioneZ;
    static bool Przesun(Pow ruszana, Pl pr, float eo, Dictionary<Renderer, (int, float)> ruch, BraxKontrola.Spis spis)
    {
        var r = ruszana.r; if (!r || ruch.ContainsKey(r)) return false;
        var b = r.bounds; float gr = pr.os == 0 ? b.size.x : pr.os == 1 ? b.size.y : b.size.z;
        if (gr > .12f) return false;                                            // nie płaska wzdłuż tej osi — przesunięcie ruszyłoby bryły
        if (r.GetComponentInParent<BraxGymPlayer>()) return false;
        if (chronioneZ != spis) { chronione.Clear(); chronioneZ = spis; foreach (var j in spis.jednostki) if (j.chroniony) foreach (var x in j.rs) chronione.Add(x); }
        if (chronione.Contains(r) || Chroniony(r.transform)) return false;
        float e = Pozycja(pr);
        float cel = e >= eo - .0005f ? eo + .02f : eo - .02f;                  // z przodu (albo w tej samej płaszczyźnie) → 2 cm przed; schowana → 2 cm za
        float de = cel - e;                                                     // wzdłuż normalnej
        float dw = pr.znak == 1 ? de : -de;                                     // wzdłuż osi świata
        var o = pr.os == 0 ? Vector3.right : pr.os == 1 ? Vector3.up : Vector3.forward;
        r.transform.position += o * dw;
        ruch[r] = (pr.os, dw);
        return true;
    }

    static bool Chroniony(Transform t) => BraxKontrola.WChronionym(t);
    static float Os(Vector3 p, int os) => os == 0 ? p.x : os == 1 ? p.y : p.z;
    static float U(Vector3 p, int os) => os == 0 ? p.z : p.x;
    static float V(Vector3 p, int os) => os == 1 ? p.z : p.y;
    static Vector3 Swiat(int os, float d, float u, float v) => os == 0 ? new Vector3(d, v, u) : os == 1 ? new Vector3(u, d, v) : new Vector3(u, v, d);
    static long Klucz(int os, int znak, int db, int cu, int cv) => ((((long)(os * 2 + znak) * 65536L + (db + 32768)) * 4096L + (cu + 2048)) * 4096L) + (cv + 2048);
    static void Rozpakuj(long k, out int os, out int znak, out int db, out int cu, out int cv)
    {
        cv = (int)(k % 4096L) - 2048; k /= 4096L; cu = (int)(k % 4096L) - 2048; k /= 4096L; db = (int)(k % 65536L) - 32768; k /= 65536L; znak = (int)(k % 2); os = (int)(k / 2);
    }

    // Bliska płaszczyzna kamery gracza ≥ 0.15 m (lepsza precyzja głębi na telefonie). BraxClaudeKamera przełącza 0.25 / 0.1 m przy
    // ścianie — „blisko” podniesione do 0.15 m: róg bliskiej płaszczyzny 0.15 m przy FOV 55° i ekranie 20:9 jest 0.24 m od kamery,
    // a kamera gracza (SphereCast r 0.24 w BraxGymPlayer) trzyma się ≥ 0.24 m od ścian — róg nie wchodzi w ścianę.
    static void Kamera()
    {
        int n = 0;
        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cam.targetTexture) continue;
            if (cam.nearClipPlane < .15f) { cam.nearClipPlane = .15f; n++; }
            var k = cam.GetComponent<BraxClaudeKamera>();
            if (k && k.blisko < .15f) { k.blisko = .15f; n++; }
        }
        Debug.Log("BRAXHORN_MIGOTANIE " + BraxKontrola.Obszar + ": kamera — bliska płaszczyzna ≥ 0.15 m (zmienione ustawienia: " + n + ")");
    }
}

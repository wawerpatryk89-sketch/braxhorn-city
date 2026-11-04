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
public static class BraxKontrolaMigotania
{
    struct Prost { public int pl; public float u0, u1, v0, v1; }
    sealed class Pl { public int pow, os, znak; public float d, pole; public List<Vector2[]> tri; }
    sealed class Pow { public Renderer r; public int sub; public string nazwa; public float pole; public Material mat; public bool silownia; }
    sealed class Para { public int a, b, plA, plB; public float pole, dd; public Vector3 gdzie; public HashSet<(int, int)> plany; }
    sealed class Wykrycie { public List<Pow> pows = new List<Pow>(); public List<Pl> pls = new List<Pl>(); public List<Para> lista; public long trojkaty; public int renderery, kubelki, prostokaty; public bool przerwane; }

    // C3 (właściciel 2.11: „wyjście z siłowni: futryna migocze — od tygodnia nie zrobione; elewacja migocze”). Siłownia jest
    // strefą chronioną (zatwierdzona) — przesuwanie jej części było zabronione, więc pary przy drzwiach wyjścia i na elewacji
    // zostawały. Teraz w strefie siłowni (+2 m: elewacja, wyjście, chodnik przed drzwiami) DOLNA warstwa jest WYCINANA dokładnie
    // pod górną (odejmowanie trójkątów — wzór z osiedla, BraxWycinanieSiatki, ccaeaa1): ościeżnica/futryna na ścianie → ściana
    // pod futryną wycięta; dwie płaszczyzny elewacji w jednej płaszczyźnie → większa wycięta pod mniejszą. Wygląd bez zmian
    // (usuwane jest tylko to, co i tak zasłonięte), nic nie jest przesuwane. Ruchome (skrzydło drzwi siłowni, BraxCityDoor,
    // animacje) i warstwy z alfą przed tłem — bez cięcia (raport). Log BRAXHORN_SILOWNIA_MIGOTANIE pary_przed=… pary_po=….
    public static Bounds StrefaSilowni()
    {
        var b = new Bounds(); b.SetMinMax(new Vector3(-19f, -.5f, -17f), new Vector3(19f, 10f, 21.7f));
        if (BraxKontrola.JestStrefa) b = BraxKontrola.StrefaChroniona;
        b.Expand(4f); return b;   // ±2 m: elewacja od ulicy, wyjście, chodnik i schody przed drzwiami
    }
    static Bounds strefa; static bool tylkoStrefa;
    // pkt 5 / 49 / 27 (właściciel: „mozaika” — dwie elewacje w jednej płaszczyźnie): ta sama zasada wycinania dolnej warstwy
    // także na kamienicach przy BRAXHORN NUTRITION i lewej przy BRAXHORN GYM oraz w alejce przy BANKU (miasto B).
    // Tylko w scenach miasta (osiedle: sama strefa siłowni nie istnieje — bez zmian).
    static Bounds[] strefy = new Bounds[0];
    static Bounds[] Strefy()
    {
        var l = new List<Bounds> { StrefaSilowni() };
        if (BraxKontrola.Obszar == "miasto") // scena w CI może nie być aktywną BraxhornCity podczas audytu; obszar jest ustawiany jawnie przez BraxGymBuild
            foreach (var (a, b) in new[] { (new Vector3(12f, -.5f, 4f), new Vector3(50f, 34f, 46f)),          // kamienica z BRAXHORN NUTRITION i sąsiednie
                                           (new Vector3(-50f, -.5f, -22f), new Vector3(-16f, 34f, 26f)),      // lewa kamienica przy BRAXHORN GYM
                                           (new Vector3(-121f, -.5f, 44f), new Vector3(-90f, 40f, 76f)) })   // alejka przy BANKU (Aleja Północna 1)
            { var x = new Bounds(); x.SetMinMax(a, b); l.Add(x); }
        return l.ToArray();
    }
    static bool WStrefie(Vector3 p) { foreach (var b in strefy) if (b.Contains(p)) return true; return false; }
    static bool WStrefie(Bounds r) { foreach (var b in strefy) if (b.Intersects(r)) return true; return false; }
    static readonly Dictionary<Renderer, LODGroup> lody = new Dictionary<Renderer, LODGroup>();
    static LODGroup Lod(Renderer r) { if (!lody.TryGetValue(r, out var g)) lody[r] = g = r.GetComponentInParent<LODGroup>(); return g; }

    public static void Wykonaj(Scene scena, BraxKontrola.Spis spis)
    {
        try { Kamera(); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola migotania — kamera: " + e.Message); }
        float t0 = BraxKontrola.Sekundy;
        lody.Clear();
        strefa = StrefaSilowni(); strefy = Strefy(); tylkoStrefa = false;
        var W = Wykryj(scena, 60f);
        var pows = W.pows; var pls = W.pls; var lista = W.lista;
        // naprawa
        var ruch = new Dictionary<Renderer, (int os, float delta)>();
        float Teraz(Pow w, Pl pl) => Pozycja(pl) + (ruch.TryGetValue(w.r, out var rr) && rr.os == pl.os ? (pl.znak == 1 ? rr.delta : -rr.delta) : 0f);
        int popr = 0;
        foreach (var p in lista)
        {
            var A = pows[p.a]; var B = pows[p.b]; var pa = pls[p.plA]; var pb = pls[p.plB];
            if (WStrefie(p.gdzie)) continue; // ELEWACJE: bez przesuwania i nakładania. Podkład wyciąć niżej pod warstwą docelową.
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
            ", renderery " + W.renderery + ", trójkąty osiowe " + W.trojkaty + ", prostokąty " + W.prostokaty + ", kubełki " + W.kubelki + ", czas " + (BraxKontrola.Sekundy - t0).ToString("F1") + " s" + (W.przerwane ? ", PRZERWANE limitem czasu" : "") + "; pary LOD0↔LOD1 tego samego obiektu pominięte — nie rysują się razem)");
        sb.Append("\nBRAXHORN_MIGOTANIE nierozwiązane (największe, do zdjęć):");
        foreach (var p in nierozwiazane.Where(p => !(pows[p.a].silownia && pows[p.b].silownia && WStrefie(p.gdzie))).Take(20))
        {
            string l = "  " + pows[p.a].nazwa + " ↔ " + pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, wspólne " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie);
            sb.Append('\n').Append(l); BraxKontrola.Zapisz("migotanie: " + l.Trim());
        }
        foreach (var kv in ruch.Take(200)) BraxKontrola.Zapisz("migotanie: przesunięto " + kv.Key.name + " o " + (kv.Value.delta * 100f).ToString("F1") + " cm wzdłuż " + "XYZ"[kv.Value.os]);
        Debug.Log(sb.ToString());
        // C3: siłownia — wycięcie dolnej warstwy (przed/po)
        // WŁAŚCICIEL 3.11 10:33 („elewacje, co migają, jak nie zmienione, to zmienić”): pary, których przesunięcie nie rozsunęło,
        // też idą do WYCINANIA dolnej warstwy (zasada „nie nakładamy — wycinamy”); w strefach elewacji NIGDY nie przesuwamy obu warstw.
        try { Silownia(scena, W, ruch, nierozwiazane); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_SILOWNIA_MIGOTANIE — wycinanie: " + e); }
        lody.Clear();
    }

    static Wykrycie Wykryj(Scene scena, float limit)
    {
        var W = new Wykrycie(); float t0 = BraxKontrola.Sekundy;
        var pows = W.pows; var pls = W.pls; var prosty = new List<Prost>(1 << 16);
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
                bool wSilowni = WStrefie(r.bounds);
                if (tylkoStrefa && !wSilowni) continue;
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
                    pows.Add(new Pow { r = r, sub = s, mat = mat, silownia = wSilowni, nazwa = r.name + (mat && mat.name != r.name ? " [" + mat.name + "]" : "") });
                    for (int i = 0; i + 2 < t.Length; i += 3)
                    {
                        Vector3 a = wv[t[i]], b = wv[t[i + 1]], c = wv[t[i + 2]];
                        var n = Vector3.Cross(b - a, c - a); float p2 = n.magnitude; if (p2 < .005f) continue;   // pole < 0.0025 m²
                        n /= p2; int os = Mathf.Abs(n.x) > Mathf.Abs(n.y) ? (Mathf.Abs(n.x) > Mathf.Abs(n.z) ? 0 : 2) : (Mathf.Abs(n.y) > Mathf.Abs(n.z) ? 1 : 2);
                        float no = os == 0 ? n.x : os == 1 ? n.y : n.z; if (Mathf.Abs(no) < .995f) continue;
                        int znak = no > 0 ? 1 : 0; trojkaty++;
                        float d = (Os(a, os) + Os(b, os) + Os(c, os)) / 3f;
                        float u0 = Mathf.Min(U(a, os), Mathf.Min(U(b, os), U(c, os))), u1 = Mathf.Max(U(a, os), Mathf.Max(U(b, os), U(c, os)));
                        float v0 = Mathf.Min(V(a, os), Mathf.Min(V(b, os), V(c, os))), v1 = Mathf.Max(V(a, os), Mathf.Max(V(b, os), V(c, os)));
                        if (u1 - u0 < .02f || v1 - v0 < .02f) continue;
                        var kp = (pow, os, znak, Mathf.RoundToInt(d * 1000f));
                        if (!plIdx.TryGetValue(kp, out int pl)) { pl = pls.Count; plIdx[kp] = pl; pls.Add(new Pl { pow = pow, os = os, znak = znak, d = d, tri = wSilowni ? new List<Vector2[]>() : null }); }
                        if (wSilowni) pls[pl].tri.Add(new[] { new Vector2(U(a, os), V(a, os)), new Vector2(U(b, os), V(b, os)), new Vector2(U(c, os), V(c, os)) });   // C3: do wycinania pod górną warstwą
                        // dwa trójkąty czworokąta mają ten sam prostokąt — raz
                        if (!bylyProst.Add((pl, Mathf.RoundToInt(u0 * 200f), Mathf.RoundToInt(u1 * 200f), Mathf.RoundToInt(v0 * 200f), Mathf.RoundToInt(v1 * 200f)))) continue;
                        float pole = (u1 - u0) * (v1 - v0); pls[pl].pole += pole; polePow += pole;
                        int id = prosty.Count; prosty.Add(new Prost { pl = pl, u0 = u0, u1 = u1, v0 = v0, v1 = v1 });
                        int cu0 = Mathf.FloorToInt(u0 / 4f), cu1 = Mathf.FloorToInt(u1 / 4f), cv0 = Mathf.FloorToInt(v0 / 4f), cv1 = Mathf.FloorToInt(v1 / 4f);
                        if ((long)(cu1 - cu0 + 1) * (cv1 - cv0 + 1) > 4000) continue;
                        int db = Mathf.FloorToInt(d / .02f);
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
                    float dd = Mathf.Abs(plA.d - plB.d); if (dd >= .02f) continue;
                    var ra = pows[plA.pow].r; var rb = pows[plB.pow].r;
                    if (ra != rb) { var la = Lod(ra); if (la && la == Lod(rb)) continue; }   // LOD0 ↔ LOD1 tego samego obiektu — nigdy razem na ekranie
                    float iu0 = Mathf.Max(A.u0, B.u0), iu1 = Mathf.Min(A.u1, B.u1), iv0 = Mathf.Max(A.v0, B.v0), iv1 = Mathf.Min(A.v1, B.v1);
                    if (iu1 <= iu0 || iv1 <= iv0) continue;
                    // para prostokątów widziana w kilku komórkach — liczona w komórce lewego dolnego rogu części wspólnej
                    if (Mathf.FloorToInt(iu0 / 4f) != cu || Mathf.FloorToInt(iv0 / 4f) != cv) continue;
                    var key = plA.pow < plB.pow ? (plA.pow, plB.pow) : (plB.pow, plA.pow);
                    if (!pary.TryGetValue(key, out var p)) pary[key] = p = new Para { a = key.Item1, b = key.Item2, plA = plA.pow == key.Item1 ? A.pl : B.pl, plB = plA.pow == key.Item1 ? B.pl : A.pl, dd = dd };
                    float pole = (iu1 - iu0) * (iv1 - iv0);
                    if (pole > p.pole * .5f || p.pole == 0) p.gdzie = Swiat(os, plA.d, (iu0 + iu1) * .5f, (iv0 + iv1) * .5f);
                    p.pole += pole; if (pole > .01f) p.dd = Mathf.Min(p.dd, dd);
                    if (pows[plA.pow].silownia && pows[plB.pow].silownia) (p.plany ?? (p.plany = new HashSet<(int, int)>())).Add(plA.pow == key.Item1 ? (A.pl, B.pl) : (B.pl, A.pl));
                }
            }
        }
        indeks.Clear();
        W.lista = pary.Values.Where(p => p.pole >= .02f).OrderByDescending(p => p.pole).ToList();
        W.trojkaty = trojkaty; W.renderery = renderery; W.kubelki = kubelki; W.prostokaty = prosty.Count; W.przerwane = przerwane;
        return W;
    }

    // ---------------- C3: siłownia — wycięcie dolnej warstwy pod górną ----------------
    static bool Przezroczysty(Material m)
    {
        if (!m) return false;
        if (m.renderQueue >= 2450) return true;
        if (m.IsKeywordEnabled("_ALPHATEST_ON") || m.IsKeywordEnabled("_ALPHABLEND_ON") || m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")) return true;
        if (m.HasProperty("_Mode") && m.GetFloat("_Mode") > .5f && m.shader && m.shader.name == "Standard") return true;
        string sh = m.shader ? m.shader.name.ToLowerInvariant() : "";
        return sh.Contains("transparent") || sh.Contains("cutout") || sh.Contains("fade");
    }
    static readonly System.Text.RegularExpressions.Regex Szklo = new System.Text.RegularExpressions.Regex(@"szk|szyb|glass|okn|window|glazing", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    static readonly Type[] Ruchome = { typeof(Rigidbody), typeof(Animation), typeof(Animator), typeof(BraxCityDoor), typeof(BraxCityElevator), typeof(BraxVessel), typeof(BraxGymPlayer), typeof(BraxAuto) };
    static readonly HashSet<Transform> ruchomeKorzenie = new HashSet<Transform>();
    static bool Ruchomy(Renderer r)
    {
        foreach (var t in Ruchome) if (r.GetComponentInParent(t)) return true;
        foreach (var k in ruchomeKorzenie) if (k && r.transform.IsChildOf(k)) return true;
        return false;
    }
    static (Renderer, Renderer) Klucz2(Renderer a, Renderer b) => a.GetInstanceID() < b.GetInstanceID() ? (a, b) : (b, a);
    const string Folder = "Assets/PlayableGenerated/KontrolaMigotania";
    static int zapisane;
    static string Bezpieczna(string s) { var sb = new StringBuilder(); foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_'); return sb.Length > 40 ? sb.ToString(0, 40) : sb.ToString(); }

    static void Silownia(Scene scena, Wykrycie W, Dictionary<Renderer, (int os, float delta)> ruch, List<Para> reszta = null)
    {
        // ruchome części siłowni bez komponentów: skrzydło drzwi wyjścia (BraxGymPlayer.door, obracane w Update) i worki
        ruchomeKorzenie.Clear();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var g in root.GetComponentsInChildren<BraxGymPlayer>(true))
            {
                if (g.door) ruchomeKorzenie.Add(g.door);
                if (g.bags != null) foreach (var b in g.bags) if (b) ruchomeKorzenie.Add(b);
            }
        var przed = W.lista.Where(p => W.pows[p.a].silownia && W.pows[p.b].silownia && WStrefie(p.gdzie)).ToList();
        int wStrefach = przed.Count;
        if (reszta != null) { var juz = new HashSet<Para>(przed); foreach (var p in reszta) if (juz.Add(p)) przed.Add(p); }
        var stoper = System.Diagnostics.Stopwatch.StartNew(); int pominieteBudzet = 0;
        var powody = new Dictionary<(Renderer, Renderer), string>();
        var zadania = new Dictionary<(Renderer, int, int, int, int), (float d, List<Vector2[]> gorne, HashSet<string> nad)>();
        int paryCiete = 0;
        foreach (var p in przed)
        {
            var A = W.pows[p.a]; var B = W.pows[p.b];
            if (p.plany == null) continue;
            string powod = null; int dodane = 0;
            foreach (var (ia, ib) in p.plany)
            {
                var pa = W.pls[ia]; var pb = W.pls[ib];
                if (pa.tri == null || pb.tri == null) continue;
                float ea = Pozycja(pa), eb = Pozycja(pb);
                Pow ciety, gorny; Pl plC, plG;
                if (Mathf.Abs(ea - eb) < .001f)
                {   // ta sama płaszczyzna: podkład (większy, np. ściana) wycięty pod nakładką (mniejszą, np. futryna, lizena)
                    bool aPodklad = A.pole >= B.pole;
                    (ciety, plC, gorny, plG) = aPodklad ? (A, pa, B, pb) : (B, pb, A, pa);
                    if (Przezroczysty(gorny.mat)) { powod = "nakładka z alfą w tej samej płaszczyźnie (" + gorny.nazwa + ")"; continue; }
                }
                else
                {   // różne płaszczyzny (< 2 cm): tylna wycięta pod przednią
                    bool aPrzod = ea > eb;
                    (gorny, plG, ciety, plC) = aPrzod ? (A, pa, B, pb) : (B, pb, A, pa);
                    if (Przezroczysty(gorny.mat))
                    {
                        if (!Przezroczysty(ciety.mat) && gorny.mat && Szklo.IsMatch(gorny.mat.name + " " + gorny.r.name)) { var t = (ciety, plC); (ciety, plC) = (gorny, plG); (gorny, plG) = t; }   // szkło przed ramą: tniemy szkło
                        else { powod = "przednia warstwa z alfą (" + gorny.nazwa + ") — tła nie wycinam (byłaby dziura)"; continue; }
                    }
                }
                if (Ruchomy(ciety.r) || Ruchomy(gorny.r)) { powod = "ruchoma część (skrzydło drzwi, worek, winda…) — bez wycinania"; continue; }
                if (ruch.ContainsKey(ciety.r) || ruch.ContainsKey(gorny.r)) { powod = "już rozsunięte przesunięciem"; continue; }
                var key = (ciety.r, ciety.sub, plC.os, plC.znak, Mathf.RoundToInt(plC.d * 1000f));
                if (!zadania.TryGetValue(key, out var z)) zadania[key] = z = (plC.d, new List<Vector2[]>(), new HashSet<string>());
                z.gorne.AddRange(plG.tri); z.nad.Add(gorny.nazwa);
                dodane++;
            }
            if (dodane > 0) paryCiete++;
            else if (powod != null) powody[Klucz2(A.r, B.r)] = powod;
        }
        if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets", "PlayableGenerated");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/PlayableGenerated", "KontrolaMigotania");
        int powierzchnie = 0, trojkaty = 0; float pole = 0; var opisy = new List<string>();
        foreach (var grupa in zadania.GroupBy(kv => kv.Key.Item1))
        {
            // budżet (OOM osiedla 2.11 — wycinanie bez limitu): najwyżej 240 s i 600 nowych siatek; reszta zostaje z logiem
            if (stoper.Elapsed.TotalSeconds > 240 || powierzchnie >= 600) { pominieteBudzet++; continue; }
            var r = grupa.Key; var mf = r ? r.GetComponent<MeshFilter>() : null; if (!mf || !mf.sharedMesh) continue;
            if (mf.sharedMesh.vertexCount > 65000 || grupa.Sum(kv => kv.Value.gorne.Count) > 6000) { pominieteBudzet++; continue; }
            var oryginal = mf.sharedMesh; var mesh = oryginal; int zmian = 0; float usuniete = 0; var nad = new HashSet<string>();
            try
            {
                foreach (var kv in grupa)
                {
                    var (_, sub, os, znak, _) = kv.Key; var z = kv.Value;
                    var nowa = BraxWycinanieSiatki.OdejmijTrojkaty(mesh, r.transform.localToWorldMatrix, sub, os, znak, z.d, .002f, z.gorne, out int ch, out float p0, out float p1);
                    if (!nowa) continue;
                    if (mesh != oryginal) UnityEngine.Object.DestroyImmediate(mesh);
                    mesh = nowa; zmian += ch; usuniete += p0 - p1; nad.UnionWith(z.nad);
                }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_SILOWNIA_MIGOTANIE — wycinanie " + r.name + ": " + e.Message); if (mesh != oryginal) UnityEngine.Object.DestroyImmediate(mesh); continue; }
            if (mesh == oryginal) continue;
            mesh.name = oryginal.name + " (bez nakładania)";
            BraxZapisAssetu.Utworz(mesh, Folder + "/silownia_" + Bezpieczna(r.name) + "_" + (zapisane++) + ".asset");
            mf.sharedMesh = mesh;                                   // tylko ten renderer (siatka bywa wspólna); kolizje bez zmian
            powierzchnie++; trojkaty += zmian; pole += usuniete;
            opisy.Add(r.name + ": wycięte " + usuniete.ToString("F2") + " m² (" + zmian + " trójkątów) pod: " + string.Join(", ", nad.Take(4)) + (nad.Count > 4 ? " …" : "") + " " + BraxKontrola.F(r.bounds.center));
        }
        Physics.SyncTransforms();
        // po: ponowne wykrycie tylko w strefie siłowni
        tylkoStrefa = true; Wykrycie po;
        try { po = Wykryj(scena, 30f); } finally { tylkoStrefa = false; }
        var zostaly = po.lista.Where(p => po.pows[p.a].silownia && po.pows[p.b].silownia && WStrefie(p.gdzie)).ToList();
        var sb = new StringBuilder("BRAXHORN_SILOWNIA_MIGOTANIE pary_przed=" + przed.Count + " pary_po=" + zostaly.Count + " pary_po_powyżej_0.1m²=" + zostaly.Count(p => p.pole > .1f)
            + " wycięte_pary=" + paryCiete + " powierzchnie=" + powierzchnie + " trójkątów=" + trojkaty + " pole_usunięte=" + pole.ToString("F2") + " m² przesunięte=0 pary_w_strefach=" + wStrefach + " pary_poza_strefami=" + (przed.Count - wStrefach) + " pominięte_budżet=" + pominieteBudzet + " czas_cięcia=" + stoper.Elapsed.TotalSeconds.ToString("F0") + " s stref_ciecia=" + strefy.Length + " (strefa siłowni " + BraxKontrola.F(strefa.min) + " … " + BraxKontrola.F(strefa.max) + ")");
        sb.Append("\n  PRZED (największe):");
        foreach (var p in przed.Take(25)) sb.Append("\n    ").Append(W.pows[p.a].nazwa + " ↔ " + W.pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie));
        sb.Append("\n  WYCIĘTE:");
        foreach (var l in opisy.Take(40)) sb.Append("\n    ").Append(l);
        sb.Append("\n  ZOSTAŁY:");
        foreach (var p in zostaly.Take(25))
            sb.Append("\n    ").Append(po.pows[p.a].nazwa + " ↔ " + po.pows[p.b].nazwa + " odstęp " + (p.dd * 100f).ToString("F1") + " cm, " + p.pole.ToString("F2") + " m² " + BraxKontrola.F(p.gdzie)
                + (powody.TryGetValue(Klucz2(po.pows[p.a].r, po.pows[p.b].r), out var pw) ? " — " + pw : ""));
        foreach (var l in opisy.Take(200)) BraxKontrola.Zapisz("migotanie siłowni: " + l);
        if (zostaly.Count(p => p.pole > .1f) > 0) Debug.LogWarning(sb.ToString()); else Debug.Log(sb.ToString());
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

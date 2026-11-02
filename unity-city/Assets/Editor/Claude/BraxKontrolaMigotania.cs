using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    sealed class Pl { public int pow, os, znak; public float d, pole; }
    sealed class Pow { public Renderer r; public int sub; public string nazwa; public float pole; }
    sealed class Para { public int a, b, plA, plB; public float pole, dd; public Vector3 gdzie; }

    public static void Wykonaj(Scene scena, BraxKontrola.Spis spis)
    {
        try { Kamera(); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola migotania — kamera: " + e.Message); }
        float t0 = BraxKontrola.Sekundy, limit = 60f;
        var pows = new List<Pow>(); var pls = new List<Pl>(); var prosty = new List<Prost>(1 << 16);
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
                    pows.Add(new Pow { r = r, sub = s, nazwa = r.name + (mat && mat.name != r.name ? " [" + mat.name + "]" : "") });
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
                        if (!plIdx.TryGetValue(kp, out int pl)) { pl = pls.Count; plIdx[kp] = pl; pls.Add(new Pl { pow = pow, os = os, znak = znak, d = d }); }
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
                    float iu0 = Mathf.Max(A.u0, B.u0), iu1 = Mathf.Min(A.u1, B.u1), iv0 = Mathf.Max(A.v0, B.v0), iv1 = Mathf.Min(A.v1, B.v1);
                    if (iu1 <= iu0 || iv1 <= iv0) continue;
                    // para prostokątów widziana w kilku komórkach — liczona w komórce lewego dolnego rogu części wspólnej
                    if (Mathf.FloorToInt(iu0 / 4f) != cu || Mathf.FloorToInt(iv0 / 4f) != cv) continue;
                    var key = plA.pow < plB.pow ? (plA.pow, plB.pow) : (plB.pow, plA.pow);
                    if (!pary.TryGetValue(key, out var p)) pary[key] = p = new Para { a = key.Item1, b = key.Item2, plA = plA.pow == key.Item1 ? A.pl : B.pl, plB = plA.pow == key.Item1 ? B.pl : A.pl, dd = dd };
                    float pole = (iu1 - iu0) * (iv1 - iv0);
                    if (pole > p.pole * .5f || p.pole == 0) p.gdzie = Swiat(os, plA.d, (iu0 + iu1) * .5f, (iv0 + iv1) * .5f);
                    p.pole += pole; if (pole > .01f) p.dd = Mathf.Min(p.dd, dd);
                }
            }
        }
        indeks.Clear();
        var lista = pary.Values.Where(p => p.pole >= .02f).OrderByDescending(p => p.pole).ToList();
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
            ", renderery " + renderery + ", trójkąty osiowe " + trojkaty + ", prostokąty " + prosty.Count + ", kubełki " + kubelki + ", czas " + (BraxKontrola.Sekundy - t0).ToString("F1") + " s" + (przerwane ? ", PRZERWANE limitem czasu" : "") + ")");
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

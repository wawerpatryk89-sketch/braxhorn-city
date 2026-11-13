using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using J = BraxKontrola.Jednostka;

// KAŻDA RZECZ SPRAWDZONA — UMIESZCZENIE I SENS (właściciel 1.11, 22:23: „czy w odpowiednim miejscu stoi… jak jest bar, czy
// jest wejście do baru”; 22:22: stół ruletki „rozwalony, byle jak leży”). Dla każdego rozpoznanego mebla / przedmiotu:
//   • prosto: oś modelu najbliższa pionowi odchylona o > 3° → wyprostowany wokół podstawy (bez rzeczy celowo pochylonych:
//     leżak, parasol, rower); stoły: dodatkowo blat (pole trójkątów zwróconych w górę w górnych 15 %) musi być poziomy;
//   • na podłodze: promień w dół (kolizje + sondy widocznych siatek, bez samego mebla) — szczelina lub zapadnięcie > 2 cm
//     (do 0.5 m) → poprawione Y; większe → zgłoszone (wisi / zapadnięty);
//   • nie w ścianie ani w innym meblu: prostopadłościan mebla (bez 5 cm przy podłodze) kontra kolizje sceny — wypchnięcie
//     w poziomie do 0.25 m, większe zgłoszone (krzesło wsunięte pod stół — dozwolone);
//   • części razem: część oddalona od reszty mebla o > 0.5 m → zgłoszona;
//   • szyldy: promień z lica szyldu (1.5 m wzdłuż normalnej) i w stronę oczu przechodnia 6 m przed szyldem (pierwsze 2.5 m)
//     nie może trafić w widoczną siatkę (daszek, markiza) — zgłoszone;
//   • daszki: płyty „sufit dla kamery” (warstwa 29) o krótszym boku < 2.5 m nad otwartą przestrzenią (≥ 3 z 4 kierunków bez
//     ściany w 4 m) — usunięte (kamera nie odbija się od daszków nad wejściami); korytarze zostają;
//   • lady (bar, recepcja, kasa): za ladą ≥ 0.6 m miejsca dla obsługi i przejście ≥ 0.8 m przy jednym z końców — brak zgłoszony.
public static class BraxKontrolaUmieszczenia
{
    static int poprawione, zgloszone;
    static readonly List<string> lista = new List<string>();
    static void Popr(string s) { poprawione++; if (lista.Count < 400) lista.Add("POPRAWIONE " + s); BraxKontrola.Zapisz("umieszczenie: POPRAWIONE " + s); }
    static void Zgl(string s) { zgloszone++; if (lista.Count < 400) lista.Add("ZGŁOSZONE " + s); BraxKontrola.Zapisz("umieszczenie: ZGŁOSZONE " + s); }
    static bool Ruszac(J j) => j.typ != null && !j.typ.raport && !j.chroniony;

    // ---------------- prostowanie (przed skalą) ----------------
    public static void Prostuj(BraxKontrola.Spis spis)
    {
        poprawione = 0; zgloszone = 0; lista.Clear();
        foreach (var j in spis.jednostki)
        {
            if (j.typ == null || j.typ.pochylony || j.typ.wisi) continue;
            try
            {
                var t = j.t; Vector3 best = t.up; float bd = -2f;
                foreach (var a in new[] { t.up, -t.up, t.right, -t.right, t.forward, -t.forward }) { float d = Vector3.Dot(a, Vector3.up); if (d > bd) { bd = d; best = a; } }
                float kat = Vector3.Angle(best, Vector3.up);
                if (kat > 3f && kat <= 30f)
                {
                    if (!Ruszac(j)) { Zgl(j.typ.nazwa + " przechylony o " + kat.ToString("F0") + "° (tylko raport) " + j.opis + " " + BraxKontrola.F(j.Podstawa)); continue; }
                    j.Obroc(Quaternion.FromToRotation(best, Vector3.up), j.Podstawa);
                    Popr(j.typ.nazwa + " wyprostowany (przechył " + kat.ToString("F0") + "°) " + j.opis + " " + BraxKontrola.F(j.Podstawa));
                    continue;
                }
                if (kat > 30f && kat < 87f) { Zgl(j.typ.nazwa + " przewrócony? przechył " + kat.ToString("F0") + "° " + j.opis + " " + BraxKontrola.F(j.Podstawa)); continue; }
                if (j.typ.stol && Ruszac(j))
                {
                    var n = NormalnaBlatu(j);
                    float kb = n == Vector3.zero ? 0 : Vector3.Angle(n, Vector3.up);
                    if (kb > 3f && kb <= 20f)
                    {
                        j.Obroc(Quaternion.FromToRotation(n, Vector3.up), j.Podstawa);
                        Popr(j.typ.nazwa + " — blat wypoziomowany (był przechył " + kb.ToString("F0") + "°) " + j.opis + " " + BraxKontrola.F(j.Podstawa));
                    }
                }
            }
            catch (Exception e) { BraxKontrola.Zapisz("prostowanie: błąd " + j.opis + ": " + e.Message); }
        }
        Physics.SyncTransforms();
    }

    // średnia normalna (ważona polem) trójkątów zwróconych w górę w górnych 15 % wysokości mebla
    static Vector3 NormalnaBlatu(J j)
    {
        Vector3 s = Vector3.zero; float y1 = j.b.max.y, y0 = y1 - Mathf.Max(.04f, j.b.size.y * .15f);
        foreach (var r in j.rs)
        {
            var mf = r ? r.GetComponent<MeshFilter>() : null;
            if (!mf || !BraxKontrola.Dane(mf.sharedMesh, out var v, out var t) || t.Length > 600000) continue;
            var m = r.transform.localToWorldMatrix;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
                if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) < y0) continue;
                var n = Vector3.Cross(b - a, c - a); if (n.y < 0) continue;
                if (n.sqrMagnitude < 1e-10f || n.y / n.magnitude < .8f) continue;
                s += n;                                                    // długość = 2 × pole → ważenie polem
            }
        }
        return s.sqrMagnitude < 1e-8f ? Vector3.zero : s.normalized;
    }

    // ---------------- reszta (po skali) ----------------
    public static void Wykonaj(Scene scena, BraxKontrola.Spis spis)
    {
        int podloga = 0, sciany = 0, czesci = 0;
        Physics.SyncTransforms();
        foreach (var j in spis.jednostki) { if (j.typ == null) continue; try { if (Podloga(j)) podloga++; } catch (Exception e) { BraxKontrola.Zapisz("podłoga: błąd " + j.opis + ": " + e.Message); } }
        Physics.SyncTransforms();
        var tmp = new GameObject("BRAXHORN kontrola: pudełko próbne") { hideFlags = HideFlags.DontSave, layer = 2 };
        var box = tmp.AddComponent<BoxCollider>();
        try { foreach (var j in spis.jednostki) { if (j.typ == null) continue; try { if (Sciany(j, spis, box)) sciany++; } catch (Exception e) { BraxKontrola.Zapisz("ściany: błąd " + j.opis + ": " + e.Message); } } }
        finally { Object.DestroyImmediate(tmp); }
        Physics.SyncTransforms();
        foreach (var j in spis.jednostki) { try { if (Czesci(j)) czesci++; } catch { } }
        int szyldy = 0, zaslonieteSz = 0, daszki = 0, lady = 0, ladyBez = 0;
        try { Szyldy(scena, out szyldy, out zaslonieteSz); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola szyldów: " + e); }
        try { daszki = Daszki(scena); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola daszków: " + e); }
        try { Lady(scena, spis, out lady, out ladyBez); } catch (Exception e) { Debug.LogWarning("DO POPRAWKI: kontrola lad: " + e); }
        Physics.SyncTransforms();
        var sb = new StringBuilder("BRAXHORN_UMIESZCZENIE " + BraxKontrola.Obszar + ": poprawione=" + poprawione + " zgloszone=" + zgloszone +
            " (podłoga/wysokość " + podloga + ", ściany i meble " + sciany + ", części odjechane " + czesci + ", szyldy sprawdzone " + szyldy + " zasłonięte " + zaslonieteSz +
            ", daszki bez płyty kamery " + daszki + ", lady " + lady + " bez przejścia " + ladyBez + ")");
        foreach (var l in lista.Take(60)) sb.Append("\n  ").Append(l);
        if (lista.Count > 60) sb.Append("\n  … (+").Append(lista.Count - 60).Append(" w city-review/kontrola-").Append(BraxKontrola.Obszar).Append(".json)");
        Debug.Log(sb.ToString());
    }

    // ---------- podłoga ----------
    static bool Podloga(J j)
    {
        if (j.typ.wisi || j.typ.nazwa == "drzwi") return false;
        j.Odswiez();
        int maska = BraxKontrola.MaskaStala | BraxKontrola.MaskaSond;
        float start = Mathf.Min(.5f, j.b.size.y * .5f);
        var c = new Vector3(j.srodekXZ.x, j.b.min.y + start, j.srodekXZ.y);
        float? y = null;
        if (BraxKontrola.Promien(c, Vector3.down, start + 1.2f, maska, j, out var h)) y = h.point.y;
        else
        {   // środek nad otworem (np. stół z dziurą na parasol) — najniższe z 4 narożników wsuniętych o 30 %
            var q = Quaternion.Euler(0, j.kat, 0);
            foreach (var (u, v) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
            {
                var p = c + q * new Vector3(u * j.dl * .35f, 0, v * j.sz * .35f);
                if (BraxKontrola.Promien(p, Vector3.down, start + 1.2f, maska, j, out var h2) && (y == null || h2.point.y < y.Value)) y = h2.point.y;
            }
        }
        if (y == null) { Zgl(j.typ.nazwa + " — brak podłogi pod spodem (do " + (start + 1.2f).ToString("F1") + " m) " + j.opis + " " + BraxKontrola.F(j.Podstawa)); return true; }
        float gap = j.b.min.y - y.Value;
        if (Mathf.Abs(gap) <= .02f) return false;
        if (!Ruszac(j)) { Zgl(j.typ.nazwa + (gap > 0 ? " wisi " : " zapadnięty ") + Mathf.Abs(gap * 100).ToString("F0") + " cm (tylko raport) " + j.opis + " " + BraxKontrola.F(j.Podstawa)); return true; }
        if (gap > .5f || gap < -.5f) { Zgl(j.typ.nazwa + (gap > 0 ? " wisi " : " zapadnięty ") + Mathf.Abs(gap).ToString("F2") + " m nad/pod podłogą — za dużo do poprawki " + j.opis + " " + BraxKontrola.F(j.Podstawa)); return true; }
        j.Przesun(new Vector3(0, -gap, 0)); j.Odswiez();
        Popr(j.typ.nazwa + (gap > 0 ? " opuszczony na podłogę o " : " podniesiony z podłogi o ") + Mathf.Abs(gap * 100).ToString("F0") + " cm " + j.opis + " " + BraxKontrola.F(j.Podstawa));
        return true;
    }

    // ---------- ściany i inne meble ----------
    static readonly Collider[] bufor = new Collider[64];
    static bool Sciany(J j, BraxKontrola.Spis spis, BoxCollider box)
    {
        if (j.typ.nazwa == "drzwi" || j.typ.wisi) return false;
        j.Odswiez();
        float hy = (j.b.size.y - .05f) * .5f - .02f; if (hy < .03f || j.dl < .1f) return false;
        var rot = Quaternion.Euler(0, j.kat, 0);
        var half = new Vector3(Mathf.Max(.02f, j.dl * .5f - .02f), hy, Mathf.Max(.02f, j.sz * .5f - .02f));
        bool zgloszony = false;
        for (int proba = 0; proba < 3; proba++)
        {
            var c = new Vector3(j.srodekXZ.x, j.b.min.y + .05f + hy + .02f, j.srodekXZ.y);
            box.size = half * 2f;
            int n = Physics.OverlapBoxNonAlloc(c, half, bufor, rot, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore);
            Vector3 push = Vector3.zero; string w = null; float naj = 0;
            for (int i = 0; i < n; i++)
            {
                var o = bufor[i];
                if (!o || j.kol.Contains(o) || o.transform.IsChildOf(j.t) || o == box) continue;
                var ob = o.bounds;
                if (ob.max.y <= j.b.min.y + .07f) continue;                                  // podłoga
                if (ob.size.x > 300f || ob.size.z > 300f) continue;
                spis.poKolizji.TryGetValue(o, out var inny);
                if (inny != null && Dozwolone(j, inny)) continue;
                if (!Physics.ComputePenetration(box, c, rot, o, o.transform.position, o.transform.rotation, out var dir, out var dist)) continue;
                if (dist < .015f) continue;
                if (dir.y > .7f) continue;                                                     // wypycha w górę = stoi na czymś
                var hz = new Vector3(dir.x, 0, dir.z); if (hz.sqrMagnitude < 1e-4f) continue;
                push += hz.normalized * dist;
                if (dist > naj) { naj = dist; w = inny != null ? "mebel " + inny.opis : "ściana/kolizja " + o.name; }
            }
            if (push == Vector3.zero) { if (proba > 0) { Popr(j.typ.nazwa + " odsunięty od: " + ostatnie + " " + j.opis + " " + BraxKontrola.F(j.Podstawa)); return true; } return zgloszony; }
            if (!Ruszac(j) || push.magnitude > .25f)
            {
                Zgl(j.typ.nazwa + " wchodzi w " + w + " na " + (push.magnitude * 100).ToString("F0") + " cm" + (Ruszac(j) ? " (za dużo do przesunięcia)" : " (tylko raport)") + " " + j.opis + " " + BraxKontrola.F(j.Podstawa));
                return true;
            }
            ostatnie = w;
            j.Przesun(push + push.normalized * .01f); j.Odswiez();
            Physics.SyncTransforms();
        }
        Zgl(j.typ.nazwa + " nadal wchodzi w " + ostatnie + " po 3 próbach " + j.opis + " " + BraxKontrola.F(j.Podstawa));
        return true;
    }
    static string ostatnie;
    // krzesło / sofa / hoker wsunięte pod stół; rzeczy tego samego zestawu; rzeczy na meblu
    static bool Dozwolone(J a, J b)
    {
        if (a.typ != null && b.typ != null && ((a.typ.siedzi && b.typ.stol) || (b.typ.siedzi && a.typ.stol) || (a.typ.lada && b.typ.nazwa == "hoker") || (b.typ.lada && a.typ.nazwa == "hoker"))) return true;
        if (a.t.parent && a.t.parent == b.t.parent && a.t.parent.GetComponent<LODGroup>()) return true;
        if ((a.pasazerowie != null && a.pasazerowie.Contains(b.t)) || (b.pasazerowie != null && b.pasazerowie.Contains(a.t))) return true;
        return false;
    }

    // ---------- części razem ----------
    static bool Czesci(J j)
    {
        if (j.rs.Count < 2 || j.rs.Count > 64) return false;
        var bs = j.rs.Where(r => r).Select(r => r.bounds).ToList();
        for (int i = 0; i < bs.Count; i++)
        {
            bool any = false; Bounds reszta = default;
            for (int k = 0; k < bs.Count; k++) if (k != i) { if (!any) { reszta = bs[k]; any = true; } else reszta.Encapsulate(bs[k]); }
            if (!any) continue;
            float d = Odstep(bs[i], reszta);
            if (d > .5f)
            {
                Zgl((j.typ != null ? j.typ.nazwa : "przedmiot") + " — część „" + j.rs[i].name + "” odjechała od reszty o " + d.ToString("F2") + " m " + j.opis + " " + BraxKontrola.F(bs[i].center));
                return true;
            }
        }
        return false;
    }
    static float Odstep(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x)), dy = Mathf.Max(0, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y)), dz = Mathf.Max(0, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // ---------- szyldy ----------
    static readonly Regex Szyld = new Regex(@"\b(szyld\w*|sign|signs|signage|tablic\w*|napis\w*|logo\w*|neon\w*|baner\w*|banner\w*|kaseton\w*|reklam\w*|billboard\w*)\b");
    static readonly Regex NieSzyld = new Regex(@"\b(rama|ramy|frame|tablica menu|kartk\w*|plyty|wet floor|sygnaliz\w*|signal\w*|design\w*)\b");
    static void Szyldy(Scene scena, out int sprawdzone, out int zasloniete)
    {
        sprawdzone = 0; zasloniete = 0;
        foreach (var root in scena.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!BraxKontrola.Widoczny(r)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                string n = BraxKontrola.Norm(r.name) + BraxKontrola.Norm(r.sharedMaterial ? r.sharedMaterial.name : "");
                if (!Szyld.IsMatch(n) || NieSzyld.IsMatch(n)) continue;
                if (!BraxKontrola.Dane(mf.sharedMesh, out var v, out var t) || t.Length > 60000) continue;
                var m = r.transform.localToWorldMatrix;
                // lica: trójkąty pionowe ≥ 0.02 m², grupowane po komórce 0.5 m i kierunku
                var lica = new Dictionary<(int, int, int, int), (Vector3 s, Vector3 n, float pole)>();
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[t[i]]), b = m.MultiplyPoint3x4(v[t[i + 1]]), c = m.MultiplyPoint3x4(v[t[i + 2]]);
                    var nn = Vector3.Cross(b - a, c - a); float p2 = nn.magnitude; if (p2 < .04f) continue;
                    nn /= p2; if (Mathf.Abs(nn.y) > .5f) continue;
                    // W pierzei front lokali jest od ulicy (−Z); tył płaskiego szyldu przylega do muru.
                    if (BraxKontrola.Obszar == "osiedle" && (n.Contains(" pas szyldowy lokalu ") || n.Contains(" szyld nutrition ")) && nn.z > .9f) continue;
                    var cc = (a + b + c) / 3f;
                    var key = (Mathf.FloorToInt(cc.x * 2f), Mathf.FloorToInt(cc.y * 2f), Mathf.FloorToInt(cc.z * 2f), Mathf.RoundToInt(Mathf.Atan2(nn.x, nn.z) * 4f / Mathf.PI));
                    lica.TryGetValue(key, out var acc); acc.s += cc * p2; acc.n += nn * p2; acc.pole += p2; lica[key] = acc;
                }
                // komórki tego samego lica w promieniu 1.5 m = jeden szyld (jedno sprawdzenie, jedno zgłoszenie)
                var byly = new List<(Vector3 c, Vector3 n)>();
                foreach (var kv in lica.OrderByDescending(q => q.Value.pole))
                {
                    var c0 = kv.Value.s / kv.Value.pole; var nrm = kv.Value.n.normalized;
                    if (byly.Any(q => (q.c - c0).sqrMagnitude < 2.25f && Vector3.Dot(q.n, nrm) > .9f)) continue;
                    byly.Add((c0, nrm)); sprawdzone++;
                    var z = c0 + nrm * .03f; string co = null, jak = null;
                    if (Trafienie(z, nrm, 1.5f, r, out var h1)) { co = h1; jak = "w 1.5 m przed licem"; }
                    else
                    {   // oczy przechodnia po drugiej stronie ulicy (12 m przed szyldem), sprawdzane pierwsze 2.5 m od lica
                        var widz = c0 + nrm * 12f; float ziemia = c0.y - 3f;
                        if (Physics.Raycast(new Vector3(widz.x, c0.y + 1f, widz.z), Vector3.down, out var g, 40f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) ziemia = g.point.y;
                        var oko = new Vector3(widz.x, ziemia + 1.65f * BraxKontrola.K, widz.z);
                        var d = (oko - z).normalized;
                        if (Trafienie(z, d, 2.5f, r, out var h2)) { co = h2; jak = "od strony przechodnia (12 m, oczy " + (1.65f * BraxKontrola.K).ToString("F2") + " m)"; }
                    }
                    if (co != null)
                    {
                        zasloniete++;
                        Zgl("szyld „" + r.name + "” zasłonięty przez „" + co + "” " + jak + " " + BraxKontrola.F(c0) + " lico w stronę " + BraxKontrola.F(nrm));
                    }
                }
            }
    }
    static readonly RaycastHit[] bufR = new RaycastHit[32];
    static bool Trafienie(Vector3 z, Vector3 d, float dl, Renderer wlasny, out string co)
    {
        co = null;
        int n = Physics.RaycastNonAlloc(z, d, bufR, dl, BraxKontrola.MaskaSond, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var r = BraxKontrola.Sondy.Czyj(bufR[i].collider);
            if (!r || r == wlasny) continue;
            string nn = BraxKontrola.Norm(r.name) + BraxKontrola.Norm(r.sharedMaterial ? r.sharedMaterial.name : "");
            if (Szyld.IsMatch(nn) || nn.Contains(" rama ") || nn.Contains(" frame ")) continue;    // rama / tablica pod szyldem
            if (bufR[i].distance < best) { best = bufR[i].distance; co = r.name; }
        }
        return co != null;
    }

    // ---------- daszki: płyty kamery nad otwartą przestrzenią ----------
    static int Daszki(Scene scena)
    {
        int usuniete = 0;
        var doUsuniecia = new List<GameObject>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<BoxCollider>(false))
            {
                if (c.gameObject.layer != BraxKontrola.WarstwaKamery || c.name != "sufit dla kamery") continue;
                var b = c.bounds; if (Mathf.Min(b.size.x, b.size.z) >= 2.5f) continue;
                // pod płytą: na wysokości 1.5 m nad ziemią, 4 kierunki po 4 m — ile ścian
                float ziemia = b.min.y - 3f;
                if (Physics.Raycast(new Vector3(b.center.x, b.min.y - .1f, b.center.z), Vector3.down, out var g, 30f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) ziemia = g.point.y;
                var p = new Vector3(b.center.x, Mathf.Min(ziemia + 1.5f, b.min.y - .2f), b.center.z);
                int otwarte = 0;
                foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                    if (!Physics.Raycast(p, d, 4f, BraxKontrola.MaskaStala | BraxKontrola.MaskaSond, QueryTriggerInteraction.Ignore)) otwarte++;
                if (otwarte >= 3) doUsuniecia.Add(c.gameObject);
            }
        foreach (var g in doUsuniecia)
        {
            var b = g.GetComponent<BoxCollider>().bounds;
            Popr("daszek " + b.size.x.ToString("F1") + " × " + b.size.z.ToString("F1") + " m na " + b.min.y.ToString("F1") + " m — bez płyty kolizji dla kamery " + BraxKontrola.F(b.center));
            Object.DestroyImmediate(g); usuniete++;
        }
        return usuniete;
    }

    // ---------- lady: przejście za ladę ----------
    static readonly Regex Lokal = new Regex(@"\b(recepcj\w*|reception|bar|bary|baru|pub|kawiarn\w*|cafe|kafej\w*|shake|kasa|kasy|kasyn\w*|przedsion\w*|sklep\w*|butik\w*|market\w*|nutrition|lada|lady|dyzur\w*|komisariat\w*|klub\w*|dyskotek\w*|apteka|piekarn\w*|warzywn\w*|shop|store)\b");
    static void Lady(Scene scena, BraxKontrola.Spis spis, out int lady, out int bez)
    {
        lady = 0; bez = 0;
        var kandydaci = new List<(Vector3 c, Quaternion q, Vector3 roz, string opis)>();
        foreach (var j in spis.jednostki) if (j.typ != null && j.typ.lada) { j.Odswiez(); kandydaci.Add((new Vector3(j.srodekXZ.x, j.b.center.y, j.srodekXZ.y), Quaternion.Euler(0, j.kat, 0), new Vector3(j.dl, j.b.size.y, j.sz), j.opis)); }
        // lady z generatora (kolizje pudełkowe): blat 0.95–1.25 m nad podłogą, długość ≥ 1.4 m, głębokość 0.35–1.2 m, w obrysie lokalu
        var lokale = new List<(Bounds b, string nazwa)>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(false))
            {
                if (t.childCount == 0 || !Lokal.IsMatch(BraxKontrola.Norm(t.name))) continue;
                bool any = false; Bounds b = default;
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(false)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (any && b.size.x < 120f && b.size.z < 120f) lokale.Add((b, t.name));
            }
        foreach (var root in scena.GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<BoxCollider>(false))
            {
                if (c.isTrigger || c.gameObject.layer == BraxKontrola.WarstwaKamery || c.gameObject.layer == BraxKontrola.WarstwaSondy || spis.poKolizji.ContainsKey(c)) continue;
                var s = Vector3.Scale(c.size, c.transform.lossyScale); s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                float dl = Mathf.Max(s.x, s.z), gl = Mathf.Min(s.x, s.z);
                if (dl < 1.4f || dl > 16f || gl < .35f || gl > 1.2f) continue;
                var b = c.bounds;
                float k = BraxKontrola.K, wys = b.max.y - Podloze(b);
                if (wys < .95f * Mathf.Min(1f, k) || wys > 1.25f * Mathf.Max(1f, k)) continue;
                if (Mathf.Abs(Vector3.Dot(c.transform.up, Vector3.up)) < .99f) continue;
                var lok = lokale.FirstOrDefault(l => l.b.Contains(b.center));
                if (lok.nazwa == null) continue;
                var q = c.transform.rotation; if (s.z > s.x) q *= Quaternion.Euler(0, 90, 0);
                kandydaci.Add((c.transform.TransformPoint(c.center), Quaternion.Euler(0, q.eulerAngles.y, 0), new Vector3(dl, s.y, gl), "kolizja lady w „" + lok.nazwa + "”"));
            }
        // ta sama lada z modelu i z kolizji — raz
        var byly = new List<Vector3>();
        foreach (var (c, q, roz, opis) in kandydaci)
        {
            if (byly.Any(p => (p - c).sqrMagnitude < .5f)) continue; byly.Add(c);
            lady++;
            if (!Przejscie(c, q, roz, out string why)) { bez++; Zgl("lada bez przejścia: " + why + " — " + opis + " " + BraxKontrola.F(c)); }
        }
    }
    static float Podloze(Bounds b)
    {
        var o = new Vector3(b.center.x, b.min.y + .05f, b.center.z);
        // podłoga obok lady (pod nią zwykle nic nie ma oprócz samej lady)
        foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            var p = o + d * (Mathf.Max(b.extents.x, b.extents.z) + .4f) + Vector3.up * .5f;
            if (Physics.Raycast(p, Vector3.down, out var h, 2.5f, BraxKontrola.MaskaStala, QueryTriggerInteraction.Ignore)) return h.point.y;
        }
        return b.min.y;
    }
    // za ladą (strona bliżej ściany) ≥ 0.6 m, przejście ≥ 0.8 m przy jednym z końców (kapsuła r 0.4 mieści się na końcu lady)
    static bool Przejscie(Vector3 c, Quaternion q, Vector3 roz, out string why)
    {
        why = null; int maska = BraxKontrola.MaskaStala;
        Vector3 u = q * Vector3.right, v = q * Vector3.forward; float L = roz.x * .5f, G = roz.z * .5f;
        float podl = c.y - roz.y * .5f;
        float y = podl + 1.0f;
        float Odl(Vector3 dir) { float best = 99f; foreach (float s in new[] { -.6f, 0f, .6f }) { var o = new Vector3(c.x, y, c.z) + u * (L * s) + dir * (G + .02f); if (Physics.Raycast(o, dir, out var h, 6f, maska, QueryTriggerInteraction.Ignore)) best = Mathf.Min(best, h.distance); else best = Mathf.Min(best, 6f); } return best; }
        float dp = Odl(v), dt = Odl(-v);
        if (dp >= 3f && dt >= 3f) return true;                      // lada wolnostojąca (wyspa) — dostęp z każdej strony
        var tyl = dp < dt ? v : -v; float miejsce = Mathf.Min(dp, dt);
        if (miejsce < .6f) { why = "za ladą tylko " + (miejsce * 100).ToString("F0") + " cm (potrzeba ≥ 60)"; return false; }
        float r = .4f;
        foreach (float s in new[] { -1f, 1f })
        {
            // koniec lady: od strony obsługi do strony klienta (pozycja przy końcu, kapsuła 0.8 m szerokości)
            var kon = new Vector3(c.x, podl, c.z) + u * s * (L + r + .02f);
            bool wolne = true;
            foreach (float w in new[] { -1f, 0f, 1f })
            {
                var p = kon + tyl * (w * (G + Mathf.Min(miejsce, 1f) * .5f));
                if (Physics.CheckCapsule(p + Vector3.up * (r + .3f), p + Vector3.up * 1.6f, r, maska, QueryTriggerInteraction.Ignore)) { wolne = false; break; }
            }
            if (wolne) return true;
        }
        why = "oba końce zastawione (brak przejścia ≥ 80 cm)";
        return false;
    }
}

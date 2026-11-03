using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PKT 4 dopisku 23.11 — kontrole globalne miasta z naprawą (każda w osobnym try, „DO POPRAWKI” w logu):
//  • NAPISY NACHODZĄ (pkt 3): napisy TextMesh / „Napis …” nachodzące na siebie (ta sama płaszczyzna ± 0.3 m, wspólne pole
//    > 20 % mniejszego) — mniejszy (albo duplikat) usunięty. Log BRAXHORN_NAPISY_KOLIZJE przed=… po=….
//  • PRZESZKODY PRZED DRZWIAMI (pkt 2): drzewo / donica / ławka / hydrant / słup / kosz w pasie przed drzwiami
//    (oś ± (szer./2 + 1.0 m·k), głębokość 2.5 m·k, obie strony) — usunięte (z kolizjami). Log BRAXHORN_DRZEWA_DRZWI.
//  • MEBLE W POWIETRZU (pkt 4 / 31 b): stół / blat / krzesło / ławka / sofa / biurko / parasol, którego spód jest > 5 cm nad
//    podłożem i nic pod nim nie ma (5 promieni) — usunięty. Log BRAXHORN_MEBLE_WISZACE.
//  • LADY BEZ KOLIZJI (pkt 4, recepcja BRAXHORN TOWER — gracz przechodził przez ladę): lada / recepcja / kontuar bez
//    żadnej kolizji dostaje bryłę kolizji z obrysu. Log BRAXHORN_LADY_KOLIZJE.
public static class BraxKontrolaMiastaDetale
{
    static float K => BraxClaudeOsiedle.K;

    public static void Wykonaj(Transform root)
    {
        Physics.SyncTransforms();
        try { Napisy(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_NAPISY_KOLIZJE DO POPRAWKI " + e.Message); }
        try { PrzedDrzwiami(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_DRZEWA_DRZWI DO POPRAWKI " + e.Message); }
        try { Wiszace(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_MEBLE_WISZACE DO POPRAWKI " + e.Message); }
        try { LampyBezSciany(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_LAMPY_BEZ_SCIANY DO POPRAWKI " + e.Message); }
        try { Lady(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_LADY_KOLIZJE DO POPRAWKI " + e.Message); }
        try { Hydranty(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_HYDRANTY DO POPRAWKI " + e.Message); }
        Physics.SyncTransforms();
    }

    static Bounds Obrys(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static string Nazwa(Transform t) => t.name.ToLowerInvariant();

    // ---------------- napisy ----------------
    static void Napisy(Transform root)
    {
        var napisy = new List<(GameObject g, Bounds b, float pole, string tekst)>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var tm = t.GetComponent<TextMesh>();
            bool jest = tm || t.name.StartsWith("Napis ", StringComparison.Ordinal) || t.name.StartsWith("napis ", StringComparison.Ordinal);
            if (!jest || !t.GetComponentInChildren<Renderer>(true)) continue;
            var b = Obrys(t.gameObject); var s = b.size;
            float pole = Mathf.Max(s.x * s.y, Mathf.Max(s.z * s.y, s.x * s.z));
            if (pole < 1e-4f) continue;
            napisy.Add((t.gameObject, b, pole, tm ? tm.text : t.name));
        }
        var usun = new HashSet<GameObject>(); var opis = new List<string>(); int przed = 0;
        for (int i = 0; i < napisy.Count; i++)
            for (int j = i + 1; j < napisy.Count; j++)
            {
                var A = napisy[i]; var B = napisy[j];
                if (usun.Contains(A.g) || usun.Contains(B.g)) continue;
                if (A.g.transform.IsChildOf(B.g.transform) || B.g.transform.IsChildOf(A.g.transform)) continue;
                // oś najcieńsza = normalna napisu; oba w tej samej płaszczyźnie (± 0.3 m) i wspólne pole na niej > 20 % mniejszego
                int osA = A.b.size.x <= A.b.size.z ? 0 : 2, osB = B.b.size.x <= B.b.size.z ? 0 : 2;
                if (osA != osB) continue;
                float dA = osA == 0 ? A.b.center.x : A.b.center.z, dB = osA == 0 ? B.b.center.x : B.b.center.z;
                if (Mathf.Abs(dA - dB) > .3f) continue;
                float ua0 = osA == 0 ? A.b.min.z : A.b.min.x, ua1 = osA == 0 ? A.b.max.z : A.b.max.x, ub0 = osA == 0 ? B.b.min.z : B.b.min.x, ub1 = osA == 0 ? B.b.max.z : B.b.max.x;
                float du = Mathf.Min(ua1, ub1) - Mathf.Max(ua0, ub0), dy = Mathf.Min(A.b.max.y, B.b.max.y) - Mathf.Max(A.b.min.y, B.b.min.y);
                if (du <= 0f || dy <= 0f) continue;
                float pa = (ua1 - ua0) * A.b.size.y, pb = (ub1 - ub0) * B.b.size.y;
                if (du * dy < .2f * Mathf.Min(pa, pb)) continue;
                przed++;
                var mniejszy = A.pole <= B.pole ? A : B;
                usun.Add(mniejszy.g); opis.Add("„" + A.tekst + "” × „" + B.tekst + "” → usunięty „" + mniejszy.tekst + "” @" + mniejszy.b.center.ToString("F0"));
            }
        if (usun.Count > 40) { Debug.LogWarning("BRAXHORN_DETALE_BEZPIECZNIK za dużo do usunięcia (" + usun.Count + ") — podejrzenie błędu wykrywania, NIC nie usuwam, tylko lista: " + string.Join("; ", usun.Take(15).Select(g => g ? g.name : "?"))); usun.Clear(); }   // prowadzący (audyt 3.11)
        foreach (var g in usun) if (g) UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("BRAXHORN_NAPISY_KOLIZJE przed=" + przed + " po=0 usuniete=" + usun.Count + " napisow=" + napisy.Count + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(25)) + "]" : "")
            + " (napisy 3D wtopione w elewacje — Napis3D — poza tą kontrolą)");
    }

    // ---------------- przeszkody przed drzwiami ----------------
    static readonly string[] Przeszkody = { "drzew", "tree", "donic", "planter", "ławk", "lawk", "bench", "hydrant", "słup ", "slup ", "latarn", "lamp post", "streetlight" };
    static readonly string[] NiePrzeszkody = { "napis", "szyld", "drzwi", "door", "znak", "wnętrz", "fasad", "elewac", "budyn", "kamieni", "teren", "chodnik", "trawnik" };

    static void PrzedDrzwiami(Transform root)
    {
        var drzwi = new List<(Vector3 c, Vector3 n, float w, string nazwa)>();
        foreach (var d in root.GetComponentsInChildren<BraxCityDoor>(true))
        {
            if (d.leaves == null || d.leaves.Length == 0) continue;
            var bb = new Bounds(); bool jest = false;
            foreach (var l in d.leaves) if (l) { var b = Obrys(l.gameObject); if (!jest) { bb = b; jest = true; } else bb.Encapsulate(b); }
            if (!jest) continue;
            var n = bb.size.x < bb.size.z ? Vector3.right : Vector3.forward;   // normalna drzwi = oś najcieńsza
            drzwi.Add((bb.center, n, Mathf.Max(bb.size.x, bb.size.z), string.IsNullOrEmpty(d.label) ? d.name : d.label));
        }
        foreach (var d in root.GetComponentsInChildren<BraxDrzwiPrzesuwne>(true))
        {
            var bb = Obrys(d.gameObject); if (bb.size.sqrMagnitude < 1e-4f) continue;
            var n = bb.size.x < bb.size.z ? Vector3.right : Vector3.forward;
            drzwi.Add((bb.center, n, Mathf.Max(bb.size.x, bb.size.z), d.name));
        }
        float glab = 2.5f * K, bok = 1.0f * K;
        var usun = new HashSet<GameObject>(); var opis = new List<string>();
        var kandydaci = root.GetComponentsInChildren<Transform>(true).Where(t =>
        {
            string n = Nazwa(t);
            return Przeszkody.Any(n.Contains) && !NiePrzeszkody.Any(n.Contains) && t.GetComponentInChildren<Renderer>(true)
                && !t.GetComponentInChildren<SkinnedMeshRenderer>(true) && !t.GetComponentInChildren<BraxCityDoor>(true) && !t.GetComponentInChildren<BraxWinda>(true) && !t.GetComponentInParent<BraxGymPlayer>();
        }).ToList();
        // tylko najwyższy kandydat w gałęzi (drzewo z kratą jako jedna rzecz)
        var zbior = new HashSet<Transform>(kandydaci);
        kandydaci = kandydaci.Where(t => { for (var q = t.parent; q; q = q.parent) if (zbior.Contains(q)) return false; return true; }).ToList();
        foreach (var t in kandydaci)
        {
            var b = Obrys(t.gameObject); if (b.size.x > 4f || b.size.z > 4f || b.size.y < .3f) continue;   // duże zespoły / płaskie kraty — nie
            var p = b.center;
            foreach (var d in drzwi)
            {
                var wzd = Vector3.Cross(Vector3.up, d.n);
                float u = Vector3.Dot(p - d.c, wzd), v = Vector3.Dot(p - d.c, d.n);
                if (Mathf.Abs(u) > d.w * .5f + bok || Mathf.Abs(v) > glab || Mathf.Abs(v) < .05f) continue;
                if (Mathf.Abs(b.min.y - (d.c.y - 1.2f)) > 1.5f) continue;                               // inny poziom
                usun.Add(t.gameObject); opis.Add(t.name + " przed „" + d.nazwa + "” @" + p.ToString("F0")); break;
            }
        }
        if (usun.Count > 40) { Debug.LogWarning("BRAXHORN_DETALE_BEZPIECZNIK za dużo do usunięcia (" + usun.Count + ") — podejrzenie błędu wykrywania, NIC nie usuwam, tylko lista: " + string.Join("; ", usun.Take(15).Select(g => g ? g.name : "?"))); usun.Clear(); }   // prowadzący (audyt 3.11)
        foreach (var g in usun) if (g) UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("BRAXHORN_DRZEWA_DRZWI przeniesione=0 usuniete=" + usun.Count + " drzwi=" + drzwi.Count + " (cel 0 kolizji)" + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(30)) + "]" : ""));
    }

    // ---------------- meble w powietrzu ----------------
    static readonly string[] Meble = { "stół", "stol", "stolik", "table", "blat", "krzes", "chair", "ławk", "lawk", "bench", "sofa", "kanap", "biurk", "desk", "parasol", "umbrella", "hoker", "stool", "fotel" };
    static readonly string[] NieMeble = { "półk", "polk", "shelf", "kinkiet", "lamp", "obraz", "tablic", "szafka wisz", "tv", "telewiz", "nadwozie", "auto", "kierowc", "fotel kierowcy", "fotel pasażera", "zawias", "wisz" };

    // właściciel 3.11 10:44 (zdjęcie): latarnia ścienna (na wysięgniku) stoi w powietrzu, bez ściany i bez słupa. Lampa / latarnia /
    // kinkiet do 1.6 m wysokości, na zewnątrz (nic nad nią), dół ≥ 0.8 m nad podłożem, bez podparcia i bez ściany w promieniu 0.4 m
    // (4 promienie poziome na wysokości środka) — usuwana. Bezpiecznik: > 40 = nic nie usuwam (lista w logu).
    static readonly string[] Lampy = { "latarn", "lantern", "kinkiet", "wall lamp", "wall_lamp", "walllamp", "sconce", "lampa ścienna", "lampa scienna" };
    static void LampyBezSciany(Transform root)
    {
        var kand = root.GetComponentsInChildren<Transform>(true).Where(t => { string n = Nazwa(t); return Lampy.Any(n.Contains) && t.GetComponentInChildren<Renderer>(true); }).ToList();
        var zb = new HashSet<Transform>(kand);
        kand = kand.Where(t => { for (var q = t.parent; q; q = q.parent) if (zb.Contains(q)) return false; return true; }).ToList();
        var usun = new List<GameObject>(); var opis = new List<string>();
        foreach (var t in kand)
        {
            var b = Obrys(t.gameObject); if (b.size.y > 1.6f || b.size.x > 2f || b.size.z > 2f) continue;
            var wl = new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
            float dol = 99f;
            foreach (var h in Physics.RaycastAll(new Vector3(b.center.x, b.min.y + .01f, b.center.z), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            { if (wl.Contains(h.collider)) continue; dol = h.distance; break; }
            if (dol < .8f) continue;                                                                  // stoi / leży na czymś
            if (Physics.RaycastAll(b.center, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore).Any(h => !wl.Contains(h.collider))) continue;   // pod stropem (lampa wisząca)
            bool sciana = false;
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                foreach (var h in Physics.RaycastAll(b.center, d, Mathf.Max(b.extents.x, b.extents.z) + .4f, ~0, QueryTriggerInteraction.Ignore))
                    if (!wl.Contains(h.collider)) { sciana = true; break; }
            if (sciana) continue;
            usun.Add(t.gameObject); opis.Add(t.name + " " + (dol * 100f).ToString("F0") + " cm nad podłożem, bez ściany @" + b.center.ToString("F0"));
        }
        if (usun.Count > 40) { Debug.LogWarning("BRAXHORN_DETALE_BEZPIECZNIK lampy bez ściany: " + usun.Count + " — NIC nie usuwam: " + string.Join("; ", opis.Take(15))); usun.Clear(); }
        foreach (var g in usun) if (g) UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("BRAXHORN_LAMPY_BEZ_SCIANY usuniete=" + usun.Count + " sprawdzone=" + kand.Count + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(20)) + "]" : ""));
    }

    static void Wiszace(Transform root)
    {
        var kandydaci = root.GetComponentsInChildren<Transform>(true).Where(t =>
        {
            string n = Nazwa(t);
            return Meble.Any(n.Contains) && !NieMeble.Any(n.Contains) && t.GetComponentInChildren<Renderer>(true) && !t.GetComponentInParent<BraxGymPlayer>() && !t.GetComponentInParent<BraxAuto>()
                && !t.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }).ToList();
        var zbior = new HashSet<Transform>(kandydaci);
        kandydaci = kandydaci.Where(t => { for (var q = t.parent; q; q = q.parent) if (zbior.Contains(q)) return false; return true; }).ToList();
        var usun = new List<GameObject>(); var opis = new List<string>();
        foreach (var t in kandydaci)
        {
            var b = Obrys(t.gameObject); if (b.size.x > 6f || b.size.z > 6f || b.size.y > 3f) continue;
            var wlasne = new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
            bool podparty = false; float najblizej = 99f;
            foreach (var o in new[] { b.center, new Vector3(b.min.x + .05f, 0, b.min.z + .05f), new Vector3(b.max.x - .05f, 0, b.min.z + .05f), new Vector3(b.min.x + .05f, 0, b.max.z - .05f), new Vector3(b.max.x - .05f, 0, b.max.z - .05f) })
            {
                var s = new Vector3(o.x, b.min.y + .01f, o.z);
                foreach (var h in Physics.RaycastAll(s, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                {
                    if (wlasne.Contains(h.collider)) continue;
                    najblizej = Mathf.Min(najblizej, h.distance - .01f);
                    if (h.distance - .01f <= .05f) podparty = true;
                    break;
                }
                if (podparty) break;
            }
            bool sufit = Physics.RaycastAll(b.center, Vector3.up, 8f, ~0, QueryTriggerInteraction.Ignore).Any(h => !wlasne.Contains(h.collider));
            if (podparty || najblizej > 2.9f || (!sufit && najblizej > 1.5f)) continue;   // podparty / nad pustką / na zewnątrz bez kolizji gruntu — nie ruszam
            usun.Add(t.gameObject); opis.Add(t.name + " " + (najblizej * 100f).ToString("F0") + " cm nad podłożem @" + b.center.ToString("F0"));
        }
        if (usun.Count > 40) { Debug.LogWarning("BRAXHORN_DETALE_BEZPIECZNIK za dużo do usunięcia (" + usun.Count + ") — podejrzenie błędu wykrywania, NIC nie usuwam, tylko lista: " + string.Join("; ", usun.Take(15).Select(g => g ? g.name : "?"))); usun.Clear(); }   // prowadzący (audyt 3.11)
        foreach (var g in usun) if (g) UnityEngine.Object.DestroyImmediate(g);
        Debug.Log("BRAXHORN_MEBLE_WISZACE usuniete=" + usun.Count + " sprawdzone=" + kandydaci.Count + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(30)) + "]" : "") + " (cel 0 wiszących)");
    }

    // ---------------- lady bez kolizji ----------------
    static void Lady(Transform root)
    {
        int dodane = 0; var opis = new List<string>();
        bool Lada(Transform t)
        {
            string n = Nazwa(t);
            return (n.Contains("lada") || n.Contains("recepcj") || n.Contains("counter") || n.Contains("kontuar")) && !n.Contains("napis") && !n.Contains("punkt") && !n.Contains("osoba") && !n.Contains("·")
                && t.GetComponentInChildren<Renderer>(true);
        }
        var lady = root.GetComponentsInChildren<Transform>(true).Where(Lada).ToList(); var zbior = new HashSet<Transform>(lady);
        foreach (var t in lady)
        {
            bool wyzej = false; for (var q = t.parent; q; q = q.parent) if (zbior.Contains(q)) { wyzej = true; break; }
            if (wyzej || t.GetComponentInChildren<Collider>(true)) continue;
            var b = Obrys(t.gameObject); if (b.size.y < .6f || b.size.y > 2.2f || b.size.x > 12f || b.size.z > 12f || b.size.x < .3f || b.size.z < .3f) continue;
            var g = new GameObject("kolizja lady (BRAXHORN_LADY_KOLIZJE)"); g.transform.SetParent(t.parent ? t.parent : root, false);
            g.transform.SetPositionAndRotation(b.center, Quaternion.identity);
            var ls = g.transform.lossyScale; var bc = g.AddComponent<BoxCollider>();
            bc.size = new Vector3(b.size.x / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), b.size.y / Mathf.Max(1e-4f, Mathf.Abs(ls.y)), b.size.z / Mathf.Max(1e-4f, Mathf.Abs(ls.z)));
            dodane++; opis.Add(t.name + " @" + b.center.ToString("F0"));
        }
        Debug.Log("BRAXHORN_LADY_KOLIZJE dodane=" + dodane + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(20)) + "]" : ""));
    }

    // pkt 2: dwa hydranty obok siebie (ul. z DELIKATESAMI) → jeden (w promieniu 4 m zostaje pierwszy, reszta usunięta z kolizjami)
    static void Hydranty(Transform root)
    {
        var h = root.GetComponentsInChildren<Transform>(true).Where(t => Nazwa(t).Contains("hydrant") && !Nazwa(t).Contains("szafk") && t.GetComponentInChildren<Renderer>(true)).ToList();
        var zb = new HashSet<Transform>(h);
        h = h.Where(t => { for (var q = t.parent; q; q = q.parent) if (zb.Contains(q)) return false; return true; }).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToList();
        var zostaja = new List<Vector3>(); var opis = new List<string>(); int usun = 0;
        foreach (var t in h)
        {
            var p = Obrys(t.gameObject).center;
            if (zostaja.Any(q => (new Vector2(q.x - p.x, q.z - p.z)).magnitude < 4f && Mathf.Abs(q.y - p.y) < 2f)) { opis.Add(t.name + " @" + p.ToString("F0")); UnityEngine.Object.DestroyImmediate(t.gameObject); usun++; }
            else zostaja.Add(p);
        }
        Debug.Log("BRAXHORN_HYDRANTY zostaja=" + zostaja.Count + " usuniete_duplikaty=" + usun + (opis.Count > 0 ? " [" + string.Join("; ", opis.Take(10)) + "]" : ""));
    }
}

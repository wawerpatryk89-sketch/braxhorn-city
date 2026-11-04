using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using M = BraxWnetrzaKit.Mat;

// BILLBOARDY I MURALE W MIEŚCIE (pkt 48 / 48b, właściciel 02:01; plan/GRAFIKI_ROZMIESZCZENIE.md — część MIASTO i „Nośniki”).
// Zasady: NIE za dużo, każdy w innym miejscu; billboard tylko na nośniku (słup, kratownica, rama, pomost, lampy), citylight =
// gablota; MURAL tylko na murze (ślepa ściana), nigdy na szkle ani na wieżowcu; wszystkie stare billboardy / plakaty / banery
// w mieście USUNIĘTE (plakaty NOVA i ekrany LED na wieżach wyłączone w ich kodzie, tu przegląd nazw w całym mieście).
//  09 billboard przy głównej alei (plac ronda miasta B), 12 billboard przy ARENIE (wyspa), 03 citylight na chodniku przy NOVA,
//  06 billboard przy plaży / bulwarze, 10 citylight przy przystanku RONDO, MURAL 19 (3:2) na szarej ścianie przy wejściu do
//  BRAXHORN GYM, z kinkietami. Grafika: Assets/Source/Prywatne/Grafiki/billboardy/NN.jpg (paczka koordynatora); bez pliku —
//  nośnik z pustą ramą (ciemna płyta). Każde miejsce sprawdzane fizyką (grunt, wolne miejsce, nie na jezdni, mur bez szkła).
// Wymiary × K. Log BRAXHORN_BILLBOARDY postawione=… brak_grafiki=… usuniete_stare=….
internal static class BraxMiastoBillboardy
{
    const string Dir = "Assets/Source/Prywatne/Grafiki/billboardy/";
    static float K => BraxMiastoA.K;
    static int postawione, brak; static readonly List<string> opis = new List<string>();
    static Material stal, grafit, beton, alu, szklo, krata, pusta, led;

    public static void Apply(Transform root)
    {
        postawione = brak = 0; opis.Clear();
        Physics.SyncTransforms();
        int stare = 0;
        try { stare = UsunStare(root); } catch (Exception e) { Debug.LogWarning("BRAXHORN_BILLBOARDY stare DO POPRAWKI " + e.Message); }
        Physics.SyncTransforms();
        stal = M.Plain("nośnik: stal ocynkowana malowana", new Color(.36f, .37f, .38f), .5f, .7f);
        grafit = M.Plain("nośnik: grafit (ramy, obudowy)", new Color(.07f, .07f, .08f), .45f, .6f);
        beton = M.Plain("nośnik: fundament betonowy", new Color(.6f, .6f, .58f), .9f);
        alu = M.Plain("nośnik: profil aluminiowy", new Color(.72f, .73f, .75f), .3f, .9f);
        szklo = M.Plain("nośnik: szyba gabloty", new Color(.8f, .88f, .9f), .03f, .2f);
        krata = M.Plain("nośnik: krata pomostu", new Color(.25f, .26f, .27f), .6f, .7f);
        pusta = M.Plain("nośnik: pusta rama (bez grafiki)", new Color(.16f, .16f, .17f), .7f);
        led = BraxRegionMeble.Swiecacy("nośnik: LED podświetlenia", new Color(1f, .97f, .9f), 1.6f);
        var g = new GameObject("CLAUDE nośniki reklamowe (billboardy, citylighty, mural)").transform; g.SetParent(root, false);
        void Krok(string n, Action a) { try { a(); } catch (Exception e) { Debug.LogWarning("BRAXHORN_BILLBOARDY " + n + " DO POPRAWKI " + e.Message); opis.Add(n + ": DO POPRAWKI"); } }

        Krok("09 aleja", () =>
        {
            var c = new Vector3(BraxRegionMiastoB.RX, 0f, BraxRegionMiastoB.RZ);
            if (Szukaj(c, 30f, 46f, 8f, true, out var p, out float yaw)) Billboard(g, p, yaw, "09", 8.0f, "główna aleja (plac ronda)"); else opis.Add("09: brak wolnego miejsca przy rondzie");
        });
        Krok("12 arena", () =>
        {
            var ar = root.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterial && r.sharedMaterial.name.Contains("Arena: szkło fasady")).ToList();
            if (ar.Count == 0) { opis.Add("12: brak areny"); return; }
            var b = ar[0].bounds; foreach (var r in ar) b.Encapsulate(r.bounds);
            var wysp = root.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("CLAUDE wyspa", StringComparison.Ordinal));
            if (Szukaj(new Vector3(b.center.x, b.min.y, b.center.z), b.extents.x + 10f, b.extents.x + 26f, 6.52f, false, out var p, out float yaw))
                Billboard(wysp ? wysp : g, p, yaw, "12", 6.52f, "arena (wyspa)");
            else opis.Add("12: brak wolnego miejsca przy arenie");
        });
        Krok("03 NOVA", () =>
        {
            foreach (float x in new[] { 27.6f, 29.0f, 47.4f })
            {
                var p = Grunt(x, 34.0f); if (float.IsNaN(p.y) || !Wolne(p, 1.0f, .25f, 2.6f, 0f)) continue;
                Citylight(g, p, 0f, "03", "chodnik przy NOVA"); return;
            }
            opis.Add("03: brak wolnego miejsca na chodniku przy NOVA");
        });
        Krok("06 plaża", () =>
        {
            foreach (var (x, z) in new[] { (68f, 106f), (72f, 106f), (68f, 172f), (76f, 104f), (66f, 140f) })
            {
                var p = Grunt(x, z); if (float.IsNaN(p.y) || !Wolne(p, 1.2f, 1.2f, 4.5f, 0f)) continue;
                var f = new Vector3(75f - x, 0f, 139f - z); float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                if (!WolnyPanel(p, yaw, 6.52f)) continue;
                Billboard(g, p, yaw, "06", 6.52f, "bulwar przy plaży"); return;
            }
            opis.Add("06: brak wolnego miejsca na bulwarze");
        });
        Krok("10 przystanek", () =>
        {
            var w = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Przystanek RONDO", StringComparison.Ordinal));
            if (!w) { opis.Add("10: brak przystanku RONDO"); return; }
            var rs = w.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            foreach (float z in new[] { b.max.z + 1.1f, b.min.z - 1.1f, b.max.z + 2.4f })
            {
                var p = Grunt(b.center.x, z); if (float.IsNaN(p.y) || !Wolne(p, 1.0f, .25f, 2.6f, 0f)) continue;
                Citylight(w, p, 0f, "10", "przystanek RONDO"); return;
            }
            opis.Add("10: brak miejsca przy wiacie");
        });
        Krok("19 mural GYM", () => MuralSilowni(g));

        string linia = "BRAXHORN_BILLBOARDY postawione=" + postawione + " brak_grafiki=" + brak + " usuniete_stare=" + stare + " [" + string.Join("; ", opis) + "]";
        if (postawione >= 5) Debug.Log(linia); else Debug.LogWarning(linia);
    }

    // ---------------- stare reklamy ----------------
    static int UsunStare(Transform root)
    {
        string[] slowa = { "billboard", "plakat", "baner", "banner", "poster", "reklam", "artwork" };
        var t = root.GetComponentsInChildren<Transform>(true).Where(x =>
        {
            string n = x.name.ToLowerInvariant();
            return slowa.Any(n.Contains) && !n.Contains("nośnik") && !n.Contains("citylight") && !n.Contains("mural") && x.GetComponentInChildren<Renderer>(true);
        }).ToList();
        var zb = new HashSet<Transform>(t);
        t = t.Where(x => { for (var q = x.parent; q; q = q.parent) if (zb.Contains(q)) return false; return true; }).ToList();
        // prowadzący (audyt 3.11): usuwamy tylko obiekty wielkości reklamy (≤ 14 m, ≤ 24 renderery) — grupa o nazwie z „reklam”/
        // „artwork” obejmująca np. całą fasadę zostaje (tylko log)
        int zostawione = 0;
        t = t.Where(x =>
        {
            var rs = x.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            bool ok = rs.Length <= 24 && Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) <= 14f;
            if (!ok) { zostawione++; Debug.Log("BRAXHORN_BILLBOARDY stara grupa ZOSTAWIONA (za duża do automatu): " + x.name + " " + b.size.ToString("F1")); }
            return ok;
        }).ToList();
        foreach (var x in t) if (x) UnityEngine.Object.DestroyImmediate(x.gameObject);
        return t.Count;
    }

    // ---------------- miejsca ----------------
    static Vector3 Grunt(float x, float z)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(x, 40f, z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            if (h.normal.y > .8f) return h.point;
        return new Vector3(x, float.NaN, z);
    }
    static bool Wolne(Vector3 p, float rx, float rz, float h, float yaw)
    {
        var c = p + Vector3.up * (h * .5f + .25f);
        return Physics.OverlapBox(c, new Vector3(rx, h * .5f, rz), Quaternion.Euler(0, yaw, 0), ~0, QueryTriggerInteraction.Ignore).Length == 0;
    }
    static bool WolnyPanel(Vector3 p, float yaw, float szer)
    {
        var c = p + Vector3.up * (5.0f + 2.2f); return Physics.OverlapBox(c, new Vector3(szer * .5f + .6f, 2.4f, 1.2f), Quaternion.Euler(0, yaw, 0), ~0, QueryTriggerInteraction.Ignore).Length == 0;
    }
    /// <summary>Pierścień wokół środka: grunt równy, nie jezdnia (miasto B), wolny słup i wolne miejsce na płytę; przodem do środka.</summary>
    static bool Szukaj(Vector3 c, float r0, float r1, float szer, bool miastoB, out Vector3 p, out float yaw)
    {
        p = default; yaw = 0f;
        for (float r = r0; r <= r1; r += 4f)
            for (float a = 0f; a < 360f; a += 15f)
            {
                var q = c + new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0f, Mathf.Cos(a * Mathf.Deg2Rad)) * r;
                var gp = Grunt(q.x, q.z); if (float.IsNaN(gp.y) || Mathf.Abs(gp.y - c.y) > 2.5f) continue;
                if (miastoB && BraxRegionMiastoB.NaJezdni(gp)) continue;
                var f = c - gp; f.y = 0f; float y = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                if (!Wolne(gp, 1.4f, 1.4f, 4.5f, y) || !WolnyPanel(gp, y, szer)) continue;
                p = gp; yaw = y; return true;
            }
        return false;
    }

    // ---------------- grafika ----------------
    static Material Grafika(string nr, float domyslnaProporcja, out float proporcja)
    {
        proporcja = domyslnaProporcja;
        // 3.11 14:37: nowe grafiki właściciela (paczka miasto_sklepy/billboardy) mają pierwszeństwo przed starymi
        string nowy = "Assets/Source/Prywatne/Miasto/miasto_sklepy/billboardy/" + nr + ".jpg";
        // Mural 19 przy siłowni to konkretny oryginał właściciela (Brax + Nyxa).
        // Nigdy nie podmieniaj go automatycznie na plakat z paczki sklepowej.
        string oryginal = Dir + nr + ".jpg";
        string plik = nr == "19" ? oryginal : (File.Exists(nowy) ? nowy : oryginal);
        try
        {
            if (File.Exists(plik))
            {
                AssetDatabase.ImportAsset(plik, ImportAssetOptions.ForceSynchronousImport);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(plik);
                if (tex)
                {
                    var m = new Material(Shader.Find("Braxhorn/CityStaticPBR")) { name = "billboard_" + nr };
                    m.SetTexture("_MainTex", tex); m.SetColor("_Color", Color.white);
                    if (m.HasProperty("_Roughness")) m.SetFloat("_Roughness", .7f);
                    string dir = W.Out + "/Nosniki"; Directory.CreateDirectory(dir);
                    BraxZapisAssetu.Utworz(m, dir + "/billboard_" + nr + ".mat");
                    proporcja = tex.width / (float)Mathf.Max(1, tex.height);
                    return m;
                }
            }
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_BILLBOARDY grafika " + nr + " DO POPRAWKI " + e.Message); }
        brak++;
        return pusta;
    }

    /// <summary>Mural / szyld z paczki grafik właściciela (Assets/Source/Prywatne/Grafiki/szyldy/plik.jpg), podświetlony emisją.
    /// Zwraca false, gdy brak pliku (paczka nieodszyfrowana) — wtedy zostaje to, co było.</summary>
    public static bool Mural(Transform g, string plik, Vector3 c, Vector3 n, float w, float h)
    {
        string p = "Assets/Source/Prywatne/Grafiki/szyldy/" + plik + ".jpg";
        if (!File.Exists(p)) { Debug.Log("BRAXHORN_MURAL brak " + p); return false; }
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (!tex) return false;
        var m = new Material(Shader.Find("Standard")) { name = "mural_" + plik, mainTexture = tex };
        m.SetFloat("_Glossiness", .3f); m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.55f, .55f, .55f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        string dir = W.Out + "/Nosniki"; Directory.CreateDirectory(dir);
        BraxZapisAssetu.Utworz(m, dir + "/mural_" + plik + ".mat");
        Plakat(g, "mural " + plik, c, n, w, h, m);
        var ch = g.Find("mural " + plik); if (ch) ch.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // siatka w układzie świata (rodzic może być obrócony)
        Debug.Log("BRAXHORN_MURAL " + plik + " " + w.ToString("F1") + "×" + h.ToString("F2") + " m");
        return true;
    }
    /// <summary>Plakat: quad z UV 0…1, przód = n (obraz nieodbity dla patrzącego od strony n).</summary>
    static void Plakat(Transform g, string nazwa, Vector3 c, Vector3 n, float w, float h, Material m)
    {
        var r = Vector3.Cross(Vector3.up, -n).normalized; var u = Vector3.up;
        var mesh = new Mesh { name = nazwa };
        mesh.vertices = new[] { c - r * w * .5f - u * h * .5f, c + r * w * .5f - u * h * .5f, c + r * w * .5f + u * h * .5f, c - r * w * .5f + u * h * .5f };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.normals = new[] { n, n, n, n }; mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 }; mesh.RecalculateBounds();
        BraxMiastoA.Obiekt(g, nazwa, mesh, m);
    }

    // ---------------- billboard na słupie ----------------
    static void Billboard(Transform rodzic, Vector3 p, float yaw, string nr, float szer, string gdzie)
    {
        var g = new GameObject("BRAXHORN nośnik: billboard " + nr + " (" + gdzie + ")").transform; g.SetParent(rodzic, true); g.position = Vector3.zero;
        var mat = Grafika(nr, 504f / 238f, out float asp);
        float Wp = szer, Hp = Wp / asp; if (Hp > 4.2f * K) { Hp = 4.2f * K; Wp = Hp * asp; }
        var q = Quaternion.Euler(0f, yaw, 0f); var f = q * Vector3.forward; var r = Vector3.Cross(Vector3.up, -f).normalized; var u = Vector3.up;
        float yB = 5.0f;                                                                  // dół płyty nad gruntem
        var o = p + f * .35f + u * yB;                                                    // środek dolnej krawędzi lica
        Vector3 F(float s, float hh, float d) => o + r * s + u * hh + f * d;
        W.Start(g); W.Wnetrze = false;
        // fundament, blacha podstawy, kotwy, słup Ø 0.6 m × K z kołnierzem
        W.Walec(beton, p - u * .3f, .75f, .35f, 18);
        W.Walec(stal, p + u * .05f, .55f, .04f, 18);
        for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; W.Walec(stal, p + u * .09f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .44f, .028f, .07f, 6); }
        float rs = .3f * K; var top = p + u * (yB + Hp * .5f);
        W.Rura(stal, p + u * .09f, top, rs, 16);
        W.Walec(stal, p + u * (yB - .6f), rs + .06f, .05f, 16);
        W.Kolizja(p + u * 2.5f, new Vector3(rs * 2f, 5f, rs * 2f), Quaternion.identity);
        // kratownica za licem: 3 pasy, słupki co 1.5 m, krzyżulce, łączniki do płyty, głowica słupa
        foreach (float hh in new[] { .15f, Hp * .5f, Hp - .15f }) W.Rura(stal, F(-Wp * .5f, hh, -.45f), F(Wp * .5f, hh, -.45f), .05f, 8);
        int nPion = Mathf.Max(2, Mathf.RoundToInt(Wp / 1.5f)); float krok = Wp / nPion;
        for (int i = 0; i <= nPion; i++)
        {
            float s = -Wp * .5f + i * krok;
            W.Rura(stal, F(s, .15f, -.45f), F(s, Hp - .15f, -.45f), .04f, 8);
            foreach (float hh in new[] { .3f, Hp - .3f }) W.Rura(stal, F(s, hh, -.45f), F(s, hh, -.07f), .025f, 6);
            if (i < nPion) W.Rura(stal, F(s, i % 2 == 0 ? .15f : Hp - .15f, -.45f), F(s + krok, i % 2 == 0 ? Hp - .15f : .15f, -.45f), .022f, 6);
        }
        W.Rura(stal, top, F(0f, Hp * .5f, -.45f), .12f, 10);
        // płyta tylna (podkład 3 cm za plakatem) i osłona kratownicy od tyłu
        W.OBox(grafit, F(0f, Hp * .5f, -.05f), new Vector3(Wp, Hp, .04f), q);
        W.OBox(stal, F(0f, Hp * .5f, -.75f), new Vector3(Wp + .1f, Hp + .1f, .03f), q);
        // rama aluminiowa z profilem (14 cm głęb.) wokół lica
        W.OBox(alu, F(0f, -.06f, -.02f), new Vector3(Wp + .24f, .12f, .14f), q); W.OBox(alu, F(0f, Hp + .06f, -.02f), new Vector3(Wp + .24f, .12f, .14f), q);
        W.OBox(alu, F(-Wp * .5f - .06f, Hp * .5f, -.02f), new Vector3(.12f, Hp, .14f), q); W.OBox(alu, F(Wp * .5f + .06f, Hp * .5f, -.02f), new Vector3(.12f, Hp, .14f), q);
        Plakat(g, "billboard_" + nr + " (lico)", F(0f, Hp * .5f, 0f), f, Wp, Hp, mat);
        // pomost serwisowy z kraty, barierka (pochwyt, przeciągi, krawężnik), wsporniki, drabina na słupie
        W.OBox(krata, F(0f, -.25f, .5f), new Vector3(Wp, .05f, 1.0f), q);
        for (float s = -Wp * .5f; s <= Wp * .5f + .01f; s += Wp / Mathf.Max(2, Mathf.RoundToInt(Wp / 1.5f)))
        {
            W.Rura(stal, F(s, -.22f, 1.0f), F(s, .88f, 1.0f), .025f, 6);
            W.Rura(stal, F(s, .15f, -.45f), F(s, -.27f, .95f), .03f, 6);
        }
        foreach (float hh in new[] { .88f, .4f }) W.Rura(stal, F(-Wp * .5f, hh, 1.0f), F(Wp * .5f, hh, 1.0f), .022f, 6);
        W.OBox(stal, F(0f, -.15f, .99f), new Vector3(Wp, .15f, .02f), q);
        var lad = p + f * (rs + .12f);
        foreach (float s in new[] { -.22f, .22f }) W.Rura(stal, lad + r * s + u * 1.2f, lad + r * s + u * (yB - .2f), .02f, 6);
        for (float y = 1.4f; y < yB - .3f; y += .3f) W.Rura(stal, lad - r * .22f + u * y, lad + r * .22f + u * y, .014f, 4, false);
        for (float y = 2.6f; y < yB - .3f; y += .9f)                                       // kosz drabiny (obręcze)
            for (int i = 0; i < 6; i++)
            {
                float a0 = Mathf.PI * i / 6f, a1 = Mathf.PI * (i + 1) / 6f;
                Vector3 A(float a) => lad + f * .38f + r * (Mathf.Cos(a) * .38f) + f * (Mathf.Sin(a) * .3f) + u * y;
                W.Rura(stal, A(a0), A(a1), .012f, 4, false);
            }
        // 3 lampy wysięgnikowe nad licem (świecą na plakat)
        foreach (float s in new[] { -Wp / 3f, 0f, Wp / 3f })
        {
            W.Rura(grafit, F(s, Hp + .1f, -.05f), F(s, Hp + .45f, 1.1f), .03f, 8);
            W.OBox(grafit, F(s, Hp + .42f, 1.18f), new Vector3(.55f, .14f, .32f), q * Quaternion.Euler(35f, 0f, 0f));
            W.OBox(led, F(s, Hp + .36f, 1.12f), new Vector3(.45f, .02f, .22f), q * Quaternion.Euler(35f, 0f, 0f));
        }
        W.Lampa("nośnik " + nr + ": światło lica", F(0f, Hp * .6f, 1.6f), .7f, Hp + 4f);
        W.Zapisz("nosnik_billboard_" + nr);
        postawione++; opis.Add(nr + " billboard " + Wp.ToString("F1") + "×" + Hp.ToString("F1") + " m @" + p.ToString("F0") + " yaw " + yaw.ToString("F0") + " (" + gdzie + ")" + (mat == pusta ? " PUSTA RAMA" : ""));
    }

    // ---------------- citylight (gablota dwustronna) ----------------
    static void Citylight(Transform rodzic, Vector3 p, float yaw, string nr, string gdzie)
    {
        var g = new GameObject("BRAXHORN nośnik: citylight " + nr + " (" + gdzie + ")").transform; g.SetParent(rodzic, true); g.position = Vector3.zero;
        var mat = Grafika(nr, 1.185f / 1.75f, out float asp);
        float Hv = 1.75f * K, Wv = Mathf.Min(1.185f * K, Hv * asp); Hv = Wv / asp;
        var q = Quaternion.Euler(0f, yaw, 0f); var f = q * Vector3.forward; var r = Vector3.Cross(Vector3.up, -f).normalized; var u = Vector3.up;
        float yb = .3f; var c = p + u * (yb + Hv * .5f);
        W.Start(g); W.Wnetrze = false;
        W.OBox(grafit, p + u * .14f, new Vector3(Wv + .3f, .28f, .34f), q, true);                                    // cokół
        foreach (float s in new[] { -1f, 1f }) W.OBox(grafit, p + r * s * (Wv * .5f + .06f) + u * (yb + Hv * .5f), new Vector3(.12f, Hv + .14f, .2f), q);   // boki ramy
        W.OBox(grafit, p + u * (yb - .04f), new Vector3(Wv + .24f, .08f, .2f), q); W.OBox(grafit, p + u * (yb + Hv + .04f), new Vector3(Wv + .24f, .08f, .2f), q);
        W.OBox(alu, p + u * (yb + Hv + .11f), new Vector3(Wv + .34f, .06f, .3f), q);                                    // daszek
        W.OBox(led, p + u * (yb + Hv - .01f), new Vector3(Wv - .04f, .02f, .12f), q);                                   // LED górny
        foreach (float s in new[] { -1f, 1f })
        {
            W.OBox(szklo, c + f * s * .085f, new Vector3(Wv, Hv, .008f), q);
            Plakat(g, "citylight_" + nr + (s > 0 ? " (przód)" : " (tył)"), c + f * s * .06f, f * s, Wv, Hv, mat);
        }
        W.OBox(M.Plain("nośnik: wnętrze gabloty (biel)", new Color(.92f, .92f, .9f), .6f), c, new Vector3(Wv, Hv, .04f), q);
        W.Kolizja(c, new Vector3(Wv + .3f, Hv + .6f, .3f), q);
        W.Lampa("citylight " + nr, c + f * .6f, .25f, 3f);
        W.Zapisz("nosnik_citylight_" + nr);
        postawione++; opis.Add(nr + " citylight @" + p.ToString("F0") + " (" + gdzie + ")" + (mat == pusta ? " PUSTA RAMA" : ""));
    }

    // ---------------- mural 19 na szarej ścianie przy wejściu do BRAXHORN GYM ----------------
    static void MuralSilowni(Transform rodzic)
    {
        // lico ściany od ulicy (płd. pierzeja, z ≈ 20.6): odcinki obok wyjścia z siłowni (x 2.5…9.5); siatka promieni —
        // wszystkie trafiają w to samo płaskie lico (± 3 cm), normalna do ulicy, materiał bez szkła / witryny
        foreach (var (x0, x1) in new[] { (9.8f, 14.4f), (-0.8f, 2.3f) })
        {
            var traf = new List<float>(); bool ok = true;
            for (int i = 0; i < 5 && ok; i++)
                for (int j = 0; j < 4 && ok; j++)
                {
                    float x = Mathf.Lerp(x0 + .2f, x1 - .2f, i / 4f), y = Mathf.Lerp(.9f, 3.6f, j / 3f);
                    if (!Physics.Raycast(new Vector3(x, y, 23.2f), Vector3.back, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) || h.normal.z < .9f) { ok = false; break; }
                    var rr = h.collider.GetComponent<Renderer>(); var nm = (h.collider.name + " " + (rr && rr.sharedMaterial ? rr.sharedMaterial.name : "")).ToLowerInvariant();
                    if (nm.Contains("szkł") || nm.Contains("szkl") || nm.Contains("glass") || nm.Contains("witryn") || nm.Contains("drzwi") || nm.Contains("door")) { ok = false; break; }
                    traf.Add(h.point.z);
                }
            // Pierwotny test wymagał 20/20 trafień w dokładnie jeden collider ściany.
            // Fasada jest rzeczywistą siatką (także bez pełnych colliderów), więc build #64
            // wykazał "19: brak ślepego muru" mimo pustej ściany widocznej w grze.
            // Jeśli raycast nie potwierdzi płaszczyzny, użyj zewnętrznego lica fasady
            // z planu miasta z=20.6, NIE stawiaj nowej elewacji ani pustej ramy.
            float zw;
            bool zweryfikowanyMur = ok && traf.Count > 0;
            if (zweryfikowanyMur)
            {
                traf.Sort(); zw = traf[traf.Count / 2];
                zweryfikowanyMur = !traf.Any(z => Mathf.Abs(z - zw) > .03f);
            }
            else zw = 20.6f;
            if (!zweryfikowanyMur)
            {
                if (x0 < 9f) continue; // jedyna znana ślepa ściana jest po prawej od wejścia
                zw = 20.6f;
                Debug.LogWarning("BRAXHORN_MURAL_19 kotwica siłowni z=20.60 z planu; raycast nie rozpoznał ściany — potwierdzić zrzutem Unity");
            }
            var mat = Grafika("19", 1.5f, out float asp);
            if (mat == pusta) { opis.Add("19: brak tekstury właściciela — nie montujemy pustej ramy"); return; }
            float w = Mathf.Min(x1 - x0 - .6f, 4.5f), h2 = w / asp; if (h2 > 3.0f) { h2 = 3.0f; w = h2 * asp; }
            float xc = (x0 + x1) * .5f, yb = .15f + .7f;
            var g = new GameObject("BRAXHORN mural 19 (szara ściana przy wejściu do BRAXHORN GYM)").transform; g.SetParent(rodzic, true); g.position = Vector3.zero;
            W.Start(g); W.Wnetrze = false;
            Plakat(g, "mural_19", new Vector3(xc, yb + h2 * .5f, zw + .05f), Vector3.forward, w, h2, mat);
            // rama stalowa na kotwach (8 cm od muru), kotwy w narożach i w połowie
            float zf = zw + .02f;
            W.MM(grafit, xc - w * .5f - .08f, xc + w * .5f + .08f, yb - .08f, yb, zf, zf + .07f); W.MM(grafit, xc - w * .5f - .08f, xc + w * .5f + .08f, yb + h2, yb + h2 + .08f, zf, zf + .07f);
            W.MM(grafit, xc - w * .5f - .08f, xc - w * .5f, yb, yb + h2, zf, zf + .07f); W.MM(grafit, xc + w * .5f, xc + w * .5f + .08f, yb, yb + h2, zf, zf + .07f);
            foreach (float x in new[] { xc - w * .5f - .04f, xc, xc + w * .5f + .04f })
                foreach (float y in new[] { yb - .04f, yb + h2 + .04f }) W.Rura(stal, new Vector3(x, y, zw), new Vector3(x, y, zf), .02f, 6);
            // 4 kinkiety nad muralem (płytka, wysięgnik, oprawa świecąca w dół na mural)
            for (int i = 0; i < 4; i++)
            {
                float x = xc - w * .5f + w * (i + .5f) / 4f, y = yb + h2 + .35f;
                W.MM(grafit, x - .07f, x + .07f, y - .07f, y + .07f, zw, zw + .02f);
                W.Rura(grafit, new Vector3(x, y, zw + .02f), new Vector3(x, y + .1f, zw + .5f), .015f, 6);
                W.OBox(grafit, new Vector3(x, y + .1f, zw + .55f), new Vector3(.22f, .08f, .14f), Quaternion.Euler(-30f, 0f, 0f));
                W.OBox(led, new Vector3(x, y + .065f, zw + .54f), new Vector3(.18f, .01f, .1f), Quaternion.Euler(-30f, 0f, 0f));
            }
            W.Lampa("mural 19: kinkiety", new Vector3(xc, yb + h2 * .6f, zw + 1.2f), .6f, w + 2f);
            W.Zapisz("mural_19_silownia");
            postawione++; opis.Add("19 mural " + w.ToString("F2") + "×" + h2.ToString("F2") + " m na licu z " + zw.ToString("F2") + " x " + x0 + "…" + x1 + (mat == pusta ? " PUSTA RAMA" : ""));
            return;
        }
        opis.Add("19: brak ślepego, płaskiego muru przy wejściu do siłowni (x 9.8…14.4 i −0.8…2.3) — mural nie postawiony");
    }
}

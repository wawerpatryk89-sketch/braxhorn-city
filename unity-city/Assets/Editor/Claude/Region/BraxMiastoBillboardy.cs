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
        Krok("19 mural GYM", () => MuralSilowni(root, g));

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
        string plik = File.Exists(nowy) ? nowy : Dir + nr + ".jpg";
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

    // Tylko historyczna tablica z grafiką przy szarej ścianie: x=9.8..14.4.
    // Może pochodzić zarówno z renowacji, jak i z oryginalnej siatki miasta.
    // Nie kasujemy ceglanej ściany, witryn, drzwi ani żadnej innej reklamy.
    static int UsunBilbord19PrzySzarejScianie(Transform cityRoot)
    {
        // Obejmuje też importowany oryginalny gym, który nie jest dzieckiem
        // korzenia miasta. Lokalizacja i identyfikacja grafiki ograniczają usuwanie.
        var widoczne = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r && r.enabled).ToArray();
        bool WObszarze(Renderer r)
        {
            var b = r.bounds;
            return b.center.x >= 9.5f && b.center.x <= 14.8f &&
                   Mathf.Abs(b.center.z - 20.6f) < .8f &&
                   b.center.y < 6.5f && b.size.x > .65f && b.size.x < 5.5f &&
                   b.size.y > .65f && b.size.y < 5.3f && b.size.z < .8f;
        }
        bool Reklama(Renderer r)
        {
            string name = r.name.ToLowerInvariant();
            if (name == "mural_19" || name.Contains("billboard") ||
                name.Contains("poster") || name.Contains("plakat") ||
                name.Contains("artwork")) return true;
            foreach (var mat in r.sharedMaterials)
            {
                if (!mat) continue;
                string mn = mat.name.ToLowerInvariant();
                string tn = mat.mainTexture ? mat.mainTexture.name.ToLowerInvariant() : "";
                string path = mat.mainTexture ? AssetDatabase.GetAssetPath(mat.mainTexture).ToLowerInvariant() : "";
                if (mn.Contains("billboard_") || mn.Contains("mural_19") ||
                    mn.Contains("original artwork") || tn == "19" ||
                    tn.Contains("brand-poster") || tn.Contains("poster") || tn.Contains("billboard") ||
                    path.Contains("/billboardy/") || path.Contains("/brand-poster.")) return true;
            }
            return false;
        }
        // Stara grupa 19 miała własną ramę i lampy. Usuń całość, o ile zawiera
        // panel właśnie w tej zatoce ceglanej; nie dotykaj innych grup sceny.
        var grupy = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t &&
                (t.name.StartsWith("BRAXHORN mural 19 (", StringComparison.Ordinal) ||
                 t.name.StartsWith("BRAXHORN nośnik: billboard 19", StringComparison.Ordinal)))
            .Where(t => t.GetComponentsInChildren<Renderer>(true).Any(WObszarze))
            .Distinct().ToArray();
        var glowne = grupy.Where(t => !grupy.Any(p => p != t && t.IsChildOf(p))).ToArray();
        int usuniete = 0;
        foreach (var t in glowne)
            if (t) { UnityEngine.Object.DestroyImmediate(t.gameObject); usuniete++; }

        // Osobne płaskie plakaty, których grupa nie miała historycznej nazwy.
        // Ważne: wymagamy nazwy/materialu grafiki i wymiaru tablicy; nie wycinamy
        // jakichkolwiek trójkątów zwykłej ceglanej elewacji.
        foreach (var r in widoczne)
        {
            if (!r || !WObszarze(r) || !Reklama(r)) continue;
            if (r.transform.GetComponentInChildren<MeshRenderer>(true) != r &&
                r.GetComponent<MeshFilter>() == null) continue;
            UnityEngine.Object.DestroyImmediate(r.gameObject);
            usuniete++;
        }
        if (usuniete == 0)
        {
            string kandydaci = string.Join("; ", widoczne.Where(WObszarze).Take(8)
                .Select(r => r.name + " [" + string.Join(",", r.sharedMaterials.Where(m => m).Select(m => m.name)) + "]"));
            Debug.LogWarning("BRAXHORN_MURAL_19 NIE POTWIERDZONO USUNIECIA STAREGO BILLBOARDU; obiekty w zatoce: " + kandydaci);
        }
        Debug.Log("BRAXHORN_MURAL_19 usuniete_plakaty_z_ceglanej_zatoki=" + usuniete +
                  " (x=9.5..14.8, bez zmian poza ta sciana)");
        return usuniete;
    }

    // ---------------- mural 19 NA SZAREJ ŚCIANIE BRAXHORN GYM (pigment na murze, bez tablicy, ramy i słupków) ----------------
    // Właściciel 6.11: „cała szara ściana, grafika 19, otwór na drzwi”; 9.11: „mural na szarej ścianie siłowni musi być, a jest
    // przed”; „mural przed siłownią — to ma być na ścianie”. PRZYCZYNA (pomiar oryginalnego modelu siłowni, 9.11): szara elewacja
    // siłowni to Gym_Mesh_37 — lico z = 11.6, x −18.6…18.6, wysokość 8.5 m (drzwi Gym_Mesh_24 x 5.2…6.8 do 3.0 m, szyld nad
    // drzwiami Gym_Mesh_48/34 — wszystko w tej samej płaszczyźnie). Przed nią jest otwarty dziedziniec do ulicy (z 11.6…20.6),
    // ograniczony ceglanymi ścianami bocznymi Gym_Mesh_38. Stary kod stawiał mural na z = 20.6 (linia ulicy) — czyli w powietrzu,
    // 9 m PRZED ścianą; „lico z rendererów” brało max.z bryły ścian bocznych dziedzińca. Teraz: NIC z planu. Ścianę i jej kształt
    // mierzymy z prawdziwej geometrii: tymczasowe kolizje siatek w pasie przed siłownią, siatka promieni od ulicy co 10 cm.
    // Mural powstaje tylko tam, gdzie promień trafia lico szarej ściany, widoczne z ulicy; drzwi, szyld i wszystko, co jest
    // w płaszczyźnie ściany (±30 cm), zostaje otworem; mury przed ścianą (boki dziedzińca, budynki) zasłaniają. Kadr grafiki
    // = prostokąt widocznej szarej ściany. Ściana siłowni ma własną kolizję (gym-collisions z 11…11.6) — bez dodatkowych pudeł.
    const float MuralKrok = .10f, MuralOdstep = .012f;
    /// <summary>11.11: mural 19 na całą widoczną szarą ścianę (pokrycie, dół obrazu pod chodnikiem) zamiast całego obrazu z białymi bokami.</summary>
    const bool MuralCalaSciana = true;
    // dziedziniec przed siłownią: od lica boku BAR 24 (StreetBuildings „bar” x1 = −1) do lica NUTRITION (x 14.5)
    const float DziedzinecX0 = -1.0f, DziedzinecX1 = 14.5f;
    static readonly string[] MuralPomijaj = { "mural", "drzewo", "tree", "verdant", "palm", "liść", "lisc", "leaf", "krzew", "bush",
                                              "latarn", "lamp", "człowiek", "czlowiek", "npc", "ludzie", "przechod" };

    static void MuralSilowni(Transform cityRoot, Transform rodzic)
    {
        int usuniete = UsunBilbord19PrzySzarejScianie(cityRoot);
        // 11.11 (właściciel 14:43: „napis BRAXHORN GYM ma zrobić ładny w 3D. Tam to usuń”): płaski szyld nad drzwiami wyłączony
        // PRZED muralem (mural przechodzi przez jego miejsce), szyld 3D stawiany po muralu (BraxSzyldGym3D)
        try { BraxSzyldGym3D.ZdejmijPlaski(); } catch (Exception e) { Debug.LogWarning("BRAXHORN_GYM_SZYLD_3D płaski DO POPRAWKI " + e.Message); }
        licoSciany = float.NaN;
        try { MuralSilowniWlasciwy(cityRoot, rodzic, usuniete); }
        finally
        {
            try { opis.Add(BraxSzyldGym3D.Postaw(rodzic, licoSciany)); } catch (Exception e) { Debug.LogWarning("BRAXHORN_GYM_SZYLD_3D DO POPRAWKI " + e.Message); opis.Add("szyld GYM 3D: DO POPRAWKI"); }
        }
    }
    static float licoSciany = float.NaN;

    static void MuralSilowniWlasciwy(Transform cityRoot, Transform rodzic, int usuniete)
    {
        var mat = Grafika("19", 1.5f, out float proporcja);
        if (mat == pusta || !mat.mainTexture)
        {
            string msg = "BRAXHORN_MURAL_19 BRAK ORYGINAŁU (19.jpg). Nie stawiam pustej tablicy ani zamiennika.";
            Debug.LogWarning(msg); opis.Add(msg);
            return;
        }
        var pas = new Bounds(); pas.SetMinMax(new Vector3(-22f, -.5f, 8f), new Vector3(22f, 12f, 26f));   // siłownia + dziedziniec + chodnik
        var sondy = new List<GameObject>();
        var zrodlo = new Dictionary<Collider, Renderer>();
        try
        {
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(rodzic)) continue;
                var rb = r.bounds;
                if (!rb.Intersects(pas) || rb.size.y < .25f) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                string nr = (r.name + " " + (r.transform.parent ? r.transform.parent.name : "")).ToLowerInvariant();
                if (MuralPomijaj.Any(nr.Contains)) continue;
                try
                {
                    var sonda = new GameObject("mural 19: sonda"); sonda.transform.SetParent(r.transform, false);
                    sondy.Add(sonda);
                    var mc = sonda.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                    zrodlo[mc] = r;
                }
                catch (Exception e) { Debug.LogWarning("BRAXHORN_MURAL_19 sonda " + r.name + ": " + e.Message); }
            }
            Physics.SyncTransforms();
            PostawMural19(rodzic, mat, proporcja, pas, zrodlo, usuniete);
        }
        finally
        {
            foreach (var s in sondy) if (s) UnityEngine.Object.DestroyImmediate(s);
            Physics.SyncTransforms();
        }
    }

    static void PostawMural19(Transform rodzic, Material mat, float proporcja, Bounds pas, Dictionary<Collider, Renderer> zrodlo, int usuniete)
    {
        float startZ = pas.max.z, dlug = pas.size.z;
        RaycastHit[] Trafienia(float x, float y)
        {
            var h = Physics.RaycastAll(new Vector3(x, y, startZ), Vector3.back, dlug, ~0, QueryTriggerInteraction.Ignore)
                .Where(t => zrodlo.ContainsKey(t.collider)).ToArray();
            Array.Sort(h, (p, q) => p.distance.CompareTo(q.distance));
            return h;
        }

        // 1) która siatka jest szarą ścianą: największe lico zwrócone do ulicy wśród siatek siłowni (Gym_Mesh_*), zgrubnie co 0.5 m
        var liczba = new Dictionary<Renderer, int>(); var sumaZ = new Dictionary<Renderer, float>();
        for (float x = pas.min.x; x <= pas.max.x; x += .5f)
            for (float y = .25f; y <= pas.max.y; y += .5f)
            {
                // na promień liczy się PIERWSZE lico do ulicy: siatki siłowni osobno (szara Gym_Mesh_37 z = 11.6 zasłania białą
                // warstwę wewnętrzną Gym_Mesh_36 z = 11.3 — obie mają ten sam obrys), dowolnej siatki osobno (zapas)
                bool gymJuz = false, dowolnaJuz = false;
                foreach (var t in Trafienia(x, y))
                {
                    if (t.normal.z < .85f) continue;
                    var r = zrodlo[t.collider];
                    bool gym = r.name.StartsWith("Gym_Mesh_", StringComparison.Ordinal);
                    if ((gym && gymJuz) || (!gym && dowolnaJuz)) continue;
                    if (gym) gymJuz = true; else dowolnaJuz = true;
                    liczba[r] = (liczba.TryGetValue(r, out int c) ? c : 0) + 1;
                    sumaZ[r] = (sumaZ.TryGetValue(r, out float sz) ? sz : 0f) + t.point.z;
                    if (gymJuz && dowolnaJuz) break;
                }
            }
        var silownia = liczba.Where(kv => kv.Key.name.StartsWith("Gym_Mesh_", StringComparison.Ordinal)).OrderByDescending(kv => kv.Value).ToList();
        var dowolna = liczba.OrderByDescending(kv => kv.Value).ToList();
        Renderer sciana = null;
        if (silownia.Count > 0 && silownia[0].Value >= 12) sciana = silownia[0].Key;
        else if (dowolna.Count > 0 && dowolna[0].Value >= 12)
        {
            sciana = dowolna[0].Key;
            Debug.LogWarning("BRAXHORN_MURAL_19 DO POPRAWKI: nie rozpoznano siatki siłowni (Gym_Mesh_*) — największe lico od ulicy: " + sciana.name);
        }
        if (!sciana)
        {
            string msg = "BRAXHORN_MURAL_19 DO POPRAWKI: w pasie przed siłownią brak lica ściany zwróconego do ulicy — mural NIE postawiony";
            Debug.LogWarning(msg); opis.Add(msg); return;
        }
        float lico = sumaZ[sciana] / liczba[sciana];
        licoSciany = lico;

        // 2) siatka co 10 cm na obrysie tej ściany: komórka = mural tylko, gdy promień trafia lico ściany, a przed nim nie ma
        //    (a) niczego w płaszczyźnie ściany (drzwi, szyld, plakat, okno) ani (b) muru/budynku, który ją zasłania od ulicy.
        var sb = sciana.bounds;
        float gx0 = Mathf.Max(sb.min.x, pas.min.x), gx1 = Mathf.Min(sb.max.x, pas.max.x);
        float gy0 = Mathf.Max(sb.min.y, 0f), gy1 = Mathf.Min(sb.max.y, pas.max.y);
        int nx = Mathf.Max(1, Mathf.FloorToInt((gx1 - gx0) / MuralKrok)), ny = Mathf.Max(1, Mathf.FloorToInt((gy1 - gy0) / MuralKrok));
        var komorka = new bool[nx, ny];
        int otwory = 0, zaslonione = 0, trafione = 0;
        bool SzerokiMur(Collider c, float y, float x)
        {
            // Zasłania tylko MUR: ta sama bryła trafiona także 0.75 m w lewo i w prawo oraz 1 m wyżej i 1 m niżej (przy ziemi: 1 i 2 m
            // wyżej) — budynek przed siłownią, ściana boczna dziedzińca. Słup latarni, znak, pień, ławka, donica, skrzynki — nie
            // (mural idzie za nimi, jak farba na murze). Wymiary z GEOMETRII trafień, nie z obrysu scalonej siatki (scalone siatki
            // miasta mają obrys na pół ulicy).
            float yb = y >= 1.1f ? y - 1f : y + 2f;
            foreach (var (px, py) in new[] { (x - .75f, y), (x + .75f, y), (x, y + 1f), (x, yb) })
                if (!Trafienia(px, py).Any(t => t.collider == c)) return false;
            return true;
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                float x = gx0 + (i + .5f) * MuralKrok, y = gy0 + (j + .5f) * MuralKrok;
                if (x < DziedzinecX0 || x > DziedzinecX1) continue;     // 10.11: tylko między licami sąsiadów (bez prześwitów przez ich okna)
                var h = Trafienia(x, y);
                int w = Array.FindIndex(h, t => zrodlo[t.collider] == sciana && t.normal.z > .85f && Mathf.Abs(t.point.z - lico) < .08f);
                if (w < 0) continue;                                    // otwór w samej ścianie
                bool ok = true;
                for (int k = 0; k < h.Length && ok; k++)
                {
                    if (k == w) continue;
                    var rk = zrodlo[h[k].collider];
                    if (rk == sciana) continue;
                    float dz = h[k].point.z - lico;
                    // drzwi Gym_Mesh_24 i szyld Gym_Mesh_48/34 leżą W TEJ SAMEJ płaszczyźnie co ściana (z = 11.6) — kolejność trafień
                    // bywa dowolna, więc otwór niezależnie od kolejności. Duże nakładki (> 25 m²) to warstwa elewacji, nie otwór.
                    if (dz >= -.05f && dz < .30f) { if (rk.bounds.size.x * rk.bounds.size.y <= 25f) { ok = false; otwory++; } }
                    else if (k < w && dz >= .30f && SzerokiMur(h[k].collider, y, x)) { ok = false; zaslonione++; }
                }
                if (ok) { komorka[i, j] = true; trafione++; }
            }

        // 3) największy spójny fragment widocznej szarej ściany (bez pojedynczych prześwitów przez szczeliny)
        var etykieta = new int[nx, ny]; int najw = 0, najwRozm = 0, nastepna = 0;
        var kolejka = new Queue<Vector2Int>();
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                if (!komorka[i, j] || etykieta[i, j] != 0) continue;
                int id = ++nastepna, rozm = 0; etykieta[i, j] = id; kolejka.Enqueue(new Vector2Int(i, j));
                while (kolejka.Count > 0)
                {
                    var c = kolejka.Dequeue(); rozm++;
                    foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        var q = c + d;
                        if (q.x < 0 || q.y < 0 || q.x >= nx || q.y >= ny || !komorka[q.x, q.y] || etykieta[q.x, q.y] != 0) continue;
                        etykieta[q.x, q.y] = id; kolejka.Enqueue(q);
                    }
                }
                if (rozm > najwRozm) { najwRozm = rozm; najw = id; }
            }
        float pole = najwRozm * MuralKrok * MuralKrok;
        if (pole < 6f)
        {
            string msg = "BRAXHORN_MURAL_19 DO POPRAWKI: widoczna część szarej ściany " + sciana.name + " (lico z=" + lico.ToString("F2") +
                         ") ma tylko " + pole.ToString("F1") + " m² — mural NIE postawiony; trafień " + trafione + ", otworów " + otwory + ", zasłoniętych " + zaslonione;
            Debug.LogWarning(msg); opis.Add(msg); return;
        }
        int i0 = nx, i1 = -1, j0 = ny, j1 = -1;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                if (etykieta[i, j] == najw) { i0 = Mathf.Min(i0, i); i1 = Mathf.Max(i1, i); j0 = Mathf.Min(j0, j); j1 = Mathf.Max(j1, j); }
        float x0 = gx0 + i0 * MuralKrok, x1 = gx0 + (i1 + 1) * MuralKrok, y0 = gy0 + j0 * MuralKrok, y1 = gy0 + (j1 + 1) * MuralKrok;

        // 4) 10.11 (właściciel 09:21: „mural na siłowni — zobacz, jak jest ucięty”): w APK 9.11 kadr = obrys widocznej ściany
        //    x −1.9…18.0 (prześwity przez okna sąsiadów) przy proporcji 3:2 → widać było tylko górne 64 % obrazu, a boki z bykami
        //    chowały się za BAR 24 i NUTRITION. Teraz CAŁY obraz bez kadrowania: wysokość = wysokość ściany dziedzińca, szerokość
        //    z proporcji (gdy się nie mieści — szerokość dziedzińca), środek w osi dziedzińca; poza obrazem zostaje szara ściana.
        float imgH = y1 - y0, imgW = imgH * proporcja;
        float ix1, ix0, iy1, iy0;
        if (MuralCalaSciana)
        {
            // 11.11 (właściciel 14:14: „ten mural masz zrobić w końcu prawidłowo”; 6.11: „cała szara ściana, grafika 19, otwór na
            // drzwi”): APK 2bfadac — cały obraz 12.75 × 8.5 m na widocznej ścianie 15.5 m, po bokach 1.4 m gołej białej cegły.
            // Teraz obraz pokrywa CAŁĄ widoczną szarą ścianę od krawędzi do krawędzi: szerokość = szerokość ściany, wysokość
            // z proporcji; góra obrazu przy górnej krawędzi ściany (napis i głowy całe, 3 % nad krawędzią), nadmiar wysokości
            // schodzi pod chodnik (dół obrazu — nogi). Gdyby ściana była wyższa niż obraz — pokrycie po wysokości, boki symetrycznie.
            imgW = x1 - x0; imgH = imgW / proporcja;
            if (imgH < y1 - y0) { imgH = y1 - y0; imgW = imgH * proporcja; }
            ix1 = (x0 + x1) * .5f + imgW * .5f; ix0 = ix1 - imgW; iy1 = y1 + imgH * .03f; iy0 = iy1 - imgH;
        }
        else
        {
            if (imgW > x1 - x0) { imgW = x1 - x0; imgH = imgW / proporcja; }
            ix1 = (x0 + x1) * .5f + imgW * .5f; ix0 = ix1 - imgW; iy1 = y1; iy0 = iy1 - imgH;
        }
        float z = lico + MuralOdstep;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
        for (int j = j0; j <= j1; j++)
        {
            int i = i0;
            while (i <= i1)
            {
                if (etykieta[i, j] != najw) { i++; continue; }
                int a = i; while (i <= i1 && etykieta[i, j] == najw) i++;
                float xa = Mathf.Max(gx0 + a * MuralKrok, ix0), xb = Mathf.Min(gx0 + i * MuralKrok, ix1), ya = Mathf.Max(gy0 + j * MuralKrok, iy0), yb = Mathf.Min(gy0 + (j + 1) * MuralKrok, iy1);
                if (xb - xa < .001f || yb - ya < .001f) continue;
                int n0 = v.Count;
                v.Add(new Vector3(xa, ya, z)); v.Add(new Vector3(xb, ya, z)); v.Add(new Vector3(xb, yb, z)); v.Add(new Vector3(xa, yb, z));
                foreach (var p in v.Skip(n0).Take(4))   // widz patrzy w −z: +x jest po jego LEWEJ — u rośnie w stronę −x (bez lustra)
                    uv.Add(new Vector2((ix1 - p.x) / imgW, (p.y - iy0) / imgH));
                tr.AddRange(new[] { n0, n0 + 1, n0 + 2, n0, n0 + 2, n0 + 3 });   // front +z — do ulicy
            }
        }
        var mesh = new Mesh { name = "BRAXHORN mural 19 — pigment na szarej ścianie siłowni" };
        if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tr, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var mural = new GameObject("BRAXHORN GYM — mural 19 na szarej ścianie (" + sciana.name + ")").transform;
        mural.SetParent(rodzic, false); mural.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        BraxMiastoA.Obiekt(mural, "mural_19_szara_sciana_silowni", mesh, mat);
        postawione++;
        string opis19 = "19: oryginał 19.jpg " + (MuralCalaSciana ? "NA CAŁĄ ŚCIANĘ (pod chodnikiem " + Mathf.Max(0f, y0 - iy0).ToString("F2") + " m dołu obrazu, nad krawędzią " + Mathf.Max(0f, iy1 - y1).ToString("F2") + " m) " : "CAŁY (bez kadrowania) ") + imgW.ToString("F2") + "×" + imgH.ToString("F2") + " m, x=" + ix0.ToString("F2") + ".." + ix1.ToString("F2") +
            ", y=" + iy0.ToString("F2") + ".." + iy1.ToString("F2") + " na " + sciana.name + " (lico z=" + lico.ToString("F2") + "), widoczna ściana x=" + x0.ToString("F1") + ".." + x1.ToString("F1") +
            ", y=" + y0.ToString("F1") + ".." + y1.ToString("F1") + ", " + pole.ToString("F1") + " m², otwory (drzwi/szyld) " + (otwory * MuralKrok * MuralKrok).ToString("F1") +
            " m², zasłonięte murami " + (zaslonione * MuralKrok * MuralKrok).ToString("F1") + " m², pasów " + (tr.Count / 6) + ", usunięte stare tablice " + usuniete;
        opis.Add(opis19);
        Debug.Log("BRAXHORN_MURAL_19 " + opis19);
    }
}

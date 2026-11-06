using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BILLBOARDY OSIEDLA (runda 6; docs/zlecenia/GRAFIKI_ROZMIESZCZENIE.md — część OSIEDLE; wzór: BraxMiastoBillboardy na gałęzi
// miasta). Zasada właściciela: mało, tylko na nośnikach (citylight, rama na ślepej ścianie, tablica na słupach), nigdy na szkle.
//  02 BRAXHORN FITNESS — citylight w zaułku przy wejściu do siłowni A; 04 kopnięcie w worek — rama na ścianie przy wejściu
//  z ulicy do sali bokserskiej; 05 BRAXHORN GYM — rama w środku siłowni A przy recepcji; 13 mistrzyni z pasem — rama przy
//  wejściu do areny; 14 WILLA MISTRZA — tablica na dwóch słupach przy wjeździe do części z willami (B); 08 tygrysica —
//  citylight przy przystanku OSIEDLE A 1. Grafika: Assets/Source/Prywatne/Grafiki/billboardy/NN.jpg (paczka prywatna,
//  rozpakowywana w CI) — brak pliku = pusta rama (ciemna płyta). Każde miejsce sprawdzane fizyką (wolne miejsce; rama tylko
//  na płaskim, nieszklanym murze). Wymiary × K. Log BRAXHORN_BILLBOARDY postawione=… brak_grafiki=… [lista].
public static partial class BraxClaudeOsiedle
{
    const string BillDir = "Assets/Source/Prywatne/Grafiki/billboardy/";
    static int billPost, billBrak; static readonly List<string> billOpis = new List<string>();
    static Material billPusta;

    static Material BillGrafika(string nr, float domyslna, out float proporcja)
    {
        proporcja = domyslna; string plik = BillDir + nr + ".jpg";
        if (File.Exists(plik))
        {
            AssetDatabase.ImportAsset(plik, ImportAssetOptions.ForceSynchronousImport);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(plik);
            if (tex)
            {
                var m = new Material(Shader.Find("Standard")) { name = "Osiedle: billboard " + nr, mainTexture = tex };
                m.SetFloat("_Glossiness", .25f);
                m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.35f, .35f, .35f));   // lekkie podświetlenie
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                proporcja = tex.width / (float)Mathf.Max(1, tex.height);
                return SalonZapisz(m, "billboard " + nr);
            }
        }
        billBrak++;
        return billPusta;
    }
    // plakat: osobna siatka z UV 0…1, przód = n (obraz nieodbity dla patrzącego od strony n)
    static void BillPlakat(Transform g, string nazwa, Vector3 c, Vector3 n, float w, float h, Material m)
    {
        var r = Vector3.Cross(Vector3.up, -n).normalized; var u = Vector3.up; var sb = new SiatkaSalonu();
        sb.Quad(0, c - r * w * .5f - u * h * .5f, c + r * w * .5f - u * h * .5f, c + r * w * .5f + u * h * .5f, c - r * w * .5f + u * h * .5f, n);
        // Dwa lica pylonu mogą mieć tę samą zaokrągloną pozycję. Kierunek i pełna pozycja odróżniają ich siatki.
        var mesh = sb.Zapisz("plakat " + nazwa + " " + c.ToString("F3") + " " + n.ToString("F3"));
        SalonObiekt(nazwa, g, mesh, new[] { m }, Vector3.zero);
    }
    static bool BillWolne(Vector3 p, float rx, float rz, float h, float yaw) =>
        Physics.OverlapBox(p + Vector3.up * (h * .5f + .2f), new Vector3(rx, h * .5f, rz), Quaternion.Euler(0, yaw, 0), ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore).Length == 0;
    static float BillGrunt(float x, float z)
    {
        foreach (var h in Physics.RaycastAll(new Vector3(x, 20f, z), Vector3.down, 40f, ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            if (h.normal.y > .8f && h.point.y < 1.2f) return h.point.y;
        return 0f;
    }

    // citylight dwustronny: cokół, rama, daszek, LED, szyby, plakaty z obu stron, wnętrze białe, kolizja
    static void BillCitylight(Transform rod, Vector3 p, float yaw, string nr, string gdzie)
    {
        var mat = BillGrafika(nr, 1.185f / 1.75f, out float asp);
        float Hv = 1.75f * K, Wv = Mathf.Min(1.185f * K, Hv * asp); Hv = Wv / asp;
        var q = Quaternion.Euler(0f, yaw, 0f); var f = q * Vector3.forward; var r = Vector3.Cross(Vector3.up, -f).normalized; var u = Vector3.up;
        var grafit = Plain("nośnik grafit", new Color(.07f, .07f, .08f), .45f, .6f); var alu = Plain("nośnik aluminium", new Color(.72f, .73f, .75f), .3f, .9f);
        var led = StSwiatlo("nośnik LED", new Color(1f, .97f, .9f)); var bialy = Plain("nośnik wnętrze białe", new Color(.92f, .92f, .9f), .6f);
        float yb = p.y + .3f; var c = new Vector3(p.x, yb + Hv * .5f, p.z);
        OBox(grafit, new Vector3(p.x, p.y + .14f, p.z), new Vector3(Wv + .3f, .28f, .34f), yaw);
        foreach (float s in new[] { -1f, 1f }) OBox(grafit, c + r * s * (Wv * .5f + .06f), new Vector3(.12f, Hv + .14f, .2f), yaw);
        OBox(grafit, new Vector3(p.x, yb - .04f, p.z), new Vector3(Wv + .24f, .08f, .2f), yaw); OBox(grafit, new Vector3(p.x, yb + Hv + .04f, p.z), new Vector3(Wv + .24f, .08f, .2f), yaw);
        OBox(alu, new Vector3(p.x, yb + Hv + .11f, p.z), new Vector3(Wv + .34f, .06f, .3f), yaw);
        OBox(led, new Vector3(p.x, yb + Hv - .01f, p.z), new Vector3(Wv - .04f, .02f, .12f), yaw);
        OBox(bialy, c, new Vector3(Wv, Hv, .04f), yaw);
        var g = new GameObject("BRAXHORN nośnik: citylight " + nr + " (" + gdzie + ")").transform; g.SetParent(Group("Osiedle / nośniki reklamowe"), false);
        foreach (float s in new[] { -1f, 1f })
        {
            OBox(GlassClear, c + f * s * .085f, new Vector3(Wv, Hv, .008f), yaw);
            BillPlakat(g, "citylight " + nr + (s > 0 ? " przód" : " tył"), c + f * s * .06f, f * s, Wv, Hv, mat);
        }
        ColliderRot(c, new Vector3(Wv + .3f, Hv + .6f, .3f), yaw);
        billPost++; billOpis.Add(nr + " citylight @" + p.ToString("F1") + " (" + gdzie + ")" + (mat == billPusta ? " PUSTA RAMA" : ""));
    }
    // rama na murze: szukanie płaskiego, nieszklanego lica (siatka promieni), rama stalowa na kotwach, 2 kinkiety
    static bool BillRama(Vector3 start, Vector3 doMuru, float zasieg, string nr, string gdzie, float maxW, float maxH, float yDol)
    {
        doMuru.y = 0f; doMuru.Normalize(); var r = Vector3.Cross(Vector3.up, doMuru).normalized;
        var traf = new List<float>(); Vector3 n = -doMuru; float lico = 0f;
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
            {
                var o = start + r * Mathf.Lerp(-maxW * .45f, maxW * .45f, i / 3f) + Vector3.up * (yDol + Mathf.Lerp(.1f, maxH - .1f, j / 2f));
                if (!Physics.Raycast(o, doMuru, out var h, zasieg, ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore) || Vector3.Dot(h.normal, n) < .9f) return false;
                var rr = h.collider.GetComponent<Renderer>(); var nm = (h.collider.name + " " + (rr && rr.sharedMaterial ? rr.sharedMaterial.name : "")).ToLowerInvariant();
                if (nm.Contains("szkł") || nm.Contains("szkl") || nm.Contains("glass") || nm.Contains("witryn") || nm.Contains("drzwi") || nm.Contains("door")) return false;
                traf.Add(Vector3.Dot(h.point - start, doMuru));
            }
        traf.Sort(); lico = traf[traf.Count / 2]; if (traf.Any(t => Mathf.Abs(t - lico) > .03f)) return false;
        var mat = BillGrafika(nr, .75f, out float asp);
        float w = maxW, hh = w / asp; if (hh > maxH) { hh = maxH; w = hh * asp; }
        var sc = start + doMuru * lico; sc.y = start.y; var cc = sc + Vector3.up * (yDol + hh * .5f);
        float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        var grafit = Plain("nośnik grafit", new Color(.07f, .07f, .08f), .45f, .6f); var led = StSwiatlo("nośnik LED", new Color(1f, .97f, .9f));
        var g = new GameObject("BRAXHORN nośnik: rama " + nr + " (" + gdzie + ")").transform; g.SetParent(Group("Osiedle / nośniki reklamowe"), false);
        BillPlakat(g, "rama " + nr, cc + n * .06f, n, w, hh, mat);
        OBox(grafit, cc + n * .035f, new Vector3(w + .02f, hh + .02f, .05f), yaw);                                     // płyta tylna
        foreach (float s in new[] { -1f, 1f })
        {
            OBox(grafit, cc + r * s * (w * .5f + .04f) + n * .05f, new Vector3(.08f, hh + .16f, .08f), yaw);            // boki ramy
            OBox(grafit, cc + Vector3.up * s * (hh * .5f + .04f) + n * .05f, new Vector3(w + .16f, .08f, .08f), yaw);     // góra / dół
            var kp = cc + Vector3.up * (hh * .5f + .3f) + r * s * w * .25f;                                              // kinkiet
            OBox(grafit, kp + n * .01f, new Vector3(.14f, .14f, .02f), yaw);
            Tube(grafit, kp + n * .02f, kp + n * .45f + Vector3.up * .08f, .015f, 6);
            OBox(grafit, kp + n * .5f + Vector3.up * .08f, new Vector3(.22f, .07f, .14f), yaw);
            OBox(led, kp + n * .5f + Vector3.up * .04f, new Vector3(.18f, .01f, .1f), yaw);
        }
        billPost++; billOpis.Add(nr + " rama " + w.ToString("F2") + "×" + hh.ToString("F2") + " m @" + cc.ToString("F1") + " (" + gdzie + ")" + (mat == billPusta ? " PUSTA RAMA" : ""));
        return true;
    }
    // tablica na dwóch słupach (fundamenty, rama, płyta tylna, plakat, 2 lampy)
    static void BillTablica(Vector3 p, float yaw, string nr, string gdzie)
    {
        var mat = BillGrafika(nr, 2f, out float asp);
        float w = 3.2f * K, hh = w / asp; if (hh > 1.8f * K) { hh = 1.8f * K; w = hh * asp; }
        var q = Quaternion.Euler(0, yaw, 0); var f = q * Vector3.forward; var r = Vector3.Cross(Vector3.up, -f).normalized;
        var stal = Plain("nośnik stal malowana", new Color(.36f, .37f, .38f), .5f, .7f); var grafit = Plain("nośnik grafit", new Color(.07f, .07f, .08f), .45f, .6f);
        var beton = Plain("nośnik fundament", new Color(.6f, .6f, .58f), .9f); var led = StSwiatlo("nośnik LED", new Color(1f, .97f, .9f));
        float yb = p.y + 1.0f * K; var cc = new Vector3(p.x, yb + hh * .5f, p.z);
        foreach (float s in new[] { -1f, 1f })
        {
            var sp = p + r * s * (w * .5f - .15f);
            Cylinder(beton, sp - Vector3.up * .05f, .25f, .2f, 14, true, true);
            Cylinder(stal, sp + Vector3.up * .15f, .07f, yb + hh - p.y - .1f, 12, true, false);
            Tube(grafit, sp + Vector3.up * (yb + hh - p.y + .05f), sp + Vector3.up * (yb + hh - p.y + .3f) + f * .55f, .02f, 6);
            OBox(grafit, sp + Vector3.up * (yb + hh - p.y + .3f) + f * .6f, new Vector3(.3f, .08f, .16f), yaw);
            OBox(led, sp + Vector3.up * (yb + hh - p.y + .255f) + f * .6f, new Vector3(.25f, .01f, .12f), yaw);
        }
        OBox(grafit, cc - f * .03f, new Vector3(w + .16f, hh + .16f, .06f), yaw);
        var g = new GameObject("BRAXHORN nośnik: tablica " + nr + " (" + gdzie + ")").transform; g.SetParent(Group("Osiedle / nośniki reklamowe"), false);
        BillPlakat(g, "tablica " + nr, cc + f * .005f, f, w, hh, mat);
        ColliderRot(new Vector3(p.x, p.y + (yb + hh - p.y) * .5f, p.z), new Vector3(w + .2f, yb + hh - p.y, .3f), yaw);
        billPost++; billOpis.Add(nr + " tablica " + w.ToString("F2") + "×" + hh.ToString("F2") + " m @" + p.ToString("F1") + " (" + gdzie + ")" + (mat == billPusta ? " PUSTA RAMA" : ""));
    }

    static void BillboardyOsiedla()
    {
        var g0 = group; group = "Osiedle / nośniki reklamowe";
        billPost = billBrak = 0; billOpis.Clear(); Physics.SyncTransforms();
        billPusta = Plain("nośnik pusta rama (bez grafiki)", new Color(.16f, .16f, .17f), .7f);
        void Krok(string n, System.Action a) { try { a(); } catch (System.Exception e) { billOpis.Add(n + ": DO POPRAWKI " + e.Message); } }
        Krok("02 siłownia A", () =>
        {   // zaułek przy wejściu do siłowni A (drzwi x ≈ 92, z −66)
            foreach (var (x, z) in new[] { (88.4f, -63.6f), (95.6f, -63.6f), (84.0f, -63.6f), (99.0f, -63.6f) })
            { var p = new Vector3(x, BillGrunt(x, z), z); if (!BillWolne(p, .9f, .35f, 2.6f, 0f)) continue; BillCitylight(Group(group), p, 0f, "02", "zaułek przy wejściu do siłowni A"); return; }
            billOpis.Add("02: brak wolnego miejsca w zaułku przy siłowni A");
        });
        Krok("05 recepcja siłowni A", () =>
        {   // ściana frontowa od środka, obok recepcji (recepcja x 85, z −68.05; drzwi x ≈ 92)
            foreach (float x in new[] { 81.0f, 78.5f, 88.8f })
                if (BillRama(new Vector3(x, .06f, -68.5f), Vector3.forward, 3f, "05", "siłownia A przy recepcji", 1.4f, 2.0f, 1.0f)) return;
            billOpis.Add("05: brak płaskiej ściany przy recepcji siłowni A");
        });
        Krok("04 sala bokserska", () =>
        {   // ściana zewnętrzna od ulicy B3 (z 78.82), obok drzwi x 84–88
            foreach (float x in new[] { 81.2f, 90.8f, 79.0f, 93.0f })
                if (BillRama(new Vector3(x, .1f, 81.5f), Vector3.back, 4f, "04", "sala bokserska przy wejściu z ulicy", 1.8f, 2.4f, .9f)) return;
            billOpis.Add("04: brak płaskiego muru przy wejściu do sali bokserskiej");
        });
        Krok("13 arena", () =>
        {   // ściana z drzwiami areny (x −88.05…−83.95): najpierw od ulicy B3 (z 78.82), potem od płd. (z 49.18)
            foreach (var (zs, dir) in new[] { (81.5f, Vector3.back), (46.5f, Vector3.forward) })
                foreach (float x in new[] { -91.2f, -80.8f, -93.4f })
                    if (BillRama(new Vector3(x, .1f, zs), dir, 4f, "13", "arena przy wejściu", 1.4f, 2.4f, .9f)) return;
            billOpis.Add("13: brak płaskiego muru przy wejściu do areny");
        });
        Krok("14 wille", () =>
        {   // wjazd do części z willami (rząd płd. ulicy B, przy willi z basenem) — tablica przodem do ulicy
            float zt = StreetZ - Half - 1.2f;
            foreach (float x in new[] { 38f, 34f, 42f, 30f })
            { var p = new Vector3(x, BillGrunt(x, zt), zt); if (!BillWolne(p, 2.3f, .5f, 3.6f, 0f)) continue; BillTablica(p, 0f, "14", "wjazd do części z willami (B)"); return; }
            billOpis.Add("14: brak wolnego miejsca przy wjeździe do willi");
        });
        Krok("08 przystanek A 1", () =>
        {   // obok wiaty OSIEDLE A 1, przodem wzdłuż chodnika
            var st = przystankiLinii.FirstOrDefault(q => q.nazwa == "OSIEDLE A 1"); if (st.nazwa == null) { billOpis.Add("08: brak przystanku OSIEDLE A 1"); return; }
            foreach (float dz in new[] { 3.0f, -3.0f, 4.2f })
            { var p = st.c + st.ruch * dz; p.y = BillGrunt(p.x, p.z); if (!BillWolne(p, .9f, .35f, 2.6f, 0f)) continue; BillCitylight(Group(group), p, 0f, "08", "przystanek OSIEDLE A 1"); return; }
            billOpis.Add("08: brak miejsca przy wiacie OSIEDLE A 1");
        });
        // Six exact owner's graphics, at six exact specified destinations.
        // Five-of-six is NOT a pass; neither is a mounted empty frame.
        var wymagane = new[] { "02", "04", "05", "13", "14", "08" };
        bool Postawiony(string nr) => billOpis.Any(s =>
            s.StartsWith(nr + " citylight ", StringComparison.Ordinal) ||
            s.StartsWith(nr + " rama ", StringComparison.Ordinal) ||
            s.StartsWith(nr + " tablica ", StringComparison.Ordinal));
        var brakujace = wymagane.Where(n => !Postawiony(n)).ToArray();
        string l = "BRAXHORN_BILLBOARDY postawione=" + billPost + " brak_grafiki=" + billBrak + " (osiedle: 02, 04, 05, 13, 14, 08; mało, tylko na nośnikach) [" + string.Join("; ", billOpis) + "]";
        rep.notes.Add(l.Replace("BRAXHORN_BILLBOARDY ", "billboardy: "));
        if (brakujace.Length > 0 || billBrak > 0 || billPost != wymagane.Length)
        {
            string problem = "DO POPRAWKI: nośniki osiedla: " + billPost + "/" + wymagane.Length +
                ", brak wymaganych miejsc: " + (brakujace.Length == 0 ? "0" : string.Join(", ", brakujace)) +
                ", puste ramy bez grafiki: " + billBrak;
            rep.notes.Add(problem);
            Debug.LogWarning("BRAXHORN_BILLBOARDY " + problem + " " + l);
        }
        else Debug.Log(l);
        group = g0;
    }
}

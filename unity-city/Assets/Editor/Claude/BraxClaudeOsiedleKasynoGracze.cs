using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// KASYNO — GRACZE (właściciel 3.11 15:17: „gdzie są ludzie grający w pokera i w ogóle w automaty?”). Stoły sali gier są w siatce
// osiedla Codexa (bez nazw), więc wyszukiwane z geometrii: poziome lica (normalna w górę) na wysokości blatu stołu do gry,
// zebrane w siatkę 0.25 m i połączone w skupiska; skupisko 1.1–3.6 m = stół (bar i lady odpadają po rozmiarze).
// Przy każdym stole: 3–4 gracze na krzesłach (siedzą przodem do środka, wzdłuż dłuższych boków) i krupier stojący przy
// jednym z końców. Log BRAXHORN_KASYNO_GRACZE.
public static partial class BraxClaudeOsiedle
{
    static void KasynoGracze()
    {
        var hala = MinMax(68.5f, 103.5f, -.5f, 4.5f, -51.3f, -34.7f);
        var est = GameObject.Find("braxhorn-estate")?.transform;
        if (!est) { Debug.Log("BRAXHORN_KASYNO_GRACZE brak siatki osiedla"); return; }
        const float kom = .25f; var komorki = new Dictionary<(int, int), float>();
        foreach (var mf in est.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mf.sharedMesh || !mr.bounds.Intersects(hala)) continue;
            Vector3[] v; try { v = mf.sharedMesh.vertices; } catch { continue; }
            var t = mf.sharedMesh.triangles; var w = mf.transform.localToWorldMatrix;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 a = w.MultiplyPoint3x4(v[t[i]]), b = w.MultiplyPoint3x4(v[t[i + 1]]), c = w.MultiplyPoint3x4(v[t[i + 2]]);
                var n = Vector3.Cross(b - a, c - a); float pole = n.magnitude * .5f; if (pole < 1e-4f) continue;
                if (n.y / (2f * pole) < .96f) continue;                       // lico poziome, w górę
                var s = (a + b + c) / 3f; if (s.y < .65f || s.y > 1.25f || !hala.Contains(s)) continue;
                var key = (Mathf.FloorToInt(s.x / kom), Mathf.FloorToInt(s.z / kom));
                komorki[key] = komorki.TryGetValue(key, out var y0) ? Mathf.Max(y0, s.y) : s.y;
            }
        }
        // skupiska (sąsiedztwo 8 komórek)
        var odw = new HashSet<(int, int)>(); var stoly = new List<(Vector3 c, Vector3 roz, float y)>();
        foreach (var k0 in komorki.Keys)
        {
            if (odw.Contains(k0)) continue;
            var q = new Queue<(int, int)>(); q.Enqueue(k0); odw.Add(k0); var lista = new List<(int, int)>();
            while (q.Count > 0)
            {
                var k = q.Dequeue(); lista.Add(k);
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    {
                        var kk = (k.Item1 + dx, k.Item2 + dz);
                        if (komorki.ContainsKey(kk) && !odw.Contains(kk) && Mathf.Abs(komorki[kk] - komorki[k]) < .12f) { odw.Add(kk); q.Enqueue(kk); }
                    }
            }
            float x0 = lista.Min(k => k.Item1) * kom, x1 = (lista.Max(k => k.Item1) + 1) * kom, z0 = lista.Min(k => k.Item2) * kom, z1 = (lista.Max(k => k.Item2) + 1) * kom;
            float dl = Mathf.Max(x1 - x0, z1 - z0), sz = Mathf.Min(x1 - x0, z1 - z0);
            if (dl < 1.1f || dl > 3.6f || sz < .7f || lista.Count < 10) continue;
            stoly.Add((new Vector3((x0 + x1) * .5f, 0f, (z0 + z1) * .5f), new Vector3(x1 - x0, 0f, z1 - z0), lista.Average(k => komorki[k])));
        }
        var g = Group("Osiedle A / kasyno — gracze"); var rnd = new System.Random(7070); int siedzi = 0, krupierzy = 0;
        foreach (var st in stoly.Take(12))
        {
            bool wzdluzX = st.roz.x >= st.roz.z; float dl = wzdluzX ? st.roz.x : st.roz.z, sz = wzdluzX ? st.roz.z : st.roz.x;
            Vector3 os = wzdluzX ? Vector3.right : Vector3.forward, bok = wzdluzX ? Vector3.forward : Vector3.right;
            float ySiedz = st.y - .375f;   // blat 0.76 × K, siedzisko 0.47 × K
            int ile = 0;
            foreach (int strona in new[] { -1, 1 })
                foreach (float u in new[] { -.28f, .28f })
                {
                    if (rnd.NextDouble() < .2) continue;   // nie każde miejsce zajęte
                    var p = st.c + os * (u * dl) + bok * strona * (sz * .5f + .42f); p.y = ySiedz;
                    var m = Siedzacy(RolaImpreza, g, "Kasyno: gracz", p, -bok * strona, rnd, new[] { "sit_chair_idle_neutral_02", "sit_chair_breathe_01" });
                    if (m) { m.wnetrze = true; siedzi++; ile++; }
                }
            var pk = st.c + os * (dl * .5f + .45f); pk.y = 0f;
            if (Wolne(pk))
            {
                var kr = Obsada("kasyno — krupier", RolaRecepcja, "kasyno", pk, Quaternion.LookRotation(-os).eulerAngles.y, rnd, new[] { "idle_neutral_01", "gestic_talk_neutral_01" }, .062f);
                if (kr) { kr.wnetrze = true; krupierzy++; }
            }
        }
        Debug.Log("BRAXHORN_KASYNO_GRACZE stoły=" + stoly.Count + " gracze_siedzą=" + siedzi + " krupierzy=" + krupierzy + " [" + string.Join("; ", stoly.Take(12).Select(s => s.c.ToString("F1") + " " + s.roz.x.ToString("F1") + "×" + s.roz.z.ToString("F1") + " y" + s.y.ToString("F2"))) + "]");
    }
}

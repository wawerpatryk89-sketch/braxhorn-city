using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// MISY DRZEW W CAŁYM MIEŚCIE (właściciel 3.11 10:53: „przy każdej palmie, drzewie, jeżeli rośnie w chodniku czy w czymś,
// ma być mały kwadracik z trawnikiem”). Dla każdego drzewa / palmy stojącego na chodniku, placu lub asfalcie (nie na trawie,
// ziemi, piasku): nawierzchnia pod nim WYCINANA (kwadrat 1.6 m × K — BraxWycinanieSiatki, bez nakładania), w otworze
// obrzeże z granitu (4 krawężniki, wierzch 2 cm nad nawierzchnią) i trawnik miasta 3 cm niżej. Gdy nawierzchni nie da się
// wyciąć (siatka nieczytelna, inna płaszczyzna) — misa NIE powstaje (żadnej nakładki), drzewo w logu.
// Budżet: najwyżej 400 drzew i 150 s. Wołane przed BraxClaudeSiatki (siatki jeszcze czytelne). Log BRAXHORN_MISY_DRZEW.
public static class BraxMisyDrzew
{
    const float K = 1.294f;
    static readonly Regex Drzewo = new Regex(@"drzew|\btree|palm", RegexOptions.IgnoreCase);
    static readonly Regex NieDrzewo = new Regex(@"donic|doniczk|\bpot\b|kwiat|krzew|bush|shrub|kolizja|collider|misa|krata|sylwet|pierścień|pierscien", RegexOptions.IgnoreCase);
    static readonly Regex Zielen = new Regex(@"traw|grass|ziel|lawn|gleb|ziemi|soil|forest|leaf|okryw|piasek|piask|sand|terrain|teren|plaż|plaz|wyspa", RegexOptions.IgnoreCase);
    static readonly Regex Twarda = new Regex(@"chodnik|płyt|plyt|granit|bruk|kostk|paving|pavement|sidewalk|asfalt|asphalt|jezdni|road|beton|concrete|plac|tile|kamie|stone|nawierzch|deptak|promenad|bulwar", RegexOptions.IgnoreCase);
    const string Folder = "Assets/PlayableGenerated/MisyDrzew";

    public static void Wykonaj(Scene scena)
    {
        var stoper = System.Diagnostics.Stopwatch.StartNew();
        Physics.SyncTransforms();
        var wszystkie = scena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r && r.enabled).ToArray();
        // drzewa: najwyższy pasujący węzeł (bez pasującego przodka), z rendererami
        var kand = scena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => Drzewo.IsMatch(t.name) && !NieDrzewo.IsMatch(t.name) && t.GetComponentInChildren<Renderer>(true)).ToList();
        var zb = new HashSet<Transform>(kand);
        var drzewa = kand.Where(t => { for (var q = t.parent; q; q = q.parent) if (zb.Contains(q)) return false; return true; }).ToList();

        var granit = BraxCityMaterials.Surface("Misa drzewa: obrzeże (granit płomieniowany)", new Color(.62f, .61f, .58f), .7f, 0f);
        var trawa = BraxRegionTrawa.Material;
        var biezace = new Dictionary<MeshFilter, Mesh>();   // renderer nawierzchni → aktualna (wycięta) siatka
        var korzen = new GameObject("CLAUDE misy drzew (trawnik w wycięciu nawierzchni)").transform;
        SceneManager.MoveGameObjectToScene(korzen.gameObject, scena);
        int zrobione = 0, naTrawie = 0, nieznane = 0, bezCiecia = 0, juzMisa = 0, budzet = 0; var nieWyciete = new List<string>();
        float a = .8f * K, bw = .12f * K;
        var srodki = new List<Vector3>();   // misy już zrobione (dwa drzewa obok siebie — jedna misa, bez nachodzenia krawężników)

        foreach (var t in drzewa)
        {
            if (zrobione >= 400 || stoper.Elapsed.TotalSeconds > 150) { budzet++; continue; }
            var rs = t.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y < 2f) continue;                                         // nie drzewo (sadzonka, dekoracja)
            var wlasne = new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
            var p = new Vector3(t.position.x, b.min.y, t.position.z);
            RaycastHit grunt = default; bool jest = false;
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            { if (wlasne.Contains(h.collider) || h.normal.y < .9f) continue; grunt = h; jest = true; break; }
            if (!jest) { nieznane++; continue; }
            if (srodki.Any(c => Mathf.Abs(c.x - p.x) < 2f * a + .05f && Mathf.Abs(c.z - p.z) < 2f * a + .05f)) { juzMisa++; continue; }
            float y = grunt.point.y;
            // nawierzchnie pod drzewem: renderery (nie drzewa) z obrysem nad punktem i wierzchem przy y
            var pod = wszystkie.Where(r => !r.transform.IsChildOf(t) && r.bounds.min.x <= p.x && r.bounds.max.x >= p.x && r.bounds.min.z <= p.z && r.bounds.max.z >= p.z
                                            && Mathf.Abs(r.bounds.max.y - y) < .12f).ToList();
            string Nazwy(Renderer r) => r.name + " " + string.Join(" ", r.sharedMaterials.Where(m => m).Select(m => m.name));
            if (pod.Any(r => Zielen.IsMatch(Nazwy(r)))) { naTrawie++; continue; }
            var twarde = pod.Where(r => Twarda.IsMatch(Nazwy(r))).ToList();
            if (twarde.Count == 0) { nieznane++; continue; }
            if (wszystkie.Any(r => r && (r.name.IndexOf("misa", StringComparison.OrdinalIgnoreCase) >= 0 || r.name.IndexOf("krata", StringComparison.OrdinalIgnoreCase) >= 0)
                                 && (r.bounds.center - p).sqrMagnitude < 1.5f * 1.5f)) { juzMisa++; continue; }
            // wycięcie kwadratu a × a w każdej twardej nawierzchni w płaszczyźnie y (± 1.5 cm)
            var gorne = new List<Vector2[]> {
                new[] { new Vector2(p.x - a, p.z - a), new Vector2(p.x + a, p.z - a), new Vector2(p.x + a, p.z + a) },
                new[] { new Vector2(p.x - a, p.z - a), new Vector2(p.x + a, p.z + a), new Vector2(p.x - a, p.z + a) } };
            bool wyciete = false;
            foreach (var r in twarde)
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                if (!biezace.TryGetValue(mf, out var mesh)) mesh = mf.sharedMesh;
                if (!mesh.isReadable) continue;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    Mesh nowa;
                    try { nowa = BraxWycinanieSiatki.OdejmijTrojkaty(mesh, r.transform.localToWorldMatrix, sub, 1, 1, y, .015f, gorne, out int ch, out _, out _); }
                    catch (Exception e) { Debug.LogWarning("BRAXHORN_MISY_DRZEW DO POPRAWKI " + r.name + ": " + e.Message); nowa = null; }
                    if (!nowa) continue;
                    if (biezace.TryGetValue(mf, out var stara) && stara && stara != mf.sharedMesh) UnityEngine.Object.DestroyImmediate(stara);
                    mesh = nowa; biezace[mf] = nowa; wyciete = true;
                }
            }
            if (!wyciete) { bezCiecia++; if (nieWyciete.Count < 20) nieWyciete.Add(t.name + " @" + p.ToString("F0") + " [" + string.Join(", ", twarde.Take(2).Select(r => r.name)) + "]"); continue; }
            // obrzeże (4 krawężniki po 2 elementy, fuga 4 mm) i trawnik 3 cm niżej
            var obrz = new BraxRegionMeble.Bud(); var traw = new BraxRegionMeble.Bud();
            float x0 = p.x - a, x1 = p.x + a, z0 = p.z - a, z1 = p.z + a;
            foreach (var (u0, u1, v0, v1, wzdX) in new[] { (x0, x1, z0, z0 + bw, true), (x0, x1, z1 - bw, z1, true), (x0, x0 + bw, z0 + bw, z1 - bw, false), (x1 - bw, x1, z0 + bw, z1 - bw, false) })
            {
                float mid = wzdX ? (u0 + u1) * .5f : (v0 + v1) * .5f;
                if (wzdX) { obrz.MinMax(new Vector3(u0, y - .15f, v0), new Vector3(mid - .002f, y + .02f, v1)); obrz.MinMax(new Vector3(mid + .002f, y - .15f, v0), new Vector3(u1, y + .02f, v1)); }
                else { obrz.MinMax(new Vector3(u0, y - .15f, v0), new Vector3(u1, y + .02f, mid - .002f)); obrz.MinMax(new Vector3(u0, y - .15f, mid + .002f), new Vector3(u1, y + .02f, v1)); }
            }
            float ci = a - bw;
            traw.Tri(new Vector3(p.x - ci, y - .03f, p.z - ci), new Vector3(p.x - ci, y - .03f, p.z + ci), new Vector3(p.x + ci, y - .03f, p.z + ci));
            traw.Tri(new Vector3(p.x - ci, y - .03f, p.z - ci), new Vector3(p.x + ci, y - .03f, p.z + ci), new Vector3(p.x + ci, y - .03f, p.z - ci));
            var misa = new GameObject("Misa drzewa (" + t.name + ")").transform; misa.SetParent(korzen, false);
            var go1 = obrz.Obiekt("Misa drzewa: obrzeże", misa, granit, true, false);
            var go2 = traw.Obiekt("Misa drzewa: trawnik", misa, trawa, false, false);
            foreach (var g in new[] { go1, go2 }) if (g) foreach (var mr in g.GetComponentsInChildren<MeshRenderer>(true)) mr.shadowCastingMode = ShadowCastingMode.Off;
            srodki.Add(p); zrobione++;
        }
        // zapis wyciętych siatek (raz na renderer)
        if (biezace.Count > 0)
        {
            if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets", "PlayableGenerated");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/PlayableGenerated", "MisyDrzew");
            int i = 0;
            foreach (var kv in biezace)
            {
                if (!kv.Key || !kv.Value) continue;
                kv.Value.name = kv.Key.sharedMesh.name + " (misy drzew)";
                BraxZapisAssetu.Utworz(kv.Value, Folder + "/nawierzchnia_" + (i++) + ".asset");
                kv.Key.sharedMesh = kv.Value;
            }
        }
        string linia = "BRAXHORN_MISY_DRZEW drzew=" + drzewa.Count + " misy=" + zrobione + " na_trawie=" + naTrawie + " już_misa=" + juzMisa + " nieznane_podłoże=" + nieznane
            + " bez_wycięcia=" + bezCiecia + " poza_budżetem=" + budzet + " siatek_wyciętych=" + biezace.Count + " czas=" + stoper.Elapsed.TotalSeconds.ToString("F0") + " s"
            + (nieWyciete.Count > 0 ? " [nie wycięte: " + string.Join("; ", nieWyciete) + "]" : "");
        if (bezCiecia > 0) Debug.LogWarning(linia); else Debug.Log(linia);
    }
}

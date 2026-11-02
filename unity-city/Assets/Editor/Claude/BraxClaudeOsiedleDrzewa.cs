using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// DRZEWA OSIEDLA — build 14 (zdjęcia właściciela z buildu 12: 22:25 przy parkingu, 22:56 przy ul. Bydgoskiej, 20:37 korony
// w dachach). Trzy rzeczy:
//  1) „Płaskie trójkątne liście”: wszystkie drzewa osiedla (park, ulice, działki — jeden model drzewo_verdant) miały liście
//     jako romby z dwóch trójkątów wycięte z liścia atlasu, powiększone przy przerzedzaniu → z bliska low-poly. Teraz korony
//     z kart-gałązek (dziesiątki małych liści na pędach; unity-tools/claude/drzewo_klaster.py) w tych samych miejscach korony:
//     ładne z bliska i z daleka, ~10× mniej trójkątów (LOD0 3.5 tys. zamiast 46 tys.). Pień bez zmian, w dalszych LOD
//     pień uproszczony (444 zamiast 2873 trójkątów). Krzewy z liści drzew — te same gałązki.
//  2) Stare drzewa Codexa w parku Wiśniowym (EstateMesh_439…466: warstwa płaskich trójkątów bez tekstury) — usunięte,
//     w ich miejscu drzewa jak w parku.
//  3) Korony w dachach: mapa wysokości wszystkich budynków (stare domy A, nowe wille, sklepy, hale — prawdziwe trójkąty
//     siatek, nie tylko kolizje) co 1 m; korona (z 1 m zapasu) nie może nachodzić na budynek wyższy niż jej spód →
//     drzewo przesuwane na wolny trawnik do 4 m, zmniejszane albo usuwane.
public static partial class BraxClaudeOsiedle
{
    [Serializable] sealed class KlasterInfo { public float meanGamma, meanLinear; }

    static Mesh[] klasterLiscie;
    static Mesh klasterPien;
    static Material klasterMat;

    static bool KlasterWczytaj()
    {
        if (klasterMat) return true;
        var src = Model("drzewo_klaster");
        if (!src) { rep.notes.Add("drzewa: brak drzewo_klaster.glb"); return false; }
        var mfs = src.GetComponentsInChildren<MeshFilter>(true);
        Mesh M(string n) => mfs.FirstOrDefault(f => f.name == n)?.sharedMesh;
        klasterLiscie = new[] { M("liscie_0"), M("liscie_1"), M("liscie_2"), M("liscie_3") };
        klasterPien = M("pien_lod");
        if (klasterLiscie.Any(m => !m)) { rep.notes.Add("drzewa: drzewo_klaster bez siatek liscie_0..3"); return false; }
        const string atlas = Mdl + "drzewo_klaster/galazki.png";
        var imp = AssetImporter.GetAtPath(atlas) as TextureImporter;
        if (!imp) { rep.notes.Add("drzewa: brak atlasu gałązek " + atlas); return false; }
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f;
        imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Compressed;
        imp.SaveAndReimport();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas);
        var info = new KlasterInfo { meanGamma = .11f, meanLinear = .0142f };
        try { var js = Mdl + "drzewo_klaster/drzewo_klaster.json"; if (File.Exists(js)) JsonUtility.FromJsonOverwrite(File.ReadAllText(js), info); } catch { }
        var sh = Shader.Find("Braxhorn/OsiedleLiscie");
        if (!sh || !tex) { rep.notes.Add("drzewa: brak shadera liści albo atlasu"); return false; }
        // ten sam shader i ta sama zieleń co dotąd (_Leaf, _Leaf2, wnętrze korony) — inne tylko karty i średnia jasność atlasu
        Material wzor = mats.TryGetValue("drzewa", out var d) ? d : null;
        var m = wzor ? new Material(wzor) : new Material(sh);
        m.name = "Osiedle: liście drzew (gałązki)"; m.shader = sh; m.enableInstancing = true; m.mainTexture = tex;
        if (!wzor) { m.SetColor("_Leaf", new Color(.310f, .522f, .147f)); m.SetColor("_Leaf2", new Color(.408f, .553f, .164f)); m.SetFloat("_InnerDark", .35f); m.SetFloat("_Recolor", .85f); }
        m.SetFloat("_MeanGamma", info.meanGamma); m.SetFloat("_MeanLinear", info.meanLinear); m.SetFloat("_Cutoff", .45f);
        var b = klasterLiscie[0].bounds;
        m.SetVector("_Center", b.center + new Vector3(0, b.extents.y * .15f, 0)); m.SetVector("_Radius", b.extents);
        AssetDatabase.CreateAsset(m, Out + "/liscie_galazki.mat"); mats["drzewa gałązki"] = m; tile[m] = 1f;
        klasterMat = m;
        return true;
    }

    // Podmiana liści jednego drzewa (dzieci liscie_0..3 z drzewo_verdant — ten sam układ współrzędnych siatek).
    static bool KlasterNaDrzewo(GameObject t)
    {
        if (!t || !klasterMat) return false;
        var rs = t.GetComponentsInChildren<Renderer>(true);
        Renderer R(string n) => rs.FirstOrDefault(r => r.name == n);
        var pien = R("pien"); if (!pien) return false;
        var lv = new Renderer[4]; bool any = false;
        for (int k = 0; k < 4; k++)
        {
            var r = R("liscie_" + k); lv[k] = r; if (!r) continue;
            var mf = r.GetComponent<MeshFilter>(); if (!mf) continue;
            mf.sharedMesh = klasterLiscie[k]; r.sharedMaterial = klasterMat; any = true;
            r.shadowCastingMode = k <= 1 ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
        }
        if (!any) return false;
        Renderer pienLod = pien;
        if (klasterPien)
        {
            var g = new GameObject("pien_lod"); g.transform.SetParent(pien.transform.parent, false);
            g.transform.localPosition = pien.transform.localPosition; g.transform.localRotation = pien.transform.localRotation; g.transform.localScale = pien.transform.localScale;
            g.AddComponent<MeshFilter>().sharedMesh = klasterPien;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = pien.sharedMaterials; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
            pienLod = mr;
        }
        var lod = t.GetComponent<LODGroup>(); if (!lod) lod = t.AddComponent<LODGroup>();
        // progi przy lodBias 1 (drzewo ~9 m): LOD0 do ~20 m, LOD1 do ~45 m, LOD2 (pień uproszczony) do ~110 m, LOD3 do ~700 m
        var lods = new List<LOD>();
        if (lv[0]) lods.Add(new LOD(.40f, new[] { pien, lv[0] }));
        if (lv[1]) lods.Add(new LOD(.18f, new[] { pien, lv[1] }));
        if (lv[2]) lods.Add(new LOD(.075f, new[] { pienLod, lv[2] }));
        if (lv[3]) lods.Add(new LOD(.012f, new[] { pienLod, lv[3] }));
        lod.SetLODs(lods.ToArray()); lod.fadeMode = LODFadeMode.CrossFade; lod.animateCrossFading = true; lod.RecalculateBounds();
        return true;
    }

    static void DrzewaOsiedla(Transform estate, Transform physics)
    {
        group = "Osiedle / drzewa (gałązki)";
        if (!KlasterWczytaj()) { Debug.Log("BRAXHORN_DRZEWA zamienione=0 usunięte_z_dachów=0 (brak modelu drzewo_klaster)"); return; }
        // 2) stare drzewa Codexa: pień 439–445, gałązki 446–452, liście 453–459, płaskie trójkąty 460–466 (7 drzew)
        var stare = new List<Vector3>();
        if (estate)
        {
            var codex = estate.GetComponentsInChildren<MeshRenderer>(true).Where(r =>
            {
                if (!r || !r.name.StartsWith("EstateMesh_")) return false;
                return int.TryParse(r.name.Substring(11), out var id) && id >= 439 && id <= 466;
            }).ToList();
            foreach (var r in codex.Where(r => int.TryParse(r.name.Substring(11), out var id) && id <= 445))
                stare.Add(new Vector3(r.bounds.center.x, 0, r.bounds.center.z));
            foreach (var r in codex) if (r) UnityEngine.Object.DestroyImmediate(r.gameObject);
        }
        int kodeks = 0;
        foreach (var p in stare)
        {
            // nowe drzewo z kolizją pnia (w tym samym miejscu co pień oryginału)
            if (Tree(new Vector3(p.x, 0, p.z), 8.6f + Mathf.Abs(p.x * 1.7f + p.z) % 1.4f, (p.x * 37f + p.z * 11f) % 360f)) kodeks++;
        }
        // 1) gałązki zamiast trójkątów: wszystkie drzewa z rejestru (park, ulice, działki, aleje, za płotem, Codexa)
        int zamienione = 0;
        foreach (var tr in trees) if (tr != null && tr.go && KlasterNaDrzewo(tr.go)) zamienione++;
        // krzewy z liści drzewa (Bush): ta sama korona w skali krzewu — gałązki LOD3 (karty po ~30 cm, liście ~5 cm)
        int krzewy = 0;
        if (root)
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(q => q.name == "Krzew (liście)").ToList())
            {
                var mf = t.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(); if (!mf || !mf.sharedMesh) continue;
                var r = mf.GetComponent<MeshRenderer>();
                var oldB = mf.sharedMesh.bounds; var nb = klasterLiscie[3].bounds;
                float wOld = Mathf.Max(oldB.size.x, oldB.size.z) * mf.transform.localScale.x;
                float s = wOld / Mathf.Max(.1f, Mathf.Max(nb.size.x, nb.size.z));
                float sy = mf.transform.localScale.y / Mathf.Max(1e-4f, mf.transform.localScale.x);
                mf.sharedMesh = klasterLiscie[3]; if (r) r.sharedMaterial = klasterMat;
                mf.transform.localScale = new Vector3(s, s * sy, s);
                mf.transform.localPosition = -Vector3.Scale(new Vector3(nb.center.x, nb.min.y, nb.center.z), mf.transform.localScale) - Vector3.up * .05f;
                var lg = t.GetComponent<LODGroup>(); if (lg) { lg.fadeMode = LODFadeMode.CrossFade; lg.animateCrossFading = true; lg.RecalculateBounds(); }
                krzewy++;
            }
        // 3) korony z dala od budynków (min. 1 m od ściany i dachu)
        var (przes, zmn, usun, mapa) = KoronyOdBudynkow(estate, physics);
        rep.notes.Add("drzewa: " + zamienione + " koron z gałązek (było: trójkąty), " + krzewy + " krzewów, " + kodeks + " drzew zamiast starych Codexa; korony przy budynkach: przesunięte " + przes + ", zmniejszone " + zmn + ", usunięte " + usun);
        Debug.Log("BRAXHORN_DRZEWA zamienione=" + zamienione + " usunięte_z_dachów=" + usun + " przesunięte=" + przes + " zmniejszone=" + zmn +
                  " krzewy=" + krzewy + " stare_codexa=" + stare.Count + "→" + kodeks + " komórek_budynków=" + mapa + " (liście: gałązki, LOD0 " + klasterLiscie[0].GetIndexCount(0) / 3 + " trójkątów)");
    }

    // Mapa wysokości budynków: komórka 1 m → najwyższy punkt geometrii budynku (próbkowane trójkąty powyżej 1.8 m).
    const float MB_X0 = -140f, MB_Z0 = -260f, MB_C = 1f; const int MB_W = 280, MB_H = 390;
    static float[] mapaBud;

    static int ZbudujMapeBudynkow(Transform estate)
    {
        mapaBud = new float[MB_W * MB_H];
        for (int i = 0; i < mapaBud.Length; i++) mapaBud[i] = float.NegativeInfinity;
        var pomin = new HashSet<Transform>();
        foreach (var tr in trees) if (tr != null && tr.go) pomin.Add(tr.go.transform);
        bool Roslina(Transform t)
        {
            for (var p = t; p; p = p.parent)
            {
                if (pomin.Contains(p)) return true;
                string n = p.name;
                if (n.StartsWith("Drzewo") || n.StartsWith("Krzew") || n.StartsWith("Świerk") || n.StartsWith("Tuja") || n.StartsWith("Kraniec") || n.StartsWith("Kępy") || n == "Trawa 3D" || n == "Kwiaty rabat") return true;
                if (p.GetComponent<BraxAuto>() || p.GetComponent<BraxPieszy>()) return true;
            }
            return false;
        }
        int komorki = 0;
        void Trojkat(Vector3 a, Vector3 bb, Vector3 c)
        {
            if (a.y < 1.8f && bb.y < 1.8f && c.y < 1.8f) return;
            float l = Mathf.Max((a - bb).magnitude, Mathf.Max((bb - c).magnitude, (c - a).magnitude));
            int n = Mathf.Clamp(Mathf.CeilToInt(l / .7f), 1, 60);
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n - i; j++)
                {
                    float u = i / (float)n, q = j / (float)n;
                    var p = a + (bb - a) * u + (c - a) * q;
                    if (p.y < 1.8f) continue;
                    int cx = Mathf.FloorToInt((p.x - MB_X0) / MB_C), cz = Mathf.FloorToInt((p.z - MB_Z0) / MB_C);
                    if (cx < 0 || cz < 0 || cx >= MB_W || cz >= MB_H) continue;
                    int id = cz * MB_W + cx;
                    if (float.IsNegativeInfinity(mapaBud[id])) komorki++;
                    if (p.y > mapaBud[id]) mapaBud[id] = p.y;
                }
        }
        bool Roslinny(Material m) => m && m.shader && (m.shader.name.Contains("Liscie") || m.shader.name.Contains("Kep") || m.shader.name.Contains("Trawa") || m.shader.name.Contains("Dal"));
        // a) gotowe siatki: stare osiedle A (Codex) i modele w CLAUDE (bez roślin, aut, ludzi)
        var roots = new List<Transform>(); if (estate) roots.Add(estate); if (root) roots.Add(root);
        foreach (var rt in roots)
            foreach (var mf in rt.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mr.enabled || !mf.sharedMesh) continue;
                var b = mr.bounds;
                if (b.max.y < 2.2f || Mathf.Max(b.size.x, b.size.z) < .9f) continue;           // płaskie i cienkie (słupy, latarnie)
                if (mr.sharedMaterials.Any(Roslinny) || Roslina(mf.transform)) continue;
                Vector3[] v; int[] tri;
                try { v = mf.sharedMesh.vertices; tri = mf.sharedMesh.triangles; } catch { continue; }
                if (v == null || tri == null || v.Length == 0) continue;
                var M = mf.transform.localToWorldMatrix;
                var w = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) w[i] = M.MultiplyPoint3x4(v[i]);
                for (int k = 0; k + 2 < tri.Length; k += 3) Trojkat(w[tri[k]], w[tri[k + 1]], w[tri[k + 2]]);
            }
        // b) zabudowa CLAUDE jeszcze w paczkach geometrii (wille, sklepy, hale, portal tunelu — scalane później w Flush)
        foreach (var kv in batches)
        {
            if (Roslinny(kv.Key.Item2)) continue;
            var bt = kv.Value;
            for (int k = 0; k + 2 < bt.i.Count; k += 3) Trojkat(bt.v[bt.i[k]], bt.v[bt.i[k + 1]], bt.v[bt.i[k + 2]]);
        }
        return komorki;
    }

    // Ile komórek budynku wchodzi w koronę (elipsa z granic liści + 1 m zapasu, budynek wyżej niż spód korony).
    static int KoronaWBudynku(Bounds korona)
    {
        if (mapaBud == null) return 0;
        float ex = korona.extents.x + 1f, ez = korona.extents.z + 1f, spod = korona.min.y + .2f;
        int n = 0;
        int x0 = Mathf.FloorToInt((korona.center.x - ex - MB_X0) / MB_C), x1 = Mathf.FloorToInt((korona.center.x + ex - MB_X0) / MB_C);
        int z0 = Mathf.FloorToInt((korona.center.z - ez - MB_Z0) / MB_C), z1 = Mathf.FloorToInt((korona.center.z + ez - MB_Z0) / MB_C);
        for (int cz = Mathf.Max(0, z0); cz <= Mathf.Min(MB_H - 1, z1); cz++)
            for (int cx = Mathf.Max(0, x0); cx <= Mathf.Min(MB_W - 1, x1); cx++)
            {
                float h = mapaBud[cz * MB_W + cx]; if (h < spod) continue;
                float px = MB_X0 + (cx + .5f) * MB_C - korona.center.x, pz = MB_Z0 + (cz + .5f) * MB_C - korona.center.z;
                if ((px * px) / (ex * ex) + (pz * pz) / (ez * ez) <= 1f) n++;
            }
        return n;
    }

    static (int, int, int, int) KoronyOdBudynkow(Transform estate, Transform physics)
    {
        int mapa = ZbudujMapeBudynkow(estate);
        int przes = 0, zmn = 0, usun = 0;
        Bounds Korona(TreeRec tr) => tr.leaves ? tr.leaves.bounds : new Bounds(tr.go.transform.position + Vector3.up * tr.height * .66f, new Vector3(tr.height * 1.3f, tr.height * .68f, tr.height * 1.1f));
        foreach (var tr in trees.ToList())
        {
            if (tr == null || !tr.go) continue;
            if (KoronaWBudynku(Korona(tr)) < 4) continue;
            var start = tr.go.transform.position; var s0 = tr.go.transform.localScale;
            bool ok = false;
            // a) przesunięcie na wolny trawnik (do 4 m): nie na chodnik, nie w zabudowę, nie w inne drzewo
            for (float r = 1f; r <= 4.01f && !ok; r += 1f)
                for (int a = 0; a < 12 && !ok; a++)
                {
                    var d = Quaternion.Euler(0, a * 30f, 0) * Vector3.forward * r;
                    var p = start + d;
                    if (OnPaving(p.x, p.z, .9f) || IsHard(p.x, p.z, .6f)) continue;
                    bool tlok = false;
                    foreach (var q in treeSpots)
                        if ((Mathf.Abs(q.x - tr.at.x) > .01f || Mathf.Abs(q.z - tr.at.z) > .01f) && (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z) < 9f) { tlok = true; break; }
                    if (tlok) continue;
                    tr.go.transform.position = p;
                    if (KoronaWBudynku(Korona(tr)) < 4) ok = true; else tr.go.transform.position = start;
                }
            // drzewo bez własnej kolizji pnia (po „kulkach” Codexa) ma okrąg w fizyce oryginału — wyłączany przy przesunięciu/usunięciu
            void StaryPien()
            {
                if (!physics || tr.trunk) return;
                foreach (var c in physics.GetComponentsInChildren<Collider>(true))
                {
                    var b = c.bounds;
                    if (c.enabled && b.size.x < .9f && b.size.z < .9f && b.size.y < 4f && Mathf.Abs(b.center.x - start.x) < .45f && Mathf.Abs(b.center.z - start.z) < .45f) c.enabled = false;
                }
            }
            if (ok)
            {
                var d = tr.go.transform.position - start;
                if (!tr.trunk && physics)
                {
                    StaryPien();
                    tr.trunk = new GameObject("pień kolizja"); tr.trunk.transform.SetParent(colliders, false); tr.trunk.transform.position = start + Vector3.up * 1.4f;
                    var cap = tr.trunk.AddComponent<CapsuleCollider>(); cap.radius = .3f; cap.height = 2.8f;
                }
                if (tr.trunk) tr.trunk.transform.position += d;
                treeSpots.RemoveAll(q => Mathf.Abs(q.x - tr.at.x) < .01f && Mathf.Abs(q.z - tr.at.z) < .01f);
                tr.at += d; treeSpots.Add(new Vector3(tr.at.x, tr.height, tr.at.z)); przes++; continue;
            }
            // b) mniejsze drzewo (nie niżej niż 4.5 m)
            foreach (float f in new[] { .85f, .7f })
            {
                if (tr.height * f < 4.5f) break;
                tr.go.transform.localScale = s0 * f;
                if (KoronaWBudynku(Korona(tr)) < 4) { ok = true; tr.height *= f; break; }
            }
            if (ok) { zmn++; continue; }
            // c) usunięte
            StaryPien();
            UnityEngine.Object.DestroyImmediate(tr.go); if (tr.trunk) UnityEngine.Object.DestroyImmediate(tr.trunk);
            treeSpots.RemoveAll(q => Mathf.Abs(q.x - tr.at.x) < .01f && Mathf.Abs(q.z - tr.at.z) < .01f);
            trees.Remove(tr); usun++;
        }
        return (przes, zmn, usun, mapa);
    }
}

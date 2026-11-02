using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// KASYNO — AUTOMATY DO GIER (build 45 runda 2, E34). Model kasyno_automat_nermin (paczka osiedle_wnetrza): dwa automaty
// w jednym pliku — węzły bez przyrostka = automat 1, z przyrostkiem „_2” = automat 2 (strip1/2/3 bębny, handel dźwignia,
// button1..8, toplight, topsign, frontdisplay, lowerpayglass, moneybox). Budowa jednego automatu: kopia modelu bez węzłów
// drugiego, skala do 1.85 m × K, osie bębnów (pivot w środku obrysu, oś = wymiar różny od dwóch pozostałych), pivot dźwigni
// u jej podstawy, warianty grafiki BRAXHORN (_tekstury/kasyno_automat_warianty/<wariant>_{pas,tablica,ekran}.png:
// pas → lowerpayglass, tablica → topsign, ekran → frontdisplay z oknami bębnów w alfie), komponent BraxAutomat (ZAGRAJ).
// W 45: TYLKO SPIS starych automatów sali gier (gameready_casino_scene, model z CI — lokalnie niedostępny), nowe automaty
// za flagą AutomatyNoweWlaczone = false (miejsca do decyzji po spisie: log BRAXHORN_KASYNO automaty_stare=…).
public static partial class BraxClaudeOsiedle
{
    public static bool AutomatyNoweWlaczone = false;
    public static readonly List<(Vector3 p, float yaw, int maszyna, string wariant, bool npc)> AutomatyMiejsca = new List<(Vector3, float, int, string, bool)>();
    static readonly System.Text.RegularExpressions.Regex AutomatNazwa = new System.Text.RegularExpressions.Regex("slot|machine|automat|bandit|jackpot", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    static void KasynoAutomaty()
    {
        var hala = MinMax(68.5f, 103.5f, -.5f, 4.5f, -51.3f, -34.7f);
        var stare = new List<string>(); var widziane = new HashSet<Transform>();
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!r || !hala.Contains(r.bounds.center)) continue;
            var mf = r.GetComponent<MeshFilter>(); string mesh = mf && mf.sharedMesh ? mf.sharedMesh.name : "";
            Transform hit = null;
            for (var t = r.transform; t && !hit; t = t.parent) if (AutomatNazwa.IsMatch(t.name)) hit = t;
            if (!hit && !AutomatNazwa.IsMatch(mesh)) continue;
            var key = hit ? hit : r.transform; if (!widziane.Add(key)) continue;
            stare.Add(key.name + (mesh.Length > 0 ? "/" + mesh : "") + " " + BraxKontrola.F(r.bounds.center) + " " + r.bounds.size.x.ToString("F1") + "×" + r.bounds.size.y.ToString("F1") + "×" + r.bounds.size.z.ToString("F1"));
        }
        int nowe = 0, dzialajace = 0;
        if (AutomatyNoweWlaczone && AutomatyMiejsca.Count > 0)
            foreach (var (p, yaw, maszyna, wariant, npc) in AutomatyMiejsca)
            {
                var a = AutomatNowy(p, yaw, maszyna, wariant, npc);
                if (a) { nowe++; if (a.bebny != null && a.bebny.Length == 3 && a.bebny.All(b => b)) dzialajace++; }
            }
        Debug.Log("BRAXHORN_KASYNO automaty_stare=" + stare.Count + " wyciete=0 nowe=" + nowe + " dzialajace=" + dzialajace
                  + (AutomatyNoweWlaczone ? "" : " (nowe za flagą AutomatyNoweWlaczone=false — sala bez zmian w 45)") + (stare.Count > 0 ? " spis: " + string.Join("; ", stare.Take(40)) : " (brak nazw slot/machine/automat — automaty wtopione w siatkę sali?)"));
    }

    static bool Drugi(string n) => n.EndsWith("_2", StringComparison.Ordinal);

    static BraxAutomat AutomatNowy(Vector3 p, float yaw, int maszyna, string wariant, bool npc)
    {
        try
        {
            var src = BraxOsiedleAssety.ModelWnetrz("kasyno_automat_nermin");
            if (!src) return null;
            var o = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false);
            o.name = "kasyno: automat " + maszyna + " (" + wariant + ")"; o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.identity;
            // usunięcie węzłów drugiego automatu
            var wszystkie = o.GetComponentsInChildren<Transform>(true).Where(t => t != o.transform).ToList();
            var nazwy = new HashSet<string>(wszystkie.Select(t => t.name));
            foreach (var t in wszystkie)
            {
                if (!t) continue;
                bool usun = maszyna == 1 ? Drugi(t.name) : !Drugi(t.name) && nazwy.Contains(t.name + "_2");
                if (usun) UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            string suf = maszyna == 1 ? "" : "_2";
            Transform Cz(string n) => BraxOsiedleAssety.Znajdz(o.transform, n + suf);
            // skala i ustawienie
            var b = BraxOsiedleAssety.Granice(o); o.transform.localScale = src.transform.localScale * (1.85f * K / Mathf.Max(1e-4f, b.size.y));
            var ekran = Cz("frontdisplay");
            b = BraxOsiedleAssety.Granice(o);
            var przodW = ekran ? BraxOsiedleAssety.Granice(ekran).center - b.center : Vector3.forward; przodW.y = 0f;
            przodW = Mathf.Abs(przodW.x) > Mathf.Abs(przodW.z) ? new Vector3(Mathf.Sign(przodW.x), 0, 0) : new Vector3(0, 0, przodW.z >= 0f ? 1f : -1f);
            o.transform.rotation = Quaternion.Euler(0f, yaw + Vector3.SignedAngle(przodW, Vector3.forward, Vector3.up), 0f);
            b = BraxOsiedleAssety.Granice(o); o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
            BraxOsiedleAssety.Przygotuj(o, true);
            var prawoW = o.transform.rotation * Vector3.right;
            // pivoty bębnów (oś = boczna automatu) i dźwigni (u podstawy)
            Transform Pivot(Transform cz, bool podstawa)
            {
                if (!cz) return null;
                var bb = BraxOsiedleAssety.Granice(cz);
                var piv = new GameObject(cz.name + " — oś").transform; piv.SetParent(cz.parent, true);
                piv.position = podstawa ? new Vector3(bb.center.x, bb.min.y, bb.center.z) : bb.center;
                piv.rotation = Quaternion.FromToRotation(Vector3.right, prawoW);
                cz.SetParent(piv, true); return piv;
            }
            var a = o.AddComponent<BraxAutomat>();
            a.bebny = new[] { Pivot(Cz("strip1"), false), Pivot(Cz("strip2"), false), Pivot(Cz("strip3"), false) };
            a.dzwignia = Pivot(Cz("handel"), true);
            a.przycisk = Cz("button1"); a.kieszen = Cz("moneybox");
            var sw = Cz("toplight"); a.swiatlo = sw ? sw.GetComponentInChildren<Renderer>() : null;
            if (a.swiatlo) foreach (var m in a.swiatlo.sharedMaterials) if (m) { m.EnableKeyword("_EMISSION"); }
            a.przod = Quaternion.Inverse(o.transform.rotation) * (Quaternion.Euler(0f, yaw, 0f) * Vector3.forward);
            a.npc = npc;
            a.zeton = Toczona("żeton kasyna", new[] { new Vector2(.02f, 0f), new Vector2(.02f, .004f) }, 12, true);
            a.zetonMat = Plain("żeton kasyna czerwony", new Color(.7f, .06f, .05f), .5f);
            // warianty grafiki
            foreach (var (czesc, plik) in new[] { ("lowerpayglass", "pas"), ("topsign", "tablica"), ("frontdisplay", "ekran") })
            {
                var t = Cz(czesc); var tex = BraxOsiedleAssety.Tekstura(BraxOsiedleAssety.Wnetrza, "_tekstury/kasyno_automat_warianty/" + wariant + "_" + plik + ".png");
                if (!t || !tex) { BraxOsiedleAssety.Log("automat: brak " + czesc + " / " + wariant + "_" + plik); continue; }
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    var m = new Material(r.sharedMaterial ? r.sharedMaterial : new Material(Shader.Find("Standard"))) { name = "automat " + wariant + " " + plik, mainTexture = tex };
                    if (plik == "ekran") { m.SetFloat("_Mode", 1f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", .5f); m.renderQueue = 2450; }
                    AssetDatabase.CreateAsset(m, Out + "/" + Safe("automat " + maszyna + " " + wariant + " " + plik + " " + r.GetInstanceID()) + ".mat");
                    r.sharedMaterial = m;
                }
            }
            b = BraxOsiedleAssety.Granice(o); Collider(b.center, new Vector3(b.size.x * .9f, b.size.y, b.size.z * .9f));
            return a;
        }
        catch (Exception e) { Debug.LogError("BRAXHORN_KASYNO automat DO POPRAWKI: " + e.Message); return null; }
    }
}

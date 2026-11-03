using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// KONTROLA MATERIAŁÓW (dyspozytor 2.11, zgłoszenia właściciela z miasta 13.1: „posadzka-lustro” pod wiaduktem, na placu i na
// końcu mapy — gracz chodził po odbiciu nieba). Duże poziome powierzchnie, po których się chodzi (podłogi, place, chodniki),
// nie mogą być lustrem: materiał z gładkością ≥ 0.85 albo metalicznością ≥ 0.8 na płaskiej powierzchni ≥ 4 m² (poza szkłem,
// wodą, basenem, lodem, lustrem) → gładkość 0.3, metaliczność 0 (zmiana materiału = wszystkie jego użycia: podłoga nie ma
// być lustrem nigdzie). Wynik: BRAXHORN_MATERIALY lustra=N poprawione=N (lista do city-review/kontrola-<obszar>.json).
public static class BraxKontrolaMaterialow
{
    static readonly Regex Dozwolone = new Regex(@"\b(szkl\w*|szyb\w*|glass\w*|woda|wody|wodz\w*|water\w*|morz\w*|sea|ocean\w*|basen\w*|pool\w*|lod|lodu|ice|lustr\w*|mirror\w*|kaluz\w*|puddle\w*|chrom\w*|neon\w*|ekran\w*|screen\w*)\b");

    // Gładkość efektywna materiału (górna granica): Standard — _Glossiness albo _GlossMapScale, gdy gładkość idzie z mapy;
    // CityStaticPBR (glTF) — 1 − _Roughness (szorstkość = ARM.g × _Roughness, więc z mapą bywa jeszcze gładziej).
    static float Gladkosc(Material m)
    {
        if (m.HasProperty("_Roughness")) return 1f - m.GetFloat("_Roughness");
        bool zMapy = (m.HasProperty("_MetallicGlossMap") && m.GetTexture("_MetallicGlossMap")) || (m.HasProperty("_SmoothnessTextureChannel") && m.GetFloat("_SmoothnessTextureChannel") > .5f);
        if (zMapy && m.HasProperty("_GlossMapScale")) return m.GetFloat("_GlossMapScale");
        return m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0f;
    }

    public static void Wykonaj(Scene scena)
    {
        // 3.11 (właściciel 11:09, zdjęcie: biała „lustrzana” płyta na placu): próg z 0.85 obniżony do 0.55 (posadzka ma być
        // matowa — szorstkość ≥ 0.5, jak na osiedlu), liczona też gładkość z mapy i shader CityStaticPBR (_Roughness — dotąd
        // pomijany), płaskość względna (pochyły plac / droga też), a materiał podłogi NIE jest zmieniany wspólnie z innymi
        // użyciami (szklane wieżowce z tym samym metalem zostają lśniące) — podłoga dostaje matową kopię (zapis w PlayableGenerated).
        int lustra = 0, poprawione = 0; var lista = new List<string>(); var kopie = new Dictionary<Material, Material>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!BraxKontrola.Widoczny(r)) continue;
                var b = r.bounds;
                if (b.size.x * b.size.z < 4f || b.size.y > Mathf.Max(.3f, .03f * Mathf.Max(b.size.x, b.size.z))) continue;   // tylko duże płaskie (podłogi, place, drogi)
                if (BraxKontrola.WChronionym(r.transform)) continue;
                string nr = BraxKontrola.Norm(r.name);
                var mats = r.sharedMaterials; bool zmiana = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!m) continue;
                    if (kopie.TryGetValue(m, out var gotowa)) { if (gotowa) { mats[i] = gotowa; zmiana = true; } continue; }
                    string n = nr + BraxKontrola.Norm(m.name) + BraxKontrola.Norm(m.shader ? m.shader.name : "");
                    if (Dozwolone.IsMatch(n)) { kopie[m] = null; continue; }
                    float g = Gladkosc(m);
                    float met = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                    if (g <= .55f && met <= .5f) { kopie[m] = null; continue; }
                    lustra++;
                    string opis = "„" + m.name + "” (gładkość " + g.ToString("F2") + ", metal " + met.ToString("F2") + ") na " + BraxKontrola.Sciezka(r.transform) + " " + BraxKontrola.F(b.center) + " " + b.size.x.ToString("F0") + "×" + b.size.z.ToString("F0") + " m";
                    try
                    {
                        var c = new Material(m) { name = m.name + " (posadzka matowa)" };
                        if (c.HasProperty("_Glossiness")) c.SetFloat("_Glossiness", Mathf.Min(c.GetFloat("_Glossiness"), .3f));
                        if (c.HasProperty("_Smoothness")) c.SetFloat("_Smoothness", Mathf.Min(c.GetFloat("_Smoothness"), .3f));
                        if (c.HasProperty("_GlossMapScale")) c.SetFloat("_GlossMapScale", Mathf.Min(c.GetFloat("_GlossMapScale"), .35f));
                        if (c.HasProperty("_Metallic")) c.SetFloat("_Metallic", 0f);
                        if (c.HasProperty("_Roughness")) c.SetFloat("_Roughness", Mathf.Max(c.GetFloat("_Roughness"), .8f));
                        // ARM bez AO: szorstkość i metal tylko z czynników (mapa potrafi mieć szorstkość 0.1 = lustro)
                        if (c.HasProperty("_ARM") && c.GetTexture("_ARM") && (!c.HasProperty("_UseARMForAO") || c.GetFloat("_UseARMForAO") < .5f)) c.SetTexture("_ARM", null);
                        if (c.HasProperty("_ReflectionStrength")) c.SetFloat("_ReflectionStrength", Mathf.Min(c.GetFloat("_ReflectionStrength"), .25f));
                        if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets", "PlayableGenerated");
                        if (!AssetDatabase.IsValidFolder("Assets/PlayableGenerated/PosadzkiMatowe")) AssetDatabase.CreateFolder("Assets/PlayableGenerated", "PosadzkiMatowe");
                        BraxZapisAssetu.Utworz(c, "Assets/PlayableGenerated/PosadzkiMatowe/posadzka_" + kopie.Count + ".mat");
                        kopie[m] = c; mats[i] = c; zmiana = true;
                        poprawione++; opis = "POPRAWIONE " + opis;
                    }
                    catch (Exception e) { kopie[m] = null; opis = "ZGŁOSZONE " + opis + " — " + e.Message; }
                    if (lista.Count < 60) lista.Add(opis);
                    BraxKontrola.Zapisz("materiały: lustro na podłodze " + opis);
                }
                if (zmiana) r.sharedMaterials = mats;
            }
        var sb = new StringBuilder("BRAXHORN_MATERIALY " + BraxKontrola.Obszar + ": lustra=" + lustra + " poprawione=" + poprawione + " (płaskie ≥ 4 m², gładkość > 0.55 lub metal > 0.5 — także CityStaticPBR; podłoga dostaje matową kopię, bez szkła/wody/basenu)");
        foreach (var l in lista.Take(25)) sb.Append("\n  ").Append(l);
        Debug.Log(sb.ToString());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

    public static void Wykonaj(Scene scena)
    {
        int lustra = 0, poprawione = 0; var lista = new List<string>(); var zrobione = new HashSet<Material>();
        foreach (var root in scena.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!BraxKontrola.Widoczny(r)) continue;
                var b = r.bounds;
                if (b.size.y > .3f || b.size.x * b.size.z < 4f) continue;            // tylko duże płaskie (podłogi, place)
                if (BraxKontrola.WChronionym(r.transform)) continue;
                string nr = BraxKontrola.Norm(r.name);
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || zrobione.Contains(m)) continue;
                    string n = nr + BraxKontrola.Norm(m.name) + BraxKontrola.Norm(m.shader ? m.shader.name : "");
                    if (Dozwolone.IsMatch(n)) continue;
                    float g = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0f;
                    float met = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                    if (g < .85f && met < .8f) continue;
                    lustra++; zrobione.Add(m);
                    string opis = "„" + m.name + "” (gładkość " + g.ToString("F2") + ", metal " + met.ToString("F2") + ") na " + BraxKontrola.Sciezka(r.transform) + " " + BraxKontrola.F(b.center) + " " + b.size.x.ToString("F0") + "×" + b.size.z.ToString("F0") + " m";
                    try
                    {
                        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .3f);
                        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .3f);
                        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                        if (m.HasProperty("_GlossMapScale")) m.SetFloat("_GlossMapScale", Mathf.Min(m.GetFloat("_GlossMapScale"), .35f));
                        poprawione++; opis = "POPRAWIONE " + opis;
                    }
                    catch (Exception e) { opis = "ZGŁOSZONE " + opis + " — " + e.Message; }
                    if (lista.Count < 60) lista.Add(opis);
                    BraxKontrola.Zapisz("materiały: lustro na podłodze " + opis);
                }
            }
        var sb = new StringBuilder("BRAXHORN_MATERIALY " + BraxKontrola.Obszar + ": lustra=" + lustra + " poprawione=" + poprawione + " (płaskie ≥ 4 m², gładkość ≥ 0.85 lub metal ≥ 0.8, bez szkła/wody/basenu)");
        foreach (var l in lista.Take(25)) sb.Append("\n  ").Append(l);
        Debug.Log(sb.ToString());
    }
}

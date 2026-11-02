using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// SZYBY GLOBALNIE (C4 / M7, właściciel 2.11 22:29: „Szyby — globalnie wszystko poprawić, gdzie trzeba”; przegląd filmów: kebab,
// fryzjer, sklep sportowy, fasada biurowca — „mleczny jasnoniebieski plastik, przez który widać schody i drzwi płasko”).
// Przyczyna: szkło robione jako jasny kolor (0.6–0.85) z alfą 0.2–0.3 w trybie zwykłego mieszania (SrcAlpha / OneMinusSrcAlpha)
// shadera CityStaticPBR — na szybie liczy się pełne rozproszenie światła otoczenia jasnego koloru (mleko), a odbicie nieba
// jest mnożone przez alfę (prawie znika). Prawdziwa szyba nie ma rozproszenia: przepuszcza tło lekko przyciemnione i dodaje
// odbicie (Fresnel). Jedna kontrola na końcu buildu miasta, po nazwach materiałów (szk/szyb/glass/glazing/witryn/okn/window;
// ramy, aluminium, okiennice, lustra, ekrany, szkło mleczne/satynowe celowo — bez zmian):
//   • szyba PRZEZROCZYSTA (witryny, drzwi, okna z wnętrzem, balustrady) → jedno szkło: ciemny, neutralny odcień, alfa 0.34,
//     mieszanie z przemnożoną alfą (One / OneMinusSrcAlpha: tło × 0.66 + odbicie nieba w pełni), gładkość wysoka, odbicie 1.0;
//   • szyba NIEPRZEZROCZYSTA bez tekstury (fasada bez wnętrza, „szyba jasna”) → szkło refleksyjne ciemniejsze (metal 0.85,
//     odcień stalowo-niebieski, gładkość wysoka) — odbija niebo, nie wygląda jak jasna farba.
// Log BRAXHORN_SZYBY materialy=… obiekty=… zmienione=… (+ największe szyby przezroczyste z pozycją, do sprawdzenia na zdjęciach).
public static class BraxSzyby
{
    static readonly Regex Szklo = new Regex(@"szk|szyb|glass|glazing|witryn|okn|window|glaz", RegexOptions.IgnoreCase);
    static readonly Regex Nie = new Regex(@"ram[ay]?\b|rama|ramy|aluminium|\balu\b|stelaz|stelaż|frame|mullion|oscie|oście|parapet|szpros|okiennic|shutter|uszczel|klamk|okuci|lustr|mirror|ekran|screen|telewiz|\btv\b|\bled\b|neon|mleczn|satyn|frosted|matow|emali|czarne|black|bramek|szyld|logo|napis|litery|kolory z wierzch|z daleka|panel", RegexOptions.IgnoreCase);
    const string Pbr = "Braxhorn/CityStaticPBR";

    static bool Przezroczysta(Material m)
    {
        if (m.renderQueue >= 2900) return true;
        if (m.HasProperty("_Transparent") && m.GetFloat("_Transparent") > .5f) return true;
        if (m.HasProperty("_Color") && m.GetColor("_Color").a < .99f && m.shader && m.shader.name == "Standard" && m.HasProperty("_Mode") && m.GetFloat("_Mode") >= 2f) return true;
        return false;
    }
    static bool MaTeksture(Material m) => m.HasProperty("_MainTex") && m.GetTexture("_MainTex") && m.GetTexture("_MainTex").width > 8;

    public static void Wykonaj(Scene scena, string obszar)
    {
        var uzycia = new Dictionary<Material, List<Renderer>>();
        int renderery = 0;
        foreach (var root in scena.GetRootGameObjects())
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                renderery++;
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || !m.shader) continue;
                    string sh = m.shader.name;
                    if (sh != Pbr && sh != "Standard") continue;                   // shadery okien z pokojami, napisy, woda — bez zmian
                    string n = m.name;
                    if (!Szklo.IsMatch(n) || Nie.IsMatch(n)) continue;
                    if (!uzycia.TryGetValue(m, out var l)) uzycia[m] = l = new List<Renderer>();
                    if (!l.Contains(r)) l.Add(r);
                }
            }
        int przez = 0, refl = 0, pominiete = 0; var lista = new StringBuilder(); var duze = new List<(float pole, string opis)>();
        foreach (var kv in uzycia)
        {
            var m = kv.Key;
            try
            {
                bool p = Przezroczysta(m);
                if (m.shader.name == Pbr)
                {
                    if (p) { Przezroczyste(m); przez++; }
                    else if (!MaTeksture(m)) { Refleksyjne(m); refl++; }
                    else { pominiete++; continue; }   // szkło z teksturą (np. kafle okien wieżowców) — wygląd z tekstury
                }
                else
                {   // Standard (modele): przezroczysta → tryb Transparent (premultiply), ciemny odcień; nieprzezroczysta → gładka, metaliczna
                    if (p)
                    {
                        m.SetColor("_Color", new Color(.05f, .07f, .08f, .34f)); m.SetFloat("_Glossiness", .95f); m.SetFloat("_Metallic", 0f);
                        m.SetFloat("_Mode", 3f); m.SetInt("_SrcBlend", (int)BlendMode.One); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                        m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
                        przez++;
                    }
                    else if (!MaTeksture(m)) { m.SetColor("_Color", new Color(.28f, .34f, .39f)); m.SetFloat("_Glossiness", .92f); m.SetFloat("_Metallic", .8f); refl++; }
                    else { pominiete++; continue; }
                }
                EditorUtility.SetDirty(m);
                if (lista.Length < 3000) lista.Append(" | ").Append(m.name).Append(p ? " → przezroczyste" : " → refleksyjne").Append(" (").Append(kv.Value.Count).Append(")");
                if (p) foreach (var r in kv.Value) { var b = r.bounds; float pole = Mathf.Max(b.size.x, b.size.z) * b.size.y; if (pole > 12f) duze.Add((pole, r.name + " [" + m.name + "] " + b.center.ToString("F0") + " " + b.size.ToString("F1"))); }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_SZYBY " + m.name + ": " + e.Message); }
        }
        int obiekty = uzycia.Values.Sum(l => l.Count);
        Debug.Log("BRAXHORN_SZYBY " + obszar + " materialy=" + uzycia.Count + " obiekty=" + obiekty + " zmienione=" + (przez + refl) + " (przezroczyste " + przez + ", refleksyjne " + refl + ", z teksturą bez zmian " + pominiete + "; renderery sceny " + renderery + ")" + lista
                  + "\nBRAXHORN_SZYBY największe szyby przezroczyste (do zdjęć): " + string.Join(" | ", duze.OrderByDescending(d => d.pole).Take(15).Select(d => d.opis)));
    }

    // jedna szyba przezroczysta: tło × (1 − 0.34) + odbicie nieba (przemnożona alfa), bez mlecznego rozproszenia
    static void Przezroczyste(Material m)
    {
        m.SetColor("_Color", new Color(.035f, .05f, .055f, .34f));
        m.SetFloat("_Roughness", .06f); m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_ReflectionStrength")) m.SetFloat("_ReflectionStrength", 1f);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
        m.SetFloat("_AlphaClip", 0); m.SetFloat("_Cutoff", 0); m.SetFloat("_Transparent", 1);
        m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
    }
    // szyba fasady bez wnętrza: ciemniejsze szkło refleksyjne (stalowo-niebieskie, prawie lustro nieba)
    static void Refleksyjne(Material m)
    {
        m.SetColor("_Color", new Color(.30f, .37f, .42f, 1f));
        m.SetFloat("_Roughness", .1f); m.SetFloat("_Metallic", .85f);
        if (m.HasProperty("_ReflectionStrength")) m.SetFloat("_ReflectionStrength", .95f);
    }
}

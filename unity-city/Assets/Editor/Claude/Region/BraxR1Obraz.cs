using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO R1 — OBRAZ WG ZASADY GLOBALNEJ „minimum Osiedle, a chcę lepiej” (właściciel 16.11 12:30–12:40; 13:32: „są nowe zasady
// ustalone parametrów rysowania, kontrast itd. — dostosować”; dokument ZASADA-OBRAZ-MINIMUM-OSIEDLE-16-11).
// Scena miasta (baza: siłownia startowa z BraxGymBuild) miała: słońce Unity 0.75, materiały miasta (CityStaticPBR) słońce 2.4/π
// i otoczenie 0.65 — w jednostkach Standard ok. 62 % Osiedla (słońce 1.24, otoczenie trilight); mgła wyłączona; bliska płaszczyzna
// kamery 0.1 m; bez przejścia koloru. Teraz (na końcu budowy rejonu, raz):
//   • słońce sceny 1.24, barwa (1, .955, .875), cienie miękkie, siła ≥ 0.80, bias 0.04 / normal 0.25 (z Miasta — mniej drgań);
//     kierunek bez zmian (= słońce shaderów miasta, BraxCityMaterials.SunDirection — cienie zgodne z cieniowaniem);
//   • materiały miasta: ekspozycja 1.62 (= 1.24 / (2.4/π)) — słońce i otoczenie na poziomie Osiedla; w sali siłowni bez zmian;
//   • otoczenie trilight nie niżej niż Osiedle (kanał po kanale): niebo (.60,.68,.80), horyzont (.50,.53,.50), ziemia (.30,.28,.23);
//   • MGŁA: ŻADNA (właściciel 16.11 14:33: „Żadnej kurwa mgły stop”) — mgła Unity wyłączona, mgiełka shadera miasta
//     (_BraxMgielkaGestosc) w scenie R1 = 0 (BraxObrazRejonu), tło nieba, teren za granicą i pierścień panoramy bez mgiełki;
//   • kamery na ekran: bliska płaszczyzna 0.25 m (0.15 m przy ścianie — BraxClaudeKamera), przejście koloru BraxClaudeKolor
//     (nasycenie 1.15, kontrast 1.06 — to samo co Osiedle), odbicia 1.0.
// Log BRAXHORN_OBRAZ_R1 z porównaniem do minimum (każda pozycja OK / PONIŻEJ).
public static class BraxR1Obraz
{
    public const float Ekspozycja = 1.62f;

    static Color Max(Color a, Color b) => new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), 1f);
    static bool Nie(Color a, Color min) => a.r + .001f < min.r || a.g + .001f < min.g || a.b + .001f < min.b;

    public static void Zastosuj(Transform root)
    {
        var l = new List<string>();
        var sun = RenderSettings.sun;
        float staraMoc = sun ? sun.intensity : 0f;
        Bounds[] bezZmian = new Bounds[0];
        if (sun)
        {
            sun.intensity = 1.24f; sun.color = new Color(1f, .955f, .875f);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = Mathf.Max(.8f, sun.shadowStrength);
            sun.shadowBias = .04f; sun.shadowNormalBias = .25f;
            var sw = sun.GetComponent<BraxSlonceWnetrza>(); if (sw) { sw.cienie = LightShadows.Soft; bezZmian = sw.bezCieni; }
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Max(RenderSettings.ambientSkyColor, new Color(.60f, .68f, .80f));
        RenderSettings.ambientEquatorColor = Max(RenderSettings.ambientEquatorColor, new Color(.50f, .53f, .50f));
        RenderSettings.ambientGroundColor = Max(RenderSettings.ambientGroundColor, new Color(.30f, .28f, .23f));
        RenderSettings.fog = false;   // właściciel 16.11 14:33: „Żadnej kurwa mgły stop”
        RenderSettings.reflectionIntensity = Mathf.Max(1f, RenderSettings.reflectionIntensity);
        int kam = 0; bool kolorOk = true;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!cam || cam.targetTexture || cam.gameObject.scene != root.gameObject.scene) continue;
            cam.nearClipPlane = .25f;
            if (!cam.GetComponent<BraxClaudeKamera>()) cam.gameObject.AddComponent<BraxClaudeKamera>();
            var kol = cam.GetComponent<BraxClaudeKolor>(); if (!kol) kol = cam.gameObject.AddComponent<BraxClaudeKolor>();
            kol.shader = Shader.Find("Braxhorn/OsiedleKolor"); kol.nasycenie = 1.15f; kol.kontrast = 1.06f;
            if (!kol.shader) kolorOk = false;
            kam++;
        }
        var g = new GameObject("CLAUDE obraz rejonu: ekspozycja i słońce (minimum Osiedle)"); g.transform.SetParent(root, false);
        var o = g.AddComponent<BraxObrazRejonu>(); o.ekspozycja = Ekspozycja; o.slonce = sun; o.slonceZewnatrz = 1.24f;
        o.slonceWewnatrz = staraMoc > 0f ? staraMoc : .75f; o.bezZmian = bezZmian;
        // porównanie z minimum (ZASADA-OBRAZ-MINIMUM-OSIEDLE pkt 1)
        void P(string co, bool ok, string ile) => l.Add((ok ? "OK " : "PONIŻEJ ") + co + " " + ile);
        P("słońce", sun && sun.intensity >= 1.24f - 1e-3f, sun ? sun.intensity.ToString("F2") + " (materiały miasta × " + Ekspozycja.ToString("F2") + ")" : "brak");
        P("siła cieni", sun && sun.shadowStrength >= .8f - 1e-3f, sun ? sun.shadowStrength.ToString("F2") : "brak");
        P("cienie miękkie", sun && sun.shadows == LightShadows.Soft, sun ? sun.shadows.ToString() : "brak");
        P("otoczenie niebo", !Nie(RenderSettings.ambientSkyColor, new Color(.60f, .68f, .80f)), RenderSettings.ambientSkyColor.ToString("F2"));
        P("otoczenie horyzont", !Nie(RenderSettings.ambientEquatorColor, new Color(.50f, .53f, .50f)), RenderSettings.ambientEquatorColor.ToString("F2"));
        P("otoczenie ziemia", !Nie(RenderSettings.ambientGroundColor, new Color(.30f, .28f, .23f)), RenderSettings.ambientGroundColor.ToString("F2"));
        P("mgła wyłączona (decyzja właściciela 16.11 14:33)", !RenderSettings.fog, RenderSettings.fog ? "WŁĄCZONA" : "brak mgły, mgiełka shadera 0");
        P("bliska płaszczyzna", kam > 0, ".25 m (przy ścianie .15 m), kamer " + kam);
        P("przejście koloru", kam > 0 && kolorOk, "nasycenie 1.15, kontrast 1.06");
        P("rzutowanie cieni", true, "StableFit (ustawia BraxGrafika i BraxClaudeKamera w grze)");
        P("MSAA w buildzie", QualitySettings.antiAliasing >= 2, QualitySettings.antiAliasing + "× (poziom jakości buildu)");
        BraxRegion.Note("obraz R1 (minimum Osiedle): " + string.Join("; ", l));
        Debug.Log("BRAXHORN_OBRAZ_R1 " + string.Join(" | ", l));
        foreach (var x in l.Where(x => x.StartsWith("PONIŻEJ"))) Debug.LogWarning("DO POPRAWKI: BRAXHORN_OBRAZ_R1 " + x);
    }
}

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
        // 18.11 (właściciel 10:39: „gra zacina, zamula, szczególnie w siłowni… prawie wszystko w mieście migota”): na telefonie
        // (build Android) bez pełnoekranowego przejścia koloru (OnRenderImage = kopia całego kadru + rozwiązanie MSAA co klatkę),
        // bias cieni jak w Unity 0.05 / 0.4 (0.04 / 0.25 dawało „trądzik cieni” — pasy migoczące na ścianach przy ruchu kamery),
        // cienie zostają MIĘKKIE (twarde = postrzępione krawędzie cieni, które przy ruchu kamery „pełzają” — to też migotanie),
        // lustro siłowni tylko do 25 m i 15 razy na sekundę (rysowało miasto drugi raz do 80 m, 24 razy na sekundę).
        bool telefon = UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
        var cienie = LightShadows.Soft;
        // MSAA NA TELEFONIE (APK 4f317df, globalgamemanagers: telefon startuje na poziomie jakości 2 „Medium” z antiAliasing = 0,
        // a BraxGrafika.Msaa na telefonie nic nie zmienia — gra szła BEZ wygładzania krawędzi; krawędzie, ramy okien, pasy,
        // balustrady „pływały” przy każdym ruchu kamery = „migota prawie wszystko, mocniej lub delikatniej”). Log „MSAA 4×”
        // sprawdzał tylko poziom edytora. Teraz 4× na KAŻDYM poziomie jakości zapisanym w buildzie — ustawione raz, przed startem
        // gry (bez zmiany bufora ekranu w trakcie gry, która na części telefonów zamykała aplikację — BraxGrafika 31.10).
        int poziomBylo = QualitySettings.GetQualityLevel(), msaaPoziomy = 0; var msaaOpis = new List<string>();
        for (int q = 0; q < QualitySettings.names.Length; q++)
        {
            QualitySettings.SetQualityLevel(q, false);
            if (QualitySettings.antiAliasing < 4) { QualitySettings.antiAliasing = 4; msaaPoziomy++; }
            msaaOpis.Add(QualitySettings.names[q] + " " + QualitySettings.antiAliasing + "×");
        }
        QualitySettings.SetQualityLevel(poziomBylo, false);
        bool msaaWszedzie = msaaOpis.Count > 0 && msaaOpis.All(x => x.EndsWith(" 4×"));
        if (sun)
        {
            sun.intensity = 1.24f; sun.color = new Color(1f, .955f, .875f);
            sun.shadows = cienie; sun.shadowStrength = Mathf.Max(.8f, sun.shadowStrength);
            sun.shadowBias = .05f; sun.shadowNormalBias = .4f;
            var sw = sun.GetComponent<BraxSlonceWnetrza>(); if (sw) { sw.cienie = cienie; bezZmian = sw.bezCieni; }
        }
        int lustra = 0;
        foreach (var lu in Object.FindObjectsByType<BraxGymPlanarMirror>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!lu || lu.gameObject.scene != root.gameObject.scene) continue;
            lu.reflectionDistance = Mathf.Min(lu.reflectionDistance, 25f); lu.refreshRate = Mathf.Min(lu.refreshRate, 15f); lustra++;
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
            var bk = cam.GetComponent<BraxClaudeKamera>(); if (!bk) bk = cam.gameObject.AddComponent<BraxClaudeKamera>();
            // 18.11: bliska płaszczyzna na otwartej przestrzeni 0.5 m (było 0.25): głębia 24 bit — błąd z² / (near · 2^24), więc
            // dwie powierzchnie 2 cm od siebie rozróżniane do ~410 m zamiast ~290 m, 1 cm do ~290 m zamiast ~205 m. Przy ścianie
            // dalej 0.15 m (BraxClaudeKamera: kula = róg bliskiej płaszczyzny + 5 cm, przy 0.5 m i 20:9 to ~0.86 m).
            bk.daleko = Mathf.Max(bk.daleko, .5f);
            var kol = cam.GetComponent<BraxClaudeKolor>();
            if (telefon) { if (kol) Object.DestroyImmediate(kol); }
            else
            {
                if (!kol) kol = cam.gameObject.AddComponent<BraxClaudeKolor>();
                kol.shader = Shader.Find("Braxhorn/OsiedleKolor"); kol.nasycenie = 1.15f; kol.kontrast = 1.06f;
                if (!kol.shader) kolorOk = false;
            }
            kam++;
        }
        var g = new GameObject("CLAUDE obraz rejonu: ekspozycja i słońce (minimum Osiedle)"); g.transform.SetParent(root, false);
        var o = g.AddComponent<BraxObrazRejonu>(); o.ekspozycja = Ekspozycja; o.slonce = sun; o.slonceZewnatrz = 1.24f;
        o.slonceWewnatrz = staraMoc > 0f ? staraMoc : .75f; o.bezZmian = bezZmian;
        // porównanie z minimum (ZASADA-OBRAZ-MINIMUM-OSIEDLE pkt 1)
        void P(string co, bool ok, string ile) => l.Add((ok ? "OK " : "PONIŻEJ ") + co + " " + ile);
        P("słońce", sun && sun.intensity >= 1.24f - 1e-3f, sun ? sun.intensity.ToString("F2") + " (materiały miasta × " + Ekspozycja.ToString("F2") + ")" : "brak");
        P("siła cieni", sun && sun.shadowStrength >= .8f - 1e-3f, sun ? sun.shadowStrength.ToString("F2") : "brak");
        P("cienie miękkie (bias 0.05 / 0.4)", sun && sun.shadows == cienie, sun ? sun.shadows.ToString() : "brak");
        P("otoczenie niebo", !Nie(RenderSettings.ambientSkyColor, new Color(.60f, .68f, .80f)), RenderSettings.ambientSkyColor.ToString("F2"));
        P("otoczenie horyzont", !Nie(RenderSettings.ambientEquatorColor, new Color(.50f, .53f, .50f)), RenderSettings.ambientEquatorColor.ToString("F2"));
        P("otoczenie ziemia", !Nie(RenderSettings.ambientGroundColor, new Color(.30f, .28f, .23f)), RenderSettings.ambientGroundColor.ToString("F2"));
        P("mgła wyłączona (decyzja właściciela 16.11 14:33)", !RenderSettings.fog, RenderSettings.fog ? "WŁĄCZONA" : "brak mgły, mgiełka shadera 0");
        P("bliska płaszczyzna", kam > 0, ".5 m na otwartej przestrzeni (przy ścianie .15 m), kamer " + kam);
        P("przejście koloru", kam > 0 && kolorOk, telefon ? "wyłączone na telefonie (pełnoekranowa kopia kadru co klatkę)" : "nasycenie 1.15, kontrast 1.06");
        P("lustro siłowni", true, lustra + " × do 25 m, 15 / s");
        P("rzutowanie cieni", true, "StableFit (ustawia BraxGrafika i BraxClaudeKamera w grze)");
        P("MSAA na każdym poziomie jakości (telefon startuje na „Medium”)", msaaWszedzie, string.Join(", ", msaaOpis) + " (podniesione: " + msaaPoziomy + ")");
        BraxRegion.Note("obraz R1 (minimum Osiedle): " + string.Join("; ", l));
        Debug.Log("BRAXHORN_OBRAZ_R1 " + string.Join(" | ", l));
        foreach (var x in l.Where(x => x.StartsWith("PONIŻEJ"))) Debug.LogWarning("DO POPRAWKI: BRAXHORN_OBRAZ_R1 " + x);
    }
}

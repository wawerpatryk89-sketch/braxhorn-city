using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// JEDNO NIEBO NA CAŁE MIASTO (właściciel 11.11 12:49: „spójrz na niebo, co się dzieje — to jest tak od 10 buildów”, 12:54:
// „tu widać niebo, linie — niebo musi być jedna całość na całym mieście”, 12:57: „zrób jedno niebo… i słońce, żeby było”).
//
// PRZYCZYNA LINII: w grze były DWA różne nieba. Niebo gry (RenderSettings.skybox) ustawiał na końcu AttachCity stary krok
// BraxCitySkyAuthoring (Skybox/Panoramic, obraz Kloofendal bez obrotu, nasycenie 1.0, bez słońca miasta) — po kroku horyzontu,
// który ustawił Braxhorn/MiastoNiebo. A szkło wieżowców (SzkloFasady), teren za granicą (MiastoDal) i las (MiastoDalLas)
// liczyły niebo wzorem MiastoTlo z materiału horyzontu: obraz obrócony (słońce obrazu na słońce miasta), nasycenie 1.3, pas gór
// i grzbiety 2D (w niebie gry zdjęte krokiem PANORAMA 3D). Gdzie szyba albo daleki teren przechodził w niebo — inne chmury,
// inny kolor = prostokąt, linia na niebie.
//
// TERAZ (ten krok, ostatni w AttachCity, po BraxCitySkyAuthoring): niebem gry jest materiał horyzontu (Braxhorn/MiastoNiebo),
// a WSZYSTKIE materiały liczące tło (MiastoDal, MiastoDalLas, SzkloFasady) dostają kopię jego parametrów — ten sam obraz,
// obrót, nasycenie, ekspozycja, ruch chmur, słońce, mgiełka, pas gór i grzbiety. Jedno niebo w każdym pikselu: nad głową,
// w odbiciu szyb i w przejściu dalekiego terenu w tło. Słońce: czysta tarcza tam, skąd pada światło sceny (cienie się zgadzają),
// z poświatą — widoczna na niebie i w odbiciach. Kontrola w logu: BRAXHORN_NIEBO_JEDNO (różnic parametrów = 0).
public static class BraxNieboJedno
{
    const string NieboSciezka = BraxRegion.Out + "/Horyzont/niebo_miasta.mat";
    const string NieboShader = "Braxhorn/MiastoNiebo";
    static readonly string[] ShaderyTla = { "Braxhorn/MiastoDal", "Braxhorn/MiastoDalLas", BraxSzkloFasady.ShaderName };
    // tarcza słońca: promień (°) i jasność; poświata w MiastoTlo.cginc (MT_Niebo)
    public const float SlonceStopnie = 1.0f, SlonceJasnosc = 2.2f;

    static Material ZnajdzNiebo()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(NieboSciezka);
        if (m && m.shader && m.shader.name == NieboShader) return m;
        if (RenderSettings.skybox && RenderSettings.skybox.shader && RenderSettings.skybox.shader.name == NieboShader) return RenderSettings.skybox;
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { BraxRegion.Out }))
        {
            var k = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (k && k.shader && k.shader.name == NieboShader) return k;
        }
        return null;
    }

    /// <summary>Niebo horyzontu jako niebo gry + parametry tła skopiowane do wszystkich materiałów MiastoTlo. false = brak nieba
    /// horyzontu (zostaje stare niebo, log DO POPRAWKI).</summary>
    public static bool Zastosuj(Camera kamera)
    {
        var niebo = ZnajdzNiebo();
        if (!niebo) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_NIEBO_JEDNO brak materiału " + NieboShader + " (krok horyzontu) — zostaje stare niebo, linie na niebie zostaną"); return false; }
        var stare = RenderSettings.skybox;
        string stareOpis = stare ? (stare.shader ? stare.shader.name : "?") + (stare.HasProperty("_MainTex") && stare.GetTexture("_MainTex") ? " " + stare.GetTexture("_MainTex").name : "") : "brak";

        // słońce: tarcza widoczna (było 0.6° i 1.6 — kropka ginęła w jasnym niebie)
        niebo.SetVector("_SloTarcza", new Vector4(Mathf.Cos(SlonceStopnie * Mathf.Deg2Rad), SlonceJasnosc, 0, 0));
        var sun = BraxCityMaterials.SunDirection.normalized;
        niebo.SetVector("_SloKierunek", new Vector4(sun.x, sun.y, sun.z, 0));
        EditorUtility.SetDirty(niebo);

        RenderSettings.skybox = niebo;
        if (kamera) kamera.clearFlags = CameraClearFlags.Skybox;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (cam && cam.targetTexture == null && cam.CompareTag("MainCamera")) cam.clearFlags = CameraClearFlags.Skybox;

        // materiały tła: w scenie (renderery) i zapisane w folderze regionu (np. szkło z pamięci podręcznej)
        var cele = new HashSet<Material>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (m && m != niebo && m.shader && ShaderyTla.Contains(m.shader.name)) cele.Add(m);
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { BraxRegion.Out }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m && m != niebo && m.shader && ShaderyTla.Contains(m.shader.name)) cele.Add(m);
        }
        var sh = niebo.shader; int nProp = sh.GetPropertyCount();
        var licznik = new Dictionary<string, int>(); int skopiowane = 0, roznice = 0;
        foreach (var m in cele)
        {
            // wszystkie parametry nieba — także te, których shader celu nie wypisuje w Properties (MiastoTlo.cginc deklaruje je
            // jako zmienne: _SloObraz, _SloTarcza w MiastoDal / MiastoDalLas) — inaczej słońce w tle terenu byłoby stare
            for (int i = 0; i < nProp; i++)
            {
                string p = sh.GetPropertyName(i);
                switch (sh.GetPropertyType(i))
                {
                    case ShaderPropertyType.Texture: m.SetTexture(p, niebo.GetTexture(p)); break;
                    case ShaderPropertyType.Color: m.SetColor(p, niebo.GetColor(p)); break;
                    case ShaderPropertyType.Vector: m.SetVector(p, niebo.GetVector(p)); break;
                    case ShaderPropertyType.Float: case ShaderPropertyType.Range: m.SetFloat(p, niebo.GetFloat(p)); break;
                    case ShaderPropertyType.Int: m.SetInteger(p, niebo.GetInteger(p)); break;
                }
                skopiowane++;
            }
            // kontrola: każdy wspólny parametr równy niebu
            for (int i = 0; i < nProp; i++)
            {
                string p = sh.GetPropertyName(i);
                bool ok = sh.GetPropertyType(i) switch
                {
                    ShaderPropertyType.Texture => m.GetTexture(p) == niebo.GetTexture(p),
                    ShaderPropertyType.Color => m.GetColor(p) == niebo.GetColor(p),
                    ShaderPropertyType.Vector => m.GetVector(p) == niebo.GetVector(p),
                    ShaderPropertyType.Int => m.GetInteger(p) == niebo.GetInteger(p),
                    _ => Mathf.Approximately(m.GetFloat(p), niebo.GetFloat(p)),
                };
                if (!ok) { roznice++; if (roznice <= 10) Debug.LogWarning("DO POPRAWKI: BRAXHORN_NIEBO_JEDNO różnica " + m.name + "." + p); }
            }
            EditorUtility.SetDirty(m);
            string k = m.shader.name.Replace("Braxhorn/", ""); licznik[k] = licznik.TryGetValue(k, out var c) ? c + 1 : 1;
        }
        AssetDatabase.SaveAssets();
        DynamicGI.UpdateEnvironment();
        var t = niebo.GetVector("_SloTarcza");
        Debug.Log("BRAXHORN_NIEBO_JEDNO niebo gry = " + niebo.shader.name + " (" + (niebo.GetTexture("_NieboTex") ? niebo.GetTexture("_NieboTex").name : "brak obrazu") +
                  "), było: " + stareOpis + "; materiały tła zgodne z niebem: " + string.Join(", ", licznik.Select(p => p.Key + " " + p.Value)) +
                  " (parametrów " + skopiowane + ", różnic " + roznice + "); obrót " + niebo.GetFloat("_NieboObrot").ToString("F3") + " rad, nasycenie " + niebo.GetFloat("_NieboNasycenie").ToString("F2") +
                  ", pas gór " + (niebo.GetFloat("_PasmoJest") > .5f ? "tak" : "nie (pierścień 3D)") +
                  "; słońce: elewacja " + (Mathf.Asin(sun.y) * Mathf.Rad2Deg).ToString("F1") + "°, azymut " + (Mathf.Atan2(sun.x, sun.z) * Mathf.Rad2Deg).ToString("F0") + "°, tarcza " + (2f * Mathf.Acos(t.x) * Mathf.Rad2Deg).ToString("F1") + "° jasność " + t.y.ToString("F1"));
        return true;
    }
}

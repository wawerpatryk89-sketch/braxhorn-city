using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// REJON BUILDU W CI + PAMIĘĆ PODRĘCZNA WYGENEROWANEGO REJONU (build 14, właściciel: „aktualizacja, a nie budowanie całego
// świata od nowa za każdym razem” — „to jest podstawa”; rejony OSOBNO: osiedle i miasto jako osobne buildy).
//
// Workflow (.github/workflows/claude-region-build.yml → unity-tools/claude/rejon_buildu.py) przed Unity zapisuje plik
// rejon-ci.txt (katalog projektu unity-city):
//   rejon=osiedle|miasto|oba   — co budować (bez pliku = „oba”, czyli jak przed buildem 14),
//   pamiec=tak|nie             — czy ten rejon korzysta z pamięci podręcznej (actions/cache),
//   klucz=<…>                  — SHA-256 kodu generatora rejonu + wszystkich źródeł + wersji Unity + rejonu + platformy.
// TRAFIENIE: workflow przywrócił Assets/PlayableGenerated, wszystkie pliki .meta i ProjectSettings z udanego buildu o TYM
// SAMYM kluczu, a znacznik Assets/PlayableGenerated/pamiec-rejonu.json (pisany na końcu udanego generowania) zgadza się
// z rejonem, kluczem i platformą → generowanie pominięte, paczka z gotowych scen. Inaczej pełne generowanie jak dotąd.
// Zmiana czegokolwiek w kodzie generatora / źródłach rejonu = nowy klucz = pełne generowanie (nigdy stary świat z nowym kodem).
public static class BraxRejonBuildu
{
    const string PlikCI = "rejon-ci.txt";
    public const string Znacznik = "Assets/PlayableGenerated/pamiec-rejonu.json";
    // Pliki pisane przez Unity poza PlayableGenerated i czytane po buildzie (unity-tools/kickboxing/package.py) — są w ścieżkach
    // pamięci w workflow; tu sprawdzane, żeby trafienie bez nich nie wywaliło joba (recenzja 2.11, K1).
    static readonly string[] Wymagane = { "Assets/Source/Premium/unity-premium-report.json", "Assets/Source/Premium/source-refinements.json" };

    // Ustawienia projektu (ProjectSettings) zmieniane przez kod generatora (siłownia, osiedle, miasto). Zapisane w znaczniku —
    // przy trafieniu porównane z przywróconymi ProjectSettings i w razie różnicy ustawione ze znacznika (zabezpieczenie).
    // NOWE globalne ustawienie w generatorze (QualitySettings / PlayerSettings) → dopisać tutaj.
    [Serializable] class Ustawienia
    {
        public int przestrzenBarw, pixelLightCount, antiAliasing, cienie, rozdzielczoscCieni, rzutowanieCieni, kaskadyCieni, anizotropia;
        public float zasiegCieni, lodBias;
        public bool kolizjeWBuildzie, webglFallback;
        public int webglKompresja;
        public string webglSzablon, firma;
    }
    [Serializable] class Stan
    {
        public string rejon, klucz, platforma, commit, utworzono;
        public string[] sceny;
        public string[] zaleznosci;   // wszystkie zasoby, których potrzebują sceny (recenzja 2.11: pliki generatora poza PlayableGenerated)
        public bool ustawieniaNaDysku;
        public Ustawienia ustawienia;
    }

    static Dictionary<string, string> ci;
    static string Wartosc(string k)
    {
        if (ci == null)
        {
            ci = new Dictionary<string, string>();
            try
            {
                if (File.Exists(PlikCI))
                    foreach (var linia in File.ReadAllLines(PlikCI))
                    {
                        var l = linia.Trim(); int i = l.IndexOf('=');
                        if (l.Length == 0 || l[0] == '#' || i <= 0) continue;
                        ci[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim();
                    }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: " + PlikCI + ": " + e.Message); }
        }
        return ci.TryGetValue(k, out var v) ? v : null;
    }

    public static string Rejon { get { var r = (Wartosc("rejon") ?? "").ToLowerInvariant(); return r == "osiedle" || r == "miasto" ? r : "oba"; } }
    public static string Klucz => Wartosc("klucz") ?? "";
    static bool PamiecWlaczona => Wartosc("pamiec") == "tak" && Klucz.Length > 10;

    // Pomiar etapów do logu CI: BRAXHORN_CZAS etap=<nazwa> [rejon=… z_pamięci=…] s=<sekundy>
    public static void Czas(string etap, DateTime od, string dodatek = null) { Sekundy(etap, (DateTime.UtcNow - od).TotalSeconds, dodatek); }
    public static void Sekundy(string etap, double s, string dodatek = null)
    {
        Debug.Log("BRAXHORN_CZAS etap=" + etap + (string.IsNullOrEmpty(dodatek) ? "" : " " + dodatek) + " s=" + Math.Round(s).ToString(CultureInfo.InvariantCulture));
    }

    static Ustawienia Teraz()
    {
        return new Ustawienia
        {
            przestrzenBarw = (int)PlayerSettings.colorSpace, pixelLightCount = QualitySettings.pixelLightCount, antiAliasing = QualitySettings.antiAliasing,
            cienie = (int)QualitySettings.shadows, rozdzielczoscCieni = (int)QualitySettings.shadowResolution, rzutowanieCieni = (int)QualitySettings.shadowProjection,
            kaskadyCieni = QualitySettings.shadowCascades, anizotropia = (int)QualitySettings.anisotropicFiltering, zasiegCieni = QualitySettings.shadowDistance,
            lodBias = QualitySettings.lodBias, kolizjeWBuildzie = PlayerSettings.bakeCollisionMeshes, webglFallback = PlayerSettings.WebGL.decompressionFallback,
            webglKompresja = (int)PlayerSettings.WebGL.compressionFormat, webglSzablon = PlayerSettings.WebGL.template, firma = PlayerSettings.companyName,
        };
    }
    static List<string> Roznice(Ustawienia zapisane, Ustawienia teraz)
    {
        var r = new List<string>();
        void P(string n, object a, object b) { if (!Equals(a, b)) r.Add(n + " " + b + "→" + a); }
        P("przestrzeń barw", (ColorSpace)zapisane.przestrzenBarw, (ColorSpace)teraz.przestrzenBarw); P("pixelLightCount", zapisane.pixelLightCount, teraz.pixelLightCount);
        P("antiAliasing", zapisane.antiAliasing, teraz.antiAliasing); P("cienie", (ShadowQuality)zapisane.cienie, (ShadowQuality)teraz.cienie);
        P("rozdzielczość cieni", (ShadowResolution)zapisane.rozdzielczoscCieni, (ShadowResolution)teraz.rozdzielczoscCieni);
        P("rzutowanie cieni", (ShadowProjection)zapisane.rzutowanieCieni, (ShadowProjection)teraz.rzutowanieCieni); P("kaskady cieni", zapisane.kaskadyCieni, teraz.kaskadyCieni);
        P("anizotropia", (AnisotropicFiltering)zapisane.anizotropia, (AnisotropicFiltering)teraz.anizotropia);
        if (Mathf.Abs(zapisane.zasiegCieni - teraz.zasiegCieni) > .001f) r.Add("zasięg cieni " + teraz.zasiegCieni + "→" + zapisane.zasiegCieni);
        if (Mathf.Abs(zapisane.lodBias - teraz.lodBias) > .001f) r.Add("lodBias " + teraz.lodBias + "→" + zapisane.lodBias);
        P("kolizje liczone w buildzie", zapisane.kolizjeWBuildzie, teraz.kolizjeWBuildzie); P("WebGL fallback", zapisane.webglFallback, teraz.webglFallback);
        P("WebGL kompresja", (WebGLCompressionFormat)zapisane.webglKompresja, (WebGLCompressionFormat)teraz.webglKompresja);
        P("WebGL szablon", zapisane.webglSzablon ?? "", teraz.webglSzablon ?? ""); P("firma", zapisane.firma ?? "", teraz.firma ?? "");
        return r;
    }
    static void Ustaw(Ustawienia u)
    {
        if (PlayerSettings.colorSpace != (ColorSpace)u.przestrzenBarw) PlayerSettings.colorSpace = (ColorSpace)u.przestrzenBarw;   // zmiana = ponowny import tekstur
        QualitySettings.pixelLightCount = u.pixelLightCount; QualitySettings.antiAliasing = u.antiAliasing; QualitySettings.shadows = (ShadowQuality)u.cienie;
        QualitySettings.shadowResolution = (ShadowResolution)u.rozdzielczoscCieni; QualitySettings.shadowProjection = (ShadowProjection)u.rzutowanieCieni;
        QualitySettings.shadowCascades = u.kaskadyCieni; QualitySettings.anisotropicFiltering = (AnisotropicFiltering)u.anizotropia;
        QualitySettings.shadowDistance = u.zasiegCieni; QualitySettings.lodBias = u.lodBias; PlayerSettings.bakeCollisionMeshes = u.kolizjeWBuildzie;
        PlayerSettings.WebGL.decompressionFallback = u.webglFallback; PlayerSettings.WebGL.compressionFormat = (WebGLCompressionFormat)u.webglKompresja;
        if (!string.IsNullOrEmpty(u.webglSzablon)) PlayerSettings.WebGL.template = u.webglSzablon;
        if (!string.IsNullOrEmpty(u.firma)) PlayerSettings.companyName = u.firma;
    }

    // Czy świat rejonu przywrócony z pamięci podręcznej CI nadaje się do paczki (ten sam rejon, klucz, platforma, sceny na dysku).
    public static bool ZPamieci(BuildTarget target, out string[] sceny)
    {
        sceny = null; string rejon = Rejon;
        if (rejon == "oba") return false;
        if (!PamiecWlaczona) { Debug.Log("BRAXHORN_PAMIEC rejon=" + rejon + " bez pamięci podręcznej (pamiec=" + (Wartosc("pamiec") ?? "brak") + ") — pełne generowanie"); return false; }
        if (!File.Exists(Znacznik))
        {
            // przywrócone sceny bez znacznika (np. przerwany zapis znacznika) — nie mieszamy ich z nowym generowaniem
            if (Directory.Exists("Assets/PlayableGenerated") && Directory.GetFiles("Assets/PlayableGenerated", "*.unity").Length > 0)
            { Debug.LogWarning("BRAXHORN_PAMIEC rejon=" + rejon + " odrzucona: sceny bez znacznika świata — usuwam i generuję od nowa"); Wyczysc(); }
            else Debug.Log("BRAXHORN_PAMIEC rejon=" + rejon + " pudło: brak zapisanego świata dla klucza " + Klucz + " — pełne generowanie, zapis po udanym buildzie");
            return false;
        }
        Stan s = null;
        try { s = JsonUtility.FromJson<Stan>(File.ReadAllText(Znacznik)); } catch (Exception e) { Debug.LogWarning("BRAXHORN_PAMIEC znacznik nieczytelny: " + e.Message); }
        string powod = s == null ? "znacznik nieczytelny"
            : s.rejon != rejon ? "znacznik innego rejonu (" + s.rejon + ")"
            : s.klucz != Klucz ? "znacznik innego klucza (" + s.klucz + ")"
            : s.platforma != target.ToString() ? "znacznik innej platformy (" + s.platforma + ")"
            // flaga z końca generowania; gdy Unity zapisało ProjectSettings dopiero przy wyjściu, przywrócone mają już kolor liniowy
            : !s.ustawieniaNaDysku && s.ustawienia != null && (int)PlayerSettings.colorSpace != s.ustawienia.przestrzenBarw ? "ustawienia projektu (ProjectSettings) nie zapisały się na dysk w buildzie, który generował świat"
            : s.ustawienia == null || s.sceny == null || s.sceny.Length == 0 ? "znacznik bez scen / ustawień"
            : s.sceny.FirstOrDefault(p => !File.Exists(p)) is string brak ? "brak sceny " + brak
            : (s.zaleznosci ?? new string[0]).FirstOrDefault(p => !File.Exists(p)) is string brakZ ? "brak zasobu sceny " + brakZ + " (nie wrócił z pamięci)"
            : Wymagane.FirstOrDefault(p => !File.Exists(p)) is string brakW ? "brak pliku " + brakW + " (czyta go pakowanie playera)" : null;
        if (powod != null)
        {
            Debug.LogWarning("BRAXHORN_PAMIEC rejon=" + rejon + " odrzucona: " + powod + " — usuwam przywrócone Assets/PlayableGenerated i generuję od nowa");
            Wyczysc();
            return false;
        }
        var roz = Roznice(s.ustawienia, Teraz());
        if (roz.Count > 0) { Ustaw(s.ustawienia); AssetDatabase.SaveAssets(); Debug.LogWarning("BRAXHORN_PAMIEC ustawienia projektu ustawione ze znacznika (ProjectSettings z pamięci różniły się): " + string.Join(", ", roz)); }
        else Debug.Log("BRAXHORN_PAMIEC ustawienia projektu zgodne ze światem z pamięci");
        sceny = s.sceny;
        Debug.Log("BRAXHORN_PAMIEC rejon=" + rejon + " TRAFIENIE: świat wygenerowany w commicie " + s.commit + " (" + s.utworzono + "), klucz " + s.klucz + ", sceny: " + string.Join(", ", s.sceny));
        return true;
    }

    // Usunięcie przywróconego świata (zostaje tylko odchudzanie.json — wejście z kroku przygotowania CI, nie wynik generatora).
    static void Wyczysc()
    {
        try
        {
            const string g = "Assets/PlayableGenerated";
            foreach (var d in Directory.GetDirectories(g)) AssetDatabase.DeleteAsset(d.Replace('\\', '/'));
            foreach (var f in Directory.GetFiles(g).Where(x => !x.EndsWith(".meta") && Path.GetFileName(x) != "odchudzanie.json")) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: czyszczenie odrzuconej pamięci rejonu: " + e.Message); }
    }

    // Zasoby, od których zależą sceny rejonu (bez skryptów i pakietów — te są w repo / w Unity). Brak któregokolwiek po
    // przywróceniu = świat odrzucony i pełne generowanie (zamiast cicho zerwanej referencji, np. odbić nieba).
    static string[] Zaleznosci(string[] sceny)
    {
        try
        {
            return AssetDatabase.GetDependencies(sceny, true)
                .Where(p => p.StartsWith("Assets/") && !p.EndsWith(".cs") && !p.EndsWith(".unity"))
                .Distinct().OrderBy(p => p).ToArray();
        }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: zależności scen do znacznika pamięci: " + e.Message); return new string[0]; }
    }

    // Na końcu udanego builda z pełnym generowaniem: znacznik świata (workflow zapisze pamięć podręczną po udanym jobie).
    public static void ZapiszZnacznik(BuildTarget target, string[] sceny, string commit, DateTime startBuildu)
    {
        string rejon = Rejon; if (rejon == "oba") return;
        try
        {
            AssetDatabase.SaveAssets();   // ProjectSettings (kolor liniowy, cienie…) na dysk — idą do pamięci razem ze światem
            const string ps = "ProjectSettings/ProjectSettings.asset";
            bool naDysku = false;
            try
            {
                naDysku = File.Exists(ps) && File.Exists("ProjectSettings/QualitySettings.asset")
                    && (File.GetLastWriteTimeUtc(ps) >= startBuildu.AddSeconds(-5) || File.ReadAllText(ps).Contains("m_ActiveColorSpace: " + (int)PlayerSettings.colorSpace));
            }
            catch { }
            var s = new Stan { rejon = rejon, klucz = Klucz, platforma = target.ToString(), commit = commit, utworzono = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture), sceny = sceny, zaleznosci = Zaleznosci(sceny), ustawieniaNaDysku = naDysku, ustawienia = Teraz() };
            File.WriteAllText(Znacznik, JsonUtility.ToJson(s, true));
            if (naDysku) Debug.Log("BRAXHORN_PAMIEC rejon=" + rejon + " zapisany znacznik świata (klucz " + Klucz + ", scen " + sceny.Length + ") — workflow zapisze pamięć po udanym jobie" + (PamiecWlaczona ? "" : " (ten rejon bez pamięci w CI)"));
            else Debug.LogWarning("BRAXHORN_PAMIEC UWAGA: ProjectSettings nie zapisały się przy SaveAssets — przy trafieniu liczy się zapis przy wyjściu z Unity (sprawdzane po przestrzeni barw; inaczej świat odrzucony = pełne generowanie)");
        }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: znacznik pamięci rejonu: " + e.Message); }
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// JEDEN OBRAZ — CZĘŚĆ WYPALANA W BUILDZIE (właściciel 16.11, 13:17: „parametry dla każdego
// rejonu, każdy build który odpalę ma na nich iść”).
//
// Większość standardu wymusza BraxObrazGlobalny po wczytaniu scenu. Są jednak dwie rzeczy,
// których NIE WOLNO ustawiać w trakcie gry i które trzeba wypalić tutaj:
//
// 1. WYGŁADZANIE KRAWĘDZI (MSAA). BraxGrafika.Msaa() na telefonie wychodzi bez ustawienia
//    czegokolwiek, i słusznie: zmiana MSAA w trakcie gry odtwarza bufor ekranu, a na Vulkanie
//    zamykało to aplikację (właściciel 31.10, 21:33: „wypierdala z aplikacji”). Ale to zakaz
//    ZMIANY, nie zakaz POSIADANIA. Wypalone w buildzie nikt już nie zmienia.
//    Bez tego cienkie rzeczy — pasy na jezdni, balustrady, przewody, krawędzie dachów —
//    migoczą przy każdym kroku (właściciel 16.11, 12:54: „migotają najczęściej pasy, paski”),
//    bo biała linia 12 cm widziana z 50 m jest węższa od piksela.
//    Ustawione 2×, nie 4×: 2× zbiera większość migotania, a kosztuje ok. 10 % wypełnienia.
//    Podniesienie do 4× dopiero po sprawdzeniu na telefonie.
//
// 2. ANIZOTROPIA. Wypalona, żeby posadzki i asfalt nie pełzały od pierwszej klatki, zanim
//    BraxObrazGlobalny w ogóle wstanie.
//
// Każdy build wypisuje BRAX_OBRAZ_BUILD — widać w logu, że poszedł na standardzie.
public sealed class BraxObrazBuild : IPreprocessBuildWithReport
{
    public const int MsaaTelefon = 2;
    public const int MsaaKomputer = 4;

    public int callbackOrder => -100;   // przed resztą, zanim cokolwiek zacznie się serializować

    public void OnPreprocessBuild(BuildReport report)
    {
        var cel = report.summary.platform;
        bool telefon = cel == BuildTarget.Android || cel == BuildTarget.iOS;
        int msaa = telefon ? MsaaTelefon : MsaaKomputer;

        int bylo = QualitySettings.antiAliasing;
        var byloAniz = QualitySettings.anisotropicFiltering;
        var byloRzut = QualitySettings.shadowProjection;

        QualitySettings.antiAliasing = msaa;
        if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;

        // Kolor kadru leży w Assets/Claude/Resources, więc nie zostanie wycięty z buildu
        // i BraxObrazGlobalny wczyta go w każdym rejonie (Resources.Load<Shader>).
        var kolor = Resources.Load<Shader>("OsiedleKolor");
        bool kolorJest = kolor || Shader.Find("Braxhorn/OsiedleKolor");

        AssetDatabase.SaveAssets();

        Debug.Log(string.Format(
            "BRAX_OBRAZ_BUILD cel={0} msaa={1}->{2} anizotropia={3}->{4} rzutowanie={5}->{6} " +
            "kolor_kadru={7} przestrzen_barw={8}",
            cel, bylo, QualitySettings.antiAliasing, byloAniz, QualitySettings.anisotropicFiltering,
            byloRzut, QualitySettings.shadowProjection,
            kolorJest ? "jest" : "BRAK — rejony pojda bez gradingu",
            PlayerSettings.colorSpace));

        if (!kolorJest)
            Debug.LogWarning("BRAX_OBRAZ_BUILD: nie ma shadera Braxhorn/OsiedleKolor w Resources — " +
                             "nasycenie i kontrast nie wejdą. Sprawdź Assets/Claude/Resources/OsiedleKolor.shader");
    }
}

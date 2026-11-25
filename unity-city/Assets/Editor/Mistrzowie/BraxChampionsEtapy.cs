using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

// DWA PRZEBIEGI JAK MIASTO R1 (właściciel 25.11.2026 17:22 „Go” na: „Build jak u R1: limit czasu: jak Unity nie zdąży,
// kończy czysto po zbudowaniu świata, zapis świata, także po przerwaniu, drugi przebieg robi tylko APK”).
// #52 (ostatnie APK, 18.11): import 43 min, generowanie 1 h 50 min, sam APK 2 h 07 min - krok Unity 4 h 46 min, czyli tyle,
// ile wolno całemu krokowi; każdy wolniejszy runner ucinał build bez śladu i bez zapisu.
//  - workflow pisze termin kroku Unity do mistrzowie-ci.txt (termin=<sekundy epoki>, reszta_min=<ile trwa APK>);
//  - gdy świat jest gotowy (sceny wygenerowane i podzielone), Unity zapisuje zasoby i mistrzowie-swiat-gotowy.txt (commit
//    źródła); workflow pakuje świat (pliki zmienione przez Unity) zaszyfrowany do magazynu, także gdy potem APK padnie;
//  - gdy do terminu zostaje mniej niż reszta_min: etap 1 kończy się czysto (build/Android/etap-1.txt), bez APK;
//  - następny przebieg tego samego commitu przywraca świat (mistrzowie-swiat-przywrocony.txt) i robi tylko APK.
public static class BraxChampionsEtapy
{
    public const string Termin = "mistrzowie-ci.txt";
    public const string Gotowy = "mistrzowie-swiat-gotowy.txt";
    public const string Przywrocony = "mistrzowie-swiat-przywrocony.txt";
    public const string Etap1 = "../build/Android/etap-1.txt";

    static string Czytaj(string path) { try { return File.Exists(path) ? File.ReadAllText(path).Trim() : ""; } catch (Exception) { return ""; } }

    /// <summary>The world of this very commit was restored before Unity started (stage 2: the APK only).</summary>
    public static bool SwiatPrzywrocony(string commit) =>
        !string.IsNullOrEmpty(commit) && Czytaj(Przywrocony) == commit && Czytaj(Gotowy) == commit;

    /// <summary>The world is complete: every asset saved, then the marker the workflow packs it by.</summary>
    public static void SwiatGotowy(string commit)
    {
        AssetDatabase.SaveAssets();
        File.WriteAllText(Gotowy, commit + "\n");
        Debug.Log("BRAX_ETAP swiat gotowy commit=" + commit + " " + DateTime.UtcNow.ToString("HH:mm:ss"));
    }

    static long Wartosc(string tekst, string klucz)
    {
        foreach (var l in tekst.Split('\n'))
            if (l.StartsWith(klucz + "=") && long.TryParse(l.Substring(klucz.Length + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v))
                return v;
        return -1;
    }

    /// <summary>True when the APK would not fit before the step's deadline: stage 1 ends here, cleanly, without the APK.</summary>
    public static bool KoniecEtapu1(string commit)
    {
        string t = Czytaj(Termin);
        long termin = Wartosc(t, "termin"), reszta = Wartosc(t, "reszta_min");
        if (termin <= 0 || reszta <= 0) { Debug.Log("BRAX_ETAP brak terminu - APK w tym przebiegu"); return false; }
        double zostalo = (termin - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) / 60.0;
        Debug.Log("BRAX_ETAP do terminu " + zostalo.ToString("F0", CultureInfo.InvariantCulture) + " min, APK potrzebuje " + reszta + " min");
        if (zostalo >= reszta) return false;
        Directory.CreateDirectory(Path.GetDirectoryName(Etap1));
        File.WriteAllText(Etap1, "ETAP 1 z 2: swiat Miasta Mistrzow wygenerowany (commit " + commit + "), do terminu " +
            zostalo.ToString("F0", CultureInfo.InvariantCulture) + " min < " + reszta + " min na APK. Swiat zapisany - nastepny przebieg tego commitu robi tylko APK.\n");
        Debug.Log("BRAX_ETAP etap 1 koniec: APK w nastepnym przebiegu");
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// ZAPIS ASSETÓW BEZ NADPISYWANIA (C1, właściciel 2.11: różowe jachty, motorówki, skutery, drzewa przy plaży, budka ratownika,
// żurawie portu, przedmioty przy arenie). AssetDatabase.CreateAsset na ISTNIEJĄCEJ ścieżce USUWA stary plik (nowy GUID) —
// wszystkie obiekty, które już używały starego materiału/siatki, zostają z NULL → magenta / brak bryły. W buildzie folder
// Assets/PlayableGenerated jest na starcie pusty (świeże CI albo BraxRejonBuildu.Wyczysc), więc KAŻDY istniejący plik pod tą
// ścieżką powstał w tym samym buildzie i może być w użyciu. Dlatego cały generator zapisuje przez Utworz():
//   ścieżka wolna → zapis jak dotąd; zajęta → ten sam plik w podfolderze _kolizja_N (nazwa obiektu = nazwa pliku zostaje,
//   np. klip animacji o nazwie klucza), stary plik nietknięty; log BRAXHORN_ZAPIS_KOLIZJA (do znalezienia źródła).
// Ścieżki spoza Assets/PlayableGenerated (narzędzia ręczne, źródła) — bez zmian (nadpisanie zamierzone).
public static class BraxZapisAssetu
{
    public static int Kolizje, Zapisy, FolderyZPlikami;
    static readonly List<string> przyklady = new List<string>();

    static bool Zajeta(string path) =>
        File.Exists(path) || AssetDatabase.GetMainAssetTypeAtPath(path) != null;

    /// <summary>Wolna ścieżka pliku: ta sama albo (gdy zajęta w tym buildzie) ten sam plik w podfolderze _kolizja_N.</summary>
    public static string Wolna(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.Replace('\\', '/').StartsWith("Assets/PlayableGenerated/") || !Zajeta(path)) return path;
        string dir = Path.GetDirectoryName(path).Replace('\\', '/'), file = Path.GetFileName(path), nowa = path;
        for (int i = 1; i < 10000; i++)
        {
            string sub = dir + "/_kolizja_" + i;
            nowa = sub + "/" + file;
            if (Zajeta(nowa)) continue;
            try
            {
                if (!AssetDatabase.IsValidFolder(sub))
                {
                    if (AssetDatabase.IsValidFolder(dir) && !Directory.Exists(sub)) AssetDatabase.CreateFolder(dir, "_kolizja_" + i);
                    if (!Directory.Exists(sub)) Directory.CreateDirectory(sub);
                }
            }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: zapis assetu — folder " + sub + ": " + e.Message); }
            break;
        }
        Kolizje++;
        if (przyklady.Count < 25) przyklady.Add(path);
        if (Kolizje <= 60) Debug.LogWarning("BRAXHORN_ZAPIS_KOLIZJA " + path + " był już zapisany w tym buildzie (nadpisanie = różowe obiekty) → " + nowa);
        return nowa;
    }

    /// <summary>AssetDatabase.CreateAsset bez nadpisywania istniejącego pliku. Zwraca ścieżkę faktycznego zapisu.</summary>
    public static string Utworz(Object obiekt, string path)
    {
        string p = Wolna(path);
        AssetDatabase.CreateAsset(obiekt, p);
        Zapisy++;
        return p;
    }

    /// <summary>Usunięcie folderu wyników kroku (początek kroku). W CI folder jest pusty; pliki w nim = zrobione wcześniej
    /// w tym buildzie i mogą być w użyciu (usunięcie = różowe obiekty) — log, żeby to wyłapać.</summary>
    public static void UsunFolder(string dir, string krok)
    {
        if (!AssetDatabase.IsValidFolder(dir)) return;
        int n = 0;
        try { foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) if (!f.EndsWith(".meta")) n++; } catch { }
        if (n > 0) { FolderyZPlikami++; Debug.LogWarning("BRAXHORN_ZAPIS_FOLDER " + krok + ": usuwany folder " + dir + " miał już " + n + " plików z tego buildu — jeśli coś ich używa, będzie różowe/bez siatki (BRAXHORN_MATERIALY_BRAK)"); }
        AssetDatabase.DeleteAsset(dir);
    }

    public static string Podsumowanie() =>
        "zapisy=" + Zapisy + " kolizje=" + Kolizje + " foldery_z_plikami=" + FolderyZPlikami + (przyklady.Count > 0 ? " [" + string.Join(", ", przyklady) + "]" : "");
}

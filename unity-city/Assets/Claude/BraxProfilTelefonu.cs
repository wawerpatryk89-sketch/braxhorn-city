using UnityEngine;

// AUTOMATYCZNA JAKOŚĆ GRAFIKI WG TELEFONU (build 14; decyzja właściciela 21:42–21:46: Motorola Edge 60 Pro = WYSOKA,
// moto g86 = telefon testowy, ŚREDNIA). Wynik = domyślny tryb BraxGrafika, dopóki gracz nie wybierze ręcznie w MENU
// (wybór ręczny zapamiętany w PlayerPrefs i ma pierwszeństwo).
//  • Edge 60 Pro (Dimensity 8350, Mali-G615 MC6, 12 GB) i inne mocne: Mali G6xx/G7xx z ≥ 6 rdzeniami, Immortalis, Adreno ≥ 725,
//    Xclipse, Apple — przy ≥ 7.5 GB RAM → WYSOKA;
//  • moto g86 (Dimensity 7300, Mali-G615 MC2) → ŚREDNIA;
//  • < 5 GB RAM → NISKA; każdy inny telefon → ŚREDNIA; komputer → WYSOKA.
public static class BraxProfilTelefonu
{
    static bool policzony; static int tryb; static string powod;

    public static int Domyslny() { Licz(); return tryb; }
    public static string Powod { get { Licz(); return powod; } }

    static void Licz()
    {
        if (policzony) return;
        policzony = true;
        if (!Application.isMobilePlatform) { tryb = 2; powod = "komputer"; return; }
        string model = (SystemInfo.deviceModel ?? "").ToLowerInvariant();
        string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
        int ram = SystemInfo.systemMemorySize;
        if (model.Contains("edge 60 pro") || model.Contains("edge60 pro") || model.Contains("edge 60pro")) { tryb = 2; powod = "Motorola Edge 60 Pro"; return; }
        if (model.Contains("g86")) { tryb = 1; powod = "moto g86"; return; }
        if (ram > 0 && ram < 5000) { tryb = 0; powod = "mało pamięci (" + ram + " MB)"; return; }
        if (ram >= 7500 && MocneGpu(gpu)) { tryb = 2; powod = "mocna grafika: " + SystemInfo.graphicsDeviceName; return; }
        tryb = 1; powod = SystemInfo.graphicsDeviceName;
    }

    // „mali-g615 mc6”, „mali-g715 mc7”, „immortalis-g720 mc12”, „adreno (tm) 740”, „xclipse 940”, „apple a18 gpu”
    static bool MocneGpu(string g)
    {
        if (g.Contains("immortalis") || g.Contains("xclipse") || g.Contains("apple")) return true;
        if (g.Contains("mali-g"))
        {
            int mc = Liczba(g, "mc"), seria = Liczba(g, "mali-g");
            return seria >= 610 && mc >= 6;
        }
        if (g.Contains("adreno")) return Liczba(g, "adreno") >= 725 || Liczba(g, ")") >= 725;
        return false;
    }

    // pierwsza liczba po znaczniku (np. „mc6” → 6), 0 gdy brak
    static int Liczba(string s, string po)
    {
        int i = s.IndexOf(po, System.StringComparison.Ordinal); if (i < 0) return 0;
        i += po.Length; int v = 0, n = 0;
        while (i < s.Length && (s[i] == ' ' || s[i] == '(' || s[i] == 't' || s[i] == 'm' || s[i] == ')') && n == 0) i++;
        while (i < s.Length && char.IsDigit(s[i]) && n < 5) { v = v * 10 + (s[i] - '0'); i++; n++; }
        return v;
    }
}

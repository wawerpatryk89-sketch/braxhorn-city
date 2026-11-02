using UnityEngine;

// GODZINY OTWARCIA LOKALI (build 45 runda 2, właściciel: „dyskoteka otwarta wieczorami”). Na razie gra nie ma zegara doby —
// flaga ZawszeOtwarte = true (klub działa zawsze). Gdy przyjdzie zegar gry: Godzina = () => zegar (0–24), ZawszeOtwarte = false.
// Pytają: efekty klubu (ściana LED, lasery, kula), bar (ZAMÓW, nalewanie).
public static class BraxGodzinyOtwarcia
{
    public static bool ZawszeOtwarte = true;
    public static System.Func<float> Godzina;   // null = zegar systemowy

    public static bool Otwarte(string lokal)
    {
        if (ZawszeOtwarte) return true;
        float h = Godzina != null ? Godzina() : (float)System.DateTime.Now.TimeOfDay.TotalHours;
        switch (lokal)
        {
            case "dyskoteka": return h >= 20f || h < 4f;
            case "kasyno": return h >= 12f || h < 4f;
            default: return h >= 8f && h < 22f;
        }
    }
}

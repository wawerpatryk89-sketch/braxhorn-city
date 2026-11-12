using UnityEditor;
using UnityEngine;

// PLAKATY BRAXHORN NUTRITION Z KOLEGAMI WŁAŚCICIELA (12.11.2026, 02:34–02:38: „moich kolegów z plakatów — na siłowniach, na sklepach
// z odżywkami mają być oni”; „masz zdjęcia, możesz zrobić ich sam”). Grafiki właściciela bez zmian treści, w zaszyfrowanej paczce
// globalnej braxhorn-sklep-plakaty-01 (CI rozpakowuje do Assets/Source/Prywatne/Miasto/plakaty_01/). Plakat w ramie z czarnego
// aluminium: grafika 2.5 cm przed licem ściany (zero migotania), rama 3 cm na zewnątrz grafiki, głębokość 3.5 cm. Bez paczki — nic
// (ściana zostaje czysta, bez zastępczych klocków). Wspólne dla innych rejonów (kopiować): Plakat.
internal static class BraxPlakaty
{
    public const string Katalog = "Assets/Source/Prywatne/Miasto/plakaty_01/";
    public const float Aspekt = 1169f / 1280f;
    public static bool Jest => System.IO.File.Exists(Katalog + "plakat_01.jpg");

    /// <summary>Plakat nr (1, 2) na ścianie: srodek = punkt na licu ściany w środku plakatu, n = normalna ściany (do pomieszczenia),
    /// wys = wysokość grafiki w metrach świata. Zwraca false, gdy paczki nie ma.</summary>
    public static bool Plakat(Transform rodzic, string nazwa, int nr, Vector3 srodek, Vector3 n, float wys)
    {
        string plik = Katalog + "plakat_0" + Mathf.Clamp(nr, 1, 2) + ".jpg";
        if (!System.IO.File.Exists(plik)) return false;
        AssetDatabase.ImportAsset(plik, ImportAssetOptions.ForceSynchronousImport);
        n.y = 0f; n.Normalize(); var R = -Vector3.Cross(Vector3.up, n).normalized;   // prawo patrzącego na plakat
        float w = wys * Aspekt, h = wys;
        var ld = srodek + n * .025f - R * (w * .5f) - Vector3.up * (h * .5f);
        if (!BraxMiastoA.Obraz(rodzic, "plakat " + nazwa, plik, ld, R, w, h, n, 0f, .6f)) return false;
        var rama = new UlSiatka();   // własna siatka (działa też poza grupami BraxUliceKit, np. w siłowni)
        float b = .03f, d = .035f; var c = srodek + n * (d * .5f);
        rama.OBox(c + Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, d);
        rama.OBox(c - Vector3.up * (h * .5f + b * .5f), R, Vector3.up, n, w + 2f * b, b, d);
        rama.OBox(c + R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, d);
        rama.OBox(c - R * (w * .5f + b * .5f), R, Vector3.up, n, b, h, d);
        BraxMiastoA.Obiekt(rodzic, "Plakat: rama · " + nazwa, rama, BraxMiastoA.Mat("plakat: rama aluminium czarne", new Color(.04f, .04f, .045f), .35f, .6f));
        return true;
    }
}

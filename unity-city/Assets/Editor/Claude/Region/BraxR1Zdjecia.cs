using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// ZDJĘCIA Z EDYTORA W BUILDZIE R1 (właściciel 16.11.2026 19:02: „bez zdjęcia nic nie akceptuje, nie ma opcji”).
// Zdjęcia CI z playera WebGL są wyłączone (właściciel 3.11 18:40: „to tylko czas zajmuje” — 12 części × 80 min). Tu zamiast nich
// kilkanaście ujęć z GOTOWEJ sceny miasta w edytorze, w tym samym przebiegu APK, przed zapisem i podziałem na obszary (wszystko
// w scenie): kamera tymczasowa 55° (jak w grze), 1600 × 900, MSAA 4, ekspozycja i moc słońca jak przy starcie sceny
// (BraxObrazRejonu), bez mgły. Pliki JPG w unity-city/Logs/r1-zdjecia (artefakt „android-build-diagnostics”, razem z logiem).
// Bez karty graficznej (urządzenie Null) — tylko wpis w logu, build idzie dalej. Korekcja koloru gry (BraxClaudeKolor) nie działa
// w edytorze — kolory na zdjęciu są przed korekcją.
public static class BraxR1Zdjecia
{
    public const string Katalog = "Logs/r1-zdjecia";
    const int Szer = 1600, Wys = 900;

    // nowa część miasta (16.11): lokale w parterach wieżowców, BRAXHORN BET, biuro dewelopera, plac ustawek, komis aut
    static readonly (string plik, Vector3 kam, Vector3 cel, string opis)[] Ujecia =
    {
        ("01_promenada_lokale", new Vector3(-452f, 2.2f, 12f), new Vector3(-425f, 3.0f, -3.5f), "promenada rzędu C: KAWIARNIA i BRAXHORN BET w parterze wieżowca C5_U07"),
        ("02_bet_z_zewnatrz", new Vector3(-401f, 1.75f, 5.5f), new Vector3(-405f, 3.6f, -3.5f), "BRAXHORN BET od promenady: witryny, drzwi, szyld"),
        ("03_bet_wnetrze_ekrany", new Vector3(-408.5f, 1.9f, -4.6f), new Vector3(-397f, 2.3f, -6.5f), "BRAXHORN BET w środku: ekrany, kontuar z hokerami, stoliki wysokie"),
        ("04_bet_wnetrze_kasa", new Vector3(-398f, 1.9f, -4.4f), new Vector3(-407.5f, 2.2f, -10f), "BRAXHORN BET w środku: kasa z okienkiem i tablica kursów"),
        ("05_deweloper_z_zewnatrz", new Vector3(-401.5f, 1.75f, 31.5f), new Vector3(-397.8f, 3.4f, 40.5f), "biuro dewelopera od Alei Centralnej"),
        ("06_deweloper_wnetrze", new Vector3(-395.5f, 1.9f, 41.2f), new Vector3(-401.5f, 1.5f, 45.5f), "biuro dewelopera w środku: biurka doradców, tablica oferty"),
        ("07_aleja_lokale_polnoc", new Vector3(-505f, 2.2f, 31f), new Vector3(-478f, 3.0f, 40.5f), "Aleja Centralna, strona północna: DELIKATESY w parterze C5_U08"),
        ("08_pizzeria_wnetrze", new Vector3(-465.5f, 1.9f, -4.5f), new Vector3(-474f, 1.2f, -9.5f), "PIZZERIA w środku (C5_U04)"),
        ("09_plac_ustawek_wjazd", new Vector3(-808f, 2.0f, 128.5f), new Vector3(-845f, 1.2f, 128f), "plac ustawek za ATLANTĄ od wjazdu"),
        ("10_plac_ustawek_z_gory", new Vector3(-823f, 9.0f, 111f), new Vector3(-850f, .5f, 136f), "plac ustawek z góry: płot betonowy, kontenery, bariery"),
        ("11_komis_z_chodnika", new Vector3(-698f, 1.75f, -68.8f), new Vector3(-714f, 1.0f, -77.5f), "komis aut z chodnika ul. Tramwajowej"),
        ("12_komis_od_tramwaju", new Vector3(-676f, 3.0f, -70f), new Vector3(-716f, 1.0f, -78f), "komis aut od przystanku T1 TRAMWAJOWA"),
    };

    public static int Wykonaj(Scene scena)
    {
        Directory.CreateDirectory(Katalog);
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Debug.LogWarning("BRAXHORN_ZDJECIA brak karty graficznej (urządzenie Null) — zdjęć z edytora nie ma");
            File.WriteAllText(Katalog + "/BRAK_ZDJEC.txt", "Unity bez karty graficznej (GraphicsDeviceType.Null) — kamera nie renderuje.\n");
            return 0;
        }
        var sw = Stopwatch.StartNew();
        int idE = Shader.PropertyToID("_BraxEkspozycja"), idM = Shader.PropertyToID("_BraxMgielkaGestosc");
        float eksPrzed = Shader.GetGlobalFloat(idE), mgPrzed = Shader.GetGlobalFloat(idM);
        var obraz = UnityEngine.Object.FindAnyObjectByType<BraxObrazRejonu>();
        float slPrzed = obraz && obraz.slonce ? obraz.slonce.intensity : -1f;
        if (obraz) { Shader.SetGlobalFloat(idE, obraz.ekspozycja); if (obraz.slonce) obraz.slonce.intensity = obraz.slonceZewnatrz; }
        Shader.SetGlobalFloat(idM, 0f);
        var go = new GameObject("BRAXHORN zdjęcia z edytora (kamera tymczasowa)");
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 55f; cam.nearClipPlane = .08f; cam.farClipPlane = 2500f; cam.clearFlags = CameraClearFlags.Skybox; cam.allowMSAA = true; cam.allowHDR = false;
        var rt = new RenderTexture(Szer, Wys, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var tex = new Texture2D(Szer, Wys, TextureFormat.RGB24, false);
        var zrobione = new List<string>(); var opisy = new List<string>();
        try
        {
            foreach (var u in Ujecia)
            {
                try
                {
                    cam.transform.SetPositionAndRotation(u.kam, Quaternion.LookRotation(u.cel - u.kam, Vector3.up));
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, Szer, Wys), 0, 0); tex.Apply(false);
                    RenderTexture.active = prev;
                    // pusta klatka (np. render bez efektu) — średnia jasność
                    var px = tex.GetPixels32(); double s = 0; for (int i = 0; i < px.Length; i += 97) s += px[i].r + px[i].g + px[i].b;
                    float jasnosc = (float)(s / (px.Length / 97 + 1) / 3.0 / 255.0);
                    File.WriteAllBytes(Katalog + "/" + u.plik + ".jpg", tex.EncodeToJPG(90));
                    zrobione.Add(u.plik + (jasnosc < .02f ? " (CZARNE)" : ""));
                    opisy.Add(u.plik + ".jpg — " + u.opis + " | kamera " + u.kam.ToString("F1") + " → " + u.cel.ToString("F1") + " | jasność " + jasnosc.ToString("F2"));
                }
                catch (Exception e) { Debug.LogWarning("BRAXHORN_ZDJECIA " + u.plik + " DO POPRAWKI: " + e.Message); }
            }
        }
        finally
        {
            cam.targetTexture = null; RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(go); rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
            Shader.SetGlobalFloat(idE, eksPrzed); Shader.SetGlobalFloat(idM, mgPrzed);
            if (obraz && obraz.slonce && slPrzed >= 0f) obraz.slonce.intensity = slPrzed;
        }
        File.WriteAllText(Katalog + "/opis.txt", "MIASTO R1 — zdjęcia z edytora (scena " + scena.name + ", przed podziałem na obszary, bez korekcji koloru gry)\n" + string.Join("\n", opisy) + "\n");
        Debug.Log("BRAXHORN_ZDJECIA z edytora: " + zrobione.Count + "/" + Ujecia.Length + " w " + (sw.ElapsedMilliseconds / 1000f).ToString("F0") + " s (karta: "
                  + SystemInfo.graphicsDeviceName + ", " + SystemInfo.graphicsDeviceType + ") → unity-city/" + Katalog + ": " + string.Join(", ", zrobione));
        return zrobione.Count;
    }
}

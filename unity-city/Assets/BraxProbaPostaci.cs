using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

// POKÓJ PRÓB POSTACI (właściciel 16.11, 08:47: „starczy zbudować zwykłe pomieszczenie, w którym
// będą testowane postacie”). Jedna postać po drugiej przechodzi ten sam zestaw: spoczynek, chód,
// bieg, rozmowa z głosem. Po drodze mierzymy mruganie i ruch ust — tak jak w próbie Patryka.
// Scenę stawia BraxPokojProbBuild, ten skrypt tylko prowadzi próbę i zapisuje klatki.
public sealed class BraxProbaPostaci : MonoBehaviour
{
    public Camera kamera;
    public Animation[] postacie;
    public string[] nazwy;
    public BraxMimikaOsiedla[] mimiki;
    public string kwestia = "9a79ffe04cb33bdb_m";
    public int klatekNaEtap = 36;          // 1,5 s przy 24 kl./s

    static readonly (string klip, string opis)[] Etapy =
    {
        ("Idle", "Spoczynek"),
        ("Walk", "Chód"),
        ("Run",  "Bieg"),
        ("Talk", "Rozmowa"),
        ("Idle", "Spoczynek"),
    };

    string katalog;
    int numer;
    GUIStyle styl;
    string podpis = "";

    bool BezKlatek => Environment.GetEnvironmentVariable("BRAX_BEZ_KLATEK") == "1";

    void Start()
    {
        katalog = Environment.GetEnvironmentVariable("BRAX_PROBA_KLATKI");
        if (string.IsNullOrEmpty(katalog)) katalog = Path.Combine(Application.persistentDataPath, "klatki");
        Directory.CreateDirectory(katalog);
        LekkaGrafika();
        Time.captureFramerate = 24;
        StartCoroutine(Prowadz());
    }


    // Pokój prób leci na serwerze bez karty graficznej (llvmpipe, render programowy).
    // BraxGrafika sama ustawia WYSOKĄ, bo widzi komputer — miękkie cienie, 4 kaskady i MSAA
    // dają wtedy klatki po kilka sekund i próba nie kończy się przed limitem (run 43831783370).
    // Próba sprawdza RUCH, nie urodę kadru, więc ścinamy wszystko, co kosztuje a nic nie wnosi.
    static void LekkaGrafika()
    {
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowCascades = 1;
        QualitySettings.shadowDistance = 18f;
        QualitySettings.antiAliasing = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
        QualitySettings.lodBias = 1f;
        QualitySettings.globalTextureMipmapLimit = 1;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Debug.Log("BRAX_POKOJ grafika proby: cienie twarde, bez MSAA, mip 1");
    }

    void OnGUI()
    {
        if (styl == null)
            styl = new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = Color.white } };
        GUI.Label(new Rect(18, 12, 900, 40), podpis, styl);
    }

    IEnumerator Prowadz()
    {
        var koniec = new WaitForEndOfFrame();
        var zrzut = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        var wynik = new System.Text.StringBuilder();
        bool wszystkoOk = true;

        for (int i = 0; i < postacie.Length; i++)
        {
            var anim = postacie[i];
            string nazwa = i < nazwy.Length ? nazwy[i] : anim.name;
            var mimika = i < mimiki.Length ? mimiki[i] : null;
            foreach (var p in postacie) p.gameObject.SetActive(false);
            anim.gameObject.SetActive(true);

            float mrugMax = 0f, szczekaMax = 0f, wargiMax = 0f;
            var blok = new MaterialPropertyBlock();
            int brakKlipow = 0;

            // GŁOWA MA PRACOWAĆ (właściciel 16.11, 09:21: „na nagraniu samej buzi unieruchomił głowę…
            // to będzie trzeba wyłapać w teście”). BraxSpojrzeniePatryka dokłada tylko korektę na
            // wierzchu — jeśli klip przychodzi z zamrożoną szyją i głową, postać poza rozmową stoi
            // jak posąg. Mierzymy zakres obrotu głowy WZGLĘDEM klatki piersiowej, żeby nie liczyć
            // obrotu całej postaci.
            Transform glowaK = null, piers = null;
            foreach (var t in anim.GetComponentsInChildren<Transform>(true))
            {
                if (!glowaK && t.name.EndsWith("Head", StringComparison.Ordinal)) glowaK = t;
                if (!piers && t.name.EndsWith("Spine2", StringComparison.Ordinal)) piers = t;
            }
            Quaternion? pierwsza = null;
            float glowaZakres = 0f;

            foreach (var (klip, opis) in Etapy)
            {
                if (!anim.GetClip(klip)) { brakKlipow++; continue; }
                anim[klip].wrapMode = WrapMode.Loop;
                anim.CrossFade(klip, .2f);
                if (klip == "Talk")
                {
                    var glos = Resources.Load<AudioClip>("OsiedleGlosy/" + kwestia);
                    if (glos)
                    {
                        var zrodlo = anim.GetComponent<BraxGlosPostaci>() ??
                                     anim.gameObject.AddComponent<BraxGlosPostaci>();
                        zrodlo.Mow(glos);
                    }
                }
                for (int k = 0; k < klatekNaEtap; k++)
                {
                    podpis = $"{nazwa}  ·  {opis}";
                    yield return koniec;
                    if (mimika && mimika.twarz)
                    {
                        mimika.twarz.GetPropertyBlock(blok);
                        mrugMax = Mathf.Max(mrugMax, blok.GetFloat("_Blink"));
                        if (mimika.szczeka >= 0)
                            szczekaMax = Mathf.Max(szczekaMax, mimika.twarz.GetBlendShapeWeight(mimika.szczeka));
                        if (mimika.wargi >= 0)
                            wargiMax = Mathf.Max(wargiMax, mimika.twarz.GetBlendShapeWeight(mimika.wargi));
                    }
                    if (glowaK && piers)
                    {
                        var wzgledny = Quaternion.Inverse(piers.rotation) * glowaK.rotation;
                        if (pierwsza.HasValue)
                            glowaZakres = Mathf.Max(glowaZakres, Quaternion.Angle(pierwsza.Value, wzgledny));
                        else pierwsza = wzgledny;
                    }
                    // BRAX_BEZ_KLATEK=1 — tylko liczby. Serwer CI nie ma karty graficznej
                    // („Renderer: llvmpipe”), a odczyt bufora i JPEG na każdą klatkę kosztuje tam
                    // tyle, że próba nie dochodzi do końca. Film bierzemy z APK, gdzie jest GPU.
                    if (!BezKlatek)
                    {
                        zrzut.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                        zrzut.Apply(false);
                        File.WriteAllBytes(Path.Combine(katalog, numer.ToString("D5") + ".jpg"),
                                           zrzut.EncodeToJPG(88));
                        numer++;
                    }
                }
            }

            // Próg 4°: tyle głowa rusza się u człowieka nawet w spokojnym staniu.
            // Poniżej tego klip ma wyciszoną ścieżkę szyi i głowy — postać będzie posągiem.
            bool glowaPracuje = glowaZakres >= 4f;
            bool ok = brakKlipow == 0 && mrugMax >= .60f && szczekaMax >= 20f && glowaPracuje;
            wszystkoOk &= ok;
            string linia = string.Format(CultureInfo.InvariantCulture,
                "BRAX_POSTAC {0} brak_klipow={1} mruganie={2:F2} szczeka={3:F0} wargi={4:F0} " +
                "glowa={5:F1} stopni {6} {7}",
                nazwa, brakKlipow, mrugMax, szczekaMax, wargiMax, glowaZakres,
                glowaPracuje ? "pracuje" : "ZAMROZONA", ok ? "PASS" : "FAIL");
            Debug.Log(linia);
            wynik.AppendLine(linia);
        }

        wynik.AppendLine(wszystkoOk ? "WYNIK PASS" : "WYNIK FAIL");
        File.WriteAllText(Path.Combine(katalog, "wynik.txt"), wynik.ToString());
        Debug.Log("BRAX_PROBA_POSTACI_KONIEC klatek=" + numer);
        Time.captureFramerate = 0;
        Application.Quit(wszystkoOk ? 0 : 1);
    }
}

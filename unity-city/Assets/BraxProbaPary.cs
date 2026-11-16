using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

// POKÓJ PRÓB — POSTACIE RAZEM (właściciel 16.11, 09:02: „możesz testować postacie, jak ze sobą
// rozmawiają, czy patrzą na siebie, gesty jakie mają… tak samo jak się biją też, czy dochodzą ciosy”).
//
// Dwa tryby, obydwa mierzone liczbami, nie na oko:
//  • ROZMOWA — dwie lub trzy postacie naprzeciw siebie. Mierzymy kąt, pod jakim głowa mówiącego
//    jest skierowana na rozmówcę (0° = patrzy prosto w niego), ruch ust i domknięcie powiek.
//  • WALKA — dwie postacie. Dla każdego ciosu mierzymy NAJMNIEJSZĄ odległość pięści albo stopy
//    od tułowia i głowy przeciwnika. Jeśli nigdy nie zbliży się na dystans trafienia — cios nie dochodzi.
public sealed class BraxProbaPary : MonoBehaviour
{
    public enum Tryb { Rozmowa, Walka }

    public Tryb tryb = Tryb.Rozmowa;
    public Animation[] postacie;
    public string[] nazwy;
    public BraxMimikaOsiedla[] mimiki;
    public BraxSpojrzeniePatryka[] spojrzenia;
    public string[] kwestie = { "9a79ffe04cb33bdb_m" };
    public int klatekNaEtap = 48;

    // Zasięg trafienia: pięść bliżej niż 25 cm × K od tułowia to kontakt (rękawice, odzież).
    public const float ZasiegTrafienia = .25f * (2.33f / 1.80f);
    static readonly string[] Ciosy = { "Jab", "Cross", "Hook", "Knee", "SideKick" };
    static readonly string[] Pieści = { "RightHand", "LeftHand" };
    static readonly string[] Nogi = { "RightFoot", "LeftFoot" };
    static readonly string[] Cele = { "Spine2", "Spine1", "Head" };

    string katalog;
    int numer;
    GUIStyle styl;
    string podpis = "";

    static Transform Kosc(Transform korzen, string nazwa) =>
        korzen.GetComponentsInChildren<Transform>(true)
              .FirstOrDefault(t => t.name.EndsWith(nazwa, StringComparison.Ordinal));

    void Start()
    {
        katalog = Environment.GetEnvironmentVariable("BRAX_PROBA_KLATKI");
        if (string.IsNullOrEmpty(katalog)) katalog = Path.Combine(Application.persistentDataPath, "klatki");
        Directory.CreateDirectory(katalog);
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowCascades = 1;
        QualitySettings.shadowDistance = 18f;
        QualitySettings.antiAliasing = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.globalTextureMipmapLimit = 1;
        QualitySettings.vSyncCount = 0;
        Time.captureFramerate = 24;
        StartCoroutine(tryb == Tryb.Rozmowa ? Rozmowa() : Walka());
    }

    void OnGUI()
    {
        if (styl == null)
            styl = new GUIStyle(GUI.skin.label) { fontSize = 21, normal = { textColor = Color.white } };
        GUI.Label(new Rect(18, 12, 1000, 40), podpis, styl);
    }

    IEnumerator Klatki(int ile, Action naKlatce)
    {
        var koniec = new WaitForEndOfFrame();
        var zrzut = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        for (int i = 0; i < ile; i++)
        {
            yield return koniec;
            naKlatce?.Invoke();
            zrzut.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            zrzut.Apply(false);
            File.WriteAllBytes(Path.Combine(katalog, numer.ToString("D5") + ".jpg"), zrzut.EncodeToJPG(88));
            numer++;
        }
        Destroy(zrzut);
    }

    void Graj(Animation a, string klip)
    {
        if (!a || !a.GetClip(klip)) return;
        a[klip].wrapMode = WrapMode.Loop;
        a.CrossFade(klip, .18f);
    }

    // ---------------------------------------------------------------- ROZMOWA

    IEnumerator Rozmowa()
    {
        var wynik = new System.Text.StringBuilder();
        bool ok = true;
        foreach (var a in postacie) Graj(a, "Idle");
        yield return Klatki(klatekNaEtap / 2, null);

        for (int i = 0; i < postacie.Length; i++)
        {
            int j = (i + 1) % postacie.Length;
            var mowi = postacie[i];
            var sluch = postacie[j];
            var glowaMowi = Kosc(mowi.transform, "Head");
            var glowaSluch = Kosc(sluch.transform, "Head");
            var mim = i < mimiki.Length ? mimiki[i] : null;

            // Wzajemne spojrzenie — ta sama droga co w grze.
            var para = BraxSpojrzeniePatryka.Rozmowa(
                i < spojrzenia.Length ? spojrzenia[i] : null,
                j < spojrzenia.Length ? spojrzenia[j] : null,
                Kwestia(mowi, sluch, i));
            StartCoroutine(para);

            float katMin = 999f, ustaMax = 0f, mrugMax = 0f;
            var blok = new MaterialPropertyBlock();
            yield return Klatki(klatekNaEtap * 2, () =>
            {
                podpis = $"ROZMOWA  ·  {nazwy[i]} → {nazwy[j]}";
                if (glowaMowi && glowaSluch)
                {
                    var doRozmowcy = glowaSluch.position - glowaMowi.position;
                    doRozmowcy.y = 0f;
                    var przod = mowi.transform.forward; przod.y = 0f;
                    if (doRozmowcy.sqrMagnitude > .0001f && przod.sqrMagnitude > .0001f)
                        katMin = Mathf.Min(katMin, Vector3.Angle(przod, doRozmowcy));
                }
                if (mim && mim.twarz)
                {
                    if (mim.szczeka >= 0)
                        ustaMax = Mathf.Max(ustaMax, mim.twarz.GetBlendShapeWeight(mim.szczeka));
                    mim.twarz.GetPropertyBlock(blok);
                    mrugMax = Mathf.Max(mrugMax, blok.GetFloat("_Blink"));
                }
            });

            bool patrzy = katMin <= 35f;
            bool gada = ustaMax >= 20f;
            bool mruga = mrugMax >= .60f;
            bool zdana = patrzy && gada;
            ok &= zdana;
            string linia = string.Format(CultureInfo.InvariantCulture,
                "BRAX_ROZMOWA {0}->{1} kat_na_rozmowce={2:F0} usta={3:F0} mruganie={4:F2} " +
                "patrzy={5} mowi={6} mruga={7} {8}",
                nazwy[i], nazwy[j], katMin, ustaMax, mrugMax,
                patrzy ? "tak" : "NIE", gada ? "tak" : "NIE", mruga ? "tak" : "NIE",
                zdana ? "PASS" : "FAIL");
            Debug.Log(linia);
            wynik.AppendLine(linia);
            foreach (var a in postacie) Graj(a, "Idle");
            yield return Klatki(klatekNaEtap / 3, null);
        }
        Zakoncz(wynik, ok);
    }

    IEnumerator Kwestia(Animation mowi, Animation sluch, int i)
    {
        Graj(mowi, "Talk");
        Graj(sluch, "Idle");
        var glos = Resources.Load<AudioClip>("OsiedleGlosy/" + kwestie[i % kwestie.Length]);
        if (glos)
        {
            var zr = mowi.GetComponent<BraxGlosPostaci>() ?? mowi.gameObject.AddComponent<BraxGlosPostaci>();
            zr.Mow(glos);
            yield return new WaitForSeconds(Mathf.Min(glos.length, 4f));
        }
        else yield return new WaitForSeconds(2f);
    }

    // ------------------------------------------------------------------ WALKA

    IEnumerator Walka()
    {
        var wynik = new System.Text.StringBuilder();
        bool ok = true;
        if (postacie.Length < 2)
        {
            wynik.AppendLine("BRAX_WALKA FAIL — do próby walki trzeba dwóch postaci");
            Zakoncz(wynik, false);
            yield break;
        }
        var bijacy = postacie[0];
        var cel = postacie[1];
        var celeKosci = Cele.Select(n => Kosc(cel.transform, n)).Where(t => t).ToArray();

        Graj(bijacy, "CombatIdle");
        Graj(cel, "CombatIdle");
        yield return Klatki(klatekNaEtap / 2, null);

        foreach (string cios in Ciosy)
        {
            if (!bijacy.GetClip(cios))
            {
                string b = $"BRAX_CIOS {nazwy[0]} {cios} BRAK_KLIPU FAIL";
                Debug.Log(b); wynik.AppendLine(b); ok = false;
                continue;
            }
            var konczyny = (cios == "Knee" || cios == "SideKick" ? Nogi : Pieści)
                .Select(n => Kosc(bijacy.transform, n)).Where(t => t).ToArray();
            bijacy[cios].wrapMode = WrapMode.Loop;
            bijacy.CrossFade(cios, .12f);
            float naj = 999f;
            yield return Klatki(klatekNaEtap, () =>
            {
                podpis = $"WALKA  ·  {nazwy[0]} → {nazwy[1]}  ·  {cios}";
                foreach (var k in konczyny)
                    foreach (var c in celeKosci)
                        naj = Mathf.Min(naj, Vector3.Distance(k.position, c.position));
            });
            bool dochodzi = naj <= ZasiegTrafienia;
            ok &= dochodzi;
            string linia = string.Format(CultureInfo.InvariantCulture,
                "BRAX_CIOS {0}->{1} {2} najblizej={3:F3} m zasieg={4:F3} m {5}",
                nazwy[0], nazwy[1], cios, naj, ZasiegTrafienia, dochodzi ? "DOCHODZI PASS" : "NIE DOCHODZI FAIL");
            Debug.Log(linia);
            wynik.AppendLine(linia);
            Graj(bijacy, "CombatIdle");
            yield return Klatki(klatekNaEtap / 4, null);
        }
        Zakoncz(wynik, ok);
    }

    void Zakoncz(System.Text.StringBuilder wynik, bool ok)
    {
        wynik.AppendLine(ok ? "WYNIK PASS" : "WYNIK FAIL");
        File.WriteAllText(Path.Combine(katalog, "wynik.txt"), wynik.ToString());
        Debug.Log("BRAX_PROBA_PARY_KONIEC klatek=" + numer);
        Time.captureFramerate = 0;
        Application.Quit(ok ? 0 : 1);
    }
}

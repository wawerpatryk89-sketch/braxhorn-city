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
//  • STRES (właściciel 16.11, 12:50: „pokój do testów ze stresowaniem postacią i reakcjami innych”) —
//    PIERWSZA postać dostaje łomot: wszystkie swoje ujęcia pod rząd, przeplatane w 0,05 s, w trzech
//    prędkościach (×1, ×1.6, ×2.2). Pozostałe stoją obok jako ŚWIADKOWIE i muszą na nią reagować.
//    Mierzone na stresowanej: największa zmiana DŁUGOŚCI KOŚCI (szkielet się nie rozciąga od samego
//    animowania — jeśli rośnie, rig pęka; paczki postaci same zgłaszają „stretch warnings” np. dla
//    Jump i KickBall), czy mimika żyje pod obciążeniem (szczęka, powieki) i czy głowa nie zamarza
//    względem Spine2. Mierzone na świadkach: najmniejszy kąt głowy na stresowaną (czy w ogóle na nią
//    patrzą) i czy ich własne ujęcie dalej idzie, a nie stoi zamrożone w trakcie patrzenia.
public sealed class BraxProbaPary : MonoBehaviour
{
    public enum Tryb { Rozmowa, Walka, Stres }

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
        StartCoroutine(tryb == Tryb.Rozmowa ? Rozmowa() : tryb == Tryb.Walka ? Walka() : Stres());
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

    // ------------------------------------------------------------------ STRES

    static readonly float[] Predkosci = { 1f, 1.6f, 2.2f };

    struct Ogniwo { public Transform dziecko; public float spoczynek; }

    // Długości kości w szkielecie są stałe — animacja tylko OBRACA stawy. Każda zmiana długości
    // znaczy, że coś skaluje albo rozciąga siatkę, a tego nie widać na oko przy szybkim ruchu.
    static Ogniwo[] Ogniwa(Transform korzen)
    {
        var biodra = Kosc(korzen, "Hips");
        if (!biodra) return Array.Empty<Ogniwo>();
        return biodra.GetComponentsInChildren<Transform>(true)
                     .Where(t => t != biodra && t.parent)
                     .Select(t => new Ogniwo { dziecko = t, spoczynek = t.localPosition.magnitude })
                     .Where(o => o.spoczynek > .003f)
                     .ToArray();
    }

    static float NajwiekszeRozciagniecie(Ogniwo[] ogniwa)
    {
        float max = 0f;
        foreach (var o in ogniwa)
            if (o.dziecko)
                max = Mathf.Max(max, Mathf.Abs(o.dziecko.localPosition.magnitude - o.spoczynek) / o.spoczynek);
        return max;
    }

    static float KatNa(Animation kto, Transform cel)
    {
        if (!kto || !cel) return 999f;
        var glowa = Kosc(kto.transform, "Head");
        if (!glowa) return 999f;
        var doCelu = cel.position - glowa.position; doCelu.y = 0f;
        var przod = kto.transform.forward; przod.y = 0f;
        if (doCelu.sqrMagnitude < .0001f || przod.sqrMagnitude < .0001f) return 999f;
        return Vector3.Angle(przod, doCelu);
    }

    IEnumerator Stres()
    {
        var wynik = new System.Text.StringBuilder();
        bool ok = true;
        if (postacie.Length < 1 || !postacie[0])
        {
            wynik.AppendLine("BRAX_STRES FAIL — brak postaci do stresowania");
            Zakoncz(wynik, false);
            yield break;
        }

        var ofiara = postacie[0];
        string imie = nazwy.Length > 0 ? nazwy[0] : ofiara.name;
        var mim = mimiki.Length > 0 ? mimiki[0] : null;
        var glowa = Kosc(ofiara.transform, "Head");
        var tulow = Kosc(ofiara.transform, "Spine2");
        var cel = tulow ? tulow : ofiara.transform;

        foreach (var a in postacie) Graj(a, "Idle");
        yield return Klatki(klatekNaEtap / 2, null);
        var ogniwa = Ogniwa(ofiara.transform);
        if (ogniwa.Length == 0)
        {
            wynik.AppendLine("BRAX_STRES " + imie + " FAIL — nie znaleziono szkieletu (brak Hips)");
            Zakoncz(wynik, false);
            yield break;
        }

        var ujecia = new System.Collections.Generic.List<string>();
        foreach (AnimationState st in ofiara)
            if (st.clip && st.name != "Idle") ujecia.Add(st.name);
        ujecia.Sort(StringComparer.Ordinal);
        if (ujecia.Count == 0) ujecia.Add("Idle");

        // świadkowie: patrzą na stresowaną tą samą drogą co w grze
        for (int w = 1; w < postacie.Length; w++)
        {
            var wzrok = w < spojrzenia.Length ? spojrzenia[w] : null;
            if (wzrok) wzrok.postac = cel;
        }

        float rozciagMax = 0f, szczekaMax = 0f, mrugMax = 0f, katGlowyMin = 999f, katGlowyMax = -999f;
        string najgorsze = "-";
        var swiadekKat = new float[Mathf.Max(0, postacie.Length - 1)];
        var swiadekPostep = new float[swiadekKat.Length];
        for (int w = 0; w < swiadekKat.Length; w++) swiadekKat[w] = 999f;
        var czasStartu = new float[swiadekKat.Length];
        for (int w = 0; w < swiadekKat.Length; w++)
        {
            var a = postacie[w + 1];
            czasStartu[w] = a && a.clip ? a[a.clip.name].time : 0f;
        }
        var blok = new MaterialPropertyBlock();

        foreach (float predkosc in Predkosci)
        {
            foreach (string ujecie in ujecia)
            {
                if (!ofiara.GetClip(ujecie)) continue;
                ofiara[ujecie].wrapMode = WrapMode.Loop;
                ofiara[ujecie].speed = predkosc;
                ofiara.CrossFade(ujecie, .05f);
                float rozciagPrzed = rozciagMax;
                yield return Klatki(6, () =>
                {
                    podpis = string.Format(CultureInfo.InvariantCulture,
                        "STRES  ·  {0}  ·  {1}  ·  x{2:F1}  ·  świadków {3}",
                        imie, ujecie, predkosc, swiadekKat.Length);
                    rozciagMax = Mathf.Max(rozciagMax, NajwiekszeRozciagniecie(ogniwa));
                    if (mim && mim.twarz)
                    {
                        if (mim.szczeka >= 0)
                            szczekaMax = Mathf.Max(szczekaMax, mim.twarz.GetBlendShapeWeight(mim.szczeka));
                        mim.twarz.GetPropertyBlock(blok);
                        mrugMax = Mathf.Max(mrugMax, blok.GetFloat("_Blink"));
                    }
                    if (glowa && tulow)
                    {
                        float kat = Quaternion.Angle(tulow.rotation, glowa.rotation);
                        katGlowyMin = Mathf.Min(katGlowyMin, kat);
                        katGlowyMax = Mathf.Max(katGlowyMax, kat);
                    }
                    for (int w = 0; w < swiadekKat.Length; w++)
                        swiadekKat[w] = Mathf.Min(swiadekKat[w], KatNa(postacie[w + 1], cel));
                });
                if (rozciagMax > rozciagPrzed + .001f)
                    najgorsze = ujecie + " x" + predkosc.ToString("F1", CultureInfo.InvariantCulture);
            }
        }

        for (int w = 0; w < swiadekKat.Length; w++)
        {
            var a = postacie[w + 1];
            swiadekPostep[w] = a && a.clip ? Mathf.Abs(a[a.clip.name].time - czasStartu[w]) : 0f;
        }

        float zakresGlowy = katGlowyMax - katGlowyMin;
        bool rigCaly = rozciagMax <= .02f;              // 2 % na długości kości
        bool mimikaZyje = szczekaMax >= 20f && mrugMax >= .60f;
        bool glowaPracuje = zakresGlowy >= 4f;
        ok &= rigCaly && mimikaZyje && glowaPracuje;
        string linia = string.Format(CultureInfo.InvariantCulture,
            "BRAX_STRES {0} ujec={1} predkosci={2} rozciagniecie_kosci={3:P2} (najgorsze: {4}) " +
            "szczeka={5:F0} mruganie={6:F2} zakres_glowy={7:F1} st. rig={8} mimika={9} glowa={10} {11}",
            imie, ujecia.Count, Predkosci.Length, rozciagMax, najgorsze,
            szczekaMax, mrugMax, zakresGlowy,
            rigCaly ? "caly" : "PEKA", mimikaZyje ? "zyje" : "ZAMARLA",
            glowaPracuje ? "pracuje" : "ZAMROZONA", ok ? "PASS" : "FAIL");
        Debug.Log(linia);
        wynik.AppendLine(linia);

        for (int w = 0; w < swiadekKat.Length; w++)
        {
            string imieS = w + 1 < nazwy.Length ? nazwy[w + 1] : postacie[w + 1].name;
            bool patrzy = swiadekKat[w] <= 45f;
            bool zyje = swiadekPostep[w] > .05f;
            bool zdany = patrzy && zyje;
            ok &= zdany;
            string ls = string.Format(CultureInfo.InvariantCulture,
                "BRAX_SWIADEK {0} kat_na_{1}={2:F0} st. wlasne_ujecie_poszlo={3:F2} s patrzy={4} zyje={5} {6}",
                imieS, imie, swiadekKat[w], swiadekPostep[w],
                patrzy ? "tak" : "NIE", zyje ? "tak" : "NIE", zdany ? "PASS" : "FAIL");
            Debug.Log(ls);
            wynik.AppendLine(ls);
        }

        foreach (var a in postacie) Graj(a, "Idle");
        yield return Klatki(klatekNaEtap / 3, null);
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

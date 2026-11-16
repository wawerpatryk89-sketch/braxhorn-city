using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

// POKAZ W APK (właściciel 16.11, 10:34: „APK, które ci nagra film, jak się rusza postać”).
// Telefon ma kartę graficzną, serwer CI nie — więc obraz powstaje TUTAJ, na urządzeniu.
// Każda postać po kolei wychodzi na środek i robi: spoczynek, chód, bieg, rozmowę.
// Kamera trzyma ją w kadrze. Przycisk NAGRYWAJ zapisuje klatki do pamięci telefonu,
// a kto woli — po prostu włącza nagrywanie ekranu i ma gotowy film.
// KOLEJNOSC 20000 — POKAZ USTAWIA KAMERE JAKO OSTATNI.
// Nagranie wlasciciela z 16.11, 14:48: przez caly pokaz widac bylo Patryka, mimo ze podpis mowil
// „bramkarz” albo „kapitan”. Powod: BraxGymPlayer tez ustawia kamere w LateUpdate (trzecia osoba,
// za graczem), a pokaz robil to wczesniej — wiec kamera wracala za gracza, czyli za Patryka,
// a prezentowana postac stala poza kadrem. Teraz pokaz idzie po nim i wygrywa.
[DefaultExecutionOrder(20000)]
public sealed class BraxPokazPostaci : MonoBehaviour
{
    public Animation[] postacie;
    public string[] nazwy;
    public Transform srodek;
    public Camera kamera;
    public float naEtap = 3f;
    public const float ObrotSekund = 6f;        // pełne 360 stopni, spokojnie, żeby dało się patrzeć
    public const string Kwestia = "9a79ffe04cb33bdb_m";
    public const float SKALA = 2.33f / 1.80f;

    static readonly (string klip, string opis)[] Etapy =
    {
        ("Idle", "SPOCZYNEK"), ("Idle", "OBRÓT — każdy profil"),
        ("Walk", "CHÓD"), ("Run", "BIEG"), ("Talk", "ROZMOWA"),
    };

    // Te same cztery warianty co w próbie połysku — żeby numer, który właściciel poda,
    // znaczył dokładnie to samo w obu miejscach.
    public static readonly (string nazwa, float gl, float me, float nr)[] Ustawienia =
    {
        ("1 jak w mapie", 1.00f, 1f, 1.0f),
        ("2 matowy",      0.55f, 0f, 1.0f),
        ("3 mat+faktura", 0.45f, 0f, 1.6f),
        ("4 pełny mat",   0.33f, 0f, 2.0f),
        ("5 maksymalny",  0.22f, 0f, 2.6f),
    };
    public int wariant = 2;      // pokój startuje na wariancie 3

    void ZastosujMaterial()
    {
        var u = Ustawienia[wariant];
        foreach (var a in postacie)
        {
            if (!a) continue;
            foreach (var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var m in r.materials)
                    if (m)
                    {
                        m.SetFloat("_Gladkosc", u.gl);
                        m.SetFloat("_MetalMnoznik", u.me);
                        m.SetFloat("_SilaNormalnych", u.nr);
                    }
        }
        Debug.Log($"BRAX_POKAZ_MATERIAL {u.nazwa} gl={u.gl:F2} metal={u.me:F0} norm={u.nr:F1}");
    }

    int kto = -1;
    string etap = "";
    bool nagrywa;
    int klatka;
    string katalog;
    GUIStyle duzy, maly;

    void Start()
    {
        katalog = Path.Combine(Application.persistentDataPath, "pokaz");
        foreach (var a in postacie) if (a) a.gameObject.SetActive(false);
        StartCoroutine(Pokaz());
    }

    IEnumerator Pokaz()
    {
        while (true)
        {
            for (int i = 0; i < postacie.Length; i++)
            {
                var a = postacie[i];
                if (!a) continue;
                kto = i;
                foreach (var b in postacie) if (b) b.gameObject.SetActive(b == a);
                var korzen = a.transform.parent;
                var staraPoz = korzen.localPosition;
                var staryObrot = korzen.localRotation;
                korzen.localPosition = srodek ? srodek.localPosition : Vector3.zero;
                korzen.localRotation = Quaternion.Euler(0, 180f, 0);

                int nrEtapu = -1;
                foreach (var (klip, opis) in Etapy)
                {
                    nrEtapu++;
                    etap = opis;
                    bool obrot = opis.StartsWith("OBRÓT");
                    bool rozmowa = opis == "ROZMOWA";
                    if (a.GetClip(klip)) { a[klip].wrapMode = WrapMode.Loop; a.CrossFade(klip, .2f); }
                    else
                    {
                        // Nie samo „BRAK KLIPU” — od razu widac, co ta postac ma, zeby nie trzeba
                        // bylo tego dochodzic osobno.
                        var ma = new System.Collections.Generic.List<string>();
                        foreach (AnimationState st in a) if (st.clip != null) ma.Add(st.name);
                        ma.Sort(System.StringComparer.Ordinal);
                        // Na telefonie w pionie dluga linia ucina sie na krawedzi (widac na
                        // nagraniu wlasciciela: „BRAK KLIPU Run (ma:” i koniec). Krotko i konkretnie.
                        etap = opis + " — BRAK " + klip + " (" + ma.Count + " ujec: " +
                               string.Join(",", ma.GetRange(0, System.Math.Min(4, ma.Count))) +
                               (ma.Count > 4 ? "…" : "") + ")";
                    }
                    // OBRÓT: pełne 360 stopni, żeby było widać każdy profil — tył, bok, przód
                    // (właściciel 16.11, 20:34: „chcę widzieć jak się obracają, każdy profil”).
                    // GŁOS: w rozmowie leci nagranie, a mimika jedzie z jego ścieżki, więc usta
                    // chodzą do tego, co słychać.
                    AudioSource zrodlo = null;
                    if (rozmowa)
                    {
                        var mim = a.GetComponentInChildren<BraxMimikaOsiedla>();
                        zrodlo = a.GetComponent<AudioSource>();
                        if (!zrodlo) zrodlo = a.gameObject.AddComponent<AudioSource>();
                        zrodlo.spatialBlend = 0f;
                        zrodlo.playOnAwake = false;
                        if (!zrodlo.clip) zrodlo.clip = Resources.Load<AudioClip>("OsiedleGlosy/" + Kwestia);
                        if (zrodlo.clip)
                        {
                            zrodlo.Play();
                            if (mim) mim.Wypowiedz(zrodlo);
                            etap = opis + "  ♪ " + zrodlo.clip.name;
                        }
                        else etap = opis + " — brak nagrania głosu";
                    }

                    float dlugosc = obrot ? ObrotSekund
                                  : (rozmowa && zrodlo && zrodlo.clip
                                        ? Mathf.Max(naEtap, zrodlo.clip.length + .4f) : naEtap);
                    float start = Time.time, do_ = start + dlugosc;
                    while (Time.time < do_)
                    {
                        if (obrot)
                        {
                            float post = (Time.time - start) / ObrotSekund;
                            korzen.localRotation = Quaternion.Euler(0, 180f + post * 360f, 0);
                        }
                        yield return null;
                    }
                    if (obrot) korzen.localRotation = Quaternion.Euler(0, 180f, 0);
                    if (zrodlo && zrodlo.isPlaying) zrodlo.Stop();
                }
                korzen.localPosition = staraPoz;
                korzen.localRotation = staryObrot;
            }
        }
    }

    void LateUpdate()
    {
        if (kto < 0 || kto >= postacie.Length || !postacie[kto] || !kamera) return;
        // Kamera stoi przed postacią na wysokości piersi i trzyma ją w kadrze przez cały pokaz.
        var stopy = postacie[kto].transform.position;
        var cel = stopy + Vector3.up * (1.15f * SKALA);
        kamera.transform.position = stopy + new Vector3(0f, 1.35f * SKALA, -3.1f * SKALA);
        kamera.transform.LookAt(cel);
        if (nagrywa) klatka++;
    }

    void OnGUI()
    {
        if (duzy == null)
        {
            duzy = new GUIStyle(GUI.skin.label) { fontSize = 30, normal = { textColor = Color.white } };
            maly = new GUIStyle(GUI.skin.button) { fontSize = 26 };
        }
        string kim = kto >= 0 && kto < nazwy.Length ? nazwy[kto] : "";
        GUI.Label(new Rect(20, 18, Screen.width - 40, 42), kim, duzy);
        GUI.Label(new Rect(20, 62, Screen.width - 40, 42), etap, duzy);

        // PODKRĘCANIE NA ŻYWO (właściciel 16.11, 20:35: „czy możemy to jeszcze podkręcić?”).
        // Przycisk przestawia materiał wszystkim postaciom naraz, bez budowania nowego APK.
        // Właściciel wybiera na oko i podaje numer — wtedy wpisuję go na stałe w ModelObsady.
        var przyciskStyl = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(Screen.height * .022f) };
        if (GUI.Button(new Rect(Screen.width - 330, Screen.height - 150, 300, 90),
                       "MATERIAŁ: " + Ustawienia[wariant].nazwa, przyciskStyl))
        {
            wariant = (wariant + 1) % Ustawienia.Length;
            ZastosujMaterial();
        }
        GUI.Label(new Rect(20, Screen.height - 50, Screen.width - 40, 36),
                  $"postać {kto + 1}/{postacie.Length}", duzy);

        if (GUI.Button(new Rect(Screen.width - 250, 18, 230, 62),
                       nagrywa ? "ZATRZYMAJ" : "NAGRYWAJ", maly))
        {
            nagrywa = !nagrywa;
            if (nagrywa) { Directory.CreateDirectory(katalog); klatka = 0; }
            else Debug.Log("BRAX_POKAZ klatek=" + klatka + " w " + katalog);
        }
        if (nagrywa)
            GUI.Label(new Rect(Screen.width - 250, 86, 230, 36),
                      string.Format(CultureInfo.InvariantCulture, "klatek: {0}", klatka), duzy);
    }
}

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
    public const float ObrotSekund = 6f;
    public const float SpoczynekSekund = 10f;   // spoczynek też się obraca, tylko wolniej        // pełne 360 stopni, spokojnie, żeby dało się patrzeć
    public const string Kwestia = "9a79ffe04cb33bdb_m";
    public const int NagranPodRzad = 6;     // więcej tekstu — jedno nagranie to za mało, żeby ocenić usta
    public bool[] kobieta = new bool[0];    // nagrania mają w nazwie _f i _m — głos ma pasować do postaci
    public bool zblizenie;                  // ROZMOWA: kamera na twarz, inaczej mimiki nie widać
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

    // WYBÓR POZY SPOCZYNKOWEJ (właściciel 16.11, 20:41: „zrób pozę Patryka”).
    // Patryk ma w paczce DZIESIĘĆ ujęć spoczynkowych (Idle, CarIdle, CombatIdle, SeatedIdle…),
    // a zmierzone kąty ramion pokazują, że nie odstaje od reszty obsady — więc nie zgaduję,
    // który spoczynek jest właściwy, tylko daję je do przejrzenia. Przycisk przełącza spoczynek
    // pokazywanej postaci; nazwa ujęcia jest na ekranie, żeby dało się ją podać.
    string[] pozy = new string[0];
    int poza;
    public const string PozaZPostawa = "Idle + postawa";

    // DOŁOŻONA POSTAWA (właściciel 16.11, 21:06: Patryk „chujowo stoi, jak taka łajza”).
    // Zmierzone najpierw, żeby nie poprawiać czegoś, co nie jest zepsute: jego „Idle” ma
    // najprostszy kręgosłup ze wszystkich dziesięciu jego spoczynków (pochylenie 0,12 stopnia
    // wobec 10,55 u najgorszego), a kąty ramion są w tym samym zakresie co u kapitana i Ewy.
    // Nic nie jest zepsute — poza jest po prostu IDEALNIE SYMETRYCZNA: stopy razem, ręce prosto
    // wzdłuż ciała, zero przeniesienia ciężaru. Tak stoi manekin, nie człowiek.
    // To jest dołożone NA WIERZCH nagrania, nie zamiast niego: ramiona odchylone od tułowia,
    // łokcie lekko zgięte, ciężar na jednej nodze, biodro przesunięte. Działa tylko w pokoju
    // prób i tylko gdy właściciel wybierze tę pozycję przyciskiem — do gry nic nie wchodzi,
    // dopóki nie powie, że ma wejść.
    static readonly (string kosc, Vector3 kat)[] Postawa =
    {
        ("LeftArm",      new Vector3(0f, 0f,  7f)),
        ("RightArm",     new Vector3(0f, 0f, -7f)),
        ("LeftForeArm",  new Vector3(0f, 12f, 0f)),
        ("RightForeArm", new Vector3(0f,-12f, 0f)),
        ("Spine",        new Vector3(0f, 0f,  2.5f)),
        ("LeftUpLeg",    new Vector3(0f, 0f, -2.5f)),
        ("RightUpLeg",   new Vector3(4f, 0f,  1.5f)),
        ("Hips",         new Vector3(0f, 0f, -1.5f)),
    };

    void DolozPostawe(Animation a)
    {
        var korzenK = a.transform.Find("mixamorig:Hips");
        if (!korzenK) return;
        foreach (var (nazwaK, kat) in Postawa)
        {
            Transform b = null;
            foreach (var tr in korzenK.GetComponentsInChildren<Transform>(true))
                if (tr.name.EndsWith(nazwaK, System.StringComparison.Ordinal)) { b = tr; break; }
            if (b) b.localRotation *= Quaternion.Euler(kat);
        }
    }

    void ZbierzPozy(Animation a)
    {
        var lista = new System.Collections.Generic.List<string>();
        foreach (AnimationState st in a)
            if (st.clip && st.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                lista.Add(st.name);
        lista.Sort(System.StringComparer.Ordinal);
        int i = lista.IndexOf("Idle");
        if (i > 0) { lista.RemoveAt(i); lista.Insert(0, "Idle"); }   // własny spoczynek pierwszy
        lista.Add(PozaZPostawa);   // ostatnia pozycja: własny Idle + dołożona postawa
        pozy = lista.ToArray();
        poza = 0;
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

                ZbierzPozy(a);
                // W próbie mrugnięcie ma być widać kilka razy, a nie raz na ujęcie.
                foreach (var m in a.GetComponentsInChildren<BraxMimikaOsiedla>(true))
                    m.odstepMrugania = .4f;
                int nrEtapu = -1;
                foreach (var (klip, opis) in Etapy)
                {
                    nrEtapu++;
                    etap = opis;
                    bool obrot = opis.StartsWith("OBRÓT");
                    bool rozmowa = opis == "ROZMOWA";
                    string doGrania = klip;
                    if (klip == "Idle" && pozy.Length > 0) doGrania = pozy[Mathf.Clamp(poza, 0, pozy.Length - 1)];
                    if (a.GetClip(doGrania)) { a[doGrania].wrapMode = WrapMode.Loop; a.CrossFade(doGrania, .2f); }
                    else
                    {
                        // Nie samo „BRAK KLIPU” — od razu widac, co ta postac ma, zeby nie trzeba
                        // bylo tego dochodzic osobno.
                        var ma = new System.Collections.Generic.List<string>();
                        foreach (AnimationState st in a) if (st.clip != null) ma.Add(st.name);
                        ma.Sort(System.StringComparer.Ordinal);
                        // Na telefonie w pionie dluga linia ucina sie na krawedzi (widac na
                        // nagraniu wlasciciela: „BRAK KLIPU Run (ma:” i koniec). Krotko i konkretnie.
                        etap = opis + " — BRAK " + doGrania + " (" + ma.Count + " ujec: " +
                               string.Join(",", ma.GetRange(0, System.Math.Min(4, ma.Count))) +
                               (ma.Count > 4 ? "…" : "") + ")";
                    }
                    // OBRÓT: pełne 360 stopni, żeby było widać każdy profil — tył, bok, przód
                    // (właściciel 16.11, 20:34: „chcę widzieć jak się obracają, każdy profil”).
                    // GŁOS: w rozmowie leci nagranie, a mimika jedzie z jego ścieżki, więc usta
                    // chodzą do tego, co słychać.
                    // ROZMOWA: KAMERA NA TWARZ i KILKA NAGRAŃ POD RZĄD.
                    // Właściciel 16.11, 21:06: „nie widać jak ustami ruszają w ogóle, za słabo,
                    // więcej tekstu potrzebuję, żeby było widać normalnie jak mrugają”.
                    // Przyczyna nie była w sile mimiki, tylko w kadrze: kamera stała 3,1 m od
                    // postaci, więc twarz miała na telefonie około stu pikseli. Przy takim ujęciu
                    // nie da się ocenić ani ust, ani powiek, choćby chodziły idealnie.
                    zblizenie = rozmowa;
                    if (rozmowa)
                    {
                        var mim = a.GetComponentInChildren<BraxMimikaOsiedla>();
                        var zrodlo2 = a.GetComponent<AudioSource>();
                        if (!zrodlo2) zrodlo2 = a.gameObject.AddComponent<AudioSource>();
                        zrodlo2.spatialBlend = 0f;
                        zrodlo2.playOnAwake = false;
                        // GŁOS WEDŁUG PŁCI. Nagrania mają w nazwie _f i _m — wcześniej brałem
                        // pierwsze z brzegu i kobieta dostawała męski głos (właściciel 16.11, 21:08).
                        bool ona = kto < kobieta.Length && kobieta[kto];
                        var wszystkie = Resources.LoadAll<AudioClip>("OsiedleGlosy");
                        var pasujace = new System.Collections.Generic.List<AudioClip>();
                        foreach (var k in wszystkie)
                            if (k && k.name.EndsWith(ona ? "_f" : "_m", System.StringComparison.Ordinal))
                                pasujace.Add(k);
                        var nagrania = pasujace.Count > 0 ? pasujace.ToArray() : wszystkie;
                        for (int n = 0; n < NagranPodRzad; n++)
                        {
                            var klipGlosu = nagrania != null && nagrania.Length > 0
                                ? nagrania[(kto * 7 + n * 13) % nagrania.Length]
                                : Resources.Load<AudioClip>("OsiedleGlosy/" + Kwestia);
                            if (!klipGlosu) { etap = opis + " — brak nagrań głosu"; break; }
                            zrodlo2.clip = klipGlosu;
                            zrodlo2.Play();
                            if (mim) mim.Wypowiedz(zrodlo2);
                            etap = opis + "  ♪ " + (n + 1) + "/" + NagranPodRzad + (ona ? "  (głos kobiecy)" : "");
                            float koniecGlosu = Time.time + klipGlosu.length + .25f;
                            while (Time.time < koniecGlosu) yield return null;
                        }
                        if (zrodlo2.isPlaying) zrodlo2.Stop();
                        zblizenie = false;
                        continue;
                    }

                    // W spoczynku postać powoli się obraca — właściciel 16.11, 21:29:
                    // „nie mogą tak w fazie spoczynkowej się nic nie ruszać, normalnie powinien
                    // się obkręcać albo coś”. Pełny obrót jest wolniejszy niż w etapie OBRÓT,
                    // żeby dało się patrzeć na samą postawę, a nie gonić ją wzrokiem.
                    bool spoczynek = opis == "SPOCZYNEK";
                    float dlugosc = obrot ? ObrotSekund : (spoczynek ? SpoczynekSekund : naEtap);
                    float start = Time.time, do_ = start + dlugosc;
                    while (Time.time < do_)
                    {
                        if (obrot || spoczynek)
                        {
                            float okres = obrot ? ObrotSekund : SpoczynekSekund;
                            float post = (Time.time - start) / okres;
                            korzen.localRotation = Quaternion.Euler(0, 180f + post * 360f, 0);
                        }
                        yield return null;
                    }
                    if (obrot || spoczynek) korzen.localRotation = Quaternion.Euler(0, 180f, 0);
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
        if (zblizenie)
        {
            // Zbliżenie na twarz: 0,62 m od głowy. Usta i powieki zajmują wtedy realną część
            // ekranu, więc widać, czy naprawdę chodzą.
            var glowa = postacie[kto].transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:Neck/mixamorig:Head");
            var celGlowy = glowa ? glowa.position + Vector3.up * (.08f * SKALA)
                                 : stopy + Vector3.up * (1.62f * SKALA);
            // Kamera staje PRZED twarzą, licząc od kierunku, w którym postać patrzy — nie
            // w stałym kierunku świata. Przedtem offset szedł zawsze wzdłuż -Z, więc przy
            // obróconej postaci kamera lądowała z boku głowy i twarz uciekała poza kadr
            // (nagranie właściciela 16.11, 21:29 — imprezowiczka ucięta przy krawędzi).
            var przod = postacie[kto].transform.forward;
            przod.y = 0f;
            if (przod.sqrMagnitude < .0001f) przod = Vector3.back;
            przod.Normalize();
            kamera.transform.position = celGlowy + przod * (.62f * SKALA) + Vector3.up * (.02f * SKALA);
            kamera.transform.LookAt(celGlowy);
            return;
        }
        var cel = stopy + Vector3.up * (1.15f * SKALA);
        kamera.transform.position = stopy + new Vector3(0f, 1.35f * SKALA, -3.1f * SKALA);
        kamera.transform.LookAt(cel);
        if (pozy.Length > 0 && pozy[Mathf.Clamp(poza, 0, pozy.Length - 1)] == PozaZPostawa)
            DolozPostawe(postacie[kto]);
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
        if (GUI.Button(new Rect(Screen.width - 24 - Mathf.Min(360, Screen.width * .52f), Screen.height - 160, Mathf.Min(360, Screen.width * .52f), 92),
                       "MATERIAŁ: " + Ustawienia[wariant].nazwa, przyciskStyl))
        {
            wariant = (wariant + 1) % Ustawienia.Length;
            ZastosujMaterial();
        }
        if (pozy.Length > 1 &&
            GUI.Button(new Rect(Screen.width - 24 - Mathf.Min(360, Screen.width * .52f), Screen.height - 262, Mathf.Min(360, Screen.width * .52f), 92),
                       "POZA: " + pozy[Mathf.Clamp(poza, 0, pozy.Length - 1)] +
                       "  (" + (poza + 1) + "/" + pozy.Length + ")", przyciskStyl))
        {
            poza = (poza + 1) % pozy.Length;
            var a = postacie[Mathf.Clamp(kto, 0, postacie.Length - 1)];
            if (a && pozy[poza] == PozaZPostawa && a.GetClip("Idle"))
            {
                a["Idle"].wrapMode = WrapMode.Loop;
                a.CrossFade("Idle", .25f);
                Debug.Log("BRAX_POKAZ_POZA Idle + dołożona postawa");
            }
            else if (a && a.GetClip(pozy[poza]))
            {
                a[pozy[poza]].wrapMode = WrapMode.Loop;
                a.CrossFade(pozy[poza], .25f);
                Debug.Log("BRAX_POKAZ_POZA " + pozy[poza]);
            }
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

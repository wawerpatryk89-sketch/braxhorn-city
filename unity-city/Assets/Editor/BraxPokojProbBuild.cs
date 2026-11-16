using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// POKÓJ PRÓB POSTACI — osobna, mała scena (właściciel 16.11, 08:48: „osobny pokój na testy dla
// postaci i tyle. I tam będziemy testować”). Nie generuje rejonu, nie buduje APK, nie czeka
// w kolejce za buildem Osiedla — ma własny workflow (.github/workflows/postacie-proba.yml).
//
// Pokój: 14 × 9 m × K, posadzka, cztery ściany, sufit, światło dzienne przez „okno”.
// Postacie stawia BraxClaudeOsiedle.ModelObsady — ta sama droga co w grze, nie kopia kodu.
public static class BraxPokojProbBuild
{
    const float K = 2.33f / 1.80f;

    static Material Mat(string nazwa, Color kolor, float gladkosc)
    {
        var m = new Material(Shader.Find("Standard")) { name = nazwa };
        m.SetColor("_Color", kolor);
        m.SetFloat("_Glossiness", gladkosc);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    static GameObject Plyta(string nazwa, Transform rodzic, Vector3 srodek, Vector3 rozmiar, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nazwa;
        g.transform.SetParent(rodzic, false);
        g.transform.localPosition = srodek;
        g.transform.localScale = rozmiar;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    // game-ci/unity-builder NIE przepuszcza własnych zmiennych środowiska do kontenera z Unity
    // (pierwszy przebieg pokoju: scena dostała domyślnego Patryka zamiast trzech postaci).
    // Dlatego zlecenie idzie plikiem — tak samo jak rejon buildu przez unity-city/rejon-ci.txt.
    const string PlikZlecenia = "pokoj-prob.txt";

    // Właściciel 16.11, 12:25: „Max 5 nie więcej”. Pokój bierze NAJWYŻEJ pięć postaci na próbę.
    const int MaxPostaci = 5;

    static string[] Zlecenie()
    {
        string sciezka = Path.GetFullPath(Path.Combine(Application.dataPath, "..", PlikZlecenia));
        if (File.Exists(sciezka))
        {
            var linie = File.ReadAllLines(sciezka)
                            .Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            if (linie.Length > 0) return linie;
        }
        return new[] { Environment.GetEnvironmentVariable("BRAX_POSTACIE") ?? "patryk",
                       Environment.GetEnvironmentVariable("BRAX_TRYB") ?? "solo" };
    }

    static string[] PostacieZOtoczenia()
    {
        string v = Zlecenie()[0];
        if (string.IsNullOrWhiteSpace(v)) return new[] { "patryk" };
        var lista = v.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToArray();
        if (lista.Length > MaxPostaci)
        {
            Debug.Log("BRAX_POKOJ limit " + MaxPostaci + " — odłożone: " +
                      string.Join(",", lista.Skip(MaxPostaci)));
            lista = lista.Take(MaxPostaci).ToArray();
        }
        return lista;
    }

    static string TrybZlecenia()
    {
        var z = Zlecenie();
        string t = z.Length > 1 ? z[1] : null;
        if (string.IsNullOrWhiteSpace(t)) t = Environment.GetEnvironmentVariable("BRAX_TRYB");
        return (t ?? "solo").Trim().ToLowerInvariant();
    }

    // Pliki wgrane przed startem Unity bywają jeszcze nieprzetworzone, gdy budujemy scenę
    // od razu po uruchomieniu edytora — wymuszamy import, żeby klipy legacy były na miejscu.
    static void DoczytajZrodla(IEnumerable<string> ids)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string bar = BraxObsadaImport.KatalogBaru;
        if (AssetDatabase.IsValidFolder(bar.TrimEnd('/')))
            foreach (string p in Directory.GetFiles(bar, "*.fbx"))
            {
                string sciezka = p.Replace('\\', '/');
                // Jawnie, a nie licząc na kolejność AssetPostprocessora: klipy baru muszą być
                // legacy, inaczej DodajKlipBaru ich nie znajdzie (run 43829276955: Patryk bez Drink,
                // choć w patryk.fbx z paczki baru klip „Armature|Drink” jest).
                if (AssetImporter.GetAtPath(sciezka) is ModelImporter mi)
                {
                    mi.animationType = ModelImporterAnimationType.Legacy;
                    mi.importAnimation = true;
                    mi.SaveAndReimport();
                }
                else AssetDatabase.ImportAsset(sciezka, ImportAssetOptions.ForceSynchronousImport);
            }
        foreach (string id in ids)
        {
            string folder = BraxObsadaImport.Katalog + id + "/";
            if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/'))) continue;
            foreach (string p in Directory.GetFiles(folder))
                if (!p.EndsWith(".meta", StringComparison.Ordinal))
                    AssetDatabase.ImportAsset(p.Replace('\\', '/'), ImportAssetOptions.ForceSynchronousImport);
        }
    }

    public static void Zbuduj()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- pomieszczenie ------------------------------------------------
        float dl = 14f * K, szer = 9f * K, wys = 3.4f * K, gr = .12f * K;
        var pokoj = new GameObject("Pokój prób").transform;
        var matPodloga = Mat("posadzka", new Color(.34f, .33f, .32f), .25f);
        var matSciana = Mat("ściana", new Color(.70f, .70f, .68f), .12f);
        var matSufit = Mat("sufit", new Color(.86f, .86f, .85f), .08f);
        var matPas = Mat("pas pomiarowy", new Color(.22f, .45f, .72f), .3f);

        Plyta("posadzka", pokoj, new Vector3(0, -gr * .5f, 0), new Vector3(dl, gr, szer), matPodloga);
        Plyta("sufit", pokoj, new Vector3(0, wys, 0), new Vector3(dl, gr, szer), matSufit);
        Plyta("ściana tył", pokoj, new Vector3(0, wys * .5f, szer * .5f), new Vector3(dl, wys, gr), matSciana);
        Plyta("ściana przód", pokoj, new Vector3(0, wys * .5f, -szer * .5f), new Vector3(dl, wys, gr), matSciana);
        Plyta("ściana lewa", pokoj, new Vector3(-dl * .5f, wys * .5f, 0), new Vector3(gr, wys, szer), matSciana);
        Plyta("ściana prawa", pokoj, new Vector3(dl * .5f, wys * .5f, 0), new Vector3(gr, wys, szer), matSciana);
        // pasy co metr na posadzce — widać po nich, czy chód i bieg naprawdę niosą postać
        for (int i = -6; i <= 6; i++)
            Plyta($"pas {i} m", pokoj, new Vector3(i * K, .004f, 0), new Vector3(.04f * K, .008f, szer * .9f), matPas);

        // --- światło ------------------------------------------------------
        var slonce = new GameObject("Słońce").AddComponent<Light>();
        slonce.type = LightType.Directional;
        slonce.transform.rotation = Quaternion.Euler(42, -36, 0);
        slonce.intensity = 1.15f;
        slonce.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.62f, .66f, .72f);
        RenderSettings.ambientEquatorColor = new Color(.48f, .48f, .50f);
        RenderSettings.ambientGroundColor = new Color(.26f, .25f, .24f);
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 40f;
        QualitySettings.shadowCascades = 2;

        // --- postacie -----------------------------------------------------
        var ids = PostacieZOtoczenia();
        DoczytajZrodla(ids);
        var anim = new List<Animation>();
        var nazwy = new List<string>();
        var mimiki = new List<BraxMimikaOsiedla>();
        var brakujace = new List<string>();

        foreach (string id in ids)
        {
            try
            {
                var korzen = new GameObject("Postać " + id).transform;
                korzen.SetParent(pokoj, false);
                korzen.localPosition = new Vector3(0, .02f, 0);
                korzen.localRotation = Quaternion.identity;
                var a = BraxClaudeOsiedle.ModelObsady(id, korzen, 1.80f * K, false);
                anim.Add(a);
                nazwy.Add(id);
                mimiki.Add(a.GetComponentInChildren<BraxMimikaOsiedla>());
                a.gameObject.SetActive(false);
            }
            catch (Exception e)
            {
                brakujace.Add(id + ": " + e.Message);
                Debug.LogWarning("BRAX_POKOJ postać pominięta " + id + " — " + e.Message);
            }
        }
        if (anim.Count == 0)
            throw new InvalidOperationException("Pokój prób: żadna postać się nie wczytała. " +
                                                string.Join(" | ", brakujace));
        Debug.Log("BRAX_POKOJ postacie=" + anim.Count + "/" + ids.Length +
                  (brakujace.Count > 0 ? " pominięte: " + string.Join(" | ", brakujace) : ""));

        // --- kamera i tryb próby -------------------------------------------
        string tryb = TrybZlecenia();
        var kam = new GameObject("Kamera próby").AddComponent<Camera>();
        kam.tag = "MainCamera";
        kam.gameObject.AddComponent<AudioListener>();
        kam.clearFlags = CameraClearFlags.SolidColor;
        kam.backgroundColor = new Color(.10f, .11f, .13f);
        kam.nearClipPlane = .15f;

        if (tryb == "solo")
        {
            kam.fieldOfView = 34f;
            kam.transform.position = new Vector3(0f, 1.45f * K, -4.6f * K);
            kam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
            var proba = new GameObject("Próba postaci").AddComponent<BraxProbaPostaci>();
            proba.kamera = kam;
            proba.postacie = anim.ToArray();
            proba.nazwy = nazwy.ToArray();
            proba.mimiki = mimiki.ToArray();
        }
        else
        {
            bool walka = tryb == "walka";
            bool stres = tryb == "stres";
            // Bić się mają TYLKO te postacie, które mają zestaw walki (właściciel 16.11, 09:02).
            if (walka)
            {
                var umieja = Enumerable.Range(0, anim.Count)
                    .Where(i => anim[i].GetClip("CombatIdle") &&
                                new[] { "Jab", "Cross", "Hook" }.Any(c => anim[i].GetClip(c)))
                    .ToList();
                if (umieja.Count < 2)
                    throw new InvalidOperationException(
                        "Próba walki: trzeba dwóch z zestawem walki. Mają go: " +
                        (umieja.Count == 0 ? "nikt z podanych" : string.Join(", ", umieja.Select(i => nazwy[i]))));
                anim = umieja.Select(i => anim[i]).ToList();
                nazwy = umieja.Select(i => nazwy[i]).ToList();
                mimiki = umieja.Select(i => mimiki[i]).ToList();
            }

            float promien = walka ? .62f * K : stres ? 1.75f * K : .85f * K;   // walka: dystans ciosu; stres: świadkowie wokół; rozmowa: dystans rozmowy
            int n = Mathf.Min(anim.Count, walka ? 2 : stres ? MaxPostaci : 3);
            anim = anim.Take(n).ToList(); nazwy = nazwy.Take(n).ToList(); mimiki = mimiki.Take(n).ToList();
            var spojrzenia = new List<BraxSpojrzeniePatryka>();
            for (int i = 0; i < n; i++)
            {
                // STRES: stresowana stoi w ŚRODKU, świadkowie w półkolu wokół niej i przodem do niej —
                // inaczej nie da się zmierzyć, czy na nią patrzą.
                Vector3 poz;
                if (stres)
                    poz = i == 0 ? Vector3.zero
                        : Quaternion.Euler(0, -70f + 140f * (i - 1) / Mathf.Max(1, n - 2), 0) * Vector3.back * promien;
                else
                {
                    float katUst = n == 2 ? (i == 0 ? 180f : 0f) : i * 120f;
                    poz = Quaternion.Euler(0, katUst, 0) * Vector3.forward * promien;
                }
                var korzen = anim[i].transform.parent;
                korzen.localPosition = new Vector3(poz.x, .02f, poz.z);
                korzen.localRotation = poz.sqrMagnitude < .0001f
                    ? Quaternion.LookRotation(Vector3.back, Vector3.up)
                    : Quaternion.LookRotation(-poz.normalized, Vector3.up);
                anim[i].gameObject.SetActive(true);

                var kosci = anim[i].GetComponentsInChildren<Transform>(true);
                var szyja = kosci.FirstOrDefault(t => t.name.EndsWith("Neck", StringComparison.Ordinal));
                var glowa = kosci.FirstOrDefault(t => t.name.EndsWith("Head", StringComparison.Ordinal));
                var wzrok = anim[i].gameObject.AddComponent<BraxSpojrzeniePatryka>();
                wzrok.postac = korzen;
                wzrok.szyja = szyja;
                wzrok.glowa = glowa;
                wzrok.przodGlowy = glowa ? glowa.InverseTransformDirection(korzen.forward) : Vector3.forward;
                spojrzenia.Add(wzrok);
            }

            kam.fieldOfView = walka ? 38f : stres ? 52f : 40f;
            kam.transform.position = stres
                ? new Vector3(0f, 1.6f * K, -4.3f * K)
                : new Vector3(2.9f * K, 1.5f * K, -3.4f * K);
            kam.transform.LookAt(new Vector3(0f, 1.15f * K, 0f));

            var para = new GameObject("Próba par").AddComponent<BraxProbaPary>();
            para.tryb = walka ? BraxProbaPary.Tryb.Walka
                      : stres ? BraxProbaPary.Tryb.Stres
                      : BraxProbaPary.Tryb.Rozmowa;
            para.postacie = anim.ToArray();
            para.nazwy = nazwy.ToArray();
            para.mimiki = mimiki.ToArray();
            para.spojrzenia = spojrzenia.ToArray();
            Debug.Log("BRAX_POKOJ tryb=" + tryb + " postacie=" + string.Join(",", nazwy));
        }

        const string path = "Assets/PokojProb.unity";
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException(path);
        var dest = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/pokoj/PokojProb.x86_64"));
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        var wynik = BuildPipeline.BuildPlayer(new[] { path }, dest, BuildTarget.StandaloneLinux64, BuildOptions.None);
        if (wynik.summary.result != BuildResult.Succeeded)
            throw new Exception(wynik.summary.result.ToString());
    }

    // ======================= APK: POKÓJ DO OBEJRZENIA NA TELEFONIE =======================
    // Właściciel 16.11, 10:31: „pokój, który generuje APK, lekki pokój, do którego wchodzą
    // postacie tylko na testy”. Serwer CI nie ma karty graficznej i renderuje programowo —
    // dlatego obrazu nie robimy tam wcale. APK renderuje TELEFON właściciela: wchodzi do
    // pokoju, chodzi między postaciami i sam je ogląda. Bez świata, bez rejonu, kilka MB.
    // PORÓWNANIE POŁYSKU (właściciel 16.11, 15:31: „świecą się i są plastikowi wszyscy”).
    // Ta sama postać trzy razy obok siebie, trzy mnożniki gładkości. Nie zmienia map ani gry —
    // mnożnik w shaderze domyślnie wynosi 1, czyli bez tej próby nic się nie dzieje.
    // Zmierzone podstawy: u Patryka 52 % powierzchni ma gładkość 0,4–0,5 przy odchyleniu 0,11
    // w całej mapie, czyli skóra i ubranie odbijają światło prawie tak samo. Skóra ma być
    // matowa (0,2–0,3), tkanina 0,1–0,25.
    // Cztery warianty obok siebie (właściciel 16.11, 17:58: „postacie mają być naturalne,
    // prawdziwe, żadnego plastiku, żadnego połysku”). Każdy rusza WSZYSTKIE trzy przyczyny
    // zmierzone w paczkach, nie samą gładkość:
    //   gładkość  — mediana w mapach to 0,43, a skóra ma mieć 0,2–0,3;
    //   metal     — imprezowiczka ma 0,60 na całej postaci (artefakt generatora 3D);
    //   normalne  — u Ewy 74 % pikseli bez faktury, więc nie ma co rozbić odblasku.
    // nazwa, mnożnik gładkości, mnożnik metaliczności, siła normalnych
    static readonly (string nazwa, float gl, float me, float nr)[] Warianty =
    {
        ("1  jak teraz",      1.00f, 1f, 1.0f),
        ("2  matowy",         0.55f, 0f, 1.0f),
        ("3  mat + faktura",  0.45f, 0f, 1.6f),
        ("4  pelny mat",      0.33f, 0f, 2.0f),
        ("5  bez odblasku",   0.00f, 0f, 1.3f),
    };

    static void Polysk(Transform pokoj, Camera kam, List<Animation> anim, List<string> nazwy)
    {
        if (anim.Count == 0) throw new InvalidOperationException("Próba połysku: brak postaci");
        float rozstaw = 1.05f * K;
        float srodek = (Warianty.Length - 1) * .5f;
        for (int i = 0; i < Warianty.Length; i++)
        {
            var zrodlo = anim[0];
            var kopia = i == 0 ? zrodlo.gameObject
                               : UnityEngine.Object.Instantiate(zrodlo.gameObject,
                                     zrodlo.transform.parent.parent, false);
            var korzen = kopia.transform.parent ? kopia.transform.parent : kopia.transform;
            korzen.localPosition = new Vector3((i - srodek) * rozstaw, .02f, 0f);
            korzen.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            kopia.SetActive(true);

            // Własny materiał na każdą kopię — inaczej wszystkie dzieliłyby jeden.
            //
            // BEZ AssetDatabase.CreateAsset. Pierwsza wersja zapisywała skopiowany materiał jako
            // plik w katalogu ŹRÓDŁOWYM postaci (Assets/Source/ObsadaOsiedla/patryk/). Zapis
            // w tym katalogu w trakcie budowania sceny uruchamia import tego katalogu, a ten
            // przechodzi przez BraxObsadaImport — i materiały, które już były przypisane,
            // traciły odwołania do tekstur. Skutek: postać wyszła CAŁKOWICIE BIAŁA, bo pusty
            // _MainTex w Unity to biel (nagranie właściciela 16.11, 20:27).
            // Sprawdzone: tekstury SĄ w APK (Color 2048x2048 ASTC 6x6), więc kompresja ich nie
            // zgubiła — zgubiło je zapisywanie materiału.
            // new Material(źródło) kopiuje wszystkie właściwości razem z teksturami, a materiał
            // zapisuje się wtedy wewnątrz sceny. Baza zasobów nie jest w ogóle ruszana.
            foreach (var r in kopia.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (!mats[m]) continue;
                    var kopiaMat = new Material(mats[m]) { name = mats[m].name + " w" + i };
                    kopiaMat.SetFloat("_Gladkosc", Warianty[i].gl);
                    kopiaMat.SetFloat("_MetalMnoznik", Warianty[i].me);
                    kopiaMat.SetFloat("_SilaNormalnych", Warianty[i].nr);
                    if (!kopiaMat.GetTexture("_MainTex"))
                        throw new InvalidOperationException(
                            "Wariant " + i + ": kopia materiału straciła teksturę — przerywam, " +
                            "zamiast oddać białą postać");
                    mats[m] = kopiaMat;
                }
                r.sharedMaterials = mats;
            }

            var a = kopia.GetComponent<Animation>();
            if (a && a.GetClip("Idle")) { a["Idle"].wrapMode = WrapMode.Loop; a.Play("Idle"); }
            // Podpisy nad postaciami ZOSTAJĄ tylko w porównaniu materiału, gdzie bez nich nie
            // wiadomo, który wariant jest który. W pokazie są zdjęte — właściciel 16.11, 21:06:
            // „napisy usuń z obrazu”, bo lądowały na twarzy oglądanej postaci.
            var opis = new GameObject("Podpis " + i).AddComponent<BraxPodpisPostaci>();
            opis.transform.SetParent(korzen, false);
            opis.transform.localPosition = Vector3.up * (2.05f * K);
            opis.nazwa = Warianty[i].nazwa;
        }
        kam.fieldOfView = 50f;
        kam.transform.position = new Vector3(0f, 1.3f * K, -4.9f * K);
        kam.transform.LookAt(new Vector3(0f, 1.05f * K, 0f));
        Debug.Log("BRAX_POKOJ_POLYSK postac=" + nazwy[0] + " warianty=" +
                  string.Join(" | ", Warianty.Select(w =>
                      $"{w.nazwa}: gl={w.gl:F2} metal={w.me:F0} norm={w.nr:F1}")));
    }

    static void Twarze(Transform pokoj, Camera kam, List<Animation> anim, List<string> nazwy)
    {
        int ile = Mathf.Min(4, anim.Count);
        if (ile == 0) throw new InvalidOperationException("Próba twarzy: brak postaci");

        // Postacie stoją daleko od siebie, żeby żadna nie weszła w kadr sąsiada.
        for (int i = 0; i < anim.Count; i++)
        {
            var korzen = anim[i].transform.parent ? anim[i].transform.parent : anim[i].transform;
            korzen.localPosition = new Vector3(i * 6f * K, .02f, 0f);
            korzen.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            anim[i].gameObject.SetActive(i < ile);
        }

        var kamery = new Camera[ile];
        var kafelki = new[]
        {
            new Rect(0f, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f),
            new Rect(0f, 0f, .5f, .5f),  new Rect(.5f, 0f, .5f, .5f),
        };
        for (int i = 0; i < ile; i++)
        {
            var c = i == 0 ? kam : new GameObject("Kamera twarzy " + i).AddComponent<Camera>();
            c.rect = kafelki[i];
            c.fieldOfView = 32f;
            c.nearClipPlane = .05f;
            c.farClipPlane = 30f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(.13f, .14f, .15f);   // ciemne, neutralne tło jak we wzorcu
            c.depth = i;
            kamery[i] = c;
        }

        var siatka = new GameObject("Siatka twarzy").AddComponent<BraxTwarzeSiatka>();
        siatka.postacie = anim.GetRange(0, ile).ToArray();
        siatka.nazwy = nazwy.GetRange(0, ile).ToArray();
        siatka.kamery = kamery;
        var zenskieT = new HashSet<string> { "ewa", "monika", "aneta", "martyna" };
        var plcieT = new bool[ile];
        for (int i = 0; i < ile; i++)
            plcieT[i] = zenskieT.Contains(nazwy[i]) ||
                        nazwy[i].StartsWith("imprezowiczka", StringComparison.Ordinal) ||
                        nazwy[i].StartsWith("kobieta", StringComparison.Ordinal);
        siatka.kobieta = plcieT;
        Debug.Log("BRAX_POKOJ_TWARZE " + string.Join(",", nazwy.GetRange(0, ile)
            .Select((n, i) => n + "=" + (plcieT[i] ? "K" : "M"))));
    }

    public static void ZbudujApk()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        float dl = 20f * K, szer = 10f * K, wys = 3.6f * K, gr = .12f * K;
        var pokoj = new GameObject("Pokój prób").transform;
        // Neutralna szarość studyjna i materiały MATOWE (gładkość prawie zero): ściany nie mają
        // dokładać własnych odblasków ani barwić postaci. Ani mleczna biel (było 0.70 — zalewało
        // postać odbitym światłem), ani ciemnica (było 0.26 — wypychało odblask na wierzch).
        var matPodloga = Mat("posadzka", new Color(.34f, .34f, .34f), .04f);
        var matSciana = Mat("ściana", new Color(.46f, .46f, .47f), .03f);
        var matSufit = Mat("sufit", new Color(.52f, .52f, .52f), .02f);
        var matPas = Mat("pas pomiarowy", new Color(.22f, .45f, .72f), .3f);

        var podloga = Plyta("posadzka", pokoj, new Vector3(0, -gr * .5f, 0), new Vector3(dl, gr, szer), matPodloga);
        podloga.isStatic = true;
        Plyta("sufit", pokoj, new Vector3(0, wys, 0), new Vector3(dl, gr, szer), matSufit);
        Plyta("ściana tył", pokoj, new Vector3(0, wys * .5f, szer * .5f), new Vector3(dl, wys, gr), matSciana);
        Plyta("ściana przód", pokoj, new Vector3(0, wys * .5f, -szer * .5f), new Vector3(dl, wys, gr), matSciana);
        Plyta("ściana lewa", pokoj, new Vector3(-dl * .5f, wys * .5f, 0), new Vector3(gr, wys, szer), matSciana);
        Plyta("ściana prawa", pokoj, new Vector3(dl * .5f, wys * .5f, 0), new Vector3(gr, wys, szer), matSciana);
        for (int i = -8; i <= 8; i++)
            Plyta($"pas {i} m", pokoj, new Vector3(i * K, .005f, 0), new Vector3(.04f * K, .008f, szer * .9f), matPas);

        var slonce = new GameObject("Słońce").AddComponent<Light>();
        slonce.type = LightType.Directional;
        // ŚWIATŁO DOKŁADNIE TAKIE JAK NA OSIEDLU — nie wymyślone przeze mnie.
        // Poprawka 16.11, 15:26 (właściciel: „te postacie wyglądają jeszcze gorzej”).
        // Zrobiłem wcześniej dwie zmiany naraz: ciemne wnętrze I mocniejsze słońce. To najgorsze
        // połączenie dla skóry — w ciemnym otoczeniu odblask zostaje jedyną jasną rzeczą w kadrze,
        // więc świeci MOCNIEJ, nie słabiej. A przedtem było odwrotnie: mleczne białe ściany
        // zalewały postać odbitym światłem.
        //
        // Właściwa odpowiedź nie jest pośrodku, tylko gdzie indziej: pokój służy do oceniania,
        // czy postać nadaje się DO GRY, więc ma ją pokazywać w świetle GRY. Liczby poniżej to
        // co do jednej wartości BraxClaudeOsiedle.Daylight (linie 3928–3940) — czyli wzorzec,
        // który właściciel wskazał jako najlepszy. Jeśli postać wygląda źle tutaj, będzie
        // wyglądać źle na Osiedlu, i o to w tej próbie chodzi.
        slonce.intensity = 1.24f;
        slonce.color = new Color(1f, .955f, .875f);
        slonce.transform.rotation = Quaternion.Euler(43f, -35f, 0f);
        slonce.shadows = LightShadows.Soft;
        slonce.shadowStrength = .80f;
        slonce.shadowBias = .04f;
        slonce.shadowNormalBias = .25f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.60f, .68f, .80f);
        RenderSettings.ambientEquatorColor = new Color(.50f, .53f, .50f);
        RenderSettings.ambientGroundColor = new Color(.30f, .28f, .23f);

        // POSTACIE W ŁUKU przodem do gracza. Szereg był zły: policzone 16.11 — przy 19 postaciach
        // w kadrze (FOV 55 st., 6,2 m) mieściły się TRZY. Łuk 150 st. na promieniu 5,2 m trzyma
        // wszystkie w zasięgu obrotu gracza, a pokaz i tak wyciąga je pojedynczo na środek.
        var ids = PostacieZOtoczenia();
        DoczytajZrodla(ids);
        var ustawioneAnim = new List<Animation>();
        var ustawioneNazwy = new List<string>();
        var braki = new List<string>();
        float promien = 5.2f * K;
        float luk = Mathf.Min(150f, Mathf.Max(0f, (ids.Length - 1) * 9f));
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            try
            {
                float kat = ids.Length < 2 ? 0f : -luk * .5f + luk * i / (ids.Length - 1);
                var kier = Quaternion.Euler(0, kat, 0) * Vector3.forward;
                var korzen = new GameObject("Postać " + id).transform;
                korzen.SetParent(pokoj, false);
                korzen.localPosition = new Vector3(kier.x * promien, .02f, kier.z * promien);
                korzen.localRotation = Quaternion.LookRotation(-kier, Vector3.up);
                var a = BraxClaudeOsiedle.ModelObsady(id, korzen, 1.80f * K, false);
                if (a.GetClip("Idle")) { a["Idle"].wrapMode = WrapMode.Loop; a.Play("Idle"); }
                var opis = new GameObject("Podpis " + id).AddComponent<BraxPodpisPostaci>();
                opis.transform.SetParent(korzen, false);
                opis.transform.localPosition = Vector3.up * (2.05f * K);
                opis.nazwa = id;
                ustawioneAnim.Add(a);
                ustawioneNazwy.Add(id);
            }
            catch (Exception e)
            {
                braki.Add(id + ": " + e.Message);
                Debug.LogWarning("BRAX_POKOJ pominięta " + id + " — " + e.Message);
            }
        }
        int ustawione = ustawioneAnim.Count;
        if (ustawione == 0)
            throw new InvalidOperationException("Pokój APK: żadna postać się nie wczytała. " + string.Join(" | ", braki));
        Debug.Log("BRAX_POKOJ_APK postacie=" + ustawione + "/" + ids.Length + " łuk=" + luk.ToString("F0") + " st." +
                  (braki.Count > 0 ? " pominięte: " + string.Join(" | ", braki) : ""));

        // gracz: ten sam Patryk i to samo sterowanie co w rejonie (BraxTouchControls dokłada się sam)
        var gracz = new GameObject("BraxhornPlayer");
        gracz.transform.position = new Vector3(0, .05f, -2.6f * K);
        var kapsula = gracz.AddComponent<CharacterController>();
        kapsula.radius = .32f;
        // Run 43841249558 padł DOKŁADNIE tutaj: jedna wadliwa postać (klip Drink) rzuciła wyjątek
        // poza pętlą postaci i zabiła całe APK. Pokój służy do PATRZENIA na obsadę — gdy gracz się
        // nie złoży, próba musi mimo to powstać.
        GameObject bohater = null;
        try { bohater = BraxClaudeOsiedle.NowyPatrykOsiedla(gracz.transform, kapsula); }
        catch (Exception e)
        {
            Debug.LogWarning("BRAX_POKOJ gracz bez modelu Patryka — " + e.Message);
            foreach (string zapas in ustawioneNazwy)
            {
                try
                {
                    var a = BraxClaudeOsiedle.ModelObsady(zapas, gracz.transform, 1.80f * K, false);
                    foreach (var tr in a.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 8;
                    kapsula.height = 1.80f * K;
                    kapsula.center = Vector3.up * (kapsula.height * .5f);
                    bohater = a.gameObject;
                    Debug.Log("BRAX_POKOJ gracz zastępczy: " + zapas);
                    break;
                }
                catch (Exception e2) { Debug.LogWarning("BRAX_POKOJ zastępczy " + zapas + " też nie — " + e2.Message); }
            }
            if (!bohater)
                throw new InvalidOperationException("Pokój APK: nie ma z czego zrobić gracza. " +
                                                    string.Join(" | ", braki) + " | " + e.Message);
        }
        // Model gracza jest WYLACZANY: w pokoju oglada sie obsade, a nie wlasne plecy.
        // Na nagraniu wlasciciela (16.11, 14:48) to wlasnie model gracza zaslanial cala scene.
        if (bohater) bohater.SetActive(false);
        var runtime = gracz.AddComponent<BraxGymPlayer>();
        runtime.visual = bohater.transform;
        runtime.body = kapsula;
        runtime.clips = bohater.GetComponent<Animation>();
        runtime.bags = Array.Empty<Transform>();
        runtime.worldSpawn = gracz.transform.position;
        runtime.buildId = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "pokoj";
        var kam = new GameObject("Kamera gracza").AddComponent<Camera>();
        kam.tag = "MainCamera";
        kam.fieldOfView = 55;
        kam.nearClipPlane = .15f;
        kam.farClipPlane = 60f;
        kam.gameObject.AddComponent<AudioListener>();
        runtime.view = kam;
        BraxCityBuild.UstawSterowanieOsiedla(runtime);

        // POŁYSK: zamiast pokazu — ta sama postać trzy razy obok siebie, trzy mnożniki
        // gładkości, do porównania na jednym ekranie (właściciel 16.11, 15:31–15:32).
        // Bez wczesnego wyjścia: ogon metody (klucz podpisu, zapis sceny, build) jest wspólny,
        // inaczej APK porównania wyszłoby niepodpisane.
        // POKÓJ DOSTAJE SKÓRĘ BEZ ODBLASKU. Właściciel 16.11, 21:56: „Naturalna skóra ma być,
        // żadnych białych odblasków, nic”. Wcześniej szedł wariant 3 (gładkość x0,45) i on
        // wciąż zostawiał połysk — zmierzone na bramkarzu: najbardziej błyszczący procent jego
        // powierzchni ma gładkość 0,64 przy kolorze 53–96, czyli na prawie czarnym mundurze
        // odblask jest JEDYNĄ rzeczą, którą widać (stąd „białe ślady”), a na kapitanie z tego
        // samego powodu rozmywa się namalowany krawat. Gładkość na zero gasi to całkowicie;
        // normalne x1,3 zostają, żeby skóra i tkanina nie wyszły jak guma.
        if (TrybZlecenia() != "polysk")
        {
            var w = Warianty[4];
            foreach (var a in ustawioneAnim)
                foreach (var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m)
                        {
                            m.SetFloat("_Gladkosc", w.gl);
                            m.SetFloat("_MetalMnoznik", w.me);
                            m.SetFloat("_SilaNormalnych", w.nr);
                        }
            Debug.Log($"BRAX_POKOJ_MATERIAL wariant={w.nazwa} gladkosc={w.gl:F2} " +
                      $"metal={w.me:F0} normalne={w.nr:F1}");
        }

        // SIATKA TWARZY — odtworzenie wzorca, który właściciel pokazał 16.11, 21:31
        // („BRAXHORN Mowa — próba głosu, 17–20 z 68”): cztery twarze naraz, kadr na twarz,
        // nazwa pod spodem, ciemne tło. Tak się ocenia mimikę; w ujęciu całej sylwetki twarz
        // ma na telefonie około stu pikseli i nie da się jej ocenić.
        bool siatkaTwarzy = TrybZlecenia() == "twarze";
        if (siatkaTwarzy)
        {
            runtime.enabled = false;        // gracz nie prowadzi kamer siatki
            Twarze(pokoj, kam, ustawioneAnim, ustawioneNazwy);
        }

        bool porownanie = TrybZlecenia() == "polysk";
        if (porownanie)
        {
            // Łuk z pozostałymi postaciami tylko zaśmieca kadr porównania — chowamy go.
            for (int i = 1; i < ustawioneAnim.Count; i++)
                if (ustawioneAnim[i]) ustawioneAnim[i].gameObject.SetActive(false);
            // Gracz musi przestać prowadzić kamerę, inaczej nadpisze ustawienie porównania —
            // dokładnie ten błąd widać było na nagraniu z 14:48, gdzie zamiast prezentowanej
            // postaci przez cały czas widać było Patryka.
            runtime.enabled = false;
            Polysk(pokoj, kam, ustawioneAnim, ustawioneNazwy);
        }

        // POKAZ: każda postać po kolei na środek, spoczynek → chód → bieg → rozmowa,
        // kamera trzyma ją w kadrze, na ekranie nazwa i etap. Przycisk NAGRYWAJ albo
        // nagrywanie ekranu telefonu daje gotowy film.
        if (!porownanie && !siatkaTwarzy)
        {
        var srodek = new GameObject("Środek pokazu").transform;
        srodek.SetParent(pokoj, false);
        srodek.localPosition = new Vector3(0f, .02f, 1.2f * K);
        var pokaz = new GameObject("Pokaz postaci").AddComponent<BraxPokazPostaci>();
        pokaz.postacie = ustawioneAnim.ToArray();
        pokaz.nazwy = ustawioneNazwy.ToArray();
        pokaz.srodek = srodek;
        pokaz.kamera = kam;
        pokaz.wariant = 2;   // startuje na „3 mat+faktura”; przyciskiem na ekranie idzie dalej
        // Płeć postaci — żeby kobieta dostała kobiecy głos. Lista z imion obsady właściciela.
        var zenskie = new HashSet<string> { "ewa", "monika", "aneta", "martyna" };
        var plcie = new bool[ustawioneNazwy.Count];
        for (int i = 0; i < ustawioneNazwy.Count; i++)
            plcie[i] = zenskie.Contains(ustawioneNazwy[i]) ||
                       ustawioneNazwy[i].StartsWith("imprezowiczka", StringComparison.Ordinal) ||
                       ustawioneNazwy[i].StartsWith("kobieta", StringComparison.Ordinal);
        pokaz.kobieta = plcie;
        Debug.Log("BRAX_POKOJ_PLCIE " + string.Join(",", ustawioneNazwy.Select((n, i) => n + "=" + (plcie[i] ? "K" : "M"))));
        // Podpisy nad głowami zdjęte z pokazu (lądowały na twarzy oglądanej postaci).
        foreach (var podpis in UnityEngine.Object.FindObjectsByType<BraxPodpisPostaci>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(podpis.gameObject);
        }

        PlayerSettings.companyName = "Braxhorn";
        PlayerSettings.productName = "Braxhorn Pokój Prób";
        // OSOBNY identyfikator, żeby pokój NIE nadpisał gry na telefonie.
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.braxhorn.pokojprob");
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.useAPKExpansionFiles = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
        string pass = File.Exists("braxhorn.keystore.pass") ? File.ReadAllText("braxhorn.keystore.pass").Trim() : null;
        if (File.Exists("braxhorn.keystore") && !string.IsNullOrEmpty(pass))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath("braxhorn.keystore");
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = "braxhorn";
            PlayerSettings.Android.keyaliasPass = pass;
        }
        else PlayerSettings.Android.useCustomKeystore = false;

        const string path = "Assets/PokojProbApk.unity";
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException(path);
        var dest = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/Android/braxhorn-pokoj.apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        var wynik = BuildPipeline.BuildPlayer(new[] { path }, dest, BuildTarget.Android, BuildOptions.None);
        if (wynik.summary.result != BuildResult.Succeeded) throw new Exception(wynik.summary.result.ToString());
        Debug.Log("BRAX_POKOJ_APK gotowe: " + dest + " (" + wynik.summary.totalSize / 1048576 + " MB)");
    }
}

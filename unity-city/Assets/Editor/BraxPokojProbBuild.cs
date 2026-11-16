using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
        return v.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToArray();
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
        foreach (string folder in new[] { BraxObsadaImport.KatalogBaru })
            if (AssetDatabase.IsValidFolder(folder.TrimEnd('/')))
                foreach (string p in Directory.GetFiles(folder, "*.fbx"))
                    AssetDatabase.ImportAsset(p.Replace('\\', '/'), ImportAssetOptions.ForceSynchronousImport);
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
                var a = BraxClaudeOsiedle.ModelObsady(id, korzen, 1.80f * K);
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

            float promien = walka ? .62f * K : .85f * K;   // walka: dystans ciosu; rozmowa: dystans rozmowy
            int n = Mathf.Min(anim.Count, walka ? 2 : 3);
            anim = anim.Take(n).ToList(); nazwy = nazwy.Take(n).ToList(); mimiki = mimiki.Take(n).ToList();
            var spojrzenia = new List<BraxSpojrzeniePatryka>();
            for (int i = 0; i < n; i++)
            {
                float katUst = n == 2 ? (i == 0 ? 180f : 0f) : i * 120f;
                var poz = Quaternion.Euler(0, katUst, 0) * Vector3.forward * promien;
                var korzen = anim[i].transform.parent;
                korzen.localPosition = new Vector3(poz.x, .02f, poz.z);
                korzen.localRotation = Quaternion.LookRotation(-poz.normalized, Vector3.up);
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

            kam.fieldOfView = walka ? 38f : 40f;
            kam.transform.position = new Vector3(2.9f * K, 1.5f * K, -3.4f * K);
            kam.transform.LookAt(new Vector3(0f, 1.15f * K, 0f));

            var para = new GameObject("Próba par").AddComponent<BraxProbaPary>();
            para.tryb = walka ? BraxProbaPary.Tryb.Walka : BraxProbaPary.Tryb.Rozmowa;
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
}

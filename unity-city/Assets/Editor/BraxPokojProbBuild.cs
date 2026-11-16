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

    static string[] PostacieZOtoczenia()
    {
        string v = Environment.GetEnvironmentVariable("BRAX_POSTACIE");
        if (string.IsNullOrWhiteSpace(v)) return new[] { "patryk" };
        return v.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToArray();
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

        // --- kamera -------------------------------------------------------
        var kam = new GameObject("Kamera próby").AddComponent<Camera>();
        kam.tag = "MainCamera";
        kam.gameObject.AddComponent<AudioListener>();
        kam.clearFlags = CameraClearFlags.SolidColor;
        kam.backgroundColor = new Color(.10f, .11f, .13f);
        kam.fieldOfView = 34f;
        kam.nearClipPlane = .15f;
        kam.transform.position = new Vector3(0f, 1.45f * K, -4.6f * K);
        kam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);

        var proba = new GameObject("Próba postaci").AddComponent<BraxProbaPostaci>();
        proba.kamera = kam;
        proba.postacie = anim.ToArray();
        proba.nazwy = nazwy.ToArray();
        proba.mimiki = mimiki.ToArray();

        const string path = "Assets/PokojProb.unity";
        if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException(path);
        var dest = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/pokoj/PokojProb.x86_64"));
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        var wynik = BuildPipeline.BuildPlayer(new[] { path }, dest, BuildTarget.StandaloneLinux64, BuildOptions.None);
        if (wynik.summary.result != BuildResult.Succeeded)
            throw new Exception(wynik.summary.result.ToString());
    }
}

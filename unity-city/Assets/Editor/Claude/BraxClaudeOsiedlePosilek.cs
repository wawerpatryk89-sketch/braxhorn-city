using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    [Serializable] sealed class ChwytJedzenia { public Vector3 srodek; public float promien; }

    static void DodajJedzenie(Animation ruch, string plik)
    {
        var chwyt = JsonUtility.FromJson<ChwytJedzenia>(File.ReadAllText(plik));
        var klip = ruch.GetClip("Eat");
        if (!klip || chwyt.promien <= 0f) throw new InvalidDataException(plik);
        klip.SampleAnimation(ruch.gameObject, 0f);
        var dlon = KoscObsady(ruch.transform,"RightHand");
        float ramie = Vector3.Distance(KoscObsady(ruch.transform,"RightArm").position,
            KoscObsady(ruch.transform,"RightForeArm").position);
        var owoc = Jablko(chwyt.promien*ramie,dlon.position+WektorChwytu(ruch.transform,"Right",chwyt.srodek)*ramie);
        owoc.transform.SetParent(dlon,true);
        var stan = owoc.AddComponent<BraxJedzenie>();
        stan.ruch = ruch;
        stan.widok = owoc.GetComponentsInChildren<Renderer>();
        foreach (var widok in stan.widok) widok.enabled = false;
        ruch.GetClip("Idle").SampleAnimation(ruch.gameObject,0f);
    }

    static GameObject Jablko(float promien, Vector3 srodek)
    {
        const string folder = "Assets/Claude/RekwizytyObsady/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder+"Jablko.obj");
        if (!prefab) throw new FileNotFoundException(folder+"Jablko.obj");
        var owoc = UnityEngine.Object.Instantiate(prefab);
        owoc.name = "Jabłko";
        var mesh = owoc.GetComponentInChildren<MeshFilter>().sharedMesh;
        float skala = promien*2f/mesh.bounds.size.x;
        owoc.transform.localScale *= skala;
        owoc.transform.position = srodek;
        foreach (var widok in owoc.GetComponentsInChildren<Renderer>())
            widok.sharedMaterial = Plain("Skórka jabłka",new Color(.57f,.072f,.039f),.29f);
        return owoc;
    }
}

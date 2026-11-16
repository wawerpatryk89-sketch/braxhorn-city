using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class BraxClaudeOsiedle
{
    [Serializable] sealed class OdstepPostaci
    {
        public string id;
        public Vector2 spoczynek, rozmowa, kierunek;
        public string bar;
        public float[] lewa, prawa;
    }
    [Serializable] sealed class OdstepyPostaci { public OdstepPostaci[] postacie; }
    static Dictionary<string, OdstepPostaci> odstepy;

    static void DopasujOdstepPrzyRozmowie(string id, Animation ruch, bool osiedle)
    {
        if (odstepy == null)
        {
            var dane = JsonUtility.FromJson<OdstepyPostaci>(File.ReadAllText("Assets/Editor/Claude/odstep-postaci.json"));
            odstepy = new Dictionary<string,OdstepPostaci>();
            foreach (var postac in dane.postacie) odstepy.Add(postac.id, postac);
        }
        if (!odstepy.TryGetValue(id,out var profil)) return;
        var stan = ruch.gameObject.AddComponent<BraxOdstepPrzyRozmowie>();
        stan.ruch = ruch;
        stan.lewyBark = KoscObsady(ruch.transform,"LeftArm");
        stan.prawyBark = KoscObsady(ruch.transform,"RightArm");
        stan.lewyLokiec = KoscObsady(ruch.transform,"LeftForeArm");
        stan.prawyLokiec = KoscObsady(ruch.transform,"RightForeArm");
        var bok = stan.lewyBark.position - stan.prawyBark.position;
        var palce = KoscObsady(ruch.transform,"LeftToeBase").position - KoscObsady(ruch.transform,"LeftFoot").position;
        stan.znakPrzodu = Mathf.Sign(Vector3.Dot(Vector3.Cross(bok, ruch.transform.up), palce));
        stan.spoczynek = profil.spoczynek;
        stan.rozmowa = profil.rozmowa;
        stan.kierunekRozmowy = profil.kierunek;
        if (osiedle && !string.IsNullOrEmpty(profil.bar))
        {
            stan.klipPrzyBarze = profil.bar;
            stan.lewaPrzyBarze = profil.lewa;
            stan.prawaPrzyBarze = profil.prawa;
        }
    }

    [Serializable] sealed class ChwytNapoju
    {
        public Vector3 dno, os;
        public float promien, wysokosc;
    }

    static void DodajSzklankeModelu(Animation ruch, string plik)
    {
        var profil = JsonUtility.FromJson<ChwytNapoju>(File.ReadAllText(plik));
        var klip = ruch.GetClip("Drink");
        if (!klip || profil.promien <= 0 || profil.wysokosc <= 0 || profil.os.sqrMagnitude < .5f)
            throw new InvalidDataException(plik);
        const string folder = "Assets/Claude/RekwizytyObsady/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Szklanka.obj");
        if (!model) throw new FileNotFoundException(folder + "Szklanka.obj");
        var mesh = model.GetComponentInChildren<MeshFilter>().sharedMesh;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(folder + "Szklo.mat");
        if (!mat)
        {
            mat = new Material(Shader.Find("Standard")) { color = new Color(.72f, .84f, .89f, .32f) };
            mat.SetFloat("_Mode", 3f);
            mat.SetFloat("_Glossiness", .92f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetOverrideTag("RenderType","Transparent");
            mat.renderQueue = 3000;
            AssetDatabase.CreateAsset(mat, folder + "Szklo.mat");
        }

        var napojMat = AssetDatabase.LoadAssetAtPath<Material>(folder + "Napoj.mat");
        if (!napojMat)
        {
            napojMat = new Material(Shader.Find("Standard")) { color = new Color(.55f,.22f,.045f) };
            napojMat.SetFloat("_Glossiness", .7f);
            AssetDatabase.CreateAsset(napojMat, folder + "Napoj.mat");
        }
        klip.SampleAnimation(ruch.gameObject, 0f);
        var dlon = KoscObsady(ruch.transform, "LeftHand");
        float dl = Vector3.Distance(KoscObsady(ruch.transform, "LeftArm").position,
            KoscObsady(ruch.transform, "LeftForeArm").position);
        var szklo = new GameObject("Szklanka w lewej dłoni");
        szklo.transform.position = dlon.position + WektorChwytu(ruch.transform, "Left", profil.dno) * dl;
        szklo.transform.rotation = Quaternion.FromToRotation(Vector3.up,
            WektorChwytu(ruch.transform, "Left", profil.os).normalized);
        // OBJ is centred on its axis. Position the bottom at the calibrated grip even after import scaling.
        var powierzchnia = new GameObject("Szkło");
        powierzchnia.transform.SetParent(szklo.transform, false);
        var rozmiar = mesh.bounds.size;
        var skala = new Vector3(2f*profil.promien*dl/rozmiar.x, profil.wysokosc*dl/rozmiar.y,
            2f*profil.promien*dl/rozmiar.z);
        powierzchnia.transform.localScale = skala;
        var podstawa = new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z);
        powierzchnia.transform.localPosition = -Vector3.Scale(podstawa, skala);
        powierzchnia.AddComponent<MeshFilter>().sharedMesh = mesh;
        var widok = powierzchnia.AddComponent<MeshRenderer>();
        widok.sharedMaterial = mat;
        widok.enabled = false;
        var ciecz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ciecz.name = "Napój";
        UnityEngine.Object.DestroyImmediate(ciecz.GetComponent<Collider>());
        ciecz.transform.SetParent(szklo.transform, false);
        float wysokosc = dl * profil.wysokosc;
        float srednica = 2f * profil.promien * dl * .88f;
        ciecz.transform.localScale = new Vector3(srednica, wysokosc*.25f, srednica);
        var plyn = ciecz.GetComponent<Renderer>();
        plyn.sharedMaterial = napojMat;
        plyn.enabled = false;
        szklo.transform.SetParent(dlon, true);
        var stan = szklo.AddComponent<BraxWidocznoscNapoju>();
        stan.ruch = ruch;
        stan.szklo = widok;
        stan.plyn = plyn;
        stan.wysokosc = wysokosc;
        ruch.GetClip("Idle").SampleAnimation(ruch.gameObject, 0f);
    }
}

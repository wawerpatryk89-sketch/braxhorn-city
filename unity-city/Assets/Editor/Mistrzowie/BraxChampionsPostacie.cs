using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Owner 12.11 01:59-02:33 (Tripo characters): "mają stać pod klubem ... między nimi chodzi. Pod każdym klubem" and "ma się
// ruszać po mieście ... chodziła, biegała, gesty miała". Global pack braxhorn-sklep-postacie-01 (postacie_01):
//  * five club girls (one Tripo rig cut into five static figures, real 1.71-1.75 m) stand in two groups on both sides of
//    the door of every club of the region - E1 CENTRUM ROZRYWKI and KASYNO WAWER T11 - so the player walks between them;
//  * four rigged walkers carry their own baked clips (unity-tools/sklep/postac_animacja.py: Walking, Running, Idle, Taunt,
//    Warming Up, Head Hit, Hit To Body on their own Mixamo bones) and walk the promenade with the city's pedestrian brain
//    (BraxPieszy: right side of the walkway, stops, gestures, greets, reacts to a hit and runs, turns at the ends), each
//    twice, in different lanes. Legacy Animation, nothing retargeted at runtime.
public static class BraxChampionsPostacie
{
    const string Paczka = "postacie_01", Out = "Assets/PlayableGenerated/Mistrzowie/Postacie";
    static float K => BraxClaudeOsiedle.K;
    static readonly string[] Klub = { "klub_czarna_sukienka", "klub_czerwona_sukienka", "klub_biala_sukienka", "klub_panterka", "klub_rozowa_sukienka" };
    static readonly string[] Chodzace = { "chodzi_czerwona_bluzka", "chodzi_niebieska_bluzka", "chodzi_czarne_bikini", "chodzi_biale_bikini" };
    static readonly float[] Wzrost = { 1.68f, 1.66f, 1.70f, 1.72f };

    [Serializable] sealed class Meta { public Speed speed_units_per_s; }
    [Serializable] sealed class Speed { public float Walking, Running; }

    public static int Generate()
    {
        if (!BraxMiastoAssety.Jest(Paczka)) { BraxBuildQuality.Warn("MISTRZOWIE_POSTACIE brak paczki " + Paczka + " (braxhorn-sklep-postacie-01)"); return 0; }
        Directory.CreateDirectory(Out);
        Physics.SyncTransforms();
        var root = new GameObject("MIASTO MISTRZÓW | postacie (pod klubami, na promenadzie)").transform;
        int stoja = 0, chodza = 0;
        // E1: door in the portal (local x 5 of the 530/-13 site), facade z -38, the forecourt to the road z -60
        stoja += PodKlubem(root, "E1 CENTRUM ROZRYWKI", new Vector3(535f, 0f, -38f), Vector3.back, 4.2f);
        // KASYNO WAWER T11: door on the axis of the 495/465 lot, face z 428.4, canopy and gold lanterns to z 425.3
        stoja += PodKlubem(root, "KASYNO WAWER", new Vector3(495f, 0f, 428.4f), Vector3.back, 4.6f);
        // the promenade walkway (z -145.5..-125, palms at -143), x 110..1090: four lanes, every walker twice
        float[] tory = { -128.6f, -131.8f, -135.0f, -138.2f };
        var rnd = new System.Random(1211);
        for (int k = 0; k < Chodzace.Length * 2; k++)
        {
            int m = k % Chodzace.Length;
            float z = tory[(k + k / Chodzace.Length) % tory.Length];
            float x0 = 110f + (float)rnd.NextDouble() * 90f, x1 = 1090f - (float)rnd.NextDouble() * 90f;
            var trasa = new List<Vector3>();
            for (int i = 0; i <= 12; i++) { float x = Mathf.Lerp(x0, x1, i / 12f); trasa.Add(Grunt(new Vector3(x, 0f, z + (i % 2 == 0 ? 0f : .6f)))); }
            if (Chodzaca(root, Chodzace[m], Wzrost[m] + (k >= Chodzace.Length ? .02f : 0f), trasa.ToArray(), rnd)) chodza++;
        }
        Debug.Log("MISTRZOWIE_POSTACIE pod klubami=" + stoja + " chodzące=" + chodza + " paczka=" + Paczka);
        return stoja + chodza;
    }

    // world point (nominal x,z) on the real ground: the first collider under it, else the paving height
    static Vector3 Grunt(Vector3 nominal)
    {
        var p = new Vector3(nominal.x, 0f, nominal.z) * K;
        if (Physics.Raycast(p + Vector3.up * 30f * K, Vector3.down, out var hit, 40f * K, ~0, QueryTriggerInteraction.Ignore)) return hit.point;
        return p + Vector3.up * .16f * K;
    }

    // two groups of the five, 3-4.6 m either side of the door axis, 3.6 m out from the facade, half-turned to the path
    static int PodKlubem(Transform root, string klub, Vector3 drzwi, Vector3 naZewnatrz, float odOsi)
    {
        var g = new GameObject(klub + " | dziewczyny przy wejściu").transform; g.SetParent(root, false);
        var bok = Vector3.Cross(Vector3.up, naZewnatrz).normalized;
        int n = 0;
        for (int i = 0; i < Klub.Length; i++)
        {
            var src = BraxMiastoAssety.Model(Paczka, Klub[i]); if (!src) continue;
            float s = i < 2 ? -1f : 1f; int w = i < 2 ? i : i - 2;
            var at = drzwi + naZewnatrz * (3.6f + w * .55f) + bok * s * (odOsi * .78f + w * .95f);
            // facing: toward the path, a little toward the street
            var f = (-bok * s + naZewnatrz * .8f).normalized;
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg + (w - 1) * 9f;
            var o = BraxMiastoAssety.Postaw(g, src, klub + " | " + Klub[i], Grunt(at), yaw, 1f, true, true);
            if (o) { Kolizja(o, 1.72f); n++; }
        }
        return n;
    }

    static void Kolizja(GameObject o, float h)
    {
        var c = o.AddComponent<CapsuleCollider>();
        c.direction = 1; c.height = h; c.radius = .24f; c.center = new Vector3(0f, h * .5f, 0f);   // local = metres (scale K on the object)
    }

    static bool Chodzaca(Transform root, string nazwa, float wzrost, Vector3[] trasa, System.Random rnd)
    {
        var src = BraxMiastoAssety.Model(Paczka, nazwa); if (!src) return false;
        string glb = BraxMiastoAssety.Baza + Paczka + "/" + nazwa + ".glb";
        var o = (GameObject)UnityEngine.Object.Instantiate(src, root, false);
        o.name = "Przechodzień (promenada) · " + nazwa;
        o.transform.position = trasa[0]; o.transform.rotation = Quaternion.identity; o.transform.localScale = Vector3.one;
        var skin = o.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.sharedMesh).OrderByDescending(r => r.sharedMesh.vertexCount).FirstOrDefault();
        if (!skin) { BraxBuildQuality.Warn("MISTRZOWIE_POSTACIE " + nazwa + ": brak siatki ze szkieletem"); UnityEngine.Object.DestroyImmediate(o); return false; }
        // height of the bind pose (T-pose) from the skinned vertices
        var v = skin.sharedMesh.vertices; var mtx = skin.transform.localToWorldMatrix; float y0 = float.MaxValue, y1 = float.MinValue;
        foreach (var p in v) { float y = mtx.MultiplyPoint3x4(p).y; y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
        float h0 = y1 - y0; if (!(h0 > .05f)) { UnityEngine.Object.DestroyImmediate(o); return false; }
        float s = wzrost / h0;                                   // model units -> metres
        o.transform.localScale = Vector3.one * (s * K);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(BraxMiastoAssety.MaterialMiasta).ToArray();
            r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            if (r is SkinnedMeshRenderer sr) { sr.quality = SkinQuality.Bone4; sr.updateWhenOffscreen = true; }   // bounds follow the pose: never culled mid-step (zero flicker)
        }
        foreach (var a in o.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
        // the baked clips of the GLB as Legacy clips on the object their curve paths start from
        var zrodla = AssetDatabase.LoadAllAssetsAtPath(glb).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToList();
        if (zrodla.Count == 0) { BraxBuildQuality.Warn("MISTRZOWIE_POSTACIE " + nazwa + ": GLB bez klipów (postac_animacja.py)"); UnityEngine.Object.DestroyImmediate(o); return false; }
        var nosnik = Nosnik(o.transform, zrodla[0]);
        foreach (var a in o.GetComponentsInChildren<Animation>(true)) if (a.gameObject != nosnik.gameObject) UnityEngine.Object.DestroyImmediate(a);
        var anim = nosnik.GetComponent<Animation>(); if (!anim) anim = nosnik.gameObject.AddComponent<Animation>();
        var nazwy = new Dictionary<string, string>();
        foreach (var c in zrodla)
        {
            var l = Legacy(c, nazwa);
            if (!anim.GetClip(l.name)) anim.AddClip(l, l.name);
            nazwy[Klucz(c.name)] = l.name;
        }
        string Kl(string n) => nazwy.TryGetValue(Klucz(n), out var x) ? x : null;
        string chod = Kl("Walking"), stoi = Kl("Idle") ?? chod, bieg = Kl("Running") ?? "";
        if (chod == null) { BraxBuildQuality.Warn("MISTRZOWIE_POSTACIE " + nazwa + ": brak klipu Walking"); UnityEngine.Object.DestroyImmediate(o); return false; }
        anim.clip = anim.GetClip(chod); anim.playAutomatically = true; anim.cullingType = AnimationCullingType.BasedOnRenderers;
        // speeds of the clips (character units / s, from the sidecar json) in world units
        float vWalk = 1.0f, vRun = 2.6f;
        var metaPath = BraxMiastoAssety.Baza + Paczka + "/" + nazwa + ".json";
        if (File.Exists(metaPath))
        {
            var meta = JsonUtility.FromJson<Meta>(File.ReadAllText(metaPath));
            if (meta != null && meta.speed_units_per_s != null) { if (meta.speed_units_per_s.Walking > .1f) vWalk = meta.speed_units_per_s.Walking; if (meta.speed_units_per_s.Running > .1f) vRun = meta.speed_units_per_s.Running; }
        }
        float world = s * K;
        var p2 = o.AddComponent<BraxPieszy>();
        p2.trasa = trasa; p2.przejscie = new bool[trasa.Length]; p2.a = trasa[0]; p2.b = trasa[trasa.Length - 1];
        p2.anim = anim; p2.chod = chod; p2.stoi = stoi; p2.bieg = bieg;
        p2.postoje = new[] { stoi, Kl("Warming Up") }.Where(x => x != null).ToArray();
        p2.mowi = new[] { Kl("Taunt"), stoi }.Where(x => x != null).ToArray(); p2.slucha = new[] { stoi };
        p2.reakcje = new[] { Kl("Head Hit"), Kl("Hit To Body") }.Where(x => x != null).ToArray();
        p2.predkoscKlipu = vWalk * world;
        p2.predkosc = p2.predkoscKlipu * Mathf.Lerp(.74f, .84f, (float)rnd.NextDouble());   // a stroll on the seafront
        p2.predkoscKlipuBiegu = vRun * world;
        p2.bok = Mathf.Lerp(.25f, .6f, (float)rnd.NextDouble());
        p2.glowa = BraxMiastoAssety.Znajdz(o.transform, "mixamorig:Head"); p2.szyja = BraxMiastoAssety.Znajdz(o.transform, "mixamorig:Neck");
        p2.kregoslup = BraxMiastoAssety.Znajdz(o.transform, "mixamorig:Spine1");
        p2.wzrost = wzrost * K; p2.ton = Mathf.Lerp(1.25f, 1.5f, (float)rnd.NextDouble());
        var cap = o.AddComponent<CapsuleCollider>(); cap.direction = 1; cap.height = h0; cap.radius = .24f / s; cap.center = new Vector3(0f, h0 * .5f, 0f);
        return true;
    }

    static string Klucz(string n) => new string((n ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    // the object every curve path of the clip starts from (glTFast: the scene root of the GLB)
    static Transform Nosnik(Transform o, AnimationClip c)
    {
        var b = AnimationUtility.GetCurveBindings(c).Select(x => x.path).FirstOrDefault(x => !string.IsNullOrEmpty(x));
        if (b == null) return o;
        foreach (var t in o.GetComponentsInChildren<Transform>(true)) if (t.Find(b)) return t;
        return o;
    }

    static AnimationClip Legacy(AnimationClip src, string model)
    {
        string path = Out + "/" + model + "_" + Klucz(src.name) + ".anim";
        var stary = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (stary) return stary;
        var c = new AnimationClip { name = src.name, legacy = true, frameRate = src.frameRate };
        foreach (var b in AnimationUtility.GetCurveBindings(src)) AnimationUtility.SetEditorCurve(c, b, AnimationUtility.GetEditorCurve(src, b));
        c.wrapMode = Klucz(src.name) == "headhit" || Klucz(src.name) == "hittobody" ? WrapMode.Once : WrapMode.Loop;
        c.EnsureQuaternionContinuity();
        BraxZapisAssetu.Utworz(c, path);
        return c;
    }
}

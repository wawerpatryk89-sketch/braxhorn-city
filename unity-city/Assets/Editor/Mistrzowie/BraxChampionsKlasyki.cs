using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// THE OWNER'S POLISH CLASSICS (owner 24.11.2026 22:21-23:11, three zips from Tripo: "Samochody i motory. Klasyki ... koła
// zrobić, żeby się kręciły. Szyby trzeba powymieniać ... napisy poprawić"; "wszystko odrestaurować co trzeba"). The pack
// pojazdy_klasyki_01 (unity-tools/mistrzowie/klasyki/przygotuj_klasyki.py, encrypted: the repository is public) holds each
// vehicle cut and restored: the body with its two far LODs, every wheel its own part pivoted at its hub, the glass (painted
// windows as the tinted glass part; open windows given new panes), the colour / normal / metal-roughness maps. Here:
//   - a car or a truck stands as a BraxChampionsCar (AKCJA "WSIĄDŹ", the joystick, the handbrake): its four wheels roll,
//     the front two steer (a steering pivot over each rolling hub), its real wheelbase, its driver on the left;
//   - a motorcycle stands on its stand (its wheels cut for the riding to come);
//   - the body in the city's PBR (CityStaticPBR, both sides: Tripo's thin shells), the glass in the region's car glass.
// Nothing without the pack: the place stays empty and the log says so (no KLUCZ in a fork's CI).
public static class BraxChampionsKlasyki
{
    public const string Paczka = "pojazdy_klasyki_01";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/Klasyki";
    static float K => BraxClaudeOsiedle.K;

    [Serializable] sealed class Lod { public string plik; public int trojkaty; }
    [Serializable] sealed class Czesc { public string nazwa, plik, material; public float[] pivot; public int trojkaty; public float promien, szerokosc; public Lod[] lody; }
    [Serializable] sealed class Mapy { public string kolor, normal, mr; }
    [Serializable] sealed class Szyby { public string plik, rodzaj, otwarte; }
    [Serializable] sealed class Znaczek { public string znaczek, plik; public float[] pos, rot; }
    [Serializable] sealed class ZnaczekWzor { public string znaczek, plik, kolor, normal, mr; public float szerokosc; public int trojkaty; }
    [Serializable] sealed class Model
    {
        public string model, nazwa, typ, wzor, kolor, normal, mr, swiatla; public Czesc[] czesci; public Szyby szyby; public Znaczek[] znaczki;
        public float dlugosc, szerokosc, wysokosc, rozstaw_osi;
    }
    [Serializable] sealed class Katalog { public string zrodlo, osie; public Model[] modele; public ZnaczekWzor[] znaczki; public Mapy kola; }

    public static bool Jest => BraxMiastoAssety.Jest(Paczka);
    public static int Postawione, Jezdne, Trojkaty;
    static Katalog kat;
    static Katalog Kat()
    {
        if (kat != null) return kat;
        string p = BraxMiastoAssety.Baza + Paczka + "/katalog.json";
        if (!File.Exists(p)) return null;
        kat = JsonUtility.FromJson<Katalog>(File.ReadAllText(p));
        return kat;
    }
    static Model M(string model) => Kat()?.modele?.FirstOrDefault(x => x.model == model);

    /// <summary>The vehicle's size in real metres (length, width) - the bunker's bays are laid by it; zero without the pack.</summary>
    public static Vector2 Wymiar(string model) { var m = M(model); return m == null ? Vector2.zero : new Vector2(m.dlugosc, m.szerokosc); }
    public static string Typ(string model) => M(model)?.typ ?? "";

    // ---------------------------------------------------------------- materials
    static readonly Dictionary<string, Material> materialy = new Dictionary<string, Material>();
    static Texture2D Tekstura(string nazwa)
    {
        string p = BraxMiastoAssety.Baza + Paczka + "/_tekstury/" + nazwa + ".jpg";
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    static Material Zapisz(Material mat, string plik)
    {
        Directory.CreateDirectory(Out + "/Materials");
        string path = Out + "/Materials/" + plik + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
    // the city's PBR: the restored colour map, Tripo's normal map (RGB, glTF's +Y), its metallic-roughness map as ARM (R 1:
    // no AO map; G roughness; B metal; the glass' texels smooth and not metal); both sides (Tripo's thin shells)
    static Material Nadwozie(Model m)
    {
        if (materialy.TryGetValue(m.model, out var mat) && mat) return mat;
        var kolor = Tekstura(m.kolor); var nrm = Tekstura(m.normal); var arm = Tekstura(m.mr);
        if (!kolor || !nrm || !arm) throw new InvalidOperationException("classic's maps missing: " + m.model);
        mat = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Mistrzowie klasyk " + m.model, enableInstancing = true };
        mat.SetTexture("_MainTex", kolor); mat.SetTexture("_BumpMap", nrm); mat.SetTexture("_ARM", arm);
        mat.SetFloat("_NormalEncoding", 0f); mat.SetFloat("_Roughness", 1f); mat.SetFloat("_Metallic", 1f); mat.SetFloat("_UseARMForAO", 0f);
        mat.SetFloat("_VertexTint", 0f); mat.SetFloat("_Cull", 0f); mat.SetColor("_Color", Color.white);
        mat.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        // its lamps for the night: the light map as the emission, black by day (the shader skips it) - lit by
        // BraxChampionsSwiatlaKlasykow.Noc when the game's night comes
        var swiatla = string.IsNullOrEmpty(m.swiatla) ? null : Tekstura(m.swiatla);
        if (swiatla) { mat.SetTexture("_EmissionMap", swiatla); mat.SetColor("_EmissionColor", Color.black); }
        return materialy[m.model] = Zapisz(mat, "klasyk_" + m.model);
    }
    // the owner's 3D badges (FSO, the Trabant's S, POLSKI FIAT, FSO POLONEZ): each its own maps, glossy, chrome where it
    // is chrome (metal kept at 0.6: the city's shade would turn a full metal black)
    static readonly Dictionary<string, Material> znaczkiMat = new Dictionary<string, Material>();
    static Material ZnaczekMat(ZnaczekWzor w)
    {
        if (znaczkiMat.TryGetValue(w.znaczek, out var mat) && mat) return mat;
        var kolor = Tekstura(w.kolor); var nrm = Tekstura(w.normal); var arm = Tekstura(w.mr);
        if (!kolor || !nrm || !arm) throw new InvalidOperationException("badge's maps missing: " + w.znaczek);
        mat = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Mistrzowie znaczek " + w.znaczek, enableInstancing = true };
        mat.SetTexture("_MainTex", kolor); mat.SetTexture("_BumpMap", nrm); mat.SetTexture("_ARM", arm);
        mat.SetFloat("_NormalEncoding", 0f); mat.SetFloat("_Roughness", 1f); mat.SetFloat("_Metallic", 1f); mat.SetFloat("_UseARMForAO", 0f);
        mat.SetFloat("_VertexTint", 0f); mat.SetFloat("_Cull", 0f); mat.SetColor("_Color", Color.white);
        mat.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        return znaczkiMat[w.znaczek] = Zapisz(mat, "znaczek_" + w.znaczek);
    }
    static void Znaczki(Transform nadwozie, Model m)
    {
        if (m.znaczki == null) return;
        foreach (var z in m.znaczki)
        {
            var w = Kat()?.znaczki?.FirstOrDefault(x => x.znaczek == z.znaczek);
            if (w == null || z.pos == null || z.rot == null || z.pos.Length < 3 || z.rot.Length < 4) continue;
            var o = new GameObject("znaczek " + z.znaczek); o.transform.SetParent(nadwozie, false);
            o.transform.localPosition = V(z.pos) * K; o.transform.localRotation = new Quaternion(z.rot[0], z.rot[1], z.rot[2], z.rot[3]);
            o.transform.localScale = Vector3.one * K;
            o.AddComponent<MeshFilter>().sharedMesh = Siatka(w.plik);
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = ZnaczekMat(w); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            // a few centimetres: drawn only near (2 % of the screen's height)
            var lg = o.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(.02f, new Renderer[] { r }) }); lg.RecalculateBounds();
            Trojkaty += w.trojkaty;
        }
    }

    // the new wheels (a rubber tyre, the car's kind of rim: one atlas for all) - their own material
    static Material kolaMat;
    static Material KolaMat()
    {
        if (kolaMat) return kolaMat;
        var k = Kat()?.kola; if (k == null) return null;
        var kolor = Tekstura(k.kolor); var nrm = Tekstura(k.normal); var arm = Tekstura(k.mr);
        if (!kolor || !nrm || !arm) throw new InvalidOperationException("the wheels' maps missing");
        var mat = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Mistrzowie klasyki koła", enableInstancing = true };
        mat.SetTexture("_MainTex", kolor); mat.SetTexture("_BumpMap", nrm); mat.SetTexture("_ARM", arm);
        mat.SetFloat("_NormalEncoding", 0f); mat.SetFloat("_Roughness", 1f); mat.SetFloat("_Metallic", 1f); mat.SetFloat("_UseARMForAO", 0f);
        mat.SetFloat("_VertexTint", 0f); mat.SetFloat("_Cull", 0f); mat.SetColor("_Color", Color.white);
        mat.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        return kolaMat = Zapisz(mat, "klasyki_kola");
    }

    static Material szybaCiemna, szybaJasna;
    // the painted windows (no inside behind them): dark tinted glass, glossy, opaque; the new panes in open windows: the
    // period's faintly green glass, see-through
    static Material SzybaCiemna => szybaCiemna ? szybaCiemna : szybaCiemna = BraxChampionsHarbour.Surface("klasyki_szyba_przyciemniana", new Color(.10f, .125f, .15f), .05f, 0f);
    static Material SzybaJasna => szybaJasna ? szybaJasna : szybaJasna = BraxChampionsHarbour.Surface("klasyki_szyba", new Color(.50f, .60f, .58f, .26f), .04f, .05f, null, true);

    // a part's mesh from the pack's .glb (glTFast), with tangents (the normal map needs them; a copy if the import has none)
    static readonly Dictionary<string, Mesh> siatki = new Dictionary<string, Mesh>();
    static Mesh Siatka(string plik)
    {
        if (siatki.TryGetValue(plik, out var s) && s) return s;
        var g = BraxMiastoAssety.Model(Paczka, plik);
        var mf = g ? g.GetComponentInChildren<MeshFilter>(true) : null;
        if (!mf || !mf.sharedMesh) throw new InvalidOperationException("classic's part missing: " + plik);
        s = mf.sharedMesh;
        if (s.tangents == null || s.tangents.Length != s.vertexCount)
        {
            Directory.CreateDirectory(Folder);
            var kopia = UnityEngine.Object.Instantiate(s); kopia.name = plik; kopia.RecalculateTangents();
            string path = Folder + "/" + plik + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(kopia, path); s = kopia;
        }
        return siatki[plik] = s;
    }
    static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;

    // a part (its LOD0 and its far ones) under t, the meshes in real metres x K; the glass rides the body's near LODs
    static int Czesci(Transform t, Czesc c, Material mat, string nazwa, List<Renderer> szklo = null)
    {
        var lody = new List<LOD>(); int tri = 0;
        var pliki = new List<string> { c.plik }; if (c.lody != null) pliki.AddRange(c.lody.Select(l => l.plik));
        float[] progi = { .22f, .07f, .012f };                         // the share of the screen's height each LOD holds to
        for (int i = 0; i < pliki.Count; i++)
        {
            var o = new GameObject(nazwa + (i == 0 ? "" : " LOD" + i)); o.transform.SetParent(t, false); o.transform.localScale = Vector3.one * K;
            var mesh = Siatka(pliki[i]); o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = i == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            var rs = new List<Renderer> { r }; if (szklo != null && i < 2) rs.AddRange(szklo);
            lody.Add(new LOD(progi[Mathf.Min(i, progi.Length - 1)], rs.ToArray()));
            if (i == 0) tri += mesh.triangles.Length / 3;
        }
        if (lody.Count > 1) { var lg = t.gameObject.AddComponent<LODGroup>(); lg.SetLODs(lody.ToArray()); lg.RecalculateBounds(); }
        return tri;
    }
    static Renderer Szklo(Transform t, string plik, Material mat, string nazwa)
    {
        var o = new GameObject(nazwa); o.transform.SetParent(t, false); o.transform.localScale = Vector3.one * K;
        o.AddComponent<MeshFilter>().sharedMesh = Siatka(plik);
        var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        Trojkaty += r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
        return r;
    }

    // the top speeds of the time (km/h) - the game's pace kept, the maluch still the slowest car
    static float VMax(Model m) =>
        m.typ == "ciezarowka" ? 85f : m.model.StartsWith("maluch") ? (m.model.Contains("tuning") ? 140f : 115f)
        : m.model.StartsWith("trabant") ? (m.model.Contains("tuning") ? 130f : 105f)
        : m.model == "warszawa_m20" ? 105f : m.model == "nysa_bus" || m.model == "zuk_bus" ? 100f
        : m.model.Contains("tuning") ? 190f : m.model.StartsWith("syrena") || m.model == "nysa_milicja" ? 115f : 150f;

    /// <summary>A classic at p (nominal metres; its wheels on p's height), facing yaw; a car or a truck driveable. Null
    /// without the pack.</summary>
    public static GameObject Postaw(Transform parent, string model, Vector3 p, float yaw, bool jezdny = true)
    {
        if (!Jest) { BraxMiastoAssety.Log("klasyk " + model + ": brak paczki " + Paczka + " (KLUCZ w CI) - miejsce zostaje puste"); return null; }
        var m = M(model);
        if (m == null) throw new InvalidOperationException("classic not in the pack's catalogue: " + model);
        var mat = Nadwozie(m);
        var root = new GameObject("KLASYK | " + m.nazwa + " | " + m.wzor);
        root.transform.SetParent(parent, false); root.transform.SetPositionAndRotation(p * K, Quaternion.Euler(0f, yaw, 0f));
        bool motor = m.typ == "motor";
        var toczace = new List<Transform>(); var skretne = new List<Transform>();
        int tri = 0; Czesc korpus = null;
        foreach (var c in m.czesci)
        {
            if (c.nazwa.StartsWith("kolo"))
            {
                // the front wheels of a car steer: a steering pivot at the hub, the wheel rolling inside it
                bool przod = c.nazwa.Contains("przod");
                var piasta = new GameObject("piasta " + c.nazwa).transform; piasta.SetParent(root.transform, false);
                piasta.localPosition = V(c.pivot) * K;
                var kolo = piasta;
                if (przod && !motor) { kolo = new GameObject("koło " + c.nazwa).transform; kolo.SetParent(piasta, false); skretne.Add(piasta); }
                toczace.Add(kolo);
                tri += Czesci(kolo, c, c.material == "kola" && KolaMat() ? KolaMat() : mat, c.plik);
            }
            else if (c.nazwa == "korpus") korpus = c;
        }
        if (korpus == null) throw new InvalidOperationException("classic without its body: " + model);
        var nadwozie = new GameObject("nadwozie").transform; nadwozie.SetParent(root.transform, false);
        var szklo = new List<Renderer>();
        if (m.szyby != null && !string.IsNullOrEmpty(m.szyby.plik)) szklo.Add(Szklo(nadwozie, m.szyby.plik, SzybaCiemna, "szyby (przyciemniane)"));
        if (m.szyby != null && !string.IsNullOrEmpty(m.szyby.otwarte)) szklo.Add(Szklo(nadwozie, m.szyby.otwarte, SzybaJasna, "szyby (nowe, w otwartych oknach)"));
        tri += Czesci(nadwozie, korpus, mat, korpus.plik, szklo);
        Znaczki(nadwozie, m);
        if (mat.GetTexture("_EmissionMap"))
        {
            var sw = root.AddComponent<BraxChampionsSwiatlaKlasykow>();
            sw.rendery = root.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterial == mat).ToArray();
        }
        Trojkaty += tri; Postawione++;
        // its box (glTFast's x is negated: the vehicle's left is Unity's -x)
        var srodek = new Vector3(0f, m.wysokosc * .5f, 0f) * K; var rozmiar = new Vector3(m.szerokosc, m.wysokosc, m.dlugosc) * K;
        if (motor || !jezdny)
        {
            var kol = new GameObject("kolizja").AddComponent<BoxCollider>(); kol.transform.SetParent(root.transform, false);
            kol.center = srodek; kol.size = Vector3.Scale(rozmiar, new Vector3(motor ? .45f : 1f, 1f, 1f));
            Debug.Log("MISTRZOWIE_KLASYK " + m.nazwa + " (" + m.wzor + ") tri=" + tri + " stoi" + (motor ? " na stopce" : ""));
            return root;
        }
        var box = root.AddComponent<BoxCollider>(); box.center = srodek; box.size = rozmiar;
        var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false; rb.interpolation = RigidbodyInterpolation.Interpolate;
        var seat = new GameObject("fotel kierowcy (lewy)").transform; seat.SetParent(root.transform, false);
        seat.localPosition = new Vector3(-m.szerokosc * .5f * .39f, Mathf.Clamp(m.wysokosc * .30f, .26f, 1.2f), m.dlugosc * .5f * .14f) * K;
        var enter = new GameObject("podejście do drzwi kierowcy").transform; enter.SetParent(root.transform, false);
        enter.localPosition = new Vector3(-m.szerokosc * .5f - .78f, .04f, seat.localPosition.z / K - .15f) * K;
        var car = root.AddComponent<BraxChampionsCar>();
        car.bodyCollider = box; car.driverSeat = seat; car.doorApproach = enter;
        car.doorHinge = null; car.doorAxis = Vector3.left;                       // the doors are the body's: none invented
        car.wheelbase = (m.rozstaw_osi > .5f ? m.rozstaw_osi : 2.5f) * K;
        car.worldScale = K;
        float v = VMax(m);
        car.maxSpeed = v / 3.6f * K; car.reverseMaxSpeed = 25f / 3.6f * K;
        car.acceleration = (m.typ == "ciezarowka" ? 3.2f : m.model.Contains("tuning") ? 8.5f : 5.5f) * K;
        car.braking = (m.typ == "ciezarowka" ? 7f : 10f) * K; car.coastDecel = 4.5f * K;
        car.interactionRadius = 3.8f * K;
        var audio = new GameObject("BRAXHORN engine audio 3D anchor").transform; audio.SetParent(root.transform, false);
        audio.localPosition = new Vector3(0f, m.wysokosc * .4f, m.dlugosc * (m.model.StartsWith("maluch") ? -.36f : .32f)) * K;   // the maluch's engine at its back
        car.engineAudioAnchor = audio;
        car.engineIdleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineIdleAudio);
        car.engineRevClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineRevAudio);
        car.engineHighClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BraxChampionsCarBuilder.EngineHighAudio);
        car.rollingWheels = toczace.ToArray(); car.steeringWheels = skretne.ToArray();
        Jezdne++;
        Debug.Log("MISTRZOWIE_KLASYK " + m.nazwa + " (" + m.wzor + ") tri=" + tri + " jeździ: " + v + " km/h, rozstaw osi " + m.rozstaw_osi.ToString("F2") +
                  " m, koła " + toczace.Count + " (skręcają " + skretne.Count + ")");
        return root;
    }
}

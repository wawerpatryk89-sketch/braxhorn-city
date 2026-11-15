using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BraxClaudeOsiedle
{
    static Transform KoscObsady(Transform root, string nazwa) =>
        root.GetComponentsInChildren<Transform>(true).First(t => t.name.EndsWith(nazwa, StringComparison.Ordinal));

    static Animation ModelObsady(string id, Transform rodzic, float wzrost)
    {
        string folder = BraxObsadaImport.Katalog + id + "/";
        string path = folder + id + ".fbx";
        var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!wzor) throw new InvalidOperationException("Brak modelu obsady " + path);
        var model = Object.Instantiate(wzor, rodzic, false);
        model.name = id == "patryk" ? "Patryk" : id;
        foreach (var a in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
        foreach (var a in model.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
        var anim = model.AddComponent<Animation>();
        anim.playAutomatically = false;
        anim.cullingType = id == "patryk" ? AnimationCullingType.AlwaysAnimate : AnimationCullingType.BasedOnRenderers;
        foreach (var klip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
        {
            if (klip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
            string nazwa = klip.name.Split('|').Last();
            if (!klip.legacy || anim.GetClip(nazwa)) throw new InvalidOperationException(id + " wadliwy klip " + nazwa);
            var stan = klip;
            if (klip.name != nazwa)
            {
                // Animation zapisuje w scenie referencje do klipów. Zachowaj nazwę stanu po ponownym wczytaniu.
                string cel = folder + nazwa + ".anim";
                stan = AssetDatabase.LoadAssetAtPath<AnimationClip>(cel);
                if (!stan) { stan = Object.Instantiate(klip); stan.name = nazwa; AssetDatabase.CreateAsset(stan, cel); }
                else { EditorUtility.CopySerialized(klip, stan); stan.name = nazwa; EditorUtility.SetDirty(stan); }
            }
            anim.AddClip(stan, nazwa);
        }
        if (!anim.GetClip("Idle") || !anim.GetClip("Walk")) throw new InvalidOperationException(id + " brak Idle/Walk");
        var renders = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renders.Length == 0) throw new InvalidOperationException(id + " bez siatki");
        var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "Postac.mat");
        if (!material)
        {
            material = new Material(Shader.Find("Standard"));
            Texture2D Tekstura(string plik) => AssetDatabase.LoadAssetAtPath<Texture2D>(folder + plik + ".png") ?? throw new InvalidOperationException(id + " brak " + plik);
            material.mainTexture = Tekstura("Color");
            material.SetTexture("_BumpMap", Tekstura("Normal"));
            material.SetTexture("_MetallicGlossMap", Tekstura("MetalSmooth"));
            material.SetFloat("_GlossMapScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            AssetDatabase.CreateAsset(material, folder + "Postac.mat");
        }
        float dol = float.PositiveInfinity, gora = float.NegativeInfinity;
        foreach (var r in renders)
        {
            var mesh = new Mesh();
            r.BakeMesh(mesh);
            foreach (var p in mesh.vertices)
            {
                float y = rodzic.InverseTransformPoint(r.transform.TransformPoint(p)).y;
                dol = Mathf.Min(dol, y); gora = Mathf.Max(gora, y);
            }
            Object.DestroyImmediate(mesh);
            r.quality = SkinQuality.Bone4;
            r.sharedMaterials = Enumerable.Repeat(material, r.sharedMesh.subMeshCount).ToArray();
        }
        if (!(gora > dol + .05f)) throw new InvalidOperationException(id + " pusty model");
        float skala = wzrost / (gora - dol);
        model.transform.localScale *= skala;
        model.transform.localPosition -= Vector3.up * dol * skala;
        // Kierunek palców stóp ustala przód modelu również przy różnych osiach pliku FBX.
        var stopy = model.GetComponentsInChildren<Transform>(true);
        var palec = stopy.FirstOrDefault(t => t.name.EndsWith("LeftToeBase", StringComparison.Ordinal));
        if (palec)
        {
            var stopa = KoscObsady(model.transform, "LeftFoot");
            var v = Vector3.ProjectOnPlane(rodzic.InverseTransformVector(palec.position - stopa.position), Vector3.up);
            if (v.sqrMagnitude > .00001f && v.z < 0) model.transform.localRotation = Quaternion.Euler(0,180,0) * model.transform.localRotation;
        }
        anim.clip = anim.GetClip("Idle");
        return anim;
    }

    public static GameObject NowyPatrykOsiedla(Transform gracz, CharacterController kapsula)
    {
        var a = ModelObsady("patryk", gracz, WzrostPatrykaWGrze);
        foreach (var t in a.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
        WzrostZModelu = WzrostPatrykaWGrze;
        kapsula.height = WzrostZModelu;
        kapsula.center = Vector3.up * (kapsula.height * .5f);
        return a.gameObject;
    }

    static void UstawObsadeOsiedla()
    {
        var gracz = Object.FindFirstObjectByType<BraxGymPlayer>().transform;
        var rodzic = Group("Obsada Osiedla");
        BraxZnajomyPatryka Osoba(string id, Vector3 p, Vector3 przod, string animacja, float wys, bool kobieta, int rozmowa,
            string powitanie, string temat, string odpowiedz, string koniec)
        {
            var korzen = new GameObject(id);
            korzen.transform.SetParent(rodzic, false);
            korzen.transform.SetPositionAndRotation(p, Quaternion.LookRotation(przod));
            var a = korzen.AddComponent<BraxZnajomyPatryka>();
            a.postac = id; a.gracz = gracz; a.zajecie = animacja; a.kobieta = kobieta;
            a.powitanie = powitanie; a.temat = temat; a.odpowiedz = odpowiedz; a.zakonczenie = koniec;
            a.drugaRozmowa = rozmowa;
            a.ruch = ModelObsady(id, a.transform, wys * K);
            a.glowa = KoscObsady(a.ruch.transform, "Head");
            var klip = a.ruch.GetClip(animacja);
            if (!klip) throw new InvalidOperationException(id + " brak animacji " + animacja);
            klip.SampleAnimation(a.ruch.gameObject, 0);
            if (animacja == "SeatedIdle")
            {
                var biodra = KoscObsady(a.ruch.transform, "Hips");
                var przesuniecie = p + Vector3.up * (.10f * K) - biodra.position;
                a.ruch.transform.position += przesuniecie;
            }
            return a;
        }

        var f = Vector3.forward;
        Vector3 P(float x, float z, float y = .10f) => new Vector3(x, y, z);
        string[] w(int i) => BraxDymki.Wymiany[i];
        var ewa = Osoba("ewa", P(-20,58), f, "Walk", 1.67f, true, 11, "Cześć, Patryk!", "Trening dziś był?", "No, trening był ciężki.", "To pij dużo wody.");
        var martyna = Osoba("martyna", P(-15,63), f, "Walk", 1.68f, true, 13, "Cześć, co słychać?", w(11)[0], w(11)[1], w(11)[2]);
        var wiechu = Osoba("wiechu", P(-23,59.6f), f, "Walk", 1.78f, false, 5, "O, Patryk! Siema!", "Grill w sobotę? Zapraszam.", "Dzięki, wpadnę.", "To do zobaczenia!");
        ewa.spacer = martyna.spacer = wiechu.spacer = true;
        ewa.celSpaceru = P(-17.4f,58); martyna.celSpaceru = P(-15,67); wiechu.celSpaceru = P(-19.5f,59.6f);

        Osoba("zadymiarz_021fef69", P(89.5f,30.1f), f, "Idle", 1.87f, false, 14, "Siemanko!", "Wszystko w porządku?", "Spoko, ogarnę to.", "Dobry wieczór. Zapraszam do środka.");
        Osoba("kiler", P(82.6f,30.1f), f, "Idle", 1.84f, false, 1, "Cześć, mistrzu!", "Pamiętaj o gardzie, Patryk.", "Dobra, pamiętam.", "Do zobaczenia na gali!");
        var mateusz = Osoba("mateusz", P(79.5f,32.8f), Vector3.right, "Talk", 1.79f, false, 7, "O, Patryk! Siema!", w(2)[0], w(2)[1], w(2)[2]);
        mateusz.partner = Osoba("dziecko_003f4e46", P(81.1f,32.8f), Vector3.left, "Talk", 1.36f, false, 10, "Hej!", w(12)[0], w(12)[1], w(12)[2]);
        if (klub == null || !klubBar || klub.sofy.Count < 3) throw new InvalidOperationException("Brak stanowisk w Club Puls");
        Osoba("kamil", klub.dj, Vector3.left, "Talk", 1.80f, false, 2, "Elo! Co tam?", "W klubie w piątek gra nowy DJ. Ma być tłum.", "Brzmi nieźle.", "Wpadaj, kiedy chcesz!");
        var barman = Osoba("bramkarz_1c96d333", klub.barmani[1], f, "Idle", 1.83f, false, 6, "Cześć, Patryk!", "Coś jeszcze?", "Dzięki, trzymaj się.", "Wpadaj, kiedy chcesz!");
        var st = klubBar.stanowiska[1];
        klubBar.odpowiedzi = new[] {"Zaraz podam, chwileczkę.", "Proszę, smacznego!", "Coś jeszcze?"};
        st.barman = barman.transform;
        st.ramie = KoscObsady(barman.transform, "RightArm"); st.dlon = KoscObsady(barman.transform, "RightHand");
        var bar = klub.barStoja.Last();
        var gitara = Osoba("gitara", bar.p, bar.przod, "Drink", 1.80f, false, 9, "Siema, wszystko gra?", w(5)[0], w(5)[1], w(5)[2]);
        var szklanka = Object.Instantiate(st.szklanka.gameObject);
        szklanka.name = "Szklanka Gitary";
        var dlonGitary = KoscObsady(gitara.transform, "LeftHand");
        var palce = gitara.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("LeftHandMiddle1", StringComparison.Ordinal));
        szklanka.transform.position = (palce ? Vector3.Lerp(dlonGitary.position, palce.position, .8f) : dlonGitary.position) - Vector3.up * .035f * K;
        szklanka.transform.rotation = Quaternion.identity;
        szklanka.transform.SetParent(dlonGitary, true);
        foreach (var collider in szklanka.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);

        string[] taniec = {"aneta","monika","imprezowiczka_11baefe5","imprezowiczka_13e8d41d","imprezowiczka_0dbe3922"};
        var miejsca = new[] {P(89,9,.11f),P(93,11,.11f),P(96,14,.11f),P(90,16,.11f),P(95,7.5f,.11f)};
        int[] tematy = {1,3,10,6,11};
        for (int i = 0; i < taniec.Length; i++)
        {
            var t = w(tematy[i]);
            Osoba(taniec[i], miejsca[i], Vector3.right, i%2 == 0 ? "DanceHouse" : "DanceHipHop", 1.68f, true, (tematy[i]+4)%15,
                  BraxDymki.Mijanie[new[] {6,11,10,15,5}[i]], t[0], t[1], t[2]);
        }
        string[] loza = {"kapitan_02d85d24","plazowicz_0493f781","zadymiarz_163dca80"};
        int[] tematyLozy = {9,13,14};
        for (int i = 0; i < loza.Length; i++)
        {
            var miejsce = klub.sofy[(i * klub.sofy.Count) / loza.Length];
            var t = w(tematyLozy[i]);
            Osoba(loza[i], miejsce.p, miejsce.przod, "SeatedIdle", 1.80f, false, i+2,
                  BraxDymki.Mijanie[new[] {19,6,11}[i]], t[0], t[1], t[2]);
        }
    }

    public static void SprawdzNowaObsade(BraxGymPlayer gracz)
    {
        if (rep == null || rep.status != "ZBUDOWANO")
            throw new InvalidOperationException("Generator nie ukończył Osiedla: " + rep?.error);
        var osoby = Object.FindObjectsByType<BraxZnajomyPatryka>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (!gracz || !gracz.clips || osoby.Any(o => !o.ruch || !o.glowa || !o.ruch.GetClip("Talk")))
            throw new InvalidOperationException("Niekompletne powiązania postaci lub rozmów");
        int klipy = gracz.clips.GetClipCount() + osoby.Sum(o => o.ruch.GetClipCount());
        if (osoby.Length != 18 || osoby.Select(x => x.postac).Distinct().Count() != 18 || klipy != 375 ||
            gracz.clips.GetClipCount() != 50 || osoby.Any(o => o.gracz != gracz.transform || !o.ruch.GetClip(o.zajecie)))
            throw new InvalidOperationException("Obsada Osiedla: " + osoby.Length + " NPC, klipy=" + klipy);
        if (Object.FindObjectsByType<BraxMieszkaniec>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0 ||
            Object.FindObjectsByType<BraxPieszy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Pozostała tymczasowa postać");
        var ruch = Object.FindFirstObjectByType<BraxRuchAut>();
        int autobusy = Object.FindObjectsByType<BraxAutobus>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        if (!ruch || ruch.auta == null || ruch.auta.Length != 4 || ruch.auta.Length + autobusy > 6)
            throw new InvalidOperationException("Niepoprawna liczba pojazdów w ruchu");
        Debug.Log("BRAXHORN_RUCH samochody=" + ruch.auta.Length + " autobusy=" + autobusy);
        Debug.Log("BRAXHORN_OBSADA postacie=19 sterowanych=1 znajomych=18 animacje=" + klipy);
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BraxClaudeOsiedle
{
    static Transform KoscObsady(Transform root, string nazwa) =>
        root.GetComponentsInChildren<Transform>(true).First(t => t.name.EndsWith(nazwa, StringComparison.Ordinal));

    // Three anatomical directions at the reference grip, including the thumb side, work on mirrored avatars.
    [Serializable] sealed class ProbkiChwytu { public Vector3[] przesuniecia; }

    static Vector3 WektorChwytu(Transform root, string bok, Vector3 skladniki)
    {
        var dlon = KoscObsady(root,bok+"Hand");
        var srodek = KoscObsady(root,bok+"HandMiddle1").position;
        var wzdluz = (srodek-dlon.position).normalized;
        var wszerz = Vector3.ProjectOnPlane(KoscObsady(root,bok+"HandIndex1").position
            - KoscObsady(root,bok+"HandPinky1").position,wzdluz).normalized;
        var stronaKciuka = Vector3.Cross(wzdluz,wszerz).normalized;
        if (Vector3.Dot(stronaKciuka,KoscObsady(root,bok+"HandThumb2").position-srodek)<0f)
            stronaKciuka = -stronaKciuka;
        return wzdluz*skladniki.x + wszerz*skladniki.y + stronaKciuka*skladniki.z;
    }

    static void DodajKlipBaru(Animation anim, string folder, string plik, string stan, float czas)
    {
        var source = AssetDatabase.LoadAllAssetsAtPath(BraxObsadaImport.KatalogBaru + plik).OfType<AnimationClip>()
            .SingleOrDefault(c => c.legacy && !c.name.StartsWith("__preview__") && (c.name.Split('|').Last() == stan || stan == "Pour"));
        if (!source) throw new InvalidOperationException("Nie znaleziono klipu " + stan + " w " + plik);
        var clips = new AnimationClip { name = stan, legacy = true, frameRate = source.frameRate, wrapMode = WrapMode.Once };
        var bones = anim.GetComponentsInChildren<Transform>(true).ToLookup(t => t.name);
        var found = new System.Collections.Generic.HashSet<string>();
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.type != typeof(Transform) || string.IsNullOrEmpty(binding.path)) continue;
            var leaf = binding.path.Split('/').Last();
            // Only keyed skeletal joints belong to this take. Leave the actor's scene transform alone.
            var bone = bones[leaf].SingleOrDefault();
            if (!bone || bone == anim.transform || !leaf.Contains("Hips") && !bone.IsChildOf(KoscObsady(anim.transform,"Hips"))) continue;
            var dest = binding;
            dest.path = AnimationUtility.CalculateTransformPath(bone,anim.transform);
            AnimationUtility.SetEditorCurve(clips,dest,AnimationUtility.GetEditorCurve(source,binding));
            found.Add(leaf);
        }
        if (!new[] {"Hips","Head","RightForeArm","RightHand","RightHandIndex3","LeftHand","LeftFoot","RightFoot"}.All(k => found.Any(n => n.EndsWith(k,StringComparison.Ordinal)))
            || Mathf.Abs(clips.length - czas) > .06f)
            throw new InvalidOperationException("Niepełny szkielet lub czas nagrania " + plik);
        clips.EnsureQuaternionContinuity();
        string path = folder + "Bar_" + stan + ".anim";
        var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (saved) { EditorUtility.CopySerialized(clips,saved); Object.DestroyImmediate(clips); EditorUtility.SetDirty(saved); }
        else { saved=clips; AssetDatabase.CreateAsset(saved,path); }
        if (anim.GetClip(stan)) anim.RemoveClip(stan);
        anim.AddClip(saved,stan);
    }

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
        bool ruchPrzyBarze = id == "patryk" || id == "gitara" || id == "bramkarz_1c96d333";
        anim.cullingType = ruchPrzyBarze ? AnimationCullingType.AlwaysAnimate : AnimationCullingType.BasedOnRenderers;
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
        if (id == "bramkarz_1c96d333") DodajKlipBaru(anim,folder,"Barman.fbx","Pour",85f/24f);
        if (id == "gitara" || id == "patryk") DodajKlipBaru(anim,folder,id+".fbx","Drink",8.8667f);
        if (!anim.GetClip("Idle") || !anim.GetClip("Walk")) throw new InvalidOperationException(id + " brak Idle/Walk");
        var renders = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renders.Length == 0) throw new InvalidOperationException(id + " bez siatki");
        var shader = Shader.Find("Braxhorn/Obsada Osiedla");
        if (!shader) throw new InvalidOperationException("Brak shadera postaci Osiedla");
        string materialPath = folder + "Postac.mat";
        Texture2D Tekstura(string plik) => AssetDatabase.LoadAssetAtPath<Texture2D>(folder + plik + ".png") ?? throw new InvalidOperationException(id + " brak " + plik);
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.shader = shader;
        material.SetTexture("_MainTex", Tekstura("Color"));
        material.SetTexture("_BumpMap", Tekstura("Normal"));
        material.SetTexture("_MetalSmooth", Tekstura("MetalSmooth"));
        material.SetTexture("_FaceTex", Tekstura("Mimika"));
        int Kanal(Mesh siatka, string nazwa)
        {
            for (int i = 0; i < siatka.blendShapeCount; i++)
            {
                string kanal = siatka.GetBlendShapeName(i);
                if (kanal == nazwa || kanal.EndsWith("." + nazwa, StringComparison.Ordinal)) return i;
            }
            return -1;
        }
        var skora = renders.Single(r => Kanal(r.sharedMesh,"BraxJaw") >= 0 && Kanal(r.sharedMesh,"BraxRound") >= 0);
        var mimika = model.AddComponent<BraxMimikaOsiedla>();
        mimika.twarz = skora;
        mimika.szczeka = Kanal(skora.sharedMesh,"BraxJaw");
        mimika.wargi = Kanal(skora.sharedMesh,"BraxRound");
        mimika.widoczneOczy = id != "wiechu";
        EditorUtility.SetDirty(material);
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
            BraxObjetoscRak.Przygotuj(r, folder);
            r.quality = SkinQuality.Bone4;
            if (ruchPrzyBarze) r.updateWhenOffscreen = true;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
            r.probeAnchor = KoscObsady(model.transform, "Spine2");
            r.receiveShadows = true;
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
        UstawSpojrzenie(a, gracz);
        foreach (var t in a.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
        WzrostZModelu = WzrostPatrykaWGrze;
        kapsula.height = WzrostZModelu;
        kapsula.center = Vector3.up * (kapsula.height * .5f);
        return a.gameObject;
    }

    static BraxSpojrzeniePatryka UstawSpojrzenie(Animation a, Transform gracz)
    {
        var wzrok = a.gameObject.AddComponent<BraxSpojrzeniePatryka>();
        wzrok.postac = gracz;
        wzrok.szyja = KoscObsady(a.transform, "Neck");
        wzrok.glowa = KoscObsady(a.transform, "Head");
        wzrok.przodGlowy = wzrok.glowa.InverseTransformDirection(gracz.forward);
        return wzrok;
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
            a.spojrzenie = UstawSpojrzenie(a.ruch, a.transform);
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
        st.ruch = barman.ruch;
        st.ramie = KoscObsady(barman.transform,"RightArm");
        st.dlon = KoscObsady(barman.transform,"RightHand");
        st.chwyt = new[] {"Thumb3","Index2","Middle2","Ring2","Pinky2"}
            .Select(pal => KoscObsady(barman.transform,"RightHand"+pal)).ToArray();
        var nalewanie = st.ruch.GetClip("Pour");
        nalewanie.SampleAnimation(st.ruch.gameObject,BraxBarDyskoteki.Bierze-BraxBarDyskoteki.Poczatek);
        st.osButelkiWRece = st.dlon.InverseTransformDirection(Vector3.up);
        var stojak = st.dlon.position + WektorChwytu(st.ruch.transform,"Right",
            new Vector3(.4217686f,.0825908f,.0113165f))*K;
        st.dnoWRece = st.dlon.InverseTransformPoint(stojak);
        var daneChwytu = AssetDatabase.LoadAssetAtPath<TextAsset>(BraxObsadaImport.KatalogBaru+"Chwyt.json");
        if (!daneChwytu) throw new InvalidOperationException("Brak Chwyt.json");
        var probki = JsonUtility.FromJson<ProbkiChwytu>(daneChwytu.text).przesuniecia;
        if (probki.Length != 62) throw new InvalidOperationException("Niepełny chwyt barmana.");
        st.korekta = probki.Select(v=>st.dlon.InverseTransformVector(WektorChwytu(st.ruch.transform,"Right",v)*K)).ToArray();
        st.butelki[0].SetPositionAndRotation(stojak,Quaternion.identity);
        // Keep the other bottles on the display rail, clear of the recorded reach.
        for (int i=1; i<st.butelki.Length; i++) st.butelki[i].position += Vector3.right*(.80f*K);
        Vector3 srednia = Vector3.zero;
        for (int frame=117;frame<=121;frame++)
        {
            nalewanie.SampleAnimation(st.ruch.gameObject,frame/24f-BraxBarDyskoteki.Poczatek);
            srednia += BraxBarDyskoteki.DnoButelki(st,frame/24f) + st.dlon.TransformDirection(st.osButelkiWRece)*st.butelkaWys;
        }
        var podSzyjka = srednia/5f;
        podSzyjka.y = klub.barmani[1].y + .9f*K;
        st.ciecz.position = podSzyjka + Vector3.up*(.008f*K);
        st.szklanka.position = podSzyjka;
        // A small holder is placed at the actual bottle pickup point below the worktop.
        var uchwyt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        uchwyt.name="Podstawka butelki barmana"; uchwyt.transform.SetParent(st.butelki[0].parent,true);
        uchwyt.transform.position=stojak-Vector3.up*.012f*K;
        uchwyt.transform.localScale=new Vector3(.13f*K,.012f*K,.13f*K);
        Object.DestroyImmediate(uchwyt.GetComponent<Collider>());
        var podstawa=st.butelki[1].GetComponentInChildren<Renderer>();
        if (podstawa) uchwyt.GetComponent<Renderer>().sharedMaterial=podstawa.sharedMaterial;
        st.ruch.GetClip("Idle").SampleAnimation(st.ruch.gameObject,0f);
        barman.przyBarze = true;
        var bar = klub.barStoja.Last();
        var gitara = Osoba("gitara", bar.p, bar.przod, "Drink", 1.80f, false, 9, "Siema, wszystko gra?", w(5)[0], w(5)[1], w(5)[2]);
        BraxSzklanka DoReki(Animation ruch, Transform postac, Vector3 dno, Vector3 os, float promien, float wysokosc)
        {
            ruch.GetClip("Drink").SampleAnimation(ruch.gameObject,0f);
            var dlon = KoscObsady(ruch.transform,"LeftHand");
            var szklo = Object.Instantiate(st.szklanka.gameObject,postac,true);
            szklo.name = "Szklanka " + postac.name;
            szklo.transform.localScale = new Vector3(promien/.042f,wysokosc/.095f,promien/.042f)*K;
            var napoj = Object.Instantiate(st.ciecz.gameObject,szklo.transform,false);
            napoj.transform.localPosition = Vector3.up*.008f;
            napoj.transform.localRotation = Quaternion.identity;
            napoj.transform.localScale = new Vector3(.034f,.075f,.034f);
            var rekwizyt = szklo.AddComponent<BraxSzklanka>();
            rekwizyt.ruch = ruch; rekwizyt.postac = postac; rekwizyt.dlon = dlon;
            rekwizyt.dnoWDloni = dlon.InverseTransformPoint(dlon.position+WektorChwytu(ruch.transform,"Left",dno)*K);
            var osSzkla = WektorChwytu(ruch.transform,"Left",os).normalized;
            rekwizyt.obrotWDloni = Quaternion.Inverse(dlon.rotation)*Quaternion.FromToRotation(Vector3.up,osSzkla);
            rekwizyt.ciecz = napoj.transform;
            ruch.GetClip("Idle").SampleAnimation(ruch.gameObject,0f);
            return rekwizyt;
        }
        DoReki(gitara.ruch,gitara.transform,new Vector3(.1218654f,-.0123168f,.0341197f),
            new Vector3(.05109f,.99337f,.10301f),.01900154f,.11241838f);
        var prowadzony = gracz.GetComponent<BraxGymPlayer>();
        prowadzony.szklanka = DoReki(prowadzony.clips,gracz,new Vector3(.1120149f,-.0155896f,.0373275f),
            new Vector3(.15388f,.98686f,-.04926f),.0200831f,.10207736f);
        prowadzony.szklanka.Schowaj();
        klubBar.gracz = prowadzony;
        prowadzony.bar = klubBar;

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
        if (gracz.walkOnly || gracz.TargetSpeedForStick(0f) != 0f ||
            gracz.TargetSpeedForStick(.5f) >= 3f || gracz.TargetSpeedForStick(.9f) <= 3f ||
            !gracz.clips.GetClip("Run") || !gracz.clips.GetClip("Talk"))
            throw new InvalidOperationException("Patryk Osiedla wymaga biegu z joysticka i animacji odpowiedzi.");
        var wzrok = gracz.GetComponentInChildren<BraxSpojrzeniePatryka>();
        if (!wzrok || wzrok.postac != gracz.transform || !wzrok.szyja || !wzrok.glowa ||
            !wzrok.glowa.IsChildOf(wzrok.szyja) || wzrok.przodGlowy.sqrMagnitude < .9f)
            throw new InvalidOperationException("Niepoprawne powiązanie szyi i głowy Patryka.");
        var szklanki = Object.FindObjectsByType<BraxSzklanka>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        if (!gracz.bar || !gracz.szklanka || !gracz.clips.GetClip("Drink") || !gracz.clips.GetClip("Jump") ||
            szklanki.Length != 2 || gracz.szklanka.gameObject.activeSelf ||
            szklanki.Any(s => !s.ciecz || !s.dlon || !s.ruch || s.dnoWDloni.sqrMagnitude < 1e-12f) ||
            !szklanki.Any(s => s.postac && s.postac.name=="gitara" && s.gameObject.activeSelf) ||
            (gracz.bar.stanowiska == null || !gracz.bar.stanowiska.Any(s => BraxBarDyskoteki.Gotowe(s) && s.chwyt.All(k=>k))) ||
            osoby.Any(o=>!o.spojrzenie || o.spojrzenie.glowa!=o.glowa || !o.ruch.GetComponentInChildren<BraxMimikaOsiedla>()))
            throw new InvalidOperationException("Brak chwytu, szkła, mimiki lub odniesienia do rozmówcy w Osiedlu.");
        int klipy = gracz.clips.GetClipCount() + osoby.Sum(o => o.ruch.GetClipCount());
        if (osoby.Length != 18 || osoby.Select(x => x.postac).Distinct().Count() != 18 || klipy != 376 ||
            gracz.clips.GetClipCount() != 50 || osoby.Any(o => o.gracz != gracz.transform || !o.ruch.GetClip(o.zajecie)))
            throw new InvalidOperationException("Obsada Osiedla: " + osoby.Length + " NPC, klipy=" + klipy);
        if (Object.FindObjectsByType<BraxMieszkaniec>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0 ||
            Object.FindObjectsByType<BraxPieszy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Pozostała tymczasowa postać");
        Debug.Log("BRAXHORN_OBSADA postacie=19 sterowanych=1 znajomych=18 animacje=" + klipy);
    }
}

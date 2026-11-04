using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// PATRYK — POSTAĆ GRACZA NA OSIEDLU (build 14, decyzja właściciela 1.11/2.11). Brax zostaje w projekcie „na poczekalni”
// (miasto bez zmian; na osiedlu wraca sam, gdy brak odszyfrowanych plików albo cokolwiek pójdzie źle).
//
// Źródła (prywatne, repo publiczne): CI odszyfrowuje unity-city/Prywatne/*.tar.gz.enc kluczem z sekretów
// (unity-tools/claude/prywatne.py) do Assets/Source/Prywatne (poza gitem):
//   patryk/Patryk.fbx                 — model z Tripo (1 siatka, ~10 tys. wierzchołków, 65 kości Mixamo), import Humanoid
//                                        (awatar z modelu — tylko kontrola; gra odtwarza klipy Legacy jak u Braxa)
//   patryk/Patryk_{BaseColor,Normal,ORM}.png — tekstury 2K (4K z FBX zmniejszone w CI; ORM = 1 / chropowatość / metal)
//   ruchy/<grupa>/<ruch>.fbx          — surowe animacje Mixamo (Legacy), te same co u Braxa + paczki właściciela
//
// Animacje: BraxGymPlayer gra klipy Legacy Idle / Walk / Run / Punch / Kick / Jump. Model Patryka NIE stoi w T-pozie
// (ramiona ~24° od pionu), szkielet źródeł Mixamo — w T-pozie, inne proporcje. Dlatego klipy są przenoszone na kości Patryka
// geometrycznie: (1) spoczynek Patryka wyrównany do spoczynku źródła — każda kość obrócona tak, by kierunek do dziecka był
// jak w źródle (biodra / klatka / dłonie: pełna rama z dwóch osi); (2) w każdej klatce obrót świata kości Patryka =
// obrót świata kości źródła × stała korekta z kroku 1; (3) zapis TYLKO obrotów lokalnych (+ pozycja bioder przeskalowana
// do wysokości bioder Patryka). Długości kości Patryka nie mogą się zmienić — sprawdza to test kości (8 klatek każdego
// klipu, odchyłka > 2 % = błąd; T-poza w Idle/Walk/Run = błąd). Jakikolwiek błąd → Brax.
public static partial class BraxClaudeOsiedle
{
    public const string PrywatneZrodla = "Assets/Source/Prywatne";
    const string PatrykKatalog = PrywatneZrodla + "/patryk", PatrykRuchy = PrywatneZrodla + "/ruchy", PatrykWynik = "Assets/PlayableGenerated/Patryk";
    // Dokładny wzrost postaci gracza (siatka w pozie spoczynkowej × skala) — ZmierzBohatera bierze go zamiast bounds.
    public static float WzrostZModelu = float.NaN;
    public const float WzrostPatrykaWGrze = 2.27f;   // Brax 2.33 m — Patryk trochę niższy (decyzja właściciela 2.11)

    struct PatrykKlip { public string nazwa; public bool petla, wMiejscu; public string[] zrodla; }
    static PatrykKlip PK(string n, bool petla, bool wMiejscu, params string[] z) => new PatrykKlip { nazwa = n, petla = petla, wMiejscu = wMiejscu, zrodla = z };
    // Kolejność kandydatów = pierwszeństwo (pierwszy = ten sam ruch co u Braxa). Pliki muszą być na liście RUCHY_DO_UNITY
    // w unity-tools/claude/prywatne.py. wMiejscu = bez dryfu bioder w poziomie (Mixamo bez „In Place”; u Braxa chód, bieg
    // i Kicking były w miejscu — przesuwa gracza fizyka BraxGymPlayer).
    static readonly PatrykKlip[] KlipyPatryka =
    {
        PK("Idle", true, true, "brax/Idle.fbx"),
        PK("Walk", true, true, "patryk_owner/Strut Walking.fbx", "extra/Walking.fbx", "nyxa/Walking.fbx", "extra2/Walking.fbx"),
        PK("Run", true, true, "patryk_owner/Fast Run.fbx", "extra/Running.fbx", "extra2/Unarmed Run Forward.fbx"),
        PK("Punch", false, false, "brax/Jab Cross (1).fbx", "xd/Jab Cross.fbx"),
        PK("Kick", false, false, "brax/Mma Kick (1).fbx", "nyxa/Mma Kick (1).fbx"),
        PK("Jump", false, true, "patryk_owner/Jump.fbx", "brax/Jump.fbx", "extra2/Unarmed Jump Running.fbx"),
        // dodatkowe (na komponencie Animation, na przyszłość — BraxGymPlayer ich nie gra, zachowanie bez zmian)
        PK("Boxing", false, false, "brax/Boxing.fbx", "xd/Boxing.fbx"),
        PK("Hook", false, false, "brax/Hook.fbx", "nyxa/Hook.fbx"),
        PK("Kicking", false, true, "brax/Kicking.fbx", "xd/Kicking.fbx"),
        PK("HeadHit", false, false, "brax/Head Hit.fbx", "nyxa/Head Hit.fbx"),
        PK("HitToBody", false, false, "brax/Hit To Body.fbx", "xd/Hit To Body.fbx"),
        PK("GettingUp", false, false, "extra2/Getting Up.fbx", "nyxa/Getting Up.fbx"),
        PK("Dodging", false, false, "extra/Dodging.fbx", "nyxa/Dodging.fbx"),
        PK("Block", false, false, "extra/Center Block.fbx", "xd/Center Block.fbx", "xd/Blocking.fbx", "nyxa/Right Block.fbx"),
        PK("EnteringCar", false, true, "patryk_owner/Entering Car.fbx"),
        PK("GoalkeeperDivingSave", false, true, "patryk_owner/Goalkeeper Diving Save.fbx"),
        PK("GoalkeeperCatch", false, true, "patryk_owner/Goalkeeper Catch.fbx"),
    };
    static readonly string[] PatrykWymagane = { "Idle", "Walk", "Run", "Punch", "Kick", "Jump" };

    // „mixamorig:LeftArm”, „mixamorig1:LeftArm”, „mixamorigLeftArm” → „LeftArm”
    static string PatrykKosc(string n)
    {
        int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
        if (n.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)) n = n.Substring(9).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_');
        return n;
    }

    // Postać gracza na osiedle: Patryk (dziecko obiektu gracza, warstwa 8, Animation z klipami Legacy) albo null = Brax.
    public static GameObject PatrykBohater(Transform gracz, CharacterController cialo)
    {
        WzrostZModelu = float.NaN;
        GameObject hero = null;
        try { hero = PatrykZbuduj(gracz, cialo); return hero; }
        catch (Exception e)
        {
            Debug.LogWarning("BRAXHORN_PATRYK fallback=Brax błąd: " + e);
            if (hero) Object.DestroyImmediate(hero);
            WzrostZModelu = float.NaN;
            return null;
        }
    }

    sealed class PatrykStan { public Transform t; public Quaternion r; public Vector3 p; }

    static GameObject PatrykZbuduj(Transform gracz, CharacterController cialo)
    {
        string model = Directory.Exists(PatrykKatalog) ? Directory.GetFiles(PatrykKatalog, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).FirstOrDefault() : null;
        if (model == null) { Debug.Log("BRAXHORN_PATRYK model=brak (nieodszyfrowany — sekret BRAXHORN_KEYSTORE_PASS?) → postać gracza: Brax"); return null; }
        var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        if (!wzor) { Debug.LogWarning("BRAXHORN_PATRYK model=nie zaimportowany " + model + " → Brax"); return null; }
        var avatar = AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().FirstOrDefault();

        var hero = Object.Instantiate(wzor); hero.name = "Patryk";
        try
        {
            hero.transform.position = Vector3.zero; hero.transform.rotation = Quaternion.identity; hero.transform.localScale = Vector3.one;
            foreach (var a in hero.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);    // gra: Legacy Animation jak Brax
            foreach (var a in hero.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
            var smr = hero.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.sharedMesh).OrderByDescending(r => r.sharedMesh.vertexCount).FirstOrDefault();
            if (!smr) throw new Exception("brak siatki ze szkieletem w " + model);
            var hips = hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => PatrykKosc(t.name) == "Hips");
            if (!hips) throw new Exception("brak kości Hips");
            // BraxGymPlayer.JumpHips porównuje hips.localPosition.y — rodzic bioder musi mieć oś Y w górę
            if (!hips.parent || Vector3.Angle(hips.parent.up, Vector3.up) > .5f)
            {
                var k = new GameObject("Patryk szkielet"); k.transform.SetParent(hero.transform, false); hips.SetParent(k.transform, true);
            }

            // --- wzrost w pozie spoczynkowej (wierzchołki siatki = poza wiązania) ---
            var v = smr.sharedMesh.vertices; var m = smr.transform.localToWorldMatrix;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in v) { float y = m.MultiplyPoint3x4(p).y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            if (v.Length == 0) { var b = smr.bounds; minY = b.min.y; maxY = b.max.y; }
            float h0 = maxY - minY;
            if (!(h0 > .05f)) throw new Exception("niepoprawna wysokość modelu " + h0);
            // właściciel 2.11 10:39–10:43: „pod Braxa ma być wszystko … Brax może być wyższy od Patryka, ale trochę” — świat w skali Braxa (2.33 m),
            // Patryk w grze 2.27 m (model ×skala; proporcje bez zmian, kości bez zmian — test kości liczy spoczynek przed skalą)
            bool zModelu = false;
            float skala = WzrostPatrykaWGrze / h0, wzrost = WzrostPatrykaWGrze;

            var kosci = hips.GetComponentsInChildren<Transform>(true).ToList();   // od korzenia w dół (rodzic przed dzieckiem)
            var spoczynek = kosci.Select(t => new PatrykStan { t = t, r = t.localRotation, p = t.localPosition }).ToList();
            float ramiona = PatrykKatRamion(kosci);

            // --- materiał jak u Braxa (Braxhorn/HeroPremium, ORM) ---
            if (!AssetDatabase.IsValidFolder(PatrykWynik))
            {
                if (AssetDatabase.IsValidFolder("Assets/PlayableGenerated")) AssetDatabase.CreateFolder("Assets/PlayableGenerated", "Patryk");
                else { Directory.CreateDirectory(PatrykWynik); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); }
            }
            var mat = PatrykMaterial();
            foreach (var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int n = r.sharedMesh ? Mathf.Max(1, r.sharedMesh.subMeshCount) : Mathf.Max(1, r.sharedMaterials.Length);
                r.sharedMaterials = Enumerable.Repeat(mat, n).ToArray(); r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- klipy przeniesione na kości Patryka ---
            var klipy = new List<AnimationClip>(); var opis = new List<string>();
            foreach (var k in KlipyPatryka)
            {
                AnimationClip gotowy = null;
                foreach (var z in k.zrodla)
                {
                    string sciezka = PatrykRuchy + "/" + z;
                    if (!File.Exists(sciezka)) continue;
                    try
                    {
                        gotowy = PatrykPrzenies(hero, hips, kosci, spoczynek, minY, sciezka, k, out string uwagi);
                        opis.Add(k.nazwa + "←" + z + uwagi);
                        break;
                    }
                    catch (Exception e) { Debug.LogWarning("BRAXHORN_PATRYK klip " + k.nazwa + " z " + z + ": " + e.Message); }
                    finally { PatrykSpoczynek(spoczynek); }
                }
                if (gotowy) klipy.Add(gotowy);
                else Debug.LogWarning("BRAXHORN_PATRYK brak klipu " + k.nazwa);
            }
            var brak = PatrykWymagane.Where(n => !klipy.Any(c => c.name == n)).ToArray();
            if (brak.Length > 0) throw new Exception("brak wymaganych klipów: " + string.Join(", ", brak));

            var anim = hero.AddComponent<Animation>(); anim.playAutomatically = false;
            foreach (var c in klipy) anim.AddClip(c, c.name);

            // --- test kości: 8 klatek każdego klipu vs spoczynek (> 2 % = błąd), T-poza w Idle/Walk/Run = błąd ---
            int bledy = PatrykTestKosci(hero, kosci, spoczynek, klipy, out string raportTestu);
            Debug.Log("BRAXHORN_PATRYK_KOSCI bledy=" + bledy + " " + raportTestu);
            if (bledy > 0) throw new Exception("test kości: " + bledy + " błędów — " + raportTestu);

            anim.clip = anim.GetClip("Idle"); anim.Play("Idle"); anim.Sample();

            // --- na miejsce: dziecko gracza, skala do wzrostu, stopy na podstawie kapsuły, warstwa gracza 8 ---
            hero.transform.SetParent(gracz, false);
            hero.transform.localRotation = Quaternion.identity;
            hero.transform.localScale = Vector3.one * skala;
            hero.transform.localPosition = new Vector3(0f, -minY * skala, 0f);
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            // Kapsuła na wzrost postaci (Brax 2.33 m miał 2.25 m; Patryk w realnym wzroście musi zmieścić się w drzwiach × k).
            if (cialo)
            {
                cialo.height = wzrost + .04f; cialo.radius = Mathf.Clamp(wzrost * .15f, .26f, .35f);
                cialo.center = new Vector3(0f, cialo.height * .5f, 0f);
                if (cialo.stepOffset > cialo.height * .25f) cialo.stepOffset = cialo.height * .25f;
            }
            WzrostZModelu = wzrost;

            Debug.Log("BRAXHORN_PATRYK model=" + Path.GetFileName(model) + " (" + v.Length + " wierzch., " + kosci.Count + " kości, awatar Humanoid "
                + (avatar ? "isHuman=" + avatar.isHuman + " isValid=" + avatar.isValid : "brak") + ", poza modelu: ramiona " + ramiona.ToString("F0") + "° od pionu"
                + (ramiona > 75f ? " = T" : ramiona > 35f ? " = A" : " = swobodna (nie T/A — klipy przenoszone geometrycznie)") + ")"
                + " wzrost=" + wzrost.ToString("F2") + " m" + " (model " + h0.ToString("F2") + " m ×" + skala.ToString("F3") + " → skala Braxa: Patryk " + WzrostPatrykaWGrze.ToString("F2") + " m, Brax " + WzrostBraxa.ToString("F2") + " m)"
                + " kapsuła=" + (cialo ? cialo.height.ToString("F2") + "×" + cialo.radius.ToString("F2") : "—")
                + " klipy=" + klipy.Count + " [" + string.Join(", ", opis) + "] bledy_kosci=" + bledy);
            return hero;
        }
        catch { Object.DestroyImmediate(hero); throw; }
    }

    static void PatrykSpoczynek(List<PatrykStan> s) { foreach (var x in s) { x.t.localRotation = x.r; x.t.localPosition = x.p; } }

    static Transform PatrykZnajdz(IEnumerable<Transform> kosci, string nazwa) => kosci.FirstOrDefault(t => PatrykKosc(t.name) == nazwa);

    // Kąt ramienia (Arm → ForeArm) od pionu w dół, średnia z obu stron: T-poza ≈ 90°, A-poza ≈ 45°, ręce wzdłuż ciała ≈ 0–25°.
    static float PatrykKatRamion(IEnumerable<Transform> kosci)
    {
        var l = kosci as IList<Transform> ?? kosci.ToList(); float suma = 0; int n = 0;
        foreach (var s in new[] { "Left", "Right" })
        {
            var a = PatrykZnajdz(l, s + "Arm"); var f = PatrykZnajdz(l, s + "ForeArm");
            if (a && f && (f.position - a.position).sqrMagnitude > 1e-10f) { suma += Vector3.Angle(f.position - a.position, Vector3.down); n++; }
        }
        return n > 0 ? suma / n : 0f;
    }

    static Texture2D PatrykTekstura(string plik, bool liniowa, bool normalna)
    {
        string p = PatrykKatalog + "/" + plik;
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp == null) return null;
        var typ = normalna ? TextureImporterType.NormalMap : TextureImporterType.Default; bool srgb = !(liniowa || normalna);
        if (imp.textureType != typ || imp.sRGBTexture != srgb || imp.maxTextureSize > 2048 || !imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            imp.textureType = typ; imp.sRGBTexture = srgb; imp.maxTextureSize = Mathf.Min(imp.maxTextureSize, 2048); imp.mipmapEnabled = true; imp.anisoLevel = 8;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit"); if (ign != null && ign.CanWrite) ign.SetValue(imp, true);   // postać z bliska — jak tekstury Braxa
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Material PatrykMaterial()
    {
        var shader = Shader.Find("Braxhorn/HeroPremium"); if (!shader) throw new Exception("brak shadera Braxhorn/HeroPremium");
        var kolor = PatrykTekstura("Patryk_BaseColor.png", false, false); if (!kolor) throw new Exception("brak Patryk_BaseColor.png");
        var normal = PatrykTekstura("Patryk_Normal.png", false, true);
        var orm = PatrykTekstura("Patryk_ORM.png", true, false);
        // Ustawienia jak „Brax original PBR restored” (BraxHeroRefinements): normal .85, dolna granica chropowatości .42, metal ×.30.
        var mat = new Material(shader) { name = "Patryk PBR (ORM)", color = Color.white };
        mat.SetTexture("_MainTex", kolor);
        if (normal) mat.SetTexture("_BumpMap", normal);
        if (orm) mat.SetTexture("_Orm", orm);
        else mat.SetFloat("_MetallicScale", 0f);   // bez ORM (biała): bez metalu; chropowatość z dolnej granicy
        mat.SetFloat("_BumpScale", normal ? .85f : 0f); mat.SetFloat("_RoughnessFloor", .42f); if (orm) mat.SetFloat("_MetallicScale", .30f); mat.SetFloat("_OcclusionStrength", .8f);
        string sc = PatrykWynik + "/PatrykORM.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(sc)) AssetDatabase.DeleteAsset(sc);
        BraxZapisAssetu.Utworz(mat, sc);
        return mat;
    }

    // Dziecko kości wyznaczające jej kierunek + opcjonalna para boczna (pełna rama: biodra, klatka, dłonie).
    static bool PatrykKierunek(Transform t, List<Transform> kosci, out Transform dziecko, out Transform lewy, out Transform prawy)
    {
        string n = PatrykKosc(t.name); dziecko = lewy = prawy = null;
        Transform D(string x) { foreach (Transform c in t) if (PatrykKosc(c.name) == x) return c; return null; }
        if (n == "Hips") { dziecko = D("Spine"); lewy = D("LeftUpLeg"); prawy = D("RightUpLeg"); }
        else if (n == "LeftHand" || n == "RightHand")
        {
            string s = n.Substring(0, n.Length - 4);
            dziecko = D(s + "HandMiddle1") ?? D(s + "HandIndex1") ?? D(s + "HandRing1");
            lewy = D(s + "HandIndex1"); prawy = D(s + "HandPinky1") ?? D(s + "HandRing1");
            if (lewy == dziecko || prawy == dziecko) lewy = prawy = null;
        }
        else
        {
            dziecko = D("Neck");
            if (dziecko) { lewy = D("LeftShoulder"); prawy = D("RightShoulder"); }
            else
            {
                Transform jedyny = null; int ile = 0;
                foreach (Transform c in t) if (kosci.Contains(c)) { jedyny = c; ile++; }
                if (ile == 1) dziecko = jedyny;
                else if (ile > 1) dziecko = t.Cast<Transform>().Where(kosci.Contains).OrderByDescending(c => c.GetComponentsInChildren<Transform>(true).Length).First();
            }
        }
        if (!(lewy && prawy)) lewy = prawy = null;
        return dziecko;
    }

    // Próbka klipu źródłowego na instancji źródła. reczne = krzywe czytane z AnimationUtility (gdy SampleAnimation nie zadziała).
    sealed class PatrykKrzywa { public Transform t; public int pole, os; public AnimationCurve c; }
    static List<PatrykKrzywa> PatrykKrzywe(AnimationClip clip, GameObject src)
    {
        var l = new List<PatrykKrzywa>();
        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            if (b.type != typeof(Transform)) continue;
            var t = string.IsNullOrEmpty(b.path) ? src.transform : src.transform.Find(b.path); if (!t) continue;
            string p = b.propertyName; int pole = p.StartsWith("m_LocalRotation") ? 0 : p.StartsWith("m_LocalPosition") ? 1 : p.StartsWith("localEulerAngles") ? 2 : -1;
            if (pole < 0) continue;
            int os = "xyzw".IndexOf(p[p.Length - 1]); if (os < 0) continue;
            l.Add(new PatrykKrzywa { t = t, pole = pole, os = os, c = AnimationUtility.GetEditorCurve(clip, b) });
        }
        return l;
    }
    static void PatrykProbka(AnimationClip clip, GameObject src, float czas, List<PatrykKrzywa> reczne)
    {
        if (reczne == null) { clip.SampleAnimation(src, czas); return; }
        var rot = new Dictionary<Transform, Vector4>(); var eul = new Dictionary<Transform, Vector3>(); var pos = new Dictionary<Transform, Vector3>();
        foreach (var k in reczne)
        {
            float v = k.c.Evaluate(czas);
            if (k.pole == 0) { var q = rot.TryGetValue(k.t, out var x) ? x : new Vector4(k.t.localRotation.x, k.t.localRotation.y, k.t.localRotation.z, k.t.localRotation.w); q[k.os] = v; rot[k.t] = q; }
            else if (k.pole == 1) { var p = pos.TryGetValue(k.t, out var x) ? x : k.t.localPosition; p[k.os] = v; pos[k.t] = p; }
            else if (k.os < 3) { var e = eul.TryGetValue(k.t, out var x) ? x : k.t.localEulerAngles; e[k.os] = v; eul[k.t] = e; }
        }
        foreach (var kv in rot) { var q = kv.Value.normalized; kv.Key.localRotation = new Quaternion(q.x, q.y, q.z, q.w); }
        foreach (var kv in eul) kv.Key.localRotation = Quaternion.Euler(kv.Value);
        foreach (var kv in pos) kv.Key.localPosition = kv.Value;
    }

    // podloga = wysokość stóp Patryka (najniższy wierzchołek w spoczynku) w układzie obiektu Patryka (instancja w (0,0,0), skala 1).
    static AnimationClip PatrykPrzenies(GameObject hero, Transform hipsT, List<Transform> kosci, List<PatrykStan> spoczynek, float podloga, string sciezka, PatrykKlip k, out string uwagi)
    {
        var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(sciezka); if (!wzor) throw new Exception("nie zaimportowany");
        var zrodlo = AssetDatabase.LoadAllAssetsAtPath(sciezka).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();
        if (!zrodlo || zrodlo.length <= 0f) throw new Exception("brak klipu w pliku");
        var src = Object.Instantiate(wzor);
        try
        {
            src.transform.position = Vector3.zero; src.transform.rotation = Quaternion.identity; src.transform.localScale = Vector3.one;
            foreach (var a in src.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            var mapa = new Dictionary<string, Transform>();
            foreach (var t in src.GetComponentsInChildren<Transform>(true)) { string nk = PatrykKosc(t.name); if (!mapa.ContainsKey(nk)) mapa[nk] = t; }
            Transform Z(Transform t) => t && mapa.TryGetValue(PatrykKosc(t.name), out var s) ? s : null;
            var sHips = Z(hipsT); if (!sHips) throw new Exception("źródło bez Hips");
            var sL = mapa.TryGetValue("LeftUpLeg", out var a1) ? a1 : null; var sR = mapa.TryGetValue("RightUpLeg", out var a2) ? a2 : null;
            if (!sL || !sR) throw new Exception("źródło bez nóg");
            var pary = kosci.Select(t => new KeyValuePair<Transform, Transform>(t, Z(t))).Where(p => p.Value).ToList();
            if (pary.Count < 40) throw new Exception("za mało wspólnych kości (" + pary.Count + ")");

            // Spoczynek źródła (T-poza Mixamo) — przód z nóg: prawo = RightUpLeg − LeftUpLeg, przód = prawo × góra.
            PatrykSpoczynek(spoczynek);
            Vector3 przod = Vector3.Cross(sR.position - sL.position, Vector3.up); przod.y = 0f;
            var yaw = Quaternion.Euler(0f, przod.sqrMagnitude > 1e-8f ? Vector3.SignedAngle(przod, Vector3.forward, Vector3.up) : 0f, 0f);
            var ws0 = pary.ToDictionary(p => p.Key, p => p.Value.rotation);
            // Biodra: wysokość nad stopami skalowana proporcją wysokości bioder (Patryk / źródło); w poziomie od spoczynku.
            Vector3 sRest = yaw * (sHips.position - src.transform.position), tRest = hipsT.position - hero.transform.position;
            float biodraS = sRest.y, biodraT = tRest.y - podloga;
            if (biodraS < 1e-3f || biodraT < 1e-3f) throw new Exception("biodra na ziemi w spoczynku (źródło " + biodraS + ", Patryk " + biodraT + ")");
            float skalaB = biodraT / biodraS;

            // (1) Spoczynek Patryka wyrównany kierunkami kości do spoczynku źródła (od korzenia w dół). Źródło stoi tu w spoczynku.
            foreach (var p in pary)
            {
                var t = p.Key;
                if (!PatrykKierunek(t, kosci, out var tc, out var tl, out var tr)) continue;
                var sc = Z(tc); if (!sc) continue;
                Vector3 dt = tc.position - t.position, ds = yaw * (sc.position - p.Value.position);
                if (dt.sqrMagnitude < 1e-12f || ds.sqrMagnitude < 1e-12f) continue;
                Quaternion A;
                var sl = Z(tl); var sr = Z(tr);
                if (tl && tr && sl && sr)
                {
                    Vector3 bt = tr.position - tl.position, bs = yaw * (sr.position - sl.position);
                    A = Quaternion.LookRotation(ds, bs) * Quaternion.Inverse(Quaternion.LookRotation(dt, bt));
                }
                else A = Quaternion.FromToRotation(dt, ds);
                t.rotation = A * t.rotation;
            }
            var korekta = pary.ToDictionary(p => p.Key, p => Quaternion.Inverse(yaw * ws0[p.Key]) * p.Key.rotation);
            PatrykSpoczynek(spoczynek);

            // Czy SampleAnimation rusza źródło? Jeśli nie (zła ścieżka korzenia) — krzywe ręcznie.
            float dl = zrodlo.length;
            List<PatrykKrzywa> reczne = null;
            if (PatrykRuch(zrodlo, src, pary, ws0, dl, null) < .5f)
            {
                reczne = PatrykKrzywe(zrodlo, src);
                if (PatrykRuch(zrodlo, src, pary, ws0, dl, reczne) < .5f) throw new Exception("klip źródłowy bez ruchu (SampleAnimation i krzywe)");
            }
            Vector3 PS(float czas) { PatrykProbka(zrodlo, src, czas, reczne); return yaw * (sHips.position - src.transform.position); }
            Vector3 p0 = PS(0f), p1 = PS(dl);
            Vector3 dryf = k.wMiejscu ? new Vector3(p1.x - p0.x, 0f, p1.z - p0.z) : Vector3.zero;

            // (2) Klatki 30/s: obrót świata kości Patryka = yaw × obrót źródła × korekta; biodra × (biodra Patryka / biodra źródła).
            int n = Mathf.Max(2, Mathf.RoundToInt(dl * 30f) + 1);
            var czasy = new float[n]; var obroty = pary.Select(_ => new Quaternion[n]).ToArray(); var biodra = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float czas = i == n - 1 ? dl : Mathf.Min(dl, i / 30f); czasy[i] = czas;
                Vector3 ps = PS(czas) - dryf * (czas / dl);
                hipsT.position = hero.transform.position + new Vector3(tRest.x + (ps.x - sRest.x) * skalaB, podloga + ps.y * skalaB, tRest.z + (ps.z - sRest.z) * skalaB);
                for (int j = 0; j < pary.Count; j++) pary[j].Key.rotation = yaw * pary[j].Value.rotation * korekta[pary[j].Key];
                for (int j = 0; j < pary.Count; j++)
                {
                    var q = pary[j].Key.localRotation;
                    if (i > 0 && Quaternion.Dot(q, obroty[j][i - 1]) < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);   // ciągłość znaku
                    obroty[j][i] = q;
                }
                biodra[i] = hipsT.localPosition;
            }
            PatrykSpoczynek(spoczynek);

            // (3) Klip Legacy: tylko obroty lokalne (+ pozycja bioder). Kości bez ruchu — 2 klucze (stały obrót we wszystkich klipach,
            // żeby przenikanie Legacy nie zostawiało kości z poprzedniego klipu).
            var klip = new AnimationClip { name = k.nazwa, legacy = true, frameRate = 30f, wrapMode = k.petla ? WrapMode.Loop : WrapMode.Once };
            for (int j = 0; j < pary.Count; j++)
            {
                string sc = AnimationUtility.CalculateTransformPath(pary[j].Key, hero.transform);
                var q = obroty[j];
                bool staly = q.All(x => Quaternion.Angle(x, q[0]) < .05f);
                for (int o = 0; o < 4; o++) klip.SetCurve(sc, typeof(Transform), "localRotation." + "xyzw"[o], PatrykKrzywaZ(czasy, q.Select(x => x[o]).ToArray(), staly));
            }
            string sh = AnimationUtility.CalculateTransformPath(hipsT, hero.transform);
            for (int o = 0; o < 3; o++) klip.SetCurve(sh, typeof(Transform), "localPosition." + "xyz"[o], PatrykKrzywaZ(czasy, biodra.Select(x => x[o]).ToArray(), false));
            klip.EnsureQuaternionContinuity();
            string cel = PatrykWynik + "/" + k.nazwa + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(cel)) AssetDatabase.DeleteAsset(cel);
            BraxZapisAssetu.Utworz(klip, cel);
            uwagi = (reczne != null ? " (krzywe ręcznie)" : "") + (k.wMiejscu && dryf.magnitude > .05f ? " (w miejscu, dryf " + dryf.magnitude.ToString("F2") + " m)" : "")
                + " " + dl.ToString("F1") + " s, biodra ×" + skalaB.ToString("F2") + (Mathf.Abs(Quaternion.Angle(yaw, Quaternion.identity)) > 1f ? ", obrót źródła " + yaw.eulerAngles.y.ToString("F0") + "°" : "");
            return klip;
        }
        finally { Object.DestroyImmediate(src); }
    }

    // Największy kąt (°) między spoczynkiem a pozą źródła w połowie i na końcu klipu (czy próbkowanie w ogóle działa).
    static float PatrykRuch(AnimationClip clip, GameObject src, List<KeyValuePair<Transform, Transform>> pary, Dictionary<Transform, Quaternion> ws0, float dl, List<PatrykKrzywa> reczne)
    {
        float max = 0f;
        foreach (float czas in new[] { dl * .5f, dl * .27f, dl })
        {
            PatrykProbka(clip, src, czas, reczne);
            foreach (var p in pary) max = Mathf.Max(max, Quaternion.Angle(ws0[p.Key], p.Value.rotation));
        }
        return max;
    }

    static AnimationCurve PatrykKrzywaZ(float[] t, float[] v, bool staly)
    {
        if (staly || t.Length < 2) return new AnimationCurve(new Keyframe(0f, v[0], 0f, 0f), new Keyframe(Mathf.Max(t[t.Length - 1], 1f / 30f), v[0], 0f, 0f));
        var kl = new Keyframe[t.Length];
        for (int i = 0; i < t.Length; i++)
        {
            int a = Mathf.Max(0, i - 1), b = Mathf.Min(t.Length - 1, i + 1);
            float dt = t[b] - t[a], tan = dt > 1e-6f ? (v[b] - v[a]) / dt : 0f;   // styczne Catmulla-Roma (płynnie między klatkami)
            kl[i] = new Keyframe(t[i], v[i], tan, tan);
        }
        return new AnimationCurve(kl);
    }

    // ZERO deformacji: długość każdej kości (od rodzica) w 8 klatkach każdego klipu = spoczynek (±2 %); T-poza w Idle/Walk/Run = błąd.
    static int PatrykTestKosci(GameObject hero, List<Transform> kosci, List<PatrykStan> spoczynek, List<AnimationClip> klipy, out string raport)
    {
        PatrykSpoczynek(spoczynek);
        var zbior = new HashSet<Transform>(kosci);
        var dl0 = kosci.Where(t => t.parent && zbior.Contains(t.parent)).ToDictionary(t => t, t => (t.position - t.parent.position).magnitude);
        int bledy = 0; var uwagi = new List<string>(); float maxOdch = 0f;
        foreach (var c in klipy)
        {
            int bk = 0, tpoza = 0; float ruch = 0f;
            for (int i = 0; i < 8; i++)
            {
                c.SampleAnimation(hero, c.length * i / 7f);
                foreach (var kv in dl0)
                {
                    if (kv.Value < 1e-5f) continue;
                    float odch = Mathf.Abs((kv.Key.position - kv.Key.parent.position).magnitude / kv.Value - 1f);
                    if (odch > maxOdch) maxOdch = odch;
                    if (odch > .02f) bk++;
                }
                if (PatrykKatRamion(kosci) > 70f) tpoza++;
                foreach (var s in spoczynek) ruch = Mathf.Max(ruch, Quaternion.Angle(s.r, s.t.localRotation));
            }
            if (bk > 0) { bledy += bk; uwagi.Add(c.name + ": " + bk + " kości > 2 %"); }
            if ((c.name == "Idle" || c.name == "Walk" || c.name == "Run") && tpoza == 8) { bledy++; uwagi.Add(c.name + ": T-poza"); }
            if (ruch < 1f) { bledy++; uwagi.Add(c.name + ": bez ruchu (poza spoczynkowa)"); }
            PatrykSpoczynek(spoczynek);
        }
        raport = "(klipy=" + klipy.Count + " × 8 klatek, kości=" + dl0.Count + ", max odchyłka " + (maxOdch * 100f).ToString("F3") + " %, próg 2 %)" + (uwagi.Count > 0 ? " " + string.Join("; ", uwagi) : "");
        return bledy;
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Patryk: wymagany gracz osiedla. Wysokość na ekranie pozostaje taka jak w buildzie 41.
// Prywatne modele i animacje są odszyfrowywane wyłącznie przez istniejący krok CI.
// Źródła i model są Humanoid; Unity przenosi anatomiczną pozę i zapisuje klipy Legacy
// dla BraxGymPlayer. Oryginalna hierarchia i bindposes zostają zachowane.
// Idle = spokojna poza modelu, CombatIdle = garda dopiero po ciosie. Błąd zatrzymuje build.
public static partial class BraxClaudeOsiedle
{
    public const string PrywatneZrodla = "Assets/Source/Prywatne";
    const string PatrykKatalog = PrywatneZrodla + "/patryk", PatrykRuchy = PrywatneZrodla + "/ruchy", PatrykWynik = "Assets/PlayableGenerated/Patryk";
    // Dokładny wzrost postaci gracza (siatka w pozie spoczynkowej × skala) — ZmierzBohatera bierze go zamiast bounds.
    public static float WzrostZModelu = float.NaN;
    public const float WzrostPatrykaWGrze = 2.27f;   // 2.27 jednostki gry reprezentują 1.80 m; nie zmniejszać widocznego gracza

    struct PatrykKlip { public string nazwa; public bool petla, wMiejscu; public string[] zrodla; }
    static PatrykKlip PK(string n, bool petla, bool wMiejscu, params string[] z) => new PatrykKlip { nazwa = n, petla = petla, wMiejscu = wMiejscu, zrodla = z };
    // Kolejność kandydatów = pierwszeństwo (pierwszy = ten sam ruch co u Braxa). Pliki muszą być na liście RUCHY_DO_UNITY
    // w unity-tools/claude/prywatne.py. wMiejscu = bez dryfu bioder w poziomie (Mixamo bez „In Place”; u Braxa chód, bieg
    // i Kicking były w miejscu — przesuwa gracza fizyka BraxGymPlayer).
    static readonly PatrykKlip[] KlipyPatryka =
    {
        PK("CombatIdle", true, true, "brax/Idle.fbx"),
        PK("Walk", true, true, "extra/Walking.fbx", "nyxa/Walking.fbx", "extra2/Walking.fbx"),
        PK("Run", true, true, "extra/Running.fbx", "extra2/Unarmed Run Forward.fbx"),
        PK("Punch", false, false, "brax/Jab Cross (1).fbx", "xd/Jab Cross.fbx"),
        PK("Kick", false, false, "brax/Mma Kick (1).fbx", "nyxa/Mma Kick (1).fbx"),
        PK("Jump", false, true, "brax/Jump.fbx", "extra2/Unarmed Jump Running.fbx"),
        // dodatkowe (na komponencie Animation, na przyszłość — BraxGymPlayer ich nie gra, zachowanie bez zmian)
        PK("Boxing", false, false, "brax/Boxing.fbx", "xd/Boxing.fbx"),
        PK("Hook", false, false, "brax/Hook.fbx", "nyxa/Hook.fbx"),
        PK("Kicking", false, true, "brax/Kicking.fbx", "xd/Kicking.fbx"),
        PK("HeadHit", false, false, "brax/Head Hit.fbx", "nyxa/Head Hit.fbx"),
        PK("HitToBody", false, false, "brax/Hit To Body.fbx", "xd/Hit To Body.fbx"),
        PK("GettingUp", false, false, "extra2/Getting Up.fbx", "nyxa/Getting Up.fbx"),
        PK("Dodging", false, false, "extra/Dodging.fbx", "nyxa/Dodging.fbx"),
        PK("Block", false, false, "extra/Center Block.fbx", "xd/Center Block.fbx", "xd/Blocking.fbx", "nyxa/Right Block.fbx"),
    };
    static readonly string[] PatrykWymagane = { "Idle", "CarIdle", "CombatIdle", "Walk", "Run", "Punch", "Kick", "Jump" };

    // „mixamorig:LeftArm”, „mixamorig1:LeftArm”, „mixamorigLeftArm” → „LeftArm”
    static string PatrykKosc(string n)
    {
        int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
        if (n.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase)) n = n.Substring(9).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_');
        return n;
    }

    // Postać osiedla: wymagany Patryk; błąd importu zatrzymuje build, bez ukrytej podmiany na Braxa.
    public static GameObject PatrykBohater(Transform gracz, CharacterController cialo)
    {
        WzrostZModelu = float.NaN;
        GameObject hero = null;
        try { hero = PatrykZbuduj(gracz, cialo); return hero; }
        catch (Exception e)
        {
            Debug.LogError("BRAXHORN_PATRYK błąd: " + e);
            if (hero) Object.DestroyImmediate(hero);
            WzrostZModelu = float.NaN;
            throw new InvalidOperationException("Nie można zbudować Patryka; przerwano zamiast podmieniać gracza", e);
        }
    }

    sealed class PatrykStan { public Transform t; public Quaternion r; public Vector3 p; }

    static GameObject PatrykZbuduj(Transform gracz, CharacterController cialo)
    {
        string model = Directory.Exists(PatrykKatalog) ? Directory.GetFiles(PatrykKatalog, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).FirstOrDefault() : null;
        if (model == null) throw new InvalidOperationException("Brak odszyfrowanego modelu Patryka");
        var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        if (!wzor) throw new InvalidOperationException("Nie zaimportowano modelu Patryka: " + model);
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
                r.quality = SkinQuality.Bone4;
                r.sharedMaterials = Enumerable.Repeat(mat, n).ToArray(); r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- klipy przeniesione na kości Patryka ---
            var klipy = new List<AnimationClip> { PatrykSpokojny(hero, spoczynek), PatrykPozaStala(hero,spoczynek,"CarIdle",true) }; var opis = new List<string>();
            foreach (var k in KlipyPatryka)
            {
                AnimationClip gotowy = null;
                foreach (var z in k.zrodla)
                {
                    string sciezka = PatrykRuchy + "/" + z;
                    if (!File.Exists(sciezka)) continue;
                    try
                    {
                        gotowy = PatrykHumanoid(hero, avatar, hips, kosci, spoczynek, sciezka, k, out string uwagi);
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
        AssetDatabase.CreateAsset(mat, sc);
        return mat;
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

    // Kontrola długości kości (nie zastępuje oceny deformacji skóry na nagraniu): długość każdej kości (od rodzica) w 8 klatkach każdego klipu = spoczynek (±2 %); T-poza w Idle/Walk/Run = błąd.
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
                    if (float.IsNaN(odch) || float.IsInfinity(odch) || odch > .02f) bk++;
                }
                if (PatrykKatRamion(kosci) > 70f) tpoza++;
                foreach (var s in spoczynek) ruch = Mathf.Max(ruch, Quaternion.Angle(s.r, s.t.localRotation));
            }
            if (c.name == "Idle" && PatrykKatRamion(kosci) > 40f) { bledy++; uwagi.Add("Idle: ramiona nie są opuszczone"); }
            if (bk > 0) { bledy += bk; uwagi.Add(c.name + ": " + bk + " kości > 2 %"); }
            if ((c.name == "Idle" || c.name == "Walk" || c.name == "Run") && tpoza == 8) { bledy++; uwagi.Add(c.name + ": T-poza"); }
            if (c.name != "Idle" && ruch < 1f) { bledy++; uwagi.Add(c.name + ": bez ruchu (poza spoczynkowa)"); }
            PatrykSpoczynek(spoczynek);
        }
        raport = "(klipy=" + klipy.Count + " × 8 klatek, kości=" + dl0.Count + ", max odchyłka " + (maxOdch * 100f).ToString("F3") + " %, próg 2 %)" + (uwagi.Count > 0 ? " " + string.Join("; ", uwagi) : "");
        return bledy;
    }
}

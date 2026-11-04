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
    public const float WzrostPatrykaWGrze = 2.33f;   // właściciel 3.11: Patryk 1,80 m = Brax 1,80 m = 2,33 w grze (K = 1,294), wyglądają tak samo

    struct PatrykKlip { public string nazwa; public bool petla, wMiejscu; public string[] zrodla; }
    static PatrykKlip PK(string n, bool petla, bool wMiejscu, params string[] z) => new PatrykKlip { nazwa = n, petla = petla, wMiejscu = wMiejscu, zrodla = z };
    // Kolejność kandydatów = pierwszeństwo (pierwszy = ten sam ruch co u Braxa). Pliki muszą być na liście RUCHY_DO_UNITY
    // w unity-tools/claude/prywatne.py. wMiejscu = bez dryfu bioder w poziomie (Mixamo bez „In Place”; u Braxa chód, bieg
    // i Kicking były w miejscu — przesuwa gracza fizyka BraxGymPlayer).
    static readonly PatrykKlip[] KlipyPatryka =
    {
        PK("CombatIdle", true, true, "brax/Idle.fbx"),
        PK("Walk", true, true, "patryk_owner/Strut Walking.fbx", "extra/Walking.fbx", "nyxa/Walking.fbx", "extra2/Walking.fbx"),
        PK("Run", true, true, "patryk_owner/Fast Run.fbx", "extra/Running.fbx", "extra2/Unarmed Run Forward.fbx"),
        PK("Punch", false, true, "brax/Jab Cross (1).fbx", "xd/Jab Cross.fbx"),
        PK("Kick", false, true, "brax/Mma Kick (1).fbx", "nyxa/Mma Kick (1).fbx"),
        PK("Jump", false, true, "patryk_owner/Jump.fbx", "brax/Jump.fbx", "extra2/Unarmed Jump Running.fbx"),
        PK("EnteringCar", false, true, "patryk_owner/Entering Car.fbx"),
        PK("GoalkeeperDivingSave", false, true, "patryk_owner/Goalkeeper Diving Save.fbx"),
        PK("GoalkeeperCatch", false, true, "patryk_owner/Goalkeeper Catch.fbx"),
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
            throw new InvalidOperationException("Wymagany nowy Patryk; bez starego modelu lub zastępstwa", e);
        }
    }

    sealed class PatrykStan { public Transform t; public Quaternion r; public Vector3 p; }

    static GameObject PatrykZbuduj(Transform gracz, CharacterController cialo)
    {
        string model = Directory.Exists(PatrykKatalog) ? Directory.GetFiles(PatrykKatalog, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).FirstOrDefault() : null;
        if (model == null) throw new InvalidOperationException("Brak odszyfrowanego nowego Patryka");
        using (var hash = System.Security.Cryptography.SHA256.Create())
        using (var stream = File.OpenRead(model))
            if (BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != "fb8e2630bc7f835a743d917c35c378c7d074f0fde993a0bbf864b31ab67e1d97")
                throw new InvalidOperationException("Nieprawidłowy model: wymagany Patryk.zip z 3.11.2026");
        var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(model);
        if (!wzor) throw new InvalidOperationException("Nie zaimportowano modelu Patryka: " + model);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().FirstOrDefault();

        var hero = Object.Instantiate(wzor); hero.name = "Patryk";
        Avatar tPoza = null;
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
            // Właściciel: 1,80 m rzeczywistego wzrostu = 2,33 jednostki świata, bez zmiany proporcji.
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
                r.sharedMaterials = Enumerable.Repeat(mat, n).ToArray(); r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.On;
            }

            // --- klipy przeniesione na kości Patryka ---
            var klipy = new List<AnimationClip> { PatrykSpokojny(hero, spoczynek), PatrykPozaStala(hero,spoczynek,"CarIdle",true) }; var opis = new List<string>();
            if (!avatar || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Nowy Patryk: nieprawidłowy awatar Humanoid");
            // Rig mismatch was logged in build #63. Compare the imported Humanoid
            // and a rebuilt anatomical T-pose against BOTH Walk and Run before
            // baking Legacy clips; preserve the original mesh and bindposes.
            Avatar awatarRuchu = avatar;
            try
            {
                tPoza = PatrykAwatarTPoza(wzor, avatar, model, out string opisTPozy);
                awatarRuchu = PatrykWybierzAwatar(hero, avatar, tPoza, hips, kosci, spoczynek, out string porownanie);
                Debug.Log("BRAXHORN_PATRYK_RETARGET " + opisTPozy + " | " + porownanie + "; chód/bieg: osobna kontrola każdego źródła i awatara");
            }
            catch (Exception e)
            {
                Debug.LogWarning("BRAXHORN_PATRYK_RETARGET T-poza niedostępna, importowany awatar: " + e.Message);
                if (tPoza) { Object.DestroyImmediate(tPoza); tPoza = null; }
            }
            PatrykSpoczynek(spoczynek);
            foreach (var k in KlipyPatryka)
            {
                AnimationClip gotowy = null;
                if (k.nazwa == "Walk" || k.nazwa == "Run")
                {
                    gotowy = PatrykWybierzLokomocje(hero, avatar, tPoza, hips, kosci, spoczynek, k, out string wybor);
                    opis.Add(wybor);
                }
                else foreach (var z in k.zrodla)
                {
                    string sciezka = PatrykRuchy + "/" + z;
                    if (!File.Exists(sciezka)) continue;
                    try
                    {
                        gotowy = PatrykHumanoid(hero, awatarRuchu, hips, kosci, spoczynek, sciezka, k, out string uwagi);
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

            var anim = hero.AddComponent<Animation>(); anim.playAutomatically = false; anim.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (var c in klipy) anim.AddClip(c, c.name);

            // --- test kości: 8 klatek każdego klipu vs spoczynek (> 2 % = błąd), T-poza w Idle/Walk/Run = błąd ---
            int bledy = PatrykTestKosci(hero, kosci, spoczynek, klipy, out string raportTestu);
            Debug.Log("BRAXHORN_PATRYK_KOSCI bledy=" + bledy + " " + raportTestu);
            // E2: test nóg na zapisanych klipach Legacy (to, co gra gracz): 16 klatek chodu i biegu
            foreach (var c in klipy.Where(q => q.name == "Walk" || q.name == "Run"))
            {
                var w = PatrykTestNog(hero, kosci, spoczynek, c);
                bool ok = w.stopaPonad == 0 && w.koloTyl == 0 && w.przeprost == 0 && w.wBok == 0;
                string l = "BRAXHORN_PATRYK_NOGI klip=" + c.name + " awatar=sprawdzony_dla_klipu" + " " + w.Opis() + " cele: stopa<35°, kolano do przodu, przeprost≤10°, kolano w bok≤35° → " + (ok ? "spełnione" : "NIESPEŁNIONE");
                if (ok) Debug.Log(l); else Debug.LogWarning(l + " — DO SPRAWDZENIA W APK");
                // Błąd ze sprawdzonego buildu #41812927670: stopy 159°/174°.
                // Tak duże odchylenie po retargetingu oznacza wadliwą
                // animację — nie wydawaj APK z obróconymi butami.
                if (!ok || w.klatki == 0)
                    throw new InvalidOperationException("BRAXHORN_PATRYK_STOPY_BLOKADA klip=" + c.name +
                        " stopa_max=" + w.stopaMax.ToString("F1") +
                        "°. Ruch nie spełnia kontroli nóg; wstrzymano wadliwą animację.");
            }
            if (bledy > 0) Debug.LogWarning("BRAXHORN_PATRYK_KOSCI DO SPRAWDZENIA: " + bledy + " błędów — " + raportTestu);

            var idleTest = anim.GetClip("Idle");
            idleTest.SampleAnimation(hero, 0f);
            float idleArms = PatrykKatRamion(kosci);
            if (idleArms > 30f)
                throw new InvalidOperationException("Patryk: ramiona w spoczynku uniesione (" + idleArms.ToString("F1") + "°)");
            PatrykSpoczynek(spoczynek);
            Debug.Log("BRAXHORN_PATRYK_IDLE ramiona=" + idleArms.ToString("F1") + "° od pionu (cel <30°)");
            anim.clip = idleTest; anim.Play("Idle"); anim.Sample();

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
            if (tPoza) { Object.DestroyImmediate(tPoza); tPoza = null; }
            return hero;
        }
        catch { if (tPoza) Object.DestroyImmediate(tPoza); Object.DestroyImmediate(hero); throw; }
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
        if (imp == null) throw new InvalidOperationException("Brak tekstury nowego Patryka: " + p);
        var typ = normalna ? TextureImporterType.NormalMap : TextureImporterType.Default; bool srgb = !(liniowa || normalna);
        int limit = normalna || !liniowa ? 4096 : 2048;
        var android = imp.GetPlatformTextureSettings("Android");
        var ignoreLimit = typeof(TextureImporter).GetProperty("ignoreMipmapLimit");
        bool limited = ignoreLimit != null && ignoreLimit.CanRead && !(bool)ignoreLimit.GetValue(imp);
        bool wrongAndroid = !android.overridden || android.maxTextureSize != limit || android.format != TextureImporterFormat.ASTC_4x4 || android.compressionQuality != 50;
        if (imp.textureType != typ || imp.sRGBTexture != srgb || imp.maxTextureSize != limit || !imp.mipmapEnabled
            || imp.textureCompression != TextureImporterCompression.CompressedHQ || imp.crunchedCompression || imp.streamingMipmaps || wrongAndroid || limited || imp.convertToNormalmap)
        {
            imp.textureType = typ; imp.sRGBTexture = srgb; imp.maxTextureSize = limit; imp.mipmapEnabled = true; imp.anisoLevel = 8;
            imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.crunchedCompression = false;
            imp.streamingMipmaps = false; imp.convertToNormalmap = false; imp.npotScale = TextureImporterNPOTScale.None;
            imp.compressionQuality = 50;
            android.overridden = true; android.maxTextureSize = limit; android.format = TextureImporterFormat.ASTC_4x4; android.compressionQuality = 50;
            imp.SetPlatformTextureSettings(android);
            var ign = typeof(TextureImporter).GetProperty("ignoreMipmapLimit"); if (ign != null && ign.CanWrite) ign.SetValue(imp, true);   // postać z bliska — jak tekstury Braxa
            imp.SaveAndReimport();
        }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (!texture || texture.width != limit || texture.height != limit)
            throw new InvalidOperationException("Nieprawidłowa rozdzielczość tekstury Patryka: " + p + ", wymagane " + limit);
        Debug.Log("BRAXHORN_PATRYK_TEKSTURA " + plik + " " + texture.width + "x" + texture.height + " Android ASTC 4x4 jakość 50");
        return texture;
    }

    static Material PatrykMaterial()
    {
        var shader = Shader.Find("Braxhorn/HeroPremium"); if (!shader) throw new Exception("brak shadera Braxhorn/HeroPremium");
        var kolor = PatrykTekstura("Patryk_BaseColor.png", false, false); if (!kolor) throw new Exception("brak Patryk_BaseColor.png");
        var normal = PatrykTekstura("Patryk_Normal.png", false, true);
        var orm = PatrykTekstura("Patryk_ORM.png", true, false);
        // Ten sam shader co Brax; nowe źródłowe kolory, normalne i kanały PBR bez tłumienia.
        var mat = new Material(shader) { name = "Patryk PBR (ORM)", color = Color.white };
        mat.SetTexture("_MainTex", kolor);
        if (normal) mat.SetTexture("_BumpMap", normal);
        if (orm) mat.SetTexture("_Orm", orm);
        else mat.SetFloat("_MetallicScale", 0f);   // bez ORM (biała): bez metalu; chropowatość z dolnej granicy
        mat.SetFloat("_BumpScale", 1f); mat.SetFloat("_RoughnessFloor", 0f); mat.SetFloat("_RoughnessCeiling", 1f);
        mat.SetFloat("_MetallicScale", 1f); mat.SetFloat("_OcclusionStrength", 1f);
        string sc = PatrykWynik + "/PatrykORM.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(sc);
        if (existing) { EditorUtility.CopySerialized(mat, existing); Object.DestroyImmediate(mat); return existing; }
        AssetDatabase.CreateAsset(mat, sc); return mat;
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


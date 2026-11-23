using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// NOWY PATRYK W MIEŚCIE MISTRZÓW (właściciel 23.11, 15:47 i 15:54: „Patryka dałeś złą wersję, to nie jest ten najnowszy,
// to nie są jego ruchy, ani ta postać, to jest stara do wyrzucenia” — „dać nowego”).
//
// Ten sam Patryk co na Osiedlu: globalna biblioteka postaci, Patryk z 13.11 (szkic wydania postacie-globalne-20261112,
// Patryk_globalny_13-11_Unity.zip), szkielet Mixamo 65 kości, 31 ruchów zrobionych przez właściciela pod tę postać, mapy 2K.
// CI kładzie go do Assets/Source/Mistrzowie/Player/Postacie/patryk (unity-tools/mistrzowie/restore_patryk_globalny.py,
// każda suma SHA-256 sprawdzona). Ruchy idą PROSTO na jego kości, bez Humanoid — tą samą metodą co na Osiedlu
// (BraxClaudePatrykBezposrednio na gałęzi Osiedla): w każdej klatce obrót kości w świecie = obrót kości źródła × poza
// spoczynkowa, biodra przeskalowane do wysokości bioder Patryka. Kości, długości i bindposes bez zmian.
// Wzrost bez zmian: 1,80 m = 2,33 w grze. Gra (BraxGymPlayer) gra klipy Legacy Idle/Walk/Run/Punch/Kick/Jump/CombatIdle
// oraz EnteringCar/Driving/ExitingCar w aucie i helikopterze; Driving = jego CarIdle (siedzenie w aucie z paczki).
public static partial class BraxChampionsPatryk
{
    public const string GlobalnyKatalog = PrywatneZrodla + "/Postacie/patryk";
    const string GlobalnyModelSha = "eacee0beb02fac53587337edee09f790577a700464448bf797d2ed65a2c66d1f";
    public static bool GlobalnyJest => File.Exists(GlobalnyKatalog + "/Patryk.fbx");

    const string Ruchy = "Ruchy/";
    static readonly PatrykKlip[] KlipyGlobalne =
    {
        PK("Walk", true, true, Ruchy + "Walk.fbx"),
        PK("Run", true, true, Ruchy + "Run.fbx"),
        PK("Jump", false, true, Ruchy + "Jump.fbx"),
        PK("EnteringCar", false, true, Ruchy + "EnteringCar.fbx"),
        PK("CarIdle", true, true, Ruchy + "CarIdle.fbx"),
        PK("ExitingCar", false, true, Ruchy + "ExitingCar.fbx"),
        PK("CombatIdle", true, true, Ruchy + "CombatIdle.fbx"),
        PK("CombatForward", true, true, Ruchy + "CombatForward.fbx"),
        PK("CombatBackward", true, true, Ruchy + "CombatBackward.fbx"),
        PK("CombatLeft", true, true, Ruchy + "CombatLeft.fbx"),
        PK("Punch", false, true, Ruchy + "Punch.fbx"),
        PK("Punch2", false, true, Ruchy + "Punch2.fbx"),
        PK("Punch3", false, true, Ruchy + "Punch3.fbx"),
        PK("Kick", false, true, Ruchy + "Kick.fbx"),
        PK("Kick2", false, true, Ruchy + "Kick2.fbx"),
        PK("Kick3", false, true, Ruchy + "Kick3.fbx"),
        PK("Dodge", false, true, Ruchy + "Dodge.fbx"),
        PK("Block", false, true, Ruchy + "Block.fbx"),
        PK("HitHead", false, true, Ruchy + "HitHead.fbx"),
        PK("HitBody", false, true, Ruchy + "HitBody.fbx"),
        PK("Knockdown", false, true, Ruchy + "Knockdown.fbx"),
        PK("GetUp", false, true, Ruchy + "GetUp.fbx"),
        PK("Bow", false, true, Ruchy + "Bow.fbx"),
        PK("Taunt", false, true, Ruchy + "Taunt.fbx"),
        PK("Victory", false, true, Ruchy + "Victory.fbx"),
        PK("Defeat", false, true, Ruchy + "Defeat.fbx"),
        PK("WarmUp", false, true, Ruchy + "WarmUp.fbx"),
        PK("Roll", false, true, Ruchy + "Roll.fbx"),
        PK("GolfDrive", false, true, Ruchy + "GolfDrive.fbx"),
        PK("GolfPutt", false, true, Ruchy + "GolfPutt.fbx"),
        PK("Speedbag", true, true, Ruchy + "Speedbag.fbx"),
    };
    // what the game plays in Miasto Mistrzów (BraxGymPlayer + the car and the helicopter)
    static readonly string[] GlobalnyWymagane = { "Idle", "Walk", "Run", "Jump", "Punch", "Kick", "CombatIdle", "EnteringCar", "CarIdle", "Driving", "ExitingCar" };

    static GameObject PatrykZbudujGlobalny(Transform gracz, CharacterController cialo)
    {
        string model = GlobalnyKatalog + "/Patryk.fbx";
        using (var hash = System.Security.Cryptography.SHA256.Create())
        using (var stream = File.OpenRead(model))
            if (BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != GlobalnyModelSha)
                throw new InvalidOperationException("Nieprawidłowy model: wymagany Patryk z 13.11.2026 (globalna biblioteka postaci)");
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
            // --- wzrost w pozie spoczynkowej (wierzchołki siatki = poza wiązania, T) ---
            var v = smr.sharedMesh.vertices; var m = smr.transform.localToWorldMatrix;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in v) { float y = m.MultiplyPoint3x4(p).y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
            if (v.Length == 0) { var b = smr.bounds; minY = b.min.y; maxY = b.max.y; }
            float h0 = maxY - minY;
            if (!(h0 > .05f)) throw new Exception("niepoprawna wysokość modelu " + h0);
            float skala = WzrostPatrykaWGrze / h0, wzrost = WzrostPatrykaWGrze;   // 1,80 m = 2,33, bez zmiany proporcji

            var kosci = hips.GetComponentsInChildren<Transform>(true).ToList();   // od korzenia w dół (rodzic przed dzieckiem)
            var spoczynek = kosci.Select(t => new PatrykStan { t = t, r = t.localRotation, p = t.localPosition }).ToList();
            float ramiona = PatrykKatRamion(kosci);

            if (!AssetDatabase.IsValidFolder(PatrykWynik))
            {
                if (AssetDatabase.IsValidFolder("Assets/PlayableGenerated/Mistrzowie")) AssetDatabase.CreateFolder("Assets/PlayableGenerated/Mistrzowie", "Patryk");
                else { Directory.CreateDirectory(PatrykWynik); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); }
            }
            var mat = PatrykMaterialGlobalny();
            foreach (var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int n = r.sharedMesh ? Mathf.Max(1, r.sharedMesh.subMeshCount) : Mathf.Max(1, r.sharedMaterials.Length);
                r.quality = SkinQuality.Bone4;
                r.sharedMaterials = Enumerable.Repeat(mat, n).ToArray(); r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.On;
            }

            // --- klipy: Idle z kodu (w paczce nie ma stania), 31 ruchów właściciela prosto na kości ---
            var klipy = new List<AnimationClip> { PatrykSpokojny(hero, spoczynek) };
            var opis = new List<string>();
            foreach (var k in KlipyGlobalne)
            {
                string sciezka = GlobalnyKatalog + "/" + k.zrodla[0];
                var gotowy = PatrykBezposrednio(hero, hips, kosci, spoczynek, sciezka, k, out string uwagi);
                if (!gotowy) throw new InvalidOperationException("Brak wymaganego ruchu Patryka: " + k.nazwa);
                klipy.Add(gotowy); opis.Add(k.nazwa + uwagi);
                PatrykSpoczynek(spoczynek);
            }
            // the region's car and the helicopter play "Driving" in the seat: his own sitting-in-the-car motion
            var jazda = Object.Instantiate(klipy.First(c => c.name == "CarIdle")); jazda.name = "Driving"; jazda.wrapMode = WrapMode.Loop;
            ZapiszKlipPatryka(jazda);
            klipy.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/Driving.anim"));
            var brak = GlobalnyWymagane.Where(n => !klipy.Any(c => c && c.name == n)).ToArray();
            if (brak.Length > 0) throw new Exception("brak wymaganych klipów: " + string.Join(", ", brak));

            var anim = hero.AddComponent<Animation>(); anim.playAutomatically = false; anim.cullingType = AnimationCullingType.AlwaysAnimate;
            foreach (var c in klipy) anim.AddClip(c, c.name);

            // --- test kości: 8 klatek każdego klipu vs spoczynek (> 2 % = błąd), T-poza w Idle/Walk/Run = błąd ---
            int bledy = PatrykTestKosci(hero, kosci, spoczynek, klipy, out string raportTestu);
            Debug.Log("BRAXHORN_PATRYK_KOSCI bledy=" + bledy + " " + raportTestu);
            foreach (var c in klipy.Where(q => q.name == "Walk" || q.name == "Run"))
            {
                var w = PatrykTestNog(hero, kosci, spoczynek, c);
                bool ok = w.stopaPonad == 0 && w.koloTyl == 0 && w.przeprost == 0 && w.wBok == 0;
                string l = "BRAXHORN_PATRYK_NOGI klip=" + c.name + " globalny 13.11 " + w.Opis() + " cele: stopa<35°, kolano do przodu, przeprost≤10°, kolano w bok≤35° → " + (ok ? "spełnione" : "NIESPEŁNIONE");
                if (ok) Debug.Log(l); else Debug.LogWarning(l + " — DO SPRAWDZENIA W APK");
            }
            if (bledy > 0) throw new InvalidOperationException("Patryk: błędy kości przy odtwarzaniu: " + raportTestu);

            var idleTest = anim.GetClip("Idle");
            idleTest.SampleAnimation(hero, 0f);
            float idleArms = PatrykKatRamion(kosci);
            if (idleArms > 30f) throw new InvalidOperationException("Patryk: ramiona w spoczynku uniesione (" + idleArms.ToString("F1") + "°)");
            PatrykSpoczynek(spoczynek);
            Debug.Log("BRAXHORN_PATRYK_IDLE ramiona=" + idleArms.ToString("F1") + "° od pionu (cel <30°)");
            anim.clip = idleTest; anim.Play("Idle"); anim.Sample();

            // stopy na ziemi: podeszwy policzone ze skóry w pozie Idle, nie z T-pozy pliku
            float idleFloor = float.PositiveInfinity;
            var snapshot = new Mesh();
            try
            {
                foreach (var skin in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    snapshot.Clear(); skin.BakeMesh(snapshot, false);
                    foreach (var vertex in snapshot.vertices)
                        idleFloor = Mathf.Min(idleFloor, hero.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex)).y);
                }
            }
            finally { Object.DestroyImmediate(snapshot); }
            if (float.IsInfinity(idleFloor) || float.IsNaN(idleFloor)) throw new InvalidOperationException("Brak pomiaru podeszew w Idle");

            hero.transform.SetParent(gracz, false);
            hero.transform.localRotation = Quaternion.identity;
            hero.transform.localScale = Vector3.one * skala;
            hero.transform.localPosition = new Vector3(0f, -idleFloor * skala, 0f);
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 8;
            if (cialo)
            {
                cialo.height = wzrost + .04f; cialo.radius = Mathf.Clamp(wzrost * .15f, .26f, .35f);
                cialo.center = new Vector3(0f, cialo.height * .5f, 0f);
                if (cialo.stepOffset > cialo.height * .25f) cialo.stepOffset = cialo.height * .25f;
            }
            WzrostZModelu = wzrost;

            Debug.Log("BRAXHORN_PATRYK model=globalny 13.11 " + Path.GetFileName(model) + " (" + v.Length + " wierzch., " + kosci.Count + " kości, awatar "
                + (avatar ? "isHuman=" + avatar.isHuman + " isValid=" + avatar.isValid : "brak") + ", poza pliku: ramiona " + ramiona.ToString("F0") + "° od pionu)"
                + " wzrost=" + wzrost.ToString("F2") + " (model " + h0.ToString("F2") + " ×" + skala.ToString("F3") + ", Brax " + WzrostBraxa.ToString("F2") + ")"
                + " kapsuła=" + (cialo ? cialo.height.ToString("F2") + "×" + cialo.radius.ToString("F2") : "—")
                + " klipy=" + klipy.Count + " [" + string.Join(", ", opis) + "] bledy_kosci=" + bledy);
            return hero;
        }
        catch { Object.DestroyImmediate(hero); throw; }
    }

    static AnimationClip PatrykKlipZrodla(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).OrderByDescending(c => c.length).FirstOrDefault();

    static Dictionary<string, Transform> PatrykKosciPoNazwie(IEnumerable<Transform> ts)
    {
        var d = new Dictionary<string, Transform>();
        foreach (var t in ts) { var k = PatrykKosc(t.name); if (!d.ContainsKey(k)) d[k] = t; }
        return d;
    }

    /// <summary>Ruch właściciela (Generic, ten sam szkielet Mixamo) → klip Legacy Patryka bez konwersji Humanoid
    /// (metoda Osiedla z 10.11: Humanoid psuł stopy, kolana i dłonie ruchów zrobionych pod Patryka).</summary>
    static AnimationClip PatrykBezposrednio(GameObject hero, Transform hips, List<Transform> bones, List<PatrykStan> rest,
        string path, PatrykKlip spec, out string info)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var clip = PatrykKlipZrodla(path);
        if (!prefab || !clip || clip.length <= 0f) throw new InvalidOperationException("Brak klipu w " + path);
        if (clip.humanMotion) throw new InvalidOperationException("Ruch właściciela zaimportowany jako Humanoid (oczekiwany Generic): " + path);
        var source = Object.Instantiate(prefab);
        try
        {
            source.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); source.transform.localScale = Vector3.one;
            foreach (var a in source.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var src = PatrykKosciPoNazwie(source.GetComponentsInChildren<Transform>(true));
            if (!src.TryGetValue("Hips", out var sHips) || !src.TryGetValue("LeftFoot", out var sFoot))
                throw new InvalidOperationException("Źródło bez kości Hips/LeftFoot: " + path);
            var S0 = src.ToDictionary(p => p.Key, p => p.Value.rotation);   // poza spoczynkowa źródła (przed próbkowaniem)
            Vector3 sh0 = sHips.position; float hSrc = sh0.y - sFoot.position.y;

            PatrykSpoczynek(rest);
            var tFoot = PatrykZnajdz(bones, "LeftFoot");
            if (!tFoot) throw new InvalidOperationException("Patryk bez kości LeftFoot");
            var T0 = bones.ToDictionary(b => b, b => b.rotation);
            Vector3 hipsRest = hips.localPosition; float hTgt = hips.position.y - tFoot.position.y;
            float skala = hSrc > 1e-5f ? hTgt / hSrc : 1f;

            int count = Mathf.CeilToInt(clip.length * 30f) + 1, dopasowane = 0;
            var times = new float[count];
            var rotations = bones.Select(_ => new Quaternion[count]).ToArray();
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Min(i / 30f, clip.length); times[i] = t;
                clip.SampleAnimation(source, t);
                PatrykSpoczynek(rest);
                int n = 0;
                foreach (var b in bones)    // rodzic przed dzieckiem
                {
                    if (!src.TryGetValue(PatrykKosc(b.name), out var s) || !S0.TryGetValue(PatrykKosc(b.name), out var s0)) continue;
                    b.rotation = s.rotation * Quaternion.Inverse(s0) * T0[b]; n++;
                }
                if (i == 0) dopasowane = n;
                var d = (sHips.position - sh0) * skala;
                if (spec.wMiejscu) { d.x = 0f; d.z = 0f; }
                hips.localPosition = hipsRest + (hips.parent ? hips.parent.InverseTransformVector(d) : d);
                for (int b = 0; b < bones.Count; b++)
                {
                    var q = bones[b].localRotation;
                    if (i > 0 && Quaternion.Dot(q, rotations[b][i - 1]) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    rotations[b][i] = q;
                }
                positions[i] = hips.localPosition;
            }
            if (dopasowane != bones.Count) throw new InvalidOperationException("Szkielet źródła nie pasuje do Patryka: " + dopasowane + " kości wspólnych (" + path + ")");
            var result = new AnimationClip { name = spec.nazwa, legacy = true, frameRate = 30, wrapMode = spec.petla ? WrapMode.Loop : WrapMode.Once };
            for (int b = 0; b < bones.Count; b++)
            {
                string bonePath = AnimationUtility.CalculateTransformPath(bones[b], hero.transform);
                for (int axis = 0; axis < 4; axis++)
                    result.SetCurve(bonePath, typeof(Transform), "localRotation." + "xyzw"[axis], PatrykKrzywaZ(times, rotations[b].Select(q => q[axis]).ToArray(), false));
            }
            string hipsPath = AnimationUtility.CalculateTransformPath(hips, hero.transform);
            for (int axis = 0; axis < 3; axis++)
                result.SetCurve(hipsPath, typeof(Transform), "localPosition." + "xyz"[axis], PatrykKrzywaZ(times, positions.Select(p => p[axis]).ToArray(), false));
            result.EnsureQuaternionContinuity();
            ZapiszKlipPatryka(result);
            info = " bezpośrednio na kości Patryka (bez Humanoid), " + count + " próbek, kości wspólnych " + dopasowane + ", biodra ×" + skala.ToString("F3");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(PatrykWynik + "/" + spec.nazwa + ".anim");
        }
        finally { Object.DestroyImmediate(source); PatrykSpoczynek(rest); }
    }

    // Mapy 2K z paczki (źródła są 2K): kolor i normalne ASTC 4×4 (twarz jest na tej samej mapie co ciało), ORM 6×6 (ZASADY 3.2)
    static Texture2D PatrykTeksturaGlobalna(string plik, bool liniowa, bool normalna)
    {
        string p = GlobalnyKatalog + "/" + plik;
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp == null) throw new InvalidOperationException("Brak tekstury nowego Patryka: " + p);
        var typ = normalna ? TextureImporterType.NormalMap : TextureImporterType.Default; bool srgb = !(liniowa || normalna);
        const int limit = 2048;
        var format = liniowa ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
        var android = imp.GetPlatformTextureSettings("Android");
        bool wrongAndroid = !android.overridden || android.maxTextureSize != limit || android.format != format || android.compressionQuality != 50;
        if (imp.textureType != typ || imp.sRGBTexture != srgb || imp.maxTextureSize != limit || !imp.mipmapEnabled
            || imp.textureCompression != TextureImporterCompression.CompressedHQ || imp.crunchedCompression || !imp.streamingMipmaps || wrongAndroid)
        {
            imp.textureType = typ; imp.sRGBTexture = srgb; imp.maxTextureSize = limit; imp.mipmapEnabled = true; imp.anisoLevel = 8;
            imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.crunchedCompression = false;
            imp.streamingMipmaps = true; imp.streamingMipmapsPriority = 100; imp.npotScale = TextureImporterNPOTScale.None;   // ZASADY A.5, the hero first
            imp.compressionQuality = 50;
            android.overridden = true; android.maxTextureSize = limit; android.format = format; android.compressionQuality = 50;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
        }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (!texture || texture.width != limit || texture.height != limit)
            throw new InvalidOperationException("Nieprawidłowa rozdzielczość tekstury Patryka: " + p + ", wymagane " + limit);
        Debug.Log("BRAXHORN_PATRYK_TEKSTURA " + plik + " " + texture.width + "x" + texture.height + " Android " + format + " jakość 50");
        return texture;
    }

    static Material PatrykMaterialGlobalny()
    {
        var shader = Shader.Find("Braxhorn/ChampionsPatryk"); if (!shader) throw new Exception("brak shadera Braxhorn/ChampionsPatryk");
        var kolor = PatrykTeksturaGlobalna("Patryk_BaseColor.png", false, false);
        var normal = PatrykTeksturaGlobalna("Patryk_Normal.png", false, true);
        var orm = PatrykTeksturaGlobalna("Patryk_ORM.png", true, false);
        // jak na Osiedlu (właściciel 10.11, 18:40: „ma wyglądać Patryk tak jak go dostarczyłem, żadnego plastiku”):
        // chropowatość z mapy przycięta do 0,78–0,96, bez metalu (kanał metalu Tripo to szum na skórze i tkaninie)
        var mat = new Material(shader) { name = "Patryk PBR (ORM) globalny 13.11", color = Color.white };
        mat.SetTexture("_MainTex", kolor);
        mat.SetTexture("_BumpMap", normal);
        mat.SetTexture("_Orm", orm);
        mat.SetFloat("_BumpScale", 1f); mat.SetFloat("_RoughnessFloor", .78f); mat.SetFloat("_RoughnessCeiling", .96f);
        mat.SetFloat("_MetallicScale", 0f); mat.SetFloat("_OcclusionStrength", 1f);
        string sc = PatrykWynik + "/PatrykORM.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(sc);
        if (existing) { EditorUtility.CopySerialized(mat, existing); Object.DestroyImmediate(mat); return existing; }
        AssetDatabase.CreateAsset(mat, sc); return mat;
    }
}

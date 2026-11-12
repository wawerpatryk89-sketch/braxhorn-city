using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// POSTACIE GLOBALNE — jedne na całą grę (właściciel 12.11.2026 16:57: „Postacie będą musiały i tak być globalnie zrobione,
// te same pod całą grę”; 16:48: „są animacje T dla nich, wystarczy tylko dodać, bez kombinowania”; 16:54: „Brax nie ruszaj”).
//
// Źródła (prywatne, repo publiczne): paczki unity-city/Prywatne/braxhorn-postacie-*/ (zaszyfrowane; CI otwiera je przed Unity —
// unity-tools/postacie/install_postacie.py) → Assets/Source/Prywatne/postacie (poza gitem):
//   <id>/<id>.fbx                  model z Tripo, szkielet Mixamo 65 kości, poza T (import Legacy — BraxPrywatneImport)
//   <id>/<id>_{BaseColor.jpg,Normal.png,ORM.png}
//   _ruchy_T/<Klucz>.fbx           klipy Mixamo bez skóry z paczki właściciela „pozycja T animacje” (ten sam szkielet)
//
// Klipy kładzione są WPROST na kości postaci tym samym, sprawdzonym przenoszeniem co u Patryka (PatrykPrzenies: obroty lokalne
// + wysokość bioder w skali postaci; długości kości nie mogą się zmienić — test kości, próg 2 %). Żadnego mostu Humanoid →
// Legacy z obozu: to on deformował zawodników. Jakikolwiek błąd (brak paczki, klucza, klipu, odchyłka kości) = postać NIE
// powstaje i build idzie dalej (ostrzeżenie BRAXHORN_POSTACIE …) — zdeformowany zawodnik nigdy nie trafia do gry.
public static partial class BraxClaudeOsiedle
{
    const string PostacieZrodla = PrywatneZrodla + "/postacie", PostacieRuchy = PostacieZrodla + "/_ruchy_T", PostacieWynik = "Assets/PlayableGenerated/Postacie";

    // klucz klipu = nazwa pliku w _ruchy_T i nazwa klipu na komponencie Animation (BraxArenaWalka gra je po tych nazwach)
    static readonly (string klucz, bool petla)[] KlipyWalki =
    {
        ("Garda", true), ("KrokP", false), ("KrokT", false), ("KrokB", false), ("KrokB2", false),
        ("JabCross", false), ("JabCross2", false), ("Cross", false), ("Seria", false), ("SeriaDluga", false), ("Jab", false), ("Korpus", false),
        ("Prosty", false), ("Sierp", false), ("Dol", false),
        ("Kop", false), ("KopWysoki", false), ("KopFront", false), ("KopPchniecie", false), ("KopOkrezny", false), ("KopBoczny", false),
        ("GlowaLekko", false), ("Glowa", false), ("GlowaSrednio", false), ("GlowaMocno", false), ("Brzuch", false), ("Bok", false),
        ("Blok", false), ("BlokL", false), ("BlokP", false), ("BlokDlugi", false), ("Unik", false), ("Zaczepka", false),
    };
    static readonly string[] KlipyWalkiWymagane = { "Garda", "KrokP", "KrokT", "KrokB", "JabCross", "Glowa", "GlowaLekko", "Brzuch", "Blok" };

    public static bool PostacJest(string id) => File.Exists(PostacieZrodla + "/" + id + "/" + id + ".fbx") && Directory.Exists(PostacieRuchy);

    static void PostacieFolder(string sciezka)
    {
        if (AssetDatabase.IsValidFolder(sciezka)) return;
        string rodzic = Path.GetDirectoryName(sciezka).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(rodzic)) PostacieFolder(rodzic);
        AssetDatabase.CreateFolder(rodzic, Path.GetFileName(sciezka));
    }

    static Texture2D PostacTekstura(string sciezka, bool liniowa, bool normalna)
    {
        var imp = AssetImporter.GetAtPath(sciezka) as TextureImporter;
        if (imp == null) return null;
        var typ = normalna ? TextureImporterType.NormalMap : TextureImporterType.Default; bool srgb = !(liniowa || normalna);
        if (imp.textureType != typ || imp.sRGBTexture != srgb || imp.maxTextureSize > 2048 || !imp.mipmapEnabled)
        {
            imp.textureType = typ; imp.sRGBTexture = srgb; imp.maxTextureSize = Mathf.Min(imp.maxTextureSize, 2048); imp.mipmapEnabled = true; imp.anisoLevel = 8;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(sciezka);
    }

    static Material PostacMaterial(string id, string katalogWyniku)
    {
        var shader = Shader.Find("Braxhorn/HeroPremium"); if (!shader) throw new Exception("brak shadera Braxhorn/HeroPremium");
        string b = PostacieZrodla + "/" + id + "/" + id;
        var kolor = PostacTekstura(b + "_BaseColor.jpg", false, false) ?? PostacTekstura(b + "_BaseColor.png", false, false);
        if (!kolor) throw new Exception("brak tekstury koloru " + id + "_BaseColor");
        var normal = PostacTekstura(b + "_Normal.png", false, true);
        var orm = PostacTekstura(b + "_ORM.png", true, false);
        // ustawienia jak materiał Patryka (skóra / sierść / tkanina: mat, bez połysku metalu)
        var mat = new Material(shader) { name = "Postać " + id + " PBR (ORM)", color = Color.white };
        mat.SetTexture("_MainTex", kolor);
        if (normal) mat.SetTexture("_BumpMap", normal);
        if (orm) mat.SetTexture("_Orm", orm); else mat.SetFloat("_MetallicScale", 0f);
        mat.SetFloat("_BumpScale", normal ? .85f : 0f); mat.SetFloat("_RoughnessFloor", .42f); if (orm) mat.SetFloat("_MetallicScale", .30f); mat.SetFloat("_OcclusionStrength", .8f);
        string sc = katalogWyniku + "/" + id + "_ORM.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(sc)) AssetDatabase.DeleteAsset(sc);
        BraxZapisAssetu.Utworz(mat, sc);
        return mat;
    }

    /// <summary>Postać globalna z kompletem klipów walki: dziecko <paramref name="rodzic"/> (stopy w jego punkcie, przodem w +Z),
    /// komponent Animation (Legacy) z klipami KlipyWalki. null = postaci nie ma (powód w logu), build idzie dalej.</summary>
    public static GameObject PostacZawodnik(string id, string nazwa, Transform rodzic, float wzrost, out Animation animacja)
    {
        animacja = null;
        GameObject hero = null;
        try
        {
            string model = PostacieZrodla + "/" + id + "/" + id + ".fbx";
            if (!File.Exists(model)) { Debug.Log("BRAXHORN_POSTACIE " + id + ": brak modelu (paczka braxhorn-postacie-* nieodszyfrowana — sekret KLUCZ?)"); return null; }
            if (!Directory.Exists(PostacieRuchy)) { Debug.LogWarning("BRAXHORN_POSTACIE " + id + ": brak klipów " + PostacieRuchy); return null; }
            var wzor = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (!wzor) { Debug.LogWarning("BRAXHORN_POSTACIE " + id + ": model nie zaimportowany " + model); return null; }

            hero = Object.Instantiate(wzor); hero.name = nazwa;
            hero.transform.position = Vector3.zero; hero.transform.rotation = Quaternion.identity; hero.transform.localScale = Vector3.one;
            foreach (var a in hero.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(a);
            foreach (var a in hero.GetComponentsInChildren<Animation>(true)) Object.DestroyImmediate(a);
            var smr = hero.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.sharedMesh).OrderByDescending(r => r.sharedMesh.vertexCount).FirstOrDefault();
            if (!smr) throw new Exception("brak siatki ze szkieletem w " + model);
            var hips = hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => PatrykKosc(t.name) == "Hips");
            if (!hips) throw new Exception("brak kości Hips");
            // rodzic bioder z osią Y w górę (modele Tripo mają wysokość w osi Z — import obraca węzeł szkieletu)
            if (!hips.parent || Vector3.Angle(hips.parent.up, Vector3.up) > .5f)
            {
                var k = new GameObject(nazwa + " szkielet"); k.transform.SetParent(hero.transform, false); hips.SetParent(k.transform, true);
            }
            var kosci = hips.GetComponentsInChildren<Transform>(true).ToList();   // od korzenia w dół (rodzic przed dzieckiem)
            if (kosci.Count < 50) throw new Exception("za mało kości (" + kosci.Count + ")");
            var spoczynek = kosci.Select(t => new PatrykStan { t = t, r = t.localRotation, p = t.localPosition }).ToList();

            // --- wzrost w pozie spoczynkowej z KOŚCI (pewne niezależnie od osi siatki): czubek głowy − podeszwa ---
            Transform Kosc(string n) => PatrykZnajdz(kosci, n);
            var czubek = Kosc("HeadTop_End") ?? Kosc("Head"); if (!czubek) throw new Exception("brak kości głowy");
            float stopy = float.MaxValue;
            foreach (var n in new[] { "LeftToeBase", "RightToeBase", "LeftFoot", "RightFoot", "LeftToe_End", "RightToe_End" }) { var t = Kosc(n); if (t) stopy = Mathf.Min(stopy, t.position.y); }
            if (stopy == float.MaxValue) throw new Exception("brak kości stóp");
            float hk = czubek.position.y - stopy;
            if (!(hk > .05f)) throw new Exception("niepoprawna wysokość szkieletu " + hk);
            float minY = stopy - hk * .025f, h0 = hk * 1.045f;   // podeszwa buta pod kością palców, czubek głowy / uszy nad kością
            float skala = wzrost / h0;
            float ramiona = PatrykKatRamion(kosci);

            string katalog = PostacieWynik + "/" + id;
            PostacieFolder(katalog);
            var mat = PostacMaterial(id, katalog);
            foreach (var r in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int n = r.sharedMesh ? Mathf.Max(1, r.sharedMesh.subMeshCount) : Mathf.Max(1, r.sharedMaterials.Length);
                r.sharedMaterials = Enumerable.Repeat(mat, n).ToArray(); r.updateWhenOffscreen = true; r.shadowCastingMode = ShadowCastingMode.Off;
            }

            // --- klipy walki wprost na kości tej postaci ---
            var klipy = new List<AnimationClip>(); var braki = new List<string>();
            foreach (var (klucz, petla) in KlipyWalki)
            {
                string sciezka = PostacieRuchy + "/" + klucz + ".fbx";
                if (!File.Exists(sciezka)) { braki.Add(klucz); continue; }
                try { klipy.Add(PatrykPrzenies(hero, hips, kosci, spoczynek, minY, sciezka, PK(klucz, petla, true), out _, katalog)); }
                catch (Exception e) { braki.Add(klucz); Debug.LogWarning("BRAXHORN_POSTACIE " + id + " klip " + klucz + ": " + e.Message); }
                finally { PatrykSpoczynek(spoczynek); }
            }
            var brak = KlipyWalkiWymagane.Where(n => !klipy.Any(c => c.name == n)).ToArray();
            if (brak.Length > 0) throw new Exception("brak wymaganych klipów: " + string.Join(", ", brak));

            // --- ZERO deformacji: 8 klatek każdego klipu, długość każdej kości = spoczynek (±2 %) ---
            int bledy = PatrykTestKosci(hero, kosci, spoczynek, klipy, out string raportTestu);
            if (bledy > 0) throw new Exception("test kości: " + bledy + " błędów — " + raportTestu);

            var anim = hero.AddComponent<Animation>(); anim.playAutomatically = false;
            foreach (var c in klipy) anim.AddClip(c, c.name);
            anim.clip = anim.GetClip("Garda"); anim.Play("Garda"); anim.Sample();

            hero.transform.SetParent(rodzic, false);
            hero.transform.localRotation = Quaternion.identity;
            hero.transform.localScale = Vector3.one * skala;
            hero.transform.localPosition = new Vector3(0f, -minY * skala, 0f);
            foreach (var t in hero.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 0; GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); }
            animacja = anim;
            Debug.Log("BRAXHORN_POSTACIE " + id + " (" + nazwa + "): kości=" + kosci.Count + ", poza: ramiona " + ramiona.ToString("F0") + "° od pionu" + (ramiona > 75f ? " = T" : " (nie T)")
                + ", wzrost " + wzrost.ToString("F2") + " m (szkielet " + hk.ToString("F2") + " ×" + skala.ToString("F3") + "), klipy=" + klipy.Count + "/" + KlipyWalki.Length
                + (braki.Count > 0 ? " bez: " + string.Join(", ", braki) : "") + ", test kości: 0 błędów " + raportTestu);
            return hero;
        }
        catch (Exception e)
        {
            Debug.LogWarning("BRAXHORN_POSTACIE " + id + " POMINIĘTA (zdeformowana albo niekompletna postać nie trafia do gry): " + e.Message);
            if (hero) Object.DestroyImmediate(hero);
            animacja = null;
            return null;
        }
    }

    /// <summary>Walka dwóch postaci globalnych na macie (środek <paramref name="srodekMaty"/> = poziom maty, promień w metrach).
    /// false = brak postaci albo błąd (nic nie zostaje w scenie), build idzie dalej.</summary>
    public static bool WalkaNaMacie(Transform korzen, Vector3 srodekMaty, float promien, float skalaSwiata, string idA, string nazwaA, string idB, string nazwaB)
    {
        if (!PostacJest(idA) || !PostacJest(idB))
        {
            Debug.Log("BRAXHORN_ARENA_WALKA pominięta: brak postaci globalnych " + idA + " / " + idB + " (paczka braxhorn-postacie-* albo sekret KLUCZ)");
            return false;
        }
        GameObject holder = null;
        try
        {
            float wzrost = 1.8f * skalaSwiata;   // świat w skali Braxa (k = 1.294): zawodnik 1.8 m → 2.33 m
            holder = new GameObject("BRAXHORN ARENA: walka " + nazwaA + " / " + nazwaB + " (postacie globalne)"); holder.transform.SetParent(korzen, false);
            var walka = holder.AddComponent<BraxArenaWalka>();
            walka.srodek = srodekMaty; walka.promien = promien; walka.wzrost = wzrost;
            var ids = new[] { idA, idB }; var nazwy = new[] { nazwaA, nazwaB };
            for (int i = 0; i < 2; i++)
            {
                var zaw = new GameObject("Zawodnik: " + nazwy[i]); zaw.transform.SetParent(holder.transform, false);
                zaw.transform.position = srodekMaty + Vector3.forward * (i == 0 ? -.36f : .36f) * wzrost;
                zaw.transform.rotation = Quaternion.Euler(0, i == 0 ? 0 : 180, 0);
                var postac = PostacZawodnik(ids[i], nazwy[i], zaw.transform, wzrost, out var anim);
                if (!postac || !anim) throw new Exception("postać " + ids[i] + " nie powstała");
                anim.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (var smr in postac.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!smr.sharedMesh || smr.bones.Length == 0 || smr.bones.Any(b => !b)) throw new Exception("niepełny szkielet " + smr.name);
                    smr.updateWhenOffscreen = false;
                    // granice na cały zasięg ciosów (bez przeliczania co klatkę): sześcian wzrostu wokół bioder
                    var lb = smr.localBounds; float s = Mathf.Max(lb.size.x, lb.size.y, lb.size.z) * 1.6f; smr.localBounds = new Bounds(lb.center, new Vector3(s, s, s));
                }
                var kaps = zaw.AddComponent<CapsuleCollider>(); kaps.radius = .34f * skalaSwiata; kaps.height = wzrost; kaps.center = Vector3.up * wzrost * .5f;
                var cialo = zaw.AddComponent<Rigidbody>(); cialo.isKinematic = true; cialo.useGravity = false;
                if (i == 0) { walka.zawodnikA = zaw.transform; walka.animacjaA = anim; } else { walka.zawodnikB = zaw.transform; walka.animacjaB = anim; }
            }
            Debug.Log("BRAXHORN_ARENA_WALKA gotowa: " + nazwaA + " kontra " + nazwaB + " na macie " + srodekMaty.ToString("F1") + ", promień " + promien.ToString("F1") + " m, wzrost " + wzrost.ToString("F2") + " m");
            return true;
        }
        catch (Exception e)
        {
            if (holder) Object.DestroyImmediate(holder);
            Debug.LogWarning("BRAXHORN_ARENA_WALKA DO POPRAWKI (pominięta, klatka bez zawodników): " + e.Message);
            return false;
        }
    }
}

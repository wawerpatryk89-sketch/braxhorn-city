using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// LUDZIE NA OSIEDLU — BUILD 14 (zlecenie „ludzie”; audyt: LUDZIE WSZĘDZIE — OBSADA, SKALA WZGLĘDEM BOHATERA,
// BEZ DEFORMACJI POSTACI, KIEROWCY I PASAŻEROWIE, LUDZIE REAGUJĄ NA CIOSY; dyspozytor 01:28: zróżnicowanie, role).
// Część 1 (ten plik): paczka Microsoft Rocketbox (MIT; pobiera unity-tools/claude/fetch_rocketbox.py), postacie,
// materiały, klipy, test bez deformacji, fabryka osoby (skala, wygląd, akcesoria, role).
//  • SZKIELET: postacie i animacje Rocketbox mają ten sam Biped (Bip01) — dwa warianty proporcji (męski / żeński,
//    rozpoznawane po stosunku przedramię : łydka, nie po nazwie), animacje m_* / f_* zrobione na nich → klipy grają
//    na postaciach BEZ przepinania (Legacy). Chód i bieg z paczki przesuwają Bip01 do przodu — kopia „w miejscu”
//    (odjęte przesunięcie poziome, prędkość klipu zapamiętana → krok zgrany z ruchem, bez ślizgania stóp).
//  • TEST BEZ DEFORMACJI (każdy build): długości kości (od stawu rodzica anatomicznego; udo od miednicy) w pozie
//    spoczynkowej vs 8 klatek każdego klipu na obu szkieletach — zmiana > 2 % = błąd; klip, który nie rusza kośćmi
//    = błąd; ręce poziomo w klatkach chodu/biegu (T-poza) = błąd. Log: BRAXHORN_LUDZIE_KOSCI bledy=N tpoza=N.
//  • SKALA: wzrost osoby = (0.945–1.0) × WzrostBazaLudzi (2.27 m — jak w #44, właściciel 20:45; świat idzie z K Braxa) (kobiety niżej w przedziale, mężczyźni wyżej) — jednolita
//    skala modelu, bez zmiany proporcji („tusza” pominięta: nierówna skala kości = deformacja).
//  • WYGLĄD: odcień ubrań (maska w alfie tekstury ciała, MaterialPropertyBlock — BraxWyglad), mundury bez odcienia;
//    akcesoria z kodu przypięte do kości: czapka z daszkiem, okulary, plecak, torba, kubek kawy, telefon.
//  • ROLE: Rola = które postacie + kolor ubrań + akcesoria (np. kierowca autobusu: czapka służbowa, koszula granat).
// Części 2: BraxClaudeOsiedleLudzieMiejsca.cs (przechodnie, ławki, pary, obsada, kierowcy).
public static partial class BraxClaudeOsiedle
{
    const string RbDir = "Assets/Source/Rocketbox";
    const string RbOut = Out + "/Ludzie";
    // GĘSTOŚĆ przechodniów na rejon (dyspozytor 01:28): osiedle niżej, miasto ok. 1.5–2× (miasto — w kolejnym buildzie)
    public const float GestoscOsiedle = 1f, GestoscMiasto = 1.75f;
    const bool OdcienUbran = true;          // wyłącznik przebarwiania ubrań (gdyby maska zawiodła na zdjęciach)

    sealed class RbPostac
    {
        public string nazwa, kategoria, typ;
        public GameObject prefab;
        public bool kobieta;                  // szkielet żeński (animacje f_*)
        public Material[] mats;
        public int[] ciala = new int[0];      // indeksy materiałów ciała (ubrania z maską)
        public bool maska;
        public float wys = 1.8f, podstawa;    // wysokość i spód siatki w pozie spoczynkowej (skala 1)
        public Quaternion korekta = Quaternion.identity;   // obrót modelu, żeby patrzył w +z
    }
    sealed class Osoba
    {
        public GameObject root, model; public Animation anim; public RbPostac p; public float wzrost, s;
        public Transform glowa, szyja, kregoslup, ramieL, ramieP, dlonL, dlonP, spine2;
        public BraxWyglad wyglad; public string pl;
        public readonly List<Renderer> akc = new List<Renderer>(); public readonly List<Color> akcKol = new List<Color>();
    }
    sealed class Rola
    {
        public string nazwa;
        public string[] postaci;              // początki nazw postaci Rocketbox (null = cywile)
        public bool odcien = true, kobietyMniej, czapkaSluzbowa, sluchawki;   // sluchawki: DJ (build 45 runda 2)
        public Color? kolor;                  // jeden kolor ubrań (koszula / mundur roli)
        public float czapka, okulary, plecak, torba, kubek, telefon;   // prawdopodobieństwa akcesoriów
    }

    static readonly string[] Cywile = { "Male_Adult", "Female_Adult", "Business", "Sports" };
    // E13 (build 45, właściciel 20:42: „ludzie na majtkach chodzą po osiedlu — oni na plażę do miasta”): stroje PLAŻOWE —
    // pomiar udziału skóry w teksturze *_body_color (barwa skóry z twarzy *_head_color, jak maska ubrań w fetch_rocketbox):
    // Sports_Male_01 90 % (kąpielówki, bez koszulki), Sports_Female_01 79 % (bikini, boso); reszta 3–48 % (ubrani).
    // Na osiedlu wykluczeni ze WSZYSTKICH ról (przechodnie, mieszkańcy, obsada, kierowcy, pasażerowie, podróżni). Miasto bez zmian.
    public static readonly string[] Plazowe = { "Sports_Male_01", "Sports_Female_01" };
    static bool Plazowy(RbPostac p) => Plazowe.Contains(p.nazwa);
    static int rbPlazowiPominieci;
    static readonly Rola RolaPrzechodzien = new Rola { nazwa = "przechodzień", postaci = Cywile, czapka = .18f, okulary = .18f, plecak = .14f, torba = .14f, kubek = .1f };
    static readonly Rola RolaImpreza = new Rola { nazwa = "gość klubu", postaci = new[] { "Female_Party", "Female_Adult", "Male_Adult" } };
    static readonly Rola RolaObsluga = new Rola { nazwa = "obsługa", postaci = new[] { "Male_Adult", "Female_Adult" }, kolor = new Color(.12f, .12f, .13f) };
    static readonly Rola RolaKucharz = new Rola { nazwa = "kucharz", postaci = new[] { "Chef" }, odcien = false };
    static readonly Rola RolaRecepcja = new Rola { nazwa = "recepcja", postaci = new[] { "Business_Female", "Business_Male", "Female_Adult" }, odcien = false };
    static readonly Rola RolaOchrona = new Rola { nazwa = "ochrona", postaci = new[] { "Security" }, odcien = false };
    static readonly Rola RolaPolicja = new Rola { nazwa = "policja", postaci = new[] { "Police" }, odcien = false };
    static readonly Rola RolaMedyk = new Rola { nazwa = "medyk", postaci = new[] { "Medical" }, odcien = false };
    static readonly Rola RolaKierowca = new Rola { nazwa = "kierowca", postaci = new[] { "Male_Adult", "Female_Adult", "Business" }, okulary = .25f };
    // MECHANIZM ROLI (dyspozytor 01:28 — autobusy w mieście później): kierowca autobusu = czapka służbowa + koszula granat
    static readonly Rola RolaKierowcaAutobusu = new Rola { nazwa = "kierowca autobusu", postaci = new[] { "Male_Adult", "Business_Male" }, czapkaSluzbowa = true, kolor = new Color(.10f, .16f, .32f) };
    static readonly Color[] PaletaUbran =
    {
        new Color(.12f, .18f, .34f), new Color(.35f, .36f, .38f), new Color(.62f, .56f, .44f), new Color(.30f, .34f, .20f),
        new Color(.45f, .10f, .12f), new Color(.24f, .38f, .58f), new Color(.08f, .08f, .09f), new Color(.86f, .85f, .82f),
        new Color(.40f, .26f, .16f), new Color(.14f, .32f, .24f), new Color(.70f, .52f, .14f), new Color(.52f, .30f, .44f),
        new Color(.62f, .66f, .70f), new Color(.80f, .42f, .30f),
    };

    static List<RbPostac> rbPostacie;
    static Dictionary<string, AnimationClip> rbKlipy;
    static Dictionary<string, float> rbPredkosc;   // m/s klipu chodu/biegu (świat, skala postaci 1)
    static bool rbZaladowane;
    static int rbBledyKosci, rbTpoza, rbLudzie, rbPrzechodnie, rbObsada, rbKierowcy, rbSiedzacy, rbPary, rbInni;
    static readonly Dictionary<string, int> rbKolejka = new Dictionary<string, int>();

    // Czy pobrano paczkę (wtedy przechodnie Mixamo z build 13 się nie budują — ludzie powstają w LudzieRocketbox).
    static bool RocketboxDostepny() => AssetDatabase.IsValidFolder(RbDir + "/Avatars") && Directory.GetFiles(RbDir + "/Avatars", "*.fbx", SearchOption.AllDirectories).Length > 0;

    // ---------------- wczytanie paczki ----------------
    static bool LadujRocketbox()
    {
        if (rbZaladowane) return rbPostacie.Count > 0;
        rbWlosySprawdzone = 0; rbWlosyPoprawione = 0; rbWlosyUwagi.Clear(); rbPlecakiPominiete = 0;
        rbZaladowane = true; rbPostacie = new List<RbPostac>(); rbKlipy = new Dictionary<string, AnimationClip>(); rbPredkosc = new Dictionary<string, float>();
        rbBledyKosci = rbTpoza = rbLudzie = rbPrzechodnie = rbObsada = rbKierowcy = rbSiedzacy = rbPary = rbInni = 0; rbKolejka.Clear(); akcSiatki.Clear(); akcMat.Clear();
        if (!RocketboxDostepny()) return false;
        Directory.CreateDirectory(RbOut);
        var ubranie = Shader.Find("Braxhorn/LudzieUbranie"); var wlosy = Shader.Find("Braxhorn/LudzieWlosy"); var std = Shader.Find("Standard");
        if (!ubranie || !wlosy) rep.notes.Add("DO POPRAWKI: ludzie — brak shaderów Braxhorn/LudzieUbranie / LudzieWlosy (Standard zamiast)");
        foreach (var plik in Directory.GetFiles(RbDir + "/Avatars", "*.fbx", SearchOption.AllDirectories).Select(f => f.Replace('\\', '/')).OrderBy(f => f))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(plik);
            if (!go) { AssetDatabase.ImportAsset(plik, ImportAssetOptions.ForceSynchronousImport); go = AssetDatabase.LoadAssetAtPath<GameObject>(plik); }
            var smr = go ? go.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (!smr) { rep.notes.Add("DO POPRAWKI: ludzie — " + plik + " bez siatki ze szkieletem"); continue; }
            Transform T(GameObject g, string n) => g.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
            Transform fa = T(go, "Bip01 L Forearm"), ha = T(go, "Bip01 L Hand"), ca = T(go, "Bip01 L Calf"), fo = T(go, "Bip01 L Foot");
            if (!fa || !ha || !ca || !fo) { rep.notes.Add("DO POPRAWKI: ludzie — " + go.name + ": nieznany szkielet (brak Bip01)"); continue; }
            var p = new RbPostac { nazwa = go.name, prefab = go, kategoria = plik.Contains("/Professions/") ? "Professions" : "Adults" };
            p.kobieta = Vector3.Distance(fa.position, ha.position) / Mathf.Max(1e-4f, Vector3.Distance(ca.position, fo.position)) < .62f;
            p.typ = go.name.Split('_')[0];
            // materiały: tekstury z folderu Textures po nazwie podsiatki (m002_body → m002_body_color / _normal)
            string texDir = Path.GetDirectoryName(plik).Replace('\\', '/') + "/Textures";
            var tex = Directory.Exists(texDir) ? Directory.GetFiles(texDir, "*.png").Select(f => f.Replace('\\', '/')).ToArray() : new string[0];
            var src = smr.sharedMaterials; var mats = new Material[src.Length]; var ciala = new List<int>();
            for (int i = 0; i < src.Length; i++)
            {
                string mn = src[i] ? src[i].name : "slot" + i, low = mn.ToLowerInvariant();
                string kolor = Tekstura(tex, mn, "color"), norm = Tekstura(tex, mn, "normal");
                bool opac = low.Contains("opacity"), body = low.Contains("body");
                var m = new Material(opac ? (wlosy ? wlosy : std) : body && ubranie ? ubranie : std) { name = "Ludzie: " + go.name + " " + mn };
                var ck = kolor != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(kolor) : null;
                var nk = norm != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(norm) : null;
                if (ck) m.SetTexture("_MainTex", ck); else rep.notes.Add("ludzie: " + go.name + " — brak tekstury koloru dla " + mn);
                if (nk) { m.SetTexture("_BumpMap", nk); m.EnableKeyword("_NORMALMAP"); }
                m.SetFloat("_Glossiness", opac ? .1f : low.Contains("head") ? .38f : low.Contains("equipment") || low.Contains("pistol") || low.Contains("helmet") ? .45f : .28f);
                if (opac)
                {   // E15 (build 45): niebiesko-białe plamy na włosach = odbicie nieba (gładkość 0.28, karty dwustronne, Fresnel pod
                    // kątem) + biała tekstura domyślna, gdy brak pliku krycia. Gładkość włosów 0.1; brak tekstury = karta wyłączona z alfą 0.
                    rbWlosySprawdzone++;
                    var imp = kolor != null ? AssetImporter.GetAtPath(kolor) as TextureImporter : null;
                    bool alfaOk = ck && imp && imp.alphaIsTransparency && imp.DoesSourceTextureHaveAlpha();
                    if (!ck) { m.SetTexture("_MainTex", Texture2D.blackTexture); m.SetColor("_Color", new Color(0, 0, 0, 0)); rbWlosyUwagi.Add(go.name + " " + mn + ": brak tekstury — karta ukryta"); }
                    else if (!alfaOk) rbWlosyUwagi.Add(go.name + " " + mn + ": tekstura bez alfy/dylatacji (" + System.IO.Path.GetFileName(kolor) + ")");
                    rbWlosyPoprawione++;
                }
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                if (opac && !wlosy)
                {
                    m.SetFloat("_Mode", 1f); m.SetOverrideTag("RenderType", "TransparentCutout"); m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cutoff", .45f); m.renderQueue = 2450;
                }
                if (body && m.shader == ubranie)
                {
                    ciala.Add(i);
                    var imp = kolor != null ? AssetImporter.GetAtPath(kolor) as TextureImporter : null;
                    if (imp && imp.DoesSourceTextureHaveAlpha()) p.maska = true;
                }
                string mp = RbOut + "/" + Safe(go.name + "_" + mn) + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(mp)) AssetDatabase.DeleteAsset(mp);
                AssetDatabase.CreateAsset(m, mp); mats[i] = m;
            }
            p.mats = mats; p.ciala = ciala.ToArray();
            // wymiary i przód w pozie spoczynkowej
            var inst = (GameObject)UnityEngine.Object.Instantiate(go); inst.transform.position = Vector3.zero;
            var ismr = inst.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var b = ismr.bounds; p.wys = Mathf.Max(.5f, b.size.y); p.podstawa = b.min.y;
            Transform lt = T(inst, "Bip01 L Toe0"), lf = T(inst, "Bip01 L Foot"), rt = T(inst, "Bip01 R Toe0"), rf = T(inst, "Bip01 R Foot");
            var fwd = lt && lf && rt && rf ? (lt.position - lf.position) + (rt.position - rf.position) : Vector3.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 1e-6f) p.korekta = Quaternion.Euler(0, -Vector3.SignedAngle(Vector3.forward, fwd, Vector3.up), 0);
            UnityEngine.Object.DestroyImmediate(inst);
            rbPostacie.Add(p);
        }
        // klipy
        if (AssetDatabase.IsValidFolder(RbDir + "/Animations"))
        {
            float skalaPliku = rbPostacie.Count > 0 ? rbPostacie[0].prefab.transform.localScale.x : 1f;
            foreach (var plik in Directory.GetFiles(RbDir + "/Animations", "*.fbx").Select(f => f.Replace('\\', '/')).OrderBy(f => f))
            {
                string key = Path.GetFileNameWithoutExtension(plik);
                var clip = AssetDatabase.LoadAllAssetsAtPath(plik).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (!clip) { rep.notes.Add("DO POPRAWKI: ludzie — klip " + key + " bez animacji"); continue; }
                bool lok = key.Contains("_walk_") || key.Contains("_run_");
                bool bip02 = AnimationUtility.GetCurveBindings(clip).Any(q => q.path.StartsWith("Bip02"));
                if (lok || bip02 || !clip.legacy)
                {
                    var d = KlipPochodny(clip, key, lok, out float v);
                    if (!d) continue;
                    clip = d; if (lok) rbPredkosc[key] = v * skalaPliku;
                }
                rbKlipy[key] = clip;
            }
        }
        int m0 = rbPostacie.Count(q => !q.kobieta);
        Debug.Log("BRAXHORN_LUDZIE_WLOSY sprawdzone=" + rbWlosySprawdzone + " poprawione=" + rbWlosyPoprawione + " (gładkość 0.28 → 0.10, odbicie nieba na kartach włosów)" + (rbWlosyUwagi.Count > 0 ? " uwagi: " + string.Join("; ", rbWlosyUwagi) : ""));
        Debug.Log("BRAXHORN_LUDZIE_STROJE osiedle: ubrani=" + rbPostacie.Count(q => !Plazowy(q)) + " plażowi_wykluczeni=" + rbPostacie.Count(Plazowy) + " (" + string.Join(", ", rbPostacie.Where(Plazowy).Select(q => q.nazwa)) + ") — reguła tylko osiedla; w mieście bez zmian");
        Debug.Log("BRAXHORN_LUDZIE paczka: postaci " + rbPostacie.Count + " (szkielet męski " + m0 + ", żeński " + (rbPostacie.Count - m0) + "), klipów " + rbKlipy.Count +
                  ", prędkości chodu: " + string.Join(", ", rbPredkosc.Select(kv => kv.Key + " " + kv.Value.ToString("F2"))));
        TestKosci();
        return rbPostacie.Count > 0 && rbKlipy.Count > 0;
    }

    static string[] Tokeny(string s) => s.ToLowerInvariant().Split(new[] { '_', ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
    // Tekstura dla podsiatki: ten sam zestaw słów nazwy bez „color/normal” (f110_A_head ↔ f110_head_color_a).
    static string Tekstura(string[] pliki, string material, string rodzaj)
    {
        var mt = new HashSet<string>(Tokeny(material)); string best = null; int bs = 0;
        foreach (var f in pliki)
        {
            var t = Tokeny(Path.GetFileNameWithoutExtension(f)).ToList();
            if (!t.Contains(rodzaj) || t.Contains("wrinkle")) continue;
            t.Remove(rodzaj);
            if (t.Count == 0 || !mt.Contains(t[0])) continue;              // ten sam przedrostek postaci (m002, f110…)
            int s = t.Count(mt.Contains) * 10 - t.Count(q => !mt.Contains(q)) * 3 - mt.Count(q => !t.Contains(q)) * 3;
            if (s > bs) { bs = s; best = f; }
        }
        return best;
    }

    // Kopia klipu Legacy: ścieżki Bip02 → Bip01, bez skali i węzłów pomocniczych; chód/bieg „w miejscu” (prędkość → v).
    static AnimationClip KlipPochodny(AnimationClip src, string key, bool wMiejscu, out float v)
    {
        v = 0f;
        var dst = new AnimationClip { name = key, legacy = true, frameRate = src.frameRate, wrapMode = WrapMode.Loop };
        float len = Mathf.Max(.05f, src.length); float dx = 0f, dz = 0f; int n = 0;
        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            if (b.type != typeof(Transform) || b.propertyName.StartsWith("m_LocalScale")) continue;
            string path = b.path.Replace("Bip02", "Bip01"), last = path.Substring(path.LastIndexOf('/') + 1);
            if (last.EndsWith("Nub") || last.Contains("Footsteps") || last.Contains("MotionExtraction")) continue;
            var c = AnimationUtility.GetEditorCurve(src, b); if (c == null || c.length == 0) continue;
            if (wMiejscu && path == "Bip01" && (b.propertyName == "m_LocalPosition.x" || b.propertyName == "m_LocalPosition.z"))
            {
                float d = c.Evaluate(len) - c.Evaluate(0f); var keys = c.keys;
                for (int k = 0; k < keys.Length; k++) { keys[k].value -= d * keys[k].time / len; keys[k].inTangent -= d / len; keys[k].outTangent -= d / len; }
                c.keys = keys;
                if (b.propertyName.EndsWith("x")) dx = d; else dz = d;
            }
            AnimationUtility.SetEditorCurve(dst, EditorCurveBinding.FloatCurve(path, typeof(Transform), b.propertyName), c); n++;
        }
        if (n == 0) { rep.notes.Add("DO POPRAWKI: ludzie — klip " + key + " bez krzywych kości"); return null; }
        dst.EnsureQuaternionContinuity();
        v = Mathf.Sqrt(dx * dx + dz * dz) / len;
        string p = RbOut + "/" + Safe(key) + ".anim";   // nazwa pliku = nazwa klipu = klucz (AddClip bez kopii — patrz DodajKlipy)
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(p)) AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(dst, p);
        return dst;
    }

    // ---------------- test: bez deformacji, bez T-pozy ----------------
    static bool KoscTwarzy(string n) => new[] { "Eye", "Mouth", "Lip", "lip", "Jaw", "Tongue", "Cheek", "brow", "Brow", "Nose", "Masseter", "Caninus", "Nub", "Footsteps" }.Any(n.Contains);

    static void TestKosci()
    {
        var bledy = new List<string>(); int naruszen = 0, sprawdzone = 0;
        foreach (bool kobieta in new[] { false, true })
        {
            var p = rbPostacie.FirstOrDefault(q => q.kobieta == kobieta); if (p == null) continue;
            string pre = kobieta ? "f_" : "m_";
            var inst = (GameObject)UnityEngine.Object.Instantiate(p.prefab); inst.transform.position = Vector3.zero;
            var all = inst.GetComponentsInChildren<Transform>(true);
            Transform T(string n) => all.FirstOrDefault(t => t.name == n);
            var bip = T("Bip01"); var pelvis = T("Bip01 Pelvis");
            var pary = new List<(Transform k, Transform r, float L0)>();
            foreach (var t in bip ? bip.GetComponentsInChildren<Transform>(true) : new Transform[0])
            {
                if (t == bip || KoscTwarzy(t.name)) continue;
                var r = t.name.Contains("Thigh") && pelvis ? pelvis : t.parent;
                if (!r || r == bip) continue;
                float L0 = Vector3.Distance(t.position, r.position); if (L0 < .005f) continue;
                pary.Add((t, r, L0));
            }
            var spocz = all.ToDictionary(t => t, t => t.localRotation); var spoczP = all.ToDictionary(t => t, t => t.localPosition);
            Transform uL = T("Bip01 L UpperArm"), fL = T("Bip01 L Forearm"), uR = T("Bip01 R UpperArm"), fR = T("Bip01 R Forearm");
            foreach (var kv in rbKlipy.Where(q => q.Key.StartsWith(pre)))
            {
                var clip = kv.Value; bool zle = false, chod = kv.Key.Contains("_walk_") || kv.Key.Contains("_run_");
                foreach (var t in all) { t.localRotation = spocz[t]; t.localPosition = spoczP[t]; }   // poza spoczynkowa przed każdym klipem
                for (int k = 0; k < 8; k++)
                {
                    clip.SampleAnimation(inst, clip.length * k / 8f);
                    if (k == 0 && all.Where(t => !KoscTwarzy(t.name)).All(t => Quaternion.Angle(t.localRotation, spocz[t]) < .5f))
                    { bledy.Add(kv.Key + ": klip nie rusza kośćmi (ścieżki nie pasują)"); zle = true; break; }
                    foreach (var (kk, r, L0) in pary)
                    {
                        float L = Vector3.Distance(kk.position, r.position), dev = Mathf.Abs(L / L0 - 1f);
                        if (dev > .02f) { naruszen++; zle = true; if (bledy.Count < 12) bledy.Add(kv.Key + " t=" + (clip.length * k / 8f).ToString("F2") + " " + kk.name + " " + (dev * 100f).ToString("F1") + "%"); }
                    }
                    if (chod && uL && fL && uR && fR)
                        foreach (var (u, f) in new[] { (uL, fL), (uR, fR) })
                            if (Vector3.Angle(f.position - u.position, Vector3.down) > 60f) { rbTpoza++; if (bledy.Count < 12) bledy.Add(kv.Key + ": ręka poziomo (T-poza) " + u.name); }
                }
                sprawdzone++; if (zle) rbBledyKosci++;
            }
            UnityEngine.Object.DestroyImmediate(inst);
        }
        Debug.Log("BRAXHORN_LUDZIE_KOSCI bledy=" + rbBledyKosci + " naruszen=" + naruszen + " tpoza=" + rbTpoza + " klipow=" + sprawdzone + (bledy.Count > 0 ? " | " + string.Join(" | ", bledy) : ""));
        rep.notes.Add("ludzie: test kości — klipów " + sprawdzone + ", z błędem " + rbBledyKosci + ", T-poza " + rbTpoza);
    }

    // ---------------- klipy dla osoby ----------------
    // Nazwa klipu dla płci szkieletu osoby (m_ / f_), z zapasem na drugą płeć (ten sam szkielet tylko gdy pasuje).
    static string Klip(Osoba o, string baza)
    {
        string k = o.pl + baza;
        return rbKlipy.ContainsKey(k) ? k : null;
    }
    static string[] Klipy(Osoba o, params string[] bazy) => bazy.Select(b => Klip(o, b)).Where(k => k != null).Distinct().ToArray();
    // AddClip(klip, nazwa) z nazwą INNĄ niż klip.name = pełna kopia klipu zapisana w scenie przy KAŻDEJ osobie (build 14:
    // 1695 kopii, 1,6 GB). Klip musi się nazywać jak klucz; jeśli nie (inny plik FBX) — JEDNA wspólna kopia jako plik .anim.
    static int kopieKlipow;
    static void DodajKlipy(Osoba o, IEnumerable<string> klucze)
    {
        foreach (var k in klucze.Where(q => q != null).Distinct())
        {
            if (!rbKlipy.TryGetValue(k, out var c) || o.anim.GetClip(k)) continue;
            if (c.name != k)
            {
                string p = RbOut + "/" + Safe(k) + ".anim";
                var kopia = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (!kopia || kopia.name != k) { kopia = UnityEngine.Object.Instantiate(c); AssetDatabase.CreateAsset(kopia, p); }
                if (kopia.name != k) { rep.notes.Add("DO POPRAWKI: ludzie — klip " + k + " ma nazwę pliku " + kopia.name + " (znaki spoza liter/cyfr) — kopia w scenie"); }
                rbKlipy[k] = c = kopia; kopieKlipow++;
            }
            o.anim.AddClip(c, k);
        }
    }
    static void Pozuj(Osoba o, string klip, float t)
    {
        if (klip == null || !rbKlipy.TryGetValue(klip, out var c)) return;
        o.anim.clip = c; c.SampleAnimation(o.model, t * c.length);
    }

    // ---------------- fabryka osoby ----------------
    static RbPostac WybierzPostac(Rola r, System.Random rnd)
    {
        var kand = rbPostacie.Where(p => !Plazowy(p) && (r.postaci == null || r.postaci.Any(x => p.nazwa.StartsWith(x)))).ToList();
        if (kand.Count == 0) kand = rbPostacie.Where(p => !Plazowy(p) && Cywile.Any(x => p.nazwa.StartsWith(x))).ToList();
        if (kand.Count == 0) kand = rbPostacie.Where(p => !Plazowy(p)).ToList();
        if (kand.Count == 0) return null;
        // po kolei z losowym przesunięciem — różne postacie obok siebie, każda użyta
        rbKolejka.TryGetValue(r.nazwa, out int n); rbKolejka[r.nazwa] = n + 1 + (rnd.NextDouble() < .3 ? 1 : 0);
        return kand[(n + rnd.Next(2)) % kand.Count];
    }

    static Osoba NowaOsoba(Rola rola, Transform parent, string nazwa, Vector3 pos, float yaw, System.Random rnd, RbPostac wymus = null)
    {
        var p = wymus ?? WybierzPostac(rola, rnd); if (p == null) return null;
        // właściciel 2.11 10:43: każdy człowiek „mniej więcej jak Brax” (2.33 m), różnice kilku cm; kobiety trochę niżej
        float H = WzrostBazaLudzi;   // build 45: baza ludzi przypięta (2.27 jak w #44), niezależna od K świata (Brax 1.294)
        float u = (float)rnd.NextDouble(), mnoz = p.kobieta ? Mathf.Lerp(.945f, .975f, u) : Mathf.Lerp(.97f, 1.0f, u);
        var o = new Osoba { p = p, wzrost = H * mnoz, pl = p.kobieta ? "f_" : "m_" };
        WzrostyLudzi(p.kobieta).Add(o.wzrost);
        o.s = o.wzrost / p.wys;
        o.root = new GameObject(nazwa + " · " + p.nazwa); o.root.transform.SetParent(parent, false);
        o.root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        o.model = (GameObject)UnityEngine.Object.Instantiate(p.prefab); o.model.name = p.nazwa;
        o.model.transform.SetParent(o.root.transform, false);
        o.model.transform.localRotation = p.korekta * p.prefab.transform.localRotation;
        o.model.transform.localScale = p.prefab.transform.localScale * o.s;
        o.model.transform.localPosition = new Vector3(0f, -p.podstawa * o.s, 0f);
        var smr = o.model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        smr.sharedMaterials = p.mats; smr.updateWhenOffscreen = false; smr.quality = SkinQuality.Bone4; smr.skinnedMotionVectors = false;
        smr.shadowCastingMode = ShadowCastingMode.On; smr.receiveShadows = true; smr.lightProbeUsage = LightProbeUsage.Off; smr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        o.anim = o.model.GetComponent<Animation>(); if (!o.anim) o.anim = o.model.AddComponent<Animation>();
        o.anim.playAutomatically = true; o.anim.cullingType = AnimationCullingType.BasedOnRenderers;
        var all = o.model.GetComponentsInChildren<Transform>(true);
        Transform T(string n) => all.FirstOrDefault(t => t.name == n);
        o.glowa = T("Bip01 Head"); o.szyja = T("Bip01 Neck"); o.kregoslup = T("Bip01 Spine1"); o.spine2 = T("Bip01 Spine2");
        o.ramieL = T("Bip01 L UpperArm"); o.ramieP = T("Bip01 R UpperArm"); o.dlonL = T("Bip01 L Hand"); o.dlonP = T("Bip01 R Hand");
        // wygląd: odcień ubrań
        o.wyglad = o.root.AddComponent<BraxWyglad>(); o.wyglad.cialo = smr; o.wyglad.indeksyUbran = p.ciala;
        if (OdcienUbran && p.maska && p.ciala.Length > 0)
        {
            if (rola.kolor.HasValue) { o.wyglad.odcien = rola.kolor.Value; o.wyglad.sila = .9f; }
            else if (rola.odcien && p.kategoria == "Adults" || rola.odcien && (p.nazwa.StartsWith("Business") || p.nazwa.StartsWith("Sports")))
            {
                var c = PaletaUbran[rnd.Next(PaletaUbran.Length)];
                if (p.nazwa.StartsWith("Business")) c = new[] { PaletaUbran[0], PaletaUbran[1], PaletaUbran[6], PaletaUbran[8] }[rnd.Next(4)];
                o.wyglad.odcien = c; o.wyglad.sila = Mathf.Lerp(.5f, .85f, (float)rnd.NextDouble());
                if (rnd.NextDouble() < .2) o.wyglad.sila = 0f;            // część w oryginalnych kolorach paczki
            }
        }
        rbLudzie++;
        return o;
    }

    // Domknięcie osoby po klipach i akcesoriach: LOD (znika z daleka razem z akcesoriami), kolory akcesoriów.
    static void Zamknij(Osoba o, bool kolizja)
    {
        o.wyglad.akcesoria = o.akc.ToArray(); o.wyglad.koloryAkcesoriow = o.akcKol.ToArray();
        var rs = o.root.GetComponentsInChildren<Renderer>(true);
        var lod = o.root.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.012f, rs) }); lod.RecalculateBounds();
        if (kolizja)
        {
            var col = o.root.AddComponent<CapsuleCollider>(); col.height = o.wzrost; col.radius = .26f; col.center = new Vector3(0, o.wzrost * .5f, 0);
            var rb = o.root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        }
    }

    // ---------------- akcesoria (siatki z kodu, przypięte do kości) ----------------
    static int rbWlosySprawdzone, rbWlosyPoprawione; static readonly List<string> rbWlosyUwagi = new List<string>();
    static readonly Dictionary<string, Mesh> akcSiatki = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> akcMat = new Dictionary<string, Material>();

    sealed class Bud
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int>[] tri = { new List<int>(), new List<int>() };
        public void Box(Vector3 c, Vector3 s, Quaternion q, int sub = 0)
        {
            var h = s * .5f;
            foreach (var (nn, u, w) in new[] { (Vector3.up, Vector3.right, Vector3.forward), (Vector3.down, Vector3.forward, Vector3.right), (Vector3.forward, Vector3.up, Vector3.right),
                                               (Vector3.back, Vector3.right, Vector3.up), (Vector3.right, Vector3.forward, Vector3.up), (Vector3.left, Vector3.up, Vector3.forward) })
            {
                Vector3 cc = c + q * Vector3.Scale(nn, h), uu = q * Vector3.Scale(u, h), ww = q * Vector3.Scale(w, h), no = q * nn;
                int k = v.Count;
                v.Add(cc - uu - ww); v.Add(cc + uu - ww); v.Add(cc + uu + ww); v.Add(cc - uu + ww);
                for (int j = 0; j < 4; j++) { n.Add(no); uv.Add(new Vector2(j == 1 || j == 2 ? 1 : 0, j >= 2 ? 1 : 0)); }
                if (Vector3.Dot(Vector3.Cross(v[k + 1] - v[k], v[k + 2] - v[k]), no) >= 0) tri[sub].AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                else tri[sub].AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            }
        }
        // walec wzdłuż osi y: podstawa w c, promień r (rx/rz), wysokość h
        public void Walec(Vector3 c, float rx, float rz, float h, int seg, int sub = 0, bool gora = true)
        {
            int k0 = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = c + new Vector3(d.x * rx, 0, d.z * rz);
                v.Add(p); v.Add(p + Vector3.up * h); n.Add(d); n.Add(d); uv.Add(new Vector2(i / (float)seg, 0)); uv.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++) { int a = k0 + i * 2; tri[sub].AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 }); }
            if (gora)
            {
                int cK = v.Count; v.Add(c + Vector3.up * h); n.Add(Vector3.up); uv.Add(new Vector2(.5f, .5f));
                for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; v.Add(c + new Vector3(Mathf.Cos(a) * rx, h, Mathf.Sin(a) * rz)); n.Add(Vector3.up); uv.Add(new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f)); }
                for (int i = 0; i < seg; i++) tri[sub].AddRange(new[] { cK, cK + 2 + i, cK + 1 + i });
            }
        }
        // zaokrąglony prostopadłościan (superelipsoida, e = 0.35: płaskie ściany, okrągłe krawędzie) o półwymiarach r
        public void Zaokraglony(Vector3 c, Vector3 r, float e, int segU, int segV, int sub = 0)
        {
            int k0 = v.Count;
            float S(float x, float p) => Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), p);
            for (int j = 0; j <= segV; j++)
                for (int i = 0; i <= segU; i++)
                {
                    float th = -Mathf.PI * .5f + j * Mathf.PI / segV, ph = i * Mathf.PI * 2f / segU;
                    float cx = Mathf.Cos(th) * Mathf.Cos(ph), cy = Mathf.Sin(th), cz = Mathf.Cos(th) * Mathf.Sin(ph);
                    var q = new Vector3(S(Mathf.Cos(th), e) * S(Mathf.Cos(ph), e), S(Mathf.Sin(th), e), S(Mathf.Cos(th), e) * S(Mathf.Sin(ph), e));
                    v.Add(c + Vector3.Scale(q, r));
                    var nn = new Vector3(S(Mathf.Cos(th), 2f - e) * S(Mathf.Cos(ph), 2f - e) / r.x, S(Mathf.Sin(th), 2f - e) / r.y, S(Mathf.Cos(th), 2f - e) * S(Mathf.Sin(ph), 2f - e) / r.z);
                    n.Add(nn.sqrMagnitude > 1e-12f ? nn.normalized : new Vector3(cx, cy, cz)); uv.Add(new Vector2(i / (float)segU, j / (float)segV));
                }
            for (int j = 0; j < segV; j++)
                for (int i = 0; i < segU; i++)
                {
                    int a = k0 + j * (segU + 1) + i, b2 = a + segU + 1;
                    tri[sub].AddRange(new[] { a, b2, a + 1, a + 1, b2, b2 + 1 });
                }
        }
        // pół elipsoidy (kopuła) nad c
        public void Kopula(Vector3 c, Vector3 r, int segU, int segV, int sub = 0)
        {
            int k0 = v.Count;
            for (int j = 0; j <= segV; j++)
                for (int i = 0; i <= segU; i++)
                {
                    float th = j * Mathf.PI * .5f / segV, ph = i * Mathf.PI * 2f / segU;
                    var d = new Vector3(Mathf.Cos(th) * Mathf.Cos(ph), Mathf.Sin(th), Mathf.Cos(th) * Mathf.Sin(ph));
                    v.Add(c + Vector3.Scale(d, r)); n.Add(new Vector3(d.x / r.x, d.y / r.y, d.z / r.z).normalized); uv.Add(new Vector2(i / (float)segU, j / (float)segV));
                }
            for (int j = 0; j < segV; j++)
                for (int i = 0; i < segU; i++)
                {
                    int a = k0 + j * (segU + 1) + i, b = a + segU + 1;
                    tri[sub].AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
        }
        public Mesh Siatka(string nazwa)
        {
            var m = new Mesh { name = nazwa }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            int subs = tri[1].Count > 0 ? 2 : 1; m.subMeshCount = subs;
            for (int s = 0; s < subs; s++) m.SetTriangles(tri[s], s);
            m.RecalculateBounds(); return m;
        }
    }

    static Mesh AkcSiatka(string id)
    {
        if (akcSiatki.TryGetValue(id, out var have)) return have;
        var b = new Bud();
        switch (id)
        {
            case "czapka":            // czapka z daszkiem: kopuła + daszek (jeden kolor), spód kopuły w y = 0, przód +z
                b.Kopula(Vector3.zero, new Vector3(.098f, .085f, .106f), 16, 6);
                b.Box(new Vector3(0, .01f, .132f), new Vector3(.16f, .01f, .08f), Quaternion.Euler(10f, 0, 0));
                break;
            case "czapkaSluzbowa":    // otok + denko (kolor roli) i czarny daszek (podsiatka 1)
                b.Walec(Vector3.zero, .1f, .108f, .055f, 18, 0, false);
                b.Walec(new Vector3(0, .055f, .012f), .118f, .124f, .028f, 18, 0, true);
                b.Box(new Vector3(0, .004f, .128f), new Vector3(.15f, .008f, .07f), Quaternion.Euler(16f, 0, 0), 1);
                break;
            case "sluchawki":         // słuchawki DJ-a: pałąk łukiem nad głową (od ucha do ucha), dwie muszle na uszach; góra głowy w y = 0
                for (int i = 0; i < 8; i++)
                {
                    float a0 = Mathf.PI * i / 8f, a1 = Mathf.PI * (i + 1) / 8f, xm = (Mathf.Cos(a0) + Mathf.Cos(a1)) * .5f * .088f, ym = -.09f + (Mathf.Sin(a0) + Mathf.Sin(a1)) * .5f * .1f;
                    b.Box(new Vector3(xm, ym, 0), new Vector3(.03f, .012f, .028f), Quaternion.Euler(0, 0, (a0 + a1) * .5f * Mathf.Rad2Deg - 90f));
                }
                foreach (float x in new[] { -.094f, .094f }) b.Box(new Vector3(x, -.115f, .005f), new Vector3(.032f, .078f, .07f), Quaternion.identity);
                break;
            case "okulary":           // szkła na wysokości oczu (rozstaw 6.6 cm), mostek, zauszniki do tyłu
                foreach (float x in new[] { -.033f, .033f }) { b.Box(new Vector3(x, 0, 0), new Vector3(.052f, .036f, .005f), Quaternion.identity); b.Box(new Vector3(x * 2.06f, .006f, -.055f), new Vector3(.004f, .006f, .11f), Quaternion.identity); }
                b.Box(new Vector3(0, .008f, 0), new Vector3(.016f, .005f, .005f), Quaternion.identity);
                break;
            case "kubek":             // kubek kawy na wynos: kubek (kolor) + wieczko (podsiatka 1); spód w y = 0
                b.Walec(Vector3.zero, .036f, .036f, .1f, 14, 0, false);
                b.Walec(new Vector3(0, .1f, 0), .04f, .04f, .014f, 14, 1, true);
                break;
            case "telefon":
                b.Box(Vector3.zero, new Vector3(.072f, .15f, .009f), Quaternion.identity);
                break;
            case "plecak":            // przód plecaka (+z) przy plecach
                b.Box(Vector3.zero, new Vector3(.30f, .40f, .13f), Quaternion.identity);
                b.Box(new Vector3(0, -.08f, -.08f), new Vector3(.24f, .17f, .04f), Quaternion.identity);
                b.Box(new Vector3(0, .21f, .02f), new Vector3(.1f, .02f, .03f), Quaternion.identity);
                break;
            case "torba":             // torba w dłoni (build 45: zaokrąglona bryła zamiast klocka): korpus z zaokrąglonymi krawędziami, pasek-uchwyt łukiem
                b.Zaokraglony(new Vector3(0, -.15f, 0), new Vector3(.15f, .11f, .05f), .35f, 14, 10);
                for (int i = 0; i < 8; i++)
                {
                    float a0 = Mathf.PI * i / 8f, a1 = Mathf.PI * (i + 1) / 8f;
                    var p0 = new Vector3(Mathf.Cos(a0) * .06f, -.04f + Mathf.Sin(a0) * .05f, 0); var p1 = new Vector3(Mathf.Cos(a1) * .06f, -.04f + Mathf.Sin(a1) * .05f, 0);
                    b.Box((p0 + p1) * .5f, new Vector3((p1 - p0).magnitude + .002f, .01f, .012f), Quaternion.FromToRotation(Vector3.right, p1 - p0));
                }
                break;
        }
        var m = b.Siatka("Ludzie: akcesorium " + id);
        string p = RbOut + "/akcesorium_" + id + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(p)) AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(m, p);
        return akcSiatki[id] = m;
    }

    static Material AkcMaterial(string id, Color c, float gl)
    {
        if (akcMat.TryGetValue(id, out var have)) return have;
        var m = new Material(Shader.Find("Standard")) { name = "Ludzie: " + id, color = c };
        m.SetFloat("_Glossiness", gl);
        string p = RbOut + "/akcesorium_" + Safe(id) + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(p)) AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(m, p);
        return akcMat[id] = m;
    }

    static Renderer Akcesorium(Osoba o, string id, Transform kosc, Vector3 pos, Quaternion rot, Color? kolor, params Material[] mats)
    {
        if (!kosc) return null;
        var g = new GameObject("akcesorium: " + id);
        g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = Vector3.one * o.s;
        g.transform.SetParent(kosc, true);
        g.AddComponent<MeshFilter>().sharedMesh = AkcSiatka(id);
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        r.lightProbeUsage = LightProbeUsage.Off;
        if (kolor.HasValue) { o.akc.Add(r); o.akcKol.Add(kolor.Value); }
        return r;
    }

    // Głowa (poza spoczynkowa, przed klipami): czapka, okulary.
    static void AkcesoriaGlowy(Osoba o, Rola rola, System.Random rnd)
    {
        var all = o.model.GetComponentsInChildren<Transform>(true);
        Transform lE = all.FirstOrDefault(t => t.name == "Bip01 LEye"), rE = all.FirstOrDefault(t => t.name == "Bip01 REye");
        if (!o.glowa || !lE || !rE) return;
        var F = o.root.transform.forward; var U = Vector3.up;
        var oczy = (lE.position + rE.position) * .5f;
        var smr = o.model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        float top = smr.bounds.max.y;
        var srodek = oczy - F * .075f * o.s; srodek.y = top;
        bool dlugieWlosy = o.p.kobieta;
        if (rola.czapkaSluzbowa)
            Akcesorium(o, "czapkaSluzbowa", o.glowa, srodek - U * .07f * o.s + F * .005f * o.s, Quaternion.LookRotation(F, U), rola.kolor ?? new Color(.1f, .14f, .3f),
                       AkcMaterial("czapka (kolor)", Color.white, .25f), AkcMaterial("daszek czarny", new Color(.03f, .03f, .035f), .7f));
        else if (!dlugieWlosy && rnd.NextDouble() < rola.czapka)
            Akcesorium(o, "czapka", o.glowa, srodek - U * .072f * o.s, Quaternion.LookRotation(F, U), PaletaUbran[rnd.Next(PaletaUbran.Length)], AkcMaterial("czapka (kolor)", Color.white, .25f));
        if (rola.sluchawki)
            Akcesorium(o, "sluchawki", o.glowa, srodek - F * .005f * o.s, Quaternion.LookRotation(F, U), null, AkcMaterial("słuchawki czarne", new Color(.03f, .03f, .035f), .6f));
        if (rnd.NextDouble() < rola.okulary)
            Akcesorium(o, "okulary", o.glowa, oczy + F * .028f * o.s, Quaternion.LookRotation(F, U), null, AkcMaterial("okulary czarne", new Color(.02f, .02f, .025f), .85f));
    }

    // Dłonie i plecy (w pozie chodu — dłoń zwisa, wnętrze dłoni do uda): kubek, telefon, torba, plecak.
    static void AkcesoriaDloni(Osoba o, Rola rola, System.Random rnd, bool telefon, bool kubek)
    {
        var all = o.model.GetComponentsInChildren<Transform>(true);
        Transform T(string n) => all.FirstOrDefault(t => t.name == n);
        var pel = T("Bip01 Pelvis");
        (Vector3 srodek, Vector3 fd, Vector3 pn)? Dlon(Transform h, string s)
        {
            var f2 = T("Bip01 " + s + " Finger2"); var f0 = T("Bip01 " + s + " Finger0");
            if (!h || !f2 || !f0) return null;
            var fd = (f2.position - h.position).normalized; var ts = Vector3.ProjectOnPlane(f0.position - h.position, fd).normalized;
            var pn = Vector3.Cross(fd, ts).normalized;
            if (pel && Vector3.Dot(pn, pel.position - h.position) < 0f) pn = -pn;   // wnętrze dłoni do ciała
            return ((h.position + f2.position) * .5f, fd, pn);
        }
        var F = o.root.transform.forward;
        if (telefon || kubek)
        {
            var d = Dlon(o.dlonP, "R");
            if (d.HasValue)
            {
                var (c, fd, pn) = d.Value;
                if (telefon) Akcesorium(o, "telefon", o.dlonP, c + pn * .014f * o.s + fd * .02f * o.s, Quaternion.LookRotation(pn, fd), null, AkcMaterial("telefon czarny", new Color(.03f, .03f, .035f), .85f));
                else Akcesorium(o, "kubek", o.dlonP, c + pn * .04f * o.s - fd * .045f * o.s, Quaternion.LookRotation(Vector3.ProjectOnPlane(F, fd).normalized, -fd),
                                rnd.NextDouble() < .5 ? new Color(.92f, .91f, .88f) : new Color(.55f, .40f, .26f),
                                AkcMaterial("kubek (kolor)", Color.white, .3f), AkcMaterial("wieczko", new Color(.08f, .08f, .08f), .4f));
            }
        }
        if (rnd.NextDouble() < rola.torba && o.p.kobieta)
        {
            var d = Dlon(o.dlonL, "L");
            if (d.HasValue)
            {
                var (c, fd, pn) = d.Value;
                var kol = new[] { new Color(.12f, .08f, .06f), new Color(.45f, .28f, .15f), new Color(.08f, .08f, .09f), new Color(.55f, .12f, .14f) }[rnd.Next(4)];
                Akcesorium(o, "torba", o.dlonL, c + fd * .02f * o.s, Quaternion.LookRotation(Vector3.ProjectOnPlane(F, fd).normalized, -fd), kol, AkcMaterial("torba (kolor)", Color.white, .45f));
            }
        }
        else if (PlecakiWlaczone && rnd.NextDouble() < rola.plecak && o.spine2)
        {
            var kol = new[] { new Color(.1f, .1f, .11f), new Color(.14f, .22f, .38f), new Color(.45f, .12f, .1f), new Color(.3f, .34f, .22f) }[rnd.Next(4)];
            Akcesorium(o, "plecak", o.spine2, o.spine2.position - F * .19f * o.s - Vector3.up * .06f * o.s, Quaternion.LookRotation(F, Vector3.up), kol, AkcMaterial("plecak (kolor)", Color.white, .3f));
        }
        else if (!PlecakiWlaczone && rola.plecak > 0f) rbPlecakiPominiete++;
    }

    // E15 (build 45, zdjęcie właściciela: „zielony prostopadłościan na plecach”): plecak z brył (3 klocki, bez szelek, odstaje
    // od pleców — wygląda jak zaślepka) WYŁĄCZONY, dopóki nie będzie plecaka z szelkami dopasowanego do pleców postaci
    // (pomiar skóry pleców). Torba w dłoni: zaokrąglona bryła z paskiem zamiast klocka.
    const bool PlecakiWlaczone = false;
    static int rbPlecakiPominiete;

    // ---------------- osoba z gotowym zachowaniem ----------------
    static readonly string[] KlipyPostoj = { "idle_neutral_01", "idle_neutral_02", "idle_neutral_03", "idle_look_around_01", "idle_waiting_01" };
    static readonly string[] KlipyReakcje = { "idle_scratch_head_01", "idle_cough_01", "idle_angry_01", "idle_nervous_01" };
    static readonly string[] KlipyMowi = { "gestic_talk_neutral_01", "gestic_talk_neutral_02", "gestic_talk_relaxed_01", "gestic_laugh_low" };
    static readonly string[] KlipySlucha = { "gestic_listen_neutral_01", "gestic_listen_neutral_02" };

    static BraxPieszy NowyPrzechodzien(Transform parent, Vector3[] trasa, bool[] przejscie, System.Random rnd, int nr)
    {
        var rola = RolaPrzechodzien;
        bool telefon = rnd.NextDouble() < .12, kubek = !telefon && rnd.NextDouble() < rola.kubek;
        var o = NowaOsoba(rola, parent, "Przechodzień " + nr, trasa[0], 0f, rnd); if (o == null) return null;
        string chod = Klip(o, rnd.NextDouble() < .3 ? "walk_stroll_01" : rnd.NextDouble() < .5 ? "walk_neutral_02" : "walk_neutral_01") ?? Klip(o, "walk_neutral_01");
        if (chod == null) { UnityEngine.Object.DestroyImmediate(o.root); rbLudzie--; return null; }
        string bieg = Klip(o, "run_neutral_01");
        var postoje = Klipy(o, KlipyPostoj).ToList();
        if (telefon) postoje.AddRange(Klipy(o, "cell_phone_textmessage", "cell_phone_talk_01"));
        if (kubek) postoje.AddRange(Klipy(o, "drink_drinking"));
        var reakcje = Klipy(o, KlipyReakcje); var mowi = Klipy(o, KlipyMowi); var slucha = Klipy(o, KlipySlucha);
        DodajKlipy(o, new[] { chod, bieg }.Concat(postoje).Concat(reakcje).Concat(mowi).Concat(slucha));
        AkcesoriaGlowy(o, rola, rnd);
        Pozuj(o, chod, 0f);
        AkcesoriaDloni(o, rola, rnd, telefon, kubek);
        Pozuj(o, chod, (float)rnd.NextDouble());
        o.anim.clip = rbKlipy[chod];
        var p = o.root.AddComponent<BraxPieszy>();
        p.trasa = trasa; p.przejscie = przejscie; p.a = trasa[0]; p.b = trasa[trasa.Length - 1];
        p.anim = o.anim; p.chod = chod; p.stoi = postoje.FirstOrDefault() ?? chod; p.bieg = bieg ?? "";
        p.postoje = postoje.ToArray(); p.reakcje = reakcje; p.mowi = mowi; p.slucha = slucha;
        float vk = rbPredkosc.TryGetValue(chod, out var v0) && v0 > .3f ? v0 * o.s : 1.3f * o.s;
        p.predkoscKlipu = vk; p.predkosc = vk * Mathf.Lerp(.92f, 1.12f, (float)rnd.NextDouble());
        p.predkoscKlipuBiegu = bieg != null && rbPredkosc.TryGetValue(bieg, out var v1) && v1 > .8f ? v1 * o.s : 3.5f * o.s;
        p.bok = Mathf.Lerp(.2f, .55f, (float)rnd.NextDouble());
        p.glowa = o.glowa; p.szyja = o.szyja; p.kregoslup = o.kregoslup; p.wzrost = o.wzrost;
        p.ton = o.p.kobieta ? Mathf.Lerp(1.25f, 1.55f, (float)rnd.NextDouble()) : Mathf.Lerp(.82f, 1.08f, (float)rnd.NextDouble());
        Zamknij(o, true);
        rbPrzechodnie++;
        return p;
    }

    // Osoba w miejscu: klipy do zmiany, gesty, reakcje; pozycja = stopy (stojąca) albo punkt siedzenia (siedząca).
    static BraxMieszkaniec NowyMieszkaniec(Rola rola, Transform parent, string nazwa, Vector3 pos, float yaw, System.Random rnd, string[] bazyKlipow,
                                           bool siedzi = false, bool telefon = false, bool kubek = false, RbPostac wymus = null)
    {
        var o = NowaOsoba(rola, parent, nazwa, pos, yaw, rnd, wymus); if (o == null) return null;
        var klipy = Klipy(o, bazyKlipow);
        if (klipy.Length == 0) klipy = Klipy(o, siedzi ? new[] { "sit_chair_breathe_01", "sit_chair_idle_neutral_02" } : KlipyPostoj);
        if (klipy.Length == 0) { UnityEngine.Object.DestroyImmediate(o.root); rbLudzie--; return null; }
        var reakcje = siedzi ? new string[0] : Klipy(o, KlipyReakcje); var mowi = siedzi ? new string[0] : Klipy(o, KlipyMowi); var slucha = siedzi ? new string[0] : Klipy(o, KlipySlucha);
        DodajKlipy(o, klipy.Concat(reakcje).Concat(mowi).Concat(slucha));
        AkcesoriaGlowy(o, rola, rnd);
        string chod = Klip(o, "walk_neutral_01");
        if (chod != null && (telefon || kubek || rola.plecak > 0f || rola.torba > 0f)) { Pozuj(o, chod, 0f); AkcesoriaDloni(o, rola, rnd, telefon, kubek); }
        Pozuj(o, klipy[0], (float)rnd.NextDouble());
        var m = o.root.AddComponent<BraxMieszkaniec>();
        m.anim = o.anim; m.klipy = klipy; m.reakcje = reakcje; m.mowi = mowi; m.slucha = slucha; m.siedzi = siedzi;
        m.glowa = o.glowa; m.szyja = o.szyja; m.kregoslup = o.kregoslup; m.ramieL = o.ramieL; m.ramieP = o.ramieP; m.wzrost = o.wzrost;
        m.ton = o.p.kobieta ? Mathf.Lerp(1.25f, 1.55f, (float)rnd.NextDouble()) : Mathf.Lerp(.82f, 1.08f, (float)rnd.NextDouble());
        m.rola = rola.nazwa;
        Zamknij(o, !siedzi);
        return m;
    }

    // Siedząca osoba: korzeń w punkcie siedziska (wysokość powierzchni), przodem w stronę „przod”. Model przesunięty tak,
    // żeby miednica w klipie siedzenia wypadła ok. 12 cm (× skala) nad siedziskiem, nad jego środkiem.
    static BraxMieszkaniec Siedzacy(Rola rola, Transform parent, string nazwa, Vector3 siedzisko, Vector3 przod, System.Random rnd, string[] bazy, bool wAucie = false)
    {
        przod.y = 0f; if (przod.sqrMagnitude < 1e-4f) przod = Vector3.forward;
        float yaw = Quaternion.LookRotation(przod).eulerAngles.y;
        var m = NowyMieszkaniec(rola, parent, nazwa, siedzisko, yaw, rnd, bazy, siedzi: true);
        if (!m) return null;
        var model = m.anim.gameObject; var pel = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Bip01 Pelvis");
        if (pel)
        {
            float s = model.transform.lossyScale.y / Mathf.Max(1e-4f, rbPostacie.First(p => p.prefab.name == model.name).prefab.transform.localScale.y);
            var cel = siedzisko + Vector3.up * .12f * s - przod.normalized * .02f;
            var d = cel - pel.position;
            model.transform.position += d;
            var lod = m.GetComponent<LODGroup>(); if (lod) lod.RecalculateBounds();
        }
        m.wAucie = wAucie;
        rbSiedzacy++;
        return m;
    }
}

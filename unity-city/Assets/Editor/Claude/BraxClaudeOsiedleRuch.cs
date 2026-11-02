using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// RUCH NA OSIEDLU (właściciel 1.11, 16:57–17:04: „ludzi wstaw, żeby chodzili po osiedlu… samochody, żeby sobie pojeździły —
// tylko żeby się ruch toczył; ludzie mają tylko żyć”). Na razie na nowych ulicach części A (DrogiA):
//   • przechodnie: postacie Mixamo z wersji przeglądarkowej (public/assets/people → dekoduj_glb.mjs → Source/Claude/Ludzie),
//     klipy Walking / Idle Braxa (ten sam szkielet Mixamo) przepięte na kości postaci — tylko obroty (różne skale modeli);
//   • auta w ruchu: 4 pętle prawym pasem (skrzyżowania łukami, rondo przeciwnie do wskazówek zegara) + auta zaparkowane.
//     Modele: public/assets/cars-src (licencje w plikach; NC — „DO WYMIANY PRZED SPRZEDAŻĄ”, decyzja właściciela 17:03).
public static partial class BraxClaudeOsiedle
{
    const string LudzieDir = "Assets/Source/Claude/Ludzie", AutaDir = "Assets/Source/Claude/Auta";

    static void RuchNaOsiedlu()
    {
        group = "Ruch: ludzie i auta";
        Przechodnie();
        Auta();
    }

    // ---------------- przechodnie ----------------
    static string NormBone(string n) => n.Replace(":", "").Replace("mixamorig", "").Replace("_", "").ToLowerInvariant();

    static AnimationClip ClipNaSzkielet(AnimationClip src, Transform root, string name, string key)
    {
        if (!src) return null;
        string path = Out + "/ruch_" + Safe(key) + "_" + Safe(name) + ".anim";
        var have = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); if (have) return have;
        var bones = new Dictionary<string, Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) { var k = NormBone(t.name); if (k.Length > 0 && !bones.ContainsKey(k)) bones[k] = t; }
        var dst = new AnimationClip { legacy = true, name = name, wrapMode = WrapMode.Loop, frameRate = src.frameRate };
        int n = 0;
        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            if (b.type != typeof(Transform) || !b.propertyName.StartsWith("m_LocalRotation")) continue;   // tylko obroty
            string bone = b.path.Contains("/") ? b.path.Substring(b.path.LastIndexOf('/') + 1) : b.path;
            if (!bones.TryGetValue(NormBone(bone), out var t)) continue;
            var curve = AnimationUtility.GetEditorCurve(src, b); if (curve == null) continue;
            AnimationUtility.SetEditorCurve(dst, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(t, root), typeof(Transform), b.propertyName), curve);
            n++;
        }
        if (n == 0) { rep.notes.Add("DO POPRAWKI: klip " + name + " nie pasuje do szkieletu " + key); return null; }
        dst.EnsureQuaternionContinuity();
        AssetDatabase.CreateAsset(dst, path);
        return dst;
    }

    static void Przechodnie()
    {
        var walkSrc = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/PlayableGenerated/Walk.anim");   // 1.11 build 12: było „Walking.anim” (nie istnieje) → ludzie bez ruchu
        var idleSrc = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/PlayableGenerated/Idle.anim");
        if (!walkSrc || !idleSrc) { rep.notes.Add("DO POPRAWKI: brak klipów Braxa Walking/Idle — przechodnie bez ruchu"); }
        if (!AssetDatabase.IsValidFolder(LudzieDir)) { rep.notes.Add("DO POPRAWKI: brak " + LudzieDir + " (dekoduj_glb.mjs)"); return; }
        // lżejsze postacie najpierw (tłum); wszystkie mają szkielet Mixamo
        string[] kolej = { "Arissa", "Big_Vegas", "Lola_B_Styperek", "The_Boss", "Kachujin_G_Rosales", "Ch06_nonPBR", "Ch12_nonPBR", "Ch18_nonPBR", "Ch22_nonPBR", "Ch26_nonPBR", "Ch37_nonPBR", "Ch47_nonPBR", "Remy", "Brute" };
        var modele = new List<GameObject>();
        foreach (var id in kolej)
        {
            string p = LudzieDir + "/" + id + ".glb"; if (!File.Exists(p)) continue;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
            if (go && go.GetComponentInChildren<SkinnedMeshRenderer>(true)) modele.Add(go);
        }
        if (modele.Count == 0 || chodnikiA.Count == 0) { rep.notes.Add("DO POPRAWKI: przechodnie — modeli " + modele.Count + ", chodników " + chodnikiA.Count); return; }
        var rnd = new System.Random(1101);
        // część A (nowe chodniki) + część B: chodniki ulicy głównej, ulicy N-S i aleja parku
        var odcinki = new List<(Vector3 a, Vector3 b)>(chodnikiA.OrderBy(_ => rnd.Next()).Take(14));
        float wB = Carriage + Kerb + Green + Walk * .5f;
        foreach (int sd in new[] { -1, 1 })
        {
            odcinki.Add((new Vector3(X0 + 8f, .14f, StreetZ + sd * wB), new Vector3(AxisX - 10f, .14f, StreetZ + sd * wB)));
            odcinki.Add((new Vector3(AxisX + 10f, .14f, StreetZ + sd * wB), new Vector3(X1 - 8f, .14f, StreetZ + sd * wB)));
            odcinki.Add((new Vector3(AxisX + sd * wB, .14f, ParkZ0 - 2f), new Vector3(AxisX + sd * wB, .14f, StreetZ + Half + 2f)));
        }
        odcinki.Add((new Vector3(AxisX + 1f, .05f, FenceA - 3f), new Vector3(AxisX + 1f, .05f, -127f)));   // aleja parku do fontanny
        odcinki.Add((new Vector3(AxisX - 1f, .05f, -137f), new Vector3(AxisX - 1f, .05f, ParkZ0 + 2f)));
        int ile = odcinki.Count, zrobione = 0;
        var klipy = new Dictionary<GameObject, (AnimationClip w, AnimationClip i)>();
        for (int k = 0; k < ile; k++)
        {
            var src = modele[k % modele.Count];
            var o = (GameObject)UnityEngine.Object.Instantiate(src, Group(group), false); o.name = "Przechodzień (" + src.name + ")";
            o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.identity;
            var rs = o.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) { UnityEngine.Object.DestroyImmediate(o); continue; }
            var b = RB(o);
            float h = Mathf.Lerp(1.68f, 1.86f, (float)rnd.NextDouble());
            if (b.size.y > .3f) o.transform.localScale *= h / b.size.y;
            b = RB(o); o.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            foreach (var smr in o.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = false; smr.shadowCastingMode = ShadowCastingMode.On; smr.quality = SkinQuality.Bone2; }
            // klipy: przepięte raz na model (ścieżki względem korzenia instancji = korzenia prefabu)
            if (!klipy.TryGetValue(src, out var kl)) { kl = (ClipNaSzkielet(walkSrc, o.transform, "Walking", src.name), ClipNaSzkielet(idleSrc, o.transform, "Idle", src.name)); klipy[src] = kl; }
            // korzeń ruchu: pusty obiekt (model jako dziecko — skala modelu nie wpływa na ruch)
            var root = new GameObject("Przechodzień " + (k + 1) + " · " + src.name); root.transform.SetParent(Group(group), false);
            Vector3 a = odcinki[k].a, bb = odcinki[k].b;
            root.transform.position = a; o.transform.SetParent(root.transform, false); o.transform.localPosition = new Vector3(o.transform.localPosition.x, o.transform.localPosition.y, o.transform.localPosition.z);
            var anim = o.AddComponent<Animation>(); anim.playAutomatically = true; anim.cullingType = AnimationCullingType.BasedOnRenderers;
            if (kl.w) { anim.AddClip(kl.w, "Walking"); anim.clip = kl.w; }
            if (kl.i) anim.AddClip(kl.i, "Idle");
            var col = root.AddComponent<CapsuleCollider>(); col.height = 1.75f; col.radius = .28f; col.center = new Vector3(0, .9f, 0);
            var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var p = root.AddComponent<BraxPieszy>(); p.a = a; p.b = bb; p.anim = anim; p.predkosc = 1.1f + (float)rnd.NextDouble() * .35f;
            if (!kl.w) p.enabled = false;
            zrobione++;
        }
        rep.notes.Add("przechodnie: " + zrobione + " postaci na chodnikach części A (" + modele.Count + " modeli Mixamo)");
    }

    // ---------------- auta ----------------
    static readonly string[] AutaNC = { "car_918", "car_dc", "car_dia", "car_mar", "car_mus", "car_r8", "car_s4" };   // DO WYMIANY PRZED SPRZEDAŻĄ (CC-BY-NC-SA)

    // Auta (model z geometrii, trasy, ruch, zaparkowane): BraxClaudeOsiedleAuta.cs, BraxClaudeOsiedleAutaModel.cs, BraxTrasyAut.cs (build 14).
}

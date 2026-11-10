using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// MIASTO R1 10.11 — klip „Jazda” Braxa na skuterze, motorówce i jachcie (właściciel 17:37: „siedzieć z przodu i mieć ręce
// na kierownicy — jest animacja driver”). Źródło: oryginalny Driving.fbx właściciela (Mixamo, szkielet Braxa; ten sam plik co
// samochód w Mieście Mistrzów, SHA 74ccdfa8…). CI kopiuje go z wydania właściciela do Assets/Source/Claude/Brax/Ruchy/
// (unity-tools/claude/brax_jazda.py) — plik nie jest w repozytorium. Przepinamy WYŁĄCZNIE obroty kości po nazwie (bez
// translacji: biodra stawia BraxVessel w fotelu), jak w Mistrzach. Brak pliku = poza z kodu (BraxPose), log DO POPRAWKI.
public static class BraxMiastoJazdaBraxa
{
    public const string Zrodlo = "Assets/Source/Claude/Brax/Ruchy/Driving.fbx";
    const string Wyjscie = "Assets/PlayableGenerated/ClaudeRegion/Ruchy/";
    static AnimationClip zrobiony;

    static string Kosc(string pelna)
    {
        string s = (pelna ?? "").Replace('\\', '/');
        int i = s.LastIndexOf('/'); if (i >= 0) s = s.Substring(i + 1);
        i = s.LastIndexOf('|'); if (i >= 0) s = s.Substring(i + 1);
        i = s.LastIndexOf(':'); if (i >= 0) s = s.Substring(i + 1);
        s = s.Trim();
        if (s.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase))
        {
            s = s.Substring("mixamorig".Length).TrimStart(':', '_', '-', '.', ' ');
            while (s.Length > 0 && char.IsDigit(s[0])) s = s.Substring(1);
            s = s.TrimStart(':', '_', '-', '.', ' ');
        }
        return s;
    }

    /// <summary>Dodaje klip „Jazda” do każdego gracza w scenie (Animation na modelu Braxa). Zwraca liczbę graczy.</summary>
    public static int DodajDoGraczy()
    {
        int n = 0;
        foreach (var p in Resources.FindObjectsOfTypeAll<BraxGymPlayer>())
            if (p && p.gameObject.scene.IsValid() && p.clips && Dodaj(p.clips)) n++;
        Debug.Log("BRAXHORN_BRAX_JAZDA klip „Jazda” u graczy: " + n + (File.Exists(Zrodlo) ? "" : " — brak " + Zrodlo + " (poza z kodu) DO POPRAWKI"));
        return n;
    }

    public static bool Dodaj(Animation anim)
    {
        if (!anim) return false;
        if (anim.GetClip("Jazda")) return true;
        var klip = Przepnij(anim);
        if (!klip) return false;
        anim.AddClip(klip, "Jazda");
        return true;
    }

    static AnimationClip Przepnij(Animation anim)
    {
        if (!File.Exists(Zrodlo)) return null;
        try
        {
            if (zrobiony) return zrobiony;
            AssetDatabase.ImportAsset(Zrodlo, ImportAssetOptions.ForceSynchronousImport);
            var src = AssetDatabase.LoadAllAssetsAtPath(Zrodlo).OfType<AnimationClip>()
                .Where(c => c && c.length > .01f && AnimationUtility.GetCurveBindings(c).Length > 0 && !c.name.StartsWith("__preview__"))
                .OrderByDescending(c => AnimationUtility.GetCurveBindings(c).Length).FirstOrDefault();
            if (!src) { Debug.LogWarning("BRAXHORN_BRAX_JAZDA DO POPRAWKI: brak krzywych w " + Zrodlo); return null; }
            var kosci = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in anim.GetComponentsInChildren<Transform>(true)) { var k = Kosc(t.name); if (!kosci.ContainsKey(k)) kosci.Add(k, t); }
            var klip = new AnimationClip { name = "Jazda", legacy = true, frameRate = src.frameRate > 0 ? src.frameRate : 30f, wrapMode = WrapMode.Loop };
            var trafione = new HashSet<string>(StringComparer.OrdinalIgnoreCase); int krzywe = 0, pominiete = 0;
            foreach (var b in AnimationUtility.GetCurveBindings(src))
            {
                if (b.propertyName.IndexOf("Rotation", StringComparison.OrdinalIgnoreCase) < 0) continue;   // tylko obroty kości
                var czesci = b.path.Replace('\\', '/').Split('/');
                string kosc = Kosc(czesci[czesci.Length - 1]);
                if (!kosci.TryGetValue(kosc, out var t)) { pominiete++; continue; }
                var nb = b; nb.path = AnimationUtility.CalculateTransformPath(t, anim.transform); nb.type = typeof(Transform);
                AnimationUtility.SetEditorCurve(klip, nb, AnimationUtility.GetEditorCurve(src, b));
                trafione.Add(kosc); krzywe++;
            }
            if (krzywe < 100 || !trafione.Contains("Hips") || !trafione.Contains("LeftUpLeg") || !trafione.Contains("RightArm") || !trafione.Contains("LeftHand"))
            {
                Debug.LogWarning("BRAXHORN_BRAX_JAZDA DO POPRAWKI: szkielet nie pasuje (krzywe " + krzywe + ", kości " + trafione.Count + ", pominięte " + pominiete + ")");
                return null;
            }
            klip.EnsureQuaternionContinuity();
            if (!AssetDatabase.IsValidFolder(Wyjscie.TrimEnd('/'))) { Directory.CreateDirectory(Wyjscie); AssetDatabase.Refresh(); }
            BraxZapisAssetu.Utworz(klip, Wyjscie + "Jazda.anim");
            Debug.Log("BRAXHORN_BRAX_JAZDA klip z " + Zrodlo + ": " + src.length.ToString("F2") + " s, kości " + trafione.Count + ", krzywe " + krzywe + ", pominięte " + pominiete);
            zrobiony = klip;
            return klip;
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_BRAX_JAZDA DO POPRAWKI: " + e.Message); return null; }
    }
}

/// <summary>Driving.fbx właściciela: import jako klip Legacy (Animation Braxa), bez materiałów i siatek do sceny.</summary>
public sealed class BraxMiastoJazdaImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/Source/Claude/Brax/Ruchy/", StringComparison.Ordinal) || !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return;
        var m = assetImporter as ModelImporter; if (m == null) return;
        m.importAnimation = true;
        m.animationType = ModelImporterAnimationType.Legacy;
        m.animationCompression = ModelImporterAnimationCompression.Optimal;
        m.materialImportMode = ModelImporterMaterialImportMode.None;
        m.importBlendShapes = false;
    }
}

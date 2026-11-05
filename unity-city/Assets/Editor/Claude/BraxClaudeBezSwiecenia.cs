using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// ZASADA „NIC NIE ŚWIECI” DLA CAŁEJ SCENY (audyt miasta 31.10, Opus). Ostatni krok przed zapisem sceny miasta i osiedla.
// Audyt znalazł świecenie w wielu krokach (kula disco ×10, lasery, tunel metra, lampy Nutrition, siłownia, przeniesienie
// emisji do shadera miasta) i lampy punktowe poza ForceVertex (wypełnienie siłowni 26 m sięgało ulicy).
// Zamiast łatać każdy krok osobno — jedna pętla na końcu:
//   1) każdy materiał każdego renderera: słowo kluczowe _EMISSION wyłączone, wszystkie kolory „Emission/Emissive” czarne,
//      GI „emisja czarna”. Materiał z modelu (.glb/.fbx — tylko do odczytu) dostaje kopię w PlayableGenerated/BezSwiecenia.
//   2) każde światło punktowe i stożkowe: ForceVertex, bez cieni (światła postaci są kierunkowe — nietknięte).
// Raport: Assets/PlayableGenerated/BezSwiecenia/<scena>.json (ile materiałów zgaszonych, ile kopii, ile lamp).
public static class BraxClaudeBezSwiecenia
{
    const string Out = "Assets/PlayableGenerated/BezSwiecenia";

    [System.Serializable] sealed class Raport
    {
        public string scena; public int materialy, zgaszone, kopie, lampy, lampyZmienione, oprawyArenyZachowane;
        public List<string> przyklady = new List<string>();
    }

    public static void Apply(Scene scene, string label)
    {
        Directory.CreateDirectory(Out);
        var rep = new Raport { scena = label };
        var done = new Dictionary<Material, Material>();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i]; if (!m) continue;
                if (done.TryGetValue(m, out var d)) { if (d != m) { mats[i] = d; changed = true; } continue; }
                rep.materialy++;
                // Cztery oryginalne grupy LED i szyldow BRAXHORN ARENA
                // zachowuja swoje assety PBR i emisje. Globalny audyt miasta
                // nie moze ich wygaszac, by potem odtwarzac je po nazwie.
                // Dotyczy tylko miasta i oznaczonych materialow z generatora areny.
                if (label == "miasto" && m.name.StartsWith("A świeci: Arena", System.StringComparison.Ordinal))
                {
                    done[m] = m;
                    rep.oprawyArenyZachowane++;
                    continue;
                }
                if (!Glows(m)) { done[m] = m; continue; }
                var target = m;
                string path = AssetDatabase.GetAssetPath(m);
                bool own = !string.IsNullOrEmpty(path) && path.EndsWith(".mat") && !AssetDatabase.IsSubAsset(m);
                bool runtime = string.IsNullOrEmpty(path);
                if (!own && !runtime)
                {
                    target = new Material(m) { name = m.name + " (bez świecenia)" };
                    BraxZapisAssetu.Utworz(target, AssetDatabase.GenerateUniqueAssetPath(Out + "/" + Safe(m.name) + ".mat"));
                    rep.kopie++;
                }
                Dim(target);
                if (own) EditorUtility.SetDirty(target);
                rep.zgaszone++;
                if (rep.przyklady.Count < 40) rep.przyklady.Add(m.name + " ← " + r.name);
                done[m] = target;
                if (target != m) { mats[i] = target; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
        foreach (var root in roots)
        foreach (var l in root.GetComponentsInChildren<Light>(true))
        {
            if (l.type != LightType.Point && l.type != LightType.Spot) continue;
            rep.lampy++;
            if (l.renderMode == LightRenderMode.ForceVertex && l.shadows == LightShadows.None) continue;
            l.renderMode = LightRenderMode.ForceVertex; l.shadows = LightShadows.None; rep.lampyZmienione++;
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Out + "/" + Safe(label) + ".json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_BEZ_SWIECENIA " + label + ": materiały " + rep.materialy + ", zgaszone " + rep.zgaszone + " (kopie " + rep.kopie + "), oryginalne oprawy areny " + rep.oprawyArenyZachowane + ", lampy " + rep.lampy + " (zmienione na ForceVertex " + rep.lampyZmienione + ")");
    }

    static bool Glows(Material m)
    {
        if (m.IsKeywordEnabled("_EMISSION")) return true;
        var sh = m.shader; if (!sh) return false;
        for (int p = 0; p < sh.GetPropertyCount(); p++)
        {
            if (sh.GetPropertyType(p) != ShaderPropertyType.Color) continue;
            string n = sh.GetPropertyName(p);
            if (!IsEmissionName(n)) continue;
            var c = m.GetColor(n);
            if (c.maxColorComponent > .004f) return true;
        }
        return false;
    }

    static void Dim(Material m)
    {
        m.DisableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        var sh = m.shader; if (!sh) return;
        for (int p = 0; p < sh.GetPropertyCount(); p++)
        {
            string n = sh.GetPropertyName(p);
            if (!IsEmissionName(n)) continue;
            var t = sh.GetPropertyType(p);
            if (t == ShaderPropertyType.Color) m.SetColor(n, Color.black);
            else if (t == ShaderPropertyType.Texture) m.SetTexture(n, null);
            else if ((t == ShaderPropertyType.Float || t == ShaderPropertyType.Range) && n.ToLowerInvariant().Contains("intens")) m.SetFloat(n, 0f);
        }
    }

    static bool IsEmissionName(string n)
    {
        var s = n.ToLowerInvariant();
        return s.Contains("emission") || s.Contains("emissive");
    }

    static string Safe(string n)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        n = n.Replace(' ', '_');
        return n.Length > 60 ? n.Substring(0, 60) : n;
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — anty-migotanie.
// 1) Z-fighting: wszystko dobudowane pod korzeniem renowacji dostaje przesunięcie głębi
//    (polygon offset -1/-1 w CityStaticPBR), więc wygrywa ze starą ścianą leżącą w tym samym
//    miejscu. Stare mesze źródłowe (poza korzeniem) zostają bez offsetu.
// 2) Filtrowanie tekstur: komponent runtime przełącza wszystkie tekstury na trilinear + anizo,
//    co usuwa „gotowanie się” cienkich ram okien i gzymsów przy ruchu kamery na telefonie.
public static class BraxRegionFlicker
{
    public static void Apply(Transform root)
    {
        var swapped = new Dictionary<Material, Material>();
        int renderers = 0;
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m || !m.shader || m.shader.name != BraxCityMaterials.ShaderName) continue;
                if (!m.HasProperty("_OffsetFactor")) continue;
                if (Mathf.Abs(m.GetFloat("_OffsetFactor")) > .001f) continue;
                Material o;
                if (!swapped.TryGetValue(m, out o))
                {
                    o = new Material(m);
                    o.name = m.name + " (offset)";
                    o.SetFloat("_OffsetFactor", -1f);
                    o.SetFloat("_OffsetUnits", -1f);
                    AssetDatabase.CreateAsset(o, BraxRegion.Out + "/Materials/offset_" + swapped.Count + ".mat");
                    swapped[m] = o;
                }
                mats[i] = o;
                changed = true;
            }
            if (changed) { r.sharedMaterials = mats; renderers++; }
        }
        BraxRegion.Rep.offsetMaterials = swapped.Count;
        BraxRegion.Rep.offsetRenderers = renderers;

        var quality = root.GetComponentInChildren<BraxRegionTextureQuality>(true);
        if (!quality)
        {
            var g = new GameObject("CLAUDE jakość tekstur (trilinear, anizo)");
            g.transform.SetParent(root, false);
            quality = g.AddComponent<BraxRegionTextureQuality>();
        }
        quality.anisotropy = 4;

        // Napisy 3D z testem głębi (domyślny GUI/Text Shader ma ZTest Always i prześwituje przez ściany).
        var signage = root.GetComponentInChildren<BraxRegionSignage>(true);
        if (!signage)
        {
            var g = new GameObject("CLAUDE napisy 3D z testem głębi");
            g.transform.SetParent(root, false);
            signage = g.AddComponent<BraxRegionSignage>();
        }
        var shader3d = Shader.Find("GUI/3D Text Shader");
        if (shader3d)
        {
            var template = new Material(shader3d) { name = "Napis 3D: szablon z testem głębi" };
            AssetDatabase.CreateAsset(template, BraxRegion.Out + "/Materials/signage-3d-text.mat");
            signage.depthTested = template;
        }
        else BraxRegion.Note("brak shadera GUI/3D Text Shader, napisy zostają z ZTest Always");
        BraxRegion.Note("offset głębi: materiałów " + swapped.Count + ", rendererów " + renderers);
    }
}

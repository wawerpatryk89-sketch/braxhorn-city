using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — fałszywe pokoje za oknami budynków nieużytkowych (decyzja właściciela 31.10: „fejkowe okna, że widać pomieszczenie”).
// Elewacje z BraxCityArchitecture mają 0,5 m za szkłem czarną płytę „Deep window interior” (stąd czarne okna).
// Ta płyta dostaje materiał Brax/CityInteriorMap: shader liczy per piksel pokój 4,8×3,2×5 m ze ścianami z tynku
// (white_plaster_02), podłogą z drewna (wood_floor) i sufitem; część pokoi z roletami, część ciemna. Nic nie świeci.
// Jedna siatka, jeden materiał — zero dodatkowej geometrii, zero dodatkowych tekstur poza dwiema z Poly Haven.
public static class BraxRegionInteriorMap
{
    const string BackingName = "Deep window interior";

    public static void Apply(Transform root)
    {
        var shader = Shader.Find("Brax/CityInteriorMap");
        if (!shader) { BraxRegion.Note("interior mapping: brak shadera Brax/CityInteriorMap"); return; }
        var wall = Albedo("white_plaster_02", "plastered_wall_04", "painted_plaster_wall");
        var floor = Albedo("wood_floor", "plank_flooring_02", "laminate_floor_02", "wood_planks_grey");
        if (!wall || !floor) { BraxRegion.Note("interior mapping: brak tekstur ściany/podłogi (white_plaster_02 / wood_floor)"); return; }

        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        string path = BraxRegion.Out + "/Materials/interior-map.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(shader) { name = "Interior mapping: pokoje za oknami" }; AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetTexture("_WallTex", wall); m.SetTexture("_FloorTex", floor); m.SetTexture("_CeilTex", wall);
        m.SetVector("_RoomSize", new Vector4(4.8f, 3.2f, 5.0f, 0));
        // Kondygnacje elewacji: parter 0,15..3,15, piętra 3,75 + k·3,2 → siatka pionowa od 0,55 m co 3,2 m.
        m.SetVector("_RoomOffset", new Vector4(0, .55f, 0, 0));
        m.SetVector("_TexMetres", new Vector4(3f, 3f, 3f, 0));
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);

        int slots = 0, renderers = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i] || !mats[i].name.StartsWith(BackingName)) continue;
                mats[i] = m; changed = true; slots++;
            }
            if (changed) { r.sharedMaterials = mats; renderers++; }
        }
        BraxRegion.Note("interior mapping: pokoje za oknami na " + renderers + " rendererach (" + slots + " slotów materiału)");
    }

    static Texture2D Albedo(params string[] slugs)
    {
        foreach (var slug in slugs)
        {
            string ph = BraxRegion.PH + "textures/" + slug + "/diffuse.jpg";
            string cr = "Assets/Source/CityRebuild/textures/" + slug + "/2k/" + slug + "_diff_2k.jpg";
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(File.Exists(ph) ? ph : cr);
            if (t) return t;
        }
        return null;
    }
}

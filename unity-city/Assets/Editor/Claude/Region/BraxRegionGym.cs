using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// BRAXHORN CITY — siłownia (wizytówka): strefa judo z czerwonym tatami (Poly Haven tatami_mat),
// zegar ścienny, gaśnica, kosz, roślina przy wyjściu. Stawiane tylko w wolnym miejscu hali
// (kolizje sprawdzane po odbudowie sali i sprzętu), ring, worki i maty treningowe bez zmian.
// Wywołanie: BraxGymBuild.BuildInternal po BraxGymReferenceEquipment.Apply.
public static class BraxRegionGym
{
    [Serializable] sealed class Report { public bool tatami; public Vector3 tatamiAt; public int props; public List<string> notes = new List<string>(); }

    public static void Apply(GameObject gym, BraxGymPlayer runtime)
    {
        if (!BraxRegion.Enabled) return;
        var rep = new Report();
        try { Build(gym, rep); }
        catch (Exception e) { rep.notes.Add("BŁĄD: " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GYM " + e); }
        try { NicerBags(runtime, rep); }
        catch (Exception e) { rep.notes.Add("worki: BŁĄD " + e.Message); Debug.LogError("BRAXHORN_CLAUDE_GYM worki " + e); }
        Directory.CreateDirectory(BraxRegion.Out);
        File.WriteAllText(BraxRegion.Out + "/gym.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_GYM " + JsonUtility.ToJson(rep));
    }

    // Worki (decyzja właściciela: „worki ładniejsze”). Worek z Blendera to matowa, prawie czarna skóra
    // (#171a1b, roughness .69, ziarno .38) — na telefonie czarna plama bez kształtu. Tu: ciemny grafit ze
    // sheenem (roughness .42), wyraźne ziarno skóry (normal ×1.0), wzmocnienia ciemniejsze z lekkim połyskiem,
    // chromowane okucia i łańcuch. Materiały zostają na shaderze importu (reagują na światła sali, warstwa 10).
    static void NicerBags(BraxGymPlayer runtime, Report rep)
    {
        if (!runtime || runtime.bags == null) return;
        var swapped = new Dictionary<Material, Material>();
        int renderers = 0;
        foreach (var pivot in runtime.bags)
        {
            if (!pivot) continue;
            foreach (var r in pivot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (!m) continue;
                    Material o;
                    if (!swapped.TryGetValue(m, out o))
                    {
                        string n = m.name.ToLowerInvariant();
                        if (n.Contains("matte black leather")) o = Tuned(m, "Worek: skóra grafitowa z połyskiem", new Color(.13f, .14f, .15f), .24f, .8f);   // 31.10: skóra ma się błyszczeć
                        else if (n.Contains("reinforced leather")) o = Tuned(m, "Worek: wzmocnienia i taśmy", new Color(.09f, .10f, .10f), .52f, .9f);
                        else if (n.Contains("suspension hardware")) o = Tuned(m, "Worek: chromowane okucia", new Color(.80f, .81f, .82f), .22f, 0f);
                        else if (n.Contains("stitching")) o = Tuned(m, "Worek: przeszycia", new Color(.52f, .55f, .50f), .80f, 0f);
                        else o = m;
                        swapped[m] = o;
                    }
                    if (o != m) { mats[i] = o; changed = true; }
                }
                if (changed) { r.sharedMaterials = mats; renderers++; }
            }
        }
        rep.notes.Add("worki: materiałów " + swapped.Count(kv => kv.Key != kv.Value) + ", rendererów " + renderers);
    }

    static Material Tuned(Material src, string name, Color baseColour, float roughness, float normalScale)
    {
        var m = new Material(src) { name = name };
        if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", baseColour);
        if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", roughness);
        if (normalScale > 0 && m.HasProperty("normalTexture_scale")) m.SetFloat("normalTexture_scale", normalScale);
        if (m.HasProperty("_Color")) m.SetColor("_Color", baseColour);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 1f - roughness);
        if (m.HasProperty("_Roughness")) m.SetFloat("_Roughness", roughness);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        AssetDatabase.CreateAsset(m, BraxRegion.Out + "/Materials/worek-" + name.Replace(' ', '_').Replace(':', '_') + ".mat");
        return m;
    }

    static void Build(GameObject gym, Report rep)
    {
        Physics.SyncTransforms();
        var root = new GameObject("CLAUDE siłownia: strefa judo i dodatki").transform;
        root.SetParent(gym ? gym.transform.parent : null, false);
        root.position = Vector3.zero;

        // 1) Strefa judo 6×6 m: szukamy wolnego miejsca w hali (x -19..10, z -8..8), z dala od ringu i worków.
        var tatami = BraxRegion.Pbr("Siłownia: tatami judo (czerwone)", 1.0f, new Color(.80f, .16f, .14f), "tatami_mat", "rubber_tiles", "leather_white");
        var tatamiGreen = BraxRegion.Pbr("Siłownia: tatami judo (zielone pole)", 1.0f, new Color(.25f, .55f, .22f), "tatami_mat", "rubber_tiles", "leather_white");
        var edge = BraxRegion.Fitting("Siłownia: listwa krawędziowa tatami", new Color(.08f, .08f, .08f), .7f, 0f);
        Vector3? spot = null;
        for (float z = 7.5f; z >= -7.5f && !spot.HasValue; z -= 1.5f)
            for (float x = -18f; x <= 9f; x += 1.5f)
            {
                RaycastHit h;
                if (!BraxRegion.Ground(x, z, -.5f, .6f, out h)) continue;
                if (h.point.y > .2f) continue;                                     // tylko podłoga hali
                if (!BraxRegion.Free(new Vector3(x, h.point.y + 1.35f, z), new Vector3(7.2f, 2.0f, 7.2f))) continue;
                if (Mathf.Abs(x - (-9f)) < 8.5f && Mathf.Abs(z - (-5.1f)) < 8.8f) continue;   // ring
                if (x > 10f) continue;                                               // rama worków przy x=14
                spot = new Vector3(x, h.point.y, z); break;
            }
        if (spot.HasValue)
        {
            var c = spot.Value;
            const float S = 6f, T = .06f;
            BraxRegion.Box("Siłownia: tatami pole walki", root, c + new Vector3(0, T * .5f + .005f, 0), new Vector3(S - 1.2f, T, S - 1.2f), tatami, true, 1f);
            // obwódka zielona (strefa bezpieczeństwa) jak na prawdziwym tatami
            BraxRegion.Box("Siłownia: tatami strefa N", root, c + new Vector3(0, T * .5f, S * .5f - .3f), new Vector3(S, T, .6f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa S", root, c + new Vector3(0, T * .5f, -S * .5f + .3f), new Vector3(S, T, .6f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa W", root, c + new Vector3(-S * .5f + .3f, T * .5f, 0), new Vector3(.6f, T, S - 1.2f), tatamiGreen, true, 1f);
            BraxRegion.Box("Siłownia: tatami strefa E", root, c + new Vector3(S * .5f - .3f, T * .5f, 0), new Vector3(.6f, T, S - 1.2f), tatamiGreen, true, 1f);
            foreach (var (dx, dz, sx, sz) in new[] { (0f, S * .5f + .02f, S + .08f, .04f), (0f, -S * .5f - .02f, S + .08f, .04f), (S * .5f + .02f, 0f, .04f, S + .08f), (-S * .5f - .02f, 0f, .04f, S + .08f) })
                BraxRegion.Box("Siłownia: listwa tatami", root, c + new Vector3(dx, T * .5f, dz), new Vector3(sx, T + .004f, sz), edge);
            rep.tatami = true; rep.tatamiAt = c;
        }
        else rep.notes.Add("brak wolnego miejsca 7×7 m na tatami");

        // 2) Dodatki z Poly Haven: zegar nad wyjściem, gaśnica, kosz, roślina przy drzwiach (drzwi ok. (6, 11.4)).
        var clock = BraxRegion.Model("wall_clock");
        var ext = BraxRegion.Model("korean_fire_extinguisher_01");
        var bin = BraxRegion.Model("metal_trash_can");
        var pot = BraxRegion.Model("potted_plant_04", "potted_plant_02");
        if (clock)
        {
            var o = (GameObject)UnityEngine.Object.Instantiate(clock, root, false);
            o.name = "Siłownia: zegar"; o.transform.position = Vector3.zero;
            o.transform.rotation = Quaternion.Euler(0, 180, 0) * o.transform.rotation;
            var b = BraxRegion.Bounds(o); float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (big > .5f && big > .001f) { o.transform.localScale *= .5f / big; b = BraxRegion.Bounds(o); }
            RaycastHit wall;
            // ściana za wyjściem: promień z hali w stronę drzwi (+Z)
            if (Physics.Raycast(new Vector3(2.5f, 2.9f, 6f), Vector3.forward, out wall, 8f, ~0, QueryTriggerInteraction.Ignore))
            { o.transform.position = wall.point + Vector3.back * .06f - b.center + new Vector3(0, 0, 0); rep.props++; }
            else UnityEngine.Object.DestroyImmediate(o);
        }
        foreach (var (model, label, at, h) in new[] { (ext, "Siłownia: gaśnica", new Vector3(8.8f, 0, 10.6f), .7f), (bin, "Siłownia: kosz", new Vector3(3.2f, 0, 10.6f), .95f), (pot, "Siłownia: roślina przy wyjściu", new Vector3(9.6f, 0, 10.2f), 1.6f) })
        {
            if (!model) continue;
            RaycastHit g;
            if (!BraxRegion.Ground(at.x, at.z, -.5f, .6f, out g) || g.point.y > .2f) continue;
            if (!BraxRegion.Free(g.point + Vector3.up * .6f, new Vector3(.9f, 1.1f, .9f))) continue;
            if (BraxRegion.Put(root, model, label, g.point, 0f, 1f, true, h)) rep.props++;
        }
    }
}

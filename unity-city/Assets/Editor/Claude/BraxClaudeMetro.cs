using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY: stacja metra „BRAXHORN — NABRZEŻE” pod istniejącym holem Codexa.
// Hol (podłoga -3.25 m) kończy się na z = 48.5. Stąd drugie schody w dół do peronu (podłoga -7.25 m),
// żeby 5-metrowa hala w całości była pod ziemią i nie wystawała nad ulicę ani nad wodę (x < 84).
// Peron wzdłuż X, za szklanymi drzwiami peronowymi tor z wagonem (stoi, ruch później), po obu stronach tunel.
public static class BraxClaudeMetro
{
    const string MD = "Assets/Source/Claude/Metro/";
    const string PH = "Assets/Source/Claude/PolyHaven/";
    const float Vest = -3.25f, Floor = -7.25f, Ceil = -2.35f;           // hala: 4.9 m wysokości
    const float StairX = 78f, StairW = 3.6f, Z0 = 48.6f;                  // schody od końca holu
    const float HX0 = 58f, HX1 = 83f;                                     // długość hali 25 m
    const float PZ0 = 56.4f, PZ1 = 62.6f, TZ1 = 67.2f;                    // peron 6.2 m, tor 4.6 m
    const float Track = Floor - 1.05f;                                    // poziom torowiska

    [Serializable] sealed class Report { public string status = "NOT_RUN", error = ""; public int removedBlockers, tunnelSegments; public bool train; public List<string> notes = new List<string>(); }
    static Transform root;

    static void Box(string n, Vector3 at, Vector3 size, Material m, bool solid = false) => BraxCityArchitecture.Box(n, root, at, size, m, solid);

    static Material Pbr(string name, string slug, float metres, Color tint)
    {
        string d = PH + "textures/" + slug + "/";
        Texture2D T(string f) { AssetDatabase.ImportAsset(d + f); return AssetDatabase.LoadAssetAtPath<Texture2D>(d + f); }
        var m = BraxCityMaterials.Textured(name, T("diffuse.jpg"), T("normal.jpg"), T("arm.jpg"), 1f / metres);
        m.SetColor("_Color", tint); EditorUtility.SetDirty(m); return m;
    }

    static GameObject Load(string path)
    {
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
    }
    static Bounds RB(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>(true); var b = rs[0].bounds;
        foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }

    // Pochylona kolizja schodów (rampa), żeby postać płynnie schodziła.
    static void Ramp(string n, Vector3 top, Vector3 bottom, float width)
    {
        var g = new GameObject(n); g.transform.SetParent(root, false);
        var mid = (top + bottom) * .5f; var dir = bottom - top;
        g.transform.position = mid - Vector3.up * .05f;
        g.transform.rotation = Quaternion.LookRotation(new Vector3(0, dir.y, dir.z).normalized, Vector3.up);
        var c = g.AddComponent<BoxCollider>(); c.size = new Vector3(width, .1f, dir.magnitude);
    }

    public static void Apply(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        var rep = new Report();
        try { Build(cityRoot, rep); rep.status = "BUILT"; }
        catch (Exception e) { rep.status = "FAILED"; rep.error = e.Message; Debug.LogError("BRAXHORN_CLAUDE_METRO " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeMetro");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeMetro/metro.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_CLAUDE_METRO " + JsonUtility.ToJson(rep));
    }

    static void Build(Transform cityRoot, Report rep)
    {
        root = new GameObject("CLAUDE stacja metra BRAXHORN NABRZEŻE").transform;
        root.SetParent(cityRoot, false); root.position = Vector3.zero;

        // 0) Zaślepki „przyszłej stacji” na końcu holu: wyłączamy wszystko, co stoi w przejściu (oprócz podłogi holu).
        var gate = new Bounds(new Vector3(StairX, Vest + 1.6f, 48.9f), new Vector3(StairW + .4f, 3.0f, 1.4f));
        foreach (var c in cityRoot.GetComponentsInChildren<Collider>(true))
        {
            if (c.transform.IsChildOf(root) || !c.enabled) continue;
            var b = c.bounds;
            if (!gate.Intersects(b) || b.max.y < Vest + .3f) continue;
            if (b.size.z > 1.5f || b.size.x < 1.5f || b.center.z < 48.0f || b.center.z > 49.8f) continue;
            c.enabled = false; rep.removedBlockers++;
            foreach (var r in c.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.transform.IsChildOf(root) || !r.enabled) continue; var b = r.bounds;
            if (!gate.Intersects(b) || b.max.y < Vest + .3f) continue;
            if (b.size.z > 1.5f || b.size.x < 1.5f || b.size.y < 1.5f || b.center.z < 48.0f || b.center.z > 49.8f) continue;
            r.enabled = false; rep.removedBlockers++;
        }

        // Materiały
        var tiles = BraxCityMaterials.Surface("Metro: białe kafle (matowe)", new Color(.88f, .88f, .86f), .55f);
        var dark = BraxCityMaterials.Surface("Metro: grafitowe panele", new Color(.13f, .14f, .15f), .6f, .3f);
        var steel = BraxCityMaterials.Surface("Metro: szczotkowana stal", new Color(.62f, .63f, .64f), .32f, .9f);
        var glass = BraxCityMaterials.Surface("Metro: szkło drzwi peronowych", new Color(.70f, .80f, .82f, .2f), .05f);
        var yellow = BraxCityMaterials.Surface("Metro: żółta linia bezpieczeństwa", new Color(.86f, .70f, .08f), .8f);
        var ballast = BraxCityMaterials.Surface("Metro: tłuczeń torowiska", new Color(.20f, .19f, .18f), .98f);
        var rail = BraxCityMaterials.Surface("Metro: szyna", new Color(.45f, .44f, .43f), .35f, .9f);
        var light = BraxCityMaterials.Surface("Metro: oprawa światła (bez poświaty)", new Color(.95f, .94f, .90f), .6f);
        Material floor = File.Exists(PH + "textures/granite_tile/diffuse.jpg") ? Pbr("Metro: granitowa posadzka", "granite_tile", 1.2f, new Color(.55f, .55f, .56f)) : dark;
        Material wallStone = File.Exists(PH + "textures/rectangular_facade_tiles/diffuse.jpg") ? Pbr("Metro: okładzina ścian", "rectangular_facade_tiles", 1.6f, new Color(.92f, .92f, .9f)) : tiles;

        // 1) Schody z holu na peron (4 m w dół), obudowane.
        float drop = Vest - Floor; int steps = 25; float rise = drop / steps, run = .3f;
        float z = Z0;
        for (int i = 0; i < steps; i++)
        {
            float top = Vest - i * rise;
            Box("Metro: stopień", new Vector3(StairX, top - rise * .5f - .15f, z + run * .5f), new Vector3(StairW, rise + .3f, run), floor);
            Box("Metro: nosek stopnia", new Vector3(StairX, top - .01f, z + .03f), new Vector3(StairW - .1f, .02f, .05f), yellow);
            z += run;
        }
        float zStairEnd = z;
        Ramp("Metro: rampa schodów (kolizja)", new Vector3(StairX, Vest, Z0), new Vector3(StairX, Floor, zStairEnd), StairW);
        foreach (float side in new[] { -1f, 1f })
        {
            float wx = StairX + side * (StairW * .5f + .12f);
            Box("Metro: ściana schodów", new Vector3(wx, (Floor + Vest) * .5f + 1.2f, (Z0 + zStairEnd) * .5f), new Vector3(.24f, drop + 2.6f, zStairEnd - Z0 + .4f), wallStone, true);
            Box("Metro: poręcz", new Vector3(wx - side * .17f, (Floor + Vest) * .5f + .95f, (Z0 + zStairEnd) * .5f), new Vector3(.06f, .06f, zStairEnd - Z0), steel);
        }
        Box("Metro: strop nad schodami", new Vector3(StairX, Vest + 2.75f, (Z0 + zStairEnd) * .5f), new Vector3(StairW + .5f, .25f, zStairEnd - Z0 + .4f), tiles, true);

        // 2) Hala peronu
        float hx = (HX0 + HX1) * .5f, hl = HX1 - HX0;
        Box("Metro: posadzka peronu", new Vector3(hx, Floor - .15f, (PZ0 + PZ1) * .5f), new Vector3(hl, .3f, PZ1 - PZ0), floor, true);
        Box("Metro: łącznik schody–peron", new Vector3(StairX, Floor - .15f, (zStairEnd + PZ0) * .5f), new Vector3(StairW + .4f, .3f, Mathf.Max(.2f, PZ0 - zStairEnd + .2f)), floor, true);
        Box("Metro: strop hali", new Vector3(hx, Ceil + .15f, (PZ0 + TZ1) * .5f), new Vector3(hl + .5f, .3f, TZ1 - PZ0 + 1f), tiles, true);
        float gapL = StairX - StairW * .5f - .2f, gapR = StairX + StairW * .5f + .2f;
        foreach (var (a0, a1) in new[] { (HX0, gapL), (gapR, HX1) })
        {
            if (a1 - a0 < .2f) continue;
            Box("Metro: ściana tylna peronu", new Vector3((a0 + a1) * .5f, (Floor + Ceil) * .5f, PZ0 - .12f), new Vector3(a1 - a0, Ceil - Floor, .24f), wallStone, true);
            Box("Metro: lamperia", new Vector3((a0 + a1) * .5f, Floor + .6f, PZ0 + .01f), new Vector3(a1 - a0, 1.2f, .04f), dark);
            Box("Metro: pas BRAXHORN (matowa zieleń)", new Vector3((a0 + a1) * .5f, Floor + 2.6f, PZ0 + .01f), new Vector3(a1 - a0, .12f, .04f),
                BraxCityMaterials.Surface("Metro: matowa zieleń marki", new Color(.33f, .55f, .18f), .85f));
        }
        Box("Metro: nadproże nad wejściem ze schodów", new Vector3(StairX, (Floor + 2.6f + Ceil) * .5f, PZ0 - .12f), new Vector3(gapR - gapL, Ceil - Floor - 2.6f, .24f), wallStone, true);
        Box("Metro: ściana za torem", new Vector3(hx, (Track + Ceil) * .5f, TZ1 + .12f), new Vector3(hl, Ceil - Track, .24f), wallStone, true);
        foreach (float ex in new[] { HX0, HX1 })
            Box("Metro: ściana szczytowa peronu", new Vector3(ex, (Floor + Ceil) * .5f, (PZ0 + PZ1) * .5f), new Vector3(.24f, Ceil - Floor, PZ1 - PZ0), wallStone, true);
        // otwór na schody w ścianie tylnej: zastępujemy fragment ściany portalem
        // kolumny
        for (float cx = HX0 + 4f; cx < HX1 - 2f; cx += 5f)
            Box("Metro: kolumna", new Vector3(cx, (Floor + Ceil) * .5f, PZ0 + 2.2f), new Vector3(.55f, Ceil - Floor, .55f), wallStone, true);
        // krawędź peronu: żółta linia + drzwi peronowe (szkło w stalowych ramach)
        Box("Metro: żółta linia", new Vector3(hx, Floor + .006f, PZ1 - .55f), new Vector3(hl - .5f, .01f, .3f), yellow);
        for (float px = HX0 + .8f; px < HX1 - .8f; px += 2.4f)
        {
            Box("Metro: drzwi peronowe szyba", new Vector3(px + 1.1f, Floor + 1.05f, PZ1 - .08f), new Vector3(2.0f, 1.9f, .03f), glass, true);
            Box("Metro: słupek drzwi peronowych", new Vector3(px, Floor + 1.1f, PZ1 - .08f), new Vector3(.12f, 2.2f, .12f), steel, true);
        }
        Box("Metro: belka nad drzwiami peronowymi", new Vector3(hx, Floor + 2.25f, PZ1 - .08f), new Vector3(hl - .4f, .12f, .14f), steel);
        // ławki i kosze
        var bench = Load(PH + "models/modular_street_seating/modular_street_seating.gltf");
        if (bench)
            foreach (float bx in new[] { HX0 + 3f, HX0 + 8f, HX1 - 8f, HX1 - 3f })
            {
                if (Mathf.Abs(bx - StairX) < 3f) continue;
                var o = (GameObject)UnityEngine.Object.Instantiate(bench, root, false); o.name = "Metro: ławka";
                var b = RB(o); o.transform.position += new Vector3(bx - b.center.x, Floor - b.min.y, PZ0 + 1.0f - b.center.z);
            }
        // światło: oprawy liniowe w suficie + kilka ciepłych lamp (bez cieni, bez poświaty)
        for (float lx = HX0 + 2f; lx < HX1 - 1f; lx += 4f)
        {
            Box("Metro: oprawa liniowa", new Vector3(lx, Ceil - .03f, (PZ0 + PZ1) * .5f), new Vector3(2.4f, .05f, .25f), light);
        }
        foreach (float lx in new[] { HX0 + 4f, hx, HX1 - 4f })
        {
            var l = new GameObject("Metro: światło peronu").AddComponent<Light>(); l.transform.SetParent(root, false);
            l.transform.position = new Vector3(lx, Ceil - .4f, (PZ0 + PZ1) * .5f + .5f); l.type = LightType.Point;
            l.color = new Color(1f, .95f, .88f); l.intensity = 1.5f; l.range = 11f; l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
        }
        var ls = new GameObject("Metro: światło schodów").AddComponent<Light>(); ls.transform.SetParent(root, false);
        ls.transform.position = new Vector3(StairX, (Vest + Floor) * .5f + 2.2f, (Z0 + zStairEnd) * .5f); ls.type = LightType.Point;
        ls.color = new Color(1f, .95f, .88f); ls.intensity = 1.2f; ls.range = 9f; ls.shadows = LightShadows.None;

        // 3) Torowisko + tunel w obie strony
        float tz = (PZ1 + TZ1) * .5f;
        float tunnelEnd0 = HX0 - 26f, tunnelEnd1 = HX1 + .3f;
        Box("Metro: torowisko", new Vector3((tunnelEnd0 + tunnelEnd1) * .5f, Track - .1f, tz), new Vector3(tunnelEnd1 - tunnelEnd0, .2f, TZ1 - PZ1), ballast, true);
        Box("Metro: ściana końca hali nad torem", new Vector3(HX1 + .12f, (Track + Ceil) * .5f, tz), new Vector3(.24f, Ceil - Track, TZ1 - PZ1 + .3f), wallStone, true);
        Box("Metro: ściana krawędzi peronu", new Vector3(hx, (Track + Floor) * .5f, PZ1 + .05f), new Vector3(hl, Floor - Track, .1f), dark, true);
        foreach (float off in new[] { -.72f, .72f })
            Box("Metro: szyna", new Vector3((tunnelEnd0 + tunnelEnd1) * .5f, Track + .08f, tz + off), new Vector3(tunnelEnd1 - tunnelEnd0, .14f, .08f), rail);
        for (float sx = tunnelEnd0 + .3f; sx < tunnelEnd1; sx += .65f)
            Box("Metro: podkład", new Vector3(sx, Track + .01f, tz), new Vector3(.24f, .1f, 2.4f), dark);
        var tunnel = Load(MD + "metro_tunnel_section.glb");
        if (tunnel)
        {
            var probe = (GameObject)UnityEngine.Object.Instantiate(tunnel); probe.transform.rotation = Quaternion.Euler(0, 90, 0);
            var pb = RB(probe); UnityEngine.Object.DestroyImmediate(probe);
            float sc = 4.8f / Mathf.Max(.1f, pb.size.y), seg = pb.size.x * sc;
            foreach (var (start, end) in new[] { (tunnelEnd0, HX0) })
                for (float sx = start; sx < end - .1f; sx += seg)
                {
                    var o = (GameObject)UnityEngine.Object.Instantiate(tunnel, root, false); o.name = "Metro: odcinek tunelu";
                    o.transform.rotation = Quaternion.Euler(0, 90, 0); o.transform.localScale *= sc;
                    var b = RB(o); o.transform.position += new Vector3(sx + seg * .5f - b.center.x, Track - .15f - b.min.y, tz - b.center.z);
                    rep.tunnelSegments++;
                }
            foreach (float ex in new[] { tunnelEnd0 - .3f })
                Box("Metro: ciemny koniec tunelu", new Vector3(ex, Track + 2.5f, tz), new Vector3(.3f, 6f, 6f), BraxCityMaterials.Surface("Metro: czerń tunelu", Color.black, 1f), true);
        }
        else rep.notes.Add("brak modelu tunelu");

        // 4) Wagon na torze (stoi przy peronie; ruch dodamy razem z ruchem ulicznym)
        var train = Load(MD + "metro_train.glb");
        if (train)
        {
            var o = (GameObject)UnityEngine.Object.Instantiate(train, root, false); o.name = "Metro: wagon BRAXHORN";
            var b = RB(o); bool alongZ = b.size.z > b.size.x;
            o.transform.rotation = Quaternion.Euler(0, alongZ ? 90 : 0, 0);
            b = RB(o); float sc = 22f / Mathf.Max(b.size.x, b.size.z); o.transform.localScale *= sc;
            b = RB(o); o.transform.position += new Vector3(hx - b.center.x, Track + .2f - b.min.y, tz - b.center.z);
            b = RB(o);
            if (b.max.y > Ceil - .1f) rep.notes.Add("wagon wyższy niż hala: " + b.size);
            foreach (var r in o.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; }
            var col = new GameObject("Metro: kolizja wagonu").AddComponent<BoxCollider>(); col.transform.SetParent(root, false);
            col.center = b.center; col.size = b.size;
            rep.train = true;
        }
        else rep.notes.Add("brak modelu wagonu");
    }
}

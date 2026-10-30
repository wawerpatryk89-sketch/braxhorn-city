using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — taras widokowy na dachu wieżowca R1_TOWER (quay_corner).
// Tylko testowy build gałęzi claude/port-plaza-20261030 (plik ENABLE_TEST_BUILD.txt).
// Prawdziwe materiały i modele CC0 z Poly Haven, pobierane w buildzie
// (unity-tools/claude/fetch_polyhaven.py). Winda, wejście na dach i środek dachu
// (rezerwacja R1_ROOF_DUEL) zostają wolne. Nic nie świeci.
public static class BraxClaudeTowerTerrace
{
    const string PH = "Assets/Source/Claude/PolyHaven/";
    const string TowerRoot = "R1_TOWER accessible premium lobby lift and roof";
    const float ShaftZ = 53f;

    public static bool Enabled =>
        File.Exists("Assets/Source/HarborBeach/ENABLE_TEST_BUILD.txt") && Directory.Exists(PH);

    static Texture2D Tex(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Poly Haven: brak pliku", path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!t) throw new InvalidOperationException("Poly Haven: tekstura nie zaimportowana " + path);
        return t;
    }

    static Material Pbr(string name, string slug, float metresPerTile)
    {
        string d = PH + "textures/" + slug + "/";
        return BraxCityMaterials.Textured(name, Tex(d + "diffuse.jpg"), Tex(d + "normal.jpg"),
            Tex(d + "arm.jpg"), 1f / metresPerTile);
    }

    static GameObject Source(string id)
    {
        string p = PH + "models/" + id + "/" + id + ".gltf";
        if (!File.Exists(p)) throw new FileNotFoundException("Poly Haven: brak modelu", p);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(p) ??
                 AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
        if (!go) throw new InvalidOperationException("glTFast nie zaimportował " + p);
        return go;
    }

    static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("Model bez siatek: " + go.name);
        var b = rs[0].bounds;
        foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    // Stawia model w skali rzeczywistej (Poly Haven = metry) na podłodze dachu.
    static GameObject Put(Transform parent, string id, string label, Vector3 floor, float yaw, bool solid = true)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(Source(id), parent, false);
        o.name = label;
        o.transform.position = Vector3.zero;
        o.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var b = Bounds(o);
        if (b.size.y < .05f || b.size.y > 6f)
            throw new InvalidOperationException("Nierealna skala modelu " + id + ": " + b.size);
        o.transform.position = floor - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }
        if (solid)
        {
            b = Bounds(o);
            var c = new GameObject(label + " kolizja").AddComponent<BoxCollider>();
            c.transform.SetParent(parent, false);
            c.transform.position = b.center;
            c.size = b.size;
        }
        return o;
    }

    static void Box(string name, Transform root, Vector3 at, Vector3 size, Material m, bool solid = false)
        => BraxCityArchitecture.Box(name, root, at, size, m, solid);

    // Szklana balustrada: niski kamienny cokół, szyby i stalowy pochwyt (bez świecenia).
    static void Balustrade(Transform root, Vector3 a, Vector3 b, float y, Material curb, Material glass, Material steel)
    {
        var dir = b - a; float len = dir.magnitude; var n = dir / len;
        bool alongX = Mathf.Abs(n.x) > .5f;
        Vector3 Size(float l, float h, float t) => alongX ? new Vector3(l, h, t) : new Vector3(t, h, l);
        var mid = (a + b) * .5f;
        Box("Taras: kamienny cokół balustrady", root, mid + Vector3.up * (y + .14f), Size(len, .28f, .34f), curb);
        Box("Taras: stalowy pochwyt", root, mid + Vector3.up * (y + 1.17f), Size(len, .06f, .09f), steel);
        int panels = Mathf.Max(1, Mathf.RoundToInt(len / 1.5f));
        float step = len / panels;
        for (int i = 0; i < panels; i++)
        {
            var c = a + n * (step * (i + .5f));
            Box("Taras: szyba balustrady", root, c + Vector3.up * (y + .72f), Size(step - .06f, .86f, .02f), glass);
            var post = a + n * (step * i);
            Box("Taras: stalowy słupek", root, post + Vector3.up * (y + .72f), Size(.05f, .9f, .05f), steel);
        }
        Box("Taras: stalowy słupek", root, b + Vector3.up * (y + .72f), Size(.05f, .9f, .05f), steel);
    }

    public static void Apply(Transform streetRoot)
    {
        if (!Enabled) return;
        var tower = streetRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == TowerRoot);
        if (!tower) throw new InvalidOperationException("Brak wieżowca: " + TowerRoot);
        var bld = BraxCityArchitecture.StreetBuildings.Single(x => x.id == BraxCityStreetSkyline.AccessibleTowerId);
        var plan = BraxCityStreetSkyline.HighRises.Single(x => x.id == bld.id);
        float roofY = BraxCityStreetSkyline.RoofHeight(bld, plan.additionalFloors);
        float x0 = bld.x0, x1 = bld.x1, z0 = bld.z0, z1 = bld.z1, cx = (x0 + x1) * .5f;
        float width = x1 - x0, depth = z1 - z0;

        var deck = Pbr("Taras: deski tarasowe Poly Haven wood_floor_deck", "wood_floor_deck", 2.0f);
        var curb = Pbr("Taras: granit Poly Haven granite_tile", "granite_tile", 1.2f);
        var glass = BraxCityMaterials.Surface("Taras: bezbarwne szkło hartowane", new Color(.62f, .72f, .74f, .26f), .06f, 0f);
        var steel = BraxCityMaterials.Surface("Taras: szczotkowana stal", new Color(.58f, .59f, .60f), .34f, .85f);

        int deckSwapped = 0, hidden = 0;
        foreach (var r in tower.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("Terrace roof deck")) { r.sharedMaterial = deck; deckSwapped++; }
            // Pełny murek i stare listwy zastępuje szklana balustrada. Kolizje murka
            // zostają (ta sama linia co szkło), więc nie da się spaść z dachu.
            else if (n.EndsWith("roof parapet") || n.StartsWith("Roof handrail") ||
                     n.StartsWith("Roof terrace") || n.StartsWith("Roof parapet"))
            { r.enabled = false; hidden++; }
        }
        if (deckSwapped != 4) throw new InvalidOperationException("Oczekiwano 4 płyt tarasu, jest " + deckSwapped);

        var root = new GameObject("CLAUDE taras widokowy wieżowca").transform;
        root.SetParent(tower, false);
        root.position = Vector3.zero;

        float e = .22f;
        Balustrade(root, new Vector3(x0 + e, 0, z0 + e), new Vector3(x1 - e, 0, z0 + e), roofY, curb, glass, steel);
        Balustrade(root, new Vector3(x0 + e, 0, z1 - e), new Vector3(x1 - e, 0, z1 - e), roofY, curb, glass, steel);
        Balustrade(root, new Vector3(x0 + e, 0, z0 + e), new Vector3(x0 + e, 0, z1 - e), roofY, curb, glass, steel);
        Balustrade(root, new Vector3(x1 - e, 0, z0 + e), new Vector3(x1 - e, 0, z1 - e), roofY, curb, glass, steel);

        // Rośliny w narożnikach (tylne narożniki omijają agregaty HVAC).
        Put(root, "potted_plant_02", "Taras: roślina w donicy SW", new Vector3(x0 + 1.0f, roofY, z0 + 1.0f), 15);
        Put(root, "potted_plant_02", "Taras: roślina w donicy SE", new Vector3(x1 - 1.0f, roofY, z0 + 1.0f), -40);
        Put(root, "potted_plant_04", "Taras: roślina w donicy NW", new Vector3(x0 + 1.0f, roofY, z1 - 1.0f), 70);
        Put(root, "potted_plant_04", "Taras: roślina w donicy NE", new Vector3(x1 - 1.0f, roofY, z1 - 1.0f), -110);

        // Donice wzdłuż boków, poza przejściem do windy i polem przyszłej walki.
        int boxes = 0;
        for (float z = z0 + 4.0f; z < z1 - 5.5f; z += 4.0f)
        {
            if (Mathf.Abs(z - ShaftZ) < 3.2f) continue;
            Put(root, "planter_box_01", "Taras: donica W", new Vector3(x0 + .95f, roofY, z), 90);
            Put(root, "planter_box_01", "Taras: donica E", new Vector3(x1 - .95f, roofY, z), 90);
            boxes += 2;
        }

        // Ławki widokowe przy frontowej balustradzie (widok na ulicę i port).
        Put(root, "modular_street_seating", "Taras: ławka widokowa 1", new Vector3(cx - width * .25f, roofY, z0 + 1.55f), 0);
        Put(root, "modular_street_seating", "Taras: ławka widokowa 2", new Vector3(cx + width * .25f, roofY, z0 + 1.55f), 0);

        // Stoliki z krzesłami z tyłu, przed agregatami, z dala od wyjścia z windy.
        int tables = 0;
        float tz = Mathf.Min(z1 - 6.5f, ShaftZ - 4.5f);
        if (width >= 13f && tz > z0 + 12f)
        {
            Put(root, "outdoor_table_chair_set_01", "Taras: stolik z krzesłami W", new Vector3(x0 + 3.4f, roofY, tz), 20);
            Put(root, "outdoor_table_chair_set_01", "Taras: stolik z krzesłami E", new Vector3(x1 - 3.4f, roofY, tz), -20);
            tables = 2;
        }

        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeTower");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeTower/terrace.json", JsonUtility.ToJson(new Report
        {
            roofY = roofY, width = width, depth = depth, deckPanels = deckSwapped, hiddenOldParts = hidden,
            planters = boxes, tables = tables, plants = 4, benches = 2
        }, true));
        Debug.Log($"BRAXHORN_CLAUDE_TOWER_TERRACE roofY={roofY:F1} {width:F1}x{depth:F1} deck={deckSwapped} planters={boxes} tables={tables}");
    }

    [Serializable] sealed class Report
    {
        public float roofY, width, depth; public int deckPanels, hiddenOldParts, planters, tables, plants, benches;
        public string source = "Poly Haven CC0 (wood_floor_deck, granite_tile, planter_box_01, potted_plant_02/04, outdoor_table_chair_set_01, modular_street_seating)";
    }
}

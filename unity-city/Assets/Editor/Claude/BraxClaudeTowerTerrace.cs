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
        o.transform.rotation = Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
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

        // Miejsce walki: wolny kwadrat 7×7 m (wielkość ringu) wokół R1_ROOF_DUEL.
        var duel = new Vector3(cx, roofY, z0 + 7.4f);
        bool InRing(Vector3 p, float r) => Mathf.Abs(p.x - duel.x) < 3.5f + r && Mathf.Abs(p.z - duel.z) < 3.5f + r;
        int tables = 0;
        float tz = Mathf.Min(z1 - 6.5f, ShaftZ - 4.5f);
        foreach (var t in new[] { new Vector3(x0 + 3.2f, roofY, tz), new Vector3(x1 - 3.2f, roofY, tz) })
        {
            if (width < 12f || InRing(t, 1.3f) || Mathf.Abs(t.x - cx) < 3.2f) continue;
            Put(root, "outdoor_table_chair_set_01", "Taras: stolik z krzesłami " + (tables + 1), t, t.x < cx ? 20 : -20);
            tables++;
        }

        PremiumLift(tower, cx, roofY);
        LobbyAndEntrance(tower, x0, x1, z0, cx);

        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeTower");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeTower/terrace.json", JsonUtility.ToJson(new Report
        {
            roofY = roofY, width = width, depth = depth, deckPanels = deckSwapped, hiddenOldParts = hidden,
            planters = boxes, tables = tables, plants = 4, benches = 2
        }, true));
        Debug.Log($"BRAXHORN_CLAUDE_TOWER_TERRACE roofY={roofY:F1} {width:F1}x{depth:F1} deck={deckSwapped} planters={boxes} tables={tables}");
    }



    // Lobby i wejście od A do Z: marmurowa posadzka, granit i orzech na ścianach, jasny sufit
    // z ciepłym światłem, recepcja, sofy, rośliny, logo BRAXHORN, granitowy portal i daszek.
    static void LobbyAndEntrance(Transform tower, float x0, float x1, float z0, float cx)
    {
        float s0 = ShaftZ - 1.72f;
        var marble = Pbr("Lobby: marmurowa posadzka", "marble_01", 1.1f);
        var granite = Pbr("Lobby: granit okładzin", "granite_tile", 1.0f);
        granite.SetColor("_Color", new Color(.40f, .40f, .42f)); EditorUtility.SetDirty(granite);
        var walnut = Pbr("Lobby: orzech na ścianach", "wood_planks", 1.0f);
        walnut.SetColor("_Color", new Color(.44f, .33f, .25f)); EditorUtility.SetDirty(walnut);
        var champagne = BraxCityMaterials.Surface("Lobby: stal szampańska", new Color(.66f, .58f, .46f), .30f, .92f);
        var ceiling = BraxCityMaterials.Surface("Lobby: jasny matowy sufit", new Color(.86f, .84f, .80f), .95f);

        foreach (var r in tower.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("Walkable imported PBR stone lobby floor")) r.sharedMaterial = marble;
            else if (n.StartsWith("Ground-floor") && n.Contains("wall")) r.sharedMaterial = granite;
            else if (n.StartsWith("Entrance wall")) r.sharedMaterial = granite;
            else if (n.StartsWith("Tower painted lobby interior wall") || n.StartsWith("Tower lobby painted rear wall")) r.sharedMaterial = walnut;
            else if (n.StartsWith("Tower lobby stainless skirting")) r.sharedMaterial = champagne;
        }

        var root = new GameObject("CLAUDE lobby i wejście wieżowca").transform;
        root.SetParent(tower, false); root.position = Vector3.zero;
        float depth = s0 - z0, w = x1 - x0;

        // Jasny podwieszany sufit (zamiast czarnej „dziury”) i ciepłe światło bez poświaty.
        Box("Lobby: sufit podwieszany", root, new Vector3(cx, 3.56f, (z0 + s0) * .5f - .2f), new Vector3(w - .7f, .05f, depth - .8f), ceiling);
        foreach (float fz in new[] { z0 + depth * .25f, z0 + depth * .6f })
            foreach (float fx in new[] { cx - w * .22f, cx + w * .22f })
            {
                var l = new GameObject("Lobby: ciepłe światło sufitowe").AddComponent<Light>();
                l.transform.SetParent(root, false); l.transform.position = new Vector3(fx, 3.3f, fz);
                l.type = LightType.Point; l.color = new Color(1f, .88f, .74f); l.intensity = 1.25f; l.range = 7.5f;
                l.shadows = LightShadows.None; l.renderMode = LightRenderMode.ForcePixel;
            }

        // Recepcja po prawej (z dala od przejścia drzwi → winda).
        if (x1 - cx > 4.6f)
        {
            var d = new Vector3(cx + 2.9f, 0, z0 + Mathf.Min(7f, depth * .45f));
            Box("Lobby: recepcja front granit", root, d + Vector3.up * .55f, new Vector3(3.0f, 1.1f, .75f), granite, true);
            Box("Lobby: recepcja blat marmur", root, d + Vector3.up * 1.125f, new Vector3(3.15f, .05f, .9f), marble);
            Box("Lobby: recepcja listwa", root, d + new Vector3(0, .9f, -.38f), new Vector3(3.0f, .04f, .02f), champagne);
        }
        // Sofy z biblioteki po lewej, przodem do środka.
        const string sofa = "Assets/Source/LibrarySofa02.glb";
        if (File.Exists(sofa) && cx - x0 > 4.6f)
        {
            AssetDatabase.ImportAsset(sofa, ImportAssetOptions.ForceSynchronousImport);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(sofa);
            if (src)
                foreach (float fz in new[] { z0 + depth * .32f, z0 + depth * .62f })
                {
                    var o = (GameObject)UnityEngine.Object.Instantiate(src, root, false);
                    o.name = "Lobby: sofa"; o.transform.rotation = Quaternion.Euler(0, 90, 0) * o.transform.rotation;
                    var b = Bounds(o); float sc = 2.0f / Mathf.Max(b.size.x, b.size.z); o.transform.localScale *= sc;
                    b = Bounds(o); o.transform.position += new Vector3(x0 + 1.2f - b.center.x, .15f - b.min.y, fz - b.center.z);
                    b = Bounds(o); var c = new GameObject("Lobby: sofa kolizja").AddComponent<BoxCollider>();
                    c.transform.SetParent(root, false); c.transform.position = b.center; c.size = b.size;
                }
        }
        Put(root, "potted_plant_04", "Lobby: roślina przy wejściu L", new Vector3(cx - 2.0f, .15f, z0 + .9f), 30);
        Put(root, "potted_plant_04", "Lobby: roślina przy wejściu P", new Vector3(cx + 2.0f, .15f, z0 + .9f), -50);
        Put(root, "potted_plant_02", "Lobby: roślina przy windzie L", new Vector3(cx - 2.6f, .15f, s0 - 1.0f), 80);
        Put(root, "potted_plant_02", "Lobby: roślina przy windzie P", new Vector3(cx + 2.6f, .15f, s0 - 1.0f), -20);

        // Logo BRAXHORN na ścianie (matowe, oświetlone jak reszta, nie świeci).
        const string logo = "Assets/CityRebuild/Brand/brand-wide.png";
        if (File.Exists(logo))
        {
            AssetDatabase.ImportAsset(logo, ImportAssetOptions.ForceSynchronousImport);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(logo);
            var m = new Material(Shader.Find("Standard")) { name = "Lobby: logo BRAXHORN" };
            m.SetTexture("_MainTex", t); m.SetFloat("_Mode", 1); m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cutoff", .4f); m.SetFloat("_Glossiness", .15f); m.renderQueue = 2450;
            Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeTower");
            AssetDatabase.CreateAsset(m, "Assets/PlayableGenerated/ClaudeTower/lobby-logo.mat");
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Lobby: logo BRAXHORN";
            UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(root, false);
            q.transform.position = new Vector3(x0 + .36f, 2.2f, z0 + depth * .47f);
            q.transform.rotation = Quaternion.Euler(0, -90, 0);
            float aspect = t ? (float)t.width / t.height : 3f;
            q.transform.localScale = new Vector3(Mathf.Min(3.4f, depth * .4f), Mathf.Min(3.4f, depth * .4f) / aspect, 1);
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        // Wejście z ulicy: granitowe okładziny po bokach drzwi i daszek ze stali szampańskiej.
        foreach (float side in new[] { -1f, 1f })
            Box("Wejście: granitowa okładzina", root, new Vector3(cx + side * 2.36f, 1.85f, z0 - .07f), new Vector3(2.6f, 3.7f, .1f), granite);
        Box("Wejście: nadproże granit", root, new Vector3(cx, 3.55f, z0 - .07f), new Vector3(2.2f, .3f, .1f), granite);
        Box("Wejście: daszek", root, new Vector3(cx, 3.95f, z0 - 1.15f), new Vector3(7.6f, .18f, 2.4f), champagne);
        Box("Wejście: spód daszka", root, new Vector3(cx, 3.85f, z0 - 1.15f), new Vector3(7.4f, .02f, 2.3f), ceiling);
    }

    // Winda premium: granitowa posadzka, szczotkowana stal w kolorze szampana, ciemne drewno,
    // polerowane poręcze, kamienne portale drzwi na parterze i na dachu. Mechanika windy bez zmian.
    static void PremiumLift(Transform tower, float cx, float roofY)
    {
        var cabin = tower.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Moving physical elevator cabin");
        if (!cabin) throw new InvalidOperationException("Brak kabiny windy");
        var granite = Pbr("Winda: polerowany ciemny granit", "granite_tile", .9f);
        granite.SetColor("_Color", new Color(.36f, .36f, .38f)); EditorUtility.SetDirty(granite);
        var champagne = BraxCityMaterials.Surface("Winda: szczotkowana stal szampańska", new Color(.66f, .58f, .46f), .30f, .92f);
        var polished = BraxCityMaterials.Surface("Winda: polerowana stal poręczy", new Color(.80f, .80f, .80f), .14f, 1f);
        var walnut = Pbr("Winda: ciemne drewno orzechowe", "wood_planks", .9f);
        walnut.SetColor("_Color", new Color(.36f, .26f, .20f)); EditorUtility.SetDirty(walnut);
        foreach (var r in cabin.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("Genuine PBR basalt passenger floor")) r.sharedMaterial = granite;
            else if (n.StartsWith("Lift lateral glass panel")) r.sharedMaterial = champagne;
            else if (n.StartsWith("Lift rear panel")) r.sharedMaterial = walnut;
            else if (n.StartsWith("Lift stainless handrail")) r.sharedMaterial = polished;
        }
        foreach (var r in tower.GetComponentsInChildren<MeshRenderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("Slim charcoal stainless vertical door stile") || n.StartsWith("Premium brushed lower door rail"))
                r.sharedMaterial = champagne;
        }
        // Kamienne portale wokół drzwi windy (parter i dach), front szybu s0 = 53 - 1.72.
        float s0 = ShaftZ - 1.72f;
        var portal = new GameObject("CLAUDE portale windy premium").transform;
        portal.SetParent(tower, false); portal.position = Vector3.zero;
        foreach (float baseY in new[] { .15f, roofY })
        {
            Box("Winda: portal granitowy lewy", portal, new Vector3(cx - 1.72f, baseY + 1.55f, s0 - .30f), new Vector3(.28f, 3.1f, .16f), granite);
            Box("Winda: portal granitowy prawy", portal, new Vector3(cx + 1.72f, baseY + 1.55f, s0 - .30f), new Vector3(.28f, 3.1f, .16f), granite);
            Box("Winda: portal granitowy nadproże", portal, new Vector3(cx, baseY + 3.22f, s0 - .30f), new Vector3(3.72f, .26f, .16f), granite);
            Box("Winda: listwa szampańska", portal, new Vector3(cx, baseY + 3.06f, s0 - .39f), new Vector3(3.2f, .04f, .02f), champagne);
        }
    }

    [Serializable] sealed class Report
    {
        public float roofY, width, depth; public int deckPanels, hiddenOldParts, planters, tables, plants, benches;
        public string source = "Poly Haven CC0 (wood_floor_deck, granite_tile, planter_box_01, potted_plant_02/04, outdoor_table_chair_set_01, modular_street_seating)";
    }
}

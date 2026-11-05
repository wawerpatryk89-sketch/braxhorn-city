using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// A1 local coordinates are nominal metres, relative to the approved (600,800) plot.
// Every primitive is multiplied by BraxClaudeOsiedle.K exactly once.
// This does not modify player models, the estate arena, metro, or neighbouring plots.
public static class BraxChampionsArenaArrival
{
    const string Out = "Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/";
    const string BeltRoot = "Assets/Source/Mistrzowie/Trophies/";
    const string BeltName = "tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419";
    const string BeltAsset = BeltRoot + BeltName + ".fbx";
    const string BeltMaps = BeltRoot + BeltName + ".fbm/";
    static float K => BraxClaudeOsiedle.K;

    static GameObject Box(Transform parent, string name, Material material, Vector3 centre, Vector3 size, bool solid = true)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = name;
        o.transform.SetParent(parent, false);
        o.transform.localPosition = centre * K;
        o.transform.localScale = size * K;
        o.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        return o;
    }

    static Transform Group(Transform parent, string name, Vector3 at)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = at * K;
        return t;
    }

    static Material Paint(string id, Color color)
    {
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("Original city PBR shader missing for arena arrival");
        var m = new Material(shader) { name = "Champion arena " + id, enableInstancing = true };
        m.SetColor("_Color", color);
        m.SetFloat("_Roughness", .78f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_VertexTint", 0f);
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m, Out + id + ".mat");
        return m;
    }

    static void Cord(Transform parent, Material rope, Vector3 a, Vector3 b)
    {
        // Six short, naturally drooping rope sections; no rigid horizontal bars.
        const int segments = 6;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
            Vector3 P(float t) => Vector3.Lerp(a, b, t) - Vector3.up * (.21f * 4f * t * (1f - t));
            var p = P(t0) * K; var q = P(t1) * K;
            var d = q - p;
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Red velvet rope - hanging segment";
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = (p + q) * .5f;
            cylinder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            cylinder.transform.localScale = new Vector3(.038f * K, d.magnitude / 2f, .038f * K);
            cylinder.GetComponent<Renderer>().sharedMaterial = rope;
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        }
    }

    static void Stanchions(Transform parent, Material metal, Material rope, Material carpet)
    {
        // Entrance is at the south (-Z) curtain wall. Keep an unobstructed 6.4m centre aisle.
        Box(parent, "Public red-carpet arrival", carpet,
            new Vector3(0, .239f, -67.0f), new Vector3(5.8f, .018f, 15.4f), false);
        foreach (float x in new[] { -3.5f, 3.5f })
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                float z = -73f + i * 2.65f;
                Box(parent, "Barrier post base", metal, new Vector3(x, .28f, z), new Vector3(.33f, .11f, .33f));
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Championship queue post";
                post.transform.SetParent(parent, false);
                post.transform.localPosition = new Vector3(x, .80f, z) * K;
                post.transform.localScale = new Vector3(.052f, .46f, .052f) * K;
                post.GetComponent<Renderer>().sharedMaterial = metal;
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                Box(parent, "Queue post polished cap", metal,
                    new Vector3(x, 1.27f, z), new Vector3(.16f, .10f, .16f), false);
                Vector3 top = new Vector3(x, 1.18f, z);
                if (i > 0) Cord(parent, rope, prev, top);
                prev = top;
            }
        }
    }

    static void Parking(Transform parent, Material asphalt, Material paint, Material curb, Material dark)
    {
        // Two independent courts, west/east of the preserved 30m central walking axis.
        // Entry lanes connect the z=650 road boundary to the internal two-way parking aisles.
        foreach (float x in new[] { -65f, 65f })
        {
            Box(parent, "Car park - asphalt, real ground collider", asphalt,
                new Vector3(x, .199f, -97f), new Vector3(70f, .078f, 24f));
            Box(parent, "Parking vehicle access lane", asphalt,
                new Vector3(x, .195f, -121f), new Vector3(6.4f, .07f, 26f));
            // 5.2m marked bays, two opposed rows, 6.4m minimum centre manoeuvring zone.
            for (int row = 0; row < 2; row++)
            {
                float a = row == 0 ? -108.6f : -90.6f;
                float b = a + 5.2f;
                for (int i = -10; i <= 10; i++)
                {
                    float stallX = x + i * 3.05f;
                    if (Mathf.Abs(stallX - x) < 3.6f) continue; // driveway gap, no painted phantom bays
                    Box(parent, "Parking stall division - durable white marking", paint,
                        new Vector3(stallX, .243f, (a + b) / 2f),
                        new Vector3(.09f, .008f, b - a), false);
                    if (i < 10)
                        Box(parent, "Parking bay wheel stop", curb,
                            new Vector3(stallX + 1.48f, .29f, row == 0 ? a + .65f : b - .65f),
                            new Vector3(1.28f, .15f, .19f));
                }
                Box(parent, "Parking stall head line", paint,
                    new Vector3(x, .244f, row == 0 ? a : b), new Vector3(63f, .008f, .08f), false);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Box(parent, "Parking outer curb", curb,
                    new Vector3(x + side * 35.15f, .31f, -97f), new Vector3(.25f, .22f, 24f));
            }
            // Accessible wider spaces nearest the pedestrian route, with an unblocked transfer strip.
            float accessibleX = x < 0 ? x + 30.2f : x - 30.2f;
            Box(parent, "Wide accessible bay dividing line", paint,
                new Vector3(accessibleX, .244f, -87.9f), new Vector3(.11f, .008f, 5.2f), false);
            Box(parent, "Pedestrian path out of parking", curb,
                new Vector3(x, .192f, -80f), new Vector3(8f, .03f, 10f));
            // Small solid raised islands guard pedestrians without obstructing the driveway.
            Box(parent, "Parking planting island / protective curb", dark,
                new Vector3(x - 25f, .27f, -111.4f), new Vector3(4f, .15f, 1.0f));
        }
        // Main spectator route stays permanently clear between parking and entrance.
        Box(parent, "Arena public promenade", curb,
            new Vector3(0, .183f, -105.5f), new Vector3(16f, .04f, 63f));
        for (float x = -7f; x <= 7f; x += 14f)
            Box(parent, "Public path edge line", paint,
                new Vector3(x, .211f, -105.5f), new Vector3(.12f, .008f, 54f), false);
    }

    static void Door(Transform parent, string id, string label, float z,
        Material glass, Material metal)
    {
        var root = Group(parent, label + " | functional glass double door", new Vector3(0, .24f, z));
        var hinges = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var h = Group(root, "Real opening glass leaf " + (i + 1),
                new Vector3(side * 1.72f, 0, 0));
            hinges[i] = h;
            Box(h, "Toughened transparent glass", glass,
                new Vector3(-side * .81f, 1.61f, 0), new Vector3(1.60f, 3.22f, .085f));
            Box(h, "Door pull - satin metal", metal,
                new Vector3(-side * .23f, 1.32f, side * .11f),
                new Vector3(.08f, .68f, .08f), false);
        }
        // The exterior generator leaves a real 9.4m opening. Fill the sides
        // with fixed transparent sidelights; the central 3.44m double door opens.
        foreach (float side in new[] { -1f, 1f })
        {
            Box(root, "Framed fixed glass sidelight", glass,
                new Vector3(side * 3.38f, 1.61f, 0),
                new Vector3(2.98f, 3.22f, .09f));
            Box(root, "Sidelight frame", metal,
                new Vector3(side * 4.87f, 1.63f, 0),
                new Vector3(.065f, 3.3f, .14f), false);
        }
        // Exterior opening is 4.5m high; doors are 3.22m. Close the real
        // remaining transom with glass, not an invisible wall or open air gap.
        Box(root, "Fixed clear glass transom over double doors", glass,
            new Vector3(0, 3.74f, 0), new Vector3(9.74f, 1.04f, .085f));
        Box(root, "Slim metal transom beam", metal,
            new Vector3(0, 3.23f, 0), new Vector3(9.74f, .075f, .12f), false);
        var door = root.gameObject.AddComponent<BraxCityDoor>();
        door.id = id; door.label = label; door.leaves = hinges; door.sides = new[] { 1f, -1f };
    }

    static void DressingRooms(Transform parent, Material stone, Material dark, Material metal)
    {
        // Rear of adapted bowl = +Z. Athletes enter from north (+59) to original (+35) gate.
        // Four-metre central route must remain unobstructed.
        foreach (float side in new[] { -1f, 1f })
        {
            float mid = side * 14f, inner = side * 6.2f, outer = side * 22f;
            Box(parent, "Athlete changing room floor", stone,
                new Vector3(mid, .239f, 45.5f), new Vector3(15.8f, .018f, 14f), false);
            Box(parent, "Rear changing room exterior partition", dark,
                new Vector3(mid, 1.9f, 52.5f), new Vector3(15.8f, 3.5f, .16f));
            Box(parent, "Changing room front privacy partition", dark,
                new Vector3(mid, 1.9f, 38.5f), new Vector3(15.8f, 3.5f, .16f));
            Box(parent, "Changing room enclosed ceiling", dark,
                new Vector3(mid, 3.62f, 45.5f), new Vector3(15.8f, .13f, 14f), false);
            Box(parent, "Changing room outer partition", dark,
                new Vector3(outer, 1.9f, 45.5f), new Vector3(.16f, 3.5f, 14f));
            // Break the inward wall to leave a real 2.4m dressing-room entrance.
            Box(parent, "Changing room inner wall 1", dark,
                new Vector3(inner, 1.9f, 41.2f), new Vector3(.16f, 3.5f, 5.4f));
            Box(parent, "Changing room inner wall 2", dark,
                new Vector3(inner, 1.9f, 50.6f), new Vector3(.16f, 3.5f, 3.8f));
            Box(parent, "Locker room bench", stone,
                new Vector3(mid, .64f, 48.5f), new Vector3(4.8f, .25f, .65f));
            for (int i = 0; i < 5; i++)
                Box(parent, "Individual athlete locker", metal,
                    new Vector3(mid - 5f + i * 2.45f, 1.48f, 51.5f),
                    new Vector3(2.2f, 2.65f, .62f));
            // Display name says what the room actually contains; no false WC/showers.
        }
    }

    static Material ChampionshipBeltPbr()
    {
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltMaps + "Color.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltMaps + "Normal.png");
        var arm = AssetDatabase.LoadAssetAtPath<Texture2D>(BeltRoot + "ChampionshipBelt_ARM.png");
        if (!color || !normal || !arm)
        {
            Debug.LogWarning("CHAMPION_BELT_TEXTURES_MISSING: colour, normal and packed roughness/metallic must be restored without changing source");
            return null;
        }
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("Original city PBR shader missing");
        var m = new Material(shader) { name = "Original world champion belt PBR", enableInstancing = true };
        m.SetColor("_Color", Color.white);
        m.SetTexture("_MainTex", color);
        m.SetTexture("_BumpMap", normal);
        m.SetTexture("_ARM", arm);
        m.SetFloat("_NormalEncoding", 0);
        m.SetFloat("_Roughness", 1);
        m.SetFloat("_Metallic", 1);
        m.SetFloat("_UseARMForAO", 0); // No AO data provided; channel R is neutral white.
        m.SetFloat("_VertexTint", 0);
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m, Out + "arena_championship_belt_pbr.mat");
        return m;
    }

    static bool Trophy(Transform parent, Material glass, Material metal, Material stone)
    {
        bool found = false;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(BeltAsset);
        // One original belt, placed visibly beside the public route to the stands.
        foreach (float x in new[] { 9f })
        {
            var cabinet = Group(parent, "Champion belt glass museum display", new Vector3(x, 0, -38.8f));
            Box(cabinet, "Museum stone plinth", stone,
                new Vector3(0, .54f, 0), new Vector3(2.7f, .66f, 1.35f));
            Box(cabinet, "Warm metal belt support", metal,
                new Vector3(0, .95f, 0), new Vector3(1.8f, .14f, .65f), false);
            foreach (float side in new[] { -1f, 1f })
            {
                Box(cabinet, "Secure museum glass side", glass,
                    new Vector3(side * 1.36f, 1.64f, 0), new Vector3(.06f, 1.65f, 1.38f));
                Box(cabinet, "Secure museum glass front/back", glass,
                    new Vector3(0, 1.64f, side * .70f), new Vector3(2.69f, 1.65f, .06f));
            }
            Box(cabinet, "Museum glass top", glass,
                new Vector3(0, 2.50f, 0), new Vector3(2.75f, .06f, 1.43f));
            if (!source) continue; // original owner-supplied FBX only, never fabricate a mesh
            var beltMaterial = ChampionshipBeltPbr();
            if (!beltMaterial) continue;
            var belt = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (!belt) continue;
            belt.name = "Owner supplied championship belt 3D - original";
            belt.transform.SetParent(cabinet, false);
            var renderers = belt.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Champion belt FBX contains no visible renderer");
            var bb = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bb.Encapsulate(renderer.bounds);
            float longest = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
            if (longest < 1e-6f) throw new InvalidOperationException("Champion belt FBX has invalid bounds");
            belt.transform.localScale *= (1.55f * K / longest);
            bb = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bb.Encapsulate(renderer.bounds);
            belt.transform.position += cabinet.TransformPoint(new Vector3(0, 1.64f, 0) * K) - bb.center;
            foreach (var renderer in renderers)
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = beltMaterial;
                renderer.sharedMaterials = mats;
            }
            found = true;
        }
        if (!found) Debug.LogWarning("CHAMPION_BELT_ASSET_MISSING: place the owner's original FBX and maps at " + BeltAsset + " before claiming trophy-ready APK");
        return found;
    }

    public static bool Generate(Transform arenaRoot, Dictionary<string, Material> exterior)
    {
        var root = Group(arenaRoot, "A1 | arena entrance, parking, trophy and athlete facilities", Vector3.zero);
        var asphalt = BraxChampionsFoundation.SourceMaterial("arena_parking_asphalt", "clean_asphalt", 2.2f,
            new Color(.89f, .89f, .89f));
        var sidewalk = BraxChampionsFoundation.SourceMaterial("arena_arrival_paving", "concrete_pavement_02", 1.1f,
            new Color(.94f, .94f, .93f));
        var rope = BraxChampionsFoundation.SourceMaterial("arena_red_velvet", "fabric_pattern_05", .6f,
            new Color(.62f, .025f, .045f));
        var paint = Paint("arena_parking_markings", new Color(.93f, .92f, .85f));
        Parking(root, asphalt, paint, sidewalk, exterior["dark_stone"]);
        Stanchions(root, exterior["bronze"], rope, rope);
        Door(root, "CHAMPIONS_A1_PUBLIC", "Main public entrance", -59.04f, exterior["clear_glass"], exterior["steel"]);
        Door(root, "CHAMPIONS_A1_ATHLETES", "Separate athlete rear entrance", 59.04f, exterior["clear_glass"], exterior["steel"]);
        DressingRooms(root, sidewalk, exterior["dark_stone"], exterior["steel"]);
        Box(root, "Main lobby continuous carpet", rope,
            new Vector3(0, .239f, -47.3f), new Vector3(3.9f, .018f, 22.0f), false);
        Box(root, "Separate red athlete walkway to cage", rope,
            new Vector3(0, .239f, 47.2f), new Vector3(3.9f, .018f, 22.0f), false);
        // Warm lobby lighting keeps the interior visible through the glazed curtain wall.
        // Vertex lights are used, not shadow-casting dynamic lights per glass bay.
        for (int i = -1; i <= 1; i++)
        {
            var lamp = new GameObject("Warm public lobby light " + i).AddComponent<Light>();
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(i * 7f, 4.8f, -49f) * K;
            lamp.type = LightType.Point;
            lamp.range = 17f * K;
            lamp.intensity = 1.35f;
            lamp.color = new Color(1f, .89f, .72f);
            lamp.shadows = LightShadows.None;
            lamp.renderMode = LightRenderMode.ForceVertex;
        }
        return Trophy(root, exterior["clear_glass"], exterior["bronze"], exterior["stone"]);
    }
}

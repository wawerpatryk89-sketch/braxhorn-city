using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// A1 local coordinates are nominal metres, relative to the approved (600,800) plot.
// Every primitive is multiplied by BraxClaudeOsiedle.K exactly once.
// This does not modify player models, the estate arena, metro, or neighbouring plots.
public static partial class BraxChampionsArenaArrival
{
    const string Out = "Assets/PlayableGenerated/Mistrzowie/Buildings/Materials/";
    const string BeltRoot = "Assets/Source/Mistrzowie/Trophies/";
    const string BeltName = "tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419";
    const string BeltAsset = BeltRoot + BeltName + ".fbx";
    const string BeltMaps = BeltRoot + BeltName + ".fbm/";
    static float K => BraxClaudeOsiedle.K;
    static int tiledMeshSerial;

    static void MetreScaleUv(GameObject target, Vector3 nominalSize)
    {
        // Unity primitive cube UV are 0..1 on each face, which would stretch a
        // 4K asphalt map over an entire 70m car park. Tile by REAL surface metres.
        // The Poly Haven material's SourceMaterial repeat is applied on top.
        if (Mathf.Max(nominalSize.x, nominalSize.z) < 6f) return;
        var filter = target.GetComponent<MeshFilter>();
        var original = filter.sharedMesh;
        var tiled = UnityEngine.Object.Instantiate(original);
        var vertices = tiled.vertices;
        var normals = tiled.normals;
        var uv = tiled.uv;
        for (int i = 0; i < uv.Length; i++)
        {
            var p = vertices[i];
            var n = normals[i];
            if (Mathf.Abs(n.y) > .7f)
                uv[i] = new Vector2((p.x + .5f) * nominalSize.x, (p.z + .5f) * nominalSize.z);
            else if (Mathf.Abs(n.x) > .7f)
                uv[i] = new Vector2((p.z + .5f) * nominalSize.z, (p.y + .5f) * nominalSize.y);
            else
                uv[i] = new Vector2((p.x + .5f) * nominalSize.x, (p.y + .5f) * nominalSize.y);
        }
        tiled.uv = uv;
        tiled.name = "A1 tiled physical-scale surface " + tiledMeshSerial;
        string path = "Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_Tiled_"
            + (tiledMeshSerial++) + ".asset";
        AssetDatabase.CreateAsset(tiled, path);
        filter.sharedMesh = tiled;
    }

    static GameObject Box(Transform parent, string name, Material material, Vector3 centre, Vector3 size, bool solid = true)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
        o.name = name;
        o.transform.SetParent(parent, false);
        o.transform.localPosition = centre * K;
        o.transform.localScale = size * K;
        o.GetComponent<Renderer>().sharedMaterial = material;
        MetreScaleUv(o, size);
        if (!solid) UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        return o;
    }

    static GameObject GradedSurface(Transform parent,string name,Material material,
        float x0,float x1,float z0,float z1,float y0,float y1,bool solid=true)
    {
        if(x1<=x0||z1<=z0)throw new ArgumentException("Invalid graded A1 surface "+name);
        var mesh=new Mesh{name=name+" | exact edge-to-edge graded mesh"};
        mesh.vertices=new[]{
            new Vector3(x0,y0,z0)*K,
            new Vector3(x0,y1,z1)*K,
            new Vector3(x1,y1,z1)*K,
            new Vector3(x1,y0,z0)*K
        };
        mesh.uv=new[]{
            new Vector2(x0,z0),new Vector2(x0,z1),
            new Vector2(x1,z1),new Vector2(x1,z0)
        };
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string asset="Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_Graded_"
            +(tiledMeshSerial++)+".asset";
        AssetDatabase.CreateAsset(mesh,asset);
        var o=new GameObject(name);
        o.transform.SetParent(parent,false);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        o.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(solid)o.AddComponent<MeshCollider>().sharedMesh=mesh;
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
        // Smooth, naturally sagging velvet rope; never render it as a rigid bar.
        const int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
            Vector3 P(float t) => Vector3.Lerp(a, b, t) - Vector3.up * (.30f * 4f * t * (1f - t));
            var p = P(t0) * K; var q = P(t1) * K;
            var d = q - p;
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "RED velvet VIP rope - naturally hanging segment";
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
        // Owner-approved entrance: deep RED carpet, GOLD posts and naturally sagging RED ropes.
        Box(parent, "Public RED velvet carpet | VIP arrival", carpet,
            new Vector3(0, .239f, -67.0f), new Vector3(5.8f, .018f, 15.4f), false);
        // Flush gold piping along the red carpet; avoid obstructing the walkway.
        foreach (float edge in new[] { -2.95f, 2.95f })
            Box(parent, "Premium carpet edge | polished gold", metal,
                new Vector3(edge, .251f, -67f),
                new Vector3(.065f, .012f, 15.4f), false);
        foreach (float x in new[] { -3.5f, 3.5f })
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i < 5; i++)
            {
                float z = -73f + i * 2.65f;
                // Real round stanchion base, not a square placeholder.
                var baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                baseDisc.name = "Premium GOLD stanchion | round weighted base";
                baseDisc.transform.SetParent(parent, false);
                baseDisc.transform.localPosition = new Vector3(x, .285f, z) * K;
                baseDisc.transform.localScale = new Vector3(.19f, .055f, .19f) * K;
                baseDisc.GetComponent<Renderer>().sharedMaterial = metal;
                UnityEngine.Object.DestroyImmediate(baseDisc.GetComponent<Collider>());
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Premium GOLD VIP stanchion | upright";
                post.transform.SetParent(parent, false);
                post.transform.localPosition = new Vector3(x, .80f, z) * K;
                post.transform.localScale = new Vector3(.052f, .46f, .052f) * K;
                post.GetComponent<Renderer>().sharedMaterial = metal;
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                Box(parent, "Premium GOLD VIP stanchion | polished cap", metal,
                    new Vector3(x, 1.27f, z), new Vector3(.16f, .10f, .16f), false);
                Vector3 top = new Vector3(x, 1.18f, z);
                if (i > 0) Cord(parent, rope, prev, top);
                prev = top;
            }
        }
    }

    public const float MouthR=3.0f;
    const string MouthMeshes="Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes";
    // the city's own sidewalk and kerb materials for the street mouths (the arena's paving only when the foundation
    // has not been generated)
    static Material CityWalk(Material fallback)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/Mistrzowie/Materials/paving.mat");
        return m?m:fallback;
    }
    static Material CityKerb()=>BraxChampionsFoundation.SourceMaterial("infrastructure_kerb","granite_tile",.9f,new Color(.78f,.79f,.80f));

    static void Parking(Transform parent, Material asphalt, Material paint, Material curb, Material dark, Material metal)
    {
        // Two independent courts, west/east of the preserved central walking axis.
        // City road Z650 ends at local z=-140; its north sidewalk occupies
        // z=-140..-133. The foundation now has two exact 6.4m cuts there.
        // Each cut is replaced by a gentle asphalt ramp: road y=0 -> parking y=.238.
        foreach (float x in new[] { -65f, 65f })
        {
            const float parkingTop=.238f;
            Box(parent, "Car park - asphalt, real ground collider", asphalt,
                new Vector3(x, .199f, -97f), new Vector3(70f, .078f, 24f));
            Box(parent, "Parking vehicle access lane", asphalt,
                new Vector3(x, .199f, -121f), new Vector3(6.4f, .078f, 24f));
            // owner 10.11 17:38-17:40: a real street mouth across the Z650 sidewalk - city kerb open, R 3 m kerb arcs,
            // road -> sidewalk over R, then up to the car-park lane (layout.A1_DRIVE_MOUTHS / A1_MOUTH_CORNERS)
            BraxChampionsDriveMouth.Build(parent,new BraxChampionsDriveMouth.Spec{id="A1 car park "+(x<0?"west":"east")+" street mouth",
                alongX=true,edge=-140f,dir=1f,u0=x-3.2f,u1=x+3.2f,R=MouthR,depth=7f,topEnd=parkingTop,unit=K,assetDir=MouthMeshes},
                asphalt,CityWalk(curb),CityKerb());
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
                        new Vector3(stallX, .242f, (a + b) / 2f),
                        new Vector3(.09f, .008f, b - a), false);
                    if (i < 10)
                        Box(parent, "Parking bay wheel stop", curb,
                            new Vector3(stallX + 1.48f, .29f, row == 0 ? a + .65f : b - .65f),
                            new Vector3(1.28f, .15f, .19f));
                }
                Box(parent, "Parking stall head line", paint,
                    new Vector3(x, .242f, row == 0 ? a : b), new Vector3(63f, .008f, .08f), false);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Box(parent, "Parking outer curb", curb,
                    new Vector3(x + side * 35.125f, .31f, -97f), new Vector3(.25f, .22f, 24f));
            }
            // North edge has an 8m pedestrian opening; south edge has only
            // the exact 6.4m vehicle opening. Curbs touch the asphalt edge,
            // never overlap the driving surface.
            foreach(float side in new[]{-1f,1f})
            {
                Box(parent,"Parking north end concrete curb",curb,
                    new Vector3(x+side*19.5f,.31f,-84.875f),
                    new Vector3(31f,.22f,.25f));
                Box(parent,"Parking south end concrete curb",curb,
                    new Vector3(x+side*19.1f,.31f,-109.125f),
                    new Vector3(31.8f,.22f,.25f));
            }
            Box(parent,"Parking entrance flush trench drain",metal,
                new Vector3(x,.244f,-109.02f),new Vector3(5.9f,.008f,.12f),false);        // 8 mm grate on the asphalt
            // Accessible wider spaces nearest the pedestrian route, with an unblocked transfer strip.
            float accessibleX = x < 0 ? x + 30.2f : x - 30.2f;
            Box(parent, "Wide accessible bay dividing line", paint,
                new Vector3(accessibleX, .242f, -87.9f), new Vector3(.11f, .008f, 5.2f), false);
            // Real curb ramp from asphalt to the pedestrian level, then a flat
            // path. This replaces the old abrupt raised slab at the accessible exit.
            GradedSurface(parent,"Accessible parking curb ramp",curb,
                x-4f,x+4f,-85f,-83.4f,parkingTop,.20f,true);
            Box(parent, "Pedestrian path out of parking", curb,
                new Vector3(x, .18f, -79.2f), new Vector3(8f, .04f, 8.4f));
            // Small solid raised islands guard pedestrians without obstructing the driveway.
            Box(parent, "Parking planting island / protective curb", dark,
                new Vector3(x - 25f, .27f, -111.4f), new Vector3(4f, .15f, 1.0f));
        }
        // Wheelchair-friendly pedestrian connections from both side car parks
        // to the central public promenade, wholly behind the existing z=650 road.
        // The 5m cross-link is flat, unblocked and separate from vehicle aisles.
        // 10.11: the walk to the doors is 10 m (x +-5), the connectors meet its edge.
        foreach (float sx in new[] { -33f, 33f })
            Box(parent, "Accessible parking-to-arena paved connector", curb,
                new Vector3(sx, .18f, -79f), new Vector3(56f, .04f, 5f));
        // Main spectator route stays permanently clear between parking and entrance.
        // Connectors meet x=+-5 and the parking exit paths at x=+-61 exactly:
        // no coplanar overlap rectangles.
        // Owner render: polished dark stone in front of the arena, not grey sidewalk pavers.
        var promenade=BraxChampionsFoundation.SourceMaterial("arena_promenade_polished_dark_granite","granite_tile_02",1.2f,new Color(.34f,.34f,.36f));
        promenade.SetFloat("_Roughness",.22f);
        // Owner 10.11 08:36-08:38: the main entrance for people, 10 m between lawns (16 m with lines read as a road).
        Box(parent, "Arena public promenade", promenade,
            new Vector3(0, .18f, -104f), new Vector3(10f, .04f, 58f));
        for (float x = -4.75f; x <= 4.75f; x += 9.5f)
            Box(parent, "Public path warm LED inlay line", arenaLed ? arenaLed : paint,      // owner render: lit lines lead to the door
                new Vector3(x, .204f, -105.5f), new Vector3(.10f, .008f, 54f), false);
    }

    static void ParkingLights(Transform parent, Material frame, Material led)
    {
        // Four architectural lamps on the OUTER edges of the courts.
        // No pole crosses an actual stall, lane, spectator path or rope barrier.
        foreach (float x in new[] { -104f, 104f })
        foreach (float z in new[] { -109f, -86f })
        {
            Box(parent, "Premium parking steel lamp post", frame,
                new Vector3(x, 3.18f, z), new Vector3(.19f, 6.0f, .19f));
            Box(parent, "Premium parking LED luminaire", led,
                new Vector3(x, 6.22f, z), new Vector3(1.35f, .10f, .55f), false);
            var lamp=new GameObject("Championship parking - warm mobile light").AddComponent<Light>();
            lamp.transform.SetParent(parent,false);
            lamp.transform.localPosition=new Vector3(x, 6.0f, z)*K;
            lamp.type=LightType.Point;
            lamp.intensity=.95f;
            lamp.range=23f*K;
            lamp.color=new Color(1f,.91f,.78f);
            lamp.shadows=LightShadows.None;
            lamp.renderMode=LightRenderMode.ForceVertex;
        }
    }

    // Owner 10.11 12:28 (phone, APK 37, lobby looking at the entrance): "tylko i wyłącznie drzwi mają być jasne ze
    // szkła i mają być dobre, bo są zjebane, a naokoło ta szyba ma być czarna". The 9.74 m opening was clear glass
    // everywhere with hair-thin frames, the doors did not read as doors. Now: black glass sidelights and transom
    // (the arena's black facade glass), a black portal frame with a gold inner edge round the door opening, and two
    // frameless clear toughened-glass leaves with gold top / bottom patch rails and full-height gold pull bars on
    // both faces. Only the leaves are clear.
    static void Door(Transform parent, string id, string label, float z,
        Material glass, Material metal, Material blackGlass, Material frame, Material gold)
    {
        var root = Group(parent, label + " | functional glass double door", new Vector3(0, .24f, z));
        var hinges = new Transform[2];
        const float leafW = 1.60f, leafH = 3.22f, leafT = .03f, hingeX = 1.72f;
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            var h = Group(root, "Real opening glass leaf " + (i + 1), new Vector3(side * hingeX, 0, 0));
            hinges[i] = h;
            float cx = -side * leafW * .5f;
            // (10.11 zero flicker) the glass ends INSIDE both patch rails: no glass face in the plane of a rail face
            Box(h, "Toughened clear glass leaf (frameless)", glass, new Vector3(cx, (leafH + .03f) * .5f, 0), new Vector3(leafW - .012f, leafH - .03f, leafT));
            foreach (float y in new[] { .025f + .073f, leafH + .009f - .073f })
                Box(h, "Gold patch rail", gold, new Vector3(cx, y, 0), new Vector3(leafW, .146f, leafT + .04f), false);
            // full-height pull bar on both faces near the meeting edge, two standoffs each
            float px = -side * (leafW - .16f);
            foreach (float f in new[] { -1f, 1f })
            {
                Box(h, "Gold pull bar", gold, new Vector3(px, 1.45f, f * .085f), new Vector3(.045f, 1.80f, .045f), false);
                foreach (float y in new[] { .70f, 2.20f })
                    Box(h, "Pull bar standoff", gold, new Vector3(px, y, f * .05f), new Vector3(.025f, .025f, .07f), false);
            }
        }
        // black glass to both sides of the doors and above them (the facade around the doors is black)
        foreach (float side in new[] { -1f, 1f })
            Box(root, "Black glass sidelight", blackGlass, new Vector3(side * 3.38f, 1.70f, 0), new Vector3(2.98f, 3.40f, .06f));
        Box(root, "Black glass transom over the doors", blackGlass, new Vector3(0, 3.95f, 0), new Vector3(9.74f, 1.10f, .06f));
        // black portal frame round the door opening with a gold inner edge (door clear 3.44 x 3.30)
        foreach (float side in new[] { -1f, 1f })
        {
            Box(root, "Door portal jamb (black)", frame, new Vector3(side * (hingeX + .09f), 1.70f, 0), new Vector3(.18f, 3.40f, .22f));
            // gold edge 8 mm proud of the jamb face and 15 mm past both jamb faces (was 1 mm / 5 mm: it flickered)
            Box(root, "Door portal gold inner edge", gold, new Vector3(side * (hingeX - .002f), 1.66f, 0), new Vector3(.012f, 3.32f, .25f), false);
            Box(root, "Sidelight outer frame (black)", frame, new Vector3(side * 4.85f, 1.70f, 0), new Vector3(.10f, 3.40f, .14f), false);
        }
        Box(root, "Door portal head (black)", frame, new Vector3(0, 3.40f, 0), new Vector3(9.74f, .18f, .22f));
        Box(root, "Door portal gold head edge", gold, new Vector3(0, 3.302f, 0), new Vector3(2f * hingeX, .012f, .25f), false);
        Box(root, "Black threshold", frame, new Vector3(0, .01f, 0), new Vector3(9.74f, .02f, .30f), false);
        var door = root.gameObject.AddComponent<BraxCityDoor>();
        door.id = id; door.label = label; door.leaves = hinges; door.sides = new[] { 1f, -1f };
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


    // Premium A1 / Issue #27: approved execution geometry for the external A1 facade.
    // Nominal metres are multiplied by K exactly once. Owner sign PNGs are not modified.
    // The mid-belt, columns and public canopy are true geometry; no flat gold/black decals.
    sealed class FacadeBatch
    {
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();

        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
        {
            int n=positions.Count;
            positions.Add(a*K); positions.Add(b*K);
            positions.Add(c*K); positions.Add(d*K);
            uvs.Add(Vector2.zero); uvs.Add(new Vector2(u,0));
            uvs.Add(new Vector2(u,v)); uvs.Add(new Vector2(0,v));
            triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2);
            triangles.Add(n); triangles.Add(n+2); triangles.Add(n+3);
        }

        public void Beam(Vector3 center, Vector3 size, Quaternion rotation)
        {
            if(size.x<=0 || size.y<=0 || size.z<=0)throw new ArgumentException("Invalid premium facade beam");
            var h=size*.5f;
            Vector3 P(float x,float y,float z)=>center+rotation*new Vector3(x,y,z);
            var a=P(-h.x,-h.y,-h.z);var b=P(h.x,-h.y,-h.z);
            var c=P(h.x,h.y,-h.z);var d=P(-h.x,h.y,-h.z);
            var e=P(-h.x,-h.y,h.z);var f=P(h.x,-h.y,h.z);
            var g=P(h.x,h.y,h.z);var q=P(-h.x,h.y,h.z);
            Face(e,f,g,q,size.x,size.y);Face(b,a,d,c,size.x,size.y);
            Face(f,b,c,g,size.z,size.y);Face(a,e,q,d,size.z,size.y);
            Face(q,g,c,d,size.x,size.z);Face(a,b,f,e,size.x,size.z);
        }

        // Rounded rectangular prism used for the 38 structural facade columns,
        // canopy cassettes and rounded gold edge pieces. Vertical corner radius
        // and the top/bottom edge radius are modelled as geometry.
        public void RoundedPrism(Vector3 center,Vector3 size,Quaternion rotation,
            float cornerRadius,float capRadius,int cornerSegments=3,int capSegments=2)
        {
            if(size.x<=0 || size.y<=0 || size.z<=0)throw new ArgumentException("Invalid rounded facade prism");
            float hx=size.x*.5f,hy=size.y*.5f,hz=size.z*.5f;
            float cap=Mathf.Clamp(capRadius,0,
                Mathf.Max(0,Mathf.Min(hy-.0002f,Mathf.Min(hx,hz)-.0002f)));
            int cs=Mathf.Max(1,cornerSegments),caps=Mathf.Max(1,capSegments);

            List<Vector2> Perimeter(float shrink)
            {
                float sx=hx-shrink,sz=hz-shrink;
                float maxR=Mathf.Max(.0002f,Mathf.Min(sx,sz)*.95f);
                float rr=Mathf.Clamp(cornerRadius-shrink*.35f,.0002f,maxR);
                var pts=new List<Vector2>(4*(cs+1));
                void Arc(float cx,float cz,float deg0,float deg1)
                {
                    for(int j=0;j<=cs;j++)
                    {
                        float deg=Mathf.Lerp(deg0,deg1,j/(float)cs)*Mathf.Deg2Rad;
                        pts.Add(new Vector2(cx+rr*Mathf.Cos(deg),cz+rr*Mathf.Sin(deg)));
                    }
                }
                // Counter-clockwise perimeter in the local X/Z plane.
                Arc(sx-rr,-sz+rr,-90f,0f);
                Arc(sx-rr, sz-rr,0f,90f);
                Arc(-sx+rr,sz-rr,90f,180f);
                Arc(-sx+rr,-sz+rr,180f,270f);
                return pts;
            }

            var rings=new List<Vector2>(); // x = local Y, y = horizontal inset
            if(cap>.0001f)
            {
                for(int j=0;j<=caps;j++)
                {
                    float theta=.5f*Mathf.PI*j/caps;
                    rings.Add(new Vector2(-hy+cap*Mathf.Sin(theta),cap*Mathf.Cos(theta)));
                }
                float upperFull=hy-cap;
                if(upperFull>rings[rings.Count-1].x+.0001f)rings.Add(new Vector2(upperFull,0));
                for(int j=caps-1;j>=0;j--)
                {
                    float theta=.5f*Mathf.PI*j/caps;
                    rings.Add(new Vector2(hy-cap*Mathf.Sin(theta),cap*Mathf.Cos(theta)));
                }
            }
            else
            {
                rings.Add(new Vector2(-hy,0));rings.Add(new Vector2(hy,0));
            }

            var perimeters=new List<List<Vector2>>(rings.Count);
            foreach(var ring in rings)perimeters.Add(Perimeter(ring.y));
            int count=perimeters[0].Count;
            void Add(Vector3 local)
            {
                positions.Add((center+rotation*local)*K);
                uvs.Add(Vector2.zero);
            }
            for(int r=0;r<rings.Count-1;r++)
            {
                var lower=perimeters[r];var upper=perimeters[r+1];
                for(int i=0;i<count;i++)
                {
                    int n=positions.Count,next=(i+1)%count;
                    Add(new Vector3(lower[i].x,rings[r].x,lower[i].y));
                    Add(new Vector3(lower[next].x,rings[r].x,lower[next].y));
                    Add(new Vector3(upper[next].x,rings[r+1].x,upper[next].y));
                    Add(new Vector3(upper[i].x,rings[r+1].x,upper[i].y));
                    // Reversed relative to the CCW perimeter -> outward normals.
                    triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
                    triangles.Add(n);triangles.Add(n+3);triangles.Add(n+2);
                }
            }

            // Flat end faces sit inside the rounded edge rings.
            var bottom=perimeters[0];float yBottom=rings[0].x;
            for(int i=0;i<count;i++)
            {
                int n=positions.Count,next=(i+1)%count;
                Add(new Vector3(0,yBottom,0));
                Add(new Vector3(bottom[i].x,yBottom,bottom[i].y));
                Add(new Vector3(bottom[next].x,yBottom,bottom[next].y));
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            }
            var top=perimeters[perimeters.Count-1];float yTop=rings[rings.Count-1].x;
            for(int i=0;i<count;i++)
            {
                int n=positions.Count,next=(i+1)%count;
                Add(new Vector3(0,yTop,0));
                Add(new Vector3(top[next].x,yTop,top[next].y));
                Add(new Vector3(top[i].x,yTop,top[i].y));
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            }
        }

        // 30 mm deep vertical gold plaque with a true 120 mm capsule face.
        // The 60 mm end radius is exact in the visible Y/Z face.
        public void CapsulePlaque(Vector3 center,float totalDepth,float height,float width,
            float edgeRadius,Quaternion rotation,int arcSegments=8,int bevelSegments=4)
        {
            if(totalDepth<=0||height<=width||width<=0||edgeRadius<=0)
                throw new ArgumentException("Invalid capsule plaque");
            int arcs=Mathf.Max(4,arcSegments),bevels=Mathf.Max(2,bevelSegments);
            float maxInset=width*.5f-.0005f;
            float bevel=Mathf.Min(edgeRadius,Mathf.Min(totalDepth*.5f-.0002f,maxInset));
            if(Mathf.Abs(edgeRadius-.018f)<.0001f&&Mathf.Abs(bevel-.018f)>.0005f)
                throw new InvalidOperationException("Issue27 capsule R18 bevel cannot be represented by supplied depth");

            List<Vector2> Profile(float inset)
            {
                float radius=width*.5f-inset;
                float straight=height*.5f-width*.5f;
                var p=new List<Vector2>();
                for(int j=0;j<=arcs;j++)
                {
                    float a=Mathf.PI*j/arcs;
                    p.Add(new Vector2(radius*Mathf.Cos(a),straight+radius*Mathf.Sin(a)));
                }
                for(int j=0;j<=arcs;j++)
                {
                    float a=Mathf.PI+Mathf.PI*j/arcs;
                    p.Add(new Vector2(radius*Mathf.Cos(a),-straight+radius*Mathf.Sin(a)));
                }
                return p;
            }

            float front=totalDepth*.5f,back=-front;
            var rings=new List<(float x,float inset)>();
            // Convex profile: the embedded/back section is narrower by R18,
            // then the fillet opens OUTWARD to the exact 120mm front face.
            rings.Add((back,bevel));
            if(front-back>bevel+.0002f)rings.Add((front-bevel,bevel));
            for(int j=1;j<=bevels;j++)
            {
                float theta=.5f*Mathf.PI*j/bevels;
                rings.Add((front-bevel+bevel*Mathf.Sin(theta),
                    bevel*Mathf.Cos(theta)));
            }

            void Add(Vector3 local)
            {
                positions.Add((center+rotation*local)*K);
                uvs.Add(Vector2.zero);
            }

            for(int r=0;r<rings.Count-1;r++)
            {
                var a=Profile(rings[r].inset);
                var b=Profile(rings[r+1].inset);
                int pc=a.Count;
                for(int i=0;i<pc;i++)
                {
                    int next=(i+1)%pc,n=positions.Count;
                    Add(new Vector3(rings[r].x,a[i].y,a[i].x));
                    Add(new Vector3(rings[r].x,a[next].y,a[next].x));
                    Add(new Vector3(rings[r+1].x,b[next].y,b[next].x));
                    Add(new Vector3(rings[r+1].x,b[i].y,b[i].x));
                    triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
                    triangles.Add(n);triangles.Add(n+3);triangles.Add(n+2);
                }
            }

            // Front cap is the exact approved 120mm visible face with R60 ends.
            // Back cap is inset by R18 and disappears into the black column.
            foreach(var cap in new[]{(x:back,inset:bevel,frontFace:false),
                                     (x:front,inset:0f,frontFace:true)})
            {
                var p=Profile(cap.inset);int pc=p.Count;
                for(int i=0;i<pc;i++)
                {
                    int next=(i+1)%pc,n=positions.Count;
                    Add(new Vector3(cap.x,0,0));
                    if(cap.frontFace)
                    {
                        Add(new Vector3(cap.x,p[next].y,p[next].x));
                        Add(new Vector3(cap.x,p[i].y,p[i].x));
                    }
                    else
                    {
                        Add(new Vector3(cap.x,p[i].y,p[i].x));
                        Add(new Vector3(cap.x,p[next].y,p[next].x));
                    }
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
                }
            }
        }

        public void Emit(Transform parent,string suffix,Material material)
        {
            if(positions.Count==0)return;
            string asset="Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_Premium_"
                +suffix+".asset";
            var mesh=new Mesh{name="A1 premium "+suffix,
                indexFormat=positions.Count>65535 ?
                    UnityEngine.Rendering.IndexFormat.UInt32 :
                    UnityEngine.Rendering.IndexFormat.UInt16};
            mesh.SetVertices(positions);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,asset);
            var o=new GameObject("A1 PREMIUM facade | "+suffix);
            o.transform.SetParent(parent,false);
            o.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=o.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=true;
        }
    }

    static GameObject RoundedCylinder(Transform parent,string name,Material material,
        Vector3 center,float radius,float height,float edgeRadius,bool solid)
    {
        const int radialSegments=32;
        const int arcSegments=4;
        float half=height*.5f;
        float edge=Mathf.Clamp(edgeRadius,0,Mathf.Min(radius*.48f,half*.48f));
        var profile=new List<Vector2>(); // x radius, y local height
        if(edge>.0001f)
        {
            profile.Add(new Vector2(radius-edge,-half));
            for(int j=1;j<=arcSegments;j++)
            {
                float a=-.5f*Mathf.PI+.5f*Mathf.PI*j/arcSegments;
                profile.Add(new Vector2(radius-edge+edge*Mathf.Cos(a),
                    -half+edge+edge*Mathf.Sin(a)));
            }
            if(half-edge> -half+edge+.0001f)
                profile.Add(new Vector2(radius,half-edge));
            for(int j=1;j<=arcSegments;j++)
            {
                float a=.5f*Mathf.PI*j/arcSegments;
                profile.Add(new Vector2(radius-edge+edge*Mathf.Cos(a),
                    half-edge+edge*Mathf.Sin(a)));
            }
        }
        else
        {
            profile.Add(new Vector2(radius,-half));
            profile.Add(new Vector2(radius,half));
        }

        var verts=new List<Vector3>();
        var tris=new List<int>();
        for(int p=0;p<profile.Count;p++)
        for(int i=0;i<radialSegments;i++)
        {
            float a=2f*Mathf.PI*i/radialSegments;
            verts.Add((center+new Vector3(profile[p].x*Mathf.Cos(a),
                profile[p].y,profile[p].x*Mathf.Sin(a)))*K);
        }
        for(int p=0;p<profile.Count-1;p++)
        for(int i=0;i<radialSegments;i++)
        {
            int n=(i+1)%radialSegments;
            int a=p*radialSegments+i,b=p*radialSegments+n;
            int c=(p+1)*radialSegments+n,d=(p+1)*radialSegments+i;
            tris.Add(a);tris.Add(c);tris.Add(b);
            tris.Add(a);tris.Add(d);tris.Add(c);
        }
        int bottomCenter=verts.Count;verts.Add((center+Vector3.down*half)*K);
        int topCenter=verts.Count;verts.Add((center+Vector3.up*half)*K);
        int topBase=(profile.Count-1)*radialSegments;
        for(int i=0;i<radialSegments;i++)
        {
            int n=(i+1)%radialSegments;
            tris.Add(bottomCenter);tris.Add(i);tris.Add(n);
            tris.Add(topCenter);tris.Add(topBase+n);tris.Add(topBase+i);
        }
        var mesh=new Mesh{name=name+" rounded mesh",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string asset="Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_Premium_Round_"
            +(tiledMeshSerial++)+".asset";
        AssetDatabase.CreateAsset(mesh,asset);
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(solid)
        {
            // Fixed architectural support: use the exact authored rounded mesh.
            // The generated mesh already contains nominal dimensions times K;
            // no primitive approximation, extra scaling or dynamic Rigidbody.
            var cc=go.AddComponent<MeshCollider>();
            cc.sharedMesh=mesh;
        }
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static float ActualFloorYNominal(Transform arenaRoot,float x,float z)
    {
        // During prefab generation BraxChampionsBuildings intentionally moves
        // the reusable A1 root to world zero. The A1 structural slab/colliders
        // move with that root, so grounding must query THIS A1 geometry before
        // the prefab is restored to plot (600,800). Querying the separate world
        // foundation here hit its underside at y=-0.25 instead of the arena
        // arrival slab (nominal top ~0.16m).
        Physics.SyncTransforms();
        var start=arenaRoot.TransformPoint(new Vector3(x,20f,z)*K);
        if(!Physics.Raycast(start,Vector3.down,out var hit,50f*K,~0,QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("Issue27 canopy support floor raycast missed at A1 local x="+
                x+" z="+z);
        float localScaled=arenaRoot.InverseTransformPoint(hit.point).y;
        return localScaled/K;
    }

    static void PremiumExterior(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        const float rx=96f,rz=59f;
        const int bays=360;
        var group=Group(arenaRoot,"A1 PREMIUM | ISSUE 27 approved external rebuild",Vector3.zero);
        var black=exterior["arena_black"];
        var gold=exterior["arena_gold"];

        var plinth=new FacadeBatch();
        var fins=new FacadeBatch();
        var blackColumns=new FacadeBatch();
        var goldColumnInserts=new FacadeBatch();
        var midBelt=new FacadeBatch();
        var beltGold=new FacadeBatch();
        var roof=new FacadeBatch();
        var crownLight=new FacadeBatch();
        var columnUplights=new FacadeBatch();
        var columnGold=new FacadeBatch();
        var upperCeilingHalo=new FacadeBatch();

        int actualFeatureColumns=0,skippedFeatureColumns=0,skippedDoorSegments=0;
        for(int i=0;i<bays;i++)
        {
            float a=2f*Mathf.PI*i/bays;
            float b=2f*Mathf.PI*(i+1)/bays;
            Vector3 p=new Vector3(rx*Mathf.Cos(a),0,rz*Mathf.Sin(a));
            Vector3 next=new Vector3(rx*Mathf.Cos(b),0,rz*Mathf.Sin(b));
            var mid=(p+next)*.5f;
            var outward=new Vector3(mid.x/(rx*rx),0,mid.z/(rz*rz)).normalized;
            var tangent=(next-p).normalized;
            var rotation=Quaternion.LookRotation(tangent,Vector3.up);
            float length=Vector3.Distance(p,next)+.035f;
            bool entrance=Mathf.Abs(mid.x)<8.4f&&Mathf.Abs(mid.z)>57f;
            bool featurePillar=i%9==0;

            if(!entrance)
            {
                plinth.Beam(mid+outward*.23f+Vector3.up*.50f,
                    new Vector3(.38f,.78f,length),rotation);
                if(featurePillar)
                {
                    actualFeatureColumns++;
                    // Owner render 9.11: black columns run UNBROKEN from the
                    // plinth to the roof, in front of the belt (2.20 x 0.85,
                    // y 1.03..26.55), one gold LED capsule the full height.
                    blackColumns.RoundedPrism(
                        mid+outward*.425f+Vector3.up*13.79f,
                        new Vector3(.85f,25.52f,2.20f),rotation,.10f,.08f,4,3);
                    // owner 10.11 07:51 "te pionowe tak samo": a WIDE rounded gold strip on every column, a warm
                    // LED line glowing in its middle (owner render)
                    columnGold.CapsulePlaque(
                        mid+outward*.877f+Vector3.up*13.79f,
                        .070f,25.40f,.440f,.030f,rotation,8,4);
                    goldColumnInserts.CapsulePlaque(
                        mid+outward*.921f+Vector3.up*13.79f,
                        .030f,25.20f,.120f,.012f,rotation,8,3);
                    // owner render: a warm in-ground uplight at the foot of every lit column
                    columnUplights.Beam(mid+outward*1.45f+Vector3.up*.19f,new Vector3(.34f,.05f,.34f),rotation);
                }
                else if(i%2==0)
                {
                    // Render: dense black mullions (~2.8 m), not pale steel.
                    fins.Beam(mid+outward*.19f+Vector3.up*((.18f+BeltBottom)*.5f),
                        new Vector3(.12f,BeltBottom-.18f,.17f),rotation);
                    fins.Beam(mid+outward*.19f+Vector3.up*((BeltTop+26.55f)*.5f),
                        new Vector3(.12f,26.55f-BeltTop,.17f),rotation);
                }
            }
            else
            {
                skippedDoorSegments++;
                if(featurePillar)skippedFeatureColumns++;
            }

            // Continuous black belt between the floors, render proportion:
            // outer face +0.23 m, y BeltBottom..BeltTop (3.8 m, was 7.7 m).
            midBelt.Beam(mid+outward*.12f+Vector3.up*((BeltBottom+BeltTop)*.5f),
                new Vector3(.22f,BeltTop-BeltBottom,length),rotation);

            // Transoms: above the 4.5 m door head, mid upper glass, top.
            foreach(float y in new[]{4.60f,16.00f,26.62f})
                fins.Beam(mid+outward*.19f+Vector3.up*y,
                    new Vector3(.20f,.15f,length),rotation);

            roof.Beam(mid+outward*.28f+Vector3.up*27.39f,
                new Vector3(.56f,.34f,length),rotation);
            // (10.11: the 4.5 x 7.5 cm "gold roof detail" lay entirely inside the roof slab, its underside 2.5 mm over the
            // slab's - invisible except as flicker; removed)
            crownLight.Beam(mid+outward*.58f+Vector3.up*27.50f,
                new Vector3(.035f,.085f,length),rotation);
        }
        if(actualFeatureColumns!=38||skippedFeatureColumns!=2)
            throw new InvalidOperationException("ISSUE27 A1 facade regression: expected 38 actual columns and 2 skipped entrance positions, got "
                +actualFeatureColumns+" / "+skippedFeatureColumns);

        // 9.11 audit: these two 8-12 cm lines were ONE mesh of 1.47 million
        // triangles (720 parts x 3x3 rounded segments). From the street a 2 cm
        // edge radius reads the same with one chamfer segment (~10x lighter).
        // Gold upper/lower lines around the full 360-degree belt. The front
        // representative 48 m is 120 mm high / 50 mm proud. Elsewhere it is
        // 80 mm / 40 mm. A minimum 0.8 m taper is computed around x=+/-24 m.
        const int trimParts=720;
        for(int i=0;i<trimParts;i++)
        {
            float a=2f*Mathf.PI*i/trimParts,b=2f*Mathf.PI*(i+1)/trimParts;
            var p=new Vector3(rx*Mathf.Cos(a),0,rz*Mathf.Sin(a));
            var q=new Vector3(rx*Mathf.Cos(b),0,rz*Mathf.Sin(b));
            var mid=(p+q)*.5f;
            var outward=new Vector3(mid.x/(rx*rx),0,mid.z/(rz*rz)).normalized;
            var tangent=(q-p).normalized;
            var rotation=Quaternion.LookRotation(tangent,Vector3.up);
            float frontFactor=0f;
            if(mid.z<0)
                frontFactor=1f-Mathf.Clamp01((Mathf.Abs(mid.x)-24f)/.80f);
            float h=Mathf.Lerp(.10f,.20f,frontFactor);   // owner render: bold gold lines on the black belt
            float protrusion=Mathf.Lerp(.04f,.05f,frontFactor);
            float radius=Mathf.Lerp(.020f,.025f,frontFactor);
            float len=Vector3.Distance(p,q)+.025f;
            float totalDepth=protrusion+.002f; // 2mm hidden in black belt
            float radialCenter=.229f+protrusion*.5f; // visible outer face unchanged
            beltGold.RoundedPrism(mid+outward*radialCenter+
                    Vector3.up*(BeltBottom+h*.5f),
                new Vector3(totalDepth,h,len),rotation,radius,radius,1,1);
            beltGold.RoundedPrism(mid+outward*radialCenter+
                    Vector3.up*(BeltTop-h*.5f),
                new Vector3(totalDepth,h,len),rotation,radius,radius,1,1);
        }

        // Interior elliptical halo remains visible through the upper smoked glass.
        const int haloParts=96;
        for(int i=0;i<haloParts;i++)
        {
            float a=2f*Mathf.PI*i/haloParts,b=2f*Mathf.PI*(i+1)/haloParts;
            var p=new Vector3(57f*Mathf.Cos(a),0,34f*Mathf.Sin(a));
            var q=new Vector3(57f*Mathf.Cos(b),0,34f*Mathf.Sin(b));
            var mid=(p+q)*.5f;
            var rot=Quaternion.LookRotation((q-p).normalized,Vector3.up);
            upperCeilingHalo.Beam(mid+Vector3.up*23.25f,
                new Vector3(.22f,.045f,Vector3.Distance(p,q)+.025f),rot);
        }

        // Owner 9.11: every sign is its own 3D body (artwork outline extruded in
        // BraxChampionsArenaSigns) - no flat picture inside a rectangular frame.
        if(!BraxChampionsArenaSigns.HasSources())
            Debug.LogWarning("A1: owner sign PNGs unavailable; facade may generate but visual acceptance MUST NOT be marked PASS");

        plinth.Emit(group,"granite_plinth_preserved",exterior["dark_stone"]);
        fins.Emit(group,"curtain_wall_black_mullions_owner_render",black);
        blackColumns.Emit(group,"ISSUE27_black_columns_38_width2p20_depth0p85_rounded",black);
        // Owner render: the line on every black column is a glowing warm LED, not painted gold.
        goldColumnInserts.Emit(group,"ISSUE27_warm_LED_column_capsules_width0p12_projection0p03",ArenaLed(exterior));
        midBelt.Emit(group,"black_belt_360_owner_render_y7p60_to11p40",black);
        // 10.11: the thin lines are replaced by the owner render's wide rounded nosings (FacadeGoldTrims).
        roof.Emit(group,"PBR_roof_coping_preserved",exterior["roof"]);
        crownLight.Emit(group,"warm_roof_crown_LED_preserved",exterior["light"]);
        upperCeilingHalo.Emit(group,"upper_interior_elliptical_halo_preserved",exterior["light"]);
        columnUplights.Emit(group,"warm_column_foot_uplights_38",ArenaLed(exterior));
        columnGold.Emit(group,"wide_rounded_gold_column_strips_38_w0p44",gold);

        // FINAL OWNER CORRECTION 8.11:
        // zero separate red geometry outside the original CHAMPION ARENA artwork.
        // Red remains ONLY in the owner PNG itself. Exterior framing is black + gold.

        // Owner render 9.11 ("Zadaszenie tak samo"): the old black 48 x 13 m
        // cassette canopy and its two round gold posts are replaced by the
        // render's gold cantilever canopy with the sign box standing on it.
        FrontGoldCanopy(group,exterior);
        FacadeGoldTrims(group,exterior);
        EntrancePortal(group,exterior);

        // ATHLETES NORTH portico is intentionally preserved unchanged.
        {
            const float side=1f;
            const string entry="ATHLETES";
            float z=side*62.6f;
            var portico=Group(group,"A1 ATHLETES | preserved pre-Issue27 PBR glass portico",Vector3.zero);
            foreach(float frameX in new[]{-10.45f,10.45f})
                Box(portico,entry+" Poly Haven granite canopy side edge",
                    exterior["stone"],new Vector3(frameX,5.23f,z),
                    new Vector3(.36f,.20f,7.20f),false);
            Box(portico,entry+" safety-glass canopy visible upper skin",
                exterior["clear_glass"],new Vector3(0,5.34f,z),
                new Vector3(21.0f,.07f,6.8f),false);
            foreach(float x in new[]{-10.8f,10.8f})
            {
                Box(portico,entry+" PBR polished metal support",
                    exterior["bronze"],new Vector3(x,2.78f,z+side*1.85f),
                    new Vector3(.22f,5.35f,.22f),true);
                Box(portico,entry+" PBR polished granite foot",
                    exterior["dark_stone"],new Vector3(x,.32f,z+side*1.85f),
                    new Vector3(.62f,.44f,.62f),true);
                Box(portico,entry+" continuous canopy edge",
                    exterior["steel"],new Vector3(x,5.42f,z),
                    new Vector3(.15f,.22f,7.3f),false);
            }
            Box(portico,entry+" brushed-metal fascia",
                exterior["bronze"],new Vector3(0,5.43f,z+side*3.60f),
                new Vector3(21.8f,.20f,.15f),false);
            Box(portico,entry+" warm facade light diffuser (no shadow lamp)",
                exterior["light"],new Vector3(0,5.14f,z+side*3.52f),
                new Vector3(20f,.035f,.07f),false);
        }

        Debug.Log("ISSUE27_A1_PRECHANGE_AUDIT measured_ellipse=192x118 circumference=493.91m"+
            " source_bays=360 potential_feature_positions=40 actual_feature_columns="+actualFeatureColumns+
            " skipped_feature_positions_at_doors="+skippedFeatureColumns+
            " owner_signs=4 sign_front=3D_39x6.5_on_canopy sign_rear=36x6 sign_sides=34x5.667"+
            " legacy_public_canopy=21x6.8x0.07_removed=true athlete_canopy_preserved=true");
        Debug.Log("A1_OWNER_RENDER_FACADE black_columns=38 width=2.20 depth=0.85 y=1.03..26.55 unbroken"+
            " warm_LED_column_capsule=full_height LED_column_front_width=.200 projection=.030 totalDepth=.038 embedded=.008 edge_R=.018 capsule_end_R=.060"+
            " belt_360_y="+BeltBottom+".."+BeltTop+" depth=0.22 outer_face=0.23"+
            " mullions=black_every_2nd_bay transoms=4.60,16.00,26.62 white_interior_bands=REMOVED"+
            " canopy=gold_cantilever_40m posts=0 sign_frames=REMOVED signs=3D_bodies front_sign_external_red_geometry=0"+
" APK_NOT_STARTED=true scene_visual_review_required=true");
    }

    // Owner's approved exterior photograph shows a genuine furnished upper
    // VIP lounge through the two-storey smoke glass. Previously the render
    // had only an LED halo and empty air. Furnish the FRONT glazing recession
    // with PBR leather and native Poly Haven oak/metal, no new textures,
    // no duplicate real arena VIP seating, no collider or gameplay change.
    static void ReferenceUpperFacadeLounge(Transform exteriorRoot,
        Dictionary<string,Material> exterior)
    {
        // Owner render 9.11: the lounge floor sits directly on the black belt
        // (sofas visible just above the band). The whole furnished lounge is
        // authored at its old 18.24 m floor and moved down ONCE here.
        var root=Group(exteriorRoot,
            "A1 UPPER GLASS | reference furnished VIP lounge silhouette",Vector3.up*LoungeDrop);
        var leather=BraxChampionsFoundation.LibraryLeather(
            "A1_upper_facade_charcoal_leather_2k",new Color(.23f,.225f,.22f));
        // All visible upholstery stays black leather; no brown/warm cushions.
        var teak=exterior["teak"];
        var gold=exterior["gold"];
        var dark=exterior["dark_stone"];
        var light=exterior["light"];

        // The glazed A1 ellipse is 192x118 nominal metres. At x=+-42,
        // southern glass is z~-53, so this slab stays inside the envelope
        // (front edge z=-51.5). Preserve the central ground-level lobby.
        // Depth 6.9 m (was 10.6): with the lower floor the slab must stay
        // clear of the entrance mural so it reads whole from the lobby.
        Box(root,"UPPER lounge | real premium dark-stone mezzanine floor",dark,
            new Vector3(0,18.12f,-48.05f),new Vector3(84f,.24f,6.9f),false);
        BraxChampionsFoundation.Optional("loża VIP na piętrze: rośliny i balustrada",()=>UpperLoungeFinish(root,exterior));
        // Owner render: the lobby ceiling behind the lower glass is full of warm light lines (seen from the street
        // through the glass and from the lobby). Underside of the mezzanine slab = 18.00 in lounge space.
        {
            var led=ArenaLed(exterior);
            Box(root,"UPPER lounge slab | warm white plaster soffit over the lobby",
                BraxChampionsFoundation.SourceMaterial("A1_lobby_soffit_warm_white_plaster",BraxChampionsFoundation.InteriorPaintSlug,2.0f,BraxChampionsFoundation.PaintTint(new Color(.93f,.92f,.90f))),
                new Vector3(0,17.997f,-48.05f),new Vector3(83.9f,.004f,6.88f),false);
            for(float x=-40.5f;x<=40.51f;x+=3f)
                Box(root,"UPPER lounge slab | warm LED line in the lobby ceiling",led,
                    new Vector3(x,17.992f,-48.05f),new Vector3(.06f,.016f,6.3f),false);
            Box(root,"UPPER lounge slab | warm LED line along the glass",led,
                new Vector3(0,17.992f,-51.15f),new Vector3(82f,.016f,.06f),false);
        }
        Box(root,"UPPER lounge | fine champagne fascia at floor edge",gold,
            new Vector3(0,18.13f,-51.56f),new Vector3(83.5f,.11f,.12f),false);

        // Six correctly scaled upright black-leather sofas face the glazed frontage.
        // Backrest is on +Z and the open seat faces SOUTH glazing (-Z), so the
        // furniture cannot read as lying on its back or facing away from glass.
        // Visible upholstery is black PBR leather throughout.
        foreach(float x in new[]{-35f,-22f,-9f,9f,22f,35f})
        {
            float z=-45.9f;
            var sofa=Group(root,"UPPER lounge | upright black leather sofa x="+x,
                new Vector3(x,0,z));
            // Realistic ~2.4 m three-seat sofa footprint, seated directly above
            // the mezzanine floor instead of the previous oversized silhouette.
            Box(sofa,"Original PBR oak underframe",teak,
                new Vector3(0,18.51f,0),new Vector3(2.42f,.18f,.92f),false);
            Box(sofa,"Textured black leather sofa seat",leather,
                new Vector3(0,18.74f,-.08f),new Vector3(2.30f,.28f,.88f),false);
            Box(sofa,"Upright black leather backrest",leather,
                new Vector3(0,19.10f,.37f),new Vector3(2.36f,.78f,.24f),false);
            foreach(float side in new[]{-1f,1f})
            {
                Box(sofa,"Black leather arm support",leather,
                    new Vector3(side*1.10f,18.92f,-.02f),
                    new Vector3(.20f,.54f,.92f),false);
                foreach(float legZ in new[]{-.35f,.35f})
                    Box(sofa,"Champagne metal furniture foot",gold,
                        new Vector3(side*.98f,18.33f,legZ),
                        new Vector3(.06f,.18f,.06f),false);
            }
            foreach(float offset in new[]{-.72f,0f,.72f})
            {
                Box(sofa,"Three black leather seat cushions",leather,
                    new Vector3(offset,18.93f,-.10f),
                    new Vector3(.68f,.10f,.72f),false);
                Box(sofa,"Three black leather back cushions",leather,
                    new Vector3(offset,19.08f,.22f),
                    new Vector3(.66f,.46f,.13f),false);
            }
            var table=Group(root,"UPPER lounge | oak coffee table x="+x,
                new Vector3(x,0,-48.2f));
            Box(table,"Native Poly Haven teak table surface",teak,
                new Vector3(0,18.82f,0),new Vector3(1.74f,.09f,.75f),false);
            foreach(float side in new[]{-1f,1f})
                Box(table,"Gold table leg",gold,
                    new Vector3(side*.73f,18.60f,0),
                    new Vector3(.065f,.44f,.065f),false);
        }
        // The reference has warm lamps and refined uplighting behind the
        // glazing; this uses only existing emissive facade material.
        foreach(float x in new[]{-40f,-15f,15f,40f})
        {
            Box(root,"UPPER lounge | warm table lamp",light,
                new Vector3(x,19.11f,-47.0f),new Vector3(.44f,.47f,.44f),false);
            // Audit 9.11: no table stands at x=+-15/+-40, the old 23 cm foot
            // floated 0.6 m above the slab. Real floor lamp: base + stem.
            Box(root,"UPPER lounge | lamp champagne foot",gold,
                new Vector3(x,18.255f,-47.0f),new Vector3(.30f,.03f,.30f),false);
            Box(root,"UPPER lounge | lamp champagne stem",gold,
                new Vector3(x,18.56f,-47.0f),new Vector3(.035f,.62f,.035f),false);
        }
        Debug.Log("A1_REFERENCE_UPPER_GLASS_LOUNGE furnishings=6_upright_blackPBR_leather_sofas"+
            " 6_oak_tables warm_lamps=4 upper_floor_inside_ellipse=true"+
            " no_collision=true originalArenaVIPSeatsUnchanged=true"+
            " playerVisualProofPending=true");
    }

    const string PremiumShrubSource = "Assets/Source/CityRebuild/landscape/park_shrub.glb";
    public const string PremiumShrubSourcePath = PremiumShrubSource;

    static Bounds RendererBounds(GameObject go)
    {
        var renderers=go.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static void PlacePremiumShrub(Transform parent,Vector3 nominal,float yaw,float nominalHeight,float baseY=.245f)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(PremiumShrubSource);
        if(!source)
        {
            BraxBuildQuality.Warn("A1_PREMIUM_SHRUB_SOURCE_MISSING: "+PremiumShrubSource);
            return;
        }
        var go=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!go){BraxBuildQuality.Warn("A1_PREMIUM_SHRUB_INSTANTIATE_FAILED");return;}
        go.name="A1 premium entrance shrub | original project GLB";
        go.transform.SetParent(parent,false);
        go.transform.localPosition=nominal*K;
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        Physics.SyncTransforms();
        var b=RendererBounds(go);
        if(b.size.y>.001f)
            go.transform.localScale*=nominalHeight*K/b.size.y;
        Physics.SyncTransforms();
        b=RendererBounds(go);
        // Put the visible bottom on the same arrival paving plane.
        go.transform.position+=Vector3.up*(baseY*K-b.min.y);
        foreach(var col in go.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(col);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows=true;
        }
    }

    static void ReferenceEntranceLandscaping(Transform parent,Dictionary<string,Material> exterior)
    {
        // Reference photo: symmetrical low dark-stone planters and clipped greenery
        // framing the red-carpet approach, never blocking the 6.4m public aisle.
        foreach(float side in new[]{-1f,1f})
        {
            float x=side*17.2f;
            Box(parent,"A1 reference entrance | black-stone planter",exterior["dark_stone"],
                new Vector3(x,.46f,-61.7f),new Vector3(10.6f,.48f,2.55f),true);
            Box(parent,"A1 reference entrance | gold planter cap",exterior["gold"],
                new Vector3(x,.715f,-61.7f),new Vector3(10.72f,.035f,2.67f),false);
            for(int i=-2;i<=2;i++)
                PlacePremiumShrub(parent,new Vector3(x+i*1.85f,.25f,-61.7f),
                    side<0?12f:-12f,1.22f);
        }
        // Owner render (10.11 07:31 "ja ci zdjęcie dałem, tak wygląda"): a square black planter with a clipped shrub
        // either side of the doors, just outside the rope line, gold rim, lit from the paving in front of it.
        foreach(float side in new[]{-1f,1f})
        {
            float x=side*7.25f,z=-60.9f;
            Box(parent,"A1 reference entrance | square black planter by the door",exterior["arena_black"],
                new Vector3(x,.245f+.45f,z),new Vector3(1.05f,.90f,1.05f),true);
            Box(parent,"A1 reference entrance | gold rim of the door planter",exterior["arena_gold"],
                new Vector3(x,.245f+.915f,z),new Vector3(1.11f,.03f,1.11f),false);
            Box(parent,"A1 reference entrance | soil of the door planter",exterior["dark_stone"],
                new Vector3(x,.245f+.89f,z),new Vector3(.97f,.02f,.97f),false);
            PlacePremiumShrub(parent,new Vector3(x,.25f,z),side<0?20f:-20f,1.10f,.245f+.88f);
            Box(parent,"A1 reference entrance | door planter warm uplight",ArenaLed(exterior),
                new Vector3(x,.255f,z-.82f),new Vector3(.26f,.012f,.26f),false);
        }
        // Owner render: warm in-ground uplights along the carpet approach and
        // in front of both planters (flush 30 x 30 cm, 1 cm proud, no collider).
        foreach(float x in new[]{-5.4f,5.4f})
            for(float z=-60.6f;z>=-75.0f;z-=2.4f)
                Box(parent,"A1 reference entrance | flush warm in-ground uplight",exterior["light"],
                    new Vector3(x,.255f,z),new Vector3(.30f,.012f,.30f),false);
        foreach(float side in new[]{-1f,1f})
            for(int i=-2;i<=2;i++)
                Box(parent,"A1 reference entrance | planter in-ground uplight",exterior["light"],
                    new Vector3(side*17.2f+i*2.1f,.255f,-63.5f),new Vector3(.24f,.012f,.24f),false);
        // Low architectural path lights outside the rope line; no obstruction in the carpet aisle.
        foreach(float x in new[]{-8.6f,8.6f})
        foreach(float z in new[]{-73.0f,-68.8f,-64.6f,-60.8f})
        {
            Box(parent,"A1 reference entrance | black bollard",exterior["dark_stone"],
                new Vector3(x,.66f,z),new Vector3(.16f,.92f,.16f),true);
            Box(parent,"A1 reference entrance | warm bollard head",exterior["light"],
                new Vector3(x,1.13f,z),new Vector3(.25f,.12f,.25f),false);
        }
    }

    public static bool Generate(Transform arenaRoot, Dictionary<string, Material> exterior)
    {
        tiledMeshSerial = 0;
        arenaLed = null;
        ArenaSmokedGlass(arenaRoot, exterior);
        // First: the lobby furniture, cases and runner are grounded on this finished floor.
        LobbyFloor(arenaRoot, exterior, LobbyFloorMaterial());
        LobbyCeiling(arenaRoot, exterior);
        LobbyPendants(arenaRoot, exterior);
        var root = Group(arenaRoot, "A1 | arena entrance, parking, trophy and athlete facilities", Vector3.zero);
        var asphalt = BraxChampionsFoundation.SourceMaterial("arena_parking_asphalt", "clean_asphalt", 2.2f,
            new Color(.89f, .89f, .89f));
        var sidewalk = BraxChampionsFoundation.SourceMaterial("arena_arrival_paving", "concrete_pavement_02", 1.1f,
            new Color(.94f, .94f, .93f));
        // Separate original PBR textiles: deep red carpet and red velvet ropes.
        // b33 phone proof: the fabric_pattern_05 weave read as red TILES.
        // Real carpet pile = the same Poly Haven velour as the arena stairs.
        var redCarpet = BraxChampionsFoundation.SourceMaterial(
            "arena_vip_red_carpet_velour", "velour_velvet", .90f,
            new Color(.55f, .03f, .045f));
        redCarpet.SetFloat("_Roughness", .62f);
        redCarpet.SetFloat("_Metallic", 0f);
        var redRope = BraxChampionsFoundation.SourceMaterial(
            "arena_vip_red_velvet_rope", "fabric_pattern_05", .6f,
            new Color(.48f, .012f, .022f));
        // Owner-selected interior finishes: Stone Tiles on athlete/changing floors,
        // Wooden Panels only on athlete entrance/changing-room WALLS.
        var arenaStoneFloor = BraxChampionsFoundation.SourceMaterial(
            "arena_athlete_floor_stone_tiles_owner", "stone_tiles", 3.2f, Color.white);
        var athleteWoodWall = BraxChampionsFoundation.SourceMaterial(
            "arena_athlete_walls_wooden_panels_owner", "wooden_panels", 2.1f, Color.white);
        var paint = BraxChampionsHarbour.Nakladka(Paint("arena_parking_markings", new Color(.93f, .92f, .85f)));   // no flicker
        Parking(root, asphalt, paint, sidewalk, exterior["dark_stone"], exterior["steel"]);
        ParkingLights(root, exterior["steel"], exterior["light"]);
        // Architectural PBR finish is generated independently of source signs,
        // without shifting the approved ellipse, doors or visitor routes.
        PremiumExterior(root, exterior);
        // Reference glass must reveal the finished VIP level, not empty black air.
        ReferenceUpperFacadeLounge(root, exterior);
        ReferenceEntranceLandscaping(root, exterior);
        Stanchions(root, exterior["gold"], redRope, redCarpet);
        Door(root, "CHAMPIONS_A1_PUBLIC", "Main public entrance", -59.04f, exterior["clear_glass"], exterior["steel"],
            arenaBlackGlass, exterior["arena_black"], exterior["arena_gold"]);
        Door(root, "CHAMPIONS_A1_ATHLETES", "Separate athlete rear entrance", 59.04f, exterior["clear_glass"], exterior["steel"],
            arenaBlackGlass, exterior["arena_black"], exterior["arena_gold"]);
        // Athletes' wing (BraxChampionsArenaAthletes.cs): same room shells and owner
        // finishes, lux furniture, private rear car park and arrival walk.
        AthleteRoomShells(root, arenaStoneFloor, athleteWoodWall, exterior["dark_stone"]);
        BraxChampionsFoundation.Optional("szatnie zawodników lux", () => LuxDressingRooms(root, exterior));
        BraxChampionsFoundation.Optional("tył areny: parking i wejście zawodników", () => AthleteRearArrival(root, exterior, asphalt, paint, sidewalk));
        Box(root, "Main lobby continuous RED carpet", redCarpet,
            // Join exterior carpet exactly at z=-59.3; previously the
            // interior runner started at -58.3 leaving a 1m bare threshold.
            // Adjacent ends, not overlapping coplanar panels (no Z-fighting).
            new Vector3(0, .239f, -47.3f), new Vector3(3.9f, .018f, 24.0f), false);
        foreach(float edge in new[]{-1.98f,1.98f})
            Box(root,"Main lobby RED carpet | continuous flush gold edge",
                exterior["gold"],new Vector3(edge,.251f,-47.3f),
                new Vector3(.045f,.010f,24.0f),false);
        Box(root, "Separate red athlete walkway to cage", redCarpet,
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
        // Owner 9.11: the belt goes in the owner-photo vitrine where it works best:
        // the lobby, facing the entrance. Shown ONCE (removed from the judges' area).
        BraxChampionsFoundation.Optional("gablota pasa w lobby",()=>BraxChampionsBeltShowcase.GenerateLobby(root));
        BraxChampionsFoundation.Optional("lobby: sofy i kwiaty za szybą wejścia",()=>EntranceLobbyFurniture(arenaRoot, exterior));
        BraxChampionsFoundation.Optional("palmy przed areną",()=>EntrancePalmAvenue(arenaRoot, root));
        BraxChampionsFoundation.Optional("palmy na trawnikach przy ulicy i po bokach",()=>LawnPalms(arenaRoot, root, exterior));
        BraxChampionsFoundation.Optional("flagi przed areną",()=>ArenaFlags(arenaRoot, root, exterior));
        BraxChampionsFoundation.Optional("lobby: galeria portretów, kanapy, rośliny",()=>LobbyGallery(arenaRoot, exterior));
        BraxChampionsFoundation.Optional("gabloty z pucharami w lobby",()=>EntranceTrophyCases(arenaRoot, exterior));
        return AssetDatabase.LoadAssetAtPath<GameObject>(BeltAsset) != null;
    }
}

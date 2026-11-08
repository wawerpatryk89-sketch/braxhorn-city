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
            GradedSurface(parent,"Parking road-to-city gentle asphalt entry",asphalt,
                x-3.2f,x+3.2f,-140f,-133f,0f,parkingTop,true);
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
                new Vector3(x,.239f,-109.02f),new Vector3(5.9f,.002f,.12f),false);
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
        foreach (float sx in new[] { -34.5f, 34.5f })
            Box(parent, "Accessible parking-to-arena paved connector", curb,
                new Vector3(sx, .18f, -79f), new Vector3(53f, .04f, 5f));
        // Main spectator route stays permanently clear between parking and entrance.
        // Connectors meet x=+-8 and the parking exit paths at x=+-61 exactly:
        // no coplanar overlap rectangles.
        Box(parent, "Arena public promenade", curb,
            new Vector3(0, .18f, -104f), new Vector3(16f, .04f, 58f));
        for (float x = -7f; x <= 7f; x += 14f)
            Box(parent, "Public path edge line", paint,
                new Vector3(x, .204f, -105.5f), new Vector3(.12f, .008f, 54f), false);
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

    static void DressingRooms(Transform parent, Material floor, Material woodWall, Material ceiling, Material bench, Material metal)
    {
        // Rear of adapted bowl = +Z. Athletes enter from north (+59) to original (+35) gate.
        // Four-metre central route must remain unobstructed.
        foreach (float side in new[] { -1f, 1f })
        {
            float mid = side * 14f, inner = side * 6.2f, outer = side * 22f;
            Box(parent, "Athlete changing room floor | green Stone Tiles", floor,
                new Vector3(mid, .239f, 45.5f), new Vector3(15.8f, .018f, 14f), false);
            Box(parent, "Rear changing room exterior partition | Wooden Panels", woodWall,
                new Vector3(mid, 1.9f, 52.5f), new Vector3(15.8f, 3.5f, .16f));
            Box(parent, "Changing room front privacy partition | Wooden Panels", woodWall,
                new Vector3(mid, 1.9f, 38.5f), new Vector3(15.8f, 3.5f, .16f));
            Box(parent, "Changing room enclosed ceiling", ceiling,
                new Vector3(mid, 3.62f, 45.5f), new Vector3(15.8f, .13f, 14f), false);
            Box(parent, "Changing room outer partition | Wooden Panels", woodWall,
                new Vector3(outer, 1.9f, 45.5f), new Vector3(.16f, 3.5f, 14f));
            // Break the inward wall to leave a real 2.4m dressing-room entrance.
            Box(parent, "Changing room inner wall 1 | Wooden Panels", woodWall,
                new Vector3(inner, 1.9f, 41.2f), new Vector3(.16f, 3.5f, 5.4f));
            Box(parent, "Changing room inner wall 2 | Wooden Panels", woodWall,
                new Vector3(inner, 1.9f, 50.6f), new Vector3(.16f, 3.5f, 3.8f));
            Box(parent, "Locker room bench", bench,
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
            var cc=go.AddComponent<CylinderCollider>();
            cc.center=center*K;cc.radius=radius*K;cc.height=height*K;cc.direction=1;
        }
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static GameObject PublicCanopyShell(Transform parent,Material material)
    {
        const float rx=96f,rz=59f,halfWidth=24f,depth=13f,corner=.80f;
        const float y0=5.65f,y1=6.50f;
        const int segments=64;
        float BackZ(float x)=>-rz*Mathf.Sqrt(1f-x*x/(rx*rx))+.20f;
        float EffectiveDepth(float x)
        {
            float ax=Mathf.Abs(x);
            if(ax<=halfWidth-corner)return depth;
            float dx=Mathf.Clamp(ax-(halfWidth-corner),0,corner);
            return depth-corner+Mathf.Sqrt(Mathf.Max(0,corner*corner-dx*dx));
        }
        Vector3 Back(float x,float y)=>new Vector3(x,y,BackZ(x));
        Vector3 Front(float x,float y)=>new Vector3(x,y,BackZ(x)-EffectiveDepth(x));

        var v=new List<Vector3>();var t=new List<int>();
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int n=v.Count;v.Add(a*K);v.Add(b*K);v.Add(c*K);v.Add(d*K);
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);
        }
        for(int i=0;i<segments;i++)
        {
            float x0=Mathf.Lerp(-halfWidth,halfWidth,i/(float)segments);
            float x1=Mathf.Lerp(-halfWidth,halfWidth,(i+1)/(float)segments);
            // Top plate. Bottom is intentionally open and replaced by recessed cassettes.
            Quad(Back(x0,y1),Back(x1,y1),Front(x1,y1),Front(x0,y1));
            // Full-thickness front wall.
            Quad(Front(x0,y0),Front(x0,y1),Front(x1,y1),Front(x1,y0));
        }
        // End walls close the rounded plan at +/-24 m.
        float xr=halfWidth,xl=-halfWidth;
        Quad(Back(xr,y0),Front(xr,y0),Front(xr,y1),Back(xr,y1));
        Quad(Front(xl,y0),Back(xl,y0),Back(xl,y1),Front(xl,y1));

        var mesh=new Mesh{name="A1 Issue27 public canopy curved 48x13x0.85",
            indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,
            "Assets/PlayableGenerated/Mistrzowie/Buildings/Meshes/A1_Premium_public_canopy_shell.asset");
        var go=new GameObject("A1 PUBLIC | ISSUE27 curved black canopy 48.00x13.00x0.85");
        go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static float ActualFloorYNominal(Transform arenaRoot,float x,float z)
    {
        // BraxChampionsBuildings temporarily moves the reusable A1 root to world
        // zero before generating/saving the prefab. The FOUNDATION remains at
        // the approved world plot centre (600,800), therefore the grounding
        // raycast must be cast at the true plot coordinates, not TransformPoint
        // through the temporarily-zero prefab root.
        const float plotX=600f,plotZ=800f;
        Physics.SyncTransforms();
        var start=new Vector3((plotX+x)*K,20f*K,(plotZ+z)*K);
        if(!Physics.Raycast(start,Vector3.down,out var hit,50f*K,~0,QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException("Issue27 canopy support floor raycast missed at world plot x="+
                (plotX+x)+" z="+(plotZ+z));
        return hit.point.y/K;
    }

    static void PremiumExterior(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        const float rx=96f,rz=59f;
        const int bays=360;
        var group=Group(arenaRoot,"A1 PREMIUM | ISSUE 27 approved external rebuild",Vector3.zero);
        var black=exterior["arena_black"];
        var gold=exterior["arena_gold"];
        var redNeon=Paint("arena_reference_red_neon",new Color(.68f,.018f,.025f));
        redNeon.SetColor("_EmissionColor",new Color(.70f,.018f,.020f));
        EditorUtility.SetDirty(redNeon);

        var plinth=new FacadeBatch();
        var fins=new FacadeBatch();
        var blackColumns=new FacadeBatch();
        var goldColumnInserts=new FacadeBatch();
        var midBelt=new FacadeBatch();
        var beltGold=new FacadeBatch();
        var signGoldFrames=new FacadeBatch();
        var underSignGold=new FacadeBatch();
        var roof=new FacadeBatch();
        var roofGold=new FacadeBatch();
        var crownLight=new FacadeBatch();
        var interiorWarmBands=new FacadeBatch();
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
                    // ISSUE #27 exact column envelope:
                    // tangential width 2.20 m, radial depth 0.85 m,
                    // lower y 1.03..10.85, upper y 18.55..26.55.
                    blackColumns.RoundedPrism(
                        mid+outward*.425f+Vector3.up*5.94f,
                        new Vector3(.85f,9.82f,2.20f),rotation,.10f,.08f,4,3);
                    blackColumns.RoundedPrism(
                        mid+outward*.425f+Vector3.up*22.55f,
                        new Vector3(.85f,8.00f,2.20f),rotation,.10f,.08f,4,3);

                    // Gold is physically attached to the outer black face:
                    // 120 mm face width, 30 mm projection, 60 mm rounded ends.
                    goldColumnInserts.CapsulePlaque(
                        mid+outward*.861f+Vector3.up*5.94f,
                        .038f,9.70f,.120f,.018f,rotation,8,4);
                    goldColumnInserts.CapsulePlaque(
                        mid+outward*.861f+Vector3.up*22.55f,
                        .038f,7.88f,.120f,.018f,rotation,8,4);
                }
                else if(i%3==0)
                {
                    fins.Beam(mid+outward*.19f+Vector3.up*5.68f,
                        new Vector3(.19f,9.30f,.17f),rotation);
                    fins.Beam(mid+outward*.19f+Vector3.up*22.87f,
                        new Vector3(.19f,7.35f,.17f),rotation);
                }
            }
            else
            {
                skippedDoorSegments++;
                if(featurePillar)skippedFeatureColumns++;
            }

            // ISSUE #27 continuous black belt: outer face at +0.23 m,
            // radial depth 0.22 m, y 10.85..18.55 (7.70 m).
            midBelt.Beam(mid+outward*.12f+Vector3.up*14.70f,
                new Vector3(.22f,7.70f,length),rotation);

            // Existing upper/lower curtain-wall separators remain outside the belt.
            foreach(float y in new[]{5.18f,10.52f,19.05f,26.62f})
                fins.Beam(mid+outward*.19f+Vector3.up*y,
                    new Vector3(.20f,.15f,length),rotation);

            roof.Beam(mid+outward*.28f+Vector3.up*27.39f,
                new Vector3(.56f,.34f,length),rotation);
            roofGold.Beam(mid+outward*.32f+Vector3.up*27.09f,
                new Vector3(.045f,.075f,length),rotation);
            crownLight.Beam(mid+outward*.58f+Vector3.up*27.50f,
                new Vector3(.035f,.085f,length),rotation);
            foreach(float warmY in new[]{20.55f,22.65f,24.75f})
                interiorWarmBands.Beam(mid-outward*.13f+Vector3.up*warmY,
                    new Vector3(.025f,.055f,length),rotation);
        }
        if(actualFeatureColumns!=38||skippedFeatureColumns!=2)
            throw new InvalidOperationException("ISSUE27 A1 facade regression: expected 38 actual columns and 2 skipped entrance positions, got "
                +actualFeatureColumns+" / "+skippedFeatureColumns);

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
            float h=Mathf.Lerp(.08f,.12f,frontFactor);
            float protrusion=Mathf.Lerp(.04f,.05f,frontFactor);
            float radius=Mathf.Lerp(.020f,.025f,frontFactor);
            float len=Vector3.Distance(p,q)+.025f;
            float totalDepth=protrusion+.002f; // 2mm hidden in black belt
            float radialCenter=.229f+protrusion*.5f; // visible outer face unchanged
            beltGold.RoundedPrism(mid+outward*radialCenter+
                    Vector3.up*(10.85f+h*.5f),
                new Vector3(totalDepth,h,len),rotation,radius,radius,3,3);
            beltGold.RoundedPrism(mid+outward*radialCenter+
                    Vector3.up*(18.55f-h*.5f),
                new Vector3(totalDepth,h,len),rotation,radius,radius,3,3);
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

        // ISSUE #27: real gold frame geometry around the four original owner PNGs.
        // Artwork, UVs, colours and GUIDs remain untouched.
        if(BraxChampionsArenaSigns.HasSources())
        {
            void FrameSign(bool front,bool positive,float span,float bottom,float height,float profile)
            {
                const int parts=48;
                const float graphicDepth=.36f;
                const float graphicGap=.08f;
                const float frameDepth=.082f;
                float frameOffset=graphicDepth+graphicGap+frameDepth*.5f;
                Vector3 Surface(float q)
                {
                    if(front)
                        return new Vector3(q,0,(positive?1f:-1f)*rz*
                            Mathf.Sqrt(1f-q*q/(rx*rx)));
                    return new Vector3((positive?1f:-1f)*rx*
                        Mathf.Sqrt(1f-q*q/(rz*rz)),0,q);
                }
                for(int j=0;j<parts;j++)
                {
                    float q0=Mathf.Lerp(-span*.5f,span*.5f,j/(float)parts);
                    float q1=Mathf.Lerp(-span*.5f,span*.5f,(j+1)/(float)parts);
                    var p0=Surface(q0);var p1=Surface(q1);var m=(p0+p1)*.5f;
                    var n=new Vector3(m.x/(rx*rx),0,m.z/(rz*rz)).normalized;
                    var rotation=Quaternion.LookRotation((p1-p0).normalized,Vector3.up);
                    float len=Vector3.Distance(p0,p1)+.018f;
                    signGoldFrames.RoundedPrism(
                        m+n*frameOffset+Vector3.up*(bottom-graphicGap-profile*.5f),
                        new Vector3(frameDepth,profile,len),rotation,.040f,.040f,4,3);
                    signGoldFrames.RoundedPrism(
                        m+n*frameOffset+Vector3.up*(bottom+height+graphicGap+profile*.5f),
                        new Vector3(frameDepth,profile,len),rotation,.040f,.040f,4,3);
                    // Additional 60 mm gold line below the sign, 25 mm proud.
                    underSignGold.RoundedPrism(
                        m+n*(graphicDepth+graphicGap+.0125f)+Vector3.up*(bottom-.11f),
                        new Vector3(.025f,.060f,len),rotation,.010f,.018f,2,1);
                }
                foreach(float side in new[]{-1f,1f})
                {
                    float q=side*(span*.5f+graphicGap+profile*.5f);
                    var p=Surface(q);
                    var n=new Vector3(p.x/(rx*rx),0,p.z/(rz*rz)).normalized;
                    var tangent=front?Vector3.right:Vector3.forward;
                    if((front&&positive)||(!front&&!positive))tangent=-tangent;
                    var rot=Quaternion.LookRotation(tangent,Vector3.up);
                    signGoldFrames.RoundedPrism(
                        p+n*frameOffset+Vector3.up*(bottom+height*.5f),
                        new Vector3(frameDepth,height+2f*(graphicGap+profile),profile),
                        rot,.040f,.040f,4,3);
                }
            }
            FrameSign(true,false,42f,11.12f,7f,.16f);
            FrameSign(true,true,36f,11.42f,6f,.16f);
            FrameSign(false,true,34f,11.70f,5.667f,.10f);
            FrameSign(false,false,34f,11.70f,5.667f,.10f);
        }
        else
        {
            Debug.LogWarning("ISSUE27 A1: owner sign PNGs unavailable; facade may generate but visual acceptance MUST NOT be marked PASS");
        }

        plinth.Emit(group,"granite_plinth_preserved",exterior["dark_stone"]);
        fins.Emit(group,"curtain_wall_aluminium_preserved",exterior["steel"]);
        blackColumns.Emit(group,"ISSUE27_black_columns_38_width2p20_depth0p85_rounded",black);
        goldColumnInserts.Emit(group,"ISSUE27_gold_column_capsules_width0p12_projection0p03",gold);
        midBelt.Emit(group,"ISSUE27_black_belt_360_y10p85_to18p55",black);
        beltGold.Emit(group,"ISSUE27_gold_belt_lines_front120mm_rest80mm",gold);
        signGoldFrames.Emit(group,"ISSUE27_rounded_gold_sign_frames_original_artwork_untouched",gold);
        underSignGold.Emit(group,"ISSUE27_thin_gold_line_under_original_signs",gold);
        roof.Emit(group,"PBR_roof_coping_preserved",exterior["roof"]);
        roofGold.Emit(group,"PBR_gold_roof_detail",gold);
        crownLight.Emit(group,"warm_roof_crown_LED_preserved",exterior["light"]);
        interiorWarmBands.Emit(group,"three_warm_upper_glass_bands_preserved",exterior["light"]);
        upperCeilingHalo.Emit(group,"upper_interior_elliptical_halo_preserved",exterior["light"]);

        // Existing red reference neon remains clear of the new gold frame and
        // does not alter any sign pixels.
        foreach(float x in new[]{-22.2f,22.2f})
            Box(group,"REFERENCE red neon vertical sign frame",redNeon,
                new Vector3(x,14.55f,-58.18f),new Vector3(.24f,5.8f,.18f),false);
        foreach(float x in new[]{-16.8f,16.8f})
        {
            Box(group,"REFERENCE red neon sign top accent",redNeon,
                new Vector3(x,17.10f,-59.02f),new Vector3(8.2f,.11f,.15f),false);
            Box(group,"REFERENCE red neon sign lower accent",redNeon,
                new Vector3(x,12.05f,-59.02f),new Vector3(8.2f,.09f,.15f),false);
        }

        // ISSUE #27 NEW PUBLIC SOUTH CANOPY. The old 21x6.8x0.07 public glass
        // canopy is deliberately NOT generated, preventing double geometry.
        var publicCanopy=Group(group,"A1 PUBLIC SOUTH | ISSUE27 canopy assembly",Vector3.zero);
        PublicCanopyShell(publicCanopy,black);

        float CanopyBackZ(float x)=>-rz*Mathf.Sqrt(1f-x*x/(rx*rx))+.20f;
        float CanopyDepthAt(float x)
        {
            const float halfWidth=24f,depth=13f,corner=.80f;
            float ax=Mathf.Abs(x);
            if(ax<=halfWidth-corner)return depth;
            float dx=Mathf.Clamp(ax-(halfWidth-corner),0,corner);
            return depth-corner+Mathf.Sqrt(Mathf.Max(0,corner*corner-dx*dx));
        }

        var canopyPanels=new FacadeBatch();
        var canopyFrontFascia=new FacadeBatch();
        var canopyGoldTrim=new FacadeBatch();
        var canopyUnderGold=new FacadeBatch();

        // Ten recessed black cassettes; the nine gaps carry warm LED strips.
        for(int i=0;i<10;i++)
        {
            float x=-22.5f+(i+.5f)*4.5f;
            float back=CanopyBackZ(x),front=back-CanopyDepthAt(x);
            canopyPanels.RoundedPrism(
                new Vector3(x,5.690f,(back+front)*.5f),
                new Vector3(4.36f,.030f,Mathf.Max(1f,back-front-1.20f)),
                Quaternion.identity,.035f,.012f,3,2);
        }
        for(int i=1;i<=9;i++)
        {
            float x=-22.5f+i*4.5f;
            float back=CanopyBackZ(x),front=back-CanopyDepthAt(x);
            Box(publicCanopy,"PUBLIC ISSUE27 recessed warm LED 3000K "+i,exterior["light"],
                new Vector3(x,5.675f,(back+front)*.5f),
                new Vector3(.050f,.012f,Mathf.Max(1f,back-front-1.00f)),false);
        }

        const int canopyEdgeParts=64;
        for(int i=0;i<canopyEdgeParts;i++)
        {
            float x0=Mathf.Lerp(-24f,24f,i/(float)canopyEdgeParts);
            float x1=Mathf.Lerp(-24f,24f,(i+1)/(float)canopyEdgeParts);
            float z0=CanopyBackZ(x0)-CanopyDepthAt(x0);
            float z1=CanopyBackZ(x1)-CanopyDepthAt(x1);
            var p0=new Vector3(x0,0,z0);var p1=new Vector3(x1,0,z1);
            var mid=(p0+p1)*.5f;
            var tangent=(p1-p0).normalized;
            var outward=Vector3.Cross(Vector3.up,tangent).normalized;
            var rot=Quaternion.LookRotation(tangent,Vector3.up);
            float len=Vector3.Distance(p0,p1)+.018f;
            // Fascia total depth .24m gives exact visible R=.12m.
            canopyFrontFascia.RoundedPrism(
                mid+outward*.121f+Vector3.up*5.89f,
                new Vector3(.242f,.48f,len),rot,.120f,.120f,4,4);
            // Upper gold: .112m total, outer face .332m; fascia outer face .242m -> exactly .090m exposed, R=.055.
            canopyGoldTrim.RoundedPrism(
                mid+outward*.276f+Vector3.up*6.42f,
                new Vector3(.112f,.16f,len),rot,.055f,.055f,4,4);
            // Lower gold: .052m total, outer face .282m; fascia outer face .242m -> exactly .040m exposed, R=.025.
            canopyGoldTrim.RoundedPrism(
                mid+outward*.256f+Vector3.up*5.69f,
                new Vector3(.052f,.08f,len),rot,.025f,.025f,3,3);
            canopyUnderGold.RoundedPrism(
                mid-outward*.22f+Vector3.up*5.6725f,
                new Vector3(.035f,.035f,len),rot,.015f,.015f,2,1);
        }
        // Upper side gold rails: 100 mm high, 60 mm proud.
        foreach(float x in new[]{-24f,24f})
        {
            float back=CanopyBackZ(x),front=back-CanopyDepthAt(x);
            float side=Mathf.Sign(x);
            canopyGoldTrim.RoundedPrism(
                new Vector3(x+side*.024f,6.45f,(back+front)*.5f),
                new Vector3(.072f,.100f,back-front),
                Quaternion.identity,.035f,.035f,4,3);
        }

        canopyPanels.Emit(publicCanopy,"ISSUE27_public_canopy_recessed_black_cassettes",black);
        canopyFrontFascia.Emit(publicCanopy,"ISSUE27_public_canopy_black_front_fascia_0p48",black);
        canopyGoldTrim.Emit(publicCanopy,"ISSUE27_public_canopy_gold_outline",gold);
        canopyUnderGold.Emit(publicCanopy,"ISSUE27_public_canopy_thin_under_gold_line",gold);

        // Two exact round gold supports. Raycast the REAL floor separately for
        // each support, then preserve the .20m base + .18m capital and touch the
        // canopy underside at y=5.65 exactly.
        var supportFloors=new List<float>();
        foreach(float x in new[]{-20.50f,20.50f})
        {
            float floorY=ActualFloorYNominal(arenaRoot,x,-68.70f);
            supportFloors.Add(floorY);
            float shaftH=5.65f-floorY-.20f-.18f;
            if(shaftH<4.8f||shaftH>5.2f)
                throw new InvalidOperationException("Issue27 support floor out of expected range: y="+floorY);
            float baseTop=floorY+.20f;
            float shaftTop=baseTop+shaftH;
            var support=Group(publicCanopy,"A1 PUBLIC | ISSUE27 round gold support x="+x+
                " floorY="+floorY.ToString("F3"),Vector3.zero);
            RoundedCylinder(support,"Gold support base Ø1.40 h0.20 R0.055",gold,
                new Vector3(x,floorY+.10f,-68.70f),.70f,.20f,.055f,true);
            RoundedCylinder(support,"Gold support stem Ø1.00 raycast-fit",gold,
                new Vector3(x,baseTop+shaftH*.5f,-68.70f),.50f,shaftH,.060f,true);
            RoundedCylinder(support,"Gold support head Ø1.25 h0.18 R0.045",gold,
                new Vector3(x,5.56f,-68.70f),.625f,.18f,.045f,false);
            RoundedCylinder(support,"Gold support transition collar R0.10",gold,
                new Vector3(x,shaftTop-.05f,-68.70f),.56f,.20f,.10f,false);
            RoundedCylinder(support,"Gold base decorative ring h0.065",gold,
                new Vector3(x,baseTop+.0325f,-68.70f),.73f,.065f,.020f,false);
        }

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
            " owner_signs=4 sign_front=42x7 sign_rear=36x6 sign_sides=34x5.667"+
            " legacy_public_canopy=21x6.8x0.07_removed=true athlete_canopy_preserved=true");
        Debug.Log("ISSUE27_A1_EXECUTION black_columns=38 width=2.20 depth=0.85 lower_y=1.03..10.85 upper_y=18.55..26.55"+
            " gold_column_front_width=.120 projection=.030 totalDepth=.038 embedded=.008 edge_R=.018 capsule_end_R=.060"+
            " belt_360_y=10.85..18.55 depth=0.22 outer_face=0.23"+
            " public_canopy=48x13x0.85 y=5.65..6.50 rounded_plan_R=0.80"+
            " canopy_round_gold_supports=2 supportFloorRaycast=true supportFloors="+
            string.Join(",",supportFloors.Select(v=>v.ToString("F3")))+
            " nominalVolumeAtFloor0p24_each=4.479m3 total=8.959m3"+
            " canopy_LED=9 recessed_cassettes=10 beltTrimR_front=.025 rest=.020 signFrameR=.040"+
            " fasciaR=.120 upperGoldR=.055 sideGoldR=.035 lowerGoldR=.025 original_sign_artwork_untouched=true"+
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
        var root=Group(exteriorRoot,
            "A1 UPPER GLASS | reference furnished VIP lounge silhouette",Vector3.zero);
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
        Box(root,"UPPER lounge | real premium dark-stone mezzanine floor",dark,
            new Vector3(0,18.12f,-46.2f),new Vector3(84f,.24f,10.6f),false);
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
            Box(root,"UPPER lounge | lamp champagne foot",gold,
                new Vector3(x,18.83f,-47.0f),new Vector3(.13f,.23f,.13f),false);
        }
        Debug.Log("A1_REFERENCE_UPPER_GLASS_LOUNGE furnishings=6_upright_blackPBR_leather_sofas"+
            " 6_oak_tables warm_lamps=4 upper_floor_inside_ellipse=true"+
            " no_collision=true originalArenaVIPSeatsUnchanged=true"+
            " playerVisualProofPending=true");
    }

    const string PremiumShrubSource = "Assets/Source/CityRebuild/landscape/park_shrub.glb";

    static Bounds RendererBounds(GameObject go)
    {
        var renderers=go.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
        return b;
    }

    static void PlacePremiumShrub(Transform parent,Vector3 nominal,float yaw,float nominalHeight)
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
        go.transform.position+=Vector3.up*(.245f*K-b.min.y);
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
        var root = Group(arenaRoot, "A1 | arena entrance, parking, trophy and athlete facilities", Vector3.zero);
        var asphalt = BraxChampionsFoundation.SourceMaterial("arena_parking_asphalt", "clean_asphalt", 2.2f,
            new Color(.89f, .89f, .89f));
        var sidewalk = BraxChampionsFoundation.SourceMaterial("arena_arrival_paving", "concrete_pavement_02", 1.1f,
            new Color(.94f, .94f, .93f));
        // Separate original PBR textiles: deep red carpet and red velvet ropes.
        var redCarpet = BraxChampionsFoundation.SourceMaterial(
            "arena_vip_red_carpet", "fabric_pattern_05", .6f,
            new Color(.62f, .025f, .045f));
        var redRope = BraxChampionsFoundation.SourceMaterial(
            "arena_vip_red_velvet_rope", "fabric_pattern_05", .6f,
            new Color(.48f, .012f, .022f));
        // Owner-selected interior finishes: Stone Tiles on athlete/changing floors,
        // Wooden Panels only on athlete entrance/changing-room WALLS.
        var arenaStoneFloor = BraxChampionsFoundation.SourceMaterial(
            "arena_athlete_floor_stone_tiles_owner", "stone_tiles", 3.2f, Color.white);
        var athleteWoodWall = BraxChampionsFoundation.SourceMaterial(
            "arena_athlete_walls_wooden_panels_owner", "wooden_panels", 2.1f, Color.white);
        var paint = Paint("arena_parking_markings", new Color(.93f, .92f, .85f));
        Parking(root, asphalt, paint, sidewalk, exterior["dark_stone"], exterior["steel"]);
        ParkingLights(root, exterior["steel"], exterior["light"]);
        // Architectural PBR finish is generated independently of source signs,
        // without shifting the approved ellipse, doors or visitor routes.
        PremiumExterior(root, exterior);
        // Reference glass must reveal the finished VIP level, not empty black air.
        ReferenceUpperFacadeLounge(root, exterior);
        ReferenceEntranceLandscaping(root, exterior);
        Stanchions(root, exterior["gold"], redRope, redCarpet);
        Door(root, "CHAMPIONS_A1_PUBLIC", "Main public entrance", -59.04f, exterior["clear_glass"], exterior["steel"]);
        Door(root, "CHAMPIONS_A1_ATHLETES", "Separate athlete rear entrance", 59.04f, exterior["clear_glass"], exterior["steel"]);
        DressingRooms(root, arenaStoneFloor, athleteWoodWall, exterior["dark_stone"], exterior["teak"], exterior["steel"]);
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
        // The original FBX is shown ONCE, in the judges' showcase generated by
        // BraxChampionsBeltShowcase inside the arena. Never duplicate it at entry.
        return AssetDatabase.LoadAssetAtPath<GameObject>(BeltAsset) != null;
    }
}

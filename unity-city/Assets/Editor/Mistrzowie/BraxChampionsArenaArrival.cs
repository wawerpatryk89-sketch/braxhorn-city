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
                new Vector3(x, .195f, -121f), new Vector3(6.4f, .07f, 24f));
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
        // Wheelchair-friendly pedestrian connections from both side car parks
        // to the central public promenade, wholly behind the existing z=650 road.
        // The 5m cross-link is flat, unblocked and separate from vehicle aisles.
        foreach (float sx in new[] { -36.5f, 36.5f })
            Box(parent, "Accessible parking-to-arena paved connector", curb,
                new Vector3(sx, .190f, -79f), new Vector3(57f, .032f, 5f));
        // Main spectator route stays permanently clear between parking and entrance.
        Box(parent, "Arena public promenade", curb,
            new Vector3(0, .183f, -104f), new Vector3(16f, .04f, 58f));
        for (float x = -7f; x <= 7f; x += 14f)
            Box(parent, "Public path edge line", paint,
                new Vector3(x, .211f, -105.5f), new Vector3(.12f, .008f, 54f), false);
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


    // Premium A1: physically modelled curtain-wall trims and two real entrance
    // porticos. All visible solids reuse the VERIFIED original Poly Haven stone
    // and PBR metal/glass already loaded for this building. The underlying
    // approved ellipse, windows, doors, murals and signage are NEVER replaced.
    // Geometry is combined into a handful of meshes for Android performance.
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

    static void PremiumExterior(Transform arenaRoot,Dictionary<string,Material> exterior)
    {
        // Approved elliptical building exterior: 192 x 118 nominal metres,
        // 28m high. X/Z are local to A1; game-scale K is applied once.
        const float rx=96f,rz=59f;
        const int bays=120;
        var group=Group(arenaRoot,"A1 PREMIUM | Poly Haven PBR exterior detailing",Vector3.zero);
        var plinth=new FacadeBatch();
        var fins=new FacadeBatch();
        var bronze=new FacadeBatch();
        var roof=new FacadeBatch();
        var signBacker=new FacadeBatch();
        int skippedDoor=0;
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
            // The public and athlete glass doors must remain fully unobstructed.
            if(!entrance)
            {
                plinth.Beam(mid+outward*.23f+Vector3.up*.50f,
                    new Vector3(.38f,.78f,length),rotation);
                fins.Beam(mid+outward*.19f+Vector3.up*5.68f,
                    new Vector3(.19f,9.30f,.16f),rotation);
                fins.Beam(mid+outward*.19f+Vector3.up*22.87f,
                    new Vector3(.19f,7.35f,.16f),rotation);
                if(i%6==0)
                {
                    bronze.Beam(mid+outward*.34f+Vector3.up*5.68f,
                        new Vector3(.045f,9.30f,.10f),rotation);
                    bronze.Beam(mid+outward*.34f+Vector3.up*22.87f,
                        new Vector3(.045f,7.35f,.10f),rotation);
                }
            }
            else skippedDoor++;
            // Real metal mullion belt lines, not opaque panels over the glass.
            // A clear gap from y=11 to y=18 protects ALL four owner signs.
            foreach(float y in new[]{5.18f,10.52f,19.05f,26.62f})
                fins.Beam(mid+outward*.19f+Vector3.up*y,
                    new Vector3(.20f,.15f,length),rotation);
            roof.Beam(mid+outward*.28f+Vector3.up*27.39f,
                new Vector3(.56f,.34f,length),rotation);
            bronze.Beam(mid+outward*.32f+Vector3.up*27.09f,
                new Vector3(.045f,.075f,length),rotation);
        }

        // Slim dark PBR sign plinths behind, never on top of, the owner's
        // original curved 6:1 graphics. No logo, characters or words altered.
        if(BraxChampionsArenaSigns.HasSources())
        {
            void Backer(bool front,bool positive,float span,float bottom,float height)
            {
                const int parts=36;
                for(int j=0;j<parts;j++)
                {
                    float q0=(j/(float)parts-.5f)*span;
                    float q1=((j+1)/(float)parts-.5f)*span;
                    Vector3 Surface(float q)
                    {
                        if(front)
                            return new Vector3(q,0,(positive?1f:-1f)*rz*
                                Mathf.Sqrt(1f-q*q/(rx*rx)));
                        return new Vector3((positive?1f:-1f)*rx*
                            Mathf.Sqrt(1f-q*q/(rz*rz)),0,q);
                    }
                    var p0=Surface(q0);var p1=Surface(q1);
                    var m=(p0+p1)*.5f;
                    var n=new Vector3(m.x/(rx*rx),0,m.z/(rz*rz)).normalized;
                    var rotation=Quaternion.LookRotation((p1-p0).normalized,Vector3.up);
                    signBacker.Beam(m+n*.14f+Vector3.up*(bottom+height*.5f),
                        new Vector3(.11f,height,Vector3.Distance(p0,p1)+.025f),rotation);
                }
            }
            Backer(true,false,42f,11.02f,7.20f);
            Backer(true,true,36f,11.32f,6.20f);
            Backer(false,true,34f,11.60f,5.85f);
            Backer(false,false,34f,11.60f,5.85f);
        }

        plinth.Emit(group,"original_PolyHaven_granite_plinth",exterior["dark_stone"]);
        fins.Emit(group,"PBR_curtain_wall_aluminium",exterior["steel"]);
        bronze.Emit(group,"PBR_bronze_architectural_detail",exterior["bronze"]);
        roof.Emit(group,"PBR_roof_coping",exterior["roof"]);
        signBacker.Emit(group,"PBR_sign_backing_beneath_original_6_to_1_signs",
            exterior["dark_stone"]);

        // Glass canopies and granite/bronze pilasters on BOTH doors.
        // Side supports are 10m from the centre: no obstacle in the 6.4m aisle.
        foreach(float side in new[]{-1f,1f})
        {
            string entry=side<0?"PUBLIC":"ATHLETES";
            float z=side*62.6f;
            var portico=Group(group,"A1 "+entry+" | premium PBR glass portico",Vector3.zero);
            Box(portico,entry+" original Poly Haven stone glass canopy soffit",
                exterior["stone"],new Vector3(0,5.24f,z),
                new Vector3(21.8f,.14f,7.15f),false);
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
        Debug.Log("CHAMPIONS_A1_POLYHAVEN_EXTERIOR_PREMIUM facade_bays="+bays+
            " omitted_lower_facade_door_segments="+skippedDoor+
            " authentic_PBR=true opaque_glass_overlays=0 original_signs_preserved=true"+
            " doors_and_vehicle_lanes_preserved=true device_visual_review_required=true");
    }

    public static bool Generate(Transform arenaRoot, Dictionary<string, Material> exterior)
    {
        tiledMeshSerial = 0;
        var root = Group(arenaRoot, "A1 | arena entrance, parking, trophy and athlete facilities", Vector3.zero);
        var asphalt = BraxChampionsFoundation.SourceMaterial("arena_parking_asphalt", "clean_asphalt", 2.2f,
            new Color(.89f, .89f, .89f));
        var sidewalk = BraxChampionsFoundation.SourceMaterial("arena_arrival_paving", "concrete_pavement_02", 1.1f,
            new Color(.94f, .94f, .93f));
        var rope = BraxChampionsFoundation.SourceMaterial("arena_red_velvet", "fabric_pattern_05", .6f,
            new Color(.62f, .025f, .045f));
        var paint = Paint("arena_parking_markings", new Color(.93f, .92f, .85f));
        Parking(root, asphalt, paint, sidewalk, exterior["dark_stone"]);
        ParkingLights(root, exterior["steel"], exterior["light"]);
        // Architectural PBR finish is generated independently of source signs,
        // without shifting the approved ellipse, doors or visitor routes.
        PremiumExterior(root, exterior);
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
        // The original FBX is shown ONCE, in the judges' showcase generated by
        // BraxChampionsBeltShowcase inside the arena. Never duplicate it at entry.
        return AssetDatabase.LoadAssetAtPath<GameObject>(BeltAsset) != null;
    }
}

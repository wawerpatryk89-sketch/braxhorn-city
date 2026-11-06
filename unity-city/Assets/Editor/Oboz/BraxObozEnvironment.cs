using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Reconstructed authoring source. Generated scene is a work in progress;
// it is not labelled a release and no build or source-branch push is invoked.
public static partial class BraxObozEnvironment
{
    const float K = 1.294f;
    const string Folder = "Assets/PlayableGenerated/Oboz/Environment";
    public const string ScenePath = Folder + "/BraxhornObozEnvironment.unity";
    static readonly List<BraxCityDoor> doors = new List<BraxCityDoor>();
    static readonly List<BraxWorek> bags = new List<BraxWorek>();
    static readonly List<Transform> covered = new List<Transform>();
    static readonly List<BraxObozArena> arenas = new List<BraxObozArena>();
    static Material wood, floor, metal, gravel, ground, burgundy, dark, rope, fabric;
    static Transform root;
    static Vector3 P(float x, float y, float z) => new Vector3(x, y, z) * K;
    static Transform Group(string name, Transform parent, Vector3 local)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = local; return t;
    }
    static GameObject Box(Transform parent, string name, Vector3 at, Vector3 size, Material material, bool solid = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = at * K; g.transform.localScale = size * K;
        g.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>());
        // Clone UVs in metres to avoid stretching grain over a complete wall.
        var mf = g.GetComponent<MeshFilter>(); var mesh = Object.Instantiate(mf.sharedMesh);
        var vertices = mesh.vertices; var normals = mesh.normals; var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = Vector3.Scale(vertices[i], size) * K, n = normals[i];
            uv[i] = Mathf.Abs(n.y) > .9f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > .9f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
        }
        mesh.uv = uv; mesh.RecalculateTangents(); mf.sharedMesh = SaveMesh(mesh);
        return g;
    }
    static GameObject Cylinder(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material, bool solid = false)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = (a + b) * (.5f * K); g.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        g.transform.localScale = new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2) * K;
        g.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>());
        // Match the metre-based box UVs on poles, rails and roof details.
        // Primitive UVs otherwise stretch one whole wood map along every pole.
        var filter=g.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);
        var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
        float length=Vector3.Distance(a,b)*K,circumference=2*Mathf.PI*radius*K;
        for(int i=0;i<vertices.Length;i++)
            uv[i]=Mathf.Abs(normals[i].y)>.9f
                ? new Vector2(vertices[i].x,vertices[i].z)*(2*radius*K)
                : new Vector2(uv[i].x*circumference,(vertices[i].y*.5f+.5f)*length);
        mesh.uv=uv;mesh.RecalculateTangents();filter.sharedMesh=SaveMesh(mesh);
        return g;
    }
    static Mesh SaveMesh(Mesh mesh)
    {
        return BraxObozStableMesh.Save(mesh);
    }
    [MenuItem("BRAXHORN/Obóz/Odtwórz zabudowę roboczą (bez APK)")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zakończ Play Mode");
        BraxObozCharacters.ReadManifest();
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        doors.Clear(); bags.Clear(); covered.Clear(); arenas.Clear();
        wood = BraxObozMaterials.Pbr("wood"); floor = BraxObozMaterials.Pbr("floor"); metal = BraxObozMaterials.Pbr("metal");
        gravel = BraxObozMaterials.Pbr("gravel"); ground = BraxObozMaterials.ForestGround();
        burgundy = BraxObozMaterials.Plain("bordo", new Color(.478f, .118f, .118f));
        dark = BraxObozMaterials.Plain("grafit", new Color(.09f, .10f, .11f), .4f);
        rope = BraxObozMaterials.Plain("liny", new Color(.77f, .69f, .54f), .15f);
        fabric = BraxObozMaterials.Pbr("towel");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            root = Group("Obóz Misia — odbudowa robocza, wymagany odbiór", null, Vector3.zero);
            var info = root.gameObject.AddComponent<BraxObozSceneInfo>();
            CampusMaterials(); OwnerPanorama(); Terrain(); Paths();
            RunningTracks(); // profesjonalny czerwony owal i osobna bieznia 100 m
            // Main activity area retained at real scale. Additional cabins/service areas
            // are reconstructed placements; appearance has not been approved.
            Cabin("Domek 1", -60, -18, -90); Cabin("Domek 2", -60, -36, -90);
            Cabin("Domek 3", -42, -18, 90); Cabin("Domek 4", -42, -36, 90);
            Ring("Ring 1", -23, 15); Octagon(0, -5);
            // Owner rejected the imported city gym. Replace its entire instance,
            // interior, doors and collision source with camp training facilities.
            TrainingCampus();
            CoveredStrengthGym(); // wolne ciezary i silownia pod dachem
            // Total: four pavilion + eight roofed outdoor + twelve open = 24.
            Bags(-23, -40, true,4); Bags(23, -40, false,6);
            ServiceBuildings();
            MuralBoards(); // Po budynkach: murale fizycznie na ścianach
            CampusArrival();
            BraxObozParkedBus.Add(root);
            CampusPerimeter();
            // Strumien lezy ZA ogrodzeniem jako naturalne tlo.
            // Nie stawiamy fikcyjnych mostow wewnatrz zamknietego obozu.
            BraxObozVegetation.Populate(root);
            var anchors = Group("Kotwice planu R3 — do połączenia z oryginalnymi modelami", root, Vector3.zero);
            foreach (var a in new[] { ("Fort 3 na 3",83f,25f), ("Chata leśniczego i sklep",-105f,-103f), ("Tartak",76f,-105f), ("Kojce",39f,-94f) })
                Group(a.Item1, anchors, P(a.Item2, 0, a.Item3));
            var light = new GameObject("Popołudniowe słońce").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(34, -35, 0);
            light.color = new Color(1f, .975f, .94f); light.intensity = .93f; // neutralna skora Moniki, bez zoltego plastiku
            light.shadows = LightShadows.Soft; RenderSettings.sun = light;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .003f;
            RenderSettings.fogColor = new Color(.34f, .46f, .43f);
            info.bags = bags.ToArray(); info.coveredBags = covered.ToArray(); info.cabinDoors = doors.Take(4).ToArray(); info.arenas = arenas.ToArray();
            BraxObozMaterials.ValidateHierarchy(root);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Nie zapisano zabudowy obozu");
            File.WriteAllText(Folder + "/status.json", "{\"completeCamp\":false,\"runtimeVerified\":false,\"cabins\":4,\"bags\":24,\"coveredBags\":12,\"rings\":2,\"octagons\":1}");
            Debug.Log("BRAXHORN_OBOZ zapisano roboczą zabudowę i rozmieszczenie lasu; fort, wnętrza i pełny odbiór nadal nieukończone");
        }
        finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
        AssetDatabase.SaveAssets();
    }
    // Prawdziwa, źródłowa 360 x 180 panorama HDR 4096x2048.
    // Wyłącznie tło świata; brak sztucznych ścian panoramicznych za płotem.
    static void OwnerPanorama()
    {
        const string source="Assets/Source/Prywatne/Oboz/OwnerAssets/Panoramy/montorfano_4k.hdr";
        var panorama=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(!panorama || panorama.width!=4096 || panorama.height!=2048)
            throw new InvalidOperationException("Brak oryginalnej panoramy HDR 360 4096x2048: "+source);
        var shader=Shader.Find("Skybox/Panoramic");
        if(!shader)throw new InvalidOperationException("Brak shadera HDR Skybox/Panoramic");
        var material=new Material(shader){name="Obóz — Montorfano HDR 360 oryginalne 4K"};
        material.SetTexture("_MainTex",panorama);
        material.SetFloat("_Exposure",1f);
        material.SetFloat("_Rotation",0f);
        string path=Folder+"/ObozMontorfano4K.mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing)
        {
            EditorUtility.CopySerialized(material,existing);
            Object.DestroyImmediate(material);
            material=existing;
        }
        else AssetDatabase.CreateAsset(material,path);
        RenderSettings.skybox=material;
        // Światła i shadery postaci zostają bez zmian.
        Debug.Log("BRAXHORN_OBOZ_HDR panorama=montorfano_4k.hdr 360 4096x2048");
    }

    // 4 oryginalne murale: źródłowy JPEG pozostaje nietknięty, bez przycinania.
    // Obraz na Quad ma dokładnie naturalną proporcję z JPEG, w tym liternictwo.
    static void OriginalMural(Transform parent,string key,string filename,Vector3 at,
        Quaternion facing,float width,bool woodenBacking)
    {
        string texturePath="Assets/Source/Prywatne/Oboz/OwnerAssets/Murale/"+filename;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(!texture || texture.width<512 || texture.height<256)
            throw new InvalidOperationException("Brak oryginalnego muralu JPEG lub zła tekstura: "+texturePath);
        float height=width*texture.height/(float)texture.width;
        var board=Group("Oryginalny mural BRAXHORN — "+key,parent,P(at.x,at.y,at.z));
        board.localRotation=facing;
        if(woodenBacking)
        {
            Box(board,"Drewniana ściana pod mural",new Vector3(0,0,-.03f),
                new Vector3(width+.35f,height+.34f,.20f),wood,true);
            foreach(float side in new[]{-1f,1f})
                Box(board,"Stalowy słup od gruntu do szczytu muralu",
                    new Vector3(side*(width*.5f+.08f),(height*.5f-at.y)*.5f,0),
                    new Vector3(.16f,at.y+height*.5f+.15f,.18f),metal,true);
        }
        var shader=Shader.Find("Unlit/Texture");
        if(!shader)throw new InvalidOperationException("Brak shadera oryginalnego muralu");
        var material=new Material(shader){name="BRAXHORN mural 1do1 — "+key,mainTexture=texture,
            enableInstancing=true};
        string path=Folder+"/Mural-"+key+".mat";
        var previous=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(previous)
        {
            EditorUtility.CopySerialized(material,previous);
            Object.DestroyImmediate(material);
            material=previous;
        }
        else AssetDatabase.CreateAsset(material,path);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name="JPEG 1:1 — "+key;
        quad.transform.SetParent(board,false);
        quad.transform.localPosition=P(0,0,woodenBacking ? 0.105f : 0.025f);
        quad.transform.localScale=new Vector3(width*K,height*K,1f);
        quad.GetComponent<MeshRenderer>().sharedMaterial=material;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        Debug.Log("BRAXHORN_OBOZ_MURAL: "+key+" piksele="+texture.width+"x"+texture.height+
            " format 1:1, bez modyfikacji oryginalnego JPEG");
    }

    // Właścicielskie murale są zamontowane na architekturze, a nie
    // na przypadkowych wolnostojących tablicach.
    static void MuralBoards()
    {
        var buildings = root.GetComponentsInChildren<Transform>(true);
        var cabin1 = buildings.FirstOrDefault(t => t.name == "Domek 1");
        var pavilion = buildings.FirstOrDefault(t =>
            t.name == "Pawilon treningowy — otwarty, bez starej siłowni");
        if (!cabin1 || !pavilion)
            throw new InvalidOperationException("Brak budynku pod oryginalny mural BRAXHORN");

        OriginalMural(cabin1,"WOJOWNIKOW",
            "87405F34-A95B-4AD6-8637-A19599AADA61.jpeg",
            new Vector3(0,1.55f,4.17f),Quaternion.identity,4.6f,false);
        // Fizyczna ściana na południowej krawędzi pawilonu: skierowana
        // do Ring 2, bez zasłaniania schodów i wejść.
        OriginalMural(pavilion,"FIGHT_ZONE",
            "35502A48-F7AB-4650-9874-3266881B2DF6.jpeg",
            new Vector3(-20.05f,2.7f,0),Quaternion.Euler(0,90,0),8.8f,true);
    }

    static void Terrain()
    {
        var paths = PathRectangles();
        const int n = 112; var vertices = new Vector3[(n + 1) * (n + 1)]; var uv = new Vector2[vertices.Length]; var tris = new int[n * n * 6];
        var colors = new Color[vertices.Length];
        for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
        {
            float px = -140 + 280f * x / n, pz = -140 + 280f * z / n;
            // Strumień biegnie tuż poza wschodnim ogrodzeniem. Teren pozostaje
            // niezależny od geometrii ogrodzenia i zachowuje ciągłość podłoża.
            float channel = Mathf.Abs(px - 135);
            float y = channel < 3.5f ? -.9f : channel < 5.5f
                ? Mathf.Lerp(-.9f, 0, (channel - 3.5f) / 2) : 0;
            int i = z * (n + 1) + x; vertices[i] = P(px, y, pz); uv[i] = new Vector2(px * K / 3, pz * K / 3);
            float distance = float.PositiveInfinity;
            foreach (var path in paths)
            {
                float dx = Mathf.Max(path.xMin-px, 0, px-path.xMax);
                float dz = Mathf.Max(path.yMin-pz, 0, pz-path.yMax);
                distance = Mathf.Min(distance, Mathf.Sqrt(dx*dx+dz*dz));
            }
            // Source gravel at maintained path shoulders and stream banks,
            // with soft irregular transitions to the original forest litter.
            float noise = Mathf.PerlinNoise(px*.17f+123.4f,pz*.17f+456.7f);
            float shoulder = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,3.2f+noise*1.8f,distance));
            float bank = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.5f,7+noise*2,channel));
            // Managed lawns inside the camp fade into litter towards the forest.
            float meadow = Mathf.Max(Mathf.Abs((px+35)/100),Mathf.Abs((pz+10)/105));
            float lawn = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1.08f+(noise-.5f)*.12f,meadow));
            // Owner's mud_forest_2k: local foot traffic wear and low damp
            // ground near the OUTSIDE stream, not everywhere on the lawn.
            float wearStart=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.8f,2.4f,distance));
            float wearEnd=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(3.8f,7f,distance));
            float trafficMud=wearStart*wearEnd*(.18f+.62f*noise);
            float streamMud=(1f-Mathf.SmoothStep(0f,1f,
                Mathf.InverseLerp(5.5f,11f,channel)))*(.22f+.24f*(1-noise));
            float mud=Mathf.Clamp01((trafficMud+streamMud)*(1f-lawn*.62f));
            colors[i] = new Color(Mathf.Max(shoulder,bank)*(1f-mud*.55f),
                lawn*(1f-mud*.42f),mud,1f);
            if (x < n && z < n) { int t = (z * n + x) * 6; int j = i + n + 1; tris[t] = i; tris[t + 1] = j; tris[t + 2] = i + 1; tris[t + 3] = i + 1; tris[t + 4] = j; tris[t + 5] = j + 1; }
        }
        var mesh = new Mesh { name = "Teren kompaktowy 280 x 280 m; strumien zewnetrzny" }; mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        mesh = SaveMesh(mesh);
        var g = Group("Podłoże — trawnik, ściółka, oryginalne błoto 2K i żwirowe przejścia", root, Vector3.zero).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = BraxObozMaterials.CampGround(ground,gravel); g.AddComponent<MeshCollider>().sharedMesh = mesh;
        // Short sections allow transparent sorting/culling along the stream.
        var water=BraxObozMaterials.Stream();
        for(int section=0;section<14;section++)
        {
            float z=-140+section*20;
            var surface=new Mesh{name="Odcinek strumienia"};
            surface.vertices=new[]{P(132,-.41f,z),P(132,-.41f,z+20),P(138,-.41f,z),P(138,-.41f,z+20)};
            surface.uv=new[]{Vector2.zero,Vector2.up*20,Vector2.right*6,new Vector2(6,20)};
            surface.triangles=new[]{0,1,2,2,1,3};surface.RecalculateNormals();surface.RecalculateTangents();surface.RecalculateBounds();
            var stream=Group("Strumień — płynąca tafla",root,Vector3.zero).gameObject;
            stream.AddComponent<MeshFilter>().sharedMesh=SaveMesh(surface);
            var renderer=stream.AddComponent<MeshRenderer>();renderer.sharedMaterial=water;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // The visible perimeter supplies its own collision. No invisible wall
        // is allowed to seal the new vehicle entrance at the south boundary.
    }
    // Union of paved rectangles: one surface at intersections, flush edges at doors.
    static void Paths()
    {
        PavedPaths(PathRectangles());
    }
    internal static List<Rect> PathRectangles()
    {
        var paths = new List<Rect> {
            // Przystanek i brama: jedyne źródło powierzchni kostki.
            // PavedPaths przytnie te prostokąty po geometrii obu bieżni.
            new Rect(24.6f,-122f,2.8f,49f),   // zatoka / dojście pasażerów
            new Rect(5f,-122.4f,22f,2.8f),    // furtka i plac przed bramą
            new Rect(5.9f,-140f,1.8f,18f),    // piesi od drogi poza obozem
            new Rect(0f,-78.4f,27f,2.8f),    // wiata -> główna alejka
            // Dostęp pieszy od istniejącego ronda i przystanku — poza oktagonem.
            new Rect(-1.2f,-77f,2.4f,54f),
            new Rect(-1.2f,-25f,19.2f,2.4f),
            new Rect(16.8f,-24f,2.4f,42f),
            new Rect(-1.2f,15.6f,19.2f,2.4f),
            new Rect(-1.2f,16f,2.4f,41f),
            // Dziedziniec domków: wejścia naprzeciwko siebie, nie przez elewacje.
            // Lewa kolumna x=-60 i prawa x=-42; obrócone drzwi na środek.
            new Rect(-51.5f,-41f,2.5f,28f),
            new Rect(-56.5f,-19.2f,11.5f,2.4f),
            new Rect(-56.5f,-37.2f,11.5f,2.4f),
            new Rect(-50.5f,-28f,50.5f,2.4f),
            // Sanitariaty, kuchnia, medyczny i magazyn.
            new Rect(-68f,-61f,68f,2.4f),
            new Rect(-33f,-78f,33f,2.4f),
            // Podejście do strefy siłowej i odcinka 100 m.
            new Rect(18f,-4.5f,28f,2.4f),
            new Rect(44f,-4.5f,2.4f,49f),
            new Rect(0f,39f,18f,2.4f)
        };
        // Łączniki do drzwi usługowych, układ bez prowadzenia pieszych
        // przez modele kuchni, łaźni i stołówki.
        paths.Add(new Rect(-66f,-59.2f,2.4f,11.2f)); // wejście WC od strony północnej
        paths.Add(new Rect(-42f,-65f,2.4f,6.4f));   // dojście do stołówki
        paths.Add(new Rect(-58f,-65.6f,2.4f,7f));   // dojście do pryszniców
        paths.Add(new Rect(-25f,-80.5f,2.4f,5f));   // medyczny
        paths.Add(new Rect(-25f,-65f,2.4f,7f));     // magazyn
        return paths;
    }
    static void Cabin(string name, float x, float z, float yaw)
    {
        var t = Group(name, root, P(x,0,z)); t.localRotation = Quaternion.Euler(0,yaw,0);
        Box(t,"Podłoga z desek",new Vector3(0,.08f,0),new Vector3(6.2f,.16f,8.2f),floor);
        for(int row=0;row<14;row++)
        {
            float y=.25f+row*.19f;
            Box(t,"Deska tylnej elewacji",new Vector3(0,y,4),new Vector3(6.2f,.20f,.14f),wood,false);
            foreach(float side in new[]{-1f,1f})
            {
                // The window is a real opening; no invisible solid wall behind it.
                if(y>1.05f&&y<2.02f){foreach(float end in new[]{-1f,1f})Box(t,"Deska przy oknie",new Vector3(side*3,y,end*2.6f),new Vector3(.14f,.20f,3),wood,false);}
                else Box(t,"Deska bocznej elewacji",new Vector3(side*3,y,0),new Vector3(.14f,.20f,8.2f),wood,false);
                Box(t,"Deska przy wejściu",new Vector3(side*1.89f,y,-4),new Vector3(2.42f,.20f,.14f),wood,false);
            }
            if(y>2.38f)Box(t,"Deska nad wejściem",new Vector3(0,y,-4),new Vector3(1.36f,.20f,.14f),wood,false);
        }
        CabinFinish(t);
        if(name=="Domek 3")
            OriginalMural(t,"DROGA_MISTRZA","6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg",
                new Vector3(0,1.5f,4.14f),Quaternion.identity,4.1f,false);
        // Simple colliders follow the actual openings; decorative log cylinders do not each add physics work.
        foreach(float side in new[]{-1f,1f})
        {
            ColliderBox(t,new Vector3(side*1.85f,1.45f,-4),new Vector3(2.3f,2.7f,.22f));
            ColliderBox(t,new Vector3(side*3,.55f,0),new Vector3(.22f,.95f,8));
            ColliderBox(t,new Vector3(side*3,2.43f,0),new Vector3(.22f,.7f,8));
            foreach(float end in new[]{-1f,1f})ColliderBox(t,new Vector3(side*3,1.55f,end*2.55f),new Vector3(.22f,1.05f,2.9f));
        }
        ColliderBox(t,new Vector3(0,1.45f,4),new Vector3(6,2.7f,.22f)); ColliderBox(t,new Vector3(0,2.6f,-4),new Vector3(1.4f,.4f,.22f));
        foreach(float side in new[]{-1f,1f})
        {

            for(int k=0;k<4;k++)Box(t,"Łóżko — noga",new Vector3(side*2.05f+(k%2-.5f)*.72f,.32f,1.6f+(k/2-.5f)*1.8f),new Vector3(.09f,.5f,.09f),wood);
            Box(t,"Łóżko — rama",new Vector3(side*2.05f,.48f,1.6f),new Vector3(1.05f,.15f,2.1f),wood);
            Box(t,"Materac z tkaniną",new Vector3(side*2.05f,.61f,1.6f),new Vector3(.99f,.16f,2.02f),fabric);
            Box(t,"Poduszka",new Vector3(side*2.05f,.75f,2.32f),new Vector3(.73f,.12f,.42f),fabric,false);
            Box(t,"Złożony koc",new Vector3(side*2.05f,.715f,.95f),new Vector3(.98f,.05f,.65f),burgundy,false);
            Box(t,"Wezgłowie",new Vector3(side*2.05f,.64f,2.68f),new Vector3(1.05f,.48f,.08f),wood,false);
            var bed=Group("Połóż się · "+name,t,P(side*2.05f,0,1.6f)).gameObject.AddComponent<BraxSeat>();
            bed.label=name+" · łóżko";bed.lie=true;bed.surfaceY=.69f*K;bed.hipsAboveRoot=.9f*K;
        }
        // Osiem prawdziwych wojskowych szafek 3D, ze współdzieloną geometrią.
        // Nie dotykaj oryginalnych łóżek/postaci ani tekstur źródłowych.
        foreach(float side in new[]{-1f,1f})
            BraxObozServices.OwnerFixture(t,"metal_cabinet_low_poly.glb",
                new Vector3(side*2.2f,.16f,-2.4f),
                new Vector3(1.15f,2.05f,.78f));
        Box(t,"Stół",new Vector3(1.9f,.8f,-1.7f),new Vector3(1.1f,.08f,.7f),wood);
        foreach(float dx in new[]{-.46f,.46f})foreach(float dz in new[]{-.26f,.26f})Box(t,"Noga stołu",new Vector3(1.9f+dx,.43f,-1.7f+dz),new Vector3(.07f,.7f,.07f),wood);
        CombineDecoration(t); // Static shell only; keep the door leaf movable.
        // Nie zostawiaj pionowej szpary na nadprozu ani pod skrzydlem.
        Door(t,name+" — drzwi",new Vector3(-.63f,.17f,-4.05f),1.26f,2.29f);
        Box(t,"Próg szczelny pod skrzydłem",new Vector3(0,.105f,-4.06f),new Vector3(1.35f,.14f,.26f),metal,false);
        Box(t,"Stopień wejścia",new Vector3(0,.05f,-4.5f),new Vector3(1.6f,.1f,.8f),wood);
    }
    static void ColliderBox(Transform parent,Vector3 pos,Vector3 size){var g=Group("Kolizja zgodna z otworem",parent,pos*K).gameObject;g.AddComponent<BoxCollider>().size=size*K;}
    static void Door(Transform parent,string name,Vector3 hinge,float width,float height)
    {
        var pivot=Group(name,parent,hinge*K);Box(pivot,"Skrzydło drewniane",new Vector3(width*.5f,height*.5f,0),new Vector3(width,height,.065f),wood);
        Cylinder(pivot,"Klamka",new Vector3(width-.14f,1.02f,-.06f),new Vector3(width-.28f,1.02f,-.06f),.018f,metal);
        var d=pivot.gameObject.AddComponent<BraxCityDoor>();d.id=name;d.label=name;d.leaves=new[]{pivot};d.sides=new[]{1f};doors.Add(d);
    }
    static void Ring(string name,float x,float z)
    {
        var t=Group(name,root,P(x,0,z));
        // Ring nie stoi na chodniku: osobna elastyczna posadzka i krawedz sportowa.
        Box(t,"Czarna strefa bezpieczenstwa",new Vector3(0,.07f,0),new Vector3(13,.14f,13),dark,false);
        foreach(float borderZ in new[]{-6.3f,6.3f})
            Box(t,"Granica strefy ringu",new Vector3(0,.147f,borderZ),new Vector3(13,.015f,.11f),burgundy,false);
        var platform=Box(t,"Podest drewniany",new Vector3(0,.24f,0),new Vector3(8.6f,.48f,8.6f),wood);
        Box(t,"Mata bez cudzych logo",new Vector3(0,.50f,0),new Vector3(8.2f,.04f,8.2f),burgundy);
        foreach(float dx in new[]{-4f,4f})foreach(float dz in new[]{-4f,4f}) {
            Cylinder(t,"Słupek ringu",new Vector3(dx,.48f,dz),new Vector3(dx,2.15f,dz),.09f,metal,true);
            Cylinder(t,"Ochraniacz narożnika",new Vector3(dx*.97f,.72f,dz*.97f),new Vector3(dx*.97f,2.10f,dz*.97f),.16f,burgundy);
        }
        foreach(float side in new[]{-1f,1f}) {
            Box(t,"Fartuch ringu",new Vector3(side*4.32f,.26f,0),new Vector3(.025f,.42f,8.6f),burgundy,false);
            Box(t,"Fartuch ringu",new Vector3(0,.26f,side*4.32f),new Vector3(8.6f,.42f,.025f),burgundy,false);
        }
        // A physical entrance gap stays open; the other sides use shared rope physics.
        DynamicRopes(x,z,new Vector3(-4,0,-4),new Vector3(-4,0,4),Vector3.left);
        DynamicRopes(x,z,new Vector3(4,0,-4),new Vector3(4,0,4),Vector3.right);
        DynamicRopes(x,z,new Vector3(-4,0,4),new Vector3(4,0,4),Vector3.forward);
        DynamicRopes(x,z,new Vector3(-4,0,-4),new Vector3(-.7f,0,-4),Vector3.back);
        DynamicRopes(x,z,new Vector3(.7f,0,-4),new Vector3(4,0,-4),Vector3.back);
        ColliderBox(t,new Vector3(-4,1.3f,0),new Vector3(.1f,1.6f,8));ColliderBox(t,new Vector3(4,1.3f,0),new Vector3(.1f,1.6f,8));
        ColliderBox(t,new Vector3(0,1.3f,4),new Vector3(8,1.6f,.1f));
        ColliderBox(t,new Vector3(-2.35f,1.3f,-4),new Vector3(3.3f,1.6f,.1f));ColliderBox(t,new Vector3(2.35f,1.3f,-4),new Vector3(3.3f,1.6f,.1f));
        // Keep the first two treads and the front edge of the top tread unchanged.
        // The old top tread ended at -4.425, leaving a 0.125*K gap before
        // the platform edge (-4.3). Extend its back into the platform so
        // navigation cannot see a drop to the pavilion floor between them.
        for(int i=0;i<3;i++)Box(t,"Schodek",
            new Vector3(0,.08f*(i+1),i==2?-4.45f:-5.2f+i*.3f),
            new Vector3(1.4f,.16f*(i+1),i==2?.65f:.35f),wood);
        var aComp=t.gameObject.AddComponent<BraxObozArena>();aComp.label=name;aComp.halfSize=new Vector2(3.9f,3.9f)*K;arenas.Add(aComp);
    }
    static void DynamicRopes(float x,float z,Vector3 a,Vector3 b,Vector3 outward)
    {
        var g=Group("Liny ringu — wspólna fizyka",root,Vector3.zero).gameObject;
        g.AddComponent<MeshRenderer>().sharedMaterial=rope;
        var line=g.AddComponent<BraxLina>();line.a=(a+new Vector3(x,0,z))*K;line.b=(b+new Vector3(x,0,z))*K;
        line.wysokosci=new[]{.9f*K,1.27f*K,1.64f*K,2.01f*K};line.promien=.025f*K;line.naZewnatrz=outward;
        line.Zbuduj();var mf=g.GetComponent<MeshFilter>();mf.sharedMesh=SaveMesh(UnityEngine.Object.Instantiate(mf.sharedMesh));
    }
    static void Octagon(float x,float z)
    {
        var t=Group("Oktagon",root,P(x,0,z));const float radius=5.3f;
        Box(t,"Odrębna czarna mata bezpieczeństwa",new Vector3(0,.07f,0),new Vector3(13,.14f,13),dark,false);
        var mesh=new Mesh();var v=new Vector3[9];var uv=new Vector2[9];var tris=new int[24];v[0]=P(0,.35f,0);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;v[i+1]=P(Mathf.Sin(a)*radius,.35f,Mathf.Cos(a)*radius);uv[i+1]=new Vector2(v[i+1].x,v[i+1].z);tris[i*3]=0;tris[i*3+1]=i+1;tris[i*3+2]=(i+1)%8+1;}
        mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        mesh=SaveMesh(mesh);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=burgundy;t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;var p=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);var q=new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius);
            var fascia=Box(t,"Zamknięta krawędź podestu",(p+q)*.5f+Vector3.up*.175f,new Vector3(.08f,.35f,Vector3.Distance(p,q)),dark,false);
            fascia.transform.localRotation=Quaternion.LookRotation(q-p);
            Cylinder(t,"Słupek klatki",p+Vector3.up*.35f,p+Vector3.up*2.4f,.065f,dark,true);
            if(i==3)continue; // entrance panel remains open in the reconstruction
            Cylinder(t,"Górny profil klatki",p+Vector3.up*2.35f,q+Vector3.up*2.35f,.04f,dark);
            Cylinder(t,"Dolny profil klatki",p+Vector3.up*.4f,q+Vector3.up*.4f,.04f,dark);
            CagePanel(t,p,q);
            Cylinder(t,"Miękka osłona słupka",p+Vector3.up*.52f,p+Vector3.up*2.3f,.105f,burgundy);
            var mid=(p+q)*.5f;var col=Group("Kolizja klatki",t,(mid+Vector3.up*1.3f)*K);col.localRotation=Quaternion.LookRotation(q-p);col.gameObject.AddComponent<BoxCollider>().size=new Vector3(.08f,2.1f,Vector3.Distance(p,q))*K;
        }
        // Approach the existing open panel; the top tread overlaps the mat edge.
        float entryAngle=3.5f*Mathf.PI/4;
        var entry=Group("Schody wejścia do oktagonu",t,Vector3.zero);
        entry.localRotation=Quaternion.Euler(0,entryAngle*Mathf.Rad2Deg,0);
        float edge=radius*Mathf.Cos(Mathf.PI/8);
        for(int i=0;i<3;i++)Box(entry,"Stopień antypoślizgowy",new Vector3(0,(i+1)*.35f/6,edge+.7f-i*.3f),new Vector3(1.4f,(i+1)*.35f/3,.42f),dark);
        CombineDecoration(t);
        var arena=t.gameObject.AddComponent<BraxObozArena>();arena.label="OKTAGON";arena.halfSize=new Vector2(4.8f,4.8f)*K;arenas.Add(arena);
    }
    // Oryginalny model 2K od właściciela (57 części, zawieszenie w GLB).
    // Wszystkie 24 instancje współdzielą GLB, mesh, materiały i tekstury.
    // Brak prywatnego źródła jest błędem, nie usprawiedliwia atrap.
    static GameObject OwnerBagPrototype(int index)
    {
        const string rootPath="Assets/Source/Prywatne/Oboz/OwnerAssets/";
        const string leather="leather_punching_bag.glb";
        const string vintage="vintage_old_punching_bag.glb";
        var first=AssetDatabase.LoadAssetAtPath<GameObject>(rootPath+leather);
        if(!first)throw new InvalidOperationException("Brak właścicielskiego worka 2K: "+
            rootPath+leather+"; wstrzymano import zamiast tworzyć atrapę.");
        var second=AssetDatabase.LoadAssetAtPath<GameObject>(rootPath+vintage);
        if(second && index%2==1)return second;
        if(!second && index==0)Debug.LogWarning(
            "BRAXHORN_OBOZ_BAG_VARIANT_PENDING: drugi rodzaj worka nie ma jeszcze "+
            "potwierdzonego pliku GLB; wszystkie instancje otrzymają oryginalny Leather Punching Bag.");
        return first;
    }

    static void OwnerBagMesh(Transform pivot,GameObject original)
    {
        var model=PrefabUtility.InstantiatePrefab(original) as GameObject;
        if(!model)throw new InvalidOperationException("Nie udało się utworzyć skórzanego worka GLB");
        model.name="Oryginalny worek skórzany — mesh/UV/PBR bez zmian";
        model.transform.SetParent(pivot,false);
        model.transform.localPosition=Vector3.zero;
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Worek 3D nie zawiera renderera");
        if(renderers.Any(x=>x.sharedMaterials.Any(m=>!m)))
            throw new InvalidOperationException("Worek 3D ma brakujące materiały");
        Bounds bound=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))bound.Encapsulate(renderer.bounds);
        if(bound.size.y<=.001f)throw new InvalidOperationException("Worek ma niepoprawną wysokość");
        // Źródło ma cały korpus i łańcuch. 2.1*K = wysokość całego zespołu,
        // pasuje do istniejącego collidera korpusu 1.5*K i haka przy 3.25*K.
        float targetHeight=2.1f*K;
        model.transform.localScale*=targetHeight/bound.size.y;
        bound=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))bound.Encapsulate(renderer.bounds);
        model.transform.position+=new Vector3(pivot.position.x-bound.center.x,
            pivot.position.y-2.2f*K-bound.min.y,
            pivot.position.z-bound.center.z);
        // Nie wolno wymieniać materiałów ani zmniejszać tekstur.
    }

    static void Bags(float x,float z,bool roof,int columns)
    {
        var t=Group((columns*2)+(roof?" worków zadaszonych":" worków otwartych"),root,P(x,0,z));
        foreach(float side in new[]{-1f,1f})
        {
            Cylinder(t,"Belka nośna",new Vector3(-7,3.4f,side*2),new Vector3(7,3.4f,side*2),.12f,wood);
            foreach(float end in new[]{-7f,7f})Cylinder(t,"Słup",new Vector3(end,0,side*2),new Vector3(end,3.4f,side*2),.13f,wood,true);
        }
        if(roof)MetalRoof(t,15,6,3.63f,.65f);
        for(int row=0;row<2;row++)for(int col=0;col<columns;col++)
        {
            var pivot=Group("Worek "+(bags.Count+1),t,P(-(columns-1)*1.1f+col*2.2f,3.25f,row==0?-2:2));
            // Wyłącznie oryginalny model; GLB już zawiera skórę i łańcuchy.
            // Nie dubluj elementów proceduralnymi cylindrami ani UV.
            OwnerBagMesh(pivot,OwnerBagPrototype(bags.Count));
            var collider=pivot.gameObject.AddComponent<CapsuleCollider>();collider.center=P(0,-1.45f,0);collider.height=1.5f*K;collider.radius=.28f*K;
            var bag=pivot.gameObject.AddComponent<BraxWorek>();bag.gora=.7f*K;bag.dol=2.2f*K;bag.dlugosc=1.45f*K;bag.promien=.28f*K;bags.Add(bag);if(roof)covered.Add(pivot);
        }
    }
    // One mesh per panel/suspension, rather than a GameObject per wire/link.
    sealed class DetailTubes
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int> indices=new List<int>();
        public void Segment(Vector3 a,Vector3 b,float radius,int sides=6)
        {
            Vector3 axis=(b-a).normalized;
            if((b-a).sqrMagnitude<.000001f)return;
            Vector3 u=Vector3.Cross(axis,Mathf.Abs(axis.y)<.9f?Vector3.up:Vector3.right).normalized;
            Vector3 v=Vector3.Cross(axis,u);
            int start=vertices.Count;
            for(int end=0;end<2;end++)for(int j=0;j<=sides;j++) {
                float angle=2*Mathf.PI*j/sides;
                vertices.Add(((end==0?a:b)+radius*(Mathf.Cos(angle)*u+Mathf.Sin(angle)*v))*K);
                uv.Add(new Vector2(j/(float)sides*2*Mathf.PI*radius*K,end*Vector3.Distance(a,b)*K));
            }
            for(int j=0;j<sides;j++) {
                int a0=start+j,b0=a0+sides+1;
                indices.Add(a0);indices.Add(a0+1);indices.Add(b0);
                indices.Add(a0+1);indices.Add(b0+1);indices.Add(b0);
            }
        }
        public void Save(Transform parent,string name,Material material)
        {
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var g=Group(name,parent,Vector3.zero).gameObject;
            g.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
            g.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
    static void CagePanel(Transform parent,Vector3 a,Vector3 b)
    {
        float length=Vector3.Distance(a,b),height=1.9f;
        Vector3 along=(b-a).normalized;
        var wire=new DetailTubes();
        // Diagonal chain-link wires clipped to the real panel rectangle.
        foreach(float slope in new[]{-1f,1f})
        for(float c=-length;c<=height+length;c+=.12f) {
            float lo=Mathf.Max(0,slope>0?-c:c-height);
            float hi=Mathf.Min(length,slope>0?height-c:c);
            if(hi-lo<.001f)continue;
            float offset=slope*.003f;
            Vector3 normal=Vector3.Cross(along,Vector3.up)*offset;
            wire.Segment(a+along*lo+Vector3.up*(.4f+slope*lo+c)+normal,
                a+along*hi+Vector3.up*(.4f+slope*hi+c)+normal,.0025f);
        }
        wire.Save(parent,"Siatka rombowa ocynkowana",metal);
    }
    static void BagSuspension(Transform parent)
    {
        var chain=new DetailTubes();
        // Alternating closed oval links, with a four-point bridle to the bag.
        for(int link=0;link<8;link++) {
            Vector3 center=new Vector3(0,-.025f-link*.043f,0);
            Vector3 lateral=link%2==0?Vector3.right:Vector3.forward;
            for(int j=0;j<12;j++) {
                float a=j*Mathf.PI/6,b=(j+1)*Mathf.PI/6;
                chain.Segment(center+lateral*(.018f*Mathf.Cos(a))+Vector3.up*(.029f*Mathf.Sin(a)),
                    center+lateral*(.018f*Mathf.Cos(b))+Vector3.up*(.029f*Mathf.Sin(b)),.004f);
            }
        }
        for(int arm=0;arm<4;arm++) {
            float angle=arm*Mathf.PI/2;
            chain.Segment(new Vector3(0,-.35f,0),new Vector3(.23f*Mathf.Cos(angle),-.72f,.23f*Mathf.Sin(angle)),.012f);
        }
        chain.Save(parent,"Ogniwa i czteropunktowe zawieszenie",metal);
    }
    static void ServiceBuildings()
    {
        BraxObozServices.AddAll(root);
    }
    static void Bridge(float x,float z)
    {
        var t=Group("Kładka",root,P(x,0,z));
        for(int i=0;i<30;i++)Box(t,"Deska pomostu",new Vector3(-5.8f+i*.4f,.08f,0),new Vector3(.38f,.16f,3.8f),wood);
        foreach(float side in new[]{-1.8f,1.8f})
        {
            Cylinder(t,"Poręcz",new Vector3(-6,1.15f,side),new Vector3(6,1.15f,side),.045f,wood);
            for(int i=0;i<7;i++)Cylinder(t,"Słupek kładki",new Vector3(-6+i*2,0,side),new Vector3(-6+i*2,1.2f,side),.055f,wood,true);
        }
    }

}


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
    static Material wood, bark, floor, metal, gravel, ground, burgundy, dark, rope, fabric;
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

    // Tylko sekcje opcjonalne; grunt podstawowy, postać i zapis sceny krytyczne.
    // Zniszcz pół-utworzony obiekt, usuń jego rejestracje, zachowaj wcześniejsze.
    static void Optional(string label,Action action)
    {
        // Snapshot CAŁEJ istniejącej hierarchii. Mural lub wyposażenie
        // może dopinać nowe obiekty POD już istniejącym domkiem.
        var before=new HashSet<int>(root.GetComponentsInChildren<Transform>(true)
            .Select(x=>x.gameObject.GetInstanceID()));
        int d=doors.Count,b=bags.Count,c=covered.Count,a=arenas.Count;
        try { action(); }
        catch(Exception ex)
        {
            // Najwyższe nowe korzenie kasują również swoje nowe dzieci.
            // Istniejących wcześniej renderów/murali/obiektów NIE usuwaj.
            var added=root.GetComponentsInChildren<Transform>(true)
                .Where(x=>x!=root && !before.Contains(x.gameObject.GetInstanceID()) &&
                    x.parent && before.Contains(x.parent.gameObject.GetInstanceID()))
                .ToArray();
            foreach(var child in added)
                if(child)Object.DestroyImmediate(child.gameObject);
            if(doors.Count>d)doors.RemoveRange(d,doors.Count-d);
            if(bags.Count>b)bags.RemoveRange(b,bags.Count-b);
            if(covered.Count>c)covered.RemoveRange(c,covered.Count-c);
            if(arenas.Count>a)arenas.RemoveRange(a,arenas.Count-a);
            BraxBuildQuality.Warn("OBÓZ OPCJONALNE POMINIĘTE "+label+
                " — "+ex.ToString());
        }
    }
    [MenuItem("BRAXHORN/Obóz/Odtwórz zabudowę roboczą (bez APK)")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zakończ Play Mode");
        BraxObozCharacters.ReadManifest();
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        doors.Clear(); bags.Clear(); covered.Clear(); arenas.Clear();
        wood = BraxObozMaterials.Pbr("wood");
        bark = BraxObozMaterials.PublicPbr("bark_brown_01",1f); // naturalna kora 2K CC0
        floor = BraxObozMaterials.Pbr("floor"); metal = BraxObozMaterials.Pbr("metal");
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
            CampusMaterials();
            Optional("panorama HDR",()=>{ OwnerPanorama(); });
            Terrain(); // grunt jest kluczowy, brak terenu nadal zatrzymuje generator
            Optional("leśne ścieżki",()=>{ Paths(); });
            Optional("bieżnie",()=>{ RunningTracks(); });
            // Main activity area retained at real scale. Additional cabins/service areas
            // are reconstructed placements; appearance has not been approved.
            Optional("Domek 1",()=>{ Cabin("Domek 1", -60, -18, -90); });
            Optional("Domek 2",()=>{ Cabin("Domek 2", -60, -36, -90); });
            Optional("Domek 3",()=>{ Cabin("Domek 3", -42, -18, 90); });
            Optional("Domek 4",()=>{ Cabin("Domek 4", -42, -36, 90); });
            Optional("Ring 1",()=>{ Ring("Ring 1", -23, 15); });
            Optional("Oktagon",()=>{ Octagon(0, -5); });
            // Owner rejected the imported city gym. Replace its entire instance,
            // interior, doors and collision source with camp training facilities.
            Optional("pawilon z Ringiem 2",()=>{ TrainingCampus(); });
            Optional("siłownia pod dachem",()=>{ CoveredStrengthGym(); });
            // 48: dwadzieścia zadaszonych przy belkach, cztery w pawilonie
            // i dwadzieścia cztery otwarte — tylko prawdziwe modele.
            Optional("20 worków zadaszonych",()=>{ Bags(-23, -40, true,10); });
            Optional("24 worki otwarte",()=>{ Bags(23, -40, false,12); });
            Optional("budynki usługowe",()=>{ ServiceBuildings(); });
            Optional("ciągłe dojścia do wejść",()=>{ EntranceApproaches(); });
            Optional("ognisko",()=>{ CampfireFromOriginalRocks(); });
            Optional("plac apelowy",()=>{ AssemblyGroundWithSchedule(); });
            Optional("oryginalne murale",()=>{ MuralBoards(); // Po budynkach: murale fizycznie na ścianach
            });
            Optional("strefa przyjazdu",()=>{ CampusArrival(); });
            Optional("autobus właściciela",()=>{ BraxObozParkedBus.Add(root); });
            Optional("ogrodzenie i brama",()=>{ CampusPerimeter(); });
            // Strumien lezy ZA ogrodzeniem jako naturalne tlo.
            // Nie stawiamy fikcyjnych mostow wewnatrz zamknietego obozu.
            Optional("las i murawa",()=>{ BraxObozVegetation.Populate(root); });
            var anchors = Group("Kotwice planu R3 — do połączenia z oryginalnymi modelami", root, Vector3.zero);
            foreach (var a in new[] { ("Fort 3 na 3",83f,25f), ("Chata leśniczego i sklep",-105f,-103f), ("Tartak",76f,-105f), ("Kojce",39f,-94f) })
                Group(a.Item1, anchors, P(a.Item2, 0, a.Item3));
            var light = new GameObject("Popołudniowe słońce").AddComponent<Light>(); light.type = LightType.Directional;
            BraxObozAtmosphere.Apply(root,light);
            Optional("wypalenie odbić luster łaźni",()=>{
                BraxObozServices.BakeBathroomReflections(root);
            });
            info.bags = bags.ToArray(); info.coveredBags = covered.ToArray(); info.cabinDoors = doors.Take(4).ToArray(); info.arenas = arenas.ToArray();
            try { BraxObozMaterials.ValidateHierarchy(root); }
            catch(Exception ex) { BraxBuildQuality.Warn(
                "OBÓZ PBR DO KONTROLI: "+ex.ToString()); }
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Nie zapisano zabudowy obozu");
            // Stan jest INWENTARZEM wyniku sceny, nie wpisaną na sztywno obietnicą.
            File.WriteAllText(Folder+"/status.json",
                "{\"completeCamp\":false,\"runtimeVerified\":false,"+
                "\"cabins\":"+doors.Count+",\"bags\":"+bags.Count+
                ",\"coveredBags\":"+covered.Count+
                ",\"rings\":"+arenas.Count(r=>r && r.label.StartsWith("Ring",StringComparison.Ordinal))+
                ",\"octagons\":"+arenas.Count(r=>r && r.label=="OKTAGON")+"}");
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
        var importer=AssetImporter.GetAtPath(source) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Brak oryginalnej panoramy HDR: "+source);
        if(importer.textureType!=TextureImporterType.Default ||
            importer.textureShape!=TextureImporterShape.Texture2D ||
            importer.npotScale!=TextureImporterNPOTScale.None)
        {
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(source,ImportAssetOptions.ForceSynchronousImport);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth!=4096 || sourceHeight!=2048)
            BraxBuildQuality.Warn("OBÓZ PANORAMA — źródło Montorfano ma "+
                sourceWidth+"x"+sourceHeight+", oczekiwano 4096x2048");
        var panorama=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(!panorama)throw new InvalidOperationException("Nie wczytano panoramy HDR: "+source);
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
        var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if(!importer)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_MURAL_MISSING "+texturePath+
                " — raport zamiast zatrzymania generowania sceny");
            return;
        }
        // An artwork must stay a 2D image with its native aspect ratio. Configure
        // the importer before loading, including non-power-of-two source art.
        if(importer.textureType!=TextureImporterType.Default ||
            importer.textureShape!=TextureImporterShape.Texture2D ||
            importer.npotScale!=TextureImporterNPOTScale.None)
        {
            importer.textureType=TextureImporterType.Default;
            importer.textureShape=TextureImporterShape.Texture2D;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
        if(sourceWidth<4096)
            Debug.LogWarning("BRAXHORN_OBOZ_MURAL_LOW_SOURCE_RESOLUTION "+
                texturePath+" szerokosc="+sourceWidth+" — wymagane min 4096 px");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if(!texture || sourceWidth<=0 || sourceHeight<=0)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_MURAL_MISSING "+texturePath);
            return;
        }
        // Artwork size may be chosen for the available wall, but it is never
        // stretched: one physical width and the original source proportion.
        float aspect=sourceWidth/(float)sourceHeight;
        width=Mathf.Min(width,(woodenBacking?3.8f:2.35f)*aspect);
        float height=width/aspect;
        var board=Group("Oryginalny mural BRAXHORN — "+key,parent,P(at.x,at.y,at.z));
        board.localRotation=facing;
        if(woodenBacking)
        {
            // A genuine part of the building, sibling of the painting. Its
            // collider must not be excluded with the painting's own hierarchy.
            var wall=Group("Drewniana ściana budynku — mural "+key,parent,P(at.x,at.y,at.z));
            wall.localRotation=facing;
            float wallHeight=parent.name.Contains("SIŁOWNIA")?5.1f:5.45f;
            float wallWidth=width+.35f;
            int planks=Mathf.CeilToInt(wallWidth/.18f);
            for(int i=0;i<planks;i++)
                Box(wall,"Deska ściany 18 cm",
                    new Vector3(-wallWidth*.5f+(i+.5f)*wallWidth/planks,wallHeight*.5f-at.y,-.06f),
                    new Vector3(wallWidth/planks-.004f,wallHeight,.12f),wood,false);
            ColliderBox(wall,new Vector3(0,wallHeight*.5f-at.y,-.06f),
                new Vector3(wallWidth,wallHeight,.12f));
            foreach(float side in new[]{-1f,1f})
                Cylinder(wall,"Słup z bali przy ścianie budynku",
                    new Vector3(side*(wallWidth*.5f+.12f),-at.y,-.06f),
                    new Vector3(side*(wallWidth*.5f+.12f),wallHeight-at.y,-.06f),
                    .16f,wood,true);
            Cylinder(wall,"Oczep ściany budynku",
                new Vector3(-wallWidth*.5f-.12f,wallHeight-at.y,-.06f),
                new Vector3(wallWidth*.5f+.12f,wallHeight-at.y,-.06f),.16f,wood,false);
            CombineDecoration(wall);
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
        // Unity's primitive Quad faces local -Z. Turn its visible face toward
        // the board's outward +Z; this also keeps the lettering readable.
        quad.transform.localRotation=Quaternion.Euler(0,180,0);
        quad.transform.localScale=new Vector3(width*K,height*K,1f);
        quad.GetComponent<MeshRenderer>().sharedMaterial=material;
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        // Raycast poziomy przez najbliższą, faktyczną ścianę budynku.
        // Sztuczny tylny wspornik nie udaje znalezionej architektury.
        Physics.SyncTransforms();
        var origin=board.position+board.forward*(.35f*K);
        var towardWall=-board.forward;
        var hits=Physics.RaycastAll(origin,towardWall,1.25f*K,
            ~0,QueryTriggerInteraction.Ignore)
            .Where(h=>h.collider && !h.collider.transform.IsChildOf(board) &&
                      Vector3.Dot(h.normal,board.forward)>.55f)
            .OrderBy(h=>h.distance).ToArray();
        if(hits.Length>0)
            BraxObozPlacement.ToWall(board,quad.GetComponentsInChildren<Renderer>(),
                board.position,towardWall,"mural "+key);
        else
            Debug.LogWarning("BRAXHORN_OBOZ_MURAL_NO_WALL "+key+
                " — geometria wymaga kontroli montażu, nie oznaczaj jako gotowe");
        var placement=BraxObozPlacementRecord.For(board);
        placement.SetMembers(quad.GetComponentsInChildren<Renderer>(true));
        placement.support=BraxObozPlacementRecord.SupportKind.Wall;
        placement.wallDirection=towardWall;placement.label="mural "+key;
        placement.expectedDimensionMetres=width;
        placement.dimensionAxis=(int)BraxObozPlacement.Axis.X;placement.dimensionFrame=board;
        Debug.Log("BRAXHORN_OBOZ_MURAL: "+key+" piksele="+texture.width+"x"+texture.height+
            " format 1:1, bez modyfikacji oryginalnego JPEG");
    }

    // Właścicielskie murale są zamontowane na architekturze, a nie
    // na przypadkowych wolnostojących tablicach.
    static void MuralBoards()
    {
        var buildings = root.GetComponentsInChildren<Transform>(true);
        var cabin3 = buildings.FirstOrDefault(t => t.name == "Domek 3");
        var pavilion = buildings.FirstOrDefault(t =>
            t.name == "Pawilon treningowy — otwarty, bez starej siłowni");
        var gym=buildings.FirstOrDefault(t=>t.name=="BRAXHORN · SIŁOWNIA 3D WŁAŚCICIELA");
        // Each mural is independent; a missing optional building does not
        // erase murals successfully installed on the other buildings.
        if(cabin3)Optional("mural DROGA MISTRZA",()=>OriginalMural(cabin3,"DROGA_MISTRZA",
            "6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg",
            new Vector3(0,1.5f,4.14f),Quaternion.identity,4.1f,false));
        else BraxBuildQuality.Warn("OBÓZ MURAL DROGA MISTRZA — brak Domku 3");
        // Fizyczna ściana na południowej krawędzi pawilonu: skierowana
        // do Ring 2, bez zasłaniania schodów i wejść.
        if(pavilion)
        {
            Optional("mural FIGHT ZONE",()=>OriginalMural(pavilion,"FIGHT_ZONE",
                "35502A48-F7AB-4650-9874-3266881B2DF6.jpeg",
                new Vector3(-20.05f,2.7f,0),Quaternion.Euler(0,90,0),8.8f,true));
            // East elevation faces the obstacle/strength exercise stations.
            Optional("mural OBÓZ WOJOWNIKÓW",()=>OriginalMural(pavilion,"WOJOWNIKOW",
                "87405F34-A95B-4AD6-8637-A19599AADA61.jpeg",
                new Vector3(20.05f,2.7f,0),Quaternion.Euler(0,90,0),4.6f,true));
        }
        else BraxBuildQuality.Warn("OBÓZ MURALE FIGHT ZONE/WOJOWNIKÓW — brak budynku treningowego");
        if(gym)Optional("mural TRAINING CAMP",()=>OriginalMural(gym,"TRAINING_CAMP",
            "6B6E1CA3-605A-47FB-BF0E-F23B1D2C3301(1).jpeg",
            new Vector3(13.85f,2.7f,0),Quaternion.Euler(0,90,0),7f,true));
        else BraxBuildQuality.Warn("OBÓZ MURAL TRAINING CAMP — brak budynku siłowni");
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
            // Fight camp: centrum jest UBITYM GRUNTEM i blotem.
            // Trawa tylko na obwodzie i pod drzewami, nie plac miejski.
            float fence=BraxObozCampusLayout.FenceDistance(px,pz);
            float edgeLawn=Mathf.SmoothStep(0f,1f,
                Mathf.InverseLerp(19f,8f,fence));
            float lawn=Mathf.Clamp01(edgeLawn*(.45f+.32f*noise));
            float wear=1f-Mathf.SmoothStep(0f,1f,
                Mathf.InverseLerp(.9f,4.2f,distance));
            float work=0f;
            foreach(var station in new[]{new Vector2(-23,15),new Vector2(-12,51),new Vector2(12,51),
                new Vector2(0,-5),new Vector2(-23,-40),new Vector2(23,-40),
                new Vector2(-40,34)})
            {
                float d=Vector2.Distance(new Vector2(px,pz),station);
                work=Mathf.Max(work,1f-Mathf.SmoothStep(0f,1f,
                    Mathf.InverseLerp(7f,19f,d)));
            }
            float streamMud=(1f-Mathf.SmoothStep(0f,1f,
                Mathf.InverseLerp(5.5f,11f,channel)))*(.28f+.18f*(1-noise));
            float mud=Mathf.Clamp01(Mathf.Max(work*(.68f+.20f*noise),
                wear*(.40f+.35f*noise))+streamMud);
            // Kamienne ścieżki zachowują własny tilowany PBR, ich obrzeża
            // pozostają ziemiste; wejścia i bieżnia nie dostają plam.
            float gravel=Mathf.Max(shoulder,bank)*(1f-mud*.70f);
            colors[i]=new Color(gravel,lawn*(1f-mud),mud,1f);
            if (x < n && z < n) { int t = (z * n + x) * 6; int j = i + n + 1; tris[t] = i; tris[t + 1] = j; tris[t + 2] = i + 1; tris[t + 3] = i + 1; tris[t + 4] = j; tris[t + 5] = j + 1; }
        }
        var mesh = new Mesh { name = "Teren kompaktowy 280 x 280 m; strumien zewnetrzny" }; mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        mesh = SaveMesh(mesh);
        var g = Group("Podłoże — trawnik, ściółka, oryginalne błoto 2K i żwirowe przejścia", root, Vector3.zero).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = BraxObozMaterials.CampGround(ground,gravel); g.AddComponent<MeshCollider>().sharedMesh = mesh;
        BraxObozStructureAuthoring.RegisterGround(g.GetComponent<MeshCollider>());
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
            new Rect(-47f,-78f,47f,2.4f),
            // Podejście do strefy siłowej i odcinka 100 m.
            new Rect(18f,-4.5f,28f,2.4f),
            new Rect(44f,-4.5f,2.4f,49f),
            new Rect(0f,39f,18f,2.4f),
            new Rect(-26.5f,37.8f,26.5f,2.4f) // wejście do siłowni pod dachem
        };
        // Łączniki do drzwi usługowych, układ bez prowadzenia pieszych
        // przez modele kuchni, łaźni i stołówki.
        // The bathhouse opens NORTH. The previous x=-66 branch crossed its
        // entire interior from the south. Approach around the east wall.
        paths.Add(new Rect(-59.2f,-60f,2.4f,17.2f));
        paths.Add(new Rect(-67.2f,-45.2f,10.4f,2.4f));
        paths.Add(new Rect(-67.2f,-46.5f,2.4f,2.5f));
        // Service GLBs open SOUTH (local z=-depth/2), not at their rear.
        // A shared north/south lane passes between canteen and storage.
        paths.Add(new Rect(-32.7f,-91.2f,2.4f,32.6f));
        paths.Add(new Rect(-41.2f,-77f,2.4f,2.5f));   // stołówka, próg z=-74.5
        paths.Add(new Rect(-26.2f,-77f,2.4f,7f));     // magazyn, próg z=-70
        paths.Add(new Rect(-48.2f,-91.2f,24.4f,2.4f));
        paths.Add(new Rect(-48.2f,-90f,2.4f,2f));    // sauna, próg z=-88
        paths.Add(new Rect(-26.2f,-90f,2.4f,2f));    // medyczny, próg z=-88
        return paths;
    }
    static void Cabin(string name, float x, float z, float yaw)
    {
        var t = Group(name, root, P(x,0,z)); t.localRotation = Quaternion.Euler(0,yaw,0);
        Box(t,"Podłoga z desek",new Vector3(0,.08f,0),new Vector3(6.2f,.16f,8.2f),floor);
        for(int row=0;row<14;row++)
        {
            float y=.25f+row*.19f;
            // Realne B A L E: walcowe przekroje, nie głęboki płaski box.
            // Podłużne modele używają istniejącego PBR drewna, bez naciągania 2K.
            Cylinder(t,"Bal tylnej elewacji — okrągły",
                new Vector3(-3.1f,y,4),new Vector3(3.1f,y,4),.108f,bark,false);
            foreach(float side in new[]{-1f,1f})
            {
                // Otwarte okna pozostają faktycznie puste pomiędzy balami.
                if(y>1.05f&&y<2.02f)
                {
                    Cylinder(t,"Bal boczny przed oknem",
                        new Vector3(side*3,y,-4.1f),new Vector3(side*3,y,-1.1f),.108f,bark,false);
                    Cylinder(t,"Bal boczny za oknem",
                        new Vector3(side*3,y,1.1f),new Vector3(side*3,y,4.1f),.108f,bark,false);
                }
                else Cylinder(t,"Bal bocznej ściany — okrągły",
                    new Vector3(side*3,y,-4.1f),new Vector3(side*3,y,4.1f),.108f,bark,false);
                Cylinder(t,"Bal obok drzwi",
                    new Vector3(side<0?-3.1f:.60f,y,-4f),
                    new Vector3(side<0?-.60f:3.1f,y,-4f),.108f,bark,false);
            }
            if(y-.108f>=.17f+2.1f)Cylinder(t,"Bal nad wejściem",
                new Vector3(-.60f,y,-4),new Vector3(.60f,y,-4),.108f,bark,false);
        }
        CabinFinish(t);
        // Simple colliders follow the actual openings; decorative log cylinders do not each add physics work.
        foreach(float side in new[]{-1f,1f})
        {
            ColliderBox(t,new Vector3(side*1.815f,1.45f,-4),new Vector3(2.37f,2.7f,.22f));
            ColliderBox(t,new Vector3(side*.55f,1.295f,-4.17f),new Vector3(.16f,2.27f,.16f));
            ColliderBox(t,new Vector3(side*3,.55f,0),new Vector3(.22f,.95f,8));
            ColliderBox(t,new Vector3(side*3,2.43f,0),new Vector3(.22f,.7f,8));
            foreach(float end in new[]{-1f,1f})ColliderBox(t,new Vector3(side*3,1.55f,end*2.55f),new Vector3(.22f,1.05f,2.9f));
        }
        ColliderBox(t,new Vector3(0,1.45f,4),new Vector3(6,2.7f,.22f));
        ColliderBox(t,new Vector3(0,2.535f,-4),new Vector3(1.26f,.53f,.22f));
        // Batch ONLY the immobile cabin shell before placing the original
        // equipment. Previously batching stripped the imported GLB renderers
        // from all four kitchens and eight lockers.
        CombineDecoration(t);
        var porchRope=t.GetComponentsInChildren<Collider>(true)
            .FirstOrDefault(c=>c.name=="Sznur na ręczniki");
        BraxObozServices.OwnerPorchTowel(t,new Vector3(-1.65f,2.05f,-5.1f),porchRope);
        // IKEA IDANAS: original owner GLB; two SINGLE beds per cabin.
        // No old box-frame/mattress/pillow fakery. Do NOT merge their source
        // mesh renderers with the decorative cabin shell.
        // Osiem prawdziwych wojskowych szafek 3D, ze współdzieloną geometrią.
        // Nie dotykaj oryginalnych łóżek/postaci ani tekstur źródłowych.
        foreach(float side in new[]{-1f,1f})
            BraxObozServices.OwnerFixtureSourceMetres(t,"metal_cabinet_low_poly.glb",
                new Vector3(side*2.2f,.16f,-2.4f),
                new Vector3(1.15f,2.05f,.78f));
        // Replace the fake cuboid table with actual source cooking table,
        // portable field stove, gas cylinder, cookware rack, plates and sink.
        // This runs on all FOUR cabins; the central doorway stays clear.
        BraxObozServices.OwnerCabinKitchenette(t);
        foreach(float side in new[]{-1f,1f})
        {
            // Owner uploaded the complete original single-bed GLB to the
            // private draft release; imported separately at verified SHA256.
            // Position and real-size envelope leave walls, the entrance and
            // the central walking passage free. Prefab stays independent.
            var originalBed=BraxObozServices.OwnerSingleBed(t,
                new Vector3(side*2.05f,.16f,1.6f));
            originalBed.name="ORYGINAŁ IKEA IDANAS — łóżko jednoosobowe "+(side<0?"A":"B");
            var bedSpot=Group("Połóż się — "+name+" / IDANAS "+(side<0?"A":"B"),
                t,P(side*2.05f,0,1.6f)).gameObject.AddComponent<BraxSeat>();
            bedSpot.label=name+" · oryginalne łóżko IDANAS";
            bedSpot.lie=true;
            bedSpot.surfaceY=.69f*K;
            bedSpot.hipsAboveRoot=.9f*K;
        }
        // Nie zostawiaj pionowej szpary na nadprozu ani pod skrzydlem.
        Door(t,name+" — drzwi",new Vector3(-.45f,.17f,-4.05f),.90f,2.10f);
        Box(t,"Próg szczelny pod skrzydłem",new Vector3(0,.105f,-4.06f),new Vector3(1.10f,.14f,.26f),metal,false);
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
        BuildTrainingRing(name,x,z);
    }
    static void Octagon(float x,float z)
    {
        BuildTrainingOctagon(x,z);
    }
    // Oryginalny model 2K od właściciela (57 części, zawieszenie w GLB).
    // Wszystkie 48 instancji współdzieli jedną pochodną siatkę i oryginalny PBR.
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

    static void Bags(float x,float z,bool roof,int columns)
    {
        var t=Group((columns*2)+(roof?" worków zadaszonych":" worków otwartych"),root,P(x,0,z));
        var prototype=BraxObozBagAssembly.Prepare(OwnerBagPrototype(0));
        float halfBeam=(columns-1)*1.1f+1.8f;
        var supports=new RaycastHit[2,columns];
        var beams=new Collider[2];float roofBase=float.NegativeInfinity;
        for(int row=0;row<2;row++)
        {
            float rz=row==0?-2f:2f,maxMount=float.NegativeInfinity;
            for(int col=0;col<columns;col++)
            {
                float cx=-(columns-1)*1.1f+col*2.2f;
                var at=t.TransformPoint(P(cx,0,rz));
                if(!BraxObozBagAssembly.TryFloor(at,t,out supports[row,col]))
                {
                    BraxBuildQuality.Warn("OBÓZ WOREK: brak rzeczywistego podłoża dla "+row+"/"+col);
                    continue;
                }
                float floor=(supports[row,col].point.y-t.position.y)/K;
                maxMount=Mathf.Max(maxMount,floor+prototype.MountAboveFloorMetres);
            }
            if(float.IsNegativeInfinity(maxMount))continue;
            // Whole original suspension is retained. Its top enters the lower
            // 12 mm of a thick timber beam; the body never grows to an old hook.
            float beamY=maxMount+.18f-.012f;
            var beam=ExactTimber(t,"Belka nośna z bali — mocowanie oryginalnego zawiesia",
                new Vector3(-halfBeam,beamY,rz),new Vector3(halfBeam,beamY,rz),.18f,wood);
            beams[row]=beam.GetComponent<Collider>();
            foreach(float end in new[]{-halfBeam,halfBeam})
            {
                if(!BraxObozBagAssembly.TryFloor(t.TransformPoint(P(end,0,rz)),t,out var footing))
                    throw new InvalidOperationException("Brak podłoża pod słupem konstrukcji worków");
                float bottom=(footing.point.y-t.position.y)/K;
                ExactTimber(t,"Gruby drewniany słup — od rzeczywistej posadzki",
                    new Vector3(end,bottom,rz),new Vector3(end,beamY+.18f,rz),.22f,wood);
            }
            // The actual purlin underside lands on the log's upper surface.
            // No floating roof is justified merely by being in this group.
            float eave=beamY+.18f-.65f*.5f+RoofPurlinOffset+RoofPurlinHeight*.5f;
            roofBase=Mathf.Max(roofBase,eave);
        }
        if(roof && !float.IsNegativeInfinity(roofBase))
            Optional("dach placu worków",()=>MetalRoof(t,halfBeam*2+1.2f,6,roofBase,.65f));
        // Explicit construction membership is captured before the original
        // bags are attached, so neither bags nor later children can certify
        // the support of an otherwise floating beam or roof.
        BraxObozStructureAuthoring.Capture(t,t.GetComponentsInChildren<MeshFilter>(),
            t.GetComponentsInChildren<Collider>());
        for(int row=0;row<2;row++)for(int col=0;col<columns;col++)
        {
            if(!supports[row,col].collider || !beams[row])continue;
            int r=row,c=col;
            Optional("oryginalny worek "+(bags.Count+1),()=>{
                var bag=BraxObozBagAssembly.Create(t,"Worek "+(bags.Count+1),
                    prototype,supports[r,c],beams[r]);
                bags.Add(bag);if(roof)covered.Add(bag.transform);
            });
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

    // Realne ognisko na leśnej polanie. Kamienie bierzemy z istniejącej
    // biblioteki modeli 3D, nigdy z sześcianów imitujących skałę.
    static void CampfireFromOriginalRocks()
    {
        var camp=Group("Ognisko leśne — oryginalne skały 3D i ławy z bali",
            root,P(-67,0,-7));
        const string rockPath="Assets/Source/Claude/Library/park_rock.glb";
        var rockAsset=AssetDatabase.LoadAssetAtPath<GameObject>(rockPath);
        if(rockAsset)
        {
            for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI*2f/12f;
                var rock=PrefabUtility.InstantiatePrefab(rockAsset) as GameObject;
                if(!rock)continue;
                rock.name="Naturalny kamień z biblioteki — krąg ogniska "+i;
                rock.transform.SetParent(camp,false);
                var visible=rock.GetComponentsInChildren<Renderer>(true);
                if(visible.Length==0){Object.DestroyImmediate(rock);continue;}
                BraxObozPlacement.RealDimension(rock.transform,visible,.33f,
                    BraxObozPlacement.Axis.Y,rock.name);
                rock.transform.localRotation=Quaternion.Euler(0,i*53f,0);
                var at=camp.TransformPoint(P(Mathf.Cos(angle)*1.1f,.03f,
                    Mathf.Sin(angle)*1.1f));
                BraxObozPlacement.OnGround(rock.transform,visible,at,rock.name);
            }
        }
        else Debug.LogWarning("BRAXHORN_OBOZ_FIRE_ROCK_MISSING "+rockPath+
            " — nie twórz zastępczych kostek");
        CampfireWoodStack(camp);
        CampfireLogBenches(camp);
        var flameObject=Group("Żywy ogień — drobne cząsteczki, bez ciężkich cieni",
            camp,P(0,.27f,0)).gameObject;
        var particles=flameObject.AddComponent<ParticleSystem>();
        var main=particles.main;main.loop=true;main.maxParticles=90;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,1.1f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(.4f,1.4f);
        main.startSize=new ParticleSystem.MinMaxCurve(.10f,.35f);
        main.startColor=new ParticleSystem.MinMaxGradient(
            new Color(1f,.34f,.06f,.75f),new Color(1f,.71f,.24f,.85f));
        var emission=particles.emission;emission.rateOverTime=48f;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;
        shape.angle=16f;shape.radius=.30f*K;
        var shader=Shader.Find("Particles/Standard Unlit");
        if(!shader)shader=Shader.Find("Legacy Shaders/Particles/Additive");
        if(shader)
        {
            var flameMat=new Material(shader){name="Ognisko — płomień"};
            if(flameMat.HasProperty("_TintColor"))
                flameMat.SetColor("_TintColor",new Color(1,.47f,.09f,.68f));
            string destination=Folder+"/ObozCampfireParticles.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(destination);
            if(existing)
            {
                EditorUtility.CopySerialized(flameMat,existing);
                Object.DestroyImmediate(flameMat);
                flameMat=existing;
            }
            else AssetDatabase.CreateAsset(flameMat,destination);
            flameObject.GetComponent<ParticleSystemRenderer>().sharedMaterial=flameMat;
        }
        else Debug.LogWarning("BRAXHORN_OBOZ_FIRE_SHADER_MISSING: brak mobilnego shadera ognia");
        var light=Group("Ciepły blask ogniska bez cieni",camp,P(0,1f,0))
            .gameObject.AddComponent<Light>();
        light.type=LightType.Point;light.color=new Color(1,.43f,.14f);
        light.intensity=1.1f;light.range=9f*K;light.shadows=LightShadows.None;
        light.enabled=false;
        light.gameObject.AddComponent<BraxObozLocalLight>().source=light;
    }

    static void AssemblyGroundWithSchedule()
    {
        var plaza=Group("Plac apelowy przy wejściu — plan dnia i maszt",
            root,P(-12,0,-69));
        var board=Group("Drewniana tablica planu dnia",plaza,P(0,0,0));
        foreach(float x in new[]{-1.75f,1.75f})
            Cylinder(board,"Drewniany słup tablicy",
                new Vector3(x,0,0),new Vector3(x,2.7f,0),.19f,wood,true);
        Box(board,"Drewniany panel tablicy",
            new Vector3(0,1.86f,0),new Vector3(4.0f,1.55f,.18f),wood);
        BraxObozStructureAuthoring.Capture(board,board.GetComponentsInChildren<MeshFilter>(),
            board.GetComponentsInChildren<Collider>());
        var notice=Group("Plan dnia — informacja dla obozowiczów",
            board,P(0,1.86f,-.11f)).gameObject.AddComponent<TextMesh>();
        notice.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notice.text="BRAXHORN — PLAN DNIA\nROZGRZEWKA\nTRENING\nSTOŁÓWKA\nSPARING";
        // The former lettering measured 7.847 x 3.523 m on a 4 x 1.55 m
        // board in scene C. This size leaves a real margin on every side.
        notice.fontSize=54;notice.characterSize=.036f*K;
        notice.anchor=TextAnchor.MiddleCenter;notice.alignment=TextAlignment.Center;
        notice.color=new Color(.98f,.93f,.83f);
        notice.font.RequestCharactersInTexture(notice.text,notice.fontSize,notice.fontStyle);
        var textRenderer=notice.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial=BraxObozMaterials.SignText(notice.font);
        notice.gameObject.AddComponent<BraxCitySignText>();
        BraxObozPlacement.ToWall(notice.transform,new Renderer[]{textRenderer},
            board.TransformPoint(P(0,1.86f,-.3f)),board.forward,"napis planu dnia");
        var mast=Cylinder(plaza,"Maszt flagowy — czeka na zatwierdzoną flagę 3D",
            new Vector3(5f,0,0),new Vector3(5f,8f,0),.13f,wood,true);
        GroundBuiltStructure(mast.transform,mast.GetComponentsInChildren<Renderer>(),
            plaza.position.y,"maszt placu apelowego");
        Debug.LogWarning("BRAXHORN_OBOZ_OWNER_ASSETS_PENDING: flaga bordowa BRAXHORN "+
            "oraz namioty wojskowe wymagają zatwierdzonych modeli; bez atrap");
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


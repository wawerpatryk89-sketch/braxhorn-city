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
    static Material wood, floor, metal, gravel, ground, burgundy, dark, rope;
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
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            root = Group("Obóz Misia — odbudowa robocza, wymagany odbiór", null, Vector3.zero);
            var info = root.gameObject.AddComponent<BraxObozSceneInfo>();
            Terrain(); Paths();
            // Main R3 anchors remain unchanged. Additional cabins/service areas
            // are reconstructed placements; appearance has not been approved.
            Cabin("Domek 1", -94, -20, 90); Cabin("Domek 2", -94, -40, 90);
            Cabin("Domek 3", -69, -20, -90); Cabin("Domek 4", -69, -40, -90);
            Ring("Ring 1", -23, 15); Octagon(0, -5);
            // Owner rejected the imported city gym. Replace its entire instance,
            // interior, doors and collision source with camp training facilities.
            TrainingCampus();
            // Total: four pavilion + eight roofed outdoor + twelve open = 24.
            Bags(-23, -40, true,4); Bags(23, -40, false,6);
            ServiceBuildings();
            CampusArrival();
            BraxObozParkedBus.Add(root);
            CampusPerimeter();
            Bridge(72, -78); Bridge(72, 76);
            // The source bunker is retained without changing its mesh, origins
            // or doors. Entrance/collider conversion remains a separate audit.
            SourceBunker(-75, 89);
            BraxObozVegetation.Populate(root);
            var anchors = Group("Kotwice planu R3 — do połączenia z oryginalnymi modelami", root, Vector3.zero);
            foreach (var a in new[] { ("Fort 3 na 3",88f,95f), ("Chata leśniczego i sklep",-78f,-95f), ("Tartak",100f,-85f), ("Kojce",39f,-94f), ("Bunkier bramy",-24f,-127f) })
                Group(a.Item1, anchors, P(a.Item2, 0, a.Item3));
            var light = new GameObject("Popołudniowe słońce").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(25, -35, 0); light.color = new Color(1, .886f, .69f); light.intensity = 1.1f;
            light.shadows = LightShadows.Soft; RenderSettings.sun = light;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .003f;
            RenderSettings.fogColor = new Color(.34f, .46f, .43f);
            info.bags = bags.ToArray(); info.coveredBags = covered.ToArray(); info.cabinDoors = doors.Take(4).ToArray(); info.arenas = arenas.ToArray();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Nie zapisano zabudowy obozu");
            File.WriteAllText(Folder + "/status.json", "{\"completeCamp\":false,\"runtimeVerified\":false,\"cabins\":4,\"bags\":24,\"coveredBags\":12,\"rings\":2,\"octagons\":1}");
            Debug.Log("BRAXHORN_OBOZ zapisano roboczą zabudowę i rozmieszczenie lasu; fort, wnętrza, audyt bunkra i pełny odbiór nadal nieukończone");
        }
        finally { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
        AssetDatabase.SaveAssets();
    }
    static void Terrain()
    {
        const int n = 140; var vertices = new Vector3[(n + 1) * (n + 1)]; var uv = new Vector2[vertices.Length]; var tris = new int[n * n * 6];
        for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
        {
            float px = -210 + 420f * x / n, pz = -210 + 420f * z / n;
            float channel = Mathf.Abs(px - 72); float y = channel < 3.5f ? -.9f : channel < 5.5f ? Mathf.Lerp(-.9f, 0, (channel - 3.5f) / 2) : 0;
            int i = z * (n + 1) + x; vertices[i] = P(px, y, pz); uv[i] = new Vector2(px / 3, pz / 3);
            if (x < n && z < n) { int t = (z * n + x) * 6; int j = i + n + 1; tris[t] = i; tris[t + 1] = j; tris[t + 2] = i + 1; tris[t + 3] = i + 1; tris[t + 4] = j; tris[t + 5] = j + 1; }
        }
        var mesh = new Mesh { name = "Teren 420 m; pas strumienia" }; mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        mesh = SaveMesh(mesh);
        var g = Group("Podłoże — oryginalne mapy igliwia", root, Vector3.zero).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = ground; g.AddComponent<MeshCollider>().sharedMesh = mesh;
        // Water surface is a material placeholder clearly kept in the draft;
        // no assertion of finished reflections or stream animation.
        var water = BraxObozMaterials.Plain("woda-robocza", new Color(.176f, .353f, .353f), .85f);
        Box(root, "Strumień — powierzchnia robocza", new Vector3(72, -.42f, 0), new Vector3(6, .02f, 420), water, false);
        // The visible perimeter supplies its own collision. No invisible wall
        // is allowed to seal the new vehicle entrance at the south boundary.
    }
    static void Paths()
    {
        Box(root,"Główna ścieżka",new Vector3(0,.012f,0),new Vector3(6,.02f,300),gravel,false);
        Box(root,"Ścieżka do domków",new Vector3(-50,.014f,-55),new Vector3(100,.02f,3.5f),gravel,false);
        foreach(float x in new[]{-100f,-63f})
            Box(root,"Dojście przed wejściami domków",new Vector3(x,.014f,-35),new Vector3(3,.02f,40),gravel,false);
        Box(root,"Ścieżka przed budynkami zaplecza",new Vector3(-61.5f,.014f,-74),new Vector3(123,.02f,3.5f),gravel,false);
        foreach(var a in new[]{(-58f,-71.75f,4.5f),(-95f,-71f,6f),(-115f,-71f,6f)})
            Box(root,"Dojście do drzwi zaplecza",new Vector3(a.Item1,.014f,a.Item2),new Vector3(1.8f,.02f,a.Item3),gravel,false);
        Box(root,"Ścieżka medyczny–magazyn",new Vector3(-25,.014f,-99),new Vector3(50,.02f,3.5f),gravel,false);
        foreach(float x in new[]{-46f,-24f})
            Box(root,"Dojście do medycznego lub magazynu",new Vector3(x,.014f,-96),new Vector3(1.8f,.02f,6),gravel,false);
        Box(root,"Ścieżka do bunkra",new Vector3(-37.5f,.014f,76),new Vector3(75,.02f,3.5f),gravel,false);
        Box(root,"Podejście do fortu",new Vector3(35,.014f,76),new Vector3(70,.02f,3.5f),gravel,false);
        Box(root,"Podejście do tartaku",new Vector3(35,.014f,-78),new Vector3(70,.02f,3.5f),gravel,false);
    }
    static void Cabin(string name, float x, float z, float yaw)
    {
        var t = Group(name, root, P(x,0,z)); t.localRotation = Quaternion.Euler(0,yaw,0);
        Box(t,"Podłoga z desek",new Vector3(0,.08f,0),new Vector3(6.2f,.16f,8.2f),floor);
        for(int row=0;row<14;row++)
        {
            float y=.25f+row*.19f;
            Cylinder(t,"Bale tylnej ściany",new Vector3(-3.1f,y,4),new Vector3(3.1f,y,4),.11f,wood);
            foreach(float side in new[]{-1f,1f})
            {
                // The window is a real opening; no invisible solid wall behind it.
                if(y>1.05f&&y<2.02f){Cylinder(t,"Bale przy oknie",new Vector3(side*3,y,-4.1f),new Vector3(side*3,y,-1.1f),.11f,wood);Cylinder(t,"Bale przy oknie",new Vector3(side*3,y,1.1f),new Vector3(side*3,y,4.1f),.11f,wood);}
                else Cylinder(t,"Bale boczne",new Vector3(side*3,y,-4.1f),new Vector3(side*3,y,4.1f),.11f,wood);
                Cylinder(t,"Bale przy wejściu",new Vector3(side*.68f,y,-4),new Vector3(side*3.1f,y,-4),.11f,wood);
            }
            if(y>2.38f)Cylinder(t,"Nadproże",new Vector3(-.68f,y,-4),new Vector3(.68f,y,-4),.11f,wood);
        }
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
            var roof=Box(t,"Spadzisty dach",new Vector3(side*1.65f,3.2f,0),new Vector3(3.65f,.16f,8.8f),wood);roof.transform.localRotation=Quaternion.Euler(0,0,-side*20);
            for(int k=0;k<4;k++)Box(t,"Łóżko — noga",new Vector3(side*2.05f+(k%2-.5f)*.72f,.32f,1.6f+(k/2-.5f)*1.8f),new Vector3(.09f,.5f,.09f),wood);
            Box(t,"Łóżko — rama",new Vector3(side*2.05f,.48f,1.6f),new Vector3(1.05f,.15f,2.1f),wood);
            Box(t,"Materac — do wykończenia tkaniną",new Vector3(side*2.05f,.61f,1.6f),new Vector3(.99f,.16f,2.02f),dark);
            var bed=Group("Połóż się · "+name,t,P(side*2.05f,0,1.6f)).gameObject.AddComponent<BraxSeat>();
            bed.label=name+" · łóżko";bed.lie=true;bed.surfaceY=.69f*K;bed.hipsAboveRoot=.9f*K;
        }
        Box(t,"Stół",new Vector3(1.9f,.8f,-1.7f),new Vector3(1.1f,.08f,.7f),wood);
        foreach(float dx in new[]{-.46f,.46f})foreach(float dz in new[]{-.26f,.26f})Box(t,"Noga stołu",new Vector3(1.9f+dx,.43f,-1.7f+dz),new Vector3(.07f,.7f,.07f),wood);
        Door(t,name+" — drzwi",new Vector3(-.63f,.17f,-4.05f),1.26f,2.22f);
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
        var t=Group(name,root,P(x,0,z));Box(t,"Podest drewniany",new Vector3(0,.24f,0),new Vector3(8.6f,.48f,8.6f),wood);
        Box(t,"Mata bez cudzych logo",new Vector3(0,.50f,0),new Vector3(8.2f,.04f,8.2f),burgundy);
        foreach(float dx in new[]{-4f,4f})foreach(float dz in new[]{-4f,4f})Cylinder(t,"Słupek z osłoną",new Vector3(dx,.48f,dz),new Vector3(dx,2.15f,dz),.09f,dark,true);
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
        var mesh=new Mesh();var v=new Vector3[9];var uv=new Vector2[9];var tris=new int[24];v[0]=P(0,.35f,0);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;v[i+1]=P(Mathf.Sin(a)*radius,.35f,Mathf.Cos(a)*radius);uv[i+1]=new Vector2(v[i+1].x,v[i+1].z);tris[i*3]=0;tris[i*3+1]=i+1;tris[i*3+2]=(i+1)%8+1;}
        mesh.vertices=v;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        mesh=SaveMesh(mesh);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=burgundy;t.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;var p=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);var q=new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius);
            Cylinder(t,"Słupek klatki",p+Vector3.up*.35f,p+Vector3.up*2.4f,.065f,dark,true);
            if(i==3)continue; // entrance panel remains open in the reconstruction
            Cylinder(t,"Górny profil klatki",p+Vector3.up*2.35f,q+Vector3.up*2.35f,.04f,dark);
            for(int j=0;j<=16;j++){var c=Vector3.Lerp(p,q,j/16f);Cylinder(t,"Pion siatki — roboczy",c+Vector3.up*.4f,c+Vector3.up*2.3f,.005f,metal);}
            for(int j=0;j<12;j++)Cylinder(t,"Poziom siatki — roboczy",p+Vector3.up*(.45f+j*.16f),q+Vector3.up*(.45f+j*.16f),.005f,metal);
            var mid=(p+q)*.5f;var col=Group("Kolizja klatki",t,(mid+Vector3.up*1.3f)*K);col.localRotation=Quaternion.LookRotation(q-p);col.gameObject.AddComponent<BoxCollider>().size=new Vector3(.08f,2.1f,Vector3.Distance(p,q))*K;
        }
        var arena=t.gameObject.AddComponent<BraxObozArena>();arena.label="OKTAGON";arena.halfSize=new Vector2(4.8f,4.8f)*K;arenas.Add(arena);
    }
    static void Bags(float x,float z,bool roof,int columns)
    {
        var t=Group((columns*2)+(roof?" worków zadaszonych":" worków otwartych"),root,P(x,0,z));
        foreach(float side in new[]{-1f,1f})
        {
            Cylinder(t,"Belka nośna",new Vector3(-7,3.4f,side*2),new Vector3(7,3.4f,side*2),.12f,wood);
            foreach(float end in new[]{-7f,7f})Cylinder(t,"Słup",new Vector3(end,0,side*2),new Vector3(end,3.4f,side*2),.13f,wood,true);
        }
        if(roof)Box(t,"Zadaszenie",new Vector3(0,3.63f,0),new Vector3(15,.16f,6),wood);
        for(int row=0;row<2;row++)for(int col=0;col<columns;col++)
        {
            var pivot=Group("Worek "+(bags.Count+1),t,P(-(columns-1)*1.1f+col*2.2f,3.25f,row==0?-2:2));
            Cylinder(pivot,"Łańcuch",Vector3.zero,new Vector3(0,-.7f,0),.015f,metal);
            Cylinder(pivot,"Korpus worka",new Vector3(0,-.7f,0),new Vector3(0,-2.2f,0),.28f,dark);
            Cylinder(pivot,"Górne wzmocnienie",new Vector3(0,-.7f,0),new Vector3(0,-.84f,0),.285f,burgundy);
            Cylinder(pivot,"Dolne wzmocnienie",new Vector3(0,-2.07f,0),new Vector3(0,-2.2f,0),.285f,burgundy);
            var collider=pivot.gameObject.AddComponent<CapsuleCollider>();collider.center=P(0,-1.45f,0);collider.height=1.5f*K;collider.radius=.28f*K;
            var bag=pivot.gameObject.AddComponent<BraxWorek>();bag.gora=.7f*K;bag.dol=2.2f*K;bag.dlugosc=1.45f*K;bag.promien=.28f*K;bags.Add(bag);if(roof)covered.Add(pivot);
        }
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
    static void SourceBunker(float x,float z)
    {
        string path="Assets/Source/Prywatne/Oboz/environment/models/underground_bunker__unity.glb";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)throw new InvalidOperationException("Brak zaimportowanego oryginalnego bunkra");
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab);obj.name="Bunkier dowodzenia — oryginał, wejście wymaga audytu";
        obj.transform.SetParent(root,false);obj.transform.localPosition=P(x,0,z);obj.transform.localScale*=K;
    }
}


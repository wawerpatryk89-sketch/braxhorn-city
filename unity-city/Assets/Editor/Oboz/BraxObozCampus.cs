using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Camp-only construction in the original metre coordinates, scaled once by K.
public static partial class BraxObozEnvironment
{
    static Material campusAsphalt, campusPaving, campusSteel, campusRubber, campusMark;
    const float RoofPurlinOffset=.3025f, RoofPurlinHeight=.22f;
    static void CampusMaterials()
    {
        campusAsphalt = BraxObozMaterials.PublicPbr("clean_asphalt", 2.1f);
        campusPaving = BraxObozMaterials.PublicPbr("concrete_pavers", 1.9f);
        campusSteel = BraxObozMaterials.Plain("campus-antracyt", new Color(.045f,.055f,.06f), .38f, .7f);
        campusRubber = BraxObozMaterials.Plain("campus-guma", new Color(.045f,.055f,.05f), .12f);
        campusMark = BraxObozMaterials.Plain("campus-oznaczenia", new Color(.88f,.86f,.72f), .15f);
    }

    // Owal i 100 m na syntetycznym tartanie. Wspolne, tilowane materialy,
    // oryginalna skala metrow. Geometryczne pasy na nawierzchni, nie billboard.
    static void RunningTracks()
    {
        BuildRunningTracks();
    }
    static void TrackAnnulus(Transform parent,string name,float ax0,float az0,float ax1,float az1,
        float y,Material material,bool solid)
    {
        const int steps=TrackSegments;
        var vertices=new Vector3[(steps+1)*2];
        var uv=new Vector2[vertices.Length];
        var triangles=new int[steps*6];
        for(int i=0;i<=steps;i++)
        {
            float theta=(Mathf.PI*2*i)/steps;
            float sin=Mathf.Sin(theta),cos=Mathf.Cos(theta);
            vertices[2*i]=P(cos*ax0,y,sin*az0);
            vertices[2*i+1]=P(cos*ax1,y,sin*az1);
            uv[2*i]=new Vector2(i*.28f,0);
            uv[2*i+1]=new Vector2(i*.28f,1);
            if(i==steps)continue;
            int t=i*6,v=i*2;
            triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;
            triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
        }
        var mesh=new Mesh{name=name};
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        mesh=SaveMesh(mesh);
        var go=Group(name,parent,Vector3.zero).gameObject;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }

    // Private owner gym_environment FBX: 57 authored objects, one 2048x2048
    // colour atlas. Never substitute primitive weights/benches/machines.
    // The imported gym is a PROP collection, not the old city gym shell.
    static void CoveredStrengthGym()
    {
        var gym=Group("BRAXHORN · SIŁOWNIA 3D WŁAŚCICIELA",root,P(-40,0,34));
        var rubberFloor=BraxObozMaterials.Plain("silownia-gumowa-podloga",
            new Color(.065f,.073f,.075f),.12f);
        Box(gym,"Drewniana podbudowa maty — rzeczywista grubość",new Vector3(0,.09f,0),
            new Vector3(27f,.18f,19.5f),wood);
        var gymMat=Box(gym,"Podłoga z kolizją",new Vector3(0,.20f,0),
            new Vector3(26.7f,.04f,19.2f),rubberFloor);
        gymMat.AddComponent<BraxObozDeformableMat>().Configure(26.7f,19.2f);
        MatTimberEdges(gym,27f,19.5f,.18f);
        foreach(float z in new[]{-7.9f,7.9f})
        foreach(float x in new[]{-12.4f,-6.2f,0f,6.2f,12.4f})
        {
            var post=Cylinder(gym,"Drewniany słup z bala pod dachem siłowni",
                new Vector3(x,.22f,z),new Vector3(x,5.13f,z),.20f,wood,true);
            BraxObozPlacement.OnGround(post.transform,post.GetComponentsInChildren<Renderer>(),
                gym.TransformPoint(P(x,.22f,z)),"słup siłowni "+x+"/"+z);
        }
        foreach(float z in new[]{-7.9f,7.9f})
            Cylinder(gym,"Belka nośna z grubego drewna",
                new Vector3(-13.5f,5.13f,z),new Vector3(13.5f,5.13f,z),.23f,wood,true);
        // Longitudinal purlins cross the existing beam rows, while the roof's
        // repeated trusses do not. Four real timber bearings close that gap.
        float beamTop=5.13f+.23f;
        float purlinBottom=5.4f+1.25f*.5f-RoofPurlinOffset-RoofPurlinHeight*.5f;
        foreach(float x in new[]{-27.7f*.25f,27.7f*.25f})
        foreach(float z in new[]{-7.9f,7.9f})
            Box(gym,"Drewniane oparcie płatwi na belce",
                new Vector3(x,(beamTop+purlinBottom)*.5f,z),
                new Vector3(.18f,purlinBottom-beamTop,.24f),wood);
        MetalRoof(gym,27.7f,20.2f,5.4f,1.25f);
        Sign(gym,"BRAXHORN GYM · OBÓZ",new Vector3(0,4.7f,-8.15f),19f);
        BraxObozStructureAuthoring.Capture(gym,gym.GetComponentsInChildren<MeshFilter>(),
            gym.GetComponentsInChildren<Collider>());
        OwnerStrengthEquipment(gym);
    }

    static void OwnerStrengthEquipment(Transform gym)
    {
        OwnerStrengthInteriors(gym);
    }

    static void TrainingCampus()
    {
        var pavilion=Group("Pawilon treningowy — otwarty, bez starej siłowni",root,P(0,0,51));
        Box(pavilion,"Podbudowa ziemna pawilonu",new Vector3(0,.035f,0),new Vector3(42,.07f,16),gravel);
        // Individual twenty-centimetre boards keep real plank spacing. They
        // are construction, not a box standing in for missing equipment.
        var decking=Group("Deski pawilonu",pavilion,Vector3.zero);
        for(int board=0;board<210;board++)
            Box(decking,"Deska podestu 20 cm",new Vector3(-20.9f+board*.2f,.09f,0),
                new Vector3(.197f,.04f,16),wood,false);
        ColliderBox(decking,new Vector3(0,.09f,0),new Vector3(42,.04f,16));
        CombineDecoration(decking);
        foreach(float x in new[]{-20f,-10f,0f,10f,20f})
        foreach(float z in new[]{-7.5f,7.5f})
        {
            var post=Cylinder(pavilion,"Słup pawilonu z grubego bala",
                new Vector3(x,.11f,z),new Vector3(x,5.45f,z),.24f,wood,true);
            BraxObozPlacement.OnGround(post.transform,post.GetComponentsInChildren<Renderer>(),
                pavilion.TransformPoint(P(x,.11f,z)),"słup pawilonu "+x+"/"+z);
        }
        foreach(float z in new[]{-7.5f,7.5f})
            Cylinder(pavilion,"Oczep pawilonu z bali",
                new Vector3(-21f,5.45f,z),new Vector3(21f,5.45f,z),.22f,wood,true);
        var roofing=Group("Dach dwuspadowy i wiązary",pavilion,Vector3.zero);
        roofing.localRotation=Quaternion.Euler(0,90,0);
        MetalRoof(roofing,17,43,5.6f,2.2f);
        BraxObozStructureAuthoring.Capture(pavilion,pavilion.GetComponentsInChildren<MeshFilter>(),
            pavilion.GetComponentsInChildren<Collider>());
        Optional("Ring 2",()=>Ring("Ring 2",-12,51));
        Optional("cztery oryginalne worki w pawilonie",()=>{
            int first=bags.Count; Bags(12,51,false,2);
            foreach(var bag in bags.Skip(first)) covered.Add(bag.transform);
        });
        Sign(pavilion,"BRAXHORN · OBÓZ TRENINGOWY",new Vector3(0,4.85f,-7.7f),12);
        OwnerTrainingStations();
    }
    static void Station(string label,float x,float z,float width,float depth,Action<Transform> build)
    {
        Optional("stanowisko "+label,()=>{
            var t=Group("Stanowisko · "+label,root,P(x,0,z));
            // The visible top stays at .08 m. A 3 cm depression must remain
            // above the base, not disappear into its old .07 m top surface.
            Box(t,"Podkład",new Vector3(0,.02f,0),new Vector3(width,.04f,depth),campusPaving);
            var mat=Box(t,"Nawierzchnia gumowa",new Vector3(0,.06f,0),
                new Vector3(width-.3f,.04f,depth-.3f),campusRubber,true);
            mat.AddComponent<BraxObozDeformableMat>().Configure(width-.3f,depth-.3f);
            MatTimberEdges(t,width,depth,.04f);
            BraxObozStructureAuthoring.Capture(t,t.GetComponentsInChildren<MeshFilter>(),
                t.GetComponentsInChildren<Collider>());
            build(t);
            float signWidth=Mathf.Min(width-1,8);
            Sign(t,label,new Vector3(0,1.6f,depth*.5f),signWidth);
            // This marker records geometry only. Exercise animations are not implemented.
            var station=t.gameObject.AddComponent<BraxObozTrainingStation>();station.label=label;
            station.footprint=new Vector2(width,depth)*K;
        });
    }
    static void MatTimberEdges(Transform parent,float width,float depth,float baseHeight)
    {
        // Flush with the rubber surface; edges do not make a new trip step.
        float centre=baseHeight+.02f;
        foreach(float side in new[]{-1f,1f})
        {
            Box(parent,"Drewniane obrzeże maty",new Vector3(side*(width*.5f-.075f),centre,0),
                new Vector3(.15f,.04f,depth),wood);
            Box(parent,"Drewniane obrzeże maty",new Vector3(0,centre,side*(depth*.5f-.075f)),
                new Vector3(width-.3f,.04f,.15f),wood);
        }
    }
    static void Sign(Transform parent,string label,Vector3 p,float width)
    {
        var board=Group("Drewniana tablica · "+label,parent,p*K);
        // These are actual planks and grounded log supports. Several previous
        // signs had a floating panel or posts ending below its lower edge.
        for(int row=0;row<4;row++)
            Box(board,"Deska tablicy",new Vector3(0,-.24375f+row*.1625f,0),
                new Vector3(width,.1595f,.12f),wood,false);
        ColliderBox(board,Vector3.zero,new Vector3(width,.65f,.12f));
        var signParts=new List<MeshFilter>(board.GetComponentsInChildren<MeshFilter>());
        var signColliders=new List<Collider>(board.GetComponentsInChildren<Collider>());
        Physics.SyncTransforms();
        foreach(float side in new[]{-1f,1f})
        {
            float px=p.x+side*width*.42f,pz=p.z+.08f;
            var proposed=parent.TransformPoint(P(px,0,pz));
            var hit=Physics.RaycastAll(proposed+Vector3.up*(1.5f*K),Vector3.down,
                3f*K,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>h.collider && !h.collider.transform.IsChildOf(board) &&
                    h.normal.y>.7f && h.point.y<=proposed.y+.35f*K)
                .OrderByDescending(h=>h.point.y).FirstOrDefault();
            float baseY=hit.collider?parent.InverseTransformPoint(hit.point).y/K:0f;
            if(!hit.collider)BraxBuildQuality.Warn("OBÓZ TABLICA — brak podłoża: "+label);
            var post=Cylinder(parent,"Słup z bali pod tablicą · "+label,
                new Vector3(px,baseY,pz),new Vector3(px,p.y+.26f,pz),.12f,wood,true);
            signParts.AddRange(post.GetComponentsInChildren<MeshFilter>());
            signColliders.AddRange(post.GetComponentsInChildren<Collider>());
            if(hit.collider)
            {
                var record=BraxObozPlacementRecord.For(post.transform);
                record.SetMembers(post.GetComponentsInChildren<Renderer>(true));
                record.support=BraxObozPlacementRecord.SupportKind.Ground;
                record.verticalSupport=hit.collider;record.label="słup tablicy "+label;
            }
        }
        BraxObozStructureAuthoring.Capture(board,signParts.ToArray(),signColliders.ToArray());
        var text=Group("Napis · "+label,board,P(0,0,-.065f)).gameObject.AddComponent<TextMesh>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text=label;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=72;
        text.characterSize=.38f*K*10/72;text.color=new Color(.9f,.87f,.73f);
        text.font.RequestCharactersInTexture(label,text.fontSize,text.fontStyle);
        float advance=0;
        foreach(char c in label)if(text.font.GetCharacterInfo(c,out var glyph,text.fontSize,text.fontStyle))advance+=glyph.advance;
        float measured=advance*text.characterSize/10;
        if(measured>(width-.3f)*K)text.characterSize*=((width-.3f)*K)/measured;
        var renderer=text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=BraxObozMaterials.SignText(text.font);
        text.gameObject.AddComponent<BraxCitySignText>();
        text.transform.localRotation=Quaternion.identity;
        BraxObozPlacement.ToWall(text.transform,new Renderer[]{renderer},
            board.position-board.forward*(.3f*K),board.forward,"napis "+label);
    }
    static void CampusArrival()
    {
        var t=Group("Dojazd i plac autobusowy",root,Vector3.zero);
        TrackClippedRoad(t);
        // Turning forecourt: outer radius 18 m, island radius 6 m; the design
        // envelope is a 12 x 2.6 m coach, not a claim of a tested driving model.
        Disc(t,"Plac zawracania",new Vector3(0,.095f,-94),18,campusAsphalt,true);
        // The owner specified a GREEN central island, not a paved disk.
        // Keep the asphalt footprint and original bull/pedestal geometry.
        var islandGrass=BraxObozMaterials.PublicPbr("Grass001",1.4f);
        Disc(t,"Zielona wyspa centralna z oryginalną murawą 3D",
            new Vector3(0,.16f,-94),6f,islandGrass,true);
        // Biała obwodnica i pomnik pozostają wewnątrz wyspy, bez zmiany bramy.
        var rondo = Group("Rondo BRAXHORN — biały obwód i wyspa",t,P(0,0,-94));
        var whiteRondo = BraxObozMaterials.Plain("rondo-linia-biala",new Color(.96f,.96f,.94f),.06f);
        // The bay connector reaches .100 m. Paint at .104 m clears both
        // surfaces; the former .149 m floated 5.4 cm above the forecourt.
        TrackAnnulus(rondo,"Pełny biały obrys ronda",17.82f,17.82f,17.98f,17.98f,.104f,whiteRondo,false);
        BraxObozStructureAuthoring.Capture(rondo,rondo.GetComponentsInChildren<MeshFilter>(),
            rondo.GetComponentsInChildren<Collider>());
        // WŁAŚCICIEL 7.11: BYK/POMNIK ZAMROŻONY. Nie zmieniaj pozycji,
        // rotacji, skali, materiałów ani colliderów zestawu byka/cokołu.
        // Ewentualne korekty ronda wykonuj wyłącznie na nawierzchni wokół niego.
        RondoBull(rondo);
        Box(t,"Zatoka autobusu",new Vector3(21,.055f,-92),new Vector3(4,.11f,20),campusAsphalt);
        // Thin surface just above the measured terrain: no 9.5 cm buried slab.
        Box(t,"Połączenie zatoki",new Vector3(14,.0985f,-94),new Vector3(12,.005f,24),campusAsphalt);
        // Four arrival walkways are now authored by PavedPaths/PathRectangles.
        // Their mesh and curbs are cut against BOTH tracks instead of being
        // rendered as unbroken cubes through the red running surface.
        // Owner's 6 November follow-up: remove the unnecessary road stripes
        // beside the entrance. Track lane markings remain part of the track.
        Sign(t,"BRAXHORN · PRZYJAZDY",new Vector3(26,2.1f,-84),5);
        foreach(float z in new[]{-110f,-100f,-90f,-80f})Lamp(t,new Vector3(28,0,z));
        // Footpath protection, leaving boarding openings at both coach doors.
        foreach(float z in new[]{-99f,-93f,-86f})
        {
            var post=Cylinder(t,"Słupek ochronny",new Vector3(24,0,z),
                new Vector3(24,.97f,z),.075f,campusSteel,true);
            BraxObozPlacement.OnGround(post.transform,post.GetComponentsInChildren<Renderer>(true),
                t.TransformPoint(P(24,0,z)),"słupek ochronny przy zatoce");
        }
        // The owner's FBX contains its own seating. Never duplicate fake standalone benches outside its shelter.
        // Nowy FBX Meshy od właściciela zastępuje starą wiatę, bez atrap.
        var shelter=Group("Wlascicielska wiata 3D — przystanek obozowy",t,P(27,0,-92));
        OwnerShelter(shelter);
        var route=Group("Trasa kontrolna autobusu — bez automatycznej jazdy",t,Vector3.zero);
        Group("Dojazd",route,P(0,0,-138));Group("Brama",route,P(0,0,-125));
        for(int i=0;i<=32;i++){
            float a=Mathf.PI+i*Mathf.PI*2/32;
            Group("Łuk "+i,route,P(Mathf.Sin(a)*12,0,-94+Mathf.Cos(a)*12));
        }
    }
    static void OwnerShelter(Transform parent)
    {
        const string sourcePath=
            "Assets/Source/Prywatne/Oboz/OwnerAssets/Przystanek/Meshy_AI_The_object_is_a_recta_1104160815_texture.fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if(!source)throw new InvalidOperationException("Brak oryginalnego FBX wiaty, nie budować starej atrapy: "+sourcePath);
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Nie można zaimportować oryginalnego FBX wiaty");
        model.name="Nowa wiata właściciela — FBX + oryginalny atlas 2K";
        model.transform.SetParent(parent,false);
        const string texturePath=
            "Assets/Source/Prywatne/Oboz/OwnerAssets/Przystanek/Meshy_AI_The_object_is_a_recta_1104160815_texture.png";
        var ownerAtlas=BraxObozMaterials.OriginalColorMap(texturePath,2048,"oryginalny atlas wiaty właściciela");
        var ownerShader=Shader.Find("Standard");
        if(!ownerShader)throw new InvalidOperationException("Brak shadera PBR dla oryginalnej wiaty");
        var newMaterial=new Material(ownerShader){
            name="Wiata właściciela — oryginalna tekstura FBX 2K",
            mainTexture=ownerAtlas,enableInstancing=true
        };
        newMaterial.SetFloat("_Glossiness",.18f);
        newMaterial.SetFloat("_Metallic",.10f);
        string ownerMaterialPath=Folder+"/OwnerShelterOriginal2K.mat";
        var savedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ownerMaterialPath);
        if(savedMaterial){
            EditorUtility.CopySerialized(newMaterial,savedMaterial);
            Object.DestroyImmediate(newMaterial);
            newMaterial=savedMaterial;
        }else AssetDatabase.CreateAsset(newMaterial,ownerMaterialPath);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("FBX wiaty nie zawiera renderera");
        foreach(var meshRenderer in renderers)
            meshRenderer.sharedMaterials=meshRenderer.sharedMaterials.Select(_=>newMaterial).ToArray();
        Bounds b=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);
        if(b.size.x<=.001f||b.size.y<=.001f||b.size.z<=.001f)
            throw new InvalidOperationException("Wiata FBX ma niepoprawne wymiary");
        if(b.size.x>b.size.z*1.3f)
            model.transform.localRotation=Quaternion.Euler(0,90,0);
        // Owner's review: the APK #22 shelter faces backwards. Turn the imported
        // FBX itself (not the pavement or bus) 180 degrees towards the boarding bay.
        model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
        b=renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);
        // Wiata: oryginalne realne metry z FBX * K, bez wpychania w pudełko.
        // Nie znamy zatwierdzonego wymiaru konkretnej wiaty.
        BraxObozPlacement.NativeMetres(model.transform,renderers,
            "wiata FBX właściciela");
        BraxObozPlacement.OnGround(model.transform,renderers,
            parent.position,"wiata FBX właściciela");
        b=BraxObozPlacement.Bounds(renderers);
        if(b.size.x>8.6f*K || b.size.z>8.6f*K)
            Debug.LogWarning("BRAXHORN_OBOZ_SHELTER_FOOTPRINT_REVIEW: "+
                b.size+" — wiata może nachodzić na zatokę lub przejście");
        Debug.Log("BRAXHORN_OBOZ_WIATA_3D: zrodlo=FBX 2K + oryginalny PNG 2048, wymiary="+b.size+
            ", correctedYaw=180 względem APK22; wejście i ławki wymagają oględzin w Unity");
    }

    static void RondoBull(Transform center)
    {
        // Najpierw dokładny oryginał właściciela 4X, nigdy 4X podwójnie.
        // Gdy dostępne jest tylko źródło 1X, zastosuj DOKŁADNIE x4
        // na transformacji tego samego modelu, bez zmiany siatki/UV/tekstur.
        // W raporcie wyraźnie oznacz wariant źródłowy zamiast udawać plik 4X.
        const string originalPath =
            "Assets/Source/Prywatne/Oboz/OwnerAssets/A_charging_bull_statu_0725030443_refine.glb";
        const string scaledPath =
            "Assets/Source/Prywatne/Oboz/OwnerAssets/BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(scaledPath);
        bool scaledOwnerAsset=source;
        string sourcePath=scaledPath;
        if(!source)
        {
            source=AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
            sourcePath=originalPath;
            if(source)BraxBuildQuality.Warn("OBOZ_BULL_4X_SOURCE_PENDING: "+
                "w prywatnej dostawie jest tylko oryginał 1X; wykonano dokładne "+
                "powiększenie x4 Transform, sprawdzić wizualnie przed odbiorem.");
        }
        if (!source)
        {
            Debug.LogWarning("BRAXHORN_OBOZ_BULL_PENDING: Brak prywatnego GLB: " + sourcePath +
                ". Nie wolno oznaczyc przebudowy ronda jako skonczonej.");
            return;
        }
        var monument = Group("Pomnik byka BRAXHORN — model wlasciciela",center,Vector3.zero);
        Box(monument,"Cokol kamienny PBR",new Vector3(0,.53f,0),
            new Vector3(3.4f,.80f,5.4f),campusPaving);
        var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (!model) throw new InvalidOperationException("Nie mozna instancjonowac oryginalnego GLB byka");
        model.name = "Oryginalny byk GLB — 100% tekstur i geometrii";
        model.transform.SetParent(monument,false);
        model.transform.localPosition = Vector3.zero;
        // Owner's review: the charging bull was facing away from the entry.
        // Turn the original GLB 180 degrees while preserving mesh, UV and JPEG.
        model.transform.localRotation = Quaternion.Euler(0f,180f,0f);
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("Model byka bez renderera");
        Bounds b = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) b.Encapsulate(renderer.bounds);
        float longest = Mathf.Max(b.size.x,b.size.z);
        if (longest < .0001f) throw new InvalidOperationException("Model byka ma zerowy rozmiar");
        // BYK 4X: zachowaj oryginalną skalę modelu, bez zmniejszenia
        // do wymyślonej koperty 4,8 m. Podstawa na górze cokołu.
        BraxObozPlacement.NativeMetres(model.transform,renderers,
            "oryginalny byk 4X");
        if(!scaledOwnerAsset)model.transform.localScale*=4f;
        BraxObozPlacement.OnGround(model.transform,renderers,
            monument.TransformPoint(P(0,.93f,0)),"oryginalny byk 4X");
        b=BraxObozPlacement.Bounds(renderers);
        if(b.size.x>5.6f*K || b.size.z>5.6f*K)
            Debug.LogWarning("BRAXHORN_OBOZ_BULL_CLEARANCE_REVIEW: "+
                b.size+" — sprawdzić przejazd dookoła ronda");
        Debug.Log("BRAXHORN_OBOZ_BULL_IMPORT: obrys=" + b.size +
            " asset=" + sourcePath + " PBR=oryginalny UV=oryginalne");
    }

    static void Disc(Transform parent,string name,Vector3 center,float radius,Material mat,bool solid)
    {
        // Preserve the exact footprint/top of the roundabout. Its previous
        // single face was suspended 9.5/16 cm above the real ground.
        var polygon=Enumerable.Range(0,96).Select(i=>new Vector2(
            center.x+Mathf.Cos(i*Mathf.PI*2/96)*radius,
            center.z+Mathf.Sin(i*Mathf.PI*2/96)*radius)).ToList();
        var surface=PolygonVolume(parent,name,polygon,0,p=>center.y,mat,solid);
        BraxObozStructureAuthoring.Capture(surface.transform,
            surface.GetComponentsInChildren<MeshFilter>(),surface.GetComponentsInChildren<Collider>());
    }
    // Zachowany graficzny szyld właściciela; automatyczna maska usuwa tylko
    // białe tło łączące się z krawędzią JPEG. Źródłowy JPEG nie jest edytowany.
    // Prawdziwy przestrzenny emblemat z oryginalnego JPEG. Kolor i litery
    // pozostają na okleinie; ciemna blacha 26 cm jest wytłoczona w jej kontur.
    // Żaden słupek nie może wejść w światło 8,4 m przesuwnej bramy.
    static void OwnerGateSign(Transform gate)
    {
        const string sourcePath =
            "Assets/Source/Prywatne/Oboz/OwnerAssets/Murale/44B8F5D6-7F27-4EF3-A804-9BC8C4F462D0.jpeg";
        if (!System.IO.File.Exists(sourcePath))
            throw new InvalidOperationException("Brak oryginalnego szyldu BRAXHORN: " + sourcePath);
        var jpeg = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(jpeg, System.IO.File.ReadAllBytes(sourcePath), false))
            throw new InvalidOperationException("Nieczytelny oryginalny JPEG szyldu");
        int width = jpeg.width, height = jpeg.height;
        if (width < 512 || height < 200)
            throw new InvalidOperationException("Szyld BRAXHORN: źródło ma błędne wymiary");
        var pixels = jpeg.GetPixels32();
        var outside = new bool[pixels.Length];
        var queue = new Queue<int>();
        bool IsBackground(int i)
        {
            var pixel = pixels[i];
            return pixel.r >= 238 && pixel.g >= 238 && pixel.b >= 238;
        }
        void Visit(int i)
        {
            if (i < 0 || i >= outside.Length || outside[i] || !IsBackground(i)) return;
            outside[i] = true;
            queue.Enqueue(i);
        }
        for (int x = 0; x < width; x++) { Visit(x); Visit((height - 1) * width + x); }
        for (int y = 0; y < height; y++) { Visit(y * width); Visit(y * width + width - 1); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), x = i % width, y = i / width;
            if (x > 0) Visit(i - 1);
            if (x + 1 < width) Visit(i + 1);
            if (y > 0) Visit(i - width);
            if (y + 1 < height) Visit(i + width);
        }
        for (int i = 0; i < pixels.Length; i++) if (outside[i]) pixels[i].a = 0;

        // Zapisujemy tylko wygenerowaną okleinę PNG; oryginalny JPEG pozostaje nietknięty.
        var alpha = new Texture2D(width, height, TextureFormat.RGBA32, false);
        alpha.SetPixels32(pixels);
        alpha.Apply(false, false);
        string alphaPath = Folder + "/OwnerGateSignAlpha.png";
        var encoded = alpha.EncodeToPNG();
        if (!System.IO.File.Exists(alphaPath) ||
            !System.IO.File.ReadAllBytes(alphaPath).SequenceEqual(encoded))
        {
            System.IO.File.WriteAllBytes(alphaPath, encoded);
            AssetDatabase.ImportAsset(alphaPath, ImportAssetOptions.ForceSynchronousImport);
        }
        Object.DestroyImmediate(alpha);
        Object.DestroyImmediate(jpeg);
        var importer = AssetImporter.GetAtPath(alphaPath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Nie można zaimportować okleiny szyldu");
        bool adjust = !importer.alphaIsTransparency ||
            importer.maxTextureSize != 2048 ||
            importer.textureType != TextureImporterType.Default ||
            importer.textureShape != TextureImporterShape.Texture2D ||
            importer.npotScale != TextureImporterNPOTScale.None ||
            importer.filterMode != FilterMode.Trilinear ||
            !importer.mipmapEnabled;
        if (adjust)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(alphaPath,ImportAssetOptions.ForceSynchronousImport);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaPath);
        var shader = Shader.Find("Unlit/Transparent");
        if (!texture || !shader)
            throw new InvalidOperationException("Brak tekstury lub shadera okleiny BRAXHORN");
        var material = new Material(shader)
        {
            name = "BRAXHORN — oryginalna okleina szyldu, bez zmiany liter",
            mainTexture = texture,
            enableInstancing = true
        };
        string materialPath = Folder + "/OwnerGateSign.mat";
        var savedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (savedMaterial)
        {
            EditorUtility.CopySerialized(material, savedMaterial);
            Object.DestroyImmediate(material);
            material = savedMaterial;
        }
        else AssetDatabase.CreateAsset(material, materialPath);

        const float signWidth = 8.4f;
        const float centreY = 6.2f;
        const float faceZ = -0.14f;
        const float backZ = 0.12f; // fizyczna grubość blachy = 26 cm
        float signHeight = signWidth * height / (float)width;
        const int columns = 128;
        int rows = Mathf.Max(32, Mathf.RoundToInt(columns * height / (float)width));
        var filled = new bool[columns, rows];
        int filledCount = 0, firstVisibleRow=rows;
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            // Nadpróbkowanie zachowuje drobne krawędzie dekoracyjnego byka.
            bool visible = false;
            foreach (float fy in new[] { .25f, .5f, .75f })
            foreach (float fx in new[] { .25f, .5f, .75f })
            {
                int sx = Mathf.Min(width - 1, Mathf.FloorToInt((x + fx) * width / columns));
                int sy = Mathf.Min(height - 1, Mathf.FloorToInt((y + fy) * height / rows));
                if (pixels[sy * width + sx].a >= 90) { visible = true; break; }
            }
            filled[x, y] = visible;
            if (visible) { filledCount++; firstVisibleRow=Mathf.Min(firstVisibleRow,y); }
        }
        if (filledCount < columns * rows / 30)
            throw new InvalidOperationException("Kontur okleiny szyldu jest pusty");
        // The JPEG has a white margin which becomes transparent; mount the
        // lowest VISIBLE pixel, not the bottom edge of the original JPEG.
        float visibleSignBottom=centreY+signHeight*(firstVisibleRow/(float)rows-.5f);
        bool Has(int x, int y) =>
            x >= 0 && x < columns && y >= 0 && y < rows && filled[x, y];

        // Trwała siatka statyczna: front, tył i boki każdego pola konturu.
        // Nie jest to płaski billboard ani prostokąt zakrywający ornamenty.
        var vertices = new List<Vector3>(filledCount * 8);
        var uv = new List<Vector2>(filledCount * 8);
        var triangles = new List<int>(filledCount * 16);
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int index = vertices.Count;
            foreach (var v in new[] { a, b, c, d })
            {
                vertices.Add(P(v.x, v.y, v.z));
                uv.Add(new Vector2(v.x, v.y));
            }
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
            triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
        }
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            if (!filled[x, y]) continue;
            float x0 = signWidth * (x / (float)columns - .5f);
            float x1 = signWidth * ((x + 1) / (float)columns - .5f);
            float y0 = centreY + signHeight * (y / (float)rows - .5f);
            float y1 = centreY + signHeight * ((y + 1) / (float)rows - .5f);
            Face(new Vector3(x0,y0,faceZ),new Vector3(x0,y1,faceZ),
                 new Vector3(x1,y1,faceZ),new Vector3(x1,y0,faceZ));
            Face(new Vector3(x0,y0,backZ),new Vector3(x1,y0,backZ),
                 new Vector3(x1,y1,backZ),new Vector3(x0,y1,backZ));
            if (!Has(x-1,y))
                Face(new Vector3(x0,y0,faceZ),new Vector3(x0,y0,backZ),
                     new Vector3(x0,y1,backZ),new Vector3(x0,y1,faceZ));
            if (!Has(x+1,y))
                Face(new Vector3(x1,y0,backZ),new Vector3(x1,y0,faceZ),
                     new Vector3(x1,y1,faceZ),new Vector3(x1,y1,backZ));
            if (!Has(x,y-1))
                Face(new Vector3(x0,y0,backZ),new Vector3(x0,y0,faceZ),
                     new Vector3(x1,y0,faceZ),new Vector3(x1,y0,backZ));
            if (!Has(x,y+1))
                Face(new Vector3(x0,y1,faceZ),new Vector3(x0,y1,backZ),
                     new Vector3(x1,y1,backZ),new Vector3(x1,y1,faceZ));
        }
        var solidMesh = new Mesh
        {
            name = "BRAXHORN — przestrzenny kontur metalowej bryły",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        solidMesh.SetVertices(vertices);
        solidMesh.SetUVs(0, uv);
        solidMesh.SetTriangles(triangles, 0);
        solidMesh.RecalculateNormals();
        solidMesh.RecalculateTangents();
        solidMesh.RecalculateBounds();
        var sign = Group("BRAXHORN TRAINING CAMP — oryginalny szyld 3D", gate, Vector3.zero);
        var shell = Group("Metalowa bryła 26 cm — wytłoczona po kształcie grafiki", sign, Vector3.zero).gameObject;
        shell.AddComponent<MeshFilter>().sharedMesh = SaveMesh(solidMesh);
        var shellRenderer=shell.AddComponent<MeshRenderer>();
        shellRenderer.sharedMaterial = campusSteel;
        // Jeden uproszczony collider nad przejazdem, bez tysięcy kosztownych
        // trójkątów fizyki odpowiadających konturowi liter.
        var casingCollision = shell.AddComponent<BoxCollider>();
        casingCollision.center = solidMesh.bounds.center;
        casingCollision.size = solidMesh.bounds.size;

        // Okleina 1:1 ma swoje UV, dzięki czemu NIGDY nie odwraca liter.
        // Przyklejona 1 cm przed metalową powierzchnią, bez z-fightingu.
        var print = Group("Okleina — oryginalny JPEG na froncie bryły", sign, Vector3.zero).gameObject;
        var printMesh = new Mesh { name = "BRAXHORN — okleina UV 1do1" };
        float lo = centreY - signHeight * .5f, hi = centreY + signHeight * .5f;
        float decalZ = faceZ - .012f;
        printMesh.vertices = new [] {
            P(-signWidth*.5f,lo,decalZ),P(-signWidth*.5f,hi,decalZ),
            P(signWidth*.5f,hi,decalZ),P(signWidth*.5f,lo,decalZ)
        };
        printMesh.uv = new [] {
            new Vector2(0,0),new Vector2(0,1),
            new Vector2(1,1),new Vector2(1,0)
        };
        printMesh.triangles = new [] {0,1,2,0,2,3};
        printMesh.RecalculateNormals();printMesh.RecalculateTangents();printMesh.RecalculateBounds();
        print.AddComponent<MeshFilter>().sharedMesh = SaveMesh(printMesh);
        var printed = print.AddComponent<MeshRenderer>();
        printed.sharedMaterial = material;
        printed.shadowCastingMode = ShadowCastingMode.Off;
        printed.receiveShadows = false;

        // Tylna okleina jest również oryginalną grafiką, czytelną od środka
        // obozu. UV lustrzane względem geometrii, ale litery NIE są odwrócone
        // dla obserwatora stojącego po północnej stronie bramy.
        var backPrint = Group("Okleina — oryginalny JPEG na tylnej ścianie bryły",
            sign, Vector3.zero).gameObject;
        var backMesh = new Mesh { name = "BRAXHORN — tylna okleina UV 1do1" };
        float backDecalZ = backZ + .012f;
        backMesh.vertices = new [] {
            P(-signWidth*.5f,lo,backDecalZ),P(signWidth*.5f,lo,backDecalZ),
            P(signWidth*.5f,hi,backDecalZ),P(-signWidth*.5f,hi,backDecalZ)
        };
        backMesh.uv = new [] {
            new Vector2(1,0),new Vector2(0,0),
            new Vector2(0,1),new Vector2(1,1)
        };
        backMesh.triangles = new [] {0,1,2,0,2,3};
        backMesh.RecalculateNormals();backMesh.RecalculateTangents();backMesh.RecalculateBounds();
        backPrint.AddComponent<MeshFilter>().sharedMesh = SaveMesh(backMesh);
        var backRenderer = backPrint.AddComponent<MeshRenderer>();
        backRenderer.sharedMaterial = material;
        backRenderer.shadowCastingMode = ShadowCastingMode.Off;
        backRenderer.receiveShadows = false;

        // Rygiel nad pojazdami i podpory na zewnątrz przejazdu: x=+-4,45,
        // a nie w dawnych pozycjach +-3,7 wewnątrz światła bramy.
        // Jedna gruba DREWNIANA belka na pełnej szerokości bramy.
        // Nie ma podpór z cienkich metalowych patyków wewnątrz przejazdu.
        // Keep the real vehicle scale. Establish headroom from its measured
        // height and the road ray, instead of lowering the sign into traffic.
        float beamCentre=3.67f;
        var bus=root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t=>t.name==BraxObozParkedBus.Name);
        var roadOrigin=gate.TransformPoint(P(0,1,.28f));
        var road=Physics.RaycastAll(roadOrigin,Vector3.down,2f*K,~0,
            QueryTriggerInteraction.Ignore).Where(h=>h.collider&&h.normal.y>.7f&&
                !h.collider.transform.IsChildOf(gate))
            .OrderByDescending(h=>h.point.y).FirstOrDefault();
        if(bus&&road.collider)
        {
            var busBounds=BraxObozPlacement.Bounds(bus.GetComponentsInChildren<Renderer>(true));
            float roadY=gate.InverseTransformPoint(road.point).y/K;
            beamCentre=Mathf.Max(beamCentre,roadY+busBounds.size.y/K+.30f+.29f);
        }
        var beam=Cylinder(gate,"Jednolity bal ponad bramą — 9,4 m",
            new Vector3(-4.7f,beamCentre,.28f),new Vector3(4.7f,beamCentre,.28f),
            .29f,wood,true);
        foreach (float x in new[] {-4.45f,4.45f})
        {
            var post=Cylinder(gate,"Dwa grube pionowe słupy z bali",
                new Vector3(x,0,.28f),new Vector3(x,beamCentre,.28f),
                .34f,wood,true);
            BraxObozPlacement.OnGround(post.transform,post.GetComponentsInChildren<Renderer>(),
                gate.TransformPoint(P(x,0,.28f)),"słup bramy "+x);
        }
        // The old canvas centre (6.2 m) did not establish a physical join to
        // the beam. Mount the lowest vertex of the visible metal contour on
        // the beam's actual collider. Transparent JPEG margins are not feet.
        var beamBounds=beam.GetComponent<Renderer>().bounds;
        var contactGeometry=new Renderer[]{shellRenderer};
        bool supported=BraxObozPlacement.OnSupport(sign,contactGeometry,beam.transform,
            new Vector3(beamBounds.center.x,beamBounds.max.y,beamBounds.center.z),
            "oryginalny szyld na belce bramy");
        var placement=BraxObozPlacementRecord.For(sign);
        placement.SetMembers(sign.GetComponentsInChildren<Renderer>(true));
        placement.contactRenderers=contactGeometry;
        var gateParts=gate.GetComponentsInChildren<MeshFilter>()
            .Where(f=>!f.transform.IsChildOf(sign)).ToArray();
        BraxObozStructureAuthoring.Capture(gate,gateParts,
            gate.GetComponentsInChildren<Collider>().Where(c=>!c.transform.IsChildOf(sign)).ToArray());
        float mountedBottom=gate.InverseTransformPoint(shellRenderer.bounds.min).y/K;
        Debug.Log("BRAXHORN_OBOZ_SIGN_3D: originalVisibleBottom="+visibleSignBottom+
            " mountedVisibleBottom="+mountedBottom+" supportRay="+supported+" source=" + width + "x" + height +
            " metalDepth=0.26m stickerUV=1:1 supportX=+-4.45 opening=8.4m; test Unity wymagany");
    }

    static void CampusPerimeter()
    {
        var fence=Group("Ogrodzenie całego obozu",root,Vector3.zero);
        const int curveSegments=160;
        float radiusX=BraxObozCampusLayout.PlotRadiusX;
        float radiusZ=BraxObozCampusLayout.PlotRadiusZ;
        float centerZ=BraxObozCampusLayout.PlotCenterZ;
        float delta=Mathf.Asin(12f/radiusX);
        float start=-Mathf.PI*.5f+delta;
        float sweep=Mathf.PI*2-2*delta;
        Vector2 At(float a)=>new Vector2(radiusX*Mathf.Cos(a),centerZ+radiusZ*Mathf.Sin(a));
        // Obwód zamknięty z wyjątkiem prawdziwej bramy i furtki.
        // Wąski dolny łącznik utrzymuje oryginalny punkt bramy (0,-125).
        Vector2 first=At(start),last=At(start+sweep);
        for(int i=0;i<curveSegments;i++)
        {
            Vector2 p=At(start+sweep*i/curveSegments);
            Vector2 q=At(start+sweep*(i+1)/curveSegments);
            FenceLine(fence,p,q);
        }
        FenceLine(fence,new Vector2(12f,BraxObozCampusLayout.GateZ),first);
        FenceLine(fence,last,new Vector2(-12f,BraxObozCampusLayout.GateZ));
        FenceLine(fence,new Vector2(-12f,BraxObozCampusLayout.GateZ),
            new Vector2(-4.2f,BraxObozCampusLayout.GateZ));
        FenceLine(fence,new Vector2(4.2f,BraxObozCampusLayout.GateZ),
            new Vector2(5.8f,BraxObozCampusLayout.GateZ));
        FenceLine(fence,new Vector2(7.8f,BraxObozCampusLayout.GateZ),
            new Vector2(12f,BraxObozCampusLayout.GateZ));
        var gate=Group("Brama przesuwna · 8,4 m światła",root,P(0,0,-125));
        // Filarow nie dublujemy: dwa prawdziwe słupy drewniane
        // są integralną częścią OwnerGateSign i zachowują przejazd 8,4m.
        // Szyld 3D z okleiną i podporami poza światłem przejazdu.
        OwnerGateSign(gate);
        var leaf=Group("Skrzydło bramy",gate,Vector3.zero);
        Box(leaf,"Rama bramy",new Vector3(0,1.15f,0),new Vector3(8.2f,2.3f,.12f),campusSteel);
        for(int i=0;i<54;i++)Box(leaf,"Lamele bramy",new Vector3(-4+i*.15f,1.2f,-.09f),new Vector3(.095f,2.1f,.07f),wood,false);
        CombineDecoration(leaf);
        var controller=gate.gameObject.AddComponent<BraxObozSlidingGate>();controller.leaf=leaf;
        controller.openOffset=Vector3.left*(8.7f*K);controller.detectionRadius=24*K;
        var foot=Group("Furtka piesza",root,P(6.8f,0,-125));
        var footLeaf=Group("Skrzydło furtki",foot,Vector3.zero);
        Box(footLeaf,"Furtka",new Vector3(0,1.1f,0),new Vector3(1.8f,2.2f,.1f),wood);
        var footController=foot.gameObject.AddComponent<BraxObozSlidingGate>();footController.leaf=footLeaf;
        footController.openOffset=Vector3.right*(2.1f*K);footController.detectionRadius=5*K;
        // Droga do bramy pozostaje przejezdna; bez sztucznej zapory
        // i bez niewidzialnego collidera blokującego wjazd spoza obozu.
    }
    static void FenceLine(Transform parent,Vector2 a,Vector2 b)
    {
        float length=Vector2.Distance(a,b);int chunks=Mathf.CeilToInt(length/12);
        for(int chunk=0;chunk<chunks;chunk++){
            Vector2 p=Vector2.Lerp(a,b,chunk/(float)chunks),q=Vector2.Lerp(a,b,(chunk+1)/(float)chunks);float span=Vector2.Distance(p,q);
            var t=Group("Przęsło ogrodzenia",parent,P((p.x+q.x)*.5f,0,(p.y+q.y)*.5f));
            t.localRotation=Quaternion.Euler(0,-Mathf.Atan2(q.y-p.y,q.x-p.x)*Mathf.Rad2Deg,0);
            Box(t,"Podmurówka",new Vector3(0,.12f,0),new Vector3(span,.24f,.24f),campusPaving,false);
            foreach(float y in new[]{.45f,2.15f})Box(t,"Rygiel",new Vector3(0,y,0),new Vector3(span,.08f,.10f),campusSteel,false);
            int posts=Mathf.CeilToInt(span/3);
            for(int i=0;i<=posts;i++)Box(t,"Słupek",new Vector3(-span*.5f+span*i/posts,1.3f,0),new Vector3(.1f,2.6f,.1f),campusSteel,false);
            int slats=Mathf.FloorToInt(span/.15f);
            for(int i=0;i<slats;i++)Box(t,"Lamel drewniany",new Vector3(-span*.5f+(i+.5f)*span/slats,1.35f,-.08f),new Vector3(.095f,2.2f,.065f),wood,false);
            t.gameObject.AddComponent<BoxCollider>().center=P(0,1.3f,0);t.GetComponent<BoxCollider>().size=P(span,2.6f,.25f);
            CombineDecoration(t);
        }
    }
    static void CombineDecoration(Transform parent)
    {
        var filters=parent.GetComponentsInChildren<MeshFilter>()
            .Where(f=>f.sharedMesh && f.sharedMesh.subMeshCount==1 && f.GetComponent<MeshRenderer>() &&
                !f.GetComponentsInParent<BraxObozPlacementRecord>(true)
                    .Any(r=>r.Contains(f.GetComponent<Renderer>()))).ToArray();
        // Capture the actual construction before furniture is added. Preserve
        // individual pieces even through nested material batches.
        var construction=BraxObozStructureAuthoring.Capture(parent,filters,
            parent.GetComponentsInChildren<Collider>(true));
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var sources=group.ToArray();
            var mesh=new Mesh{name="Geometria zbiorcza",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(sources.Select(f=>new CombineInstance{mesh=f.sharedMesh,
                transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            mesh.RecalculateBounds();var target=Group("Geometria · "+group.Key.name,parent,Vector3.zero).gameObject;
            var targetFilter=target.AddComponent<MeshFilter>();targetFilter.sharedMesh=SaveMesh(mesh);
            target.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            BraxObozStructureAuthoring.RemapCombined(construction,sources,targetFilter);
        }
        foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
    }
    static void Lamp(Transform parent,Vector3 p)
    {
        var post=Group("Drewniany słup światła dojścia",parent,p*K);
        try
        {
            var pole=Cylinder(post,"Podparty słup z drewna",Vector3.zero,Vector3.up*3.5f,.12f,bark,true);
            var renderers=new[]{pole.GetComponent<Renderer>()};
            BraxObozPlacement.RealDimension(post,renderers,3.5f,BraxObozPlacement.Axis.Y,"drewniany słup dojścia");
            if(!BraxObozPlacement.OnGround(post,renderers,parent.TransformPoint(p*K),"drewniany słup dojścia"))
                throw new InvalidOperationException("brak rzeczywistej podpory drewnianego słupa dojścia");
            var record=BraxObozPlacementRecord.For(post);record.contactPoint=post;record.contactRenderers=renderers;
            // Fixture geometry is an explicit H gap. Do not pretend a box is
            // the owner's lamp. The existing local light follows the real post.
            BraxBuildQuality.Warn("BRAXHORN_OBOZ_OWNER_ASSETS_PENDING: oryginalna oprawa lampy dojścia; bez kostki zastępczej");
            var fixture=Group("Światło dojścia — oprawa oczekuje na model H",post,Vector3.up*(3.35f*K)).gameObject;
            var light=fixture.AddComponent<Light>();light.type=LightType.Point;
            light.useColorTemperature=true;light.colorTemperature=4000f;light.color=Color.white;
            light.intensity=1.5f;light.range=8*K;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForceVertex;
            fixture.AddComponent<BraxObozLocalLight>().source=light;
        }
        catch(Exception ex){Object.DestroyImmediate(post.gameObject);BraxBuildQuality.Warn("OBÓZ słup dojścia pominięty: "+ex);}
    }
    static void Bench(Transform parent,Vector3 p)
    {
        for(int i=0;i<4;i++)Box(parent,"Deska siedziska",p+new Vector3(0,.48f,-.22f+i*.145f),new Vector3(1.8f,.07f,.12f),wood);
        foreach(float x in new[]{-.65f,.65f}) {
            Box(parent,"Noga ławki",p+new Vector3(x,.23f,0),new Vector3(.08f,.46f,.5f),campusSteel);
            Box(parent,"Wspornik oparcia",p+new Vector3(x,.69f,.24f),new Vector3(.06f,.62f,.06f),campusSteel,false);
            Box(parent,"Podłokietnik",p+new Vector3(x,.7f,0),new Vector3(.075f,.06f,.47f),wood,false);
        }
        for(int i=0;i<3;i++)Box(parent,"Deska oparcia",p+new Vector3(0,.65f+i*.14f,.27f),new Vector3(1.8f,.11f,.055f),wood,false);
    }
    // Conservative source-metre AABB check for BOTH the full 4-lane oval
    // and the separate 100 m straight. Even boundary tiles must not sit on
    // the red tartan: no raised paving, decorative curb or invisible collider.
    static bool IntersectsRunningTrack(float x0,float x1,float z0,float z1,float safety=.06f)
    {
        float cx=0,cz=BraxObozCampusLayout.PlotCenterZ;
        float innerX=BraxObozCampusLayout.PlotRadiusX-8.8f;
        float innerZ=BraxObozCampusLayout.PlotRadiusZ-8.8f;
        float outerX=innerX+4*1.2f,outerZ=innerZ+4*1.2f;
        float closestX=x0>cx?x0-cx:x1<cx?cx-x1:0;
        float closestZ=z0>cz?z0-cz:z1<cz?cz-z1:0;
        float farX=Mathf.Max(Mathf.Abs(x0-cx),Mathf.Abs(x1-cx));
        float farZ=Mathf.Max(Mathf.Abs(z0-cz),Mathf.Abs(z1-cz));
        float innerFar=(farX*farX)/((innerX-safety)*(innerX-safety))+
            (farZ*farZ)/((innerZ-safety)*(innerZ-safety));
        float outerNear=(closestX*closestX)/((outerX+safety)*(outerX+safety))+
            (closestZ*closestZ)/((outerZ+safety)*(outerZ+safety));
        bool oval=outerNear<=1f && innerFar>=1f;
        float halfLength=50f,halfWidth=3f;
        bool straight=x1>=68f-halfWidth-safety&&x0<=68f+halfWidth+safety&&
            z1>=-24f-halfLength-safety&&z0<=-24f+halfLength+safety;
        return oval||straight;
    }

    static void PavedPaths(List<Rect> areas)
    {
        var t=Group("Leśne dojścia na płytach z drewnianym obrzeżem",root,Vector3.zero);
        var walkwayStone=BraxObozMaterials.OwnerSlabTilesPbr();
        var xs=areas.SelectMany(r=>new[]{r.xMin,r.xMax}).Distinct().OrderBy(v=>v).ToArray();
        var zs=areas.SelectMany(r=>new[]{r.yMin,r.yMax}).Distinct().OrderBy(v=>v).ToArray();
        bool Inside(float x,float z)=>areas.Any(r=>r.Contains(new Vector2(x,z)));
        int created=0;
        void Paver(float x0,float x1,float z0,float z1)
        {
            created+=TrackAwarePaving(t,x0,x1,z0,z1,walkwayStone,false);
        }
        void Curb(float x,float z,float w,float d)
        {
            TrackAwarePaving(t,x-w*.5f,x+w*.5f,z-d*.5f,z+d*.5f,wood,true);
        }
        for(int i=0;i<xs.Length-1;i++)
        for(int j=0;j<zs.Length-1;j++)
        {
            float x=(xs[i]+xs[i+1])*.5f,z=(zs[j]+zs[j+1])*.5f;
            if(!Inside(x,z))continue;
            float w=xs[i+1]-xs[i],d=zs[j+1]-zs[j];
            Paver(xs[i],xs[i+1],zs[j],zs[j+1]);
            if(!Inside(xs[i]-.001f,z))Curb(xs[i]-.04f,z,.08f,d);
            if(!Inside(xs[i+1]+.001f,z))Curb(xs[i+1]+.04f,z,.08f,d);
            if(!Inside(x,zs[j]-.001f))Curb(x,zs[j]-.04f,w,.08f);
            if(!Inside(x,zs[j+1]+.001f))Curb(x,zs[j+1]+.04f,w,.08f);
        }
        // Paving count is diagnostic, not a reason to waste a full Android
        // build; the alignment is verified in the source layout review.
        // Combined static geometry keeps draw calls low. All physics is built
        // only on the final clipped pavement mesh, never over running tracks.
        CombineDecoration(t);
        foreach(var mf in t.GetComponentsInChildren<MeshFilter>())
            if(mf.GetComponent<MeshRenderer>().sharedMaterial==walkwayStone)
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;
        Debug.Log("BRAXHORN_OBOZ_CHODNIKI_TRACK_CLIP created="+created+
            " exactTrackBoundary=true approachRise=.074 approachRun=1 pendingUnityVisual=true");
    }

    static void MetalRoof(Transform parent,float width,float depth,float eave,float rise)
    {
        var t=Group("Blacha na rąbek — dach, konstrukcja, odwodnienie",parent,Vector3.zero);
        float half=width*.5f, slope=Mathf.Sqrt(half*half+rise*rise);
        float angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
        for(int side=-1;side<=1;side+=2) {
            var plane=Group("Połać",t,P(side*half*.5f,eave+rise*.5f,0));
            plane.localRotation=Quaternion.Euler(0,0,-side*angle);
            Box(plane,"Pokrycie blaszane",Vector3.zero,new Vector3(slope,.035f,depth),metal,false);
            int seams=Mathf.CeilToInt(depth/.5f);
            for(int i=0;i<=seams;i++)
                Box(plane,"Rąbek stojący",new Vector3(0,.038f,-depth*.5f+i*depth/seams),new Vector3(slope,.04f,.018f),metal,false);
            foreach(float z in new[]{-depth*.5f,depth*.5f})
                Box(plane,"Wiatrownica",new Vector3(0,-.02f,z),new Vector3(slope,.16f,.055f),metal,false);
            Box(t,"Deska okapowa",new Vector3(side*half,eave-.1f,0),new Vector3(.1f,.22f,depth),wood,false);
            // Open U-channel gutter; downpipes discharge outside the footprint.
            float gutterX=side*(half+.09f);
            Box(t,"Dno rynny",new Vector3(gutterX,eave-.16f,0),new Vector3(.15f,.015f,depth),metal,false);
            foreach(float lip in new[]{-.075f,.075f})
                Box(t,"Krawędź rynny",new Vector3(gutterX+lip,eave-.105f,0),new Vector3(.015f,.11f,depth),metal,false);
            foreach(float z in new[]{-depth*.5f+.25f,depth*.5f-.25f}) {
                Cylinder(t,"Rura spustowa",new Vector3(gutterX,eave-.16f,z),new Vector3(gutterX,.28f,z),.045f,metal);
                Cylinder(t,"Wylewka",new Vector3(gutterX,.28f,z),new Vector3(gutterX+side*.25f,.12f,z),.045f,metal);
            }
        }
        Box(t,"Obróbka kalenicy",new Vector3(0,eave+rise+.03f,0),new Vector3(.18f,.08f,depth+.08f),metal,false);
        // Repeated timber trusses and longitudinal purlins visible from below.
        foreach(float side in new[]{-1f,1f})
            Box(t,"Płatew",new Vector3(side*half*.5f,eave+rise*.5f-RoofPurlinOffset,0),
                new Vector3(.16f,RoofPurlinHeight,depth),wood,false);
        int bays=Mathf.CeilToInt(depth/3);
        for(int i=0;i<=bays;i++) {
            float z=-depth*.5f+.25f+(depth-.5f)*i/bays;
            Box(t,"Pas dolny wiązara",new Vector3(0,eave-.18f,z),new Vector3(width,.24f,.14f),wood,false);
            foreach(float side in new[]{-1f,1f}) {
                // Its upper surface meets the sheet underside; the old
                // 15 cm centre offset left over 4 cm of empty air.
                var beam=Box(t,"Krokiew",new Vector3(side*half*.5f,eave+rise*.5f-.105f,z),new Vector3(slope,.18f,.12f),wood,false);
                beam.transform.localRotation=Quaternion.Euler(0,0,-side*angle);
            }
            Box(t,"Słupek wiązara",new Vector3(0,eave+rise*.5f-.18f,z),new Vector3(.12f,rise,.12f),wood,false);
        }
        CombineDecoration(t);
    }
    static void CabinFinish(Transform cabin)
    {
        var trim=Group("Wykończenie domku",cabin,Vector3.zero);
        MetalRoof(trim,7.1f,9.1f,2.85f,1.15f);
        var foundation=Box(trim,"Cokół fundamentowy",new Vector3(0,.075f,0),
            new Vector3(6.3f,.15f,8.3f),gravel,false);
        var terrain=root.GetComponentsInChildren<BraxObozStructureGround>(true)
            .FirstOrDefault(g=>g.surface&&g.geometry&&g.surface.sharedMesh==g.geometry.sharedMesh);
        if(terrain)BraxObozPlacement.OnSupport(foundation.transform,
            foundation.GetComponentsInChildren<Renderer>(true),terrain.surface.transform,
            cabin.position,"cokół domku — rzeczywisty grunt");
        else BraxBuildQuality.Warn("OBÓZ COKÓŁ: brak fizycznego terenu do raycastu; montaż niezweryfikowany");
        // Ganek z deskami i balustradą; skrzydło 0,90 × 2,10 m przed K.
        var ganek=Group("GANek z bali i poręczą",trim,Vector3.zero);
        Box(ganek,"Deski pomostu wejściowego",new Vector3(0,.065f,-5.1f),
            new Vector3(3.4f,.13f,2.1f),floor);
        foreach(float x in new[]{-1.65f,1.65f})
        {
            float postHeight=x<0?2.18f:1.03f;
            Cylinder(ganek,"Słup balustrady",
                new Vector3(x,.14f,-5.92f),new Vector3(x,postHeight,-5.92f),
                .10f,wood,true);
            Cylinder(ganek,"Słup balustrady przy domu",
                new Vector3(x,.14f,-4.22f),new Vector3(x,postHeight,-4.22f),
                .10f,wood,true);
            Cylinder(ganek,"Pochwyt boczny z drewna",
                new Vector3(x,1.02f,-5.92f),new Vector3(x,1.02f,-4.22f),
                .08f,wood,false);
        }
        // Both anchors are inside full-height posts. The line runs along the
        // side of the porch, leaving the whole doorway and approach clear.
        Cylinder(ganek,"Sznur na ręczniki",
            new Vector3(-1.65f,2.05f,-5.92f),new Vector3(-1.65f,2.05f,-4.22f),
            .012f,rope,true);
        foreach(float x in new[]{-3.13f,3.13f})foreach(float z in new[]{-4.13f,4.13f})
            Box(trim,"Deska narożna",new Vector3(x,1.5f,z),new Vector3(.17f,2.7f,.17f),wood,false);
        foreach(float z in new[]{-4.05f,4.05f}) {
            var mesh=new Mesh{name="Zamknięty szczyt"};
            var frontVertices=new[]{P(-3.15f,2.78f,z),P(3.15f,2.78f,z),P(3.15f,2.985f,z),P(0,4,z),P(-3.15f,2.985f,z)};
            var frontUv=frontVertices.Select(v=>new Vector2(v.x,v.y)).ToArray();
            // Changing vertex count invalidates existing Mesh UV arrays.
            // Assemble both complete arrays before assigning either one.
            mesh.vertices=frontVertices.Concat(frontVertices).ToArray();
            mesh.uv=frontUv.Concat(frontUv).ToArray();
            mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,5,6,7,5,7,8,5,8,9};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var g=Group("Szczyt z desek",trim,Vector3.zero).gameObject;
            g.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);g.AddComponent<MeshRenderer>().sharedMaterial=wood;
        }
        var glass=BraxObozMaterials.Plain("szyby-domkow",new Color(.65f,.75f,.79f,.2f),.9f);
        glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",1);glass.SetInt("_DstBlend",10);
        glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;
        foreach(float side in new[]{-1f,1f}) {
            Box(trim,"Zamknięcie pod okapem",new Vector3(side*3,2.93f,0),new Vector3(.14f,.24f,8.2f),wood,false);
            foreach(float z in new[]{-1.1f,0,1.1f})Box(trim,"Rama okienna",new Vector3(side*3.08f,1.55f,z),new Vector3(.16f,1.08f,.07f),wood,false);
            foreach(float y in new[]{1.03f,2.07f})Box(trim,"Rama pozioma",new Vector3(side*3.08f,y,0),new Vector3(.16f,.08f,2.26f),wood,false);
            Box(trim,"Szyba",new Vector3(side*3.07f,1.55f,0),new Vector3(.015f,.96f,2.14f),glass,false);
            Box(trim,"Parapet",new Vector3(side*3.12f,1.0f,0),new Vector3(.36f,.055f,2.4f),metal,false);
        }
        foreach(float x in new[]{-.55f,.55f})
        {
            Box(trim,"Futryna zewnetrzna",new Vector3(x,1.295f,-4.17f),
                new Vector3(.16f,2.27f,.16f),wood,false);
            Box(trim,"Ciemna uszczelka futryny",new Vector3(Mathf.Sign(x)*.4625f,1.22f,-4.18f),
                new Vector3(.025f,2.10f,.025f),dark,false);
        }
        Box(trim,"Opaska gorna drzwi",new Vector3(0,2.35f,-4.17f),
            new Vector3(1.26f,.16f,.16f),wood,false);
        CombineDecoration(trim);
    }
    public static void ReplaceServiceRoof(Transform sourceGroup,Transform model,float width,float depth)
    {
        // The authored GLB joins walls, roof and furniture in StaticVisual.
        // Strip only the known upper shell in a generated mesh copy; never
        // edit the imported source asset or the articulated door hierarchy.
        int removed=0;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            bool isStatic=false;
            for(var node=filter.transform;node && node!=model.parent;node=node.parent)
                if(node.name=="StaticVisual"){isStatic=true;break;}
            if(!isStatic)continue;
            var original=filter.sharedMesh;
            if(!original || !original.isReadable)throw new InvalidOperationException("Nieczytelna siatka zaplecza: "+sourceGroup.name);
            var vertices=original.vertices;
            var matrix=sourceGroup.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var points=vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
            var mesh=Object.Instantiate(original);int removedHere=0;
            for(int sub=0;sub<mesh.subMeshCount;sub++)
            {
                if(mesh.GetTopology(sub)!=MeshTopology.Triangles)throw new InvalidOperationException("Nieobsługiwana siatka zaplecza");
                var triangles=mesh.GetTriangles(sub);var kept=new List<int>(triangles.Length);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=points[triangles[i]];var b=points[triangles[i+1]];var c=points[triangles[i+2]];
                    float high=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
                    bool outside=Mathf.Max(Mathf.Abs(a.x),Mathf.Max(Mathf.Abs(b.x),Mathf.Abs(c.x)))>width*.5f+.13f ||
                        Mathf.Max(Mathf.Abs(a.z),Mathf.Max(Mathf.Abs(b.z),Mathf.Abs(c.z)))>depth*.5f+.13f;
                    if(high>2.901f || (high>2.80f && outside)){removedHere++;continue;}
                    kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
                }
                mesh.SetTriangles(kept,sub);
            }
            if(removedHere==0){Object.DestroyImmediate(mesh);continue;}
            mesh.RecalculateBounds();filter.sharedMesh=SaveMesh(mesh);removed+=removedHere;
        }
        if(removed==0)throw new InvalidOperationException("Nie znaleziono starego dachu zaplecza: "+sourceGroup.name);
        var shell=Group(sourceGroup.name+" — nowy dach",sourceGroup.parent,sourceGroup.localPosition);
        float half=width*.5f+.45f,rise=half*.38f,eave=2.94f;
        MetalRoof(shell,half*2,depth+.8f,eave,rise);
        foreach(float z in new[]{-depth*.5f,depth*.5f})
        {
            float wallTop=eave+rise*(1-width*.5f/half);
            var mesh=new Mesh{name="Szczyt zaplecza"};
            var v=new[]{P(-width*.5f,2.9f,z),P(width*.5f,2.9f,z),
                P(width*.5f,wallTop,z),P(0,eave+rise,z),P(-width*.5f,wallTop,z)};
            mesh.vertices=v.Concat(v).ToArray();mesh.uv=mesh.vertices.Select(p=>new Vector2(p.x,p.y)).ToArray();
            mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,5,6,7,5,7,8,5,8,9};
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var face=Group("Szczyt z desek",shell,Vector3.zero).gameObject;
            face.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);face.AddComponent<MeshRenderer>().sharedMaterial=wood;
        }
        foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})
            Box(shell,"Wykończenie narożnika",new Vector3(x,1.52f,z),new Vector3(.17f,2.74f,.17f),wood,false);
        CombineDecoration(shell);
        Debug.Log("BRAXHORN_OBOZ wymieniono dach: "+sourceGroup.name+" usunięte trójkąty="+removed);
    }
}


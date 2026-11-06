using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BraxObozServices
{
    const string Root="Assets/Source/Prywatne/Oboz/environment/services/";
    const float K=1.294f;
    [Serializable] public sealed class Box { public float[] position,size;public float yaw; }
    [Serializable] public sealed class Door { public string pivot;public float[] position,colliderCenter,colliderSize; }
    [Serializable] public sealed class Seat { public string name;public float[] position;public float yaw;public bool lie; }
    [Serializable] public sealed class Facility { public string kind,name;public float[] position,effectPosition; }
    [Serializable] public sealed class Fixture { public float[] position;public float range,intensity; }
    [Serializable] public sealed class Model { public string id;public float worldScale;public Box[] colliders;public Door[] doors;public Seat[] seats;public Facility[] facilities;public Fixture[] lights; }
    static Vector3 V(float[] a)
    {
        if(a==null||a.Length!=3||a.Any(x=>float.IsNaN(x)||float.IsInfinity(x)))throw new InvalidOperationException("Błędne współrzędne wyposażenia");
        return new Vector3(a[0],a[1],a[2]);
    }
    static Transform Node(string name,Transform parent,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    public static void AddAll(Transform parent)
    {
        var mats=new Dictionary<string,Material>{
            ["wood"]=BraxObozMaterials.Pbr("wood"),["floor"]=BraxObozMaterials.Pbr("floor"),["metal"]=BraxObozMaterials.Pbr("metal"),["towel"]=BraxObozMaterials.Pbr("towel"),
            ["white"]=BraxObozMaterials.Plain("wyposazenie-jasne",new Color(.78f,.77f,.70f),.32f),
            ["burgundy"]=BraxObozMaterials.Plain("bordo",new Color(.478f,.118f,.118f)),
            ["rubber"]=BraxObozMaterials.Plain("guma",new Color(.035f,.035f,.038f),.08f),
            ["canvas"]=BraxObozMaterials.Plain("plotno-robocze",new Color(.356f,.372f,.227f),.12f)};
        var glass=BraxObozMaterials.Plain("szklo-budynkow",new Color(.25f,.4f,.45f,.18f),.8f);
        glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;mats["glass"]=glass;
        var lamp=BraxObozMaterials.Plain("zarowki",new Color(1,.7f,.32f));lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.7f,.32f)*2);mats["lamp"]=lamp;
        foreach(var item in new[]{("canteen","Stołówka",-40f,-70f),("sauna","Sauna",-47f,-85f),("medical","Punkt medyczny",-25f,-85f),("storage","Magazyn",-25f,-67f)})
            Add(item.Item1,item.Item2,parent,new Vector3(item.Item3,0,item.Item4),mats);
        // Osobny blok sanitarny obok istniejących pryszniców i sauny.
        // Nie zastępuje oryginalnych modeli, nie zamyka ich drzwi ani dojść.
        SanitaryBlock(parent,mats);
    }
    static void Add(string id,string label,Transform parent,Vector3 position,Dictionary<string,Material> mats)
    {
        var data=JsonUtility.FromJson<Model>(File.ReadAllText(Root+id+".json"));
        if(data.id!=id||Mathf.Abs(data.worldScale-K)>.00001f||data.doors.Length!=1)throw new InvalidOperationException("Nieprawidłowa zabudowa: "+id);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+id+".glb");if(!asset)throw new InvalidOperationException("Brak modelu "+id);
        var group=Node(label,parent,position*K);group.localScale=Vector3.one*K;
        var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.transform.SetParent(group,false);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m.name.Replace("OBOZ_","");int suffix=key.IndexOf(" (",StringComparison.Ordinal);if(suffix>=0)key=key.Substring(0,suffix);
                if(!mats.TryGetValue(key,out var material))throw new InvalidOperationException("Nieznany materiał "+m.name);return material;
            }).ToArray();
        float width=id=="canteen"?12:id=="sauna"?7:8;
        float depth=id=="canteen"?9:6;
        BraxObozEnvironment.ReplaceServiceRoof(group,model.transform,width,depth);
        foreach(var box in data.colliders)
        {
            var t=Node("Kolizja zgodna z otworami",group,V(box.position));t.localRotation=Quaternion.Euler(0,box.yaw,0);t.gameObject.AddComponent<BoxCollider>().size=V(box.size);
        }
        foreach(var d in data.doors)
        {
            var pivot=model.GetComponentsInChildren<Transform>().Single(t=>t.name==d.pivot);
            var controller=Node("Drzwi — "+label,group,V(d.position)).gameObject.AddComponent<BraxCityDoor>();
            controller.id="oboz-"+id;controller.label=label;controller.leaves=new[]{pivot};controller.sides=new[]{1f};
            var collider=pivot.gameObject.AddComponent<BoxCollider>();collider.center=V(d.colliderCenter);collider.size=V(d.colliderSize);
        }
        foreach(var s in data.seats)
        {
            var t=Node(s.name,group,V(s.position));t.localRotation=Quaternion.Euler(0,s.yaw,0);
            var seat=t.gameObject.AddComponent<BraxSeat>();seat.label=s.name;seat.lie=s.lie;seat.surfaceY=t.position.y;seat.hipsAboveRoot=.95f*K;
        }
        foreach(var f in data.facilities)
        {
            // Meal and storage markers reserve the supplied layout. They are
            // not exposed as working actions until their behavior is supplied.
            if(!Enum.TryParse(f.kind,out BraxObozFacility.Kind kind))continue;
            var t=Node(f.name,group,V(f.position));var facility=t.gameObject.AddComponent<BraxObozFacility>();facility.kind=kind;
            if(kind!=BraxObozFacility.Kind.Drink)facility.effect=Effect(group,V(f.effectPosition),kind==BraxObozFacility.Kind.Sauna);
        }
        foreach(var f in data.lights ?? Array.Empty<Fixture>())
        {
            var t=Node("Lampa wnętrza",group,V(f.position));var light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;
            light.color=new Color(1,.82f,.60f);light.intensity=f.intensity;light.range=f.range*K;light.shadows=LightShadows.None;
            t.gameObject.AddComponent<BraxObozLocalLight>().source=light;
        }
        if(id=="canteen")FurnishCanteen(group);
    }
    // Zasoby stołówki są prawdziwymi GLB z dostarczonej paczki. Żaden
    // detal kuchni ani mapy PBR nie jest redukowany; bez duplikatów modeli.
    static void FurnishCanteen(Transform room)
    {
        // Przedni pas jest wolny na wejście i podejścia do stołów.
        var kitchen=OwnerFixture(room,"kitchen_-_assets.glb",
            new Vector3(-4.0f,.24f,2.6f),new Vector3(2.35f,2.30f,2.4f));
        OwnerFixture(room,"glass-food-display-showcase-with-trays.glb",
            new Vector3(3.35f,.24f,2.5f),new Vector3(2.05f,1.6f,1.2f));
        OwnerFixture(room,"picnic_set_1.glb",
            new Vector3(-2.45f,.24f,-1.2f),new Vector3(1.95f,.85f,2.35f));
        OwnerFixture(room,"picnic_set_1.glb",
            new Vector3(2.45f,.24f,-1.2f),new Vector3(1.95f,.85f,2.35f));
        // Real owner cutlery, cookware and serving tray, positioned by
        // the ORIGINAL surfaces of the kitchen model (not arbitrary float Y).
        OwnerOnExistingSurface(room,kitchen,"CookingTable_Shelf",
            "24-piece-cutlery-set.glb",new Vector3(.65f,.11f,.42f),
            new Vector3(.34f,0,.12f));
        OwnerOnExistingSurface(room,kitchen,"CookingTable_Shelf",
            "food_tray.glb",new Vector3(.58f,.09f,.40f),
            new Vector3(-.34f,0,-.13f));
        OwnerOnExistingSurface(room,kitchen,"PortableStove_Grate_L",
            "stockpot.glb",new Vector3(.43f,.37f,.43f),Vector3.zero);
        Debug.Log("BRAXHORN_OBOZ_CANTEEN: oryginalna kuchnia GLB, witryna i 2 komplety stołowe; "+
            "przejścia i miejsca siedzące wymagają testu Unity Play Mode");
    }

    // Place original owner models directly on a measured imported FBX/GLB
    // surface. This avoids relying on a guessed world Y when the source scale
    // changes. Requires a Unity render review for overlap/accessibility.
    static void OwnerOnExistingSurface(Transform room,GameObject model,
        string partPrefix,string filename,Vector3 maximumSize,Vector3 localOffset)
    {
        var part=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(
            t=>t.name.StartsWith(partPrefix,StringComparison.OrdinalIgnoreCase));
        if(!part)throw new InvalidOperationException(
            "Brak powierzchni oryginalnej kuchni dla "+filename+": "+partPrefix);
        var r=part.GetComponentsInChildren<Renderer>(true);
        if(r.Length==0)throw new InvalidOperationException(
            "Brak mesha powierzchni oryginalnej kuchni: "+partPrefix);
        Bounds b=r[0].bounds;foreach(var x in r.Skip(1))b.Encapsulate(x.bounds);
        Vector3 topWorld=new Vector3(b.center.x,b.max.y+.012f*1.294f,b.center.z);
        var local=room.InverseTransformPoint(topWorld)+localOffset;
        OwnerFixture(room,filename,local,maximumSize);
    }

    // Rzeczywisty budynek WC: dwie kabiny, dwa stanowiska prysznicowe,
    // umywalki, lustra, wentylacja i dostępne dla gracza fizyczne wejście.
    // Wszystkie wymiary lokalne są w metrach; K stosuje wyłącznie rodzic.
    static GameObject FixtureBox(Transform parent,string name,Vector3 position,Vector3 size,
        Material material,bool solid=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name=name;g.transform.SetParent(parent,false);
        g.transform.localPosition=position;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    // PBR tiles use measured metre UV on EVERY face, not one stretched 2K
    // image over an entire 12-metre bathroom wall. Source mesh is retained.
    static GameObject PbrWall(Transform parent,string label,Vector3 pos,
        Vector3 dimensions,Material source,bool solid=true)
    {
        var wall=FixtureBox(parent,label,pos,dimensions,source,solid);
        var filter=wall.GetComponent<MeshFilter>();
        var mesh=Object.Instantiate(filter.sharedMesh);
        var verts=mesh.vertices;var normals=mesh.normals;
        var uv=new Vector2[verts.Length];
        for(int i=0;i<verts.Length;i++)
        {
            Vector3 metre=Vector3.Scale(verts[i],dimensions);
            Vector3 n=normals[i];
            uv[i]=Mathf.Abs(n.y)>.90f?new Vector2(metre.x,metre.z):
                Mathf.Abs(n.x)>.90f?new Vector2(metre.z,metre.y):
                new Vector2(metre.x,metre.y);
        }
        mesh.uv=uv;mesh.RecalculateTangents();
        filter.sharedMesh=BraxObozStableMesh.Save(mesh);
        return wall;
    }

    static GameObject FixtureCylinder(Transform parent,string name,Vector3 position,
        float radius,float height,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);
        g.GetComponent<Renderer>().sharedMaterial=material;
        return g;
    }
    // The cabins and open-air training stations have unscaled parent roots:
    // their primitive geometry is authored in source metres and multiplied
    // by K inside Box(). OwnerFixture() by contrast expects Unity LOCAL units.
    // Convert just the placement to K here; do NOT rescale original GLB mesh.
    public static GameObject OwnerFixtureSourceMetres(Transform parent,string filename,
        Vector3 bottomCenterMetres,Vector3 maxSourceMetres)
    {
        return OwnerFixture(parent,filename,bottomCenterMetres*K,maxSourceMetres);
    }

    // Model łóżka IKEA IDANAS — dokładnie dostarczony przez właściciela GLB,
    // nie generować nóg, materaca, poduszki ani koca z brył zastępczych.
    // Kabiny mają obrót +/-90°, więc wszystkie wymiary mierzymy w ich
    // lokalnym układzie, a nie względem osi globalnych.
    public static GameObject OwnerSingleBed(Transform cabin,Vector3 bottomCenterSourceMetres)
    {
        const string filename="OwnerExtras/Bedroom/ikea_idanas_single_bed.glb";
        const string sourcePath="Assets/Source/Prywatne/Oboz/OwnerAssets/"+filename;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if(!source)throw new InvalidOperationException(
            "Brak oryginalnego łóżka jednoosobowego IKEA IDANAS — przerwij generowanie obozu: "+sourcePath);
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Nie można instancjonować oryginalnego łóżka: "+sourcePath);
        model.name="Łóżko 3D właściciela — IKEA IDANAS — oryginalny GLB";
        model.transform.SetParent(cabin,false);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Łóżko GLB nie ma renderera");

        Bounds LocalBounds()
        {
            Bounds result=new Bounds();bool first=true;
            foreach(var renderer in renderers)
            {
                Bounds b=renderer.bounds;
                for(int ix=-1;ix<=1;ix+=2)
                for(int iy=-1;iy<=1;iy+=2)
                for(int iz=-1;iz<=1;iz+=2)
                {
                    var local=cabin.InverseTransformPoint(b.center+
                        Vector3.Scale(b.extents,new Vector3(ix,iy,iz)));
                    if(first){result=new Bounds(local,Vector3.zero);first=false;}
                    else result.Encapsulate(local);
                }
            }
            return result;
        }
        var bounds=LocalBounds();
        if(bounds.size.x>.001f&&bounds.size.z>.001f && bounds.size.x>bounds.size.z*1.10f)
        {
            model.transform.localRotation=Quaternion.Euler(0,90,0)*model.transform.localRotation;
            bounds=LocalBounds();
        }
        if(bounds.size.x<.001f||bounds.size.y<.001f||bounds.size.z<.001f)
            throw new InvalidOperationException("Błędny rozmiar źródłowego łóżka IDANAS");
        // Cabin source metres are scaled exactly once by the game's K=1.294.
        var max=new Vector3(1.16f,1.25f,2.30f)*K;
        float uniform=Mathf.Min(max.x/bounds.size.x,
            Mathf.Min(max.y/bounds.size.y,max.z/bounds.size.z));
        if(uniform<=0f||float.IsNaN(uniform)||float.IsInfinity(uniform))
            throw new InvalidOperationException("Nieprawidłowa skala łóżka IDANAS");
        model.transform.localScale*=uniform;
        bounds=LocalBounds();
        var centerSource=bottomCenterSourceMetres*K;
        var shift=centerSource-
            new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        model.transform.position+=cabin.TransformVector(shift);
        bounds=LocalBounds();
        // Contact proxy for a single bed only; never block the doorway or
        // make an invisible collider spanning an entire cabin.
        if(!model.GetComponentInChildren<Collider>())
        {
            var collider=model.AddComponent<BoxCollider>();
            var localCenter=new Vector3(bounds.center.x,
                bounds.min.y+Mathf.Min(bounds.size.y*.53f,.78f*K)*.5f,
                bounds.center.z);
            collider.center=model.transform.InverseTransformPoint(cabin.TransformPoint(localCenter));
            var lo=model.transform.lossyScale;
            collider.size=new Vector3(
                bounds.size.x/Mathf.Max(.001f,Mathf.Abs(lo.x)),
                Mathf.Min(bounds.size.y*.53f,.78f*K)/Mathf.Max(.001f,Mathf.Abs(lo.y)),
                bounds.size.z/Mathf.Max(.001f,Mathf.Abs(lo.z)));
        }
        Debug.Log("BRAXHORN_OBOZ_OWNER_BED: "+filename+
            " worldScale=K cabin="+cabin.name+" footprintLocal="+bounds.size+
            " visualDeviceReview=pending");
        return model;
    }

    // Prywatne 3D wyposażenie sanitariatów — bez zastępowania siatek, UV i PBR.
    // Jednorodna skala: wyłącznie dopasowanie do stanowiska w murowanym WC.
    public static GameObject OwnerFixture(Transform parent,string filename,
        Vector3 bottomCenter,Vector3 maxLocalSize)
    {
        const string prefix="Assets/Source/Prywatne/Oboz/OwnerAssets/";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefix+filename);
        if(!source)throw new InvalidOperationException("Brak właścicielskiego modelu sanitariatu: "+filename);
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Błąd importu GLB sanitariatu: "+filename);
        model.name="Właściciel / oryginalny "+filename;
        model.transform.SetParent(parent,false);
        var renderers=model.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Brak renderera "+filename);
        Bounds bounds=renderers[0].bounds;
        foreach(var rr in renderers.Skip(1))bounds.Encapsulate(rr.bounds);
        if(bounds.size.x<=.0001f||bounds.size.y<=.0001f||bounds.size.z<=.0001f)
            throw new InvalidOperationException("Zerowe wymiary modelu: "+filename);
        float kx=maxLocalSize.x*K/bounds.size.x;
        float ky=maxLocalSize.y*K/bounds.size.y;
        float kz=maxLocalSize.z*K/bounds.size.z;
        float uniform=Mathf.Min(kx,Mathf.Min(ky,kz));
        if(uniform<=0f||float.IsNaN(uniform)||float.IsInfinity(uniform))
            throw new InvalidOperationException("Błąd skalowania modelu: "+filename);
        model.transform.localScale*=uniform;
        bounds=renderers[0].bounds;
        foreach(var rr in renderers.Skip(1))bounds.Encapsulate(rr.bounds);
        var target=parent.TransformPoint(bottomCenter);
        model.transform.position+=new Vector3(target.x-bounds.center.x,
            target.y-bounds.min.y,target.z-bounds.center.z);
        // Korpus jest nieruchomy: zachowujemy pojedynczy fizyczny hitbox
        // odpowiadający siatce; nie wstawiamy kolizji przez drzwi.
        if(!model.GetComponentInChildren<Collider>())
        {
            var box=model.AddComponent<BoxCollider>();
            box.center=model.transform.InverseTransformPoint(bounds.center);
            Vector3 lossy=model.transform.lossyScale;
            box.size=new Vector3(bounds.size.x/Mathf.Abs(lossy.x),
                bounds.size.y/Mathf.Abs(lossy.y),bounds.size.z/Mathf.Abs(lossy.z));
        }
        return model;
    }

    static void SanitaryBlock(Transform parent,Dictionary<string,Material> mats)
    {
        const string label="BRAXHORN — WC / ŁAŹNIA / UMYWALNIA";
        var building=Node(label,parent,new Vector3(-66,0,-52)*K);
        building.localScale=Vector3.one*K;
        // Real CC0 Poly Haven surfaces: white ceramic indoors, dark concrete
        // cladding outside. Source textures must pass 2K MD5 verification.
        var tile=BraxObozMaterials.PublicPbr("long_white_tiles",1.3f);
        var facade=BraxObozMaterials.PublicPbr("rectangular_facade_tiles",2f);
        var porcelain=BraxObozMaterials.Plain("sanitariaty-porcelana",new Color(.94f,.95f,.92f),.70f);
        var dark=BraxObozMaterials.Plain("sanitariaty-spoiny",new Color(.15f,.18f,.20f),.22f);
        var mirror=BraxObozMaterials.Plain("sanitariaty-lustro",new Color(.54f,.68f,.71f),.88f,.06f);
        var rubber=mats["rubber"];var metal=mats["metal"];var wood=mats["wood"];
        var darkTile=BraxObozMaterials.OwnerSlabTilesPbr();
        var tileFloor=FixtureBox(building,"Oryginalny ciemny gres 2K — podłoga",
            new Vector3(0,.12f,0),new Vector3(12,.24f,10),darkTile);
        // Skala UV w metrach: kamienne płytki powtarzalne, bez rozciągnięcia
        // jednej mapy 2K na całe 12 x 10 m.
        var tileFilter=tileFloor.GetComponent<MeshFilter>();
        var tileMesh=Object.Instantiate(tileFilter.sharedMesh);
        var tileVertices=tileMesh.vertices;
        var tileNormals=tileMesh.normals;
        var tileUv=new Vector2[tileVertices.Length];
        for(int i=0;i<tileVertices.Length;i++)
        {
            Vector3 p=Vector3.Scale(tileVertices[i],new Vector3(12,.24f,10));
            Vector3 n=tileNormals[i];
            tileUv[i]=Mathf.Abs(n.y)>.9f?new Vector2(p.x,p.z)*.5f:
                Mathf.Abs(n.x)>.9f?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f;
        }
        tileMesh.uv=tileUv;tileMesh.RecalculateTangents();
        tileFilter.sharedMesh=BraxObozStableMesh.Save(tileMesh);
        PbrWall(building,"Białe płytki ściany południowej 2K",new Vector3(0,1.70f,-5f),new Vector3(12,3.25f,.20f),tile);
        PbrWall(building,"Białe płytki ściany zachodniej 2K",new Vector3(-6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        PbrWall(building,"Białe płytki ściany wschodniej 2K",new Vector3(6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        // Prawdziwy otwór wejściowy 2.8m, bez niewidzialnego collidera.
        foreach(float x in new[]{-3.7f,3.7f})
            PbrWall(building,"Białe płytki przy wejściu",new Vector3(x,1.70f,5f),new Vector3(4.6f,3.25f,.20f),tile);
        PbrWall(building,"Białe płytki nad wejściem",new Vector3(0,2.81f,5f),new Vector3(2.85f,.95f,.23f),tile);
        // Thin noncolliding cladding OUTSIDE the existing solid wall. The
        // owner's concrete building remains, door opening remains 2.8m.
        PbrWall(building,"Ciemna elewacja Poly Haven — południe",
            new Vector3(0,1.70f,-5.13f),new Vector3(12.25f,3.25f,.035f),facade,false);
        PbrWall(building,"Ciemna elewacja Poly Haven — zachód",
            new Vector3(-6.13f,1.70f,0),new Vector3(.035f,3.25f,10.25f),facade,false);
        PbrWall(building,"Ciemna elewacja Poly Haven — wschód",
            new Vector3(6.13f,1.70f,0),new Vector3(.035f,3.25f,10.25f),facade,false);
        foreach(float x in new[]{-3.7f,3.7f})
            PbrWall(building,"Ciemna elewacja Poly Haven — front i ościeże",
                new Vector3(x,1.70f,5.13f),new Vector3(4.6f,3.25f,.035f),facade,false);
        PbrWall(building,"Ciemny pas elewacji nad wejściem",
            new Vector3(0,2.81f,5.14f),new Vector3(2.85f,.95f,.035f),facade,false);
        FixtureBox(building,"Próg kamienny",new Vector3(0,.06f,5.12f),new Vector3(2.75f,.12f,.34f),tile);
        FixtureBox(building,"Dach hydroizolowany",new Vector3(0,3.42f,0),new Vector3(12.6f,.23f,10.6f),dark);
        foreach(float x in new[]{-5.75f,5.75f})
            FixtureBox(building,"Obróbka dachu",new Vector3(x,3.53f,0),new Vector3(.18f,.25f,10.6f),metal,false);
        // Oddzielna strefa WC (zachód) oraz pryszniców (wschód);
        // korytarz od wejścia pozostaje szeroki i przechodni.
        PbrWall(building,"Białe płytki 2K — podział stref",new Vector3(0,1.45f,-2.0f),
            new Vector3(.12f,2.65f,6.0f),tile);
        PbrWall(building,"Białe płytki 2K — WC",new Vector3(-3.05f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        PbrWall(building,"Białe płytki 2K — prysznice",new Vector3(3.02f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        foreach(float x in new[]{-4.5f,-1.58f})
        {
            var cubicle=Node("Kabina WC — "+x,building,new Vector3(x,0,-2.95f));
            OwnerFixture(cubicle,"close-coupled-toilet.glb",
                new Vector3(0,.24f,0),new Vector3(.70f,1.10f,1.02f));
            FixtureBox(cubicle,"Przycisk spłukiwania",new Vector3(0,1.34f,-.24f),
                new Vector3(.17f,.025f,.085f),metal,false);
            var flush=Node("SPŁUCZ TOALETĘ",cubicle,new Vector3(0,1.32f,-.15f))
                .gameObject.AddComponent<BraxObozFacility>();
            flush.kind=BraxObozFacility.Kind.Toilet;
            // Ościeżnica i zamknięte boki kabiny; wejście ma faktyczne
            // światło 1.72 m, bez niewidzialnej ściany na przejściu.
            FixtureBox(cubicle,"Lewa ościeżnica WC",new Vector3(-1.21f,1.25f,1.48f),
                new Vector3(.38f,2.15f,.12f),wood);
            FixtureBox(cubicle,"Prawa ościeżnica WC",new Vector3(1.02f,1.25f,1.48f),
                new Vector3(.65f,2.15f,.12f),wood);
            FixtureBox(cubicle,"Nadproże kabiny WC",new Vector3(0,2.25f,1.48f),
                new Vector3(2.78f,.25f,.12f),tile,false);
            var seat=Node("Usiądź — toaleta",cubicle,new Vector3(0,.69f,.10f)).gameObject.AddComponent<BraxSeat>();
            seat.label="TOALETA";seat.lie=false;seat.surfaceY=seat.transform.position.y;seat.hipsAboveRoot=.95f*K;
            // Drzwi każdej kabiny otwierają się, skrzydło ma własną kolizję.
            var hinge=Node("Zawias drzwi kabiny",cubicle,new Vector3(-1.02f,.18f,1.48f));
            FixtureBox(hinge,"Skrzydło kabiny",new Vector3(.86f,.95f,0),
                new Vector3(1.72f,1.9f,.055f),wood);
            var door=hinge.gameObject.AddComponent<BraxCityDoor>();
            door.id="oboz-wc-"+(x< -3?"1":"2");door.label="KABINA WC";
            door.leaves=new[]{hinge};door.sides=new[]{1f};
        }
        foreach(float x in new[]{1.5f,4.52f})
        {
            var shower=Node("Kabina prysznicowa — "+x,building,new Vector3(x,0,-2.95f));
            OwnerFixture(shower,"shower-tray-square.glb",
                new Vector3(0,.24f,0),new Vector3(1.65f,.14f,1.65f));
            FixtureCylinder(shower,"Odpływ prysznicowy",new Vector3(0,.29f,0),
                .10f,.015f,metal);
            FixtureBox(shower,"Panel baterii",new Vector3(0,1.15f,-1.55f),
                new Vector3(.27f,.48f,.10f),metal,false);
            FixtureCylinder(shower,"Deszczownica",new Vector3(0,2.46f,-1.30f),.16f,.07f,metal);
            var control=Node("Odkręć prysznic",shower,new Vector3(0,1.15f,-1.1f)).gameObject.AddComponent<BraxObozFacility>();
            control.kind=BraxObozFacility.Kind.Shower;
            control.effect=Effect(shower,new Vector3(0,2.37f,-1.28f),false);
            // Ścianka przeciwbryzgowa, otwarte przejście 1.2 m.
            FixtureBox(shower,"Szkło przy wejściu do prysznica",new Vector3(1.02f,1.23f,1.43f),
                new Vector3(.72f,2.08f,.04f),mats["glass"],false);
            FixtureBox(shower,"Profil szyby",new Vector3(.66f,1.23f,1.43f),
                new Vector3(.055f,2.10f,.07f),metal,false);
        }
        foreach(float x in new[]{-4.1f,4.1f})
        {
            // Sink rear must touch the north interior wall (z=4.90).
            // Previous z=3.4 left a 1.2m gap, making basins look airborne.
            var sink=Node("Umywalka z lustrem — przy ścianie",building,new Vector3(x,0,4.6f));
            OwnerFixture(sink,"trough-washbasin.glb",new Vector3(0,.70f,0),new Vector3(1.20f,.35f,.55f));
            FixtureBox(sink,"Syfon",new Vector3(0,.54f,-.13f),new Vector3(.18f,.5f,.14f),metal);
            FixtureBox(sink,"Bateria",new Vector3(0,1.04f,-.29f),new Vector3(.09f,.26f,.12f),metal);
            FixtureBox(sink,"Lustro na ścianie",new Vector3(0,1.89f,.275f),new Vector3(1.25f,1.25f,.035f),mirror,false);
            foreach(float bracket in new[]{-.43f,.43f}){
                FixtureBox(sink,"Stalowy wspornik ścienny umywalki",
                    new Vector3(bracket,.66f,.61f),new Vector3(.07f,.14f,.55f),metal);
                FixtureBox(sink,"Kotwa do ściany pod umywalką",
                    new Vector3(bracket,.65f,.895f),new Vector3(.14f,.42f,.06f),metal);
            }
            // Oryginalny wieszak i ręcznik GLB, bez proceduralnej atrapy.
            var towel=OwnerFixture(sink,"towel-rail-with-towel.glb",
                new Vector3(.63f,1.19f,.10f),new Vector3(.70f,.55f,.32f));
            var towelCollider=towel.GetComponent<Collider>();
            if(towelCollider)Object.DestroyImmediate(towelCollider);
            var tap=Node("UMYJ RĘCE — KRAN",sink,new Vector3(0,1.04f,-.19f))
                .gameObject.AddComponent<BraxObozFacility>();
            tap.kind=BraxObozFacility.Kind.Sink;
            tap.effect=Effect(sink,new Vector3(0,1.04f,-.15f),false,true);
        }
        // Prawdziwe pisuary 3D na zachodniej ścianie; nie zastępuj ich bryłami.
        foreach(float z in new[]{.2f,2.0f})
            OwnerFixture(building,"wall-urinal.glb",new Vector3(-5.62f,.55f,z),
                new Vector3(.52f,.95f,.38f));
        // The owner's independent towel GLB rests on a physically modelled
        // wall shelf; avoid another floating decorative sanitary object.
        FixtureBox(building,"Półka na ręczniki — ściana łaźni",
            new Vector3(-5f,1.47f,3.78f),new Vector3(.85f,.07f,.50f),metal);
        OwnerFixture(building,"bath-towel.glb",
            new Vector3(-5f,1.515f,3.78f),new Vector3(.72f,.25f,.43f));
        // Neutralne oprawy LED w sanitariatach: normalne światło do użytku,
        // aktywne jedynie blisko gracza (BraxObozLocalLight), bez ciężkich cieni.
        foreach(float x in new[]{-4f,0f,4f})
        {
            var fixture=Node("Oprawa LED — łazienka",building,new Vector3(x,3.04f,0.8f));
            FixtureBox(fixture,"Mleczny klosz",Vector3.zero,
                new Vector3(.72f,.065f,.72f),porcelain,false);
            var lamp=fixture.gameObject.AddComponent<Light>();
            lamp.type=LightType.Point;lamp.color=new Color(1f,.97f,.91f);
            lamp.intensity=.92f;lamp.range=6f*K;lamp.shadows=LightShadows.None;
            lamp.enabled=false;
            var local=fixture.gameObject.AddComponent<BraxObozLocalLight>();
            local.source=lamp;local.range=12f*K;
        }
        Debug.Log("BRAXHORN_OBOZ_SANITARIATY dwa WC, dwie kabiny z prysznicem, umywalki; wymagany test przejścia i pozycji gracza");
    }

    static ParticleSystem Effect(Transform parent,Vector3 position,bool steam,bool faucet=false)
    {
        var t=Node(steam?"Para przy piecu":faucet?"Woda z kranu":"Strumień prysznica",parent,position);
        t.localRotation=Quaternion.Euler(steam?-90:90,0,0);var particles=t.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=steam?70:120;
        main.startLifetime=steam?2.0f:faucet?.16f:.52f;
        main.startSpeed=steam?.35f:faucet?1.1f:4.1f;
        main.startSize=steam?.22f:faucet?.010f:.018f;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=particles.emission;emission.rateOverTime=steam?18:faucet?45:180;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;
        shape.angle=steam?20:3;shape.radius=steam?.15f:faucet?.018f:.08f;
        var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});color.color=gradient;
        var shader=Shader.Find("Braxhorn/ObozWaterParticle");if(!shader)throw new InvalidOperationException("Brak shadera wody/pary");
        string path=BraxObozCharacters.Generated+"/Materials/"+(steam?"steam":"shower")+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(shader);material.SetColor("_Color",steam?new Color(.85f,.85f,.80f,.13f):new Color(.6f,.78f,.85f,.38f));AssetDatabase.CreateAsset(material,path);}
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;return particles;
    }
    public static void BindPlayer(BraxObozSceneInfo info)
    {
        foreach(var f in info.GetComponentsInChildren<BraxObozFacility>(true))f.player=info.player;
        foreach(var f in info.GetComponentsInChildren<BraxObozLocalLight>(true))f.player=info.player;
        var hips=info.player.visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("Hips",StringComparison.Ordinal));
        if(hips)
        {
            float offset=hips.position.y-info.player.transform.position.y;
            foreach(var seat in info.GetComponentsInChildren<BraxSeat>(true))seat.hipsAboveRoot=offset;
        }
    }
}


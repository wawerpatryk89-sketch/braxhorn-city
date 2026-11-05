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
        foreach(var item in new[]{("canteen","Stołówka",-58f,-65f),("showers","Prysznice",-95f,-65f),("sauna","Sauna",-115f,-65f),("medical","Punkt medyczny",-46f,-90f),("storage","Magazyn",-24f,-90f)})
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
    static GameObject FixtureCylinder(Transform parent,string name,Vector3 position,
        float radius,float height,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;
        g.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);
        g.GetComponent<Renderer>().sharedMaterial=material;
        return g;
    }
    static void SanitaryBlock(Transform parent,Dictionary<string,Material> mats)
    {
        const string label="BRAXHORN — WC / ŁAŹNIA / UMYWALNIA";
        var building=Node(label,parent,new Vector3(-92,0,-83)*K);
        building.localScale=Vector3.one*K;
        var tile=BraxObozMaterials.Plain("sanitariaty-ceramika",new Color(.86f,.89f,.87f),.65f);
        var porcelain=BraxObozMaterials.Plain("sanitariaty-porcelana",new Color(.94f,.95f,.92f),.70f);
        var dark=BraxObozMaterials.Plain("sanitariaty-spoiny",new Color(.15f,.18f,.20f),.22f);
        var mirror=BraxObozMaterials.Plain("sanitariaty-lustro",new Color(.54f,.68f,.71f),.88f,.06f);
        var rubber=mats["rubber"];var metal=mats["metal"];var wood=mats["wood"];
        FixtureBox(building,"Płytki gresowe — podłoga",new Vector3(0,.12f,0),new Vector3(12,.24f,10),tile);
        FixtureBox(building,"Ściana południowa",new Vector3(0,1.70f,-5f),new Vector3(12,3.25f,.20f),tile);
        FixtureBox(building,"Ściana zachodnia",new Vector3(-6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        FixtureBox(building,"Ściana wschodnia",new Vector3(6f,1.70f,0),new Vector3(.20f,3.25f,10),tile);
        // Prawdziwy otwór wejściowy 2.8m, bez niewidzialnego collidera.
        foreach(float x in new[]{-3.7f,3.7f})
            FixtureBox(building,"Ściana z wejściem",new Vector3(x,1.70f,5f),new Vector3(4.6f,3.25f,.20f),tile);
        FixtureBox(building,"Nadproże wejścia",new Vector3(0,2.81f,5f),new Vector3(2.85f,.95f,.23f),tile);
        FixtureBox(building,"Próg kamienny",new Vector3(0,.06f,5.12f),new Vector3(2.75f,.12f,.34f),tile);
        FixtureBox(building,"Dach hydroizolowany",new Vector3(0,3.42f,0),new Vector3(12.6f,.23f,10.6f),dark);
        foreach(float x in new[]{-5.75f,5.75f})
            FixtureBox(building,"Obróbka dachu",new Vector3(x,3.53f,0),new Vector3(.18f,.25f,10.6f),metal,false);
        // Oddzielna strefa WC (zachód) oraz pryszniców (wschód);
        // korytarz od wejścia pozostaje szeroki i przechodni.
        FixtureBox(building,"Ścianka pomiędzy strefami",new Vector3(0,1.45f,-2.0f),
            new Vector3(.12f,2.65f,6.0f),tile);
        FixtureBox(building,"Ścianka kabin WC",new Vector3(-3.05f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        FixtureBox(building,"Ścianka kabin prysznicowych",new Vector3(3.02f,1.30f,-2.65f),
            new Vector3(.10f,2.20f,4.25f),tile);
        foreach(float x in new[]{-4.5f,-1.58f})
        {
            var cubicle=Node("Kabina WC — "+x,building,new Vector3(x,0,-2.95f));
            FixtureBox(cubicle,"Miska z odpływem",new Vector3(0,.34f,0),new Vector3(.62f,.54f,.88f),porcelain);
            FixtureCylinder(cubicle,"Siedzisko ceramiczne",new Vector3(0,.65f,.10f),.36f,.08f,porcelain);
            FixtureCylinder(cubicle,"Wewnętrzny odpływ",new Vector3(0,.701f,.10f),.21f,.012f,dark);
            FixtureBox(cubicle,"Spłuczka z przyciskiem",new Vector3(0,1.01f,-.39f),
                new Vector3(.70f,.65f,.24f),porcelain);
            FixtureBox(cubicle,"Przycisk spłukiwania",new Vector3(0,1.34f,-.24f),
                new Vector3(.17f,.025f,.085f),metal,false);
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
            FixtureBox(shower,"Brodzik antypoślizgowy",new Vector3(0,.23f,0),
                new Vector3(2.65f,.09f,3.30f),rubber);
            FixtureCylinder(shower,"Odpływ prysznicowy",new Vector3(0,.29f,0),
                .10f,.015f,metal);
            FixtureBox(shower,"Panel baterii",new Vector3(0,1.15f,-1.55f),
                new Vector3(.27f,.48f,.10f),metal,false);
            FixtureCylinder(shower,"Deszczownica",new Vector3(0,2.46f,-1.30f),.16f,.07f,metal);
            var control=Node("Odkręć prysznic",shower,new Vector3(0,1.15f,-1.1f)).gameObject.AddComponent<BraxObozFacility>();
            control.kind=BraxObozFacility.Kind.Shower;
            control.effect=Effect(shower,new Vector3(0,2.37f,-1.28f),false);
        }
        foreach(float x in new[]{-4.1f,4.1f})
        {
            var sink=Node("Umywalka z lustrem",building,new Vector3(x,0,3.4f));
            FixtureBox(sink,"Misa porcelanowa",new Vector3(0,.89f,0),new Vector3(1.12f,.18f,.64f),porcelain);
            FixtureBox(sink,"Syfon",new Vector3(0,.54f,-.13f),new Vector3(.18f,.5f,.14f),metal);
            FixtureBox(sink,"Bateria",new Vector3(0,1.04f,-.29f),new Vector3(.09f,.26f,.12f),metal);
            FixtureBox(sink,"Lustro",new Vector3(0,1.89f,.48f),new Vector3(1.25f,1.25f,.035f),mirror,false);
            FixtureBox(sink,"Wieszak na ręcznik",new Vector3(.63f,1.19f,.10f),
                new Vector3(.04f,.06f,.47f),metal,false);
        }
        Debug.Log("BRAXHORN_OBOZ_SANITARIATY dwa WC, dwie kabiny z prysznicem, umywalki; wymagany test przejścia i pozycji gracza");
    }

    static ParticleSystem Effect(Transform parent,Vector3 position,bool steam)
    {
        var t=Node(steam?"Para przy piecu":"Strumień prysznica",parent,position);
        t.localRotation=Quaternion.Euler(steam?-90:90,0,0);var particles=t.gameObject.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.playOnAwake=false;main.loop=true;main.maxParticles=steam?70:120;
        main.startLifetime=steam?2.0f:.52f;main.startSpeed=steam?.35f:4.1f;main.startSize=steam?.22f:.018f;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=particles.emission;emission.rateOverTime=steam?18:180;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=steam?20:3;shape.radius=steam?.15f:.08f;
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


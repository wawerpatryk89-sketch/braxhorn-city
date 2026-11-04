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

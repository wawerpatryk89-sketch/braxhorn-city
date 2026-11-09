using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxCitySystemsBuild
{
    // 9.11: metra w mieście nie ma (decyzja właściciela) — żadnego wykopu w bruku promenady.
    public const float PavementY = .15f;
    public static readonly Rect[] CrossingCurbCuts = {
        new Rect(50f,22.9f,4f,1.15f), new Rect(50f,32.95f,4f,1.15f)
    };
    private const string Output = "Assets/PlayableGenerated/CityRebuildSystems";
    private static int assetIndex;
    private static Material black, steel, white, brass, signText;
    private static Material redOn, amberOn, greenOn, redOff, amberOff, greenOff;

    public static void Apply(BraxCityWorld world, Transform parent, Material stone, Material concrete, Material metal, Material glass)
    {
        if (!world || world.area != "city" || !parent || !stone || !concrete || !metal)
            throw new InvalidOperationException("City systems require the actual city and assigned materials");
        if (Vector3.Distance(parent.lossyScale,Vector3.one) > .001f || Quaternion.Angle(parent.rotation,Quaternion.identity) > .01f)
            throw new InvalidOperationException("City systems parent must preserve Unity metre scale and axes");
        var previous = parent.Find("City mission access systems");
        if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        BraxZapisAssetu.UsunFolder(Output, "systemy miasta");
        Directory.CreateDirectory(Output); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); assetIndex = 0;
        var root = new GameObject("City mission access systems").transform;
        root.SetParent(parent,false); root.position=Vector3.zero; root.rotation=Quaternion.identity;
        PrepareMaterials(metal,concrete);
        var catalogue = PrepareReservations(world,root);
        var controller = BuildSignals(world,root,concrete);
        BuildCrossing(root,stone,concrete);
        var telemetry = root.gameObject.AddComponent<BraxCitySystemsTelemetry>();
        telemetry.world=world; telemetry.traffic=controller; telemetry.catalogue=catalogue;
        BraxCitySystemsValidation.Validate(world,catalogue,controller,root);
        AssetDatabase.SaveAssets();
        Debug.Log("BRAX_CITY_SYSTEMS_PREPARED reserves="+catalogue.reservations.Length+" metro=brak_9_11; runtime/visual validation still required");
    }

    private static Material Copy(Material source, string name, Color color)
    {
        var material = new Material(source) { name = name };
        if (material.HasProperty("_Color")) material.SetColor("_Color",color);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.24f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.24f);
        BraxZapisAssetu.Utworz(material,Output+"/"+name+".mat"); return material;
    }

    private static Material Lens(string name, Color color)
    {
        var shader=Shader.Find("Braxhorn/CitySignalLens");
        if (!shader) throw new InvalidOperationException("City signal lens shader is missing");
        var material=new Material(shader){name=name}; material.SetColor("_Color",color);
        BraxZapisAssetu.Utworz(material,Output+"/"+name+".mat"); return material;
    }

    private static void PrepareMaterials(Material metal, Material concrete)
    {
        black=Copy(metal,"Traffic graphite enamel",new Color(.055f,.061f,.066f));
        steel=Copy(metal,"City stainless details",new Color(.35f,.38f,.40f));
        white=Copy(concrete,"City marking paint",new Color(.82f,.81f,.75f));
        brass=Copy(metal,"Tactile ochre metal",new Color(.47f,.35f,.12f));
        redOn=Lens("Signal red active",new Color(1f,.025f,.009f));
        amberOn=Lens("Signal amber active",new Color(1f,.45f,.015f));
        greenOn=Lens("Signal green active",new Color(.015f,.8f,.24f));
        redOff=Lens("Signal red extinguished",new Color(.046f,.018f,.014f));
        amberOff=Lens("Signal amber extinguished",new Color(.04f,.031f,.015f));
        greenOff=Lens("Signal green extinguished",new Color(.014f,.033f,.021f));
        var fontShader=Shader.Find("Braxhorn/CitySignText");
        if(!fontShader)throw new InvalidOperationException("Depth tested city sign shader is missing");
        signText=new Material(fontShader){name="City sign depth tested text"};
        signText.mainTexture=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;
        BraxZapisAssetu.Utworz(signText,Output+"/CitySignText.mat");
    }

    private static BraxMissionReservations PrepareReservations(BraxCityWorld world, Transform parent)
    {
        string path=Path.Combine(Application.dataPath,"../Docs/AI/CityRebuildSystems.json");
        var doc=JsonUtility.FromJson<BraxCitySystemsDocument>(File.ReadAllText(path));
        if (doc==null || doc.reservations==null || doc.reservations.Length!=11) throw new InvalidOperationException("Exactly eleven documented reservations are required");
        var points=world.points.ToList();
        foreach (var anchor in doc.anchors)
        {
            var current=points.FirstOrDefault(p=>p.id==anchor.id);
            if (current!=null)
            {
                if (Vector3.Distance(current.position,anchor.position)>.025f)
                    throw new InvalidOperationException("Source anchor moved; integration needs review: "+anchor.id);
            }
            else
            {
                if (anchor.requireExisting) throw new InvalidOperationException("Original anchor missing: "+anchor.id);
                points.Add(new BraxCityPoint{id=anchor.id,label=anchor.label,position=anchor.position});
            }
        }
        world.points=points.ToArray();
        var result=parent.gameObject.AddComponent<BraxMissionReservations>();
        result.world=world; result.reservations=doc.reservations; return result;
    }

    private static GameObject MeshObject(string name, Transform parent, BraxCitySystemMesh data, Material material, bool solid=false, bool isStatic=true)
    {
        var obj=new GameObject(name); obj.transform.SetParent(parent,false);
        var mesh=data.Build(name); BraxZapisAssetu.Utworz(mesh,Output+"/mesh_"+(assetIndex++).ToString("D3")+".asset");
        obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        if (solid) obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        if (isStatic) GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
        return obj;
    }

    private static void SolidBox(string name, Transform parent, Vector3 center, Vector3 size, Material material, float textureMetres=1)
    {
        var data=new BraxCitySystemMesh(); data.Box(center,size,textureMetres);
        var obj=MeshObject(name,parent,data,material);
        var collider=obj.AddComponent<BoxCollider>(); collider.center=center; collider.size=size;
    }

    private static void Pole(string name, Transform root, Vector3 at, float height)
    {
        var metal=new BraxCitySystemMesh();
        metal.Tube(at,at+Vector3.up*height,.065f,16,.045f);
        metal.Tube(at,at+Vector3.up*.38f,.12f,16,.08f);
        metal.Box(at+Vector3.up*.035f,new Vector3(.31f,.07f,.31f));
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {
            var bolt=at+new Vector3(x*.105f,.075f,z*.105f);
            metal.Tube(bolt,bolt+Vector3.up*.035f,.022f,6);
        }
        var obj=MeshObject(name,root,metal,black);
        var collider=obj.AddComponent<CapsuleCollider>(); collider.center=at+Vector3.up*(height*.5f); collider.height=height; collider.radius=.1f;
    }

    private static BraxCityTrafficController BuildSignals(BraxCityWorld world,Transform root,Material concrete)
    {
        var signals=new GameObject("Single controlled pedestrian crossing X52").transform; signals.SetParent(root,false);
        var controller=signals.gameObject.AddComponent<BraxCityTrafficController>(); controller.world=world;
        var vr=new List<BraxSignalLamp>(); var va=new List<BraxSignalLamp>(); var vg=new List<BraxSignalLamp>();
        var pr=new List<BraxSignalLamp>(); var pg=new List<BraxSignalLamp>();
        VehicleHead(signals,new Vector3(49.1f,PavementY,23.5f),-90,vr,va,vg);
        VehicleHead(signals,new Vector3(54.9f,PavementY,33.5f),90,vr,va,vg);
        PedestrianHead(signals,new Vector3(54.2f,PavementY,23.55f),0,pr,pg);
        PedestrianHead(signals,new Vector3(49.8f,PavementY,33.45f),180,pr,pg);
        controller.vehicleRed=vr.ToArray(); controller.vehicleAmber=va.ToArray(); controller.vehicleGreen=vg.ToArray();
        controller.pedestrianRed=pr.ToArray(); controller.pedestrianGreen=pg.ToArray();
        // Compact control cabinet, hinge, handle and vents: one physical object.
        var box=new BraxCitySystemMesh(); box.RoundedPanel(new Vector3(56.6f,.74f,34.8f),.62f,1.1f,.4f,.035f);
        box.Box(new Vector3(56.6f,.235f,34.8f),new Vector3(.7f,.17f,.48f));
        var cabinet=MeshObject("Signal controller cabinet",signals,box,steel);
        var col=cabinet.AddComponent<BoxCollider>(); col.center=new Vector3(56.6f,.73f,34.8f); col.size=new Vector3(.7f,1.15f,.48f);
        var details=new BraxCitySystemMesh();
        for(int i=0;i<7;i++)details.Box(new Vector3(56.6f,.47f+i*.046f,35.006f),new Vector3(.36f,.012f,.014f));
        details.Tube(new Vector3(56.83f,.86f,35.03f),new Vector3(56.83f,1.02f,35.03f),.012f,8);
        MeshObject("Cabinet vents and latch",signals,details,black);
        return controller;
    }

    private static void VehicleHead(Transform root,Vector3 at,float yaw,List<BraxSignalLamp> red,List<BraxSignalLamp> amber,List<BraxSignalLamp> green)
    {
        var pole=new GameObject("Vehicle signal "+at.x).transform; pole.SetParent(root,false); pole.position=at; pole.rotation=Quaternion.Euler(0,yaw,0);
        Pole("Enamel pole, flange and anchors",pole,Vector3.zero,3.55f);
        var housing=new BraxCitySystemMesh();
        housing.RoundedPanel(new Vector3(0,2.86f,.12f),.47f,1.27f,.25f,.07f);
        housing.Tube(new Vector3(0,2.45f,0),new Vector3(0,2.45f,.14f),.033f,12);
        housing.Tube(new Vector3(0,3.28f,0),new Vector3(0,3.28f,.14f),.033f,12);
        foreach(float y in new[]{3.24f,2.86f,2.48f})
        {
            housing.Ring(new Vector3(0,y,.25f),.126f,.147f,.022f);
            housing.Ring(new Vector3(0,y,.27f),.145f,.154f,.23f,-15,195,20);
        }
        MeshObject("Rounded signal housing with lens visors",pole,housing,black);
        red.Add(Lamp("Red lens",pole,new Vector3(0,3.24f,.28f),redOn,redOff,false,false));
        amber.Add(Lamp("Amber lens",pole,new Vector3(0,2.86f,.28f),amberOn,amberOff,false,false));
        green.Add(Lamp("Green lens",pole,new Vector3(0,2.48f,.28f),greenOn,greenOff,false,false));
    }

    private static void PedestrianHead(Transform root,Vector3 at,float yaw,List<BraxSignalLamp> red,List<BraxSignalLamp> green)
    {
        var pole=new GameObject("Pedestrian signal "+at.x).transform; pole.SetParent(root,false); pole.position=at; pole.rotation=Quaternion.Euler(0,yaw,0);
        Pole("Pedestrian pole and anchors",pole,Vector3.zero,2.85f);
        var housing=new BraxCitySystemMesh(); housing.RoundedPanel(new Vector3(0,2.25f,.12f),.43f,.8f,.24f,.055f);
        foreach(float y in new[]{2.44f,2.06f})
        {
            housing.Ring(new Vector3(0,y,.248f),.141f,.158f,.025f);
            housing.Ring(new Vector3(0,y,.273f),.155f,.163f,.16f,-15,195,18);
            housing.Tube(new Vector3(0,y,.25f),new Vector3(0,y,.267f),.14f,24);
        }
        housing.RoundedPanel(new Vector3(0,1.05f,.11f),.14f,.22f,.1f,.025f);
        MeshObject("Pedestrian housing, hoods and request box",pole,housing,black);
        red.Add(Lamp("Standing pedestrian",pole,new Vector3(0,2.44f,.28f),redOn,redOff,true,false));
        green.Add(Lamp("Walking pedestrian",pole,new Vector3(0,2.06f,.28f),greenOn,greenOff,true,true));
        var request=new BraxCitySystemMesh(); request.Tube(new Vector3(0,1.05f,.166f),new Vector3(0,1.05f,.177f),.033f,16);
        MeshObject("Pedestrian button face",pole,request,brass);
    }

    private static BraxSignalLamp Lamp(string name,Transform parent,Vector3 at,Material lit,Material unlit,bool pedestrian,bool walking)
    {
        var lens=new BraxCitySystemMesh();
        if(!pedestrian)lens.Tube(at,at+Vector3.forward*.014f,.125f,32);
        else
        {
            lens.Tube(at+new Vector3(0,.081f,0),at+new Vector3(0,.081f,.008f),.025f,16);
            var trunk=at+new Vector3(0,.018f,.005f);
            lens.Tube(trunk+Vector3.up*.035f,trunk-Vector3.up*.04f,.019f,8);
            if(walking)
            {
                lens.Tube(trunk+Vector3.up*.027f,trunk+new Vector3(-.061f,-.017f,0),.013f,8);
                lens.Tube(trunk+Vector3.up*.027f,trunk+new Vector3(.051f,.048f,0),.013f,8);
                lens.Tube(trunk-Vector3.up*.033f,at+new Vector3(-.064f,-.091f,.005f),.015f,8);
                lens.Tube(trunk-Vector3.up*.033f,at+new Vector3(.062f,-.074f,.005f),.015f,8);
            }
            else
            {
                for(int s=-1;s<=1;s+=2)
                {
                    lens.Tube(trunk+new Vector3(s*.018f,.025f,0),trunk+new Vector3(s*.036f,-.038f,0),.012f,8);
                    lens.Tube(trunk+new Vector3(s*.01f,-.033f,0),at+new Vector3(s*.024f,-.094f,.005f),.014f,8);
                }
            }
        }
        var obj=MeshObject(name,parent,lens,unlit,false,false);
        return new BraxSignalLamp{surface=obj.GetComponent<Renderer>(),illuminated=lit,extinguished=unlit};
    }

    private static void BuildCrossing(Transform root,Material stone,Material concrete)
    {
        var crossing=new GameObject("Zebra crossing and lowered kerbs").transform; crossing.SetParent(root,false);
        var markings=new BraxCitySystemMesh();
        for(float z=24.15f;z<32.9f;z+=.9f)markings.Box(new Vector3(52,.012f,z+.22f),new Vector3(3.8f,.008f,.44f));
        // One stop line for each approach, leaving a clear buffer to the zebra.
        markings.Box(new Vector3(48.7f,.012f,26.3f),new Vector3(.35f,.008f,4.4f));
        markings.Box(new Vector3(55.3f,.012f,30.7f),new Vector3(.35f,.008f,4.4f));
        MeshObject("Daylight road paint",crossing,markings,white);
        foreach(var cut in CrossingCurbCuts)
        {
            bool near=cut.y<30; float z0=cut.yMin,z1=cut.yMax;
            float y0=near?PavementY:.01f,y1=near?.01f:PavementY;
            var ramp=new BraxCitySystemMesh();
            ramp.Quad(new Vector3(cut.xMin,y0,z0),new Vector3(cut.xMin,y1,z1),new Vector3(cut.xMax,y1,z1),new Vector3(cut.xMax,y0,z0),1);
            ramp.Quad(new Vector3(cut.xMin,-.16f,z0),new Vector3(cut.xMin,-.16f,z1),new Vector3(cut.xMin,y1,z1),new Vector3(cut.xMin,y0,z0));
            ramp.Quad(new Vector3(cut.xMax,-.16f,z1),new Vector3(cut.xMax,-.16f,z0),new Vector3(cut.xMax,y0,z0),new Vector3(cut.xMax,y1,z1));
            ramp.Quad(new Vector3(cut.xMax,-.16f,z0),new Vector3(cut.xMin,-.16f,z0),new Vector3(cut.xMin,y0,z0),new Vector3(cut.xMax,y0,z0));
            ramp.Quad(new Vector3(cut.xMin,-.16f,z1),new Vector3(cut.xMax,-.16f,z1),new Vector3(cut.xMax,y1,z1),new Vector3(cut.xMin,y1,z1));
            MeshObject(near?"Near kerb accessible ramp":"Far kerb accessible ramp",crossing,ramp,stone,true);
            var studs=new BraxCitySystemMesh();
            for(float x=50.2f;x<53.9f;x+=.15f)for(int row=0;row<3;row++)
            {
                float z=near?z0+.16f+row*.15f:z1-.16f-row*.15f;
                float y=Mathf.Lerp(y0,y1,(z-z0)/(z1-z0))+.004f;
                studs.Tube(new Vector3(x,y,z),new Vector3(x,y+.006f,z),.025f,8,.021f);
            }
            MeshObject("Tactile warning studs "+(near?"near":"far"),crossing,studs,brass);
        }
    }

    // 9.11 (właściciel: „metro całkowicie usunąć i zaplanować od zera”): stara stacja G5 (BuildMetro, BuildMetroStation,
    // BuildMetroSigns — schody przy promenadzie x 77–80, z 35–50) usunięta z kodu. Text() zostaje dla innych systemów.

    private static void Text(Transform parent,string name,string text,Vector3 position,float yaw,float heightMetres,Color color)
    {
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(0,yaw,0);
        var textMesh=obj.AddComponent<TextMesh>();textMesh.text=text;textMesh.fontSize=64;textMesh.characterSize=heightMetres*10f/64f;
        textMesh.anchor=TextAnchor.MiddleCenter;textMesh.alignment=TextAlignment.Center;textMesh.color=color;
        textMesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        obj.GetComponent<MeshRenderer>().sharedMaterial=signText;
        obj.AddComponent<BraxCitySignText>();
    }
}

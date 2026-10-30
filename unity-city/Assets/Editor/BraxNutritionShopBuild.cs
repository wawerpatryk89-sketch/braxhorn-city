using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Whole Nutrition location, in the existing footprint and original city.
// All new retail models are original authored mesh assets, not scene primitives.
public static class BraxNutritionShopBuild
{
    const string Source="Assets/Nutrition/";
    const string Output="Assets/PlayableGenerated/Nutrition";
    static readonly Dictionary<Material,Material> converted=new Dictionary<Material,Material>();
    static GameObject kit;
    static Shader interiorShader;
    static int materialSerial,productCount,shelfCount;
    static readonly string[] RequiredParts={"WheyTub","CreatineJar","Shaker","ShelvingBay","Counter","DisplayIsland","LinearPendant","FloorTile"};
    static Material KitMaterial(string name)
    {
        var source=kit.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)
            .Where(m=>m&&m.name==name).Distinct().SingleOrDefault();
        if(!source||!source.HasProperty("baseColorTexture")||!source.GetTexture("baseColorTexture"))
            throw new InvalidOperationException("Nutrition embedded material/map is unavailable: "+name);
        return source;
    }
    // The kit embeds the exact source artwork/maps. Use those persistent
    // glTFast textures, rather than inferring a second standalone importer.
    public static void ValidateSources()
    {
        kit=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Models/NutritionKit.glb");
        interiorShader=Shader.Find("Braxhorn/NutritionInteriorPBR");
        if(!kit||!interiorShader)throw new InvalidOperationException("Actual authored Nutrition kit/shader not imported");
        var names=kit.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
        foreach(var name in RequiredParts)
            if(names.Count(n=>n==name)!=1)throw new InvalidOperationException("Nutrition kit part is missing or ambiguous: "+name);
        foreach(var name in new[]{"BH honed warm limestone furniture","BH satin oiled walnut joinery","BH wrap labels original atlas"})
        {
            var texture=KitMaterial(name).GetTexture("baseColorTexture");
            if(!(texture is Texture2D)||texture.width<512||texture.height<512)
                throw new InvalidOperationException("Nutrition embedded source map is not the expected Texture2D: "+name);
            Debug.Log("NUTRITION_EMBEDDED_SOURCE_READY "+name+" texture="+texture.name+" size="+texture.width+"x"+texture.height);
        }
    }
    static Material KitSurface(string name){return Interior(BraxCityMaterials.Imported(KitMaterial(name)));}
    [Serializable] sealed class Receipt
    {
        public string engine="Unity6",stage="whole-shop-awaiting-runtime-and-owner-acceptance";
        public string source="Original owner-authorized BRAXHORN Nutrition mesh kit and artwork";
        public int installedProducts,installedShelvingBays,renderers,triangles;
        public bool originalAssetsPreserved=true,sharedPlayerFileChanged=false,phoneAccepted=false,visualAccepted=false;
        public Vector3 servicePoint;
    }
    static Material Interior(Material source)
    {
        if(converted.TryGetValue(source,out var cached))return cached;
        var m=new Material(source){name="Nutrition / "+source.name,shader=interiorShader,enableInstancing=true};
        m.renderQueue=source.renderQueue;
        m.SetOverrideTag("RenderType",source.GetTag("RenderType",false,"Opaque"));
        m.SetFloat("_SunIntensity",0);
        m.SetFloat("_AmbientIntensity",.64f);
        m.SetColor("_SkyColor",new Color(.77f,.75f,.69f));
        m.SetColor("_GroundColor",new Color(.38f,.36f,.32f));
        m.SetFloat("_ReflectionStrength",.30f);
        m.SetVector("_ShopLightA",new Vector4(17.8f,4.11f,16.0f,8));
        m.SetVector("_ShopLightB",new Vector4(23.0f,4.11f,16.0f,8));
        m.SetColor("_ShopColourA",new Color(5.8f,5.35f,4.65f));
        m.SetColor("_ShopColourB",new Color(5.8f,5.35f,4.65f));
        AssetDatabase.CreateAsset(m,Output+"/material_"+(materialSerial++)+".mat");
        converted[source]=m;return m;
    }
    static Material Finish(string name,Color colour,float roughness=.8f,float metal=0)
    {return Interior(BraxCityMaterials.Surface("Nutrition source "+name,colour,roughness,metal));}
    static Transform Part(string name,Transform parent,Vector3 at,float yaw=0)
    {
        var original=kit.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==name);
        if(!original)throw new InvalidOperationException("Authored Nutrition kit part missing: "+name);
        var o=UnityEngine.Object.Instantiate(original.gameObject,parent,false);
        o.name="Authored Nutrition "+name;
        o.transform.localPosition=at;o.transform.localRotation=Quaternion.Euler(0,yaw,0);
        foreach(var r in o.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials=r.sharedMaterials.Select(m=>Interior(BraxCityMaterials.Imported(m))).ToArray();
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        return o.transform;
    }
    static void Product(string name,Transform parent,Vector3 at,float yaw=0)
    {Part(name,parent,at,yaw);productCount++;}
    static void Box(string name,Transform root,Vector3 at,Vector3 size,Material mat,bool solid=false)
    {BraxCityArchitecture.Box("Nutrition "+name,root,at,size,mat,solid);}
    static void PhysicsBox(Transform root,string name,Vector3 centre,Vector3 size)
    {
        var g=new GameObject(name);g.transform.SetParent(root,false);
        var c=g.AddComponent<BoxCollider>();c.center=centre;c.size=size;
    }
    static TextMesh Text(Transform parent,string label,Vector3 at,float size,Color color)
    {
        var g=new GameObject("Nutrition lettering "+label);g.transform.SetParent(parent,false);
        g.transform.localPosition=at;g.transform.localRotation=Quaternion.Euler(0,180,0);
        var text=g.AddComponent<TextMesh>();text.text=label;text.fontSize=64;
        text.characterSize=size*10/64f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
        text.color=color;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var shader=Shader.Find("Braxhorn/CitySignText");if(!shader)throw new InvalidOperationException("Nutrition lettering shader missing");
        var m=new Material(shader){name="Nutrition depth-tested text",mainTexture=text.font.material.mainTexture};
        AssetDatabase.CreateAsset(m,Output+"/text_"+(materialSerial++)+".mat");
        g.GetComponent<MeshRenderer>().sharedMaterial=m;g.AddComponent<BraxCitySignText>();return text;
    }
    static void BrandPanel(Transform root,Vector3 at,float width,Material backing,Material art)
    {
        float height=width*.25f;
        Box("physical brand panel",root,at,new Vector3(width+.1f,height+.1f,.045f),backing);
        var data=new BraxCitySystemMesh();
        Vector3 p=at+Vector3.forward*.025f;
        data.Quad(p+new Vector3(-width*.5f,-height*.5f,0),p+new Vector3(width*.5f,-height*.5f,0),
            p+new Vector3(width*.5f,height*.5f,0),p+new Vector3(-width*.5f,height*.5f,0));
        var face=BraxCityArchitecture.MeshObject("Original BRAXHORN Nutrition print",root,data,art);
        // The readable front faces +Z; its viewer's right is world -X.
        face.GetComponent<MeshFilter>().sharedMesh.uv=new[]{new Vector2(1,0),new Vector2(0,0),new Vector2(0,.25f),new Vector2(1,.25f)};
    }
    static void Bay(Transform root,Vector3 position,float yaw,string category,int variant)
    {
        var bay=Part("ShelvingBay",root,position,yaw);shelfCount++;
        var collider=bay.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.2f,0);collider.size=new Vector3(1.62f,2.43f,.38f);
        Text(bay,category,new Vector3(0,2.58f,0),.15f,new Color(.79f,.69f,.49f));
        var names=new[]{"WheyTub","CreatineJar","Shaker"};
        for(int row=0;row<4;row++)for(int column=0;column<4;column++)
        {
            string name=names[(row+variant)%3];
            Product(name,bay,new Vector3(-.54f+column*.36f,.364f+row*.51f,.03f),(column-1.5f)*4);
        }
    }
    public static void Apply(BraxCityWorld world,Transform original,Transform physics,Transform cityRoot)
    {
        if(!world||world.area!="city"||!world.player||!original||!physics)
            throw new InvalidOperationException("Nutrition must be installed into the actual city source");
        if(world.nutritionCounter)throw new InvalidOperationException("Duplicate Nutrition shop");
        if(!world.points.Any(p=>p.id=="shop_inside"))throw new InvalidOperationException("Original Nutrition anchor missing");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();converted.Clear();materialSerial=productCount=shelfCount=0;
        ValidateSources();
        var cut=new Bounds(new Vector3(20.25f,2.60f,16.15f),new Vector3(11.36f,4.94f,9.06f));
        BraxCityMeshCut.Subtract(original,cut,Output+"/Cuts","nutrition_whole_interior");
        BraxCityMeshCut.SubtractBoxColliders(physics,cut);
        var root=new GameObject("BRAXHORN NUTRITION complete authored shop").transform;root.SetParent(cityRoot,false);
        var wall=Finish("warm mineral plaster",new Color(.67f,.65f,.59f),.93f);
        var dark=Finish("powder coated graphite",new Color(.075f,.09f,.095f),.58f,.25f);
        var metal=Finish("warm brushed metal",new Color(.47f,.37f,.23f),.42f,.7f);
        var seam=Finish("recessed stone joints",new Color(.27f,.27f,.25f),.96f);
        var stone=KitSurface("BH honed warm limestone furniture");
        var art=KitSurface("BH wrap labels original atlas");
        var glass=Finish("clear warm shopfront glazing",new Color(.69f,.76f,.76f,.17f),.16f,.02f);
        Box("lowered floor joint bed",root,new Vector3(20.25f,.10f,16.2f),new Vector3(11.1f,.062f,8.8f),seam);
        PhysicsBox(root,"Nutrition continuous original-level floor",new Vector3(20.25f,.06f,16.2f),new Vector3(11.1f,.18f,8.8f));
        for(int x=0;x<9;x++)for(int z=0;z<14;z++)
            Part("FloorTile",root,new Vector3(15.45f+x*1.2f,.15f,12.3f+z*.6f));
        Box("rear stone perimeter",root,new Vector3(20.25f,.14f,11.9f),new Vector3(11.1f,.02f,.2f),stone);
        Box("entry stone perimeter",root,new Vector3(20.25f,.14f,20.5f),new Vector3(11.1f,.02f,.2f),stone);
        foreach(float x in new[]{14.775f,25.725f})
            Box("side stone perimeter",root,new Vector3(x,.14f,16.2f),new Vector3(.15f,.02f,8.4f),stone);
        foreach(float x in new[]{14.68f,25.82f})
        {
            Box("mineral side wall",root,new Vector3(x,2.55f,16.15f),new Vector3(.16f,4.8f,9.0f),wall,true);
            Box("sidewall recessed bronze skirting",root,new Vector3(x+(x<20?.09f:-.09f),.22f,16.15f),new Vector3(.025f,.14f,8.8f),metal);
        }
        Box("rear mineral wall",root,new Vector3(20.25f,2.55f,11.72f),new Vector3(11.3f,4.8f,.18f),wall,true);
        Box("acoustic ceiling",root,new Vector3(20.25f,4.91f,16.15f),new Vector3(11.3f,.16f,9.0f),wall,true);
        foreach(float z in new[]{12.2f,13.4f,14.6f,15.8f,17.0f,18.2f,19.4f})
            Box("recessed ceiling seam",root,new Vector3(20.25f,4.822f,z),new Vector3(10.9f,.012f,.018f),dark);
        // Keep the already functional Nutrition door in its exact portal.
        // All new shopfront glass is outside its 19.6..21.8m opening.
        foreach(var range in new[]{new Vector2(14.76f,19.53f),new Vector2(21.87f,25.74f)})
        {
            float x=(range.x+range.y)*.5f,w=range.y-range.x;
            Box("shopfront window glazing",root,new Vector3(x,1.62f,20.57f),new Vector3(w,2.83f,.024f),glass,true);
            Box("shopfront base stone",root,new Vector3(x,.22f,20.57f),new Vector3(w,.14f,.16f),stone);
            foreach(float side in new[]{range.x,range.y})
                Box("shopfront steel mullion",root,new Vector3(side,1.62f,20.57f),new Vector3(.045f,2.94f,.09f),dark);
        }
        Box("shopfront sign fascia",root,new Vector3(20.25f,3.95f,20.55f),new Vector3(11.15f,1.78f,.14f),dark,true);
        Box("shopfront bronze reveal",root,new Vector3(20.25f,3.06f,20.67f),new Vector3(11.15f,.035f,.04f),metal);
        BrandPanel(root,new Vector3(20.25f,3.96f,20.64f),4.4f,dark,art);
        BrandPanel(root,new Vector3(20.6f,3.20f,11.84f),3.2f,dark,art);
        Bay(root,new Vector3(15.85f,.15f,12.08f),0,"BIAŁKO",0);
        Bay(root,new Vector3(17.58f,.15f,12.08f),0,"REGENERACJA",1);
        Bay(root,new Vector3(23.0f,.15f,12.08f),0,"KREATYNA",1);
        Bay(root,new Vector3(24.72f,.15f,12.08f),0,"AKCESORIA",2);
        Bay(root,new Vector3(15.02f,.15f,15.0f),90,"BRAXHORN",0);
        Bay(root,new Vector3(15.02f,.15f,17.0f),90,"TRENING",2);
        Bay(root,new Vector3(25.48f,.15f,15.0f),-90,"NAWODNIENIE",2);
        Bay(root,new Vector3(25.48f,.15f,17.0f),-90,"SIŁA",1);
        var counter=Part("Counter",root,new Vector3(20.6f,.15f,13.2f));
        PhysicsBox(counter,"Real counter obstacle",new Vector3(0,.55f,0),new Vector3(2.8f,1.1f,.90f));
        Product("WheyTub",root,new Vector3(19.92f,1.255f,13.17f));
        Product("Shaker",root,new Vector3(21.28f,1.255f,13.17f));
        Product("CreatineJar",root,new Vector3(20.6f,1.185f,13.17f));
        foreach(float x in new[]{17.8f,23.1f})
        {
            var island=Part("DisplayIsland",root,new Vector3(x,.15f,17.0f));
            PhysicsBox(island,"Real display island obstacle",new Vector3(0,.49f,0),new Vector3(1.74f,.98f,.91f));
            Product("WheyTub",root,new Vector3(x-.45f,1.105f,17.0f),-8);
            Product("CreatineJar",root,new Vector3(x,1.105f,17.0f));
            Product("Shaker",root,new Vector3(x+.42f,1.105f,17.0f),8);
        }
        foreach(float x in new[]{17.8f,23.0f})
            // The authored 0.68m suspension reaches the 4.83m ceiling underside.
            Part("LinearPendant",root,new Vector3(x,4.15f,16.0f));
        var service=root.gameObject.AddComponent<BraxNutritionCounter>();service.player=world.player;
        service.servicePoint=new Vector3(20.6f,.15f,14.6f);
        service.shopBounds=new Bounds(new Vector3(20.25f,2.55f,16.15f),new Vector3(11.3f,4.8f,9f));
        Box("physical service display",root,new Vector3(20.6f,1.63f,13.68f),new Vector3(1.45f,.66f,.035f),dark);
        foreach(float x in new[]{20.13f,21.07f})
            Box("service display support",root,new Vector3(x,1.28f,13.68f),new Vector3(.018f,.22f,.025f),metal);
        service.readout=Text(root,"REGENERACJA",new Vector3(20.6f,1.63f,13.71f),.18f,new Color(.86f,.76f,.55f));
        world.nutritionCounter=service;
        var energy=new GameObject("BraxhornFighterEnergy");energy.AddComponent<BraxFighterEnergy>();
        world.points=world.points.Concat(new[]{
            new BraxCityPoint{id="NUTRITION_COUNTER",label="Nutrition — regeneracja",position=service.servicePoint},
            new BraxCityPoint{id="NUTRITION_SHOWROOM",label="Nutrition — wnętrze",position=new Vector3(20.6f,.15f,18.8f)}}).ToArray();
        BraxCityArchitecture.Batch(root);BraxCityArchitecture.Finish(root);
        // Offline contact AO of this small room only, with no global light or
        // source material edits. Both shaders share the same baked UV contract.
        var materials=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled)
            .SelectMany(r=>r.sharedMaterials).Distinct().Where(m=>m&&m.shader==interiorShader).ToArray();
        var bakeShader=Shader.Find(BraxCityMaterials.ShaderName);
        var queues=materials.ToDictionary(m=>m,m=>m.renderQueue);
        try
        {
            foreach(var m in materials){m.shader=bakeShader;m.renderQueue=queues[m];}
            BraxCityMaterials.Bake(root);
        }
        finally{foreach(var m in materials)if(m){m.shader=interiorShader;m.renderQueue=queues[m];}}
        foreach(var m in materials)EditorUtility.SetDirty(m);
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.GetComponent<MeshFilter>()).ToArray();
        var receipt=new Receipt{installedProducts=productCount,installedShelvingBays=shelfCount,renderers=renderers.Length,
            triangles=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),servicePoint=service.servicePoint};
        File.WriteAllText(Output+"/shop-receipt.json",JsonUtility.ToJson(receipt,true));AssetDatabase.SaveAssets();
        if(productCount<130||shelfCount!=8)throw new InvalidOperationException("Nutrition retail installation incomplete");
        Debug.Log("NUTRITION_WHOLE_SHOP_AUTHORED "+JsonUtility.ToJson(receipt));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Persistent source for the city renovation. Source kits stay immutable; only
// selected metre-scale parts are instantiated. One storey is one designed
// storey: no building is stretched vertically to manufacture a taller skyline.
public static class BraxCityArchitecture
{
    public const string Output="Assets/PlayableGenerated/CityRebuild/Architecture";
    static int serial;
    static readonly Dictionary<string,GameObject> libraries=new Dictionary<string,GameObject>();
    static readonly Dictionary<Mesh,Mesh> planarWalls=new Dictionary<Mesh,Mesh>();
    static Material stone, darkMetal, roof, backing, brick;
    const string Urban="modular_urban_apartments_facade";
    const string Factory="modular_factory_facade";
    public sealed class Building
    {
        public string id,title;public float x0,x1,z0,z1;public int floors;public bool far;
        public Building(string id,string title,float x0,float x1,float z0,float z1,int floors,bool far=false)
        {this.id=id;this.title=title;this.x0=x0;this.x1=x1;this.z0=z0;this.z1=z1;this.floors=floors;this.far=far;}
        public float Height=>3.75f+floors*3.2f+.75f;
    }
    public static readonly Building[] StreetBuildings={
        new Building("apothecary","APTEKA",38,52,11.6f,20.6f,8),
        new Building("corner_store","DELIKATESY",26,38,11.6f,20.6f,6),
        new Building("bar","BAR 24",-12,-1,11.6f,20.6f,7),
        new Building("restaurant","BISTRO",-24,-12,11.6f,20.6f,10),
        new Building("workshop_shop","PRACOWNIA",-36,-24,11.6f,20.6f,7),
        new Building("hotel","HOTEL",-48,-36,11.6f,20.6f,11),
        new Building("viaduct_corner","BRAXHORN",-61.4f,-48,11.6f,20.6f,8),
        new Building("workshop","WARSZTAT",-87,-71,11.6f,20.6f,6),
        new Building("estate_corner","KAWIARNIA",-104,-87,11.6f,20.6f,9),
        new Building("quay_corner","NABRZEŻE",52,66,36.1f,60,9,true),
        new Building("bakery","PIEKARNIA",18,32,36.1f,48,6,true),
        new Building("gallery","GALERIA",-10,2,36.1f,48,8,true),
        new Building("office_entrance","BRAXHORN",-22,-10,36.1f,48,7,true),
        new Building("apartments_1","KSIĘGARNIA",-34,-22,36.1f,48,9,true),
        new Building("apartments_2","KAWIARNIA",-46,-34,36.1f,48,7,true),
        new Building("apartments_3","PRACOWNIA",-61.4f,-46,36.1f,48,8,true),
        new Building("estate_apartments_1","BRAXHORN",-88,-71,36.1f,48,6,true),
        new Building("estate_apartments_2","BRAXHORN",-104,-88,36.1f,48,9,true)
    };

    static GameObject Library(string slug)
    {
        if(libraries.TryGetValue(slug,out var value))return value;
        string path="Assets/Source/CityRebuild/models/"+slug+"/1k/"+slug+"_1k.gltf";
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path)??AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!model)throw new InvalidOperationException("Verified source kit missing: "+path);
        value=UnityEngine.Object.Instantiate(model);value.name="SOURCE_LIBRARY_DO_NOT_RENDER_"+slug;value.SetActive(false);
        libraries.Add(slug,value);return value;
    }
    internal static Transform Part(string slug,string name,Transform parent,Vector3 at,Quaternion rotation,Vector3 scale)
    {
        var source=Library(slug).GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        if(!source)throw new InvalidOperationException("Audited source module absent: "+slug+" / "+name);
        var copy=UnityEngine.Object.Instantiate(source.gameObject,parent,false);copy.name=slug+"__"+name;copy.SetActive(true);
        copy.transform.localPosition=at;copy.transform.localRotation=rotation;copy.transform.localScale=scale;
        foreach(var r in copy.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials=r.sharedMaterials.Select(BraxCityMaterials.Imported).ToArray();
            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            if(slug==Urban&&(name=="wall_window_centered_large_01"||name=="wall_window_centered_large_02"))
            {var filter=r.GetComponent<MeshFilter>();if(filter)filter.sharedMesh=PlanarWall(filter.sharedMesh);}
        }
        return copy.transform;
    }
    static Mesh PlanarWall(Mesh source)
    {
        if(planarWalls.TryGetValue(source,out var cached))return cached;
        var p=source.vertices;var uv=source.uv;var normal=source.normals;var bounds=source.bounds;
        // Audited glTF topology: an exactly planar 3 x 3 m panel, with an
        // exactly rectangular opening x=.55..2.45, y=.46..2.15 in Unity.
        // This removes only redundant coplanar tessellation. Its outline,
        // aperture, affine source UV map and all original window meshes stay.
        if(source.subMeshCount!=1||bounds.size.z>.0001f||Mathf.Abs(bounds.min.x)>.0001f||Mathf.Abs(bounds.max.x-3)>.0001f||Mathf.Abs(bounds.max.y-3)>.0001f||uv.Length!=p.Length)
            throw new InvalidOperationException("Source planar-wall contract changed; refuse silent simplification");
        float area=0;var tr=source.triangles;
        for(int i=0;i<tr.Length;i+=3)area+=Vector3.Cross(p[tr[i+1]]-p[tr[i]],p[tr[i+2]]-p[tr[i]]).magnitude*.5f;
        if(Mathf.Abs(area-(9-1.9f*1.69f))>.002f)throw new InvalidOperationException("Source window aperture is no longer the audited rectangle");
        int a=0,b=-1,c=-1;
        for(int i=1;i<p.Length;i++)if((p[i]-p[a]).sqrMagnitude>.001f){b=i;break;}
        for(int i=1;i<p.Length&&b>=0;i++)if(Vector3.Cross(p[b]-p[a],p[i]-p[a]).sqrMagnitude>.001f){c=i;break;}
        if(c<0)throw new InvalidOperationException("Planar wall has no usable UV basis");
        Vector2 d1=new Vector2(p[b].x-p[a].x,p[b].y-p[a].y),d2=new Vector2(p[c].x-p[a].x,p[c].y-p[a].y);
        float determinant=d1.x*d2.y-d1.y*d2.x;
        Func<Vector3,Vector2> map=v=>{var d=new Vector2(v.x-p[a].x,v.y-p[a].y);float s=(d.x*d2.y-d.y*d2.x)/determinant,t=(d1.x*d.y-d1.y*d.x)/determinant;return uv[a]+s*(uv[b]-uv[a])+t*(uv[c]-uv[a]);};
        for(int i=0;i<p.Length;i++)if((map(p[i])-uv[i]).sqrMagnitude>1e-7f)throw new InvalidOperationException("Planar wall UV is not affine; preserve source instead");
        var xs=new SortedSet<float>{0,.55f,2.45f,3};var ys=new SortedSet<float>{0,.46f,2.15f,3};
        for(float v=.5f;v<3;v+=.5f){xs.Add(v);ys.Add(v);}var xx=xs.ToArray();var yy=ys.ToArray();var data=new BraxCitySystemMesh();
        for(int i=0;i<xx.Length-1;i++)for(int j=0;j<yy.Length-1;j++)
        {
            float x=(xx[i]+xx[i+1])*.5f,y=(yy[j]+yy[j+1])*.5f;
            if(x>.55f&&x<2.45f&&y>.46f&&y<2.15f)continue;
            data.Quad(new Vector3(xx[i],yy[j],0),new Vector3(xx[i+1],yy[j],0),new Vector3(xx[i+1],yy[j+1],0),new Vector3(xx[i],yy[j+1],0));
        }
        var mesh=data.Build("Source-equivalent planar window panel");mesh.uv=mesh.vertices.Select(map).ToArray();mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,Output+"/planar_"+(serial++)+".asset");planarWalls.Add(source,mesh);
        Debug.Log("CITY_EXACT_PLANAR_OPTIMISATION "+source.name+" "+source.triangles.Length/3+" -> "+mesh.triangles.Length/3+" triangles; outline/aperture/UV retained");return mesh;
    }
    static Transform Group(string name,Transform parent,Vector3 at,Quaternion rotation)
    {var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=at;g.localRotation=rotation;return g;}
    internal static GameObject MeshObject(string name,Transform parent,BraxCitySystemMesh data,Material material,bool collider=false)
    {
        var mesh=data.Build(name);AssetDatabase.CreateAsset(mesh,Output+"/"+(serial++).ToString("D5")+".asset");
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        if(collider)g.AddComponent<MeshCollider>().sharedMesh=mesh;
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);return g;
    }
    public static GameObject Box(string name,Transform parent,Vector3 at,Vector3 size,Material material,bool collider=false,float metres=1)
    {
        var m=new BraxCitySystemMesh();m.Box(at,size,metres);var g=MeshObject(name,parent,m,material);
        if(collider){var c=g.AddComponent<BoxCollider>();c.center=at;c.size=size;}return g;
    }
    public static void Prepare()
    {
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();serial=0;libraries.Clear();planarWalls.Clear();
        stone=BraxCityMaterials.Surface("Facade warm limestone",new Color(.62f,.59f,.53f),.88f);
        darkMetal=BraxCityMaterials.Surface("Facade charcoal painted aluminium",new Color(.075f,.085f,.092f),.52f,.35f);
        roof=BraxCityMaterials.Surface("Roof matte zinc",new Color(.21f,.23f,.24f),.8f,.12f);
        backing=BraxCityMaterials.Surface("Deep window interior",new Color(.025f,.033f,.039f),1);
        brick=SurfaceTexture("Masonry brick at three metres","brick_wall_001","brick_wall_001_diffuse_2k.jpg",3);
    }
    public static Material SurfaceTexture(string name,string slug,string diffuse,float metres)
    {
        string dir="Assets/Source/CityRebuild/textures/"+slug+"/2k/";
        var baseColor=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+diffuse);
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+slug+"_nor_gl_2k.png");
        var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+slug+"_arm_2k.jpg");
        if(!baseColor||!normal||!arm)throw new InvalidOperationException("Incomplete verified PBR texture set: "+slug);
        return BraxCityMaterials.Textured(name,baseColor,normal,arm,1f/metres);
    }

    static void Elevation(Transform building,string name,Vector3 at,float yaw,float width,int floors,bool entry,string title)
    {
        var face=Group(name,building,at,Quaternion.Euler(0,yaw,0));
        int bays=Mathf.Max(1,Mathf.RoundToInt(width/3f));float bay=width/bays;
        // The original windows retain all their depth; matching source wall
        // apertures are used on every near storey. Only horizontal bay fitting
        // (normally under 12%) changes; doors and floor heights stay metric.
        for(int i=0;i<bays;i++)
        {
            bool door=entry&&i==bays/2;
            string wall=door?"wall_door_centered_large_01":"wall_window_centered_large_01";
            string detail=door?"door_centered_large_01":"window_centered_large_01";
            var scale=new Vector3(bay/3,1,1);
            Part(Urban,wall,face,new Vector3(i*bay,.15f,0),Quaternion.identity,scale);
            Part(Urban,detail,face,new Vector3(i*bay,.15f,0),Quaternion.identity,scale);
            Part(Urban,"cornice_standard_standard_01",face,new Vector3(i*bay,3.25f,0),Quaternion.identity,scale);
        }
        Box("Deep ground-floor recess",face,new Vector3(width*.5f,1.65f,-.48f),new Vector3(width,3,.08f),backing);
        Box("Stone ground-storey entablature",face,new Vector3(width*.5f,3.575f,-.08f),new Vector3(width,.35f,.3f),stone);
        for(int floor=0;floor<floors;floor++)
        {
            var storey=Group("Storey "+(floor+2),face,Vector3.zero,Quaternion.identity);float y=3.75f+floor*3.2f;
            bool top=floor==floors-1;
            string suffix=top?"03":floor%3==0?"01":"02";
            for(int i=0;i<bays;i++)
            {
                var scale=new Vector3(bay/3,1,1);
                Part(Urban,"wall_window_centered_large_"+suffix,storey,new Vector3(i*bay,y,0),Quaternion.identity,scale);
                Part(Urban,"window_centered_large_"+suffix,storey,new Vector3(i*bay,y,0),Quaternion.identity,scale);
                Part(Urban,"cornice_standard_standard_01",storey,new Vector3(i*bay,y+3,0),Quaternion.identity,scale);
            }
            Box("Window recess backing",storey,new Vector3(width*.5f,y+1.5f,-.5f),new Vector3(width,3,.08f),backing);
            Batch(storey);
        }
        for(int i=0;i<bays;i++)Part(Urban,"crown_standard_standard_01",face,new Vector3(i*bay,3.75f+floors*3.2f,0),Quaternion.identity,new Vector3(bay/3,1,1));
        if(entry&& !string.IsNullOrEmpty(title))
        {
            Box("Shop sign backing",face,new Vector3(width*.5f,3.42f,.18f),new Vector3(Mathf.Min(width-.6f,7.5f),.43f,.12f),darkMetal);
            Sign(face,title,new Vector3(width*.5f,3.42f,.25f),Mathf.Min(.31f,5.8f/Mathf.Max(3,title.Length)),Color.white);
        }
        Batch(face);
    }

    public static Transform Build(Transform parent,Building b)
    {
        var g=Group("Building "+b.id,parent,Vector3.zero,Quaternion.identity);float width=b.x1-b.x0,depth=b.z1-b.z0;
        if(b.far)Elevation(g,"Street frontage",new Vector3(b.x1,0,b.z0),180,width,b.floors,true,b.title);
        else Elevation(g,"Street frontage",new Vector3(b.x0,0,b.z1),0,width,b.floors,true,b.title);
        // Exposed backs are designed too. Recessed windows and a complete roof
        // avoid a dressed front with a blank, stretched box behind it.
        if(b.far)Elevation(g,"Courtyard frontage",new Vector3(b.x0,0,b.z1),0,width,b.floors,false,"");
        else Elevation(g,"Rear frontage",new Vector3(b.x1,0,b.z0),180,width,b.floors,false,"");
        Box("West party wall masonry",g,new Vector3(b.x0+.12f,b.Height*.5f,(b.z0+b.z1)*.5f),new Vector3(.24f,b.Height,depth),brick,false,1);
        Box("East party wall masonry",g,new Vector3(b.x1-.12f,b.Height*.5f,(b.z0+b.z1)*.5f),new Vector3(.24f,b.Height,depth),brick,false,1);
        for(int i=0;i<=b.floors;i++)
        {
            float y=i==0?3.6f:3.75f+i*3.2f;
            Box("Return course west",g,new Vector3(b.x0-.015f,y,(b.z0+b.z1)*.5f),new Vector3(.33f,.16f,depth+.35f),stone);
            Box("Return course east",g,new Vector3(b.x1+.015f,y,(b.z0+b.z1)*.5f),new Vector3(.33f,.16f,depth+.35f),stone);
        }
        Box("Weatherproof roof",g,new Vector3((b.x0+b.x1)*.5f,b.Height-.45f,(b.z0+b.z1)*.5f),new Vector3(width,.15f,depth),roof);
        // Future premises remain closed. The existing shop, NOVA and gym are
        // excluded from this builder and keep their real doorway openings.
        var physical=new GameObject("Closed future premises collision");physical.transform.SetParent(g,false);
        physical.transform.position=new Vector3((b.x0+b.x1)*.5f,b.Height*.5f,(b.z0+b.z1)*.5f);
        physical.AddComponent<BoxCollider>().size=new Vector3(width,b.Height,depth);
        return g;
    }

    public static void UpperExtension(Transform parent,string id,float x0,float x1,float z0,float z1,float baseY,int floors)
    {
        var g=Group(id+" preserved interior upper storeys",parent,new Vector3(0,baseY,0),Quaternion.identity);
        float w=x1-x0,h=3.75f+floors*3.2f+.75f;
        Elevation(g,"Upper front",new Vector3(x0,0,z1),0,w,floors,false,"");
        Elevation(g,"Upper rear",new Vector3(x1,0,z0),180,w,floors,false,"");
        Box("Upper side west",g,new Vector3(x0+.1f,h*.5f,(z0+z1)*.5f),new Vector3(.2f,h,z1-z0),brick);
        Box("Upper side east",g,new Vector3(x1-.1f,h*.5f,(z0+z1)*.5f),new Vector3(.2f,h,z1-z0),brick);
        Box("Upper roof",g,new Vector3((x0+x1)*.5f,h-.4f,(z0+z1)*.5f),new Vector3(w,.2f,z1-z0),roof);
    }

    public static void Tower(Transform parent,string id,Rect footprint,float baseY,int floors,float floorHeight=3.35f)
    {
        var g=Group("High-rise "+id,parent,Vector3.zero,Quaternion.identity);
        var glass=BraxCityMaterials.Surface("Tower blue grey glazing",new Color(.19f,.25f,.29f),.22f,.28f);
        var glassLight=BraxCityMaterials.Surface("Tower glazing subtle variation",new Color(.24f,.29f,.32f),.3f,.22f);
        var spandrel=BraxCityMaterials.Surface("Tower dark ceramic spandrels",new Color(.12f,.14f,.15f),.7f,.08f);
        var faces=new[]{new[]{footprint.xMin,footprint.yMax,0f,footprint.width},new[]{footprint.xMax,footprint.yMin,180f,footprint.width},
            new[]{footprint.xMin,footprint.yMin,270f,footprint.height},new[]{footprint.xMax,footprint.yMax,90f,footprint.height}};
        foreach(var f in faces)
        {
            var face=Group("Curtain wall",g,new Vector3(f[0],baseY,f[1]),Quaternion.Euler(0,f[2],0));
            int bays=Mathf.Max(2,Mathf.RoundToInt(f[3]/1.65f));float bay=f[3]/bays;
            // Each panel is recessed behind real mullions, floor spandrels and
            // transoms. Tall towers add floors, not taller windows or textures.
            for(int storey=0;storey<floors;storey++)
            {
                var frame=new BraxCitySystemMesh();var windows=new BraxCitySystemMesh();var variant=new BraxCitySystemMesh();var opaque=new BraxCitySystemMesh();
                float y=storey*floorHeight;
                for(int i=0;i<bays;i++)
                {
                    float x=i*bay;
                    (i%5==storey%5?variant:windows).Box(new Vector3(x+bay*.5f,y+floorHeight*.57f,-.065f),new Vector3(bay-.11f,floorHeight*.76f,.045f));
                    frame.Box(new Vector3(x+.045f,y+floorHeight*.5f,.035f),new Vector3(.09f,floorHeight,.16f));
                    frame.Box(new Vector3(x+bay*.5f,y+floorHeight*.63f,.035f),new Vector3(bay,.055f,.15f));
                    opaque.Box(new Vector3(x+bay*.5f,y+.23f,-.015f),new Vector3(bay,.46f,.12f));
                }
                frame.Box(new Vector3(f[3],y+floorHeight*.5f,.035f),new Vector3(.09f,floorHeight,.16f));
                frame.Box(new Vector3(f[3]*.5f,y+floorHeight-.04f,.035f),new Vector3(f[3],.08f,.18f));
                MeshObject("Glazed panels storey "+(storey+1),face,windows,glass);
                if(variant.VertexCount>0)MeshObject("Subtle glazing variation",face,variant,glassLight);
                MeshObject("Mullions and transoms",face,frame,darkMetal);MeshObject("Opaque floor edge",face,opaque,spandrel);
            }
            Batch(face);
        }
        float height=floors*floorHeight;
        Box("Tower roof and coping",g,new Vector3(footprint.center.x,baseY+height+.12f,footprint.center.y),new Vector3(footprint.width+.3f,.24f,footprint.height+.3f),roof);
        Box("Recessed plant enclosure",g,new Vector3(footprint.center.x,baseY+height+1.3f,footprint.center.y),new Vector3(footprint.width*.52f,2.3f,footprint.height*.5f),darkMetal);
        if(baseY<.2f)Box("Tower closed structural core",g,new Vector3(footprint.center.x,height*.5f,footprint.center.y),new Vector3(footprint.width-.3f,height,footprint.height-.3f),backing,true);
    }

    public static void Billboard(Transform parent,string id,Vector3 center,float width,string texturePath,float yaw)
    {
        var texture=BraxCityArtwork.Load(texturePath);
        float height=width*texture.height/texture.width;
        var g=Group("BRAXHORN original artwork billboard "+id,parent,center,Quaternion.Euler(0,yaw,0));
        Box("Galvanised billboard back",g,Vector3.zero,new Vector3(width+.18f,height+.18f,.18f),darkMetal);
        var art=BraxCityMaterials.Textured("Original artwork "+id,texture,null,null,1);
        var face=new BraxCitySystemMesh();face.Quad(new Vector3(-width*.5f,-height*.5f,.101f),new Vector3(width*.5f,-height*.5f,.101f),new Vector3(width*.5f,height*.5f,.101f),new Vector3(-width*.5f,height*.5f,.101f));
        var obj=MeshObject("Unmodified supplied brand artwork",g,face,art);
        var mesh=obj.GetComponent<MeshFilter>().sharedMesh;mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
        var mount=new BraxCitySystemMesh();
        foreach(float x in new[]{-width*.42f,width*.42f})
        {
            mount.Box(new Vector3(x,0,-.24f),new Vector3(.12f,height+.6f,.15f));
            for(float y=-height*.45f;y<=height*.45f+.01f;y+=Mathf.Max(1,height*.45f))mount.Box(new Vector3(x,y,-.43f),new Vector3(.26f,.2f,.45f));
        }
        MeshObject("Steel frame and wall anchors",g,mount,darkMetal);Batch(g);
    }
    public static void Sign(Transform parent,string text,Vector3 at,float size,Color color)
    {
        var obj=new GameObject("Architectural lettering "+text);obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localRotation=Quaternion.Euler(0,180,0);
        var tm=obj.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.fontSize=64;tm.characterSize=size*10f/64f;tm.color=color;
        tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var shader=Shader.Find("Braxhorn/CitySignText");if(!shader)throw new InvalidOperationException("City text shader missing");
        var mat=new Material(shader){name="Depth tested lettering "+serial};mat.mainTexture=tm.font.material.mainTexture;
        AssetDatabase.CreateAsset(mat,Output+"/lettering_"+(serial++)+".mat");obj.GetComponent<MeshRenderer>().sharedMaterial=mat;
        obj.AddComponent<BraxCitySignText>();
    }
    // Separate spatial batches, never one indivisible district mesh.
    public static void Batch(Transform root)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).ToArray();
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(var f in filters)
        {
            var r=f.GetComponent<MeshRenderer>();if(r.GetComponent<TextMesh>()||r.GetComponent<LODGroup>())continue;
            for(int i=0;i<f.sharedMesh.subMeshCount;i++)
            {
                var mat=r.sharedMaterials[Mathf.Min(i,r.sharedMaterials.Length-1)];
                if(!groups.TryGetValue(mat,out var list))groups[mat]=list=new List<CombineInstance>();
                list.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
            }
            r.enabled=false;
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh{name=root.name+" / "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true,false);
            AssetDatabase.CreateAsset(mesh,Output+"/batch_"+(serial++).ToString("D5")+".asset");
            var g=new GameObject("Local batch "+pair.Key.name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
        }
    }
    public static void Finish()
    {foreach(var g in libraries.Values)if(g)UnityEngine.Object.DestroyImmediate(g);libraries.Clear();}
}

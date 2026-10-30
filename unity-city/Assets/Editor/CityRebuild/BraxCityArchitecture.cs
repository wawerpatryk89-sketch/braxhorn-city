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
        public string id,title;public float x0,x1,z0,z1;public int floors;public bool far,industrial;
        public Building(string id,string title,float x0,float x1,float z0,float z1,int floors,bool far=false,bool industrial=false)
        {this.id=id;this.title=title;this.x0=x0;this.x1=x1;this.z0=z0;this.z1=z1;this.floors=floors;this.far=far;this.industrial=industrial;}
        public float Height=>3.75f+floors*3.2f+.75f;
    }
    public static readonly Building[] StreetBuildings={
        new Building("apothecary","APTEKA",38,52,-4,20.6f,8),
        new Building("corner_store","DELIKATESY",26,38,-4,20.6f,6),
        new Building("bar","BAR 24",-12,-1,11.6f,20.6f,7),
        new Building("restaurant","BISTRO",-24,-12,11.6f,20.6f,10),
        new Building("workshop_shop","PRACOWNIA",-36,-24,11.6f,20.6f,7,false,true),
        new Building("hotel","HOTEL",-48,-36,11.6f,20.6f,11),
        new Building("viaduct_corner","BRAXHORN",-61.4f,-48,11.6f,20.6f,8),
        new Building("workshop","WARSZTAT",-87,-71,11.6f,20.6f,6,false,true),
        new Building("estate_corner","KAWIARNIA",-104,-87,11.6f,20.6f,9),
        new Building("quay_corner","BRAXHORN TOWER",52,66,36.1f,60,9,true),
        new Building("bakery","PIEKARNIA",18,32,36.1f,48,6,true),
        new Building("gallery","GALERIA",-10,2,36.1f,48,8,true,true),
        new Building("office_entrance","BRAXHORN",-22,-10,36.1f,48,7,true,true),
        new Building("apartments_1","KSIĘGARNIA",-34,-22,36.1f,48,9,true),
        new Building("apartments_2","KAWIARNIA",-46,-34,36.1f,48,7,true,true),
        new Building("apartments_3","PRACOWNIA",-61.4f,-46,36.1f,48,8,true),
        new Building("estate_apartments_1","BRAXHORN",-88,-71,36.1f,48,6,true,true),
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
        // Deliberately defer asset serialization. Most of these small authored
        // pieces are merged by Batch() and otherwise would create >5,000
        // redundant imported mesh assets during the full-street build.
        // Finish(streetRoot) persists only surviving render/collision meshes.
        var mesh=data.Build(name);serial++;
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

    static void Elevation(Transform building,string name,Vector3 at,float yaw,float width,int floors,bool entry,string title,bool industrial=false,bool functionalEntry=false)
    {
        var face=Group(name,building,at,Quaternion.Euler(0,yaw,0));
        string kit=industrial?Factory:Urban;
        string cornice=industrial?"cornice02_standard_standard_01":"cornice_standard_standard_01";
        int bays=Mathf.Max(1,Mathf.RoundToInt(width/3f));float bay=width/bays;
        // The original windows retain all their depth; matching source wall
        // apertures are used on every near storey. Only horizontal bay fitting
        // (normally under 12%) changes; doors and floor heights stay metric.
        for(int i=0;i<bays;i++)
        {
            bool door=entry&&i==bays/2;
            string wall=door?(industrial?"wall_door_recessed_large_01":"wall_door_centered_large_01"):"wall_window_centered_large_01";
            string detail=door?(industrial?"door_recessed_large_01":"door_centered_large_01"):"window_centered_large_01";
            var scale=new Vector3(bay/3,1,1);
            // The tower doorway is not a painted glass rectangle. Reserve the
            // entire central modular bay as a true exterior opening. Its
            // separate hinged doors and structural jambs are authored by the
            // tower-fit-out builder, without blocking the CharacterController.
            if(!functionalEntry || !door)
            {
                Part(kit,wall,face,new Vector3(i*bay,.15f,0),Quaternion.identity,scale);
                Part(kit,detail,face,new Vector3(i*bay,.15f,0),Quaternion.identity,scale);
            }
            Part(kit,cornice,face,new Vector3(i*bay,3.25f,0),Quaternion.identity,scale);
        }
        if(functionalEntry)
        {
            for(int i=0;i<bays;i++)if(i!=bays/2)
                Box("Deep ground-floor glazing recess",face,new Vector3((i+.5f)*bay,1.65f,-.48f),new Vector3(bay,3,.08f),backing);
        }
        else Box("Deep ground-floor recess",face,new Vector3(width*.5f,1.65f,-.48f),new Vector3(width,3,.08f),backing);
        Box("Continuous ground-storey lintel",face,new Vector3(width*.5f,3.28f,-.1f),new Vector3(width,.3f,.2f),stone);
        Box("Stone ground-storey entablature",face,new Vector3(width*.5f,3.575f,-.08f),new Vector3(width,.35f,.3f),stone);
        for(int floor=0;floor<floors;floor++)
        {
            var storey=Group("Storey "+(floor+2),face,Vector3.zero,Quaternion.identity);float y=3.75f+floor*3.2f;
            bool top=floor==floors-1;
            string suffix=industrial?"01":top?"03":floor%3==0?"01":"02";
            for(int i=0;i<bays;i++)
            {
                var scale=new Vector3(bay/3,1,1);
                Part(kit,"wall_window_centered_large_"+suffix,storey,new Vector3(i*bay,y,0),Quaternion.identity,scale);
                Part(kit,"window_centered_large_"+suffix,storey,new Vector3(i*bay,y,0),Quaternion.identity,scale);
                Part(kit,cornice,storey,new Vector3(i*bay,y+3,0),Quaternion.identity,scale);
            }
            Box("Window recess backing",storey,new Vector3(width*.5f,y+1.5f,-.5f),new Vector3(width,3,.08f),backing);
            Box("Continuous floor joint",storey,new Vector3(width*.5f,y+3.1f,-.1f),new Vector3(width,.2f,.18f),stone);
            Batch(storey);
        }
        for(int i=0;i<bays;i++)Part(kit,"crown_standard_standard_01",face,new Vector3(i*bay,3.75f+floors*3.2f,0),Quaternion.identity,new Vector3(bay/3,1,1));
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
        // All eighteen frontages require a REAL portal, not a non-interactive
        // door mesh over a solid interior collider. Quay tower retains its
        // separately designed double glass doors in PremiumTowerBuild.
        if(b.far)Elevation(g,"Street frontage",new Vector3(b.x1,0,b.z0),180,width,b.floors,true,b.title,b.industrial,true);
        else Elevation(g,"Street frontage",new Vector3(b.x0,0,b.z1),0,width,b.floors,true,b.title,b.industrial,true);
        // Exposed backs are designed too. Recessed windows and a complete roof
        // avoid a dressed front with a blank, stretched box behind it.
        if(b.far)Elevation(g,"Courtyard frontage",new Vector3(b.x0,0,b.z1),0,width,b.floors,false,"",b.industrial);
        else Elevation(g,"Rear frontage",new Vector3(b.x1,0,b.z0),180,width,b.floors,false,"",b.industrial);
        bool westNeighbour=StreetBuildings.Any(n=>n!=b&&Mathf.Abs(n.x1-b.x0)<.01f&&n.z0<b.z1&&n.z1>b.z0);
        bool eastNeighbour=StreetBuildings.Any(n=>n!=b&&Mathf.Abs(n.x0-b.x1)<.01f&&n.z0<b.z1&&n.z1>b.z0);
        // These shared returns were formerly visibly RED even between glass
        // towers. Their audited geometry is retained, but finished in
        // subdued graphite metal so no isolated red-brick slab reads as a
        // rendering error on the public street.
        if(westNeighbour)Box("West shared graphite structural return",g,new Vector3(b.x0+.12f,b.Height*.5f,(b.z0+b.z1)*.5f),new Vector3(.24f,b.Height,depth),darkMetal,false,1);
        else Elevation(g,"Exposed west return",new Vector3(b.x0,0,b.z0),270,depth,b.floors,false,"",b.industrial);
        if(eastNeighbour)Box("East shared graphite structural return",g,new Vector3(b.x1-.12f,b.Height*.5f,(b.z0+b.z1)*.5f),new Vector3(.24f,b.Height,depth),darkMetal,false,1);
        else Elevation(g,"Exposed east return",new Vector3(b.x1,0,b.z1),90,depth,b.floors,false,"",b.industrial);
        for(int i=0;i<=b.floors;i++)
        {
            float y=i==0?3.6f:3.75f+i*3.2f;
            Box("Return course west",g,new Vector3(b.x0-.015f,y,(b.z0+b.z1)*.5f),new Vector3(.33f,.16f,depth+.35f),stone);
            Box("Return course east",g,new Vector3(b.x1+.015f,y,(b.z0+b.z1)*.5f),new Vector3(.33f,.16f,depth+.35f),stone);
        }
        // No intermediate full roof may seal the accessible lift shaft.
        // Its upper tower and final roof deck are composed of real cut slabs.
        // Any building carrying an accessible skyscraper must have a real
        // vertical shaft, without opaque roof slabs sealing the lower facade.
        bool hasLift=BraxCityStreetSkyline.HighRises.Any(t=>t.id==b.id);
        if(!hasLift)
            Box("Weatherproof roof",g,new Vector3((b.x0+b.x1)*.5f,b.Height-.45f,(b.z0+b.z1)*.5f),new Vector3(width,.15f,depth),roof);
        // Remove the old full-block collider: it physically prevented entry
        // through every source door. Author a short (7 m) walkable ground
        // lobby with solid lateral boundaries and an actual moving door.
        // No unlocked warehouse, NPC, shop logic or extra gameplay is added.
        if(b.id!="quay_corner")
        {
            const float lobbyHeight=3.58f;
            float effectiveDepth=Mathf.Min(7.05f,depth-.55f);
            float streetZ=b.far?b.z0:b.z1;
            float behind=b.far?1f:-1f;
            float lobbyCenterZ=streetZ+behind*effectiveDepth*.5f;
            float interiorEnd=streetZ+behind*effectiveDepth;
            var warmFloor=BraxCityMaterials.Surface("Natural non-gloss stone in accessible ground-floor lobbies",
                new Color(.45f,.45f,.43f),.93f,.02f);
            var wallFinish=BraxCityMaterials.Surface("Matte premium interior walls in authored lobbies",
                new Color(.29f,.305f,.30f),.87f,.04f);
            Box("REAL walkable entry floor "+b.id,g,new Vector3((b.x0+b.x1)*.5f,.055f,lobbyCenterZ),
                new Vector3(width-.38f,.11f,effectiveDepth),warmFloor,true);
            Box("Lobby ceiling "+b.id,g,new Vector3((b.x0+b.x1)*.5f,lobbyHeight+.18f,lobbyCenterZ),
                new Vector3(width-.32f,.16f,effectiveDepth),wallFinish,true);
            foreach(float sideX in new[]{b.x0+.14f,b.x1-.14f})
                Box("Walkable lobby sidewall "+b.id,g,new Vector3(sideX,lobbyHeight*.5f,lobbyCenterZ),
                    new Vector3(.27f,lobbyHeight,effectiveDepth),wallFinish,true);
            Box("Closed rear of finished street lobby "+b.id,g,
                new Vector3((b.x0+b.x1)*.5f,lobbyHeight*.5f,interiorEnd),
                new Vector3(width-.2f,lobbyHeight,.24f),wallFinish,true);
            if(depth-effectiveDepth>.45f)
                Box("Physically closed unbuilt rear premises "+b.id,g,
                    new Vector3((b.x0+b.x1)*.5f,b.Height*.5f,(interiorEnd+(b.far?b.z1:b.z0))*.5f),
                    new Vector3(width-.3f,b.Height,depth-effectiveDepth),wallFinish,true);
            // Upper unoccupied floors are protected by structural partitions,
            // but there is intentionally NO solid collider across the
            // player's ground-floor entrance.
            if(!hasLift)
                Box("Solid nonpublic upper floors "+b.id,g,
                    new Vector3((b.x0+b.x1)*.5f,(b.Height+lobbyHeight)*.5f,(b.z0+b.z1)*.5f),
                    new Vector3(width-.3f,b.Height-lobbyHeight,depth-.3f),wallFinish,true);
        }
        // The accessible tower receives partitioned structure, not a solid
        // whole-building collider that traps the player at the lobby entrance.
        return g;
    }

    /// <summary>
    /// The sourced facade kit contained decorative non-functional door
    /// geometry; all other buildings now receive a real, hinged, collidable
    /// entrance in their EXISTING dedicated modular middle bay. This
    /// registers with BraxCityWorld so the user's original Action button
    /// can open/close it. The one showcase tower owns its own door design.
    /// </summary>
    public static BraxCityDoor BuildStreetAccessDoor(Transform building,Building b)
    {
        if(b.id=="quay_corner")return null;
        if(!building || building.name!="Building "+b.id)
            throw new InvalidOperationException("Cannot attach city door to wrong building "+b.id);
        float frontZ=b.far?b.z0:b.z1;
        float yaw=b.far?180f:0f;
        float x=(b.x0+b.x1)*.5f;
        var root=Group("PUBLIC WORKING ENTRANCE "+b.title+" "+b.id,building,
            new Vector3(x,.15f,frontZ),Quaternion.Euler(0,yaw,0));
        float width=b.x1-b.x0;
        int bays=Mathf.Max(1,Mathf.RoundToInt(width/3f));
        float bay=width/bays;
        // Exact original middle modular bay centre. For odd bay counts the
        // visible door is not always exactly at the block centre.
        float frontX=b.far?b.x1:b.x0;
        float direction=b.far?-1f:1f;
        float portalX=frontX+direction*(bays/2+.5f)*bay;
        root.position=new Vector3(portalX,.15f,frontZ);
        const float widthEach=.94f;
        var colour=BraxCityMaterials.Surface("City working door graphite steel / nonemissive",
            new Color(.11f,.14f,.145f),.59f,.43f);
        var tinted=BraxCityMaterials.Surface("City working door smoked safety glazing",
            new Color(.27f,.36f,.39f,.73f),.23f,.12f);
        var handle=BraxCityMaterials.Surface("City door stainless handle",
            new Color(.58f,.60f,.56f),.33f,.62f);
        var leaves=new Transform[2];var sideDirections=new[]{1f,-1f};
        for(int i=0;i<2;i++)
        {
            float axis=i==0?-1.0f:1.0f;
            float center= i==0?widthEach*.5f:-widthEach*.5f;
            var leaf=Group((i==0?"Left":"Right")+" real door leaf with capsule collision",root,
                new Vector3(axis,0,0),Quaternion.identity);
            Box("Operable smoked safety door",leaf,new Vector3(center,1.37f,-.02f),
                new Vector3(widthEach,2.74f,.075f),tinted,true);
            foreach(float frame in new[]{-.43f,.43f})
                Box("Powder coated frame",leaf,new Vector3(center+frame,1.37f,-.070f),
                    new Vector3(.04f,2.74f,.08f),colour);
            Box("Lockable handle",leaf,new Vector3(center+(i==0?.34f:-.34f),1.12f,.08f),
                new Vector3(.035f,.31f,.12f),handle);
            leaves[i]=leaf;
        }
        foreach(float jamb in new[]{-1.1f,1.1f})
            Box("Structurally framed entrance jamb",root,new Vector3(jamb,1.46f,-.02f),
                new Vector3(.13f,2.93f,.28f),colour);
        Box("Architectural entrance lintel",root,new Vector3(0,2.97f,-.02f),
            new Vector3(2.32f,.15f,.28f),colour);
        var door=root.gameObject.AddComponent<BraxCityDoor>();
        door.id="R1_PUBLIC_DOOR_"+b.id;
        door.label=b.title;
        door.leaves=leaves;door.sides=sideDirections;
        Debug.Log("CITY_FUNCTIONAL_ENTRY id="+door.id+" worldXYZ="+root.position+
            " sourceFootprint="+b.id+" actualDoorCollision=2");
        return door;
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

    public static void Tower(Transform parent,string id,Rect footprint,float baseY,int floors,float floorHeight=3.35f,bool openLiftRoof=false)
    {
        if(floors<2||floors>48||floorHeight<3f||footprint.width<6||footprint.height<6)
            throw new InvalidOperationException("Invalid high-rise metre-scale geometry: "+id);
        var g=Group("High-rise "+id,parent,Vector3.zero,Quaternion.identity);
        bool warm=id.Contains("brick")||id.Contains("hotel")||id.Contains("estate");
        bool cool=id.Contains("quay")||id.Contains("glass")||id.Contains("gallery");
        var glass=BraxCityMaterials.Surface("Tower PBR glazing "+(warm?"smoky grey":cool?"blue steel":"slate"),
            warm?new Color(.24f,.255f,.26f):cool?new Color(.15f,.25f,.29f):new Color(.19f,.22f,.25f),.20f,.18f);
        var glassLight=BraxCityMaterials.Surface("Tower glazing tile variation "+id,
            warm?new Color(.29f,.29f,.28f):cool?new Color(.20f,.29f,.32f):new Color(.24f,.26f,.28f),.28f,.12f);
        var spandrel=BraxCityMaterials.Surface("Tower ceramic spandrels "+(warm?"warm":"dark"),
            warm?new Color(.19f,.18f,.17f):new Color(.10f,.13f,.14f),.73f,.08f);
        var metal=BraxCityMaterials.Surface("Tower facade brushed mullion "+(warm?"bronze":"steel"),
            warm?new Color(.31f,.275f,.23f):new Color(.19f,.22f,.235f),.55f,.62f);
        var faces=new[]{new[]{footprint.xMin,footprint.yMax,0f,footprint.width},
            new[]{footprint.xMax,footprint.yMin,180f,footprint.width},
            new[]{footprint.xMin,footprint.yMin,270f,footprint.height},
            new[]{footprint.xMax,footprint.yMax,90f,footprint.height}};
        foreach(var f in faces)
        {
            var face=Group("Real recessed curtain facade",g,new Vector3(f[0],baseY,f[1]),Quaternion.Euler(0,f[2],0));
            int bays=Mathf.Max(2,Mathf.RoundToInt(f[3]/1.65f));
            float bay=f[3]/bays;
            // Vertices are accumulated over the full local elevation before
            // baking: 4 material meshes per face, not 4 temporary GameObjects
            // per floor. This substantially reduces Unity import churn.
            var frame=new BraxCitySystemMesh();
            var windows=new BraxCitySystemMesh();
            var variant=new BraxCitySystemMesh();
            var opaque=new BraxCitySystemMesh();
            for(int storey=0;storey<floors;storey++)
            {
                float y=storey*floorHeight;
                for(int i=0;i<bays;i++)
                {
                    float x=i*bay;
                    (i%7==storey%7?variant:windows).Box(new Vector3(x+bay*.5f,y+floorHeight*.57f,-.07f),
                        new Vector3(bay-.14f,floorHeight*.75f,.055f));
                    frame.Box(new Vector3(x+.045f,y+floorHeight*.5f,.075f),new Vector3(.09f,floorHeight,.22f));
                    frame.Box(new Vector3(x+bay*.5f,y+floorHeight*.65f,.075f),new Vector3(bay,.055f,.16f));
                    opaque.Box(new Vector3(x+bay*.5f,y+.28f,.0f),new Vector3(bay,.56f,.14f));
                }
                frame.Box(new Vector3(f[3],y+floorHeight*.5f,.075f),new Vector3(.09f,floorHeight,.22f));
                frame.Box(new Vector3(f[3]*.5f,y+floorHeight-.04f,.075f),new Vector3(f[3],.09f,.19f));
            }
            MeshObject("Glazed panels real depth",face,windows,glass);
            if(variant.VertexCount>0)MeshObject("Irregular glass bays",face,variant,glassLight);
            MeshObject("Extruded metal mullions",face,frame,metal);
            MeshObject("Ceramic floor spandrels",face,opaque,spandrel);
            Batch(face);
        }
        float height=floors*floorHeight;
        if(id=="quay observation tower"||openLiftRoof)
        {
            // Every street-linked tall building needs a true roof lift exit.
            // A solid top slab would seal the elevator or trap the player.
            Debug.Log("CITY_LIFT_OPEN_ROOF full slab suppressed for "+id);
        }
        else
        {
            Box("Weatherproof roof and coping",g,new Vector3(footprint.center.x,baseY+height+.12f,footprint.center.y),
                new Vector3(footprint.width+.3f,.24f,footprint.height+.3f),roof);
            Box("Recessed technical penthouse",g,new Vector3(footprint.center.x,baseY+height+1.30f,footprint.center.y),
                new Vector3(footprint.width*.52f,2.30f,footprint.height*.5f),metal);
        }
        if(baseY<.2f)Box("Tower closed structural core",g,
            new Vector3(footprint.center.x,height*.5f,footprint.center.y),
            new Vector3(footprint.width-.3f,height,footprint.height-.3f),backing,true);
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
        // Front tablicy patrzy w +Z grupy (w stronę ulicy), więc U musi MALEĆ wraz z X,
        // inaczej napis czyta się w lustrzanym odbiciu (błąd widoczny na nagraniach z telefonu).
        var mesh=obj.GetComponent<MeshFilter>().sharedMesh;mesh.uv=new[]{Vector2.right,Vector2.zero,Vector2.up,Vector2.one};
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
            serial++; // the combined mesh is persisted once by Finish(root)
            var g=new GameObject("Local batch "+pair.Key.name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.OccluderStatic);
        }
    }
    public static void Finish(Transform streetRoot)
    {
        if(!streetRoot)throw new ArgumentNullException("streetRoot");
        foreach(var g in libraries.Values)
            if(g)UnityEngine.Object.DestroyImmediate(g);
        libraries.Clear();
        var filters=streetRoot.GetComponentsInChildren<MeshFilter>(true);
        int persisted=0,transientDiscarded=0;
        // Saving all retained meshes in one import transaction avoids thousands
        // of Editor auto-refreshes, and keeps the final scene byte-identical to
        // its generated runtime geometry, never depending on temporary objects.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var filter in filters)
            {
                if(!filter)continue;
                var mesh=filter.sharedMesh;
                if(!mesh||AssetDatabase.Contains(mesh))continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                var collider=filter.GetComponent<MeshCollider>();
                if((!renderer||!renderer.enabled)&&(!collider||!collider.enabled))
                {
                    filter.sharedMesh=null;
                    UnityEngine.Object.DestroyImmediate(mesh);
                    UnityEngine.Object.DestroyImmediate(filter);
                    if(renderer)UnityEngine.Object.DestroyImmediate(renderer);
                    transientDiscarded++;
                    continue;
                }
                var path=Output+"/persistent_"+(serial++).ToString("D6")+".asset";
                AssetDatabase.CreateAsset(mesh,path);
                persisted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CITY_MESH_ASSET_COMPACTION persistent="+persisted+
            " mergedTemporaryMeshesDiscarded="+transientDiscarded+
            " all original facade prefabs and source PBR retained");
    }
}

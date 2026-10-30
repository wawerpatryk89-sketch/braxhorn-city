using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// SECOND district only. No changes to the original gym, city source,
// mission IDs, premium quay tower, or any existing passenger lift controller.
// Fit-out uses source PBR library materials and real supplied sofa/shrub GLBs.
public static class BraxCityTowerDistrictFinish
{
    const string SourceShrub="Assets/Source/CityRebuild/landscape/park_shrub.glb";
    const string SourceSofa="Assets/Source/LibrarySofa02.glb";
    const string Tile="Assets/Source/CityRebuild/textures/concrete_pavement_02/";
    static Material floor,wall,metal,glass,wood;
    static GameObject Load(string path)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<GameObject>().FirstOrDefault();
        if(!model)throw new InvalidOperationException("Real user library asset missing: "+path);
        return model;
    }
    static Transform Group(string name,Transform parent)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    static void Panel(string name,Transform root,Vector3 center,Vector3 size,Material material,bool solid=false)
    {
        BraxCityArchitecture.Box(name,root,center,size,material,solid);
    }
    static void Model(string label,GameObject source,Transform root,Vector3 position,
        float desiredHorizontalWidth,float yaw)
    {
        var obj=UnityEngine.Object.Instantiate(source,root,false);
        obj.name=label;obj.transform.position=position;
        obj.transform.rotation=Quaternion.Euler(0,yaw,0);
        var renderers=obj.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Source library model has no meshes: "+label);
        Bounds bounds=renderers[0].bounds;
        foreach(var rr in renderers.Skip(1))bounds.Encapsulate(rr.bounds);
        float width=Mathf.Max(bounds.size.x,bounds.size.z);
        if(width<.01f||width>100f)throw new InvalidOperationException("Source model scale is implausible: "+label);
        obj.transform.localScale*=desiredHorizontalWidth/width;
        var scaled=obj.GetComponentsInChildren<Renderer>(true);
        Bounds placed=scaled[0].bounds;
        foreach(var rr in scaled.Skip(1))placed.Encapsulate(rr.bounds);
        obj.transform.position+=Vector3.up*(position.y-placed.min.y);
        foreach(var rr in renderers)
        {
            rr.shadowCastingMode=ShadowCastingMode.Off;
            if(rr is MeshRenderer mesh)mesh.sharedMaterials=mesh.sharedMaterials.Select(BraxCityMaterials.Imported).ToArray();
        }
        // Only reception sofa gets a simple real interaction blocker. The
        // source shrubs remain non-solid and cannot trap the player.
        if(label.Contains("reception sofa"))
        {
            var collider=obj.AddComponent<BoxCollider>();
            collider.center=obj.transform.InverseTransformPoint(placed.center);
            var local=obj.transform.InverseTransformVector(placed.size);
            collider.size=new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
        }
    }
    public static void Apply(BraxCityWorld world,Transform streetRoot)
    {
        if(!world||world.area!="city"||!streetRoot)
            throw new InvalidOperationException("Tower district must be attached to real city");
        var sofa=Load(SourceSofa);
        var shrub=Load(SourceShrub);
        var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Tile+"diffuse.jpg");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Tile+"normal.png") ??
                   AssetDatabase.LoadAssetAtPath<Texture2D>(Tile+"normal.jpg");
        var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(Tile+"arm.jpg");
        if(!albedo||!normal||!arm)
            throw new InvalidOperationException("Existing premium stone PBR material missing");
        floor=BraxCityMaterials.Textured("Eight tower office foyer genuine stone PBR",albedo,normal,arm,.65f);
        wall=BraxCityMaterials.Surface("Eight tower office warm painted plaster",new Color(.79f,.77f,.73f),.95f);
        metal=BraxCityMaterials.Surface("Eight tower brushed steel trim",new Color(.34f,.36f,.37f),.44f,.58f);
        glass=BraxCityMaterials.Surface("Eight tower office safety glass",new Color(.35f,.45f,.47f,.58f),.23f,.09f);
        wood=BraxCityMaterials.Surface("Eight tower wood reception skirting",new Color(.30f,.23f,.18f),.66f);
        var root=Group("Eight tower premium ground floor reception and landscape - user source assets",streetRoot);
        int offices=0,plants=0,seats=0;
        foreach(var plan in BraxCityStreetSkyline.HighRises.Where(p=>p.id!=BraxCityStreetSkyline.AccessibleTowerId))
        {
            var b=BraxCityArchitecture.StreetBuildings.Single(x=>x.id==plan.id);
            float x=(b.x0+b.x1)*.5f;
            float street=b.far?b.z0:b.z1;
            float dir=b.far?1f:-1f;
            float z=street+dir*2.65f;
            var room=Group("Real PBR tower lobby "+b.id,root);
            // Overlay finish only: no new ground collider over the preserved
            // walkable source geometry, and no blocking of the entrance.
            Panel("Real PBR lobby floor finish",room,new Vector3(x,.169f,z),
                new Vector3(Mathf.Min(b.x1-b.x0-1.2f,9.2f),.025f,4.0f),floor);
            float sideX=b.x0+.48f;
            Panel("Warm painted lobby side",room,new Vector3(sideX,1.65f,z),
                new Vector3(.04f,2.95f,4.0f),wall);
            Panel("Brushed steel lobby base trim",room,new Vector3(sideX+.03f,.26f,z),
                new Vector3(.05f,.14f,3.9f),metal);
            Panel("Real timber reception accent",room,new Vector3(b.x1-.65f,1.52f,z+dir*.5f),
                new Vector3(.045f,2.55f,2.8f),wood);
            Panel("Glass office partition on rear side",room,
                new Vector3(b.x1-.68f,1.65f,z+dir*1.65f),
                new Vector3(.06f,2.85f,1.05f),glass);
            Panel("Architectural satin light cove",room,
                new Vector3(x,3.25f,z),new Vector3(3.0f,.05f,.15f),metal);
            // Original 4K sofa from the user's existing verified source kit.
            // It is kept off the 2.1m entrance and off the lift call area.
            Model("Library reception sofa "+b.id,sofa,room,
                new Vector3(b.x1-2.0f,.18f,z+dir*.75f),1.75f,b.far?0:180);
            seats++;
            // Two REAL park shrubs from the user's original osiedle source,
            // planted only on the frontage, not in the traffic lanes.
            for(int i=0;i<2;i++)
            {
                float px=i==0?b.x0+1.05f:b.x1-1.05f;
                float pz=street-dir*.72f;
                Model("User library park shrub "+b.id+" "+i,shrub,root,
                    new Vector3(px,.16f,pz),.90f,90f*i);
                plants++;
            }
            world.points=world.points.Concat(new[]{
                new BraxCityPoint{id="R2_OFFICE_"+b.id,label=b.title+" lobby premium",
                    position=new Vector3(x,.19f,z)}
            }).ToArray();
            offices++;
        }
        if(offices!=8||seats!=8||plants!=16)
            throw new InvalidOperationException("Eight tower real library fit-out incomplete");
        Debug.Log("CITY_EIGHT_TOWER_FITOUT_SOURCE_PBR offices="+offices+
            " originalLibrarySofas="+seats+" originalLibraryShrubs="+plants+
            " noNewElevatorControllers=true originalGymPreserved=true phoneQA=pending");
    }
}

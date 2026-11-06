using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY / MIASTO MISTRZÓW
// Only source-verified Poly Haven 2K CC0 3D models inside K1..K12.
// No human models, no NPC, no animation, no owner / seller / crowd.
// No invented product geometry, no pseudo ice-cream, fake fish or pizza.
public static class BraxChampionsPromenadeStock
{
    const string Src="Assets/Source/Mistrzowie/PolyHaven";
    const string Approved="unity-tools/mistrzowie/promenade_polyhaven_models.json";
    const int ExpectedSources=25;
    const int MaxInstances=62;
    static float K=>BraxClaudeOsiedle.K;

    [Serializable] sealed class ModelEntry
    {
        public string id;
        public int polycount;
        public string[] kiosks;
    }
    [Serializable] sealed class Inventory
    {
        public ModelEntry[] assets;
    }
    static readonly Dictionary<string,GameObject> Templates=new Dictionary<string,GameObject>();

    static GameObject Original(string id)
    {
        if(Templates.TryGetValue(id,out var prefab))return prefab;
        if(string.IsNullOrEmpty(id) || id.IndexOfAny(new[]{'/', '\\', '.'})>=0)
            throw new InvalidOperationException("Unsafe Poly Haven ID "+id);
        string file=Src+"/"+id+"/"+id+"_2k.gltf";
        if(!File.Exists(file))
            throw new FileNotFoundException(
                "Missing original downloaded 2K Poly Haven GLTF: "+id+"; DO NOT substitute an invented model",file);
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(file);
        if(!root)
        {
            AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
            root=AssetDatabase.LoadAssetAtPath<GameObject>(file)
                ??AssetDatabase.LoadAllAssetsAtPath(file).OfType<GameObject>().FirstOrDefault();
        }
        if(!root)
            throw new InvalidOperationException("glTFast 6.20.0 did not import original 2K model: "+file);
        if(!root.GetComponentsInChildren<MeshFilter>(true).Any(m=>m.sharedMesh))
            throw new InvalidOperationException("Imported asset contains no original source geometry: "+id);
        Templates.Add(id,root);
        return root;
    }

    static bool IsFurniture(string id)
    {
        return id=="outdoor_table_chair_set_01" || id=="metal_stool_03";
    }
    static bool IsDisplay(string id)
    {
        return id=="wooden_display_shelves_01" || id=="painted_wooden_shelves";
    }
    static bool IsLarge(string id)
    {
        return id=="plastic_crate_02" || id=="wicker_basket_02"
            || id=="wooden_crate_02" || id=="life_jacket";
    }

    static float DesiredSize(string id,bool furniture,bool display,bool large)
    {
        if(furniture)return id=="metal_stool_03" ? .72f : 1.75f;
        if(display)return 1.50f;
        if(large)return .47f;
        if(id=="bananas")return .38f;
        if(id=="tea_set_01")return .45f;
        return .22f;
    }

    static Transform FindShop(Transform parent,string id)
    {
        // The outer full-size shop shell is created by BraxChampionsPromenade,
        // and stock is parented to its actual transform.
        var shops=parent.GetComponentsInChildren<Transform>(true)
            .Where(t=>t.name.StartsWith(id+" | ",StringComparison.Ordinal)
                 &&t.name.Contains("shop shell")).ToArray();
        if(shops.Length!=1)
            throw new InvalidOperationException("Exactly one original shop shell required for "+id
                                               +"; got "+shops.Length);
        return shops[0];
    }

    static void AddStock(Transform shop,BraxChampionsFoundation.Pavilion bay,
                         ModelEntry spec,int index)
    {
        bool furniture=IsFurniture(spec.id),display=IsDisplay(spec.id),large=IsLarge(spec.id);
        float width=DesiredSize(spec.id,furniture,display,large);
        float x,z,y;
        float safeHalf=bay.w/2f-.52f;
        float offset=(index%3-1)*Mathf.Min(.67f,safeHalf*.42f);
        if(furniture)
        {
            // Real stools/tables rest on empty floor sections of the kiosk.
            // No overlap with the existing rear shelving or customer counter.
            x=index%2==0?-safeHalf*.64f:safeHalf*.64f;
            z=-.30f;
            y=.164f;
        }
        else if(display)
        {
            // Existing roof-height shop interior free corner, not duplicate
            // central shelf position at back wall.
            x=index%2==0?-safeHalf*.72f:safeHalf*.72f;
            z=.15f;
            y=.164f;
        }
        else if(large)
        {
            x=offset;
            z=bay.d/2f-.65f;
            // Blat lady zaplecza: środek .52 + połowa wysokości .55 = .795.
            // Wcześniej .98 -> skrzynki i kosze wisiały ok. 18 cm nad blatem.
            y=.80f;
        }
        else
        {
            // Food products arranged on ACTUAL existing three timber shelves,
            // not lying on a colliding overlay floor in the pedestrian path.
            // Pozycja na półce (index%3) i numer półki (index/3) są niezależne:
            // wcześniej oba = index%3, więc produkt 4 stał w miejscu produktu 1.
            x=offset;
            z=bay.d/2f-.34f;
            y=1.31f+((index/3)%3)*.43f;
        }

        var prefab=Original(spec.id);
        var instance=UnityEngine.Object.Instantiate(prefab);
        instance.name=bay.id+" | Poly Haven CC0 original "+spec.id+" | 2K PBR";
        instance.transform.SetParent(shop,false);
        // Use exact source bounding box, never assume Poly Haven model units.
        var before=BraxRegion.Bounds(instance);
        if(before.size.sqrMagnitude<1e-6f)
            throw new InvalidOperationException("Empty imported 3D source bounds: "+spec.id);
        float modelSpan=display?Mathf.Max(before.size.y,.001f):
                        furniture?Mathf.Max(before.size.x,before.size.z):
                        Mathf.Max(before.size.x,before.size.z);
        float scale=width*K/Mathf.Max(.001f,modelSpan);
        if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<.001f||scale>1000f)
            throw new InvalidOperationException("Invalid Poly Haven normalised shop prop scale: "+spec.id);
        instance.transform.localScale*=scale;
        var after=BraxRegion.Bounds(instance);
        Vector3 bottomTarget=shop.TransformPoint(new Vector3(x*K,y*K,z*K));
        // Source meshes sometimes have very distant pivots; place by real bounds.
        instance.transform.position+=new Vector3(
             bottomTarget.x-after.center.x,
             bottomTarget.y-after.min.y,
             bottomTarget.z-after.center.z);
        foreach(var rend in instance.GetComponentsInChildren<Renderer>(true))
        {
            rend.shadowCastingMode=ShadowCastingMode.Off;
            rend.receiveShadows=true;
            if(rend.sharedMaterials.Any(m=>!m))
                throw new InvalidOperationException("Missing original Poly Haven PBR material "+spec.id);
        }
        // Display objects are visual stock only: no MeshCollider and no scripts
        // capable of spawning people, modifying actors or driving game events.
        foreach(var c in instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
        foreach(var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
            UnityEngine.Object.DestroyImmediate(component);
    }

    public static int Populate(Transform parent,BraxChampionsFoundation.Pavilion[] bays)
    {
        if(!parent||bays==null||bays.Length!=12)
            throw new InvalidOperationException("All 12 original shop shells are required to display products");
        if(!File.Exists(Approved))
            throw new FileNotFoundException("Verified source-only Poly Haven 3D item manifest missing",Approved);
        var sources=JsonUtility.FromJson<Inventory>(File.ReadAllText(Approved));
        if(sources==null||sources.assets==null||sources.assets.Length!=ExpectedSources
           ||sources.assets.Select(x=>x.id).Distinct().Count()!=ExpectedSources)
            throw new InvalidOperationException("25 distinct original approved Poly Haven models required");
        if(sources.assets.Any(x=>x.polycount<=0||x.kiosks==null||x.kiosks.Length==0))
            throw new InvalidOperationException("Incomplete Poly Haven source model metadata");
        Templates.Clear();
        var kiosks=bays.ToDictionary(x=>x.id,x=>x);
        int placed=0;
        var current=bays.ToDictionary(x=>x.id,x=>0);
        foreach(var spec in sources.assets)
        {
            foreach(var id in spec.kiosks)
            {
                if(!kiosks.TryGetValue(id,out var bay))
                    throw new InvalidOperationException("Original prop assigned to a non-existent kiosk: "+id);
                var shop=FindShop(parent,id);
                AddStock(shop,bay,spec,current[id]++);
                if(++placed>MaxInstances)throw new InvalidOperationException("Android prop budget exceeded");
            }
        }
        if(current.Any(k=>k.Value<=0)||placed<45)
            throw new InvalidOperationException("Incomplete owner-approved products: "+placed);
        Debug.Log("CHAMPIONS_PROMENADE_25_VERIFIED_REAL_POLYHAVEN_CC0_2K unique=25 placed="+placed
                 +" NPC=0 crowd=0 sellers=0 pedestrians=0 animation=0 fakeFood=0");
        return placed;
    }
}

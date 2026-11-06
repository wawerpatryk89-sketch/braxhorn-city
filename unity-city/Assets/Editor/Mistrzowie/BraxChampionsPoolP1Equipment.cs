using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// P1 ONLY. All actual owner-supplied models are loaded from the verified
// private asset archive, not replaced by primitive approximations.
// Local coordinates are the approved nominal layout metres; use K once.
public static class BraxChampionsPoolP1Equipment
{
    const string Prefix="Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/";
    static float K=>BraxClaudeOsiedle.K;

    static GameObject Source(string file)
    {
        var path=Prefix+file;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!source)throw new FileNotFoundException(
            "P1 owner equipment not imported. Check private source archive, file="+path,path);
        return source;
    }
    static Bounds GetBounds(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0)throw new InvalidOperationException(
            "Owner P1 equipment model has no active rendered geometry: "+root.name);
        Bounds bounds=renderers[0].bounds;
        foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        if(bounds.size.x<=.0001f||bounds.size.y<=.0001f||bounds.size.z<=.0001f)
            throw new InvalidOperationException("Unusable owner model dimensions: "+root.name);
        return bounds;
    }

    // Place one imported real 3D asset. Never stretch along one axis; preserve
    // model proportions with ONE uniform scale to fit its allocated envelope.
    static GameObject Place(Transform parent,string file,string label,
        float x,float z,float yaw,float maxW,float maxH,float maxD,float floorY=.26f)
    {
        var model=PrefabUtility.InstantiatePrefab(Source(file)) as GameObject;
        if(!model)throw new InvalidOperationException("Could not instantiate original model: "+file);
        model.name="P1 ORIGINAL OWNER 3D | "+label;
        model.transform.SetParent(parent,false);
        model.transform.localPosition=Vector3.zero;
        model.transform.localRotation=Quaternion.Euler(0,yaw,0);
        model.transform.localScale=model.transform.localScale*K;
        Bounds sourceBounds=GetBounds(model);
        float fit=Mathf.Min(maxW*K/sourceBounds.size.x,
                   Mathf.Min(maxH*K/sourceBounds.size.y,maxD*K/sourceBounds.size.z));
        if(fit<=0f||float.IsNaN(fit)||float.IsInfinity(fit))
            throw new InvalidOperationException("Invalid original source dimensions: "+file);
        model.transform.localScale*=fit;
        var actual=GetBounds(model);
        var target=parent.TransformPoint(new Vector3(x,floorY,z)*K);
        model.transform.position+=new Vector3(
            target.x-actual.center.x,target.y-actual.min.y,target.z-actual.center.z);
        var result=GetBounds(model);
        if(result.size.x>maxW*K+.02f*K||result.size.y>maxH*K+.02f*K
           ||result.size.z>maxD*K+.02f*K)
            throw new InvalidOperationException("Original model exceeds planned P1 room: "+file);
        return model;
    }

    static void SolidMeshes(GameObject model,Func<string,bool> selection)
    {
        // Static mesh colliders on real solid surfaces only; NEVER on glass
        // doors, towel props, temporary presentation scenery or pool water.
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh||!filter.gameObject.activeInHierarchy)continue;
            if(!selection(filter.name.ToLowerInvariant()))continue;
            if(filter.GetComponent<Collider>())continue;
            var c=filter.gameObject.AddComponent<MeshCollider>();
            c.sharedMesh=filter.sharedMesh;
        }
    }

    static void OwnerJacuzzi(Transform room,int id,float x,float z,float yaw)
    {
        var tub=Place(room,"hot-tub-original-exterior-model.glb",
            "SPA JACUZZI "+id+" original seated spa basin",x,z,yaw,2.87f,1.00f,2.26f);
        // Exact genuine source liner, seats, rims, jet parts and boarding steps.
        // Only the supplied static opaque water is hidden, never the bathtub.
        var original=tub.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t=>string.Equals(t.name,"Spa water surface",StringComparison.Ordinal));
        if(!original||!original.GetComponent<Renderer>())
            throw new InvalidOperationException("Owner jacuzzi original water surface mesh unavailable");
        var bounds=original.GetComponent<Renderer>().bounds;
        Vector3 local=room.InverseTransformPoint(bounds.center)/K;
        Vector2 dimensions=new Vector2(
            Mathf.Max(.2f,bounds.size.x/K-.055f),
            Mathf.Max(.2f,bounds.size.z/K-.055f));
        original.gameObject.SetActive(false);
        BraxChampionsPoolP1Water.Jacuzzi(room,"JACUZZI_"+id,local,dimensions);
        SolidMeshes(tub,n=>n.Contains("spa enclosed base")||n.Contains("spa outer")||
            n.Contains("spa access step")||n.Contains("continuous spa basin liner")||
            n.Contains("spa interior seat")||n.Contains("spa padded rim"));
    }

    static void InstallSpa(Transform room)
    {
        // Room SPA (x=6.6..17.4, z=-17.7..-9.9).
        // Four genuine 2.23x2.85m Jacuzzi tubs rotated towards the 2.45m
        // CENTRAL walking lane. West door at x=6.6,z=-13.8 remains clear;
        // the .8m gap from the Sauna front corridor is NOT the SPA access.
        foreach(float x in new[]{9.35f,14.65f})
            foreach(float z in new[]{-15.70f,-12.00f})
            {
                int id=(x<12f?0:2)+(z<-13f?1:2);
                OwnerJacuzzi(room,id,x,z,x<12f?90f:270f);
            }
        var sauna=Place(room,"sauna_product_model.glb","separate glass SAUNA unit",
            13.70f,-24.35f,0f,3.8f,2.75f,3.5f);
        SolidMeshes(sauna,n=>n.Contains("base")||n.Contains("stand"));
        // Two complete folding upholstered tables, not rectangular boxes.
        for(int i=0;i<2;i++)
        {
            var bed=Place(room,"portable-massage-table.glb",
                "massage treatment table "+(i+1),-4.95f,i==0?-10.45f:-7.45f,
                0f,.82f,.85f,2.3f);
            SolidMeshes(bed,n=>n.Contains("leg")||n.Contains("rail"));
        }
        var vanity=Place(room,"double-bathroom-vanity.glb",
            "SPA bathroom double vanity original stone+wood", -4.35f,-12.03f,
            0f,1.62f,1.20f,.75f);
        SolidMeshes(vanity,n=>n.Contains("case")||n.Contains("leg")||n.Contains("counter"));
    }

    static void InstallChanging(Transform room)
    {
        // One full original locker set is 2.1m high and ~3.01m wide,
        // rather than 18 separate primitive boxes. The model is reused:
        // same material/texture assets, no per-bay duplicates in the package.
        foreach(var loc in new[]{
            new Vector3(-31.2f,14.3f,0f),new Vector3(-26.75f,14.3f,0f),
            new Vector3(-14.0f,14.3f,0f),new Vector3(13.0f,14.3f,0f)})
        {
            var bank=Place(room,"lockers.glb","premium real locker bank",loc.x,loc.y,0f,3.32f,2.17f,.73f);
            SolidMeshes(bank,_=>true);
        }
        foreach(var loc in new[]{
            new Vector2(-30f,11.7f),new Vector2(-13.5f,11.7f),new Vector2(12f,11.7f)})
        {
            var bench=Place(room,"locker_room_bench_free_model.glb",
                "metal-and-wood real dressing bench",loc.x,loc.y,90f,
                1.28f,.55f,.54f);
            SolidMeshes(bench,_=>true);
        }
        // Three complete real 2K PBR shower cabins in an enclosed,
        // accessible wet-room. Avoid a solid full-bounds collider: it would
        // block walking into a shower through its actual door.
        foreach(float x in new[]{-3.64f,-2.15f,-.67f})
            Place(room,"shower_cabin.glb","real wet-room shower with glass and drain",
                x,13.0f,180f,1.36f,2.30f,1.14f);
    }

    static void InstallOutdoor(Transform outside,Transform indoor)
    {
        // Original real lounger source FBX: outside on the separate eastern
        // recreation terrace, not blocking the indoor four-Jacuzzi room.
        foreach(float x in new[]{47f,55f,63f})
            Place(outside,"Sun-lounger_FBX.fbx","premium terrace chaise longue",
                x,2.3f,0f,1.5f,1.35f,2.4f);
        var rescue=Place(indoor,"lifeguard-high-chair-original.glb",
            "genuine high lifeguard lookout",21.0f,19.3f,180f,
            1.45f,2.7f,1.5f);
        SolidMeshes(rescue,_=>true);
    }

    public static void Install(Transform room,Transform outdoors)
    {
        if(!room||!outdoors)throw new ArgumentNullException("P1 room/outdoor root missing");
        // Import all critical source assets BEFORE creating a misleading
        // half-finished prefab; the caller runs this only after P1 source
        // verification of the owner archive.
        foreach(var id in new[]{
            "hot-tub-original-exterior-model.glb","sauna_product_model.glb",
            "portable-massage-table.glb","double-bathroom-vanity.glb",
            "locker_room_bench_free_model.glb","shower_cabin.glb","lockers.glb",
            "Sun-lounger_FBX.fbx","lifeguard-high-chair-original.glb"})Source(id);
        var group=new GameObject("P1 | OWNER 3D FURNITURE | replaceable source-locked modules").transform;
        group.SetParent(room,false);
        InstallSpa(group);
        InstallChanging(group);
        InstallOutdoor(outdoors,group);
        Debug.Log("P1 OWNER EQUIPMENT SOURCE INSTALLED | 4 Jacuzzi with actual water/bubbles "+
            "| 1 Sauna | 2 massage tables | 1 double vanity | 4 locker banks "+
            "| 3 locker benches | 3 premium showers | 3 exterior loungers | 1 lifeguard chair "+
            "| Editor source authoring only; Unity/phone visual review pending");
    }
}

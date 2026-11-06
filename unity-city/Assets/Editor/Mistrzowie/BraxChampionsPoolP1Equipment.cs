using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Add ONLY remaining original P1 models that the independent SPA furnishing
// module does not already install. Never duplicate sauna, jacuzzi, vanity,
// massage tables, changing lockers, showers or benches.
public static class BraxChampionsPoolP1Equipment
{
    const string Prefix="Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/";
    static float K=>BraxClaudeOsiedle.K;

    static Bounds BoundsOf(GameObject model)
    {
        var renderers=model.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0)throw new InvalidOperationException(
            "Owner P1 model has no active mesh: "+model.name);
        var bounds=renderers[0].bounds;
        foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        return bounds;
    }

    // Uniform fit in real nominal metres; original proportions and materials
    // are never stretched, painted over or replaced by procedural furniture.
    static GameObject Place(Transform parent,string file,string label,
        float x,float z,float yaw,float maxW,float maxH,float maxD,float floorY)
    {
        var path=Prefix+file;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(!prefab)throw new FileNotFoundException(
            "P1 owner private source not imported: "+path,path);
        var model=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if(!model)throw new InvalidOperationException("Original P1 source import failed: "+file);
        model.name="P1 ORIGINAL OWNER MODEL | "+label;
        model.transform.SetParent(parent,false);
        model.transform.localPosition=Vector3.zero;
        model.transform.localRotation=Quaternion.Euler(0f,yaw,0f);
        var first=BoundsOf(model);
        float fit=Mathf.Min(maxW*K/first.size.x,Mathf.Min(maxH*K/first.size.y,maxD*K/first.size.z));
        if(!(fit>0f)||float.IsNaN(fit)||float.IsInfinity(fit))
            throw new InvalidOperationException("P1 source model has invalid dimensions: "+file);
        model.transform.localScale*=fit;
        var current=BoundsOf(model);
        var target=parent.TransformPoint(new Vector3(x,floorY,z)*K);
        model.transform.position+=target-new Vector3(current.center.x,current.min.y,current.center.z);
        var final=BoundsOf(model).size/K;
        if(final.x>maxW+.02f||final.y>maxH+.02f||final.z>maxD+.02f)
            throw new InvalidOperationException("Owner source exceeds reserved P1 area: "+file);
        return model;
    }
    static void StaticMeshCollision(GameObject original)
    {
        foreach(var filter in original.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh||!filter.gameObject.activeInHierarchy)continue;
            if(filter.GetComponent<Collider>())continue;
            filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        }
    }

    public static void InstallRemaining(Transform room,Transform outside)
    {
        if(!room||!outside)throw new ArgumentNullException("P1 furniture root");
        // Two real owner-supplied stainless ladders on original, collidable
        // competition and family basins. Both are aligned at the true coping,
        // not fabricated out of repeated floating cubes.
        var sport=Place(room,"pool-ladder.glb","SPORT 25m actual steel ladder",
            5.55f,18.65f,0f,1.25f,1.88f,1.34f,.26f-1.2f);
        StaticMeshCollision(sport);
        var family=Place(room,"pool-ladder.glb","FAMILY actual steel ladder",
            -47.95f,21.65f,0f,1.25f,1.88f,1.34f,.26f-1.06f);
        StaticMeshCollision(family);

        var lookout=Place(room,"lifeguard-high-chair-original.glb",
            "real lifeguard high-chair by pool",
            20.7f,19.5f,180f,1.55f,2.65f,1.62f,.26f);
        StaticMeshCollision(lookout);

        // Three genuine owner's sun-lounger FBX prefabs sit on the
        // herringbone east terrace and never block the four indoor jacuzzis.
        foreach(float x in new[]{47f,55f,63f})
        {
            var chaise=Place(outside,"Sun-lounger_FBX.fbx",
                "premium east terrace chaise longue",x,2.3f,0f,
                1.50f,1.4f,2.45f,.36f);
            StaticMeshCollision(chaise);
        }
        // The supplied diving board is deliberately NOT placed: the
        // 2.15m competition pool is not approved for diving here. One
        // 94k-triangle stack of towels is deferred for Android cost audit.
        Debug.Log("P1 OWNER REMAINING EQUIPMENT: 2 real stainless ladders, "+
            "1 original lifeguard chair, 3 real FBX terrace loungers. "+
            "No duplicated SPA interior models or unapproved diving board.");
    }
}

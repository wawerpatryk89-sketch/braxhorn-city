using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Original owner-supplied luxurious barrel seat, not a procedural look-alike.
// Exactly one source model is instanced into the VIP area beside the unchanged judges.
public static class BraxChampionsArenaVip
{
    public const string Source="Assets/Source/Mistrzowie/VIP/free_luxurious_barrel_seat_2k.glb";
    static readonly float[] VipZ={70.3f,72.05f};
    static readonly Vector2[] VipX={new Vector2(-94.6f,-88.9f),new Vector2(-83.1f,-77.4f)};

    public static int Populate(Transform arenaBowl)
    {
        if(!arenaBowl)throw new ArgumentNullException(nameof(arenaBowl));
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if(!source)
        {
            Debug.LogWarning("CHAMPION_ARENA_VIP_SOURCE_MISSING: missing original seat at "+Source+
                "; no counterfeit VIP seats placed");
            return 0;
        }
        var seats=new GameObject("VIP | original barrel seats beside referees").transform;
        seats.SetParent(arenaBowl,false);
        int count=0;
        foreach(float z in VipZ)
        foreach(var range in VipX)
        for(float x=range.x+.9f;x<=range.y-.25f;x+=1.35f)
        {
            var anchor=new GameObject("VIP original barrel armchair "+(++count)).transform;
            anchor.SetParent(seats,false);
            anchor.localPosition=new Vector3(x,.055f,z);
            // Geometry in the supplier's GLB stays untouched; scaling is uniform.
            var chair=PrefabUtility.InstantiatePrefab(source) as GameObject;
            if(!chair)throw new InvalidOperationException("Original VIP GLB failed to instantiate");
            chair.transform.SetParent(anchor,false);
            chair.transform.localPosition=Vector3.zero;
            chair.transform.localRotation=Quaternion.Euler(0,180,0);
            var renderers=chair.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new InvalidOperationException("Empty original VIP seat mesh");
            var b=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);
            float k=BraxClaudeOsiedle.K;
            // The source GLB is in authoring units (source 85 x 74 x 78).
            // Fit to a human-sized 1.05m width and max 1.25m height in this game.
            float maxHorizontal=Mathf.Max(b.size.x,b.size.z);
            if(maxHorizontal<.00001f||b.size.y<.00001f)
                throw new InvalidOperationException("Invalid original VIP mesh dimensions");
            float scale=Mathf.Min(1.05f*k/maxHorizontal,1.25f*k/b.size.y);
            chair.transform.localScale*=scale;
            b=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))b.Encapsulate(renderer.bounds);
            chair.transform.position+=Vector3.up*(anchor.position.y-b.min.y);
            // One simple convex-space seating collider; no per-triangle dynamic mesh.
            var collision=anchor.gameObject.AddComponent<BoxCollider>();
            collision.center=new Vector3(0,.57f,0);
            collision.size=new Vector3(1.07f,1.15f,1.07f);
        }
        Debug.Log("CHAMPION_ARENA_VIP_ORIGINAL count="+count+" source="+Source);
        return count;
    }
}

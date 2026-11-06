using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// P1 only. Owner's exact source GLB prefabs are kept intact and can later be
// exchanged without moving finished walls, doors, glass or utility corridors.
// Source assets are imported from the separate private Unity equipment ZIP;
// missing assets are visibly reported, never replaced by fake primitives.
public static class BraxChampionsPoolP1SpaFurnishing
{
    const string Src="Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/";
    static float K=>BraxClaudeOsiedle.K;

    static void Pending(Transform parent,string file,float x,float z)
    {
        var socket=new GameObject("P1 ORIGINAL OWNER MODEL REQUIRED | "+file);
        socket.transform.SetParent(parent,false);
        socket.transform.localPosition=new Vector3(x,.26f,z)*K;
        Debug.LogWarning("P1 SPA equipment source not yet imported in this Unity workspace: "+Src+file);
    }

    static Bounds RenderingBounds(GameObject model)
    {
        var items=model.GetComponentsInChildren<Renderer>(true).Where(r=>r && r.enabled).ToArray();
        if(items.Length==0)throw new InvalidOperationException("Original owner SPA GLB has no meshes: "+model.name);
        var b=items[0].bounds;
        for(int i=1;i<items.Length;i++)b.Encapsulate(items[i].bounds);
        return b;
    }

    static GameObject Install(Transform parent,string file,string label,
        float x,float z,float maxW,float maxD,float maxH,float turn=0f)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Src+file);
        if(!source)throw new System.IO.FileNotFoundException(
            "P1 owner SPA asset missing; the verified private release must be imported before any prefab is generated: "+Src+file,
            Src+file);
        var instance=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!instance)throw new InvalidOperationException("P1 owner model importer failed for "+file);
        instance.name="P1 REAL OWNER MODEL | "+label+" | "+file;
        instance.transform.SetParent(parent,false);
        instance.transform.localPosition=Vector3.zero;
        instance.transform.localRotation=Quaternion.Euler(0,turn,0);
        instance.transform.localScale=Vector3.one;
        var raw=RenderingBounds(instance);
        if(raw.size.x<.01f||raw.size.y<.01f||raw.size.z<.01f)
            throw new InvalidOperationException("P1 source has zero/invalid bounds: "+file);
        // Uniform scale only, without stretching any of the owner's original
        // geometry, UVs, logos, proportions or texture colours.
        float factor=Mathf.Min(maxW*K/raw.size.x,Mathf.Min(maxD*K/raw.size.z,maxH*K/raw.size.y));
        if(factor<=0f||float.IsNaN(factor)||float.IsInfinity(factor))
            throw new InvalidOperationException("P1 owner GLB scale invalid: "+file);
        instance.transform.localScale=Vector3.one*factor;
        var fitted=RenderingBounds(instance);
        var floorWorld=parent.TransformPoint(new Vector3(x*K,.26f*K,z*K));
        instance.transform.position+=floorWorld-
            new Vector3(fitted.center.x,fitted.min.y,fitted.center.z);
        var actual=RenderingBounds(instance).size/K;
        if(actual.x>maxW+.02f||actual.y>maxH+.02f||actual.z>maxD+.02f)
            throw new InvalidOperationException("P1 fitted owner model exceeds allocated dimensions: "+file);
        Debug.Log("P1 SPA REAL MODEL INSTALLED "+label+" | dims_nominal_m="+
            actual.ToString("F2")+" | uniform_scale="+factor+" | not Unity playmode-tested");
        return instance;
    }

    public static void Generate(Transform rooms,Transform details,Material mosaic,Material stainless)
    {
        // Sauna: original freestanding source model ~1.32 x 1.22 x 1.97m
        // inside the already finished, independently accessible 8.9x8m sauna.
        Install(rooms,"sauna_product_model.glb","SPA freestanding sauna",
            14.2f,-22.3f,1.7f,1.65f,2.25f);

        // Two real massage treatment rooms, each with a real 2.22m-long bed.
        // Positioned away from the east-side corridor/door at X=-10m.
        Install(rooms,"portable-massage-table.glb","MASSAGE ROOM 1 therapy table",
            -14.35f,-1.5f,.83f,2.35f,.95f);
        Install(rooms,"portable-massage-table.glb","MASSAGE ROOM 2 therapy table",
            -14.35f,3.45f,.83f,2.35f,.95f);

        // The source is a two-basin vanity with sink and drawers, NOT a
        // fabricated cosmetic dressing table. It is installed on the
        // treatment room wall as a wash-up counter.
        Install(rooms,"double-bathroom-vanity.glb","MASSAGE ROOM 1 double wash vanity",
            -16.5f,-.45f,1.70f,.70f,1.30f);

        // Four owner tubs (2.23x2.85m including entrance step) in the
        // existing SPA RELAKS room: X=[6.6,17.4], Z=[-17.7,-9.9].
        // The central aisle between jacuzzi columns is 3.2m; the WEST
        // entrance at X=6.6, Z=-13.8 and its circulation stay open.
        foreach(float x in new[]{9.2f,14.7f})
            foreach(float z in new[]{-11.85f,-15.65f})
            {
                string id="JACUZZI_"+(x<12f?"LEFT":"RIGHT")+(z>-13f?"_NORTH":"_SOUTH");
                var tub=Install(rooms,"hot-tub-original-exterior-model.glb",
                    id,x,z,2.30f,2.89f,1.02f);
                if(tub)
                {
                    // Original source padded rim sits at y=0.94m. An azure
                    // 1.78x1.78m surface at y=0.80m fits INSIDE the real tub.
                    // Owner water surface + bubbles, no substitute geometry.
                    BraxChampionsPoolP1Water.Jacuzzi(tub.transform,id,
                        .94f,1.78f,1.78f);
                }
            }

        // Real 2K textured premium shower cabins. When a real original
        // exists, remove old procedural shower heads to avoid duplicates.
        var shower=AssetDatabase.LoadAssetAtPath<GameObject>(Src+"shower_cabin.glb");
        if(shower)
        {
            var old=rooms.GetComponentsInChildren<Transform>(true).Where(x=>x &&
                (x.name=="Shower chrome riser"||x.name=="Shower circular head"||
                 x.name=="Shower wet-floor drain")).ToArray();
            foreach(var obj in old)UnityEngine.Object.DestroyImmediate(obj.gameObject);
            // All four real 2K shower cabins are now inside the dedicated
            // 10.8x8m shower room at X=-5.4..5.4, Z=8..16.
            // The south-facing room entrance at (0,8) remains clear.
            foreach(float x in new[]{-3.6f,-1.2f,1.2f,3.6f})
                Install(rooms,"shower_cabin.glb","PREMIUM CABIN SHOWER "+x,
                    x,12.4f,1.38f,1.16f,2.35f);
        }
        else throw new System.IO.FileNotFoundException("The owner premium shower model is required: "+Src+"shower_cabin.glb");

        // Actual owner locker banks (source 2.88x2.01x0.67m), not 18
        // separately cloned heavy texture collections. Two banks keep 2K
        // materials shared on Android.
        Install(rooms,"lockers.glb","WOMEN'S CHANGING ROOM bank",
            -29f,15.4f,3.0f,.80f,2.15f);
        Install(rooms,"lockers.glb","MEN'S CHANGING ROOM bank",
            12.4f,15.4f,3.0f,.80f,2.15f);

        // At least one real owner changing bench, kept inside the room.
        Install(rooms,"locker_room_bench_free_model.glb","FAMILY CHANGE real bench",
            -12.2f,12.2f,.55f,1.30f,.59f);
    }
}

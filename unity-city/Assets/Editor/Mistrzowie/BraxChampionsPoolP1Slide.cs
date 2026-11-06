using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Independent, replaceable P1 raft-slide installation, using exactly the
// original owner-selected GLB (SHA256 checked by prepare_pool_p1_slide.py).
// Geometry 77.65 x 33 x 127.18 source units -> 0.18 nominal scale:
// 14.0m wide, 5.94m high, 22.9m deep. Entire installation fits the
// NORTH side of the 160x120 P1 lot; no change to the original building.
public static class BraxChampionsPoolP1Slide
{
    public const string Source="Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/raft-slide-water-park.glb";
    const string Out="Assets/PlayableGenerated/Mistrzowie/Buildings/P1";
    const float NominalScale=.18f;
    const float AnchorX=-54f,AnchorZ=47f,Deck=.26f;
    static float K=>BraxClaudeOsiedle.K;
    static int serial;

    static Material Fiberglass()
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("P1 slide: CityStaticPBR missing");
        var m=new Material(shader){name="P1 actual raft slide premium azure fiberglass",
            enableInstancing=true};
        m.SetColor("_Color",new Color(.10f,.53f,.79f));
        m.SetFloat("_Roughness",.17f);
        m.SetFloat("_Metallic",0);
        m.SetFloat("_VertexTint",0);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Out+"/Materials/P1_SLIDE_azure_fiberglass.mat");
        return m;
    }
    static GameObject Box(Transform root,string name,Material material,float x,float y,float z,float w,float h,float d,bool collision=true)
    {
        if(w<=0||h<=0||d<=0)throw new ArgumentException("Invalid slide geometry "+name);
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;go.transform.SetParent(root,false);
        go.transform.localPosition=new Vector3(x,y,z)*K;
        go.transform.localScale=new Vector3(w,h,d)*K;
        go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.BatchingStatic);
        return go;
    }
    static void Railing(Transform root,Material metal,float x0,float z0,float y0,float x1,float z1,float y1)
    {
        var a=new Vector3(x0,y0,z0);var b=new Vector3(x1,y1,z1);
        var v=b-a;float len=v.magnitude;
        var rail=Box(root,"P1 slide stainless handrail anchored to proper posts",
            metal,(a.x+b.x)*.5f,(a.y+b.y)*.5f,(a.z+b.z)*.5f,len,.070f,.072f,false);
        rail.transform.localRotation=Quaternion.FromToRotation(Vector3.right,v.normalized);
    }

    public static void Generate(Transform poolOutdoors,Material stone,Material aluminium,Material poolBlue)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
        if(!source)throw new FileNotFoundException(
            "Exact original P1 raft slide GLB missing from CI verification. Do not use a substitute: "+Source,Source);
        var system=new GameObject("P1 | OWNER RAFT WATER SLIDE | REPLACEABLE MODULE");
        system.transform.SetParent(poolOutdoors,false);
        // Centre the slide as one module; X [-61,-46], Z [36.2,59.2] NOMINAL.
        var model=PrefabUtility.InstantiatePrefab(source) as GameObject;
        if(!model)throw new InvalidOperationException("Original raft slide is not a Unity GLTFast prefab");
        model.transform.SetParent(system.transform,false);
        model.name="ORIGINAL OWNER raft-slide-water-park GLB | source hash locked";
        model.transform.localPosition=new Vector3(AnchorX,Deck,AnchorZ)*K;
        model.transform.localRotation=Quaternion.identity;
        model.transform.localScale=Vector3.one*(NominalScale*K);

        var finish=Fiberglass();
        int slideSurfaces=0,stairSurfaces=0,removedDressing=0,slideColliders=0;
        foreach(var t in model.GetComponentsInChildren<Transform>(true))
        {
            if(t==model.transform)continue;
            string n=t.name.ToLowerInvariant();
            // Strip source low-poly scenery and fake water. Preserve the
            // original winding slide channel, stairs, supports, platform,
            // safety gates, landing-basin rim and its real railing.
            if(n.StartsWith("beach")||n.StartsWith("hill")||n.StartsWith("palm")||
                n.StartsWith("booth")||n.StartsWith("sign_")||n.StartsWith("poolwater")||
                n.StartsWith("slidewater")||n.StartsWith("raft_")||n.StartsWith("flag"))
            {t.gameObject.SetActive(false);removedDressing++;continue;}
            var renderers=t.GetComponents<Renderer>();
            if(n.StartsWith("slide")&&!n.StartsWith("slidesupports"))
            {
                foreach(var renderer in renderers)
                {
                    var old=renderer.sharedMaterials;
                    if(old.Length==0)continue;
                    var replacement=new Material[old.Length];
                    for(int i=0;i<replacement.Length;i++)replacement[i]=finish;
                    renderer.sharedMaterials=replacement;slideSurfaces++;
                }
            }
            // Upgrade the real 5k-triangle owner slide structure without
            // distorting its geometry. Reuse P1's existing authentic PBR
            // stainless and stone assets instead of source toy paint.
            if(n.StartsWith("slidesupports")||n=="gatepost"||n=="startgate"||n=="stairgate"||n=="poolrim")
            {
                var structureFinish=n=="poolrim"?stone:aluminium;
                foreach(var renderer in renderers)
                {
                    var originalSlots=renderer.sharedMaterials;
                    if(originalSlots.Length==0)continue;
                    var replacements=new Material[originalSlots.Length];
                    for(int slot=0;slot<replacements.Length;slot++)replacements[slot]=structureFinish;
                    renderer.sharedMaterials=replacements;
                }
            }
            if(n.StartsWith("stairs"))
            {
                // Preserve source in prefab for provenance/replacement, but do
                // NOT render a low-poly staircase over the new 18 physical
                // stone treads; coplanar doubles would flicker on Android.
                stairSurfaces++;t.gameObject.SetActive(false);continue;
            }
            if(n.StartsWith("platform"))
            {
                // The complete new load-bearing platform/guardrails replace
                // the toy source platform. No double floor or Z-fighting.
                t.gameObject.SetActive(false);continue;
            }
            if(n.StartsWith("slide")||n.StartsWith("poolrim"))
            {
                foreach(var filter in t.GetComponents<MeshFilter>())
                {
                    if(!filter.sharedMesh)continue;
                    if(!filter.GetComponent<Collider>())
                    {
                        var collision=filter.gameObject.AddComponent<MeshCollider>();
                        collision.sharedMesh=filter.sharedMesh;
                        slideColliders++;
                    }
                }
            }
        }
        if(slideSurfaces==0||stairSurfaces==0)
            throw new InvalidOperationException("P1 original slide hierarchy lost actual slide or stairs geometry");

        var structural=new GameObject("P1 engineered slide foundations | separate from source GLB");
        structural.transform.SetParent(system.transform,false);
        var st=structural.transform;

        // Entrance from family wing: clean real paving, clear of the original
        // promenade and the family roof (its north edge is nominal Z=34).
        Box(st,"P1 slide entrance real granite walkway",stone,-47.0f,Deck-.05f,37.9f,4.5f,.11f,6.0f);
        Box(st,"P1 slide entry tread base",stone,-48.1f,Deck+.04f,41.5f,2.5f,.16f,2.8f);

        // 18 actual accessible treads, level with the original source stair
        // mesh. Do not place a decorative stair across a solid glass facade.
        const int treads=18;
        for(int i=0;i<treads;i++)
        {
            float u=(i+.5f)/treads;
            float xx=Mathf.Lerp(-48.25f,-53.85f,u);
            float top=Deck+u*4.00f;
            Box(st,"P1 SLIDE ACTUAL STAIR tread "+(i+1),stone,
                xx,top-.09f,41.6f,.34f,.18f,2.15f);
            if(i%3==0)
                foreach(float side in new[]{-1f,1f})
                    Box(st,"P1 slide stair stainless railing upright",aluminium,
                        xx,top+.52f,41.6f+side*1.16f,.09f,1.04f,.09f);
        }
        foreach(float side in new[]{-1f,1f})
        {
            float zz=41.6f+side*1.16f;
            Railing(st,aluminium,-48.25f,zz,Deck+1.02f,-53.85f,zz,Deck+5.05f);
        }

        // The upper platform is not a floating cube; four real columns
        // connect it to the public deck, and 1.15m guardrails protect its sides.
        const float topLevel=Deck+4.12f;
        Box(st,"P1 water slide elevated structural boarding platform",stone,
            AnchorX,topLevel-.12f,42.6f,3.4f,.24f,3.15f);
        foreach(float x in new[]{AnchorX-1.40f,AnchorX+1.40f})
            foreach(float z in new[]{41.25f,43.9f})
                Box(st,"P1 slide independent tall galvanized post",aluminium,x,
                    Deck+(topLevel-Deck)*.5f,z,.14f,topLevel-Deck,.14f);
        foreach(float z in new[]{41.18f,44.02f})
            Railing(st,aluminium,AnchorX-1.7f,z,topLevel+1.15f,
                AnchorX+1.7f,z,topLevel+1.15f);
        // No railing across the actual stair exit or slide's starting mouth.
        foreach(float x in new[]{AnchorX-1.69f,AnchorX+1.69f})
            Railing(st,aluminium,x,41.18f,topLevel+1.15f,x,44.02f,topLevel+1.15f);
        foreach(float x in new[]{AnchorX-1.69f,AnchorX+1.69f})
            foreach(float z in new[]{41.18f,44.02f})
                Box(st,"P1 fixed platform safety rail post",aluminium,x,topLevel+.56f,z,.12f,1.15f,.12f);

        // Original landing bowl is retained, but its toy-like flat blue
        // water node was removed and replaced by the project water shader.
        Box(st,"P1 landing pool retaining base, physically connected to grade",
            stone,AnchorX,Deck-.10f,55.3f,6.5f,.20f,6.6f);
        BraxChampionsPoolP1Water.SlideLanding(st,AnchorX,55.2f,5.3f,5.3f,
            Deck+NominalScale*1.55f);
        AssetDatabase.SaveAssets();
        Debug.Log("P1 RAFT SLIDE: original SHA locked; source scale=0.18 worldK=once; "+
            "footprint 14x23m north plot; physical treads="+treads+
            " prefab slide surfaces="+slideSurfaces+" stair nodes="+stairSurfaces+
            " mesh colliders="+slideColliders+" source scenery removed="+removedDressing+
            " | water/sliding runtime device test NOT RUN");
    }
}

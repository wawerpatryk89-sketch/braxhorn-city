using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Pool P1 blue water is NOW enabled by the owner (5 Nov 2026).
// Mobile-friendly animated, translucent water with no floor collider.
public static class BraxChampionsPoolP1Water
{
    const string ShaderPath="Assets/Mistrzowie/BraxChampionsPoolWater.shader";
    const string Output="Assets/PlayableGenerated/Mistrzowie/Buildings/P1";
    static float K=>BraxClaudeOsiedle.K;
    static Shader WaterShader()
    {
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if(!shader)throw new InvalidOperationException("P1 water shader missing: "+ShaderPath);
        if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("P1 blue water shader compilation error");
        return shader;
    }
    static Material MaterialFor(string id,bool jacuzzi)
    {
        var m=new Material(WaterShader()){name="P1 actual SWIMMABLE azure water "+id};
        m.SetColor("_DeepColor",jacuzzi?new Color(.028f,.24f,.38f):new Color(.035f,.26f,.48f));
        m.SetColor("_ShallowColor",jacuzzi?new Color(.16f,.70f,.85f):new Color(.10f,.61f,.85f));
        m.SetFloat("_Opacity",jacuzzi?.70f:.69f);
        m.SetFloat("_RippleStrength",jacuzzi?1.45f:.75f);
        m.SetFloat("_Bubbles",jacuzzi?1f:0f);
        m.renderQueue=3000;
        AssetDatabase.CreateAsset(m,Output+"/Materials/P1_WATER_"+id+".mat");
        return m;
    }

    public static void Basin(Transform parent,string id,float x,float z,float w,float d,float depth,bool swimmable)
    {
        if(w<=.5f||d<=.5f||depth<=.3f)throw new ArgumentException("P1 pool water dimensions invalid: "+id);
        var group=new GameObject(id+" | REAL WATER FILLED | swim buoyancy volume").transform;
        group.SetParent(parent,false);
        var plane=GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name=id+" | turquoise animated translucent surface | NOT solid glass";
        plane.transform.SetParent(group,false);
        // Deck=.26 nominal in the P1 generator: water is below the coping.
        plane.transform.localPosition=new Vector3(x,.26f-.18f,z)*K;
        plane.transform.localRotation=Quaternion.Euler(-90,0,0); // Quad normal faces UP
        plane.transform.localScale=new Vector3(w-.20f,d-.20f,1f)*K;
        plane.GetComponent<MeshRenderer>().sharedMaterial=MaterialFor(id,false);
        var renderer=plane.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        UnityEngine.Object.DestroyImmediate(plane.GetComponent<Collider>());
        var trigger=new GameObject(id+" | trigger, swimming allowed="+swimmable);
        trigger.transform.SetParent(group,false);
        trigger.transform.localPosition=new Vector3(x,.26f-depth*.5f,z)*K;
        var box=trigger.AddComponent<BoxCollider>();
        box.isTrigger=true;
        box.size=new Vector3(w-.34f,depth+.56f,d-.34f)*K;
        var swimming=trigger.AddComponent<BraxChampionsPoolSwimVolume>();
        swimming.waterSurface=plane.transform;
        swimming.allowsSwimming=swimmable;
        swimming.basinId=id;
    }

    public static void SlideLanding(Transform parent,float x,float z,float width,float depth,float surfaceY)
    {
        // The source's opaque PoolWater is intentionally hidden by the slide
        // installer. The original curved landing rim is preserved, with a
        // real project-consistent animated surface inside.
        if(width<=0||depth<=0)throw new ArgumentException("Slide landing dimensions");
        var water=GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name="P1 REAL slide landing pool WATER | moving azure, no source toy-water";
        water.transform.SetParent(parent,false);
        water.transform.localPosition=new Vector3(x,surfaceY,z)*K;
        water.transform.localRotation=Quaternion.Euler(-90,0,0);
        water.transform.localScale=new Vector3(width,depth,1f)*K;
        var renderer=water.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=MaterialFor("SLIDE_LANDING",false);
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    // Four actual, owner-supplied jacuzzi models provide the precise surface
    // mesh position and dimensions. Mount a shader-water quad at that source
    // location; never replace the owner basin, seats, jets or access steps.
    public static void Jacuzzi(Transform p1Room,string sourceId,Vector3 sourceWaterCentre,Vector2 sourceWaterDimensions)
    {
        if(!p1Room)throw new ArgumentNullException(nameof(p1Room));
        if(sourceWaterDimensions.x<.2f||sourceWaterDimensions.y<.2f)
            throw new ArgumentOutOfRangeException(nameof(sourceWaterDimensions));
        var water=GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name="P1 "+sourceId+" | ORIGINAL basin + REAL animated jacuzzi water and foam";
        water.transform.SetParent(p1Room,false);
        water.transform.localPosition=sourceWaterCentre*K;
        water.transform.localRotation=Quaternion.Euler(-90,0,0);
        water.transform.localScale=new Vector3(sourceWaterDimensions.x,sourceWaterDimensions.y,1f)*K;
        var renderer=water.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=MaterialFor("ORIGINAL_"+sourceId,true);
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    public static void Jacuzzi(Transform approvedModel,string sourceId,float rimHeightInMetres,float surfaceWidth,float surfaceDepth)
    {
        if(!approvedModel)throw new ArgumentNullException(nameof(approvedModel));
        if(surfaceWidth<=.2f||surfaceDepth<=.2f)
            throw new ArgumentOutOfRangeException(nameof(surfaceWidth));
        // Use the water mesh that is ALREADY part of the owner's genuine
        // jacuzzi GLB as the exact dimensional/positioning authority.
        // Hide its original opaque water; otherwise it would draw over the
        // turquoise animated project shader and cause Z-fighting.
        Transform original=null;
        foreach(var child in approvedModel.GetComponentsInChildren<Transform>(true))
            if(child.name=="Spa water surface"){original=child;break;}
        if(!original)throw new InvalidOperationException(
            "Original owner jacuzzi has no named Spa water surface: "+approvedModel.name);
        var sourceRenderer=original.GetComponent<Renderer>();
        if(!sourceRenderer)throw new InvalidOperationException(
            "Original owner jacuzzi has no water-surface renderer: "+approvedModel.name);
        var measured=sourceRenderer.bounds;
        var parent=approvedModel.parent;
        if(!parent)throw new InvalidOperationException("Original jacuzzi placement needs a parent");
        var local=parent.InverseTransformPoint(measured.center)/K;
        var width=Mathf.Min(surfaceWidth,measured.size.x/K-.035f);
        var depth=Mathf.Min(surfaceDepth,measured.size.z/K-.035f);
        if(width<=.2f||depth<=.2f)throw new InvalidOperationException(
            "Approved original jacuzzi source water bounds are invalid");
        original.gameObject.SetActive(false);
        Jacuzzi(parent,sourceId,local,new Vector2(width,depth));
    }}

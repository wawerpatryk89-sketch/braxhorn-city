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

    public static void Jacuzzi(Transform approvedModel,string sourceId,float rimHeightInMetres,float surfaceWidth,float surfaceDepth)
    {
        // Called after the actual owner jacuzzi FBX/GLB has been imported and
        // measured. No 'floating water' on an absent bathtub. The existing
        // SPA room receives this water when its model has passed validation.
        if(!approvedModel)throw new ArgumentNullException(nameof(approvedModel));
        if(surfaceWidth<=.2f||surfaceDepth<=.2f)throw new ArgumentOutOfRangeException("jacuzzi water dimensions");
        var water=GameObject.CreatePrimitive(PrimitiveType.Quad);
        water.name=sourceId+" | owner jacuzzi water, animated splashes and foam";
        water.transform.SetParent(approvedModel,false);
        water.transform.localPosition=new Vector3(0,rimHeightInMetres-.14f,0);
        water.transform.localRotation=Quaternion.Euler(-90,0,0);
        water.transform.localScale=new Vector3(surfaceWidth,surfaceDepth,1f);
        var renderer=water.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=MaterialFor("OWNER_JACUZZI_"+sourceId,true);
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    }
}

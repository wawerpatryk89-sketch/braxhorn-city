using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Existing city exterior (the user's OLD buildings) cannot remain on
// BRAXHORN/OriginalCityBaked material settings with doubled light gain or
// excessive vertex-light brightening after a premium facade rebuild.
// Retune only EXPOSED nontransparent original surfaces within the 170-m
// street influence area. It must never touch gym, NOVA and Nutrition interiors,
// the source glTF asset or the estate. No additional source texture copies.
public static class BraxCityLegacyFacadeFinish
{
    const float Left=-104f,Right=70f,Near=11.2f,Far=64f;
    static readonly Bounds ProtectedNutrition=new Bounds(
        new Vector3(20.25f,2.2f,16.1f),new Vector3(12.5f,7.8f,10.9f));
    static readonly Bounds ProtectedNova=new Bounds(
        new Vector3(42f,2.2f,49f),new Vector3(21f,8.0f,26.0f));
    public static int RetunedRenderers {get;private set;}
    public static int ProtectedOriginalRenderers {get;private set;}
    public static int RetunedMaterials {get;private set;}
    public static void Apply(Transform sourceCity)
    {
        if(!sourceCity || !sourceCity.name.Contains("braxhorn-city"))
            throw new InvalidOperationException("City surface finishing requires the ORIGINAL city source");
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("PBR city shader not imported");
        var materials=new Dictionary<Material,Material>();
        RetunedRenderers=ProtectedOriginalRenderers=RetunedMaterials=0;
        foreach(var renderer in sourceCity.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
            var bounds=renderer.bounds;
            if(bounds.max.x<Left||bounds.min.x>Right ||
               bounds.max.z<Near||bounds.min.z>Far||bounds.max.y<.3f)
                continue;
            // Do not change any protected functioning interiors. The gym is
            // a separate source object and can't be reached by this method.
            if(bounds.Intersects(ProtectedNutrition) ||bounds.Intersects(ProtectedNova))
            {
                ProtectedOriginalRenderers++;
                continue;
            }
            var current=renderer.sharedMaterials;
            bool changed=false;
            for(int i=0;i<current.Length;i++)
            {
                var old=current[i];
                if(!old || !old.shader ||
                    old.shader.name!="Braxhorn/OriginalCityBaked" ||
                    BraxCityMaterials.AlphaMode(old)!=0)
                    continue;
                Material neo;
                if(!materials.TryGetValue(old,out neo))
                {
                    var tint=old.HasProperty("_Color")?old.GetColor("_Color"):Color.white;
                    // Original atlases preserve brick, stone, glass frames
                    // and site identity, but are shaded like neutral non-gloss
                    // architecture rather than bright tone-mapped billboards.
                    var colour=new Color(Mathf.Clamp(tint.r*.86f,.24f,.88f),
                        Mathf.Clamp(tint.g*.85f,.24f,.88f),
                        Mathf.Clamp(tint.b*.82f,.24f,.88f),1f);
                    neo=BraxCityMaterials.Surface(
                        "Upgraded existing street facade / matte PBR "+old.name,
                        colour,.87f,.075f);
                    if(old.HasProperty("_MainTex")&&old.GetTexture("_MainTex"))
                    {
                        // Zero extra atlas copies: source PNG/GLB remains
                        // referenced unchanged with its original UV offsets.
                        neo.SetTexture("_MainTex",old.GetTexture("_MainTex"));
                        neo.SetTextureScale("_MainTex",old.GetTextureScale("_MainTex"));
                        neo.SetTextureOffset("_MainTex",old.GetTextureOffset("_MainTex"));
                    }
                    neo.SetFloat("_VertexTint",0f);
                    neo.SetFloat("_SunIntensity",1.2f);
                    neo.SetFloat("_AmbientIntensity",.72f);
                    neo.SetFloat("_ReflectionStrength",.12f);
                    neo.SetFloat("_Roughness",.88f);
                    neo.SetFloat("_Metallic",.03f);
                    EditorUtility.SetDirty(neo);
                    materials.Add(old,neo);
                    RetunedMaterials++;
                }
                current[i]=neo;changed=true;
            }
            if(changed)
            {
                renderer.sharedMaterials=current;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                RetunedRenderers++;
            }
        }
        Debug.Log("CITY_OLD_BUILDINGS_REFINISHED_PBR activeSourceRenderers="+
            RetunedRenderers+" reusableSourceMaterials="+RetunedMaterials+
            " preservedNOVAandNutritionRenderers="+ProtectedOriginalRenderers+
            " alteredSourceGLTF=false shadedEmission=false");
    }
}

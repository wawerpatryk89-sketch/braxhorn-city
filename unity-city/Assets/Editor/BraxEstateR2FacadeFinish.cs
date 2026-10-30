using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Precise estate-only re-finishing of identified source-house wall materials.
// Keeps the imported real house geometry, source UVs, albedo/normal maps and doors.
public static class BraxEstateR2FacadeFinish
{
    [Serializable] private sealed class Audit
    {
        public string scene="BraxhornEstate",status="R2_ORIGINAL_FACADE_MATERIAL_FINISH_APPLIED__VISUAL_QA_PENDING";
        public string method="Original renderer materials cloned; source mesh, UV, texture bindings retained";
        public int renovatedMaterialSlots,renovatedSourceRoles;
        public bool originalMeshesUnchanged=true,originalBraxUnchanged=true,originalCityAndGymUnchanged=true;
        public string[] sourceRoleNames;
    }
    // Names verified against Unity original estate-manifest.json, not guessed
    // from object positions or interpreted as replacement geometry.
    private static readonly Dictionary<string,Color> Finish=new Dictionary<string,Color>
    {
        {"estate_lit_97", new Color(.91f,.91f,.88f,1f)}, // siding
        {"estate_lit_98", new Color(.97f,.96f,.93f,1f)}, // white
        {"estate_lit_108",new Color(.94f,.93f,.89f,1f)}, // stucco_indoor
        {"estate_lit_118",new Color(.91f,.91f,.87f,1f)}, // omitka
        {"estate_lit_123",new Color(.95f,.92f,.84f,1f)}, // stucco_yellow_outdoor
        {"estate_lit_124",new Color(.66f,.70f,.68f,1f)}, // stucco_dark
        {"estate_lit_132",new Color(.91f,.90f,.87f,1f)}  // M_Basic_Wall
    };
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate");
        if(!root)throw new InvalidOperationException("Original estate root missing before facade refinish");
        const string directory="Assets/PlayableGenerated/R2Approved";
        Directory.CreateDirectory(directory);
        var clones=new Dictionary<string,Material>();
        var used=new HashSet<string>();
        int slots=0;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;
            bool touched=false;
            for(int i=0;i<materials.Length;i++)
            {
                var original=materials[i];
                if(!original)continue;
                string name=original.name.Replace(" (Instance)","");
                if(!Finish.TryGetValue(name,out var tint))continue;
                var filter=renderer.GetComponent<MeshFilter>();
                if(!filter||!filter.sharedMesh)
                    throw new InvalidOperationException("Facade original source renderer lacks mesh: "+renderer.name);
                if(!clones.TryGetValue(name,out var polished))
                {
                    polished=new Material(original){name="R2 refinished original wall "+name};
                    if(polished.HasProperty("_Color"))polished.SetColor("_Color",tint);
                    if(polished.HasProperty("_BaseColor"))polished.SetColor("_BaseColor",tint);
                    if(polished.HasProperty("_Metallic"))polished.SetFloat("_Metallic",0f);
                    if(polished.HasProperty("_Glossiness"))polished.SetFloat("_Glossiness",0.14f);
                    AssetDatabase.CreateAsset(polished,directory+"/R2Wall_"+name+".mat");
                    clones.Add(name,polished);
                }
                // A material clone leaves imported original geometry, openings,
                // source textures and colliders untouched.
                materials[i]=polished;
                used.Add(name);
                slots++;
                touched=true;
            }
            if(touched)renderer.sharedMaterials=materials;
        }
        if(used.Count<2||slots<3)
            throw new InvalidOperationException("Source facade walls not detected, no fake fallback permitted: "+slots+"/"+used.Count);
        var names=new List<string>(used);names.Sort(StringComparer.Ordinal);
        var audit=new Audit{renovatedMaterialSlots=slots,renovatedSourceRoles=used.Count,sourceRoleNames=names.ToArray()};
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/r2-facade-finishes.json",JsonUtility.ToJson(audit,true));
        Debug.Log("R2_UNITY_ORIGINAL_FACADES_REFINISHED "+JsonUtility.ToJson(audit));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// R2 ONLY: apply verified Poly Haven CC0 1K PBR texture copies to original
/// imported architectural materials. Never re-create geometry, doors, glass,
/// city player, animations, source UVs or shared city/gym systems.
/// </summary>
public static class BraxEstatePremiumPBR
{
    const string Source = "Assets/Source/EstateRenovation/";
    const string Generated = "Assets/PlayableGenerated/City/";
    [Serializable] public sealed class Entry {
        public string sourceMaterial, replacement, assetId;
        public int rendererSlots;
    }
    [Serializable] public sealed class Report {
        public bool productionChanged = false, originalMeshesChanged = false;
        public string scope = "R2 ESTATE ONLY";
        public string source = "verified Poly Haven CC0 textures, max 1024px";
        public int rendererSlotsUpdated;
        public Entry[] entries;
    }
    static readonly Dictionary<string,string> Assignments =
        new Dictionary<string,string>(StringComparer.Ordinal)
        {
            // Only documented material names from the existing estate manifest.
            // Preserve original doors/windows/glass and any vertex color materials.
            {"roof", "roof_tiles"},
            {"HOUSE_ROOF", "grey_roof_01"},
            {"stucco_indoor", "white_plaster_02"},
            {"stucco_yellow_outdoor", "beige_wall_001"},
            {"M_Basic_Wall", "white_plaster_02"},
            {"PlainRedBricks_1K", "red_brick"},
            {"M_Brick_Clay_Beveled", "red_brick"}
        };
    static Material MakeMaterial(string id)
    {
        string prefix=Source+id+"_";
        Texture2D Load(string suffix)
        {
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+suffix+"_1k.png");
            if(!t)throw new InvalidOperationException("BLOCKED: missing verified R2 PBR source "+prefix+suffix+"_1k.png");
            if(Mathf.Max(t.width,t.height)>1024)throw new InvalidOperationException("BLOCKED: runtime map over 1K "+id);
            return t;
        }
        var diffuse=Load("diff");
        var normal=Load("nor_gl");
        var packed=Load("metallic_gloss");
        var shader=Shader.Find("Standard");
        if(!shader)throw new InvalidOperationException("BLOCKED: Standard PBR shader not found");
        var m=new Material(shader) {name="R2_CC0_PBR_"+id};
        m.mainTexture=diffuse;
        m.color=Color.white;
        m.SetFloat("_Metallic",0f);
        m.SetFloat("_Glossiness",.35f);
        m.SetFloat("_GlossMapScale",1f);
        m.SetTexture("_BumpMap",normal);
        m.SetFloat("_BumpScale",.65f);
        m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",packed);
        m.EnableKeyword("_METALLICGLOSSMAP");
        // Preserve source UVs; no arbitrary re-scaling or triplanar projection.
        m.mainTextureScale=Vector2.one;
        string assetPath=Generated+m.name+".mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(assetPath))
            throw new InvalidOperationException("Unexpected duplicate R2 material: "+assetPath);
        AssetDatabase.CreateAsset(m,assetPath);
        return m;
    }
    public static int Apply(GameObject estate, BraxCityBuild.MaterialData[] roles)
    {
        if(!estate||roles==null||roles.Length<100)throw new InvalidOperationException("R2 source inventory absent");
        Directory.CreateDirectory(Generated);
        var byName=roles.ToDictionary(x=>x.name,x=>x,StringComparer.Ordinal);
        var materials=new Dictionary<string,Material>(StringComparer.Ordinal);
        var changes=new Dictionary<string,Entry>(StringComparer.Ordinal);
        var count=0;
        foreach(var renderer in estate.GetComponentsInChildren<MeshRenderer>(true))
        {
            var slots=renderer.sharedMaterials;
            var modified=false;
            for(int slot=0;slot<slots.Length;slot++)
            {
                var original=slots[slot];
                if(!original)continue;
                var name=original.name.Replace(" (Instance)","");
                if(!byName.TryGetValue(name,out var role))continue;
                if(role.transparent||role.alphaTest>0||role.vertexColors||role.baked)continue;
                if(!Assignments.TryGetValue(role.originalName??"",out var id))continue;
                if(!materials.TryGetValue(id,out var replacement)) {
                    replacement=MakeMaterial(id);materials[id]=replacement;
                }
                slots[slot]=replacement;
                modified=true;count++;
                if(!changes.TryGetValue(name,out var record)) {
                    record=new Entry{sourceMaterial=role.originalName,replacement=replacement.name,assetId=id};
                    changes[name]=record;
                }
                record.rendererSlots++;
            }
            if(modified)renderer.sharedMaterials=slots;
        }
        var report=new Report{rendererSlotsUpdated=count,entries=changes.Values.OrderBy(x=>x.sourceMaterial).ToArray()};
        File.WriteAllText(Generated+"estate-pbr-material-report.json",JsonUtility.ToJson(report,true)+"\n");
        // Do not claim coverage if source material naming changed.
        if(count<4||materials.Count<3)
            throw new InvalidOperationException("BLOCKED: too few verified estate materials mapped; source preserved. Count "+count+" families "+materials.Count);
        Debug.Log("BRAXHORN_R2_PBR_VERIFIED "+JsonUtility.ToJson(report));
        return count;
    }
}

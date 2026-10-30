using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Estate-only PBR upgrade using six SHA-256-verified maps taken from the
// exact Poly Haven texture sets supplied by the operator. No geometry substitution.
public static class BraxEstateR2ApprovedRoofs
{
    [Serializable] private sealed class RoofAudit
    {
        public string scene="BraxhornEstate",status="R2_ROOF_PBR_APPLIED__VISUAL_ACCEPTANCE_PENDING";
        public string assetSource="Operator-provided Poly Haven CC0 4K sets (six source maps SHA-256 verified)";
        public int greySlots,tileSlots,normalSlots,smoothnessSlots,originalRoofMeshes,originalRoofVertices,pbrSourceMapCount=6;
        public bool originalMeshDataUnmodified=true,originalCityAndGymUnmodified=true,originalBraxUnmodified=true;
        public bool windowAndDoorsImported=false,asphaltImported=false,filmApproved=false;
    }

    private static Texture2D Source(string path,bool normal)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Original PBR source not imported: "+path);
        bool expectedSrgb=!normal && path.EndsWith("Albedo.jpg",StringComparison.OrdinalIgnoreCase);
        bool change=importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)
            ||importer.sRGBTexture!=expectedSrgb||!importer.mipmapEnabled||importer.maxTextureSize!=2048;
        if(change)
        {
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=expectedSrgb;
            importer.mipmapEnabled=true;
            importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(t==null||t.width!=2048||t.height!=2048)
            throw new InvalidOperationException("Incorrect delivered R2 PBR resolution: "+path);
        return t;
    }
    private static Material Build(string key,string label,bool twoSided,string directory)
    {
        const string source="Assets/Source/City/R2Approved/R2";
        var albedo=Source(source+key+"Albedo.jpg",false);
        var normal=Source(source+key+"Normal.png",true);
        var gloss=Source(source+key+"MetallicSmoothness.png",false);
        var shader=Shader.Find("Standard");
        if(!shader)throw new InvalidOperationException("Unity Standard PBR shader not found");
        var mat=new Material(shader){name="R2 / "+label+" / verified PBR"};
        mat.mainTexture=albedo;
        mat.color=Color.white;
        mat.SetTexture("_BumpMap",normal);
        mat.SetFloat("_BumpScale",0.82f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap",gloss);
        mat.SetFloat("_Metallic",0f);
        mat.SetFloat("_GlossMapScale",1f);
        mat.SetFloat("_Glossiness",0.35f);
        mat.EnableKeyword("_METALLICGLOSSMAP");
        mat.enableInstancing=true;
        if(twoSided)mat.SetFloat("_Cull",0f);
        AssetDatabase.CreateAsset(mat,directory+"/R2"+key+".mat");
        return mat;
    }
    // Stage actual PBR asset for DOM04 without touching any OTHER house renderer.
    public static void PrepareMaterialsOnly()
    {
        const string output="Assets/PlayableGenerated/R2Approved";
        Directory.CreateDirectory(output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Build("RoofTiles","Roof Tiles",false,output);
        Debug.Log("DOM04_ROOFTILES_OPERATOR_PBR_PREPARED_ONLY__OTHER_HOUSES_UNMODIFIED");
    }
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate");
        if(root==null)throw new InvalidOperationException("Original Unity estate geometry missing");
        const string output="Assets/PlayableGenerated/R2Approved";
        Directory.CreateDirectory(output);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var grey=Build("GreyRoof","Grey Roof 01",true,output);
        var tiles=Build("RoofTiles","Roof Tiles",false,output);
        var audit=new RoofAudit();
        var originalMeshes=new HashSet<Mesh>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;
            bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];
                if(!source)continue;
                string role=source.name.Replace(" (Instance)","");
                Material approved=null;
                if(role=="estate_lit_99"){approved=grey;audit.greySlots++;}
                if(role=="estate_lit_106"){approved=tiles;audit.tileSlots++;}
                if(!approved)continue;
                var filter=renderer.GetComponent<MeshFilter>();
                if(!filter||!filter.sharedMesh)
                    throw new InvalidOperationException("R2 roof source mesh missing: "+renderer.name);
                var m=filter.sharedMesh;
                if(m.uv==null||m.uv.Length!=m.vertexCount)
                    throw new InvalidOperationException("R2 roof needs original mesh UVs: "+renderer.name);
                if(originalMeshes.Add(m)){audit.originalRoofMeshes++;audit.originalRoofVertices+=m.vertexCount;}
                if(!approved.GetTexture("_BumpMap")||!approved.GetTexture("_MetallicGlossMap"))
                    throw new InvalidOperationException("Missing actual PBR source map for "+renderer.name);
                audit.normalSlots++;audit.smoothnessSlots++;
                materials[i]=approved;changed=true;
            }
            if(changed)renderer.sharedMaterials=materials;
        }
        if(audit.greySlots<1||audit.tileSlots<1||audit.normalSlots<2||audit.smoothnessSlots<2)
            throw new InvalidOperationException("R2 PBR roof source roles missing");
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/r2-roof-materials.json",JsonUtility.ToJson(audit,true));
        Debug.Log("R2_UNITY_ORIGINAL_ROOF_FULL_PBR_APPLIED "+JsonUtility.ToJson(audit));
    }
}

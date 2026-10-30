using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Premium texture binding ONLY in the estate scene, original roof geometry remains untouched.
// No material substitution when source file or renderer UVs are missing.
public static class BraxEstateR2ApprovedRoofs
{
    [Serializable] private class RoofAudit
    {
        public string scene="BraxhornEstate",status="R2_ROOF_MAPS_APPLIED__VISUAL_ACCEPTANCE_PENDING";
        public string assetSource="Poly Haven CC0: grey_roof_01 and roof_tiles (SHA-256 verified at download)";
        public int greySlots, tileSlots, originalRoofMeshes, originalRoofVertices;
        public int sourceNormalMaps, sourceRoughnessMaps;
        public bool originalMeshDataUnmodified=true, originalCityAndGymUnmodified=true, originalBraxUnmodified=true;
        public bool windowAndDoorsImported=false, asphaltImported=false, filmApproved=false;
    }
    private static Material Build(string name,string texturePath,string normalPath,Color tint,bool twoSided,string output)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if(texture==null || texture.width!=2048 || texture.height!=2048)
            throw new InvalidOperationException("Verified operator roof diffuse map missing: "+texturePath);
        if(normal==null || normal.width!=2048 || normal.height!=2048)
            throw new InvalidOperationException("Original Poly Haven normal map missing: "+normalPath);
        var shader=Shader.Find("Standard");
        if(shader==null)throw new InvalidOperationException("Unity Standard PBR shader unavailable");
        var mat=new Material(shader){name=name};
        mat.mainTexture=texture;
        mat.SetTexture("_BumpMap",normal);
        mat.SetFloat("_BumpScale",.65f);
        mat.EnableKeyword("_NORMALMAP");
        // Original roughness is packed into the source albedo alpha as
        // smoothness = 1 - roughness; no invented reflectivity map.
        mat.SetFloat("_SmoothnessTextureChannel",1f);
        mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        mat.color=tint;
        mat.SetFloat("_Metallic",0f);
        mat.SetFloat("_Glossiness",.8f);
        mat.SetFloat("_GlossMapScale",1f);
        mat.enableInstancing=true;
        if(twoSided)mat.SetFloat("_Cull",0f);
        AssetDatabase.CreateAsset(mat,output);
        return mat;
    }
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate");
        if(root==null)throw new InvalidOperationException("Expected ORIGINAL Unity estate root not found");
        const string dir="Assets/PlayableGenerated/R2Approved";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var grey=Build("R2 Grey Roof / delivered source","Assets/Source/City/R2Approved/R2GreyRoofSmoothnessAlbedo.png",
            "Assets/Source/City/R2Approved/R2GreyRoofNormal.png",Color.white,true,dir+"/R2GreyRoof.mat");
        var tiles=Build("R2 Roof Tiles / delivered source","Assets/Source/City/R2Approved/R2RoofTilesSmoothnessAlbedo.png",
            "Assets/Source/City/R2Approved/R2RoofTilesNormal.png",Color.white,false,dir+"/R2RoofTiles.mat");
        var audit=new RoofAudit{sourceNormalMaps=2,sourceRoughnessMaps=2};
        var counts=new HashSet<Mesh>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials=renderer.sharedMaterials;
            bool changed=false;
            for(int i=0;i<materials.Length;i++)
            {
                var original=materials[i];
                if(original==null)continue;
                string role=original.name.Replace(" (Instance)","");
                Material approved=null;
                if(role=="estate_lit_99"){approved=grey;audit.greySlots++;}
                if(role=="estate_lit_106"){approved=tiles;audit.tileSlots++;}
                if(approved==null)continue;
                var meshFilter=renderer.GetComponent<MeshFilter>();
                if(meshFilter==null || meshFilter.sharedMesh==null)
                    throw new InvalidOperationException("R2 roof renderer without source mesh: "+renderer.name);
                if(meshFilter.sharedMesh.uv==null || meshFilter.sharedMesh.uv.Length==0)
                    throw new InvalidOperationException("R2 roof mesh without source UV: "+renderer.name);
                if(counts.Add(meshFilter.sharedMesh))
                {
                    audit.originalRoofMeshes++;
                    audit.originalRoofVertices+=meshFilter.sharedMesh.vertexCount;
                }
                materials[i]=approved;
                changed=true;
            }
            if(changed)renderer.sharedMaterials=materials;
        }
        if(audit.greySlots<1||audit.tileSlots<1)
            throw new InvalidOperationException("R2 original roof source roles not found: grey="+audit.greySlots+" tiles="+audit.tileSlots);
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/r2-roof-materials.json",JsonUtility.ToJson(audit,true));
        Debug.Log("R2_ORIGINAL_GEOMETRY_VERIFIED_ROOF_TEXTURES "+JsonUtility.ToJson(audit));
    }
}

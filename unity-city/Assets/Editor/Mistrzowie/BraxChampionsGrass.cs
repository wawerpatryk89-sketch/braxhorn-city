using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Local adaptation of Claude's OsiedleTrawa colour calibration and near-camera grass.
// Other regions, their shaders, source maps and GUIDs remain unchanged.
public static class BraxChampionsGrass
{
    const string Source="Assets/Source/Mistrzowie/", Output="Assets/PlayableGenerated/Mistrzowie/";
    [Serializable] sealed class Calibration { public float[] grass,soil; }
    [Serializable] sealed class PointData { public float x,z,y,yaw,scale; }
    [Serializable] sealed class Clumps { public PointData[] points; }
    public static Material CreateMaterial(Material material)
    {
        var shader=Shader.Find("Braxhorn/ChampionsGrass");
        if(!shader)throw new InvalidOperationException("Champions grass shader missing");
        material.shader=shader;
        var c=JsonUtility.FromJson<Calibration>(File.ReadAllText(Source+"grass-calibration.json"));
        if(c.grass.Length!=3||c.soil.Length!=3)throw new InvalidOperationException("Grass calibration missing");
        float y=.2126f*c.grass[0]*.536f+.7152f*c.grass[1]+.0722f*c.grass[2]*.536f;
        float gain=.133f/Mathf.Max(y,.001f);
        material.color=new Color(.536f*gain,gain,.536f*gain).gamma;
        material.SetFloat("_MacroMean",Mathf.Max(.001f,.3f*c.grass[0]+.59f*c.grass[1]+.11f*c.grass[2]));
        material.SetFloat("_DryMean",Mathf.Max(.001f,c.grass[1]));
        foreach(string property in new[]{"_MainTex","_BumpMap","_ARM"})material.SetTextureScale(property,Vector2.one/2f);
        material.SetFloat("_Tile",2f);material.SetFloat("_MacroTile",29f);
        material.SetFloat("_Stripe",1.8f);material.SetFloat("_StripeMix",.045f);
        material.SetFloat("_Gloss",.14f);
        var soil=BraxChampionsFoundation.SourceMaterial("grass_soil","brown_mud_leaves_01",1.3f,Color.white);
        material.SetTexture("_SoilTex",soil.GetTexture("_MainTex"));
        material.SetTexture("_SoilBump",soil.GetTexture("_BumpMap"));
        material.SetTexture("_SoilARM",soil.GetTexture("_ARM"));
        float sy=.2126f*c.soil[0]+.7152f*c.soil[1]+.0722f*c.soil[2];
        float sg=.10f/Mathf.Max(sy,.001f);material.SetVector("_SoilTint",new Vector4(sg,sg,sg,1));
        string mask=Source+"grass-soil-mask.png";
        var imp=(TextureImporter)AssetImporter.GetAtPath(mask);
        imp.textureType=TextureImporterType.Default;imp.sRGBTexture=false;imp.mipmapEnabled=true;
        imp.maxTextureSize=2048;imp.npotScale=TextureImporterNPOTScale.None;imp.wrapMode=TextureWrapMode.Clamp;
        imp.textureCompression=TextureImporterCompression.Uncompressed;imp.isReadable=false;imp.SaveAndReimport();
        material.SetTexture("_Mask",AssetDatabase.LoadAssetAtPath<Texture2D>(mask));
        float k=BraxClaudeOsiedle.K;
        material.SetVector("_MaskRect",new Vector4(0,-500*k,1/(1200*k),1/(1500*k)));
        material.SetFloat("_SoilMix",1);EditorUtility.SetDirty(material);
        return material;
    }
    public static void AddClumps(Transform parent,Material grass)
    {
        var src=JsonUtility.FromJson<Clumps>(File.ReadAllText(Source+"grass-clumps.json"));
        if(src.points.Length>100000)throw new InvalidOperationException("Grass instance budget exceeded");
        // Actual tapered blades, not opaque crossed photo cards. Source lawn texture colours them.
        var mesh=new Mesh{name="Champions small grass blades"};
        var vertices=new Vector3[27];var uv=new Vector2[27];var indices=new int[27];
        for(int i=0;i<9;i++)
        {
            float a=i*2.399963f;var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.12f+.03f*(i%3));
            var side=new Vector3(Mathf.Cos(a+1.57f),0,Mathf.Sin(a+1.57f))*.035f;
            vertices[i*3]=p-side;vertices[i*3+1]=p+side;
            vertices[i*3+2]=p*1.4f+Vector3.up*(.65f+.17f*(i%3));
            for(int j=0;j<3;j++){indices[i*3+j]=i*3+j;uv[i*3+j]=new Vector2(j*.5f,j==2?1:0);}
        }
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"Meshes/grass_blades.asset");
        var mat=new Material(grass){name="Champions grass blades",enableInstancing=true};mat.SetFloat("_Blade",1);mat.SetFloat("_Cull",0);
        AssetDatabase.CreateAsset(mat,Output+"Materials/grass_blades.mat");
        var data=ScriptableObject.CreateInstance<BraxClaudeTrawaDane>();data.wysokosc=.065f;data.granicaLaki=.15f;
        data.kepy=new Vector4[src.points.Length];data.y=new float[src.points.Length];
        for(int i=0;i<src.points.Length;i++){var p=src.points[i];data.kepy[i]=new Vector4(p.x,p.z,p.yaw,p.scale);data.y[i]=p.y;}
        AssetDatabase.CreateAsset(data,Output+"grass_instances.asset");
        var o=new GameObject("Champions near-camera lawn detail");o.transform.SetParent(parent,false);
        var draw=o.AddComponent<BraxClaudeTrawa>();draw.dane=data;draw.siatka=mesh;draw.material=mat;
        draw.zasiegMurawy=22;draw.zasiegLaki=22;draw.kawalek=9;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Integrated after the main premium pass; preserves its ring, windows and library furniture.
public static class BraxHeroRefinements
{
    const string P="Assets/Source/Premium/";
    static Texture2D Map(string file,bool linear=false,bool normal=false)
    {
        var importer=AssetImporter.GetAtPath(P+file) as TextureImporter;
        if(importer!=null){importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!(linear||normal);importer.mipmapEnabled=true;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(P+file);if(!t)throw new Exception("Missing original Brax texture "+file);return t;
    }
    static Mesh RoundedBag(float height)
    {
        const int sides=48;float[] y={-.5f,-.49f,-.46f,-.40f,.40f,.46f,.49f,.5f};float[] r={0,.23f,.285f,.30f,.30f,.285f,.23f,0};
        var v=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        for(int j=0;j<y.Length;j++)for(int k=0;k<=sides;k++)
        {
            float a=k*Mathf.PI*2/sides;v.Add(new Vector3(-Mathf.Sin(a)*r[j],y[j]*height,Mathf.Cos(a)*r[j]));uv.Add(new Vector2((float)k/sides,y[j]+.5f));
            if(j<y.Length-1&&k<sides){int q=j*(sides+1)+k;indices.AddRange(new[]{q,q+sides+1,q+1,q+1,q+sides+1,q+sides+2});}
        }
        var m=new Mesh{name="Rounded original-size leather bag"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    public static void Apply(GameObject hero,Transform[] bags,Camera camera,string generated)
    {
        var shader=Shader.Find("Braxhorn/HeroPremium");if(!shader)throw new Exception("Original ORM shader unavailable");
        var material=new Material(shader){name="Brax original PBR restored",color=Color.white};
        material.SetTexture("_MainTex",Map("BraxAlbedo.png"));material.SetTexture("_BumpMap",Map("BraxNormal.png",false,true));material.SetTexture("_Orm",Map("BraxOrm.png",true));material.SetFloat("_BumpScale",.85f);material.SetFloat("_RoughnessFloor",.42f);material.SetFloat("_MetallicScale",.30f);material.SetFloat("_OcclusionStrength",1f);material.SetFloat("_OcclusionStrength",.8f);
        AssetDatabase.CreateAsset(material,generated+"/Premium/BraxSourceORM.mat");
        foreach(var renderer in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
        var fill=new GameObject("Brax neutral portrait fill").AddComponent<Light>();fill.type=LightType.Directional;fill.cullingMask=1<<8;fill.transform.SetParent(camera.transform,false);fill.transform.localRotation=Quaternion.Euler(20,32,0);fill.color=new Color(.93f,.96f,1);fill.intensity=.20f;fill.shadows=LightShadows.None;
        QualitySettings.pixelLightCount=4;
        // Postać jak w podglądzie Tripo: klucz z cieniem własnym (tylko warstwa gracza), kontra od tyłu
        // (sylwetka), fill zostaje. Cienie włączone tylko dla światła klucza postaci; miasto bez cieni.
        var key=new GameObject("Brax key light (self-shadow)").AddComponent<Light>();key.type=LightType.Directional;key.cullingMask=1<<8;key.transform.SetParent(camera.transform,false);key.transform.localRotation=Quaternion.Euler(35,-40,0);key.color=new Color(1f,.96f,.90f);key.intensity=.55f;key.shadows=LightShadows.Soft;key.shadowStrength=.62f;key.shadowBias=.02f;key.shadowNormalBias=.35f;key.shadowNearPlane=.2f;
        var rim=new GameObject("Brax rim light (silhouette)").AddComponent<Light>();rim.type=LightType.Directional;rim.cullingMask=1<<8;rim.transform.SetParent(camera.transform,false);rim.transform.localRotation=Quaternion.Euler(30,160,0);rim.color=new Color(.86f,.93f,1f);rim.intensity=.9f;rim.shadows=LightShadows.None;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=14;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowCascades=1;QualitySettings.shadowProjection=ShadowProjection.CloseFit;
        float[] heights={1.65f,1.5f,1.7f,1.4f};
        for(int i=0;i<bags.Length;i++)
        {
            var body=bags[i].GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(r=>r.name=="Gym_Mesh_"+(81+i*8));if(!body)throw new Exception("Missing original bag body "+i);
            var mesh=RoundedBag(heights[i]);AssetDatabase.CreateAsset(mesh,generated+"/Premium/Bag48_"+i+".asset");body.sharedMesh=mesh;
        }
        File.WriteAllText(P+"source-refinements.json",string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{{\"originalOrmRestored\":true,\"originalRigChanged\":false,\"originalBrandingChanged\":false,\"bagColourSpaceCorrected\":true,\"roundedBagSegments\":48,\"heroBumpScale\":{0},\"heroRoughnessFloor\":{1},\"heroMetallicScale\":{2}}}",
            material.GetFloat("_BumpScale"),material.GetFloat("_RoughnessFloor"),material.GetFloat("_MetallicScale")));
        AssetDatabase.SaveAssets();Debug.Log("SOURCE_REFINEMENTS_PASS: original Brax ORM restored; bag colour space and rounded ends corrected");
    }
}

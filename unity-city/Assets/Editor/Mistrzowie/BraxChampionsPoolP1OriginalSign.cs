using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// P1 signage only. Use the owner's exact original 2048x512 CitySigns PNG.
// No generated lettering, substitute artwork, recolouring or cropping.
// The structural 3D carrier is created only after the correct original is loaded.
public static class BraxChampionsPoolP1OriginalSign
{
    const string Signs="Assets/Source/Mistrzowie/CitySigns";
    const string Out="Assets/PlayableGenerated/Mistrzowie/Buildings/P1";
    const float W=10.0f,H=2.5f; // exact 4:1, scaled to the SPA canopy/roof clearance
    static float K=>BraxClaudeOsiedle.K;

    public static string FindOriginal()
    {
        if(!AssetDatabase.IsValidFolder(Signs))return null;
        var candidates=AssetDatabase.FindAssets("t:Texture2D",new[]{Signs})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)
                && Path.GetFileNameWithoutExtension(p).IndexOf("BASEN",StringComparison.OrdinalIgnoreCase)>=0)
            .OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        // If multiple assets have BASEN in their names, do not guess which
        // one the owner approved for P1. Refuse to replace his artwork.
        if(candidates.Length!=1)
        {
            Debug.LogWarning("P1 OWNER SIGN NOT VERIFIED: expected exactly one original BASEN PNG in "+
                Signs+"; candidates="+candidates.Length+" ["+string.Join(", ",candidates)+
                "]. The front stays unsigned rather than displaying an invented logo.");
            return null;
        }
        return candidates[0];
    }

    static Color EdgeFromSource(string path)
    {
        // The carrier finish is derived from actual pixels, not from a guessed
        // brand colour. The original graphic is NOT modified or regenerated.
        if(!File.Exists(path))throw new FileNotFoundException("Original sign image missing: "+path);
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            if(!ImageConversion.LoadImage(image,File.ReadAllBytes(path),false))
                throw new InvalidDataException("Could not read owner original BASEN sign PNG");
            var coords=new[]{new Vector2Int(8,8),new Vector2Int(image.width-9,8),
                new Vector2Int(8,image.height-9),new Vector2Int(image.width-9,image.height-9)};
            Color sum=Color.black;int n=0;
            foreach(var p in coords)
            {
                var c=image.GetPixel(p.x,p.y);
                if(c.a<.5f)continue;
                sum+=c;n++;
            }
            // For transparent-edge artwork, the safe, nonbranding support
            // material is neutral brushed aluminium rather than a fake colour.
            if(n==0)return new Color(.70f,.74f,.76f,1f);
            var average=sum/n;average.a=1f;
            return average;
        }
        finally {UnityEngine.Object.DestroyImmediate(image);}
    }

    static Material CreateMaterial(string name,Color color,float metallic,float roughness)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Existing CityStaticPBR shader is required for P1 signage");
        var mat=new Material(shader){name=name,enableInstancing=true};
        mat.SetColor("_Color",color);
        mat.SetFloat("_Roughness",roughness);
        mat.SetFloat("_Metallic",metallic);
        mat.SetFloat("_VertexTint",0f);
        mat.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(mat,Out+"/Materials/"+name+".mat");
        return mat;
    }

    static GameObject Part(Transform parent,string name,Material material,
        float x,float y,float z,float w,float h,float d,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=new Vector3(x,y,z)*K;
        go.transform.localScale=new Vector3(w,h,d)*K;
        go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static bool Generate(Transform p1Root)
    {
        var source=FindOriginal();
        if(string.IsNullOrEmpty(source))
        {
            var pending=new GameObject("P1 ORIGINAL BASEN SIGN | SOURCE ZIP NOT YET IMPORTED");
            pending.transform.SetParent(p1Root,false);
            pending.transform.localPosition=new Vector3(0,6.6f,-28.5f)*K;
            return false;
        }
        var sourceTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(!sourceTexture || sourceTexture.width!=2048 || sourceTexture.height!=512)
            throw new InvalidOperationException("Original owner BASEN sign must be 2048x512 (4:1): "+source);
        var importer=AssetImporter.GetAtPath(source) as TextureImporter;
        if(!importer || !importer.sRGBTexture || !importer.mipmapEnabled)
            throw new InvalidOperationException("Original BASEN sign texture has invalid colour/mipmap import: "+source);
        var bodyColor=EdgeFromSource(source);
        var matchingBody=CreateMaterial("P1_SIGN_ORIGINAL_ART_EDGE_FINISH",bodyColor,.50f,.36f);
        var aluminium=CreateMaterial("P1_SIGN_BRUSHED_ALUMINIUM_FRAME",new Color(.73f,.76f,.78f),.82f,.29f);
        var led=CreateMaterial("P1_SIGN_EDGE_WARM_LED",new Color(1f,.94f,.82f),0f,.30f);
        led.SetColor("_EmissionColor",new Color(.65f,.52f,.35f));
        led.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(led);
        var artMat=CreateMaterial("P1_SIGN_EXACT_OWNER_2048x512",Color.white,0f,.35f);
        artMat.SetTexture("_MainTex",sourceTexture);
        artMat.SetFloat("_Transparent",1f);
        artMat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        artMat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        artMat.SetFloat("_ZWrite",0f);
        artMat.SetOverrideTag("RenderType","Transparent");
        artMat.renderQueue=3000;
        EditorUtility.SetDirty(artMat);

        var frame=new GameObject("P1 OWNER'S EXACT BASEN SIGN | 4:1 | actual 3D cladded carrier");
        frame.transform.SetParent(p1Root,false);
        const float cy=6.95f,cz=-28.40f; // bottom clears 5.14m canopy, top below 8.5m roof
        // Casing centre and thickness: the artwork is on its outside, z<0.
        Part(frame.transform,"physical colour-matched 3D signage casing",
            matchingBody,0,cy,cz,W+.22f,H+.22f,.30f,true);
        Part(frame.transform,"aluminium top cap",aluminium,0,cy+H*.5f+.13f,cz-.08f,W+.44f,.12f,.47f);
        Part(frame.transform,"aluminium bottom cap",aluminium,0,cy-H*.5f-.13f,cz-.08f,W+.44f,.12f,.47f);
        foreach(float side in new[]{-1f,1f})
        {
            Part(frame.transform,"aluminium vertical extrusion",aluminium,side*(W*.5f+.13f),cy,cz-.08f,.11f,H+.22f,.47f);
            for(float x=2.4f;x<=5.0f;x+=2.6f)
                Part(frame.transform,"concealed wall standoff mount",aluminium,side*x,cy,cz+.26f,.14f,.30f,.45f);
        }
        Part(frame.transform,"architectural warm edge LED TOP",led,0,cy+H*.5f+.055f,cz-.28f,W,.04f,.045f);
        Part(frame.transform,"architectural warm edge LED BOTTOM",led,0,cy-H*.5f-.055f,cz-.28f,W,.04f,.045f);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name="ORIGINAL BASEN PRINT | no crop | no recolour | no generated text";
        quad.transform.SetParent(frame.transform,false);
        quad.transform.localPosition=new Vector3(0,cy,cz-.177f)*K;
        quad.transform.localRotation=Quaternion.Euler(0f,180f,0f);
        quad.transform.localScale=new Vector3(W,H,1f)*K;
        quad.GetComponent<MeshRenderer>().sharedMaterial=artMat;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        Debug.Log("P1 OWNER SIGN: ORIGINAL CitySigns graphic used unchanged "+source+
            " (2048x512), casing matched to original edge pixels, aspect 4:1.");
        return true;
    }
}

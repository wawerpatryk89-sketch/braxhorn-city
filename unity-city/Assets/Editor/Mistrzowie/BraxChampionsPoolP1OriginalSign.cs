using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// EXACT owner-approved originals, from the owner's 148 CitySigns bundle.
// P1 BASEN and P1 SPA are distinct, untouched 2048x512 PNGs, their original
// Unity GUIDs are imported from the private source pack. NO generated text.
public static class BraxChampionsPoolP1OriginalSign
{
    const string Root="Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/";
    public const string PoolSign=Root+"P1_BASEN_BASEN_MIEJSKI.png";
    public const string SpaSign=Root+"P1_SPA_SPA_WELLNESS.png";
    const string Out="Assets/PlayableGenerated/Mistrzowie/Buildings/P1";
    static float K=>BraxClaudeOsiedle.K;

    public static string FindOriginal()
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PoolSign)?PoolSign:null;
    }

    static Color BodyFromOriginal(string path)
    {
        // Only sample actual SOURCE edge pixels for carrier paint. The image
        // remains byte-for-byte the owner's source (alpha and fonts included).
        var pixels=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            if(!File.Exists(path)||!ImageConversion.LoadImage(pixels,File.ReadAllBytes(path),false))
                throw new FileNotFoundException("P1 original sign source unreadable",path);
            Color sum=Color.black;int count=0;
            foreach(var p in new[]{
                new Vector2Int(8,8),new Vector2Int(pixels.width-9,8),
                new Vector2Int(8,pixels.height-9),new Vector2Int(pixels.width-9,pixels.height-9)})
            {
                var c=pixels.GetPixel(p.x,p.y);
                if(c.a<.5f)continue;
                sum+=c;count++;
            }
            if(count==0)return new Color(.73f,.76f,.78f);
            var result=sum/count;result.a=1f;return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(pixels);}
    }

    static Material Material(string name,Color color,float metal,float rough)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("P1 signage requires city PBR shader");
        var material=new Material(shader){name=name,enableInstancing=true};
        material.SetColor("_Color",color);
        material.SetFloat("_Roughness",rough);
        material.SetFloat("_Metallic",metal);
        material.SetFloat("_VertexTint",0);
        material.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(material,Out+"/Materials/"+name+".mat");
        return material;
    }

    static void Part(Transform parent,string name,Material material,
        float x,float y,float z,float width,float height,float thickness,bool collision=false)
    {
        var p=GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name=name;p.transform.SetParent(parent,false);
        p.transform.localPosition=new Vector3(x,y,z)*K;
        p.transform.localScale=new Vector3(width,height,thickness)*K;
        p.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(!collision)UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());
    }

    static bool Build(Transform p1,string id,string path,float x,float y,float z,float width)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!texture)
        {
            var pending=new GameObject("P1 ORIGINAL OWNER SOURCE REQUIRED | "+path);
            pending.transform.SetParent(p1,false);
            pending.transform.localPosition=new Vector3(x,y,z)*K;
            Debug.LogWarning("P1 sign source is not imported: "+path);
            return false;
        }
        if(texture.width!=2048||texture.height!=512)
            throw new InvalidOperationException("P1 owner sign not original 2048x512: "+path);
        var h=width/4f;
        var body=Material("P1_SIGN_"+id+"_CARRIER_EDGE_SOURCE_COLOR",
            BodyFromOriginal(path),.58f,.34f);
        var trim=Material("P1_SIGN_"+id+"_REAL_ALUMINIUM_TRIM",
            new Color(.73f,.77f,.80f),.80f,.29f);
        var led=Material("P1_SIGN_"+id+"_LED_DIFFUSER",
            new Color(1f,.93f,.79f),0f,.32f);
        led.SetColor("_EmissionColor",new Color(.35f,.27f,.18f));
        led.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(led);
        var art=Material("P1_SIGN_"+id+"_ORIGINAL_OWNER_ART_UNCHANGED",Color.white,0f,.40f);
        art.SetTexture("_MainTex",texture);
        art.SetTexture("_EmissionMap",texture);
        art.SetColor("_EmissionColor",new Color(.07f,.06f,.05f));
        art.SetFloat("_Transparent",1f);
        art.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        art.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        art.SetFloat("_ZWrite",0f);
        art.SetOverrideTag("RenderType","Transparent");
        art.renderQueue=3000;
        EditorUtility.SetDirty(art);

        var structure=new GameObject("P1 | ORIGINAL "+id+" sign | exact 4:1 hard-body carrier");
        structure.transform.SetParent(p1,false);
        var support=structure.transform;
        Part(support,"Owner colour-matched deep 3D sign case",body,
            x,y,z,width+.18f,h+.18f,.29f,true);
        Part(support,"Aluminium top edge profile",trim,
            x,y+h*.5f+.09f,z-.075f,width+.39f,.10f,.44f);
        Part(support,"Aluminium bottom edge profile",trim,
            x,y-h*.5f-.09f,z-.075f,width+.39f,.10f,.44f);
        foreach(float side in new[]{-1f,1f})
        {
            Part(support,"Aluminium vertical edge",trim,
                x+side*(width*.5f+.09f),y,z-.075f,.10f,h+.17f,.44f);
            Part(support,"Steel rear wall mount",trim,
                x+side*width*.32f,y,z+.22f,.12f,.28f,.35f,true);
        }
        Part(support,"Warm facade edge LED top",led,
            x,y+h*.5f+.045f,z-.26f,width,.028f,.032f);
        Part(support,"Warm facade edge LED bottom",led,
            x,y-h*.5f-.045f,z-.26f,width,.028f,.032f);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name="ORIGINAL P1 "+id+" PNG | source 2048x512 | no crop recolour retyping";
        quad.transform.SetParent(support,false);
        quad.transform.localPosition=new Vector3(x,y,z-.161f)*K;
        quad.transform.localRotation=Quaternion.Euler(0,180,0);
        quad.transform.localScale=new Vector3(width,h,1f)*K;
        quad.GetComponent<MeshRenderer>().sharedMaterial=art;
        UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        Debug.Log("P1 OWNER ORIGINAL SIGN installed unchanged: "+path+
            " ratio 4:1, carrier tied to original PNG edge colour");
        return true;
    }

    public static bool Generate(Transform root)
    {
        if(!root)throw new ArgumentNullException(nameof(root));
        // Front lobby centred; 10m case top ~8.14m stays BELOW actual roof underside 8.20m, bottom ~5.46m above canopy top 5.14m.
        bool basen=Build(root,"BASEN_MIEJSKI",PoolSign,0f,6.80f,-28.40f,10.0f);
        // Second owner-approved SPA sign on the existing right solid wood
        // wing, not across the clear glass nor overlapping the lobby plaque.
        bool spa=Build(root,"SPA_WELLNESS",SpaSign,15.2f,5.37f,-28.40f,4.40f);
        return basen&&spa;
    }
}

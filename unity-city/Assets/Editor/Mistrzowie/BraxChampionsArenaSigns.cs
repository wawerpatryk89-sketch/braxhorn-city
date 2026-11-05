using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxChampionsArenaSigns
{
    const string Source="Assets/Source/Mistrzowie/ArenaSigns/";
    const string Out="Assets/PlayableGenerated/Mistrzowie/Buildings/";
    static readonly string[] Names={
        "champion_arena_red_front.png",
        "champion_arena_gold_front.png",
        "champion_arena_red_fight_night.png",
        "champion_arena_gold_arena_walk.png"
    };
    public static bool HasSources()
    {
        foreach(var file in Names)
            if(!AssetDatabase.LoadAssetAtPath<Texture2D>(Source+file))return false;
        return true;
    }
    static Material MakeMat(int index)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("City PBR shader missing");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+Names[index]);
        var m=new Material(shader){name="Owner's Champion Arena sign "+index,enableInstancing=true};
        m.SetTexture("_MainTex",texture);
        m.SetTexture("_EmissionMap",texture);
        m.SetColor("_EmissionColor",new Color(.13f,.105f,.10f));
        m.SetColor("_Color",Color.white);
        m.SetFloat("_VertexTint",0);
        m.SetFloat("_Roughness",.35f);
        m.SetFloat("_Metallic",0);
        m.SetFloat("_AlphaClip",1);
        m.SetFloat("_Cutoff",.12f);
        m.renderQueue=2450;
        m.SetOverrideTag("RenderType","TransparentCutout");
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Out+"Materials/champion_sign_"+index+".mat");
        return m;
    }
    static void Curved(Transform root,int index,Material mat,bool front,bool east,float length,float bottom,float height)
    {
        const int steps=48;
        float k=BraxClaudeOsiedle.K;
        var verts=new Vector3[(steps+1)*2];
        var uvs=new Vector2[verts.Length];
        var triangles=new int[steps*6];
        bool alongX=front;
        bool positive=front ? east : east;
        for(int i=0;i<=steps;i++)
        {
            float t=i/(float)steps, p=(t-.5f)*length;
            float x,z;
            if(alongX)
            {
                x=p;
                z=(positive?1f:-1f)*59f*Mathf.Sqrt(1f-p*p/(96f*96f))+(positive?.36f:-.36f);
            }
            else
            {
                z=p;
                x=(positive?1f:-1f)*96f*Mathf.Sqrt(1f-p*p/(59f*59f))+(positive?.36f:-.36f);
            }
            verts[2*i]=new Vector3(x,bottom,z)*k;
            verts[2*i+1]=new Vector3(x,bottom+height,z)*k;
            uvs[2*i]=new Vector2(t,0);
            uvs[2*i+1]=new Vector2(t,1);
            if(i==steps)continue;
            int a=2*i,n=a+2,j=6*i;
            bool flip=alongX?positive:!positive;
            if(flip)
            {
                triangles[j]=a;triangles[j+1]=n+1;triangles[j+2]=a+1;
                triangles[j+3]=a;triangles[j+4]=n;triangles[j+5]=n+1;
            }
            else
            {
                triangles[j]=a;triangles[j+1]=a+1;triangles[j+2]=n+1;
                triangles[j+3]=a;triangles[j+4]=n+1;triangles[j+5]=n;
            }
        }
        var mesh=new Mesh{name="A1 curved owner's sign "+index};
        mesh.vertices=verts;mesh.uv=uvs;mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Out+"Meshes/A1_sign_"+index+".asset");
        var go=new GameObject("A1 facade | original premium sign "+index);
        go.transform.SetParent(root,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
    }
    public static bool Generate(Transform arenaRoot)
    {
        if(!HasSources())
        {
            Debug.LogWarning("CHAMPION_ARENA_SIGNS_MISSING: supplied artwork not imported into "+Source);
            return false;
        }
        var t=new GameObject("Owner's four CHAMPION ARENA signs").transform;
        t.SetParent(arenaRoot,false);
        Curved(t,0,MakeMat(0),true,false,48f,11.12f,6.9f);
        Curved(t,1,MakeMat(1),true,true,42f,11.65f,5.9f);
        Curved(t,2,MakeMat(2),false,true,34f,11.7f,5.7f);
        Curved(t,3,MakeMat(3),false,false,34f,11.7f,5.7f);
        return true;
    }
}

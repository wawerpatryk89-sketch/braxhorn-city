using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// One way to hang an owner 6144 x 2048 mural, used by all three (entrance, left, right).
// Owner 16.11 13:52-13:53: "arena od 10 buildów nie ma murali ... to dzielone ... usuwasz tamte złe ... dajesz nowe co ci
// dałem". Since 9.11 every mural was cut into three 2048 tiles (split_arena_murals.py in CI) and none was seen on the phone.
// Now, for every mural:
// * THE OWNER'S WHOLE PICTURE, NOT SPLIT: the one 6144 x 2048 file he sent (the release copy has the same SHA-256 as his
//   attachment, checked in CI by install_arena_murals_hd.py / verify_arena_three_murals.py) on ONE panel. No tiles, no crop.
// * WHY IT DREW BLACK: the panel had no mesh tangents, so the region shader built its normal frame from screen
//   derivatives; on a picture 30-90 m wide seen from across the hall that frame underflows a phone's half precision and the
//   surface came out NaN = black (BraxChampionsPBR fixed 16.11). The panel now carries real tangents and white vertex
//   colours, so the shader never takes that path for it at all.
// * A BACK-LIT PRINT: the picture is its own light (emission .86, albedo .30), so it shows the owner's colours at
//   full strength on a wall the sun never reaches.
public static class BraxChampionsArenaMuralPrint
{
    const string Output="Assets/PlayableGenerated/Mistrzowie/Arena/Mural";
    public const float Glow=.86f, Albedo=.30f, FrameBar=.16f;

    static Material Print(Texture2D picture,string name,string asset)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 mural CityStaticPBR shader missing");
        var m=new Material(shader){name=name,enableInstancing=true};
        m.SetTexture("_MainTex",picture);
        m.SetTexture("_EmissionMap",picture);
        m.SetColor("_Color",new Color(Albedo,Albedo,Albedo));
        m.SetColor("_EmissionColor",new Color(Glow,Glow,Glow));
        m.SetFloat("_Metallic",0f);m.SetFloat("_Roughness",.90f);m.SetFloat("_VertexTint",0f);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        // owner 12.11 00:30-00:55 (APK 38: "dwa murale ... po prawej i po lewej ... ich nie ma"): the print is drawn from both
        // sides, so no winding / camera side can ever drop it; it stays self-lit (emission = the picture)
        m.SetFloat("_Cull",0f);
        AssetDatabase.CreateAsset(m,asset);
        return m;
    }

    static Texture2D Load(string path)
    {
        if(!File.Exists(path))return null;
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void Quad(Transform parent,string name,string id,Material material,Vector3 bl,Vector3 right,float width,float height,
        Vector3 facing,float u0,float u1)
    {
        Vector3 br=bl+right*width,tl=bl+Vector3.up*height,tr=br+Vector3.up*height;
        var mesh=new Mesh{name=name};
        mesh.vertices=new[]{bl,tl,tr,br};
        mesh.uv=new[]{new Vector2(u0,0),new Vector2(u0,1),new Vector2(u1,1),new Vector2(u1,0)};
        // Unity front = clockwise: the normal of (a,b,c) is (b-a) x (c-a). Face the viewer, whatever the wall.
        bool flip=Vector3.Dot(Vector3.Cross(tl-bl,tr-bl),facing)<0;
        mesh.triangles=flip?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3};
        // normal = facing, tangent = along the picture (u grows to the viewer's right), white colours: the shader's
        // supplied-tangent path, never the screen-derivative one that drew the murals black on the phone
        var nrm=facing.normalized;
        var tan=new Vector4(right.x,right.y,right.z,Vector3.Dot(Vector3.Cross(nrm,right),Vector3.up)<0f?-1f:1f);
        mesh.normals=new[]{nrm,nrm,nrm,nrm};
        mesh.RecalculateTangents();
        var tg=mesh.tangents;
        if(tg==null||tg.Length!=4||new Vector3(tg[0].x,tg[0].y,tg[0].z).sqrMagnitude<.5f)mesh.tangents=new[]{tan,tan,tan,tan};
        mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
        mesh.RecalculateBounds();mesh.RecalculateUVDistributionMetrics();
        AssetDatabase.CreateAsset(mesh,Output+"/"+id+".asset");
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        // A printed surface only: no collider, and never static-batched with anything else.
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
    }

    // bottomLeft: lower-left corner AS THE VIEWER SEES IT; right: unit vector to the viewer's right;
    // facing: unit vector from the print toward the viewer. Width : height must be 3 : 1.
    // Returns 1 (the owner's whole picture on one panel) or 0 when the owner source is missing (loud FAIL, no substitute).
    public static int Hang(Transform parent,string name,string id,string source,Vector3 bottomLeft,Vector3 right,Vector3 facing,
        float width,float height,out Transform group)
    {
        group=null;
        if(Mathf.Abs(width-3f*height)>.02f)throw new InvalidOperationException("A1 mural must stay 3:1, got "+width+"x"+height);
        Directory.CreateDirectory(Output);
        var whole=Load(source);
        if(!whole)
        {
            BraxBuildQuality.Warn("A1_MURAL_PRINT "+id+" FAIL: owner source missing: "+source+"; NO substitute image");
            return 0;
        }
        group=new GameObject(name).transform;
        group.SetParent(parent,false);
        Quad(group,name+" | owner picture 6144x2048 whole",id+"_whole",Print(whole,name+" whole",Output+"/"+id+"_whole.mat"),
            bottomLeft,right,width,height,facing,0f,1f);
        Debug.Log("A1_MURAL_PRINT "+id+" whole=6144x2048 split=NO tangents=YES size="+width.ToString("F2")+"x"+height.ToString("F2")+
            " pxPerMetre="+(6144f/width).ToString("F0")+" backlit=true glow="+Glow+" source="+source);
        return 1;
    }

    // Plain box (frame bar, light line) in bowl metres: centre and full size.
    public static GameObject Bar(Transform parent,string name,string id,Material material,Vector3 centre,Vector3 size)
    {
        Vector3 h=size*.5f;
        var v=new Vector3[24];var t=new int[36];int n=0,q=0;
        void Face(Vector3 normal,Vector3 a,Vector3 b)
        {
            var c=centre+Vector3.Scale(normal,h);
            Vector3 p0=c-a-b,p1=c+a-b,p2=c+a+b,p3=c-a+b;
            bool flip=Vector3.Dot(Vector3.Cross(p1-p0,p2-p0),normal)<0;
            v[n]=p0;v[n+1]=p1;v[n+2]=p2;v[n+3]=p3;
            if(flip){t[q]=n;t[q+1]=n+2;t[q+2]=n+1;t[q+3]=n;t[q+4]=n+3;t[q+5]=n+2;}
            else{t[q]=n;t[q+1]=n+1;t[q+2]=n+2;t[q+3]=n;t[q+4]=n+2;t[q+5]=n+3;}
            n+=4;q+=6;
        }
        Vector3 X=new Vector3(h.x,0,0),Y=new Vector3(0,h.y,0),Z=new Vector3(0,0,h.z);
        Face(Vector3.right,Z,Y);Face(Vector3.left,Z,Y);Face(Vector3.up,X,Z);Face(Vector3.down,X,Z);
        Face(Vector3.forward,X,Y);Face(Vector3.back,X,Y);
        var mesh=new Mesh{name=name};
        mesh.vertices=v;mesh.triangles=t;mesh.uv=new Vector2[24];
        mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"/"+id+".asset");
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static Material FrameMaterial(string id,Color colour,float roughness,float metallic,Color emission)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("A1 mural frame CityStaticPBR shader missing");
        var m=new Material(shader){name="A1 mural frame "+id,enableInstancing=true};
        m.SetColor("_Color",colour);m.SetFloat("_Roughness",roughness);m.SetFloat("_Metallic",metallic);m.SetFloat("_VertexTint",0f);
        if(emission.r+emission.g+emission.b>0)m.SetColor("_EmissionColor",emission);
        m.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m,Output+"/"+id+".mat");
        return m;
    }

    // Gold frame with a warm light line above and below, around a print lying in a vertical plane.
    // centre: centre of the print; right: along the print; outward: toward the viewer.
    public static void Frame(Transform parent,string id,Vector3 centre,Vector3 right,Vector3 outward,
        float width,float height,float bar=FrameBar,float depth=.10f)
    {
        var gold=BraxChampionsGold.Apply(FrameMaterial(id+"_frame_gold",BraxChampionsGold.Colour,.19f,.98f,Color.black));
        var light=FrameMaterial(id+"_frame_warm_line",new Color(.95f,.80f,.56f),.40f,0f,new Color(1.10f,.80f,.44f));
        bool alongX=Mathf.Abs(right.x)>.5f;
        Vector3 Size(float along,float up,float d)=>alongX?new Vector3(along,up,d):new Vector3(d,up,along);
        var mid=centre+outward*(depth*.5f-.04f);
        float hw=width*.5f,hh=height*.5f;
        Bar(parent,"A1 MURAL FRAME | "+id+" | gold top",id+"_frame_top",gold,mid+Vector3.up*(hh+bar*.5f),Size(width+2f*bar,bar,depth));
        Bar(parent,"A1 MURAL FRAME | "+id+" | gold bottom",id+"_frame_bottom",gold,mid-Vector3.up*(hh+bar*.5f),Size(width+2f*bar,bar,depth));
        Bar(parent,"A1 MURAL FRAME | "+id+" | gold left",id+"_frame_left",gold,mid-right*(hw+bar*.5f),Size(bar,height,depth));
        Bar(parent,"A1 MURAL FRAME | "+id+" | gold right",id+"_frame_right",gold,mid+right*(hw+bar*.5f),Size(bar,height,depth));
        var lit=centre+outward*(depth-.04f+.012f);
        Bar(parent,"A1 MURAL FRAME | "+id+" | warm line top",id+"_line_top",light,lit+Vector3.up*(hh+bar*.5f),Size(width+bar,.035f,.024f));
        Bar(parent,"A1 MURAL FRAME | "+id+" | warm line bottom",id+"_line_bottom",light,lit-Vector3.up*(hh+bar*.5f),Size(width+bar,.035f,.024f));
    }
}

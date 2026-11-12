using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// One way to hang an owner 6144 x 2048 mural, used by all three (entrance, left, right).
// Owner 9.11 23:43: "Mural jest ucięty, ten pierwszy. Dwóch murali kolejnych nie ma, nadal nie ma ... mają być
// wszystkie 3". APK 35 logged all three as APPLIED, yet the phone showed the two hall murals black.
// What changes here, for every mural:
// * THREE 2048 x 2048 TILES instead of one 6144 x 2048 texture (which Unity padded to 8192 x 2048 for ASTC and
//   sampled 4-5 mip levels down across a 70 m wall). 6144 = 3 x 2048: split_arena_murals.py cuts the verified
//   originals without touching a pixel. Square power-of-two tiles are the most ordinary texture a phone can get.
// * A BACK-LIT PRINT: the picture is its own light (emission .86, albedo .30), so it shows the owner's colours at
//   full strength on a wall the sun never reaches, instead of albedo x a dim interior ambient.
// * If the tiles are not there, the whole original is used on one panel (never an empty wall) and the log says so.
public static class BraxChampionsArenaMuralPrint
{
    const string Output="Assets/PlayableGenerated/Mistrzowie/Arena/Mural";
    public const int TileCount=3;
    public const float Glow=.86f, Albedo=.30f, FrameBar=.16f;
    public static string TilePath(string source,int i)=>source.Substring(0,source.Length-4)+"_TILE"+i+".png";

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
        mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateUVDistributionMetrics();
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
    // Returns the number of tiles hung (3), 1 for the whole-picture fallback, 0 when the owner source is missing.
    public static int Hang(Transform parent,string name,string id,string source,Vector3 bottomLeft,Vector3 right,Vector3 facing,
        float width,float height,out Transform group)
    {
        group=null;
        if(Mathf.Abs(width-3f*height)>.02f)throw new InvalidOperationException("A1 mural must stay 3:1, got "+width+"x"+height);
        Directory.CreateDirectory(Output);
        group=new GameObject(name).transform;
        group.SetParent(parent,false);
        var tiles=new Texture2D[TileCount];bool all=true;
        for(int i=0;i<TileCount;i++){tiles[i]=Load(TilePath(source,i));all&=tiles[i];}
        if(all)
        {
            float w=width/TileCount;
            for(int i=0;i<TileCount;i++)
                Quad(group,name+" | tile "+(i+1)+"/3 | 2048x2048",id+"_tile"+i,
                    Print(tiles[i],name+" tile "+i,Output+"/"+id+"_tile"+i+".mat"),
                    bottomLeft+right*(w*i),right,w,height,facing,0f,1f);
            Debug.Log("A1_MURAL_PRINT "+id+" tiles=3x2048 size="+width.ToString("F2")+"x"+height.ToString("F2")+
                " pxPerMetre="+(6144f/width).ToString("F0")+" backlit=true glow="+Glow+" source="+source);
            return TileCount;
        }
        var whole=Load(source);
        if(!whole)
        {
            BraxBuildQuality.Warn("A1_MURAL_PRINT "+id+" FAIL: owner source missing: "+source+"; NO substitute image");
            UnityEngine.Object.DestroyImmediate(group.gameObject);group=null;
            return 0;
        }
        BraxBuildQuality.Warn("A1_MURAL_PRINT "+id+" tiles missing (split_arena_murals.py): whole 6144x2048 picture on one panel");
        Quad(group,name+" | whole owner picture",id+"_whole",Print(whole,name+" whole",Output+"/"+id+"_whole.mat"),
            bottomLeft,right,width,height,facing,0f,1f);
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

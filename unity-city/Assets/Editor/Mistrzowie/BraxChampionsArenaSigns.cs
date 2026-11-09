using System;
using System.Collections.Generic;
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
    static Material MakeMat(int index)=>MakeMat(index,index);
    static Material MakeMat(int slot,int index)
    {
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("City PBR shader missing");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+Names[index]);
        var m=new Material(shader){name="Owner's Champion Arena sign "+slot+" art "+index,enableInstancing=true};
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
        AssetDatabase.CreateAsset(m,Out+"Materials/champion_sign_"+slot+".mat");
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
            // Screen-right for an outward-facing Unity camera is UP x
            // LOOK-DIRECTION (not the opposite cross product). From SOUTH
            // the +X mesh parameter reads normally; from EAST +Z also reads
            // normally. Only NORTH and WEST need U reversal. Preserve the
            // owner's approved original sign pixels and lettering.
            bool reverseU=alongX?positive:!positive;
            float outwardFacingU=reverseU?1f-t:t;
            uvs[2*i]=new Vector2(outwardFacingU,0);
            uvs[2*i+1]=new Vector2(outwardFacingU,1);
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
    // ---- Owner 9.11: "Szyld ma być naklejony na bryle żeby wyglądał jak 3d" ----
    // The body is the artwork's own outline (BraxChampionsArenaSignSilhouettes,
    // measured from the PNG alpha) extruded in depth. The artwork itself stays
    // untouched on the front face; holes in the artwork show the black body.
    public const float FrontWidth=39f, FrontHeight=6.5f, FrontDepth=1.50f;
    public const float FrontFaceZ=BraxChampionsArenaArrival.CanopyFrontZ+.30f;
    public const float CurvedBodyDepth=.32f;

    static void Tri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 facing)
    {
        if(Vector3.Dot(Vector3.Cross(b-a,c-a),facing)<0){var x=b;b=c;c=x;}
        int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);
    }
    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 facing)
    {
        Tri(v,t,a,b,c,facing);Tri(v,t,a,c,d,facing);
    }
    static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Out+"Meshes/"+name.Replace(' ','_')+".asset");
        return mesh;
    }
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material m,bool solid)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=m;
        if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        return go;
    }
    // Ear clipping of the CCW outline (u right, v up).
    static List<int> Triangulate(List<Vector2> p)
    {
        var idx=new List<int>();for(int i=0;i<p.Count;i++)idx.Add(i);
        var tris=new List<int>();int guard=0;
        bool Inside(Vector2 a,Vector2 b,Vector2 c,Vector2 q)
        {
            float d1=(b.x-a.x)*(q.y-a.y)-(b.y-a.y)*(q.x-a.x);
            float d2=(c.x-b.x)*(q.y-b.y)-(c.y-b.y)*(q.x-b.x);
            float d3=(a.x-c.x)*(q.y-c.y)-(a.y-c.y)*(q.x-c.x);
            return d1>=0&&d2>=0&&d3>=0;
        }
        while(idx.Count>3&&guard++<20000)
        {
            bool clipped=false;
            for(int i=0;i<idx.Count;i++)
            {
                int ia=idx[(i+idx.Count-1)%idx.Count],ib=idx[i],ic=idx[(i+1)%idx.Count];
                Vector2 a=p[ia],b=p[ib],c=p[ic];
                if((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x)<=1e-9f)continue; // reflex
                bool ear=true;
                foreach(int j in idx)
                    if(j!=ia&&j!=ib&&j!=ic&&Inside(a,b,c,p[j])){ear=false;break;}
                if(!ear)continue;
                tris.Add(ia);tris.Add(ib);tris.Add(ic);idx.RemoveAt(i);clipped=true;break;
            }
            if(!clipped)break;
        }
        if(idx.Count==3){tris.Add(idx[0]);tris.Add(idx[1]);tris.Add(idx[2]);}
        return tris;
    }
    static List<Vector2> Outline(int index)
    {
        var f=BraxChampionsArenaSignSilhouettes.UV[index];var o=new List<Vector2>();
        for(int i=0;i+1<f.Length;i+=2)o.Add(new Vector2(f[i],f[i+1]));
        return o;
    }

    // Flat sign body: artwork face + outline extruded backwards by depth.
    // origin = bottom centre of the artwork face; right = viewer's right; n = toward viewer.
    static void FlatSign3D(Transform root,int index,Material art,Material body,Vector3 origin,Vector3 right,Vector3 n,
        float w,float h,float depth,string label,bool solid)
    {
        float k=BraxClaudeOsiedle.K;
        var outline=Outline(index);
        Vector3 P(Vector2 uv,float back)=>(origin+right*((uv.x-.5f)*w)+Vector3.up*(uv.y*h)-n*back)*k;
        var av=new List<Vector3>{P(new Vector2(0,0),0),P(new Vector2(0,1),0),P(new Vector2(1,1),0),P(new Vector2(1,0),0)};
        var at=new List<int>();
        var aq=new List<Vector3>();
        Quad(aq,at,av[0],av[1],av[2],av[3],n);
        // Quad() may reorder vertices; rebuild UVs from the actual positions.
        var auv=new List<Vector2>();
        foreach(var q in aq){var local=q/k-origin;auv.Add(new Vector2(Vector3.Dot(local,right)/w+.5f,local.y/h));}
        var artMesh=new Mesh{name=label+" artwork face"};
        artMesh.SetVertices(aq);artMesh.SetUVs(0,auv);artMesh.SetTriangles(at,0);artMesh.RecalculateNormals();artMesh.RecalculateBounds();
        AssetDatabase.CreateAsset(artMesh,Out+"Meshes/"+label.Replace(' ','_').Replace('|','_')+"_face.asset");
        var face=MeshObject(root,label+" | owner artwork on the 3D body",artMesh,art,false);
        face.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<outline.Count;i++)
        {
            var a=outline[i];var c=outline[(i+1)%outline.Count];
            var e=right*((c.x-a.x)*w)+Vector3.up*((c.y-a.y)*h);
            var outward=Vector3.Cross(n,e); // CCW outline seen by the viewer
            Quad(v,t,P(a,.015f),P(c,.015f),P(c,depth),P(a,depth),outward);
        }
        var tris=Triangulate(outline);
        for(int i=0;i+2<tris.Count;i+=3)
        {
            Tri(v,t,P(outline[tris[i]],.015f),P(outline[tris[i+1]],.015f),P(outline[tris[i+2]],.015f),n);
            Tri(v,t,P(outline[tris[i]],depth),P(outline[tris[i+1]],depth),P(outline[tris[i+2]],depth),-n);
        }
        // Body name must NOT start with "A1 facade | original premium sign "
        // (BraxChampionsBuild counts exactly 4 owner artworks by that prefix).
        MeshObject(root,"A1 SIGN BODY | "+label+" | black metal body extruded from the artwork outline",
            SaveMesh(label.Replace('|','_')+" body",v,t),body,solid);
        Debug.Log("A1_SIGN_3D "+label+" width="+w+" height="+h+" depth="+depth+" outlinePoints="+outline.Count+" capTriangles="+(tris.Count/3));
    }

    // FRONT public sign: flat box face, standing on the gold canopy (render).
    static void FrontSign3D(Transform root,Material art,Material body)
    {
        FlatSign3D(root,0,art,body,new Vector3(0,BraxChampionsArenaArrival.CanopyTop,FrontFaceZ),
            Vector3.right,Vector3.back,FrontWidth,FrontHeight,FrontDepth,"A1 facade | original premium sign 0",true);
    }

    // Owner 9.11 "te ściany na których murali nie ma - wykończ": the two long
    // hall walls get the owner's own artwork as 36 x 6 m 3D LED banners above
    // the stands (16 rows x .40 = 6.4 m), 25 cm off the wall, 0.45 m deep.
    // Hall in building space: inner faces z=+-34.955, x -60..60, bowl +.165 m.
    public const float HallBannerWidth=36f, HallBannerHeight=6f, HallBannerBottom=13.165f;
    static void HallBanners(Transform root,Material body)
    {
        FlatSign3D(root,2,MakeMat(4,2),body,new Vector3(0,HallBannerBottom,34.955f-.25f),
            Vector3.right,Vector3.back,HallBannerWidth,HallBannerHeight,.45f,"A1 HALL BANNER north wall",false);
        FlatSign3D(root,1,MakeMat(5,1),body,new Vector3(0,HallBannerBottom,-34.955f+.25f),
            Vector3.left,Vector3.forward,HallBannerWidth,HallBannerHeight,.45f,"A1 HALL BANNER entrance wall",false);
    }

    // Curved signs on the other facades: same outline, extruded from the
    // artwork surface back toward the glazing (they hang .36 m proud of it).
    static void CurvedSignBody(Transform root,int index,bool front,bool east,float length,float bottom,float height,Material body)
    {
        float k=BraxClaudeOsiedle.K;
        bool reverseU=front?east:!east;
        // Same placement rule as Curved(): the artwork hangs .36 m proud of the
        // ellipse along Z (front/rear) or X (sides); the body goes back along it.
        Vector3 Surface(float u,float vv,float inset)
        {
            float tt=reverseU?1f-u:u,p=(tt-.5f)*length,x,z,off=.36f-inset;
            if(front)
            {
                x=p;z=(east?1f:-1f)*(59f*Mathf.Sqrt(Mathf.Max(0,1f-p*p/(96f*96f)))+off);
            }
            else
            {
                z=p;x=(east?1f:-1f)*(96f*Mathf.Sqrt(Mathf.Max(0,1f-p*p/(59f*59f)))+off);
            }
            return new Vector3(x,bottom+vv*height,z)*k;
        }
        var outline=Outline(index);
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<outline.Count;i++)
        {
            var a=outline[i];var c=outline[(i+1)%outline.Count];
            float span=Mathf.Abs(c.x-a.x)*length;
            int steps=Mathf.Clamp(Mathf.CeilToInt(span/.5f),1,64);
            for(int sgm=0;sgm<steps;sgm++)
            {
                var p0=Vector2.Lerp(a,c,sgm/(float)steps);var p1=Vector2.Lerp(a,c,(sgm+1)/(float)steps);
                var f0=Surface(p0.x,p0.y,.012f);var f1=Surface(p1.x,p1.y,.012f);
                var b0=Surface(p0.x,p0.y,CurvedBodyDepth);var b1=Surface(p1.x,p1.y,CurvedBodyDepth);
                var normal=(f0-b0).normalized;            // toward the viewer
                var outward=Vector3.Cross(normal,(f1-f0).normalized);
                Quad(v,t,f0,f1,b1,b0,outward);
            }
        }
        MeshObject(root,"A1 SIGN "+index+" | black metal body extruded from the artwork outline",
            SaveMesh("A1 curved sign body "+index,v,t),body,false);
    }

    public static bool Generate(Transform arenaRoot,System.Collections.Generic.Dictionary<string,Material> exterior)
    {
        if(!HasSources())
        {
            Debug.LogWarning("CHAMPION_ARENA_SIGNS_MISSING: supplied artwork not imported into "+Source);
            return false;
        }
        var t=new GameObject("Owner's four CHAMPION ARENA signs | 3D bodies").transform;
        t.SetParent(arenaRoot,false);
        var body=exterior["arena_black"];
        // Front: render sign box on the gold canopy (artwork 6:1 kept exactly).
        FrontSign3D(t,MakeMat(0),body);
        Curved(t,1,MakeMat(1),true,true,36f,11.42f,6f);
        CurvedSignBody(t,1,true,true,36f,11.42f,6f,body);
        Curved(t,2,MakeMat(2),false,true,34f,11.7f,5.667f);
        CurvedSignBody(t,2,false,true,34f,11.7f,5.667f,body);
        Curved(t,3,MakeMat(3),false,false,34f,11.7f,5.667f);
        CurvedSignBody(t,3,false,false,34f,11.7f,5.667f,body);
        HallBanners(t,body);
        return true;
    }
}

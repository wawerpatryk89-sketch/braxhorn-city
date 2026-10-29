using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxCityEnvironment
{
    // The integration layer removes these original renderers. Existing ground,
    // bench, bin and hydrant collision proxies are retained, not duplicated.
    public static readonly int[] ReplacedOriginalMeshes = {
        116,117,118,119,120,121,122,123,124,125,126,127,129,
        151,152,153,154,155,156,166,167,168,169,220,234,250
    };
    // Only CityMesh_231 is clipped here; its warehouse shutter portions outside
    // this box are retained. CityMesh_234 contains only the replaced containers.
    public static readonly Bounds ContainerReplacementRegion = new Bounds(new Vector3(75,2.6f,-13),new Vector3(18.1f,5.3f,12.2f));
    // Only CityMesh_226 is clipped in these small pallet boxes. Its pier timber
    // and courtyard sandbox geometry outside them must remain untouched.
    public static readonly Bounds[] PalletReplacementRegions = {
        new Bounds(new Vector3(78.6f,.22f,81.2f),new Vector3(1.22f,.16f,1.02f)),
        new Bounds(new Vector3(78.6f,.22f,79.6f),new Vector3(1.22f,.16f,1.02f)),
        new Bounds(new Vector3(76.9f,.22f,80.4f),new Vector3(1.22f,.16f,1.02f))
    };
    private const string Output="Assets/PlayableGenerated/CityRebuildEnvironment";
    private static int meshIndex;
    private static Material pavement,asphalt,timber,iron,steel,paint,black,rubber,ochre,white,glass,red,fontMaterial;

    public static void Apply(Transform parent)
    {
        if(!parent)throw new ArgumentNullException("parent");
        if(Vector3.Distance(parent.lossyScale,Vector3.one)>.001f || Quaternion.Angle(parent.rotation,Quaternion.identity)>.01f)
            throw new InvalidOperationException("Environment parent must preserve metre scale and Unity axes");
        var previous=parent.Find("City finished outdoor details");
        if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        if(AssetDatabase.IsValidFolder(Output))AssetDatabase.DeleteAsset(Output);
        Directory.CreateDirectory(Output);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);meshIndex=0;
        var root=new GameObject("City finished outdoor details").transform;root.SetParent(parent,false);root.position=Vector3.zero;
        Materials();
        Pavements(root);
        StreetDetails(root);
        QuayDetails(root);
        YardDetails(root);
        PierDetails(root);
        HarbourDetails(root);
        AssetDatabase.SaveAssets();
        Debug.Log("BRAX_CITY_ENVIRONMENT_BUILT chunksAndObjects="+meshIndex+"; original palms and pier geometry retained; visual and mobile acceptance pending");
    }

    private static Texture2D Map(string slug,string role)
    {
        string root="Assets/Source/CityRebuild/textures/"+slug;
        string[] aliases=role=="color"?new[]{"diffuse.jpg","diffuse.png"}:role=="normal"?new[]{"normal.png","normal.jpg"}:new[]{"arm.jpg","arm.png"};
        foreach(string alias in aliases)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/"+alias);
            if(texture)return texture;
        }
        if(!Directory.Exists(root))throw new InvalidOperationException("Verified PBR texture set is missing: "+slug);
        var files=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta",StringComparison.OrdinalIgnoreCase));
        files=files.Where(p=>role=="color"?Path.GetFileName(p).Contains("_diff"):role=="normal"?Path.GetFileName(p).Contains("_nor_gl"):Path.GetFileName(p).Contains("_arm"));
        string path=files.FirstOrDefault(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase));
        var result=path==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\','/'));
        if(!result)throw new InvalidOperationException("Missing verified "+role+" texture for "+slug);
        return result;
    }

    private static Material Pbr(string name,string slug,float repeat)
    {
        return BraxCityMaterials.Textured(name,Map(slug,"color"),Map(slug,"normal"),Map(slug,"arm"),repeat);
    }

    private static void Materials()
    {
        // Poly Haven CC0: pavement tile 1.8 m; wooden planks tile 2 m.
        pavement=Pbr("Environment concrete paving","concrete_pavement_02",1f/1.8f);
        timber=Pbr("Environment dry timber","wooden_planks",.5f);
        asphalt=Pbr("Environment courtyard asphalt","clean_asphalt",1);
        iron=BraxCityMaterials.Surface("Environment graphite iron",new Color(.09f,.105f,.11f),.76f,.35f);
        steel=BraxCityMaterials.Surface("Environment galvanized steel",new Color(.38f,.41f,.43f),.54f,.7f);
        paint=BraxCityMaterials.Surface("Environment evergreen enamel",new Color(.10f,.18f,.155f),.74f,.12f);
        black=BraxCityMaterials.Surface("Environment dark recess",new Color(.035f,.04f,.043f),.96f,0);
        rubber=BraxCityMaterials.Surface("Environment weathered rubber",new Color(.055f,.06f,.057f),.97f,0);
        ochre=BraxCityMaterials.Surface("Environment safety ochre",new Color(.65f,.43f,.10f),.87f,0);
        white=BraxCityMaterials.Surface("Environment off white paint",new Color(.78f,.79f,.74f),.85f,0);
        red=BraxCityMaterials.Surface("Environment hydrant enamel",new Color(.43f,.07f,.045f),.63f,.16f);
        var shader=Shader.Find("Standard");if(!shader)throw new InvalidOperationException("Built-in Standard shader is missing");
        glass=new Material(shader){name="Bus laminated glass"};glass.color=new Color(.57f,.69f,.74f,.18f);
        glass.SetFloat("_Mode",3);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Glossiness",.35f);
        glass.EnableKeyword("_ALPHABLEND_ON");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;AssetDatabase.CreateAsset(glass,Output+"/BusGlass.mat");
        var fontShader=Shader.Find("Braxhorn/CitySignText");if(!fontShader)throw new InvalidOperationException("City world text shader is missing");
        fontMaterial=new Material(fontShader){name="Environment depth tested lettering"};AssetDatabase.CreateAsset(fontMaterial,Output+"/Lettering.mat");
    }

    private static GameObject Emit(string name,Transform parent,BraxCitySystemMesh data,Material material,bool solid=false)
    {
        var mesh=data.Build(name);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
        // Metre-scaled planar UVs remain continuous across tessellation cells.
        for(int i=0;i<vertices.Length;i++)
        {
            var p=vertices[i];var n=normals[i];
            if(Mathf.Abs(n.y)>=Mathf.Abs(n.x)&&Mathf.Abs(n.y)>=Mathf.Abs(n.z))uv[i]=new Vector2(p.x,p.z);
            else if(Mathf.Abs(n.x)>Mathf.Abs(n.z))uv[i]=new Vector2(p.z,p.y);
            else uv[i]=new Vector2(p.x,p.y);
        }
        mesh.uv=uv;mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,Output+"/mesh_"+(meshIndex++).ToString("D3")+".asset");
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(solid)obj.AddComponent<MeshCollider>().sharedMesh=mesh;
        var flags=StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic;
        if(material.renderQueue<3000)flags|=StaticEditorFlags.OccluderStatic;
        GameObjectUtility.SetStaticEditorFlags(obj,flags);
        return obj;
    }

    private static Transform Group(string name,Transform parent,Vector3 position,float yaw=0)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);return t;
    }

    private static void Quad(BraxCitySystemMesh mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        int nx=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b))),ny=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,d)));
        for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)
        {
            float x0=(float)x/nx,x1=(float)(x+1)/nx,y0=(float)y/ny,y1=(float)(y+1)/ny;
            mesh.Quad(Vector3.Lerp(Vector3.Lerp(a,b,x0),Vector3.Lerp(d,c,x0),y0),Vector3.Lerp(Vector3.Lerp(a,b,x1),Vector3.Lerp(d,c,x1),y0),
                Vector3.Lerp(Vector3.Lerp(a,b,x1),Vector3.Lerp(d,c,x1),y1),Vector3.Lerp(Vector3.Lerp(a,b,x0),Vector3.Lerp(d,c,x0),y1));
        }
    }

    private static void Box(BraxCitySystemMesh m,Vector3 p,Vector3 size)
    {
        Vector3 a=p-size*.5f,b=p+size*.5f;
        Quad(m,new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z));
        Quad(m,new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z));
        Quad(m,new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z));
        Quad(m,new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z));
        Quad(m,new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),new Vector3(a.x,b.y,a.z));
        Quad(m,new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),new Vector3(a.x,a.y,b.z));
    }

    private static IEnumerable<Rect> Without(Rect area,Rect hole)
    {
        float x0=Mathf.Max(area.xMin,hole.xMin),x1=Mathf.Min(area.xMax,hole.xMax),z0=Mathf.Max(area.yMin,hole.yMin),z1=Mathf.Min(area.yMax,hole.yMax);
        if(x1<=x0||z1<=z0){yield return area;yield break;}
        if(x0>area.xMin)yield return new Rect(area.xMin,area.yMin,x0-area.xMin,area.height);
        if(x1<area.xMax)yield return new Rect(x1,area.yMin,area.xMax-x1,area.height);
        if(z0>area.yMin)yield return new Rect(x0,area.yMin,x1-x0,z0-area.yMin);
        if(z1<area.yMax)yield return new Rect(x0,z1,x1-x0,area.yMax-z1);
    }

    private static void Pave(Transform root,string name,Rect area,float y,Material material,bool metroHole)
    {
        for(float x=area.xMin;x<area.xMax-.001f;x+=8)for(float z=area.yMin;z<area.yMax-.001f;z+=8)
        {
            var chunk=new Rect(x,z,Mathf.Min(8,area.xMax-x),Mathf.Min(8,area.yMax-z));
            var patches=metroHole?Without(chunk,BraxCitySystemsBuild.MetroExcavation):new[]{chunk};
            var data=new BraxCitySystemMesh();
            foreach(var p in patches)Quad(data,new Vector3(p.xMin,y,p.yMin),new Vector3(p.xMin,y,p.yMax),new Vector3(p.xMax,y,p.yMax),new Vector3(p.xMax,y,p.yMin));
            if(data.VertexCount>0)Emit(name+" "+x+" "+z,root,data,material);
        }
    }

    private static void Pavements(Transform root)
    {
        Pave(root,"Promenade paving",new Rect(66,-6,18,66),.15f,pavement,true);
        Pave(root,"South promenade paving",new Rect(64,60,20,52),.15f,pavement,false);
        Pave(root,"Courtyard front asphalt",new Rect(-20,48,52,14),.15f,asphalt,false);
        Pave(root,"Courtyard rear asphalt",new Rect(-20,62,72,38),.15f,asphalt,false);
        Pave(root,"Courtyard garages asphalt",new Rect(52,60,12,40),.15f,asphalt,false);
    }

    private static void StreetDetails(Transform root)
    {
        foreach(float x in new[]{34f,2.5f,-18f,-42f})Bin(root,new Vector3(x,.15f,21.2f),false);
        foreach(float x in new[]{20f,-26f})Hydrant(root,new Vector3(x,.15f,23.45f));
        foreach(float x in new[]{-88f,-40f,-2f,21f,43f,61f})
        {Drain(root,new Vector3(x,.011f,24.3f));Drain(root,new Vector3(x+3,.011f,32.7f));}
        foreach(float x in new[]{-80f,-10f,35f})Manhole(root,new Vector3(x,.014f,28.5f));
        BusShelter(root);
    }

    private static void Bench(Transform root,Vector3 position,float yaw,bool collision)
    {
        var t=Group("Timber and cast steel bench",root,position,yaw);var frame=new BraxCitySystemMesh();var boards=new BraxCitySystemMesh();var bolts=new BraxCitySystemMesh();
        foreach(float x in new[]{-.68f,.68f})
        {
            frame.Tube(new Vector3(x,.03f,-.23f),new Vector3(x,.43f,-.18f),.031f,12);
            frame.Tube(new Vector3(x,.03f,.25f),new Vector3(x,.42f,.18f),.031f,12);
            frame.Tube(new Vector3(x,.42f,.23f),new Vector3(x,.43f,-.27f),.034f,12);
            frame.Tube(new Vector3(x,.42f,-.23f),new Vector3(x,1.02f,-.39f),.028f,12);
            Box(frame,new Vector3(x,.018f,-.22f),new Vector3(.18f,.036f,.12f));Box(frame,new Vector3(x,.018f,.23f),new Vector3(.18f,.036f,.12f));
            frame.Tube(new Vector3(x,.51f,.19f),new Vector3(x,.68f,.12f),.023f,12);
            frame.Tube(new Vector3(x,.68f,.12f),new Vector3(x,.68f,-.3f),.023f,12);
            frame.Tube(new Vector3(x,.68f,-.3f),new Vector3(x,.81f,-.33f),.023f,12);
        }
        frame.Tube(new Vector3(-.7f,.31f,-.15f),new Vector3(.7f,.31f,-.15f),.027f,12);
        for(int i=0;i<5;i++)Box(boards,new Vector3(0,.465f,-.23f+i*.115f),new Vector3(1.85f,.065f,.097f));
        for(int i=0;i<3;i++)Box(boards,new Vector3(0,.68f+i*.135f,-.3f-i*.034f),new Vector3(1.85f,.11f,.065f));
        foreach(float x in new[]{-.68f,.68f})for(int i=0;i<5;i++)bolts.Tube(new Vector3(x,.499f,-.23f+i*.115f),new Vector3(x,.507f,-.23f+i*.115f),.012f,8);
        Emit("Bench cast frame and arms",t,frame,iron);Emit("Dry timber slats",t,boards,timber);Emit("Countersunk fixings",t,bolts,steel);
        if(collision){var c=t.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.51f,-.05f);c.size=new Vector3(1.9f,1.02f,.74f);}
    }

    private static void Bin(Transform root,Vector3 at,bool collision)
    {
        var t=Group("Street litter bin",root,at);var shell=new BraxCitySystemMesh();var dark=new BraxCitySystemMesh();
        shell.Tube(new Vector3(0,.035f,0),new Vector3(0,.115f,0),.24f,24,.26f);
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.PI/12;Vector3 p=new Vector3(Mathf.Cos(a)*.245f,.13f,Mathf.Sin(a)*.245f);
            shell.Tube(p,p+Vector3.up*.65f,.012f,6);
        }
        var rim=new BraxCitySystemMesh();rim.Ring(Vector3.zero,.185f,.273f,.055f);rim.Transform(Matrix4x4.TRS(new Vector3(0,.84f,0),Quaternion.Euler(90,0,0),Vector3.one));
        Emit("Rolled steel bin rim",t,rim,paint);
        dark.Tube(new Vector3(0,.12f,0),new Vector3(0,.78f,0),.215f,20);
        shell.Tube(new Vector3(0,.97f,0),new Vector3(0,1.00f,0),.275f,24,.23f);
        foreach(float x in new[]{-.2f,.2f})shell.Tube(new Vector3(x,.76f,0),new Vector3(x,.98f,0),.021f,10);
        Emit("Bin slats lid and supports",t,shell,paint);Emit("Recessed replaceable bin liner",t,dark,black);
        if(collision){var c=t.gameObject.AddComponent<CapsuleCollider>();c.center=new Vector3(0,.5f,0);c.height=1;c.radius=.28f;}
    }

    private static void Hydrant(Transform root,Vector3 at)
    {
        var t=Group("Street hydrant with valves",root,at);var body=new BraxCitySystemMesh();var caps=new BraxCitySystemMesh();
        body.Tube(Vector3.zero,Vector3.up*.13f,.19f,20,.16f);body.Tube(Vector3.up*.13f,Vector3.up*.7f,.12f,20);
        body.Tube(Vector3.up*.7f,Vector3.up*.8f,.155f,20,.075f);
        foreach(float s in new[]{-1f,1f})
        {
            body.Tube(new Vector3(0,.5f,0),new Vector3(s*.23f,.5f,0),.085f,16);
            caps.Tube(new Vector3(s*.23f,.5f,0),new Vector3(s*.27f,.5f,0),.098f,12);
            caps.Tube(new Vector3(s*.27f,.5f,0),new Vector3(s*.29f,.5f,0),.032f,6);
        }
        caps.Tube(new Vector3(0,.8f,0),new Vector3(0,.835f,0),.028f,6);
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*.135f,.13f,Mathf.Sin(a)*.135f);caps.Tube(p,p+Vector3.up*.024f,.016f,6);}
        Emit("Hydrant enamel casting",t,body,red);Emit("Hydrant capped valves and flange bolts",t,caps,steel);
    }

    private static void Drain(Transform root,Vector3 at)
    {
        var t=Group("Kerb drainage inlet",root,at);var grate=new BraxCitySystemMesh();var well=new BraxCitySystemMesh();
        Box(well,new Vector3(0,-.012f,0),new Vector3(.73f,.02f,.35f));
        foreach(float z in new[]{-.195f,.195f})Box(grate,new Vector3(0,0,z),new Vector3(.82f,.025f,.04f));
        foreach(float x in new[]{-.39f,.39f})Box(grate,new Vector3(x,0,0),new Vector3(.04f,.025f,.39f));
        for(float x=-.33f;x<=.34f;x+=.06f)Box(grate,new Vector3(x,.004f,0),new Vector3(.022f,.025f,.35f));
        Emit("Drain cast frame and grille",t,grate,iron);Emit("Drain recessed opening",t,well,black);
    }

    private static void Manhole(Transform root,Vector3 at)
    {
        var t=Group("Service access cover",root,at);var cover=new BraxCitySystemMesh();
        cover.Tube(new Vector3(0,-.012f,0),Vector3.zero,.38f,32,.36f);
        for(float x=-.26f;x<=.27f;x+=.085f)
        {
            float half=Mathf.Sqrt(.31f*.31f-x*x);Box(cover,new Vector3(x,.006f,0),new Vector3(.012f,.008f,half*2));
        }
        foreach(float z in new[]{-.18f,.18f})Box(cover,new Vector3(0,.008f,z),new Vector3(.14f,.01f,.023f));
        Emit("Embossed round cast cover",t,cover,iron);
    }

    private static void BusShelter(Transform root)
    {
        var t=Group("Finished BRAXHORN bus shelter",root,new Vector3(-18,.15f,22.5f));var frame=new BraxCitySystemMesh();
        foreach(float x in new[]{-2.1f,2.1f})foreach(float z in new[]{-.8f,.8f})
        {
            frame.Tube(new Vector3(x,0,z),new Vector3(x,2.55f,z),.042f,12);
            Box(frame,new Vector3(x,.025f,z),new Vector3(.18f,.05f,.18f));
        }
        foreach(float z in new[]{-.8f,.8f})frame.Tube(new Vector3(-2.1f,2.55f,z),new Vector3(2.1f,2.55f,z),.055f,16);
        var roof=new BraxCitySystemMesh();Box(roof,new Vector3(0,2.59f,0),new Vector3(4.45f,.1f,1.83f));
        foreach(float z in new[]{-.9f,.9f})roof.Tube(new Vector3(-2.22f,2.61f,z),new Vector3(2.22f,2.61f,z),.055f,12);
        Emit("Shelter stainless posts and joints",t,frame,steel);Emit("Seamed shelter roof with rolled edges",t,roof,iron);
        var panes=new BraxCitySystemMesh();
        for(int i=0;i<2;i++)
        {
            float a=-2+i*2,b=a+1.96f;panes.Quad(new Vector3(a,.25f,-.78f),new Vector3(b,.25f,-.78f),new Vector3(b,2.42f,-.78f),new Vector3(a,2.42f,-.78f));
            panes.Quad(new Vector3(b,.25f,-.781f),new Vector3(a,.25f,-.781f),new Vector3(a,2.42f,-.781f),new Vector3(b,2.42f,-.781f));
        }
        Emit("Laminated shelter glazing",t,panes,glass);
        var clamps=new BraxCitySystemMesh();
        foreach(float x in new[]{-1.96f,0,1.96f})foreach(float y in new[]{.42f,2.25f})Box(clamps,new Vector3(x,y,-.77f),new Vector3(.075f,.055f,.06f));
        Emit("Glazing clamps",t,clamps,steel);
        Bench(t,new Vector3(0,0,-.22f),0,true);
        // Original CityMesh_128 carries the user's ad artwork and is retained.
    }

    private static void QuayDetails(Transform root)
    {
        foreach(float z in new[]{18f,58f,104f})Bench(root,new Vector3(67.2f,.15f,z),90,false);
        foreach(float z in new[]{31f,65f,102f})Bin(root,new Vector3(67.35f,.15f,z),true);
        foreach(float z in new[]{4f,38f,58f,94f})
        {
            var t=Group("Quay mooring ring",root,new Vector3(83.6f,.33f,z));var parts=new BraxCitySystemMesh();
            parts.Ring(Vector3.zero,.15f,.188f,.035f);parts.Transform(Matrix4x4.Rotate(Quaternion.Euler(90,0,0)));
            Box(parts,new Vector3(0,-.035f,0),new Vector3(.28f,.05f,.18f));Emit("Forged mooring eye and anchor plate",t,parts,steel);
        }
        // The ring faces the public promenade; its rear bracket meets the
        // original continuous quay rail at x=83.74 rather than floating beside it.
        Lifebelt(root,new Vector3(83.68f,1.05f,55),-90);
        Lifebelt(root,new Vector3(83.68f,1.05f,100),-90);
    }

    private static void YardDetails(Transform root)
    {
        Bench(root,new Vector3(15,.15f,89.6f),180,false);Bench(root,new Vector3(5,.15f,89.6f),180,false);
        Bin(root,new Vector3(18,.15f,90.2f),true);
        foreach(var p in new[]{new Vector3(55,.166f,75),new Vector3(30,.166f,94),new Vector3(-16,.166f,64)})Drain(root,p);
        foreach(float z in new[]{84.5f,91.5f})
        {
            var data=new BraxCitySystemMesh();
            // Door sill and drain enhance the existing imported garages;
            // their original bodies remain intact.
            Box(data,new Vector3(58.72f,.18f,z),new Vector3(.18f,.05f,2.85f));
            for(float d=-1.3f;d<=1.31f;d+=.16f)Box(data,new Vector3(58.7f,.212f,z+d),new Vector3(.16f,.008f,.018f));
            Emit("Garage threshold and drainage "+z,root,data,steel);
        }
    }

    private static void Lifebelt(Transform root,Vector3 at,float yaw)
    {
        var t=Group("Harbour life ring",root,at,yaw);var ring=new BraxCitySystemMesh();ring.Ring(Vector3.zero,.23f,.35f,.11f,0,360,40);
        Emit("Life ring body",t,ring,red);var bands=new BraxCitySystemMesh();
        for(int i=0;i<4;i++)bands.Ring(new Vector3(0,0,.112f),.227f,.353f,.008f,i*90-10,i*90+10,4);
        Emit("Life ring reflective bands",t,bands,white);var bracket=new BraxCitySystemMesh();
        Box(bracket,new Vector3(0,-.3f,-.04f),new Vector3(.14f,.12f,.16f));bracket.Tube(new Vector3(0,-.3f,-.06f),new Vector3(0,.38f,-.06f),.018f,10);
        Emit("Life ring bracket",t,bracket,steel);
    }

    private static void PierDetails(Transform root)
    {
        // Existing timber deck, piles, palms, original railing and access stay.
        foreach(float x in new[]{87f,95f,103f,110f})foreach(float z in new[]{70.38f,75.62f})
        {
            var t=Group("Pier timber fastenings",root,new Vector3(x,.321f,z));var metal=new BraxCitySystemMesh();
            Box(metal,new Vector3(0,.008f,0),new Vector3(.22f,.018f,.16f));
            foreach(float dx in new[]{-.065f,.065f})metal.Tube(new Vector3(dx,.015f,0),new Vector3(dx,.028f,0),.02f,6);
            Emit("Galvanized deck plate and bolts",t,metal,steel);
        }
        foreach(float x in new[]{91f,102f})
        {
            var t=Group("Pier mooring cleat",root,new Vector3(x,.32f,75.52f));var cleat=new BraxCitySystemMesh();
            Box(cleat,new Vector3(0,.035f,0),new Vector3(.38f,.07f,.22f));
            cleat.Tube(new Vector3(-.1f,.06f,0),new Vector3(-.1f,.28f,0),.05f,12);cleat.Tube(new Vector3(.1f,.06f,0),new Vector3(.1f,.28f,0),.05f,12);
            cleat.Tube(new Vector3(-.31f,.28f,0),new Vector3(.31f,.28f,0),.045f,14);
            Emit("Cast iron cleat",t,cleat,iron);
        }
        var ladder=new BraxCitySystemMesh();
        foreach(float x in new[]{98.7f,99.3f})
        {
            ladder.Tube(new Vector3(x,-2.65f,76.25f),new Vector3(x,1.45f,76.25f),.034f,12);
            ladder.Tube(new Vector3(x,1.45f,76.25f),new Vector3(x,1.45f,75.85f),.034f,12);
            ladder.Tube(new Vector3(x,1.45f,75.85f),new Vector3(x,1.30f,75.85f),.034f,12);
            // Bolt the water-side rails to the retained timber fascia.
            ladder.Tube(new Vector3(x,.08f,76.25f),new Vector3(x,.08f,76.14f),.026f,12);
            Box(ladder,new Vector3(x,.08f,76.15f),new Vector3(.14f,.2f,.02f));
            foreach(float y in new[]{.015f,.145f})ladder.Tube(new Vector3(x,y,76.16f),new Vector3(x,y,76.18f),.018f,6);
        }
        for(float y=-2.45f;y<.3f;y+=.3f)ladder.Tube(new Vector3(98.7f,y,76.25f),new Vector3(99.3f,y,76.25f),.024f,10);
        Emit("Water access ladder outside pier guard",root,ladder,steel);
        Lifebelt(root,new Vector3(95.5f,1.02f,75.79f),180);
    }

    private static void HarbourDetails(Transform root)
    {
        var colors=new[]{new Color(.11f,.20f,.25f),new Color(.31f,.24f,.18f),new Color(.29f,.32f,.31f)};
        var coatings=colors.Select((c,i)=>BraxCityMaterials.Surface("Harbour container coating "+i,c,.83f,.18f)).ToArray();
        for(int column=0;column<6;column++)for(int level=0;level<2;level++)
            Container(root,new Vector3(82.275f-column*2.6f,level*2.6f,-13),coatings[(column+level)%coatings.Length],column*2+level);
        foreach(var box in PalletReplacementRegions)Pallet(root,new Vector3(box.center.x,.15f,box.center.z));
        SackTruck(root,new Vector3(79.55f,.15f,85.35f),-90);
    }

    private static void Container(Transform root,Vector3 at,Material coating,int number)
    {
        var t=Group("Detailed freight container "+number,root,at);var body=new BraxCitySystemMesh();var hardware=new BraxCitySystemMesh();
        const float half=1.225f,depth=6,height=2.55f;
        foreach(float s in new[]{-1f,1f})
        {
            for(float z=-5.88f;z<5.88f-.001f;z+=.28f)
            {
                float end=Mathf.Min(z+.28f,5.88f);
                var zs=new[]{z,Mathf.Min(z+.06f,end),Mathf.Min(z+.2f,end),end};var offsets=new[]{0f,.035f,.035f,0f};
                for(int i=0;i<3;i++)
                {
                    Vector3 a=new Vector3(s*(half-.045f+offsets[i]),.15f,zs[i]),b=new Vector3(s*(half-.045f+offsets[i+1]),.15f,zs[i+1]);
                    if(s>0)Quad(body,b,a,a+Vector3.up*2.25f,b+Vector3.up*2.25f);else Quad(body,a,b,b+Vector3.up*2.25f,a+Vector3.up*2.25f);
                }
            }
            foreach(float z in new[]{-depth+.04f,depth-.04f})Box(hardware,new Vector3(s*(half-.045f),height*.5f,z),new Vector3(.09f,height,.09f));
            foreach(float y in new[]{.06f,height-.06f})Box(hardware,new Vector3(s*(half-.045f),y,0),new Vector3(.09f,.12f,12));
        }
        Box(body,new Vector3(0,height-.035f,0),new Vector3(2.35f,.07f,11.86f));
        Box(body,new Vector3(0,height*.5f,-5.98f),new Vector3(2.35f,2.4f,.06f));
        foreach(float side in new[]{-1f,1f})
        {
            float center=side*.592f;Box(body,new Vector3(center,1.275f,5.985f),new Vector3(1.145f,2.32f,.05f));
            for(float x=center-.47f;x<center+.48f;x+=.235f)Box(body,new Vector3(x,1.275f,6.028f),new Vector3(.07f,2.12f,.05f));
            foreach(float dx in new[]{-.3f,.3f})
            {
                float x=center+dx;hardware.Tube(new Vector3(x,.2f,6.08f),new Vector3(x,2.34f,6.08f),.023f,10);
                for(float y=.38f;y<2.3f;y+=.57f)Box(hardware,new Vector3(x,y,6.075f),new Vector3(.1f,.07f,.1f));
                hardware.Tube(new Vector3(x,1.06f,6.08f),new Vector3(x+side*.19f,1.06f,6.11f),.016f,10);
            }
            for(float y=.35f;y<2.4f;y+=.67f)hardware.Tube(new Vector3(side*1.14f,y-.07f,6.035f),new Vector3(side*1.14f,y+.07f,6.035f),.035f,12);
        }
        foreach(float x in new[]{-1.14f,1.14f})foreach(float y in new[]{.1f,2.45f})foreach(float z in new[]{-5.98f,6.02f})
        {
            Box(hardware,new Vector3(x,y-.055f,z),new Vector3(.17f,.045f,.13f));Box(hardware,new Vector3(x,y+.055f,z),new Vector3(.17f,.045f,.13f));
            foreach(float dx in new[]{-.065f,.065f})Box(hardware,new Vector3(x+dx,y,z),new Vector3(.04f,.08f,.13f));
        }
        Emit("Corrugated steel container panels",t,body,coating);Emit("Corner castings frame locking rods and hinges",t,hardware,steel);
        Lettering(t,"Container serial","BX "+(4100+number),new Vector3(-.48f,1.8f,6.073f),180,.11f);
    }

    private static void Pallet(Transform root,Vector3 at)
    {
        var t=Group("Slatted delivery pallet",root,at);var boards=new BraxCitySystemMesh();var nails=new BraxCitySystemMesh();
        foreach(float x in new[]{-.48f,0,.48f})Box(boards,new Vector3(x,.054f,0),new Vector3(.12f,.08f,1));
        for(int i=0;i<7;i++)Box(boards,new Vector3(0,.119f,-.43f+i*.143f),new Vector3(1.2f,.042f,.115f));
        foreach(float z in new[]{-.38f,.38f})Box(boards,new Vector3(0,.013f,z),new Vector3(1.2f,.026f,.12f));
        foreach(float x in new[]{-.48f,0,.48f})for(int i=0;i<7;i++)nails.Tube(new Vector3(x,.14f,-.43f+i*.143f),new Vector3(x,.143f,-.43f+i*.143f),.006f,6);
        Emit("Pallet stringers top boards and lower bearers",t,boards,timber);Emit("Pallet nail heads",t,nails,iron);
    }

    private static void SackTruck(Transform root,Vector3 at,float yaw)
    {
        var t=Group("Port sack truck",root,at,yaw);var frame=new BraxCitySystemMesh();var wheels=new BraxCitySystemMesh();
        foreach(float x in new[]{-.22f,.22f})
        {
            frame.Tube(new Vector3(x,.16f,0),new Vector3(x,1.34f,-.16f),.024f,12);
            frame.Tube(new Vector3(x,1.34f,-.16f),new Vector3(x,1.34f,-.32f),.026f,12);
            wheels.Tube(new Vector3(x-.05f,.17f,-.08f),new Vector3(x+.05f,.17f,-.08f),.16f,20);
        }
        foreach(float y in new[]{.4f,.7f,1f})frame.Tube(new Vector3(-.22f,y,-y*.1f),new Vector3(.22f,y,-y*.1f),.02f,10);
        Box(frame,new Vector3(0,.08f,.15f),new Vector3(.52f,.035f,.4f));
        Emit("Sack truck tubular frame and footplate",t,frame,ochre);Emit("Rubber transport wheels",t,wheels,rubber);
        var c=t.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.66f,-.04f);c.size=new Vector3(.62f,1.32f,.56f);
    }

    private static void Lettering(Transform parent,string name,string text,Vector3 at,float yaw,float height)
    {
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var tm=obj.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=height*10/64;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=new Color(.82f,.81f,.75f);
        tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");obj.GetComponent<MeshRenderer>().sharedMaterial=fontMaterial;obj.AddComponent<BraxCitySignText>();
    }
}

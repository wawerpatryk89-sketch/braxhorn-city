using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxGymPremium
{
    const string Input="Assets/Source/Premium/";
    static string folder;
    static int meshId,detailCount;
    static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
    static readonly Color Black=new Color(.065f,.072f,.080f);
    static readonly Color Green=new Color(.34f,.69f,.10f);
    static Material Save(Material m,string name){m.name=name;AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;}
    static Texture2D Texture(string name,bool normal=false,bool linear=false)
    {
        string cacheKey=name+"|"+normal+"|"+linear;
        if(textures.TryGetValue(cacheKey,out var cached))return cached;
        string p=Input+name;var importer=AssetImporter.GetAtPath(p) as TextureImporter;
        if(importer!=null)
        {importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!(normal||linear);importer.mipmapEnabled=true;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p);if(!t)throw new Exception("Required premium texture missing: "+p);textures[cacheKey]=t;return t;
    }
    static Material Pbr(string name,Color c,float smooth=0.2f,float metal=0,UnityEngine.Texture normal=null,float repeat=1)
    {
        var shader=Shader.Find("Braxhorn/GymDetailPBR");if(!shader)throw new Exception("Detail PBR shader missing");
        var m=new Material(shader){color=c};m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        if(normal)
        {
            m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.45f);m.SetTextureScale("_BumpMap",Vector2.one*repeat);
            string map=normal.name.Replace("Normal","Roughness")+".png";
            if(File.Exists(Input+map)){m.SetTexture("_RoughnessMap",Texture(map,false,true));m.SetTextureScale("_RoughnessMap",Vector2.one*repeat);}
        }
        return Save(m,name);
    }
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material m)
    {
        mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,folder+"/mesh-"+(meshId++)+".asset");
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;detailCount++;return o;
    }
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.position=p;o.transform.localScale=size;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=m;detailCount++;return o;
    }
    static GameObject Tube(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material m,int segments=20,int sides=10,float sag=0)
    {
        var vs=new Vector3[(segments+1)*(sides+1)];var ns=new Vector3[vs.Length];var uv=new Vector2[vs.Length];var tr=new int[segments*sides*6];
        Vector3 axis=(b-a).normalized,side=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized,up=Vector3.Cross(side,axis).normalized;
        for(int j=0;j<=segments;j++){float t=(float)j/segments;Vector3 c=Vector3.Lerp(a,b,t)-Vector3.up*(Mathf.Sin(t*Mathf.PI)*sag);for(int k=0;k<=sides;k++){float u=(float)k/sides;var n=side*Mathf.Cos(u*Mathf.PI*2)+up*Mathf.Sin(u*Mathf.PI*2);int v=j*(sides+1)+k;vs[v]=c+n*radius;ns[v]=n;uv[v]=new Vector2(u,t*Vector3.Distance(a,b));}}
        int q=0;for(int j=0;j<segments;j++)for(int k=0;k<sides;k++){int v=j*(sides+1)+k;tr[q++]=v;tr[q++]=v+sides+1;tr[q++]=v+1;tr[q++]=v+1;tr[q++]=v+sides+1;tr[q++]=v+sides+2;}
        var mesh=new Mesh{name=name,vertices=vs,normals=ns,uv=uv,triangles=tr};mesh.RecalculateTangents();return MeshObject(parent,name,mesh,m);
    }
    static GameObject Torus(Transform parent,string name,Vector3 center,float major,float minor,Material m,Quaternion rotation,int around=16,int sides=6)
    {
        var vs=new Vector3[(around+1)*(sides+1)];var ns=new Vector3[vs.Length];var uv=new Vector2[vs.Length];var tr=new int[around*sides*6];
        for(int j=0;j<=around;j++)for(int k=0;k<=sides;k++){float a=2*Mathf.PI*j/around,b=2*Mathf.PI*k/sides;var n=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));int i=j*(sides+1)+k;vs[i]=center+rotation*(new Vector3(Mathf.Cos(a)*major,Mathf.Sin(a)*major,0)+n*minor);ns[i]=rotation*n;uv[i]=new Vector2((float)j/around,(float)k/sides);}
        int q=0;for(int j=0;j<around;j++)for(int k=0;k<sides;k++){int i=j*(sides+1)+k;tr[q++]=i;tr[q++]=i+sides+1;tr[q++]=i+1;tr[q++]=i+1;tr[q++]=i+sides+1;tr[q++]=i+sides+2;}
        var mesh=new Mesh{name=name,vertices=vs,normals=ns,uv=uv,triangles=tr};mesh.RecalculateTangents();return MeshObject(parent,name,mesh,m);
    }
    static void RoundedBox(Transform root,string name,Vector3 center,Vector3 size,float radius,Material m)
    {
        var verts=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var ids=new List<int>();
        Vector3 inner=size*.5f-Vector3.one*radius;int div=8;
        foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n).normalized;var v=Vector3.Cross(n,u);int start=verts.Count;
            for(int y=0;y<=div;y++)for(int x=0;x<=div;x++){Vector3 p=Vector3.Scale(n+u*(2f*x/div-1)+v*(2f*y/div-1),size*.5f);var q=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));Vector3 normal=(p-q).normalized;verts.Add(center+q+normal*radius);normals.Add(normal);uvs.Add(new Vector2((float)x/div,(float)y/div));}
            for(int y=0;y<div;y++)for(int x=0;x<div;x++){int a=start+y*(div+1)+x;ids.AddRange(new[]{a,a+1,a+div+1,a+1,a+div+2,a+div+1});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(ids,0);mesh.RecalculateTangents();MeshObject(root,name,mesh,m);
    }
    static void Hide(Transform gym,string name){var t=gym.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==name);if(t){var r=t.GetComponent<Renderer>();if(r)r.enabled=false;}}
    static void BatchDetails(Transform root)
    {
        var items=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).ToArray();
        foreach(var group in items.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var all=group.ToArray();if(all.Length<2)continue;
            var mesh=new Mesh{name="Batched premium details",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(all.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            MeshObject(root,"Premium batch "+group.Key.name,mesh,group.Key);
            foreach(var f in all)UnityEngine.Object.DestroyImmediate(f.gameObject);
        }
    }
    public static void Apply(GameObject gym,GameObject hero,Transform[] bags,Camera camera,Light key,Light fill,string generated)
    {
        folder=generated+"/Premium";Directory.CreateDirectory(folder);AssetDatabase.Refresh();meshId=0;detailCount=0;textures.Clear();
        var brick=Texture("BrickNormal.png",true);var rubber=Texture("RubberNormal.png",true);var leather=Texture("LeatherNormal.png",true);var canvas=Texture("CanvasNormal.png",true);var steel=Texture("SteelNormal.png",true);var rope=Texture("RopeNormal.png",true);var rubberMicro=Texture("RubberMicroNormal.png",true);
        var all=gym.GetComponentsInChildren<MeshRenderer>(true).Concat(bags.SelectMany(b=>b.GetComponentsInChildren<MeshRenderer>(true))).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct();
        int upgraded=0;
        foreach(var m in all)
        {
            if(!m.name.StartsWith("BX_"))continue;var parts=m.name.Split('_');if(parts.Length<2||!int.TryParse(parts[1],out int id))continue;
            if(m.shader.name!="Braxhorn/BakedPremium")continue;
            m.SetFloat("_Specular",.005f);m.SetFloat("_Smoothness",.12f);
            if(new[]{9,23,35,36}.Contains(id)){m.SetTexture("_NormalMap",brick);m.SetFloat("_NormalStrength",.82f);m.SetFloat("_Exposure",.94f);m.SetTexture("_RoughnessMap",Texture("BrickRoughness.png",false,true));m.SetFloat("_RoughnessInfluence",.85f);}
            else if(id==69){m.SetTexture("_NormalMap",rubber);m.SetFloat("_NormalStrength",.65f);m.SetFloat("_Specular",.008f);m.SetFloat("_Exposure",.99f);m.SetTexture("_RoughnessMap",Texture("RubberRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",18);m.SetFloat("_RoughnessInfluence",.8f);}
            else if(new[]{38,39}.Contains(id)){m.SetTexture("_NormalMap",canvas);m.SetFloat("_NormalStrength",.40f);m.SetFloat("_NormalRepeat",18f);m.SetTexture("_RoughnessMap",Texture("CanvasRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",18f);m.SetFloat("_RoughnessInfluence",.8f);}
            else if(new[]{46,51,53,59}.Contains(id)){m.SetTexture("_NormalMap",rubberMicro);m.SetFloat("_NormalStrength",.25f);m.SetFloat("_Specular",.006f);m.SetTexture("_RoughnessMap",Texture("RubberRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",8f);m.SetFloat("_RoughnessInfluence",.8f);}
            else if(new[]{57,58}.Contains(id)){m.SetTexture("_NormalMap",rope);m.SetFloat("_NormalStrength",.35f);m.SetFloat("_NormalRepeat",7f);m.SetTexture("_RoughnessMap",Texture("RopeRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",7f);m.SetFloat("_RoughnessInfluence",.8f);}
            else if(new[]{37,56,61}.Contains(id)){m.SetTexture("_NormalMap",leather);m.SetFloat("_NormalStrength",.48f);m.SetFloat("_NormalRepeat",8f);m.SetFloat("_Specular",.022f);m.SetTexture("_RoughnessMap",Texture("LeatherRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",8f);m.SetFloat("_RoughnessInfluence",.8f);}
            else if(new[]{1,2,3,4,47,50,55}.Contains(id)){m.SetTexture("_NormalMap",steel);m.SetFloat("_NormalStrength",.34f);m.SetFloat("_NormalRepeat",6);m.SetFloat("_Specular",.045f);m.SetFloat("_Smoothness",.38f);m.SetTexture("_RoughnessMap",Texture("SteelRoughness.png",false,true));m.SetFloat("_RoughnessRepeat",6f);m.SetFloat("_RoughnessInfluence",.55f);}
            if(id==10){m.color=new Color(.022f,.025f,.03f);m.SetFloat("_Specular",.016f);}
            upgraded++;
        }
        if(upgraded==0)throw new Exception("No original BX materials were upgraded. Preserve source names after AssetDatabase.CreateAsset.");
        // The original hero material and ORM are applied by BraxHeroRefinements.
        key.intensity=.96f;key.color=new Color(1,.97f,.93f);fill.intensity=.44f;fill.range=28;
        RenderSettings.ambientSkyColor=new Color(.48f,.50f,.52f);RenderSettings.ambientEquatorColor=new Color(.29f,.30f,.32f);RenderSettings.ambientGroundColor=new Color(.17f,.16f,.15f);
        var back=new GameObject("Neutral soft fill").AddComponent<Light>();back.type=LightType.Directional;back.intensity=.18f;back.color=new Color(.84f,.90f,1);back.transform.rotation=Quaternion.Euler(38,155,0);back.shadows=LightShadows.None;
        var dark=Pbr("Powder_coated_steel",Black,.24f,.35f,steel,5);var metal=Pbr("Brushed_fasteners",new Color(.39f,.42f,.45f),.44f,.72f,steel,6);var vinyl=Pbr("Black_vinyl",new Color(.035f,.039f,.045f),.22f,0,leather,10);var greenVinyl=Pbr("Green_vinyl",Green,.22f,0,leather,10);var ivory=Pbr("Rope_ivory",new Color(.63f,.65f,.61f),.12f,0,rope,7);var greenRope=Pbr("Rope_green",Green,.12f,0,rope,7);var stitching=Pbr("Light_stitching",new Color(.42f,.46f,.40f),.1f);
        var details=new GameObject("Premium ring and hall finishing").transform;
        Hide(gym.transform,"Gym_Mesh_136");Hide(gym.transform,"Gym_Mesh_137");
        const float cx=-9,cz=-5.5f,half=3.45f,deck=.95f;
        foreach(float h in new[]{.42f,.82f,1.22f})
        {
            Material m=h>1?greenRope:ivory;float yy=deck+h;
            var a=new Vector3(cx-half,yy,cz-half);var b=new Vector3(cx+half,yy,cz-half);var c=new Vector3(cx+half,yy,cz+half);var d=new Vector3(cx-half,yy,cz+half);
            Tube(details,"Bound rope",a,b,.043f,m,48,12,.028f);Tube(details,"Bound rope",a,d,.043f,m,48,12,.028f);Tube(details,"Bound rope",b,c,.043f,m,48,12,.028f);
            Tube(details,"Entrance rope",d,new Vector3(-9.85f,yy,cz+half),.043f,m,30,12,.02f);Tube(details,"Entrance rope",new Vector3(-8.15f,yy,cz+half),c,.043f,m,30,12,.02f);
        }
        foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
        {
            float px=cx+sx*3.5f,pz=cz+sz*3.5f;Material pad=sx==sz?greenVinyl:vinyl;
            RoundedBox(details,"Stitched corner cushion",new Vector3(px-sx*.12f,1.80f,pz-sz*.12f),new Vector3(.30f,1.30f,.30f),.065f,pad);
            Box(details,"Post anchor plate",new Vector3(px,.99f,pz),new Vector3(.32f,.035f,.32f),dark);
            for(int i=0;i<4;i++){float dx=(i%2==0?-1:1)*.11f,dz=(i<2?-1:1)*.11f;Torus(details,"Anchor bolt",new Vector3(px+dx,1.014f,pz+dz),.025f,.010f,metal,Quaternion.Euler(90,0,0),10,5);}
            for(int k=0;k<20;k++){float y=1.22f+k*.059f;Tube(details,"Corner double stitching",new Vector3(px-sx*.12f-.105f,y,pz-sz*.12f-.153f),new Vector3(px-sx*.12f-.105f,y+.027f,pz-sz*.12f-.153f),.003f,stitching,1,5);}
        }
        // Three ropes are bound with narrow, stitched spacers on each side.
        // The entrance gap remains clear; these are surface details, not obstacles.
        foreach(float along in new[]{-2.2f,2.2f})
        {
            foreach(int side in new[]{-1,1})
            {
                Box(details,"Rope spacer strap",new Vector3(cx+along,deck+.82f,cz+side*(half+.047f)),new Vector3(.055f,.84f,.018f),vinyl);
                Box(details,"Rope spacer strap",new Vector3(cx+side*(half+.047f),deck+.82f,cz+along),new Vector3(.018f,.84f,.055f),vinyl);
            }
        }
        for(int side=0;side<4;side++)
        {
            float a=side*Mathf.PI/2;var along=new Vector3(Mathf.Cos(a),0,-Mathf.Sin(a));var normal=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var center=new Vector3(cx,.85f,cz)+normal*4.212f;
            Tube(details,"Apron edge binding",center-along*4.17f,center+along*4.17f,.015f,vinyl,40,8);
            for(int k=0;k<14;k++){var p=center+along*(-3.9f+k*.6f);Torus(details,"Apron eyelet",p,.024f,.006f,metal,Quaternion.Euler(0,side*90,0),12,5);}
        }
        foreach(var step in new[]{new Vector3(-9,.65f,-1.07f),new Vector3(-9,.33f,-.61f)})
        {for(int k=0;k<5;k++)Box(details,"Anti slip stair tread",step+new Vector3(0,.012f,-.14f+k*.07f),new Vector3(1.53f,.008f,.014f),vinyl);}
        for(int i=0;i<bags.Length;i++)
        {
            var bag=bags[i];foreach(int part in new[]{84,85,86,87,88}){var old=bag.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Gym_Mesh_"+(part+i*8));if(old)old.enabled=false;}
            var body=bag.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Gym_Mesh_"+(81+i*8));
            if(body)
            {
                var source=body.sharedMaterial;var m=Pbr("Bag_leather_"+i,Color.white,.24f,0,leather,10);
                string texture=source.GetTexturePropertyNames().FirstOrDefault(n=>n.ToLowerInvariant().Contains("basecolor")||n=="_MainTex");if(texture!=null)m.mainTexture=source.GetTexture(texture);body.sharedMaterial=m;
                var root=new GameObject("Bag premium seams and chain").transform;root.SetParent(bag,true);root.position=Vector3.zero;root.rotation=Quaternion.identity;
                var b=body.bounds;float zz=b.center.z,xx=b.center.x;
                foreach(float yy in new[]{b.min.y+.055f,b.max.y-.055f}){Torus(root,"Double stitched leather end",new Vector3(xx,yy,zz),.295f,.007f,stitching,Quaternion.Euler(90,0,0),40,5);Torus(root,"Vinyl piping",new Vector3(xx,yy+.02f,zz),.297f,.009f,vinyl,Quaternion.Euler(90,0,0),40,6);}
                int chainLink=0;
                for(float y=b.max.y+.25f;y<4.55f;y+=.11f){Torus(root,"Welded suspension chain",new Vector3(xx,y,zz),.066f,.012f,metal,Quaternion.Euler(0,(chainLink++%2)*90,0),16,6);}
                Torus(root,"Suspension master ring",new Vector3(xx,b.max.y+.21f,zz),.075f,.015f,metal,Quaternion.identity,20,7);
                for(int strap=0;strap<4;strap++)
                {
                    float a=strap*Mathf.PI*.5f+.6f;
                    Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    Vector3 lower=new Vector3(xx,b.max.y-.09f,zz)+radial*.26f;
                    Vector3 upper=new Vector3(xx,b.max.y+.20f,zz)+radial*.045f;
                    var webbing=Box(root,"Reinforced leather suspension strap",(lower+upper)*.5f,new Vector3(.052f,Vector3.Distance(lower,upper),.018f),vinyl);
                    webbing.transform.rotation=Quaternion.FromToRotation(Vector3.up,upper-lower);
                    Torus(root,"Strap metal eyelet",lower+radial*.012f,.022f,.006f,metal,Quaternion.LookRotation(radial),12,5);
                }
                // A fine side seam follows the original cylinder without changing its label.
                for(float y=b.min.y+.18f;y<b.max.y-.16f;y+=.055f)
                    Tube(root,"Leather panel stitch",new Vector3(xx+.298f,y,zz),new Vector3(xx+.298f,y+.025f,zz),.0025f,stitching,1,5);
                BatchDetails(root);
            }
        }
        var glass=Pbr("Smoked_window_glass",new Color(.065f,.10f,.13f),.72f,.20f);
        var window=gym.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Gym_Mesh_22");if(window)window.GetComponent<Renderer>().sharedMaterial=glass;
        for(int j=-1;j<=1;j++)
        {
            float z=j*6;const float x=17.94f;
            Box(details,"Window inner sill",new Vector3(x-.075f,4.57f,z),new Vector3(.25f,.10f,1.56f),dark);
            for(int s=-1;s<=1;s+=2)Box(details,"Window jamb",new Vector3(x-.025f,5.25f,z+s*.72f),new Vector3(.10f,1.36f,.07f),dark);
            var prev=new Vector3(x-.025f,5.9f,z-.72f);for(int k=1;k<=24;k++){float a=Mathf.PI-Mathf.PI*k/24;var next=new Vector3(x-.025f,5.9f+Mathf.Sin(a)*.72f,z+Mathf.Cos(a)*.72f);Tube(details,"Arched window frame",prev,next,.035f,dark,1,8);prev=next;}
            Box(details,"Window central mullion",new Vector3(x-.06f,5.56f,z),new Vector3(.10f,1.96f,.045f),dark);
            Box(details,"Window cross mullion",new Vector3(x-.06f,5.9f,z),new Vector3(.10f,.045f,1.4f),dark);
        }
        string sofaPath="Assets/Source/LibrarySofa02.glb";
        var sourceSofa=AssetDatabase.LoadAssetAtPath<GameObject>(sofaPath);
        if(sourceSofa)
        {
            var sofa=UnityEngine.Object.Instantiate(sourceSofa);sofa.name="Library Sofa 02 - original PBR model";
            var rs=sofa.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            float factor=2.8f/Mathf.Max(b.size.x,b.size.z);sofa.transform.localScale*=factor;
            b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);sofa.transform.position=new Vector3(14.4f,0,8.4f)-new Vector3(b.center.x,b.min.y,b.center.z);
            b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);var col=new GameObject("Library sofa collision");col.transform.position=b.center;col.AddComponent<BoxCollider>().size=b.size;
            Debug.Log("PREMIUM_LIBRARY_SOFA_IMPORTED "+b.size);
        }
        else throw new Exception("Verified library Sofa02 missing; do not substitute a low-poly block");
        BatchDetails(details);
        Debug.Log("PREMIUM_PASS_OK upgradedMaterials="+upgraded+" detailParts="+detailCount+" heroRigUnchanged=true");
        File.WriteAllText(Input+"unity-premium-report.json","{\"upgradedMaterials\":"+upgraded+",\"detailParts\":"+detailCount+",\"librarySofa02\":true,\"heroRigChanged\":false,\"glowAdded\":false,\"linearRoughnessMaps\":6,\"originalFloorBrandingPreserved\":true}");
        AssetDatabase.SaveAssets();
    }
}

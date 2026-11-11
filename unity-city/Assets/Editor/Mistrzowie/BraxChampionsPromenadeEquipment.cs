using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Equipment of the promenade kiosks, owner 10.11 09:06-09:17: "budki ... wyposażyć w kebaby, w pizzę. Tam mamy w katalogu
// ... sklepy ... wszystko jest" / "kebaba tego musisz stworzyć, tego co się kręci ... do cięcia. Zrób go jak najlepiej" /
// "kurczaka mamy ... pizzę mamy prawdziwą".
// * Food and shop goods: the owner's shop pack miasto_sklepy (the same models as the city shops: kebab, szawarma, pizza,
//   hamburger, frytki, kurczak, losos, owoce_skrzynka, bottles, fridges, tills, menu boards). Encrypted, restored in CI by
//   restore_private.py; a missing model leaves its place empty and is listed in the log (never a substitute block).
// * Built here: the doner kebab rotisserie (burner panel with glowing ceramic rows, motor head, spit and a layered meat
//   cone that TURNS - BraxChampionsObrot), the chicken rotisserie (glass cabinet, two turning spits with the owner's
//   chickens), pizza oven (tiled dome, glowing mouth, chimney), gelato cabinet with twelve flavours, espresso machine,
//   flat grill, fryers, beer tower, juicer, fruit stand, towels, SUP boards, postcard rack, info screen, bar stools.
// Positions in kiosk-local nominal metres (BraxChampionsPromenade anchors: counters, shelves, walls), front = -z.
public static class BraxChampionsPromenadeEquipment
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    const string MeatTexture="Assets/Source/Mistrzowie/PromenadeFood/kebab_meat.jpg";
    const string Pack="miasto_sklepy";
    static float K=>BraxClaudeOsiedle.K;
    static readonly List<string> placed=new List<string>(),missing=new List<string>();
    static int built,turning;
    static Material meat,ember,tiles,ice,white,screen,glass;
    static Material[] gelato;

    public static string Summary()=>"built="+built+" turning="+turning+" packPlaced="+placed.Count+" packMissing="+missing.Count
        +(missing.Count>0?" ["+string.Join(",",missing.Distinct().Take(20))+"]":"");

    static void Materials()
    {
        if(ember)return;
        placed.Clear();missing.Clear();built=turning=0;
        ember=BraxChampionsHarbour.Surface("grill_ember_glow",new Color(1f,.42f,.12f),.6f,0f,new Color(1.6f,.55f,.14f));
        tiles=BraxChampionsFoundation.SourceMaterial("prom_pizza_oven_black_tiles","rectangular_facade_tiles",.30f,new Color(.13f,.13f,.14f));
        ice=BraxChampionsHarbour.Surface("crushed_ice_white",new Color(.93f,.96f,.98f),.25f,0f);
        white=BraxChampionsHarbour.Surface("porcelain_white",new Color(.96f,.96f,.95f),.20f,0f);
        screen=BraxChampionsHarbour.Surface("info_screen_glow",new Color(.20f,.36f,.55f),.2f,0f,new Color(.35f,.60f,.95f));
        glass=BraxChampionsPromenade.Glass;
        var flavours=new[]{("pistacja",new Color(.62f,.74f,.44f)),("truskawka",new Color(.93f,.45f,.52f)),("wanilia",new Color(.98f,.93f,.76f)),
            ("czekolada",new Color(.33f,.19f,.11f)),("cytryna",new Color(.98f,.93f,.55f)),("jagoda",new Color(.45f,.40f,.68f)),
            ("mango",new Color(.99f,.68f,.30f)),("stracciatella",new Color(.95f,.93f,.88f)),("malina",new Color(.80f,.25f,.38f)),
            ("kawa",new Color(.62f,.45f,.30f)),("kokos",new Color(.99f,.98f,.95f)),("mięta",new Color(.70f,.90f,.78f))};
        gelato=flavours.Select(f=>BraxChampionsHarbour.Surface("gelato_"+f.Item1,f.Item2,.55f,0f)).ToArray();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(MeatTexture);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        meat=new Material(shader){name="PROMENADA | kebab - mięso z rożna",enableInstancing=true};
        if(tex)meat.SetTexture("_MainTex",tex);else BraxBuildQuality.Warn("DO POPRAWKI KEBAB_TEXTURE_MISSING "+MeatTexture);
        meat.SetColor("_Color",new Color(1.08f,1.04f,1.0f));meat.SetFloat("_Roughness",.48f);meat.SetFloat("_Metallic",0f);
        meat.SetFloat("_VertexTint",0f);meat.SetColor("_EmissionColor",new Color(.10f,.04f,.01f));   // warmth of the burners
        meat.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        string path=Output+"/Materials/prom_kebab_meat.mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(meat,old);UnityEngine.Object.DestroyImmediate(meat);EditorUtility.SetDirty(old);meat=old;}
        else AssetDatabase.CreateAsset(meat,path);
    }

    // ---------------------------------------------------------------- the owner's shop pack
    static GameObject PackModel(Transform root,string name,Vector3 local,float yaw,string label,Transform parent=null)
    {
        var src=BraxMiastoAssety.Jest(Pack)?BraxMiastoAssety.Model(Pack,name):null;
        if(!src){missing.Add(name);return null;}
        var at=root.TransformPoint(local*K);
        var o=BraxMiastoAssety.Postaw(parent?parent:root,src,root.name.Split(' ')[0]+" | "+label+" ("+name+")",at,root.eulerAngles.y+yaw,1f,false,false);
        if(!o){missing.Add(name);return null;}
        var b=BraxRegion.Bounds(o);
        o.transform.position+=new Vector3(at.x-b.center.x,at.y-b.min.y,at.z-b.center.z);
        foreach(var r in o.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;}
        foreach(var c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        placed.Add(name);
        return o;
    }
    static void PackRow(Transform root,string[] names,float x0,float x1,float y,float z,float yaw,string label)
    {
        if(names.Length==0)return;
        float step=(x1-x0)/names.Length;
        for(int i=0;i<names.Length;i++)PackModel(root,names[i],new Vector3(x0+step*(i+.5f),y,z),yaw,label);
    }

    // ---------------------------------------------------------------- built equipment
    static Material Steel=>BraxChampionsHarbour.Steel;
    static Material Dark=>BraxChampionsHarbour.Graphite;
    static Material Led=>BraxChampionsHarbour.Led;

    /// <summary>Doner kebab rotisserie standing on a counter at b (front to -z); the meat cone is its own turning object.</summary>
    static void Kebab(Transform root,BraxChampionsMeshBatch m,Vector3 b)
    {
        m.BoxMinMax(Steel,b.x-.34f,b.x+.34f,b.y,b.y+.03f,b.z-.30f,b.z+.32f);                  // base plate
        m.Cylinder(Steel,b+new Vector3(0,.03f,0),.20f,.035f,24,Quaternion.identity,true);     // drip tray
        m.Cylinder(Dark,b+new Vector3(0,.065f,0),.17f,.012f,24,Quaternion.identity,true);     // grease pan, 12 mm
        // burner panel behind the meat: stainless housing, three rows of glowing ceramic burners facing the cone
        m.BoxMinMax(Steel,b.x-.32f,b.x+.32f,b.y+.03f,b.y+1.02f,b.z+.24f,b.z+.34f);
        for(int i=0;i<3;i++)
        {
            float y0=b.y+.12f+i*.28f;
            m.BoxMinMax(Dark,b.x-.26f,b.x+.26f,y0-.01f,y0+.21f,b.z+.225f,b.z+.24f);
            m.BoxMinMax(ember,b.x-.23f,b.x+.23f,y0+.01f,y0+.19f,b.z+.214f,b.z+.226f);   // 11 mm proud of the panel
        }
        // side knobs, top motor head on a rear column, the spit
        foreach(float s in new[]{-1f,1f})m.Cylinder(Dark,new Vector3(b.x+s*.33f,b.y+.18f,b.z+.29f),.025f,.03f,10,Quaternion.Euler(0,0,s*90f));
        m.BoxMinMax(Steel,b.x-.04f,b.x+.04f,b.y+1.02f,b.y+1.16f,b.z+.26f,b.z+.32f);
        m.BoxMinMax(Steel,b.x-.16f,b.x+.16f,b.y+1.10f,b.y+1.24f,b.z-.08f,b.z+.32f);
        m.Cylinder(Dark,b+new Vector3(0,1.04f,0),.03f,.06f,12);
        m.Cylinder(Steel,b+new Vector3(0,.07f,0),.012f,.98f,8);
        // long doner knife and two stacks of flatbread beside the rotisserie
        m.BoxMinMax(Steel,b.x+.38f,b.x+.78f,b.y,b.y+.006f,b.z-.24f,b.z-.19f);
        m.BoxMinMax(Dark,b.x+.78f,b.x+.92f,b.y,b.y+.022f,b.z-.235f,b.z-.195f);
        for(int i=0;i<6;i++)m.Cylinder(white,new Vector3(b.x+.56f,b.y+i*.012f,b.z+.10f),.15f,.011f,20,Quaternion.identity,true);

        // the meat: a layered cone on the spit, turning slowly (one turn in 20 s)
        var cone=new GameObject("KEBAB | mięso na rożnie (kręci się)");cone.transform.SetParent(root,false);
        cone.transform.localPosition=(b+new Vector3(0,.10f,0))*K;
        const int seg=32,rings=28;const float height=.66f;
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        var rnd=new System.Random(1016);var bump=new float[seg];for(int j=0;j<seg;j++)bump[j]=(float)rnd.NextDouble();
        for(int i=0;i<=rings;i++)
        {
            float f=(float)i/rings;
            float r=Mathf.Lerp(.10f,.205f,Mathf.Pow(f,.85f))+.012f*Mathf.Sin(f*Mathf.PI);       // wider at the top, slight belly
            for(int j=0;j<=seg;j++)
            {
                float a=j*Mathf.PI*2f/seg;
                float rr=r+.005f*Mathf.Sin(f*rings*Mathf.PI)+.007f*(bump[j%seg]-.5f)*Mathf.Sin(f*9f+j);   // slice ridges + irregular cut
                v.Add(new Vector3(Mathf.Cos(a)*rr,f*height,Mathf.Sin(a)*rr)*K);
                uv.Add(new Vector2((float)j/seg,f));
            }
        }
        for(int i=0;i<rings;i++)for(int j=0;j<seg;j++)
        {
            int a=i*(seg+1)+j,c=a+seg+1;
            t.AddRange(new[]{a,c,a+1,a+1,c,c+1});
        }
        int top=v.Count;v.Add(new Vector3(0,height,0)*K);uv.Add(new Vector2(.5f,.98f));
        int rim=rings*(seg+1);
        for(int j=0;j<seg;j++)t.AddRange(new[]{top,rim+j+1,rim+j});
        int bottom=v.Count;v.Add(Vector3.zero);uv.Add(new Vector2(.5f,.02f));
        for(int j=0;j<seg;j++)t.AddRange(new[]{bottom,j,j+1});
        var mesh=new Mesh{name="kebab meat cone"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=Output+"/Meshes/Harbour/kebab_meat_cone.asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);mesh=old;}
        else AssetDatabase.CreateAsset(mesh,path);
        cone.AddComponent<MeshFilter>().sharedMesh=mesh;
        var rend=cone.AddComponent<MeshRenderer>();rend.sharedMaterial=meat;rend.shadowCastingMode=ShadowCastingMode.Off;
        var spin=cone.AddComponent<BraxChampionsObrot>();spin.os=Vector3.up;spin.stopniNaSekunde=30f;   // owner 20:03 "kebab ma się kręcić": a turn in 12 s, clearly seen
        turning++;built++;
    }

    // a kiosk's till: a black tablet on a brushed stand (screen to the staff, +z), a card reader toward the customer (-z)
    static void SmallTill(BraxChampionsMeshBatch m,Vector3 b)
    {
        m.Cylinder(Dark,b,.07f,.012f,18,Quaternion.identity,true);
        m.BoxMinMax(Steel,b.x-.012f,b.x+.012f,b.y+.012f,b.y+.14f,b.z-.012f,b.z+.012f);
        var t=b+new Vector3(0,.22f,.0f);var rot=Quaternion.Euler(-22f,180f,0f);
        m.Box(Dark,t,new Vector3(.26f,.175f,.010f),rot);
        m.Box(screen,t+rot*new Vector3(0,0,.007f),new Vector3(.236f,.150f,.004f),rot);
        m.Box(Dark,b+new Vector3(.20f,.035f,-.03f),new Vector3(.075f,.035f,.15f),Quaternion.Euler(12f,0,0));
        m.Box(screen,b+new Vector3(.20f,.056f,-.05f),new Vector3(.05f,.004f,.035f),Quaternion.Euler(12f,0,0));
        built++;
    }
    /// <summary>the doner spit for other generators (the gallery's food court): the same turning cone</summary>
    public static void KebabSpit(Transform root,BraxChampionsMeshBatch m,Vector3 b){Materials();Kebab(root,m,b);}

    /// <summary>Chicken rotisserie: glass cabinet with two horizontal spits of the owner's roast chickens, turning.</summary>
    static void ChickenRotisserie(Transform root,BraxChampionsMeshBatch m,Vector3 b)
    {
        float w=.86f,d=.46f,h=.66f;
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y,b.y+.06f,b.z-d*.5f,b.z+d*.5f);
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y+h-.08f,b.y+h,b.z-d*.5f,b.z+d*.5f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(Steel,b.x+s*w*.5f-.04f,b.x+s*w*.5f+.04f,b.y,b.y+h,b.z-d*.5f,b.z+d*.5f);
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y,b.y+h,b.z+d*.5f-.02f,b.z+d*.5f);
        m.BoxMinMax(glass,b.x-w*.5f+.04f,b.x+w*.5f-.04f,b.y+.06f,b.y+h-.08f,b.z-d*.5f,b.z-d*.5f+.01f);
        m.BoxMinMax(ember,b.x-w*.5f+.06f,b.x+w*.5f-.06f,b.y+h-.10f,b.y+h-.085f,b.z-.12f,b.z+.12f);
        m.BoxMinMax(Dark,b.x-w*.5f+.06f,b.x+w*.5f-.06f,b.y+.06f,b.y+.08f,b.z-d*.5f+.04f,b.z+d*.5f-.04f);
        foreach(float y in new[]{.20f,.42f})
        {
            var spit=new GameObject("ROŻEN Z KURCZAKAMI | kręci się");spit.transform.SetParent(root,false);
            spit.transform.localPosition=(b+new Vector3(0,y,0))*K;
            var sm=new BraxChampionsMeshBatch();sm.Tube(Steel,new Vector3(-w*.5f+.04f,0,0),new Vector3(w*.5f-.04f,0,0),.008f,6);
            sm.Build(spit.transform,"rożen",Output+"/Meshes/Harbour");
            for(int i=0;i<3;i++)
            {
                var c=PackModel(root,"kurczak",b+new Vector3(-.26f+i*.26f,y-.07f,0),90f,"kurczak z rożna",spit.transform);
                if(c)c.transform.SetParent(spit.transform,true);
            }
            var spin=spit.AddComponent<BraxChampionsObrot>();spin.os=Vector3.right;spin.stopniNaSekunde=40f;turning++;
        }
        built++;
    }

    static void PizzaOven(BraxChampionsMeshBatch m,Vector3 floor,float roofY)
    {
        float r=.62f,baseH=.95f;
        m.BoxMinMax(BraxChampionsPromenade.Trav,floor.x-.68f,floor.x+.68f,floor.y,floor.y+baseH,floor.z-.68f,floor.z+.68f);
        m.BoxMinMax(BraxChampionsPromenade.Marble,floor.x-.72f,floor.x+.72f,floor.y+baseH,floor.y+baseH+.04f,floor.z-.72f,floor.z+.72f);
        var c=floor+Vector3.up*(baseH+.04f);
        const int n=7;
        for(int i=0;i<n;i++)
        {
            float a0=i*Mathf.PI*.5f/n,a1=(i+1)*Mathf.PI*.5f/n;
            m.Cylinder(tiles,c+Vector3.up*(Mathf.Sin(a0)*r),Mathf.Cos(a0)*r,(Mathf.Sin(a1)-Mathf.Sin(a0))*r,20,Quaternion.identity,i==n-1,false,Mathf.Cos(a1)*r);
        }
        // mouth with the fire inside, a steel arch, the chimney to the roof, a small stack of logs
        m.BoxMinMax(Dark,floor.x-.30f,floor.x+.30f,c.y,c.y+.36f,floor.z-r-.06f,floor.z-r+.10f);
        m.BoxMinMax(ember,floor.x-.22f,floor.x+.22f,c.y+.01f,c.y+.26f,floor.z-r+.02f,floor.z-r+.04f);
        m.BoxMinMax(Steel,floor.x-.32f,floor.x+.32f,c.y+.36f,c.y+.40f,floor.z-r-.08f,floor.z-r+.10f);
        m.Cylinder(Steel,c+Vector3.up*(r-.04f),.10f,roofY-(c.y+r-.04f),14);
        for(int i=0;i<5;i++)
            m.Cylinder(BraxChampionsPromenade.Wood,new Vector3(floor.x-.55f+i*.12f,floor.y+.07f+(i%2)*.10f,floor.z-.75f),.05f,.40f,8,Quaternion.Euler(90,0,0));
        // pizza peel leaning on the oven
        m.BoxMinMax(Steel,floor.x+.70f,floor.x+.72f,floor.y+.9f,floor.y+1.25f,floor.z-.70f,floor.z-.38f);
        m.Tube(BraxChampionsPromenade.Wood,new Vector3(floor.x+.71f,floor.y+1.25f,floor.z-.54f),new Vector3(floor.x+.71f,floor.y+2.2f,floor.z-.54f),.02f,6);
        built++;
    }

    static void Gelato(BraxChampionsMeshBatch m,float y,float z,float width)
    {
        float d=.56f;
        m.BoxMinMax(Steel,-width*.5f,width*.5f,y,y+.10f,z-d*.5f,z+d*.5f);
        int n=Mathf.Max(3,Mathf.FloorToInt((width-.12f)/.19f));
        float step=(width-.12f)/n;int k=0;
        foreach(float row in new[]{-.12f,.13f})
            for(int i=0;i<n;i++)
            {
                float x=-width*.5f+.06f+step*(i+.5f);
                m.BoxMinMax(Steel,x-step*.45f,x+step*.45f,y+.06f,y+.10f,z+row-.11f,z+row+.11f);
                var mat=gelato[k++%gelato.Length];
                m.Cylinder(mat,new Vector3(x,y+.08f,z+row),step*.40f,.07f,14,Quaternion.identity,true,false,step*.18f);
                m.Cylinder(mat,new Vector3(x+step*.12f,y+.09f,z+row-.04f),step*.22f,.06f,12,Quaternion.identity,true,false,step*.08f);
                m.BoxMinMax(Steel,x+step*.18f,x+step*.22f,y+.10f,y+.24f,z+row+.06f,z+row+.08f);     // spade
            }
        // curved glass front (three facets), glass top, steel frame, LED canopy light
        float gy=y+.10f;
        m.Quad(glass,new Vector3(-width*.5f,gy,z-d*.5f),new Vector3(width*.5f,gy,z-d*.5f),new Vector3(width*.5f,gy+.16f,z-d*.5f+.02f),new Vector3(-width*.5f,gy+.16f,z-d*.5f+.02f));
        m.Quad(glass,new Vector3(-width*.5f,gy+.16f,z-d*.5f+.02f),new Vector3(width*.5f,gy+.16f,z-d*.5f+.02f),new Vector3(width*.5f,gy+.28f,z-d*.5f+.10f),new Vector3(-width*.5f,gy+.28f,z-d*.5f+.10f));
        m.Quad(glass,new Vector3(-width*.5f,gy+.28f,z-d*.5f+.10f),new Vector3(width*.5f,gy+.28f,z-d*.5f+.10f),new Vector3(width*.5f,gy+.32f,z+.05f),new Vector3(-width*.5f,gy+.32f,z+.05f));
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(Steel,s*width*.5f-.015f,s*width*.5f+.015f,y,gy+.33f,z-d*.5f,z+.08f);
        m.BoxMinMax(Steel,-width*.5f,width*.5f,gy+.31f,gy+.34f,z+.03f,z+.09f);
        m.BoxMinMax(Led,-width*.5f+.05f,width*.5f-.05f,gy+.29f,gy+.31f,z+.04f,z+.08f);    // 20 mm LED bar under the canopy
        built++;
    }

    static void Espresso(BraxChampionsMeshBatch m,Vector3 b)
    {
        m.BoxMinMax(Steel,b.x-.37f,b.x+.37f,b.y,b.y+.46f,b.z-.20f,b.z+.22f);
        m.BoxMinMax(Dark,b.x-.36f,b.x+.36f,b.y+.10f,b.y+.40f,b.z-.215f,b.z-.20f);         // 15 mm group panel
        m.BoxMinMax(Steel,b.x-.37f,b.x+.37f,b.y,b.y+.04f,b.z-.34f,b.z-.20f);
        foreach(float s in new[]{-.17f,.17f})
        {
            m.Cylinder(Steel,new Vector3(b.x+s,b.y+.24f,b.z-.25f),.045f,.06f,12);
            m.BoxMinMax(Dark,b.x+s-.012f,b.x+s+.012f,b.y+.26f,b.y+.28f,b.z-.38f,b.z-.25f);
        }
        foreach(float s in new[]{-.32f,.32f})m.Tube(Steel,new Vector3(b.x+s,b.y+.30f,b.z-.22f),new Vector3(b.x+s,b.y+.12f,b.z-.28f),.006f,6);
        for(int i=0;i<6;i++)m.Cylinder(white,new Vector3(b.x-.25f+i*.10f,b.y+.46f,b.z+.02f),.035f,.06f,10,Quaternion.identity,true,false,.03f);
        // grinder beside it
        m.BoxMinMax(Dark,b.x+.46f,b.x+.66f,b.y,b.y+.32f,b.z-.12f,b.z+.12f);
        m.Cylinder(glass,new Vector3(b.x+.56f,b.y+.32f,b.z),.08f,.18f,14,Quaternion.identity,true,false,.10f);
        built++;
    }

    static void FlatGrill(BraxChampionsMeshBatch m,Vector3 b,float w=.80f)
    {
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y,b.y+.18f,b.z-.27f,b.z+.27f);
        m.BoxMinMax(Dark,b.x-w*.5f+.03f,b.x+w*.5f-.03f,b.y+.18f,b.y+.20f,b.z-.24f,b.z+.22f);
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y+.18f,b.y+.30f,b.z+.22f,b.z+.27f);
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(Steel,b.x+s*(w*.5f-.015f)-.015f,b.x+s*(w*.5f-.015f)+.015f,b.y+.18f,b.y+.26f,b.z-.24f,b.z+.27f);
        for(int i=0;i<3;i++)m.Cylinder(Dark,new Vector3(b.x-.2f+i*.2f,b.y+.09f,b.z-.27f),.022f,.03f,10,Quaternion.Euler(-90,0,0));
        m.BoxMinMax(Steel,b.x-.06f,b.x+.06f,b.y+.205f,b.y+.21f,b.z-.18f,b.z-.02f);     // spatula
        built++;
    }

    static void Fryer(BraxChampionsMeshBatch m,Vector3 b)
    {
        // body, a real rim round the oil well and the oil 13 mm below the rim (the dark plate lay in the steel top)
        m.BoxMinMax(Steel,b.x-.21f,b.x+.21f,b.y,b.y+.28f,b.z-.24f,b.z+.24f);
        m.BoxMinMax(Steel,b.x-.21f,b.x-.18f,b.y+.28f,b.y+.30f,b.z-.24f,b.z+.24f);
        m.BoxMinMax(Steel,b.x+.18f,b.x+.21f,b.y+.28f,b.y+.30f,b.z-.24f,b.z+.24f);
        m.BoxMinMax(Steel,b.x-.18f,b.x+.18f,b.y+.28f,b.y+.30f,b.z-.24f,b.z-.18f);
        m.BoxMinMax(Steel,b.x-.18f,b.x+.18f,b.y+.28f,b.y+.30f,b.z+.18f,b.z+.24f);
        m.BoxMinMax(Dark,b.x-.18f,b.x+.18f,b.y+.28f,b.y+.287f,b.z-.18f,b.z+.18f);
        foreach(float s in new[]{-.09f,.09f})
        {
            m.BoxMinMax(Steel,b.x+s-.07f,b.x+s+.07f,b.y+.30f,b.y+.36f,b.z-.12f,b.z+.10f);
            m.Tube(Dark,new Vector3(b.x+s,b.y+.36f,b.z-.12f),new Vector3(b.x+s,b.y+.42f,b.z-.34f),.012f,6);
        }
        built++;
    }

    static void BeerTower(BraxChampionsMeshBatch m,Vector3 b)
    {
        m.BoxMinMax(Steel,b.x-.32f,b.x+.32f,b.y,b.y+.02f,b.z-.10f,b.z+.10f);
        m.Cylinder(Steel,b+Vector3.up*.02f,.05f,.40f,14);
        m.Tube(Steel,new Vector3(b.x-.28f,b.y+.42f,b.z),new Vector3(b.x+.28f,b.y+.42f,b.z),.045f,12);
        for(int i=0;i<4;i++)
        {
            float x=b.x-.21f+i*.14f;
            m.Cylinder(Steel,new Vector3(x,b.y+.33f,b.z-.04f),.012f,.06f,8);
            m.Cylinder(Dark,new Vector3(x,b.y+.46f,b.z-.03f),.016f,.13f,8,Quaternion.identity,true,false,.012f);
        }
        built++;
    }

    static void Juicer(BraxChampionsMeshBatch m,Vector3 b)
    {
        m.Cylinder(Steel,b,.13f,.26f,18);
        m.Cylinder(Steel,b+Vector3.up*.26f,.16f,.10f,18,Quaternion.identity,true,false,.20f);
        m.Cylinder(gelato[6],b+Vector3.up*.32f,.12f,.05f,14);     // oranges in the hopper
        m.Cylinder(glass,b+new Vector3(.30f,0,0),.08f,.22f,14);
        m.Cylinder(gelato[6],b+new Vector3(.30f,0,0),.068f,.15f,14);                 // juice 12 mm inside the glass
        built++;
    }

    static void Vitrine(BraxChampionsMeshBatch m,Vector3 b,float w)
    {
        m.BoxMinMax(Dark,b.x-w*.5f,b.x+w*.5f,b.y,b.y+.08f,b.z-.24f,b.z+.24f);
        m.BoxMinMax(glass,b.x-w*.5f,b.x+w*.5f,b.y+.08f,b.y+.52f,b.z-.24f,b.z+.24f);
        m.BoxMinMax(BraxChampionsPromenade.Marble,b.x-w*.5f+.02f,b.x+w*.5f-.02f,b.y+.28f,b.y+.30f,b.z-.22f,b.z+.22f);
        m.BoxMinMax(Led,b.x-w*.5f+.04f,b.x+w*.5f-.04f,b.y+.505f,b.y+.51f,b.z-.05f,b.z+.05f);
        m.BoxMinMax(Steel,b.x-w*.5f,b.x+w*.5f,b.y+.52f,b.y+.545f,b.z-.25f,b.z+.25f);
        built++;
    }

    static void Stool(BraxChampionsMeshBatch m,Vector3 f)
    {
        m.Cylinder(Dark,f,.20f,.03f,16);m.Cylinder(Steel,f+Vector3.up*.03f,.03f,.70f,10);
        m.Cylinder(Dark,f+Vector3.up*.30f,.17f,.02f,16);
        m.Cylinder(Steel,f+Vector3.up*.72f,.195f,.015f,20);                                          // stainless seat pan
        m.Cylinder(BraxChampionsPromenade.Leather,f+Vector3.up*.735f,.19f,.065f,20,Quaternion.identity,true,false,.165f); // padded leather
    }

    static void FruitStand(Transform root,BraxChampionsMeshBatch m,Vector3 f)
    {
        for(int i=0;i<3;i++)
        {
            float y=f.y+.30f+i*.32f,z=f.z+i*.30f;
            m.BoxMinMax(BraxChampionsPromenade.Wood,f.x-.62f,f.x+.62f,y-.04f,y,z-.17f,z+.17f);
            PackModel(root,"owoce_skrzynka",new Vector3(f.x,y,z),180f,"owoce na straganie");
        }
        foreach(float s in new[]{-1f,1f})m.BoxMinMax(Dark,f.x+s*.62f-.03f,f.x+s*.62f+.03f,f.y,f.y+1.0f,f.z-.17f,f.z+.80f);
        built++;
    }

    static void Towels(BraxChampionsMeshBatch m,Vector3 b)
    {
        for(int s=0;s<3;s++)for(int i=0;i<5;i++)
            m.BoxMinMax(gelato[(s*5+i*3)%gelato.Length],b.x-.15f+s*.36f,b.x+.15f+s*.36f,b.y+i*.045f,b.y+i*.045f+.043f,b.z-.12f,b.z+.12f);
        built++;
    }

    static void Boards(BraxChampionsMeshBatch m,Vector3 f)
    {
        m.BoxMinMax(Dark,f.x-.05f,f.x+.05f,f.y,f.y+1.6f,f.z-.05f,f.z+.05f);
        m.BoxMinMax(Dark,f.x-.05f,f.x+.05f,f.y,f.y+1.6f,f.z+1.95f,f.z+2.05f);
        m.BoxMinMax(Dark,f.x-.05f,f.x+.05f,f.y+1.5f,f.y+1.6f,f.z,f.z+2f);
        // three 56 cm surfboards side by side with 8 cm between them (five 66 cm boards on a 38 cm pitch overlapped:
        // their faces lay in one plane and flickered - owner 10.11 13:05, zero flicker)
        for(int i=0;i<3;i++)
        {
            var mat=i%2==0?white:gelato[5];
            float z=f.z+.36f+i*.64f;
            m.Box(mat,new Vector3(f.x+.55f,f.y+1.40f,z),new Vector3(.08f,2.9f,.56f),Quaternion.Euler(0,0,-18f));
        }
        built++;
    }

    static void PostcardRack(BraxChampionsMeshBatch m,Vector3 f)
    {
        m.Cylinder(Dark,f,.22f,.03f,14);m.Cylinder(Steel,f+Vector3.up*.03f,.025f,1.6f,10);
        for(int i=0;i<4;i++)for(int r=0;r<4;r++)
        {
            var q=Quaternion.Euler(0,i*90f,0);
            m.Box(gelato[(i*4+r)%gelato.Length],f+Vector3.up*(.55f+r*.26f)+q*new Vector3(0,0,.07f),new Vector3(.18f,.12f,.01f),q);
        }
        built++;
    }

    static void InfoScreen(BraxChampionsMeshBatch m,Vector3 wall,float w)
    {
        m.BoxMinMax(Dark,wall.x-w*.5f-.04f,wall.x+w*.5f+.04f,wall.y-.04f,wall.y+w*.56f+.04f,wall.z-.05f,wall.z);
        m.BoxMinMax(screen,wall.x-w*.5f,wall.x+w*.5f,wall.y,wall.y+w*.56f,wall.z-.062f,wall.z-.05f);  // glass 12 mm proud of the bezel
        built++;
    }

    static void Brochures(BraxChampionsMeshBatch m,Vector3 f)
    {
        m.BoxMinMax(Dark,f.x-.30f,f.x+.30f,f.y,f.y+1.35f,f.z-.08f,f.z+.08f);
        for(int r=0;r<4;r++)for(int i=0;i<3;i++)
            m.Box(gelato[(r*3+i+2)%gelato.Length],new Vector3(f.x-.19f+i*.19f,f.y+.40f+r*.26f,f.z-.10f),new Vector3(.15f,.21f,.02f),Quaternion.Euler(-12,0,0));
        built++;
    }

    /// <summary>x ranges (kiosk-local) of equipment on the back counter taller than the space under the first shelf.</summary>
    public static IEnumerable<(float,float)> TallOnBackCounter(BraxChampionsPromenade.KioskRecord p)
    {
        float hw=p.w*.5f,t=.18f;
        switch(p.style)
        {
            case "cafe":yield return(-hw+.85f,-hw+1.95f);break;
            case "fish":yield return(-.80f,.20f);break;
            case "burger":yield return(.30f,.80f);break;
            case "grill":yield return(-.55f,.30f);yield return(hw-t-1.08f,hw-t-.16f);break;
        }
    }

    static readonly Dictionary<string,string[]> Menus=new Dictionary<string,string[]>
    {
        {"cafe",new[]{"BRAXHORN CAFÉ","ESPRESSO 9 ZŁ","CAPPUCCINO 12 ZŁ","LATTE 13 ZŁ","SERNIK 14 ZŁ"}},
        {"icecream",new[]{"LODY • GOFRY","GAŁKA LODÓW 7 ZŁ","GOFR 14 ZŁ","GOFR Z OWOCAMI 19 ZŁ","SHAKE 16 ZŁ"}},
        {"fish",new[]{"RYBY NA WYNOS","DORSZ Z FRYTKAMI 39 ZŁ","ŁOSOŚ Z GRILLA 45 ZŁ","FLĄDRA 36 ZŁ","FRYTKI 12 ZŁ"}},
        {"pizza",new[]{"PIZZA • BISTRO","MARGHERITA 32 ZŁ","PEPPERONI 38 ZŁ","CAPRICCIOSA 39 ZŁ","4 SERY 42 ZŁ"}},
        {"burger",new[]{"BRAX BURGER","CLASSIC 29 ZŁ","CHEESEBURGER 32 ZŁ","BRAX DOUBLE 42 ZŁ","FRYTKI 12 ZŁ"}},
        {"juice",new[]{"SOKI • OWOCE","POMARAŃCZA 14 ZŁ","JABŁKO • MARCHEW 14 ZŁ","SMOOTHIE 18 ZŁ","LEMONIADA 12 ZŁ"}},
        {"bar",new[]{"BAR • DRINKI","PIWO 14 ZŁ","MOJITO 28 ZŁ","APEROL 29 ZŁ","LEMONIADA 12 ZŁ"}},
        {"grill",new[]{"KEBAB • GRILL","KEBAB 29 ZŁ","SZAWARMA 27 ZŁ","KURCZAK Z ROŻNA 35 ZŁ","FRYTKI 12 ZŁ"}},
    };
    // the menu on the back wall above the shelves: y0 its bottom, wallZ the wall's inner face (the board faces the counter, -z)
    static void MenuBoard(BraxChampionsMeshBatch m,BraxChampionsPromenade.KioskRecord p,float y0,float wallZ)
    {
        if(!Menus.TryGetValue(p.style??"",out var lines))return;
        float bw=Mathf.Min(p.w-1.2f,3.0f),bh=.50f,z1=wallZ-.003f,z0=z1-.03f,hw=bw*.5f;
        var dark=BraxChampionsHarbour.SignDark;var face=BraxChampionsHarbour.SignFace;
        if(!dark||!face){BraxBuildQuality.Warn("DO POPRAWKI PROMENADE_MENU_NO_MATERIALS "+p.id);return;}
        m.BoxMinMax(dark,-hw,hw,y0,y0+bh,z0,z1);
        foreach(var (a,b,c,d) in new[]{(-hw-.02f,hw+.02f,y0+bh,y0+bh+.02f),(-hw-.02f,hw+.02f,y0-.02f,y0),(-hw-.02f,-hw,y0,y0+bh),(hw,hw+.02f,y0,y0+bh)})
            m.BoxMinMax(Steel,a,b,c,d,z0-.006f,z1);
        BraxChampionsLetters3D.Text(m,face,dark,lines[0],new Vector3(0,y0+bh-.135f,z0),180f,.10f,.008f,.12f,bw-.2f);
        m.BoxMinMax(Steel,-hw+.12f,hw-.12f,y0+bh-.165f,y0+bh-.160f,z0-.004f,z0);   // a steel rule under the title
        for(int i=1;i<lines.Length;i++)
        {
            float col=(i-1)%2==0?-bw*.25f:bw*.25f;   // seen from the counter -x is the reader's left (render check 12.11)
            float row=y0+bh-.27f-((i-1)/2)*.12f;
            BraxChampionsLetters3D.Text(m,face,dark,lines[i],new Vector3(col,row,z0),180f,.062f,.006f,.12f,bw*.5f-.12f);
        }
    }

    public static void Fit(Transform root,BraxChampionsPromenade.KioskRecord p)
    {
        Materials();
        var m=new BraxChampionsMeshBatch();
        float hw=p.w*.5f,hd=p.d*.5f,t=.18f,ft=BraxChampionsPromenade.FrontCounterTop,bt=BraxChampionsPromenade.BackCounterTop;
        float fz=BraxChampionsPromenade.FrontCounterZ(p.d),bz=BraxChampionsPromenade.BackCounterZ(p.d),floor=BraxChampionsPromenade.FloorTop+.012f;
        float wallZ=hd-t;float roofY=p.h-.20f;
        var shelves=BraxChampionsPromenade.ShelfTops;float sz=BraxChampionsPromenade.ShelfZ(p.d);
        // fridge on the left wall facing into the kiosk, till on the counter, menu board above the shelves
        void Fridge()=>PackModel(root,"lodowka",new Vector3(-hw+t+.38f,floor,-.05f),90f,"lodówka z napojami");
        // owner 11.11 20:03 ("pousuwać te kasy fiskalne wielkie, które są, zagradzają"): no more the pack's supermarket till on
        // the counters - a small POS tablet on a stand and a card reader (code-built)
        void Till(float x,float y,float z)=>SmallTill(m,new Vector3(x,y,z));
        // owner 12.11 00:13 ("jak jest kebab, to jest tabliczka shaker bar ... z cennikami"): the pack's menu boards carried other
        // bars' names and prices - each kiosk has its own 3D menu now (graphite board, steel frame, its name and its prices)
        void Menu(string id)=>MenuBoard(m,p,shelves[2]+.28f,wallZ);
        void Bottles(int count,int shelf,int from)
        {
            var names=Enumerable.Range(0,count).Select(i=>"but_"+(((from+i)%18)+1).ToString("00")).ToArray();
            PackRow(root,names,-hw+t+.12f,hw-t-.12f,shelves[shelf],sz,180f,"butelki");
        }
        switch(p.style)
        {
            case "cafe":
                Espresso(m,new Vector3(-hw+1.25f,bt,bz));
                Vitrine(m,new Vector3(hw-1.15f,ft,fz),1.2f);
                Fridge();Till(-hw+.75f,ft,fz);Menu("menu_2");
                break;
            case "icecream":
                Gelato(m,ft,fz,p.w-1.0f);
                FlatGrill(m,new Vector3(hw-1.0f,bt,bz),.55f);     // waffle iron
                Fridge();Till(-hw+.75f,bt,bz);Menu("menu_3");
                break;
            case "fish":
                m.BoxMinMax(Steel,-1.15f,1.15f,ft,ft+.04f,fz-.24f,fz+.20f);
                m.BoxMinMax(ice,-1.10f,1.10f,ft+.04f,ft+.075f,fz-.20f,fz+.16f);
                PackRow(root,new[]{"losos","losos","frytki"},-1.0f,1.0f,ft+.075f,fz,180f,"ryba na lodzie");
                Fryer(m,new Vector3(-.55f,bt,bz));Fryer(m,new Vector3(-.05f,bt,bz));
                PackModel(root,"frytki",new Vector3(hw-.75f,ft,fz),180f,"frytki");
                Till(-hw+.75f,ft,fz);Menu("menu_4");
                break;
            case "pizza":
                PizzaOven(m,new Vector3(hw-t-.80f,floor,-.15f),roofY);
                foreach(float x in new[]{-1.4f,-.55f,.3f})
                {
                    m.Cylinder(BraxChampionsPromenade.Wood,new Vector3(x,ft,fz),.22f,.02f,20);
                    PackModel(root,"pizza",new Vector3(x,ft+.02f,fz),180f,"pizza");
                }
                Till(-hw+.6f,bt,bz);Menu("menu_3");
                break;
            case "burger":
                FlatGrill(m,new Vector3(-.4f,bt,bz),.9f);Fryer(m,new Vector3(.55f,bt,bz));
                PackRow(root,new[]{"hamburger","hamburger","frytki","hamburger","frytki"},-hw+.6f,hw-.6f,ft,fz,180f,"burgery i frytki");
                Fridge();Till(hw-.5f,bt,bz);Menu("menu_2");
                break;
            case "juice":
                Juicer(m,new Vector3(-.6f,ft,fz));Juicer(m,new Vector3(.4f,ft,fz));
                FruitStand(root,m,new Vector3(-hw-.95f,BraxChampionsPromenade.FloorTop,-hd-.60f));
                PackRow(root,new[]{"owoce_skrzynka","owoce_skrzynka"},-hw+.5f,hw-.5f,bt,bz,180f,"owoce");
                Fridge();Bottles(8,1,0);Till(hw-.6f,ft,fz);Menu("menu_4");
                break;
            case "beach_shop":
                Towels(m,new Vector3(-hw+.8f,bt,bz));
                Fridge();Bottles(8,0,2);Bottles(8,1,6);Till(hw-.7f,bt,bz);
                break;
            case "souvenirs":
                PostcardRack(m,new Vector3(hw+.55f,BraxChampionsPromenade.FloorTop,-hd-.70f));
                Till(hw-.7f,bt,bz);
                break;
            case "bar":
                BeerTower(m,new Vector3(-.4f,ft,fz));
                foreach(float x in new[]{-hw+.8f,-hw+1.9f,hw-1.9f,hw-.8f})Stool(m,new Vector3(x,BraxChampionsPromenade.FloorTop,-hd-.55f));
                Fridge();Bottles(9,0,0);Bottles(9,1,9);Bottles(9,2,4);Till(hw-.6f,ft,fz);Menu("menu_4");
                break;
            case "grill":
                // the doner kebab rotisserie in the middle of the back counter, chicken rotisserie and grill beside it
                Kebab(root,m,new Vector3(-.15f,bt,bz-.02f));
                ChickenRotisserie(root,m,new Vector3(hw-t-.62f,bt,bz));
                FlatGrill(m,new Vector3(-hw+t+.62f,bt,bz),.75f);
                PackRow(root,new[]{"szawarma","kebab","frytki","szawarma"},-hw+.7f,hw-.7f,ft,fz,180f,"kebab, szawarma, frytki");
                Fridge();Till(hw-.5f,ft,fz-.05f);Menu("menu_2");
                break;
            case "rental":
                Boards(m,new Vector3(hw+.35f,BraxChampionsPromenade.FloorTop,-hd+.2f));
                Till(hw-.7f,bt,bz);
                break;
            case "info":
                InfoScreen(m,new Vector3(0,shelves[2]+.22f,wallZ),1.2f);
                Brochures(m,new Vector3(-hw-.55f,BraxChampionsPromenade.FloorTop,-hd-.45f));
                break;
        }
        m.Build(root,p.id+" wyposażenie",Output+"/Meshes/Harbour");
    }
}

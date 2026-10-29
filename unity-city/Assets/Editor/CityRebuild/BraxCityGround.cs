using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxCityGround
{
    static Material pavement,pavementVariation,asphalt,curb,paint,iron,joints;
    static Texture2D Texture(string dir,string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(dir+name);
    public static void Prepare()
    {
        string dir="Assets/Source/CityRebuild/textures/concrete_pavement_02/";
        var diffuse=Texture(dir,"diffuse.jpg");var normal=Texture(dir,"normal.png")??Texture(dir,"normal.jpg");var arm=Texture(dir,"arm.jpg");
        if(!diffuse||!normal||!arm)throw new InvalidOperationException("Verified pavement PBR source is incomplete");
        pavement=BraxCityMaterials.Textured("Street realistic granite paving stone / existing 2K maps",diffuse,normal,arm,1f/1.8f);
        pavement.SetColor("_Color",new Color(.80f,.77f,.72f));
        pavement.SetFloat("_BumpScale",1.25f);
        pavementVariation=BraxCityMaterials.Textured("Street granite warm tone / existing 2K maps",diffuse,normal,arm,1f/1.8f);
        pavementVariation.SetColor("_Color",new Color(.69f,.66f,.62f));
        pavementVariation.SetFloat("_BumpScale",1.20f);
        // Deep neutral-charcoal material retains the real source albedo,
        // original roughness, physical metalness and normal maps. Never a
        // plain black material or a blue-tinted glossy road.
        asphalt=BraxCityArchitecture.SurfaceTexture("Deep charcoal grainy asphalt PBR / original source","clean_asphalt","clean_asphalt_diff_2k.jpg",2.1f);
        asphalt.SetColor("_Color",new Color(.44f,.43f,.41f));
        asphalt.SetFloat("_BumpScale",1.35f);
        joints=BraxCityMaterials.Surface("Dark recessed mortar joints between actual stones",new Color(.17f,.16f,.15f),.99f);
        curb=BraxCityMaterials.Surface("Rough natural granite kerb neutral",new Color(.44f,.43f,.40f),.97f);
        paint=BraxCityMaterials.Surface("Road weathered white",new Color(.73f,.73f,.68f),.96f);
        iron=BraxCityMaterials.Surface("Drain cast iron",new Color(.11f,.12f,.115f),.86f,.3f);
    }
    public static void Plane(Transform root,string name,Rect area,float y,Material material,Rect[] holes=null,float step=.8f)
    {
        var xs=new SortedSet<float>{area.xMin,area.xMax};var zs=new SortedSet<float>{area.yMin,area.yMax};
        // Use integer-indexed grid positions instead of cumulative float
        // addition. 46 + 5 * .8 must coincide with the crossing's exact x=50:
        // repeated += .8 produced x=49.999996 and a zero-area thin cell.
        if(step<=0 || float.IsNaN(step) || float.IsInfinity(step))throw new ArgumentOutOfRangeException(nameof(step));
        for(int i=1; ;i++)
        {
            float x=area.xMin+i*step;
            if(x>=area.xMax-.001f)break;
            xs.Add(x);
        }
        for(int i=1; ;i++)
        {
            float z=area.yMin+i*step;
            if(z>=area.yMax-.001f)break;
            zs.Add(z);
        }
        foreach(var h in holes??Array.Empty<Rect>())
        {
            if(h.xMin>area.xMin&&h.xMin<area.xMax)xs.Add(h.xMin);if(h.xMax>area.xMin&&h.xMax<area.xMax)xs.Add(h.xMax);
            if(h.yMin>area.yMin&&h.yMin<area.yMax)zs.Add(h.yMin);if(h.yMax>area.yMin&&h.yMax<area.yMax)zs.Add(h.yMax);
        }
        var xx=xs.ToArray();var zz=zs.ToArray();var data=new BraxCitySystemMesh();
        for(int i=0;i<xx.Length-1;i++)for(int j=0;j<zz.Length-1;j++)
        {
            var center=new Vector2((xx[i]+xx[i+1])*.5f,(zz[j]+zz[j+1])*.5f);
            if(holes!=null&&holes.Any(h=>h.Contains(center)))continue;
            // Float32 grid stepping can produce near-duplicate boundaries at the
            // crossing cuts (e.g. 49.999996 vs 50.0 metres). Their triangles
            // have zero normals and abort the offline city lighting bake.
            // Omit only sub-2mm nonphysical strips; preserve exact cut edges.
            if(xx[i+1]-xx[i]<.002f||zz[j+1]-zz[j]<.002f)continue;
            data.Quad(new Vector3(xx[i],y,zz[j]),new Vector3(xx[i],y,zz[j+1]),new Vector3(xx[i+1],y,zz[j+1]),new Vector3(xx[i+1],y,zz[j]));
        }
        if(data.VertexCount==0)return;
        var obj=BraxCityArchitecture.MeshObject(name,root,data,material);var mesh=obj.GetComponent<MeshFilter>().sharedMesh;
        mesh.uv=mesh.vertices.Select(p=>new Vector2(p.x,p.z)).ToArray();mesh.RecalculateTangents();
        // A pavement plane must have upward-facing, non-degenerate normals.
        // Validate before attempting the expensive static-lighting bake.
        foreach(var normal in mesh.normals)
            if(normal.sqrMagnitude<.9f||Vector3.Dot(normal,Vector3.up)<.99f)
                throw new InvalidOperationException("Invalid pavement mesh normal: "+name+" ("+area+")");
    }
    // Stone pavers are genuine geometry rather than a painted grid. Each
    // ten-metre block has two spatially separate surface meshes: warmer and
    // cooler sourced PBR textures. Underlying grout sits below the original
    // walkable y=.15f collision, so there is no extra step or capsule snag.
    // Staggered joints, millimetre gaps and true stone side faces read from
    // the player's 1.65m eye height without per-stone GameObjects.
    static void Cobblestone(Transform root,string side,Rect area)
    {
        const float surfaceY=.15f;
        const float stoneX=.52f,stoneZ=.34f,gap=.014f,thickness=.028f;
        Plane(root,side+" real recessed stone grout",area,surfaceY-thickness-.004f,joints,null,1.4f);
        var light=new BraxCitySystemMesh();
        var dark=new BraxCitySystemMesh();
        int row=0,count=0;
        for(float z=area.yMin;z<area.yMax-.002f;z+=stoneZ,row++)
        {
            float endZ=Mathf.Min(z+stoneZ,area.yMax);
            float startX=area.xMin-(row%2==0?0:stoneX*.5f);
            for(float x=startX;x<area.xMax-.002f;x+=stoneX)
            {
                float x0=Mathf.Max(x,area.xMin),x1=Mathf.Min(x+stoneX,area.xMax);
                if(x1-x0<.08f)continue;
                float w=x1-x0-gap,d=endZ-z-gap;
                if(w<.05f||d<.06f)continue;
                var m=(count++%7==0 || (row+count)%11==0)?dark:light;
                m.Box(new Vector3((x0+x1)*.5f,surfaceY-thickness*.5f,(z+endZ)*.5f),
                    new Vector3(w,thickness,d),1.8f);
            }
        }
        if(light.VertexCount>0)BraxCityArchitecture.MeshObject(side+" hand-laid full stone pavers",root,light,pavement);
        if(dark.VertexCount>0)BraxCityArchitecture.MeshObject(side+" natural stone variation",root,dark,pavementVariation);
        Debug.Log("CITY_STREET_PHYSICAL_PAVERS "+side+" stones="+count+" area="+area);
    }

    static void Kerb(Transform root,float x0,float x1,float z,bool reverse)
    {
        var m=new BraxCitySystemMesh();
        for(float x=x0;x<x1-.01f;x+=1)
        {
            float end=Mathf.Min(x+.99f,x1);if(x<54&&end>50)continue;
            float a=z-.12f,b=z+.12f;
            // Chamfered wearing edge, actual stone joints, no collision steps
            // added on top of the original walkable kerb physics.
            var profile=new[]{new Vector2(a,-.03f),new Vector2(a,.12f),new Vector2(a+.035f,.15f),new Vector2(b-.035f,.15f),new Vector2(b,.12f),new Vector2(b,-.03f)};
            for(int k=0;k<profile.Length-1;k++)
            {
                var q=profile[k];var r=profile[k+1];
                m.Quad(new Vector3(x,q.y,q.x),new Vector3(x,r.y,r.x),new Vector3(end,r.y,r.x),new Vector3(end,q.y,q.x));
            }
        }
        if(m.VertexCount>0)BraxCityArchitecture.MeshObject("Jointed chamfered kerbstones",root,m,curb);
    }
    public static void Street(Transform parent)
    {
        Prepare();
        var root=new GameObject("Street pavement and dry asphalt").transform;root.SetParent(parent,false);
        for(float x=-104;x<66;x+=10)
        {
            float end=Mathf.Min(x+10,66);
            Cobblestone(root,"Near pavement",new Rect(x,20.6f,end-x,3.4f));
            Cobblestone(root,"Far pavement",new Rect(x,33,end-x,3.1f));
            Plane(root,"Road segment",new Rect(x,24,end-x,9),.003f,asphalt);
            Kerb(root,x,end,23.875f,false);Kerb(root,x,end,33.125f,true);
        }
        for(float x=-98;x<62;x+=7)
        {
            if(x>43&&x<58)continue;
            BraxCityArchitecture.Box("Centre line dash",root,new Vector3(x,.009f,28.5f),new Vector3(3,.006f,.12f),paint);
        }
        foreach(float x in new[]{-91f,-73f,-55f,-32f,-9f,22f,42f,60f})
        {
            var grate=new BraxCitySystemMesh();
            grate.Box(new Vector3(x,.012f,24.28f),new Vector3(.65f,.018f,.41f));
            for(int i=0;i<9;i++)grate.Box(new Vector3(x-.28f+i*.07f,.024f,24.28f),new Vector3(.026f,.012f,.35f));
            BraxCityArchitecture.MeshObject("Road drainage inlet",root,grate,iron);
        }
    }

    public static void Viaduct(Transform parent)
    {
        var steel=BraxCityMaterials.Surface("Viaduct weathered graphite steel",new Color(.19f,.21f,.21f),.76f,.48f);
        var concrete=BraxCityMaterials.Surface("Viaduct stone abutments",new Color(.45f,.44f,.40f),.96f);
        var root=new GameObject("Riveted steel viaduct").transform;root.SetParent(parent,false);
        for(float z=-14;z<120;z+=10)
        {
            var segment=new GameObject("Viaduct bay "+z).transform;segment.SetParent(root,false);
            var frame=new BraxCitySystemMesh();
            foreach(float x in new[]{-70.7f,-61.7f})
            {
                frame.Box(new Vector3(x,8.7f,z+5),new Vector3(.13f,1.05f,10));
                frame.Box(new Vector3(x,8.2f,z+5),new Vector3(.52f,.13f,10));
                frame.Box(new Vector3(x,9.2f,z+5),new Vector3(.52f,.13f,10));
                for(float k=z;k<z+10;k+=1)
                {
                    frame.Box(new Vector3(x,8.7f,k),new Vector3(.32f,1.05f,.085f));
                    foreach(float y in new[]{8.37f,9.03f})frame.Tube(new Vector3(x-.11f,y,k),new Vector3(x+.11f,y,k),.025f,8);
                }
            }
            for(float k=z;k<z+10;k+=2.5f)
            {
                frame.Box(new Vector3(-66.2f,8.37f,k),new Vector3(9.4f,.43f,.12f));
                frame.Box(new Vector3(-66.2f,8.18f,k),new Vector3(9.4f,.07f,.32f));
                frame.Tube(new Vector3(-70.7f,8.35f,k),new Vector3(-61.7f,8.35f,k+2.4f),.045f,8);
            }
            BraxCityArchitecture.MeshObject("Riveted longitudinal girders and cross members",segment,frame,steel);
            BraxCityArchitecture.Box("Concrete railway deck",segment,new Vector3(-66.2f,9.35f,z+5),new Vector3(9.5f,.24f,10),concrete,true);
            foreach(float x in new[]{-67.0f,-65.5f})BraxCityArchitecture.Box("Rail retained for later train module",segment,new Vector3(x,9.58f,z+5),new Vector3(.09f,.16f,10),steel);
            if(z+5>21&&z+5<37)continue;
            foreach(float x in new[]{-69.9f,-62.5f})
            {
                BraxCityArchitecture.Box("Viaduct pier footing",segment,new Vector3(x,.18f,z+5),new Vector3(1.25f,.36f,1.25f),concrete,true);
                BraxCityArchitecture.Box("Viaduct structural column",segment,new Vector3(x,4.23f,z+5),new Vector3(.72f,8.1f,.78f),concrete,true);
                BraxCityArchitecture.Box("Column capital",segment,new Vector3(x,8.02f,z+5),new Vector3(1.2f,.35f,1.1f),concrete);
            }
        }
    }
}

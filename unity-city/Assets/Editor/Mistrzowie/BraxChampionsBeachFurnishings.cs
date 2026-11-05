using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Miasto Mistrzów beach. Owner's original R1 lifeguard_tower.glb and the
// SAME R1 BraxNabrzezeMebel.Lezak dimensions: 1.95 x 0.65m, 40° backrest.
// No simulated vendor, NPC, sunbathers or crowd in this phase.
// Retain existing 650m long golden PBR beach; do not touch promenade or city R1.
// Fabric and timber use actual verified Poly Haven 4K albedo/normal/ARM,
// imported 2K ASTC for Android by BraxChampionsFoundation.SourceMaterial.
public static class BraxChampionsBeachFurnishings
{
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    static float K=>BraxClaudeOsiedle.K;

    static float S(float t) { t=Mathf.Clamp01(t); return t*t*(3f-2f*t); }

    static float Shore(float x)
    {
        float blend=S((x-50f)/37f)*S((700f-x)/49f);
        float deeper=-273f+7.2f*Mathf.Sin(x*.013f+.4f)+3.3f*Mathf.Sin(x*.034f+1.1f);
        return -163f+(deeper+163f)*blend;
    }

    static float SandY(float x,float z)
    {
        float s=Shore(x);
        float t=Mathf.Clamp01((-161f-z)/(-161f-s));
        return .16f-2.26f*Mathf.Pow(t,1.32f)
               +.034f*Mathf.Sin(x*.045f)*Mathf.Sin(z*.082f)*Mathf.Sin(Mathf.PI*t);
    }

    static void Cube(Transform root,string name,Material material,
                     Vector3 centre,Vector3 dimensions,Quaternion localRotation,
                     bool collision=false)
    {
        if(dimensions.x<=.008f||dimensions.y<=.008f||dimensions.z<=.008f)
            throw new InvalidOperationException("Zero-thickness beach furniture: "+name);
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name=name;obj.transform.SetParent(root,false);
        obj.transform.localPosition=centre*K;
        obj.transform.localScale=dimensions*K;
        obj.transform.localRotation=localRotation;
        obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        obj.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        if(!collision)UnityEngine.Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
    }

    static Material Timber,Metal,FabricCream,FabricNavy,FabricCoral;

    static void BuildLounger(Transform beach,float x,float z,int count)
    {
        float y=SandY(x,z);
        if(y<=-1.6f)throw new InvalidOperationException("Lounger position too near tide zone");
        var basis=new GameObject("R1 ORIGINAL DIMENSIONS | empty beach lounger "+count);
        basis.transform.SetParent(beach,false);
        basis.transform.position=new Vector3(x,y,z)*K;
        var t=basis.transform;
        float w=.65f,L=1.95f,h=.32f,lie=L*.62f,back=L-lie;
        foreach(float side in new[]{-.46f,.46f})
        {
            float sx=side*w;
            Cube(t,"R1 steel longitudinal frame "+side,Metal,
                 new Vector3(sx,h,-L*.5f+lie*.5f),new Vector3(.035f,.05f,lie),Quaternion.identity);
            foreach(float zz in new[]{-L*.46f,-L*.5f+lie-.05f})
                Cube(t,"R1 steel lounger foot",Metal,
                     new Vector3(sx,h*.5f,zz),new Vector3(.035f,h,.035f),Quaternion.identity);
        }
        for(int i=0;i<9;i++)
            Cube(t,"R1 closely fitted teak seat slat",Timber,
                 new Vector3(0,h+.03f,-L*.5f+.06f+i*lie/9f),
                 new Vector3(w*.9f,.02f,lie/9f*.78f),Quaternion.identity);
        var backRot=Quaternion.Euler(-40f,0,0);
        var bc=new Vector3(0,h,-L*.5f+lie)+backRot*new Vector3(0,0,back*.5f);
        for(int i=0;i<5;i++)
            Cube(t,"R1 matching raised timber back slat",Timber,
                 bc+backRot*new Vector3(0,.03f,-back*.4f+i*back*.2f),
                 new Vector3(w*.9f,.02f,back*.16f),backRot);
        foreach(float side in new[]{-.46f,.46f})
            Cube(t,"R1 matching back arm steel tube",Metal,
                 bc+backRot*new Vector3(side*w,0,0),
                 new Vector3(.03f,.04f,back),backRot);
        var fabric=count%3==0?FabricCoral:count%2==0?FabricNavy:FabricCream;
        // Inlaid R1-style towel sits above slats (real-world thickness), not
        // duplicate carpet mesh on the existing sand plane.
        Cube(t,"R1 original geometric towel seat",fabric,
             new Vector3(0,h+.055f,-L*.5f+lie*.55f),
             new Vector3(w*.82f,.012f,lie*.9f),Quaternion.identity);
        Cube(t,"R1 original geometric towel back",fabric,
             bc+backRot*new Vector3(0,.055f,0),
             new Vector3(w*.82f,.012f,back*.8f),backRot);
        var collider=basis.AddComponent<BoxCollider>();
        collider.center=new Vector3(0,h*.6f,0)*K;
        collider.size=new Vector3(w,h*1.2f+.1f,L*.95f)*K;
        // Existing BraxSeat permits a later gameplay action. No human is
        // spawned, no animation/player rig is touched in this phase.
    }

    static void BuildUmbrella(Transform beach,float x,float z,int count)
    {
        float y=SandY(x,z);
        var obj=new GameObject("R1 original 2.30m sunshade geometry "+count);
        obj.transform.SetParent(beach,false);
        obj.transform.position=new Vector3(x,y,z)*K;
        var root=obj.transform;
        var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name="Sunshade real steel pole";
        pole.transform.SetParent(root,false);
        pole.transform.localPosition=new Vector3(0,1.1f,0)*K;
        pole.transform.localScale=new Vector3(.019f,1.10f,.019f)*K;
        pole.GetComponent<Renderer>().sharedMaterial=Metal;
        var fabric=count%2==0?FabricCream:FabricNavy;
        // R1 has a 16 segment cloth canopy with 8 light struts and fall.
        var mesh=new Mesh{name="R1 16 segment umbrella canopy cloth"};
        var verts=new List<Vector3>();
        var uvs=new List<Vector2>();
        var tris=new List<int>();
        float radius=1.2f;
        verts.Add(new Vector3(0,2.6f,0)*K);uvs.Add(new Vector2(.5f,.5f));
        for(int j=0;j<16;j++)
        {
            float a=2f*Mathf.PI*j/16;
            float sag=j%2==0?0f:.06f;
            verts.Add(new Vector3(radius*Mathf.Cos(a),2.18f-sag,radius*Mathf.Sin(a))*K);
            uvs.Add(new Vector2(.5f+.5f*Mathf.Cos(a),.5f+.5f*Mathf.Sin(a)));
        }
        for(int j=0;j<16;j++)
        {
            int a=j+1,b=(j+1)%16+1;
            tris.Add(0);tris.Add(a);tris.Add(b);
            tris.Add(0);tris.Add(b);tris.Add(a); // visible from both sides
        }
        mesh.SetVertices(verts);mesh.SetUVs(0,uvs);
        mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var path=Output+"/Meshes/beach_umbrella_"+count+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);}
        else AssetDatabase.CreateAsset(mesh,path);
        var canopy=new GameObject("Poly Haven 4K cloth 2K Android canopy");
        canopy.transform.SetParent(root,false);
        canopy.AddComponent<MeshFilter>().sharedMesh=mesh;
        canopy.AddComponent<MeshRenderer>().sharedMaterial=fabric;
        var c=obj.AddComponent<CapsuleCollider>();
        c.center=new Vector3(0,1.15f,0)*K;
        c.height=2.3f*K;c.radius=.05f*K;
    }

    static GameObject OriginalLifeguard(Transform parent)
    {
        // Exact file used by the R1 sea-front generator. It is already in
        // the owner's repository; do not download a substitute or modify R1.
        if(!File.Exists("Assets/Source/HarborBeach/Models/lifeguard_tower.glb"))
            throw new FileNotFoundException("Existing R1 lifeguard tower model required");
        const float x=331f,z=-218f;
        float seaY=SandY(x,z);
        if(seaY<=-1.5f)throw new InvalidOperationException("Lifeguard steps reach tide zone");
        var root=new GameObject("ORIGINAL FIRST-CITY R1 LIFEGUARD TOWER COPY - NO NPC").transform;
        root.SetParent(parent,false);
        var targetHeight=BraxNabrzezeRatownik.WysModelu*BraxNabrzezeRatownik.Skala;
        var tower=BraxHarborBeachBuild.Postaw(root,
            "lifeguard_tower.glb","R1 original tower | access to beach",
            new Vector3(x,seaY,z)*K,targetHeight,0);
        if(!tower)throw new InvalidOperationException("Cannot import original existing R1 lifeguard tower");
        var colliders=new GameObject("R1 original lifeguard entrance and stair collisions").transform;
        colliders.SetParent(root,false);
        int count=BraxNabrzezeRatownik.Wejscie(tower,colliders,out float deckHeight);
        if(count<=0)
            throw new InvalidOperationException("R1 tower imported but measured stair/walkway colliders FAILED, do not place phantom tower");
        Debug.Log("CHAMPIONS_BEACH_R1_LIFEGUARD original_GLb=TRUE stairs_colliders="+count+" accessibleDeckY="+deckHeight);
        return tower;
    }

    public static void Build(Transform foundation)
    {
        if(!foundation)throw new ArgumentNullException(nameof(foundation));
        Directory.CreateDirectory(Output+"/Meshes");
        Directory.CreateDirectory(Output+"/Materials");
        Timber=BraxChampionsFoundation.SourceMaterial("beach_r1_original_teak","teak_veneer",1.1f,new Color(.88f,.82f,.73f));
        Metal=BraxChampionsFoundation.LibraryMetal("beach_r1_tubular_steel",new Color(.70f,.73f,.76f));
        FabricCream=BraxChampionsFoundation.SourceMaterial("beach_fabric_cream","fabric_pattern_05",.62f,new Color(.97f,.94f,.83f));
        FabricNavy=BraxChampionsFoundation.SourceMaterial("beach_fabric_navy","fabric_pattern_05",.62f,new Color(.33f,.51f,.68f));
        FabricCoral=BraxChampionsFoundation.SourceMaterial("beach_fabric_coral","fabric_pattern_05",.62f,new Color(.93f,.58f,.49f));
        var root=new GameObject("MIASTO MISTRZOW | ORIGINAL R1 LIFEGUARD + 32 EMPTY LOUNGERS + 16 PARASOLS").transform;
        root.SetParent(foundation,false);
        var tower=OriginalLifeguard(root);
        int number=0;
        for(int n=0;n<16;n++)
        {
            float x=132f+n*33f;
            if(Mathf.Abs(x-331f)<27f)continue; // emergency access to tower
            BuildUmbrella(root,x,-204f,n);
            BuildLounger(root,x-1.12f,-208f,number++);
            BuildLounger(root,x+1.12f,-208f,number++);
        }
        Debug.Log("CHAMPIONS_BEACH_ASSETS_SOURCE R1_LIFEGUARD=1 original_r1_chaise_dimensions=1.95x.65m"+
                  " occupied_by_people=0 actor_modifications=0 sea_waves_unchanged=1 Android2K=true");
    }
}

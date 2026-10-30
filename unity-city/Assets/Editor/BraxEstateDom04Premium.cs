using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BraxEstateDom04Premium
{
    const string Source="Assets/Source/City/Dom04/";
    const string Output="Assets/PlayableGenerated/Dom04/";
    static Texture2D Tex(string name,bool normal=false)
    {
        string p=Source+name+".png";
        var imp=AssetImporter.GetAtPath(p) as TextureImporter;
        if(!imp) throw new InvalidOperationException("DOM04 verified material missing: "+p);
        imp.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        imp.sRGBTexture=!normal && !name.EndsWith("MR");
        imp.maxTextureSize=1024; imp.mipmapEnabled=true; imp.wrapMode=TextureWrapMode.Repeat;
        imp.anisoLevel=4; imp.textureCompression=TextureImporterCompression.Compressed;
        imp.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if(!tex)throw new InvalidOperationException("DOM04 texture import failed: "+p);
        return tex;
    }
    static Material Pbr(string label, string key, Color tint, float normalStrength)
    {
        var shader=Shader.Find("Standard");
        if(!shader)throw new Exception("Unity Standard PBR missing");
        var m=new Material(shader){name="DOM04 / "+label,enableInstancing=true};
        m.mainTexture=Tex(key+"Albedo");m.color=tint;
        m.SetTexture("_BumpMap",Tex(key+"Normal",true));m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_BumpScale",normalStrength);
        if(key=="Beige"){m.SetTexture("_MetallicGlossMap",Tex("BeigeMR"));m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_GlossMapScale",.55f);}
        else {m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.13f);}
        AssetDatabase.CreateAsset(m,Output+label+".mat");return m;
    }
    static Mesh Ground()
    {
        // Exact bounded DOM04 lawn only: old city estate stays as underlying floor.
        // Top at 0.051: above original lawn y<=0.04, below existing paving y>=0.08.
        var m=new Mesh{name="DOM04 lawn terrain surface / UV 3.2m"};
        m.vertices=new[]{new Vector3(-40,.051f,-10),new Vector3(-7.5f,.051f,-10),new Vector3(-7.5f,.051f,10),new Vector3(-40,.051f,10)};
        m.triangles=new[]{0,2,1,0,3,2};m.normals=Enumerable.Repeat(Vector3.up,4).ToArray();
        m.uv=new[]{new Vector2(0,0),new Vector2(10.15625f,0),new Vector2(10.15625f,6.25f),new Vector2(0,6.25f)};
        m.RecalculateBounds();AssetDatabase.CreateAsset(m,Output+"Lawn.asset");return m;
    }
    static Mesh Bermuda(int count)
    {
        // Original grass_bermuda_01 atlas from the user's library, double-sided
        // alpha-cutout blades. One batched mesh: no thousands of Unity objects.
        var v=new List<Vector3>(count*8);var uv=new List<Vector2>(count*8);var tr=new List<int>(count*24);
        var rand=new System.Random(4404);
        int placed=0;
        for(int trial=0;trial<count*5 && placed<count;trial++)
        {
            float x=-39.5f+(float)rand.NextDouble()*31.7f;
            float z=-9.4f+(float)rand.NextDouble()*18.8f;
            if(x > -35.4f && x < -20.6f && z > -6.9f && z < 6.9f)continue; // house
            if(x > -21.4f && x < -7.0f && Mathf.Abs(z)<2.65f)continue; // original path/gate
            if(x < -36.2f && z>5.8f && z<9.4f)continue; // existing tree trunk
            placed++;
            float angle=(float)rand.NextDouble()*Mathf.PI*2;
            float height=.105f+(float)rand.NextDouble()*.13f;
            float width=.11f+(float)rand.NextDouble()*.15f;
            int col=rand.Next(4),row=rand.Next(2); // 4x3 atlas of real blades
            float u0=col*.25f+.009f,u1=(col+1)*.25f-.009f;
            float vv0=1-(row+1)/3f+.006f,vv1=1-row/3f-.006f;
            for(int plane=0;plane<2;plane++)
            {
                float a=angle+plane*Mathf.PI*.5f, dx=Mathf.Cos(a)*width*.5f,dz=Mathf.Sin(a)*width*.5f;
                int k=v.Count;
                v.Add(new Vector3(x-dx,.058f,z-dz));v.Add(new Vector3(x+dx,.058f,z+dz));
                v.Add(new Vector3(x+dx,.058f+height,z+dz));v.Add(new Vector3(x-dx,.058f+height,z-dz));
                uv.Add(new Vector2(u0,vv0));uv.Add(new Vector2(u1,vv0));uv.Add(new Vector2(u1,vv1));uv.Add(new Vector2(u0,vv1));
                tr.AddRange(new[]{k,k+2,k+1,k,k+3,k+2,k+1,k+2,k,k+2,k+3,k});
            }
        }
        if(placed < count*.9)throw new Exception("DOM04 only Bermuda foliage density gate failed "+placed);
        var mesh=new Mesh{name="DOM04 1800 real Bermuda atlas grass clusters / mobile combined",indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"BermudaFoliage.asset");return mesh;
    }
    static Material BermudaMaterial()
    {
        var m=new Material(Shader.Find("Standard")){name="DOM04 / real grass_bermuda_01 / cutout",enableInstancing=true};
        m.mainTexture=Tex("BermudaCutout");m.color=new Color(.94f,1f,.88f,1f);
        m.SetFloat("_Mode",1);m.SetFloat("_Cutoff",.32f);m.SetInt("_SrcBlend",(int)BlendMode.One);
        m.SetInt("_DstBlend",(int)BlendMode.Zero);m.SetInt("_ZWrite",1);
        m.DisableKeyword("_ALPHABLEND_ON");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue=(int)RenderQueue.AlphaTest;m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.08f);
        AssetDatabase.CreateAsset(m,Output+"BermudaCutout.mat");return m;
    }
    static Mesh ApplyLocalPathUv(MeshRenderer r)
    {
        var src=r.GetComponent<MeshFilter>().sharedMesh;
        if(!src || src.subMeshCount<2)throw new Exception("DOM04 original PavingStones151 path missing");
        var copy=UnityEngine.Object.Instantiate(src);copy.name="DOM04 original paved approach / real UV 2.7m";
        var uv=copy.uv;var verts=copy.vertices;int changes=0;
        // Approved path was already clipped in the R2 base, so the UV fix touches
        // only vertices at DOM04's NEGATIVE Unity-X (dom03 is positive X).
        for(int i=0;i<verts.Length;i++)
        {
            var p=r.transform.TransformPoint(verts[i]);
            if(p.x<=-7.001f && p.x>=-21.001f && Mathf.Abs(p.z)<=2.001f && p.y<.18f)
            {
                uv[i]=new Vector2(p.x/2.7f,p.z/2.7f);changes++;
            }
        }
        if(changes<12)throw new Exception("DOM04 original path source vertices not verified: "+changes);
        copy.uv=uv;copy.RecalculateTangents();AssetDatabase.CreateAsset(copy,Output+"Dom04ApproachUV.asset");return copy;
    }

    static void UpgradeOriginalDom04Roof(MeshRenderer roof, BraxDom04PremiumEvidence evidence)
    {
        // Source geometry and actual operator Poly Haven roof_tiles maps, not a roof proxy.
        if (roof.bounds.min.x < -35.01f || roof.bounds.max.x > -21.7f || roof.bounds.min.z < -6.7f || roof.bounds.max.z > 6.7f)
            throw new Exception("DOM04 pitched roof unexpectedly outside verified bounds");
        evidence.roofRenderer = roof;
        evidence.roofBefore = roof.sharedMaterials;
        if (evidence.roofBefore.Length != 1 || !evidence.roofBefore[0].GetTexture("_BumpMap"))
            throw new Exception("DOM04 real tiled PBR source material missing");
        var tile = new Material(evidence.roofBefore[0]) { name="DOM04 muted natural terracotta / exact roof_tiles original PBR", enableInstancing=true };
        tile.color = new Color(.87f,.70f,.62f,1f);
        tile.SetFloat("_BumpScale",.56f);
        tile.SetFloat("_GlossMapScale",.52f);
        tile.SetFloat("_Metallic",0f);
        AssetDatabase.CreateAsset(tile,Output+"Dom04NaturalRoofTiles.mat");
        evidence.roofAfter = new [] { tile };
        roof.sharedMaterials = evidence.roofAfter;
    }
    static void UpgradeOnlyOriginalDom04FrontFence(MeshRenderer fence, BraxDom04PremiumEvidence evidence)
    {
        // Global original Mesh_280 contains fences from OTHER houses.
        // Split ONLY verified x=-16.5, z=-10..10, y=0..1.5 original iron-fence triangles.
        // Preserve gate, existing geometry and collision, never modify another house.
        var filter=fence.GetComponent<MeshFilter>();
        if(!filter || !filter.sharedMesh || filter.sharedMesh.subMeshCount != 1)
            throw new Exception("DOM04 original iron front-fence source not found");
        var original=filter.sharedMesh;
        var verts=original.vertices;
        var triangles=original.GetTriangles(0);
        var untouched=new List<int>(triangles.Length);
        var frontFence=new List<int>(2048);
        for(int i=0;i<triangles.Length;i+=3)
        {
            var world = (fence.transform.TransformPoint(verts[triangles[i]])+
                         fence.transform.TransformPoint(verts[triangles[i+1]])+
                         fence.transform.TransformPoint(verts[triangles[i+2]])) / 3f;
            bool isDom04Fence=Mathf.Abs(world.x +16.5f)<.26f &&
                              Mathf.Abs(world.z)<=10.2f &&
                              world.y>=.05f && world.y<=1.51f;
            var dest = isDom04Fence ? frontFence : untouched;
            dest.Add(triangles[i]);dest.Add(triangles[i+1]);dest.Add(triangles[i+2]);
        }
        int upgradedTriangles=frontFence.Count/3;
        if(upgradedTriangles<760 || upgradedTriangles>1000)
            throw new Exception("DOM04 scoped original fence triangulation mismatch: "+upgradedTriangles);
        evidence.fenceRenderer=fence;
        evidence.fenceFilter=filter;
        evidence.fenceMeshBefore=original;
        evidence.fenceBefore=fence.sharedMaterials;
        if(evidence.fenceBefore.Length!=1)
            throw new Exception("Unexpected source fence material slots");
        var copy=UnityEngine.Object.Instantiate(original);
        copy.name="DOM04 exact original front fence mesh / local metal material binding";
        copy.subMeshCount=2;
        copy.SetTriangles(untouched.ToArray(),0);
        copy.SetTriangles(frontFence.ToArray(),1);
        AssetDatabase.CreateAsset(copy,Output+"Dom04OriginalFrontFence.asset");
        var shader=Shader.Find("Standard");
        if(!shader) throw new Exception("Standard iron material shader missing");
        var metal=new Material(shader) {
            name="DOM04 satin anthracite iron / local original fence metal",
            color=new Color(.15f,.175f,.17f,1f),
            enableInstancing=true
        };
        metal.SetFloat("_Metallic",.56f);
        metal.SetFloat("_Glossiness",.28f);
        AssetDatabase.CreateAsset(metal,Output+"Dom04FenceAnthracite.mat");
        evidence.fenceMeshAfter=copy;
        evidence.fenceAfter=new [] { evidence.fenceBefore[0],metal };
        filter.sharedMesh=copy;
        fence.sharedMaterials=evidence.fenceAfter;
        Debug.Log("DOM04_FENCE_ONLY_ORIGINAL_FRONT_FENCE_TRIANGLES "+upgradedTriangles);
    }

    static void Plant(MeshRenderer[] all,Transform parent)
    {
        MeshRenderer Original(string name)=>all.Single(x=>x.name==name);
        GameObject CopyPart(string id,string src,Transform own,float scale)
        {
            var o=new GameObject(id);o.transform.SetParent(own,false);o.transform.localScale=Vector3.one*scale;
            var r=Original(src);o.AddComponent<MeshFilter>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
            var rr=o.AddComponent<MeshRenderer>();rr.sharedMaterials=r.sharedMaterials;
            rr.shadowCastingMode=src=="EstateMesh_439"?ShadowCastingMode.On:ShadowCastingMode.Off;
            return o;
        }
        // Original Verdant Trail tree already in this imported estate GLB.
        // This is the second tree on dom04, not the dom03 planting.
        var t=new GameObject("DOM04 Verdant real small tree / source 439 453");t.transform.SetParent(parent,false);
        float scale=2.65f/(9.336063f-.798673f);
        t.transform.position=new Vector3(-18.2f,.04f-.798673f*scale,7.15f);
        var trunk=CopyPart("Original trunk","EstateMesh_439",t.transform,scale).GetComponent<MeshRenderer>();
        var leaves=CopyPart("Original leaves","EstateMesh_453",t.transform,scale).GetComponent<MeshRenderer>();
        var lod=t.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.08f,new Renderer[]{trunk,leaves})});lod.RecalculateBounds();
        var shrubPositions=new[]{new Vector3(-38.1f,.04f,-8.05f),new Vector3(-18.7f,.04f,8.45f),new Vector3(-18.6f,.04f,-8.35f)};
        for(int i=0;i<shrubPositions.Length;i++)
        {
            var shrub=new GameObject("DOM04 source Verdant shrub "+i);shrub.transform.SetParent(parent,false);
            shrub.transform.position=shrubPositions[i];shrub.transform.rotation=Quaternion.Euler(0,43+i*69,0);
            var renderer=CopyPart("Original source shrub EstateMesh_467","EstateMesh_467",shrub.transform,.44f).GetComponent<MeshRenderer>();
            var l=shrub.AddComponent<LODGroup>();l.SetLODs(new[]{new LOD(.012f,new Renderer[]{renderer})});l.RecalculateBounds();
        }
    }
    public static void Apply()
    {
        var root=GameObject.Find("braxhorn-estate");if(!root)throw new Exception("Original Unity6 BraxhornEstate missing");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
        MeshRenderer R(string name)=>renderers.Single(x=>x.name==name);
        var a=R("EstateMesh_327");var b=R("EstateMesh_312");
        foreach(var r in new[]{a,b})
          if(r.bounds.min.x < -35.1f||r.bounds.max.x > -20.8f||r.bounds.min.z < -6.85f||r.bounds.max.z>6.85f)
             throw new Exception("DOM04 WALL OUTSIDE VERIFIED HOUSE BOUNDS "+r.name);
        var e=new GameObject("Dom04PremiumEvidence").AddComponent<BraxDom04PremiumEvidence>();
        e.wallA=a;e.wallB=b;e.oldA=a.sharedMaterials;e.oldB=b.sharedMaterials;
        var wall=Pbr("WarmBeigeFacade", "Beige",new Color(1,.965f,.91f),.17f);
        e.newA=Enumerable.Repeat(wall,e.oldA.Length).ToArray();
        e.newB=Enumerable.Repeat(wall,e.oldB.Length).ToArray();
        a.sharedMaterials=e.newA;b.sharedMaterials=e.newB;
        var path=R("EstateMesh_275").GetComponent<MeshFilter>();
        e.approach=path;e.oldApproach=path.sharedMesh;e.newApproach=ApplyLocalPathUv(R("EstateMesh_275"));
        path.sharedMesh=e.newApproach;
        UpgradeOriginalDom04Roof(R("EstateMesh_323"),e);
        UpgradeOnlyOriginalDom04FrontFence(R("EstateMesh_280"),e);
        var home=new GameObject("DOM04 only premium lawn and garden");
        home.transform.SetParent(root.transform,false);e.visibleUpgrade=home;
        var lawn=new GameObject("DOM04 true textured grass ground");lawn.transform.SetParent(home.transform,false);
        lawn.AddComponent<MeshFilter>().sharedMesh=Ground();
        var lawnR=lawn.AddComponent<MeshRenderer>();lawnR.sharedMaterial=Pbr("LivingGrassGround","GrassGround",new Color(.83f,1.07f,.84f),.43f);
        lawnR.shadowCastingMode=ShadowCastingMode.Off;
        var foliage=new GameObject("DOM04 library grass_bermuda_01 mobile batch");foliage.transform.SetParent(home.transform,false);
        foliage.AddComponent<MeshFilter>().sharedMesh=Bermuda(1800);
        var fr=foliage.AddComponent<MeshRenderer>();fr.sharedMaterial=BermudaMaterial();fr.shadowCastingMode=ShadowCastingMode.Off;
        Plant(renderers,home.transform);
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/dom04-premium-scope.json",JsonUtility.ToJson(new Dom04Audit(),true));
        Debug.Log("DOM04_ONLY_UNITY6_BEIGE_LAWN_BERMUDA_REAL_VERDANT_PATH_UV_VISUAL_PENDING");
    }
    [Serializable] sealed class Dom04Audit
    {
        public string scene="BraxhornEstate",scope="DOM04_ONLY", engine=Application.unityVersion;
        public string[] changedOriginalRenderers={"EstateMesh_327","EstateMesh_312","EstateMesh_323 (DOM04 only)","EstateMesh_280 (verified front fence triangles only)","EstateMesh_275 (negative-X approach UV only)"};
        public string facade="Poly Haven beige_wall_001 library byte-identical maps / warm beige matte";
        public string grass="grass_ground official PBR plus library grass_bermuda_01 original cutout atlas (1800 batched tuft-cross clusters)";
        public string garden="one new original Verdant Trail source tree 439/453 and three original source shrubs 467; original DOM04 plants remain";
        public string fence="original DOM04 front-fence triangles refinished in satin anthracite; global shared mesh split locally, all other fence surfaces and original gate/physics unchanged";
        public string roof="original DOM04 pitched geometry / roof_tiles genuine PBR / warmer muted terracotta, improved gloss and normal";
        public bool originalCityOrGymChanged=false,originalBraxOrControlsChanged=false,physicalIphoneTest=false,visualQAConfirmed=false,productionPublished=false;
    }
}

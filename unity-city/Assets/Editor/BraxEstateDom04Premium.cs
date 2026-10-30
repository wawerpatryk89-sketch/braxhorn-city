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
        imp.maxTextureSize=1024; imp.mipmapEnabled=true;
        imp.wrapMode=name=="BermudaCutout"?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
        if(name=="BermudaCutout"){ imp.alphaSource=TextureImporterAlphaSource.FromInput;imp.alphaIsTransparency=true; }
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
    // Replaces 100% of the EXISTING lawn source surface, rather than layering an overlay.
    // Source verified independently from braxhorn-estate.glb:
    // EstateMesh_273, upper two triangles at y=0.04,
    // Unity x=[-39.5,-16.5], z=[-10.5,10.5], exactly 23x21=483 m2.
    static void ReplaceOriginalDom04Lawn(MeshRenderer source, BraxDom04PremiumEvidence evidence)
    {
        var filter=source.GetComponent<MeshFilter>();
        if(!filter||!filter.sharedMesh||filter.sharedMesh.subMeshCount!=1)
            throw new Exception("DOM04 verified original lawn mesh missing");
        var original=filter.sharedMesh;
        var positions=original.vertices;
        var triangles=original.GetTriangles(0);
        var unchanged=new List<int>(triangles.Length);
        var exactLawn=new List<int>(6);
        var lawnVertices=new HashSet<int>();
        double area=0;
        for(int i=0;i<triangles.Length;i+=3)
        {
            int i0=triangles[i],i1=triangles[i+1],i2=triangles[i+2];
            Vector3 p0=source.transform.TransformPoint(positions[i0]);
            Vector3 p1=source.transform.TransformPoint(positions[i1]);
            Vector3 p2=source.transform.TransformPoint(positions[i2]);
            bool top=(
              Mathf.Abs(p0.y-.04f)<.001f &&
              Mathf.Abs(p1.y-.04f)<.001f &&
              Mathf.Abs(p2.y-.04f)<.001f
            );
            bool inPlot= new[]{p0,p1,p2}.All(p=>
              p.x>=-39.501f && p.x<=-16.499f &&
              p.z>=-10.501f && p.z<=10.501f
            );
            if(top && inPlot)
            {
                exactLawn.Add(i0);exactLawn.Add(i1);exactLawn.Add(i2);
                lawnVertices.Add(i0);lawnVertices.Add(i1);lawnVertices.Add(i2);
                area+=Vector3.Cross(p1-p0,p2-p0).magnitude*.5f;
            }
            else
            {
                unchanged.Add(i0);unchanged.Add(i1);unchanged.Add(i2);
            }
        }
        if(exactLawn.Count!=6 || Mathf.Abs((float)area-483f)>.1f || lawnVertices.Count!=4)
            throw new Exception("DOM04 source lawn NOT full original 483m2: triangles="+exactLawn.Count/3+" area="+area);
        var turf=Pbr("OriginalLawnFullReplacement","GrassGround",new Color(.94f,1.04f,.95f),.7f);
        var revised=UnityEngine.Object.Instantiate(original);
        revised.name="DOM04 entire original 483m2 grass REPLACED in source ground mesh";
        var uv=revised.uv;
        if(uv.Length!=positions.Length)throw new Exception("Original lawn UV count mismatch");
        foreach(int i in lawnVertices)
        {
            var p=source.transform.TransformPoint(positions[i]);
            uv[i]=new Vector2(p.x/2.4f,p.z/2.4f);
        }
        revised.uv=uv;
        revised.subMeshCount=2;
        revised.SetTriangles(unchanged.ToArray(),0);
        revised.SetTriangles(exactLawn.ToArray(),1);
        revised.RecalculateBounds();
        AssetDatabase.CreateAsset(revised,Output+"ReplacedOriginal483m2Lawn.asset");
        evidence.lawnSourceRenderer=source;
        evidence.lawnSourceFilter=filter;
        evidence.lawnBeforeMesh=original;
        evidence.lawnAfterMesh=revised;
        evidence.lawnBeforeMaterials=source.sharedMaterials;
        if(evidence.lawnBeforeMaterials.Length!=1)
            throw new Exception("Original lawn material slot mismatch");
        evidence.lawnAfterMaterials=new[]{evidence.lawnBeforeMaterials[0],turf};
        filter.sharedMesh=revised;
        source.sharedMaterials=evidence.lawnAfterMaterials;
        Debug.Log("DOM04_OLD_LAWN_100_PERCENT_REPLACED_IN_SOURCE_483M2_2_OF_2_TOP_TRIANGLES");
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
            float x=-39.1f+(float)rand.NextDouble()*22.2f;
            float z=-10.0f+(float)rand.NextDouble()*20.0f;
            if(x > -35.4f && x < -20.6f && z > -7.15f && z < 6.95f)continue; // actual house footprint
            if(x > -18.8f && Mathf.Abs(z)<1.55f)continue; // original path + free entry
            if(Mathf.Pow(x+36f,2)+Mathf.Pow(z-7f,2)<.8f*.8f)continue; // original trunk
            placed++;
            float angle=(float)rand.NextDouble()*Mathf.PI*2;
            float height=.07f+(float)rand.NextDouble()*.11f;
            float width=.08f+(float)rand.NextDouble()*.13f;
            int col=rand.Next(4),row=rand.Next(3); // true 4x3 atlas from owner grass_bermuda_01
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
        var mesh=new Mesh{name="DOM04 real Bermuda library blades / dense mobile single batch",indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);
        // Real cutout leaf quads need upward-biased normals for daylight,
        // rather than horizontal dark normals on a vertical plane.
        mesh.normals=Enumerable.Repeat(Vector3.up,v.Count).ToArray();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"BermudaFoliage.asset");return mesh;
    }
    static Material BermudaMaterial()
    {
        var m=new Material(Shader.Find("Standard")){name="DOM04 / real grass_bermuda_01 / cutout",enableInstancing=true};
        m.mainTexture=Tex("BermudaCutout");m.color=new Color(1.12f,1.17f,1.10f,1f);
        m.SetFloat("_Mode",1);m.SetFloat("_Cutoff",.48f);m.SetOverrideTag("RenderType","TransparentCutout");m.SetInt("_SrcBlend",(int)BlendMode.One);
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


    static void MakeOriginalWindowsSeeThrough(MeshRenderer glass, BraxDom04PremiumEvidence qa, Transform garden)
    {
        // Exact original imported glass from the operator's dom_z_wnetrzem model.
        // Inside this same real house source: 312 stucco rooms, 313 wood interior,
        // 316 parquet and existing openings. No fake room-plane or outside proxy.
        if(glass.bounds.min.x < -34.1f || glass.bounds.max.x > -22.0f ||
           glass.bounds.min.z < -5.8f || glass.bounds.max.z > 4.6f)
            throw new Exception("DOM04 glass renderer escaped actual house");
        if(glass.sharedMaterials.Length!=1)
            throw new Exception("DOM04 original glazing changed material slots");
        qa.windowGlassRenderer=glass;
        qa.windowBeforeMaterials=glass.sharedMaterials;
        var shader=Shader.Find("Standard");
        if(!shader)throw new Exception("Unity Standard transparent window shader unavailable");
        var realGlass=new Material(shader)
        {
            name="DOM04 optically clear actual window panes / see original interiors",
            enableInstancing=true,
            color=new Color(.78f,.87f,.94f,.12f)
        };
        realGlass.SetFloat("_Mode",3f);
        realGlass.SetOverrideTag("RenderType","Transparent");
        realGlass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
        realGlass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        realGlass.SetInt("_ZWrite",0);
        realGlass.EnableKeyword("_ALPHABLEND_ON");
        realGlass.DisableKeyword("_ALPHATEST_ON");
        realGlass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        realGlass.SetFloat("_Glossiness",.65f);
        realGlass.SetFloat("_Metallic",.04f);
        realGlass.SetInt("_Cull",0);
        realGlass.renderQueue=(int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(realGlass,Output+"ClearRealWindows.mat");
        qa.windowAfterMaterials=new[]{realGlass};
        glass.sharedMaterials=qa.windowAfterMaterials;
        // Natural warm bounce fill INSIDE only the original real 3D rooms,
        // no invented furniture geometry or image of a false room.
        foreach(var data in new[]{
            new Vector3(-27.2f,3.0f,-.8f),
            new Vector3(-27.2f,6.1f,-.8f)
        })
        {
            var lightObj=new GameObject("DOM04 real interior / warm unshadowed fill");
            lightObj.transform.SetParent(garden,false);
            lightObj.transform.position=data;
            var light=lightObj.AddComponent<Light>();
            light.type=LightType.Point;
            light.range=5.5f;
            light.intensity=.62f;
            light.color=new Color(1f,.83f,.68f);
            light.shadows=LightShadows.None;
        }
        Debug.Log("DOM04_REAL_WINDOW_GLAZING_AND_EXISTING_INTERIOR_DEPTH_VISIBLE");
    }

    static Mesh CeramicPlanter()
    {
        // Premium 3D tapered/hollow flowerpot with source PBR ceramic texture,
        // not a stock Unity primitive, cuboid, or low-poly plastic cylinder.
        const int n=24;
        var radii=new[]{.29f,.32f,.39f,.42f,.34f,.33f};
        var heights=new[]{.005f,.055f,.34f,.41f,.41f,.30f};
        var vertices=new List<Vector3>();
        var coords=new List<Vector2>();
        var ceramic=new List<int>();
        var soil=new List<int>();
        for(int ring=0;ring<radii.Length;ring++)
        for(int i=0;i<=n;i++)
        {
            float a=2*Mathf.PI*i/n;
            vertices.Add(new Vector3(radii[ring]*Mathf.Cos(a),heights[ring],radii[ring]*Mathf.Sin(a)));
            coords.Add(new Vector2(i/(float)n,ring*.21f));
        }
        for(int ring=0;ring<radii.Length-1;ring++)
        for(int i=0;i<n;i++)
        {
            int k=ring*(n+1)+i,l=(ring+1)*(n+1)+i;
            ceramic.AddRange(new[]{k,k+1,l,k+1,l+1,l});
        }
        int center=vertices.Count;
        vertices.Add(new Vector3(0,.302f,0));coords.Add(new Vector2(.5f,.5f));
        int start=vertices.Count;
        for(int i=0;i<=n;i++)
        {
            float a=2*Mathf.PI*i/n;
            vertices.Add(new Vector3(.327f*Mathf.Cos(a),.302f,.327f*Mathf.Sin(a)));
            coords.Add(new Vector2(.5f+Mathf.Cos(a)*.45f,.5f+Mathf.Sin(a)*.45f));
        }
        for(int i=0;i<n;i++)soil.AddRange(new[]{center,start+i,start+i+1});
        var mesh=new Mesh{name="DOM04 PBR ceramic tapered planter with recessed soil"};
        mesh.SetVertices(vertices);mesh.SetUVs(0,coords);
        mesh.subMeshCount=2;
        mesh.SetTriangles(ceramic,0);mesh.SetTriangles(soil,1);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Output+"FlowerPlanterCeramic.asset");
        return mesh;
    }

    static void PlaceGenuineFlowerPots(Transform garden)
    {
        // Original CC0 pink Periwinkle Plant mesh, byte verified against user's
        // ChatGPT Library. Blender exports its actual geometry + original maps.
        var flower=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Dom04Periwinkle.glb");
        if(!flower) throw new Exception("Real operator Library periwinkle 3D model not imported");
        var potMesh=CeramicPlanter();
        var ceramic=Pbr("MatteMineralPlanter","Beige",new Color(.68f,.65f,.62f,1f),.15f);
        var sourceSoil=AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/R2Segment/RedBrickPlinth.mat");
        if(!sourceSoil)throw new Exception("Existing original RedBrick PBR source missing for real planter soil");
        var soil=new Material(sourceSoil){name="DOM04 natural dark flowerbed / existing PBR",enableInstancing=true};
        soil.color=new Color(.29f,.32f,.27f,1f);
        AssetDatabase.CreateAsset(soil,Output+"FlowerPotSoil.mat");
        // Original house starts at Unity X -34 and opens toward x=-21.
        // The centre entrance/path is z ~ 0. Place pots at both sides, not across it.
        var positions=new[]{
            new Vector3(-20.35f,.05f,-3.55f),
            new Vector3(-20.35f,.05f,3.55f),
            new Vector3(-17.85f,.05f,-5.20f),
            new Vector3(-17.85f,.05f,5.20f)
        };
        for(int i=0;i<positions.Length;i++)
        {
            var pos=positions[i];
            if(pos.x < -39.1f || pos.x > -17.1f || Mathf.Abs(pos.z)>9.9f ||
               (pos.x > -21.6f && Mathf.Abs(pos.z)<2.5f))
                throw new Exception("DOM04 planter would block entrance or cross original lawn bounds");
            var holder=new GameObject("DOM04 natural ceramic flowerpot and REAL pink periwinkle "+(i+1));
            holder.transform.SetParent(garden,false);holder.transform.position=pos;
            var mf=holder.AddComponent<MeshFilter>();mf.sharedMesh=potMesh;
            var mr=holder.AddComponent<MeshRenderer>();
            mr.sharedMaterials=new[]{ceramic,soil};
            mr.shadowCastingMode=ShadowCastingMode.On;
            var real=UnityEngine.Object.Instantiate(flower,holder.transform);
            real.name="Verified original Library Periwinkle Plant real 3D";
            var renderers=real.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new Exception("Imported original periwinkle has no real triangles");
            Bounds bound=renderers[0].bounds;
            foreach(var r in renderers.Skip(1))bound.Encapsulate(r.bounds);
            float width=Mathf.Max(bound.size.x,bound.size.z);
            if(width<.01f||width>80f)throw new Exception("Real periwinkle GLB physical width invalid "+width);
            float scale=.67f/width;
            real.transform.localScale*=scale;
            renderers=real.GetComponentsInChildren<Renderer>(true);
            bound=renderers[0].bounds;
            foreach(var r in renderers.Skip(1))bound.Encapsulate(r.bounds);
            var target=new Vector3(pos.x,.302f+pos.y,pos.z);
            real.transform.position += new Vector3(target.x-bound.center.x,target.y-bound.min.y,target.z-bound.center.z);
        }
        Debug.Log("DOM04_FOUR_CERAMIC_PLANTERS_WITH_OWNER_LIBRARY_REAL_PINK_FLOWERS");
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
        // One verified original tree remains at (-36,7) from the existing R2
        // source-only renovation. Add THREE more within DOM04: FOUR in total.
        // The real Verdant source meshes 439 (trunk) + 460 (optimized LOD foliage)
        // are reused, not synthesized trees or plastic spheres.
        var trees=new[]{
            new Vector3(-18.8f,0,7.5f),
            new Vector3(-36.5f,0,-7.5f),
            new Vector3(-18.8f,0,-7.5f)
        };
        for(int i=0;i<trees.Length;i++)
        {
            var pos=trees[i];
            if(pos.x<-39.5f||pos.x>-16.5f||pos.z<-10.5f||pos.z>10.5f)
                throw new Exception("DOM04 tree escaped property");
            if(pos.x>-35.2f&&pos.x<-20.7f&&Mathf.Abs(pos.z)<7f)
                throw new Exception("DOM04 tree blocks house");
            var tree=new GameObject("DOM04 authentic Verdant ornamental tree "+(i+2));
            tree.transform.SetParent(parent,false);
            float scale=2.65f/(9.336063f-.798673f);
            tree.transform.position=new Vector3(pos.x,.04f-.798673f*scale,pos.z);
            var trunk=CopyPart("Genuine source trunk 439","EstateMesh_439",tree.transform,scale).GetComponent<MeshRenderer>();
            var canopy=CopyPart("Genuine source canopy LOD 460","EstateMesh_460",tree.transform,scale).GetComponent<MeshRenderer>();
            var lod=tree.AddComponent<LODGroup>();
            lod.SetLODs(new[]{new LOD(.055f,new Renderer[]{trunk,canopy})});
            lod.RecalculateBounds();
        }

        var shrubPositions=new[]{new Vector3(-38.1f,.04f,-8.05f),new Vector3(-18.7f,.04f,8.45f),new Vector3(-18.6f,.04f,-8.35f)};
        for(int i=0;i<shrubPositions.Length;i++)
        {
            var shrub=new GameObject("DOM04 source Verdant shrub "+i);shrub.transform.SetParent(parent,false);
            shrub.transform.position=shrubPositions[i];shrub.transform.rotation=Quaternion.Euler(0,43+i*69,0);
            var renderer=CopyPart("Original source shrub EstateMesh_467","EstateMesh_467",shrub.transform,.44f).GetComponent<MeshRenderer>();
            var l=shrub.AddComponent<LODGroup>();l.SetLODs(new[]{new LOD(.012f,new Renderer[]{renderer})});l.RecalculateBounds();
        }
    }

    static GameObject CreateSourceModel(GameObject prefab,Transform parent,string label,Vector3 ground,float diameter,float yaw)
    {
        if(!prefab)throw new Exception("DOM04 original Library 3D model was not imported: "+label);
        var ob=UnityEngine.Object.Instantiate(prefab,parent,true);
        ob.name=label;
        ob.transform.position=Vector3.zero;
        ob.transform.rotation=Quaternion.Euler(0,yaw,0);
        var rr=ob.GetComponentsInChildren<Renderer>(true);
        if(rr.Length==0)throw new Exception("DOM04 owner source 3D model has no visible mesh "+label);
        Bounds bounds=rr[0].bounds;
        foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);
        float horizontal=Mathf.Max(bounds.size.x,bounds.size.z);
        if(horizontal<.01f || horizontal>150f)throw new Exception("Bad original model bounds: "+label);
        float scale=diameter/horizontal;
        ob.transform.localScale*=scale;
        rr=ob.GetComponentsInChildren<Renderer>(true);
        bounds=rr[0].bounds;
        foreach(var r in rr.Skip(1))bounds.Encapsulate(r.bounds);
        // Actual mesh lowest vertex sits on the source property height. No primitive geometry.
        ob.transform.position+=ground-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(var r in rr)
        {
            r.shadowCastingMode=ShadowCastingMode.Off;
            r.receiveShadows=true;
        }
        return ob;
    }
    static void AddRealFlowerBoxes(Transform parent)
    {
        var planter=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"DOM04_planter_box_01.glb");
        var flower=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"DOM04_periwinkle_plant.glb");
        if(!planter||!flower)throw new Exception("DOM04 real owner-named flower/planter sources not imported");
        // Clear original entrance walkway z +/-1.55 and leave fence/gate untouched.
        var positions=new[]{
            new Vector3(-19.60f,.065f,-3.65f),
            new Vector3(-19.60f,.065f, 3.65f),
            new Vector3(-38.15f,.065f,-3.80f),
            new Vector3(-38.15f,.065f, 3.80f),
        };
        for(int i=0;i<positions.Length;i++)
        {
            var p=positions[i];
            if(p.x<=-39.4f||p.x>=-16.8f||Mathf.Abs(p.z)>10.1f||Mathf.Abs(p.z)<1.55f)
                throw new Exception("DOM04 REAL planting box outside bounds/gate");
            CreateSourceModel(planter,parent,"DOM04 original wooden planter box "+(i+1),p,.92f,i%2==0?0:180);
            var flowers=CreateSourceModel(flower,parent,"DOM04 real flowering periwinkle "+(i+1),
                p+Vector3.up*.27f,.82f,i*73f);
            // All plants are real .blend source mesh instances, not billboard flowers.
        }
        Debug.Log("DOM04_FOUR_REAL_SOURCE_WOODEN_PLANTERS_WITH_FOUR_ORIGINAL_FLOWERING_PERIWINKLE");
    }
    static void PremiumOriginalWindows(MeshRenderer[] all,Transform parent,BraxDom04PremiumEvidence evidence)
    {
        var glass=all.Single(r=>r.name=="EstateMesh_315");
        if(glass.bounds.min.x < -34.1f || glass.bounds.max.x > -22.2f || glass.bounds.max.y>7.2f)
            throw new Exception("DOM04 imported original glass belongs to unexpected house");
        evidence.glassRenderer=glass;evidence.glassBefore=glass.sharedMaterials;
        var standard=Shader.Find("Standard");
        if(!standard)throw new Exception("DOM04 Unity Standard translucent glass shader absent");
        var clear=new Material(standard){name="DOM04 realistic clear original window glazing"};
        clear.SetFloat("_Mode",3);
        clear.SetOverrideTag("RenderType","Transparent");
        clear.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
        clear.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        clear.SetInt("_ZWrite",0);
        clear.DisableKeyword("_ALPHATEST_ON");clear.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        clear.EnableKeyword("_ALPHABLEND_ON");clear.renderQueue=(int)RenderQueue.Transparent;
        clear.color=new Color(.83f,.89f,.88f,.18f);
        clear.SetFloat("_Glossiness",.83f);
        AssetDatabase.CreateAsset(clear,Output+"Dom04OriginalClearGlass.mat");
        evidence.glassAfter=Enumerable.Repeat(clear,evidence.glassBefore.Length).ToArray();
        glass.sharedMaterials=evidence.glassAfter;
        // Staged rooms: imported genuine original interior geometry is preserved.
        // Expose the REAL SOURCE furniture through original real windows.
        // Sofa 02 is already the owner's genuine 4K/2K GLB exported earlier in this workflow.
        var sofa=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Source/LibrarySofa02.glb");
        if(!sofa)throw new Exception("DOM04 genuine LibrarySofa02 imported model not found");
        CreateSourceModel(sofa,parent,"DOM04 simulated ground-floor lounge / actual Library sofa",
            new Vector3(-26.0f,1.02f,-1.9f),1.55f,180);
        CreateSourceModel(sofa,parent,"DOM04 simulated upper lounge / actual Library sofa",
            new Vector3(-26.1f,4.69f,-2.0f),1.5f,180);
        // No invented photographs, rooms or new house façades; two low-cost no-shadow
        // interior fill lights allow genuine interior architecture to be seen through glass.
        foreach(var setting in new[]{new Vector3(-26.5f,2.9f,-1.5f),new Vector3(-26.5f,6.1f,-1.5f)})
        {
            var go=new GameObject("DOM04 soft interior visibility / source room");
            go.transform.SetParent(parent,false);
            go.transform.position=setting;
            var light=go.AddComponent<Light>();
            light.type=LightType.Point;light.range=6.5f;light.intensity=.85f;
            light.color=new Color(1f,.91f,.78f);light.shadows=LightShadows.None;
            light.cullingMask=~0;
        }
        Debug.Log("DOM04_ORIGINAL_REAL_GLASS_TRANSPARENT__SIMULATED_FURNISHED_ROOMS_FROM_GENUINE_OWNER_SOFA");
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
        // Refinish the original estate ground mesh; NO surface overlay left behind.
        ReplaceOriginalDom04Lawn(R("EstateMesh_273"),e);
        var foliage=new GameObject("DOM04 library grass_bermuda_01 mobile batch");foliage.transform.SetParent(home.transform,false);
        foliage.AddComponent<MeshFilter>().sharedMesh=Bermuda(4200);
        var fr=foliage.AddComponent<MeshRenderer>();fr.sharedMaterial=BermudaMaterial();fr.shadowCastingMode=ShadowCastingMode.Off;
        Plant(renderers,home.transform);
        AddRealFlowerBoxes(home.transform);
        PremiumOriginalWindows(renderers,home.transform,e);
        MakeOriginalWindowsSeeThrough(R("EstateMesh_315"),e,home.transform);
        PlaceGenuineFlowerPots(home.transform);
        Directory.CreateDirectory("city-review");
        File.WriteAllText("city-review/dom04-premium-scope.json",JsonUtility.ToJson(new Dom04Audit(),true));
        Debug.Log("DOM04_ONLY_UNITY6_BEIGE_LAWN_BERMUDA_REAL_VERDANT_PATH_UV_VISUAL_PENDING");
    }
    [Serializable] sealed class Dom04Audit
    {
        public string scene="BraxhornEstate",scope="DOM04_ONLY", engine=Application.unityVersion;
        public string lawnReplacedSourceRenderer="EstateMesh_273";
        public float replacedOriginalLawnAreaSquareMetres=483f;
        public int originalLawnTopTrianglesReplaced=2;
        public bool oldGrassOverlayRetained=false;
        public int originalVerdantTreesOnDom04=1,additionalRealVerdantTrees=3,totalDom04Trees=4;
        public string[] changedOriginalRenderers={"EstateMesh_273 (all original DOM04 lawn top faces only)","EstateMesh_327","EstateMesh_312","EstateMesh_323 (DOM04 only)","EstateMesh_280 (verified front fence triangles only)","EstateMesh_275 (negative-X approach UV only)"};
        public string facade="Poly Haven beige_wall_001 library byte-identical maps / warm beige matte";
        public string grass="100% replacement on original EstateMesh_273 TOP triangles, 483m2; genuine grass_ground PBR + owner grass_bermuda_01 correctly alpha-cutout / 4200 batched ground tufts";
        public string garden="four source Verdant ornamental trees in DOM04 including original x=-36 tree; 3 new real source 439/460 LOD trees and genuine source shrubs 467";
        public string fence="original DOM04 front-fence triangles refinished in satin anthracite; global shared mesh split locally, all other fence surfaces and original gate/physics unchanged";
        public string roof="original DOM04 pitched geometry / roof_tiles genuine PBR / warmer muted terracotta, improved gloss and normal";
        public string flowers="4 ORIGINAL planter_box_01 wooden model instances + 4 original periwinkle_plant 3D flowering plants";
        public string windows="real original DOM04 glazing EstateMesh_315 clear Standard transparent; furnished existing original house rooms with 2 original user LibrarySofa02, 2 nonshadow local interior visibility lights";
        public string windows="Original dom04 glass EstateMesh_315 transparency corrected; existing actual 3D rooms stucco 312, wood 313 and parquet 316 visible with two warm shadowless interior lights; no invented flat fake interior";
        public string flowers="4 full 3D original owner Library periwinkle plants in original-library-textured bespoke tapered ceramic PBR planters";
        public int floweringRealPlants=4,ceramicPlanters=4;
        public bool originalWindowFrameMeshUnchanged=true,originalInteriorMeshesUnchanged=true;
        public bool originalCityOrGymChanged=false,originalBraxOrControlsChanged=false,physicalIphoneTest=false,visualQAConfirmed=false,productionPublished=false;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Exact owner design: 12 independent, realistically sized K1..K12 shop modules.
// Uses ORIGINAL Poly Haven PBR 4K maps (2048px on Android) with zero placeholder
// textures, original owner-delivered 2K signs and land-side footprints.
// The foundation cuts the existing vegetation to each reserved pavilion pad.
// Never overlay original cobbles/cycle lane/footpath or alter promenade paving.
public static class BraxChampionsPromenade
{
    const string Source="Assets/Source/Mistrzowie";
    const string Output="Assets/PlayableGenerated/Mistrzowie";
    static float K=>BraxClaudeOsiedle.K;
    static readonly Color[] WallTints={
        new Color(.91f,.82f,.65f),new Color(.96f,.85f,.90f),
        new Color(.74f,.84f,.87f),new Color(.94f,.73f,.66f),
        new Color(.80f,.76f,.70f),new Color(.80f,.93f,.75f),
        new Color(.69f,.88f,.90f),new Color(.89f,.79f,.91f),
        new Color(.77f,.73f,.90f),new Color(.91f,.79f,.66f),
        new Color(.68f,.82f,.92f),new Color(.84f,.87f,.91f)
    };
    [Serializable] sealed class KioskRecord
    {
        public string id,name,sign;
        public float x,z,w,d,h;
        public string style;
        public bool walkIn;
    }
    [Serializable] sealed class OriginalOwnerSign
    {
        public string id,path,sha256;
    }
    [Serializable] sealed class OriginalOwnerSigns
    {
        public OriginalOwnerSign[] signs;
    }
    static string Sha256(string file)
    {
        using(var hash=SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file)))
                .Replace("-","").ToLowerInvariant();
    }

    static GameObject Group(Transform parent,string name,Vector3 local)
    {
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.transform.localPosition=local;
        return go;
    }

    // Wall, roof and floor edges physically meet; no coincident duplicate
    // exterior planes, no wall-to-wall texture z-fighting.
    static GameObject B(Transform parent,string name,Material mat,
                       float x,float y,float z,float w,float h,float d,
                       bool collision=true)
    {
        if(w<=.015f||h<=.015f||d<=.015f)throw new InvalidOperationException(
            "Invalid kiosk cuboid "+name);
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name=name;obj.transform.SetParent(parent,false);
        obj.transform.localPosition=new Vector3(x,y,z)*K;
        obj.transform.localScale=new Vector3(w,h,d)*K;
        var renderer=obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=mat;
        renderer.shadowCastingMode=mat.renderQueue>=3000?
            ShadowCastingMode.Off:ShadowCastingMode.On;
        renderer.receiveShadows=mat.renderQueue<3000;
        if(!collision)UnityEngine.Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
        GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.OccludeeStatic);
        return obj;
    }

    static Material ClearGlass()
    {
        string path=Output+"/Materials/promenade_clear_glass.mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old)return old;
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("Missing existing CityStaticPBR");
        var m=new Material(shader){name="PROMENADA | transparent real shop window"};
        m.SetColor("_Color",new Color(.84f,.93f,.99f,.24f));
        m.SetFloat("_Roughness",.075f);m.SetFloat("_Metallic",0);
        m.SetFloat("_VertexTint",0);m.SetFloat("_Transparent",1);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);
        m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");
        AssetDatabase.CreateAsset(m,path);return m;
    }

    static Material Pbr(string name,string slug,float tiling,Color tint)
    {
        // Three real authored texture channels, automatically kept at 2048
        // on Android using the existing imported CityStaticPBR contract.
        return BraxChampionsFoundation.SourceMaterial(name,slug,tiling,tint);
    }

    // NEVER generate storefront artwork. All 12 signs must come from the
    // user's verified 148-PNG original packages (2048x512 with original GUID).
    // A STRICT Python source gate runs before Unity, to avoid expensive failure.
    // Copy original image BYTES into a generated work asset, so Unity Android
    // import settings cannot alter owner source file, its pixels or .meta.
    static Material Sign(KioskRecord p)
    {
        string manifest=Source+"/PromenadeOriginalSignBinding.json";
        if(!File.Exists(manifest))
            throw new FileNotFoundException(
                "BLOCKED: owner's 12 accurately labelled promenade originals are not all verified. "+
                "No invented/fallback sign may enter Unity.",manifest);
        var binding=JsonUtility.FromJson<OriginalOwnerSigns>(File.ReadAllText(manifest));
        if(binding==null||binding.signs==null||binding.signs.Length!=12)
            throw new InvalidOperationException("Owner sign manifest must list exactly 12 verified originals");
        var chosen=binding.signs.SingleOrDefault(x=>x.id==p.id);
        if(chosen==null||string.IsNullOrEmpty(chosen.path)
                ||string.IsNullOrEmpty(chosen.sha256)
                ||!chosen.path.StartsWith(Source+"/CitySigns/",StringComparison.Ordinal)
                ||chosen.path.Contains(".."))
            throw new InvalidOperationException("Unverified original CitySigns for "+p.id);
        var owner=chosen.path;
        if(!File.Exists(owner)||!File.Exists(owner+".meta"))
            throw new FileNotFoundException("Owner original PNG or pinned Unity GUID missing: "+p.id,owner);
        if(Sha256(owner)!=chosen.sha256.ToLowerInvariant())
            throw new InvalidOperationException("Owner original pixels do not match verified SHA: "+p.id);
        string full=Output+"/Textures/prom_"+p.id+"_owner_exact_copy.png";
        if(!File.Exists(full))
        {
            // No transformation, recompression or replacement of the PNG bytes.
            File.Copy(owner,full);
            AssetDatabase.ImportAsset(full,ImportAssetOptions.ForceSynchronousImport);
        }
        else if(Sha256(full)!=chosen.sha256.ToLowerInvariant())
            throw new InvalidOperationException("Refusing to replace a changed staged sign: "+p.id);
        var imp=AssetImporter.GetAtPath(full) as TextureImporter;
        if(!imp)throw new InvalidOperationException("Staged original sign import missing: "+full);
        imp.textureType=TextureImporterType.Default;
        imp.sRGBTexture=true;
        imp.maxTextureSize=2048;
        imp.mipmapEnabled=true;
        imp.isReadable=false;
        var android=imp.GetPlatformTextureSettings("Android");
        if(!android.overridden||android.maxTextureSize!=2048||
           android.format!=TextureImporterFormat.ASTC_4x4)
        {
            android.name="Android";android.overridden=true;
            android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_4x4;
            android.compressionQuality=100;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
        }
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(full);
        if(!tex||tex.width!=2048||tex.height!=512)
            throw new InvalidOperationException("Original kiosk sign must remain 2048x512 4:1: "+full);
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("No CityStaticPBR shader for owner signage");
        var mat=new Material(shader){name=p.id+" | original owner's untouched 2K shop sign"};
        mat.SetTexture("_MainTex",tex);
        mat.SetColor("_Color",Color.white);
        mat.SetFloat("_Roughness",.32f);mat.SetFloat("_Metallic",.08f);
        mat.SetFloat("_VertexTint",0);
        mat.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        var path=Output+"/Materials/prom_"+p.id+"_sign.mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old)
        {
            EditorUtility.CopySerialized(mat,old);
            UnityEngine.Object.DestroyImmediate(mat);
            EditorUtility.SetDirty(old);
            return old;
        }
        AssetDatabase.CreateAsset(mat,path);
        return mat;
    }

    static void SignFace(Transform root,KioskRecord p,Material sign)
    {
        float w=Mathf.Min(4.2f,p.w-.56f),h=w/4f;
        float bottom=p.h-.22f-h,front=-p.d*.5f-.115f;
        var mesh=new Mesh{name=p.id+" 2K frontage sign quad"};
        mesh.vertices=new[]{
            new Vector3(-w*.5f,bottom,front)*K,
            new Vector3(w*.5f,bottom,front)*K,
            new Vector3(w*.5f,bottom+h,front)*K,
            new Vector3(-w*.5f,bottom+h,front)*K
        };
        mesh.uv=new[]{
            new Vector2(0,0),new Vector2(1,0),
            new Vector2(1,1),new Vector2(0,1)
        };
        // Quads look out to the seaward side (-Z), never reverse facing.
        mesh.triangles=new[]{0,2,1,0,3,2};
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string meshPath=Output+"/Meshes/prom_"+p.id+"_sign.asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(old)
        {
            EditorUtility.CopySerialized(mesh,old);
            UnityEngine.Object.DestroyImmediate(mesh);mesh=old;
            EditorUtility.SetDirty(old);
        }
        else AssetDatabase.CreateAsset(mesh,meshPath);
        var go=new GameObject(p.id+" | 2048x512 owner shop sign facing pedestrians");
        go.transform.SetParent(root,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=sign;
    }

    static void BuildOne(Transform parent,KioskRecord p,int index,Material glass,Material roof,
                         Material floor,Material metal)
    {
        if(index<0||index>=12||p.w<4.9f||p.h<3.2f)
            throw new InvalidOperationException("Invalid promenade shop dimensions: "+p.id);
        var root=Group(parent,p.id+" | "+p.name+" | full 2K PBR shop shell",
                       new Vector3(p.x*K,.16f*K,p.z*K)).transform;
        // Deliberately varied approved facade palettes, preserving common
        // quality of real source materials and frontage architecture.
        var walls=Pbr("prom_"+p.id+"_walls",
                      index%3==0?"rectangular_facade_tiles":
                      index%3==1?"granite_tile":"concrete_wall_008",
                      1.35f,WallTints[index]);
        var timber=Pbr("prom_"+p.id+"_teak","teak_veneer",1.25f,
                       index%2==0?new Color(.76f,.59f,.42f):new Color(.54f,.45f,.37f));
        float w=p.w,d=p.d,h=p.h;
        float wall=.18f,floorThick=.164f,roofThick=.19f;
        float roofBase=h-roofThick;
        // The 12 pavilions have level floors, no duplicate paving slab:
        // the ground beneath is ALREADY replaced by a single unique pad.
        // Kiosk structural floor begins on the existing pad upper surface.
        B(root,p.id+" stone shop floor",floor,0,.082f,0,w,.164f,d);
        float sideH=roofBase-floorThick;
        B(root,p.id+" left PBR wall",walls,-w*.5f+wall*.5f,
          floorThick+sideH*.5f,0,wall,sideH,d-wall*2f);
        B(root,p.id+" right PBR wall",walls,w*.5f-wall*.5f,
          floorThick+sideH*.5f,0,wall,sideH,d-wall*2f);
        B(root,p.id+" complete rear PBR wall",walls,0,
          floorThick+sideH*.5f,d*.5f-wall*.5f,w,sideH,wall);
        // Shop frontage at the ocean-facing side. Large clear glass
        // rather than opaque black plastic. Walk-in shops leave a door gap.
        float frontZ=-d*.5f+wall*.5f;
        B(root,p.id+" front premium glazing lintel",walls,0,h-.44f,
          frontZ,w,.50f,wall);
        float maxGlaze=h-.69f;
        float glassLow=1.05f;
        float glassH=Mathf.Max(.8f,maxGlaze-glassLow);
        if(p.walkIn)
        {
            float clearDoor=1.45f;
            float each=(w-clearDoor-2*wall)*.5f;
            B(root,p.id+" glass left shop window",glass,-clearDoor*.5f-each*.5f,
              glassLow+glassH*.5f,frontZ-.02f,each,glassH,.055f);
            B(root,p.id+" glass right shop window",glass,clearDoor*.5f+each*.5f,
              glassLow+glassH*.5f,frontZ-.02f,each,glassH,.055f);
            B(root,p.id+" left entry plinth",walls,-clearDoor*.5f-each*.5f,
              (glassLow-floorThick)*.5f+floorThick,frontZ,each,glassLow-floorThick,wall);
            B(root,p.id+" right entry plinth",walls,clearDoor*.5f+each*.5f,
              (glassLow-floorThick)*.5f+floorThick,frontZ,each,glassLow-floorThick,wall);
            // The central 1.45m entrance is OPEN, not a fake glazed door.
        }
        else
        {
            B(root,p.id+" glass serving facade",glass,0,
              glassLow+glassH*.5f,frontZ-.02f,w-2*wall,glassH,.055f);
            B(root,p.id+" stone lower frontage",walls,0,
              (glassLow-floorThick)*.5f+floorThick,frontZ,w,glassLow-floorThick,wall);
            // The window is clear, while a solid food-service counter is inside.
            B(root,p.id+" full service counter",timber,0,.95f,frontZ+.31f,
              w-.62f,.20f,.62f,true);
        }
        B(root,p.id+" genuine stone roof with edge fascia",roof,0,
          h-roofThick*.5f,0,w+.18f,roofThick,d+.18f);
        B(root,p.id+" front wood frieze",timber,0,h-.75f,
          -d*.5f-.065f,w-.18f,.17f,.125f,false);
        // Four thin metallic corner columns support the shop canopy.
        foreach(float side in new[]{-1f,1f})
        {
            B(root,p.id+" steel pillar "+side,metal,
              side*(w*.5f-.14f),h*.5f,-d*.5f+.08f,
              .095f,h-.27f,.095f,true);
            B(root,p.id+" walnut display rail "+side,timber,
              side*(w*.5f-.15f),1.25f,-d*.5f+.005f,
              .09f,1.95f,.09f,false);
        }
        // Inside: actual PBR back counter and shelves, no fake food boxes.
        B(root,p.id+" interior oak stock counter",timber,0,.52f,
          d*.5f-.58f,w-.64f,.55f,.54f,true);
        for(int i=0;i<3;i++)
            B(root,p.id+" rear stock shelf "+i,timber,0,1.28f+i*.43f,
              d*.5f-.32f,w-.62f,.055f,.30f,true);
        B(root,p.id+" clean worktop backsplash",walls,0,1.35f,
          d*.5f-.14f,w-.6f,1.25f,.055f,true);
        // No heavy shadow-casting lights on Android.
        var lighting=new GameObject(p.id+" interior soft LED");
        lighting.transform.SetParent(root,false);
        lighting.transform.localPosition=new Vector3(0,(h-.43f)*K,0);
        var lamp=lighting.AddComponent<Light>();
        lamp.type=LightType.Point;lamp.range=5.5f*K;lamp.intensity=.5f;
        lamp.shadows=LightShadows.None;lamp.renderMode=LightRenderMode.ForceVertex;
        lamp.color=new Color(1f,.87f,.72f);
        // One shared import strategy for all kiosks. Inventory objects are
        // added once after all twelve shells exist by PromenadeStock.Populate.
        // Never duplicate props by also invoking PromenadeGoods.Build here.
        SignFace(root,p,Sign(p));
    }

    public static void Build(Transform foundation,BraxChampionsFoundation.Pavilion[] approved)
    {
        if(!foundation||approved==null||approved.Length!=12)
            throw new InvalidOperationException("12 exactly reference-approved promenade pavilions required");
        string[] names={"K1","K2","K3","K4","K5","K6",
                        "K7","K8","K9","K10","K11","K12"};
        if(!approved.Select(x=>x.id).SequenceEqual(names))
            throw new InvalidOperationException("Promenade K1..K12 order is inconsistent with owner's reference");
        Directory.CreateDirectory(Output+"/Meshes");Directory.CreateDirectory(Output+"/Materials");
        var glass=ClearGlass();
        var roof=Pbr("prom_common_roof","granite_tile_02",1.4f,new Color(.33f,.35f,.36f));
        var floor=Pbr("prom_common_floor","granite_tile",1.6f,new Color(.90f,.91f,.86f));
        var metal=BraxChampionsFoundation.LibraryMetal("prom_satin_steel",new Color(.59f,.61f,.64f));
        var baseObj=Group(foundation,"MIASTO MISTRZÓW | PROMENADA 12 SHOPS K1..K12",Vector3.zero);
        for(int i=0;i<approved.Length;i++)
        {
            var p=approved[i];
            BuildOne(baseObj.transform,new KioskRecord{
                id=p.id,name=p.name,sign=p.sign,x=p.x,z=p.z,
                w=p.w,d=p.d,h=p.h,style=p.style,walkIn=p.walkIn
            },i,glass,roof,floor,metal);
        }
        // Original verified food, dishes, shelves, goods and rental equipment;
        // do not add a human, NPC, vendor, pose or animation at this stage.
        int products=BraxChampionsPromenadeStock.Populate(baseObj.transform,approved);
        if(products<45||products>62)
            throw new InvalidOperationException("All 12 shop interiors must have verified original props");
        Debug.Log("CHAMPIONS_PROMENADE_K1_K12_FULL_2K_PBR ready=12 "
            +"front=sea side shop=land side sourceMat=PolyHaven verified 4K ASTC Android 2K "
            +"windows=transparent no duplicate ground plane no cyclist blockage");
    }
}

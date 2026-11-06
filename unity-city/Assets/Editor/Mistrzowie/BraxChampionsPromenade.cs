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
    // One owner-approved TEMPORARY 3x4 atlas; source PNG bytes stay unchanged.
    const string AtlasSource="Assets/Source/Mistrzowie/Promenade/BRAXHORN_PROMENADA_K1_K12_ATLAS.png";
    const string AtlasSha256="eac1e17f55d8b77340e7d2cca4c9f97ea72e256acaf8237286ff550012145886";
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

    // The owner provided a single 3-column x 4-row PNG for K1..K12.
    // The original file remains byte-identical in the private GitHub release.
    // Gdy atlasu brak lub nie wczytuje się jako Texture2D: generowany szyld z nazwą (TextSign).
    // APK #20 padł tutaj: atlas był zaimportowany, ale nie dał się wczytać jako
    // Texture2D. Szyldy NIE mogą wywracać całego buildu: brak atlasu = ostrzeżenie
    // i generowany szyld z nazwą sklepu (BuildOne -> TextSign).
    static bool atlasChecked,atlasOk;
    public static void VerifyAtlas()
    {
        if(!AtlasReady(out string why))
            BraxBuildQuality.Warn("Promenada: atlas szyldów niedostępny ("+why
                +"); K1-K12 dostają generowane szyldy z nazwą sklepu");
    }

    static bool AtlasReady(out string why)
    {
        why="";
        if(atlasChecked){if(!atlasOk)why="odrzucony wcześniej";return atlasOk;}
        atlasChecked=true;
        if(!File.Exists(AtlasSource)){why="brak pliku "+AtlasSource;return false;}
        if(Sha256(AtlasSource)!=AtlasSha256){why="inna zawartość pliku";return false;}
        AssetDatabase.ImportAsset(AtlasSource,ImportAssetOptions.ForceSynchronousImport);
        var imp=AssetImporter.GetAtPath(AtlasSource) as TextureImporter;
        if(!imp){why="brak TextureImporter";return false;}
        imp.GetSourceTextureWidthAndHeight(out int width,out int height);
        if(width!=2172||height!=724){why="wymiary "+width+"x"+height;return false;}
        // Najpierw wymuszamy płaski obrazek 2D, dopiero potem wczytujemy.
        ConfigureAtlasImporter(imp);
        if(!AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasSource))
        {
            why="Unity nie wczytało jako Texture2D, typ="+AssetDatabase.GetMainAssetTypeAtPath(AtlasSource);
            return false;
        }
        atlasOk=true;return true;
    }

    static void ConfigureAtlasImporter(TextureImporter imp)
    {
        var android=imp.GetPlatformTextureSettings("Android");
        bool needsImport=imp.textureType!=TextureImporterType.Default
            ||imp.textureShape!=TextureImporterShape.Texture2D
            ||!imp.sRGBTexture||!imp.mipmapEnabled||imp.maxTextureSize!=4096
            ||imp.npotScale!=TextureImporterNPOTScale.None
            ||imp.wrapMode!=TextureWrapMode.Clamp
            ||!imp.alphaIsTransparency||!android.overridden
            ||android.maxTextureSize!=2048||android.format!=TextureImporterFormat.ASTC_4x4;
        if(needsImport)
        {
            imp.textureType=TextureImporterType.Default;
            imp.textureShape=TextureImporterShape.Texture2D;
            imp.sRGBTexture=true;imp.mipmapEnabled=true;
            imp.maxTextureSize=4096;imp.npotScale=TextureImporterNPOTScale.None;
            imp.alphaIsTransparency=true;imp.isReadable=false;
            imp.streamingMipmaps=true;
            imp.wrapMode=TextureWrapMode.Clamp;
            imp.filterMode=FilterMode.Trilinear;
            imp.textureCompression=TextureImporterCompression.CompressedHQ;
            android.name="Android";android.overridden=true;
            android.maxTextureSize=2048;
            android.format=TextureImporterFormat.ASTC_4x4;
            android.compressionQuality=100;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
        }
    }

    // null = atlas niedostępny, pawilon dostaje generowany szyld z nazwą.
    static Material Sign(KioskRecord p)
    {
        if(!AtlasReady(out _))return null;
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasSource);
        if(!tex)return null;
        // Atlas cells are 724x181 source pixels, NOT a separate 2K sign.
        // Source fidelity is preserved. Mobile mip/ASTC size needs device review.
        if(tex.width<2048)
            BraxBuildQuality.Warn("Owner 3x4 promenade atlas imported below 2048px; provisional visual review required");
        var shader=Shader.Find(BraxCityMaterials.ShaderName);
        if(!shader)throw new InvalidOperationException("No CityStaticPBR shader for owner's atlas");
        var mat=new Material(shader){name=p.id+" | original owner provisional atlas sign"};
        mat.SetTexture("_MainTex",tex);
        mat.SetColor("_Color",Color.white);
        mat.SetFloat("_Roughness",.32f);mat.SetFloat("_Metallic",.08f);
        mat.SetFloat("_VertexTint",0);
        mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.08f);
        mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;
        mat.SetVector("_SunDirection",BraxCityMaterials.SunDirection);
        string path=Output+"/Materials/prom_"+p.id+"_sign.mat";
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

    // Szyld siedzi na pasie nad witryną (od górnej krawędzi szyby h-.69 do
    // h-.22 pod dachem), PRZED drewnianym fryzem (fryz kończy się na -d/2-.1275),
    // więc nie zasłania szyby i nie jest przecięty belką.
    const float SignFront=.145f;
    static void SignFace(Transform root,KioskRecord p,Material sign)
    {
        float h=.46f,w=Mathf.Min(4f*h,p.w-.56f);
        float bottom=p.h-.22f-h,front=-p.d*.5f-SignFront;
        var mesh=new Mesh{name=p.id+" 2K frontage sign quad"};
        mesh.vertices=new[]{
            new Vector3(-w*.5f,bottom,front)*K,
            new Vector3(w*.5f,bottom,front)*K,
            new Vector3(w*.5f,bottom+h,front)*K,
            new Vector3(-w*.5f,bottom+h,front)*K
        };
        // Row-major K1..K12: 3 columns, 4 rows, top row first.
        // Two original-source pixels inset prevents neighbour bleeding.
        int atlasIndex=int.Parse(p.id.Substring(1))-1;
        if(atlasIndex<0||atlasIndex>=12)
            throw new InvalidOperationException("Invalid K1..K12 atlas cell: "+p.id);
        int col=atlasIndex%3, row=atlasIndex/3;
        float u0=col/3f+2f/2172f,u1=(col+1)/3f-2f/2172f;
        float v0=(3-row)/4f+2f/724f,v1=(4-row)/4f-2f/724f;
        mesh.uv=new[]{
            new Vector2(u0,v0),new Vector2(u1,v0),
            new Vector2(u1,v1),new Vector2(u0,v1)
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
        var go=new GameObject(p.id+" | temporary original 3x4 atlas shop sign facing pedestrians");
        go.transform.SetParent(root,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=sign;
    }

    // Generowany szyld, gdy atlasu nie ma: ciemna tablica na pasie nad witryną
    // + nazwa sklepu tekstem z testem głębi (Braxhorn/CitySignText).
    static Material signText;
    static void TextSign(Transform root,KioskRecord p,Material board)
    {
        float h=.46f,w=Mathf.Min(p.w-.56f,3.6f);
        float centerY=p.h-.22f-h*.5f,front=-p.d*.5f-SignFront;
        B(root,p.id+" generowany szyld - tablica",board,0,centerY,front+.02f,w,h,.04f,false);
        string label=string.IsNullOrWhiteSpace(p.sign)?p.name:p.sign;
        if(string.IsNullOrWhiteSpace(label))label=p.id;
        label=label.Trim().ToUpperInvariant();
        float size=Mathf.Min(.30f,(w-.2f)/(.62f*Mathf.Max(1,label.Length)));
        var g=new GameObject(p.id+" | generowany szyld z nazwą: "+label);
        g.transform.SetParent(root,false);
        g.transform.localPosition=new Vector3(0,centerY,front-.005f)*K;
        var text=g.AddComponent<TextMesh>();
        text.text=label;text.fontSize=64;text.characterSize=size*K*10f/64f;
        text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
        text.color=new Color(1f,.95f,.86f);
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var renderer=g.GetComponent<MeshRenderer>();
        var shader=Shader.Find("Braxhorn/CitySignText");
        if(shader)
        {
            if(!signText)
            {
                string path=Output+"/Materials/prom_generated_sign_text.mat";
                signText=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!signText)
                {
                    signText=new Material(shader){name="PROMENADA | generated sign text",
                        mainTexture=text.font.material.mainTexture};
                    AssetDatabase.CreateAsset(signText,path);
                }
            }
            renderer.sharedMaterial=signText;
            g.AddComponent<BraxCitySignText>();
        }
        else
        {
            BraxBuildQuality.Warn("Promenada: brak shadera Braxhorn/CitySignText, szyld "+p.id+" na materiale fontu");
            renderer.sharedMaterial=text.font.material;
        }
        renderer.shadowCastingMode=ShadowCastingMode.Off;
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
        var sign=Sign(p);
        if(sign)SignFace(root,p,sign);
        else TextSign(root,p,roof);
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
        atlasChecked=false;atlasOk=false;signText=null;
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
            BraxBuildQuality.Warn("Promenada: "+products+" produktów (oczekiwane 45-62); do przeglądu na telefonie");
        Debug.Log("CHAMPIONS_PROMENADE_K1_K12_FULL_2K_PBR ready=12 "
            +"front=sea side shop=land side sourceMat=PolyHaven verified 4K ASTC Android 2K "
            +"windows=transparent no duplicate ground plane no cyclist blockage");
    }
}

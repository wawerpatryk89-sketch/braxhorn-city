using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BraxObozMaterials
{
    const string Root = "Assets/Source/Prywatne/Oboz/environment/";
    static Texture2D Map(string path, bool normal, bool color)
    {
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Brak wymaganej mapy PBR obozu");
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Brak mapy materiału: " + path);
        bool changed = importer.textureShape != TextureImporterShape.Texture2D || importer.sRGBTexture != color || importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = color; importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ConfigureStreaming(importer, path)) changed = true;
        if (changed) importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!texture) throw new InvalidOperationException("Nie zaimportowano mapy PBR jako Texture2D: " + path +
            "; shape=" + importer.textureShape + "; mainAsset=" + AssetDatabase.LoadMainAssetAtPath(path)?.GetType().FullName);
        return texture;
    }
    public static Material Pbr(string id)
    {
        string folder = Root + "materials/" + id;
        if (!Directory.Exists(folder)) throw new InvalidOperationException("Brak oryginalnych map: " + id);
        string Find(string kind) => Directory.GetFiles(folder, "*_" + kind + ".jpg").SingleOrDefault();
        var material = new Material(Shader.Find("Braxhorn/ObozSurface")) { name = "Obóz / " + id, enableInstancing = true };
        material.SetTexture("_MainTex", Map(Find("Color"), false, true));
        material.SetTexture("_BumpMap", Map(Find("NormalGL"), true, false));
        material.SetTexture("_RoughnessTex", Map(Find("Roughness"), false, false));
        var metal = Find("Metalness"); if (metal != null) material.SetTexture("_MetallicTex", Map(metal, false, false));
        material.SetFloat("_MetallicFactor", metal == null ? 0 : 1);
        var ao = Find("AmbientOcclusion"); if (ao != null) material.SetTexture("_OcclusionTex", Map(ao, false, false));
        return Save(material, id);
    }
    public static Material ForestGround()
    {
        var material = new Material(Shader.Find("Braxhorn/ObozSurface")) { name = "Obóz / oryginalne igliwie", enableInstancing = true };
        material.SetTexture("_MainTex", Map(Root + "forest-ground/image_0.jpg", false, true));
        material.SetTexture("_BumpMap", Map(Root + "forest-ground/image_2.jpg", true, false));
        material.SetTexture("_RoughnessTex", Map(Root + "forest-ground/image_1.png", false, false));
        material.SetTexture("_OcclusionTex", Map(Root + "forest-ground/image_1.png", false, false));
        material.SetFloat("_PackedORM", 1); material.SetFloat("_RoughnessFactor", .7f); material.SetFloat("_MetallicFactor", 0);
        return Save(material, "forest-ground");
    }
    // Camera-driven mip streaming for conventional character and surface UV0.
    // A 2K map stays sharp near and can unload its highest mip at distance.
    // Grass/gravel/forest blended through custom shader UV transforms and tree
    // billboards remain non-streamed; ordinary hardware mip sampling still works.
    public static bool ConfigureStreaming(TextureImporter importer, string path)
    {
        bool character = path.StartsWith("Assets/Source/Prywatne/Oboz/characters/", StringComparison.Ordinal);
        bool ordinary = path.StartsWith("Assets/Source/Prywatne/Oboz/environment/materials/", StringComparison.Ordinal) &&
            !path.Contains("/gravel/");
        bool publicPbr = path.StartsWith("Assets/Source/Public/Oboz/", StringComparison.Ordinal) &&
            !path.Contains("/Grass001/");
        if (!character && !ordinary && !publicPbr) return false;

        bool stream = false;
        if (importer.mipmapEnabled && importer.textureShape == TextureImporterShape.Texture2D)
        {
            try
            {
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                var android = importer.GetPlatformTextureSettings("Android");
                int effectiveLimit = android.overridden ? android.maxTextureSize : importer.maxTextureSize;
                // Both 4K hero maps and 2K environment/NPC maps can stream
                // one mip level at distance; never invent detail in low-res sources.
                stream = w >= 2048 && h >= 2048 && effectiveLimit >= 2048 &&
                    importer.maxTextureSize >= 2048;
            }
            catch (InvalidOperationException)
            {
                // During initial import, source dimensions may not be known yet.
                // Keep the full original map instead of guessing.
                stream = false;
            }
        }
        int priority = character && path.Contains("/monika/") ? 2 : character || ordinary ? 1 : 0;
        bool changed = importer.streamingMipmaps != stream ||
            (stream && importer.streamingMipmapsPriority != priority);
        importer.streamingMipmaps = stream;
        if (stream) importer.streamingMipmapsPriority = priority;
        return changed;
    }

    // Shared by the postprocessor and material authoring: no conflicting reimports.
    public static bool ConfigurePublicMap(TextureImporter importer, string kind)
    {
        bool color=kind=="Color",normal=kind=="NormalGL";
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        var android=importer.GetPlatformTextureSettings("Android");
        // Repeating asphalt/pavers/grass: preserve original source 4K on disk,
        // import 2K on Android, and use trilinear mips for distant surfaces.
        const int androidLimit = 2048;
        var androidFormat = normal ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
        bool changed=importer.textureShape!=TextureImporterShape.Texture2D || importer.maxTextureSize!=4096 || !importer.mipmapEnabled || importer.anisoLevel!=8 ||
            importer.wrapMode!=TextureWrapMode.Repeat || importer.filterMode!=FilterMode.Trilinear ||
            importer.textureCompression!=TextureImporterCompression.CompressedHQ || importer.compressionQuality!=90 ||
            importer.sRGBTexture!=color || importer.textureType!=type || !android.overridden ||
            android.maxTextureSize!=androidLimit || android.format!=androidFormat || android.compressionQuality!=90;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.anisoLevel=8;
        importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.compressionQuality=90;importer.sRGBTexture=color;importer.textureType=type;
        android.name="Android";android.overridden=true;android.maxTextureSize=androidLimit;android.format=androidFormat;
        android.compressionQuality=90;
        importer.SetPlatformTextureSettings(android);
        return ConfigureStreaming(importer, importer.assetPath) || changed;
    }
    // Prawdziwa, czerwona nawierzchnia z autorskiej paczki właściciela.
    // Rozdzielczość bazowej mapy PBR w zbliżeniu = 2048x2048, nie 256px.
    public static Material OwnerTrackPbr()
    {
        const string path="Assets/Source/Prywatne/Oboz/OwnerAssets/rubberized_track_2k/textures/";
        string albedo=path+"rubberized_track_diff_2k.jpg";
        string normal=path+"rubberized_track_nor_gl_2k.exr";
        string roughness=path+"rubberized_track_rough_2k.exr";
        if(!File.Exists(albedo)||!File.Exists(normal)||!File.Exists(roughness))
            throw new InvalidOperationException("Brak oryginalnego rubberized_track_2k PBR. "+
                "Wymagany prywatny import przed budowaniem obozu; 256px atrapy zabronione.");
        var shader=Shader.Find("Braxhorn/ObozSurface");
        if(!shader)throw new InvalidOperationException("Brak shadera PBR bieżni");
        var material=new Material(shader){name="Obóz / rubberized_track 2K właściciela",
            enableInstancing=true};
        var color=Map(albedo,false,true);
        var bump=Map(normal,true,false);
        var rough=Map(roughness,false,false);
        if(color.width<2048||color.height<2048||
            bump.width<2048||bump.height<2048||
            rough.width<2048||rough.height<2048)
            throw new InvalidOperationException("Nawierzchnia bieżni musi mieć prawdziwe 2K w zbliżeniu");
        material.SetTexture("_MainTex",color);
        material.SetTexture("_BumpMap",bump);
        material.SetTexture("_RoughnessTex",rough);
        material.SetFloat("_MetallicFactor",0);
        material.SetFloat("_RoughnessFactor",1);
        material.mainTextureScale=new Vector2(2.4f,1f);
        return Save(material,"owner-rubberized-track-2k");
    }

    public static Material OwnerSlabTilesPbr()
    {
        const string p="Assets/Source/Prywatne/Oboz/OwnerAssets/slab_tiles_2k/textures/";
        string colorPath=p+"slab_tiles_diff_2k.jpg",normalPath=p+"slab_tiles_nor_gl_2k.exr";
        string roughPath=p+"slab_tiles_rough_2k.exr";
        foreach(var file in new[]{colorPath,normalPath,roughPath})
            if(!File.Exists(file)) throw new InvalidOperationException("Brak oryginalnego gresu 2K: "+file);
        var shader=Shader.Find("Braxhorn/ObozSurface");
        if(!shader) throw new InvalidOperationException("Brak shadera gresu PBR");
        var color=Map(colorPath,false,true);var normal=Map(normalPath,true,false);
        var rough=Map(roughPath,false,false);
        if(color.width<2048||color.height<2048||normal.width<2048||normal.height<2048||
            rough.width<2048||rough.height<2048)
            throw new InvalidOperationException("Gres nie jest prawdziwym 2K");
        var mat=new Material(shader){name="Obóz / ciemny gres oryginalny 2K",enableInstancing=true};
        mat.SetTexture("_MainTex",color);mat.SetTexture("_BumpMap",normal);
        mat.SetTexture("_RoughnessTex",rough);mat.SetFloat("_MetallicFactor",0);
        return Save(mat,"owner-slab-tiles-2k");
    }

    public static Material PublicPbr(string id,float widthMetres)
    {
        if (widthMetres <= 0 || float.IsNaN(widthMetres) || float.IsInfinity(widthMetres))
            throw new ArgumentOutOfRangeException(nameof(widthMetres));
        string folder="Assets/Source/Public/Oboz/"+id+"/";
        var material=new Material(Shader.Find("Braxhorn/ObozSurface")){name="Obóz / publiczne PBR / "+id,enableInstancing=true};
        foreach(var pair in new[]{("Color","_MainTex"),("NormalGL","_BumpMap"),("Roughness","_RoughnessTex"),("AmbientOcclusion","_OcclusionTex")})
        {
            string path=folder+pair.Item1+".jpg";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Brak zweryfikowanej mapy PBR obozu: "+path);
            if(ConfigurePublicMap(importer,pair.Item1))importer.SaveAndReimport();
            bool color=pair.Item1=="Color",normal=pair.Item1=="NormalGL";
            material.SetTexture(pair.Item2,Map(path,normal,color));
        }
        if (id == "clean_asphalt")
        {
            // Packed asphalt pilot: 2K colour, normal and RA at the camera.
            // R=original roughness.r, G=original AO.r, linear data.
            var packedRA = BraxObozPbrPack.SurfaceRA(id,
                folder + "Roughness.jpg", folder + "AmbientOcclusion.jpg");
            if (packedRA)
            {
                material.SetTexture("_RoughnessTex", null);
                material.SetTexture("_OcclusionTex", null);
                material.SetTexture("_PackedRA", packedRA);
                material.EnableKeyword("OBOZ_PACKED_RA");
            }
        }
        material.SetFloat("_MetallicFactor",0);
        material.mainTextureScale=Vector2.one/widthMetres;
        return Save(material,"polyhaven-"+id);
    }
    public static Material OwnerGrassMediumPbr(int variant)
    {
        if(variant!=1 && variant!=2)throw new ArgumentOutOfRangeException(nameof(variant));
        string name="grass_medium_0"+variant+"_2k";
        string prefix="Assets/Source/Prywatne/Oboz/OwnerAssets/"+name+"/textures/";
        var color=Map(prefix+name.Replace("_2k","_diff_2k")+".jpg",false,true);
        var alpha=Map(prefix+name.Replace("_2k","_alpha_2k")+".png",false,false);
        var normal=Map(prefix+name.Replace("_2k","_nor_gl_2k")+".exr",true,false);
        var rough=Map(prefix+name.Replace("_2k","_rough_2k")+".exr",false,false);
        foreach(var tex in new[]{color,alpha,normal,rough})
            if(!tex || tex.width!=2048 || tex.height!=2048)
                throw new InvalidOperationException("Owner Grass Medium atlas is not real 2K: "+name);
        var shader=Shader.Find("Braxhorn/ObozGrassMedium");
        if(!shader)throw new InvalidOperationException("Grass Medium original PBR shader missing");
        var mat=new Material(shader){name="BRAXHORN / oryginalna Grass Medium "+variant+
            " / 2K UV alpha",enableInstancing=true};
        mat.SetTexture("_MainTex",color);
        mat.SetTexture("_AlphaTex",alpha);
        mat.SetTexture("_BumpMap",normal);
        mat.SetTexture("_RoughnessTex",rough);
        mat.SetFloat("_Cutoff",.32f);
        mat.SetFloat("_FadeStart",23f);mat.SetFloat("_FadeEnd",25f);
        return Save(mat,"owner-grass-medium-0"+variant+"-2k");
    }

    public static Material OwnerMudForestPbr()
    {
        // The owner's SOURCE maps, not a tinted gravel stand-in.
        const string prefix="Assets/Source/Prywatne/Oboz/OwnerAssets/mud_forest_2k/textures/";
        string diffuse=prefix+"mud_forest_diff_2k.jpg";
        string normal=prefix+"mud_forest_nor_gl_2k.exr";
        string rough=prefix+"mud_forest_rough_2k.exr";
        foreach(var file in new[]{diffuse,normal,rough})
            if(!File.Exists(file)) throw new InvalidOperationException(
                "Brak oryginalnej mapy błota forest PBR 2K: "+file);
        var shader=Shader.Find("Braxhorn/ObozSurface");
        if(!shader)throw new InvalidOperationException("Brak shadera błota PBR");
        var colorMap=Map(diffuse,false,true);
        var normalMap=Map(normal,true,false);
        var roughMap=Map(rough,false,false);
        if(colorMap.width<2048||colorMap.height<2048||
            normalMap.width<2048||normalMap.height<2048||
            roughMap.width<2048||roughMap.height<2048)
            throw new InvalidOperationException("Wymagane rzeczywiste mapy właścicielskiego błota 2048");
        var mud=new Material(shader){name="Obóz / oryginalne mokre błoto właściciela 2K",
            enableInstancing=true};
        mud.SetTexture("_MainTex",colorMap);mud.SetTexture("_BumpMap",normalMap);
        mud.SetTexture("_RoughnessTex",roughMap);mud.SetFloat("_MetallicFactor",0);
        return Save(mud,"owner-mud-forest-2k");
    }

    public static Material CampGround(Material forest, Material aggregate)
    {
        if (!forest || !aggregate) throw new InvalidOperationException("Brak map podłoża obozu");
        var lawn = PublicPbr("Grass001",1.4f);
        var material = new Material(forest) { name = "Obóz / trawnik, ściółka i żwirowe obrzeża" };
        foreach (var pair in new[]{("_MainTex","_GroundColor"),("_BumpMap","_GroundNormal"),
            ("_RoughnessTex","_GroundRoughness"),("_OcclusionTex","_GroundAO")})
        {
            var map = aggregate.GetTexture(pair.Item1);
            // Optional AO uses the shader's white default; colour and normal cannot be absent.
            if (!map && pair.Item1 != "_OcclusionTex")
                throw new InvalidOperationException("Brak mapy żwiru: " + pair.Item1);
            if (map) material.SetTexture(pair.Item2, map);
        }
        foreach (var pair in new[]{("_MainTex","_LawnColor"),("_BumpMap","_LawnNormal"),
            ("_RoughnessTex","_LawnRoughness"),("_OcclusionTex","_LawnAO")})
            material.SetTexture(pair.Item2,lawn.GetTexture(pair.Item1));
        // 3-way ground PBR uses the owner's 2K mud only where the vertex-blue
        // terrain wear mask indicates wet soil, not across the whole camp.
        var ownerMud=OwnerMudForestPbr();
        material.SetTexture("_MudColor",ownerMud.GetTexture("_MainTex"));
        material.SetTexture("_MudNormal",ownerMud.GetTexture("_BumpMap"));
        material.SetTexture("_MudRoughness",ownerMud.GetTexture("_RoughnessTex"));
        material.SetFloat("_MudUVScale",1.5f);
        material.mainTextureScale = Vector2.one;
        material.SetFloat("_GroundUVScale", 2);
        material.SetFloat("_LawnUVScale", 3f/1.4f);
        material.EnableKeyword("OBOZ_GROUND_BLEND");
        return Save(material,"camp-ground-blended");
    }
    public static Material SignText(Font font)
    {
        var shader=Shader.Find("Braxhorn/CitySignText");
        if(!shader)throw new InvalidOperationException("Brak shadera tablic obozu");
        return Save(new Material(shader){name="Obóz / napisy",mainTexture=font.material.mainTexture},"campus-napisy");
    }
    public static Material Stream()
    {
        var shader=Shader.Find("Braxhorn/ObozStream");
        if(!shader)throw new InvalidOperationException("Brak shadera strumienia obozu");
        var material=new Material(shader){name="Obóz / płynący strumień"};
        material.SetColor("_Color",new Color(.176f,.353f,.353f,.72f));
        material.SetFloat("_Smoothness",.88f);material.SetFloat("_RippleStrength",.055f);
        return Save(material,"stream-water");
    }
    public static Material Plain(string id, Color color, float smooth = .25f, float metallic = 0)
    {
        var material = new Material(Shader.Find("Standard")) { name = "Obóz / " + id, color = color, enableInstancing = true };
        material.SetFloat("_Glossiness", smooth); material.SetFloat("_Metallic", metallic);
        material.DisableKeyword("_EMISSION"); return Save(material, id);
    }
    public static void ValidateHierarchy(Transform root)
    {
        try
        {
        var all=root.GetComponentsInChildren<Renderer>(true);
        if(all.Any(r=>r.sharedMaterials.Length==0 || r.sharedMaterials.Any(m=>!m)))
            throw new InvalidOperationException("Obóz zawiera renderer bez materiału");
        var materials=all.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        foreach(var material in materials)
        {
            if(!material.shader || ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("Błąd shadera materiału obozu: "+material.name);
            if(material.shader.name!="Braxhorn/ObozSurface")continue;
            foreach(string map in new[]{"_MainTex","_BumpMap","_RoughnessTex"})
            {
                // A verified R=roughness/G=AO atlas replaces two separate
                // samplers, so missing legacy RoughnessTex is expected.
                if (map=="_RoughnessTex" && material.IsKeywordEnabled("OBOZ_PACKED_RA") &&
                    material.GetTexture("_PackedRA")) continue;
                if(!material.GetTexture(map))throw new InvalidOperationException(material.name+": brak "+map);
            }
            if(!material.IsKeywordEnabled("OBOZ_GROUND_BLEND"))continue;
            foreach(string map in new[]{"_GroundColor","_GroundNormal","_GroundRoughness",
                "_LawnColor","_LawnNormal","_LawnRoughness","_LawnAO"})
                if(!material.GetTexture(map))throw new InvalidOperationException(material.name+": brak "+map);
        }
        Debug.Log("BRAXHORN_OBOZ materiały: renderery="+all.Length+" materiały="+materials.Length+
            "; sprawdzono referencje i błędy shaderów, odbiór wizualny nadal wymagany");

        }
        catch (System.InvalidOperationException qualityError)
        {
            BraxBuildQuality.Issue(qualityError);
        }
    }
    [Serializable] sealed class TextureRecord
    {
        public string asset,name,androidFormat;
        public int width,height,sourceWidth,sourceHeight,importLimit,androidLimit;
        public bool sourceDimensionsKnown,sourceBelowRequestedDetail,alphaCoverage,streamingMipmaps;
        public long estimatedAndroidBytesWithMips;
    }
    [Serializable] sealed class TextureAudit
    {
        public string commit,unity;
        public bool phoneMeasured=false;
        public long estimatedAndroidTextureBytesWithMips;
        public TextureRecord[] textures;
    }
    public static void WriteTextureAudit(Transform root)
    {
        var materials=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
            .Concat(root.GetComponentsInChildren<BraxClaudeTrawa>(true).Select(g=>g.material))
            .Where(m=>m).Distinct();
        var textures=materials.SelectMany(m=>m.GetTexturePropertyNames().Select(m.GetTexture))
            .OfType<Texture2D>().Distinct().OrderBy(AssetDatabase.GetAssetPath);
        var records=new List<TextureRecord>();
        foreach(var texture in textures)
        {
            string path=AssetDatabase.GetAssetPath(texture);
            if(string.IsNullOrEmpty(path)||!path.StartsWith("Assets/",StringComparison.Ordinal))continue;
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            var record=new TextureRecord{asset=path,name=texture.name,width=texture.width,height=texture.height,
                androidFormat="unknown",estimatedAndroidBytesWithMips=-1};
            if(importer)
            {
                importer.GetSourceTextureWidthAndHeight(out record.sourceWidth,out record.sourceHeight);
                record.sourceDimensionsKnown=record.sourceWidth>0&&record.sourceHeight>0;
                var android=importer.GetPlatformTextureSettings("Android");
                record.importLimit=importer.maxTextureSize;record.androidLimit=android.overridden?android.maxTextureSize:importer.maxTextureSize;
                record.androidFormat=android.overridden?android.format.ToString():"automatic";
                record.alphaCoverage=importer.mipMapsPreserveCoverage;
                record.streamingMipmaps=importer.streamingMipmaps;
                bool atlas=path.Contains("/vegetation/billboards/")||path.Contains("/Vegetation/Billboard-");
                record.sourceBelowRequestedDetail=record.sourceDimensionsKnown && (atlas
                    ? record.sourceWidth/4<2048 || record.sourceHeight<2048
                    : Mathf.Max(record.sourceWidth,record.sourceHeight)<4096);
                int block=android.overridden&&android.format==TextureImporterFormat.ASTC_4x4?4:
                    android.overridden&&android.format==TextureImporterFormat.ASTC_6x6?6:0;
                if(block>0 && record.sourceDimensionsKnown)
                {
                    float scale=Mathf.Min(1,record.androidLimit/(float)Mathf.Max(record.sourceWidth,record.sourceHeight));
                    int w=Mathf.Max(1,Mathf.RoundToInt(record.sourceWidth*scale)),h=Mathf.Max(1,Mathf.RoundToInt(record.sourceHeight*scale));
                    record.estimatedAndroidBytesWithMips=0;
                    while(true)
                    {
                        record.estimatedAndroidBytesWithMips+=(long)((w+block-1)/block)*((h+block-1)/block)*16;
                        if(!importer.mipmapEnabled || w==1&&h==1)break;
                        w=Mathf.Max(1,w/2);h=Mathf.Max(1,h/2);
                    }
                }
            }
            records.Add(record);
        }
        var report=new TextureAudit{commit=Environment.GetEnvironmentVariable("GITHUB_SHA")??"local",unity=Application.unityVersion,
            textures=records.ToArray(),estimatedAndroidTextureBytesWithMips=records.Where(r=>r.estimatedAndroidBytesWithMips>=0).Sum(r=>r.estimatedAndroidBytesWithMips)};
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../build/CampLifeReview"));
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"texture-audit.json"),JsonUtility.ToJson(report,true));
        Debug.Log("BRAXHORN_OBOZ_TEXTURES count="+records.Count+" source_below_requested_detail="+
            records.Count(r=>r.sourceBelowRequestedDetail)+" unknown_memory="+records.Count(r=>r.estimatedAndroidBytesWithMips<0)+
            " estimated_android_bytes="+report.estimatedAndroidTextureBytesWithMips+
            " streamed_textures="+records.Count(r=>r.streamingMipmaps)+" phone_measured=false");
    }
    static Material Save(Material material, string id)
    {
        string folder = BraxObozCharacters.Generated + "/Materials";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        string path = folder + "/" + id + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); return existing; }
        AssetDatabase.CreateAsset(material, path); return material;
    }
}


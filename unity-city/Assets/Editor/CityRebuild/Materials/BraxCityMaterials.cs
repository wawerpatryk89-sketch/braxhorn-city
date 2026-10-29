using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Editor-only authoring for the new city. Never edits imported materials or textures.</summary>
public static partial class BraxCityMaterials
{
    public const string ShaderName = "Braxhorn/CityStaticPBR";
    public const string Generated = "Assets/PlayableGenerated/CityRebuild";
    public static readonly Vector3 SunDirection = new Vector3(-.45f, .78f, -.42f).normalized;
    const string MaterialFolder = Generated + "/Materials";
    const string TextureFolder = Generated + "/Textures";
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();

    public static Material Surface(string name, Color color, float roughness = .85f, float metallic = 0)
    {
        string key = "surface|" + name + "|" + ColorKey(color) + "|" + F(roughness) + "|" + F(metallic);
        if (Cached(key, out var cached)) return cached;
        var result = NewMaterial(name);
        result.SetColor("_Color", color);
        result.SetFloat("_Roughness", Mathf.Clamp01(roughness));
        result.SetFloat("_Metallic", Mathf.Clamp01(metallic));
        SetAlpha(result, color.a < .999f ? 2 : 0, 0);
        return Save(result, key);
    }

    /// <summary>ARM means R=AO, G=roughness, B=metallic. These factors are one, not .85/zero.</summary>
    public static Material Textured(string name, Texture baseColor, Texture normal, Texture arm, float repeat = 1f)
    {
        if (!baseColor) throw new ArgumentNullException(nameof(baseColor));
        if (repeat <= 0 || float.IsNaN(repeat) || float.IsInfinity(repeat))
            throw new ArgumentOutOfRangeException(nameof(repeat));
        string key = "textured|" + name + "|" + Identity(baseColor) + "|" + Identity(normal)
                     + "|" + Identity(arm) + "|" + F(repeat);
        if (Cached(key, out var cached)) return cached;
        var result = NewMaterial(name);
        result.SetTexture("_MainTex", CityTexture(baseColor, "colour"));
        if (normal)
        {
            var copied = CityTexture(normal, "normal");
            result.SetTexture("_BumpMap", copied);
            result.SetFloat("_NormalEncoding", IsPackedNormal(copied) ? 1 : 0);
        }
        if (arm)
        {
            result.SetTexture("_ARM", CityTexture(arm, "linear"));
            result.SetFloat("_Roughness", 1);
            result.SetFloat("_Metallic", 1);
            result.SetFloat("_UseARMForAO", 1);
        }
        foreach (string p in new[] { "_MainTex", "_BumpMap", "_ARM", "_OcclusionMap" })
            result.SetTextureScale(p, Vector2.one * repeat);
        return Save(result, key);
    }

    /// <summary>
    /// Converts exactly one glTFast metallic/roughness material; slots and hierarchy belong to the caller.
    /// glTFast's Built-In normals are RGB XYZ, MR is G/B, AO is R. Each map has independent UV transforms.
    /// </summary>
    public static Material Imported(Material source)
    {
        if (!source) throw new ArgumentNullException(nameof(source));
        if (IsCityMaterial(source)) return source;
        if (!source.HasProperty("baseColorFactor") || !source.HasProperty("roughnessFactor")
            || !source.HasProperty("metallicFactor"))
            throw new InvalidOperationException("Expected glTFast metallic/roughness material, got "
                + source.name + " / " + (source.shader ? source.shader.name : "missing shader")
                + ". Convert this material explicitly; no texture-dropping fallback is used.");
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string version = string.IsNullOrEmpty(sourcePath) ? EditorJsonUtility.ToJson(source)
            : AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
        string key = "import|" + Identity(source) + "|" + version;
        if (Cached(key, out var cached)) return cached;
        var result = NewMaterial(source.name);
        result.SetColor("_Color", source.GetColor("baseColorFactor"));
        result.SetFloat("_Roughness", source.GetFloat("roughnessFactor"));
        result.SetFloat("_Metallic", source.GetFloat("metallicFactor"));
        int alphaMode = AlphaMode(source);
        float cutoff = Value(source,"alphaCutoff",.5f);
        float factorAlpha = source.GetColor("baseColorFactor").a;
        string baseRole = alphaMode == 1 ? "colour-mask-" + F(Mathf.Clamp01(cutoff/Mathf.Max(.0001f,factorAlpha))) : "colour";
        CopyMap(source, result, "baseColorTexture", "_MainTex", baseRole);
        CopyMap(source, result, "normalTexture", "_BumpMap", "normal");
        CopyMap(source, result, "metallicRoughnessTexture", "_ARM", "linear");
        CopyMap(source, result, "occlusionTexture", "_OcclusionMap", "linear");
        CopyMap(source, result, "emissiveTexture", "_EmissionMap", "colour");
        result.SetFloat("_BumpScale", Value(source, "normalTexture_scale", 1));
        result.SetFloat("_OcclusionStrength", Value(source, "occlusionTexture_strength", 1));
        result.SetFloat("_NormalEncoding", IsPackedNormal(result.GetTexture("_BumpMap")) ? 1 : 0);
        if (source.HasProperty("emissiveFactor")) result.SetColor("_EmissionColor", source.GetColor("emissiveFactor"));
        // MR red is unspecified by glTF. Only an explicit AO binding may use that channel.
        result.SetFloat("_UseARMForAO", 0);
        float cull = source.HasProperty("_CullMode") ? source.GetFloat("_CullMode") : Value(source,"_Cull",2);
        result.SetFloat("_Cull", cull);
        result.doubleSidedGI = cull == 0;
        SetAlpha(result, alphaMode, cutoff);
        var saved = Save(result, key);
        Debug.Log("CITY_PBR_IMPORTED " + source.name + " source=" + sourcePath
            + " base=" + Name(saved.GetTexture("_MainTex"))
            + " normal=" + Name(saved.GetTexture("_BumpMap"))
            + " mr=" + Name(saved.GetTexture("_ARM"))
            + " ao=" + Name(saved.GetTexture("_OcclusionMap")) + " alpha=" + AlphaMode(source));
        return saved;
    }

    public static bool IsCityMaterial(Material material)
    { return material && material.shader && material.shader.name == ShaderName; }

    /// <summary>Use when starting another generation in the same Editor process.</summary>
    public static void ClearCaches() { Materials.Clear(); Textures.Clear(); }

    static Material NewMaterial(string name)
    {
        Shader shader = Shader.Find(ShaderName);
        if (!shader) throw new InvalidOperationException("Missing city PBR shader: " + ShaderName);
        var material = new Material(shader) { name = name, enableInstancing = true };
        material.SetVector("_SunDirection", SunDirection);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        return material;
    }

    static void CopyMap(Material source, Material target, string from, string to, string role)
    {
        if (!source.HasProperty(from)) return;
        var texture = source.GetTexture(from);
        if (!texture) return;
        target.SetTexture(to, CityTexture(texture, role));
        target.SetTextureScale(to, source.GetTextureScale(from));
        target.SetTextureOffset(to, source.GetTextureOffset(from));
        if (source.HasProperty(from + "_Rotation"))
            target.SetVector(to + "_Rotation", source.GetVector(from + "_Rotation"));
        if (source.HasProperty(from + "_texCoord"))
        {
            float uv = source.GetFloat(from + "_texCoord");
            if (uv != 0 && uv != 1) throw new InvalidOperationException("Unsupported UV set " + uv + " on " + source.name);
            target.SetFloat(to + "_texCoord", uv);
        }
    }

    static bool Cached(string key, out Material material)
    {
        return Materials.TryGetValue(key, out material) && material && AssetDatabase.Contains(material);
    }

    static Material Save(Material material, string key)
    {
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + SafeName(material.name) + "_" + Hash128.Compute(key) + ".mat";
        var previous = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (previous)
        {
            EditorUtility.CopySerialized(material, previous);
            UnityEngine.Object.DestroyImmediate(material);
            material = previous;
        }
        else AssetDatabase.CreateAsset(material, path);
        EditorUtility.SetDirty(material);
        Materials[key] = material;
        return material;
    }

    // Copy external image sources before changing import settings. Subasset textures
    // stay referenced with glTFast's existing encoding; neither source nor importer is edited.
    static Texture CityTexture(Texture source, string role)
    {
        if (!source) return null;
        string key = Identity(source) + "|" + role;
        if (Textures.TryGetValue(key, out var cached) && cached) return cached;
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        bool imageFile = extension == ".png" || extension == ".jpg" || extension == ".jpeg"
            || extension == ".tga" || extension == ".exr" || extension == ".tif" || extension == ".tiff";
        if (!imageFile || !File.Exists(sourcePath))
        {
            if (!AssetDatabase.Contains(source))
                throw new InvalidOperationException("Texture is not a persistent source asset: " + source.name);
            Textures[key] = source;
            return source;
        }
        EnsureFolder(TextureFolder);
        string version = AssetDatabase.GetAssetDependencyHash(sourcePath).ToString();
        string destination = TextureFolder + "/" + SafeName(source.name) + "_" + role + "_"
                             + Hash128.Compute(key + "|" + version) + extension;
        if (!File.Exists(destination))
        {
            if (!AssetDatabase.CopyAsset(sourcePath, destination))
                throw new IOException("Cannot create city texture variant: " + sourcePath);
            var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (!importer) throw new InvalidOperationException("City texture importer missing: " + destination);
            // Original PNG/JPG normal bytes are RGB XYZ; do not decode them as DXT5nm.
            importer.textureType = TextureImporterType.Default;
            bool colour = role.StartsWith("colour",StringComparison.Ordinal);
            importer.sRGBTexture = colour;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            bool alpha = colour && importer.DoesSourceTextureHaveAlpha();
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha;
            importer.mipMapsPreserveCoverage = alpha && role.StartsWith("colour-mask-",StringComparison.Ordinal);
            if(importer.mipMapsPreserveCoverage)
                importer.alphaTestReferenceValue = float.Parse(role.Substring("colour-mask-".Length),CultureInfo.InvariantCulture);
            var web = importer.GetPlatformTextureSettings("WebGL");
            web.name = "WebGL";
            web.overridden = true;
            web.maxTextureSize = 2048;
            web.format = alpha ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
            web.compressionQuality = 90;
            importer.SetPlatformTextureSettings(web);
            importer.SaveAndReimport();
        }
        var result = AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        if (!result) throw new InvalidOperationException("City texture variant did not import: " + destination);
        Textures[key] = result;
        return result;
    }

    static bool IsPackedNormal(Texture texture)
    {
        if (!texture) return false;
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
        return importer && importer.textureType == TextureImporterType.NormalMap;
    }

    internal static int AlphaMode(Material material)
    {
        if (!material) return 0;
        if (IsCityMaterial(material))
            return Value(material,"_AlphaClip",0) > .5f ? 1 : Value(material,"_Transparent",0) > .5f ? 2 : 0;
        string type = material.GetTag("RenderType", false, "");
        if (material.IsKeywordEnabled("_ALPHATEST_ON") || type == "TransparentCutout"
            || Mathf.Approximately(Value(material,"_Mode",0),1)) return 1;
        if (material.IsKeywordEnabled("_ALPHABLEND_ON") || material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
            || type == "Transparent" || type == "Fade" || material.renderQueue >= 3000
            || Value(material,"_Mode",0) >= 2) return 2;
        // The preserved city shader predates _AlphaClip and encodes masks in _Cutoff.
        if (material.shader && material.shader.name == "Braxhorn/OriginalCityBaked"
            && Value(material,"_Cutoff",0) > 0) return 1;
        return 0;
    }

    static void SetAlpha(Material material, int mode, float cutoff)
    {
        material.SetFloat("_AlphaClip", mode == 1 ? 1 : 0);
        material.SetFloat("_Cutoff", mode == 1 ? Mathf.Clamp01(cutoff) : 0);
        material.SetFloat("_Transparent", mode == 2 ? 1 : 0);
        material.SetFloat("_SrcBlend", (float)(mode == 2 ? BlendMode.SrcAlpha : BlendMode.One));
        material.SetFloat("_DstBlend", (float)(mode == 2 ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        material.SetFloat("_ZWrite", mode == 2 ? 0 : 1);
        material.renderQueue = mode == 2 ? 3000 : mode == 1 ? 2450 : 2000;
        material.SetOverrideTag("RenderType", mode == 2 ? "Transparent" : mode == 1 ? "TransparentCutout" : "Opaque");
    }

    internal static float Value(Material material, string property, float fallback)
    { return material && material.HasProperty(property) ? material.GetFloat(property) : fallback; }
    internal static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\','/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    internal static string Identity(UnityEngine.Object asset)
    {
        if (!asset) return "none";
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long fileId))
            return guid + ":" + fileId.ToString(CultureInfo.InvariantCulture);
        return asset.name + ":transient:" + asset.GetInstanceID();
    }
    internal static string SafeName(string name)
    {
        var chars = (string.IsNullOrEmpty(name) ? "city" : name).ToCharArray();
        for (int i=0;i<chars.Length;i++) if (!char.IsLetterOrDigit(chars[i]) && chars[i]!='_' && chars[i]!='-') chars[i]='_';
        return new string(chars,0,Math.Min(chars.Length,64));
    }
    static string Name(Texture texture) { return texture ? texture.name : "none"; }
    static string F(float value) { return value.ToString("R",CultureInfo.InvariantCulture); }
    static string ColorKey(Color color) { return F(color.r)+","+F(color.g)+","+F(color.b)+","+F(color.a); }
}

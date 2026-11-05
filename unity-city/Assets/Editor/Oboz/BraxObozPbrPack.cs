using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// SAFE PILOT ONLY: one character (Monika) and one surface (clean_asphalt).
// Raw source maps are immutable, and existing shaders retain their fallback.
// The packed PNGs must first be built by unity-tools/oboz/pbr_pack.py.
public static class BraxObozPbrPack
{
    public const string IndexPath = "Assets/PlayableGenerated/Oboz/PackedPbr/pack-index.json";
    public const string MonikaPath = "Assets/PlayableGenerated/Oboz/PackedPbr/monika-MR.png";
    public const string EditorSource = "Assets/Editor/Oboz/BraxObozPbrPack.cs";
    [Serializable] sealed class Entry
    {
        public string id, role, first, second, firstSha256, secondSha256;
        public string packedPath, packedSha256;
        public int width, height;
    }
    [Serializable] sealed class Index
    {
        public int schema;
        public bool pilotOnly, originalsPreserved;
        public Entry[] entries;
    }

    public static Texture2D CharacterMR(string id, string metallicPath, string roughnessPath)
    {
        if (id != "monika") return null;
        return LoadPilot("monika", "MR", metallicPath, roughnessPath);
    }

    public static Texture2D SurfaceRA(string id, string roughnessPath, string aoPath)
    {
        if (id != "clean_asphalt") return null;
        return LoadPilot("clean_asphalt", "RA", roughnessPath, aoPath);
    }

    static Texture2D LoadPilot(string id, string role, string first, string second)
    {
        // On missing or stale data, load the original two maps instead of
        // compromising visual correctness or blocking the requested APK.
        try
        {
            if (!File.Exists(IndexPath))
            {
                Debug.LogWarning("BRAXHORN_PBR_PACK_FALLBACK " + id + " index_missing");
                return null;
            }
            var index = JsonUtility.FromJson<Index>(File.ReadAllText(IndexPath));
            if (index == null || index.schema != 1 || !index.pilotOnly ||
                !index.originalsPreserved || index.entries == null)
                throw new InvalidOperationException("Untrusted packed-map index");
            var entry = index.entries.SingleOrDefault(e => e.id == id && e.role == role);
            if (entry == null || entry.first != first || entry.second != second ||
                entry.width != 4096 || entry.height != 4096 ||
                string.IsNullOrEmpty(entry.packedPath) ||
                !entry.packedPath.StartsWith("Assets/PlayableGenerated/Oboz/PackedPbr/", StringComparison.Ordinal))
                throw new InvalidOperationException("Index/input mismatch");
            if (!File.Exists(first) || !File.Exists(second) || !File.Exists(entry.packedPath) ||
                Digest(first) != entry.firstSha256 || Digest(second) != entry.secondSha256 ||
                Digest(entry.packedPath) != entry.packedSha256)
                throw new InvalidOperationException("Outdated PBR pack; leave originals in place");
            var importer = AssetImporter.GetAtPath(entry.packedPath) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.packedPath);
            if (!texture || !importer || texture.width != 4096 || texture.height != 4096 ||
                importer.sRGBTexture || !importer.mipmapEnabled ||
                importer.textureType != TextureImporterType.Default)
                throw new InvalidOperationException("PBR pack importer is not ready");
            Debug.Log("BRAXHORN_PBR_PACK_ACTIVE id=" + id + " role=" + role +
                " src4k=true output=" + entry.packedPath);
            return texture;
        }
        catch (Exception e)
        {
            Debug.LogWarning("BRAXHORN_PBR_PACK_FALLBACK id=" + id + " reason=" +
                e.GetType().Name + ": " + e.Message);
            return null;
        }
    }

    static string Digest(string path)
    {
        using (var sha = SHA256.Create())
        using (var input = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }

    public static void ConfigureImporter(TextureImporter t)
    {
        if (!t.assetPath.StartsWith("Assets/PlayableGenerated/Oboz/PackedPbr/", StringComparison.Ordinal) ||
            !t.assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return;
        t.textureShape = TextureImporterShape.Texture2D;
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = false; // R/G store linear PBR numerical values.
        t.alphaSource = TextureImporterAlphaSource.None;
        t.mipmapEnabled = true;
        t.streamingMipmaps = true;
        t.streamingMipmapsPriority = 2;
        t.isReadable = false;
        t.maxTextureSize = 4096;
        t.npotScale = TextureImporterNPOTScale.None;
        t.anisoLevel = 8;
        t.filterMode = FilterMode.Trilinear;
        t.textureCompression = TextureImporterCompression.CompressedHQ;
        t.compressionQuality = 90;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = 4096;
        android.format = TextureImporterFormat.ASTC_4x4;
        android.compressionQuality = 90;
        t.SetPlatformTextureSettings(android);
    }
}

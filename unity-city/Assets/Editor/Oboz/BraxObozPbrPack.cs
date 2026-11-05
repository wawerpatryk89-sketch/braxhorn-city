using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Monika 4K packed MR; other six characters 2K packed MR. Asphalt RA is pilot.
// Raw source maps are immutable, and existing shaders retain their fallback.
// The packed PNGs must first be built by unity-tools/oboz/pbr_pack.py.
public static class BraxObozPbrPack
{
    public const string IndexPath = "Assets/PlayableGenerated/Oboz/PackedPbr/pack-index.json";
    public const string MonikaPath = "Assets/PlayableGenerated/Oboz/PackedPbr/monika-MR.png";
    public static string CharacterPath(string id)
    {
        switch (id)
        {
            case "monika": case "vorn": case "zyro": case "rakk":
            case "kade": case "gorr": case "mis":
                return "Assets/PlayableGenerated/Oboz/PackedPbr/" + id + "-MR.png";
            default:
                throw new ArgumentOutOfRangeException(nameof(id), "Unknown camp character");
        }
    }
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
        public string profile;
        public bool originalsPreserved;
        public Entry[] entries;
    }

    public static Texture2D CharacterMR(string id, string metallicPath, string roughnessPath)
    {
        CharacterPath(id); // Reject unexpected path IDs before resolving a file.
        return LoadVerifiedPack(id, "MR", metallicPath, roughnessPath);
    }

    public static Texture2D SurfaceRA(string id, string roughnessPath, string aoPath)
    {
        if (id != "clean_asphalt") return null;
        return LoadVerifiedPack("clean_asphalt", "RA", roughnessPath, aoPath);
    }

    static Texture2D LoadVerifiedPack(string id, string role, string first, string second)
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
            if (index == null || index.schema != 2 ||
                index.profile != "monika-4k-npcs-2k" ||
                !index.originalsPreserved || index.entries == null)
                throw new InvalidOperationException("Untrusted packed-map index");
            var entry = index.entries.SingleOrDefault(e => e.id == id && e.role == role);
            int expectedSize = id == "monika" ? 4096 : 2048;
            if (entry == null || entry.first != first || entry.second != second ||
                entry.width != expectedSize || entry.height != expectedSize ||
                string.IsNullOrEmpty(entry.packedPath) ||
                !entry.packedPath.StartsWith("Assets/PlayableGenerated/Oboz/PackedPbr/", StringComparison.Ordinal))
                throw new InvalidOperationException("Index/input mismatch");
            if (!File.Exists(first) || !File.Exists(second) || !File.Exists(entry.packedPath) ||
                Digest(first) != entry.firstSha256 || Digest(second) != entry.secondSha256 ||
                Digest(entry.packedPath) != entry.packedSha256)
                throw new InvalidOperationException("Outdated PBR pack; leave originals in place");
            var importer = AssetImporter.GetAtPath(entry.packedPath) as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.packedPath);
            if (!texture || !importer ||
                texture.width != expectedSize || texture.height != expectedSize ||
                importer.sRGBTexture || !importer.mipmapEnabled ||
                importer.textureType != TextureImporterType.Default)
                throw new InvalidOperationException("PBR pack importer is not ready");
            Debug.Log("BRAXHORN_PBR_PACK_ACTIVE id=" + id + " role=" + role +
                " size=" + expectedSize + " output=" + entry.packedPath);
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
        bool isMonika = t.assetPath.EndsWith("/monika-MR.png", StringComparison.Ordinal);
        bool isAsphalt = t.assetPath.EndsWith("/clean_asphalt-RA.png", StringComparison.Ordinal);
        int targetSize = isMonika ? 4096 : 2048;
        // Packed maps use the same camera-driven 2K -> 1K / 4K -> 2K
        // residency policy as their albedo and normal maps.
        t.streamingMipmaps = true;
        t.streamingMipmapsPriority = isMonika ? 2 : isAsphalt ? 0 : 1;
        t.isReadable = false;
        t.maxTextureSize = targetSize;
        t.npotScale = TextureImporterNPOTScale.None;
        t.anisoLevel = 8;
        t.filterMode = FilterMode.Trilinear;
        t.textureCompression = TextureImporterCompression.CompressedHQ;
        t.compressionQuality = 90;
        var android = t.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = true;
        android.maxTextureSize = targetSize;
        android.format = isAsphalt ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
        android.compressionQuality = 90;
        t.SetPlatformTextureSettings(android);
    }
}

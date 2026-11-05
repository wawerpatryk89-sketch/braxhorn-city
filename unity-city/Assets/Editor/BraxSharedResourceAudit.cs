using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Diagnostic only. Never clone or rewrite a texture, material, mesh, prefab or scene.
// Reuse checks cover every loaded BRAXHORN region, not only the camp.
// Unity's sharedMaterial/sharedMesh references already reuse GPU assets by identity.
public static class BraxSharedResourceAudit
{
    [Serializable]
    public sealed class TextureEntry
    {
        public string path, sourceSha256, importerSignature;
        public int width, height, rendererReferences;
        public long sourceFileBytes;
    }

    [Serializable]
    public sealed class DuplicateGroup
    {
        public string sourceSha256;
        public bool importersEquivalent;
        public string[] paths;
    }

    [Serializable]
    public sealed class Report
    {
        public string unity, createdUtc, scope = "all loaded scenes";
        public bool phoneMeasured = false;
        public string[] sceneNames;
        public int renderers, prefabInstances, materialReferences, uniqueMaterials;
        public int meshReferences, uniqueMeshes, textureReferences, uniqueTextures;
        public int identicalSourceGroups, eligibleForReferenceReuseGroups, sourceBytesRepeated;
        public TextureEntry[] textures;
        public DuplicateGroup[] identicalSourceCandidates;
        public string status = "READ_ONLY_AUDIT_NO_ASSETS_MODIFIED";
    }

    [MenuItem("BRAXHORN/Audyt/Wspoldzielone modele, materialy i tekstury (bez zmian)")]
    public static void RunOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Audyt zasobow: zakoncz Play Mode");
        var roots = new List<GameObject>();
        var sceneNames = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded) continue;
            sceneNames.Add(scene.name);
            roots.AddRange(scene.GetRootGameObjects());
        }
        if (roots.Count == 0)
            throw new InvalidOperationException("Brak zaladowanych scen do audytu");

        Report report = Inspect(roots, sceneNames);
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/SharedResourceAudit"));
        Directory.CreateDirectory(folder);
        string output = Path.Combine(folder, "shared-assets.json");
        File.WriteAllText(output, JsonUtility.ToJson(report, true));
        Debug.Log("BRAXHORN_SHARED_ASSETS scenes=" + report.sceneNames.Length +
            " renderers=" + report.renderers + " prefab_instances=" + report.prefabInstances +
            " materials=" + report.uniqueMaterials + "/" + report.materialReferences +
            " meshes=" + report.uniqueMeshes + "/" + report.meshReferences +
            " textures=" + report.uniqueTextures + "/" + report.textureReferences +
            " identical_source_groups=" + report.identicalSourceGroups +
            " same_importer_candidate_groups=" + report.eligibleForReferenceReuseGroups +
            " report=" + output + " scene_modified=false phone_measured=false");
    }

    public static Report Inspect(IEnumerable<GameObject> roots, IEnumerable<string> sceneNames)
    {
        var report = new Report
        {
            unity = Application.unityVersion,
            createdUtc = DateTime.UtcNow.ToString("o"),
            sceneNames = sceneNames.ToArray()
        };

        var materialRefs = new Dictionary<int, Material>();
        var meshRefs = new Dictionary<int, Mesh>();
        var textureRefs = new Dictionary<int, Texture2D>();
        var textureUses = new Dictionary<int, int>();

        foreach (var root in roots)
        {
            if (!root) continue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    report.prefabInstances++;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                report.renderers++;
                Mesh mesh = renderer is SkinnedMeshRenderer skin
                    ? skin.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh)
                {
                    report.meshReferences++;
                    meshRefs[mesh.GetInstanceID()] = mesh;
                }
                foreach (var material in renderer.sharedMaterials)
                    Collect(material);
            }

            // Instanced lawn grass has its own material field rather than a MeshRenderer.
            foreach (var grass in root.GetComponentsInChildren<BraxClaudeTrawa>(true))
            {
                Collect(grass.material);
                if (grass.siatka)
                {
                    report.meshReferences++;
                    meshRefs[grass.siatka.GetInstanceID()] = grass.siatka;
                }
            }
        }

        void Collect(Material material)
        {
            if (!material) return;
            report.materialReferences++;
            int id = material.GetInstanceID();
            if (materialRefs.ContainsKey(id)) return;
            materialRefs.Add(id, material);
            foreach (var slot in material.GetTexturePropertyNames())
            {
                if (!(material.GetTexture(slot) is Texture2D texture)) continue;
                report.textureReferences++;
                int key = texture.GetInstanceID();
                textureRefs[key] = texture;
                textureUses[key] = textureUses.TryGetValue(key, out int old) ? old + 1 : 1;
            }
        }

        report.uniqueMaterials = materialRefs.Count;
        report.uniqueMeshes = meshRefs.Count;
        report.uniqueTextures = textureRefs.Count;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var entries = new List<TextureEntry>();
        var shaByPath = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var texture in textureRefs.Values.OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal))
        {
            string path = AssetDatabase.GetAssetPath(texture);
            var entry = new TextureEntry
            {
                path = path,
                width = texture.width,
                height = texture.height,
                rendererReferences = textureUses[texture.GetInstanceID()],
                importerSignature = ImportSignature(path)
            };
            if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                string disk = Path.GetFullPath(Path.Combine(projectRoot, path));
                if (File.Exists(disk))
                {
                    var info = new FileInfo(disk);
                    entry.sourceFileBytes = info.Length;
                    if (!shaByPath.TryGetValue(path, out string sha))
                    {
                        using (var stream = File.OpenRead(disk))
                        using (var hash = SHA256.Create())
                            sha = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        shaByPath.Add(path, sha);
                    }
                    entry.sourceSha256 = sha;
                }
            }
            entries.Add(entry);
        }

        report.textures = entries.ToArray();
        var groups = entries.Where(e => !string.IsNullOrEmpty(e.sourceSha256))
            .GroupBy(e => e.sourceSha256).Where(g => g.Select(e => e.path).Distinct().Count() > 1).ToArray();
        var duplicates = new List<DuplicateGroup>();
        foreach (var group in groups)
        {
            // Same PNG bytes do not guarantee same rendering: import settings must also match.
            var members = group.GroupBy(e => e.path).Select(g => g.First()).ToArray();
            bool sameImporter = members.All(e => !string.IsNullOrEmpty(e.importerSignature) &&
                e.importerSignature == members[0].importerSignature);
            duplicates.Add(new DuplicateGroup
            {
                sourceSha256 = group.Key,
                importersEquivalent = sameImporter,
                paths = members.Select(e => e.path).ToArray()
            });
            if (sameImporter) report.eligibleForReferenceReuseGroups++;
            long smallest = members.Min(e => e.sourceFileBytes);
            // Raw redundant disk bytes only; NOT RAM or APK savings.
            long repeated = smallest * (members.Length - 1);
            report.sourceBytesRepeated = (int)Math.Min(int.MaxValue,
                (long)report.sourceBytesRepeated + repeated);
        }
        report.identicalSourceGroups = duplicates.Count;
        report.identicalSourceCandidates = duplicates.ToArray();
        return report;
    }

    static string ImportSignature(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) return "";
        var android = imp.GetPlatformTextureSettings("Android");
        return string.Join("|", new[]
        {
            imp.textureShape.ToString(), imp.textureType.ToString(), imp.sRGBTexture.ToString(),
            imp.mipmapEnabled.ToString(), imp.maxTextureSize.ToString(), imp.isReadable.ToString(),
            imp.alphaSource.ToString(), imp.alphaIsTransparency.ToString(),
            imp.mipMapsPreserveCoverage.ToString(),
            imp.alphaTestReferenceValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            imp.wrapMode.ToString(), imp.filterMode.ToString(), imp.anisoLevel.ToString(),
            imp.npotScale.ToString(), imp.textureCompression.ToString(), imp.compressionQuality.ToString(),
            imp.streamingMipmaps.ToString(), imp.streamingMipmapsPriority.ToString(),
            android.overridden.ToString(), android.format.ToString(),
            android.maxTextureSize.ToString(), android.compressionQuality.ToString()
        });
    }
}

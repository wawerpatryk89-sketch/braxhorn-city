using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Static procedural meshes only. Identity depends on geometry, never generation order.
// Existing assets and their metadata are reused without rewriting them.
public static class BraxObozStableMesh
{
    const string Folder = "Assets/PlayableGenerated/Oboz/Environment/StableMeshes";

    public static Mesh Save(Mesh mesh)
    {
        string hash = Fingerprint(mesh);
        string path = Folder + "/mesh-" + hash + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing)
        {
            if (Fingerprint(existing) != hash)
                throw new InvalidOperationException("Zmieniono niezmienną siatkę obozu: " + path);
            Object.DestroyImmediate(mesh);
            return existing;
        }
        // Do not overwrite an unloaded or damaged existing asset.
        if (File.Exists(path) || File.Exists(path + ".meta"))
            throw new IOException("Niejednoznaczny zapis siatki obozu: " + path);
        if (!Directory.Exists(Folder)) { Directory.CreateDirectory(Folder); AssetDatabase.Refresh(); }
        mesh.name = "ObozMesh-" + hash;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    public static string Fingerprint(Mesh mesh)
    {
        if (!mesh || !mesh.isReadable || mesh.blendShapeCount != 0 ||
            mesh.bindposes.Length != 0 || mesh.boneWeights.Length != 0)
            throw new InvalidOperationException("Oczekiwano czytelnej statycznej siatki generatora obozu");
        using (var bytes = new MemoryStream())
        using (var writer = new BinaryWriter(bytes))
        {
            writer.Write("oboz-static-mesh-v1");
            writer.Write((int)mesh.indexFormat);
            Write(writer, mesh.vertices); Write(writer, mesh.normals);
            var tangents = mesh.tangents; writer.Write(tangents.Length);
            foreach (var v in tangents) Write(writer, v);
            var colors = mesh.colors; writer.Write(colors.Length);
            foreach (var c in colors) { writer.Write(c.r); writer.Write(c.g); writer.Write(c.b); writer.Write(c.a); }
            for (int channel = 0; channel < 8; channel++)
            {
                var uv = new List<Vector4>(); mesh.GetUVs(channel, uv); writer.Write(uv.Count);
                foreach (var v in uv) Write(writer, v);
            }
            writer.Write(mesh.subMeshCount);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                writer.Write((int)mesh.GetTopology(i));
                var indices = mesh.GetIndices(i); writer.Write(indices.Length);
                foreach (int index in indices) writer.Write(index);
                var sub = mesh.GetSubMesh(i);
                Write(writer, sub.bounds.center); Write(writer, sub.bounds.size);
            }
            Write(writer, mesh.bounds.center); Write(writer, mesh.bounds.size);
            writer.Flush();
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }
    static void Write(BinaryWriter w, Vector3[] values)
    { w.Write(values.Length); foreach (var v in values) Write(w, v); }
    static void Write(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
    static void Write(BinaryWriter w, Vector4 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); w.Write(v.w); }
}

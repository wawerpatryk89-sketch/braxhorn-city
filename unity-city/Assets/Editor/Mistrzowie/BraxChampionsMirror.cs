using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using W=BraxWnetrzaKit;

// Owner 10.11 12:33 (APK 37, athletes' vanity: a flat lavender "mirror" plate): "Wszystkie lustra, sprawdzasz wszystko.
// Lustro ma być lustrem, żadnej tandety." And the global rule (docs/zasady/OBIEKTY_Z_KODU_PRAWDZIWE_MATERIALY): a mirror
// is a real reflection, never a grey or silver plate. Every mirror of the region is built here: the city's live planar
// mirror (BraxGymPlanarMirror + shader Braxhorn/GymPlanarMirror, R1, unchanged) on its own layer, linked to the player
// camera at run time (BraxChampionsMirrorCamera), a polished stainless edge frame round it. Built in W-kit build-time
// world coordinates under the current W group (the root is still at the origin, scale 1), so it moves with the room.
public static class BraxChampionsMirror
{
    const string Folder = "Assets/PlayableGenerated/Mistrzowie/Mirrors";
    static Material shared;
    public static int Built { get; private set; }
    public static void Reset() { Built = 0; shared = null; }

    static Material Glass()
    {
        if (shared) return shared;
        Directory.CreateDirectory(Folder);
        string path = Folder + "/mistrzowie_live_mirror.mat";
        shared = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (shared) return shared;
        var shader = Shader.Find("Braxhorn/GymPlanarMirror");
        if (!shader) throw new InvalidOperationException("Live mirror shader Braxhorn/GymPlanarMirror missing (R1 Kickboxing)");
        shared = new Material(shader) { name = "Mistrzowie live mirror glass" };
        AssetDatabase.CreateAsset(shared, path);
        return shared;
    }

    /// <summary>
    /// Live mirror on a wall. centre = middle of the glass (build-time world = root-local nominal); normal = unit vector
    /// from the wall into the room (axis aligned: ±x or ±z); frame = edge material (polished steel / brass / black).
    /// range = how near the player must be for it to render; reach = how far the reflection looks into the room.
    /// </summary>
    public static GameObject Wall(string label, Vector3 centre, Vector3 normal, float width, float height, Material frame,
                                  float range = 8f, float reach = 14f, int texWidth = 512, int texHeight = 256)
    {
        var host = W.Grupa; bool inside = W.Wnetrze;
        if (!host) throw new InvalidOperationException("BraxChampionsMirror needs a W group");
        normal = normal.normalized;
        var go = new GameObject(label + " | live mirror (planar reflection)");
        go.transform.SetParent(host, true);
        go.transform.position = centre + normal * .004f;
        go.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
        go.layer = BraxChampionsMirrorCamera.MirrorLayer;
        float hw = width * .5f, hh = height * .5f;
        var mesh = new Mesh { name = label + " mirror glass" };
        mesh.vertices = new[] { new Vector3(-hw, -hh, 0), new Vector3(hw, -hh, 0), new Vector3(hw, hh, 0), new Vector3(-hw, hh, 0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        Directory.CreateDirectory(Folder);
        BraxZapisAssetu.Utworz(mesh, Folder + "/" + W.Bezpieczna(label) + "_" + Built.ToString("D4") + ".asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = Glass();
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        var live = go.AddComponent<BraxGymPlanarMirror>();
        live.mirrorRenderer = r; live.mirrorLayer = BraxChampionsMirrorCamera.MirrorLayer;
        live.textureWidth = texWidth; live.textureHeight = texHeight; live.refreshRate = 24f;
        live.maximumViewDistance = range; live.reflectionDistance = reach;
        go.AddComponent<BraxChampionsMirrorCamera>();
        // polished edge frame 18 mm wide, 14 mm deep, round the glass (a mirror never ends in a raw edge)
        if (frame)
        {
            Vector3 u = Vector3.Cross(Vector3.up, normal).normalized, v = Vector3.up;
            const float fw = .018f, fd = .014f;
            void Bar(Vector3 c, float along, float up)
            {
                Vector3 a = c - u * along * .5f - v * up * .5f, b = c + u * along * .5f + v * up * .5f;
                Vector3 n0 = c - normal * .004f, n1 = c + normal * fd;
                W.MM(frame, Mathf.Min(a.x, b.x, n0.x, n1.x), Mathf.Max(a.x, b.x, n0.x, n1.x), Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y),
                     Mathf.Min(a.z, b.z, n0.z, n1.z), Mathf.Max(a.z, b.z, n0.z, n1.z));
            }
            Bar(centre + v * (hh + fw * .5f), width + 2f * fw, fw);
            Bar(centre - v * (hh + fw * .5f), width + 2f * fw, fw);
            Bar(centre + u * (hw + fw * .5f), fw, height);
            Bar(centre - u * (hw + fw * .5f), fw, height);
        }
        W.Grupa = host; W.Wnetrze = inside;
        Built++;
        return go;
    }
}

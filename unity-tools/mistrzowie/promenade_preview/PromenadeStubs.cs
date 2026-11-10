// Stand-ins for the editor API used by the waterfront generators (harbour, railings, promenade furniture, kiosks, beach).
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static partial class BraxChampionsFoundation
{
    [Serializable] public sealed class Foundation
    {
        public string version, regionId, sourceCommit; public float scaleK;
        public MeshData[] meshes; public Plant[] plants; public Building[] buildings;
        public Berth[] berths; public Pier[] piers; public Pavilion[] promenade;
        public PalmBed[] palmBeds; public Furniture[] furniture; public Opening[] openings;
    }
    [Serializable] public sealed class Plant { public string id, kind, band; public float x, z, yaw; public int variant; }
    [Serializable] public sealed class Building { public string id, name; public float x, z, w, d, height; }
    [Serializable] public sealed class Pavilion { public string id, name, sign, style; public float x, z, w, d, h; public bool walkIn; }
    [Serializable] public sealed class Pier { public string id, role; public float x, z, w, d; }
    [Serializable] public sealed class Berth { public string id, kind, dock; public float x, z, length, width, accessX, accessZ, yaw; }
    [Serializable] public sealed class PalmBed { public float x, z, w, d; }
    [Serializable] public sealed class Furniture { public string kind; public float x, z; }
    [Serializable] public sealed class Opening { public float x0, x1; }
}

// The owner's harbour GLBs (Assets/Source/HarborBeach/Models): real bounds, drawn by the preview from the files.
public static class BraxHarborBeachBuild
{
    public static string Root = "Assets/Source/HarborBeach/Models/";
    public static GameObject Model(string file)
    {
        var p = Root + file; if (!File.Exists(p)) return null;
        var g = new GameObject(file); GameObject.Registry.Remove(g);
        var pm = g.AddComponent<PreviewModel>(); pm.path = p; (pm.min, pm.max) = GltfBounds.Read(p);
        g.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true };
        return g;
    }
    public static GameObject Postaw(Transform parent, string file, string name, Vector3 floor, float height, float yaw)
    {
        var src = Model(file); if (src == null) return null;
        var o = UnityEngine.Object.Instantiate(src); o.name = name; o.transform.SetParent(parent, false);
        var pm = o.GetComponent<PreviewModel>(); float s = height / Math.Max(.01f, pm.max.y - pm.min.y);
        o.transform.localScale = Vector3.one * s; o.transform.rotation = Quaternion.Euler(0, yaw, 0);
        o.transform.position = floor - new Vector3(0, pm.min.y * s, 0);
        return o;
    }
}
public static class BraxNabrzezeRatownik { public const float WysModelu = 5.2f, Skala = 1.2944444f; public static int Wejscie(GameObject t, Transform c, out float deck) { deck = 2.4f; return 1; } }
public static class BraxRegion
{
    public static Bounds Bounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    public static Material CityMaterial(Material m) => m;
    public static GameObject Model(params string[] ids) => null;
}
public sealed class BraxVessel : MonoBehaviour
{
    public string label; public float waterY, draft, length, width, maxSpeed, turnRate, accel, moorYaw; public bool sit;
    public Vector3 seat, moor, wejscie;
}
public sealed class BraxCitySignText : MonoBehaviour { }
namespace UnityEngine { public class DisallowMultipleComponentAttribute : System.Attribute { }
    public static class Time { public static float unscaledTime, deltaTime; }
    public class Camera : Behaviour { public static Camera main; }
    public static class Application { public static string dataPath => System.IO.Path.GetFullPath("Assets"); } }

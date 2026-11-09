// Minimal stand-ins for the Unity/editor API used by the Champions generators (apartments T04-T07, hotel T03).
// Purpose: compile with C# 9 (Unity 6) and record the generated geometry for a preview. Transforms keep a real
// hierarchy (position, rotation, scale) so meshes placed under scaled/rotated parents can be dumped exactly.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public float sqrMagnitude => x * x + y * y; public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized => this / magnitude; }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0); public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0); public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0); public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 back => new Vector3(0, 0, -1); public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 positiveInfinity => new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z); public Vector3 normalized => this * (1f / magnitude);
        public override string ToString() => "(" + x.ToString("F2") + ", " + y.ToString("F2") + ", " + z.ToString("F2") + ")"; }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0); }
    public struct Quaternion { public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        static Quaternion Axis(Vector3 a, float deg) { var r = deg * Mathf.Deg2Rad * .5f; var s = (float)Math.Sin(r); return new Quaternion(a.x * s, a.y * s, a.z * s, (float)Math.Cos(r)); }
        // Unity order: Z, then X, then Y (q = qy * qx * qz)
        public static Quaternion Euler(float x, float y, float z) => Axis(Vector3.up, y) * Axis(Vector3.right, x) * Axis(Vector3.forward, z);
        public static Quaternion LookRotation(Vector3 f, Vector3 up)
        {
            var yaw = (float)Math.Atan2(f.x, f.z) * Mathf.Rad2Deg; var h = (float)Math.Sqrt(f.x * f.x + f.z * f.z);
            var pitch = -(float)Math.Atan2(f.y, h) * Mathf.Rad2Deg; return Euler(pitch, yaw, 0);
        }
        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v) { var u = new Vector3(q.x, q.y, q.z); var t = Vector3.Cross(u, v) * 2f; return v + t * q.w + Vector3.Cross(u, t); } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1); public static Color operator *(Color c, float d) => new Color(c.r * d, c.g * d, c.b * d, c.a); }
    public struct Bounds { public Vector3 center, size;
        public Bounds(Vector3 c, Vector3 s) { center = c; size = s; }
        public Vector3 extents => size * .5f; public Vector3 min => center - extents; public Vector3 max => center + extents;
        public void Encapsulate(Bounds b)
        {
            var lo = new Vector3(Math.Min(min.x, b.min.x), Math.Min(min.y, b.min.y), Math.Min(min.z, b.min.z));
            var hi = new Vector3(Math.Max(max.x, b.max.x), Math.Max(max.y, b.max.y), Math.Max(max.z, b.max.z));
            center = (lo + hi) * .5f; size = hi - lo;
        }
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z; }
    public static class Mathf { public const float PI = (float)Math.PI, Deg2Rad = (float)(Math.PI / 180), Rad2Deg = (float)(180 / Math.PI);
        public static float Min(float a, float b) => Math.Min(a, b); public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b); public static int Max(int a, int b) => Math.Max(a, b); public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Abs(float a) => Math.Abs(a); public static float Sign(float a) => a >= 0 ? 1 : -1;
        public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven); public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static int CeilToInt(float a) => (int)Math.Ceiling(a); public static int FloorToInt(float a) => (int)Math.Floor(a);
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Cos(float a) => (float)Math.Cos(a); public static float Sin(float a) => (float)Math.Sin(a); }
    public class Object { string nm; public virtual string name { get => nm; set => nm = value; }
        public static implicit operator bool(Object o) => o != null && !o.destroyed;
        internal bool destroyed;
        public static void DestroyImmediate(Object o)
        {
            if (o is GameObject g) { g.transform.SetParent(null, false); GameObject.Registry.Remove(g); }
            if (o != null) o.destroyed = true;
        } }
    public class Texture : Object { }
    public class Shader : Object { public static Shader Find(string n) => new Shader { name = n }; }
    public class Material : Object { public Material() { } public Material(Material m) { name = m.name; } public Material(Shader s) { }
        public bool enableInstancing; public int renderQueue; public Texture mainTexture;
        public void SetFloat(string n, float v) { } public void SetColor(string n, Color c) { } public void SetVector(string n, Vector4 v) { }
        public void SetOverrideTag(string t, string v) { } public bool HasProperty(string n) => true;
        public string[] GetTexturePropertyNames() => new string[0]; public Texture GetTexture(string n) => null; }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform;
        public override string name { get => gameObject.name; set => gameObject.name = value; }
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool inactive) where T : Component => gameObject.All<T>(); }
    public class Transform : Component { public Transform parent; public readonly List<Transform> children = new List<Transform>();
        public Vector3 localPosition; public Quaternion localRotation = Quaternion.identity; public Vector3 localScale = Vector3.one;
        public Vector3 position { get => parent == null ? localPosition : parent.TransformPoint(localPosition); set => localPosition = parent == null ? value : parent.InverseTransformPoint(value); }
        public Quaternion rotation { get => parent == null ? localRotation : parent.rotation * localRotation; set => localRotation = parent == null ? value : Quaternion.Inverse(parent.rotation) * value; }
        public void SetParent(Transform p, bool keep) { parent?.children.Remove(this); parent = p; p?.children.Add(this); }
        public Transform Find(string n) => children.FirstOrDefault(c => c.gameObject.name == n);
        public Vector3 TransformPoint(Vector3 v) { var l = localPosition + localRotation * Vector3.Scale(localScale, v); return parent == null ? l : parent.TransformPoint(l); }
        public Vector3 TransformVector(Vector3 v) { var l = localRotation * Vector3.Scale(localScale, v); return parent == null ? l : parent.TransformVector(l); }
        public Vector3 InverseTransformPoint(Vector3 w)
        {
            var l = parent == null ? w : parent.InverseTransformPoint(w); var d = Quaternion.Inverse(localRotation) * (l - localPosition);
            return new Vector3(d.x / localScale.x, d.y / localScale.y, d.z / localScale.z);
        }
        public Vector3 InverseTransformVector(Vector3 w)
        {
            var l = parent == null ? w : parent.InverseTransformVector(w); var d = Quaternion.Inverse(localRotation) * l;
            return new Vector3(d.x / localScale.x, d.y / localScale.y, d.z / localScale.z);
        } }
    public class GameObject : Object { public Transform transform; public readonly List<Component> comps = new List<Component>();
        internal static readonly List<GameObject> Registry = new List<GameObject>();
        public bool activeSelf = true; public void SetActive(bool on) => activeSelf = on;
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; comps.Add(transform); Registry.Add(this); }
        public static GameObject Find(string n) => Registry.FirstOrDefault(g => g.name == n && g.transform.parent == null);
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; comps.Add(c); return c; }
        public T GetComponent<T>() where T : Component => comps.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool inactive) where T : Component => All<T>();
        public T[] All<T>() where T : Component { var r = new List<T>(); void Walk(Transform t) { r.AddRange(t.gameObject.comps.OfType<T>()); foreach (var c in t.children.ToArray()) Walk(c); } Walk(transform); return r.ToArray(); } }
    public class Behaviour : Component { public bool enabled = true; } public class MonoBehaviour : Behaviour { }
    public class Renderer : Component { public Material sharedMaterial; public Material[] sharedMaterials => new[] { sharedMaterial }; public Bounds bounds => new Bounds(transform.position, Vector3.zero); }
    public class MeshRenderer : Renderer { }
    public class Mesh : Object { public Vector3[] vertices = new Vector3[0]; public Vector2[] uv; public int[] triangles = new int[0];
        public Rendering.IndexFormat indexFormat;
        public void SetVertices(List<Vector3> v) => vertices = v.ToArray(); public void SetNormals(List<Vector3> n) { }
        public void SetUVs(int ch, List<Vector2> u) { } public void SetTriangles(List<int> t, int sub) => triangles = t.ToArray();
        public void RecalculateNormals() { } public void RecalculateTangents() { } public void RecalculateBounds() { } }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Collider : Component { } public class BoxCollider : Collider { public Vector3 center, size; }
    public class TextMesh : Component { public string text; }
    public class Light : Behaviour { public float range, intensity; }
    public struct LOD { public LOD(float h, Renderer[] r) { } }
    public class LODGroup : Component { public void SetLODs(LOD[] l) { } public void RecalculateBounds() { } }
    public struct RaycastHit { public Vector3 point; }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public static class Physics { public static void SyncTransforms() { }
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit h, float dist, int mask, QueryTriggerInteraction q) { h = new RaycastHit { point = new Vector3(o.x, -.25f * BraxClaudeOsiedle.K, o.z) }; return true; } }
    public static class Debug { public static void Log(object o) => Console.WriteLine(o); }
    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions O = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        public static T FromJson<T>(string s) => System.Text.Json.JsonSerializer.Deserialize<T>(s, O);
        public static string ToJson(object o, bool pretty) => System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), O);
    }
}
namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
    public enum BlendMode { Zero = 0, One = 1, DstColor = 2, SrcColor = 3, OneMinusDstColor = 4, SrcAlpha = 5, OneMinusSrcColor = 6, DstAlpha = 7, OneMinusDstAlpha = 8, SrcAlphaSaturate = 9, OneMinusSrcAlpha = 10 }
}
namespace UnityEditor
{
    using UnityEngine;
    public enum InteractionMode { AutomatedAction, UserAction }
    [Flags] public enum ImportAssetOptions { Default = 0, ForceUpdate = 1, ForceSynchronousImport = 8 }
    [Flags] public enum StaticEditorFlags { ContributeGI = 1, OccluderStatic = 2, BatchingStatic = 4, NavigationStatic = 8, OccludeeStatic = 16 }
    public static class AssetDatabase { public static T LoadAssetAtPath<T>(string p) where T : Object => null; public static void CreateAsset(Object o, string p) { }
        public static void SaveAssets() { } public static void ImportAsset(string p, ImportAssetOptions o) { } }
    public static class EditorUtility { public static void SetDirty(Object o) { } public static void CopySerialized(Object a, Object b) { } }
    public static class GameObjectUtility { public static void SetStaticEditorFlags(GameObject g, StaticEditorFlags f) { } }
    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAssetAndConnect(GameObject g, string p, InteractionMode m) => g;
        public static GameObject SaveAsPrefabAsset(GameObject g, string p) { var asset = new GameObject(g.name); GameObject.Registry.Remove(asset); return asset; }
        // A prefab instance in the preview is an empty stand-in; the preview draws the template itself at every level.
        public static Object InstantiatePrefab(Object o) => new GameObject(o.name + " (instance)");
        public static Object InstantiatePrefab(Object o, Transform parent) { var g = new GameObject(o.name + " (instance)"); g.transform.SetParent(parent, false); return g; }
    }
}

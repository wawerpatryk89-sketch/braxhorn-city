// Minimal stand-ins for the Unity/editor API used by BraxChampionsApartments.cs.
// Purpose: compile with C# 9 (Unity 6) and record the generated geometry for a preview.
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
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => a * d;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z); public Vector3 normalized => this * (1f / magnitude); }
    public struct Quaternion { public float x, y, z, w; public static Quaternion identity => new Quaternion { w = 1 };
        public static Quaternion Euler(float x, float y, float z) { var r = y * Mathf.Deg2Rad * .5f; return new Quaternion { y = (float)Math.Sin(r), w = (float)Math.Cos(r) }; }
        public static Quaternion LookRotation(Vector3 f, Vector3 up) { var yaw = (float)Math.Atan2(f.x, f.z); return Euler(0, yaw * Mathf.Rad2Deg, 0); }
        public static Vector3 operator *(Quaternion q, Vector3 v) { var u = new Vector3(q.x, q.y, q.z); var t = Vector3.Cross(u, v) * 2f; return v + t * q.w + Vector3.Cross(u, t); } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1); public static Color operator *(Color c, float d) => new Color(c.r * d, c.g * d, c.b * d, c.a); }
    public struct Bounds { public Bounds(Vector3 c, Vector3 s) { } }
    public static class Mathf { public const float Deg2Rad = (float)(Math.PI / 180), Rad2Deg = (float)(180 / Math.PI);
        public static float Min(float a, float b) => Math.Min(a, b); public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b); public static int Max(int a, int b) => Math.Max(a, b); public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Abs(float a) => Math.Abs(a); public static float Sign(float a) => a >= 0 ? 1 : -1;
        public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven); public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static int CeilToInt(float a) => (int)Math.Ceiling(a); public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Cos(float a) => (float)Math.Cos(a); public static float Sin(float a) => (float)Math.Sin(a); }
    public class Object { public string name; public static implicit operator bool(Object o) => o != null; }
    public class Material : Object { public Material() { } public Material(Material m) { name = m.name; }
        public void SetFloat(string n, float v) { } public void SetColor(string n, Color c) { } public bool HasProperty(string n) => true; }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform;
        public T[] GetComponentsInChildren<T>(bool inactive) where T : Component => gameObject.All<T>(); }
    public class Transform : Component { public Transform parent; public readonly List<Transform> children = new List<Transform>();
        public Vector3 position; public Quaternion rotation = Quaternion.identity; public Vector3 localScale = Vector3.one;
        public void SetParent(Transform p, bool keep) { parent = p; p.children.Add(this); }
        public Vector3 TransformPoint(Vector3 v) => position + rotation * new Vector3(v.x * localScale.x, v.y * localScale.y, v.z * localScale.z); }
    public class GameObject : Object { public Transform transform; public readonly List<Component> comps = new List<Component>();
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; comps.Add(transform); }
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; comps.Add(c); return c; }
        public T[] All<T>() where T : Component { var r = new List<T>(); void Walk(Transform t) { r.AddRange(t.gameObject.comps.OfType<T>()); foreach (var c in t.children) Walk(c); } Walk(transform); return r.ToArray(); } }
    public class Behaviour : Component { } public class MonoBehaviour : Behaviour { }
    public class Renderer : Component { } public class MeshRenderer : Renderer { }
    public class Light : Behaviour { public float range, intensity; }
    public struct LOD { public LOD(float h, Renderer[] r) { } }
    public class LODGroup : Component { public void SetLODs(LOD[] l) { } public void RecalculateBounds() { } }
    public struct RaycastHit { public Vector3 point; }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public static class Physics { public static void SyncTransforms() { }
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit h, float dist, int mask, QueryTriggerInteraction q) { h = new RaycastHit { point = new Vector3(o.x, -.25f * BraxClaudeOsiedle.K, o.z) }; return true; } }
    public static class Debug { public static void Log(object o) => Console.WriteLine(o); }
}
namespace UnityEditor
{
    using UnityEngine;
    public enum InteractionMode { AutomatedAction, UserAction }
    public static class AssetDatabase { public static T LoadAssetAtPath<T>(string p) where T : Object => null; public static void CreateAsset(Object o, string p) { } }
    public static class EditorUtility { public static void SetDirty(Object o) { } }
    public static class PrefabUtility { public static GameObject SaveAsPrefabAssetAndConnect(GameObject g, string p, InteractionMode m) => g; }
}

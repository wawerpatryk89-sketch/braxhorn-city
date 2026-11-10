// Minimal stand-ins for the Unity/editor API used by the Champions generators (apartments T04-T07, hotel T03).
// Purpose: compile with C# 9 (Unity 6) and record the generated geometry for a preview. Transforms keep a real
// hierarchy (position, rotation, scale) so meshes placed under scaled/rotated parents can be dumped exactly.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1); public static Vector2 up => new Vector2(0, 1); public static Vector2 right => new Vector2(1, 0);
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * Mathf.Clamp(t, 0, 1);
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
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude; public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp(t, 0, 1);
        public void Normalize() { float m = magnitude; if (m > 1e-9f) { x /= m; y /= m; z /= m; } }
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z); public Vector3 normalized => this * (1f / magnitude);
        public override string ToString() => ToString("F2"); public string ToString(string f) => "(" + x.ToString(f) + ", " + y.ToString(f) + ", " + z.ToString(f) + ")"; }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static implicit operator Vector4(Vector3 v) => new Vector4(v.x, v.y, v.z, 0); }
    public struct Quaternion { public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public Vector3 eulerAngles { get { var f = this * Vector3.forward; return new Vector3(-(float)Math.Asin(Math.Max(-1, Math.Min(1, f.y))) * Mathf.Rad2Deg, (float)Math.Atan2(f.x, f.z) * Mathf.Rad2Deg, 0); } }
        static Quaternion Axis(Vector3 a, float deg) { var r = deg * Mathf.Deg2Rad * .5f; var s = (float)Math.Sin(r); return new Quaternion(a.x * s, a.y * s, a.z * s, (float)Math.Cos(r)); }
        // Unity order: Z, then X, then Y (q = qy * qx * qz)
        public static Quaternion Euler(float x, float y, float z) => Axis(Vector3.up, y) * Axis(Vector3.right, x) * Axis(Vector3.forward, z);
        public static Quaternion LookRotation(Vector3 f, Vector3 up)
        {
            var yaw = (float)Math.Atan2(f.x, f.z) * Mathf.Rad2Deg; var h = (float)Math.Sqrt(f.x * f.x + f.z * f.z);
            var pitch = -(float)Math.Atan2(f.y, h) * Mathf.Rad2Deg; return Euler(pitch, yaw, 0);
        }
        public static Quaternion FromToRotation(Vector3 a, Vector3 b)
        {
            a = a.normalized; b = b.normalized; float d = Vector3.Dot(a, b);
            if (d > .999999f) return identity;
            if (d < -.999999f) { var o = Vector3.Cross(Math.Abs(a.x) < .9f ? Vector3.right : Vector3.up, a).normalized; return new Quaternion(o.x, o.y, o.z, 0); }
            var c = Vector3.Cross(a, b); var q = new Quaternion(c.x, c.y, c.z, 1 + d); float m = (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }
        public static Quaternion AngleAxis(float deg, Vector3 axis) => Axis(axis.normalized, deg);
        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v) { var u = new Vector3(q.x, q.y, q.z); var t = Vector3.Cross(u, v) * 2f; return v + t * q.w + Vector3.Cross(u, t); } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1); public static Color black => new Color(0, 0, 0); public static Color operator *(Color c, float d) => new Color(c.r * d, c.g * d, c.b * d, c.a);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp(t, 0, 1); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); } }
    public struct Ray { public Vector3 origin, direction; public Ray(Vector3 o, Vector3 d) { origin = o; direction = d; } }
    public struct Bounds { public Vector3 center, size;
        public Bounds(Vector3 c, Vector3 s) { center = c; size = s; }
        public Vector3 extents => size * .5f; public Vector3 min => center - extents; public Vector3 max => center + extents;
        public void Encapsulate(Bounds b)
        {
            var lo = new Vector3(Math.Min(min.x, b.min.x), Math.Min(min.y, b.min.y), Math.Min(min.z, b.min.z));
            var hi = new Vector3(Math.Max(max.x, b.max.x), Math.Max(max.y, b.max.y), Math.Max(max.z, b.max.z));
            center = (lo + hi) * .5f; size = hi - lo;
        }
        public void Encapsulate(Vector3 p) => Encapsulate(new Bounds(p, Vector3.zero));
        public bool IntersectRay(Ray r, out float d) { d = 0; return false; }
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z; }
    public static class Mathf { public const float PI = (float)Math.PI, Deg2Rad = (float)(Math.PI / 180), Rad2Deg = (float)(180 / Math.PI);
        public static float Min(float a, float b) => Math.Min(a, b); public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b); public static int Max(int a, int b) => Math.Max(a, b); public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Abs(float a) => Math.Abs(a); public static float Sign(float a) => a >= 0 ? 1 : -1;
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven); public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static int CeilToInt(float a) => (int)Math.Ceiling(a); public static int FloorToInt(float a) => (int)Math.Floor(a);
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v; public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Max(params float[] v) => v.Max(); public static float Min(params float[] v) => v.Min(); public static float Log(float a) => (float)Math.Log(a); public static float Log(float a, float b) => (float)Math.Log(a, b); public static float Exp(float a) => (float)Math.Exp(a);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x); public static float Sqrt(float a) => (float)Math.Sqrt(a); public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0, 1);
        public static float Tan(float a) => (float)Math.Tan(a); public static float Floor(float a) => (float)Math.Floor(a); public static float Ceil(float a) => (float)Math.Ceiling(a);
        public static float Cos(float a) => (float)Math.Cos(a); public static float Sin(float a) => (float)Math.Sin(a); }
    public class Object { string nm; public virtual string name { get => nm; set => nm = value; }
        public static implicit operator bool(Object o) => o != null && !o.destroyed;
        internal bool destroyed;
        public static T Instantiate<T>(T original) where T : Object
        {
            if (original is Mesh m) return new Mesh { name = m.name, vertices = (Vector3[])m.vertices.Clone(), normals = (Vector3[])m.normals?.Clone(), uv = (Vector2[])m.uv?.Clone(), triangles = (int[])m.triangles.Clone() } as T;
            // a preview model (glTF stand-in): a real copy, so every instance has its own transform (harbour boats)
            if (original is GameObject g && g.GetComponent<PreviewModel>() is PreviewModel src)
            {
                var c = new GameObject(g.name + "(Clone)"); var pm = c.AddComponent<PreviewModel>(); pm.path = src.path; pm.min = src.min; pm.max = src.max;
                c.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true };
                c.transform.localScale = g.transform.localScale; c.transform.localRotation = g.transform.localRotation; return c as T;
            }
            return original;
        }
        public static T Instantiate<T>(T original, Transform parent, bool stays) where T : Object
        {
            if (original is GameObject g) { var c = new GameObject(g.name + "(Clone)"); c.transform.SetParent(parent, false); return c as T; }
            return original;
        }
        public static void DestroyImmediate(Object o)
        {
            if (o is GameObject g) { g.transform.SetParent(null, false); GameObject.Registry.Remove(g); }
            if (o != null) o.destroyed = true;
        } }
    public class Texture : Object { } public class Texture2D : Texture { public int width = 2048, height = 512; public bool real; } public enum TextureWrapMode { Repeat, Clamp } public enum FilterMode { Point, Bilinear, Trilinear }
    public class Shader : Object { public static Shader Find(string n) => new Shader { name = n }; }
    public class Material : Object { public Material() { } public Material(Material m) { name = m.name; color = m.color; sky = m.sky; ground = m.ground; ambient = m.ambient; renderQueue = m.renderQueue; metallic = m.metallic; roughness = m.roughness; emission = m.emission; picture = m.picture; textured = m.textured; } public Material(Shader s) { }
        public bool enableInstancing, textured; public int renderQueue; public Color color = new Color(1, 1, 1);
        Texture main; public Texture mainTexture { get => main; set { main = value; if (value is Texture2D t && t.real) { picture = t.name; textured = true; } } }
        // CityStaticPBR ambient (shader defaults), recorded so the preview lights each material as Unity does
        public Color sky = new Color(.60f, .68f, .78f), ground = new Color(.42f, .40f, .36f); public float ambient = .65f;
        // metallic / roughness / emission are recorded for generators that build plain meshes (casino, arena)
        public float metallic = 0, roughness = .5f; public Color emission = new Color(0, 0, 0); public string picture;
        public void SetFloat(string n, float v) { if (n == "_AmbientIntensity") ambient = v; else if (n == "_Metallic") metallic = v; else if (n == "_Roughness") roughness = v; }
        public void SetColor(string n, Color c) { if (n == "_Color") color = c; else if (n == "_SkyColor") sky = c; else if (n == "_GroundColor") ground = c; else if (n == "_EmissionColor") emission = c; }
        public void SetTexture(string n, Texture t) { if (n == "_MainTex" && t is Texture2D t2 && t2.real) { picture = t.name; textured = true; } }
        public void SetVector(string n, Vector4 v) { }
        public void SetOverrideTag(string t, string v) { } public bool HasProperty(string n) => true;
        public string[] GetTexturePropertyNames() => textured ? new[] { "_MainTex" } : new string[0]; public Texture GetTexture(string n) => textured ? new Texture() : null; }
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
        public Vector3 up => rotation * Vector3.up; public Vector3 forward => rotation * Vector3.forward; public Vector3 right => rotation * Vector3.right;
        public Vector3 eulerAngles { get { var f = rotation * Vector3.forward; return new Vector3(0, (float)(Math.Atan2(f.x, f.z) * 180 / Math.PI), 0); } }
        public void Rotate(Vector3 axis, float angle, Space space) { localRotation = localRotation * Quaternion.AngleAxis(angle, axis); }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { var q = Quaternion.AngleAxis(angle, axis); var p = position; position = point + q * (p - point); rotation = q * rotation; }
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
    public class GameObject : Object { public int layer; public Transform transform; public readonly List<Component> comps = new List<Component>();
        internal static readonly List<GameObject> Registry = new List<GameObject>();
        public bool activeSelf = true; public void SetActive(bool on) => activeSelf = on;
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; comps.Add(transform); Registry.Add(this); }
        // Unity's built-in Cube (1 m, 24 vertices) and Quad (1 m, faces -Z)
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var g = new GameObject(type.ToString()); var m = new Mesh { name = type.ToString() };
            var v = new List<Vector3>(); var nn = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            void Face(Vector3 n, Vector3 a, Vector3 b)   // a, b: in-plane half axes with n = a x b pointing out in Unity's left-handed, clockwise order
            {
                int i = v.Count; var c = n * .5f;
                v.Add(c - a - b); v.Add(c + a - b); v.Add(c + a + b); v.Add(c - a + b);
                for (int k = 0; k < 4; k++) nn.Add(n);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            if (type == PrimitiveType.Quad) { Face(new Vector3(0, 0, -1), new Vector3(.5f, 0, 0), new Vector3(0, .5f, 0)); for (int i = 0; i < 4; i++) v[i] = new Vector3(v[i].x, v[i].y, 0); }
            else
            {
                Face(new Vector3(0, 0, -1), new Vector3(.5f, 0, 0), new Vector3(0, .5f, 0)); Face(new Vector3(0, 0, 1), new Vector3(-.5f, 0, 0), new Vector3(0, .5f, 0));
                Face(new Vector3(1, 0, 0), new Vector3(0, 0, .5f), new Vector3(0, .5f, 0)); Face(new Vector3(-1, 0, 0), new Vector3(0, 0, -.5f), new Vector3(0, .5f, 0));
                Face(new Vector3(0, 1, 0), new Vector3(.5f, 0, 0), new Vector3(0, 0, .5f)); Face(new Vector3(0, -1, 0), new Vector3(.5f, 0, 0), new Vector3(0, 0, -.5f));
            }
            m.vertices = v.ToArray(); m.normals = nn.ToArray(); m.uv = uv.ToArray(); m.triangles = t.ToArray();
            g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>(); g.AddComponent<BoxCollider>();
            return g;
        }
        public static GameObject Find(string n) => Registry.FirstOrDefault(g => g.name == n && g.transform.parent == null);
        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T { gameObject = this }; comps.Add(c);
            if (c is TextMesh && GetComponent<MeshRenderer>() == null) comps.Add(new MeshRenderer { gameObject = this });   // Unity adds the renderer itself
            return c;
        }
        public T GetComponent<T>() where T : Component => comps.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool inactive) where T : Component => All<T>();
        public T[] All<T>() where T : Component { var r = new List<T>(); void Walk(Transform t) { r.AddRange(t.gameObject.comps.OfType<T>()); foreach (var c in t.children.ToArray()) Walk(c); } Walk(transform); return r.ToArray(); } }
    public class Behaviour : Component { public bool enabled = true; } public class MonoBehaviour : Behaviour { }
    public enum Space { World, Self }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public struct Vector3Int : IEquatable<Vector3Int> { public int x, y, z; public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public bool Equals(Vector3Int o) => x == o.x && y == o.y && z == o.z; public override bool Equals(object o) => o is Vector3Int v && Equals(v); public override int GetHashCode() => (x * 73856093) ^ (y * 19349663) ^ (z * 83492791); }
    public class Renderer : Component { public Material sharedMaterial; public Material[] sharedMaterials { get => new[] { sharedMaterial }; set { if (value != null && value.Length > 0) sharedMaterial = value[0]; } }
        public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows = true, enabled = true;
        // A preview model carries the real glTF bounds (Unity axes): bounds follow its transform exactly like Unity's AABB.
        public Bounds bounds { get {
            var pm = gameObject.GetComponent<PreviewModel>(); if (pm == null) return new Bounds(transform.position, Vector3.zero);
            Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var c = transform.TransformPoint(new Vector3((i & 1) == 0 ? pm.min.x : pm.max.x, (i & 2) == 0 ? pm.min.y : pm.max.y, (i & 4) == 0 ? pm.min.z : pm.max.z));
                lo = new Vector3(Math.Min(lo.x, c.x), Math.Min(lo.y, c.y), Math.Min(lo.z, c.z)); hi = new Vector3(Math.Max(hi.x, c.x), Math.Max(hi.y, c.y), Math.Max(hi.z, c.z));
            }
            return new Bounds((lo + hi) * .5f, hi - lo); } } }
    // Stand-in for an imported glTF model: its path and the real bounds read from the glTF accessors (x mirrored like glTFast).
    public class PreviewModel : Component { public string path; public Vector3 min, max; }
    public class MeshRenderer : Renderer { }
    public class Mesh : Object { public Vector3[] vertices = new Vector3[0]; public Vector3[] normals; public Vector2[] uv; public int[] triangles = new int[0]; public Bounds bounds;
        public Rendering.IndexFormat indexFormat;
        public void SetVertices(List<Vector3> v) => vertices = v.ToArray(); public void SetNormals(List<Vector3> n) { }
        public void SetUVs(int ch, List<Vector2> u) { if (ch == 0) uv = u.ToArray(); } public void SetTriangles(List<int> t, int sub) => triangles = t.ToArray();
        public void SetUVs(int ch, Vector2[] u) { if (ch == 0) uv = u; } public void SetTriangles(int[] t, int sub) => triangles = t; public void SetVertices(Vector3[] v) => vertices = v;
        public void RecalculateTangents() { } public void RecalculateBounds() { } public void RecalculateUVDistributionMetrics() { }
        // Unity winding: clockwise front, normal = (b - a) x (c - a)
        public void RecalculateNormals()
        {
            normals = new Vector3[vertices.Length];
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                var n = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                for (int k = 0; k < 3; k++) normals[triangles[i + k]] += n;
            }
            for (int i = 0; i < normals.Length; i++) normals[i].Normalize();
        } }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Collider : Component { public bool isTrigger; } public class BoxCollider : Collider { public Vector3 center, size; }
    public class MeshCollider : Collider { public Mesh sharedMesh; public bool convex; } public class CapsuleCollider : Collider { public Vector3 center; public float height, radius; public int direction; }
    public class Font : Object { public Material material = new Material { name = "font" }; } public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight } public enum TextAlignment { Left, Center, Right }
    public static class Resources { public static T GetBuiltinResource<T>(string n) where T : Object, new() => new T { name = n }; public static T Load<T>(string n) where T : Object => null; }
    public class TextMesh : Component { public string text; public Font font; public int fontSize; public float characterSize; public TextAnchor anchor; public TextAlignment alignment; public Color color; }
    public enum LightType { Spot, Directional, Point } public enum LightShadows { None, Hard, Soft } public enum LightRenderMode { Auto, ForcePixel, ForceVertex }
    public class Light : Behaviour { public float range, intensity; public LightType type; public Color color; public LightShadows shadows; public LightRenderMode renderMode; public float spotAngle; public int cullingMask; }
    public class ReflectionProbe : Behaviour { public Rendering.ReflectionProbeMode mode; public int resolution, importance, cullingMask; public Vector3 size, center; public float intensity, nearClipPlane, farClipPlane; public bool boxProjection; public Texture customBakedTexture, bakedTexture; }
    public struct LOD { public LOD(float h, Renderer[] r) { } }
    public class LODGroup : Component { public void SetLODs(LOD[] l) { } public void RecalculateBounds() { } }
    public struct RaycastHit { public Vector3 point, normal; public Collider collider; public float distance; }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public static class Physics { public static void SyncTransforms() { }
        public static float PreviewGroundY = -.25f * BraxClaudeOsiedle.K;   // the surface every downward ray finds (apartments: the plot foundation)
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit h, float dist, int mask, QueryTriggerInteraction q) { h = new RaycastHit { point = new Vector3(o.x, PreviewGroundY, o.z), normal = Vector3.up }; return true; }
        public static RaycastHit[] RaycastAll(Vector3 o, Vector3 d, float dist, int mask, QueryTriggerInteraction q) => new[] { new RaycastHit { point = new Vector3(o.x, PreviewGroundY, o.z), normal = Vector3.up } }; }
    public static class Debug { public static void Log(object o) => Console.WriteLine(o); public static void LogWarning(object o) => Console.WriteLine("WARN " + o); public static void LogError(object o) => Console.WriteLine("ERROR " + o); public static void LogException(Exception e) => Console.WriteLine("ERROR " + e.Message); }
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
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum ReflectionProbeMode { Baked, Realtime, Custom } public enum ReflectionProbeRefreshMode { OnAwake, EveryFrame, ViaScripting }
    public enum BlendMode { Zero = 0, One = 1, DstColor = 2, SrcColor = 3, OneMinusDstColor = 4, SrcAlpha = 5, OneMinusSrcColor = 6, DstAlpha = 7, OneMinusDstAlpha = 8, SrcAlphaSaturate = 9, OneMinusSrcAlpha = 10 }
}
namespace UnityEditor
{
    using UnityEngine;
    public enum InteractionMode { AutomatedAction, UserAction }
    [Flags] public enum ImportAssetOptions { Default = 0, ForceUpdate = 1, ForceSynchronousImport = 8 }
    [Flags] public enum StaticEditorFlags { ContributeGI = 1, OccluderStatic = 2, BatchingStatic = 4, NavigationStatic = 8, OccludeeStatic = 16 }
    public static class AssetDatabase {
        public static T LoadAssetAtPath<T>(string p) where T : Object
        {
            if (typeof(T) == typeof(Material) && p.EndsWith(".mat") && (p.Contains("/Arena/") || p.Contains("/Buildings/Materials/") || p.EndsWith("/sea.mat")))   // a material saved by another generator (LED, glass): a plain warm-emissive stand-in
            { var lm = new Material { name = System.IO.Path.GetFileNameWithoutExtension(p) }; if (p.ToLowerInvariant().Contains("led") || p.ToLowerInvariant().Contains("light")) lm.emission = new Color(.9f, .7f, .45f);
              if (p.ToLowerInvariant().Contains("glass")) { lm.renderQueue = 3000; lm.color = new Color(.78f, .88f, .93f, .19f); } return lm as T; }
            // owner pictures: murals only when the file is really there, small estate graphics / signs always (drawn flat)
            if (typeof(T) == typeof(Texture2D)) return (System.IO.File.Exists(p) || p.Contains("/ArenaGraphics/") || p.Contains("/ArenaSigns/") || p.Contains("/ArenaLobby/")) ? new Texture2D { name = p, real = System.IO.File.Exists(p) } as T : null;
            if (typeof(T) == typeof(Mesh) && p.EndsWith(".asset") && p.Contains("/Buildings/Meshes/")) return new Mesh { name = p } as T;   // a mesh the same run has just saved
            if (typeof(T) == typeof(Shader) && p.EndsWith(".shader") && System.IO.File.Exists(p)) return new Shader { name = p } as T;   // a shader the generator has just written (flags)
            if (typeof(T) == typeof(Texture) && p.EndsWith(".exr")) return new Texture { name = p } as T;   // Editor-baked reflection cubemap
            if (typeof(T) == typeof(GameObject) && p.EndsWith(".glb"))   // embedded GLB (casino vegetation): a stand-in volume, drawn only when the file is there
            {
                var b = new GameObject(System.IO.Path.GetFileNameWithoutExtension(p)); GameObject.Registry.Remove(b);
                var bm = b.AddComponent<PreviewModel>(); bm.path = p; bm.min = new Vector3(-.8f, 0, -.8f); bm.max = new Vector3(.8f, 1.6f, .8f);
                // the preview may hold the same Poly Haven model as a plain .gltf beside it: real bounds, and it is drawn
                var dir = System.IO.Path.GetDirectoryName(p); var twin = System.IO.Directory.Exists(dir) ? System.IO.Directory.GetFiles(dir, "*.gltf").FirstOrDefault() : null;
                if (twin != null) { bm.path = twin.Replace('\\', '/'); (bm.min, bm.max) = GltfBounds.Read(twin); }
                else if (System.IO.File.Exists(p)) (bm.min, bm.max) = GltfBounds.Read(p);   // the real .glb is there: real bounds, drawn
                b.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true }; return b as T;
            }
            if (typeof(T) != typeof(GameObject) || !p.EndsWith(".gltf") || !System.IO.File.Exists(p)) return null;
            var g = new GameObject(System.IO.Path.GetFileNameWithoutExtension(p)); GameObject.Registry.Remove(g);
            var pm = g.AddComponent<PreviewModel>(); pm.path = p; (pm.min, pm.max) = GltfBounds.Read(p);
            g.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true };
            // like glTFast: the imported model has a mesh (generators check for real geometry); the dump skips it, three.js
            // draws the .gltf itself
            g.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = "gltf stand-in (drawn from the file)" };
            return g as T;
        }
        public static void CreateAsset(Object o, string p) { } public static void Refresh(ImportAssetOptions o = ImportAssetOptions.Default) { } public static Object[] LoadAllAssetsAtPath(string p) => new Object[0];
        public static void SaveAssets() { } public static void ImportAsset(string p, ImportAssetOptions o) { } }
    public class AssetImporter : Object { public static AssetImporter GetAtPath(string p) => new TextureImporter { name = p }; public void SaveAndReimport() { } }
    public enum TextureImporterFormat { Automatic, ASTC_4x4, ASTC_6x6, ASTC_8x8 }
    public sealed class TextureImporterPlatformSettings { public bool overridden; public int maxTextureSize, compressionQuality; public TextureImporterFormat format; }
    public class TextureImporter : AssetImporter { public bool streamingMipmaps; public TextureWrapMode wrapMode; public FilterMode filterMode; public int anisoLevel;
        public TextureImporterPlatformSettings GetPlatformTextureSettings(string platform) => new TextureImporterPlatformSettings(); public void SetPlatformTextureSettings(TextureImporterPlatformSettings s) { }
        public void GetSourceTextureWidthAndHeight(out int w, out int h) { bool mural = name != null && name.Contains("6144x2048"); w = mural ? 6144 : 2048; h = mural ? 2048 : 512; } public bool isReadable, mipmapEnabled, sRGBTexture, alphaIsTransparency; public int maxTextureSize; }
    public static class Lightmapping { public static bool BakeReflectionProbe(ReflectionProbe p, string path) => true; }
    public static class EditorUtility { public static void SetDirty(Object o) { } public static void CopySerialized(Object a, Object b) { } }
    public static class ShaderUtil { public static bool ShaderHasError(Shader s) => false; }
    public static class GameObjectUtility { public static void SetStaticEditorFlags(GameObject g, StaticEditorFlags f) { } }
    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAssetAndConnect(GameObject g, string p, InteractionMode m) => g;
        public static GameObject SaveAsPrefabAsset(GameObject g, string p) { var asset = new GameObject(g.name); GameObject.Registry.Remove(asset); Tree(g.transform, asset.transform, false); return asset; }
        // A prefab instance in the preview is a stand-in with the template's empty transform tree (names + local TRS, no
        // meshes): the preview draws the template itself at every level, per-floor additions (3D room numbers on the
        // ROOM NUMBER anchors) are built on the instance like in Unity.
        public static Object InstantiatePrefab(Object o) => Clone(o);
        public static Object InstantiatePrefab(Object o, Transform parent) { var g = Clone(o); g.transform.SetParent(parent, false); return g; }
        static void Tree(Transform from, Transform to, bool register)
        {
            foreach (var c in from.children.ToArray())
            {
                var n = new GameObject(c.gameObject.name); if (!register) GameObject.Registry.Remove(n);
                n.transform.SetParent(to, false); n.transform.localPosition = c.localPosition; n.transform.localRotation = c.localRotation; n.transform.localScale = c.localScale;
                Tree(c, n.transform, register);
            }
        }
        static GameObject Clone(Object o)
        {
            var g = new GameObject(o.name + " (instance)"); var src = (o as GameObject)?.GetComponent<PreviewModel>();
            if (src != null) { var pm = g.AddComponent<PreviewModel>(); pm.path = src.path; pm.min = src.min; pm.max = src.max; g.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "gltf", textured = true }; }
            g.transform.localScale = (o as GameObject)?.transform.localScale ?? Vector3.one;
            if (o is GameObject go && src == null) Tree(go.transform, g.transform, true);
            return g;
        }
    }
}

// Bounds of a glTF 2.0 model from its accessors and node transforms, mirrored on X like glTFast (glTF right-handed -> Unity).
public static class GltfBounds
{
    public static (UnityEngine.Vector3, UnityEngine.Vector3) Read(string path)
    {
        string json;
        var raw = System.IO.File.ReadAllBytes(path);
        if (raw.Length > 20 && raw[0] == (byte)'g' && raw[1] == (byte)'l' && raw[2] == (byte)'T' && raw[3] == (byte)'F')   // binary .glb: the first chunk is the JSON
            json = System.Text.Encoding.UTF8.GetString(raw, 20, (int)BitConverter.ToUInt32(raw, 12));
        else json = System.Text.Encoding.UTF8.GetString(raw);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement; var acc = root.GetProperty("accessors"); var meshes = root.GetProperty("meshes"); var nodes = root.GetProperty("nodes");
        double[] lo = { double.MaxValue, double.MaxValue, double.MaxValue }, hi = { double.MinValue, double.MinValue, double.MinValue };
        double[] Mul(double[] a, double[] b) { var r = new double[16]; for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) { double s = 0; for (int k = 0; k < 4; k++) s += a[i * 4 + k] * b[k * 4 + j]; r[i * 4 + j] = s; } return r; }
        double[] Trs(System.Text.Json.JsonElement n)
        {
            if (n.TryGetProperty("matrix", out var mm)) { var c = mm.EnumerateArray().Select(x => x.GetDouble()).ToArray(); var r = new double[16]; for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) r[i * 4 + j] = c[j * 4 + i]; return r; }
            double tx = 0, ty = 0, tz = 0, qx = 0, qy = 0, qz = 0, qw = 1, sx = 1, sy = 1, sz = 1;
            if (n.TryGetProperty("translation", out var t)) { var a = t.EnumerateArray().Select(x => x.GetDouble()).ToArray(); tx = a[0]; ty = a[1]; tz = a[2]; }
            if (n.TryGetProperty("rotation", out var q)) { var a = q.EnumerateArray().Select(x => x.GetDouble()).ToArray(); qx = a[0]; qy = a[1]; qz = a[2]; qw = a[3]; }
            if (n.TryGetProperty("scale", out var s)) { var a = s.EnumerateArray().Select(x => x.GetDouble()).ToArray(); sx = a[0]; sy = a[1]; sz = a[2]; }
            double[] R = { 1 - 2 * (qy * qy + qz * qz), 2 * (qx * qy - qz * qw), 2 * (qx * qz + qy * qw), 2 * (qx * qy + qz * qw), 1 - 2 * (qx * qx + qz * qz), 2 * (qy * qz - qx * qw), 2 * (qx * qz - qy * qw), 2 * (qy * qz + qx * qw), 1 - 2 * (qx * qx + qy * qy) };
            return new double[] { R[0] * sx, R[1] * sy, R[2] * sz, tx, R[3] * sx, R[4] * sy, R[5] * sz, ty, R[6] * sx, R[7] * sy, R[8] * sz, tz, 0, 0, 0, 1 };
        }
        void Walk(int ni, double[] parent)
        {
            var n = nodes[ni]; var m = Mul(parent, Trs(n));
            if (n.TryGetProperty("mesh", out var mi))
                foreach (var prim in meshes[mi.GetInt32()].GetProperty("primitives").EnumerateArray())
                {
                    var a = acc[prim.GetProperty("attributes").GetProperty("POSITION").GetInt32()];
                    var mn = a.GetProperty("min").EnumerateArray().Select(x => x.GetDouble()).ToArray(); var mx = a.GetProperty("max").EnumerateArray().Select(x => x.GetDouble()).ToArray();
                    for (int i = 0; i < 8; i++)
                    {
                        double px = (i & 1) == 0 ? mn[0] : mx[0], py = (i & 2) == 0 ? mn[1] : mx[1], pz = (i & 4) == 0 ? mn[2] : mx[2];
                        double[] w = { m[0] * px + m[1] * py + m[2] * pz + m[3], m[4] * px + m[5] * py + m[6] * pz + m[7], m[8] * px + m[9] * py + m[10] * pz + m[11] };
                        for (int k = 0; k < 3; k++) { lo[k] = Math.Min(lo[k], w[k]); hi[k] = Math.Max(hi[k], w[k]); }
                    }
                }
            if (n.TryGetProperty("children", out var ch)) foreach (var c in ch.EnumerateArray()) Walk(c.GetInt32(), m);
        }
        var scene = root.GetProperty("scenes")[root.TryGetProperty("scene", out var sc) ? sc.GetInt32() : 0];
        foreach (var n in scene.GetProperty("nodes").EnumerateArray()) Walk(n.GetInt32(), new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 });
        return (new UnityEngine.Vector3((float)-hi[0], (float)lo[1], (float)lo[2]), new UnityEngine.Vector3((float)-lo[0], (float)hi[1], (float)hi[2]));
    }
}

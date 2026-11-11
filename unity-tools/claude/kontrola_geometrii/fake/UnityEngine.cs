// Minimalny zamiennik UnityEngine / UnityEditor do uruchomienia generatorów R1 poza Unity (zapis geometrii + kontrola migotania).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1);
        public static Vector2 right => new Vector2(1, 0); public static Vector2 up => new Vector2(0, 1); public static Vector2 down => new Vector2(0, -1); public static Vector2 left => new Vector2(-1, 0);
        public float sqrMagnitude => x * x + y * y; public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public void Normalize() { var n = normalized; x = n.x; y = n.y; }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object o) => o is Vector2 v && v == this; public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() * 7;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector2 Min(Vector2 a, Vector2 b) => new Vector2(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        public static Vector2 Max(Vector2 a, Vector2 b) => new Vector2(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public float this[int i] { get => i == 0 ? x : i == 1 ? y : z; set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public static Vector3 zero => new Vector3(0, 0, 0); public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0); public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0); public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1); public static Vector3 back => new Vector3(0, 0, -1);
        public float sqrMagnitude => x * x + y * y + z * z; public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector3(x / m, y / m, z / m) : zero; } }
        public void Normalize() { var n = normalized; x = n.x; y = n.y; z = n.z; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object o) => o is Vector3 v && v == this; public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() * 7 ^ z.GetHashCode() * 13;
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static Vector3 Min(Vector3 a, Vector3 b) => new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        public static Vector3 Max(Vector3 a, Vector3 b) => new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Normalize(Vector3 v) => v.normalized;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) { n = n.normalized; return v - n * Dot(v, n); }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) { var t = b - a; float m = t.magnitude; return m <= d || m == 0 ? b : a + t / m * d; }
        public static float Angle(Vector3 a, Vector3 b) { float d = Dot(a.normalized, b.normalized); return (float)(Math.Acos(Math.Max(-1, Math.Min(1, d))) * 180 / Math.PI); }
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => Lerp(a, b, t);
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1); public static Color black => new Color(0, 0, 0); public static Color clear => new Color(0, 0, 0, 0);
        public static Color gray => new Color(.5f, .5f, .5f); public static Color grey => gray; public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0); public static Color blue => new Color(0, 0, 1); public static Color yellow => new Color(1, .92f, .016f);
        public static Color operator *(Color c, float d) => new Color(c.r * d, c.g * d, c.b * d, c.a * d);
        public static Color operator *(float d, Color c) => c * d;
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public float grayscale => .299f * r + .587f * g + .114f * b;
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new Color32((byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), (byte)(Mathf.Clamp01(c.a) * 255));
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var u = new Vector3(q.x, q.y, q.z); float s = q.w;
            return 2f * Vector3.Dot(u, v) * u + (s * s - Vector3.Dot(u, u)) * v + 2f * s * Vector3.Cross(u, v);
        }
        public static Quaternion Inverse(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public static Quaternion AngleAxis(float deg, Vector3 axis)
        {
            axis = axis.normalized; double h = deg * Math.PI / 360.0; float s = (float)Math.Sin(h);
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, (float)Math.Cos(h));
        }
        // Unity: obrót Z, potem X, potem Y (q = qy · qx · qz)
        public static Quaternion Euler(float ex, float ey, float ez) => AngleAxis(ey, Vector3.up) * AngleAxis(ex, Vector3.right) * AngleAxis(ez, Vector3.forward);
        public static Quaternion Euler(Vector3 e) => Euler(e.x, e.y, e.z);
        public static Quaternion LookRotation(Vector3 f) => LookRotation(f, Vector3.up);
        public static Quaternion LookRotation(Vector3 f, Vector3 up)
        {
            var zA = f.normalized; if (zA.sqrMagnitude < 1e-8f) return identity;
            var xA = Vector3.Cross(up, zA); if (xA.sqrMagnitude < 1e-8f) xA = Vector3.Cross(Math.Abs(zA.y) < .9f ? Vector3.up : Vector3.right, zA);
            xA = xA.normalized; var yA = Vector3.Cross(zA, xA);
            return FromBasis(xA, yA, zA);
        }
        static Quaternion FromBasis(Vector3 X, Vector3 Y, Vector3 Z)
        {
            float m00 = X.x, m01 = Y.x, m02 = Z.x, m10 = X.y, m11 = Y.y, m12 = Z.y, m20 = X.z, m21 = Y.z, m22 = Z.z;
            float tr = m00 + m11 + m22; Quaternion q;
            if (tr > 0) { float s = (float)Math.Sqrt(tr + 1) * 2; q = new Quaternion((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, .25f * s); }
            else if (m00 > m11 && m00 > m22) { float s = (float)Math.Sqrt(1 + m00 - m11 - m22) * 2; q = new Quaternion(.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s); }
            else if (m11 > m22) { float s = (float)Math.Sqrt(1 + m11 - m00 - m22) * 2; q = new Quaternion((m01 + m10) / s, .25f * s, (m12 + m21) / s, (m02 - m20) / s); }
            else { float s = (float)Math.Sqrt(1 + m22 - m00 - m11) * 2; q = new Quaternion((m02 + m20) / s, (m12 + m21) / s, .25f * s, (m10 - m01) / s); }
            return q;
        }
        public static Quaternion FromToRotation(Vector3 a, Vector3 b)
        {
            a = a.normalized; b = b.normalized; float d = Vector3.Dot(a, b);
            if (d > .99999f) return identity;
            if (d < -.99999f) { var ax = Vector3.Cross(Vector3.right, a); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.Cross(Vector3.up, a); return AngleAxis(180, ax); }
            var c = Vector3.Cross(a, b); var q = new Quaternion(c.x, c.y, c.z, 1 + d); float m = (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }
        public Vector3 eulerAngles => Vector3.zero;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public static Rect MinMaxRect(float x0, float y0, float x1, float y1) => new Rect(x0, y0, x1 - x0, y1 - y0);
        public float xMin { get => x; set { width += x - value; x = value; } }
        public float yMin { get => y; set { height += y - value; y = value; } }
        public float xMax { get => x + width; set => width = value - x; }
        public float yMax { get => y + height; set => height = value - y; }
        public Vector2 center => new Vector2(x + width * .5f, y + height * .5f);
        public Vector2 size => new Vector2(width, height);
        public bool Contains(Vector2 p) => p.x >= xMin && p.x < xMax && p.y >= yMin && p.y < yMax;
        public bool Contains(Vector3 p) => Contains(new Vector2(p.x, p.y));
        public bool Overlaps(Rect o) => o.xMax > xMin && o.xMin < xMax && o.yMax > yMin && o.yMin < yMax;
    }

    public struct Bounds
    {
        public Vector3 center, extents;
        public Bounds(Vector3 c, Vector3 size) { center = c; extents = size * .5f; }
        public Vector3 size { get => extents * 2f; set => extents = value * .5f; }
        public Vector3 min => center - extents; public Vector3 max => center + extents;
        public void SetMinMax(Vector3 a, Vector3 b) { center = (a + b) * .5f; extents = (b - a) * .5f; }
        public void Encapsulate(Vector3 p) => SetMinMax(Vector3.Min(min, p), Vector3.Max(max, p));
        public void Encapsulate(Bounds b) { Encapsulate(b.min); Encapsulate(b.max); }
        public void Expand(float a) => extents += Vector3.one * a * .5f;
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
        public bool Intersects(Bounds b) => min.x <= b.max.x && max.x >= b.min.x && min.y <= b.max.y && max.y >= b.min.y && min.z <= b.max.z && max.z >= b.min.z;
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI, Infinity = float.PositiveInfinity, Epsilon = float.Epsilon;
        public static float Min(float a, float b) => a < b ? a : b; public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b; public static int Max(int a, int b) => a > b ? a : b;
        public static float Min(params float[] v) => v.Min(); public static float Max(params float[] v) => v.Max();
        public static float Abs(float a) => Math.Abs(a); public static int Abs(int a) => Math.Abs(a);
        public static float Sign(float a) => a >= 0 ? 1f : -1f;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v; public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = -2f * t * t * t + 3f * t * t; return b * t + a * (1f - t); }
        public static float Sqrt(float a) => (float)Math.Sqrt(a); public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Sin(float a) => (float)Math.Sin(a); public static float Cos(float a) => (float)Math.Cos(a); public static float Tan(float a) => (float)Math.Tan(a);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x); public static float Atan(float a) => (float)Math.Atan(a);
        public static float Asin(float a) => (float)Math.Asin(a); public static float Acos(float a) => (float)Math.Acos(a);
        public static float Exp(float a) => (float)Math.Exp(a); public static float Log(float a) => (float)Math.Log(a);
        public static float Floor(float a) => (float)Math.Floor(a); public static float Ceil(float a) => (float)Math.Ceiling(a); public static float Round(float a) => (float)Math.Round(a, MidpointRounding.ToEven);
        public static int FloorToInt(float a) => (int)Math.Floor(a); public static int CeilToInt(float a) => (int)Math.Ceiling(a); public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static float Repeat(float t, float l) => Clamp(t - Floor(t / l) * l, 0, l);
        public static float PingPong(float t, float l) { t = Repeat(t, l * 2); return l - Abs(t - l); }
        public static float MoveTowards(float a, float b, float d) => Abs(b - a) <= d ? b : a + Sign(b - a) * d;
        public static float DeltaAngle(float a, float b) { float d = Repeat(b - a, 360); if (d > 180) d -= 360; return d; }
        public static bool Approximately(float a, float b) => Abs(a - b) < 1e-6f;
        public static float PerlinNoise(float x, float y) { double v = Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453; return (float)(v - Math.Floor(v)); }
    }

    public static class Debug
    {
        public static readonly List<string> Log_ = new List<string>();
        public static void Log(object o) { Log_.Add("LOG " + o); }
        public static void LogWarning(object o) { Log_.Add("WARN " + o); Console.WriteLine("WARN " + o); }
        public static void LogError(object o) { Log_.Add("ERR " + o); Console.WriteLine("ERR " + o); }
    }

    public class Object
    {
        public string name = "";
        public static implicit operator bool(Object o) => !(o is null) && !o.zniszczony;
        internal bool zniszczony;
        public static void DestroyImmediate(Object o) { if (o != null) o.zniszczony = true; }
        public static void Destroy(Object o) => DestroyImmediate(o);
        public int GetInstanceID() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        public static T Instantiate<T>(T o) where T : Object => o;
        public static Object Instantiate(Object o, Transform p, bool w) => o;
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool inactive = false) where T : class => gameObject.GetComponentsInChildren<T>(inactive);
        public T GetComponentInParent<T>() where T : class => gameObject.GetComponentInParent<T>();
        public T AddComponent<T>() where T : Component, new() => gameObject.AddComponent<T>();
        public string tag = "Untagged";
    }
    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour { }

    public class Transform : Component, IEnumerable
    {
        public Transform parentT; public readonly List<Transform> children = new List<Transform>();
        public Vector3 localPosition = Vector3.zero, localScale = Vector3.one; public Quaternion localRotation = Quaternion.identity;
        public new string name { get => gameObject.name; set => gameObject.name = value; }
        public Transform parent { get => parentT; set => SetParent(value, true); }
        public void SetParent(Transform p) => SetParent(p, true);
        public void SetParent(Transform p, bool worldStays)
        {
            Vector3 wp = position; Quaternion wr = rotation;
            if (parentT != null) parentT.children.Remove(this);
            parentT = p; if (p != null) p.children.Add(this);
            if (worldStays) { position = wp; rotation = wr; }
        }
        public Vector3 position
        {
            get => parentT != null ? parentT.TransformPoint(localPosition) : localPosition;
            set => localPosition = parentT != null ? parentT.InverseTransformPoint(value) : value;
        }
        public Quaternion rotation
        {
            get => parentT != null ? parentT.rotation * localRotation : localRotation;
            set => localRotation = parentT != null ? Quaternion.Inverse(parentT.rotation) * value : value;
        }
        public Vector3 lossyScale => parentT != null ? Vector3.Scale(parentT.lossyScale, localScale) : localScale;
        public Vector3 eulerAngles { get => Vector3.zero; set => rotation = Quaternion.Euler(value); }
        public Vector3 localEulerAngles { get => Vector3.zero; set => localRotation = Quaternion.Euler(value); }
        public Vector3 TransformPoint(Vector3 p) { var q = localPosition + localRotation * Vector3.Scale(localScale, p); return parentT != null ? parentT.TransformPoint(q) : q; }
        public Vector3 TransformPoint(float x, float y, float z) => TransformPoint(new Vector3(x, y, z));
        public Vector3 InverseTransformPoint(Vector3 p)
        {
            var q = parentT != null ? parentT.InverseTransformPoint(p) : p;
            var r = Quaternion.Inverse(localRotation) * (q - localPosition);
            return new Vector3(r.x / localScale.x, r.y / localScale.y, r.z / localScale.z);
        }
        public Vector3 TransformDirection(Vector3 d) => rotation * d;
        public Vector3 InverseTransformDirection(Vector3 d) => Quaternion.Inverse(rotation) * d;
        public Vector3 forward { get => rotation * Vector3.forward; set => rotation = Quaternion.LookRotation(value); }
        public Vector3 right => rotation * Vector3.right; public Vector3 up => rotation * Vector3.up;
        public int childCount => children.Count; public Transform GetChild(int i) => children[i];
        public Transform Find(string n) => children.FirstOrDefault(c => c.name == n);
        public void Rotate(Vector3 e) => localRotation = localRotation * Quaternion.Euler(e);
        public void Rotate(float x, float y, float z) => Rotate(new Vector3(x, y, z));
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
        public void SetSiblingIndex(int i) { }
        public IEnumerator GetEnumerator() => children.ToList().GetEnumerator();
    }

    public class GameObject : Object
    {
        public static readonly List<GameObject> Wszystkie = new List<GameObject>();
        public readonly List<Component> comps = new List<Component>();
        public Transform transform; public int layer; public bool activeSelf = true; public string tag = "Untagged";
        public bool isStatic;
        public GameObject() : this("GameObject") { }
        public GameObject(string n) { name = n; transform = new Transform { gameObject = this }; comps.Add(transform); Wszystkie.Add(this); }
        public GameObject(string n, params Type[] t) : this(n) { foreach (var x in t) { var c = (Component)Activator.CreateInstance(x); c.gameObject = this; comps.Add(c); } }
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; comps.Add(c); return c; }
        public Component AddComponent(Type t) { var c = (Component)Activator.CreateInstance(t); c.gameObject = this; comps.Add(c); return c; }
        public T GetComponent<T>() where T : class => comps.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() where T : class => comps.OfType<T>().ToArray();
        public T[] GetComponentsInChildren<T>(bool inactive = false) where T : class
        {
            var l = new List<T>(); void R(Transform t) { l.AddRange(t.gameObject.comps.OfType<T>()); foreach (var c in t.children) R(c); }
            R(transform); return l.ToArray();
        }
        public T GetComponentInParent<T>() where T : class { for (var t = transform; t != null; t = t.parentT) { var c = t.gameObject.GetComponent<T>(); if (c != null) return c; } return null; }
        public void SetActive(bool a) => activeSelf = a;
        public bool activeInHierarchy => activeSelf;
        public static GameObject Find(string n) => Wszystkie.FirstOrDefault(g => g.name == n && g);
    }

    public enum TextureFormat { RGB24, RGBA32, ARGB32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public class Texture : Object { public int width, height; public TextureWrapMode wrapMode; public FilterMode filterMode; }
    public class Texture2D : Texture
    {
        public Color[] px;
        public Texture2D(int w, int h) { width = w; height = h; }
        public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; }
        public void SetPixels(Color[] p) => px = p; public void Apply() { }
        public byte[] EncodeToPNG() => new byte[0];
    }
    public class Shader : Object { public static Shader Find(string n) => new Shader { name = n }; }
    public class Material : Object
    {
        public static readonly List<Material> Wszystkie = new List<Material>();
        public readonly Dictionary<string, float> f = new Dictionary<string, float>();
        public readonly Dictionary<string, Color> c = new Dictionary<string, Color>();
        public readonly Dictionary<string, Texture> t = new Dictionary<string, Texture>();
        public float metry = 1f; public string[] slugi = new string[0]; public string rodzaj = "kolor"; public Shader shader;
        public int renderQueue = 2000;
        public Material(Shader s) { shader = s; Wszystkie.Add(this); }
        public Material(Material m) { name = m.name; Wszystkie.Add(this); }
        public Color color { get => c.TryGetValue("_Color", out var x) ? x : Color.white; set => c["_Color"] = value; }
        public Texture mainTexture { get => t.TryGetValue("_MainTex", out var x) ? x : null; set => t["_MainTex"] = value; }
        public void SetFloat(string n, float v) => f[n] = v; public void SetColor(string n, Color v) => c[n] = v; public void SetTexture(string n, Texture v) => t[n] = v;
        public float GetFloat(string n) => f.TryGetValue(n, out var x) ? x : 0f; public Color GetColor(string n) => c.TryGetValue(n, out var x) ? x : Color.white;
        public bool HasProperty(string n) => true; public void EnableKeyword(string k) { } public void DisableKeyword(string k) { }
        public void SetInt(string n, int v) => f[n] = v;
    }

    public class Mesh : Object
    {
        public Vector3[] vertices = new Vector3[0], normals = new Vector3[0]; public Vector2[] uv = new Vector2[0]; public Vector4[] tangents;
        public Color[] colors; public Color32[] colors32;
        public readonly List<int[]> sub = new List<int[]>();
        public Rendering.IndexFormat indexFormat;
        public int subMeshCount { get => sub.Count; set { while (sub.Count < value) sub.Add(new int[0]); } }
        public int vertexCount => vertices.Length;
        public int[] triangles { get => sub.SelectMany(x => x).ToArray(); set { sub.Clear(); sub.Add(value); } }
        public void SetVertices(List<Vector3> v) => vertices = v.ToArray(); public void SetNormals(List<Vector3> n) => normals = n.ToArray();
        public void SetTangents(List<Vector4> t) => tangents = t.ToArray(); public void SetUVs(int ch, List<Vector2> u) { if (ch == 0) uv = u.ToArray(); }
        public void SetColors(List<Color32> c) => colors32 = c.ToArray(); public void SetColors(List<Color> c) => colors = c.ToArray();
        public void SetTriangles(List<int> t, int s) { subMeshCount = Math.Max(subMeshCount, s + 1); sub[s] = t.ToArray(); }
        public void SetTriangles(int[] t, int s) { subMeshCount = Math.Max(subMeshCount, s + 1); sub[s] = t; }
        public int[] GetTriangles(int s) => sub[s];
        public void RecalculateBounds() { } public void RecalculateTangents() { } public void Optimize() { } public void UploadMeshData(bool b) { }
        public void RecalculateNormals()
        {
            var n = new Vector3[vertices.Length];
            foreach (var t in sub) for (int i = 0; i + 2 < t.Length; i += 3) { var c = Vector3.Cross(vertices[t[i + 1]] - vertices[t[i]], vertices[t[i + 2]] - vertices[t[i]]); n[t[i]] += c; n[t[i + 1]] += c; n[t[i + 2]] += c; }
            normals = n.Select(x => x.normalized).ToArray();
        }
        public Bounds bounds { get { var b = new Bounds(vertices.Length > 0 ? vertices[0] : Vector3.zero, Vector3.zero); foreach (var v in vertices) b.Encapsulate(v); return b; } }
    }
    public class MeshFilter : Component { public Mesh sharedMesh; public Mesh mesh { get => sharedMesh; set => sharedMesh = value; } }
    public class Renderer : Component
    {
        public Material sharedMaterial { get => sharedMaterials.Length > 0 ? sharedMaterials[0] : null; set => sharedMaterials = new[] { value }; }
        public Material[] sharedMaterials = new Material[0]; public Material material { get => sharedMaterial; set => sharedMaterial = value; }
        public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows = true, enabled = true;
        public Bounds bounds => new Bounds(transform.position, Vector3.one);
    }
    public class MeshRenderer : Renderer { }
    public class Collider : Component { public bool enabled = true, isTrigger; }
    public class BoxCollider : Collider { public Vector3 center = Vector3.zero, size = Vector3.one; }
    public class MeshCollider : Collider { public Mesh sharedMesh; public bool convex; }
    public class SphereCollider : Collider { public Vector3 center; public float radius = .5f; }
    public class CapsuleCollider : Collider { public Vector3 center; public float radius = .5f, height = 2f; public int direction = 1; }
    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour { public LightType type = LightType.Point; public Color color = Color.white; public float intensity = 1f, range = 10f, spotAngle = 30f, shadowStrength = 1f; public LightShadows shadows; }
    public class LOD { public float screenRelativeTransitionHeight; public Renderer[] renderers; public LOD(float h, Renderer[] r) { screenRelativeTransitionHeight = h; renderers = r; } }
    public class LODGroup : Component { public LOD[] lods = new LOD[0]; public void SetLODs(LOD[] l) => lods = l; public void RecalculateBounds() { } public LOD[] GetLODs() => lods; }
    public class Rigidbody : Component { public bool isKinematic; }
    public class Animator : Behaviour { }
    public class Animation : Behaviour { }
    public static class Physics { public static void SyncTransforms() { } }
    public static class Random
    {
        static System.Random r = new System.Random(1);
        public static float value => (float)r.NextDouble(); public static float Range(float a, float b) => a + (b - a) * value; public static int Range(int a, int b) => r.Next(a, b);
        public static void InitState(int s) => r = new System.Random(s);
    }
    public static class Application { public static string dataPath = "/tmp"; public static bool isPlaying = false; }
    public static class Time { public static float time, deltaTime = .02f; }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum IndexFormat { UInt16, UInt32 }
}

namespace UnityEditor
{
    using UnityEngine;
    [Flags] public enum StaticEditorFlags { ContributeGI = 1, OccluderStatic = 2, BatchingStatic = 4, NavigationStatic = 8, OccludeeStatic = 16, OffMeshLinkGeneration = 32, ReflectionProbeStatic = 64 }
    public static class GameObjectUtility { public static void SetStaticEditorFlags(GameObject g, StaticEditorFlags f) { g.isStatic = f != 0; } public static StaticEditorFlags GetStaticEditorFlags(GameObject g) => 0; }
    public static class EditorUtility { public static void SetDirty(Object o) { } }
    public enum ImportAssetOptions { Default, ForceSynchronousImport, ForceUpdate }
    public static class AssetDatabase
    {
        public static void ImportAsset(string p, ImportAssetOptions o = ImportAssetOptions.Default) { }
        public static T LoadAssetAtPath<T>(string p) where T : Object => null;
        public static string GenerateUniqueAssetPath(string p) => p;
        public static void CreateAsset(Object o, string p) { } public static void SaveAssets() { } public static void Refresh() { }
    }
    public class AssetImporter : Object { public static AssetImporter GetAtPath(string p) => null; public void SaveAndReimport() { } }
    public class TextureImporter : AssetImporter { public bool sRGBTexture, mipmapEnabled; public TextureWrapMode wrapMode; public int maxTextureSize; }
}

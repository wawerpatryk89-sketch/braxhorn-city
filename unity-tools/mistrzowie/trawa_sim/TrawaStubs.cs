// The few UnityEngine types the 3D grass's mask and clumps (BraxChampionsTrawa3DMaska, BraxChampionsTrawa3DDane) and the sea's
// shore field (BraxChampionsMorzeBrzeg) use, with
// Unity's own arithmetic (Euler order Z, X, Y; TRS = T * R * S), so the simulator runs the real code on the real lawn.
using System;
namespace UnityEngine
{
    public class Object { }
    public class ScriptableObject : Object { }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Max(float a, float b) => a > b ? a : b; public static int Max(int a, int b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b; public static int Min(int a, int b) => a < b ? a : b;
        public static float Abs(float a) => Math.Abs(a);
        public static float Floor(float a) => (float)Math.Floor(a);
        public static int FloorToInt(float a) => (int)Math.Floor(a);
        public static int CeilToInt(float a) => (int)Math.Ceiling(a);
        public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.ToEven);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
        public static float Sin(float a) => (float)Math.Sin(a); public static float Cos(float a) => (float)Math.Cos(a);
        public static float Clamp01(float a) => a < 0f ? 0f : a > 1f ? 1f : a; public static float Sqrt(float a) => (float)Math.Sqrt(a);
    }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    }
    public struct Quaternion
    {
        public float x, y, z, w; public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
            a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        static Quaternion Axis(float ax, float ay, float az, float deg) { float h = deg * (float)Math.PI / 360f, s = (float)Math.Sin(h); return new Quaternion(ax * s, ay * s, az * s, (float)Math.Cos(h)); }
        public static Quaternion Euler(float x, float y, float z) => Axis(0, 1, 0, y) * Axis(1, 0, 0, x) * Axis(0, 0, 1, z);
    }
    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23;
        public static Matrix4x4 TRS(Vector3 t, Quaternion q, Vector3 s)
        {
            float x = q.x, y = q.y, z = q.z, w = q.w;
            float r00 = 1 - 2 * (y * y + z * z), r01 = 2 * (x * y - z * w), r02 = 2 * (x * z + y * w);
            float r10 = 2 * (x * y + z * w), r11 = 1 - 2 * (x * x + z * z), r12 = 2 * (y * z - x * w);
            float r20 = 2 * (x * z - y * w), r21 = 2 * (y * z + x * w), r22 = 1 - 2 * (x * x + y * y);
            return new Matrix4x4 { m00 = r00 * s.x, m01 = r01 * s.y, m02 = r02 * s.z, m03 = t.x, m10 = r10 * s.x, m11 = r11 * s.y, m12 = r12 * s.z, m13 = t.y,
                                   m20 = r20 * s.x, m21 = r21 * s.y, m22 = r22 * s.z, m23 = t.z };
        }
    }
}

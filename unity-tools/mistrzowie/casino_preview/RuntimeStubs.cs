// runtime BraxChampionsObrot (the turning roof dollar) compiles against these
namespace UnityEngine { public class DisallowMultipleComponentAttribute : System.Attribute { }
    public static class Time { public static float unscaledTime, deltaTime, time, unscaledDeltaTime = 1f / 60f; }
    public class Camera : Behaviour { public static Camera main; public RenderTexture targetTexture; public float fieldOfView = 60f, nearClipPlane = .3f, farClipPlane = 1000f; public CameraClearFlags clearFlags; public Color backgroundColor; public void Render() { } public Vector3 WorldToScreenPoint(Vector3 p) => p; } }

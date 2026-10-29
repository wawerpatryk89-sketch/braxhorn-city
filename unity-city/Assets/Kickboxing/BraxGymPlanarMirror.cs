using UnityEngine;

// Attach to the same object as the mirror renderer. Local +Z is the physical
// plane normal, pointing into the room; local XY is the reflective surface.
// The generated scene assigns the existing player camera explicitly.
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class BraxGymPlanarMirror : MonoBehaviour
{
    public Camera sourceCamera;
    public Renderer mirrorRenderer;
    [Range(128, 1024)] public int textureWidth = 768;
    [Range(128, 1024)] public int textureHeight = 384;
    [Range(1, 30)] public float refreshRate = 24f;
    [Min(1)] public float maximumViewDistance = 42f;
    // A reflected camera can be one hall-width behind the mirror. Eighty metres
    // keeps the opposite wall in view in the existing 36-m-wide hall.
    [Min(1)] public float reflectionDistance = 80f;
    [Range(0.005f, 0.1f)] public float clipPlaneOffset = 0.025f;
    [Range(0, 31)] public int mirrorLayer = 9;

    public int RenderCount { get; private set; }
    public float LastRenderTime { get; private set; } = -1f;
    public bool Ready => isActiveAndEnabled && !faulted && RenderCount > 0
        && reflectionTexture && reflectionTexture.IsCreated();
    public RenderTexture ReflectionTexture => reflectionTexture;

    static readonly int ReflectionTextureId = Shader.PropertyToID("_ReflectionTex");
    static readonly int ReflectionMatrixId = Shader.PropertyToID("_ReflectionTextureMatrix");
    static readonly int ReflectionReadyId = Shader.PropertyToID("_ReflectionReady");
    static readonly int MirrorPlaneId = Shader.PropertyToID("_MirrorPlane");
    static bool renderingReflection;

    Camera reflectionCamera;
    RenderTexture reflectionTexture;
    MaterialPropertyBlock properties;
    float nextRenderTime;
    int lastFrame = -1;
    bool faulted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRenderingGuard() { renderingReflection = false; }

    void Awake()
    {
        if (!mirrorRenderer) mirrorRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        nextRenderTime = 0;
        lastFrame = -1;
        RenderCount = 0;
        LastRenderTime = -1;
        faulted = false;
    }

    void Start()
    {
        if (!sourceCamera || !mirrorRenderer || mirrorRenderer.gameObject != gameObject)
        {
            Debug.LogError("Gym mirror requires the player camera and its own mesh renderer.", this);
            enabled = false;
            return;
        }
        if (gameObject.layer != mirrorLayer || (sourceCamera.cullingMask & (1 << mirrorLayer)) == 0)
        {
            Debug.LogError("Gym mirror must use its dedicated mirror layer, visible to the player camera.", this);
            enabled = false;
        }
    }

    // Built-in invokes this only when the renderer is considered for a camera.
    // Source-camera and static guards prevent nested mirrors from rendering.
    void OnWillRenderObject()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || faulted || renderingReflection
            || !sourceCamera || Camera.current != sourceCamera || !mirrorRenderer
            || !mirrorRenderer.enabled || Time.frameCount == lastFrame) return;

        float now = Time.unscaledTime;
        if (now < nextRenderTime) return;
        Vector3 position = transform.position;
        Vector3 normal = transform.forward;
        Vector3 viewer = sourceCamera.transform.position;
        if (Vector3.Dot(normal, viewer - position) <= clipPlaneOffset) return;
        if (mirrorRenderer.bounds.SqrDistance(viewer) > maximumViewDistance * maximumViewDistance) return;
        if (!EnsureResources()) return;

        reflectionCamera.CopyFrom(sourceCamera);
        reflectionCamera.enabled = false;
        reflectionCamera.targetTexture = reflectionTexture;
        reflectionCamera.rect = new Rect(0, 0, 1, 1);
        reflectionCamera.aspect = sourceCamera.aspect;
        reflectionCamera.allowHDR = false;
        reflectionCamera.allowMSAA = false;
        reflectionCamera.depthTextureMode = DepthTextureMode.None;
        reflectionCamera.renderingPath = RenderingPath.Forward;
        reflectionCamera.useOcclusionCulling = false;
        reflectionCamera.cullingMask = sourceCamera.cullingMask
            & ~(1 << Mathf.Clamp(mirrorLayer, 0, 31)) & ~(1 << gameObject.layer);
        reflectionCamera.farClipPlane = Mathf.Max(sourceCamera.nearClipPlane + 1f,
            Mathf.Min(sourceCamera.farClipPlane, reflectionDistance));
        reflectionCamera.ResetProjectionMatrix();

        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, position));
        Matrix4x4 reflection = ReflectionMatrix(plane);
        reflectionCamera.transform.SetPositionAndRotation(reflection.MultiplyPoint(viewer),
            Quaternion.LookRotation(reflection.MultiplyVector(sourceCamera.transform.forward),
                reflection.MultiplyVector(sourceCamera.transform.up)));
        reflectionCamera.worldToCameraMatrix = sourceCamera.worldToCameraMatrix * reflection;

        // Move the clipping surface slightly into the room to avoid rendering
        // the wall behind the glass. The character remains in the retained half-space.
        Vector3 clipPoint = reflectionCamera.worldToCameraMatrix.MultiplyPoint(position + normal * clipPlaneOffset);
        Vector3 clipNormal = reflectionCamera.worldToCameraMatrix.MultiplyVector(normal).normalized;
        Vector4 cameraPlane = new Vector4(clipNormal.x, clipNormal.y, clipNormal.z, -Vector3.Dot(clipPoint, clipNormal));
        reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(cameraPlane);

        Matrix4x4 clipToTexture = Matrix4x4.identity;
        clipToTexture.m00 = 0.5f;
        clipToTexture.m03 = 0.5f;
        // GPU clip Y maps to opposite texture coordinates on D3D/Metal/Vulkan.
        // WebGL uses the positive direction. The GPU matrix includes RT projection changes.
        clipToTexture.m11 = SystemInfo.graphicsUVStartsAtTop ? -0.5f : 0.5f;
        clipToTexture.m13 = 0.5f;
        Matrix4x4 textureMatrix = clipToTexture
            * GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, true)
            * reflectionCamera.worldToCameraMatrix;

        bool previousCulling = GL.invertCulling;
        RenderTexture previousTarget = RenderTexture.active;
        renderingReflection = true;
        lastFrame = Time.frameCount;
        try
        {
            GL.invertCulling = !previousCulling;
            reflectionCamera.Render();
            mirrorRenderer.GetPropertyBlock(properties);
            properties.SetTexture(ReflectionTextureId, reflectionTexture);
            properties.SetMatrix(ReflectionMatrixId, textureMatrix);
            properties.SetVector(MirrorPlaneId, plane);
            properties.SetFloat(ReflectionReadyId, 1);
            mirrorRenderer.SetPropertyBlock(properties);
            RenderCount++;
            LastRenderTime = now;
            float interval = 1f / Mathf.Clamp(refreshRate, 1f, 30f);
            // Keep the average target cadence at 30/60 Hz without catching up
            // a backlog after the player has looked away from the mirror.
            nextRenderTime = now - nextRenderTime > interval ? now + interval : nextRenderTime + interval;
        }
        catch (System.Exception exception)
        {
            faulted = true;
            ClearReflection();
            Debug.LogException(exception, this);
        }
        finally
        {
            GL.invertCulling = previousCulling;
            RenderTexture.active = previousTarget;
            renderingReflection = false;
        }
    }

    bool EnsureResources()
    {
        if (!reflectionCamera)
        {
            var cameraObject = new GameObject("BRAXHORN visible mirror camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = cameraObject.AddComponent<Camera>();
            reflectionCamera.enabled = false;
        }
        int width = Mathf.Clamp(textureWidth, 128, 1024);
        int height = Mathf.Clamp(textureHeight, 128, 1024);
        if (reflectionTexture && reflectionTexture.width == width && reflectionTexture.height == height)
        {
            if (reflectionTexture.IsCreated()) return true;
            if (reflectionTexture.Create()) return true;
        }
        ReleaseTexture();
        reflectionTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        {
            name = "BRAXHORN planar mirror",
            hideFlags = HideFlags.HideAndDontSave,
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        if (reflectionTexture.Create()) return true;
        faulted = true;
        Debug.LogError("Gym mirror render target could not be created.", this);
        ReleaseTexture();
        ClearReflection();
        return false;
    }

    static Matrix4x4 ReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1 - 2 * plane.x * plane.x; m.m01 = -2 * plane.x * plane.y;
        m.m02 = -2 * plane.x * plane.z; m.m03 = -2 * plane.w * plane.x;
        m.m10 = -2 * plane.y * plane.x; m.m11 = 1 - 2 * plane.y * plane.y;
        m.m12 = -2 * plane.y * plane.z; m.m13 = -2 * plane.w * plane.y;
        m.m20 = -2 * plane.z * plane.x; m.m21 = -2 * plane.z * plane.y;
        m.m22 = 1 - 2 * plane.z * plane.z; m.m23 = -2 * plane.w * plane.z;
        return m;
    }

    void ClearReflection()
    {
        if (!mirrorRenderer || properties == null) return;
        mirrorRenderer.GetPropertyBlock(properties);
        properties.SetFloat(ReflectionReadyId, 0);
        properties.SetTexture(ReflectionTextureId, Texture2D.blackTexture);
        mirrorRenderer.SetPropertyBlock(properties);
    }

    void ReleaseTexture()
    {
        if (reflectionCamera) reflectionCamera.targetTexture = null;
        if (!reflectionTexture) return;
        reflectionTexture.Release();
        DestroyOwned(reflectionTexture);
        reflectionTexture = null;
    }

    static void DestroyOwned(Object value)
    {
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }

    void OnDisable()
    {
        ClearReflection();
        ReleaseTexture();
        if (!reflectionCamera) return;
        DestroyOwned(reflectionCamera.gameObject);
        reflectionCamera = null;
    }

    void OnDestroy() { OnDisable(); }
}

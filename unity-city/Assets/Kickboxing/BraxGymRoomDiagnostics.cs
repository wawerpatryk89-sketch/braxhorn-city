using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

// Read-only acceptance evidence. Attach to the existing BraxhornPlayer and wire
// references in the scene builder. Sampling and the JS bridge require ?qa=1.
// This component never moves the player, opens doors, or changes rendering.
[DisallowMultipleComponent]
public sealed class BraxGymRoomDiagnostics : MonoBehaviour
{
    public BraxGymPlanarMirror mirror;
    public Transform wcFixture;
    public BraxCityDoor[] roomDoors = new BraxCityDoor[0];
    public Bounds hallBounds = new Bounds(new Vector3(0, 4.25f, -1), new Vector3(36, 8.5f, 24));
    public Bounds annexBounds = new Bounds(new Vector3(-21.75f, 1.55f, 2.5f), new Vector3(7.5f, 3.1f, 11));
    public string[] privacyViewpointNames = { "entry", "mat", "ring", "bags" };
    public Vector3[] privacyViewpoints = {
        new Vector3(6, 1.65f, 9.5f), new Vector3(2, 1.65f, 1.5f),
        new Vector3(-9, 2.6f, -5.5f), new Vector3(12.4f, 1.65f, -5.4f)
    };
    // World-space offsets from the fixture anchor: bowl and the two upper sides.
    public Vector3[] wcPrivacyTargetOffsets = {
        new Vector3(0, .45f, 0), new Vector3(-.27f, .75f, 0), new Vector3(.27f, .75f, 0)
    };

    const int SampleCapacity = 600;
    readonly float[] frameMilliseconds = new float[SampleCapacity];
    BraxGymPlayer player;
    bool qaEnabled;
    int sampleCount, sampleCursor, captureSequence;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void BXRoomState(string data);
#endif

    void Awake()
    {
        player = GetComponent<BraxGymPlayer>();
        if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var url))
            foreach (string value in url.Query.TrimStart('?').Split('&'))
                if (value == "qa=1") { qaEnabled = true; break; }
    }

    void Update()
    {
        if (!qaEnabled || !player || !player.ready) return;
        float milliseconds = Time.unscaledDeltaTime * 1000f;
        if (milliseconds <= 0 || float.IsNaN(milliseconds) || float.IsInfinity(milliseconds)) return;
        frameMilliseconds[sampleCursor] = milliseconds;
        sampleCursor = (sampleCursor + 1) % SampleCapacity;
        if (sampleCount < SampleCapacity) sampleCount++;
    }

    public void CaptureRoomDiagnostics(string label)
    {
        if (!qaEnabled) return;
        var snapshot = new Snapshot {
            label = label ?? "", sequence = ++captureSequence, frame = Time.frameCount,
            realtimeSeconds = Time.realtimeSinceStartup, build = player ? player.buildId : "",
            playerPosition = transform.position, hallBounds = hallBounds, annexBounds = annexBounds,
            wcConfigured = wcFixture != null, wcPosition = wcFixture ? wcFixture.position : Vector3.zero,
            mirror = CaptureMirror(), doors = CaptureDoors(), frameTiming = CaptureFrameTiming(),
            memory = new MemoryState {
                unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                unityReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                monoUsedBytes = Profiler.GetMonoUsedSizeLong(),
                managedBytes = GC.GetTotalMemory(false),
                note = "Zero Unity profiler counters can mean unavailable on this WebGL player. Managed counters include not-yet-collected objects; no GC is forced. Browser heap is recorded separately."
            },
            device = new DeviceState {
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                screenWidth = Screen.width, screenHeight = Screen.height,
                platform = Application.platform.ToString(), physicalIPhoneValidated = false
            }
        };
        snapshot.allRoomDoorsOpen = snapshot.doors.Length >= 3;
        foreach (DoorState door in snapshot.doors)
            snapshot.allRoomDoorsOpen &= door.configured && door.open;
        snapshot.privacy = CapturePrivacy();
        snapshot.privacyBlocked = 0;
        foreach (PrivacyRay ray in snapshot.privacy) if (ray.blocked) snapshot.privacyBlocked++;
        snapshot.privacySamplePassedWithDoorsOpen = snapshot.wcConfigured && snapshot.allRoomDoorsOpen
            && snapshot.privacy.Length >= 12 && snapshot.privacyBlocked == snapshot.privacy.Length;
        string json = JsonUtility.ToJson(snapshot);
#if UNITY_WEBGL && !UNITY_EDITOR
        BXRoomState(json);
#else
        Debug.Log("BRAXHORN_ROOM_DIAGNOSTICS " + json, this);
#endif
    }

    MirrorState CaptureMirror()
    {
        var result = new MirrorState { configured = mirror != null };
        if (!mirror) return result;
        result.ready = mirror.Ready;
        result.renders = mirror.RenderCount;
        result.lastRenderTime = mirror.LastRenderTime;
        result.refreshRate = mirror.refreshRate;
        result.position = mirror.transform.position;
        result.normal = mirror.transform.forward;
        result.sourceIncludesPlayer = mirror.sourceCamera && (mirror.sourceCamera.cullingMask & (1 << 8)) != 0;
        result.sourceIncludesMirror = mirror.sourceCamera && (mirror.sourceCamera.cullingMask & (1 << mirror.mirrorLayer)) != 0;
        if (mirror.ReflectionTexture) {
            result.width = mirror.ReflectionTexture.width;
            result.height = mirror.ReflectionTexture.height;
            result.textureCreated = mirror.ReflectionTexture.IsCreated();
        }
        return result;
    }

    DoorState[] CaptureDoors()
    {
        var result = new DoorState[roomDoors == null ? 0 : roomDoors.Length];
        for (int i = 0; i < result.Length; i++) {
            BraxCityDoor door = roomDoors[i];
            var value = new DoorState { configured = door != null };
            if (door) {
                value.id = door.id; value.label = door.label; value.open = door.IsOpen;
                value.position = door.transform.position;
                value.leafLocalYaw = new float[door.leaves == null ? 0 : door.leaves.Length];
                for (int j = 0; j < value.leafLocalYaw.Length; j++)
                    value.leafLocalYaw[j] = door.leaves[j] ? Mathf.DeltaAngle(0, door.leaves[j].localEulerAngles.y) : 0;
            }
            result[i] = value;
        }
        return result;
    }

    PrivacyRay[] CapturePrivacy()
    {
        if (!wcFixture || privacyViewpoints == null || wcPrivacyTargetOffsets == null) return new PrivacyRay[0];
        var result = new PrivacyRay[privacyViewpoints.Length * wcPrivacyTargetOffsets.Length];
        int index = 0;
        for (int i = 0; i < privacyViewpoints.Length; i++) {
            for (int j = 0; j < wcPrivacyTargetOffsets.Length; j++) {
                Vector3 from = privacyViewpoints[i], to = wcFixture.position + wcPrivacyTargetOffsets[j];
                float length = Vector3.Distance(from, to);
                var ray = new PrivacyRay {
                    viewpoint = privacyViewpointNames != null && i < privacyViewpointNames.Length ? privacyViewpointNames[i] : "view-" + i,
                    target = j, from = from, to = to, length = length
                };
                // Ignore the player, reflective glass, and triggers. A hit on the
                // WC itself cannot establish privacy: the blocker must be at least
                // 0.65 m before the target, outside the fixture envelope.
                if (Physics.Linecast(from, to, out RaycastHit hit, ~((1 << 8) | (1 << 9)), QueryTriggerInteraction.Ignore)) {
                    ray.hit = hit.collider.name; ray.hitPosition = hit.point; ray.hitDistance = hit.distance;
                    ray.blocked = hit.distance < length - .65f && !hit.collider.bounds.Contains(to);
                }
                result[index++] = ray;
            }
        }
        return result;
    }

    FrameTimingState CaptureFrameTiming()
    {
        var result = new FrameTimingState { samples = sampleCount, capacity = SampleCapacity,
            source = "Time.unscaledDeltaTime; latest ready-player frames; browser/desktop observation only" };
        if (sampleCount == 0) return result;
        var sorted = new float[sampleCount];
        Array.Copy(frameMilliseconds, sorted, sampleCount);
        double sum = 0;
        foreach (float value in sorted) { sum += value; if (value > 1000f / 30f) result.framesAbove33ms++; }
        Array.Sort(sorted);
        result.meanMs = (float)(sum / sampleCount);
        result.p50Ms = sorted[Mathf.Clamp(Mathf.CeilToInt(sampleCount * .50f) - 1, 0, sampleCount - 1)];
        result.p95Ms = sorted[Mathf.Clamp(Mathf.CeilToInt(sampleCount * .95f) - 1, 0, sampleCount - 1)];
        result.p99Ms = sorted[Mathf.Clamp(Mathf.CeilToInt(sampleCount * .99f) - 1, 0, sampleCount - 1)];
        result.maximumMs = sorted[sampleCount - 1]; result.minimumMs = sorted[0];
        result.meanFps = result.meanMs > 0 ? 1000f / result.meanMs : 0;
        result.windowSeconds = (float)(sum / 1000);
        return result;
    }

    [Serializable] sealed class Snapshot {
        public string label, build;
        public int sequence, frame, privacyBlocked;
        public float realtimeSeconds;
        public Vector3 playerPosition, wcPosition;
        public Bounds hallBounds, annexBounds;
        public bool wcConfigured, allRoomDoorsOpen, privacySamplePassedWithDoorsOpen;
        public MirrorState mirror; public DoorState[] doors; public PrivacyRay[] privacy;
        public FrameTimingState frameTiming; public MemoryState memory; public DeviceState device;
    }
    [Serializable] sealed class MirrorState {
        public bool configured, ready, textureCreated, sourceIncludesPlayer, sourceIncludesMirror;
        public int renders, width, height;
        public float lastRenderTime, refreshRate;
        public Vector3 position, normal;
    }
    [Serializable] sealed class DoorState {
        public bool configured, open;
        public string id, label;
        public Vector3 position;
        public float[] leafLocalYaw;
    }
    [Serializable] sealed class PrivacyRay {
        public string viewpoint, hit;
        public int target;
        public bool blocked;
        public Vector3 from, to, hitPosition;
        public float length, hitDistance;
    }
    [Serializable] sealed class FrameTimingState {
        public string source;
        public int samples, capacity, framesAbove33ms;
        public float meanMs, p50Ms, p95Ms, p99Ms, maximumMs, minimumMs, meanFps, windowSeconds;
    }
    [Serializable] sealed class MemoryState {
        public long unityAllocatedBytes, unityReservedBytes, monoUsedBytes, managedBytes;
        public string note;
    }
    [Serializable] sealed class DeviceState {
        public string graphicsDevice, graphicsApi, operatingSystem, platform;
        public int screenWidth, screenHeight;
        public bool physicalIPhoneValidated;
    }
}

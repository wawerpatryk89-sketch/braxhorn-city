using System;
using System.Collections;
using UnityEngine;

// One owner car from the existing estate, parked beside Champion Arena A1.
// Interacts through the EXISTING city action button + joystick + follow camera.
// No invisible barrier, new road, traffic NPC or substitute car mesh is generated.
[DisallowMultipleComponent]
public sealed class BraxChampionsCar : MonoBehaviour, IBraxInterakcja
{
    public Transform driverSeat, doorApproach, doorHinge;
    // Hinge axis in the hinge's local space. Up = ordinary swing door; the
    // owner's Revuelto uses scissor doors (lateral axis, set by the builder).
    public Vector3 doorAxis = Vector3.up;
    public BoxCollider bodyCollider;
    public float maxSpeed = 200f / 3.6f, reverseMaxSpeed = 30f / 3.6f; // owner 10.11 00:43: "samochód ma jeździć 200 jednak"
    // World units per real metre (K). Speeds are stored in world units; the
    // builder sets maxSpeed = 120/3.6 x K so a real 120 km/h is reached.
    public float worldScale = 1f;
    // Acceleration fades toward top speed: 7.2 m/s2 launch, 0-120 km/h in ~6.7 s.
    public float topEndAccelerationFactor = .45f;
    public float wheelbase = 2.7f, maxSteer = 34f, highSpeedSteerFactor = .16f;
    public float interactionRadius = 3.8f;
    public float acceleration = 5.5f, braking = 9.5f;
    // Owner 10.11 08:40: "hamulec ręczny, bo się trochę pobawię" and "joystickiem: jak lekko - jedzie wolno, jak na
    // maksa - zapierdala, regulacja tym joystickiem". The JUMP button (and Space on a desktop) pulls the handbrake for
    // handbrakeHold seconds per press: hard rear braking, no throttle, the car rotates more than it travels (drift).
    public float handbrakeHold = .9f, handbrakeBrakeFactor = 1.25f, driftSteerBoost = 1.7f;
    public float driftGrip = 2.2f, normalGrip = 10f;
    // Throttle = how far the stick is pushed (its whole length, so a diagonal push while steering keeps the gas),
    // beyond the .08 dead zone, raised to throttleCurve: 20 % = ~8 km/h, half = ~57 km/h, full = 200 km/h.
    // Easing the stick lets the car roll down at coastDecel (engine braking); pulling it back brakes at `braking`.
    public float throttleCurve = 1.6f, coastDecel = 4.5f;
    // Owner 10.11 12:41 (APK 37): "gwałtownie skręca nagle". The wheel jumped to the stick's sideways value in one
    // frame and nothing limited the yaw rate (at 100 km/h a half push swung the car ~140 deg/s). Now: dead zone + curve
    // on the stick (a light touch = a light correction), the wheel turns at steerRate (full lock ~.45 s) and comes back
    // at steerReturn, and the yaw rate never exceeds what the tyres hold: lateralGrip (m/s2, ~0.9 g) / speed.
    public float steerDeadZone = .10f, steerCurve = 1.6f, steerRate = 2.2f, steerReturn = 4.0f;
    public float lateralGrip = 8.8f, driftLateralFactor = 1.7f;
    public float groundProbeReach = 2.5f, maxGroundStep = .80f, collisionSkin = .10f;
    // Owner phone video 9.11: the car stopped at the first kerb/parking stop.
    // Low edges (kerbs ~.19-.26 m in game scale, parking stops, tactile strips)
    // are driven over like real tyres; anything taller still blocks.
    public float climbStep = .38f, climbRate = 3.2f, dropRate = 5.5f;
    public Transform[] rollingWheels = new Transform[0];
    public Transform[] steeringWheels = new Transform[0];

    // Issue #30 engine audio: real recorded clips only. No procedural fake engine.
    public Transform engineAudioAnchor;
    // Owner 9.11 "zrób V8": three cross-plane V8 loops (idle 850, low 2200,
    // high 4800 rpm). engineRevClip keeps its field name = the 2200 rpm loop.
    public AudioClip engineIdleClip, engineRevClip, engineHighClip;
    public float engineMinDistance = 2.2f, engineMaxDistance = 36f;

    public BraxGymPlayer Driver => driver;
    public bool HandbrakeOn => Time.time < handbrakeUntil;
    public void PullHandbrake()
    {
        if (!driver || transition) return;
        if (!HandbrakeOn) Debug.Log("A1_CAR_HANDBRAKE speedKph=" + SpeedKph.ToString("F0"));
        handbrakeUntil = Time.time + handbrakeHold;
    }
    /// <summary>Handbrake held by the on-screen button (BraxChampionsCarHud): on while the finger stays on it.</summary>
    public void HoldHandbrake(float seconds)
    {
        if (!driver || transition) return;
        if (!HandbrakeOn) Debug.Log("A1_CAR_HANDBRAKE button speedKph=" + SpeedKph.ToString("F0"));
        handbrakeUntil = Mathf.Max(handbrakeUntil, Time.time + seconds);
    }
    public float Speed => speed;
    public float SpeedKph => Mathf.Abs(speed) * 3.6f / Mathf.Max(.01f, worldScale);
    public bool Transitioning => transition;
    const int PlayerLayer = 8;
    readonly RaycastHit[] groundHits = new RaycastHit[24];
    readonly RaycastHit[] obstacleHits = new RaycastHit[24];
    Rigidbody physicsBody;
    BraxGymPlayer driver;
    Vector2 control;
    float speed, doorAngle, wheelAngle, handbrakeUntil, steerInput;
    Vector3 travelDir;
    float groundPivotOffset, nextBlockDiagnostic;
    float engineLoad, idleGain;
    float nextRevAllowed, engineRpm = 850f;
    int engineGear = 1;
    AudioSource engineHighSource;
    bool revArmed = true;
    bool transition;
    Quaternion doorRest;
    AudioSource engineIdleSource, engineRevSource;

    void Awake()
    {
        physicsBody = GetComponent<Rigidbody>();
        if (!physicsBody || !bodyCollider || !driverSeat || !doorApproach)
            throw new InvalidOperationException("A1 owner's car is incomplete: real chassis, driver seat and entrance required");
        physicsBody.isKinematic = true;
        physicsBody.useGravity = false;
        physicsBody.interpolation = RigidbodyInterpolation.Interpolate;
        Physics.SyncTransforms();
        // Preserve the authored wheel/body clearance: the vehicle root is not necessarily at tyre level.
        groundPivotOffset = transform.position.y - bodyCollider.bounds.min.y;
        if (doorHinge) doorRest = doorHinge.localRotation;
        SetupEngineAudio();
    }

    void OnEnable()
    {
        BraxInterakcje.Dodaj(this);
        StartIdleIfReady();
    }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        EmergencyExit();
        if (engineIdleSource) engineIdleSource.Stop();
        if (engineRevSource) engineRevSource.Stop();
    }

    public string Kontekst(Vector3 position)
    {
        if (driver)
            return transition ? "WSIADANIE / WYSIADANIE"
                : Mathf.Abs(speed) > .25f ? "ZATRZYMAJ AUTO, ABY WYSIĄŚĆ"
                : "WYSIĄDŹ · AUTO BRAXHORN";
        if (transition || !doorApproach) return "";
        return (position - doorApproach.position).sqrMagnitude <= interactionRadius * interactionRadius
            ? "WSIĄDŹ · AUTO BRAXHORN" : "";
    }

    public bool Wykonaj(Vector3 position)
    {
        if (driver)
        {
            if (!transition && Mathf.Abs(speed) < .25f)
            {
                Vector3 exit;
                if (SafeExit(out exit)) StartCoroutine(Exit(exit));
                else Debug.LogWarning("A1_CAR_EXIT_PATH_ADVISORY: real ground or free player capsule missing");
            }
            return true;
        }
        if (transition || (position - doorApproach.position).sqrMagnitude > interactionRadius * interactionRadius)
            return false;
        var player = FindAnyObjectByType<BraxGymPlayer>();
        if (!player || !player.body || !player.clips || !player.cityWorld ||
            player.cityWorld.area != "mistrzowie" || player.seat || player.vessel ||
            player.championsCar) return false;
        foreach (var name in new[] { "EnteringCar", "Driving", "ExitingCar" })
            if (!player.clips.GetClip(name))
            {
                Debug.LogWarning("A1_CAR_MIXAMO_REQUIRED: missing owner Brax clip " + name);
                return false;
            }
        StartCoroutine(Board(player));
        return true;
    }

    public void Steer(Vector2 input)
    {
        control = Vector2.ClampMagnitude(input, 1f);
    }

    AudioSource CreateEngineSource(string name, AudioClip clip, bool loop)
    {
        var host = engineAudioAnchor ? engineAudioAnchor.gameObject : gameObject;
        var source = host.AddComponent<AudioSource>();
        source.name = name;
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = engineMinDistance;
        source.maxDistance = engineMaxDistance;
        source.dopplerLevel = .18f;
        source.spread = 35f;
        source.priority = 96;
        source.volume = 0f;
        return source;
    }

    void SetupEngineAudio()
    {
        if (!engineIdleClip || !engineRevClip)
        {
            Debug.LogWarning("A1_CAR_AUDIO_PENDING: Issue #30 requires real idle+rev recordings; binary clips are not assigned");
            return;
        }
        if (!engineAudioAnchor) engineAudioAnchor = transform;
        if (!engineIdleSource) engineIdleSource = CreateEngineSource("BRAXHORN engine | idle 3D", engineIdleClip, true);
        if (!engineRevSource) engineRevSource = CreateEngineSource("BRAXHORN V8 | 2200 rpm loop 3D", engineRevClip, true);
        if (!engineHighSource && engineHighClip) engineHighSource = CreateEngineSource("BRAXHORN V8 | 4800 rpm loop 3D", engineHighClip, true);
        StartIdleIfReady();
        Debug.Log("A1_CAR_AUDIO_READY idle=" + engineIdleClip.name + " rev=" + engineRevClip.name
            + " spatial=3D min=" + engineMinDistance.ToString("F1")
            + " max=" + engineMaxDistance.ToString("F1"));
    }

    void StartIdleIfReady()
    {
        if (!engineIdleSource || !engineIdleClip || engineIdleSource.isPlaying) return;
        engineIdleSource.pitch = 1f;
        engineIdleSource.volume = 0f;
        idleGain = 0f;
        engineRpm = V8IdleRpm;
        engineIdleSource.Play();
        if (engineRevSource && engineRevClip && !engineRevSource.isPlaying) engineRevSource.Play();
        if (engineHighSource && engineHighClip && !engineHighSource.isPlaying) engineHighSource.Play();
    }


    // Engine model (owner 9.11: "chodzi źle i źle się jeździ"): the old
    // Lada recording only moved its pitch +-7%, so 30 and 120 km/h sounded the
    // same. Now a 6-speed box turns REAL speed into rpm; three V8 loops are
    // pitched to that rpm and crossfaded in log-rpm, with real upshifts.
    const float V8IdleRpm = 850f, V8Redline = 6800f, V8Upshift = 6200f, V8Downshift = 3400f;
    static readonly float[] V8LoopRpm = { 850f, 2200f, 4800f };
    // real km/h at redline per gear (gear 6 reaches the 200 km/h limit at ~6000 rpm)
    static readonly float[] V8GearTopKph = { 0f, 62f, 98f, 134f, 168f, 198f, 228f };

    void UpdateEngineAudio()
    {
        if (!engineIdleSource || !engineIdleClip) return;
        if (!engineIdleSource.isPlaying) StartIdleIfReady();

        float dt = Mathf.Max(.001f, Time.unscaledDeltaTime);
        float throttle = Mathf.Clamp01(Mathf.Abs(control.y));
        float kph = Mathf.Abs(SpeedKph);
        engineLoad = Mathf.MoveTowards(engineLoad, throttle, dt * (throttle > engineLoad ? 4f : 2.5f));

        // gearbox with hysteresis
        float GearRpm(int g) => kph / V8GearTopKph[g] * V8Redline;
        if (engineGear < 6 && GearRpm(engineGear) > V8Upshift) engineGear++;
        else if (engineGear > 1 && GearRpm(engineGear) < V8Downshift && GearRpm(engineGear - 1) < V8Upshift - 400f) engineGear--;
        if (kph < 3f) engineGear = 1;
        float rolling = Mathf.Max(V8IdleRpm * 1.15f, GearRpm(engineGear));
        float target = kph < 3f ? V8IdleRpm + engineLoad * 2600f : Mathf.Max(rolling, V8IdleRpm + engineLoad * 900f);
        if (!driver) target = V8IdleRpm;
        engineRpm = Mathf.MoveTowards(engineRpm, Mathf.Min(target, V8Redline),
            dt * (target > engineRpm ? 7000f : 9000f));

        float master = driver ? Mathf.Lerp(.42f, .86f, engineLoad) : 0f;
        idleGain = Mathf.MoveTowards(idleGain, master, dt / .25f);
        // log-rpm crossfade between the three loops
        float l = Mathf.Log(engineRpm);
        float l0 = Mathf.Log(V8LoopRpm[0]), l1 = Mathf.Log(V8LoopRpm[1]), l2 = Mathf.Log(V8LoopRpm[2]);
        float w0 = Mathf.Clamp01((l1 - l) / (l1 - l0));
        float w2 = engineHighSource ? Mathf.Clamp01((l - l1) / (l2 - l1)) : 0f;
        float w1 = Mathf.Clamp01(1f - w0 - w2);
        if (!engineRevSource) { w0 = 1f; w1 = 0f; w2 = 0f; }
        void Drive(AudioSource src, float weight, float loopRpm)
        {
            if (!src) return;
            src.volume = idleGain * Mathf.Sqrt(weight);           // equal-power crossfade
            src.pitch = Mathf.Clamp(engineRpm / loopRpm, .5f, 2.2f);
        }
        Drive(engineIdleSource, w0, V8LoopRpm[0]);
        Drive(engineRevSource, w1, V8LoopRpm[1]);
        Drive(engineHighSource, w2, V8LoopRpm[2]);
    }

    void Update()
    {
        UpdateEngineAudio();
#if UNITY_EDITOR || UNITY_STANDALONE
        if (driver && !transition && Input.GetKey(KeyCode.Space)) handbrakeUntil = Mathf.Max(handbrakeUntil, Time.time + .12f);
#endif
    }

    static float TransitionSeconds(AnimationClip clip)
        => clip ? Mathf.Clamp(clip.length, .45f, 3.4f) : 1f;

    static string BoneName(string raw)
    {
        string s = raw ?? "";
        int slash = s.LastIndexOf('/');
        if (slash >= 0) s = s.Substring(slash + 1);
        int pipe = s.LastIndexOf('|');
        if (pipe >= 0) s = s.Substring(pipe + 1);
        int colon = s.LastIndexOf(':');
        if (colon >= 0) s = s.Substring(colon + 1);
        s = s.Trim();
        if (s.StartsWith("mixamorig", StringComparison.OrdinalIgnoreCase))
            s = s.Substring("mixamorig".Length).TrimStart(':', '_', '-', '.', ' ');
        return s;
    }

    static Transform FindDriverHips(Transform root)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(BoneName(t.name), "Hips", StringComparison.OrdinalIgnoreCase))
                return t;
        return null;
    }

    IEnumerator MoveDoor(float target, float seconds)
    {
        if (!doorHinge) yield break; // No new pretend doors when source mesh is merged.
        float from = doorAngle;
        float duration = Mathf.Max(.1f, seconds);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            doorAngle = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration)));
            doorHinge.localRotation = doorRest * Quaternion.AngleAxis(doorAngle, doorAxis);
            yield return null;
        }
        doorAngle = target;
        doorHinge.localRotation = doorRest * Quaternion.AngleAxis(doorAngle, doorAxis);
    }

    IEnumerator Board(BraxGymPlayer p)
    {
        transition = true;
        driver = p;
        p.championsCar = this;
        control = Vector2.zero;
        speed = 0f;
        travelDir = transform.forward;
        handbrakeUntil = 0f; steerInput = 0f;
        // the touch buttons SendMessage Command(key) to the player: this relay turns JUMP into the handbrake
        if (!p.GetComponent<BraxChampionsCarCommands>()) p.gameObject.AddComponent<BraxChampionsCarCommands>();
        // Owner 10.11 12:41: "nie ma żadnego hamulca ręcznego, żaden się przycisk nie pokazuje" - the city touch controls
        // hide every fight button (JUMP too) while driving, so the car draws its own HAMULEC button.
        if (!GetComponent<BraxChampionsCarHud>()) gameObject.AddComponent<BraxChampionsCarHud>().car = this;
        p.body.enabled = false;
        var clip = p.clips.GetClip("EnteringCar");
        Vector3 start = p.transform.position;
        Quaternion startRot = p.transform.rotation;
        Transform hips = FindDriverHips(p.transform);
        Vector3 rootToHips = hips ? hips.position - start : Vector3.zero;
        Quaternion seatDelta = driverSeat.rotation * Quaternion.Inverse(startRot);
        Vector3 seatedRoot = hips ? driverSeat.position - seatDelta * rootToHips : driverSeat.position;
        if (!hips)
            Debug.LogWarning("A1_CAR_DRIVER_HIPS_MISSING: fallback root-to-seat used; verify original Brax skeleton");
        Vector3 door = doorApproach.position;
        p.clips.CrossFade("EnteringCar", .14f, PlayMode.StopAll);
        yield return MoveDoor(-65f, .38f);
        float duration = TransitionSeconds(clip);
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
            p.transform.position = Vector3.Lerp(start, Vector3.Lerp(door, seatedRoot, t), t);
            p.transform.rotation = Quaternion.Slerp(startRot, driverSeat.rotation, t);
            yield return null;
        }
        // driverSeat is a HIPS target, not Brax's root/feet target.
        // Parenting the character root directly to the seat put a tall Brax on the roof.
        p.transform.SetParent(driverSeat, true);
        p.transform.localRotation = Quaternion.identity;
        if (hips) p.transform.position += driverSeat.position - hips.position;
        else p.transform.localPosition = Vector3.zero;
        p.clips["Driving"].wrapMode = WrapMode.Loop;
        p.clips.CrossFade("Driving", .18f, PlayMode.StopAll);
        yield return null;
        if (hips) p.transform.position += driverSeat.position - hips.position;
        yield return MoveDoor(0f, .35f);
        transition = false;
        float hipError = hips ? Vector3.Distance(hips.position, driverSeat.position) : -1f;
        Debug.Log("A1_CAR_BOARDED: original owner car_rev + original Brax Mixamo Driving"
            + " hipsAnchored=" + (hips != null) + " hipSeatError=" + hipError.ToString("F3") + "m");
    }

    bool SafeExit(out Vector3 feet)
    {
        feet = doorApproach.position;
        if (!driver || !driver.body) return false;
        float radius = driver.body.radius;
        float height = driver.body.height;
        // A visible car and real terrain are allowed physics. No hidden walls.
        // Find real walkable terrain just outside the actual driver-side door.
        int n = Physics.RaycastNonAlloc(feet + Vector3.up * 2f,
            Vector3.down, groundHits, 5f, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        bool ground = false;
        float best = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var h = groundHits[i];
            if (!h.collider || h.collider.transform.IsChildOf(transform) || h.normal.y < .65f) continue;
            if (h.point.y > best) { best = h.point.y; ground = true; }
        }
        if (!ground) return false;
        feet.y = best + .04f;
        int overlaps = Physics.OverlapCapsuleNonAlloc(
            feet + Vector3.up * radius,
            feet + Vector3.up * (height - radius),
            radius, overlapScratch, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps; i++)
        {
            var c = overlapScratch[i];
            if (!c || c.transform.IsChildOf(transform)) continue;
            return false;
        }
        return true;
    }
    readonly Collider[] overlapScratch = new Collider[24];

    IEnumerator Exit(Vector3 spot)
    {
        transition = true;
        control = Vector2.zero;
        speed = 0f;
        var p = driver;
        yield return MoveDoor(-65f, .35f);
        p.clips.CrossFade("ExitingCar", .16f, PlayMode.StopAll);
        Vector3 from = p.transform.position;
        float duration = TransitionSeconds(p.clips.GetClip("ExitingCar"));
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            p.transform.position = Vector3.Lerp(from, spot,
                Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration)));
            yield return null;
        }
        p.transform.SetParent(null, true);
        p.transform.position = spot;
        p.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        p.championsCar = null;
        p.body.enabled = true;
        p.clips.CrossFade("Idle", .16f, PlayMode.StopAll);
        driver = null;
        yield return MoveDoor(0f, .35f);
        transition = false;
        Debug.Log("A1_CAR_EXITED: original owner Mixamo Exiting Car");
    }

    // Called by the EXISTING Reset command and on scene unload; never leave
    // the player's CharacterController disabled or parented to an old car.
    public void EmergencyExit()
    {
        StopAllCoroutines();
        if (driver)
        {
            var p = driver;
            p.transform.SetParent(null, true);
            p.championsCar = null;
            if (p.body) p.body.enabled = true;
            if (p.clips && p.clips.GetClip("Idle")) p.clips.Play("Idle");
        }
        driver = null; speed = 0f; control = Vector2.zero; steerInput = 0f; transition = false;
        revArmed = true;
        if (engineRevSource) engineRevSource.Stop();
        if (doorHinge) { doorHinge.localRotation = doorRest; doorAngle = 0; }
    }

    // One tyre contact: the real surface under this probe that is closest to the
    // current road height, accepting a climb of at most climbStep and a drop of
    // at most maxGroundStep (kerb down, ramp, parking edge).
    bool ProbeGround(Vector3 probe, float expectedGroundY, out float surfaceY)
    {
        surfaceY = expectedGroundY;
        float bestDelta = float.PositiveInfinity;
        int n = Physics.RaycastNonAlloc(probe + Vector3.up * groundProbeReach,
            Vector3.down, groundHits, groundProbeReach * 2f,
            ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var hit = groundHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform) || hit.normal.y < .55f) continue;
            float rise = hit.point.y - expectedGroundY;
            if (rise > climbStep + .02f || rise < -maxGroundStep) continue;
            float deltaY = Mathf.Abs(rise);
            if (deltaY >= bestDelta) continue;
            bestDelta = deltaY;
            surfaceY = hit.point.y;
        }
        return bestDelta < float.PositiveInfinity;
    }

    bool DriveableGround(Vector3 next, Quaternion nextRotation, out Vector3 groundPosition)
    {
        groundPosition = next;
        // Compare road height with the current tyre/chassis bottom, not with the vehicle pivot.
        float currentPivotY = physicsBody ? physicsBody.position.y : transform.position.y;
        float expectedGroundY = currentPivotY - groundPivotOffset;
        Vector3 scale = transform.lossyScale;
        float halfX = bodyCollider ? bodyCollider.size.x * .32f * Mathf.Abs(scale.x) : .5f;
        float halfZ = bodyCollider ? bodyCollider.size.z * .32f * Mathf.Abs(scale.z) : 1f;
        Vector3 right = nextRotation * Vector3.right * halfX;
        Vector3 forward = nextRotation * Vector3.forward * halfZ;
        // Four tyre contacts + centre. The chassis rests on the HIGHEST supported
        // contact, so a front wheel on the kerb lifts the car instead of the
        // old "closest to the road" rule that pushed the bumper into the kerb.
        int supported = 0;
        float highest = float.NegativeInfinity;
        foreach (var probe in new[] { next, next + right + forward, next - right + forward,
                                      next + right - forward, next - right - forward })
        {
            if (!ProbeGround(probe, expectedGroundY, out float y)) continue;
            supported++;
            if (y > highest) highest = y;
        }
        if (supported == 0) return false;
        float targetPivotY = highest + groundPivotOffset;
        float rate = targetPivotY > currentPivotY ? climbRate : dropRate;
        groundPosition.y = Mathf.MoveTowards(currentPivotY, targetPivotY, rate * Time.fixedDeltaTime);
        return true;
    }

    bool ObstacleOnVisibleCarPath(Vector3 advance, Quaternion nextRotation, out RaycastHit blocker)
    {
        blocker = default;
        if (advance.sqrMagnitude < .000001f || !bodyCollider) return false;
        Vector3 scale = transform.lossyScale;
        Vector3 absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        var half = Vector3.Scale(bodyCollider.size * .5f, absScale);
        float skin = Mathf.Max(.03f, collisionSkin);
        half.x = Mathf.Max(.12f, half.x - skin);
        half.z = Mathf.Max(.20f, half.z - skin);
        // Sweep only the part of the body ABOVE the climbable step. A kerb,
        // parking stop or lowered crossing edge is handled by the tyre probes;
        // walls, poles, bollards, benches, trees and buildings still block.
        float fullHalfY = half.y;
        float bottom = -fullHalfY + Mathf.Max(climbStep, skin);
        float top = fullHalfY - skin;
        if (top - bottom < .24f) bottom = top - .24f;
        half.y = (top - bottom) * .5f;
        Vector3 localCenter = Vector3.Scale(bodyCollider.center, scale);
        var center = physicsBody.position + nextRotation * localCenter + Vector3.up * ((top + bottom) * .5f);
        int count = Physics.BoxCastNonAlloc(center, half,
            advance.normalized, obstacleHits, nextRotation, advance.magnitude + skin,
            ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = obstacleHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y > .70f) continue; // real road/floor, not a wall
            blocker = hit;
            return true;
        }
        return false;
    }

    static string V(Vector3 v)
        => "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";

    void LogDriveBlock(string reason, Vector3 next, RaycastHit blocker)
    {
        if (Time.unscaledTime < nextBlockDiagnostic) return;
        nextBlockDiagnostic = Time.unscaledTime + .35f;
        if (blocker.collider)
        {
            var c = blocker.collider;
            var b = c.bounds;
            string parent = c.transform.parent ? c.transform.parent.name : "<root>";
            Debug.LogWarning("A1_CAR_DRIVE_BLOCK reason=" + reason
                + " carPos=" + V(transform.position) + " next=" + V(next)
                + " speedKph=" + SpeedKph.ToString("F1")
                + " collider=" + c.name + " instance=" + c.GetInstanceID()
                + " layer=" + c.gameObject.layer + " parent=" + parent
                + " point=" + V(blocker.point) + " normal=" + V(blocker.normal)
                + " distance=" + blocker.distance.ToString("F3")
                + " boundsCenter=" + V(b.center) + " boundsSize=" + V(b.size));
        }
        else
            Debug.LogWarning("A1_CAR_DRIVE_BLOCK reason=" + reason
                + " carPos=" + V(transform.position) + " next=" + V(next)
                + " speedKph=" + SpeedKph.ToString("F1")
                + " groundProbeReach=" + groundProbeReach.ToString("F2")
                + " maxGroundStep=" + maxGroundStep.ToString("F2"));
    }

    void FixedUpdate()
    {
        if (!driver || transition || !physicsBody || !bodyCollider) return;
        float dt = Time.fixedDeltaTime;
        bool handbrake = HandbrakeOn;
        float deflection = control.magnitude;
        // forward share of the push: up to ~55 degrees off straight counts in full, a pure sideways push gives no gas
        float forward = deflection < .08f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.20f, .58f, Mathf.Abs(control.y) / deflection));
        float amount = deflection < .08f ? 0f : Mathf.Pow(Mathf.Clamp01((deflection - .08f) / .92f), throttleCurve) * forward;
        float target = handbrake || amount <= 0f ? 0f
            : control.y >= 0f ? amount * maxSpeed : -amount * reverseMaxSpeed;
        bool changingDirection = speed * target < 0f;
        float launch = Mathf.Lerp(1f, topEndAccelerationFactor,
            Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(.01f, maxSpeed)));
        float rate = handbrake ? braking * handbrakeBrakeFactor
            : changingDirection ? braking
            : Mathf.Abs(target) < Mathf.Abs(speed) ? coastDecel : acceleration * launch;
        speed = Mathf.MoveTowards(speed, target, rate * dt);

        float speed01 = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(.01f, maxSpeed));
        float steerFactor = Mathf.Lerp(1f, highSpeedSteerFactor, speed01 * speed01);
        bool sliding = handbrake && Mathf.Abs(speed) > 2f * Mathf.Max(.01f, worldScale);
        float raw = Mathf.Clamp(control.x, -1f, 1f);
        float shaped = Mathf.Abs(raw) < steerDeadZone ? 0f
            : Mathf.Sign(raw) * Mathf.Pow((Mathf.Abs(raw) - steerDeadZone) / (1f - steerDeadZone), steerCurve);
        bool easing = Mathf.Abs(shaped) < Mathf.Abs(steerInput) || shaped * steerInput < 0f;
        steerInput = Mathf.MoveTowards(steerInput, shaped, (easing ? steerReturn : steerRate) * dt);
        float yaw = steerInput * maxSteer * steerFactor * (sliding ? driftSteerBoost : 1f);
        float turn = speed / Mathf.Max(1f, wheelbase) * Mathf.Tan(yaw * Mathf.Deg2Rad);
        float scale = Mathf.Max(.01f, worldScale);
        float maxTurn = lateralGrip * scale * (sliding ? driftLateralFactor : 1f) / Mathf.Max(.5f * scale, Mathf.Abs(speed));
        turn = Mathf.Clamp(turn, -maxTurn, maxTurn);
        var nextRotation = physicsBody.rotation * Quaternion.Euler(0, turn * Mathf.Rad2Deg * dt, 0);
        // The car travels along travelDir, which follows the body heading at the tyres' grip rate: instantly on
        // grip, slowly with the handbrake pulled -> the body swings round while the car keeps sliding (drift).
        Vector3 heading = nextRotation * Vector3.forward;
        if (travelDir.sqrMagnitude < .5f) travelDir = heading;
        travelDir = Vector3.Slerp(travelDir, heading, 1f - Mathf.Exp(-(sliding ? driftGrip : normalGrip) * dt)).normalized;
        Vector3 delta = travelDir * speed * dt;
        var next = physicsBody.position + delta;

        bool hasGround = DriveableGround(next, nextRotation, out Vector3 grounded);
        RaycastHit blocker = default;
        bool blocked = hasGround && ObstacleOnVisibleCarPath(delta, nextRotation, out blocker);
        if (!hasGround || blocked)
        {
            speed = Mathf.MoveTowards(speed, 0f, braking * dt);
            LogDriveBlock(hasGround ? "obstacle" : "ground", next, blocker);
            return;
        }

        physicsBody.MoveRotation(nextRotation);
        physicsBody.MovePosition(grounded);
        // Real tyre radius .34 m lives in the K-scaled world too (speed is x K).
        wheelAngle += speed * dt * Mathf.Rad2Deg / (.34f * Mathf.Max(.01f, worldScale));
        foreach (var wheel in rollingWheels)
            if (wheel) wheel.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
        foreach (var wheel in steeringWheels)
            if (wheel) wheel.localRotation = Quaternion.Euler(0, yaw, 0);
    }
}

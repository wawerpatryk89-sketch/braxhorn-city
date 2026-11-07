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
    public BoxCollider bodyCollider;
    public float maxSpeed = 65f / 3.6f, wheelbase = 2.7f, maxSteer = 34f;
    public float interactionRadius = 3.8f;
    public float acceleration = 5.5f, braking = 9.5f;
    public Transform[] rollingWheels = new Transform[0];
    public Transform[] steeringWheels = new Transform[0];

    public BraxGymPlayer Driver => driver;
    public float Speed => speed;
    public bool Transitioning => transition;
    const int PlayerLayer = 8;
    readonly RaycastHit[] groundHits = new RaycastHit[24];
    readonly RaycastHit[] obstacleHits = new RaycastHit[24];
    Rigidbody physicsBody;
    BraxGymPlayer driver;
    Vector2 control;
    float speed, doorAngle, wheelAngle;
    bool transition;
    Quaternion doorRest;

    void Awake()
    {
        physicsBody = GetComponent<Rigidbody>();
        if (!physicsBody || !bodyCollider || !driverSeat || !doorApproach)
            throw new InvalidOperationException("A1 owner's car is incomplete: real chassis, driver seat and entrance required");
        physicsBody.isKinematic = true;
        physicsBody.useGravity = false;
        physicsBody.interpolation = RigidbodyInterpolation.Interpolate;
        if (doorHinge) doorRest = doorHinge.localRotation;
    }

    void OnEnable() => BraxInterakcje.Dodaj(this);
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        EmergencyExit();
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

    static float TransitionSeconds(AnimationClip clip)
        => clip ? Mathf.Clamp(clip.length, .45f, 3.4f) : 1f;

    IEnumerator MoveDoor(float target, float seconds)
    {
        if (!doorHinge) yield break; // No new pretend doors when source mesh is merged.
        float from = doorAngle;
        float duration = Mathf.Max(.1f, seconds);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            doorAngle = Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration)));
            doorHinge.localRotation = doorRest * Quaternion.Euler(0, doorAngle, 0);
            yield return null;
        }
        doorAngle = target;
        doorHinge.localRotation = doorRest * Quaternion.Euler(0, doorAngle, 0);
    }

    IEnumerator Board(BraxGymPlayer p)
    {
        transition = true;
        driver = p;
        p.championsCar = this;
        control = Vector2.zero;
        speed = 0f;
        p.body.enabled = false;
        var clip = p.clips.GetClip("EnteringCar");
        Vector3 start = p.transform.position;
        Quaternion startRot = p.transform.rotation;
        Vector3 door = doorApproach.position;
        p.clips.CrossFade("EnteringCar", .14f, PlayMode.StopAll);
        yield return MoveDoor(-65f, .38f);
        float duration = TransitionSeconds(clip);
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
            p.transform.position = Vector3.Lerp(start, Vector3.Lerp(door, driverSeat.position, t), t);
            p.transform.rotation = Quaternion.Slerp(startRot, driverSeat.rotation, t);
            yield return null;
        }
        p.transform.SetParent(driverSeat, false);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;
        p.clips["Driving"].wrapMode = WrapMode.Loop;
        p.clips.CrossFade("Driving", .18f, PlayMode.StopAll);
        yield return MoveDoor(0f, .35f);
        transition = false;
        Debug.Log("A1_CAR_BOARDED: original owner car_rev + original Brax Mixamo Driving");
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
        driver = null; speed = 0f; control = Vector2.zero; transition = false;
        if (doorHinge) { doorHinge.localRotation = doorRest; doorAngle = 0; }
    }

    bool DriveableGround(Vector3 next, out Vector3 groundPosition)
    {
        groundPosition = next;
        int n = Physics.RaycastNonAlloc(next + Vector3.up * 2f,
            Vector3.down, groundHits, 5f, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        bool found = false; float bestY = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            var hit = groundHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform) || hit.normal.y < .55f) continue;
            if (hit.point.y > bestY) { bestY = hit.point.y; found = true; }
        }
        if (found) groundPosition.y = bestY;
        return found;
    }

    bool ObstacleOnVisibleCarPath(Vector3 advance, Quaternion nextRotation)
    {
        if (advance.sqrMagnitude < .000001f || !bodyCollider) return false;
        Vector3 scale = transform.lossyScale;
        var size = bodyCollider.size;
        var half = Vector3.Scale(size * .5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        // Shrink slightly to avoid treating the legitimate ground as a wall.
        half.y = Mathf.Max(.12f, half.y - .15f);
        var center = transform.position + nextRotation * bodyCollider.center + Vector3.up * .15f;
        int count = Physics.BoxCastNonAlloc(center, half,
            advance.normalized, obstacleHits, nextRotation, advance.magnitude + .08f,
            ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = obstacleHits[i];
            if (!hit.collider || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.normal.y > .70f) continue; // real road/floor, not an obstacle
            return true;
        }
        return false;
    }

    void FixedUpdate()
    {
        if (!driver || transition || !physicsBody || !bodyCollider) return;
        float dt = Time.fixedDeltaTime;
        float target = Mathf.Abs(control.y) < .08f ? 0f : control.y * maxSpeed;
        speed = Mathf.MoveTowards(speed, target,
            (Mathf.Abs(target) < Mathf.Abs(speed) ? braking : acceleration) * dt);
        float yaw = Mathf.Clamp(control.x, -1f, 1f) *
            Mathf.Lerp(maxSteer, maxSteer * .48f, Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed));
        float turn = speed / Mathf.Max(1f, wheelbase) * Mathf.Tan(yaw * Mathf.Deg2Rad);
        var nextRotation = physicsBody.rotation * Quaternion.Euler(0, turn * Mathf.Rad2Deg * dt, 0);
        Vector3 delta = nextRotation * Vector3.forward * speed * dt;
        var next = physicsBody.position + delta;
        if (!DriveableGround(next, out Vector3 grounded) ||
            ObstacleOnVisibleCarPath(delta, nextRotation))
        {
            speed = 0f; // real terrain/car collision only; NO artificial boundary colliders
            return;
        }
        physicsBody.MoveRotation(nextRotation);
        physicsBody.MovePosition(grounded);
        wheelAngle += speed * dt * Mathf.Rad2Deg / .34f;
        foreach (var wheel in rollingWheels)
            if (wheel) wheel.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
        foreach (var wheel in steeringWheels)
            if (wheel) wheel.localRotation = Quaternion.Euler(0, yaw, 0);
    }
}

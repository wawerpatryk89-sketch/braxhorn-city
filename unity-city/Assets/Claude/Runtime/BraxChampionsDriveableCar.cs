using System;
using System.Collections;
using UnityEngine;

// Miasto Mistrzów: the original estate sports-car MODEL and the same interaction
// model (AKCJA + left joystick), installed only in the Champions arena zone.
// Do not add phantom fences, artificial blockers or substitute car primitives.
// Physical collision is limited to the actual car body; all geometry is imported.
public sealed class BraxChampionsDriveableCar : MonoBehaviour, IBraxInterakcja
{
    public Transform driverSeat;
    public Transform driverDoor;
    public Transform[] wheelSpinners = Array.Empty<Transform>();
    public BoxCollider bodyHull;
    public Rigidbody carBody;
    public float maxForward = 11.5f, maxReverse = 3.4f;
    public float wheelBase = 2.8f, wheelRadius = .35f;
    public Vector3 doorClosedEuler, seatPositionOffset;
    public string originalModel = "car_rev";

    BraxGymPlayer passenger;
    Vector3 priorPosition;
    Quaternion priorRotation;
    Transform priorParent;
    Vector2 input;
    float velocity, wheelAngle;
    bool transition;
    const int GroundMask = ~((1 << 8) | (1 << 2));
    readonly RaycastHit[] hits = new RaycastHit[20];

    public bool Occupied => passenger != null;
    public bool Transitioning => transition;
    public float Speed => velocity;
    public Vector3 CameraTarget => transform.TransformPoint(
        bodyHull ? bodyHull.center + Vector3.up * (bodyHull.size.y * .5f + 1.0f)
                 : new Vector3(0f, 2.1f, 0f));

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable()
    {
        BraxInterakcje.Usun(this);
        if (passenger) RestorePassenger(priorPosition, priorRotation);
    }

    // Driver side is the negative local X side. The foot position is outside
    // the VISUAL hull; nothing is spawned in the arena's spectator centre aisle.
    Vector3 DoorApproach => transform.TransformPoint(
        new Vector3(bodyHull ? -bodyHull.size.x * .5f - .68f : -1.7f,
            .06f, driverSeat ? driverSeat.localPosition.z : .4f));

    public string Kontekst(Vector3 playerPosition)
    {
        if (passenger) return transition ? "WSIADANIE / WYSIADANIE"
            : Mathf.Abs(velocity) > .32f ? "ZATRZYMAJ AUTO ABY WYSIĄŚĆ"
            : "WYSIĄDŹ · BRAXHORN";
        if (!driverSeat || !bodyHull) return "";
        return Vector3.Distance(playerPosition, DoorApproach) <= 3.0f
            ? "WSIĄDŹ · SPORTOWY BRAXHORN" : "";
    }

    public bool Wykonaj(Vector3 playerPosition)
    {
        if (transition) return true;
        if (passenger)
        {
            if (Mathf.Abs(velocity) <= .32f) StartCoroutine(Exit());
            return true;
        }
        var player = FindAnyObjectByType<BraxGymPlayer>();
        if (!player || !player.ready || !player.cityWorld || player.cityWorld.area != "mistrzowie"
            || player.carMistrzow || player.vessel || player.seat
            || !player.clips || !driverSeat || !bodyHull) return false;
        if (Vector3.Distance(playerPosition, DoorApproach) > 3.05f) return false;
        foreach (string clip in new[] { "EnteringCar", "CarIdle", "ExitingCar", "Walk", "Idle" })
            if (!player.clips.GetClip(clip))
            {
                Debug.LogWarning("BRAX_A1_CAR_MISSING_OWNER_MIXAMO_CLIP " + clip);
                return false;
            }
        passenger = player;
        priorParent = player.transform.parent;
        priorPosition = player.transform.position;
        priorRotation = player.transform.rotation;
        StartCoroutine(Enter());
        return true;
    }

    IEnumerator DoorAnimation(bool open)
    {
        if (!driverDoor) yield break; // Do not invent animated doors if the source has none.
        var original = driverDoor.localRotation;
        var target = Quaternion.Euler(doorClosedEuler + new Vector3(0, open ? -64f : 0f, 0));
        float seconds = .48f, start = Time.time;
        while (Time.time - start < seconds)
        {
            float t = Mathf.SmoothStep(0, 1, (Time.time - start) / seconds);
            driverDoor.localRotation = Quaternion.Slerp(original, target, t);
            yield return null;
        }
        driverDoor.localRotation = target;
    }

    IEnumerator Enter()
    {
        transition = true;
        var p = passenger;
        if (!p) yield break;
        var first = p.transform.position;
        var startRotation = p.transform.rotation;
        var dest = DoorApproach;
        var travel = Mathf.Clamp(Vector3.Distance(first, dest) / 1.7f, .1f, 1.6f);
        p.clips.CrossFade("Walk", .12f);
        for (float t = 0; t < 1f; t += Time.deltaTime / travel)
        {
            if (!p) yield break;
            p.transform.SetPositionAndRotation(Vector3.Lerp(first, dest, Mathf.SmoothStep(0, 1, t)),
                Quaternion.Slerp(startRotation, transform.rotation, t));
            yield return null;
        }
        yield return DoorAnimation(true);
        if (!p) yield break;
        p.clips["EnteringCar"].wrapMode = WrapMode.ClampForever;
        p.clips.CrossFade("EnteringCar", .12f);
        p.body.enabled = false;
        var entryDuration = Mathf.Max(.2f, p.clips["EnteringCar"].length);
        var entryFrom = dest;
        var seat = driverSeat.position + driverSeat.TransformVector(seatPositionOffset);
        for (float t = 0; t < 1f; t += Time.deltaTime / entryDuration)
        {
            if (!p) yield break;
            p.transform.SetPositionAndRotation(
                Vector3.Lerp(entryFrom, seat, Mathf.SmoothStep(0, 1, t)),
                Quaternion.Slerp(transform.rotation, driverSeat.rotation, t));
            yield return null;
        }
        p.transform.SetParent(transform, true);
        p.transform.SetPositionAndRotation(seat, driverSeat.rotation);
        p.carMistrzow = this;
        p.clips["CarIdle"].wrapMode = WrapMode.Loop;
        p.clips.CrossFade("CarIdle", .20f);
        yield return DoorAnimation(false);
        transition = false;
        Debug.Log("BRAX_A1_CAR_ENTER driver=Brax source=original-car model=" + originalModel);
    }

    IEnumerator Exit()
    {
        if (!passenger || transition || Mathf.Abs(velocity) > .32f) yield break;
        var p = passenger;
        Vector3 destination = DoorApproach;
        // Clearance probe is diagnostic, not an invisible wall or navigation blocker.
        bool occupiedByWorld = false;
        var overlaps = Physics.OverlapCapsule(
            destination + Vector3.up * .40f, destination + Vector3.up * 1.82f,
            .33f, GroundMask, QueryTriggerInteraction.Ignore);
        foreach (var hit in overlaps)
            if (hit && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(p.transform))
                occupiedByWorld = true;
        if (occupiedByWorld)
        {
            Debug.LogWarning("BRAX_A1_CAR_EXIT_CLEARANCE_WARNING: manual in-game review required");
            // A grounded position beside the door is still permitted; do not
            // introduce an invisible player-blocking volume.
        }
        transition = true;
        yield return DoorAnimation(true);
        if (!p) yield break;
        p.clips["ExitingCar"].wrapMode = WrapMode.ClampForever;
        p.clips.CrossFade("ExitingCar", .18f);
        Vector3 start = p.transform.position;
        Quaternion firstRotation = p.transform.rotation;
        p.transform.SetParent(priorParent, true);
        float seconds = Mathf.Max(.2f, p.clips["ExitingCar"].length);
        for (float t = 0; t < 1f; t += Time.deltaTime / seconds)
        {
            if (!p) yield break;
            p.transform.SetPositionAndRotation(
                Vector3.Lerp(start, destination, Mathf.SmoothStep(0, 1, t)),
                Quaternion.Slerp(firstRotation, transform.rotation, t));
            yield return null;
        }
        RestorePassenger(destination, transform.rotation);
        yield return DoorAnimation(false);
        transition = false;
        Debug.Log("BRAX_A1_CAR_EXIT returned_to_original_Brax_3D=true");
    }

    void RestorePassenger(Vector3 position, Quaternion facing)
    {
        var p = passenger;
        if (!p) return;
        passenger = null;
        p.transform.SetParent(priorParent, true);
        if (p.body) p.body.enabled = false;
        p.transform.SetPositionAndRotation(position, facing);
        if (p.body) p.body.enabled = true;
        p.carMistrzow = null;
        if (p.clips && p.clips.GetClip("Idle")) p.clips.CrossFade("Idle", .15f);
        input = Vector2.zero;
        velocity = 0f;
        transition = false;
    }

    public void ResetPassenger()
    {
        StopAllCoroutines();
        if (driverDoor) driverDoor.localRotation = Quaternion.Euler(doorClosedEuler);
        if (passenger) RestorePassenger(priorPosition, priorRotation);
    }

    public void Steer(Vector2 joystick)
    {
        input = Vector2.ClampMagnitude(joystick, 1f);
    }

    void FixedUpdate()
    {
        if (!passenger || transition || !carBody || !bodyHull) return;
        float dt = Time.fixedDeltaTime;
        float wanted = input.y >= 0f ? maxForward * input.y : maxReverse * input.y;
        velocity = Mathf.MoveTowards(velocity, wanted, dt *
            (Mathf.Abs(wanted) < Mathf.Abs(velocity) ? 9.5f : 5.5f));
        float angle = Mathf.Lerp(34f, 12f, Mathf.Clamp01(Mathf.Abs(velocity) / maxForward));
        float turn = angle * input.x;
        float yaw = velocity / Mathf.Max(1.75f, wheelBase) *
            Mathf.Tan(turn * Mathf.Deg2Rad) * Mathf.Rad2Deg * dt;
        Quaternion rotation = transform.rotation * Quaternion.Euler(0f, yaw, 0f);
        Vector3 move = rotation * Vector3.forward * (velocity * dt);
        if (move.sqrMagnitude > .00001f)
        {
            // Cast the actual visible vehicle's hull, ignoring its own collider.
            Vector3 centre = transform.TransformPoint(bodyHull.center);
            Vector3 half = Vector3.Scale(bodyHull.size, transform.lossyScale) * .5f;
            half = new Vector3(Mathf.Max(.25f, Mathf.Abs(half.x) - .08f),
                Mathf.Max(.25f, Mathf.Abs(half.y) - .12f),
                Mathf.Max(.40f, Mathf.Abs(half.z) - .07f));
            int count = Physics.BoxCastNonAlloc(centre, half, move.normalized,
                hits, rotation, move.magnitude + .06f, GroundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (c && !c.transform.IsChildOf(transform) &&
                    !c.transform.IsChildOf(passenger.transform) &&
                    hits[i].distance <= move.magnitude + .03f)
                {
                    velocity = 0f;
                    move = Vector3.zero;
                    break;
                }
            }
        }
        carBody.MoveRotation(rotation);
        carBody.MovePosition(carBody.position + move);
        wheelAngle = Mathf.Repeat(wheelAngle + velocity * dt /
            Mathf.Max(.1f, wheelRadius) * Mathf.Rad2Deg, 360f);
        foreach (var wheel in wheelSpinners) if (wheel)
            wheel.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
    }
}

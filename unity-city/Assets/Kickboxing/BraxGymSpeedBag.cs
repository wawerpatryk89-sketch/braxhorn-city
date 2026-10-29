using UnityEngine;

// BRAXHORN kickboxing room ONLY. Receives the already-timed original punch
// from BraxGymPlayer, checks a real SphereCollider hit volume, and springs a
// 3D pear beneath its steel swivel. No fake hits from camera proximity.
public sealed class BraxGymSpeedBag : MonoBehaviour
{
    public Transform pivot;
    public SphereCollider hitbox;
    [Range(15f, 220f)] public float impactAngularImpulse = 122f;
    [Range(15f, 100f)] public float restoringSpring = 38f;
    [Range(1f, 20f)] public float damping = 6.5f;
    [Range(10f, 50f)] public float maximumAngle = 27f;

    public int HitCount { get; private set; }
    public float LastHitTime { get; private set; } = -1000f;
    public float LastDistance { get; private set; }
    public float LastAlignment { get; private set; }
    public Vector2 LastImpulse { get; private set; }
    public Vector2 Angles { get; private set; }
    public Vector2 AngularVelocity { get; private set; }
    public bool HitBoxAssigned { get { return pivot && hitbox && hitbox.isTrigger; } }

    Quaternion originalRotation = Quaternion.identity;

    void Awake()
    {
        if (!HitBoxAssigned)
        {
            Debug.LogError("BRAXHORN_SPEED_BAG_MISSING_TRUE_3D_TRIGGER");
            enabled = false;
            return;
        }
        originalRotation = pivot.localRotation;
    }

    public bool TryReceivePunch(Vector3 fighterPosition, Vector3 fighterForward,
        out float distance, out float alignment)
    {
        distance = float.PositiveInfinity;
        alignment = -1f;
        if (!enabled || !HitBoxAssigned || Time.time - LastHitTime < .21f)
            return false;

        // The player can punch only at the recorded punch animation contact
        // frame (BraxGymPlayer.HitBag). Check real sphere volume with a sweep
        // corridor rather than awarding a hit when merely standing nearby.
        Vector3 direction = Vector3.ProjectOnPlane(fighterForward, Vector3.up);
        if (direction.sqrMagnitude < .01f) return false;
        direction.Normalize();
        Vector3 center = hitbox.bounds.center;
        Vector3 horizontal = Vector3.ProjectOnPlane(center - fighterPosition, Vector3.up);
        distance = horizontal.magnitude;
        if (distance < .48f || distance > 1.82f) return false;
        alignment = Vector3.Dot(direction, horizontal / distance);
        if (alignment < .72f) return false;

        Vector3 fistOrigin = fighterPosition + Vector3.up * 1.63f;
        float along = Mathf.Clamp(Vector3.Dot(center - fistOrigin, direction), 0f, 1.89f);
        Vector3 punchingArc = fistOrigin + direction * along;
        // ClosestPoint is evaluated on the actual imported Unity sphere
        // collider, and tolerates the small source hand-rig animation offset.
        if (Vector3.Distance(hitbox.ClosestPoint(punchingArc), punchingArc) > .20f)
            return false;

        Vector3 swingDirection = pivot.parent
            ? pivot.parent.InverseTransformDirection(direction) : direction;
        LastImpulse = new Vector2(-swingDirection.z, swingDirection.x) * impactAngularImpulse;
        AngularVelocity += LastImpulse;
        HitCount++;
        LastHitTime = Time.time;
        LastDistance = distance;
        LastAlignment = alignment;
        return true;
    }

    void Update()
    {
        if (!HitBoxAssigned) return;
        float dt = Mathf.Min(Time.deltaTime, .04f);
        if (dt <= 0f) return;
        // Damped two-axis spring: rebounds several times and settles rather
        // than snapping back, while keeping the striking board stationary.
        AngularVelocity += (-Angles * restoringSpring - AngularVelocity * damping) * dt;
        Angles += AngularVelocity * dt;
        Angles = Vector2.ClampMagnitude(Angles, maximumAngle);
        pivot.localRotation = originalRotation * Quaternion.Euler(Angles.x, 0f, Angles.y);
    }
}

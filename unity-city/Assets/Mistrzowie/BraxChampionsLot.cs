using UnityEngine;

// MIASTO MISTRZÓW - the BRAXHORN helicopter's flight (BraxChampionsHelikopter flies it; automaty_sim tests it without
// Unity): an arcade model on the EC135's real figures - cruise 234 km/h (65 m/s), climb 7.6 m/s, the main rotor spooling
// up in 4.5 s; the cyclic sets the speed along the heading, the pedals the turn, the collective the vertical speed (let go
// = hover); nose down to speed up, banked into a turn; it settles on the ground under it when it comes down onto it.
public sealed class BraxChampionsLot
{
    public const float MaxSpeed = 65f, MaxBack = 10f, Accel = 6.5f, Brake = 9f, Drift = 2.5f, YawRate = 55f,
        Climb = 7.6f, Sink = 6.0f, VsAccel = 5f, SpoolUp = 4.5f, SpoolDown = 9f, MaxHeight = 650f;
    public Vector3 pos, vel;                 // world units (metres x k), units / s
    public float heading, vs, rotor, pitch, roll, yawRate, k = 1f;
    public bool engine, grounded = true;
    public bool Ready => rotor >= .97f;
    public float SpeedKph => new Vector2(vel.x, vel.z).magnitude / Mathf.Max(.0001f, k) * 3.6f;

    /// <summary>One step: stick (x = pedals, y = cyclic), collective -1..1, the ground height under the skids (world y),
    /// dt. Returns the wanted displacement; the caller limits it against walls and calls Apply.</summary>
    public Vector3 Step(Vector2 stick, float collective, float groundY, float dt)
    {
        rotor = Mathf.MoveTowards(rotor, engine ? 1f : 0f, dt / (engine ? SpoolUp : SpoolDown));
        bool fly = Ready && engine;
        // the collective: climb / sink toward the stick, hover when let go; on the ground only up lifts it
        float want = !fly ? (grounded ? 0f : -Sink) : collective > 0f ? collective * Climb : collective * Sink;
        if (grounded && want < 0f) want = 0f;
        vs = Mathf.MoveTowards(vs, want, VsAccel * dt);
        // the speed in the frame it has been flying in: the turn below carries it round with the heading (a banked turn,
        // no skid sideways)
        var fwd0 = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        float f = Vector3.Dot(vel, fwd0) / k, s = Vector3.Dot(vel, new Vector3(fwd0.z, 0f, -fwd0.x)) / k;
        // the pedals turn it (in the air, or light on its skids with the rotor up)
        float yawWant = fly && (!grounded || collective > .05f) ? Mathf.Clamp(stick.x, -1f, 1f) * YawRate : 0f;
        yawRate = Mathf.MoveTowards(yawRate, yawWant, 160f * dt);
        heading = Mathf.Repeat(heading + yawRate * dt, 360f);
        // the cyclic: the speed along the heading; a sideways drift (a wall's push) dies out
        var fwd = Quaternion.Euler(0f, heading, 0f) * Vector3.forward; var right = new Vector3(fwd.z, 0f, -fwd.x);
        float target = fly && !grounded ? (stick.y >= 0f ? stick.y * MaxSpeed : stick.y * MaxBack) : 0f;
        float fNew = Mathf.MoveTowards(f, target, (Mathf.Abs(target) < Mathf.Abs(f) || target * f < 0f ? Brake : Accel) * dt);
        if (grounded) fNew = Mathf.MoveTowards(f, 0f, 12f * dt);
        float sNew = Mathf.MoveTowards(s, 0f, (grounded ? 12f : Drift) * dt);
        float accel = (fNew - f) / Mathf.Max(1e-4f, dt);
        vel = (fwd * fNew + right * sNew) * k;
        // the attitude: nose down to speed up and at speed, up to brake; banked into a turn at speed
        float pWant = Mathf.Clamp(-accel * 1.3f - fNew / MaxSpeed * 7f, -16f, 12f), rWant = Mathf.Clamp(-yawRate * Mathf.Clamp01(Mathf.Abs(fNew) / 25f) * .35f, -22f, 22f);
        if (grounded) { pWant = 0f; rWant = 0f; }
        pitch = Mathf.Lerp(pitch, pWant, 1f - Mathf.Exp(-3f * dt)); roll = Mathf.Lerp(roll, rWant, 1f - Mathf.Exp(-3f * dt));
        return new Vector3(vel.x * dt, vs * k * dt, vel.z * dt);
    }
    /// <summary>The displacement after the walls; settles it on groundY when it comes down onto it.</summary>
    public void Apply(Vector3 d, float groundY, float dt)
    {
        var p = pos + d;
        if (vs > .01f) grounded = false;                             // the collective up lifts it off
        if (grounded) p.y = groundY;                                 // on its skids: on the ground under it
        else if (p.y <= groundY && vs <= .05f) { p.y = groundY; vs = 0f; grounded = true; }   // touch-down (no damage model)
        else if (p.y < groundY) p.y = groundY;                       // climbing out over a kerb / a roof edge
        p.y = Mathf.Min(p.y, MaxHeight * k);
        if (dt > 0f) vel = new Vector3((p.x - pos.x) / dt, 0f, (p.z - pos.z) / dt);
        pos = p;
    }
}

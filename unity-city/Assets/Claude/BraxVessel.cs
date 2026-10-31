using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pojazd wodny gracza (skuter, motorówka, jacht) — decyzja właściciela 31.10: po morzu można pływać.
/// Wsiadanie przyciskiem kontekstowym przy pomoście, jazda joystickiem (y = gaz, x = ster), kołysanie
/// na tych samych falach, które liczy shader wody (HarbourWater: SwellPhase), zejście na pomost/piasek.
/// Ląd zatrzymuje pojazd (raycast dna przed dziobem). Stawiany z kodu w BraxHarborBeachBuild.
/// </summary>
public sealed class BraxVessel : MonoBehaviour
{
    public static readonly List<BraxVessel> All = new List<BraxVessel>();

    public string label = "SKUTER";
    public float waterY = -1.7f;
    public float draft = .22f;            // zanurzenie kadłuba (spód modelu pod lustrem wody)
    public float maxSpeed = 11f;          // m/s
    public float accel = 5f;
    public float turnRate = 70f;          // stopni/s przy pełnym sterze
    public float length = 3f, width = 1.2f;
    public Vector3 seat = new Vector3(0, .9f, -.2f);   // miejsce gracza (lokalnie, względem środka pojazdu)
    public Vector3 moor;                  // gdzie pojazd stoi zacumowany
    public float moorYaw;

    public bool Occupied { get { return rider; } }
    BraxGymPlayer rider;
    float speed, yawVel, baseYaw;
    Vector3 home; bool homed;

    void OnEnable() { All.Add(this); if (!homed) { homed = true; home = transform.position; baseYaw = transform.eulerAngles.y; } }
    void OnDisable() { All.Remove(this); }

    /// <summary>Wysokość fali jak w HarbourWater.shader (SwellPhase + amplitudy).</summary>
    public static float Wave(float x, float z, float t)
    {
        float qx = x - 84f, qz = z - 60f;
        float p0 = (qx * .84f + qz * .5426f) * .84f - t * .65f;
        float p1 = (qx * -.4191f + qz * .9079f) * 1.46f - t * .92f;
        return Mathf.Sin(p0) * .10f + Mathf.Sin(p1) * .06f;
    }

    public static BraxVessel Nearest(Vector3 p, float max)
    {
        BraxVessel best = null; float bd = max;
        foreach (var v in All)
        {
            if (!v || v.rider) continue;
            var d = v.transform.position - p; d.y = 0;
            if (d.magnitude < bd) { bd = d.magnitude; best = v; }
        }
        return best;
    }

    public void Board(BraxGymPlayer player)
    {
        if (rider || !player) return;
        rider = player;
        player.vessel = this;
        player.body.enabled = false;
        player.transform.SetParent(transform, true);
        player.transform.localPosition = seat;
        player.transform.localRotation = Quaternion.identity;
        speed = 0; yawVel = 0;
    }

    /// <summary>Zejście: na najbliższy twardy grunt nad wodą w promieniu 4 m (pomost, piasek). Zwraca false, gdy za daleko od brzegu.</summary>
    public bool Dismount()
    {
        if (!rider) return false;
        Vector3 best = Vector3.zero; bool found = false; float bd = 99f;
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            for (float r = 1.2f; r <= 4.5f; r += .6f)
            {
                var o = transform.position + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                RaycastHit hit;
                if (!Physics.Raycast(o + Vector3.up * 6f, Vector3.down, out hit, 12f, ~(1 << 8), QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y < waterY + .15f) continue;                 // dno pod wodą — nie
                if (hit.collider.transform.IsChildOf(transform)) continue;  // własny kadłub — nie
                if (r < bd) { bd = r; best = hit.point; found = true; }
            }
        }
        if (!found) return false;
        var p = rider;
        p.transform.SetParent(null, true);
        p.transform.position = best + Vector3.up * .08f;
        p.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        p.body.enabled = true;
        p.vessel = null;
        rider = null; speed = 0;
        home = transform.position; baseYaw = transform.eulerAngles.y;   // pojazd zostaje tam, gdzie go zostawiono
        return true;
    }

    /// <summary>Jazda: move.y = gaz (ujemny = wstecz, wolniej), move.x = ster. Wywołuje gracz w Update.</summary>
    public void Drive(Vector2 move, float dt)
    {
        float target = move.y > 0 ? move.y * maxSpeed : move.y * maxSpeed * .3f;
        speed = Mathf.MoveTowards(speed, target, dt * (Mathf.Abs(target) > Mathf.Abs(speed) ? accel : accel * 1.6f));
        float steer = move.x * turnRate * Mathf.Clamp01(Mathf.Abs(speed) / 3f) * (speed < 0 ? -1f : 1f);
        yawVel = Mathf.Lerp(yawVel, steer, 1f - Mathf.Exp(-dt * 4f));
        float yaw = transform.eulerAngles.y + yawVel * dt;
        var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        var next = transform.position + fwd * speed * dt;
        // ląd przed dziobem: dno wyżej niż 0.8 m pod lustrem wody → stop (plaża, pomost, skały)
        var probe = next + fwd * (length * .5f + .5f);
        RaycastHit hit;
        if (Physics.Raycast(probe + Vector3.up * 4f, Vector3.down, out hit, 20f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            bool own = hit.collider.transform.IsChildOf(transform);
            if (!own && hit.point.y > waterY - .8f) { speed = 0; next = transform.position; }
        }
        float t = Time.time;
        float h = Wave(next.x, next.z, t);
        float hf = Wave(next.x + fwd.x * 2f, next.z + fwd.z * 2f, t);
        float hr = Wave(next.x + fwd.z * 1f, next.z - fwd.x * 1f, t);
        next.y = waterY + h - draft;
        float pitch = Mathf.Atan2(hf - h, 2f) * Mathf.Rad2Deg - Mathf.Clamp(speed, 0, maxSpeed) * .35f;
        float roll = Mathf.Atan2(hr - h, 1f) * Mathf.Rad2Deg - yawVel * .08f;
        transform.position = next;
        transform.rotation = Quaternion.Euler(-pitch, yaw, roll);
    }

    /// <summary>Zacumowany pojazd lekko kołysze się na fali.</summary>
    void Update()
    {
        if (rider) return;
        var p = home; p.y = waterY + Wave(home.x, home.z, Time.time) - draft;
        transform.position = p;
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * .9f + home.x) * 1.2f, baseYaw, Mathf.Sin(Time.time * 1.3f + home.z) * 1.6f);
    }
}

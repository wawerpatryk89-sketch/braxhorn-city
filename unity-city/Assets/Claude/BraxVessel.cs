using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pojazd wodny gracza (skuter, motorówka, jacht) — decyzja właściciela 31.10 i 2.11 („ma być możliwość pływania skuterem
/// i jachtem”): przy pomoście AKCJA „WSIĄDŹ”, jazda joystickiem (y = gaz / wsteczny, x = ster), AKCJA „WYSIĄDŹ” przy pomoście
/// lub brzegu (np. pomost wyspy). Prosta fizyka łodzi: przyspieszenie i opór, poślizg boczny w zakręcie, przechył (skuter
/// i motorówka do środka zakrętu, jacht lekko na zewnątrz), dziób unosi się przy rozpędzaniu i wchodzeniu w ślizg, kołysanie
/// na tych samych falach co shader wody (HarbourWater: SwellPhase). Fala dziobowa i ślad za rufą to siatki z pianą
/// (shader Braxhorn/NabrzezePiana) — siła zależy od prędkości, bez cząsteczek. Ląd, pomosty i boje kąpieliska zatrzymują
/// łódź (promienie przed dziobem i przy burtach). Stawiany z kodu w BraxNabrzezeMarina.
/// </summary>
public sealed class BraxVessel : MonoBehaviour
{
    public static readonly List<BraxVessel> All = new List<BraxVessel>();

    public string label = "SKUTER";
    public float waterY = -1.7f;
    public float draft = .22f;            // dodatkowe zanurzenie korzenia (model ustawiony już na swoim zanurzeniu)
    public float maxSpeed = 11f;          // m/s
    public float accel = 5f;              // m/s²
    public float turnRate = 70f;          // stopni/s przy pełnym sterze i prędkości przelotowej
    public float length = 3f, width = 1.2f;
    public Vector3 seat = new Vector3(0, .9f, -.2f);   // miejsce gracza (lokalnie, względem środka pojazdu)
    public bool sit = true;               // skuter/motorówka: siedzi (BraxPose.Ride); jacht: stoi za sterem
    public float sitLower = .95f;         // biodra 0,95 m nad stopami → korzeń postaci niżej o tyle przy siedzeniu
    public Vector3 moor;                  // gdzie pojazd stoi zacumowany
    public float moorYaw;
    public Vector3 wejscie;               // miejsce wejścia (lokalnie): przy burcie od pomostu albo za rufą; zero = środek
    public Renderer[] fale = new Renderer[0];   // fala dziobowa i ślad za rufą (siatki z pianą)

    public bool Occupied { get { return rider; } }
    public float Speed { get { return speed; } }
    BraxGymPlayer rider;
    UnityEngine.SceneManagement.Scene riderScene;
    float speed, yawVel, baseYaw, side, pitchS, rollS, heave;
    Vector3 home; bool homed;
    MaterialPropertyBlock mpb;
    static readonly int MocId = Shader.PropertyToID("_Moc"), BiegId = Shader.PropertyToID("_Bieg");

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

    public Vector3 BoardingPoint { get { return wejscie == Vector3.zero ? transform.position : transform.TransformPoint(wejscie); } }

    public static BraxVessel Nearest(Vector3 p, float max)
    {
        BraxVessel best = null; float bd = max;
        foreach (var v in All)
        {
            if (!v || v.rider) continue;
            var d = v.BoardingPoint - p; d.y = 0;
            if (d.magnitude < bd) { bd = d.magnitude; best = v; }
        }
        return best;
    }

    public void Board(BraxGymPlayer player)
    {
        if (rider || !player) return;
        rider = player;
        riderScene = player.gameObject.scene;   // obszary doczytywane (1.11): po zejściu gracz wraca do swojej sceny
        player.vessel = this;
        player.body.enabled = false;
        player.transform.SetParent(transform, true);
        player.transform.localPosition = sit ? seat - Vector3.up * sitLower : seat;
        player.transform.localRotation = Quaternion.identity;
        speed = 0; yawVel = 0; side = 0;
        if (sit) BraxPose.Get(player).Set(BraxPose.Mode.Ride, transform);
        Debug.Log("BRAXHORN_LODZ wsiadł: " + label + " " + name);
    }

    /// <summary>Zejście: na najbliższy twardy grunt nad wodą w promieniu 4,5 m (pomost, piasek). Zwraca false, gdy za daleko od brzegu.</summary>
    public bool Dismount()
    {
        if (!rider) return false;
        Vector3 best = Vector3.zero; bool found = false; float bd = 99f;
        // A 2.11: szukanie wzdłuż całego kadłuba (środek, dziób, rufa) — długi jacht dobity dziobem albo burtą do pomostu
        var fw = transform.forward; fw.y = 0f; fw.Normalize();
        foreach (float wz in new[] { 0f, .38f, -.38f, .2f, -.2f })
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            for (float r = 1.2f; r <= Mathf.Max(4.5f, width * .5f + 3f); r += .6f)
            {
                var o = transform.position + fw * (length * wz) + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                RaycastHit hit;
                if (!Physics.Raycast(o + Vector3.up * 6f, Vector3.down, out hit, 12f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y < waterY + .15f) continue;                 // dno pod wodą — nie
                if (hit.collider.transform.IsChildOf(transform)) continue;  // własny kadłub — nie
                if (hit.normal.y < .7f) continue;                          // ściana, skała — nie
                if (Physics.CheckCapsule(hit.point + Vector3.up * .5f, hit.point + Vector3.up * 1.9f, .3f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) continue;   // miejsce zajęte (krawędź, słupek)
                float score = r + Mathf.Abs(wz) * length * .5f;
                if (score < bd) { bd = score; best = hit.point; found = true; }
            }
        }
        if (!found) return false;
        var p = rider;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        p.transform.SetParent(null, true);
        if (riderScene.IsValid() && riderScene.isLoaded && p.gameObject.scene != riderScene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(p.gameObject, riderScene);
        p.transform.position = best + Vector3.up * .08f;
        p.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        p.body.enabled = true;
        p.vessel = null;
        rider = null; speed = 0; side = 0;
        home = transform.position; baseYaw = transform.eulerAngles.y;   // pojazd zostaje tam, gdzie go zostawiono
        Debug.Log("BRAXHORN_LODZ wysiadł: " + label + " przy " + best.ToString("F1"));
        return true;
    }

    bool Lad(Vector3 p)
    {
        RaycastHit hit;
        if (!Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out hit, 20f, ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore)) return false;
        return !hit.collider.transform.IsChildOf(transform) && hit.point.y > waterY - .8f;
    }

    /// <summary>Punkt w obrysie innej łodzi (kapsuła wzdłuż kadłuba) — łodzie nie mają kolizji, żeby gracz wsiadał bez zacięć.</summary>
    bool Lodz(Vector3 p)
    {
        foreach (var o in All)
        {
            if (!o || o == this) continue;
            var d = p - o.transform.position; d.y = 0;
            if (d.sqrMagnitude > (o.length * o.length)) continue;
            var f = o.transform.forward; f.y = 0; f.Normalize();
            float along = Mathf.Clamp(Vector3.Dot(d, f), -o.length * .5f + o.width * .5f, o.length * .5f - o.width * .5f);
            if ((d - f * along).magnitude < o.width * .5f + .25f) return true;
        }
        return false;
    }

    /// <summary>Jazda: move.y = gaz (ujemny = wstecz, wolniej), move.x = ster. Wywołuje gracz w Update.</summary>
    public void Drive(Vector2 move, float dt)
    {
        float target = move.y > 0 ? move.y * maxSpeed : move.y * maxSpeed * .3f;
        float a = Mathf.Abs(target) > Mathf.Abs(speed) ? accel : accel * 1.4f;      // zdejmowanie gazu: opór wody hamuje szybciej
        float before = speed;
        speed = Mathf.MoveTowards(speed, target, dt * a);
        float acc = (speed - before) / Mathf.Max(dt, 1e-4f);
        // ster działa tylko przy ruchu (strumień wody na sterze / dyszy), przy prędkości przelotowej najmocniej
        float steerEff = Mathf.Clamp01(Mathf.Abs(speed) / (maxSpeed * .25f));
        float steer = move.x * turnRate * steerEff * (speed < 0 ? -1f : 1f);
        yawVel = Mathf.Lerp(yawVel, steer, 1f - Mathf.Exp(-dt * (sit ? 4f : 1.6f)));    // jacht: bezwładny
        float yaw = transform.eulerAngles.y + yawVel * dt;
        var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        // poślizg boczny: w zakręcie łódź „wynosi” na zewnątrz, opór boczny wygasza go
        side = Mathf.Lerp(side, -yawVel * Mathf.Deg2Rad * speed * .35f, 1f - Mathf.Exp(-dt * 2.5f));
        var next = transform.position + (fwd * speed + right * side) * dt;
        // ląd / pomost / boje przed dziobem i przy burtach → zatrzymanie
        var probe = next + fwd * (length * .5f + .6f) * Mathf.Sign(speed == 0 ? 1 : speed);
        if (Lad(probe) || Lodz(probe))
        {
            if (Mathf.Abs(speed) > 3f) Debug.Log("BRAXHORN_LODZ uderzenie w brzeg/pomost: " + label + " v=" + speed.ToString("F1"));
            speed = 0; side = 0; next = transform.position;
        }
        float t = Time.time;
        float h = Wave(next.x, next.z, t);
        float hf = Wave(next.x + fwd.x * length * .4f, next.z + fwd.z * length * .4f, t);
        float hr = Wave(next.x + right.x * width * .5f, next.z + right.z * width * .5f, t);
        float v01 = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
        // ślizg: dziób w górę przy rozpędzaniu i w „garbie” fali (ok. 40 % prędkości), na pełnej prędkości kadłub płasko
        float hump = Mathf.Exp(-Mathf.Pow((v01 - .38f) / .18f, 2f));
        float wantPitch = Mathf.Atan2(hf - h, length * .4f) * Mathf.Rad2Deg * (sit ? 1f : .5f)
                          + (sit ? 7f : 2.5f) * hump + Mathf.Clamp(acc, -4f, 4f) * (sit ? .9f : .3f);
        float wantRoll = Mathf.Atan2(hr - h, width * .5f) * Mathf.Rad2Deg * .6f
                         + yawVel * v01 * (sit ? -.22f : .05f);                     // skuter/motorówka do środka, jacht lekko na zewnątrz
        pitchS = Mathf.Lerp(pitchS, wantPitch, 1f - Mathf.Exp(-dt * 3f));
        rollS = Mathf.Lerp(rollS, Mathf.Clamp(wantRoll, -24f, 24f), 1f - Mathf.Exp(-dt * 3f));
        heave = Mathf.Lerp(heave, sit ? v01 * .12f : 0f, 1f - Mathf.Exp(-dt * 2f));   // w ślizgu kadłub wynurza się
        next.y = waterY + h - draft + heave;
        transform.position = next;
        transform.rotation = Quaternion.Euler(-pitchS, yaw, rollS);
        Fale(v01, speed > 0);
    }

    void Fale(float v01, bool naprzod)
    {
        if (fale == null || fale.Length == 0) return;
        bool on = v01 > .06f && naprzod;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        mpb.SetFloat(MocId, Mathf.Clamp01(v01 * 1.4f));
        mpb.SetFloat(BiegId, 1f + v01 * 6f);
        foreach (var r in fale)
        {
            if (!r) continue;
            if (r.enabled != on) r.enabled = on;
            if (on) r.SetPropertyBlock(mpb);
        }
    }

    /// <summary>Zacumowany pojazd lekko kołysze się na fali.</summary>
    void Update()
    {
        if (rider) return;
        if (speed != 0) speed = 0;
        Fale(0, false);
        var p = home; p.y = waterY + Wave(home.x, home.z, Time.time) - draft;
        transform.position = p;
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * .9f + home.x) * (sit ? 1.2f : .5f), baseYaw, Mathf.Sin(Time.time * 1.3f + home.z) * (sit ? 1.6f : .7f));
    }
}

using System;
using System.Collections;
using UnityEngine;

// MIASTO MISTRZÓW - the BRAXHORN helicopter (owner 16.11 22:00: "Masz tutaj helikopter ... przemaluj go na nasz kolor, z
// napisem naszym, z naszym herbem. I go postaw na dachu i zrób wejście. I żeby był funkcjonalny, żeby można było latać"):
// the Airbus EC135 (model GRIP420, CC-BY 4.0, Sketchfab) in the BRAXHORN livery on the heliport of the BRAXHORN TOWER.
//  - AKCJA at its right door: the player climbs into the pilot's seat (the EC135 is flown from the right), the turbines
//    start and the rotor spools up (4.5 s); AKCJA landed: the rotor runs down, he climbs out at the door;
//  - the city joystick flies it: forward / back = the cyclic (fly forward up to 234 km/h, the EC135's cruise; back up to
//    36 km/h), left / right = the pedals (turn on the spot up to 55 deg/s); W GÓRĘ / W DÓŁ on the screen (or E / Q) =
//    the collective (climb 7.6 m/s, the EC135's rate, sink 6 m/s); nothing pressed = it hovers where it is;
//  - it lands on whatever is under its skids (the heliport, a roof, a street): touching down slowly settles it, the
//    ground under it is found by the city's colliders; walls and towers stop it (it slides along them);
//  - the camera follows it as it follows the car (the player's vehicle hook: a BraxChampionsCar in proxy mode on the
//    camera point behind the cabin), the panel shows the height and the speed.
// The flight itself is BraxChampionsLot (pure, tested without Unity by automaty_sim).
[DisallowMultipleComponent]
public sealed class BraxChampionsHelikopter : MonoBehaviour, IBraxInterakcja
{
    public Transform model, wirnikGlowny, wirnikOgonowy, fotel, wejscie;
    public BraxChampionsCar kamera;              // the player's vehicle hook (proxy mode): joystick + follow camera
    public string nazwa = "HELIKOPTER BRAXHORN";
    public float interactionRadius = 3.2f;       // metres (x the world scale)
    public float worldScale = 1f;
    public float rotorRadius = 5.1f, skidHalfLength = 1.6f;   // metres (the hull's colliders are the generator's, under kamera)

    public BraxGymPlayer Pilot => pilot;
    public bool Transitioning => transition;
    public bool InAir => lot != null && !lot.grounded;
    public BraxChampionsLot Lot => lot;
    public float Collective { get; set; }        // the HUD's W GÓRĘ / W DÓŁ (held)

    const int PlayerLayer = 8;
    BraxChampionsLot lot;
    BraxGymPlayer pilot;
    Vector2 stick;
    bool transition;
    float rotorAngle, tailAngle;
    readonly RaycastHit[] hits = new RaycastHit[32];

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); EmergencyExit(); }

    void Start()
    {
        // it flies anywhere: out of the tower's hierarchy (the tower's interior culling never hides it in flight)
        worldScale = Mathf.Max(.01f, transform.lossyScale.y);
        if (transform.parent) transform.SetParent(null, true);
        lot = new BraxChampionsLot { pos = transform.position, heading = transform.eulerAngles.y, k = worldScale };
        if (kamera) kamera.helikopter = this;
    }

    // ---------------- interakcja ----------------
    Vector3 Drzwi => wejscie ? wejscie.position : transform.position;
    public string Kontekst(Vector3 position)
    {
        if (pilot) return transition ? "WSIADANIE / WYSIADANIE" : InAir || lot.SpeedKph > 3f ? "WYLĄDUJ, ABY WYSIĄŚĆ" : "WYSIĄDŹ · " + nazwa;
        if (transition) return "";
        float r = interactionRadius * worldScale; return (position - Drzwi).sqrMagnitude <= r * r ? "WSIĄDŹ · " + nazwa : "";
    }
    public bool Wykonaj(Vector3 position)
    {
        if (pilot)
        {
            if (!transition && !InAir && lot.SpeedKph <= 3f) StartCoroutine(Wysiadz());
            return true;
        }
        float rr = interactionRadius * worldScale; if (transition || (position - Drzwi).sqrMagnitude > rr * rr) return false;
        var p = BraxChampionsGry.Gracz;
        if (!p || !p.body || p.seat || p.vessel || p.championsCar || !kamera) return false;
        StartCoroutine(Wsiadz(p));
        return true;
    }
    public void Steer(Vector2 input) => stick = Vector2.ClampMagnitude(input, 1f);

    static Transform Biodra(Transform root)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.EndsWith("Hips", StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }
    IEnumerator Wsiadz(BraxGymPlayer p)
    {
        transition = true; pilot = p; stick = Vector2.zero; Collective = 0f;
        p.championsCar = kamera;                                    // the player's hook: no walking, the follow camera, the joystick here
        if (!GetComponent<BraxChampionsHelikopterHud>()) gameObject.AddComponent<BraxChampionsHelikopterHud>().heli = this;
        p.body.enabled = false;
        Vector3 start = p.transform.position; Quaternion startRot = p.transform.rotation;
        var hips = Biodra(p.transform); Vector3 rootToHips = hips ? hips.position - start : Vector3.zero;
        if (p.clips && p.clips.GetClip("Driving")) { p.clips["Driving"].wrapMode = WrapMode.Loop; p.clips.CrossFade("Driving", .25f, PlayMode.StopAll); }
        const float D = 1.1f;
        for (float t = 0f; t < D; t += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0f, 1f, t / D);
            Vector3 seated = fotel ? fotel.position - (fotel.rotation * Quaternion.Inverse(startRot)) * rootToHips : transform.position;
            p.transform.position = Vector3.Lerp(Vector3.Lerp(start, Drzwi, Mathf.Min(1f, e * 2f)), seated, e);
            p.transform.rotation = Quaternion.Slerp(startRot, fotel ? fotel.rotation : transform.rotation, e);
            yield return null;
        }
        if (fotel) { p.transform.SetParent(fotel, true); p.transform.localRotation = Quaternion.identity; if (hips) p.transform.position += fotel.position - hips.position; }
        lot.engine = true; transition = false;
        BraxChampionsGry.Pokaz(nazwa + " · ROZRUCH TURBIN", 3f);
        Debug.Log("HELI_BOARDED hips=" + (hips != null));
    }
    IEnumerator Wysiadz()
    {
        transition = true; stick = Vector2.zero; Collective = 0f; lot.engine = false;
        var p = pilot; Vector3 from = p.transform.position, to = Drzwi;
        // the ground under the door
        if (Physics.Raycast(to + Vector3.up * 2f * worldScale, Vector3.down, out var h, 6f * worldScale, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore)) to = h.point;
        p.transform.SetParent(null, true);
        const float D = .9f;
        for (float t = 0f; t < D; t += Time.deltaTime) { p.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / D)); yield return null; }
        p.transform.position = to; p.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Drzwi - transform.position, Vector3.up).normalized + transform.forward * .01f, Vector3.up);
        p.championsCar = null; p.body.enabled = true;
        if (p.clips && p.clips.GetClip("Idle")) p.clips.CrossFade("Idle", .2f, PlayMode.StopAll);
        pilot = null; transition = false;
        BraxChampionsGry.Pokaz(nazwa + " · WYŁĄCZONY", 2f);
    }
    public void EmergencyExit()
    {
        StopAllCoroutines();
        if (pilot)
        {
            var p = pilot; p.transform.SetParent(null, true); p.championsCar = null;
            if (p.body) p.body.enabled = true;
            if (p.clips && p.clips.GetClip("Idle")) p.clips.Play("Idle");
        }
        pilot = null; transition = false; stick = Vector2.zero; Collective = 0f;
        if (lot != null) lot.engine = false;
    }

    // ---------------- flight ----------------
    bool Wlasny(Collider c) => c && (c.transform.IsChildOf(transform) || (pilot && c.transform.IsChildOf(pilot.transform)));
    // the highest real surface under the skids (from the fuselage down), world y; the current height if none
    float Grunt(Vector3 p)
    {
        float k = worldScale, best = float.NegativeInfinity;
        var fwd = Quaternion.Euler(0f, lot.heading, 0f) * Vector3.forward;
        foreach (var o in new[] { Vector3.zero, fwd * skidHalfLength * k, -fwd * skidHalfLength * k })
        {
            int n = Physics.RaycastNonAlloc(p + o + Vector3.up * 2.5f * k, Vector3.down, hits, 2.5f * k + 60f * k, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var h = hits[i]; if (Wlasny(h.collider) || h.normal.y < .6f) continue; if (h.point.y > best && h.point.y <= p.y + .6f * k) best = h.point.y; }
        }
        return float.IsNegativeInfinity(best) ? -1e6f : best;
    }
    // the walls: the fuselage (a sphere round the cabin) swept along the move; it slides along what it meets
    Vector3 Sciany(Vector3 from, Vector3 d)
    {
        float k = worldScale, r = 2.2f * k; var c = from + Vector3.up * 1.7f * k;
        for (int pass = 0; pass < 2; pass++)
        {
            float len = d.magnitude; if (len < 1e-5f) return d;
            int n = Physics.SphereCastNonAlloc(c, r, d / len, hits, len, ~(1 << PlayerLayer), QueryTriggerInteraction.Ignore);
            float bestD = float.MaxValue; Vector3 nrm = Vector3.zero;
            for (int i = 0; i < n; i++) { var h = hits[i]; if (Wlasny(h.collider) || h.normal.y > .6f) continue; if (h.distance < bestD) { bestD = h.distance; nrm = h.normal; } }
            if (bestD == float.MaxValue) return d;
            nrm.y = 0f; nrm.Normalize();
            d = d - nrm * Mathf.Min(0f, Vector3.Dot(d, nrm)) + nrm * .01f * k;   // slide: lose the part into the wall
        }
        return d;
    }

    void Update()
    {
        if (lot == null) return;
        float dt = Mathf.Min(Time.deltaTime, .05f);
        if (pilot && !transition)
        {
            float col = Collective;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKey(KeyCode.E)) col = 1f; else if (Input.GetKey(KeyCode.Q)) col = -1f;
#endif
            float g = Grunt(lot.pos);
            var d = lot.Step(stick, col, g, dt);
            d = new Vector3(0f, d.y, 0f) + Sciany(lot.pos, new Vector3(d.x, 0f, d.z));
            lot.Apply(d, Mathf.Max(g, Grunt(lot.pos + new Vector3(d.x, 0f, d.z))), dt);
            transform.SetPositionAndRotation(lot.pos, Quaternion.Euler(0f, lot.heading, 0f));
            if (model) model.localRotation = Quaternion.Euler(lot.pitch, 0f, lot.roll);
            BraxChampionsGry.Ustaw(nazwa + " · WYSOKOŚĆ " + Mathf.RoundToInt(Mathf.Max(0f, lot.pos.y - g) / worldScale) + " M · " + Mathf.RoundToInt(lot.SpeedKph) + " KM/H" +
                (lot.Ready ? "" : " · ROZRUCH " + Mathf.RoundToInt(lot.rotor * 100f) + " %"));
        }
        else if (!pilot && lot.rotor > 0f) lot.Step(Vector2.zero, 0f, lot.pos.y, dt);       // the rotor runs down after landing
        // the rotors: the main 395 rpm (6.6 rev/s, clockwise from above), the fenestron shown at a visible 18 rev/s
        rotorAngle = Mathf.Repeat(rotorAngle + lot.rotor * 6.6f * 360f * dt, 360f);
        tailAngle = Mathf.Repeat(tailAngle + lot.rotor * 18f * 360f * dt, 360f);
        if (wirnikGlowny) wirnikGlowny.localRotation = Quaternion.AngleAxis(rotorAngle, Vector3.up);
        if (wirnikOgonowy) wirnikOgonowy.localRotation = Quaternion.AngleAxis(tailAngle, Vector3.right);
    }
}

// W GÓRĘ / W DÓŁ while the player flies (the city's touch controls hide their fight buttons in a vehicle): two round
// buttons in the city's style where PUNCH and KICK sit on foot, held = the collective up / down.
[DisallowMultipleComponent]
public sealed class BraxChampionsHelikopterHud : MonoBehaviour
{
    public BraxChampionsHelikopter heli;
    static Texture2D gora, dol;
    int fGora = -1, fDol = -1;
    static Texture2D Strzalka(bool up)
    {
        const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = up ? "ikona w górę" : "ikona w dół", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color32[n * n]; float texel = 2f / n;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + .5f) / n * 2f - 1f, (y + .5f) / n * 2f - 1f) / .92f; if (!up) p.y = -p.y;
                // a chevron arrow: the head (a triangle) and the stem
                float head = Mathf.Max(Mathf.Abs(p.x) * .9f + (p.y - .55f) * .78f, -(p.y + .05f));
                float stem = Mathf.Max(Mathf.Abs(p.x) - .14f, Mathf.Abs(p.y + .32f) - .38f);
                float d = Mathf.Min(head, stem);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(.5f - d / texel) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        t.SetPixels32(px); t.Apply(true, true); return t;
    }
    bool Layout(out Vector2 cUp, out Vector2 cDown, out float r)
    {
        var safe = BraxGui.BezpiecznyProstokat(); int w = Screen.width, h = Screen.height;
        var sa = new Rect(safe.x, h - safe.yMax, safe.width, safe.height);
        float s = Mathf.Min(w, h), k = BraxUstawienia.SkalaPrzyciskow, m = s * .045f;
        r = s * .074f * k;
        cDown = new Vector2(sa.xMax - m - r, sa.yMin + m + r);
        cUp = cDown + new Vector2(0f, 2.35f * r);
        return s > 0f;
    }
    bool Active => heli && heli.Pilot && !heli.Transitioning && !BraxChampionsCarCommands.Paused;
    void Update()
    {
        if (!Active) { fGora = fDol = -1; if (heli) heli.Collective = 0f; return; }
        if (!Layout(out var cu, out var cd, out var r)) return;
        bool up = false, down = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began)
            {
                if (fGora < 0 && (t.position - cu).sqrMagnitude <= r * r * 1.8f) fGora = t.fingerId;
                else if (fDol < 0 && (t.position - cd).sqrMagnitude <= r * r * 1.8f) fDol = t.fingerId;
            }
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { if (t.fingerId == fGora) fGora = -1; if (t.fingerId == fDol) fDol = -1; continue; }
            if (t.fingerId == fGora) up = true; if (t.fingerId == fDol) down = true;
        }
        if (Input.touchCount == 0) fGora = fDol = -1;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(0)) { var mp = (Vector2)Input.mousePosition; if ((mp - cu).sqrMagnitude <= r * r) up = true; else if ((mp - cd).sqrMagnitude <= r * r) down = true; }
#endif
        heli.Collective = up ? 1f : down ? -1f : 0f;
    }
    void OnGUI()
    {
        if (!Active || Event.current.type != EventType.Repaint) return;
        if (!Layout(out var cu, out var cd, out var r)) return;
        if (!gora) gora = Strzalka(true); if (!dol) dol = Strzalka(false);
        GUI.depth = 1;
        BraxGui.PrzyciskKolowy(new Vector2(cu.x, Screen.height - cu.y), r, gora, heli.Collective > 0f, BraxGui.Trafienie);
        BraxGui.PrzyciskKolowy(new Vector2(cd.x, Screen.height - cd.y), r, dol, heli.Collective < 0f, BraxGui.Trafienie);
    }
}

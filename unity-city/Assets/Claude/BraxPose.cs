using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pozy Braxa z kodu (decyzja właściciela 31.10): siedzenie na skuterze/motorówce, siedzenie na sofie/ławce/hokerze,
/// leżenie, picie shakera. Kości obracane geometrycznie po animacji (LateUpdate): każda kość jest celowana
/// (Quaternion.FromToRotation) w kierunek zadany względem obiektu, na którym Brax siedzi — niezależnie od osi rigu.
/// Działa na każdym szkielecie z nazwami Mixamo (Hips, Spine, LeftUpLeg, LeftLeg, LeftArm, LeftForeArm, ...).
/// Gdy właściciel dostarczy klipy z Mixamo, zastąpią pozę 1:1 (ten sam punkt siedzenia).
/// </summary>
public sealed class BraxPose : MonoBehaviour
{
    public enum Mode { None, Ride, Sit, Lie, Drink }
    public Mode mode;
    public Transform anchor;         // obiekt, względem którego liczona jest poza (pojazd, siedzisko)
    public float weight;             // 0..1 — płynne wejście/wyjście z pozy
    public float drinkUntil;

    Transform visual, hips, spine, chest, head, lUp, lLeg, lFoot, rUp, rLeg, rFoot, lArm, lFore, lHand, rArm, rFore, rHand;
    GameObject shaker;
    bool searched;

    public static BraxPose Get(BraxGymPlayer p)
    {
        var pose = p.GetComponent<BraxPose>();
        if (!pose) { pose = p.gameObject.AddComponent<BraxPose>(); pose.visual = p.visual; }
        return pose;
    }

    public void Set(Mode m, Transform a) { mode = m; anchor = a; if (m == Mode.None) anchor = null; }
    public void Drink(float seconds = 2.6f) { if (mode != Mode.None) return; mode = Mode.Drink; drinkUntil = Time.time + seconds; }

    Transform Find(string key)
    {
        if (!visual) return null;
        Transform fallback = null;
        foreach (var t in visual.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name; int i = n.LastIndexOf(':'); if (i >= 0) n = n.Substring(i + 1);
            if (n == key) return t;
            if (fallback == null && n.EndsWith(key)) fallback = t;
        }
        return fallback;
    }

    void Search()
    {
        searched = true;
        var p = GetComponent<BraxGymPlayer>(); if (p && !visual) visual = p.visual;
        hips = Find("Hips"); spine = Find("Spine"); chest = Find("Spine2") ?? Find("Spine1"); head = Find("Head");
        lUp = Find("LeftUpLeg"); lLeg = Find("LeftLeg"); lFoot = Find("LeftFoot");
        rUp = Find("RightUpLeg"); rLeg = Find("RightLeg"); rFoot = Find("RightFoot");
        lArm = Find("LeftArm"); lFore = Find("LeftForeArm"); lHand = Find("LeftHand");
        rArm = Find("RightArm"); rFore = Find("RightForeArm"); rHand = Find("RightHand");
        Debug.Log("BRAXHORN_POSE kości: hips=" + (bool)hips + " spine=" + (bool)spine + " nogi=" + ((bool)lUp && (bool)lLeg && (bool)rUp && (bool)rLeg) + " ręce=" + ((bool)lArm && (bool)lFore && (bool)rArm && (bool)rFore));
    }

    public bool Ready { get { if (!searched) Search(); return hips && lUp && lLeg && rUp && rLeg && lArm && lFore && rArm && rFore; } }

    static void Aim(Transform bone, Transform child, Vector3 want, float w)
    {
        if (!bone || !child || want.sqrMagnitude < 1e-6f) return;
        var cur = (child.position - bone.position);
        if (cur.sqrMagnitude < 1e-8f) return;
        var q = Quaternion.FromToRotation(cur.normalized, want.normalized);
        bone.rotation = Quaternion.Slerp(Quaternion.identity, q, w) * bone.rotation;
    }

    void LateUpdate()
    {
        if (!searched) Search();
        if (mode == Mode.Drink && Time.time > drinkUntil) mode = Mode.None;
        float target = mode == Mode.None ? 0f : 1f;
        weight = Mathf.MoveTowards(weight, target, Time.deltaTime * (mode == Mode.Drink ? 5f : 4f));
        if (weight <= .001f) { if (shaker) { Destroy(shaker); shaker = null; } return; }
        if (!Ready) return;
        var a = anchor ? anchor : transform;
        Vector3 F = Vector3.ProjectOnPlane(a.forward, Vector3.up).normalized, U = Vector3.up, R = Vector3.Cross(U, F);
        if (F.sqrMagnitude < .01f) F = transform.forward;
        float w = weight;
        switch (mode)
        {
            case Mode.Ride:   // skuter / motorówka: siedzi, pochylony, ręce na kierownicy, kolana zgięte, stopy na podnóżkach
                Aim(spine, chest ? chest : head, U * .82f + F * .5f, w);
                Aim(lUp, lLeg, F * .95f - U * .3f + R * -.12f, w); Aim(rUp, rLeg, F * .95f - U * .3f + R * .12f, w);
                Aim(lLeg, lFoot, -U * .85f + F * .3f, w); Aim(rLeg, rFoot, -U * .85f + F * .3f, w);
                Aim(lArm, lFore, F * .55f - U * .6f + R * -.25f, w); Aim(rArm, rFore, F * .55f - U * .6f + R * .25f, w);
                Aim(lFore, lHand, F * .95f - U * .12f, w); Aim(rFore, rHand, F * .95f - U * .12f, w);
                break;
            case Mode.Sit:    // sofa / ławka / hoker: wyprostowany, uda w przód, łydki w dół, dłonie na udach
                Aim(spine, chest ? chest : head, U * .97f + F * .12f, w);
                Aim(lUp, lLeg, F * .98f - U * .08f + R * -.14f, w); Aim(rUp, rLeg, F * .98f - U * .08f + R * .14f, w);
                Aim(lLeg, lFoot, -U * .92f + F * .18f, w); Aim(rLeg, rFoot, -U * .92f + F * .18f, w);
                Aim(lArm, lFore, -U * .85f + F * .3f + R * -.2f, w); Aim(rArm, rFore, -U * .85f + F * .3f + R * .2f, w);
                Aim(lFore, lHand, F * .9f - U * .25f, w); Aim(rFore, rHand, F * .9f - U * .25f, w);
                break;
            case Mode.Lie:    // łóżko: na plecach, ręce wzdłuż ciała — cała postać obrócona przez anchor (BraxSeat.Lie)
                Aim(lUp, lLeg, F * .98f - U * .05f + R * -.1f, w); Aim(rUp, rLeg, F * .98f - U * .05f + R * .1f, w);
                Aim(lLeg, lFoot, F, w); Aim(rLeg, rFoot, F, w);
                Aim(lArm, lFore, F * .9f + R * -.3f, w); Aim(rArm, rFore, F * .9f + R * .3f, w);
                Aim(lFore, lHand, F, w); Aim(rFore, rHand, F, w);
                break;
            case Mode.Drink:  // shaker: prawa ręka do ust, głowa lekko w tył
                {
                    float t = Mathf.Clamp01((drinkUntil - Time.time) / 2.6f);   // 1 → 0
                    float lift = Mathf.Sin(Mathf.Clamp01(1f - t) * Mathf.PI) ;  // podnosi i opuszcza
                    Vector3 Fp = transform.forward, Rp = transform.right;
                    Aim(rArm, rFore, -U * (1f - .55f * lift) + Fp * (.35f + .3f * lift) + Rp * .15f, w);
                    Aim(rFore, rHand, U * (.2f + .75f * lift) + Fp * (.9f - .6f * lift) + Rp * -.35f * lift, w);
                    if (head) head.rotation = Quaternion.AngleAxis(-14f * lift * w, Rp) * head.rotation;
                    if (!shaker && rHand)
                    {
                        shaker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); shaker.name = "Shaker";
                        Destroy(shaker.GetComponent<Collider>());
                        shaker.transform.SetParent(rHand, false); shaker.transform.localScale = new Vector3(.085f, .1f, .085f) / Mathf.Max(.001f, rHand.lossyScale.x) * .024f * 40f;
                        shaker.transform.localPosition = Vector3.zero;
                        var mr = shaker.GetComponent<MeshRenderer>(); mr.material = new Material(Shader.Find("Standard")) { color = new Color(.2f, .8f, .35f) };
                    }
                }
                break;
        }
        if (mode != Mode.Drink && shaker) { Destroy(shaker); shaker = null; }
    }
}

/// <summary>Siedzisko (sofa, ławka, hoker) albo łóżko: punkt i kierunek siedzenia. Stawiane z kodu (BraxRegionSeats).</summary>
public sealed class BraxSeat : MonoBehaviour
{
    public static readonly List<BraxSeat> All = new List<BraxSeat>();
    public string label = "SOFA";
    public bool lie;
    public float hipsAboveRoot = .95f;    // wysokość bioder nad stopami w pozie stojącej (model Braxa)
    public float surfaceY;                // wysokość siedziska / materaca (świat)
    BraxGymPlayer rider; Vector3 before; Quaternion beforeRot;
    public bool Occupied { get { return rider; } }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static BraxSeat Nearest(Vector3 p, float max)
    {
        BraxSeat best = null; float bd = max;
        foreach (var s in All) { if (!s || s.rider) continue; var d = s.transform.position - p; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = s; } }
        return best;
    }

    public void Use(BraxGymPlayer p)
    {
        if (rider || !p) return;
        rider = p; before = p.transform.position; beforeRot = p.transform.rotation;
        p.seat = this; p.body.enabled = false;
        var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (!lie)
        {
            p.transform.position = new Vector3(transform.position.x, surfaceY - hipsAboveRoot, transform.position.z) - fwd * .1f;
            p.transform.rotation = Quaternion.LookRotation(fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Sit, transform);
        }
        else
        {
            // na plecach: stopy w stronę forward łóżka, głowa w tył; oś postaci pozioma
            p.transform.position = new Vector3(transform.position.x, surfaceY + .12f, transform.position.z) + fwd * .9f;   // stopy przy końcu „forward”, głowa z tyłu
            p.transform.rotation = Quaternion.LookRotation(Vector3.up, -fwd);
            BraxPose.Get(p).Set(BraxPose.Mode.Lie, transform);
        }
    }

    public void Leave()
    {
        if (!rider) return;
        var p = rider; rider = null;
        BraxPose.Get(p).Set(BraxPose.Mode.None, null);
        p.transform.rotation = Quaternion.Euler(0, beforeRot.eulerAngles.y, 0);
        p.transform.position = before;
        p.body.enabled = true; p.seat = null;
    }
}

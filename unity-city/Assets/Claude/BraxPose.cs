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
    // Kierowca (10.11, właściciel 17:37: „ręce na kierownicy — jest animacja driver”): postawę daje klip właściciela „Jazda”
    // (Mixamo Driving.fbx), poza dokłada tylko dłonie na prawdziwym kole / kierownicy.
    public enum Mode { None, Ride, Sit, Lie, Drink, Ster, Kierowca }
    public Mode mode;
    public Transform anchor;         // obiekt, względem którego liczona jest poza (pojazd, siedzisko)
    public float weight;             // 0..1 — płynne wejście/wyjście z pozy
    public float drinkUntil;
    // Celów nie przypinamy do kości ręki: oba pochodzą z nieruchomych punktów
    // uchwytu w oryginalnym modelu jachtu / skutera / motorówki.
    public Transform leftGrip, rightGrip;
    // klip = gra klip „Jazda” (siedzenie i ręce z animacji właściciela). Skuter: z klipu tułów i ręce, nogi okrakiem z kodu.
    public bool klip;


    Transform visual, hips, spine, chest, head, lUp, lLeg, lFoot, rUp, rLeg, rFoot, lArm, lFore, lHand, rArm, rFore, rHand;
    GameObject shaker;
    static Material shakerMat;
    bool searched;

    public static BraxPose Get(BraxGymPlayer p)
    {
        var pose = p.GetComponent<BraxPose>();
        if (!pose) { pose = p.gameObject.AddComponent<BraxPose>(); pose.visual = p.visual; }
        return pose;
    }

    public void Set(Mode m, Transform a)
    {
        mode = m; anchor = m == Mode.None ? null : a;
        if (m == Mode.None) { SetGrips(null, null); klip = false; }
    }
    public void SetGrips(Transform left, Transform right)
    { leftGrip = left; rightGrip = right; }

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


    // Krótki CCD na oryginalnych kościach; nie deformujemy siatki poprzez
    // translację kości ani skalowanie szkieletu. Przy nieosiągalnej kierownicy
    // cel jest ograniczony do naturalnej długości ramienia.
    static void DlonNaUchwyt(Transform bark, Transform lokiec, Transform dlon, Transform uchwyt, float weight)
    {
        if (!bark || !lokiec || !dlon || !uchwyt || weight <= .001f) return;
        float dl1 = Vector3.Distance(bark.position, lokiec.position);
        float dl2 = Vector3.Distance(lokiec.position, dlon.position);
        if (dl1 < .05f || dl2 < .04f) return;
        Vector3 cel = uchwyt.position;
        Vector3 doCel = cel - bark.position;
        float maks = Mathf.Max(.1f, dl1 + dl2 - .025f);
        if (doCel.magnitude > maks)
            cel = bark.position + doCel.normalized * maks;
        float w = Mathf.Clamp01(weight);
        for (int iter = 0; iter < 8; iter++)   // 10.11: 8 kroków (było 3 — dłonie zostawały 8–13 cm od kierownicy skutera)
        {
            Vector3 odBarku = dlon.position - bark.position;
            Vector3 kier = cel - bark.position;
            if (odBarku.sqrMagnitude > .0001f && kier.sqrMagnitude > .0001f)
                bark.rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(odBarku, kier), w * .60f) * bark.rotation;
            Vector3 odLokcia = dlon.position - lokiec.position;
            Vector3 kierL = cel - lokiec.position;
            if (odLokcia.sqrMagnitude > .0001f && kierL.sqrMagnitude > .0001f)
                lokiec.rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(odLokcia, kierL), w * .72f) * lokiec.rotation;
        }
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
            case Mode.Ride:   // SKUTER (właściciel 3.11: „ma normalnie siedzieć”): okrakiem — uda na boki siodła i w dół, łydki w dół
                              // i lekko do tyłu (stopy w zagłębieniach po bokach), tułów pochylony, ręce na kierownicy
                // 10.11 (podgląd): tułów mniej zgarbiony, uda bardziej w przód i mniej na boki, łydki w dół i w tył (stopy w zagłębieniach)
                Aim(spine, chest ? chest : head, U * .9f + F * .4f, w);
                Aim(lUp, lLeg, F * .75f - U * .35f + R * -.45f, w); Aim(rUp, rLeg, F * .75f - U * .35f + R * .45f, w);
                Aim(lLeg, lFoot, -U * .8f - F * .35f + R * -.05f, w); Aim(rLeg, rFoot, -U * .8f - F * .35f + R * .05f, w);
                if (!klip)
                {
                    Aim(lArm, lFore, F * .55f - U * .6f + R * -.25f, w); Aim(rArm, rFore, F * .55f - U * .6f + R * .25f, w);
                    Aim(lFore, lHand, F * .95f - U * .12f, w); Aim(rFore, rHand, F * .95f - U * .12f, w);
                }
                break;
            case Mode.Kierowca:   // motorówka, jacht: siedzenie i ręce z klipu „Jazda”; tułów lekko do koła (klip auta odchyla
                                  // plecy do oparcia, przy sterze łodzi ramiona nie sięgały wieńca), dłonie na kole niżej
                Aim(spine, chest ? chest : head, U * .9f + F * .32f, w * .85f);
                break;
            case Mode.Ster:   // MOTORÓWKA: fotel sternika — wyprostowany, uda w przód, łydki w dół, ręce do koła sterowego przed sobą
                Aim(spine, chest ? chest : head, U * .95f + F * .2f, w);
                Aim(lUp, lLeg, F * .98f - U * .08f + R * -.14f, w); Aim(rUp, rLeg, F * .98f - U * .08f + R * .14f, w);
                Aim(lLeg, lFoot, -U * .92f + F * .18f, w); Aim(rLeg, rFoot, -U * .92f + F * .18f, w);
                Aim(lArm, lFore, F * .6f - U * .55f + R * -.18f, w); Aim(rArm, rFore, F * .6f - U * .55f + R * .18f, w);
                Aim(lFore, lHand, F * .9f + U * .1f + R * .15f, w); Aim(rFore, rHand, F * .9f + U * .1f + R * -.15f, w);
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
                        var mr = shaker.GetComponent<MeshRenderer>();
                        // Jeden materiał na całą grę (audyt 31.10: nowy materiał przy każdym piciu); shader z materiału prymitywu,
                        // gdy „Standard” nie wszedł do buildu — inaczej różowy shaker.
                        if (!shakerMat)
                        {
                            var sh = Shader.Find("Standard"); if (!sh && mr.sharedMaterial) sh = mr.sharedMaterial.shader;
                            if (sh) shakerMat = new Material(sh) { color = new Color(.2f, .8f, .35f) };
                        }
                        if (shakerMat) mr.sharedMaterial = shakerMat;
                    }
                }
                break;
        }
        if (mode == Mode.Ride || mode == Mode.Ster || mode == Mode.Kierowca)
        {
            // Oryginalny Mixamo rig, prawdziwe punkty na kierownicy:
            // ręce poruszają się razem ze sterem przy przechyle na falach.
            DlonNaUchwyt(lArm, lFore, lHand, leftGrip, w);
            DlonNaUchwyt(rArm, rFore, rHand, rightGrip, w);
        }
        if (mode != Mode.Drink && shaker) { Destroy(shaker); shaker = null; }
    }
}

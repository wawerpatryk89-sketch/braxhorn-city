using UnityEngine;

// THE QUAD CLUB'S TURNTABLE (owner 24.11.2026 19:31 "bardziej luksusowo ... koło tego wypożyczalni", his pick "Kłady na
// podestach": the six quads on black podiums as in a showroom, one turning). The one quad on the big podium turns slowly round
// its centre while it stands there with nobody on it (its body still: parked); a rider takes it off and it rides as any
// other - when it is back on the podium, parked, it turns again. Its podium (round, glossy) and the plaza are static.
[DisallowMultipleComponent]
public sealed class BraxChampionsObrotnica : MonoBehaviour
{
    public BraxChampionsKlad klad;
    public Vector3 srodek;                       // world: the podium's centre on its top
    public float promien = 2f;                   // world: the podium's radius (the quad turns while its middle is within it)
    public float stopniNaSekunde = 9f;           // a showroom turntable: one turn in 40 s

    Rigidbody rb;

    void Start() { if (klad) rb = klad.GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        if (!klad || !rb || !rb.isKinematic || klad.Rywal || BraxChampionsKlad.Jedzie == klad) return;
        var d = klad.transform.position - srodek; d.y = 0f;
        if (d.magnitude > promien * .5f || Mathf.Abs(klad.transform.position.y - srodek.y) > promien) return;   // ridden off its podium
        var q = Quaternion.Euler(0f, stopniNaSekunde * Time.fixedDeltaTime, 0f);
        rb.MovePosition(srodek + q * (klad.transform.position - srodek));
        rb.MoveRotation(q * klad.transform.rotation);
    }
}

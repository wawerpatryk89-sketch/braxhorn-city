using UnityEngine;

// BRAXHORN — piłka lekarska na podłodze siłowni (właściciel 1.11, 18:12: „przechodzi się przez nie normalnie, a powinny
// mieć ciało”). Kula z SphereCollider i Rigidbody (ok. 5 kg, opór toczenia — szybko staje i zasypia). Kapsuła gracza
// (CharacterController) sama nie pcha brył fizyki, więc: Brax idący na piłkę przekazuje jej swoją prędkość (w stronę
// piłki), a CIOS / KOP (BraxGymPlayer.OnCios) w zasięgu 1.3 m przed nim — impuls w kierunku uderzenia (kop mocniej).
// Liczone tylko, gdy Brax jest bliżej niż 4 m albo piłka się toczy.
[RequireComponent(typeof(Rigidbody))]
public sealed class BraxPilka : MonoBehaviour
{
    public float promien = .17f;
    Rigidbody rb;
    static Transform gracz; static CharacterController cialo; static float szukaj;
    Vector3 ostatnio; bool jest;

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void OnEnable() { BraxGymPlayer.OnCios += Cios; }
    void OnDisable() { BraxGymPlayer.OnCios -= Cios; }

    static void ZnajdzGracza()
    {
        if (gracz || Time.unscaledTime < szukaj) return;
        szukaj = Time.unscaledTime + 1f;
        var p = FindAnyObjectByType<BraxGymPlayer>();
        if (p) { gracz = p.transform; cialo = p.GetComponent<CharacterController>(); }
    }

    void Cios(Vector3 kto, Vector3 kierunek, bool kop)
    {
        if (!isActiveAndEnabled || !rb) return;
        var d = transform.position - kto; d.y = 0f; kierunek.y = 0f;
        if (d.magnitude - promien > 1.3f || kierunek.sqrMagnitude < .01f) return;
        kierunek.Normalize();
        if (d.sqrMagnitude > .0001f && Vector3.Dot(kierunek, d.normalized) < .35f) return;
        rb.WakeUp();
        rb.AddForce(kierunek * (kop ? 5.5f : 3f) + Vector3.up * (kop ? 1.6f : .6f), ForceMode.VelocityChange);
    }

    void FixedUpdate()
    {
        ZnajdzGracza();
        if (!gracz || !rb) return;
        var p = gracz.position;
        var d = transform.position - p; d.y = 0f;
        if (d.sqrMagnitude > 16f) { jest = false; return; }
        var v = jest ? (p - ostatnio) / Mathf.Max(1e-4f, Time.fixedDeltaTime) : Vector3.zero; v.y = 0f;
        ostatnio = p; jest = true;
        float r = (cialo ? cialo.radius : .35f) + promien + .06f;
        if (d.magnitude < r && transform.position.y < p.y + 1.2f && d.sqrMagnitude > 1e-6f)
        {
            var n = d.normalized; float naPilke = Vector3.Dot(v, n);
            if (naPilke > .05f)
            {
                rb.WakeUp();
                var cel = n * Mathf.Min(naPilke * 1.15f + .3f, 6f);
                var vh = rb.linearVelocity; vh.y = 0f;
                if (Vector3.Dot(vh, n) < cel.magnitude) rb.AddForce(cel - n * Vector3.Dot(vh, n), ForceMode.VelocityChange);
            }
        }
    }
}

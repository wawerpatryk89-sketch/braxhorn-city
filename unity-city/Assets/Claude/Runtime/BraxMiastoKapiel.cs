using UnityEngine;

// MIASTO R1 — STREFA KĄPIELI W MORZU MIASTA (18.11.2026; właściciel 13:04 „tak, zrób tak” — pływanie i nurkowanie z gałęzi
// Mistrzów, wzór BraxChampionsPoolSwimVolume). Wspólna BraxSwimmingZone z progiem głębokości: pływa się dopiero tam, gdzie woda
// sięga ponad pas (stopy > 0.9 m pod lustrem; powrót do chodzenia przy 0.7 m — bez migania trybu na granicy), płyciej zwykły
// chód po piasku. Trzymany NURKUJ (BraxMiastoNurek): strefa podaje graczowi lustro obniżone o głębokość nurkowania przez
// publiczne EnterSwimmingZone — wspólna wyporność BraxGymPlayer niesie ciało w dół do dna i z powrotem po puszczeniu.
public sealed class BraxMiastoKapiel : BraxSwimmingZone
{
    public float wejscie = .9f, wyjscie = .7f;   // głębokość stóp pod lustrem: zaczyna pływać / wraca do chodzenia
    BoxCollider pudlo;
    internal Bounds Obszar => pudlo ? pudlo.bounds : new Bounds(transform.position, Vector3.zero);

    protected override void Awake() { base.Awake(); pudlo = GetComponent<BoxCollider>(); }
    void OnEnable() { if (!BraxMiastoNurek.Strefy.Contains(this)) BraxMiastoNurek.Strefy.Add(this); }
    void OnDisable() { BraxMiastoNurek.Strefy.Remove(this); }

    protected override void OnTriggerEnter(Collider other) { Woda(other); }
    protected override void OnTriggerStay(Collider other) { Woda(other); }

    void Woda(Collider other)
    {
        if (!allowsSwimming || !waterSurface) return;
        var p = other.GetComponent<BraxGymPlayer>();
        if (!p) p = other.GetComponentInParent<BraxGymPlayer>();
        if (!p || p.vessel || p.seat) return;
        float lustro = waterSurface.position.y, gl = lustro - p.transform.position.y;
        if (!p.Swimming && gl < wejscie) return;                                // płytko: chodzi po piasku
        if (p.Swimming && gl < wyjscie && BraxMiastoNurek.Glebokosc <= 0f) { p.ExitSwimmingZone(GetInstanceID()); return; }
        BraxMiastoNurek.PlywaDo = Time.time + .3f;
        p.EnterSwimmingZone(GetInstanceID(), lustro - BraxMiastoNurek.Glebokosc);
    }
}

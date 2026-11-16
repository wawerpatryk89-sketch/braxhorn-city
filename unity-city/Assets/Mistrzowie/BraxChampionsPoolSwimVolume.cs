using UnityEngine;

// Compatibility adapter retaining the P1 component name and Unity GUID.
// All real water-entry logic is in the shared BraxSwimmingZone, usable in
// every BRAXHORN CITY region. No P1-only player animation or physics code.
// 16.11 (owner "zanurkuję"): the region's zone also gives the dive - while NURKUJ is held (BraxChampionsPodWoda) it hands
// the player the surface lowered by the dive depth through the shared public EnterSwimmingZone; the shared buoyancy does
// the rest. It also tells BraxChampionsPodWoda where the water is (the veil when the camera is under the surface).
public sealed class BraxChampionsPoolSwimVolume : BraxSwimmingZone
{
    BoxCollider pudlo;
    internal Bounds Obszar => pudlo ? pudlo.bounds : new Bounds(transform.position, Vector3.zero);

    protected override void Awake() { base.Awake(); pudlo = GetComponent<BoxCollider>(); }
    void OnEnable() { if (!BraxChampionsPodWoda.Strefy.Contains(this)) BraxChampionsPodWoda.Strefy.Add(this); }
    void OnDisable() { BraxChampionsPodWoda.Strefy.Remove(this); }

    protected override void OnTriggerEnter(Collider other) { base.OnTriggerEnter(other); Nurek(other); }
    protected override void OnTriggerStay(Collider other) { base.OnTriggerStay(other); Nurek(other); }

    void Nurek(Collider other)
    {
        if (!allowsSwimming || !waterSurface) return;
        var p = other.GetComponent<BraxGymPlayer>();
        if (!p) p = other.GetComponentInParent<BraxGymPlayer>();
        if (!p || !p.Swimming) return;
        BraxChampionsPodWoda.PlywaDo = Time.time + .3f;
        if (BraxChampionsPodWoda.Glebokosc > 0f) p.EnterSwimmingZone(GetInstanceID(), waterSurface.position.y - BraxChampionsPodWoda.Glebokosc);
    }
}

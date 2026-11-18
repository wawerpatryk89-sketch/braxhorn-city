using UnityEngine;

// Reusable water volume for the player's SHARED BraxGymPlayer controller.
// Region-neutral: use in Mistrzowie, first City, Estate, lake, islands, etc.
// Does not change animation clips, vehicles, boats, rig or touch controls.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class BraxSwimmingZone : MonoBehaviour
{
    public Transform waterSurface;
    public bool allowsSwimming = true;
    public string basinId;

    protected virtual void Awake()
    {
        var volume = GetComponent<BoxCollider>();
        if (volume) volume.isTrigger = true; // water must not be a solid floor
        if (!waterSurface) Debug.LogWarning("Water zone missing surface: " + name);
    }

    protected virtual void OnTriggerEnter(Collider other) { Enter(other); }
    protected virtual void OnTriggerStay(Collider other) { Enter(other); }
    protected virtual void OnTriggerExit(Collider other)
    {
        var p = other.GetComponent<BraxGymPlayer>();
        if (!p) p = other.GetComponentInParent<BraxGymPlayer>();
        if (p) p.ExitSwimmingZone(GetInstanceID());
    }

    void Enter(Collider other)
    {
        if (!allowsSwimming || !waterSurface) return;
        var p = other.GetComponent<BraxGymPlayer>();
        if (!p) p = other.GetComponentInParent<BraxGymPlayer>();
        if (!p) return;
        // Never steal control from a boarded boat/car or a seated character.
        if (p.vessel || p.seat) return;
        p.EnterSwimmingZone(GetInstanceID(), waterSurface.position.y);
    }
}

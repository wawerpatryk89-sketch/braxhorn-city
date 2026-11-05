using UnityEngine;

// Mistrzowie P1 ONLY. A non-solid water volume: the player falls into the
// actual tiled basin and receives swimming buoyancy from BraxGymPlayer.
// Existing city, estate, arena and camp locomotion remain unchanged.
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BraxChampionsPoolSwimVolume : MonoBehaviour
{
    public Transform waterSurface;
    public bool allowsSwimming=true;
    public string basinId;

    void Awake()
    {
        var box=GetComponent<BoxCollider>();
        box.isTrigger=true;
        if(!waterSurface)Debug.LogError("P1 water volume missing visual water reference: "+basinId);
    }
    void OnTriggerEnter(Collider other) { Contact(other); }
    void OnTriggerStay(Collider other) { Contact(other); }
    void OnTriggerExit(Collider other)
    {
        var p=other.GetComponent<BraxGymPlayer>()??other.GetComponentInParent<BraxGymPlayer>();
        if(p!=null && p.p1WaterVolumeId==GetInstanceID())
        {
            p.p1Swimming=false;p.p1WaterVolumeId=0;
        }
    }
    void Contact(Collider other)
    {
        if(!waterSurface)return;
        var p=other.GetComponent<BraxGymPlayer>()??other.GetComponentInParent<BraxGymPlayer>();
        if(p==null||p.cityWorld==null||p.cityWorld.area!="mistrzowie")return;
        // Sports/family deep basins have swimming buoyancy. In the shallow
        // children pool the character only wades; no forced submersion.
        p.p1Swimming=allowsSwimming;
        p.p1WaterSurfaceY=waterSurface.position.y;
        p.p1WaterVolumeId=GetInstanceID();
    }
}

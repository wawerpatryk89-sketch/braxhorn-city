using UnityEngine;

// Camp fixture lights are limited to the player's immediate building area.
public sealed class BraxObozLocalLight : MonoBehaviour
{
    public Light source;
    public BraxGymPlayer player;
    public float range=22*1.294f;
    float next;
    void Update()
    {
        if(!source||!player||Time.unscaledTime<next)return;
        next=Time.unscaledTime+.5f;
        source.enabled=(player.transform.position-transform.position).sqrMagnitude<range*range;
    }
}

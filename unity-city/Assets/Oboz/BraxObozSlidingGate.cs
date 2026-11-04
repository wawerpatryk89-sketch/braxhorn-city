using UnityEngine;

// Local camp gate. Colliders stay attached to the moving leaf.
public sealed class BraxObozSlidingGate : MonoBehaviour
{
    public Transform leaf;
    public Vector3 openOffset;
    public float detectionRadius=25f, speed=4f, closeDelay=5f;
    public BraxGymPlayer player;
    Vector3 closed;
    float lastApproach=-100f;
    void Start() { if(leaf)closed=leaf.localPosition; }
    void Update()
    {
        if(!leaf||!player)return;
        var delta=player.transform.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude<detectionRadius*detectionRadius)lastApproach=Time.time;
        // A long delay prevents closure while the character passes the opening.
        bool open=Time.time-lastApproach<closeDelay;
        leaf.localPosition=Vector3.MoveTowards(leaf.localPosition,closed+(open?openOffset:Vector3.zero),speed*Time.deltaTime);
    }
}

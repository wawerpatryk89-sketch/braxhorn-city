using UnityEngine;

// Isolated gym detail. Keeps city, character, fight, lighting and input unchanged.
[DisallowMultipleComponent]
public sealed class BraxGymCeilingFan : MonoBehaviour
{
    [Range(0f, 90f)]
    public float revolutionsPerMinute = 28f;

    void Update()
    {
        // Real frame-time rotation: 6 degrees/second per RPM.
        // No physics or shadow-casting on the low-cost ceiling rotor.
        transform.Rotate(0f, revolutionsPerMinute * 6f * Time.deltaTime, 0f, Space.Self);
    }
}

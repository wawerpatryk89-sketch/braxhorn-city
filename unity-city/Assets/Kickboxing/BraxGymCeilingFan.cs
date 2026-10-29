using UnityEngine;

// KICKBOXING ROOM ONLY. Slow ambient rotation for the two high-mounted
// ceiling air circulators. No player, camera or city state is modified.
public sealed class BraxGymCeilingFan : MonoBehaviour
{
    public Transform rotor;
    [Range(0f, 120f)] public float revolutionsPerMinute = 52f;

    void Update()
    {
        if (!rotor || revolutionsPerMinute <= 0f) return;
        rotor.Rotate(0f, revolutionsPerMinute * 6f * Time.deltaTime, 0f, Space.Self);
    }
}

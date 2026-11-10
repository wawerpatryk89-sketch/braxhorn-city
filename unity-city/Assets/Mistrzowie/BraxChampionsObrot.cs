using UnityEngine;

// Owner 10.11 09:16: "kebaba tego musisz stworzyć, tego co się kręci ... do cięcia". Turns the doner meat cone (and the
// spits of the chicken rotisserie) slowly about its own axis. Only while the camera is near: one distance check
// every half second, no physics, never static-batched (BraxChampionsPromenadeEquipment leaves these objects dynamic).
[DisallowMultipleComponent]
public sealed class BraxChampionsObrot : MonoBehaviour
{
    public Vector3 os = Vector3.up;
    public float stopniNaSekunde = 24f;
    public float zasieg = 80f;
    float nastepny;
    bool blisko = true;

    void Update()
    {
        if (Time.unscaledTime >= nastepny)
        {
            nastepny = Time.unscaledTime + .5f;
            var kamera = Camera.main;
            blisko = !kamera || (kamera.transform.position - transform.position).sqrMagnitude < zasieg * zasieg;
        }
        if (blisko) transform.Rotate(os, stopniNaSekunde * Time.deltaTime, Space.Self);
    }
}

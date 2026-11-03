using UnityEngine;

// Obrotowy podest w salonie samochodowym (runda 5): talerz z autem obraca się powoli wokół osi pionowej (ok. 1 obrót / 50 s).
// Tylko gdy widać talerz (Renderer.isVisible) — bez kosztu poza kadrem. Kolizja auta jest dzieckiem talerza (obraca się z nim).
public sealed class BraxObrotowyPodest : MonoBehaviour
{
    public float stopniNaSekunde = 7.2f;
    public Renderer talerz;

    void Update()
    {
        if (talerz && !talerz.isVisible) return;
        transform.Rotate(0f, stopniNaSekunde * Time.deltaTime, 0f, Space.Self);
    }
}

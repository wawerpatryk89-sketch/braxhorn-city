using UnityEngine;

// BRAXHORN — wolny obrót wokół pionowej osi obiektu (MIASTO R1, 10.11: rożen kebaba w barach — właściciel 09:10: „kebaba, który się
// kręci”). Mięso na pręcie (Library/kebab_mieso.glb, oś obrotu = oś Y modelu) obraca się jak w prawdziwym rożnie, ok. 1 obrót na 20 s.
// Koszt: jeden Transform.Rotate na klatkę i tylko gdy obiekt jest widoczny (OnBecameVisible / Invisible na rendererze dziecka nie
// dochodzi do rodzica — dlatego sprawdzamy odległość od kamery co 0.5 s i dalej niż 25 m nie kręcimy).
public sealed class BraxObrot : MonoBehaviour
{
    public float stopniNaSekunde = 18f;
    public float zasieg = 25f;
    bool blisko = true; float sprawdz;

    void Update()
    {
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            var cam = Camera.main;
            blisko = cam == null || (cam.transform.position - transform.position).sqrMagnitude < zasieg * zasieg;
        }
        if (blisko) transform.Rotate(0f, stopniNaSekunde * Time.deltaTime, 0f, Space.World);
    }
}
